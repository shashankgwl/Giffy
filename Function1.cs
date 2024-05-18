namespace GifConvert
{
    using DocumentFormat.OpenXml;
    using DocumentFormat.OpenXml.Packaging;
    using DocumentFormat.OpenXml.Wordprocessing;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Azure.WebJobs;
    using Microsoft.Azure.WebJobs.Extensions.Http;
    using Microsoft.Extensions.Logging;
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Threading.Tasks;
    public static class Function1
    {

        [FunctionName("CreateWordDocument")]
        public static IActionResult CreateWordDocument(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req)
        {
            var stream = new MemoryStream();
            using (var wordDocument = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document, true))
            {
                var mainPart = wordDocument.AddMainDocumentPart();
                mainPart.Document = new Document();
                var body = mainPart.Document.AppendChild(new Body());
                var para = body.AppendChild(new Paragraph());
                var run = para.AppendChild(new Run() { RunProperties = new RunProperties { Bold = new Bold() } });

                run.AppendChild(new Text("1st Layer Title"));
                para = body.AppendChild(new Paragraph());
                run = para.AppendChild(new Run());
                run.AppendChild(new Text("1st Layer Detials"));

                para = body.AppendChild(new Paragraph());
                run = para.AppendChild(new Run() { RunProperties = new RunProperties { Bold = new Bold() } });
                run.AppendChild(new TabChar());
                run = para.AppendChild(new Run() { RunProperties = new RunProperties { Bold = new Bold(), Underline = new Underline() { Val = UnderlineValues.Single } } });
                run.AppendChild(new Text("Second Layer title"));

                para = body.AppendChild(new Paragraph());
                run = para.AppendChild(new Run());
                run.AppendChild(new TabChar());
                run.AppendChild(new Text("Second Layer details"));


                para = body.AppendChild(new Paragraph());
                run = para.AppendChild(new Run() { RunProperties = new RunProperties { Bold = new Bold(), Italic = new Italic() { Val = new OnOffValue(true) } } });
                run.AppendChild(new TabChar());
                run.AppendChild(new TabChar());
                ////run = para.AppendChild(new Run() { RunProperties = new RunProperties { Bold = new Bold(), Italic = new Italic() { Val = new OnOffValue(true) } } });
                run.AppendChild(new Text("Third layer title"));

                para = body.AppendChild(new Paragraph());
                run = para.AppendChild(new Run());
                run.AppendChild(new TabChar());
                run.AppendChild(new TabChar());
                run.AppendChild(new Text("Third Layer's details"));
            }
            stream.Position = 0;
            return new FileStreamResult(stream, "application/vnd.openxmlformats-officedocument.wordprocessingml.document")
            {
                FileDownloadName = "Document.docx"
            };
        }

        [FunctionName("FxBlobReceiver")]
        public static async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequest req,
        ILogger log)
        {
            string requestBody = string.Empty;
            using (var streamReader = new StreamReader(req.Body))
            {
                requestBody = await streamReader.ReadToEndAsync();
            }

            ////var context = Guid.NewGuid().ToString();

            var blobData = Convert.FromBase64String(GetDataAfterBase64(requestBody));
            log.LogInformation($"Total length of the incoming file is {blobData.Length} bytes.");
            var inputPath = "C:\\home\\site\\wwwroot\\received";
            var outputPath = "C:\\home\\site\\wwwroot\\GIF";
            var exePath = "C:\\home\\site\\wwwroot\\ffmpeg\\ffmpeg.exe";

            var webmPath = $"{inputPath}\\{Guid.NewGuid()}.webm";
            var gifPath = $"{outputPath}\\{Guid.NewGuid()}.gif";
            File.WriteAllBytes(webmPath, blobData);

            using (var ffmpeg = new Process())
            {
                ffmpeg.StartInfo.UseShellExecute = false;
                ffmpeg.StartInfo.RedirectStandardOutput = true;
                string eOut = null;
                ffmpeg.StartInfo.RedirectStandardError = true;
                ffmpeg.ErrorDataReceived += new DataReceivedEventHandler((sender, e) =>
                { eOut += e.Data; });
                ffmpeg.StartInfo.FileName = exePath;
                ffmpeg.StartInfo.Arguments = $"-i  {webmPath} -filter_complex \"fps=25, scale=800:-1\" -pix_fmt rgb8 {gifPath}";
                ffmpeg.Start();

                // To avoid deadlocks, use an asynchronous read operation on at least one of the streams.  
                ffmpeg.BeginErrorReadLine();
                string output = ffmpeg.StandardOutput.ReadToEnd();
                ffmpeg.WaitForExit();

                log.LogInformation($"The Exe Result are:\n'{output}'");
                log.LogInformation($"C# function executed at: {DateTime.Now}");
            }

            var gifBase64 = File.ReadAllBytes(gifPath);
            await Task.Delay(1000).ContinueWith(task =>
            {

                if (File.Exists(gifPath))
                {
                    log.LogInformation($"Deleting {gifPath} file");
                    File.Delete(gifPath);
                }

                if (File.Exists(webmPath))
                {
                    log.LogInformation($"Deleting {webmPath} file");
                    File.Delete(webmPath);
                }
            });
            return new FileContentResult(gifBase64, "application/octet-stream");
        }


        public static string GetDataAfterBase64(string data)
        {
            string keyword = "base64,";
            int index = data.IndexOf(keyword);
            if (index < 0)
            {
                return "Keyword 'base64,' not found.";
            }
            else
            {
                return data.Substring(index + keyword.Length);
            }
        }
    }
}
