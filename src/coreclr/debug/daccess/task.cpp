// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//*****************************************************************************
// File: task.cpp
//

//
// ClrDataTask.
//
//*****************************************************************************

#include "stdafx.h"

// XXX Microsoft - Why aren't these extra MD APIs in a header?
STDAPI GetMDPublicInterfaceFromInternal(
   void        *pIUnkPublic,           // [IN] Given scope.
   REFIID      riid,                   // [in] The interface desired.
   void        **ppIUnkInternal);      // [out] Return interface on success.

STDAPI  GetMetaDataPublicInterfaceFromInternal(
    void        *pv,                    // [IN] Given interface.
    REFIID      riid,                   // [IN] desired interface.
    void        **ppv)                  // [OUT] returned interface
{
    return GetMDPublicInterfaceFromInternal(pv, riid, ppv);
}

//----------------------------------------------------------------------------
//
// ClrDataTask.
//
//----------------------------------------------------------------------------

ClrDataTask::ClrDataTask(ClrDataAccess* dac,
                         Thread* thread)
{
    m_dac = dac;
    m_dac->AddRef();
    m_instanceAge = m_dac->m_instanceAge;
    m_thread = thread;
    m_refs = 1;
}

ClrDataTask::~ClrDataTask(void)
{
    m_dac->Release();
}

STDMETHODIMP
ClrDataTask::QueryInterface(THIS_
                            IN REFIID interfaceId,
                            OUT PVOID* iface)
{
    if (IsEqualIID(interfaceId, IID_IUnknown) ||
        IsEqualIID(interfaceId, __uuidof(IXCLRDataTask)))
    {
        AddRef();
        *iface = static_cast<IUnknown*>
            (static_cast<IXCLRDataTask*>(this));
        return S_OK;
    }
    else
    {
        *iface = NULL;
        return E_NOINTERFACE;
    }
}

STDMETHODIMP_(ULONG)
ClrDataTask::AddRef(THIS)
{
    return InterlockedIncrement(&m_refs);
}

STDMETHODIMP_(ULONG)
ClrDataTask::Release(THIS)
{
    SUPPORTS_DAC_HOST_ONLY;
    LONG newRefs = InterlockedDecrement(&m_refs);
    if (newRefs == 0)
    {
        delete this;
    }
    return newRefs;
}

HRESULT STDMETHODCALLTYPE
ClrDataTask::GetProcess(
    /* [out] */ IXCLRDataProcess **process)
{
    HRESULT status;

    DAC_ENTER_SUB(m_dac);

    EX_TRY
    {
        *process = static_cast<IXCLRDataProcess*>(m_dac);
        m_dac->AddRef();
        status = S_OK;
    }
    EX_CATCH
    {
        if (!DacExceptionFilter(GET_EXCEPTION(), m_dac, &status))
        {
            EX_RETHROW;
        }
    }
    EX_END_CATCH(SwallowAllExceptions)

    DAC_LEAVE();
    return status;
}

HRESULT STDMETHODCALLTYPE
ClrDataTask::GetCurrentAppDomain(
    /* [out] */ IXCLRDataAppDomain **appDomain)
{
    HRESULT status;

    DAC_ENTER_SUB(m_dac);

    EX_TRY
    {
        if (m_thread->GetDomain())
        {
            *appDomain = new (nothrow)
                ClrDataAppDomain(m_dac, m_thread->GetDomain());
            status = *appDomain ? S_OK : E_OUTOFMEMORY;
        }
        else
        {
            status = E_INVALIDARG;
        }
    }
    EX_CATCH
    {
        if (!DacExceptionFilter(GET_EXCEPTION(), m_dac, &status))
        {
            EX_RETHROW;
        }
    }
    EX_END_CATCH(SwallowAllExceptions)

    DAC_LEAVE();
    return status;
}

HRESULT STDMETHODCALLTYPE
ClrDataTask::GetName(
    /* [in] */ ULONG32 bufLen,
    /* [out] */ ULONG32 *nameLen,
    /* [size_is][out] */ __out_ecount_part_opt(bufLen, *nameLen) WCHAR name[  ])
{
    HRESULT status;

    DAC_ENTER_SUB(m_dac);

    EX_TRY
    {
        // XXX - Microsoft.
        status = E_NOTIMPL;
    }
    EX_CATCH
    {
        if (!DacExceptionFilter(GET_EXCEPTION(), m_dac, &status))
        {
            EX_RETHROW;
        }
    }
    EX_END_CATCH(SwallowAllExceptions)

    DAC_LEAVE();
    return status;
}

HRESULT STDMETHODCALLTYPE
ClrDataTask::GetUniqueID(
    /* [out] */ ULONG64 *id)
{
    HRESULT status;

    DAC_ENTER_SUB(m_dac);

    EX_TRY
    {
        *id = m_thread->GetThreadId();
        status = S_OK;
    }
    EX_CATCH
    {
        if (!DacExceptionFilter(GET_EXCEPTION(), m_dac, &status))
        {
            EX_RETHROW;
        }
    }
    EX_END_CATCH(SwallowAllExceptions)

    DAC_LEAVE();
    return status;
}

HRESULT STDMETHODCALLTYPE
ClrDataTask::GetFlags(
    /* [out] */ ULONG32 *flags)
{
    HRESULT status;

    DAC_ENTER_SUB(m_dac);

    EX_TRY
    {
        // XXX Microsoft - GC check.
        *flags = CLRDATA_TASK_DEFAULT;
        status = S_OK;
    }
    EX_CATCH
    {
        if (!DacExceptionFilter(GET_EXCEPTION(), m_dac, &status))
        {
            EX_RETHROW;
        }
    }
    EX_END_CATCH(SwallowAllExceptions)

    DAC_LEAVE();
    return status;
}

HRESULT STDMETHODCALLTYPE
ClrDataTask::IsSameObject(
    /* [in] */ IXCLRDataTask* task)
{
    HRESULT status;

    DAC_ENTER_SUB(m_dac);

    EX_TRY
    {
        status = PTR_HOST_TO_TADDR(m_thread) ==
            PTR_HOST_TO_TADDR(((ClrDataTask*)task)->m_thread) ?
            S_OK : S_FALSE;
    }
    EX_CATCH
    {
        if (!DacExceptionFilter(GET_EXCEPTION(), m_dac, &status))
        {
            EX_RETHROW;
        }
    }
    EX_END_CATCH(SwallowAllExceptions)

    DAC_LEAVE();
    return status;
}

HRESULT STDMETHODCALLTYPE
ClrDataTask::GetManagedObject(
    /* [out] */ IXCLRDataValue **value)
{
    HRESULT status;

    DAC_ENTER_SUB(m_dac);

    EX_TRY
    {
        // XXX Microsoft.
        status = E_NOTIMPL;
    }
    EX_CATCH
    {
        if (!DacExceptionFilter(GET_EXCEPTION(), m_dac, &status))
        {
            EX_RETHROW;
        }
    }
    EX_END_CATCH(SwallowAllExceptions)

    DAC_LEAVE();
    return status;
}

HRESULT STDMETHODCALLTYPE
ClrDataTask::GetDesiredExecutionState(
    /* [out] */ ULONG32 *state)
{
    HRESULT status;

    DAC_ENTER_SUB(m_dac);

    EX_TRY
    {
        // XXX Microsoft.
        status = E_NOTIMPL;
    }
    EX_CATCH
    {
        if (!DacExceptionFilter(GET_EXCEPTION(), m_dac, &status))
        {
            EX_RETHROW;
        }
    }
    EX_END_CATCH(SwallowAllExceptions)

    DAC_LEAVE();
    return status;
}

HRESULT STDMETHODCALLTYPE
ClrDataTask::SetDesiredExecutionState(
    /* [in] */ ULONG32 state)
{
    HRESULT status;

    DAC_ENTER_SUB(m_dac);

    EX_TRY
    {
        // XXX Microsoft.
        status = E_NOTIMPL;
    }
    EX_CATCH
    {
        if (!DacExceptionFilter(GET_EXCEPTION(), m_dac, &status))
        {
            EX_RETHROW;
        }
    }
    EX_END_CATCH(SwallowAllExceptions)

    DAC_LEAVE();
    return status;
}

HRESULT STDMETHODCALLTYPE
ClrDataTask::CreateStackWalk(
    /* [in] */ ULONG32 flags,
    /* [out] */ IXCLRDataStackWalk **stackWalk)
{
    HRESULT status;

    if (flags & ~SIMPFRAME_ALL)
    {
        return E_INVALIDARG;
    }

    DAC_ENTER_SUB(m_dac);

    ClrDataStackWalk* walkClass = NULL;

    EX_TRY
    {
        walkClass = new (nothrow) ClrDataStackWalk(m_dac, m_thread, flags);

        if (!walkClass)
        {
            status = E_OUTOFMEMORY;
        }
        else if ((status = walkClass->Init()) != S_OK)
        {
            delete walkClass;
        }
        else
        {
            *stackWalk = static_cast<IXCLRDataStackWalk*>(walkClass);
        }
    }
    EX_CATCH
    {
        if (walkClass)
        {
            delete walkClass;
        }

        if (!DacExceptionFilter(GET_EXCEPTION(), m_dac, &status))
        {
            EX_RETHROW;
        }
    }
    EX_END_CATCH(SwallowAllExceptions)

    DAC_LEAVE();
    return status;
}

HRESULT STDMETHODCALLTYPE
ClrDataTask::GetOSThreadID(
    /* [out] */ ULONG32 *id)
{
    HRESULT status;

    DAC_ENTER_SUB(m_dac);

    EX_TRY
    {
        if (m_thread->GetOSThreadId() &&
            m_thread->GetOSThreadId() != 0xbaadf00d)
        {
            *id = m_thread->GetOSThreadId();
            status = S_OK;
        }
        else
        {
            *id = 0;
            status = S_FALSE;
        }
    }
    EX_CATCH
    {
        if (!DacExceptionFilter(GET_EXCEPTION(), m_dac, &status))
        {
            EX_RETHROW;
        }
    }
    EX_END_CATCH(SwallowAllExceptions)

    DAC_LEAVE();
    return status;
}

HRESULT STDMETHODCALLTYPE
ClrDataTask::GetContext(
    /* [in] */ ULONG32 contextFlags,
    /* [in] */ ULONG32 contextBufSize,
    /* [out] */ ULONG32 *contextSize,
    /* [size_is][out] */ BYTE contextBuf[  ])
{
    HRESULT status;

    if (contextSize)
    {
        *contextSize = ContextSizeForFlags(contextFlags);
    }

    if (!CheckContextSizeForFlags(contextBufSize, contextFlags))
    {
        return E_INVALIDARG;
    }

    DAC_ENTER_SUB(m_dac);

    EX_TRY
    {
        if (m_thread->GetOSThreadId())
        {
            status = m_dac->m_pTarget->
                GetThreadContext(m_thread->GetOSThreadId(),
                                 contextFlags,
                                 contextBufSize,
                                 contextBuf);
        }
        else
        {
            status = E_INVALIDARG;
        }
    }
    EX_CATCH
    {
        if (!DacExceptionFilter(GET_EXCEPTION(), m_dac, &status))
        {
            EX_RETHROW;
        }
    }
    EX_END_CATCH(SwallowAllExceptions)

    DAC_LEAVE();
    return status;
}

HRESULT STDMETHODCALLTYPE
ClrDataTask::SetContext(
    /* [in] */ ULONG32 contextSize,
    /* [size_is][in] */ BYTE context[  ])
{
    HRESULT status;

    if (!CheckContextSizeForBuffer(contextSize, context))
    {
        return E_INVALIDARG;
    }

    DAC_ENTER_SUB(m_dac);

    EX_TRY
    {
        if (m_thread->GetOSThreadId())
        {
            status = m_dac->m_pMutableTarget->
                SetThreadContext(m_thread->GetOSThreadId(),
                                 contextSize,
                                 context);
        }
        else
        {
            status = E_INVALIDARG;
        }
    }
    EX_CATCH
    {
        if (!DacExceptionFilter(GET_EXCEPTION(), m_dac, &status))
        {
            EX_RETHROW;
        }
    }
    EX_END_CATCH(SwallowAllExceptions)

    DAC_LEAVE();
    return status;
}

HRESULT STDMETHODCALLTYPE
ClrDataTask::GetCurrentExceptionState(
    /* [out] */ IXCLRDataExceptionState **exception)
{
    HRESULT status;

    DAC_ENTER_SUB(m_dac);

    EX_TRY
    {
        status = ClrDataExceptionState::NewFromThread(m_dac,
                                                      m_thread,
                                                      NULL,
                                                      exception);
    }
    EX_CATCH
    {
        if (!DacExceptionFilter(GET_EXCEPTION(), m_dac, &status))
        {
            EX_RETHROW;
        }
    }
    EX_END_CATCH(SwallowAllExceptions)

    DAC_LEAVE();
    return status;
}

HRESULT STDMETHODCALLTYPE
ClrDataTask::GetLastExceptionState(
    /* [out] */ IXCLRDataExceptionState **exception)
{
    HRESULT status;

    DAC_ENTER_SUB(m_dac);

    EX_TRY
    {
        if (m_thread->m_LastThrownObjectHandle)
        {
            *exception = new (nothrow)
                ClrDataExceptionState(m_dac,
                                      m_thread->GetDomain(),
                                      m_thread,
                                      CLRDATA_EXCEPTION_PARTIAL,
                                      NULL,
                                      m_thread->m_LastThrownObjectHandle,
                                      NULL);
            status = *exception ? S_OK : E_OUTOFMEMORY;
        }
        else
        {
            status = E_NOINTERFACE;
        }
    }
    EX_CATCH
    {
        if (!DacExceptionFilter(GET_EXCEPTION(), m_dac, &status))
        {
            EX_RETHROW;
        }
    }
    EX_END_CATCH(SwallowAllExceptions)

    DAC_LEAVE();
    return status;
}

HRESULT STDMETHODCALLTYPE
ClrDataTask::Request(
    /* [in] */ ULONG32 reqCode,
    /* [in] */ ULONG32 inBufferSize,
    /* [size_is][in] */ BYTE *inBuffer,
    /* [in] */ ULONG32 outBufferSize,
    /* [size_is][out] */ BYTE *outBuffer)
{
    HRESULT status;

    DAC_ENTER_SUB(m_dac);

    EX_TRY
    {
        switch(reqCode)
        {
        case CLRDATA_REQUEST_REVISION:
            if (inBufferSize != 0 ||
                inBuffer ||
                outBufferSize != sizeof(ULONG32))
            {
                status = E_INVALIDARG;
            }
            else
            {
                *(ULONG32*)outBuffer = 3;
                status = S_OK;
            }
            break;

        default:
            status = E_INVALIDARG;
            break;
        }
    }
    EX_CATCH
    {
        if (!DacExceptionFilter(GET_EXCEPTION(), m_dac, &status))
        {
            EX_RETHROW;
        }
    }
    EX_END_CATCH(SwallowAllExceptions)

    DAC_LEAVE();
    return status;
}

//----------------------------------------------------------------------------
//
// ClrDataAppDomain.
//
//----------------------------------------------------------------------------

ClrDataAppDomain::ClrDataAppDomain(ClrDataAccess* dac,
                                   AppDomain* appDomain)
{
    m_dac = dac;
    m_dac->AddRef();
    m_instanceAge = m_dac->m_instanceAge;
    m_appDomain = appDomain;
    m_refs = 1;
}

ClrDataAppDomain::~ClrDataAppDomain(void)
{
    m_dac->Release();
}

STDMETHODIMP
ClrDataAppDomain::QueryInterface(THIS_
                                 IN REFIID interfaceId,
                                 OUT PVOID* iface)
{
    if (IsEqualIID(interfaceId, IID_IUnknown) ||
        IsEqualIID(interfaceId, __uuidof(IXCLRDataAppDomain)))
    {
        AddRef();
        *iface = static_cast<IUnknown*>
            (static_cast<IXCLRDataAppDomain*>(this));
        return S_OK;
    }
    else
    {
        *iface = NULL;
        return E_NOINTERFACE;
    }
}

STDMETHODIMP_(ULONG)
ClrDataAppDomain::AddRef(THIS)
{
    return InterlockedIncrement(&m_refs);
}

STDMETHODIMP_(ULONG)
ClrDataAppDomain::Release(THIS)
{
    SUPPORTS_DAC_HOST_ONLY;
    LONG newRefs = InterlockedDecrement(&m_refs);
    if (newRefs == 0)
    {
        delete this;
    }
    return newRefs;
}

