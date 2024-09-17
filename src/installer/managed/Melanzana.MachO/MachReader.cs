// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Melanzana.MachO.BinaryFormat;
using Melanzana.Streams;
using System.Buffers.Binary;
using System.Diagnostics;
using System.Text;

namespace Melanzana.MachO
{
    public static class MachReader
    {
        private static MachSegment ReadSegment(ReadOnlySpan<byte> loadCommandPtr, MachObjectFile objectFile, Stream stream)
        {
            var segmentHeader = SegmentHeader.Read(loadCommandPtr.Slice(LoadCommandHeader.BinarySize), objectFile.IsLittleEndian, out var _);

            var machSegment = segmentHeader.NumberOfSections == 0 && segmentHeader.FileSize != 0 ?
                new MachSegment(objectFile, segmentHeader.Name, stream.Slice(segmentHeader.FileOffset, segmentHeader.FileSize)) :
                new MachSegment(objectFile, segmentHeader.Name);
            machSegment.FileOffset = segmentHeader.FileOffset;
            machSegment.FileSize = segmentHeader.FileSize;
            machSegment.VirtualAddress = segmentHeader.Address;
            machSegment.Size = segmentHeader.Size;
            machSegment.MaximumProtection = segmentHeader.MaximumProtection;
            machSegment.InitialProtection = segmentHeader.InitialProtection;
            machSegment.Flags = segmentHeader.Flags;

            for (int s = 0; s < segmentHeader.NumberOfSections; s++)
            {
                var sectionHeader = SectionHeader.Read(loadCommandPtr.Slice(LoadCommandHeader.BinarySize + SegmentHeader.BinarySize + s * SectionHeader.BinarySize), objectFile.IsLittleEndian, out var _);
                var sectionType = (MachSectionType)(sectionHeader.Flags & 0xff);
                MachSection section;

                if (sectionHeader.Size != 0 &&
                    sectionType != MachSectionType.ZeroFill &&
                    sectionType != MachSectionType.GBZeroFill &&
                    sectionType != MachSectionType.ThreadLocalZeroFill)
                {
                    section = new MachSection(
                        objectFile,
                        sectionHeader.SegmentName,
                        sectionHeader.SectionName,
                        stream.Slice(sectionHeader.FileOffset, sectionHeader.Size),
                        new MachLinkEditData(stream, sectionHeader.RelocationOffset, sectionHeader.NumberOfReloationEntries * 8));
                }
                else
                {
                    section = new MachSection(
                        objectFile,
                        sectionHeader.SegmentName,
                        sectionHeader.SectionName,
                        null,
                        new MachLinkEditData(stream, sectionHeader.RelocationOffset, sectionHeader.NumberOfReloationEntries * 8))
                    {
                        Size = sectionHeader.Size
                    };
                }

                section.VirtualAddress = sectionHeader.Address;
                section.FileOffset = sectionHeader.FileOffset;
                section.Log2Alignment = sectionHeader.Log2Alignment;
                section.Flags = sectionHeader.Flags;
                section.Reserved1 = sectionHeader.Reserved1;
                section.Reserved2 = sectionHeader.Reserved2;
                machSegment.Sections.Add(section);
            }

            return machSegment;
        }

