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
// riscv64), so this file no longer reasons about it.
//
// GPR register VALUES come from the cached target CONTEXT byte buffer
// (m_pContext) via a single batched IDacDbiInterface::ReadRegistersFromContext
// call. Float / SIMD register VALUES come from the thread's float cache
// (IDacDbiInterface::ReadFloatRegistersFromContext, loaded up front so the
// requested bits can be partitioned into a GPR batch and a float batch).
//
// The single remaining per-arch wart here is the x87 stack-relative slot
// inversion on x86 (REGISTER_X86_FPSTACK_N points to a stack-relative
// position, not a physical slot).
//
//*****************************************************************************
#include "stdafx.h"
#include "primitives.h"

namespace
{
    // Read the float-state cache, loading it on demand. Returns S_OK if the
    // cache is populated (or there's nothing to do); failure HRESULT if
    // LoadFloatState failed.
    HRESULT EnsureFloatStateLoaded(CordbThread * pThread)
    {
        if (pThread->m_fFloatStateValid)
            return S_OK;

        HRESULT hr = S_OK;
        EX_TRY
        {
            pThread->LoadFloatState();
        }
        EX_CATCH_HRESULT(hr);
        if (FAILED(hr))
            return hr;

        LOG((LF_CORDB, LL_INFO1000, "CRS::GR: Loaded float state\n"));
        return S_OK;
    }

    // Buffer size for the byte-form availability mask. arm64 needs 9 bytes
    // (REGISTER_ARM64_V31 is bit 64); 16 bytes covers every arch with room
    // to spare without forcing dynamic allocation.
    constexpr ULONG32 kAvailMaskBytes = 16;

    // Upper bound on distinct register bit positions (kAvailMaskBytes * 8),
    // used to size the batched GPR request/value arrays on the stack.
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
    // fills regBuffer in ascending bit order. The requested bits are partitioned
    // into two batches: GPR bits are read in a single ReadRegistersFromContext
    // call, and float bits are served from the thread's float cache.
    HRESULT FillRegisters(IDacDbiInterface * pDAC,
                          ContextBuffer contextBuffer,
                          CordbThread * pThread,
                          const BYTE * requestMask, ULONG32 requestMaskCount,
                          const BYTE * availMask, ULONG32 availMaskCount,
                          ULONG32 regCount, CORDB_REGISTER regBuffer[])
    {
        // Validate request is a subset of available.
        for (ULONG32 b = 0; b < requestMaskCount * 8; b++)
        {
            if (!MaskBitSet(requestMask, requestMaskCount, (int)b))
                continue;
            if (!MaskBitSet(availMask, availMaskCount, (int)b))
                return E_INVALIDARG;
        }

        // Load the float cache up front so its register range can classify the
        // requested bits into GPR and float batches.
        HRESULT hr = EnsureFloatStateLoaded(pThread);
        bool areFloatsValid = !!SUCCEEDED(hr);

        int firstFloat = -1;
        int lastFloat  = -1;
        if (pThread->m_floatValuesCount > 0 && pThread->m_firstFloatReg >= 0 && areFloatsValid)
        {
            firstFloat = pThread->m_firstFloatReg;
            lastFloat  = firstFloat + (int)pThread->m_floatValuesCount - 1;
        }

        auto IsFloatBit = [firstFloat, lastFloat](int b)
        {
            return firstFloat >= 0 && b >= firstFloat && b <= lastFloat;
        };

        // First pass: collect the GPR register ids (ascending bit order) for the
        // batched read.
        CorDebugRegister gprRegs[kMaxRegisters];
        TADDR            gprValues[kMaxRegisters];
        ULONG32          gprCount = 0;
        for (ULONG32 b = 0; b < requestMaskCount * 8; b++)
        {
            if (!MaskBitSet(requestMask, requestMaskCount, (int)b))
                continue;
            if (IsFloatBit((int)b))
                continue;
            if (gprCount >= kMaxRegisters)
                return E_FAIL;
            gprRegs[gprCount++] = (CorDebugRegister)b;
        }

        if (gprCount > 0)
        {
            hr = pDAC->ReadRegistersFromContext(contextBuffer, gprRegs, gprCount, gprValues);
            if (FAILED(hr))
                return hr;
        }

        // Second pass: fill regBuffer in ascending bit order, consuming from the
        // GPR batch and the float cache.
        UINT    iRegister = 0;
        ULONG32 gprIdx    = 0;
        for (ULONG32 b = 0; b < requestMaskCount * 8 && iRegister < regCount; b++)
        {
            if (!MaskBitSet(requestMask, requestMaskCount, (int)b))
                continue;

            if (IsFloatBit((int)b))
            {
                ULONG32 idx = (ULONG32)((int)b - firstFloat);
#if defined(TARGET_X86)
                // x86 storage is pop-physical order: m_floatValues[0] = ST(0)
                // = top of stack. REGISTER_X86_FPSTACK_0 is the logical name
                // of the BOTTOM of the stack, so flip.
                idx = (ULONG32)(pThread->m_floatStackTop - ((int)b - REGISTER_X86_FPSTACK_0));
#endif
                memcpy(&regBuffer[iRegister++], &pThread->m_floatValues[idx], sizeof(CORDB_REGISTER));
            }
            else
            {
                regBuffer[iRegister++] = (CORDB_REGISTER)gprValues[gprIdx++];
            }
        }

        _ASSERTE(iRegister <= regCount);
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

    ContextBuffer contextBuffer = { m_pContext, m_contextSize };
    return FillRegisters(pDAC, contextBuffer, m_thread,
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

    ContextBuffer contextBuffer = { m_pContext, m_contextSize };
    return FillRegisters(pDAC, contextBuffer, m_thread,
                         mask, maskCount,
                         availBytes, kAvailMaskBytes, regCount, regBuffer);
}