HRESULT STDMETHODCALLTYPE
ClrDataAppDomain::GetProcess(
    /* [out] */ IXCLRDataProcess **process)
{
    HRESULT status;

    DAC_ENTER_SUB(m_dac);

    EX_TRY
    {
        *process = static_cast<IXCLRDataProcess*>(m_dac);
        m_dac->AddRef();
        status = S_OK;
    }
    EX_CATCH
    {
        if (!DacExceptionFilter(GET_EXCEPTION(), m_dac, &status))
        {
            EX_RETHROW;
        }
    }
    EX_END_CATCH(SwallowAllExceptions)

    DAC_LEAVE();
    return status;
}

HRESULT STDMETHODCALLTYPE
ClrDataAppDomain::GetName(
    /* [in] */ ULONG32 bufLen,
    /* [out] */ ULONG32 *nameLen,
    /* [size_is][out] */ __out_ecount_part_opt(bufLen, *nameLen) WCHAR name[  ])
{
    HRESULT status = S_OK;

    DAC_ENTER_SUB(m_dac);

    EX_TRY
    {
        bool isUtf8;
        PVOID rawName = m_appDomain->GetFriendlyNameNoSet(&isUtf8);
        if (rawName)
        {
            if (isUtf8)
            {
                status = ConvertUtf8((LPCUTF8)rawName,
                                     bufLen, nameLen, name);
            }
            else
            {
                status = StringCchCopy(name, bufLen, (PCWSTR)rawName) == S_OK ?
                    S_OK : S_FALSE;
                if (nameLen)
                {
                    size_t cchName = wcslen((PCWSTR)rawName) + 1;
                    if (FitsIn<ULONG32>(cchName))
                    {
                        *nameLen = (ULONG32) cchName;
                    }
                    else
                    {
                        status = COR_E_OVERFLOW;
                    }
                }
            }
        }
        else
        {
            status = E_NOINTERFACE;
        }
    }
    EX_CATCH
    {
        if (!DacExceptionFilter(GET_EXCEPTION(), m_dac, &status))
        {
            EX_RETHROW;
        }
    }
    EX_END_CATCH(SwallowAllExceptions)

    DAC_LEAVE();
    return status;
}

HRESULT STDMETHODCALLTYPE
ClrDataAppDomain::GetFlags(
    /* [out] */ ULONG32 *flags)
{
    HRESULT status;

    DAC_ENTER_SUB(m_dac);

    EX_TRY
    {
        *flags = CLRDATA_DOMAIN_DEFAULT;
        status = S_OK;
    }
    EX_CATCH
    {
        if (!DacExceptionFilter(GET_EXCEPTION(), m_dac, &status))
        {
            EX_RETHROW;
        }
    }
    EX_END_CATCH(SwallowAllExceptions)

    DAC_LEAVE();
    return status;
}

HRESULT STDMETHODCALLTYPE
ClrDataAppDomain::IsSameObject(
    /* [in] */ IXCLRDataAppDomain* appDomain)
{
    HRESULT status;

    DAC_ENTER_SUB(m_dac);

    EX_TRY
    {
        status = PTR_HOST_TO_TADDR(m_appDomain) ==
            PTR_HOST_TO_TADDR(((ClrDataAppDomain*)appDomain)->m_appDomain) ?
            S_OK : S_FALSE;
    }
    EX_CATCH
    {
        if (!DacExceptionFilter(GET_EXCEPTION(), m_dac, &status))
        {
            EX_RETHROW;
        }
    }
    EX_END_CATCH(SwallowAllExceptions)

    DAC_LEAVE();
    return status;
}

HRESULT STDMETHODCALLTYPE
ClrDataAppDomain::GetManagedObject(
    /* [out] */ IXCLRDataValue **value)
{
    HRESULT status;

    DAC_ENTER_SUB(m_dac);

    EX_TRY
    {
        // XXX Microsoft.
        status = E_NOTIMPL;
    }
    EX_CATCH
    {
        if (!DacExceptionFilter(GET_EXCEPTION(), m_dac, &status))
        {
            EX_RETHROW;
        }
    }
    EX_END_CATCH(SwallowAllExceptions)

    DAC_LEAVE();
    return status;
}

HRESULT STDMETHODCALLTYPE
ClrDataAppDomain::GetUniqueID(
    /* [out] */ ULONG64 *id)
{
    HRESULT status;

    DAC_ENTER_SUB(m_dac);

    *id = DefaultADID;
    status = S_OK;

    DAC_LEAVE();
    return status;
}

HRESULT STDMETHODCALLTYPE
ClrDataAppDomain::Request(
    /* [in] */ ULONG32 reqCode,
    /* [in] */ ULONG32 inBufferSize,
    /* [size_is][in] */ BYTE *inBuffer,
    /* [in] */ ULONG32 outBufferSize,
    /* [size_is][out] */ BYTE *outBuffer)
{
    HRESULT status;

    DAC_ENTER_SUB(m_dac);

    EX_TRY
    {
        status = E_INVALIDARG;
    }
    EX_CATCH
    {
        if (!DacExceptionFilter(GET_EXCEPTION(), m_dac, &status))
        {
            EX_RETHROW;
        }
    }
    EX_END_CATCH(SwallowAllExceptions)

    DAC_LEAVE();
    return status;
}

//----------------------------------------------------------------------------
//
// ClrDataAssembly.
//
//----------------------------------------------------------------------------

ClrDataAssembly::ClrDataAssembly(ClrDataAccess* dac,
                                 Assembly* assembly)
{
    m_dac = dac;
    m_dac->AddRef();
    m_instanceAge = m_dac->m_instanceAge;
    m_refs = 1;
    m_assembly = assembly;
}

ClrDataAssembly::~ClrDataAssembly(void)
{
    m_dac->Release();
}

STDMETHODIMP
ClrDataAssembly::QueryInterface(THIS_
                                IN REFIID interfaceId,
                                OUT PVOID* iface)
{
    if (IsEqualIID(interfaceId, IID_IUnknown) ||
        IsEqualIID(interfaceId, __uuidof(IXCLRDataAssembly)))
    {
        AddRef();
        *iface = static_cast<IUnknown*>
            (static_cast<IXCLRDataAssembly*>(this));
        return S_OK;
    }
    else
    {
        *iface = NULL;
        return E_NOINTERFACE;
    }
}

STDMETHODIMP_(ULONG)
ClrDataAssembly::AddRef(THIS)
{
    return InterlockedIncrement(&m_refs);
}

STDMETHODIMP_(ULONG)
ClrDataAssembly::Release(THIS)
{
    SUPPORTS_DAC_HOST_ONLY;
    LONG newRefs = InterlockedDecrement(&m_refs);
    if (newRefs == 0)
    {
        delete this;
    }
    return newRefs;
}

HRESULT STDMETHODCALLTYPE
ClrDataAssembly::StartEnumModules(
    /* [out] */ CLRDATA_ENUM* handle)
{
    HRESULT status;

    DAC_ENTER_SUB(m_dac);

    EX_TRY
    {
        Assembly::ModuleIterator* iter = new (nothrow)
            Assembly::ModuleIterator;
        if (iter)
        {
            *iter = m_assembly->IterateModules();
            *handle = TO_CDENUM(iter);
            status = S_OK;
        }
        else
        {
            status = E_OUTOFMEMORY;
        }
    }
    EX_CATCH
    {
        if (!DacExceptionFilter(GET_EXCEPTION(), m_dac, &status))
        {
            EX_RETHROW;
        }
    }
    EX_END_CATCH(SwallowAllExceptions)

    DAC_LEAVE();
    return status;
}

HRESULT STDMETHODCALLTYPE
ClrDataAssembly::EnumModule(
    /* [in, out] */ CLRDATA_ENUM* handle,
    /* [out] */ IXCLRDataModule **mod)
{
    HRESULT status;

    DAC_ENTER_SUB(m_dac);

    EX_TRY
    {
        Assembly::ModuleIterator* iter =
            FROM_CDENUM(Assembly::ModuleIterator, *handle);
        if (iter->Next())
        {
            *mod = new (nothrow)
                ClrDataModule(m_dac, iter->GetModule());
            status = *mod ? S_OK : E_OUTOFMEMORY;
        }
        else
        {
            status = S_FALSE;
        }
    }
    EX_CATCH
    {
        if (!DacExceptionFilter(GET_EXCEPTION(), m_dac, &status))
        {
            EX_RETHROW;
        }
    }
    EX_END_CATCH(SwallowAllExceptions)

    DAC_LEAVE();
    return status;
}

HRESULT STDMETHODCALLTYPE
ClrDataAssembly::EndEnumModules(
    /* [in] */ CLRDATA_ENUM handle)
{
    HRESULT status;

    DAC_ENTER_SUB(m_dac);

    EX_TRY
    {
        Assembly::ModuleIterator* iter =
            FROM_CDENUM(Assembly::ModuleIterator, handle);
        delete iter;
        status = S_OK;
    }
    EX_CATCH
    {
        if (!DacExceptionFilter(GET_EXCEPTION(), m_dac, &status))
        {
            EX_RETHROW;
        }
    }
    EX_END_CATCH(SwallowAllExceptions)

    DAC_LEAVE();
    return status;
}

HRESULT STDMETHODCALLTYPE
ClrDataAssembly::StartEnumAppDomains(
    /* [out] */ CLRDATA_ENUM* handle)
{
    HRESULT status;

    DAC_ENTER_SUB(m_dac);

    EX_TRY
    {
        // XXX Microsoft.
        status = E_NOTIMPL;
    }
    EX_CATCH
    {
        if (!DacExceptionFilter(GET_EXCEPTION(), m_dac, &status))
        {
            EX_RETHROW;
        }
    }
    EX_END_CATCH(SwallowAllExceptions)

    DAC_LEAVE();
    return status;
}

HRESULT STDMETHODCALLTYPE
ClrDataAssembly::EnumAppDomain(
    /* [in, out] */ CLRDATA_ENUM* handle,
    /* [out] */ IXCLRDataAppDomain **appDomain)
{
    HRESULT status;

    DAC_ENTER_SUB(m_dac);

    EX_TRY
    {
        // XXX Microsoft.
        status = E_NOTIMPL;
    }
    EX_CATCH
    {
        if (!DacExceptionFilter(GET_EXCEPTION(), m_dac, &status))
        {
            EX_RETHROW;
        }
    }
    EX_END_CATCH(SwallowAllExceptions)

    DAC_LEAVE();
    return status;
}

HRESULT STDMETHODCALLTYPE
ClrDataAssembly::EndEnumAppDomains(
    /* [in] */ CLRDATA_ENUM handle)
{
    HRESULT status;

    DAC_ENTER_SUB(m_dac);

    EX_TRY
    {
        // XXX Microsoft.
        status = E_NOTIMPL;
    }
    EX_CATCH
    {
        if (!DacExceptionFilter(GET_EXCEPTION(), m_dac, &status))
        {
            EX_RETHROW;
        }
    }
    EX_END_CATCH(SwallowAllExceptions)

    DAC_LEAVE();
    return status;
}

HRESULT STDMETHODCALLTYPE
ClrDataAssembly::GetName(
    /* [in] */ ULONG32 bufLen,
    /* [out] */ ULONG32 *nameLen,
    /* [size_is][out] */ __out_ecount_part_opt(bufLen, *nameLen) WCHAR name[  ])
{
    HRESULT status;

    DAC_ENTER_SUB(m_dac);

    EX_TRY
    {
        status = ConvertUtf8(m_assembly->GetSimpleName(),
                             bufLen, nameLen, name);
    }
    EX_CATCH
    {
        if (!DacExceptionFilter(GET_EXCEPTION(), m_dac, &status))
        {
            EX_RETHROW;
        }
    }
    EX_END_CATCH(SwallowAllExceptions)

    DAC_LEAVE();
    return status;
}

HRESULT STDMETHODCALLTYPE
ClrDataAssembly::GetFileName(
    /* [in] */ ULONG32 bufLen,
    /* [out] */ ULONG32 *nameLen,
    /* [size_is][out] */ __out_ecount_part_opt(bufLen, *nameLen) WCHAR name[  ])
{
    HRESULT status;

    DAC_ENTER_SUB(m_dac);

    EX_TRY
    {
        COUNT_T _nameLen;

        if (m_assembly->GetManifestFile()->GetPath().
            DacGetUnicode(bufLen, name, &_nameLen))
        {
            if (nameLen)
            {
                *nameLen = _nameLen;
            }
            status = S_OK;
        }
        else
        {
            status = E_FAIL;
        }
    }
    EX_CATCH
    {
        if (!DacExceptionFilter(GET_EXCEPTION(), m_dac, &status))
        {
            EX_RETHROW;
        }
    }
    EX_END_CATCH(SwallowAllExceptions)

    DAC_LEAVE();
    return status;
}

HRESULT STDMETHODCALLTYPE
ClrDataAssembly::GetDisplayName(
    /* [in] */ ULONG32 bufLen,
    /* [out] */ ULONG32 *nameLen,
    /* [size_is][out] */ __out_ecount_part_opt(bufLen, *nameLen) WCHAR name[  ])
{
    HRESULT status;

    DAC_ENTER_SUB(m_dac);

    EX_TRY
    {
        // XXX Microsoft.
        status = E_NOTIMPL;
    }
    EX_CATCH
    {
        if (!DacExceptionFilter(GET_EXCEPTION(), m_dac, &status))
        {
            EX_RETHROW;
        }
    }
    EX_END_CATCH(SwallowAllExceptions)

    DAC_LEAVE();
    return status;
}

HRESULT STDMETHODCALLTYPE
ClrDataAssembly::GetFlags(
    /* [out] */ ULONG32 *flags)
{
    HRESULT status;

    DAC_ENTER_SUB(m_dac);

    EX_TRY
    {
        *flags = CLRDATA_ASSEMBLY_DEFAULT;
        status = S_OK;
    }
    EX_CATCH
    {
        if (!DacExceptionFilter(GET_EXCEPTION(), m_dac, &status))
        {
            EX_RETHROW;
        }
    }
    EX_END_CATCH(SwallowAllExceptions)

    DAC_LEAVE();
    return status;
}

HRESULT STDMETHODCALLTYPE
ClrDataAssembly::IsSameObject(
    /* [in] */ IXCLRDataAssembly* assembly)
{
    HRESULT status;

    DAC_ENTER_SUB(m_dac);

    EX_TRY
    {
        status = (PTR_HOST_TO_TADDR(m_assembly) ==
                  PTR_HOST_TO_TADDR(((ClrDataAssembly*)assembly)->
                                    m_assembly)) ?
            S_OK : S_FALSE;
    }
    EX_CATCH
    {
        if (!DacExceptionFilter(GET_EXCEPTION(), m_dac, &status))
        {
            EX_RETHROW;
        }
    }
    EX_END_CATCH(SwallowAllExceptions)

    DAC_LEAVE();
    return status;
}

HRESULT STDMETHODCALLTYPE
ClrDataAssembly::Request(
    /* [in] */ ULONG32 reqCode,
    /* [in] */ ULONG32 inBufferSize,
    /* [size_is][in] */ BYTE *inBuffer,
    /* [in] */ ULONG32 outBufferSize,
    /* [size_is][out] */ BYTE *outBuffer)
{
    HRESULT status;

    DAC_ENTER_SUB(m_dac);

    EX_TRY
    {
        switch(reqCode)
        {
        case CLRDATA_REQUEST_REVISION:
            if (inBufferSize != 0 ||
                inBuffer ||
                outBufferSize != sizeof(ULONG32))
            {
                status = E_INVALIDARG;
            }
            else
            {
                *(ULONG32*)outBuffer = 2;
                status = S_OK;
            }
            break;

        default:
            status = E_INVALIDARG;
            break;
        }
    }
    EX_CATCH
    {
        if (!DacExceptionFilter(GET_EXCEPTION(), m_dac, &status))
        {
            EX_RETHROW;
        }
    }
    EX_END_CATCH(SwallowAllExceptions)

    DAC_LEAVE();
    return status;
}

//----------------------------------------------------------------------------
//
// ClrDataModule.
//
//----------------------------------------------------------------------------

ClrDataModule::ClrDataModule(ClrDataAccess* dac,
                             Module* module)
{
    m_dac = dac;
    m_dac->AddRef();
    m_instanceAge = m_dac->m_instanceAge;
    m_refs = 1;
    m_module = module;
    m_mdImport = NULL;
    m_setExtents = false;
}

ClrDataModule::~ClrDataModule(void)
{
    m_dac->Release();
    if (m_mdImport)
    {
        m_mdImport->Release();
    }
}