        private static MachSegment ReadSegment64(ReadOnlySpan<byte> loadCommandPtr, MachObjectFile objectFile, Stream stream)
        {
            var segmentHeader = Segment64Header.Read(loadCommandPtr.Slice(LoadCommandHeader.BinarySize), objectFile.IsLittleEndian, out var _);

            var machSegment = segmentHeader.NumberOfSections == 0 && segmentHeader.FileSize != 0 ?
                new MachSegment(objectFile, segmentHeader.Name, stream.Slice((long)segmentHeader.FileOffset, (long)segmentHeader.FileSize)) :
                new MachSegment(objectFile, segmentHeader.Name);
            machSegment.FileOffset = segmentHeader.FileOffset;
            machSegment.FileSize = segmentHeader.FileSize;
            machSegment.VirtualAddress = segmentHeader.Address;
            machSegment.Size = segmentHeader.Size;
            machSegment.MaximumProtection = segmentHeader.MaximumProtection;
            machSegment.InitialProtection = segmentHeader.InitialProtection;
            machSegment.Flags = segmentHeader.Flags;

            for (int s = 0; s < segmentHeader.NumberOfSections; s++)
            {
                var sectionHeader = Section64Header.Read(loadCommandPtr.Slice(LoadCommandHeader.BinarySize + Segment64Header.BinarySize + s * Section64Header.BinarySize), objectFile.IsLittleEndian, out var _);
                var sectionType = (MachSectionType)(sectionHeader.Flags & 0xff);
                MachSection section;

                if (sectionHeader.Size != 0 &&
                    sectionType != MachSectionType.ZeroFill &&
                    sectionType != MachSectionType.GBZeroFill &&
                    sectionType != MachSectionType.ThreadLocalZeroFill)
                {
                    section = new MachSection(
                        objectFile,
                        sectionHeader.SegmentName,
                        sectionHeader.SectionName,
                        stream.Slice(sectionHeader.FileOffset, (long)sectionHeader.Size),
                        new MachLinkEditData(stream, sectionHeader.RelocationOffset, sectionHeader.NumberOfReloationEntries * 8));
                }
                else
                {
                    section = new MachSection(
                        objectFile,
                        sectionHeader.SegmentName,
                        sectionHeader.SectionName,
                        null,
                        new MachLinkEditData(stream, sectionHeader.RelocationOffset, sectionHeader.NumberOfReloationEntries * 8))
                    {
                        Size = sectionHeader.Size
                    };
                }

                section.VirtualAddress = sectionHeader.Address;
                section.FileOffset = sectionHeader.FileOffset;
                section.Log2Alignment = sectionHeader.Log2Alignment;
                section.Flags = sectionHeader.Flags;
                section.Reserved1 = sectionHeader.Reserved1;
                section.Reserved2 = sectionHeader.Reserved2;
                section.Reserved3 = sectionHeader.Reserved3;
                machSegment.Sections.Add(section);
            }

            return machSegment;
        }

        private static MachLinkEditData ReadLinkEdit(ReadOnlySpan<byte> loadCommandPtr, MachObjectFile objectFile, Stream stream)
        {
            var linkEditHeader = LinkEditHeader.Read(loadCommandPtr.Slice(LoadCommandHeader.BinarySize), objectFile.IsLittleEndian, out var _);
            return new MachLinkEditData(stream, linkEditHeader.FileOffset, linkEditHeader.FileSize);
        }

        private static T ReadDylibCommand<T>(ReadOnlySpan<byte> loadCommandPtr, uint commandSize, bool isLittleEndian)
            where T : MachDylibCommand, new()
        {
            var dylibCommandHeader = DylibCommandHeader.Read(loadCommandPtr.Slice(LoadCommandHeader.BinarySize), isLittleEndian, out var _);

            Debug.Assert(dylibCommandHeader.NameOffset == LoadCommandHeader.BinarySize + DylibCommandHeader.BinarySize);
            var nameSlice = loadCommandPtr.Slice((int)dylibCommandHeader.NameOffset, (int)commandSize - (int)dylibCommandHeader.NameOffset);
            int zeroIndex = nameSlice.IndexOf((byte)0);
            string name = zeroIndex >= 0 ? Encoding.UTF8.GetString(nameSlice.Slice(0, zeroIndex).ToArray()) : Encoding.UTF8.GetString(nameSlice.ToArray());

            return new T
            {
                Name = name,
                Timestamp = dylibCommandHeader.Timestamp,
                CurrentVersion = dylibCommandHeader.CurrentVersion,
                CompatibilityVersion = dylibCommandHeader.CompatibilityVersion,
            };
        }

        private static MachEntrypointCommand ReadMainCommand(ReadOnlySpan<byte> loadCommandPtr, bool isLittleEndian)
        {
            var mainCommandHeader = MainCommandHeader.Read(loadCommandPtr.Slice(LoadCommandHeader.BinarySize), isLittleEndian, out var _);

            return new MachEntrypointCommand
            {
                FileOffset = mainCommandHeader.FileOffset,
                StackSize = mainCommandHeader.StackSize,
            };
        }

        private static Version ConvertVersion(uint version)
            => new Version((int)(version >> 16), (int)((version >> 8) & 0xff), (int)(version & 0xff));

        private static T ReadVersionMinCommand<T>(ReadOnlySpan<byte> loadCommandPtr, bool isLittleEndian)
            where T : MachBuildVersionBase, new()
        {
            var versionMinCommandHeader = VersionMinCommandHeader.Read(loadCommandPtr.Slice(LoadCommandHeader.BinarySize), isLittleEndian, out var _);

            return new T
            {
                MinimumPlatformVersion = ConvertVersion(versionMinCommandHeader.MinimumPlatformVersion),
                SdkVersion = ConvertVersion(versionMinCommandHeader.SdkVersion),
            };
        }

