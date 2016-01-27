// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


/*============================================================
**
** Header: COMNativeOverlapped.h
**
** Purpose: Native methods for allocating and freeing NativeOverlapped
**

** 
===========================================================*/

#ifndef _OVERLAPPED_H
#define _OVERLAPPED_H

// This should match the managed Overlapped object.
// If you make any change here, you need to change the managed part Overlapped.
class OverlappedDataObject : public Object
{
public:
    ASYNCRESULTREF m_asyncResult;
    OBJECTREF m_iocb;
    OBJECTREF m_iocbHelper;
    OBJECTREF m_overlapped;
    OBJECTREF m_userObject;

    //
    // NOTE!  WCF directly accesses m_pinSelf from managed code, using a hard-coded negative
    // offset from the Internal member, below.  They need this so they can modify the
    // contents of m_userObject; after such modification, they need to update this handle
    // to be in the correct GC generation.
    //
    // If you need to add or remove fields between this one and Internal, be sure that
    // you also fix the hard-coded offsets in ndp\cdf\src\WCF\ServiceModel\System\ServiceModel\Channels\OverlappedContext.cs.
    //
    OBJECTHANDLE m_pinSelf;

    // OverlappedDataObject is very special.  An async pin handle keeps it alive.
    // During GC, we also make sure
    // 1. m_userObject itself does not move if m_userObject is not array
    // 2. Every object pointed by m_userObject does not move if m_userObject is array
    // We do not want to pin m_userObject if it is array.  But m_userObject may be updated
    // during relocation phase before OverlappedDataObject is doing relocation.
    // m_userObjectInternal is used to track the location of the m_userObject before it is updated.
    void *m_userObjectInternal;
    DWORD m_AppDomainId;
    unsigned char m_isArray;
    unsigned char m_toBeCleaned;

    ULONG_PTR  Internal;
    ULONG_PTR  InternalHigh;
    int     OffsetLow;
    int     OffsetHigh;
    ULONG_PTR  EventHandle;

    static OverlappedDataObject* GetOverlapped (LPOVERLAPPED nativeOverlapped)
    {
        LIMITED_METHOD_CONTRACT;
        STATIC_CONTRACT_SO_TOLERANT;
        
        _ASSERTE (nativeOverlapped != NULL);
        _ASSERTE (GCHeap::GetGCHeap()->IsHeapPointer((BYTE *) nativeOverlapped));
        
        return (OverlappedDataObject*)((BYTE*)nativeOverlapped - offsetof(OverlappedDataObject, Internal));
    }

    DWORD GetAppDomainId()
    {
        return m_AppDomainId;
    }

    void HandleAsyncPinHandle();

    void FreeAsyncPinHandles();

    BOOL HasCompleted()
    {
        LIMITED_METHOD_CONTRACT;
#ifndef FEATURE_PAL        
        return HasOverlappedIoCompleted((LPOVERLAPPED) &Internal);
#else // !FEATURE_PAL
        return FALSE;
#endif // !FEATURE_PAL
    }

private:
    static LONG s_CleanupRequestCount;
    static BOOL s_CleanupInProgress;
    static BOOL s_GCDetectsCleanup;
    static BOOL s_CleanupFreeHandle;

public:
    static void RequestCleanup()
    {
        WRAPPER_NO_CONTRACT;

        FastInterlockIncrement(&s_CleanupRequestCount);
        if (!s_CleanupInProgress)
        {
            StartCleanup();
        }
    }
    static void StartCleanup();

    static void FinishCleanup(bool wasDrained);

    static void MarkCleanupNeededFromGC()
    {
        LIMITED_METHOD_CONTRACT;
        s_GCDetectsCleanup = TRUE;
    }

    static BOOL CleanupNeededFromGC()
    {
        return s_GCDetectsCleanup;
    }

    static void RequestCleanupFromGC()
    {
        WRAPPER_NO_CONTRACT;

        if (s_GCDetectsCleanup)
        {
            s_GCDetectsCleanup = FALSE;
            RequestCleanup();
        }
    }
};

#ifdef USE_CHECKED_OBJECTREFS

typedef REF<OverlappedDataObject> OVERLAPPEDDATAREF;
#define ObjectToOVERLAPPEDDATAREF(obj)     (OVERLAPPEDDATAREF(obj))
#define OVERLAPPEDDATAREFToObject(objref)  (OBJECTREFToObject (objref))

#else

typedef OverlappedDataObject* OVERLAPPEDDATAREF;
#define ObjectToOVERLAPPEDDATAREF(obj)    ((OverlappedDataObject*) (obj))
#define OVERLAPPEDDATAREFToObject(objref) ((OverlappedDataObject*) (objref))

#endif

FCDECL3(void, CheckVMForIOPacket, LPOVERLAPPED* lpOverlapped, DWORD* errorCode, DWORD* numBytes);
FCDECL1(void*, AllocateNativeOverlapped, OverlappedDataObject* overlapped);
FCDECL1(void, FreeNativeOverlapped, LPOVERLAPPED lpOverlapped);
FCDECL1(OverlappedDataObject*, GetOverlappedFromNative, LPOVERLAPPED lpOverlapped);

void InitializePinHandleTable();
void AddMTForPinHandle(OBJECTREF obj);
void BashMTForPinnedObject(OBJECTREF obj);

#endif
