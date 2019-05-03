// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

//

/*============================================================
**
** File:  COMUtilNative
**
**  
**
** Purpose: A dumping ground for classes which aren't large
** enough to get their own file in the EE.
**
**
**
===========================================================*/
#include "common.h"
#include "object.h"
#include "excep.h"
#include "vars.hpp"
#include "comutilnative.h"

#include "utilcode.h"
#include "frames.h"
#include "field.h"
#include "winwrap.h"
#include "gcheaputilities.h"
#include "fcall.h"
#include "invokeutil.h"
#include "eeconfig.h"
#include "typestring.h"
#include "sha1.h"
#include "finalizerthread.h"

#ifdef FEATURE_COMINTEROP
    #include "comcallablewrapper.h"
    #include "comcache.h"
#endif // FEATURE_COMINTEROP

#include "arraynative.inl"

#define STACK_OVERFLOW_MESSAGE   W("StackOverflowException")

/*===================================IsDigit====================================
**Returns a bool indicating whether the character passed in represents a   **
**digit.
==============================================================================*/
bool IsDigit(WCHAR c, int radix, int *result)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        PRECONDITION(CheckPointer(result));
    }
    CONTRACTL_END;

    if (IS_DIGIT(c)) {
        *result = DIGIT_TO_INT(c);
    }
    else if (c>='A' && c<='Z') {
        //+10 is necessary because A is actually 10, etc.
        *result = c-'A'+10;
    }
    else if (c>='a' && c<='z') {
        //+10 is necessary because a is actually 10, etc.
        *result = c-'a'+10;
    }
    else {
        *result = -1;
    }

    if ((*result >=0) && (*result < radix))
        return true;

    return false;
}

INT32 wtoi(__in_ecount(length) WCHAR* wstr, DWORD length)
{  
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        PRECONDITION(CheckPointer(wstr));
        PRECONDITION(length >= 0);
    }
    CONTRACTL_END;

    DWORD i = 0;
    int value;
    INT32 result = 0;

    while ( (i < length) && (IsDigit(wstr[i], 10 ,&value)) ) {
        //Read all of the digits and convert to a number
        result = result*10 + value;
        i++;
    }

    return result;
}



//
//
// EXCEPTION NATIVE
//
//
FCIMPL1(FC_BOOL_RET, ExceptionNative::IsImmutableAgileException, Object* pExceptionUNSAFE)
{
    FCALL_CONTRACT;

    ASSERT(pExceptionUNSAFE != NULL);

    OBJECTREF pException = (OBJECTREF) pExceptionUNSAFE;

    // The preallocated exception objects may be used from multiple AppDomains
    // and therefore must remain immutable from the application's perspective.
    FC_RETURN_BOOL(CLRException::IsPreallocatedExceptionObject(pException));
}
FCIMPLEND

FCIMPL1(FC_BOOL_RET, ExceptionNative::IsTransient, INT32 hresult)
{
    FCALL_CONTRACT;

    FC_RETURN_BOOL(Exception::IsTransient(hresult));
}
FCIMPLEND


// This FCall sets a flag against the thread exception state to indicate to
// IL_Throw and the StackTraceInfo implementation to account for the fact
// that we have restored a foreign exception dispatch details.
//
// Refer to the respective methods for details on how they use this flag.
FCIMPL0(VOID, ExceptionNative::PrepareForForeignExceptionRaise)
{
    FCALL_CONTRACT;

    PTR_ThreadExceptionState pCurTES = GetThread()->GetExceptionState();

	// Set a flag against the TES to indicate this is a foreign exception raise.
	pCurTES->SetRaisingForeignException();
}
FCIMPLEND

// Given an exception object, this method will extract the stacktrace and dynamic method array and set them up for return to the caller.
FCIMPL3(VOID, ExceptionNative::GetStackTracesDeepCopy, Object* pExceptionObjectUnsafe, Object **pStackTraceUnsafe, Object **pDynamicMethodsUnsafe);
{
    CONTRACTL
    {
        FCALL_CHECK;
    }
    CONTRACTL_END;

    ASSERT(pExceptionObjectUnsafe != NULL);
    ASSERT(pStackTraceUnsafe != NULL);
    ASSERT(pDynamicMethodsUnsafe != NULL);

    struct _gc
    {
        StackTraceArray stackTrace;
        StackTraceArray stackTraceCopy;
        EXCEPTIONREF refException;
        PTRARRAYREF dynamicMethodsArray; // Object array of Managed Resolvers
        PTRARRAYREF dynamicMethodsArrayCopy; // Copy of the object array of Managed Resolvers
    };
    _gc gc;
    ZeroMemory(&gc, sizeof(gc));

    // GC protect the array reference
    HELPER_METHOD_FRAME_BEGIN_PROTECT(gc);
    
    // Get the exception object reference
    gc.refException = (EXCEPTIONREF)(ObjectToOBJECTREF(pExceptionObjectUnsafe));

    // Fetch the stacktrace details from the exception under a lock
    gc.refException->GetStackTrace(gc.stackTrace, &gc.dynamicMethodsArray);
    
    bool fHaveStackTrace = false;
    bool fHaveDynamicMethodArray = false;

    if ((unsigned)gc.stackTrace.Size() > 0)
    {
        // Deepcopy the array
        gc.stackTraceCopy.CopyFrom(gc.stackTrace);
        fHaveStackTrace = true;
    }
    
    if (gc.dynamicMethodsArray != NULL)
    {
        // Get the number of elements in the dynamic methods array
        unsigned   cOrigDynamic = gc.dynamicMethodsArray->GetNumComponents();
    
        // ..and allocate a new array. This can trigger GC or throw under OOM.
        gc.dynamicMethodsArrayCopy = (PTRARRAYREF)AllocateObjectArray(cOrigDynamic, g_pObjectClass);
    
        // Deepcopy references to the new array we just allocated
        memmoveGCRefs(gc.dynamicMethodsArrayCopy->GetDataPtr(), gc.dynamicMethodsArray->GetDataPtr(),
                                                  cOrigDynamic * sizeof(Object *));

        fHaveDynamicMethodArray = true;
    }

    // Prep to return
    *pStackTraceUnsafe = fHaveStackTrace?OBJECTREFToObject(gc.stackTraceCopy.Get()):NULL;
    *pDynamicMethodsUnsafe = fHaveDynamicMethodArray?OBJECTREFToObject(gc.dynamicMethodsArrayCopy):NULL;

    HELPER_METHOD_FRAME_END();
}
FCIMPLEND

// Given an exception object and deep copied instances of a stacktrace and/or dynamic method array, this method will set the latter in the exception object instance.
FCIMPL3(VOID, ExceptionNative::SaveStackTracesFromDeepCopy, Object* pExceptionObjectUnsafe, Object *pStackTraceUnsafe, Object *pDynamicMethodsUnsafe);
{
    CONTRACTL
    {
        FCALL_CHECK;
    }
    CONTRACTL_END;

    ASSERT(pExceptionObjectUnsafe != NULL);

    struct _gc
    {
        StackTraceArray stackTrace;
        EXCEPTIONREF refException;
        PTRARRAYREF dynamicMethodsArray; // Object array of Managed Resolvers
    };
    _gc gc;
    ZeroMemory(&gc, sizeof(gc));

    // GC protect the array reference
    HELPER_METHOD_FRAME_BEGIN_PROTECT(gc);
    
    // Get the exception object reference
    gc.refException = (EXCEPTIONREF)(ObjectToOBJECTREF(pExceptionObjectUnsafe));

    if (pStackTraceUnsafe != NULL)
    {
        // Copy the stacktrace
        StackTraceArray stackTraceArray((I1ARRAYREF)ObjectToOBJECTREF(pStackTraceUnsafe));
        gc.stackTrace.Swap(stackTraceArray);
    }

    gc.dynamicMethodsArray = NULL;
    if (pDynamicMethodsUnsafe != NULL)
    {
        gc.dynamicMethodsArray = (PTRARRAYREF)ObjectToOBJECTREF(pDynamicMethodsUnsafe);
    }

    // If there is no stacktrace, then there cannot be any dynamic method array. Thus,
    // save stacktrace only when we have it.
    if (gc.stackTrace.Size() > 0)
    {
        // Save the stacktrace details in the exception under a lock
        gc.refException->SetStackTrace(gc.stackTrace, gc.dynamicMethodsArray);
    }
    else
    {
        gc.refException->SetNullStackTrace();
    }

    HELPER_METHOD_FRAME_END();
}
FCIMPLEND

// This method performs a deep copy of the stack trace array.
FCIMPL1(Object*, ExceptionNative::CopyStackTrace, Object* pStackTraceUNSAFE)
{
    FCALL_CONTRACT;

    ASSERT(pStackTraceUNSAFE != NULL);

    struct _gc
    {
        StackTraceArray stackTrace;
        StackTraceArray stackTraceCopy;
        _gc(I1ARRAYREF refStackTrace)
            : stackTrace(refStackTrace)
        {}
    };
    _gc gc((I1ARRAYREF)(ObjectToOBJECTREF(pStackTraceUNSAFE)));

    // GC protect the array reference
    HELPER_METHOD_FRAME_BEGIN_RET_PROTECT(gc);
        
    // Deepcopy the array
    gc.stackTraceCopy.CopyFrom(gc.stackTrace);
    
    HELPER_METHOD_FRAME_END();

    return OBJECTREFToObject(gc.stackTraceCopy.Get());
}
FCIMPLEND