        private static MachBuildVersion ReadBuildVersion(ReadOnlySpan<byte> loadCommandPtr, bool isLittleEndian)
        {
            var buildVersionCommandHeader = BuildVersionCommandHeader.Read(loadCommandPtr.Slice(LoadCommandHeader.BinarySize), isLittleEndian, out var _);
            var buildVersion = new MachBuildVersion
            {
                TargetPlatform = buildVersionCommandHeader.Platform,
                MinimumPlatformVersion = ConvertVersion(buildVersionCommandHeader.MinimumPlatformVersion),
                SdkVersion = ConvertVersion(buildVersionCommandHeader.SdkVersion),
            };


            loadCommandPtr = loadCommandPtr.Slice(LoadCommandHeader.BinarySize + BuildVersionCommandHeader.BinarySize);
            for (int i = 0; i < buildVersionCommandHeader.NumberOfTools; i++)
            {
                var buildToolVersionHeader = BuildToolVersionHeader.Read(loadCommandPtr, isLittleEndian, out var _);
                buildVersion.ToolVersions.Add(new MachBuildToolVersion
                {
                    BuildTool = buildToolVersionHeader.BuildTool,
                    Version = ConvertVersion(buildToolVersionHeader.Version),
                });
                loadCommandPtr = loadCommandPtr.Slice(BuildToolVersionHeader.BinarySize);
            }

            return buildVersion;
        }

        private static MachSymbolTable ReadSymbolTable(ReadOnlySpan<byte> loadCommandPtr, MachObjectFile objectFile, Stream stream)
        {
            var symbolTableHeader = SymbolTableCommandHeader.Read(loadCommandPtr.Slice(LoadCommandHeader.BinarySize), objectFile.IsLittleEndian, out var _);

            uint symbolTableSize =
                symbolTableHeader.NumberOfSymbols *
                (SymbolHeader.BinarySize + (objectFile.Is64Bit ? 8u : 4u));

            return new MachSymbolTable(
                objectFile,
                new MachLinkEditData(stream, symbolTableHeader.SymbolTableOffset, symbolTableSize),
                new MachLinkEditData(stream, symbolTableHeader.StringTableOffset, symbolTableHeader.StringTableSize));
        }

        private static MachDynamicLinkEditSymbolTable ReadDynamicLinkEditSymbolTable(ReadOnlySpan<byte> loadCommandPtr, MachObjectFile objectFile, Stream stream)
        {
            var dynamicSymbolTableHeader = DynamicSymbolTableCommandHeader.Read(loadCommandPtr.Slice(LoadCommandHeader.BinarySize), objectFile.IsLittleEndian, out var _);

            // TODO: Clean up
            return new MachDynamicLinkEditSymbolTable(objectFile, stream, dynamicSymbolTableHeader);
        }

        private static MachDyldInfo ReadDyldInfo(
            MachLoadCommandType loadCommandType,
            ReadOnlySpan<byte> loadCommandPtr,
            MachObjectFile objectFile,
            Stream stream)
        {
            var dyldInfoHeader = DyldInfoHeader.Read(loadCommandPtr.Slice(LoadCommandHeader.BinarySize), objectFile.IsLittleEndian, out var _);

            var rebaseData = new MachLinkEditData(stream, dyldInfoHeader.RebaseOffset, dyldInfoHeader.RebaseSize);
            var bindData = new MachLinkEditData(stream, dyldInfoHeader.BindOffset, dyldInfoHeader.BindSize);
            var weakBindData = new MachLinkEditData(stream, dyldInfoHeader.WeakBindOffset, dyldInfoHeader.WeakBindSize);
            var lazyBindData = new MachLinkEditData(stream, dyldInfoHeader.LazyBindOffset, dyldInfoHeader.LazyBindSize);
            var exportData = new MachLinkEditData(stream, dyldInfoHeader.ExportOffset, dyldInfoHeader.ExportSize);

            if (loadCommandType == MachLoadCommandType.DyldInfo)
            {
                return new MachDyldInfo(objectFile, rebaseData, bindData, weakBindData, lazyBindData, exportData);
            }

            return new MachDyldInfoOnly(objectFile, rebaseData, bindData, weakBindData, lazyBindData, exportData);
        }

