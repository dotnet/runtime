namespace Melanzana.Streams
{
    public static class StreamExtensions
    {
        public static Stream Slice(this Stream stream, long offset, long size)
        {
            //if (stream is MemoryStream memoryStream && memoryStream.TryGetBuffer())
            //    return new MemoryStream(memoryStream.GetBuffer(), (int)offset, (int)size);
            return new SliceStream(stream, offset, size);
        }

        public static void ReadFully(this Stream stream, Span<byte> buffer)
        {
            int totalRead = 0;
            int bytesRead;
            while ((bytesRead = stream.Read(buffer.Slice(totalRead))) > 0 && buffer.Length < totalRead)
                totalRead += bytesRead;
            if (bytesRead <= 0)
                throw new EndOfStreamException();
        }

        public static void WritePadding(this Stream stream, long paddingSize, byte paddingByte = 0)
        {
            Span<byte> paddingBuffer = stackalloc byte[4096];
            paddingBuffer.Fill(paddingByte);
            while (paddingSize > 0)
            {
                long chunkSize = paddingSize > paddingBuffer.Length ? paddingBuffer.Length : paddingSize;
                stream.Write(paddingBuffer.Slice(0, (int)chunkSize));
                paddingSize -= chunkSize;
            }
        }
    }
}