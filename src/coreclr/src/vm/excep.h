// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// EXCEP.H
//

//


#ifndef __excep_h__
#define __excep_h__

#include "exstatecommon.h"
#include "exceptmacros.h"
#include "corerror.h"  // HResults for the COM+ Runtime
#include "corexcep.h"  // Exception codes for the COM+ Runtime

class Thread;

#include "../dlls/mscorrc/resource.h"

#include <excepcpu.h>
#include "interoputil.h"

BOOL IsExceptionFromManagedCode(const EXCEPTION_RECORD * pExceptionRecord);
bool IsIPInMarkedJitHelper(UINT_PTR uControlPc);

#if defined(_TARGET_AMD64_) && defined(FEATURE_HIJACK)

// General purpose functions for use on an IP in jitted code. 
bool IsIPInProlog(EECodeInfo *pCodeInfo);
bool IsIPInEpilog(PTR_CONTEXT pContextToCheck, EECodeInfo *pCodeInfo, BOOL *pSafeToInjectThreadAbort);

#endif // defined(_TARGET_AMD64_) && defined(FEATURE_HIJACK)

void RaiseFailFastExceptionOnWin7(PEXCEPTION_RECORD pExceptionRecord, PT_CONTEXT pContext);

// Check if the Win32 Error code is an IO error.
BOOL IsWin32IOError(SCODE scode);

//******************************************************************************
//
//  SwallowUnhandledExceptions
//
//   Consult the EE policy and the app config to determine if the runtime should "swallow" unhandled exceptions.
//   Swallow if: the EEPolicy->UnhandledExceptionPolicy is "eHostDeterminedPolicy"
//           or: the app config value LegacyUnhandledExceptionPolicy() is set.
//
//  Parameters: 
//    none
//
//  Return value:
//    true - the runtime should "swallow" unhandled exceptions
//
inline bool SwallowUnhandledExceptions()
{
    return (eHostDeterminedPolicy == GetEEPolicy()->GetUnhandledExceptionPolicy()) ||
           g_pConfig->LegacyUnhandledExceptionPolicy() ||
           GetCompatibilityFlag(compatSwallowUnhandledExceptions);
}

// Enums
// return values of LookForHandler
enum LFH {
    LFH_NOT_FOUND = 0,
    LFH_FOUND = 1,
};

#include "runtimeexceptionkind.h"

class IJitManager;

//
// ThrowCallbackType is used to pass information to between various functions and the callbacks that they call
// during a managed stack walk.
//
struct ThrowCallbackType
{
    MethodDesc * pFunc;     // the function containing a filter that returned catch indication
    int     dHandler;       // the index of the handler whose filter returned catch indication
    BOOL    bIsUnwind;      // are we currently unwinding an exception
    BOOL    bUnwindStack;   // reset the stack before calling the handler? (Stack overflow only)
    BOOL    bAllowAllocMem; // are we allowed to allocate memory?
    BOOL    bDontCatch;     // can we catch this exception?
    BYTE    *pStack;
    Frame * pTopFrame;
    Frame * pBottomFrame;
    MethodDesc * pProfilerNotify;   // Context for profiler callbacks -- see COMPlusFrameHandler().
    BOOL    bReplaceStack;  // Used to pass info to SaveStackTrace call
    BOOL    bSkipLastElement;// Used to pass info to SaveStackTrace call
    HANDLE hCallerToken;
    HANDLE hImpersonationToken;
    BOOL bImpersonationTokenSet;
#ifdef _DEBUG
    void * pCurrentExceptionRecord;
    void * pPrevExceptionRecord;
#endif

    // Is the current exception a longjmp?
    CORRUPTING_EXCEPTIONS_ONLY(BOOL     m_fIsLongJump;)
    void Init()
    {
        LIMITED_METHOD_CONTRACT;

        pFunc = NULL;
        dHandler = 0;
        bIsUnwind = FALSE;
        bUnwindStack = FALSE;
        bAllowAllocMem = TRUE;
        bDontCatch = FALSE;
        pStack = NULL;
        pTopFrame = (Frame *)-1;
        pBottomFrame = (Frame *)-1;
        pProfilerNotify = NULL;
        bReplaceStack = FALSE;
        bSkipLastElement = FALSE;
        hCallerToken = NULL;
        hImpersonationToken = NULL;
        bImpersonationTokenSet = FALSE;
        
#ifdef _DEBUG
        pCurrentExceptionRecord = 0;
        pPrevExceptionRecord = 0;
#endif
        // By default, the current exception is not a longjmp
        CORRUPTING_EXCEPTIONS_ONLY(m_fIsLongJump = FALSE;)
    }
};



struct EE_ILEXCEPTION_CLAUSE;

void InitializeExceptionHandling();
void CLRAddVectoredHandlers(void);
void CLRRemoveVectoredHandlers(void);
void TerminateExceptionHandling();

// Prototypes
EXTERN_C VOID STDCALL ResetCurrentContext();
#if !defined(WIN64EXCEPTIONS)
#ifdef _DEBUG
void CheckStackBarrier(EXCEPTION_REGISTRATION_RECORD *exRecord);
#endif
EXCEPTION_REGISTRATION_RECORD *FindNestedEstablisherFrame(EXCEPTION_REGISTRATION_RECORD *pEstablisherFrame);
LFH LookForHandler(const EXCEPTION_POINTERS *pExceptionPointers, Thread *pThread, ThrowCallbackType *tct);
StackWalkAction COMPlusThrowCallback (CrawlFrame *pCf, ThrowCallbackType *pData);
void UnwindFrames(Thread *pThread, ThrowCallbackType *tct);
#endif // !defined(WIN64EXCEPTIONS)