STDMETHODIMP
ClrDataModule::QueryInterface(THIS_
                              IN REFIID interfaceId,
                              OUT PVOID* iface)
{
    _ASSERTE(iface != NULL);

    if (IsEqualIID(interfaceId, IID_IUnknown) ||
        IsEqualIID(interfaceId, __uuidof(IXCLRDataModule)))
    {
        AddRef();
        *iface = static_cast<IUnknown*>
            (static_cast<IXCLRDataModule*>(this));
        return S_OK;
    }
    else if (IsEqualIID(interfaceId, __uuidof(IXCLRDataModule2)))
    {
        AddRef();
        *iface = static_cast<IUnknown*>
            (static_cast<IXCLRDataModule2*>(this));
        return S_OK;
    }
    else if (IsEqualIID(interfaceId, IID_IMetaDataImport))
    {
        return GetMdInterface(iface);
    }
    else
    {
        *iface = NULL;
        return E_NOINTERFACE;
    }
}

STDMETHODIMP_(ULONG)
ClrDataModule::AddRef(THIS)
{
    return InterlockedIncrement(&m_refs);
}

STDMETHODIMP_(ULONG)
ClrDataModule::Release(THIS)
{
    SUPPORTS_DAC_HOST_ONLY;
    LONG newRefs = InterlockedDecrement(&m_refs);
    if (newRefs == 0)
    {
        delete this;
    }
    return newRefs;
}

HRESULT STDMETHODCALLTYPE
ClrDataModule::StartEnumAssemblies(
    /* [out] */ CLRDATA_ENUM* handle)
{
    HRESULT status;

    DAC_ENTER_SUB(m_dac);

    EX_TRY
    {
        ProcessModIter* iter = new (nothrow) ProcessModIter;
        if (iter)
        {
            *handle = TO_CDENUM(iter);
            status = S_OK;
        }
        else
        {
            status = E_OUTOFMEMORY;
        }
    }
    EX_CATCH
    {
        if (!DacExceptionFilter(GET_EXCEPTION(), m_dac, &status))
        {
            EX_RETHROW;
        }
    }
    EX_END_CATCH(SwallowAllExceptions)

    DAC_LEAVE();
    return status;
}

HRESULT STDMETHODCALLTYPE
ClrDataModule::EnumAssembly(
    /* [in, out] */ CLRDATA_ENUM* handle,
    /* [out] */ IXCLRDataAssembly **assembly)
{
    HRESULT status;

    DAC_ENTER_SUB(m_dac);

    EX_TRY
    {
        ProcessModIter* iter = FROM_CDENUM(ProcessModIter, *handle);
        Module* module;

        //
        // Iterate over all of the modules in the process.
        // When this module is found, return the containing
        // assembly.
        // Is there a more direct way?
        //

        for (;;)
        {
            if (!(module = iter->NextModule()))
            {
                status = S_FALSE;
                break;
            }

            if (PTR_HOST_TO_TADDR(module) == PTR_HOST_TO_TADDR(m_module))
            {
                *assembly = new (nothrow)
                    ClrDataAssembly(m_dac, iter->m_curAssem);
                status = *assembly ? S_OK : E_OUTOFMEMORY;
                break;
            }
        }
    }
    EX_CATCH
    {
        if (!DacExceptionFilter(GET_EXCEPTION(), m_dac, &status))
        {
            EX_RETHROW;
        }
    }
    EX_END_CATCH(SwallowAllExceptions)

    DAC_LEAVE();
    return status;
}

HRESULT STDMETHODCALLTYPE
ClrDataModule::EndEnumAssemblies(
    /* [in] */ CLRDATA_ENUM handle)
{
    HRESULT status;

    DAC_ENTER_SUB(m_dac);

    EX_TRY
    {
        ProcessModIter* iter = FROM_CDENUM(ProcessModIter, handle);
        delete iter;
        status = S_OK;
    }
    EX_CATCH
    {
        if (!DacExceptionFilter(GET_EXCEPTION(), m_dac, &status))
        {
            EX_RETHROW;
        }
    }
    EX_END_CATCH(SwallowAllExceptions)

    DAC_LEAVE();
    return status;
}

HRESULT STDMETHODCALLTYPE
ClrDataModule::StartEnumAppDomains(
    /* [out] */ CLRDATA_ENUM* handle)
{
    HRESULT status;

    DAC_ENTER_SUB(m_dac);

    EX_TRY
    {
        // XXX Microsoft.
        status = E_NOTIMPL;
    }
    EX_CATCH
    {
        if (!DacExceptionFilter(GET_EXCEPTION(), m_dac, &status))
        {
            EX_RETHROW;
        }
    }
    EX_END_CATCH(SwallowAllExceptions)

    DAC_LEAVE();
    return status;
}

HRESULT STDMETHODCALLTYPE
ClrDataModule::EnumAppDomain(
    /* [in, out] */ CLRDATA_ENUM* handle,
    /* [out] */ IXCLRDataAppDomain **appDomain)
{
    HRESULT status;

    DAC_ENTER_SUB(m_dac);

    EX_TRY
    {
        // XXX Microsoft.
        status = E_NOTIMPL;
    }
    EX_CATCH
    {
        if (!DacExceptionFilter(GET_EXCEPTION(), m_dac, &status))
        {
            EX_RETHROW;
        }
    }
    EX_END_CATCH(SwallowAllExceptions)

    DAC_LEAVE();
    return status;
}

HRESULT STDMETHODCALLTYPE
ClrDataModule::EndEnumAppDomains(
    /* [in] */ CLRDATA_ENUM handle)
{
    HRESULT status;

    DAC_ENTER_SUB(m_dac);

    EX_TRY
    {
        // XXX Microsoft.
        status = E_NOTIMPL;
    }
    EX_CATCH
    {
        if (!DacExceptionFilter(GET_EXCEPTION(), m_dac, &status))
        {
            EX_RETHROW;
        }
    }
    EX_END_CATCH(SwallowAllExceptions)

    DAC_LEAVE();
    return status;
}

HRESULT STDMETHODCALLTYPE
ClrDataModule::StartEnumTypeDefinitions(
    /* [out] */ CLRDATA_ENUM* handle)
{
    HRESULT status;

    DAC_ENTER_SUB(m_dac);

    EX_TRY
    {
        status = MetaEnum::New(m_module,
                               mdtTypeDef,
                               0,
                               NULL,
                               NULL,
                               handle);
    }
    EX_CATCH
    {
        if (!DacExceptionFilter(GET_EXCEPTION(), m_dac, &status))
        {
            EX_RETHROW;
        }
    }
    EX_END_CATCH(SwallowAllExceptions)

    DAC_LEAVE();
    return status;
}

HRESULT STDMETHODCALLTYPE
ClrDataModule::EnumTypeDefinition(
    /* [in, out] */ CLRDATA_ENUM* handle,
    /* [out] */ IXCLRDataTypeDefinition **typeDefinition)
{
    HRESULT status;

    DAC_ENTER_SUB(m_dac);

    EX_TRY
    {
        mdTypeDef token;

        if ((status = MetaEnum::CdNextToken(handle, &token)) == S_OK)
        {
            status = ClrDataTypeDefinition::
                NewFromModule(m_dac,
                              m_module,
                              token,
                              NULL,
                              typeDefinition);
        }
    }
    EX_CATCH
    {
        if (!DacExceptionFilter(GET_EXCEPTION(), m_dac, &status))
        {
            EX_RETHROW;
        }
    }
    EX_END_CATCH(SwallowAllExceptions)

    DAC_LEAVE();
    return status;
}

HRESULT STDMETHODCALLTYPE
ClrDataModule::EndEnumTypeDefinitions(
    /* [in] */ CLRDATA_ENUM handle)
{
    HRESULT status;

    DAC_ENTER_SUB(m_dac);

    EX_TRY
    {
        status = MetaEnum::CdEnd(handle);
    }
    EX_CATCH
    {
        if (!DacExceptionFilter(GET_EXCEPTION(), m_dac, &status))
        {
            EX_RETHROW;
        }
    }
    EX_END_CATCH(SwallowAllExceptions)

    DAC_LEAVE();
    return status;
}

HRESULT STDMETHODCALLTYPE
ClrDataModule::StartEnumTypeInstances(
    /* [in] */ IXCLRDataAppDomain* appDomain,
    /* [out] */ CLRDATA_ENUM* handle)
{
    HRESULT status;

    DAC_ENTER_SUB(m_dac);

    EX_TRY
    {
        status = MetaEnum::New(m_module,
                               mdtTypeDef,
                               0,
                               appDomain,
                               NULL,
                               handle);
    }
    EX_CATCH
    {
        if (!DacExceptionFilter(GET_EXCEPTION(), m_dac, &status))
        {
            EX_RETHROW;
        }
    }
    EX_END_CATCH(SwallowAllExceptions)

    DAC_LEAVE();
    return status;
}

HRESULT STDMETHODCALLTYPE
ClrDataModule::EnumTypeInstance(
    /* [in, out] */ CLRDATA_ENUM* handle,
    /* [out] */ IXCLRDataTypeInstance **typeInstance)
{
    HRESULT status;

    DAC_ENTER_SUB(m_dac);

    EX_TRY
    {
        for (;;)
        {
            AppDomain* appDomain;
            mdTypeDef token;

            if ((status = MetaEnum::
                 CdNextDomainToken(handle, &appDomain, &token)) != S_OK)
            {
                break;
            }

            // If the type hasn't been used there won't be anything
            // loaded.  It's not an instance, then, just keep going.
            if ((status = ClrDataTypeInstance::
                 NewFromModule(m_dac,
                               appDomain,
                               m_module,
                               token,
                               NULL,
                               typeInstance)) != E_INVALIDARG)
            {
                break;
            }
        }
    }
    EX_CATCH
    {
        if (!DacExceptionFilter(GET_EXCEPTION(), m_dac, &status))
        {
            EX_RETHROW;
        }
    }
    EX_END_CATCH(SwallowAllExceptions)

    DAC_LEAVE();
    return status;
}

HRESULT STDMETHODCALLTYPE
ClrDataModule::EndEnumTypeInstances(
    /* [in] */ CLRDATA_ENUM handle)
{
    HRESULT status;

    DAC_ENTER_SUB(m_dac);

    EX_TRY
    {
        status = MetaEnum::CdEnd(handle);
    }
    EX_CATCH
    {
        if (!DacExceptionFilter(GET_EXCEPTION(), m_dac, &status))
        {
            EX_RETHROW;
        }
    }
    EX_END_CATCH(SwallowAllExceptions)

    DAC_LEAVE();
    return status;
}

HRESULT STDMETHODCALLTYPE
ClrDataModule::StartEnumTypeDefinitionsByName(
    /* [in] */ LPCWSTR name,
    /* [in] */ ULONG32 flags,
    /* [out] */ CLRDATA_ENUM* handle)
{
    HRESULT status;

    DAC_ENTER_SUB(m_dac);

    EX_TRY
    {
        status = SplitName::CdStartType(name,
                                        flags,
                                        m_module,
                                        NULL,
                                        NULL,
                                        NULL,
                                        handle);
    }
    EX_CATCH
    {
        if (!DacExceptionFilter(GET_EXCEPTION(), m_dac, &status))
        {
            EX_RETHROW;
        }
    }
    EX_END_CATCH(SwallowAllExceptions)

    DAC_LEAVE();
    return status;
}

HRESULT STDMETHODCALLTYPE
ClrDataModule::EnumTypeDefinitionByName(
    /* [out][in] */ CLRDATA_ENUM* handle,
    /* [out] */ IXCLRDataTypeDefinition **type)
{
    HRESULT status;

    DAC_ENTER_SUB(m_dac);

    EX_TRY
    {
        mdTypeDef token;

        if ((status = SplitName::CdNextType(handle, &token)) == S_OK)
        {
            status = ClrDataTypeDefinition::
                NewFromModule(m_dac,
                              m_module,
                              token,
                              NULL,
                              type);
        }
    }
    EX_CATCH
    {
        if (!DacExceptionFilter(GET_EXCEPTION(), m_dac, &status))
        {
            EX_RETHROW;
        }
    }
    EX_END_CATCH(SwallowAllExceptions)

    DAC_LEAVE();
    return status;
}

HRESULT STDMETHODCALLTYPE
ClrDataModule::EndEnumTypeDefinitionsByName(
    /* [in] */ CLRDATA_ENUM handle)
{
    HRESULT status;

    DAC_ENTER_SUB(m_dac);

    EX_TRY
    {
        status = SplitName::CdEnd(handle);
    }
    EX_CATCH
    {
        if (!DacExceptionFilter(GET_EXCEPTION(), m_dac, &status))
        {
            EX_RETHROW;
        }
    }
    EX_END_CATCH(SwallowAllExceptions)

    DAC_LEAVE();
    return status;
}

HRESULT STDMETHODCALLTYPE
ClrDataModule::StartEnumTypeInstancesByName(
    /* [in] */ LPCWSTR name,
    /* [in] */ ULONG32 flags,
    /* [in] */ IXCLRDataAppDomain *appDomain,
    /* [out] */ CLRDATA_ENUM* handle)
{
    HRESULT status;

    DAC_ENTER_SUB(m_dac);

    EX_TRY
    {
        status = SplitName::CdStartType(name,
                                        flags,
                                        m_module,
                                        NULL,
                                        appDomain,
                                        NULL,
                                        handle);
    }
    EX_CATCH
    {
        if (!DacExceptionFilter(GET_EXCEPTION(), m_dac, &status))
        {
            EX_RETHROW;
        }
    }
    EX_END_CATCH(SwallowAllExceptions)

    DAC_LEAVE();
    return status;
}

HRESULT STDMETHODCALLTYPE
ClrDataModule::EnumTypeInstanceByName(
    /* [out][in] */ CLRDATA_ENUM* handle,
    /* [out] */ IXCLRDataTypeInstance **type)
{
    HRESULT status;

    DAC_ENTER_SUB(m_dac);

    EX_TRY
    {
        SplitName* split; split = FROM_CDENUM(SplitName, *handle);

        for (;;)
        {
            AppDomain* appDomain;
            mdTypeDef token;

            if ((status = SplitName::
                 CdNextDomainType(handle, &appDomain, &token)) != S_OK)
            {
                break;
            }

            // If the type hasn't been used there won't be anything
            // loaded.  It's not an instance, then, just keep going.
            if ((status = ClrDataTypeInstance::
                 NewFromModule(m_dac,
                               appDomain,
                               m_module,
                               token,
                               NULL,
                               type)) != E_INVALIDARG)
            {
                break;
            }
        }
    }
    EX_CATCH
    {
        if (!DacExceptionFilter(GET_EXCEPTION(), m_dac, &status))
        {
            EX_RETHROW;
        }
    }
    EX_END_CATCH(SwallowAllExceptions)

    DAC_LEAVE();
    return status;
}

HRESULT STDMETHODCALLTYPE
ClrDataModule::EndEnumTypeInstancesByName(
    /* [in] */ CLRDATA_ENUM handle)
{
    HRESULT status;

    DAC_ENTER_SUB(m_dac);

    EX_TRY
    {
        status = SplitName::CdEnd(handle);
    }
    EX_CATCH
    {
        if (!DacExceptionFilter(GET_EXCEPTION(), m_dac, &status))
        {
            EX_RETHROW;
        }
    }
    EX_END_CATCH(SwallowAllExceptions)

    DAC_LEAVE();
    return status;
}

HRESULT STDMETHODCALLTYPE
ClrDataModule::GetTypeDefinitionByToken(
    /* [in] */ mdTypeDef token,
    /* [out] */ IXCLRDataTypeDefinition **typeDefinition)
{
    HRESULT status;

    // This isn't critically necessary but it prevents
    // an assert in the metadata code.
    if (TypeFromToken(token) != mdtTypeDef)
    {
        return E_INVALIDARG;
    }

    DAC_ENTER_SUB(m_dac);

    EX_TRY
    {
        status = ClrDataTypeDefinition::
            NewFromModule(m_dac,
                          m_module,
                          token,
                          NULL,
                          typeDefinition);
    }
    EX_CATCH
    {
        if (!DacExceptionFilter(GET_EXCEPTION(), m_dac, &status))
        {
            EX_RETHROW;
        }
    }
    EX_END_CATCH(SwallowAllExceptions)

    DAC_LEAVE();
    return status;
}

HRESULT STDMETHODCALLTYPE
ClrDataModule::StartEnumMethodDefinitionsByName(
    /* [in] */ LPCWSTR name,
    /* [in] */ ULONG32 flags,
    /* [out] */ CLRDATA_ENUM* handle)
{
    HRESULT status;

    DAC_ENTER_SUB(m_dac);

    EX_TRY
    {
        status = SplitName::CdStartMethod(name,
                                          flags,
                                          m_module,
                                          mdTypeDefNil,
                                          NULL,
                                          NULL,
                                          NULL,
                                          handle);
    }
    EX_CATCH
    {
        if (!DacExceptionFilter(GET_EXCEPTION(), m_dac, &status))
        {
            EX_RETHROW;
        }
    }
    EX_END_CATCH(SwallowAllExceptions)

    DAC_LEAVE();
    return status;
}

