//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//


#if !defined(_EX_H_)
#define _EX_H_

void RetailAssertIfExpectedClean();             // Defined in src/utilcode/debug.cpp

#ifdef CLR_STANDALONE_BINDER

#define INCONTRACT(x)

#define EX_THROW(type, value)   throw type(value)

void DECLSPEC_NORETURN ThrowLastError();

#define EX_TRY  try
#define EX_CATCH_HRESULT(_hr)    catch (HRESULT hr) { _hr = hr; }
#define EX_CATCH    catch(...)
#define EX_END_CATCH(a)
#define EX_RETHROW  throw
#define EX_SWALLOW_NONTERMINAL catch(...) {}
#define EX_END_CATCH_UNREACHABLE
#define EX_CATCH_HRESULT_NO_ERRORINFO(_hr)                                      \
    EX_CATCH                                                                    \
    {                                                                           \
        (_hr) = GET_EXCEPTION()->GetHR();                                       \
        _ASSERTE(FAILED(_hr));                                                  \
    }                                                                           \
    EX_END_CATCH(SwallowAllExceptions)

void DECLSPEC_NORETURN ThrowHR(HRESULT hr);
inline void DECLSPEC_NORETURN ThrowHR(HRESULT hr, int msgId)
{
    throw hr;
}
void DECLSPEC_NORETURN ThrowHR(HRESULT hr, SString const &msg);

#define GET_EXCEPTION() ((Exception*)NULL)

void DECLSPEC_NORETURN ThrowWin32(DWORD err);

inline void IfFailThrow(HRESULT hr)
{
    WRAPPER_NO_CONTRACT;

    if (FAILED(hr))
    {
        ThrowHR(hr);
    }
}

/*
inline HRESULT OutOfMemory()
{
    LEAF_CONTRACT;
    return (E_OUTOFMEMORY);
}
*/
#define COMPlusThrowNonLocalized(key, msg)  throw msg

// Set if fatal error (like stack overflow or out of memory) occurred in this process.
extern HRESULT g_hrFatalError;

#endif //CLR_STANDALONE_BINDER

#include "sstring.h"
#ifndef CLR_STANDALONE_BINDER
#include "crtwrap.h"
#include "winwrap.h"
#include "corerror.h"
#include "stresslog.h"
#include "genericstackprobe.h"
#include "staticcontract.h"
#include "entrypoints.h"

#if !defined(_DEBUG_IMPL) && defined(_DEBUG) && !defined(DACCESS_COMPILE)
#define _DEBUG_IMPL 1
#endif

#endif //!CLR_STANDALONE_BINDER


//=========================================================================================== 
// These abstractions hide the difference between legacy desktop CLR's (that don't support
// side-by-side-inproc and rely on a fixed SEH code to identify managed exceptions) and
// new CLR's that support side-by-side inproc.
//
// The new CLR's use a different set of SEH codes to avoid conflicting with the legacy CLR's.
// In addition, to distinguish between EH's raised by different inproc instances of the CLR,
// the module handle of the owning CLR is stored in ExceptionRecord.ExceptionInformation[4].
//
// (Note: all existing SEH's use either only slot [0] or no slots at all. We are leaving
//  slots [1] thru [3] open for future expansion.) 
//===========================================================================================

// Is this exception code one of the special CLR-specific SEH codes that participate in the
// instance-tagging scheme?
BOOL IsInstanceTaggedSEHCode(DWORD dwExceptionCode);


// This set of overloads generates the NumberParameters and ExceptionInformation[] array to
// pass to RaiseException().
//
// Parameters:
//    exceptionArgs:   a fixed-size array of size INSTANCE_TAGGED_SEH_PARAM_ARRAY_SIZE.
//                     This will get filled in by this function. (The module handle goes
//                     in the last slot if this is a side-by-side-inproc enabled build.)
//
//    exceptionArg1... up to four arguments that go in slots [0]..[3]. These depends
//                     the specific requirements of your exception code.
//
// Returns:
//    The NumberParameters to pass to RaiseException().
//
//    Basically, this is  either INSTANCE_TAGGED_SEH_PARAM_ARRAY_SIZE or the count of your
//    fixed arguments depending on whether this tagged-SEH-enabled build.
//
// This function is not permitted to fail.

#define INSTANCE_TAGGED_SEH_PARAM_ARRAY_SIZE 5
DWORD MarkAsThrownByUs(/*out*/ ULONG_PTR exceptionArgs[INSTANCE_TAGGED_SEH_PARAM_ARRAY_SIZE]);
DWORD MarkAsThrownByUs(/*out*/ ULONG_PTR exceptionArgs[INSTANCE_TAGGED_SEH_PARAM_ARRAY_SIZE], ULONG_PTR arg0);
// (the existing system can support more overloads up to 4 fixed arguments but we don't need them at this time.)


// Given an exception record, checks if it's exception code matches a specific exception code
// *and* whether it was tagged by the calling instance of the CLR.
//
// If this is a non-tagged-SEH-enabled build, it is blindly assumed to be tagged by the
// calling instance of the CLR. 
BOOL WasThrownByUs(const EXCEPTION_RECORD *pcER, DWORD dwExceptionCode);


//-----------------------------------------------------------------------------------
// The following group wraps the basic abstracts specifically for EXCEPTION_COMPLUS.
//-----------------------------------------------------------------------------------
BOOL IsComPlusException(const EXCEPTION_RECORD *pcER);
VOID RaiseComPlusException();


//=========================================================================================== 
//=========================================================================================== 


//-------------------------------------------------------------------------------------------
// This routine will generate the most descriptive possible error message for an hresult.
// It will generate at minimum the hex value. It will also try to generate the symbolic name
// (E_POINTER) and the friendly description (from the message tables.)
//
// bNoGeekStuff suppresses hex HR codes. Use this sparingly as most error strings generated by the
// CLR are aimed at developers, not end-users.
//-------------------------------------------------------------------------------------------
void GetHRMsg(HRESULT hresult, SString &result, BOOL bNoGeekStuff = FALSE);


//-------------------------------------------------------------------------------------------
// Similar to GetHRMsg but phrased for top-level exception message.
//-------------------------------------------------------------------------------------------
void GenerateTopLevelHRExceptionMessage(HRESULT hresult, SString &result);


// ---------------------------------------------------------------------------
//   We save current ExceptionPointers using VectoredExceptionHandler.  The save data is only valid
//   duing exception handling.  GetCurrentExceptionPointers returns the saved data.
// ---------------------------------------------------------------------------
void GetCurrentExceptionPointers(PEXCEPTION_POINTERS pExceptionInfo);

// ---------------------------------------------------------------------------
//   We save current ExceptionPointers using VectoredExceptionHandler.  The save data is only valid
//   duing exception handling.  GetCurrentExceptionCode returns the current exception code.
// ---------------------------------------------------------------------------
DWORD GetCurrentExceptionCode();

// ---------------------------------------------------------------------------
//   We save current ExceptionPointers using VectoredExceptionHandler.  The save data is only valid
//   duing exception handling.  Return TRUE if the current exception is hard or soft SO.
// ---------------------------------------------------------------------------
bool IsCurrentExceptionSO();

// ---------------------------------------------------------------------------
//   Return TRUE if the current exception is hard( or soft) SO. Soft SO
//   is defined when the stack probing code is enabled (FEATURE_STACK_PROBE)
// ---------------------------------------------------------------------------
bool IsSOExceptionCode(DWORD exceptionCode);