void UnwindFrameChain(Thread *pThread, LPVOID pvLimitSP);
DWORD MapWin32FaultToCOMPlusException(EXCEPTION_RECORD *pExceptionRecord);
DWORD ComputeEnclosingHandlerNestingLevel(IJitManager *pIJM, const METHODTOKEN& mdTok, SIZE_T offsNat);
BOOL IsException(MethodTable *pMT);
BOOL IsExceptionOfType(RuntimeExceptionKind reKind, OBJECTREF *pThrowable);
BOOL IsExceptionOfType(RuntimeExceptionKind reKind, Exception *pException);
BOOL IsAsyncThreadException(OBJECTREF *pThrowable);
BOOL IsUncatchable(OBJECTREF *pThrowable);
VOID FixupOnRethrow(Thread *pCurThread, EXCEPTION_POINTERS *pExceptionPointers);
BOOL UpdateCurrentThrowable(PEXCEPTION_RECORD pExceptionRecord);
BOOL IsStackOverflowException(Thread* pThread, EXCEPTION_RECORD* pExceptionRecord);
void WrapNonCompliantException(OBJECTREF *ppThrowable);
OBJECTREF PossiblyUnwrapThrowable(OBJECTREF throwable, Assembly *pAssembly);
BOOL ExceptionTypeOverridesStackTraceGetter(PTR_MethodTable pMT);

// Removes source file names/paths and line information from a stack trace.
void StripFileInfoFromStackTrace(SString &ssStackTrace); 

#ifdef _DEBUG
// C++ EH cracking material gleaned from the debugger:
// (DO NOT USE THIS KNOWLEDGE IN NON-DEBUG CODE!!!)
void *DebugGetCxxException(EXCEPTION_RECORD* pExceptionRecord);
#endif


#ifdef _DEBUG_IMPL
BOOL IsValidClause(EE_ILEXCEPTION_CLAUSE *EHClause);
BOOL IsCOMPlusExceptionHandlerInstalled();
#endif

BOOL InstallUnhandledExceptionFilter();
void UninstallUnhandledExceptionFilter();

#if defined(FEATURE_CORECLR) && !defined(FEATURE_PAL)
// Section naming is a strategy by itself. Ideally, we could have named the UEF section
// ".text$zzz" (lowercase after $ is important). What the linker does is look for the sections
// that has the same name before '$' sign. It combines them together but sorted in an alphabetical
// order. Thus, naming the UEF section ".text$zzz" would ensure that the UEF section is the last
// thing in the .text section. Reason for opting out of this approach was that BBT can move code
// within a section, no matter where it was located - and for this case, we need the UEF code
// at the right location to ensure that we can check the memory protection of its following
// section so that shouldnt affect UEF's memory protection. For details, read the comment in
// "CExecutionEngine::ClrVirtualProtect".
//
// Keeping UEF in its own section helps prevent code movement as BBT does not reorder
// sections. As per my understanding of the linker, ".text" section always comes first,
// followed by other "executable" sections (like our UEF section) and then ".data", etc.
// The order of user defined executable sections is typically defined by the linker
// in terms of which section it sees first. So, if there is another custom executable
// section that comes after UEF section, it can affect the UEF section and we will
// assert about it in "CExecutionEngine::ClrVirtualProtect".
#define CLR_UEF_SECTION_NAME ".CLR_UEF"
#endif // defined(FEATURE_CORECLR) && !defined(FEATURE_PAL)
LONG __stdcall COMUnhandledExceptionFilter(EXCEPTION_POINTERS *pExceptionInfo);


//////////////
// A list of places where we might have unhandled exceptions or other serious faults. These can be used as a mask in
// DbgJITDebuggerLaunchSetting to help control when we decide to ask the user about whether or not to launch a debugger.
//
enum UnhandledExceptionLocation
    {
    ProcessWideHandler    = 0x000001,
    ManagedThread         = 0x000002, // Does not terminate the application. CLR swallows the unhandled exception.
    ThreadPoolThread      = 0x000004, // ditto.
    FinalizerThread       = 0x000008, // ditto.
    FatalStackOverflow    = 0x000010,
    SystemNotification    = 0x000020, // CLR will swallow after the notification occurs
    FatalExecutionEngineException = 0x000040,
    ClassInitUnhandledException   = 0x000080, // Does not terminate the application. CLR transforms this into TypeInitializationException

    MaximumLocationValue  = 0x800000, // This is the maximum location value you're allowed to use. (Max 24 bits allowed.)

    // This is a mask of all the locations that the debugger will attach to by default.
    DefaultDebuggerAttach = ProcessWideHandler |
                            FatalStackOverflow |
                            FatalExecutionEngineException
};

struct ThreadBaseExceptionFilterParam
{
    UnhandledExceptionLocation location;
};

LONG ThreadBaseExceptionFilter(PEXCEPTION_POINTERS pExceptionInfo, PVOID pvParam);
LONG ThreadBaseExceptionSwallowingFilter(PEXCEPTION_POINTERS pExceptionInfo, PVOID pvParam);
LONG ThreadBaseExceptionAppDomainFilter(PEXCEPTION_POINTERS pExceptionInfo, PVOID pvParam);

