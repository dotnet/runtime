// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// --------------------------------------------------------------------------------
// patchpointinfo.h
// --------------------------------------------------------------------------------

#include <clrtypes.h>

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
    static unsigned ComputeSize(unsigned localCount)
    {
        unsigned baseSize     = sizeof(PatchpointInfo);
        unsigned variableSize = localCount * sizeof(int);
        unsigned totalSize    = baseSize + variableSize;
        return totalSize;
    }

    // Initialize
    void Initialize(unsigned localCount, int totalFrameSize)
    {
        m_calleeSaveRegisters     = 0;
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
        m_genericContextArgOffset = original->m_genericContextArgOffset;
        m_keptAliveThisOffset = original->m_keptAliveThisOffset;
        m_securityCookieOffset = original->m_securityCookieOffset;
        m_monitorAcquiredOffset = original->m_monitorAcquiredOffset;

        for (unsigned i = 0; i < original->m_numberOfLocals; i++)
        {
            m_offsetAndExposureData[i] = original->m_offsetAndExposureData[i];
        }
    }

    // Total size of this patchpoint info record, in bytes
    unsigned PatchpointInfoSize() const
    {
        return ComputeSize(m_numberOfLocals);
    }

    // Total frame size of the original method
    int TotalFrameSize() const
    {
        return m_totalFrameSize;
    }

    // Number of locals in the original method (including special locals)
    unsigned NumberOfLocals() const
    {
        return m_numberOfLocals;
    }

    // Original method caller SP offset for generic context arg
    int GenericContextArgOffset() const
    {
        return m_genericContextArgOffset;
    }

    bool HasGenericContextArgOffset() const
    {
        return m_genericContextArgOffset != -1;
    }

    void SetGenericContextArgOffset(int offset)
    {
        m_genericContextArgOffset = offset;
    }

    // Original method FP relative offset for kept-alive this
    int KeptAliveThisOffset() const
    {
        return m_keptAliveThisOffset;
    }

    bool HasKeptAliveThis() const
    {
        return m_keptAliveThisOffset != -1;
    }

    void SetKeptAliveThisOffset(int offset)
    {
        m_keptAliveThisOffset = offset;
    }

    // Original method FP relative offset for security cookie
    int SecurityCookieOffset() const
    {
        return m_securityCookieOffset;
    }

    bool HasSecurityCookie() const
    {
        return m_securityCookieOffset != -1;
    }

    void SetSecurityCookieOffset(int offset)
    {
        m_securityCookieOffset = offset;
    }

    // Original method FP relative offset for monitor acquired flag
    int MonitorAcquiredOffset() const
    {
        return m_monitorAcquiredOffset;
    }

    bool HasMonitorAcquired() const
    {
        return m_monitorAcquiredOffset != -1;
    }

    void SetMonitorAcquiredOffset(int offset)
    {
        m_monitorAcquiredOffset = offset;
    }

    // True if this local was address exposed in the original method
    bool IsExposed(unsigned localNum) const
    {
        return ((m_offsetAndExposureData[localNum] & EXPOSURE_MASK) != 0);
    }

    // FP relative offset of this local in the original method
    int Offset(unsigned localNum) const
    {
        return (m_offsetAndExposureData[localNum] >> OFFSET_SHIFT);
    }

    void SetOffsetAndExposure(unsigned localNum, int offset, bool isExposed)
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

private:
    enum
    {
        OFFSET_SHIFT = 0x1,
        EXPOSURE_MASK = 0x1
    };

    uint64_t m_calleeSaveRegisters;
    unsigned m_numberOfLocals;
    int      m_totalFrameSize;
    int      m_genericContextArgOffset;
    int      m_keptAliveThisOffset;
    int      m_securityCookieOffset;
    int      m_monitorAcquiredOffset;
    int      m_offsetAndExposureData[];
};

typedef DPTR(struct PatchpointInfo) PTR_PatchpointInfo;

#endif // _PATCHPOINTINFO_H_