// ---------------------------------------------------------------------------
//   Standard exception hierarchy & infrastructure for library code & EE
// ---------------------------------------------------------------------------

// ---------------------------------------------------------------------------
// Exception class.  Abstract root exception of our hierarchy.
// ---------------------------------------------------------------------------

class Exception;
class SEHException;


// Exception hierarchy:
/*                                               GetInstanceType
Exception
    |
    |-> HRException                                     Y
    |        |
    |        |-> HRMsgException
    |        |-> COMException
    |
    |-> SEHException                                    Y
    |
    |-> DelegatingException                             Y
    |
    |-> StackOverflowException                          Y
    |
    |-> OutOfMemoryException                            Y
    |
    |-> CLRException                                    Y
              |
              |-> EEException                           Y
              |        |
              |        |-> EEMessageException
              |        |
              |        |-> EEResourceException
              |        |
              |        |-> EECOMException
              |        |
              |        |-> EEFieldException
              |        |
              |        |-> EEMethodException
              |        |
              |        |-> EEArgumentException
              |        |
              |        |-> EETypeLoadException
              |        |
              |        |-> EEFileLoadException
              |
              |-> ObjrefException                          Y
              |
              |-> CLRLastThrownObjectException             Y
*/

class Exception
{
    friend bool DebugIsEECxxExceptionPointer(void* pv);

 private:
    static const int c_type = 0x524f4f54;   // 'ROOT'
    static Exception * g_OOMException;
    static Exception * g_SOException;

 protected:
    Exception           *m_innerException;

 public:
    Exception() {LIMITED_METHOD_DAC_CONTRACT; m_innerException = NULL;}
    virtual ~Exception() {LIMITED_METHOD_DAC_CONTRACT; if (m_innerException != NULL) Exception::Delete(m_innerException); }
    virtual BOOL IsDomainBound() {return m_innerException!=NULL && m_innerException->IsDomainBound();} ;
    virtual HRESULT GetHR() = 0;
    virtual void GetMessage(SString &s);
    virtual IErrorInfo *GetErrorInfo() { LIMITED_METHOD_CONTRACT; return NULL; }
    virtual HRESULT SetErrorInfo() { LIMITED_METHOD_CONTRACT; return S_OK; }
    void SetInnerException(Exception * pInnerException) { LIMITED_METHOD_CONTRACT; m_innerException = pInnerException; }

    // Dynamic type query for catchers
    static int GetType() { LIMITED_METHOD_CONTRACT; return c_type; }
    // !!! If GetInstanceType is implemented, IsSameInstanceType should be implemented
    virtual int GetInstanceType() = 0;
    virtual BOOL IsType(int type) {LIMITED_METHOD_CONTRACT;  return type == c_type; }

    // This is used in CLRException::GetThrowable to detect if we are in a recursive situation.
    virtual BOOL IsSameInstanceType(Exception *pException) = 0;

    // Will create a new instance of the Exception.  Note that this will
    // be free of app domain or thread affinity.  Not every type of exception
    // can be cloned with full fidelity.
    virtual Exception *Clone();

    // DomainBoundClone is a specialized form of cloning which is guaranteed
    // to provide full fidelity.  However, the result is bound to the current
    // app domain and should not be leaked.
    Exception *DomainBoundClone();

    class HandlerState
    {
        enum CaughtFlags
        {
            Caught = 1,
            CaughtSO = 2,
            CaughtCxx = 4,
        };

        DWORD               m_dwFlags;
    public:
        Exception*          m_pExceptionPtr;

        HandlerState();

        void CleanupTry();
        void SetupCatch(INDEBUG_COMMA(__in_z const char * szFile) int lineNum, bool fVMInitialized = true);
        void SucceedCatch();

        BOOL DidCatch() { return (m_dwFlags & Caught); }
        void SetCaught() { m_dwFlags |= Caught; }

        BOOL DidCatchSO() { return (m_dwFlags & CaughtSO); }
        void SetCaughtSO() { m_dwFlags |= CaughtSO; }

        BOOL DidCatchCxx() { return (m_dwFlags & CaughtCxx); }
        void SetCaughtCxx() { m_dwFlags |= CaughtCxx; }
    };

    // Is this exception type considered "uncatchable"?
    BOOL IsTerminal();

    // Is this exception type considered "transient" (would a retry possibly succeed)?
    BOOL IsTransient();
    static BOOL IsTransient(HRESULT hr);

    // Get an HRESULT's source representation, if known
    static LPCSTR GetHRSymbolicName(HRESULT hr);

    static Exception* GetOOMException();

    // Preallocated exceptions:  If there is a preallocated instance of some
    //  subclass of Exception, override this function and return a correct
    //  value.  The default implementation returns constant FALSE
    virtual BOOL IsPreallocatedException(); 
    BOOL IsPreallocatedOOMException();

    static void Delete(Exception* pvMemory);

protected:

    // This virtual method must be implemented by any non abstract Exception
    // derived class. It must allocate a NEW exception of the identical type and
    // copy all the relevant fields from the current exception to the new one.
    // It is NOT responsible however for copying the inner exception. This 
    // will be handled by the base Exception class.
    virtual Exception *CloneHelper();   

    // This virtual method must be implemented by Exception subclasses whose
    // DomainBoundClone behavior is different than their normal clone behavior. 
    // It must allocate a NEW exception of the identical type and
    // copy all the relevant fields from the current exception to the new one.
    // It is NOT responsible however for copying the inner exception. This 
    // will be handled by the base Exception class.    
    virtual Exception *DomainBoundCloneHelper() { return CloneHelper(); }
};

#if 1
template <typename T>
inline void Exception__Delete(T* pvMemory);

template <>
inline void Exception__Delete<Exception>(Exception* pvMemory)
{
  Exception::Delete(pvMemory);
}

NEW_WRAPPER_TEMPLATE1(ExceptionHolderTemplate, Exception__Delete<_TYPE>);
typedef ExceptionHolderTemplate<Exception> ExceptionHolder;
#else

//------------------------------------------------------------------------------
// class ExceptionHolder
//
// This is a very lightweight holder class for use inside the EX_TRY family
//  of macros.  It is based on the standard Holder classes, but has been 
//  highly specialized for this one function, so that extra code can be
//  removed, and the resulting code can be simple enough for all of the
//  non-exceptional-case code to be inlined.
class ExceptionHolder
{
private:
    Exception *m_value;   
    BOOL      m_acquired;
    
public:
    FORCEINLINE ExceptionHolder(Exception *pException = NULL, BOOL take = TRUE)
      : m_value(pException)
    {
        m_acquired = pException && take;
    }

    FORCEINLINE ~ExceptionHolder()
    {
        if (m_acquired)
        {
            Exception::Delete(m_value);
        }
    }

    Exception* operator->() { return m_value; }
    
    void operator=(Exception *p)
    {
        Release();
        m_value = p;
        Acquire();
    }

    BOOL IsNull() { return m_value == NULL; }
    
    operator Exception*() { return m_value; }    
    
    Exception* GetValue() { return m_value; }
    
    void SuppressRelease() { m_acquired = FALSE; }

private:
    void Acquire()
    {
        _ASSERTE(!m_acquired);

        if (!IsNull())
        {
            m_acquired = TRUE;
        }
    }
    void Release()
    {
        if (m_acquired)
        {
            _ASSERTE(!IsNull());
            Exception::Delete(m_value);
            m_acquired = FALSE;
        }
    }

};

#endif     

