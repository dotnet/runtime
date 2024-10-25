// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Runtime.InteropServices;

namespace Microsoft.NET.HostModel.MachO;

/// <summary>
/// Managed class with information about a Mach-O code signature.
/// </summary>
internal class CodeSignature
{
    private readonly long _fileOffset;
    private EmbeddedSignatureHeader _embeddedSignature;
    private CodeDirectoryHeader _codeDirectory;
    private byte[] _identifier;
    private byte[] _cdHashes;
    private RequirementsBlob _requirementsBlob;
    private CmsWrapperBlob _cmsWrapperBlob;
    private bool _unrecognizedFormat;

    public uint Size => _embeddedSignature.Size;

    private CodeSignature(long fileOffset) { _fileOffset = fileOffset; }

    public static bool AreEquivalent(CodeSignature a, CodeSignature b)
    {
        if (a is null ^ b is null)
            return false;
        if (a is null && b is null)
            return true;
        if (a._unrecognizedFormat || b._unrecognizedFormat)
            return false;
        if (!a._embeddedSignature.Equals(b._embeddedSignature))
            return false;
        if (!a._codeDirectory.Equals(b._codeDirectory))
            return false;
        if (!a._identifier.SequenceEqual(b._identifier))
            return false;

        var aSpecialSlotHashes = a._cdHashes.AsSpan(0, (int)MachObjectFile.SpecialSlotCount * MachObjectFile.DefaultHashSize);
        var bSpecialSlotHashes = b._cdHashes.AsSpan(0, (int)MachObjectFile.SpecialSlotCount * MachObjectFile.DefaultHashSize);
        if (!aSpecialSlotHashes.SequenceEqual(bSpecialSlotHashes))
            return false;
        var aCodeHashes = a._cdHashes.AsSpan(((int)MachObjectFile.SpecialSlotCount + 1) * MachObjectFile.DefaultHashSize);
        var bCodeHashes = b._cdHashes.AsSpan(((int)MachObjectFile.SpecialSlotCount + 1) * MachObjectFile.DefaultHashSize);
        if (!aCodeHashes.SequenceEqual(bCodeHashes))
            return false;

        return true;
    }

    public static CodeSignature Create(
        long fileOffset,
        EmbeddedSignatureHeader embeddedSignature,
        CodeDirectoryHeader codeDirectory,
        byte[] identifier,
        byte[] cdHashes,
        RequirementsBlob requirementsBlob,
        CmsWrapperBlob cmsWrapperBlob)
    {
        var cs = new CodeSignature(fileOffset)
        {
            _embeddedSignature = embeddedSignature,
            _codeDirectory = codeDirectory,
            _identifier = identifier,
            _cdHashes = cdHashes,
            _requirementsBlob = requirementsBlob,
            _cmsWrapperBlob = cmsWrapperBlob,
            _unrecognizedFormat = false
        };
        return cs;
    }

    public static CodeSignature Read(MemoryMappedViewAccessor file, long fileOffset)
    {
        CodeSignature cs = new CodeSignature(fileOffset);
        file.Read(fileOffset, out cs._embeddedSignature);
        if (cs._embeddedSignature.BlobCount != 3
            || cs._embeddedSignature.CodeDirectory.Slot != CodeDirectorySpecialSlot.CodeDirectory
            || cs._embeddedSignature.Requirements.Slot != CodeDirectorySpecialSlot.Requirements
            || cs._embeddedSignature.CmsWrapper.Slot != CodeDirectorySpecialSlot.CmsWrapper)
        {
            cs._unrecognizedFormat = true;
            return cs;
        }
        var cdOffset = cs._fileOffset + cs._embeddedSignature.CodeDirectory.Offset;
        file.Read(cdOffset, out cs._codeDirectory);
        if (cs._codeDirectory.Version != CodeDirectoryVersion.HighestVersion
            || cs._codeDirectory.HashType != HashType.SHA256
            || cs._codeDirectory.SpecialSlotCount != MachObjectFile.SpecialSlotCount)
        {
            cs._unrecognizedFormat = true;
            return cs;
        }

        long identifierOffset = cdOffset + cs._codeDirectory.IdentifierOffset;
        long codeHashesOffset = cdOffset + cs._codeDirectory.HashesOffset - (MachObjectFile.SpecialSlotCount * MachObjectFile.DefaultHashSize);

        cs._identifier = new byte[codeHashesOffset - identifierOffset];
        file.ReadArray(identifierOffset, cs._identifier, 0, cs._identifier.Length);

        cs._cdHashes = new byte[(MachObjectFile.SpecialSlotCount + cs._codeDirectory.CodeSlotCount) * MachObjectFile.DefaultHashSize];
        file.ReadArray(codeHashesOffset, cs._cdHashes, 0, cs._cdHashes.Length);

        var requirementsOffset = cs._fileOffset + cs._embeddedSignature.Requirements.Offset;
        file.Read(requirementsOffset, out cs._requirementsBlob);
        if (!cs._requirementsBlob.Equals(RequirementsBlob.Empty))
        {
            cs._unrecognizedFormat = true;
            return cs;
        }

        var cmsOffset = fileOffset + cs._embeddedSignature.CmsWrapper.Offset;
        file.Read(cmsOffset, out cs._cmsWrapperBlob);
        if (!cs._cmsWrapperBlob.Equals(CmsWrapperBlob.Empty))
        {
            cs._unrecognizedFormat = true;
            return cs;
        }
        return cs;
    }

    internal void WriteToFile(MemoryMappedViewAccessor file)
    {
        long fileOffset = _fileOffset;

        file.Write(fileOffset, ref _embeddedSignature);
        fileOffset += Marshal.SizeOf<EmbeddedSignatureHeader>();

        file.Write(fileOffset, ref _codeDirectory);
        fileOffset += Marshal.SizeOf<CodeDirectoryHeader>();

        file.WriteArray(fileOffset, _identifier, 0, _identifier.Length);
        fileOffset += _identifier.Length;

        file.WriteArray(fileOffset, _cdHashes, 0, _cdHashes.Length);
        fileOffset += _cdHashes.Length;

        file.Write(fileOffset, ref _requirementsBlob);
        fileOffset += Marshal.SizeOf<RequirementsBlob>();

        file.Write(fileOffset, ref _cmsWrapperBlob);
        Debug.Assert(fileOffset + Marshal.SizeOf<CmsWrapperBlob>() == _fileOffset + Size);
    }

    internal void WriteToStream(FileStream file)
    {
        byte[] arr = new byte[(int)Size];
        Span<byte> buffer = arr;

        MemoryMarshal.Write(buffer, ref _embeddedSignature);
        int bufferOffset = Marshal.SizeOf<EmbeddedSignatureHeader>();

        MemoryMarshal.Write(buffer.Slice(bufferOffset), ref _codeDirectory);
        bufferOffset += Marshal.SizeOf<CodeDirectoryHeader>();

        _identifier.CopyTo(buffer.Slice(bufferOffset));
        bufferOffset += _identifier.Length;

        _cdHashes.AsSpan().CopyTo(buffer.Slice(bufferOffset));
        bufferOffset += _cdHashes.Length;

        MemoryMarshal.Write(buffer.Slice(bufferOffset), ref _requirementsBlob);
        bufferOffset += Marshal.SizeOf<RequirementsBlob>();

        MemoryMarshal.Write(buffer.Slice(bufferOffset), ref _cmsWrapperBlob);
        Debug.Assert(bufferOffset + Marshal.SizeOf<CmsWrapperBlob>() == buffer.Length);

        file.Write(arr, 0, buffer.Length);
    }
}