// This method performs a deep copy of the dynamic method array.
FCIMPL1(Object*, ExceptionNative::CopyDynamicMethods, Object* pDynamicMethodsUNSAFE)
{
    FCALL_CONTRACT;

    ASSERT(pDynamicMethodsUNSAFE != NULL);

    struct _gc
    {
        PTRARRAYREF dynamicMethodsArray; // Object array of Managed Resolvers
        PTRARRAYREF dynamicMethodsArrayCopy; // Copy of the object array of Managed Resolvers
        _gc()
        {}
    };
    _gc gc;
    ZeroMemory(&gc, sizeof(gc));
    HELPER_METHOD_FRAME_BEGIN_RET_PROTECT(gc);
    
    gc.dynamicMethodsArray = (PTRARRAYREF)(ObjectToOBJECTREF(pDynamicMethodsUNSAFE));

    // Get the number of elements in the array
    unsigned   cOrigDynamic = gc.dynamicMethodsArray->GetNumComponents();
    // ..and allocate a new array. This can trigger GC or throw under OOM.
    gc.dynamicMethodsArrayCopy = (PTRARRAYREF)AllocateObjectArray(cOrigDynamic, g_pObjectClass);
    
    // Copy references to the new array we just allocated
    memmoveGCRefs(gc.dynamicMethodsArrayCopy->GetDataPtr(), gc.dynamicMethodsArray->GetDataPtr(),
                                              cOrigDynamic * sizeof(Object *));
    HELPER_METHOD_FRAME_END();

    return OBJECTREFToObject(gc.dynamicMethodsArrayCopy);
}
FCIMPLEND


BSTR BStrFromString(STRINGREF s)
{
    CONTRACTL
    {
        THROWS;
    }
    CONTRACTL_END;

    WCHAR *wz;
    int cch;
    BSTR bstr;

    if (s == NULL)
        return NULL;

    s->RefInterpretGetStringValuesDangerousForGC(&wz, &cch);

    bstr = SysAllocString(wz);
    if (bstr == NULL)
        COMPlusThrowOM();

    return bstr;
}

static BSTR GetExceptionDescription(OBJECTREF objException)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION( IsException(objException->GetMethodTable()) );
    }
    CONTRACTL_END;

    BSTR bstrDescription;

    STRINGREF MessageString = NULL;
    GCPROTECT_BEGIN(MessageString)
    GCPROTECT_BEGIN(objException)
    {
#ifdef FEATURE_APPX
        if (AppX::IsAppXProcess())
        {
            // In AppX, call Exception.ToString(false, false) which returns a string containing the exception class
            // name and callstack without file paths/names. This is used for unhandled exception bucketing/analysis.
            MethodDescCallSite getMessage(METHOD__EXCEPTION__TO_STRING, &objException);

            ARG_SLOT GetMessageArgs[] =
            {
                ObjToArgSlot(objException),
                BoolToArgSlot(false),  // needFileLineInfo
                BoolToArgSlot(false)   // needMessage
            };
            MessageString = getMessage.Call_RetSTRINGREF(GetMessageArgs);
        }
        else
#endif // FEATURE_APPX
        {
            // read Exception.Message property
            MethodDescCallSite getMessage(METHOD__EXCEPTION__GET_MESSAGE, &objException);

            ARG_SLOT GetMessageArgs[] = { ObjToArgSlot(objException)};
            MessageString = getMessage.Call_RetSTRINGREF(GetMessageArgs);

            // if the message string is empty then use the exception classname.
            if (MessageString == NULL || MessageString->GetStringLength() == 0) {
                // call GetClassName
                MethodDescCallSite getClassName(METHOD__EXCEPTION__GET_CLASS_NAME, &objException);
                ARG_SLOT GetClassNameArgs[] = { ObjToArgSlot(objException)};
                MessageString = getClassName.Call_RetSTRINGREF(GetClassNameArgs);
                _ASSERTE(MessageString != NULL && MessageString->GetStringLength() != 0);
            }
        }

        // Allocate the description BSTR.
        int DescriptionLen = MessageString->GetStringLength();
        bstrDescription = SysAllocStringLen(MessageString->GetBuffer(), DescriptionLen);
    }
    GCPROTECT_END();
    GCPROTECT_END();

    return bstrDescription;
}

static BSTR GetExceptionSource(OBJECTREF objException)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION( IsException(objException->GetMethodTable()) );
    }
    CONTRACTL_END;

    STRINGREF refRetVal;
    GCPROTECT_BEGIN(objException)

    // read Exception.Source property
    MethodDescCallSite getSource(METHOD__EXCEPTION__GET_SOURCE, &objException);

    ARG_SLOT GetSourceArgs[] = { ObjToArgSlot(objException)};

    refRetVal = getSource.Call_RetSTRINGREF(GetSourceArgs);

    GCPROTECT_END();
    return BStrFromString(refRetVal);
}

static void GetExceptionHelp(OBJECTREF objException, BSTR *pbstrHelpFile, DWORD *pdwHelpContext)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        INJECT_FAULT(COMPlusThrowOM());
        PRECONDITION(IsException(objException->GetMethodTable()));
        PRECONDITION(CheckPointer(pbstrHelpFile));
        PRECONDITION(CheckPointer(pdwHelpContext));
    }
    CONTRACTL_END;

    *pdwHelpContext = 0;

    GCPROTECT_BEGIN(objException);

    // read Exception.HelpLink property
    MethodDescCallSite getHelpLink(METHOD__EXCEPTION__GET_HELP_LINK, &objException);

    ARG_SLOT GetHelpLinkArgs[] = { ObjToArgSlot(objException)};
    *pbstrHelpFile = BStrFromString(getHelpLink.Call_RetSTRINGREF(GetHelpLinkArgs));

    GCPROTECT_END();

    // parse the help file to check for the presence of helpcontext
    int len = SysStringLen(*pbstrHelpFile);
    int pos = len;
    WCHAR *pwstr = *pbstrHelpFile;
    if (pwstr) {
        BOOL fFoundPound = FALSE;

        for (pos = len - 1; pos >= 0; pos--) {
            if (pwstr[pos] == W('#')) {
                fFoundPound = TRUE;
                break;
            }
        }

        if (fFoundPound) {
            int PoundPos = pos;
            int NumberStartPos = -1;
            BOOL bNumberStarted = FALSE;
            BOOL bNumberFinished = FALSE;
            BOOL bInvalidDigitsFound = FALSE;

            _ASSERTE(pwstr[pos] == W('#'));

            // Check to see if the string to the right of the pound a valid number.
            for (pos++; pos < len; pos++) {
                if (bNumberFinished) {
                    if (!COMCharacter::nativeIsWhiteSpace(pwstr[pos])) {
                        bInvalidDigitsFound = TRUE;
                        break;
                    }
                }
                else if (bNumberStarted) {
                    if (COMCharacter::nativeIsWhiteSpace(pwstr[pos])) {
                        bNumberFinished = TRUE;
                    }
                    else if (!COMCharacter::nativeIsDigit(pwstr[pos])) {
                        bInvalidDigitsFound = TRUE;
                        break;
                    }
                }
                else {
                    if (COMCharacter::nativeIsDigit(pwstr[pos])) {
                        NumberStartPos = pos;
                        bNumberStarted = TRUE;
                    }
                    else if (!COMCharacter::nativeIsWhiteSpace(pwstr[pos])) {
                        bInvalidDigitsFound = TRUE;
                        break;
                    }
                }
            }

            if (bNumberStarted && !bInvalidDigitsFound) {
                // Grab the help context and remove it from the help file.
                *pdwHelpContext = (DWORD)wtoi(&pwstr[NumberStartPos], len - NumberStartPos);

                // Allocate a new help file string of the right length.
                BSTR strOld = *pbstrHelpFile;
                *pbstrHelpFile = SysAllocStringLen(strOld, PoundPos);
                SysFreeString(strOld);
                if (!*pbstrHelpFile)
                    COMPlusThrowOM();
            }
        }
    }
}

// NOTE: caller cleans up any partially initialized BSTRs in pED
void ExceptionNative::GetExceptionData(OBJECTREF objException, ExceptionData *pED)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(IsException(objException->GetMethodTable()));
        PRECONDITION(CheckPointer(pED));
    }
    CONTRACTL_END;

    ZeroMemory(pED, sizeof(ExceptionData));

    if (objException->GetMethodTable() == g_pStackOverflowExceptionClass) {
        // In a low stack situation, most everything else in here will fail.
        // <TODO>@TODO: We're not turning the guard page back on here, yet.</TODO>
        pED->hr = COR_E_STACKOVERFLOW;
        pED->bstrDescription = SysAllocString(STACK_OVERFLOW_MESSAGE);
        return;
    }

    GCPROTECT_BEGIN(objException);
    pED->hr = GetExceptionHResult(objException);
    pED->bstrDescription = GetExceptionDescription(objException);
    pED->bstrSource = GetExceptionSource(objException);
    GetExceptionHelp(objException, &pED->bstrHelpFile, &pED->dwHelpContext);
    GCPROTECT_END();
    return;
}

#ifdef FEATURE_COMINTEROP

HRESULT SimpleComCallWrapper::IErrorInfo_hr()
{
    WRAPPER_NO_CONTRACT;
    return GetExceptionHResult(this->GetObjectRef());
}

BSTR SimpleComCallWrapper::IErrorInfo_bstrDescription()
{
    WRAPPER_NO_CONTRACT;
    return GetExceptionDescription(this->GetObjectRef());
}

BSTR SimpleComCallWrapper::IErrorInfo_bstrSource()
{
    WRAPPER_NO_CONTRACT;
    return GetExceptionSource(this->GetObjectRef());
}

BSTR SimpleComCallWrapper::IErrorInfo_bstrHelpFile()
{
    WRAPPER_NO_CONTRACT;
    BSTR  bstrHelpFile;
    DWORD dwHelpContext;
    GetExceptionHelp(this->GetObjectRef(), &bstrHelpFile, &dwHelpContext);
    return bstrHelpFile;
}

DWORD SimpleComCallWrapper::IErrorInfo_dwHelpContext()
{
    WRAPPER_NO_CONTRACT;
    BSTR  bstrHelpFile;
    DWORD dwHelpContext;
    GetExceptionHelp(this->GetObjectRef(), &bstrHelpFile, &dwHelpContext);
    SysFreeString(bstrHelpFile);
    return dwHelpContext;
}

GUID SimpleComCallWrapper::IErrorInfo_guid()
{
    LIMITED_METHOD_CONTRACT;
    return GUID_NULL;
}

#endif // FEATURE_COMINTEROP

FCIMPL0(EXCEPTION_POINTERS*, ExceptionNative::GetExceptionPointers)
{
    FCALL_CONTRACT;

    EXCEPTION_POINTERS* retVal = NULL;

    Thread *pThread = GetThread();
    _ASSERTE(pThread);

    if (pThread->IsExceptionInProgress())
    {
        retVal = pThread->GetExceptionState()->GetExceptionPointers();
    }

    return retVal;
}
FCIMPLEND

