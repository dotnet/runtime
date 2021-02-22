// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// File: CORDB-VALUE.H
//

#ifndef __MONO_DEBUGGER_CORDB_VALUE_H__
#define __MONO_DEBUGGER_CORDB_VALUE_H__

#include <cordb-type.h>
#include <cordb.h>

union CordbContent
{
    int16_t charValue;
    int8_t  booleanValue;
    int32_t intValue;
    int64_t longValue;
    void*   pointerValue;
};

class CordbValue : public CordbBaseMono, public ICorDebugValue2, public ICorDebugValue3, public ICorDebugGenericValue
{
    CorElementType m_type;
    CordbContent   m_value;
    int            m_size;
    CordbType*     m_pType;

public:
    CordbValue(Connection* conn, CorElementType type, CordbContent value, int size);
    ULONG AddRef(void)
    {
        return (BaseAddRef());
    }
    ULONG Release(void)
    {
        return (BaseRelease());
    }
    const char* GetClassName()
    {
        return "CordbValue";
    }
    ~CordbValue();
    HRESULT GetType(CorElementType* pType);
    HRESULT GetSize(ULONG32* pSize);
    HRESULT GetAddress(CORDB_ADDRESS* pAddress);
    HRESULT
    CreateBreakpoint(ICorDebugValueBreakpoint** ppBreakpoint);
    HRESULT QueryInterface(REFIID riid, void** ppvObject);

    HRESULT GetExactType(ICorDebugType** ppType);
    HRESULT GetSize64(ULONG64* pSize);
    HRESULT GetValue(void* pTo);
    HRESULT SetValue(void* pFrom);
};

class CordbReferenceValue : public CordbBaseMono,
                            public ICorDebugReferenceValue,
                            public ICorDebugValue2,
                            public ICorDebugValue3,
                            public ICorDebugGenericValue
{
    CorElementType m_type;
    int            m_debuggerId;
    CordbClass*    m_pClass;
    CordbType*     m_pCordbType;

public:
    CordbReferenceValue(
        Connection* conn, CorElementType type, int object_id, CordbClass* klass = NULL, CordbType* cordbType = NULL);
    ULONG AddRef(void)
    {
        return (BaseAddRef());
    }
    ULONG Release(void)
    {
        return (BaseRelease());
    }
    const char* GetClassName()
    {
        return "CordbReferenceValue";
    }
    ~CordbReferenceValue();
    HRESULT GetType(CorElementType* pType);
    HRESULT GetSize(ULONG32* pSize);
    HRESULT GetAddress(CORDB_ADDRESS* pAddress);
    HRESULT
    CreateBreakpoint(ICorDebugValueBreakpoint** ppBreakpoint);
    HRESULT QueryInterface(REFIID riid, void** ppvObject);

    HRESULT GetExactType(ICorDebugType** ppType);
    HRESULT GetSize64(ULONG64* pSize);
    HRESULT GetValue(void* pTo);
    HRESULT SetValue(void* pFrom);
    HRESULT IsNull(BOOL* pbNull);
    HRESULT GetValue(CORDB_ADDRESS* pValue);
    HRESULT SetValue(CORDB_ADDRESS value);
    HRESULT Dereference(ICorDebugValue** ppValue);
    HRESULT DereferenceStrong(ICorDebugValue** ppValue);
};

class CordbObjectValue : public CordbBaseMono,
                         public ICorDebugObjectValue,
                         public ICorDebugObjectValue2,
                         public ICorDebugGenericValue,
                         public ICorDebugStringValue,
                         public ICorDebugValue2,
                         public ICorDebugValue3,
                         public ICorDebugHeapValue2,
                         public ICorDebugHeapValue3,
                         public ICorDebugExceptionObjectValue,
                         public ICorDebugComObjectValue,
                         public ICorDebugDelegateObjectValue
{
    CorElementType m_type;
    int            m_debuggerId;
    CordbClass*    m_pClass;
    CordbType*     m_pCordbType;

public:
    CordbObjectValue(Connection* conn, CorElementType type, int object_id, CordbClass* klass);
    ULONG AddRef(void)
    {
        return (BaseAddRef());
    }
    ULONG Release(void)
    {
        return (BaseRelease());
    }
    const char* GetClassName()
    {
        return "CordbObjectValue";
    }
    ~CordbObjectValue();
    HRESULT        GetClass(ICorDebugClass** ppClass);
    HRESULT        GetFieldValue(ICorDebugClass* pClass, mdFieldDef fieldDef, ICorDebugValue** ppValue);
    static HRESULT CreateCordbValue(Connection* conn, MdbgProtBuffer* pReply, ICorDebugValue** ppValue);
    static int     GetTypeSize(int type);
    HRESULT        GetVirtualMethod(mdMemberRef memberRef, ICorDebugFunction** ppFunction);
    HRESULT        GetContext(ICorDebugContext** ppContext);
    HRESULT        IsValueClass(BOOL* pbIsValueClass);
    HRESULT        GetManagedCopy(IUnknown** ppObject);
    HRESULT        SetFromManagedCopy(IUnknown* pObject);
    HRESULT        IsValid(BOOL* pbValid);
    HRESULT
    CreateRelocBreakpoint(ICorDebugValueBreakpoint** ppBreakpoint);
    HRESULT GetType(CorElementType* pType);
    HRESULT GetSize(ULONG32* pSize);
    HRESULT GetAddress(CORDB_ADDRESS* pAddress);
    HRESULT
    CreateBreakpoint(ICorDebugValueBreakpoint** ppBreakpoint);
    HRESULT QueryInterface(REFIID riid, void** ppvObject);

    HRESULT GetExactType(ICorDebugType** ppType);
    HRESULT GetSize64(ULONG64* pSize);
    HRESULT GetValue(void* pTo);
    HRESULT SetValue(void* pFrom);
    HRESULT
    GetVirtualMethodAndType(mdMemberRef memberRef, ICorDebugFunction** ppFunction, ICorDebugType** ppType);
    HRESULT GetLength(ULONG32* pcchString);
    HRESULT GetString(ULONG32 cchString, ULONG32* pcchString, WCHAR szString[]);
    HRESULT CreateHandle(CorDebugHandleType type, ICorDebugHandleValue** ppHandle);
    HRESULT GetThreadOwningMonitorLock(ICorDebugThread** ppThread, DWORD* pAcquisitionCount);
    HRESULT
    GetMonitorEventWaitList(ICorDebugThreadEnum** ppThreadEnum);
    HRESULT EnumerateExceptionCallStack(ICorDebugExceptionObjectCallStackEnum** ppCallStackEnum);
    HRESULT GetCachedInterfaceTypes(BOOL bIInspectableOnly, ICorDebugTypeEnum** ppInterfacesEnum);
    HRESULT GetCachedInterfacePointers(BOOL           bIInspectableOnly,
                                       ULONG32        celt,
                                       ULONG32*       pcEltFetched,
                                       CORDB_ADDRESS* ptrs);
    HRESULT GetTarget(ICorDebugReferenceValue** ppObject);
    HRESULT GetFunction(ICorDebugFunction** ppFunction);
};

