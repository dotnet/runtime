// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//
// File: ObjectClone.cpp
// 

//


#include "common.h"

#ifdef FEATURE_REMOTING
#include "objectclone.h"
#include "frames.h"
#include "assembly.hpp"
#include "field.h"
#include "security.h"
#include "virtualcallstub.h"
#include "crossdomaincalls.h"
#include "callhelpers.h"
#include "jitinterface.h"
#include "typestring.h"
#include "typeparse.h"
#include "runtimehandles.h"
#include "appdomain.inl"

// Define the following to re-enable object cloner strict mode (where we require source fields for non-optional destination fields
// and don't attempt to load assemblies we can't find via display via partial names instead).
//#define OBJECT_CLONER_STRICT_MODE

void MakeIDeserializationCallback(OBJECTREF refTarget);

MethodDesc *GetInterfaceMethodImpl(MethodTable *pMT, MethodTable *pItfMT, WORD wSlot)
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
    } CONTRACTL_END;

    MethodDesc *pMeth = NULL;
    DispatchSlot slot(pMT->FindDispatchSlot(pItfMT->GetTypeID(), (UINT32)wSlot));
    CONSISTENCY_CHECK(!slot.IsNull());
    pMeth = slot.GetMethodDesc();
    return pMeth;
}

// Given a FieldDesc which may be representative and an object which contains said field, return the actual type of the field. This
// works even when called from a different appdomain from which the type was loaded (though naturally it is the caller's
// responsbility to ensure such an appdomain cannot be unloaded during the processing of this method).
TypeHandle LoadExactFieldType(FieldDesc *pFD, OBJECTREF orefParent, AppDomain *pDomain)
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    } CONTRACTL_END;

    MethodTable *pEnclosingMT = orefParent->GetMethodTable();

    // Set up a field signature with the owning type providing a type context for any type variables.
    MetaSig sig(pFD, TypeHandle(pEnclosingMT));
    sig.NextArg();

    // If the enclosing type is resident to this domain or domain neutral and loaded in this domain then we can simply go get it.
    // The logic is trickier (and more expensive to calculate) for generic types, so skip the optimization there.
    if (pEnclosingMT->GetDomain() == GetAppDomain() ||
        (pEnclosingMT->IsDomainNeutral() &&
         !pEnclosingMT->HasInstantiation() &&
         pEnclosingMT->GetAssembly()->FindDomainAssembly(GetAppDomain())))
        return sig.GetLastTypeHandleThrowing();

    TypeHandle retTH;

    // Otherwise we have to do this the expensive way -- switch to the home domain for the type lookup.
    ENTER_DOMAIN_PTR(pDomain, ADV_RUNNINGIN); 
    retTH = sig.GetLastTypeHandleThrowing();
    END_DOMAIN_TRANSITION;

    return retTH;
}

extern TypeHandle GetTypeByName( _In_opt_z_ LPUTF8 szFullClassName,
                                BOOL bThrowOnError, 
                                BOOL bIgnoreCase, 
                                StackCrawlMark *stackMark,
                                BOOL *pbAssemblyIsLoading);

#ifndef DACCESS_COMPILE
#define CUSTOM_GCPROTECT_BEGIN(context)           do {                      \
                FrameWithCookie<GCSafeCollectionFrame> __gcframe(context);  \
                /* work around unreachable code warning */                  \
                if (true) { DEBUG_ASSURE_NO_RETURN_BEGIN(GCPROTECT)

#define CUSTOM_GCPROTECT_END()                                              \
                DEBUG_ASSURE_NO_RETURN_END(GCPROTECT) }                     \
                __gcframe.Pop(); } while(0)

#else // #ifndef DACCESS_COMPILE

#define CUSTOM_GCPROTECT_BEGIN(context)
#define CUSTOM_GCPROTECT_END()

#endif // #ifndef DACCESS_COMPILE

int GCSafeObjectHashTable::HasID(OBJECTREF refObj, OBJECTREF *newObj)
{
    CONTRACTL
    {
        MODE_COOPERATIVE;
        THROWS;
        GC_NOTRIGGER;
    }
    CONTRACTL_END

    BOOL seenBefore = FALSE;
    *newObj = NULL;
    int index = FindElement(refObj, seenBefore);

    if (seenBefore)
    {
        _ASSERTE(index < (int)m_currArraySize);
        *newObj = m_newObjects[index];
        return m_ids[index];
    }

    return -1;
}

// returns the object id
int GCSafeObjectHashTable::AddObject(OBJECTREF refObj, OBJECTREF newObj)
{
    CONTRACTL
    {
        MODE_COOPERATIVE;
        THROWS;
        GC_NOTRIGGER;
    }
    CONTRACTL_END

    int index = -1;
    GCPROTECT_BEGIN(refObj);
    GCPROTECT_BEGIN(newObj);
    
    if (m_count > m_currArraySize / 2)
    {
        Resize();
    }

    BOOL seenBefore = FALSE;
    index = FindElement(refObj, seenBefore);

    _ASSERTE(index >= 0 && index < (int)m_currArraySize);
    if (seenBefore)
    {
        _ASSERTE(!"Adding an object thats already present");
    }
    else
    {
        m_objects[index] = refObj;
        m_newObjects[index] = newObj;
        m_ids[index] = ++m_count;
    }

    GCPROTECT_END();
    GCPROTECT_END();

    return m_ids[index];
}

// returns the object id
int GCSafeObjectHashTable::UpdateObject(OBJECTREF refObj, OBJECTREF newObj)
{
    CONTRACTL
    {
        MODE_COOPERATIVE;
        THROWS;
        GC_NOTRIGGER;
    }
    CONTRACTL_END

    int index = -1;
    GCPROTECT_BEGIN(refObj);
    GCPROTECT_BEGIN(newObj);
    
    BOOL seenBefore = FALSE;
    index = FindElement(refObj, seenBefore);

    _ASSERTE(index >= 0 && index < (int)m_currArraySize);
    if (!seenBefore)
    {
        _ASSERTE(!"An object has to exist in the table, to update it");
    }
    else
    {
        _ASSERTE(m_objects[index] == refObj);
        m_newObjects[index] = newObj;
    }

    GCPROTECT_END();
    GCPROTECT_END();

    return m_ids[index];
}

// returns index into array where obj was found or will fit in
int GCSafeObjectHashTable::FindElement(OBJECTREF refObj, BOOL &seenBefore)
{
    CONTRACTL
    {
        MODE_COOPERATIVE;
        THROWS;
        GC_NOTRIGGER;
    }
    CONTRACTL_END

    int currentNumBuckets = m_currArraySize / NUM_SLOTS_PER_BUCKET;
    int hashcode = 0;
    GCPROTECT_BEGIN(refObj);
    hashcode = refObj->GetHashCodeEx();
    GCPROTECT_END();
    
    hashcode &= 0x7FFFFFFF; // ignore sign bit
    int hashIncrement = (1+((hashcode)%(currentNumBuckets-2)));        
#ifdef _DEBUG 
    int numLoops = 0;
#endif

    do
    {
        int index = ((unsigned)hashcode % currentNumBuckets) * NUM_SLOTS_PER_BUCKET;
        _ASSERTE(index >= 0 && index < (int)m_currArraySize);
        for (int i = index; i < index + NUM_SLOTS_PER_BUCKET; i++)
        {
            if (m_objects[i] == refObj)
            {
                seenBefore = TRUE;
                return i;
            }

            if (m_objects[i] == NULL)
            {
                seenBefore = FALSE;
                return i;
            }
        }
        hashcode += hashIncrement;
#ifdef _DEBUG 
        if (++numLoops > currentNumBuckets)
            _ASSERTE(!"Looped too many times, trying to find object in hashtable. If hitting ignore doesnt seem to help, then contact Ashok");
#endif
    }while (true);

    _ASSERTE(!"Not expected to reach here in GCSafeObjectHashTable::FindElement");
    return -1;
}

void GCSafeObjectHashTable::Resize()
{
    CONTRACTL
    {
        MODE_COOPERATIVE;
        THROWS;
        GC_NOTRIGGER;
    }
    CONTRACTL_END
    // Allocate new space
    DWORD newSize = m_currArraySize * 2;
    for (int i = 0; (DWORD) i < sizeof(g_rgPrimes)/sizeof(DWORD); i++)
    {
        if (g_rgPrimes[i] > newSize)
        {
            newSize = g_rgPrimes[i];
            break;
        }
    }

    newSize *= NUM_SLOTS_PER_BUCKET;
    NewArrayHolder<OBJECTREF> refTemp (new OBJECTREF[newSize]);
    ZeroMemory((void *)refTemp, sizeof(OBJECTREF) * newSize);

    NewArrayHolder<OBJECTREF> refTempNewObj (new OBJECTREF[newSize]);
#ifdef USE_CHECKED_OBJECTREFS
    ZeroMemory((void *)refTempNewObj, sizeof(OBJECTREF) * newSize);
#endif

    NewArrayHolder<int> bTemp (new int[newSize]);
    ZeroMemory((void *)bTemp, sizeof(int) * newSize);

    // Copy over objects and data
    NewArrayHolder<OBJECTREF> refOldObj (m_objects);
    NewArrayHolder<OBJECTREF> refOldNewObj (m_newObjects);
    NewArrayHolder<int> oldIds (m_ids);
    DWORD oldArrSize = m_currArraySize;

    if (oldIds == (int *)&m_dataOnStack[0])
    {
        refOldObj.SuppressRelease();
        refOldNewObj.SuppressRelease();
        oldIds.SuppressRelease();
    }
    
    refTemp.SuppressRelease();
    refTempNewObj.SuppressRelease();
    bTemp.SuppressRelease();
    
    m_ids = bTemp;
    m_objects = refTemp;
    m_newObjects = refTempNewObj;
    m_currArraySize = newSize;
    
    for (DWORD i = 0; i < oldArrSize; i++)
    {
        if (refOldObj[i] == NULL)
            continue;

        BOOL seenBefore = FALSE;
        int newIndex = FindElement(refOldObj[i], seenBefore);

        if (!seenBefore)
        {
            _ASSERTE(newIndex < (int)m_currArraySize);
            m_objects[newIndex] = refOldObj[i];
            m_newObjects[newIndex] = refOldNewObj[i];
            m_ids[newIndex] = oldIds[i];
        }
        else
            _ASSERTE(!"Object seen twice while rehashing");
    }

#ifdef USE_CHECKED_OBJECTREFS
    for(DWORD i = 0; i < m_currArraySize; i++)
        Thread::ObjectRefProtected(&m_objects[i]);
    for(DWORD i = 0; i < m_currArraySize; i++)
        Thread::ObjectRefProtected(&m_newObjects[i]);
#endif

}

void GCSafeObjectTable::Push(OBJECTREF refObj, OBJECTREF refParent, OBJECTREF refAux, QueuedObjectInfo * pQOI)
{
    CONTRACTL
    {
        THROWS;
        MODE_COOPERATIVE;
        GC_NOTRIGGER;
    }
    CONTRACTL_END
    _ASSERTE(refObj != NULL);
    _ASSERTE(m_QueueType == LIFO_QUEUE);
    _ASSERTE(m_head == 0 && m_dataHead == 0);
    
    // First find the size of the object info
    DWORD size = pQOI->GetSize();

    // Check if resize is needed
    EnsureSize(size);

    // Push on the stack, first the objects
    DWORD index = m_count;
    if (m_Objects1)
        m_Objects1[index] = refObj;
#ifdef _DEBUG
    else
        _ASSERTE(refObj == NULL);
#endif    
    if (m_Objects2)
        m_Objects2[index] = refParent;
#ifdef _DEBUG
    else
        _ASSERTE(refParent == NULL);
#endif    
    if (m_Objects3)
        m_Objects3[index] = refAux;
#ifdef _DEBUG
    else
        _ASSERTE(refAux == NULL);
#endif    

    // then the info
    if (m_dataIndices)
        m_dataIndices[index] = m_numDataBytes;
    BYTE *pData = &m_data[m_numDataBytes]; 
    memcpy(pData, (VOID*)pQOI, size);

    m_numDataBytes += size;
    m_count++;
}

OBJECTREF GCSafeObjectTable::Pop(OBJECTREF *refParent, OBJECTREF *refAux, QueuedObjectInfo ** pQOI)
{
    LIMITED_METHOD_CONTRACT;
    _ASSERTE(m_QueueType == LIFO_QUEUE);
    _ASSERTE(m_head == 0 && m_dataHead == 0);
    _ASSERTE(m_dataIndices != NULL);
    
    *pQOI = NULL;
    OBJECTREF refRet = NULL;
    *refParent = NULL;
    *refAux = NULL;
    if (m_count == 0)
        return NULL;

    m_count--;
    refRet = m_Objects1[m_count];
    if (m_Objects2)
        *refParent = m_Objects2[m_count];
    if (m_Objects3)
        *refAux = m_Objects3[m_count];
    *pQOI = (QueuedObjectInfo *) &m_data[m_dataIndices[m_count]];

    m_numDataBytes -= (*pQOI)->GetSize();
    return refRet;
}

void GCSafeObjectTable::SetAt(DWORD index, OBJECTREF refObj, OBJECTREF refParent, OBJECTREF refAux, QueuedObjectInfo * pQOI)
{
    CONTRACTL
    {
        THROWS;
        MODE_COOPERATIVE;
        GC_NOTRIGGER;
    }
    CONTRACTL_END
    _ASSERTE(refObj != NULL);
#ifdef _DEBUG
    if (m_QueueType == LIFO_QUEUE)
        _ASSERTE(index >= 0 && index < m_count);
    else
        _ASSERTE(index < m_currArraySize);
#endif
    
    // First find the size of the object info
    DWORD size = pQOI->GetSize();

    // Push on the stack, first the objects
    m_Objects1[index] = refObj;
    if (m_Objects2)
        m_Objects2[index] = refParent;
    if (m_Objects3)
        m_Objects3[index] = refAux;

    // then the info
    _ASSERTE(m_dataIndices != NULL);
    
    QueuedObjectInfo *pData = (QueuedObjectInfo *)&m_data[m_dataIndices[index]]; 
    _ASSERTE(pData->GetSize() == size);
    
    memcpy(pData, (VOID*)pQOI, size);
}

OBJECTREF GCSafeObjectTable::GetAt(DWORD index, OBJECTREF *refParent, OBJECTREF *refAux, QueuedObjectInfo ** pQOI)
{
    LIMITED_METHOD_CONTRACT;
#ifdef _DEBUG
    if (m_QueueType == LIFO_QUEUE)
        _ASSERTE(index >= 0 && index < m_count);
    else
        _ASSERTE(index < m_currArraySize);
#endif

    OBJECTREF refRet = m_Objects1[index];
    if (m_Objects2)
        *refParent = m_Objects2[index];
    else
        *refParent = NULL;
    if (m_Objects3)
        *refAux = m_Objects3[index];
    else
        *refAux = NULL;

    _ASSERTE(m_dataIndices != NULL);
    
    *pQOI = (QueuedObjectInfo *) &m_data[m_dataIndices[index]];

    return refRet;
}

void GCSafeObjectTable::Enqueue(OBJECTREF refObj, OBJECTREF refParent, OBJECTREF refAux, QueuedObjectInfo *pQOI)
{
    CONTRACTL
    {
        THROWS;
        MODE_COOPERATIVE;
        GC_NOTRIGGER;
    }
    CONTRACTL_END
        
    _ASSERTE(refObj != NULL);
    _ASSERTE(m_QueueType == FIFO_QUEUE);
    
    // First find the size of the object info
    DWORD size = pQOI ? pQOI->GetSize() : 0;

    // Check if resize is needed
    EnsureSize(size);

    // Append to queue, first the objects
    DWORD index = (m_head + m_count) % m_currArraySize;
    m_Objects1[index] = refObj;
    if (m_Objects2)
        m_Objects2[index] = refParent;
    if (m_Objects3)
        m_Objects3[index] = refAux;

    // then the info
    if (pQOI)
    {
        DWORD dataIndex = (m_dataHead + m_numDataBytes) % (m_currArraySize * MAGIC_FACTOR);
        BYTE *pData = &m_data[dataIndex];
        memcpy(pData, (VOID*)pQOI, size);

        if (m_dataIndices)
            m_dataIndices[index] = dataIndex;
        m_numDataBytes += size;
    }

    m_count++;
}

OBJECTREF GCSafeObjectTable::Dequeue(OBJECTREF *refParent, OBJECTREF *refAux, QueuedObjectInfo ** pQOI)
{
    LIMITED_METHOD_CONTRACT;
    
    _ASSERTE(m_QueueType == FIFO_QUEUE);
    
    if (pQOI)
        *pQOI = NULL;
    OBJECTREF refRet = NULL;
    *refParent = NULL;
    *refAux = NULL;
    if (m_count == 0)
        return NULL;

    refRet = m_Objects1[m_head];
    if (m_Objects2)
        *refParent = m_Objects2[m_head];
    if (m_Objects3)
        *refAux = m_Objects3[m_head];
    
    if (pQOI)
    {
        *pQOI = (QueuedObjectInfo *) &m_data[m_dataHead];

        m_dataHead = (m_dataHead + (*pQOI)->GetSize()) % (m_currArraySize * MAGIC_FACTOR);

        m_numDataBytes -= (*pQOI)->GetSize();
    }

    m_head = (m_head + 1) % m_currArraySize;
    m_count--;
    return refRet;
}

OBJECTREF GCSafeObjectTable::Peek(OBJECTREF *refParent, OBJECTREF *refAux, QueuedObjectInfo **pQOI)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        SO_TOLERANT;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;
    
   *pQOI = NULL;
    *refParent = NULL;
    *refAux = NULL;
    if (m_count == 0)
        return NULL;

    DWORD indexToPeek;
    if (m_QueueType == LIFO_QUEUE)
    {
        indexToPeek = m_count;
        return GetAt(indexToPeek, refParent, refAux, pQOI);
    }
    else
    {
        indexToPeek = m_head;
        if (m_Objects2)
            *refParent = m_Objects2[m_head];
        if (m_Objects3)
            *refParent = m_Objects3[m_head];
        *pQOI = (QueuedObjectInfo *) &m_data[m_dataHead];
        return m_Objects1[m_head];
    }

}

void GCSafeObjectTable::EnsureSize(DWORD requiredDataSize)
{
    CONTRACTL
    {
        THROWS;
        MODE_COOPERATIVE;
        GC_NOTRIGGER;
    }
    CONTRACTL_END
    // Check if the object queue is sized enough
    if (m_count == m_currArraySize)
    {
        Resize();
        return;
    }

    // Check if the data array size is enough 
    if (m_numDataBytes + requiredDataSize > m_currArraySize * MAGIC_FACTOR)
    {
        Resize();
        return;
    }

    if (m_QueueType == FIFO_QUEUE)
    {
        // Will current QueuedObjectInfo go beyond the edge of the array ?
        if (m_dataHead + m_numDataBytes + requiredDataSize > m_currArraySize * MAGIC_FACTOR)
        {
            Resize();
            return;
        }
    }
}