FCIMPL0(INT32, ExceptionNative::GetExceptionCode)
{
    FCALL_CONTRACT;

    INT32 retVal = 0;

    Thread *pThread = GetThread();
    _ASSERTE(pThread);

    if (pThread->IsExceptionInProgress())
    {
        retVal = pThread->GetExceptionState()->GetExceptionCode();
    }

    return retVal;
}
FCIMPLEND

extern uint32_t g_exceptionCount;
FCIMPL0(UINT32, ExceptionNative::GetExceptionCount)
{
    FCALL_CONTRACT;
    return g_exceptionCount;
}
FCIMPLEND


//
// This must be implemented as an FCALL because managed code cannot
// swallow a thread abort exception without resetting the abort,
// which we don't want to do.  Additionally, we can run into deadlocks
// if we use the ResourceManager to do resource lookups - it requires
// taking managed locks when initializing Globalization & Security,
// but a thread abort on a separate thread initializing those same
// systems would also do a resource lookup via the ResourceManager.
// We've deadlocked in CompareInfo.GetCompareInfo &
// Environment.GetResourceString.  It's not practical to take all of
// our locks within CER's to avoid this problem - just use the CLR's
// unmanaged resources.
//
void QCALLTYPE ExceptionNative::GetMessageFromNativeResources(ExceptionMessageKind kind, QCall::StringHandleOnStack retMesg)
{
    QCALL_CONTRACT;

    BEGIN_QCALL;

    SString buffer;
    HRESULT hr = S_OK;
    const WCHAR * wszFallbackString = NULL;

    switch(kind) {
    case ThreadAbort:
        hr = buffer.LoadResourceAndReturnHR(CCompRC::Error, IDS_EE_THREAD_ABORT);
        if (FAILED(hr)) {
            wszFallbackString = W("Thread was being aborted.");
        }
        break;

    case ThreadInterrupted:
        hr = buffer.LoadResourceAndReturnHR(CCompRC::Error, IDS_EE_THREAD_INTERRUPTED);
        if (FAILED(hr)) {
            wszFallbackString = W("Thread was interrupted from a waiting state.");
        }
        break;

    case OutOfMemory:
        hr = buffer.LoadResourceAndReturnHR(CCompRC::Error, IDS_EE_OUT_OF_MEMORY);
        if (FAILED(hr)) {
            wszFallbackString = W("Insufficient memory to continue the execution of the program.");
        }
        break;

    default:
        _ASSERTE(!"Unknown ExceptionMessageKind value!");
    }
    if (FAILED(hr)) {       
        STRESS_LOG1(LF_BCL, LL_ALWAYS, "LoadResource error: %x", hr);
        _ASSERTE(wszFallbackString != NULL);
        retMesg.Set(wszFallbackString);
    }
    else {
        retMesg.Set(buffer);
    }

    END_QCALL;
}

// BlockCopy
// This method from one primitive array to another based
//  upon an offset into each an a byte count.
FCIMPL5(VOID, Buffer::BlockCopy, ArrayBase *src, int srcOffset, ArrayBase *dst, int dstOffset, int count)
{
    FCALL_CONTRACT;

    // Verify that both the src and dst are Arrays of primitive
    //  types.
    // <TODO>@TODO: We need to check for booleans</TODO>
    if (src==NULL || dst==NULL)
        FCThrowArgumentNullVoid((src==NULL) ? W("src") : W("dst"));

    SIZE_T srcLen, dstLen;

    //
    // Use specialized fast path for byte arrays because of it is what Buffer::BlockCopy is 
    // typically used for.
    //

    MethodTable * pByteArrayMT = g_pByteArrayMT;
    _ASSERTE(pByteArrayMT != NULL);
    
    // Optimization: If src is a byte array, we can
    // simply set srcLen to GetNumComponents, without having
    // to call GetComponentSize or verifying GetArrayElementType
    if (src->GetMethodTable() == pByteArrayMT)
    {
        srcLen = src->GetNumComponents();
    }
    else
    {
        srcLen = src->GetNumComponents() * src->GetComponentSize();

        // We only want to allow arrays of primitives, no Objects.
        const CorElementType srcET = src->GetArrayElementType();
        if (!CorTypeInfo::IsPrimitiveType_NoThrow(srcET))
            FCThrowArgumentVoid(W("src"), W("Arg_MustBePrimArray"));
    }
    
    // Optimization: If copying to/from the same array, then
    // we know that dstLen and srcLen must be the same.
    if (src == dst)
    {
        dstLen = srcLen;
    }
    else if (dst->GetMethodTable() == pByteArrayMT)
    {
        dstLen = dst->GetNumComponents();
    }
    else
    {
        dstLen = dst->GetNumComponents() * dst->GetComponentSize();
        if (dst->GetMethodTable() != src->GetMethodTable())
        {
            const CorElementType dstET = dst->GetArrayElementType();
            if (!CorTypeInfo::IsPrimitiveType_NoThrow(dstET))
                FCThrowArgumentVoid(W("dst"), W("Arg_MustBePrimArray"));
        }
    }

    if (srcOffset < 0 || dstOffset < 0 || count < 0) {
        const wchar_t* str = W("srcOffset");
        if (dstOffset < 0) str = W("dstOffset");
        if (count < 0) str = W("count");
        FCThrowArgumentOutOfRangeVoid(str, W("ArgumentOutOfRange_NeedNonNegNum"));
    }

    if (srcLen < (SIZE_T)srcOffset + (SIZE_T)count || dstLen < (SIZE_T)dstOffset + (SIZE_T)count) {
        FCThrowArgumentVoid(NULL, W("Argument_InvalidOffLen"));
    }
    
    PTR_BYTE srcPtr = src->GetDataPtr() + srcOffset;
    PTR_BYTE dstPtr = dst->GetDataPtr() + dstOffset;

    if ((srcPtr != dstPtr) && (count > 0)) {
#if defined(_AMD64_) && !defined(PLATFORM_UNIX)
        JIT_MemCpy(dstPtr, srcPtr, count);
#else
        memmove(dstPtr, srcPtr, count);
#endif
    }

    FC_GC_POLL();
}
FCIMPLEND


void QCALLTYPE MemoryNative::Clear(void *dst, size_t length)
{
    QCALL_CONTRACT;

#if defined(_X86_) || defined(_AMD64_)
    if (length > 0x100)
    {
        // memset ends up calling rep stosb if the hardware claims to support it efficiently. rep stosb is up to 2x slower
        // on misaligned blocks. Workaround this issue by aligning the blocks passed to memset upfront.

        *(uint64_t*)dst = 0;
        *((uint64_t*)dst + 1) = 0;
        *((uint64_t*)dst + 2) = 0;
        *((uint64_t*)dst + 3) = 0;

        void* end = (uint8_t*)dst + length;
        *((uint64_t*)end - 1) = 0;
        *((uint64_t*)end - 2) = 0;
        *((uint64_t*)end - 3) = 0;
        *((uint64_t*)end - 4) = 0;

        dst = ALIGN_UP((uint8_t*)dst + 1, 32);
        length = ALIGN_DOWN((uint8_t*)end - 1, 32) - (uint8_t*)dst;
    }
#endif

    memset(dst, 0, length);
}

FCIMPL3(VOID, MemoryNative::BulkMoveWithWriteBarrier, void *dst, void *src, size_t byteCount)
{
    FCALL_CONTRACT;

    InlinedMemmoveGCRefsHelper(dst, src, byteCount);

    FC_GC_POLL();
}
FCIMPLEND

void QCALLTYPE Buffer::MemMove(void *dst, void *src, size_t length)
{
    QCALL_CONTRACT;

#if !defined(FEATURE_CORESYSTEM)
    // Callers of memcpy do expect and handle access violations in some scenarios.
    // Access violations in the runtime dll are turned into fail fast by the vector exception handler by default.
    // We need to supress this behavior for CoreCLR using AVInRuntimeImplOkayHolder because of memcpy is statically linked in.
    AVInRuntimeImplOkayHolder avOk;
#endif

    memmove(dst, src, length);
}

// Returns a bool to indicate if the array is of primitive types or not.
FCIMPL1(FC_BOOL_RET, Buffer::IsPrimitiveTypeArray, ArrayBase *arrayUNSAFE)
{
    FCALL_CONTRACT;

    _ASSERTE(arrayUNSAFE != NULL);

    // Check the type from the contained element's handle
    TypeHandle elementTH = arrayUNSAFE->GetArrayElementTypeHandle();
    BOOL fIsPrimitiveTypeArray = CorTypeInfo::IsPrimitiveType_NoThrow(elementTH.GetVerifierCorElementType());

    FC_RETURN_BOOL(fIsPrimitiveTypeArray);

}
FCIMPLEND

// Returns the length in bytes of an array containing
// primitive type elements
FCIMPL1(INT32, Buffer::ByteLength, ArrayBase* arrayUNSAFE)
{
    FCALL_CONTRACT;

    _ASSERTE(arrayUNSAFE != NULL);

    SIZE_T iRetVal = arrayUNSAFE->GetNumComponents() * arrayUNSAFE->GetComponentSize();

    // This API is explosed both as Buffer.ByteLength and also used indirectly in argument
    // checks for Buffer.GetByte/SetByte.
    //
    // If somebody called Get/SetByte on 2GB+ arrays, there is a decent chance that 
    // the computation of the index has overflowed. Thus we intentionally always 
    // throw on 2GB+ arrays in Get/SetByte argument checks (even for indicies <2GB)
    // to prevent people from running into a trap silently.
    if (iRetVal > INT32_MAX)
        FCThrow(kOverflowException);

    return (INT32)iRetVal;
}
FCIMPLEND

//
// GCInterface
//
MethodDesc *GCInterface::m_pCacheMethod=NULL;

UINT64   GCInterface::m_ulMemPressure = 0;
UINT64   GCInterface::m_ulThreshold = MIN_GC_MEMORYPRESSURE_THRESHOLD;
INT32    GCInterface::m_gc_counts[3] = {0,0,0};
CrstStatic GCInterface::m_MemoryPressureLock;

