using Nop.Core.Caching;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Stores;
using Nop.Core.Domain.Discounts;
using Nop.Plugin.Aptean.DynamicPricing.Services;
using Nop.Services.Catalog;
using Nop.Services.Directory;
using Nop.Services.Discounts;
using Nop.Services.Customers;
using Nop.Services.Tax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nop.Core.Domain.Directory;
using Nop.Services.Logging;

namespace Nop.Plugin.Aptean.DynamicPricing.Overrides;

public class CustomPriceCalculationService : PriceCalculationService
{
    private readonly IERPPriceService _erpPriceService;
    private readonly ILogger _logger;
    private readonly IStaticCacheManager _staticCacheManager;
    private static Dictionary<int, decimal> _cachedItemPrices = new Dictionary<int, decimal>();

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
        ILogger logger)
        : base(catalogSettings, currencySettings, categoryService, currencyService, customerService,
              discountService, manufacturerService, productAttributeParser, productService, staticCacheManager)
    {
        _erpPriceService = erpPriceService;
        _logger = logger;
        _staticCacheManager = staticCacheManager;
    }

    public override async Task<(decimal priceWithoutDiscounts, decimal finalPrice, decimal appliedDiscountAmount, List<Discount> appliedDiscounts)> GetFinalPriceAsync(Product product,
            Customer customer,
            Store store,
            decimal? overriddenProductPrice = null,
            decimal additionalCharge = 0,
            bool includeDiscounts = true,
            int quantity = 1,
            DateTime? rentalStartDate = null,
            DateTime? rentalEndDate = null)
    {
        if (!_erpPriceService.IsDynamicPricingEnabled())
        {
            return await base.GetFinalPriceAsync(product, customer, store, overriddenProductPrice, additionalCharge, includeDiscounts, quantity,
            rentalStartDate, rentalEndDate);
        }

        ArgumentNullException.ThrowIfNull(product);

        var cacheKey = _staticCacheManager.PrepareKeyForDefaultCache(
            new CacheKey($"ProductPrice-{product.Id}-{customer.Id}-{quantity}", $"{customer.Id}"),
            product.Id,
            quantity,
            customer.Id);

        if (!_catalogSettings.CacheProductPrices || product.IsRental)
            cacheKey.CacheTime = 0;

        if (_cachedItemPrices.TryGetValue(product.Id, out var cachedPrice))
        {
            return (cachedPrice, cachedPrice * quantity, 0, new List<Discount>());
        }

        var cachedResult = await _staticCacheManager.GetAsync(cacheKey, async () =>
        {
            var discounts = new List<Discount>();
            var appliedDiscountAmount = decimal.Zero;

            // Use ERP API to get the dynamic price
            var erpPrices = await _erpPriceService.GetProductPricesAsync(customer.Id, new List<int> { product.Id });

            var price = overriddenProductPrice ?? product.Price;
            if (erpPrices.TryGetValue(product.Id, out var erpPrice))
            {
                price = erpPrice;
            }

            // Add additional charges
            price += additionalCharge;

            var priceWithoutDiscount = price;

            // Apply discounts if necessary
            if (includeDiscounts)
            {
                var (tmpDiscountAmount, tmpAppliedDiscounts) = await GetDiscountAmountAsync(product, customer, price);
                price -= tmpDiscountAmount;

                if (tmpAppliedDiscounts?.Any() ?? false)
                {
                    discounts.AddRange(tmpAppliedDiscounts);
                    appliedDiscountAmount = tmpDiscountAmount;
                }
            }

            if (price < decimal.Zero)
                price = decimal.Zero;

            if (priceWithoutDiscount < decimal.Zero)
                priceWithoutDiscount = decimal.Zero;

            _cachedItemPrices[product.Id] = price;

            return (priceWithoutDiscount, price, appliedDiscountAmount, discounts);
        });

        if (cachedResult.price == 0 && quantity > 1)
        {
            // Recalculate pricing in case the cache was set with the wrong quantity before
            await _staticCacheManager.RemoveAsync(cacheKey);
            return await GetFinalPriceAsync(product, customer, store, overriddenProductPrice, additionalCharge, includeDiscounts, quantity, rentalStartDate, rentalEndDate);
        }

        return cachedResult;
    }
}
