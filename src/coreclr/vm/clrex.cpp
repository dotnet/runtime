// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

//
// ---------------------------------------------------------------------------
// Clrex.cpp
// ---------------------------------------------------------------------------


#include "common.h"
#include "clrex.h"
#include "field.h"
#include "eetoprofinterfacewrapper.inl"
#include "typestring.h"
#include "sigformat.h"
#include "eeconfig.h"

#ifdef FEATURE_EH_FUNCLETS
#include "exceptionhandling.h"
#endif // FEATURE_EH_FUNCLETS

#ifdef FEATURE_COMINTEROP
#include "interoputil.inl"
#endif // FEATURE_COMINTEROP

// ---------------------------------------------------------------------------
// CLRException methods
// ---------------------------------------------------------------------------

CLRException::~CLRException()
{
    CONTRACTL
    {
        GC_NOTRIGGER;
        NOTHROW;
        MODE_ANY;
        if (GetThrowableHandle() == NULL)
        {
            CANNOT_TAKE_LOCK;
        }
        else
        {
            CAN_TAKE_LOCK;         // because of DestroyHandle
        }
    }
    CONTRACTL_END;

    OBJECTHANDLE throwableHandle = GetThrowableHandle();
    if (throwableHandle != NULL)
    {
        STRESS_LOG1(LF_EH, LL_INFO100, "CLRException::~CLRException destroying throwable: obj = %x\n", GetThrowableHandle());
        // clear the handle first, so if we SO on destroying it, we don't have a dangling reference
        SetThrowableHandle(NULL);
        DestroyHandle(throwableHandle);
    }
}

OBJECTREF CLRException::GetThrowable()
{
    CONTRACTL
    {
        GC_TRIGGERS;
        NOTHROW;
        MODE_COOPERATIVE;
        FORBID_FAULT;
    }
    CONTRACTL_END;

    OBJECTREF throwable = NULL;

    if (NingenEnabled())
    {
        return NULL;
    }

    Thread *pThread = GetThread();

    if (pThread->IsRudeAbortInitiated()) {
        return GetBestThreadAbortException();
    }

    if ((IsType(CLRLastThrownObjectException::GetType()) &&
         pThread->LastThrownObject() == GetPreallocatedStackOverflowException()))
    {
        return GetPreallocatedStackOverflowException();
    }

    OBJECTHANDLE oh = GetThrowableHandle();
    if (oh != NULL)
    {
        return ObjectFromHandle(oh);
    }

    Exception *pLastException = pThread->m_pCreatingThrowableForException;
    if (pLastException != NULL)
    {
        if (IsSameInstanceType(pLastException))
        {
#if defined(_DEBUG)
            static int BreakOnExceptionInGetThrowable = -1;
            if (BreakOnExceptionInGetThrowable == -1)
            {
                BreakOnExceptionInGetThrowable = CLRConfig::GetConfigValue(CLRConfig::INTERNAL_BreakOnExceptionInGetThrowable);
            }
            if (BreakOnExceptionInGetThrowable)
            {
                _ASSERTE(!"BreakOnExceptionInGetThrowable");
            }
            LOG((LF_EH, LL_INFO100, "GetThrowable: Exception in GetThrowable, translating to a preallocated exception.\n"));
#endif // _DEBUG
            // Look at the type of GET_EXCEPTION() and see if it is OOM or SO.
            if (IsPreallocatedOOMException())
            {
                throwable = GetPreallocatedOutOfMemoryException();
            }
            else if (GetInstanceType() == EEException::GetType() && GetHR() == COR_E_THREADABORTED)
            {
                // the only use for thread abort in coreclr is when debugger needs to tear down
                // a runaway expression evaluation.
                // it would not escalate OOM into another abort, so OOM in low memory case is appropriate.
                throwable = GetBestThreadAbortException();
            }
            else
            {
                // I am not convinced if this case is actually a fatal error in the runtime.
                // There have been two bugs in early 2006 (VSW 575647 and 575650) that came in here,
                // both because of OOM and resulted in the ThreadAbort clause above being added since
                // we were creating a ThreadAbort throwable that, due to OOM, got us on a path
                // which came here. Both were valid execution paths and scenarios and not a fatal condition.
                //
                // I am tempted to return preallocated OOM from here but my concern is that it *may*
                // result in fake OOM exceptions being thrown that could break valid scenarios.
                //
                // Hence, we return preallocated System.Exception instance. Lossy information is better
                // than wrong or no information (or even FailFast).
                _ASSERTE (!"Recursion in CLRException::GetThrowable");

                // We didn't recognize it, so use the preallocated System.Exception instance.
                STRESS_LOG0(LF_EH, LL_INFO100, "CLRException::GetThrowable: Recursion! Translating to preallocated System.Exception.\n");
                throwable = GetBestBaseException(); 
            }
        }
    }

    GCPROTECT_BEGIN(throwable);

    if (throwable == NULL)
    {
        class RestoreLastException
        {
            Thread *m_pThread;
            Exception *m_pLastException;
        public:
            RestoreLastException(Thread *pThread, Exception *pException)
            {
                m_pThread = pThread;
                m_pLastException = m_pThread->m_pCreatingThrowableForException;
                m_pThread->m_pCreatingThrowableForException = pException;
            }
            ~RestoreLastException()
            {
                m_pThread->m_pCreatingThrowableForException = m_pLastException;
            }
        };

        RestoreLastException restore(pThread, this);

        EX_TRY
        {
            FAULT_NOT_FATAL();
            throwable = CreateThrowable();
        }
        EX_CATCH
        {
            // This code used to be this line:
            //      throwable = GET_THROWABLE();
            // GET_THROWABLE() expands to CLRException::GetThrowable(GET_EXCEPTION()),
            //  (where GET_EXCEPTION() refers to the exception that was thrown from
            //  CreateThrowable() and is being caught in this EX_TRY/EX_CATCH.)
            //  If that exception is the same as the one for which this GetThrowable()
            //  was called, we're in a recursive situation.
            // Since the CreateThrowable() call should return a type from CoreLib,
            //  there really shouldn't be much opportunity for error.  We could be
            //  out of memory, we could overflow the stack, or the runtime could
            //  be in a weird state(the thread could be aborted as well).
            // Because we've seen a number of recursive death bugs here, just look
            //  explicitly for OOM and SO, and otherwise use ExecutionEngineException.

            // Check whether the exception from CreateThrowable() is the same as the current
            //  exception.  If not, call GetThrowable(), otherwise, settle for a
            //  preallocated exception.
            Exception *pException = GET_EXCEPTION();

            if (GetHR() == COR_E_THREADABORTED)
            {
                // the only use for thread abort in coreclr is when debugger needs to tear down
                // a runaway expression evaluation.
                // it would not escalate OOM into another abort, so OOM in low memory case is appropriate.
                throwable = GetBestThreadAbortException();
            }
            else
            {
                throwable = CLRException::GetThrowableFromException(pException);
            }
        }
        EX_END_CATCH(SwallowAllExceptions)

    }

    {
        if (throwable == NULL)
        {
            STRESS_LOG0(LF_EH, LL_INFO100, "CLRException::GetThrowable: We have failed to track exceptions accurately through the system.\n");

            // There's no reason to believe that it is an OOM.  A better choice is ExecutionEngineException.
            // We have failed to track exceptions accurately through the system.  However, it's arguably
            // better to give the wrong exception object than it is to rip the process.  So let's leave
            // it as an Assert for now and convert it to ExecutionEngineException in the next release.

            // SQL Stress is hitting the assert.  We want to remove it, so that we can see if there are further errors
            //  masked by the assert.
            // _ASSERTE(FALSE);

            throwable = GetPreallocatedOutOfMemoryException();
        }

        EX_TRY
        {
            SetThrowableHandle(GetAppDomain()->CreateHandle(throwable));
            if (m_innerException != NULL && !CLRException::IsPreallocatedExceptionObject(throwable))
            {
                // Only set inner exception if the exception is not preallocated.
                FAULT_NOT_FATAL();

                // If inner exception is not empty, then set the managed exception's
                // _innerException field properly
                OBJECTREF throwableValue = CLRException::GetThrowableFromException(m_innerException);
                ((EXCEPTIONREF)throwable)->SetInnerException(throwableValue);
            }

        }
        EX_CATCH
        {
            // No matter... we just don't get to cache the throwable.
        }
        EX_END_CATCH(SwallowAllExceptions)
    }

    GCPROTECT_END();

    return throwable;
}

