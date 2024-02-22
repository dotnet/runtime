// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

//

/*  EXCEP.CPP:
 *
 */

#include "common.h"

#include "frames.h"
#include "threads.h"
#include "excep.h"
#include "object.h"
#include "field.h"
#include "dbginterface.h"
#include "cgensys.h"
#include "comutilnative.h"
#include "siginfo.hpp"
#include "gcheaputilities.h"
#include "eedbginterfaceimpl.h" //so we can clearexception in RealCOMPlusThrow
#include "dllimportcallback.h"
#include "stackwalk.h" //for CrawlFrame, in SetIPFromSrcToDst
#include "shimload.h"
#include "eeconfig.h"
#include "virtualcallstub.h"
#include "typestring.h"

#ifndef TARGET_UNIX
#include "dwreport.h"
#endif // !TARGET_UNIX

#include "eventreporter.h"

#ifdef FEATURE_COMINTEROP
#include<roerrorapi.h>
#endif
#ifdef FEATURE_EH_FUNCLETS
#include "exceptionhandling.h"
#endif

#include <errorrep.h>
#ifndef TARGET_UNIX
// Include definition of GenericModeBlock
#include <msodw.h>
#endif // TARGET_UNIX


// Support for extracting MethodDesc of a delegate.
#include "comdelegate.h"

#ifdef HAVE_GCCOVER
#include "gccover.h"
#endif // HAVE_GCCOVER

#include "exinfo.h"

//----------------------------------------------------------------------------
//
// IsExceptionFromManagedCode - determine if pExceptionRecord points to a managed exception
//
// Arguments:
//    pExceptionRecord - pointer to exception record
//
// Return Value:
//    TRUE or FALSE
//
//----------------------------------------------------------------------------
BOOL IsExceptionFromManagedCode(const EXCEPTION_RECORD * pExceptionRecord)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        SUPPORTS_DAC;
        PRECONDITION(CheckPointer(pExceptionRecord));
    } CONTRACTL_END;

    if (pExceptionRecord == NULL)
    {
        return FALSE;
    }

    DACCOP_IGNORE(FieldAccess, "EXCEPTION_RECORD is a OS structure, and ExceptionAddress is actually a target address here.");
    UINT_PTR address = reinterpret_cast<UINT_PTR>(pExceptionRecord->ExceptionAddress);

    // An exception code of EXCEPTION_COMPLUS indicates a managed exception
    // has occurred (most likely due to executing a "throw" instruction).
    //
    // Also, a hardware level exception may not have an exception code of
    // EXCEPTION_COMPLUS. In this case, an exception address that resides in
    // managed code indicates a managed exception has occurred.
    return (IsComPlusException(pExceptionRecord) ||
            (ExecutionManager::IsManagedCode((PCODE)address)));
}


#ifndef DACCESS_COMPILE

#define SZ_UNHANDLED_EXCEPTION W("Unhandled exception.")
#define SZ_UNHANDLED_EXCEPTION_CHARLEN ((sizeof(SZ_UNHANDLED_EXCEPTION) / sizeof(WCHAR)))

PEXCEPTION_REGISTRATION_RECORD GetCurrentSEHRecord();
BOOL IsUnmanagedToManagedSEHHandler(EXCEPTION_REGISTRATION_RECORD*);

VOID DECLSPEC_NORETURN RealCOMPlusThrow(OBJECTREF throwable, BOOL rethrow);

//-------------------------------------------------------------------------------
// Basically, this asks whether the exception is a managed exception thrown by
// this instance of the CLR.
//
// The way the result is used, however, is to decide whether this instance is the
// one to throw up the Watson box.
//-------------------------------------------------------------------------------
BOOL ShouldOurUEFDisplayUI(PEXCEPTION_POINTERS pExceptionInfo)
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_FORBID_FAULT;

    // Test first for the canned SO EXCEPTION_POINTERS structure as it has a NULL context record and will break the code below.
    extern EXCEPTION_POINTERS g_SOExceptionPointers;
    if (pExceptionInfo == &g_SOExceptionPointers)
    {
        return TRUE;
    }
    return IsComPlusException(pExceptionInfo->ExceptionRecord) || ExecutionManager::IsManagedCode(GetIP(pExceptionInfo->ContextRecord));
}

BOOL NotifyAppDomainsOfUnhandledException(
    PEXCEPTION_POINTERS pExceptionPointers,
    OBJECTREF   *pThrowableIn,
    BOOL        useLastThrownObject,
    BOOL        isTerminating);

VOID SetManagedUnhandledExceptionBit(
    BOOL        useLastThrownObject);

//-------------------------------------------------------------------------------
// This simply tests to see if the exception object is a subclass of
// the descriminating class specified in the exception clause.
//-------------------------------------------------------------------------------
BOOL ExceptionIsOfRightType(TypeHandle clauseType, TypeHandle thrownType)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        FORBID_FAULT;
    }
    CONTRACTL_END;

    // if not resolved to, then it wasn't loaded and couldn't have been thrown
    if (clauseType.IsNull())
        return FALSE;

    if (clauseType == thrownType)
        return TRUE;

    // now look for parent match
    TypeHandle superType = thrownType;
    while (!superType.IsNull()) {
        if (superType == clauseType) {
            break;
        }
        superType = superType.GetParent();
    }

    return !superType.IsNull();
}

//===========================================================================
// Gets the message text from an exception
//===========================================================================
ULONG GetExceptionMessage(OBJECTREF throwable,
                          _Inout_updates_(bufferLength) LPWSTR buffer,
                          ULONG bufferLength)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        INJECT_FAULT(ThrowOutOfMemory());
    }
    CONTRACTL_END;

    // Prefast buffer sanity check.  Don't call the API with a zero length buffer.
    if (bufferLength == 0)
    {
        _ASSERTE(bufferLength > 0);
        return 0;
    }

    StackSString result;
    GetExceptionMessage(throwable, result);

    ULONG length = result.GetCount();
    LPCWSTR chars = result.GetUnicode();

    if (length < bufferLength)
    {
        wcsncpy_s(buffer, bufferLength, chars, length);
    }
    else
    {
        wcsncpy_s(buffer, bufferLength, chars, bufferLength-1);
    }

    return length;
}

//-----------------------------------------------------------------------------
// Given an object, get the "message" from it.  If the object is an Exception
//  call Exception.ToString, otherwise, call Object.ToString
//-----------------------------------------------------------------------------
void GetExceptionMessage(OBJECTREF throwable, SString &result)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        INJECT_FAULT(ThrowOutOfMemory());
    }
    CONTRACTL_END;

    STRINGREF pString = GetExceptionMessage(throwable);

    // If call returned NULL (not empty), oh well, no message.
    if (pString != NULL)
        pString->GetSString(result);
} // void GetExceptionMessage()

STRINGREF GetExceptionMessage(OBJECTREF throwable)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        INJECT_FAULT(ThrowOutOfMemory());
    }
    CONTRACTL_END;

    // If there is no object, there is no message.
    if (throwable == NULL)
        return NULL;

    // Return value.
    STRINGREF pString = NULL;

    GCPROTECT_BEGIN(throwable);

    // Call Object.ToString(). Note that exceptions do not have to inherit from System.Exception
    MethodDescCallSite toString(METHOD__OBJECT__TO_STRING, &throwable);

    // Make the call.
    ARG_SLOT arg[1] = {ObjToArgSlot(throwable)};
    pString = toString.Call_RetSTRINGREF(arg);

    GCPROTECT_END();

    return pString;
}

HRESULT GetExceptionHResult(OBJECTREF throwable)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;

    HRESULT hr = E_FAIL;
    if (throwable == NULL)
        return hr;

    // Since any object can be thrown in managed code, not only instances of System.Exception subclasses
    // we need to check to see if we are dealing with an exception before attempting to retrieve
    // the HRESULT field. If we are not dealing with an exception, then we will simply return E_FAIL.
    _ASSERTE(IsException(throwable->GetMethodTable()));        // what is the pathway here?
    if (IsException(throwable->GetMethodTable()))
    {
        hr = ((EXCEPTIONREF)throwable)->GetHResult();
    }

    return hr;
} // HRESULT GetExceptionHResult()

DWORD GetExceptionXCode(OBJECTREF throwable)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;

    HRESULT hr = E_FAIL;
    if (throwable == NULL)
        return hr;

    // Since any object can be thrown in managed code, not only instances of System.Exception subclasses
    // we need to check to see if we are dealing with an exception before attempting to retrieve
    // the HRESULT field. If we are not dealing with an exception, then we will simply return E_FAIL.
    _ASSERTE(IsException(throwable->GetMethodTable()));        // what is the pathway here?
    if (IsException(throwable->GetMethodTable()))
    {
        hr = ((EXCEPTIONREF)throwable)->GetXCode();
    }

    return hr;
} // DWORD GetExceptionXCode()

//------------------------------------------------------------------------------
// This function will extract some information from an Access Violation SEH
//  exception, and store it in the System.AccessViolationException object.
// - the faulting instruction's IP.
// - the target address of the faulting instruction.
// - a code indicating attempted read vs write
//------------------------------------------------------------------------------
void SetExceptionAVParameters(              // No return.
    OBJECTREF throwable,                    // The object into which to set the values.
    EXCEPTION_RECORD *pExceptionRecord)     // The SEH exception information.
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(throwable != NULL);
    }
    CONTRACTL_END;

    GCPROTECT_BEGIN(throwable)
    {
        // This should only be called for AccessViolationException
        _ASSERTE(CoreLibBinder::GetException(kAccessViolationException) == throwable->GetMethodTable());

        FieldDesc *pFD_ip = CoreLibBinder::GetField(FIELD__ACCESS_VIOLATION_EXCEPTION__IP);
        FieldDesc *pFD_target = CoreLibBinder::GetField(FIELD__ACCESS_VIOLATION_EXCEPTION__TARGET);
        FieldDesc *pFD_access = CoreLibBinder::GetField(FIELD__ACCESS_VIOLATION_EXCEPTION__ACCESSTYPE);

        _ASSERTE(pFD_ip->GetFieldType() == ELEMENT_TYPE_I);
        _ASSERTE(pFD_target->GetFieldType() == ELEMENT_TYPE_I);
        _ASSERTE(pFD_access->GetFieldType() == ELEMENT_TYPE_I4);

        void *ip     = pExceptionRecord->ExceptionAddress;
        void *target = (void*)(pExceptionRecord->ExceptionInformation[1]);
        DWORD access = (DWORD)(pExceptionRecord->ExceptionInformation[0]);

        pFD_ip->SetValuePtr(throwable, ip);
        pFD_target->SetValuePtr(throwable, target);
        pFD_access->SetValue32(throwable, access);

    }
    GCPROTECT_END();

} // void SetExceptionAVParameters()

//------------------------------------------------------------------------------
// This will call InternalPreserveStackTrace (if the throwable derives from
//  System.Exception), to copy the stack trace to the _remoteStackTraceString.
// Doing so allows the stack trace of an exception caught by the runtime, and
//  rethrown with COMPlusThrow(OBJECTREF thowable), to be preserved.  Otherwise
//  the exception handling code may clear the stack trace.  (Generally, we see
//  the stack trace preserved on win32 and cleared on win64.)
//------------------------------------------------------------------------------
void ExceptionPreserveStackTrace(   // No return.
    OBJECTREF throwable)            // Object about to be thrown.
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        INJECT_FAULT(ThrowOutOfMemory());
    }
    CONTRACTL_END;

    // If there is no object, there is no stack trace to save.
    if (throwable == NULL)
        return;

    GCPROTECT_BEGIN(throwable);

    // Make sure it is derived from System.Exception, that it is not one of the
    //  preallocated exception objects, and that it has a stack trace to save.
    if (IsException(throwable->GetMethodTable()) &&
        !CLRException::IsPreallocatedExceptionObject(throwable))
    {
        LOG((LF_EH, LL_INFO1000, "ExceptionPreserveStackTrace called\n"));

        // Call Exception.InternalPreserveStackTrace() ...
        MethodDescCallSite preserveStackTrace(METHOD__EXCEPTION__INTERNAL_PRESERVE_STACK_TRACE, &throwable);

        // Make the call.
        ARG_SLOT arg[1] = {ObjToArgSlot(throwable)};
        preserveStackTrace.Call(arg);
    }

    GCPROTECT_END();

} // void ExceptionPreserveStackTrace()


// We have to cache the MethodTable and FieldDesc for wrapped non-compliant exceptions the first
// time we wrap, because we cannot tolerate a GC when it comes time to detect and unwrap one.

static MethodTable *pMT_RuntimeWrappedException;
static FieldDesc   *pFD_WrappedException;

// Non-compliant exceptions are immediately wrapped in a RuntimeWrappedException instance.  The entire
// exception system can now ignore the possibility of these cases except:
//
// 1) IL_Throw, which must wrap via this API
// 2) Calls to Filters & Catch handlers, which must unwrap based on whether the assembly is on the legacy
//    plan.
//
void WrapNonCompliantException(OBJECTREF *ppThrowable)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(IsProtectedByGCFrame(ppThrowable));
    }
    CONTRACTL_END;

    _ASSERTE(!IsException((*ppThrowable)->GetMethodTable()));

    EX_TRY
    {
        // idempotent operations, so the race condition is okay.
        if (pMT_RuntimeWrappedException == NULL)
            pMT_RuntimeWrappedException = CoreLibBinder::GetException(kRuntimeWrappedException);

        if (pFD_WrappedException == NULL)
            pFD_WrappedException = CoreLibBinder::GetField(FIELD__RUNTIME_WRAPPED_EXCEPTION__WRAPPED_EXCEPTION);

        OBJECTREF orWrapper = AllocateObject(CoreLibBinder::GetException(kRuntimeWrappedException));

        GCPROTECT_BEGIN(orWrapper);

        MethodDescCallSite ctor(METHOD__RUNTIME_WRAPPED_EXCEPTION__OBJ_CTOR, &orWrapper);

        ARG_SLOT args[] =
        {
            ObjToArgSlot(orWrapper),
            ObjToArgSlot(*ppThrowable)
        };

        ctor.Call(args);

        *ppThrowable = orWrapper;

        GCPROTECT_END();
    }
    EX_CATCH
    {
        // If we took an exception while binding, or running the constructor of the RuntimeWrappedException
        // instance, we know that this new exception is CLS compliant.  In fact, it's likely to be
        // OutOfMemoryException, StackOverflowException or ThreadAbortException.
        OBJECTREF orReplacement = GET_THROWABLE();

        _ASSERTE(IsException(orReplacement->GetMethodTable()));

        *ppThrowable = orReplacement;

    } EX_END_CATCH(SwallowAllExceptions);
}

// Before presenting an exception object to a handler (filter or catch, not finally or fault), it
// may be necessary to turn it back into a non-compliant exception.  This is conditioned on an
// assembly level setting.
OBJECTREF PossiblyUnwrapThrowable(OBJECTREF throwable, Assembly *pAssembly)
{
    // Check if we are required to compute the RuntimeWrapExceptions status.
    BOOL fIsRuntimeWrappedException = ((throwable != NULL) && (throwable->GetMethodTable() == pMT_RuntimeWrappedException));
    BOOL fRequiresComputingRuntimeWrapExceptionsStatus = (fIsRuntimeWrappedException &&
                                                          (!(pAssembly->GetModule()->IsRuntimeWrapExceptionsStatusComputed())));

    CONTRACTL
    {
        THROWS;
        // If we are required to compute the status of RuntimeWrapExceptions, then the operation could trigger a GC.
        // Thus, conditionally setup the contract.
        if (fRequiresComputingRuntimeWrapExceptionsStatus) GC_TRIGGERS; else GC_NOTRIGGER;
        MODE_COOPERATIVE;
        PRECONDITION(CheckPointer(pAssembly));
    }
    CONTRACTL_END;

    if (fIsRuntimeWrappedException && (!pAssembly->GetModule()->IsRuntimeWrapExceptions()))
    {
        // We already created the instance, fetched the field.  We know it is
        // not marshal by ref, or any of the other cases that might trigger GC.
        ENABLE_FORBID_GC_LOADER_USE_IN_THIS_SCOPE();

        throwable = pFD_WrappedException->GetRefValue(throwable);
    }

    return throwable;
}


// This is used by a holder in CreateTypeInitializationExceptionObject to
// reset the state as appropriate.
void ResetTypeInitializationExceptionState(BOOL isAlreadyCreating)
{
    LIMITED_METHOD_CONTRACT;
    if (!isAlreadyCreating)
        GetThread()->ResetIsCreatingTypeInitException();
}

void CreateTypeInitializationExceptionObject(LPCWSTR pTypeThatFailed,
                                             OBJECTREF *pInnerException,
                                             OBJECTREF *pInitException,
                                             OBJECTREF *pThrowable)
{
    CONTRACTL {
        NOTHROW;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(CheckPointer(pInnerException, NULL_OK));
        PRECONDITION(CheckPointer(pInitException));
        PRECONDITION(CheckPointer(pThrowable));
        PRECONDITION(IsProtectedByGCFrame(pInnerException));
        PRECONDITION(IsProtectedByGCFrame(pInitException));
        PRECONDITION(IsProtectedByGCFrame(pThrowable));
        PRECONDITION(CheckPointer(GetThreadNULLOk()));
    } CONTRACTL_END;

    Thread *pThread  = GetThreadNULLOk();
    *pThrowable = NULL;

    // This will make sure to put the thread back to its original state if something
    // throws out of this function (like an OOM exception or something)
    Holder< BOOL, DoNothing< BOOL >, ResetTypeInitializationExceptionState, FALSE, NoNull< BOOL > >
        isAlreadyCreating(pThread->IsCreatingTypeInitException());

    EX_TRY {
        // This will contain the type of exception we want to create. Read comment below
        // on why we'd want to create an exception other than TypeInitException
        MethodTable *pMT;
        BinderMethodID methodID;

        // If we are already in the midst of creating a TypeInitializationException object,
        // and we get here, it means there was an exception thrown while initializing the
        // TypeInitializationException type itself, or one of the types used by its class
        // constructor. In this case, we're going to back down and use a SystemException
        // object in its place. It is *KNOWN* that both these exception types have identical
        // .ctor sigs "void instance (string, exception)" so both can be used interchangeably
        // in the code that follows.
        if (!isAlreadyCreating.GetValue()) {
            pThread->SetIsCreatingTypeInitException();
            pMT = CoreLibBinder::GetException(kTypeInitializationException);
            methodID = METHOD__TYPE_INIT_EXCEPTION__STR_EX_CTOR;
        }
        else {
            // If we ever hit one of these asserts, then it is bad
            // because we do not know what exception to return then.
            _ASSERTE(pInnerException != NULL);
            _ASSERTE(*pInnerException != NULL);
            *pThrowable = *pInnerException;
            *pInitException = *pInnerException;
            goto ErrExit;
        }

        // Allocate the exception object
        *pThrowable = AllocateObject(pMT);

        MethodDescCallSite ctor(methodID, pThrowable);

        // Since the inner exception object in the .ctor is of type Exception, make sure
        // that the object we're passed in derives from Exception. If not, pass NULL.
        BOOL isException = FALSE;
        if (pInnerException != NULL)
            isException = IsException((*pInnerException)->GetMethodTable());

        _ASSERTE(isException);      // What pathway can give us non-compliant exceptions?

        STRINGREF sType = StringObject::NewString(pTypeThatFailed);

        // If the inner object derives from exception, set it as the third argument.
        ARG_SLOT args[] = { ObjToArgSlot(*pThrowable),
                            ObjToArgSlot(sType),
                            ObjToArgSlot(isException ? *pInnerException : NULL) };

        // Call the .ctor
        ctor.Call(args);

        // On success, set the init exception.
        *pInitException = *pThrowable;
    }
    EX_CATCH {
        // If calling the constructor fails, then we'll call ourselves again, and this time
        // through we will try and create an EEException object. If that fails, then the
        // else block of this will be executed.
        if (!isAlreadyCreating.GetValue()) {
            CreateTypeInitializationExceptionObject(pTypeThatFailed, pInnerException, pInitException, pThrowable);
        }

        // If we were already in the middle of creating a type init
        // exception when we were called, we would have tried to create an EEException instead
        // of a TypeInitException.
        else {
            // If we're recursing, then we should be calling ourselves from DoRunClassInitThrowing,
            // in which case we're guaranteed that we're passing in all three arguments.
            *pInitException = pInnerException ? *pInnerException : NULL;
            *pThrowable = GET_THROWABLE();
        }
    } EX_END_CATCH(SwallowAllExceptions);

    CONSISTENCY_CHECK(*pInitException != NULL || !pInnerException);

 ErrExit:
    ;
}

// ==========================================================================
// ComputeEnclosingHandlerNestingLevel
//
//  This is code factored out of COMPlusThrowCallback to figure out
//  what the number of nested exception handlers is.
// ==========================================================================
DWORD ComputeEnclosingHandlerNestingLevel(IJitManager *pIJM,
                                          const METHODTOKEN& mdTok,
                                          SIZE_T offsNat)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        FORBID_FAULT;
    }
    CONTRACTL_END;

    // Determine the nesting level of EHClause. Just walk the table
    // again, and find out how many handlers enclose it
    DWORD nestingLevel = 0;
    EH_CLAUSE_ENUMERATOR pEnumState;
    unsigned EHCount = pIJM->InitializeEHEnumeration(mdTok, &pEnumState);

    for (unsigned j=0; j<EHCount; j++)
    {
        EE_ILEXCEPTION_CLAUSE EHClause;

        pIJM->GetNextEHClause(&pEnumState,&EHClause);
        _ASSERTE(EHClause.HandlerEndPC != (DWORD) -1);  // <TODO> remove, only protects against a deprecated convention</TODO>

        if ((offsNat > EHClause.HandlerStartPC) &&
            (offsNat < EHClause.HandlerEndPC))
        {
            nestingLevel++;
        }
    }

    return nestingLevel;
}

// ******************************* EHRangeTreeNode ************************** //
EHRangeTreeNode::EHRangeTreeNode(void)
{
    WRAPPER_NO_CONTRACT;
    CommonCtor(0, false);
}

EHRangeTreeNode::EHRangeTreeNode(DWORD offset, bool fIsRange /* = false */)
{
    WRAPPER_NO_CONTRACT;
    CommonCtor(offset, fIsRange);
}

void EHRangeTreeNode::CommonCtor(DWORD offset, bool fIsRange)
{
    LIMITED_METHOD_CONTRACT;

    m_pTree = NULL;
    m_clause = NULL;

    m_pContainedBy = NULL;

    m_offset   = offset;
    m_fIsRange = fIsRange;
    m_fIsRoot  = false;      // must set this flag explicitly
}

inline bool EHRangeTreeNode::IsRange()
{
    // Please see the header file for an explanation of this assertion.
    _ASSERTE(m_fIsRoot || m_clause != NULL || !m_fIsRange);
    return m_fIsRange;
}

void EHRangeTreeNode::MarkAsRange()
{
    m_offset   = 0;
    m_fIsRange = true;
    m_fIsRoot  = false;
}

inline bool EHRangeTreeNode::IsRoot()
{
    // Please see the header file for an explanation of this assertion.
    _ASSERTE(m_fIsRoot || m_clause != NULL || !m_fIsRange);
    return m_fIsRoot;
}

void EHRangeTreeNode::MarkAsRoot(DWORD offset)
{
    m_offset   = offset;
    m_fIsRange = true;
    m_fIsRoot  = true;
}

inline DWORD EHRangeTreeNode::GetOffset()
{
    _ASSERTE(m_clause == NULL);
    _ASSERTE(IsRoot() || !IsRange());
    return m_offset;
}

inline DWORD EHRangeTreeNode::GetTryStart()
{
    _ASSERTE(IsRange());
    _ASSERTE(!IsRoot());
    if (IsRoot())
    {
        return 0;
    }
    else
    {
        return m_clause->TryStartPC;
    }
}

inline DWORD EHRangeTreeNode::GetTryEnd()
{
    _ASSERTE(IsRange());
    _ASSERTE(!IsRoot());
    if (IsRoot())
    {
        return GetOffset();
    }
    else
    {
        return m_clause->TryEndPC;
    }
}

inline DWORD EHRangeTreeNode::GetHandlerStart()
{
    _ASSERTE(IsRange());
    _ASSERTE(!IsRoot());
    if (IsRoot())
    {
        return 0;
    }
    else
    {
        return m_clause->HandlerStartPC;
    }
}

inline DWORD EHRangeTreeNode::GetHandlerEnd()
{
    _ASSERTE(IsRange());
    _ASSERTE(!IsRoot());
    if (IsRoot())
    {
        return GetOffset();
    }
    else
    {
        return m_clause->HandlerEndPC;
    }
}

inline DWORD EHRangeTreeNode::GetFilterStart()
{
    _ASSERTE(IsRange());
    _ASSERTE(!IsRoot());
    if (IsRoot())
    {
        return 0;
    }
    else
    {
        return m_clause->FilterOffset;
    }
}

// Get the end offset of the filter clause.  This offset is exclusive.
inline DWORD EHRangeTreeNode::GetFilterEnd()
{
    _ASSERTE(IsRange());
    _ASSERTE(!IsRoot());
    if (IsRoot())
    {
        // We should never get here if the "this" node is the root.
        // By definition, the root contains everything.  No checking is necessary.
        return 0;
    }
    else
    {
        return m_FilterEndPC;
    }
}

bool EHRangeTreeNode::Contains(DWORD offset)
{
    WRAPPER_NO_CONTRACT;

    EHRangeTreeNode node(offset);
    return Contains(&node);
}

bool EHRangeTreeNode::TryContains(DWORD offset)
{
    WRAPPER_NO_CONTRACT;

    EHRangeTreeNode node(offset);
    return TryContains(&node);
}

bool EHRangeTreeNode::HandlerContains(DWORD offset)
{
    WRAPPER_NO_CONTRACT;

    EHRangeTreeNode node(offset);
    return HandlerContains(&node);
}

bool EHRangeTreeNode::FilterContains(DWORD offset)
{
    WRAPPER_NO_CONTRACT;

    EHRangeTreeNode node(offset);
    return FilterContains(&node);
}

bool EHRangeTreeNode::Contains(EHRangeTreeNode* pNode)
{
    LIMITED_METHOD_CONTRACT;

    // If we are checking a range of address, then we should check the end address inclusively.
    if (pNode->IsRoot())
    {
        // No node contains the root node.
        return false;
    }
    else if (this->IsRoot())
    {
        return (pNode->IsRange() ?
                  (pNode->GetTryEnd() <= this->GetOffset()) && (pNode->GetHandlerEnd() <= this->GetOffset())
                : (pNode->GetOffset() < this->GetOffset()) );
    }
    else
    {
        return (this->TryContains(pNode) || this->HandlerContains(pNode) || this->FilterContains(pNode));
    }
}

bool EHRangeTreeNode::TryContains(EHRangeTreeNode* pNode)
{
    LIMITED_METHOD_CONTRACT;

    _ASSERTE(this->IsRange());

    if (pNode->IsRoot())
    {
        // No node contains the root node.
        return false;
    }
    else if (this->IsRoot())
    {
        // We will only get here from GetTcf() to determine if an address is in a try clause.
        // In this case we want to return false.
        return false;
    }
    else
    {
        DWORD tryStart = this->GetTryStart();
        DWORD tryEnd   = this->GetTryEnd();

        // If we are checking a range of address, then we should check the end address inclusively.
        if (pNode->IsRange())
        {
            DWORD start = pNode->GetTryStart();
            DWORD end   = pNode->GetTryEnd();

            if (start == tryStart && end == tryEnd)
            {
                return false;
            }
            else if (start == end)
            {
                // This is effectively a single offset.
                if ((tryStart <= start) && (end < tryEnd))
                {
                    return true;
                }
            }
            else if ((tryStart <= start) && (end <= tryEnd))
            {
                return true;
            }
        }
        else
        {
            DWORD offset = pNode->GetOffset();
            if ((tryStart <= offset) && (offset < tryEnd))
            {
                return true;
            }
        }
    }

#ifdef FEATURE_EH_FUNCLETS
    // If we are boot-strapping the tree, don't recurse down because the result could be unreliable.  Note that
    // even if we don't recurse, given a particular node, we can still always find its most specific container with
    // the logic above, i.e. it's always safe to do one depth level of checking.
    //
    // To build the tree, all we need to know is the most specific container of a particular node.  This can be
    // done by just comparing the offsets of the try regions.  However, funclets create a problem because even if
    // a funclet is conceptually contained in a try region, we cannot determine this fact just by comparing the offsets.
    // This is when we need to recurse the tree.  Here is a classic example:
    // try
    // {
    //     try
    //     {
    //     }
    //     catch
    //     {
    //         // If the offset is here, then we need to recurse.
    //     }
    // }
    // catch
    // {
    // }
    if (!m_pTree->m_fInitializing)
    {
        // Iterate all the contained clauses, and for the ones which are contained in the try region,
        // ask if the requested range is contained by it.
        USHORT i        = 0;
        USHORT numNodes = m_containees.Count();
        EHRangeTreeNode** ppNodes = NULL;
        for (i = 0, ppNodes = m_containees.Table(); i < numNodes; i++, ppNodes++)
        {
            // This variable is purely used for readability.
            EHRangeTreeNode* pNodeCur = *ppNodes;

            // it's possible for nested try blocks to have the same beginning and end offsets
            if ( ( this->GetTryStart()   <= pNodeCur->GetTryStart() ) &&
                 ( pNodeCur->GetTryEnd() <=  this->GetTryEnd()       ) )
            {
                if (pNodeCur->Contains(pNode))
                {
                    return true;
                }
            }
        }
    }
#endif // FEATURE_EH_FUNCLETS

    return false;
}

bool EHRangeTreeNode::HandlerContains(EHRangeTreeNode* pNode)
{
    LIMITED_METHOD_CONTRACT;

    _ASSERTE(this->IsRange());

    if (pNode->IsRoot())
    {
        // No node contains the root node.
        return false;
    }
    else if (this->IsRoot())
    {
        // We will only get here from GetTcf() to determine if an address is in a try clause.
        // In this case we want to return false.
        return false;
    }
    else
    {
        DWORD handlerStart = this->GetHandlerStart();
        DWORD handlerEnd   = this->GetHandlerEnd();

        // If we are checking a range of address, then we should check the end address inclusively.
        if (pNode->IsRange())
        {
            DWORD start = pNode->GetTryStart();
            DWORD end   = pNode->GetTryEnd();

            if (start == handlerStart && end == handlerEnd)
            {
                return false;
            }
            else if ((handlerStart <= start) && (end <= handlerEnd))
            {
                return true;
            }
        }
        else
        {
            DWORD offset = pNode->GetOffset();
            if ((handlerStart <= offset) && (offset < handlerEnd))
            {
                return true;
            }
        }
    }

#ifdef FEATURE_EH_FUNCLETS
    // Refer to the comment in TryContains().
    if (!m_pTree->m_fInitializing)
    {
        // Iterate all the contained clauses, and for the ones which are contained in the try region,
        // ask if the requested range is contained by it.
        USHORT i        = 0;
        USHORT numNodes = m_containees.Count();
        EHRangeTreeNode** ppNodes = NULL;
        for (i = 0, ppNodes = m_containees.Table(); i < numNodes; i++, ppNodes++)
        {
            // This variable is purely used for readability.
            EHRangeTreeNode* pNodeCur = *ppNodes;

            if ( ( this->GetHandlerStart() <= pNodeCur->GetTryStart() ) &&
                 ( pNodeCur->GetTryEnd()   <  this->GetHandlerEnd()   ) )
            {
                if (pNodeCur->Contains(pNode))
                {
                    return true;
                }
            }
        }
    }
#endif // FEATURE_EH_FUNCLETS

    return false;
}

bool EHRangeTreeNode::FilterContains(EHRangeTreeNode* pNode)
{
    LIMITED_METHOD_CONTRACT;

    _ASSERTE(this->IsRange());

    if (pNode->IsRoot())
    {
        // No node contains the root node.
        return false;
    }
    else if (this->IsRoot() || !IsFilterHandler(this->m_clause))
    {
        // We will only get here from GetTcf() to determine if an address is in a try clause.
        // In this case we want to return false.
        return false;
    }
    else
    {
        DWORD filterStart = this->GetFilterStart();
        DWORD filterEnd   = this->GetFilterEnd();

        // If we are checking a range of address, then we should check the end address inclusively.
        if (pNode->IsRange())
        {
            DWORD start = pNode->GetTryStart();
            DWORD end   = pNode->GetTryEnd();

            if (start == filterStart && end == filterEnd)
            {
                return false;
            }
            else if ((filterStart <= start) && (end <= filterEnd))
            {
                return true;
            }
        }
        else
        {
            DWORD offset = pNode->GetOffset();
            if ((filterStart <= offset) && (offset < filterEnd))
            {
                return true;
            }
        }
    }

#ifdef FEATURE_EH_FUNCLETS
    // Refer to the comment in TryContains().
    if (!m_pTree->m_fInitializing)
    {
        // Iterate all the contained clauses, and for the ones which are contained in the try region,
        // ask if the requested range is contained by it.
        USHORT i        = 0;
        USHORT numNodes = m_containees.Count();
        EHRangeTreeNode** ppNodes = NULL;
        for (i = 0, ppNodes = m_containees.Table(); i < numNodes; i++, ppNodes++)
        {
            // This variable is purely used for readability.
            EHRangeTreeNode* pNodeCur = *ppNodes;

            if ( ( this->GetFilterStart() <= pNodeCur->GetTryStart() ) &&
                 ( pNodeCur->GetTryEnd()  <  this->GetFilterEnd() ) )
            {
                if (pNodeCur->Contains(pNode))
                {
                    return true;
                }
            }
        }
    }
#endif // FEATURE_EH_FUNCLETS

    return false;
}

EHRangeTreeNode* EHRangeTreeNode::GetContainer()
{
    return m_pContainedBy;
}

HRESULT EHRangeTreeNode::AddNode(EHRangeTreeNode *pNode)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        INJECT_FAULT(return E_OUTOFMEMORY;);
        PRECONDITION(pNode != NULL);
    }
    CONTRACTL_END;

    EHRangeTreeNode **ppEH = m_containees.Append();

    if (ppEH == NULL)
        return E_OUTOFMEMORY;

    (*ppEH) = pNode;
    return S_OK;
}

// ******************************* EHRangeTree ************************** //

EHRangeTree::EHRangeTree(IJitManager* pIJM,
                         const METHODTOKEN& methodToken,
                         DWORD         methodSize,
                         int           cFunclet,
                         const DWORD * rgFunclet)
{
    LIMITED_METHOD_CONTRACT;

    LOG((LF_CORDB, LL_INFO10000, "EHRT::ERHT: already loaded!\n"));

    EH_CLAUSE_ENUMERATOR pEnumState;
    m_EHCount = pIJM->InitializeEHEnumeration(methodToken, &pEnumState);

    _ASSERTE(m_EHCount != 0xFFFFFFFF);

    ULONG i = 0;

    m_rgClauses = NULL;
    m_rgNodes = NULL;
    m_root = NULL;
    m_hrInit = S_OK;
    m_fInitializing = true;

    if (m_EHCount > 0)
    {
        m_rgClauses = new (nothrow) EE_ILEXCEPTION_CLAUSE[m_EHCount];
        if (m_rgClauses == NULL)
        {
           m_hrInit = E_OUTOFMEMORY;
           goto LError;
        }
    }

    LOG((LF_CORDB, LL_INFO10000, "EHRT::CC: m_ehcount:0x%x, m_rgClauses:0%x\n",
         m_EHCount, m_rgClauses));

    m_rgNodes = new (nothrow) EHRangeTreeNode[m_EHCount+1];
    if (m_rgNodes == NULL)
    {
       m_hrInit = E_OUTOFMEMORY;
       goto LError;
    }

    //this contains everything, even stuff on the last IP
    m_root = &(m_rgNodes[m_EHCount]);
    m_root->MarkAsRoot(methodSize + 1);

    LOG((LF_CORDB, LL_INFO10000, "EHRT::CC: rgNodes:0x%x\n", m_rgNodes));

    if (m_EHCount ==0)
    {
        LOG((LF_CORDB, LL_INFO10000, "EHRT::CC: About to leave!\n"));
        goto LSuccess;
    }

    LOG((LF_CORDB, LL_INFO10000, "EHRT::CC: Sticking around!\n"));

    // First, load all the EH clauses into the object.
    for (i = 0; i < m_EHCount; i++)
    {
        EE_ILEXCEPTION_CLAUSE * pEHClause = &(m_rgClauses[i]);

        LOG((LF_CORDB, LL_INFO10000, "EHRT::CC: i:0x%x!\n", i));

        pIJM->GetNextEHClause(&pEnumState, pEHClause);

        LOG((LF_CORDB, LL_INFO10000, "EHRT::CC: EHRTT_JIT_MANAGER got clause\n", i));

        LOG((LF_CORDB, LL_INFO10000, "EHRT::CC: clause 0x%x,"
                    "addrof:0x%x\n", i, pEHClause ));

        _ASSERTE(pEHClause->HandlerEndPC != (DWORD) -1);  // <TODO> remove, only protects against a deprecated convention</TODO>

        EHRangeTreeNode * pNodeCur = &(m_rgNodes[i]);

        pNodeCur->m_pTree = this;
        pNodeCur->m_clause = pEHClause;

        if (pEHClause->Flags == COR_ILEXCEPTION_CLAUSE_FILTER)
        {
#ifdef FEATURE_EH_FUNCLETS
            // Because of funclets, there is no way to guarantee the placement of a filter.
            // Thus, we need to loop through the funclets to find the end offset.
            for (int f = 0; f < cFunclet; f++)
            {
                // Check the start offset of the filter funclet.
                if (pEHClause->FilterOffset == rgFunclet[f])
                {
                    if (f < (cFunclet - 1))
                    {
                        // If it's NOT the last funclet, use the start offset of the next funclet.
                        pNodeCur->m_FilterEndPC = rgFunclet[f + 1];
                    }
                    else
                    {
                        // If it's the last funclet, use the size of the method.
                        pNodeCur->m_FilterEndPC = methodSize;
                    }
                    break;
                }
            }
#else  // FEATURE_EH_FUNCLETS
            // On x86, since the filter doesn't have an end FilterPC, the only way we can know the size
            // of the filter is if it's located immediately prior to it's handler and immediately after
            // its try region.  We assume that this is, and if it isn't, we're so amazingly hosed that
            // we can't continue.
            if ((pEHClause->FilterOffset >= pEHClause->HandlerStartPC) ||
                (pEHClause->FilterOffset < pEHClause->TryEndPC))
        {
            m_hrInit = CORDBG_E_SET_IP_IMPOSSIBLE;
            goto LError;
        }
            pNodeCur->m_FilterEndPC = pEHClause->HandlerStartPC;
#endif // FEATURE_EH_FUNCLETS
        }

        pNodeCur->MarkAsRange();
    }

    LOG((LF_CORDB, LL_INFO10000, "EHRT::CC: about to do the second pass\n"));


    // Second, for each EH, find it's most limited, containing clause
    // On WIN64, we have duplicate clauses.  There are two types of duplicate clauses.
    //
    // The first type is described in ExceptionHandling.cpp.  This type doesn't add additional information to the
    // EH tree structure.  For example, if an offset is in the try region of a duplicate clause of this type,
    // then some clause which comes before the duplicate clause should contain the offset in its handler region.
    // Therefore, even though this type of duplicate clauses are added to the EH tree, they should never be used.
    //
    // The second type is what's called the protected clause.  These clauses are used to mark the cloned finally
    // region.  They have an empty try region.  Here's an example:
    //
    // // C# code
    // try
    // {
    //     A
    // }
    // finally
    // {
    //     B
    // }
    //
    // // jitted code
    // parent
    // -------
    // A
    // B'
    // -------
    //
    // funclet
    // -------
    // B
    // -------
    //
    // A protected clause covers the B' region in the parent method.  In essence you can think of the method as
    // having two try/finally regions, and that's exactly how protected clauses are handled in the EH tree.
    // They are added to the EH tree just like any other EH clauses.
    for (i = 0; i < m_EHCount; i++)
    {
        LOG((LF_CORDB, LL_INFO10000, "EHRT::CC: SP:0x%x\n", i));

        EHRangeTreeNode * pNodeCur = &(m_rgNodes[i]);

        EHRangeTreeNode *pNodeCandidate = NULL;
        pNodeCandidate = FindContainer(pNodeCur);
        _ASSERTE(pNodeCandidate != NULL);

        pNodeCur->m_pContainedBy = pNodeCandidate;

        LOG((LF_CORDB, LL_INFO10000, "EHRT::CC: SP: about to add to tree\n"));

        HRESULT hr = pNodeCandidate->AddNode(pNodeCur);
        if (FAILED(hr))
        {
            m_hrInit = hr;
            goto LError;
        }
    }

LSuccess:
    m_fInitializing = false;
    return;

LError:
    LOG((LF_CORDB, LL_INFO10000, "EHRT::CC: LError - something went wrong!\n"));

    if (m_rgClauses != NULL)
    {
        delete [] m_rgClauses;
        m_rgClauses = NULL;
    }

    if (m_rgNodes != NULL)
    {
        delete [] m_rgNodes;
        m_rgNodes = NULL;
    }

    m_fInitializing = false;

    LOG((LF_CORDB, LL_INFO10000, "EHRT::CC: Falling off of LError!\n"));
} // Ctor Core

EHRangeTree::~EHRangeTree()
{
    LIMITED_METHOD_CONTRACT;

    if (m_rgNodes != NULL)
        delete [] m_rgNodes;

    if (m_rgClauses != NULL)
        delete [] m_rgClauses;
} //Dtor

EHRangeTreeNode *EHRangeTree::FindContainer(EHRangeTreeNode *pNodeSearch)
{
    LIMITED_METHOD_CONTRACT;

    EHRangeTreeNode *pNodeCandidate = NULL;

    // Examine the root, too.
    for (ULONG iInner = 0; iInner < m_EHCount+1; iInner++)
    {
        EHRangeTreeNode *pNodeCur = &(m_rgNodes[iInner]);

        // Check if the current node contains the node we are searching for.
        if ((pNodeSearch != pNodeCur) &&
            pNodeCur->Contains(pNodeSearch))
        {
            // Update the candidate node if it is NULL or if it contains the current node
            // (i.e. the current node is more specific than the candidate node).
            if ((pNodeCandidate == NULL) ||
                pNodeCandidate->Contains(pNodeCur))
            {
                pNodeCandidate = pNodeCur;
            }
        }
    }

    return pNodeCandidate;
}

EHRangeTreeNode *EHRangeTree::FindMostSpecificContainer(DWORD addr)
{
    WRAPPER_NO_CONTRACT;

    EHRangeTreeNode node(addr);
    return FindContainer(&node);
}

EHRangeTreeNode *EHRangeTree::FindNextMostSpecificContainer(EHRangeTreeNode *pNodeSearch, DWORD addr)
{
    WRAPPER_NO_CONTRACT;

    _ASSERTE(!m_fInitializing);

    EHRangeTreeNode **rgpNodes = pNodeSearch->m_containees.Table();

    if (NULL == rgpNodes)
        return pNodeSearch;

    // It's possible that no subrange contains the desired address, so
    // keep a reasonable default around.
    EHRangeTreeNode *pNodeCandidate = pNodeSearch;

    USHORT cSubRanges = pNodeSearch->m_containees.Count();
    EHRangeTreeNode **ppNodeCur = pNodeSearch->m_containees.Table();

    for (int i = 0; i < cSubRanges; i++, ppNodeCur++)
    {
        if ((*ppNodeCur)->Contains(addr) &&
            pNodeCandidate->Contains((*ppNodeCur)))
        {
            pNodeCandidate = (*ppNodeCur);
        }
    }

    return pNodeCandidate;
}

BOOL EHRangeTree::isAtStartOfCatch(DWORD offset)
{
    LIMITED_METHOD_CONTRACT;

    if (NULL != m_rgNodes && m_EHCount != 0)
    {
        for(unsigned i = 0; i < m_EHCount;i++)
        {
            if (m_rgNodes[i].m_clause->HandlerStartPC == offset &&
                (!IsFilterHandler(m_rgNodes[i].m_clause) && !IsFaultOrFinally(m_rgNodes[i].m_clause)))
                return TRUE;
        }
    }

    return FALSE;
}

enum TRY_CATCH_FINALLY
{
    TCF_NONE= 0,
    TCF_TRY,
    TCF_FILTER,
    TCF_CATCH,
    TCF_FINALLY,
    TCF_COUNT, //count of all elements, not an element itself
};

#ifdef LOGGING
const char *TCFStringFromConst(TRY_CATCH_FINALLY tcf)
{
    LIMITED_METHOD_CONTRACT;

    switch( tcf )
    {
        case TCF_NONE:
            return "TCFS_NONE";
            break;
        case TCF_TRY:
            return "TCFS_TRY";
            break;
        case TCF_FILTER:
            return "TCF_FILTER";
            break;
        case TCF_CATCH:
            return "TCFS_CATCH";
            break;
        case TCF_FINALLY:
            return "TCFS_FINALLY";
            break;
        case TCF_COUNT:
            return "TCFS_COUNT";
            break;
        default:
            return "INVALID TCFS VALUE";
            break;
    }
}
#endif //LOGGING

#ifndef FEATURE_EH_FUNCLETS
// We're unwinding if we'll return to the EE's code.  Otherwise
// we'll return to someplace in the current code.  Anywhere outside
// this function is "EE code".
bool FinallyIsUnwinding(EHRangeTreeNode *pNode,
                        ICodeManager* pEECM,
                        PREGDISPLAY pReg,
                        SLOT addrStart)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        FORBID_FAULT;
    }
    CONTRACTL_END;

    const BYTE *pbRetAddr = pEECM->GetFinallyReturnAddr(pReg);

    if (pbRetAddr < (const BYTE *)addrStart)
        return true;

    DWORD offset = (DWORD)(size_t)(pbRetAddr - addrStart);
    EHRangeTreeNode *pRoot = pNode->m_pTree->m_root;

    if (!pRoot->Contains(offset))
        return true;
    else
        return false;
}

BOOL LeaveCatch(ICodeManager* pEECM,
                Thread *pThread,
                CONTEXT *pCtx,
                GCInfoToken gcInfoToken,
                unsigned offset)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    // We can assert these things here, and skip a call
    // to COMPlusCheckForAbort later.

            // If no abort has been requested,
    _ASSERTE((pThread->GetThrowable() != NULL) ||
            // or if there is a pending exception.
            (!pThread->IsAbortRequested()) );

    LPVOID esp = COMPlusEndCatchWorker(pThread);

    PopNestedExceptionRecords(esp, pCtx, pThread->GetExceptionListPtr());

    // Do JIT-specific work
    pEECM->LeaveCatch(gcInfoToken, offset, pCtx);

    SetSP(pCtx, (UINT_PTR)esp);
    return TRUE;
}
#endif // FEATURE_EH_FUNCLETS

TRY_CATCH_FINALLY GetTcf(EHRangeTreeNode *pNode,
                         unsigned offset)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        FORBID_FAULT;
    }
    CONTRACTL_END;

    _ASSERTE(pNode->IsRange() && !pNode->IsRoot());

    TRY_CATCH_FINALLY tcf;

    if (!pNode->Contains(offset))
    {
        tcf = TCF_NONE;
    }
    else if (pNode->TryContains(offset))
    {
        tcf = TCF_TRY;
    }
    else if (pNode->FilterContains(offset))
    {
        tcf = TCF_FILTER;
    }
    else
    {
        _ASSERTE(pNode->HandlerContains(offset));
        if (IsFaultOrFinally(pNode->m_clause))
            tcf = TCF_FINALLY;
        else
            tcf = TCF_CATCH;
    }

    return tcf;
}

const DWORD bEnter = 0x01;
const DWORD bLeave = 0x02;

HRESULT IsLegalTransition(Thread *pThread,
                          bool fCanSetIPOnly,
                          DWORD fEnter,
                          EHRangeTreeNode *pNode,
                          DWORD offFrom,
                          DWORD offTo,
                          ICodeManager* pEECM,
                          PREGDISPLAY pReg,
                          SLOT addrStart,
                          GCInfoToken gcInfoToken,
                          PCONTEXT pCtx)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

#ifdef _DEBUG
    if (fEnter & bEnter)
    {
        _ASSERTE(pNode->Contains(offTo));
    }
    if (fEnter & bLeave)
    {
        _ASSERTE(pNode->Contains(offFrom));
    }
#endif //_DEBUG

    // First, figure out where we're coming from/going to
    TRY_CATCH_FINALLY tcfFrom = GetTcf(pNode,
                                       offFrom);

    TRY_CATCH_FINALLY tcfTo =  GetTcf(pNode,
                                      offTo);

    LOG((LF_CORDB, LL_INFO10000, "ILT: from %s to %s\n",
        TCFStringFromConst(tcfFrom),
        TCFStringFromConst(tcfTo)));

    // Now we'll consider, case-by-case, the various permutations that
    // can arise
    switch(tcfFrom)
    {
        case TCF_NONE:
        case TCF_TRY:
        {
            switch(tcfTo)
            {
                case TCF_NONE:
                case TCF_TRY:
                {
                    return S_OK;
                    break;
                }

                case TCF_FILTER:
                {
                    return CORDBG_E_CANT_SETIP_INTO_OR_OUT_OF_FILTER;
                    break;
                }

                case TCF_CATCH:
                {
                    return CORDBG_E_CANT_SET_IP_INTO_CATCH;
                    break;
                }

                case TCF_FINALLY:
                {
                    return CORDBG_E_CANT_SET_IP_INTO_FINALLY;
                    break;
                }
                default:
                    break;
            }
            break;
        }

        case TCF_FILTER:
        {
            switch(tcfTo)
            {
                case TCF_NONE:
                case TCF_TRY:
                case TCF_CATCH:
                case TCF_FINALLY:
                {
                    return CORDBG_E_CANT_SETIP_INTO_OR_OUT_OF_FILTER;
                    break;
                }
                case TCF_FILTER:
                {
                    return S_OK;
                    break;
                }
                default:
                    break;

            }
            break;
        }

        case TCF_CATCH:
        {
            switch(tcfTo)
            {
                case TCF_NONE:
                case TCF_TRY:
                {
#if !defined(FEATURE_EH_FUNCLETS)
                    CONTEXT *pFilterCtx = pThread->GetFilterContext();
                    if (pFilterCtx == NULL)
                        return CORDBG_E_SET_IP_IMPOSSIBLE;

                    if (!fCanSetIPOnly)
                    {
                        if (!LeaveCatch(pEECM,
                                        pThread,
                                        pFilterCtx,
                                        gcInfoToken,
                                        offFrom))
                            return E_FAIL;
                    }
                    return S_OK;
#else  // FEATURE_EH_FUNCLETS
                    // <NOTE>
                    // Setting IP out of a catch clause is not supported for FEATURE_EH_FUNCLETS because of funclets.
                    // This scenario is disabled with approval from VS because it's not considered to
                    // be a common user scenario.
                    // </NOTE>
                    return CORDBG_E_CANT_SET_IP_OUT_OF_CATCH_ON_WIN64;
#endif // !FEATURE_EH_FUNCLETS
                    break;
                }

                case TCF_FILTER:
                {
                    return CORDBG_E_CANT_SETIP_INTO_OR_OUT_OF_FILTER;
                    break;
                }

                case TCF_CATCH:
                {
                    return S_OK;
                    break;
                }

                case TCF_FINALLY:
                {
                    return CORDBG_E_CANT_SET_IP_INTO_FINALLY;
                    break;
                }
                default:
                    break;
            }
            break;
        }

        case TCF_FINALLY:
        {
            switch(tcfTo)
            {
                case TCF_NONE:
                case TCF_TRY:
                {
#ifndef FEATURE_EH_FUNCLETS
                    if (!FinallyIsUnwinding(pNode, pEECM, pReg, addrStart))
                    {
                        CONTEXT *pFilterCtx = pThread->GetFilterContext();
                        if (pFilterCtx == NULL)
                            return CORDBG_E_SET_IP_IMPOSSIBLE;

                        if (!fCanSetIPOnly)
                        {
                            if (!pEECM->LeaveFinally(gcInfoToken,
                                                     offFrom,
                                                     pFilterCtx))
                                return E_FAIL;
                        }
                        return S_OK;
                    }
                    else
                    {
                        return CORDBG_E_CANT_SET_IP_OUT_OF_FINALLY;
                    }
#else // !FEATURE_EH_FUNCLETS
                    // <NOTE>
                    // Setting IP out of a non-unwinding finally clause is not supported on FEATURE_EH_FUNCLETS because of funclets.
                    // This scenario is disabled with approval from VS because it's not considered to be a common user
                    // scenario.
                    // </NOTE>
                    return CORDBG_E_CANT_SET_IP_OUT_OF_FINALLY_ON_WIN64;
#endif // FEATURE_EH_FUNCLETS

                    break;
                }

                case TCF_FILTER:
                {
                    return CORDBG_E_CANT_SETIP_INTO_OR_OUT_OF_FILTER;
                    break;
                }

                case TCF_CATCH:
                {
                    return CORDBG_E_CANT_SET_IP_INTO_CATCH;
                    break;
                }

                case TCF_FINALLY:
                {
                    return S_OK;
                    break;
                }
                default:
                    break;
            }
            break;
        }
       break;
       default:
        break;
    }

    _ASSERTE( !"IsLegalTransition: We should never reach this point!" );

    return CORDBG_E_SET_IP_IMPOSSIBLE;
}

// We need this to determine what
// to do based on whether the stack in general is empty
HRESULT DestinationIsValid(void *pDjiToken,
                           DWORD offTo,
                           EHRangeTree *pEHRT)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        FORBID_FAULT;
    }
    CONTRACTL_END;

    // We'll add a call to the DebugInterface that takes this
    // & tells us if the destination is a stack empty point.
//    DebuggerJitInfo *pDji = (DebuggerJitInfo *)pDjiToken;

    if (pEHRT->isAtStartOfCatch(offTo))
        return CORDBG_S_BAD_START_SEQUENCE_POINT;
    else
        return S_OK;
} // HRESULT DestinationIsValid()

// We want to keep the 'worst' HRESULT - if one has failed (..._E_...) & the
// other hasn't, take the failing one.  If they've both/neither failed, then
// it doesn't matter which we take.
// Note that this macro favors retaining the first argument
#define WORST_HR(hr1,hr2) (FAILED(hr1)?hr1:hr2)
HRESULT SetIPFromSrcToDst(Thread *pThread,
                          SLOT addrStart,       // base address of method
                          DWORD offFrom,        // native offset
                          DWORD offTo,          // native offset
                          bool fCanSetIPOnly,   // if true, don't do any real work
                          PREGDISPLAY pReg,
                          PCONTEXT pCtx,
                          void *pDji,
                          EHRangeTree *pEHRT)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        INJECT_FAULT(return E_OUTOFMEMORY;);
    }
    CONTRACTL_END;

    HRESULT         hr = S_OK;
    HRESULT         hrReturn = S_OK;
    bool            fCheckOnly = true;

    EECodeInfo codeInfo((TADDR)(addrStart));

    ICodeManager * pEECM = codeInfo.GetCodeManager();
    GCInfoToken gcInfoToken = codeInfo.GetGCInfoToken();

    // Do both checks here so compiler doesn't complain about skipping
    // initialization b/c of goto.
    if (fCanSetIPOnly && !pEECM->IsGcSafe(&codeInfo, offFrom))
    {
        hrReturn = WORST_HR(hrReturn, CORDBG_E_SET_IP_IMPOSSIBLE);
    }

    if (fCanSetIPOnly && !pEECM->IsGcSafe(&codeInfo, offTo))
    {
        hrReturn = WORST_HR(hrReturn, CORDBG_E_SET_IP_IMPOSSIBLE);
    }

    if ((hr = DestinationIsValid(pDji, offTo, pEHRT)) != S_OK
        && fCanSetIPOnly)
    {
        hrReturn = WORST_HR(hrReturn,hr);
    }

    // The basic approach is this:  We'll start with the most specific (smallest)
    // EHClause that contains the starting address.  We'll 'back out', to larger
    // and larger ranges, until we either find an EHClause that contains both
    // the from and to addresses, or until we reach the root EHRangeTreeNode,
    // which contains all addresses within it.  At each step, we check/do work
    // that the various transitions (from inside to outside a catch, etc).
    // At that point, we do the reverse process  - we go from the EHClause that
    // encompasses both from and to, and narrow down to the smallest EHClause that
    // encompasses the to point.  We use our nifty data structure to manage
    // the tree structure inherent in this process.
    //
    // NOTE:  We do this process twice, once to check that we're not doing an
    //        overall illegal transition, such as ultimately set the IP into
    //        a catch, which is never allowed.  We're doing this because VS
    //        calls SetIP without calling CanSetIP first, and so we should be able
    //        to return an error code and have the stack in the same condition
    //        as the start of the call, and so we shouldn't back out of clauses
    //        or move into them until we're sure that can be done.

retryForCommit:

    EHRangeTreeNode *node;
    EHRangeTreeNode *nodeNext;
    node = pEHRT->FindMostSpecificContainer(offFrom);

    while (!node->Contains(offTo))
    {
        hr = IsLegalTransition(pThread,
                               fCheckOnly,
                               bLeave,
                               node,
                               offFrom,
                               offTo,
                               pEECM,
                               pReg,
                               addrStart,
                               gcInfoToken,
                               pCtx);

        if (FAILED(hr))
        {
            hrReturn = WORST_HR(hrReturn,hr);
        }

        node = node->GetContainer();
        // m_root prevents node from ever being NULL.
    }

    if (node != pEHRT->m_root)
    {
        hr = IsLegalTransition(pThread,
                               fCheckOnly,
                               bEnter|bLeave,
                               node,
                               offFrom,
                               offTo,
                               pEECM,
                               pReg,
                               addrStart,
                               gcInfoToken,
                               pCtx);

        if (FAILED(hr))
        {
            hrReturn = WORST_HR(hrReturn,hr);
        }
    }

    nodeNext = pEHRT->FindNextMostSpecificContainer(node,
                                                    offTo);

    while(nodeNext != node)
    {
        hr = IsLegalTransition(pThread,
                               fCheckOnly,
                               bEnter,
                               nodeNext,
                               offFrom,
                               offTo,
                               pEECM,
                               pReg,
                               addrStart,
                               gcInfoToken,
                               pCtx);

        if (FAILED(hr))
        {
            hrReturn = WORST_HR(hrReturn, hr);
        }

        node = nodeNext;
        nodeNext = pEHRT->FindNextMostSpecificContainer(node,
                                                        offTo);
    }

    // If it was the intention to actually set the IP and the above transition checks succeeded,
    // then go back and do it all again but this time widen and narrow the thread's actual scope
    if (!fCanSetIPOnly && fCheckOnly && SUCCEEDED(hrReturn))
    {
        fCheckOnly = false;
        goto retryForCommit;
    }

    return hrReturn;
} // HRESULT SetIPFromSrcToDst()

// This function should only be called if the thread is suspended and sitting in jitted code
BOOL IsInFirstFrameOfHandler(Thread *pThread, IJitManager *pJitManager, const METHODTOKEN& MethodToken, DWORD offset)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        FORBID_FAULT;
    }
    CONTRACTL_END;

    // if don't have a throwable the aren't processing an exception
    if (IsHandleNullUnchecked(pThread->GetThrowableAsHandle()))
        return FALSE;

    EH_CLAUSE_ENUMERATOR pEnumState;
    unsigned EHCount = pJitManager->InitializeEHEnumeration(MethodToken, &pEnumState);

    for(ULONG i=0; i < EHCount; i++)
    {
        EE_ILEXCEPTION_CLAUSE EHClause;
        pJitManager->GetNextEHClause(&pEnumState, &EHClause);
        _ASSERTE(IsValidClause(&EHClause));

        if ( offset >= EHClause.HandlerStartPC && offset < EHClause.HandlerEndPC)
            return TRUE;

        // check if it's in the filter itself if we're not in the handler
        if (IsFilterHandler(&EHClause) && offset >= EHClause.FilterOffset && offset < EHClause.HandlerStartPC)
            return TRUE;
    }
    return FALSE;
} // BOOL IsInFirstFrameOfHandler()


#if !defined(FEATURE_EH_FUNCLETS)

//******************************************************************************
// LookForHandler -- search for a function that will handle the exception.
//******************************************************************************
LFH LookForHandler(                         // LFH return types
    const EXCEPTION_POINTERS *pExceptionPointers, // The ExceptionRecord and ExceptionContext
    Thread      *pThread,                   // Thread on which to look (always current?)
    ThrowCallbackType *tct)                 // Structure to pass back to callback functions.
{
    // We don't want to use a runtime contract here since this codepath is used during
    // the processing of a hard SO. Contracts use a significant amount of stack
    // which we can't afford for those cases.
    STATIC_CONTRACT_THROWS;
    STATIC_CONTRACT_GC_TRIGGERS;
    STATIC_CONTRACT_MODE_COOPERATIVE;

    // go through to find if anyone handles the exception
    StackWalkAction action = pThread->StackWalkFrames((PSTACKWALKFRAMESCALLBACK)COMPlusThrowCallback,
                                 tct,
                                 0,     //can't use FUNCTIONSONLY because the callback uses non-function frames to stop the walk
                                 tct->pBottomFrame);

    // If someone handles it, the action will be SWA_ABORT with pFunc and dHandler indicating the
        // function and handler that is handling the exception. Debugger can put a hook in here.
    if (action == SWA_ABORT && tct->pFunc != NULL)
        return LFH_FOUND;

    // nobody is handling it
    return LFH_NOT_FOUND;
} // LFH LookForHandler()

StackWalkAction COMPlusUnwindCallback (CrawlFrame *pCf, ThrowCallbackType *pData);

//******************************************************************************
//  UnwindFrames
//******************************************************************************
void UnwindFrames(                      // No return value.
    Thread      *pThread,               // Thread to unwind.
    ThrowCallbackType *tct)             // Structure to pass back to callback function.
{
    STATIC_CONTRACT_THROWS;
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_MODE_COOPERATIVE;

    if (pThread->IsExceptionInProgress())
    {
        pThread->GetExceptionState()->GetFlags()->SetUnwindHasStarted();
    }

    #ifdef DEBUGGING_SUPPORTED
        //
        // If a debugger is attached, notify it that unwinding is going on.
        //
        if (CORDebuggerAttached())
        {
            g_pDebugInterface->ManagedExceptionUnwindBegin(pThread);
        }
    #endif // DEBUGGING_SUPPORTED

    LOG((LF_EH, LL_INFO1000, "UnwindFrames: going to: pFunc:%#X, pStack:%#X\n",
        tct->pFunc, tct->pStack));

    pThread->StackWalkFrames((PSTACKWALKFRAMESCALLBACK)COMPlusUnwindCallback,
                             tct,
                             POPFRAMES,
                             tct->pBottomFrame);
} // void UnwindFrames()

#endif // !defined(FEATURE_EH_FUNCLETS)

void StackTraceInfo::SaveStackTrace(BOOL bAllowAllocMem, OBJECTHANDLE hThrowable, BOOL bReplaceStack, BOOL bSkipLastElement)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;

    // Do not save stacktrace to preallocated exception.  These are shared.
    if (CLRException::IsPreallocatedExceptionHandle(hThrowable))
    {
        // Preallocated exceptions will never have this flag set. However, its possible
        // that after this flag is set for a regular exception but before we throw, we have an async
        // exception like a RudeThreadAbort, which will replace the exception
        // containing the restored stack trace.
        //
        // In such a case, we should clear the flag as the throwable representing the
        // preallocated exception will not have the restored (or any) stack trace.
        PTR_ThreadExceptionState pCurTES = GetThread()->GetExceptionState();
        pCurTES->ResetRaisingForeignException();

        return;
    }

    LOG((LF_EH, LL_INFO1000, "StackTraceInfo::SaveStackTrace (%p), alloc = %d, replace = %d, skiplast = %d\n", this, bAllowAllocMem, bReplaceStack, bSkipLastElement));

    // if have bSkipLastElement, must also keep the stack
    _ASSERTE(! bSkipLastElement || ! bReplaceStack);

    bool         fSuccess = false;
    MethodTable* pMT      = ObjectFromHandle(hThrowable)->GetMethodTable();

    // Check if the flag indicating foreign exception raise has been setup or not,
    // and then reset it so that subsequent processing of managed frames proceeds
    // normally.
    PTR_ThreadExceptionState pCurTES = GetThread()->GetExceptionState();
    BOOL fRaisingForeignException = pCurTES->IsRaisingForeignException();
    pCurTES->ResetRaisingForeignException();

    if (bAllowAllocMem && m_dFrameCount != 0)
    {
        EX_TRY
        {
            // Only save stack trace info on exceptions
            _ASSERTE(IsException(pMT));     // what is the pathway here?
            if (!IsException(pMT))
            {
                fSuccess = true;
            }
            else
            {
                // If the stack trace contains DynamicMethodDescs, we need to save the corrosponding
                // System.Resolver objects in the Exception._dynamicMethods field. Failing to do that
                // will cause an AV in the runtime when we try to visit those MethodDescs in the
                // Exception._stackTrace field, because they have been recycled or destroyed.
                unsigned    iNumDynamics      = 0;

                // How many DynamicMethodDescs do we need to keep alive?
                for (unsigned iElement=0; iElement < m_dFrameCount; iElement++)
                {
                    MethodDesc *pMethod = m_pStackTrace[iElement].pFunc;
                    _ASSERTE(pMethod);

                    if (pMethod->IsLCGMethod())
                    {
                        // Increment the number of new dynamic methods we have found
                        iNumDynamics++;
                    }
                    else
                    if (pMethod->GetMethodTable()->Collectible())
                    {
                        iNumDynamics++;
                    }
                }

                struct _gc
                {
                    StackTraceArray stackTrace;
                    StackTraceArray stackTraceTemp;
                    PTRARRAYREF dynamicMethodsArrayTemp;
                    PTRARRAYREF dynamicMethodsArray; // Object array of Managed Resolvers
                    PTRARRAYREF pOrigDynamicArray;

                    _gc()
                        : stackTrace()
                        , stackTraceTemp()
                        , dynamicMethodsArrayTemp(static_cast<PTRArray *>(NULL))
                        , dynamicMethodsArray(static_cast<PTRArray *>(NULL))
                        , pOrigDynamicArray(static_cast<PTRArray *>(NULL))
                    {}
                };

                _gc gc;
                GCPROTECT_BEGIN(gc);

                // If the flag indicating foreign exception raise has been setup, then check
                // if the exception object has stacktrace or not. If we have an async non-preallocated
                // exception after setting this flag but before we throw, then the new
                // exception will not have any stack trace set and thus, we should behave as if
                // the flag was not setup.
                if (fRaisingForeignException)
                {
                    // Get the reference to stack trace and reset our flag if applicable.
                    ((EXCEPTIONREF)ObjectFromHandle(hThrowable))->GetStackTrace(gc.stackTraceTemp);
                    if (gc.stackTraceTemp.Size() == 0)
                    {
                        fRaisingForeignException = FALSE;
                    }
                }

                // Replace stack (i.e. build a new stack trace) only if we are not raising a foreign exception.
                // If we are, then we will continue to extend the existing stack trace.
                if (bReplaceStack
                    && (!fRaisingForeignException)
                    )
                {
                    // Cleanup previous info
                    gc.stackTrace.Append(m_pStackTrace, m_pStackTrace + m_dFrameCount);

                    if (iNumDynamics)
                    {
                        // Adjust the allocation size of the array, if required
                        if (iNumDynamics > m_cDynamicMethodItems)
                        {
                            S_UINT32 cNewSize = S_UINT32(2) * S_UINT32(iNumDynamics);
                            if (cNewSize.IsOverflow())
                            {
                                // Overflow here implies we cannot allocate memory anymore
                                LOG((LF_EH, LL_INFO100, "StackTraceInfo::SaveStackTrace - Cannot calculate initial resolver array size due to overflow!\n"));
                                COMPlusThrowOM();
                            }

                            m_cDynamicMethodItems = cNewSize.Value();
                        }

                        gc.dynamicMethodsArray = (PTRARRAYREF)AllocateObjectArray(m_cDynamicMethodItems, g_pObjectClass);
                        LOG((LF_EH, LL_INFO100, "StackTraceInfo::SaveStackTrace - allocated dynamic array for first frame of size %lu\n",
                            m_cDynamicMethodItems));
                    }

                    m_dCurrentDynamicIndex = 0;
                }
                else
                {
                    // Fetch the stacktrace and the dynamic method array
                    ((EXCEPTIONREF)ObjectFromHandle(hThrowable))->GetStackTrace(gc.stackTrace, &gc.pOrigDynamicArray);

                    if (fRaisingForeignException)
                    {
                        // Just before we append to the stack trace, mark the last recorded frame to be from
                        // the foreign thread so that we can insert an annotation indicating so when building
                        // the stack trace string.
                        size_t numCurrentFrames = gc.stackTrace.Size();
                        if (numCurrentFrames > 0)
                        {
                            // "numCurrentFrames" can be zero if the user created an EDI using
                            // an unthrown exception.
                            StackTraceElement & refLastElementFromForeignStackTrace = gc.stackTrace[numCurrentFrames - 1];
                            refLastElementFromForeignStackTrace.flags |= STEF_LAST_FRAME_FROM_FOREIGN_STACK_TRACE;
                        }
                    }

                    if (!bSkipLastElement)
                        gc.stackTrace.Append(m_pStackTrace, m_pStackTrace + m_dFrameCount);

                    //////////////////////////////

                    unsigned   cOrigDynamic = 0;    // number of objects in the old array
                    if (gc.pOrigDynamicArray != NULL)
                    {
                        cOrigDynamic = gc.pOrigDynamicArray->GetNumComponents();
                    }
                    else
                    {
                        // Since there is no dynamic method array, reset the corresponding state variables
                        m_dCurrentDynamicIndex = 0;
                        m_cDynamicMethodItems = 0;
                    }

                    if ((gc.pOrigDynamicArray != NULL)
                    || (fRaisingForeignException)
                    )
                    {
                        // Since we have just restored the dynamic method array as well,
                        // calculate the dynamic array index which would be the total
                        // number of dynamic methods present in the stack trace.
                        //
                        // In addition to the ForeignException scenario, we need to reset these
                        // values incase the exception object in question is being thrown by
                        // multiple threads in parallel and thus, could have potentially different
                        // dynamic method array contents/size as opposed to the current state of
                        // StackTraceInfo.

                        unsigned iStackTraceElements = (unsigned)gc.stackTrace.Size();
                        m_dCurrentDynamicIndex = 0;
                        for (unsigned iIndex = 0; iIndex < iStackTraceElements; iIndex++)
                        {
                            MethodDesc *pMethod = gc.stackTrace[iIndex].pFunc;
                            if (pMethod)
                            {
                                if ((pMethod->IsLCGMethod()) || (pMethod->GetMethodTable()->Collectible()))
                                {
                                    // Increment the number of new dynamic methods we have found
                                    m_dCurrentDynamicIndex++;
                                }
                            }
                        }

                        // Total number of elements in the dynamic method array should also be
                        // reset based upon the restored array size.
                        m_cDynamicMethodItems = cOrigDynamic;
                    }

                    // Make the dynamic Array field reference the original array we got from the
                    // Exception object. If, below, we have to add new entries, we will add it to the
                    // array if it is allocated, or else, we will allocate it before doing so.
                    gc.dynamicMethodsArray = gc.pOrigDynamicArray;

                    // Create an object array if we have new dynamic method entries AND
                    // if we are at the (or went past) the current size limit
                    if (iNumDynamics > 0)
                    {
                        // Reallocate the array if we are at the (or went past) the current size limit
                        unsigned cTotalDynamicMethodCount = m_dCurrentDynamicIndex;

                        S_UINT32 cNewSum = S_UINT32(cTotalDynamicMethodCount) + S_UINT32(iNumDynamics);
                        if (cNewSum.IsOverflow())
                        {
                            // If the current size is already the UINT32 max size, then we
                            // cannot go further. Overflow here implies we cannot allocate memory anymore.
                            LOG((LF_EH, LL_INFO100, "StackTraceInfo::SaveStackTrace - Cannot calculate resolver array size due to overflow!\n"));
                            COMPlusThrowOM();
                        }

                        cTotalDynamicMethodCount = cNewSum.Value();

                        if (cTotalDynamicMethodCount > m_cDynamicMethodItems)
                        {
                            // Double the current limit of the array.
                            S_UINT32 cNewSize = S_UINT32(2) * S_UINT32(cTotalDynamicMethodCount);
                            if (cNewSize.IsOverflow())
                            {
                                // Overflow here implies that we cannot allocate any more memory
                                LOG((LF_EH, LL_INFO100, "StackTraceInfo::SaveStackTrace - Cannot resize resolver array beyond max size due to overflow!\n"));
                                COMPlusThrowOM();
                            }

                            m_cDynamicMethodItems = cNewSize.Value();
                            gc.dynamicMethodsArray = (PTRARRAYREF)AllocateObjectArray(m_cDynamicMethodItems,
                                                                                      g_pObjectClass);

                            _ASSERTE(!(cOrigDynamic && !gc.pOrigDynamicArray));

                            LOG((LF_EH, LL_INFO100, "StackTraceInfo::SaveStackTrace - resized dynamic array to size %lu\n",
                            m_cDynamicMethodItems));

                            // Copy previous entries if there are any, and update iCurDynamic to point
                            // to the following index.
                            if (cOrigDynamic && (gc.pOrigDynamicArray != NULL))
                            {
                                memmoveGCRefs(gc.dynamicMethodsArray->GetDataPtr(),
                                              gc.pOrigDynamicArray->GetDataPtr(),
                                              cOrigDynamic * sizeof(Object *));

                                // m_dCurrentDynamicIndex is already referring to the correct index
                                // at which the next resolver object will be saved
                            }
                        }
                        else
                        {
                            // We are adding objects to the existing array.
                            //
                            // We have new dynamic method entries for which
                            // resolver objects need to be saved. Ensure
                            // that we have the array to store them
                            if (gc.dynamicMethodsArray == NULL)
                            {
                                _ASSERTE(m_cDynamicMethodItems > 0);

                                gc.dynamicMethodsArray = (PTRARRAYREF)AllocateObjectArray(m_cDynamicMethodItems,
                                                                                          g_pObjectClass);
                                m_dCurrentDynamicIndex = 0;
                                LOG((LF_EH, LL_INFO100, "StackTraceInfo::SaveStackTrace - allocated dynamic array of size %lu\n",
                                    m_cDynamicMethodItems));
                            }
                            else
                            {
                                // The array exists for storing resolver objects.
                                // Simply set the index at which the next resolver
                                // will be stored in it.
                            }
                        }
                    }
                }

                // Update _dynamicMethods field
                if (iNumDynamics)
                {
                    // At this point, we should be having a valid array for storage
                    _ASSERTE(gc.dynamicMethodsArray != NULL);

                    // Assert that we are in valid range of the array in which resolver objects will be saved.
                    // We subtract 1 below since storage will start from m_dCurrentDynamicIndex onwards and not
                    // from (m_dCurrentDynamicIndex + 1).
                    _ASSERTE((m_dCurrentDynamicIndex + iNumDynamics - 1) < gc.dynamicMethodsArray->GetNumComponents());

                    for (unsigned i=0; i < m_dFrameCount; i++)
                    {
                        MethodDesc *pMethod = m_pStackTrace[i].pFunc;
                        _ASSERTE(pMethod);

                        if (pMethod->IsLCGMethod())
                        {
                            // We need to append the corresponding System.Resolver for
                            // this DynamicMethodDesc to keep it alive.
                            DynamicMethodDesc *pDMD = (DynamicMethodDesc *) pMethod;
                            OBJECTREF pResolver = pDMD->GetLCGMethodResolver()->GetManagedResolver();

                            _ASSERTE(pResolver != NULL);

                            // Store Resolver information in the array
                            gc.dynamicMethodsArray->SetAt(m_dCurrentDynamicIndex++, pResolver);
                        }
                        else
                        if (pMethod->GetMethodTable()->Collectible())
                        {
                            OBJECTREF pLoaderAllocator = pMethod->GetMethodTable()->GetLoaderAllocator()->GetExposedObject();
                            _ASSERTE(pLoaderAllocator != NULL);
                            gc.dynamicMethodsArray->SetAt (m_dCurrentDynamicIndex++, pLoaderAllocator);
                        }
                    }
                }

                ((EXCEPTIONREF)ObjectFromHandle(hThrowable))->SetStackTrace(gc.stackTrace.Get(), gc.dynamicMethodsArray);

                // Update _stackTraceString field.
                ((EXCEPTIONREF)ObjectFromHandle(hThrowable))->SetStackTraceString(NULL);
                fSuccess = true;

                GCPROTECT_END();    // gc
            }
        }
        EX_CATCH
        {
        }
        EX_END_CATCH(SwallowAllExceptions)
    }

    ClearStackTrace();

    if (!fSuccess)
    {
        EX_TRY
        {
            _ASSERTE(IsException(pMT));         // what is the pathway here?
            if (bReplaceStack && IsException(pMT))
                ((EXCEPTIONREF)ObjectFromHandle(hThrowable))->ClearStackTraceForThrow();
        }
        EX_CATCH
        {
            // Do nothing
        }
        EX_END_CATCH(SwallowAllExceptions);
    }
}

// Copy a context record, being careful about whether or not the target
// is large enough to support CONTEXT_EXTENDED_REGISTERS.
//
// NOTE: this function can ONLY be used when a filter function will return
// EXCEPTION_CONTINUE_EXECUTION.  On AMD64, replacing the CONTEXT in any other
// situation may break exception unwinding.
//
// NOTE: this function MUST be used on AMD64.  During exception handling,
// parts of the CONTEXT struct must not be modified.


// High 2 bytes are machine type.  Low 2 bytes are register subset.
#define CONTEXT_EXTENDED_BIT (CONTEXT_EXTENDED_REGISTERS & 0xffff)

VOID
ReplaceExceptionContextRecord(CONTEXT *pTarget, CONTEXT *pSource)
{
    LIMITED_METHOD_CONTRACT;

    _ASSERTE(pTarget);
    _ASSERTE(pSource);

#if defined(TARGET_X86)
    //<TODO>
    // @TODO IA64: CONTEXT_DEBUG_REGISTERS not defined on IA64, may need updated SDK
    //</TODO>

    // Want CONTROL, INTEGER, SEGMENTS.  If we have Floating Point, fine.
    _ASSERTE((pSource->ContextFlags & CONTEXT_FULL) == CONTEXT_FULL);
#endif // TARGET_X86

#ifdef CONTEXT_EXTENDED_REGISTERS

    if (pSource->ContextFlags & CONTEXT_EXTENDED_BIT)
    {
        if (pTarget->ContextFlags & CONTEXT_EXTENDED_BIT)
        {   // Source and Target have EXTENDED bit set.
            *pTarget = *pSource;
        }
        else
        {   // Source has but Target doesn't have EXTENDED bit set.  (Target is shorter than Source.)
            //  Copy non-extended part of the struct, and reset the bit on the Target, as it was.
            memcpy(pTarget, pSource, offsetof(CONTEXT, ExtendedRegisters));
            pTarget->ContextFlags &= ~CONTEXT_EXTENDED_BIT;  // Target was short.  Reset the extended bit.
        }
    }
    else
    {   // Source does not have EXTENDED bit.  Copy only non-extended part of the struct.
        memcpy(pTarget, pSource, offsetof(CONTEXT, ExtendedRegisters));
    }
    STRESS_LOG3(LF_SYNC, LL_INFO1000, "ReSet thread context EIP = %p ESP = %p EBP = %p\n",
        GetIP((CONTEXT*)pTarget), GetSP((CONTEXT*)pTarget), GetFP((CONTEXT*)pTarget));

#else // !CONTEXT_EXTENDED_REGISTERS

    // Everything that's left
    *pTarget = *pSource;

#endif // !CONTEXT_EXTENDED_REGISTERS
}

VOID FixupOnRethrow(Thread* pCurThread, EXCEPTION_POINTERS* pExceptionPointers)
{
    WRAPPER_NO_CONTRACT;

    ThreadExceptionState* pExState = pCurThread->GetExceptionState();

#ifdef FEATURE_INTERPRETER
    // Abort if we don't have any state from the original exception.
    if (!pExState->IsExceptionInProgress())
    {
        return;
    }
#endif // FEATURE_INTERPRETER

    // Don't allow rethrow of a STATUS_STACK_OVERFLOW -- it's a new throw of the COM+ exception.
    if (pExState->GetExceptionCode() == STATUS_STACK_OVERFLOW)
    {
        return;
    }

    // For COMPLUS exceptions, we don't need the original context for our rethrow.
    if (!(pExState->IsComPlusException()))
    {
        _ASSERTE(pExState->GetExceptionRecord());

        // don't copy parm args as have already supplied them on the throw
        memcpy((void*)pExceptionPointers->ExceptionRecord,
               (void*)pExState->GetExceptionRecord(),
               offsetof(EXCEPTION_RECORD, ExceptionInformation));

// Replacing the exception context breaks unwinding on AMD64.  It also breaks exception dispatch on IA64.
// The info saved by pExState will be given to exception filters.
#ifndef FEATURE_EH_FUNCLETS
        // Restore original context if available.
        if (pExState->GetContextRecord())
        {
            ReplaceExceptionContextRecord(pExceptionPointers->ContextRecord,
                                          pExState->GetContextRecord());
        }
#endif // !FEATURE_EH_FUNCLETS
    }

    pExState->GetFlags()->SetIsRethrown();
}

struct RaiseExceptionFilterParam
{
    BOOL isRethrown;
};

LONG RaiseExceptionFilter(EXCEPTION_POINTERS* ep, LPVOID pv)
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_MODE_ANY;

    RaiseExceptionFilterParam *pParam = (RaiseExceptionFilterParam *) pv;

    if (1 == pParam->isRethrown)
    {
        // need to reset the EH info back to the original thrown exception
        FixupOnRethrow(GetThread(), ep);
#ifdef FEATURE_EH_FUNCLETS
        // only do this once
        pParam->isRethrown++;
#endif // FEATURE_EH_FUNCLETS
    }
    else
    {
        CONSISTENCY_CHECK((2 == pParam->isRethrown) || (0 == pParam->isRethrown));
    }

    return EXCEPTION_CONTINUE_SEARCH;
}

HRESULT GetHRFromThrowable(OBJECTREF throwable)
{
    STATIC_CONTRACT_THROWS;
    STATIC_CONTRACT_GC_TRIGGERS;
    STATIC_CONTRACT_MODE_ANY;

    HRESULT    hr  = E_FAIL;
    MethodTable *pMT = throwable->GetMethodTable();

    // Only Exception objects have a HResult field
    // So don't fetch the field unless we have an exception

    _ASSERTE(IsException(pMT));     // what is the pathway here?
    if (IsException(pMT))
    {
        hr = ((EXCEPTIONREF)throwable)->GetHResult();
    }

    return hr;
}

VOID DECLSPEC_NORETURN RaiseTheExceptionInternalOnly(OBJECTREF throwable, BOOL rethrow, BOOL fForStackOverflow)
{
    STATIC_CONTRACT_THROWS;
    STATIC_CONTRACT_GC_TRIGGERS;
    STATIC_CONTRACT_MODE_COOPERATIVE;

    STRESS_LOG3(LF_EH, LL_INFO100, "******* MANAGED EXCEPTION THROWN: Object thrown: %p MT %pT rethrow %d\n",
                OBJECTREFToObject(throwable), (throwable!=0)?throwable->GetMethodTable():0, rethrow);

#ifdef STRESS_LOG
    // Any object could have been thrown, but System.Exception objects have useful information for the stress log
    if (throwable == CLRException::GetPreallocatedStackOverflowException())
    {
        // if are handling an SO, don't try to get all that other goop.  It isn't there anyway,
        // and it could cause us to take another SO.
        STRESS_LOG1(LF_EH, LL_INFO100, "Exception HRESULT = 0x%x \n", COR_E_STACKOVERFLOW);
    }
    else if (throwable != 0)
    {
        _ASSERTE(IsException(throwable->GetMethodTable()));

        int hr = ((EXCEPTIONREF)throwable)->GetHResult();
        STRINGREF message = ((EXCEPTIONREF)throwable)->GetMessage();
        OBJECTREF innerEH = ((EXCEPTIONREF)throwable)->GetInnerException();

        STRESS_LOG4(LF_EH, LL_INFO100, "Exception HRESULT = 0x%x Message String 0x%p (db will display) InnerException %p MT %pT\n",
            hr, OBJECTREFToObject(message), OBJECTREFToObject(innerEH), (innerEH!=0)?innerEH->GetMethodTable():0);
    }
#endif

    struct Param : RaiseExceptionFilterParam
    {
        OBJECTREF throwable;
        BOOL fForStackOverflow;
        ULONG_PTR exceptionArgs[INSTANCE_TAGGED_SEH_PARAM_ARRAY_SIZE];
        Thread *pThread;
        ThreadExceptionState* pExState;
    } param;
    param.isRethrown = rethrow ? 1 : 0; // normalize because we use it as a count in RaiseExceptionFilter
    param.throwable = throwable;
    param.fForStackOverflow = fForStackOverflow;
    param.pThread = GetThread();

    _ASSERTE(param.pThread);
    param.pExState = param.pThread->GetExceptionState();

    if (param.pThread->IsRudeAbortInitiated())
    {
        // Nobody should be able to swallow rude thread abort.
        param.throwable = CLRException::GetBestThreadAbortException();
    }

#if 0
    // TODO: enable this after we change RealCOMPlusThrow
#ifdef _DEBUG
    // If ThreadAbort exception is thrown, the thread should be marked with AbortRequest.
    // If not, we may see unhandled exception.
    if (param.throwable->GetMethodTable() == g_pThreadAbortExceptionClass)
    {
        _ASSERTE(GetThread()->IsAbortRequested()
#ifdef TARGET_X86
                 ||
                 GetFirstCOMPlusSEHRecord(this) == EXCEPTION_CHAIN_END
#endif
                 );
    }
#endif
#endif

    // raise
    PAL_TRY(Param *, pParam, &param)
    {
        //_ASSERTE(! pParam->isRethrown || pParam->pExState->m_pExceptionRecord);
        ULONG_PTR *args = NULL;
        ULONG argCount = 0;
        ULONG flags = 0;
        ULONG code = 0;

        // Always save the current object in the handle so on rethrow we can reuse it. This is important as it
        // contains stack trace info.
        //
        // Note: we use SafeSetLastThrownObject, which will try to set the throwable and if there are any problems,
        // it will set the throwable to something appropriate (like OOM exception) and return the new
        // exception. Thus, the user's exception object can be replaced here.
        pParam->throwable = pParam->pThread->SafeSetLastThrownObject(pParam->throwable);

        if (!pParam->isRethrown ||
#ifdef FEATURE_INTERPRETER
            !pParam->pExState->IsExceptionInProgress() ||
#endif // FEATURE_INTERPRETER
             pParam->pExState->IsComPlusException() ||
            (pParam->pExState->GetExceptionCode() == STATUS_STACK_OVERFLOW))
        {
            ULONG_PTR hr = GetHRFromThrowable(pParam->throwable);

            args = pParam->exceptionArgs;
            argCount = MarkAsThrownByUs(args, hr);
            flags = EXCEPTION_NONCONTINUABLE;
            code = EXCEPTION_COMPLUS;
        }
        else
        {
            // Exception code should be consistent.
            _ASSERTE((DWORD)(pParam->pExState->GetExceptionRecord()->ExceptionCode) == pParam->pExState->GetExceptionCode());

            args     = pParam->pExState->GetExceptionRecord()->ExceptionInformation;
            argCount = pParam->pExState->GetExceptionRecord()->NumberParameters;
            flags    = pParam->pExState->GetExceptionRecord()->ExceptionFlags;
            code     = pParam->pExState->GetExceptionRecord()->ExceptionCode;
        }

        if (pParam->pThread->IsAbortInitiated () && IsExceptionOfType(kThreadAbortException,&pParam->throwable))
        {
            pParam->pThread->ResetPreparingAbort();

            if (pParam->pThread->GetFrame() == FRAME_TOP)
            {
                // There is no more managed code on stack.
                pParam->pThread->ResetAbort();
            }
        }

        // Can't access the exception object when are in pre-emptive, so find out before
        // if its an SO.
        BOOL fIsStackOverflow = IsExceptionOfType(kStackOverflowException, &pParam->throwable);

        if (fIsStackOverflow || pParam->fForStackOverflow)
        {
            // Don't probe if we're already handling an SO.  Just throw the exception.
            RaiseException(code, flags, argCount, args);
        }

        // This needs to be both here and inside the handler below
        // enable preemptive mode before call into OS
        GCX_PREEMP_NO_DTOR();

        // In non-debug, we can just raise the exception once we've probed.
        RaiseException(code, flags, argCount, args);
    }
    PAL_EXCEPT_FILTER (RaiseExceptionFilter)
    {
    }
    PAL_ENDTRY
    _ASSERTE(!"Cannot continue after COM+ exception");      // Debugger can bring you here.
    // For example,
    // Debugger breaks in due to second chance exception (unhandled)
    // User hits 'g'
    // Then debugger can bring us here.
    EEPOLICY_HANDLE_FATAL_ERROR(COR_E_EXECUTIONENGINE);
    UNREACHABLE();
}


// INSTALL_COMPLUS_EXCEPTION_HANDLER has a filter, so must put the call in a separate fcn
static VOID DECLSPEC_NORETURN RealCOMPlusThrowWorker(OBJECTREF throwable, BOOL rethrow)
{
    STATIC_CONTRACT_THROWS;
    STATIC_CONTRACT_GC_TRIGGERS;
    STATIC_CONTRACT_MODE_ANY;

    // RaiseTheException will throw C++ OOM and SO, so that our escalation policy can kick in.
    // Unfortunately, COMPlusFrameHandler installed here, will try to create managed exception object.
    // We may hit a recursion.

    _ASSERTE(throwable != CLRException::GetPreallocatedStackOverflowException());

    // TODO: Do we need to install COMPlusFrameHandler here?
    INSTALL_COMPLUS_EXCEPTION_HANDLER();
    if (throwable == NULL)
    {
        _ASSERTE(!"RealCOMPlusThrow(OBJECTREF) called with NULL argument. Somebody forgot to post an exception!");
        EEPOLICY_HANDLE_FATAL_ERROR(COR_E_EXECUTIONENGINE);
    }
    RaiseTheExceptionInternalOnly(throwable, rethrow);
    UNINSTALL_COMPLUS_EXCEPTION_HANDLER();
}

VOID DECLSPEC_NORETURN RealCOMPlusThrow(OBJECTREF throwable, BOOL rethrow)
{
    STATIC_CONTRACT_THROWS;
    STATIC_CONTRACT_GC_TRIGGERS;
    STATIC_CONTRACT_MODE_ANY;

    LOG((LF_EH, LL_INFO100, "RealCOMPlusThrow throwing %s\n",
        throwable->GetMethodTable()->GetDebugClassName()));

    GCPROTECT_BEGIN(throwable);

    _ASSERTE(IsException(throwable->GetMethodTable()));

    // This may look a bit odd, but there is an explanation.  The rethrow boolean
    //  means that an actual RaiseException(EXCEPTION_COMPLUS,...) is being re-thrown,
    //  and that the exception context saved on the Thread object should replace
    //  the exception context from the upcoming RaiseException().  There is logic
    //  in the stack trace code to preserve MOST of the stack trace, but to drop the
    //  last element of the stack trace (has to do with having the address of the rethrow
    //  instead of the address of the original call in the stack trace.  That is
    //  controversial itself, but we won't get into that here.)
    // However, if this is not re-raising that original exception, but rather a new
    //  os exception for what may be an existing exception object, it is generally
    //  a good thing to preserve the stack trace.
    if (!rethrow)
    {
        Thread *pThread = GetThread();
        pThread->IncPreventAbort();
        ExceptionPreserveStackTrace(throwable);
        pThread->DecPreventAbort();
    }

    RealCOMPlusThrowWorker(throwable, rethrow);

    GCPROTECT_END();
}

VOID DECLSPEC_NORETURN RealCOMPlusThrow(OBJECTREF throwable)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;

    RealCOMPlusThrow(throwable, FALSE);
}

#ifdef USE_CHECKED_OBJECTREFS
VOID DECLSPEC_NORETURN RealCOMPlusThrow(Object *exceptionObj)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;

    OBJECTREF throwable = ObjectToOBJECTREF(exceptionObj);
    RealCOMPlusThrow(throwable, FALSE);
}
#endif // USE_CHECKED_OBJECTREFS

// this function finds the managed callback to get a resource
// string from the then current local domain and calls it
// this could be a lot of work
STRINGREF GetResourceStringFromManaged(STRINGREF key)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(key != NULL);
    }
    CONTRACTL_END;

    struct xx {
        STRINGREF key;
        STRINGREF ret;
    } gc;

    gc.key = key;
    gc.ret = NULL;

    GCPROTECT_BEGIN(gc);

    MethodDescCallSite getResourceStringLocal(METHOD__ENVIRONMENT__GET_RESOURCE_STRING_LOCAL);

    // Call Environment::GetResourceStringLocal(String name).  Returns String value (or maybe null)
    // Don't need to GCPROTECT pArgs, since it's not used after the function call.

    ARG_SLOT pArgs[1] = { ObjToArgSlot(gc.key) };
    gc.ret = getResourceStringLocal.Call_RetSTRINGREF(pArgs);

    GCPROTECT_END();

    return gc.ret;
}

// This function does poentially a LOT of work (loading possibly 50 classes).
// The return value is an un-GC-protected string ref, or possibly NULL.
void ResMgrGetString(LPCWSTR wszResourceName, STRINGREF * ppMessage)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;

    _ASSERTE(ppMessage != NULL);

    if (wszResourceName == NULL || *wszResourceName == W('\0'))
    {
        ppMessage = NULL;
        return;
    }

    // this function never looks at name again after
    // calling the helper so no need to GCPROTECT it
    STRINGREF name = StringObject::NewString(wszResourceName);

    if (wszResourceName != NULL)
    {
        STRINGREF value = GetResourceStringFromManaged(name);

        _ASSERTE(value!=NULL || !"Resource string lookup failed - possible misspelling or .resources missing or out of date?");
        *ppMessage = value;
    }
}

#ifdef FEATURE_COMINTEROP
void FreeExceptionData(ExceptionData *pedata)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
    }
    CONTRACTL_END;

    _ASSERTE(pedata != NULL);

    // <TODO>@NICE: At one point, we had the comment:
    //     (DM) Remove this when shutdown works better.</TODO>
    // This test may no longer be necessary.  Remove at own peril.
    Thread *pThread = GetThreadNULLOk();
    if (!pThread)
        return;

    if (pedata->bstrSource)
        SysFreeString(pedata->bstrSource);
    if (pedata->bstrDescription)
        SysFreeString(pedata->bstrDescription);
    if (pedata->bstrHelpFile)
        SysFreeString(pedata->bstrHelpFile);
}
#endif // FEATURE_COMINTEROP

void GetExceptionForHR(HRESULT hr, IErrorInfo* pErrInfo, OBJECTREF* pProtectedThrowable)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(IsProtectedByGCFrame(pProtectedThrowable));
    }
    CONTRACTL_END;

    // Initialize
    *pProtectedThrowable = NULL;

#if defined(FEATURE_COMINTEROP)
    if (pErrInfo != NULL)
    {
        // If this represents a managed object...
        // ...then get the managed exception object and also check if it is a __ComObject...
        if (IsManagedObject(pErrInfo))
        {
            GetObjectRefFromComIP(pProtectedThrowable, pErrInfo);
            if ((*pProtectedThrowable) != NULL)
            {
                // ...if it is, then we'll just default to an exception based on the IErrorInfo.
                if ((*pProtectedThrowable)->GetMethodTable()->IsComObjectType())
                {
                    (*pProtectedThrowable) = NULL;
                }
                else
                {
                    // We have created an exception. Release the IErrorInfo
                    ULONG cbRef = SafeRelease(pErrInfo);
                    LogInteropRelease(pErrInfo, cbRef, "IErrorInfo release");
                    return;
                }
            }
        }

        // If we got here and we don't have an exception object, we have a native IErrorInfo or
        // a managed __ComObject based IErrorInfo, so we'll just create an exception based on
        // the native IErrorInfo.
        if ((*pProtectedThrowable) == NULL)
        {
            EECOMException ex(hr, pErrInfo COMMA_INDEBUG(FALSE));
            (*pProtectedThrowable) = ex.GetThrowable();
        }
    }
#endif // defined(FEATURE_COMINTEROP)

    // If we made it here and we don't have an exception object, we didn't have a valid IErrorInfo
    // so we'll create an exception based solely on the hresult.
    if ((*pProtectedThrowable) == NULL)
    {
        EEMessageException ex(hr);
        (*pProtectedThrowable) = ex.GetThrowable();
    }
}

void GetExceptionForHR(HRESULT hr, OBJECTREF* pProtectedThrowable)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;        // because of IErrorInfo
        MODE_ANY;
    }
    CONTRACTL_END;

    // Get an IErrorInfo if one is available.
    IErrorInfo *pErrInfo = NULL;
#ifdef FEATURE_COMINTEROP
    if (SafeGetErrorInfo(&pErrInfo) != S_OK)
        pErrInfo = NULL;
#endif // FEATURE_COMINTEROP

    GetExceptionForHR(hr, pErrInfo, pProtectedThrowable);
}


//
// Maps a Win32 fault to a COM+ Exception enumeration code
//
DWORD MapWin32FaultToCOMPlusException(EXCEPTION_RECORD *pExceptionRecord)
{
    WRAPPER_NO_CONTRACT;

    switch (pExceptionRecord->ExceptionCode)
    {
        case STATUS_FLOAT_INEXACT_RESULT:
        case STATUS_FLOAT_INVALID_OPERATION:
        case STATUS_FLOAT_STACK_CHECK:
        case STATUS_FLOAT_UNDERFLOW:
            return (DWORD) kArithmeticException;
        case STATUS_FLOAT_OVERFLOW:
        case STATUS_INTEGER_OVERFLOW:
            return (DWORD) kOverflowException;

        case STATUS_FLOAT_DIVIDE_BY_ZERO:
        case STATUS_INTEGER_DIVIDE_BY_ZERO:
            return (DWORD) kDivideByZeroException;

        case STATUS_FLOAT_DENORMAL_OPERAND:
            return (DWORD) kFormatException;

        case STATUS_ACCESS_VIOLATION:
            {
#if defined(FEATURE_HIJACK) && !defined(TARGET_UNIX)
                // If we got the exception on a redirect function it means the original exception happened in managed code:
                if (Thread::IsAddrOfRedirectFunc(pExceptionRecord->ExceptionAddress))
                    return (DWORD) kNullReferenceException;

                if (pExceptionRecord->ExceptionAddress == (LPVOID)GetEEFuncEntryPoint(THROW_CONTROL_FOR_THREAD_FUNCTION))
                {
                    return (DWORD) kNullReferenceException;
                }
#endif // FEATURE_HIJACK && !TARGET_UNIX

                // If the IP of the AV is not in managed code, then its an AccessViolationException.
                if (!ExecutionManager::IsManagedCode((PCODE)pExceptionRecord->ExceptionAddress))
                {
                    return (DWORD) kAccessViolationException;
                }

                // If the address accessed is above 64k (Windows) or page size (Unix), then its an AccessViolationException.
                if (pExceptionRecord->ExceptionInformation[1] >= NULL_AREA_SIZE)
                {
                    return (DWORD) kAccessViolationException;
                }

                return (DWORD) kNullReferenceException;
            }

        case STATUS_ARRAY_BOUNDS_EXCEEDED:
            return (DWORD) kIndexOutOfRangeException;

        case STATUS_NO_MEMORY:
            return (DWORD) kOutOfMemoryException;

        case STATUS_STACK_OVERFLOW:
            return (DWORD) kStackOverflowException;

#ifdef ALIGN_ACCESS
        case STATUS_DATATYPE_MISALIGNMENT:
            return (DWORD) kDataMisalignedException;
#endif // ALIGN_ACCESS

        default:
            return kSEHException;
    }
}

#ifdef _DEBUG
#ifndef FEATURE_EH_FUNCLETS
// check if anyone has written to the stack above the handler which would wipe out the EH registration
void CheckStackBarrier(EXCEPTION_REGISTRATION_RECORD *exRecord)
{
    LIMITED_METHOD_CONTRACT;

    if (exRecord->Handler != (PEXCEPTION_ROUTINE)COMPlusFrameHandler)
        return;

    DWORD *stackOverwriteBarrier = (DWORD *)((BYTE*)exRecord - offsetof(FrameHandlerExRecordWithBarrier, m_ExRecord));
    for (int i =0; i < STACK_OVERWRITE_BARRIER_SIZE; i++) {
        if (*(stackOverwriteBarrier+i) != STACK_OVERWRITE_BARRIER_VALUE) {
            // to debug this error, you must determine who erroneously overwrote the stack
            _ASSERTE(!"Fatal error: the stack has been overwritten");
        }
    }
}
#endif // FEATURE_EH_FUNCLETS
#endif // _DEBUG

//-------------------------------------------------------------------------
// A marker for JIT -> EE transition when we know we're in preemptive
// gc mode.  As we leave the EE, we fix a few things:
//
//      - the gc state must be set back to preemptive-operative
//      - the COM+ frame chain must be rewound to what it was on entry
//      - ExInfo()->m_pSearchBoundary must be adjusted
//        if we popped the frame that is identified as begnning the next
//        crawl.
//-------------------------------------------------------------------------

void COMPlusCooperativeTransitionHandler(Frame* pFrame)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    LOG((LF_EH, LL_INFO1000, "COMPlusCooprativeTransitionHandler unwinding\n"));

    {
    Thread* pThread = GetThread();

    // Restore us to cooperative gc mode.
        GCX_COOP();

    // Pop the frame chain.
    UnwindFrameChain(pThread, pFrame);
    CONSISTENCY_CHECK(pFrame == pThread->GetFrame());

#ifndef FEATURE_EH_FUNCLETS
    // An exception is being thrown through here.  The COM+ exception
    // info keeps a pointer to a frame that is used by the next
    // COM+ Exception Handler as the starting point of its crawl.
    // We may have popped this marker -- in which case, we need to
    // update it to the current frame.
    //
    ThreadExceptionState* pExState = pThread->GetExceptionState();
    Frame*  pSearchBoundary = NULL;

    if (pThread->IsExceptionInProgress())
    {
        pSearchBoundary = pExState->m_currentExInfo.m_pSearchBoundary;
    }

    if (pSearchBoundary && pSearchBoundary < pFrame)
    {
        LOG((LF_EH, LL_INFO1000, "\tpExInfo->m_pSearchBoundary = %08x\n", (void*)pFrame));
        pExState->m_currentExInfo.m_pSearchBoundary = pFrame;
    }
#endif // FEATURE_EH_FUNCLETS
}

    // Restore us to preemptive gc mode.
    GCX_PREEMP_NO_DTOR();
}



void StackTraceInfo::Init()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        FORBID_FAULT;
    }
    CONTRACTL_END;

    LOG((LF_EH, LL_INFO10000, "StackTraceInfo::Init (%p)\n", this));

    m_pStackTrace = NULL;
    m_cStackTrace = 0;
    m_dFrameCount = 0;
    m_cDynamicMethodItems = 0;
    m_dCurrentDynamicIndex = 0;
}

void StackTraceInfo::FreeStackTrace()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        FORBID_FAULT;
    }
    CONTRACTL_END;

    if (m_pStackTrace)
    {
        delete [] m_pStackTrace;
        m_pStackTrace = NULL;
        m_cStackTrace = 0;
        m_dFrameCount = 0;
        m_cDynamicMethodItems = 0;
        m_dCurrentDynamicIndex = 0;
    }
}

BOOL StackTraceInfo::IsEmpty()
{
    LIMITED_METHOD_CONTRACT;

    return 0 == m_dFrameCount;
}

void StackTraceInfo::ClearStackTrace()
{
    LIMITED_METHOD_CONTRACT;

    LOG((LF_EH, LL_INFO1000, "StackTraceInfo::ClearStackTrace (%p)\n", this));
    m_dFrameCount = 0;
}

// allocate stack trace info. As each function is found in the stack crawl, it will be added
// to this list. If the list is too small, it is reallocated.
void StackTraceInfo::AllocateStackTrace()
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_MODE_ANY;
    STATIC_CONTRACT_FORBID_FAULT;

    LOG((LF_EH, LL_INFO1000, "StackTraceInfo::AllocateStackTrace (%p)\n", this));

    if (!m_pStackTrace)
    {
#ifdef _DEBUG
        unsigned int allocSize = 2;    // make small to exercise realloc
#else
        unsigned int allocSize = 30;
#endif

        SCAN_IGNORE_FAULT; // A fault of new is okay here. The rest of the system is cool if we don't have enough
                           // memory to remember the stack as we run our first pass.
        m_pStackTrace = new (nothrow) StackTraceElement[allocSize];

        if (m_pStackTrace != NULL)
        {
            // Remember how much we allocated.
            m_cStackTrace = allocSize;
            m_cDynamicMethodItems = allocSize;
        }
        else
        {
            m_cStackTrace = 0;
            m_cDynamicMethodItems = 0;
        }
    }
}

//
// Returns true if it appended the element, false otherwise.
//
BOOL StackTraceInfo::AppendElement(BOOL bAllowAllocMem, UINT_PTR currentIP, UINT_PTR currentSP, MethodDesc* pFunc, CrawlFrame* pCf)
{
    CONTRACTL
    {
        GC_TRIGGERS;
        NOTHROW;
    }
    CONTRACTL_END

    LOG((LF_EH, LL_INFO10000, "StackTraceInfo::AppendElement (%p), IP = %p, SP = %p, %s::%s\n", this, currentIP, currentSP, pFunc ? pFunc->m_pszDebugClassName : "", pFunc ? pFunc->m_pszDebugMethodName : "" ));
    BOOL bRetVal = FALSE;

    if (pFunc != NULL && pFunc->IsILStub())
        return FALSE;

    // Save this function in the stack trace array, which we only build on the first pass. We'll try to expand the
    // stack trace array if we don't have enough room. Note that we only try to expand if we're allowed to allocate
    // memory (bAllowAllocMem).
    if (bAllowAllocMem && (m_dFrameCount >= m_cStackTrace))
    {
        StackTraceElement* pTempElement = new (nothrow) StackTraceElement[m_cStackTrace*2];

        if (pTempElement != NULL)
        {
            memcpy(pTempElement, m_pStackTrace, m_cStackTrace * sizeof(StackTraceElement));
            delete [] m_pStackTrace;
            m_pStackTrace = pTempElement;
            m_cStackTrace *= 2;
        }
    }

    // Add the function to the stack trace array if there's room.
    if (m_dFrameCount < m_cStackTrace)
    {
        StackTraceElement* pStackTraceElem;

        // If we get in here, we'd better have a stack trace array.
        CONSISTENCY_CHECK(m_pStackTrace != NULL);

        pStackTraceElem = &(m_pStackTrace[m_dFrameCount]);

        pStackTraceElem->pFunc = pFunc;

        pStackTraceElem->ip = currentIP;
        pStackTraceElem->sp = currentSP;

        // When we are building stack trace as we encounter managed frames during exception dispatch,
        // then none of those frames represent a stack trace from a foreign exception (as they represent
        // the current exception). Hence, set the corresponding flag to FALSE.
        pStackTraceElem->flags = 0;

        // This is a workaround to fix the generation of stack traces from exception objects so that
        // they point to the line that actually generated the exception instead of the line
        // following.
        if (pCf->IsIPadjusted())
        {
            pStackTraceElem->flags |= STEF_IP_ADJUSTED;
        }
        else if (!pCf->HasFaulted() && pStackTraceElem->ip != 0)
        {
            pStackTraceElem->ip -= STACKWALK_CONTROLPC_ADJUST_OFFSET;
            pStackTraceElem->flags |= STEF_IP_ADJUSTED;
        }

        ++m_dFrameCount;
        bRetVal = TRUE;
    }

#ifndef TARGET_UNIX // Watson is supported on Windows only
    Thread *pThread = GetThread();

    if (pThread && (currentIP != 0))
    {
        // Setup the watson bucketing details for the initial throw
        // callback only if we dont already have them.
        ThreadExceptionState *pExState = pThread->GetExceptionState();
        if (!pExState->GetFlags()->GotWatsonBucketDetails())
        {
            // Adjust the IP if necessary.
            UINT_PTR adjustedIp = currentIP;
            // This is a workaround copied from above.
            if (!(pCf->HasFaulted() || pCf->IsIPadjusted()) && adjustedIp != 0)
            {
                adjustedIp -= 1;
            }

            // Setup the bucketing details for the initial throw
            SetupInitialThrowBucketDetails(adjustedIp);
        }
    }
#endif // !TARGET_UNIX

    return bRetVal;
}

void UnwindFrameChain(Thread* pThread, LPVOID pvLimitSP)
{
    CONTRACTL
    {
        NOTHROW;
        DISABLED(GC_TRIGGERS);  // some Frames' ExceptionUnwind methods trigger  :(
        MODE_ANY;
    }
    CONTRACTL_END;

    Frame* pFrame = pThread->m_pFrame;
    if (pFrame < pvLimitSP)
    {
        GCX_COOP_THREAD_EXISTS(pThread);

        //
        // call ExceptionUnwind with the Frame chain intact
        //
        pFrame = pThread->NotifyFrameChainOfExceptionUnwind(pFrame, pvLimitSP);

        //
        // now pop the frames off by trimming the Frame chain
        //
        pThread->SetFrame(pFrame);
    }
}

BOOL IsExceptionOfType(RuntimeExceptionKind reKind, Exception *pException)
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_TRIGGERS;
    STATIC_CONTRACT_MODE_ANY;
    STATIC_CONTRACT_FORBID_FAULT;

      if (pException->IsType(reKind))
        return TRUE;

    if (pException->IsType(CLRException::GetType()))
    {
        // Since we're going to be holding onto the Throwable object we
        // need to be in COOPERATIVE.
        GCX_COOP();

        OBJECTREF Throwable=((CLRException*)pException)->GetThrowable();

        GCX_FORBID();
        if (IsExceptionOfType(reKind, &Throwable))
            return TRUE;
    }
    return FALSE;
}

BOOL IsExceptionOfType(RuntimeExceptionKind reKind, OBJECTREF *pThrowable)
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_MODE_COOPERATIVE;
    STATIC_CONTRACT_FORBID_FAULT;

    _ASSERTE(pThrowable != NULL);

    if (*pThrowable == NULL)
        return FALSE;

    MethodTable *pThrowableMT = (*pThrowable)->GetMethodTable();

    return CoreLibBinder::IsException(pThrowableMT, reKind);
}

BOOL IsAsyncThreadException(OBJECTREF *pThrowable) {
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_MODE_COOPERATIVE;
    STATIC_CONTRACT_FORBID_FAULT;

    if (  (GetThreadNULLOk() && GetThread()->IsRudeAbort() && GetThread()->IsRudeAbortInitiated())
        ||IsExceptionOfType(kThreadAbortException, pThrowable)
        ||IsExceptionOfType(kThreadInterruptedException, pThrowable)) {
        return TRUE;
    } else {
        return FALSE;
    }
}

BOOL IsUncatchable(OBJECTREF *pThrowable)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_COOPERATIVE;
        FORBID_FAULT;
    } CONTRACTL_END;

    _ASSERTE(pThrowable != NULL);

    Thread *pThread = GetThreadNULLOk();

    if (pThread)
    {
        if (pThread->IsAbortInitiated())
            return TRUE;

        if (OBJECTREFToObject(*pThrowable)->GetMethodTable() == g_pExecutionEngineExceptionClass)
            return TRUE;
    }

    return FALSE;
}

BOOL IsStackOverflowException(Thread* pThread, EXCEPTION_RECORD* pExceptionRecord)
{
    if (pExceptionRecord->ExceptionCode == STATUS_STACK_OVERFLOW)
    {
        return true;
    }

    if (IsComPlusException(pExceptionRecord) &&
         pThread->IsLastThrownObjectStackOverflowException())
    {
        return true;
    }

    return false;
}


#ifdef _DEBUG
BOOL IsValidClause(EE_ILEXCEPTION_CLAUSE *EHClause)
{
    LIMITED_METHOD_CONTRACT;

#if 0
    DWORD valid = COR_ILEXCEPTION_CLAUSE_FILTER | COR_ILEXCEPTION_CLAUSE_FINALLY |
        COR_ILEXCEPTION_CLAUSE_FAULT | COR_ILEXCEPTION_CLAUSE_CACHED_CLASS;

    // <TODO>@NICE: enable this when VC stops generatng a bogus 0x8000.</TODO>
    if (EHClause->Flags & ~valid)
        return FALSE;
#endif
    if (EHClause->TryStartPC > EHClause->TryEndPC)
        return FALSE;
    return TRUE;
}
#endif


#ifdef DEBUGGING_SUPPORTED
LONG NotifyDebuggerLastChance(Thread *pThread,
                              EXCEPTION_POINTERS *pExceptionInfo,
                              BOOL jitAttachRequested)
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_TRIGGERS;
    STATIC_CONTRACT_MODE_ANY;

    LONG retval = EXCEPTION_CONTINUE_SEARCH;

    // Debugger does func-evals inside this call, which may take nested exceptions. We need a nested exception
    // handler to allow this.
    INSTALL_NESTED_EXCEPTION_HANDLER(pThread->GetFrame());

    EXCEPTION_POINTERS dummy;
    dummy.ExceptionRecord = NULL;
    dummy.ContextRecord = NULL;

    if (NULL == pExceptionInfo)
    {
        pExceptionInfo = &dummy;
    }
    else if (NULL != pExceptionInfo->ExceptionRecord && NULL == pExceptionInfo->ContextRecord)
    {
        // In a soft stack overflow, we have an exception record but not a  context record.
        // Debugger::LastChanceManagedException requires that both ExceptionRecord and
        // ContextRecord be valid or both be NULL.
        pExceptionInfo = &dummy;
    }

    if  (g_pDebugInterface && g_pDebugInterface->LastChanceManagedException(pExceptionInfo,
                                                                            pThread,
                                                                            jitAttachRequested) == ExceptionContinueExecution)
    {
        retval = EXCEPTION_CONTINUE_EXECUTION;
    }

    UNINSTALL_NESTED_EXCEPTION_HANDLER();

#ifdef DEBUGGER_EXCEPTION_INTERCEPTION_SUPPORTED
    EX_TRY
    {
        // if the debugger wants to intercept the unhandled exception then we immediately unwind without returning
        // If there is a problem with this function unwinding here it could be separated out however
        // we need to be very careful. Previously we had the opposite problem in that we notified the debugger
        // of an unhandled exception and then either:
        // a) never gave the debugger a chance to intercept later, or
        // b) code changed more process state unaware that the debugger would be handling the exception
        if ((pThread->IsExceptionInProgress()) && pThread->GetExceptionState()->GetFlags()->DebuggerInterceptInfo())
        {
            // The debugger wants to intercept this exception.  It may return in a failure case, in which case we want
            // to continue thru this path.
            ClrDebuggerDoUnwindAndIntercept(X86_FIRST_ARG(EXCEPTION_CHAIN_END) pExceptionInfo->ExceptionRecord);
        }
    }
    EX_CATCH // if we fail to intercept just continue as is
    {
    }
    EX_END_CATCH(SwallowAllExceptions);
#endif // DEBUGGER_EXCEPTION_INTERCEPTION_SUPPORTED

    return retval;
}

#ifndef TARGET_UNIX
//----------------------------------------------------------------------------
//
// DoReportFault - wrapper for ReportFault in FaultRep.dll, which also handles
//                 debugger launch synchronization if the user chooses to launch
//                 a debugger
//
// Arguments:
//    pExceptionInfo - pointer to exception info
//
// Return Value:
//    The returned EFaultRepRetVal value from ReportFault
//
// Note:
//
//----------------------------------------------------------------------------
EFaultRepRetVal DoReportFault(EXCEPTION_POINTERS * pExceptionInfo)
{
    LIMITED_METHOD_CONTRACT;

    HINSTANCE hmod = WszLoadLibrary(W("FaultRep.dll"));
    EFaultRepRetVal r = frrvErr;
    if (hmod)
    {
        pfn_REPORTFAULT pfnReportFault = (pfn_REPORTFAULT)GetProcAddress(hmod, "ReportFault");
        if (pfnReportFault)
        {
            r = pfnReportFault(pExceptionInfo, 0);
        }
        FreeLibrary(hmod);
    }

    if (r == frrvLaunchDebugger)
    {
        // Wait until the pending managed debugger attach is completed
        if (g_pDebugInterface != NULL)
        {
            g_pDebugInterface->WaitForDebuggerAttach();
        }
    }
    return r;
}

//----------------------------------------------------------------------------
//
// DisableOSWatson - Set error mode to disable OS Watson
//
// Arguments:
//    None
//
// Return Value:
//    None
//
// Note: SetErrorMode changes the process wide error mode, which can be overridden by other threads
//       in a race.  The solution is to use new Win7 per thread error mode APIs, which take precedence
//       over process wide error mode. However, we shall not use per thread error mode if the runtime
//       is being hosted because with per thread error mode being used the OS will ignore the process
//       wide error mode set by the host.
//
//----------------------------------------------------------------------------
void DisableOSWatson(void)
{
    LIMITED_METHOD_CONTRACT;

    // When a debugger is attached (or will be attaching), we need to disable the OS GPF dialog.
    // If we don't, an unhandled managed exception will launch the OS watson dialog even when
    // the debugger is attached.
    const UINT lastErrorMode = SetErrorMode(0);
    SetErrorMode(lastErrorMode | SEM_NOGPFAULTERRORBOX);
    LOG((LF_EH, LL_INFO100, "DisableOSWatson: SetErrorMode = 0x%x\n", lastErrorMode | SEM_NOGPFAULTERRORBOX));

}
#endif // !TARGET_UNIX

//------------------------------------------------------------------------------
// This function is called on an unhandled exception, via the runtime's
//  Unhandled Exception Filter (Hence the name, "last chance", because this
//  is the last chance to see the exception.  When running under a native
//  debugger, that won't generally happen, because the OS notifies the debugger
//  instead of calling the application's registered UEF; the debugger will
//  show the exception as second chance.)
// The function is also called sometimes for the side effects, which are
//  to possibly invoke Watson and to possibly notify the managed debugger.
// If running in a debugger already, either native or managed, we shouldn't
//  invoke Watson.
// If not running under a managed debugger, we shouldn't try to send a debugger
//   notification.
//------------------------------------------------------------------------------
LONG WatsonLastChance(                  // EXCEPTION_CONTINUE_SEARCH, _CONTINUE_EXECUTION
    Thread              *pThread,       // Thread object.
    EXCEPTION_POINTERS  *pExceptionInfo,// Information about reported exception.
    TypeOfReportedError tore)           // Just what kind of error is reported?
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_TRIGGERS;
    STATIC_CONTRACT_MODE_ANY;

    // If allocation fails, we may not produce watson dump.  But this is not fatal.
    CONTRACT_VIOLATION(AllViolation);
    LOG((LF_EH, LL_INFO10, "D::WLC: Enter WatsonLastChance\n"));

#ifndef TARGET_UNIX
    static DWORD fDisableWatson = -1;
    if (fDisableWatson == -1)
    {
        fDisableWatson = CLRConfig::GetConfigValue(CLRConfig::INTERNAL_DisableWatsonForManagedExceptions);
    }

    if (fDisableWatson && (tore.GetType() == TypeOfReportedError::UnhandledException))
    {
        DisableOSWatson();
        LOG((LF_EH, LL_INFO10, "D::WLC: OS Watson is disabled for an managed unhandled exception\n"));
        return EXCEPTION_CONTINUE_SEARCH;
    }
#endif // !TARGET_UNIX

    // We don't want to launch Watson if a debugger is already attached to
    // the process.
    BOOL shouldNotifyDebugger = FALSE;  // Assume we won't debug.

    // VS debugger team requested the Whidbey experience, which is no Watson when the debugger thread detects
    // that the debugger process is abruptly terminated, and triggers a failfast error.  In this particular
    // scenario CORDebuggerAttached() will be TRUE, but IsDebuggerPresent() will be FALSE because from OS
    // perspective the native debugger has been detached from the debuggee, but CLR has not yet marked the
    // managed debugger as detached.  Therefore, CORDebuggerAttached() is checked, so Watson will not pop up
    // when a debugger is abruptly terminated.  It also prevents a debugger from being launched on a helper
    // thread.
    BOOL alreadyDebugging     = CORDebuggerAttached() || IsDebuggerPresent();

    BOOL jitAttachRequested   = !alreadyDebugging; // Launch debugger if not already running.

#ifdef _DEBUG
    // If BreakOnUnCaughtException is set, we may be using a native debugger to debug this stuff
    BOOL BreakOnUnCaughtException = CLRConfig::GetConfigValue(CLRConfig::INTERNAL_BreakOnUncaughtException);
    if(!alreadyDebugging || (!CORDebuggerAttached() && BreakOnUnCaughtException) )
#else
    if (!alreadyDebugging)
#endif
    {
        LOG((LF_EH, LL_INFO10, "WatsonLastChance: Debugger not attached at sp %p ...\n", GetCurrentSP()));

#ifndef TARGET_UNIX
        FaultReportResult result = FaultReportResultQuit;

        BOOL fSOException = FALSE;

        if ((pExceptionInfo != NULL) &&
            (pExceptionInfo->ExceptionRecord != NULL) &&
            (pExceptionInfo->ExceptionRecord->ExceptionCode == STATUS_STACK_OVERFLOW))
        {
            fSOException = TRUE;
        }

        if (g_pDebugInterface)
        {
            // we are about to let the OS trigger jit attach, however we need to synchronize with our
            // own jit attach that we might be doing on another thread
            // PreJitAttach races this thread against any others which might be attaching and if some other
            // thread is doing it then we wait for its attach to complete first
            g_pDebugInterface->PreJitAttach(TRUE, FALSE, FALSE);
        }

        // Let unhandled exceptions except stack overflow go to the OS
        if (tore.IsUnhandledException() && !fSOException)
        {
            return EXCEPTION_CONTINUE_SEARCH;
        }
        else if (tore.IsUserBreakpoint())
        {
            DoReportFault(pExceptionInfo);
        }
        else
        {
            BOOL fWatsonAlreadyLaunched = FALSE;
            if (InterlockedCompareExchange(&g_watsonAlreadyLaunched, 1, 0) != 0)
            {
                fWatsonAlreadyLaunched = TRUE;
            }

            // Logic to avoid double prompt if more than one threads calling into WatsonLastChance
            if (!fWatsonAlreadyLaunched)
            {
                // EEPolicy::HandleFatalStackOverflow pushes a FaultingExceptionFrame on the stack after SO
                // exception.   Our hijack code runs in the exception context, and overwrites the stack space
                // after SO exception, so we need to pop up this frame before invoking RaiseFailFast.
                // This cumbersome code should be removed once SO synchronization is moved to be completely
                // out-of-process.
                if (fSOException && pThread && pThread->GetFrame() != FRAME_TOP)
                {
                    GCX_COOP();     // Must be cooperative to modify frame chain.
                    pThread->GetFrame()->Pop(pThread);
                }

                LOG((LF_EH, LL_INFO10, "D::WLC: Call RaiseFailFastException\n"));

                // enable preemptive mode before call into OS to allow runtime suspend to finish
                GCX_PREEMP();

                STRESS_LOG0(LF_CORDB, LL_INFO10, "D::RFFE: About to call RaiseFailFastException\n");
#ifdef HOST_WINDOWS
                CreateCrashDumpIfEnabled(fSOException);
#endif
                RaiseFailFastException(pExceptionInfo == NULL ? NULL : pExceptionInfo->ExceptionRecord,
                                       pExceptionInfo == NULL ? NULL : pExceptionInfo->ContextRecord,
                                       0);
                STRESS_LOG0(LF_CORDB, LL_INFO10, "D::RFFE: Return from RaiseFailFastException\n");
            }
        }

        if (g_pDebugInterface)
        {
            // if execution resumed here then we may or may not be attached
            // either way we need to end the attach process and unblock any other
            // threads which were waiting for the attach here to complete
            g_pDebugInterface->PostJitAttach();
        }


        if (IsDebuggerPresent())
        {
            result = FaultReportResultDebug;
            jitAttachRequested = FALSE;
        }

        switch(result)
        {
            case FaultReportResultAbort:
            {
                // We couldn't launch watson properly. First fall-back to OS error-reporting
                // so that we don't break native apps.
                EFaultRepRetVal r = frrvErr;

                if (pExceptionInfo != NULL)
                {
                    GCX_PREEMP();

                    if (pExceptionInfo->ExceptionRecord->ExceptionCode != STATUS_STACK_OVERFLOW)
                    {
                        r = DoReportFault(pExceptionInfo);
                    }
                    else
                    {
                        // Since the StackOverflow handler also calls us, we must keep our stack budget
                        // to a minimum. Thus, we will launch a thread to do the actual work.
                        FaultReportInfo fri;
                        fri.m_pExceptionInfo       = pExceptionInfo;
                        // DoFaultCreateThreadReportCallback will overwrite this - if it doesn't, we'll assume it failed.
                        fri.m_faultRepRetValResult = frrvErr;

                        // Stack overflow case - we don't have enough stack on our own thread so let the debugger
                        // helper thread do the work.
                        if (!g_pDebugInterface || FAILED(g_pDebugInterface->RequestFavor(DoFaultReportDoFavorCallback, &fri)))
                        {
                            // If we can't initialize the debugger helper thread or we are running on the debugger helper
                            // thread, give it up. We don't have enough stack space.
                        }

                        r = fri.m_faultRepRetValResult;
                    }
                }

                if ((r == frrvErr) || (r == frrvErrNoDW) || (r == frrvErrTimeout))
                {
                    // If we don't have an exception record, or otherwise can't use OS error
                    // reporting then offer the old "press OK to terminate, cancel to debug"
                    // dialog as a futher fallback.
                    if (g_pDebugInterface && g_pDebugInterface->FallbackJITAttachPrompt())
                    {
                        // User requested to launch the debugger
                        shouldNotifyDebugger = TRUE;
                    }
                }
                else if (r == frrvLaunchDebugger)
                {
                    // User requested to launch the debugger
                    shouldNotifyDebugger = TRUE;
                }
                break;
            }
            case FaultReportResultQuit:
                // No debugger, just exit normally
                break;
            case FaultReportResultDebug:
                // JIT attach a debugger here.
                shouldNotifyDebugger = TRUE;
                break;
            default:
                UNREACHABLE_MSG("Unknown FaultReportResult");
                break;
        }
    }
    // When the debugger thread detects that the debugger process is abruptly terminated, and triggers
    // a failfast error, CORDebuggerAttached() will be TRUE, but IsDebuggerPresent() will be FALSE.
    // If IsDebuggerPresent() is FALSE, do not try to notify the deubgger.
    else if (CORDebuggerAttached() && IsDebuggerPresent())
#else
    }
    else if (CORDebuggerAttached())
#endif // !TARGET_UNIX
    {
        // Already debugging with a managed debugger.  Should let that debugger know.
        LOG((LF_EH, LL_INFO100, "WatsonLastChance: Managed debugger already attached at sp %p ...\n", GetCurrentSP()));

        // The managed EH subsystem ignores native breakpoints and single step exceptions.  These exceptions are
        // not considered managed, and the managed debugger should not be notified.  Moreover, we won't have
        // created a managed exception object at this point.
        if (tore.GetType() != TypeOfReportedError::NativeBreakpoint)
        {
            shouldNotifyDebugger = TRUE;
        }
    }

#ifndef TARGET_UNIX
    DisableOSWatson();
#endif // !TARGET_UNIX

    if (!shouldNotifyDebugger)
    {
        LOG((LF_EH, LL_INFO100, "WatsonLastChance: should not notify debugger.  Returning EXCEPTION_CONTINUE_SEARCH\n"));
        return EXCEPTION_CONTINUE_SEARCH;
    }

    // If no debugger interface, we can't notify the debugger.
    if (g_pDebugInterface == NULL)
    {
        LOG((LF_EH, LL_INFO100, "WatsonLastChance: No debugger interface.  Returning EXCEPTION_CONTINUE_SEARCH\n"));
        return EXCEPTION_CONTINUE_SEARCH;
    }

    LOG((LF_EH, LL_INFO10, "WatsonLastChance: Notifying debugger\n"));

    switch (tore.GetType())
    {
        case TypeOfReportedError::FatalError:
            if (pThread != NULL)
            {
                NotifyDebuggerLastChance(pThread, pExceptionInfo, jitAttachRequested);

                // If the registed debugger is not a managed debugger, we need to stop the debugger here.
                if (!CORDebuggerAttached() && IsDebuggerPresent())
                {
                    DebugBreak();
                }
            }
            else
            {
                g_pDebugInterface->LaunchDebuggerForUser(GetThreadNULLOk(), pExceptionInfo, FALSE, FALSE);
            }

            return EXCEPTION_CONTINUE_SEARCH;

        case TypeOfReportedError::UnhandledException:
        case TypeOfReportedError::NativeBreakpoint:
            // Notify the debugger only if this is a managed thread.
            if (pThread != NULL)
            {
                return NotifyDebuggerLastChance(pThread, pExceptionInfo, jitAttachRequested);
            }
            else
            {
                g_pDebugInterface->JitAttach(pThread, pExceptionInfo, FALSE, FALSE);

                // return EXCEPTION_CONTINUE_SEARCH, so OS's UEF will reraise the unhandled exception for debuggers
                return EXCEPTION_CONTINUE_SEARCH;
            }

        case TypeOfReportedError::UserBreakpoint:
            g_pDebugInterface->LaunchDebuggerForUser(pThread, pExceptionInfo, TRUE, FALSE);

            return EXCEPTION_CONTINUE_EXECUTION;

        case TypeOfReportedError::NativeThreadUnhandledException:
            g_pDebugInterface->JitAttach(pThread, pExceptionInfo, FALSE, FALSE);

            // return EXCEPTION_CONTINUE_SEARCH, so OS's UEF will reraise the unhandled exception for debuggers
            return EXCEPTION_CONTINUE_SEARCH;

        default:
            _ASSERTE(!"Unknown case in WatsonLastChance");
            return EXCEPTION_CONTINUE_SEARCH;
    }

    UNREACHABLE();
} // LONG WatsonLastChance()

//===========================================================================================
//
// Windows crash dump (createdump) support
//

#ifdef HOST_WINDOWS

// Crash dump generating program arguments if enabled.
LPCWSTR g_createDumpCommandLine = nullptr;

static void
BuildCreateDumpCommandLine(
    SString& commandLine,
    LPCWSTR dumpName,
    int dumpType,
    bool diag)
{
    const char* DumpGeneratorName = "createdump.exe";

    PathString coreclrPath;
    if (GetClrModulePathName(coreclrPath))
    {
        SString::CIterator lastBackslash = coreclrPath.End();
        if (coreclrPath.FindBack(lastBackslash, W('\\')))
        {
            commandLine.Set(coreclrPath, coreclrPath.Begin(), lastBackslash + 1);
        }
    }

    commandLine.AppendUTF8(DumpGeneratorName);

    if (dumpName != nullptr)
    {
        commandLine.AppendUTF8(" --name ");
        commandLine.Append(dumpName);
    }

    const char* dumpTypeOption = nullptr;
    switch (dumpType)
    {
        case 1:
            dumpTypeOption = "--normal";
            break;
        case 2:
            dumpTypeOption = "--withheap";
            break;
        case 3:
            dumpTypeOption = "--triage";
            break;
        case 4:
            dumpTypeOption = "--full";
            break;
    }

    if (dumpTypeOption != nullptr)
    {
        commandLine.AppendPrintf(" %s", dumpTypeOption);
    }

    if (diag)
    {
        commandLine.AppendPrintf(" --diag");
    }
}

static DWORD
LaunchCreateDump(LPCWSTR lpCommandLine)
{
    DWORD fSuccess = false;

    EX_TRY
    {
        SECURITY_ATTRIBUTES sa;
        sa.nLength = sizeof(sa);
        sa.lpSecurityDescriptor = NULL;
        sa.bInheritHandle = FALSE;

        STARTUPINFO StartupInfo;
        memset(&StartupInfo, 0, sizeof(StartupInfo));
        StartupInfo.cb = sizeof(StartupInfo);

        PROCESS_INFORMATION processInformation;
        if (WszCreateProcess(NULL, lpCommandLine, NULL, NULL, TRUE, 0, NULL, NULL, &StartupInfo, &processInformation))
        {
            WaitForSingleObject(processInformation.hProcess, INFINITE);

            DWORD exitCode = 0;
            GetExitCodeProcess(processInformation.hProcess, &exitCode);
            fSuccess = exitCode == 0;
        }
    }
    EX_CATCH
    {
    }
    EX_END_CATCH(SwallowAllExceptions);

    return fSuccess;
}

void
CreateCrashDumpIfEnabled(bool stackoverflow)
{
    // If enabled, launch the create minidump utility and wait until it completes. Only launch createdump once for this process.
    LPCWSTR createDumpCommandLine = InterlockedExchangeT<LPCWSTR>(&g_createDumpCommandLine, nullptr);
    if (createDumpCommandLine != nullptr)
    {
        if (stackoverflow)
        {
            HandleHolder createDumpThreadHandle = Thread::CreateUtilityThread(Thread::StackSize_Small, (LPTHREAD_START_ROUTINE)LaunchCreateDump, (void*)createDumpCommandLine, W(".NET Stack overflow create dump"));
            if (createDumpThreadHandle != INVALID_HANDLE_VALUE)
            {
                // Wait for the dump to be generated
                DWORD res = WaitForSingleObject(createDumpThreadHandle, INFINITE);
                _ASSERTE(res == WAIT_OBJECT_0);
            }
        }
        else
        {
            LaunchCreateDump(createDumpCommandLine);
        }
    }
}

bool
GenerateCrashDump(
    LPCWSTR dumpName,
    int dumpType,
    bool diag)
{
    SString commandLine;
    if (dumpType < 1 || dumpType > 4)
    {
        return false;
    }
    if (dumpName != nullptr && dumpName[0] == '\0')
    {
        dumpName = nullptr;
    }
    BuildCreateDumpCommandLine(commandLine, dumpName, dumpType, diag);
    return LaunchCreateDump(commandLine);
}

void
InitializeCrashDump()
{
    DWORD enabled = CLRConfig::GetConfigValue(CLRConfig::INTERNAL_DbgEnableMiniDump);
    if (enabled == 1)
    {
        LPCWSTR dumpName = CLRConfig::GetConfigValue(CLRConfig::INTERNAL_DbgMiniDumpName);
        int dumpType = CLRConfig::GetConfigValue(CLRConfig::INTERNAL_DbgMiniDumpType);
        DWORD diag = CLRConfig::GetConfigValue(CLRConfig::INTERNAL_CreateDumpDiagnostics);

        SString commandLine;
        BuildCreateDumpCommandLine(commandLine, dumpName, dumpType, diag == 1);
        g_createDumpCommandLine = commandLine.GetCopyOfUnicodeString();
    }
}

#endif // HOST_WINDOWS

bool GenerateDump(
    LPCWSTR dumpName,
    INT dumpType,
    ULONG32 flags,
    LPSTR errorMessageBuffer,
    INT cbErrorMessageBuffer)
{
#ifdef TARGET_UNIX
    MAKE_UTF8PTR_FROMWIDE_NOTHROW (dumpNameUtf8, dumpName);
    if (dumpNameUtf8 == nullptr)
    {
        return false;
    }
    else
    {
        return PAL_GenerateCoreDump(dumpNameUtf8, dumpType, flags, errorMessageBuffer, cbErrorMessageBuffer);
    }
#else // TARGET_UNIX
    return GenerateCrashDump(dumpName, dumpType, flags & GenerateDumpFlagsLoggingEnabled);
#endif // TARGET_UNIX
}

//************************************************************************************
// Create crash dump if enabled and terminate process. Generates crash dumps for both
// Windows and Linux if enabled. For Linux, it happens in TerminateProcess in the PAL.
//************************************************************************************

void CrashDumpAndTerminateProcess(UINT exitCode)
{
#ifdef HOST_WINDOWS
    CreateCrashDumpIfEnabled(exitCode == COR_E_STACKOVERFLOW);
#endif
    TerminateProcess(GetCurrentProcess(), exitCode);
}

//---------------------------------------------------------------------------------------
//
// This is just a simple helper to do some basic checking to see if an exception is intercepted.
// It checks that we are on a managed thread and that an exception is indeed in progress.
//
// Return Value:
//    true iff we are on a managed thread and an exception is in flight
//

bool CheckThreadExceptionStateForInterception()
{
    LIMITED_METHOD_CONTRACT;

    Thread* pThread = GetThreadNULLOk();
    if (pThread == NULL)
    {
        return false;
    }

    if (!pThread->IsExceptionInProgress())
    {
        return false;
    }

    return true;
}
#endif

//===========================================================================================
//
// UNHANDLED EXCEPTION HANDLING
//

static Volatile<BOOL> fReady = 0;
static SpinLock initLock;

//******************************************************************************
//
//  ExceptionIsAlwaysSwallowed
//
//    Determine whether an exception is of a type that it should always
//     be swallowed, even when exceptions otherwise are left to go unhandled.
//     (For Whidbey, ThreadAbort, RudeThreadAbort, or AppDomainUnload exception)
//
//  Parameters:
//    pExceptionInfo    EXCEPTION_POINTERS for current exception
//
//  Returns:
//    true              If the exception is of a type that is always swallowed.
//
BOOL ExceptionIsAlwaysSwallowed(EXCEPTION_POINTERS *pExceptionInfo)
{
    BOOL isSwallowed = false;

    // The exception code must be ours, if it is one of our Exceptions.
    if (IsComPlusException(pExceptionInfo->ExceptionRecord))
    {
        // Our exception code.  Get the current exception from the thread.
        Thread *pThread = GetThreadNULLOk();
        if (pThread)
        {
            OBJECTREF throwable;

            GCX_COOP();
            if ((throwable = pThread->GetThrowable()) == NULL)
            {
                throwable = pThread->LastThrownObject();
            }
            //@todo: could throwable be NULL here?
            isSwallowed = IsExceptionOfType(kThreadAbortException, &throwable);
        }
    }

    return isSwallowed;
} // BOOL ExceptionIsAlwaysSwallowed()

//
// UserBreakpointFilter is used to ensure that we get a popup on user breakpoints (DebugBreak(), hard-coded int 3,
// etc.) as soon as possible.
//
LONG UserBreakpointFilter(EXCEPTION_POINTERS* pEP)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        FORBID_FAULT;
    }
    CONTRACTL_END;

#ifdef DEBUGGING_SUPPORTED
    // Invoke the unhandled exception filter, bypassing any further first pass exception processing and treating
    // user breakpoints as if they're unhandled exceptions right away.
    //
    // @todo: The InternalUnhandledExceptionFilter can trigger.
    CONTRACT_VIOLATION(GCViolation | ThrowsViolation | ModeViolation | FaultViolation | FaultNotFatal);

#ifdef TARGET_UNIX
    int result = COMUnhandledExceptionFilter(pEP);
#else
    int result = UnhandledExceptionFilter(pEP);
#endif

    if (result == EXCEPTION_CONTINUE_SEARCH)
    {
        // A debugger got attached.  Instead of allowing the exception to continue up, and hope for the
        // second-chance, we cause it to happen again. The debugger snags all int3's on first-chance.  NOTE: the
        // InternalUnhandledExceptionFilter allowed GC's to occur, but it may be the case that some managed frames
        // may have been unprotected. Therefore, you may have GC holes if you attempt to continue execution from
        // here.
        return EXCEPTION_CONTINUE_EXECUTION;
    }
#endif // DEBUGGING_SUPPORTED

    if(ETW_EVENT_ENABLED(MICROSOFT_WINDOWS_DOTNETRUNTIME_PRIVATE_PROVIDER_DOTNET_Context, FailFast))
    {
        // Fire an ETW FailFast event
        FireEtwFailFast(W("StatusBreakpoint"),
                       (const PVOID)((pEP && pEP->ContextRecord) ? GetIP(pEP->ContextRecord) : 0),
                       ((pEP && pEP->ExceptionRecord) ? pEP->ExceptionRecord->ExceptionCode : 0),
                       STATUS_BREAKPOINT,
                       GetClrInstanceId());
    }

    // Otherwise, we terminate the process.
    CrashDumpAndTerminateProcess(STATUS_BREAKPOINT);

    // Shouldn't get here ...
    return EXCEPTION_CONTINUE_EXECUTION;
} // LONG UserBreakpointFilter()

//******************************************************************************
//
//  DefaultCatchFilter
//
//    The old default except filter (v1.0/v1.1) .  For user breakpoints, call out to UserBreakpointFilter()
//     but otherwise return EXCEPTION_EXECUTE_HANDLER, to swallow the exception.
//
//  Parameters:
//    pExceptionInfo    EXCEPTION_POINTERS for current exception
//    pv                A constant as an INT_PTR.  Must be COMPLUS_EXCEPTION_EXECUTE_HANDLER.
//
//  Returns:
//    EXCEPTION_EXECUTE_HANDLER     Generally returns this to swallow the exception.
//
// IMPORTANT!! READ ME!!
//
// This filter is very similar to DefaultCatchNoSwallowFilter, except when unhandled
//  exception policy/config dictate swallowing the exception.
// If you make any changes to this function, look to see if the other one also needs
//  the same change.
//
LONG DefaultCatchFilter(EXCEPTION_POINTERS *ep, PVOID pv)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        FORBID_FAULT;
    }
    CONTRACTL_END;

    //
    // @TODO: this seems like a strong candidate for elimination due to duplication with
    //        our vectored exception handler.
    //

    DefaultCatchFilterParam *pParam;
    pParam = (DefaultCatchFilterParam *) pv;

    // the only valid parameter for DefaultCatchFilter so far
    _ASSERTE(pParam->pv == COMPLUS_EXCEPTION_EXECUTE_HANDLER);

    PEXCEPTION_RECORD er = ep->ExceptionRecord;
    DWORD code = er->ExceptionCode;

    if (code == STATUS_SINGLE_STEP || code == STATUS_BREAKPOINT)
    {
        return UserBreakpointFilter(ep);
    }

    // return EXCEPTION_EXECUTE_HANDLER to swallow the exception.
    return EXCEPTION_EXECUTE_HANDLER;
} // LONG DefaultCatchFilter()


//******************************************************************************
//
//  DefaultCatchNoSwallowFilter
//
//    The new default except filter (v2.0).  For user breakpoints, call out to UserBreakpointFilter().
//     Otherwise consults host policy and config file to return EXECUTE_HANDLER / CONTINUE_SEARCH.
//
//  Parameters:
//    pExceptionInfo    EXCEPTION_POINTERS for current exception
//    pv                A constant as an INT_PTR.  Must be COMPLUS_EXCEPTION_EXECUTE_HANDLER.
//
//  Returns:
//    EXCEPTION_CONTINUE_SEARCH     Generally returns this to let the exception go unhandled.
//    EXCEPTION_EXECUTE_HANDLER     May return this to swallow the exception.
//
// IMPORTANT!! READ ME!!
//
// This filter is very similar to DefaultCatchFilter, except when unhandled
//  exception policy/config dictate swallowing the exception.
// If you make any changes to this function, look to see if the other one also needs
//  the same change.
//
LONG DefaultCatchNoSwallowFilter(EXCEPTION_POINTERS *ep, PVOID pv)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    DefaultCatchFilterParam *pParam; pParam = (DefaultCatchFilterParam *) pv;

    // the only valid parameter for DefaultCatchFilter so far
    _ASSERTE(pParam->pv == COMPLUS_EXCEPTION_EXECUTE_HANDLER);

    PEXCEPTION_RECORD er = ep->ExceptionRecord;
    DWORD code = er->ExceptionCode;

    if (code == STATUS_SINGLE_STEP || code == STATUS_BREAKPOINT)
    {
        return UserBreakpointFilter(ep);
    }

    // If the exception is of a type that is always swallowed (ThreadAbort, AppDomainUnload)...
    if (ExceptionIsAlwaysSwallowed(ep))
    {   // ...return EXCEPTION_EXECUTE_HANDLER to swallow the exception.
        return EXCEPTION_EXECUTE_HANDLER;
    }

    // Otherwise, continue search. i.e. let the exception go unhandled (at least for now).
    return EXCEPTION_CONTINUE_SEARCH;
} // LONG DefaultCatchNoSwallowFilter()

// We keep a pointer to the previous unhandled exception filter.  After we install, we use
// this to call the previous guy.

static LPTOP_LEVEL_EXCEPTION_FILTER g_pOriginalUnhandledExceptionFilter = NULL;


BOOL InstallUnhandledExceptionFilter() {
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_MODE_ANY;
    STATIC_CONTRACT_FORBID_FAULT;

#ifndef TARGET_UNIX
    g_pOriginalUnhandledExceptionFilter = SetUnhandledExceptionFilter(COMUnhandledExceptionFilter);

    LOG((LF_EH, LL_INFO10, "InstallUnhandledExceptionFilter registered UEF with OS for CoreCLR!\n"));
#endif // !TARGET_UNIX

    // All done - successfully!
    return TRUE;
}

//
// Update the current throwable on the thread if necessary. If we're looking at one of our exceptions, and if the
// current throwable on the thread is NULL, then we'll set it to something more useful based on the
// LastThrownObject.
//
BOOL UpdateCurrentThrowable(PEXCEPTION_RECORD pExceptionRecord)
{
    STATIC_CONTRACT_THROWS;
    STATIC_CONTRACT_MODE_ANY;
    STATIC_CONTRACT_GC_TRIGGERS;

    BOOL useLastThrownObject = FALSE;

    Thread* pThread = GetThread();

    // GetThrowable needs cooperative.
    GCX_COOP();

    if ((pThread->GetThrowable() == NULL) && (pThread->LastThrownObject() != NULL))
    {
        // If GetThrowable is NULL and LastThrownObject is not, use lastThrownObject.
        //  In current (June 05) implementation, this is only used to pass to
        //  NotifyAppDomainsOfUnhandledException, which needs to get a throwable
        //  from somewhere, with which to notify the AppDomains.
        useLastThrownObject = TRUE;

        if (IsComPlusException(pExceptionRecord))
        {
#ifndef FEATURE_EH_FUNCLETS
            OBJECTREF oThrowable = pThread->LastThrownObject();

            // @TODO: we have a problem on Win64 where we won't have any place to
            //        store the throwable on an unhandled exception.  Currently this
            //        only effects the managed debugging services as they will try
            //        to inspect the thread to see what the throwable is on an unhandled
            //        exception.. (but clearly it needs to be fixed asap)
            //        We have the same problem in EEPolicy::LogFatalError().
            LOG((LF_EH, LL_INFO100, "UpdateCurrentThrowable: setting throwable to %s\n", (oThrowable == NULL) ? "NULL" : oThrowable->GetMethodTable()->GetDebugClassName()));
            pThread->SafeSetThrowables(oThrowable);
#endif // FEATURE_EH_FUNCLETS
        }
    }

    return useLastThrownObject;
}

//
// COMUnhandledExceptionFilter is used to catch all unhandled exceptions.
// The debugger will either handle the exception, attach a debugger, or
// notify an existing attached debugger.
//

struct SaveIPFilterParam
{
    SLOT ExceptionEIP;
};

LONG SaveIPFilter(EXCEPTION_POINTERS* ep, LPVOID pv)
{
    WRAPPER_NO_CONTRACT;

    SaveIPFilterParam *pParam = (SaveIPFilterParam *) pv;
    pParam->ExceptionEIP = (SLOT)GetIP(ep->ContextRecord);
    DefaultCatchFilterParam param(COMPLUS_EXCEPTION_EXECUTE_HANDLER);
    return DefaultCatchFilter(ep, &param);
}

//------------------------------------------------------------------------------
// Description
//   Does not call any previous UnhandledExceptionFilter.  The assumption is that
//    either it is inappropriate to call it (because we have elected to rip the
//    process without transitioning completely to the base of the thread), or
//    the caller has already consulted the previously installed UnhandledExceptionFilter.
//
//    So we know we are ripping and Watson is appropriate.
//
//    **** Note*****
//    This is a stack-sensitive function if we have an unhandled SO.
//    Do not allocate more than a few bytes on the stack or we risk taking an
//    AV while trying to throw up Watson.

// Parameters
//    pExceptionInfo -- information about the exception that caused the error.
//           If the error is not the result of an exception, pass NULL for this
//           parameter
//
// Returns
//   EXCEPTION_CONTINUE_SEARCH -- we've done anything we will with the exception.
//      As far as the runtime is concerned, the process is doomed.
//   EXCEPTION_CONTINUE_EXECUTION -- means a debugger "caught" the exception and
//      wants to continue running.
//   EXCEPTION_EXECUTE_HANDLER -- CoreCLR only, and only when not running as a UEF.
//      Returned only if the host has asked us to swallow unhandled exceptions on
//      managed threads in an AD they (the host) creates.
//------------------------------------------------------------------------------
LONG InternalUnhandledExceptionFilter_Worker(
    EXCEPTION_POINTERS *pExceptionInfo)     // Information about the exception
{
    STATIC_CONTRACT_THROWS;
    STATIC_CONTRACT_GC_TRIGGERS;
    STATIC_CONTRACT_MODE_ANY;

#ifdef _DEBUG
    static int fBreakOnUEF = -1;
    if (fBreakOnUEF==-1) fBreakOnUEF = CLRConfig::GetConfigValue(CLRConfig::INTERNAL_BreakOnUEF);
    _ASSERTE(!fBreakOnUEF);
#endif

    STRESS_LOG2(LF_EH, LL_INFO10, "In InternalUnhandledExceptionFilter_Worker, Exception = %x, sp = %p\n",
                                    pExceptionInfo->ExceptionRecord->ExceptionCode, GetCurrentSP());

    // If we can't enter the EE, done.
    if (g_fForbidEnterEE)
    {
        LOG((LF_EH, LL_INFO100, "InternalUnhandledExceptionFilter_Worker: g_fForbidEnterEE is TRUE\n"));
        return EXCEPTION_CONTINUE_SEARCH;
    }

    // We don't do anything when this is called from an unmanaged thread.
    Thread *pThread = GetThreadNULLOk();

#ifdef _DEBUG
    static bool bBreakOnUncaught = false;
    static int fBreakOnUncaught = 0;

    if (!bBreakOnUncaught)
    {
        fBreakOnUncaught = CLRConfig::GetConfigValue(CLRConfig::INTERNAL_BreakOnUncaughtException);
        bBreakOnUncaught = true;
    }
    if (fBreakOnUncaught != 0)
    {
        if (pExceptionInfo->ExceptionRecord->ExceptionCode == STATUS_STACK_OVERFLOW)
        {
            // if we've got an uncaught SO, we don't have enough stack to pop a debug break.  So instead,
            // loop infinitely and we can attach a debugger at that point and break in.
            LOG((LF_EH, LL_INFO100, "InternalUnhandledExceptionFilter_Worker: Infinite loop on uncaught SO\n"));
            for ( ;; )
            {
            }
        }
        else
        {
            LOG((LF_EH, LL_INFO100, "InternalUnhandledExceptionFilter_Worker: ASSERTING on uncaught\n"));
            _ASSERTE(!"BreakOnUnCaughtException");
        }
    }
#endif

    // This shouldn't be possible, but MSVC re-installs us... for now, just bail if this happens.
    if (g_fNoExceptions)
    {
        return EXCEPTION_CONTINUE_SEARCH;
    }

    // Are we looking at a stack overflow here?
    if ((pThread !=  NULL) && !pThread->DetermineIfGuardPagePresent())
    {
        g_fForbidEnterEE = true;
    }

#ifdef DEBUGGING_SUPPORTED

    // Mark that this exception has gone unhandled. At the moment only the debugger will
    // ever look at this flag. This should come before any user-visible side effect of an exception
    // being unhandled as seen from managed code or from a debugger. These include the
    // managed unhandled notification callback, execution of catch/finally clauses,
    // receiving the managed debugger unhandled exception event,
    // the OS sending the debugger 2nd pass native exception notification, etc.
    //
    // This needs to be done before the check for TSNC_ProcessedUnhandledException because it is perfectly
    // legitimate (though rare) for the debugger to be inspecting exceptions which are nested in finally
    // clauses that run after an unhandled exception has already occurred on the thread
    if ((pThread != NULL) && pThread->IsExceptionInProgress())
    {
        LOG((LF_EH, LL_INFO1000, "InternalUnhandledExceptionFilter_Worker: Set unhandled exception flag at %p\n",
            pThread->GetExceptionState()->GetFlags() ));
        pThread->GetExceptionState()->GetFlags()->SetUnhandled();
    }
#endif

    // If we have already done unhandled exception processing for this thread, then
    // simply return back. See comment in threads.h for details for the flag
    // below.
    //
    if (pThread && pThread->HasThreadStateNC(Thread::TSNC_ProcessedUnhandledException))
    {
        LOG((LF_EH, LL_INFO100, "InternalUnhandledExceptionFilter_Worker: have already processed unhandled exception for this thread.\n"));
        return EXCEPTION_CONTINUE_SEARCH;
    }

    LOG((LF_EH, LL_INFO100, "InternalUnhandledExceptionFilter_Worker: Handling\n"));

    struct Param : SaveIPFilterParam
    {
        EXCEPTION_POINTERS *pExceptionInfo;
        Thread *pThread;
        LONG retval;
        BOOL fIgnore;
    }; Param param;

    param.ExceptionEIP = 0;
    param.pExceptionInfo = pExceptionInfo;
    param.pThread = pThread;
    param.retval = EXCEPTION_CONTINUE_SEARCH;   // Result of UEF filter.

    // Is this a particular kind of exception that we'd like to ignore?
    param.fIgnore = ((param.pExceptionInfo->ExceptionRecord->ExceptionCode == STATUS_BREAKPOINT) ||
                    (param.pExceptionInfo->ExceptionRecord->ExceptionCode == STATUS_SINGLE_STEP));

    PAL_TRY(Param *, pParam, &param)
    {
        // If fIgnore, then this is some sort of breakpoint, not a "normal" unhandled exception.  But, the
        //  breakpoint is due to an int3 or debugger step instruction, not due to calling Debugger.Break()
        TypeOfReportedError tore = pParam->fIgnore ? TypeOfReportedError::NativeBreakpoint : TypeOfReportedError::UnhandledException;

        //
        // If this exception is on a thread without managed code, then report this as a NativeThreadUnhandledException
        //
        // The thread object may exist if there was once managed code on the stack, but if the exception never
        // bubbled thru managed code, ie no managed code is on its stack, then this is a native unhandled exception
        //
        // Ignore breakpoints and single-step.
        if (!pParam->fIgnore)
        {   // Possibly interesting exception.  Is there no Thread at all?  Or, is there a Thread,
            //  but with no exception at all on it?
            if ((pParam->pThread == NULL) ||
                (pParam->pThread->IsThrowableNull() && pParam->pThread->IsLastThrownObjectNull()) )
            {   // Whatever this exception is, we don't know about it.  Treat as Native.
                tore = TypeOfReportedError::NativeThreadUnhandledException;
            }
        }

        // If there is no throwable on the thread, go ahead and update from the last thrown exception if possible.
        // Note: don't do this for exceptions that we're going to ignore below anyway...
        BOOL useLastThrownObject = FALSE;
        if (!pParam->fIgnore && (pParam->pThread != NULL))
        {
            useLastThrownObject = UpdateCurrentThrowable(pParam->pExceptionInfo->ExceptionRecord);
        }

#ifdef DEBUGGING_SUPPORTED

        LOG((LF_EH, LL_INFO100, "InternalUnhandledExceptionFilter_Worker: Notifying Debugger...\n"));

        // If we are using the throwable in LastThrownObject, mark that it is now unhandled
        if ((pParam->pThread != NULL) && useLastThrownObject)
        {
            LOG((LF_EH, LL_INFO1000, "InternalUnhandledExceptionFilter_Worker: Set lto is unhandled\n"));
            pParam->pThread->MarkLastThrownObjectUnhandled();
        }

        //
        // We don't want the managed debugger to try to "intercept" breakpoints
        // or singlestep exceptions.
        // TODO: why does the exception handling code need to set this? Shouldn't the debugger code
        // be able to determine what it can/should intercept?
        if ((pParam->pThread != NULL) && pParam->pThread->IsExceptionInProgress() && pParam->fIgnore)
        {
            pParam->pThread->GetExceptionState()->GetFlags()->SetDebuggerInterceptNotPossible();
        }


        if (pParam->pThread != NULL)
        {
            BOOL fIsProcessTerminating = TRUE;

            // In CoreCLR, we can be asked to not let an exception go unhandled on managed threads in a given AppDomain.
            // If the exception reaches the top of the thread's stack, we simply deliver AppDomain's UnhandledException event and
            // return back to the filter, instead of letting the process terminate because of unhandled exception.

            // Below is how we perform the check:
            //
            // 1) The flag is specified on the AD when it is created by the host and all managed threads created
            //    in such an AD will inherit the flag. For non-finalizer and non-threadpool threads, we check the flag against the thread.
            // 2) The finalizer thread always switches to the AD of the object that is going to be finalized. Thus,
            //    while it wont have the flag specified, the AD it switches to will.
            // 3) The threadpool thread also switches to the correct AD before executing the request. The thread wont have the
            //    flag specified, but the AD it switches to will.

            // This code must only be exercised when running as a normal filter; returning
            // EXCEPTION_EXECUTE_HANDLER is not valid if this code is being invoked from
            // the UEF.
            // Fortunately, we should never get into this case, since the thread flag about
            // ignoring unhandled exceptions cannot be set on the default domain.

            if (IsFinalizerThread() || (pParam->pThread->IsThreadPoolThread()))
                fIsProcessTerminating = !(pParam->pThread->GetDomain()->IgnoreUnhandledExceptions());
            else
                fIsProcessTerminating = !(pParam->pThread->HasThreadStateNC(Thread::TSNC_IgnoreUnhandledExceptions));

#ifndef TARGET_UNIX
            // Setup the watson bucketing details for UE processing.
            // do this before notifying appdomains of the UE so if an AD attempts to
            // retrieve the bucket params in the UE event handler it gets the correct data.
            SetupWatsonBucketsForUEF(useLastThrownObject);
#endif // !TARGET_UNIX

            // Send notifications to the AppDomains.
            NotifyAppDomainsOfUnhandledException(pParam->pExceptionInfo, NULL, useLastThrownObject, fIsProcessTerminating /*isTerminating*/);

            // If the process is not terminating, then return back to the filter and ask it to execute
            if (!fIsProcessTerminating)
            {
                pParam->retval = EXCEPTION_EXECUTE_HANDLER;
                goto lDone;
            }
        }
        else
        {
            LOG((LF_EH, LL_INFO100, "InternalUnhandledExceptionFilter_Worker: Not collecting bucket information as thread object does not exist\n"));
        }

        // AppDomain.UnhandledException event could have thrown an exception that would have gone unhandled in managed code.
        // The runtime swallows all such exceptions. Hence, if we are not using LastThrownObject and the current LastThrownObject
        // is not the same as the one in active exception tracker (if available), then update the last thrown object.
        if ((pParam->pThread != NULL) && (!useLastThrownObject))
        {
            GCX_COOP_NO_DTOR();

            OBJECTREF oThrowable = pParam->pThread->GetThrowable();
            if ((oThrowable != NULL) && (pParam->pThread->LastThrownObject() != oThrowable))
            {
                pParam->pThread->SafeSetLastThrownObject(oThrowable);
                LOG((LF_EH, LL_INFO100, "InternalUnhandledExceptionFilter_Worker: Resetting the LastThrownObject as it appears to have changed.\n"));
            }

            GCX_COOP_NO_DTOR_END();
        }

        // Launch Watson and see if we want to debug the process
        //
        // Note that we need to do this before "ignoring" exceptions like
        // breakpoints and single step exceptions
        //

        LOG((LF_EH, LL_INFO100, "InternalUnhandledExceptionFilter_Worker: Launching Watson at sp %p ...\n", GetCurrentSP()));

        if (WatsonLastChance(pParam->pThread, pParam->pExceptionInfo, tore) == EXCEPTION_CONTINUE_EXECUTION)
        {
            LOG((LF_EH, LL_INFO100, "InternalUnhandledExceptionFilter_Worker: debugger ==> EXCEPTION_CONTINUE_EXECUTION\n"));
            pParam->retval = EXCEPTION_CONTINUE_EXECUTION;
            goto lDone;
        }

        LOG((LF_EH, LL_INFO100, "InternalUnhandledExceptionFilter_Worker: ... returned.\n"));
#endif // DEBUGGING_SUPPORTED


        //
        // Except for notifying debugger, ignore exception if unmanaged, or
        // if it's a debugger-generated exception or user breakpoint exception.
        //
        if (tore.GetType() == TypeOfReportedError::NativeThreadUnhandledException)
        {
            pParam->retval = EXCEPTION_CONTINUE_SEARCH;
#if defined(FEATURE_EVENT_TRACE) && !defined(TARGET_UNIX)
            DoReportForUnhandledNativeException(pParam->pExceptionInfo);
#endif
            goto lDone;
        }

        if (pParam->fIgnore)
        {
            LOG((LF_EH, LL_INFO100, "InternalUnhandledExceptionFilter_Worker, ignoring the exception\n"));
            pParam->retval = EXCEPTION_CONTINUE_SEARCH;
#if defined(FEATURE_EVENT_TRACE) && !defined(TARGET_UNIX)
            DoReportForUnhandledNativeException(pParam->pExceptionInfo);
#endif
            goto lDone;
        }

        LOG((LF_EH, LL_INFO100, "InternalUnhandledExceptionFilter_Worker: Calling DefaultCatchHandler\n"));

        // Call our default catch handler to do the managed unhandled exception work.
        DefaultCatchHandler(pParam->pExceptionInfo, NULL, useLastThrownObject,
            TRUE /*isTerminating*/, FALSE /*isThreadBaseFIlter*/, FALSE /*sendAppDomainEvents*/, TRUE /* sendWindowsEventLog */);

lDone: ;
    }
    PAL_EXCEPT_FILTER (SaveIPFilter)
    {
        // Should never get here.
#ifdef _DEBUG
        char buffer[200];
        sprintf_s(buffer, 200, "\nInternal error: Uncaught exception was thrown from IP = %p in UnhandledExceptionFilter_Worker on thread 0x%08x\n",
                param.ExceptionEIP, ((GetThreadNULLOk() == NULL) ? NULL : GetThread()->GetThreadId()));
        PrintToStdErrA(buffer);
        _ASSERTE(!"Unexpected exception in UnhandledExceptionFilter_Worker");
#endif
        EEPOLICY_HANDLE_FATAL_ERROR(COR_E_EXECUTIONENGINE)
    }
    PAL_ENDTRY;

    return param.retval;
} // LONG InternalUnhandledExceptionFilter_Worker()

//------------------------------------------------------------------------------
// Description
//   Calls our InternalUnhandledExceptionFilter for Watson at the appropriate
//   place in the chain.
//
//   For non-side-by-side CLR's, we call everyone else's UEF first.
//
//   For side-by-side CLR's, we call our own filter first. This is primary
//      so Whidbey's UEF won't put up a second dialog box. In exchange,
//      side-by-side CLR's won't put up UI's unless the EH really came
//      from that instance's managed code.
//
// Parameters
//    pExceptionInfo -- information about the exception that caused the error.
//           If the error is not the result of an exception, pass NULL for this
//           parameter
//
// Returns
//   EXCEPTION_CONTINUE_SEARCH -- we've done anything we will with the exception.
//      As far as the runtime is concerned, the process is doomed.
//   EXCEPTION_CONTINUE_EXECUTION -- means a debugger "caught" the exception and
//      wants to continue running.
//------------------------------------------------------------------------------
LONG InternalUnhandledExceptionFilter(
    EXCEPTION_POINTERS *pExceptionInfo)     // Information about the exception
{
    STATIC_CONTRACT_THROWS;
    STATIC_CONTRACT_GC_TRIGGERS;
    STATIC_CONTRACT_MODE_ANY;

    LOG((LF_EH, LL_INFO100, "InternalUnhandledExceptionFilter: at sp %p.\n", GetCurrentSP()));

    // Side-by-side UEF: Calls ours first, then the rest (unless we put up a UI for
    // the exception.)

    LONG    retval = InternalUnhandledExceptionFilter_Worker(pExceptionInfo);   // Result of UEF filter.

    // Keep looking, or done?
    if (retval != EXCEPTION_CONTINUE_SEARCH)
    {   // done.
        return retval;
    }

#ifdef HOST_WINDOWS
    CreateCrashDumpIfEnabled();
#endif

    BOOL fShouldOurUEFDisplayUI = ShouldOurUEFDisplayUI(pExceptionInfo);

    // If this is a managed exception thrown by this instance of the CLR, the exception is no one's
    // business but ours (nudge, nudge: Whidbey). Break the UEF chain at this point.
    if (fShouldOurUEFDisplayUI)
    {
        return retval;
    }

    if (g_pOriginalUnhandledExceptionFilter != NULL)
    {
        return g_pOriginalUnhandledExceptionFilter(pExceptionInfo);
    }

    return retval;

} // LONG InternalUnhandledExceptionFilter()


// Represent the value of USE_ENTRYPOINT_FILTER as passed in the property bag to the host during construction
static bool s_useEntryPointFilterCorhostProperty = false;

void ParseUseEntryPointFilter(LPCWSTR value)
{
    // set s_useEntryPointFilter true if value != "0"
    if (value && (_wcsicmp(value, W("0")) != 0))
    {
        s_useEntryPointFilterCorhostProperty = true;
    }
}

bool GetUseEntryPointFilter()
{
#ifdef TARGET_WINDOWS // This feature has only been tested on Windows, keep it disabled on other platforms
    static bool s_useEntryPointFilterEnv = CLRConfig::GetConfigValue(CLRConfig::INTERNAL_UseEntryPointFilter) != 0;

    return s_useEntryPointFilterCorhostProperty || s_useEntryPointFilterEnv;
#else
    return false;
#endif

}

// This filter is used to trigger unhandled exception processing for the entrypoint thread
LONG EntryPointFilter(PEXCEPTION_POINTERS pExceptionInfo, PVOID _pData)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    LONG ret = -1;

    ret = CLRNoCatchHandler(pExceptionInfo, _pData);

    if (ret != EXCEPTION_CONTINUE_SEARCH)
    {
        return ret;
    }

    if (!GetUseEntryPointFilter())
    {
        return EXCEPTION_CONTINUE_SEARCH;
    }

    Thread* pThread = GetThreadNULLOk();
    if (pThread && !pThread->HasThreadStateNC(Thread::TSNC_ProcessedUnhandledException))
    {
        // Invoke the UEF worker to perform unhandled exception processing
        ret = InternalUnhandledExceptionFilter_Worker (pExceptionInfo);

        // Set the flag that we have done unhandled exception processing for this thread
        // so that we dont duplicate the effort in the UEF.
        //
        // For details on this flag, refer to threads.h.
        LOG((LF_EH, LL_INFO100, "EntryPointFilter: setting TSNC_ProcessedUnhandledException\n"));
        pThread->SetThreadStateNC(Thread::TSNC_ProcessedUnhandledException);

        if (ret == EXCEPTION_EXECUTE_HANDLER)
        {
            // Do not swallow the exception, we just want to log it
            return EXCEPTION_CONTINUE_SEARCH;
        }
    }

    return ret;
}

//------------------------------------------------------------------------------
// Description
//   The actual UEF.  Defers to InternalUnhandledExceptionFilter.
//
// Updated to be in its own code segment named CLR_UEF_SECTION_NAME to prevent
// "VirtualProtect" calls from affecting its pages and thus, its
// invocation. For details, see the comment within the implementation of
// ClrVirtualProtect.
//
// Parameters
//   pExceptionInfo -- information about the exception
//
// Returns
//   the result of calling InternalUnhandledExceptionFilter
//------------------------------------------------------------------------------
#if !defined(TARGET_UNIX)
#pragma code_seg(push, uef, CLR_UEF_SECTION_NAME)
#endif // !TARGET_UNIX
LONG __stdcall COMUnhandledExceptionFilter(     // EXCEPTION_CONTINUE_SEARCH or EXCEPTION_CONTINUE_EXECUTION
    EXCEPTION_POINTERS *pExceptionInfo)         // Information about the exception.
{
    STATIC_CONTRACT_THROWS;
    STATIC_CONTRACT_GC_TRIGGERS;
    STATIC_CONTRACT_MODE_ANY;

    LONG retVal = EXCEPTION_CONTINUE_SEARCH;

    // Incase of unhandled exceptions on managed threads, we kick in our UE processing at the thread base and also invoke
    // UEF callbacks that various runtimes have registered with us. Once the callbacks return, we return back to the OS
    // to give other registered UEFs a chance to do their custom processing.
    //
    // If the topmost UEF registered with the OS belongs to mscoruef.dll (or someone chained back to its UEF callback),
    // it will start invoking the UEF callbacks (which is this function, COMUnhandledExceptionFiler) registered by
    // various runtimes again.
    //
    // Thus, check if this UEF has already been invoked in context of this thread and runtime and if so, dont invoke it again.
    if (GetThreadNULLOk() && (GetThread()->HasThreadStateNC(Thread::TSNC_ProcessedUnhandledException)))
    {
        LOG((LF_EH, LL_INFO10, "Exiting COMUnhandledExceptionFilter since we have already done UE processing for this thread!\n"));
        return retVal;
    }


    retVal = InternalUnhandledExceptionFilter(pExceptionInfo);

    // If thread object exists, mark that this thread has done unhandled exception processing
    if (GetThreadNULLOk())
    {
        LOG((LF_EH, LL_INFO100, "COMUnhandledExceptionFilter: setting TSNC_ProcessedUnhandledException\n"));
        GetThread()->SetThreadStateNC(Thread::TSNC_ProcessedUnhandledException);
    }

    return retVal;
} // LONG __stdcall COMUnhandledExceptionFilter()
#if !defined(TARGET_UNIX)
#pragma code_seg(pop, uef)
#endif // !TARGET_UNIX

void PrintStackTraceToStdout();

static SString GetExceptionMessageWrapper(Thread* pThread, OBJECTREF throwable)
{
    STATIC_CONTRACT_THROWS;
    STATIC_CONTRACT_MODE_COOPERATIVE;
    STATIC_CONTRACT_GC_TRIGGERS;

    StackSString result;

    INSTALL_NESTED_EXCEPTION_HANDLER(pThread->GetFrame());
    GetExceptionMessage(throwable, result);
    UNINSTALL_NESTED_EXCEPTION_HANDLER();

    return SString{ result };
}

void STDMETHODCALLTYPE
DefaultCatchHandlerExceptionMessageWorker(Thread* pThread,
                                          OBJECTREF throwable,
                                          _Inout_updates_(buf_size) WCHAR *buf,
                                          const int buf_size,
                                          BOOL sendWindowsEventLog)
{
    GCPROTECT_BEGIN(throwable);
    if (throwable != NULL)
    {
        if (FAILED(UtilLoadResourceString(CCompRC::Error, IDS_EE_UNHANDLED_EXCEPTION, buf, buf_size)))
        {
            wcsncpy_s(buf, buf_size, SZ_UNHANDLED_EXCEPTION, SZ_UNHANDLED_EXCEPTION_CHARLEN);
        }

        PrintToStdErrW(buf);
        PrintToStdErrA(" ");

        SString message = GetExceptionMessageWrapper(pThread, throwable);

        if (!message.IsEmpty())
        {
            PrintToStdErrW(message);
        }

        PrintToStdErrA("\n");

#if defined(FEATURE_EVENT_TRACE) && !defined(TARGET_UNIX)
        // Send the log to Windows Event Log
        if (sendWindowsEventLog && ShouldLogInEventLog())
        {
            EX_TRY
            {
                EventReporter reporter(EventReporter::ERT_UnhandledException);

                if (IsException(throwable->GetMethodTable()))
                {
                    if (!message.IsEmpty())
                    {
                        reporter.AddDescription(message);
                    }
                    reporter.Report();
                }
                else
                {
                    StackSString s;
                    TypeString::AppendType(s, TypeHandle(throwable->GetMethodTable()), TypeString::FormatNamespace | TypeString::FormatFullInst);
                    reporter.AddDescription(s);
                    LogCallstackForEventReporter(reporter);
                }
            }
            EX_CATCH
            {
            }
            EX_END_CATCH(SwallowAllExceptions);
        }
#endif
    }
    GCPROTECT_END();
}

//******************************************************************************
// DefaultCatchHandler -- common processing for otherwise uncaught exceptions.
//******************************************************************************
void STDMETHODCALLTYPE
DefaultCatchHandler(PEXCEPTION_POINTERS pExceptionPointers,
                    OBJECTREF *pThrowableIn,
                    BOOL useLastThrownObject,
                    BOOL isTerminating,
                    BOOL isThreadBaseFilter,
                    BOOL sendAppDomainEvents,
                    BOOL sendWindowsEventLog)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    // <TODO> The strings in here should be translatable.</TODO>
    LOG((LF_EH, LL_INFO10, "In DefaultCatchHandler\n"));

#if defined(_DEBUG)
    static bool bHaveInitialized_BreakOnUncaught = false;
    enum BreakOnUncaughtAction {
        breakOnNone     =   0,          // Default.
        breakOnAll      =   1,          // Always break.
        breakSelective  =   2,          // Break on exceptions application can catch,
                                        //  but not ThreadAbort, AppdomainUnload
        breakOnMax      =   2
    };
    static DWORD breakOnUncaught = breakOnNone;

    if (!bHaveInitialized_BreakOnUncaught)
    {
        breakOnUncaught = CLRConfig::GetConfigValue(CLRConfig::INTERNAL_BreakOnUncaughtException);
        if (breakOnUncaught > breakOnMax)
        {   // Could turn it off completely, or turn into legal value.  Since it is debug code, be accommodating.
            breakOnUncaught = breakOnAll;
        }
        bHaveInitialized_BreakOnUncaught = true;
    }

    if (breakOnUncaught == breakOnAll)
    {
        _ASSERTE(!"BreakOnUnCaughtException");
    }

    int suppressSelectiveBreak = false; // to filter for the case where breakOnUncaught == "2"
#endif

    Thread *pThread = GetThreadNULLOk();

    //     The following reduces a window for a race during shutdown.
    if (!pThread)
    {
        _ASSERTE(g_fEEShutDown);
        return;
    }

    _ASSERTE(pThread);

    ThreadPreventAsyncHolder prevAsync;

    GCX_COOP();

    OBJECTREF throwable;

    if (pThrowableIn != NULL)
    {
        throwable = *pThrowableIn;
    }
    else if (useLastThrownObject)
    {
        throwable = pThread->LastThrownObject();
    }
    else
    {
        throwable = pThread->GetThrowable();
    }

    // If we've got no managed object, then we can't send an event or print a message, so we just return.
    if (throwable == NULL)
    {
#ifdef LOGGING
        if (!pThread->IsRudeAbortInitiated())
        {
            LOG((LF_EH, LL_INFO10, "Unhandled exception, throwable == NULL\n"));
        }
#endif

        return;
    }

#ifdef _DEBUG
    DWORD unbreakableLockCount = 0;
    // Do not care about lock check for unhandled exception.
    while (pThread->HasUnbreakableLock())
    {
        pThread->DecUnbreakableLockCount();
        unbreakableLockCount ++;
    }
    BOOL fOwnsSpinLock = pThread->HasThreadStateNC(Thread::TSNC_OwnsSpinLock);
    if (fOwnsSpinLock)
    {
        pThread->ResetThreadStateNC(Thread::TSNC_OwnsSpinLock);
    }
#endif

    GCPROTECT_BEGIN(throwable);
    //BOOL IsStackOverflow = (throwable->GetMethodTable() == g_pStackOverflowExceptionClass);
    BOOL IsOutOfMemory = (throwable->GetMethodTable() == g_pOutOfMemoryExceptionClass);

    // Notify the AppDomain that we have taken an unhandled exception.  Can't notify of stack overflow -- guard
    // page is not yet reset.
    BOOL SentEvent = FALSE;

    // Send up the unhandled exception appdomain event.
    if (sendAppDomainEvents)
    {
        SentEvent = NotifyAppDomainsOfUnhandledException(pExceptionPointers, &throwable, useLastThrownObject, isTerminating);
    }

    const int buf_size = 128;
    WCHAR buf[buf_size] = {0};

    {
        EX_TRY
        {
            EX_TRY
            {
                // If this isn't ThreadAbortException, we want to print a stack trace to indicate why this thread abruptly
                // terminated. Exceptions kill threads rarely enough that an uncached name check is reasonable.
                BOOL        dump = TRUE;

                if (/*IsStackOverflow ||*/
                    !pThread->DetermineIfGuardPagePresent() ||
                    IsOutOfMemory)
                {
                    // We have to be very careful.  If we walk off the end of the stack, the process will just
                    // die. e.g. IsAsyncThreadException() and Exception.ToString both consume too much stack -- and can't
                    // be called here.
                    dump = FALSE;

                    if (IsOutOfMemory)
                    {
                        PrintToStdErrA("Out of memory.\n");
                    }
                    else
                    {
                        PrintToStdErrA("Stack overflow.\n");
                    }
                }
                else if (SentEvent || IsAsyncThreadException(&throwable))
                {
                    // We don't print anything on async exceptions, like ThreadAbort.
                    dump = FALSE;
                    INDEBUG(suppressSelectiveBreak=TRUE);
                }

                // Finally, should we print the message?
                if (dump)
                {
                    // this is stack heavy because of the CQuickWSTRBase, so we break it out
                    // and don't have to carry the weight through our other code paths.
                    DefaultCatchHandlerExceptionMessageWorker(pThread, throwable, buf, buf_size, sendWindowsEventLog);
                }
            }
            EX_CATCH
            {
                LOG((LF_EH, LL_INFO10, "Exception occurred while processing uncaught exception\n"));
                UtilLoadStringRC(IDS_EE_EXCEPTION_TOSTRING_FAILED, buf, buf_size);
                PrintToStdErrA("\n   ");
                PrintToStdErrW(buf);
                PrintToStdErrA("\n");
            }
            EX_END_CATCH(SwallowAllExceptions);
        }
        EX_CATCH
        {   // If we got here, we can't even print the localized error message.  Print non-localized.
            LOG((LF_EH, LL_INFO10, "Exception occurred while logging processing uncaught exception\n"));
            PrintToStdErrA("\n   Error: Can't print exception string because Exception.ToString() failed.\n");
        }
        EX_END_CATCH(SwallowAllExceptions);
    }

#if defined(_DEBUG)
    if ((breakOnUncaught == breakSelective) && !suppressSelectiveBreak)
    {
        _ASSERTE(!"BreakOnUnCaughtException");
    }
#endif // defined(_DEBUG)

    FlushLogging();     // Flush any logging output
    GCPROTECT_END();

#ifdef _DEBUG
    // Do not care about lock check for unhandled exception.
    while (unbreakableLockCount)
    {
        pThread->IncUnbreakableLockCount();
        unbreakableLockCount --;
    }
    if (fOwnsSpinLock)
    {
        pThread->SetThreadStateNC(Thread::TSNC_OwnsSpinLock);
    }
#endif
} // DefaultCatchHandler()


//******************************************************************************
// NotifyAppDomainsOfUnhandledException -- common processing for otherwise uncaught exceptions.
//******************************************************************************
BOOL NotifyAppDomainsOfUnhandledException(
    PEXCEPTION_POINTERS pExceptionPointers,
    OBJECTREF   *pThrowableIn,
    BOOL        useLastThrownObject,
    BOOL        isTerminating)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

#ifdef _DEBUG
    static int fBreakOnNotify = -1;
    if (fBreakOnNotify==-1) fBreakOnNotify = CLRConfig::GetConfigValue(CLRConfig::INTERNAL_BreakOnNotify);
    _ASSERTE(!fBreakOnNotify);
#endif

    BOOL SentEvent = FALSE;

    LOG((LF_EH, LL_INFO10, "In NotifyAppDomainsOfUnhandledException\n"));

    Thread *pThread = GetThreadNULLOk();

    //     The following reduces a window for a race during shutdown.
    if (!pThread)
    {
        _ASSERTE(g_fEEShutDown);
        return FALSE;
    }

    ThreadPreventAsyncHolder prevAsync;

    GCX_COOP();

    OBJECTREF throwable;

    if (pThrowableIn != NULL)
    {
        throwable = *pThrowableIn;
    }
    else if (useLastThrownObject)
    {
        throwable = pThread->LastThrownObject();
    }
    else
    {
        throwable = pThread->GetThrowable();
    }

    // If we've got no managed object, then we can't send an event, so we just return.
    if (throwable == NULL)
    {
        return FALSE;
    }

#ifdef _DEBUG
    DWORD unbreakableLockCount = 0;
    // Do not care about lock check for unhandled exception.
    while (pThread->HasUnbreakableLock())
    {
        pThread->DecUnbreakableLockCount();
        unbreakableLockCount ++;
    }
    BOOL fOwnsSpinLock = pThread->HasThreadStateNC(Thread::TSNC_OwnsSpinLock);
    if (fOwnsSpinLock)
    {
        pThread->ResetThreadStateNC(Thread::TSNC_OwnsSpinLock);
    }
#endif

    GCPROTECT_BEGIN(throwable);

    // Notify the AppDomain that we have taken an unhandled exception.  Can't notify of stack overflow -- guard
    // page is not yet reset.

    // Send up the unhandled exception appdomain event.
    if (pThread->DetermineIfGuardPagePresent())
    {
        // x86 only
#if !defined(FEATURE_EH_FUNCLETS)
        // If the Thread object's exception state's exception pointers
        //  is null, use the passed-in pointer.
        BOOL bSetPointers = FALSE;

        ThreadExceptionState* pExceptionState = pThread->GetExceptionState();

        if (pExceptionState->GetExceptionPointers() == NULL)
        {
            bSetPointers = TRUE;
            pExceptionState->SetExceptionPointers(pExceptionPointers);
        }

#endif // !defined(FEATURE_EH_FUNCLETS)

        INSTALL_NESTED_EXCEPTION_HANDLER(pThread->GetFrame());

        // This guy will never throw, but it will need a spot to store
        // any nested exceptions it might find.
        SentEvent = AppDomain::OnUnhandledException(&throwable, isTerminating);

        UNINSTALL_NESTED_EXCEPTION_HANDLER();

#if !defined(FEATURE_EH_FUNCLETS)

        if (bSetPointers)
        {
            pExceptionState->SetExceptionPointers(NULL);
        }

#endif // !defined(FEATURE_EH_FUNCLETS)

    }

    GCPROTECT_END();

#ifdef _DEBUG
    // Do not care about lock check for unhandled exception.
    while (unbreakableLockCount)
    {
        pThread->IncUnbreakableLockCount();
        unbreakableLockCount --;
    }
    if (fOwnsSpinLock)
    {
        pThread->SetThreadStateNC(Thread::TSNC_OwnsSpinLock);
    }
#endif

    return SentEvent;

} // NotifyAppDomainsOfUnhandledException()


//******************************************************************************
//
//  ThreadBaseExceptionFilter_Worker
//
//    The return from the function can be EXCEPTION_CONTINUE_SEARCH to let an
//     exception go unhandled.  This is the default behaviour (starting in v2.0),
//     but can be overridden by hosts or by config file.
//    When the behaviour is overridden, the return will be EXCEPTION_EXECUTE_HANDLER
//     to swallow the exception.
//    Note that some exceptions are always swallowed: ThreadAbort, and AppDomainUnload.
//
//  Parameters:
//    pExceptionInfo    EXCEPTION_POINTERS for current exception
//    _location         A constant as an INT_PTR.  Tells the context from whence called.
//    swallowing        Are we swallowing unhandled exceptions based on policy?
//
//  Returns:
//    EXCEPTION_CONTINUE_SEARCH     Generally returns this to let the exception go unhandled.
//    EXCEPTION_EXECUTE_HANDLER     May return this to swallow the exception.
//
static LONG ThreadBaseExceptionFilter_Worker(PEXCEPTION_POINTERS pExceptionInfo,
                                             PVOID pvParam,
                                             BOOL swallowing)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    LOG((LF_EH, LL_INFO100, "ThreadBaseExceptionFilter_Worker: Enter\n"));

    ThreadBaseExceptionFilterParam *pParam = (ThreadBaseExceptionFilterParam *) pvParam;
    UnhandledExceptionLocation location = pParam->location;

    _ASSERTE(!g_fNoExceptions);

    Thread* pThread = GetThread();

#ifdef _DEBUG
    if (CLRConfig::GetConfigValue(CLRConfig::INTERNAL_BreakOnUncaughtException) &&
        !(swallowing && ExceptionIsAlwaysSwallowed(pExceptionInfo)) &&
        !(location == ClassInitUnhandledException && pThread->IsRudeAbortInitiated()))
        _ASSERTE(!"BreakOnUnCaughtException");
#endif

    BOOL doDefault =  ((location != ClassInitUnhandledException) &&
                       (pExceptionInfo->ExceptionRecord->ExceptionCode != STATUS_BREAKPOINT) &&
                       (pExceptionInfo->ExceptionRecord->ExceptionCode != STATUS_SINGLE_STEP));

    if (swallowing)
    {
        // No, don't swallow unhandled exceptions...
        // ...except if the exception is of a type that is always swallowed (ThreadAbort, ...)
        if (ExceptionIsAlwaysSwallowed(pExceptionInfo))
        {   // ...return EXCEPTION_EXECUTE_HANDLER to swallow the exception anyway.
            return EXCEPTION_EXECUTE_HANDLER;
        }

        #ifdef _DEBUG
        if (CLRConfig::GetConfigValue(CLRConfig::INTERNAL_BreakOnUncaughtException))
            _ASSERTE(!"BreakOnUnCaughtException");
        #endif

        // ...so, continue search. i.e. let the exception go unhandled.
        return EXCEPTION_CONTINUE_SEARCH;
    }

#ifdef DEBUGGING_SUPPORTED
    // If there's a debugger (and not doing a thread abort), give the debugger a shot at the exception.
    // If the debugger is going to try to continue the exception, it will return ContinueException (which
    // we see here as EXCEPTION_CONTINUE_EXECUTION).
    if (!pThread->IsAbortRequested())
    {
        // TODO: do we really need this check? I don't think we do
        if(CORDebuggerAttached())
        {
            if (NotifyDebuggerLastChance(pThread, pExceptionInfo, FALSE) == EXCEPTION_CONTINUE_EXECUTION)
            {
                LOG((LF_EH, LL_INFO100, "ThreadBaseExceptionFilter_Worker: EXCEPTION_CONTINUE_EXECUTION\n"));
                return EXCEPTION_CONTINUE_EXECUTION;
            }
        }
    }
#endif // DEBUGGING_SUPPORTED

    // Do default handling, but ignore breakpoint exceptions and class init exceptions
    if (doDefault)
    {
        LOG((LF_EH, LL_INFO100, "ThreadBaseExceptionFilter_Worker: Calling DefaultCatchHandler\n"));

        BOOL useLastThrownObject = UpdateCurrentThrowable(pExceptionInfo->ExceptionRecord);

        DefaultCatchHandler(pExceptionInfo,
                            NULL,
                            useLastThrownObject,
                            FALSE,
                            location == ManagedThread || location == ThreadPoolThread || location == FinalizerThread);
    }

    // Return EXCEPTION_EXECUTE_HANDLER to swallow the exception.
    return (swallowing
            ? EXCEPTION_EXECUTE_HANDLER
            : EXCEPTION_CONTINUE_SEARCH);
} // LONG ThreadBaseExceptionFilter_Worker()


//    This is the filter for new managed threads, for threadpool threads, and for
//     running finalizer methods.
LONG ThreadBaseExceptionSwallowingFilter(PEXCEPTION_POINTERS pExceptionInfo, PVOID pvParam)
{
    return ThreadBaseExceptionFilter_Worker(pExceptionInfo, pvParam, /*swallowing=*/true);
}

//    This is the filter that we install when transitioning an AppDomain at the base of a managed
//     thread.  Nothing interesting will get swallowed after us.  So we never decide to continue
//     the search.  Instead, we let it go unhandled and get the Watson report and debugging
//     experience before the AD transition has an opportunity to catch/rethrow and lose all the
//     relevant information.
LONG ThreadBaseExceptionAppDomainFilter(EXCEPTION_POINTERS *pExceptionInfo, PVOID pvParam)
{
    LONG ret = ThreadBaseExceptionSwallowingFilter(pExceptionInfo, pvParam);

    if (ret != EXCEPTION_CONTINUE_SEARCH)
        return ret;

    // Consider the exception to be unhandled
    return InternalUnhandledExceptionFilter_Worker(pExceptionInfo);
}

// Filter for calls out from the 'vm' to native code, if there's a possibility of SEH exceptions
// in the native code.
LONG CallOutFilter(PEXCEPTION_POINTERS pExceptionInfo, PVOID pv)
{
    CallOutFilterParam *pParam = static_cast<CallOutFilterParam *>(pv);

    _ASSERTE(pParam && (pParam->OneShot == TRUE || pParam->OneShot == FALSE));

    if (pParam->OneShot == TRUE)
    {
        pParam->OneShot = FALSE;

        // Replace whatever SEH exception is in flight, with an SEHException derived from
        // CLRException.  But if the exception already looks like one of ours, let it
        // go past since LastThrownObject should already represent it.
        if ((!IsComPlusException(pExceptionInfo->ExceptionRecord)) &&
            (pExceptionInfo->ExceptionRecord->ExceptionCode != EXCEPTION_MSVC))
            PAL_CPP_THROW(SEHException *, new SEHException(pExceptionInfo->ExceptionRecord,
                                                           pExceptionInfo->ContextRecord));
    }
    return EXCEPTION_CONTINUE_SEARCH;
}


//==========================================================================
// Convert the format string used by sprintf to the format used by String.Format.
// Using the managed formatting routine avoids bogus access violations
// that happen for long strings in Win32's FormatMessage.
//
// Note: This is not general purpose routine. It handles only cases found
// in TypeLoadException and FileLoadException.
//==========================================================================
static BOOL GetManagedFormatStringForResourceID(CCompRC::ResourceCategory eCategory, UINT32 resId, SString & converted)
{
    STANDARD_VM_CONTRACT;

    StackSString temp;
    if (!temp.LoadResource(eCategory, resId))
        return FALSE;

    SString::Iterator itr = temp.Begin();
    while (*itr)
    {
        WCHAR c = *itr++;
        switch (c) {
        case '%':
            {
                WCHAR fmt = *itr++;
                if (fmt >= '1' && fmt <= '9') {
                    converted.Append(W("{"));
                    converted.Append(fmt - 1); // the managed args start at 0
                    converted.Append(W("}"));
                }
                else
                if (fmt == '%') {
                    converted.Append(W("%"));
                }
                else {
                    _ASSERTE(!"Unexpected formating string: %s");
                }
            }
            break;
        case '{':
            converted.Append(W("{{"));
            break;
        case '}':
            converted.Append(W("}}"));
            break;
        default:
            converted.Append(c);
            break;
        }
    }
    return TRUE;
}

//==========================================================================
// Private helper for TypeLoadException.
//==========================================================================
extern "C" void QCALLTYPE GetTypeLoadExceptionMessage(UINT32 resId, QCall::StringHandleOnStack retString)
{
    QCALL_CONTRACT;

    BEGIN_QCALL;

    StackSString format;
    GetManagedFormatStringForResourceID(CCompRC::Error, resId ? resId : IDS_CLASSLOAD_GENERAL,  format);
    retString.Set(format);

    END_QCALL;
}



//==========================================================================
// Private helper for FileLoadException and FileNotFoundException.
//==========================================================================

extern "C" void QCALLTYPE GetFileLoadExceptionMessage(UINT32 hr, QCall::StringHandleOnStack retString)
{
    QCALL_CONTRACT;

    BEGIN_QCALL;

    StackSString format;
    GetManagedFormatStringForResourceID(CCompRC::Error, GetResourceIDForFileLoadExceptionHR(hr), format);
    retString.Set(format);

    END_QCALL;
}

//==========================================================================
// Private helper for FileLoadException and FileNotFoundException.
//==========================================================================
extern "C" void QCALLTYPE FileLoadException_GetMessageForHR(UINT32 hresult, QCall::StringHandleOnStack retString)
{
    QCALL_CONTRACT;

    BEGIN_QCALL;

    BOOL bNoGeekStuff = FALSE;
    switch ((HRESULT)hresult)
    {
        // These are not usually app errors - as long
        // as the message is reasonably clear, we can live without the hex code stuff.
        case COR_E_FILENOTFOUND:
        case __HRESULT_FROM_WIN32(ERROR_MOD_NOT_FOUND):
        case __HRESULT_FROM_WIN32(ERROR_PATH_NOT_FOUND):
        case __HRESULT_FROM_WIN32(ERROR_INVALID_NAME):
        case __HRESULT_FROM_WIN32(ERROR_BAD_NET_NAME):
        case __HRESULT_FROM_WIN32(ERROR_BAD_NETPATH):
        case __HRESULT_FROM_WIN32(ERROR_DLL_NOT_FOUND):
        case CTL_E_FILENOTFOUND:
        case COR_E_DLLNOTFOUND:
        case COR_E_PATHTOOLONG:
        case E_ACCESSDENIED:
        case COR_E_BADIMAGEFORMAT:
        case COR_E_NEWER_RUNTIME:
        case COR_E_ASSEMBLYEXPECTED:
        case CLR_E_BIND_ARCHITECTURE_MISMATCH:
            bNoGeekStuff = TRUE;
            break;
    }

    SString s;
    GetHRMsg((HRESULT)hresult, s, bNoGeekStuff);
    retString.Set(s);

    END_QCALL;
}

// Check if there is a pending exception or the thread is already aborting. Returns 0 if yes.
// Otherwise, sets the thread up for generating an abort and returns address of ThrowControlForThread
// It is the caller's responsibility to set up Thread::m_OSContext prior to this call.  This is used as
// the context for checking if a ThreadAbort is allowed, and also as the context for the ThreadAbortException
// itself.
LPVOID COMPlusCheckForAbort(UINT_PTR uTryCatchResumeAddress)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    // Initialize the return address
    LPVOID pRetAddress = 0;

    Thread* pThread = GetThread();

    if ((!pThread->IsAbortRequested()) ||         // if no abort has been requested
        (!pThread->IsRudeAbort() &&
        (pThread->GetThrowable() != NULL)) )  // or if there is a pending exception
    {
        goto exit;
    }

    // Reverse COM interop IL stubs map all exceptions to HRESULTs and must not propagate Thread.Abort
    // to their unmanaged callers.
    if (uTryCatchResumeAddress != NULL)
    {
        MethodDesc * pMDResumeMethod = ExecutionManager::GetCodeMethodDesc((PCODE)uTryCatchResumeAddress);
        if (pMDResumeMethod->IsILStub())
            goto exit;
    }

    // else we must produce an abort
    if ((pThread->GetThrowable() == NULL) &&
        (pThread->IsAbortInitiated()))
    {
        // Oops, we just swallowed an abort, must restart the process
        pThread->ResetAbortInitiated();
    }

    // Question: Should we also check for (pThread->m_PreventAsync == 0)

    pThread->SetThrowControlForThread(Thread::InducedThreadRedirectAtEndOfCatch);
    if (!pThread->ReadyForAbort())
    {
        pThread->ResetThrowControlForThread();
        goto exit;
    }
    pThread->SetThrowControlForThread(Thread::InducedThreadStop);

    pRetAddress = (LPVOID)THROW_CONTROL_FOR_THREAD_FUNCTION;

exit:

#ifndef TARGET_UNIX

    // Only proceed if Watson is enabled - CoreCLR may have it disabled.
    if (IsWatsonEnabled())
    {
        BOOL fClearUEWatsonBucketTracker = TRUE;
        PTR_EHWatsonBucketTracker pUEWatsonBucketTracker = pThread->GetExceptionState()->GetUEWatsonBucketTracker();

        if (pRetAddress && pThread->IsAbortRequested())
        {
            // Since we are going to reraise the thread abort exception,  we would like to assert that
            // the buckets present in the UE tracker are the ones which were setup TAE was first raised.
            //
            // However, these buckets could come from across AD transition as well and thus, would be
            // marked for "Captured at AD transition". Thus, we cannot just assert them to be only from
            // TAE raise.
            //
            // We try to preserve buckets incase there is another catch that may catch the exception we reraise
            // and it attempts to FailFast using the TA exception object. In such a case,
            // we should maintain the original exception point's bucket details.
            if (pUEWatsonBucketTracker->RetrieveWatsonBucketIp() != NULL)
            {
                _ASSERTE(pUEWatsonBucketTracker->CapturedForThreadAbort() || pUEWatsonBucketTracker->CapturedAtADTransition());
                fClearUEWatsonBucketTracker = FALSE;
            }
#ifdef _DEBUG
            else
            {
                // If we are here and UE Watson bucket tracker is empty,
                // then it is possible that a thread abort was signalled when the catch was executing
                // and thus, hijack for TA from here is not a reraise but an initial raise.
                //
                // However, if we have partial details, then something is really not right.
                if (!((pUEWatsonBucketTracker->RetrieveWatsonBucketIp() == NULL) &&
                    (pUEWatsonBucketTracker->RetrieveWatsonBuckets() == NULL)))
                {
                    _ASSERTE(!"How come TA is being [re]raised and we have incomplete watson bucket details?");
                }
            }
#endif // _DEBUG
        }

        if (fClearUEWatsonBucketTracker)
        {
            // Clear the UE watson bucket tracker for future use since it does not have anything
            // useful for us right now.
            pUEWatsonBucketTracker->ClearWatsonBucketDetails();
            LOG((LF_EH, LL_INFO100, "COMPlusCheckForAbort - Cleared UE watson bucket tracker since TAE was not being reraised.\n"));
        }
    }

#endif // !TARGET_UNIX

    return pRetAddress;
}


BOOL IsThreadHijackedForThreadStop(Thread* pThread, EXCEPTION_RECORD* pExceptionRecord)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        FORBID_FAULT;
    }
    CONTRACTL_END;

    if (IsComPlusException(pExceptionRecord))
    {
        if (pThread->ThrewControlForThread() == Thread::InducedThreadStop)
        {
            LOG((LF_EH, LL_INFO100, "Asynchronous Thread Stop or Abort\n"));
            return TRUE;
        }
    }
    else if (IsStackOverflowException(pThread, pExceptionRecord))
    {
        // SO happens before we are able to change the state to InducedThreadStop, but
        // we are still in our hijack routine.
        if (pThread->ThrewControlForThread() == Thread::InducedThreadRedirect)
        {
            LOG((LF_EH, LL_INFO100, "Asynchronous Thread Stop or Abort caused by SO\n"));
            return TRUE;
        }
    }
    return FALSE;
}

// We sometimes move a thread's execution so it will throw an exception for us.
// But then we have to treat the exception as if it came from the instruction
// the thread was originally running.
//
// NOTE: This code depends on the fact that there are no register-based data dependencies
// between a try block and a catch, fault, or finally block.  If there were, then we need
// to preserve more of the register context.

void AdjustContextForThreadStop(Thread* pThread,
                                CONTEXT* pContext)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        FORBID_FAULT;
    }
    CONTRACTL_END;

    _ASSERTE(pThread->m_OSContext);

#ifndef FEATURE_EH_FUNCLETS
    SetIP(pContext, GetIP(pThread->m_OSContext));
    SetSP(pContext, (GetSP(pThread->m_OSContext)));

    if (GetFP(pThread->m_OSContext) != 0)  // ebp = 0 implies that we got here with the right values for ebp
    {
        SetFP(pContext, GetFP(pThread->m_OSContext));
    }

    // We might have been interrupted execution at a point where the jit has roots in
    // registers.  We just need to store a "safe" value in here so that the collector
    // doesn't trap.  We're not going to use these objects after the exception.
    //
    // Only callee saved registers are going to be reported by the faulting excepiton frame.
#if defined(TARGET_X86)
    // Ebx,esi,edi are important.  Eax,ecx,edx are not.
    pContext->Ebx = 0;
    pContext->Edi = 0;
    pContext->Esi = 0;
#else
    PORTABILITY_ASSERT("AdjustContextForThreadStop");
#endif

#else // !FEATURE_EH_FUNCLETS
    CopyOSContext(pContext, pThread->m_OSContext);
#if defined(TARGET_ARM) && defined(_DEBUG)
    // Make sure that the thumb bit is set on the IP of the original abort context we just restored.
    PCODE controlPC = GetIP(pContext);
    _ASSERTE(controlPC & THUMB_CODE);
#endif // TARGET_ARM
#endif // !FEATURE_EH_FUNCLETS

    pThread->ResetThrowControlForThread();

    // Should never get here if we're already throwing an exception.
    _ASSERTE(!pThread->IsExceptionInProgress() || pThread->IsRudeAbort());

    // Should never get here if we're already abort initiated.
    _ASSERTE(!pThread->IsAbortInitiated() || pThread->IsRudeAbort());

    if (pThread->IsAbortRequested())
    {
        pThread->SetAbortInitiated();    // to prevent duplicate aborts
    }
}

// Create a COM+ exception , stick it in the thread.
OBJECTREF
CreateCOMPlusExceptionObject(Thread *pThread, EXCEPTION_RECORD *pExceptionRecord, BOOL bAsynchronousThreadStop)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        FORBID_FAULT;
    }
    CONTRACTL_END;

    _ASSERTE(GetThreadNULLOk() == pThread);

    DWORD exceptionCode = pExceptionRecord->ExceptionCode;

    OBJECTREF result = 0;

    DWORD COMPlusExceptionCode = (bAsynchronousThreadStop
                                    ? kThreadAbortException
                                    : MapWin32FaultToCOMPlusException(pExceptionRecord));

    if (exceptionCode == STATUS_NO_MEMORY)
    {
        result = CLRException::GetBestOutOfMemoryException();
    }
    else if (IsStackOverflowException(pThread, pExceptionRecord))
    {
        result = CLRException::GetPreallocatedStackOverflowException();
    }
    else if (bAsynchronousThreadStop && pThread->IsAbortRequested() && pThread->IsRudeAbort())
    {
        result = CLRException::GetBestThreadAbortException();
    }
    else
    {
        EX_TRY
        {
            FAULT_NOT_FATAL();

            ThreadPreventAsyncHolder preventAsync;
            ResetProcessorStateHolder procState;

            INSTALL_UNWIND_AND_CONTINUE_HANDLER_EX;

            GCPROTECT_BEGIN(result)

            EEException e((RuntimeExceptionKind)COMPlusExceptionCode);
            result = e.CreateThrowable();

            // EEException is "one size fits all".  But AV needs some more information.
            if (COMPlusExceptionCode == kAccessViolationException)
            {
                SetExceptionAVParameters(result, pExceptionRecord);
            }

            GCPROTECT_END();

            UNINSTALL_UNWIND_AND_CONTINUE_HANDLER_EX(true);
        }
        EX_CATCH
        {
            // If we get an exception trying to build the managed exception object, then go ahead and return the
            // thrown object as the result of this function. This is preferable to letting the exception try to
            // percolate up through the EH code, and it effectively replaces the thrown exception with this
            // exception.
            result = GET_THROWABLE();
        }
        EX_END_CATCH(SwallowAllExceptions);
    }

    return result;
}

LONG FilterAccessViolation(PEXCEPTION_POINTERS pExceptionPointers, LPVOID lpvParam)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
        FORBID_FAULT;
    }
    CONTRACTL_END;

    if (pExceptionPointers->ExceptionRecord->ExceptionCode == EXCEPTION_ACCESS_VIOLATION)
        return EXCEPTION_EXECUTE_HANDLER;

    return EXCEPTION_CONTINUE_SEARCH;
}

/*
 * IsInterceptableException
 *
 * Returns whether this is an exception the EE knows how to intercept and continue from.
 *
 * Parameters:
 *   pThread - The thread the exception occurred on.
 *
 * Returns:
 *   TRUE if the exception on the thread is interceptable or not.
 *
 * Notes:
 *   Conditions for an interceptable exception:
 *   1) must be on a managed thread
 *   2) an exception must be in progress
 *   3) a managed exception object must have been created
 *   4) the thread must not be aborting
 *   5) the exception must not be a breakpoint, a single step, or a stack overflow
 *   6) the exception dispatch must be in the first pass
 *   7) the exception must not be a fatal error, as determined by the EE policy (see LogFatalError())
 */
bool IsInterceptableException(Thread *pThread)
{
    CONTRACTL
    {
        MODE_ANY;
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    return ((pThread != NULL)                       &&
            (!pThread->IsAbortRequested())          &&
            (pThread->IsExceptionInProgress())      &&
            (!pThread->IsThrowableNull())

#ifdef DEBUGGING_SUPPORTED
            &&
            pThread->GetExceptionState()->IsDebuggerInterceptable()
#endif

            );
}

// Determines whether we hit an DO_A_GC_HERE marker in JITted code, and returns the
// appropriate exception code, or zero if the code is not a GC marker.
DWORD GetGcMarkerExceptionCode(LPVOID ip)
{
#if defined(HAVE_GCCOVER)
    WRAPPER_NO_CONTRACT;

    // IsGcCoverageInterrupt(ip) requires "gccover.h"
    //
    if (GCStress<cfg_any>::IsEnabled() && IsGcCoverageInterrupt(ip))
    {
        return STATUS_CLR_GCCOVER_CODE;
    }
#else // defined(HAVE_GCCOVER)
    LIMITED_METHOD_CONTRACT;
#endif // defined(HAVE_GCCOVER)
    return 0;
}

// Did we hit an DO_A_GC_HERE marker in JITted code?
bool IsGcMarker(CONTEXT* pContext, EXCEPTION_RECORD *pExceptionRecord)
{
    DWORD exceptionCode = pExceptionRecord->ExceptionCode;
#ifdef HAVE_GCCOVER
    WRAPPER_NO_CONTRACT;

    if (GCStress<cfg_any>::IsEnabled())
    {
#if defined(GCCOVER_TOLERATE_SPURIOUS_AV)

        // We sometimes can't suspend the EE to update the GC marker instruction so
        // we update it directly without suspending.  This can sometimes yield
        // a STATUS_ACCESS_VIOLATION instead of STATUS_CLR_GCCOVER_CODE.  In
        // this case we let the AV through and retry the instruction as hopefully
        // the race will have resolved.  We'll track the IP of the instruction
        // that generated an AV so we don't mix up a real AV with a "fake" AV.
        //
        // See comments in function DoGcStress for more details on this race.
        //
        // Note these "fake" AVs will be reported by the kernel as reads from
        // address 0xF...F so we also use that as a screen.
        Thread* pThread = GetThreadNULLOk();
        if (exceptionCode == STATUS_ACCESS_VIOLATION &&
            GCStress<cfg_instr>::IsEnabled() &&
            pExceptionRecord->ExceptionInformation[0] == 0 &&
            pExceptionRecord->ExceptionInformation[1] == ~0 &&
            pThread->GetLastAVAddress() != (LPVOID)GetIP(pContext))
        {
            pThread->SetLastAVAddress((LPVOID)GetIP(pContext));
            return true;
        }
#endif // defined(GCCOVER_TOLERATE_SPURIOUS_AV)

        if (exceptionCode == STATUS_CLR_GCCOVER_CODE)
        {
            // GCStress processing can disturb last error, so preserve it.
            BOOL res;
            BEGIN_PRESERVE_LAST_ERROR;
            res = OnGcCoverageInterrupt(pContext);
            END_PRESERVE_LAST_ERROR;
            if (res)
            {
                return true;
            }

            {
                // ExecutionManager::IsManagedCode takes a spinlock.  Since this is in a debug-only
                // check, we'll allow the lock.
                CONTRACT_VIOLATION(TakesLockViolation);

                // Should never be in managed code.
                CONSISTENCY_CHECK_MSG(!ExecutionManager::IsManagedCode(GetIP(pContext)), "hit privileged instruction!");
            }
        }
    }
#else
    LIMITED_METHOD_CONTRACT;
#endif // HAVE_GCCOVER
    return false;
}

#ifndef TARGET_UNIX

// Return true if the access violation is well formed (has two info parameters
// at the end)
static inline BOOL
IsWellFormedAV(EXCEPTION_RECORD *pExceptionRecord)
{
    LIMITED_METHOD_CONTRACT;

    #define NUM_AV_PARAMS 2

    if (pExceptionRecord->NumberParameters == NUM_AV_PARAMS)
    {
        return TRUE;
    }
    else
    {
        return FALSE;
    }
}

static inline BOOL
IsDebuggerFault(EXCEPTION_RECORD *pExceptionRecord,
                CONTEXT *pContext,
                DWORD exceptionCode,
                Thread *pThread)
{
    LIMITED_METHOD_CONTRACT;

#ifdef DEBUGGING_SUPPORTED

#ifdef FEATURE_EMULATE_SINGLESTEP
    // On ARM we don't have any reliable hardware support for single stepping so it is emulated in software.
    // The implementation will end up throwing an EXCEPTION_BREAKPOINT rather than an EXCEPTION_SINGLE_STEP
    // and leaves other aspects of the thread context in an invalid state. Therefore we use this opportunity
    // to fixup the state before any other part of the system uses it (we do it here since only the debugger
    // uses single step functionality).

    // First ask the emulation itself whether this exception occurred while single stepping was enabled. If so
    // it will fix up the context to be consistent again and return true. If so and the exception was
    // EXCEPTION_BREAKPOINT then we translate it to EXCEPTION_SINGLE_STEP (otherwise we leave it be, e.g. the
    // instruction stepped caused an access violation).  since this is called from our VEH there might not
    // be a thread object so we must check pThread first.
    if ((pThread != NULL) && pThread->HandleSingleStep(pContext, exceptionCode) && (exceptionCode == EXCEPTION_BREAKPOINT))
    {
        exceptionCode = EXCEPTION_SINGLE_STEP;
        pExceptionRecord->ExceptionCode = EXCEPTION_SINGLE_STEP;
        pExceptionRecord->ExceptionAddress = (PVOID)pContext->Pc;
    }
#endif // FEATURE_EMULATE_SINGLESTEP

    // Is this exception really meant for the COM+ Debugger? Note: we will let the debugger have a chance if there
    // is a debugger attached to any part of the process. It is incorrect to consider whether or not the debugger
    // is attached the thread's current app domain at this point.

    // Even if a debugger is not attached, we must let the debugger handle the exception in case it's coming from a
    // patch-skipper.
    if ((!IsComPlusException(pExceptionRecord)) &&
        (GetThreadNULLOk() != NULL) &&
        (g_pDebugInterface != NULL) &&
        g_pDebugInterface->FirstChanceNativeException(pExceptionRecord,
                                                      pContext,
                                                      exceptionCode,
                                                      pThread))
    {
        LOG((LF_EH | LF_CORDB, LL_INFO1000, "IsDebuggerFault - it's the debugger's fault\n"));
        return true;
    }
#endif // DEBUGGING_SUPPORTED
    return false;
}

#endif // TARGET_UNIX

#if !defined(TARGET_ARM64) && !defined(TARGET_LOONGARCH64) && !defined(TARGET_RISCV64)
EXTERN_C void JIT_StackProbe_End();
#endif // !TARGET_ARM64 && !TARGET_LOONGARCH64 && !TARGET_RISCV64

#ifdef FEATURE_EH_FUNCLETS

#ifndef TARGET_X86
EXTERN_C void JIT_MemSet_End();
EXTERN_C void JIT_MemCpy_End();

EXTERN_C void JIT_WriteBarrier_End();
EXTERN_C void JIT_CheckedWriteBarrier_End();
EXTERN_C void JIT_ByRefWriteBarrier_End();
#endif // TARGET_X86

#if defined(TARGET_AMD64) && defined(_DEBUG)
EXTERN_C void JIT_WriteBarrier_Debug();
EXTERN_C void JIT_WriteBarrier_Debug_End();
#endif

//Return TRUE if pContext->Pc is in VirtualStub
BOOL IsIPinVirtualStub(PCODE f_IP)
{
    LIMITED_METHOD_CONTRACT;

    Thread * pThread = GetThreadNULLOk();

    // We may not have a managed thread object. Example is an AV on the helper thread.
    // (perhaps during StubManager::IsStub)
    if (pThread == NULL)
    {
        return FALSE;
    }

    StubCodeBlockKind sk = RangeSectionStubManager::GetStubKind(f_IP);

    if (sk == STUB_CODE_BLOCK_VSD_DISPATCH_STUB)
    {
        return TRUE;
    }
    else if (sk == STUB_CODE_BLOCK_VSD_RESOLVE_STUB)
    {
        return TRUE;
    }

    else {
        return FALSE;
    }
}

// Check if the passed in instruction pointer is in one of the
// JIT helper functions.
bool IsIPInMarkedJitHelper(UINT_PTR uControlPc)
{
    LIMITED_METHOD_CONTRACT;

#define CHECK_RANGE(name) \
    if (GetEEFuncEntryPoint(name) <= uControlPc && uControlPc < GetEEFuncEntryPoint(name##_End)) return true;

#ifndef TARGET_X86
    CHECK_RANGE(JIT_MemSet)
    CHECK_RANGE(JIT_MemCpy)

    CHECK_RANGE(JIT_WriteBarrier)
    CHECK_RANGE(JIT_CheckedWriteBarrier)
    CHECK_RANGE(JIT_ByRefWriteBarrier)
#if !defined(TARGET_ARM64) && !defined(TARGET_LOONGARCH64) && !defined(TARGET_RISCV64)
    CHECK_RANGE(JIT_StackProbe)
#endif // !TARGET_ARM64 && !TARGET_LOONGARCH64 && !TARGET_RISCV64
#else
#ifdef TARGET_UNIX
    CHECK_RANGE(JIT_WriteBarrierGroup)
    CHECK_RANGE(JIT_PatchedWriteBarrierGroup)
#endif // TARGET_UNIX
#endif // TARGET_X86

#if defined(TARGET_AMD64) && defined(_DEBUG)
    CHECK_RANGE(JIT_WriteBarrier_Debug)
#endif

    return false;
}
#endif // FEATURE_EH_FUNCLETS

// Returns TRUE if caller should resume execution.
BOOL
AdjustContextForJITHelpers(
        EXCEPTION_RECORD *pExceptionRecord,
        CONTEXT *pContext)
{
    WRAPPER_NO_CONTRACT;

    PCODE ip = GetIP(pContext);

    if (IsIPInWriteBarrierCodeCopy(ip))
    {
        // Pretend we were executing the barrier function at its original location so that the unwinder can unwind the frame
        ip = AdjustWriteBarrierIP(ip);
        SetIP(pContext, ip);
    }

#ifdef FEATURE_DATABREAKPOINT

    // If pExceptionRecord is null, it means it is called from EEDbgInterfaceImpl::AdjustContextForJITHelpersForDebugger()
    // This is called only when a data breakpoint is hitm which could be inside a JIT write barrier helper and required
    // this logic to help unwind out of it. For the x86, not patched case, we assume the IP lies within the region where we
    // have already saved the registers on the stack, and therefore the code unwind those registers as well. This is not true
    // for the usual AV case where the registers are not saved yet.

    if (pExceptionRecord == nullptr)
    {
#if defined(TARGET_X86)
        bool withinWriteBarrierGroup = ((ip >= (PCODE) JIT_WriteBarrierGroup) && (ip <= (PCODE) JIT_WriteBarrierGroup_End));
        bool withinPatchedWriteBarrierGroup = ((ip >= (PCODE) JIT_PatchedWriteBarrierGroup) && (ip <= (PCODE) JIT_PatchedWriteBarrierGroup_End));
        if (withinWriteBarrierGroup || withinPatchedWriteBarrierGroup)
        {
            DWORD* esp = (DWORD*)pContext->Esp;
            if (withinWriteBarrierGroup)
            {
#if defined(WRITE_BARRIER_CHECK)
                pContext->Ebp = *esp++;
                pContext->Ecx = *esp++;
#endif
            }
            pContext->Eip = *esp++;
            pContext->Esp = (DWORD)esp;
            return TRUE;
        }
#elif defined(TARGET_AMD64)
        if (IsIPInMarkedJitHelper((UINT_PTR)ip))
        {
            Thread::VirtualUnwindToFirstManagedCallFrame(pContext);
            return TRUE;
        }
#else
        #error Not supported
#endif
        return FALSE;
    }

#endif // FEATURE_DATABREAKPOINT

#if defined(TARGET_X86) && !defined(TARGET_UNIX)
    void* f_IP = (void *)GetIP(pContext);

    if (((f_IP >= (void *) JIT_WriteBarrierGroup) && (f_IP <= (void *) JIT_WriteBarrierGroup_End)) ||
        ((f_IP >= (void *) JIT_PatchedWriteBarrierGroup) && (f_IP <= (void *) JIT_PatchedWriteBarrierGroup_End)))
    {
        // set the exception IP to be the instruction that called the write barrier
        void* callsite = (void *)GetAdjustedCallAddress(*dac_cast<PTR_PCODE>(GetSP(pContext)));
        pExceptionRecord->ExceptionAddress = callsite;
        SetIP(pContext, (PCODE)callsite);

        // put ESP back to what it was before the call.
        SetSP(pContext, PCODE((BYTE*)GetSP(pContext) + sizeof(void*)));
    }

    if ((f_IP >= (void *) JIT_StackProbe) && (f_IP <= (void *) JIT_StackProbe_End))
    {
        TADDR ebp = GetFP(pContext);
        void* callsite = (void *)*dac_cast<PTR_PCODE>(ebp + 4);
        pExceptionRecord->ExceptionAddress = callsite;
        SetIP(pContext, (PCODE)callsite);

        // Restore EBP / ESP back to what it was before the call.
        SetFP(pContext, *dac_cast<PTR_PCODE>(ebp));
        SetSP(pContext, ebp + 8);
    }

    return FALSE;
#elif defined(FEATURE_EH_FUNCLETS) // TARGET_X86 && !TARGET_UNIX
    void* f_IP = dac_cast<PTR_VOID>(GetIP(pContext));

    CONTEXT             tempContext;
    CONTEXT*            pExceptionContext = pContext;

    BOOL fExcluded = IsIPInMarkedJitHelper((UINT_PTR)f_IP);

    if (fExcluded)
    {
        bool fShouldHandleManagedFault = false;

        if (pContext != &tempContext)
        {
            tempContext = *pContext;
            pContext = &tempContext;
        }

        Thread::VirtualUnwindToFirstManagedCallFrame(pContext);

#if defined(TARGET_ARM) || defined(TARGET_ARM64) || defined(TARGET_LOONGARCH64) || defined(TARGET_RISCV64)
        // We had an AV in the writebarrier that needs to be treated
        // as originating in managed code. At this point, the stack (growing
        // from left->right) looks like this:
        //
        // ManagedFunc -> Native_WriteBarrierInVM -> AV
        //
        // We just performed an unwind from the write-barrier
        // and now have the context in ManagedFunc. Since
        // ManagedFunc called into the write-barrier, the return
        // address in the unwound context corresponds to the
        // instruction where the call will return.
        //
        // On ARM, just like we perform ControlPC adjustment
        // during exception dispatch (refer to ExceptionTracker::InitializeCrawlFrame),
        // we will need to perform the corresponding adjustment of IP
        // we got from unwind above, so as to indicate that the AV
        // happened "before" the call to the writebarrier and not at
        // the instruction at which the control will return.
       PCODE ControlPCPostAdjustment = GetIP(pContext) - STACKWALK_CONTROLPC_ADJUST_OFFSET;

       // Now we save the address back into the context so that it gets used
       // as the faulting address.
       SetIP(pContext, ControlPCPostAdjustment);
#endif // TARGET_ARM || TARGET_ARM64 || TARGET_LOONGARCH64 || TARGET_RISCV64

        // Unwind the frame chain - On Win64, this is required since we may handle the managed fault and to do so,
        // we will replace the exception context with the managed context and "continue execution" there. Thus, we do not
        // want any explicit frames active below the resumption SP.
        //
        // Question: Why do we unwind before determining whether we will handle the exception or not?
        UnwindFrameChain(GetThread(), (Frame*)GetSP(pContext));
        fShouldHandleManagedFault = ShouldHandleManagedFault(pExceptionRecord,pContext,
                               NULL, // establisher frame (x86 only)
                               NULL  // pThread           (x86 only)
                               );

        if (fShouldHandleManagedFault)
        {
            ReplaceExceptionContextRecord(pExceptionContext, pContext);
            pExceptionRecord->ExceptionAddress = dac_cast<PTR_VOID>(GetIP(pContext));
            return TRUE;
        }
    }

    return FALSE;
#else // FEATURE_EH_FUNCLETS
    PORTABILITY_ASSERT("AdjustContextForJITHelpers");
    return FALSE;
#endif // ELSE
}

#if defined(USE_FEF) && !defined(TARGET_UNIX)

struct HandleManagedFaultFilterParam
{
    // It's possible for our filter to be called more than once if some other first-pass
    // handler lets an exception out.  We need to make sure we only fix the context for
    // the first exception we see.  This flag takes care of that.
    BOOL fFilterExecuted;
    EXCEPTION_RECORD *pOriginalExceptionRecord;
};

static LONG HandleManagedFaultFilter(EXCEPTION_POINTERS* ep, LPVOID pv)
{
    WRAPPER_NO_CONTRACT;

    HandleManagedFaultFilterParam *pParam = (HandleManagedFaultFilterParam *) pv;

    if (!pParam->fFilterExecuted)
    {
        ep->ExceptionRecord->ExceptionAddress = pParam->pOriginalExceptionRecord->ExceptionAddress;
        GetThread()->ResetThreadStateNC(Thread::TSNC_DebuggerIsManagedException);
        pParam->fFilterExecuted = TRUE;
    }

    return EXCEPTION_CONTINUE_SEARCH;
}

void HandleManagedFaultNew(EXCEPTION_RECORD* pExceptionRecord, CONTEXT* pContext)
{
    WRAPPER_NO_CONTRACT;

    FrameWithCookie<FaultingExceptionFrame> frameWithCookie;
    FaultingExceptionFrame *frame = &frameWithCookie;
#if defined(FEATURE_EH_FUNCLETS)
    *frame->GetGSCookiePtr() = GetProcessGSCookie();
#endif // FEATURE_EH_FUNCLETS
    frame->InitAndLink(pContext);

    Thread *pThread = GetThread();

    ExInfo exInfo(pThread, pExceptionRecord, pContext, ExKind::HardwareFault);

    DWORD exceptionCode = pExceptionRecord->ExceptionCode;
    if (exceptionCode == STATUS_ACCESS_VIOLATION)
    {
        if (pExceptionRecord->ExceptionInformation[1] < NULL_AREA_SIZE)
        {
            exceptionCode = 0; //STATUS_REDHAWK_NULL_REFERENCE;
        }
    }

    GCPROTECT_BEGIN(exInfo.m_exception);
    PREPARE_NONVIRTUAL_CALLSITE(METHOD__EH__RH_THROWHW_EX);
    DECLARE_ARGHOLDER_ARRAY(args, 2);
    args[ARGNUM_0] = DWORD_TO_ARGHOLDER(exceptionCode);
    args[ARGNUM_1] = PTR_TO_ARGHOLDER(&exInfo);

    pThread->IncPreventAbort();

    //Ex.RhThrowHwEx(exceptionCode, &exInfo)
    CALL_MANAGED_METHOD_NORET(args)

    GCPROTECT_END();
}

void HandleManagedFault(EXCEPTION_RECORD* pExceptionRecord, CONTEXT* pContext)
{
    WRAPPER_NO_CONTRACT;

    // Ok.  Now we have a brand new fault in jitted code.
    FrameWithCookie<FaultingExceptionFrame> frameWithCookie;
    FaultingExceptionFrame *frame = &frameWithCookie;
#if defined(FEATURE_EH_FUNCLETS)
    *frame->GetGSCookiePtr() = GetProcessGSCookie();
#endif // FEATURE_EH_FUNCLETS
    frame->InitAndLink(pContext);

    HandleManagedFaultFilterParam param;
    param.fFilterExecuted = FALSE;
    param.pOriginalExceptionRecord = pExceptionRecord;

    PAL_TRY(HandleManagedFaultFilterParam *, pParam, &param)
    {
        GetThread()->SetThreadStateNC(Thread::TSNC_DebuggerIsManagedException);

        EXCEPTION_RECORD *pRecord = pParam->pOriginalExceptionRecord;

        RaiseException(pRecord->ExceptionCode, 0,
            pRecord->NumberParameters, pRecord->ExceptionInformation);
    }
    PAL_EXCEPT_FILTER(HandleManagedFaultFilter)
    {
    }
    PAL_ENDTRY
}

#endif // USE_FEF && !TARGET_UNIX

//
// Init a new frame
//
void FaultingExceptionFrame::Init(CONTEXT *pContext)
{
    WRAPPER_NO_CONTRACT;
#ifndef FEATURE_EH_FUNCLETS
#ifdef TARGET_X86
    CalleeSavedRegisters *pRegs = GetCalleeSavedRegisters();
#define CALLEE_SAVED_REGISTER(regname) pRegs->regname = pContext->regname;
    ENUM_CALLEE_SAVED_REGISTERS();
#undef CALLEE_SAVED_REGISTER
    m_ReturnAddress = ::GetIP(pContext);
    m_Esp = (DWORD)GetSP(pContext);
#else // TARGET_X86
    PORTABILITY_ASSERT("FaultingExceptionFrame::Init");
#endif // _TARGET_???_ (ELSE)
#else // !FEATURE_EH_FUNCLETS
    m_ReturnAddress = ::GetIP(pContext);
    CopyOSContext(&m_ctx, pContext);
#endif // !FEATURE_EH_FUNCLETS
}

//
// Init and Link in a new frame
//
void FaultingExceptionFrame::InitAndLink(CONTEXT *pContext)
{
    WRAPPER_NO_CONTRACT;

    Init(pContext);

    Push();
}


bool ShouldHandleManagedFault(
                        EXCEPTION_RECORD*               pExceptionRecord,
                        CONTEXT*                        pContext,
                        EXCEPTION_REGISTRATION_RECORD*  pEstablisherFrame,
                        Thread*                         pThread)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    // If we get a faulting instruction inside managed code, we're going to
    //  1. Allocate the correct exception object, store it in the thread.
    //  2. Save the EIP in the thread.
    //  3. Change the EIP to our throw helper
    //  4. Resume execution.
    //
    //  The helper will push a frame for us, and then throw the correct managed exception.
    //
    // Is this exception really meant for the COM+ Debugger? Note: we will let the debugger have a chance if there is a
    // debugger attached to any part of the process. It is incorrect to consider whether or not the debugger is attached
    // the thread's current app domain at this point.


    // A managed exception never comes from managed code, and we can ignore all breakpoint
    // exceptions.
    //
    DWORD exceptionCode = pExceptionRecord->ExceptionCode;
    if (IsComPlusException(pExceptionRecord)
        || exceptionCode == STATUS_BREAKPOINT
        || exceptionCode == STATUS_SINGLE_STEP)
    {
        return false;
    }

#ifdef _DEBUG
    // This is a workaround, but it's debug-only as is gc stress 4.
    // The problem is that if we get an exception with this code that
    // didn't come from GCStress=4, then we won't push a FeF and will
    // end up with a gc hole and potential crash.
    if (exceptionCode == STATUS_CLR_GCCOVER_CODE)
        return false;
#endif // _DEBUG

#ifndef FEATURE_EH_FUNCLETS
    // If there's any frame below the ESP of the exception, then we can forget it.
    if (pThread->m_pFrame < dac_cast<PTR_VOID>(GetSP(pContext)))
        return false;

    // If we're a subsequent handler forget it.
    EXCEPTION_REGISTRATION_RECORD* pBottomMostHandler = pThread->GetExceptionState()->m_currentExInfo.m_pBottomMostHandler;
    if (pBottomMostHandler != NULL && pEstablisherFrame > pBottomMostHandler)
    {
        return false;
    }
#endif // FEATURE_EH_FUNCLETS

    {
        // If it's not a fault in jitted code, forget it.

        // ExecutionManager::IsManagedCode takes a spinlock.  Since we're in the middle of throwing,
        // we'll allow the lock, even if a caller didn't expect it.
        CONTRACT_VIOLATION(TakesLockViolation);

        if (!ExecutionManager::IsManagedCode(GetIP(pContext)))
            return false;
    }

    // caller should call HandleManagedFault and resume execution.
    return true;
}

#ifndef TARGET_UNIX

VEH_ACTION WINAPI CLRVectoredExceptionHandlerPhase2(PEXCEPTION_POINTERS pExceptionInfo);
VEH_ACTION WINAPI CLRVectoredExceptionHandlerPhase3(PEXCEPTION_POINTERS pExceptionInfo);

VEH_ACTION WINAPI CLRVectoredExceptionHandler(PEXCEPTION_POINTERS pExceptionInfo)
{
    // It is not safe to execute code inside VM after we shutdown EE.  One example is DisablePreemptiveGC
    // will block forever.
    if (g_fForbidEnterEE)
    {
        return VEH_CONTINUE_SEARCH;
    }


    //
    // DO NOT USE CONTRACTS HERE AS THIS ROUTINE MAY NEVER RETURN.  You can use
    // static contracts, but currently this is all WRAPPER_NO_CONTRACT.
    //


    //
    //        READ THIS!
    //
    //
    // You cannot put any code in here that allocates during an out-of-memory handling.
    // This routine runs before *any* other handlers, including __try.  Thus, if you
    // allocate anything in this routine then it will throw out-of-memory and end up
    // right back here.
    //
    // There are various things that allocate that you may not expect to allocate.  One
    // instance of this is STRESS_LOG.  It allocates the log buffer if the thread does
    // not already have one allocated.  Thus, if we OOM during the setting up of the
    // thread, the log buffer will not be allocated and this will try to do so.  Thus,
    // all STRESS_LOGs in here need to be after you have guaranteed the allocation has
    // already occurred.
    //

    Thread *pThread;

    {
        MAYBE_FAULT_FORBID_NO_ALLOC((pExceptionInfo->ExceptionRecord->ExceptionCode == STATUS_NO_MEMORY));

        pThread = GetThreadNULLOk();

        //
        // Since we are in an OOM situation, we test the thread object before logging since if the
        // thread exists we know the log buffer has been allocated already.
        //
        if (pThread != NULL)
        {
            CantAllocHolder caHolder;
            STRESS_LOG4(LF_EH, LL_INFO100, "In CLRVectoredExceptionHandler, Exception = %x, Context = %p, IP = %p SP = %p\n",
                    pExceptionInfo->ExceptionRecord->ExceptionCode, pExceptionInfo->ContextRecord,
                    GetIP(pExceptionInfo->ContextRecord), GetSP(pExceptionInfo->ContextRecord));
        }

    }
#ifdef TARGET_AMD64

#ifndef STATUS_RETURN_ADDRESS_HIJACK_ATTEMPT
#define STATUS_RETURN_ADDRESS_HIJACK_ATTEMPT ((DWORD)0x80000033L)
#endif

    if (pExceptionInfo->ExceptionRecord->ExceptionCode == STATUS_RETURN_ADDRESS_HIJACK_ATTEMPT)
    {
        HijackArgs hijackArgs;
        hijackArgs.Rax = pExceptionInfo->ContextRecord->Rax;
        hijackArgs.Rsp = pExceptionInfo->ContextRecord->Rsp;

        bool areCetShadowStacksEnabled = Thread::AreCetShadowStacksEnabled();
        if (areCetShadowStacksEnabled)
        {
            // When the CET is enabled, the return address is still on stack, so we need to set the Rsp as
            // if it was popped.
            hijackArgs.Rsp += 8;
        }
        hijackArgs.Rip = 0 ; // The OnHijackWorker sets this
        #define CALLEE_SAVED_REGISTER(regname) hijackArgs.Regs.regname = pExceptionInfo->ContextRecord->regname;
        ENUM_CALLEE_SAVED_REGISTERS();
        #undef CALLEE_SAVED_REGISTER

        OnHijackWorker(&hijackArgs);

        #define CALLEE_SAVED_REGISTER(regname) pExceptionInfo->ContextRecord->regname = hijackArgs.Regs.regname;
        ENUM_CALLEE_SAVED_REGISTERS();
        #undef CALLEE_SAVED_REGISTER
        pExceptionInfo->ContextRecord->Rax = hijackArgs.Rax;

        if (areCetShadowStacksEnabled)
        {
            // The context refers to the return instruction
            // Set the return address on the stack to the original one
            *(size_t *)pExceptionInfo->ContextRecord->Rsp = hijackArgs.ReturnAddress;
        }
        else
        {
            // The context refers to the location after the return was processed
            pExceptionInfo->ContextRecord->Rip = hijackArgs.ReturnAddress;
        }

        return VEH_CONTINUE_EXECUTION;
    }
#endif

    // We need to unhijack the thread here if it is not unhijacked already.  On x86 systems,
    // we do this in Thread::StackWalkFramesEx, but on amd64 systems we have the OS walk the
    // stack for us.  If we leave CLRVectoredExceptionHandler with a thread still hijacked,
    // the operating system will not be able to walk the stack and not find the handlers for
    // the exception.  It is safe to unhijack the thread in this case for two reasons:
    // 1.  pThread refers to *this* thread.
    // 2.  If another thread tries to hijack this thread, it will see we are not in managed
    //     code (and thus won't try to hijack us).
#if defined(FEATURE_EH_FUNCLETS) && defined(FEATURE_HIJACK)
    if (pThread != NULL)
    {
        pThread->UnhijackThreadNoAlloc();
    }
#endif // defined(FEATURE_EH_FUNCLETS) && defined(FEATURE_HIJACK)

    if (pExceptionInfo->ExceptionRecord->ExceptionCode == STATUS_STACK_OVERFLOW)
    {
        //
        // Not an Out-of-memory situation, so no need for a forbid fault region here
        //
#ifdef FEATURE_EH_FUNCLETS
        if (g_isNewExceptionHandlingEnabled)
        {
            EEPolicy::HandleStackOverflow();
        }
#endif // FEATURE_EH_FUNCLETS
        return VEH_CONTINUE_SEARCH;
    }

    // We can't probe here, because we won't return from the CLRVectoredExceptionHandlerPhase2
    // on WIN64
    //

    if (pThread)
    {
        FAULT_FORBID_NO_ALLOC();
        CantAllocHolder caHolder;
    }

    return CLRVectoredExceptionHandlerPhase2(pExceptionInfo);
}

VEH_ACTION WINAPI CLRVectoredExceptionHandlerPhase2(PEXCEPTION_POINTERS pExceptionInfo)
{
    //
    // DO NOT USE CONTRACTS HERE AS THIS ROUTINE MAY NEVER RETURN.  You can use
    // static contracts, but currently this is all WRAPPER_NO_CONTRACT.
    //

    //
    //        READ THIS!
    //
    //
    // You cannot put any code in here that allocates during an out-of-memory handling.
    // This routine runs before *any* other handlers, including __try.  Thus, if you
    // allocate anything in this routine then it will throw out-of-memory and end up
    // right back here.
    //
    // There are various things that allocate that you may not expect to allocate.  One
    // instance of this is STRESS_LOG.  It allocates the log buffer if the thread does
    // not already have one allocated.  Thus, if we OOM during the setting up of the
    // thread, the log buffer will not be allocated and this will try to do so.  Thus,
    // all STRESS_LOGs in here need to be after you have guaranteed the allocation has
    // already occurred.
    //

    PEXCEPTION_RECORD pExceptionRecord  = pExceptionInfo->ExceptionRecord;
    VEH_ACTION action;

    {
        MAYBE_FAULT_FORBID_NO_ALLOC((pExceptionRecord->ExceptionCode == STATUS_NO_MEMORY));
        CantAllocHolder caHolder;

        action = CLRVectoredExceptionHandlerPhase3(pExceptionInfo);
    }

    if ((action == VEH_CONTINUE_EXECUTION) || (action == VEH_CONTINUE_SEARCH) || (action == VEH_EXECUTE_HANDLER))
    {
        return action;
    }

#if defined(FEATURE_EH_FUNCLETS)

    if (action == VEH_EXECUTE_HANDLE_MANAGED_EXCEPTION)
    {
        return action;
    }

#endif // defined(FEATURE_EH_FUNCLETS)


    //
    // In OOM situations, this call better not fault.
    //
    {
        MAYBE_FAULT_FORBID_NO_ALLOC((pExceptionRecord->ExceptionCode == STATUS_NO_MEMORY));
        CantAllocHolder caHolder;

        // Give the debugger a chance. Note that its okay for this call to trigger a GC, since the debugger will take
        // special steps to make that okay.
        //
        // @TODO: I'd love a way to call into the debugger with GCX_NOTRIGGER still in scope, and force them to make
        // the choice to break the no-trigger region after taking all necessary precautions.
        if (IsDebuggerFault(pExceptionRecord, pExceptionInfo->ContextRecord, pExceptionRecord->ExceptionCode, GetThreadNULLOk()))
        {
            return VEH_CONTINUE_EXECUTION;
        }
    }

    //
    // No reason to put a forbid fault region here as the exception code is not STATUS_NO_MEMORY.
    //

    // Handle a user breakpoint. Note that its okay for the UserBreakpointFilter to trigger a GC, since we're going
    // to either a) terminate the process, or b) let a user attach an unmanaged debugger, and debug knowing that
    // managed state may be messed up.
    if ((pExceptionRecord->ExceptionCode == STATUS_BREAKPOINT) ||
        (pExceptionRecord->ExceptionCode == STATUS_SINGLE_STEP))
    {
        // A breakpoint outside managed code and outside the runtime will have to be handled by some
        //  other piece of code.

        BOOL fExternalException = FALSE;

        {
            // ExecutionManager::IsManagedCode takes a spinlock.  Since we're in the middle of throwing,
            // we'll allow the lock, even if a caller didn't expect it.
            CONTRACT_VIOLATION(TakesLockViolation);

            fExternalException = (!ExecutionManager::IsManagedCode(GetIP(pExceptionInfo->ContextRecord)) &&
                                  !IsIPInModule(GetClrModuleBase(), GetIP(pExceptionInfo->ContextRecord)));
        }

        if (fExternalException)
        {
            // The breakpoint was not ours.  Someone else can handle it.  (Or if not, we'll get it again as
            //  an unhandled exception.)
            return VEH_CONTINUE_SEARCH;
        }

        // The breakpoint was from managed or the runtime.  Handle it.
        return (VEH_ACTION)UserBreakpointFilter(pExceptionInfo);
    }

#if defined(FEATURE_EH_FUNCLETS)
    BOOL fShouldHandleManagedFault;

    {
        MAYBE_FAULT_FORBID_NO_ALLOC((pExceptionRecord->ExceptionCode == STATUS_NO_MEMORY));
        CantAllocHolder caHolder;
        fShouldHandleManagedFault = ShouldHandleManagedFault(pExceptionInfo->ExceptionRecord,
                                                             pExceptionInfo->ContextRecord,
                                                             NULL, // establisher frame (x86 only)
                                                             NULL  // pThread           (x86 only)
                                                            );
    }

    if (fShouldHandleManagedFault)
    {
        return VEH_EXECUTE_HANDLE_MANAGED_EXCEPTION;
    }
#endif // defined(FEATURE_EH_FUNCLETS)

    return VEH_EXECUTE_HANDLER;
}

/*
 * CLRVectoredExceptionHandlerPhase3
 *
 * This routine does some basic processing on the exception, making decisions about common
 * exception types and whether to continue them or not.  It has side-effects, in that it may
 * adjust the context in the exception.
 *
 * Parameters:
 *    pExceptionInfo - pointer to the exception
 *
 * Returns:
 *    VEH_NO_ACTION - This indicates that Phase3 has no specific action to take and that further
 *       processing of this exception should continue.
 *    VEH_EXECUTE_HANDLE_MANAGED_EXCEPTION - This indicates that the caller should call HandleMandagedException
 *       immediately.
 *    VEH_CONTINUE_EXECUTION - Caller should return EXCEPTION_CONTINUE_EXECUTION.
 *    VEH_CONTINUE_SEARCH - Caller should return EXCEPTION_CONTINUE_SEARCH;
 *    VEH_EXECUTE_HANDLER - Caller should return EXCEPTION_EXECUTE_HANDLER.
 *
 *   Note that in all cases the context in the exception may have been adjusted.
 *
 */

VEH_ACTION WINAPI CLRVectoredExceptionHandlerPhase3(PEXCEPTION_POINTERS pExceptionInfo)
{
    //
    // DO NOT USE CONTRACTS HERE AS THIS ROUTINE MAY NEVER RETURN.  You can use
    // static contracts, but currently this is all WRAPPER_NO_CONTRACT.
    //

    //
    //        READ THIS!
    //
    //
    // You cannot put any code in here that allocates during an out-of-memory handling.
    // This routine runs before *any* other handlers, including __try.  Thus, if you
    // allocate anything in this routine then it will throw out-of-memory and end up
    // right back here.
    //
    // There are various things that allocate that you may not expect to allocate.  One
    // instance of this is STRESS_LOG.  It allocates the log buffer if the thread does
    // not already have one allocated.  Thus, if we OOM during the setting up of the
    // thread, the log buffer will not be allocated and this will try to do so.  Thus,
    // all STRESS_LOGs in here need to be after you have guaranteed the allocation has
    // already occurred.
    //

    // Handle special cases which are common amongst all filters.
    PEXCEPTION_RECORD pExceptionRecord  = pExceptionInfo->ExceptionRecord;
    PCONTEXT          pContext          = pExceptionInfo->ContextRecord;
    DWORD             exceptionCode     = pExceptionRecord->ExceptionCode;

        // Its extremely important that no one trigger a GC in here. This is called from CPFH_FirstPassHandler, in
        // cases where we've taken an unmanaged exception on a managed thread (AV, divide by zero, etc.) but
        // _before_ we've done our work to erect a FaultingExceptionFrame. Thus, the managed frames are
        // unprotected. We setup a GCX_NOTRIGGER holder in this scope to prevent us from messing this up. Note
        // that the scope of this is limited, since there are times when its okay to trigger even in this special
        // case. The debugger is a good example: if it gets a breakpoint in managed code, it has the smarts to
        // prevent the GC before enabling GC, thus its okay for it to trigger.

    GCX_NOTRIGGER();

#ifdef USE_REDIRECT_FOR_GCSTRESS
    // NOTE: this is effectively ifdef (TARGET_AMD64 || TARGET_ARM), and does not actually trigger
    // a GC.  This will redirect the exception context to a stub which will
    // push a frame and cause GC.
    if (Thread::UseRedirectForGcStress())
    {
        if (IsGcMarker(pContext, pExceptionRecord))
        {
            return VEH_CONTINUE_EXECUTION;;
        }
    }
#endif // USE_REDIRECT_FOR_GCSTRESS

#if defined(FEATURE_HIJACK) && !defined(TARGET_UNIX)
#ifdef TARGET_X86
    CPFH_AdjustContextForThreadSuspensionRace(pContext, GetThread());
#endif // TARGET_X86
#endif // FEATURE_HIJACK && !TARGET_UNIX

    // Some other parts of the EE use exceptions in their own nefarious ways.  We do some up-front processing
    // here to fix up the exception if needed.
    if (exceptionCode == STATUS_ACCESS_VIOLATION)
    {
        if (IsWellFormedAV(pExceptionRecord))
        {
            if (AdjustContextForJITHelpers(pExceptionRecord, pContext))
            {
                // On x86, AdjustContextForJITHelpers simply backs up AV's
                // in write barrier helpers into the calling frame, so that
                // the subsequent logic here sees a managed fault.
                //
                // On 64-bit, some additional work is required..
#ifdef FEATURE_EH_FUNCLETS
                pContext->ContextFlags &= ~CONTEXT_EXCEPTION_ACTIVE;
                return VEH_EXECUTE_HANDLE_MANAGED_EXCEPTION;
#endif // defined(FEATURE_EH_FUNCLETS)
            }
            else if (AdjustContextForVirtualStub(pExceptionRecord, pContext))
            {
#ifdef FEATURE_EH_FUNCLETS
                pContext->ContextFlags &= ~CONTEXT_EXCEPTION_ACTIVE;
                return VEH_EXECUTE_HANDLE_MANAGED_EXCEPTION;
#endif
            }

            // Remember the EIP for stress debugging purposes.
            g_LastAccessViolationEIP = (void*) ::GetIP(pContext);

            // Note: we have a holder, called AVInRuntimeImplOkayHolder, that tells us that its okay to have an
            // AV in the Runtime's implementation in certain places. So, if its okay to have an AV at this
            // time, then skip the check for whether or not the AV is in our impl.
            // AVs are ok on the Helper thread (for which there is no pThread object,
            // and so the AVInRuntime holder doesn't work.
            Thread *pThread = GetThreadNULLOk();

            bool fAVisOk =
                (IsDbgHelperSpecialThread() || IsETWRundownSpecialThread() ||
                    ((pThread != NULL) && (pThread->AVInRuntimeImplOkay())) );


            // It is unnecessary to check this on second pass as we would have torn down
            // the process on the first pass. Also, the context record is not reliable
            // on second pass and this subjects us to false positives.
            if ((!fAVisOk) && !(pExceptionRecord->ExceptionFlags & EXCEPTION_UNWINDING))
            {
                PCODE ip = (PCODE)GetIP(pContext);
                if (IsIPInModule(GetClrModuleBase(), ip) || IsIPInModule(GCHeapUtilities::GetGCModuleBase(), ip))
                {
                    CONTRACT_VIOLATION(ThrowsViolation|FaultViolation);

                    //
                    // If you're debugging, set the debugger to catch first-chance AV's, then simply hit F5 or
                    // 'go' and continue after the assert. We'll recgonize that a debugger is attached, and
                    // return EXCEPTION_CONTINUE_EXECUTION. You'll re-execute the faulting instruction, and the
                    // debugger will stop at the AV. The value of EXCEPTION_CONTINUE_EXECUTION is -1, just in
                    // case you need to verify the return value for some reason. If you need to actually debug
                    // the failure path, then set your IP around the check below.
                    //
                    // You can also use Windbg's .cxr command to set the context to pContext.
                    //
#if defined(_DEBUG)
                    const char * pStack = "<stack not available>";
                    SString sStack;
                    if (GetStackTraceAtContext(sStack, pContext))
                    {
                        pStack = sStack.GetUTF8();
                    }

                    DWORD tid = GetCurrentThreadId();

                    BOOL debuggerPresentBeforeAssert = IsDebuggerPresent();


                    CONSISTENCY_CHECK_MSGF(false, ("AV in clr at this callstack:\n------\n%s\n-----\n.AV on tid=0x%x (%d), cxr=%p, exr=%p\n",
                        pStack, tid, tid, pContext, pExceptionRecord));

                    // @todo - this may not be what we want for interop-debugging...
                    //
                    // If there was no debugger before the assert, but there is one now, then go ahead and
                    // return EXCEPTION_CONTINUE_EXECUTION to re-execute the faulting instruction. This is
                    // supposed to be a nice little feature for CLR devs who attach debuggers on the "Av in
                    // mscorwks" assert above. Since this is only for that case, its only in debug builds.
                    if (!debuggerPresentBeforeAssert && IsDebuggerPresent())
                    {
                        return VEH_CONTINUE_EXECUTION;;
                    }
#endif // defined(_DEBUG)

                    EEPOLICY_HANDLE_FATAL_ERROR_USING_EXCEPTION_INFO(COR_E_EXECUTIONENGINE, pExceptionInfo);
                }
            }
        }
    }
    else if (exceptionCode == BOOTUP_EXCEPTION_COMPLUS)
    {
        // Don't handle a boot exception
        return VEH_CONTINUE_SEARCH;
    }

    return VEH_NO_ACTION;
}

#endif // !TARGET_UNIX

#if defined(FEATURE_HIJACK) && (!defined(TARGET_X86) || defined(TARGET_UNIX))

// This function is used to check if the specified IP is in the prolog or not.
bool IsIPInProlog(EECodeInfo *pCodeInfo)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    bool fInsideProlog = true;

    _ASSERTE(pCodeInfo->IsValid());

#ifdef TARGET_AMD64

    // Optimized version for AMD64 that doesn't need to go through the GC info decoding
    PTR_RUNTIME_FUNCTION funcEntry = pCodeInfo->GetFunctionEntry();

    // We should always get a function entry for a managed method
    _ASSERTE(funcEntry != NULL);

    // Get the unwindInfo from the function entry
    PUNWIND_INFO pUnwindInfo = (PUNWIND_INFO)(pCodeInfo->GetModuleBase() + funcEntry->UnwindData);

    // Check if the specified IP is beyond the prolog or not.
    DWORD prologLen = pUnwindInfo->SizeOfProlog;

#else // TARGET_AMD64

    GCInfoToken    gcInfoToken = pCodeInfo->GetGCInfoToken();

#ifdef USE_GC_INFO_DECODER

    GcInfoDecoder gcInfoDecoder(
        gcInfoToken,
        DECODE_PROLOG_LENGTH
    );

    DWORD prologLen = gcInfoDecoder.GetPrologSize();

#else // USE_GC_INFO_DECODER

    size_t prologLen;
    pCodeInfo->GetCodeManager()->IsInPrologOrEpilog(0, gcInfoToken, &prologLen);

#endif // USE_GC_INFO_DECODER

#endif // TARGET_AMD64

    if (pCodeInfo->GetRelOffset() >= prologLen)
    {
        fInsideProlog = false;
    }

    return fInsideProlog;
}

// This function is used to check if the specified IP is in the epilog or not.
bool IsIPInEpilog(PTR_CONTEXT pContextToCheck, EECodeInfo *pCodeInfo, BOOL *pSafeToInjectThreadAbort)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        PRECONDITION(pContextToCheck != NULL);
        PRECONDITION(ExecutionManager::IsManagedCode(GetIP(pContextToCheck)));
        PRECONDITION(pSafeToInjectThreadAbort != NULL);
    }
    CONTRACTL_END;

    TADDR ipToCheck = GetIP(pContextToCheck);

    _ASSERTE(pCodeInfo->IsValid());

    // The Codeinfo should correspond to the IP we are interested in.
    _ASSERTE(PCODEToPINSTR(ipToCheck) == pCodeInfo->GetCodeAddress());

    // By default, assume its safe to inject the abort.
    *pSafeToInjectThreadAbort = TRUE;

    // If we are inside a prolog, then we are obviously not in the epilog.
    // Its safe to inject the abort here.
    if (IsIPInProlog(pCodeInfo))
    {
        return false;
    }

    // We are not inside the prolog. We could either be in the middle of the method body or
    // inside the epilog. While unwindInfo contains the prolog length, it does not contain the
    // epilog length.
    //
    // Thus, to determine if we are inside the epilog, we use a property of RtlVirtualUnwind.
    // When invoked for an IP, it will return a NULL for personality routine in only two scenarios:
    //
    // 1) The unwindInfo does not contain any personality routine information, OR
    // 2) The IP is in prolog or epilog.
    //
    // For jitted code, (1) is not applicable since we *always* emit details of the managed personality routine
    // in the unwindInfo. Thus, since we have already determined that we are not inside the prolog, if performing
    // RtlVirtualUnwind against "ipToCheck" results in a NULL personality routine, it implies that we are inside
    // the epilog.

    DWORD_PTR imageBase = 0;
    CONTEXT tempContext;
    PVOID HandlerData;
    DWORD_PTR establisherFrame = 0;
    PEXCEPTION_ROUTINE personalityRoutine = NULL;

    // Lookup the function entry for the IP
    PTR_RUNTIME_FUNCTION funcEntry = pCodeInfo->GetFunctionEntry();

    // We should always get a function entry for a managed method
    _ASSERTE(funcEntry != NULL);

    imageBase = pCodeInfo->GetModuleBase();

    ZeroMemory(&tempContext, sizeof(CONTEXT));
    CopyOSContext(&tempContext, pContextToCheck);
    KNONVOLATILE_CONTEXT_POINTERS ctxPtrs;
    ZeroMemory(&ctxPtrs, sizeof(ctxPtrs));

    personalityRoutine = RtlVirtualUnwind(UNW_FLAG_EHANDLER,     // HandlerType
                     imageBase,
                     ipToCheck,
                     funcEntry,
                     &tempContext,
                     &HandlerData,
                     &establisherFrame,
                     &ctxPtrs);

    bool fIsInEpilog = false;

    if (personalityRoutine == NULL)
    {
        // We are in epilog.
        fIsInEpilog = true;

#ifdef TARGET_AMD64
        // Check if context pointers has returned the address of the stack location in the hijacked function
        // from where RBP was restored. If the address is NULL, then it implies that RBP has been popped off.
        // Since JIT64 ensures that pop of RBP is the last instruction before ret/jmp, it implies its not safe
        // to inject an abort @ this point as EstablisherFrame (which will be based
        // of RBP for managed code since that is the FramePointer register, as indicated in the UnwindInfo)
        // will be off and can result in bad managed exception dispatch.
        if (ctxPtrs.Rbp == NULL)
#endif
        {
            *pSafeToInjectThreadAbort = FALSE;
        }
    }

    return fIsInEpilog;
}

#endif // FEATURE_HIJACK && (!TARGET_X86 || TARGET_UNIX)

#define EXCEPTION_VISUALCPP_DEBUGGER        ((DWORD) (1<<30 | 0x6D<<16 | 5000))

#if defined(TARGET_X86)

// This holder is used to capture the FPU state, reset it to what the CLR expects
// and then restore the original state that was captured.
//
// FPU has a set of exception masks which the CLR expects to be always set,
// implying that any corresponding condition will *not* result in FPU raising
// an exception.
//
// However, native code (e.g. high precision math libs) can change this mask.
// Thus, when control enters the CLR (e.g. via exception dispatch into the VEH),
// we will end up using floating point instructions that could satify the exception mask
// condition and raise an exception. This could result in an infinite loop, resulting in
// SO.
//
// We use this holder to protect applicable parts of the runtime from running into such cases.
extern "C" void CaptureFPUContext(BYTE *pFPBUBuf);
extern "C" void RestoreFPUContext(BYTE *pFPBUBuf);

// This is FPU specific and only applicable to x86 on Windows.
class FPUStateHolder
{
    // Capturing FPU state requires a 28byte buffer
    BYTE m_bufFPUState[28];

public:
    FPUStateHolder()
    {
        LIMITED_METHOD_CONTRACT;

        BYTE *pFPUBuf = m_bufFPUState;

        // Save the FPU state using the non-waiting instruction
        // so that FPU may not raise an exception incase the
        // exception masks are unset in the FPU Control Word
        CaptureFPUContext(pFPUBuf);

        // Reset the FPU state
        ResetCurrentContext();
    }

    ~FPUStateHolder()
    {
        LIMITED_METHOD_CONTRACT;

        BYTE *pFPUBuf = m_bufFPUState;

        // Restore the capture FPU state
        RestoreFPUContext(pFPUBuf);
    }
};

#endif // defined(TARGET_X86)

#ifndef TARGET_UNIX

LONG WINAPI CLRVectoredExceptionHandlerShim(PEXCEPTION_POINTERS pExceptionInfo)
{
    //
    // DO NOT USE CONTRACTS HERE AS THIS ROUTINE MAY NEVER RETURN.  You can use
    // static contracts, but currently this is all WRAPPER_NO_CONTRACT.
    //

    //
    // WARNING WARNING WARNING WARNING WARNING WARNING WARNING
    //
    // o This function should not call functions that acquire
    //   synchronization objects or allocate memory, because this
    //   can cause problems.  <-- quoteth MSDN  -- probably for
    //   the same reason as we cannot use LOG(); we'll recurse
    //   into a stack overflow.
    //
    // o You cannot use LOG() in here because that will trigger an
    //   exception which will cause infinite recursion with this
    //   function.  We work around this by ignoring all non-error
    //   exception codes, which serves as the base of the recursion.
    //   That way, we can LOG() from the rest of the function
    //
    // The same goes for any function called by this
    // function.
    //
    // WARNING WARNING WARNING WARNING WARNING WARNING WARNING
    //

    // If exceptions (or runtime) have been disabled, then simply return.
    if (g_fForbidEnterEE || g_fNoExceptions)
    {
        return EXCEPTION_CONTINUE_SEARCH;
    }

#ifdef FEATURE_EH_FUNCLETS
    pExceptionInfo->ContextRecord->ContextFlags |= CONTEXT_EXCEPTION_ACTIVE;
#endif // FEATURE_EH_FUNCLETS

    // WARNING
    //
    // We must preserve this so that GCStress=4 eh processing doesnt kill last error.
    // Note that even GetThread below can affect the LastError.
    // Keep this in mind when adding code above this line!
    //
    // WARNING
    DWORD dwLastError = GetLastError();

#if defined(TARGET_X86)
    // Capture the FPU state before we do anything involving floating point instructions
    FPUStateHolder captureFPUState;
#endif // defined(TARGET_X86)

#ifdef FEATURE_INTEROP_DEBUGGING
    // For interop debugging we have a fancy exception queueing stunt. When the debugger
    // initially gets the first chance exception notification it may not know whether to
    // continue it handled or unhandled, but it must continue the process to allow the
    // in-proc helper thread to work. What it does is continue the exception unhandled which
    // will let the thread immediately execute to this point. Inside this worker the thread
    // will block until the debugger knows how to continue the exception. If it decides the
    // exception was handled then we immediately resume execution as if the exception had never
    // even been allowed to run into this handler. If it is unhandled then we keep processing
    // this handler
    //
    // WARNING: This function could potentially throw an exception, however it should only
    // be able to do so when an interop debugger is attached
    if (g_pDebugInterface != NULL)
    {
        if (g_pDebugInterface->FirstChanceSuspendHijackWorker(pExceptionInfo->ContextRecord,
            pExceptionInfo->ExceptionRecord) == EXCEPTION_CONTINUE_EXECUTION)
            return EXCEPTION_CONTINUE_EXECUTION;
    }
#endif


    DWORD dwCode = pExceptionInfo->ExceptionRecord->ExceptionCode;
    if (dwCode == DBG_PRINTEXCEPTION_C || dwCode == EXCEPTION_VISUALCPP_DEBUGGER)
    {
        return EXCEPTION_CONTINUE_SEARCH;
    }

#if defined(TARGET_X86)
    if (dwCode == EXCEPTION_BREAKPOINT || dwCode == EXCEPTION_SINGLE_STEP)
    {
        // For interop debugging, debugger bashes our managed exception handler.
        // Interop debugging does not work with real vectored exception handler :(
        return EXCEPTION_CONTINUE_SEARCH;
    }
#endif

    if (NtCurrentTeb()->ThreadLocalStoragePointer == NULL)
    {
        // Ignore exceptions early during thread startup before the thread is fully initialized by the OS
        return EXCEPTION_CONTINUE_SEARCH;
    }

    bool bIsGCMarker = false;

#ifdef USE_REDIRECT_FOR_GCSTRESS
    // This is AMD64 & ARM specific as the macro above is defined for AMD64 & ARM only
    bIsGCMarker = IsGcMarker(pExceptionInfo->ContextRecord, pExceptionInfo->ExceptionRecord);
#elif defined(TARGET_X86) && defined(HAVE_GCCOVER)
    // This is the equivalent of the check done in COMPlusFrameHandler, incase the exception is
    // seen by VEH first on x86.
    bIsGCMarker = IsGcMarker(pExceptionInfo->ContextRecord, pExceptionInfo->ExceptionRecord);
#endif // USE_REDIRECT_FOR_GCSTRESS

    // Do not update the TLS with exception details for exceptions pertaining to GCStress
    // as they are continueable in nature.
    if (!bIsGCMarker)
    {
        SaveCurrentExceptionInfo(pExceptionInfo->ExceptionRecord, pExceptionInfo->ContextRecord);
    }


    LONG result = EXCEPTION_CONTINUE_SEARCH;

    // If we cannot obtain a Thread object, then we have no business processing any
    // exceptions on this thread.  Indeed, even checking to see if the faulting
    // address is in JITted code is problematic if we have no Thread object, since
    // this thread will bypass all our locks.
    Thread *pThread = GetThreadNULLOk();

    if (pThread)
    {
        // Fiber-friendly Vectored Exception Handling:
        // Check if the current and the cached stack-base match.
        // If they don't match then probably the thread is running on a different Fiber
        // than during the initialization of the Thread-object.
        void* stopPoint = pThread->GetCachedStackBase();
        void* currentStackBase = Thread::GetStackUpperBound();
        if (currentStackBase != stopPoint)
        {
            CantAllocHolder caHolder;
            STRESS_LOG2(LF_EH, LL_INFO100, "CLRVectoredExceptionShim: mismatch of cached and current stack-base "
                "indicating use of Fibers, return with EXCEPTION_CONTINUE_SEARCH: current = %p; cache = %p\n",
                currentStackBase, stopPoint);
            return EXCEPTION_CONTINUE_SEARCH;
        }
    }

    // We are going to process the exception only if one of the following conditions is true:
    //
    // 1) We have a valid Thread object (implies exception on managed thread)
    // 3) The exception occurred in a GC marked location when no thread exists (i.e. reverse P/Invoke with UnmanagedCallersOnlyAttribute).
    if (pThread)
    {
        if (!bIsGCMarker)
        {
            VEH_ACTION action = CLRVectoredExceptionHandler(pExceptionInfo);

#ifdef FEATURE_EH_FUNCLETS
            if (VEH_EXECUTE_HANDLE_MANAGED_EXCEPTION == action)
            {
                //
                // HandleManagedFault may never return, so we cannot use a forbid fault region around it.
                //
                if (g_isNewExceptionHandlingEnabled)
                {
                    HandleManagedFaultNew(pExceptionInfo->ExceptionRecord, pExceptionInfo->ContextRecord);
                }
                else
                {
                    HandleManagedFault(pExceptionInfo->ExceptionRecord, pExceptionInfo->ContextRecord);
                }
                return EXCEPTION_CONTINUE_EXECUTION;
            }
#endif // FEATURE_EH_FUNCLETS

            if (VEH_EXECUTE_HANDLER == action)
            {
                result = EXCEPTION_CONTINUE_SEARCH;
            }
            else
            {
                _ASSERTE((action == VEH_CONTINUE_EXECUTION) || (action == VEH_CONTINUE_SEARCH));
                result = (LONG)action;
            }

        }
        else
        {
            result = EXCEPTION_CONTINUE_EXECUTION;
        }

#ifdef _DEBUG
#ifndef FEATURE_EH_FUNCLETS
        {
            CantAllocHolder caHolder;

            PEXCEPTION_REGISTRATION_RECORD pRecord = GetCurrentSEHRecord();
            while (pRecord != EXCEPTION_CHAIN_END)
            {
                STRESS_LOG2(LF_EH, LL_INFO10000, "CLRVectoredExceptionHandlerShim: FS:0 %p:%p\n",
                            pRecord, pRecord->Handler);
                pRecord = pRecord->Next;
            }
        }
#endif // FEATURE_EH_FUNCLETS

        {
            // The call to "CLRVectoredExceptionHandler" above can return EXCEPTION_CONTINUE_SEARCH
            // for different scenarios like StackOverFlow/SOFT_SO, or if it is forbidden to enter the EE.
            // Thus, if we dont have a Thread object for the thread that has faulted and we came this far
            // because the fault was in MSCORWKS, then we work with the frame chain below only if we have
            // valid Thread object.

            if (pThread)
            {
                CantAllocHolder caHolder;

                TADDR* sp;
                sp = (TADDR*)&sp;
                DWORD count = 0;
                void* stopPoint = pThread->GetCachedStackBase();
                // If Frame chain is corrupted, we may get AV while accessing frames, and this function will be
                // called recursively.  We use Frame chain to limit our search range.  It is not disaster if we
                // can not use it.
                // Find the stop point (most jitted function)
                Frame* pFrame = pThread->GetFrame();
                for(;;)
                {
                    // skip GC frames
                    if (pFrame == 0 || pFrame == (Frame*) -1)
                        break;

                    Frame::ETransitionType type = pFrame->GetTransitionType();
                    if (type == Frame::TT_M2U || type == Frame::TT_InternalCall)
                    {
                        stopPoint = pFrame;
                        break;
                    }
                    pFrame = pFrame->Next();
                }
                STRESS_LOG0(LF_EH, LL_INFO100, "CLRVectoredExceptionHandlerShim: stack");
                while (count < 20 && sp < stopPoint)
                {
                    sp += 1;
                }
            }
        }
#endif // _DEBUG

#ifndef FEATURE_EH_FUNCLETS
        {
            CantAllocHolder caHolder;
            STRESS_LOG1(LF_EH, LL_INFO1000, "CLRVectoredExceptionHandlerShim: returning %d\n", result);
        }
#endif // FEATURE_EH_FUNCLETS

    }
    else if (bIsGCMarker)
    {
        _ASSERTE(pThread == NULL);
        result = EXCEPTION_CONTINUE_EXECUTION;
    }

    SetLastError(dwLastError);

    return result;
}

#endif // !TARGET_UNIX

// Contains the handle to the registered VEH
static PVOID g_hVectoredExceptionHandler = NULL;

void CLRAddVectoredHandlers(void)
{
#ifndef TARGET_UNIX

    // We now install a vectored exception handler on all supporting Windows architectures.
    g_hVectoredExceptionHandler = AddVectoredExceptionHandler(TRUE, (PVECTORED_EXCEPTION_HANDLER)CLRVectoredExceptionHandlerShim);
    if (g_hVectoredExceptionHandler == NULL)
    {
        LOG((LF_EH, LL_INFO100, "CLRAddVectoredHandlers: AddVectoredExceptionHandler() failed\n"));
        COMPlusThrowHR(E_FAIL);
    }

    LOG((LF_EH, LL_INFO100, "CLRAddVectoredHandlers: AddVectoredExceptionHandler() succeeded\n"));
#endif // !TARGET_UNIX
}

// This function removes the vectored exception and continue handler registration
// from the OS.
void CLRRemoveVectoredHandlers(void)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;
#ifndef TARGET_UNIX

    // Unregister the vectored exception handler if one is registered (and we can).
    if (g_hVectoredExceptionHandler != NULL)
    {
        // Unregister the vectored exception handler
        if (RemoveVectoredExceptionHandler(g_hVectoredExceptionHandler) == FALSE)
        {
            LOG((LF_EH, LL_INFO100, "CLRRemoveVectoredHandlers: RemoveVectoredExceptionHandler() failed.\n"));
        }
        else
        {
            LOG((LF_EH, LL_INFO100, "CLRRemoveVectoredHandlers: RemoveVectoredExceptionHandler() succeeded.\n"));
        }
    }
#endif // !TARGET_UNIX
}

//
// This does the work of the Unwind and Continue Hanlder inside the catch clause of that handler. The stack has not
// been unwound when this is called. Keep that in mind when deciding where to put new code :)
//
void UnwindAndContinueRethrowHelperInsideCatch(Frame* pEntryFrame, Exception* pException)
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_TRIGGERS;
    STATIC_CONTRACT_MODE_ANY;

    Thread* pThread = GetThread();

    GCX_COOP();

    LOG((LF_EH, LL_INFO1000, "UNWIND_AND_CONTINUE inside catch, unwinding frame chain\n"));

    // This SetFrame is OK because we will not have frames that require ExceptionUnwind in strictly unmanaged EE
    // code chunks which is all that an UnC handler can guard.
    //
    // @todo: we'd rather use UnwindFrameChain, but there is a concern: some of the ExceptionUnwind methods on some
    // of the Frame types do a great deal of work; load classes, throw exceptions, etc. We need to decide on some
    // policy here. Do we want to let such functions throw, etc.? Right now, we believe that there are no such
    // frames on the stack to be unwound, so the SetFrame is alright (see the first comment above.) At the very
    // least, we should add some way to assert that.
    pThread->SetFrame(pEntryFrame);

#ifdef _DEBUG
    {
        CONTRACT_VIOLATION(ThrowsViolation);
        // Call CLRException::GetThrowableFromException to force us to retrieve the THROWABLE
        // while we are still within the context of the catch block. This will help diagnose
        // cases where the last thrown object is NULL.
        OBJECTREF orThrowable = CLRException::GetThrowableFromException(pException);
        CONSISTENCY_CHECK(orThrowable != NULL);
    }
#endif
}

#ifdef FEATURE_EH_FUNCLETS
//
// This function continues exception interception unwind after it crossed native frames using
// standard EH / SEH.
//
VOID DECLSPEC_NORETURN ContinueExceptionInterceptionUnwind()
{
    STATIC_CONTRACT_THROWS;
    STATIC_CONTRACT_GC_TRIGGERS;
    STATIC_CONTRACT_MODE_ANY;

    GCX_COOP();

    Thread *pThread = GetThread();
    ThreadExceptionState* pExState = pThread->GetExceptionState();
    UINT_PTR uInterceptStackFrame  = 0;

    pExState->GetDebuggerState()->GetDebuggerInterceptInfo(NULL, NULL,
                                                        (PBYTE*)&uInterceptStackFrame,
                                                        NULL, NULL);

    PREPARE_NONVIRTUAL_CALLSITE(METHOD__EH__UNWIND_AND_INTERCEPT);
    DECLARE_ARGHOLDER_ARRAY(args, 2);
    args[ARGNUM_0] = PTR_TO_ARGHOLDER((ExInfo*)pExState->GetCurrentExceptionTracker());
    args[ARGNUM_1] = PTR_TO_ARGHOLDER(uInterceptStackFrame);
    pThread->IncPreventAbort();

    //Ex.RhUnwindAndIntercept(throwable, &exInfo)
    CRITICAL_CALLSITE;
    CALL_MANAGED_METHOD_NORET(args)

    UNREACHABLE();
}

#endif // FEATURE_EH_FUNCLETS

//
// This does the work of the Unwind and Continue Hanlder after the catch clause of that handler. The stack has been
// unwound by the time this is called. Keep that in mind when deciding where to put new code :)
//
VOID DECLSPEC_NORETURN UnwindAndContinueRethrowHelperAfterCatch(Frame* pEntryFrame, Exception* pException, bool nativeRethrow)
{
    STATIC_CONTRACT_THROWS;
    STATIC_CONTRACT_GC_TRIGGERS;
    STATIC_CONTRACT_MODE_ANY;

    GCX_COOP();

    LOG((LF_EH, LL_INFO1000, "UNWIND_AND_CONTINUE caught and will rethrow\n"));

    OBJECTREF orThrowable = CLRException::GetThrowableFromException(pException);
    LOG((LF_EH, LL_INFO1000, "UNWIND_AND_CONTINUE got throwable %p\n",
        OBJECTREFToObject(orThrowable)));

    Exception::Delete(pException);

#ifdef FEATURE_EH_FUNCLETS
    if (g_isNewExceptionHandlingEnabled && !nativeRethrow)
    {
        Thread *pThread = GetThread();
        ThreadExceptionState* pExState = pThread->GetExceptionState();
        ExInfo *pPrevExInfo = (ExInfo*)pExState->GetCurrentExceptionTracker();
        if (pPrevExInfo != NULL && pPrevExInfo->m_DebuggerExState.GetDebuggerInterceptContext() != NULL)
        {
            ContinueExceptionInterceptionUnwind();
            UNREACHABLE();
        }
        else
        {
            DispatchManagedException(orThrowable);
        }
    }
    else
#endif // FEATURE_EH_FUNCLETS
    {
        RaiseTheExceptionInternalOnly(orThrowable, FALSE);
    }
}

thread_local DWORD t_dwCurrentExceptionCode;
thread_local PEXCEPTION_RECORD t_pCurrentExceptionRecord;
thread_local PCONTEXT t_pCurrentExceptionContext;

void GetCurrentExceptionPointers(PEXCEPTION_POINTERS pExceptionInfo DEBUG_ARG(bool checkExceptionRecordLocation))
{
    WRAPPER_NO_CONTRACT;

    pExceptionInfo->ContextRecord = t_pCurrentExceptionContext;
    pExceptionInfo->ExceptionRecord = t_pCurrentExceptionRecord;

#ifdef _DEBUG
    if (pExceptionInfo->ExceptionRecord != NULL && checkExceptionRecordLocation)
    {
        _ASSERTE((PVOID)(pExceptionInfo->ExceptionRecord) > (PVOID)(&checkExceptionRecordLocation));
    }
#endif
}

DWORD GetCurrentExceptionCode()
{
    WRAPPER_NO_CONTRACT;
    SUPPORTS_DAC_HOST_ONLY;

    return t_dwCurrentExceptionCode;
}

void SaveCurrentExceptionInfo(PEXCEPTION_RECORD pRecord, PCONTEXT pContext)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    if ((pRecord->ExceptionFlags & (EXCEPTION_UNWINDING | EXCEPTION_EXIT_UNWIND)))
    {
        // If exception is unwinding the stack, the ExceptionCode may have been changed to
        // STATUS_UNWIND if RtlUnwind is called with a NULL ExceptionRecord.
        // Since we have captured exception info in the first pass, we don't need to capture it again.
        return;
    }

    BOOL fSave = TRUE;
    if (pRecord->ExceptionCode != STATUS_STACK_OVERFLOW)
    {
        DWORD dwLastExceptionCode = t_dwCurrentExceptionCode;
        if (dwLastExceptionCode == STATUS_STACK_OVERFLOW)
        {
            PEXCEPTION_RECORD lastRecord = t_pCurrentExceptionRecord;

            // We are trying to see if C++ is attempting a rethrow of a SO exception. If so,
            // we want to prevent updating the exception details in the TLS. This is a workaround,
            // as explained below.
            if (pRecord->ExceptionCode == EXCEPTION_MSVC)
            {
                // This is a workaround.
                // When C++ rethrows, C++ internally gets rid of the new exception record after
                // unwinding stack, and present the original exception record to the thread.
                // When we get VC's support to obtain exception record in try/catch, we will replace
                // this code.
                if (pRecord < lastRecord)
                {
                    // For the C++ rethrow workaround, ensure that the last exception record is still valid and as we expect it to be.
                    //
                    // Its possible that we are still below the address of last exception record
                    // but since the execution stack could have changed, simply comparing its address
                    // with the address of the current exception record may not be enough.
                    //
                    // Thus, ensure that its still valid and holds the exception code we expect it to
                    // have (i.e. value in dwLastExceptionCode).
                    if ((lastRecord != NULL) && (lastRecord->ExceptionCode == dwLastExceptionCode))
                    {
                        fSave = FALSE;
                    }
                }
            }
        }
    }
    if (fSave)
    {
        t_dwCurrentExceptionCode = pRecord->ExceptionCode;
        t_pCurrentExceptionRecord = pRecord;
        t_pCurrentExceptionContext = pContext;
    }
}

#ifndef DACCESS_COMPILE
//******************************************************************************
//
// NotifyOfCHFFilterWrapper
//
// Helper function to deliver notifications of CatchHandlerFound inside a
//  EX_TRY/EX_CATCH.
//
// Parameters:
//   pExceptionInfo - the pExceptionInfo passed to a filter function.
//   pCatcherStackAddr - a Frame* from the PAL_TRY/PAL_EXCEPT_FILTER site.
//
// Return:
//   always returns EXCEPTION_CONTINUE_SEARCH.
//
//******************************************************************************
LONG NotifyOfCHFFilterWrapper(
    EXCEPTION_POINTERS *pExceptionInfo, // the pExceptionInfo passed to a filter function.
    PVOID               pParam)         // contains a Frame* from the PAL_TRY/PAL_EXCEPT_FILTER site.
{
    LIMITED_METHOD_CONTRACT;

    PVOID pCatcherStackAddr = ((NotifyOfCHFFilterWrapperParam *)pParam)->pFrame;
    ULONG ret = EXCEPTION_CONTINUE_SEARCH;

    // We are here to send an event notification to the debugger and to the appdomain.  To
    //  determine if it is safe to send these notifications, check the following:
    // 1) The thread object has been set up.
    // 2) The thread has an exception on it.
    // 3) The exception is the same as the one this filter is called on.
    Thread *pThread = GetThreadNULLOk();
    if ( (pThread == NULL)  ||
         (pThread->GetExceptionState()->GetContextRecord() == NULL)  ||
         (GetSP(pThread->GetExceptionState()->GetContextRecord()) != GetSP(pExceptionInfo->ContextRecord) ) )
    {
        LOG((LF_EH, LL_INFO1000, "NotifyOfCHFFilterWrapper: not sending notices. pThread: %0x8", pThread));
        if (pThread)
        {
            LOG((LF_EH, LL_INFO1000, ", Thread SP: %0x8, Exception SP: %08x",
                 pThread->GetExceptionState()->GetContextRecord() ? GetSP(pThread->GetExceptionState()->GetContextRecord()) : NULL,
                 pExceptionInfo->ContextRecord ? GetSP(pExceptionInfo->ContextRecord) : NULL ));
        }
        LOG((LF_EH, LL_INFO1000, "\n"));
        return ret;
    }

    if (g_pDebugInterface)
    {
        // It looks safe, so make the debugger notification.
        ret = g_pDebugInterface->NotifyOfCHFFilter(pExceptionInfo, pCatcherStackAddr);
    }

    return ret;
} // LONG NotifyOfCHFFilterWrapper()

// This filter will be used process exceptions escaping out of AD transition boundaries
// that are not at the base of the managed thread. Those are handled in ThreadBaseRedirectingFilter.
// This will be invoked when an exception is going unhandled from the called AppDomain.
//
// This can be used to do last moment work before the exception gets caught by the EX_CATCH setup
// at the AD transition point.
LONG AppDomainTransitionExceptionFilter(
    EXCEPTION_POINTERS *pExceptionInfo, // the pExceptionInfo passed to a filter function.
    PVOID               pParam)
{
    // Ideally, we would be NOTHROW here. However, NotifyOfCHFFilterWrapper calls into
    // NotifyOfCHFFilter that is THROWS. Thus, to prevent contract violation,
    // we abide by the rules and be THROWS.
    //
    // Same rationale for GC_TRIGGERS as well.
    CONTRACTL
    {
        GC_TRIGGERS;
        MODE_ANY;
        THROWS;
    }
    CONTRACTL_END;

    ULONG ret = EXCEPTION_CONTINUE_SEARCH;

    // First, call into NotifyOfCHFFilterWrapper
    ret = NotifyOfCHFFilterWrapper(pExceptionInfo, pParam);

#ifndef TARGET_UNIX
    // Setup the watson bucketing details if the escaping
    // exception is preallocated.
    if (SetupWatsonBucketsForEscapingPreallocatedExceptions())
    {
        // Set the flag that these were captured at AD Transition
        DEBUG_STMT(GetThread()->GetExceptionState()->GetUEWatsonBucketTracker()->SetCapturedAtADTransition());
    }

    // Attempt to capture buckets for non-preallocated exceptions just before the AppDomain transition boundary
    {
        GCX_COOP();
        OBJECTREF oThrowable = GetThread()->GetThrowable();
        if ((oThrowable != NULL) && (CLRException::IsPreallocatedExceptionObject(oThrowable) == FALSE))
        {
            SetupWatsonBucketsForNonPreallocatedExceptions();
        }
    }
#endif // !TARGET_UNIX

    return ret;
} // LONG AppDomainTransitionExceptionFilter()

// This filter will be used process exceptions escaping out of dynamic reflection invocation as
// unhandled and will eventually be caught in the VM to be made as inner exception of
// TargetInvocationException that will be thrown from the VM.
LONG ReflectionInvocationExceptionFilter(
    EXCEPTION_POINTERS *pExceptionInfo, // the pExceptionInfo passed to a filter function.
    PVOID               pParam)
{
    // Ideally, we would be NOTHROW here. However, NotifyOfCHFFilterWrapper calls into
    // NotifyOfCHFFilter that is THROWS. Thus, to prevent contract violation,
    // we abide by the rules and be THROWS.
    //
    // Same rationale for GC_TRIGGERS as well.
    CONTRACTL
    {
        GC_TRIGGERS;
        MODE_ANY;
        THROWS;
    }
    CONTRACTL_END;

    ULONG ret = EXCEPTION_CONTINUE_SEARCH;

    // First, call into NotifyOfCHFFilterWrapper
    ret = NotifyOfCHFFilterWrapper(pExceptionInfo, pParam);

#ifndef TARGET_UNIX
    // Setup the watson bucketing details if the escaping
    // exception is preallocated.
    if (SetupWatsonBucketsForEscapingPreallocatedExceptions())
    {
        // Set the flag that these were captured during Reflection Invocation
        DEBUG_STMT(GetThread()->GetExceptionState()->GetUEWatsonBucketTracker()->SetCapturedAtReflectionInvocation());
    }

    // Attempt to capture buckets for non-preallocated exceptions just before the ReflectionInvocation boundary
    {
        GCX_COOP();
        OBJECTREF oThrowable = GetThread()->GetThrowable();
        if ((oThrowable != NULL) && (CLRException::IsPreallocatedExceptionObject(oThrowable) == FALSE))
        {
            SetupWatsonBucketsForNonPreallocatedExceptions();
        }
    }
#endif // !TARGET_UNIX

    return ret;
} // LONG ReflectionInvocationExceptionFilter()

#endif // !DACCESS_COMPILE

#ifdef _DEBUG
bool DebugIsEECxxExceptionPointer(void* pv)
{
    CONTRACTL
    {
        DISABLED(NOTHROW);
        GC_NOTRIGGER;
        MODE_ANY;
        DEBUG_ONLY;
    }
    CONTRACTL_END;

    if (pv == NULL)
    {
        return false;
    }

    // check whether the memory is readable in no-throw way
    if (!isMemoryReadable((TADDR)pv, sizeof(UINT_PTR)))
    {
        return false;
    }

    bool retVal = false;

    EX_TRY
    {
        UINT_PTR  vtbl  = *(UINT_PTR*)pv;

        // ex.h

        HRException             boilerplate1;
        COMException            boilerplate2;
        SEHException            boilerplate3;

        // clrex.h

        CLRException            boilerplate4;
        CLRLastThrownObjectException boilerplate5;
        EEException             boilerplate6;
        EEMessageException      boilerplate7;
        EEResourceException     boilerplate8;

#ifdef FEATURE_COMINTEROP
        // EECOMException::~EECOMException calls FreeExceptionData, which is GC_TRIGGERS,
        // but it won't trigger in this case because EECOMException's members remain NULL.
        CONTRACT_VIOLATION(GCViolation);
        EECOMException          boilerplate9;
#endif // FEATURE_COMINTEROP

        EEFieldException        boilerplate10;
        EEMethodException       boilerplate11;
        EEArgumentException     boilerplate12;
        EETypeLoadException     boilerplate13;
        EEFileLoadException     boilerplate14;
        ObjrefException         boilerplate15;

        UINT_PTR    ValidVtbls[] =
        {
            *((TADDR*)&boilerplate1),
            *((TADDR*)&boilerplate2),
            *((TADDR*)&boilerplate3),
            *((TADDR*)&boilerplate4),
            *((TADDR*)&boilerplate5),
            *((TADDR*)&boilerplate6),
            *((TADDR*)&boilerplate7),
            *((TADDR*)&boilerplate8),
#ifdef FEATURE_COMINTEROP
            *((TADDR*)&boilerplate9),
#endif // FEATURE_COMINTEROP
            *((TADDR*)&boilerplate10),
            *((TADDR*)&boilerplate11),
            *((TADDR*)&boilerplate12),
            *((TADDR*)&boilerplate13),
            *((TADDR*)&boilerplate14),
            *((TADDR*)&boilerplate15)
        };

        const int nVtbls = sizeof(ValidVtbls) / sizeof(ValidVtbls[0]);

        for (int i = 0; i < nVtbls; i++)
        {
            if (vtbl == ValidVtbls[i])
            {
                retVal = true;
                break;
            }
        }
    }
    EX_CATCH
    {
        // Swallow any exception out of the exception constructors above and simply return false.
    }
    EX_END_CATCH(SwallowAllExceptions);

    return retVal;
}

void *DebugGetCxxException(EXCEPTION_RECORD* pExceptionRecord);

bool DebugIsEECxxException(EXCEPTION_RECORD* pExceptionRecord)
{
    return DebugIsEECxxExceptionPointer(DebugGetCxxException(pExceptionRecord));
}

//
// C++ EH cracking material gleaned from the debugger:
// (DO NOT USE THIS KNOWLEDGE IN NON-DEBUG CODE!!!)
//
// EHExceptionRecord::EHParameters
//     [0] magicNumber      : uint
//     [1] pExceptionObject : void*
//     [2] pThrowInfo       : ThrowInfo*

#ifdef HOST_64BIT
#define NUM_CXX_EXCEPTION_PARAMS 4
#else
#define NUM_CXX_EXCEPTION_PARAMS 3
#endif

void *DebugGetCxxException(EXCEPTION_RECORD* pExceptionRecord)
{
    WRAPPER_NO_CONTRACT;

    bool fExCodeIsCxx           = (EXCEPTION_MSVC == pExceptionRecord->ExceptionCode);
    bool fExHasCorrectNumParams = (NUM_CXX_EXCEPTION_PARAMS == pExceptionRecord->NumberParameters);

    if (fExCodeIsCxx && fExHasCorrectNumParams)
    {
        void** ppException = (void**)pExceptionRecord->ExceptionInformation[1];

        if (NULL == ppException)
        {
            return NULL;
        }

        return *ppException;

    }

    CONSISTENCY_CHECK_MSG(!fExCodeIsCxx || fExHasCorrectNumParams, "We expected an EXCEPTION_MSVC exception to have 3 parameters.  Did the CRT change its exception format?");

    return NULL;
}

#endif // _DEBUG

#endif // #ifndef DACCESS_COMPILE

BOOL IsException(MethodTable *pMT) {
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        SUPPORTS_DAC;
    }
    CONTRACTL_END;

    ASSERT(g_pExceptionClass != NULL);

    while (pMT != NULL && pMT != g_pExceptionClass) {
        pMT = pMT->GetParentMethodTable();
    }

    return pMT != NULL;
} // BOOL IsException()

// Returns TRUE iff calling get_StackTrace on an exception of the given type ends up
// executing some other code than just Exception.get_StackTrace.
BOOL ExceptionTypeOverridesStackTraceGetter(PTR_MethodTable pMT)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
        SUPPORTS_DAC;
    }
    CONTRACTL_END;

    _ASSERTE(IsException(pMT));

    if (pMT == g_pExceptionClass)
    {
        // if the type is System.Exception, it certainly doesn't override anything
        return FALSE;
    }

    // find the slot corresponding to get_StackTrace
    for (DWORD slot = g_pObjectClass->GetNumVirtuals(); slot < g_pExceptionClass->GetNumVirtuals(); slot++)
    {
        MethodDesc *pMD = g_pExceptionClass->GetMethodDescForSlot(slot);
        LPCUTF8 name = pMD->GetName();

        if (name != NULL && strcmp(name, "get_StackTrace") == 0)
        {
            // see if the slot is overridden by pMT
            MethodDesc *pDerivedMD = pMT->GetMethodDescForSlot(slot);
            return (pDerivedMD != pMD);
        }
    }

    // there must be get_StackTrace on System.Exception
    UNREACHABLE();
}

// Removes source file names/paths and line information from a stack trace generated
// by Environment.GetStackTrace.
void StripFileInfoFromStackTrace(SString &ssStackTrace)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
        SUPPORTS_DAC;
    }
    CONTRACTL_END;

    SString::Iterator i = ssStackTrace.Begin();
    SString::Iterator end;
    int countBracket = 0;
    int position = 0;

    while (i < ssStackTrace.End())
    {
        if (i[0] == W('('))
        {
            countBracket ++;
        }
        else if (i[0] == W(')'))
        {
            if (countBracket == 1)
            {
                end = i + 1;
                SString::Iterator j = i + 1;
                while (j < ssStackTrace.End())
                {
                    if (j[0] == W('\r') || j[0] == W('\n'))
                    {
                        break;
                    }
                    j++;
                }
                if (j > end)
                {
                    ssStackTrace.Delete(end,j-end);
                    i = ssStackTrace.Begin() + position;
                }
            }
            countBracket --;
        }
        i ++;
        position ++;
    }
    ssStackTrace.Truncate(end);
}

#ifdef _DEBUG
//==============================================================================
// This function will set a thread state indicating if an exception is escaping
// the last CLR personality routine on the stack in a reverse pinvoke scenario.
//
// If the exception continues to go unhandled, it will eventually reach the OS
// that will start invoking the UEFs. Since CLR registers its UEF only to handle
// unhandled exceptions on such reverse pinvoke threads, we will assert this
// state in our UEF to ensure it does not get called for any other reason.
//
// This function should be called only if the personality routine returned
// EXCEPTION_CONTINUE_SEARCH.
//==============================================================================
void SetReversePInvokeEscapingUnhandledExceptionStatus(BOOL fIsUnwinding,
#if defined(TARGET_X86)
                                                       EXCEPTION_REGISTRATION_RECORD * pEstablisherFrame
#elif defined(FEATURE_EH_FUNCLETS)
                                                       PVOID pEstablisherFrame
#else
#error Unsupported platform
#endif
                                                       )
{
#ifndef DACCESS_COMPILE

    LIMITED_METHOD_CONTRACT;

    Thread *pCurThread = GetThread();
    if (pCurThread->GetExceptionState()->IsExceptionInProgress())
    {
        if (!fIsUnwinding)
        {
            // Get the top-most Frame of this thread.
            Frame* pCurFrame = pCurThread->GetFrame();
            Frame* pTopMostFrame = pCurFrame;
            while (pCurFrame && (pCurFrame != FRAME_TOP))
            {
                pTopMostFrame = pCurFrame;
                pCurFrame = pCurFrame->PtrNextFrame();
            }

            // Is the exception escaping the last CLR personality routine on the stack of a
            // reverse pinvoke thread?
            if (((pTopMostFrame == NULL) || (pTopMostFrame == FRAME_TOP)) ||
                ((void *)(pEstablisherFrame) > (void *)(pTopMostFrame)))
            {
                LOG((LF_EH, LL_INFO100, "SetReversePInvokeEscapingUnhandledExceptionStatus: setting Ex_RPInvokeEscapingException\n"));
                // Set the flag on the thread indicating the exception is escaping the
                // top most reverse pinvoke exception handler.
                pCurThread->GetExceptionState()->GetFlags()->SetReversePInvokeEscapingException();
            }
        }
        else
        {
            // Since we are unwinding, simply unset the flag indicating escaping unhandled exception
            // if it was set.
            if (pCurThread->GetExceptionState()->GetFlags()->ReversePInvokeEscapingException())
            {
                LOG((LF_EH, LL_INFO100, "SetReversePInvokeEscapingUnhandledExceptionStatus: unsetting Ex_RPInvokeEscapingException\n"));
                pCurThread->GetExceptionState()->GetFlags()->ResetReversePInvokeEscapingException();
            }
        }
    }
    else
    {
        LOG((LF_EH, LL_INFO100, "SetReversePInvokeEscapingUnhandledExceptionStatus: not setting Ex_RPInvokeEscapingException since no exception is in progress.\n"));
    }
#endif // !DACCESS_COMPILE
}

#endif // _DEBUG

#ifndef TARGET_UNIX

// This function will capture the watson buckets for the current exception object that is:
//
// 1) Non-preallocated
// 2) Already contains the IP for watson bucketing
BOOL SetupWatsonBucketsForNonPreallocatedExceptions(OBJECTREF oThrowable /* = NULL */)
{
#ifndef DACCESS_COMPILE

    // CoreCLR may have watson bucketing conditionally enabled.
    if (!IsWatsonEnabled())
    {
        return FALSE;
    }

    CONTRACTL
    {
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        NOTHROW;
        PRECONDITION(GetThreadNULLOk() != NULL);
    }
    CONTRACTL_END;

    // By default, assume we didnt get the buckets
    BOOL fSetupWatsonBuckets = FALSE;

    Thread * pThread = GetThread();

    struct
    {
        OBJECTREF oThrowable;
    } gc;
    gc.oThrowable = NULL;
    GCPROTECT_BEGIN(gc);

    // Get the throwable to be used
    gc.oThrowable = (oThrowable != NULL) ? oThrowable : pThread->GetThrowable();
    if (gc.oThrowable == NULL)
    {
        // If we have no throwable, then simply return back.
        //
        // We could be here because the VM may have raised an exception,
        // and not managed code, for its internal usage (e.g. TA to end the
        // threads when unloading an AppDomain). Thus, there would be no throwable
        // present since the exception has not been seen by the runtime's
        // personality routine.
        //
        // Hence, we have no work to do here.
        LOG((LF_EH, LL_INFO100, "SetupWatsonBucketsForNonPreallocatedExceptions - No throwable available.\n"));
        goto done;
    }

    // The exception object should be non-preallocated
    _ASSERTE(!CLRException::IsPreallocatedExceptionObject(gc.oThrowable));

    if (((EXCEPTIONREF)gc.oThrowable)->AreWatsonBucketsPresent() == FALSE)
    {
        // Attempt to capture the watson buckets since they are not present.
        UINT_PTR ip = ((EXCEPTIONREF)gc.oThrowable)->GetIPForWatsonBuckets();
        if (ip != NULL)
        {
            // Attempt to capture the buckets
            PTR_VOID pBuckets = GetBucketParametersForManagedException(ip, TypeOfReportedError::UnhandledException, pThread, &gc.oThrowable);
            if (pBuckets != NULL)
            {
                // Got the buckets - save them to the exception object
                fSetupWatsonBuckets = FALSE;
                EX_TRY
                {
                    fSetupWatsonBuckets = CopyWatsonBucketsToThrowable(pBuckets, gc.oThrowable);
                }
                EX_CATCH
                {
                    // OOM can bring us here
                    fSetupWatsonBuckets = FALSE;
                }
                EX_END_CATCH(SwallowAllExceptions);

                if (!fSetupWatsonBuckets)
                {
                    LOG((LF_EH, LL_INFO100, "SetupWatsonBucketsForNonPreallocatedExceptions - Unable to copy buckets to throwable likely due to OOM.\n"));
                }
                else
                {
                    // Clear the saved IP since we have captured the buckets
                    ((EXCEPTIONREF)gc.oThrowable)->SetIPForWatsonBuckets(NULL);
                    LOG((LF_EH, LL_INFO100, "SetupWatsonBucketsForNonPreallocatedExceptions - Buckets copied to throwable.\n"));
                }
                FreeBucketParametersForManagedException(pBuckets);
            }
            else
            {
                LOG((LF_EH, LL_INFO100, "SetupWatsonBucketsForNonPreallocatedExceptions - Unable to capture buckets from IP likely due to OOM.\n"));
            }
        }
        else
        {
            LOG((LF_EH, LL_INFO100, "SetupWatsonBucketsForNonPreallocatedExceptions - No IP available to capture buckets from.\n"));
        }
    }

done:;
    GCPROTECT_END();

    return fSetupWatsonBuckets;
#else // DACCESS_COMPILE
    return FALSE;
#endif // !DACCESS_COMPILE
}

// When exceptions are escaping out of various transition boundaries,
// we will need to capture bucket details for the original exception
// before the exception goes across the boundary to the caller.
//
// Examples of such boundaries include:
//
// 1) AppDomain transition boundaries (these are physical transition boundaries)
// 2) Dynamic method invocation in Reflection (these are logical transition boundaries).
//
// This function will capture the bucketing details in the UE tracker so that
// they can be used once we cross over.
BOOL SetupWatsonBucketsForEscapingPreallocatedExceptions()
{
#ifndef DACCESS_COMPILE

    // CoreCLR may have watson bucketing conditionally enabled.
    if (!IsWatsonEnabled())
    {
        return FALSE;
    }

    CONTRACTL
    {
        GC_TRIGGERS;
        MODE_ANY;
        NOTHROW;
        PRECONDITION(GetThreadNULLOk() != NULL);
    }
    CONTRACTL_END;

    // By default, assume we didnt get the buckets
    BOOL fSetupWatsonBuckets = FALSE;
    PTR_EHWatsonBucketTracker pUEWatsonBucketTracker;

    Thread * pThread = GetThread();

    // If the exception going unhandled is preallocated, then capture the Watson buckets in the UE Watson
    // bucket tracker provided its not already populated.
    //
    // Switch to COOP mode
    GCX_COOP();

    struct
    {
        OBJECTREF oThrowable;
    } gc;
    gc.oThrowable = NULL;
    GCPROTECT_BEGIN(gc);

    // Get the throwable corresponding to the escaping exception
    gc.oThrowable = pThread->GetThrowable();
    if (gc.oThrowable == NULL)
    {
        // If we have no throwable, then simply return back.
        //
        // We could be here because the VM may have raised an exception,
        // and not managed code, for its internal usage (e.g. TA to end the
        // threads when unloading an AppDomain). Thus, there would be no throwable
        // present since the exception has not been seen by the runtime's
        // personality routine.
        //
        // Hence, we have no work to do here.
        LOG((LF_EH, LL_INFO100, "SetupWatsonBucketsForEscapingPreallocatedExceptions - No throwable available.\n"));
        goto done;
    }

    // Is the exception preallocated? We are not going to process non-preallocated exception objects since
    // they already have the watson buckets in them.
    //
    // We skip thread abort as well since we track them in the UE watson bucket tracker at
    // throw time itself.
    if (!((CLRException::IsPreallocatedExceptionObject(gc.oThrowable)) &&
        !IsThrowableThreadAbortException(gc.oThrowable)))
    {
        // Its either not preallocated or a thread abort exception,
        // neither of which we need to process.
        goto done;
    }

    // The UE watson bucket tracker could be non-empty if there were earlier transitions
    // on the threads stack before the exception got raised.
    pUEWatsonBucketTracker = pThread->GetExceptionState()->GetUEWatsonBucketTracker();
    _ASSERTE(pUEWatsonBucketTracker != NULL);

    // Proceed to capture bucketing details only if the UE watson bucket tracker is empty.
    if((pUEWatsonBucketTracker->RetrieveWatsonBucketIp() == NULL) && (pUEWatsonBucketTracker->RetrieveWatsonBuckets() == NULL))
    {
        // Get the Watson Bucket tracker for this preallocated exception
        PTR_EHWatsonBucketTracker pCurWatsonBucketTracker = GetWatsonBucketTrackerForPreallocatedException(gc.oThrowable, FALSE);

        if (pCurWatsonBucketTracker != NULL)
        {
            // If the tracker exists, we must have the throw site IP
            _ASSERTE(pCurWatsonBucketTracker->RetrieveWatsonBucketIp() != NULL);

            // Init the UE Watson bucket tracker
            pUEWatsonBucketTracker->ClearWatsonBucketDetails();

            // Copy the Bucket details to the UE watson bucket tracker
            pUEWatsonBucketTracker->CopyEHWatsonBucketTracker(*(pCurWatsonBucketTracker));

            // If the buckets dont exist, capture them now
            if (pUEWatsonBucketTracker->RetrieveWatsonBuckets() == NULL)
            {
                pUEWatsonBucketTracker->CaptureUnhandledInfoForWatson(TypeOfReportedError::UnhandledException, pThread, &gc.oThrowable);
            }

            // If the IP was in managed code, we will have the buckets.
            if(pUEWatsonBucketTracker->RetrieveWatsonBuckets() != NULL)
            {
                fSetupWatsonBuckets = TRUE;
                LOG((LF_EH, LL_INFO100, "SetupWatsonBucketsForEscapingPreallocatedExceptions - Captured watson buckets for preallocated exception at transition.\n"));
            }
            else
            {
                // IP was likely in native code - hence, watson helper functions couldnt get us the buckets
                LOG((LF_EH, LL_INFO100, "SetupWatsonBucketsForEscapingPreallocatedExceptions - Watson buckets not found for IP. IP likely in native code.\n"));

                // Clear the UE tracker
                pUEWatsonBucketTracker->ClearWatsonBucketDetails();
            }
        }
        else
        {
            LOG((LF_EH, LL_INFO100, "SetupWatsonBucketsForEscapingPreallocatedExceptions - Watson bucket tracker for preallocated exception not found. Exception likely thrown in native code.\n"));
        }
    }

done:;
    GCPROTECT_END();

    return fSetupWatsonBuckets;
#else // DACCESS_COMPILE
    return FALSE;
#endif // !DACCESS_COMPILE
}

// This function is invoked from the UEF worker to setup the watson buckets
// for the exception going unhandled, if details are available. See
// implementation below for specifics.
void SetupWatsonBucketsForUEF(BOOL fUseLastThrownObject)
{
#ifndef DACCESS_COMPILE

    // CoreCLR may have watson bucketing conditionally enabled.
    if (!IsWatsonEnabled())
    {
        return;
    }

    CONTRACTL
    {
        GC_TRIGGERS;
        MODE_ANY;
        NOTHROW;
        PRECONDITION(GetThreadNULLOk() != NULL);
    }
    CONTRACTL_END;

    Thread *pThread = GetThread();

    PTR_EHWatsonBucketTracker pCurWatsonBucketTracker = NULL;
    ThreadExceptionState *pExState = pThread->GetExceptionState();
    _ASSERTE(pExState != NULL);

    // If the exception tracker exists, then copy the bucketing details
    // from it to the UE Watson Bucket tracker.
    //
    // On 64bit, the EH system allocates the EHTracker only in the case of an exception.
    // Thus, assume a reverse pinvoke thread transitions to managed code from native,
    // does some work in managed and returns back to native code.
    //
    // In the native code, it has an exception that goes unhandled and the OS
    // ends up invoking our UEF, and thus, we land up here.
    //
    // In such a case, on 64bit, we wont have an exception tracker since there
    // was no managed exception active. On 32bit, we will have a tracker
    // but there wont be an IP corresponding to the throw site since exception
    // was raised in native code.
    //
    // But if the tracker exists, simply copy the bucket details to the UE Watson Bucket
    // tracker for use by the "WatsonLastChance" path.
    BOOL fDoWeHaveWatsonBuckets = FALSE;
    if (pExState->GetCurrentExceptionTracker() != NULL)
    {
        // Check the exception state if we have Watson bucket details
        fDoWeHaveWatsonBuckets = pExState->GetFlags()->GotWatsonBucketDetails();
    }

    // Switch to COOP mode before working with the throwable
    GCX_COOP();

    // Get the throwable we are going to work with
    struct
    {
        OBJECTREF oThrowable;
        U1ARRAYREF oBuckets;
    } gc;
    gc.oThrowable = NULL;
    gc.oBuckets = NULL;
    GCPROTECT_BEGIN(gc);

    gc.oThrowable = fUseLastThrownObject ? pThread->LastThrownObject() : pThread->GetThrowable();
    BOOL fThrowableExists = (gc.oThrowable != NULL);
    BOOL fIsThrowablePreallocated = !fThrowableExists ? FALSE : CLRException::IsPreallocatedExceptionObject(gc.oThrowable);

    if ((!fDoWeHaveWatsonBuckets) && fThrowableExists)
    {
        // Check the throwable if it has buckets - this could be the scenario
        // of native code calling into a non-default domain and thus, have an AD
        // transition in between that could reraise the exception but that would
        // never be seen by our exception handler. Thus, there wont be any tracker
        // or tracker state.
        //
        // Invocation of entry point on WLC via reverse pinvoke is an example.
        if (!fIsThrowablePreallocated)
        {
            fDoWeHaveWatsonBuckets = ((EXCEPTIONREF)gc.oThrowable)->AreWatsonBucketsPresent();
            if (!fDoWeHaveWatsonBuckets)
            {
                // If buckets are not present, then we may have IP to capture the buckets from.
                fDoWeHaveWatsonBuckets = ((EXCEPTIONREF)gc.oThrowable)->IsIPForWatsonBucketsPresent();
            }
        }
        else
        {
            // Get the watson bucket tracker for the preallocated exception
            PTR_EHWatsonBucketTracker pCurWBTracker = GetWatsonBucketTrackerForPreallocatedException(gc.oThrowable, FALSE);

            // We would have buckets if we have the IP
            if (pCurWBTracker && (pCurWBTracker->RetrieveWatsonBucketIp() != NULL))
            {
                fDoWeHaveWatsonBuckets = TRUE;
            }
        }
    }

    if (fDoWeHaveWatsonBuckets)
    {
        // Get the UE Watson bucket tracker
        PTR_EHWatsonBucketTracker pUEWatsonBucketTracker = pExState->GetUEWatsonBucketTracker();

        // Clear any existing information
        pUEWatsonBucketTracker->ClearWatsonBucketDetails();

        if (fIsThrowablePreallocated)
        {
            // Get the watson bucket tracker for the preallocated exception
            PTR_EHWatsonBucketTracker pCurWBTracker = GetWatsonBucketTrackerForPreallocatedException(gc.oThrowable, FALSE);

            if (pCurWBTracker != NULL)
            {
                // We should be having an IP for this exception at this point
                _ASSERTE(pCurWBTracker->RetrieveWatsonBucketIp() != NULL);

                // Copy the existing bucketing details to the UE tracker
                pUEWatsonBucketTracker->CopyEHWatsonBucketTracker(*(pCurWBTracker));

                // Get the buckets if we dont already have them since we
                // dont want to overwrite existing bucket information (e.g.
                // from an AD transition)
                if (pUEWatsonBucketTracker->RetrieveWatsonBuckets() == NULL)
                {
                    pUEWatsonBucketTracker->CaptureUnhandledInfoForWatson(TypeOfReportedError::UnhandledException, pThread, &gc.oThrowable);
                    if (pUEWatsonBucketTracker->RetrieveWatsonBuckets() != NULL)
                    {
                        LOG((LF_EH, LL_INFO100, "SetupWatsonBucketsForUEF: Collected watson bucket information for preallocated exception\n"));
                    }
                    else
                    {
                        // If we are here, then one of the following could have happened:
                        //
                        // 1) pCurWBTracker had buckets but we couldnt copy them over to pUEWatsonBucketTracker due to OOM, or
                        // 2) pCurWBTracker's IP was in native code; thus pUEWatsonBucketTracker->CaptureUnhandledInfoForWatson()
                        //    couldnt get us the watson buckets.
                        LOG((LF_EH, LL_INFO100, "SetupWatsonBucketsForUEF: Unable to collect watson bucket information for preallocated exception due to OOM or IP being in native code.\n"));
                    }
                }
            }
            else
            {
                // We likely had an OOM earlier (while copying the bucket information) if we are here
                LOG((LF_EH, LL_INFO100, "SetupWatsonBucketsForUEF: Watson bucket tracker for preallocated exception not found.\n"));
            }
        }
        else
        {
            // Throwable is not preallocated - get the bucket details from it for use by Watson
            _ASSERTE_MSG(((EXCEPTIONREF)gc.oThrowable)->AreWatsonBucketsPresent() ||
                        ((EXCEPTIONREF)gc.oThrowable)->IsIPForWatsonBucketsPresent(),
                        "How come we dont have watson buckets (or IP) for a non-preallocated exception in the UEF?");

            if ((((EXCEPTIONREF)gc.oThrowable)->AreWatsonBucketsPresent() == FALSE) &&
                ((EXCEPTIONREF)gc.oThrowable)->IsIPForWatsonBucketsPresent())
            {
                // Capture the buckets using the IP we have.
                SetupWatsonBucketsForNonPreallocatedExceptions(gc.oThrowable);
            }

            gc.oBuckets = ((EXCEPTIONREF)gc.oThrowable)->GetWatsonBucketReference();
            if (gc.oBuckets != NULL)
            {
                pUEWatsonBucketTracker->CopyBuckets(gc.oBuckets);
            }

            if (pUEWatsonBucketTracker->RetrieveWatsonBuckets() == NULL)
            {
                LOG((LF_EH, LL_INFO100, "SetupWatsonBucketsForUEF: Unable to copy watson buckets from regular exception throwable (%p), likely due to OOM.\n",
                                    OBJECTREFToObject(gc.oThrowable)));
            }
        }
    }
    else
    {
        // We dont have the watson buckets; exception was in native code that we dont care about
        LOG((LF_EH, LL_INFO100, "SetupWatsonBucketsForUEF: We dont have watson buckets - likely an exception in native code.\n"));
    }

    GCPROTECT_END();
#endif // !DACCESS_COMPILE
}

// Given a throwable, this function will return a BOOL indicating
// if it corresponds to any of the following thread abort exception
// objects:
//
// 1) Regular allocated ThreadAbortException
// 2) Preallocated ThreadAbortException
// 3) Preallocated RudeThreadAbortException
BOOL IsThrowableThreadAbortException(OBJECTREF oThrowable)
{
#ifndef DACCESS_COMPILE
    CONTRACTL
    {
        GC_NOTRIGGER;
        MODE_COOPERATIVE;
        NOTHROW;
        PRECONDITION(GetThreadNULLOk() != NULL);
        PRECONDITION(oThrowable != NULL);
    }
    CONTRACTL_END;

    BOOL fIsTAE = FALSE;

    struct
    {
        OBJECTREF oThrowable;
    } gc;
    gc.oThrowable = oThrowable;
    GCPROTECT_BEGIN(gc);

    fIsTAE = IsExceptionOfType(kThreadAbortException, &gc.oThrowable);

    GCPROTECT_END();

    return fIsTAE;

#else // DACCESS_COMPILE
    return FALSE;
#endif // !DACCESS_COMPILE
}

// Given a throwable, this function will walk the exception tracker
// list to return the tracker, if available, corresponding to the preallocated
// exception object.
//
// The caller can also specify the starting EHTracker to walk the list from.
// If not specified, this will default to the current exception tracker active
// on the thread.
#if defined(FEATURE_EH_FUNCLETS)
PTR_ExceptionTrackerBase GetEHTrackerForPreallocatedException(OBJECTREF oPreAllocThrowable,
                                                          PTR_ExceptionTrackerBase pStartingEHTracker)
#elif TARGET_X86
PTR_ExInfo GetEHTrackerForPreallocatedException(OBJECTREF oPreAllocThrowable,
                                                PTR_ExInfo pStartingEHTracker)
#else
#error Unsupported platform
#endif
{
    CONTRACTL
    {
        GC_NOTRIGGER;
        MODE_COOPERATIVE;
        NOTHROW;
        PRECONDITION(GetThreadNULLOk() != NULL);
        PRECONDITION(oPreAllocThrowable != NULL);
        PRECONDITION(CLRException::IsPreallocatedExceptionObject(oPreAllocThrowable));
        PRECONDITION(IsWatsonEnabled());
    }
    CONTRACTL_END;

    // Get the reference to the current exception tracker
#if defined(FEATURE_EH_FUNCLETS)
    PTR_ExceptionTrackerBase pEHTracker = (pStartingEHTracker != NULL) ? pStartingEHTracker : GetThread()->GetExceptionState()->GetCurrentExceptionTracker();
#elif TARGET_X86
    PTR_ExInfo pEHTracker = (pStartingEHTracker != NULL) ? pStartingEHTracker : GetThread()->GetExceptionState()->GetCurrentExceptionTracker();
#else // !(HOST_64BIT || TARGET_X86)
#error Unsupported platform
#endif // HOST_64BIT

    BOOL fFoundTracker = FALSE;

    struct
    {
        OBJECTREF oPreAllocThrowable;
    } gc;
    gc.oPreAllocThrowable = oPreAllocThrowable;
    GCPROTECT_BEGIN(gc);

    // Start walking the list to find the tracker corresponding
    // to the preallocated exception object.
    while (pEHTracker != NULL)
    {
        if (pEHTracker->GetThrowable() == gc.oPreAllocThrowable)
        {
            // found the tracker - break out.
            fFoundTracker = TRUE;
            break;
        }

        // move to the previous tracker...
        pEHTracker = pEHTracker->GetPreviousExceptionTracker();
    }

    GCPROTECT_END();

    return fFoundTracker ? pEHTracker : NULL;
}

// This function will return the pointer to EHWatsonBucketTracker corresponding to the
// preallocated exception object. If none is found, it will return NULL.
PTR_EHWatsonBucketTracker GetWatsonBucketTrackerForPreallocatedException(OBJECTREF oPreAllocThrowable,
                                                                         BOOL fCaptureBucketsIfNotPresent,
                                                                         BOOL fStartSearchFromPreviousTracker /*= FALSE*/)
{
#ifndef DACCESS_COMPILE
    CONTRACTL
    {
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        NOTHROW;
        PRECONDITION(GetThreadNULLOk() != NULL);
        PRECONDITION(oPreAllocThrowable != NULL);
        PRECONDITION(CLRException::IsPreallocatedExceptionObject(oPreAllocThrowable));
        PRECONDITION(IsWatsonEnabled());
    }
    CONTRACTL_END;

    PTR_EHWatsonBucketTracker pWBTracker = NULL;

    struct
    {
        OBJECTREF oPreAllocThrowable;
    } gc;
    gc.oPreAllocThrowable = oPreAllocThrowable;
    GCPROTECT_BEGIN(gc);

    // Before doing anything, check if this is a thread abort exception. If it is,
    // then simply return the reference to the UE watson bucket tracker since it
    // tracks the bucketing details for all types of TAE.
    if (IsThrowableThreadAbortException(gc.oPreAllocThrowable))
    {
        pWBTracker = GetThread()->GetExceptionState()->GetUEWatsonBucketTracker();
        LOG((LF_EH, LL_INFO100, "GetWatsonBucketTrackerForPreallocatedException - Setting UE Watson Bucket Tracker to be returned for preallocated ThreadAbortException.\n"));
        goto doValidation;
    }

    {
        // Find the reference to the exception tracker corresponding to the preallocated exception,
        // starting the search from the current exception tracker (2nd arg of NULL specifies that).
 #if defined(FEATURE_EH_FUNCLETS)
        PTR_ExceptionTrackerBase pEHTracker = NULL;
        PTR_ExceptionTrackerBase pPreviousEHTracker = NULL;

#elif TARGET_X86
        PTR_ExInfo pEHTracker = NULL;
        PTR_ExInfo pPreviousEHTracker = NULL;
#else // !(HOST_64BIT || TARGET_X86)
#error Unsupported platform
#endif // HOST_64BIT

        if (fStartSearchFromPreviousTracker)
        {
            // Get the exception tracker previous to the current one
            pPreviousEHTracker = GetThread()->GetExceptionState()->GetCurrentExceptionTracker()->GetPreviousExceptionTracker();

            // If there is no previous tracker to start from, then simply abort the search attempt.
            // If we couldnt find the exception tracker, then buckets are not available
            if (pPreviousEHTracker == NULL)
            {
                LOG((LF_EH, LL_INFO100, "GetWatsonBucketTrackerForPreallocatedException - Couldnt find the previous EHTracker to start the search from.\n"));
                pWBTracker = NULL;
                goto done;
            }
        }

        pEHTracker = GetEHTrackerForPreallocatedException(gc.oPreAllocThrowable, pPreviousEHTracker);

        // If we couldnt find the exception tracker, then buckets are not available
        if (pEHTracker == NULL)
        {
            LOG((LF_EH, LL_INFO100, "GetWatsonBucketTrackerForPreallocatedException - Couldnt find EHTracker for preallocated exception object.\n"));
            pWBTracker = NULL;
            goto done;
        }

        // Get the Watson Bucket Tracker from the exception tracker
        pWBTracker = pEHTracker->GetWatsonBucketTracker();
    }
doValidation:
    _ASSERTE(pWBTracker != NULL);

    // Incase of an OOM, we may not have an IP in the Watson bucket tracker. A scenario
    // would be default domain calling to AD 2 that calls into AD 3.
    //
    // AD 3 has an exception that is represented by a preallocated exception object. The
    // exception goes unhandled and reaches AD2/AD3 transition boundary. The bucketing details
    // from AD3 are copied to UETracker and once the exception is reraised in AD2, we will
    // enter SetupInitialThrowBucketingDetails to copy the bucketing details to the active
    // exception tracker.
    //
    // This copy operation could fail due to OOM and the active exception tracker in AD 2,
    // for the preallocated exception object, will not have any bucketing details. If the
    // exception remains unhandled in AD 2, then just before it reaches DefDomain/AD2 boundary,
    // we will attempt to capture the bucketing details in AppDomainTransitionExceptionFilter,
    // that will bring us here.
    //
    // In such a case, the active exception tracker will not have any bucket details for the
    // preallocated exception. In such a case, if the IP does not exist, we will return NULL
    // indicating that we couldnt find the Watson bucket tracker, since returning a tracker
    // that does not have any bucketing details will be of no use to the caller.
    if (pWBTracker->RetrieveWatsonBucketIp() != NULL)
    {
        // Check if the buckets exist or not..
        PTR_VOID pBuckets = pWBTracker->RetrieveWatsonBuckets();

        // If they dont exist and we have been asked to collect them,
        // then do so.
        if (pBuckets == NULL)
        {
            if (fCaptureBucketsIfNotPresent)
            {
                pWBTracker->CaptureUnhandledInfoForWatson(TypeOfReportedError::UnhandledException, GetThreadNULLOk(), &gc.oPreAllocThrowable);

                // Check if we have the buckets now
                if (pWBTracker->RetrieveWatsonBuckets() != NULL)
                {
                    LOG((LF_EH, LL_INFO100, "GetWatsonBucketTrackerForPreallocatedException - Captured watson buckets for preallocated exception object.\n"));
                }
                else
                {
                    LOG((LF_EH, LL_INFO100, "GetWatsonBucketTrackerForPreallocatedException - Unable to capture watson buckets for preallocated exception object due to OOM.\n"));
                }
            }
            else
            {
                LOG((LF_EH, LL_INFO100, "GetWatsonBucketTrackerForPreallocatedException - Found IP but no buckets for preallocated exception object.\n"));
            }
        }
        else
        {
            LOG((LF_EH, LL_INFO100, "GetWatsonBucketTrackerForPreallocatedException - Buckets already exist for preallocated exception object.\n"));
        }
    }
    else
    {
        LOG((LF_EH, LL_INFO100, "GetWatsonBucketTrackerForPreallocatedException - Returning NULL EHWatsonBucketTracker since bucketing IP does not exist. This is likely due to an earlier OOM.\n"));
        pWBTracker = NULL;
    }

done:;

    GCPROTECT_END();

    // Return the Watson bucket tracker
    return pWBTracker;
#else // DACCESS_COMPILE
    return NULL;
#endif // !DACCESS_COMPILE
}

// Given an exception object, this function will attempt to look up
// the watson buckets for it and set them up against the thread
// for use by FailFast mechanism.
// Return TRUE when it succeeds or Waston is disabled on CoreCLR
// Return FALSE when refException neither has buckets nor has inner exception
BOOL SetupWatsonBucketsForFailFast(EXCEPTIONREF refException)
{
    BOOL fResult = TRUE;

#ifndef DACCESS_COMPILE
    // On CoreCLR, Watson may not be enabled. Thus, we should
    // skip this.
    if (!IsWatsonEnabled())
    {
        return fResult;
    }

    CONTRACTL
    {
        GC_TRIGGERS;
        MODE_ANY;
        NOTHROW;
        PRECONDITION(GetThreadNULLOk() != NULL);
        PRECONDITION(refException != NULL);
        PRECONDITION(IsWatsonEnabled());
    }
    CONTRACTL_END;

    // Switch to COOP mode
    GCX_COOP();

    struct
    {
        OBJECTREF refException;
        OBJECTREF oInnerMostExceptionThrowable;
        U1ARRAYREF oBuckets;
    } gc;
    gc.refException = refException;
    gc.oInnerMostExceptionThrowable = NULL;
    gc.oBuckets = NULL;
    GCPROTECT_BEGIN(gc);

    Thread *pThread = GetThread();

    // If we dont already have the bucketing details for the exception
    // being thrown, then get them.
    ThreadExceptionState *pExState = pThread->GetExceptionState();

    // Check if the exception object is preallocated or not
    BOOL fIsPreallocatedException = CLRException::IsPreallocatedExceptionObject(gc.refException);

    // Get the WatsonBucketTracker where bucketing details will be copied to
    PTR_EHWatsonBucketTracker pUEWatsonBucketTracker = pExState->GetUEWatsonBucketTracker();

    // Check if this is a thread abort exception of any kind.
    // See IsThrowableThreadAbortException implementation for details.
    BOOL fIsThreadAbortException = IsThrowableThreadAbortException(gc.refException);

    if (fIsPreallocatedException)
    {
        // If the exception being used to FailFast is preallocated,
        // then it cannot have any inner exception. Thus, try to
        // find the watson bucket tracker corresponding to this exception.
        //
        // Also, capture the buckets if we dont have them already.
        PTR_EHWatsonBucketTracker pTargetWatsonBucketTracker = GetWatsonBucketTrackerForPreallocatedException(gc.refException, TRUE);
        if ((pTargetWatsonBucketTracker != NULL) && (!fIsThreadAbortException))
        {
            // Buckets are not captured proactively for preallocated exception objects. We only
            // save the IP in the watson bucket tracker (see SetupInitialThrowBucketingDetails for
            // details).
            //
            // Thus, if, say in DefDomain, a preallocated exception is thrown and we enter
            // the catch block and invoke the FailFast API with the reference to the preallocated
            // exception object, we will have the IP but not the buckets. In such a case,
            // capture the buckets before proceeding ahead.
            if (pTargetWatsonBucketTracker->RetrieveWatsonBuckets() == NULL)
            {
                LOG((LF_EH, LL_INFO100, "SetupWatsonBucketsForFailFast - Collecting watson bucket details for preallocated exception.\n"));
                pTargetWatsonBucketTracker->CaptureUnhandledInfoForWatson(TypeOfReportedError::UnhandledException, pThread, &gc.refException);
            }

            // Copy the buckets to the UE tracker
            pUEWatsonBucketTracker->ClearWatsonBucketDetails();
            pUEWatsonBucketTracker->CopyEHWatsonBucketTracker(*pTargetWatsonBucketTracker);
            if (pUEWatsonBucketTracker->RetrieveWatsonBuckets() != NULL)
            {
                LOG((LF_EH, LL_INFO100, "SetupWatsonBucketsForFailFast - Collected watson bucket details for preallocated exception in UE tracker.\n"));
            }
            else
            {
                // If we are here, then the copy operation above had an OOM, resulting
                // in no buckets for us.
                LOG((LF_EH, LL_INFO100, "SetupWatsonBucketsForFailFast - Unable to collect watson bucket details for preallocated exception due to out of memory.\n"));

                // Make sure the tracker is clean.
                pUEWatsonBucketTracker->ClearWatsonBucketDetails();
            }
        }
        else
        {
            // For TAE, UE watson bucket tracker is the one that tracks the buckets. It *may*
            // not have the bucket details if FailFast is being invoked from outside the
            // managed EH clauses. But if invoked from within the active EH clause for the exception,
            // UETracker will have the bucketing details (see SetupInitialThrowBucketingDetails for details).
            if (fIsThreadAbortException && (pUEWatsonBucketTracker->RetrieveWatsonBuckets() != NULL))
            {
                _ASSERTE(pTargetWatsonBucketTracker == pUEWatsonBucketTracker);
                LOG((LF_EH, LL_INFO100, "SetupWatsonBucketsForFailFast - UE tracker already watson bucket details for preallocated thread abort exception.\n"));
            }
            else
            {
                LOG((LF_EH, LL_INFO100, "SetupWatsonBucketsForFailFast - Unable to find bucket details for preallocated %s exception.\n",
                    fIsThreadAbortException?"rude/thread abort":""));

                // Make sure the tracker is clean.
                pUEWatsonBucketTracker->ClearWatsonBucketDetails();
            }
        }
    }
    else
    {
        // Since the exception object is not preallocated, start by assuming
        // that we dont need to check it for watson buckets
        BOOL fCheckThrowableForWatsonBuckets = FALSE;

        // Get the innermost exception object (if any)
        gc.oInnerMostExceptionThrowable = ((EXCEPTIONREF)gc.refException)->GetBaseException();
        if (gc.oInnerMostExceptionThrowable != NULL)
        {
            if (CLRException::IsPreallocatedExceptionObject(gc.oInnerMostExceptionThrowable))
            {
                // If the inner most exception being used to FailFast is preallocated,
                // try to find the watson bucket tracker corresponding to it.
                //
                // Also, capture the buckets if we dont have them already.
                PTR_EHWatsonBucketTracker pTargetWatsonBucketTracker =
                    GetWatsonBucketTrackerForPreallocatedException(gc.oInnerMostExceptionThrowable, TRUE);

                if (pTargetWatsonBucketTracker != NULL)
                {
                    if (pTargetWatsonBucketTracker->RetrieveWatsonBuckets() == NULL)
                    {
                        LOG((LF_EH, LL_INFO1000, "SetupWatsonBucketsForFailFast - Capturing Watson bucket details for preallocated inner exception.\n"));
                        pTargetWatsonBucketTracker->CaptureUnhandledInfoForWatson(TypeOfReportedError::UnhandledException, pThread, &gc.oInnerMostExceptionThrowable);
                    }

                    // Copy the details to the UE tracker
                    pUEWatsonBucketTracker->ClearWatsonBucketDetails();
                    pUEWatsonBucketTracker->CopyEHWatsonBucketTracker(*pTargetWatsonBucketTracker);
                    if (pUEWatsonBucketTracker->RetrieveWatsonBuckets() != NULL)
                    {
                        LOG((LF_EH, LL_INFO1000, "SetupWatsonBucketsForFailFast - Watson bucket details collected for preallocated inner exception.\n"));
                    }
                    else
                    {
                        // If we are here, copy operation failed likely due to OOM
                        LOG((LF_EH, LL_INFO1000, "SetupWatsonBucketsForFailFast - Unable to copy watson bucket details for preallocated inner exception.\n"));

                        // Keep the UETracker clean
                        pUEWatsonBucketTracker->ClearWatsonBucketDetails();
                    }
                }
                else
                {
                    LOG((LF_EH, LL_INFO1000, "SetupWatsonBucketsForFailFast - Unable to find bucket details for preallocated inner exception.\n"));

                    // Keep the UETracker clean
                    pUEWatsonBucketTracker->ClearWatsonBucketDetails();

                    // Since we couldnt find the watson bucket tracker for the inner most exception,
                    // try to look for the buckets in the throwable.
                    fCheckThrowableForWatsonBuckets = TRUE;
                }
            }
            else
            {
                // Inner most exception is not preallocated.
                //
                // If it has the IP but not the buckets, then capture them now.
                if ((((EXCEPTIONREF)gc.oInnerMostExceptionThrowable)->AreWatsonBucketsPresent() == FALSE) &&
                    (((EXCEPTIONREF)gc.oInnerMostExceptionThrowable)->IsIPForWatsonBucketsPresent()))
                {
                    SetupWatsonBucketsForNonPreallocatedExceptions(gc.oInnerMostExceptionThrowable);
                }

                // If it has the buckets, copy them over to the current Watson bucket tracker
                gc.oBuckets = ((EXCEPTIONREF)gc.oInnerMostExceptionThrowable)->GetWatsonBucketReference();
                if (gc.oBuckets != NULL)
                {
                    pUEWatsonBucketTracker->ClearWatsonBucketDetails();
                    pUEWatsonBucketTracker->CopyBuckets(gc.oBuckets);
                    if (pUEWatsonBucketTracker->RetrieveWatsonBuckets() != NULL)
                    {
                        LOG((LF_EH, LL_INFO1000, "SetupWatsonBucketsForFailFast - Got watson buckets from regular innermost exception.\n"));
                    }
                    else
                    {
                        // Copy operation can fail due to OOM
                        LOG((LF_EH, LL_INFO1000, "SetupWatsonBucketsForFailFast - Unable to copy watson buckets from regular innermost exception, likely due to OOM.\n"));
                    }
                }
                else
                {
                    // Since the inner most exception didnt have the buckets,
                    // try to look for them in the throwable
                    fCheckThrowableForWatsonBuckets = TRUE;
                    LOG((LF_EH, LL_INFO1000, "SetupWatsonBucketsForFailFast - Neither exception object nor its inner exception has watson buckets.\n"));
                }
            }
        }
        else
        {
            // There is no innermost exception - try to look for buckets
            // in the throwable
            fCheckThrowableForWatsonBuckets = TRUE;
            LOG((LF_EH, LL_INFO1000, "SetupWatsonBucketsForFailFast - Innermost exception does not exist\n"));
        }

        if (fCheckThrowableForWatsonBuckets)
        {
            // Since we have not found buckets anywhere, try to look for them
            // in the throwable.
            if ((((EXCEPTIONREF)gc.refException)->AreWatsonBucketsPresent() == FALSE) &&
                (((EXCEPTIONREF)gc.refException)->IsIPForWatsonBucketsPresent()))
            {
                // Capture the buckets from the IP.
                SetupWatsonBucketsForNonPreallocatedExceptions(gc.refException);
            }

            gc.oBuckets = ((EXCEPTIONREF)gc.refException)->GetWatsonBucketReference();
            if (gc.oBuckets != NULL)
            {
                // Copy the buckets to the current watson bucket tracker
                pUEWatsonBucketTracker->ClearWatsonBucketDetails();
                pUEWatsonBucketTracker->CopyBuckets(gc.oBuckets);
                if (pUEWatsonBucketTracker->RetrieveWatsonBuckets() != NULL)
                {
                    LOG((LF_EH, LL_INFO1000, "SetupWatsonBucketsForFailFast - Watson buckets copied from the exception object.\n"));
                }
                else
                {
                    LOG((LF_EH, LL_INFO1000, "SetupWatsonBucketsForFailFast - Unable to copy Watson buckets copied from the exception object, likely due to OOM.\n"));
                }
            }
            else
            {
                fResult = FALSE;
                LOG((LF_EH, LL_INFO1000, "SetupWatsonBucketsForFailFast - Exception object neither has buckets nor has inner exception.\n"));
            }
        }
    }

    GCPROTECT_END();

#endif // !DACCESS_COMPILE

    return fResult;
}

// This function will setup the bucketing details in the exception
// tracker or the throwable, if they are not already setup.
//
// This is called when an exception is thrown (or raised):
//
// 1) from outside the confines of managed EH clauses, OR
// 2) from within the confines of managed EH clauses but the
//    exception does not have bucketing details with it, OR
// 3) When an exception is reraised at AD transition boundary
//    after it has been marshalled over to the returning AD.
void SetupInitialThrowBucketDetails(UINT_PTR adjustedIp)
{
#ifndef DACCESS_COMPILE

    // On CoreCLR, Watson may not be enabled. Thus, we should
    // skip this.
    if (!IsWatsonEnabled())
    {
        return;
    }

    CONTRACTL
    {
        GC_TRIGGERS;
        MODE_ANY;
        NOTHROW;
        PRECONDITION(GetThreadNULLOk() != NULL);
        PRECONDITION(!(GetThread()->GetExceptionState()->GetFlags()->GotWatsonBucketDetails()));
        PRECONDITION(adjustedIp != NULL);
        PRECONDITION(IsWatsonEnabled());
    }
    CONTRACTL_END;

    Thread *pThread = GetThread();

    // If we dont already have the bucketing details for the exception
    // being thrown, then get them.
    ThreadExceptionState *pExState = pThread->GetExceptionState();

    _ASSERTE(pExState->GetCurrentExceptionTracker() != NULL);

    // Switch to COOP mode
    GCX_COOP();

    // Get the throwable for the exception being thrown
    struct
    {
        OBJECTREF oCurrentThrowable;
        OBJECTREF oInnerMostExceptionThrowable;
        U1ARRAYREF refSourceWatsonBucketArray;
    } gc;
    gc.oCurrentThrowable = NULL;
    gc.oInnerMostExceptionThrowable = NULL;
    gc.refSourceWatsonBucketArray = NULL;

    GCPROTECT_BEGIN(gc);

    gc.oCurrentThrowable = pExState->GetThrowable();

    // Check if the exception object is preallocated or not
    BOOL fIsPreallocatedException = CLRException::IsPreallocatedExceptionObject(gc.oCurrentThrowable);

    // Get the WatsonBucketTracker for the current exception
    PTR_EHWatsonBucketTracker pWatsonBucketTracker;
    pWatsonBucketTracker = pExState->GetCurrentExceptionTracker()->GetWatsonBucketTracker();

    // Get the innermost exception object (if any)
    gc.oInnerMostExceptionThrowable = ((EXCEPTIONREF)gc.oCurrentThrowable)->GetBaseException();

    // By default, assume that no watson bucketing details are available and inner exception
    // is not preallocated
    BOOL fAreBucketingDetailsPresent = FALSE;
    BOOL fIsInnerExceptionPreallocated = FALSE;

    // Check if this is a thread abort exception of any kind. See IsThrowableThreadAbortException implementation for details.
    // We shouldnt use the thread state as well to determine if it is a TAE since, in cases like throwing a cached exception
    // as part of type initialization failure, we could throw a TAE but the thread will not be in abort state (which is expected).
    BOOL fIsThreadAbortException = IsThrowableThreadAbortException(gc.oCurrentThrowable);

    // If we are here, then this was a new exception raised
    // from outside the managed EH clauses (fault/finally/catch).
    //
    // The throwable *may* have the bucketing details already
    // if this exception was raised when it was crossing over
    // an AD transition boundary. Those are stored in UE watson bucket
    // tracker by AppDomainTransitionExceptionFilter.
    if (fIsPreallocatedException)
    {
        PTR_EHWatsonBucketTracker pUEWatsonBucketTracker = pExState->GetUEWatsonBucketTracker();
        fAreBucketingDetailsPresent = ((pUEWatsonBucketTracker->RetrieveWatsonBucketIp() != NULL) &&
                                       (pUEWatsonBucketTracker->RetrieveWatsonBuckets() != NULL));

        // If they are present, copy them over to the watson tracker for the exception
        // being processed.
        if (fAreBucketingDetailsPresent)
        {
#ifdef _DEBUG
            // Under OOM scenarios, its possible that when we are raising a threadabort,
            // the throwable may get converted to preallocated OOM object when RaiseTheExceptionInternalOnly
            // invokes Thread::SafeSetLastThrownObject. We check if this is the current case and use it in
            // our validation below.
            BOOL fIsPreallocatedOOMExceptionForTA = FALSE;
            if ((!fIsThreadAbortException) && pUEWatsonBucketTracker->CapturedForThreadAbort())
            {
                fIsPreallocatedOOMExceptionForTA = (gc.oCurrentThrowable == CLRException::GetPreallocatedOutOfMemoryException());
                if (fIsPreallocatedOOMExceptionForTA)
                {
                    LOG((LF_EH, LL_INFO100, "SetupInitialThrowBucketDetails - Got preallocated OOM throwable for buckets captured for thread abort.\n"));
                }
            }
#endif // _DEBUG
            // These should have been captured at AD transition OR
            // could be bucketing details of preallocated [rude] thread abort exception.
            _ASSERTE(pUEWatsonBucketTracker->CapturedAtADTransition() ||
                     ((fIsThreadAbortException || fIsPreallocatedOOMExceptionForTA) && pUEWatsonBucketTracker->CapturedForThreadAbort()));

            if (!fIsThreadAbortException)
            {
                // The watson bucket tracker for the exceptiong being raised should be empty at this point
                // since we are here because of a cross AD reraise of the original exception.
                _ASSERTE((pWatsonBucketTracker->RetrieveWatsonBucketIp() == NULL) && (pWatsonBucketTracker->RetrieveWatsonBuckets() == NULL));

                // Copy the buckets over to it
                pWatsonBucketTracker->CopyEHWatsonBucketTracker(*(pUEWatsonBucketTracker));
                if (pWatsonBucketTracker->RetrieveWatsonBuckets() == NULL)
                {
                    // If we dont have buckets after the copy operation, its due to us running out of
                    // memory.
                    LOG((LF_EH, LL_INFO100, "SetupInitialThrowBucketDetails - Unable to copy watson buckets from cross AD rethrow, likely due to out of memory.\n"));
                }
                else
                {
                    LOG((LF_EH, LL_INFO100, "SetupInitialThrowBucketDetails - Copied watson buckets from cross AD rethrow.\n"));
                }
            }
            else
            {
                // Thread abort watson bucket details are already present in the
                // UE watson bucket tracker.
                LOG((LF_EH, LL_INFO100, "SetupInitialThrowBucketDetails - Already have watson buckets for preallocated thread abort reraise.\n"));
            }
        }
        else if (fIsThreadAbortException)
        {
            // This is a preallocated thread abort exception.
            UINT_PTR ip = pUEWatsonBucketTracker->RetrieveWatsonBucketIp();
            if (ip != NULL)
            {
                // Since we have the IP, assert that this was the one setup
                // for ThreadAbort. This is for the reraise scenario where
                // the original exception was non-preallocated TA but the
                // reraise resulted in preallocated TA.
                //
                // In this case, we will update the ip to be used as the
                // one we have. The control flow below will automatically
                // endup using it.
                _ASSERTE(pUEWatsonBucketTracker->CapturedForThreadAbort());
                adjustedIp = ip;
                LOG((LF_EH, LL_INFO100, "SetupInitialThrowBucketDetails - Setting an existing IP (%p) to be used for capturing buckets for preallocated thread abort.\n", ip));
                goto phase1;
            }
        }

        if (!fAreBucketingDetailsPresent || !fIsThreadAbortException)
        {
            // Clear the UE Watson bucket tracker so that its usable
            // in future. We dont clear this for ThreadAbort since
            // the UE watson bucket tracker carries bucketing details
            // for the same, unless the UE tracker is not containing them
            // already.
            pUEWatsonBucketTracker->ClearWatsonBucketDetails();
        }
    }
    else
    {
        // The exception object is not preallocated
        fAreBucketingDetailsPresent = ((EXCEPTIONREF)gc.oCurrentThrowable)->AreWatsonBucketsPresent();
        if (!fAreBucketingDetailsPresent)
        {
            // If buckets are not present, check if the bucketing IP is present.
            fAreBucketingDetailsPresent = ((EXCEPTIONREF)gc.oCurrentThrowable)->IsIPForWatsonBucketsPresent();
        }

        // If throwable does not have buckets and this is a thread abort exception,
        // then this maybe a reraise of the original thread abort.
        //
        // We can also be here if an exception was caught at AppDomain transition and
        // in the returning domain, a non-preallocated TAE was raised. In such a case,
        // the UE tracker flags could indicate the exception is from AD transition.
        // This is similar to preallocated case above.
        //
        // Check the UE Watson bucket tracker if it has the buckets and if it does,
        // copy them over to the current throwable.
        if (!fAreBucketingDetailsPresent && fIsThreadAbortException)
        {
            PTR_EHWatsonBucketTracker pUEWatsonBucketTracker = pExState->GetUEWatsonBucketTracker();
            UINT_PTR ip = pUEWatsonBucketTracker->RetrieveWatsonBucketIp();
            if (ip != NULL)
            {
                // Confirm that we had the buckets captured for thread abort
                _ASSERTE(pUEWatsonBucketTracker->CapturedForThreadAbort() || pUEWatsonBucketTracker->CapturedAtADTransition());

                if (pUEWatsonBucketTracker->RetrieveWatsonBuckets() != NULL)
                {
                    // Copy the buckets to the current throwable - CopyWatsonBucketsToThrowable
                    // can throw in OOM. However, since the current function is called as part of
                    // setting up the stack trace, where we bail out incase of OOM, we will do
                    // no different here as well.
                    BOOL fCopiedBuckets = TRUE;
                    EX_TRY
                    {
                        CopyWatsonBucketsToThrowable(pUEWatsonBucketTracker->RetrieveWatsonBuckets());

                        // Technically this assert can fail, as another thread could clear the buckets after
                        // CopyWatsonBucketsToThrowable but before the assert runs, but it is very unlikely.
                        _ASSERTE(((EXCEPTIONREF)gc.oCurrentThrowable)->AreWatsonBucketsPresent());
                    }
                    EX_CATCH
                    {
                        fCopiedBuckets = FALSE;
                    }
                    EX_END_CATCH(SwallowAllExceptions);

                    if (fCopiedBuckets)
                    {
                        // Since the throwable has the buckets, set the flag that indicates so
                        fAreBucketingDetailsPresent = TRUE;
                        LOG((LF_EH, LL_INFO100, "SetupInitialThrowBucketDetails - Setup watson buckets for thread abort reraise.\n"));
                    }
                }
                else
                {
                    // Copy the faulting IP from the UE tracker to the exception object. This was setup in COMPlusCheckForAbort
                    // for non-preallocated exceptions.
                    ((EXCEPTIONREF)gc.oCurrentThrowable)->SetIPForWatsonBuckets(ip);
                    fAreBucketingDetailsPresent = TRUE;
                    LOG((LF_EH, LL_INFO100, "SetupInitialThrowBucketDetails - Setup watson bucket IP for thread abort reraise.\n"));
                }
            }
            else
            {
                // Clear the UE Watson bucket tracker so that its usable
                // in future.
                pUEWatsonBucketTracker->ClearWatsonBucketDetails();
                LOG((LF_EH, LL_INFO100, "SetupInitialThrowBucketDetails - Didnt find watson buckets for thread abort - likely being raised.\n"));
            }
        }
    }

phase1:
    UINT_PTR ipForWatsonBuckets = 0;
    gc.refSourceWatsonBucketArray = NULL;
    bool gotWatsonBucketDetails = true;

    if (fAreBucketingDetailsPresent)
    {
        // Since we already have the buckets, simply bail out
        LOG((LF_EH, LL_INFO100, "SetupInitialThrowBucketDetails - Already had watson ip/buckets.\n"));
        goto done;
    }

    // Check if an inner most exception exists and if it does, examine
    // it for watson bucketing details.
    if (gc.oInnerMostExceptionThrowable != NULL)
    {
        // Preallocated exception objects do not have inner exception objects.
        // Thus, if we are here, then the current throwable cannot be
        // a preallocated exception object.
        _ASSERTE(!fIsPreallocatedException);

        fIsInnerExceptionPreallocated = CLRException::IsPreallocatedExceptionObject(gc.oInnerMostExceptionThrowable);

        // If we are here, then this was a "throw" with inner exception
        // outside of any managed EH clauses.
        //
        // If the inner exception object is preallocated, then we will need to create the
        // watson buckets since we are outside the managed EH clauses with no exception tracking
        // information relating to the inner exception.
        //
        // But if the inner exception object was not preallocated, create new watson buckets
        // only if inner exception does not have them.
        if (fIsInnerExceptionPreallocated)
        {
            fAreBucketingDetailsPresent = FALSE;
        }
        else
        {
            ipForWatsonBuckets = ((EXCEPTIONREF)gc.oInnerMostExceptionThrowable)->GetIPForWatsonBuckets();
            gc.refSourceWatsonBucketArray = ((EXCEPTIONREF)gc.oInnerMostExceptionThrowable)->GetWatsonBucketReference();

            // Do we have either the IP for Watson buckets or the buckets themselves?
            fAreBucketingDetailsPresent = (gc.refSourceWatsonBucketArray != NULL ||
                                           ipForWatsonBuckets != 0);
        }
    }

    if (!fAreBucketingDetailsPresent)
    {
        // Collect the bucketing details since they are not already present
        pWatsonBucketTracker->SaveIpForWatsonBucket(adjustedIp);

        if (!fIsPreallocatedException || fIsThreadAbortException)
        {
            if (!fIsPreallocatedException)
            {
                // Save the IP for Watson bucketing in the exception object for non-preallocated exception
                // objects
                ((EXCEPTIONREF)gc.oCurrentThrowable)->SetIPForWatsonBuckets(adjustedIp);

                // Save the IP in the UE tracker as well for TAE if an abort is in progress
                // since when we attempt reraise, the exception object is not available. Otherwise,
                // treat the exception like a regular non-preallocated exception and not do anything else.
                if (fIsThreadAbortException && pThread->IsAbortInitiated())
                {
                    PTR_EHWatsonBucketTracker pUEWatsonBucketTracker = pExState->GetUEWatsonBucketTracker();

                    pUEWatsonBucketTracker->ClearWatsonBucketDetails();
                    pUEWatsonBucketTracker->SaveIpForWatsonBucket(adjustedIp);

                    // Set the flag that we captured the IP for Thread abort
                    DEBUG_STMT(pUEWatsonBucketTracker->SetCapturedForThreadAbort());
                    LOG((LF_EH, LL_INFO100, "SetupInitialThrowBucketDetails - Saved bucket IP for initial thread abort raise.\n"));
                }
            }
            else
            {
                // Create the buckets proactively for preallocated threadabort exception
                pWatsonBucketTracker->CaptureUnhandledInfoForWatson(TypeOfReportedError::UnhandledException, pThread, &gc.oCurrentThrowable);
                PTR_VOID pUnmanagedBuckets = pWatsonBucketTracker->RetrieveWatsonBuckets();
                if(pUnmanagedBuckets != NULL)
                {
                    // Copy the details over to the UE Watson bucket tracker so that we can use them if the exception
                    // is "reraised" after invoking the catch block.
                    //
                    // Since we can be here for preallocated threadabort exception when UE Tracker is simply
                    // carrying the IP (that has been copied to pWatsonBucketTracker and buckets captured for it),
                    // we will need to clear UE tracker so that we can copy over the captured buckets.
                    PTR_EHWatsonBucketTracker pUEWatsonBucketTracker = pExState->GetUEWatsonBucketTracker();
                    pUEWatsonBucketTracker->ClearWatsonBucketDetails();

                    // Copy over the buckets from the current tracker that captured them.
                    pUEWatsonBucketTracker->CopyEHWatsonBucketTracker(*(pWatsonBucketTracker));

                    // Buckets should be present now (unless the copy operation had an OOM)
                    if (pUEWatsonBucketTracker->RetrieveWatsonBuckets() != NULL)
                    {
                        // Set the flag that we captured buckets for Thread abort
                        DEBUG_STMT(pUEWatsonBucketTracker->SetCapturedForThreadAbort());
                        LOG((LF_EH, LL_INFO100, "SetupInitialThrowBucketDetails - Saved buckets for Watson Bucketing for initial thread abort raise.\n"));
                    }
                    else
                    {
                        // If we are here, then the bucket copy operation (above) failed due to OOM.
                        LOG((LF_EH, LL_INFO100, "SetupInitialThrowBucketDetails - Unable to save buckets for Watson Bucketing for initial thread abort raise, likely due to OOM.\n"));
                        pUEWatsonBucketTracker->ClearWatsonBucketDetails();
                    }
                }
                else
                {
                    // Watson helper function can bail out on us under OOM scenarios and return a NULL.
                    // We cannot do much in such a case.
                    LOG((LF_EH, LL_INFO100, "SetupInitialThrowBucketDetails - No buckets were captured and returned to us for initial thread abort raise. Likely encountered an OOM.\n"));
                }

                // Clear the buckets since we no longer need them
                pWatsonBucketTracker->ClearWatsonBucketDetails();
            }
        }
        else
        {
            // We have already saved the throw site IP for bucketing the non-ThreadAbort preallocated exceptions
            LOG((LF_EH, LL_INFO100, "SetupInitialThrowBucketDetails - Saved IP (%p) for Watson Bucketing for a preallocated exception\n", adjustedIp));
        }
    }
    else
    {
        // The inner exception object should be having either the IP for watson bucketing or the buckets themselves.
        // We shall copy over, whatever is available, to the current exception object.
        if (gc.refSourceWatsonBucketArray != NULL)
        {
            EX_TRY
            {
                // Copy the bucket details from innermost exception to the current exception object.
                CopyWatsonBucketsFromThrowableToCurrentThrowable(gc.refSourceWatsonBucketArray);
            }
            EX_CATCH
            {
            }
            EX_END_CATCH(SwallowAllExceptions);

            LOG((LF_EH, LL_INFO100, "SetupInitialThrowBucketDetails - Copied watson bucket details from the innermost exception\n"));
        }
        else if (ipForWatsonBuckets != NULL)
        {
            // Copy the IP to the current exception object
            ((EXCEPTIONREF)gc.oCurrentThrowable)->SetIPForWatsonBuckets(ipForWatsonBuckets);
            LOG((LF_EH, LL_INFO100, "SetupInitialThrowBucketDetails - Copied watson bucket IP from the innermost exception\n"));
        }
        else
        {
            gotWatsonBucketDetails = false;
        }
    }

done:
    // Set the flag that we have got the bucketing details
    if (gotWatsonBucketDetails)
    {
        pExState->GetFlags()->SetGotWatsonBucketDetails();
    }

    GCPROTECT_END();

#endif // !DACCESS_COMPILE
}

// This function is a wrapper to copy the watson bucket byte[] from the specified
// throwable to the current throwable.
void CopyWatsonBucketsFromThrowableToCurrentThrowable(U1ARRAYREF oManagedWatsonBuckets)
{
#ifndef DACCESS_COMPILE

    CONTRACTL
    {
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        THROWS;
        PRECONDITION(oManagedWatsonBuckets != NULL);
        PRECONDITION(IsWatsonEnabled());
    }
    CONTRACTL_END;

    struct
    {
        U1ARRAYREF oSourceWatsonBuckets;
    } _gc;

    ZeroMemory(&_gc, sizeof(_gc));
    GCPROTECT_BEGIN(_gc);
    _gc.oSourceWatsonBuckets = oManagedWatsonBuckets;

    // Copy the watson buckets to the current throwable by NOT passing
    // the second argument that will default to NULL.
    //
    // CopyWatsonBucketsBetweenThrowables will pass that NULL to
    // CopyWatsonBucketsToThrowables that will make it copy the buckets
    // to the current throwable.
    CopyWatsonBucketsBetweenThrowables(_gc.oSourceWatsonBuckets);

    GCPROTECT_END();

#endif // !DACCESS_COMPILE
}

// This function will copy the watson bucket byte[] from the source
// throwable to the destination throwable.
//
// If the destination throwable is NULL, it will result in the buckets
// being copied to the current throwable.
void CopyWatsonBucketsBetweenThrowables(U1ARRAYREF oManagedWatsonBuckets, OBJECTREF oThrowableTo /*=NULL*/)
{
#ifndef DACCESS_COMPILE

    CONTRACTL
    {
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        THROWS;
        PRECONDITION(oManagedWatsonBuckets != NULL);
        PRECONDITION(IsWatsonEnabled());
    }
    CONTRACTL_END;

    BOOL fRetVal = FALSE;

    struct
    {
        U1ARRAYREF oSourceWatsonBuckets;
        OBJECTREF oTo;
        OBJECTREF oWatsonBuckets;
    } _gc;

    ZeroMemory(&_gc, sizeof(_gc));
    GCPROTECT_BEGIN(_gc);

    _gc.oSourceWatsonBuckets = oManagedWatsonBuckets;
    _gc.oTo = (oThrowableTo == NULL)?GetThread()->GetThrowable():oThrowableTo;
    _ASSERTE(_gc.oTo != NULL);

    // The target throwable to which Watson buckets are going to be copied
    // shouldnt be preallocated exception object.
    _ASSERTE(!CLRException::IsPreallocatedExceptionObject(_gc.oTo));

    // Size of a watson bucket
    DWORD size = sizeof(GenericModeBlock);

    // Create the managed byte[] to hold the bucket details
    _gc.oWatsonBuckets = AllocatePrimitiveArray(ELEMENT_TYPE_U1, size);
    if (_gc.oWatsonBuckets == NULL)
    {
        // return failure if failed to create bucket array
        fRetVal = FALSE;
    }
    else
    {
        // Get the raw array data pointer of the source array
        PTR_VOID pRawSourceWatsonBucketArray = dac_cast<PTR_VOID>(_gc.oSourceWatsonBuckets->GetDataPtr());

        // Get the raw array data pointer to the destination array
        U1ARRAYREF refDestWatsonBucketArray = (U1ARRAYREF)_gc.oWatsonBuckets;
        PTR_VOID pRawDestWatsonBucketArray = dac_cast<PTR_VOID>(refDestWatsonBucketArray->GetDataPtr());

        // Deep copy the bucket information to the managed array
        memcpyNoGCRefs(pRawDestWatsonBucketArray, pRawSourceWatsonBucketArray, size);

        // Setup the managed field reference to point to the byte array.
        //
        // The throwable, to which the buckets are being copied to, may be
        // having existing buckets (e.g. when TypeInitialization exception
        // maybe thrown again when attempt is made to load the originally
        // failed type).
        //
        // This is also possible if exception object is used as singleton
        // and thrown by multiple threads.
        if (((EXCEPTIONREF)_gc.oTo)->AreWatsonBucketsPresent())
        {
            LOG((LF_EH, LL_INFO1000, "CopyWatsonBucketsBetweenThrowables: Throwable (%p) being copied to had previous buckets.\n", OBJECTREFToObject(_gc.oTo)));
        }

        ((EXCEPTIONREF)_gc.oTo)->SetWatsonBucketReference(_gc.oWatsonBuckets);

        fRetVal = TRUE;
    }

    // We shouldn't be here when fRetVal is FALSE since failure to allocate the primitive
    // array should throw an OOM.
    _ASSERTE(fRetVal);

    GCPROTECT_END();
#endif // !DACCESS_COMPILE
}

// This function will copy the watson bucket information to the managed byte[] in
// the specified managed exception object.
//
// If throwable is not specified, it will be copied to the current throwable.
//
// pUnmanagedBuckets is a pointer to native memory that cannot be affected by GC.
BOOL CopyWatsonBucketsToThrowable(PTR_VOID pUnmanagedBuckets, OBJECTREF oTargetThrowable /*= NULL*/)
{
#ifndef DACCESS_COMPILE

    CONTRACTL
    {
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        THROWS;
        PRECONDITION(GetThreadNULLOk() != NULL);
        PRECONDITION(pUnmanagedBuckets != NULL);
        PRECONDITION(!CLRException::IsPreallocatedExceptionObject((oTargetThrowable == NULL)?GetThread()->GetThrowable():oTargetThrowable));
        PRECONDITION(IsWatsonEnabled());
    }
    CONTRACTL_END;

    BOOL fRetVal = TRUE;
    struct
    {
        OBJECTREF oThrowable;
        OBJECTREF oWatsonBuckets;
    } _gc;

    ZeroMemory(&_gc, sizeof(_gc));
    GCPROTECT_BEGIN(_gc);
    _gc.oThrowable = (oTargetThrowable == NULL)?GetThread()->GetThrowable():oTargetThrowable;

    // Throwable to which buckets should be copied to, must exist.
    _ASSERTE(_gc.oThrowable != NULL);

    // Size of a watson bucket
    DWORD size = sizeof(GenericModeBlock);

    _gc.oWatsonBuckets = AllocatePrimitiveArray(ELEMENT_TYPE_U1, size);
    if (_gc.oWatsonBuckets == NULL)
    {
        // return failure if failed to create bucket array
        fRetVal = FALSE;
    }
    else
    {
        // Get the raw array data pointer
        U1ARRAYREF refWatsonBucketArray = (U1ARRAYREF)_gc.oWatsonBuckets;
        PTR_VOID pRawWatsonBucketArray = dac_cast<PTR_VOID>(refWatsonBucketArray->GetDataPtr());

        // Deep copy the bucket information to the managed array
        memcpyNoGCRefs(pRawWatsonBucketArray, pUnmanagedBuckets, size);

        // Setup the managed field reference to point to the byte array.
        //
        // The throwable, to which the buckets are being copied to, may be
        // having existing buckets (e.g. when TypeInitialization exception
        // maybe thrown again when attempt is made to load the originally
        // failed type).
        //
        // This is also possible if exception object is used as singleton
        // and thrown by multiple threads.
        if (((EXCEPTIONREF)_gc.oThrowable)->AreWatsonBucketsPresent())
        {
            LOG((LF_EH, LL_INFO1000, "CopyWatsonBucketsToThrowable: Throwable (%p) being copied to had previous buckets.\n", OBJECTREFToObject(_gc.oThrowable)));
        }

        ((EXCEPTIONREF)_gc.oThrowable)->SetWatsonBucketReference(_gc.oWatsonBuckets);
    }

    GCPROTECT_END();

    return fRetVal;
#else // DACCESS_COMPILE
    return TRUE;
#endif // !DACCESS_COMPILE
}

// This function will setup the bucketing information for nested exceptions
// raised. These would be any exceptions thrown from within the confines of
// managed EH clauses and include "rethrow" and "throw new ...".
//
// This is called from within CLR's personality routine for managed
// exceptions to preemptively setup the watson buckets from the ones that may
// already exist. If none exist already, we will automatically endup in the
// path (SetupInitialThrowBucketDetails) that will set up buckets for the
// exception being thrown.
void SetStateForWatsonBucketing(BOOL fIsRethrownException, OBJECTHANDLE ohOriginalException)
{
#ifndef DACCESS_COMPILE

    // On CoreCLR, Watson may not be enabled. Thus, we should
    // skip this.
    if (!IsWatsonEnabled())
    {
        return;
    }

    CONTRACTL
    {
        GC_TRIGGERS;
        MODE_ANY;
        NOTHROW;
        PRECONDITION(GetThreadNULLOk() != NULL);
        PRECONDITION(IsWatsonEnabled());
    }
    CONTRACTL_END;

    // Switch to COOP mode
    GCX_COOP();

    struct
    {
        OBJECTREF oCurrentThrowable;
        OBJECTREF oInnerMostExceptionThrowable;
        U1ARRAYREF refSourceWatsonBucketArray;
    } gc;
    gc.oCurrentThrowable = NULL;
    gc.oInnerMostExceptionThrowable = NULL;
    gc.refSourceWatsonBucketArray = NULL;
    GCPROTECT_BEGIN(gc);

    Thread* pThread = GetThread();

    // Get the current exception state of the thread
    ThreadExceptionState* pCurExState = pThread->GetExceptionState();
    _ASSERTE(NULL != pCurExState);

    // Ensure that the exception tracker exists
    _ASSERTE(pCurExState->GetCurrentExceptionTracker() != NULL);

    // Get the current throwable
    gc.oCurrentThrowable = pThread->GetThrowable();
    _ASSERTE(gc.oCurrentThrowable != NULL);

    // Is the throwable a preallocated exception object?
    BOOL fIsPreallocatedExceptionObject = CLRException::IsPreallocatedExceptionObject(gc.oCurrentThrowable);

    // Copy the bucketing details from the original exception tracker if the current exception is a rethrow
    // AND the throwable is a preallocated exception object.
    //
    // For rethrown non-preallocated exception objects, the throwable would already have the bucketing
    // details inside it.
    if (fIsRethrownException)
    {
        if (fIsPreallocatedExceptionObject)
        {
            // Get the WatsonBucket tracker for the original exception, starting search from the previous EH tracker.
            // This is required so that when a preallocated exception is rethrown, then the current tracker would have
            // the same throwable as the original exception but no bucketing details.
            //
            // To ensure GetWatsonBucketTrackerForPreallocatedException uses the EH tracker corresponding to the original
            // exception to get the bucketing details, we pass TRUE as the third parameter.
            PTR_EHWatsonBucketTracker pPreallocWatsonBucketTracker = GetWatsonBucketTrackerForPreallocatedException(gc.oCurrentThrowable, FALSE, TRUE);
            if (pPreallocWatsonBucketTracker != NULL)
            {
                if (!IsThrowableThreadAbortException(gc.oCurrentThrowable))
                {
                    // For non-thread abort preallocated exceptions, we copy the bucketing details
                    // from their corresponding watson bucket tracker to the one corresponding to the
                    // rethrow that is taking place.
                    //
                    // Bucketing details for preallocated exception may not be present if the exception came
                    // from across AD transition and we attempted to copy them over from the UETracker, when
                    // the exception was reraised in the calling AD, and the copy operation failed due to OOM.
                    //
                    // In such a case, when the reraised exception is caught and rethrown, we will not have
                    // any bucketing details.
                    if (NULL != pPreallocWatsonBucketTracker->RetrieveWatsonBucketIp())
                    {
                        // Copy the bucketing details now
                        pCurExState->GetCurrentExceptionTracker()->GetWatsonBucketTracker()->CopyEHWatsonBucketTracker(*pPreallocWatsonBucketTracker);
                    }
                    else
                    {
                        LOG((LF_EH, LL_INFO1000, "SetStateForWatsonBucketing - Watson bucketing details for rethrown preallocated exception not found in the EH tracker corresponding to the original exception. This is likely due to a previous OOM.\n"));
                        LOG((LF_EH, LL_INFO1000, ">>>>>>>>>>>>>>>>>>>>>>>>>>   Original WatsonBucketTracker = %p\n", pPreallocWatsonBucketTracker));

                        // Make the active tracker clear
                        pCurExState->GetCurrentExceptionTracker()->GetWatsonBucketTracker()->ClearWatsonBucketDetails();
                    }
                }
    #ifdef _DEBUG
                else
                {
                    // For thread abort exceptions, the returned watson bucket tracker
                    // would correspond to UE Watson bucket tracker and it will have
                    // all the details.
                    _ASSERTE(pPreallocWatsonBucketTracker == pCurExState->GetUEWatsonBucketTracker());
                }
    #endif // _DEBUG
            }
            else
            {
                // OOM can result in not having a Watson bucket tracker with valid bucketing details for a preallocated exception.
                // Thus, we may end up here. For details, see implementation of GetWatsonBucketTrackerForPreallocatedException.
                LOG((LF_EH, LL_INFO1000, "SetStateForWatsonBucketing - Watson bucketing tracker for rethrown preallocated exception not found. This is likely due to a previous OOM.\n"));

                // Make the active tracker clear
                pCurExState->GetCurrentExceptionTracker()->GetWatsonBucketTracker()->ClearWatsonBucketDetails();
            }
        }
        else
        {
            // We dont need to do anything here since the throwable would already have the bucketing
            // details inside it. Simply assert that the original exception object is the same as the current throwable.
            //
            // We cannot assert for Watson buckets since the original throwable may not have got them in
            // SetupInitialThrowBucketDetails due to OOM
            _ASSERTE((NULL != ohOriginalException) && (ObjectFromHandle(ohOriginalException) == gc.oCurrentThrowable));
            if ((((EXCEPTIONREF)gc.oCurrentThrowable)->AreWatsonBucketsPresent() == FALSE) &&
                (((EXCEPTIONREF)gc.oCurrentThrowable)->IsIPForWatsonBucketsPresent() == FALSE))
            {
                LOG((LF_EH, LL_INFO1000, "SetStateForWatsonBucketing - Regular rethrown exception (%p) does not have Watson buckets, likely due to OOM.\n",
                        OBJECTREFToObject(gc.oCurrentThrowable)));
            }
        }

        // Set the flag that we have bucketing details for the exception
        pCurExState->GetFlags()->SetGotWatsonBucketDetails();
        LOG((LF_EH, LL_INFO1000, "SetStateForWatsonBucketing - Using original exception details for Watson bucketing for rethrown exception.\n"));
    }
    else
    {
        // If we are here, then an exception is being thrown from within the
        // managed EH clauses of fault, finally or catch, with an inner exception.

        // By default, we will create buckets based upon the exception being thrown unless
        // thrown exception has an inner exception that has got bucketing details
        BOOL fCreateBucketsForExceptionBeingThrown = TRUE;

        // Start off by assuming that inner exception object is not preallocated
        BOOL fIsInnerExceptionPreallocated = FALSE;

        // Reference to the WatsonBucket tracker for the inner exception, if it is preallocated
        PTR_EHWatsonBucketTracker pInnerExceptionWatsonBucketTracker = NULL;

        // Since this is a new exception being thrown, we will check if it has buckets already or not.
        // This is possible when Reflection throws TargetInvocationException with an inner exception
        // that is preallocated exception object. In such a case, we copy the inner exception details
        // to the TargetInvocationException object already. This is done in InvokeImpl in ReflectionInvocation.cpp.
        if (((EXCEPTIONREF)gc.oCurrentThrowable)->AreWatsonBucketsPresent() ||
            ((EXCEPTIONREF)gc.oCurrentThrowable)->IsIPForWatsonBucketsPresent())
        {
            goto done;
        }

        // If no buckets are present, then we will check if it has an innermost exception or not.
        // If it does, then we will make the exception being thrown use the bucketing details of the
        // innermost exception.
        //
        // If there is no innermost exception or if one is present without bucketing details, then
        // we will have bucket details based upon the exception being thrown.

        // Get the innermost exception from the exception being thrown.
        gc.oInnerMostExceptionThrowable = ((EXCEPTIONREF)gc.oCurrentThrowable)->GetBaseException();
        if (gc.oInnerMostExceptionThrowable != NULL)
        {
            fIsInnerExceptionPreallocated = CLRException::IsPreallocatedExceptionObject(gc.oInnerMostExceptionThrowable);

            // Preallocated exception objects do not have inner exception objects.
            // Thus, if we are here, then the current throwable cannot be
            // a preallocated exception object.
            _ASSERTE(!fIsPreallocatedExceptionObject);

            // Create the new buckets only if the innermost exception object
            // does not have them already.
            if (fIsInnerExceptionPreallocated)
            {
                // If we are able to find the watson bucket tracker for the preallocated
                // inner exception, then we dont need to create buckets for throw site.
                pInnerExceptionWatsonBucketTracker = GetWatsonBucketTrackerForPreallocatedException(gc.oInnerMostExceptionThrowable, FALSE, TRUE);
                fCreateBucketsForExceptionBeingThrown = ((pInnerExceptionWatsonBucketTracker != NULL) &&
                                                         (pInnerExceptionWatsonBucketTracker->RetrieveWatsonBucketIp() != NULL)) ? FALSE : TRUE;
            }
            else
            {
                // Since the inner exception object is not preallocated, create
                // watson buckets only if it does not have them.
                fCreateBucketsForExceptionBeingThrown = !(((EXCEPTIONREF)gc.oInnerMostExceptionThrowable)->AreWatsonBucketsPresent() ||
                                                          ((EXCEPTIONREF)gc.oInnerMostExceptionThrowable)->IsIPForWatsonBucketsPresent());
            }
        }

        // If we are NOT going to create buckets for the thrown exception,
        // then copy them over from the inner exception object.
        //
        // If we have to create the buckets for the thrown exception,
        // we wont do that now - it will be done in StackTraceInfo::AppendElement
        // when we get the IP for bucketing.
        if (!fCreateBucketsForExceptionBeingThrown)
        {
            bool gotWatsonBucketDetails = true;

            // Preallocated exception objects do not have inner exception objects.
            // Thus, if we are here, then the current throwable cannot be
            // a preallocated exception object.
            _ASSERTE(!fIsPreallocatedExceptionObject);

            if (fIsInnerExceptionPreallocated)
            {

                // We should have the inner exception watson bucket tracker
                _ASSERTE((pInnerExceptionWatsonBucketTracker != NULL) && (pInnerExceptionWatsonBucketTracker->RetrieveWatsonBucketIp() != NULL));

                // Capture the buckets for the innermost exception if they dont already exist.
                // Since the current throwable cannot be preallocated (see the assert above),
                // copy the buckets to the throwable.
                PTR_VOID pInnerExceptionWatsonBuckets = pInnerExceptionWatsonBucketTracker->RetrieveWatsonBuckets();
                if (pInnerExceptionWatsonBuckets == NULL)
                {
                    // Capture the buckets since they dont exist
                    pInnerExceptionWatsonBucketTracker->CaptureUnhandledInfoForWatson(TypeOfReportedError::UnhandledException, pThread, &gc.oInnerMostExceptionThrowable);
                    pInnerExceptionWatsonBuckets = pInnerExceptionWatsonBucketTracker->RetrieveWatsonBuckets();
                }

                if (pInnerExceptionWatsonBuckets == NULL)
                {
                    // Couldnt capture details like due to OOM
                    LOG((LF_EH, LL_INFO1000, "SetStateForWatsonBucketing - Preallocated inner-exception's WBTracker (%p) has no bucketing details for the thrown exception, likely due to OOM.\n", pInnerExceptionWatsonBucketTracker));
                }
                else
                {
                    // Copy the buckets to the current throwable
                    BOOL fCopied = TRUE;
                    EX_TRY
                    {
                        fCopied = CopyWatsonBucketsToThrowable(pInnerExceptionWatsonBuckets);
                        _ASSERTE(fCopied);
                    }
                    EX_CATCH
                    {
                        // Dont do anything if we fail to copy the buckets - this is no different than
                        // the native watson helper functions failing under OOM
                        fCopied = FALSE;
                    }
                    EX_END_CATCH(SwallowAllExceptions);
                }
            }
            else
            {
                // Assert that the inner exception has the Watson buckets
                _ASSERTE(gc.oInnerMostExceptionThrowable != NULL);

                gc.refSourceWatsonBucketArray = ((EXCEPTIONREF)gc.oInnerMostExceptionThrowable)->GetWatsonBucketReference();
                UINT_PTR ipForWatsonBuckets = ((EXCEPTIONREF)gc.oInnerMostExceptionThrowable)->GetIPForWatsonBuckets();

                if (gc.refSourceWatsonBucketArray != NULL)
                {
                    // Copy the bucket information from the inner exception object to the current throwable
                    EX_TRY
                    {
                        CopyWatsonBucketsFromThrowableToCurrentThrowable(gc.refSourceWatsonBucketArray);
                    }
                    EX_CATCH
                    {
                        // Dont do anything if we fail to copy the buckets - this is no different than
                        // the native watson helper functions failing under OOM
                    }
                    EX_END_CATCH(SwallowAllExceptions);
                }
                else if (ipForWatsonBuckets != NULL)
                {
                    // Copy the IP for Watson bucketing to the exception object
                    ((EXCEPTIONREF)gc.oCurrentThrowable)->SetIPForWatsonBuckets(ipForWatsonBuckets);
                }
                else
                {
                    gotWatsonBucketDetails = false;
                }
            }

            if (gotWatsonBucketDetails)
            {
                // Set the flag that we got bucketing details for the exception
                pCurExState->GetFlags()->SetGotWatsonBucketDetails();
                LOG((LF_EH, LL_INFO1000, "SetStateForWatsonBucketing - Using innermost exception details for Watson bucketing for thrown exception.\n"));
            }
        }
done:;
    }

    GCPROTECT_END();

#endif // !DACCESS_COMPILE
}

// Constructor that will do the initialization of the object
EHWatsonBucketTracker::EHWatsonBucketTracker()
{
    LIMITED_METHOD_CONTRACT;

    Init();
}

// Reset the fields to default values
void EHWatsonBucketTracker::Init()
{
    LIMITED_METHOD_CONTRACT;

    m_WatsonUnhandledInfo.m_UnhandledIp = 0;
    m_WatsonUnhandledInfo.m_pUnhandledBuckets = NULL;

    DEBUG_STMT(ResetFlags());

    LOG((LF_EH, LL_INFO1000, "EHWatsonBucketTracker::Init - initializing watson bucket tracker (%p)\n", this));
}

// This method copies the bucketing details from the specified throwable
// to the current Watson Bucket tracker.
void EHWatsonBucketTracker::CopyBuckets(U1ARRAYREF oBuckets)
{
#ifndef DACCESS_COMPILE
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        PRECONDITION(oBuckets != NULL);
        PRECONDITION(IsWatsonEnabled());
    }
    CONTRACTL_END;

    GCX_COOP();

    struct
    {
        U1ARRAYREF oFromBuckets;
    } _gc;

    ZeroMemory(&_gc, sizeof(_gc));
    GCPROTECT_BEGIN(_gc);

    _gc.oFromBuckets = oBuckets;

    LOG((LF_EH, LL_INFO1000, "EHWatsonBucketTracker::CopyEHWatsonBucketTracker - Copying bucketing details from bucket (%p) to tracker (%p)\n",
                            OBJECTREFToObject(_gc.oFromBuckets), this));

    // Watson bucket is a "GenericModeBlock" type. Set up an empty GenericModeBlock
    // to hold the bucket parameters.
    GenericModeBlock *pgmb = new (nothrow) GenericModeBlock;
    if (pgmb == NULL)
    {
        // If we are unable to allocate memory to hold the WatsonBucket, then
        // reset the IP and bucket pointer to NULL and bail out
        SaveIpForWatsonBucket(NULL);
        m_WatsonUnhandledInfo.m_pUnhandledBuckets = NULL;
    }
    else
    {
        // Get the raw array data pointer
        PTR_VOID pRawWatsonBucketArray = dac_cast<PTR_VOID>(_gc.oFromBuckets->GetDataPtr());

        // Copy over the details to our new allocation
        memcpyNoGCRefs(pgmb, pRawWatsonBucketArray, sizeof(GenericModeBlock));

        // and save the address where the buckets were copied
        _ASSERTE(m_WatsonUnhandledInfo.m_pUnhandledBuckets == NULL);
        m_WatsonUnhandledInfo.m_pUnhandledBuckets = pgmb;
    }

    GCPROTECT_END();

    LOG((LF_EH, LL_INFO1000, "EHWatsonBucketTracker::CopyEHWatsonBucketTracker - Copied Watson Buckets from throwable to (%p)\n",
                            m_WatsonUnhandledInfo.m_pUnhandledBuckets));
#endif // !DACCESS_COMPILE
}

// This method copies the bucketing details from the specified Watson Bucket tracker
// to the current one.
void EHWatsonBucketTracker::CopyEHWatsonBucketTracker(const EHWatsonBucketTracker& srcTracker)
{
#ifndef DACCESS_COMPILE
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        PRECONDITION(m_WatsonUnhandledInfo.m_UnhandledIp == 0);
        PRECONDITION(m_WatsonUnhandledInfo.m_pUnhandledBuckets == NULL);
        PRECONDITION(IsWatsonEnabled());
    }
    CONTRACTL_END;

    LOG((LF_EH, LL_INFO1000, "EHWatsonBucketTracker::CopyEHWatsonBucketTracker - Copying bucketing details from %p to %p\n", &srcTracker, this));

    // Copy the tracking details over from the specified tracker
    SaveIpForWatsonBucket(srcTracker.m_WatsonUnhandledInfo.m_UnhandledIp);

    if (srcTracker.m_WatsonUnhandledInfo.m_pUnhandledBuckets != NULL)
    {
        // To save the bucket information, we will need to memcpy.
        // This is to ensure that if the original watson bucket tracker
        // (for original exception) is released and its memory deallocated,
        // the new watson bucket tracker (for rethrown exception, for e.g.)
        // would still have all the bucket details.

        // Watson bucket is a "GenericModeBlock" type. Set up an empty GenericModeBlock
        // to hold the bucket parameters.
        GenericModeBlock *pgmb = new (nothrow) GenericModeBlock;
        if (pgmb == NULL)
        {
            // If we are unable to allocate memory to hold the WatsonBucket, then
            // reset the IP and bucket pointer to NULL and bail out
            SaveIpForWatsonBucket(NULL);
            m_WatsonUnhandledInfo.m_pUnhandledBuckets = NULL;

            LOG((LF_EH, LL_INFO1000, "EHWatsonBucketTracker::CopyEHWatsonBucketTracker - Not copying buckets due to out of memory.\n"));
        }
        else
        {
            // Copy over the details to our new allocation
            memcpyNoGCRefs(pgmb, srcTracker.m_WatsonUnhandledInfo.m_pUnhandledBuckets, sizeof(GenericModeBlock));

            // and save the address where the buckets were copied
            m_WatsonUnhandledInfo.m_pUnhandledBuckets = pgmb;
        }
    }

    LOG((LF_EH, LL_INFO1000, "EHWatsonBucketTracker::CopyEHWatsonBucketTracker - Copied Watson Bucket to (%p)\n", m_WatsonUnhandledInfo.m_pUnhandledBuckets));
#endif // !DACCESS_COMPILE
}

void EHWatsonBucketTracker::SaveIpForWatsonBucket(
    UINT_PTR    ip)                     // The new IP.
{
#ifndef DACCESS_COMPILE
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        PRECONDITION(IsWatsonEnabled());
    }
    CONTRACTL_END;

    LOG((LF_EH, LL_INFO1000, "EHWatsonBucketTracker::SaveIpForUnhandledInfo  - this = %p, IP = %p\n", this, ip));

    // Since we are setting a new IP for tracking buckets,
    // clear any existing details we may hold
    ClearWatsonBucketDetails();

    // Save the new IP for bucketing
    m_WatsonUnhandledInfo.m_UnhandledIp = ip;
#endif // !DACCESS_COMPILE
}

UINT_PTR EHWatsonBucketTracker::RetrieveWatsonBucketIp()
{
    LIMITED_METHOD_CONTRACT;

    LOG((LF_EH, LL_INFO1000, "EHWatsonBucketTracker::RetrieveWatsonBucketIp - this = %p, IP = %p\n", this, m_WatsonUnhandledInfo.m_UnhandledIp));

    return m_WatsonUnhandledInfo.m_UnhandledIp;
}

// This function returns the reference to the Watson buckets tracked by the
// instance of WatsonBucket tracker.
//
// This is *also* invoked from the DAC when buckets are requested.
PTR_VOID EHWatsonBucketTracker::RetrieveWatsonBuckets()
{
#if !defined(DACCESS_COMPILE)
    if (!IsWatsonEnabled())
    {
        return NULL;
    }
#endif //!defined(DACCESS_COMPILE)

    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        PRECONDITION(IsWatsonEnabled());
    }
    CONTRACTL_END;

    LOG((LF_EH, LL_INFO1000, "EHWatsonBucketTracker::RetrieveWatsonBuckets - this = %p, bucket address = %p\n", this, m_WatsonUnhandledInfo.m_pUnhandledBuckets));

    return m_WatsonUnhandledInfo.m_pUnhandledBuckets;
}

void EHWatsonBucketTracker::ClearWatsonBucketDetails()
{
#ifndef DACCESS_COMPILE

    if (!IsWatsonEnabled())
    {
        return;
    }

    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        PRECONDITION(IsWatsonEnabled());
    }
    CONTRACTL_END;


    LOG((LF_EH, LL_INFO1000, "EHWatsonBucketTracker::ClearWatsonBucketDetails for tracker (%p)\n", this));

    if (m_WatsonUnhandledInfo.m_pUnhandledBuckets != NULL)
    {
        FreeBucketParametersForManagedException(m_WatsonUnhandledInfo.m_pUnhandledBuckets);
    }

    Init();
#endif // !DACCESS_COMPILE
}

void EHWatsonBucketTracker::CaptureUnhandledInfoForWatson(TypeOfReportedError tore, Thread * pThread, OBJECTREF * pThrowable)
{
#ifndef DACCESS_COMPILE
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(IsWatsonEnabled());
    }
    CONTRACTL_END;

    LOG((LF_EH, LL_INFO1000, "EHWatsonBucketTracker::CaptureUnhandledInfoForWatson capturing watson bucket details for (%p)\n", this));

    // Only capture the bucket information if there is an IP AND we dont already have collected them.
    // We could have collected them from a previous AD transition and wouldnt want to overwrite them.
    if (m_WatsonUnhandledInfo.m_UnhandledIp != 0)
    {
        if (m_WatsonUnhandledInfo.m_pUnhandledBuckets == NULL)
        {
            // Get the bucket details since we dont have them
            m_WatsonUnhandledInfo.m_pUnhandledBuckets = GetBucketParametersForManagedException(m_WatsonUnhandledInfo.m_UnhandledIp, tore, pThread, pThrowable);
            LOG((LF_EH, LL_INFO1000, "EHWatsonBucketTracker::CaptureUnhandledInfoForWatson captured the following watson bucket details: (this = %p, bucket addr = %p)\n",
                this, m_WatsonUnhandledInfo.m_pUnhandledBuckets));
        }
        else
        {
            // We already have the bucket details - so no need to capture them again
            LOG((LF_EH, LL_INFO1000, "EHWatsonBucketTracker::CaptureUnhandledInfoForWatson already have the watson bucket details: (this = %p, bucket addr = %p)\n",
                this, m_WatsonUnhandledInfo.m_pUnhandledBuckets));
        }
    }
    else
    {
        LOG((LF_EH, LL_INFO1000, "EHWatsonBucketTracker::CaptureUnhandledInfoForWatson didnt have an IP to use for capturing watson buckets\n"));
    }
#endif // !DACCESS_COMPILE
}
#endif // !TARGET_UNIX

// Given an exception code, this method returns a BOOL to indicate if the
// code belongs to a corrupting exception or not.
BOOL IsProcessCorruptedStateException(DWORD dwExceptionCode, OBJECTREF throwable)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        if (throwable != NULL) MODE_COOPERATIVE; else MODE_ANY;
    }
    CONTRACTL_END;

    switch (dwExceptionCode)
    {
    case STATUS_ACCESS_VIOLATION:
        if (throwable != NULL && CoreLibBinder::IsException(throwable->GetMethodTable(), kNullReferenceException))
            return FALSE;
        break;
    case STATUS_STACK_OVERFLOW:
    case EXCEPTION_ILLEGAL_INSTRUCTION:
    case EXCEPTION_IN_PAGE_ERROR:
    case EXCEPTION_INVALID_DISPOSITION:
    case EXCEPTION_NONCONTINUABLE_EXCEPTION:
    case EXCEPTION_PRIV_INSTRUCTION:
    case STATUS_UNWIND_CONSOLIDATE:
        break;
    default:
        return FALSE;
    }

    if (CLRConfig::GetConfigValue(CLRConfig::UNSUPPORTED_legacyCorruptedStateExceptionsPolicy))
        return FALSE;

    return TRUE;
}

#ifndef DACCESS_COMPILE
// This method will deliver the actual exception notification. Its assumed that the caller has done the necessary checks, including
// checking whether the delegate can be invoked for the exception's corruption severity.
void ExceptionNotifications::DeliverExceptionNotification(ExceptionNotificationHandlerType notificationType, OBJECTREF *pDelegate,
        OBJECTREF *pAppDomain, OBJECTREF *pEventArgs)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(pDelegate  != NULL && IsProtectedByGCFrame(pDelegate) && (*pDelegate != NULL));
        PRECONDITION(pEventArgs != NULL && IsProtectedByGCFrame(pEventArgs));
        PRECONDITION(pAppDomain != NULL && IsProtectedByGCFrame(pAppDomain));
    }
    CONTRACTL_END;

    PREPARE_NONVIRTUAL_CALLSITE_USING_CODE(DELEGATEREF(*pDelegate)->GetMethodPtr());

    DECLARE_ARGHOLDER_ARRAY(args, 3);

    args[ARGNUM_0] = OBJECTREF_TO_ARGHOLDER(DELEGATEREF(*pDelegate)->GetTarget());
    args[ARGNUM_1] = OBJECTREF_TO_ARGHOLDER(*pAppDomain);
    args[ARGNUM_2] = OBJECTREF_TO_ARGHOLDER(*pEventArgs);

    CALL_MANAGED_METHOD_NORET(args);
}

// To include definition of COMDelegate::GetMethodDesc
#include "comdelegate.h"

// This method constructs the arguments to be passed to the exception notification event callback
void ExceptionNotifications::GetEventArgsForNotification(ExceptionNotificationHandlerType notificationType,
                                                         OBJECTREF *pOutEventArgs, OBJECTREF *pThrowable)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(notificationType != UnhandledExceptionHandler);
        PRECONDITION((pOutEventArgs != NULL) && IsProtectedByGCFrame(pOutEventArgs));
        PRECONDITION(*pOutEventArgs == NULL);
        PRECONDITION((pThrowable != NULL) && (*pThrowable != NULL) && IsProtectedByGCFrame(pThrowable));
        PRECONDITION(IsException((*pThrowable)->GetMethodTable())); // We expect a valid exception object
    }
    CONTRACTL_END;

    MethodTable *pMTEventArgs = NULL;
    BinderMethodID idEventArgsCtor = METHOD__FIRSTCHANCE_EVENTARGS__CTOR;

    EX_TRY
    {
        switch(notificationType)
        {
            case FirstChanceExceptionHandler:
                pMTEventArgs = CoreLibBinder::GetClass(CLASS__FIRSTCHANCE_EVENTARGS);
                idEventArgsCtor = METHOD__FIRSTCHANCE_EVENTARGS__CTOR;
                break;
            default:
                _ASSERTE(!"Invalid Exception Notification Handler!");
                break;
        }

        // Allocate the instance of the eventargs corresponding to the notification
        *pOutEventArgs = AllocateObject(pMTEventArgs);

        // Prepare to invoke the .ctor
        MethodDescCallSite ctor(idEventArgsCtor, pOutEventArgs);

        // Setup the arguments to be passed to the notification specific EventArgs .ctor
        if (notificationType == FirstChanceExceptionHandler)
        {
            // FirstChance notification takes only a single argument: the exception object.
            ARG_SLOT args[] =
            {
                ObjToArgSlot(*pOutEventArgs),
                ObjToArgSlot(*pThrowable),
            };

            ctor.Call(args);
        }
        else
        {
            // Since we have already asserted above, just set the args to NULL.
            *pOutEventArgs = NULL;
        }
    }
    EX_CATCH
    {
        // Set event args to be NULL incase of any error (e.g. OOM)
        *pOutEventArgs = NULL;
        LOG((LF_EH, LL_INFO100, "ExceptionNotifications::GetEventArgsForNotification: Setting event args to NULL due to an exception.\n"));
    }
    EX_END_CATCH(RethrowTerminalExceptions);
}

// This SEH filter will be invoked when an exception escapes out of the exception notification
// callback and enters the runtime. In such a case, we ill simply failfast.
static LONG ExceptionNotificationFilter(PEXCEPTION_POINTERS pExceptionInfo, LPVOID pParam)
{
    EEPOLICY_HANDLE_FATAL_ERROR(COR_E_EXECUTIONENGINE);
    return -1;
}

// This method returns a BOOL to indicate if the AppDomain is ready to receive exception notifications or not.
BOOL ExceptionNotifications::CanDeliverNotificationToCurrentAppDomain(ExceptionNotificationHandlerType notificationType)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(GetThreadNULLOk() != NULL);
        PRECONDITION(notificationType  != UnhandledExceptionHandler);
    }
    CONTRACTL_END;

    // Do we have handler(s) of the specific type wired up?
    if (notificationType == FirstChanceExceptionHandler)
    {
        return CoreLibBinder::GetField(FIELD__APPCONTEXT__FIRST_CHANCE_EXCEPTION)->GetStaticOBJECTREF() != NULL;
    }
    else
    {
        _ASSERTE(!"Invalid exception notification handler specified!");
        return FALSE;
    }
}

// This method wraps the call to the actual 'DeliverNotificationInternal' method in an SEH filter
// so that if an exception escapes out of the notification callback, we will trigger failfast from
// our filter.
void ExceptionNotifications::DeliverNotification(ExceptionNotificationHandlerType notificationType,
                                                 OBJECTREF *pThrowable)
{
    STATIC_CONTRACT_GC_TRIGGERS;
    STATIC_CONTRACT_NOTHROW; // NOTHROW because incase of an exception, we will FailFast.
    STATIC_CONTRACT_MODE_COOPERATIVE;

    struct TryArgs
    {
        ExceptionNotificationHandlerType notificationType;
        OBJECTREF *pThrowable;
    } args;

    args.notificationType = notificationType;
    args.pThrowable = pThrowable;

    PAL_TRY(TryArgs *, pArgs, &args)
    {
        // Make the call to the actual method that will invoke the callbacks
        ExceptionNotifications::DeliverNotificationInternal(pArgs->notificationType,
            pArgs->pThrowable);
    }
    PAL_EXCEPT_FILTER(ExceptionNotificationFilter)
    {
        // We should never be entering this handler since there should be
        // no exception escaping out of a callback. If we are here,
        // failfast.
        EEPOLICY_HANDLE_FATAL_ERROR(COR_E_EXECUTIONENGINE);
    }
    PAL_ENDTRY;
}

// This method will deliver the exception notification to the current AppDomain.
void ExceptionNotifications::DeliverNotificationInternal(ExceptionNotificationHandlerType notificationType,
                                                 OBJECTREF *pThrowable)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;

        // Unhandled Exception Notification is delivered via Unhandled Exception Processing
        // mechanism.
        PRECONDITION(notificationType != UnhandledExceptionHandler);
        PRECONDITION((pThrowable != NULL) && (*pThrowable != NULL));
        PRECONDITION(ExceptionNotifications::CanDeliverNotificationToCurrentAppDomain(notificationType));
    }
    CONTRACTL_END;

    Thread *pCurThread = GetThreadNULLOk();
    _ASSERTE(pCurThread != NULL);

    // Get the current AppDomain
    AppDomain *pCurDomain = GetAppDomain();
    _ASSERTE(pCurDomain != NULL);

    struct
    {
        OBJECTREF oNotificationDelegate;
        PTRARRAYREF arrDelegates;
        OBJECTREF   oInnerDelegate;
        OBJECTREF   oEventArgs;
        OBJECTREF   oCurrentThrowable;
        OBJECTREF   oCurAppDomain;
    } gc;
    gc.oNotificationDelegate = NULL;
    gc.arrDelegates = NULL;
    gc.oInnerDelegate = NULL;
    gc.oEventArgs = NULL;
    gc.oCurrentThrowable = NULL;
    gc.oCurAppDomain = NULL;

    // This will hold the MethodDesc of the callback that will be invoked.
    MethodDesc *pMDDelegate = NULL;

    GCPROTECT_BEGIN(gc);

    // Protect the throwable to be passed to the delegate callback
    gc.oCurrentThrowable = *pThrowable;

    // We expect a valid exception object
    _ASSERTE(IsException(gc.oCurrentThrowable->GetMethodTable()));

    // Save the reference to the current AppDomain. If the user code has
    // wired upto this event, then the managed AppDomain object will exist.
    gc.oCurAppDomain = pCurDomain->GetRawExposedObject();

    // Get the reference to the delegate based upon the type of notification
    if (notificationType == FirstChanceExceptionHandler)
    {
        gc.oNotificationDelegate = CoreLibBinder::GetField(FIELD__APPCONTEXT__FIRST_CHANCE_EXCEPTION)->GetStaticOBJECTREF();
    }
    else
    {
        gc.oNotificationDelegate = NULL;
        _ASSERTE(!"Invalid Exception Notification Handler specified!");
    }

    if (gc.oNotificationDelegate != NULL)
    {
        // Prevent any async exceptions from this moment on this thread
        ThreadPreventAsyncHolder prevAsync;

        gc.oEventArgs = NULL;

        // Get the arguments to be passed to the delegate callback. Incase of any
        // problem while allocating the event args, we will return a NULL.
        ExceptionNotifications::GetEventArgsForNotification(notificationType, &gc.oEventArgs,
            &gc.oCurrentThrowable);

        // Check if there are multiple callbacks registered? If there are, we will
        // loop through them, invoking each one at a time. Before invoking the target,
        // we will check if the target can be invoked based upon the corruption severity
        // for the active exception that was passed to us.
        gc.arrDelegates = (PTRARRAYREF) ((DELEGATEREF)(gc.oNotificationDelegate))->GetInvocationList();
        if (gc.arrDelegates == NULL || !gc.arrDelegates->GetMethodTable()->IsArray())
        {
            ExceptionNotifications::DeliverExceptionNotification(notificationType, &gc.oNotificationDelegate, &gc.oCurAppDomain, &gc.oEventArgs);
        }
        else
        {
            // The _invocationCount could be less than the array size, if we are sharing
            // immutable arrays cleverly.
            UINT_PTR      cnt = ((DELEGATEREF)(gc.oNotificationDelegate))->GetInvocationCount();
            _ASSERTE(cnt <= gc.arrDelegates->GetNumComponents());

            for (UINT_PTR i=0; i<cnt; i++)
            {
                gc.oInnerDelegate = gc.arrDelegates->m_Array[i];
                ExceptionNotifications::DeliverExceptionNotification(notificationType, &gc.oInnerDelegate, &gc.oCurAppDomain, &gc.oEventArgs);
            }
        }
    }

    GCPROTECT_END();
}

void ExceptionNotifications::DeliverFirstChanceNotification()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    // We check for FirstChance notification delivery after setting up the corruption severity
    // so that we can determine if the callback delegate can handle CSE (or not).
    //
    // Deliver it only if not already done and someone has wiredup to receive it.
    //
    // We do this provided this is the first frame of a new exception
    // that was thrown or a rethrown exception. We dont want to do this
    // processing for subsequent frames on the stack since FirstChance notification
    // will be delivered only when the exception is first thrown/rethrown.
    ThreadExceptionState *pCurTES = GetThread()->GetExceptionState();
    _ASSERTE(pCurTES->GetCurrentExceptionTracker());
    _ASSERTE(!(pCurTES->GetCurrentExceptionTracker()->DeliveredFirstChanceNotification()));
    {
        GCX_COOP();
        if (ExceptionNotifications::CanDeliverNotificationToCurrentAppDomain(FirstChanceExceptionHandler))
        {
            OBJECTREF oThrowable = NULL;
            GCPROTECT_BEGIN(oThrowable);

            oThrowable = pCurTES->GetThrowable();
            _ASSERTE(oThrowable != NULL);

            ExceptionNotifications::DeliverNotification(FirstChanceExceptionHandler, &oThrowable);
            GCPROTECT_END();

        }

        // Mark the exception tracker as having delivered the first chance notification
        pCurTES->GetCurrentExceptionTracker()->SetFirstChanceNotificationStatus(TRUE);
    }
}


#ifdef FEATURE_EH_FUNCLETS
struct TAResetStateCallbackData
{
    // Do we have more managed code up the stack?
    BOOL fDoWeHaveMoreManagedCodeOnStack;

    // StackFrame representing the crawlFrame above which
    // we are searching for presence of managed code.
    StackFrame sfSeedCrawlFrame;
};

// This callback helps the 64bit EH attempt to determine if there is more managed code
// up the stack (or not). Currently, it is used to conditionally reset the thread abort state
// as the unwind passes by.
StackWalkAction TAResetStateCallback(CrawlFrame* pCf, void* data)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    TAResetStateCallbackData *pTAResetStateCallbackData = static_cast<TAResetStateCallbackData *>(data);
    StackWalkAction retStatus = SWA_CONTINUE;

    if(pCf->IsFrameless())
    {
        IJitManager* pJitManager = pCf->GetJitManager();
        _ASSERTE(pJitManager);
        if (pJitManager && (!pTAResetStateCallbackData->fDoWeHaveMoreManagedCodeOnStack))
        {
            // The stackwalker can give us a callback for the seeding CrawlFrame (or other crawlframes)
            // depending upon which is closer to the leaf: the seeding crawlframe or the explicit frame
            // specified when starting the stackwalk.
            //
            // Since we are interested in checking if there is more managed code up the stack from
            // the seeding crawlframe, we check if the current crawlframe is above it or not. If it is,
            // then we have found managed code up the stack and should stop the stack walk. Otherwise,
            // continue searching.
            StackFrame sfCurrentFrame = StackFrame::FromRegDisplay(pCf->GetRegisterSet());
            if (pTAResetStateCallbackData->sfSeedCrawlFrame < sfCurrentFrame)
            {
                // We have found managed code on the stack. Flag it and stop the stackwalk.
                pTAResetStateCallbackData->fDoWeHaveMoreManagedCodeOnStack = TRUE;
                retStatus = SWA_ABORT;
            }
        }
    }

    return retStatus;
}
#endif // FEATURE_EH_FUNCLETS

// This function will reset the thread abort state against the specified thread if it is determined that
// there is no more managed code on the stack.
//
// Note: This function should be invoked ONLY during unwind.
#ifndef FEATURE_EH_FUNCLETS
void ResetThreadAbortState(PTR_Thread pThread, void *pEstablisherFrame)
#else
void ResetThreadAbortState(PTR_Thread pThread, CrawlFrame *pCf, StackFrame sfCurrentStackFrame)
#endif
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        PRECONDITION(pThread != NULL);
#ifndef FEATURE_EH_FUNCLETS
        PRECONDITION(pEstablisherFrame != NULL);
#else
        PRECONDITION(pCf != NULL);
        PRECONDITION(!sfCurrentStackFrame.IsNull());
#endif
    }
    CONTRACTL_END;

    if (pThread->IsAbortRequested())
    {
#ifndef FEATURE_EH_FUNCLETS
        if (GetNextCOMPlusSEHRecord(static_cast<EXCEPTION_REGISTRATION_RECORD *>(pEstablisherFrame)) == EXCEPTION_CHAIN_END)
        {
            _ASSERTE(!"Topmost handler and abort requested.");
        }
#else // !FEATURE_EH_FUNCLETS
        // Get the active exception tracker
        PTR_ExceptionTracker pCurEHTracker = (PTR_ExceptionTracker)pThread->GetExceptionState()->GetCurrentExceptionTracker();
        _ASSERTE(pCurEHTracker != NULL);

        // We will check if thread abort state needs to be reset only for the case of exception caught in
        // native code. This will happen when:
        //
        // 1) an unwind is triggered and
        // 2) current frame is the topmost frame we saw in the first pass and
        // 3) a thread abort is requested and
        // 4) we dont have address of the exception handler to be invoked.
        //
        // (1), (2) and (4) above are checked for in ExceptionTracker::ProcessOSExceptionNotification from where we call this
        // function.

        // Current frame should be the topmost frame we saw in the first pass
        _ASSERTE(pCurEHTracker->GetTopmostStackFrameFromFirstPass() == sfCurrentStackFrame);

        // If the exception has been caught in native code, then alongwith not having address of the handler to be
        // invoked, we also wont have the IL clause for the catch block and resume stack frame will be NULL as well.
        _ASSERTE((pCurEHTracker->GetCatchToCallPC() == NULL) &&
            (pCurEHTracker->GetCatchHandlerExceptionClauseToken() == NULL) &&
                 (pCurEHTracker->GetResumeStackFrame().IsNull()));

        // Walk the frame chain to see if there is any more managed code on the stack. If not, then this is the last managed frame
        // on the stack and we can reset the thread abort state.
        //
        // Get the frame from which to start the stack walk from
        Frame*  pFrame = pCurEHTracker->GetLimitFrame();

        // At this point, we are at the topmost frame we saw during the first pass
        // before the unwind began. Walk the stack using the specified crawlframe and the topmost
        // explicit frame to determine if we have more managed code up the stack. If none is found,
        // we can reset the thread abort state.

        // Setup the data structure to be passed to the callback
        TAResetStateCallbackData dataCallback;
        dataCallback.fDoWeHaveMoreManagedCodeOnStack = FALSE;

        // At this point, the StackFrame in CrawlFrame should represent the current frame we have been called for.
        // _ASSERTE(sfCurrentStackFrame == StackFrame::FromRegDisplay(pCf->GetRegisterSet()));

        // Reference to the StackFrame beyond which we are looking for managed code.
        dataCallback.sfSeedCrawlFrame = sfCurrentStackFrame;

        pThread->StackWalkFramesEx(pCf->GetRegisterSet(), TAResetStateCallback, &dataCallback, QUICKUNWIND, pFrame);

        if (!dataCallback.fDoWeHaveMoreManagedCodeOnStack)
        {
            _ASSERTE(!"There is no more managed code on the stack, and thread abort is requested.");
        }
#endif // !FEATURE_EH_FUNCLETS
    }
}
#endif // !DACCESS_COMPILE


//---------------------------------------------------------------------------------
//
//
// EXCEPTION THROWING HELPERS
//
//
//---------------------------------------------------------------------------------

//---------------------------------------------------------------------------------
// Funnel-worker for THROW_BAD_FORMAT and friends.
//
// Note: The "cond" argument is there to tide us over during the transition from
//  BAD_FORMAT_ASSERT to THROW_BAD_FORMAT. It will go away soon.
//---------------------------------------------------------------------------------
VOID ThrowBadFormatWorker(UINT resID, LPCWSTR imageName DEBUGARG(_In_z_ const char *cond))
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        INJECT_FAULT(COMPlusThrowOM(););
        SUPPORTS_DAC;
    }
    CONTRACTL_END

#ifndef DACCESS_COMPILE
    SString msgStr;

    SString resStr;
    if (resID == 0 || !resStr.LoadResource(CCompRC::Optional, resID))
    {
        resStr.LoadResource(CCompRC::Error, BFA_BAD_IL); // "Bad IL format."
    }
    msgStr.Append(resStr);

    if ((imageName != NULL) && (imageName[0] != 0))
    {
        SString suffixResStr;
        if (suffixResStr.LoadResource(CCompRC::Optional, COR_E_BADIMAGEFORMAT)) // "The format of the file '%1' is invalid."
        {
            SString suffixMsgStr;
            suffixMsgStr.FormatMessage(FORMAT_MESSAGE_FROM_STRING, (LPCWSTR)suffixResStr, 0, 0, SString{ SString::Literal, imageName });
            msgStr.Append(W(" "));
            msgStr.Append(suffixMsgStr);
        }
    }

#ifdef _DEBUG
    if (0 != strcmp(cond, "FALSE"))
    {
        msgStr.Append(W(" (Failed condition: ")); // this is in DEBUG only - not going to localize it.
        SString condStr(SString::Ascii, cond);
        msgStr.Append(condStr);
        msgStr.Append(W(")"));
    }
#endif
    ThrowHR(COR_E_BADIMAGEFORMAT, msgStr);
#else // DACCESS_COMPILE
    ThrowHR(COR_E_BADIMAGEFORMAT);
#endif // #ifndef DACCESS_COMPILE
}

UINT GetResourceIDForFileLoadExceptionHR(HRESULT hr)
{
    switch (hr) {

    case CTL_E_FILENOTFOUND:
        hr = IDS_EE_FILE_NOT_FOUND;
        break;

    case (HRESULT)IDS_EE_PROC_NOT_FOUND:
    case (HRESULT)IDS_EE_PATH_TOO_LONG:
    case INET_E_OBJECT_NOT_FOUND:
    case INET_E_DATA_NOT_AVAILABLE:
    case INET_E_DOWNLOAD_FAILURE:
    case INET_E_UNKNOWN_PROTOCOL:
    case (HRESULT)IDS_INET_E_SECURITY_PROBLEM:
    case (HRESULT)IDS_EE_BAD_USER_PROFILE:
    case (HRESULT)IDS_EE_ALREADY_EXISTS:
    case IDS_CLASSLOAD_32BITCLRLOADING64BITASSEMBLY:
       break;

    case MK_E_SYNTAX:
        hr = FUSION_E_INVALID_NAME;
        break;

    case INET_E_CONNECTION_TIMEOUT:
        hr = IDS_INET_E_CONNECTION_TIMEOUT;
        break;

    case INET_E_CANNOT_CONNECT:
        hr = IDS_INET_E_CANNOT_CONNECT;
        break;

    case INET_E_RESOURCE_NOT_FOUND:
        hr = IDS_INET_E_RESOURCE_NOT_FOUND;
        break;

    case NTE_BAD_HASH:
    case NTE_BAD_LEN:
    case NTE_BAD_KEY:
    case NTE_BAD_DATA:
    case NTE_BAD_ALGID:
    case NTE_BAD_FLAGS:
    case NTE_BAD_HASH_STATE:
    case NTE_BAD_UID:
    case NTE_FAIL:
    case NTE_BAD_TYPE:
    case NTE_BAD_VER:
    case NTE_BAD_SIGNATURE:
    case NTE_SIGNATURE_FILE_BAD:
    case CRYPT_E_HASH_VALUE:
        hr = IDS_EE_HASH_VAL_FAILED;
        break;

    default:
        hr = IDS_EE_FILELOAD_ERROR_GENERIC;
        break;

    }

    return (UINT) hr;
}

#ifndef DACCESS_COMPILE

//==========================================================================
// Throw a runtime exception based on the last Win32 error (GetLastError())
//==========================================================================
VOID DECLSPEC_NORETURN RealCOMPlusThrowWin32()
{

// before we do anything else...
    DWORD   err = ::GetLastError();

    CONTRACTL
    {
        THROWS;
        DISABLED(GC_NOTRIGGER);  // Must sanitize first pass handling to enable this
        MODE_ANY;
    }
    CONTRACTL_END;

    RealCOMPlusThrowWin32(HRESULT_FROM_WIN32(err));
} // VOID DECLSPEC_NORETURN RealCOMPlusThrowWin32()

//==========================================================================
// Throw a runtime exception based on the last Win32 error (GetLastError())
//==========================================================================
VOID DECLSPEC_NORETURN RealCOMPlusThrowWin32(HRESULT hr)
{
    CONTRACTL
    {
        THROWS;
        DISABLED(GC_NOTRIGGER);  // Must sanitize first pass handling to enable this
        MODE_ANY;
}
    CONTRACTL_END;

    // Force to ApplicationException for compatibility with previous versions.  We would
    //  prefer a "Win32Exception" here.
    EX_THROW(EEMessageException, (kApplicationException, hr, 0 /* resid*/,
                                 NULL /* szArg1 */, NULL /* szArg2 */, NULL /* szArg3 */, NULL /* szArg4 */,
                                 NULL /* szArg5 */, NULL /* szArg6 */));
} // VOID DECLSPEC_NORETURN RealCOMPlusThrowWin32()


//==========================================================================
// Throw an OutOfMemoryError
//==========================================================================
VOID DECLSPEC_NORETURN RealCOMPlusThrowOM()
{
    CONTRACTL
    {
        THROWS;
        DISABLED(GC_NOTRIGGER);  // Must sanitize first pass handling to enable this
        CANNOT_TAKE_LOCK;
        MODE_ANY;
        SUPPORTS_DAC;
    }
    CONTRACTL_END;

    ThrowOutOfMemory();
}

//==========================================================================
// Throw an undecorated runtime exception.
//==========================================================================
VOID DECLSPEC_NORETURN RealCOMPlusThrow(RuntimeExceptionKind reKind)
{
    CONTRACTL
    {
        THROWS;
        DISABLED(GC_NOTRIGGER);  // Must sanitize first pass handling to enable this
        MODE_ANY;
    }
    CONTRACTL_END;

    _ASSERTE((reKind != kExecutionEngineException) ||
             !"ExecutionEngineException shouldn't be thrown. Use EEPolicy to failfast or a better exception. The caller of this function should modify their code.");

    EX_THROW(EEException, (reKind));
}

//==========================================================================
// Throw a decorated runtime exception.
// Try using RealCOMPlusThrow(reKind, wszResourceName) instead.
//==========================================================================
VOID DECLSPEC_NORETURN RealCOMPlusThrowNonLocalized(RuntimeExceptionKind reKind, LPCWSTR wszTag)
{
    CONTRACTL
    {
        THROWS;
        DISABLED(GC_NOTRIGGER);  // Must sanitize first pass handling to enable this
        MODE_ANY;
    }
    CONTRACTL_END;

    _ASSERTE((reKind != kExecutionEngineException) ||
             !"ExecutionEngineException shouldn't be thrown. Use EEPolicy to failfast or a better exception. The caller of this function should modify their code.");

    EX_THROW(EEMessageException, (reKind, IDS_EE_GENERIC, wszTag));
}

//==========================================================================
// Throw a runtime exception based on an HResult
//==========================================================================
VOID DECLSPEC_NORETURN RealCOMPlusThrowHR(HRESULT hr, IErrorInfo* pErrInfo, Exception * pInnerException)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;        // because of IErrorInfo
        MODE_ANY;
    }
    CONTRACTL_END;

    _ASSERTE (FAILED(hr));

    // Though we would like to assert this, it can happen in the following scenario:
    //
    // MgdCode --RCW-> COM --CCW-> MgdCode2
    //
    // If MgdCode2 throws EEE, when it reaches the RCW, it will invoking MarshalNative::ThrowExceptionForHr and thus,
    // reach here. Hence, we will need to keep the assert off, until user code is stopped for creating an EEE.

    //_ASSERTE((hr != COR_E_EXECUTIONENGINE) ||
    //         !"ExecutionEngineException shouldn't be thrown. Use EEPolicy to failfast or a better exception. The caller of this function should modify their code.");

#ifdef FEATURE_COMINTEROP
    // check for complus created IErrorInfo pointers
    if (pErrInfo != NULL)
    {
        GCX_COOP();
        {
            OBJECTREF oRetVal = NULL;
            GCPROTECT_BEGIN(oRetVal);
            GetExceptionForHR(hr, pErrInfo, &oRetVal);
            _ASSERTE(oRetVal != NULL);
            RealCOMPlusThrow(oRetVal);
            GCPROTECT_END ();
        }
    }
#endif // FEATURE_COMINTEROP

    _ASSERTE((pErrInfo == NULL) || !"pErrInfo should always be null when FEATURE_COMINTEROP is disabled.");
    if (pInnerException == NULL)
    {
        EX_THROW(EEMessageException, (hr));
    }
    else
    {
        EX_THROW_WITH_INNER(EEMessageException, (hr), pInnerException);
    }
}

VOID DECLSPEC_NORETURN RealCOMPlusThrowHR(HRESULT hr)
{
    CONTRACTL
    {
        THROWS;
        DISABLED(GC_NOTRIGGER);  // Must sanitize first pass handling to enable this
        MODE_ANY;
    }
    CONTRACTL_END;


    // ! COMPlusThrowHR(hr) no longer snags the IErrorInfo off the TLS (Too many places
    // ! call this routine where no IErrorInfo was set by the prior call.)
    // !
    // ! If you actually want to pull IErrorInfo off the TLS, call
    // !
    // ! COMPlusThrowHR(hr, kGetErrorInfo)

    RealCOMPlusThrowHR(hr, (IErrorInfo*)NULL);
}


#ifdef FEATURE_COMINTEROP
VOID DECLSPEC_NORETURN RealCOMPlusThrowHR(HRESULT hr, tagGetErrorInfo)
{
    CONTRACTL
    {
        THROWS;
        DISABLED(GC_NOTRIGGER);  // Must sanitize first pass handling to enable this
        MODE_ANY;
    }
    CONTRACTL_END;

    // Get an IErrorInfo if one is available.
    IErrorInfo *pErrInfo = NULL;

    if (SafeGetErrorInfo(&pErrInfo) != S_OK)
        pErrInfo = NULL;

    // Throw the exception.
    RealCOMPlusThrowHR(hr, pErrInfo);
}
#endif // FEATURE_COMINTEROP



VOID DECLSPEC_NORETURN RealCOMPlusThrowHR(HRESULT hr, UINT resID, LPCWSTR wszArg1,
                                          LPCWSTR wszArg2, LPCWSTR wszArg3, LPCWSTR wszArg4,
                                          LPCWSTR wszArg5, LPCWSTR wszArg6)
{
    CONTRACTL
    {
        THROWS;
        DISABLED(GC_NOTRIGGER);  // Must sanitize first pass handling to enable this
        MODE_ANY;
    }
    CONTRACTL_END;

    _ASSERTE (FAILED(hr));

    // Though we would like to assert this, it can happen in the following scenario:
    //
    // MgdCode --RCW-> COM --CCW-> MgdCode2
    //
    // If MgdCode2 throws EEE, when it reaches the RCW, it will invoking MarshalNative::ThrowExceptionForHr and thus,
    // reach here. Hence, we will need to keep the assert off, until user code is stopped for creating an EEE.

    //_ASSERTE((hr != COR_E_EXECUTIONENGINE) ||
    //         !"ExecutionEngineException shouldn't be thrown. Use EEPolicy to failfast or a better exception. The caller of this function should modify their code.");

    EX_THROW(EEMessageException,
        (hr, resID, wszArg1, wszArg2, wszArg3, wszArg4, wszArg5, wszArg6));
}

//==========================================================================
// Throw a decorated runtime exception with a localized message.
// Queries the ResourceManager for a corresponding resource value.
//==========================================================================
VOID DECLSPEC_NORETURN RealCOMPlusThrow(RuntimeExceptionKind reKind, LPCWSTR wszResourceName, Exception * pInnerException)
{
    CONTRACTL
    {
        THROWS;
        DISABLED(GC_NOTRIGGER);  // Must sanitize first pass handling to enable this
        MODE_ANY;
        PRECONDITION(CheckPointer(wszResourceName));
    }
    CONTRACTL_END;

    _ASSERTE((reKind != kExecutionEngineException) ||
             !"ExecutionEngineException shouldn't be thrown. Use EEPolicy to failfast or a better exception. The caller of this function should modify their code.");
    //
    // For some reason, the compiler complains about unreachable code if
    // we don't split the new from the throw.  So we're left with this
    // unnecessarily verbose syntax.
    //

    if (pInnerException == NULL)
    {
        EX_THROW(EEResourceException, (reKind, wszResourceName));
    }
    else
    {
        EX_THROW_WITH_INNER(EEResourceException, (reKind, wszResourceName), pInnerException);
    }
}

//==========================================================================
// Used by the classloader to record a managed exception object to explain
// why a classload got botched.
//
// - Can be called with gc enabled or disabled.
//   This allows a catch-all error path to post a generic catchall error
//   message w/out overwriting more specific error messages posted by inner functions.
//==========================================================================
VOID DECLSPEC_NORETURN ThrowTypeLoadException(LPCWSTR pFullTypeName,
                                              LPCWSTR pAssemblyName,
                                              LPCUTF8 pMessageArg,
                                              UINT resIDWhy)
{
    CONTRACTL
    {
        THROWS;
        DISABLED(GC_NOTRIGGER);  // Must sanitize first pass handling to enable this
        MODE_ANY;
    }
    CONTRACTL_END;

    EX_THROW(EETypeLoadException, (pFullTypeName, pAssemblyName, pMessageArg, resIDWhy));
}


//==========================================================================
// Used by the classloader to post illegal layout
//==========================================================================
VOID DECLSPEC_NORETURN ThrowFieldLayoutError(mdTypeDef cl,                // cl of the NStruct being loaded
                           Module* pModule,             // Module that defines the scope, loader and heap (for allocated NativeFieldDescriptors)
                           DWORD   dwOffset,            // Offset of field
                           DWORD   dwID)                // Message id
{
    CONTRACTL
    {
        THROWS;
        DISABLED(GC_NOTRIGGER);  // Must sanitize first pass handling to enable this
        MODE_ANY;
    }
    CONTRACTL_END;

    IMDInternalImport *pInternalImport = pModule->GetMDImport();    // Internal interface for the NStruct being loaded.

    LPCUTF8 pszName, pszNamespace;
    if (FAILED(pInternalImport->GetNameOfTypeDef(cl, &pszName, &pszNamespace)))
    {
        pszName = pszNamespace = "Invalid TypeDef record";
    }

    CHAR offsetBuf[16];
    sprintf_s(offsetBuf, ARRAY_SIZE(offsetBuf), "%d", dwOffset);
    offsetBuf[ARRAY_SIZE(offsetBuf) - 1] = '\0';

    pModule->GetAssembly()->ThrowTypeLoadException(pszNamespace,
                                                   pszName,
                                                   offsetBuf,
                                                   dwID);
}

//==========================================================================
// Throw an ArithmeticException
//==========================================================================
VOID DECLSPEC_NORETURN RealCOMPlusThrowArithmetic()
{
    CONTRACTL
    {
        THROWS;
        DISABLED(GC_NOTRIGGER);  // Must sanitize first pass handling to enable this
        MODE_ANY;
    }
    CONTRACTL_END;

    RealCOMPlusThrow(kArithmeticException);
}

//==========================================================================
// Throw an ArgumentNullException
//==========================================================================
VOID DECLSPEC_NORETURN RealCOMPlusThrowArgumentNull(LPCWSTR argName, LPCWSTR wszResourceName)
{
    CONTRACTL
    {
        THROWS;
        DISABLED(GC_NOTRIGGER);  // Must sanitize first pass handling to enable this
        MODE_ANY;
        PRECONDITION(CheckPointer(wszResourceName));
    }
    CONTRACTL_END;

    EX_THROW(EEArgumentException, (kArgumentNullException, argName, wszResourceName));
}


VOID DECLSPEC_NORETURN RealCOMPlusThrowArgumentNull(LPCWSTR argName)
{
    CONTRACTL
    {
        THROWS;
        DISABLED(GC_NOTRIGGER);  // Must sanitize first pass handling to enable this
        MODE_ANY;
    }
    CONTRACTL_END;

    EX_THROW(EEArgumentException, (kArgumentNullException, argName, W("ArgumentNull_Generic")));
}


//==========================================================================
// Throw an ArgumentOutOfRangeException
//==========================================================================
VOID DECLSPEC_NORETURN RealCOMPlusThrowArgumentOutOfRange(LPCWSTR argName, LPCWSTR wszResourceName)
{
    CONTRACTL
    {
        THROWS;
        DISABLED(GC_NOTRIGGER);  // Must sanitize first pass handling to enable this
        MODE_ANY;
    }
    CONTRACTL_END;

    EX_THROW(EEArgumentException, (kArgumentOutOfRangeException, argName, wszResourceName));
}

//==========================================================================
// Throw an ArgumentException
//==========================================================================
VOID DECLSPEC_NORETURN RealCOMPlusThrowArgumentException(LPCWSTR argName, LPCWSTR wszResourceName)
{
    CONTRACTL
    {
        THROWS;
        DISABLED(GC_NOTRIGGER);  // Must sanitize first pass handling to enable this
        MODE_ANY;
    }
    CONTRACTL_END;

    EX_THROW(EEArgumentException, (kArgumentException, argName, wszResourceName));
}

//=========================================================================
// Used by the classloader to record a managed exception object to explain
// why a classload got botched.
//
// - Can be called with gc enabled or disabled.
//   This allows a catch-all error path to post a generic catchall error
//   message w/out overwriting more specific error messages posted by inner functions.
//==========================================================================
VOID DECLSPEC_NORETURN ThrowTypeLoadException(LPCUTF8 pszNameSpace,
                                              LPCUTF8 pTypeName,
                                              LPCWSTR pAssemblyName,
                                              LPCUTF8 pMessageArg,
                                              UINT resIDWhy)
{
    CONTRACTL
    {
        THROWS;
        DISABLED(GC_NOTRIGGER);  // Must sanitize first pass handling to enable this
        MODE_ANY;
    }
    CONTRACTL_END;

    EX_THROW(EETypeLoadException, (pszNameSpace, pTypeName, pAssemblyName, pMessageArg, resIDWhy));
}

//==========================================================================
// Throw a decorated runtime exception.
//==========================================================================
VOID DECLSPEC_NORETURN RealCOMPlusThrow(RuntimeExceptionKind  reKind, UINT resID,
                                        LPCWSTR wszArg1, LPCWSTR wszArg2, LPCWSTR wszArg3,
                                        LPCWSTR wszArg4, LPCWSTR wszArg5, LPCWSTR wszArg6)
{
    CONTRACTL
    {
        THROWS;
        DISABLED(GC_NOTRIGGER);  // Must sanitize first pass handling to enable this
        MODE_ANY;
    }
    CONTRACTL_END;

    EX_THROW(EEMessageException,
        (reKind, resID, wszArg1, wszArg2, wszArg3, wszArg4, wszArg5, wszArg6));
}

#ifdef FEATURE_COMINTEROP
//==========================================================================
// Throw a runtime exception based on an HResult, check for error info
//==========================================================================
VOID DECLSPEC_NORETURN RealCOMPlusThrowHR(HRESULT hr, IUnknown *iface, REFIID riid)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;         // because of IErrorInfo
        MODE_ANY;
    }
    CONTRACTL_END;

    IErrorInfo *info = NULL;
    {
        GCX_PREEMP();
        info = GetSupportedErrorInfo(iface, riid);
    }
    RealCOMPlusThrowHR(hr, info);
}

//==========================================================================
// Throw a runtime exception based on an EXCEPINFO. This function will free
// the strings in the EXCEPINFO that is passed in.
//==========================================================================
VOID DECLSPEC_NORETURN RealCOMPlusThrowHR(EXCEPINFO *pExcepInfo)
{
    CONTRACTL
    {
        THROWS;
        DISABLED(GC_NOTRIGGER);  // Must sanitize first pass handling to enable this
        MODE_ANY;
    }
    CONTRACTL_END;

    EX_THROW(EECOMException, (pExcepInfo));
}

#endif // FEATURE_COMINTEROP

//==========================================================================
// Throw an InvalidCastException
//==========================================================================

VOID GetAssemblyDetailInfo(SString    &sType,
                           SString    &sAssemblyDisplayName,
                           PEAssembly *pPEAssembly,
                           SString    &sAssemblyDetailInfo)
{
    WRAPPER_NO_CONTRACT;

    SString detailsUtf8;

    SString sAlcName;
    pPEAssembly->GetAssemblyBinder()->GetNameForDiagnostics(sAlcName);
    SString assemblyPath{ pPEAssembly->GetPath() };
    if (assemblyPath.IsEmpty())
    {
        detailsUtf8.Printf("Type %s originates from '%s' in the context '%s' in a byte array",
                                   sType.GetUTF8(),
                                   sAssemblyDisplayName.GetUTF8(),
                                   sAlcName.GetUTF8());
    }
    else
    {
        detailsUtf8.Printf("Type %s originates from '%s' in the context '%s' at location '%s'",
                                   sType.GetUTF8(),
                                   sAssemblyDisplayName.GetUTF8(),
                                   sAlcName.GetUTF8(),
                                   assemblyPath.GetUTF8());
    }

    sAssemblyDetailInfo.Append(detailsUtf8.GetUnicode());
}

VOID CheckAndThrowSameTypeAndAssemblyInvalidCastException(TypeHandle thCastFrom,
                                                          TypeHandle thCastTo)
{
     CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    } CONTRACTL_END;

     Module *pModuleTypeFrom = thCastFrom.GetModule();
     Module *pModuleTypeTo = thCastTo.GetModule();

     if ((pModuleTypeFrom != NULL) && (pModuleTypeTo != NULL))
     {
         Assembly *pAssemblyTypeFrom = pModuleTypeFrom->GetAssembly();
         Assembly *pAssemblyTypeTo = pModuleTypeTo->GetAssembly();

         _ASSERTE(pAssemblyTypeFrom != NULL);
         _ASSERTE(pAssemblyTypeTo != NULL);

         PEAssembly *pPEAssemblyTypeFrom = pAssemblyTypeFrom->GetPEAssembly();
         PEAssembly *pPEAssemblyTypeTo = pAssemblyTypeTo->GetPEAssembly();

         _ASSERTE(pPEAssemblyTypeFrom != NULL);
         _ASSERTE(pPEAssemblyTypeTo != NULL);

         StackSString sAssemblyFromDisplayName;
         StackSString sAssemblyToDisplayName;

         pPEAssemblyTypeFrom->GetDisplayName(sAssemblyFromDisplayName);
         pPEAssemblyTypeTo->GetDisplayName(sAssemblyToDisplayName);

         // Found the culprit case. Now format the new exception text.
         StackSString strCastFromName;
         StackSString strCastToName;
         StackSString sAssemblyDetailInfoFrom;
         StackSString sAssemblyDetailInfoTo;

         thCastFrom.GetName(strCastFromName);
         thCastTo.GetName(strCastToName);

         SString typeA = SL(W("A"));
         GetAssemblyDetailInfo(typeA,
                               sAssemblyFromDisplayName,
                               pPEAssemblyTypeFrom,
                               sAssemblyDetailInfoFrom);
         SString typeB = SL(W("B"));
         GetAssemblyDetailInfo(typeB,
                               sAssemblyToDisplayName,
                               pPEAssemblyTypeTo,
                               sAssemblyDetailInfoTo);

         COMPlusThrow(kInvalidCastException,
                      IDS_EE_CANNOTCASTSAME,
                      strCastFromName.GetUnicode(),
                      strCastToName.GetUnicode(),
                      sAssemblyDetailInfoFrom.GetUnicode(),
                      sAssemblyDetailInfoTo.GetUnicode());
     }
}

VOID RealCOMPlusThrowInvalidCastException(TypeHandle thCastFrom, TypeHandle thCastTo)
{
     CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    } CONTRACTL_END;

    // Use an InlineSString with a size of MAX_CLASSNAME_LENGTH + 1 to prevent
    // TypeHandle::GetName from having to allocate a new block of memory. This
    // significantly improves the performance of throwing an InvalidCastException.
    InlineSString<MAX_CLASSNAME_LENGTH + 1> strCastFromName;
    InlineSString<MAX_CLASSNAME_LENGTH + 1> strCastToName;

    thCastTo.GetName(strCastToName);
    {
        thCastFrom.GetName(strCastFromName);
        // Attempt to catch the A.T != A.T case that causes so much user confusion.
        if (strCastFromName.Equals(strCastToName))
        {
            CheckAndThrowSameTypeAndAssemblyInvalidCastException(thCastFrom, thCastTo);
        }
         COMPlusThrow(kInvalidCastException, IDS_EE_CANNOTCAST, strCastFromName.GetUnicode(), strCastToName.GetUnicode());
    }
}

VOID RealCOMPlusThrowInvalidCastException(OBJECTREF *pObj, TypeHandle thCastTo)
{
     CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(IsProtectedByGCFrame (pObj));
    } CONTRACTL_END;

    TypeHandle thCastFrom = (*pObj)->GetTypeHandle();
#ifdef FEATURE_COMINTEROP
    if (thCastFrom.GetMethodTable()->IsComObjectType())
    {
        // Special case casting RCWs so we can give better error information when the
        // cast fails.
        ComObject::ThrowInvalidCastException(pObj, thCastTo.GetMethodTable());
    }
#endif
    COMPlusThrowInvalidCastException(thCastFrom, thCastTo);
}

#endif // DACCESS_COMPILE

#ifdef FEATURE_COMINTEROP
#include "comtoclrcall.h"
#endif // FEATURE_COMINTEROP

// Reverse COM interop IL stubs need to catch all exceptions and translate them into HRESULTs.
// But we allow for CSEs to be rethrown.  Our corrupting state policy gets applied to the
// original user-visible method that triggered the IL stub to be generated.  So we must be able
// to map back from a given IL stub to the user-visible method.  Here, we do that only when we
// see a 'matching' ComMethodFrame further up the stack.
MethodDesc * GetUserMethodForILStub(Thread * pThread, UINT_PTR uStubSP, MethodDesc * pILStubMD, Frame ** ppFrameOut)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        PRECONDITION(pILStubMD->IsILStub());
    }
    CONTRACTL_END;

    MethodDesc * pUserMD = pILStubMD;
#ifdef FEATURE_COMINTEROP
    DynamicMethodDesc * pDMD = pILStubMD->AsDynamicMethodDesc();
    if (pDMD->IsCOMToCLRStub())
    {
        // There are some differences across architectures for "which" SP is passed in.
        // On ARM, the SP is the SP on entry to the IL stub, on the other arches, it's
        // a post-prolog SP.  But this doesn't matter here because the COM->CLR path
        // always pushes the Frame in a caller's stack frame.

        Frame * pCurFrame = pThread->GetFrame();
        while ((UINT_PTR)pCurFrame < uStubSP)
        {
            pCurFrame = pCurFrame->PtrNextFrame();
        }

        // The construction of the COM->CLR path ensures that our corresponding ComMethodFrame
        // should be present further up the stack. Normally, the ComMethodFrame in question is
        // simply the next stack frame; however, there are situations where there may be other
        // stack frames present (such as an inlined stack frame from a QCall in the IL stub).
        while (pCurFrame->GetVTablePtr() != ComMethodFrame::GetMethodFrameVPtr())
        {
            pCurFrame = pCurFrame->PtrNextFrame();
        }

        ComMethodFrame * pComFrame = (ComMethodFrame *)pCurFrame;
        _ASSERTE((UINT_PTR)pComFrame > uStubSP);

        CONSISTENCY_CHECK_MSG(pComFrame->GetVTablePtr() == ComMethodFrame::GetMethodFrameVPtr(),
                              "Expected to find a ComMethodFrame.");

        ComCallMethodDesc * pCMD = pComFrame->GetComCallMethodDesc();

        CONSISTENCY_CHECK_MSG(pILStubMD == ExecutionManager::GetCodeMethodDesc(pCMD->GetILStub()),
                              "The ComMethodFrame that we found doesn't match the IL stub passed in.");

        pUserMD = pCMD->GetMethodDesc();
        *ppFrameOut = pComFrame;
    }
#endif // FEATURE_COMINTEROP
    return pUserMD;
}
