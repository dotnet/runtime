// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

namespace Microsoft.NET.HostModel.MachO;

/// <summary>
/// A managed object containing relevant information for AdHoc signing a Mach-O file.
/// The object is created from a memory mapped file, and a signature can be calculated from the memory mapped file.
/// However, since a memory mapped file cannot be extended, the signature is written to a file stream.
/// </summary>
internal class MachObjectFile
{
    internal const uint SpecialSlotCount = 2;
    internal const uint PageSize = 4096;
    internal const byte Log2PageSize = 12;
    internal const byte DefaultHashSize = 32;
    internal const HashType DefaultHashType = HashType.SHA256;
    internal static IncrementalHash GetDefaultIncrementalHash() => IncrementalHash.CreateHash(HashAlgorithmName.SHA256);

    private MachHeader _header;
    private (LinkEditCommand Command, long FileOffset) _codeSignatureLC;
    private readonly (Segment64LoadCommand Command, long FileOffset) _textSegment64;
    private (Segment64LoadCommand Command, long FileOffset) _linkEditSegment64;
    private CodeSignature _codeSignatureBlob;
    /// <summary>
    /// The offset of the lowest section in the object file. This is to ensure that additional load commands do not overwrite sections.
    /// </summary>
    private readonly long _lowestSectionOffset;
    /// <summary>
    /// The offset in the object file where the next additional load command should be written.
    /// </summary>
    private readonly long _nextCommandPtr;

    private MachObjectFile(
        MachHeader header,
        (LinkEditCommand Command, long FileOffset) codeSignatureLC,
        (Segment64LoadCommand Command, long FileOffset) textSegment64,
        (Segment64LoadCommand Command, long FileOffset) linkEditSegment64,
        long lowestSection,
        CodeSignature codeSignatureBlob,
        long nextCommandPtr)
    {
        _codeSignatureBlob = codeSignatureBlob;
        _header = header;
        _codeSignatureLC = codeSignatureLC;
        _textSegment64 = textSegment64;
        _linkEditSegment64 = linkEditSegment64;
        _lowestSectionOffset = lowestSection;
        _nextCommandPtr = nextCommandPtr;
    }

    /// <summary>
    /// Reads the information from a memory mapped Mach-O file and creates a <see cref="MachObjectFile"/> that represents it.
    /// </summary>
    public static MachObjectFile Create(MemoryMappedViewAccessor file)
    {
        long commandsPtr = 0;
        file.Read(commandsPtr, out MachHeader header);
        long nextCommandPtr = ReadCommands(
            file,
            in header,
            out (LinkEditCommand Command, long FileOffset) codeSignatureLC,
            out (Segment64LoadCommand Command, long FileOffset) textSegment64,
            out (Segment64LoadCommand Command, long FileOffset) linkEditSegment64,
            out long lowestSection);
        CodeSignature codeSignatureBlob = null;
        if (!codeSignatureLC.Command.IsDefault)
            codeSignatureBlob = CodeSignature.Read(file, codeSignatureLC.Command.GetDataOffset(header));
        return new MachObjectFile(
            header,
            codeSignatureLC,
            textSegment64,
            linkEditSegment64,
            lowestSection,
            codeSignatureBlob,
            nextCommandPtr);
    }

    /// <summary>
    /// Returns true if the file has a code signature load command.
    /// </summary>
    public bool HasSignature => !_codeSignatureLC.Command.IsDefault;

    /// <summary>
    /// Adds or replaces the code signature load command and modifies the __LINKEDIT segment size to accomodate the signature.
    /// Calculates the signature from the file and returns the offset to the start of the signature.
    /// Since memory mapped files cannot be extended, this does not write the signature to the file.
    /// </summary>
    /// <remarks>
    /// Use <see cref="WriteCodeSignature(FileStream)"/> to write the signature to a file.
    /// </remarks>
    public long CreateAdHocSignature(MemoryMappedViewAccessor file, string identifier)
    {
        AllocateCodeSignatureLC(identifier);
        WriteLoadCommands(file);
        _codeSignatureBlob = CreateSignature(file, identifier);
        _codeSignatureBlob.WriteToFile(file);
        return GetFileSize();
    }

    /// <summary>
    /// Writes the embedded signature blob to the stream at the current offset.
    /// </summary>
    public void WriteCodeSignature(FileStream stream)
    {
        if (_codeSignatureBlob is null)
            throw new InvalidDataException("Code signature blob is missing");
        _codeSignatureBlob.WriteToStream(stream);
    }

