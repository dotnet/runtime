// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.IO.MemoryMappedFiles;
using Microsoft.NET.HostModel.AppHost;

namespace Microsoft.NET.HostModel.MachO;

/// <summary>
/// A managed object containing relevant information for AdHoc signing a Mach-O file.
/// The object is created from a memory mapped file, and a signature can be calculated from the memory mapped file.
/// However, since a memory mapped file cannot be extended, the signature is written to a file stream.
/// </summary>
internal unsafe partial class MachObjectFile
{
    private MachHeader _header;
    private (LinkEditCommand Command, long FileOffset) _codeSignatureLoadCommand;
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
        _codeSignatureLoadCommand = codeSignatureLC;
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
        if (!IsMachOImage(file))
            throw new InvalidDataException("File is not a Mach-O image");

        file.Read(commandsPtr, out MachHeader header);
        if (!header.Is64Bit)
            throw new AppHostMachOFormatException(MachOFormatError.Not64BitExe);

        long nextCommandPtr = ReadCommands(
            file,
            in header,
            out (LinkEditCommand Command, long FileOffset) codeSignatureLC,
            out (Segment64LoadCommand Command, long FileOffset) textSegment64,
            out (Segment64LoadCommand Command, long FileOffset) linkEditSegment64,
            out long lowestSection);
        CodeSignature codeSignatureBlob = codeSignatureLC.Command.IsDefault
            ? null
            : CodeSignature.Read(file, codeSignatureLC.Command.GetDataOffset(header));
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
    public bool HasSignature => !_codeSignatureLoadCommand.Command.IsDefault;

    /// <summary>
    /// Adds or replaces the code signature load command and modifies the __LINKEDIT segment size to accomodate the signature.
    /// Writes the EmbeddedSignature blob to the file.
    /// Returns the new size of the file (the end of the signature blob).
    /// </summary>
    public long CreateAdHocSignature(MemoryMappedViewAccessor file, string identifier)
    {
        AllocateCodeSignatureLoadCommand(identifier);
        _codeSignatureBlob = null;
        // The code signature includes hashes of the entire file up to the code signature.
        // In order to calculate the hashes correctly, everything up to the code signature must be written before the signature is built.
        Write(file);
        _codeSignatureBlob = CodeSignature.CreateSignature(this, file, identifier);
        _codeSignatureBlob.WriteToFile(file);
        return GetFileSize();
    }

    public static bool IsMachOImage(MemoryMappedViewAccessor memoryMappedViewAccessor)
    {
        memoryMappedViewAccessor.Read(0, out MachMagic magic);
        return magic is MachMagic.MachHeaderCurrentEndian or MachMagic.MachHeaderOppositeEndian
            or MachMagic.MachHeader64CurrentEndian or MachMagic.MachHeader64OppositeEndian
            or MachMagic.FatMagicCurrentEndian or MachMagic.FatMagicOppositeEndian;
    }

