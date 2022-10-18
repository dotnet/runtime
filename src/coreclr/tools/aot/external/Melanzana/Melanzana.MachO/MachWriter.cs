using System.Buffers.Binary;
using System.Diagnostics;
using System.IO;
using System.Text;
using Melanzana.MachO.BinaryFormat;
using Melanzana.Streams;

namespace Melanzana.MachO
{
    public static class MachWriter
    {
        private static void WriteLoadCommandHeader(Stream stream, MachLoadCommandType commandType, int commandSize, bool isLittleEndian)
        {
            Span<byte> loadCommandHeaderBuffer = stackalloc byte[LoadCommandHeader.BinarySize];
            var loadCommandHeader = new LoadCommandHeader
            {
                CommandType = commandType,
                CommandSize = (uint)commandSize,
            };
            loadCommandHeader.Write(loadCommandHeaderBuffer, isLittleEndian, out var _);
            stream.Write(loadCommandHeaderBuffer);
        }

        private static void WriteSegment(Stream stream, MachSegment segment, bool isLittleEndian, bool is64bit)
        {
            if (is64bit)
            {
                Span<byte> sectionBuffer = stackalloc byte[Section64Header.BinarySize];
                Span<byte> segmentBuffer = stackalloc byte[Segment64Header.BinarySize];

                WriteLoadCommandHeader(
                    stream,
                    MachLoadCommandType.Segment64,
                    LoadCommandHeader.BinarySize + Segment64Header.BinarySize + segment.Sections.Count * Section64Header.BinarySize,
                    isLittleEndian);

                var segmentHeader = new Segment64Header
                {
                    Name = segment.Name,
                    Address = segment.VirtualAddress,
                    Size = segment.Size,
                    FileOffset = (ulong)segment.FileOffset,
                    FileSize = (ulong)segment.FileSize,
                    MaximumProtection = segment.MaximumProtection,
                    InitialProtection = segment.InitialProtection,
                    NumberOfSections = (uint)segment.Sections.Count,
                    Flags = segment.Flags,
                };
                segmentHeader.Write(segmentBuffer, isLittleEndian, out var _);
                stream.Write(segmentBuffer);

                foreach (var section in segment.Sections)
                {
                    var sectionHeader = new Section64Header
                    {
                        SectionName = section.SectionName,
                        SegmentName = section.SegmentName,
                        Address = section.VirtualAddress,
                        Size = section.Size,
                        FileOffset = section.FileOffset,
                        Log2Alignment = section.Log2Alignment,
                        RelocationOffset = section.RelocationOffset,
                        NumberOfReloationEntries = section.NumberOfRelocationEntries,
                        Flags = section.Flags,
                        Reserved1 = section.Reserved1,
                        Reserved2 = section.Reserved2,
                        Reserved3 = section.Reserved3,
                    };
                    sectionHeader.Write(sectionBuffer, isLittleEndian, out var _);
                    stream.Write(sectionBuffer);
                }
            }
            else
            {
                Span<byte> sectionBuffer = stackalloc byte[SectionHeader.BinarySize];
                Span<byte> segmentBuffer = stackalloc byte[SegmentHeader.BinarySize];

                WriteLoadCommandHeader(
                    stream,
                    MachLoadCommandType.Segment,
                    LoadCommandHeader.BinarySize + SegmentHeader.BinarySize + segment.Sections.Count * SectionHeader.BinarySize,
                    isLittleEndian);

                // FIXME: Validation

                var segmentHeader = new SegmentHeader
                {
                    Name = segment.Name,
                    Address = (uint)segment.VirtualAddress,
                    Size = (uint)segment.Size,
                    FileOffset = (uint)segment.FileOffset,
                    FileSize = (uint)segment.FileSize,
                    MaximumProtection = segment.MaximumProtection,
                    InitialProtection = segment.InitialProtection,
                    NumberOfSections = (uint)segment.Sections.Count,
                    Flags = segment.Flags,
                };
                segmentHeader.Write(segmentBuffer, isLittleEndian, out var _);
                stream.Write(segmentBuffer);

                foreach (var section in segment.Sections)
                {
                    var sectionHeader = new SectionHeader
                    {
                        SectionName = section.SectionName,
                        SegmentName = section.SegmentName,
                        Address = (uint)section.VirtualAddress,
                        Size = (uint)section.Size,
                        FileOffset = section.FileOffset,
                        Log2Alignment = section.Log2Alignment,
                        RelocationOffset = section.RelocationOffset,
                        NumberOfReloationEntries = section.NumberOfRelocationEntries,
                        Flags = section.Flags,
                        Reserved1 = section.Reserved1,
                        Reserved2 = section.Reserved2,
                    };
                    sectionHeader.Write(sectionBuffer, isLittleEndian, out var _);
                    stream.Write(sectionBuffer);
                }
            }
        }