    /// <summary>
    /// Writes the entire file to <paramref name="file"/>.
    /// Should not be called if the object file requires more space than the capacity of <paramref name="file"/>.
    /// </summary>
    public long Write(MemoryMappedViewAccessor file)
    {
        if (file.Capacity < GetFileSize())
            throw new ArgumentException("File is too small", nameof(file));

        file.Write(0, ref _header);

        file.Write(_linkEditSegment64.FileOffset, ref _linkEditSegment64.Command);

        if (!_codeSignatureLC.Command.IsDefault)
        {
            file.Write(_codeSignatureLC.FileOffset, ref _codeSignatureLC.Command);
            if (_codeSignatureBlob is null)
                throw new InvalidDataException("Code signature blob is missing");
            _codeSignatureBlob.WriteToFile(file);
        }
        return GetFileSize();
    }

    public static bool IsMachOImage(MemoryMappedViewAccessor memoryMappedViewAccessor)
    {
        memoryMappedViewAccessor.Read(0, out MachMagic magic);
        return magic is MachMagic.MachHeaderCurrentEndian or MachMagic.MachHeaderOppositeEndian
            or MachMagic.MachHeader64CurrentEndian or MachMagic.MachHeader64OppositeEndian
            or MachMagic.FatMagicCurrentEndian or MachMagic.FatMagicOppositeEndian;
    }

    public static bool IsMachOImage(string filePath)
    {
        using (BinaryReader reader = new BinaryReader(File.OpenRead(filePath)))
        {
            if (reader.BaseStream.Length < 256) // Header size
            {
                return false;
            }
            uint magic = reader.ReadUInt32();
            return Enum.IsDefined(typeof(MachMagic), magic);
        }
    }

    /// <summary>
    /// Removes the code signature load command and signature blob from the file if present.
    /// Returns true and sets <paramref name="newLength"/> to a non-null value if the file is a MachO file and the signature was removed.
    /// Returns false and sets newLength to null otherwise.
    /// </summary>
    /// <param name="memoryMappedViewAccessor">The file to remove the signature from.</param>
    /// <param name="newLength">The new length of the file if the signature is remove and the method returns true</param>
    /// <returns></returns>
    public static bool TryRemoveCodesign(MemoryMappedViewAccessor memoryMappedViewAccessor, out long? newLength)
    {
        newLength = null;
        if (!IsMachOImage(memoryMappedViewAccessor))
            return false;

        MachObjectFile machFile = Create(memoryMappedViewAccessor);
        if (machFile._codeSignatureLC.Command.IsDefault)
            return false;

        machFile._header.NumberOfCommands -= 1;
        machFile._header.SizeOfCommands -= (uint)Marshal.SizeOf<LinkEditCommand>();
        machFile._linkEditSegment64.Command.SetFileSize(
            machFile._linkEditSegment64.Command.GetFileSize(machFile._header)
                - machFile._codeSignatureLC.Command.GetFileSize(machFile._header),
            machFile._header);
        newLength = machFile.GetFileSize();
        machFile._codeSignatureLC = default;
        machFile.Write(memoryMappedViewAccessor);
        return true;
    }

    /// <summary>
    /// Returns true if the two signed MachObjectFiles are equivalent.
    /// Since the entire file isn't store in the object, the code signature is required.
    /// The __LINKEDIT segment size is allowed to be different since codesign adds additional padding at the end.
    /// The difference in __LINKEDIT size causes the first page hash to be different, so the first code hash is ignored.
    /// </summary>
    public static bool AreEquivalent(MachObjectFile a, MachObjectFile b)
    {
        if (!a._header.Equals(b._header))
            return false;
        if (!CodeSignatureLCsAreEquivalent(a._codeSignatureLC, b._codeSignatureLC, a._header))
            return false;
        if (!a._textSegment64.Equals(b._textSegment64))
            return false;
        if (!LinkEditSegmentsAreEquivalent(a._linkEditSegment64, b._linkEditSegment64, a._header))
            return false;
        if (a._codeSignatureBlob is null || b._codeSignatureBlob is null)
            return false;
        // This may be false if the __LINKEDIT segment load command is not on the first page, but that is unlikely.
        if (!CodeSignature.AreEquivalent(a._codeSignatureBlob, b._codeSignatureBlob))
            return false;

        return true;

        static bool CodeSignatureLCsAreEquivalent((LinkEditCommand Command, long FileOffset) a, (LinkEditCommand Command, long FileOffset) b, MachHeader header)
        {
            if (a.Command.GetDataOffset(header) != b.Command.GetDataOffset(header))
                return false;
            if (a.FileOffset != b.FileOffset)
                return false;
            return true;
        }

        static bool LinkEditSegmentsAreEquivalent((Segment64LoadCommand Command, long FileOffset) a, (Segment64LoadCommand Command, long FileOffset) b, MachHeader header)
        {
            if (a.Command.GetFileOffset(header) != b.Command.GetFileOffset(header))
                return false;
            if (a.Command.GetSectionsCount(header) != b.Command.GetSectionsCount(header))
                return false;
            if (a.FileOffset != b.FileOffset)
                return false;
            return true;
        }
    }

