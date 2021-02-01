// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// File: stubcache.h
//

//
// Base class for caching stubs.
//


#ifndef __mlcache_h__
#define __mlcache_h__


#include "vars.hpp"
#include "util.hpp"
#include "crst.h"

class Stub;
class StubLinker;

class StubCacheBase : private CClosedHashBase
{
private:
    //---------------------------------------------------------
    // Hash entry for CClosedHashBase.
    //---------------------------------------------------------
    struct STUBHASHENTRY
    {
        // Values:
        //   NULL  = free
        //   -1    = deleted
        //   other = used
        Stub    *m_pStub;

        // Offset where the RawStub begins (the RawStub can be
        // preceded by native stub code.)
        UINT16   m_offsetOfRawStub;
    };


public:
    //---------------------------------------------------------
    // Constructor
    //---------------------------------------------------------
    StubCacheBase(LoaderHeap *heap = 0);

    //---------------------------------------------------------
    // Destructor
    //---------------------------------------------------------
    virtual ~StubCacheBase();

    //---------------------------------------------------------
    // Returns the equivalent hashed Stub, creating a new hash
    // entry if necessary. If the latter, will call out to CompileStub.
    //
    // Throws on out of memory or other fatal error.
    //---------------------------------------------------------
    Stub *Canonicalize(const BYTE *pRawStub);

protected:
    //---------------------------------------------------------
    // OVERRIDE.
    // Compile a native (ASM) version of the stub.
    //
    // This method should compile into the provided stublinker (but
    // not call the Link method.)
    //
    // It should return the chosen compilation mode.
    //
    // If the method fails for some reason, it should return
    // INTERPRETED so that the EE can fall back on the already
    // created ML code.
    //---------------------------------------------------------
    virtual void CompileStub(const BYTE *pRawStub,
                             StubLinker *psl) = 0;

    //---------------------------------------------------------
    // OVERRIDE
    // Tells the StubCacheBase the length of a stub.
    //---------------------------------------------------------
    virtual UINT Length(const BYTE *pRawStub) = 0;

    //---------------------------------------------------------
    // OVERRIDE (OPTIONAL)
    // Notifies the various derived classes that a new stub has been created
    //---------------------------------------------------------
    virtual void AddStub(const BYTE* pRawStub, Stub* pNewStub);


private:
    // *** OVERRIDES FOR CClosedHashBase ***/

    //*****************************************************************************
    // Hash is called with a pointer to an element in the table.  You must override
    // this method and provide a hash algorithm for your element type.
    //*****************************************************************************
    virtual unsigned int Hash(             // The key value.
        void const  *pData);                // Raw data to hash.

    //*****************************************************************************
    // Compare is used in the typical memcmp way, 0 is eqaulity, -1/1 indicate
    // direction of miscompare.  In this system everything is always equal or not.
    //*****************************************************************************
    virtual unsigned int Compare(          // 0, -1, or 1.
        void const  *pData,                 // Raw key data on lookup.
        BYTE        *pElement);             // The element to compare data against.

    //*****************************************************************************
    // Return true if the element is free to be used.
    //*****************************************************************************
    virtual ELEMENTSTATUS Status(           // The status of the entry.
        BYTE        *pElement);             // The element to check.

    //*****************************************************************************
    // Sets the status of the given element.
    //*****************************************************************************
    virtual void SetStatus(
        BYTE        *pElement,              // The element to set status for.
        ELEMENTSTATUS eStatus);             // New status.

    //*****************************************************************************
    // Returns the internal key value for an element.
    //*****************************************************************************
    virtual void *GetKey(                   // The data to hash on.
        BYTE        *pElement);             // The element to return data ptr for.




private:
    Crst        m_crst;
    LoaderHeap* m_heap;
};


#endif // __mlcache_h__