HRESULT STDMETHODCALLTYPE
ClrDataModule::EnumMethodDefinitionByName(
    /* [out][in] */ CLRDATA_ENUM* handle,
    /* [out] */ IXCLRDataMethodDefinition **method)
{
    HRESULT status;

    DAC_ENTER_SUB(m_dac);

    EX_TRY
    {
        mdMethodDef token;

        if ((status = SplitName::CdNextMethod(handle, &token)) == S_OK)
        {
            status = ClrDataMethodDefinition::
                NewFromModule(m_dac,
                              m_module,
                              token,
                              NULL,
                              method);
        }
    }
    EX_CATCH
    {
        if (!DacExceptionFilter(GET_EXCEPTION(), m_dac, &status))
        {
            EX_RETHROW;
        }
    }
    EX_END_CATCH(SwallowAllExceptions)

    DAC_LEAVE();
    return status;
}

HRESULT STDMETHODCALLTYPE
ClrDataModule::EndEnumMethodDefinitionsByName(
    /* [in] */ CLRDATA_ENUM handle)
{
    HRESULT status;

    DAC_ENTER_SUB(m_dac);

    EX_TRY
    {
        status = SplitName::CdEnd(handle);
    }
    EX_CATCH
    {
        if (!DacExceptionFilter(GET_EXCEPTION(), m_dac, &status))
        {
            EX_RETHROW;
        }
    }
    EX_END_CATCH(SwallowAllExceptions)

    DAC_LEAVE();
    return status;
}

HRESULT STDMETHODCALLTYPE
ClrDataModule::StartEnumMethodInstancesByName(
    /* [in] */ LPCWSTR name,
    /* [in] */ ULONG32 flags,
    /* [in] */ IXCLRDataAppDomain* appDomain,
    /* [out] */ CLRDATA_ENUM* handle)
{
    HRESULT status;

    DAC_ENTER_SUB(m_dac);

    EX_TRY
    {
        status = SplitName::CdStartMethod(name,
                                          flags,
                                          m_module,
                                          mdTypeDefNil,
                                          NULL,
                                          appDomain,
                                          NULL,
                                          handle);
    }
    EX_CATCH
    {
        if (!DacExceptionFilter(GET_EXCEPTION(), m_dac, &status))
        {
            EX_RETHROW;
        }
    }
    EX_END_CATCH(SwallowAllExceptions)

    DAC_LEAVE();
    return status;
}

HRESULT STDMETHODCALLTYPE
ClrDataModule::EnumMethodInstanceByName(
    /* [out][in] */ CLRDATA_ENUM* handle,
    /* [out] */ IXCLRDataMethodInstance **method)
{
    HRESULT status;

    DAC_ENTER_SUB(m_dac);

    EX_TRY
    {
        SplitName* split; split = FROM_CDENUM(SplitName, *handle);

        for (;;)
        {
            AppDomain* appDomain;
            mdMethodDef token;

            if ((status = SplitName::
                 CdNextDomainMethod(handle, &appDomain, &token)) != S_OK)
            {
                break;
            }

            // If the method doesn't have a MethodDesc or hasn't
            // been JIT'ed yet it's not an instance and should
            // just be skipped.
            if ((status = ClrDataMethodInstance::
                 NewFromModule(m_dac,
                               appDomain,
                               m_module,
                               token,
                               NULL,
                               method)) != E_INVALIDARG)
            {
                break;
            }
        }
    }
    EX_CATCH
    {
        if (!DacExceptionFilter(GET_EXCEPTION(), m_dac, &status))
        {
            EX_RETHROW;
        }
    }
    EX_END_CATCH(SwallowAllExceptions)

    DAC_LEAVE();
    return status;
}

HRESULT STDMETHODCALLTYPE
ClrDataModule::EndEnumMethodInstancesByName(
    /* [in] */ CLRDATA_ENUM handle)
{
    HRESULT status;

    DAC_ENTER_SUB(m_dac);

    EX_TRY
    {
        status = SplitName::CdEnd(handle);
    }
    EX_CATCH
    {
        if (!DacExceptionFilter(GET_EXCEPTION(), m_dac, &status))
        {
            EX_RETHROW;
        }
    }
    EX_END_CATCH(SwallowAllExceptions)

    DAC_LEAVE();
    return status;
}

HRESULT STDMETHODCALLTYPE
ClrDataModule::GetMethodDefinitionByToken(
    /* [in] */ mdMethodDef token,
    /* [out] */ IXCLRDataMethodDefinition **methodDefinition)
{
    HRESULT status;

    // This isn't critically necessary but it prevents
    // an assert in the metadata code.
    if (TypeFromToken(token) != mdtMethodDef)
    {
        return E_INVALIDARG;
    }

    DAC_ENTER_SUB(m_dac);

    EX_TRY
    {
        status = ClrDataMethodDefinition::
            NewFromModule(m_dac,
                          m_module,
                          token,
                          NULL,
                          methodDefinition);
    }
    EX_CATCH
    {
        if (!DacExceptionFilter(GET_EXCEPTION(), m_dac, &status))
        {
            EX_RETHROW;
        }
    }
    EX_END_CATCH(SwallowAllExceptions)

    DAC_LEAVE();
    return status;
}

HRESULT STDMETHODCALLTYPE
ClrDataModule::StartEnumDataByName(
    /* [in] */ LPCWSTR name,
    /* [in] */ ULONG32 flags,
    /* [in] */ IXCLRDataAppDomain* appDomain,
    /* [in] */ IXCLRDataTask* tlsTask,
    /* [out] */ CLRDATA_ENUM* handle)
{
    HRESULT status;

    DAC_ENTER_SUB(m_dac);

    EX_TRY
    {
        status = SplitName::CdStartField(name,
                                         flags,
                                         INH_STATIC,
                                         NULL,
                                         TypeHandle(),
                                         m_module,
                                         mdTypeDefNil,
                                         0,
                                         NULL,
                                         tlsTask,
                                         NULL,
                                         appDomain,
                                         NULL,
                                         handle);
    }
    EX_CATCH
    {
        if (!DacExceptionFilter(GET_EXCEPTION(), m_dac, &status))
        {
            EX_RETHROW;
        }
    }
    EX_END_CATCH(SwallowAllExceptions)

    DAC_LEAVE();
    return status;
}

HRESULT STDMETHODCALLTYPE
ClrDataModule::EnumDataByName(
    /* [out][in] */ CLRDATA_ENUM* handle,
    /* [out] */ IXCLRDataValue **value)
{
    HRESULT status;

    DAC_ENTER_SUB(m_dac);

    EX_TRY
    {
        status = SplitName::CdNextDomainField(m_dac, handle, value);
    }
    EX_CATCH
    {
        if (!DacExceptionFilter(GET_EXCEPTION(), m_dac, &status))
        {
            EX_RETHROW;
        }
    }
    EX_END_CATCH(SwallowAllExceptions)

    DAC_LEAVE();
    return status;
}

HRESULT STDMETHODCALLTYPE
ClrDataModule::EndEnumDataByName(
    /* [in] */ CLRDATA_ENUM handle)
{
    HRESULT status;

    DAC_ENTER_SUB(m_dac);

    EX_TRY
    {
        status = SplitName::CdEnd(handle);
    }
    EX_CATCH
    {
        if (!DacExceptionFilter(GET_EXCEPTION(), m_dac, &status))
        {
            EX_RETHROW;
        }
    }
    EX_END_CATCH(SwallowAllExceptions)

    DAC_LEAVE();
    return status;
}

HRESULT STDMETHODCALLTYPE
ClrDataModule::GetName(
    /* [in] */ ULONG32 bufLen,
    /* [out] */ ULONG32 *nameLen,
    /* [size_is][out] */ __out_ecount_part_opt(bufLen, *nameLen) WCHAR name[  ])
{
    HRESULT status;

    DAC_ENTER_SUB(m_dac);

    EX_TRY
    {
        status = ConvertUtf8(m_module->GetSimpleName(),
                             bufLen, nameLen, name);
    }
    EX_CATCH
    {
        if (!DacExceptionFilter(GET_EXCEPTION(), m_dac, &status))
        {
            EX_RETHROW;
        }
    }
    EX_END_CATCH(SwallowAllExceptions)

    DAC_LEAVE();
    return status;
}

HRESULT STDMETHODCALLTYPE
ClrDataModule::GetFileName(
    /* [in] */ ULONG32 bufLen,
    /* [out] */ ULONG32 *nameLen,
    /* [size_is][out] */ __out_ecount_part(bufLen, *nameLen) WCHAR name[  ])
{
    HRESULT status;

    DAC_ENTER_SUB(m_dac);

    EX_TRY
    {
        COUNT_T _nameLen;

        // Try to get the file name through GetPath.
        // If the returned name is empty, then try to get the guessed module file name.
        // The guessed file name is propogated from metadata's module name.
        //
        if ((m_module->GetFile()->GetPath().DacGetUnicode(bufLen, name, &_nameLen) && name[0])||
            (m_module->GetFile()->GetModuleFileNameHint().DacGetUnicode(bufLen, name, &_nameLen) && name[0]))
        {
            if (nameLen)
            {
                *nameLen = _nameLen;
            }
            status = S_OK;
        }
        else
        {
            status = E_FAIL;
        }
    }
    EX_CATCH
    {
        if (!DacExceptionFilter(GET_EXCEPTION(), m_dac, &status))
        {
            EX_RETHROW;
        }
    }
    EX_END_CATCH(SwallowAllExceptions)

    DAC_LEAVE();
    return status;
}

HRESULT STDMETHODCALLTYPE
ClrDataModule::GetVersionId(
    /* [out] */ GUID* vid)
{
    HRESULT status;

    DAC_ENTER_SUB(m_dac);

    EX_TRY
    {
        if (!m_module->GetFile()->HasMetadata())
        {
            status = E_NOINTERFACE;
        }
        else
        {
            GUID mdVid;

            status = m_module->GetMDImport()->GetScopeProps(NULL, &mdVid);
            if (SUCCEEDED(status))
            {
                *vid = mdVid;
            }
        }
    }
    EX_CATCH
    {
        if (!DacExceptionFilter(GET_EXCEPTION(), m_dac, &status))
        {
            EX_RETHROW;
        }
    }
    EX_END_CATCH(SwallowAllExceptions)

    DAC_LEAVE();
    return status;
}

HRESULT STDMETHODCALLTYPE
ClrDataModule::GetFlags(
    /* [out] */ ULONG32 *flags)
{
    HRESULT status;

    DAC_ENTER_SUB(m_dac);

    EX_TRY
    {
        *flags = 0;

        if (m_module->IsReflection())
        {
            (*flags) |= CLRDATA_MODULE_IS_DYNAMIC;
        }
        if (m_module->IsIStream())
        {
            (*flags) |= CLRDATA_MODULE_IS_MEMORY_STREAM;
        }
        PTR_Assembly pAssembly = m_module->GetAssembly();
        PTR_BaseDomain pBaseDomain = pAssembly->GetDomain();
        if (pBaseDomain->IsAppDomain())
        {
            AppDomain* pAppDomain = pBaseDomain->AsAppDomain();
            if (pAssembly == pAppDomain->GetRootAssembly())
            {
                (*flags) |= CLRDATA_MODULE_IS_MAIN_MODULE;
            }
        }
        status = S_OK;
    }
    EX_CATCH
    {
        if (!DacExceptionFilter(GET_EXCEPTION(), m_dac, &status))
        {
            EX_RETHROW;
        }
    }
    EX_END_CATCH(SwallowAllExceptions)

    DAC_LEAVE();
    return status;
}

HRESULT STDMETHODCALLTYPE
ClrDataModule::IsSameObject(
    /* [in] */ IXCLRDataModule* mod)
{
    HRESULT status;

    DAC_ENTER_SUB(m_dac);

    EX_TRY
    {
        status = (PTR_HOST_TO_TADDR(m_module) ==
                  PTR_HOST_TO_TADDR(((ClrDataModule*)mod)->
                                    m_module)) ?
            S_OK : S_FALSE;
    }
    EX_CATCH
    {
        if (!DacExceptionFilter(GET_EXCEPTION(), m_dac, &status))
        {
            EX_RETHROW;
        }
    }
    EX_END_CATCH(SwallowAllExceptions)

    DAC_LEAVE();
    return status;
}

HRESULT STDMETHODCALLTYPE
ClrDataModule::StartEnumExtents(
    /* [out] */ CLRDATA_ENUM* handle)
{
    HRESULT status;

    DAC_ENTER_SUB(m_dac);

    EX_TRY
    {
        if (!m_setExtents)
        {
            PEFile* file = m_module->GetFile();
            if (!file)
            {
                *handle = 0;
                status = E_INVALIDARG;
                goto Exit;
            }

            CLRDATA_MODULE_EXTENT* extent = m_extents;

            if (file->GetLoadedImageContents() != NULL)
            {
                extent->base =
                    TO_CDADDR( PTR_TO_TADDR(file->GetLoadedImageContents(&extent->length)) );
                extent->type = CLRDATA_MODULE_PE_FILE;
                extent++;
            }

            m_setExtents = true;
            m_extentsEnd = extent;
        }

        *handle = TO_CDENUM(m_extents);
        status = m_extents != m_extentsEnd ? S_OK : S_FALSE;

    Exit: ;
    }
    EX_CATCH
    {
        if (!DacExceptionFilter(GET_EXCEPTION(), m_dac, &status))
        {
            EX_RETHROW;
        }
    }
    EX_END_CATCH(SwallowAllExceptions)

    DAC_LEAVE();
    return status;
}

HRESULT STDMETHODCALLTYPE
ClrDataModule::EnumExtent(
    /* [in, out] */ CLRDATA_ENUM* handle,
    /* [out] */ CLRDATA_MODULE_EXTENT *extent)
{
    HRESULT status;

    DAC_ENTER_SUB(m_dac);

    EX_TRY
    {
        CLRDATA_MODULE_EXTENT* curExtent =
            FROM_CDENUM(CLRDATA_MODULE_EXTENT, *handle);
        if (!m_setExtents ||
            curExtent < m_extents ||
            curExtent > m_extentsEnd)
        {
            status = E_INVALIDARG;
        }
        else if (curExtent < m_extentsEnd)
        {
            *extent = *curExtent++;
            *handle = TO_CDENUM(curExtent);
            status = S_OK;
        }
        else
        {
            status = S_FALSE;
        }
    }
    EX_CATCH
    {
        if (!DacExceptionFilter(GET_EXCEPTION(), m_dac, &status))
        {
            EX_RETHROW;
        }
    }
    EX_END_CATCH(SwallowAllExceptions)

    DAC_LEAVE();
    return status;
}

HRESULT STDMETHODCALLTYPE
ClrDataModule::EndEnumExtents(
    /* [in] */ CLRDATA_ENUM handle)
{
    HRESULT status;

    DAC_ENTER_SUB(m_dac);

    EX_TRY
    {
        // Enumerator holds no resources.
        status = S_OK;
    }
    EX_CATCH
    {
        if (!DacExceptionFilter(GET_EXCEPTION(), m_dac, &status))
        {
            EX_RETHROW;
        }
    }
    EX_END_CATCH(SwallowAllExceptions)

    DAC_LEAVE();
    return status;
}

HRESULT
ClrDataModule::RequestGetModulePtr(
    /* [in] */ ULONG32 inBufferSize,
    /* [size_is][in] */ BYTE *inBuffer,
    /* [in] */ ULONG32 outBufferSize,
    /* [size_is][out] */ BYTE *outBuffer)
{
    // Validate params.
    // Input: Nothing.
    // Output: a DacpGetModuleAddress structure.
    if ((inBufferSize != 0) ||
        (inBuffer != NULL) ||
        (outBufferSize != sizeof(DacpGetModuleAddress)) ||
        (outBuffer == NULL))
    {
        return E_INVALIDARG;
    }

    DacpGetModuleAddress * outGMA = reinterpret_cast<DacpGetModuleAddress *> (outBuffer);

    outGMA->ModulePtr = TO_CDADDR(PTR_HOST_TO_TADDR(m_module));
    return S_OK;
}