        private static void WriteLinkEdit(Stream stream, MachLoadCommandType commandType, MachLinkEdit linkEdit, bool isLittleEndian)
        {
            WriteLoadCommandHeader(
                stream,
                commandType,
                LoadCommandHeader.BinarySize + LinkEditHeader.BinarySize,
                isLittleEndian);

            var linkEditHeaderBuffer = new byte[LinkEditHeader.BinarySize];
            var linkEditHeader = new LinkEditHeader
            {
                FileOffset = linkEdit.FileOffset,
                FileSize = linkEdit.FileSize,
            };
            linkEditHeader.Write(linkEditHeaderBuffer, isLittleEndian, out var _);
            stream.Write(linkEditHeaderBuffer);
        }

        private static int AlignedSize(int size, bool is64bit)
            => is64bit ? (size + 7) & ~7 : (size + 3) & ~3;

        private static void WriteDylibCommand(Stream stream, MachLoadCommandType commandType, MachDylibCommand dylibCommand, bool isLittleEndian, bool is64Bit)
        {
            byte[] nameBytes = Encoding.UTF8.GetBytes(dylibCommand.Name);
            int commandSize = AlignedSize(LoadCommandHeader.BinarySize + DylibCommandHeader.BinarySize + nameBytes.Length + 1, is64Bit);

            WriteLoadCommandHeader(
                stream,
                commandType,
                commandSize,
                isLittleEndian);

            Span<byte> dylibCommandHeaderBuffer = stackalloc byte[DylibCommandHeader.BinarySize];
            var dylibCommandHeader = new DylibCommandHeader
            {
                NameOffset = LoadCommandHeader.BinarySize + DylibCommandHeader.BinarySize,
                Timestamp = dylibCommand.Timestamp,
                CurrentVersion = dylibCommand.CurrentVersion,
                CompatibilityVersion = dylibCommand.CompatibilityVersion,
            };
            dylibCommandHeader.Write(dylibCommandHeaderBuffer, isLittleEndian, out var _);
            stream.Write(dylibCommandHeaderBuffer);
            stream.Write(nameBytes);
            // The name is always written with terminating `\0` and aligned to platform
            // pointer size.
            stream.WritePadding(commandSize - dylibCommandHeader.NameOffset - nameBytes.Length);
        }

        private static void WriteMainCommand(Stream stream, MachEntrypointCommand entrypointCommand, bool isLittleEndian)
        {
            WriteLoadCommandHeader(
                stream,
                MachLoadCommandType.Main,
                LoadCommandHeader.BinarySize + MainCommandHeader.BinarySize,
                isLittleEndian);

            Span<byte> mainCommandHeaderBuffer = stackalloc byte[MainCommandHeader.BinarySize];
            var mainCommandHeader = new MainCommandHeader
            {
                FileOffset = entrypointCommand.FileOffset,
                StackSize = entrypointCommand.StackSize,
            };
            mainCommandHeader.Write(mainCommandHeaderBuffer, isLittleEndian, out var _);
            stream.Write(mainCommandHeaderBuffer);
        }

