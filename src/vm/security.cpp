// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// 

#include "common.h"

#include "security.h"

//
// The method in this file have nothing to do with security. They historically lived in security subsystem.
// TODO: Move them to move appropriate place.
//

void Security::CopyByteArrayToEncoding(IN U1ARRAYREF* pArray, OUT PBYTE* ppbData, OUT DWORD* pcbData)
{
    CONTRACTL {
        THROWS;
        GC_NOTRIGGER;
        MODE_COOPERATIVE;
        PRECONDITION(CheckPointer(pArray));
        PRECONDITION(CheckPointer(ppbData));
        PRECONDITION(CheckPointer(pcbData));
        PRECONDITION(*pArray != NULL);
    } CONTRACTL_END;

    DWORD size = (DWORD) (*pArray)->GetNumComponents();
    *ppbData = new BYTE[size];
    *pcbData = size;
        
    CopyMemory(*ppbData, (*pArray)->GetDirectPointerToNonObjectElements(), size);
}

void Security::CopyEncodingToByteArray(IN PBYTE   pbData, IN DWORD   cbData, IN OBJECTREF* pArray)
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    } CONTRACTL_END;

    U1ARRAYREF pObj;
    _ASSERTE(pArray);

    pObj = (U1ARRAYREF)AllocatePrimitiveArray(ELEMENT_TYPE_U1,cbData);
    memcpyNoGCRefs(pObj->m_Array, pbData, cbData);
    *pArray = (OBJECTREF) pObj;
}
