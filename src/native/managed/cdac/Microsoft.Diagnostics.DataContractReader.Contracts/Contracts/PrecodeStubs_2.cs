// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

// Runtimes built with FEATURE_PORTABLE_ENTRYPOINTS (e.g. WebAssembly) have no executable precode
// stubs: every entry point is a PortableEntryPoint that records its owning MethodDesc. There are no
// interpreter precodes, so GetInterpreterCodeFromInterpreterPrecodeIfPresent keeps the interface
// default (the entry point unchanged), and GetPrecodeEntryPointFromInteriorAddress is not supported.
internal sealed class PrecodeStubs_2 : IPrecodeStubs
{
    private readonly Target _target;

    public PrecodeStubs_2(Target target)
    {
        _target = target;
    }

    // Mirrors the FEATURE_PORTABLE_ENTRYPOINTS path of MethodDesc::GetMethodDescFromPrecode.
    TargetPointer IPrecodeStubs.GetMethodDescFromStubAddress(TargetCodePointer entryPoint)
        => _target.ProcessedData.GetOrAdd<Data.PortableEntryPoint>(entryPoint.AsTargetPointer).MethodDesc;
}
