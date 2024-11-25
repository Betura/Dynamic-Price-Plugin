using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Collections.Generic;

namespace Nop.Plugin.Aptean.DynamicPricing.Models;

/// <summary>
/// Represents the response from the ERP API for cart pricing.
/// </summary>
public class CartPriceResponse
{
    /// <summary>
    /// Dictionary containing product IDs and their respective prices.
    /// </summary>
    public Dictionary<int, decimal>? ProductPrices { get; set; }

    /// <summary>
    /// The total price for the cart.
    /// </summary>
    public decimal TotalPrice { get; set; }
}