// Filter for calls out from the 'vm' to native code, if there's a possibility of SEH exceptions
// in the native code.
struct CallOutFilterParam { BOOL OneShot; };
LONG CallOutFilter(PEXCEPTION_POINTERS pExceptionInfo, PVOID pv);


void DECLSPEC_NORETURN RaiseDeadLockException();

void STDMETHODCALLTYPE DefaultCatchHandler(PEXCEPTION_POINTERS pExceptionInfo,
                                           OBJECTREF *Throwable = NULL,
                                           BOOL useLastThrownObject = FALSE,
                                           BOOL isTerminating = FALSE,
                                           BOOL isThreadBaseFilter = FALSE,
                                           BOOL sendAppDomainEvents = TRUE);

void ReplaceExceptionContextRecord(T_CONTEXT *pTarget, T_CONTEXT *pSource);

// Localization helper function
void ResMgrGetString(LPCWSTR wszResourceName, STRINGREF * ppMessage);

// externs

//==========================================================================
// Various routines to throw COM+ objects.
//==========================================================================

//==========================================================================
// Throw an undecorated runtime exception with a specific string parameter
// that won't be localized.  If possible, try using
// COMPlusThrow(reKind, LPCWSTR wszResourceName) instead.
//==========================================================================

VOID DECLSPEC_NORETURN RealCOMPlusThrowNonLocalized(RuntimeExceptionKind reKind, LPCWSTR wszTag);

//==========================================================================
// Throw an object.
//==========================================================================

VOID DECLSPEC_NORETURN RealCOMPlusThrow(OBJECTREF throwable 
#ifdef FEATURE_CORRUPTING_EXCEPTIONS
                                        , CorruptionSeverity severity = NotCorrupting
#endif // FEATURE_CORRUPTING_EXCEPTIONS
                                        );

//==========================================================================
// Throw an undecorated runtime exception.
//==========================================================================

VOID DECLSPEC_NORETURN RealCOMPlusThrow(RuntimeExceptionKind reKind);

//==========================================================================
// Throw an undecorated runtime exception with a localized message.  Given
// a resource name, the ResourceManager will find the correct paired string
// in our .resources file.
//==========================================================================

VOID DECLSPEC_NORETURN RealCOMPlusThrow(RuntimeExceptionKind reKind, LPCWSTR wszResourceName, Exception * pInnerException = NULL);

//==========================================================================
// Throw a decorated runtime exception.
//==========================================================================

VOID DECLSPEC_NORETURN RealCOMPlusThrow(RuntimeExceptionKind  reKind, UINT resID, 
                                        LPCWSTR wszArg1 = NULL, LPCWSTR wszArg2 = NULL, LPCWSTR wszArg3 = NULL, 
                                        LPCWSTR wszArg4 = NULL, LPCWSTR wszArg5 = NULL, LPCWSTR wszArg6 = NULL);


//==========================================================================
// Throw a runtime exception based on an HResult. Note that for the version 
// of RealCOMPlusThrowHR that takes a resource ID, the HRESULT will be 
// passed as the first substitution string (%1).
//==========================================================================

enum tagGetErrorInfo
{
    kGetErrorInfo
};

VOID DECLSPEC_NORETURN RealCOMPlusThrowHR(HRESULT hr, IErrorInfo* pErrInfo, Exception * pInnerException = NULL);
VOID DECLSPEC_NORETURN RealCOMPlusThrowHR(HRESULT hr, tagGetErrorInfo);
VOID DECLSPEC_NORETURN RealCOMPlusThrowHR(HRESULT hr);
VOID DECLSPEC_NORETURN RealCOMPlusThrowHR(HRESULT hr, UINT resID, LPCWSTR wszArg1 = NULL, LPCWSTR wszArg2 = NULL, 
                                          LPCWSTR wszArg3 = NULL, LPCWSTR wszArg4 = NULL, LPCWSTR wszArg5 = NULL, 
                                          LPCWSTR wszArg6 = NULL);

#ifdef FEATURE_COMINTEROP

//==========================================================================
// Throw a runtime exception based on an HResult, check for error info
//==========================================================================

VOID DECLSPEC_NORETURN RealCOMPlusThrowHR(HRESULT hr, IUnknown *iface, REFIID riid);


//==========================================================================
// Throw a runtime exception based on an EXCEPINFO. This function will free
// the strings in the EXCEPINFO that is passed in.
//==========================================================================

VOID DECLSPEC_NORETURN RealCOMPlusThrowHR(EXCEPINFO *pExcepInfo);

#endif // FEATURE_COMINTEROP

//==========================================================================
// Throw a runtime exception based on the last Win32 error (GetLastError())
//==========================================================================

VOID DECLSPEC_NORETURN RealCOMPlusThrowWin32();
VOID DECLSPEC_NORETURN RealCOMPlusThrowWin32(HRESULT hr);


//==========================================================================
// Create an exception object
// Note that this may not succeed due to problems creating the exception
// object. On failure, it will set pInitException to the value of
// pInnerException, and will set pThrowable to the exception that got thrown
// while trying to create the TypeInitializationException object, which
// could be due to other type init issues, OOM, thread abort, etc.
// pInnerException (may be NULL) and pInitException and are IN params.
// pThrowable is an OUT param.
//==========================================================================
void CreateTypeInitializationExceptionObject(LPCWSTR pTypeThatFailed,
                                             OBJECTREF *pInnerException, 
                                             OBJECTREF *pInitException,
                                             OBJECTREF *pThrowable);

