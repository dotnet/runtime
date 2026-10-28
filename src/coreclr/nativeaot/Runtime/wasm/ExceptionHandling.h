// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

struct WasmEHFrame
{
    // Keep in sync with TERMINATE_R2R_STACK_WALK in src\coreclr\vm\wasm\callhelpers.hpp.
    // CoreCLR stops R2R walking at this marker; NativeAOT follows m_callerSP.
    static constexpr uintptr_t Identity = 1;

    uintptr_t m_identity;
    uintptr_t m_returnIP;
    uintptr_t m_callerSP;
    uintptr_t m_padding;
};

static_assert(sizeof(WasmEHFrame) == 16);
