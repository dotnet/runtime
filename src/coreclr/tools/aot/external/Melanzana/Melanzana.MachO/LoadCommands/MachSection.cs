using System.ComponentModel;
using Melanzana.Streams;

namespace Melanzana.MachO
{
    public class MachSection
    {
        private readonly MachObjectFile objectFile;
        private Stream? dataStream;
        private MachLinkEditData? relocationData;
        private MachRelocationCollection? relocationCollection;
        private ulong size;

        public MachSection(MachObjectFile objectFile, string segmentName, string sectionName)
            : this(objectFile, segmentName, sectionName, null)
        {
        }

        public MachSection(MachObjectFile objectFile, string segmentName, string sectionName, Stream? stream)
            : this(objectFile, segmentName, sectionName, stream, null)
        {
        }

        internal MachSection(
            MachObjectFile objectFile,
            string segmentName,
            string sectionName,
            Stream? stream,
            MachLinkEditData? relocationData)
        {
            ArgumentNullException.ThrowIfNull(objectFile);
            ArgumentNullException.ThrowIfNull(segmentName);
            ArgumentNullException.ThrowIfNull(sectionName);

            this.objectFile = objectFile;
            this.SegmentName = segmentName;
            this.SectionName = sectionName;
            this.dataStream = stream;
            this.relocationData = relocationData;
        }

        /// <summary>
        /// Gets or sets the name of this section.
        /// </summary>
        public string SectionName { get; private init; }

        /// <summary>
        /// Gets or sets the name of the segment.
        /// </summary>
        /// <remarks>
        /// For fully linked executables or dynamic libraries this should always be the same as
        /// the name of the containing segment. However, intermediate object files
        /// (<see cref="MachFileType.Object"/>) use compact format where all sections are
        /// listed under single segment.
        /// </remarks>
        public string SegmentName { get; private init; }

        /// <summary>
        /// Gets or sets the virtual address of this section.
        /// </summary>
        public ulong VirtualAddress { get; set; }

        public ulong Size
        {
            get => dataStream != null ? (ulong)dataStream.Length : size;
            set
            {
                size = value;
                if (dataStream != null)
                {
                    if (!HasContentChanged)
                    {
                        var oldStream = dataStream;
                        HasContentChanged = true;
                        dataStream = new UnclosableMemoryStream();
                        oldStream?.CopyTo(dataStream);
                    }
                    dataStream.SetLength((long)size);
                }
            }
        }

        public uint FileOffset { get; set; }

        /// <summary>
        /// Gets or sets the alignment requirement of this section.
        /// </summary>
        public uint Log2Alignment { get; set; }

        /// <summary>
        /// Gets the file offset to relocation entries of this section.
        /// </summary>
        public uint RelocationOffset => RelocationData?.FileOffset ?? 0u;

        /// <summary>
        /// Gets or sets the number of relocation entries of this section.
        /// </summary>
        public uint NumberOfRelocationEntries => (uint)((RelocationData?.Size ?? 0u) / 8);

        internal uint Flags { get; set; }

        [EditorBrowsable(EditorBrowsableState.Advanced)]
        public uint Reserved1 { get; set; }

        [EditorBrowsable(EditorBrowsableState.Advanced)]
        public uint Reserved2 { get; set; }

        [EditorBrowsable(EditorBrowsableState.Advanced)]
        public uint Reserved3 { get; set; }

        public MachSectionAttributes Attributes
        {
            get => (MachSectionAttributes)(Flags & ~0xffu);
            set => Flags = (Flags & 0xffu) | (uint)value;
        }

        public MachSectionType Type
        {
            get => (MachSectionType)(Flags & 0xff);
            set => Flags = (Flags & ~0xffu) | (uint)value;
        }

        public bool IsInFile => Size > 0 && Type != MachSectionType.ZeroFill && Type != MachSectionType.GBZeroFill && Type != MachSectionType.ThreadLocalZeroFill;

        internal bool HasContentChanged { get; set; }

        public MachLinkEditData? RelocationData
        {
            get
            {
                relocationCollection?.FlushIfDirty();
                return relocationData;
            }
        }

        public IList<MachRelocation> Relocations
        {
            get
            {
                relocationData ??= new MachLinkEditData();
                relocationCollection ??= new MachRelocationCollection(objectFile, relocationData);
                return relocationCollection;
            }
        }

        public Stream GetReadStream()
        {
            if (Size == 0 || dataStream == null)
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