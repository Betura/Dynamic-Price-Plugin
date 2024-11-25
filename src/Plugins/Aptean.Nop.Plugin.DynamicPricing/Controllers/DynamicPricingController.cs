using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Plugin.Aptean.DynamicPricing;
using Nop.Plugin.Aptean.DynamicPricing.Models;
using Nop.Services.Configuration;
using Nop.Services.Localization;
using Nop.Services.Messages;
using Nop.Web.Framework;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Mvc.Filters;
using System.Threading.Tasks;

namespace Nop.Plugin.Aptean.DynamicPricing.Controllers
{
    [AutoValidateAntiforgeryToken]
    [AuthorizeAdmin]
    [Area(AreaNames.ADMIN)]
    public class DynamicPricingController : BasePluginController
    {
        protected readonly DynamicPricingSettings _dynamicPricingSettings;
        private readonly ISettingService _settingService;
        private readonly INotificationService _notificationService;
        private readonly ILocalizationService _localizationService;
        private readonly IWorkContext _workContext;

        public DynamicPricingController(DynamicPricingSettings dynamicPricingSettings,
            ISettingService settingService,
            INotificationService notificationService,
            ILocalizationService localizationService,
            IWorkContext workContext)
        {
            _dynamicPricingSettings = dynamicPricingSettings;
            _settingService = settingService;
            _notificationService = notificationService;
            _localizationService = localizationService;
            _workContext = workContext;
        }

        /// <summary>
        /// Displays the configuration page.
        /// </summary>
        [AuthorizeAdmin]
        [Area(AreaNames.ADMIN)]
        public async Task<IActionResult> Configure()
        {
            // Load settings from the database
            var model = new ConfigurationModel
            {
                ApiUrl = _dynamicPricingSettings.ApiUrl,
                ClientOid = _dynamicPricingSettings.ClientOid
            };
            return View("~/Plugins/Nop.Plugin.Aptean.DynamicPricing/Views/Configure.cshtml", model);
        }

        /// <summary>
        /// Handles the configuration form submission.
        /// </summary>
        [HttpPost]
        [AuthorizeAdmin]
        [Area(AreaNames.ADMIN)]
        public async Task<IActionResult> Configure(ConfigurationModel model)
        {
            if (!ModelState.IsValid)
            {
                return await Configure();
            }

            // Load and update settings
            _dynamicPricingSettings.ApiUrl = model.ApiUrl;
            _dynamicPricingSettings.ClientOid = model.ClientOid;

            await _settingService.SaveSettingAsync(_dynamicPricingSettings);

            // Display success notification
            var successMessage = await _localizationService.GetResourceAsync("Admin.Plugins.Saved");
            _notificationService.SuccessNotification(successMessage);

            return RedirectToAction("Configure");
        }
    }
}
