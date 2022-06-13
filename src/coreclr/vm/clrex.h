// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

//
// ---------------------------------------------------------------------------
// CLREx.h
// ---------------------------------------------------------------------------


#ifndef _CLREX_H_
#define _CLREX_H_

#include <ex.h>

#include "runtimeexceptionkind.h"
#include "interoputil.h"

class BaseBind;
class AssemblySpec;
class PEAssembly;

enum StackTraceElementFlags
{
    // Set if this element represents the last frame of the foreign exception stack trace
    STEF_LAST_FRAME_FROM_FOREIGN_STACK_TRACE = 0x0001,

    // Set if the "ip" field has already been adjusted (decremented)
    STEF_IP_ADJUSTED = 0x0002,
};

// This struct is used by SOS in the diagnostic repo.
// See: https://github.com/dotnet/diagnostics/blob/9ff35f13af2f03a68a166cfd53f1a4bb32425f2f/src/SOS/Strike/strike.cpp#L2245
struct StackTraceElement
{
    UINT_PTR        ip;
    UINT_PTR        sp;
    PTR_MethodDesc  pFunc;
    INT             flags;      // This is StackTraceElementFlags but it needs to be "int" sized for compatibility with SOS.

    bool operator==(StackTraceElement const & rhs) const
    {
        return ip == rhs.ip
            && sp == rhs.sp
            && pFunc == rhs.pFunc;
    }

    bool operator!=(StackTraceElement const & rhs) const
    {
        return !(*this == rhs);
    }
};

// This struct is used by SOS in the diagnostic repo.
// See: https://github.com/dotnet/diagnostics/blob/9ff35f13af2f03a68a166cfd53f1a4bb32425f2f/src/SOS/Strike/strike.cpp#L2669
class StackTraceInfo
{
private:
    // for building stack trace info
    StackTraceElement*  m_pStackTrace;      // pointer to stack trace storage
    unsigned            m_cStackTrace;      // size of stack trace storage
    unsigned            m_dFrameCount;      // current frame in stack trace
    unsigned            m_cDynamicMethodItems; // number of items in the Dynamic Method array
    unsigned            m_dCurrentDynamicIndex; // index of the next location where the resolver object will be stored

public:
    void Init();
    BOOL IsEmpty();
    void AllocateStackTrace();
    void ClearStackTrace();
    void FreeStackTrace();
    void SaveStackTrace(BOOL bAllowAllocMem, OBJECTHANDLE hThrowable, BOOL bReplaceStack, BOOL bSkipLastElement);
    BOOL AppendElement(BOOL bAllowAllocMem, UINT_PTR currentIP, UINT_PTR currentSP, MethodDesc* pFunc, CrawlFrame* pCf);

    void GetLeafFrameInfo(StackTraceElement* pStackTraceElement);
};


// ---------------------------------------------------------------------------
// CLRException represents an exception which has a managed representation.
// It adds the generic method GetThrowable().
// ---------------------------------------------------------------------------
class CLRException : public Exception
{
    friend bool DebugIsEECxxExceptionPointer(void* pv);
    friend class CLRLastThrownObjectException;
 private:
    static const int c_type = 0x434c5220;   // 'CLR '

 protected:
    OBJECTHANDLE            m_throwableHandle;

    void SetThrowableHandle(OBJECTHANDLE throwable);
    OBJECTHANDLE GetThrowableHandle() { return m_throwableHandle; }


    CLRException();
public:
    ~CLRException();

    OBJECTREF GetThrowable();

    // Dynamic type query for catchers
    static int GetType() {LIMITED_METHOD_CONTRACT;  return c_type; }
    virtual int GetInstanceType() { LIMITED_METHOD_CONTRACT; return c_type; }
    BOOL IsType(int type) { WRAPPER_NO_CONTRACT; return type == c_type || Exception::IsType(type);  }

    BOOL IsSameInstanceType(Exception *pException)
    {
        STATIC_CONTRACT_MODE_COOPERATIVE;
        STATIC_CONTRACT_GC_TRIGGERS;
        STATIC_CONTRACT_NOTHROW;

        if (pException->GetInstanceType() != GetInstanceType())
        {
            return FALSE;
        }
        OBJECTREF mine = GetThrowable();
        OBJECTREF other = ((CLRException*)pException)->GetThrowable();
        return mine != NULL && other != NULL &&
            mine->GetMethodTable() == other->GetMethodTable();
    }

    // Overrides
    virtual BOOL IsDomainBound()
    {
        //@TODO special case for preallocated exceptions?
        return TRUE;
    }

    HRESULT GetHR();
    IErrorInfo *GetErrorInfo();
    HRESULT SetErrorInfo();

    void GetMessage(SString &result);

 protected:

    virtual OBJECTREF CreateThrowable() { LIMITED_METHOD_CONTRACT; return NULL; }

 public: // These are really private, but are used by the exception macros


    // Accessors for all the preallocated exception objects.
    static OBJECTREF GetPreallocatedOutOfMemoryException();
    static OBJECTREF GetPreallocatedStackOverflowException();
    static OBJECTREF GetPreallocatedExecutionEngineException();

