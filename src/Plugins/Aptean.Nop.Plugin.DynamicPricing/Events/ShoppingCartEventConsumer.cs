using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Orders;
using Nop.Core.Events;
using Nop.Core.Domain.Catalog;
using Nop.Plugin.Aptean.DynamicPricing.Services;
using Nop.Services.Customers;
using Nop.Services.Events;
using Nop.Services.Logging;
using Nop.Services.Orders;
using Nop.Services.Catalog;
using Nop.Web.Factories;
using Nop.Web.Models.Catalog;

namespace Nop.Plugin.Aptean.DynamicPricing.Events;

public class ShoppingCartEventConsumer : IConsumer<EntityInsertedEvent<ShoppingCartItem>>, IConsumer<EntityUpdatedEvent<ShoppingCartItem>>
{
    private readonly IERPPriceService _erpPriceService;
    private readonly ICustomerService _customerService;
    private readonly IShoppingCartService _shoppingCartService;
    private readonly IProductService _productService;
    private readonly IProductModelFactory _productModelFactory;
    private readonly ILogger _logger;
    private readonly Dictionary<int, decimal> _cachedPrices;
    private bool _isUpdatingCartItem = false;

    public ShoppingCartEventConsumer(IERPPriceService erpPriceService, ICustomerService customerService, IShoppingCartService shoppingCartService, IProductService productService, IProductModelFactory productModelFactory, ILogger logger)
    {
        _erpPriceService = erpPriceService;
        _customerService = customerService;
        _shoppingCartService = shoppingCartService;
        _productService = productService;
        _productModelFactory = productModelFactory;
        _logger = logger;
        _cachedPrices = new Dictionary<int, decimal>();
    }

    public async Task HandleEventAsync(EntityInsertedEvent<ShoppingCartItem> eventMessage)
    {
        if (_isUpdatingCartItem)
            return;

        _logger.Information("Handling shopping cart item inserted event.");
        await UpdateCartPricesAsync(eventMessage.Entity, true);
    }

    public async Task HandleEventAsync(EntityUpdatedEvent<ShoppingCartItem> eventMessage)
    {
        if (_isUpdatingCartItem)
            return;

        _logger.Information("Handling shopping cart item updated event.");
        await UpdateCartPricesAsync(eventMessage.Entity, false);
    }

    private async Task UpdateCartPricesAsync(ShoppingCartItem cartItem, bool isNewItem)
    {
        try
        {
            _logger.Information($"Updating prices for cart item: ProductId = {cartItem.ProductId}, Quantity = {cartItem.Quantity}");

            // Retrieve the customer associated with the shopping cart item
            var customer = await _customerService.GetCustomerByIdAsync(cartItem.CustomerId);
            if (customer == null)
            {
                _logger.Warning($"Customer with ID {cartItem.CustomerId} not found.");
                return;
            }

            // If the item is new or the quantity has changed, update the price
            if (isNewItem || !_cachedPrices.ContainsKey(cartItem.ProductId) || _cachedPrices[cartItem.ProductId] != cartItem.CustomerEnteredPrice)
            {
                // Retrieve all items in the customer's shopping cart
                var cartItems = await _shoppingCartService.GetShoppingCartAsync(customer, ShoppingCartType.ShoppingCart);

                // Construct a dictionary of product IDs and quantities in the cart
                var productQuantities = cartItems.ToDictionary(item => item.ProductId, item => item.Quantity);

                // Retrieve updated prices from ERP and update the cart items accordingly
                var priceResponse = await _erpPriceService.GetCartPricesAsync(customer.Id, productQuantities);

                foreach (var item in cartItems)
                {
                    if (priceResponse.TryGetValue(item.ProductId, out var price))
                    {
                        if (item.CustomerEnteredPrice != price)
                        {
                            _logger.Information($"Updating price for ProductId = {item.ProductId} to {price}");
                            item.CustomerEnteredPrice = price;
                            _cachedPrices[item.ProductId] = price;

                            // Set the flag to indicate that the cart is being updated programmatically
                            _isUpdatingCartItem = true;

                            // Persist changes to the shopping cart item using the shopping cart service with a timeout mechanism
                            var updateTask = _shoppingCartService.UpdateShoppingCartItemAsync(customer, item.Id, item.AttributesXml, item.CustomerEnteredPrice, item.RentalStartDateUtc, item.RentalEndDateUtc, item.Quantity, false);

                            // Wait for either the update task to complete or a timeout of 10 seconds
                            var completedTask = await Task.WhenAny(updateTask, Task.Delay(TimeSpan.FromSeconds(10)));

                            if (completedTask == updateTask)
                            {
                                var warnings = await updateTask;
                                if (warnings.Count == 0)
                                {
                                    _logger.Information($"Successfully updated price for ProductId = {item.ProductId} to {price}");
                                }
                                else
                                {
                                    _logger.Warning($"Warnings while updating price for ProductId = {item.ProductId}: {string.Join(", ", warnings)}");
                                }
                            }
                            else
                            {
                                _logger.Warning($"Timeout occurred while updating price for ProductId = {item.ProductId}");
                            }

                            // Reset the flag after the operation is complete
                            _isUpdatingCartItem = false;

                            // Update the product price in the database
                            var product = await _productService.GetProductByIdAsync(item.ProductId);
                            if (product != null)
                            {
                                product.Price = price;
                                await _productService.UpdateProductAsync(product);
                                _logger.Information($"Product price for ProductId = {item.ProductId} updated in the database to {price}");
                            }
                            else
                            {
                                _logger.Warning($"Product with ID {item.ProductId} not found in the database.");
                            }
                        }
                        else
                        {
                            _logger.Information($"Price for ProductId = {item.ProductId} is already up-to-date.");
                        }
                    }
                    else
                    {
                        _logger.Warning($"No price found for ProductId = {item.ProductId} from ERP response.");
                    }
                }
            }
            else
            {
                _logger.Information($"Price for ProductId = {cartItem.ProductId} is already up-to-date.");
            }
        }
        catch (Exception ex)
        {
            _logger.Error("Error occurred while updating cart prices.", ex);
        }
        finally
        {
            // Ensure the flag is reset in case of an exception
            _isUpdatingCartItem = false;
        }
    }
}
