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
//  - location of Il-visible locals and other important state on the 
//    original (Tier0) method frame
//  - total size of the original frame, and SP-FP delta
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
    void Initialize(unsigned localCount, int fpToSpDelta)
    {
        m_fpToSpDelta             = fpToSpDelta;
        m_numberOfLocals          = localCount;
        m_genericContextArgOffset = -1;
        m_keptAliveThisOffset     = -1;
        m_securityCookieOffset    = -1;
    }

    // Total size of this patchpoint info record, in bytes
    unsigned PatchpointInfoSize() const
    {
        return ComputeSize(m_numberOfLocals);
    }

    // FP to SP delta of the original method
    int FpToSpDelta() const
    {
        return m_fpToSpDelta;
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

    // True if this local was address exposed in the original method
    bool IsExposed(unsigned localNum) const
    {
        return ((m_offsetAndExposureData[localNum] & EXPOSURE_MASK) != 0);
    }

    void SetIsExposed(unsigned localNum)
    {
        m_offsetAndExposureData[localNum] |= EXPOSURE_MASK;
    }

    // FP relative offset of this local in the original method
    int Offset(unsigned localNum) const
    {
        return (m_offsetAndExposureData[localNum] & ~EXPOSURE_MASK);
    }

    void SetOffset(unsigned localNum, int offset)
    {
        m_offsetAndExposureData[localNum] = offset;
    }

private:
    enum
    {
        EXPOSURE_MASK = 0x1
    };

    unsigned m_numberOfLocals;
    int      m_fpToSpDelta;
    int      m_genericContextArgOffset;
    int      m_keptAliveThisOffset;
    int      m_securityCookieOffset;
    int      m_offsetAndExposureData[];
};

typedef DPTR(struct PatchpointInfo) PTR_PatchpointInfo;

#endif // _PATCHPOINTINFO_H_
