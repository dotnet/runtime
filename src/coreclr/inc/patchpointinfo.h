// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// --------------------------------------------------------------------------------
// patchpointinfo.h
// --------------------------------------------------------------------------------

#include <clrtypes.h>

#ifndef JIT_BUILD
#include "cdacdata.h"
#endif // JIT_BUILD

#ifndef _PATCHPOINTINFO_H_
#define _PATCHPOINTINFO_H_

// --------------------------------------------------------------------------------
// Describes information needed to make an OSR transition
//  - location of IL-visible locals and other important state on the
//    original (Tier0) method frame, with respect to top of frame
//    (hence these offsets will be negative as stack grows down)
//  - total size of the original frame
//  - callee save registers saved on the original (Tier0) frame
//
// Currently the patchpoint info is independent of the IL offset of the patchpoint.
//
// This data is produced when jitting a Tier0 method with OSR enabled, and consumed
// by the Tier1/OSR jit request.
//
struct PatchpointInfo
{
    // Determine how much storage is needed to hold this info
    static uint32_t ComputeSize(uint32_t localCount)
    {
        uint32_t baseSize     = sizeof(PatchpointInfo);
        uint32_t variableSize = localCount * sizeof(int32_t);
        uint32_t totalSize    = baseSize + variableSize;
        return totalSize;
    }

    // Initialize
    void Initialize(uint32_t localCount, int32_t totalFrameSize)
    {
        m_calleeSaveRegisters     = 0;
        m_tier0Version            = 0;
        m_totalFrameSize          = totalFrameSize;
        m_numberOfLocals          = localCount;
        m_genericContextArgOffset = -1;
        m_keptAliveThisOffset     = -1;
        m_securityCookieOffset    = -1;
        m_monitorAcquiredOffset   = -1;
    }

    // Copy
    void Copy(const PatchpointInfo* original)
    {
        m_calleeSaveRegisters = original->m_calleeSaveRegisters;
        m_tier0Version = original->m_tier0Version;
        m_genericContextArgOffset = original->m_genericContextArgOffset;
        m_keptAliveThisOffset = original->m_keptAliveThisOffset;
        m_securityCookieOffset = original->m_securityCookieOffset;
        m_monitorAcquiredOffset = original->m_monitorAcquiredOffset;

        for (uint32_t i = 0; i < original->m_numberOfLocals; i++)
        {
            m_offsetAndExposureData[i] = original->m_offsetAndExposureData[i];
        }
    }

    // Total size of this patchpoint info record, in bytes
    uint32_t PatchpointInfoSize() const
    {
        return ComputeSize(m_numberOfLocals);
    }

    // Total frame size of the original method
    int32_t TotalFrameSize() const
    {
        return m_totalFrameSize;
    }

    // Number of locals in the original method (including special locals)
    uint32_t NumberOfLocals() const
    {
        return m_numberOfLocals;
    }

    // Original method caller SP offset for generic context arg
    int32_t GenericContextArgOffset() const
    {
        return m_genericContextArgOffset;
    }

    bool HasGenericContextArgOffset() const
    {
        return m_genericContextArgOffset != -1;
    }

    void SetGenericContextArgOffset(int32_t offset)
    {
        m_genericContextArgOffset = offset;
    }

    // Original method FP relative offset for kept-alive this
    int32_t KeptAliveThisOffset() const
    {
        return m_keptAliveThisOffset;
    }

    bool HasKeptAliveThis() const
    {
        return m_keptAliveThisOffset != -1;
    }

    void SetKeptAliveThisOffset(int32_t offset)
    {
        m_keptAliveThisOffset = offset;
    }

    // Original method FP relative offset for security cookie
    int32_t SecurityCookieOffset() const
    {
        return m_securityCookieOffset;
    }

    bool HasSecurityCookie() const
    {
        return m_securityCookieOffset != -1;
    }

    void SetSecurityCookieOffset(int32_t offset)
    {
        m_securityCookieOffset = offset;
    }

    // Original method FP relative offset for monitor acquired flag
    int32_t MonitorAcquiredOffset() const
    {
        return m_monitorAcquiredOffset;
    }

    bool HasMonitorAcquired() const
    {
        return m_monitorAcquiredOffset != -1;
    }

    void SetMonitorAcquiredOffset(int32_t offset)
    {
        m_monitorAcquiredOffset = offset;
    }

    // True if this local was address exposed in the original method
    bool IsExposed(uint32_t localNum) const
    {
        return ((m_offsetAndExposureData[localNum] & EXPOSURE_MASK) != 0);
    }

    // FP relative offset of this local in the original method
    int32_t Offset(uint32_t localNum) const
    {
        return (m_offsetAndExposureData[localNum] >> OFFSET_SHIFT);
    }

    void SetOffsetAndExposure(uint32_t localNum, int32_t offset, bool isExposed)
    {
        m_offsetAndExposureData[localNum] = (offset << OFFSET_SHIFT) | (isExposed ? EXPOSURE_MASK : 0);
    }

    // Callee save registers saved by the original method.
    // Includes all saves that must be restored (eg includes pushed RBP on x64).
    //
    uint64_t CalleeSaveRegisters() const
    {
        return m_calleeSaveRegisters;
    }

    void SetCalleeSaveRegisters(uint64_t registerMask)
    {
        m_calleeSaveRegisters = registerMask;
    }

    PCODE GetTier0EntryPoint() const
    {
        return m_tier0Version;
    }

    void SetTier0EntryPoint(PCODE ip)
    {
        m_tier0Version = ip;
    }

private:
    enum
    {
        OFFSET_SHIFT = 0x1,
        EXPOSURE_MASK = 0x1
    };

    uint64_t m_calleeSaveRegisters;
    PCODE    m_tier0Version;
    uint32_t m_numberOfLocals;
    int32_t      m_totalFrameSize;
    int32_t      m_genericContextArgOffset;
    int32_t      m_keptAliveThisOffset;
    int32_t      m_securityCookieOffset;
    int32_t      m_monitorAcquiredOffset;
    int32_t      m_offsetAndExposureData[];

#ifndef JIT_BUILD
    friend struct ::cdac_data<PatchpointInfo>;
#endif // JIT_BUILD
};

#ifndef JIT_BUILD
template<>
struct cdac_data<PatchpointInfo>
{
    static constexpr size_t LocalCount = offsetof(PatchpointInfo, m_numberOfLocals);
};
#endif // JIT_BUILD

typedef DPTR(struct PatchpointInfo) PTR_PatchpointInfo;

#endif // _PATCHPOINTINFO_H_