class CordbArrayValue : public CordbBaseMono,
                        public ICorDebugObjectValue,
                        public ICorDebugObjectValue2,
                        public ICorDebugGenericValue,
                        public ICorDebugStringValue,
                        public ICorDebugValue2,
                        public ICorDebugValue3,
                        public ICorDebugHeapValue2,
                        public ICorDebugHeapValue3,
                        public ICorDebugExceptionObjectValue,
                        public ICorDebugComObjectValue,
                        public ICorDebugDelegateObjectValue,
                        public ICorDebugArrayValue
{
    CordbType*  m_pCordbType;
    int         m_debuggerId;
    CordbClass* m_pClass;
    int         m_nCount;

public:
    CordbArrayValue(Connection* conn, CordbType* type, int object_id, CordbClass* klass);
    ULONG AddRef(void)
    {
        return (BaseAddRef());
    }
    ULONG Release(void)
    {
        return (BaseRelease());
    }
    const char* GetClassName()
    {
        return "CordbArrayValue";
    }
    ~CordbArrayValue();
    HRESULT GetClass(ICorDebugClass** ppClass);
    HRESULT GetFieldValue(ICorDebugClass* pClass, mdFieldDef fieldDef, ICorDebugValue** ppValue);
    HRESULT GetVirtualMethod(mdMemberRef memberRef, ICorDebugFunction** ppFunction);
    HRESULT GetContext(ICorDebugContext** ppContext);
    HRESULT IsValueClass(BOOL* pbIsValueClass);
    HRESULT GetManagedCopy(IUnknown** ppObject);
    HRESULT SetFromManagedCopy(IUnknown* pObject);
    HRESULT GetType(CorElementType* pType);
    HRESULT GetSize(ULONG32* pSize);
    HRESULT GetAddress(CORDB_ADDRESS* pAddress);
    HRESULT
    CreateBreakpoint(ICorDebugValueBreakpoint** ppBreakpoint);
    HRESULT QueryInterface(REFIID riid, void** ppvObject);

    HRESULT
    GetVirtualMethodAndType(mdMemberRef memberRef, ICorDebugFunction** ppFunction, ICorDebugType** ppType);
    HRESULT GetValue(void* pTo);
    HRESULT SetValue(void* pFrom);
    HRESULT GetLength(ULONG32* pcchString);
    HRESULT GetString(ULONG32 cchString, ULONG32* pcchString, WCHAR szString[]);
    HRESULT IsValid(BOOL* pbValid);
    HRESULT
    CreateRelocBreakpoint(ICorDebugValueBreakpoint** ppBreakpoint);
    HRESULT GetExactType(ICorDebugType** ppType);
    HRESULT GetSize64(ULONG64* pSize);
    HRESULT CreateHandle(CorDebugHandleType type, ICorDebugHandleValue** ppHandle);
    HRESULT GetThreadOwningMonitorLock(ICorDebugThread** ppThread, DWORD* pAcquisitionCount);
    HRESULT
    GetMonitorEventWaitList(ICorDebugThreadEnum** ppThreadEnum);
    HRESULT EnumerateExceptionCallStack(ICorDebugExceptionObjectCallStackEnum** ppCallStackEnum);
    HRESULT GetCachedInterfaceTypes(BOOL bIInspectableOnly, ICorDebugTypeEnum** ppInterfacesEnum);
    HRESULT GetCachedInterfacePointers(BOOL           bIInspectableOnly,
                                       ULONG32        celt,
                                       ULONG32*       pcEltFetched,
                                       CORDB_ADDRESS* ptrs);
    HRESULT GetTarget(ICorDebugReferenceValue** ppObject);
    HRESULT GetFunction(ICorDebugFunction** ppFunction);

    HRESULT GetElementType(CorElementType* pType);
    HRESULT GetRank(ULONG32* pnRank);
    HRESULT GetCount(ULONG32* pnCount);
    HRESULT GetDimensions(ULONG32 cdim, ULONG32 dims[]);
    HRESULT HasBaseIndicies(BOOL* pbHasBaseIndicies);
    HRESULT GetBaseIndicies(ULONG32 cdim, ULONG32 indicies[]);
    HRESULT GetElement(ULONG32 cdim, ULONG32 indices[], ICorDebugValue** ppValue);
    HRESULT GetElementAtPosition(ULONG32 nPosition, ICorDebugValue** ppValue);
};
#endif
