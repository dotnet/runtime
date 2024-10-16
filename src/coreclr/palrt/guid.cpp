// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

//
// ===========================================================================
// File: guid.cpp
//
// PALRT guids
// ===========================================================================

#include <guiddef.h>
#include <minipal/guid.h>

STDAPI
CoCreateGuid(OUT GUID * pguid)
{
    minipal_guid_t guid;
    if (!minipal_guid_v4_create(&guid))
    {
        return E_FAIL;
    }

    static_assert(sizeof(GUID) == sizeof(minipal_guid_t), "GUID and minipal_guid_t must be the same size");
    memcpy(pguid, &guid, sizeof(GUID));

    return S_OK;
}
