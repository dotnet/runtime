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
    CORDB_ADDRESS   pointerValue;
};

class CordbValue : public CordbBaseMono, public ICorDebugValue2, public ICorDebugValue3, public ICorDebugGenericValue
{
    CorElementType m_type;
    CordbContent   m_value;
    int            m_size;
    CordbType*     m_pType;

public:
    CordbValue(Connection* conn, CorElementType type, CordbContent value, int size);
    ULONG STDMETHODCALLTYPE AddRef(void)
    {
        return (BaseAddRef());
    }
    ULONG STDMETHODCALLTYPE Release(void)
    {
        return (BaseRelease());
    }
    const char* GetClassName()
    {
        return "CordbValue";
    }
    ~CordbValue();
    HRESULT STDMETHODCALLTYPE GetType(CorElementType* pType);
    HRESULT STDMETHODCALLTYPE GetSize(ULONG32* pSize);
    HRESULT STDMETHODCALLTYPE GetAddress(CORDB_ADDRESS* pAddress);
    HRESULT STDMETHODCALLTYPE CreateBreakpoint(ICorDebugValueBreakpoint** ppBreakpoint);
    HRESULT STDMETHODCALLTYPE QueryInterface(REFIID riid, void** ppvObject);

    HRESULT STDMETHODCALLTYPE GetExactType(ICorDebugType** ppType);
    HRESULT STDMETHODCALLTYPE GetSize64(ULONG64* pSize);
    HRESULT STDMETHODCALLTYPE GetValue(void* pTo);
    HRESULT STDMETHODCALLTYPE SetValue(void* pFrom);
    CordbContent* GetValue() {return &m_value;}
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
    CORDB_ADDRESS  m_pAddress;
public:
    CordbReferenceValue(Connection* conn, CorElementType type, int object_id, CordbClass* klass = NULL, CordbType* cordbType = NULL, CORDB_ADDRESS cordbAddress = NULL);
    ULONG STDMETHODCALLTYPE AddRef(void)
    {
        return (BaseAddRef());
    }
    ULONG STDMETHODCALLTYPE Release(void)
    {
        return (BaseRelease());
    }
    const char* GetClassName()
    {
        return "CordbReferenceValue";
    }
    ~CordbReferenceValue();
    HRESULT STDMETHODCALLTYPE GetType(CorElementType* pType);
    HRESULT STDMETHODCALLTYPE GetSize(ULONG32* pSize);
    HRESULT STDMETHODCALLTYPE GetAddress(CORDB_ADDRESS* pAddress);
    HRESULT STDMETHODCALLTYPE CreateBreakpoint(ICorDebugValueBreakpoint** ppBreakpoint);
    HRESULT STDMETHODCALLTYPE QueryInterface(REFIID riid, void** ppvObject);

    HRESULT STDMETHODCALLTYPE GetExactType(ICorDebugType** ppType);
    HRESULT STDMETHODCALLTYPE GetSize64(ULONG64* pSize);
    HRESULT STDMETHODCALLTYPE GetValue(void* pTo);
    HRESULT STDMETHODCALLTYPE SetValue(void* pFrom);
    HRESULT STDMETHODCALLTYPE IsNull(BOOL* pbNull);
    HRESULT STDMETHODCALLTYPE GetValue(CORDB_ADDRESS* pValue);
    HRESULT STDMETHODCALLTYPE SetValue(CORDB_ADDRESS value);
    HRESULT STDMETHODCALLTYPE Dereference(ICorDebugValue** ppValue);
    HRESULT STDMETHODCALLTYPE DereferenceStrong(ICorDebugValue** ppValue);
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
    ULONG STDMETHODCALLTYPE AddRef(void)
    {
        return (BaseAddRef());
    }
    ULONG STDMETHODCALLTYPE Release(void)
    {
        return (BaseRelease());
    }
    const char* GetClassName()
    {
        return "CordbObjectValue";
    }
    ~CordbObjectValue();
    HRESULT STDMETHODCALLTYPE        GetClass(ICorDebugClass** ppClass);
    HRESULT STDMETHODCALLTYPE        GetFieldValue(ICorDebugClass* pClass, mdFieldDef fieldDef, ICorDebugValue** ppValue);
    static HRESULT CreateCordbValue(Connection* conn, MdbgProtBuffer* pReply, ICorDebugValue** ppValue);
    static int GetTypeSize(int type);
    HRESULT STDMETHODCALLTYPE        GetVirtualMethod(mdMemberRef memberRef, ICorDebugFunction** ppFunction);
    HRESULT STDMETHODCALLTYPE        GetContext(ICorDebugContext** ppContext);
    HRESULT STDMETHODCALLTYPE        IsValueClass(BOOL* pbIsValueClass);
    HRESULT STDMETHODCALLTYPE        GetManagedCopy(IUnknown** ppObject);
    HRESULT STDMETHODCALLTYPE        SetFromManagedCopy(IUnknown* pObject);
    HRESULT STDMETHODCALLTYPE        IsValid(BOOL* pbValid);
    HRESULT STDMETHODCALLTYPE CreateRelocBreakpoint(ICorDebugValueBreakpoint** ppBreakpoint);
    HRESULT STDMETHODCALLTYPE GetType(CorElementType* pType);
    HRESULT STDMETHODCALLTYPE GetSize(ULONG32* pSize);
    HRESULT STDMETHODCALLTYPE GetAddress(CORDB_ADDRESS* pAddress);
    HRESULT STDMETHODCALLTYPE CreateBreakpoint(ICorDebugValueBreakpoint** ppBreakpoint);
    HRESULT STDMETHODCALLTYPE QueryInterface(REFIID riid, void** ppvObject);

