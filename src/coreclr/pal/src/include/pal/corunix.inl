// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*++



Module Name:



--*/

#ifndef _CORUNIX_INL
#define _CORUNIX_INL

#include "corunix.hpp"
#include "dbgmsg.h"

namespace CorUnix
{

    bool CAllowedObjectTypes::IsTypeAllowed(PalObjectTypeId eTypeId)
    {
        _ASSERTE(eTypeId != ObjectTypeIdCount);
        return m_rgfAllowedTypes[eTypeId];
    };

    CAllowedObjectTypes::CAllowedObjectTypes(
        PalObjectTypeId rgAllowedTypes[],
        DWORD dwAllowedTypeCount
    )
    {
        ZeroMemory(m_rgfAllowedTypes, sizeof(m_rgfAllowedTypes));
        for (DWORD dw = 0; dw < dwAllowedTypeCount; dw += 1)
        {
            _ASSERTE(rgAllowedTypes[dw] != ObjectTypeIdCount);
            m_rgfAllowedTypes[rgAllowedTypes[dw]] = TRUE;
        }
    };

    CAllowedObjectTypes::CAllowedObjectTypes(
       PalObjectTypeId eAllowedType
       )
    {
        ZeroMemory(m_rgfAllowedTypes, sizeof(m_rgfAllowedTypes));

        _ASSERTE(eAllowedType != ObjectTypeIdCount);
        m_rgfAllowedTypes[eAllowedType] = TRUE;
    };
}

#endif // _CORUNIX_H

