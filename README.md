# Webwonders.PdfGenerator


## About
Generate PDF's from HTML-files in Umbraco.
Webwonders.PdfGenerator is a wrapper around WkHtmlToPdfDotNet (https://github.com/HakanL/WkHtmlToPdf-DotNet)


## How to use
The package defines a service: IWebwondersHtmlToPdfService, which can be injected in your code.
This service has one method: GetPdfMemoryStream(string pdfType, object viewModel, HtmlToPdfSettings? settings = null).

PdfType is the name of the type of PDF you want to generate, this should also be the name of the folder in /Views/Pdf/ where you locate the views for the PDF.
In this folder three views can be located: Header.cshtml, Body.cshtml and Footer.cshtml.
The Body view is the main view and is required, the Header and Footer views are optional.

ViewModel is the viewmodel you want to pass to the views: all views are rendered with the viewmodel you pass.

Settings are taken from WkHtmlToPdfDotNet, but are all combined in one class: HtmlToPdfSettings, they all are optional and have default values.

The result of GetpdfMemoryStream is a named tuple: (bool success, MemoryStream? stream), indicating success and the MemoryStream containing the PDF.


### Example
```csharp

// create viewmodel
var pdfViewModel = new PdfViewModel
{
    OrderId = orderId,
    InvoiceNumber = invoiceNumber,
    InvoiceDate = invoiceDate
};

// define settings, optional: all settings have defaults
var pdfSettings = new HtmlToPdfSettings()
{
    Margins = new MarginSettings { Top = 50, Bottom = 65 },
    DocumentTitle = fileName,
    PagesCount = true,
    DefaultEncoding = "utf-8",
    UserStyleSheet = Path.Combine(webRootPath, "pdf", "pdfStyle.css"),
    HeaderSpacing = 20,
    FooterSpacing = 30,
};

// get memory stream of PDF
(bool success, MemoryStream? stream) = htmlToPdfService.GetPdfMemoryStream("Invoice", pdfViewModel, pdfSettings);

if (success && stream != null)
{
    // Do something with the stream
}

```


## Note
Do not use Webwonders.PdfGenerator with any untrusted HTML – be sure to sanitize any user-supplied HTML/JS, otherwise it can lead to complete takeover of the server it is running on.
