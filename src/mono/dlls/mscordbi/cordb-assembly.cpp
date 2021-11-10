// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// File: CORDB-ASSEMBLY.CPP
//

#include <cordb-appdomain.h>
#include <cordb-assembly.h>
#include <cordb-class.h>
#include <cordb-function.h>
#include <cordb-process.h>
#include <cordb.h>
#include "corerror.h"
#include "metamodel.h"
#include "metamodelpub.h"
#include "rwutil.h"
#include "stdafx.h"
#include "stgio.h"

#include "importhelper.h"

#include <metamodelrw.h>
#include "mdlog.h"
#include "mdperf.h"
#include "regmeta.h"
#include "ex.h"

using namespace std;

CordbAssembly::CordbAssembly(Connection* conn, CordbProcess* process, CordbAppDomain* appDomain, int id_assembly)
    : CordbBaseMono(conn)
{
    m_pProcess   = process;
    m_pAppDomain = appDomain;
    m_pAppDomain->InternalAddRef();
    m_debuggerId = id_assembly;
    m_pAssemblyName = NULL;
}

CordbAssembly::~CordbAssembly()
{
    m_pAppDomain->InternalRelease();
    if (m_pAssemblyName)
        free(m_pAssemblyName);
}

HRESULT CordbAssembly::IsFullyTrusted(BOOL* pbFullyTrusted)
{
    *pbFullyTrusted = true;
    LOG((LF_CORDB, LL_INFO100000, "CorDebugAssembly - IsFullyTrusted - NOT IMPLEMENTED\n"));
    return S_OK;
}

HRESULT CordbAssembly::GetAppDomain(ICorDebugAppDomain** ppAppDomain)
{
    LOG((LF_CORDB, LL_INFO1000000, "CorDebugAssembly - GetAppDomain - IMPLEMENTED\n"));
    m_pAppDomain->QueryInterface(IID_ICorDebugAppDomain, (void**)ppAppDomain);
    return S_OK;
}

HRESULT CordbAssembly::EnumerateModules(ICorDebugModuleEnum** ppModules)
{
    LOG((LF_CORDB, LL_INFO100000, "CorDebugAssembly - EnumerateModules - NOT IMPLEMENTED\n"));
    return E_NOTIMPL;
}

HRESULT CordbAssembly::GetCodeBase(ULONG32 cchName, ULONG32* pcchName, WCHAR szName[])
{
    LOG((LF_CORDB, LL_INFO100000, "CorDebugAssembly - GetCodeBase - NOT IMPLEMENTED\n"));
    return E_NOTIMPL;
}

HRESULT CordbAssembly::GetName(ULONG32 cchName, ULONG32* pcchName, WCHAR szName[])
{
    HRESULT hr = S_OK;
    EX_TRY
    {
        if (!m_pAssemblyName) {
            LOG((LF_CORDB, LL_INFO1000000, "CordbAssembly - GetName - IMPLEMENTED\n"));
            MdbgProtBuffer localbuf;
            m_dbgprot_buffer_init(&localbuf, 128);
            m_dbgprot_buffer_add_id(&localbuf, m_debuggerId);
            int cmdId = conn->SendEvent(MDBGPROT_CMD_SET_ASSEMBLY, MDBGPROT_CMD_ASSEMBLY_GET_LOCATION, &localbuf);
            m_dbgprot_buffer_free(&localbuf);

            ReceivedReplyPacket* received_reply_packet = conn->GetReplyWithError(cmdId);
            CHECK_ERROR_RETURN_FALSE(received_reply_packet);
            MdbgProtBuffer* pReply = received_reply_packet->Buffer();

            m_pAssemblyName = m_dbgprot_decode_string_with_len(pReply->p, &pReply->p, pReply->end, &m_nAssemblyNameLen);

            char* c_mobile_symbols_path = getenv("MOBILE_SYMBOLS_PATH");
            if (strlen(c_mobile_symbols_path) > 0) {

                size_t size_path = strlen(m_pAssemblyName);
                size_t pos_separator = 0;
                for (pos_separator = size_path ; pos_separator > 0 ; pos_separator--) {
                    if (m_pAssemblyName[pos_separator] == DIR_SEPARATOR)
                        break;
                }
                m_nAssemblyNameLen = (int)(size_path + strlen(c_mobile_symbols_path));
                char* symbols_full_path = (char*)malloc(m_nAssemblyNameLen);
                sprintf_s(symbols_full_path, m_nAssemblyNameLen, "%s%s", c_mobile_symbols_path , m_pAssemblyName + pos_separator + 1);

                free(m_pAssemblyName);
                m_pAssemblyName = symbols_full_path;
                m_nAssemblyNameLen = (int) strlen(m_pAssemblyName);
            }
        }

        if (cchName < (ULONG32) m_nAssemblyNameLen + 1)
        {
            *pcchName = m_nAssemblyNameLen + 1;
        }
        else
        {
            MultiByteToWideChar(CP_UTF8, 0, m_pAssemblyName, -1, szName, cchName);
            *pcchName = m_nAssemblyNameLen + 1;
        }
    }
    EX_CATCH_HRESULT(hr);
    return hr;
}

