using System.Collections.Generic;
using System.Threading.Tasks;
using Nop.Core.Domain.Orders;
using Nop.Services.Catalog;
using Nop.Services.Orders;
using Nop.Services.Logging;
using Nop.Services.Customers;
using Nop.Services.Tax;
using Nop.Services.Directory;
using Nop.Services.Localization;
using Nop.Services.Common;
using Nop.Services.Events;
using Nop.Plugin.Aptean.DynamicPricing.Services;
using Nop.Core.Events;
using Nop.Core;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Routing;
using Nop.Core.Caching;
using Nop.Core.Domain.Catalog;
using Nop.Data;
using Nop.Services.Attributes;
using Nop.Services.Helpers;
using Nop.Services.Security;
using Nop.Services.Seo;
using Nop.Services.Shipping.Date;
using Nop.Services.Shipping;
using Nop.Services.Stores;
using Nop.Core.Domain.Customers;

namespace Nop.Plugin.Aptean.DynamicPricing.Overrides;

public class CustomShoppingCartService : ShoppingCartService
{
    private readonly IERPPriceService _erpPriceService;

    public CustomShoppingCartService(
        IERPPriceService erpPriceService,
        CatalogSettings catalogSettings,
        IAclService aclService,
        IActionContextAccessor actionContextAccessor,
        IAttributeParser<CheckoutAttribute, CheckoutAttributeValue> checkoutAttributeParser,
        IAttributeService<CheckoutAttribute, CheckoutAttributeValue> checkoutAttributeService,
        ICurrencyService currencyService,
        ICustomerService customerService,
        IDateRangeService dateRangeService,
        IDateTimeHelper dateTimeHelper,
        IEventPublisher eventPublisher,
        IGenericAttributeService genericAttributeService,
        ILocalizationService localizationService,
        IPermissionService permissionService,
        IPriceCalculationService priceCalculationService,
        IPriceFormatter priceFormatter,
        IProductAttributeParser productAttributeParser,
        IProductAttributeService productAttributeService,
        IProductService productService,
        IRepository<ShoppingCartItem> sciRepository,
        IShippingService shippingService,
        IShortTermCacheManager shortTermCacheManager,
        IStaticCacheManager staticCacheManager,
        IStoreContext storeContext,
        IStoreService storeService,
        IStoreMappingService storeMappingService,
        IUrlHelperFactory urlHelperFactory,
        IUrlRecordService urlRecordService,
        IWorkContext workContext,
        OrderSettings orderSettings,
        ShoppingCartSettings shoppingCartSettings,
        ILogger logger) : base(
            catalogSettings, aclService, actionContextAccessor, checkoutAttributeParser, checkoutAttributeService, currencyService, customerService, dateRangeService, dateTimeHelper,
            eventPublisher, genericAttributeService, localizationService, permissionService, priceCalculationService, priceFormatter, productAttributeParser, productAttributeService,
            productService, sciRepository, shippingService, shortTermCacheManager, staticCacheManager, storeContext, storeService, storeMappingService, urlHelperFactory, urlRecordService, workContext, orderSettings, shoppingCartSettings)
    {
        _erpPriceService = erpPriceService;
    }

    public override async Task<IList<string>> UpdateShoppingCartItemAsync(
        Customer customer,
        int shoppingCartItemId,
        string attributesXml,
        decimal customerEnteredPrice,
        DateTime? rentalStartDate = null,
        DateTime? rentalEndDate = null,
        int quantity = 1,
        bool resetCheckoutData = true)
    {
        ArgumentNullException.ThrowIfNull(customer);

        var warnings = new List<string>();

        var shoppingCartItem = await _sciRepository.GetByIdAsync(shoppingCartItemId, cache => default);

        if (shoppingCartItem == null || shoppingCartItem.CustomerId != customer.Id)
            return warnings;

        if (resetCheckoutData)
        {
            // Reset checkout data
            await _customerService.ResetCheckoutDataAsync(customer, shoppingCartItem.StoreId);
        }

        var product = await _productService.GetProductByIdAsync(shoppingCartItem.ProductId);

        if (quantity > 0)
        {
            // Fetch dynamic price from ERP
            var prices = await _erpPriceService.GetProductPricesAsync(customer.Id, new List<int> { product.Id });

            if (prices.TryGetValue(product.Id, out var erpPrice))
            {
                customerEnteredPrice = erpPrice;
            }

            // Check warnings
            warnings.AddRange(await GetShoppingCartItemWarningsAsync(customer, shoppingCartItem.ShoppingCartType,
                product, shoppingCartItem.StoreId,
                attributesXml, customerEnteredPrice,
                rentalStartDate, rentalEndDate, quantity, false, shoppingCartItemId));
            if (warnings.Any())
                return warnings;

            // If everything is OK, update the shopping cart item
            shoppingCartItem.Quantity = quantity;
            shoppingCartItem.AttributesXml = attributesXml;
            shoppingCartItem.CustomerEnteredPrice = customerEnteredPrice;
            shoppingCartItem.RentalStartDateUtc = rentalStartDate;
            shoppingCartItem.RentalEndDateUtc = rentalEndDate;
            shoppingCartItem.UpdatedOnUtc = DateTime.UtcNow;

            await _sciRepository.UpdateAsync(shoppingCartItem);
        }
        else
        {
            // Check warnings for required products
            warnings.AddRange(await GetRequiredProductWarningsAsync(customer, shoppingCartItem.ShoppingCartType,
                product, shoppingCartItem.StoreId, quantity, false, shoppingCartItemId));
            if (warnings.Any())
                return warnings;

            // Delete a shopping cart item
            await DeleteShoppingCartItemAsync(shoppingCartItem, resetCheckoutData, true);
        }

        return warnings;
    }

}
