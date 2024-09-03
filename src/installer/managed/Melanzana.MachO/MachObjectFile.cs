using Melanzana.Streams;
using Melanzana.MachO.BinaryFormat;
using System.Text;
using System.Diagnostics;

namespace Melanzana.MachO
{
    public class MachObjectFile
    {
        private readonly Stream stream;

        public MachObjectFile()
            : this(Stream.Null)
        {
        }

        internal MachObjectFile(Stream stream)
        {
            this.stream = stream;
        }

        public bool Is64Bit => CpuType.HasFlag(MachCpuType.Architecture64);

        public bool IsLittleEndian { get; set; }

        public MachCpuType CpuType { get; set; }

        public uint CpuSubType { get; set; }

        public MachFileType FileType { get; set; }

        public MachHeaderFlags Flags { get; set; }

        public IList<MachLoadCommand> LoadCommands { get; } = new List<MachLoadCommand>();

        /// <summary>
        /// For object files the relocation, symbol tables and other data are stored at the end of the
        /// file but not covered by any segment/section. We maintain a list of these data to make it
        /// easier to address.
        /// 
        /// For linked files this points to the real __LINKEDIT segment. We slice it into subsections
        /// based on the known LinkEdit commands though.
        /// </summary>
        public IEnumerable<MachLinkEditData> LinkEditData => LoadCommands.SelectMany(command => command.LinkEditData);

        public IEnumerable<MachSegment> Segments => LoadCommands.OfType<MachSegment>();

        /// <summary>
        /// Get the lowest file offset of any section in the file. This allows calculating space that is
        /// reserved for adding new load commands (header pad).
        /// </summary>
        public ulong GetLowestSectionFileOffset()
        {
            ulong lowestFileOffset = ulong.MaxValue;

            foreach (var segment in Segments)
            {
                foreach (var section in segment.Sections)
                {
                    if (section.IsInFile &&
                        section.FileOffset < lowestFileOffset)
                    {
                        lowestFileOffset = section.FileOffset;
                    }
                }
            }

            return lowestFileOffset == ulong.MaxValue ? 0 : lowestFileOffset;
        }

        public ulong GetSize()
        {
            // Assume the size is the highest file offset+size of any segment
            return Segments.Max(s => s.FileOffset + s.FileSize);
        }

        public ulong GetSigningLimit()
        {
            var codeSignature = LoadCommands.OfType<MachCodeSignature>().FirstOrDefault();
            if (codeSignature != null)
            {
                // If code signature is present it has to be at the end of the file
                return codeSignature.FileOffset;
            }
            else
            {
                // If no code signature is present then we return the whole file size
                return GetSize();
            }
        }

        /// <summary>
        /// Gets a stream for a given part of range of the file.
        /// </summary>
        /// <remarks>
        /// The range must be fully contained in a single section or segment with no sections.
        /// Accessing file header or link commands through this API is currently not supported.
        /// </remarks>
        public Stream GetStreamAtFileOffset(uint fileOffset, uint fileSize)
        {
            // FIXME: Should we dispose the original stream? At the moment it would be no-op
            // anyway since it's always SliceStream or UnclosableMemoryStream.

            foreach (var segment in Segments)
            {
                if (fileOffset >= segment.FileOffset &&
                    fileOffset < segment.FileOffset + segment.FileSize)
                {
                    if (segment.Sections.Count == 0)
                    {
                        return segment.GetReadStream().Slice(
                            (long)(fileOffset - segment.FileOffset),
                            fileSize);
                    }

                    foreach (var section in segment.Sections)
                    {
                        if (fileOffset >= section.FileOffset &&
                            fileOffset < section.FileOffset + section.Size)
                        {
                            return section.GetReadStream().Slice(
                                (long)(fileOffset - section.FileOffset),
                                fileSize);
                        }
                    }

                    return Stream.Null;
                }
            }

            return Stream.Null;
        }

        public Stream GetStreamAtVirtualAddress(ulong address, uint length)
        {
            // FIXME: Should we dispose the original stream? At the moment it would be no-op
            // anyway since it's always SliceStream or UnclosableMemoryStream.

            foreach (var segment in Segments)
            {
                if (address >= segment.VirtualAddress &&
                    address < segment.VirtualAddress + segment.Size)
                {
                    if (segment.Sections.Count == 0)
                    {
                        return segment.GetReadStream().Slice(
                            (long)(address - segment.VirtualAddress),
                            (long)Math.Min(length, segment.VirtualAddress + segment.FileSize - address));
                    }

                    foreach (var section in segment.Sections)
                    {
                        if (address >= section.VirtualAddress &&
                            address < section.VirtualAddress + section.Size)
                        {
                            return section.GetReadStream().Slice(
                                (long)(address - section.VirtualAddress),
                                (long)Math.Min(length, section.VirtualAddress + section.Size - address));
                        }
                    }

                    return Stream.Null;
                }
            }

            return Stream.Null;
        }

        public Stream GetOriginalStream()
        {
            if (stream == null)
                return Stream.Null;

            return stream.Slice(0, stream.Length);
        }

