// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Microsoft.Diagnostics.DataContractReader.Contracts;

namespace Microsoft.Diagnostics.DataContractReader.Contracts.GCInfoHelpers;

internal interface IGCInfoDecoder : IGCInfoHandle
{
    GCInfoHeader GetHeader();

    uint GetCodeLength();

    // Default 0; mirrors native EECodeManager::GetStackParameterSize, which only
    // returns non-zero on x86 (where managed code uses __stdcall, callee-popped args).
    uint GetCalleePoppedArgumentsSize() => 0;

    IReadOnlyList<InterruptibleRange> GetInterruptibleRanges();
    IReadOnlyList<uint> GetSafePoints();
    IReadOnlyList<GCSlotLifetime> GetSlotLifetimes();

    IReadOnlyList<LiveSlot> EnumerateLiveSlots(uint instructionOffset, GcSlotEnumerationOptions options);
    bool IsGcSafe(uint instructionOffset);
    bool TryGetGenericContextStorage(GenericContextLoc contextKind, uint instructionOffset, out GenericContextStorage storage);
    TargetPointer GetAmbientSP(uint codeOffset, TargetPointer fp, TargetPointer sp);
}
