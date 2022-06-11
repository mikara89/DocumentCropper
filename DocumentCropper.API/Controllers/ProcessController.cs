using DocumentCropper.Lib;
using Microsoft.AspNetCore.Mvc;

namespace DocumentCropper.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProcessController : ControllerBase
    {


        [HttpPost, DisableRequestSizeLimit]
        public async Task<IActionResult> Upload([FromForm] IFormFile file)
        {
            try
            {
                using (var ms = file.OpenReadStream())
                {
                    using (var transformer = new TransformImageProcess())
                    {
                        byte[] transformedImgMs = await transformer.ProcessAsync(ms, TransformImageProcess.ProcessResultType.PDF);

                        if (transformedImgMs is null)
                        {
                            throw new Exception("Failed to transform file.");
                        }
                        var name = file.FileName.Replace("." + file.FileName.Split(".").Last(), ".pdf");
                        return File(new MemoryStream(transformedImgMs), "application/octet-stream", name);

                    }
                }
            }
            catch (Exception ex)
            {
                return Problem(ex.Message);
            }
        }
    }
}