UINT64   GCInterface::m_addPressure[NEW_PRESSURE_COUNT] = {0, 0, 0, 0};   // history of memory pressure additions
UINT64   GCInterface::m_remPressure[NEW_PRESSURE_COUNT] = {0, 0, 0, 0};   // history of memory pressure removals

// incremented after a gen2 GC has been detected,
// (m_iteration % NEW_PRESSURE_COUNT) is used as an index into m_addPressure and m_remPressure
UINT     GCInterface::m_iteration = 0;

FCIMPL5(void, GCInterface::GetMemoryInfo, UINT32* highMemLoadThreshold, UINT64* totalPhysicalMem, UINT32* lastRecordedMemLoad, size_t* lastRecordedHeapSize, size_t* lastRecordedFragmentation)
{
    FCALL_CONTRACT;

    FC_GC_POLL_NOT_NEEDED();
    
    return GCHeapUtilities::GetGCHeap()->GetMemoryInfo(highMemLoadThreshold, totalPhysicalMem, 
                                                       lastRecordedMemLoad, 
                                                       lastRecordedHeapSize, lastRecordedFragmentation);
}
FCIMPLEND

FCIMPL0(int, GCInterface::GetGcLatencyMode)
{
    FCALL_CONTRACT;

    FC_GC_POLL_NOT_NEEDED();

    int result = (INT32)GCHeapUtilities::GetGCHeap()->GetGcLatencyMode();
    return result;
}
FCIMPLEND

FCIMPL1(int, GCInterface::SetGcLatencyMode, int newLatencyMode)
{
    FCALL_CONTRACT;

    FC_GC_POLL_NOT_NEEDED();
    
    return GCHeapUtilities::GetGCHeap()->SetGcLatencyMode(newLatencyMode);
}
FCIMPLEND

FCIMPL0(int, GCInterface::GetLOHCompactionMode)
{
    FCALL_CONTRACT;

    FC_GC_POLL_NOT_NEEDED();

    int result = (INT32)GCHeapUtilities::GetGCHeap()->GetLOHCompactionMode();
    return result;
}
FCIMPLEND

FCIMPL1(void, GCInterface::SetLOHCompactionMode, int newLOHCompactionyMode)
{
    FCALL_CONTRACT;

    FC_GC_POLL_NOT_NEEDED();
    
    GCHeapUtilities::GetGCHeap()->SetLOHCompactionMode(newLOHCompactionyMode);
}
FCIMPLEND


FCIMPL2(FC_BOOL_RET, GCInterface::RegisterForFullGCNotification, UINT32 gen2Percentage, UINT32 lohPercentage)
{
    FCALL_CONTRACT;

    FC_GC_POLL_NOT_NEEDED();

    FC_RETURN_BOOL(GCHeapUtilities::GetGCHeap()->RegisterForFullGCNotification(gen2Percentage, lohPercentage));
}
FCIMPLEND

FCIMPL0(FC_BOOL_RET, GCInterface::CancelFullGCNotification)
{
    FCALL_CONTRACT;

    FC_GC_POLL_NOT_NEEDED();
    FC_RETURN_BOOL(GCHeapUtilities::GetGCHeap()->CancelFullGCNotification());
}
FCIMPLEND

FCIMPL1(int, GCInterface::WaitForFullGCApproach, int millisecondsTimeout)
{
    CONTRACTL
    {
        THROWS;
        MODE_COOPERATIVE;
        DISABLED(GC_TRIGGERS);  // can't use this in an FCALL because we're in forbid gc mode until we setup a H_M_F.
    }
    CONTRACTL_END;

    int result = 0; 

    //We don't need to check the top end because the GC will take care of that.
    HELPER_METHOD_FRAME_BEGIN_RET_0();

    DWORD dwMilliseconds = ((millisecondsTimeout == -1) ? INFINITE : millisecondsTimeout);
    result = GCHeapUtilities::GetGCHeap()->WaitForFullGCApproach(dwMilliseconds);

    HELPER_METHOD_FRAME_END();

    return result;
}
FCIMPLEND

FCIMPL1(int, GCInterface::WaitForFullGCComplete, int millisecondsTimeout)
{
    CONTRACTL
    {
        THROWS;
        MODE_COOPERATIVE;
        DISABLED(GC_TRIGGERS);  // can't use this in an FCALL because we're in forbid gc mode until we setup a H_M_F.
    }
    CONTRACTL_END;

    int result = 0; 

    //We don't need to check the top end because the GC will take care of that.
    HELPER_METHOD_FRAME_BEGIN_RET_0();

    DWORD dwMilliseconds = ((millisecondsTimeout == -1) ? INFINITE : millisecondsTimeout);
    result = GCHeapUtilities::GetGCHeap()->WaitForFullGCComplete(dwMilliseconds);

    HELPER_METHOD_FRAME_END();

    return result;
}
FCIMPLEND

/*================================GetGeneration=================================
**Action: Returns the generation in which args->obj is found.
**Returns: The generation in which args->obj is found.
**Arguments: args->obj -- The object to locate.
**Exceptions: ArgumentException if args->obj is null.
==============================================================================*/
FCIMPL1(int, GCInterface::GetGeneration, Object* objUNSAFE)
{
    FCALL_CONTRACT;

    if (objUNSAFE == NULL)
        FCThrowArgumentNull(W("obj"));

    int result = (INT32)GCHeapUtilities::GetGCHeap()->WhichGeneration(objUNSAFE);
    FC_GC_POLL_RET();
    return result;
}
FCIMPLEND

/*================================GetSegmentSize========-=======================
**Action: Returns the maximum GC heap segment size
**Returns: The maximum segment size of either the normal heap or the large object heap, whichever is bigger
==============================================================================*/
FCIMPL0(UINT64, GCInterface::GetSegmentSize)
{
    FCALL_CONTRACT;

    IGCHeap * pGC = GCHeapUtilities::GetGCHeap();
    size_t segment_size = pGC->GetValidSegmentSize(false);
    size_t large_segment_size = pGC->GetValidSegmentSize(true);
    _ASSERTE(segment_size < SIZE_T_MAX && large_segment_size < SIZE_T_MAX);
    if (segment_size < large_segment_size)
        segment_size = large_segment_size;

    FC_GC_POLL_RET();
    return (UINT64) segment_size;
}
FCIMPLEND

/*================================CollectionCount=================================
**Action: Returns the number of collections for this generation since the begining of the life of the process
**Returns: The collection count.
**Arguments: args->generation -- The generation
**Exceptions: Argument exception if args->generation is < 0 or > GetMaxGeneration();
==============================================================================*/
FCIMPL2(int, GCInterface::CollectionCount, INT32 generation, INT32 getSpecialGCCount)
{
    FCALL_CONTRACT;

    //We've already checked this in GC.cs, so we'll just assert it here.
    _ASSERTE(generation >= 0);

    //We don't need to check the top end because the GC will take care of that.
    int result = (INT32)GCHeapUtilities::GetGCHeap()->CollectionCount(generation, getSpecialGCCount);
    FC_GC_POLL_RET();
    return result;
}
FCIMPLEND

int QCALLTYPE GCInterface::StartNoGCRegion(INT64 totalSize, BOOL lohSizeKnown, INT64 lohSize, BOOL disallowFullBlockingGC)
{
    QCALL_CONTRACT;

    int retVal = 0;

    BEGIN_QCALL;

    GCX_COOP();

    retVal = GCHeapUtilities::GetGCHeap()->StartNoGCRegion((ULONGLONG)totalSize, 
                                                  !!lohSizeKnown,
                                                  (ULONGLONG)lohSize,
                                                  !!disallowFullBlockingGC);

    END_QCALL;

    return retVal;
}

int QCALLTYPE GCInterface::EndNoGCRegion()
{
    QCALL_CONTRACT;

    int retVal = FALSE;

    BEGIN_QCALL;

    retVal = GCHeapUtilities::GetGCHeap()->EndNoGCRegion();

    END_QCALL;

    return retVal;
}

/*===============================GetGenerationWR================================
**Action: Returns the generation in which the object pointed to by a WeakReference is found.
**Returns:
**Arguments: args->handle -- the OBJECTHANDLE to the object which we're locating.
**Exceptions: ArgumentException if handle points to an object which is not accessible.
==============================================================================*/
FCIMPL1(int, GCInterface::GetGenerationWR, LPVOID handle)
{
    FCALL_CONTRACT;

    int iRetVal = 0;

    HELPER_METHOD_FRAME_BEGIN_RET_0();

    OBJECTREF temp;
    temp = ObjectFromHandle((OBJECTHANDLE) handle);
    if (temp == NULL)
        COMPlusThrowArgumentNull(W("weak handle"));

    iRetVal = (INT32)GCHeapUtilities::GetGCHeap()->WhichGeneration(OBJECTREFToObject(temp));

    HELPER_METHOD_FRAME_END();

    return iRetVal;
}
FCIMPLEND

/*================================GetTotalMemory================================
**Action: Returns the total number of bytes in use
**Returns: The total number of bytes in use
**Arguments: None
**Exceptions: None
==============================================================================*/
INT64 QCALLTYPE GCInterface::GetTotalMemory()
{
    QCALL_CONTRACT;

    INT64 iRetVal = 0;

    BEGIN_QCALL;

    GCX_COOP();
    iRetVal = (INT64) GCHeapUtilities::GetGCHeap()->GetTotalBytesInUse();

    END_QCALL;

    return iRetVal;
}

/*==============================Collect=========================================
**Action: Collects all generations <= args->generation
**Returns: void
**Arguments: args->generation:  The maximum generation to collect
**Exceptions: Argument exception if args->generation is < 0 or > GetMaxGeneration();
==============================================================================*/
void QCALLTYPE GCInterface::Collect(INT32 generation, INT32 mode)
{
    QCALL_CONTRACT;

    BEGIN_QCALL;

    //We've already checked this in GC.cs, so we'll just assert it here.
    _ASSERTE(generation >= -1);

    //We don't need to check the top end because the GC will take care of that.

    GCX_COOP();
    GCHeapUtilities::GetGCHeap()->GarbageCollect(generation, false, mode);

    END_QCALL;
}