void GCSafeObjectTable::Resize()
{
    CONTRACTL
    {
        THROWS;
        MODE_COOPERATIVE;
        GC_NOTRIGGER;
    }
    CONTRACTL_END
    // Allocate new space
    DWORD newSize = m_currArraySize * 2;
    NewArrayHolder<OBJECTREF> refTemp (NULL);
    NewArrayHolder<OBJECTREF> refParentTemp (NULL);
    NewArrayHolder<OBJECTREF> refAuxTemp (NULL);

    refTemp = new OBJECTREF[newSize];
    if (m_Objects2)
        refParentTemp = new OBJECTREF[newSize];
    if (m_Objects3)
        refAuxTemp = new OBJECTREF[newSize];

#ifdef USE_CHECKED_OBJECTREFS
    ZeroMemory((void *)refTemp, sizeof(OBJECTREF) * newSize);
    if (m_Objects2)
        ZeroMemory((void *)refParentTemp, sizeof(OBJECTREF) * newSize);
    if (m_Objects3)
        ZeroMemory((void *)refAuxTemp, sizeof(OBJECTREF) * newSize);
#endif

    NewArrayHolder<BYTE> bTemp (NULL);
    NewArrayHolder<DWORD> dwIndicesTemp (NULL);

    bTemp = new BYTE[newSize * MAGIC_FACTOR];
    if (m_dataIndices)
        dwIndicesTemp = new DWORD[newSize];

    // Copy over objects and data
    if (m_QueueType == LIFO_QUEUE || (m_QueueType == FIFO_QUEUE && m_head == 0))
    {
        void *pSrc = (void *)&m_Objects1[0];
        void *pDest = (void *)&refTemp[0];
        memcpyUnsafe(pDest, pSrc, m_count * sizeof(OBJECTREF));

        if (m_Objects2)
        {
            pSrc = (void *)&m_Objects2[0];
            pDest = (void *)&refParentTemp[0];
            memcpyUnsafe(pDest, pSrc, m_count * sizeof(OBJECTREF));
        }

        if (m_Objects3)
        {
            pSrc = (void *)&m_Objects3[0];
            pDest = (void *)&refAuxTemp[0];
            memcpyUnsafe(pDest, pSrc, m_count * sizeof(OBJECTREF));
        }

        pSrc = (void *)&m_data[0];
        pDest = (void *)&bTemp[0];
        memcpyNoGCRefs(pDest, pSrc, m_numDataBytes);

        if (m_dataIndices)
        {
            pSrc = (void *)&m_dataIndices[0];
            pDest = (void *)&dwIndicesTemp[0];
            memcpyNoGCRefs(pDest, pSrc, m_count * sizeof(DWORD));
        }

    }
    else
    {
        _ASSERTE(m_QueueType == FIFO_QUEUE && m_head != 0);
        _ASSERTE(m_currArraySize > m_head);
        DWORD numObjRefsToCopy = (m_count > m_currArraySize - m_head ? m_currArraySize - m_head : m_count);

        void *pSrc = (void *)&m_Objects1[m_head];
        void *pDest = (void *)&refTemp[0];
        memcpyUnsafe(pDest, pSrc, numObjRefsToCopy * sizeof(OBJECTREF));
        pSrc = (void *)&m_Objects1[0];
        pDest = (void *)&refTemp[numObjRefsToCopy];
        memcpyUnsafe(pDest, pSrc, (m_count - numObjRefsToCopy) * sizeof(OBJECTREF));

        if (m_Objects2)
        {
            pSrc = (void *)&m_Objects2[m_head];
            pDest = (void *)&refParentTemp[0];
            memcpyUnsafe(pDest, pSrc, numObjRefsToCopy * sizeof(OBJECTREF));
            pSrc = (void *)&m_Objects2[0];
            pDest = (void *)&refParentTemp[numObjRefsToCopy];
            memcpyUnsafe(pDest, pSrc, (m_count - numObjRefsToCopy) * sizeof(OBJECTREF));
        }

        if (m_Objects3)
        {
            pSrc = (void *)&m_Objects3[m_head];
            pDest = (void *)&refAuxTemp[0];
            memcpyUnsafe(pDest, pSrc, numObjRefsToCopy * sizeof(OBJECTREF));
            pSrc = (void *)&m_Objects3[0];
            pDest = (void *)&refAuxTemp[numObjRefsToCopy];
            memcpyUnsafe(pDest, pSrc, (m_count - numObjRefsToCopy) * sizeof(OBJECTREF));
        }

        if (m_dataIndices)
        {
            pSrc = (void *)&m_dataIndices[m_head];
            pDest = (void *)&dwIndicesTemp[0];
            memcpyUnsafe(pDest, pSrc, numObjRefsToCopy * sizeof(DWORD));
            pSrc = (void *)&m_dataIndices[0];
            pDest = (void *)&dwIndicesTemp[numObjRefsToCopy];
            memcpyUnsafe(pDest, pSrc, (m_count - numObjRefsToCopy) * sizeof(DWORD));
        }

        DWORD numBytesToCopy = (m_numDataBytes > ((m_currArraySize * MAGIC_FACTOR) - m_dataHead) ? ((m_currArraySize * MAGIC_FACTOR) - m_dataHead) : m_numDataBytes);//(m_currArraySize * MAGIC_FACTOR) - m_dataHead;
        memcpyNoGCRefs((void *)bTemp, (void *) &m_data[m_dataHead], numBytesToCopy);
        memcpyNoGCRefs((void *) &bTemp[numBytesToCopy], (void *)m_data, (m_numDataBytes - numBytesToCopy)); 
    }
    
    // Delete old allocation
    if (m_usingHeap)
    {
        delete[] m_data;
        delete[] m_Objects1;
        delete[] m_Objects2;
        delete[] m_Objects3;
        delete[] m_dataIndices;
    }

    refTemp.SuppressRelease();
    refParentTemp.SuppressRelease();
    refAuxTemp.SuppressRelease();
    dwIndicesTemp.SuppressRelease();
    bTemp.SuppressRelease();
    
    m_currArraySize = newSize;
    m_Objects1 = refTemp;
    m_Objects2 = refParentTemp;
    m_Objects3 = refAuxTemp;
    m_dataIndices = dwIndicesTemp;
    m_data = bTemp;
    m_head = 0;
    m_dataHead = 0;

    m_usingHeap = TRUE;
#ifdef USE_CHECKED_OBJECTREFS
    for(DWORD i = 0; i < m_currArraySize; i++)
    {
        Thread::ObjectRefProtected(&m_Objects1[i]);
        if (m_Objects2)
            Thread::ObjectRefProtected(&m_Objects2[i]);
        if (m_Objects3)
            Thread::ObjectRefProtected(&m_Objects3[i]);
    }
#endif
}


VOID GCScanRootsInCollection(promote_func *fn, ScanContext* sc, void *context)
{
    STATIC_CONTRACT_SO_TOLERANT;
    GCSafeCollection *pObjCollection = (GCSafeCollection *)context;
    pObjCollection->ReportGCRefs(fn, sc);
}

VOID
BeginCloning(ObjectClone *pOC)
{
    pOC->Init(FALSE);
}

VOID
EndCloning(ObjectClone *pOC)
{
    pOC->Cleanup(FALSE);
}

typedef Holder<ObjectClone*, BeginCloning, EndCloning> ObjectCloneHolder;


OBJECTREF ObjectClone::Clone(OBJECTREF refObj, TypeHandle expectedType, AppDomain* fromDomain, AppDomain* toDomain, OBJECTREF refExecutionContext)
{
    CONTRACTL
    {
        MODE_COOPERATIVE;
        THROWS;
        GC_TRIGGERS;
    }
    CONTRACTL_END

    if (refObj == NULL)
        return NULL;

    if (m_context != ObjectFreezer && refObj->GetMethodTable() == g_pStringClass)
        return refObj;
    
    ObjectCloneHolder ocHolder(this);
    
    m_fromDomain = fromDomain;
    m_toDomain = toDomain;

    m_currObject = refObj;
    GCPROTECT_BEGIN(m_currObject);
    m_topObject = NULL;
    GCPROTECT_BEGIN(m_topObject);
    m_fromExecutionContext = refExecutionContext;
    GCPROTECT_BEGIN(m_fromExecutionContext);

    // Enter the domain we're cloning into, if we're not already there
    ENTER_DOMAIN_PTR(toDomain,ADV_RUNNINGIN);
    
    if (!m_securityChecked)
    {
        Security::SpecialDemand(SSWT_DEMAND_FROM_NATIVE, SECURITY_SERIALIZATION);
        m_securityChecked = TRUE;
    }
    
#ifdef _DEBUG
    DefineFullyQualifiedNameForClass();
    LOG((LF_REMOTING, LL_INFO100, "Clone. Cloning instance of type %s.\n", 
        GetFullyQualifiedNameForClassNestedAware(m_currObject->GetMethodTable())));
#endif

    m_newObject = NULL;
    GCPROTECT_BEGIN(m_newObject);
    PTRARRAYREF refValues = NULL;
    GCPROTECT_BEGIN(refValues);
    OBJECTREF refParent = NULL;
    GCPROTECT_BEGIN(refParent);

    QueuedObjectInfo    *currObjFixupInfo = NULL;
    // For some dynamically sized stack objects
    void *pTempStackSpace = NULL;
    DWORD dwCurrStackSpaceSize = 0;
    
    // Initialize QOM
    QueuedObjectInfo topObj;
    OBJECTREF dummy1, dummy2;
    QOM.Enqueue(m_currObject, NULL, NULL, (QueuedObjectInfo *)&topObj);

    while ((m_currObject = QOM.Dequeue(&dummy1, &dummy2, &currObjFixupInfo)) != NULL)
    {
        m_newObject = NULL;
        MethodTable *newMT = NULL;
        
        BOOL repeatObject = FALSE;
        BOOL isISerializable = FALSE, isIObjRef = FALSE, isBoxed = FALSE;
        DWORD ISerializableTSOIndex = (DWORD) -1;
        DWORD IObjRefTSOIndex = (DWORD) -1;
        DWORD BoxedValTSOIndex = (DWORD) -1;
        m_skipFieldScan = FALSE;

        // ALLOCATE PHASE

        // Was currObject seen before ?
        int currID = TOS.HasID(m_currObject, &m_newObject);
        if (currID != -1)
        {
            // Yes
            repeatObject = TRUE;
            m_skipFieldScan = TRUE;
            newMT = m_newObject->GetMethodTable();

            if (m_cbInterface->IsISerializableType(newMT))
            {
                currObjFixupInfo->SetIsISerializableInstance();
                isISerializable = TRUE;
                ISerializableTSOIndex = FindObjectInTSO(currID, ISerializable);
            }

#ifdef _DEBUG
            LOG((LF_REMOTING, LL_INFO1000, "Clone. Object of type %s with id %d seen before.\n",
                GetFullyQualifiedNameForClassNestedAware(m_currObject->GetMethodTable()), currID));
#endif
        }
        else
        {
#ifdef _DEBUG
            LOG((LF_REMOTING, LL_INFO1000, "Clone. Object of type %s not seen before.\n",
                GetFullyQualifiedNameForClassNestedAware(m_currObject->GetMethodTable())));
#endif
            // No
            MethodTable *currMT = m_currObject->GetMethodTable();
            
            // Check whether object is serializable
            m_cbInterface->ValidateFromType(currMT);

            // Add current object to table of seen objects and get an id
            currID = TOS.AddObject(m_currObject, m_newObject);
            LOG((LF_REMOTING, LL_INFO1000, "Clone. Current object added to Table of Objects Seen. Given id %d.\n", currID));

            if ( m_cbInterface->IsRemotedType(currMT, m_fromDomain, m_toDomain))
            {
                refValues = AllocateISerializable(currID, TRUE);
                isISerializable = TRUE;
                ISerializableTSOIndex = TSO.GetCount() - 1;
                currObjFixupInfo->SetIsISerializableInstance();
                if (refValues == NULL)
                {
                    // We found a smugglable objref. No field scanning needed
                    m_skipFieldScan = TRUE;
                }
            }
            else if( m_cbInterface->IsISerializableType(currMT))
            {
                InvokeVtsCallbacks(m_currObject, RemotingVtsInfo::VTS_CALLBACK_ON_SERIALIZING, fromDomain);
                if (HasVtsCallbacks(m_currObject->GetMethodTable(), RemotingVtsInfo::VTS_CALLBACK_ON_SERIALIZED))
                    VSC.Enqueue(m_currObject, NULL, NULL, NULL);

                refValues = AllocateISerializable(currID, FALSE);
                isISerializable = TRUE;
                ISerializableTSOIndex = TSO.GetCount() - 1;
                currObjFixupInfo->SetIsISerializableInstance();
            }
            else if (currMT->IsArray())
            {
                AllocateArray();
            }
            else
            {
                // This is a regular object
                InvokeVtsCallbacks(m_currObject, RemotingVtsInfo::VTS_CALLBACK_ON_SERIALIZING, fromDomain);
                if (HasVtsCallbacks(m_currObject->GetMethodTable(), RemotingVtsInfo::VTS_CALLBACK_ON_SERIALIZED))
                    VSC.Enqueue(m_currObject, NULL, NULL, NULL);

                AllocateObject();

                if (m_cbInterface->IsISerializableType(m_newObject->GetMethodTable()))
                {
                    // We have a situation where the serialized instnce was not ISerializable, 
                    // but the target instance is. So we make the from object look like a ISerializable
                    refValues = MakeObjectLookLikeISerializable(currID);
                    isISerializable = TRUE;
                    ISerializableTSOIndex = TSO.GetCount() - 1;
                    currObjFixupInfo->SetIsISerializableInstance();
                }
            }

            _ASSERTE(m_newObject != NULL);
            newMT = m_newObject->GetMethodTable();
            
            // Check whether new object is serializable
            m_cbInterface->ValidateToType(newMT);
            
            // Update the TOS, to include the new object
            int retId;
            retId = TOS.UpdateObject(m_currObject, m_newObject);
            _ASSERTE(retId == currID);
        }
        _ASSERTE(m_newObject != NULL);

        // FIXUP PHASE
        // Get parent to be fixed up
        ParentInfo *parentInfo;
        refParent = QOF.Peek(&dummy1, &dummy2, (QueuedObjectInfo **)&parentInfo);
        MethodTable *pParentMT = NULL;
        
        if (refParent == NULL)
        {
            LOG((LF_REMOTING, LL_INFO1000, "Clone. No parent found. This is the top object.\n"));
            // This is the top object
            _ASSERTE(m_topObject == NULL);
            m_topObject = m_newObject;
        }
        else
        {
#ifdef _DEBUG
            LOG((LF_REMOTING, LL_INFO1000, "Clone. Parent is of type %s.\n",
                GetFullyQualifiedNameForClassNestedAware(m_currObject->GetMethodTable())));
#endif
            pParentMT = refParent->GetMethodTable();
        }

        if (IsDelayedFixup(newMT, currObjFixupInfo))
        {
            // New object is IObjRef or a boxed object
            if (m_cbInterface->IsIObjectReferenceType(newMT))
            {
                LOG((LF_REMOTING, LL_INFO1000, "Clone. This is an IObjectReference. Delaying fixup.\n"));
                DWORD size = sizeof(IObjRefInstanceInfo) + (currObjFixupInfo ? currObjFixupInfo->GetSize() : 0);
                if (size > dwCurrStackSpaceSize)
                {
                    pTempStackSpace = _alloca(size);
                    dwCurrStackSpaceSize = size;
                }
                IObjRefInstanceInfo *pIORInfo = new (pTempStackSpace) IObjRefInstanceInfo(currID, 0, 0);
                if (currObjFixupInfo)
                    pIORInfo->SetFixupInfo(currObjFixupInfo);
                // Check if this instance is ISerializable also
                if (isISerializable)
                {
                    LOG((LF_REMOTING, LL_INFO1000, "Clone. This is also an ISerializable type at index %d in TSO.\n", ISerializableTSOIndex));
                    _ASSERTE(ISerializableTSOIndex != (DWORD) -1);
                    pIORInfo->SetISerTSOIndex(ISerializableTSOIndex);
                }

                if (repeatObject)
                    pIORInfo->SetIsRepeatObject();
                
                // Add to TSO
                TSO.Push(m_newObject, m_currObject, refParent, pIORInfo);

                isIObjRef = TRUE;
                IObjRefTSOIndex = TSO.GetCount() - 1;

                LOG((LF_REMOTING, LL_INFO1000, "Clone. Added to TSO at index %d.\n", IObjRefTSOIndex));
                // Any special object parent, would wait till the current object is resolved
                if (parentInfo)
                {
                    parentInfo->IncrementSpecialMembers();
                    TMappings.Add(IObjRefTSOIndex);
                }

            }
            if (currObjFixupInfo->NeedsUnboxing())
            {
                LOG((LF_REMOTING, LL_INFO1000, "Clone. This is a boxed value type. Delaying fixup.\n"));
                DWORD size = sizeof(ValueTypeInfo) + currObjFixupInfo->GetSize();
                if (size > dwCurrStackSpaceSize)
                {
                    pTempStackSpace = _alloca(size);
                    dwCurrStackSpaceSize = size;
                }
                ValueTypeInfo *valInfo = new (pTempStackSpace) ValueTypeInfo(currID, currObjFixupInfo);
                // If the value type is also ISer or IObj, then it has to wait till those interfaces are addressed
                if (isISerializable)
                {
                    LOG((LF_REMOTING, LL_INFO1000, "Clone. This is also an ISerializable type at index %d in TSO.\n", ISerializableTSOIndex));
                    valInfo->SetISerTSOIndex(ISerializableTSOIndex);
                }
                if (isIObjRef)
                {
                    LOG((LF_REMOTING, LL_INFO1000, "Clone. This is also an IObjectReference type at index %d in TSO.\n", IObjRefTSOIndex));
                    valInfo->SetIObjRefTSOIndex(IObjRefTSOIndex);
                }

                // Add to TSO
                TSO.Push(m_newObject, refParent, NULL, valInfo);

                isBoxed = TRUE;
                BoxedValTSOIndex = TSO.GetCount() - 1;

                LOG((LF_REMOTING, LL_INFO1000, "Clone. Added to TSO at index %d.\n", BoxedValTSOIndex));
                // An IObjRef parent, or a parent itself boxed, would wait till the current object is resolved
                if (parentInfo && (parentInfo->NeedsUnboxing() || parentInfo->IsIObjRefInstance()))
                {
                    parentInfo->IncrementSpecialMembers();
                    TMappings.Add(BoxedValTSOIndex);
                }
            }
        }

        if (refParent != NULL)
        {
            if (!IsDelayedFixup(newMT, currObjFixupInfo))
                Fixup(m_newObject, refParent, currObjFixupInfo);
            
            // If currObj is ISer, then an IObjRef parent would wait till the current object is resolved
            if (currObjFixupInfo->IsISerializableInstance() && 
                parentInfo->IsIObjRefInstance())
            {
                parentInfo->IncrementSpecialMembers();
                TMappings.Add(ISerializableTSOIndex);
            }
        }

        // If we are done with this parent, remove it from QOF
        if (parentInfo && parentInfo->DecrementFixupCount() == 0)
        {
            LOG((LF_REMOTING, LL_INFO1000, "Clone. All children fixed up. Removing parent from QOF.\n", BoxedValTSOIndex));
            LOG((LF_REMOTING, LL_INFO1000, "Clone. Parent has %d special member objects.\n", parentInfo->GetNumSpecialMembers()));
            OBJECTREF refTemp;
            ParentInfo *pFITemp;
            refTemp = QOF.Dequeue(&dummy1, &dummy2, (QueuedObjectInfo **)&pFITemp);
            _ASSERTE(refTemp == refParent);
            _ASSERTE(pFITemp == parentInfo);

            // If parent is a special object, then we need to know how many special members it has
            if ((parentInfo->IsIObjRefInstance() || 
                parentInfo->IsISerializableInstance() || 
                parentInfo->NeedsUnboxing()) 
                && parentInfo->GetNumSpecialMembers() > 0)
            {
                // Make a note in TSO that this parent has non-zero special members
                DWORD index[3];
                index[0] = parentInfo->GetIObjRefIndexIntoTSO();
                index[1] = parentInfo->GetISerIndexIntoTSO();
                index[2] = parentInfo->GetBoxedValIndexIntoTSO();

                for (DWORD count = 0; count < 3; count++)
                {
                    OBJECTREF refIser, refNames, refValuesTemp;
                    SpecialObjectInfo *pISerInfo;

                    if (index[count] == (DWORD) -1)
                        continue;
                    
                    refIser = TSO.GetAt(index[count], &refNames, &refValuesTemp, (QueuedObjectInfo **)&pISerInfo);
                    _ASSERTE(refIser == refParent);
                    
                    DWORD numSpecialObjects = parentInfo->GetNumSpecialMembers();
                    pISerInfo->SetNumSpecialMembers(numSpecialObjects);

                    _ASSERTE(TMappings.GetCount() >= numSpecialObjects);
                    pISerInfo->SetMappingTableIndex(TMappings.GetCount() - numSpecialObjects);
                }
            }
        }

        // FIELD SCAN PHASE
        if (!m_skipFieldScan)
        {
            if (m_currObject->GetMethodTable()->IsArray())
                ScanArrayMembers();
            else if (isISerializable)
                ScanISerializableMembers(IObjRefTSOIndex, ISerializableTSOIndex, BoxedValTSOIndex, refValues);
            else
                ScanMemberFields(IObjRefTSOIndex, BoxedValTSOIndex);
        }

    } // While there are objects in QOM

    // OBJECT COMPLETION PHASE
    CompleteSpecialObjects();

    // Deliver VTS OnDeserialized callbacks.
    CompleteVtsOnDeserializedCallbacks();

    CompleteIDeserializationCallbacks();

    _ASSERTE(m_topObject != NULL);
    // If a type check was requested, see if the returned object is of the expected type
    if (!expectedType.IsNull()
        && !ObjIsInstanceOf(OBJECTREFToObject(m_topObject), expectedType))
        COMPlusThrow(kArgumentException,W("Arg_ObjObj"));
    
    GCPROTECT_END(); // refParent
    GCPROTECT_END(); // refValues 
    
    GCPROTECT_END(); // m_newObject
    
    END_DOMAIN_TRANSITION;

    // Deliver VTS OnSerialized callbacks.
    CompleteVtsOnSerializedCallbacks();

    GCPROTECT_END(); // m_fromExecutionContext
    GCPROTECT_END(); // m_topObject
    GCPROTECT_END(); // m_currObject 

    return m_topObject;
}