HRESULT CordbAssembly::QueryInterface(REFIID id, _COM_Outptr_ void __RPC_FAR* __RPC_FAR* ppInterface)
{
    if (id == IID_ICorDebugAssembly)
        *ppInterface = static_cast<ICorDebugAssembly*>(this);
    else if (id == IID_ICorDebugAssembly2)
        *ppInterface = static_cast<ICorDebugAssembly2*>(this);
    else if (id == IID_IUnknown)
        *ppInterface = static_cast<IUnknown*>(static_cast<ICorDebugAssembly*>(this));
    else
    {
        *ppInterface = NULL;
        return E_NOINTERFACE;
    }
    AddRef();
    return S_OK;
}

HRESULT CordbAssembly::GetProcess(ICorDebugProcess** ppProcess)
{
    LOG((LF_CORDB, LL_INFO1000000, "CorDebugAssembly - GetProcess - IMPLEMENTED\n"));
    conn->GetProcess()->QueryInterface(IID_ICorDebugProcess, (void**)ppProcess);
    return S_OK;
}

CordbModule::CordbModule(Connection* conn, CordbProcess* process, CordbAssembly* assembly, int id_assembly)
    : CordbBaseMono(conn)
{
    m_pProcess   = process;
    m_pRegMeta   = NULL;
    m_pAssembly  = assembly;
    m_debuggerId = id_assembly;
    m_pAssembly->InternalAddRef();
    dwFlags = 0;
    conn->GetProcess()->AddModule(this);
    m_pPeImage = NULL;
    m_pAssemblyName = NULL;
}

CordbModule::~CordbModule()
{
    if (m_pAssembly)
        m_pAssembly->InternalRelease();
    /*if (m_pPeImage)
        free(m_pPeImage);*/
    if (m_pAssemblyName)
        free(m_pAssemblyName);
}

HRESULT CordbModule::QueryInterface(REFIID id, void** pInterface)
{
    if (id == IID_ICorDebugModule)
    {
        *pInterface = static_cast<ICorDebugModule*>(this);
    }
    else if (id == IID_ICorDebugModule2)
    {
        *pInterface = static_cast<ICorDebugModule2*>(this);
    }
    else if (id == IID_ICorDebugModule3)
    {
        *pInterface = static_cast<ICorDebugModule3*>(this);
    }
    else if (id == IID_ICorDebugModule4)
    {
        *pInterface = static_cast<ICorDebugModule4*>(this);
    }
    else if (id == IID_IUnknown)
    {
        *pInterface = static_cast<IUnknown*>(static_cast<ICorDebugModule*>(this));
    }
    else
    {
        *pInterface = NULL;
        return E_NOINTERFACE;
    }
    AddRef();
    return S_OK;
}