/*==========================WaitForPendingFinalizers============================
**Action: Run all Finalizers that haven't been run.
**Arguments: None
**Exceptions: None
==============================================================================*/
void QCALLTYPE GCInterface::WaitForPendingFinalizers()
{
    QCALL_CONTRACT;

    BEGIN_QCALL;

    FinalizerThread::FinalizerThreadWait();

    END_QCALL;
}


/*===============================GetMaxGeneration===============================
**Action: Returns the largest GC generation
**Returns: The largest GC Generation
**Arguments: None
**Exceptions: None
==============================================================================*/
FCIMPL0(int, GCInterface::GetMaxGeneration)
{
    FCALL_CONTRACT;

    return(INT32)GCHeapUtilities::GetGCHeap()->GetMaxGeneration();
}
FCIMPLEND

/*===============================GetAllocatedBytesForCurrentThread===============================
**Action: Computes the allocated bytes so far on the current thread
**Returns: The allocated bytes so far on the current thread
**Arguments: None
**Exceptions: None
==============================================================================*/
FCIMPL0(INT64, GCInterface::GetAllocatedBytesForCurrentThread)
{
    FCALL_CONTRACT;

    INT64 currentAllocated = 0;
    Thread *pThread = GetThread();
    gc_alloc_context* ac = pThread->GetAllocContext();
    currentAllocated = ac->alloc_bytes + ac->alloc_bytes_loh - (ac->alloc_limit - ac->alloc_ptr);

    return currentAllocated;
}
FCIMPLEND

/*===============================AllocateNewArray===============================
**Action: Allocates a new array object. Allows passing extra flags
**Returns: The allocated array.
**Arguments: elementTypeHandle -> type of the element, 
**           length -> number of elements, 
**           zeroingOptional -> whether caller prefers to skip clearing the content of the array, if possible.
**Exceptions: IDS_EE_ARRAY_DIMENSIONS_EXCEEDED when size is too large. OOM if can't allocate.
==============================================================================*/
FCIMPL3(Object*, GCInterface::AllocateNewArray, void* arrayTypeHandle, INT32 length, CLR_BOOL zeroingOptional)
{
    CONTRACTL {
        FCALL_CHECK;
        PRECONDITION(length >= 0);
    } CONTRACTL_END;

    OBJECTREF pRet = NULL;
    TypeHandle arrayType = TypeHandle::FromPtr(arrayTypeHandle);

    HELPER_METHOD_FRAME_BEGIN_RET_0();

    pRet = AllocateSzArray(arrayType, length, zeroingOptional ? GC_ALLOC_ZEROING_OPTIONAL : GC_ALLOC_NO_FLAGS);

    HELPER_METHOD_FRAME_END();

    return OBJECTREFToObject(pRet);
}
FCIMPLEND

#ifdef FEATURE_BASICFREEZE

/*===============================RegisterFrozenSegment===============================
**Action: Registers the frozen segment
**Returns: segment_handle
**Arguments: args-> pointer to section, size of section
**Exceptions: None
==============================================================================*/
void* QCALLTYPE GCInterface::RegisterFrozenSegment(void* pSection, INT32 sizeSection)
{
    QCALL_CONTRACT;

    void* retVal = nullptr;

    BEGIN_QCALL;

    _ASSERTE(pSection != nullptr);
    _ASSERTE(sizeSection > 0);

    GCX_COOP();

    segment_info seginfo;
    seginfo.pvMem           = pSection;
    seginfo.ibFirstObject   = sizeof(ObjHeader);
    seginfo.ibAllocated     = sizeSection;
    seginfo.ibCommit        = seginfo.ibAllocated;
    seginfo.ibReserved      = seginfo.ibAllocated;

    retVal = (void*)GCHeapUtilities::GetGCHeap()->RegisterFrozenSegment(&seginfo);

    END_QCALL;

    return retVal;
}

/*===============================UnregisterFrozenSegment===============================
**Action: Unregisters the frozen segment
**Returns: void
**Arguments: args-> segment handle
**Exceptions: None
==============================================================================*/
void QCALLTYPE GCInterface::UnregisterFrozenSegment(void* segment)
{
    QCALL_CONTRACT;

    BEGIN_QCALL;

    _ASSERTE(segment != nullptr);

    GCX_COOP();

    GCHeapUtilities::GetGCHeap()->UnregisterFrozenSegment((segment_handle)segment);

    END_QCALL;
}

#endif // FEATURE_BASICFREEZE

/*==============================SuppressFinalize================================
**Action: Indicate that an object's finalizer should not be run by the system
**Arguments: Object of interest
**Exceptions: None
==============================================================================*/
FCIMPL1(void, GCInterface::SuppressFinalize, Object *obj)
{
    FCALL_CONTRACT;

    // Checked by the caller
    _ASSERTE(obj != NULL);

    if (!obj->GetMethodTable ()->HasFinalizer())
        return;

    GCHeapUtilities::GetGCHeap()->SetFinalizationRun(obj);
    FC_GC_POLL();
}
FCIMPLEND


/*============================ReRegisterForFinalize==============================
**Action: Indicate that an object's finalizer should be run by the system.
**Arguments: Object of interest
**Exceptions: None
==============================================================================*/
FCIMPL1(void, GCInterface::ReRegisterForFinalize, Object *obj)
{
    FCALL_CONTRACT;

    // Checked by the caller
    _ASSERTE(obj != NULL);

    if (obj->GetMethodTable()->HasFinalizer())
    {
        HELPER_METHOD_FRAME_BEGIN_1(obj);
        if (!GCHeapUtilities::GetGCHeap()->RegisterForFinalization(-1, obj))
        {
            ThrowOutOfMemory();
        }
        HELPER_METHOD_FRAME_END();
    }
}
FCIMPLEND

FORCEINLINE UINT64 GCInterface::InterlockedAdd (UINT64 *pAugend, UINT64 addend) {
    WRAPPER_NO_CONTRACT;

    UINT64 oldMemValue;
    UINT64 newMemValue;

    do {
        oldMemValue = *pAugend;
        newMemValue = oldMemValue + addend;

        // check for overflow
        if (newMemValue < oldMemValue)
        {
            newMemValue = UINT64_MAX;
        }
    } while (InterlockedCompareExchange64((LONGLONG*) pAugend, (LONGLONG) newMemValue, (LONGLONG) oldMemValue) != (LONGLONG) oldMemValue);

    return newMemValue;
}

FORCEINLINE UINT64 GCInterface::InterlockedSub(UINT64 *pMinuend, UINT64 subtrahend) {
    WRAPPER_NO_CONTRACT;

    UINT64 oldMemValue;
    UINT64 newMemValue;

    do {
        oldMemValue = *pMinuend;
        newMemValue = oldMemValue - subtrahend;

        // check for underflow
        if (newMemValue > oldMemValue)
            newMemValue = 0;
        
    } while (InterlockedCompareExchange64((LONGLONG*) pMinuend, (LONGLONG) newMemValue, (LONGLONG) oldMemValue) != (LONGLONG) oldMemValue);

    return newMemValue;
}

void QCALLTYPE GCInterface::_AddMemoryPressure(UINT64 bytesAllocated) 
{
    QCALL_CONTRACT;

    // AddMemoryPressure could cause a GC, so we need a frame 
    BEGIN_QCALL;
    AddMemoryPressure(bytesAllocated);
    END_QCALL;
}

void GCInterface::AddMemoryPressure(UINT64 bytesAllocated)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    SendEtwAddMemoryPressureEvent(bytesAllocated);

    UINT64 newMemValue = InterlockedAdd(&m_ulMemPressure, bytesAllocated);

    if (newMemValue > m_ulThreshold)
    {
        INT32 gen_collect = 0;
        {
            GCX_PREEMP();
            CrstHolder holder(&m_MemoryPressureLock);

            // to avoid collecting too often, take the max threshold of the linear and geometric growth 
            // heuristics.          
            UINT64 addMethod;
            UINT64 multMethod;
            UINT64 bytesAllocatedMax = (UINT64_MAX - m_ulThreshold) / 8;

            if (bytesAllocated >= bytesAllocatedMax) // overflow check
            {
                addMethod = UINT64_MAX;
            }
            else
            {
                addMethod = m_ulThreshold + bytesAllocated * 8;
            }

            multMethod = newMemValue + newMemValue / 10;
            if (multMethod < newMemValue) // overflow check
            {
                multMethod = UINT64_MAX;
            }

            m_ulThreshold = (addMethod > multMethod) ? addMethod : multMethod;
            for (int i = 0; i <= 1; i++)
            {
                if ((GCHeapUtilities::GetGCHeap()->CollectionCount(i) / RELATIVE_GC_RATIO) > GCHeapUtilities::GetGCHeap()->CollectionCount(i + 1))
                {
                    gen_collect = i + 1;
                    break;
                }
            }
        }

        PREFIX_ASSUME(gen_collect <= 2);

        if ((gen_collect == 0) || (m_gc_counts[gen_collect] == GCHeapUtilities::GetGCHeap()->CollectionCount(gen_collect)))
        {
            GarbageCollectModeAny(gen_collect);
        }

        for (int i = 0; i < 3; i++) 
        {
            m_gc_counts [i] = GCHeapUtilities::GetGCHeap()->CollectionCount(i);
        }
    }
}

#ifdef _WIN64
const unsigned MIN_MEMORYPRESSURE_BUDGET = 4 * 1024 * 1024;        // 4 MB
#else // _WIN64
const unsigned MIN_MEMORYPRESSURE_BUDGET = 3 * 1024 * 1024;        // 3 MB
#endif // _WIN64

const unsigned MAX_MEMORYPRESSURE_RATIO = 10;                      // 40 MB or 30 MB


// Resets pressure accounting after a gen2 GC has occurred.
void GCInterface::CheckCollectionCount()
{
    LIMITED_METHOD_CONTRACT;

    IGCHeap * pHeap = GCHeapUtilities::GetGCHeap();
    
    if (m_gc_counts[2] != pHeap->CollectionCount(2))
    {
        for (int i = 0; i < 3; i++) 
        {
            m_gc_counts[i] = pHeap->CollectionCount(i);
        }

        m_iteration++;

        UINT p = m_iteration % NEW_PRESSURE_COUNT;

        m_addPressure[p] = 0;   // new pressure will be accumulated here
        m_remPressure[p] = 0; 
    }
}


