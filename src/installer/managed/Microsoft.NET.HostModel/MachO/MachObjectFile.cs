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
/// A MachO file not including the signature
/// </summary>
internal class MachObjectFile
{
    internal const uint SpecialSlotCount = 2;
    internal const uint PageSize = 4096;
    internal const byte Log2PageSize = 12;
    internal const byte DefaultHashSize = 32;
    internal const HashType DefaultHashType = HashType.SHA256;
    internal static IncrementalHash GetDefaultIncrementalHash() => IncrementalHash.CreateHash(HashAlgorithmName.SHA256);

    private readonly MemoryMappedViewAccessor _file;
    private MachHeader _header;
    private (LinkEditCommand Command, long ptr) _codeSignatureLC;
    private readonly (SegmentLoadCommand Command, long ptr) _textSegment;
    private readonly (Segment64LoadCommand Command, long ptr) _textSegment64;
    private (SegmentLoadCommand Command, long ptr) _linkEditSegment;
    private (Segment64LoadCommand Command, long ptr) _linkEditSegment64;
    private readonly long _lowestSection;
    private readonly string _identifier;
    private CodeSignature _codeSignatureBlob;
    private readonly long _nextCommandPtr;

    public MachObjectFile(MemoryMappedViewAccessor file, string identifier)
    {
        this._file = file;
        long commandsPtr = 0;
        file.Read(commandsPtr, out _header);
        _nextCommandPtr = ReadCommands(file, in _header, out _codeSignatureLC, out _textSegment, out _textSegment64, out _linkEditSegment, out _linkEditSegment64, out _lowestSection);
        Debug.Assert(_linkEditSegment.Command.IsDefault ^ _linkEditSegment64.Command.IsDefault);
        Debug.Assert(_textSegment.Command.IsDefault ^ _textSegment64.Command.IsDefault);
        this._identifier = identifier;
    }

    public bool HasSignature => !_codeSignatureLC.Command.IsDefault;

    public void AdHocSign()
    {
        AllocateCodeSignatureLC();
        WriteLoadCommands();
        _codeSignatureBlob = CreateSignature();
        _codeSignatureBlob.WriteToFile(_file);
    }

    public long GetFileSize()
        => (long)(_linkEditSegment.Command.GetFileOffset(_header) + _linkEditSegment.Command.GetFileSize(_header)
            + _linkEditSegment64.Command.GetFileOffset(_header) + _linkEditSegment64.Command.GetFileSize(_header));

    public CodeSignature ReadCodeSignature()
    {
        if (_codeSignatureLC.Command.IsDefault)
            return null;

        return CodeSignature.Read(_file, _codeSignatureLC.Command.GetDataOffset(_header));
    }

    public static long AdHocSign(MemoryMappedViewAccessor inputFile, string identifier)
    {
        var machO = new MachObjectFile(inputFile, identifier);
        machO.AdHocSign();
        return machO.GetFileSize();
    }

    public static long GetSignatureSizeEstimate(long fileSize, string identifier)
    {
        uint codeSlotCount = (uint)((fileSize + PageSize - 1) / PageSize);
        uint codeDirectorySize = (uint)Marshal.SizeOf<CodeDirectoryHeader>();
        codeDirectorySize += (uint)(Encoding.UTF8.GetByteCount(identifier) + 1);
        codeDirectorySize += (SpecialSlotCount + codeSlotCount) * DefaultHashSize;

        return (uint)(Marshal.SizeOf<EmbeddedSignatureHeader>()
                               + codeDirectorySize
                               + Marshal.SizeOf<RequirementsBlob>()
                               + Marshal.SizeOf<CmsWrapperBlob>());
    }

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
        if (!CodeSignature.AreEquivalent(a.ReadCodeSignature(), b.ReadCodeSignature()))
            return false;

        return true;

        static bool CodeSignatureLCsAreEquivalent((LinkEditCommand Command, long ptr) a, (LinkEditCommand Command, long ptr) b, MachHeader header)
        {
            if (a.Command.GetDataOffset(header) != b.Command.GetDataOffset(header))
                return false;
            if (a.ptr != b.ptr)
                return false;
            return true;
        }

        static bool LinkEditsAreEquivalent((SegmentLoadCommand Command, long ptr) a, (SegmentLoadCommand Command, long ptr) b, MachHeader header)
        {
            if (a.Command.GetFileOffset(header) != b.Command.GetFileOffset(header))
                return false;
            if (a.Command.GetSectionsCount(header) != b.Command.GetSectionsCount(header))
                return false;
            if (a.ptr != b.ptr)
                return false;
            return true;
        }

