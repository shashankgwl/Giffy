namespace GifConvert
{
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

            var context = Guid.NewGuid().ToString();

            byte[] blobData = Convert.FromBase64String(GetDataAfterBase64(requestBody));
            Console.WriteLine(blobData.Length);
            var inputPath = "C:\\home\\site\\wwwroot\\received";
            var outputPath = "C:\\home\\site\\wwwroot\\GIF";
            var exePath = "C:\\home\\site\\wwwroot\\ffmpeg\\ffmpeg.exe";

            var webmPath = $"{inputPath}\\{context}.webm";
            var gifPath = $"{outputPath}\\{context}";
            File.WriteAllBytes(webmPath, blobData);

            using (var p = new Process())
            {
                p.StartInfo.UseShellExecute = false;
                p.StartInfo.RedirectStandardOutput = true;
                string eOut = null;
                p.StartInfo.RedirectStandardError = true;
                p.ErrorDataReceived += new DataReceivedEventHandler((sender, e) =>
                { eOut += e.Data; });
                p.StartInfo.FileName = exePath;
                p.StartInfo.Arguments = $"-i  {webmPath} -filter_complex \"fps=25, scale=800:-1\" -pix_fmt rgb8 {gifPath}.gif";
                p.Start();

                // To avoid deadlocks, use an asynchronous read operation on at least one of the streams.  
                p.BeginErrorReadLine();
                string output = p.StandardOutput.ReadToEnd();
                p.WaitForExit();

                log.LogInformation($"The Exe Result are:\n'{output}'");
                log.LogInformation($"\nError stream: {(string)null}");
                log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");
            }


            var gifBase64 = File.ReadAllBytes(gifPath + ".gif");
            await Task.Delay(2000).ContinueWith(task =>
            {
                File.Delete(gifPath + ".gif");
                File.Delete(webmPath);
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