        private static uint ConvertVersion(Version version)
            => ((uint)version.Major << 16) | (uint)((version.Minor & 0xff) << 8) | (uint)(version.Build & 0xff);

        private static void WriteVersionMinCommand(Stream stream, MachLoadCommandType commandType, MachBuildVersionBase versionCommand, bool isLittleEndian)
        {
            WriteLoadCommandHeader(
                stream,
                commandType,
                LoadCommandHeader.BinarySize + VersionMinCommandHeader.BinarySize,
                isLittleEndian);

            Span<byte> versionMinHeaderBuffer = stackalloc byte[VersionMinCommandHeader.BinarySize];
            var versionMinHeader = new VersionMinCommandHeader
            {
                MinimumPlatformVersion = ConvertVersion(versionCommand.MinimumPlatformVersion),
                SdkVersion = ConvertVersion(versionCommand.SdkVersion),
            };
            versionMinHeader.Write(versionMinHeaderBuffer, isLittleEndian, out var _);
            stream.Write(versionMinHeaderBuffer);
        }

        private static void WriteBuildVersion(Stream stream, MachBuildVersion versionCommand, bool isLittleEndian)
        {
            WriteLoadCommandHeader(
                stream,
                MachLoadCommandType.BuildVersion,
                LoadCommandHeader.BinarySize + BuildVersionCommandHeader.BinarySize + (versionCommand.ToolVersions.Count * BuildToolVersionHeader.BinarySize),
                isLittleEndian);

            Span<byte> buildVersionBuffer = stackalloc byte[BuildVersionCommandHeader.BinarySize];
            Span<byte> buildToolVersionBuffer = stackalloc byte[BuildToolVersionHeader.BinarySize];
            var buildVersionHeader = new BuildVersionCommandHeader
            {
                Platform = versionCommand.Platform,
                MinimumPlatformVersion = ConvertVersion(versionCommand.MinimumPlatformVersion),
                SdkVersion = ConvertVersion(versionCommand.SdkVersion),
                NumberOfTools = (uint)versionCommand.ToolVersions.Count,
            };
            buildVersionHeader.Write(buildVersionBuffer, isLittleEndian, out var _);
            stream.Write(buildVersionBuffer);

            foreach (var toolVersion in versionCommand.ToolVersions)
            {
                var buildToolVersionHeader = new BuildToolVersionHeader
                {
                    BuildTool = toolVersion.BuildTool,
                    Version = ConvertVersion(toolVersion.Version),
                };
                buildToolVersionHeader.Write(buildToolVersionBuffer, isLittleEndian, out var _);
                stream.Write(buildToolVersionBuffer);
            }
        }

        private static void WriteSymbolTableCommand(Stream stream, MachSymbolTable symbolTable, bool isLittleEndian, bool is64Bit)
        {
            WriteLoadCommandHeader(
                stream,
                MachLoadCommandType.SymbolTable,
                LoadCommandHeader.BinarySize + SymbolTableCommandHeader.BinarySize,
                isLittleEndian);

            uint symbolSize = SymbolHeader.BinarySize + (is64Bit ? 8u : 4u);

            Span<byte> symbolTableHeaderBuffer = stackalloc byte[SymbolTableCommandHeader.BinarySize];
            var symbolTableHeader = new SymbolTableCommandHeader
            {
                SymbolTableOffset = symbolTable.SymbolTableData.FileOffset,
                NumberOfSymbols = (uint)(symbolTable.SymbolTableData.Size / symbolSize),
                StringTableOffset = symbolTable.StringTableData.FileOffset,
                StringTableSize = (uint)symbolTable.StringTableData.Size,
            };
            symbolTableHeader.Write(symbolTableHeaderBuffer, isLittleEndian, out var _);
            stream.Write(symbolTableHeaderBuffer);
        }

