// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.DataContractReader.ExecutionManagerHelpers;

namespace Microsoft.Diagnostics.DataContractReader.Contracts.StackWalkHelpers.Wasm;

/// <summary>
/// cDAC implementation of <see cref="IWasmR2RInfo"/>, backed by the ExecutionManager's
/// <see cref="WasmFunctionTableIndexLookup"/>.
/// </summary>
internal sealed class WasmR2RInfo : IWasmR2RInfo
{
    private readonly WasmFunctionTableIndexLookup _lookup;

    public WasmR2RInfo(Target target)
    {
        _lookup = new WasmFunctionTableIndexLookup(target);
    }

    public bool TryGetVirtualIPBase(uint functionTableIndex, out ulong baseVirtualIP)
        => _lookup.TryGetVirtualIPBase(functionTableIndex, out baseVirtualIP);

    public bool TryGetUnwindData(uint functionTableIndex, out TargetPointer unwindDataAddress)
        => _lookup.TryGetUnwindData(functionTableIndex, out unwindDataAddress);
}