// ---------------------------------------------------------------------------
// HRException class.  Implements exception API for exceptions generated from HRESULTs
// ---------------------------------------------------------------------------

class HRException : public Exception
{
    friend bool DebugIsEECxxExceptionPointer(void* pv);

 protected:
    HRESULT             m_hr;

 public:
    HRException();
    HRException(HRESULT hr);

    static const int c_type = 0x48522020;   // 'HR  '

    // Dynamic type query for catchers
    static int GetType() {LIMITED_METHOD_DAC_CONTRACT;  return c_type; }
    virtual int GetInstanceType() { LIMITED_METHOD_CONTRACT; return c_type; }
    virtual BOOL IsType(int type) { WRAPPER_NO_CONTRACT; return type == c_type || Exception::IsType(type);  }
    // Virtual overrides
    HRESULT GetHR();

    BOOL IsSameInstanceType(Exception *pException) 
    {
        WRAPPER_NO_CONTRACT; 
        return pException->GetInstanceType() == GetType() && pException->GetHR() == m_hr;
    }

 protected:    
    virtual Exception *CloneHelper()
    {
        WRAPPER_NO_CONTRACT;
        return new HRException(m_hr);
    }    
};

// ---------------------------------------------------------------------------
// HRMessageException class.  Implements exception API for exceptions
// generated from HRESULTs, and includes in info message.
// ---------------------------------------------------------------------------

class HRMsgException : public HRException
{
    friend bool DebugIsEECxxExceptionPointer(void* pv);

 protected:
    SString             m_msg;

 public:
    HRMsgException();
    HRMsgException(HRESULT hr, SString const &msg);

    // Virtual overrides
    void GetMessage(SString &s);

 protected:    
    virtual Exception *CloneHelper()
    {
        WRAPPER_NO_CONTRACT;
        return new HRMsgException(m_hr, m_msg);
    }    
};

// ---------------------------------------------------------------------------
// COMException class.  Implements exception API for standard COM-based error info
// ---------------------------------------------------------------------------

class COMException : public HRException
{
    friend bool DebugIsEECxxExceptionPointer(void* pv);

 private:
    IErrorInfo          *m_pErrorInfo;

 public:
    COMException();
    COMException(HRESULT hr) ;
    COMException(HRESULT hr, IErrorInfo *pErrorInfo);
    ~COMException();

    // Virtual overrides
    IErrorInfo *GetErrorInfo();
    void GetMessage(SString &result);

 protected:    
    virtual Exception *CloneHelper()
    {
        WRAPPER_NO_CONTRACT;
        return new COMException(m_hr, m_pErrorInfo);
    }    
};

// ---------------------------------------------------------------------------
// SEHException class.  Implements exception API for SEH exception info
// ---------------------------------------------------------------------------

class SEHException : public Exception
{
    friend bool DebugIsEECxxExceptionPointer(void* pv);

 public:
    EXCEPTION_RECORD        m_exception;

    SEHException();
    SEHException(EXCEPTION_RECORD *pRecord, T_CONTEXT *pContext = NULL);

    static const int c_type = 0x53454820;   // 'SEH '

    // Dynamic type query for catchers
    static int GetType() {LIMITED_METHOD_CONTRACT;  return c_type; }
    virtual int GetInstanceType() { LIMITED_METHOD_CONTRACT; return c_type; }
    virtual BOOL IsType(int type) { WRAPPER_NO_CONTRACT; return type == c_type || Exception::IsType(type);  }

    BOOL IsSameInstanceType(Exception *pException) 
    {
        WRAPPER_NO_CONTRACT; 
        return pException->GetInstanceType() == GetType() && pException->GetHR() == GetHR();
    }

    // Virtual overrides
    HRESULT GetHR();
    IErrorInfo *GetErrorInfo();
    void GetMessage(SString &result);

 protected:    
    virtual Exception *CloneHelper()
    {
        WRAPPER_NO_CONTRACT;
        return new SEHException(&m_exception);
    }    
};

// ---------------------------------------------------------------------------
// DelegatingException class.  Implements exception API for "foreign" exceptions.
// ---------------------------------------------------------------------------

class DelegatingException : public Exception
{
    Exception *m_delegatedException;
    Exception* GetDelegate();

    enum {DELEGATE_NOT_YET_SET = -1};
    bool IsDelegateSet() {LIMITED_METHOD_DAC_CONTRACT; return m_delegatedException != (Exception*)DELEGATE_NOT_YET_SET; }
    bool IsDelegateValid() {LIMITED_METHOD_DAC_CONTRACT; return IsDelegateSet() && m_delegatedException != NULL; }

 public:

    DelegatingException();
    ~DelegatingException();

    static const int c_type = 0x44454C20;   // 'DEL '

    // Dynamic type query for catchers
    static int GetType() {LIMITED_METHOD_CONTRACT; return c_type; }
    virtual int GetInstanceType() { LIMITED_METHOD_CONTRACT; return c_type; }
    virtual BOOL IsType(int type) { WRAPPER_NO_CONTRACT; return type == c_type || Exception::IsType(type);  }

    BOOL IsSameInstanceType(Exception *pException) 
    {
        WRAPPER_NO_CONTRACT; 
        return pException->GetInstanceType() == GetType() && pException->GetHR() == GetHR();
    }

    // Virtual overrides
    virtual BOOL IsDomainBound() {return Exception::IsDomainBound() ||(m_delegatedException!=NULL && m_delegatedException->IsDomainBound());} ;    
    HRESULT GetHR();
    IErrorInfo *GetErrorInfo();
    void GetMessage(SString &result);
    virtual Exception *Clone();

 protected:    
    virtual Exception *CloneHelper()
    {
        WRAPPER_NO_CONTRACT;
        return new DelegatingException();
    }    
};

//------------------------------------------------------------------------------
// class OutOfMemoryException
//
//   While there could be any number of instances of this class, there is one
//    special instance, the pre-allocated OOM exception.  Storage for that 
//    instance is allocated in the image, so we can always obtain it, even
//    in low memory situations.
//   Note that, in fact, there is only one instance.
//------------------------------------------------------------------------------
class OutOfMemoryException : public Exception
{  
 private:
    static const int c_type = 0x4F4F4D20;   // 'OOM '
    BOOL    bIsPreallocated;

 public:
     OutOfMemoryException() : bIsPreallocated(FALSE) {}
     OutOfMemoryException(BOOL b) : bIsPreallocated(b) {}

    // Dynamic type query for catchers
    static int GetType() {LIMITED_METHOD_CONTRACT;  return c_type; }
    virtual int GetInstanceType() { LIMITED_METHOD_CONTRACT; return c_type; }
    BOOL IsType(int type) { WRAPPER_NO_CONTRACT; return type == c_type || Exception::IsType(type);  }
    
    BOOL IsSameInstanceType(Exception *pException) 
    {
        WRAPPER_NO_CONTRACT; 
        return pException->GetInstanceType() == GetType();
    }

    HRESULT GetHR() {LIMITED_METHOD_DAC_CONTRACT;  return E_OUTOFMEMORY; }
    void GetMessage(SString &result) { WRAPPER_NO_CONTRACT; result.SetASCII("Out Of Memory"); }

    virtual Exception *Clone();
    
    virtual BOOL IsPreallocatedException() { return bIsPreallocated; }
};

#ifndef CLR_STANDALONE_BINDER