        /// <summary>
        /// Update layout of segments, sections and link edit data to valid
        /// non-overlapping values that can be written to disk.
        /// </summary>
        /// <param name="options">Option bag to specify additional requirements for the layout</param>
        public void UpdateLayout(MachLayoutOptions? options = null)
        {
            // If no layout options were specified then infer the details
            // from the object file.
            if (options == null)
            {
                options = new MachLayoutOptions(this);
            }

            ulong segmentAlignment = options.SegmentAlignment;
            ulong fileOffset = 0;

            // First we need to place the load commands

            // 4 bytes magic number
            fileOffset += 4u;
            // Mach header
            fileOffset += (ulong)(Is64Bit ? MachHeader64.BinarySize : MachHeader.BinarySize);
            // Calculate size of load command
            foreach (var loadCommand in LoadCommands)
            {
                fileOffset += LoadCommandHeader.BinarySize;

                switch (loadCommand)
                {
                    case MachSegment segment:
                        if (Is64Bit)
                        {
                            fileOffset += (ulong)(Segment64Header.BinarySize + segment.Sections.Count * Section64Header.BinarySize);
                        }
                        else
                        {
                            fileOffset += (ulong)(SegmentHeader.BinarySize + segment.Sections.Count * SectionHeader.BinarySize);
                        }
                        break;

                    case MachCodeSignature: 
                    case MachDylibCodeSigningDirs:
                    case MachSegmentSplitInfo:
                    case MachFunctionStarts:
                    case MachDataInCode:
                    case MachLinkerOptimizationHint:
                    case MachDyldExportsTrie:
                    case MachDyldChainedFixups:
                        fileOffset += LinkEditHeader.BinarySize;
                        break;

                    case MachLoadDylibCommand:
                    case MachLoadWeakDylibCommand:
                    case MachReexportDylibCommand:
                        fileOffset += (ulong)AlignedSize(
                            DylibCommandHeader.BinarySize +
                            Encoding.UTF8.GetByteCount(((MachDylibCommand)loadCommand).Name) + 1,
                            Is64Bit);
                        break;

                    case MachEntrypointCommand entrypointCommand:
                        fileOffset += MainCommandHeader.BinarySize;
                        break;

                    case MachVersionMinMacOS:
                    case MachVersionMinIOS:
                    case MachVersionMinTvOS:
                    case MachVersionMinWatchOS:
                        fileOffset += VersionMinCommandHeader.BinarySize;
                        break;

                    case MachBuildVersion versionCommand:
                        fileOffset += (ulong)(BuildVersionCommandHeader.BinarySize + (versionCommand.ToolVersions.Count * BuildToolVersionHeader.BinarySize));
                        break;

                    case MachSymbolTable:
                        fileOffset += SymbolTableCommandHeader.BinarySize;
                        break;

                    case MachDynamicLinkEditSymbolTable:
                        fileOffset += DynamicSymbolTableCommandHeader.BinarySize;
                        break;

                    case MachCustomLoadCommand customLoadCommand:
                        fileOffset += (ulong)customLoadCommand.Data.Length;
                        break;
                }
            }

            fileOffset = (fileOffset + segmentAlignment) & ~segmentAlignment;
            ulong virtualAddress = options.BaseAddress;
            foreach (var segment in Segments)
            {
                ulong segmentFileSize = 0;
                ulong segmentSize = 0;

                segment.VirtualAddress = virtualAddress;
                segment.FileOffset = fileOffset;

                if (!segment.IsLinkEditSegment && segment.Sections.Count > 0)
                {
                    foreach (var section in segment.Sections)
                    {
                        ulong alignment = 1u << (int)section.Log2Alignment;

                        fileOffset = (fileOffset + alignment - 1) & ~(alignment - 1);
                        virtualAddress = (virtualAddress + alignment - 1) & ~(alignment - 1);

                        if (section.Type is not MachSectionType.ZeroFill or MachSectionType.GBZeroFill or MachSectionType.ThreadLocalZeroFill)
                        {
                            section.FileOffset = (uint)fileOffset;
                            fileOffset += section.Size;
                            segmentFileSize = Math.Max(segmentFileSize, fileOffset - segment.FileOffset);
                        }
                        else
                        {
                            // The offset is unused for virtual sections.
                            section.FileOffset = 0;
                        }

                        section.VirtualAddress = virtualAddress;
                        virtualAddress += section.Size;

                        segmentSize = Math.Max(segmentSize, virtualAddress - segment.VirtualAddress);
                    }

                    segment.FileSize = (segmentFileSize + segmentAlignment - 1) & ~(segmentAlignment - 1);
                    segment.Size = (segmentSize + segmentAlignment - 1) & ~(segmentAlignment - 1);

                    virtualAddress = (virtualAddress + segmentAlignment - 1) & ~(segmentAlignment - 1);
                }
            }

            var linkEditData = new List<MachLinkEditData>(LinkEditData);
            foreach (var data in linkEditData)
            {
                data.FileOffset = (uint)fileOffset;
                fileOffset += data.Size;
            }

            static int AlignedSize(int size, bool is64bit) => is64bit ? (size + 7) & ~7 : (size + 3) & ~3;
        }
    }
}