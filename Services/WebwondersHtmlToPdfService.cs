﻿using System;
using System.IO;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using RazorLight;
using Umbraco.Cms.Core.Web;
using Umbraco.Extensions;
using Webwonders.Extensions;
using WkHtmlToPdfDotNet;
using WkHtmlToPdfDotNet.Contracts;

namespace Webwonders.PdfGenerator;

public interface IHtmlToPdfService
{
    (bool success, MemoryStream? stream) GetPdfMemoryStream(string pdfTheme, object viewModel, HtmlToPdfSettings? settings = null);

}



public class HtmlToPdfService(IWebHostEnvironment webHostEnvironment,
						      IConverter converter,
						      IUmbracoContextAccessor umbracoContextAccessor,
						      IWWCacheService cacheService) : IHtmlToPdfService
{


    private const string BodyHtml = "Body.cshtml";
    private const string HeaderHtml = "Header.cshtml";
    private const string FooterHtml = "Footer.cshtml";




	/// <summary>
	/// Get the pdf as memorystream
	/// This expects a pdfTheme which is also the folder in the Views/Pdf where the views need to be located. For instance: Views/Pdf/Invoice
	/// It expects a viewmodel which is passed to the view and when asked for to the header and footer.
	/// In the pdfTheme folder three views can be located: Body.cshtml, Header.cshtml and Footer.cshtml
	/// Body.cshtml is the main view and required. A custom header and footer are optional.
	/// The given viewmodel is passed to all three views.
	/// There are booleans to indicate no header and/or footer should be used.
	/// An optional stylesheet can be passed to the pdf, it should be called pdfStyle.css 
	/// and located in a subfolder with your pdfTheme name in the wwwroot/pdfThemes folder.
	/// </summary>
	/// <param name="pdfTheme">Type of the pdf (name of subfolder of views/pdf that will be searched)</param>
	/// <param name="viewName">Name of the view (in view/pdf/{type})</param>
	/// <param name="viewModel">Model to pass to view and if necessary to header and/or footer</param>
	/// <param name="settings">Settings of pdf</param>
	/// <returns>bool success </returns>
	/// <exception cref="ArgumentNullException"></exception>
	public (bool success, MemoryStream? stream) GetPdfMemoryStream(string pdfTheme, object viewModel, HtmlToPdfSettings? settings = null)
    {

        if (String.IsNullOrWhiteSpace(pdfTheme))
        {
            throw new ArgumentNullException(nameof(pdfTheme));
        }
        if (viewModel == null)
        {
            throw new ArgumentNullException(nameof(viewModel));
        }


        // when settings is null: use all default values
        settings ??= new HtmlToPdfSettings();

        // if no stylesheet is given: check for existance of default stylesheet for pdfTheme and use that
        if (String.IsNullOrWhiteSpace(settings.UserStyleSheet))
        {
            string styleSheet = Path.Combine(webHostEnvironment.WebRootPath, "pdfThemes", $"{pdfTheme}", "pdfStyle.css");
            if (File.Exists(styleSheet))
            {
                settings.UserStyleSheet = styleSheet;
            }
        }

        // check for existance of views
        string pdfPath = Path.Combine(webHostEnvironment.ContentRootPath, $"Views\\pdf\\{pdfTheme}\\");

        if (!File.Exists($"{pdfPath}{BodyHtml}"))
        {
            throw new FileNotFoundException($"View not found: {pdfPath}{BodyHtml}");
        }
        if (settings.UseHeaderHtml && String.IsNullOrWhiteSpace(settings.HeaderHtmlUrl) && !File.Exists($"{pdfPath}{HeaderHtml}"))
        {
            throw new FileNotFoundException($"View not found: {pdfPath}{HeaderHtml}");
        }
        if (settings.UseFooterHtml && String.IsNullOrWhiteSpace(settings.FooterHtmlUrl) && !File.Exists($"{pdfPath}{FooterHtml}"))
        {
            throw new FileNotFoundException($"View not found: {pdfPath}{FooterHtml}");
        }

        if (umbracoContextAccessor.TryGetUmbracoContext(out IUmbracoContext? context) && context != null)
        {
            var domain = context.CleanedUmbracoUrl.GetLeftPart(UriPartial.Authority);

            if (settings.UseHeaderHtml && String.IsNullOrWhiteSpace(settings.HeaderHtmlUrl))
            {
                settings.HeaderHtmlUrl = $"{domain.EnsureEndsWith('/')}umbraco/WebwondersPdfGenerator/HtmlToPdfSurface/GetHeaderHtml?type={pdfTheme}";
            }

            if (settings.UseFooterHtml && String.IsNullOrWhiteSpace(settings.FooterHtmlUrl))
            {
                settings.FooterHtmlUrl = $"{domain.EnsureEndsWith('/')}umbraco/WebwondersPdfGenerator/HtmlToPdfSurface/GetFooterHtml?type={pdfTheme}";
            }
        }



        var engine = new RazorLightEngineBuilder()
                         .UseFileSystemProject(pdfPath)
                         .UseMemoryCachingProvider()
                         .Build();

        string htmlString = engine.CompileRenderAsync(BodyHtml, viewModel).Result;

        if (!String.IsNullOrWhiteSpace(htmlString))
        {
            string cacheKey = string.Empty;

            // If header and/or footer: write to cache with a generated Guid key and add to url
            if (settings.UseHeaderHtml || settings.UseFooterHtml)
            {
                cacheKey = Guid.NewGuid().ToString();
                cacheService.InsertCacheItem<object>(cacheKey, () => { return viewModel; }, new TimeSpan(0, 10, 0)); // cache for 10 minutes, will be cleared after pdf is created

                if (settings.UseHeaderHtml && !String.IsNullOrWhiteSpace(settings.HeaderHtmlUrl))
                {
                    settings.HeaderHtmlUrl = $"{settings.HeaderHtmlUrl}&key={cacheKey}";
                }
                if (settings.UseFooterHtml && !String.IsNullOrWhiteSpace(settings.FooterHtmlUrl))
                {
                    settings.FooterHtmlUrl = $"{settings.FooterHtmlUrl}&key={cacheKey}";
                }
            }


            IDocument document = CreatePdfDocument(htmlString, settings);

            byte[] pdf = converter.Convert(document);

            // If cached: clear the cache
            if (settings.UseHeaderHtml || settings.UseFooterHtml)
            {
                cacheService.ClearCacheItem(cacheKey);
            }

            if (pdf != null)
            {
                return (true, new MemoryStream(pdf));
            }

        }

        return (false, null);

    }




    /// <summary>
    /// Create the IDocument by copying the settings and the html string
    /// </summary>
    /// <param name="html"></param>
    /// <param name="settings"></param>
    /// <returns></returns>
    private static IDocument CreatePdfDocument(string html, HtmlToPdfSettings settings)
    {
        return new HtmlToPdfDocument
        {
            GlobalSettings = {
                Orientation = (WkHtmlToPdfDotNet.Orientation)settings.Orientation,
                ColorMode = (WkHtmlToPdfDotNet.ColorMode)settings.ColorMode,
                UseCompression = settings.UseCompression,
                DPI = settings.DPI,
                PageOffset = settings.PageOffset,
                Copies = settings.Copies,
                Collate = settings.Collate,
                Outline = settings.Outline,
                OutlineDepth = settings.OutlineDepth,
                DumpOutline = settings.DumpOutline,
                Out = settings.Out,
                DocumentTitle = settings.DocumentTitle,
                ImageDPI = settings.ImageDPI,
                ImageQuality = settings.ImageQuality,
                CookieJar = settings.CookieJar,
                PaperSize = (WkHtmlToPdfDotNet.PaperKind)settings.PaperSize,
                Margins = new WkHtmlToPdfDotNet.MarginSettings(settings.Margins.Top, settings.Margins.Right, settings.Margins.Bottom, settings.Margins.Left),
            },
            Objects = {
                new ObjectSettings{
                    Page = settings.Page,
                    UseExternalLinks = settings.UseExternalLinks,
                    UseLocalLinks = settings.UseLocalLinks,
                    ProduceForms = settings.ProduceForms,
                    IncludeInOutline = settings.IncludeInOutline,
                    PagesCount = settings.PagesCount,
                    HtmlContent = html,
                    Encoding = settings.Encoding,
                    WebSettings =
                    {
                        Background = settings.Background,
                        LoadImages = settings.LoadImages,
                        EnableJavascript = settings.EnableJavascript,
                        EnableIntelligentShrinking = settings.EnableIntelligentShrinking,
                        MinimumFontSize = settings.MinimumFontSize,
                        PrintMediaType = settings.PrintMediaType,
                        DefaultEncoding = settings.DefaultEncoding,
                        UserStyleSheet = settings.UserStyleSheet,
                        enablePlugins = settings.EnablePlugins,
                    },
                    HeaderSettings = {
                        FontSize = settings.HeaderFontSize,
                        FontName = settings.HeaderFontName,
                        Left = settings.HeaderLeft,
                        Center = settings.HeaderCenter,
                        Right = settings.HeaderRight,
                        Line = settings.HeaderLine,
                        Spacing = settings.HeaderSpacing,
                        HtmlUrl = settings.HeaderHtmlUrl
                    },
                    FooterSettings = {
                        FontSize = settings.FooterFontSize,
                        FontName = settings.FooterFontName,
                        Left = settings.FooterLeft,
                        Center = settings.FooterCenter,
                        Right = settings.FooterRight,
                        Line = settings.FooterLine,
                        Spacing = settings.FooterSpacing,
                        HtmlUrl = settings.FooterHtmlUrl
                    },
                    LoadSettings = {
                        Username = settings.Username,
                        Password = settings.Password,
                        JSDelay = settings.JSDelay,
                        ZoomFactor = settings.ZoomFactor,
                        BlockLocalFileAccess = settings.BlockLocalFileAccess,
                        StopSlowScript = settings.StopSlowScript,
                        DebugJavascript = settings.DebugJavascript,
                        LoadErrorHandling = (WkHtmlToPdfDotNet.ContentErrorHandling)settings.LoadErrorHandling,
                        Proxy = settings.Proxy,
                        CustomHeaders = settings.CustomHeaders,
                        RepeatCustomHeaders = settings.RepeatCustomHeaders,
                        Cookies = settings.Cookies,
                        Post = settings.Post,
                    }
                }
            }
        };
    }
}
