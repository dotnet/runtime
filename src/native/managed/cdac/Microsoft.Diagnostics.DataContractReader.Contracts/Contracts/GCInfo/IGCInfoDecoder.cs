// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Diagnostics.DataContractReader.Contracts.GCInfoHelpers;

internal interface IGCInfoDecoder : IGCInfoHandle
{
    uint GetCodeLength();
    uint StackBaseRegister { get; }

    /// <summary>
    /// Enumerates all live GC slots at the given instruction offset.
    /// </summary>
    /// <param name="instructionOffset">Relative offset from method start.</param>
    /// <param name="inputFlags">CodeManagerFlags controlling reporting.</param>
    /// <param name="reportSlot">Callback: (isRegister, registerNumber, spOffset, spBase, gcFlags).</param>
    bool EnumerateLiveSlots(
        uint instructionOffset,
        uint inputFlags,
        LiveSlotCallback reportSlot) => throw new NotImplementedException();
}

internal delegate void LiveSlotCallback(bool isRegister, uint registerNumber, int spOffset, uint spBase, uint gcFlags);
