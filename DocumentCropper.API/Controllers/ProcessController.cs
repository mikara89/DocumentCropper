using DocumentCropper.Lib;
using Microsoft.AspNetCore.Mvc;

namespace DocumentCropper.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProcessController : ControllerBase
    {

        private readonly IImageProcessor _processor;

        public ProcessController(IImageProcessor processor)
        {
            _processor = processor;
        }


        [HttpPost, DisableRequestSizeLimit]
        public async Task<IActionResult> Upload([FromForm] IFormFile file)
        {
            try
            {
                using (var ms = file.OpenReadStream())
                {
                    var watch = new System.Diagnostics.Stopwatch();
                    watch.Start();
                    byte[] transformedImgBytes = await _processor.ProcessAsync(ms, TransformImageProcessor.ProcessResultType.PDF);
                    watch.Stop();

                    if (transformedImgBytes is null)
                    {
                        throw new Exception("Failed to transform file.");
                    }
                    var name = file.FileName.Replace("." + file.FileName.Split(".").Last(), ".pdf");

                    Response.Headers.Add("X-process-time", watch.ElapsedMilliseconds.ToString() + "ms");
                    Response.Headers.Add("X-input-file-size", BytesToMB(file.Length).ToString() + "MB");
                    Response.Headers.Add("X-output-file-size", BytesToMB(transformedImgBytes.Length).ToString() + "MB");

                    return File(new MemoryStream(transformedImgBytes), "application/octet-stream", name);
                }
            }
            catch (Exception ex)
            {
                return Problem(ex.Message);
            }

            static double BytesToMB(long number)
            {
                return Math.Round(number / 1024.0 / 1024.0, 2);
            }
        }
    }
}
