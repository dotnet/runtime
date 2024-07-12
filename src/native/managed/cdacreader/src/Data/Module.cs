// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection.PortableExecutable;

namespace Microsoft.Diagnostics.DataContractReader.Data;

internal sealed class Module : IData<Module>
{
    static Module IData<Module>.Create(Target target, TargetPointer address)
        => new Module(target, address);

    private readonly Target _target;

    public Module(Target target, TargetPointer address)
    {
        _target = target;
        Target.TypeInfo type = target.GetTypeInfo(DataType.Module);

        Flags = target.Read<uint>(address + (ulong)type.Fields[nameof(Flags)].Offset);
        Assembly = target.ReadPointer(address + (ulong)type.Fields[nameof(Assembly)].Offset);
        Base = target.ReadPointer(address + (ulong)type.Fields[nameof(Base)].Offset);
        LoaderAllocator = target.ReadPointer(address + (ulong)type.Fields[nameof(LoaderAllocator)].Offset);
        ThunkHeap = target.ReadPointer(address + (ulong)type.Fields[nameof(ThunkHeap)].Offset);

        FieldDefToDescMap = target.ReadPointer(address + (ulong)type.Fields[nameof(FieldDefToDescMap)].Offset);
        ManifestModuleReferencesMap = target.ReadPointer(address + (ulong)type.Fields[nameof(ManifestModuleReferencesMap)].Offset);
        MemberRefToDescMap = target.ReadPointer(address + (ulong)type.Fields[nameof(MemberRefToDescMap)].Offset);
        MethodDefToDescMap = target.ReadPointer(address + (ulong)type.Fields[nameof(MethodDefToDescMap)].Offset);
        TypeDefToMethodTableMap = target.ReadPointer(address + (ulong)type.Fields[nameof(TypeDefToMethodTableMap)].Offset);
        TypeRefToMethodTableMap = target.ReadPointer(address + (ulong)type.Fields[nameof(TypeRefToMethodTableMap)].Offset);
    }

    public TargetPointer Assembly { get; init; }
    public uint Flags { get; init; }
    public TargetPointer Base { get; init; }
    public TargetPointer LoaderAllocator { get; init; }
    public TargetPointer ThunkHeap { get; init; }

    public TargetPointer FieldDefToDescMap { get; init; }
    public TargetPointer ManifestModuleReferencesMap { get; init; }
    public TargetPointer MemberRefToDescMap { get; init; }
    public TargetPointer MethodDefToDescMap { get; init; }
    public TargetPointer TypeDefToMethodTableMap { get; init; }
    public TargetPointer TypeRefToMethodTableMap { get; init; }

    private TargetPointer _metadataStart = TargetPointer.Null;
    private ulong _metadataSize;
    public TargetPointer GetLoadedMetadata(out ulong size)
    {
        if (Base != TargetPointer.Null && _metadataStart == TargetPointer.Null && _metadataSize == 0)
        {
            int peSignatureOffset = _target.Read<int>(Base + PEFormat.DosStub.PESignatureOffset);
            ulong headerOffset = Base + (ulong)peSignatureOffset;
            ushort magic = _target.Read<ushort>(headerOffset + PEFormat.PEHeader.MagicOffset);
            ulong clrHeaderOffset = magic == (ushort)PEMagic.PE32
                ? PEFormat.PEHeader.CLRRuntimeHeader32Offset
                : PEFormat.PEHeader.CLRRuntimeHeader32PlusOffset;
            int corHeaderRva = _target.Read<int>(headerOffset + clrHeaderOffset);

            // Read RVA and size of the metadata
            ulong metadataDirectoryAddress = Base + (ulong)corHeaderRva + PEFormat.CorHeader.MetadataOffset;
            _metadataStart = Base + (ulong)_target.Read<int>(metadataDirectoryAddress);
            _metadataSize = (ulong)_target.Read<int>(metadataDirectoryAddress + sizeof(int));
        }

        size = _metadataSize;
        return _metadataStart;
    }

    // https://learn.microsoft.com/windows/win32/debug/pe-format
    private static class PEFormat
    {
        private const int PESignatureSize = sizeof(int);
        private const int CoffHeaderSize = 20;

        public static class DosStub
        {
            public const int PESignatureOffset = 0x3c;
        }

        public static class PEHeader
        {
            private const ulong OptionalHeaderOffset = PESignatureSize + CoffHeaderSize;
            public const ulong MagicOffset = OptionalHeaderOffset;
            public const ulong CLRRuntimeHeader32Offset = OptionalHeaderOffset + 208;
            public const ulong CLRRuntimeHeader32PlusOffset = OptionalHeaderOffset + 224;
        }

        // See ECMA-335 II.25.3.3 CLI Header
        public static class CorHeader
        {
            public const ulong MetadataOffset = 8;
        }
    }
}
