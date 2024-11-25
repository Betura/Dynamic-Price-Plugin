using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nop.Services.Common;
using Nop.Services.Plugins;

using Nop.Core.Configuration;
using Nop.Plugin.Aptean.DynamicPricing.Models;
using Nop.Services.Configuration;
using Nop.Core;
using Nop.Services.Cms;
using Microsoft.AspNetCore.Routing;

namespace Nop.Plugin.Aptean.DynamicPricing;

/// <summary>
/// Plugin to integrate ERP pricing for dynamic, real-time product and cart price retrieval.
/// </summary>
public class DynamicPricingPlugin : BasePlugin, IMiscPlugin
{
    private readonly ISettingService _settingService;
    protected readonly IWebHelper _webHelper;

    public DynamicPricingPlugin(ISettingService settingService,
        IWebHelper webHelper)
    {
        _settingService = settingService;
        _webHelper = webHelper;
    }

    /// <summary>
    /// Installs the plugin by adding default settings.
    /// </summary>
    public override async Task InstallAsync()
    {
        await _settingService.SaveSettingAsync(new DynamicPricingSettings
        {
            ApiUrl = "https://api.example.com/erp/api/v1",
            ClientOid = "default-client-id",
            EnableDynamicPricing = true // Enable dynamic pricing by default
        });

        await base.InstallAsync();
    }

    /// <summary>
    /// Uninstalls the plugin by deleting settings.
    /// </summary>
    public override async Task UninstallAsync()
    {
        await _settingService.DeleteSettingAsync<DynamicPricingSettings>();
        await base.UninstallAsync();
    }

    /// <summary>
    /// Gets a configuration page URL
    /// </summary>
    public override string GetConfigurationPageUrl()
    {
        return $"{_webHelper.GetStoreLocation()}Admin/DynamicPricing/Configure";
    }

}