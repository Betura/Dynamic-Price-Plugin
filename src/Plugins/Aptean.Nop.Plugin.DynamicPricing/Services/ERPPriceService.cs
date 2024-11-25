using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Nop.Plugin.Aptean.DynamicPricing.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Nop.Services.Configuration;
using Nop.Services.Logging;
using System.Text.Json;

namespace Nop.Plugin.Aptean.DynamicPricing.Services;

/// <summary>
/// Service for making HTTP requests to the ERP API to fetch pricing information.
/// </summary>
public class ERPPriceService : IERPPriceService
{
    private readonly DynamicPricingSettings _settings;
    private readonly HttpClient _httpClient;
    private readonly ILogger _logger;

    public ERPPriceService(ISettingService settingService, HttpClient httpClient, ILogger logger)
    {
        _settings = settingService.LoadSettingAsync<DynamicPricingSettings>().Result;
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<Dictionary<int, decimal>> GetProductPricesAsync(int customerId, List<int> productIds)
    {
        var url = $"{_settings.ApiUrl}/products/{customerId}/prices?productIds={string.Join(",", productIds)}";
        try
        {
            _logger.Information($"Sending request to ERP API: {url}");

            var responseMessage = await _httpClient.GetAsync(url);
            responseMessage.EnsureSuccessStatusCode();
            var responseContent = await responseMessage.Content.ReadAsStringAsync();

            _logger.Information($"ERP API Raw Response: {responseContent}");

            var productPricesList = JsonSerializer.Deserialize<List<ProductPrice>>(responseContent);
            if (productPricesList != null)
            {
                var result = productPricesList.ToDictionary(p => p.ProductId, p => p.Price);
                _logger.Information($"ERP API Response: {string.Join(", ", result)}");
                return result;
            }

            _logger.Warning("ERP API returned null response.");
            return new Dictionary<int, decimal>();
        }
        catch (Exception ex)
        {
            _logger.Error($"Error calling ERP API: {url}", ex);
            return new Dictionary<int, decimal>();
        }
    }

    public async Task<Dictionary<int, decimal>> GetCartPricesAsync(int customerId, Dictionary<int, int> cartItems)
    {
        var productIds = cartItems.Keys.ToList();
        var url = $"{_settings.ApiUrl}/products/{customerId}/prices?productIds={string.Join(",", productIds)}";
        try
        {
            _logger.Information($"Sending request to ERP API for cart prices: {url}");

            var responseMessage = await _httpClient.GetAsync(url);
            responseMessage.EnsureSuccessStatusCode();
            var responseContent = await responseMessage.Content.ReadAsStringAsync();

            _logger.Information($"ERP API Raw Response: {responseContent}");

            var productPricesList = JsonSerializer.Deserialize<List<ProductPrice>>(responseContent);
            if (productPricesList != null)
            {
                var result = productPricesList.ToDictionary(p => p.ProductId, p => p.Price);
                _logger.Information($"ERP API Cart Prices Response: {string.Join(", ", result)}");
                return result;
            }

            _logger.Warning("ERP API returned null response for cart prices.");
            return new Dictionary<int, decimal>();
        }
        catch (Exception ex)
        {
            _logger.Error($"Error calling ERP API (Cart Prices): {url}", ex);
            return new Dictionary<int, decimal>();
        }
    }
}