template <typename STATETYPE>
class CAutoTryCleanup
{
public:
    DEBUG_NOINLINE CAutoTryCleanup(STATETYPE& refState) :
        m_refState(refState)
    {
        SCAN_SCOPE_BEGIN;
        STATIC_CONTRACT_THROWS;
        STATIC_CONTRACT_SUPPORTS_DAC;

#ifdef ENABLE_CONTRACTS_IMPL
        // This is similar to ClrTryMarkerHolder. We're marking that its okay to throw on this thread now because
        // we're within a try block. We fold this into here strictly for performance reasons... we have one
        // stack-allocated object do the work.
        m_pClrDebugState = GetClrDebugState();
        m_oldOkayToThrowValue = m_pClrDebugState->IsOkToThrow();
        m_pClrDebugState->SetOkToThrow();
#endif
    }

    DEBUG_NOINLINE ~CAutoTryCleanup()
    {
        SCAN_SCOPE_END;
        WRAPPER_NO_CONTRACT;

        m_refState.CleanupTry();

#ifdef ENABLE_CONTRACTS_IMPL
        // Restore the original OkayToThrow value since we're leaving the try block.
       
        m_pClrDebugState->SetOkToThrow( m_oldOkayToThrowValue );
#endif // ENABLE_CONTRACTS_IMPL        
    }

protected:
    STATETYPE& m_refState;

#ifdef ENABLE_CONTRACTS_DATA
private:
    BOOL           m_oldOkayToThrowValue;
    ClrDebugState *m_pClrDebugState;
#endif    
};

// ---------------------------------------------------------------------------
// Throw/Catch macros
//
// Usage:
//
// EX_TRY
// {
//      EX_THROW(HRException, (E_FAIL));
// }
// EX_CATCH
// {
//      Exception *e = GET_EXCEPTION();
//      EX_RETHROW;
// }
// EX_END_CATCH(RethrowTerminalExceptions, RethrowTransientExceptions or SwallowAllExceptions)
//
// ---------------------------------------------------------------------------

// ---------------------------------------------------------------------------
// #NO_HOST_CPP_EH_ONLY
//
// The EX_CATCH* macros defined below can work one of two ways:
//   1. They catch all exceptions, both C++ and SEH exceptions.
//   2. They catch only C++ exceptions.
//
// Which way they are defined depends on what sort of handling of SEH 
// exceptions, like AV's, you wish to have in your DLL. In general we
// do not typically want to catch and swallow AV's.
//
// By default, the macros catch all exceptions. This is how they work when
// compiled into the primary runtime DLL (clr.dll). This is reasonable for
// the CLR becuase it needs to also catch managed exceptions, which are SEH
// exceptions, and because that DLL also includes a vectored exception 
// handler that will take down the process on any AV within clr.dll. 
//
// But for uses of these macros outside of the CLR DLL there are other
// possibilities. If a DLL only uses facilities in Utilcode that throw the
// C++ exceptions defined above, and never needs to catch a managed exception,
// then that DLL should setup the macros to only catch C++ exceptions. That
// way, AV's are not accidentally swallowed and hidden.
//
// On the other hand, if a DLL needs to catch managed exceptions, then it has
// no choice but to also catch all SEH exceptions, including AV's. In that case
// the DLL should also include a vectored handler, like CLR.dll, to take the
// process down on an AV.
//
// The behavior difference is controled by NO_HOST_CPP_EH_ONLY. When defined,
// the EX_CATCH* macros only catch C++ exceptions. When not defined, they catch
// C++ and SEH exceptions.
//
// Note: use of NO_HOST_CPP_EH_ONLY is only valid outside the primary CLR DLLs.
// Thus it is an error to attempt to define it without also defining SELF_NO_HOST.
// ---------------------------------------------------------------------------

#if defined(NO_HOST_CPP_EH_ONLY) && !defined(SELF_NO_HOST)
#error It is incorrect to attempt to have C++-only EH macros when hosted. This is only valid for components outside the runtime DLLs.
#endif

//-----------------------------------------------------------------------
// EX_END_CATCH has a mandatory argument which is one of "RethrowTerminalExceptions",
// "RethrowTransientExceptions", or "SwallowAllExceptions".
//
// If an exception is considered "terminal" (e->IsTerminal()), it should normally
// be allowed to proceed. Hence, most of the time, you should use RethrowTerminalExceptions.
//
// In some cases you will want transient exceptions (terminal plus things like
// resource exhaustion) to proceed as well.  Use RethrowTransientExceptions for this cas.
//
// If you have a good reason to use SwallowAllExceptions, (e.g. a hard COM interop boundary)
// use one of the higher level macros for this if available, or consider developing one.
// Otherwise, clearly document why you're swallowing terminal exceptions. Raw uses of
// SwallowAllExceptions will cause the cleanup police to come knocking on your door
// at some point.
//
// A lot of existing TRY's swallow terminals right now simply because there is
// backout code following the END_CATCH that has to be executed. The solution is
// to replace that backout code with holder objects.


// This is a rotten way to define an enum but as long as we're treating
// "if (optimizabletoconstant)" warnings as fatal errors, we have little choice.

//-----------------------------------------------------------------------

#define RethrowTransientExceptions                                      \
    if (GET_EXCEPTION()->IsTransient())                                 \
    {                                                                   \
        EX_RETHROW;                                                     \
    }                                                                   \

#define RethrowSOExceptions                                             \
    if (__state.DidCatchSO())                                           \
    {                                                                   \
        STATIC_CONTRACT_THROWS_TERMINAL;                                \
        EX_RETHROW;                                                     \
    }                                                                   \


// Don't use this - use RethrowCorruptingExceptions (see below) instead.
#define SwallowAllExceptions ;

//////////////////////////////////////////////////////////////////////
//
// Corrupted State Exception Support
//
/////////////////////////////////////////////////////////////////////

#ifdef FEATURE_CORRUPTING_EXCEPTIONS

#define CORRUPTING_EXCEPTIONS_ONLY(expr) expr
#define COMMA_CORRUPTING_EXCEPTIONS_ONLY(expr) ,expr

// EX_END_CATCH has been modified to not swallow Corrupting Exceptions (CE) when one of the
// following arguments are passed to it:
//
// 1) RethrowTerminalExceptions - rethrows both terminal and corrupting exceptions
// 2) RethrowCorruptingExceptions - swallows all exceptions exception corrupting exceptions. This SHOULD BE USED instead of SwallowAllExceptions.
// 3) RethrowTerminalExceptionsEx - same as (1) but rethrow of CE can be controlled via a condition.
// 4) RethrowCorruptingExceptionsEx - same as (2) but rethrow of CE can be controlled via a condition.
//
// By default, if a CE is encountered when one of the above policies are applied, the runtime will
// ensure that the CE propagates up the stack and not get swallowed unless the developer chooses to override the behaviour. 
// This can be done by using the "Ex" versions above that take a conditional which evaluates to a BOOL. In such a case,
// the CE will *only* be rethrown if the conditional evalutes to TRUE. For examples, refer to COMToCLRWorker or
// DispatchInfo::InvokeMember implementations.
//
// SET_CE_RETHROW_FLAG_FOR_EX_CATCH macros helps evaluate if the CE is to be rethrown or not. This has been redefined in
// Clrex.h to add the condition of evaluating the throwable as well (which is not available outside the VM folder).
//
// Typically, SET_CE_RETHROW_FLAG_FOR_EX_CATCH would rethrow a Corrupted State Exception. However, SO needs to be dealt
// with specially and this work is done during EX_CATCH, by calling SetupCatch against the handler state, and by EX_ENDTRY
// by calling HANDLE_STACKOVERFLOW_AFTER_CATCH.
//
// Passing FALSE as the second argument to IsProcessCorruptedStateException implies that SET_CE_RETHROW_FLAG_FOR_EX_CATCH
// will ensure that we dont rethrow SO and allow EX_ENDTRY to SO specific processing. If none is done, then EX_ENDTRY will
// rethrow SO. By that time stack has been reclaimed and thus, throwing SO will be safe.
//
// We also check the global override flag incase it has been set to force pre-V4 beahviour. "0" implies it has not
// been overriden.
#define SET_CE_RETHROW_FLAG_FOR_EX_CATCH(expr)      (((expr == TRUE) && \
                                                      (CLRConfig::GetConfigValue(CLRConfig::UNSUPPORTED_legacyCorruptedStateExceptionsPolicy) == 0) && \
                                                      IsProcessCorruptedStateException(GetCurrentExceptionCode(), FALSE)))

