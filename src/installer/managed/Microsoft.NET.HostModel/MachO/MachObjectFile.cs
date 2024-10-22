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
    private readonly (SegmentLoadCommand Command, long FileOffset) _textSegment;
    private readonly (Segment64LoadCommand Command, long FileOffset) _textSegment64;
    private (SegmentLoadCommand Command, long FileOffset) _linkEditSegment;
    private (Segment64LoadCommand Command, long FileOffset) _linkEditSegment64;
    private readonly long _lowestSection;
    private readonly string _identifier;
    private CodeSignature _codeSignatureBlob;
    private readonly long _nextCommandPtr;

    /// <summary>
    /// Creates a new MachObjectFile from a memory mapped file.
    /// </summary>
    public MachObjectFile(MemoryMappedViewAccessor file, string identifier)
    {
        long commandsPtr = 0;
        file.Read(commandsPtr, out _header);
        _nextCommandPtr = ReadCommands(file, in _header, out _codeSignatureLC, out _textSegment, out _textSegment64, out _linkEditSegment, out _linkEditSegment64, out _lowestSection);
        Debug.Assert(_linkEditSegment.Command.IsDefault ^ _linkEditSegment64.Command.IsDefault);
        Debug.Assert(_textSegment.Command.IsDefault ^ _textSegment64.Command.IsDefault);
        this._identifier = identifier;
        if (!_codeSignatureLC.Command.IsDefault)
            _codeSignatureBlob = CodeSignature.Read(file, _codeSignatureLC.Command.GetDataOffset(_header));
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
        return GetSignatureStart();
    }

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

        if (_linkEditSegment.Command.IsDefault)
            file.Write(_linkEditSegment64.FileOffset, ref _linkEditSegment64.Command);
        else
            file.Write(_linkEditSegment.FileOffset, ref _linkEditSegment.Command);

        if (!_codeSignatureLC.Command.IsDefault)
        {
            file.Write(_codeSignatureLC.FileOffset, ref _codeSignatureLC.Command);
            if (_codeSignatureBlob is null)
                throw new InvalidDataException("Code signature blob is missing");
            _codeSignatureBlob.WriteToFile(file);
        }
        return GetFileSize();
    }

    public static bool IsMachFile(MemoryMappedViewAccessor memoryMappedViewAccessor)
    {
        memoryMappedViewAccessor.Read(0, out MachMagic magic);
        return magic is MachMagic.MachHeaderCurrentEndian or MachMagic.MachHeaderOppositeEndian
            or MachMagic.MachHeader64CurrentEndian or MachMagic.MachHeader64OppositeEndian
            or MachMagic.FatMagicCurrentEndian or MachMagic.FatMagicOppositeEndian;
    }

    public static bool TryRemoveCodesign(MemoryMappedViewAccessor memoryMappedViewAccessor, out long? newLength)
    {
        newLength = null;
        if (!IsMachFile(memoryMappedViewAccessor))
            return false;

        var machFile = new MachObjectFile(memoryMappedViewAccessor, "");
        if (machFile._codeSignatureLC.Command.IsDefault)
            return false;

        machFile._header.NumberOfCommands -= 1;
        machFile._header.SizeOfCommands -= (uint)Marshal.SizeOf<LinkEditCommand>();
        if (machFile._linkEditSegment.Command.IsDefault)
        {
            machFile._linkEditSegment64.Command.SetFileSize(
                machFile._linkEditSegment64.Command.GetFileSize(machFile._header)
                    - machFile._codeSignatureLC.Command.GetFileSize(machFile._header),
                machFile._header);
        }
        else
        {
            machFile._linkEditSegment.Command.SetFileSize(
                machFile._linkEditSegment.Command.GetFileSize(machFile._header)
                    - machFile._codeSignatureLC.Command.GetFileSize(machFile._header),
                machFile._header);
        }
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
        if (!a._textSegment.Equals(b._textSegment))
            return false;
        if (!a._textSegment64.Equals(b._textSegment64))
            return false;
        if (!LinkEditsAreEquivalent(a._linkEditSegment, b._linkEditSegment, a._header))
            return false;
        if (!LinkEditsAreEquivalent64(a._linkEditSegment64, b._linkEditSegment64, a._header))
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

        static bool LinkEditsAreEquivalent((SegmentLoadCommand Command, long FileOffset) a, (SegmentLoadCommand Command, long FileOffset) b, MachHeader header)
        {
            if (a.Command.GetFileOffset(header) != b.Command.GetFileOffset(header))
                return false;
            if (a.Command.GetSectionsCount(header) != b.Command.GetSectionsCount(header))
                return false;
            if (a.FileOffset != b.FileOffset)
                return false;
            return true;
        }

        static bool LinkEditsAreEquivalent64((Segment64LoadCommand Command, long FileOffset) a, (Segment64LoadCommand Command, long FileOffset) b, MachHeader header)
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

        if (_linkEditSegment.Command.IsDefault)
            file.Write(_linkEditSegment64.FileOffset, ref _linkEditSegment64.Command);
        else
            file.Write(_linkEditSegment.FileOffset, ref _linkEditSegment.Command);

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
        out (SegmentLoadCommand Command, long FileOffset) textSegment,
        out (Segment64LoadCommand Command, long FileOffset) textSegment64,
        out (SegmentLoadCommand Command, long FileOffset) linkEditSegment,
        out (Segment64LoadCommand Command, long FileOffset) linkEditSegment64,
        out long lowestSectionOffset)
    {
        codeSignatureLC = default;
        textSegment = default;
        textSegment64 = default;
        linkEditSegment = default;
        linkEditSegment64 = default;
        long commandsPtr = Marshal.SizeOf<MachHeader>();
        // Additional reserved field for 64 bit headers
        if (header.Is64Bit)
            commandsPtr += 4;
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
                case MachLoadCommandType.Segment:
                    inputFile.Read(commandsPtr, out SegmentLoadCommand segment);
                    if (segment.Name.Equals(NameBuffer.__TEXT))
                    {
                        textSegment = (segment, commandsPtr);
                        var sectionPtr = commandsPtr + Marshal.SizeOf<SegmentLoadCommand>();
                        var sectionCount = segment.GetSectionsCount(header);
                        for (int s = 0; s < sectionCount; s++)
                        {
                            inputFile.Read(sectionPtr, out SectionLoadCommand section);
                            lowestSectionOffset = Math.Min(lowestSectionOffset, section.GetFileOffset(header));
                            sectionPtr += Marshal.SizeOf<SectionLoadCommand>();
                        }
                        break;
                    }
                    if (segment.Name.Equals(NameBuffer.__LINKEDIT))
                    {
                        linkEditSegment = (segment, commandsPtr);
                        break;
                    }
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
            if (_header.SizeOfCommands > _lowestSection)
            {
                throw new NotImplementedException("Mach Object does not have enough space for the code signature load command");
            }
        }

        var currentLinkEditOffset = _linkEditSegment.Command.GetFileOffset(_header) + _linkEditSegment64.Command.GetFileOffset(_header);
        var linkEditSize = csOffset + csSize - currentLinkEditOffset;
        if (_linkEditSegment.Command.IsDefault)
            _linkEditSegment64.Command.SetFileSize(linkEditSize, _header);
        else
            _linkEditSegment.Command.SetFileSize((uint)linkEditSize, _header);
        _codeSignatureLC = (new LinkEditCommand(MachLoadCommandType.CodeSignature, csOffset, csSize, _header), csPtr);
    }

    /// <summary>
    /// Creates a new code signature from the file.
    /// The signature is composed of an Embedded Signature Superblob header, followed by a CodeDirectory blob, a Requirements blob, and a CMS blob.
    /// The codesign tool also adds an empty Requirements blob and an empty CMS blob, which are not strictly required but are added here for compatibility.
    /// </summary>
    private CodeSignature CreateSignature(MemoryMappedViewAccessor file, string identifier)
    {
        (EmbeddedSignatureHeader Header, long FileOffset) embeddedSignature;
        (CodeDirectoryHeader Header, long FileOffset) codeDirectory;
        (byte[] identifier, long FileOffset) identifierPtr;
        (byte[], long FileOffset) cdHashes;
        (RequirementsBlob Header, long FileOffset) requirementsBlob;
        (CmsWrapperBlob Header, long FileOffset) cmsWrapperBlob;

        long signaturePtr = GetSignatureStart();

        embeddedSignature = (default, signaturePtr);
        signaturePtr += Marshal.SizeOf<EmbeddedSignatureHeader>();

        codeDirectory = (default, signaturePtr);
        signaturePtr += Marshal.SizeOf<CodeDirectoryHeader>();

        identifierPtr = (default, signaturePtr);
        signaturePtr += GetIdentifierLength(identifier);

        cdHashes = (default, signaturePtr);
        signaturePtr += SpecialSlotCount * DefaultHashSize;
        signaturePtr += GetCodeSlotCount() * DefaultHashSize;

        requirementsBlob = (RequirementsBlob.Empty, signaturePtr);
        signaturePtr += Marshal.SizeOf<RequirementsBlob>();

        cmsWrapperBlob = (CmsWrapperBlob.Empty, signaturePtr);

        identifierPtr.identifier = new byte[GetIdentifierLength(identifier)];
        Encoding.UTF8.GetBytes(_identifier).CopyTo(identifierPtr.identifier, 0);

        // Create the CodeDirectory blob
        codeDirectory.Header = CreateCodeDirectoryHeader(identifier);
        cdHashes.Item1 = new byte[(GetCodeSlotCount() + SpecialSlotCount) * DefaultHashSize];

        // Fill in the CodeDirectory hashes
        {
            var hasher = GetDefaultIncrementalHash();

            // Special slot hashes
            int hashSlotsOffset = 0;
            // -2 is the requirements blob hash
            hasher.AppendData(requirementsBlob.Header.GetBytes());
            byte[] hash = hasher.GetHashAndReset();
            Debug.Assert(hash.Length == DefaultHashSize);
            hash.CopyTo(cdHashes.Item1, hashSlotsOffset);
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
                hash.CopyTo(cdHashes.Item1, hashSlotsOffset);
                remaining -= codePageSize;
                hashSlotsOffset += DefaultHashSize;
            }
        }

        // Create Embedded Signature Header
        {
            embeddedSignature.Header.Magic = BlobMagic.EmbeddedSignature;
            embeddedSignature.Header.BlobCount = 3u;
            embeddedSignature.Header.Size = GetCodeSignatureSize(identifier);
            embeddedSignature.Header.CodeDirectory = new BlobIndex(CodeDirectorySpecialSlot.CodeDirectory, (uint)(codeDirectory.FileOffset - embeddedSignature.FileOffset));
            embeddedSignature.Header.Requirements = new BlobIndex(CodeDirectorySpecialSlot.Requirements, (uint)(requirementsBlob.FileOffset - embeddedSignature.FileOffset));
            embeddedSignature.Header.CmsWrapper = new BlobIndex(CodeDirectorySpecialSlot.CmsWrapper, (uint)(cmsWrapperBlob.FileOffset - embeddedSignature.FileOffset));
        }

        return CodeSignature.Create(
            embeddedSignature,
            codeDirectory,
            identifierPtr,
            cdHashes,
            requirementsBlob,
            cmsWrapperBlob);
    }

    private CodeDirectoryHeader CreateCodeDirectoryHeader(string identifier)
    {
        CodeDirectoryVersion version = CodeDirectoryVersion.HighestVersion;
        uint identifierLength = GetIdentifierLength(identifier);
        uint codeDirectorySize = GetCodeDirectorySize(identifier);

        CodeDirectoryHeader codeDirectoryBlob = default;
        uint hashesOffset = (uint)Marshal.SizeOf<CodeDirectoryHeader>() + identifierLength + DefaultHashSize * SpecialSlotCount;
        codeDirectoryBlob.Magic = BlobMagic.CodeDirectory;
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

        codeDirectoryBlob.Reserved2 = 0;
        codeDirectoryBlob.CodeLimit64 = GetSignatureStart() >= uint.MaxValue ? GetSignatureStart() : 0;
        codeDirectoryBlob.ExecSegmentBase = _textSegment.Command.GetFileOffset(_header) + _textSegment64.Command.GetFileOffset(_header);
        codeDirectoryBlob.ExecSegmentLimit = _textSegment.Command.GetFileSize(_header) + _textSegment64.Command.GetFileSize(_header);
        if (_header.FileType == MachFileType.Execute)
            codeDirectoryBlob.ExecSegmentFlags |= ExecutableSegmentFlags.MainBinary;

        return codeDirectoryBlob;
    }

    /// <summary>
    /// Gets the total size of the Mach-O file according to the load commands.
    /// </summary>
    private long GetFileSize()
        => (long)(_linkEditSegment.Command.GetFileOffset(_header) + _linkEditSegment.Command.GetFileSize(_header)
            + _linkEditSegment64.Command.GetFileOffset(_header) + _linkEditSegment64.Command.GetFileSize(_header));

    private static uint GetIdentifierLength(string identifier)
    {
        return (uint)(Encoding.UTF8.GetByteCount(identifier) + 1);
    }

    private uint GetCodeDirectorySize(string identifier)
    {
        return (uint)(Marshal.SizeOf<CodeDirectoryHeader>()
            + GetIdentifierLength(identifier)
            + SpecialSlotCount * DefaultHashSize
            + GetCodeSlotCount() * DefaultHashSize);
    }

    private uint GetCodeSignatureSize(string identifier)
    {
        return (uint)(Marshal.SizeOf<EmbeddedSignatureHeader>()
            + GetCodeDirectorySize(identifier)
            + Marshal.SizeOf<RequirementsBlob>()
            + Marshal.SizeOf<CmsWrapperBlob>());
    }

    private uint GetSignatureStart()
    {
        if (!_codeSignatureLC.Command.IsDefault)
        {
            return _codeSignatureLC.Command.GetDataOffset(_header);
        }
        return (uint)(_linkEditSegment.Command.GetFileOffset(_header) + _linkEditSegment.Command.GetFileSize(_header)
            + _linkEditSegment64.Command.GetFileOffset(_header) + _linkEditSegment64.Command.GetFileSize(_header));
    }

    private uint GetCodeSlotCount()
    {
        return (GetSignatureStart() + PageSize - 1) / PageSize;
    }
}
