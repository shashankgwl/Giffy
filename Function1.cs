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