        static bool LinkEditsAreEquivalent64((Segment64LoadCommand Command, long ptr) a, (Segment64LoadCommand Command, long ptr) b, MachHeader header)
        {
            if (a.Command.GetFileOffset(header) != b.Command.GetFileOffset(header))
                return false;
            if (a.Command.GetSectionsCount(header) != b.Command.GetSectionsCount(header))
                return false;
            if (a.ptr != b.ptr)
                return false;
            return true;
        }
    }

    /// <summary>
    /// Returns a pointer to the end of the commands list.
    /// Fills the content of the commands with the corresponding command if present in the file.
    /// </summary>
    internal static long ReadCommands(
        MemoryMappedViewAccessor inputFile,
        in MachHeader header,
        out (LinkEditCommand Command, long ptr) codeSignatureLC,
        out (SegmentLoadCommand Command, long ptr) textSegment,
        out (Segment64LoadCommand Command, long ptr) textSegment64,
        out (SegmentLoadCommand Command, long ptr) linkEditSegment,
        out (Segment64LoadCommand Command, long ptr) linkEditSegment64,
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
    /// Clears the old signature and sets the codeSignatureLC to a
    /// </summary>
    private void AllocateCodeSignatureLC()
    {
        uint csOffset = GetSignatureStart();
        uint csPtr = (uint)(_codeSignatureLC.Command.IsDefault ? _nextCommandPtr : _codeSignatureLC.ptr);
        uint csSize = GetCodeSignatureSize();

        if (_codeSignatureLC.Command.IsDefault)
        {
            // Add a new CodeSignature command
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

    private void WriteLoadCommands()
    {
        _file.Write(0, ref _header);
        _file.Write(_codeSignatureLC.ptr, ref _codeSignatureLC.Command);

        if (_linkEditSegment.Command.IsDefault)
            _file.Write(_linkEditSegment64.ptr, ref _linkEditSegment64.Command);
        else
            _file.Write(_linkEditSegment.ptr, ref _linkEditSegment.Command);
    }

    private CodeSignature CreateSignature()
    {
        (EmbeddedSignatureHeader Header, long ptr) embeddedSignature;
        (CodeDirectoryHeader Header, long ptr) codeDirectory;
        (byte[] identifier, long ptr) identifierPtr;
        (byte[], long ptr) cdHashes;
        (RequirementsBlob Header, long ptr) requirementsBlob;
        (CmsWrapperBlob Header, long ptr) cmsWrapperBlob;

        long signaturePtr = GetSignatureStart();

        embeddedSignature = (default, signaturePtr);
        signaturePtr += Marshal.SizeOf<EmbeddedSignatureHeader>();

        codeDirectory = (default, signaturePtr);
        signaturePtr += Marshal.SizeOf<CodeDirectoryHeader>();

        identifierPtr = (default, signaturePtr);
        signaturePtr += GetIdentifierLength();

        cdHashes = (default, signaturePtr);
        signaturePtr += SpecialSlotCount * DefaultHashSize;
        signaturePtr += GetCodeSlotCount() * DefaultHashSize;

        requirementsBlob = (RequirementsBlob.Empty, signaturePtr);
        signaturePtr += Marshal.SizeOf<RequirementsBlob>();

        cmsWrapperBlob = (CmsWrapperBlob.Empty, signaturePtr);

        identifierPtr.identifier = new byte[GetIdentifierLength()];
        Encoding.UTF8.GetBytes(_identifier).CopyTo(identifierPtr.identifier, 0);

        // Create the CodeDirectory blob
        codeDirectory.Header = CreateCodeDirectory();
        cdHashes.Item1 = new byte[(GetCodeSlotCount() + SpecialSlotCount) * DefaultHashSize];

        // fill in the CD hashes
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
                int bytesRead = _file.ReadArray(buffptr, pageBuffer, 0, codePageSize);
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
            embeddedSignature.Header.Size = GetCodeSignatureSize();
            embeddedSignature.Header.CodeDirectory = new BlobIndex(CodeDirectorySpecialSlot.CodeDirectory, (uint)(codeDirectory.ptr - embeddedSignature.ptr));
            embeddedSignature.Header.Requirements = new BlobIndex(CodeDirectorySpecialSlot.Requirements, (uint)(requirementsBlob.ptr - embeddedSignature.ptr));
            embeddedSignature.Header.CmsWrapper = new BlobIndex(CodeDirectorySpecialSlot.CmsWrapper, (uint)(cmsWrapperBlob.ptr - embeddedSignature.ptr));
        }

        return CodeSignature.Create(
            embeddedSignature,
            codeDirectory,
            identifierPtr,
            cdHashes,
            requirementsBlob,
            cmsWrapperBlob);
    }


    private uint GetIdentifierLength()
    {
        return (uint)(Encoding.UTF8.GetByteCount(_identifier) + 1);
    }

    private uint GetCodeDirectorySize()
    {
        return (uint)(Marshal.SizeOf<CodeDirectoryHeader>()
            + GetIdentifierLength()
            + SpecialSlotCount * DefaultHashSize
            + GetCodeSlotCount() * DefaultHashSize);
    }

    private uint GetCodeSignatureSize()
    {
        return (uint)(Marshal.SizeOf<EmbeddedSignatureHeader>()
            + GetCodeDirectorySize()
            + Marshal.SizeOf<RequirementsBlob>()
            + Marshal.SizeOf<CmsWrapperBlob>());
    }

    private CodeDirectoryHeader CreateCodeDirectory()
    {
        CodeDirectoryVersion version = CodeDirectoryVersion.HighestVersion;
        uint identifierLength = GetIdentifierLength();
        uint codeDirectorySize = GetCodeDirectorySize();

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

    public static bool IsMachFile(MemoryMappedViewAccessor memoryMappedViewAccessor)
    {
        memoryMappedViewAccessor.Read(0, out MachMagic magic);
        return magic is MachMagic.MachHeaderCurrentEndian or MachMagic.MachHeaderOppositeEndian
            or MachMagic.MachHeader64CurrentEndian or MachMagic.MachHeader64OppositeEndian
            or MachMagic.FatMagicCurrentEndian or MachMagic.FatMagicOppositeEndian;

    }

    internal static bool TryRemoveCodesign(MemoryMappedViewAccessor memoryMappedViewAccessor, out long? newLength)
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
        else {
            machFile._linkEditSegment.Command.SetFileSize(
                machFile._linkEditSegment.Command.GetFileSize(machFile._header)
                    - machFile._codeSignatureLC.Command.GetFileSize(machFile._header),
                machFile._header);
        }
        newLength = machFile.GetFileSize();
        machFile._codeSignatureLC = default;
        machFile.WriteLoadCommands();
        return true;
    }
}