HRESULT CLRException::GetHR()
{
    CONTRACTL
    {
        DISABLED(NOTHROW);
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    GCX_COOP();
    return GetExceptionHResult(GetThrowable());
}

#ifdef FEATURE_COMINTEROP
HRESULT CLRException::SetErrorInfo()
{
   CONTRACTL
    {
        GC_TRIGGERS;
        NOTHROW;
        MODE_ANY;
    }
    CONTRACTL_END;

    HRESULT hr = S_OK;

    IErrorInfo *pErrorInfo = NULL;

    // Try to get IErrorInfo
    EX_TRY
    {
        pErrorInfo = GetErrorInfo();
    }
    EX_CATCH
    {
        // Since there was an exception getting IErrorInfo get the exception's HR so
        // that we return it back to the caller as the new exception.
        hr = GET_EXCEPTION()->GetHR();
        pErrorInfo = NULL;
        LOG((LF_EH, LL_INFO100, "CLRException::SetErrorInfo: caught exception (hr = %08X) while trying to get IErrorInfo\n", hr));
    }
    EX_END_CATCH(SwallowAllExceptions)

    if (!pErrorInfo)
    {
        // Return the HR to the caller if we dont get IErrorInfo - if the HR is E_NOINTERFACE, then
        // there was no IErrorInfo available. If its anything else, it implies we failed to get the
        // interface and have the HR corresponding to the exception we took while trying to get IErrorInfo.
        return hr;
    }
    else
    {
        GCX_PREEMP();

        EX_TRY
        {
            ::SetErrorInfo(0, pErrorInfo);
            pErrorInfo->Release();

            // Success in setting the ErrorInfo on the thread
            hr = S_OK;
        }
        EX_CATCH
        {
            hr = GET_EXCEPTION()->GetHR();
            // Log the failure
            LOG((LF_EH, LL_INFO100, "CLRException::SetErrorInfo: caught exception (hr = %08X) while trying to set IErrorInfo\n", hr));
        }
        EX_END_CATCH(SwallowAllExceptions)
    }

    return hr;
}

IErrorInfo *CLRException::GetErrorInfo()
{
    CONTRACTL
    {
        GC_TRIGGERS;
        THROWS;
        MODE_ANY;
    }
    CONTRACTL_END;

    IErrorInfo *pErrorInfo = NULL;

    // Attempt to get IErrorInfo only if COM is initialized.
    // Not all codepaths expect to have it initialized (e.g. hosting APIs).
    if (g_fComStarted)
    {
        // Get errorinfo only when our SO probe succeeds
        {
            // Switch to coop mode since GetComIPFromObjectRef requires that
            // and we could be here in any mode...
            GCX_COOP();

            OBJECTREF e = NULL;
            GCPROTECT_BEGIN(e);

            e = GetThrowable();

            if (e != NULL)
            {
                pErrorInfo = (IErrorInfo *)GetComIPFromObjectRef(&e, IID_IErrorInfo);
            }

            GCPROTECT_END();
        }
    }
    else
    {
        // Write to the log incase COM isnt initialized.
        LOG((LF_EH, LL_INFO100, "CLRException::GetErrorInfo: exiting since COM is not initialized.\n"));
    }

    // return the IErrorInfo we got...
    return pErrorInfo;
}
#else   // FEATURE_COMINTEROP
IErrorInfo *CLRException::GetErrorInfo()
{
    LIMITED_METHOD_CONTRACT;
    return NULL;
}
HRESULT CLRException::SetErrorInfo()
{
    LIMITED_METHOD_CONTRACT;

    return S_OK;
 }
#endif  // FEATURE_COMINTEROP

void CLRException::GetMessage(SString &result)
{
    CONTRACTL
    {
        GC_TRIGGERS;
        THROWS;
        MODE_ANY;
    }
    CONTRACTL_END;

    GCX_COOP();

    OBJECTREF e = GetThrowable();
    if (e != NULL)
    {
        _ASSERTE(IsException(e->GetMethodTable()));

        GCPROTECT_BEGIN (e);

        STRINGREF message = ((EXCEPTIONREF)e)->GetMessage();

        if (!message)
            result.Clear();
        else
            message->GetSString(result);

        GCPROTECT_END ();
    }
}


OBJECTREF CLRException::GetPreallocatedOutOfMemoryException()
{
    WRAPPER_NO_CONTRACT;
    _ASSERTE(g_pPreallocatedOutOfMemoryException != NULL);
    return ObjectFromHandle(g_pPreallocatedOutOfMemoryException);
}

OBJECTREF CLRException::GetPreallocatedStackOverflowException()
{
    WRAPPER_NO_CONTRACT;
    _ASSERTE(g_pPreallocatedStackOverflowException != NULL);
    return ObjectFromHandle(g_pPreallocatedStackOverflowException);
}

OBJECTREF CLRException::GetPreallocatedExecutionEngineException()
{
    WRAPPER_NO_CONTRACT;
    _ASSERTE(g_pPreallocatedExecutionEngineException != NULL);
    return ObjectFromHandle(g_pPreallocatedExecutionEngineException);
}

OBJECTHANDLE CLRException::GetPreallocatedStackOverflowExceptionHandle()
{
    LIMITED_METHOD_CONTRACT;
    _ASSERTE(g_pPreallocatedStackOverflowException != NULL);
    return g_pPreallocatedStackOverflowException;
}

//
// Returns TRUE if the given object ref is one of the preallocated exception objects.
//
BOOL CLRException::IsPreallocatedExceptionObject(OBJECTREF o)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_COOPERATIVE;
        FORBID_FAULT;
    }
    CONTRACTL_END;

    if ((o == ObjectFromHandle(g_pPreallocatedOutOfMemoryException)) ||
        (o == ObjectFromHandle(g_pPreallocatedStackOverflowException)) ||
        (o == ObjectFromHandle(g_pPreallocatedExecutionEngineException)))
    {
        return TRUE;
    }

    return FALSE;
}

//
// Returns TRUE if the given object ref is one of the preallocated exception handles
//
BOOL CLRException::IsPreallocatedExceptionHandle(OBJECTHANDLE h)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        FORBID_FAULT;
    }
    CONTRACTL_END;

    if ((h == g_pPreallocatedOutOfMemoryException) ||
        (h == g_pPreallocatedStackOverflowException) ||
        (h == g_pPreallocatedExecutionEngineException))
    {
        return TRUE;
    }

    return FALSE;
}

//
// Returns a preallocated handle to match a preallocated exception object, or NULL if the object isn't one of the
// preallocated exception objects.
//
OBJECTHANDLE CLRException::GetPreallocatedHandleForObject(OBJECTREF o)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_COOPERATIVE;
        FORBID_FAULT;
    }
    CONTRACTL_END;

    if (o == ObjectFromHandle(g_pPreallocatedOutOfMemoryException))
    {
        return g_pPreallocatedOutOfMemoryException;
    }
    else if (o == ObjectFromHandle(g_pPreallocatedStackOverflowException))
    {
        return g_pPreallocatedStackOverflowException;
    }
    else if (o == ObjectFromHandle(g_pPreallocatedExecutionEngineException))
    {
        return g_pPreallocatedExecutionEngineException;
    }

    return NULL;
}