HRESULT CordbModule::IsMappedLayout(BOOL* pIsMapped)
{
    *pIsMapped = FALSE;
    LOG((LF_CORDB, LL_INFO1000000, "CordbModule - IsMappedLayout - IMPLEMENTED\n"));
    return S_OK;
}

HRESULT CordbModule::CreateReaderForInMemorySymbols(REFIID riid, void** ppObj)
{
    LOG((LF_CORDB, LL_INFO100000, "CordbModule - CreateReaderForInMemorySymbols - NOT IMPLEMENTED\n"));
    return E_NOTIMPL;
}

HRESULT CordbModule::SetJMCStatus(BOOL bIsJustMyCode, ULONG32 cOthers, mdToken pTokens[])
{
    if (cOthers != 0)
    {
        _ASSERTE(!"not yet impl for cOthers != 0");
        return E_NOTIMPL;
    }
    LOG((LF_CORDB, LL_INFO100000, "CordbModule - SetJMCStatus - IMPLEMENTED\n"));
    //on mono JMC is not by module, for now receiving this for one module, will affect all.
    if (bIsJustMyCode)
        conn->GetProcess()->SetJMCStatus(bIsJustMyCode);
    return S_OK;
}

HRESULT CordbModule::ApplyChanges(ULONG cbMetadata, BYTE pbMetadata[], ULONG cbIL, BYTE pbIL[])
{
    LOG((LF_CORDB, LL_INFO100000, "CordbModule - ApplyChanges - NOT IMPLEMENTED\n"));
    return E_NOTIMPL;
}

HRESULT CordbModule::SetJITCompilerFlags(DWORD dwFlags)
{
    this->dwFlags = dwFlags;
    LOG((LF_CORDB, LL_INFO100000, "CordbModule - SetJITCompilerFlags - NOT IMPLEMENTED\n"));
    return S_OK;
}

HRESULT CordbModule::GetJITCompilerFlags(DWORD* pdwFlags)
{
    *pdwFlags = dwFlags;
    LOG((LF_CORDB, LL_INFO100000, "CordbModule - GetJITCompilerFlags - NOT IMPLEMENTED\n"));
    return S_OK;
}

HRESULT CordbModule::ResolveAssembly(mdToken tkAssemblyRef, ICorDebugAssembly** ppAssembly)
{
    LOG((LF_CORDB, LL_INFO100000, "CordbModule - ResolveAssembly - NOT IMPLEMENTED\n"));
    return E_NOTIMPL;
}

HRESULT CordbModule::GetProcess(ICorDebugProcess** ppProcess)
{
    LOG((LF_CORDB, LL_INFO100000, "CordbModule - GetProcess - IMPLEMENTED\n"));
    conn->GetProcess()->QueryInterface(IID_ICorDebugProcess, (void**)ppProcess);
    return S_OK;
}

HRESULT CordbModule::GetBaseAddress(CORDB_ADDRESS* pAddress)
{
    HRESULT hr = S_OK;
    EX_TRY
    {
        if (!m_pPeImage) {
            MdbgProtBuffer localbuf;
            m_dbgprot_buffer_init(&localbuf, 128);
            m_dbgprot_buffer_add_id(&localbuf, GetDebuggerId());
            int cmdId = conn->SendEvent(MDBGPROT_CMD_SET_ASSEMBLY, MDBGPROT_CMD_ASSEMBLY_GET_PEIMAGE_ADDRESS, &localbuf);
            m_dbgprot_buffer_free(&localbuf);

            ReceivedReplyPacket* received_reply_packet = conn->GetReplyWithError(cmdId);
            CHECK_ERROR_RETURN_FALSE(received_reply_packet);
            MdbgProtBuffer* pReply = received_reply_packet->Buffer();

            m_pPeImage = m_dbgprot_decode_long(pReply->p, &pReply->p, pReply->end);
            m_nPeImageSize = m_dbgprot_decode_int(pReply->p, &pReply->p, pReply->end);
        }
        LOG((LF_CORDB, LL_INFO1000000, "CordbModule - GetBaseAddress - IMPLEMENTED\n"));
        *pAddress = (CORDB_ADDRESS)m_pPeImage;
    }
    EX_CATCH_HRESULT(hr);
    return hr;
}