// New AddMemoryPressure implementation (used by RCW and the CLRServicesImpl class)
//
//   1. Less sensitive than the original implementation (start budget 3 MB)
//   2. Focuses more on newly added memory pressure
//   3. Budget adjusted by effectiveness of last 3 triggered GC (add / remove ratio, max 10x)
//   4. Budget maxed with 30% of current managed GC size
//   5. If Gen2 GC is happening naturally, ignore past pressure
//
// Here's a brief description of the ideal algorithm for Add/Remove memory pressure:
// Do a GC when (HeapStart < X * MemPressureGrowth) where
// - HeapStart is GC Heap size after doing the last GC
// - MemPressureGrowth is the net of Add and Remove since the last GC
// - X is proportional to our guess of the ummanaged memory death rate per GC interval,
//   and would be calculated based on historic data using standard exponential approximation:
//   Xnew = UMDeath/UMTotal * 0.5 + Xprev
//
void GCInterface::NewAddMemoryPressure(UINT64 bytesAllocated)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    CheckCollectionCount();

    UINT p = m_iteration % NEW_PRESSURE_COUNT;

    UINT64 newMemValue = InterlockedAdd(&m_addPressure[p], bytesAllocated);

    static_assert(NEW_PRESSURE_COUNT == 4, "NewAddMemoryPressure contains unrolled loops which depend on NEW_PRESSURE_COUNT");

    UINT64 add = m_addPressure[0] + m_addPressure[1] + m_addPressure[2] + m_addPressure[3] - m_addPressure[p];
    UINT64 rem = m_remPressure[0] + m_remPressure[1] + m_remPressure[2] + m_remPressure[3] - m_remPressure[p];

    STRESS_LOG4(LF_GCINFO, LL_INFO10000, "AMP Add: %I64u => added=%I64u total_added=%I64u total_removed=%I64u",
        bytesAllocated, newMemValue, add, rem);

    SendEtwAddMemoryPressureEvent(bytesAllocated); 

    if (newMemValue >= MIN_MEMORYPRESSURE_BUDGET)
    {
        UINT64 budget = MIN_MEMORYPRESSURE_BUDGET;

        if (m_iteration >= NEW_PRESSURE_COUNT) // wait until we have enough data points
        {
            // Adjust according to effectiveness of GC
            // Scale budget according to past m_addPressure / m_remPressure ratio
            if (add >= rem * MAX_MEMORYPRESSURE_RATIO)
            {
                budget = MIN_MEMORYPRESSURE_BUDGET * MAX_MEMORYPRESSURE_RATIO;
            }
            else if (add > rem)
            {
                CONSISTENCY_CHECK(rem != 0);

                // Avoid overflow by calculating addPressure / remPressure as fixed point (1 = 1024)
                budget = (add * 1024 / rem) * budget / 1024;
            }
        }

        // If still over budget, check current managed heap size
        if (newMemValue >= budget)
        {
            IGCHeap *pGCHeap = GCHeapUtilities::GetGCHeap();
            UINT64 heapOver3 = pGCHeap->GetCurrentObjSize() / 3;

            if (budget < heapOver3) // Max
            {
                budget = heapOver3;
            }

            if (newMemValue >= budget)
            {
                // last check - if we would exceed 20% of GC "duty cycle", do not trigger GC at this time
                if ((pGCHeap->GetNow() - pGCHeap->GetLastGCStartTime(2)) > (pGCHeap->GetLastGCDuration(2) * 5))
                {
                    STRESS_LOG6(LF_GCINFO, LL_INFO10000, "AMP Budget: pressure=%I64u ? budget=%I64u (total_added=%I64u, total_removed=%I64u, mng_heap=%I64u) pos=%d",
                        newMemValue, budget, add, rem, heapOver3 * 3, m_iteration);

                    GarbageCollectModeAny(2);

                    CheckCollectionCount();
                }
            }
        }
    }
}

void QCALLTYPE GCInterface::_RemoveMemoryPressure(UINT64 bytesAllocated)
{
    QCALL_CONTRACT;

    BEGIN_QCALL;
    RemoveMemoryPressure(bytesAllocated);
    END_QCALL;
}

void GCInterface::RemoveMemoryPressure(UINT64 bytesAllocated)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    SendEtwRemoveMemoryPressureEvent(bytesAllocated);

    UINT64 newMemValue = InterlockedSub(&m_ulMemPressure, bytesAllocated);
    UINT64 new_th;  
    UINT64 bytesAllocatedMax = (m_ulThreshold / 4);
    UINT64 addMethod;
    UINT64 multMethod = (m_ulThreshold - m_ulThreshold / 20); // can never underflow
    if (bytesAllocated >= bytesAllocatedMax) // protect against underflow
    {
        m_ulThreshold = MIN_GC_MEMORYPRESSURE_THRESHOLD;
        return;
    }
    else
    {
        addMethod = m_ulThreshold - bytesAllocated * 4;
    }

    new_th = (addMethod < multMethod) ? addMethod : multMethod;

    if (newMemValue <= new_th)
    {
        GCX_PREEMP();
        CrstHolder holder(&m_MemoryPressureLock);
        if (new_th > MIN_GC_MEMORYPRESSURE_THRESHOLD)
            m_ulThreshold = new_th;
        else
            m_ulThreshold = MIN_GC_MEMORYPRESSURE_THRESHOLD;

        for (int i = 0; i < 3; i++) 
        {
            m_gc_counts [i] = GCHeapUtilities::GetGCHeap()->CollectionCount(i);
        }
    }
}

void GCInterface::NewRemoveMemoryPressure(UINT64 bytesAllocated)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    CheckCollectionCount();
    
    UINT p = m_iteration % NEW_PRESSURE_COUNT;

    SendEtwRemoveMemoryPressureEvent(bytesAllocated);

    InterlockedAdd(&m_remPressure[p], bytesAllocated);

    STRESS_LOG2(LF_GCINFO, LL_INFO10000, "AMP Remove: %I64u => removed=%I64u",
        bytesAllocated, m_remPressure[p]);
}

inline void GCInterface::SendEtwAddMemoryPressureEvent(UINT64 bytesAllocated)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    FireEtwIncreaseMemoryPressure(bytesAllocated, GetClrInstanceId());
}

// Out-of-line helper to avoid EH prolog/epilog in functions that otherwise don't throw.
NOINLINE void GCInterface::SendEtwRemoveMemoryPressureEvent(UINT64 bytesAllocated)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    EX_TRY
    {
        FireEtwDecreaseMemoryPressure(bytesAllocated, GetClrInstanceId());
    }
    EX_CATCH
    {
        // Ignore failures
    }
    EX_END_CATCH(SwallowAllExceptions)
}

// Out-of-line helper to avoid EH prolog/epilog in functions that otherwise don't throw.
NOINLINE void GCInterface::GarbageCollectModeAny(int generation)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    GCX_COOP();
    GCHeapUtilities::GetGCHeap()->GarbageCollect(generation, false, collection_non_blocking);
}

//
// COMInterlocked
//

#include <optsmallperfcritical.h>

FCIMPL2(INT32,COMInterlocked::Exchange, INT32 *location, INT32 value)
{
    FCALL_CONTRACT;

    if( NULL == location) {
        FCThrow(kNullReferenceException);
    }

    return FastInterlockExchange((LONG *) location, value);
}
FCIMPLEND

FCIMPL2_IV(INT64,COMInterlocked::Exchange64, INT64 *location, INT64 value)
{
    FCALL_CONTRACT;

    if( NULL == location) {
        FCThrow(kNullReferenceException);
    }

    return FastInterlockExchangeLong((INT64 *) location, value);
}
FCIMPLEND

FCIMPL2(LPVOID,COMInterlocked::ExchangePointer, LPVOID *location, LPVOID value)
{
    FCALL_CONTRACT;

    if( NULL == location) {
        FCThrow(kNullReferenceException);
    }

    FCUnique(0x15);
    return FastInterlockExchangePointer(location, value);
}
FCIMPLEND

FCIMPL3(INT32, COMInterlocked::CompareExchange, INT32* location, INT32 value, INT32 comparand)
{
    FCALL_CONTRACT;

    if( NULL == location) {
        FCThrow(kNullReferenceException);
    }

    return FastInterlockCompareExchange((LONG*)location, value, comparand);
}
FCIMPLEND

FCIMPL4(INT32, COMInterlocked::CompareExchangeReliableResult, INT32* location, INT32 value, INT32 comparand, CLR_BOOL* succeeded)
{
    FCALL_CONTRACT;

    if( NULL == location) {
        FCThrow(kNullReferenceException);
    }

    INT32 result = FastInterlockCompareExchange((LONG*)location, value, comparand);
    if (result == comparand)
        *succeeded = true;

    return result;
}
FCIMPLEND

FCIMPL3_IVV(INT64, COMInterlocked::CompareExchange64, INT64* location, INT64 value, INT64 comparand)
{
    FCALL_CONTRACT;

    if( NULL == location) {
        FCThrow(kNullReferenceException);
    }

    return FastInterlockCompareExchangeLong((INT64*)location, value, comparand);
}
FCIMPLEND

FCIMPL3(LPVOID,COMInterlocked::CompareExchangePointer, LPVOID *location, LPVOID value, LPVOID comparand)
{
    FCALL_CONTRACT;

    if( NULL == location) {
        FCThrow(kNullReferenceException);
    }

    FCUnique(0x59);
    return FastInterlockCompareExchangePointer(location, value, comparand);
}
FCIMPLEND

FCIMPL2_IV(float,COMInterlocked::ExchangeFloat, float *location, float value)
{
    FCALL_CONTRACT;

    if( NULL == location) {
        FCThrow(kNullReferenceException);
    }

    LONG ret = FastInterlockExchange((LONG *) location, *(LONG*)&value);
    return *(float*)&ret;
}
FCIMPLEND

