namespace Melanzana.Streams
{
    public class UnclosableMemoryStream : MemoryStream
    {
        protected override void Dispose(bool disposing)
        {
        }
    }
}