using Melanzana.Streams;

namespace Melanzana.MachO
{
    /// <summary>
    /// Linker data representation.
    /// 
    /// This holds data for relocations, symbol tables, string tables and various
    /// other linking related information. It's either part of the __LINKEDIT
    /// segment for fully linked files, or appended at the end of the object file
    /// for unlinked Mach-O files.
    /// </summary>
    public class MachLinkEditData
    {
        private Stream dataStream;

        internal MachLinkEditData()
        {
            this.dataStream = Stream.Null;
            this.FileOffset = 0;
        }

        internal MachLinkEditData(Stream objectStream, uint offset, uint size)
        {
            this.dataStream = size == 0 ? Stream.Null : objectStream.Slice(offset, size);
            this.FileOffset = offset;
        }

        public uint FileOffset { get; set; }

        public ulong Size
        {
            get => (ulong)dataStream.Length;
            set
            {
                if (dataStream != null)
                {
                    if (!HasContentChanged)
                    {
                        HasContentChanged = true;
                        dataStream = new UnclosableMemoryStream();
                    }
                    dataStream.SetLength((long)value);
                }
            }
        }

        internal bool HasContentChanged { get; set; }

        public Stream GetReadStream()
        {
            if (dataStream.Length == 0)
            {
                return Stream.Null;
            }

            return dataStream.Slice(0, (long)this.Size);
        }

        public Stream GetWriteStream()
        {
            HasContentChanged = true;
            dataStream = new UnclosableMemoryStream();
            return dataStream;
        }
    }
}
