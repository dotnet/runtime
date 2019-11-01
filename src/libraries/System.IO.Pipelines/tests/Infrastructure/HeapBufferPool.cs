using System.Buffers;

namespace System.IO.Pipelines.Tests
{
    // This pool returns exact buffer sizes using heap memory
    public class HeapBufferPool : MemoryPool<byte>
    {
        public override int MaxBufferSize => int.MaxValue;

        public override IMemoryOwner<byte> Rent(int minBufferSize = -1)
        {
            return new Owner(minBufferSize == -1 ? 4096 : minBufferSize);
        }

        protected override void Dispose(bool disposing)
        {

        }

        private class Owner : IMemoryOwner<byte>
        {
            public Owner(int size)
            {
                Memory = new byte[size].AsMemory();
            }

            public Memory<byte> Memory { get; }

            public void Dispose()
            {

            }
        }
    }
}