FCIMPL2_IV(double,COMInterlocked::ExchangeDouble, double *location, double value)
{
    FCALL_CONTRACT;

    if( NULL == location) {
        FCThrow(kNullReferenceException);
    }


    INT64 ret = FastInterlockExchangeLong((INT64 *) location, *(INT64*)&value);
    return *(double*)&ret;
}
FCIMPLEND

FCIMPL3_IVV(float,COMInterlocked::CompareExchangeFloat, float *location, float value, float comparand)
{
    FCALL_CONTRACT;

    if( NULL == location) {
        FCThrow(kNullReferenceException);
    }

    LONG ret = (LONG)FastInterlockCompareExchange((LONG*) location, *(LONG*)&value, *(LONG*)&comparand);
    return *(float*)&ret;
}
FCIMPLEND

FCIMPL3_IVV(double,COMInterlocked::CompareExchangeDouble, double *location, double value, double comparand)
{
    FCALL_CONTRACT;

    if( NULL == location) {
        FCThrow(kNullReferenceException);
    }

    INT64 ret = (INT64)FastInterlockCompareExchangeLong((INT64*) location, *(INT64*)&value, *(INT64*)&comparand);
    return *(double*)&ret;
}
FCIMPLEND

FCIMPL2(LPVOID,COMInterlocked::ExchangeObject, LPVOID*location, LPVOID value)
{
    FCALL_CONTRACT;

    if( NULL == location) {
        FCThrow(kNullReferenceException);
    }

    LPVOID ret = FastInterlockExchangePointer(location, value);
#ifdef _DEBUG
    Thread::ObjectRefAssign((OBJECTREF *)location);
#endif
    ErectWriteBarrier((OBJECTREF*) location, ObjectToOBJECTREF((Object*) value));
    return ret;
}
FCIMPLEND

FCIMPL3(LPVOID,COMInterlocked::CompareExchangeObject, LPVOID *location, LPVOID value, LPVOID comparand)
{
    FCALL_CONTRACT;

    if( NULL == location) {
        FCThrow(kNullReferenceException);
    }

    // <TODO>@todo: only set ref if is updated</TODO>
    LPVOID ret = FastInterlockCompareExchangePointer(location, value, comparand);
    if (ret == comparand) {
#ifdef _DEBUG
        Thread::ObjectRefAssign((OBJECTREF *)location);
#endif
        ErectWriteBarrier((OBJECTREF*) location, ObjectToOBJECTREF((Object*) value));
    }
    return ret;
}
FCIMPLEND

FCIMPL2(INT32,COMInterlocked::ExchangeAdd32, INT32 *location, INT32 value)
{
    FCALL_CONTRACT;

    if( NULL == location) {
        FCThrow(kNullReferenceException);
    }

    return FastInterlockExchangeAdd((LONG *) location, value);
}
FCIMPLEND

FCIMPL2_IV(INT64,COMInterlocked::ExchangeAdd64, INT64 *location, INT64 value)
{
    FCALL_CONTRACT;

    if( NULL == location) {
        FCThrow(kNullReferenceException);
    }

    return FastInterlockExchangeAddLong((INT64 *) location, value);
}
FCIMPLEND

FCIMPL0(void, COMInterlocked::FCMemoryBarrier)
{
    FCALL_CONTRACT;

    MemoryBarrier();
    FC_GC_POLL();
}
FCIMPLEND

#include <optdefault.h>

void QCALLTYPE COMInterlocked::MemoryBarrierProcessWide()
{
    QCALL_CONTRACT;

    FlushProcessWriteBuffers();
}

static BOOL HasOverriddenMethod(MethodTable* mt, MethodTable* classMT, WORD methodSlot)
{
    CONTRACTL{
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    } CONTRACTL_END;

    _ASSERTE(mt != NULL);
    _ASSERTE(classMT != NULL);
    _ASSERTE(methodSlot != 0);

    PCODE actual = mt->GetRestoredSlot(methodSlot);
    PCODE base = classMT->GetRestoredSlot(methodSlot);

    if (actual == base)
    {
        return FALSE;
    }

    if (!classMT->IsZapped())
    {
        // If mscorlib is JITed, the slots can be patched and thus we need to compare the actual MethodDescs
        // to detect match reliably
        if (MethodTable::GetMethodDescForSlotAddress(actual) == MethodTable::GetMethodDescForSlotAddress(base))
        {
            return FALSE;
        }
    }

    return TRUE;
}

static BOOL CanCompareBitsOrUseFastGetHashCode(MethodTable* mt)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    } CONTRACTL_END;

    _ASSERTE(mt != NULL);

    if (mt->HasCheckedCanCompareBitsOrUseFastGetHashCode())
    {
        return mt->CanCompareBitsOrUseFastGetHashCode();
    }

    if (mt->ContainsPointers()
        || mt->IsNotTightlyPacked())
    {
        mt->SetHasCheckedCanCompareBitsOrUseFastGetHashCode();
        return FALSE;
    }

    MethodTable* valueTypeMT = MscorlibBinder::GetClass(CLASS__VALUE_TYPE);
    WORD slotEquals = MscorlibBinder::GetMethod(METHOD__VALUE_TYPE__EQUALS)->GetSlot();
    WORD slotGetHashCode = MscorlibBinder::GetMethod(METHOD__VALUE_TYPE__GET_HASH_CODE)->GetSlot();

    // Check the input type.
    if (HasOverriddenMethod(mt, valueTypeMT, slotEquals)
        || HasOverriddenMethod(mt, valueTypeMT, slotGetHashCode))
    {
        mt->SetHasCheckedCanCompareBitsOrUseFastGetHashCode();

        // If overridden Equals or GetHashCode found, stop searching further.
        return FALSE;
    }

    BOOL canCompareBitsOrUseFastGetHashCode = TRUE;

    // The type itself did not override Equals or GetHashCode, go for its fields.
    ApproxFieldDescIterator iter = ApproxFieldDescIterator(mt, ApproxFieldDescIterator::INSTANCE_FIELDS);
    for (FieldDesc* pField = iter.Next(); pField != NULL; pField = iter.Next())
    {
        if (pField->GetFieldType() == ELEMENT_TYPE_VALUETYPE)
        {
            // Check current field type.
            MethodTable* fieldMethodTable = pField->GetApproxFieldTypeHandleThrowing().GetMethodTable();
            if (!CanCompareBitsOrUseFastGetHashCode(fieldMethodTable))
            {
                canCompareBitsOrUseFastGetHashCode = FALSE;
                break;
            }
        }
        else if (pField->GetFieldType() == ELEMENT_TYPE_R8
                || pField->GetFieldType() == ELEMENT_TYPE_R4)
        {
            // We have double/single field, cannot compare in fast path.
            canCompareBitsOrUseFastGetHashCode = FALSE;
            break;
        }
    }

    // We've gone through all instance fields. It's time to cache the result.
    // Note SetCanCompareBitsOrUseFastGetHashCode(BOOL) ensures the checked flag
    // and canCompare flag being set atomically to avoid race.
    mt->SetCanCompareBitsOrUseFastGetHashCode(canCompareBitsOrUseFastGetHashCode);

    return canCompareBitsOrUseFastGetHashCode;
}

NOINLINE static FC_BOOL_RET CanCompareBitsHelper(MethodTable* mt, OBJECTREF objRef)
{
    FC_INNER_PROLOG(ValueTypeHelper::CanCompareBits);

    _ASSERTE(mt != NULL);
    _ASSERTE(objRef != NULL);

    BOOL ret = FALSE;

    HELPER_METHOD_FRAME_BEGIN_RET_ATTRIB_1(Frame::FRAME_ATTR_EXACT_DEPTH|Frame::FRAME_ATTR_CAPTURE_DEPTH_2, objRef);

    ret = CanCompareBitsOrUseFastGetHashCode(mt);

    HELPER_METHOD_FRAME_END();
    FC_INNER_EPILOG();

    FC_RETURN_BOOL(ret);
}

// Return true if the valuetype does not contain pointer, is tightly packed, 
// does not have floating point number field and does not override Equals method.
FCIMPL1(FC_BOOL_RET, ValueTypeHelper::CanCompareBits, Object* obj)
{
    FCALL_CONTRACT;

    _ASSERTE(obj != NULL);
    MethodTable* mt = obj->GetMethodTable();

    if (mt->HasCheckedCanCompareBitsOrUseFastGetHashCode())
    {
        FC_RETURN_BOOL(mt->CanCompareBitsOrUseFastGetHashCode());
    }

    OBJECTREF objRef(obj);

    FC_INNER_RETURN(FC_BOOL_RET, CanCompareBitsHelper(mt, objRef));
}
FCIMPLEND

FCIMPL2(FC_BOOL_RET, ValueTypeHelper::FastEqualsCheck, Object* obj1, Object* obj2)
{
    FCALL_CONTRACT;

    _ASSERTE(obj1 != NULL);
    _ASSERTE(obj2 != NULL);
    _ASSERTE(!obj1->GetMethodTable()->ContainsPointers());
    _ASSERTE(obj1->GetSize() == obj2->GetSize());

    TypeHandle pTh = obj1->GetTypeHandle();

    FC_RETURN_BOOL(memcmp(obj1->GetData(),obj2->GetData(),pTh.GetSize()) == 0);
}
FCIMPLEND

static INT32 FastGetValueTypeHashCodeHelper(MethodTable *mt, void *pObjRef)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_COOPERATIVE;
    } CONTRACTL_END;

    INT32 hashCode = 0;
    INT32 *pObj = (INT32*)pObjRef;
            
    // this is a struct with no refs and no "strange" offsets, just go through the obj and xor the bits
    INT32 size = mt->GetNumInstanceFieldBytes();
    for (INT32 i = 0; i < (INT32)(size / sizeof(INT32)); i++)
        hashCode ^= *pObj++;

    return hashCode;
}