    static OBJECTHANDLE GetPreallocatedStackOverflowExceptionHandle();
    static OBJECTHANDLE GetPreallocatedHandleForObject(OBJECTREF o);

    // Use these to determine if a handle or object ref is one of the preallocated handles or object refs.
    static BOOL IsPreallocatedExceptionObject(OBJECTREF o);
    static BOOL IsPreallocatedExceptionHandle(OBJECTHANDLE h);

    // Prefer a new exception if we can make one.  If we cannot, then give back the pre-allocated OOM.
    static OBJECTREF GetBestException(HRESULT hr, PTR_MethodTable mt);
    static OBJECTREF GetBestOutOfMemoryException();
    static OBJECTREF GetBestBaseException();
    static OBJECTREF GetBestThreadAbortException();

    static OBJECTREF GetThrowableFromException(Exception *pException);
    static OBJECTREF GetThrowableFromExceptionRecord(EXCEPTION_RECORD *pExceptionRecord);

    class HandlerState : public Exception::HandlerState
    {
    public:
        Thread* m_pThread;
        Frame*  m_pFrame;
        BOOL    m_fPreemptiveGCDisabled;

        enum NonNullThread
        {
            ThreadIsNotNull
        };

        HandlerState(Thread * pThread);
        HandlerState(Thread * pThread, NonNullThread dummy);

        void CleanupTry();
        void SetupCatch(INDEBUG_COMMA(_In_z_ const char * szFile) int lineNum);
#ifdef LOGGING // Use parent implementation that inlines into nothing in retail build
        void SucceedCatch();
#endif
        void SetupFinally();
    };
};

// prototype for helper function to get exception object from thread's LastThrownObject.
void GetLastThrownObjectExceptionFromThread(Exception **ppException);


// ---------------------------------------------------------------------------
// EEException is a CLR exception subclass which has purely unmanaged representation.
// The standard methods will not do any GC dangerous operations.  Thus you
// can throw and catch such an exception without regard to GC mode.
// ---------------------------------------------------------------------------

class EEException : public CLRException
{
    friend bool DebugIsEECxxExceptionPointer(void* pv);

 private:
    static const int c_type = 0x45452020;   // 'EE '

 public:
    const RuntimeExceptionKind    m_kind;

    EEException(RuntimeExceptionKind kind);
    EEException(HRESULT hr);

    // Dynamic type query for catchers
    static int GetType() {LIMITED_METHOD_CONTRACT;  return c_type; }
    virtual int GetInstanceType() { LIMITED_METHOD_CONTRACT; return c_type; }
    BOOL IsType(int type) { WRAPPER_NO_CONTRACT; return type == c_type || CLRException::IsType(type); }

    BOOL IsSameInstanceType(Exception *pException)
    {
        WRAPPER_NO_CONTRACT;
        return pException->GetInstanceType() == GetType() && ((EEException*)pException)->m_kind == m_kind;
    }

    // Virtual overrides
    HRESULT GetHR();
    IErrorInfo *GetErrorInfo();
    void GetMessage(SString &result);
    OBJECTREF CreateThrowable();

    // GetThrowableMessage returns a message to be stored in the throwable.
    // Returns FALSE if there is no useful value.
    virtual BOOL GetThrowableMessage(SString &result);

    static BOOL GetResourceMessage(UINT iResourceID, SString &result,
                                   const SString &arg1 = SString::Empty(), const SString &arg2 = SString::Empty(),
                                   const SString &arg3 = SString::Empty(), const SString &arg4 = SString::Empty(),
                                   const SString &arg5 = SString::Empty(), const SString &arg6 = SString::Empty());

    // Note: reKind-->hr is a one-to-many relationship.
    //
    //   each reKind is associated with one or more hresults.
    //   every hresult is associated with exactly one reKind (with kCOMException being the catch-all.)
    static RuntimeExceptionKind GetKindFromHR(HRESULT hr);
  protected:
    static HRESULT GetHRFromKind(RuntimeExceptionKind reKind);

#ifdef _DEBUG
    EEException() : m_kind(kException)
    {
        // Used only for DebugIsEECxxExceptionPointer to get the vtable pointer.
        // We need a variant which does not allocate memory.
    }
#endif // _DEBUG

    virtual Exception *CloneHelper()
    {
        WRAPPER_NO_CONTRACT;
        return new EEException(m_kind);
    }

};

// ---------------------------------------------------------------------------
// EEMessageException is an EE exception subclass composed of a type and
// an unmanaged message of some sort.
// ---------------------------------------------------------------------------

class EEMessageException : public EEException
{
    friend bool DebugIsEECxxExceptionPointer(void* pv);

 private:
    HRESULT             m_hr;
    UINT                m_resID;
    InlineSString<32>   m_arg1;
    InlineSString<32>   m_arg2;
    SString             m_arg3;
    SString             m_arg4;
    SString             m_arg5;
    SString             m_arg6;

 public:
    EEMessageException(RuntimeExceptionKind kind, UINT resID = 0, LPCWSTR szArg1 = NULL, LPCWSTR szArg2 = NULL,
                       LPCWSTR szArg3 = NULL, LPCWSTR szArg4 = NULL, LPCWSTR szArg5 = NULL, LPCWSTR szArg6 = NULL);