// This rethrow policy can be used in EX_END_CATCH to swallow all exceptions except the corrupting ones.
// This macro can be used to rethrow the CE based upon a BOOL condition.
#define RethrowCorruptingExceptionsEx(expr)                              \
    if (SET_CE_RETHROW_FLAG_FOR_EX_CATCH(expr))                          \
    {                                                                    \
        STATIC_CONTRACT_THROWS_TERMINAL;                                 \
        EX_RETHROW;                                                      \
    }

#define RethrowCorruptingExceptionsExAndHookRethrow(shouldRethrowExpr, aboutToRethrowExpr) \
    if (SET_CE_RETHROW_FLAG_FOR_EX_CATCH(shouldRethrowExpr))             \
    {                                                                    \
        STATIC_CONTRACT_THROWS_TERMINAL;                                 \
        aboutToRethrowExpr;                                              \
        EX_RETHROW;                                                      \
    }

#else // !FEATURE_CORRUPTING_EXCEPTIONS

#define CORRUPTING_EXCEPTIONS_ONLY(expr)
#define COMMA_CORRUPTING_EXCEPTIONS_ONLY(expr)

// When we dont have support for CE, just map it to SwallowAllExceptions
#define RethrowCorruptingExceptionsEx(expr) SwallowAllExceptions
#define RethrowCorruptingExceptionsExAndHookRethrow(shouldRethrowExpr, aboutToRethrowExpr) SwallowAllExceptions
#define SET_CE_RETHROW_FLAG_FOR_EX_CATCH(expr) !TRUE
#endif // FEATURE_CORRUPTING_EXCEPTIONS

// Map to RethrowCorruptingExceptionsEx so that it does the "right" thing
#define RethrowCorruptingExceptions RethrowCorruptingExceptionsEx(TRUE)

// This macro can be used to rethrow the CE based upon a BOOL condition. It will continue to rethrow terminal
// exceptions unconditionally.
#define RethrowTerminalExceptionsEx(expr)                               \
    if (GET_EXCEPTION()->IsTerminal() ||                                \
        SET_CE_RETHROW_FLAG_FOR_EX_CATCH(expr))                         \
    {                                                                   \
        STATIC_CONTRACT_THROWS_TERMINAL;                                \
        EX_RETHROW;                                                     \
    }                                                                   \


// When applied to EX_END_CATCH, this policy will always rethrow Terminal and Corrupting exceptions if they are
// encountered.
#define RethrowTerminalExceptions  RethrowTerminalExceptionsEx(TRUE)

// Special define to be used in EEStartup that will also check for VM initialization before
// commencing on a path that may use the managed thread object.
#define RethrowTerminalExceptionsWithInitCheck  \
    if ((g_fEEStarted == TRUE) && (GetThread() != NULL))    \
    {                                                       \
        RethrowTerminalExceptions                           \
    }

#ifdef _DEBUG

void ExThrowTrap(const char *fcn, const char *file, int line, const char *szType, HRESULT hr, const char *args);

#define EX_THROW_DEBUG_TRAP(fcn, file, line, szType, hr, args) ExThrowTrap(fcn, file, line, szType, hr, args)

#else

#define EX_THROW_DEBUG_TRAP(fcn, file, line, szType, hr, args)

#endif

#define HANDLE_SO_TOLERANCE_FOR_THROW

