// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection.PortableExecutable;

namespace Microsoft.Diagnostics.DataContractReader.Data;

internal sealed class Module : IData<Module>
{
    static Module IData<Module>.Create(Target target, TargetPointer address)
        => new Module(target, address);

    public Module(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.Module);

        _address = address;
        Flags = target.ReadField<uint>(address, type, nameof(Flags));
        Assembly = target.ReadPointerField(address, type, nameof(Assembly));
        PEAssembly = target.ReadPointerField(address, type, nameof(PEAssembly));
        Base = target.ReadPointerField(address, type, nameof(Base));
        LoaderAllocator = target.ReadPointerField(address, type, nameof(LoaderAllocator));
        DynamicMetadata = target.ReadPointerField(address, type, nameof(DynamicMetadata));
        SimpleName = target.ReadPointerField(address, type, nameof(SimpleName));
        Path = target.ReadPointerField(address, type, nameof(Path));
        FileName = target.ReadPointerField(address, type, nameof(FileName));
        ReadyToRunInfo = target.ReadPointerField(address, type, nameof(ReadyToRunInfo));
        GrowableSymbolStream = target.ReadPointerField(address, type, nameof(GrowableSymbolStream));
        AvailableTypeParams = target.ReadPointerField(address, type, nameof(AvailableTypeParams));
        InstMethodHashTable = target.ReadPointerField(address, type, nameof(InstMethodHashTable));

        FieldDefToDescMap = address + (ulong)type.Fields[nameof(FieldDefToDescMap)].Offset;
        ManifestModuleReferencesMap = address + (ulong)type.Fields[nameof(ManifestModuleReferencesMap)].Offset;
        MemberRefToDescMap = address + (ulong)type.Fields[nameof(MemberRefToDescMap)].Offset;
        MethodDefToDescMap = address + (ulong)type.Fields[nameof(MethodDefToDescMap)].Offset;
        TypeDefToMethodTableMap = address + (ulong)type.Fields[nameof(TypeDefToMethodTableMap)].Offset;
        TypeRefToMethodTableMap = address + (ulong)type.Fields[nameof(TypeRefToMethodTableMap)].Offset;
        MethodDefToILCodeVersioningStateMap = address + (ulong)type.Fields[nameof(MethodDefToILCodeVersioningStateMap)].Offset;
        DynamicILBlobTable = target.ReadPointerField(address, type, nameof(DynamicILBlobTable));
    }

    private readonly TargetPointer _address;

    public void WriteFlags(Target target, uint flags)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.Module);
        ulong flagsAddr = _address + (ulong)type.Fields[nameof(Flags)].Offset;
        target.Write<uint>(flagsAddr, flags);
        Flags = flags;
    }

    public TargetPointer Assembly { get; init; }
    public TargetPointer PEAssembly { get; init; }
    public uint Flags { get; private set; }
    public TargetPointer Base { get; init; }
    public TargetPointer LoaderAllocator { get; init; }
    public TargetPointer DynamicMetadata { get; init; }
    public TargetPointer SimpleName { get; init; }
    public TargetPointer Path { get; init; }
    public TargetPointer FileName { get; init; }
    public TargetPointer ReadyToRunInfo { get; init; }
    public TargetPointer GrowableSymbolStream { get; init; }
    public TargetPointer AvailableTypeParams { get; init; }
    public TargetPointer InstMethodHashTable { get; init; }

    public TargetPointer FieldDefToDescMap { get; init; }
    public TargetPointer ManifestModuleReferencesMap { get; init; }
    public TargetPointer MemberRefToDescMap { get; init; }
    public TargetPointer MethodDefToDescMap { get; init; }
    public TargetPointer TypeDefToMethodTableMap { get; init; }
    public TargetPointer TypeRefToMethodTableMap { get; init; }
    public TargetPointer MethodDefToILCodeVersioningStateMap { get; init; }
    public TargetPointer DynamicILBlobTable { get; init; }
}