    EEMessageException(HRESULT hr);

    EEMessageException(HRESULT hr, UINT resID, LPCWSTR szArg1 = NULL, LPCWSTR szArg2 = NULL, LPCWSTR szArg3 = NULL,
                       LPCWSTR szArg4 = NULL, LPCWSTR szArg5 = NULL, LPCWSTR szArg6 = NULL);

    EEMessageException(RuntimeExceptionKind kind, HRESULT hr, UINT resID, LPCWSTR szArg1 = NULL, LPCWSTR szArg2 = NULL,
                       LPCWSTR szArg3 = NULL, LPCWSTR szArg4 = NULL, LPCWSTR szArg5 = NULL, LPCWSTR szArg6 = NULL);

    // Virtual overrides
    HRESULT GetHR();

    BOOL GetThrowableMessage(SString &result);

    UINT GetResID(void) { LIMITED_METHOD_CONTRACT; return m_resID; }

    static BOOL IsEEMessageException(Exception *pException)
    {
        return (*(PVOID*)pException == GetEEMessageExceptionVPtr());
    }

 protected:

    virtual Exception *CloneHelper()
    {
        WRAPPER_NO_CONTRACT;
        return new EEMessageException(
                m_kind, m_hr, m_resID, m_arg1, m_arg2, m_arg3, m_arg4, m_arg5, m_arg6);
    }


 private:

    static PVOID GetEEMessageExceptionVPtr()
    {
        CONTRACT (PVOID)
        {
            WRAPPER(THROWS);
            WRAPPER(GC_TRIGGERS);
            MODE_ANY;
            POSTCONDITION(CheckPointer(RETVAL));
        }
        CONTRACT_END;

        EEMessageException boilerplate(E_FAIL);
        RETURN (PVOID&)boilerplate;
    }

    BOOL GetResourceMessage(UINT iResourceID, SString &result);

#ifdef _DEBUG
    EEMessageException()
    {
        // Used only for DebugIsEECxxExceptionPointer to get the vtable pointer.
        // We need a variant which does not allocate memory.
    }
#endif // _DEBUG
};

// ---------------------------------------------------------------------------
// EEResourceException is an EE exception subclass composed of a type and
// an message using a managed exception resource.
// ---------------------------------------------------------------------------

class EEResourceException : public EEException
{
    friend bool DebugIsEECxxExceptionPointer(void* pv);

 private:
    InlineSString<32>        m_resourceName;

 public:
    EEResourceException(RuntimeExceptionKind kind, const SString &resourceName);

    // Unmanaged message text containing only the resource name (GC safe)
    void GetMessage(SString &result);

    // Throwable message containig the resource contents (not GC safe)
    BOOL GetThrowableMessage(SString &result);

 protected:

    virtual Exception *CloneHelper()
    {
        WRAPPER_NO_CONTRACT;
        return new EEResourceException(m_kind, m_resourceName);
    }

private:
#ifdef _DEBUG
    EEResourceException()
    {
        // Used only for DebugIsEECxxExceptionPointer to get the vtable pointer.
        // We need a variant which does not allocate memory.
    }
#endif // _DEBUG
};

#ifdef FEATURE_COMINTEROP
// ---------------------------------------------------------------------------
// EECOMException is an EE exception subclass composed of COM-generated data.
// Note that you must ensure that the COM data was not derived from a wrapper
// on a managed Exception object.  (If so, you must compose the exception from
// the managed object itself.)
// ---------------------------------------------------------------------------

struct ExceptionData
{
    HRESULT hr;
    BSTR    bstrDescription;
    BSTR    bstrSource;
    BSTR    bstrHelpFile;
    DWORD   dwHelpContext;
    GUID    guid;
};

class EECOMException : public EEException
{
    friend bool DebugIsEECxxExceptionPointer(void* pv);

 private:
    ExceptionData m_ED;

 public:

    EECOMException(EXCEPINFO *pExcepInfo);
    EECOMException(ExceptionData *pED);
    EECOMException(
        HRESULT hr,
        IErrorInfo *pErrInfo
        COMMA_INDEBUG(BOOL bCheckInProcCCWTearOff = TRUE));
    ~EECOMException();

    // Virtual overrides
    HRESULT GetHR();

    BOOL GetThrowableMessage(SString &result);
    OBJECTREF CreateThrowable();

 protected:

    virtual Exception *CloneHelper()
    {
        WRAPPER_NO_CONTRACT;
        return new EECOMException(&m_ED);
    }

private:
#ifdef _DEBUG
    EECOMException()
    {
        // Used only for DebugIsEECxxExceptionPointer to get the vtable pointer.
        // We need a variant which does not allocate memory.
        ZeroMemory(&m_ED, sizeof(m_ED));
    }
#endif // _DEBUG
};
#endif // FEATURE_COMINTEROP

// ---------------------------------------------------------------------------
// EEFieldException is an EE exception subclass composed of a field
// ---------------------------------------------------------------------------
class EEFieldException : public EEException
{
    friend bool DebugIsEECxxExceptionPointer(void* pv);