#define EX_THROW(_type, _args)                                                          \
    {                                                                                   \
        FAULT_NOT_FATAL();                                                              \
                                                                                        \
        HANDLE_SO_TOLERANCE_FOR_THROW;                                                  \
        _type * ___pExForExThrow =  new _type _args ;                                   \
                /* don't embed file names in retail to save space and avoid IP */       \
                /* a findstr /n will allow you to locate it in a pinch */               \
        STRESS_LOG3(LF_EH, LL_INFO100, "EX_THROW Type = 0x%x HR = 0x%x, "               \
                    INDEBUG(__FILE__) " line %d\n", _type::GetType(),                   \
                    ___pExForExThrow->GetHR(), __LINE__);                               \
        EX_THROW_DEBUG_TRAP(__FUNCTION__, __FILE__, __LINE__, #_type, ___pExForExThrow->GetHR(), #_args);          \
        PAL_CPP_THROW(_type *, ___pExForExThrow);                                       \
    }

//--------------------------------------------------------------------------------
// Clones an exception into the current domain. Also handles special cases for
// OOM and other stuff. Making this a function so we don't inline all this logic
// every place we call EX_THROW_WITH_INNER.
//--------------------------------------------------------------------------------
Exception *ExThrowWithInnerHelper(Exception *inner);

// This macro will set the m_innerException into the newly created exception
// The passed in _type has to be derived from CLRException. You cannot put OOM
// as the inner exception. If we are throwing in OOM case, allocate more memory (this macro will clone)
// does not make any sense. 
// 
#define EX_THROW_WITH_INNER(_type, _args, _inner)                                       \
    {                                                                                   \
        FAULT_NOT_FATAL();                                                              \
                                                                                        \
        HANDLE_SO_TOLERANCE_FOR_THROW;                                                  \
        Exception *_inner2 = ExThrowWithInnerHelper(_inner);                            \
        _type *___pExForExThrow =  new _type _args ;                                    \
        ___pExForExThrow->SetInnerException(_inner2);                                   \
        STRESS_LOG3(LF_EH, LL_INFO100, "EX_THROW_WITH_INNER Type = 0x%x HR = 0x%x, "    \
                    INDEBUG(__FILE__) " line %d\n", _type::GetType(),                   \
                    ___pExForExThrow->GetHR(), __LINE__);                               \
        EX_THROW_DEBUG_TRAP(__FUNCTION__, __FILE__, __LINE__, #_type, ___pExForExThrow->GetHR(), #_args);          \
        PAL_CPP_THROW(_type *, ___pExForExThrow);                                       \
    }

//#define IsCLRException(ex) ((ex !=NULL) && ex->IsType(CLRException::GetType())

#define EX_TRY EX_TRY_CUSTOM(Exception::HandlerState, , DelegatingException /* was SEHException*/)

#ifndef INCONTRACT
#ifdef ENABLE_CONTRACTS
#define INCONTRACT(x)          x
#else
#define INCONTRACT(x)
#endif
#endif

#define EX_TRY_CUSTOM(STATETYPE, STATEARG, DEFAULT_EXCEPTION_TYPE)                      \
    {                                                                                   \
        STATETYPE               __state STATEARG;                                       \
        typedef DEFAULT_EXCEPTION_TYPE  __defaultException_t;                           \
        SCAN_EHMARKER();                                                                \
        PAL_CPP_TRY                                                                     \
        {                                                                               \
            SCAN_EHMARKER_TRY();                                                        \
            SCAN_EHMARKER();                                                            \
            PAL_CPP_TRY                                                                 \
            {                                                                           \
                SCAN_EHMARKER_TRY();                                                    \
                CAutoTryCleanup<STATETYPE> __autoCleanupTry(__state);                   \
                /* prevent annotations from being dropped by optimizations in debug */  \
                INDEBUG(static bool __alwayszero;)                                      \
                INDEBUG(VolatileLoad(&__alwayszero);)                                   \
                {                                                                       \
                    /* this is necessary for Rotor exception handling to work */        \
                    DEBUG_ASSURE_NO_RETURN_BEGIN(EX_TRY)                                \

                                                                                        
#define EX_CATCH_IMPL_EX(DerivedExceptionClass)                                         \
                    DEBUG_ASSURE_NO_RETURN_END(EX_TRY)                                  \
                }                                                                       \
                SCAN_EHMARKER_END_TRY();                                                \
            }                                                                           \
            PAL_CPP_CATCH_DERIVED (DerivedExceptionClass, __pExceptionRaw)              \
            {                                                                           \
                SCAN_EHMARKER_CATCH();                                                  \
                __state.SetCaughtCxx();                                                 \
                __state.m_pExceptionPtr = __pExceptionRaw;                              \
                SCAN_EHMARKER_END_CATCH();                                              \
                SCAN_IGNORE_THROW_MARKER;                                               \
                PAL_CPP_RETHROW;                                                        \
            }                                                                           \
            PAL_CPP_ENDTRY                                                              \
            SCAN_EHMARKER_END_TRY();                                                    \
        }                                                                               \
        PAL_CPP_CATCH_ALL                                                               \
        {                                                                               \
            SCAN_EHMARKER_CATCH();                                                      \
            VALIDATE_BACKOUT_STACK_CONSUMPTION;                                         \
            __defaultException_t __defaultException;                                    \
            CHECK::ResetAssert();                                                       \
            ExceptionHolder __pException(__state.m_pExceptionPtr);                      \
            /* work around unreachable code warning */                                  \
            if (true) {                                                                 \
                DEBUG_ASSURE_NO_RETURN_BEGIN(EX_CATCH)                                  \
                /* don't embed file names in retail to save space and avoid IP */       \
                /* a findstr /n will allow you to locate it in a pinch */               \
                __state.SetupCatch(INDEBUG_COMMA(__FILE__) __LINE__);                   \

#define EX_CATCH_IMPL EX_CATCH_IMPL_EX(Exception)

//
// What we really need a different version of EX_TRY with one less try scope, but this
// gets us what we need for now...
//
#define EX_CATCH_IMPL_CPP_ONLY                                                          \
                    DEBUG_ASSURE_NO_RETURN_END(EX_TRY)                                  \
                }                                                                       \
                SCAN_EHMARKER_END_TRY();                                                \
            }                                                                           \
            PAL_CPP_CATCH_DERIVED (Exception, __pExceptionRaw)                          \
            {                                                                           \
                SCAN_EHMARKER_CATCH();                                                  \
                __state.SetCaughtCxx();                                                 \
                __state.m_pExceptionPtr = __pExceptionRaw;                              \
                SCAN_EHMARKER_END_CATCH();                                              \
                SCAN_IGNORE_THROW_MARKER;                                               \
                PAL_CPP_RETHROW;                                                        \
            }                                                                           \
            PAL_CPP_ENDTRY                                                              \
            SCAN_EHMARKER_END_TRY();                                                    \
        }                                                                               \
        PAL_CPP_CATCH_EXCEPTION_NOARG                                                   \
        {                                                                               \
            SCAN_EHMARKER_CATCH();                                                      \
            VALIDATE_BACKOUT_STACK_CONSUMPTION;                                         \
            __defaultException_t __defaultException;                                    \
            CHECK::ResetAssert();                                                       \
            ExceptionHolder __pException(__state.m_pExceptionPtr);                      \
            /* work around unreachable code warning */                                  \
            if (true) {                                                                 \
                DEBUG_ASSURE_NO_RETURN_BEGIN(EX_CATCH)                                  \
                /* don't embed file names in retail to save space and avoid IP */       \
                /* a findstr /n will allow you to locate it in a pinch */               \
                __state.SetupCatch(INDEBUG_COMMA(__FILE__) __LINE__);                   \


// Here we finally define the EX_CATCH* macros that will be used throughout the system.
// These can catch C++ and SEH exceptions, or just C++ exceptions. 
// See code:NO_HOST_CPP_EH_ONLY for more details.
//
// Note: we make it illegal to use forms that are redundant with the basic EX_CATCH
// version. I.e., in the C++ & SEH version, EX_CATCH_CPP_AND_SEH is the same as EX_CATCH. 
// Likewise, in the C++ only version, EX_CATCH_CPP_ONLY is redundant with EX_CATCH.

#ifndef NO_HOST_CPP_EH_ONLY
#define EX_CATCH                EX_CATCH_IMPL
#define EX_CATCH_EX             EX_CATCH_IMPL_EX
#define EX_CATCH_CPP_ONLY       EX_CATCH_IMPL_CPP_ONLY
#define EX_CATCH_CPP_AND_SEH    Dont_Use_EX_CATCH_CPP_AND_SEH
#else
#define EX_CATCH                EX_CATCH_IMPL_CPP_ONLY
#define EX_CATCH_CPP_ONLY       Dont_Use_EX_CATCH_CPP_ONLY
#define EX_CATCH_CPP_AND_SEH    EX_CATCH_IMPL

// Note: at this time we don't have a use case for EX_CATCH_EX, and we do not have 
// the C++-only version of the implementation available. Thus we disallow its use at this time. 
// If a real use case arises then we should go ahead and enable this.
#define EX_CATCH_EX             Dont_Use_EX_CATCH_EX
#endif

#define EX_END_CATCH_UNREACHABLE                                                        \
                DEBUG_ASSURE_NO_RETURN_END(EX_CATCH)                                    \
            }                                                                           \
            SCAN_EHMARKER_END_CATCH();                                                  \
            UNREACHABLE();                                                              \
        }                                                                               \
        PAL_CPP_ENDTRY                                                                  \
    }                                                                                   \


// "terminalexceptionpolicy" must be one of "RethrowTerminalExceptions", 
// "RethrowTransientExceptions", or "SwallowAllExceptions"

#define EX_END_CATCH(terminalexceptionpolicy)                                           \
                terminalexceptionpolicy;                                                \
                __state.SucceedCatch();                                                 \
                DEBUG_ASSURE_NO_RETURN_END(EX_CATCH)                                    \
            }                                                                           \
            SCAN_EHMARKER_END_CATCH();                                                  \
        }                                                                               \
        EX_ENDTRY                                                                       \
    }                                                                                   \


#define EX_END_CATCH_FOR_HOOK                                                           \
                __state.SucceedCatch();                                                 \
                DEBUG_ASSURE_NO_RETURN_END(EX_CATCH)                                    \
                ANNOTATION_HANDLER_END;                                                 \
            }                                                                           \
            SCAN_EHMARKER_END_CATCH();                                                  \
        }                                                                               \
        EX_ENDTRY                                                                       \
            