    public static bool IsMachOImage(FileStream file)
    {
        long oldPosition = file.Position;
        file.Position = 0;
        // We can read the Magic as any endianness since we just need to determine if it is a Mach-O file.
        uint magic = (uint)(file.ReadByte() << 24 | file.ReadByte() << 16 | file.ReadByte() << 8 | file.ReadByte());
        file.Position = oldPosition;
        return (MachMagic)magic is MachMagic.MachHeaderCurrentEndian or MachMagic.MachHeaderOppositeEndian
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
    /// <returns>True if a signature was present and removed, false otherwise</returns>
    public static bool RemoveCodeSignatureIfPresent(MemoryMappedViewAccessor memoryMappedViewAccessor, out long? newLength)
    {
        newLength = null;
        if (!IsMachOImage(memoryMappedViewAccessor))
            return false;

        MachObjectFile machFile = Create(memoryMappedViewAccessor);
        if (machFile._codeSignatureLoadCommand.Command.IsDefault)
            return false;

        machFile._header.NumberOfCommands -= 1;
        machFile._header.SizeOfCommands -= (uint)sizeof(LinkEditCommand);
        machFile._linkEditSegment64.Command.SetFileSize(
            machFile._linkEditSegment64.Command.GetFileSize(machFile._header)
                - machFile._codeSignatureLoadCommand.Command.GetFileSize(machFile._header),
            machFile._header);
        newLength = machFile.GetFileSize();
        machFile._codeSignatureLoadCommand = default;
        machFile._codeSignatureBlob = null;
        machFile.Write(memoryMappedViewAccessor);
        return true;
    }

    /// <summary>
    /// Removes the code signature load command and signature, and resizes the file if necessary.
    /// </summary>
    public static void RemoveCodeSignatureIfPresent(FileStream bundle)
    {
        long? newLength;
        bool resized;
        // Windows doesn't allow a FileStream to be resized while the file is memory mapped, so we must dispose of the memory mapped file first.
        using (MemoryMappedFile mmap = MemoryMappedFile.CreateFromFile(bundle, null, 0, MemoryMappedFileAccess.ReadWrite, HandleInheritability.None, true))
        using (MemoryMappedViewAccessor accessor = mmap.CreateViewAccessor(0, 0, MemoryMappedFileAccess.ReadWrite))
        {
            resized = RemoveCodeSignatureIfPresent(accessor, out newLength);
        }
        if (resized)
        {
            bundle.SetLength(newLength.Value);
        }
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
        if (!CodeSignatureLCsAreEquivalent(a._codeSignatureLoadCommand, b._codeSignatureLoadCommand, a._header))
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

    public static long GetSignatureSizeEstimate(uint fileSize, string identifier)
    {
        return CodeSignature.GetCodeSignatureSize(fileSize, identifier);
    }

    /// <summary>
    /// Writes the entire file to <paramref name="file"/>.
    /// </summary>
    private long Write(MemoryMappedViewAccessor file)
    {
        if (file.Capacity < GetFileSize())
            throw new ArgumentException("File is too small", nameof(file));
        file.Write(0, ref _header);
        file.Write(_linkEditSegment64.FileOffset, ref _linkEditSegment64.Command);
        if (!_codeSignatureLoadCommand.Command.IsDefault)
        {
            file.Write(_codeSignatureLoadCommand.FileOffset, ref _codeSignatureLoadCommand.Command);
            _codeSignatureBlob?.WriteToFile(file);
        }
        return GetFileSize();
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
        long commandsPtr;
        commandsPtr = sizeof(MachHeader);
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
                        long sectionPtr = commandsPtr + sizeof(Segment64LoadCommand);
                        uint sectionsCount = segment64.GetSectionsCount(header);
                        for (int s = 0; s < sectionsCount; s++)
                        {
                            inputFile.Read(sectionPtr, out Section64LoadCommand section);
                            lowestSectionOffset = Math.Min(lowestSectionOffset, section.GetFileOffset(header));
                            sectionPtr += sizeof(Section64LoadCommand);
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
    private void AllocateCodeSignatureLoadCommand(string identifier)
    {
        uint csOffset = GetSignatureStart();
        uint csPtr = (uint)(_codeSignatureLoadCommand.Command.IsDefault ? _nextCommandPtr : _codeSignatureLoadCommand.FileOffset);
        uint csSize = CodeSignature.GetCodeSignatureSize(GetSignatureStart(), identifier);;

        if (_codeSignatureLoadCommand.Command.IsDefault)
        {
            // Update the header to accomodate the new code signature load command
            _header.NumberOfCommands += 1;
            _header.SizeOfCommands += (uint)sizeof(LinkEditCommand);
            if (_header.SizeOfCommands > _lowestSectionOffset)
            {
                throw new InvalidOperationException("Mach Object does not have enough space for the code signature load command");
            }
        }

        var currentLinkEditOffset = _linkEditSegment64.Command.GetFileOffset(_header);
        var linkEditSize = csOffset + csSize - currentLinkEditOffset;
        _linkEditSegment64.Command.SetFileSize(linkEditSize, _header);
        _codeSignatureLoadCommand = (new LinkEditCommand(MachLoadCommandType.CodeSignature, csOffset, csSize, _header), csPtr);
    }

    /// <summary>
    /// The offset in the file where the code signature starts.
    /// The signature includes hashes of all bytes up to this offset.
    /// </summary>
    private uint GetSignatureStart()
    {
        if (!_codeSignatureLoadCommand.Command.IsDefault)
        {
            return _codeSignatureLoadCommand.Command.GetDataOffset(_header);
        }
        return (uint)(_linkEditSegment64.Command.GetFileOffset(_header) + _linkEditSegment64.Command.GetFileSize(_header));
    }

    /// <summary>
    /// Gets the total size of the Mach-O file according to the load commands.
    /// </summary>
    private long GetFileSize()
        => (long)(_linkEditSegment64.Command.GetFileOffset(_header) + _linkEditSegment64.Command.GetFileSize(_header));
}