// IObjRef and value types boxed by us, need to be fixed up towards the end
BOOL ObjectClone::IsDelayedFixup(MethodTable *newMT, QueuedObjectInfo *pCurrInfo)
{
    CONTRACTL
    {
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        THROWS;
    }
    CONTRACTL_END
    if (m_cbInterface->IsIObjectReferenceType(newMT) ||
        pCurrInfo->NeedsUnboxing())
        return TRUE;
    else
        return FALSE;
}

void ObjectClone::Fixup(OBJECTREF newObj, OBJECTREF refParent, QueuedObjectInfo *pFixupInfo)
{
    CONTRACTL
    {
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        THROWS;
    }
    CONTRACTL_END
    MethodTable *pParentMT = refParent->GetMethodTable();
    
    if (pFixupInfo->IsISerializableMember())
    {
        HandleISerializableFixup(refParent, pFixupInfo);
    }
    else if (pParentMT->IsArray())
    {
        HandleArrayFixup(refParent, pFixupInfo);
    }
    else
    {
        HandleObjectFixup(refParent, pFixupInfo);
    }
}

PTRARRAYREF ObjectClone::MakeObjectLookLikeISerializable(int objectId)
{
    CONTRACTL
    {
        GC_TRIGGERS;
        THROWS;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END

    _ASSERTE(m_context != ObjectFreezer);
    
    LOG((LF_REMOTING, LL_INFO1000, "MakeObjectLookLikeISerializable. Target object is ISerializable, so making from object look ISerializable\n"));
    MethodTable *pCurrMT = m_currObject->GetMethodTable();
    DWORD numFields = pCurrMT->GetNumInstanceFields();

    PTRARRAYREF fieldNames = NULL;
    PTRARRAYREF fieldValues = NULL;

    GCPROTECT_BEGIN(fieldNames);
    GCPROTECT_BEGIN(fieldValues);

    // Go back to from domain
    ENTER_DOMAIN_PTR(m_fromDomain,ADV_RUNNINGIN);

    // Reset the execution context to the original state it was in when we first
    // left the from domain (this will automatically be popped once we return
    // from this domain again).
    Thread *pThread = GetThread();
    if (pThread->IsExposedObjectSet())
    {
        THREADBASEREF refThread = (THREADBASEREF)pThread->GetExposedObjectRaw();
        refThread->SetExecutionContext(m_fromExecutionContext);
    }

    fieldNames = (PTRARRAYREF)AllocateObjectArray(numFields, g_pStringClass, FALSE);
    fieldValues = (PTRARRAYREF)AllocateObjectArray(numFields, g_pObjectClass, FALSE);

    DWORD fieldIndex = 0;
    while (pCurrMT)
    {

        DWORD numInstanceFields = pCurrMT->GetNumIntroducedInstanceFields();

        FieldDesc *pFields = pCurrMT->GetApproxFieldDescListRaw();
        
        for (DWORD i = 0; i < numInstanceFields; i++)
        {
            if (pFields[i].IsNotSerialized())
            {
                LOG((LF_REMOTING, LL_INFO1000, "MakeObjectLookLikeISerializable. Field %s is marked NonSerialized. Skipping.\n", pFields[i].GetName()));
                continue;
            }
            
            CorElementType typ = pFields[i].GetFieldType();
            DWORD offset = pFields[i].GetOffset();

            LPCUTF8 szFieldName = pFields[i].GetName();
            STRINGREF refName = StringObject::NewString(szFieldName);
            _ASSERTE(refName != NULL);

            fieldNames->SetAt(fieldIndex, refName);
            
            switch (typ)
            {
                case ELEMENT_TYPE_BOOLEAN:
                case ELEMENT_TYPE_I1:
                case ELEMENT_TYPE_U1:
                case ELEMENT_TYPE_I2:
                case ELEMENT_TYPE_U2:
                case ELEMENT_TYPE_CHAR:
                case ELEMENT_TYPE_I4:
                case ELEMENT_TYPE_U4:
                case ELEMENT_TYPE_I8:
                case ELEMENT_TYPE_U8:
                case ELEMENT_TYPE_I:
                case ELEMENT_TYPE_U:
                case ELEMENT_TYPE_R4:
                case ELEMENT_TYPE_R8:
                {
                    MethodTable *pFldMT = MscorlibBinder::GetElementType(typ);
                    void *pData = m_currObject->GetData() + offset;
                    OBJECTREF refBoxed = pFldMT->Box(pData);

                    fieldValues->SetAt(fieldIndex, refBoxed);
                    break;
                }
                case ELEMENT_TYPE_VALUETYPE:
                case ELEMENT_TYPE_PTR: 
                case ELEMENT_TYPE_FNPTR:
                {
                    TypeHandle th = LoadExactFieldType(&pFields[i], m_currObject, m_fromDomain);
                    _ASSERTE(!th.AsMethodTable()->ContainsStackPtr() && "Field types cannot contain stack pointers.");

                    OBJECTREF refBoxed = BoxValueTypeInWrongDomain(m_currObject, offset, th.AsMethodTable());

                    fieldValues->SetAt(fieldIndex, refBoxed);
                    break;
                }
                case ELEMENT_TYPE_SZARRAY:          // Single Dim
                case ELEMENT_TYPE_ARRAY:            // General Array
                case ELEMENT_TYPE_CLASS:            // Class
                case ELEMENT_TYPE_OBJECT:
                case ELEMENT_TYPE_STRING:           // System.String
                case ELEMENT_TYPE_VAR:
                {
                    OBJECTREF refField = *((OBJECTREF *) m_currObject->GetData() + offset);
                    fieldValues->SetAt(fieldIndex, refField);
                    break;
                }
                default:
                    _ASSERTE(!"Unknown element type in MakeObjectLookLikeISerializalbe");
            }

            fieldIndex++;
        }

        pCurrMT = pCurrMT->GetParentMethodTable();
    }

    // Back to original domain      
    END_DOMAIN_TRANSITION;

    // Add object to TSO
    ISerializableInstanceInfo iserInfo(objectId, 0);
    TSO.Push(m_newObject, fieldNames, NULL, (QueuedObjectInfo *)&iserInfo);

    LOG((LF_REMOTING, LL_INFO1000, "MakeObjectLookLikeISerializable. Added to TSO at index %d.\n", TSO.GetCount() - 1));
    GCPROTECT_END();
    GCPROTECT_END();

    return fieldValues;
}

PTRARRAYREF ObjectClone::AllocateISerializable(int objectId, BOOL bIsRemotingObject)
{
    CONTRACTL
    {
        GC_TRIGGERS;
        THROWS;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END
        
    _ASSERTE(m_context != ObjectFreezer);

    // Go back to from domain
    StackSString ssAssemName;
    StackSString ssTypeName;

    struct _gc {
        STRINGREF   typeName;
        STRINGREF   assemblyName;
        PTRARRAYREF fieldNames;
        PTRARRAYREF fieldValues;
        OBJECTREF   refObjRef;
    } gc;
    ZeroMemory(&gc, sizeof(gc));

    GCPROTECT_BEGIN(gc);
    
    ENTER_DOMAIN_PTR(m_fromDomain,ADV_RUNNINGIN);

    // Reset the execution context to the original state it was in when we first
    // left the from domain (this will automatically be popped once we return
    // from this domain again).
    Thread *pThread = GetThread();
    if (pThread->IsExposedObjectSet())
    {
        THREADBASEREF refThread = (THREADBASEREF)pThread->GetExposedObjectRaw();
        refThread->SetExecutionContext(m_fromExecutionContext);
    }

    // Call GetObjectData on the interface

    LOG((LF_REMOTING, LL_INFO1000, "AllocateISerializable. Instance is ISerializable type. Calling GetObjectData.\n"));

    PREPARE_NONVIRTUAL_CALLSITE(METHOD__OBJECTCLONEHELPER__GET_OBJECT_DATA);

    DECLARE_ARGHOLDER_ARRAY(args, 5);

    args[ARGNUM_0]    = OBJECTREF_TO_ARGHOLDER(m_currObject);
    args[ARGNUM_1]    = PTR_TO_ARGHOLDER(&gc.typeName);
    args[ARGNUM_2] = PTR_TO_ARGHOLDER(&gc.assemblyName);
    args[ARGNUM_3] = PTR_TO_ARGHOLDER(&gc.fieldNames);
    args[ARGNUM_4] = PTR_TO_ARGHOLDER(&gc.fieldValues);

    CATCH_HANDLER_FOUND_NOTIFICATION_CALLSITE;
    CALL_MANAGED_METHOD_RETREF(gc.refObjRef, OBJECTREF, args);

    if (!bIsRemotingObject || gc.refObjRef == NULL)
    {
        ssAssemName.Set(gc.assemblyName->GetBuffer());
        ssTypeName.Set(gc.typeName->GetBuffer());
    }

    // Back to original domain      
    END_DOMAIN_TRANSITION;

    // if its a remoting object we are dealing with, we may already have the smugglable objref
    if (bIsRemotingObject && gc.refObjRef != NULL)
    {
        m_newObject = gc.refObjRef;
        // Add object to TSO. We dont need a ISerializable record, because we are smuggling the ObjRef
        // and so, technically the ISerializable ctor can be considered already called. But we still make an entry in 
        // TSO and mark it "processed", so repeat references to the same remoting object work correctly
        ISerializableInstanceInfo iserInfo(objectId, 0);
        iserInfo.SetHasBeenProcessed();
        TSO.Push(m_newObject, NULL, NULL, (QueuedObjectInfo *)&iserInfo);

        LOG((LF_REMOTING, LL_INFO1000, "AllocateISerializable. GetObjectData returned smugglable ObjRef. Added dummy record to TSO at index %d.\n", TSO.GetCount() - 1));
    }
    else
    {
        // Find the type (and choke on any exotics such as arrays, function pointers or generic type definitions).
        TypeHandle th = GetType(ssTypeName, ssAssemName);
        if (th.IsTypeDesc() || th.ContainsGenericVariables())
        {
            StackSString ssBeforeTypeName, ssAfterTypeName;
            TypeString::AppendType(ssBeforeTypeName, m_currObject->GetTypeHandle(), TypeString::FormatNamespace | TypeString::FormatFullInst);
            TypeString::AppendType(ssAfterTypeName, th, TypeString::FormatNamespace | TypeString::FormatFullInst);
            COMPlusThrow(kSerializationException, IDS_SERIALIZATION_BAD_ISER_TYPE, ssBeforeTypeName.GetUnicode(), ssAfterTypeName.GetUnicode());
        }
        MethodTable *pSrvMT = th.AsMethodTable();
        _ASSERTE(pSrvMT);

#ifdef _DEBUG
        {
            DefineFullyQualifiedNameForClass();
            LPCUTF8 __szTypeName = GetFullyQualifiedNameForClassNestedAware(pSrvMT);
            LOG((LF_REMOTING, LL_INFO1000, "AllocateISerializable. Allocating instance of type %s.\n", &__szTypeName[0]));
        }
#endif
        // Allocate the object
        m_newObject = m_cbInterface->AllocateObject(m_currObject, pSrvMT);

        // Add object to TSO
        ISerializableInstanceInfo iserInfo(objectId, 0);
        
        // Check if the target object is ISerializable. If not, we need to treat construction of this object differently
        if (!m_cbInterface->IsISerializableType(pSrvMT))
        {
            iserInfo.SetTargetNotISerializable();
        }
        TSO.Push(m_newObject, gc.fieldNames, NULL, (QueuedObjectInfo *)&iserInfo);

        LOG((LF_REMOTING, LL_INFO1000, "AllocateISerializable. Added to TSO at index %d.\n", TSO.GetCount() - 1));
    }
    GCPROTECT_END();

    return gc.fieldValues;
}

void ObjectClone::AllocateArray()
{
    CONTRACTL
    {
        MODE_COOPERATIVE;
        GC_TRIGGERS;
        THROWS;
    }
    CONTRACTL_END

    LOG((LF_REMOTING, LL_INFO1000, "AllocateArray. Instance is an array type.\n"));
    MethodTable *pCurrMT = m_currObject->GetMethodTable();
    _ASSERTE(pCurrMT->IsArray());

    BASEARRAYREF refArray = (BASEARRAYREF)m_currObject;
    GCPROTECT_BEGIN(refArray);
        
    TypeHandle elemTh = refArray->GetArrayElementTypeHandle();
    CorElementType elemType = refArray->GetArrayElementType();
    DWORD numComponents = refArray->GetNumComponents();

    TypeHandle __elemTh = GetCorrespondingTypeForTargetDomain(elemTh);
    _ASSERTE(!__elemTh.IsNull());

    unsigned __rank = pCurrMT->GetRank();
    TypeHandle __arrayTh = ClassLoader::LoadArrayTypeThrowing(__elemTh, __rank == 1 ? ELEMENT_TYPE_SZARRAY : ELEMENT_TYPE_ARRAY, __rank);

    DWORD __numArgs =  __rank*2;
    INT32* __args = (INT32*) _alloca(sizeof(INT32)*__numArgs);

    if (__arrayTh.AsArray()->GetInternalCorElementType() == ELEMENT_TYPE_ARRAY)
    {
        const INT32* bounds = refArray->GetBoundsPtr();
        const INT32* lowerBounds = refArray->GetLowerBoundsPtr();
        for(unsigned int i=0; i < __rank; i++) 
        {
            __args[2*i]   = lowerBounds[i];
            __args[2*i+1] = bounds[i];
        }
    }
    else
    {
        __numArgs = 1;
        __args[0] = numComponents;
    }
    m_newObject = m_cbInterface->AllocateArray(m_currObject, __arrayTh, __args, __numArgs, FALSE);

    // Treat pointer as a primitive type (we shallow copy the bits).
    if (CorTypeInfo::IsPrimitiveType(elemType) || elemType == ELEMENT_TYPE_PTR)
    {
        LOG((LF_REMOTING, LL_INFO1000, "AllocateArray. Instance is an array of primitive type. Copying contents.\n"));
        // Copy contents. 
        SIZE_T numBytesToCopy = refArray->GetComponentSize() * numComponents;
        I1ARRAYREF refI1Arr = (I1ARRAYREF)m_newObject;
        BYTE *pDest = (BYTE *)refI1Arr->GetDirectPointerToNonObjectElements();
        I1ARRAYREF refFromArr = (I1ARRAYREF)refArray;
        BYTE *pSrc = (BYTE *)refFromArr->GetDirectPointerToNonObjectElements();

        memcpyNoGCRefs(pDest, pSrc, numBytesToCopy);
        m_skipFieldScan = TRUE;
    }
    else if (elemType == ELEMENT_TYPE_VALUETYPE)
    {
        if (!__elemTh.GetMethodTable()->HasFieldsWhichMustBeInited() && RemotableMethodInfo::TypeIsConduciveToBlitting(elemTh.AsMethodTable(), __elemTh.GetMethodTable()))
        {
            LOG((LF_REMOTING, LL_INFO1000, "AllocateArray. Instance is an array of value type with no embedded GC type. Copying contents.\n"));
            // Copy contents. 
            SIZE_T numBytesToCopy = refArray->GetComponentSize() * numComponents;
            I1ARRAYREF refI1Arr = (I1ARRAYREF)m_newObject;
            BYTE *pDest = (BYTE *)refI1Arr->GetDirectPointerToNonObjectElements();
            I1ARRAYREF refFromArr = (I1ARRAYREF)refArray;
            BYTE *pSrc = (BYTE *)refFromArr->GetDirectPointerToNonObjectElements();

            memcpyNoGCRefs(pDest, pSrc, numBytesToCopy);
            m_skipFieldScan = TRUE;
        }
    }
    GCPROTECT_END();
}

void ObjectClone::AllocateObject()
{
    CONTRACTL
    {
        MODE_COOPERATIVE;
        GC_TRIGGERS;
        THROWS;
    }
    CONTRACTL_END
        
    LOG((LF_REMOTING, LL_INFO1000, "AllocateObject. Instance is a regular object.\n"));
    MethodTable *pCurrMT = m_currObject->GetMethodTable();
    _ASSERTE(!pCurrMT->IsArray());
    _ASSERTE(!pCurrMT->IsMarshaledByRef() && !pCurrMT->IsTransparentProxy());
    _ASSERTE(!m_cbInterface->IsISerializableType(pCurrMT));

    MethodTable *pCorrespondingMT = GetCorrespondingTypeForTargetDomain(pCurrMT);
    _ASSERTE(pCorrespondingMT);

    pCorrespondingMT->EnsureInstanceActive();
    
    m_newObject =  m_cbInterface->AllocateObject(m_currObject, pCorrespondingMT);

    InvokeVtsCallbacks(m_newObject, RemotingVtsInfo::VTS_CALLBACK_ON_DESERIALIZING, m_toDomain);
}

// Use this wrapper when the type handle can't be represented as a raw MethodTable (i.e. it's a pointer or array type).
TypeHandle ObjectClone::GetCorrespondingTypeForTargetDomain(TypeHandle thCli)
{
    CONTRACTL
    {
        MODE_COOPERATIVE;
        GC_TRIGGERS;
        THROWS;
    }
    CONTRACTL_END

    TypeHandle thBaseType = thCli;
    TypeHandle thSrvType;

    // Strip off any pointer information (and record the depth). We'll put this back later (when we've translated the base type).
    DWORD dwPointerDepth = 0;
    while (thBaseType.IsPointer())
    {
        dwPointerDepth++;
        thBaseType = thBaseType.AsTypeDesc()->GetTypeParam();
    }

    // If we hit an array then we'll recursively translate the element type then build an array type out of it.
    if (thBaseType.IsArray())
    {
        ArrayTypeDesc *atd = (ArrayTypeDesc *)thBaseType.AsTypeDesc();
        thSrvType = GetCorrespondingTypeForTargetDomain(atd->GetArrayElementTypeHandle());
        
        thSrvType = ClassLoader::LoadArrayTypeThrowing(thSrvType, atd->GetInternalCorElementType(), atd->GetRank());
    }
    else
    {
        // We should have only unshared types if we get here.
        _ASSERTE(!thBaseType.IsTypeDesc());
        thSrvType = GetCorrespondingTypeForTargetDomain(thBaseType.AsMethodTable());
    }

    // Match the level of pointer indirection from the original client type.
    while (dwPointerDepth--)
    {
        thSrvType = thSrvType.MakePointer();
    }

    return thSrvType;
}

MethodTable * ObjectClone::GetCorrespondingTypeForTargetDomain(MethodTable *pCliMT)
{
    CONTRACTL
    {
        MODE_COOPERATIVE;
        GC_TRIGGERS;
        THROWS;
    }
    CONTRACTL_END
        
    MethodTable *pSrvMT = NULL;
    if (m_fromDomain == m_toDomain)
        return pCliMT;

    _ASSERTE(m_context != ObjectFreezer);
#ifdef _DEBUG
    SString __ssTypeName;
    StackScratchBuffer __scratchBuf;
    if (pCliMT->IsArray())
        pCliMT->_GetFullyQualifiedNameForClass(__ssTypeName);
    else
        pCliMT->_GetFullyQualifiedNameForClassNestedAware(__ssTypeName);
#endif

    // Take benefit of shared types. If a type is shared, and its assembly has been loaded
    // in the target domain, go ahead and use the same MT ptr.
    // The logic is trickier (and more expensive to calculate) for generic types, so skip the optimization there.
    if (pCliMT->IsDomainNeutral() && !pCliMT->HasInstantiation())
    {
        if (pCliMT->GetAssembly()->FindDomainAssembly(m_toDomain))
        {
            LOG((LF_REMOTING, LL_INFO1000,
                "GetCorrespondingTypeForTargetDomain. Type %s is shared. Using same MethodTable.\n", __ssTypeName.GetUTF8(__scratchBuf)));
            return pCliMT;
        }
    }

    pSrvMT = CrossDomainTypeMap::GetMethodTableForDomain(pCliMT, m_fromDomain, m_toDomain);
    if (pSrvMT)
    {
        LOG((LF_REMOTING, LL_INFO1000,
            "GetCorrespondingTypeForTargetDomain. Found matching type for %s in domain %d from cache.\n", __ssTypeName.GetUTF8(__scratchBuf), m_toDomain));
        return pSrvMT;
    }
    
    // Need to find the name and lookup in target domain
    SString ssCliTypeName;
    if (pCliMT->IsArray())
    {
        pCliMT->_GetFullyQualifiedNameForClass(ssCliTypeName);
    }
    else if (pCliMT->HasInstantiation())
    {
        TypeString::AppendType(ssCliTypeName, TypeHandle(pCliMT), TypeString::FormatNamespace | TypeString::FormatFullInst);
    }
    else
    {
        pCliMT->_GetFullyQualifiedNameForClassNestedAware(ssCliTypeName);
    }

    
    SString ssAssemblyName;
    pCliMT->GetAssembly()->GetDisplayName(ssAssemblyName);

    // Get the assembly
    TypeHandle th = GetType(ssCliTypeName, ssAssemblyName);

    if (!pCliMT->IsArray())
    {
        pSrvMT = th.AsMethodTable();
    }  
    else
    {
        _ASSERTE(th.IsArray());
        TypeDesc *td = th.AsTypeDesc();
        pSrvMT = td->GetMethodTable();
    }
    CrossDomainTypeMap::SetMethodTableForDomain(pCliMT, m_fromDomain, pSrvMT, m_toDomain);
    LOG((LF_REMOTING, LL_INFO1000,
        "GetCorrespondingTypeForTargetDomain. Loaded matching type for %s in domain %d. Added to cache.\n", __ssTypeName.GetUTF8(__scratchBuf), m_toDomain));
    return pSrvMT;
}

TypeHandle ObjectClone::GetType(const SString &ssTypeName, const SString &ssAssemName)
{
    CONTRACTL
    {
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        THROWS;
    }
    CONTRACTL_END;

    Assembly *pAssembly = NULL;

#ifndef OBJECT_CLONER_STRICT_MODE
    EX_TRY
#endif
    {
        AssemblySpec spec;
        StackScratchBuffer scratchBuf;
        HRESULT hr = spec.Init(ssAssemName.GetUTF8(scratchBuf)); 
        if (SUCCEEDED(hr)) 
        {
            pAssembly = spec.LoadAssembly(FILE_ACTIVE);
        }
        else
        {
            COMPlusThrowHR(hr);
        }
    }
#ifndef OBJECT_CLONER_STRICT_MODE
    EX_CATCH
    {
        if (GET_EXCEPTION()->IsTransient())
        {
            EX_RETHROW;
        }

        DomainAssembly *pDomainAssembly = NULL;
#ifdef FEATURE_FUSION
        // If the normal load fails then try loading from a partial assembly name (relaxed serializer rules).
        pDomainAssembly = LoadAssemblyFromPartialNameHack((SString*)&ssAssemName, TRUE);
#endif // FEATURE_FUSION
        if (pDomainAssembly == NULL)
            COMPlusThrow(kSerializationException, IDS_SERIALIZATION_UNRESOLVED_TYPE,
                         ssTypeName.GetUnicode(), ssAssemName.GetUnicode());
        else
            pAssembly = pDomainAssembly->GetAssembly();
    }
    EX_END_CATCH(SwallowAllExceptions);
#endif

    _ASSERTE(pAssembly);

    TypeHandle th = TypeName::GetTypeFromAssembly(ssTypeName.GetUnicode(), pAssembly);

    if (th.IsNull())
    {
        COMPlusThrow(kSerializationException, IDS_SERIALIZATION_UNRESOLVED_TYPE,
                     ssTypeName.GetUnicode(), ssAssemName.GetUnicode());
    }

    LOG((LF_REMOTING, LL_INFO1000, "GetType. Loaded type %S from assembly %S in domain %d. \n",
        ssTypeName.GetUnicode(), ssAssemName.GetUnicode(), m_toDomain->GetId().m_dwId));

    return th;
}

void ObjectClone::HandleISerializableFixup(OBJECTREF refParent, QueuedObjectInfo *currObjFixupInfo)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        SO_TOLERANT;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;
    
    _ASSERTE(m_context != ObjectFreezer);

    ISerializableMemberInfo *pIsInfo = (ISerializableMemberInfo *)currObjFixupInfo;
    OBJECTREF refNames, refValues;
    ISerializableInstanceInfo *dummy;
    OBJECTREF parent;
    parent = TSO.GetAt(pIsInfo->GetTableIndex(), &refNames, &refValues, (QueuedObjectInfo **)&dummy);
    _ASSERTE(parent == refParent);
    _ASSERTE(dummy->IsISerializableInstance());

    PTRARRAYREF refFields = (PTRARRAYREF)refValues;
    _ASSERTE(pIsInfo->GetFieldIndex() < refFields->GetNumComponents());
    refFields->SetAt(pIsInfo->GetFieldIndex(), m_newObject);
    
    LOG((LF_REMOTING, LL_INFO1000, "HandleISerializableFixup. Parent is ISerializable. Added field #%d to TSO record at index %d\n", pIsInfo->GetFieldIndex(), pIsInfo->GetTableIndex()));
}

void ObjectClone::HandleArrayFixup(OBJECTREF refParent, QueuedObjectInfo *currObjFixupInfo)
{
    CONTRACTL
    {
        MODE_COOPERATIVE;
        THROWS;
        GC_TRIGGERS;
    }
    CONTRACTL_END
        
    _ASSERTE(refParent->GetMethodTable()->IsArray());    
    BASEARRAYREF refParentArray = (BASEARRAYREF) refParent;
    GCPROTECT_BEGIN(refParentArray);

    NDimArrayMemberInfo *pArrInfo = (NDimArrayMemberInfo *)currObjFixupInfo;
    DWORD *pIndices = pArrInfo->GetIndices();

    TypeHandle arrayElementType = refParentArray->GetArrayElementTypeHandle();
    MethodTable *pArrayMT = refParentArray->GetMethodTable();

    DWORD Rank                  = pArrayMT->GetRank();
    SIZE_T Offset               = 0;
    SIZE_T Multiplier           = 1;

    _ASSERTE(Rank == pArrInfo->GetNumDimensions());
    
    for (int i = Rank-1; i >= 0; i--) {
        INT32 curIndex = pIndices[i];        
        const INT32 *pBoundsPtr      = refParentArray->GetBoundsPtr();
        
        // Bounds check each index
        // Casting to unsigned allows us to use one compare for [0..limit-1]
        _ASSERTE((UINT32) curIndex < (UINT32) pBoundsPtr[i]);

        Offset     += curIndex * Multiplier;
        Multiplier *= pBoundsPtr[i];
    }

    // The follwing code is loosely based on COMArrayInfo::SetValue

    if (!arrayElementType.IsValueType())
    {
        if (!ObjIsInstanceOf(OBJECTREFToObject(m_newObject), arrayElementType))
            COMPlusThrow(kInvalidCastException,W("InvalidCast_StoreArrayElement"));

        OBJECTREF* pElem = (OBJECTREF*)(refParentArray->GetDataPtr() + (Offset * pArrayMT->GetComponentSize()));
        SetObjectReference(pElem,m_newObject,GetAppDomain());
    }
    else
    {
        // value class or primitive type
        OBJECTREF* pElem = (OBJECTREF*)(refParentArray->GetDataPtr() + (Offset * pArrayMT->GetComponentSize()));
       if (!arrayElementType.GetMethodTable()->UnBoxInto(pElem, m_newObject))
                COMPlusThrow(kInvalidCastException, W("InvalidCast_StoreArrayElement"));
    }

    LOG((LF_REMOTING, LL_INFO1000, "HandleArrayFixup. Parent is an array. Added element at offset %d\n", Offset));
    GCPROTECT_END();
}

void ObjectClone::HandleObjectFixup(OBJECTREF refParent, QueuedObjectInfo *currObjFixupInfo)
{
    CONTRACTL
    {
        MODE_COOPERATIVE;
        GC_TRIGGERS;
        THROWS;
    }
    CONTRACTL_END
    ObjectMemberInfo *pObjInfo = (ObjectMemberInfo *)currObjFixupInfo;
    FieldDesc *pTargetField = pObjInfo->GetFieldDesc();
    DWORD offset = pTargetField->GetOffset();

#ifdef _DEBUG
    MethodTable *pTemp = refParent->GetMethodTable();
    _ASSERTE(offset < pTemp->GetBaseSize());
#endif

    GCPROTECT_BEGIN(refParent);

    TypeHandle fldType = LoadExactFieldType(pTargetField, refParent, m_toDomain);

    if (!ObjIsInstanceOf(OBJECTREFToObject(m_newObject), fldType))
        COMPlusThrow(kArgumentException,W("Arg_ObjObj"));

    OBJECTREF *pDest = (OBJECTREF *) (refParent->GetData() + offset);
    _ASSERTE(GetAppDomain()==m_toDomain);
    SetObjectReference(pDest, m_newObject, GetAppDomain());

    GCPROTECT_END();
    
    LOG((LF_REMOTING, LL_INFO1000, "HandleObjectFixup. Parent is a regular object. Added field at offset %d\n", offset));
}

#ifdef OBJECT_CLONER_STRICT_MODE
static void DECLSPEC_NORETURN ThrowMissingFieldException(FieldDesc *pFD)
{
    CONTRACTL
    {
        MODE_COOPERATIVE;
        GC_TRIGGERS;
        THROWS;
    }
    CONTRACTL_END;

    StackSString szField(SString::Utf8, pFD->GetName());

    StackSString szType;
    TypeString::AppendType(szType, TypeHandle(pFD->GetApproxEnclosingMethodTable()));

    COMPlusThrow(kSerializationException, 
                 IDS_SERIALIZATION_MISSING_FIELD,
                 szField.GetUnicode(),
                 szType.GetUnicode());
}
#endif

void ObjectClone::ScanMemberFields(DWORD IObjRefTSOIndex, DWORD BoxedValTSOIndex)
{
    CONTRACTL
    {
        MODE_COOPERATIVE;
        GC_TRIGGERS;
        THROWS;
    }
    CONTRACTL_END
    _ASSERTE(m_currObject != NULL);
    _ASSERTE(m_newObject != NULL);
    
    MethodTable *pMT = m_currObject->GetMethodTable();
    _ASSERTE(!pMT->IsMarshaledByRef() && !pMT->IsTransparentProxy());
    _ASSERTE(!pMT->IsArray());
    MethodTable *pTargetMT = m_newObject->GetMethodTable();

    DWORD numFixupsNeeded = 0;
    
    if (RemotableMethodInfo::TypeIsConduciveToBlitting(pMT, pTargetMT))
    {
        _ASSERTE(pMT->GetAlignedNumInstanceFieldBytes() == pTargetMT->GetAlignedNumInstanceFieldBytes());
        DWORD numBytes = pMT->GetNumInstanceFieldBytes();
        BYTE *pFrom = m_currObject->GetData();
        BYTE *pTo = m_newObject->GetData(); 
        memcpyNoGCRefs(pTo, pFrom, numBytes);
        LOG((LF_REMOTING, LL_INFO1000, "ScanMemberFields. Object has no reference type fields. Blitting contents.\n"));
    }
    else if (AreTypesEmittedIdentically(pMT, pTargetMT))
    {
        LOG((LF_REMOTING, LL_INFO1000, "ScanMemberFields. Object not blittable but types are layed out for easy cloning .\n"));
        MethodTable *pCurrMT = pMT;
        MethodTable *pCurrTargetMT = pTargetMT;
        while (pCurrMT)
        {
            DWORD numInstanceFields = pCurrMT->GetNumIntroducedInstanceFields();
            _ASSERTE(pCurrTargetMT->GetNumIntroducedInstanceFields() == numInstanceFields);

            FieldDesc *pFields = pCurrMT->GetApproxFieldDescListRaw();
            FieldDesc *pTargetFields = pCurrTargetMT->GetApproxFieldDescListRaw();
            
            for (DWORD i = 0; i < numInstanceFields; i++)
            {
                if (pFields[i].IsNotSerialized())
                {
                    LOG((LF_REMOTING, LL_INFO1000, "ScanMemberFields. Field %s is marked NonSerialized. Skipping.\n", pFields[i].GetName()));
                    continue;
                }

                numFixupsNeeded += CloneField(&pFields[i], &pTargetFields[i]);
            }

            pCurrMT = pCurrMT->GetParentMethodTable();
            pCurrTargetMT = pCurrTargetMT->GetParentMethodTable();
        }
    }
    else
    {
        LOG((LF_REMOTING, LL_INFO1000, "ScanMemberFields. Object type layout is different.\n"));

        // The object types between source and destination have significant differences (some fields may be added, removed or
        // re-ordered, the type hierarchy may have had layers added or removed). We can still clone the object if every non-optional
        // field in the destination object can be found and serialized in a type with the same name in the source object. We ignore
        // fields and entire type layers that have been added in the source object and also any fields or layers that have been
        // removed as long as they don't include any fields that are mandatory in the destination object. We allow the fields within
        // a type layer to move around (we key the field by name only, the latter stage of cloning will check type equivalency and
        // as above we will widen primitive types if necessary). Since it requires significant effort to calculate whether the
        // objects can be cloned (and then locate corresponding fields in order to do so) we cache a mapping of source object fields
        // to destination object fields.

        // The following call will return such a mapping (it's an array where each entry is a pointer to a source object field desc
        // and the entries are in destination field index order, most derived type first, followed by second most derived type
        // etc.). If a mapping is impossible the method will throw.
        FieldDesc **pFieldMap = CrossDomainFieldMap::LookupOrCreateFieldMapping(pTargetMT, pMT);
        DWORD dwMapIndex = 0;

        MethodTable *pDstMT = pTargetMT;
        while (pDstMT)
        {
            FieldDesc *pDstFields = pDstMT->GetApproxFieldDescListRaw();
            DWORD numInstanceFields = pDstMT->GetNumIntroducedInstanceFields();

            for (DWORD i = 0; i < numInstanceFields; i++)
            {
                FieldDesc *pSrcField = pFieldMap[dwMapIndex++];

                // Non-serialized fields in the destination type (or optional fields where the source type doesn't have an
                // equivalent) don't have a source field desc.
                if (pSrcField == NULL)
                    continue;

                numFixupsNeeded += CloneField(pSrcField, &pDstFields[i]);
            }

            pDstMT = pDstMT->GetParentMethodTable();
        }

        _ASSERTE(dwMapIndex == pTargetMT->GetNumInstanceFields());
    }

    if (numFixupsNeeded > 0)
    {
        ParentInfo fxInfo(numFixupsNeeded);
        if (IObjRefTSOIndex != (DWORD) -1)
        {
            _ASSERTE(m_cbInterface->IsIObjectReferenceType(pMT));
            fxInfo.SetIsIObjRefInstance();
            fxInfo.SetIObjRefIndexIntoTSO(IObjRefTSOIndex);
        }
        if (BoxedValTSOIndex != (DWORD) -1)
        {
            _ASSERTE(pMT->IsValueType());
            fxInfo.SetNeedsUnboxing();
            fxInfo.SetBoxedValIndexIntoTSO(BoxedValTSOIndex);
        }
        QOF.Enqueue(m_newObject, NULL, NULL, (QueuedObjectInfo *)&fxInfo);
        LOG((LF_REMOTING, LL_INFO1000, "ScanMemberFields. Current object had total of %d reference type fields. Adding to QOF.\n", numFixupsNeeded));
        // Delay calling any OnDeserialized callbacks until the end of the cloning operation (it's difficult to tell when all the
        // children have been deserialized).
        if (HasVtsCallbacks(m_newObject->GetMethodTable(), RemotingVtsInfo::VTS_CALLBACK_ON_DESERIALIZED))
            VDC.Enqueue(m_newObject, NULL, NULL, NULL);
        if (m_cbInterface->RequiresDeserializationCallback(m_newObject->GetMethodTable()))
        {
            LOG((LF_REMOTING, LL_INFO1000, "ScanMemberFields. Adding object to Table of IDeserialization Callbacks\n"));
            QueuedObjectInfo noInfo;
            TDC.Enqueue(m_newObject, NULL, NULL, &noInfo);
        }
    }
    else
    {
        // This is effectively a leaf node (no complex children) so if the type has a callback for OnDeserialized we'll deliver it
        // now. This fixes callback ordering for a few more edge cases (e.g. VSW 415611) and is reasonably cheap. We can never do a
        // perfect job (in the presence of object graph cycles) and a near perfect job (intuitively ordered callbacks for acyclic
        // object graphs) is prohibitively expensive; so we're stuck with workarounds like this.
        InvokeVtsCallbacks(m_newObject, RemotingVtsInfo::VTS_CALLBACK_ON_DESERIALIZED, m_toDomain);
        if (m_cbInterface->RequiresDeserializationCallback(m_newObject->GetMethodTable()))
            MakeIDeserializationCallback(m_newObject);
    }
}

DWORD ObjectClone::CloneField(FieldDesc *pSrcField, FieldDesc *pDstField)
{
    CONTRACTL
    {
        MODE_COOPERATIVE;
        GC_TRIGGERS;
        THROWS;
    }
    CONTRACTL_END;

    BOOL bFixupNeeded = FALSE;

    CorElementType srcType = pSrcField->GetFieldType();
    CorElementType dstType = pDstField->GetFieldType();
    DWORD srcOffset = pSrcField->GetOffset();
    DWORD dstOffset = pDstField->GetOffset();

    BOOL bUseWidenedValue = FALSE;
    ARG_SLOT fieldData = 0;
    if (srcType != dstType)
    {
        void *pData = m_currObject->GetData() + srcOffset;

        MethodTable *pSrcFieldMT = NULL;
        if (CorTypeInfo::IsPrimitiveType(srcType))
            pSrcFieldMT = MscorlibBinder::GetElementType(srcType);
        else
            pSrcFieldMT = LoadExactFieldType(pSrcField, m_currObject, m_fromDomain).AsMethodTable();

        LOG((LF_REMOTING, LL_INFO1000, "ScanMemberFields. Field %s has differing types at source and destination. Will try to convert.\n", pSrcField->GetName()));
        fieldData = HandleFieldTypeMismatch(dstType, srcType, pData, pSrcFieldMT);
        bUseWidenedValue = TRUE;
    }
                
    switch (dstType)
    {
    case ELEMENT_TYPE_I1:
    case ELEMENT_TYPE_U1:
    case ELEMENT_TYPE_BOOLEAN:
        {
            BYTE *pDest = m_newObject->GetData() + dstOffset;
            if (bUseWidenedValue)
                *pDest = (unsigned char) fieldData;
            else
            {
                BYTE *pByte = m_currObject->GetData() + srcOffset;
                *pDest = *pByte;
            }
        }
        break;
    case ELEMENT_TYPE_I2:
    case ELEMENT_TYPE_U2:
    case ELEMENT_TYPE_CHAR:
        {
            WORD *pDest = (WORD*)(m_newObject->GetData() + dstOffset);
            if (bUseWidenedValue)
                *pDest = (short) fieldData;
            else
            {
                WORD *pWord = (WORD*)(m_currObject->GetData() + srcOffset);
                *(pDest) = *pWord;
            }
        }
        break;
    case ELEMENT_TYPE_I4:
    case ELEMENT_TYPE_U4:
    case ELEMENT_TYPE_R4:
    IN_WIN32(case ELEMENT_TYPE_FNPTR:)
    IN_WIN32(case ELEMENT_TYPE_I:)
    IN_WIN32(case ELEMENT_TYPE_U:)
        {
            DWORD *pDest = (DWORD*)(m_newObject->GetData() + dstOffset);
            if (bUseWidenedValue)
                *pDest = (int) fieldData;
            else
            {
                DWORD *pDword = (DWORD*)(m_currObject->GetData() + srcOffset);
                *(pDest) = *pDword;
            }
        }
        break;
    case ELEMENT_TYPE_R8:
    case ELEMENT_TYPE_I8:
    case ELEMENT_TYPE_U8:
    IN_WIN64(case ELEMENT_TYPE_FNPTR:)
    IN_WIN64(case ELEMENT_TYPE_I:)
    IN_WIN64(case ELEMENT_TYPE_U:)
        {
            INT64 *pDest = (INT64*)(m_newObject->GetData() + dstOffset);
            if (bUseWidenedValue)
                *pDest = fieldData;
            else
            {
                INT64 *pLong = (INT64*)(m_currObject->GetData() + srcOffset);
                *(pDest) = *pLong;
            }
        }
        break;
    case ELEMENT_TYPE_PTR:
        {
            void **pDest = (void**)(m_newObject->GetData() + dstOffset);
            void **pPtr = (void**)(m_currObject->GetData() + srcOffset);
            *(pDest) = *pPtr;
        }
        break;
    case ELEMENT_TYPE_STRING:
    case ELEMENT_TYPE_CLASS: // objectrefs
    case ELEMENT_TYPE_OBJECT:
    case ELEMENT_TYPE_SZARRAY:      // single dim, zero
    case ELEMENT_TYPE_ARRAY:        // all other arrays
        {
            OBJECTREF *pSrc = (OBJECTREF *)(m_currObject->GetData() + srcOffset);
            OBJECTREF *pDest = (OBJECTREF *)(m_newObject->GetData() + dstOffset);

            if ((*pSrc) == NULL)
                break;

            // If no deep copy is required, just copy the reference
            if (!m_cbInterface->RequiresDeepCopy(*pSrc))
            {
                _ASSERTE(GetAppDomain()==m_toDomain);
                SetObjectReference(pDest, *pSrc, GetAppDomain());
                break;
            }
                            
            // Special case String
            if ((*pSrc)->GetMethodTable() == g_pStringClass)
            {
                // Better check the destination really expects a string (or maybe an object).
                TypeHandle thDstField = LoadExactFieldType(pDstField, m_newObject, m_toDomain);
                if (thDstField != TypeHandle(g_pStringClass) && thDstField != TypeHandle(g_pObjectClass))
                    COMPlusThrow(kArgumentException, W("Arg_ObjObj"));

                STRINGREF refStr = (STRINGREF) *pSrc;
                refStr = m_cbInterface->AllocateString(refStr);
                // Get dest addr again, as a GC might have occurred
                pDest = (OBJECTREF *)(m_newObject->GetData() + dstOffset);
                _ASSERTE(GetAppDomain()==m_toDomain);
                SetObjectReference(pDest, refStr, GetAppDomain());
                                
                break;
            }

            // Add the object to QOM
            LOG((LF_REMOTING, LL_INFO1000, "ScanMemberFields. Adding object in field %s to Queue of Objects to be Marshalled.\n", pSrcField->GetName()));
            ObjectMemberInfo objInfo(pDstField);
            bFixupNeeded = TRUE;
            QOM.Enqueue(*pSrc, NULL, NULL, (QueuedObjectInfo *)&objInfo);
        }
        break;
                    
    case ELEMENT_TYPE_VALUETYPE:
        {
            TypeHandle th = LoadExactFieldType(pSrcField, m_currObject, m_fromDomain);
            _ASSERTE(!th.AsMethodTable()->ContainsStackPtr() && "Field types cannot contain stack pointers.");

            TypeHandle thTarget = LoadExactFieldType(pDstField, m_newObject, m_toDomain);

            MethodTable *pValueClassMT = th.AsMethodTable();
            MethodTable *pValueClassTargetMT = thTarget.AsMethodTable();
            if (!RemotableMethodInfo::TypeIsConduciveToBlitting(pValueClassMT, pValueClassTargetMT))
            {
                // Needs marshalling
                // We're allocating an object in the "to" domain 
                // using a type from the "from" domain. 
                OBJECTREF refTmpBox = BoxValueTypeInWrongDomain(m_currObject, srcOffset, pValueClassMT);

                // Nullable<T> might return null here.  In that case we don't need to do anything
                // and the null value otherwise confuxes the fixup queue. 
                if (refTmpBox != NULL) 
                {
                    // Add the object to QOM
                    ObjectMemberInfo objInfo(pDstField);
                    objInfo.SetNeedsUnboxing();
                    bFixupNeeded = TRUE;
                    QOM.Enqueue(refTmpBox, NULL, NULL, (QueuedObjectInfo *)&objInfo);
                    LOG((LF_REMOTING, LL_INFO1000, "ScanMemberFields. Value type field %s has reference type contents. Boxing and adding to QOM.\n", pSrcField->GetName()));
                }
            }
            else
            {
                DWORD numBytesToCopy = th.AsMethodTable()->GetNumInstanceFieldBytes();
                BYTE *pByte = m_currObject->GetData() + srcOffset;
                BYTE *pDest = m_newObject->GetData() + dstOffset; 
                memcpyNoGCRefs(pDest, pByte, numBytesToCopy);
                LOG((LF_REMOTING, LL_INFO1000, "ScanMemberFields. Value type field %s has no reference type contents. Blitting.\n", pSrcField->GetName()));
            }
        }
        break;
    default:
        _ASSERTE(!"Unknown element type seen in ObjectClone::ScanMemberFields");
        break;
    }            

    return bFixupNeeded ? 1 : 0;
}

BOOL ObjectClone::AreTypesEmittedIdentically(MethodTable *pMT1, MethodTable *pMT2)
{
    LIMITED_METHOD_CONTRACT;

    // Identical here means that both types have the same hierarchy (depth and names match) and that each level of the hierarchy has
    // the same fields (by name) at the same index.
    // We're going to be called quite frequently (once per call to ScanMemberFields) so until we're convinced that caching this
    // information is worth it we'll just compute the fast cases here and let the rest fall through to the slower technique. The
    // fast check is that the types are shared and identical or that they're loaded from the same file (in which case we have to be
    // a little more paranoid and check up the hierarchy).
    if (pMT1 == pMT2)
        return TRUE;

    // While the current level of the type is loaded from the same file...
    // Note that we used to check that the assemblies were the same; now we're more paranoid and check the actual modules scoping
    // the type are identical. This closes a security hole where identically named types in different modules of the same assembly
    // could cause the wrong type to be loaded in the server context allowing violation of the type system.
    while (pMT1->GetModule()->GetFile()->Equals(pMT2->GetModule()->GetFile()))
    {
        // Inspect the parents.
        pMT1 = pMT1->GetParentMethodTable();
        pMT2 = pMT2->GetParentMethodTable();

        // If the parents are the same shared type (e.g. Object), then we've found a match.
        if (pMT1 == pMT2)
            return TRUE;

        // Else check if one of the hierarchies has run out before the other (and therefore can't be equivalent).
        if (pMT1 == NULL || pMT2 == NULL)
            return FALSE;
    }

    return FALSE;
}

BOOL AreTypesEquivalent(MethodTable *pMT1, MethodTable *pMT2)
{
    CONTRACTL
    {
        MODE_COOPERATIVE;
        GC_TRIGGERS;
        THROWS;
    }
    CONTRACTL_END;

    // Equivalent here is quite a weak predicate. All it means is that the types have the same (fully assembly qualified) name. The
    // derivation hierarchy is not inspected at all.
    StackSString szType1;
    StackSString szType2;

    TypeString::AppendType(szType1, TypeHandle(pMT1), TypeString::FormatNamespace |
                                                      TypeString::FormatFullInst |
                                                      TypeString::FormatAssembly |
                                                      TypeString::FormatNoVersion);
    TypeString::AppendType(szType2, TypeHandle(pMT2), TypeString::FormatNamespace |
                                                      TypeString::FormatFullInst |
                                                      TypeString::FormatAssembly |
                                                      TypeString::FormatNoVersion);

    return szType1.Equals(szType2);
}

PtrHashMap *CrossDomainFieldMap::s_pFieldMap = NULL;
SimpleRWLock *CrossDomainFieldMap::s_pFieldMapLock = NULL;

BOOL CrossDomainFieldMap::CompareFieldMapEntry(UPTR val1, UPTR val2)
{
    CONTRACTL {
        MODE_ANY;
        NOTHROW;
        GC_NOTRIGGER;
        SO_TOLERANT;           
    }
    CONTRACTL_END;

    CrossDomainFieldMap::FieldMapEntry *pEntry1 = (CrossDomainFieldMap::FieldMapEntry *)(val1 << 1);
    CrossDomainFieldMap::FieldMapEntry *pEntry2 = (CrossDomainFieldMap::FieldMapEntry *)val2;

    if (pEntry1->m_pSrcMT == pEntry2->m_pSrcMT &&
        pEntry1->m_pDstMT == pEntry2->m_pDstMT)
        return TRUE;

    return FALSE;
}

CrossDomainFieldMap::FieldMapEntry::FieldMapEntry(MethodTable *pSrcMT, MethodTable *pDstMT, FieldDesc **pFieldMap)
{
    WRAPPER_NO_CONTRACT;
    
    m_pSrcMT = pSrcMT;
    m_pDstMT = pDstMT;
    m_pFieldMap = pFieldMap;
    BaseDomain *pSrcDomain = pSrcMT->GetDomain();
    m_dwSrcDomain = pSrcDomain->IsAppDomain() ? ((AppDomain*)pSrcDomain)->GetId() : ADID(0);
    BaseDomain *pDstDomain = pDstMT->GetDomain();
    m_dwDstDomain = pDstDomain->IsAppDomain() ? ((AppDomain*)pDstDomain)->GetId() : ADID(0);
}

static BOOL IsOwnerOfRWLock(LPVOID lock)
{
    // @TODO - SimpleRWLock does not have knowledge of which thread gets the writer 
    // lock, so no way to verify
    return TRUE;
}

// Remove any entries in the table that refer to an appdomain that is no longer live.
void CrossDomainFieldMap::FlushStaleEntries()
{
    if (s_pFieldMapLock == NULL || s_pFieldMap == NULL)
        return;

    SimpleWriteLockHolder swlh(s_pFieldMapLock);

    bool fDeletedEntry = false;
    PtrHashMap::PtrIterator iter = s_pFieldMap->begin();
    while (!iter.end())
    {
        FieldMapEntry *pEntry = (FieldMapEntry *)iter.GetValue();
        AppDomainFromIDHolder adFrom(pEntry->m_dwSrcDomain, TRUE);
        AppDomainFromIDHolder adTo(pEntry->m_dwDstDomain, TRUE);
        if (adFrom.IsUnloaded() ||
            adTo.IsUnloaded()) //we do not use ptr for anything
        {
#ifdef _DEBUG
            LPVOID pDeletedEntry =
#endif
                s_pFieldMap->DeleteValue(pEntry->GetHash(), pEntry);
            _ASSERTE(pDeletedEntry == pEntry);
            delete pEntry;
            fDeletedEntry = true;
        }
        ++iter;
    }

    if (fDeletedEntry)
        s_pFieldMap->Compact();
}

FieldDesc **CrossDomainFieldMap::LookupOrCreateFieldMapping(MethodTable *pDstMT, MethodTable *pSrcMT)
{
    CONTRACTL
    {
        MODE_COOPERATIVE;
        GC_TRIGGERS;
        THROWS;
    }
    CONTRACTL_END;

    // We lazily allocate the reader/writer lock we synchronize access to the hash with.
    if (s_pFieldMapLock == NULL)
    {
        void *pLockSpace = SystemDomain::GetGlobalLoaderAllocator()->GetLowFrequencyHeap()->AllocMem(S_SIZE_T(sizeof(SimpleRWLock)));
        SimpleRWLock *pLock = new (pLockSpace) SimpleRWLock(COOPERATIVE_OR_PREEMPTIVE, LOCK_TYPE_DEFAULT);
        
        if (FastInterlockCompareExchangePointer(&s_pFieldMapLock, pLock, NULL) != NULL)
            // We lost the race, give up our copy.
            SystemDomain::GetGlobalLoaderAllocator()->GetLowFrequencyHeap()->BackoutMem(pLockSpace, sizeof(SimpleRWLock));
    }

    // Now we have a lock we can use to synchronize the remainder of the init.
    if (s_pFieldMap == NULL)
    {
        SimpleWriteLockHolder swlh(s_pFieldMapLock);

        if (s_pFieldMap == NULL)
        {
            PtrHashMap *pMap = new (SystemDomain::GetGlobalLoaderAllocator()->GetLowFrequencyHeap()) PtrHashMap();
            LockOwner lock = {s_pFieldMapLock, IsOwnerOfRWLock};
            pMap->Init(32, CompareFieldMapEntry, TRUE, &lock);
            s_pFieldMap = pMap;
        }
    }
    else
    {
        // Try getting an existing value first.

        FieldMapEntry sEntry(pSrcMT, pDstMT, NULL);

        SimpleReadLockHolder srlh(s_pFieldMapLock);
        FieldMapEntry *pFound = (FieldMapEntry *)s_pFieldMap->LookupValue(sEntry.GetHash(), (LPVOID)&sEntry);
        if (pFound != (FieldMapEntry *)INVALIDENTRY)
            return pFound->m_pFieldMap;
    }

    // We couldn't find an existing entry in the hash. Now we must go through the painstaking process of matching fields in the
    // destination object to their counterparts in the source object. We build an array of pointers to source field descs ordered by
    // destination type field index (all the fields for the most derived type first, then all the fields for the second most derived
    // type etc.).
    NewArrayHolder<FieldDesc*> pFieldMap(new FieldDesc*[pDstMT->GetNumInstanceFields()]);
    DWORD dwMapIndex = 0;

    // We start with the source and destination types for the object (which we know are equivalent at least in type name). For each
    // layer of the type hierarchy for the destination object (from the instance type through to Object) we attempt to locate the
    // corresponding source type in the hierarchy. This is non-trivial since either source or destination type hierarchies may have
    // added or removed layers. We ignore extra type layers in the source hierarchy and just concentrate on destination type layers
    // that introduce instance fields that are not marked NotSerializable. For each such layer we first locate the corresponding
    // source layer (via fully qualified type name) and then map each serialized (and possibly optional) destination field to the
    // corresponding source field (again by name). We don't allow a field to move around the type hierarchy (i.e. a field defined in
    // the base class in one version can't move to a derived type in later versions and be recognized as the original field).
    // Allowing this would introduce all sorts of ambiguity problems (consider the case of private fields all with the same name
    // implemented at every layer of the type hierarchy).

    bool fFirstPass = true;
    MethodTable *pCurrDstMT = pDstMT;
    MethodTable *pCurrSrcMT = pSrcMT;
    while (pCurrDstMT)
    {
        DWORD numInstanceFields = pCurrDstMT->GetNumIntroducedInstanceFields();

        // Skip destination types with no instance fields to clone.
        if (numInstanceFields == 0)
        {
            pCurrDstMT = pCurrDstMT->GetParentMethodTable();
            // Only safe to skip the source type as well on the first pass (the source version may have eliminated this level of
            // the type hierarchy).
            if (fFirstPass)
                pCurrSrcMT = pCurrSrcMT->GetParentMethodTable();
            fFirstPass = false;
            continue;
        }

        // We need to synchronize the source type with the destination type. This means skipping any source types in the
        // hierarchy that the destination doesn't know about.
        MethodTable *pCandidateMT = pCurrSrcMT;
        while (pCandidateMT)
        {
            if (fFirstPass || pCandidateMT == pCurrDstMT || AreTypesEquivalent(pCandidateMT, pCurrDstMT))
            {
                // Skip intermediate source types (the destination type didn't know anything about them, so they're surplus
                // to requirements).
                pCurrSrcMT = pCandidateMT;
                break;
            }

            pCandidateMT = pCandidateMT->GetParentMethodTable();
        }

#ifdef OBJECT_CLONER_STRICT_MODE
        // If there's no candidate source type equivalent to the current destination type we need to prove that the destination
        // type has no mandatory instance fields or throw an exception (since there's no place to fetch the field values from).
        if (pCandidateMT == NULL)
        {
            FieldDesc *pFields = pCurrDstMT->GetApproxFieldDescListRaw();
            
            for (DWORD i = 0; i < numInstanceFields; i++)
            {
                if (pFields[i].IsNotSerialized() || pFields[i].IsOptionallySerialized())
                {
                    pFieldMap[dwMapIndex++] = NULL;
                    continue;
                }

                // We've found a field that must be cloned but have no corresponding source-side type to clone it from. Raise an
                // exception.
                ThrowMissingFieldException(&pFields[i]);
            }

            // If we get here we know the current destination type level was effectively a no-op. Move onto the next level.
            pCurrDstMT = pCurrDstMT->GetParentMethodTable();
            fFirstPass = false;
            continue;
        }
#else
        // In lax matching mode we can ignore all fields, even those not marked optional. So the lack of an equivalent type in the
        // source hierarchy doesn't bother us. Mark all fields as having a default value and then move onto the next level in the
        // type hierarchy.
        if (pCandidateMT == NULL)
        {
            for (DWORD i = 0; i < numInstanceFields; i++)
                pFieldMap[dwMapIndex++] = NULL;

            pCurrDstMT = pCurrDstMT->GetParentMethodTable();
            fFirstPass = false;
            continue;
        }
#endif

        // If we get here we have equivalent types in pCurrDstMT and pCurrSrcMT. Now we need to locate the source field desc
        // corresponding to every mandatory (and possibly optional) field in the destination type and record it in the field map.
        DWORD numSrcFields = pCurrSrcMT->GetNumIntroducedInstanceFields();
        DWORD numDstFields = pCurrDstMT->GetNumIntroducedInstanceFields();

        FieldDesc *pDstFields = pCurrDstMT->GetApproxFieldDescListRaw();
        FieldDesc *pSrcFields = pCurrSrcMT->GetApproxFieldDescListRaw();

        for (DWORD i = 0; i < numDstFields; i++)
        {
            // Non-serialized destination fields aren't filled in from source types.
            if (pDstFields[i].IsNotSerialized())
            {
                pFieldMap[dwMapIndex++] = NULL;
                continue;
            }

            // Go look for a field in the source type with the same name.
            LPCUTF8 szDstFieldName = pDstFields[i].GetName();
            DWORD j;
            for (j = 0; j < numSrcFields; j++)
            {
                LPCUTF8 szSrcFieldName = pSrcFields[j].GetName();
                if (strcmp(szDstFieldName, szSrcFieldName) == 0)
                {
                    // Check that the field isn't marked NotSerialized (if it is then it's invisible to the cloner).
                    if (pSrcFields[j].IsNotSerialized())
                        j = numSrcFields;
                    break;
                }
            }

#ifdef OBJECT_CLONER_STRICT_MODE
            // If we didn't find a corresponding field it might not be fatal; the field could be optionally serializable from the
            // destination type's point of view.
            if (j == numSrcFields)
            {
                if (pDstFields[i].IsOptionallySerialized())
                {
                    pFieldMap[dwMapIndex++] = NULL;
                    continue;
                }
                // The field was required. Throw an exception.
                ThrowMissingFieldException(&pDstFields[i]);
            }
#else
            // In lax matching mode we can ignore all fields, even those not marked optional. Simply mark this field as having the
            // default value.
            if (j == numSrcFields)
            {
                pFieldMap[dwMapIndex++] = NULL;
                continue;
            }
#endif

            // Otherwise we found matching fields (in name at least, type processing is done later).
            pFieldMap[dwMapIndex++] = &pSrcFields[j];
        }

        pCurrDstMT = pCurrDstMT->GetParentMethodTable();
        pCurrSrcMT = pCurrSrcMT->GetParentMethodTable();
        fFirstPass = false;
    }

    _ASSERTE(dwMapIndex == pDstMT->GetNumInstanceFields());

    // Now we have a field map we should insert it into the hash.
    NewHolder<FieldMapEntry> pEntry(new FieldMapEntry(pSrcMT, pDstMT, pFieldMap));
    PREFIX_ASSUME(pEntry != NULL);
    pFieldMap.SuppressRelease();

    SimpleWriteLockHolder swlh(s_pFieldMapLock);

    UPTR key = pEntry->GetHash();

    FieldMapEntry *pFound = (FieldMapEntry *)s_pFieldMap->LookupValue(key, (LPVOID)pEntry);
    if (pFound == (FieldMapEntry *)INVALIDENTRY)
    {
        s_pFieldMap->InsertValue(key, (LPVOID)pEntry);
        pEntry.SuppressRelease();
        return pFieldMap;
    }
    else
        return pFound->m_pFieldMap;
}

ARG_SLOT ObjectClone::HandleFieldTypeMismatch(CorElementType dstType, CorElementType srcType, void *pData, MethodTable *pSrcMT)
{
    CONTRACTL
    {
        MODE_COOPERATIVE;
        GC_TRIGGERS;
        THROWS;
    }
    CONTRACTL_END
    _ASSERTE(m_context != ObjectFreezer);
    ARG_SLOT data = 0;
    InvokeUtil::CreatePrimitiveValue(dstType, srcType, pData, pSrcMT, &data);
    return data;
}

void ObjectClone::ScanISerializableMembers(DWORD IObjRefTSOIndex, DWORD ISerTSOIndex, DWORD BoxedValTSOIndex, PTRARRAYREF refValues)
{
    CONTRACTL
    {
        MODE_COOPERATIVE;
        GC_TRIGGERS;
        THROWS;
    }
    CONTRACTL_END
        
    _ASSERTE(m_context != ObjectFreezer);
    // Queue the non-primitive types
    DWORD       numFieldsToBeMarshalled = 0;
    PTRARRAYREF refNewValues = NULL;

    LOG((LF_REMOTING, LL_INFO1000, "ScanISerializableMembers. Scanning members of ISerializable type object.\n"));
    GCPROTECT_BEGIN(refValues);
    
    refNewValues = (PTRARRAYREF) AllocateObjectArray(refValues->GetNumComponents(), g_pObjectClass, FALSE);

    _ASSERTE(refNewValues != NULL);
    
    for (DWORD index = 0; index < refValues->GetNumComponents(); index++)
    {
        OBJECTREF refField = refValues->GetAt(index);
        if (refField == NULL)
            continue;
        
        if (CorTypeInfo::IsPrimitiveType(refField->GetTypeHandle().GetSignatureCorElementType()) ||
            refField->GetMethodTable() == g_pStringClass)
        {
            refNewValues->SetAt(index, refField);
            continue;
        }

        ISerializableMemberInfo isInfo(ISerTSOIndex, index);
        QOM.Enqueue(refField, NULL, NULL, (QueuedObjectInfo *) &isInfo);
        numFieldsToBeMarshalled++;
        refNewValues->SetAt(index, NULL);
        LOG((LF_REMOTING, LL_INFO1000, "ScanISerializableMembers. Member at index %d is reference type. Adding to QOM.\n", index));
    }
    GCPROTECT_END();
    
    // Update TSO
    OBJECTREF refNames = NULL, refFields = NULL;
    QueuedObjectInfo *pDummy;
    OBJECTREF newObj;
    newObj = TSO.GetAt(ISerTSOIndex, &refNames, &refFields, &pDummy);
    _ASSERTE(newObj == m_newObject);
    
    TSO.SetAt(ISerTSOIndex, m_newObject, refNames, refNewValues, pDummy);

    if (numFieldsToBeMarshalled > 0)
    {
        ParentInfo fxInfo(numFieldsToBeMarshalled);
        fxInfo.SetIsISerializableInstance();
        fxInfo.SetIObjRefIndexIntoTSO(IObjRefTSOIndex);
        fxInfo.SetISerIndexIntoTSO(ISerTSOIndex);
        fxInfo.SetBoxedValIndexIntoTSO(BoxedValTSOIndex);
        QOF.Enqueue(m_newObject, NULL, NULL, (QueuedObjectInfo *) &fxInfo);
        LOG((LF_REMOTING, LL_INFO1000, "ScanISerializableMembers. Current object had total of %d reference type fields. Adding to QOF.\n", numFieldsToBeMarshalled));
        // Delay calling any OnDeserialized callbacks until the end of the cloning operation (it's difficult to tell when all the
        // children have been deserialized).
        if (HasVtsCallbacks(m_newObject->GetMethodTable(), RemotingVtsInfo::VTS_CALLBACK_ON_DESERIALIZED))
            VDC.Enqueue(m_newObject, NULL, NULL, NULL);
        if (m_cbInterface->RequiresDeserializationCallback(m_newObject->GetMethodTable()))
        {
            LOG((LF_REMOTING, LL_INFO1000, "ScanISerializableMembers. Adding object to Table of IDeserialization Callbacks\n"));
            QueuedObjectInfo noInfo;
            TDC.Enqueue(m_newObject, NULL, NULL, &noInfo);
        }
    }
    else
    {
        // This is effectively a leaf node (no complex children) so if the type has a callback for OnDeserialized we'll deliver it
        // now. This fixes callback ordering for a few more edge cases (e.g. VSW 415611) and is reasonably cheap. We can never do a
        // perfect job (in the presence of object graph cycles) and a near perfect job (intuitively ordered callbacks for acyclic
        // object graphs) is prohibitively expensive; so we're stuck with workarounds like this.
        InvokeVtsCallbacks(m_newObject, RemotingVtsInfo::VTS_CALLBACK_ON_DESERIALIZED, m_toDomain);
        if (m_cbInterface->RequiresDeserializationCallback(m_newObject->GetMethodTable()))
            MakeIDeserializationCallback(m_newObject);
    }
}

void ObjectClone::ScanArrayMembers()
{
    CONTRACTL
    {
        MODE_COOPERATIVE;
        GC_TRIGGERS;
        THROWS;
    }
    CONTRACTL_END
#ifdef _DEBUG
    MethodTable *pCurrMT = m_currObject->GetMethodTable();
    _ASSERTE(pCurrMT && pCurrMT->IsArray());
    MethodTable *pNewMT = m_newObject->GetMethodTable();
    _ASSERTE(pNewMT && pNewMT->IsArray());
#endif

    LOG((LF_REMOTING, LL_INFO1000, "ScanArrayMembers. Scanning members of array object.\n"));
    BASEARRAYREF refFromArray = (BASEARRAYREF) m_currObject;
    BASEARRAYREF refToArray = (BASEARRAYREF) m_newObject;

    GCPROTECT_BEGIN(refFromArray);
    GCPROTECT_BEGIN(refToArray);
    
    TypeHandle toArrayElementType = refToArray->GetArrayElementTypeHandle();
    DWORD numComponents = refFromArray->GetNumComponents();
    MethodTable *pArrayMT = refFromArray->GetMethodTable();

    DWORD rank                   = pArrayMT->GetRank();
    DWORD dwOffset               = 0;

    DWORD *pIndices = (DWORD*) _alloca(sizeof(DWORD) * rank);
    VOID *pTemp = _alloca(sizeof(NDimArrayMemberInfo) + rank * sizeof(DWORD));
    NDimArrayMemberInfo *pArrInfo = new (pTemp) NDimArrayMemberInfo(rank);
    
    bool boxingObjects = (pArrayMT->GetArrayElementType() == ELEMENT_TYPE_VALUETYPE);

    // Must enter the from domain if we are going to be allocating any non-agile boxes
    ENTER_DOMAIN_PTR_PREDICATED(m_fromDomain,ADV_RUNNINGIN,boxingObjects);

    if (boxingObjects)
    {
        pArrInfo->SetNeedsUnboxing();

        // We may be required to activate value types of array elements, since we 
        // are going to box them.  Hoist out the required domain transition and 
        // activation.

        MethodTable *pMT = ((BASEARRAYREF)m_currObject)->GetArrayElementTypeHandle().GetMethodTable();
        pMT->EnsureInstanceActive();
    }

    DWORD numFixupsNeeded = 0;
    for (DWORD i = 0; i < numComponents; i++) 
    {
        // The array could be huge. To avoid keeping a pending GC waiting (and maybe timing out) we're going to pulse the GC mode
        // every so often. Do this more freqeuntly in debug builds, where each iteration through this loop takes considerably
        // longer.
#ifdef _DEBUG
#define COPY_CYCLES 1024
#else
#define COPY_CYCLES 8192
#endif
        if ((i % COPY_CYCLES) == (COPY_CYCLES - 1))
            GetThread()->PulseGCMode();

        const INT32 *pBoundsPtr      = refFromArray->GetBoundsPtr();
        DWORD findIndices = i;
        for (DWORD rankIndex = rank; rankIndex > 0; rankIndex--)
        {
            DWORD numElementsInDimension = pBoundsPtr[rankIndex - 1]; 
            DWORD quotient = findIndices / numElementsInDimension;
            DWORD remainder = findIndices % numElementsInDimension;
            pIndices[rankIndex - 1] = remainder;
            findIndices = quotient;
        }
        
        pArrInfo->SetIndices(pIndices);

        Object *rv = GetObjectFromArray((BASEARRAYREF *)&m_currObject, dwOffset);
        if (rv != NULL)
        {
            OBJECTREF oRef = ObjectToOBJECTREF(rv);

            if (oRef->GetMethodTable() == g_pStringClass && m_context != ObjectFreezer)
            {
                OBJECTREF* pElem = (OBJECTREF*)(refToArray->GetDataPtr() + (dwOffset * pArrayMT->GetComponentSize()));
                SetObjectReference(pElem,oRef,GetAppDomain());
            }
            else
            {
                // Add the object to QOM
                numFixupsNeeded++;
                QOM.Enqueue(oRef, NULL, NULL, pArrInfo);
                LOG((LF_REMOTING, LL_INFO1000, "ScanArrayMembers. Element at offset %d is reference type. Adding to QOM.\n", dwOffset));
            }
        }
        dwOffset ++;
    }

    if (numFixupsNeeded > 0)
    {
        ParentInfo fxInfo(numFixupsNeeded);
        QOF.Enqueue(m_newObject, NULL, NULL, (QueuedObjectInfo *)&fxInfo);
        LOG((LF_REMOTING, LL_INFO1000, "ScanArrayMembers. Current object had total of %d reference type fields. Adding to QOF.\n", numFixupsNeeded));
    }

    END_DOMAIN_TRANSITION;

    GCPROTECT_END();
    GCPROTECT_END();
}

#ifdef _MSC_VER
#pragma warning(push)
#pragma warning(disable:4244)
#endif // _MSC_VER
Object *ObjectClone::GetObjectFromArray(BASEARRAYREF* arrObj, DWORD dwOffset)
{
    CONTRACTL {
        THROWS;
        if ((*arrObj)->GetArrayElementTypeHandle().GetMethodTable()->IsValueType()) GC_TRIGGERS; else GC_NOTRIGGER;
    } CONTRACTL_END;

    // Get the type of the element...
    switch ((*arrObj)->GetArrayElementType()) {

    case ELEMENT_TYPE_VOID:
        return NULL;

    case ELEMENT_TYPE_CLASS:        // Class
    case ELEMENT_TYPE_SZARRAY:      // Single Dim, Zero
    case ELEMENT_TYPE_ARRAY:        // General Array
    case ELEMENT_TYPE_STRING:
    case ELEMENT_TYPE_OBJECT:
        {
            _ASSERTE((*arrObj)->GetComponentSize() == sizeof(OBJECTREF));
            BYTE* pData = ((BYTE*)(*arrObj)->GetDataPtr()) + (dwOffset * sizeof(OBJECTREF));
            return *(Object **)pData;
        }

    case ELEMENT_TYPE_VALUETYPE:
        {
            MethodTable *pMT = (*arrObj)->GetArrayElementTypeHandle().GetMethodTable();
            WORD wComponentSize = (*arrObj)->GetComponentSize();
            BYTE* pData = ((BYTE*)(*arrObj)->GetDataPtr()) + (dwOffset * wComponentSize);
            return OBJECTREFToObject(pMT->Box(pData));
        }
    case ELEMENT_TYPE_BOOLEAN:      // boolean
    case ELEMENT_TYPE_I1:           // sbyte
    case ELEMENT_TYPE_U1:
    case ELEMENT_TYPE_I2:           // short
    case ELEMENT_TYPE_U2:
    case ELEMENT_TYPE_CHAR:         // char
    case ELEMENT_TYPE_I4:           // int
    case ELEMENT_TYPE_I:
    case ELEMENT_TYPE_U:
    case ELEMENT_TYPE_U4:
    case ELEMENT_TYPE_I8:           // long
    case ELEMENT_TYPE_U8:
    case ELEMENT_TYPE_R4:           // float
    case ELEMENT_TYPE_R8:           // double
    case ELEMENT_TYPE_PTR:
        {
            // Note that this is a cloned version of the value class case above for performance

            // Watch for GC here.  We allocate the object and then
            //  grab the void* to the data we are going to copy.
            MethodTable *pMT = (*arrObj)->GetArrayElementTypeHandle().GetMethodTable();
            OBJECTREF obj = ::AllocateObject(pMT);
            WORD wComponentSize = (*arrObj)->GetComponentSize();
            BYTE* pData = ((BYTE*)(*arrObj)->GetDataPtr()) + (dwOffset * wComponentSize);
            CopyValueClassUnchecked(obj->UnBox(), pData, (*arrObj)->GetArrayElementTypeHandle().GetMethodTable());
            return OBJECTREFToObject(obj);
        }

    case ELEMENT_TYPE_END:
    default:
        _ASSERTE(!"Unknown array element type");
    }

    _ASSERTE(!"Should never get here");
    return NULL;
}
#ifdef _MSC_VER
#pragma warning(pop)
#endif // _MSC_VER: warning C4244


void ObjectClone::CompleteValueTypeFields(OBJECTREF newObj, OBJECTREF refParent, QueuedObjectInfo *objInfo)
{
    CONTRACTL
    {
        MODE_COOPERATIVE;
        GC_TRIGGERS;
        THROWS;
    }
    CONTRACTL_END

#ifdef _DEBUG
    {
        SString ssTypeName;
        SString ssParentTypeName;
        newObj->GetMethodTable()->_GetFullyQualifiedNameForClassNestedAware(ssTypeName);
        refParent->GetMethodTable()->_GetFullyQualifiedNameForClassNestedAware(ssParentTypeName);
        LOG((LF_REMOTING, LL_INFO1000, "CompleteValueTypeFields. Fixing up value type field of type %S into parent of type %S.\n",
            ssTypeName.GetUnicode(), ssParentTypeName.GetUnicode()));
    }
#endif

    ValueTypeInfo *pValTypeInfo = (ValueTypeInfo *)objInfo;
    QueuedObjectInfo *pFixupInfo = pValTypeInfo->GetFixupInfo();
    PREFIX_ASSUME(pFixupInfo != NULL);
    
    _ASSERTE(pFixupInfo->NeedsUnboxing());
    if (pFixupInfo->IsArray())
    {
        m_newObject = newObj;
        HandleArrayFixup(refParent, pFixupInfo);
    }
    else
    {
        GCPROTECT_BEGIN(refParent);
        GCPROTECT_BEGIN(newObj);
        ObjectMemberInfo *pObjInfo = (ObjectMemberInfo *)pFixupInfo;
        FieldDesc *pTargetField = pObjInfo->GetFieldDesc();

        TypeHandle fldType = LoadExactFieldType(pTargetField, refParent, m_toDomain);
        void *pDest = refParent->GetData() + pTargetField->GetOffset();    
        _ASSERTE(GetAppDomain()==m_toDomain);

        if (!fldType.GetMethodTable()->UnBoxInto(pDest, newObj))
            COMPlusThrow(kArgumentException,W("Arg_ObjObj"));
    
        GCPROTECT_END();
        GCPROTECT_END();
    }
    pValTypeInfo->SetHasBeenProcessed();
}

void ObjectClone::CompleteSpecialObjects()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;
    
    OBJECTREF nextObj = NULL;
    OBJECTREF refNames = NULL;
    OBJECTREF refValues = NULL;
    SpecialObjectInfo *pObjInfo = NULL;

    GCPROTECT_BEGIN(refNames);
    GCPROTECT_BEGIN(refValues);
    
    DWORD skippedObjects = 0;
    DWORD numLoops = 0;

    if (TSO.GetCount() == 0)
        goto EarlyExit;

    LOG((LF_REMOTING, LL_INFO1000, "CompleteSpecialObjects. Beginning.\n"));
    do
    {
        skippedObjects = 0;
        numLoops++;
        DWORD index = 0;
        TSO.BeginEnumeration(&index);
        while((nextObj = TSO.GetNext(&index, &refNames, &refValues, (QueuedObjectInfo **)&pObjInfo)) != NULL)
        {
            if (pObjInfo->HasBeenProcessed())
                continue;
            
            if (pObjInfo->IsISerializableInstance())
            {
                _ASSERTE(m_context != ObjectFreezer);
                
                LOG((LF_REMOTING, LL_INFO1000, "CompleteSpecialObjects. ISerializable instance at index %d.\n", index));
                ISerializableInstanceInfo *iserInfo = (ISerializableInstanceInfo *)pObjInfo;
                if (iserInfo->GetNumSpecialMembers() > 0)
                {
                    if (CheckForUnresolvedMembers(iserInfo))
                    {
                        LOG((LF_REMOTING, LL_INFO1000, "CompleteSpecialObjects. Skipping ISerializable instance due to unresolved members.\n"));
                        skippedObjects++;
                        continue;
                    }
                }
                CompleteISerializableObject(nextObj, refNames, refValues, iserInfo);
            }
            else if (pObjInfo->IsIObjRefInstance())
            {
                _ASSERTE(m_context != ObjectFreezer);
                
                LOG((LF_REMOTING, LL_INFO1000, "CompleteSpecialObjects. IObjectReference instance at index %d.\n", index));
                IObjRefInstanceInfo *iorInfo = (IObjRefInstanceInfo *)pObjInfo;
                if (iorInfo->GetNumSpecialMembers() > 0 || 
                    iorInfo->GetISerTSOIndex() != (DWORD) -1)
                {
                    if (CheckForUnresolvedMembers(iorInfo))
                    {
                        LOG((LF_REMOTING, LL_INFO1000, "CompleteSpecialObjects. Skipping IObjectReference instance due to unresolved members.\n"));
                        skippedObjects++;
                        continue;
                    }
                }
                if (!CompleteIObjRefObject(nextObj, index, iorInfo))
                    skippedObjects++;
            }
            else
            {
                _ASSERTE(pObjInfo->IsBoxedObject());
                LOG((LF_REMOTING, LL_INFO1000, "CompleteSpecialObjects. Boxed valuetype instance at index %d.\n", index));
                ValueTypeInfo *valTypeInfo = (ValueTypeInfo *)pObjInfo;
                if (valTypeInfo->GetNumSpecialMembers() > 0 ||
                    valTypeInfo->GetISerTSOIndex() != (DWORD) -1 ||
                    valTypeInfo->GetIObjRefTSOIndex() != (DWORD) -1)
                {
                    if (CheckForUnresolvedMembers(valTypeInfo))
                    {
                        LOG((LF_REMOTING, LL_INFO1000, "CompleteSpecialObjects. Skipping boxed value instance due to unresolved members.\n"));
                        skippedObjects++;
                        continue;
                    }
                }
                // If we were waiting on an IObjRef fixup then the target object will have changed.
                if (valTypeInfo->GetIObjRefTSOIndex() != (DWORD) -1)
                {
                    OBJECTREF dummy1, dummy2;
                    QueuedObjectInfo *dummy3;
                    nextObj = TSO.GetAt(valTypeInfo->GetIObjRefTSOIndex(), &dummy1, &dummy2, &dummy3);
                }
                CompleteValueTypeFields(nextObj, refNames, valTypeInfo);
            }
            
        };
    } while (skippedObjects > 0 && numLoops < 100);

    if (skippedObjects > 0 && numLoops >= 100)
    {
        COMPlusThrow(kSerializationException, IDS_SERIALIZATION_UNRESOLVED_SPECIAL_OBJECT);
    }
EarlyExit: ;
    GCPROTECT_END();
    GCPROTECT_END();
}

