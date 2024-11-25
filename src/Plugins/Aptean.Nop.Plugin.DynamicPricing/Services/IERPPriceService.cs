using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Nop.Plugin.Aptean.DynamicPricing.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Nop.Plugin.Aptean.DynamicPricing.Services;

/// <summary>
/// Defines methods for interacting with the ERP API for product and cart pricing.
/// </summary>
public interface IERPPriceService
{
    Task<Dictionary<int, decimal>> GetProductPricesAsync(int customerId, List<int> productIds);
    Task<Dictionary<int, decimal>> GetCartPricesAsync(int customerId, Dictionary<int, int> cartItems);
    bool IsDynamicPricingEnabled();
}