        private static MachTwoLevelHints ReadTwoLevelHints(ReadOnlySpan<byte> loadCommandPtr, MachObjectFile objectFile, Stream stream)
        {
            var twoLevelHintsHeader = TwoLevelHintsHeader.Read(loadCommandPtr.Slice(LoadCommandHeader.BinarySize), objectFile.IsLittleEndian, out var _);

            return new MachTwoLevelHints(objectFile,
                new MachLinkEditData(
                    stream,
                    twoLevelHintsHeader.FileOffset,
                    twoLevelHintsHeader.NumberOfHints * sizeof(uint)));
        }

        private static MachUuid ReadUuid(ReadOnlySpan<byte> loadCommandPtr)
        {
            return new MachUuid()
            {
                Uuid = new Guid(loadCommandPtr.Slice(LoadCommandHeader.BinarySize, 16).ToArray()),
            };
        }

        private static MachSourceVersion ReadSourceVersion(ReadOnlySpan<byte> loadCommandPtr, bool isLittleEndian)
            => MachSourceVersion.Read(loadCommandPtr.Slice(LoadCommandHeader.BinarySize), isLittleEndian, out int _);

        private static MachEncryptionInfo ReadEncryptionInfo64(ReadOnlySpan<byte> loadCommandPtr, bool isLittleEndian)
            => MachEncryptionInfo.Read(loadCommandPtr.Slice(LoadCommandHeader.BinarySize), isLittleEndian, out int _);

        private static MachRunPath ReadRunPath(ReadOnlySpan<byte> loadCommandPtr, bool isLittleEndian)
        {
            var offset =
                isLittleEndian
                ? BinaryPrimitives.ReadUInt32LittleEndian(loadCommandPtr.Slice(LoadCommandHeader.BinarySize))
                : BinaryPrimitives.ReadUInt32BigEndian(loadCommandPtr.Slice(LoadCommandHeader.BinarySize));

            var nameSlice = loadCommandPtr.Slice((int)offset);
            int zeroIndex = nameSlice.IndexOf((byte)0);
            string name = zeroIndex >= 0 ? Encoding.UTF8.GetString(nameSlice.Slice(0, zeroIndex).ToArray()) : Encoding.UTF8.GetString(nameSlice.ToArray());

            return new MachRunPath()
            {
                RunPath = name,
            };
        }

        private static MachLoadDylinkerCommand ReadLoadDylinker(ReadOnlySpan<byte> loadCommandPtr, bool isLittleEndian)
        {
            var offset =
                isLittleEndian
                ? BinaryPrimitives.ReadUInt32LittleEndian(loadCommandPtr.Slice(LoadCommandHeader.BinarySize))
                : BinaryPrimitives.ReadUInt32BigEndian(loadCommandPtr.Slice(LoadCommandHeader.BinarySize));

            var nameSlice = loadCommandPtr.Slice((int)offset);
            int zeroIndex = nameSlice.IndexOf((byte)0);
            string name = zeroIndex >= 0 ? Encoding.UTF8.GetString(nameSlice.Slice(0, zeroIndex).ToArray()) : Encoding.UTF8.GetString(nameSlice.ToArray());

            return new MachLoadDylinkerCommand()
            {
                Name = name,
            };
        }

