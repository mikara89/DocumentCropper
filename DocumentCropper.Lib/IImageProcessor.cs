namespace DocumentCropper.Lib
{
    public interface IImageProcessor
    {
        byte[] Process(Stream image, TransformImageProcessor.ProcessResultType resultType, string ext = ".png");
        Task<byte[]> ProcessAsync(Stream image, TransformImageProcessor.ProcessResultType resultType, string ext = ".png");
    }
}