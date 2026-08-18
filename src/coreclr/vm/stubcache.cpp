// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// File: stubcache.cpp
//

//
// Base class for caching stubs.
//


#include "common.h"
#include "stubcache.h"
#include "stublink.h"
#include "cgensys.h"
#include "excep.h"

//---------------------------------------------------------
// Constructor
//---------------------------------------------------------
StubCacheBase::StubCacheBase(LoaderAllocator *pLoaderAllocator) :
    CClosedHashBase(
#ifdef _DEBUG
                      3,
#else
                      17,    // CClosedHashTable will grow as necessary
#endif

                      sizeof(STUBHASHENTRY),
                      FALSE
                   ),
    m_crst(CrstStubCache),
    m_pLoaderAllocator(pLoaderAllocator)
{
    WRAPPER_NO_CONTRACT;
}


//---------------------------------------------------------
// Destructor
//---------------------------------------------------------
StubCacheBase::~StubCacheBase()
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    STUBHASHENTRY *phe = (STUBHASHENTRY*)GetFirst();
    while (phe)
    {
        _ASSERTE((PCODE)NULL != phe->m_pCode);
        phe = (STUBHASHENTRY*)GetNext((BYTE*)phe);
    }
}



//---------------------------------------------------------
// Returns the equivalent hashed code, creating a new hash
// entry if necessary. If the latter, will call out to CompileStub.
//---------------------------------------------------------
PCODE StubCacheBase::Canonicalize(const BYTE * pRawStub, const char *stubType)
{
    CONTRACTL
    {
        STANDARD_VM_CHECK;
    }
    CONTRACTL_END;

    STUBHASHENTRY *phe = NULL;

    PCODE pCode = (PCODE)NULL;
    {
        CrstHolder ch(&m_crst);

        // Try to find the stub
        phe = (STUBHASHENTRY*)Find((LPVOID)pRawStub);
        if (phe)
        {
            return phe->m_pCode;
        }
    }

    // Couldn't find it, let's try to compile it.
    CPUSTUBLINKER sl;
    CPUSTUBLINKER *psl = &sl;
    StubCodeBlockKind kind = CompileStub(pRawStub, psl);

    // Append the raw stub to the native stub
    // and link up the stub.
    CodeLabel *plabel = psl->EmitNewCodeLabel();
    psl->EmitBytes(pRawStub, Length(pRawStub));
    pCode = psl->Link(m_pLoaderAllocator, kind, stubType);
    UINT32 offset = psl->GetLabelOffset(plabel);

    if (offset > 0xffff)
        COMPlusThrowOM();

    {
        CrstHolder ch(&m_crst);

        bool bNew;
        phe = (STUBHASHENTRY*)FindOrAdd((LPVOID)pRawStub, /*modifies*/bNew);
        if (phe)
        {
            if (bNew)
            {
                phe->m_pCode = pCode;
                phe->m_offsetOfRawStub = (UINT16)offset;

                AddStub(pRawStub, pCode);
            }
            else
            {
                // If we got here, some other thread got in
                // and enregistered an identical stub during
                // the window in which we were out of the m_crst.

                //Under DEBUG, two identical ML streams can actually compile
                // to different compiled stubs due to the checked build's
                // toggling between inlined TLSGetValue and api TLSGetValue.
                //_ASSERTE(phe->m_offsetOfRawStub == (UINT16)offset);

                // Use the previously created stub
                pCode = phe->m_pCode;
            }
        }
    }

    if (!phe)
    {
        // Couldn't grow hash table due to lack of memory.
        COMPlusThrowOM();
    }

    return pCode;
}


void StubCacheBase::AddStub(const BYTE* pRawStub, PCODE pNewStub)
{
    LIMITED_METHOD_CONTRACT;

    // By default, don't do anything.
}


//*****************************************************************************
// Hash is called with a pointer to an element in the table.  You must override
// this method and provide a hash algorithm for your element type.
//*****************************************************************************
unsigned int StubCacheBase::Hash(             // The key value.
    void const  *pData)                      // Raw data to hash.
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    const BYTE *pRawStub = (const BYTE *)pData;

    UINT cb = Length(pRawStub);
    int   hash = 0;
    while (cb--)
        hash = _rotl(hash,1) + *(pRawStub++);

    return hash;
}

//*****************************************************************************
// Compare is used in the typical memcmp way, 0 is eqaulity, -1/1 indicate
// direction of miscompare.  In this system everything is always equal or not.
//*****************************************************************************
unsigned int StubCacheBase::Compare(          // 0, -1, or 1.
    void const  *pData,                 // Raw key data on lookup.
    BYTE        *pElement)            // The element to compare data against.
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    const BYTE *pRawStub1  = (const BYTE *)pData;
    const BYTE *pRawStub2  = (const BYTE *)GetKey(pElement);
    UINT cb1 = Length(pRawStub1);
    UINT cb2 = Length(pRawStub2);

    if (cb1 != cb2)
        return 1; // not equal
    else
    {
        while (cb1--)
        {
            if (*(pRawStub1++) != *(pRawStub2++))
                return 1; // not equal
        }
        return 0;
    }
}

//*****************************************************************************
// Return true if the element is free to be used.
//*****************************************************************************
CClosedHashBase::ELEMENTSTATUS StubCacheBase::Status(           // The status of the entry.
    BYTE        *pElement)           // The element to check.
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    PCODE pCode = ((STUBHASHENTRY*)pElement)->m_pCode;

    if (pCode == (PCODE)NULL)
        return FREE;
    else if (pCode == (PCODE)-1)
        return DELETED;
    else
        return USED;
}

//*****************************************************************************
// Sets the status of the given element.
//*****************************************************************************
void StubCacheBase::SetStatus(
    BYTE        *pElement,              // The element to set status for.
    ELEMENTSTATUS eStatus)            // New status.
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    STUBHASHENTRY *phe = (STUBHASHENTRY*)pElement;

    switch (eStatus)
    {
        case FREE:    phe->m_pCode = (PCODE)NULL;   break;
        case DELETED: phe->m_pCode = (PCODE)-1; break;
        default:
            _ASSERTE(!"MLCacheEntry::SetStatus(): Bad argument.");
    }
}

//*****************************************************************************
// Returns the internal key value for an element.
//*****************************************************************************
void *StubCacheBase::GetKey(                   // The data to hash on.
    BYTE        *pElement)           // The element to return data ptr for.
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    STUBHASHENTRY *phe = (STUBHASHENTRY*)pElement;
    return (void *)(PCODEToPINSTR(phe->m_pCode) + phe->m_offsetOfRawStub);
}