BOOL ObjectClone::CheckForUnresolvedMembers(SpecialObjectInfo *splInfo)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        SO_TOLERANT;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;
    
    BOOL foundUnresolvedMember = FALSE;

    DWORD mappingIndex = splInfo->GetMappingTableIndex();
    for (DWORD count = 0; count < splInfo->GetNumSpecialMembers(); count++)
    {
        DWORD memberIndex = TMappings.GetAt(mappingIndex++);
        SpecialObjectInfo *pMemberInfo;
        OBJECTREF dummy1, dummy2, dummy3;
        dummy1 = TSO.GetAt(memberIndex, &dummy2, &dummy3, (QueuedObjectInfo **)&pMemberInfo);
        // An unresolved IObjRef member is a blocker for any special object parent
        if (pMemberInfo->IsIObjRefInstance() && !pMemberInfo->HasBeenProcessed())
        {
            LOG((LF_REMOTING, LL_INFO1000, "CheckForUnresolvedMembers. Found unresolved IObjectReference member at index %d.\n", memberIndex));
            foundUnresolvedMember = TRUE;
            break;
        }

        // An unresolved ISer member is a blocker for IObjRef parent
        if (pMemberInfo->IsISerializableInstance() && 
            !pMemberInfo->HasBeenProcessed() &&
            splInfo->IsIObjRefInstance())
        {
            LOG((LF_REMOTING, LL_INFO1000, "CheckForUnresolvedMembers. Found unresolved ISerializable member at index %d.\n", memberIndex));
            foundUnresolvedMember = TRUE;
            break;
        }

        // An unresolved boxed object is a blocker for a boxed parent or an IObjRef parent
        if (pMemberInfo->IsBoxedObject() && 
            !pMemberInfo->HasBeenProcessed() &&
            (splInfo->IsIObjRefInstance() || splInfo->IsBoxedObject()))
        {
            LOG((LF_REMOTING, LL_INFO1000, "CheckForUnresolvedMembers. Found unresolved boxed valuetype member at index %d.\n", memberIndex));
            foundUnresolvedMember = TRUE;
            break;
        }
    }

    // Done checking members. Now check if this instance itself needs some processing
    // If an instance is both ISer and IObj, then ISer should be processed before IObjRef
    if (!foundUnresolvedMember && splInfo->IsIObjRefInstance())
    {
        IObjRefInstanceInfo *pObjRefInfo = (IObjRefInstanceInfo *)splInfo;
        if (pObjRefInfo->GetISerTSOIndex() != (DWORD) -1)
        {
            // Check if the ISer requirements have been met
            SpecialObjectInfo *pMemberInfo;
            OBJECTREF dummy1, dummy2, dummy3;
            dummy1 = TSO.GetAt(pObjRefInfo->GetISerTSOIndex(), &dummy2, &dummy3, (QueuedObjectInfo **)&pMemberInfo);
            if (!pMemberInfo->HasBeenProcessed())
            {
                LOG((LF_REMOTING, LL_INFO1000, "CheckForUnresolvedMembers. This instance is also ISerializable at index %d. Not resolved yet.\n", pObjRefInfo->GetISerTSOIndex()));
                foundUnresolvedMember = TRUE;
            }
        }
    }

    // If an instance is ISer, IObj and a boxed value type, then ISer,IObj should be processed before unboxing
    if (!foundUnresolvedMember && splInfo->IsBoxedObject())
    {
        ValueTypeInfo *pValTypeInfo = (ValueTypeInfo *)splInfo;
        if (pValTypeInfo->GetISerTSOIndex() != (DWORD) -1)
        {
            // Check if the ISer requirements have been met
            SpecialObjectInfo *pMemberInfo;
            OBJECTREF dummy1, dummy2, dummy3;
            dummy1 = TSO.GetAt(pValTypeInfo->GetISerTSOIndex(), &dummy2, &dummy3, (QueuedObjectInfo **)&pMemberInfo);
            if (!pMemberInfo->HasBeenProcessed())
            {
                LOG((LF_REMOTING, LL_INFO1000, "CheckForUnresolvedMembers. This instance is also ISerializable at index %d. Not resolved yet.\n", pValTypeInfo->GetISerTSOIndex()));
                foundUnresolvedMember = TRUE;
            }
        }
        if (!foundUnresolvedMember && pValTypeInfo->GetIObjRefTSOIndex() != (DWORD) -1)
        {
            // Check if the ISer requirements have been met
            SpecialObjectInfo *pMemberInfo;
            OBJECTREF dummy1, dummy2, dummy3;
            dummy1 = TSO.GetAt(pValTypeInfo->GetIObjRefTSOIndex(), &dummy2, &dummy3, (QueuedObjectInfo **)&pMemberInfo);
            if (!pMemberInfo->HasBeenProcessed())
            {
                LOG((LF_REMOTING, LL_INFO1000, "CheckForUnresolvedMembers. This instance is also IObjectReference at index %d. Not resolved yet.\n", pValTypeInfo->GetIObjRefTSOIndex()));
                foundUnresolvedMember = TRUE;
            }
        }
    }
    return foundUnresolvedMember;
}

