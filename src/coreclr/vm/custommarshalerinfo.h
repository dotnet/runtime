// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// File: CustomMarshalerInfo.h
//

//
// Custom marshaler information used when marshaling
// a parameter with a custom marshaler.
//


#ifndef _CUSTOMMARSHALERINFO_H_
#define _CUSTOMMARSHALERINFO_H_


#include "vars.hpp"
#include "slist.h"


// This enumeration is used to retrieve a method desc from CustomMarshalerInfo::GetCustomMarshalerMD().
enum EnumCustomMarshalerMethods
{
    CustomMarshalerMethods_MarshalNativeToManaged = 0,
    CustomMarshalerMethods_MarshalManagedToNative,
    CustomMarshalerMethods_CleanUpNativeData,
    CustomMarshalerMethods_CleanUpManagedData,
    CustomMarshalerMethods_GetNativeDataSize,
    CustomMarshalerMethods_GetInstance,
    CustomMarshalerMethods_LastMember
};


class CustomMarshalerInfo final
{
public:
    // Constructor and destructor.
    CustomMarshalerInfo(LoaderAllocator* pLoaderAllocator, TypeHandle hndCustomMarshalerType, TypeHandle hndManagedType, LPCUTF8 strCookie, DWORD cCookieStrBytes);
    ~CustomMarshalerInfo();

    // CustomMarshalerInfo's are always allocated on the loader heap so we need to redefine
    // the new and delete operators to ensure this.
    void* operator      new(size_t size, LoaderHeap* pHeap);
    void  operator      delete(void* pMem);

    // Helpers used to invoke the different methods in the ICustomMarshaler interface.
    OBJECTREF           InvokeMarshalNativeToManagedMeth(void* pNative);
    void*               InvokeMarshalManagedToNativeMeth(OBJECTREF MngObj);
    void                InvokeCleanUpManagedMeth(OBJECTREF MngObj);

    // Accessors.
    TypeHandle GetManagedType()
    {
        LIMITED_METHOD_CONTRACT;
        return m_hndManagedType;
    }

    OBJECTREF GetCustomMarshaler()
    {
        LIMITED_METHOD_CONTRACT;
        return m_pLoaderAllocator->GetHandleValue(m_hndCustomMarshaler);
    }

    // Helper function to retrieve a custom marshaler method desc.
    static MethodDesc*  GetCustomMarshalerMD(EnumCustomMarshalerMethods Method, TypeHandle hndCustomMarshalertype);

    // Link used to contain this CM info in a linked list.
    SLink               m_Link;

private:
    TypeHandle          m_hndManagedType;
    LoaderAllocator*    m_pLoaderAllocator;
    LOADERHANDLE        m_hndCustomMarshaler;
    MethodDesc*         m_pMarshalNativeToManagedMD;
    MethodDesc*         m_pMarshalManagedToNativeMD;
    MethodDesc*         m_pCleanUpNativeDataMD;
    MethodDesc*         m_pCleanUpManagedDataMD;
};


typedef SList<CustomMarshalerInfo, true> CMINFOLIST;

class Assembly;

class EECMInfoHashtableKey
{
public:
    EECMInfoHashtableKey(DWORD cMarshalerTypeNameBytes, LPCSTR strMarshalerTypeName, DWORD cCookieStrBytes, LPCSTR strCookie, Instantiation instantiation, Assembly* invokingAssembly)
    : m_cMarshalerTypeNameBytes(cMarshalerTypeNameBytes)
    , m_strMarshalerTypeName(strMarshalerTypeName)
    , m_cCookieStrBytes(cCookieStrBytes)
    , m_strCookie(strCookie)
    , m_Instantiation(instantiation)
    , m_invokingAssembly(invokingAssembly)
    {
        LIMITED_METHOD_CONTRACT;
    }

    DWORD GetMarshalerTypeNameByteCount() const
    {
        LIMITED_METHOD_CONTRACT;
        return m_cMarshalerTypeNameBytes;
    }
    LPCSTR GetMarshalerTypeName() const
    {
        LIMITED_METHOD_CONTRACT;
        return m_strMarshalerTypeName;
    }
    LPCSTR GetCookieString() const
    {
        LIMITED_METHOD_CONTRACT;
        return m_strCookie;
    }
    ULONG GetCookieStringByteCount() const
    {
        LIMITED_METHOD_CONTRACT;
        return m_cCookieStrBytes;
    }
    Instantiation GetMarshalerInstantiation() const
    {
        LIMITED_METHOD_CONTRACT;
        return m_Instantiation;
    }
    Assembly* GetInvokingAssembly() const
    {
        LIMITED_METHOD_CONTRACT;
        return m_invokingAssembly;
    }

    DWORD           m_cMarshalerTypeNameBytes;
    LPCSTR          m_strMarshalerTypeName;
    DWORD           m_cCookieStrBytes;
    LPCSTR          m_strCookie;
    Instantiation   m_Instantiation;
    Assembly*       m_invokingAssembly;
};


class EECMInfoHashtableHelper
{
public:
    static EEHashEntry_t*  AllocateEntry(EECMInfoHashtableKey* pKey, BOOL bDeepCopy, AllocationHeap Heap);
    static void            DeleteEntry(EEHashEntry_t* pEntry, AllocationHeap Heap);
    static BOOL            CompareKeys(EEHashEntry_t* pEntry, EECMInfoHashtableKey* pKey);
    static DWORD           Hash(EECMInfoHashtableKey* pKey);
};


typedef EEHashTable<EECMInfoHashtableKey*, EECMInfoHashtableHelper, TRUE> EECMInfoHashTable;

extern "C" void QCALLTYPE CustomMarshaler_GetMarshalerObject(CustomMarshalerInfo* pCMHelper, QCall::ObjectHandleOnStack retObject);

#endif // _CUSTOMMARSHALERINFO_H_
