// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//*****************************************************************************
// File: stack.cpp
//

//
// CLRData stack walking.
//
//*****************************************************************************

#include "stdafx.h"

//----------------------------------------------------------------------------
//
// ClrDataStackWalk.
//
//----------------------------------------------------------------------------

ClrDataStackWalk::ClrDataStackWalk(ClrDataAccess* dac,
                                   Thread* thread,
                                   ULONG32 flags)
{
    m_dac = dac;
    m_dac->AddRef();
    m_instanceAge = m_dac->m_instanceAge;
    m_thread = thread;
    m_walkFlags = flags;
    m_refs = 1;
    m_stackPrev = 0;

    INDEBUG( m_framesUnwound = 0; )
}

ClrDataStackWalk::~ClrDataStackWalk(void)
{
    m_dac->Release();
}

STDMETHODIMP
ClrDataStackWalk::QueryInterface(THIS_
                                 IN REFIID interfaceId,
                                 OUT PVOID* iface)
{
    if (IsEqualIID(interfaceId, IID_IUnknown) ||
        IsEqualIID(interfaceId, __uuidof(IXCLRDataStackWalk)))
    {
        AddRef();
        *iface = static_cast<IUnknown*>
            (static_cast<IXCLRDataStackWalk*>(this));
        return S_OK;
    }
    else
    {
        *iface = NULL;
        return E_NOINTERFACE;
    }
}

STDMETHODIMP_(ULONG)
ClrDataStackWalk::AddRef(THIS)
{
    return InterlockedIncrement(&m_refs);
}