void ObjectClone::CompleteISerializableObject(OBJECTREF IserObj, OBJECTREF refNames, OBJECTREF refValues, ISerializableInstanceInfo *iserInfo)
{
    CONTRACTL
    {
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        THROWS;
    }
    CONTRACTL_END

    _ASSERTE(m_context != ObjectFreezer);
    
    struct _gc {
        OBJECTREF   IserObj;
        OBJECTREF   refNames;
        OBJECTREF   refValues;
        OBJECTREF   refSerInfo;
    } gc;

    gc.IserObj = IserObj;
    gc.refNames = refNames;
    gc.refValues = refValues;
    gc.refSerInfo = NULL;

    GCPROTECT_BEGIN(gc);

#ifdef _DEBUG
    {
        DefineFullyQualifiedNameForClass();
        LOG((LF_REMOTING, LL_INFO1000, "CompleteISerializableObject. Completing ISerializable object of type %s.\n",
            GetFullyQualifiedNameForClassNestedAware(gc.IserObj->GetMethodTable())));
    }
#endif

    BOOL    bIsBoxed = gc.IserObj->GetMethodTable()->IsValueType();

    // StreamingContextData is an out parameter of the managed callback, so it's passed by reference on all platforms.
    RuntimeMethodHandle::StreamingContextData context = {0}; 
    
    PREPARE_NONVIRTUAL_CALLSITE(METHOD__OBJECTCLONEHELPER__PREPARE_DATA);

    DECLARE_ARGHOLDER_ARRAY(args, 4);

    args[ARGNUM_0]    = OBJECTREF_TO_ARGHOLDER(gc.IserObj);
    args[ARGNUM_1]    = OBJECTREF_TO_ARGHOLDER(gc.refNames);
    args[ARGNUM_2] = OBJECTREF_TO_ARGHOLDER(gc.refValues);
    args[ARGNUM_3] = PTR_TO_ARGHOLDER(&context);

    CATCH_HANDLER_FOUND_NOTIFICATION_CALLSITE;
    CALL_MANAGED_METHOD_RETREF(gc.refSerInfo, OBJECTREF, args);

    if (iserInfo->IsTargetNotISerializable())
    {
        // Prepare data would have constructed the object already
        _ASSERTE(gc.refSerInfo == NULL);
    }
    else
    {
        _ASSERTE(gc.refSerInfo != NULL);
        MethodTable *pMT = gc.IserObj->GetMethodTable();
        _ASSERTE(pMT);

        MethodDesc * pCtor;

#ifdef FEATURE_IMPERSONATION
        // Deal with the WindowsIdentity class specially by calling an internal
        // serialization constructor; the public one has a security demand that
        // breaks partial trust scenarios and is too expensive to assert for.
        if (MscorlibBinder::IsClass(pMT, CLASS__WINDOWS_IDENTITY))
            pCtor = MscorlibBinder::GetMethod(METHOD__WINDOWS_IDENTITY__SERIALIZATION_CTOR);
        else
#endif
            pCtor = MemberLoader::FindConstructor(pMT, &gsig_IM_SerInfo_StrContext_RetVoid);
        
        if (pCtor == NULL)
        {
            DefineFullyQualifiedNameForClassW();
            COMPlusThrow(kSerializationException, IDS_SERIALIZATION_CTOR_NOT_FOUND,
                         GetFullyQualifiedNameForClassNestedAwareW(pMT));
        }

        MethodDescCallSite ctor(pCtor);

        ARG_SLOT argSlots[3];
            // Nullable<T> does not implement ISerializable.  
        _ASSERTE(!Nullable::IsNullableType(gc.IserObj->GetMethodTable()));
        argSlots[0] = (bIsBoxed ? (ARG_SLOT)(SIZE_T)(gc.IserObj->UnBox()) : ObjToArgSlot(gc.IserObj));
        argSlots[1] = ObjToArgSlot(gc.refSerInfo);
#if defined(_TARGET_X86_) || defined(_TARGET_ARM_)
        static_assert_no_msg(sizeof(context) == sizeof(ARG_SLOT));
        argSlots[2] = *(ARG_SLOT*)(&context);           // StreamingContext is passed by value on x86 and ARM
#elif defined(_WIN64)
        static_assert_no_msg(sizeof(context) >  sizeof(ARG_SLOT));
        argSlots[2] = PtrToArgSlot(&context);           // StreamingContext is passed by reference on WIN64
#else  // !_TARGET_X86_ && !_WIN64 && !_TARGET_ARM_
        PORTABILITY_ASSERT("ObjectClone::CompleteISerializableObject() - NYI on this platform");
#endif // !_TARGET_X86_ && !_WIN64 && !_TARGET_ARM_
        ctor.CallWithValueTypes(&argSlots[0]);
    }
    iserInfo->SetHasBeenProcessed();

    GCPROTECT_END();

}

