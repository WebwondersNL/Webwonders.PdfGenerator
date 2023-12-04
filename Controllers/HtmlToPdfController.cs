using System;
using Microsoft.AspNetCore.Mvc;
using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Logging;
using Umbraco.Cms.Core.Routing;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Web;
using Umbraco.Cms.Infrastructure.Persistence;
using Umbraco.Cms.Web.Common.Attributes;
using Umbraco.Cms.Web.Website.Controllers;
using Webwonders.Extensions;

namespace Webwonders.PdfGenerator;

[PluginController("WebwondersPdfGenerator")]
public class HtmlToPdfSurfaceController(IWWCacheService wwCacheService,
								        IUmbracoContextAccessor umbracoContextAccessor,
                                        IUmbracoDatabaseFactory databaseFactory,
                                        ServiceContext services,
                                        AppCaches appCaches,
                                        IProfilingLogger profilingLogger,
                                        IPublishedUrlProvider publishedUrlProvider) : SurfaceController(umbracoContextAccessor, databaseFactory, services, appCaches, 
                                                                                                        profilingLogger, publishedUrlProvider)
{


	/// <summary>
	/// Returns view of HtmlToPdf Header
	/// </summary>
	/// <param name="orderNumber"></param>
	/// <param name="invoiceNumber"></param>
	/// <param name="invoiceDate"></param>
	/// <returns></returns>
	public ActionResult GetHeaderHtml(string type, Guid key)
    {
        // if key is a valid guid, get the cache with this key pass it to the view, 
        // otherwise just return the view
        if (key != Guid.Empty)
        {
            if (wwCacheService.GetCacheItem<object>(key.ToString()) is object viewModel)
            {
                // return the view with the viewmodel, converted to the strongly typed model
                return View($"~/Views/pdf/{type}/Header.cshtml", Convert.ChangeType(viewModel, viewModel.GetType()));
            }
        }
        return View($"~/Views/pdf/{type}/Header.cshtml");
    }



    /// <summary>
    /// Returns view of HtmlToPdf Footer
    /// </summary>
    /// <returns></returns>
    public ActionResult GetFooterHtml(string type, Guid key)
    {
        // if key is a valid guid, get the cache with this key pass it to the view, 
        // otherwise just return the view
        if (key != Guid.Empty)
        {
            if (wwCacheService.GetCacheItem<object>(key.ToString()) is object viewModel)
            {
                // return the view with the viewmodel, converted to the strongly typed model
                return View($"~/Views/pdf/{type}/Footer.cshtml", Convert.ChangeType(viewModel, viewModel.GetType()));
            }
        }
        return View($"~/Views/pdf/{type}/Footer.cshtml");

    }

}
