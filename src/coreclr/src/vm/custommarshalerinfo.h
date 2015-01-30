//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
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


class CustomMarshalerInfo
{
public:
    // Constructor and destructor.
    CustomMarshalerInfo(BaseDomain* pDomain, TypeHandle hndCustomMarshalerType, TypeHandle hndManagedType, LPCUTF8 strCookie, DWORD cCookieStrBytes);
    ~CustomMarshalerInfo();

    // CustomMarshalerInfo's are always allocated on the loader heap so we need to redefine
    // the new and delete operators to ensure this.
    void* operator      new(size_t size, LoaderHeap* pHeap);
    void  operator      delete(void* pMem);

    // Helpers used to invoke the different methods in the ICustomMarshaler interface.
    OBJECTREF           InvokeMarshalNativeToManagedMeth(void* pNative);
    void*               InvokeMarshalManagedToNativeMeth(OBJECTREF MngObj);
    void                InvokeCleanUpNativeMeth(void* pNative);
    void                InvokeCleanUpManagedMeth(OBJECTREF MngObj);

    // Accessors.
    int GetNativeSize()
    {
        LIMITED_METHOD_CONTRACT;
        return m_NativeSize;
    }

    int GetManagedSize()
    {
        WRAPPER_NO_CONTRACT;
        return m_hndManagedType.GetSize();
    }

    TypeHandle GetManagedType()
    {
        LIMITED_METHOD_CONTRACT;
        return m_hndManagedType;
    }

    BOOL IsDataByValue()
    {
        LIMITED_METHOD_CONTRACT;
        return m_bDataIsByValue;
    }

    OBJECTHANDLE GetCustomMarshaler()
    {
        LIMITED_METHOD_CONTRACT;
        return m_hndCustomMarshaler;
    }

    TypeHandle GetCustomMarshalerType()
    {
        CONTRACTL
        {
            NOTHROW;
            GC_NOTRIGGER;
            SO_TOLERANT;
            MODE_COOPERATIVE;
        }
        CONTRACTL_END;
        return ObjectFromHandle(m_hndCustomMarshaler)->GetTypeHandle();
    }

    // Helper function to retrieve a custom marshaler method desc.
    static MethodDesc*  GetCustomMarshalerMD(EnumCustomMarshalerMethods Method, TypeHandle hndCustomMarshalertype); 

    // Link used to contain this CM info in a linked list.
    SLink               m_Link;

private:
    int                 m_NativeSize;
    TypeHandle          m_hndManagedType;
    OBJECTHANDLE        m_hndCustomMarshaler;
    MethodDesc*         m_pMarshalNativeToManagedMD;
    MethodDesc*         m_pMarshalManagedToNativeMD;
    MethodDesc*         m_pCleanUpNativeDataMD;
    MethodDesc*         m_pCleanUpManagedDataMD;
    BOOL                m_bDataIsByValue;
};


typedef SList<CustomMarshalerInfo, true> CMINFOLIST;


class EECMHelperHashtableKey
{
public:
    EECMHelperHashtableKey(DWORD cMarshalerTypeNameBytes, LPCSTR strMarshalerTypeName, DWORD cCookieStrBytes, LPCSTR strCookie, Instantiation instantiation, BOOL bSharedHelper) 
    : m_cMarshalerTypeNameBytes(cMarshalerTypeNameBytes)
    , m_strMarshalerTypeName(strMarshalerTypeName)
    , m_cCookieStrBytes(cCookieStrBytes)
    , m_strCookie(strCookie)
    , m_Instantiation(instantiation)
    , m_bSharedHelper(bSharedHelper)
    {
        LIMITED_METHOD_CONTRACT;
    }

    inline DWORD GetMarshalerTypeNameByteCount() const
    {
        LIMITED_METHOD_CONTRACT;
        return m_cMarshalerTypeNameBytes;
    }
    inline LPCSTR GetMarshalerTypeName() const
    {
        LIMITED_METHOD_CONTRACT;
        return m_strMarshalerTypeName;
    }
    inline LPCSTR GetCookieString() const
    {
        LIMITED_METHOD_CONTRACT;
        return m_strCookie;
    }
    inline ULONG GetCookieStringByteCount() const
    {
        LIMITED_METHOD_CONTRACT;
        return m_cCookieStrBytes;
    }
    inline Instantiation GetMarshalerInstantiation() const
    {
        LIMITED_METHOD_CONTRACT;
        return m_Instantiation;
    }
    inline BOOL IsSharedHelper() const
    {
        LIMITED_METHOD_CONTRACT;
        return m_bSharedHelper;
    }


    DWORD           m_cMarshalerTypeNameBytes;
    LPCSTR          m_strMarshalerTypeName;
    DWORD           m_cCookieStrBytes;
    LPCSTR          m_strCookie;
    Instantiation   m_Instantiation;
    BOOL            m_bSharedHelper;
};