HRESULT CordbModule::GetName(ULONG32 cchName, ULONG32* pcchName, WCHAR szName[])
{
    return m_pAssembly->GetName(cchName, pcchName, szName);
}

HRESULT CordbModule::EnableJITDebugging(BOOL bTrackJITInfo, BOOL bAllowJitOpts)
{
    LOG((LF_CORDB, LL_INFO100000, "CordbModule - EnableJITDebugging - NOT IMPLEMENTED\n"));
    return E_NOTIMPL;
}

HRESULT CordbModule::EnableClassLoadCallbacks(BOOL bClassLoadCallbacks)
{
    LOG((LF_CORDB, LL_INFO100000, "CordbModule - EnableClassLoadCallbacks - NOT IMPLEMENTED\n"));
    return E_NOTIMPL;
}

HRESULT CordbModule::GetFunctionFromToken(mdMethodDef methodDef, ICorDebugFunction** ppFunction)
{
    HRESULT hr = S_OK;
    EX_TRY
    {
        LOG((LF_CORDB, LL_INFO1000000, "CordbModule - GetFunctionFromToken - IMPLEMENTED\n"));
        MdbgProtBuffer localbuf;
        m_dbgprot_buffer_init(&localbuf, 128);
        m_dbgprot_buffer_add_id(&localbuf, m_debuggerId);
        m_dbgprot_buffer_add_int(&localbuf, methodDef);
        int cmdId = conn->SendEvent(MDBGPROT_CMD_SET_ASSEMBLY, MDBGPROT_CMD_ASSEMBLY_GET_METHOD_FROM_TOKEN, &localbuf);
        m_dbgprot_buffer_free(&localbuf);

        ReceivedReplyPacket* received_reply_packet = conn->GetReplyWithError(cmdId);
        if (received_reply_packet->Error() == 0 && received_reply_packet->Error2() == 0)
        {
            MdbgProtBuffer* pReply = received_reply_packet->Buffer();

            int            id = m_dbgprot_decode_id(pReply->p, &pReply->p, pReply->end);
            CordbFunction* func = NULL;
            func = m_pProcess->FindFunction(id);
            if (func == NULL)
            {
                func = new CordbFunction(conn, methodDef, id, this);
            }
            func->QueryInterface(IID_ICorDebugFunction, (void**)ppFunction);
        }
    }
    EX_CATCH_HRESULT(hr);
    return hr;
}

HRESULT CordbModule::GetFunctionFromRVA(CORDB_ADDRESS rva, ICorDebugFunction** ppFunction)
{
    LOG((LF_CORDB, LL_INFO100000, "CordbModule - GetFunctionFromRVA - NOT IMPLEMENTED\n"));
    return E_NOTIMPL;
}

HRESULT CordbModule::GetClassFromToken(mdTypeDef typeDef, ICorDebugClass** ppClass)
{
    CordbClass* pClass = conn->GetProcess()->FindOrAddClass(typeDef, GetDebuggerId());
    pClass->QueryInterface(IID_ICorDebugClass, (void**)ppClass);
    return S_OK;
}

HRESULT
CordbModule::CreateBreakpoint(ICorDebugModuleBreakpoint** ppBreakpoint)
{
    LOG((LF_CORDB, LL_INFO100000, "CordbModule - CreateBreakpoint - NOT IMPLEMENTED\n"));
    return E_NOTIMPL;
}

HRESULT CordbModule::GetEditAndContinueSnapshot(ICorDebugEditAndContinueSnapshot** ppEditAndContinueSnapshot)
{
    LOG((LF_CORDB, LL_INFO100000, "CordbModule - GetEditAndContinueSnapshot - NOT IMPLEMENTED\n"));
    return E_NOTIMPL;
}

