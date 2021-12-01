// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


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

struct NATIVEOVERLAPPED_AND_HANDLE
{
    OVERLAPPED m_overlapped;
    OBJECTHANDLE m_handle;
};

// This should match the managed OverlappedData object.
// If you make any change here, you need to change the managed part OverlappedData.
class OverlappedDataObject : public Object
{
public:
    // OverlappedDataObject is very special.  An async pin handle keeps it alive.
    // During GC, we also make sure
    // 1. m_userObject itself does not move if m_userObject is not array
    // 2. Every object pointed by m_userObject does not move if m_userObject is array
    OBJECTREF m_asyncResult;
    OBJECTREF m_callback;
    OBJECTREF m_overlapped;
    OBJECTREF m_userObject;
    LPOVERLAPPED m_pNativeOverlapped;
    ULONG_PTR m_eventHandle;
    int m_offsetLow;
    int m_offsetHigh;

#ifndef DACCESS_COMPILE
    static OverlappedDataObject* GetOverlapped(LPOVERLAPPED nativeOverlapped)
    {
        LIMITED_METHOD_CONTRACT;

        _ASSERTE (nativeOverlapped != NULL);
        return (OverlappedDataObject*)OBJECTREFToObject(ObjectFromHandle(((NATIVEOVERLAPPED_AND_HANDLE*)nativeOverlapped)->m_handle));
    }

    // Return the raw OverlappedDataObject* without going into cooperative mode for tracing
    static OverlappedDataObject* GetOverlappedForTracing(LPOVERLAPPED nativeOverlapped)
    {
        LIMITED_METHOD_CONTRACT;

        _ASSERTE(nativeOverlapped != NULL);
        return *(OverlappedDataObject**)(((NATIVEOVERLAPPED_AND_HANDLE*)nativeOverlapped)->m_handle);
    }
#endif // DACCESS_COMPILE
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
FCDECL1(LPOVERLAPPED, AllocateNativeOverlapped, OverlappedDataObject* overlapped);
FCDECL1(void, FreeNativeOverlapped, LPOVERLAPPED lpOverlapped);
FCDECL1(OverlappedDataObject*, GetOverlappedFromNative, LPOVERLAPPED lpOverlapped);

#endif
