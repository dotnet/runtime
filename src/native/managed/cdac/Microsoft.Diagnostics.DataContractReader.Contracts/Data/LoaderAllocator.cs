// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

[CdacType(nameof(DataType.LoaderAllocator))]
internal sealed partial class LoaderAllocator : IData<LoaderAllocator>
{
    [Field] public partial uint ReferenceCount { get; }
    [Field] public partial TargetPointer HighFrequencyHeap { get; }
    [Field] public partial TargetPointer LowFrequencyHeap { get; }
    [Field] public partial TargetPointer StaticsHeap { get; }
    [Field] public partial TargetPointer StubHeap { get; }
    [Field] public partial TargetPointer ExecutableHeap { get; }
    [Field] public partial TargetPointer? FixupPrecodeHeap { get; }
    [Field] public partial TargetPointer? NewStubPrecodeHeap { get; }
    [Field] public partial TargetPointer? DynamicHelpersStubHeap { get; }
    [Field] public partial TargetPointer VirtualCallStubManager { get; }
    [Field] public partial ObjectHandle ObjectHandle { get; }
    [Field] public partial bool IsCollectible { get; }
    [Field] public partial ulong CreationNumber { get; }
    public bool IsAlive => ReferenceCount != 0;
}
