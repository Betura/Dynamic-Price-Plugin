using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nop.Core.Configuration;

namespace Nop.Plugin.Aptean.DynamicPricing;
public class DynamicPricingSettings : ISettings
{
    /// <summary>
    /// Gets or sets the ERP API base URL.
    /// </summary>
    public string ApiUrl { get; set; }

    /// <summary>
    /// Gets or sets the client OID for authentication.
    /// </summary>
    public string ClientOid { get; set; }

    /// <summary>
    /// Gets or sets whether dynamic pricing is enabled.
    /// </summary>
    public bool EnableDynamicPricing { get; set; } = true; // Default to enabled
}
