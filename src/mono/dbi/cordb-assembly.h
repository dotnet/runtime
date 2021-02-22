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
    uint8_t*            m_pAssemblyMetadataBlob;
    int32_t             m_assemblyMetadataLen;
    unsigned long       dwFlags;

public:
    CordbModule(Connection* conn, CordbProcess* process, CordbAssembly* assembly, int id_assembly);
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
        return "CordbModule";
    }
    ~CordbModule();

    HRESULT QueryInterface(REFIID id, void** pInterface);
    HRESULT IsMappedLayout(BOOL* pIsMapped);
    HRESULT CreateReaderForInMemorySymbols(REFIID riid, void** ppObj);
    HRESULT
    SetJMCStatus(BOOL bIsJustMyCode, ULONG32 cTokens, mdToken pTokens[]);
    HRESULT
    ApplyChanges(ULONG cbMetadata, BYTE pbMetadata[], ULONG cbIL, BYTE pbIL[]);
    HRESULT SetJITCompilerFlags(DWORD dwFlags);
    HRESULT GetJITCompilerFlags(DWORD* pdwFlags);
    HRESULT
    ResolveAssembly(mdToken tkAssemblyRef, ICorDebugAssembly** ppAssembly);
    HRESULT
    GetProcess(ICorDebugProcess** ppProcess);
    HRESULT GetBaseAddress(CORDB_ADDRESS* pAddress);
    HRESULT
    GetAssembly(ICorDebugAssembly** ppAssembly);
    HRESULT
    GetName(ULONG32 cchName, ULONG32* pcchName, WCHAR szName[]);
    HRESULT EnableJITDebugging(BOOL bTrackJITInfo, BOOL bAllowJitOpts);
    HRESULT
    EnableClassLoadCallbacks(BOOL bClassLoadCallbacks);
    HRESULT
    GetFunctionFromToken(mdMethodDef methodDef, ICorDebugFunction** ppFunction);
    HRESULT
    GetFunctionFromRVA(CORDB_ADDRESS rva, ICorDebugFunction** ppFunction);
    HRESULT GetClassFromToken(mdTypeDef typeDef, ICorDebugClass** ppClass);
    HRESULT
    CreateBreakpoint(ICorDebugModuleBreakpoint** ppBreakpoint);
    HRESULT GetEditAndContinueSnapshot(ICorDebugEditAndContinueSnapshot** ppEditAndContinueSnapshot);
    HRESULT GetMetaDataInterface(REFIID riid, IUnknown** ppObj);
    HRESULT GetToken(mdModule* pToken);
    HRESULT IsDynamic(BOOL* pDynamic);
    HRESULT
    GetGlobalVariableValue(mdFieldDef fieldDef, ICorDebugValue** ppValue);
    HRESULT GetSize(ULONG32* pcBytes);
    HRESULT IsInMemory(BOOL* pInMemory);
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

public:
    CordbAssembly(Connection* conn, CordbProcess* process, CordbAppDomain* appDomain, int id_assembly);
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
        return "CordbAssembly";
    }
    ~CordbAssembly();
    HRESULT IsFullyTrusted(BOOL* pbFullyTrusted);
    HRESULT
    GetProcess(ICorDebugProcess** ppProcess);
    HRESULT
    GetAppDomain(ICorDebugAppDomain** ppAppDomain);
    HRESULT
    EnumerateModules(ICorDebugModuleEnum** ppModules);
    HRESULT
    GetCodeBase(ULONG32 cchName, ULONG32* pcchName, WCHAR szName[]);
    HRESULT
    GetName(ULONG32 cchName, ULONG32* pcchName, WCHAR szName[]);
    HRESULT
    QueryInterface(REFIID id, _COM_Outptr_ void __RPC_FAR* __RPC_FAR* ppInterface);
};

#endif
