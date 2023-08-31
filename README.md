# Webwonders.PdfGenerator


## About
Generate PDF's from HTML-files in Umbraco.
Webwonders.PdfGenerator is a wrapper around Haukcode.WkHtmlToPdfDotNet.


## How to use
Pdf's are generated from HTML-files in the folder /Views/Pdf/{PdfType} where PdfType is the name of the type of PDF you want to generate.
In this folder three views can be located: Header.cshtml, Body.cshtml and Footer.cshtml.
The Body view is the main view and is required, the Header and Footer views are optional.
All views are rendered with the viewmodel you pass to PdfGenerator.
The result is a named tuple, indicating success and the MemoryStream containing the PDF.



### example

```csharp

// create viewmodel
var pdfViewModel = new PdfViewModel
{
    OrderId = orderId,
    InvoiceNumber = invoiceNumber,
    InvoiceDate = invoiceDate
};

// define settings
var pdfSettings = new WWHtmlToPdfSettings()
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
(bool success, MemoryStream? stream) = _wwHtmlToPdfService.GetPdfMemoryStream("Invoice", pdfViewModel, pdfSettings);

if (success && stream != null)
{
    // Do something with the stream
}

```
