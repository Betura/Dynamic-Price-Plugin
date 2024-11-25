using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nop.Web.Framework.Models;

namespace Nop.Plugin.Aptean.DynamicPricing.Models;

/// <summary>
/// Represents the price of a single product.
/// </summary>
public record ProductPrice : BaseNopModel
{
    #region Properties
    public int ProductId { get; set; }

    public decimal Price { get; set; }

    #endregion
}