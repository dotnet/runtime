// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Microsoft.Diagnostics.DataContractReader.Contracts;

namespace Microsoft.Diagnostics.DataContractReader.Contracts.GCInfoHelpers;

internal interface IGCInfoDecoder : IGCInfoHandle
{
    uint GetCodeLength();
    uint GetStackBaseRegister();
    uint GetSizeOfStackParameterArea();

    // Default 0; mirrors native EECodeManager::GetStackParameterSize, which only
    // returns non-zero on x86 (where managed code uses __stdcall, callee-popped args).
    uint GetCalleePoppedArgumentsSize() => 0;

    IReadOnlyList<InterruptibleRange> GetInterruptibleRanges();
    IReadOnlyList<LiveSlot> EnumerateLiveSlots(uint instructionOffset, GcSlotEnumerationOptions options);
    bool IsGcSafe(uint instructionOffset);
    bool TryGetGenericInstantiationContextStackSlot(out int spOffset, out bool isStackBaseRelative);
    TargetPointer GetAmbientSP(uint codeOffset, TargetPointer fp, TargetPointer sp);
}