        private static void WriteDynamicLinkEditSymbolTableCommand(Stream stream, MachDynamicLinkEditSymbolTable dySymbolTable, bool isLittleEndian)
        {
            WriteLoadCommandHeader(
                stream,
                MachLoadCommandType.DynamicLinkEditSymbolTable,
                LoadCommandHeader.BinarySize + DynamicSymbolTableCommandHeader.BinarySize,
                isLittleEndian);

            Span<byte> symbolTableHeaderBuffer = stackalloc byte[DynamicSymbolTableCommandHeader.BinarySize];
            var symbolTableHeader = dySymbolTable.Header;
            symbolTableHeader.Write(symbolTableHeaderBuffer, isLittleEndian, out var _);
            stream.Write(symbolTableHeaderBuffer);
        }

        private static void WriteDyldInfoCommand(Stream stream, MachLoadCommandType commandType, MachDyldInfo dyldInfo, bool isLittleEndian)
        {
            WriteLoadCommandHeader(
                stream,
                commandType,
                LoadCommandHeader.BinarySize + DyldInfoHeader.BinarySize,
                isLittleEndian);

            Span<byte> dyldInfoHeaderBuffer = stackalloc byte[DyldInfoHeader.BinarySize];
            var dyldInfoHeader = new DyldInfoHeader
            {
                RebaseOffset = dyldInfo.RebaseData.FileOffset,
                RebaseSize = (uint)dyldInfo.RebaseData.Size,
                BindOffset = dyldInfo.BindData.FileOffset,
                BindSize = (uint)dyldInfo.BindData.Size,
                WeakBindOffset = dyldInfo.WeakBindData.FileOffset,
                WeakBindSize = (uint)dyldInfo.WeakBindData.Size,
                LazyBindOffset = dyldInfo.LazyBindData.FileOffset,
                LazyBindSize = (uint)dyldInfo.LazyBindData.Size,
                ExportOffset = dyldInfo.ExportData.FileOffset,
                ExportSize = (uint)dyldInfo.ExportData.Size,
            };
            dyldInfoHeader.Write(dyldInfoHeaderBuffer, isLittleEndian, out var _);
            stream.Write(dyldInfoHeaderBuffer);
        }

        private static void WriteTwoLevelHintsCommand(Stream stream, MachTwoLevelHints twoLevelHints, bool isLittleEndian)
        {
            WriteLoadCommandHeader(
                stream,
                MachLoadCommandType.TowLevelHints,
                LoadCommandHeader.BinarySize + DyldInfoHeader.BinarySize,
                isLittleEndian);

            Span<byte> twoLevelHintsHeaderBuffer = stackalloc byte[TwoLevelHintsHeader.BinarySize];
            var twoLevelHintsHeader = new TwoLevelHintsHeader
            {
                FileOffset = twoLevelHints.Data.FileOffset,
                NumberOfHints = (uint)(twoLevelHints.Data.Size / sizeof(uint))
            };
            twoLevelHintsHeader.Write(twoLevelHintsHeaderBuffer, isLittleEndian, out var _);
            stream.Write(twoLevelHintsHeaderBuffer);
        }

