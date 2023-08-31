# Webwonders.PdfGenerator


## About

Webwonders.PdfGenerator is a wrapper around Haukcode.WkHtmlToPdfDotNet to generate PDF's from HTML-files to be used within Umbraco.


## How to use




### example

```csharp

 var pdfViewModel = new PdfViewModel
 {
     OrderId = orderId,
     InvoiceNumber = invoiceNumber,
     InvoiceDate = invoiceDate
 };

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

 (bool success, MemoryStream? stream) = _wwHtmlToPdfService.GetPdfMemoryStream("Invoice", pdfViewModel, pdfSettings);

 if (success && stream != null)
 {
     // Do something with the stream
 }

```