 private:
    FieldDesc   *m_pFD;
    MethodDesc  *m_pAccessingMD;
    SString      m_additionalContext;
    UINT         m_messageID;

 public:
    EEFieldException(FieldDesc *pField);
    EEFieldException(FieldDesc *pField, MethodDesc *pAccessingMD, const SString &additionalContext, UINT messageID);

    BOOL GetThrowableMessage(SString &result);
    virtual BOOL IsDomainBound() {return TRUE;};
protected:

    virtual Exception *CloneHelper()
    {
        WRAPPER_NO_CONTRACT;
        return new EEFieldException(m_pFD, m_pAccessingMD, m_additionalContext, m_messageID);
    }

private:
#ifdef _DEBUG
    EEFieldException()
    {
        // Used only for DebugIsEECxxExceptionPointer to get the vtable pointer.
        // We need a variant which does not allocate memory.
    }
#endif // _DEBUG
};

// ---------------------------------------------------------------------------
// EEMethodException is an EE exception subclass composed of a field
// ---------------------------------------------------------------------------

class EEMethodException : public EEException
{
    friend bool DebugIsEECxxExceptionPointer(void* pv);

 private:
    MethodDesc *m_pMD;
    MethodDesc *m_pAccessingMD;
    SString     m_additionalContext;
    UINT        m_messageID;

 public:
    EEMethodException(MethodDesc *pMethod);
    EEMethodException(MethodDesc *pMethod, MethodDesc *pAccessingMD, const SString &additionalContext, UINT messageID);

    BOOL GetThrowableMessage(SString &result);
    virtual BOOL IsDomainBound() {return TRUE;};
 protected:

    virtual Exception *CloneHelper()
    {
        WRAPPER_NO_CONTRACT;
        return new EEMethodException(m_pMD, m_pAccessingMD, m_additionalContext, m_messageID);
    }

private:
#ifdef _DEBUG
    EEMethodException()
    {
        // Used only for DebugIsEECxxExceptionPointer to get the vtable pointer.
        // We need a variant which does not allocate memory.
    }
#endif // _DEBUG
};

// ---------------------------------------------------------------------------
// EETypeAccessException is an EE exception subclass composed of a type being
// illegally accessed and the method doing the access
// ---------------------------------------------------------------------------

class EETypeAccessException : public EEException
{
    friend bool DebugIsEECxxExceptionPointer(void* pv);

 private:
    MethodTable *m_pMT;
    MethodDesc  *m_pAccessingMD;
    SString      m_additionalContext;
    UINT         m_messageID;

 public:
    EETypeAccessException(MethodTable *pMT);
    EETypeAccessException(MethodTable *pMT, MethodDesc *pAccessingMD, const SString &additionalContext, UINT messageID);

    BOOL GetThrowableMessage(SString &result);
    virtual BOOL IsDomainBound() {return TRUE;};
 protected:

    virtual Exception *CloneHelper()
    {
        WRAPPER_NO_CONTRACT;
        return new EETypeAccessException(m_pMT, m_pAccessingMD, m_additionalContext, m_messageID);
    }

private:
#ifdef _DEBUG
    EETypeAccessException()
    {
        // Used only for DebugIsEECxxExceptionPointer to get the vtable pointer.
        // We need a variant which does not allocate memory.
    }
#endif // _DEBUG
};

// ---------------------------------------------------------------------------
// EEArgumentException is an EE exception subclass representing a bad argument
// exception
// ---------------------------------------------------------------------------

class EEArgumentException : public EEException
{
    friend bool DebugIsEECxxExceptionPointer(void* pv);

 private:
    InlineSString<32>        m_argumentName;
    InlineSString<32>        m_resourceName;

 public:
    EEArgumentException(RuntimeExceptionKind reKind, LPCWSTR pArgName,
                        LPCWSTR wszResourceName);

    // @todo: GetMessage

    OBJECTREF CreateThrowable();

 protected:

    virtual Exception *CloneHelper()
    {
        WRAPPER_NO_CONTRACT;
        return new EEArgumentException(m_kind, m_argumentName, m_resourceName);
    }

private:
#ifdef _DEBUG
    EEArgumentException()
    {
        // Used only for DebugIsEECxxExceptionPointer to get the vtable pointer.
        // We need a variant which does not allocate memory.
    }
#endif // _DEBUG
};

// ---------------------------------------------------------------------------
// EETypeLoadException is an EE exception subclass representing a type loading
// error
// ---------------------------------------------------------------------------

class EETypeLoadException : public EEException
{
    friend bool DebugIsEECxxExceptionPointer(void* pv);

  private:
    InlineSString<64>   m_fullName;
    SString             m_pAssemblyName;
    SString             m_pMessageArg;
    UINT                m_resIDWhy;

 public:
    EETypeLoadException(LPCUTF8 pszNameSpace, LPCUTF8 pTypeName,
                        LPCWSTR pAssemblyName, LPCUTF8 pMessageArg, UINT resIDWhy);

    EETypeLoadException(LPCWSTR pFullTypeName,
                        LPCWSTR pAssemblyName, LPCUTF8 pMessageArg, UINT resIDWhy);

    // virtual overrides
    void GetMessage(SString &result);
    OBJECTREF CreateThrowable();

