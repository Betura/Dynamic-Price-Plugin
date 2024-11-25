using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Nop.Core.Domain.Orders;
using Nop.Core.Events;
using Nop.Core.Infrastructure;
using Nop.Plugin.Aptean.DynamicPricing.Events;
using Nop.Plugin.Aptean.DynamicPricing.Overrides;
using Nop.Plugin.Aptean.DynamicPricing.Services;
using Nop.Services.Catalog;
using Nop.Services.Events;
using Nop.Services.Orders;
using Nop.Web.Factories;

namespace Nop.Plugin.Aptean.DynamicPricing.Infrastructure;
public class NopStartup : INopStartup
{
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        // Register custom services
        services.AddScoped<IERPPriceService, ERPPriceService>();
        services.AddHttpClient<ERPPriceService>();

        // Override default ProductModelFactory
        services.AddSingleton<IProductModelFactory, CustomProductModelFactory>();

        // Override default PriceCalculationService
        services.AddScoped<IPriceCalculationService, CustomPriceCalculationService>();
    }

    public void Configure(IApplicationBuilder application)
    {
        // You can add middleware here if needed for your plugin
    }

    public int Order => 5000;
}
