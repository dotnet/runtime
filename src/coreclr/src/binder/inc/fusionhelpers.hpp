// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ============================================================
//
// FusionHelpers.hpp
//
// Defines various legacy fusion types
//
// ============================================================

#ifndef __FUSION_HELPERS_HPP__
#define __FUSION_HELPERS_HPP__

#include "clrtypes.h"
#include "sstring.h"

#include "clrhost.h"
#include "shlwapi.h"
#include "winwrap.h"
#include "ex.h"
#include "fusion.h"


#include "peinformation.h"

#define FUSION_NEW_SINGLETON(_type) new (nothrow) _type
#define FUSION_NEW_ARRAY(_type, _n) new (nothrow) _type[_n]
#define FUSION_DELETE_ARRAY(_ptr) if((_ptr)) delete [] (_ptr)
#define FUSION_DELETE_SINGLETON(_ptr) if((_ptr)) delete (_ptr)

#define SAFEDELETE(p) if ((p) != NULL) { FUSION_DELETE_SINGLETON((p)); (p) = NULL; };
#define SAFEDELETEARRAY(p) if ((p) != NULL) { FUSION_DELETE_ARRAY((p)); (p) = NULL; };
#define SAFERELEASE(p) if ((p) != NULL) { (p)->Release(); (p) = NULL; };

#ifndef NEW
#define NEW(_type) FUSION_NEW_SINGLETON(_type)
#endif // !NEW

#ifndef ARRAYSIZE
#define ARRAYSIZE(a) (sizeof(a)/sizeof(a[0]))
#endif // !ARRAYSIZE

#define MAX_VERSION_DISPLAY_SIZE  sizeof("65535.65535.65535.65535")

#define ASM_DISPLAYF_DEFAULT   (ASM_DISPLAYF_VERSION   \
                                |ASM_DISPLAYF_CULTURE   \
                                |ASM_DISPLAYF_PUBLIC_KEY_TOKEN  \
                                |ASM_DISPLAYF_RETARGET)

#define SIGNATURE_BLOB_LENGTH      0x80
#define SIGNATURE_BLOB_LENGTH_HASH 0x14
#define MVID_LENGTH                sizeof(GUID)

#define PUBLIC_KEY_TOKEN_LEN            8

inline
WCHAR*
WSTRDupDynamic(LPCWSTR pwszSrc)
{
    LPWSTR pwszDest = NULL;
    if (pwszSrc != NULL)
    {
        const size_t dwLen = wcslen(pwszSrc) + 1;
        pwszDest = FUSION_NEW_ARRAY(WCHAR, dwLen);

        if( pwszDest )
            memcpy(pwszDest, pwszSrc, dwLen * sizeof(WCHAR));
    }

    return pwszDest;
}

#define MAX_URL_LENGTH 2084 // same as INTERNET_MAX_URL_LENGTH

// bit mask macro helpers
#define MAX_ID_FROM_MASK(size)          ((size) << 3)
#define MASK_SIZE_FROM_ID(id)           ((id) >> 3)
#define IS_IN_RANGE(id, size)   ((id) <= ((size) << 3))
#define IS_BIT_SET(id, mask)    (mask[((id)-1)>>3] & (0x1 << (((id)-1)&0x7)))
#define SET_BIT(id, mask)       (mask[((id)-1)>>3] |= (0x1<< (((id)-1)&0x7)))
#define UNSET_BIT(id, mask)     (mask[((id)-1)>>3] &= (0xFF - (0x1<<(((id)-1)&0x7))))

inline
int FusionCompareStringI(LPCWSTR pwz1, LPCWSTR pwz2)
{
    return SString::_wcsicmp(pwz1, pwz2);
}

#endif