 protected:

    virtual Exception *CloneHelper()
    {
        WRAPPER_NO_CONTRACT;
        return new EETypeLoadException(m_fullName, m_pAssemblyName, m_pMessageArg, m_resIDWhy);
        }

 private:
    EETypeLoadException(const InlineSString<64> &fullName, LPCWSTR pAssemblyName,
                        const SString &pMessageArg, UINT resIDWhy)
       : EEException(kTypeLoadException),
         m_fullName(fullName),
         m_pAssemblyName(pAssemblyName),
         m_pMessageArg(pMessageArg),
         m_resIDWhy(resIDWhy)
    {
        WRAPPER_NO_CONTRACT;
    }


#ifdef _DEBUG
    EETypeLoadException()
    {
        // Used only for DebugIsEECxxExceptionPointer to get the vtable pointer.
        // We need a variant which does not allocate memory.
    }
#endif // _DEBUG
};

// ---------------------------------------------------------------------------
// EEFileLoadException is an EE exception subclass representing a file loading
// error
// ---------------------------------------------------------------------------

class EEFileLoadException : public EEException
{
    friend bool DebugIsEECxxExceptionPointer(void* pv);

  private:
    SString m_name;
    HRESULT m_hr;

  public:

    EEFileLoadException(const SString &name, HRESULT hr, Exception *pInnerException = NULL);
    ~EEFileLoadException();

    // virtual overrides
    HRESULT GetHR()
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return m_hr;
    }
    void GetMessage(SString &result);
    void GetName(SString &result);
    OBJECTREF CreateThrowable();

    static RuntimeExceptionKind GetFileLoadKind(HRESULT hr);
    static void DECLSPEC_NORETURN Throw(AssemblySpec *pSpec, HRESULT hr, Exception *pInnerException = NULL);
    static void DECLSPEC_NORETURN Throw(PEAssembly *pPEAssembly, HRESULT hr, Exception *pInnerException = NULL);
    static void DECLSPEC_NORETURN Throw(LPCWSTR path, HRESULT hr, Exception *pInnerException = NULL);
    static void DECLSPEC_NORETURN Throw(PEAssembly *parent, const void *memory, COUNT_T size, HRESULT hr, Exception *pInnerException = NULL);
    static BOOL CheckType(Exception* ex); // typeof(EEFileLoadException)

 protected:
    virtual Exception *CloneHelper()
    {
        WRAPPER_NO_CONTRACT;
        return new EEFileLoadException(m_name, m_hr);
    }

 private:
#ifdef _DEBUG
    EEFileLoadException()
    {
        // Used only for DebugIsEECxxExceptionPointer to get the vtable pointer.
        // We need a variant which does not allocate memory.
    }
#endif // _DEBUG

    void SetFileName(const SString &fileName, BOOL removePath);
};

// -------------------------------------------------------------------------------------------------------
// Throw/catch macros.  These are derived from the generic EXCEPTION macros,
// but add extra functionality for cleaning up thread state on catches
//
// Usage:
// EX_TRY
// {
//      EX_THROW(EEMessageException, (kind, L"Failure message"));
// }
// EX_CATCH
// {
//      EX_RETHROW()
// }
// EX_END_CATCH(RethrowTerminalExceptions)
// --------------------------------------------------------------------------------------------------------

// In DAC builds, we don't want to override the normal utilcode exception handling.
// We're not actually running in the CLR, but we may need access to some CLR-exception
// related data structures elsewhere in this header file in order to analyze CLR
// exceptions that occurred in the target.
#if !defined(DACCESS_COMPILE)

#define GET_THROWABLE() CLRException::GetThrowableFromException(GET_EXCEPTION())

#undef EX_TRY
#define EX_TRY                                                                                     \
    EX_TRY_CUSTOM(CLRException::HandlerState, (::GetThreadNULLOk()), CLRLastThrownObjectException)

#undef EX_TRY_CPP_ONLY
#define EX_TRY_CPP_ONLY                                                                             \
    EX_TRY_CUSTOM_CPP_ONLY(CLRException::HandlerState, (::GetThreadNULLOk()), CLRLastThrownObjectException)

// Faster version with thread, skipping GetThread call
#define EX_TRY_THREAD(pThread)                                                           \
    EX_TRY_CUSTOM(CLRException::HandlerState, (pThread, CLRException::HandlerState::ThreadIsNotNull), CLRLastThrownObjectException)

#if defined(_DEBUG)
  // Redefine GET_EXCEPTION to validate CLRLastThrownObjectException as much as possible.
  #undef GET_EXCEPTION
  #define GET_EXCEPTION() (__pException == NULL ? __defaultException.Validate() : __pException.GetValue())
#endif // _DEBUG

LONG CLRNoCatchHandler(EXCEPTION_POINTERS* pExceptionInfo, PVOID pv);

// Re-define the macro to add automatic restoration of the guard page to PAL_EXCEPT and PAL_EXCEPT_FILTER and
// friends. Note: RestoreGuardPage will only do work if the guard page is not present.
#undef PAL_SEH_RESTORE_GUARD_PAGE
#define PAL_SEH_RESTORE_GUARD_PAGE                                                  \
    if (__exCode == STATUS_STACK_OVERFLOW)                                          \
    {                                                                               \
        Thread *__pThread = GetThreadNULLOk();                                            \
        if (__pThread != NULL)                                                      \
        {                                                                           \
            __pThread->RestoreGuardPage();                                          \
        }                                                                           \
    }