// FALSE means the object could not be resolved and need to perform more iterations
BOOL ObjectClone::CompleteIObjRefObject(OBJECTREF IObjRef, DWORD tsoIndex, IObjRefInstanceInfo *iorInfo)
{
    CONTRACTL
    {
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        THROWS;
    }
    CONTRACTL_END

    BOOL bResult = FALSE;

    struct _gc {
        OBJECTREF   IObjRef;
        OBJECTREF   newObj;
        OBJECTREF   refParent;
        OBJECTREF   refFromObj;
        OBJECTREF   resolvedObject;
    } gc;

    gc.IObjRef = IObjRef;
    gc.newObj = NULL;
    gc.refParent = NULL;
    gc.refFromObj = NULL;
    gc.resolvedObject = NULL;

    GCPROTECT_BEGIN(gc);

    _ASSERTE(m_context != ObjectFreezer);
    // First check if this is a repeat object
    if (iorInfo->IsRepeatObject())
    {
        OBJECTREF dummy;
        dummy = TSO.GetAt(tsoIndex, &gc.refFromObj, &gc.refParent, (QueuedObjectInfo **)&iorInfo);
        PREFIX_ASSUME(gc.refFromObj != NULL);

        // Look in the Table of Seen objects whether this IObjRef has been resolved
        int currId;
        currId = TOS.HasID(gc.refFromObj, &gc.resolvedObject);
        _ASSERTE(currId != -1);

        MethodTable *pResolvedMT = gc.resolvedObject->GetMethodTable();
        if (!pResolvedMT->IsTransparentProxy() && 
            m_cbInterface->IsIObjectReferenceType(pResolvedMT))
        {
            bResult = FALSE;
        }
        else
        {
#ifdef _DEBUG
            {
                DefineFullyQualifiedNameForClass();
                LOG((LF_REMOTING, LL_INFO1000, "CompleteIObjRefObject. Found IObjectReference object of type %s already resolved.\n",
                    GetFullyQualifiedNameForClassNestedAware(gc.IObjRef->GetMethodTable())));
            }
#endif

            // Yes, its been resolved. 
            // Fix the object into its parent (unless it requires unboxing, in which case there's another entry in the TSO ready to
            // do that).
            QueuedObjectInfo *pFixupInfo = (QueuedObjectInfo *)iorInfo->GetFixupInfo();
            PREFIX_ASSUME(pFixupInfo != NULL);
            if (pFixupInfo->NeedsUnboxing())
            {
                TSO.SetAt(tsoIndex, gc.resolvedObject, gc.refFromObj, gc.refParent, iorInfo);
                iorInfo->SetHasBeenProcessed();
                bResult = TRUE;
            }
            else
            {
                if (gc.refParent == NULL)
                    m_topObject = gc.resolvedObject;
                else
                {
                    m_newObject = gc.resolvedObject;
                    if (pFixupInfo->NeedsUnboxing())
                        CompleteValueTypeFields(gc.resolvedObject, gc.refParent, pFixupInfo);
                    else
                        Fixup(gc.resolvedObject, gc.refParent, pFixupInfo);
                }
                iorInfo->SetHasBeenProcessed();
                bResult = TRUE;
            }
        }
    }
    else
    {
        MethodTable *pMT = gc.IObjRef->GetMethodTable();
        _ASSERTE(pMT);

        MethodTable *pItf = MscorlibBinder::GetClass(CLASS__IOBJECTREFERENCE);
        MethodDesc *pMeth = GetInterfaceMethodImpl(pMT, pItf, 0);
        MethodDescCallSite method(pMeth, &gc.IObjRef);
   
        // Ensure Streamingcontext type is loaded. Do not delete this line
        MethodTable *pMTStreamingContext;
        pMTStreamingContext = MscorlibBinder::GetClass(CLASS__STREAMING_CONTEXT);
        _ASSERTE(pMTStreamingContext);

        ARG_SLOT arg[2];
        arg[0] = ObjToArgSlot(gc.IObjRef);

        RuntimeMethodHandle::StreamingContextData context = { NULL, GetStreamingContextState() };
#ifdef _WIN64
        static_assert_no_msg(sizeof(context) > sizeof(ARG_SLOT));
        arg[1] = PtrToArgSlot(&context);
#else
        static_assert_no_msg(sizeof(context) <= sizeof(ARG_SLOT));
        arg[1] = *(ARG_SLOT*)(&context);
#endif

        gc.newObj = method.CallWithValueTypes_RetOBJECTREF(&arg[0]);

        INDEBUG(DefineFullyQualifiedNameForClass();)

        _ASSERTE(gc.newObj != NULL);
        MethodTable *pNewMT = gc.newObj->GetMethodTable();
        if (!pNewMT->IsTransparentProxy() && 
            gc.newObj != gc.IObjRef &&
            m_cbInterface->IsIObjectReferenceType(pNewMT))
        {
#ifdef _DEBUG
            LOG((LF_REMOTING, LL_INFO1000,
                "CompleteIObjRefObject. GetRealObject on object of type %s returned another IObjectReference. Adding back to TSO.\n",
                GetFullyQualifiedNameForClassNestedAware(gc.IObjRef->GetMethodTable())));
#endif

            // Put this back into the table
            OBJECTREF dummy;
            dummy = TSO.GetAt(tsoIndex, &gc.refFromObj, &gc.refParent, (QueuedObjectInfo **)&iorInfo);
            TSO.SetAt(tsoIndex, gc.newObj, gc.refFromObj, gc.refParent, iorInfo);
            bResult = FALSE;
        }
        else
        {
#ifdef _DEBUG
            LOG((LF_REMOTING, LL_INFO1000,
                "CompleteIObjRefObject. Called GetRealObject on object of type %s. Fixing it up into its parent.\n",
                GetFullyQualifiedNameForClassNestedAware(gc.IObjRef->GetMethodTable())));
#endif
            // Fix the object into its parent (unless it requires unboxing, in which case there's another entry in the TSO ready to
            // do that).
            QueuedObjectInfo *pFixupInfo = (QueuedObjectInfo *)iorInfo->GetFixupInfo();
            OBJECTREF dummy;
            dummy = TSO.GetAt(tsoIndex, &gc.refFromObj, &gc.refParent, (QueuedObjectInfo **)&iorInfo);
            if (pFixupInfo->NeedsUnboxing())
            {
                TSO.SetAt(tsoIndex, gc.newObj, gc.refFromObj, gc.refParent, iorInfo);
                iorInfo->SetHasBeenProcessed();
                bResult = TRUE;
            }
            else
            {
                if (gc.refParent == NULL)
                    m_topObject = gc.newObj;
                else
                {
                    m_newObject = gc.newObj;
                    Fixup(gc.newObj, gc.refParent, pFixupInfo);
                }

                // Update Table of Seen objects, so that any repeat objects can be updated too
                TOS.UpdateObject(gc.refFromObj, gc.newObj);
                iorInfo->SetHasBeenProcessed();
                bResult = TRUE;
            }
        }
    }

    GCPROTECT_END();
    return bResult;
}