HRESULT
ClrDataModule::RequestGetModuleData(
    /* [in] */ ULONG32 inBufferSize,
    /* [size_is][in] */ BYTE *inBuffer,
    /* [in] */ ULONG32 outBufferSize,
    /* [size_is][out] */ BYTE *outBuffer)
{
    // Validate params.
    // Input: Nothing.
    // Output: a DacpGetModuleData structure.
    if ((inBufferSize != 0) ||
        (inBuffer != NULL) ||
        (outBufferSize != sizeof(DacpGetModuleData)) ||
        (outBuffer == NULL))
    {
        return E_INVALIDARG;
    }

    DacpGetModuleData * outGMD = reinterpret_cast<DacpGetModuleData *>(outBuffer);
    ZeroMemory(outGMD, sizeof(DacpGetModuleData));

    Module* pModule = GetModule();
    PEFile *pPEFile = pModule->GetFile();

    outGMD->PEFile = TO_CDADDR(PTR_HOST_TO_TADDR(pPEFile));
    outGMD->IsDynamic = pModule->IsReflection();

    if (pPEFile != NULL)
    {
        outGMD->IsInMemory = pPEFile->GetPath().IsEmpty();

        COUNT_T peSize;
        outGMD->LoadedPEAddress = TO_CDADDR(PTR_TO_TADDR(pPEFile->GetLoadedImageContents(&peSize)));
        outGMD->LoadedPESize = (ULONG64)peSize;

        // Can not get the file layout for a dynamic module
        if (!outGMD->IsDynamic)
        {
            outGMD->IsFileLayout = pPEFile->GetLoaded()->IsFlat();
        }
    }

    // If there is a in memory symbol stream
    CGrowableStream* stream = pModule->GetInMemorySymbolStream();
    if (stream != NULL)
    {
        // Save the in-memory PDB address and size
        MemoryRange range = stream->GetRawBuffer();
        outGMD->InMemoryPdbAddress = TO_CDADDR(PTR_TO_TADDR(range.StartAddress()));
        outGMD->InMemoryPdbSize = range.Size();
    }

    return S_OK;
}

HRESULT STDMETHODCALLTYPE
ClrDataModule::Request(
    /* [in] */ ULONG32 reqCode,
    /* [in] */ ULONG32 inBufferSize,
    /* [size_is][in] */ BYTE *inBuffer,
    /* [in] */ ULONG32 outBufferSize,
    /* [size_is][out] */ BYTE *outBuffer)
{
    HRESULT status;

    DAC_ENTER_SUB(m_dac);

    EX_TRY
    {
        switch(reqCode)
        {
        case CLRDATA_REQUEST_REVISION:
            if (inBufferSize != 0 ||
                inBuffer ||
                outBufferSize != sizeof(ULONG32))
            {
                status = E_INVALIDARG;
            }
            else
            {
                *(ULONG32*)outBuffer = 3;
                status = S_OK;
            }
            break;

        case DACDATAMODULEPRIV_REQUEST_GET_MODULEPTR:
            status = RequestGetModulePtr(inBufferSize, inBuffer, outBufferSize, outBuffer);
            break;

        case DACDATAMODULEPRIV_REQUEST_GET_MODULEDATA:
            status = RequestGetModuleData(inBufferSize, inBuffer, outBufferSize, outBuffer);
            break;

        default:
            status = E_INVALIDARG;
            break;
        }
    }
    EX_CATCH
    {
        if (!DacExceptionFilter(GET_EXCEPTION(), m_dac, &status))
        {
            EX_RETHROW;
        }
    }
    EX_END_CATCH(SwallowAllExceptions)

    DAC_LEAVE();
    return status;
}

HRESULT STDMETHODCALLTYPE
ClrDataModule::SetJITCompilerFlags(
    /* [in] */ DWORD dwFlags)
{
    // Note: this is similar but not equivalent to the DacDbi version of this function
    HRESULT hr = S_OK;
    DAC_ENTER_SUB(m_dac);

    EX_TRY
    {
        // can't have a subset of these, eg 0x101, so make sure we have an exact match
        if ((dwFlags != CORDEBUG_JIT_DEFAULT) &&
            (dwFlags != CORDEBUG_JIT_DISABLE_OPTIMIZATION))
        {
            hr = E_INVALIDARG;
        }
        else
        {
            _ASSERTE(m_module != NULL);

            BOOL fAllowJitOpts = ((dwFlags & CORDEBUG_JIT_DISABLE_OPTIMIZATION) != CORDEBUG_JIT_DISABLE_OPTIMIZATION);

            // Initialize dwBits.
            DWORD dwBits = (m_module->GetDebuggerInfoBits() & ~(DACF_ALLOW_JIT_OPTS | DACF_ENC_ENABLED));
            dwBits &= DACF_CONTROL_FLAGS_MASK;

            if (fAllowJitOpts)
            {
                dwBits |= DACF_ALLOW_JIT_OPTS;
            }

            // Settings from the debugger take precedence over all other settings.
            dwBits |= DACF_USER_OVERRIDE;

            // set flags. This will write back to the target
            m_module->SetDebuggerInfoBits((DebuggerAssemblyControlFlags)dwBits);

            _ASSERTE(SUCCEEDED(hr));
        }
    }
    EX_CATCH
    {
        if (!DacExceptionFilter(GET_EXCEPTION(), m_dac, &hr))
        {
            EX_RETHROW;
        }
    }
    EX_END_CATCH(SwallowAllExceptions)

    DAC_LEAVE();
    return hr;
}

HRESULT
ClrDataModule::GetMdInterface(PVOID* retIface)
{
    HRESULT status;

    DAC_ENTER_SUB(m_dac);

    EX_TRY
    {
        if (m_mdImport == NULL)
        {
            if (!m_module->GetFile()->HasMetadata())
            {
                status = E_NOINTERFACE;
                goto Exit;
            }

            //
            // Make sure internal MD is in RW format.
            //
            IMDInternalImport* rwMd;

            status = ConvertMDInternalImport(m_module->GetMDImport(), &rwMd);
            if (FAILED(status))
            {
                goto Exit;
            }

            // If no conversion took place the same interface was
            // was returned without an AddRef.  AddRef now so
            // that rwMd has a reference either way.
            if (status == S_FALSE)
            {
                rwMd->AddRef();
            }

            status = GetMDPublicInterfaceFromInternal((PVOID)rwMd,
                                                      IID_IMetaDataImport,
                                                      (PVOID*)&m_mdImport);

            rwMd->Release();

            if (status != S_OK)
            {
                goto Exit;
            }
        }

        _ASSERTE(m_mdImport != NULL);
        m_mdImport->AddRef();
        *retIface = m_mdImport;
        status = S_OK;

    Exit: ;
    }
    EX_CATCH
    {
        if (!DacExceptionFilter(GET_EXCEPTION(), m_dac, &status))
        {
            EX_RETHROW;
        }
    }
    EX_END_CATCH(SwallowAllExceptions)

    DAC_LEAVE();
    return status;
}

//----------------------------------------------------------------------------
//
// ClrDataMethodDefinition.
//
//----------------------------------------------------------------------------

ClrDataMethodDefinition::ClrDataMethodDefinition(ClrDataAccess* dac,
                                                 Module* module,
                                                 mdMethodDef token,
                                                 MethodDesc* methodDesc)
{
    m_dac = dac;
    m_dac->AddRef();
    m_instanceAge = m_dac->m_instanceAge;
    m_refs = 1;
    m_module = module;
    m_token = token;
    m_methodDesc = methodDesc;
}

ClrDataMethodDefinition::~ClrDataMethodDefinition(void)
{
    m_dac->Release();
}

STDMETHODIMP
ClrDataMethodDefinition::QueryInterface(THIS_
                                        IN REFIID interfaceId,
                                        OUT PVOID* iface)
{
    if (IsEqualIID(interfaceId, IID_IUnknown) ||
        IsEqualIID(interfaceId, __uuidof(IXCLRDataMethodDefinition)))
    {
        AddRef();
        *iface = static_cast<IUnknown*>
            (static_cast<IXCLRDataMethodDefinition*>(this));
        return S_OK;
    }
    else
    {
        *iface = NULL;
        return E_NOINTERFACE;
    }
}

STDMETHODIMP_(ULONG)
ClrDataMethodDefinition::AddRef(THIS)
{
    return InterlockedIncrement(&m_refs);
}

STDMETHODIMP_(ULONG)
ClrDataMethodDefinition::Release(THIS)
{
    SUPPORTS_DAC_HOST_ONLY;
    LONG newRefs = InterlockedDecrement(&m_refs);
    if (newRefs == 0)
    {
        delete this;
    }
    return newRefs;
}

HRESULT STDMETHODCALLTYPE
ClrDataMethodDefinition::GetTypeDefinition(
    /* [out] */ IXCLRDataTypeDefinition **typeDefinition)
{
    HRESULT status;

    DAC_ENTER_SUB(m_dac);

    EX_TRY
    {
        TypeHandle typeHandle;
        mdTypeDef token;

        if (m_methodDesc)
        {
            typeHandle = TypeHandle(m_methodDesc->GetMethodTable());
            token = typeHandle.GetMethodTable()->GetCl();
        }
        else
        {
            if ((status = m_module->GetMDImport()->
                 GetParentToken(m_token, &token)) != S_OK)
            {
                goto Exit;
            }
        }

        *typeDefinition = new (nothrow)
            ClrDataTypeDefinition(m_dac, m_module, token, typeHandle);
        status = *typeDefinition ? S_OK : E_OUTOFMEMORY;

    Exit: ;
    }
    EX_CATCH
    {
        if (!DacExceptionFilter(GET_EXCEPTION(), m_dac, &status))
        {
            EX_RETHROW;
        }
    }
    EX_END_CATCH(SwallowAllExceptions)

    DAC_LEAVE();
    return status;
}

HRESULT STDMETHODCALLTYPE
ClrDataMethodDefinition::StartEnumInstances(
    /* [in] */ IXCLRDataAppDomain* appDomain,
    /* [out] */ CLRDATA_ENUM *handle)
{
    HRESULT status;

    DAC_ENTER_SUB(m_dac);

    EX_TRY
    {
        if (m_methodDesc)
        {
            status = EnumMethodInstances::CdStart(m_methodDesc, appDomain,
                                                  handle);
        }
        else
        {
            status = S_FALSE;
            *handle = 0;
        }
    }
    EX_CATCH
    {
        if (!DacExceptionFilter(GET_EXCEPTION(), m_dac, &status))
        {
            EX_RETHROW;
        }
    }
    EX_END_CATCH(SwallowAllExceptions)

    DAC_LEAVE();
    return status;
}

HRESULT STDMETHODCALLTYPE
ClrDataMethodDefinition::EnumInstance(
    /* [out][in] */ CLRDATA_ENUM *handle,
    /* [out] */ IXCLRDataMethodInstance **instance)
{
    HRESULT status;

    DAC_ENTER_SUB(m_dac);

    EX_TRY
    {
        status = EnumMethodInstances::CdNext(m_dac, handle, instance);
    }
    EX_CATCH
    {
        if (!DacExceptionFilter(GET_EXCEPTION(), m_dac, &status))
        {
            EX_RETHROW;
        }
    }
    EX_END_CATCH(SwallowAllExceptions)

    DAC_LEAVE();
    return status;
}

HRESULT STDMETHODCALLTYPE
ClrDataMethodDefinition::EndEnumInstances(
    /* [in] */ CLRDATA_ENUM handle)
{
    HRESULT status;

    DAC_ENTER_SUB(m_dac);

    EX_TRY
    {
        status = EnumMethodInstances::CdEnd(handle);
    }
    EX_CATCH
    {
        if (!DacExceptionFilter(GET_EXCEPTION(), m_dac, &status))
        {
            EX_RETHROW;
        }
    }
    EX_END_CATCH(SwallowAllExceptions)

    DAC_LEAVE();
    return status;
}

HRESULT STDMETHODCALLTYPE
ClrDataMethodDefinition::GetName(
    /* [in] */ ULONG32 flags,
    /* [in] */ ULONG32 bufLen,
    /* [out] */ ULONG32 *nameLen,
    /* [size_is][out] */ __out_ecount_part_opt(bufLen, *nameLen) WCHAR name[  ])
{
    HRESULT status;

    if (flags != 0)
    {
        return E_INVALIDARG;
    }

    DAC_ENTER_SUB(m_dac);

    EX_TRY
    {
        if (m_methodDesc)
        {
            status = m_dac->GetFullMethodName(m_methodDesc,
                                              bufLen, nameLen, name);
        }
        else
        {
            char methName[MAX_CLASSNAME_LENGTH];

            status = GetFullMethodNameFromMetadata(m_module->GetMDImport(),
                                                   m_token,
                                                   NumItems(methName),
                                                   methName);
            if (status == S_OK)
            {
                status = ConvertUtf8(methName, bufLen, nameLen, name);
            }
        }
    }
    EX_CATCH
    {
        if (!DacExceptionFilter(GET_EXCEPTION(), m_dac, &status))
        {
            EX_RETHROW;
        }
    }
    EX_END_CATCH(SwallowAllExceptions)

    DAC_LEAVE();
    return status;
}

HRESULT STDMETHODCALLTYPE
ClrDataMethodDefinition::GetTokenAndScope(
    /* [out] */ mdToken *token,
    /* [out] */ IXCLRDataModule **Module)
{
    HRESULT status;

    DAC_ENTER_SUB(m_dac);

    EX_TRY
    {
        status = S_OK;

        if (token)
        {
            *token = m_token;
        }

        if (Module)
        {
            *Module = new (nothrow)
                ClrDataModule(m_dac, m_module);
            status = *Module ? S_OK : E_OUTOFMEMORY;
        }
    }
    EX_CATCH
    {
        if (!DacExceptionFilter(GET_EXCEPTION(), m_dac, &status))
        {
            EX_RETHROW;
        }
    }
    EX_END_CATCH(SwallowAllExceptions)

    DAC_LEAVE();
    return status;
}

HRESULT STDMETHODCALLTYPE
ClrDataMethodDefinition::GetFlags(
    /* [out] */ ULONG32 *flags)
{
    HRESULT status;

    DAC_ENTER_SUB(m_dac);

    EX_TRY
    {
        status = GetSharedMethodFlags(m_methodDesc, flags);
    }
    EX_CATCH
    {
        if (!DacExceptionFilter(GET_EXCEPTION(), m_dac, &status))
        {
            EX_RETHROW;
        }
    }
    EX_END_CATCH(SwallowAllExceptions)

    DAC_LEAVE();
    return status;
}

HRESULT STDMETHODCALLTYPE
ClrDataMethodDefinition::IsSameObject(
    /* [in] */ IXCLRDataMethodDefinition* method)
{
    HRESULT status;

    DAC_ENTER_SUB(m_dac);

    EX_TRY
    {
        if (m_methodDesc)
        {
            status = (PTR_HOST_TO_TADDR(m_methodDesc) ==
                      PTR_HOST_TO_TADDR(((ClrDataMethodDefinition*)method)->
                                        m_methodDesc)) ?
                S_OK : S_FALSE;
        }
        else
        {
            status = (PTR_HOST_TO_TADDR(m_module) ==
                      PTR_HOST_TO_TADDR(((ClrDataMethodDefinition*)method)->
                                        m_module) &&
                      m_token == ((ClrDataMethodDefinition*)method)->m_token) ?
                S_OK : S_FALSE;
        }
    }
    EX_CATCH
    {
        if (!DacExceptionFilter(GET_EXCEPTION(), m_dac, &status))
        {
            EX_RETHROW;
        }
    }
    EX_END_CATCH(SwallowAllExceptions)

    DAC_LEAVE();
    return status;
}

HRESULT STDMETHODCALLTYPE
ClrDataMethodDefinition::GetLatestEnCVersion(
    /* [out] */ ULONG32* version)
{
    HRESULT status;

    DAC_ENTER_SUB(m_dac);

    EX_TRY
    {
        // XXX Microsoft.
        *version = 0;
        status = S_OK;
    }
    EX_CATCH
    {
        if (!DacExceptionFilter(GET_EXCEPTION(), m_dac, &status))
        {
            EX_RETHROW;
        }
    }
    EX_END_CATCH(SwallowAllExceptions)

    DAC_LEAVE();
    return status;
}

HRESULT STDMETHODCALLTYPE
ClrDataMethodDefinition::StartEnumExtents(
    /* [out] */ CLRDATA_ENUM *handle)
{
    HRESULT status;

    DAC_ENTER_SUB(m_dac);

    EX_TRY
    {
        COR_ILMETHOD* ilMeth = GetIlMethod();
        status = ilMeth ? S_OK : S_FALSE;
        *handle = TO_CDENUM(ilMeth);
    }
    EX_CATCH
    {
        if (!DacExceptionFilter(GET_EXCEPTION(), m_dac, &status))
        {
            EX_RETHROW;
        }
    }
    EX_END_CATCH(SwallowAllExceptions)

    DAC_LEAVE();
    return status;
}

