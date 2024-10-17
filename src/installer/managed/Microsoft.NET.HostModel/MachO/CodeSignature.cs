// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO.MemoryMappedFiles;
using System.Linq;

namespace Microsoft.NET.HostModel.MachO;

internal class CodeSignature
{
    private (EmbeddedSignatureHeader Header, long ptr) embeddedSignature;
    private (CodeDirectoryHeader Header, long ptr) codeDirectory;
    private (byte[] identifier, long ptr) identifierPtr;
    private (byte[], long ptr) cdHashes;
    private (RequirementsBlob Header, long ptr) requirementsBlob;
    private (CmsWrapperBlob Header, long ptr) cmsWrapperBlob;
    private bool _unrecognizedFormat;

    public uint Size => embeddedSignature.Header.Size;

    private CodeSignature() { }

    public static bool AreEquivalent(CodeSignature a, CodeSignature b)
    {
        if (a._unrecognizedFormat || b._unrecognizedFormat)
            return false;
        if (!a.embeddedSignature.Equals(b.embeddedSignature))
            return false;
        if (!a.codeDirectory.Equals(b.codeDirectory))
            return false;
        if (!a.identifierPtr.identifier.SequenceEqual(b.identifierPtr.identifier))
            return false;

        var aSpecialSlotHashes = a.cdHashes.Item1.AsSpan(0, (int)MachObjectFile.SpecialSlotCount * MachObjectFile.DefaultHashSize);
        var bSpecialSlotHashes = b.cdHashes.Item1.AsSpan(0, (int)MachObjectFile.SpecialSlotCount * MachObjectFile.DefaultHashSize);
        if (!aSpecialSlotHashes.SequenceEqual(bSpecialSlotHashes))
            return false;
        var aCodeHashes = a.cdHashes.Item1.AsSpan(((int)MachObjectFile.SpecialSlotCount + 1) * MachObjectFile.DefaultHashSize);
        var bCodeHashes = b.cdHashes.Item1.AsSpan(((int)MachObjectFile.SpecialSlotCount + 1) * MachObjectFile.DefaultHashSize);
        if (!aCodeHashes.SequenceEqual(bCodeHashes))
            return false;

        return true;
    }

    public static CodeSignature Create(
        (EmbeddedSignatureHeader Header, long ptr) embeddedSignature,
        (CodeDirectoryHeader Header, long ptr) codeDirectory,
        (byte[] identifier, long ptr) identifierPtr,
        (byte[], long ptr) cdHashes,
        (RequirementsBlob Header, long ptr) requirementsBlob,
        (CmsWrapperBlob Header, long ptr) cmsWrapperBlob)
    {
        var cs = new CodeSignature();
        cs.embeddedSignature = embeddedSignature;
        cs.codeDirectory = codeDirectory;
        cs.identifierPtr = identifierPtr;
        cs.cdHashes = cdHashes;
        cs.requirementsBlob = requirementsBlob;
        cs.cmsWrapperBlob = cmsWrapperBlob;
        cs._unrecognizedFormat = false;
        return cs;
    }

    public static CodeSignature Read(MemoryMappedViewAccessor file, long ptr)
    {
        CodeSignature cs = new CodeSignature();
        cs.embeddedSignature.ptr = ptr;
        file.Read(ptr, out cs.embeddedSignature.Header);
        if (cs.embeddedSignature.Header.BlobCount != 3
            || cs.embeddedSignature.Header.CodeDirectory.Slot != CodeDirectorySpecialSlot.CodeDirectory
            || cs.embeddedSignature.Header.Requirements.Slot != CodeDirectorySpecialSlot.Requirements
            || cs.embeddedSignature.Header.CmsWrapper.Slot != CodeDirectorySpecialSlot.CmsWrapper)
        {
            cs._unrecognizedFormat = true;
            return cs;
        }
        cs.codeDirectory.ptr = ptr + cs.embeddedSignature.Header.CodeDirectory.Offset;
        file.Read(cs.codeDirectory.ptr, out cs.codeDirectory.Header);
        if (cs.codeDirectory.Header.Version != CodeDirectoryVersion.HighestVersion
            || cs.codeDirectory.Header.HashType != HashType.SHA256)
        {
            cs._unrecognizedFormat = true;
            return cs;
        }

        cs.identifierPtr.ptr = cs.codeDirectory.ptr + cs.codeDirectory.Header.IdentifierOffset;
        cs.cdHashes.ptr = cs.codeDirectory.ptr + cs.codeDirectory.Header.HashesOffset - MachObjectFile.SpecialSlotCount * MachObjectFile.DefaultHashSize;;

        cs.identifierPtr.identifier = new byte[cs.cdHashes.ptr - cs.identifierPtr.ptr];
        file.ReadArray(cs.identifierPtr.ptr, cs.identifierPtr.identifier, 0, cs.identifierPtr.identifier.Length);

        cs.cdHashes.Item1 = new byte[(MachObjectFile.SpecialSlotCount + cs.codeDirectory.Header.CodeSlotCount) * MachObjectFile.DefaultHashSize];
        file.ReadArray(cs.cdHashes.ptr, cs.cdHashes.Item1, 0, cs.cdHashes.Item1.Length);

        cs.requirementsBlob.ptr = ptr + cs.embeddedSignature.Header.Requirements.Offset;
        file.Read(cs.requirementsBlob.ptr, out cs.requirementsBlob.Header);
        if (!cs.requirementsBlob.Header.Equals(RequirementsBlob.Empty))
        {
            cs._unrecognizedFormat = true;
            return cs;
        }

        cs.cmsWrapperBlob.ptr = ptr + cs.embeddedSignature.Header.CmsWrapper.Offset;
        file.Read(cs.cmsWrapperBlob.ptr, out cs.cmsWrapperBlob.Header);
        if (!cs.cmsWrapperBlob.Header.Equals(CmsWrapperBlob.Empty))
        {
            cs._unrecognizedFormat = true;
            return cs;
        }
        return cs;
    }

    internal void WriteToFile(MemoryMappedViewAccessor file)
    {
        file.Write(embeddedSignature.ptr, ref embeddedSignature.Header);
        file.Write(codeDirectory.ptr, ref codeDirectory.Header);
        file.WriteArray(identifierPtr.ptr, identifierPtr.identifier, 0, identifierPtr.identifier.Length);
        file.WriteArray(cdHashes.ptr, cdHashes.Item1, 0, cdHashes.Item1.Length);
        file.Write(requirementsBlob.ptr, ref requirementsBlob.Header);
        file.Write(cmsWrapperBlob.ptr, ref cmsWrapperBlob.Header);
    }
}