void MakeIDeserializationCallback(OBJECTREF refTarget)
{
    CONTRACTL
    {
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        THROWS;
    }
    CONTRACTL_END;

    struct _gc {
        OBJECTREF   refTarget;
    } gc;
    gc.refTarget = refTarget;

    GCPROTECT_BEGIN(gc);

    MethodTable *pMT = gc.refTarget->GetMethodTable();
    _ASSERTE(pMT);

    MethodTable *pItf = MscorlibBinder::GetClass(CLASS__IDESERIALIZATIONCB);
    MethodDesc *pMeth = GetInterfaceMethodImpl(pMT, pItf, 0);
    PCODE pCode = pMeth->GetSingleCallableAddrOfCode();

    PREPARE_NONVIRTUAL_CALLSITE_USING_CODE(pCode);

    DECLARE_ARGHOLDER_ARRAY(args, 2);

    args[ARGNUM_0]    = OBJECTREF_TO_ARGHOLDER(gc.refTarget);
    args[ARGNUM_1]    = NULL;

    CATCH_HANDLER_FOUND_NOTIFICATION_CALLSITE;
    CALL_MANAGED_METHOD_NORET(args);

    GCPROTECT_END();
}

void ObjectClone::CompleteIDeserializationCallbacks()
{
    CONTRACTL
    {
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        THROWS;
    }
    CONTRACTL_END
    OBJECTREF Dummy1 = NULL, Dummy2 = NULL;
    QueuedObjectInfo *pObjInfo = NULL;

    if (TDC.GetCount() == 0)
        return;
    
    LOG((LF_REMOTING, LL_INFO1000, "CompleteIDeserializationCallbacks. Beginning.\n"));

    OBJECTREF nextObj;
    while ((nextObj = TDC.Dequeue(&Dummy1, &Dummy2, &pObjInfo)) != NULL)
    {
        MakeIDeserializationCallback(nextObj);
    }
}