    /// <summary>
    /// Writes the current load commands to <paramref name="file"/>.
    /// </summary>
    private void WriteLoadCommands(MemoryMappedViewAccessor file)
    {
        file.Write(0, ref _header);
        file.Write(_linkEditSegment64.FileOffset, ref _linkEditSegment64.Command);
        if (_codeSignatureLC.Command.IsDefault)
            throw new InvalidOperationException("Load commands must be written after the code signature load command is allocated");
        file.Write(_codeSignatureLC.FileOffset, ref _codeSignatureLC.Command);
    }

    /// <summary>
    /// Returns a pointer to the end of the commands list.
    /// Fills the content of the commands with the corresponding command if present in the file.
    /// </summary>
    private static long ReadCommands(
        MemoryMappedViewAccessor inputFile,
        in MachHeader header,
        out (LinkEditCommand Command, long FileOffset) codeSignatureLC,
        out (Segment64LoadCommand Command, long FileOffset) textSegment64,
        out (Segment64LoadCommand Command, long FileOffset) linkEditSegment64,
        out long lowestSectionOffset)
    {
        codeSignatureLC = default;
        textSegment64 = default;
        linkEditSegment64 = default;
        long commandsPtr = Marshal.SizeOf<MachHeader>();
        if (!header.Is64Bit)
            throw new InvalidDataException("Only 64-bit Mach-O files are supported");
        lowestSectionOffset = long.MaxValue;
        for (int i = 0; i < header.NumberOfCommands; i++)
        {
            inputFile.Read(commandsPtr, out LoadCommand loadCommand);
            switch (loadCommand.GetCommandType(header))
            {
                case MachLoadCommandType.CodeSignature:
                    inputFile.Read(commandsPtr, out LinkEditCommand leCommand);
                    codeSignatureLC = (leCommand, commandsPtr);
                    break;
                case MachLoadCommandType.Segment64:
                    inputFile.Read(commandsPtr, out Segment64LoadCommand segment64);
                    if (segment64.Name.Equals(NameBuffer.__TEXT))
                    {
                        textSegment64 = (segment64, commandsPtr);
                        var sectionPtr = commandsPtr + Marshal.SizeOf<Segment64LoadCommand>();
                        var sectionsCount = segment64.GetSectionsCount(header);
                        for (int s = 0; s < sectionsCount; s++)
                        {
                            inputFile.Read(sectionPtr, out Section64LoadCommand section);
                            lowestSectionOffset = Math.Min(lowestSectionOffset, section.GetFileOffset(header));
                            sectionPtr += Marshal.SizeOf<Section64LoadCommand>();
                        }
                        break;
                    }
                    if (segment64.Name.Equals(NameBuffer.__LINKEDIT))
                    {
                        linkEditSegment64 = (segment64, commandsPtr);
                        break;
                    }
                    break;
            }
            commandsPtr += loadCommand.GetCommandSize(header);
        }
        return commandsPtr;
    }

