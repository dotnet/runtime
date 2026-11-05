// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//*****************************************************************************
// File: CordbRegisterSet.cpp
//
// Cross-platform implementation of CordbRegisterSet's ICorDebugRegisterSet /
// ICorDebugRegisterSet2 surface.
//
// Availability bits (for both GPRs and float / SIMD registers) come from a
// single byte-form DDI: IDacDbiInterface::GetAvailableRegistersMask. The DAC
// owns the per-arch policy that gates float visibility (active-only on
// x86 / amd64, suppressed on arm, always on for arm64 / loongarch64 /
// riscv64).
//
// Register VALUES (both GPR and float / SIMD) come from the single target
// CONTEXT byte buffer (m_contextBuffer) via one batched
// IDacDbiInterface::ReadRegistersFromContext call.
//
//*****************************************************************************
#include "stdafx.h"
#include "primitives.h"

namespace
{
    // Buffer size for the byte-form availability mask. arm64 needs 9 bytes
    // (REGISTER_ARM64_V31 is bit 64); 16 bytes covers every arch with room
    // to spare without forcing dynamic allocation.
    constexpr ULONG32 kAvailMaskBytes = 16;

    // Upper bound on distinct register bit positions (kAvailMaskBytes * 8),
    // used to size the batched request array on the stack.
    constexpr ULONG32 kMaxRegisters = kAvailMaskBytes * 8;

    inline bool MaskBitSet(const BYTE * mask, ULONG32 maskCount, int bit)
    {
        ULONG32 byteIdx = (ULONG32)bit / 8;
        if (byteIdx >= maskCount)
            return false;
        return (mask[byteIdx] & (BYTE)(1 << (bit % 8))) != 0;
    }

    // Shared GetRegisters core. Validates that every bit set in the caller's
    // request is also set in the available mask (E_INVALIDARG otherwise), then
    // fills regBuffer in ascending bit order by reading the target CONTEXT.
    HRESULT FillRegisters(IDacDbiInterface * pDAC,
                          ContextBuffer contextBuffer,
                          const BYTE * requestMask, ULONG32 requestMaskCount,
                          const BYTE * availMask, ULONG32 availMaskCount,
                          ULONG32 regCount, CORDB_REGISTER regBuffer[])
    {
        // Validate the request is a subset of the available registers.
        for (ULONG32 b = 0; b < requestMaskCount * 8; b++)
        {
            if (MaskBitSet(requestMask, requestMaskCount, (int)b) &&
                !MaskBitSet(availMask, availMaskCount, (int)b))
            {
                return E_INVALIDARG;
            }
        }

        // Collect the requested register ids (ascending bit order), bounded by
        // the caller's buffer, then read them all from the context at once.
        CorDebugRegister requestedRegs[kMaxRegisters];
        ULONG32          requestedCount = 0;
        for (ULONG32 b = 0; b < requestMaskCount * 8 && requestedCount < regCount; b++)
        {
            if (!MaskBitSet(requestMask, requestMaskCount, (int)b))
                continue;
            if (requestedCount >= kMaxRegisters)
                return E_FAIL;
            requestedRegs[requestedCount++] = (CorDebugRegister)b;
        }

        if (requestedCount > 0)
            return pDAC->ReadRegistersFromContext(contextBuffer, requestedRegs, requestedCount, regBuffer);

        return S_OK;
    }
} // anonymous namespace

HRESULT CordbRegisterSet::GetRegistersAvailable(ULONG64 * pAvailable)
{
    FAIL_IF_NEUTERED(this);
    VALIDATE_POINTER_TO_OBJECT(pAvailable, ULONG64 *);

    BYTE availBytes[kAvailMaskBytes] = {};
    IDacDbiInterface * pDAC = GetProcess()->GetDAC();
    HRESULT hr = pDAC->GetAvailableRegistersMask(
        m_active, m_quickUnwind, kAvailMaskBytes, availBytes);
    if (FAILED(hr))
        return hr;

    // Fold the low 64 bits into the ULONG64 surface. Bits at positions >= 64
    // (e.g. arm64 V31) are only reachable through the v2 byte-mask overload.
    ULONG64 mask = 0;
    for (int b = 0; b < 64; b++)
    {
        if (availBytes[b / 8] & (BYTE)(1 << (b % 8)))
            mask |= SETBITULONG64(b);
    }
    *pAvailable = mask;
    return S_OK;
}

HRESULT CordbRegisterSet::GetRegisters(ULONG64 mask, ULONG32 regCount,
                                       CORDB_REGISTER regBuffer[])
{
    PUBLIC_REENTRANT_API_ENTRY(this);
    FAIL_IF_NEUTERED(this);
    ATT_REQUIRE_STOPPED_MAY_FAIL(GetProcess());

    VALIDATE_POINTER_TO_OBJECT_ARRAY(regBuffer, CORDB_REGISTER, regCount, true, true);

    BYTE availBytes[kAvailMaskBytes] = {};
    IDacDbiInterface * pDAC = GetProcess()->GetDAC();
    HRESULT hr = pDAC->GetAvailableRegistersMask(
        m_active, m_quickUnwind, kAvailMaskBytes, availBytes);
    if (FAILED(hr))
        return hr;

    BYTE requestBytes[sizeof(ULONG64)] = {};
    for (int b = 0; b < 64; b++)
    {
        if (mask & SETBITULONG64(b))
            requestBytes[b / 8] |= (BYTE)(1 << (b % 8));
    }

    return FillRegisters(pDAC, m_contextBuffer,
                         requestBytes, (ULONG32)sizeof(requestBytes),
                         availBytes, kAvailMaskBytes, regCount, regBuffer);
}

HRESULT CordbRegisterSet::GetRegistersAvailable(ULONG32 regCount, BYTE pAvailable[])
{
    PUBLIC_REENTRANT_API_ENTRY(this);
    FAIL_IF_NEUTERED(this);
    VALIDATE_POINTER_TO_OBJECT_ARRAY(pAvailable, CORDB_REGISTER, regCount, true, true);

    IDacDbiInterface * pDAC = GetProcess()->GetDAC();
    return pDAC->GetAvailableRegistersMask(m_active, m_quickUnwind, regCount, pAvailable);
}

HRESULT CordbRegisterSet::GetRegisters(ULONG32 maskCount, BYTE mask[],
                                       ULONG32 regCount, CORDB_REGISTER regBuffer[])
{
    PUBLIC_REENTRANT_API_ENTRY(this);
    FAIL_IF_NEUTERED(this);
    ATT_REQUIRE_STOPPED_MAY_FAIL(GetProcess());

    VALIDATE_POINTER_TO_OBJECT_ARRAY(regBuffer, CORDB_REGISTER, regCount, true, true);

    BYTE availBytes[kAvailMaskBytes] = {};
    IDacDbiInterface * pDAC = GetProcess()->GetDAC();
    HRESULT hr = pDAC->GetAvailableRegistersMask(
        m_active, m_quickUnwind, kAvailMaskBytes, availBytes);
    if (FAILED(hr))
        return hr;

    return FillRegisters(pDAC, m_contextBuffer,
                         mask, maskCount,
                         availBytes, kAvailMaskBytes, regCount, regBuffer);
}
