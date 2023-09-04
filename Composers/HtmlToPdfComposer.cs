using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using WkHtmlToPdfDotNet;
using WkHtmlToPdfDotNet.Contracts;

namespace Webwonders.PdfGenerator;


// Adds htmltopdf service to the container

public class HtmlToPdfComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        builder.Services
            .AddSingleton(typeof(IConverter), new SynchronizedConverter(new PdfTools()))
            .AddScoped<IWebwondersHtmlToPdfService, WebwondersHtmlToPdfService>();
    }
}
