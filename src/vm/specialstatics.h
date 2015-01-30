//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

/*===========================================================================
**
** File:    SpecialStatics.h
**
**
** Purpose: Defines the data structures for context relative statics.
**          
**
**
=============================================================================*/
#ifndef _H_SPECIALSTATICS_
#define _H_SPECIALSTATICS_

// Data structure for storing special context relative static data.
typedef struct _STATIC_DATA
{
    DWORD           cElem;
    PTR_VOID        dataPtr[0];

#ifdef DACCESS_COMPILE
    static ULONG32 DacSize(TADDR addr)
    {
        DWORD cElem = *PTR_DWORD(addr);
        return offsetof(struct _STATIC_DATA, dataPtr) +
            cElem * sizeof(TADDR);
    }

    void EnumMemoryRegions(CLRDataEnumMemoryFlags flags);
#endif
    
} STATIC_DATA;
typedef SPTR(STATIC_DATA) PTR_STATIC_DATA;

typedef SimpleList<OBJECTHANDLE> ObjectHandleList;

#endif