class EECMHelperHashtableHelper
{
public:
    static EEHashEntry_t*  AllocateEntry(EECMHelperHashtableKey* pKey, BOOL bDeepCopy, AllocationHeap Heap);
    static void            DeleteEntry(EEHashEntry_t* pEntry, AllocationHeap Heap);
    static BOOL            CompareKeys(EEHashEntry_t* pEntry, EECMHelperHashtableKey* pKey);
    static DWORD           Hash(EECMHelperHashtableKey* pKey);
};


typedef EEHashTable<EECMHelperHashtableKey*, EECMHelperHashtableHelper, TRUE> EECMHelperHashTable;


class CustomMarshalerHelper
{
public:
    // Helpers used to invoke the different methods in the ICustomMarshaler interface.
    OBJECTREF           InvokeMarshalNativeToManagedMeth(void* pNative);
    void*               InvokeMarshalManagedToNativeMeth(OBJECTREF MngObj);
    void                InvokeCleanUpNativeMeth(void* pNative);
    void                InvokeCleanUpManagedMeth(OBJECTREF MngObj);

    // Accessors.
    int GetNativeSize()
    {
        WRAPPER_NO_CONTRACT;
        return GetCustomMarshalerInfo()->GetNativeSize();
    }
    
    int GetManagedSize()
    {
        WRAPPER_NO_CONTRACT;
        return GetCustomMarshalerInfo()->GetManagedSize();
    }
    
    TypeHandle GetManagedType()
    {
        WRAPPER_NO_CONTRACT;
        return GetCustomMarshalerInfo()->GetManagedType();
    }
    
    BOOL IsDataByValue()
    {
        WRAPPER_NO_CONTRACT;
        return GetCustomMarshalerInfo()->IsDataByValue();
    }

    // Helper function to retrieve the custom marshaler object.
    virtual CustomMarshalerInfo* GetCustomMarshalerInfo() = 0;

protected:
    ~CustomMarshalerHelper( void )
    {
        LIMITED_METHOD_CONTRACT;
    }
};


class NonSharedCustomMarshalerHelper : public CustomMarshalerHelper
{
public:
    // Constructor.
    NonSharedCustomMarshalerHelper(CustomMarshalerInfo* pCMInfo) : m_pCMInfo(pCMInfo)
    {
        WRAPPER_NO_CONTRACT;
    }

    // CustomMarshalerHelpers's are always allocated on the loader heap so we need to redefine
    // the new and delete operators to ensure this.
    void *operator new(size_t size, LoaderHeap *pHeap);
    void operator delete(void* pMem);

protected:
    // Helper function to retrieve the custom marshaler object.
    virtual CustomMarshalerInfo* GetCustomMarshalerInfo()
    {
        LIMITED_METHOD_CONTRACT;
        return m_pCMInfo;
    }

private:
    CustomMarshalerInfo* m_pCMInfo;
};


class SharedCustomMarshalerHelper : public CustomMarshalerHelper
{
public:
    // Constructor.
    SharedCustomMarshalerHelper(Assembly* pAssembly, TypeHandle hndManagedType, LPCUTF8 strMarshalerTypeName, DWORD cMarshalerTypeNameBytes, LPCUTF8 strCookie, DWORD cCookieStrBytes);

    // CustomMarshalerHelpers's are always allocated on the loader heap so we need to redefine
    // the new and delete operators to ensure this.
    void* operator new(size_t size, LoaderHeap* pHeap);
    void  operator delete(void* pMem);

    // Accessors.
    inline Assembly* GetAssembly()
    {
        LIMITED_METHOD_CONTRACT;
        return m_pAssembly;
    }

    inline TypeHandle GetManagedType()
    {
        LIMITED_METHOD_CONTRACT;
        return m_hndManagedType;
    }

    inline DWORD GetMarshalerTypeNameByteCount()
    {
        LIMITED_METHOD_CONTRACT;
        return m_cMarshalerTypeNameBytes;
    }

    inline LPCSTR GetMarshalerTypeName()
    {
        LIMITED_METHOD_CONTRACT;
        return m_strMarshalerTypeName;
    }

    inline LPCSTR GetCookieString()
    {
        LIMITED_METHOD_CONTRACT;
        return m_strCookie;
    }

    inline ULONG GetCookieStringByteCount()
    {
        LIMITED_METHOD_CONTRACT;
        return m_cCookieStrBytes;
    }

protected:
    // Helper function to retrieve the custom marshaler object.
    virtual CustomMarshalerInfo* GetCustomMarshalerInfo();

private:
    Assembly*       m_pAssembly;
    TypeHandle      m_hndManagedType;
    DWORD           m_cMarshalerTypeNameBytes;
    LPCUTF8         m_strMarshalerTypeName;
    DWORD           m_cCookieStrBytes;
    LPCUTF8         m_strCookie;
};


#endif // _CUSTOMMARSHALERINFO_H_
