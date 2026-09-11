// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

[CdacType(nameof(DataType.Module))]
internal sealed partial class Module : IData<Module>
{
    [Field] public partial TargetPointer Assembly { get; }
    [Field] public partial TargetPointer PEAssembly { get; }

    [Field(Writable = true)]
    public partial uint Flags { get; private set; }

    [Field] public partial TargetPointer Base { get; }
    [Field] public partial TargetPointer LoaderAllocator { get; }
    [Field] public partial TargetPointer DynamicMetadata { get; }
    [Field] public partial uint MetadataGeneration { get; }
    [Field] public partial TargetPointer SimpleName { get; }
    [Field] public partial TargetPointer Path { get; }
    [Field] public partial TargetPointer FileName { get; }
    [Field] public partial TargetPointer ReadyToRunInfo { get; }
    [Field] public partial TargetPointer GrowableSymbolStream { get; }
    [Field] public partial TargetPointer AvailableTypeParams { get; }
    [Field] public partial TargetPointer InstMethodHashTable { get; }

    [FieldAddress] public partial TargetPointer FieldDefToDescMap { get; }
    [FieldAddress] public partial TargetPointer ManifestModuleReferencesMap { get; }
    [FieldAddress] public partial TargetPointer MemberRefToDescMap { get; }
    [FieldAddress] public partial TargetPointer MethodDefToDescMap { get; }
    [FieldAddress] public partial TargetPointer TypeDefToMethodTableMap { get; }
    [FieldAddress] public partial TargetPointer TypeRefToMethodTableMap { get; }

    // Present only when the target was built with code versioning (FEATURE_CODE_VERSIONING);
    // absent on builds where it is disabled (e.g. WASM), where it reads as null.
    [FieldAddress] public partial TargetPointer? MethodDefToILCodeVersioningStateMap { get; }

    [FieldAddress] public partial TargetPointer? EnCClassList { get; }
    [Field] public partial TargetPointer DynamicILBlobTable { get; }
}
