// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

[CdacType(nameof(DataType.LoaderAllocator))]
internal sealed partial class LoaderAllocator : IData<LoaderAllocator>
{
    [Field] public uint ReferenceCount { get; }
    [Field] public TargetPointer HighFrequencyHeap { get; }
    [Field] public TargetPointer LowFrequencyHeap { get; }
    [Field] public TargetPointer StaticsHeap { get; }
    [Field] public TargetPointer StubHeap { get; }
    [Field] public TargetPointer ExecutableHeap { get; }
    [Field] public TargetPointer? FixupPrecodeHeap { get; }
    [Field] public TargetPointer? NewStubPrecodeHeap { get; }
    [Field] public TargetPointer? DynamicHelpersStubHeap { get; }
    [Field] public TargetPointer VirtualCallStubManager { get; }
    [Field] public ObjectHandle ObjectHandle { get; }
    [Field] public bool IsCollectible { get; }
    [Field] public ulong CreationNumber { get; }
    public bool IsAlive => ReferenceCount != 0;
}