#undef EX_TRY_NOCATCH
#define EX_TRY_NOCATCH(ParamType, paramDef, paramRef)            \
    PAL_TRY(ParamType, __EXparam, paramRef)                      \
    {                                                            \
        CLRException::HandlerState __state(::GetThreadNULLOk()); \
        PAL_TRY(ParamType, paramDef, __EXparam)                  \
        {

#undef EX_END_NOCATCH
#define EX_END_NOCATCH                  \
            ;                           \
        }                               \
        PAL_FINALLY                     \
        {                               \
            __state.CleanupTry();       \
        }                               \
        PAL_ENDTRY                      \
    }                                   \
    PAL_EXCEPT_FILTER(EntryPointFilter) \
    {                                   \
    }                                   \
    PAL_ENDTRY

//
// We need a way to identify an exception in managed code that is rethrown from a new exception in managed code
// when we get into our managed EH logic. Currently, we do that by checking the GC mode. If a thread has preemptive
// GC enabled, but the IP of the exception is in mangaed code, then it must be a rethrow from unmanaged code
// (including CLR code.) Therefore, we toggle the GC mode before the rethrow to indicate that. Note: we don't do
// this if we've caught one of our internal C++ Exception objects: by definition, those don't come from managed
// code, and this allows us to continue to use EX_RETHROW in no-trigger regions.
//
#undef EX_RETHROW
#define EX_RETHROW                                                                      \
        do                                                                              \
        {                                                                               \
            /* don't embed file names in retail to save space and avoid IP */           \
            /* a findstr /n will allow you to locate it in a pinch */                   \
            STRESS_LOG1(LF_EH, LL_INFO100,                                              \
                "EX_RETHROW " INDEBUG(__FILE__) " line %d\n", __LINE__);                \
            __pException.SuppressRelease();                                             \
            if ((!__state.DidCatchCxx()) && (GetThreadNULLOk() != NULL))                      \
            {                                                                           \
                if (GetThread()->PreemptiveGCDisabled())                                \
                {                                                                       \
                    LOG((LF_EH, LL_INFO10, "EX_RETHROW: going preemptive\n"));          \
                    GetThread()->EnablePreemptiveGC();                                  \
                }                                                                       \
            }                                                                           \
            PAL_CPP_RETHROW;                                                            \
        } while (0)

//
// Note: we only restore the guard page if we did _not_ catch a C++ exception, since a SO exception is a SEH
// exception.
//
// We also need to restore the SO tolerance state, including restoring the cookie for the current stack guard.
//
// For VM code EX_CATCH calls CLREXception::HandleState::SetupCatch().
// When Stack guards are disabled we will tear down the process in
// CLREXception::HandleState::SetupCatch() if there is a StackOverflow.
// So we should not reach EX_ENDTRY when there is StackOverflow.
// This change cannot be done in ex.h as for all other code
// CLREXception::HandleState::SetupCatch() is not called rather
// EXception::HandleState::SetupCatch() is called which is a nop.
//
#undef EX_ENDTRY
#define EX_ENDTRY                                           \
    PAL_CPP_ENDTRY


// CLRException::GetErrorInfo below invokes GetComIPFromObjectRef
// that invokes ObjHeader::GetSyncBlock which has the INJECT_FAULT contract.
//
// This EX_CATCH_HRESULT implementation can be used in functions
// that have FORBID_FAULT contracts.
//
// However, failure due to OOM (or any other potential exception) in GetErrorInfo
// implies that we couldnt get the interface pointer from the objectRef and would be
// returned NULL.
//
// Thus, the scoped use of FAULT_NOT_FATAL macro.
#undef EX_CATCH_HRESULT
#define EX_CATCH_HRESULT(_hr)                                                   \
    EX_CATCH                                                                    \
    {                                                                           \
        (_hr) = GET_EXCEPTION()->GetHR();                                       \
        {                                                                       \
            FAULT_NOT_FATAL();                                                  \
            HRESULT hrErrorInfo = GET_EXCEPTION()->SetErrorInfo();              \
            if (FAILED(hrErrorInfo))                                            \
            {                                                                   \
                (_hr) = hrErrorInfo;                                            \
            }                                                                   \
        }                                                                       \
        _ASSERTE(FAILED(_hr));                                                  \
    }                                                                           \
    EX_END_CATCH(SwallowAllExceptions)

#endif // !DACCESS_COMPILE

// When collecting dumps, we need to ignore errors unless the user cancels.
#define EX_CATCH_RETHROW_ONLY_COR_E_OPERATIONCANCELLED                          \
    EX_CATCH                                                                    \
    {                                                                           \
    /* Swallow the exception and keep going unless COR_E_OPERATIONCANCELED */   \
    /* was thrown. Used generating dumps, where rethrow will cancel dump. */    \
    }                                                                           \
    EX_END_CATCH(RethrowCancelExceptions)