STDMETHODIMP_(ULONG)
ClrDataStackWalk::Release(THIS)
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
ClrDataStackWalk::GetContext(
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
        if (!m_frameIter.IsValid())
        {
            status = S_FALSE;
        }
        else
        {
            *(PT_CONTEXT)contextBuf = m_context;
            UpdateContextFromRegDisp(&m_regDisp, (PT_CONTEXT)contextBuf);
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
ClrDataStackWalk::SetContext(
    /* [in] */ ULONG32 contextSize,
    /* [size_is][in] */ BYTE context[  ])
{
    return SetContext2(m_frameIter.m_crawl.IsActiveFrame() ?
                       CLRDATA_STACK_SET_CURRENT_CONTEXT :
                       CLRDATA_STACK_SET_UNWIND_CONTEXT,
                       contextSize, context);
}

HRESULT STDMETHODCALLTYPE
ClrDataStackWalk::SetContext2(
    /* [in] */ ULONG32 flags,
    /* [in] */ ULONG32 contextSize,
    /* [size_is][in] */ BYTE context[  ])
{
    HRESULT status;

    if ((flags & ~(CLRDATA_STACK_SET_CURRENT_CONTEXT |
                   CLRDATA_STACK_SET_UNWIND_CONTEXT)) != 0 ||
        !CheckContextSizeForBuffer(contextSize, context))
    {
        return E_INVALIDARG;
    }

    DAC_ENTER_SUB(m_dac);

    EX_TRY
    {
        // Copy the context to local state so
        // that its lifetime extends beyond this call.
        m_context = *(PT_CONTEXT)context;
        m_thread->FillRegDisplay(&m_regDisp, &m_context);
        m_frameIter.ResetRegDisp(&m_regDisp, (flags & CLRDATA_STACK_SET_CURRENT_CONTEXT) != 0);
        m_stackPrev = (TADDR)GetRegdisplaySP(&m_regDisp);
        FilterFrames();
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
ClrDataStackWalk::Next(void)
{
    HRESULT status = E_FAIL;

    INDEBUG( static const int kFrameToReturnForever = 56; )

    DAC_ENTER_SUB(m_dac);

    EX_TRY
    {
        if (!m_frameIter.IsValid())
        {
            status = S_FALSE;
        }
        else
#if defined(_DEBUG)
            // m_framesUnwound is not incremented unless the special config value is set below in this function.
             if (m_framesUnwound < kFrameToReturnForever)
#endif // defined(_DEBUG)
        {
            // Default the previous stack value.
            m_stackPrev = (TADDR)GetRegdisplaySP(&m_regDisp);
            StackWalkAction action = m_frameIter.Next();
            switch(action)
            {
            case SWA_CONTINUE:
                // We successfully unwound a frame so update
                // the previous stack pointer before going into
                // filtering to get the amount of stack skipped
                // by the filtering.
                m_stackPrev = (TADDR)GetRegdisplaySP(&m_regDisp);
                FilterFrames();
                status = m_frameIter.IsValid() ? S_OK : S_FALSE;
                break;
            case SWA_ABORT:
                status = S_FALSE;
                break;
            default:
                status = E_FAIL;
                break;
            }
        }

#if defined(_DEBUG)
        // Test hook: when testing on debug builds, we want an easy way to test that the target
        //  stack behaves as if it's smashed in a particular way.  It would be very difficult to create
        //  a test that carefully broke the stack in a way that would force the stackwalker to report
        //  success on the same frame forever, and have that corruption be reliable over time.  However, it's
        //  pretty easy for us to control the number of frames on the stack for tests that use this specific
        //  internal flag.
       if (CLRConfig::GetConfigValue(CLRConfig::INTERNAL_DumpGeneration_IntentionallyCorruptDataFromTarget))
       {
            if (m_framesUnwound >= kFrameToReturnForever)
            {
                status = S_OK;
            }
            else
            {
                m_framesUnwound++;
            }
        }
#endif // defined(_DEBUG)
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
ClrDataStackWalk::GetStackSizeSkipped(
    /* [out] */ ULONG64 *stackSizeSkipped)
{
    HRESULT status;

    DAC_ENTER_SUB(m_dac);

    EX_TRY
    {
        if (m_stackPrev)
        {
            *stackSizeSkipped =
                (TADDR)GetRegdisplaySP(&m_regDisp) - m_stackPrev;
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
ClrDataStackWalk::GetFrameType(
    /* [out] */ CLRDataSimpleFrameType *simpleType,
    /* [out] */ CLRDataDetailedFrameType *detailedType)
{
    HRESULT status;

    DAC_ENTER_SUB(m_dac);

    EX_TRY
    {
        if (m_frameIter.IsValid())
        {
            RawGetFrameType(simpleType, detailedType);
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
ClrDataStackWalk::GetFrame(
    /* [out] */ IXCLRDataFrame **frame)
{
    HRESULT status;

    DAC_ENTER_SUB(m_dac);

    EX_TRY
    {
        ClrDataFrame* dataFrame = NULL;
        if (!m_frameIter.IsValid())
        {
            status = E_INVALIDARG;
            goto Exit;
        }

        CLRDataSimpleFrameType simpleType;
        CLRDataDetailedFrameType detailedType;

        RawGetFrameType(&simpleType, &detailedType);
        dataFrame =
            new (nothrow) ClrDataFrame(m_dac, simpleType, detailedType,
                                       AppDomain::GetCurrentDomain(),
                                       m_frameIter.m_crawl.GetFunction());
        if (!dataFrame)
        {
            status = E_OUTOFMEMORY;
            goto Exit;
        }

        dataFrame->m_context = m_context;
        UpdateContextFromRegDisp(&m_regDisp, &dataFrame->m_context);
        m_thread->FillRegDisplay(&dataFrame->m_regDisp,
                                 &dataFrame->m_context);

        *frame = static_cast<IXCLRDataFrame*>(dataFrame);
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

HRESULT STDMETHODCALLTYPE
ClrDataStackWalk::Request(
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

        case CLRDATA_STACK_WALK_REQUEST_SET_FIRST_FRAME:
            // This code should be removed once the Windows debuggers stop using the old DAC API.
            if ((inBufferSize != sizeof(ULONG32)) ||
                (outBufferSize != 0))
            {
                status = E_INVALIDARG;
                break;
            }

            m_frameIter.SetIsFirstFrame(*(ULONG32 UNALIGNED *)inBuffer != 0);
            status = S_OK;
            break;

        case DACSTACKPRIV_REQUEST_FRAME_DATA:
            if ((inBufferSize != 0) ||
                (inBuffer != NULL) ||
                (outBufferSize != sizeof(DacpFrameData)))
            {
                status = E_INVALIDARG;
                break;
            }
            if (!m_frameIter.IsValid())
            {
                status = E_INVALIDARG;
                break;
            }

            DacpFrameData* frameData;

            frameData = (DacpFrameData*)outBuffer;
            frameData->frameAddr =
                TO_CDADDR(PTR_HOST_TO_TADDR(m_frameIter.m_crawl.GetFrame()));
            status = S_OK;
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
ClrDataStackWalk::Init(void)
{
    if (m_thread->IsUnstarted())
    {
        return E_FAIL;
    }

    if (m_thread->GetFilterContext())
    {
        m_context = *m_thread->GetFilterContext();
    }
    else
    {
        DacGetThreadContext(m_thread, &m_context);
    }
    m_thread->FillRegDisplay(&m_regDisp, &m_context);

    m_stackPrev = (TADDR)GetRegdisplaySP(&m_regDisp);

    ULONG32 iterFlags = NOTIFY_ON_NO_FRAME_TRANSITIONS;

    // If the filter is only allowing method frames
    // turn on the appropriate iterator flag.
    if ((m_walkFlags & SIMPFRAME_ALL) ==
        CLRDATA_SIMPFRAME_MANAGED_METHOD)
    {
        iterFlags |= FUNCTIONSONLY;
    }

    m_frameIter.Init(m_thread, NULL, &m_regDisp, iterFlags);
    if (m_frameIter.GetFrameState() == StackFrameIterator::SFITER_UNINITIALIZED)
    {
        return E_FAIL;
    }
    FilterFrames();

    return S_OK;
}

void
ClrDataStackWalk::FilterFrames(void)
{
    //
    // Advance to a state compatible with the
    // current filtering flags.
    //

    while (m_frameIter.IsValid())
    {
        switch(m_frameIter.GetFrameState())
        {
        case StackFrameIterator::SFITER_FRAMELESS_METHOD:
            if (m_walkFlags & CLRDATA_SIMPFRAME_MANAGED_METHOD)
            {
                return;
            }
            break;
        case StackFrameIterator::SFITER_FRAME_FUNCTION:
        case StackFrameIterator::SFITER_SKIPPED_FRAME_FUNCTION:
        case StackFrameIterator::SFITER_NO_FRAME_TRANSITION:
            if (m_walkFlags & CLRDATA_SIMPFRAME_RUNTIME_UNMANAGED_CODE)
            {
                return;
            }
            break;
        default:
            break;
        }

        m_frameIter.Next();
    }
}

void
ClrDataStackWalk::RawGetFrameType(
    /* [out] */ CLRDataSimpleFrameType* simpleType,
    /* [out] */ CLRDataDetailedFrameType* detailedType)
{
    if (simpleType)
    {
        switch(m_frameIter.GetFrameState())
        {
        case StackFrameIterator::SFITER_FRAMELESS_METHOD:
            *simpleType = CLRDATA_SIMPFRAME_MANAGED_METHOD;
            break;
        case StackFrameIterator::SFITER_FRAME_FUNCTION:
        case StackFrameIterator::SFITER_SKIPPED_FRAME_FUNCTION:
            *simpleType = CLRDATA_SIMPFRAME_RUNTIME_UNMANAGED_CODE;
            break;
        default:
            *simpleType = CLRDATA_SIMPFRAME_UNRECOGNIZED;
            break;
        }
    }

    if (detailedType)
    {
        if (m_frameIter.m_crawl.GetFrame() && m_frameIter.m_crawl.GetFrame()->GetFrameAttribs() & Frame::FRAME_ATTR_EXCEPTION)
            *detailedType = CLRDATA_DETFRAME_EXCEPTION_FILTER;
        else
            *detailedType = CLRDATA_DETFRAME_UNRECOGNIZED;
    }
}

//----------------------------------------------------------------------------
//
// ClrDataFrame.
//
//----------------------------------------------------------------------------

ClrDataFrame::ClrDataFrame(ClrDataAccess* dac,
                           CLRDataSimpleFrameType simpleType,
                           CLRDataDetailedFrameType detailedType,
                           AppDomain* appDomain,
                           MethodDesc* methodDesc)
{
    m_dac = dac;
    m_dac->AddRef();
    m_instanceAge = m_dac->m_instanceAge;
    m_simpleType = simpleType;
    m_detailedType = detailedType;
    m_appDomain = appDomain;
    m_methodDesc = methodDesc;
    m_refs = 1;
    m_methodSig = NULL;
    m_localSig = NULL;
}

ClrDataFrame::~ClrDataFrame(void)
{
    delete m_methodSig;
    delete m_localSig;
    m_dac->Release();
}

STDMETHODIMP
ClrDataFrame::QueryInterface(THIS_
                             IN REFIID interfaceId,
                             OUT PVOID* iface)
{
    if (IsEqualIID(interfaceId, IID_IUnknown) ||
        IsEqualIID(interfaceId, __uuidof(IXCLRDataFrame)))
    {
        AddRef();
        *iface = static_cast<IUnknown*>
            (static_cast<IXCLRDataFrame*>(this));
        return S_OK;
    }
    else if (IsEqualIID(interfaceId, __uuidof(IXCLRDataFrame2)))
    {
        AddRef();
        *iface = static_cast<IUnknown*>
            (static_cast<IXCLRDataFrame2*>(this));
        return S_OK;
    }
    else
    {
        *iface = NULL;
        return E_NOINTERFACE;
    }
}

STDMETHODIMP_(ULONG)
ClrDataFrame::AddRef(THIS)
{
    return InterlockedIncrement(&m_refs);
}

STDMETHODIMP_(ULONG)
ClrDataFrame::Release(THIS)
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
ClrDataFrame::GetContext(
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
        *(PT_CONTEXT)contextBuf = m_context;
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
ClrDataFrame::GetFrameType(
    /* [out] */ CLRDataSimpleFrameType *simpleType,
    /* [out] */ CLRDataDetailedFrameType *detailedType)
{
    HRESULT status;

    DAC_ENTER_SUB(m_dac);

    EX_TRY
    {
        *simpleType = m_simpleType;
        *detailedType = m_detailedType;
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
ClrDataFrame::GetAppDomain(
    /* [out] */ IXCLRDataAppDomain **appDomain)
{
    HRESULT status;

    DAC_ENTER_SUB(m_dac);

    EX_TRY
    {
        if (m_appDomain)
        {
            ClrDataAppDomain* dataAppDomain =
                new (nothrow) ClrDataAppDomain(m_dac, m_appDomain);
            if (!dataAppDomain)
            {
                status = E_OUTOFMEMORY;
            }
            else
            {
                *appDomain = static_cast<IXCLRDataAppDomain*>(dataAppDomain);
                status = S_OK;
            }
        }
        else
        {
            *appDomain = NULL;
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
ClrDataFrame::GetNumArguments(
    /* [out] */ ULONG32 *numArgs)
{
    HRESULT status;

    DAC_ENTER_SUB(m_dac);

    EX_TRY
    {
        if (!m_methodDesc)
        {
            status = E_NOINTERFACE;
        }
        else
        {
            MetaSig* sig;

            status = GetMethodSig(&sig, numArgs);
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
ClrDataFrame::GetArgumentByIndex(
    /* [in] */ ULONG32 index,
    /* [out] */ IXCLRDataValue **arg,
    /* [in] */ ULONG32 bufLen,
    /* [out] */ ULONG32 *nameLen,
    /* [size_is][out] */ _Out_writes_to_opt_(bufLen, *nameLen) WCHAR name[  ])
{
    HRESULT status;

    DAC_ENTER_SUB(m_dac);

    EX_TRY
    {
        if (nameLen)
        {
            *nameLen = 0;
        }

        if (!m_methodDesc)
        {
            status = E_NOINTERFACE;
            goto Exit;
        }

        MetaSig* sig;
        ULONG32 numArgs;

        if (FAILED(status = GetMethodSig(&sig, &numArgs)))
        {
            goto Exit;
        }

        if (index >= numArgs)
        {
            status = E_INVALIDARG;
            goto Exit;
        }

        if ((bufLen && name) || nameLen)
        {
            if (index == 0 && sig->HasThis())
            {
                if (nameLen)
                {
                    *nameLen = 5;
                }

                StringCchCopy(name, bufLen, W("this"));
            }
            else
            {
                if (!m_methodDesc->IsNoMetadata())
                {
                    IMDInternalImport* mdImport = m_methodDesc->GetMDImport();
                    mdParamDef paramToken;
                    LPCSTR paramName;
                    USHORT seq;
                    DWORD attr;

                    // Param indexing is 1-based.
                    ULONG32 mdIndex = index + 1;

                    // 'this' doesn't show up in the signature but
                    // is present in the dac API indexing so adjust the
                    // index down for methods with 'this'.
                    if (sig->HasThis())
                    {
                        mdIndex--;
                    }

                    status = mdImport->FindParamOfMethod(
                        m_methodDesc->GetMemberDef(),
                        mdIndex,
                        &paramToken);
                    if (status == S_OK)
                    {
                        status = mdImport->GetParamDefProps(
                            paramToken,
                            &seq,
                            &attr,
                            &paramName);
                        if ((status == S_OK) && (paramName != NULL))
                        {
                            if ((status = ConvertUtf8(paramName,
                                                      bufLen, nameLen, name)) != S_OK)
                            {
                                goto Exit;
                            }
                        }
                    }
                }
                else
                {
                    if (nameLen)
                    {
                        *nameLen = 1;
                    }

                    name[0] = 0;
                }
            }
        }

        status = ValueFromDebugInfo(sig, true, index, index, arg);

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
ClrDataFrame::GetNumLocalVariables(
    /* [out] */ ULONG32 *numLocals)
{
    HRESULT status;

    DAC_ENTER_SUB(m_dac);

    EX_TRY
    {
        if (!m_methodDesc)
        {
            status = E_NOINTERFACE;
        }
        else
        {
            MetaSig* sig;

            status = GetLocalSig(&sig, numLocals);
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
ClrDataFrame::GetLocalVariableByIndex(
    /* [in] */ ULONG32 index,
    /* [out] */ IXCLRDataValue **localVariable,
    /* [in] */ ULONG32 bufLen,
    /* [out] */ ULONG32 *nameLen,
    /* [size_is][out] */ _Out_writes_to_opt_(bufLen, *nameLen) WCHAR name[  ])
{
    HRESULT status;

    DAC_ENTER_SUB(m_dac);

    EX_TRY
    {
        if (!m_methodDesc)
        {
            status = E_NOINTERFACE;
            goto Exit;
        }

        MetaSig* sig;
        ULONG32 numLocals;

        if (FAILED(status = GetLocalSig(&sig, &numLocals)))
        {
            goto Exit;
        }

        if (index >= numLocals)
        {
            status = E_INVALIDARG;
            goto Exit;
        }

        MetaSig* argSig;
        ULONG32 numArgs;

        if (FAILED(status = GetMethodSig(&argSig, &numArgs)))
        {
            goto Exit;
        }

        // Can't get names for locals in the Whidbey runtime.
        if (bufLen && name)
        {
            if (nameLen)
            {
                *nameLen = 1;
            }

            name[0] = 0;
        }

        // The locals are indexed immediately following the arguments
        // in the NativeVarInfos.
        status = ValueFromDebugInfo(sig, false, index, index + numArgs,
                                    localVariable);

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
ClrDataFrame::GetNumTypeArguments(
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
ClrDataFrame::GetTypeArgumentByIndex(
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
ClrDataFrame::GetExactGenericArgsToken(
    /* [out] */ IXCLRDataValue ** genericToken)
{
    HRESULT status;

    DAC_ENTER_SUB(m_dac);

    EX_TRY
    {
        if (!m_methodDesc)
        {
            status = E_NOINTERFACE;
            goto Exit;
        }

        MetaSig* sig;
        ULONG32 numLocals;

        if (FAILED(status = GetLocalSig(&sig, &numLocals)))
        {
            goto Exit;
        }

        // The locals are indexed immediately following the arguments
        // in the NativeVarInfos.
        status = ValueFromDebugInfo(sig, false, 1, (DWORD)ICorDebugInfo::TYPECTXT_ILNUM,
                                    genericToken);
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
ClrDataFrame::GetCodeName(
    /* [in] */ ULONG32 flags,
    /* [in] */ ULONG32 bufLen,
    /* [out] */ ULONG32 *symbolLen,
    /* [size_is][out] */ _Out_writes_bytes_opt_(bufLen) WCHAR symbolBuf[  ])
{
    HRESULT status = E_FAIL;

    DAC_ENTER_SUB(m_dac);

    EX_TRY
    {
        TADDR pcAddr = PCODEToPINSTR(GetControlPC(&m_regDisp));
        status = m_dac->
            RawGetMethodName(TO_CDADDR(pcAddr), flags,
                             bufLen, symbolLen, symbolBuf,
                             NULL);
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
ClrDataFrame::GetMethodInstance(
    /* [out] */ IXCLRDataMethodInstance **method)
{
    HRESULT status;

    DAC_ENTER_SUB(m_dac);

    EX_TRY
    {
        if (!m_methodDesc)
        {
            status = E_NOINTERFACE;
        }
        else
        {
            ClrDataMethodInstance* dataMethod =
                new (nothrow) ClrDataMethodInstance(m_dac,
                                                    m_appDomain,
                                                    m_methodDesc);
            *method = static_cast<IXCLRDataMethodInstance*>(dataMethod);
            status = dataMethod ? S_OK : E_OUTOFMEMORY;
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
ClrDataFrame::Request(
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
ClrDataFrame::GetMethodSig(MetaSig** sig,
                           ULONG32* count)
{
    if (!m_methodSig)
    {
        m_methodSig = new (nothrow) MetaSig(m_methodDesc);
        if (!m_methodSig)
        {
            return E_OUTOFMEMORY;
        }
    }

    *sig = m_methodSig;
    *count = m_methodSig->NumFixedArgs() +
        (m_methodSig->HasThis() ? 1 : 0);
    return *count ? S_OK : S_FALSE;
}

HRESULT
ClrDataFrame::GetLocalSig(MetaSig** sig,
                          ULONG32* count)
{
    HRESULT hr;
    if (!m_localSig)
    {
        // It turns out we cannot really get rid of this check.  Dynamic methods
        // (including IL stubs) do not have their local sig's available after JIT time.
        if (!m_methodDesc->IsIL())
        {
            *sig = NULL;
            *count = 0;
            return E_FAIL;
        }

        COR_ILMETHOD_DECODER methodDecoder(m_methodDesc->GetILHeader());
        mdSignature localSig = methodDecoder.GetLocalVarSigTok() ?
            methodDecoder.GetLocalVarSigTok() : mdSignatureNil;
        if (localSig == mdSignatureNil)
        {
            *sig = NULL;
            *count = 0;
            return E_FAIL;
        }

        ULONG tokenSigLen;
        PCCOR_SIGNATURE tokenSig;
        IfFailRet(m_methodDesc->GetModule()->GetMDImport()->GetSigFromToken(
            localSig,
            &tokenSigLen,
            &tokenSig));

        SigTypeContext typeContext(m_methodDesc, TypeHandle());
        m_localSig = new (nothrow)
            MetaSig(tokenSig,
                    tokenSigLen,
                    m_methodDesc->GetModule(),
                    &typeContext,
                    MetaSig::sigLocalVars);
        if (!m_localSig)
        {
            return E_OUTOFMEMORY;
        }
    }

    *sig = m_localSig;
    *count = m_localSig->NumFixedArgs();
    return S_OK;
}

HRESULT
ClrDataFrame::ValueFromDebugInfo(MetaSig* sig,
                                 bool isArg,
                                 DWORD sigIndex,
                                 DWORD varInfoSlot,
                                 IXCLRDataValue** _value)
{
    HRESULT status;
    ULONG32 numVarInfo;
    NewHolder<ICorDebugInfo::NativeVarInfo> varInfo(NULL);
    ULONG32 codeOffset;
    ULONG32 valueFlags;
    ULONG32 i;

    TADDR ip = PCODEToPINSTR(GetControlPC(&m_regDisp));
    if ((status = m_dac->GetMethodVarInfo(m_methodDesc,
                                          ip,
                                          &numVarInfo,
                                          &varInfo,
                                          &codeOffset)) != S_OK)
    {
        // We have signature info indicating that there
        // are values, but couldn't find any location info.
        // Optimized routines may have eliminated all
        // traditional variable locations, so just treat
        // this as a no-location case just like not being
        // able to find a matching lifetime.
        numVarInfo = 0;
    }

    for (i = 0; i < numVarInfo; i++)
    {
        if (varInfo[i].startOffset <= codeOffset &&
            varInfo[i].endOffset >= codeOffset &&
            varInfo[i].varNumber == varInfoSlot &&
            varInfo[i].loc.vlType != ICorDebugInfo::VLT_INVALID)
        {
            break;
        }
    }

    ULONG64 baseAddr;
    NativeVarLocation locs[MAX_NATIVE_VAR_LOCS];
    ULONG32 numLocs;

    if (i >= numVarInfo)
    {
        numLocs = 0;
    }
    else
    {
        numLocs = NativeVarLocations(varInfo[i].loc, &m_context,
                                     ARRAY_SIZE(locs), locs);
    }

    if (numLocs == 1 && !locs[0].contextReg)
    {
        baseAddr = TO_CDADDR(locs[0].addr);
    }
    else
    {
        baseAddr = 0;
    }

    TypeHandle argType;

    sig->Reset();
    if (isArg && sigIndex == 0 && sig->HasThis())
    {
        argType = TypeHandle(m_methodDesc->GetMethodTable());
        valueFlags = CLRDATA_VALUE_IS_REFERENCE;
    }
    else
    {
        // 'this' doesn't show up in the signature but
        // is present in the indexing so adjust the
        // index down for methods with 'this'.
        if (isArg && sig->HasThis())
        {

            sigIndex--;
        }

        do
        {
            sig->NextArg();
        }
        while (sigIndex-- > 0);

        // == FailIfNotLoaded
        // Will also return null if type is not restored
        argType = sig->GetLastTypeHandleThrowing(ClassLoader::DontLoadTypes);
        if (argType.IsNull())
        {
            // XXX Microsoft - Sometimes types can't be looked
            // up and this at least allows the value to be used,
            // but is it the right behavior?
            argType = TypeHandle(CoreLibBinder::GetElementType(ELEMENT_TYPE_U8));
            valueFlags = 0;
        }
        else
        {
            valueFlags = GetTypeFieldValueFlags(argType, NULL, 0, false);

            // If this is a primitive variable and the actual size is smaller than what we have been told,
            // then lower the size so that we won't read in trash memory (e.g. reading 4 bytes for a short).
            if ((valueFlags & CLRDATA_VALUE_IS_PRIMITIVE) != 0)
            {
                if (numLocs == 1)
                {
                    UINT actualSize = argType.GetSize();
                    if (actualSize < locs[0].size)
                    {
                        locs[0].size = actualSize;
                    }
                }
            }
        }
    }

    ClrDataValue* value = new (nothrow)
        ClrDataValue(m_dac,
                     m_appDomain,
                     NULL,
                     valueFlags,
                     argType,
                     baseAddr,
                     numLocs,
                     locs);
    if (!value)
    {
        return E_OUTOFMEMORY;
    }

    *_value = value;
    return S_OK;
}
