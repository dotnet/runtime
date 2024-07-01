// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.Reflection.PortableExecutable;

namespace Microsoft.Diagnostics.DataContractReader.Data;

internal sealed class PEImage : IData<PEImage>
{
    static PEImage IData<PEImage>.Create(Target target, TargetPointer address)
        => new PEImage(target, address);

    private readonly Target _target;

    public PEImage(Target target, TargetPointer address)
    {
        _target = target;
        Target.TypeInfo type = target.GetTypeInfo(DataType.PEImage);
        LoadedLayout = target.ReadPointer(address + (ulong)type.Fields[nameof(LoadedLayout)].Offset);
        if (LoadedLayout != TargetPointer.Null)
        {
            Target.TypeInfo layoutType = target.GetTypeInfo(DataType.PEImageLayout);
            Base = target.ReadPointer(LoadedLayout + (ulong)layoutType.Fields[nameof(Base)].Offset);
        }
    }

    public TargetPointer Base { get; init; } = TargetPointer.Null;

    private TargetPointer LoadedLayout { get; init; }

    private TargetPointer _metadataStart = TargetPointer.Null;
    private ulong _metadataSize;
    public TargetPointer GetLoadedMetadata(out ulong size)
    {
        if (Base != TargetPointer.Null && _metadataStart == TargetPointer.Null && _metadataSize == 0)
        {
            int peSignatureOffset = _target.Read<int>(Base + PEOffsets.DosStub.PESignatureOffset);
            ulong headerOffset = Base + (ulong)peSignatureOffset;
            ushort magic = _target.Read<ushort>(headerOffset + PEOffsets.PEHeader.Magic);
            ulong clrHeaderOffset = magic == (ushort)PEMagic.PE32
                ? PEOffsets.PEHeader.CLRRuntimeHeader32
                : PEOffsets.PEHeader.CLRRuntimeHeader32Plus;
            int corHeaderRva = _target.Read<int>(headerOffset + clrHeaderOffset);

            // Read RVA and size of the metadata
            ulong metadataDirectoryAddress = Base + (ulong)corHeaderRva + PEOffsets.CorHeader.Metadata;
            _metadataStart = Base + (ulong)_target.Read<int>(metadataDirectoryAddress);
            _metadataSize = (ulong)_target.Read<int>(metadataDirectoryAddress + sizeof(int));
        }

        size = _metadataSize;
        return _metadataStart;
    }

    // https://learn.microsoft.com/windows/win32/debug/pe-format
    private static class PEOffsets
    {
        private const int PESignatureSize = sizeof(int);
        private const int CoffHeaderSize = 20;

        public static class DosStub
        {
            public const int PESignatureOffset = 0x3c;
        }

        public static class PEHeader
        {
            private const ulong OptionalHeader = PESignatureSize + CoffHeaderSize;
            public const ulong Magic = OptionalHeader;
            public const ulong CLRRuntimeHeader32 = OptionalHeader + 208;
            public const ulong CLRRuntimeHeader32Plus = OptionalHeader + 224;
        }

        // See ECMA-335 II.25.3.3 CLI Header
        public static class CorHeader
        {
            public const ulong Metadata = 8;
        }
    }
}