    HRESULT STDMETHODCALLTYPE GetExactType(ICorDebugType** ppType);
    HRESULT STDMETHODCALLTYPE GetSize64(ULONG64* pSize);
    HRESULT STDMETHODCALLTYPE GetValue(void* pTo);
    HRESULT STDMETHODCALLTYPE SetValue(void* pFrom);
    HRESULT STDMETHODCALLTYPE GetVirtualMethodAndType(mdMemberRef memberRef, ICorDebugFunction** ppFunction, ICorDebugType** ppType);
    HRESULT STDMETHODCALLTYPE GetLength(ULONG32* pcchString);
    HRESULT STDMETHODCALLTYPE GetString(ULONG32 cchString, ULONG32* pcchString, WCHAR szString[]);
    HRESULT STDMETHODCALLTYPE CreateHandle(CorDebugHandleType type, ICorDebugHandleValue** ppHandle);
    HRESULT STDMETHODCALLTYPE GetThreadOwningMonitorLock(ICorDebugThread** ppThread, DWORD* pAcquisitionCount);
    HRESULT STDMETHODCALLTYPE GetMonitorEventWaitList(ICorDebugThreadEnum** ppThreadEnum);
    HRESULT STDMETHODCALLTYPE EnumerateExceptionCallStack(ICorDebugExceptionObjectCallStackEnum** ppCallStackEnum);
    HRESULT STDMETHODCALLTYPE GetCachedInterfaceTypes(BOOL bIInspectableOnly, ICorDebugTypeEnum** ppInterfacesEnum);
    HRESULT STDMETHODCALLTYPE GetCachedInterfacePointers(BOOL           bIInspectableOnly,
                                       ULONG32        celt,
                                       ULONG32*       pcEltFetched,
                                       CORDB_ADDRESS* ptrs);
    HRESULT STDMETHODCALLTYPE GetTarget(ICorDebugReferenceValue** ppObject);
    HRESULT STDMETHODCALLTYPE GetFunction(ICorDebugFunction** ppFunction);
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
    ULONG STDMETHODCALLTYPE AddRef(void)
    {
        return (BaseAddRef());
    }
    ULONG STDMETHODCALLTYPE Release(void)
    {
        return (BaseRelease());
    }
    const char* GetClassName()
    {
        return "CordbArrayValue";
    }
    ~CordbArrayValue();
    HRESULT STDMETHODCALLTYPE GetClass(ICorDebugClass** ppClass);
    HRESULT STDMETHODCALLTYPE GetFieldValue(ICorDebugClass* pClass, mdFieldDef fieldDef, ICorDebugValue** ppValue);
    HRESULT STDMETHODCALLTYPE GetVirtualMethod(mdMemberRef memberRef, ICorDebugFunction** ppFunction);
    HRESULT STDMETHODCALLTYPE GetContext(ICorDebugContext** ppContext);
    HRESULT STDMETHODCALLTYPE IsValueClass(BOOL* pbIsValueClass);
    HRESULT STDMETHODCALLTYPE GetManagedCopy(IUnknown** ppObject);
    HRESULT STDMETHODCALLTYPE SetFromManagedCopy(IUnknown* pObject);
    HRESULT STDMETHODCALLTYPE GetType(CorElementType* pType);
    HRESULT STDMETHODCALLTYPE GetSize(ULONG32* pSize);
    HRESULT STDMETHODCALLTYPE GetAddress(CORDB_ADDRESS* pAddress);
    HRESULT STDMETHODCALLTYPE CreateBreakpoint(ICorDebugValueBreakpoint** ppBreakpoint);
    HRESULT STDMETHODCALLTYPE QueryInterface(REFIID riid, void** ppvObject);