        public static void Write(Stream stream, MachObjectFile objectFile)
        {
            long initialOffset = stream.Position;
            bool isLittleEndian = objectFile.IsLittleEndian;
            var machMagicBuffer = new byte[4];
            var machHeaderBuffer = new byte[Math.Max(MachHeader.BinarySize, MachHeader64.BinarySize)];

            uint magic = isLittleEndian ?
                (objectFile.Is64Bit ? (uint)MachMagic.MachHeader64LittleEndian : (uint)MachMagic.MachHeaderLittleEndian) :
                (objectFile.Is64Bit ? (uint)MachMagic.MachHeader64BigEndian : (uint)MachMagic.MachHeaderBigEndian);
            BinaryPrimitives.WriteUInt32BigEndian(machMagicBuffer, magic);

            var loadCommandsStream = new MemoryStream();
            foreach (var loadCommand in objectFile.LoadCommands)
            {
                switch (loadCommand)
                {
                    case MachSegment segment: WriteSegment(loadCommandsStream, segment, isLittleEndian, objectFile.Is64Bit); break;
                    case MachCodeSignature codeSignature: WriteLinkEdit(loadCommandsStream, MachLoadCommandType.CodeSignature, codeSignature, isLittleEndian); break;
                    case MachDylibCodeSigningDirs codeSigningDirs: WriteLinkEdit(loadCommandsStream, MachLoadCommandType.DylibCodeSigningDRs, codeSigningDirs, isLittleEndian); break;
                    case MachSegmentSplitInfo segmentSplitInfo: WriteLinkEdit(loadCommandsStream, MachLoadCommandType.SegmentSplitInfo, segmentSplitInfo, isLittleEndian); break;
                    case MachFunctionStarts functionStarts: WriteLinkEdit(loadCommandsStream, MachLoadCommandType.FunctionStarts, functionStarts, isLittleEndian); break;
                    case MachDataInCode dataInCode: WriteLinkEdit(loadCommandsStream, MachLoadCommandType.DataInCode, dataInCode, isLittleEndian); break;
                    case MachLinkerOptimizationHint linkerOptimizationHint: WriteLinkEdit(loadCommandsStream, MachLoadCommandType.LinkerOptimizationHint, linkerOptimizationHint, isLittleEndian); break;
                    case MachDyldExportsTrie dyldExportsTrie: WriteLinkEdit(loadCommandsStream, MachLoadCommandType.DyldExportsTrie, dyldExportsTrie, isLittleEndian); break;
                    case MachDyldChainedFixups dyldChainedFixups: WriteLinkEdit(loadCommandsStream, MachLoadCommandType.DyldChainedFixups, dyldChainedFixups, isLittleEndian); break;
                    case MachLoadDylibCommand loadDylibCommand: WriteDylibCommand(loadCommandsStream, MachLoadCommandType.LoadDylib, loadDylibCommand, isLittleEndian, objectFile.Is64Bit); break;
                    case MachLoadWeakDylibCommand loadWeakDylibCommand: WriteDylibCommand(loadCommandsStream, MachLoadCommandType.LoadWeakDylib, loadWeakDylibCommand, isLittleEndian, objectFile.Is64Bit); break;
                    case MachReexportDylibCommand reexportDylibCommand: WriteDylibCommand(loadCommandsStream, MachLoadCommandType.ReexportDylib, reexportDylibCommand, isLittleEndian, objectFile.Is64Bit); break;
                    case MachEntrypointCommand entrypointCommand: WriteMainCommand(loadCommandsStream, entrypointCommand, isLittleEndian); break;
                    case MachVersionMinMacOS macOSBuildVersion: WriteVersionMinCommand(loadCommandsStream, MachLoadCommandType.VersionMinMacOS, macOSBuildVersion, isLittleEndian); break;
                    case MachVersionMinIOS iOSBuildVersion: WriteVersionMinCommand(loadCommandsStream, MachLoadCommandType.VersionMinIPhoneOS, iOSBuildVersion, isLittleEndian); break;
                    case MachVersionMinTvOS tvOSBuildVersion: WriteVersionMinCommand(loadCommandsStream, MachLoadCommandType.VersionMinTvOS, tvOSBuildVersion, isLittleEndian); break;
                    case MachVersionMinWatchOS watchOSBuildVersion: WriteVersionMinCommand(loadCommandsStream, MachLoadCommandType.VersionMinWatchOS, watchOSBuildVersion, isLittleEndian); break;
                    case MachBuildVersion buildVersion: WriteBuildVersion(loadCommandsStream, buildVersion, isLittleEndian); break;
                    case MachSymbolTable symbolTable: WriteSymbolTableCommand(loadCommandsStream, symbolTable, isLittleEndian, objectFile.Is64Bit); break;
                    case MachDynamicLinkEditSymbolTable dySymbolTable: WriteDynamicLinkEditSymbolTableCommand(loadCommandsStream, dySymbolTable, isLittleEndian); break;
                    case MachDyldInfoOnly dyldInfoOnly: WriteDyldInfoCommand(loadCommandsStream, MachLoadCommandType.DyldInfoOnly, dyldInfoOnly, isLittleEndian); break;
                    case MachDyldInfo dyldInfo: WriteDyldInfoCommand(loadCommandsStream, MachLoadCommandType.DyldInfo, dyldInfo, isLittleEndian); break;
                    case MachTwoLevelHints twoLevelHints: WriteTwoLevelHintsCommand(loadCommandsStream, twoLevelHints, isLittleEndian); break;
                    case MachCustomLoadCommand customLoadCommand:
                        WriteLoadCommandHeader(loadCommandsStream, customLoadCommand.Type, customLoadCommand.Data.Length + LoadCommandHeader.BinarySize, isLittleEndian);
                        loadCommandsStream.Write(customLoadCommand.Data);
                        break;
                    default:
                        Debug.Fail("Unknown load command");
                        break;
                }
            }

            if (objectFile.Is64Bit)
            {
                var machHeader = new MachHeader64
                {
                    CpuType = objectFile.CpuType,
                    CpuSubType = objectFile.CpuSubType,
                    FileType = objectFile.FileType,
                    NumberOfCommands = (uint)objectFile.LoadCommands.Count,
                    SizeOfCommands = (uint)loadCommandsStream.Length,
                    Flags = objectFile.Flags,
                    Reserved = 0, // TODO
                };

                stream.Write(machMagicBuffer);
                machHeader.Write(machHeaderBuffer, isLittleEndian, out int bytesWritten);
                stream.Write(machHeaderBuffer.AsSpan(0, bytesWritten));
            }
            else
            {
                var machHeader = new MachHeader
                {
                    CpuType = objectFile.CpuType,
                    CpuSubType = objectFile.CpuSubType,
                    FileType = objectFile.FileType,
                    NumberOfCommands = (uint)objectFile.LoadCommands.Count,
                    SizeOfCommands = (uint)loadCommandsStream.Length,
                    Flags = objectFile.Flags,
                };

                stream.Write(machMagicBuffer);
                machHeader.Write(machHeaderBuffer, isLittleEndian, out int bytesWritten);
                stream.Write(machHeaderBuffer.AsSpan(0, bytesWritten));
            }

            loadCommandsStream.Position = 0;
            loadCommandsStream.CopyTo(stream);

            // Save the current position within the Mach-O file. Now we need to output the segments
            // and fill in the gaps as we go.
            ulong currentOffset = (ulong)(stream.Position - initialOffset);
            var orderedSegments = objectFile.Segments.OrderBy(s => s.FileOffset).ToList();

            foreach (var segment in orderedSegments)
            {
                if (segment.FileSize != 0)
                {
                    if (segment.Sections.Count == 0)
                    {
                        Debug.Assert(segment.FileOffset >= currentOffset);

                        if (segment.FileOffset > currentOffset)
                        {
                            ulong paddingSize = segment.FileOffset - currentOffset;
                            stream.WritePadding((long)paddingSize);
                            currentOffset += paddingSize;
                        }

                        if (segment.IsLinkEditSegment)
                        {
                            // __LINKEDIT always has to be the last segment in object file, so break
                            // out of the loop.
                            break;
                        }

                        using var segmentStream = segment.GetReadStream();
                        segmentStream.CopyTo(stream);
                        currentOffset += (ulong)segmentStream.Length;
                    }
                    else
                    {
                        byte paddingByte = 0;

                        foreach (var section in segment.Sections)
                        {
                            if (section.IsInFile)
                            {
                                Debug.Assert(section.FileOffset >= currentOffset);

                                if (section.FileOffset > currentOffset)
                                {
                                    ulong paddingSize = section.FileOffset - currentOffset;
                                    stream.WritePadding((long)paddingSize, paddingByte);
                                    currentOffset += paddingSize;
                                }

                                using var sectionStream = section.GetReadStream();
                                sectionStream.CopyTo(stream);
                                currentOffset += (ulong)sectionStream.Length;

                                bool isCodeSection =
                                    section.Type == MachSectionType.Regular &&
                                    section.Attributes.HasFlag(MachSectionAttributes.SomeInstructions) &&
                                    section.Attributes.HasFlag(MachSectionAttributes.PureInstructions);

                                if (isCodeSection)
                                {
                                    // Padding of code sections is done with NOP bytes if possible
                                    paddingByte = objectFile.CpuType switch
                                    {
                                        // TODO: Arm32 / Thumb
                                        MachCpuType.X86_64 => (byte)0x90,
                                        MachCpuType.X86 => (byte)0x90,
                                        _ => (byte)0,
                                    };
                                }
                                else
                                {
                                    paddingByte = 0;
                                }
                            }
                        }
                    }
                }
            }

            // We are either writing an unlinked object file or a __LINKEDIT segment
            var linkEditData = new List<MachLinkEditData>(objectFile.LinkEditData);

            // Sort by file offset first
            linkEditData.Sort((sectionA, sectionB) =>
                sectionA.FileOffset < sectionB.FileOffset ? -1 :
                (sectionA.FileOffset > sectionB.FileOffset ? 1 : 0));

            foreach (var data in linkEditData)
            {
                if (data.FileOffset > currentOffset)
                {
                    ulong paddingSize = data.FileOffset - currentOffset;
                    stream.WritePadding((long)paddingSize);
                    currentOffset += paddingSize;
                }

                using var segmentStream = data.GetReadStream();
                segmentStream.CopyTo(stream);
                currentOffset += (ulong)segmentStream.Length;
            }
        }