//==========================================================================
// Examine an exception object
//==========================================================================

ULONG GetExceptionMessage(OBJECTREF throwable,
                          __inout_ecount(bufferLength) LPWSTR buffer,
                          ULONG bufferLength);
void GetExceptionMessage(OBJECTREF throwable, SString &result);
STRINGREF GetExceptionMessage(OBJECTREF throwable);
HRESULT GetExceptionHResult(OBJECTREF throwable);
DWORD GetExceptionXCode(OBJECTREF throwable);

void ExceptionPreserveStackTrace(OBJECTREF throwable);


//==========================================================================
// Create an exception object for an HRESULT
//==========================================================================

void GetExceptionForHR(HRESULT hr, IErrorInfo* pErrInfo, bool fUseCOMException, OBJECTREF* pProtectedThrowable, IRestrictedErrorInfo *pResErrorInfo = NULL, BOOL bHasLanguageRestrictedErrorInfo = FALSE);
void GetExceptionForHR(HRESULT hr, IErrorInfo* pErrInfo, OBJECTREF* pProtectedThrowable);
void GetExceptionForHR(HRESULT hr, OBJECTREF* pProtectedThrowable);
HRESULT GetHRFromThrowable(OBJECTREF throwable);

#if FEATURE_COMINTEROP
IRestrictedErrorInfo* GetRestrictedErrorInfoFromErrorObject(OBJECTREF throwable);
#endif
//==========================================================================
// Throw an ArithmeticException
//==========================================================================

VOID DECLSPEC_NORETURN RealCOMPlusThrowArithmetic();

//==========================================================================
// Throw an ArgumentNullException
//==========================================================================

VOID DECLSPEC_NORETURN RealCOMPlusThrowArgumentNull(LPCWSTR argName, LPCWSTR wszResourceName);

VOID DECLSPEC_NORETURN RealCOMPlusThrowArgumentNull(LPCWSTR argName);

//==========================================================================
// Throw an ArgumentOutOfRangeException
//==========================================================================

VOID DECLSPEC_NORETURN RealCOMPlusThrowArgumentOutOfRange(LPCWSTR argName, LPCWSTR wszResourceName);

//==========================================================================
// Throw an ArgumentException
//==========================================================================
VOID DECLSPEC_NORETURN RealCOMPlusThrowArgumentException(LPCWSTR argName, LPCWSTR wszResourceName);

//==========================================================================
// Throw an InvalidCastException
//==========================================================================
VOID DECLSPEC_NORETURN RealCOMPlusThrowInvalidCastException(TypeHandle thCastFrom, TypeHandle thCastTo);

VOID DECLSPEC_NORETURN RealCOMPlusThrowInvalidCastException(OBJECTREF *pObj, TypeHandle thCastTo);


#include "eexcp.h"
#include "exinfo.h"

#ifdef _TARGET_X86_
struct FrameHandlerExRecord
{
    EXCEPTION_REGISTRATION_RECORD   m_ExReg;

    Frame *m_pEntryFrame;

    Frame *GetCurrFrame() 
    {
        LIMITED_METHOD_CONTRACT;
        return m_pEntryFrame;
    }
};

struct NestedHandlerExRecord : public FrameHandlerExRecord 
{
    ExInfo m_handlerInfo;
    BOOL   m_ActiveForUnwind;
    ExInfo *m_pCurrentExInfo;
    EXCEPTION_REGISTRATION_RECORD *m_pCurrentHandler;
    NestedHandlerExRecord() : m_handlerInfo() {LIMITED_METHOD_CONTRACT;}
    void Init(PEXCEPTION_ROUTINE pFrameHandler, Frame *pEntryFrame)
    {
        WRAPPER_NO_CONTRACT;

        m_ExReg.Next=NULL;
        m_ExReg.Handler=pFrameHandler;
        m_pEntryFrame=pEntryFrame;
        m_pCurrentExInfo = NULL;
        m_pCurrentHandler = NULL;
        m_handlerInfo.Init();
        m_ActiveForUnwind = FALSE;
    }
};

#endif // _TARGET_X86_

#if defined(ENABLE_CONTRACTS_IMPL)

// Never call this class directly: Call it through CANNOTTHROWCOMPLUSEXCEPTION.
class COMPlusCannotThrowExceptionHelper
{
public:
    DEBUG_NOINLINE COMPlusCannotThrowExceptionHelper(BOOL    fCond,
                                      const char *szFunction,
                                      const char *szFile,
                                      int         linenum)
    {
        SCAN_SCOPE_BEGIN;
        STATIC_CONTRACT_NOTHROW;  

        m_fCond = fCond;
        
        if (m_fCond)
        {
            m_pClrDebugState = GetClrDebugState();
            m_oldClrDebugState = *m_pClrDebugState;

            m_ContractStackRecord.m_szFunction = szFunction;
            m_ContractStackRecord.m_szFile     = szFile;
            m_ContractStackRecord.m_lineNum    = linenum;
            m_ContractStackRecord.m_testmask   = (Contract::ALL_Disabled & ~((UINT)(Contract::THROWS_Mask))) | Contract::THROWS_No;
            m_ContractStackRecord.m_construct  = "CANNOTTHROW";
            m_pClrDebugState->LinkContractStackTrace( &m_ContractStackRecord );

            m_pClrDebugState->ViolationMaskReset( ThrowsViolation );
            m_pClrDebugState->ResetOkToThrow();
        }
    }

