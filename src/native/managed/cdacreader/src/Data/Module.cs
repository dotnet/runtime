// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
        LoaderAllocator = target.ReadPointer(address + (ulong)type.Fields[nameof(LoaderAllocator)].Offset);
        PEAssembly = target.ProcessedData.GetOrAdd<PEAssembly>(target.ReadPointer(address + (ulong)type.Fields[nameof(PEAssembly)].Offset));
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
    public TargetPointer LoaderAllocator { get; init; }
    public PEAssembly PEAssembly { get; init; }
    public TargetPointer ThunkHeap { get; init; }

    public TargetPointer FieldDefToDescMap { get; init; }
    public TargetPointer ManifestModuleReferencesMap { get; init; }
    public TargetPointer MemberRefToDescMap { get; init; }
    public TargetPointer MethodDefToDescMap { get; init; }
    public TargetPointer TypeDefToMethodTableMap { get; init; }
    public TargetPointer TypeRefToMethodTableMap { get; init; }
}
