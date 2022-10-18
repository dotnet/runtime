using Melanzana.Streams;

namespace Melanzana.MachO
{
    /// <summary>
    /// Defines a segment in the object file.
    /// <summary>
    /// <remarks>
    /// Segments represent how parts of the object file are mapped into virtual memory.
    ///
    /// Segment may contain zero or more sections. Some segments have special properties by
    /// convention:
    ///
    /// - The `__TEXT` segment has to start at file offset zero and it contains the file
    ///   header and load commands. These metadata are not part of any of the segment's
    ///   sections though.
    /// - The `__LINKEDIT` segment needs to be the last segment in the file. It should not
    ///   contain any sections. It's contents are mapped by other load commands, such as
    ///   symbol table, function starts and code signature (collectively derived from
    ///   <see cref="MachLinkEdit"/> class).
    /// </remarks>
    public class MachSegment : MachLoadCommand
    {
        private readonly MachObjectFile objectFile;
        private Stream? dataStream;
        private ulong size;
        private ulong fileSize;

        public MachSegment(MachObjectFile objectFile, string name)
            : this(objectFile, name, null)
        {
        }

        public MachSegment(MachObjectFile objectFile, string name, Stream? stream)
        {
            this.objectFile = objectFile;
            this.dataStream = stream;
            this.Name = name;
        }

        /// <summary>
        /// Gets the position of the segement in the object file.
        /// </summary>
        /// <remarks>
        /// The position is relative to the beginning of the architecture-specific object
        /// file. In fat binaries it needs to be adjusted to account for the envelope.
        /// </remarks>
        public ulong FileOffset { get; set; }

        /// <summary>
        /// Gets the size of the segment in the file.
        /// </summary>
        /// <remarks>
        /// We preserve the original FileSize when no editing on section contents was
        /// performed. ld64 aligns either to 16Kb or 4Kb page size based on compile time
        /// options. The __LINKEDIT segment is an exception that doesn't get aligned but
        /// since that one doesn't contain sections we don't do the special treatment.
        /// </remarks>
        public ulong FileSize
        {
            get
            {
                if (IsLinkEditSegment)
                {
                    return objectFile.LinkEditData.Select(d => d.FileOffset + d.Size).Max() - FileOffset;
                }

                return Sections.Count > 0 ? fileSize : (ulong)(dataStream?.Length ?? 0);
            }
            internal set
            {
                // Used by MachReader and MachObjectFile.UpdateLayout
                fileSize = value;
            }
        }

        /// <summary>
        /// Gets or sets the name of this segment.
        /// </summary>
        public string Name { get; private set; } = string.Empty;

        /// <summary>
        /// Gets or sets the virtual address of this section.
        /// </summary>
        public ulong VirtualAddress { get; set; }

        /// <summary>
        /// Gets or sets the size in bytes occupied in memory by this segment.
        /// </summary>
        public ulong Size
        {
            get
            {
                if (IsLinkEditSegment)
                {
                    const uint pageAlignment = 0x4000 - 1;
                    return (FileSize + pageAlignment) & ~pageAlignment;
                }

                return size;
            }
            set
            {
                // NOTE: We silently ignore setting Size for __LINKEDIT to make it easier
                // for the reader.
                size = value;
            }
        }

        /// <summary>
        /// Gets or sets the maximum permitted protection of this segment.
        /// <summary>
        public MachVmProtection MaximumProtection { get; set; }

        /// <summary>
        /// Gets or sets the initial protection of this segment.
        /// <summary>
        public MachVmProtection InitialProtection { get; set; }

        public MachSegmentFlags Flags { get; set; }

        /// <summary>
        /// List of sections contained in this segment.
        /// <summary>
        public IList<MachSection> Sections { get; } = new List<MachSection>();

        public bool IsLinkEditSegment => Name == "__LINKEDIT"; 

        public Stream GetReadStream()
        {
            if (Sections.Count != 0)
            {
                throw new NotSupportedException("Segment can only be read directly if there are no sections");
            }

            if (IsLinkEditSegment)
            {
                // NOTE: We can support reading the link edit segment by constructing the stream
                // from objectFile.LinkEditData on-demand.
                throw new NotSupportedException("Reading __LINKEDIT segment is unsupported");
            }

            if (FileSize == 0 || dataStream == null)
            {
                return Stream.Null;
            }

            return dataStream.Slice(0, (long)this.FileSize);
        }

        /// <summary>
        /// Gets the stream for updating the contents of this segment if it has no sections.
        /// <summary>
        /// <remarks>
        /// This method is primarily useful for the `__LINKEDIT` segment. The other primary
        /// segments (`__TEXT`, `__DATA`) are divided into sections and each section has to
        /// be updated individually.
        /// </remarks>
        public Stream GetWriteStream()
        {
            if (Sections.Count != 0)
            {
                throw new NotSupportedException("Segment can only be written to directly if there are no sections");
            }

            if (IsLinkEditSegment)
            {
                throw new NotSupportedException("Writing __LINKEDIT segment is unsupported");
            }

            dataStream = new UnclosableMemoryStream();
            return dataStream;
        }

        internal override IEnumerable<MachLinkEditData> LinkEditData
        {
            get
            {
                foreach (var section in Sections)
                {
                    if (section.RelocationData is MachLinkEditData relocationData)
                    {
                        yield return relocationData;
                    }
                }
            }
        }
    }
}