    DEBUG_NOINLINE ~COMPlusCannotThrowExceptionHelper()
    {
        SCAN_SCOPE_END;

        if (m_fCond)
        {
            *m_pClrDebugState = m_oldClrDebugState;
        }
    }

private:
    BOOL m_fCond;

    ClrDebugState      *m_pClrDebugState;
    ClrDebugState       m_oldClrDebugState;

    ContractStackRecord   m_ContractStackRecord;
};

#endif // ENABLE_CONTRACTS_IMPL

//-------------------------------------------------------------------------------
// This simply tests to see if the exception object is a subclass of
// the descriminating class specified in the exception clause.
//-------------------------------------------------------------------------------
extern "C" BOOL ExceptionIsOfRightType(TypeHandle clauseType, TypeHandle thrownType);

//==========================================================================
// The stuff below is what works "behind the scenes" of the public macros.
//==========================================================================

#ifdef _TARGET_X86_
LPVOID COMPlusEndCatchWorker(Thread *pCurThread);
EXTERN_C LPVOID STDCALL COMPlusEndCatch(LPVOID ebp, DWORD ebx, DWORD edi, DWORD esi, LPVOID* pRetAddress);
#endif

// Specify NULL for uTryCatchResumeAddress when not checking for a InducedThreadRedirectAtEndOfCatch
EXTERN_C LPVOID COMPlusCheckForAbort(UINT_PTR uTryCatchResumeAddress = NULL);

BOOL        IsThreadHijackedForThreadStop(Thread* pThread, EXCEPTION_RECORD* pExceptionRecord);
void        AdjustContextForThreadStop(Thread* pThread, T_CONTEXT* pContext);
OBJECTREF   CreateCOMPlusExceptionObject(Thread* pThread, EXCEPTION_RECORD* pExceptionRecord, BOOL bAsynchronousThreadStop);

#if !defined(WIN64EXCEPTIONS)
EXCEPTION_HANDLER_DECL(COMPlusFrameHandler);
EXCEPTION_HANDLER_DECL(COMPlusNestedExceptionHandler);
#ifdef FEATURE_COMINTEROP
EXCEPTION_HANDLER_DECL(COMPlusFrameHandlerRevCom);
#endif // FEATURE_COMINTEROP

// Pop off any SEH handlers we have registered below pTargetSP
VOID __cdecl PopSEHRecords(LPVOID pTargetSP);

#if defined(_TARGET_X86_) && defined(DEBUGGING_SUPPORTED)
VOID UnwindExceptionTrackerAndResumeInInterceptionFrame(ExInfo* pExInfo, EHContext* context);
#endif // _TARGET_X86_ && DEBUGGING_SUPPORTED

BOOL PopNestedExceptionRecords(LPVOID pTargetSP, BOOL bCheckForUnknownHandlers = FALSE);
VOID PopNestedExceptionRecords(LPVOID pTargetSP, T_CONTEXT *pCtx, void *pSEH);

// Misc functions to access and update the SEH chain. Be very, very careful about updating the SEH chain.
// Frankly, if you think you need to use one of these function, please
// consult with the owner of the exception system.
PEXCEPTION_REGISTRATION_RECORD GetCurrentSEHRecord();
VOID SetCurrentSEHRecord(EXCEPTION_REGISTRATION_RECORD *pSEH);


#define STACK_OVERWRITE_BARRIER_SIZE 20
#define STACK_OVERWRITE_BARRIER_VALUE 0xabcdefab

#ifdef _DEBUG
#if defined(_TARGET_X86_)
struct FrameHandlerExRecordWithBarrier {
    DWORD m_StackOverwriteBarrier[STACK_OVERWRITE_BARRIER_SIZE];
    FrameHandlerExRecord m_ExRecord;
};

void VerifyValidTransitionFromManagedCode(Thread *pThread, CrawlFrame *pCF);
#endif // defined(_TARGET_X86_)
#endif // _DEBUG
#endif // !defined(WIN64EXCEPTIONS)

//==========================================================================
// This is a workaround designed to allow the use of the StubLinker object at bootup
// time where the EE isn't sufficient awake to create COM+ exception objects.
// Instead, COMPlusThrow(rexcep) does a simple RaiseException using this code.
// Or use COMPlusThrowBoot() to explicitly do so.
//==========================================================================
#define BOOTUP_EXCEPTION_COMPLUS  0xC0020001

void COMPlusThrowBoot(HRESULT hr);


//==========================================================================
// Used by the classloader to record a managed exception object to explain
// why a classload got botched.
//
// - Can be called with gc enabled or disabled.
//   This allows a catch-all error path to post a generic catchall error
//   message w/out bonking more specific error messages posted by inner functions.
//==========================================================================
VOID DECLSPEC_NORETURN ThrowTypeLoadException(LPCUTF8 pNameSpace, LPCUTF8 pTypeName,
                           LPCWSTR pAssemblyName, LPCUTF8 pMessageArg,
                           UINT resIDWhy);

VOID DECLSPEC_NORETURN ThrowTypeLoadException(LPCWSTR pFullTypeName,
                                              LPCWSTR pAssemblyName,
                                              LPCUTF8 pMessageArg,
                                              UINT resIDWhy);