        public static void Write(Stream stream, IList<MachObjectFile> objectFiles)
        {
            if (objectFiles.Count == 1)
            {
                Write(stream, objectFiles[0]);
            }
            else if (objectFiles.Count > 1)
            {
                var fatMagic = new byte[4];
                var fatHeader = new FatHeader { NumberOfFatArchitectures = (uint)objectFiles.Count };
                var fatHeaderBytes = new byte[FatHeader.BinarySize];
                var fatArchHeaderBytes = new byte[FatArchHeader.BinarySize];

                BinaryPrimitives.WriteUInt32BigEndian(fatMagic, (uint)MachMagic.FatMagicBigEndian);
                fatHeader.Write(fatHeaderBytes, isLittleEndian: false, out var _);
                stream.Write(fatMagic);
                stream.Write(fatHeaderBytes);

                uint offset = (uint)(4 + FatHeader.BinarySize + objectFiles.Count * FatArchHeader.BinarySize);
                uint alignment = 0x4000;
                foreach (var objectFile in objectFiles)
                {
                    uint size = (uint)objectFile.GetSize();

                    offset = (offset + alignment - 1) & ~(alignment - 1);
                    var fatArchHeader = new FatArchHeader
                    {
                        CpuType = objectFile.CpuType,
                        CpuSubType = objectFile.CpuSubType,
                        Offset = offset,
                        Size = size,
                        Alignment = (uint)Math.Log2(alignment),
                    };

                    fatArchHeader.Write(fatArchHeaderBytes, isLittleEndian: false, out var _);
                    stream.Write(fatArchHeaderBytes);

                    offset += size;
                }

                offset = (uint)(4 + FatHeader.BinarySize + objectFiles.Count * FatArchHeader.BinarySize);
                foreach (var objectFile in objectFiles)
                {
                    uint size = (uint)objectFile.GetSize();
                    uint alignedOffset = (offset + alignment - 1) & ~(alignment - 1);
                    stream.WritePadding(alignedOffset - offset);
                    Write(stream, objectFile);
                    offset = alignedOffset + size;
                }
            }
        }
    }
}