// Only use this version to wrap single source lines, or it makes debugging painful.
#define CATCH_ALL_EXCEPT_RETHROW_COR_E_OPERATIONCANCELLED(sourceCode)           \
    EX_TRY                                                                      \
    {                                                                           \
        sourceCode                                                              \
    }                                                                           \
    EX_CATCH_RETHROW_ONLY_COR_E_OPERATIONCANCELLED


//==============================================================================
// High-level macros for common uses of EX_TRY. Try using these rather
// than the raw EX_TRY constructs.
//==============================================================================

//===================================================================================
// Macro for defining external entrypoints such as COM interop boundaries.
// The boundary will catch all exceptions (including terminals) and convert
// them into HR/IErrorInfo pairs as appropriate.
//
// Usage:
//
//   HRESULT hr;                     ;; BEGIN will initialize HR
//   BEGIN_EXTERNAL_ENTRYPOINT(&hr)
//   <do managed stuff>              ;; this part will execute in cooperative GC mode
//   END_EXTERNAL_ENTRYPOINT
//   return hr;
//
// Comments:
//   The BEGIN macro will setup a Thread if necessary. It should only be called
//   in preemptive mode.  If you are calling it from cooperative mode, this implies
//   we are executing "external" code in cooperative mode.
//
//   Only use this macro for actual boundaries between CLR and
//   outside unmanaged code. If you want to connect internal pieces
//   of CLR code, use EX_TRY instead.
//===================================================================================
#define BEGIN_EXTERNAL_ENTRYPOINT(phresult)                         \
    {                                                               \
        HRESULT *__phr = (phresult);                                \
        *__phr = S_OK;                                              \
        _ASSERTE(GetThreadNULLOk() == NULL ||                             \
                    !GetThread()->PreemptiveGCDisabled());          \
        MAKE_CURRENT_THREAD_AVAILABLE_EX(GetThreadNULLOk());        \
        if (CURRENT_THREAD == NULL)                                 \
        {                                                           \
            CURRENT_THREAD = SetupThreadNoThrow(__phr);             \
        }                                                           \
        if (CURRENT_THREAD != NULL)                                 \
        {                                                           \
            EX_TRY_THREAD(CURRENT_THREAD);                          \
            {                                                       \

#define END_EXTERNAL_ENTRYPOINT                                     \
            }                                                       \
            EX_CATCH_HRESULT(*__phr);                               \
        }                                                           \
    }                                                               \

//==============================================================================

// ---------------------------------------------------------------------------
// Inline implementations. Pay no attention to that man behind the curtain.
// ---------------------------------------------------------------------------

inline CLRException::CLRException()
  : m_throwableHandle(NULL)
{
    LIMITED_METHOD_CONTRACT;
}

inline void CLRException::SetThrowableHandle(OBJECTHANDLE throwable)
{
    STRESS_LOG1(LF_EH, LL_INFO100, "in CLRException::SetThrowableHandle: obj = %x\n", throwable);
    m_throwableHandle = throwable;
}

inline EEException::EEException(RuntimeExceptionKind kind)
  : m_kind(kind)
{
    LIMITED_METHOD_CONTRACT;
}

inline EEException::EEException(HRESULT hr)
  : m_kind(GetKindFromHR(hr))
{
    LIMITED_METHOD_CONTRACT;
}

inline EEMessageException::EEMessageException(HRESULT hr)
  : EEException(GetKindFromHR(hr)),
    m_hr(hr),
    m_resID(0)
{
    WRAPPER_NO_CONTRACT;

    m_arg1.Printf("%.8x", hr);
}

//-----------------------------------------------------------------------------
// Constructor with lots of defaults (to 0 / null)
//   kind       -- "clr kind" of the exception
//   resid      -- resource id for message
//   strings    -- substitution text for message
inline EEMessageException::EEMessageException(RuntimeExceptionKind kind, UINT resID, LPCWSTR szArg1, LPCWSTR szArg2,
                                              LPCWSTR szArg3, LPCWSTR szArg4, LPCWSTR szArg5, LPCWSTR szArg6)
  : EEException(kind),
    m_hr(EEException::GetHRFromKind(kind)),
    m_resID(resID),
    m_arg1(szArg1),
    m_arg2(szArg2),
    m_arg3(szArg3),
    m_arg4(szArg4),
    m_arg5(szArg5),
    m_arg6(szArg6)
{
    WRAPPER_NO_CONTRACT;
}

//-----------------------------------------------------------------------------
// Constructor with lots of defaults (to 0 / null)
//   hr         -- hresult that lead to this exception
//   resid      -- resource id for message
//   strings    -- substitution text for message
inline EEMessageException::EEMessageException(HRESULT hr, UINT resID, LPCWSTR szArg1, LPCWSTR szArg2, LPCWSTR szArg3,
                                              LPCWSTR szArg4, LPCWSTR szArg5, LPCWSTR szArg6)
  : EEException(GetKindFromHR(hr)),
    m_hr(hr),
    m_resID(resID),
    m_arg1(szArg1),
    m_arg2(szArg2),
    m_arg3(szArg3),
    m_arg4(szArg4),
    m_arg5(szArg5),
    m_arg6(szArg6)
{
}