VOID DECLSPEC_NORETURN ThrowFieldLayoutError(mdTypeDef cl,                // cl of the NStruct being loaded
                           Module* pModule,             // Module that defines the scope, loader and heap (for allocate FieldMarshalers)
                           DWORD   dwOffset,            // Field offset
                           DWORD   dwID);

UINT GetResourceIDForFileLoadExceptionHR(HRESULT hr);

FCDECL1(Object*, MissingMemberException_FormatSignature, I1Array* pPersistedSigUNSAFE);
FCDECL1(Object*, GetResourceFromDefault, StringObject* key);

#define EXCEPTION_NONCONTINUABLE 0x1    // Noncontinuable exception
#define EXCEPTION_UNWINDING 0x2         // Unwind is in progress
#define EXCEPTION_EXIT_UNWIND 0x4       // Exit unwind is in progress
#define EXCEPTION_STACK_INVALID 0x8     // Stack out of limits or unaligned
#define EXCEPTION_NESTED_CALL 0x10      // Nested exception handler call
#define EXCEPTION_TARGET_UNWIND 0x20    // Target unwind in progress
#define EXCEPTION_COLLIDED_UNWIND 0x40  // Collided exception handler call

#define EXCEPTION_UNWIND (EXCEPTION_UNWINDING | EXCEPTION_EXIT_UNWIND | \
                          EXCEPTION_TARGET_UNWIND | EXCEPTION_COLLIDED_UNWIND)

#define IS_UNWINDING(Flag) ((Flag & EXCEPTION_UNWIND) != 0)

//#include "CodeMan.h"

class EHRangeTreeNode;
class EHRangeTree;

typedef CUnorderedArray<EHRangeTreeNode *, 7> EH_CLAUSE_UNORDERED_ARRAY;

class EHRangeTreeNode
{
public:
    EHRangeTree                *m_pTree;
    EE_ILEXCEPTION_CLAUSE      *m_clause;

    EHRangeTreeNode            *m_pContainedBy;
    EH_CLAUSE_UNORDERED_ARRAY   m_containees;

    DWORD                       m_FilterEndPC;

private:
    // A node can represent a range or a single offset.
    // A node representing a range can either be the root node, which
    // contains everything and has a NULL m_clause, or it can be
    // a node mapping to an EH clause.
    DWORD                       m_offset;
    bool                        m_fIsRange;
    bool                        m_fIsRoot;

public:
    EHRangeTreeNode(void);
    EHRangeTreeNode(DWORD offset, bool fIsRange = false);
    void CommonCtor(DWORD offset, bool fIsRange);

    bool IsRange();
    void MarkAsRange();

    bool IsRoot();
    void MarkAsRoot(DWORD offset);

    DWORD GetOffset();
    DWORD GetTryStart();
    DWORD GetTryEnd();
    DWORD GetHandlerStart();
    DWORD GetHandlerEnd();
    DWORD GetFilterStart();
    DWORD GetFilterEnd();

    // These four functions may actually be called via FindContainer() while we are building the tree
    // structure, in which case we shouldn't really check the tree recursively because the result is unreliable.
    // Thus, they check m_pTree->m_fInitializing to see if they should call themselves recursively.
    // Also, FindContainer() has extra logic to work around this boot-strapping problem.
    bool Contains(EHRangeTreeNode* pNode);
    bool TryContains(EHRangeTreeNode* pNode);
    bool HandlerContains(EHRangeTreeNode* pNode);
    bool FilterContains(EHRangeTreeNode* pNode);

    // These are simple wrappers around the previous four.
    bool Contains(DWORD offset);
    bool TryContains(DWORD offset);
    bool HandlerContains(DWORD offset);
    bool FilterContains(DWORD offset);

    EHRangeTreeNode* GetContainer();

    HRESULT AddNode(EHRangeTreeNode *pNode);
} ;

class EHRangeTree
{
    unsigned                m_EHCount;
    EHRangeTreeNode        *m_rgNodes;
    EE_ILEXCEPTION_CLAUSE  *m_rgClauses;

public:

    EHRangeTreeNode        *m_root; // This is a sentinel, NOT an actual
                                    // Exception Handler!
    HRESULT                 m_hrInit; // Ctor fills this out.

    bool                    m_fInitializing;

    EHRangeTree(IJitManager* pIJM,
                const METHODTOKEN& methodToken,
                DWORD         methodSize,
                int           cFunclet,
                const DWORD * rgFuncletOffset);

    ~EHRangeTree();

    EHRangeTreeNode *FindContainer(EHRangeTreeNode *pNodeCur);
    EHRangeTreeNode *FindMostSpecificContainer(DWORD addr);
    EHRangeTreeNode *FindNextMostSpecificContainer(EHRangeTreeNode *pNodeCur,
                                                   DWORD addr);

    // <TODO> We shouldn't need this - instead, we
    // should get sequence points annotated with whether they're STACK_EMPTY, etc,
    // and then we'll figure out if the destination is ok based on that, instead.</TODO>
    BOOL isAtStartOfCatch(DWORD offset);
} ;

HRESULT SetIPFromSrcToDst(Thread *pThread,
                          SLOT addrStart,       // base address of method
                          DWORD offFrom,        // native offset
                          DWORD offTo,          // native offset
                          bool fCanSetIPOnly,   // if true, don't do any real work
                          PREGDISPLAY pReg,
                          PT_CONTEXT pCtx,
                          void *pDji,
                          EHRangeTree *pEHRT);