        private static MachObjectFile ReadSingle(FatArchHeader? fatArchHeader, MachMagic magic, Stream stream)
        {
            Span<byte> headerBuffer = stackalloc byte[Math.Max(MachHeader.BinarySize, MachHeader64.BinarySize)];
            MachObjectFile objectFile;
            IMachHeader machHeader;
            bool isLittleEndian;

            switch (magic)
            {
                case MachMagic.MachHeaderLittleEndian:
                case MachMagic.MachHeaderBigEndian:
                    stream.ReadFully(headerBuffer.Slice(0, MachHeader.BinarySize));
                    isLittleEndian = magic == MachMagic.MachHeaderLittleEndian;
                    machHeader = MachHeader.Read(headerBuffer, isLittleEndian, out var _);
                    Debug.Assert(!machHeader.CpuType.HasFlag(MachCpuType.Architecture64));
                    objectFile = new MachObjectFile(stream);
                    break;

                case MachMagic.MachHeader64LittleEndian:
                case MachMagic.MachHeader64BigEndian:
                    stream.ReadFully(headerBuffer.Slice(0, MachHeader64.BinarySize));
                    isLittleEndian = magic == MachMagic.MachHeader64LittleEndian;
                    machHeader = MachHeader64.Read(headerBuffer, isLittleEndian, out var _);
                    Debug.Assert(machHeader.CpuType.HasFlag(MachCpuType.Architecture64));
                    objectFile = new MachObjectFile(stream);
                    break;

                default:
                    throw new NotSupportedException();
            }

            objectFile.IsLittleEndian = isLittleEndian;
            objectFile.CpuType = machHeader.CpuType;
            objectFile.CpuSubType = machHeader.CpuSubType;
            objectFile.FileType = machHeader.FileType;
            objectFile.Flags = machHeader.Flags;

            // Read load commands
            //
            // Mach-O uses the load command both to describe the segments/sections and content
            // within them. The commands, like "Code Signature" overlap with the segments. For
            // code signature in particular it will overlap with the LINKEDIT segment.
            var loadCommands = new byte[machHeader.SizeOfCommands];
            Span<byte> loadCommandPtr = loadCommands;
            stream.ReadFully(loadCommands);
            for (int i = 0; i < machHeader.NumberOfCommands; i++)
            {
                var loadCommandHeader = LoadCommandHeader.Read(loadCommandPtr, isLittleEndian, out var _);
                objectFile.LoadCommands.Add(loadCommandHeader.CommandType switch
                {
                    MachLoadCommandType.Segment => ReadSegment(loadCommandPtr, objectFile, stream),
                    MachLoadCommandType.Segment64 => ReadSegment64(loadCommandPtr, objectFile, stream),
                    MachLoadCommandType.CodeSignature => new MachCodeSignature(objectFile, ReadLinkEdit(loadCommandPtr, objectFile, stream)),
                    MachLoadCommandType.DylibCodeSigningDRs => new MachDylibCodeSigningDirs(objectFile, ReadLinkEdit(loadCommandPtr, objectFile, stream)),
                    MachLoadCommandType.SegmentSplitInfo => new MachSegmentSplitInfo(objectFile, ReadLinkEdit(loadCommandPtr, objectFile, stream)),
                    MachLoadCommandType.FunctionStarts => new MachFunctionStarts(objectFile, ReadLinkEdit(loadCommandPtr, objectFile, stream)),
                    MachLoadCommandType.DataInCode => new MachDataInCode(objectFile, ReadLinkEdit(loadCommandPtr, objectFile, stream)),
                    MachLoadCommandType.LinkerOptimizationHint => new MachLinkerOptimizationHint(objectFile, ReadLinkEdit(loadCommandPtr, objectFile, stream)),
                    MachLoadCommandType.DyldExportsTrie => new MachDyldExportsTrie(objectFile, ReadLinkEdit(loadCommandPtr, objectFile, stream)),
                    MachLoadCommandType.DyldChainedFixups => new MachDyldChainedFixups(objectFile, ReadLinkEdit(loadCommandPtr, objectFile, stream)),
                    MachLoadCommandType.LoadDylib => ReadDylibCommand<MachLoadDylibCommand>(loadCommandPtr, loadCommandHeader.CommandSize, isLittleEndian),
                    MachLoadCommandType.LoadWeakDylib => ReadDylibCommand<MachLoadWeakDylibCommand>(loadCommandPtr, loadCommandHeader.CommandSize, isLittleEndian),
                    MachLoadCommandType.ReexportDylib => ReadDylibCommand<MachReexportDylibCommand>(loadCommandPtr, loadCommandHeader.CommandSize, isLittleEndian),
                    MachLoadCommandType.Main => ReadMainCommand(loadCommandPtr, isLittleEndian),
                    MachLoadCommandType.VersionMinMacOS => ReadVersionMinCommand<MachVersionMinMacOS>(loadCommandPtr, isLittleEndian),
                    MachLoadCommandType.VersionMinIPhoneOS => ReadVersionMinCommand<MachVersionMinIOS>(loadCommandPtr, isLittleEndian),
                    MachLoadCommandType.VersionMinTvOS => ReadVersionMinCommand<MachVersionMinTvOS>(loadCommandPtr, isLittleEndian),
                    MachLoadCommandType.VersionMinWatchOS => ReadVersionMinCommand<MachVersionMinWatchOS>(loadCommandPtr, isLittleEndian),
                    MachLoadCommandType.BuildVersion => ReadBuildVersion(loadCommandPtr, isLittleEndian),
                    MachLoadCommandType.SymbolTable => ReadSymbolTable(loadCommandPtr, objectFile, stream),
                    MachLoadCommandType.DynamicLinkEditSymbolTable => ReadDynamicLinkEditSymbolTable(loadCommandPtr, objectFile, stream),
                    MachLoadCommandType.DyldInfo => ReadDyldInfo(loadCommandHeader.CommandType, loadCommandPtr, objectFile, stream),
                    MachLoadCommandType.DyldInfoOnly => ReadDyldInfo(loadCommandHeader.CommandType, loadCommandPtr, objectFile, stream),
                    MachLoadCommandType.TowLevelHints => ReadTwoLevelHints(loadCommandPtr, objectFile, stream),
                    MachLoadCommandType.Uuid => ReadUuid(loadCommandPtr),
                    MachLoadCommandType.SourceVersion => ReadSourceVersion(loadCommandPtr, objectFile.IsLittleEndian),
                    MachLoadCommandType.EncryptionInfo64 => ReadEncryptionInfo64(loadCommandPtr, objectFile.IsLittleEndian),
                    MachLoadCommandType.Rpath => ReadRunPath(loadCommandPtr, objectFile.IsLittleEndian),
                    MachLoadCommandType.LoadDylinker => ReadLoadDylinker(loadCommandPtr, objectFile.IsLittleEndian),
                    _ => new MachCustomLoadCommand(loadCommandHeader.CommandType, loadCommandPtr.Slice(LoadCommandHeader.BinarySize, (int)loadCommandHeader.CommandSize - LoadCommandHeader.BinarySize).ToArray()),
                });
                loadCommandPtr = loadCommandPtr.Slice((int)loadCommandHeader.CommandSize);
            }

            return objectFile;
        }

