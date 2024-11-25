using Microsoft.Extensions.Logging;
using Nop.Core.Caching;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Directory;
using Nop.Core.Domain.Discounts;
using Nop.Core.Domain.Stores;
using Nop.Plugin.Aptean.DynamicPricing.Services;
using Nop.Services.Catalog;
using Nop.Services.Customers;
using Nop.Services.Directory;
using Nop.Services.Discounts;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Nop.Plugin.Aptean.DynamicPricing.Overrides;

public class CustomPriceCalculationService : PriceCalculationService
{
    private readonly IERPPriceService _erpPriceService;
    private readonly ILogger<CustomPriceCalculationService> _logger;

    public CustomPriceCalculationService(
           CatalogSettings catalogSettings,
           CurrencySettings currencySettings,
           ICategoryService categoryService,
           ICurrencyService currencyService,
           ICustomerService customerService,
           IDiscountService discountService,
           IManufacturerService manufacturerService,
           IProductAttributeParser productAttributeParser,
           IProductService productService,
           IStaticCacheManager staticCacheManager,
           IERPPriceService erpPriceService,
           ILogger<CustomPriceCalculationService> logger)
           : base(catalogSettings, currencySettings, categoryService, currencyService, customerService,
                  discountService, manufacturerService, productAttributeParser, productService, staticCacheManager)
    {
        _erpPriceService = erpPriceService;
        _logger = logger;
    }

    public override async Task<(decimal priceWithoutDiscounts, decimal finalPrice, decimal appliedDiscountAmount, List<Discount> appliedDiscounts)> GetFinalPriceAsync(Product product,
         Customer customer,
         Store store,
         decimal additionalCharge = 0,
         bool includeDiscounts = true,
         int quantity = 1)
    {
        // Custom logic for dynamic pricing
        _logger.LogInformation($"Getting final price for ProductId: {product.Id}, CustomerId: {customer.Id}");

        var erpPrices = await _erpPriceService.GetProductPricesAsync(customer.Id, new List<int> { product.Id });

        if (erpPrices.TryGetValue(product.Id, out var erpPrice))
        {
            _logger.LogInformation($"Fetched price from ERP: {erpPrice} for ProductId: {product.Id}");

            return (erpPrice, erpPrice + additionalCharge, 0, new List<Discount>());
        }

        // Fallback to base implementation
        _logger.LogWarning($"No ERP price found for ProductId: {product.Id}, falling back to default pricing.");

        return await base.GetFinalPriceAsync(product, customer, store, additionalCharge, includeDiscounts, quantity);
    }

}