// Prefer a new exception if we can make one.  If we cannot, then give back the pre-allocated OOM.
OBJECTREF CLRException::GetBestException(HRESULT hr, PTR_MethodTable mt)
{
    CONTRACTL
    {
        NOTHROW;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;

    OBJECTREF retVal = NULL;

    EX_TRY
    {
        FAULT_NOT_FATAL();

        EXCEPTIONREF pOutOfMemory = (EXCEPTIONREF)AllocateObject(mt);
        pOutOfMemory->SetHResult(hr);
        pOutOfMemory->SetXCode(EXCEPTION_COMPLUS);

        retVal = pOutOfMemory;
    }
    EX_CATCH
    {
        retVal = GetPreallocatedOutOfMemoryException();
    }
    EX_END_CATCH(SwallowAllExceptions)

    _ASSERTE(retVal != NULL);

    return retVal;
}

OBJECTREF CLRException::GetBestOutOfMemoryException()
{
    CONTRACTL
    {
        NOTHROW;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;

    return GetBestException(COR_E_OUTOFMEMORY, g_pOutOfMemoryExceptionClass);
}

OBJECTREF CLRException::GetBestBaseException()
{
    CONTRACTL
    {
        NOTHROW;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;

    return GetBestException(COR_E_EXCEPTION, g_pOutOfMemoryExceptionClass);
}

// thread abort is generally not supported in coreclr
// FX is not hardened to handle thread aborts and it may cause corruptions and deadlocks
// the only permitted scenario is when debugger needs to tear down a runaway expression evaluation.
// while unsafe, practically it is better than debuggee termination.
// For the same reasons, in a rare case if ThreadAbortException cannot be obtained,
// throwing OOM could be a good enough substitute that may still work.
OBJECTREF CLRException::GetBestThreadAbortException()
{
    CONTRACTL
    {
        NOTHROW;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;

    return GetBestException(COR_E_THREADABORTED, g_pThreadAbortExceptionClass);
}

// Works on non-CLRExceptions as well
// static function
OBJECTREF CLRException::GetThrowableFromException(Exception *pException)
{
    CONTRACTL
    {
        GC_TRIGGERS;
        NOTHROW;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;

    // Can't have a throwable without a Thread.
    Thread* pThread = GetThread();

    if (NULL == pException)
    {
        return pThread->LastThrownObject();
    }

    if (pException->IsType(CLRException::GetType()))
        return ((CLRException*)pException)->GetThrowable();

    if (pException->IsType(EEException::GetType()))
        return ((EEException*)pException)->GetThrowable();

    // Note: we are creating a throwable on the fly in this case - so
    // multiple calls will return different objects.  If we really need identity,
    // we could store a throwable handle at the catch site, or store it
    // on the thread object.

    if (pException->IsType(SEHException::GetType()))
    {
        SEHException *pSEHException = (SEHException*)pException;

        switch (pSEHException->m_exception.ExceptionCode)
        {
        case EXCEPTION_COMPLUS:
            // Note: even though the switch compared the exception code,
            // we have to call the official IsComPlusException() routine
            // for side-by-side correctness. If that check fails, treat
            // as an unrelated unmanaged exception.
            if (IsComPlusException(&(pSEHException->m_exception)))
            {
                return pThread->LastThrownObject();
            }
            else
            {
                break;
            }

        case STATUS_NO_MEMORY:
            return GetBestOutOfMemoryException();

        case STATUS_STACK_OVERFLOW:
            return GetPreallocatedStackOverflowException();
        }

        DWORD exceptionCode =
          MapWin32FaultToCOMPlusException(&pSEHException->m_exception);

        EEException e((RuntimeExceptionKind)exceptionCode);

        OBJECTREF throwable = e.GetThrowable();
        GCPROTECT_BEGIN (throwable);
        EX_TRY
        {
            SCAN_IGNORE_FAULT;
            if (throwable != NULL  && !CLRException::IsPreallocatedExceptionObject(throwable))
            {
                _ASSERTE(IsException(throwable->GetMethodTable()));

                // set the exception code
                ((EXCEPTIONREF)throwable)->SetXCode(pSEHException->m_exception.ExceptionCode);
            }
        }
        EX_CATCH
        {
        }
        EX_END_CATCH(SwallowAllExceptions)
        GCPROTECT_END ();

        return throwable;
    }
    else
    {
        // We can enter here for HRException, COMException, DelegatingException
        // just to name a few.
        OBJECTREF oRetVal = NULL;
        GCPROTECT_BEGIN(oRetVal);
        {
            EX_TRY
            {
                HRESULT hr = pException->GetHR();

                if (hr == E_OUTOFMEMORY || hr == HRESULT_FROM_WIN32(ERROR_NOT_ENOUGH_MEMORY))
                {
                    oRetVal = GetBestOutOfMemoryException();
                }
                else if (hr == COR_E_STACKOVERFLOW)
                {
                    oRetVal = GetPreallocatedStackOverflowException();
                }
                else
                {
                    SafeComHolder<IErrorInfo> pErrInfo(pException->GetErrorInfo());

                    if (pErrInfo != NULL)
                    {
                        GetExceptionForHR(hr, pErrInfo, &oRetVal);
                    }
                    else
                    {
                        SString message;
                        pException->GetMessage(message);

                        EEMessageException e(hr, IDS_EE_GENERIC, message);

                        oRetVal = e.CreateThrowable();
                    }
                }
            }
            EX_CATCH
            {
                // We have caught an exception trying to get a Throwable for the pException we
                //  were given.  It is tempting to want to get the Throwable for the new
                //  exception, but that is dangerous, due to infinitely cascading
                //  exceptions, leading to a stack overflow.

                // If we can see that the exception was OOM, return the preallocated OOM,
                //  if we can see that it is SO, return the preallocated SO,
                //  if we can see that it is some other managed exception, return that
                //  exception, otherwise return the preallocated System.Exception.
                Exception *pNewException = GET_EXCEPTION();

                if (pNewException->IsPreallocatedOOMException())
                {   // It definitely was an OOM
                    STRESS_LOG0(LF_EH, LL_INFO100, "CLRException::GetThrowableFromException: OOM creating throwable; getting pre-alloc'd OOM.\n");
                    if (oRetVal == NULL)
                        oRetVal = GetPreallocatedOutOfMemoryException();
                }
                else
                if (pNewException->IsType(CLRLastThrownObjectException::GetType()) &&
                    (pThread->LastThrownObject() != NULL))
                {
                    STRESS_LOG0(LF_EH, LL_INFO100, "CLRException::GetThrowableFromException: LTO Exception creating throwable; getting LastThrownObject.\n");
                    if (oRetVal == NULL)
                        oRetVal = pThread->LastThrownObject();
                }
                else
                {
                    // We *could* come here if one of the calls in the EX_TRY above throws an exception (e.g. MissingMethodException if we attempt
                    // to invoke CreateThrowable for a type that does not have a default constructor) that is neither preallocated OOM nor a
                    // CLRLastThrownObject type.
                    //
                    // Like the comment says above, we cannot afford to get the throwable lest we hit SO. In such a case, runtime is not in a bad shape
                    // but we dont know what to return as well. A reasonable answer is to return something less appropriate than ripping down process
                    // or returning an incorrect exception (e.g. OOM) that could break execution paths.
                    //
                    // Hence, we return preallocated System.Exception instance.
                    if (oRetVal == NULL)
                    {
                        oRetVal = GetBestBaseException();
                        STRESS_LOG0(LF_EH, LL_INFO100, "CLRException::GetThrowableFromException: Unknown Exception creating throwable; getting System.Exception.\n");
                    }
                }

            }
            EX_END_CATCH(SwallowAllExceptions)
        }
        GCPROTECT_END();

        return oRetVal;
    }
} // OBJECTREF CLRException::GetThrowableFromException()

OBJECTREF CLRException::GetThrowableFromExceptionRecord(EXCEPTION_RECORD *pExceptionRecord)
{
    CONTRACTL
    {
        GC_NOTRIGGER;
        NOTHROW;
        MODE_ANY;
    }
    CONTRACTL_END;

    if (IsComPlusException(pExceptionRecord))
    {
        return GetThread()->LastThrownObject();
    }

    return NULL;
}

void CLRException::HandlerState::CleanupTry()
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_MODE_ANY;

    if (m_pThread != NULL)
    {
        
        // If there is no frame to unwind, UnwindFrameChain call is just an expensive NOP
        // due to setting up and tear down of EH records. So we avoid it if we can.
        if (m_pThread->GetFrame() < m_pFrame)
            UnwindFrameChain(m_pThread, m_pFrame);

        if (m_fPreemptiveGCDisabled != m_pThread->PreemptiveGCDisabled())
        {
            if (m_fPreemptiveGCDisabled)
                m_pThread->DisablePreemptiveGC();
            else
                m_pThread->EnablePreemptiveGC();
        }
        
    }

    // Make sure to call the base class's CleanupTry so it can do whatever it wants to do.
    Exception::HandlerState::CleanupTry();
}

void CLRException::HandlerState::SetupCatch(INDEBUG_COMMA(__in_z const char * szFile) int lineNum)
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_MODE_ANY;
    STATIC_CONTRACT_CANNOT_TAKE_LOCK;

    Exception::HandlerState::SetupCatch(INDEBUG_COMMA(szFile) lineNum);

    Thread *pThread = NULL;
    DWORD exceptionCode = 0;

    if (g_fEEStarted)
    {
        pThread = GetThreadNULLOk();
        exceptionCode = GetCurrentExceptionCode();
    }

    if (!DidCatchCxx())
    {
        if (exceptionCode == STATUS_STACK_OVERFLOW)
        {
            // Handle SO exception
            //
            // We should ensure that a valid Thread object exists before trying to set SO as the LTO.
            if (pThread != NULL)
            {
                // We have a nasty issue with our EX_TRY/EX_CATCH.  If EX_CATCH catches SEH exception,
                // GET_THROWABLE uses CLRLastThrownObjectException instead, because we don't know
                // what exception to use.  But for SO, we can use preallocated SO exception.
                GCX_COOP();
                pThread->SetSOForLastThrownObject();
            }

            if (exceptionCode == STATUS_STACK_OVERFLOW)
            {
                // We have called HandleStackOverflow for soft SO through our vectored exception handler.
                EEPolicy::HandleStackOverflow();
            }
        }
    }

#ifdef FEATURE_EH_FUNCLETS
    if (!DidCatchCxx())
    {
        // this must be done after the second pass has run, it does not
        // reference anything on the stack, so it is safe to run in an
        // SEH __except clause as well as a C++ catch clause.
        ExceptionTracker::PopTrackers(this);
    }
#endif // FEATURE_EH_FUNCLETS
}

#ifdef LOGGING
void CLRException::HandlerState::SucceedCatch()
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_MODE_ANY;
    STATIC_CONTRACT_CANNOT_TAKE_LOCK;

    LOG((LF_EH, LL_INFO100, "EX_CATCH catch succeeded (CLRException::HandlerState)\n"));

    //
    // At this point, we don't believe we need to do any unwinding of the ExInfo chain after an EX_CATCH. The chain
    // is unwound by CPFH_UnwindFrames1() when it detects that the exception is being caught by an unmanaged
    // catcher. EX_CATCH looks just like an unmanaged catcher now, so the unwind is already done by the time we get
    // into the catch. That's different than before the big switch to the new exeption system, and it effects
    // rethrows. Fixing rethrows is a work item for a little later. For now, we're simplying removing the unwind
    // from here to avoid the extra unwind, which is harmless in many cases, but is very harmful when a managed
    // filter throws an exception.
    //
    //

    Exception::HandlerState::SucceedCatch();
}
#endif


// ---------------------------------------------------------------------------
// EEException methods
// ---------------------------------------------------------------------------

//------------------------------------------------------------------------
// Array that is used to retrieve the right exception for a given HRESULT.
//------------------------------------------------------------------------

struct ExceptionHRInfo
{
    int cHRs;
    const HRESULT *aHRs;
};

#define DEFINE_EXCEPTION(ns, reKind, bHRformessage, ...) static const HRESULT s_##reKind##HRs[] = { __VA_ARGS__ };
#include "rexcep.h"

static const
ExceptionHRInfo gExceptionHRInfos[] = {
#define DEFINE_EXCEPTION(ns, reKind, bHRformessage, ...) {sizeof(s_##reKind##HRs) / sizeof(HRESULT), s_##reKind##HRs},
#include "rexcep.h"
};


static const
bool gShouldDisplayHR[] =
{
#define DEFINE_EXCEPTION(ns, reKind, bHRformessage, ...) bHRformessage,
#include "rexcep.h"
};


/*static*/
HRESULT EEException::GetHRFromKind(RuntimeExceptionKind reKind)
{
    LIMITED_METHOD_CONTRACT;
    return gExceptionHRInfos[reKind].aHRs[0];
}

HRESULT EEException::GetHR()
{
    LIMITED_METHOD_CONTRACT;

    return EEException::GetHRFromKind(m_kind);
}

IErrorInfo *EEException::GetErrorInfo()
{
    LIMITED_METHOD_CONTRACT;

    return NULL;
}

BOOL EEException::GetThrowableMessage(SString &result)
{
    CONTRACTL
    {
        GC_TRIGGERS;
        THROWS;
        MODE_ANY;
    }
    CONTRACTL_END;

    // Return a meaningful HR message, if there is one.

    HRESULT hr = GetHR();

    // If the hr is more interesting than the kind, use that
    // for a message.

    if (hr != S_OK
        && hr != E_FAIL
        && (gShouldDisplayHR[m_kind]
            || gExceptionHRInfos[m_kind].aHRs[0] !=  hr))
    {
        // If it has only one HR, the original message should be good enough
        _ASSERTE(gExceptionHRInfos[m_kind].cHRs > 1 ||
                 gExceptionHRInfos[m_kind].aHRs[0] !=  hr);

        GenerateTopLevelHRExceptionMessage(hr, result);
        return TRUE;
    }

    // No interesting hr - just keep the class default message.

    return FALSE;
}

void EEException::GetMessage(SString &result)
{
    CONTRACTL
    {
        GC_TRIGGERS;
        THROWS;
        MODE_ANY;
    }
    CONTRACTL_END;

    // First look for a specialized message
    if (GetThrowableMessage(result))
        return;

    // Otherwise, report the class's generic message
    LPCUTF8 pszExceptionName = CoreLibBinder::GetExceptionName(m_kind);
    result.SetUTF8(pszExceptionName);
}

OBJECTREF EEException::CreateThrowable()
{
    CONTRACTL
    {
        GC_TRIGGERS;
        THROWS;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;

    _ASSERTE(g_pPreallocatedOutOfMemoryException != NULL);
    static int allocCount = 0;

    MethodTable *pMT = CoreLibBinder::GetException(m_kind);

    ThreadPreventAsyncHolder preventAsyncHolder(m_kind == kThreadAbortException);

    OBJECTREF throwable = AllocateObject(pMT);
    allocCount++;
    GCPROTECT_BEGIN(throwable);

    {
        ThreadPreventAsyncHolder preventAbort(m_kind == kThreadAbortException ||
                                              m_kind == kThreadInterruptedException);
        CallDefaultConstructor(throwable);
    }

    HRESULT hr = GetHR();
    ((EXCEPTIONREF)throwable)->SetHResult(hr);

    SString message;
    if (GetThrowableMessage(message))
    {
        // Set the message field. It is not safe doing this through the constructor
        // since the string constructor for some exceptions add a prefix to the message
        // which we don't want.
        //
        // We only want to replace whatever the default constructor put there, if we
        // have something meaningful to add.

        STRINGREF s = StringObject::NewString(message);
        ((EXCEPTIONREF)throwable)->SetMessage(s);
    }

    GCPROTECT_END();

    return throwable;
}

RuntimeExceptionKind EEException::GetKindFromHR(HRESULT hr)
{
    LIMITED_METHOD_CONTRACT;
    for (int i = 0; i < kLastException; i++)
    {
        for (int j = 0; j < gExceptionHRInfos[i].cHRs; j++)
        {
            if (gExceptionHRInfos[i].aHRs[j] == hr)
                return (RuntimeExceptionKind) i;
        }
    }

    return kCOMException;

} // RuntimeExceptionKind EEException::GetKindFromHR()

BOOL EEException::GetResourceMessage(UINT iResourceID, SString &result,
                                     const SString &arg1, const SString &arg2,
                                     const SString &arg3, const SString &arg4,
                                     const SString &arg5, const SString &arg6)
{
    CONTRACTL
    {
        GC_TRIGGERS;
        THROWS;
        MODE_ANY;
    }
    CONTRACTL_END;

    BOOL ok;

    StackSString temp;
    ok = temp.LoadResource(CCompRC::Error, iResourceID);

    if (ok)
        result.FormatMessage(FORMAT_MESSAGE_FROM_STRING,
         (LPCWSTR)temp, 0, 0, arg1, arg2, arg3, arg4, arg5, arg6);

    return ok;
}

// ---------------------------------------------------------------------------
// EEMessageException methods
// ---------------------------------------------------------------------------

HRESULT EEMessageException::GetHR()
{
    WRAPPER_NO_CONTRACT;

    return m_hr;
}

BOOL EEMessageException::GetThrowableMessage(SString &result)
{
    CONTRACTL
    {
        GC_TRIGGERS;
        THROWS;
        MODE_ANY;
    }
    CONTRACTL_END;

    if (m_resID != 0 && GetResourceMessage(m_resID, result))
        return TRUE;

    return EEException::GetThrowableMessage(result);
}

BOOL EEMessageException::GetResourceMessage(UINT iResourceID, SString &result)
{
    WRAPPER_NO_CONTRACT;

    return EEException::GetResourceMessage(
        iResourceID, result, m_arg1, m_arg2, m_arg3, m_arg4, m_arg5, m_arg6);
}

// ---------------------------------------------------------------------------
// EEResourceException methods
// ---------------------------------------------------------------------------

void EEResourceException::GetMessage(SString &result)
{
    WRAPPER_NO_CONTRACT;
    //
    // Return a simplified message,
    // since we don't want to call managed code here.
    //

    result.Printf("%s (message resource %s)",
                  CoreLibBinder::GetExceptionName(m_kind), m_resourceName.GetUnicode());
}

BOOL EEResourceException::GetThrowableMessage(SString &result)
{
    CONTRACTL
    {
        GC_TRIGGERS;
        THROWS;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;

    STRINGREF message = NULL;
    ResMgrGetString(m_resourceName, &message);

    if (message != NULL)
    {
        message->GetSString(result);
        return TRUE;
    }

    return EEException::GetThrowableMessage(result);
}

// ---------------------------------------------------------------------------
// EEFieldException is an EE exception subclass composed of a field
// ---------------------------------------------------------------------------


BOOL EEFieldException::GetThrowableMessage(SString &result)
{
    CONTRACTL
    {
        GC_TRIGGERS;
        THROWS;
        MODE_ANY;
    }
    CONTRACTL_END;

    if (m_messageID == 0)
    {
        LPUTF8 szFullName;
        LPCUTF8 szClassName, szMember;
        szMember = m_pFD->GetName();
        DefineFullyQualifiedNameForClass();
        szClassName = GetFullyQualifiedNameForClass(m_pFD->GetApproxEnclosingMethodTable());
        MAKE_FULLY_QUALIFIED_MEMBER_NAME(szFullName, NULL, szClassName, szMember, "");
        result.SetUTF8(szFullName);

        return TRUE;
    }
    else
    {
        _ASSERTE(m_pAccessingMD != NULL);

        const TypeString::FormatFlags formatFlags = static_cast<TypeString::FormatFlags>(
            TypeString::FormatNamespace |
            TypeString::FormatAngleBrackets |
            TypeString::FormatSignature);

        StackSString caller;
        TypeString::AppendMethod(caller,
                                 m_pAccessingMD,
                                 m_pAccessingMD->GetClassInstantiation(),
                                 formatFlags);

        StackSString field;
        TypeString::AppendField(field,
                                m_pFD,
                                m_pFD->GetApproxEnclosingMethodTable()->GetInstantiation(),
                                formatFlags);

        return GetResourceMessage(m_messageID, result, caller, field, m_additionalContext);
    }
}

// ---------------------------------------------------------------------------
// EEMethodException is an EE exception subclass composed of a field
// ---------------------------------------------------------------------------

BOOL EEMethodException::GetThrowableMessage(SString &result)
{
    CONTRACTL
    {
        GC_TRIGGERS;
        THROWS;
        MODE_ANY;
    }
    CONTRACTL_END;

    if (m_messageID == 0)
    {
        LPUTF8 szFullName;
        LPCUTF8 szClassName, szMember;
        szMember = m_pMD->GetName();
        DefineFullyQualifiedNameForClass();
        szClassName = GetFullyQualifiedNameForClass(m_pMD->GetMethodTable());
        //@todo GENERICS: exact instantiations?
        MetaSig tmp(m_pMD);
        SigFormat sigFormatter(tmp, szMember);
        const char * sigStr = sigFormatter.GetCStringParmsOnly();
        MAKE_FULLY_QUALIFIED_MEMBER_NAME(szFullName, NULL, szClassName, szMember, sigStr);
        result.SetUTF8(szFullName);

        return TRUE;
    }
    else
    {
        _ASSERTE(m_pAccessingMD != NULL);

        const TypeString::FormatFlags formatFlags = static_cast<TypeString::FormatFlags>(
            TypeString::FormatNamespace |
            TypeString::FormatAngleBrackets |
            TypeString::FormatSignature);

        StackSString caller;
        TypeString::AppendMethod(caller,
                                 m_pAccessingMD,
                                 m_pAccessingMD->GetClassInstantiation(),
                                 formatFlags);

        StackSString callee;
        TypeString::AppendMethod(callee,
                                 m_pMD,
                                 m_pMD->GetClassInstantiation(),
                                 formatFlags);

        return GetResourceMessage(m_messageID, result, caller, callee, m_additionalContext);
    }
}

BOOL EETypeAccessException::GetThrowableMessage(SString &result)
{
    CONTRACTL
    {
        GC_TRIGGERS;
        THROWS;
        MODE_ANY;
    }
    CONTRACTL_END;

    const TypeString::FormatFlags formatFlags = static_cast<TypeString::FormatFlags>(
            TypeString::FormatNamespace |
            TypeString::FormatAngleBrackets |
            TypeString::FormatSignature);
    StackSString type;
    TypeString::AppendType(type, TypeHandle(m_pMT), formatFlags);

    if (m_messageID == 0)
    {
        result.Set(type);
        return TRUE;
    }
    else
    {
        _ASSERTE(m_pAccessingMD != NULL);

        StackSString caller;
        TypeString::AppendMethod(caller,
                                 m_pAccessingMD,
                                 m_pAccessingMD->GetClassInstantiation(),
                                 formatFlags);

        return GetResourceMessage(m_messageID, result, caller, type, m_additionalContext);
    }
}

// ---------------------------------------------------------------------------
// EEArgumentException is an EE exception subclass representing a bad argument
// ---------------------------------------------------------------------------

typedef struct {
    OBJECTREF pThrowable;
    STRINGREF s1;
    OBJECTREF pTmpThrowable;
} ProtectArgsStruct;

OBJECTREF EEArgumentException::CreateThrowable()
{

    CONTRACTL
    {
        GC_TRIGGERS;
        THROWS;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;

    _ASSERTE(GetThreadNULLOk() != NULL);

    ProtectArgsStruct prot;
    memset(&prot, 0, sizeof(ProtectArgsStruct));
    ResMgrGetString(m_resourceName, &prot.s1);
    GCPROTECT_BEGIN(prot);

    MethodTable *pMT = CoreLibBinder::GetException(m_kind);
    prot.pThrowable = AllocateObject(pMT);

    MethodDesc* pMD = MemberLoader::FindMethod(prot.pThrowable->GetMethodTable(),
                            COR_CTOR_METHOD_NAME, &gsig_IM_Str_Str_RetVoid);

    if (!pMD)
    {
        MAKE_WIDEPTR_FROMUTF8(wzMethodName, COR_CTOR_METHOD_NAME);
        COMPlusThrowNonLocalized(kMissingMethodException, wzMethodName);
    }

    MethodDescCallSite exceptionCtor(pMD);

    STRINGREF argName = StringObject::NewString(m_argumentName);

    // Note that ArgumentException takes arguments to its constructor in a different order,
    // for usability reasons.  However it is inconsistent with our other exceptions.
    if (m_kind == kArgumentException)
    {
        ARG_SLOT args1[] = {
            ObjToArgSlot(prot.pThrowable),
            ObjToArgSlot(prot.s1),
            ObjToArgSlot(argName),
        };
        exceptionCtor.Call(args1);
    }
    else
    {
        ARG_SLOT args1[] = {
            ObjToArgSlot(prot.pThrowable),
            ObjToArgSlot(argName),
            ObjToArgSlot(prot.s1),
        };
        exceptionCtor.Call(args1);
    }

    GCPROTECT_END(); //Prot

    return prot.pThrowable;
}


// ---------------------------------------------------------------------------
// EETypeLoadException is an EE exception subclass representing a type loading
// error
// ---------------------------------------------------------------------------

EETypeLoadException::EETypeLoadException(LPCUTF8 pszNameSpace, LPCUTF8 pTypeName,
                    LPCWSTR pAssemblyName, LPCUTF8 pMessageArg, UINT resIDWhy)
  : EEException(kTypeLoadException),
    m_pAssemblyName(pAssemblyName),
    m_pMessageArg(SString::Utf8, pMessageArg),
    m_resIDWhy(resIDWhy)
{
    CONTRACTL
    {
        GC_NOTRIGGER;
        THROWS;
        MODE_ANY;
    }
    CONTRACTL_END;

    if(pszNameSpace)
    {
        SString sNameSpace(SString::Utf8, pszNameSpace);
        SString sTypeName(SString::Utf8, pTypeName);
        m_fullName.MakeFullNamespacePath(sNameSpace, sTypeName);
    }
    else if (pTypeName)
        m_fullName.SetUTF8(pTypeName);
    else {
        WCHAR wszTemplate[30];
        if (FAILED(UtilLoadStringRC(IDS_EE_NAME_UNKNOWN,
                                    wszTemplate,
                                    sizeof(wszTemplate)/sizeof(wszTemplate[0]),
                                    FALSE)))
            wszTemplate[0] = W('\0');
        MAKE_UTF8PTR_FROMWIDE(name, wszTemplate);
        m_fullName.SetUTF8(name);
    }
}

EETypeLoadException::EETypeLoadException(LPCWSTR pFullName,
                                         LPCWSTR pAssemblyName,
                                         LPCUTF8 pMessageArg,
                                         UINT resIDWhy)
  : EEException(kTypeLoadException),
    m_pAssemblyName(pAssemblyName),
    m_pMessageArg(SString::Utf8, pMessageArg),
    m_resIDWhy(resIDWhy)
{
    CONTRACTL
    {
        GC_NOTRIGGER;
        THROWS;
        MODE_ANY;
    }
    CONTRACTL_END;

    MAKE_UTF8PTR_FROMWIDE(name, pFullName);
    m_fullName.SetUTF8(name);
}

void EETypeLoadException::GetMessage(SString &result)
{
    WRAPPER_NO_CONTRACT;
    GetResourceMessage(IDS_CLASSLOAD_GENERAL, result,
                       m_fullName, m_pAssemblyName, m_pMessageArg);
}

OBJECTREF EETypeLoadException::CreateThrowable()
{

    CONTRACTL
    {
        GC_TRIGGERS;
        THROWS;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;

    MethodTable *pMT = CoreLibBinder::GetException(kTypeLoadException);

    struct _gc {
        OBJECTREF pNewException;
        STRINGREF pNewAssemblyString;
        STRINGREF pNewClassString;
        STRINGREF pNewMessageArgString;
    } gc;
    ZeroMemory(&gc, sizeof(gc));
    GCPROTECT_BEGIN(gc);

    gc.pNewClassString = StringObject::NewString(m_fullName);

    if (!m_pMessageArg.IsEmpty())
        gc.pNewMessageArgString = StringObject::NewString(m_pMessageArg);

    if (!m_pAssemblyName.IsEmpty())
        gc.pNewAssemblyString = StringObject::NewString(m_pAssemblyName);

    gc.pNewException = AllocateObject(pMT);

    MethodDesc* pMD = MemberLoader::FindMethod(gc.pNewException->GetMethodTable(),
                            COR_CTOR_METHOD_NAME, &gsig_IM_Str_Str_Str_Int_RetVoid);

    if (!pMD)
    {
        MAKE_WIDEPTR_FROMUTF8(wzMethodName, COR_CTOR_METHOD_NAME);
        COMPlusThrowNonLocalized(kMissingMethodException, wzMethodName);
    }

    MethodDescCallSite exceptionCtor(pMD);

    ARG_SLOT args[] = {
        ObjToArgSlot(gc.pNewException),
        ObjToArgSlot(gc.pNewClassString),
        ObjToArgSlot(gc.pNewAssemblyString),
        ObjToArgSlot(gc.pNewMessageArgString),
        (ARG_SLOT)m_resIDWhy,
    };

    exceptionCtor.Call(args);

    GCPROTECT_END();

    return gc.pNewException;
}

// ---------------------------------------------------------------------------
// EEFileLoadException is an EE exception subclass representing a file loading
// error
// ---------------------------------------------------------------------------
EEFileLoadException::EEFileLoadException(const SString &name, HRESULT hr, Exception *pInnerException/* = NULL*/)
  : EEException(GetFileLoadKind(hr)),
    m_name(name),
    m_hr(hr)
{
    CONTRACTL
    {
        GC_NOTRIGGER;
        THROWS;
        MODE_ANY;
    }
    CONTRACTL_END;

    // We don't want to wrap IsTransient() exceptions. The caller should really have checked this
    // before invoking the ctor.
    _ASSERTE(pInnerException == NULL || !(pInnerException->IsTransient()));
    m_innerException = pInnerException ? pInnerException->DomainBoundClone() : NULL;

    if (m_name.IsEmpty())
    {
        WCHAR wszTemplate[30];
        if (FAILED(UtilLoadStringRC(IDS_EE_NAME_UNKNOWN,
                                    wszTemplate,
                                    sizeof(wszTemplate)/sizeof(wszTemplate[0]),
                                    FALSE)))
        {
            wszTemplate[0] = W('\0');
        }

        m_name.Set(wszTemplate);
    }
}


EEFileLoadException::~EEFileLoadException()
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;

}



void EEFileLoadException::SetFileName(const SString &fileName, BOOL removePath)
{
    CONTRACTL
    {
        GC_NOTRIGGER;
        THROWS;
        MODE_ANY;
    }
    CONTRACTL_END;

    //<TODO>@TODO: security: It would be nice for debugging purposes if the
    // user could have the full path, if the user has the right permission.</TODO>
    if (removePath)
    {
        SString::CIterator i = fileName.End();

        if (fileName.FindBack(i, W('\\')))
            i++;

        if (fileName.FindBack(i, W('/')))
            i++;

        m_name.Set(fileName, i, fileName.End());
    }
    else
        m_name.Set(fileName);
}

void EEFileLoadException::GetMessage(SString &result)
{
    WRAPPER_NO_CONTRACT;

    SString sHR;
    GetHRMsg(m_hr, sHR);
    GetResourceMessage(GetResourceIDForFileLoadExceptionHR(m_hr), result, m_name, sHR);
}

void EEFileLoadException::GetName(SString &result)
{
    WRAPPER_NO_CONTRACT;

    result.Set(m_name);
}

/* static */
RuntimeExceptionKind EEFileLoadException::GetFileLoadKind(HRESULT hr)
{
    CONTRACTL
    {
        GC_NOTRIGGER;
        NOTHROW;
        MODE_ANY;
    }
    CONTRACTL_END;

    if (Assembly::FileNotFound(hr))
        return kFileNotFoundException;
    else
    {
        // Make sure this matches the list in rexcep.h
        if ((hr == COR_E_BADIMAGEFORMAT) ||
            (hr == CLDB_E_FILE_OLDVER)   ||
            (hr == CLDB_E_INDEX_NOTFOUND)   ||
            (hr == CLDB_E_FILE_CORRUPT)   ||
            (hr == COR_E_NEWER_RUNTIME)   ||
            (hr == COR_E_ASSEMBLYEXPECTED)   ||
            (hr == HRESULT_FROM_WIN32(ERROR_BAD_EXE_FORMAT)) ||
            (hr == HRESULT_FROM_WIN32(ERROR_EXE_MARKED_INVALID)) ||
            (hr == CORSEC_E_INVALID_IMAGE_FORMAT) ||
            (hr == HRESULT_FROM_WIN32(ERROR_NOACCESS)) ||
            (hr == HRESULT_FROM_WIN32(ERROR_INVALID_ORDINAL))   ||
            (hr == HRESULT_FROM_WIN32(ERROR_INVALID_DLL)) ||
            (hr == HRESULT_FROM_WIN32(ERROR_FILE_CORRUPT)) ||
            (hr == (HRESULT) IDS_CLASSLOAD_32BITCLRLOADING64BITASSEMBLY) ||
            (hr == COR_E_LOADING_REFERENCE_ASSEMBLY) ||
            (hr == META_E_BAD_SIGNATURE))
            return kBadImageFormatException;
        else
        {
            if ((hr == E_OUTOFMEMORY) || (hr == NTE_NO_MEMORY))
                return kOutOfMemoryException;
            else
                return kFileLoadException;
        }
    }
}

OBJECTREF EEFileLoadException::CreateThrowable()
{

    CONTRACTL
    {
        GC_TRIGGERS;
        THROWS;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;

    struct _gc {
        OBJECTREF pNewException;
        STRINGREF pNewFileString;
    } gc;
    ZeroMemory(&gc, sizeof(gc));
    GCPROTECT_BEGIN(gc);

    gc.pNewFileString = StringObject::NewString(m_name);
    gc.pNewException = AllocateObject(CoreLibBinder::GetException(m_kind));

    MethodDesc* pMD = MemberLoader::FindMethod(gc.pNewException->GetMethodTable(),
                            COR_CTOR_METHOD_NAME, &gsig_IM_Str_Int_RetVoid);

    if (!pMD)
    {
        MAKE_WIDEPTR_FROMUTF8(wzMethodName, COR_CTOR_METHOD_NAME);
        COMPlusThrowNonLocalized(kMissingMethodException, wzMethodName);
    }

    MethodDescCallSite  exceptionCtor(pMD);

    ARG_SLOT args[] = {
        ObjToArgSlot(gc.pNewException),
        ObjToArgSlot(gc.pNewFileString),
        (ARG_SLOT) m_hr
    };

    exceptionCtor.Call(args);

    GCPROTECT_END();

    return gc.pNewException;
}


/* static */
BOOL EEFileLoadException::CheckType(Exception* ex)
{
    LIMITED_METHOD_CONTRACT;

    // used as typeof(EEFileLoadException)
    RuntimeExceptionKind kind = kException;
    if (ex->IsType(EEException::GetType()))
        kind=((EEException*)ex)->m_kind;


    switch(kind)
    {
        case kFileLoadException:
        case kFileNotFoundException:
        case kBadImageFormatException:
            return TRUE;
        default:
            return FALSE;
    }
};


// <TODO>@todo: ideally we would use inner exceptions with these routines</TODO>

/* static */

/* static */
void DECLSPEC_NORETURN EEFileLoadException::Throw(AssemblySpec  *pSpec, HRESULT hr, Exception *pInnerException/* = NULL*/)
{
    CONTRACTL
    {
        GC_TRIGGERS;
        THROWS;
        MODE_ANY;
    }
    CONTRACTL_END;

    if (hr == COR_E_THREADABORTED)
        COMPlusThrow(kThreadAbortException);
    if (hr == E_OUTOFMEMORY)
        COMPlusThrowOM();

    StackSString name;
    pSpec->GetFileOrDisplayName(0, name);
    EX_THROW_WITH_INNER(EEFileLoadException, (name, hr), pInnerException);
}

/* static */
void DECLSPEC_NORETURN EEFileLoadException::Throw(PEFile *pFile, HRESULT hr, Exception *pInnerException /* = NULL*/)
{
    CONTRACTL
    {
        GC_TRIGGERS;
        THROWS;
        MODE_ANY;
    }
    CONTRACTL_END;

    if (hr == COR_E_THREADABORTED)
        COMPlusThrow(kThreadAbortException);
    if (hr == E_OUTOFMEMORY)
        COMPlusThrowOM();

    StackSString name;

    if (pFile->IsAssembly())
        ((PEAssembly*)pFile)->GetDisplayName(name);
    else
        name = StackSString(SString::Utf8, pFile->GetSimpleName());
    EX_THROW_WITH_INNER(EEFileLoadException, (name, hr), pInnerException);

}

/* static */
void DECLSPEC_NORETURN EEFileLoadException::Throw(LPCWSTR path, HRESULT hr, Exception *pInnerException/* = NULL*/)
{
    CONTRACTL
    {
        GC_TRIGGERS;
        THROWS;
        MODE_ANY;
    }
    CONTRACTL_END;

    if (hr == COR_E_THREADABORTED)
        COMPlusThrow(kThreadAbortException);
    if (hr == E_OUTOFMEMORY)
        COMPlusThrowOM();

    EX_THROW_WITH_INNER(EEFileLoadException, (StackSString(path), hr), pInnerException);
}

/* static */
/* static */
void DECLSPEC_NORETURN EEFileLoadException::Throw(PEAssembly *parent,
                                                  const void *memory, COUNT_T size, HRESULT hr, Exception *pInnerException/* = NULL*/)
{
    CONTRACTL
    {
        GC_TRIGGERS;
        THROWS;
        MODE_ANY;
    }
    CONTRACTL_END;

    if (hr == COR_E_THREADABORTED)
        COMPlusThrow(kThreadAbortException);
    if (hr == E_OUTOFMEMORY)
        COMPlusThrowOM();

    StackSString name;
    name.Printf("%d bytes loaded from ", size);

    StackSString parentName;
    parent->GetDisplayName(parentName);

    name.Append(parentName);
    EX_THROW_WITH_INNER(EEFileLoadException, (name, hr), pInnerException);
}

// ---------------------------------------------------------------------------
// EEComException methods
// ---------------------------------------------------------------------------

static HRESULT Undefer(EXCEPINFO *pExcepInfo)
{
    CONTRACTL
    {
        GC_NOTRIGGER;
        NOTHROW;
        MODE_ANY;
    }
    CONTRACTL_END;

    if (pExcepInfo->pfnDeferredFillIn)
    {
        EXCEPINFO FilledInExcepInfo;

        HRESULT hr = pExcepInfo->pfnDeferredFillIn(&FilledInExcepInfo);
        if (SUCCEEDED(hr))
        {
            // Free the strings in the original EXCEPINFO.
            if (pExcepInfo->bstrDescription)
            {
                SysFreeString(pExcepInfo->bstrDescription);
                pExcepInfo->bstrDescription = NULL;
            }
            if (pExcepInfo->bstrSource)
            {
                SysFreeString(pExcepInfo->bstrSource);
                pExcepInfo->bstrSource = NULL;
            }
            if (pExcepInfo->bstrHelpFile)
            {
                SysFreeString(pExcepInfo->bstrHelpFile);
                pExcepInfo->bstrHelpFile = NULL;
            }

            // Fill in the new data
            *pExcepInfo = FilledInExcepInfo;
        }
    }

    if (pExcepInfo->scode != 0)
        return pExcepInfo->scode;
    else
        return (HRESULT)pExcepInfo->wCode;
}

EECOMException::EECOMException(EXCEPINFO *pExcepInfo)
  : EEException(GetKindFromHR(Undefer(pExcepInfo)))
{
    WRAPPER_NO_CONTRACT;

    if (pExcepInfo->scode != 0)
        m_ED.hr = pExcepInfo->scode;
    else
        m_ED.hr = (HRESULT)pExcepInfo->wCode;

    m_ED.bstrDescription = pExcepInfo->bstrDescription;
    m_ED.bstrSource = pExcepInfo->bstrSource;
    m_ED.bstrHelpFile = pExcepInfo->bstrHelpFile;
    m_ED.dwHelpContext = pExcepInfo->dwHelpContext;
    m_ED.guid = GUID_NULL;

    // Zero the EXCEPINFO.
    memset(pExcepInfo, NULL, sizeof(EXCEPINFO));
}

EECOMException::EECOMException(ExceptionData *pData)
  : EEException(GetKindFromHR(pData->hr))
{
    LIMITED_METHOD_CONTRACT;

    m_ED = *pData;

    // Zero the data.
    ZeroMemory(pData, sizeof(ExceptionData));
}

EECOMException::EECOMException(
    HRESULT hr,
    IErrorInfo *pErrInfo
    COMMA_INDEBUG(BOOL bCheckInProcCCWTearOff))
  : EEException(GetKindFromHR(hr))
{
    WRAPPER_NO_CONTRACT;

#ifdef FEATURE_COMINTEROP
    // Must use another path for managed IErrorInfos...
    //  note that this doesn't cover out-of-proc managed IErrorInfos.
    _ASSERTE(!bCheckInProcCCWTearOff || !IsInProcCCWTearOff(pErrInfo));
#endif  // FEATURE_COMINTEROP

    m_ED.hr = hr;
    m_ED.bstrDescription = NULL;
    m_ED.bstrSource = NULL;
    m_ED.bstrHelpFile = NULL;
    m_ED.dwHelpContext = NULL;
    m_ED.guid = GUID_NULL;

    FillExceptionData(&m_ED, pErrInfo);
}

BOOL EECOMException::GetThrowableMessage(SString &result)
{
     CONTRACTL
    {
        GC_TRIGGERS;
        THROWS;
        MODE_ANY;
    }
    CONTRACTL_END;

    if (m_ED.bstrDescription != NULL)
    {
        result.Set(m_ED.bstrDescription, SysStringLen(m_ED.bstrDescription));
    }
    else
    {
        GenerateTopLevelHRExceptionMessage(GetHR(), result);
    }

    return TRUE;
}

EECOMException::~EECOMException()
{
    WRAPPER_NO_CONTRACT;

    FreeExceptionData(&m_ED);
}

HRESULT EECOMException::GetHR()
{
    LIMITED_METHOD_CONTRACT;

    return m_ED.hr;
}

OBJECTREF EECOMException::CreateThrowable()
{
    CONTRACTL
    {
        GC_TRIGGERS;
        THROWS;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;

    OBJECTREF throwable = NULL;
    GCPROTECT_BEGIN(throwable);

    // Note that this will pick up the message from GetThrowableMessage
    throwable = EEException::CreateThrowable();

    // Set the _helpURL field in the exception.
    if (m_ED.bstrHelpFile)
    {
        // Create the help link from the help file and the help context.
        STRINGREF helpStr = NULL;
        if (m_ED.dwHelpContext != 0)
        {
            // We have a non 0 help context so use it to form the help link.
            SString strMessage;
            strMessage.Printf(W("%s#%d"), m_ED.bstrHelpFile, m_ED.dwHelpContext);
            helpStr = StringObject::NewString(strMessage);
        }
        else
        {
            // The help context is 0 so we simply use the help file to from the help link.
            helpStr = StringObject::NewString(m_ED.bstrHelpFile, SysStringLen(m_ED.bstrHelpFile));
        }

        ((EXCEPTIONREF)throwable)->SetHelpURL(helpStr);
    }

    // Set the Source field in the exception.
    STRINGREF sourceStr = NULL;
    if (m_ED.bstrSource)
    {
        sourceStr = StringObject::NewString(m_ED.bstrSource, SysStringLen(m_ED.bstrSource));
    }
    else
    {
        // for now set a null source
        sourceStr = StringObject::GetEmptyString();
    }
    ((EXCEPTIONREF)throwable)->SetSource(sourceStr);

    GCPROTECT_END();


    return throwable;
}

// ---------------------------------------------------------------------------
// ObjrefException methods
// ---------------------------------------------------------------------------

ObjrefException::ObjrefException()
{
    LIMITED_METHOD_CONTRACT;
}

ObjrefException::ObjrefException(OBJECTREF throwable)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    SetThrowableHandle(GetAppDomain()->CreateHandle(throwable));
}

// --------------------------------------------------------------------------------------------------------------------------------------
// ObjrefException and CLRLastThrownObjectException are never set as inner exception for an internal CLR exception.
// As a result, if we invoke DomainBoundClone against an exception, it will never reach these implementations.
// If someone does set them as inner, it will trigger contract violation - which is valid and should be fixed by whoever
// set them as inner since Exception::DomainBoundClone is implemented in utilcode that has to work outside the context of CLR and thus,
// should never trigger GC. This is also why GC_TRIGGERS is not supported in utilcode (refer to its definition in contracts.h).
// --------------------------------------------------------------------------------------------------------------------------------------
Exception *ObjrefException::DomainBoundCloneHelper()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;
    GCX_COOP();
    return new ObjrefException(GetThrowable());
}

// ---------------------------------------------------------------------------
// CLRLastThrownException methods
// ---------------------------------------------------------------------------

CLRLastThrownObjectException::CLRLastThrownObjectException()
{
    LIMITED_METHOD_CONTRACT;
}

Exception *CLRLastThrownObjectException::CloneHelper()
 {
    WRAPPER_NO_CONTRACT;
    GCX_COOP();
    return new ObjrefException(GetThrowable());
}

// ---------------------------------------------------------------------------
// See ObjrefException::DomainBoundCloneHelper comments.
// ---------------------------------------------------------------------------
Exception *CLRLastThrownObjectException::DomainBoundCloneHelper()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;
    GCX_COOP();
    return new ObjrefException(GetThrowable());
}

OBJECTREF CLRLastThrownObjectException::CreateThrowable()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;

    DEBUG_STMT(Validate());

    return GetThread()->LastThrownObject();
} // OBJECTREF CLRLastThrownObjectException::CreateThrowable()

#if defined(_DEBUG)
CLRLastThrownObjectException* CLRLastThrownObjectException::Validate()
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        DEBUG_ONLY;
    }
    CONTRACTL_END;

    // Have to be in coop for GCPROTECT_BEGIN.
    GCX_COOP();

    OBJECTREF throwable = NULL;

    GCPROTECT_BEGIN(throwable);

    Thread * pThread = GetThread();
    throwable = pThread->LastThrownObject();

    DWORD dwCurrentExceptionCode = GetCurrentExceptionCode();

    if (dwCurrentExceptionCode == BOOTUP_EXCEPTION_COMPLUS)
    {
        // BOOTUP_EXCEPTION_COMPLUS can be thrown when a thread setup is failed due to reasons like
        // runtime is being shutdown or managed code is no longer allowed to be executed.
        //
        // If this exception is caught in EX_CATCH, there may not be any LTO setup since:
        //
        // 1) It is setup against the thread that may not exist (due to thread setup failure)
        // 2) This exception is raised using RaiseException (and not the managed raise implementation in RaiseTheExceptionInternalOnly)
        //    since managed code may not be allowed to be executed.
        //
        // However, code inside EX_CATCH is abstracted of this specificity of EH and thus, will attempt to fetch the throwble
        // using GET_THROWABLE that will, in turn, use the GET_EXCEPTION macro to fetch the C++ exception type corresponding to the caught exception.
        // Since BOOTUP_EXCEPTION_COMPLUS is a SEH exception, this (C++ exception) type will be CLRLastThrownObjectException.
        //
        // GET_EXCEPTION will call this method to validate the presence of LTO for a SEH exception caught by EX_CATCH. This is based upon the assumption
        // that by the time a SEH exception is caught in EX_CATCH, the LTO is setup:
        //
        // A) For a managed exception thrown, this is done by RaiseTheExceptionInternalOnly.
        // B) For a SEH exception that enters managed code from a PInvoke call, this is done by calling SafeSetThrowables after the corresponding throwable is created
        //    using CreateCOMPlusExceptionObject.

        // Clearly, BOOTUP_EXCEPTION_COMPLUS can also be caught in EX_CATCH. However:
        //
        // (A) above is not applicable since the exception is raised using RaiseException.
        //
        // (B) scenario is interesting. On x86, CPFH_FirstPassHandler also invokes CLRVectoredExceptionHandler (for legacy purposes) that, in Phase3, will return EXCEPTION_CONTINUE_SEARCH for
        //     BOOTUP_EXCEPTION_COMPLUS. This will result in CPFH_FirstPassHandler to simply return from the x86 personality routine without invoking CreateCOMPlusExceptionObject even if managed
        //     frames were present on the stack (as happens in PInvoke). Thus, there is no LTO setup for X86.
        //
        //     On X64, the personality routine does not invoke VEH but simply creates the exception tracker and will also create throwable and setup LTO if managed frames are present on the stack.
        //     But if there are no managed frames on the stack, then the managed personality routine may or may not get invoked (depending upon if any VM native function is present on the stack whose
        //     personality routine is the managed personality routine). Thus, we may have a case of LTO not being present on X64 as well, for this exception.
        //
        // Thus, when we see BOOTUP_EXCEPTION_COMPLUS, we will return back successfully (without doing anything) to imply a successful LTO validation. Eventually, a valid
        // throwable will be returned to the user of GET_THROWABLE (for details, trace the call to CLRException::GetThrowableFromException for CLRLastThrownObjectException type).
        //
        // This also ensures that the handling of BOOTUP_EXCEPTION_COMPLUS is now insync between the chk and fre builds in terms of the throwable returned.
    }
    else if (throwable == NULL)
    {   // If there isn't a LastThrownObject at all, that's a problem for GetLastThrownObject
        // We've lost track of the exception's type.  Raise an assert.  (This is configurable to allow
        //  stress labs to turn off the assert.)

        static int iSuppress = -1;
        if (iSuppress == -1)
            iSuppress = CLRConfig::GetConfigValue(CLRConfig::INTERNAL_SuppressLostExceptionTypeAssert);
        if (!iSuppress)
        {
            // Raising an assert message can  cause a mode violation.
            CONTRACT_VIOLATION(ModeViolation);

            // Use DbgAssertDialog to get the formatting right.
            DbgAssertDialog(__FILE__, __LINE__,
                "The 'LastThrownObject' should not be, but is, NULL.\n"
                "The runtime may have lost track of the type of an exception in flight.\n"
                "Please get a good stack trace, find the caller of Validate, and file a bug against the owner.\n\n"
                "To suppress this assert 'set COMPlus_SuppressLostExceptionTypeAssert=1'");
        }
    }

    GCPROTECT_END();

    return this;
} // CLRLastThrownObjectException* CLRLastThrownObjectException::Validate()
#endif // _DEBUG

// ---------------------------------------------------------------------------
// Helper function to get an exception from outside the exception.
//  Create and return a LastThrownObjectException.  Its virtual destructor
//  will clean up properly.
void GetLastThrownObjectExceptionFromThread(Exception **ppException)
{
    CONTRACTL
    {
        GC_TRIGGERS;
        THROWS;
        MODE_ANY;
    }
    CONTRACTL_END;

    // If the Thread has been set up, then the LastThrownObject may make sense...
    if (GetThreadNULLOk())
    {
        // give back an object that knows about Threads and their exceptions.
        *ppException = new CLRLastThrownObjectException();
    }
    else
    {
        // but if no Thread, don't pretend to know about LastThrownObject.
        *ppException = NULL;
    }

} // void GetLastThrownObjectExceptionFromThread()


//@TODO: Make available generally?
// Wrapper class to encapsulate both array pointer and element count.
template <typename T>
class ArrayReference
{
public:
    typedef T value_type;
    typedef const typename std::remove_const<T>::type const_value_type;

    typedef ArrayDPTR(value_type) array_type;
    typedef ArrayDPTR(const_value_type) const_array_type;

    // Constructor taking array pointer and size.
    ArrayReference(array_type array, size_t size)
        : _array(dac_cast<array_type>(array))
        , _size(size)
    { LIMITED_METHOD_CONTRACT; }

    // Constructor taking a statically sized array by reference.
    template <size_t N>
    ArrayReference(T (&array)[N])
        : _array(dac_cast<array_type>(&array[0]))
        , _size(N)
    { LIMITED_METHOD_CONTRACT; }

    // Copy constructor.
    ArrayReference(ArrayReference const & other)
        : _array(other._array)
        , _size(other._size)
    { LIMITED_METHOD_CONTRACT; }

    // Indexer
    template <typename IdxT>
    T & operator[](IdxT idx)
    { LIMITED_METHOD_CONTRACT; _ASSERTE(idx < _size); return _array[idx]; }

    // Implicit conversion operators.
    operator array_type()
    { LIMITED_METHOD_CONTRACT; return _array; }

    operator const_array_type() const
    { LIMITED_METHOD_CONTRACT; return dac_cast<const_array_type>(_array); }

    // Returns the array element count.
    size_t size() const
    { LIMITED_METHOD_CONTRACT; return _size; }

    // Iteration methods and types.
    typedef array_type iterator;

    iterator begin()
    { LIMITED_METHOD_CONTRACT; return _array; }

    iterator end()
    { LIMITED_METHOD_CONTRACT; return _array + _size; }

    typedef const_array_type const_iterator;

    const_iterator begin() const
    { LIMITED_METHOD_CONTRACT; return dac_cast<const_array_type>(_array); }

    const_iterator end() const
    { LIMITED_METHOD_CONTRACT; return dac_cast<const_array_type>(_array) + _size; }

private:
    array_type   _array;
    size_t       _size;
};

ArrayReference<const HRESULT> GetHRESULTsForExceptionKind(RuntimeExceptionKind kind)
{
    LIMITED_METHOD_CONTRACT;

    switch (kind)
    {
        #define DEFINE_EXCEPTION(ns, reKind, bHRformessage, ...)    \
            case k##reKind:                                         \
                return ArrayReference<const HRESULT>(s_##reKind##HRs);    \
                break;
        #include "rexcep.h"

        default:
            _ASSERTE(!"Unknown exception kind!");
            break;

    }

    return ArrayReference<const HRESULT>(nullptr, 0);
}

bool IsHRESULTForExceptionKind(HRESULT hr, RuntimeExceptionKind kind)
{
    LIMITED_METHOD_CONTRACT;

    ArrayReference<const HRESULT> rgHR = GetHRESULTsForExceptionKind(kind);
    for (ArrayReference<const HRESULT>::iterator i = rgHR.begin(); i != rgHR.end(); ++i)
    {
        if (*i == hr)
        {
            return true;
        }
    }

    return false;
}


