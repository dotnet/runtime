// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef _PDBHEAP_H_
#define _PDBHEAP_H_

#if _MSC_VER >= 1100
#pragma once
#endif

#include "metamodel.h"
#include "portablepdbmdds.h"

/* Simple storage class (similar to StgPool) holding pdbstream data
** for portable PDB metadata.
*/ 
class PdbHeap
{
public:
    PdbHeap();
    ~PdbHeap();

    __checkReturn HRESULT SetData(PORT_PDB_STREAM* data);
    __checkReturn HRESULT SaveToStream(IStream* stream);
    BOOL    IsEmpty();
    ULONG   GetSize();

private:
    BYTE* m_data;
    ULONG m_size;
};

#endif