#define EX_ENDTRY                                                                       \
        PAL_CPP_ENDTRY                                                                  \
        if (__state.DidCatch())                                                         \
        {                                                                               \
            RESTORE_SO_TOLERANCE_STATE;                                                 \
        }                                                                               \
        if (__state.DidCatchSO())                                                       \
        {                                                                               \
            HANDLE_STACKOVERFLOW_AFTER_CATCH;                                           \
        }

#define EX_RETHROW                                                                      \
        {                                                                               \
            __pException.SuppressRelease();                                             \
            PAL_CPP_RETHROW;                                                            \
        }                                                                               \

 // Define a copy of GET_EXCEPTION() that will not be redefined by clrex.h
#define GET_EXCEPTION() (__pException == NULL ? &__defaultException : __pException.GetValue())
#define EXTRACT_EXCEPTION() (__pException.Extract())


//==============================================================================
// High-level macros for common uses of EX_TRY. Try using these rather
// than the raw EX_TRY constructs.
//==============================================================================

//===================================================================================
// Macro for converting exceptions into HR internally. Unlike EX_CATCH_HRESULT,
// it does not set up IErrorInfo on the current thread.
//
// Usage:
//
//   HRESULT hr = S_OK;
//   EX_TRY
//   <do managed stuff>
//   EX_CATCH_HRESULT_NO_ERRORINFO(hr);
//   return hr;
//
// Comments:
//   Since IErrorInfo is not set up, this does not require COM interop to be started.
//===================================================================================

#define EX_CATCH_HRESULT_NO_ERRORINFO(_hr)                                      \
    EX_CATCH                                                                    \
    {                                                                           \
        (_hr) = GET_EXCEPTION()->GetHR();                                       \
        _ASSERTE(FAILED(_hr));                                                  \
    }                                                                           \
    EX_END_CATCH(SwallowAllExceptions)

    
//===================================================================================
// Macro for catching managed exception object.
//
// Usage:
//
//   OBJECTREF pThrowable = NULL;
//   EX_TRY
//   <do managed stuff>
//   EX_CATCH_THROWABLE(&pThrowable);
//
//===================================================================================

#define EX_CATCH_THROWABLE(ppThrowable)                                         \
    EX_CATCH                                                                    \
    {                                                                           \
        if (NULL != ppThrowable)                                                \
        {                                                                       \
            *ppThrowable = GET_THROWABLE();                                     \
        }                                                                       \
    }                                                                           \
    EX_END_CATCH(SwallowAllExceptions)


#ifdef FEATURE_COMINTEROP
    
//===================================================================================
// Macro for defining external entrypoints such as COM interop boundaries.
// The boundary will catch all exceptions (including terminals) and convert
// them into HR/IErrorInfo pairs as appropriate.
//
// Usage:
//
//   HRESULT hr = S_OK;
//   EX_TRY
//   <do managed stuff>
//   EX_CATCH_HRESULT(hr);
//   return hr;
//
// Comments:
//   Note that IErrorInfo will automatically be set up on the thread if appropriate.
//===================================================================================

#define EX_CATCH_HRESULT(_hr)                                                   \
    EX_CATCH                                                                    \
    {                                                                           \
        (_hr) = GET_EXCEPTION()->GetHR();                                       \
        _ASSERTE(FAILED(_hr));                                                  \
        IErrorInfo *pErr = GET_EXCEPTION()->GetErrorInfo();                     \
        if (pErr != NULL)                                                       \
        {                                                                       \
            SetErrorInfo(0, pErr);                                              \
            pErr->Release();                                                    \
        }                                                                       \
    }                                                                           \
    EX_END_CATCH(SwallowAllExceptions)

//===================================================================================
// Macro to make conditional catching more succinct.
//
// Usage:
//
//   EX_TRY
//   ...
//   EX_CATCH_HRESULT_IF(IsHRESULTForExceptionKind(GET_EXCEPTION()->GetHR(), kFileNotFoundException));
//===================================================================================

#define EX_CATCH_HRESULT_IF(HR, ...)                                            \
    EX_CATCH                                                                    \
    {                                                                           \
        (HR) = GET_EXCEPTION()->GetHR();                                        \
                                                                                \
        /* Rethrow if condition is false. */                                    \
        if (!(__VA_ARGS__))                                                     \
            EX_RETHROW;                                                         \
                                                                                \
        _ASSERTE(FAILED(HR));                                                   \
        IErrorInfo *pErr = GET_EXCEPTION()->GetErrorInfo();                     \
        if (pErr != NULL)                                                       \
        {                                                                       \
            SetErrorInfo(0, pErr);                                              \
            pErr->Release();                                                    \
        }                                                                       \
    }                                                                           \
    EX_END_CATCH(SwallowAllExceptions)


//===================================================================================
// Variant of the above Macro for used by ngen and mscorsvc to add
// a RetailAssert when a reg key is set if we get an unexpected HRESULT
// from one of the RPC calls.
//===================================================================================

#define EX_CATCH_HRESULT_AND_NGEN_CLEAN(_hr)                                    \
    EX_CATCH                                                                    \
    {                                                                           \
        (_hr) = GET_EXCEPTION()->GetHR();                                       \
        RetailAssertIfExpectedClean();                                          \
        /* Enable this assert after we fix EH so that GetHR() never */          \
        /* mistakenly returns S_OK */                                           \
        /***/                                                                   \
        /* _ASSERTE(FAILED(_hr)); */                                            \
        IErrorInfo *pErr = GET_EXCEPTION()->GetErrorInfo();                     \
        if (pErr != NULL)                                                       \
        {                                                                       \
            SetErrorInfo(0, pErr);                                              \
            pErr->Release();                                                    \
        }                                                                       \
    }                                                                           \
    EX_END_CATCH(SwallowAllExceptions)

#else // FEATURE_COMINTEROP

#define EX_CATCH_HRESULT(_hr) EX_CATCH_HRESULT_NO_ERRORINFO(_hr)

#endif // FEATURE_COMINTEROP

//===================================================================================
// Macro for containing normal exceptions but letting terminal exceptions continue to propagate.
//
// Usage:
//
//  EX_TRY
//  {
//      ...your stuff...
//  }
//  EX_SWALLOW_NONTERMINAL
//
// Remember, terminal exceptions (such as ThreadAbort) will still throw out of this
// block. So don't use this as a substitute for exception-safe cleanup!
//===================================================================================

#define EX_SWALLOW_NONTERMINAL                           \
    EX_CATCH                                             \
    {                                                    \
    }                                                    \
    EX_END_CATCH(RethrowTerminalExceptions)              \


//===================================================================================
// Macro for containing normal exceptions but letting transient exceptions continue to propagate.
//
// Usage:
//
//  EX_TRY
//  {
//      ...your stuff...
//  }
//  EX_SWALLOW_NONTRANSIENT
//
// Terminal exceptions (such as ThreadAbort and OutOfMemory) will still throw out of this
// block. So don't use this as a substitute for exception-safe cleanup!
//===================================================================================

#define EX_SWALLOW_NONTRANSIENT                          \
    EX_CATCH                                             \
    {                                                    \
    }                                                    \
    EX_END_CATCH(RethrowTransientExceptions)             \


