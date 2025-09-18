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

        Flags = target.Read<uint>(address + (ulong)type.Fields[nameof(Flags)].Offset);
        Assembly = target.ReadPointer(address + (ulong)type.Fields[nameof(Assembly)].Offset);
        PEAssembly = target.ReadPointer(address + (ulong)type.Fields[nameof(PEAssembly)].Offset);
        Base = target.ReadPointer(address + (ulong)type.Fields[nameof(Base)].Offset);
        LoaderAllocator = target.ReadPointer(address + (ulong)type.Fields[nameof(LoaderAllocator)].Offset);
        DynamicMetadata = target.ReadPointer(address + (ulong)type.Fields[nameof(DynamicMetadata)].Offset);
        Path = target.ReadPointer(address + (ulong)type.Fields[nameof(Path)].Offset);
        FileName = target.ReadPointer(address + (ulong)type.Fields[nameof(FileName)].Offset);
        ReadyToRunInfo = target.ReadPointer(address + (ulong)type.Fields[nameof(ReadyToRunInfo)].Offset);
        ReadyToRunImage = target.ReadPointer(address + (ulong)type.Fields[nameof(ReadyToRunImage)].Offset);
        GrowableSymbolStream = target.ReadPointer(address + (ulong)type.Fields[nameof(GrowableSymbolStream)].Offset);
        AvailableTypeParams = target.ReadPointer(address + (ulong)type.Fields[nameof(AvailableTypeParams)].Offset);
        InstMethodHashTable = target.ReadPointer(address + (ulong)type.Fields[nameof(InstMethodHashTable)].Offset);

        FieldDefToDescMap = address + (ulong)type.Fields[nameof(FieldDefToDescMap)].Offset;
        ManifestModuleReferencesMap = address + (ulong)type.Fields[nameof(ManifestModuleReferencesMap)].Offset;
        MemberRefToDescMap = address + (ulong)type.Fields[nameof(MemberRefToDescMap)].Offset;
        MethodDefToDescMap = address + (ulong)type.Fields[nameof(MethodDefToDescMap)].Offset;
        TypeDefToMethodTableMap = address + (ulong)type.Fields[nameof(TypeDefToMethodTableMap)].Offset;
        TypeRefToMethodTableMap = address + (ulong)type.Fields[nameof(TypeRefToMethodTableMap)].Offset;
        MethodDefToILCodeVersioningStateMap = address + (ulong)type.Fields[nameof(MethodDefToILCodeVersioningStateMap)].Offset;
        DynamicILBlobTable = target.ReadPointer(address + (ulong)type.Fields[nameof(DynamicILBlobTable)].Offset);
    }

    public TargetPointer Assembly { get; init; }
    public TargetPointer PEAssembly { get; init; }
    public uint Flags { get; init; }
    public TargetPointer Base { get; init; }
    public TargetPointer LoaderAllocator { get; init; }
    public TargetPointer DynamicMetadata { get; init; }
    public TargetPointer Path { get; init; }
    public TargetPointer FileName { get; init; }
    public TargetPointer ReadyToRunInfo { get; init; }
    public TargetPointer ReadyToRunImage { get; init; }
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