    /// <summary>
    /// Clears the old signature and sets the codeSignatureLC to the proper size and offset for a new signature.
    /// </summary>
    private void AllocateCodeSignatureLC(string identifier)
    {
        uint csOffset = GetSignatureStart();
        uint csPtr = (uint)(_codeSignatureLC.Command.IsDefault ? _nextCommandPtr : _codeSignatureLC.FileOffset);
        uint csSize = GetCodeSignatureSize(identifier);

        if (_codeSignatureLC.Command.IsDefault)
        {
            // Update the header to accomodate the new code signature load command
            _header.NumberOfCommands += 1;
            _header.SizeOfCommands += (uint)Marshal.SizeOf<LinkEditCommand>();
            if (_header.SizeOfCommands > _lowestSectionOffset)
            {
                throw new NotImplementedException("Mach Object does not have enough space for the code signature load command");
            }
        }

        var currentLinkEditOffset = _linkEditSegment64.Command.GetFileOffset(_header);
        var linkEditSize = csOffset + csSize - currentLinkEditOffset;
        _linkEditSegment64.Command.SetFileSize(linkEditSize, _header);
        _codeSignatureLC = (new LinkEditCommand(MachLoadCommandType.CodeSignature, csOffset, csSize, _header), csPtr);
    }

    /// <summary>
    /// Creates a new code signature from the file.
    /// The signature is composed of an Embedded Signature Superblob header, followed by a CodeDirectory blob, a Requirements blob, and a CMS blob.
    /// The codesign tool also adds an empty Requirements blob and an empty CMS blob, which are not strictly required but are added here for compatibility.
    /// </summary>
    private CodeSignature CreateSignature(MemoryMappedViewAccessor file, string identifier)
    {
        EmbeddedSignatureHeader embeddedSignature = new();
        CodeDirectoryHeader codeDirectory = CreateCodeDirectoryHeader(identifier);
        RequirementsBlob requirementsBlob = RequirementsBlob.Empty;
        CmsWrapperBlob cmsWrapperBlob = CmsWrapperBlob.Empty;

        byte[] identifierBytes = new byte[GetIdentifierLength(identifier)];
        Encoding.UTF8.GetBytes(identifier).CopyTo(identifierBytes, 0);

        byte[] cdHashes = new byte[(GetCodeSlotCount() + SpecialSlotCount) * DefaultHashSize];

        // Fill in the CodeDirectory hashes
        {
            var hasher = GetDefaultIncrementalHash();

            // Special slot hashes
            int hashSlotsOffset = 0;
            // -2 is the requirements blob hash
            hasher.AppendData(requirementsBlob.GetBytes());
            byte[] hash = hasher.GetHashAndReset();
            Debug.Assert(hash.Length == DefaultHashSize);
            hash.CopyTo(cdHashes, hashSlotsOffset);
            hashSlotsOffset += DefaultHashSize;
            // -1 is the CMS blob hash (which is empty)
            hashSlotsOffset += DefaultHashSize;

            // 0 - N are Code hashes
            byte[] pageBuffer = new byte[(int)PageSize];
            long remaining = GetSignatureStart();
            long buffptr = 0;
            while (remaining > 0)
            {
                int codePageSize = (int)Math.Min(remaining, 4096);
                int bytesRead = file.ReadArray(buffptr, pageBuffer, 0, codePageSize);
                if (bytesRead != codePageSize)
                    throw new IOException("Could not read all bytes");
                buffptr += bytesRead;
                hasher.AppendData(pageBuffer, 0, codePageSize);
                hash = hasher.GetHashAndReset();
                Debug.Assert(hash.Length == DefaultHashSize);
                hash.CopyTo(cdHashes, hashSlotsOffset);
                remaining -= codePageSize;
                hashSlotsOffset += DefaultHashSize;
            }
        }

        // Create Embedded Signature Header
        {
            embeddedSignature.Size = GetCodeSignatureSize(identifier);
            embeddedSignature.CodeDirectory = new BlobIndex(
                CodeDirectorySpecialSlot.CodeDirectory,
                (uint)Marshal.SizeOf<EmbeddedSignatureHeader>());
            embeddedSignature.Requirements = new BlobIndex(
                CodeDirectorySpecialSlot.Requirements,
                (uint)Marshal.SizeOf<EmbeddedSignatureHeader>()
                    + GetCodeDirectorySize(identifier));
            embeddedSignature.CmsWrapper = new BlobIndex(
                CodeDirectorySpecialSlot.CmsWrapper,
                (uint)Marshal.SizeOf<EmbeddedSignatureHeader>()
                    + GetCodeDirectorySize(identifier)
                    + (uint)Marshal.SizeOf<RequirementsBlob>());
        }

        return CodeSignature.Create(
            GetSignatureStart(),
            embeddedSignature,
            codeDirectory,
            identifierBytes,
            cdHashes,
            requirementsBlob,
            cmsWrapperBlob);
    }