//===================================================================================
// Macro for observing or wrapping exceptions in flight.
//
// Usage:
//
//   EX_TRY
//   {
//      ... your stuff ...
//   }
//   EX_HOOK
//   {
//      ... your stuff ...
//   }
//   EX_END_HOOK
//   ... control will never get here ...
//
//
// EX_HOOK is like EX_CATCH except that you can't prevent the
// exception from being rethrown. You can throw a new exception inside the hook
// (for example, if you want to wrap the exception in flight with your own).
// But if control reaches the end of the hook, the original exception gets rethrown.
//
// Avoid using EX_HOOK for conditional backout if a destructor-based holder
// will suffice. Because these macros are implemented on top of SEH, using them will
// prevent the use of holders anywhere else inside the same function. That is, instead
// of saying this:
//
//     EX_TRY          // DON'T DO THIS
//     {
//          thing = new Thing();
//          blah
//     }
//     EX_HOOK
//     {
//          delete thing; // if it failed, we don't want to keep the Thing.
//     }
//     EX_END_HOOK
//
// do this:
//
//     Holder<Thing> thing = new Thing();   //DO THIS INSTEAD
//     blah
//     // If we got here, we succeeded. So tell holder we want to keep the thing.
//     thing.SuppressRelease();
//
//	We won't rethrow the exception if it is a Stack Overflow exception. Instead, we'll throw a new
//   exception. This will allow the stack to unwind point, and so we won't be jeopardizing a
//   second stack overflow.
//===================================================================================
#define EX_HOOK                                          \
    EX_CATCH                                             \
    {                                                    \

#define EX_END_HOOK                                      \
    }                                                    \
    ANNOTATION_HANDLER_END;                              \
    if (IsCurrentExceptionSO())                          \
        __state.SetCaughtSO();                           \
    VM_NO_SO_INFRASTRUCTURE_CODE(_ASSERTE(!__state.DidCatchSO());) \
    if (!__state.DidCatchSO())                           \
        EX_RETHROW;                                      \
    EX_END_CATCH_FOR_HOOK;                               \
    SO_INFRASTRUCTURE_CODE(if (__state.DidCatchSO()))    \
        SO_INFRASTRUCTURE_CODE(ThrowStackOverflow();)    \
    }                                                    \

// ---------------------------------------------------------------------------
// Inline implementations. Pay no attention to that man behind the curtain.
// ---------------------------------------------------------------------------

inline Exception::HandlerState::HandlerState()
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_SO_TOLERANT;
    STATIC_CONTRACT_CANNOT_TAKE_LOCK;
    STATIC_CONTRACT_SUPPORTS_DAC;

    m_dwFlags = 0;
    m_pExceptionPtr = NULL;

#if defined(STACK_GUARDS_DEBUG) && defined(ENABLE_CONTRACTS_IMPL)
    // If we have a debug state, use its setting for SO tolerance.  The default
    // is SO-tolerant if we have no debug state.  Can't probe w/o debug state and 
    // can't enter SO-interolant mode w/o probing.
    GetClrDebugState();
#endif    
}

inline void Exception::HandlerState::CleanupTry()
{
    LIMITED_METHOD_DAC_CONTRACT;
}

inline void Exception::HandlerState::SetupCatch(INDEBUG_COMMA(__in_z const char * szFile) int lineNum, bool fVMInitialized /* = true */)
{
    WRAPPER_NO_CONTRACT;

    if (fVMInitialized)
    {
        // Calling into IsCurrentExceptionSO will end up using various VM support entities (e.g. TLS slots, accessing CExecutionEngine
        // implementation that accesses other VM specific data, etc) that may not be ready/initialized
        // until the VM is initialized. 
        //
        // This is particularly important when we have exceptions thrown/triggerred during runtime's initialization
        // and accessing such data can result in possible recursive AV's in the runtime.
        if (IsCurrentExceptionSO())
            SetCaughtSO();
    }

    /* don't embed file names in retail to save space and avoid IP */
    /* a findstr /n will allow you to locate it in a pinch */
#ifdef _DEBUG
    STRESS_LOG2(LF_EH, LL_INFO100, "EX_CATCH %s line %d\n", szFile, lineNum);
#else
    STRESS_LOG1(LF_EH, LL_INFO100, "EX_CATCH line %d\n", lineNum);
#endif

    SetCaught();
}

inline void Exception::HandlerState::SucceedCatch()
{
    LIMITED_METHOD_DAC_CONTRACT;
}

inline HRException::HRException()
  : m_hr(E_UNEXPECTED)
{
    LIMITED_METHOD_CONTRACT;
    SUPPORTS_DAC;
}

inline HRException::HRException(HRESULT hr)
  : m_hr(hr)
{
    LIMITED_METHOD_CONTRACT;
    SUPPORTS_DAC;

    // Catchers assume only failing hresults
    _ASSERTE(FAILED(hr));
}

inline HRMsgException::HRMsgException()
  : HRException()
{
    LIMITED_METHOD_CONTRACT;
}

inline HRMsgException::HRMsgException(HRESULT hr, SString const &s)
  : HRException(hr), m_msg(s)
{
    WRAPPER_NO_CONTRACT;
}

inline COMException::COMException()
  : HRException(),
  m_pErrorInfo(NULL)
{
    WRAPPER_NO_CONTRACT;
}

inline COMException::COMException(HRESULT hr)
  : HRException(hr),
  m_pErrorInfo(NULL)
{
    LIMITED_METHOD_CONTRACT;
}

inline COMException::COMException(HRESULT hr, IErrorInfo *pErrorInfo)
  : HRException(hr),
  m_pErrorInfo(pErrorInfo)
{
    LIMITED_METHOD_CONTRACT;
}

inline SEHException::SEHException()
{
    LIMITED_METHOD_CONTRACT;
    memset(&m_exception, 0, sizeof(EXCEPTION_RECORD));
}

inline SEHException::SEHException(EXCEPTION_RECORD *pointers, T_CONTEXT *pContext)
{
    LIMITED_METHOD_CONTRACT;
    memcpy(&m_exception, pointers, sizeof(EXCEPTION_RECORD));
}

// The exception throwing helpers are intentionally not inlined
// Exception throwing is a rare slow codepath that should be optimized for code size

void DECLSPEC_NORETURN ThrowHR(HRESULT hr);
void DECLSPEC_NORETURN ThrowHR(HRESULT hr, SString const &msg);
void DECLSPEC_NORETURN ThrowHR(HRESULT hr, UINT uText);
void DECLSPEC_NORETURN ThrowWin32(DWORD err);
void DECLSPEC_NORETURN ThrowLastError();
void DECLSPEC_NORETURN ThrowOutOfMemory();
void DECLSPEC_NORETURN ThrowStackOverflow();
void DECLSPEC_NORETURN ThrowMessage(LPCSTR message, ...);

#undef IfFailThrow
inline HRESULT IfFailThrow(HRESULT hr)
{
    WRAPPER_NO_CONTRACT;

    if (FAILED(hr))
    {
        ThrowHR(hr);
    }

    return hr;
}

inline HRESULT IfFailThrow(HRESULT hr, SString &msg)
{
    WRAPPER_NO_CONTRACT;
    
    if (FAILED(hr))
    {
        ThrowHR(hr, msg);
    }

    return hr;
}

inline HRESULT IfTransientFailThrow(HRESULT hr)
{
    WRAPPER_NO_CONTRACT;

    if (FAILED(hr) && Exception::IsTransient(hr))
    {
        ThrowHR(hr);
    }

    return hr;
}

// Set if fatal error (like stack overflow or out of memory) occurred in this process.
GVAL_DECL(HRESULT, g_hrFatalError);

#endif // !CLR_STANDALONE_BINDER
#endif  // _EX_H_