BOOL IsInFirstFrameOfHandler(Thread *pThread,
                             IJitManager *pJitManager,
                             const METHODTOKEN& MethodToken,
                             DWORD offSet);

//==========================================================================
// Handy helper functions
//==========================================================================
LONG FilterAccessViolation(PEXCEPTION_POINTERS pExceptionPointers, LPVOID lpvParam);

bool IsContinuableException(Thread *pThread);

bool IsInterceptableException(Thread *pThread);

#ifdef DEBUGGING_SUPPORTED
// perform simple checking to see if the current exception is intercepted
bool CheckThreadExceptionStateForInterception();

// Intercept the current exception and start an unwind.  This function may never return.
EXCEPTION_DISPOSITION ClrDebuggerDoUnwindAndIntercept(X86_FIRST_ARG(EXCEPTION_REGISTRATION_RECORD *pEstablisherFrame)
                                                      EXCEPTION_RECORD *pExceptionRecord);

LONG NotifyDebuggerLastChance(Thread *pThread,
                              EXCEPTION_POINTERS *pExceptionInfo,
                              BOOL jitAttachRequested);
#endif // DEBUGGING_SUPPORTED

#if defined(_TARGET_X86_)
void CPFH_AdjustContextForThreadSuspensionRace(T_CONTEXT *pContext, Thread *pThread);
#endif // _TARGET_X86_

DWORD GetGcMarkerExceptionCode(LPVOID ip);
bool IsGcMarker(DWORD exceptionCode, T_CONTEXT *pContext);

void InitSavedExceptionInfo();

bool ShouldHandleManagedFault(
                        EXCEPTION_RECORD*               pExceptionRecord,
                        T_CONTEXT*                        pContext,
                        EXCEPTION_REGISTRATION_RECORD*  pEstablisherFrame,
                        Thread*                         pThread);

void HandleManagedFault(EXCEPTION_RECORD*               pExceptionRecord,
                        T_CONTEXT*                        pContext,
                        EXCEPTION_REGISTRATION_RECORD*  pEstablisherFrame,
                        Thread*                         pThread);

LONG WatsonLastChance(
    Thread              *pThread,
    EXCEPTION_POINTERS  *pExceptionInfo,
    TypeOfReportedError tore);

bool DebugIsEECxxException(EXCEPTION_RECORD* pExceptionRecord);


inline void CopyOSContext(T_CONTEXT* pDest, T_CONTEXT* pSrc)
{
    SIZE_T cbReadOnlyPost = 0;
#ifdef _TARGET_AMD64_
    cbReadOnlyPost = sizeof(CONTEXT) - FIELD_OFFSET(CONTEXT, FltSave); // older OSes don't have the vector reg fields
#endif // _TARGET_AMD64_

    memcpyNoGCRefs(pDest, pSrc, sizeof(T_CONTEXT) - cbReadOnlyPost);
}

void SaveCurrentExceptionInfo(PEXCEPTION_RECORD pRecord, PT_CONTEXT pContext);

#ifdef _DEBUG
void SetReversePInvokeEscapingUnhandledExceptionStatus(BOOL fIsUnwinding,
#ifdef _TARGET_X86_
                                                       EXCEPTION_REGISTRATION_RECORD * pEstablisherFrame
#elif defined(WIN64EXCEPTIONS)
                                                       ULONG64 pEstablisherFrame
#else
#error Unsupported platform
#endif
                                                       );
#endif // _DEBUG

// See implementation for detailed comments in excep.cpp
LONG AppDomainTransitionExceptionFilter(
    EXCEPTION_POINTERS *pExceptionInfo, // the pExceptionInfo passed to a filter function.
    PVOID               pParam);         

// See implementation for detailed comments in excep.cpp
LONG ReflectionInvocationExceptionFilter(
    EXCEPTION_POINTERS *pExceptionInfo, // the pExceptionInfo passed to a filter function.
    PVOID               pParam);         

#ifdef FEATURE_CORRUPTING_EXCEPTIONS
// -----------------------------------------------------------------------
// Support for Corrupted State Exceptions
// -----------------------------------------------------------------------
#ifndef HANDLE_PROCESS_CORRUPTED_STATE_EXCEPTION_ATTRIBUTE
#define HANDLE_PROCESS_CORRUPTED_STATE_EXCEPTION_ATTRIBUTE "System.Runtime.ExceptionServices.HandleProcessCorruptedStateExceptionsAttribute"
#endif // HANDLE_PROCESS_CORRUPTED_STATE_EXCEPTION_ATTRIBUTE

#ifndef HIGHEST_MAJOR_VERSION_OF_PREV4_RUNTIME
#define HIGHEST_MAJOR_VERSION_OF_PREV4_RUNTIME 2
#endif // HIGHEST_MAJOR_VERSION_OF_PREV4_RUNTIME