    private CodeDirectoryHeader CreateCodeDirectoryHeader(string identifier)
    {
        CodeDirectoryVersion version = CodeDirectoryVersion.HighestVersion;
        uint identifierLength = GetIdentifierLength(identifier);
        uint codeDirectorySize = GetCodeDirectorySize(identifier);

        CodeDirectoryHeader codeDirectoryBlob = new();
        uint hashesOffset = (uint)Marshal.SizeOf<CodeDirectoryHeader>() + identifierLength + DefaultHashSize * SpecialSlotCount;
        codeDirectoryBlob.Size = codeDirectorySize;
        codeDirectoryBlob.Version = version;
        codeDirectoryBlob.Flags = CodeDirectoryFlags.Adhoc;
        codeDirectoryBlob.HashesOffset = hashesOffset;
        codeDirectoryBlob.IdentifierOffset = (uint)Marshal.SizeOf<CodeDirectoryHeader>();
        codeDirectoryBlob.SpecialSlotCount = SpecialSlotCount;
        codeDirectoryBlob.CodeSlotCount = GetCodeSlotCount();
        codeDirectoryBlob.ExecutableLength = GetSignatureStart() > uint.MaxValue ? uint.MaxValue : GetSignatureStart();
        codeDirectoryBlob.HashSize = DefaultHashSize;
        codeDirectoryBlob.HashType = DefaultHashType;
        codeDirectoryBlob.Platform = 0;
        codeDirectoryBlob.Log2PageSize = Log2PageSize;

        codeDirectoryBlob.CodeLimit64 = GetSignatureStart() >= uint.MaxValue ? GetSignatureStart() : 0;
        codeDirectoryBlob.ExecSegmentBase = _textSegment64.Command.GetFileOffset(_header);
        codeDirectoryBlob.ExecSegmentLimit = _textSegment64.Command.GetFileSize(_header);
        if (_header.FileType == MachFileType.Execute)
            codeDirectoryBlob.ExecSegmentFlags |= ExecutableSegmentFlags.MainBinary;

        return codeDirectoryBlob;
    }

    /// <summary>
    /// Gets the total size of the Mach-O file according to the load commands.
    /// </summary>
    private long GetFileSize()
        => (long)(_linkEditSegment64.Command.GetFileOffset(_header) + _linkEditSegment64.Command.GetFileSize(_header));

    private static uint GetIdentifierLength(string identifier)
    {
        return (uint)(Encoding.UTF8.GetByteCount(identifier) + 1);
    }

    private uint GetCodeDirectorySize(string identifier) => GetCodeDirectorySize(GetSignatureStart(), identifier);
    private static uint GetCodeDirectorySize(uint signatureStart, string identifier)
    {
        return (uint)(Marshal.SizeOf<CodeDirectoryHeader>()
            + GetIdentifierLength(identifier)
            + SpecialSlotCount * DefaultHashSize
            + GetCodeSlotCount(signatureStart) * DefaultHashSize);
    }

    private uint GetCodeSignatureSize(string identifier) => GetCodeSignatureSize(GetSignatureStart(), identifier);
    private static uint GetCodeSignatureSize(uint signatureStart, string identifier)
    {
        return (uint)(Marshal.SizeOf<EmbeddedSignatureHeader>()
            + GetCodeDirectorySize(signatureStart, identifier)
            + Marshal.SizeOf<RequirementsBlob>()
            + Marshal.SizeOf<CmsWrapperBlob>());
    }

    private uint GetSignatureStart()
    {
        if (!_codeSignatureLC.Command.IsDefault)
        {
            return _codeSignatureLC.Command.GetDataOffset(_header);
        }
        return (uint)(_linkEditSegment64.Command.GetFileOffset(_header) + _linkEditSegment64.Command.GetFileSize(_header));
    }

    private uint GetCodeSlotCount() => GetCodeSlotCount(GetSignatureStart());
    private static uint GetCodeSlotCount(uint signatureStart)
    {
        return (signatureStart + PageSize - 1) / PageSize;
    }

    internal static long GetSignatureSizeEstimate(uint fileSize, string identifier)
    {
        return GetCodeSignatureSize(fileSize, identifier);
    }
}