HRESULT STDMETHODCALLTYPE
ClrDataMethodDefinition::EnumExtent(
    /* [out][in] */ CLRDATA_ENUM *handle,
    /* [out] */ CLRDATA_METHDEF_EXTENT *extent)
{
    HRESULT status;

    DAC_ENTER_SUB(m_dac);

    EX_TRY
    {
        if (*handle)
        {
            COR_ILMETHOD* ilMeth = FROM_CDENUM(COR_ILMETHOD, *handle);
            COR_ILMETHOD_DECODER ilDec(ilMeth);
            *handle = 0;

            extent->startAddress = TO_CDADDR(PTR_HOST_TO_TADDR(ilMeth) +
                                             4 * ilDec.GetSize());
            extent->endAddress = extent->startAddress +
                ilDec.GetCodeSize() - 1;
            extent->type = CLRDATA_METHDEF_IL;
            // XXX Microsoft - EnC version.
            extent->enCVersion = 0;

            status = S_OK;
        }
        else
        {
            status = E_INVALIDARG;
        }
    }
    EX_CATCH
    {
        if (!DacExceptionFilter(GET_EXCEPTION(), m_dac, &status))
        {
            EX_RETHROW;
        }
    }
    EX_END_CATCH(SwallowAllExceptions)

    DAC_LEAVE();
    return status;
}

HRESULT STDMETHODCALLTYPE
ClrDataMethodDefinition::EndEnumExtents(
    /* [in] */ CLRDATA_ENUM handle)
{
    HRESULT status;

    DAC_ENTER_SUB(m_dac);

    EX_TRY
    {
        // Nothing to do.
        status = S_OK;
    }
    EX_CATCH
    {
        if (!DacExceptionFilter(GET_EXCEPTION(), m_dac, &status))
        {
            EX_RETHROW;
        }
    }
    EX_END_CATCH(SwallowAllExceptions)

    DAC_LEAVE();
    return status;
}

HRESULT STDMETHODCALLTYPE
ClrDataMethodDefinition::GetCodeNotification(
    /* [out] */ ULONG32 *flags)
{
    HRESULT status;

    DAC_ENTER_SUB(m_dac);

    EX_TRY
    {
        JITNotifications jn(m_dac->GetHostJitNotificationTable());
        if (!jn.IsActive())
        {
            status = E_OUTOFMEMORY;
        }
        else
        {
            TADDR modulePtr = PTR_HOST_TO_TADDR(m_module);
            *flags = jn.Requested(modulePtr, m_token);
            status = S_OK;
        }
    }
    EX_CATCH
    {
        if (!DacExceptionFilter(GET_EXCEPTION(), m_dac, &status))
        {
            EX_RETHROW;
        }
    }
    EX_END_CATCH(SwallowAllExceptions)

    DAC_LEAVE();
    return status;
}

HRESULT STDMETHODCALLTYPE
ClrDataMethodDefinition::SetCodeNotification(
    /* [in] */ ULONG32 flags)
{
    HRESULT status;

    DAC_ENTER_SUB(m_dac);

    EX_TRY
    {
        if (!IsValidMethodCodeNotification(flags))
        {
            status = E_INVALIDARG;
        }
        else
        {
            JITNotifications jn(m_dac->GetHostJitNotificationTable());
            if (!jn.IsActive())
            {
                status = E_OUTOFMEMORY;
            }
            else
            {
                TADDR modulePtr = PTR_HOST_TO_TADDR(m_module);
                USHORT NType = jn.Requested(modulePtr, m_token);

                if (NType == flags)
                {
                    // notification already set
                    status = S_OK;
                }
                else
                {
                    if (jn.SetNotification(modulePtr, m_token, flags) &&
                        jn.UpdateOutOfProcTable())
                    {
                        // new notification added
                        status = S_OK;
                    }
                    else
                    {
                        // error setting notification
                        status = E_FAIL;
                    }
                }
            }
        }
    }
    EX_CATCH
    {
        if (!DacExceptionFilter(GET_EXCEPTION(), m_dac, &status))
        {
            EX_RETHROW;
        }
    }
    EX_END_CATCH(SwallowAllExceptions)

    DAC_LEAVE();
    return status;
}

HRESULT STDMETHODCALLTYPE
ClrDataMethodDefinition::GetRepresentativeEntryAddress(
    /* [out] */ CLRDATA_ADDRESS* addr)
{
    HRESULT status;

    DAC_ENTER_SUB(m_dac);

    EX_TRY
    {
        COR_ILMETHOD* ilMeth = GetIlMethod();
        if (ilMeth)
        {
            COR_ILMETHOD_DECODER ilDec(ilMeth);
            *addr = TO_CDADDR(PTR_HOST_TO_TADDR(ilMeth) +
                              4 * ilDec.GetSize());
            status = S_OK;
        }
        else
        {
            status = E_UNEXPECTED;
        }
    }
    EX_CATCH
    {
        if (!DacExceptionFilter(GET_EXCEPTION(), m_dac, &status))
        {
            EX_RETHROW;
        }
    }
    EX_END_CATCH(SwallowAllExceptions)

    DAC_LEAVE();
    return status;
}

HRESULT STDMETHODCALLTYPE
ClrDataMethodDefinition::HasClassOrMethodInstantiation(
    /* [out] */ BOOL* bGeneric)
{
    HRESULT status;

    DAC_ENTER_SUB(m_dac);

    EX_TRY
    {
        if (m_methodDesc)
        {
            *bGeneric = m_methodDesc->HasClassOrMethodInstantiation();
            status = S_OK;
        }
        else
        {
            status = E_UNEXPECTED;
        }
    }
    EX_CATCH
    {
        if (!DacExceptionFilter(GET_EXCEPTION(), m_dac, &status))
        {
            EX_RETHROW;
        }
    }
    EX_END_CATCH(SwallowAllExceptions)

    DAC_LEAVE();
    return status;
}

HRESULT STDMETHODCALLTYPE
ClrDataMethodDefinition::Request(
    /* [in] */ ULONG32 reqCode,
    /* [in] */ ULONG32 inBufferSize,
    /* [size_is][in] */ BYTE *inBuffer,
    /* [in] */ ULONG32 outBufferSize,
    /* [size_is][out] */ BYTE *outBuffer)
{
    HRESULT status;

    DAC_ENTER_SUB(m_dac);

    EX_TRY
    {
        switch(reqCode)
        {
        case CLRDATA_REQUEST_REVISION:
            if (inBufferSize != 0 ||
                inBuffer ||
                outBufferSize != sizeof(ULONG32))
            {
                status = E_INVALIDARG;
            }
            else
            {
                *(ULONG32*)outBuffer = 1;
                status = S_OK;
            }
            break;

        default:
            status = E_INVALIDARG;
            break;
        }
    }
    EX_CATCH
    {
        if (!DacExceptionFilter(GET_EXCEPTION(), m_dac, &status))
        {
            EX_RETHROW;
        }
    }
    EX_END_CATCH(SwallowAllExceptions)

    DAC_LEAVE();
    return status;
}

COR_ILMETHOD*
ClrDataMethodDefinition::GetIlMethod(void)
{
    if (m_methodDesc)
    {
        if (!m_methodDesc->HasILHeader())
        {
            return NULL;
        }
        else
        {
            return m_methodDesc->GetILHeader();
        }
    }
    else
    {
        ULONG ilRva;
        ULONG implFlags;

        if (FAILED(m_module->GetMDImport()->
            GetMethodImplProps(m_token, &ilRva, &implFlags)))
        {
            return NULL;
        }
        if (!ilRva)
        {
            return NULL;
        }
        else
        {
            return DacGetIlMethod(m_module->GetIL((RVA)ilRva));
        }
    }
}

HRESULT
ClrDataMethodDefinition::NewFromModule(ClrDataAccess* dac,
                                       Module* module,
                                       mdMethodDef token,
                                       ClrDataMethodDefinition** methDef,
                                       IXCLRDataMethodDefinition** pubMethDef)
{
    // The method may not have internal runtime data yet,
    // so the absence of a MethodDesc is not a failure.
    // It'll just produce a metadata-query MethoDefinition.
    MethodDesc* methodDesc = module->LookupMethodDef(token);

    ClrDataMethodDefinition* def = new (nothrow)
        ClrDataMethodDefinition(dac, module, token, methodDesc);
    if (!def)
    {
        return E_OUTOFMEMORY;
    }

    PREFIX_ASSUME(methDef || pubMethDef);

    if (methDef)
    {
        *methDef = def;
    }
    if (pubMethDef)
    {
        *pubMethDef = def;
    }

    return S_OK;
}

HRESULT
ClrDataMethodDefinition::GetSharedMethodFlags(MethodDesc* methodDesc,
                                              ULONG32* flags)
{
    *flags = CLRDATA_METHOD_DEFAULT;

    if (methodDesc)
    {
        MetaSig sig(methodDesc);

        if (sig.HasThis())
        {
            (*flags) |= CLRDATA_METHOD_HAS_THIS;
        }
    }

    return S_OK;
}

//----------------------------------------------------------------------------
//
// ClrDataMethodInstance.
//
//----------------------------------------------------------------------------

ClrDataMethodInstance::ClrDataMethodInstance(ClrDataAccess* dac,
                                             AppDomain* appDomain,
                                             MethodDesc* methodDesc)
{
    m_dac = dac;
    m_dac->AddRef();
    m_instanceAge = m_dac->m_instanceAge;
    m_refs = 1;
    m_appDomain = appDomain;
    m_methodDesc = methodDesc;
}

ClrDataMethodInstance::~ClrDataMethodInstance(void)
{
    m_dac->Release();
}

STDMETHODIMP
ClrDataMethodInstance::QueryInterface(THIS_
                                      IN REFIID interfaceId,
                                      OUT PVOID* iface)
{
    if (IsEqualIID(interfaceId, IID_IUnknown) ||
        IsEqualIID(interfaceId, __uuidof(IXCLRDataMethodInstance)))
    {
        AddRef();
        *iface = static_cast<IUnknown*>
            (static_cast<IXCLRDataMethodInstance*>(this));
        return S_OK;
    }
    else
    {
        *iface = NULL;
        return E_NOINTERFACE;
    }
}

STDMETHODIMP_(ULONG)
ClrDataMethodInstance::AddRef(THIS)
{
    SUPPORTS_DAC_HOST_ONLY;
    return InterlockedIncrement(&m_refs);
}

STDMETHODIMP_(ULONG)
ClrDataMethodInstance::Release(THIS)
{
    LONG newRefs = InterlockedDecrement(&m_refs);
    if (newRefs == 0)
    {
        delete this;
    }
    return newRefs;
}

HRESULT STDMETHODCALLTYPE
ClrDataMethodInstance::GetTypeInstance(
    /* [out] */ IXCLRDataTypeInstance **typeInstance)
{
    HRESULT status;

    DAC_ENTER_SUB(m_dac);

    EX_TRY
    {
        if (!m_appDomain)
        {
            status = E_UNEXPECTED;
        }
        else
        {
            *typeInstance = new (nothrow)
                ClrDataTypeInstance(m_dac,
                                    m_appDomain,
                                    TypeHandle(m_methodDesc->
                                               GetMethodTable()));
            status = *typeInstance ? S_OK : E_OUTOFMEMORY;
        }
    }
    EX_CATCH
    {
        if (!DacExceptionFilter(GET_EXCEPTION(), m_dac, &status))
        {
            EX_RETHROW;
        }
    }
    EX_END_CATCH(SwallowAllExceptions)

    DAC_LEAVE();
    return status;
}

HRESULT STDMETHODCALLTYPE
ClrDataMethodInstance::GetDefinition(
    /* [out] */ IXCLRDataMethodDefinition **methodDefinition)
{
    HRESULT status;

    DAC_ENTER_SUB(m_dac);

    EX_TRY
    {
        *methodDefinition = new (nothrow)
            ClrDataMethodDefinition(m_dac,
                                    m_methodDesc->GetModule(),
                                    m_methodDesc->GetMemberDef(),
                                    m_methodDesc);
        status = *methodDefinition ? S_OK : E_OUTOFMEMORY;
    }
    EX_CATCH
    {
        if (!DacExceptionFilter(GET_EXCEPTION(), m_dac, &status))
        {
            EX_RETHROW;
        }
    }
    EX_END_CATCH(SwallowAllExceptions)

    DAC_LEAVE();
    return status;
}

HRESULT STDMETHODCALLTYPE
ClrDataMethodInstance::GetTokenAndScope(
    /* [out] */ mdToken *token,
    /* [out] */ IXCLRDataModule **mod)
{
    HRESULT status;

    DAC_ENTER_SUB(m_dac);

    EX_TRY
    {
        status = S_OK;

        if (token)
        {
            *token = m_methodDesc->GetMemberDef();
        }

        if (mod)
        {
            *mod = new (nothrow)
                ClrDataModule(m_dac, m_methodDesc->GetModule());
            status = *mod ? S_OK : E_OUTOFMEMORY;
        }
    }
    EX_CATCH
    {
        if (!DacExceptionFilter(GET_EXCEPTION(), m_dac, &status))
        {
            EX_RETHROW;
        }
    }
    EX_END_CATCH(SwallowAllExceptions)

    DAC_LEAVE();
    return status;
}

HRESULT STDMETHODCALLTYPE
ClrDataMethodInstance::GetName(
    /* [in] */ ULONG32 flags,
    /* [in] */ ULONG32 bufLen,
    /* [out] */ ULONG32 *nameLen,
    /* [size_is][out] */ __out_ecount_part(bufLen, *nameLen) WCHAR name[  ])
{
    HRESULT status;

    if (flags != 0)
    {
        return E_INVALIDARG;
    }

    DAC_ENTER_SUB(m_dac);

    EX_TRY
    {
        status = m_dac->GetFullMethodName(m_methodDesc, bufLen, nameLen, name);
    }
    EX_CATCH
    {
        if (!DacExceptionFilter(GET_EXCEPTION(), m_dac, &status))
        {
            EX_RETHROW;
        }
        else
        {
            static WCHAR nameUnk[] = W("Unknown");
            wcscpy_s(name, bufLen, nameUnk);
            if (nameLen != NULL)
            {
                *nameLen = _countof(nameUnk);
            }
            status = S_OK;
        }
    }
    EX_END_CATCH(SwallowAllExceptions)

    DAC_LEAVE();
    return status;
}

HRESULT STDMETHODCALLTYPE
ClrDataMethodInstance::GetFlags(
    /* [out] */ ULONG32 *flags)
{
    HRESULT status;

    DAC_ENTER_SUB(m_dac);

    EX_TRY
    {
        status = ClrDataMethodDefinition::
            GetSharedMethodFlags(m_methodDesc, flags);
    }
    EX_CATCH
    {
        if (!DacExceptionFilter(GET_EXCEPTION(), m_dac, &status))
        {
            EX_RETHROW;
        }
    }
    EX_END_CATCH(SwallowAllExceptions)

    DAC_LEAVE();
    return status;
}

HRESULT STDMETHODCALLTYPE
ClrDataMethodInstance::IsSameObject(
    /* [in] */ IXCLRDataMethodInstance* method)
{
    HRESULT status;

    DAC_ENTER_SUB(m_dac);

    EX_TRY
    {
        status = (PTR_HOST_TO_TADDR(m_appDomain) ==
                  PTR_HOST_TO_TADDR(((ClrDataMethodInstance*)method)->
                                    m_appDomain) &&
                  PTR_HOST_TO_TADDR(m_methodDesc) ==
                  PTR_HOST_TO_TADDR(((ClrDataMethodInstance*)method)->
                                    m_methodDesc)) ?
            S_OK : S_FALSE;
    }
    EX_CATCH
    {
        if (!DacExceptionFilter(GET_EXCEPTION(), m_dac, &status))
        {
            EX_RETHROW;
        }
    }
    EX_END_CATCH(SwallowAllExceptions)

    DAC_LEAVE();
    return status;
}

HRESULT STDMETHODCALLTYPE
ClrDataMethodInstance::GetEnCVersion(
    /* [out] */ ULONG32* version)
{
    HRESULT status;

    DAC_ENTER_SUB(m_dac);

    EX_TRY
    {
        // XXX Microsoft.
        *version = 0;
        status = S_OK;
    }
    EX_CATCH
    {
        if (!DacExceptionFilter(GET_EXCEPTION(), m_dac, &status))
        {
            EX_RETHROW;
        }
    }
    EX_END_CATCH(SwallowAllExceptions)

    DAC_LEAVE();
    return status;
}

HRESULT STDMETHODCALLTYPE
ClrDataMethodInstance::GetNumTypeArguments(
    /* [out] */ ULONG32 *numTypeArgs)
{
    HRESULT status;

    DAC_ENTER_SUB(m_dac);

    EX_TRY
    {
        // XXX Microsoft.
        status = E_NOTIMPL;
    }
    EX_CATCH
    {
        if (!DacExceptionFilter(GET_EXCEPTION(), m_dac, &status))
        {
            EX_RETHROW;
        }
    }
    EX_END_CATCH(SwallowAllExceptions)

    DAC_LEAVE();
    return status;
}