HRESULT CordbModule::GetMetaDataInterface(REFIID riid, IUnknown** ppObj)
{
    if (m_pRegMeta == NULL)
    {
        OptionValue optionForNewScope;
        memset(&optionForNewScope, 0, sizeof(OptionValue));
        optionForNewScope.m_ThreadSafetyOptions = MDThreadSafetyOn;

        m_pRegMeta = new RegMeta();
        m_pRegMeta->SetOption(&optionForNewScope);

        m_pStgdbRW       = new CLiteWeightStgdbRW();
        ULONG32 pcchName = 0;
        GetName(0, &pcchName, NULL);

        WCHAR* full_path;
        full_path = (WCHAR*)malloc(sizeof(WCHAR) * pcchName);
        GetName(pcchName, &pcchName, full_path);

        HRESULT ret = m_pStgdbRW->OpenForRead(full_path, NULL, 0, 0);
        free(full_path);

        if (ret != S_OK)
        {
            delete m_pRegMeta;
            delete m_pStgdbRW;
            m_pRegMeta = NULL;
            m_pStgdbRW = NULL;
            return CORDBG_E_MISSING_METADATA;
        }

        m_pRegMeta->InitWithStgdb((ICorDebugModule*)this, m_pStgdbRW);
    }
    m_pRegMeta->QueryInterface(riid, (void**)ppObj);
    LOG((LF_CORDB, LL_INFO1000000, "CordbModule - GetMetaDataInterface - IMPLEMENTED\n"));
    return S_OK;
}

HRESULT CordbModule::GetToken(mdModule* pToken)
{
    LOG((LF_CORDB, LL_INFO100000, "CordbModule - GetToken - NOT IMPLEMENTED\n"));
    return E_NOTIMPL;
}

HRESULT CordbModule::IsDynamic(BOOL* pDynamic)
{
    LOG((LF_CORDB, LL_INFO1000000, "CordbModule - IsDynamic - IMPLEMENTED\n"));
    HRESULT hr = S_OK;
    EX_TRY
    {
        MdbgProtBuffer localbuf;
        m_dbgprot_buffer_init(&localbuf, 128);
        m_dbgprot_buffer_add_id(&localbuf, GetDebuggerId());
        int cmdId = conn->SendEvent(MDBGPROT_CMD_SET_ASSEMBLY, MDBGPROT_CMD_ASSEMBLY_GET_IS_DYNAMIC, &localbuf);
        m_dbgprot_buffer_free(&localbuf);

        ReceivedReplyPacket* received_reply_packet = conn->GetReplyWithError(cmdId);
        CHECK_ERROR_RETURN_FALSE(received_reply_packet);
        MdbgProtBuffer* pReply = received_reply_packet->Buffer();

        int m_bIsDynamic = m_dbgprot_decode_byte(pReply->p, &pReply->p, pReply->end);
        *pDynamic = m_bIsDynamic;
    }
    EX_CATCH_HRESULT(hr);
    return hr;
}

HRESULT CordbModule::GetGlobalVariableValue(mdFieldDef fieldDef, ICorDebugValue** ppValue)
{
    LOG((LF_CORDB, LL_INFO100000, "CordbModule - GetGlobalVariableValue - NOT IMPLEMENTED\n"));
    return E_NOTIMPL;
}

HRESULT CordbModule::GetSize(ULONG32* pcBytes)
{
    LOG((LF_CORDB, LL_INFO100000, "CordbModule - GetSize -IMPLEMENTED\n"));
    *pcBytes = m_nPeImageSize;
    return S_OK;
}

HRESULT CordbModule::IsInMemory(BOOL* pInMemory)
{
    LOG((LF_CORDB, LL_INFO1000000, "CordbModule - IsInMemory - IMPLEMENTED\n"));
    *pInMemory = FALSE;
    return S_OK;
}

HRESULT CordbModule::GetAssembly(ICorDebugAssembly** ppAssembly)
{
    LOG((LF_CORDB, LL_INFO1000000, "CordbModule - GetAssembly - IMPLEMENTED\n"));
    m_pAssembly->QueryInterface(IID_ICorDebugAssembly, (void**)ppAssembly);
    return S_OK;
}
