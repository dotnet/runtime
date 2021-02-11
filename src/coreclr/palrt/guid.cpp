// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

//
// ===========================================================================
// File: guid.cpp
//
// PALRT guids
// ===========================================================================

#define INITGUID
#include <guiddef.h>

// These are GUIDs and IIDs that would normally be provided by the system via uuid.lib,
// and that the PALRT exposes through headers.

DEFINE_GUID(GUID_NULL, 0x00000000, 0x0000, 0x0000, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00);
DEFINE_GUID(IID_IUnknown, 0x00000000, 0x0000, 0x0000, 0xC0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x46);
DEFINE_GUID(IID_IClassFactory, 0x00000001, 0x0000, 0x0000, 0xC0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x46);


// objidl.idl
DEFINE_GUID(IID_ISequentialStream, 0x0c733a30, 0x2a1c, 0x11ce, 0xad, 0xe5, 0x00, 0xaa, 0x00, 0x44, 0x77, 0x3d);
DEFINE_GUID(IID_IStream, 0x0000000c, 0x0000, 0x0000, 0xC0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x46);

// Create a random guid based on the https://www.ietf.org/rfc/rfc4122.txt
STDAPI
CoCreateGuid(OUT GUID * pguid)
{
    PAL_Random(pguid, sizeof(GUID));

    static const USHORT VersionMask = 0xF000;
    static const USHORT RandomGuidVersion = 0x4000;

    static const BYTE ClockSeqHiAndReservedMask = 0xC0;
    static const BYTE ClockSeqHiAndReservedValue = 0x80;

    // Modify bits indicating the type of the GUID

    // time_hi_and_version
    pguid->Data3 = (pguid->Data3 & ~VersionMask) | RandomGuidVersion;
    // clock_seq_hi_and_reserved
    pguid->Data4[0] = (pguid->Data4[0] & ~ClockSeqHiAndReservedMask) | ClockSeqHiAndReservedValue;

    return S_OK;
}
