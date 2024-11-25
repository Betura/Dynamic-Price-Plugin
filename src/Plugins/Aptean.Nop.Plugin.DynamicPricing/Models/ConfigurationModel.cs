using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nop.Core.Configuration;

namespace Nop.Plugin.Aptean.DynamicPricing.Models;

/// <summary>
/// Represents settings for the ERP API integration.
/// </summary>
public class ConfigurationModel
{
    public string ApiUrl { get; set; }
    public string ClientOid { get; set; }
}