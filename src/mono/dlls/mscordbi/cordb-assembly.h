// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// File: CORDB-ASSEMBLY.H
//

#ifndef __MONO_DEBUGGER_CORDB_ASSEMBLY_H__
#define __MONO_DEBUGGER_CORDB_ASSEMBLY_H__

#include <cordb.h>

class CLiteWeightStgdbRW;

class CordbModule : public CordbBaseMono,
                    public ICorDebugModule,
                    public ICorDebugModule2,
                    public ICorDebugModule3,
                    public ICorDebugModule4
{
    int                 m_debuggerId; // id on mono side;
    CordbProcess*       m_pProcess;
    RegMeta*            m_pRegMeta;
    CordbAssembly*      m_pAssembly;
    CLiteWeightStgdbRW* m_pStgdbRW;
    CORDB_ADDRESS       m_pPeImage;
    int32_t             m_nPeImageSize;
    unsigned long       dwFlags;
    char *              m_pAssemblyName;
    int                 m_nAssemblyNameLen;

public:
    CordbModule(Connection* conn, CordbProcess* process, CordbAssembly* assembly, int id_assembly);
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
        return "CordbModule";
    }
    ~CordbModule();

    HRESULT STDMETHODCALLTYPE QueryInterface(REFIID id, void** pInterface);
    HRESULT STDMETHODCALLTYPE IsMappedLayout(BOOL* pIsMapped);
    HRESULT STDMETHODCALLTYPE CreateReaderForInMemorySymbols(REFIID riid, void** ppObj);
    HRESULT STDMETHODCALLTYPE SetJMCStatus(BOOL bIsJustMyCode, ULONG32 cTokens, mdToken pTokens[]);
    HRESULT STDMETHODCALLTYPE ApplyChanges(ULONG cbMetadata, BYTE pbMetadata[], ULONG cbIL, BYTE pbIL[]);
    HRESULT STDMETHODCALLTYPE SetJITCompilerFlags(DWORD dwFlags);
    HRESULT STDMETHODCALLTYPE GetJITCompilerFlags(DWORD* pdwFlags);
    HRESULT STDMETHODCALLTYPE ResolveAssembly(mdToken tkAssemblyRef, ICorDebugAssembly** ppAssembly);
    HRESULT STDMETHODCALLTYPE GetProcess(ICorDebugProcess** ppProcess);
    HRESULT STDMETHODCALLTYPE GetBaseAddress(CORDB_ADDRESS* pAddress);
    HRESULT STDMETHODCALLTYPE GetAssembly(ICorDebugAssembly** ppAssembly);
    HRESULT STDMETHODCALLTYPE GetName(ULONG32 cchName, ULONG32* pcchName, WCHAR szName[]);
    HRESULT STDMETHODCALLTYPE EnableJITDebugging(BOOL bTrackJITInfo, BOOL bAllowJitOpts);
    HRESULT STDMETHODCALLTYPE EnableClassLoadCallbacks(BOOL bClassLoadCallbacks);
    HRESULT STDMETHODCALLTYPE GetFunctionFromToken(mdMethodDef methodDef, ICorDebugFunction** ppFunction);
    HRESULT STDMETHODCALLTYPE GetFunctionFromRVA(CORDB_ADDRESS rva, ICorDebugFunction** ppFunction);
    HRESULT STDMETHODCALLTYPE GetClassFromToken(mdTypeDef typeDef, ICorDebugClass** ppClass);
    HRESULT STDMETHODCALLTYPE CreateBreakpoint(ICorDebugModuleBreakpoint** ppBreakpoint);
    HRESULT STDMETHODCALLTYPE GetEditAndContinueSnapshot(ICorDebugEditAndContinueSnapshot** ppEditAndContinueSnapshot);
    HRESULT STDMETHODCALLTYPE GetMetaDataInterface(REFIID riid, IUnknown** ppObj);
    HRESULT STDMETHODCALLTYPE GetToken(mdModule* pToken);
    HRESULT STDMETHODCALLTYPE IsDynamic(BOOL* pDynamic);
    HRESULT STDMETHODCALLTYPE GetGlobalVariableValue(mdFieldDef fieldDef, ICorDebugValue** ppValue);
    HRESULT STDMETHODCALLTYPE GetSize(ULONG32* pcBytes);
    HRESULT STDMETHODCALLTYPE IsInMemory(BOOL* pInMemory);
    int     GetDebuggerId() const
    {
        return m_debuggerId;
    }
};

class CordbAssembly : public CordbBaseMono, public ICorDebugAssembly, public ICorDebugAssembly2
{
    CordbProcess*   m_pProcess;
    CordbAppDomain* m_pAppDomain;
    int             m_debuggerId;
    char*           m_pAssemblyName;
    int             m_nAssemblyNameLen;

public:
    CordbAssembly(Connection* conn, CordbProcess* process, CordbAppDomain* appDomain, int id_assembly);
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
        return "CordbAssembly";
    }
    ~CordbAssembly();
    HRESULT STDMETHODCALLTYPE IsFullyTrusted(BOOL* pbFullyTrusted);
    HRESULT STDMETHODCALLTYPE GetProcess(ICorDebugProcess** ppProcess);
    HRESULT STDMETHODCALLTYPE GetAppDomain(ICorDebugAppDomain** ppAppDomain);
    HRESULT STDMETHODCALLTYPE EnumerateModules(ICorDebugModuleEnum** ppModules);
    HRESULT STDMETHODCALLTYPE GetCodeBase(ULONG32 cchName, ULONG32* pcchName, WCHAR szName[]);
    HRESULT STDMETHODCALLTYPE GetName(ULONG32 cchName, ULONG32* pcchName, WCHAR szName[]);
    HRESULT STDMETHODCALLTYPE QueryInterface(REFIID id, _COM_Outptr_ void __RPC_FAR* __RPC_FAR* ppInterface);
};

#endif