static INT32 RegularGetValueTypeHashCode(MethodTable *mt, void *pObjRef)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    } CONTRACTL_END;

    INT32 hashCode = 0;

    GCPROTECT_BEGININTERIOR(pObjRef);

    BOOL canUseFastGetHashCodeHelper = FALSE;
    if (mt->HasCheckedCanCompareBitsOrUseFastGetHashCode())
    {
        canUseFastGetHashCodeHelper = mt->CanCompareBitsOrUseFastGetHashCode();
    }
    else
    {
        canUseFastGetHashCodeHelper = CanCompareBitsOrUseFastGetHashCode(mt);
    }

    // While we shouln't get here directly from ValueTypeHelper::GetHashCode, if we recurse we need to 
    // be able to handle getting the hashcode for an embedded structure whose hashcode is computed by the fast path.
    if (canUseFastGetHashCodeHelper)
    {
        hashCode = FastGetValueTypeHashCodeHelper(mt, pObjRef);
    }
    else
    {
        // it's looking ugly so we'll use the old behavior in managed code. Grab the first non-null
        // field and return its hash code or 'it' as hash code
        // <TODO> Note that the old behavior has already been broken for value types
        //              that is qualified for CanUseFastGetHashCodeHelper. So maybe we should
        //              change the implementation here to use all fields instead of just the 1st one.
        // </TODO>
        //
        // <TODO> check this approximation - we may be losing exact type information </TODO>
        ApproxFieldDescIterator fdIterator(mt, ApproxFieldDescIterator::INSTANCE_FIELDS);

        FieldDesc *field;
        while ((field = fdIterator.Next()) != NULL)
        {
            _ASSERTE(!field->IsRVA());
            if (field->IsObjRef())
            {
                // if we get an object reference we get the hash code out of that
                if (*(Object**)((BYTE *)pObjRef + field->GetOffsetUnsafe()) != NULL)
                {
                    PREPARE_SIMPLE_VIRTUAL_CALLSITE(METHOD__OBJECT__GET_HASH_CODE, (*(Object**)((BYTE *)pObjRef + field->GetOffsetUnsafe())));
                    DECLARE_ARGHOLDER_ARRAY(args, 1);
                    args[ARGNUM_0] = PTR_TO_ARGHOLDER(*(Object**)((BYTE *)pObjRef + field->GetOffsetUnsafe()));
                    CALL_MANAGED_METHOD(hashCode, INT32, args);
                }
                else
                {
                    // null object reference, try next
                    continue;
                }
            }
            else
            {
                CorElementType fieldType = field->GetFieldType();
                if (fieldType == ELEMENT_TYPE_R8)
                {
                    PREPARE_NONVIRTUAL_CALLSITE(METHOD__DOUBLE__GET_HASH_CODE);
                    DECLARE_ARGHOLDER_ARRAY(args, 1);
                    args[ARGNUM_0] = PTR_TO_ARGHOLDER(((BYTE *)pObjRef + field->GetOffsetUnsafe()));
                    CALL_MANAGED_METHOD(hashCode, INT32, args);
                }
                else if (fieldType == ELEMENT_TYPE_R4)
                {
                    PREPARE_NONVIRTUAL_CALLSITE(METHOD__SINGLE__GET_HASH_CODE);
                    DECLARE_ARGHOLDER_ARRAY(args, 1);
                    args[ARGNUM_0] = PTR_TO_ARGHOLDER(((BYTE *)pObjRef + field->GetOffsetUnsafe()));
                    CALL_MANAGED_METHOD(hashCode, INT32, args);
                }
                else if (fieldType != ELEMENT_TYPE_VALUETYPE)
                {
                    UINT fieldSize = field->LoadSize();
                    INT32 *pValue = (INT32*)((BYTE *)pObjRef + field->GetOffsetUnsafe());
                    for (INT32 j = 0; j < (INT32)(fieldSize / sizeof(INT32)); j++)
                        hashCode ^= *pValue++;
                }
                else
                {
                    // got another value type. Get the type
                    TypeHandle fieldTH = field->GetFieldTypeHandleThrowing();
                    _ASSERTE(!fieldTH.IsNull());
                    hashCode = RegularGetValueTypeHashCode(fieldTH.GetMethodTable(), (BYTE *)pObjRef + field->GetOffsetUnsafe());
                }
            }
            break;
        }
    }

    GCPROTECT_END();

    return hashCode;
}

// The default implementation of GetHashCode() for all value types.
// Note that this implementation reveals the value of the fields.
// So if the value type contains any sensitive information it should
// implement its own GetHashCode().
FCIMPL1(INT32, ValueTypeHelper::GetHashCode, Object* objUNSAFE)
{
    FCALL_CONTRACT;

    if (objUNSAFE == NULL)
        FCThrow(kNullReferenceException);

    OBJECTREF obj = ObjectToOBJECTREF(objUNSAFE);
    VALIDATEOBJECTREF(obj);

    INT32 hashCode = 0;
    MethodTable *pMT = objUNSAFE->GetMethodTable();

    // We don't want to expose the method table pointer in the hash code
    // Let's use the typeID instead.
    UINT32 typeID = pMT->LookupTypeID();
    if (typeID == TypeIDProvider::INVALID_TYPE_ID)
    {
        // If the typeID has yet to be generated, fall back to GetTypeID
        // This only needs to be done once per MethodTable
        HELPER_METHOD_FRAME_BEGIN_RET_1(obj);        
        typeID = pMT->GetTypeID();
        HELPER_METHOD_FRAME_END();
    }

    // To get less colliding and more evenly distributed hash codes,
    // we munge the class index with two big prime numbers
    hashCode = typeID * 711650207 + 2506965631U;

    BOOL canUseFastGetHashCodeHelper = FALSE;
    if (pMT->HasCheckedCanCompareBitsOrUseFastGetHashCode())
    {
        canUseFastGetHashCodeHelper = pMT->CanCompareBitsOrUseFastGetHashCode();
    }
    else
    {
        HELPER_METHOD_FRAME_BEGIN_RET_1(obj);
        canUseFastGetHashCodeHelper = CanCompareBitsOrUseFastGetHashCode(pMT);
        HELPER_METHOD_FRAME_END();
    }

    if (canUseFastGetHashCodeHelper)
    {
        hashCode ^= FastGetValueTypeHashCodeHelper(pMT, obj->UnBox());
    }
    else
    {
        HELPER_METHOD_FRAME_BEGIN_RET_1(obj);
        hashCode ^= RegularGetValueTypeHashCode(pMT, obj->UnBox());
        HELPER_METHOD_FRAME_END();
    }

    return hashCode;
}
FCIMPLEND

static LONG s_dwSeed;

FCIMPL1(INT32, ValueTypeHelper::GetHashCodeOfPtr, LPVOID ptr)
{
    FCALL_CONTRACT;

    INT32 hashCode = (INT32)((INT64)(ptr));

    if (hashCode == 0)
    {
        return 0;
    }

    DWORD dwSeed = s_dwSeed;

    // Initialize s_dwSeed lazily
    if (dwSeed == 0)
    {
        // We use the first non-0 pointer as the seed, all hashcodes will be based off that.
        // This is to make sure that we only reveal relative memory addresses and never absolute ones.
        dwSeed = hashCode;
        InterlockedCompareExchange(&s_dwSeed, dwSeed, 0);
        dwSeed = s_dwSeed;
    }
    _ASSERTE(dwSeed != 0);

    return hashCode - dwSeed;
}
FCIMPLEND

static MethodTable * g_pStreamMT;
static WORD g_slotBeginRead, g_slotEndRead;
static WORD g_slotBeginWrite, g_slotEndWrite;

static bool HasOverriddenStreamMethod(MethodTable * pMT, WORD slot)
{
    CONTRACTL{
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    } CONTRACTL_END;

    PCODE actual = pMT->GetRestoredSlot(slot);
    PCODE base = g_pStreamMT->GetRestoredSlot(slot);
    if (actual == base)
        return false;

    if (!g_pStreamMT->IsZapped())
    {
        // If mscorlib is JITed, the slots can be patched and thus we need to compare the actual MethodDescs 
        // to detect match reliably
        if (MethodTable::GetMethodDescForSlotAddress(actual) == MethodTable::GetMethodDescForSlotAddress(base))
            return false;
    }

    return true;
}

FCIMPL1(FC_BOOL_RET, StreamNative::HasOverriddenBeginEndRead, Object *stream)
{
    FCALL_CONTRACT;

    if (stream == NULL)
        FC_RETURN_BOOL(TRUE);

    if (g_pStreamMT == NULL || g_slotBeginRead == 0 || g_slotEndRead == 0)
    {
        HELPER_METHOD_FRAME_BEGIN_RET_1(stream);
        g_pStreamMT = MscorlibBinder::GetClass(CLASS__STREAM);
        g_slotBeginRead = MscorlibBinder::GetMethod(METHOD__STREAM__BEGIN_READ)->GetSlot();
        g_slotEndRead = MscorlibBinder::GetMethod(METHOD__STREAM__END_READ)->GetSlot();
        HELPER_METHOD_FRAME_END();
    }

    MethodTable * pMT = stream->GetMethodTable();

    FC_RETURN_BOOL(HasOverriddenStreamMethod(pMT, g_slotBeginRead) || HasOverriddenStreamMethod(pMT, g_slotEndRead));
}
FCIMPLEND

FCIMPL1(FC_BOOL_RET, StreamNative::HasOverriddenBeginEndWrite, Object *stream)
{
    FCALL_CONTRACT;

    if (stream == NULL) 
        FC_RETURN_BOOL(TRUE);

    if (g_pStreamMT == NULL || g_slotBeginWrite == 0 || g_slotEndWrite == 0)
    {
        HELPER_METHOD_FRAME_BEGIN_RET_1(stream);
        g_pStreamMT = MscorlibBinder::GetClass(CLASS__STREAM);
        g_slotBeginWrite = MscorlibBinder::GetMethod(METHOD__STREAM__BEGIN_WRITE)->GetSlot();
        g_slotEndWrite = MscorlibBinder::GetMethod(METHOD__STREAM__END_WRITE)->GetSlot();
        HELPER_METHOD_FRAME_END();
    }

    MethodTable * pMT = stream->GetMethodTable();

    FC_RETURN_BOOL(HasOverriddenStreamMethod(pMT, g_slotBeginWrite) || HasOverriddenStreamMethod(pMT, g_slotEndWrite));
}
FCIMPLEND