        private static bool TryReadMachMagic(Stream stream, out MachMagic machMagic)
        {
            if (stream is null)
                throw new ArgumentNullException(nameof(stream));

            Span<byte> magicBuffer = stackalloc byte[4];
            stream.Position = 0;
            stream.ReadFully(magicBuffer);

            machMagic = (MachMagic)BinaryPrimitives.ReadUInt32BigEndian(magicBuffer);
            return Enum.IsDefined(typeof(MachMagic), machMagic);
        }

        private static MachMagic ReadMagic(Stream stream)
        {
            if (!TryReadMachMagic(stream, out var magic))
            {
                throw new InvalidDataException("The archive is not a valid MACH object.");
            }
            return magic;
        }

        public static bool IsFatMach(Stream stream)
        {
            var magic = ReadMagic(stream);
            return magic == MachMagic.FatMagicLittleEndian || magic == MachMagic.FatMagicBigEndian;
        }

        public static bool IsMachOImage(Stream stream)
        {
            try
            {
                bool isMach = TryReadMachMagic(stream, out _);
                return isMach;
            }
            catch (EndOfStreamException)
            {
                return false;
            }
        }

        public static bool IsMachOImage(string filePath)
        {
            using var stream = File.OpenRead(filePath);
            return IsMachOImage(stream);
        }

        public static IList<MachObjectFile> Read(Stream stream)
        {
            var magic = ReadMagic(stream);
            var magicBuffer = new byte[4];

            List<MachObjectFile> values = new List<MachObjectFile>();

            if (magic == MachMagic.FatMagicLittleEndian || magic == MachMagic.FatMagicBigEndian)
            {
                var headerBuffer = new byte[Math.Max(FatHeader.BinarySize, FatArchHeader.BinarySize)];
                stream.ReadFully(headerBuffer.AsSpan(0, FatHeader.BinarySize));
                var fatHeader = FatHeader.Read(headerBuffer, isLittleEndian: magic == MachMagic.FatMagicLittleEndian, out var _);
                for (int i = 0; i < fatHeader.NumberOfFatArchitectures; i++)
                {
                    stream.ReadFully(headerBuffer.AsSpan(0, FatArchHeader.BinarySize));
                    var fatArchHeader = FatArchHeader.Read(headerBuffer, isLittleEndian: magic == MachMagic.FatMagicLittleEndian, out var _);

                    var machOSlice = stream.Slice(fatArchHeader.Offset, fatArchHeader.Size);
                    machOSlice.ReadFully(magicBuffer);
                    magic = (MachMagic)BinaryPrimitives.ReadUInt32BigEndian(magicBuffer);
                    values.Add(ReadSingle(fatArchHeader, magic, machOSlice));
                }
            }
            else
            {
                values.Add(ReadSingle(null, magic, stream));
            }

            return values;
        }
    }
}
