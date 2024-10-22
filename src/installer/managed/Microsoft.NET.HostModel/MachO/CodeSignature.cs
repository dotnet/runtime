// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
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
    private (EmbeddedSignatureHeader Header, long FileOffset) _embeddedSignature;
    private (CodeDirectoryHeader Header, long FileOffset) _codeDirectory;
    private (byte[] Identifier, long FileOffset) _identifierPtr;
    private (byte[] Blob, long FileOffset) _cdHashes;
    private (RequirementsBlob Header, long FileOffset) _requirementsBlob;
    private (CmsWrapperBlob Header, long FileOffset) _cmsWrapperBlob;
    private bool _unrecognizedFormat;

    public uint Size => _embeddedSignature.Header.Size;

    private CodeSignature() { }

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
        if (!a._identifierPtr.Identifier.SequenceEqual(b._identifierPtr.Identifier))
            return false;

        var aSpecialSlotHashes = a._cdHashes.Blob.AsSpan(0, (int)MachObjectFile.SpecialSlotCount * MachObjectFile.DefaultHashSize);
        var bSpecialSlotHashes = b._cdHashes.Blob.AsSpan(0, (int)MachObjectFile.SpecialSlotCount * MachObjectFile.DefaultHashSize);
        if (!aSpecialSlotHashes.SequenceEqual(bSpecialSlotHashes))
            return false;
        var aCodeHashes = a._cdHashes.Blob.AsSpan(((int)MachObjectFile.SpecialSlotCount + 1) * MachObjectFile.DefaultHashSize);
        var bCodeHashes = b._cdHashes.Blob.AsSpan(((int)MachObjectFile.SpecialSlotCount + 1) * MachObjectFile.DefaultHashSize);
        if (!aCodeHashes.SequenceEqual(bCodeHashes))
            return false;

        return true;
    }

    public static CodeSignature Create(
        (EmbeddedSignatureHeader Header, long FileOffset) embeddedSignature,
        (CodeDirectoryHeader Header, long FileOffset) codeDirectory,
        (byte[] identifier, long FileOffset) identifierPtr,
        (byte[], long FileOffset) cdHashes,
        (RequirementsBlob Header, long FileOffset) requirementsBlob,
        (CmsWrapperBlob Header, long FileOffset) cmsWrapperBlob)
    {
        var cs = new CodeSignature();
        cs._embeddedSignature = embeddedSignature;
        cs._codeDirectory = codeDirectory;
        cs._identifierPtr = identifierPtr;
        cs._cdHashes = cdHashes;
        cs._requirementsBlob = requirementsBlob;
        cs._cmsWrapperBlob = cmsWrapperBlob;
        cs._unrecognizedFormat = false;
        return cs;
    }

    public static CodeSignature Read(MemoryMappedViewAccessor file, long fileOffset)
    {
        CodeSignature cs = new CodeSignature();
        cs._embeddedSignature.FileOffset = fileOffset;
        file.Read(fileOffset, out cs._embeddedSignature.Header);
        if (cs._embeddedSignature.Header.BlobCount != 3
            || cs._embeddedSignature.Header.CodeDirectory.Slot != CodeDirectorySpecialSlot.CodeDirectory
            || cs._embeddedSignature.Header.Requirements.Slot != CodeDirectorySpecialSlot.Requirements
            || cs._embeddedSignature.Header.CmsWrapper.Slot != CodeDirectorySpecialSlot.CmsWrapper)
        {
            cs._unrecognizedFormat = true;
            return cs;
        }
        cs._codeDirectory.FileOffset = fileOffset + cs._embeddedSignature.Header.CodeDirectory.Offset;
        file.Read(cs._codeDirectory.FileOffset, out cs._codeDirectory.Header);
        if (cs._codeDirectory.Header.Version != CodeDirectoryVersion.HighestVersion
            || cs._codeDirectory.Header.HashType != HashType.SHA256)
        {
            cs._unrecognizedFormat = true;
            return cs;
        }

        cs._identifierPtr.FileOffset = cs._codeDirectory.FileOffset + cs._codeDirectory.Header.IdentifierOffset;
        cs._cdHashes.FileOffset = cs._codeDirectory.FileOffset + cs._codeDirectory.Header.HashesOffset - MachObjectFile.SpecialSlotCount * MachObjectFile.DefaultHashSize;;

        cs._identifierPtr.Identifier = new byte[cs._cdHashes.FileOffset - cs._identifierPtr.FileOffset];
        file.ReadArray(cs._identifierPtr.FileOffset, cs._identifierPtr.Identifier, 0, cs._identifierPtr.Identifier.Length);

        cs._cdHashes.Blob = new byte[(MachObjectFile.SpecialSlotCount + cs._codeDirectory.Header.CodeSlotCount) * MachObjectFile.DefaultHashSize];
        file.ReadArray(cs._cdHashes.FileOffset, cs._cdHashes.Blob, 0, cs._cdHashes.Blob.Length);

        cs._requirementsBlob.FileOffset = fileOffset + cs._embeddedSignature.Header.Requirements.Offset;
        file.Read(cs._requirementsBlob.FileOffset, out cs._requirementsBlob.Header);
        if (!cs._requirementsBlob.Header.Equals(RequirementsBlob.Empty))
        {
            cs._unrecognizedFormat = true;
            return cs;
        }

        cs._cmsWrapperBlob.FileOffset = fileOffset + cs._embeddedSignature.Header.CmsWrapper.Offset;
        file.Read(cs._cmsWrapperBlob.FileOffset, out cs._cmsWrapperBlob.Header);
        if (!cs._cmsWrapperBlob.Header.Equals(CmsWrapperBlob.Empty))
        {
            cs._unrecognizedFormat = true;
            return cs;
        }
        return cs;
    }

    internal void WriteToFile(MemoryMappedViewAccessor file)
    {
        file.Write(_embeddedSignature.FileOffset, ref _embeddedSignature.Header);
        file.Write(_codeDirectory.FileOffset, ref _codeDirectory.Header);
        file.WriteArray(_identifierPtr.FileOffset, _identifierPtr.Identifier, 0, _identifierPtr.Identifier.Length);
        file.WriteArray(_cdHashes.FileOffset, _cdHashes.Blob, 0, _cdHashes.Blob.Length);
        file.Write(_requirementsBlob.FileOffset, ref _requirementsBlob.Header);
        file.Write(_cmsWrapperBlob.FileOffset, ref _cmsWrapperBlob.Header);
    }

    internal void WriteToStream(FileStream file)
    {
        byte[] arr = new byte[(int)Size];
        Span<byte> buffer = arr;

        MemoryMarshal.Write(buffer, ref _embeddedSignature.Header);

        int fileOffset = (int)(_codeDirectory.FileOffset - _embeddedSignature.FileOffset);
        MemoryMarshal.Write(buffer.Slice(fileOffset), ref _codeDirectory.Header);

        fileOffset = (int)(_identifierPtr.FileOffset - _embeddedSignature.FileOffset);
        _identifierPtr.Identifier.CopyTo(buffer.Slice(fileOffset));

        fileOffset = (int)(_cdHashes.FileOffset - _embeddedSignature.FileOffset);
        _cdHashes.Blob.AsSpan().CopyTo(buffer.Slice(fileOffset));

        fileOffset = (int)(_requirementsBlob.FileOffset - _embeddedSignature.FileOffset);
        MemoryMarshal.Write(buffer.Slice(fileOffset), ref _requirementsBlob.Header);

        fileOffset = (int)(_cmsWrapperBlob.FileOffset - _embeddedSignature.FileOffset);
        MemoryMarshal.Write(buffer.Slice(fileOffset), ref _cmsWrapperBlob.Header);

        file.Write(arr, 0, buffer.Length);
    }
}