HRESULT STDMETHODCALLTYPE
ClrDataMethodInstance::GetTypeArgumentByIndex(
    /* [in] */ ULONG32 index,
    /* [out] */ IXCLRDataTypeInstance **typeArg)
{
    HRESULT status;

    DAC_ENTER_SUB(m_dac);

    EX_TRY
    {
        // XXX Microsoft.
        status = E_NOTIMPL;
    }
    EX_CATCH
    {
        if (!DacExceptionFilter(GET_EXCEPTION(), m_dac, &status))
        {
            EX_RETHROW;
        }
    }
    EX_END_CATCH(SwallowAllExceptions)

    DAC_LEAVE();
    return status;
}

HRESULT STDMETHODCALLTYPE
ClrDataMethodInstance::GetILOffsetsByAddress(
    /* [in] */ CLRDATA_ADDRESS address,
    /* [in] */ ULONG32 offsetsLen,
    /* [out] */ ULONG32 *offsetsNeeded,
    /* [size_is][out] */ ULONG32 ilOffsets[  ])
{
    HRESULT status;
    DebuggerILToNativeMap* map = NULL;
    bool mapAllocated = false;

    DAC_ENTER_SUB(m_dac);
    EX_TRY
    {
        ULONG32 numMap;
        ULONG32 codeOffset;
        ULONG32 hits = 0;

#ifdef TARGET_ARM
        address &= ~THUMB_CODE; // on ARM windbg passes in an address with the mode flag set... need to workaround
#endif
        if ((status = m_dac->GetMethodNativeMap(m_methodDesc,
                                                CLRDATA_ADDRESS_TO_TADDR(address),
                                                &numMap,
                                                &map,
                                                &mapAllocated,
                                                NULL,
                                                &codeOffset)) != S_OK)
        {
            goto Exit;
        }

        for (ULONG32 i = 0; i < numMap; i++)
        {
            if (codeOffset >= map[i].nativeStartOffset &&
                // Found the entry if it is the epilog or the last entry and nativeEndOffset == 0. For methods that don't
                // have a epilog (i.e. IL_Throw is the last instruction) the last map entry has a valid IL offset (not EPILOG)
                // and nativeEndOffset == 0.
                ((((LONG)map[i].ilOffset == ICorDebugInfo::EPILOG || i == (numMap - 1)) && map[i].nativeEndOffset == 0) ||
                    codeOffset < map[i].nativeEndOffset))
            {
                hits++;

                if (offsetsLen && ilOffsets)
                {
                    *ilOffsets = map[i].ilOffset;
                    ilOffsets++;
                    offsetsLen--;
                }
            }
        }

        if (offsetsNeeded)
        {
            *offsetsNeeded = hits;
        }
        status = hits ? S_OK : E_NOINTERFACE;

    Exit: ;
    }
    EX_CATCH
    {
        if (!DacExceptionFilter(GET_EXCEPTION(), m_dac, &status))
        {
            EX_RETHROW;
        }
    }
    EX_END_CATCH(SwallowAllExceptions)

    if (mapAllocated)
    {
        delete [] map;
    }

    DAC_LEAVE();
    return status;
}

HRESULT STDMETHODCALLTYPE
ClrDataMethodInstance::GetAddressRangesByILOffset(
    /* [in] */ ULONG32 ilOffset,
    /* [in] */ ULONG32 rangesLen,
    /* [out] */ ULONG32 *rangesNeeded,
    /* [size_is][out] */ CLRDATA_ADDRESS_RANGE addressRanges[  ])
{
    HRESULT status;
    DebuggerILToNativeMap* map = NULL;
    bool mapAllocated = false;

    DAC_ENTER_SUB(m_dac);

    EX_TRY
    {
        ULONG32 numMap;
        CLRDATA_ADDRESS codeStart;
        ULONG32 hits = 0;

        if ((status = m_dac->GetMethodNativeMap(m_methodDesc,
                                                0,
                                                &numMap,
                                                &map,
                                                &mapAllocated,
                                                &codeStart,
                                                NULL)) != S_OK)
        {
            goto Exit;
        }

        for (ULONG32 i = 0; i < numMap; i++)
        {
            if (map[i].ilOffset == ilOffset)
            {
                hits++;

                if (rangesLen && addressRanges)
                {
                    addressRanges->startAddress =
                        TO_CDADDR(codeStart + map[i].nativeStartOffset);
                    if ((LONG)map[i].ilOffset == ICorDebugInfo::EPILOG &&
                        !map[i].nativeEndOffset)
                    {
                        addressRanges->endAddress = 0;
                    }
                    else
                    {
                        addressRanges->endAddress =
                            TO_CDADDR(codeStart + map[i].nativeEndOffset);
                    }
                    addressRanges++;
                    rangesLen--;
                }
            }
        }

        if (rangesNeeded)
        {
            *rangesNeeded = hits;
        }
        status = hits ? S_OK : E_NOINTERFACE;

    Exit: ;
    }
    EX_CATCH
    {
        if (!DacExceptionFilter(GET_EXCEPTION(), m_dac, &status))
        {
            EX_RETHROW;
        }
    }
    EX_END_CATCH(SwallowAllExceptions)

    if (mapAllocated)
    {
        delete [] map;
    }

    DAC_LEAVE();
    return status;
}

HRESULT STDMETHODCALLTYPE
ClrDataMethodInstance::GetILAddressMap(
    /* [in] */ ULONG32 mapLen,
    /* [out] */ ULONG32 *mapNeeded,
    /* [size_is][out] */ CLRDATA_IL_ADDRESS_MAP maps[  ])
{
    HRESULT status;
    DebuggerILToNativeMap* map = NULL;
    bool mapAllocated = false;

    DAC_ENTER_SUB(m_dac);

    EX_TRY
    {
        ULONG32 numMap;
        CLRDATA_ADDRESS codeStart;

        if ((status = m_dac->GetMethodNativeMap(m_methodDesc,
                                                0,
                                                &numMap,
                                                &map,
                                                &mapAllocated,
                                                &codeStart,
                                                NULL)) != S_OK)
        {
            goto Exit;
        }

        for (ULONG32 i = 0; i < numMap; i++)
        {
            if (mapLen && maps)
            {
                maps->ilOffset = map[i].ilOffset;
                maps->startAddress =
                    TO_CDADDR(codeStart + map[i].nativeStartOffset);
                maps->endAddress =
                    TO_CDADDR(codeStart + map[i].nativeEndOffset);
                // XXX Microsoft - Define types as mapping of
                // ICorDebugInfo::SourceTypes.
                maps->type = CLRDATA_SOURCE_TYPE_INVALID;
                maps++;
                mapLen--;
            }
            else
            {
                break;
            }
        }

        if (mapNeeded)
        {
            *mapNeeded = numMap;
        }
        status = numMap ? S_OK : E_NOINTERFACE;

    Exit: ;
    }
    EX_CATCH
    {
        if (!DacExceptionFilter(GET_EXCEPTION(), m_dac, &status))
        {
            EX_RETHROW;
        }
    }
    EX_END_CATCH(SwallowAllExceptions)

    if (mapAllocated)
    {
        delete [] map;
    }

    DAC_LEAVE();
    return status;
}

HRESULT STDMETHODCALLTYPE
ClrDataMethodInstance::StartEnumExtents(
    /* [out] */ CLRDATA_ENUM *handle)
{
    HRESULT status;

    DAC_ENTER_SUB(m_dac);

    EX_TRY
    {
        METH_EXTENTS* extents;

        if ((status = m_dac->
             GetMethodExtents(m_methodDesc, &extents)) == S_OK)
        {
            *handle = TO_CDENUM(extents);
        }
    }
    EX_CATCH
    {
        if (!DacExceptionFilter(GET_EXCEPTION(), m_dac, &status))
        {
            EX_RETHROW;
        }
    }
    EX_END_CATCH(SwallowAllExceptions)

    DAC_LEAVE();
    return status;
}

HRESULT STDMETHODCALLTYPE
ClrDataMethodInstance::EnumExtent(
    /* [out][in] */ CLRDATA_ENUM *handle,
    /* [out] */ CLRDATA_ADDRESS_RANGE *extent)
{
    HRESULT status;

    DAC_ENTER_SUB(m_dac);

    EX_TRY
    {
        METH_EXTENTS* extents =
            FROM_CDENUM(METH_EXTENTS, *handle);
        if (extents->curExtent >= extents->numExtents)
        {
            status = S_FALSE;
        }
        else
        {
            CLRDATA_ADDRESS_RANGE* curExtent =
                extents->extents + extents->curExtent++;
            *extent = *curExtent;
            status = S_OK;
        }
    }
    EX_CATCH
    {
        if (!DacExceptionFilter(GET_EXCEPTION(), m_dac, &status))
        {
            EX_RETHROW;
        }
    }
    EX_END_CATCH(SwallowAllExceptions)

    DAC_LEAVE();
    return status;
}

HRESULT STDMETHODCALLTYPE
ClrDataMethodInstance::EndEnumExtents(
    /* [in] */ CLRDATA_ENUM handle)
{
    HRESULT status;

    DAC_ENTER_SUB(m_dac);

    EX_TRY
    {
        delete FROM_CDENUM(METH_EXTENTS, handle);
        status = S_OK;
    }
    EX_CATCH
    {
        if (!DacExceptionFilter(GET_EXCEPTION(), m_dac, &status))
        {
            EX_RETHROW;
        }
    }
    EX_END_CATCH(SwallowAllExceptions)

    DAC_LEAVE();
    return status;
}

HRESULT STDMETHODCALLTYPE
ClrDataMethodInstance::GetRepresentativeEntryAddress(
    /* [out] */ CLRDATA_ADDRESS* addr)
{
    HRESULT status;

    DAC_ENTER_SUB(m_dac);

    EX_TRY
    {
        if (m_methodDesc->HasNativeCode())
        {
            *addr = TO_CDADDR(m_methodDesc->GetNativeCode());
            status = S_OK;
        }
        else
        {
            status = E_UNEXPECTED;
        }
    }
    EX_CATCH
    {
        if (!DacExceptionFilter(GET_EXCEPTION(), m_dac, &status))
        {
            EX_RETHROW;
        }
    }
    EX_END_CATCH(SwallowAllExceptions)

    DAC_LEAVE();
    return status;
}

HRESULT STDMETHODCALLTYPE
ClrDataMethodInstance::Request(
    /* [in] */ ULONG32 reqCode,
    /* [in] */ ULONG32 inBufferSize,
    /* [size_is][in] */ BYTE *inBuffer,
    /* [in] */ ULONG32 outBufferSize,
    /* [size_is][out] */ BYTE *outBuffer)
{
    HRESULT status;

    DAC_ENTER_SUB(m_dac);

    EX_TRY
    {
        switch(reqCode)
        {
        case CLRDATA_REQUEST_REVISION:
            if (inBufferSize != 0 ||
                inBuffer ||
                outBufferSize != sizeof(ULONG32))
            {
                status = E_INVALIDARG;
            }
            else
            {
                *(ULONG32*)outBuffer = 1;
                status = S_OK;
            }
            break;

        default:
            status = E_INVALIDARG;
            break;
        }
    }
    EX_CATCH
    {
        if (!DacExceptionFilter(GET_EXCEPTION(), m_dac, &status))
        {
            EX_RETHROW;
        }
    }
    EX_END_CATCH(SwallowAllExceptions)

    DAC_LEAVE();
    return status;
}

HRESULT
ClrDataMethodInstance::NewFromModule(ClrDataAccess* dac,
                                     AppDomain* appDomain,
                                     Module* module,
                                     mdMethodDef token,
                                     ClrDataMethodInstance** methInst,
                                     IXCLRDataMethodInstance** pubMethInst)
{
    MethodDesc* methodDesc = module->LookupMethodDef(token);
    if (!methodDesc ||
        !methodDesc->HasNativeCode())
    {
        return E_INVALIDARG;
    }

    ClrDataMethodInstance* inst = new (nothrow)
        ClrDataMethodInstance(dac, appDomain, methodDesc);
    if (!inst)
    {
        return E_OUTOFMEMORY;
    }

    PREFIX_ASSUME(methInst || pubMethInst);

    if (methInst)
    {
        *methInst = inst;
    }
    if (pubMethInst)
    {
        *pubMethInst = inst;
    }

    return S_OK;
}

//----------------------------------------------------------------------------
//
// ClrDataExceptionState.
//
//----------------------------------------------------------------------------

ClrDataExceptionState::ClrDataExceptionState(ClrDataAccess* dac,
                                             AppDomain* appDomain,
                                             Thread* thread,
                                             ULONG32 flags,
                                             ClrDataExStateType* exInfo,
                                             OBJECTHANDLE throwable,
                                             ClrDataExStateType* prevExInfo)
{
    m_dac = dac;
    m_dac->AddRef();
    m_instanceAge = m_dac->m_instanceAge;
    m_appDomain = appDomain;
    m_thread = thread;
    m_flags = flags;
    m_exInfo = exInfo;
    m_throwable = throwable;
    m_prevExInfo = prevExInfo;
    m_refs = 1;
}

ClrDataExceptionState::~ClrDataExceptionState(void)
{
    m_dac->Release();
}

STDMETHODIMP
ClrDataExceptionState::QueryInterface(THIS_
                            IN REFIID interfaceId,
                            OUT PVOID* iface)
{
    if (IsEqualIID(interfaceId, IID_IUnknown) ||
        IsEqualIID(interfaceId, __uuidof(IXCLRDataExceptionState)))
    {
        AddRef();
        *iface = static_cast<IUnknown*>
            (static_cast<IXCLRDataExceptionState*>(this));
        return S_OK;
    }
    else
    {
        *iface = NULL;
        return E_NOINTERFACE;
    }
}

STDMETHODIMP_(ULONG)
ClrDataExceptionState::AddRef(THIS)
{
    return InterlockedIncrement(&m_refs);
}

STDMETHODIMP_(ULONG)
ClrDataExceptionState::Release(THIS)
{
    SUPPORTS_DAC_HOST_ONLY;
    LONG newRefs = InterlockedDecrement(&m_refs);
    if (newRefs == 0)
    {
        delete this;
    }
    return newRefs;
}

HRESULT STDMETHODCALLTYPE
ClrDataExceptionState::GetFlags(
    /* [out] */ ULONG32 *flags)
{
    HRESULT status;

    DAC_ENTER_SUB(m_dac);

    EX_TRY
    {
        *flags = m_flags;

        if (m_prevExInfo)
        {
            (*flags) |= CLRDATA_EXCEPTION_NESTED;
        }

        status = S_OK;
    }
    EX_CATCH
    {
        if (!DacExceptionFilter(GET_EXCEPTION(), m_dac, &status))
        {
            EX_RETHROW;
        }
    }
    EX_END_CATCH(SwallowAllExceptions)

    DAC_LEAVE();
    return status;
}

HRESULT STDMETHODCALLTYPE
ClrDataExceptionState::GetPrevious(
    /* [out] */ IXCLRDataExceptionState **exState)
{
    HRESULT status;

    DAC_ENTER_SUB(m_dac);

    EX_TRY
    {
        if (m_prevExInfo)
        {
            *exState = new (nothrow)
                ClrDataExceptionState(m_dac,
                                      m_appDomain,
                                      m_thread,
                                      CLRDATA_EXCEPTION_DEFAULT,
                                      m_prevExInfo,
                                      m_prevExInfo->m_hThrowable,
                                      m_prevExInfo->m_pPrevNestedInfo);
            status = *exState ? S_OK : E_OUTOFMEMORY;
        }
        else
        {
            *exState = NULL;
            status = S_FALSE;
        }
    }
    EX_CATCH
    {
        if (!DacExceptionFilter(GET_EXCEPTION(), m_dac, &status))
        {
            EX_RETHROW;
        }
    }
    EX_END_CATCH(SwallowAllExceptions)

    DAC_LEAVE();
    return status;
}

