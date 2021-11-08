using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FileUploadAsChunks.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace FileUploadAsChunks.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [RequestFormLimits(ValueLengthLimit = int.MaxValue, MultipartBodyLengthLimit = long.MaxValue)]
        [DisableRequestSizeLimit]
        [HttpPost]
        public async Task<IActionResult> UploadFileAsync(IFormFile LargeFile, CancellationToken token)
        {
            try
            {
                _logger.LogInformation($"{LargeFile.FileName} : {LargeFile.Length / (1024 * 1024)} MB");

                var fileStream = LargeFile.OpenReadStream();
                // Get the object used to communicate with the server.
                FtpWebRequest request = (FtpWebRequest)WebRequest.Create($"ftp://127.0.0.1/{LargeFile.FileName}");
                request.Method = WebRequestMethods.Ftp.UploadFile;

                // This example assumes the FTP site uses anonymous logon.
                request.Credentials = new NetworkCredential("khair", "123");

                // Copy the contents of the file to the request stream.
                byte[] fileContents;
                using (StreamReader sourceStream = new StreamReader(fileStream))
                {
                    fileContents = Encoding.UTF8.GetBytes(await sourceStream.ReadToEndAsync());
                    fileStream.Close();
                    sourceStream.Close();
                }
                request.ContentLength = fileContents.Length;

                using (Stream requestStream = await request.GetRequestStreamAsync())
                {
                    await requestStream.WriteAsync(fileContents);
                    requestStream.Close();
                }

                using (FtpWebResponse response = (FtpWebResponse)request.GetResponse())
                {
                    Console.WriteLine($"Upload File Complete, status {response.StatusDescription}");
                    response.Close();
                }
                return Ok();
            }
            catch (Exception e)
            {
                _logger.LogError(e.Message);
                return BadRequest();
            }
        }




        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
