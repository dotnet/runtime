// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;

namespace Microsoft.Diagnostics.DataContractReader.RuntimeTypeSystemHelpers;

internal static class MethodDescFlags_1
{
    [Flags]
    internal enum MethodDescFlags : ushort
    {
        ClassificationMask = 0x7,
        #region Optional slots
        // The below flags each imply that there's an extra pointer-size-aligned piece of data after the MethodDesc in the MethodDescChunk
        HasNonVtableSlot = 0x0008,
        HasMethodImpl = 0x0010,
        HasNativeCodeSlot = 0x0020,
        HasAsyncMethodData = 0x040,
        #endregion Optional slots
    }

    [Flags]
    internal enum MethodDescFlags3 : ushort
    {
        // HasPrecode implies that HasStableEntryPoint is set.
        HasStableEntryPoint = 0x1000, // The method entrypoint is stable (either precode or actual code)
        HasPrecode = 0x2000, // Precode has been allocated for this method
        IsUnboxingStub = 0x4000,
        IsEligibleForTieredCompilation = 0x8000,
    }

    [Flags]
    internal enum MethodDescEntryPointFlags : byte
    {
        TemporaryEntryPointAssigned = 0x04,
    }
}