HRESULT STDMETHODCALLTYPE
ClrDataExceptionState::GetManagedObject(
    /* [out] */ IXCLRDataValue **value)
{
    HRESULT status;

    DAC_ENTER_SUB(m_dac);

    EX_TRY
    {
        PTR_UNCHECKED_OBJECTREF throwRef(m_throwable);
        if (!throwRef.IsValid())
        {
            status = E_INVALIDARG;
            goto Exit;
        }

        NativeVarLocation varLoc;
        ClrDataValue* RefVal;

        varLoc.addr = TO_CDADDR(m_throwable);
        varLoc.size = sizeof(TADDR);
        varLoc.contextReg = false;

        RefVal = new (nothrow)
            ClrDataValue(m_dac,
                         m_appDomain,
                         m_thread,
                         CLRDATA_VALUE_IS_REFERENCE,
                         TypeHandle((*throwRef)->GetMethodTable()),
                         varLoc.addr,
                         1, &varLoc);
        if (!RefVal)
        {
            status = E_OUTOFMEMORY;
            goto Exit;
        }

        status = RefVal->GetAssociatedValue(value);

        delete RefVal;

    Exit: ;
    }
    EX_CATCH
    {
        if (!DacExceptionFilter(GET_EXCEPTION(), m_dac, &status))
        {
            EX_RETHROW;
        }
    }
    EX_END_CATCH(SwallowAllExceptions)

    DAC_LEAVE();
    return status;
}

HRESULT STDMETHODCALLTYPE
ClrDataExceptionState::GetBaseType(
    /* [out] */ CLRDataBaseExceptionType *type)
{
    HRESULT status;

    DAC_ENTER_SUB(m_dac);

    EX_TRY
    {
        // XXX Microsoft.
        status = E_NOTIMPL;
    }
    EX_CATCH
    {
        if (!DacExceptionFilter(GET_EXCEPTION(), m_dac, &status))
        {
            EX_RETHROW;
        }
    }
    EX_END_CATCH(SwallowAllExceptions)

    DAC_LEAVE();
    return status;
}

HRESULT STDMETHODCALLTYPE
ClrDataExceptionState::GetCode(
    /* [out] */ ULONG32 *code)
{
    HRESULT status;

    DAC_ENTER_SUB(m_dac);

    EX_TRY
    {
        // XXX Microsoft.
        status = E_NOTIMPL;
    }
    EX_CATCH
    {
        if (!DacExceptionFilter(GET_EXCEPTION(), m_dac, &status))
        {
            EX_RETHROW;
        }
    }
    EX_END_CATCH(SwallowAllExceptions)

    DAC_LEAVE();
    return status;
}

HRESULT STDMETHODCALLTYPE
ClrDataExceptionState::GetString(
    /* [in] */ ULONG32 bufLen,
    /* [out] */ ULONG32 *strLen,
    /* [size_is][out] */ __out_ecount_part(bufLen, *strLen) WCHAR str[  ])
{
    HRESULT status = E_FAIL;

    DAC_ENTER_SUB(m_dac);

    EX_TRY
    {
        PTR_UNCHECKED_OBJECTREF throwRef(m_throwable);
        STRINGREF message = EXCEPTIONREF(*throwRef)->GetMessage();

        if (message == NULL)
        {
            if (strLen)
            {
                *strLen = 0;
            }
            if (bufLen >= 1)
            {
                str[0] = 0;
            }
            status = S_OK;
        }
        else
        {
            PWSTR msgStr = DacInstantiateStringW((TADDR)message->GetBuffer(),
                                                 message->GetStringLength(),
                                                 true);

            status = StringCchCopy(str, bufLen, msgStr) == S_OK ? S_OK : S_FALSE;
            if (strLen != NULL)
            {
                size_t cchName = wcslen(msgStr) + 1;
                if (FitsIn<ULONG32>(cchName))
                {
                    *strLen = (ULONG32) cchName;
                }
                else
                {
                    status = COR_E_OVERFLOW;
                }
            }
        }
    }
    EX_CATCH
    {
        if (!DacExceptionFilter(GET_EXCEPTION(), m_dac, &status))
        {
            EX_RETHROW;
        }
    }
    EX_END_CATCH(SwallowAllExceptions)

    DAC_LEAVE();
    return status;
}

HRESULT STDMETHODCALLTYPE
ClrDataExceptionState::IsSameState(
    /* [in] */ EXCEPTION_RECORD64 *exRecord,
    /* [in] */ ULONG32 contextSize,
    /* [size_is][in] */ BYTE cxRecord[  ])
{
    return IsSameState2(CLRDATA_EXSAME_SECOND_CHANCE,
                        exRecord, contextSize, cxRecord);
}

HRESULT STDMETHODCALLTYPE
ClrDataExceptionState::IsSameState2(
    /* [in] */ ULONG32 flags,
    /* [in] */ EXCEPTION_RECORD64 *exRecord,
    /* [in] */ ULONG32 contextSize,
    /* [size_is][in] */ BYTE cxRecord[  ])
{
    HRESULT status;

    if ((flags & ~(CLRDATA_EXSAME_SECOND_CHANCE |
                   CLRDATA_EXSAME_FIRST_CHANCE)) != 0)
    {
        return E_INVALIDARG;
    }

    DAC_ENTER_SUB(m_dac);

    EX_TRY
    {
        PTR_EXCEPTION_RECORD infoExRecord;

        // XXX Microsoft - This should also check that the
        // context matches the context at the time
        // of the exception, but it's not clear
        // how to do that in all cases.

        status = S_FALSE;

        if (!m_exInfo)
        {
            // We don't have full state, but that's expected
            // on a first chance exception so allow that.
            if ((flags & CLRDATA_EXSAME_FIRST_CHANCE) != 0)
            {
                status = S_OK;
            }

            goto Exit;
        }

        infoExRecord = GetCurrentExceptionRecord();

        if ((TADDR)infoExRecord->ExceptionAddress ==
            (TADDR)exRecord->ExceptionAddress)
        {
            status = S_OK;
        }

    Exit: ;
    }
    EX_CATCH
    {
        if (!DacExceptionFilter(GET_EXCEPTION(), m_dac, &status))
        {
            EX_RETHROW;
        }
    }
    EX_END_CATCH(SwallowAllExceptions)

    DAC_LEAVE();
    return status;
}

HRESULT STDMETHODCALLTYPE
ClrDataExceptionState::GetTask(
    /* [out] */ IXCLRDataTask** task)
{
    HRESULT status;

    DAC_ENTER_SUB(m_dac);

    EX_TRY
    {
        *task = new (nothrow)
            ClrDataTask(m_dac,
                        m_thread);
        status = *task ? S_OK : E_OUTOFMEMORY;
    }
    EX_CATCH
    {
        if (!DacExceptionFilter(GET_EXCEPTION(), m_dac, &status))
        {
            EX_RETHROW;
        }
    }
    EX_END_CATCH(SwallowAllExceptions)

    DAC_LEAVE();
    return status;
}

HRESULT STDMETHODCALLTYPE
ClrDataExceptionState::Request(
    /* [in] */ ULONG32 reqCode,
    /* [in] */ ULONG32 inBufferSize,
    /* [size_is][in] */ BYTE *inBuffer,
    /* [in] */ ULONG32 outBufferSize,
    /* [size_is][out] */ BYTE *outBuffer)
{
    HRESULT status;

    DAC_ENTER_SUB(m_dac);

    EX_TRY
    {
        switch(reqCode)
        {
        case CLRDATA_REQUEST_REVISION:
            if (inBufferSize != 0 ||
                inBuffer ||
                outBufferSize != sizeof(ULONG32))
            {
                status = E_INVALIDARG;
            }
            else
            {
                *(ULONG32*)outBuffer = 2;
                status = S_OK;
            }
            break;

        default:
            status = E_INVALIDARG;
            break;
        }
    }
    EX_CATCH
    {
        if (!DacExceptionFilter(GET_EXCEPTION(), m_dac, &status))
        {
            EX_RETHROW;
        }
    }
    EX_END_CATCH(SwallowAllExceptions)

    DAC_LEAVE();
    return status;
}

HRESULT
ClrDataExceptionState::NewFromThread(ClrDataAccess* dac,
                                     Thread* thread,
                                     ClrDataExceptionState** exception,
                                     IXCLRDataExceptionState** pubException)
{
    if (!thread->HasException())
    {
        return E_NOINTERFACE;
    }

    ClrDataExStateType* exState;
    ClrDataExceptionState* exIf;

#ifdef FEATURE_EH_FUNCLETS
    exState = thread->GetExceptionState()->m_pCurrentTracker;
#else
    exState = &(thread->GetExceptionState()->m_currentExInfo);
#endif // FEATURE_EH_FUNCLETS

    exIf = new (nothrow)
        ClrDataExceptionState(dac,
                              thread->GetDomain(),
                              thread,
                              CLRDATA_EXCEPTION_DEFAULT,
                              exState,
                              exState->m_hThrowable,
                              exState->m_pPrevNestedInfo);
    if (!exIf)
    {
        return E_OUTOFMEMORY;
    }

    PREFIX_ASSUME(exception || pubException);

    if (exception)
    {
        *exception = exIf;
    }
    if (pubException)
    {
        *pubException = exIf;
    }

    return S_OK;
}

PTR_EXCEPTION_RECORD
ClrDataExceptionState::GetCurrentExceptionRecord()
{
    PTR_EXCEPTION_RECORD pExRecord = NULL;

#ifdef FEATURE_EH_FUNCLETS
    pExRecord = m_exInfo->m_ptrs.ExceptionRecord;
#else // FEATURE_EH_FUNCLETS
    pExRecord = m_exInfo->m_pExceptionRecord;
#endif // FEATURE_EH_FUNCLETS

    return pExRecord;
}

PTR_CONTEXT
ClrDataExceptionState::GetCurrentContextRecord()
{
    PTR_CONTEXT pContext = NULL;

#ifdef FEATURE_EH_FUNCLETS
    pContext = m_exInfo->m_ptrs.ContextRecord;
#else // FEATURE_EH_FUNCLETS
    pContext = m_exInfo->m_pContext;
#endif // FEATURE_EH_FUNCLETS

    return pContext;
}

//----------------------------------------------------------------------------
//
// EnumMethodDefinitions.
//
//----------------------------------------------------------------------------

HRESULT
EnumMethodDefinitions::Start(Module* mod,
                             bool useAddrFilter,
                             CLRDATA_ADDRESS addrFilter)
{
    m_module = mod;
    m_useAddrFilter = useAddrFilter;
    m_addrFilter = addrFilter;
    m_typeToken = mdTokenNil;
    m_needMethodStart = true;
    return m_typeEnum.Start(m_module->GetMDImport(), mdtTypeDef, mdTokenNil);
}

HRESULT
EnumMethodDefinitions::Next(ClrDataAccess* dac,
                            IXCLRDataMethodDefinition **method)
{
    HRESULT status;

 NextType:
    if (m_typeToken == mdTokenNil)
    {
        if ((status = m_typeEnum.NextToken(&m_typeToken, NULL, NULL)) != S_OK)
        {
            return status;
        }

        m_needMethodStart = true;
    }

    if (m_needMethodStart)
    {
        if ((status = m_methodEnum.
             Start(m_module->GetMDImport(),
                   mdtMethodDef, m_typeToken)) != S_OK)
        {
            return status;
        }

        m_needMethodStart = false;
    }

 NextMethod:
    mdToken methodToken;

    if ((status = m_methodEnum.NextToken(&methodToken, NULL, NULL)) != S_OK)
    {
        if (status == S_FALSE)
        {
            m_typeToken = mdTokenNil;
            goto NextType;
        }

        return status;
    }

    if (m_useAddrFilter)
    {
        ULONG ilRva;
        ULONG implFlags;

        status = m_module->GetMDImport()->
            GetMethodImplProps(methodToken, &ilRva, &implFlags);
        if (FAILED(status))
        {
            return status;
        }
        if (!ilRva)
        {
            goto NextMethod;
        }

        COR_ILMETHOD* ilMeth =
            DacGetIlMethod(m_module->GetIL((RVA)ilRva));
        COR_ILMETHOD_DECODER ilDec(ilMeth);

        CLRDATA_ADDRESS start =
            TO_CDADDR(PTR_HOST_TO_TADDR(ilMeth) + 4 * ilDec.GetSize());
        if (m_addrFilter < start ||
            m_addrFilter > start + ilDec.GetCodeSize() - 1)
        {
            goto NextMethod;
        }
    }

    return ClrDataMethodDefinition::
        NewFromModule(dac,
                      m_module,
                      methodToken,
                      NULL,
                      method);
}

HRESULT
EnumMethodDefinitions::CdStart(Module* mod,
                               bool useAddrFilter,
                               CLRDATA_ADDRESS addrFilter,
                               CLRDATA_ENUM* handle)
{
    HRESULT status;

    *handle = NULL;

    if (!mod)
    {
        return S_FALSE;
    }

    EnumMethodDefinitions* iter = new (nothrow)
        EnumMethodDefinitions;
    if (!iter)
    {
        return E_OUTOFMEMORY;
    }

    if ((status = iter->Start(mod, useAddrFilter, addrFilter)) != S_OK)
    {
        delete iter;
        return status;
    }

    *handle = TO_CDENUM(iter);
    return S_OK;
}

HRESULT
EnumMethodDefinitions::CdNext(ClrDataAccess* dac,
                              CLRDATA_ENUM* handle,
                              IXCLRDataMethodDefinition** method)
{
    EnumMethodDefinitions* iter = FROM_CDENUM(EnumMethodDefinitions, *handle);
    if (!iter)
    {
        return S_FALSE;
    }

    return iter->Next(dac, method);
}

HRESULT
EnumMethodDefinitions::CdEnd(CLRDATA_ENUM handle)
{
    EnumMethodDefinitions* iter = FROM_CDENUM(EnumMethodDefinitions, handle);
    if (iter)
    {
        delete iter;
        return S_OK;
    }
    else
    {
        return E_INVALIDARG;
    }
}

//----------------------------------------------------------------------------
//
// EnumMethodInstances.
//
//----------------------------------------------------------------------------

EnumMethodInstances::EnumMethodInstances(MethodDesc* methodDesc,
                                         IXCLRDataAppDomain* givenAppDomain)
    : m_domainIter(FALSE)
{
    m_methodDesc = methodDesc;
    if (givenAppDomain)
    {
        m_givenAppDomain =
            ((ClrDataAppDomain*)givenAppDomain)->GetAppDomain();
    }
    else
    {
        m_givenAppDomain = NULL;
    }
    m_givenAppDomainUsed = false;
    m_appDomain = NULL;
}

HRESULT
EnumMethodInstances::Next(ClrDataAccess* dac,
                          IXCLRDataMethodInstance **instance)
{
 NextDomain:
    if (!m_appDomain)
    {
        if (m_givenAppDomainUsed ||
            !m_domainIter.Next())
        {
            return S_FALSE;
        }

        if (m_givenAppDomain)
        {
            m_appDomain = m_givenAppDomain;
            m_givenAppDomainUsed = true;
        }
        else
        {
            m_appDomain = m_domainIter.GetDomain();
        }

        m_methodIter.Start(m_appDomain,
                           m_methodDesc->GetModule(),       // module
                           m_methodDesc->GetMemberDef(),    // token
                           m_methodDesc);                   // intial method desc
    }

 NextMethod:
    {
        // Note: DAC doesn't need to keep the assembly alive - see code:CollectibleAssemblyHolder#CAH_DAC
        CollectibleAssemblyHolder<DomainAssembly *> pDomainAssembly;
        if (!m_methodIter.Next(pDomainAssembly.This()))
        {
            m_appDomain = NULL;
            goto NextDomain;
        }
    }

    if (!m_methodIter.Current()->HasNativeCode())
    {
        goto NextMethod;
    }

    *instance = new (nothrow)
        ClrDataMethodInstance(dac,
                              m_appDomain,
                              m_methodIter.Current());
    return *instance ? S_OK : E_OUTOFMEMORY;
}

HRESULT
EnumMethodInstances::CdStart(MethodDesc* methodDesc,
                             IXCLRDataAppDomain* appDomain,
                             CLRDATA_ENUM* handle)
{
    if (!methodDesc->HasClassOrMethodInstantiation() &&
        !methodDesc->HasNativeCode())
    {
        *handle = 0;
        return S_FALSE;
    }

    EnumMethodInstances* iter = new (nothrow)
        EnumMethodInstances(methodDesc, appDomain);
    if (iter)
    {
        *handle = TO_CDENUM(iter);
        return S_OK;
    }
    else
    {
        *handle = 0;
        return E_OUTOFMEMORY;
    }
}

HRESULT
EnumMethodInstances::CdNext(ClrDataAccess* dac,
                            CLRDATA_ENUM* handle,
                            IXCLRDataMethodInstance** method)
{
    EnumMethodInstances* iter = FROM_CDENUM(EnumMethodInstances, *handle);
    if (!iter)
    {
        return S_FALSE;
    }

    return iter->Next(dac, method);
}

HRESULT
EnumMethodInstances::CdEnd(CLRDATA_ENUM handle)
{
    EnumMethodInstances* iter = FROM_CDENUM(EnumMethodInstances, handle);
    if (iter)
    {
        delete iter;
        return S_OK;
    }
    else
    {
        return E_INVALIDARG;
    }
}