    HRESULT STDMETHODCALLTYPE GetVirtualMethodAndType(mdMemberRef memberRef, ICorDebugFunction** ppFunction, ICorDebugType** ppType);
    HRESULT STDMETHODCALLTYPE GetValue(void* pTo);
    HRESULT STDMETHODCALLTYPE SetValue(void* pFrom);
    HRESULT STDMETHODCALLTYPE GetLength(ULONG32* pcchString);
    HRESULT STDMETHODCALLTYPE GetString(ULONG32 cchString, ULONG32* pcchString, WCHAR szString[]);
    HRESULT STDMETHODCALLTYPE IsValid(BOOL* pbValid);
    HRESULT STDMETHODCALLTYPE CreateRelocBreakpoint(ICorDebugValueBreakpoint** ppBreakpoint);
    HRESULT STDMETHODCALLTYPE GetExactType(ICorDebugType** ppType);
    HRESULT STDMETHODCALLTYPE GetSize64(ULONG64* pSize);
    HRESULT STDMETHODCALLTYPE CreateHandle(CorDebugHandleType type, ICorDebugHandleValue** ppHandle);
    HRESULT STDMETHODCALLTYPE GetThreadOwningMonitorLock(ICorDebugThread** ppThread, DWORD* pAcquisitionCount);
    HRESULT STDMETHODCALLTYPE GetMonitorEventWaitList(ICorDebugThreadEnum** ppThreadEnum);
    HRESULT STDMETHODCALLTYPE EnumerateExceptionCallStack(ICorDebugExceptionObjectCallStackEnum** ppCallStackEnum);
    HRESULT STDMETHODCALLTYPE GetCachedInterfaceTypes(BOOL bIInspectableOnly, ICorDebugTypeEnum** ppInterfacesEnum);
    HRESULT STDMETHODCALLTYPE GetCachedInterfacePointers(BOOL           bIInspectableOnly,
                                       ULONG32        celt,
                                       ULONG32*       pcEltFetched,
                                       CORDB_ADDRESS* ptrs);
    HRESULT STDMETHODCALLTYPE GetTarget(ICorDebugReferenceValue** ppObject);
    HRESULT STDMETHODCALLTYPE GetFunction(ICorDebugFunction** ppFunction);

    HRESULT STDMETHODCALLTYPE GetElementType(CorElementType* pType);
    HRESULT STDMETHODCALLTYPE GetRank(ULONG32* pnRank);
    HRESULT STDMETHODCALLTYPE GetCount(ULONG32* pnCount);
    HRESULT STDMETHODCALLTYPE GetDimensions(ULONG32 cdim, ULONG32 dims[]);
    HRESULT STDMETHODCALLTYPE HasBaseIndicies(BOOL* pbHasBaseIndicies);
    HRESULT STDMETHODCALLTYPE GetBaseIndicies(ULONG32 cdim, ULONG32 indicies[]);
    HRESULT STDMETHODCALLTYPE GetElement(ULONG32 cdim, ULONG32 indices[], ICorDebugValue** ppValue);
    HRESULT STDMETHODCALLTYPE GetElementAtPosition(ULONG32 nPosition, ICorDebugValue** ppValue);
};

class CordbValueEnum : public CordbBaseMono,
                       public ICorDebugValueEnum
{
    long m_nThreadDebuggerId;
    long m_nFrameDebuggerId;
    int  m_nCurrentValuePos;
    int  m_nCount;
    ILCodeKind m_nFlags;
    bool m_bIsArgument;
    ICorDebugValue** m_pValues;
public:
    ULONG STDMETHODCALLTYPE AddRef(void)
    {
        return (BaseAddRef());
    }
    ULONG STDMETHODCALLTYPE Release(void)
    {
        return (BaseRelease());
    }
    const char* GetClassName()
    {
        return "CordbValueEnum";
    }

    CordbValueEnum(Connection* conn, long nThreadDebuggerId, long nFrameDebuggerId, bool bIsArgument, ILCodeKind m_nFlags = ILCODE_ORIGINAL_IL);
    HRESULT STDMETHODCALLTYPE Next(ULONG celt, ICorDebugValue* values[], ULONG* pceltFetched);
    HRESULT STDMETHODCALLTYPE Skip(ULONG celt);
    HRESULT STDMETHODCALLTYPE Reset(void);
    HRESULT STDMETHODCALLTYPE Clone(ICorDebugEnum** ppEnum);
    HRESULT STDMETHODCALLTYPE GetCount(ULONG* pcelt);
    HRESULT STDMETHODCALLTYPE QueryInterface(REFIID riid, void** ppvObject);
};


#endif
