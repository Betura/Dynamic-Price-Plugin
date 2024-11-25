using System.Collections.Generic;
using System.Threading.Tasks;
using Nop.Core.Caching;
using Nop.Core;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Media;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Security;
using Nop.Core.Domain.Seo;
using Nop.Core.Domain.Shipping;
using Nop.Core.Domain.Vendors;
using Nop.Plugin.Aptean.DynamicPricing.Services;
using Nop.Services.Catalog;
using Nop.Services.Common;
using Nop.Services.Customers;
using Nop.Services.Directory;
using Nop.Services.Helpers;
using Nop.Services.Localization;
using Nop.Services.Logging;
using Nop.Services.Media;
using Nop.Services.Orders;
using Nop.Services.Security;
using Nop.Services.Seo;
using Nop.Services.Shipping.Date;
using Nop.Services.Stores;
using Nop.Services.Tax;
using Nop.Services.Vendors;
using Nop.Web.Factories;
using Nop.Web.Models.Catalog;

namespace Nop.Plugin.Aptean.DynamicPricing.Overrides
{
    public class CustomProductModelFactory : ProductModelFactory
    {
        private readonly IERPPriceService _erpPriceService;
        private readonly ILogger _logger;

        public CustomProductModelFactory(
            CaptchaSettings captchaSettings,
            CatalogSettings catalogSettings,
            CustomerSettings customerSettings,
            ICategoryService categoryService,
            ICurrencyService currencyService,
            ICustomerService customerService,
            IDateRangeService dateRangeService,
            IDateTimeHelper dateTimeHelper,
            IDownloadService downloadService,
            IGenericAttributeService genericAttributeService,
            IJsonLdModelFactory jsonLdModelFactory,
            ILocalizationService localizationService,
            IManufacturerService manufacturerService,
            IPermissionService permissionService,
            IPictureService pictureService,
            IPriceCalculationService priceCalculationService,
            IPriceFormatter priceFormatter,
            IProductAttributeParser productAttributeParser,
            IProductAttributeService productAttributeService,
            IProductService productService,
            IProductTagService productTagService,
            IProductTemplateService productTemplateService,
            IReviewTypeService reviewTypeService,
            IShoppingCartService shoppingCartService,
            ISpecificationAttributeService specificationAttributeService,
            IStaticCacheManager staticCacheManager,
            IStoreContext storeContext,
            IStoreService storeService,
            IShoppingCartModelFactory shoppingCartModelFactory,
            ITaxService taxService,
            IUrlRecordService urlRecordService,
            IVendorService vendorService,
            IVideoService videoService,
            IWebHelper webHelper,
            IWorkContext workContext,
            MediaSettings mediaSettings,
            OrderSettings orderSettings,
            SeoSettings seoSettings,
            ShippingSettings shippingSettings,
            VendorSettings vendorSettings,
            IERPPriceService erpPriceService,
            ILogger logger)
            : base(captchaSettings, catalogSettings, customerSettings, categoryService, currencyService,
                  customerService, dateRangeService, dateTimeHelper, downloadService, genericAttributeService,
                  jsonLdModelFactory, localizationService, manufacturerService, permissionService, pictureService,
                  priceCalculationService, priceFormatter, productAttributeParser, productAttributeService, productService,
                  productTagService, productTemplateService, reviewTypeService, shoppingCartService,
                  specificationAttributeService, staticCacheManager, storeContext, storeService,
                  shoppingCartModelFactory, taxService, urlRecordService, vendorService, videoService,
                  webHelper, workContext, mediaSettings, orderSettings, seoSettings,
                  shippingSettings, vendorSettings)
        {
            _erpPriceService = erpPriceService;
            _logger = logger;

        }

        public override async Task<ProductDetailsModel> PrepareProductDetailsModelAsync(Product product, ShoppingCartItem updatecartitem = null, bool isAssociatedProduct = false)
        {
            // Call base factory to prepare the default model
            var model = await base.PrepareProductDetailsModelAsync(product, updatecartitem, isAssociatedProduct);

            try
            {
                _logger.Information($"Preparing product details model for ProductId = {product.Id}");

                // Retrieve current customer
                var customer = await _workContext.GetCurrentCustomerAsync();

                // Retrieve updated price from ERP for the product
                var priceResponse = await _erpPriceService.GetProductPricesAsync(customer.Id, new List<int> { product.Id });

                if (priceResponse.TryGetValue(product.Id, out var price))
                {
                    _logger.Information($"Updating product price for ProductId = {product.Id} to {price}");

                    // Update the product price in the model to reflect ERP pricing
                    model.ProductPrice.Price = price.ToString("C");
                }
                else
                {
                    _logger.Warning($"No price found for ProductId = {product.Id} from ERP response.");
                }
            }
            catch (Exception ex)
            {
                _logger.Error("Error occurred while preparing product details model.", ex);
            }

            return model;
        }
    }
}
