// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

[CdacType(nameof(DataType.Module))]
internal sealed partial class Module : IData<Module>
{
    [Field] public TargetPointer Assembly { get; }
    [Field] public TargetPointer PEAssembly { get; }

    [Field(Writable = true)]
    public uint Flags { get; private set; }

    [Field] public TargetPointer Base { get; }
    [Field] public TargetPointer LoaderAllocator { get; }
    [Field] public TargetPointer DynamicMetadata { get; }
    [Field] public uint MetadataGeneration { get; }
    [Field] public TargetPointer SimpleName { get; }
    [Field] public TargetPointer Path { get; }
    [Field] public TargetPointer FileName { get; }
    [Field] public TargetPointer ReadyToRunInfo { get; }
    [Field] public TargetPointer GrowableSymbolStream { get; }
    [Field] public TargetPointer AvailableTypeParams { get; }
    [Field] public TargetPointer InstMethodHashTable { get; }

    [FieldAddress] public TargetPointer FieldDefToDescMap { get; }
    [FieldAddress] public TargetPointer ManifestModuleReferencesMap { get; }
    [FieldAddress] public TargetPointer MemberRefToDescMap { get; }
    [FieldAddress] public TargetPointer MethodDefToDescMap { get; }
    [FieldAddress] public TargetPointer TypeDefToMethodTableMap { get; }
    [FieldAddress] public TargetPointer TypeRefToMethodTableMap { get; }

    // Present only when the target was built with code versioning (FEATURE_CODE_VERSIONING);
    // absent on builds where it is disabled (e.g. WASM), where it reads as null.
    [FieldAddress] public TargetPointer? MethodDefToILCodeVersioningStateMap { get; }

    [FieldAddress] public TargetPointer? EnCClassList { get; }
    [Field] public TargetPointer DynamicILBlobTable { get; }
}