void ObjectClone::CompleteVtsOnDeserializedCallbacks()
{
    CONTRACTL
    {
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        THROWS;
    }
    CONTRACTL_END;

    OBJECTREF nextObj = NULL, Dummy1 = NULL, Dummy2 = NULL;

    if (VDC.GetCount() == 0)
        return;
    
    LOG((LF_REMOTING, LL_INFO1000, "CompleteVtsOnDeserializedCallbacks. Beginning.\n"));

    GCPROTECT_BEGIN(nextObj);

    while ((nextObj = VDC.Dequeue(&Dummy1, &Dummy2, NULL)) != NULL)
        InvokeVtsCallbacks(nextObj, RemotingVtsInfo::VTS_CALLBACK_ON_DESERIALIZED, m_toDomain);

    GCPROTECT_END();
}

void ObjectClone::CompleteVtsOnSerializedCallbacks()
{
    CONTRACTL
    {
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        THROWS;
    }
    CONTRACTL_END;

    OBJECTREF nextObj = NULL, Dummy1 = NULL, Dummy2 = NULL;

    if (VSC.GetCount() == 0)
        return;
    
    LOG((LF_REMOTING, LL_INFO1000, "CompleteVtsOnSerializedCallbacks. Beginning.\n"));

    GCPROTECT_BEGIN(nextObj);

    while ((nextObj = VSC.Dequeue(&Dummy1, &Dummy2, NULL)) != NULL)
        InvokeVtsCallbacks(nextObj, RemotingVtsInfo::VTS_CALLBACK_ON_SERIALIZED, m_fromDomain);

    GCPROTECT_END();
}

// Does a binary search to find the object with given id, and record of given kind
DWORD ObjectClone::FindObjectInTSO(int objId, SpecialObjects kind)
{
    CONTRACTL
    {
        MODE_COOPERATIVE;
        GC_NOTRIGGER;
        NOTHROW;
    }
    CONTRACTL_END

    DWORD lowIndex = 0;
    DWORD highIndex = TSO.GetCount();
    DWORD midIndex = highIndex / 2;
    DWORD firstMatch;

    if (highIndex == 0)
    {
        _ASSERTE(!"Special Object unexpectedly not found for given object id\n");
        return 0; // throw ?
    }

    SpecialObjectInfo *splInfo = NULL;
    while (true)
    {
        OBJECTREF refParent, refFromObj;
        OBJECTREF dummy;
        dummy = TSO.GetAt(midIndex, &refFromObj, &refParent, (QueuedObjectInfo **)&splInfo);

        if (objId < splInfo->GetObjectId())
        {
            highIndex = midIndex;
        }
        else
        {
            if (objId == splInfo->GetObjectId())
                break;
            lowIndex = midIndex;
        }

        DWORD oldIndex = midIndex;
        midIndex = lowIndex + (highIndex - lowIndex)/2;
        if (oldIndex == midIndex)
        {
            // Binary search failed. See comments below
            goto LinearSearch;
        }
    }

    // Found match at midIndex
    // Find the first record for this obj id
    firstMatch = midIndex;
    while(midIndex != 0)
    {
        midIndex -= 1;
        SpecialObjectInfo *pTemp;
        OBJECTREF refParent, refFromObj;
        OBJECTREF dummy;
        dummy = TSO.GetAt(midIndex, &refFromObj, &refParent, (QueuedObjectInfo **)&pTemp);
        if (pTemp->GetObjectId() != objId)
            break;
        else
            firstMatch = midIndex;
    };

    // Now look for the right kind of record
    do
    {
        OBJECTREF refParent, refFromObj;
        OBJECTREF dummy;
        dummy = TSO.GetAt(firstMatch, &refFromObj, &refParent, (QueuedObjectInfo **)&splInfo);
        
        if (splInfo->GetObjectId() == objId)
        {
            switch(kind)
            {
                case ISerializable:
                    if (splInfo->IsISerializableInstance())
                        return firstMatch;
                    break;
                case IObjectReference:
                    if (splInfo->IsIObjRefInstance())
                        return firstMatch;
                    break;
                case BoxedValueType:
                    if (splInfo->IsBoxedObject())
                        return firstMatch;
                    break;
                default:
                    _ASSERTE(!"Unknown enum value in FindObjectInTSO");
            };
        }

        firstMatch++;
        
    }while(firstMatch < TSO.GetCount());

LinearSearch:
    // If there are multiple objects that are ISer/IObj, and some of them repeat in a certain fashion,
    // then the entries in TSO are not in sorted order. In such a case binary search will fail. Lets do a linear search
    // in such a case for now. This is probably reasonable since the TSO should usually be short and in-order (and presumably
    // cheaper than trying to keep the list in sorted order at all times).
    DWORD currIndex = 0;
    for (; currIndex < TSO.GetCount(); currIndex++)
    {
        OBJECTREF refParent, refFromObj;
        OBJECTREF dummy;
        dummy = TSO.GetAt(currIndex, &refFromObj, &refParent, (QueuedObjectInfo **)&splInfo);

        SpecialObjects foundKind = ISerializable;
        if (splInfo->IsIObjRefInstance())
            foundKind = IObjectReference;
        else if (splInfo->IsBoxedObject())
            foundKind = BoxedValueType;
        else
            _ASSERTE(splInfo->IsISerializableInstance());
        
        if (objId == splInfo->GetObjectId()
        && kind == foundKind)
            return currIndex;
    }


    _ASSERTE(!"Special Object unexpectedly not found for given object id\n");
    return 0; // throw ?
}

// This function is effectively a replica of MethodTable::Box. Its replicated to avoid "GCPROTECT_INTERIOR" that Box uses
// and causes some leak detection asserts to go off. This is a controlled leak situation, where we know we're leaking stuff
// and dont want the asserts.
OBJECTREF ObjectClone::BoxValueTypeInWrongDomain(OBJECTREF refParent, DWORD offset, MethodTable *pValueTypeMT)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(pValueTypeMT->IsValueType());
        PRECONDITION(!pValueTypeMT->ContainsStackPtr());
    }
    CONTRACTL_END;

    OBJECTREF ref = NULL;
    void* pSrc = refParent->GetData() + offset;
    GCPROTECT_BEGININTERIOR(pSrc);
    
    // We must enter the target domain if we are boxing a non-agile type.  This of course has some overhead
    // so we want to avoid it if possible.  GetLoaderModule() == mscorlib && CanBeBlittedByObjectCloner is a
    // conservative first approximation of agile types.
    ENTER_DOMAIN_PTR_PREDICATED(m_fromDomain, ADV_RUNNINGIN,
        !pValueTypeMT->GetLoaderModule()->IsSystem() || pValueTypeMT->GetClass()->CannotBeBlittedByObjectCloner());

    ref = pValueTypeMT->FastBox(&pSrc);

    END_DOMAIN_TRANSITION;
    
    GCPROTECT_END();
    return ref;
}

// Returns whether or not a given type requires VTS callbacks of the specified kind.
BOOL ObjectClone::HasVtsCallbacks(MethodTable *pMT, RemotingVtsInfo::VtsCallbackType eCallbackType)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    while (pMT)
    {
        if (pMT->HasRemotingVtsInfo())
        {
            PTR_RemotingVtsInfo pVtsInfo = pMT->GetRemotingVtsInfo();
            _ASSERTE(pVtsInfo != NULL);

            if (!pVtsInfo->m_pCallbacks[eCallbackType].IsNull())
                return TRUE;
        }
        pMT = pMT->GetParentMethodTable();
    }

    return FALSE;
}

// Calls all of the VTS event methods for a given callback type on the object instance provided (starting at the base class).
void ObjectClone::InvokeVtsCallbacks(OBJECTREF refTarget, RemotingVtsInfo::VtsCallbackType eCallbackType, AppDomain* pDomain)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;

    GCPROTECT_BEGIN(refTarget);

    // Quickly walk the target's type hierarchy and determine the number of methods we'll need to call.
    DWORD cMethods = 0;
    MethodDesc *pLastCallback;
    MethodTable *pMT = refTarget->GetMethodTable();
    while (pMT)
    {
        if (pMT->HasRemotingVtsInfo())
        {
            PTR_RemotingVtsInfo pVtsInfo = pMT->GetRemotingVtsInfo();
            _ASSERTE(pVtsInfo != NULL);
      
            if (!pVtsInfo->m_pCallbacks[eCallbackType].IsNull())
            {
                cMethods++;

#ifdef FEATURE_PREJIT
                // Might have to restore cross module method pointers.
                Module::RestoreMethodDescPointer(&pVtsInfo->m_pCallbacks[eCallbackType]);
#endif

                pLastCallback = pVtsInfo->m_pCallbacks[eCallbackType].GetValue();
            }
        }
        pMT = pMT->GetParentMethodTable();
    }

    // Maybe there's no work to do.
    if (cMethods == 0)
        goto Done;

    // Allocate an array to hold the methods to invoke (we do this because the invocation order is the opposite way round from the
    // way we can easily scan for the methods). We can easily optimize this for the single callback case though.
    MethodDesc **pCallbacks = cMethods == 1 ? &pLastCallback : (MethodDesc**)_alloca(cMethods * sizeof(MethodDesc*));

    if (cMethods > 1)
    {
        // Walk the type hierarchy again, and this time fill in the methods to call in the correct slot of our callback table.
        DWORD dwSlotIndex = cMethods;
        pMT = refTarget->GetMethodTable();
        while (pMT)
        {
            if (pMT->HasRemotingVtsInfo())
            {
                PTR_RemotingVtsInfo pVtsInfo = pMT->GetRemotingVtsInfo();
                _ASSERTE(pVtsInfo != NULL);

                if (!pVtsInfo->m_pCallbacks[eCallbackType].IsNull())
                    pCallbacks[--dwSlotIndex] = pVtsInfo->m_pCallbacks[eCallbackType].GetValue();
            }
            pMT = pMT->GetParentMethodTable();
        }
        _ASSERTE(dwSlotIndex == 0);
    }

    bool fSwitchDomains = pDomain != GetAppDomain();

    ENTER_DOMAIN_PTR(pDomain,ADV_RUNNINGIN);

    // If we're calling back into the from domain then reset the execution context to its original state (this will automatically be
    // popped once we return from this domain again).
    if (pDomain == m_fromDomain && fSwitchDomains)
    {
        Thread *pThread = GetThread();
        if (pThread->IsExposedObjectSet())
        {
            THREADBASEREF refThread = (THREADBASEREF)pThread->GetExposedObjectRaw();
            refThread->SetExecutionContext(m_fromExecutionContext);
        }
    }

    // Remember to adjust this pointer for boxed value types.
    BOOL bIsBoxed = refTarget->GetMethodTable()->IsValueType();

    RuntimeMethodHandle::StreamingContextData sContext = { NULL, GetStreamingContextState() }; 

    // Ensure Streamingcontext type is loaded. Do not delete this line
    MethodTable *pMTStreamingContext;
    pMTStreamingContext = MscorlibBinder::GetClass(CLASS__STREAMING_CONTEXT);
    _ASSERTE(pMTStreamingContext);
    
    // Now go and call each method in order.
    for (DWORD i = 0; i < cMethods; i++)
    {
        MethodDescCallSite callback(pCallbacks[i], &refTarget);

        ARG_SLOT argSlots[2];

            // Nullable<T> does not have any VTS functions 
        _ASSERTE(!Nullable::IsNullableType(refTarget->GetMethodTable()));

        argSlots[0] = (bIsBoxed ? (ARG_SLOT)(SIZE_T)(refTarget->UnBox()) : ObjToArgSlot(refTarget));
#if defined(_TARGET_X86_) || defined(_TARGET_ARM_)
        static_assert_no_msg(sizeof(sContext) == sizeof(ARG_SLOT));
        argSlots[1] = *(ARG_SLOT*)(&sContext);           // StreamingContext is passed by value on x86 and ARM
#elif defined(_WIN64)
        static_assert_no_msg(sizeof(sContext) >  sizeof(ARG_SLOT));
        argSlots[1] = PtrToArgSlot(&sContext);           // StreamingContext is passed by reference on WIN64
#else  // !_TARGET_X86_ && !_WIN64 && !_TARGET_ARM_
        PORTABILITY_ASSERT("ObjectClone::InvokeVtsCallbacks() - NYI on this platform");
#endif // !_TARGET_X86_ && !_WIN64 && !_TARGET_ARM_

        callback.CallWithValueTypes(&argSlots[0]);
    }

    END_DOMAIN_TRANSITION;

Done: ;
    GCPROTECT_END();
}

#endif //  FEATURE_REMOTING