// This helper class contains static method to support working with Corrupted State Exceptions, 
// including checking if a method can handle it or not, copy state across throwables, etc.
class CEHelper
{
    BOOL static IsMethodInPreV4Assembly(PTR_MethodDesc pMethodDesc);
    BOOL static CanMethodHandleCE(PTR_MethodDesc pMethodDesc, CorruptionSeverity severity, BOOL fCalculateSecurityInfo = TRUE);

public:
    BOOL static CanMethodHandleException(CorruptionSeverity severity, PTR_MethodDesc pMethodDesc, BOOL fCalculateSecurityInfo = TRUE);
    BOOL static CanIDispatchTargetHandleException();
    BOOL static IsProcessCorruptedStateException(DWORD dwExceptionCode, BOOL fCheckForSO = TRUE);
    BOOL static IsProcessCorruptedStateException(OBJECTREF oThrowable);
    BOOL static IsLastActiveExceptionCorrupting(BOOL fMarkForReuseIfCorrupting = FALSE);
    BOOL static ShouldTreatActiveExceptionAsNonCorrupting();
    void static MarkLastActiveExceptionCorruptionSeverityForReraiseReuse();
    void static SetupCorruptionSeverityForActiveException(BOOL fIsRethrownException, BOOL fIsNestedException, BOOL fShouldTreatExceptionAsNonCorrupting = FALSE);
#ifdef WIN64EXCEPTIONS
    typedef DPTR(class ExceptionTracker) PTR_ExceptionTracker;
    void static SetupCorruptionSeverityForActiveExceptionInUnwindPass(Thread *pCurThread, PTR_ExceptionTracker pEHTracker, BOOL fIsFirstPass, 
                                                                     DWORD dwExceptionCode);
#endif // WIN64EXCEPTIONS
    void static ResetLastActiveCorruptionSeverityPostCatchHandler(Thread *pThread);
};

#endif // FEATURE_CORRUPTING_EXCEPTIONS

#ifndef DACCESS_COMPILE
// Switches to the previous AppDomain on the thread. See implementation for detailed comments.
BOOL ReturnToPreviousAppDomain();

// This is a generic holder that will enable you to revert to previous execution context (e.g. an AD).
// Set it up *once* you have transitioned to the target context.
class ReturnToPreviousAppDomainHolder
{
protected: // protected so that derived holder classes can also use them
    BOOL        m_fShouldReturnToPreviousAppDomain;
    Thread    * m_pThread;
#ifdef _DEBUG
    AppDomain * m_pTransitionedToAD;
#endif // _DEBUG

    void Init();
    void ReturnToPreviousAppDomain();
        
public:
    ReturnToPreviousAppDomainHolder(); 
    ~ReturnToPreviousAppDomainHolder();
    void SuppressRelease();
};

// exception filter invoked for unhandled exceptions on the entry point thread (thread 0)
LONG EntryPointFilter(PEXCEPTION_POINTERS pExceptionInfo, PVOID _pData);

#endif // !DACCESS_COMPILE

// Enum that defines the types of exception notification handlers
// that we support.
enum ExceptionNotificationHandlerType
{
    UnhandledExceptionHandler   = 0x1
#ifdef FEATURE_EXCEPTION_NOTIFICATIONS
    ,
    FirstChanceExceptionHandler = 0x2
#endif // FEATURE_EXCEPTION_NOTIFICATIONS
};

// Defined in Frames.h
// class ContextTransitionFrame;

// This class contains methods to support delivering the various exception notifications.
class ExceptionNotifications
{
#ifdef FEATURE_EXCEPTION_NOTIFICATIONS
private:
    void static GetEventArgsForNotification(ExceptionNotificationHandlerType notificationType,
        OBJECTREF *pOutEventArgs, OBJECTREF *pThrowable);

    void static DeliverNotificationInternal(ExceptionNotificationHandlerType notificationType,
        OBJECTREF *pThrowable
#ifdef FEATURE_CORRUPTING_EXCEPTIONS        
        , CorruptionSeverity severity
#endif // FEATURE_CORRUPTING_EXCEPTIONS
        );

    void static InvokeNotificationDelegate(ExceptionNotificationHandlerType notificationType, OBJECTREF *pDelegate, OBJECTREF *pEventArgs, 
        OBJECTREF *pAppDomain
#ifdef FEATURE_CORRUPTING_EXCEPTIONS        
        , CorruptionSeverity severity
#endif // FEATURE_CORRUPTING_EXCEPTIONS
        );

public:
    BOOL static CanDeliverNotificationToCurrentAppDomain(ExceptionNotificationHandlerType notificationType);
    
    void static DeliverNotification(ExceptionNotificationHandlerType notificationType,
        OBJECTREF *pThrowable
#ifdef FEATURE_CORRUPTING_EXCEPTIONS        
        , CorruptionSeverity severity
#endif // FEATURE_CORRUPTING_EXCEPTIONS
        );

#ifdef FEATURE_CORRUPTING_EXCEPTIONS
    BOOL static CanDelegateBeInvokedForException(OBJECTREF *pDelegate, CorruptionSeverity severity);
#endif // FEATURE_CORRUPTING_EXCEPTIONS
#endif // FEATURE_EXCEPTION_NOTIFICATIONS

public:
    void static DeliverExceptionNotification(ExceptionNotificationHandlerType notificationType, OBJECTREF *pDelegate, 
        OBJECTREF *pAppDomain, OBJECTREF *pEventArgs);
    void static DeliverFirstChanceNotification();
};

#ifndef DACCESS_COMPILE

#if defined(_TARGET_X86_)
void ResetThreadAbortState(PTR_Thread pThread, void *pEstablisherFrame);
#elif defined(WIN64EXCEPTIONS)
void ResetThreadAbortState(PTR_Thread pThread, CrawlFrame *pCf, StackFrame sfCurrentStackFrame);
#endif

X86_ONLY(EXCEPTION_REGISTRATION_RECORD* GetNextCOMPlusSEHRecord(EXCEPTION_REGISTRATION_RECORD* pRec);)

#endif // !DACCESS_COMPILE

#endif // __excep_h__