//-----------------------------------------------------------------------------
// Constructor with no defaults
//   kind       -- "clr kind" of the exception
//   hr         -- hresult that lead to this exception
//   resid      -- resource id for message
//   strings    -- substitution text for message
inline EEMessageException::EEMessageException(RuntimeExceptionKind kind, HRESULT hr, UINT resID, LPCWSTR szArg1,
                                              LPCWSTR szArg2, LPCWSTR szArg3, LPCWSTR szArg4, LPCWSTR szArg5,
                                              LPCWSTR szArg6)
  : EEException(kind),
    m_hr(hr),
    m_resID(resID),
    m_arg1(szArg1),
    m_arg2(szArg2),
    m_arg3(szArg3),
    m_arg4(szArg4),
    m_arg5(szArg5),
    m_arg6(szArg6)
{
    WRAPPER_NO_CONTRACT;
}


inline EEResourceException::EEResourceException(RuntimeExceptionKind kind, const SString &resourceName)
  : EEException(kind),
    m_resourceName(resourceName)
{
    WRAPPER_NO_CONTRACT;
}


inline EEFieldException::EEFieldException(FieldDesc *pField)
  : EEException(kFieldAccessException),
    m_pFD(pField),
    m_pAccessingMD(NULL),
    m_messageID(0)
{
    WRAPPER_NO_CONTRACT;
}

inline EEFieldException::EEFieldException(FieldDesc *pField, MethodDesc *pAccessingMD, const SString &additionalContext, UINT messageID)
    : EEException(kFieldAccessException),
      m_pFD(pField),
      m_pAccessingMD(pAccessingMD),
      m_additionalContext(additionalContext),
      m_messageID(messageID)
{
}

inline EEMethodException::EEMethodException(MethodDesc *pMethod)
  : EEException(kMethodAccessException),
    m_pMD(pMethod),
    m_pAccessingMD(NULL),
    m_messageID(0)
{
    WRAPPER_NO_CONTRACT;
}

inline EEMethodException::EEMethodException(MethodDesc *pMethod, MethodDesc *pAccessingMD, const SString &additionalContext, UINT messageID)
    : EEException(kMethodAccessException),
      m_pMD(pMethod),
      m_pAccessingMD(pAccessingMD),
      m_additionalContext(additionalContext),
      m_messageID(messageID)
{
}

inline EETypeAccessException::EETypeAccessException(MethodTable *pMT)
    : EEException(kTypeAccessException),
      m_pMT(pMT),
      m_pAccessingMD(NULL),
      m_messageID(0)
{
}

inline EETypeAccessException::EETypeAccessException(MethodTable *pMT, MethodDesc *pAccessingMD, const SString &additionalContext, UINT messageID)
    : EEException(kTypeAccessException),
      m_pMT(pMT),
      m_pAccessingMD(pAccessingMD),
      m_additionalContext(additionalContext),
      m_messageID(messageID)
{
}

inline EEArgumentException::EEArgumentException(RuntimeExceptionKind reKind, LPCWSTR pArgName,
                    LPCWSTR wszResourceName)
  : EEException(reKind),
    m_argumentName(pArgName),
    m_resourceName(wszResourceName)
{
    WRAPPER_NO_CONTRACT;
}


class ObjrefException : public CLRException
{
    friend bool DebugIsEECxxExceptionPointer(void* pv);

 public:

    ObjrefException();
    ObjrefException(OBJECTREF throwable);

 private:
    static const int c_type = 0x4F522020;   // 'OR '

 public:
    // Dynamic type query for catchers
    static int GetType() {LIMITED_METHOD_CONTRACT;  return c_type; }
    virtual int GetInstanceType() { LIMITED_METHOD_CONTRACT; return c_type; }
    BOOL IsType(int type) { WRAPPER_NO_CONTRACT; return type == c_type || CLRException::IsType(type); }

protected:
    virtual Exception *CloneHelper()
    {
        WRAPPER_NO_CONTRACT;
        return new ObjrefException();
    }

    virtual Exception *DomainBoundCloneHelper();
};


class CLRLastThrownObjectException : public CLRException
{
    friend bool DebugIsEECxxExceptionPointer(void* pv);

 public:
    CLRLastThrownObjectException();

 private:
    static const int c_type = 0x4C544F20;   // 'LTO '

 public:
    // Dynamic type query for catchers
    static int GetType() {LIMITED_METHOD_CONTRACT;  return c_type; }
    virtual int GetInstanceType() { LIMITED_METHOD_CONTRACT; return c_type; }
    BOOL IsType(int type) { WRAPPER_NO_CONTRACT; return type == c_type || CLRException::IsType(type); }

    #if defined(_DEBUG)
      CLRLastThrownObjectException* Validate();
    #endif // _DEBUG

 protected:
    virtual Exception *CloneHelper();

    virtual Exception *DomainBoundCloneHelper();

    virtual OBJECTREF CreateThrowable();
};

// Returns true if the HRESULT maps to the RuntimeExceptionKind (hr => kind).
bool IsHRESULTForExceptionKind(HRESULT hr, RuntimeExceptionKind kind);

#endif // _CLREX_H_

