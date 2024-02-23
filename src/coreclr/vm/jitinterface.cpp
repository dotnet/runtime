// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ===========================================================================
// File: JITinterface.CPP
//

// ===========================================================================

#include "common.h"
#include "jitinterface.h"
#include "codeman.h"
#include "method.hpp"
#include "class.h"
#include "object.h"
#include "field.h"
#include "stublink.h"
#include "virtualcallstub.h"
#include "corjit.h"
#include "eeconfig.h"
#include "excep.h"
#include "log.h"
#include "excep.h"
#include "float.h"      // for isnan
#include "dbginterface.h"
#include "dllimport.h"
#include "callconvbuilder.hpp"
#include "gcheaputilities.h"
#include "comdelegate.h"
#include "corprof.h"
#include "eeprofinterfaces.h"
#ifdef PROFILING_SUPPORTED
#include "proftoeeinterfaceimpl.h"
#include "eetoprofinterfaceimpl.h"
#include "eetoprofinterfaceimpl.inl"
#include "profilepriv.h"
#include "rejit.h"
#endif // PROFILING_SUPPORTED
#include "ecall.h"
#include "generics.h"
#include "typestring.h"
#include "typedesc.h"
#include "genericdict.h"
#include "array.h"
#include "debuginfostore.h"
#include "safemath.h"
#include "runtimehandles.h"
#include "sigbuilder.h"
#include "openum.h"
#include "fieldmarshaler.h"
#ifdef HAVE_GCCOVER
#include "gccover.h"
#endif // HAVE_GCCOVER

#ifdef FEATURE_INTERPRETER
#include "interpreter.h"
#endif // FEATURE_INTERPRETER

#ifdef FEATURE_PERFMAP
#include "perfmap.h"
#endif

#ifdef FEATURE_PGO
#include "pgo.h"
#endif

#include "tailcallhelp.h"

#ifdef TARGET_WINDOWS
EXTERN_C uint32_t _tls_index;
#endif

#ifndef _MSC_VER
extern "C" void* __tls_get_addr(void* ti);
#endif // !_MSC_VER

// The Stack Overflow probe takes place in the COOPERATIVE_TRANSITION_BEGIN() macro
//

#define JIT_TO_EE_TRANSITION()          MAKE_CURRENT_THREAD_AVAILABLE_EX(m_pThread);                \
                                        INSTALL_UNWIND_AND_CONTINUE_HANDLER_EX;               \
                                        COOPERATIVE_TRANSITION_BEGIN();                             \

#define EE_TO_JIT_TRANSITION()          COOPERATIVE_TRANSITION_END();                               \
                                        UNINSTALL_UNWIND_AND_CONTINUE_HANDLER_EX(true);

#define JIT_TO_EE_TRANSITION_LEAF()
#define EE_TO_JIT_TRANSITION_LEAF()

#ifdef DACCESS_COMPILE

// The real definitions are in jithelpers.cpp. However, those files are not included in the DAC build.
// Hence, we add them here.
GARY_IMPL(VMHELPDEF, hlpFuncTable, CORINFO_HELP_COUNT);
GARY_IMPL(VMHELPDEF, hlpDynamicFuncTable, DYNAMIC_CORINFO_HELP_COUNT);

#else // DACCESS_COMPILE

Volatile<int64_t> g_cbILJitted = 0;
Volatile<int64_t> g_cMethodsJitted = 0;
Volatile<int64_t> g_c100nsTicksInJit = 0;
thread_local int64_t t_cbILJittedForThread = 0;
thread_local int64_t t_cMethodsJittedForThread = 0;
thread_local int64_t t_c100nsTicksInJitForThread = 0;

// This prevents tearing of 64 bit values on 32 bit systems
static inline
int64_t AtomicLoad64WithoutTearing(int64_t volatile *valueRef)
{
    WRAPPER_NO_CONTRACT;
#if TARGET_64BIT
    return VolatileLoad(valueRef);
#else
    return InterlockedCompareExchangeT((LONG64 volatile *)valueRef, (LONG64)0, (LONG64)0);
#endif // TARGET_64BIT
}

FCIMPL1(INT64, GetCompiledILBytes, CLR_BOOL currentThread)
{
    FCALL_CONTRACT;

    return currentThread ? t_cbILJittedForThread : AtomicLoad64WithoutTearing(&g_cbILJitted);
}
FCIMPLEND

FCIMPL1(INT64, GetCompiledMethodCount, CLR_BOOL currentThread)
{
    FCALL_CONTRACT;

    return currentThread ? t_cMethodsJittedForThread : AtomicLoad64WithoutTearing(&g_cMethodsJitted);
}
FCIMPLEND

FCIMPL1(INT64, GetCompilationTimeInTicks, CLR_BOOL currentThread)
{
    FCALL_CONTRACT;

    return currentThread ? t_c100nsTicksInJitForThread : AtomicLoad64WithoutTearing(&g_c100nsTicksInJit);
}
FCIMPLEND

/*********************************************************************/

inline CORINFO_MODULE_HANDLE GetScopeHandle(MethodDesc* method)
{
    LIMITED_METHOD_CONTRACT;
    if (method->IsDynamicMethod())
    {
        return MakeDynamicScope(method->AsDynamicMethodDesc()->GetResolver());
    }
    else
    {
        return GetScopeHandle(method->GetModule());
    }
}

//This is common refactored code from within several of the access check functions.
static BOOL ModifyCheckForDynamicMethod(DynamicResolver *pResolver,
                                 TypeHandle *pOwnerTypeForSecurity,
                                 AccessCheckOptions::AccessCheckType *pAccessCheckType,
                                 DynamicResolver** ppAccessContext)
{
    CONTRACTL {
        STANDARD_VM_CHECK;
        PRECONDITION(CheckPointer(pResolver));
        PRECONDITION(CheckPointer(pOwnerTypeForSecurity));
        PRECONDITION(CheckPointer(pAccessCheckType));
        PRECONDITION(CheckPointer(ppAccessContext));
        PRECONDITION(*pAccessCheckType == AccessCheckOptions::kNormalAccessibilityChecks);
    } CONTRACTL_END;

    BOOL doAccessCheck = TRUE;

    //Do not blindly initialize fields, since they've already got important values.
    DynamicResolver::SecurityControlFlags dwSecurityFlags = DynamicResolver::Default;

    TypeHandle dynamicOwner;
    pResolver->GetJitContext(&dwSecurityFlags, &dynamicOwner);
    if (!dynamicOwner.IsNull())
        *pOwnerTypeForSecurity = dynamicOwner;

    if (dwSecurityFlags & DynamicResolver::SkipVisibilityChecks)
    {
        doAccessCheck = FALSE;
    }
    else if (dwSecurityFlags & DynamicResolver::RestrictedSkipVisibilityChecks)
    {
        *pAccessCheckType = AccessCheckOptions::kRestrictedMemberAccessNoTransparency;
    }
    else
    {
        *pAccessCheckType = AccessCheckOptions::kNormalAccessNoTransparency;
    }

    return doAccessCheck;
}

TransientMethodDetails::TransientMethodDetails(MethodDesc* pMD, _In_opt_ COR_ILMETHOD_DECODER* header, CORINFO_MODULE_HANDLE scope)
    : Method{ pMD }
    , Header{ header }
    , Scope{ scope }
{
    LIMITED_METHOD_CONTRACT;
    _ASSERTE(Method != NULL);
    _ASSERTE(Scope == NULL || IsDynamicScope(Scope));
}

TransientMethodDetails::TransientMethodDetails(TransientMethodDetails&& other)
{
    LIMITED_METHOD_CONTRACT;
    *this = std::move(other);
}

TransientMethodDetails::~TransientMethodDetails()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    // If the supplied scope is dynamic, release resources.
    if (IsDynamicScope(Scope))
    {
        DynamicResolver* resolver = GetDynamicResolver(Scope);
        resolver->FreeCompileTimeState();
        delete resolver;
    }
}

TransientMethodDetails& TransientMethodDetails::operator=(TransientMethodDetails&& other)
{
    LIMITED_METHOD_CONTRACT;
    if (this != &other)
    {
        Method = other.Method;
        Header = other.Header;
        Scope = other.Scope;
        other.Method = NULL;
        other.Header = NULL;
        other.Scope = NULL;
    }
    return *this;
}

/*****************************************************************************/

// Initialize from data we passed across to the JIT
void CEEInfo::GetTypeContext(const CORINFO_SIG_INST *info, SigTypeContext *pTypeContext)
{
    LIMITED_METHOD_CONTRACT;
    SigTypeContext::InitTypeContext(
        Instantiation((TypeHandle *) info->classInst, info->classInstCount),
        Instantiation((TypeHandle *) info->methInst, info->methInstCount),
        pTypeContext);
}

MethodDesc* CEEInfo::GetMethodFromContext(CORINFO_CONTEXT_HANDLE context)
{
    LIMITED_METHOD_CONTRACT;

    if (context == METHOD_BEING_COMPILED_CONTEXT())
        return m_pMethodBeingCompiled;

    if (((size_t) context & CORINFO_CONTEXTFLAGS_MASK) == CORINFO_CONTEXTFLAGS_CLASS)
    {
        return NULL;
    }
    else
    {
        return GetMethod((CORINFO_METHOD_HANDLE)((size_t) context & ~CORINFO_CONTEXTFLAGS_MASK));
    }
}

TypeHandle CEEInfo::GetTypeFromContext(CORINFO_CONTEXT_HANDLE context)
{
    LIMITED_METHOD_CONTRACT;

    if (context == METHOD_BEING_COMPILED_CONTEXT())
        return m_pMethodBeingCompiled->GetMethodTable();

    if (((size_t) context & CORINFO_CONTEXTFLAGS_MASK) == CORINFO_CONTEXTFLAGS_CLASS)
    {
        return TypeHandle((CORINFO_CLASS_HANDLE) ((size_t) context & ~CORINFO_CONTEXTFLAGS_MASK));
    }
    else
    {
        return GetMethod((CORINFO_METHOD_HANDLE)((size_t)context & ~CORINFO_CONTEXTFLAGS_MASK))->GetMethodTable();
    }
}

// Initialize from a context parameter passed to the JIT and back.  This is a parameter
// that indicates which method is being jitted.

void CEEInfo::GetTypeContext(CORINFO_CONTEXT_HANDLE context, SigTypeContext *pTypeContext)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        PRECONDITION(context != NULL);
    }
    CONTRACTL_END;
    MethodDesc* pMD = GetMethodFromContext(context);
    if (pMD != NULL)
    {
        SigTypeContext::InitTypeContext(pMD, pTypeContext);
    }
    else
    {
        SigTypeContext::InitTypeContext(GetTypeFromContext(context), pTypeContext);
    }
}

/*********************************************************************/
// This normalizes EE type information into the form expected by the JIT.
//
// If typeHnd contains exact type information, then *clsRet will contain
// the normalized CORINFO_CLASS_HANDLE information on return.

// Static
CorInfoType CEEInfo::asCorInfoType(CorElementType eeType,
                                   TypeHandle typeHnd, /* optional in */
                                   CORINFO_CLASS_HANDLE *clsRet/* optional out */ ) {
    CONTRACT(CorInfoType) {
        THROWS;
        GC_TRIGGERS;
        PRECONDITION((CorTypeInfo::IsGenericVariable(eeType)) ==
                     (!typeHnd.IsNull() && typeHnd.IsGenericVariable()));
        PRECONDITION(eeType != ELEMENT_TYPE_GENERICINST);
    } CONTRACT_END;

    TypeHandle typeHndUpdated = typeHnd;

    if (!typeHnd.IsNull())
    {
        CorElementType normType = typeHnd.GetInternalCorElementType();
        // If we have a type handle, then it has the better type
        // in some cases
        if (eeType == ELEMENT_TYPE_VALUETYPE && !CorTypeInfo::IsObjRef(normType))
            eeType = normType;

        // Zap the typeHnd when the type _really_ is a primitive
        // as far as verification is concerned. Returning a null class
        // handle means it is a primitive.
        //
        // Enums are exactly like primitives, even from a verification standpoint,
        // so we zap the type handle in this case.
        //
        // However RuntimeTypeHandle etc. are reported as E_T_INT (or something like that)
        // but don't count as primitives as far as verification is concerned...
        //
        // To make things stranger, TypedReference returns true for "IsTruePrimitive".
        // However the JIT likes us to report the type handle in that case.
        if (!typeHnd.IsTypeDesc() && (
                (typeHnd.AsMethodTable()->IsTruePrimitive() && typeHnd != TypeHandle(g_TypedReferenceMT))
                    || typeHnd.AsMethodTable()->IsEnum()) )
        {
            typeHndUpdated = TypeHandle();
        }

    }

    static const BYTE map[] = {
        CORINFO_TYPE_UNDEF,
        CORINFO_TYPE_VOID,
        CORINFO_TYPE_BOOL,
        CORINFO_TYPE_CHAR,
        CORINFO_TYPE_BYTE,
        CORINFO_TYPE_UBYTE,
        CORINFO_TYPE_SHORT,
        CORINFO_TYPE_USHORT,
        CORINFO_TYPE_INT,
        CORINFO_TYPE_UINT,
        CORINFO_TYPE_LONG,
        CORINFO_TYPE_ULONG,
        CORINFO_TYPE_FLOAT,
        CORINFO_TYPE_DOUBLE,
        CORINFO_TYPE_STRING,
        CORINFO_TYPE_PTR,            // PTR
        CORINFO_TYPE_BYREF,
        CORINFO_TYPE_VALUECLASS,
        CORINFO_TYPE_CLASS,
        CORINFO_TYPE_VAR,            // VAR (type variable)
        CORINFO_TYPE_CLASS,          // ARRAY
        CORINFO_TYPE_CLASS,          // WITH
        CORINFO_TYPE_REFANY,
        CORINFO_TYPE_UNDEF,          // VALUEARRAY_UNSUPPORTED
        CORINFO_TYPE_NATIVEINT,      // I
        CORINFO_TYPE_NATIVEUINT,     // U
        CORINFO_TYPE_UNDEF,          // R_UNSUPPORTED

        // put the correct type when we know our implementation
        CORINFO_TYPE_PTR,            // FNPTR
        CORINFO_TYPE_CLASS,          // OBJECT
        CORINFO_TYPE_CLASS,          // SZARRAY
        CORINFO_TYPE_VAR,            // MVAR

        CORINFO_TYPE_UNDEF,          // CMOD_REQD
        CORINFO_TYPE_UNDEF,          // CMOD_OPT
        CORINFO_TYPE_UNDEF,          // INTERNAL
        };

    _ASSERTE(sizeof(map) == ELEMENT_TYPE_MAX);
    _ASSERTE(eeType < (CorElementType) sizeof(map));
        // spot check of the map
    _ASSERTE((CorInfoType) map[ELEMENT_TYPE_I4] == CORINFO_TYPE_INT);
    _ASSERTE((CorInfoType) map[ELEMENT_TYPE_PTR] == CORINFO_TYPE_PTR);
    _ASSERTE((CorInfoType) map[ELEMENT_TYPE_TYPEDBYREF] == CORINFO_TYPE_REFANY);

    CorInfoType res = ((unsigned)eeType < ELEMENT_TYPE_MAX) ? ((CorInfoType) map[(unsigned)eeType]) : CORINFO_TYPE_UNDEF;

    if (clsRet)
        *clsRet = CORINFO_CLASS_HANDLE(typeHndUpdated.AsPtr());

    RETURN res;
}


inline static CorInfoType toJitType(TypeHandle typeHnd, CORINFO_CLASS_HANDLE *clsRet = NULL)
{
    WRAPPER_NO_CONTRACT;
    return CEEInfo::asCorInfoType(typeHnd.GetInternalCorElementType(), typeHnd, clsRet);
}

enum ConvToJitSigFlags : int
{
    CONV_TO_JITSIG_FLAGS_NONE       = 0x0,
    CONV_TO_JITSIG_FLAGS_LOCALSIG   = 0x1,
};

//---------------------------------------------------------------------------------------
//
//@GENERICS:
// The method handle is used to instantiate method and class type parameters
// It's also used to determine whether an extra dictionary parameter is required
//
// sig          - Input metadata signature
// scopeHnd     - The signature is to be interpreted in the context of this scope (module)
// token        - Metadata token used to refer to the signature (may be mdTokenNil for dynamic methods)
// sigRet       - Resulting output signature in a format that is understood by native compilers
// pContextMD   - The method with any instantiation information (may be NULL)
// localSig     - Is it a local variables declaration, or a method signature (with return type, etc).
// contextType  - The type with any instantiaton information
//
static void ConvToJitSig(
    PCCOR_SIGNATURE       pSig,
    DWORD                 cbSig,
    CORINFO_MODULE_HANDLE scopeHnd,
    mdToken               token,
    SigTypeContext*       typeContext,
    ConvToJitSigFlags     flags,
    CORINFO_SIG_INFO *    sigRet)
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
    } CONTRACTL_END;

    uint32_t sigRetFlags = 0;

    static_assert_no_msg(CORINFO_CALLCONV_DEFAULT == (CorInfoCallConv) IMAGE_CEE_CS_CALLCONV_DEFAULT);
    static_assert_no_msg(CORINFO_CALLCONV_VARARG == (CorInfoCallConv) IMAGE_CEE_CS_CALLCONV_VARARG);
    static_assert_no_msg(CORINFO_CALLCONV_MASK == (CorInfoCallConv) IMAGE_CEE_CS_CALLCONV_MASK);
    static_assert_no_msg(CORINFO_CALLCONV_HASTHIS == (CorInfoCallConv) IMAGE_CEE_CS_CALLCONV_HASTHIS);

    TypeHandle typeHnd = TypeHandle();

    sigRet->pSig = pSig;
    sigRet->cbSig = cbSig;
    sigRet->methodSignature = 0;
    sigRet->retTypeClass = 0;
    sigRet->retTypeSigClass = 0;
    sigRet->scope = scopeHnd;
    sigRet->token = token;
    sigRet->sigInst.classInst = (CORINFO_CLASS_HANDLE *) typeContext->m_classInst.GetRawArgs();
    sigRet->sigInst.classInstCount = (unsigned) typeContext->m_classInst.GetNumArgs();
    sigRet->sigInst.methInst = (CORINFO_CLASS_HANDLE *) typeContext->m_methodInst.GetRawArgs();
    sigRet->sigInst.methInstCount = (unsigned) typeContext->m_methodInst.GetNumArgs();

    SigPointer sig(pSig, cbSig);

    if ((flags & CONV_TO_JITSIG_FLAGS_LOCALSIG) == 0)
    {
        // This is a method signature which includes calling convention, return type,
        // arguments, etc

        _ASSERTE(!sig.IsNull());
        Module * module = GetModule(scopeHnd);

        uint32_t data;
        IfFailThrow(sig.GetCallingConvInfo(&data));
        sigRet->callConv = (CorInfoCallConv) data;

#if defined(TARGET_UNIX) || defined(TARGET_ARM)
        if ((isCallConv(sigRet->callConv, IMAGE_CEE_CS_CALLCONV_VARARG)) ||
            (isCallConv(sigRet->callConv, IMAGE_CEE_CS_CALLCONV_NATIVEVARARG)))
        {
            // This signature corresponds to a method that uses varargs, which are not supported.
             COMPlusThrow(kInvalidProgramException, IDS_EE_VARARG_NOT_SUPPORTED);
        }
#endif // defined(TARGET_UNIX) || defined(TARGET_ARM)

        // Skip number of type arguments
        if (sigRet->callConv & IMAGE_CEE_CS_CALLCONV_GENERIC)
          IfFailThrow(sig.GetData(NULL));

        uint32_t numArgs;
        IfFailThrow(sig.GetData(&numArgs));
        if (numArgs != (unsigned short) numArgs)
            COMPlusThrowHR(COR_E_INVALIDPROGRAM);

        sigRet->numArgs = (unsigned short) numArgs;

        CorElementType type = sig.PeekElemTypeClosed(module, typeContext);

        if (!CorTypeInfo::IsPrimitiveType(type))
        {
            typeHnd = sig.GetTypeHandleThrowing(module, typeContext);
            _ASSERTE(!typeHnd.IsNull());

            // I believe it doesn't make any diff. if this is
            // GetInternalCorElementType or GetSignatureCorElementType
            type = typeHnd.GetSignatureCorElementType();

        }
        sigRet->retType = CEEInfo::asCorInfoType(type, typeHnd, &sigRet->retTypeClass);
        sigRet->retTypeSigClass = CORINFO_CLASS_HANDLE(typeHnd.AsPtr());

        IfFailThrow(sig.SkipExactlyOne());  // must to a skip so we skip any class tokens associated with the return type
        _ASSERTE(sigRet->retType < CORINFO_TYPE_COUNT);

        sigRet->args = (CORINFO_ARG_LIST_HANDLE)sig.GetPtr();
    }
    else
    {
        // This is local variables declaration
        sigRetFlags |= CORINFO_SIGFLAG_IS_LOCAL_SIG;

        sigRet->callConv = CORINFO_CALLCONV_DEFAULT;
        sigRet->retType = CORINFO_TYPE_VOID;
        sigRet->numArgs = 0;
        if (!sig.IsNull())
        {
            uint32_t callConv;
            IfFailThrow(sig.GetCallingConvInfo(&callConv));
            if (callConv != IMAGE_CEE_CS_CALLCONV_LOCAL_SIG)
            {
                COMPlusThrowHR(COR_E_BADIMAGEFORMAT, BFA_CALLCONV_NOT_LOCAL_SIG);
            }

            uint32_t numArgs;
            IfFailThrow(sig.GetData(&numArgs));

            if (numArgs != (unsigned short) numArgs)
                COMPlusThrowHR(COR_E_INVALIDPROGRAM);

            sigRet->numArgs = (unsigned short) numArgs;
        }

        sigRet->args = (CORINFO_ARG_LIST_HANDLE)sig.GetPtr();
    }

    // Set computed flags
    sigRet->flags = sigRetFlags;

    _ASSERTE(SigInfoFlagsAreValid(sigRet));
} // ConvToJitSig

//---------------------------------------------------------------------------------------
//
CORINFO_CLASS_HANDLE CEEInfo::getTokenTypeAsHandle (CORINFO_RESOLVED_TOKEN * pResolvedToken)
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    } CONTRACTL_END;

    CORINFO_CLASS_HANDLE tokenType = NULL;

    JIT_TO_EE_TRANSITION();

    _ASSERTE((pResolvedToken->hMethod == NULL) || (pResolvedToken->hField == NULL));

    BinderClassID classID = CLASS__TYPE_HANDLE;

    if (pResolvedToken->hMethod != NULL)
    {
        classID = CLASS__METHOD_HANDLE;
    }
    else
    if (pResolvedToken->hField != NULL)
    {
        classID = CLASS__FIELD_HANDLE;
    }

    tokenType = CORINFO_CLASS_HANDLE(CoreLibBinder::GetClass(classID));

    EE_TO_JIT_TRANSITION();

    return tokenType;
}


int CEEInfo::getStringLiteral (
        CORINFO_MODULE_HANDLE       moduleHnd,
        mdToken                     metaTOK,
        char16_t*                   buffer,
        int                         bufferSize,
        int                         startIndex)
{
    CONTRACTL{
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    } CONTRACTL_END;

    Module* module = GetModule(moduleHnd);

    _ASSERTE(bufferSize >= 0);
    _ASSERTE(startIndex >= 0);

    int result = -1;

    JIT_TO_EE_TRANSITION();

    if (IsDynamicScope(moduleHnd))
    {
        GCX_COOP();
        STRINGREF strRef = GetDynamicResolver(moduleHnd)->GetStringLiteral(metaTOK);
        if (strRef != NULL)
        {
            StringObject* strObj = STRINGREFToObject(strRef);
            int length = (int)strObj->GetStringLength();
            result = (length >= startIndex) ? (length - startIndex) : 0;
            if (buffer != NULL && result != 0)
            {
                memcpyNoGCRefs(buffer, strObj->GetBuffer() + startIndex, min(bufferSize, result) * sizeof(char16_t));
            }
        }
    }
    else
    {
        ULONG dwCharCount;
        LPCWSTR pString;
        if (!FAILED((module)->GetMDImport()->GetUserString(metaTOK, &dwCharCount, NULL, &pString)))
        {
            _ASSERTE(dwCharCount >= 0 && dwCharCount <= INT_MAX);
            int length = (int)dwCharCount;
            result = (length >= startIndex) ? (length - startIndex) : 0;
            if (buffer != NULL && result != 0)
            {
                memcpyNoGCRefs(buffer, pString + startIndex, min(bufferSize, result) * sizeof(char16_t));
            }
        }
    }

    EE_TO_JIT_TRANSITION();

    return result;
}

size_t CEEInfo::printObjectDescription (
        CORINFO_OBJECT_HANDLE  handle,
        char*                  buffer,
        size_t                 bufferSize,
        size_t*                pRequiredBufferSize)
{
    CONTRACTL{
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    } CONTRACTL_END;

    size_t bytesWritten = 0;

    _ASSERT(handle != nullptr);

    JIT_TO_EE_TRANSITION();

    GCX_COOP();
    OBJECTREF obj = getObjectFromJitHandle(handle);
    StackSString stackStr;

    // Currently only supported for String and RuntimeType
    if (obj->GetMethodTable()->IsString())
    {
        ((STRINGREF)obj)->GetSString(stackStr);
    }
    else if (obj->GetMethodTable() == g_pRuntimeTypeClass)
    {
        ((REFLECTCLASSBASEREF)obj)->GetType().GetName(stackStr);
    }
    else
    {
        obj->GetMethodTable()->_GetFullyQualifiedNameForClass(stackStr);
    }

    const UTF8* utf8data = stackStr.GetUTF8();
    if (bufferSize > 0)
    {
        bytesWritten = min<size_t>(bufferSize - 1, stackStr.GetCount());
        memcpy((BYTE*)buffer, (BYTE*)utf8data, bytesWritten);

        // Always null-terminate
        buffer[bytesWritten] = 0;
    }

    if (pRequiredBufferSize != nullptr)
    {
        *pRequiredBufferSize = stackStr.GetCount() + 1;
    }

    EE_TO_JIT_TRANSITION();

    return bytesWritten;
}

/* static */
size_t CEEInfo::findNameOfToken (Module* module,
                                                 mdToken metaTOK,
                                                 _Out_writes_ (FQNameCapacity) char * szFQName,
                                                 size_t FQNameCapacity)
{
    CONTRACTL {
        NOTHROW;
        GC_TRIGGERS;
    } CONTRACTL_END;

#ifdef _DEBUG
    PCCOR_SIGNATURE sig = NULL;
    DWORD           cSig;
    LPCUTF8         pszNamespace = NULL;
    LPCUTF8         pszClassName = NULL;

    mdToken tokType = TypeFromToken(metaTOK);
    switch(tokType)
    {
        case mdtTypeRef:
            {
                if (FAILED(module->GetMDImport()->GetNameOfTypeRef(metaTOK, &pszNamespace, &pszClassName)))
                {
                    pszNamespace = pszClassName = "Invalid TypeRef record";
                }
                ns::MakePath(szFQName, (int)FQNameCapacity, pszNamespace, pszClassName);
                break;
            }
        case mdtTypeDef:
            {
                if (FAILED(module->GetMDImport()->GetNameOfTypeDef(metaTOK, &pszClassName, &pszNamespace)))
                {
                    pszClassName = pszNamespace = "Invalid TypeDef record";
                }
                ns::MakePath(szFQName, (int)FQNameCapacity, pszNamespace, pszClassName);
                break;
            }
        case mdtFieldDef:
            {
                LPCSTR szFieldName;
                if (FAILED(module->GetMDImport()->GetNameOfFieldDef(metaTOK, &szFieldName)))
                {
                    szFieldName = "Invalid FieldDef record";
                }
                strncpy_s(szFQName,  FQNameCapacity,  (char*)szFieldName, FQNameCapacity - 1);
                break;
            }
        case mdtMethodDef:
            {
                LPCSTR szMethodName;
                if (FAILED(module->GetMDImport()->GetNameOfMethodDef(metaTOK, &szMethodName)))
                {
                    szMethodName = "Invalid MethodDef record";
                }
                strncpy_s(szFQName, FQNameCapacity, (char*)szMethodName, FQNameCapacity - 1);
                break;
            }
        case mdtMemberRef:
            {
                LPCSTR szName;
                if (FAILED(module->GetMDImport()->GetNameAndSigOfMemberRef((mdMemberRef)metaTOK, &sig, &cSig, &szName)))
                {
                    szName = "Invalid MemberRef record";
                }
                strncpy_s(szFQName, FQNameCapacity, (char *)szName, FQNameCapacity - 1);
                break;
            }
        default:
            sprintf_s(szFQName, FQNameCapacity, "!TK_%x", metaTOK);
            break;
    }

#else // !_DEBUG
    strncpy_s (szFQName, FQNameCapacity, "<UNKNOWN>", FQNameCapacity - 1);
#endif // _DEBUG


    return strlen (szFQName);
}

CorInfoHelpFunc CEEInfo::getLazyStringLiteralHelper(CORINFO_MODULE_HANDLE handle)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_PREEMPTIVE;
    } CONTRACTL_END;

    CorInfoHelpFunc result = CORINFO_HELP_UNDEF;

    JIT_TO_EE_TRANSITION_LEAF();

    result = IsDynamicScope(handle) ? CORINFO_HELP_UNDEF : CORINFO_HELP_STRCNS;

    EE_TO_JIT_TRANSITION_LEAF();

    return result;
}


CHECK CheckContext(CORINFO_MODULE_HANDLE scopeHnd, CORINFO_CONTEXT_HANDLE context)
{
    if (context != METHOD_BEING_COMPILED_CONTEXT())
    {
        CHECK_MSG(scopeHnd != NULL, "Illegal null scope");
        CHECK_MSG(((size_t)context & ~CORINFO_CONTEXTFLAGS_MASK) != NULL, "Illegal null context");
        if (((size_t)context & CORINFO_CONTEXTFLAGS_MASK) == CORINFO_CONTEXTFLAGS_CLASS)
        {
            TypeHandle handle((CORINFO_CLASS_HANDLE)((size_t)context & ~CORINFO_CONTEXTFLAGS_MASK));
            CHECK_MSG(handle.GetModule() == GetModule(scopeHnd), "Inconsistent scope and context");
        }
        else
        {
            MethodDesc* handle = (MethodDesc*)((size_t)context & ~CORINFO_CONTEXTFLAGS_MASK);
            CHECK_MSG(handle->GetModule() == GetModule(scopeHnd), "Inconsistent scope and context");
        }
    }

    CHECK_OK;
}


static DECLSPEC_NORETURN void ThrowBadTokenException(CORINFO_RESOLVED_TOKEN * pResolvedToken)
{
    switch (pResolvedToken->tokenType & CORINFO_TOKENKIND_Mask)
    {
    case CORINFO_TOKENKIND_Class:
        COMPlusThrowHR(COR_E_BADIMAGEFORMAT, BFA_BAD_CLASS_TOKEN);
    case CORINFO_TOKENKIND_Method:
        COMPlusThrowHR(COR_E_BADIMAGEFORMAT, BFA_INVALID_METHOD_TOKEN);
    case CORINFO_TOKENKIND_Field:
        COMPlusThrowHR(COR_E_BADIMAGEFORMAT, BFA_BAD_FIELD_TOKEN);
    default:
        COMPlusThrowHR(COR_E_BADIMAGEFORMAT);
    }
}

/*********************************************************************/
void CEEInfo::resolveToken(/* IN, OUT */ CORINFO_RESOLVED_TOKEN * pResolvedToken)
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    } CONTRACTL_END;

    JIT_TO_EE_TRANSITION();

    _ASSERTE(CheckContext(pResolvedToken->tokenScope, pResolvedToken->tokenContext));

    pResolvedToken->pTypeSpec = NULL;
    pResolvedToken->cbTypeSpec = NULL;
    pResolvedToken->pMethodSpec = NULL;
    pResolvedToken->cbMethodSpec = NULL;

    TypeHandle th;
    MethodDesc * pMD = NULL;
    FieldDesc * pFD = NULL;

    CorInfoTokenKind tokenType = pResolvedToken->tokenType;

    if (IsDynamicScope(pResolvedToken->tokenScope))
    {
        GetDynamicResolver(pResolvedToken->tokenScope)->ResolveToken(pResolvedToken->token, &th, &pMD, &pFD);

        //
        // Check that we got the expected handles and fill in missing data if necessary
        //

        CorTokenType tkType = (CorTokenType)TypeFromToken(pResolvedToken->token);

        if (pMD != NULL)
        {
            if ((tkType != mdtMethodDef) && (tkType != mdtMemberRef))
                ThrowBadTokenException(pResolvedToken);
            if ((tokenType & CORINFO_TOKENKIND_Method) == 0)
                ThrowBadTokenException(pResolvedToken);
            if (th.IsNull())
                th = pMD->GetMethodTable();

            // "PermitUninstDefOrRef" check
            if ((tokenType != CORINFO_TOKENKIND_Ldtoken) && pMD->ContainsGenericVariables())
            {
                COMPlusThrow(kInvalidProgramException);
            }

            // if this is a BoxedEntryPointStub get the UnboxedEntryPoint one
            if (pMD->IsUnboxingStub())
            {
                pMD = pMD->GetMethodTable()->GetUnboxedEntryPointMD(pMD);
            }

            // Activate target if required
            if (tokenType != CORINFO_TOKENKIND_Ldtoken)
            {
                EnsureActive(th, pMD);
            }
        }
        else if (pFD != NULL)
        {
            if ((tkType != mdtFieldDef) && (tkType != mdtMemberRef))
                ThrowBadTokenException(pResolvedToken);
            if ((tokenType & CORINFO_TOKENKIND_Field) == 0)
                ThrowBadTokenException(pResolvedToken);
            if (th.IsNull())
                th = pFD->GetApproxEnclosingMethodTable();

            if (pFD->IsStatic() && (tokenType != CORINFO_TOKENKIND_Ldtoken))
            {
                EnsureActive(th);
            }
        }
        else
        {
            if ((tkType != mdtTypeDef) && (tkType != mdtTypeRef))
                ThrowBadTokenException(pResolvedToken);
            if ((tokenType & CORINFO_TOKENKIND_Class) == 0)
                ThrowBadTokenException(pResolvedToken);
            if (th.IsNull())
                ThrowBadTokenException(pResolvedToken);

            if (tokenType == CORINFO_TOKENKIND_Box || tokenType == CORINFO_TOKENKIND_Constrained)
            {
                EnsureActive(th);
            }
        }

        _ASSERTE((pMD == NULL) || (pFD == NULL));
        _ASSERTE(!th.IsNull());

        // "PermitUninstDefOrRef" check
        if ((tokenType != CORINFO_TOKENKIND_Ldtoken) && th.ContainsGenericVariables())
        {
            COMPlusThrow(kInvalidProgramException);
        }
    }
    else
    {
        mdToken metaTOK = pResolvedToken->token;
        Module * pModule = (Module *)pResolvedToken->tokenScope;

        switch (TypeFromToken(metaTOK))
        {
        case mdtModuleRef:
            if ((tokenType & CORINFO_TOKENKIND_Class) == 0)
                ThrowBadTokenException(pResolvedToken);

            {
                DomainAssembly *pTargetModule = pModule->LoadModule(metaTOK);
                if (pTargetModule == NULL)
                    COMPlusThrowHR(COR_E_BADIMAGEFORMAT);
                th = TypeHandle(pTargetModule->GetModule()->GetGlobalMethodTable());
                if (th.IsNull())
                    COMPlusThrowHR(COR_E_BADIMAGEFORMAT);
            }
            break;

        case mdtTypeDef:
        case mdtTypeRef:
            if ((tokenType & CORINFO_TOKENKIND_Class) == 0)
                ThrowBadTokenException(pResolvedToken);

            th = ClassLoader::LoadTypeDefOrRefThrowing(pModule, metaTOK,
                                         ClassLoader::ThrowIfNotFound,
                                         (tokenType == CORINFO_TOKENKIND_Ldtoken) ?
                                            ClassLoader::PermitUninstDefOrRef : ClassLoader::FailIfUninstDefOrRef);
            break;

        case mdtTypeSpec:
            {
                if ((tokenType & CORINFO_TOKENKIND_Class) == 0)
                    ThrowBadTokenException(pResolvedToken);

                IfFailThrow(pModule->GetMDImport()->GetTypeSpecFromToken(metaTOK, &pResolvedToken->pTypeSpec, (ULONG*)&pResolvedToken->cbTypeSpec));

                SigTypeContext typeContext;
                GetTypeContext(pResolvedToken->tokenContext, &typeContext);

                SigPointer sigptr(pResolvedToken->pTypeSpec, pResolvedToken->cbTypeSpec);
                th = sigptr.GetTypeHandleThrowing(pModule, &typeContext);
            }
            break;

        case mdtMethodDef:
            if ((tokenType & CORINFO_TOKENKIND_Method) == 0)
                ThrowBadTokenException(pResolvedToken);

            pMD = MemberLoader::GetMethodDescFromMethodDef(pModule, metaTOK, (tokenType != CORINFO_TOKENKIND_Ldtoken));

            th = pMD->GetMethodTable();
            break;

        case mdtFieldDef:
            if ((tokenType & CORINFO_TOKENKIND_Field) == 0)
                ThrowBadTokenException(pResolvedToken);

            pFD = MemberLoader::GetFieldDescFromFieldDef(pModule, metaTOK, (tokenType != CORINFO_TOKENKIND_Ldtoken));

            th = pFD->GetEnclosingMethodTable();
            break;

        case mdtMemberRef:
            {
                SigTypeContext typeContext;
                GetTypeContext(pResolvedToken->tokenContext, &typeContext);

                MemberLoader::GetDescFromMemberRef(pModule, metaTOK, &pMD, &pFD, &typeContext, (tokenType != CORINFO_TOKENKIND_Ldtoken),
                    &th, TRUE, &pResolvedToken->pTypeSpec, (ULONG*)&pResolvedToken->cbTypeSpec);

                _ASSERTE((pMD != NULL) ^ (pFD != NULL));
                _ASSERTE(!th.IsNull());

                if (pMD != NULL)
                {
                    if ((tokenType & CORINFO_TOKENKIND_Method) == 0)
                        ThrowBadTokenException(pResolvedToken);
                }
                else
                {
                    if ((tokenType & CORINFO_TOKENKIND_Field) == 0)
                        ThrowBadTokenException(pResolvedToken);
                }
            }
            break;

        case mdtMethodSpec:
            {
                if ((tokenType & CORINFO_TOKENKIND_Method) == 0)
                    ThrowBadTokenException(pResolvedToken);

                SigTypeContext typeContext;
                GetTypeContext(pResolvedToken->tokenContext, &typeContext);

                // We need the method desc to carry exact instantiation, thus allowInstParam == FALSE.
                pMD = MemberLoader::GetMethodDescFromMethodSpec(pModule, metaTOK, &typeContext, (tokenType != CORINFO_TOKENKIND_Ldtoken), FALSE /* allowInstParam */,
                    &th, TRUE, &pResolvedToken->pTypeSpec, (ULONG*)&pResolvedToken->cbTypeSpec, &pResolvedToken->pMethodSpec, (ULONG*)&pResolvedToken->cbMethodSpec);
            }
            break;

        default:
            ThrowBadTokenException(pResolvedToken);
        }

        //
        // Module activation tracking
        //
        if (!pModule->IsSystem())
        {
            if (pMD != NULL)
            {
                EnsureActive(th, pMD);
            }
            else
            if (pFD != NULL)
            {
                if (pFD->IsStatic())
                    EnsureActive(th);
            }
            else
            {
                // It should not be required to trigger the modules cctors for ldtoken, it is done for backward compatibility only.
                if (tokenType == CORINFO_TOKENKIND_Box || tokenType == CORINFO_TOKENKIND_Constrained || tokenType == CORINFO_TOKENKIND_Ldtoken)
                    EnsureActive(th);
            }
        }
    }

    //
    // tokenType specific verification and transformations
    //
    CorElementType et = th.GetInternalCorElementType();
    switch (tokenType)
    {
        case CORINFO_TOKENKIND_Ldtoken:
            // Allow everything.
            break;

        case CORINFO_TOKENKIND_Newarr:
            // Disallow ELEMENT_TYPE_BYREF and ELEMENT_TYPE_VOID
            if (et == ELEMENT_TYPE_BYREF || et == ELEMENT_TYPE_VOID)
                COMPlusThrow(kInvalidProgramException);

            th = ClassLoader::LoadArrayTypeThrowing(th);
            break;

        default:
            // Disallow ELEMENT_TYPE_BYREF and ELEMENT_TYPE_VOID
            if (et == ELEMENT_TYPE_BYREF || et == ELEMENT_TYPE_VOID)
                COMPlusThrow(kInvalidProgramException);
            break;
    }

    // The JIT interface should always return fully loaded types
    _ASSERTE(th.IsFullyLoaded());

    pResolvedToken->hClass = CORINFO_CLASS_HANDLE(th.AsPtr());
    pResolvedToken->hMethod = CORINFO_METHOD_HANDLE(pMD);
    pResolvedToken->hField = CORINFO_FIELD_HANDLE(pFD);

    EE_TO_JIT_TRANSITION();
}


/*********************************************************************/
// We have a few frequently used constants in CoreLib that are defined as
// readonly static fields for historic reasons. Check for them here and
// allow them to be treated as actual constants by the JIT.
static CORINFO_FIELD_ACCESSOR getFieldIntrinsic(FieldDesc * field)
{
    STANDARD_VM_CONTRACT;

    if (CoreLibBinder::GetField(FIELD__STRING__EMPTY) == field)
    {
        return CORINFO_FIELD_INTRINSIC_EMPTY_STRING;
    }
    else
    if ((CoreLibBinder::GetField(FIELD__INTPTR__ZERO) == field) ||
        (CoreLibBinder::GetField(FIELD__UINTPTR__ZERO) == field))
    {
        return CORINFO_FIELD_INTRINSIC_ZERO;
    }
    else
    if (CoreLibBinder::GetField(FIELD__BITCONVERTER__ISLITTLEENDIAN) == field)
    {
        return CORINFO_FIELD_INTRINSIC_ISLITTLEENDIAN;
    }

    return (CORINFO_FIELD_ACCESSOR)-1;
}

static CorInfoHelpFunc getGenericStaticsHelper(FieldDesc * pField)
{
    STANDARD_VM_CONTRACT;

    int helper = CORINFO_HELP_GETGENERICS_NONGCSTATIC_BASE;

    if (pField->GetFieldType() == ELEMENT_TYPE_CLASS ||
        pField->GetFieldType() == ELEMENT_TYPE_VALUETYPE)
    {
        helper = CORINFO_HELP_GETGENERICS_GCSTATIC_BASE;
    }

    if (pField->IsThreadStatic())
    {
        const int delta = CORINFO_HELP_GETGENERICS_GCTHREADSTATIC_BASE - CORINFO_HELP_GETGENERICS_GCSTATIC_BASE;

        static_assert_no_msg(CORINFO_HELP_GETGENERICS_NONGCTHREADSTATIC_BASE
            == CORINFO_HELP_GETGENERICS_NONGCSTATIC_BASE + delta);

        helper += (CORINFO_HELP_GETGENERICS_GCTHREADSTATIC_BASE - CORINFO_HELP_GETGENERICS_GCSTATIC_BASE);
    }

    return (CorInfoHelpFunc)helper;
}

CorInfoHelpFunc CEEInfo::getSharedStaticsHelper(FieldDesc * pField, MethodTable * pFieldMT)
{
    STANDARD_VM_CONTRACT;

    int helper = CORINFO_HELP_GETSHARED_NONGCSTATIC_BASE;

    if (pField->GetFieldType() == ELEMENT_TYPE_CLASS ||
        pField->GetFieldType() == ELEMENT_TYPE_VALUETYPE)
    {
        helper = CORINFO_HELP_GETSHARED_GCSTATIC_BASE;
    }

    if (pFieldMT->IsDynamicStatics())
    {
        const int delta = CORINFO_HELP_GETSHARED_GCSTATIC_BASE_DYNAMICCLASS - CORINFO_HELP_GETSHARED_GCSTATIC_BASE;

        static_assert_no_msg(CORINFO_HELP_GETSHARED_NONGCSTATIC_BASE_DYNAMICCLASS
            == CORINFO_HELP_GETSHARED_NONGCSTATIC_BASE + delta);

        helper += delta;
    }
    else
    if ((!pFieldMT->HasClassConstructor() && !pFieldMT->HasBoxedRegularStatics()) || pFieldMT->IsClassInited())
    {
        const int delta = CORINFO_HELP_GETSHARED_GCSTATIC_BASE_NOCTOR - CORINFO_HELP_GETSHARED_GCSTATIC_BASE;

        static_assert_no_msg(CORINFO_HELP_GETSHARED_NONGCSTATIC_BASE_NOCTOR
            == CORINFO_HELP_GETSHARED_NONGCSTATIC_BASE + delta);

        helper += delta;
    }

    if (pField->IsThreadStatic())
    {
        const int delta = CORINFO_HELP_GETSHARED_GCTHREADSTATIC_BASE - CORINFO_HELP_GETSHARED_GCSTATIC_BASE;

        static_assert_no_msg(CORINFO_HELP_GETSHARED_NONGCTHREADSTATIC_BASE
            == CORINFO_HELP_GETSHARED_NONGCSTATIC_BASE + delta);
        static_assert_no_msg(CORINFO_HELP_GETSHARED_GCTHREADSTATIC_BASE_NOCTOR
            == CORINFO_HELP_GETSHARED_GCSTATIC_BASE_NOCTOR + delta);
        static_assert_no_msg(CORINFO_HELP_GETSHARED_NONGCTHREADSTATIC_BASE_NOCTOR
            == CORINFO_HELP_GETSHARED_NONGCSTATIC_BASE_NOCTOR + delta);
        static_assert_no_msg(CORINFO_HELP_GETSHARED_GCTHREADSTATIC_BASE_DYNAMICCLASS
            == CORINFO_HELP_GETSHARED_GCSTATIC_BASE_DYNAMICCLASS + delta);
        static_assert_no_msg(CORINFO_HELP_GETSHARED_NONGCTHREADSTATIC_BASE_DYNAMICCLASS
            == CORINFO_HELP_GETSHARED_NONGCSTATIC_BASE_DYNAMICCLASS + delta);

        helper += delta;
    }

    return (CorInfoHelpFunc)helper;
}

static CorInfoHelpFunc getInstanceFieldHelper(FieldDesc * pField, CORINFO_ACCESS_FLAGS flags)
{
    STANDARD_VM_CONTRACT;

    int helper;

    CorElementType type = pField->GetFieldType();

    if (CorTypeInfo::IsObjRef(type))
        helper = CORINFO_HELP_GETFIELDOBJ;
    else
    switch (type)
    {
    case ELEMENT_TYPE_VALUETYPE:
        helper = CORINFO_HELP_GETFIELDSTRUCT;
        break;
    case ELEMENT_TYPE_I1:
    case ELEMENT_TYPE_BOOLEAN:
    case ELEMENT_TYPE_U1:
        helper = CORINFO_HELP_GETFIELD8;
        break;
    case ELEMENT_TYPE_I2:
    case ELEMENT_TYPE_CHAR:
    case ELEMENT_TYPE_U2:
        helper = CORINFO_HELP_GETFIELD16;
        break;
    case ELEMENT_TYPE_I4:
    case ELEMENT_TYPE_U4:
    IN_TARGET_32BIT(default:)
        helper = CORINFO_HELP_GETFIELD32;
        break;
    case ELEMENT_TYPE_I8:
    case ELEMENT_TYPE_U8:
    IN_TARGET_64BIT(default:)
        helper = CORINFO_HELP_GETFIELD64;
        break;
    case ELEMENT_TYPE_R4:
        helper = CORINFO_HELP_GETFIELDFLOAT;
        break;
    case ELEMENT_TYPE_R8:
        helper = CORINFO_HELP_GETFIELDDOUBLE;
        break;
    }

    if (flags & CORINFO_ACCESS_SET)
    {
        const int delta = CORINFO_HELP_SETFIELDOBJ - CORINFO_HELP_GETFIELDOBJ;

        static_assert_no_msg(CORINFO_HELP_SETFIELD8 == CORINFO_HELP_GETFIELD8 + delta);
        static_assert_no_msg(CORINFO_HELP_SETFIELD16 == CORINFO_HELP_GETFIELD16 + delta);
        static_assert_no_msg(CORINFO_HELP_SETFIELD32 == CORINFO_HELP_GETFIELD32 + delta);
        static_assert_no_msg(CORINFO_HELP_SETFIELD64 == CORINFO_HELP_GETFIELD64 + delta);
        static_assert_no_msg(CORINFO_HELP_SETFIELDSTRUCT == CORINFO_HELP_GETFIELDSTRUCT + delta);
        static_assert_no_msg(CORINFO_HELP_SETFIELDFLOAT == CORINFO_HELP_GETFIELDFLOAT + delta);
        static_assert_no_msg(CORINFO_HELP_SETFIELDDOUBLE == CORINFO_HELP_GETFIELDDOUBLE + delta);

        helper += delta;
    }

    return (CorInfoHelpFunc)helper;
}



/*********************************************************************/

void CEEInfo::getThreadLocalStaticInfo_NativeAOT(CORINFO_THREAD_STATIC_INFO_NATIVEAOT* pInfo)
{
    LIMITED_METHOD_CONTRACT;
    UNREACHABLE();      // only called with NativeAOT.
}

uint32_t CEEInfo::getThreadLocalFieldInfo (CORINFO_FIELD_HANDLE  field, bool isGCType)
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    } CONTRACTL_END;

    UINT32 typeIndex = 0;

    JIT_TO_EE_TRANSITION();

    FieldDesc* fieldDesc = (FieldDesc*)field;
    _ASSERTE(fieldDesc->IsThreadStatic());

    if (isGCType)
    {
        typeIndex = AppDomain::GetCurrentDomain()->GetGCThreadStaticTypeIndex(fieldDesc->GetEnclosingMethodTable());
    }
    else
    {
        typeIndex = AppDomain::GetCurrentDomain()->GetNonGCThreadStaticTypeIndex(fieldDesc->GetEnclosingMethodTable());
    }

    assert(typeIndex != TypeIDProvider::INVALID_TYPE_ID);

    EE_TO_JIT_TRANSITION();
    return typeIndex;
}

#if defined(TARGET_WINDOWS)
/*********************************************************************/
static uint32_t ThreadLocalOffset(void* p)
{
    PTEB Teb = NtCurrentTeb();
    uint8_t** pTls = (uint8_t**)Teb->ThreadLocalStoragePointer;
    uint8_t* pOurTls = pTls[_tls_index];
    return (uint32_t)((uint8_t*)p - pOurTls);
}
#elif defined(TARGET_OSX)
extern "C" void* GetThreadVarsAddress();

static void* GetThreadVarsSectionAddressFromDesc(uint8_t* p)
{
    _ASSERT(p[0] == 0x48 && p[1] == 0x8d && p[2] == 0x3d);

    // At this point, `p` contains the instruction pointer and is pointing to the above opcodes.
    // These opcodes are patched by the dynamic linker.
    // Move beyond the opcodes that we have already checked above.
    p += 3;

    // The descriptor address is located at *p at this point.
    // (p + 4) below skips the descriptor address bytes embedded in the instruction and
    // add it to the `instruction pointer` to find out the address.
    return *(uint32_t*)p + (p + 4);
}

static void* GetThreadVarsSectionAddress()
{
#ifdef TARGET_AMD64
    // On x64, the address is related to rip, so, disassemble the function,
    // read the offset, and then relative to the IP, find the final address of
    // __thread_vars section.
    uint8_t* p = reinterpret_cast<uint8_t*>(&GetThreadVarsAddress);
    return GetThreadVarsSectionAddressFromDesc(p);
#else
    return GetThreadVarsAddress();
#endif // TARGET_AMD64
}

#else

// Linux

#ifdef TARGET_AMD64

extern "C" void* GetTlsIndexObjectDescOffset();

static void* GetThreadStaticDescriptor(uint8_t* p)
{
    if (!(p[0] == 0x66 && p[1] == 0x48 && p[2] == 0x8d && p[3] == 0x3d))
    {
        // The optimization is disabled if coreclr is not compiled in .so format.
        _ASSERTE(false && "Unexpected code sequence");
        return nullptr;
    }

    // At this point, `p` contains the instruction pointer and is pointing to the above opcodes.
    // These opcodes are patched by the dynamic linker.
    // Move beyond the opcodes that we have already checked above.
    p += 4;

    // The descriptor address is located at *p at this point. Read that and add
    // it to the instruction pointer to locate the address of `ti` that will be used
    // to pass to __tls_get_addr during execution.
    // (p + 4) below skips the descriptor address bytes embedded in the instruction and
    // add it to the `instruction pointer` to find out the address.
    return *(uint32_t*)p + (p + 4);
}

static void* GetTlsIndexObjectAddress()
{
    uint8_t* p = reinterpret_cast<uint8_t*>(&GetTlsIndexObjectDescOffset);
    return GetThreadStaticDescriptor(p);
}

#elif defined(TARGET_ARM64) || defined(TARGET_LOONGARCH64) || defined(TARGET_RISCV64)

extern "C" size_t GetThreadStaticsVariableOffset();

#endif // TARGET_ARM64 || TARGET_LOONGARCH64 || TARGET_RISCV64
#endif // TARGET_WINDOWS


void CEEInfo::getThreadLocalStaticBlocksInfo (CORINFO_THREAD_STATIC_BLOCKS_INFO* pInfo, bool isGCType)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_PREEMPTIVE;
    } CONTRACTL_END;

    JIT_TO_EE_TRANSITION_LEAF();

    size_t threadStaticBaseOffset = 0;

#if defined(TARGET_WINDOWS)
    pInfo->tlsIndex.addr = (void*)static_cast<uintptr_t>(_tls_index);
    pInfo->tlsIndex.accessType = IAT_VALUE;

    pInfo->offsetOfThreadLocalStoragePointer = offsetof(_TEB, ThreadLocalStoragePointer);
    threadStaticBaseOffset = ThreadLocalOffset(&t_ThreadStatics);

#elif defined(TARGET_OSX)

    pInfo->threadVarsSection = GetThreadVarsSectionAddress();

#elif defined(TARGET_AMD64)

    // For Linux/x64, get the address of tls_get_addr system method and the base address
    // of struct that we will pass to it.
    pInfo->tlsGetAddrFtnPtr = reinterpret_cast<void*>(&__tls_get_addr);
    pInfo->tlsIndexObject = GetTlsIndexObjectAddress();

#elif defined(TARGET_ARM64) || defined(TARGET_LOONGARCH64) || defined(TARGET_RISCV64)

    // For Linux arm64/loongarch64/riscv64, just get the offset of thread static variable, and during execution,
    // this offset, arm64 taken from trpid_elp0 system register gives back the thread variable address.
    // this offset, loongarch64 taken from $tp register gives back the thread variable address.
    threadStaticBaseOffset = GetThreadStaticsVariableOffset();

#else
    _ASSERTE_MSG(false, "Unsupported scenario of optimizing TLS access on Linux Arm32/x86");
#endif // TARGET_WINDOWS

    if (isGCType)
    {
        pInfo->offsetOfMaxThreadStaticBlocks = (uint32_t)(threadStaticBaseOffset + offsetof(ThreadStaticBlockInfo, GCMaxThreadStaticBlocks));
        pInfo->offsetOfThreadStaticBlocks = (uint32_t)(threadStaticBaseOffset + offsetof(ThreadStaticBlockInfo, GCThreadStaticBlocks));
    }
    else
    {
        pInfo->offsetOfMaxThreadStaticBlocks = (uint32_t)(threadStaticBaseOffset + offsetof(ThreadStaticBlockInfo, NonGCMaxThreadStaticBlocks));
        pInfo->offsetOfThreadStaticBlocks = (uint32_t)(threadStaticBaseOffset + offsetof(ThreadStaticBlockInfo, NonGCThreadStaticBlocks));
    }
    pInfo->offsetOfGCDataPointer = static_cast<uint32_t>(PtrArray::GetDataOffset());

    EE_TO_JIT_TRANSITION_LEAF();
}

/*********************************************************************/
void CEEInfo::getFieldInfo (CORINFO_RESOLVED_TOKEN * pResolvedToken,
                            CORINFO_METHOD_HANDLE  callerHandle,
                            CORINFO_ACCESS_FLAGS   flags,
                            CORINFO_FIELD_INFO    *pResult
                           )
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    } CONTRACTL_END;

    JIT_TO_EE_TRANSITION();

    _ASSERTE((flags & (CORINFO_ACCESS_GET | CORINFO_ACCESS_SET | CORINFO_ACCESS_ADDRESS | CORINFO_ACCESS_INIT_ARRAY)) != 0);

    INDEBUG(memset(pResult, 0xCC, sizeof(*pResult)));

    FieldDesc * pField = (FieldDesc*)pResolvedToken->hField;
    MethodTable * pFieldMT = pField->GetApproxEnclosingMethodTable();

    // Helper to use if the field access requires it
    CORINFO_FIELD_ACCESSOR fieldAccessor = (CORINFO_FIELD_ACCESSOR)-1;
    DWORD fieldFlags = 0;

    pResult->offset = pField->GetOffset();
    pResult->fieldLookup.addr = nullptr;

    if (pField->IsStatic())
    {
        fieldFlags |= CORINFO_FLG_FIELD_STATIC;

        if (pField->IsRVA())
        {
            fieldFlags |= CORINFO_FLG_FIELD_UNMANAGED;

            Module* module = pFieldMT->GetModule();
            if (module->IsRvaFieldTls(pResult->offset))
            {
                fieldAccessor = CORINFO_FIELD_STATIC_TLS;

                // Provide helper to use if the JIT is not able to emit the TLS access
                // as intrinsic
                pResult->helper = CORINFO_HELP_GETSTATICFIELDADDR_TLS;

                pResult->offset = module->GetFieldTlsOffset(pResult->offset);
            }
            else
            {
                fieldAccessor = CORINFO_FIELD_STATIC_RVA_ADDRESS;
                pResult->fieldLookup.addr = pField->GetStaticAddressHandle(NULL);
                pResult->fieldLookup.accessType = IAT_VALUE;
            }

            // We are not going through a helper. The constructor has to be triggered explicitly.
            if (!pFieldMT->IsClassInited())
                fieldFlags |= CORINFO_FLG_FIELD_INITCLASS;
        }
        else
        {
            // Regular or thread static
            CORINFO_FIELD_ACCESSOR intrinsicAccessor;

            if (pField->GetFieldType() == ELEMENT_TYPE_VALUETYPE)
                fieldFlags |= CORINFO_FLG_FIELD_STATIC_IN_HEAP;

            if (pFieldMT->IsSharedByGenericInstantiations())
            {
                if (pField->IsEnCNew())
                {
                    fieldAccessor = CORINFO_FIELD_STATIC_ADDR_HELPER;

                    pResult->helper = CORINFO_HELP_GETSTATICFIELDADDR;
                }
                else
                {
                    fieldAccessor = CORINFO_FIELD_STATIC_GENERICS_STATIC_HELPER;

                    pResult->helper = getGenericStaticsHelper(pField);
                }
            }
            else
            if (pFieldMT->GetModule()->IsSystem() && (flags & CORINFO_ACCESS_GET) &&
                (intrinsicAccessor = getFieldIntrinsic(pField)) != (CORINFO_FIELD_ACCESSOR)-1)
            {
                // Intrinsics
                fieldAccessor = intrinsicAccessor;
            }
            else
            if (pFieldMT->Collectible())
            {
                // Static fields are not pinned in collectible types. We will always access
                // them using a helper since the address cannot be embedded into the code.
                fieldAccessor = CORINFO_FIELD_STATIC_SHARED_STATIC_HELPER;

                pResult->helper = getSharedStaticsHelper(pField, pFieldMT);
            }
            else if (pField->IsThreadStatic())
            {
                 // We always treat accessing thread statics as if we are in domain neutral code.
                fieldAccessor = CORINFO_FIELD_STATIC_SHARED_STATIC_HELPER;

                pResult->helper = getSharedStaticsHelper(pField, pFieldMT);
#if defined(TARGET_ARM)
                // Optimization is disabled for linux/windows arm
#elif !defined(TARGET_WINDOWS) && defined(TARGET_X86)
                // Optimization is disabled for linux/x86
#elif defined(TARGET_LINUX_MUSL) && defined(TARGET_ARM64)
                // Optimization is disabled for linux musl arm64
#elif defined(TARGET_FREEBSD) && defined(TARGET_ARM64)
                // Optimization is disabled for FreeBSD/arm64
#else
                bool optimizeThreadStaticAccess = true;
#if !defined(TARGET_OSX) && defined(TARGET_UNIX) && defined(TARGET_AMD64)
                // For linux/x64, check if compiled coreclr as .so file and not single file.
                // For single file, the `tls_index` might not be accurate.
                // Do not perform this optimization in such case.
                optimizeThreadStaticAccess = GetTlsIndexObjectAddress() != nullptr;
#endif // !TARGET_OSX && TARGET_UNIX && TARGET_AMD64

                if (optimizeThreadStaticAccess)
                {
                    // For windows x64/x86/arm64, linux x64/arm64/loongarch64/riscv64:
                    // We convert the TLS access to the optimized helper where we will store
                    // the static blocks in TLS directly and access them via inline code.
                    if ((pResult->helper == CORINFO_HELP_GETSHARED_NONGCTHREADSTATIC_BASE_NOCTOR) ||
                        (pResult->helper == CORINFO_HELP_GETSHARED_NONGCTHREADSTATIC_BASE))
                    {
                        fieldAccessor = CORINFO_FIELD_STATIC_TLS_MANAGED;
                        pResult->helper = CORINFO_HELP_GETSHARED_NONGCTHREADSTATIC_BASE_NOCTOR_OPTIMIZED;
                    }
                    else if ((pResult->helper == CORINFO_HELP_GETSHARED_GCTHREADSTATIC_BASE_NOCTOR) ||
                                (pResult->helper == CORINFO_HELP_GETSHARED_GCTHREADSTATIC_BASE))
                    {
                        fieldAccessor = CORINFO_FIELD_STATIC_TLS_MANAGED;
                        pResult->helper = CORINFO_HELP_GETSHARED_GCTHREADSTATIC_BASE_NOCTOR_OPTIMIZED;
                    }
                }
#endif // TARGET_ARM
            }
            else
            {
                fieldAccessor = CORINFO_FIELD_STATIC_ADDRESS;

                // Allocate space for the local class if necessary, but don't trigger
                // class construction.
                DomainLocalModule* pLocalModule = pFieldMT->GetDomainLocalModule();
                pLocalModule->PopulateClass(pFieldMT);

                // We are not going through a helper. The constructor has to be triggered explicitly.
                if (!pFieldMT->IsClassInited())
                    fieldFlags |= CORINFO_FLG_FIELD_INITCLASS;

                GCX_COOP();

                _ASSERT(!pFieldMT->Collectible());
                // Field address is expected to be pinned so we don't need to protect it from GC here
                pResult->fieldLookup.addr = pField->GetStaticAddressHandle((void*)pField->GetBase());
                pResult->fieldLookup.accessType = IAT_VALUE;
                if (fieldFlags & CORINFO_FLG_FIELD_STATIC_IN_HEAP)
                {
                    Object* frozenObj = VolatileLoad((Object**)pResult->fieldLookup.addr);

                    if (frozenObj == nullptr)
                    {
                        // Boxed static is not yet set, allocate it
                        pFieldMT->AllocateRegularStaticBox(pField, (Object**)pResult->fieldLookup.addr);
                        frozenObj = VolatileLoad((Object**)pResult->fieldLookup.addr);
                    }

                    _ASSERT(frozenObj != nullptr);

                    // ContainsPointers here is unnecessary but it's cheaper than IsInFrozenSegment
                    // for structs containing gc handles
                    if (!frozenObj->GetMethodTable()->ContainsPointers() &&
                        GCHeapUtilities::GetGCHeap()->IsInFrozenSegment(frozenObj))
                    {
                        pResult->fieldLookup.addr = frozenObj->GetData();
                        fieldFlags &= ~CORINFO_FLG_FIELD_STATIC_IN_HEAP;
                    }
                }
            }
        }
    }
    else
    {
        if (pField->IsEnCNew())
        {
            fieldAccessor = CORINFO_FIELD_INSTANCE_ADDR_HELPER;

            pResult->helper = CORINFO_HELP_GETFIELDADDR;
        }
        else
        {
            fieldAccessor = CORINFO_FIELD_INSTANCE;
        }

        // FieldDesc::GetOffset() does not include the size of Object
        if (!pFieldMT->IsValueType())
        {
            pResult->offset += OBJECT_SIZE;
        }
    }

    // TODO: This is touching metadata. Can we avoid it?
    DWORD fieldAttribs = pField->GetAttributes();

    if (IsFdInitOnly(fieldAttribs))
        fieldFlags |= CORINFO_FLG_FIELD_FINAL;

    pResult->fieldAccessor = fieldAccessor;
    pResult->fieldFlags = fieldFlags;

    if (!(flags & CORINFO_ACCESS_INLINECHECK))
    {
        //get the field's type.  Grab the class for structs.
        pResult->fieldType = getFieldTypeInternal(pResolvedToken->hField, &pResult->structType, pResolvedToken->hClass);


        MethodDesc * pCallerForSecurity = GetMethodForSecurity(callerHandle);

        //
        //Since we can't get the special verify-only instantiated FD like we can with MDs, go back to the parent
        //of the memberRef and load that one.  That should give us the open instantiation.
        //
        //If the field we found is owned by a generic type, you have to go back to the signature and reload.
        //Otherwise we filled in !0.
        TypeHandle fieldTypeForSecurity = TypeHandle(pResolvedToken->hClass);
        if (pResolvedToken->pTypeSpec != NULL)
        {
            SigTypeContext typeContext;
            SigTypeContext::InitTypeContext(pCallerForSecurity, &typeContext);

            SigPointer sigptr(pResolvedToken->pTypeSpec, pResolvedToken->cbTypeSpec);
            fieldTypeForSecurity = sigptr.GetTypeHandleThrowing((Module *)pResolvedToken->tokenScope, &typeContext);

            // typeHnd can be a variable type
            if (fieldTypeForSecurity.GetMethodTable() == NULL)
            {
                COMPlusThrowHR(COR_E_BADIMAGEFORMAT, BFA_METHODDEF_PARENT_NO_MEMBERS);
            }
        }

        BOOL doAccessCheck = TRUE;
        AccessCheckOptions::AccessCheckType accessCheckType = AccessCheckOptions::kNormalAccessibilityChecks;

        DynamicResolver * pAccessContext = NULL;

        //More in code:CEEInfo::getCallInfo, but the short version is that the caller and callee Descs do
        //not completely describe the type.
        TypeHandle callerTypeForSecurity = TypeHandle(pCallerForSecurity->GetMethodTable());
        if (IsDynamicScope(pResolvedToken->tokenScope))
        {
            doAccessCheck = ModifyCheckForDynamicMethod(GetDynamicResolver(pResolvedToken->tokenScope), &callerTypeForSecurity,
                &accessCheckType, &pAccessContext);
        }

        //Now for some link time checks.
        //Um... where are the field link demands?

        pResult->accessAllowed = CORINFO_ACCESS_ALLOWED;

        if (doAccessCheck)
        {
            //Well, let's check some visibility at least.
            AccessCheckOptions accessCheckOptions(accessCheckType,
                pAccessContext,
                FALSE,
                pField);

            _ASSERTE(pCallerForSecurity != NULL && callerTypeForSecurity != NULL);
            AccessCheckContext accessContext(pCallerForSecurity, callerTypeForSecurity.GetMethodTable());

            BOOL canAccess = ClassLoader::CanAccess(
                &accessContext,
                fieldTypeForSecurity.GetMethodTable(),
                fieldTypeForSecurity.GetAssembly(),
                fieldAttribs,
                NULL,
                (flags & CORINFO_ACCESS_INIT_ARRAY) ? NULL : pField, // For InitializeArray, we don't need tocheck the type of the field.
                accessCheckOptions);

            if (!canAccess)
            {
                //Set up the throw helper
                pResult->accessAllowed = CORINFO_ACCESS_ILLEGAL;

                pResult->accessCalloutHelper.helperNum = CORINFO_HELP_FIELD_ACCESS_EXCEPTION;
                pResult->accessCalloutHelper.numArgs = 2;

                pResult->accessCalloutHelper.args[0].Set(CORINFO_METHOD_HANDLE(pCallerForSecurity));
                pResult->accessCalloutHelper.args[1].Set(CORINFO_FIELD_HANDLE(pField));
            }
        }
    }

    EE_TO_JIT_TRANSITION();
}

//---------------------------------------------------------------------------------------
//
bool CEEInfo::isFieldStatic(CORINFO_FIELD_HANDLE fldHnd)
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    } CONTRACTL_END;

    bool res = false;
    JIT_TO_EE_TRANSITION_LEAF();
    FieldDesc* field = (FieldDesc*)fldHnd;
    res = (field->IsStatic() != 0);
    EE_TO_JIT_TRANSITION_LEAF();
    return res;
}

int CEEInfo::getArrayOrStringLength(CORINFO_OBJECT_HANDLE objHnd)
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    } CONTRACTL_END;

    int arrLen = -1;
    JIT_TO_EE_TRANSITION();

    _ASSERT(objHnd != NULL);

    GCX_COOP();

    OBJECTREF obj = getObjectFromJitHandle(objHnd);

    if (obj->GetMethodTable()->IsArray())
    {
        arrLen = ((ARRAYBASEREF)obj)->GetNumComponents();
    }
    else if (obj->GetMethodTable()->IsString())
    {
        arrLen = ((STRINGREF)obj)->GetStringLength();
    }

    EE_TO_JIT_TRANSITION();
    return arrLen;
}

//---------------------------------------------------------------------------------------
//
void
CEEInfo::findCallSiteSig(
    CORINFO_MODULE_HANDLE  scopeHnd,
    unsigned               sigMethTok,
    CORINFO_CONTEXT_HANDLE context,
    CORINFO_SIG_INFO *     sigRet)
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    } CONTRACTL_END;

    JIT_TO_EE_TRANSITION();

    PCCOR_SIGNATURE       pSig = NULL;
    uint32_t              cbSig = 0;

    if (IsDynamicScope(scopeHnd))
    {
        DynamicResolver * pResolver = GetDynamicResolver(scopeHnd);
        SigPointer sig;

        if (TypeFromToken(sigMethTok) == mdtMemberRef)
        {
            sig = pResolver->ResolveSignatureForVarArg(sigMethTok);
        }
        else
        {
            _ASSERTE(TypeFromToken(sigMethTok) == mdtMethodDef);

            TypeHandle classHandle;
            MethodDesc * pMD = NULL;
            FieldDesc * pFD = NULL;

            // in this case a method is asked for its sig. Resolve the method token and get the sig
            pResolver->ResolveToken(sigMethTok, &classHandle, &pMD, &pFD);
            if (pMD == NULL)
                COMPlusThrow(kInvalidProgramException);

            PCCOR_SIGNATURE pSig = NULL;
            DWORD           cbSig;
            pMD->GetSig(&pSig, &cbSig);
            sig = SigPointer(pSig, cbSig);

            context = MAKE_METHODCONTEXT(pMD);
            scopeHnd = GetScopeHandle(pMD->GetModule());
        }

        sig.GetSignature(&pSig, &cbSig);
        sigMethTok = mdTokenNil;
    }
    else
    {
        Module * module = (Module *)scopeHnd;
        LPCUTF8  szName;

        if (TypeFromToken(sigMethTok) == mdtMemberRef)
        {
            IfFailThrow(module->GetMDImport()->GetNameAndSigOfMemberRef(sigMethTok, &pSig, (ULONG*)&cbSig, &szName));
        }
        else if (TypeFromToken(sigMethTok) == mdtMethodDef)
        {
            IfFailThrow(module->GetMDImport()->GetSigOfMethodDef(sigMethTok, (ULONG*)&cbSig, &pSig));
        }
    }

    SigTypeContext typeContext;
    GetTypeContext(context, &typeContext);

    ConvToJitSig(
        pSig,
        cbSig,
        scopeHnd,
        sigMethTok,
        &typeContext,
        CONV_TO_JITSIG_FLAGS_NONE,
        sigRet);
    EE_TO_JIT_TRANSITION();
} // CEEInfo::findCallSiteSig

//---------------------------------------------------------------------------------------
//
void
CEEInfo::findSig(
    CORINFO_MODULE_HANDLE  scopeHnd,
    unsigned               sigTok,
    CORINFO_CONTEXT_HANDLE context,
    CORINFO_SIG_INFO *     sigRet)
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    } CONTRACTL_END;

    JIT_TO_EE_TRANSITION();

    PCCOR_SIGNATURE       pSig = NULL;
    uint32_t              cbSig = 0;

    if (IsDynamicScope(scopeHnd))
    {
        SigPointer sig = GetDynamicResolver(scopeHnd)->ResolveSignature(sigTok);
        sig.GetSignature(&pSig, &cbSig);
        sigTok = mdTokenNil;
    }
    else
    {
        Module * module = (Module *)scopeHnd;

        // We need to resolve this stand alone sig
        IfFailThrow(module->GetMDImport()->GetSigFromToken(
            (mdSignature)sigTok,
            (ULONG*)(&cbSig),
            &pSig));
    }

    SigTypeContext typeContext;
    GetTypeContext(context, &typeContext);

    ConvToJitSig(
        pSig,
        cbSig,
        scopeHnd,
        sigTok,
        &typeContext,
        CONV_TO_JITSIG_FLAGS_NONE,
        sigRet);

    EE_TO_JIT_TRANSITION();
} // CEEInfo::findSig

//---------------------------------------------------------------------------------------
//
unsigned
CEEInfo::getClassSize(
    CORINFO_CLASS_HANDLE clsHnd)
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    } CONTRACTL_END;

    unsigned result = 0;

    JIT_TO_EE_TRANSITION_LEAF();

    TypeHandle VMClsHnd(clsHnd);
    result = VMClsHnd.GetSize();

    EE_TO_JIT_TRANSITION_LEAF();

    return result;
}

//---------------------------------------------------------------------------------------
//
// Get the size of a reference type as allocated on the heap. This includes the size of the fields
// (and any padding between the fields) and the size of a method table pointer but doesn't include
// object header size or any padding for minimum size.
unsigned
CEEInfo::getHeapClassSize(
    CORINFO_CLASS_HANDLE clsHnd)
{
    CONTRACTL{
        NOTHROW;
        GC_NOTRIGGER;
        MODE_PREEMPTIVE;
    } CONTRACTL_END;

    unsigned result = 0;

    JIT_TO_EE_TRANSITION_LEAF();

    TypeHandle VMClsHnd(clsHnd);
    MethodTable* pMT = VMClsHnd.GetMethodTable();
    _ASSERTE(pMT);
    _ASSERTE(!pMT->IsValueType());
    _ASSERTE(!pMT->HasComponentSize());

    // Add OBJECT_SIZE to account for method table pointer.
    result = pMT->GetNumInstanceFieldBytes() + OBJECT_SIZE;

    EE_TO_JIT_TRANSITION_LEAF();
    return result;
}

//---------------------------------------------------------------------------------------
//
// Return TRUE if an object of this type can be allocated on the stack.
bool CEEInfo::canAllocateOnStack(CORINFO_CLASS_HANDLE clsHnd)
{
    CONTRACTL{
        NOTHROW;
        GC_NOTRIGGER;
        MODE_PREEMPTIVE;
    } CONTRACTL_END;

    bool result = false;

    JIT_TO_EE_TRANSITION_LEAF();

    TypeHandle VMClsHnd(clsHnd);
    MethodTable* pMT = VMClsHnd.GetMethodTable();
    _ASSERTE(pMT);
    _ASSERTE(!pMT->IsValueType());

    result = !pMT->HasFinalizer();

    EE_TO_JIT_TRANSITION_LEAF();
    return result;
}

unsigned CEEInfo::getClassAlignmentRequirement(CORINFO_CLASS_HANDLE type, bool fDoubleAlignHint)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_PREEMPTIVE;
    } CONTRACTL_END;

    // Default alignment is sizeof(void*)
    unsigned result = TARGET_POINTER_SIZE;

    JIT_TO_EE_TRANSITION_LEAF();

    TypeHandle clsHnd(type);

#ifdef FEATURE_DOUBLE_ALIGNMENT_HINT
    if (fDoubleAlignHint)
    {
        MethodTable* pMT = clsHnd.GetMethodTable();
        if (pMT != NULL)
        {
            // Return the size of the double align hint. Ignore the actual alignment info account
            // so that structs with 64-bit integer fields do not trigger double aligned frames on x86.
            if (pMT->GetClass()->IsAlign8Candidate())
                result = 8;
        }
    }
    else
#endif
    {
        result = getClassAlignmentRequirementStatic(clsHnd);
    }

    EE_TO_JIT_TRANSITION_LEAF();

    return result;
}

unsigned CEEInfo::getClassAlignmentRequirementStatic(TypeHandle clsHnd)
{
    LIMITED_METHOD_CONTRACT;

    // Default alignment is sizeof(void*)
    unsigned result = TARGET_POINTER_SIZE;

    MethodTable * pMT = clsHnd.GetMethodTable();
    if (pMT == NULL)
        return result;

    if (pMT->HasLayout())
    {
        EEClassLayoutInfo* pInfo = pMT->GetLayoutInfo();

        if (clsHnd.IsNativeValueType())
        {
            // if it's the unmanaged view of the managed type, we always use the unmanaged alignment requirement
            result = pMT->GetNativeLayoutInfo()->GetLargestAlignmentRequirement();
        }
        else if (pInfo->IsManagedSequential() || pInfo->IsBlittable())
        {
            _ASSERTE(!pMT->ContainsPointers());

            // if it's managed sequential, we use the managed alignment requirement
            result = pInfo->m_ManagedLargestAlignmentRequirementOfAllMembers;
        }
    }

#ifdef FEATURE_64BIT_ALIGNMENT
    if (result < 8 && pMT->RequiresAlign8())
    {
        // If the structure contains 64-bit primitive fields and the platform requires 8-byte alignment for
        // such fields then make sure we return at least 8-byte alignment. Note that it's technically possible
        // to create unmanaged APIs that take unaligned structures containing such fields and this
        // unconditional alignment bump would cause us to get the calling convention wrong on platforms such
        // as ARM. If we see such cases in the future we'd need to add another control (such as an alignment
        // property for the StructLayout attribute or a marshaling directive attribute for p/invoke arguments)
        // that allows more precise control. For now we'll go with the likely scenario.
        result = 8;
    }
#endif // FEATURE_64BIT_ALIGNMENT

    return result;
}

CORINFO_FIELD_HANDLE
CEEInfo::getFieldInClass(CORINFO_CLASS_HANDLE clsHnd, INT num)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_PREEMPTIVE;
    } CONTRACTL_END;

    CORINFO_FIELD_HANDLE result = NULL;

    JIT_TO_EE_TRANSITION_LEAF();

    TypeHandle VMClsHnd(clsHnd);

    MethodTable* pMT= VMClsHnd.AsMethodTable();

    result = (CORINFO_FIELD_HANDLE) ((pMT->GetApproxFieldDescListRaw()) + num);

    EE_TO_JIT_TRANSITION_LEAF();

    return result;
}

static GetTypeLayoutResult GetTypeLayoutHelper(
    MethodTable* pMT,
    unsigned parentIndex,
    unsigned baseOffs,
    FieldDesc* pFD,
    CORINFO_TYPE_LAYOUT_NODE* treeNodes,
    size_t maxTreeNodes,
    size_t* numTreeNodes)
{
    STANDARD_VM_CONTRACT;

    if (*numTreeNodes >= maxTreeNodes)
    {
        return GetTypeLayoutResult::Partial;
    }

    unsigned structNodeIndex = (unsigned)(*numTreeNodes)++;
    CORINFO_TYPE_LAYOUT_NODE& parNode = treeNodes[structNodeIndex];
    parNode.simdTypeHnd = NULL;
    parNode.diagFieldHnd = CORINFO_FIELD_HANDLE(pFD);
    parNode.parent = parentIndex;
    parNode.offset = baseOffs;
    parNode.size = pMT->GetNumInstanceFieldBytes();
    parNode.numFields = 0;
    parNode.type = CorInfoType::CORINFO_TYPE_VALUECLASS;

    EEClass* pClass = pMT->GetClass();
    parNode.hasSignificantPadding = pClass->HasExplicitFieldOffsetLayout() || pClass->HasExplicitSize();

    // The intrinsic SIMD/HW SIMD types have a lot of fields that the JIT does
    // not care about since they are considered primitives by the JIT.
    if (pMT->IsIntrinsicType())
    {
        const char* nsName;
        pMT->GetFullyQualifiedNameInfo(&nsName);

        if ((strcmp(nsName, "System.Runtime.Intrinsics") == 0) ||
            (strcmp(nsName, "System.Numerics") == 0))
        {
            parNode.simdTypeHnd = CORINFO_CLASS_HANDLE(pMT);
            if (parentIndex != UINT32_MAX)
            {
                return GetTypeLayoutResult::Success;
            }
        }
    }

    ApproxFieldDescIterator fieldIterator(pMT, ApproxFieldDescIterator::INSTANCE_FIELDS);
    for (FieldDesc* pFD = fieldIterator.Next(); pFD != NULL; pFD = fieldIterator.Next())
    {
        CorElementType fieldType = pFD->GetFieldType();
        parNode.numFields++;

        if (fieldType == ELEMENT_TYPE_VALUETYPE)
        {
            MethodTable* pFieldMT = pFD->GetApproxFieldTypeHandleThrowing().AsMethodTable();
            GetTypeLayoutResult result = GetTypeLayoutHelper(pFieldMT, structNodeIndex, baseOffs + pFD->GetOffset(), pFD, treeNodes, maxTreeNodes, numTreeNodes);
            if (result != GetTypeLayoutResult::Success)
            {
                return result;
            }
        }
        else
        {
            if (*numTreeNodes >= maxTreeNodes)
            {
                return GetTypeLayoutResult::Partial;
            }

            CorInfoType corInfoType = CEEInfo::asCorInfoType(fieldType);
            _ASSERTE(corInfoType != CORINFO_TYPE_UNDEF);

            CORINFO_TYPE_LAYOUT_NODE& treeNode = treeNodes[(*numTreeNodes)++];
            treeNode.simdTypeHnd = NULL;
            treeNode.diagFieldHnd = CORINFO_FIELD_HANDLE(pFD);
            treeNode.parent = structNodeIndex;
            treeNode.offset = baseOffs + pFD->GetOffset();
            treeNode.size = GetSizeForCorElementType(fieldType);
            treeNode.numFields = 0;
            treeNode.type = corInfoType;
            treeNode.hasSignificantPadding = false;
        }

        if (pMT->GetClass()->IsInlineArray())
        {
            size_t treeNodeEnd = *numTreeNodes;
            uint32_t elemSize = pFD->GetSize();
            uint32_t arrSize = pMT->GetNumInstanceFieldBytes();

            // Number of fields added for each element, including all
            // subfields. For example, for ValueTuple<int, int>[4]:
            // [ 0]: InlineArray
            // [ 1]:   ValueTuple<int, int>  parent = 0          -
            // [ 2]:     int                 parent = 1          |
            // [ 3]:     int                 parent = 1          |
            // [ 4]:   ValueTuple<int, int>  parent = 0          - stride = 3
            // [ 5]:     int                 parent = 4
            // [ 6]:     int                 parent = 4
            // [ 7]:   ValueTuple<int, int>  parent = 0
            // [ 8]:     int                 parent = 7
            // [ 9]:     int                 parent = 7
            // [10]:   ValueTuple<int, int>  parent = 0
            // [11]:     int                 parent = 10
            // [12]:     int                 parent = 10

            unsigned elemFieldsStride = (unsigned)*numTreeNodes - (structNodeIndex + 1);

            // Now duplicate the fields of the previous entry for each
            // additional element. For each entry we have to update the offset
            // and the parent index.
            for (uint32_t elemOffset = elemSize; elemOffset < arrSize; elemOffset += elemSize)
            {
                size_t prevElemStart = *numTreeNodes - elemFieldsStride;
                for (size_t i = 0; i < elemFieldsStride; i++)
                {
                    if (*numTreeNodes >= maxTreeNodes)
                    {
                        return GetTypeLayoutResult::Partial;
                    }

                    CORINFO_TYPE_LAYOUT_NODE& treeNode = treeNodes[(*numTreeNodes)++];
                    treeNode = treeNodes[prevElemStart + i];
                    treeNode.offset += elemSize;
                    // The first field points back to the inline array and has
                    // no bias; the rest of them do.
                    treeNode.parent += (i == 0) ? 0 : elemFieldsStride;
                }

                parNode.numFields++;
            }
        }
    }

    return GetTypeLayoutResult::Success;
}

GetTypeLayoutResult CEEInfo::getTypeLayout(
    CORINFO_CLASS_HANDLE clsHnd,
    CORINFO_TYPE_LAYOUT_NODE* treeNodes,
    size_t* numTreeNodes)
{
    STANDARD_VM_CONTRACT;

    GetTypeLayoutResult result = GetTypeLayoutResult::Failure;

    JIT_TO_EE_TRANSITION_LEAF();

    TypeHandle typeHnd(clsHnd);

    if (typeHnd.IsNativeValueType())
    {
        if (*numTreeNodes > 0)
        {
            *numTreeNodes = 1;
            treeNodes[0].simdTypeHnd = NULL;
            treeNodes[0].diagFieldHnd = NULL;
            treeNodes[0].parent = UINT32_MAX;
            treeNodes[0].offset = 0;
            treeNodes[0].size = typeHnd.GetSize();
            treeNodes[0].numFields = 0;
            treeNodes[0].type = CORINFO_TYPE_VALUECLASS;
            treeNodes[0].hasSignificantPadding = true;
            result = GetTypeLayoutResult::Success;
        }
        else
        {
            result = GetTypeLayoutResult::Partial;
        }
    }
    else if (typeHnd.IsValueType())
    {
        MethodTable* pMT = typeHnd.AsMethodTable();

        size_t maxTreeNodes = *numTreeNodes;
        *numTreeNodes = 0;
        result = GetTypeLayoutHelper(pMT, UINT32_MAX, 0, NULL, treeNodes, maxTreeNodes, numTreeNodes);
    }

    EE_TO_JIT_TRANSITION_LEAF();

    return result;
}

mdMethodDef
CEEInfo::getMethodDefFromMethod(CORINFO_METHOD_HANDLE hMethod)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_PREEMPTIVE;
    } CONTRACTL_END;

    mdMethodDef result = 0;

    JIT_TO_EE_TRANSITION_LEAF();

    MethodDesc* pMD = GetMethod(hMethod);

    if (pMD->IsDynamicMethod())
    {
        // Dynamic methods do not have tokens
        result = mdMethodDefNil;
    }
    else
    {
        result = pMD->GetMemberDef();
    }

    EE_TO_JIT_TRANSITION_LEAF();

    return result;
}

bool CEEInfo::checkMethodModifier(CORINFO_METHOD_HANDLE hMethod,
                                  LPCSTR modifier,
                                  bool fOptional)
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    } CONTRACTL_END;

    bool result = false;

    JIT_TO_EE_TRANSITION();

    MethodDesc* pMD = GetMethod(hMethod);
    Module* pModule = pMD->GetModule();
    MetaSig sig(pMD);
    CorElementType eeType = fOptional ? ELEMENT_TYPE_CMOD_OPT : ELEMENT_TYPE_CMOD_REQD;

    // modopts/modreqs for the method are by convention stored on the return type
    result = sig.GetReturnProps().HasCustomModifier(pModule, modifier, eeType);

    EE_TO_JIT_TRANSITION();

    return result;
}

static unsigned MarkGCField(BYTE* gcPtrs, CorInfoGCType type)
{
    STANDARD_VM_CONTRACT;

    // Ensure that if we have multiple fields with the same offset,
    // that we don't double count the data in the gc layout.
    if (*gcPtrs == TYPE_GC_NONE)
    {
        *gcPtrs = (BYTE)type;
        return 1;
    }
    else if (*gcPtrs != type)
    {
        COMPlusThrowHR(COR_E_BADIMAGEFORMAT);
    }

    return 0;
}

static unsigned ComputeGCLayout(MethodTable* pMT, BYTE* gcPtrs)
{
    STANDARD_VM_CONTRACT;

    _ASSERTE(pMT->IsValueType());

    unsigned result = 0;
    bool isInlineArray = pMT->GetClass()->IsInlineArray();
    ApproxFieldDescIterator fieldIterator(pMT, ApproxFieldDescIterator::INSTANCE_FIELDS);
    for (FieldDesc *pFD = fieldIterator.Next(); pFD != NULL; pFD = fieldIterator.Next())
    {
        int fieldStartIndex = pFD->GetOffset() / TARGET_POINTER_SIZE;

        if (pFD->GetFieldType() == ELEMENT_TYPE_VALUETYPE)
        {
            MethodTable* pFieldMT = pFD->GetApproxFieldTypeHandleThrowing().AsMethodTable();
            result += ComputeGCLayout(pFieldMT, gcPtrs + fieldStartIndex);
        }
        else if (pFD->IsObjRef())
        {
            result += MarkGCField(gcPtrs + fieldStartIndex, TYPE_GC_REF);
        }
        else if (pFD->IsByRef())
        {
            result += MarkGCField(gcPtrs + fieldStartIndex, TYPE_GC_BYREF);
        }

        if (isInlineArray)
        {
            if (result > 0)
            {
                _ASSERTE(pFD->GetOffset() == 0);
                DWORD totalLayoutSize = pMT->GetNumInstanceFieldBytes() / TARGET_POINTER_SIZE;
                DWORD elementLayoutSize = pFD->GetSize() / TARGET_POINTER_SIZE;
                DWORD gcPointersInElement = result;
                for (DWORD offset = elementLayoutSize; offset < totalLayoutSize; offset += elementLayoutSize)
                {
                    memcpy(gcPtrs + offset, gcPtrs, elementLayoutSize);
                    result += gcPointersInElement;
                }
            }

            // inline array has only one element field
            break;
        }
    }
    return result;
}

unsigned CEEInfo::getClassGClayout (CORINFO_CLASS_HANDLE clsHnd, BYTE* gcPtrs)
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    } CONTRACTL_END;

    unsigned result = 0;

    JIT_TO_EE_TRANSITION();

    TypeHandle VMClsHnd(clsHnd);
    result = getClassGClayoutStatic(VMClsHnd, gcPtrs);

    EE_TO_JIT_TRANSITION();

    return result;
}


unsigned CEEInfo::getClassGClayoutStatic(TypeHandle VMClsHnd, BYTE* gcPtrs)
{
    STANDARD_VM_CONTRACT;

    unsigned result = 0;
    MethodTable* pMT = VMClsHnd.GetMethodTable();

    if (VMClsHnd.IsNativeValueType())
    {
        // native value types have no GC pointers
        result = 0;
        memset(gcPtrs, TYPE_GC_NONE,
               (VMClsHnd.GetSize() + TARGET_POINTER_SIZE - 1) / TARGET_POINTER_SIZE);
    }
    else if (pMT->IsByRefLike())
    {
        memset(gcPtrs, TYPE_GC_NONE,
            (VMClsHnd.GetSize() + TARGET_POINTER_SIZE - 1) / TARGET_POINTER_SIZE);
        // ByRefLike structs can contain byref fields or other ByRefLike structs
        result = ComputeGCLayout(VMClsHnd.AsMethodTable(), gcPtrs);
    }
    else
    {
        _ASSERTE(sizeof(BYTE) == 1);

        BOOL isValueClass = pMT->IsValueType();
        unsigned int size = isValueClass ? VMClsHnd.GetSize() : pMT->GetNumInstanceFieldBytes() + OBJECT_SIZE;

        // assume no GC pointers at first
        result = 0;
        memset(gcPtrs, TYPE_GC_NONE,
               (size + TARGET_POINTER_SIZE - 1) / TARGET_POINTER_SIZE);

        // walk the GC descriptors, turning on the correct bits
        if (pMT->ContainsPointers())
        {
            CGCDesc* map = CGCDesc::GetCGCDescFromMT(pMT);
            CGCDescSeries * pByValueSeries = map->GetLowestSeries();

            for (SIZE_T i = 0; i < map->GetNumSeries(); i++)
            {
                // Get offset into the value class of the first pointer field (includes a +Object)
                size_t cbSeriesSize = pByValueSeries->GetSeriesSize() + pMT->GetBaseSize();
                size_t cbSeriesOffset = pByValueSeries->GetSeriesOffset();
                size_t cbOffset = isValueClass ? cbSeriesOffset - OBJECT_SIZE : cbSeriesOffset;

                _ASSERTE (cbOffset % TARGET_POINTER_SIZE == 0);
                _ASSERTE (cbSeriesSize % TARGET_POINTER_SIZE == 0);

                result += (unsigned) (cbSeriesSize / TARGET_POINTER_SIZE);
                memset(&gcPtrs[cbOffset / TARGET_POINTER_SIZE], TYPE_GC_REF, cbSeriesSize / TARGET_POINTER_SIZE);

                pByValueSeries++;
            }
        }
    }

    return result;
}

// returns the enregister info for a struct based on type of fields, alignment, etc.
bool CEEInfo::getSystemVAmd64PassStructInRegisterDescriptor(
                                                /*IN*/  CORINFO_CLASS_HANDLE structHnd,
                                                /*OUT*/ SYSTEMV_AMD64_CORINFO_STRUCT_REG_PASSING_DESCRIPTOR* structPassInRegDescPtr)
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    } CONTRACTL_END;

#if defined(UNIX_AMD64_ABI_ITF)
    JIT_TO_EE_TRANSITION();

    _ASSERTE(structPassInRegDescPtr != nullptr);
    TypeHandle th(structHnd);

    structPassInRegDescPtr->passedInRegisters = false;

    // Make sure this is a value type.
    if (th.IsValueType())
    {
        _ASSERTE(CorInfoType2UnixAmd64Classification(th.GetInternalCorElementType()) == SystemVClassificationTypeStruct);

        // The useNativeLayout in this case tracks whether the classification
        // is for a native layout of the struct or not.
        // If the struct has special marshaling it has a native layout.
        // In such cases the classifier needs to use the native layout.
        // For structs with no native layout, the managed layout should be used
        // even if classified for the purposes of marshaling/PInvoke passing.
        bool useNativeLayout = false;
        MethodTable* methodTablePtr = nullptr;
        if (!th.IsTypeDesc())
        {
            methodTablePtr = th.AsMethodTable();
        }
        else
        {
            _ASSERTE(th.IsNativeValueType());

            useNativeLayout = true;
            methodTablePtr = th.AsNativeValueType();
        }
        _ASSERTE(methodTablePtr != nullptr);

        // If we have full support for UNIX_AMD64_ABI, and not just the interface,
        // then we've cached whether this is a reg passed struct in the MethodTable, computed during
        // MethodTable construction. Otherwise, we are just building in the interface, and we haven't
        // computed or cached anything, so we need to compute it now.
#if defined(UNIX_AMD64_ABI)
        bool canPassInRegisters = useNativeLayout ? methodTablePtr->GetNativeLayoutInfo()->IsNativeStructPassedInRegisters()
                                                  : methodTablePtr->IsRegPassedStruct();
#else // !defined(UNIX_AMD64_ABI)
        bool canPassInRegisters = false;
        SystemVStructRegisterPassingHelper helper((unsigned int)th.GetSize());
        if (th.GetSize() <= CLR_SYSTEMV_MAX_STRUCT_BYTES_TO_PASS_IN_REGISTERS)
        {
            canPassInRegisters = methodTablePtr->ClassifyEightBytes(&helper, 0, 0, useNativeLayout);
        }
#endif // !defined(UNIX_AMD64_ABI)

        if (canPassInRegisters)
        {
#if defined(UNIX_AMD64_ABI)
            SystemVStructRegisterPassingHelper helper((unsigned int)th.GetSize());
            bool result = methodTablePtr->ClassifyEightBytes(&helper, 0, 0, useNativeLayout);

            // The answer must be true at this point.
            _ASSERTE(result);
#endif // UNIX_AMD64_ABI

            structPassInRegDescPtr->passedInRegisters = true;

            structPassInRegDescPtr->eightByteCount = (uint8_t)helper.eightByteCount;
            _ASSERTE(structPassInRegDescPtr->eightByteCount <= CLR_SYSTEMV_MAX_EIGHTBYTES_COUNT_TO_PASS_IN_REGISTERS);

            for (unsigned int i = 0; i < CLR_SYSTEMV_MAX_EIGHTBYTES_COUNT_TO_PASS_IN_REGISTERS; i++)
            {
                structPassInRegDescPtr->eightByteClassifications[i] = helper.eightByteClassifications[i];
                structPassInRegDescPtr->eightByteSizes[i] = (uint8_t)helper.eightByteSizes[i];
                structPassInRegDescPtr->eightByteOffsets[i] = (uint8_t)helper.eightByteOffsets[i];
            }
        }

        _ASSERTE(structPassInRegDescPtr->passedInRegisters == canPassInRegisters);
    }

    EE_TO_JIT_TRANSITION();

    return true;
#else // !defined(UNIX_AMD64_ABI_ITF)
    return false;
#endif // !defined(UNIX_AMD64_ABI_ITF)
}

/*********************************************************************/
unsigned CEEInfo::getClassNumInstanceFields (CORINFO_CLASS_HANDLE clsHnd)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_PREEMPTIVE;
    } CONTRACTL_END;

    unsigned result = 0;

    JIT_TO_EE_TRANSITION_LEAF();

    TypeHandle th(clsHnd);

    if (!th.IsTypeDesc())
    {
        result = th.AsMethodTable()->GetNumInstanceFields();
    }
    else
    {
        // native value types are opaque aggregates with explicit size
        result = 0;
    }

    EE_TO_JIT_TRANSITION_LEAF();

    return result;
}


CorInfoType CEEInfo::asCorInfoType (CORINFO_CLASS_HANDLE clsHnd)
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    } CONTRACTL_END;

    CorInfoType result = CORINFO_TYPE_UNDEF;

    JIT_TO_EE_TRANSITION();

    TypeHandle VMClsHnd(clsHnd);
    result = toJitType(VMClsHnd);

    EE_TO_JIT_TRANSITION();

    return result;
}


void CEEInfo::getLocationOfThisType(CORINFO_METHOD_HANDLE context, CORINFO_LOOKUP_KIND* pLookupKind)
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    } CONTRACTL_END;

    /* Initialize fields of result for debug build warning */
    pLookupKind->needsRuntimeLookup = false;
    pLookupKind->runtimeLookupKind  = CORINFO_LOOKUP_THISOBJ;

    JIT_TO_EE_TRANSITION();

    MethodDesc *pContextMD = GetMethod(context);

    // If the method table is not shared, then return CONST
    if (!pContextMD->GetMethodTable()->IsSharedByGenericInstantiations())
    {
        pLookupKind->needsRuntimeLookup = false;
    }
    else
    {
        pLookupKind->needsRuntimeLookup = true;

        // If we've got a vtable extra argument, go through that
        if (pContextMD->RequiresInstMethodTableArg())
        {
            pLookupKind->runtimeLookupKind = CORINFO_LOOKUP_CLASSPARAM;
        }
        // If we've got an object, go through its vtable
        else if (pContextMD->AcquiresInstMethodTableFromThis())
        {
            pLookupKind->runtimeLookupKind = CORINFO_LOOKUP_THISOBJ;
        }
        // Otherwise go through the method-desc argument
        else
        {
            _ASSERTE(pContextMD->RequiresInstMethodDescArg());
            pLookupKind->runtimeLookupKind = CORINFO_LOOKUP_METHODPARAM;
        }
    }

    EE_TO_JIT_TRANSITION();
}

CORINFO_METHOD_HANDLE CEEInfo::GetDelegateCtor(
                                        CORINFO_METHOD_HANDLE methHnd,
                                        CORINFO_CLASS_HANDLE clsHnd,
                                        CORINFO_METHOD_HANDLE targetMethodHnd,
                                        DelegateCtorArgs *pCtorData)
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    } CONTRACTL_END;

    CORINFO_METHOD_HANDLE result = NULL;

    JIT_TO_EE_TRANSITION();

    MethodDesc *pCurrentCtor = (MethodDesc*)methHnd;
    if (!pCurrentCtor->IsFCall())
    {
        result =  methHnd;
    }
    else
    {
        MethodDesc *pTargetMethod = (MethodDesc*)targetMethodHnd;
        TypeHandle delegateType = (TypeHandle)clsHnd;

        MethodDesc *pDelegateCtor = COMDelegate::GetDelegateCtor(delegateType, pTargetMethod, pCtorData);
        if (!pDelegateCtor)
            pDelegateCtor = pCurrentCtor;
        result = (CORINFO_METHOD_HANDLE)pDelegateCtor;
    }

    EE_TO_JIT_TRANSITION();

    return result;
}

void CEEInfo::MethodCompileComplete(CORINFO_METHOD_HANDLE methHnd)
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    } CONTRACTL_END;

    JIT_TO_EE_TRANSITION();

    MethodDesc* pMD = GetMethod(methHnd);

    if (pMD->IsDynamicMethod())
    {
        pMD->AsDynamicMethodDesc()->GetResolver()->FreeCompileTimeState();
    }

    EE_TO_JIT_TRANSITION();
}

// Given a module scope (scopeHnd), a method handle (context) and an metadata token,
// attempt to load the handle (type, field or method) associated with the token.
// If this is not possible at compile-time (because the method code is shared and the token contains type parameters)
// then indicate how the handle should be looked up at run-time.
//
// See corinfo.h for more details
//
void CEEInfo::embedGenericHandle(
            CORINFO_RESOLVED_TOKEN * pResolvedToken,
            bool                     fEmbedParent,
            CORINFO_GENERICHANDLE_RESULT *pResult)
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    } CONTRACTL_END;

    INDEBUG(memset(pResult, 0xCC, sizeof(*pResult)));

    JIT_TO_EE_TRANSITION();

    BOOL fRuntimeLookup;
    MethodDesc * pTemplateMD = NULL;

    if (!fEmbedParent && pResolvedToken->hMethod != NULL)
    {
        MethodDesc * pMD = (MethodDesc *)pResolvedToken->hMethod;
        TypeHandle th(pResolvedToken->hClass);

        pResult->handleType = CORINFO_HANDLETYPE_METHOD;

        Instantiation methodInst = pMD->GetMethodInstantiation();

        pMD = MethodDesc::FindOrCreateAssociatedMethodDesc(pMD, th.GetMethodTable(), FALSE, methodInst, FALSE);

        // Normalize the method handle for reflection
        if (pResolvedToken->tokenType == CORINFO_TOKENKIND_Ldtoken)
            pMD = MethodDesc::FindOrCreateAssociatedMethodDescForReflection(pMD, th, methodInst);

        pResult->compileTimeHandle = (CORINFO_GENERIC_HANDLE)pMD;
        pTemplateMD = pMD;

        // Runtime lookup is only required for stubs. Regular entrypoints are always the same shared MethodDescs.
        fRuntimeLookup = pMD->IsWrapperStub() &&
            (th.IsSharedByGenericInstantiations() || TypeHandle::IsCanonicalSubtypeInstantiation(methodInst));
    }
    else
    if (!fEmbedParent && pResolvedToken->hField != NULL)
    {
        FieldDesc * pFD = (FieldDesc *)pResolvedToken->hField;
        TypeHandle th(pResolvedToken->hClass);

        pResult->handleType = CORINFO_HANDLETYPE_FIELD;

        pResult->compileTimeHandle = (CORINFO_GENERIC_HANDLE)pFD;

        fRuntimeLookup = th.IsSharedByGenericInstantiations() && pFD->IsStatic();
    }
    else
    {
        TypeHandle th(pResolvedToken->hClass);

        pResult->handleType = CORINFO_HANDLETYPE_CLASS;
        pResult->compileTimeHandle = (CORINFO_GENERIC_HANDLE)th.AsPtr();

        if (fEmbedParent && pResolvedToken->hMethod != NULL)
        {
            MethodDesc * pDeclaringMD = (MethodDesc *)pResolvedToken->hMethod;

            if (!pDeclaringMD->GetMethodTable()->HasSameTypeDefAs(th.GetMethodTable()) && !pDeclaringMD->GetMethodTable()->IsArray())
            {
                //
                // The method type may point to a sub-class of the actual class that declares the method.
                // It is important to embed the declaring type in this case.
                //

                pTemplateMD = pDeclaringMD;

                pResult->compileTimeHandle = (CORINFO_GENERIC_HANDLE)pDeclaringMD->GetMethodTable();
            }
        }

        // IsSharedByGenericInstantiations would not work here. The runtime lookup is required
        // even for standalone generic variables that show up as __Canon here.
        fRuntimeLookup = th.IsCanonicalSubtype();
    }

    _ASSERTE(pResult->compileTimeHandle);

    if (fRuntimeLookup)
    {
        DictionaryEntryKind entryKind = EmptySlot;
        switch (pResult->handleType)
        {
        case CORINFO_HANDLETYPE_CLASS:
            entryKind = (pTemplateMD != NULL) ? DeclaringTypeHandleSlot : TypeHandleSlot;
            break;
        case CORINFO_HANDLETYPE_METHOD:
            entryKind = MethodDescSlot;
            break;
        case CORINFO_HANDLETYPE_FIELD:
            entryKind = FieldDescSlot;
            break;
        default:
            _ASSERTE(false);
        }

        ComputeRuntimeLookupForSharedGenericToken(entryKind,
                                                  pResolvedToken,
                                                  NULL,
                                                  pTemplateMD,
                                                  &pResult->lookup);
    }
    else
    {
        // If the target is not shared then we've already got our result and
        // can simply do a static look up
        pResult->lookup.lookupKind.needsRuntimeLookup = false;

        pResult->lookup.constLookup.handle = pResult->compileTimeHandle;
        pResult->lookup.constLookup.accessType = IAT_VALUE;
    }

    EE_TO_JIT_TRANSITION();
}

//
// EnsureActive is used to track triggers for creation of per-AppDomain state instead, including allocations required for statics and
// triggering of module cctors.
//
// The basic rule is: There should be no possibility of a module that is "active" to have a direct call into a  module that
// is not "active". And we don't want to intercept every call during runtime, so during compile time we track static calls and
// everything that can result in new virtual calls.
//
// The current algorithm (scan the parent type chain and instantiation variables) is more than enough to maintain this invariant.
// One could come up with a more efficient algorithm that still maintains the invariant, but it may introduce backward compatibility
// issues.
//
void CEEInfo::EnsureActive(TypeHandle th, MethodDesc * pMD)
{
    STANDARD_VM_CONTRACT;

    if (pMD != NULL && pMD->HasMethodInstantiation())
        pMD->EnsureActive();
    if (!th.IsTypeDesc())
        th.AsMethodTable()->EnsureInstanceActive();
}

CORINFO_OBJECT_HANDLE CEEInfo::getJitHandleForObject(OBJECTREF objref, bool knownFrozen)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;

    Object* obj = OBJECTREFToObject(objref);
    if (knownFrozen || GCHeapUtilities::GetGCHeap()->IsInFrozenSegment(obj))
    {
        return (CORINFO_OBJECT_HANDLE)obj;
    }

    if (m_pJitHandles == nullptr)
    {
        m_pJitHandles = new SArray<OBJECTHANDLE>();
    }

    OBJECTHANDLEHolder handle = AppDomain::GetCurrentDomain()->CreateHandle(objref);
    m_pJitHandles->Append(handle);
    handle.SuppressRelease();

    // We know that handle is aligned so we use the lowest bit as a marker
    // "this is a handle, not a frozen object".
    return (CORINFO_OBJECT_HANDLE)((size_t)handle.GetValue() | 1);
}

OBJECTREF CEEInfo::getObjectFromJitHandle(CORINFO_OBJECT_HANDLE handle)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;

    size_t intHandle = (size_t)handle;
    if (intHandle & 1)
    {
        return ObjectToOBJECTREF(*(Object**)(intHandle - 1));
    }

    // Frozen object
    return ObjectToOBJECTREF((Object*)intHandle);
}

MethodDesc * CEEInfo::GetMethodForSecurity(CORINFO_METHOD_HANDLE callerHandle)
{
    STANDARD_VM_CONTRACT;

    // Cache the cast lookup
    if (callerHandle == m_hMethodForSecurity_Key)
    {
        return m_pMethodForSecurity_Value;
    }

    MethodDesc * pCallerMethod = (MethodDesc *)callerHandle;

    //If the caller is generic, load the open type and then load the field again,  This allows us to
    //differentiate between BadGeneric<T> containing a memberRef for a field of type InaccessibleClass and
    //GoodGeneric<T> containing a memberRef for a field of type T instantiated over InaccessibleClass.
    MethodDesc * pMethodForSecurity = pCallerMethod->IsILStub() ?
        pCallerMethod : pCallerMethod->LoadTypicalMethodDefinition();

    m_hMethodForSecurity_Key = callerHandle;
    m_pMethodForSecurity_Value = pMethodForSecurity;

    return pMethodForSecurity;
}

// Check that the instantiation is <!/!!0, ..., !/!!(n-1)>
static bool IsSignatureForTypicalInstantiation(SigPointer sigptr, CorElementType varType, ULONG ntypars)
{
    STANDARD_VM_CONTRACT;

    for (uint32_t i = 0; i < ntypars; i++)
    {
        CorElementType type;
        IfFailThrow(sigptr.GetElemType(&type));
        if (type != varType)
            return false;

        uint32_t data;
        IfFailThrow(sigptr.GetData(&data));

        if (data != i)
             return false;
    }

    return true;
}

// Check that methodSpec instantiation is <!!0, ..., !!(n-1)>
static bool IsMethodSpecForTypicalInstantiation(SigPointer sigptr)
{
    STANDARD_VM_CONTRACT;

    BYTE etype;
    IfFailThrow(sigptr.GetByte(&etype));
    _ASSERTE(etype == (BYTE)IMAGE_CEE_CS_CALLCONV_GENERICINST);

    uint32_t ntypars;
    IfFailThrow(sigptr.GetData(&ntypars));

    return IsSignatureForTypicalInstantiation(sigptr, ELEMENT_TYPE_MVAR, ntypars);
}

// Check that typeSpec instantiation is <!0, ..., !(n-1)>
static bool IsTypeSpecForTypicalInstantiation(SigPointer sigptr)
{
    STANDARD_VM_CONTRACT;

    CorElementType type;
    IfFailThrow(sigptr.GetElemType(&type));
    if (type != ELEMENT_TYPE_GENERICINST)
        return false;

    IfFailThrow(sigptr.SkipExactlyOne());

    uint32_t ntypars;
    IfFailThrow(sigptr.GetData(&ntypars));

    return IsSignatureForTypicalInstantiation(sigptr, ELEMENT_TYPE_VAR, ntypars);
}

void CEEInfo::ComputeRuntimeLookupForSharedGenericToken(DictionaryEntryKind entryKind,
                                                        CORINFO_RESOLVED_TOKEN * pResolvedToken,
                                                        CORINFO_RESOLVED_TOKEN * pConstrainedResolvedToken,
                                                        MethodDesc * pTemplateMD /* for method-based slots */,
                                                        CORINFO_LOOKUP *pResultLookup)
{
    CONTRACTL{
        STANDARD_VM_CHECK;
        PRECONDITION(CheckPointer(pResultLookup));
    } CONTRACTL_END;

    pResultLookup->lookupKind.needsRuntimeLookup = true;
    pResultLookup->lookupKind.runtimeLookupFlags = 0;

    CORINFO_RUNTIME_LOOKUP *pResult = &pResultLookup->runtimeLookup;
    pResult->signature = NULL;

    pResult->indirectFirstOffset = 0;
    pResult->indirectSecondOffset = 0;

    // Dictionary size checks skipped by default, unless we decide otherwise
    pResult->sizeOffset = CORINFO_NO_SIZE_CHECK;

    // Unless we decide otherwise, just do the lookup via a helper function
    pResult->indirections = CORINFO_USEHELPER;

    // Runtime lookups in inlined contexts are not supported by the runtime for now
    if (pResolvedToken->tokenContext != METHOD_BEING_COMPILED_CONTEXT())
    {
        pResultLookup->lookupKind.runtimeLookupKind = CORINFO_LOOKUP_NOT_SUPPORTED;
        return;
    }

    MethodDesc* pContextMD = GetMethodFromContext(pResolvedToken->tokenContext);
    MethodTable* pContextMT = pContextMD->GetMethodTable();
    bool isStaticVirtual = (pConstrainedResolvedToken != nullptr && pContextMD != nullptr && pContextMD->IsStatic());

    // There is a pathological case where invalid IL refereces __Canon type directly, but there is no dictionary availabled to store the lookup.
    if (!pContextMD->IsSharedByGenericInstantiations())
        COMPlusThrow(kInvalidProgramException);

    BOOL fInstrument = FALSE;

    if (pContextMD->RequiresInstMethodDescArg())
    {
        pResultLookup->lookupKind.runtimeLookupKind = CORINFO_LOOKUP_METHODPARAM;
    }
    else
    {
        if (pContextMD->RequiresInstMethodTableArg())
            pResultLookup->lookupKind.runtimeLookupKind = CORINFO_LOOKUP_CLASSPARAM;
        else
            pResultLookup->lookupKind.runtimeLookupKind = CORINFO_LOOKUP_THISOBJ;
    }

    // If we've got a  method type parameter of any kind then we must look in the method desc arg
    if (pContextMD->RequiresInstMethodDescArg())
    {
        pResult->helper = fInstrument ? CORINFO_HELP_RUNTIMEHANDLE_METHOD_LOG : CORINFO_HELP_RUNTIMEHANDLE_METHOD;

        if (fInstrument)
            goto NoSpecialCase;

        // Special cases:
        // (1) Naked method type variable: look up directly in instantiation hanging off runtime md
        // (2) Reference to method-spec of current method (e.g. a recursive call) i.e. currentmeth<!0,...,!(n-1)>
        if ((entryKind == TypeHandleSlot) && (pResolvedToken->tokenType != CORINFO_TOKENKIND_Newarr))
        {
            SigPointer sigptr(pResolvedToken->pTypeSpec, pResolvedToken->cbTypeSpec);
            CorElementType type;
            IfFailThrow(sigptr.GetElemType(&type));
            if (type == ELEMENT_TYPE_MVAR)
            {
                pResult->indirections = 2;
                pResult->testForNull = 0;
                pResult->offsets[0] = offsetof(InstantiatedMethodDesc, m_pPerInstInfo);

                uint32_t data;
                IfFailThrow(sigptr.GetData(&data));
                pResult->offsets[1] = sizeof(TypeHandle) * data;

                return;
            }
        }
        else if (entryKind == MethodDescSlot)
        {
            // It's the context itself (i.e. a recursive call)
            if (!pTemplateMD->HasSameMethodDefAs(pContextMD))
                goto NoSpecialCase;

            // Now just check that the instantiation is (!!0, ..., !!(n-1))
            if (!IsMethodSpecForTypicalInstantiation(SigPointer(pResolvedToken->pMethodSpec, pResolvedToken->cbMethodSpec)))
                goto NoSpecialCase;

            // Type instantiation has to match too if there is one
            if (pContextMT->HasInstantiation())
            {
                TypeHandle thTemplate(pResolvedToken->hClass);

                if (thTemplate.IsTypeDesc() || !thTemplate.AsMethodTable()->HasSameTypeDefAs(pContextMT))
                    goto NoSpecialCase;

                // This check filters out method instantiation on generic type definition, like G::M<!!0>()
                // We may not ever get it here. Filter it out just to be sure...
                if (pResolvedToken->pTypeSpec == NULL)
                    goto NoSpecialCase;

                if (!IsTypeSpecForTypicalInstantiation(SigPointer(pResolvedToken->pTypeSpec, pResolvedToken->cbTypeSpec)))
                    goto NoSpecialCase;
            }

            // Just use the method descriptor that was passed in!
            pResult->indirections = 0;
            pResult->testForNull = 0;

            return;
        }
    }
    // Otherwise we must just have class type variables
    else
    {
        _ASSERTE(pContextMT->GetNumGenericArgs() > 0);

        if (pContextMD->RequiresInstMethodTableArg())
        {
            // If we've got a vtable extra argument, go through that
            pResult->helper = fInstrument ? CORINFO_HELP_RUNTIMEHANDLE_CLASS_LOG : CORINFO_HELP_RUNTIMEHANDLE_CLASS;
        }
        // If we've got an object, go through its vtable
        else
        {
            _ASSERTE(pContextMD->AcquiresInstMethodTableFromThis());
            pResult->helper = fInstrument ? CORINFO_HELP_RUNTIMEHANDLE_CLASS_LOG : CORINFO_HELP_RUNTIMEHANDLE_CLASS;
        }

        if (fInstrument)
            goto NoSpecialCase;

        // Special cases:
        // (1) Naked class type variable: look up directly in instantiation hanging off vtable
        // (2) C<!0,...,!(n-1)> where C is the context's class and C is sealed: just return vtable ptr
        if ((entryKind == TypeHandleSlot) && (pResolvedToken->tokenType != CORINFO_TOKENKIND_Newarr))
        {
            SigPointer sigptr(pResolvedToken->pTypeSpec, pResolvedToken->cbTypeSpec);
            CorElementType type;
            IfFailThrow(sigptr.GetElemType(&type));
            if (type == ELEMENT_TYPE_VAR)
            {
                pResult->indirections = 3;
                pResult->testForNull = 0;
                pResult->offsets[0] = MethodTable::GetOffsetOfPerInstInfo();
                pResult->offsets[1] = sizeof(TypeHandle*) * (pContextMT->GetNumDicts() - 1);
                uint32_t data;
                IfFailThrow(sigptr.GetData(&data));
                pResult->offsets[2] = sizeof(TypeHandle) * data;

                return;
            }
            else if (type == ELEMENT_TYPE_GENERICINST &&
                (pContextMT->IsSealed() || pResultLookup->lookupKind.runtimeLookupKind == CORINFO_LOOKUP_CLASSPARAM))
            {
                TypeHandle thTemplate(pResolvedToken->hClass);

                if (thTemplate.IsTypeDesc() || !thTemplate.AsMethodTable()->HasSameTypeDefAs(pContextMT))
                    goto NoSpecialCase;

                if (!IsTypeSpecForTypicalInstantiation(SigPointer(pResolvedToken->pTypeSpec, pResolvedToken->cbTypeSpec)))
                    goto NoSpecialCase;

                // Just use the vtable pointer itself!
                pResult->indirections = 0;
                pResult->testForNull = 0;

                return;
            }
        }
    }

NoSpecialCase:

    SigBuilder sigBuilder;

    sigBuilder.AppendData(entryKind);

    if (pResultLookup->lookupKind.runtimeLookupKind != CORINFO_LOOKUP_METHODPARAM)
    {
        _ASSERTE(pContextMT->GetNumDicts() > 0);
        sigBuilder.AppendData(pContextMT->GetNumDicts() - 1);
    }

    Module * pModule = (Module *)pResolvedToken->tokenScope;

    switch (entryKind)
    {
    case DeclaringTypeHandleSlot:
        _ASSERTE(pTemplateMD != NULL);
        sigBuilder.AppendElementType(ELEMENT_TYPE_INTERNAL);
        sigBuilder.AppendPointer(pTemplateMD->GetMethodTable());
        FALLTHROUGH;

    case TypeHandleSlot:
        {
            if (pResolvedToken->tokenType == CORINFO_TOKENKIND_Newarr)
            {
                sigBuilder.AppendElementType(ELEMENT_TYPE_SZARRAY);
            }

            // Note that we can come here with pResolvedToken->pTypeSpec == NULL for invalid IL that
            // directly references __Canon
            if (pResolvedToken->pTypeSpec != NULL)
            {
                SigPointer sigptr(pResolvedToken->pTypeSpec, pResolvedToken->cbTypeSpec);
                sigptr.ConvertToInternalExactlyOne(pModule, NULL, &sigBuilder);
            }
            else
            {
                sigBuilder.AppendElementType(ELEMENT_TYPE_INTERNAL);
                sigBuilder.AppendPointer(pResolvedToken->hClass);
            }
        }
        break;

    case ConstrainedMethodEntrySlot:
        // Encode constrained type token
        if (pConstrainedResolvedToken->pTypeSpec != NULL)
        {
            SigPointer sigptr(pConstrainedResolvedToken->pTypeSpec, pConstrainedResolvedToken->cbTypeSpec);
            sigptr.ConvertToInternalExactlyOne(pModule, NULL, &sigBuilder);
        }
        else
        {
            sigBuilder.AppendElementType(ELEMENT_TYPE_INTERNAL);
            sigBuilder.AppendPointer(pConstrainedResolvedToken->hClass);
        }
        FALLTHROUGH;

    case MethodDescSlot:
    case MethodEntrySlot:
    case DispatchStubAddrSlot:
        {
            // Encode containing type
            if (pResolvedToken->pTypeSpec != NULL)
            {
                SigPointer sigptr(pResolvedToken->pTypeSpec, pResolvedToken->cbTypeSpec);
                sigptr.ConvertToInternalExactlyOne(pModule, NULL, &sigBuilder);
            }
            else
            {
                sigBuilder.AppendElementType(ELEMENT_TYPE_INTERNAL);
                sigBuilder.AppendPointer(pResolvedToken->hClass);
            }

            // Encode method
            _ASSERTE(pTemplateMD != NULL);

            mdMethodDef methodToken               = pTemplateMD->GetMemberDef();
            DWORD       methodFlags               = 0;

            // Check for non-NULL method spec first. We can encode the method instantiation only if we have one in method spec to start with. Note that there are weird cases
            // like instantiating stub for generic method definition that do not have method spec but that won't be caught by the later conditions either.
            BOOL fMethodNeedsInstantiation = (pResolvedToken->pMethodSpec != NULL) && pTemplateMD->HasMethodInstantiation() && !pTemplateMD->IsGenericMethodDefinition();

            if (pTemplateMD->IsUnboxingStub())
                methodFlags |= ENCODE_METHOD_SIG_UnboxingStub;
            // Always create instantiating stub for method entry points even if the template does not ask for it. It saves caller
            // from creating throw-away instantiating stub.
            if (pTemplateMD->IsInstantiatingStub() || (entryKind == MethodEntrySlot))
                methodFlags |= ENCODE_METHOD_SIG_InstantiatingStub;
            if (fMethodNeedsInstantiation)
                methodFlags |= ENCODE_METHOD_SIG_MethodInstantiation;
            if (IsNilToken(methodToken))
            {
                methodFlags |= ENCODE_METHOD_SIG_SlotInsteadOfToken;
            }
            else
            if (entryKind == DispatchStubAddrSlot && pTemplateMD->IsVtableMethod())
            {
                // Encode the method for dispatch stub using slot to avoid touching the interface method MethodDesc at runtime

                // There should be no other flags set if we are encoding the method using slot for virtual stub dispatch
                _ASSERTE(methodFlags == 0);

                methodFlags |= ENCODE_METHOD_SIG_SlotInsteadOfToken;
            }

            sigBuilder.AppendData(methodFlags);

            if ((methodFlags & ENCODE_METHOD_SIG_SlotInsteadOfToken) == 0)
            {
                // Encode method token and its module context (as method's type)
                sigBuilder.AppendElementType(ELEMENT_TYPE_INTERNAL);
                sigBuilder.AppendPointer(pTemplateMD->GetMethodTable());

                sigBuilder.AppendData(RidFromToken(methodToken));
            }
            else
            {
                sigBuilder.AppendData(pTemplateMD->GetSlot());
            }

            if (fMethodNeedsInstantiation)
            {
                SigPointer sigptr(pResolvedToken->pMethodSpec, pResolvedToken->cbMethodSpec);

                BYTE etype;
                IfFailThrow(sigptr.GetByte(&etype));

                // Load the generic method instantiation
                THROW_BAD_FORMAT_MAYBE(etype == (BYTE)IMAGE_CEE_CS_CALLCONV_GENERICINST, 0, pModule);

                uint32_t nGenericMethodArgs;
                IfFailThrow(sigptr.GetData(&nGenericMethodArgs));
                sigBuilder.AppendData(nGenericMethodArgs);

                _ASSERTE(nGenericMethodArgs == pTemplateMD->GetNumGenericMethodArgs());

                for (DWORD i = 0; i < nGenericMethodArgs; i++)
                {
                    sigptr.ConvertToInternalExactlyOne(pModule, NULL, &sigBuilder);
                }
            }
        }
        break;

    case FieldDescSlot:
        {
            if (pResolvedToken->pTypeSpec != NULL)
            {
                 // Encode containing type
                SigPointer sigptr(pResolvedToken->pTypeSpec, pResolvedToken->cbTypeSpec);
                sigptr.ConvertToInternalExactlyOne(pModule, NULL, &sigBuilder);
            }
            else
            {
                sigBuilder.AppendElementType(ELEMENT_TYPE_INTERNAL);
                sigBuilder.AppendPointer(pResolvedToken->hClass);
            }

            FieldDesc * pField = (FieldDesc *)pResolvedToken->hField;
            _ASSERTE(pField != NULL);

            DWORD fieldIndex = pField->GetApproxEnclosingMethodTable()->GetIndexForFieldDesc(pField);
            sigBuilder.AppendData(fieldIndex);
        }
        break;

    default:
        _ASSERTE(false);
    }

    DictionaryEntrySignatureSource signatureSource = FromJIT;

    WORD slot;

    // It's a method dictionary lookup
    if (pResultLookup->lookupKind.runtimeLookupKind == CORINFO_LOOKUP_METHODPARAM)
    {
        _ASSERTE(pContextMD != NULL);
        _ASSERTE(pContextMD->HasMethodInstantiation());

        if (DictionaryLayout::FindToken(pContextMD, pContextMD->GetLoaderAllocator(), 1, &sigBuilder, NULL, signatureSource, pResult, &slot))
        {
            pResult->testForNull = 1;
            int minDictSize = pContextMD->GetNumGenericMethodArgs() + 1 + pContextMD->GetDictionaryLayout()->GetNumInitialSlots();
            if (slot >= minDictSize)
            {
                // Dictionaries are guaranteed to have at least the number of slots allocated initially, so skip size check for smaller indexes
                pResult->sizeOffset = (WORD)pContextMD->GetNumGenericMethodArgs() * sizeof(DictionaryEntry);
            }

            // Indirect through dictionary table pointer in InstantiatedMethodDesc
            pResult->offsets[0] = offsetof(InstantiatedMethodDesc, m_pPerInstInfo);
        }
    }

    // It's a class dictionary lookup (CORINFO_LOOKUP_CLASSPARAM or CORINFO_LOOKUP_THISOBJ)
    else
    {
        if (DictionaryLayout::FindToken(pContextMT, pContextMT->GetLoaderAllocator(), 2, &sigBuilder, NULL, signatureSource, pResult, &slot))
        {
            pResult->testForNull = 1;
            int minDictSize = pContextMT->GetNumGenericArgs() + 1 + pContextMT->GetClass()->GetDictionaryLayout()->GetNumInitialSlots();
            if (slot >= minDictSize)
            {
                // Dictionaries are guaranteed to have at least the number of slots allocated initially, so skip size check for smaller indexes
                pResult->sizeOffset = (WORD)pContextMT->GetNumGenericArgs() * sizeof(DictionaryEntry);
            }

            // Indirect through dictionary table pointer in vtable
            pResult->offsets[0] = MethodTable::GetOffsetOfPerInstInfo();

            // Next indirect through the dictionary appropriate to this instantiated type
            pResult->offsets[1] = sizeof(TypeHandle*) * (pContextMT->GetNumDicts() - 1);
        }
    }
}

void CEEInfo::AddTransientMethodDetails(TransientMethodDetails details)
{
    STANDARD_VM_CONTRACT;
    _ASSERTE(details.Method != NULL);

    if (m_transientDetails == NULL)
        m_transientDetails = new SArray<TransientMethodDetails, FALSE>();
    m_transientDetails->Append(std::move(details));
}

TransientMethodDetails CEEInfo::RemoveTransientMethodDetails(MethodDesc* pMD)
{
    STANDARD_VM_CONTRACT;
    _ASSERTE(pMD != NULL);

    TransientMethodDetails local{};
    TransientMethodDetails* details;
    if (FindTransientMethodDetails(pMD, &details))
    {
        // Details found, move contents to return
        // and default initialize the found instance.
        local = std::move(*details);
        *details = {};
    }
    return local;
}

bool CEEInfo::FindTransientMethodDetails(MethodDesc* pMD, TransientMethodDetails** details)
{
    STANDARD_VM_CONTRACT;
    _ASSERTE(pMD != NULL);
    _ASSERTE(details != NULL);

    if (m_transientDetails != NULL)
    {
        TransientMethodDetails* curr = m_transientDetails->GetElements();
        TransientMethodDetails* end = curr + m_transientDetails->GetCount();
        for (;curr != end; ++curr)
        {
            if (curr->Method == pMD)
            {
                *details = curr;
                return true;
            }
        }
    }
    return false;
}

/*********************************************************************/
size_t CEEInfo::printClassName(CORINFO_CLASS_HANDLE cls, char* buffer, size_t bufferSize, size_t* pRequiredBufferSize)
{
    CONTRACTL {
        MODE_PREEMPTIVE;
        THROWS;
        GC_TRIGGERS;
    } CONTRACTL_END;

    size_t bytesWritten = 0;

    JIT_TO_EE_TRANSITION();

    size_t requiredBufferSize = 0;

    TypeHandle th(cls);
    IMDInternalImport* pImport = th.GetMethodTable()->GetMDImport();

    auto append = [buffer, bufferSize, &bytesWritten, &requiredBufferSize](const char* str)
    {
        size_t strLen = strlen(str);

        if ((buffer != nullptr) && (bytesWritten + 1 < bufferSize))
        {
            if (bytesWritten + strLen >= bufferSize)
            {
                memcpy(buffer + bytesWritten, str, bufferSize - bytesWritten - 1);
                bytesWritten = bufferSize - 1;
            }
            else
            {
                memcpy(buffer + bytesWritten, str, strLen);
                bytesWritten += strLen;
            }
        }

        requiredBufferSize += strLen;
    };

    // Subset of TypeString that does just what we need while staying in UTF8
    // and avoiding expensive copies. This function is called a lot in checked
    // builds.
    // One difference is that we do not handle escaping type names here (see
    // IsTypeNameReservedChar). The situation is rare and somewhat complicated
    // to handle since it requires iterating UTF8 encoded codepoints. Given
    // that this function is only needed for diagnostics the complication seems
    // unnecessary.
    mdTypeDef td = th.GetCl();
    if (IsNilToken(td))
    {
        append("(dynamicClass)");
    }
    else
    {
        DWORD attr;
        IfFailThrow(pImport->GetTypeDefProps(td, &attr, NULL));

        StackSArray<mdTypeDef> nestedHierarchy;
        nestedHierarchy.Append(td);

        if (IsTdNested(attr))
        {
            while (SUCCEEDED(pImport->GetNestedClassProps(td, &td)))
                nestedHierarchy.Append(td);
        }

        for (SCOUNT_T i = nestedHierarchy.GetCount() - 1; i >= 0; i--)
        {
            LPCUTF8 name;
            LPCUTF8 nameSpace;
            IfFailThrow(pImport->GetNameOfTypeDef(nestedHierarchy[i], &name, &nameSpace));

            if ((nameSpace != NULL) && (*nameSpace != '\0'))
            {
                append(nameSpace);
                append(".");
            }

            append(name);

            if (i != 0)
            {
                append("+");
            }
        }
    }

    if (bufferSize > 0)
    {
        _ASSERTE(bytesWritten < bufferSize);
        buffer[bytesWritten] = '\0';
    }

    if (pRequiredBufferSize != nullptr)
    {
        *pRequiredBufferSize = requiredBufferSize + 1;
    }

    EE_TO_JIT_TRANSITION();

    return bytesWritten;
}

/*********************************************************************/
CORINFO_MODULE_HANDLE CEEInfo::getClassModule(CORINFO_CLASS_HANDLE clsHnd)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_PREEMPTIVE;
    } CONTRACTL_END;

    CORINFO_MODULE_HANDLE result = NULL;

    JIT_TO_EE_TRANSITION_LEAF();

    TypeHandle     VMClsHnd(clsHnd);

    result = CORINFO_MODULE_HANDLE(VMClsHnd.GetModule());

    EE_TO_JIT_TRANSITION_LEAF();

    return result;
}

/*********************************************************************/
CORINFO_ASSEMBLY_HANDLE CEEInfo::getModuleAssembly(CORINFO_MODULE_HANDLE modHnd)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_PREEMPTIVE;
    } CONTRACTL_END;

    CORINFO_ASSEMBLY_HANDLE result = NULL;

    JIT_TO_EE_TRANSITION_LEAF();

    result = CORINFO_ASSEMBLY_HANDLE(GetModule(modHnd)->GetAssembly());

    EE_TO_JIT_TRANSITION_LEAF();

    return result;
}

/*********************************************************************/
const char* CEEInfo::getAssemblyName(CORINFO_ASSEMBLY_HANDLE asmHnd)
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    } CONTRACTL_END;

    const char*  result = NULL;

    JIT_TO_EE_TRANSITION();
    result = ((Assembly*)asmHnd)->GetSimpleName();
    EE_TO_JIT_TRANSITION();

    return result;
}

/*********************************************************************/
void* CEEInfo::LongLifetimeMalloc(size_t sz)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_PREEMPTIVE;
    } CONTRACTL_END;

    void*  result = NULL;

    JIT_TO_EE_TRANSITION_LEAF();
    result = new (nothrow) char[sz];
    EE_TO_JIT_TRANSITION_LEAF();

    return result;
}

/*********************************************************************/
void CEEInfo::LongLifetimeFree(void* obj)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_PREEMPTIVE;
    } CONTRACTL_END;

    JIT_TO_EE_TRANSITION_LEAF();
    (operator delete)(obj);
    EE_TO_JIT_TRANSITION_LEAF();
}

/*********************************************************************/
size_t CEEInfo::getClassModuleIdForStatics(CORINFO_CLASS_HANDLE clsHnd, CORINFO_MODULE_HANDLE *pModuleHandle, void **ppIndirection)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_PREEMPTIVE;
    } CONTRACTL_END;

    size_t result = 0;

    JIT_TO_EE_TRANSITION_LEAF();

    TypeHandle     VMClsHnd(clsHnd);
    Module *pModule = VMClsHnd.AsMethodTable()->GetModuleForStatics();

    if (ppIndirection != NULL)
        *ppIndirection = NULL;

    // The zapper needs the module handle. The jit should not use it at all.
    if (pModuleHandle)
        *pModuleHandle = CORINFO_MODULE_HANDLE(pModule);

    result = pModule->GetModuleID();

    _ASSERTE(result);

    EE_TO_JIT_TRANSITION_LEAF();

    return result;
}

/*********************************************************************/
bool CEEInfo::getIsClassInitedFlagAddress(CORINFO_CLASS_HANDLE cls, CORINFO_CONST_LOOKUP* addr, int* offset)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_PREEMPTIVE;
    } CONTRACTL_END;

    _ASSERTE(addr);

    JIT_TO_EE_TRANSITION_LEAF();

    TypeHandle clsTypeHandle(cls);
    PTR_MethodTable pMT = clsTypeHandle.AsMethodTable();

    // Impl is based on IsPrecomputedClassInitialized()
    UINT32 clsIndex = 0;
    if (pMT->IsDynamicStatics())
    {
        clsIndex = (UINT32)pMT->GetModuleDynamicEntryID();
    }
    else
    {
        clsIndex = (UINT32)pMT->GetClassIndex();
    }

    size_t moduleId = pMT->GetModuleForStatics()->GetModuleID();
    addr->addr = (UINT8*)moduleId + DomainLocalModule::GetOffsetOfDataBlob() + clsIndex;
    addr->accessType = IAT_VALUE;
    *offset = 0;

    EE_TO_JIT_TRANSITION_LEAF();

    return true;
}

/*********************************************************************/
bool CEEInfo::getStaticBaseAddress(CORINFO_CLASS_HANDLE cls, bool isGc, CORINFO_CONST_LOOKUP* addr)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_PREEMPTIVE;
    } CONTRACTL_END;

    JIT_TO_EE_TRANSITION_LEAF();

    TypeHandle clsTypeHandle(cls);
    PTR_MethodTable pMT = clsTypeHandle.AsMethodTable();

    GCX_COOP();
    addr->addr = isGc ? pMT->GetGCStaticsBasePointer() : pMT->GetNonGCStaticsBasePointer();
    addr->accessType = IAT_VALUE;

    EE_TO_JIT_TRANSITION_LEAF();

    return true;
}

/*********************************************************************/
bool CEEInfo::isValueClass(CORINFO_CLASS_HANDLE clsHnd)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_PREEMPTIVE;
    } CONTRACTL_END;

    bool ret = false;

    JIT_TO_EE_TRANSITION_LEAF();

    _ASSERTE(clsHnd);

    ret = TypeHandle(clsHnd).IsValueType();

    EE_TO_JIT_TRANSITION_LEAF();

    return ret;
}

/*********************************************************************/
uint32_t CEEInfo::getClassAttribs (CORINFO_CLASS_HANDLE clsHnd)
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    } CONTRACTL_END;

    // <REVISIT_TODO>@todo FIX need to really fetch the class attributes.  at present
    // we don't need to because the JIT only cares in the case of COM classes</REVISIT_TODO>
    uint32_t ret = 0;

    JIT_TO_EE_TRANSITION();

    ret = getClassAttribsInternal(clsHnd);

    EE_TO_JIT_TRANSITION();

    return ret;
}

/*********************************************************************/
uint32_t CEEInfo::getClassAttribsInternal (CORINFO_CLASS_HANDLE clsHnd)
{
    STANDARD_VM_CONTRACT;

    DWORD ret = 0;

    _ASSERTE(clsHnd);

    TypeHandle     VMClsHnd(clsHnd);

    // Byrefs should only occur in method and local signatures, which are accessed
    // using ICorClassInfo and ICorClassInfo.getChildType.
    // So getClassAttribs() should not be called for byrefs

    if (VMClsHnd.IsByRef())
    {
        _ASSERTE(!"Did findClass() return a Byref?");
        COMPlusThrowHR(COR_E_BADIMAGEFORMAT);
    }
    else if (VMClsHnd.IsGenericVariable())
    {
        //@GENERICSVER: for now, type variables simply report "variable".
        ret |= CORINFO_FLG_GENERIC_TYPE_VARIABLE;
    }
    else
    {
        MethodTable *pMT = VMClsHnd.GetMethodTable();

        if (!pMT)
        {
            _ASSERTE(!"Did findClass() return a Byref?");
            COMPlusThrowHR(COR_E_BADIMAGEFORMAT);
        }

        EEClass * pClass = pMT->GetClass();

        // The array flag is used to identify the faked-up methods on
        // array types, i.e. .ctor, Get, Set and Address
        if (pMT->IsArray())
            ret |= CORINFO_FLG_ARRAY;

        if (pMT->IsInterface())
            ret |= CORINFO_FLG_INTERFACE;

        if (pMT->HasComponentSize())
            ret |= CORINFO_FLG_VAROBJSIZE;

        if (VMClsHnd.IsValueType())
        {
            ret |= CORINFO_FLG_VALUECLASS;

            if (pMT->IsByRefLike())
                ret |= CORINFO_FLG_BYREF_LIKE;

            if (pClass->IsUnsafeValueClass())
                ret |= CORINFO_FLG_UNSAFE_VALUECLASS;
        }
        if (pClass->HasExplicitFieldOffsetLayout() && pClass->HasOverlaidField())
            ret |= CORINFO_FLG_OVERLAPPING_FIELDS;

        if (pClass->IsInlineArray())
            ret |= CORINFO_FLG_INDEXABLE_FIELDS;

        if (VMClsHnd.IsCanonicalSubtype())
            ret |= CORINFO_FLG_SHAREDINST;

        if (pMT->ContainsPointers() || pMT == g_TypedReferenceMT)
            ret |= CORINFO_FLG_CONTAINS_GC_PTR;

        if (pMT->IsDelegate())
            ret |= CORINFO_FLG_DELEGATE;

        if (pClass->IsBeforeFieldInit())
            ret |= CORINFO_FLG_BEFOREFIELDINIT;

        if (pClass->IsAbstract())
            ret |= CORINFO_FLG_ABSTRACT;

        if (pClass->IsSealed())
            ret |= CORINFO_FLG_FINAL;

        if (pMT->IsIntrinsicType())
            ret |= CORINFO_FLG_INTRINSIC_TYPE;
    }

    return ret;
}

/*********************************************************************/
//
// See code:CorInfoFlag#ClassConstructionFlags  for details.
//
CorInfoInitClassResult CEEInfo::initClass(
            CORINFO_FIELD_HANDLE    field,
            CORINFO_METHOD_HANDLE   method,
            CORINFO_CONTEXT_HANDLE  context)
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    } CONTRACTL_END;

    DWORD result = CORINFO_INITCLASS_NOT_REQUIRED;

    JIT_TO_EE_TRANSITION();
    {

    FieldDesc * pFD = (FieldDesc *)field;
    _ASSERTE(pFD == NULL || pFD->IsStatic());

    MethodDesc* pMD = (method != NULL) ? (MethodDesc*)method : m_pMethodBeingCompiled;

    TypeHandle typeToInitTH = (pFD != NULL) ? pFD->GetEnclosingMethodTable() : GetTypeFromContext(context);

    MethodDesc *methodBeingCompiled = m_pMethodBeingCompiled;

    MethodTable *pTypeToInitMT = typeToInitTH.AsMethodTable();

    if (pTypeToInitMT->IsClassInited())
    {
        // If the type is initialized there really is nothing to do.
        result = CORINFO_INITCLASS_INITIALIZED;
        goto exit;
    }

    if (pTypeToInitMT->IsGlobalClass())
    {
        // For both jitted and ngen code the global class is always considered initialized
        result = CORINFO_INITCLASS_NOT_REQUIRED;
        goto exit;
    }

    if (pFD == NULL)
    {
        if (pTypeToInitMT->GetClass()->IsBeforeFieldInit())
        {
            // We can wait for field accesses to run .cctor
            result = CORINFO_INITCLASS_NOT_REQUIRED;
            goto exit;
        }

        // Run .cctor on statics & constructors
        if (pMD->IsStatic())
        {
            // Except don't class construct on .cctor - it would be circular
            if (pMD->IsClassConstructor())
            {
                result = CORINFO_INITCLASS_NOT_REQUIRED;
                goto exit;
            }
        }
        else
        // According to the spec, we should be able to do this optimization for both reference and valuetypes.
        // To maintain backward compatibility, we are doing it for reference types only.
        // We don't do this for interfaces though, as those don't have instance constructors.
        if (!pMD->IsCtor() && !pTypeToInitMT->IsValueType() && !pTypeToInitMT->IsInterface())
        {
            // For instance methods of types with precise-initialization
            // semantics, we can assume that the .ctor triggered the
            // type initialization.
            // This does not hold for NULL "this" object. However, the spec does
            // not require that case to work.
            result = CORINFO_INITCLASS_NOT_REQUIRED;
            goto exit;
        }
    }

    if (pTypeToInitMT->IsSharedByGenericInstantiations())
    {
        if ((pFD == NULL) && (method != NULL) && (context == METHOD_BEING_COMPILED_CONTEXT()))
        {
            _ASSERTE(pTypeToInitMT == methodBeingCompiled->GetMethodTable());
            // If we're inling a call to a method in our own type, then we should already
            // have triggered the .cctor when caller was itself called.
            result = CORINFO_INITCLASS_NOT_REQUIRED;
            goto exit;
        }

        // Shared generic code has to use helper. Moreover, tell JIT not to inline since
        // inlining of generic dictionary lookups is not supported.
        result = CORINFO_INITCLASS_USE_HELPER | CORINFO_INITCLASS_DONT_INLINE;
        goto exit;
    }

    //
    // Try to prove that the initialization is not necessary because of nesting
    //

    if (pFD == NULL)
    {
        // Handled above
        _ASSERTE(!pTypeToInitMT->GetClass()->IsBeforeFieldInit());

        if (method != NULL && pTypeToInitMT == methodBeingCompiled->GetMethodTable())
        {
            // If we're inling a call to a method in our own type, then we should already
            // have triggered the .cctor when caller was itself called.
            result = CORINFO_INITCLASS_NOT_REQUIRED;
            goto exit;
        }
    }
    else
    {
        // This optimization may cause static fields in reference types to be accessed without cctor being triggered
        // for NULL "this" object. It does not conform with what the spec says. However, we have been historically
        // doing it for perf reasons.
        if (!pTypeToInitMT->IsValueType() && !pTypeToInitMT->IsInterface() && !pTypeToInitMT->GetClass()->IsBeforeFieldInit())
        {
            if (pTypeToInitMT == GetTypeFromContext(context).AsMethodTable() || pTypeToInitMT == methodBeingCompiled->GetMethodTable())
            {
                // The class will be initialized by the time we access the field.
                result = CORINFO_INITCLASS_NOT_REQUIRED;
                goto exit;
            }
        }

        // If we are currently compiling the class constructor for this static field access then we can skip the initClass
        if (methodBeingCompiled->GetMethodTable() == pTypeToInitMT && methodBeingCompiled->IsStatic() && methodBeingCompiled->IsClassConstructor())
        {
            // The class will be initialized by the time we access the field.
            result = CORINFO_INITCLASS_NOT_REQUIRED;
            goto exit;
        }
    }

    //
    // Optimizations for domain specific code
    //

    // Allocate space for the local class if necessary, but don't trigger
    // class construction.
    DomainLocalModule *pModule = pTypeToInitMT->GetDomainLocalModule();
    pModule->PopulateClass(pTypeToInitMT);

    if (pTypeToInitMT->IsClassInited())
    {
        result = CORINFO_INITCLASS_INITIALIZED;
        goto exit;
    }

    result = CORINFO_INITCLASS_USE_HELPER;
    }
exit: ;
    EE_TO_JIT_TRANSITION();

    return (CorInfoInitClassResult)result;
}



void CEEInfo::classMustBeLoadedBeforeCodeIsRun (CORINFO_CLASS_HANDLE typeToLoadHnd)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_PREEMPTIVE;
    } CONTRACTL_END;

    JIT_TO_EE_TRANSITION_LEAF();

    TypeHandle th = TypeHandle(typeToLoadHnd);

    // Type handles returned to JIT at runtime are always fully loaded. Verify that it is the case.
    _ASSERTE(th.IsFullyLoaded());

    EE_TO_JIT_TRANSITION_LEAF();
}

/*********************************************************************/
void CEEInfo::methodMustBeLoadedBeforeCodeIsRun (CORINFO_METHOD_HANDLE methHnd)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_PREEMPTIVE;
    } CONTRACTL_END;

    JIT_TO_EE_TRANSITION_LEAF();

    MethodDesc *pMD = (MethodDesc*) methHnd;

    // MethodDescs returned to JIT at runtime are always fully loaded. Verify that it is the case.
    _ASSERTE(pMD->GetMethodTable()->IsFullyLoaded());

    EE_TO_JIT_TRANSITION_LEAF();
}

/*********************************************************************/
CORINFO_METHOD_HANDLE CEEInfo::mapMethodDeclToMethodImpl(CORINFO_METHOD_HANDLE methHnd)
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    } CONTRACTL_END;

    CORINFO_METHOD_HANDLE result = NULL;

    JIT_TO_EE_TRANSITION();

    MethodDesc *pMD = GetMethod(methHnd);
    pMD = MethodTable::MapMethodDeclToMethodImpl(pMD);
    result = (CORINFO_METHOD_HANDLE) pMD;

    EE_TO_JIT_TRANSITION();

    return result;
}

/*********************************************************************/
CORINFO_CLASS_HANDLE CEEInfo::getBuiltinClass(CorInfoClassId classId)
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    } CONTRACTL_END;

    CORINFO_CLASS_HANDLE result = (CORINFO_CLASS_HANDLE) 0;

    JIT_TO_EE_TRANSITION();

    switch (classId)
    {
    case CLASSID_SYSTEM_OBJECT:
        result = CORINFO_CLASS_HANDLE(g_pObjectClass);
        break;
    case CLASSID_TYPED_BYREF:
        result = CORINFO_CLASS_HANDLE(g_TypedReferenceMT);
        break;
    case CLASSID_TYPE_HANDLE:
        result = CORINFO_CLASS_HANDLE(CoreLibBinder::GetClass(CLASS__TYPE_HANDLE));
        break;
    case CLASSID_FIELD_HANDLE:
        result = CORINFO_CLASS_HANDLE(CoreLibBinder::GetClass(CLASS__FIELD_HANDLE));
        break;
    case CLASSID_METHOD_HANDLE:
        result = CORINFO_CLASS_HANDLE(CoreLibBinder::GetClass(CLASS__METHOD_HANDLE));
        break;
    case CLASSID_ARGUMENT_HANDLE:
        result = CORINFO_CLASS_HANDLE(CoreLibBinder::GetClass(CLASS__ARGUMENT_HANDLE));
        break;
    case CLASSID_STRING:
        result = CORINFO_CLASS_HANDLE(g_pStringClass);
        break;
    case CLASSID_RUNTIME_TYPE:
        result = CORINFO_CLASS_HANDLE(g_pRuntimeTypeClass);
        break;
    default:
        _ASSERTE(!"NYI: unknown classId");
        break;
    }

    EE_TO_JIT_TRANSITION();

    return result;
}



/*********************************************************************/
CorInfoType CEEInfo::getTypeForPrimitiveValueClass(
        CORINFO_CLASS_HANDLE clsHnd)
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    } CONTRACTL_END;

    CorInfoType result = CORINFO_TYPE_UNDEF;

    JIT_TO_EE_TRANSITION();

    TypeHandle th(clsHnd);
    _ASSERTE (!th.IsGenericVariable());

    CorElementType elementType = th.GetVerifierCorElementType();
    if (CorIsPrimitiveType(elementType))
    {
        result = asCorInfoType(elementType);
    }
    EE_TO_JIT_TRANSITION();

    return result;
}

/*********************************************************************/
CorInfoType CEEInfo::getTypeForPrimitiveNumericClass(
        CORINFO_CLASS_HANDLE clsHnd)
{
    CONTRACTL{
        NOTHROW;
        GC_NOTRIGGER;
        MODE_PREEMPTIVE;
    } CONTRACTL_END;

    CorInfoType result = CORINFO_TYPE_UNDEF;

    JIT_TO_EE_TRANSITION_LEAF();

    TypeHandle th(clsHnd);
    _ASSERTE (!th.IsGenericVariable());

    CorElementType ty = th.GetSignatureCorElementType();
    switch (ty)
    {
    case ELEMENT_TYPE_I1:
        result = CORINFO_TYPE_BYTE;
        break;
    case ELEMENT_TYPE_U1:
        result = CORINFO_TYPE_UBYTE;
        break;
    case ELEMENT_TYPE_I2:
        result = CORINFO_TYPE_SHORT;
        break;
    case ELEMENT_TYPE_U2:
        result = CORINFO_TYPE_USHORT;
        break;
    case ELEMENT_TYPE_I4:
        result = CORINFO_TYPE_INT;
        break;
    case ELEMENT_TYPE_U4:
        result = CORINFO_TYPE_UINT;
        break;
    case ELEMENT_TYPE_I8:
        result = CORINFO_TYPE_LONG;
        break;
    case ELEMENT_TYPE_U8:
        result = CORINFO_TYPE_ULONG;
        break;
    case ELEMENT_TYPE_R4:
        result = CORINFO_TYPE_FLOAT;
        break;
    case ELEMENT_TYPE_R8:
        result = CORINFO_TYPE_DOUBLE;
        break;
    case ELEMENT_TYPE_I:
        result = CORINFO_TYPE_NATIVEINT;
        break;
    case ELEMENT_TYPE_U:
        result = CORINFO_TYPE_NATIVEUINT;
        break;

    default:
        // Error case, we will return CORINFO_TYPE_UNDEF
        break;
    }

    JIT_TO_EE_TRANSITION_LEAF();

    return result;
}


void CEEInfo::getGSCookie(GSCookie * pCookieVal, GSCookie ** ppCookieVal)
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    } CONTRACTL_END;

    JIT_TO_EE_TRANSITION();

    if (pCookieVal)
    {
        *pCookieVal = GetProcessGSCookie();
        *ppCookieVal = NULL;
    }
    else
    {
        *ppCookieVal = GetProcessGSCookiePtr();
    }

    EE_TO_JIT_TRANSITION();
}


/*********************************************************************/
// TRUE if child is a subtype of parent
// if parent is an interface, then does child implement / extend parent
bool CEEInfo::canCast(
        CORINFO_CLASS_HANDLE        child,
        CORINFO_CLASS_HANDLE        parent)
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    } CONTRACTL_END;

    bool result = false;

    JIT_TO_EE_TRANSITION();

    result = !!((TypeHandle)child).CanCastTo((TypeHandle)parent);

    EE_TO_JIT_TRANSITION();

    return result;
}

/*********************************************************************/
// See if a cast from fromClass to toClass will succeed, fail, or needs
// to be resolved at runtime.
TypeCompareState CEEInfo::compareTypesForCast(
        CORINFO_CLASS_HANDLE        fromClass,
        CORINFO_CLASS_HANDLE        toClass)
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    } CONTRACTL_END;

    TypeCompareState result = TypeCompareState::May;

    JIT_TO_EE_TRANSITION();

    TypeHandle fromHnd = (TypeHandle) fromClass;
    TypeHandle toHnd = (TypeHandle) toClass;

#ifdef FEATURE_COMINTEROP
    // If casting from a com object class, don't try to optimize.
    if (fromHnd.IsComObjectType())
    {
        result = TypeCompareState::May;
    }
    else
#endif // FEATURE_COMINTEROP

    // If casting from ICastable or IDynamicInterfaceCastable, don't try to optimize
    if (fromHnd.GetMethodTable()->IsICastable() || fromHnd.GetMethodTable()->IsIDynamicInterfaceCastable())
    {
        result = TypeCompareState::May;
    }
    // If casting to Nullable<T>, don't try to optimize
    else if (Nullable::IsNullableType(toHnd))
    {
        result = TypeCompareState::May;
    }
    // If the types are not shared, we can check directly.
    else if (!fromHnd.IsCanonicalSubtype() && !toHnd.IsCanonicalSubtype())
    {
        result = fromHnd.CanCastTo(toHnd) ? TypeCompareState::Must : TypeCompareState::MustNot;
    }
    // Casting from a shared type to an unshared type.
    else if (fromHnd.IsCanonicalSubtype() && !toHnd.IsCanonicalSubtype())
    {
        // Only handle casts to interface types for now
        if (toHnd.IsInterface())
        {
            // Do a preliminary check.
            BOOL canCast = fromHnd.CanCastTo(toHnd);

            // Pass back positive results unfiltered. The unknown type
            // parameters in fromClass did not come into play.
            if (canCast)
            {
                result = TypeCompareState::Must;
            }
            // We have __Canon parameter(s) in fromClass, somewhere.
            //
            // In CanCastTo, these __Canon(s) won't match the interface or
            // instantiated types on the interface, so CanCastTo may
            // return false negatives.
            //
            // Only report MustNot if the fromClass is not __Canon
            // and the interface is not instantiated; then there is
            // no way for the fromClass __Canon(s) to confuse things.
            //
            //    __Canon       -> IBar             May
            //    IFoo<__Canon> -> IFoo<string>     May
            //    IFoo<__Canon> -> IBar             MustNot
            //
            else if (fromHnd == TypeHandle(g_pCanonMethodTableClass))
            {
                result = TypeCompareState::May;
            }
            else if (toHnd.HasInstantiation())
            {
                result = TypeCompareState::May;
            }
            else
            {
                result = TypeCompareState::MustNot;
            }
        }
    }

    EE_TO_JIT_TRANSITION();

    return result;
}

// Returns true if hnd1 and hnd2 are guaranteed to represent different types. Returns false if hnd1 and hnd2 may represent the same type.
static bool AreGuaranteedToRepresentDifferentTypes(TypeHandle hnd1, TypeHandle hnd2)
{
    STANDARD_VM_CONTRACT;

    CorElementType et1 = hnd1.GetSignatureCorElementType();
    CorElementType et2 = hnd2.GetSignatureCorElementType();

    TypeHandle canonHnd = TypeHandle(g_pCanonMethodTableClass);
    if ((hnd1 == canonHnd) || (hnd2 == canonHnd))
    {
        return CorTypeInfo::IsObjRef(et1) != CorTypeInfo::IsObjRef(et2);
    }

    if (et1 != et2)
    {
        return true;
    }

    switch (et1)
    {
    case ELEMENT_TYPE_ARRAY:
        if (hnd1.GetRank() != hnd2.GetRank())
            return true;
        return AreGuaranteedToRepresentDifferentTypes(hnd1.GetTypeParam(), hnd2.GetTypeParam());
    case ELEMENT_TYPE_SZARRAY:
    case ELEMENT_TYPE_BYREF:
    case ELEMENT_TYPE_PTR:
        return AreGuaranteedToRepresentDifferentTypes(hnd1.GetTypeParam(), hnd2.GetTypeParam());

    default:
        if (!hnd1.IsTypeDesc())
        {
            if (!hnd1.AsMethodTable()->HasSameTypeDefAs(hnd2.AsMethodTable()))
                return true;

            if (hnd1.AsMethodTable()->HasInstantiation())
            {
                Instantiation inst1 = hnd1.AsMethodTable()->GetInstantiation();
                Instantiation inst2 = hnd2.AsMethodTable()->GetInstantiation();
                _ASSERTE(inst1.GetNumArgs() == inst2.GetNumArgs());

                for (DWORD i = 0; i < inst1.GetNumArgs(); i++)
                {
                    if (AreGuaranteedToRepresentDifferentTypes(inst1[i], inst2[i]))
                        return true;
                }
            }
        }
        break;
    }

    return false;
}

/*********************************************************************/
// See if types represented by cls1 and cls2 compare equal, not
// equal, or the comparison needs to be resolved at runtime.
TypeCompareState CEEInfo::compareTypesForEquality(
        CORINFO_CLASS_HANDLE        cls1,
        CORINFO_CLASS_HANDLE        cls2)
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    } CONTRACTL_END;

    TypeCompareState result = TypeCompareState::May;

    JIT_TO_EE_TRANSITION();

    TypeHandle hnd1 = (TypeHandle) cls1;
    TypeHandle hnd2 = (TypeHandle) cls2;

    // If neither type is a canonical subtype, type handle comparison suffices
    if (!hnd1.IsCanonicalSubtype() && !hnd2.IsCanonicalSubtype())
    {
        result = (hnd1 == hnd2 ? TypeCompareState::Must : TypeCompareState::MustNot);
    }
    else
    {
        // If either or both types are canonical subtypes, we can sometimes prove inequality.
        if (AreGuaranteedToRepresentDifferentTypes(hnd1, hnd2))
        {
            result = TypeCompareState::MustNot;
        }
    }

    EE_TO_JIT_TRANSITION();

    return result;
}


/*********************************************************************/
static BOOL isMoreSpecificTypeHelper(TypeHandle hnd1, TypeHandle hnd2)
{
    STANDARD_VM_CONTRACT;

    // We can't really reason about equivalent types. Just
    // assume the new type is not more specific.
    if (hnd1.HasTypeEquivalence() || hnd2.HasTypeEquivalence())
    {
        return FALSE;
    }

    // If we have a mixture of shared and unshared types,
    // consider the unshared type as more specific.
    BOOL isHnd1CanonSubtype = hnd1.IsCanonicalSubtype();
    BOOL isHnd2CanonSubtype = hnd2.IsCanonicalSubtype();
    if (isHnd1CanonSubtype != isHnd2CanonSubtype)
    {
        // Only one of hnd1 and hnd2 is shared.
        // hdn2 is more specific if hnd1 is the shared type.
        return isHnd1CanonSubtype;
    }

    // Otherwise both types are either shared or not shared.
    // Look for a common parent type.
    TypeHandle merged = TypeHandle::MergeTypeHandlesToCommonParent(hnd1, hnd2);

    // If the common parent is hnd1, then hnd2 is more specific.
    return merged == hnd1;
}

// Returns true if cls2 is known to be a more specific type
// than cls1 (a subtype or more restrictive shared type).
bool CEEInfo::isMoreSpecificType(
        CORINFO_CLASS_HANDLE        cls1,
        CORINFO_CLASS_HANDLE        cls2)
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    } CONTRACTL_END;

    bool result = false;

    JIT_TO_EE_TRANSITION();

    result = isMoreSpecificTypeHelper(TypeHandle(cls1), TypeHandle(cls2));

    EE_TO_JIT_TRANSITION();
    return result;
}

static bool isExactTypeHelper(TypeHandle th)
{
    STANDARD_VM_CONTRACT;

    while (th.IsArray())
    {
        MethodTable* pMT = th.AsMethodTable();

        // Single dimensional array with non-zero bounds may be SZ array.
        if (pMT->IsMultiDimArray() && pMT->GetRank() == 1)
            return false;

        th = pMT->GetArrayElementTypeHandle();

        // Arrays of primitives are interchangeable with arrays of enums of the same underlying type.
        if (CorTypeInfo::IsPrimitiveType(th.GetVerifierCorElementType()))
            return false;
    }

    // Use conservative answer for pointers.
    if (th.IsTypeDesc())
        return false;

    MethodTable* pMT = th.AsMethodTable();

    // Use conservative answer for equivalent and variant types.
    if (pMT->HasTypeEquivalence() || pMT->HasVariance())
        return false;

    return pMT->IsSealed();
}

// Returns true if a class handle can only describe values of exactly one type.
bool CEEInfo::isExactType(CORINFO_CLASS_HANDLE cls)
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    } CONTRACTL_END;

    bool result = false;

    JIT_TO_EE_TRANSITION();

    result = isExactTypeHelper(TypeHandle(cls));

    EE_TO_JIT_TRANSITION();
    return result;
}

/*********************************************************************/
// Returns TypeCompareState::Must if cls is known to be an enum.
// For enums with known exact type returns the underlying
// type in underlyingType when the provided pointer is
// non-NULL.
// Returns TypeCompareState::May when a runtime check is required.
TypeCompareState CEEInfo::isEnum(
        CORINFO_CLASS_HANDLE        cls,
        CORINFO_CLASS_HANDLE*       underlyingType)
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    } CONTRACTL_END;

    TypeCompareState result = TypeCompareState::May;

    if (underlyingType != nullptr)
    {
        *underlyingType = nullptr;
    }

    JIT_TO_EE_TRANSITION_LEAF();

    TypeHandle th(cls);

    _ASSERTE(!th.IsNull());

    if (!th.IsGenericVariable())
    {
        if (!th.IsTypeDesc() && th.AsMethodTable()->IsEnum())
        {
            result = TypeCompareState::Must;
            if (underlyingType != nullptr)
            {
                CorElementType elemType = th.AsMethodTable()->GetInternalCorElementType();
                TypeHandle underlyingHandle(CoreLibBinder::GetElementType(elemType));
                *underlyingType = CORINFO_CLASS_HANDLE(underlyingHandle.AsPtr());
            }
        }
        else
        {
            result = TypeCompareState::MustNot;
        }
    }

    EE_TO_JIT_TRANSITION_LEAF();
    return result;
}

/*********************************************************************/
// Given a class handle, returns the Parent type.
// For COMObjectType, it returns Class Handle of System.Object.
// Returns 0 if System.Object is passed in.
CORINFO_CLASS_HANDLE CEEInfo::getParentType(
            CORINFO_CLASS_HANDLE    cls)
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    } CONTRACTL_END;

    CORINFO_CLASS_HANDLE result = NULL;

    JIT_TO_EE_TRANSITION();

    TypeHandle th(cls);

    _ASSERTE(!th.IsNull());
    _ASSERTE(!th.IsGenericVariable());

    TypeHandle thParent = th.GetParent();

#ifdef FEATURE_COMINTEROP
    // If we encounter __ComObject in the hierarchy, we need to skip it
    // since this hierarchy is introduced by the EE, but won't be present
    // in the metadata.
    if (!thParent.IsNull() && IsComObjectClass(thParent))
    {
        result = (CORINFO_CLASS_HANDLE) g_pObjectClass;
    }
    else
#endif // FEATURE_COMINTEROP
    {
        result = CORINFO_CLASS_HANDLE(thParent.AsPtr());
    }

    EE_TO_JIT_TRANSITION();

    return result;
}


/*********************************************************************/
// Returns the CorInfoType of the "child type". If the child type is
// not a primitive type, *clsRet will be set.
// Given an Array of Type Foo, returns Foo.
// Given BYREF Foo, returns Foo
CorInfoType CEEInfo::getChildType (
        CORINFO_CLASS_HANDLE       clsHnd,
        CORINFO_CLASS_HANDLE       *clsRet
        )
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    } CONTRACTL_END;

    CorInfoType ret = CORINFO_TYPE_UNDEF;
    *clsRet = 0;
    TypeHandle  retType = TypeHandle();

    JIT_TO_EE_TRANSITION();

    TypeHandle th(clsHnd);

    _ASSERTE(!th.IsNull());

    // BYREF, pointer types
    if (th.HasTypeParam())
    {
        retType = th.GetTypeParam();
    }

    if (!retType.IsNull()) {
        CorElementType type = retType.GetInternalCorElementType();
        ret = CEEInfo::asCorInfoType(type,retType, clsRet);

        // <REVISIT_TODO>What if this one is a value array ?</REVISIT_TODO>
    }

    EE_TO_JIT_TRANSITION();

    return ret;
}

/*********************************************************************/
// Check if this is a single dimensional, zero based array type
bool CEEInfo::isSDArray(CORINFO_CLASS_HANDLE  cls)
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    } CONTRACTL_END;

    bool result = false;

    JIT_TO_EE_TRANSITION();

    TypeHandle th(cls);

    _ASSERTE(!th.IsNull());

    if (th.IsArray())
    {
        // Lots of code used to think that System.Array's methodtable returns TRUE for IsArray(). It doesn't.
        _ASSERTE(th != TypeHandle(g_pArrayClass));

        result = (th.GetInternalCorElementType() == ELEMENT_TYPE_SZARRAY);
    }

    EE_TO_JIT_TRANSITION();

    return result;
}

/*********************************************************************/
// Get the number of dimensions in an array
unsigned CEEInfo::getArrayRank(CORINFO_CLASS_HANDLE  cls)
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    } CONTRACTL_END;

    unsigned result = 0;

    JIT_TO_EE_TRANSITION();

    TypeHandle th(cls);

    _ASSERTE(!th.IsNull());

    if (th.IsArray())
    {
        // Lots of code used to think that System.Array's methodtable returns TRUE for IsArray(). It doesn't.
        _ASSERTE(th != TypeHandle(g_pArrayClass));

        result = th.GetRank();
    }

    EE_TO_JIT_TRANSITION();

    return result;
}

/*********************************************************************/
// Get the index of runtime provided array method
CorInfoArrayIntrinsic CEEInfo::getArrayIntrinsicID(CORINFO_METHOD_HANDLE ftn)
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    } CONTRACTL_END;

    CorInfoArrayIntrinsic result = CorInfoArrayIntrinsic::ILLEGAL;

    JIT_TO_EE_TRANSITION();

    MethodDesc* pMD = GetMethod(ftn);

    if (pMD->IsArray())
    {
        DWORD index = ((ArrayMethodDesc*)pMD)->GetArrayFuncIndex();
        switch (index)
        {
            case 0: // ARRAY_FUNC_GET
                result = CorInfoArrayIntrinsic::GET;
                break;
            case 1: // ARRAY_FUNC_SET
                result = CorInfoArrayIntrinsic::SET;
                break;
            case 2: // ARRAY_FUNC_ADDRESS
                result = CorInfoArrayIntrinsic::ADDRESS;
                break;
        }
    }

    EE_TO_JIT_TRANSITION();

    return result;
}

/*********************************************************************/
// Get static field data for an array
// Note that it's OK to return NULL from this method.  This will cause
// the JIT to make a runtime call to InitializeArray instead of doing
// the inline optimization (thus preserving the original behavior).
void * CEEInfo::getArrayInitializationData(
            CORINFO_FIELD_HANDLE        field,
            uint32_t                    size
            )
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    } CONTRACTL_END;

    void * result = NULL;

    JIT_TO_EE_TRANSITION();

    FieldDesc* pField = (FieldDesc*) field;

    if (!pField                    ||
        !pField->IsRVA()           ||
        (pField->LoadSize() < size))
    {
        result = NULL;
    }
    else
    {
        result = pField->GetStaticAddressHandle(NULL);
    }

    EE_TO_JIT_TRANSITION();

    return result;
}

CorInfoIsAccessAllowedResult CEEInfo::canAccessClass(
            CORINFO_RESOLVED_TOKEN * pResolvedToken,
            CORINFO_METHOD_HANDLE   callerHandle,
            CORINFO_HELPER_DESC    *pAccessHelper
            )
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    } CONTRACTL_END;

    CorInfoIsAccessAllowedResult isAccessAllowed = CORINFO_ACCESS_ALLOWED;

    JIT_TO_EE_TRANSITION();

    INDEBUG(memset(pAccessHelper, 0xCC, sizeof(*pAccessHelper)));

    BOOL doAccessCheck = TRUE;
    AccessCheckOptions::AccessCheckType accessCheckType = AccessCheckOptions::kNormalAccessibilityChecks;
    DynamicResolver * pAccessContext = NULL;

    //All access checks must be done on the open instantiation.
    MethodDesc * pCallerForSecurity = GetMethodForSecurity(callerHandle);
    TypeHandle callerTypeForSecurity = TypeHandle(pCallerForSecurity->GetMethodTable());

    TypeHandle pCalleeForSecurity = TypeHandle(pResolvedToken->hClass);
    if (pResolvedToken->pTypeSpec != NULL)
    {
        SigTypeContext typeContext;
        SigTypeContext::InitTypeContext(pCallerForSecurity, &typeContext);

        SigPointer sigptr(pResolvedToken->pTypeSpec, pResolvedToken->cbTypeSpec);
        pCalleeForSecurity = sigptr.GetTypeHandleThrowing((Module *)pResolvedToken->tokenScope, &typeContext);
    }

    while (pCalleeForSecurity.HasTypeParam())
    {
        pCalleeForSecurity = pCalleeForSecurity.GetTypeParam();
    }

    if (IsDynamicScope(pResolvedToken->tokenScope))
    {
        doAccessCheck = ModifyCheckForDynamicMethod(GetDynamicResolver(pResolvedToken->tokenScope),
                                                    &callerTypeForSecurity, &accessCheckType,
                                                    &pAccessContext);
    }

    //Since this is a check against a TypeHandle, there are some things we can stick in a TypeHandle that
    //don't require access checks.
    if (pCalleeForSecurity.IsGenericVariable())
    {
        //I don't need to check for access against !!0.
        doAccessCheck = FALSE;
    }

    //Now do the visibility checks
    if (doAccessCheck)
    {
        AccessCheckOptions accessCheckOptions(accessCheckType,
                                              pAccessContext,
                                              FALSE /*throw on error*/,
                                              pCalleeForSecurity.GetMethodTable());

        _ASSERTE(pCallerForSecurity != NULL && callerTypeForSecurity != NULL);
        AccessCheckContext accessContext(pCallerForSecurity, callerTypeForSecurity.GetMethodTable());

        BOOL canAccessType = ClassLoader::CanAccessClass(&accessContext,
                                                         pCalleeForSecurity.GetMethodTable(),
                                                         pCalleeForSecurity.GetAssembly(),
                                                         accessCheckOptions);

        isAccessAllowed = canAccessType ? CORINFO_ACCESS_ALLOWED : CORINFO_ACCESS_ILLEGAL;
    }


    if (isAccessAllowed != CORINFO_ACCESS_ALLOWED)
    {
        //These all get the throw helper
        pAccessHelper->helperNum = CORINFO_HELP_CLASS_ACCESS_EXCEPTION;
        pAccessHelper->numArgs = 2;

        pAccessHelper->args[0].Set(CORINFO_METHOD_HANDLE(pCallerForSecurity));
        pAccessHelper->args[1].Set(CORINFO_CLASS_HANDLE(pCalleeForSecurity.AsPtr()));
    }

    EE_TO_JIT_TRANSITION();
    return isAccessAllowed;
}

//---------------------------------------------------------------------------------------
// Given a method descriptor ftnHnd, extract signature information into sigInfo
// Obtain (representative) instantiation information from ftnHnd's owner class
//@GENERICSVER: added explicit owner parameter
// Internal version without JIT-EE transition
static void getMethodSigInternal(
    CORINFO_METHOD_HANDLE ftnHnd,
    CORINFO_SIG_INFO *    sigRet,
    CORINFO_CLASS_HANDLE  owner,
    SignatureKind signatureKind)
{
    STANDARD_VM_CONTRACT;

    MethodDesc * ftn = GetMethod(ftnHnd);

    PCCOR_SIGNATURE pSig = NULL;
    DWORD           cbSig = 0;
    ftn->GetSig(&pSig, &cbSig);

    SigTypeContext context(ftn, (TypeHandle)owner);

    // Type parameters in the signature are instantiated
    // according to the class/method/array instantiation of ftnHnd and owner
    ConvToJitSig(
        pSig,
        cbSig,
        GetScopeHandle(ftn),
        mdTokenNil,
        &context,
        CONV_TO_JITSIG_FLAGS_NONE,
        sigRet);

    //@GENERICS:
    // Shared generic methods and shared methods on generic structs take an extra argument representing their instantiation
    if (ftn->RequiresInstArg())
    {
        //
        // If we are making a virtual call to an instance method on an interface, we need to lie to the JIT.
        // The reason being that we already made sure target is always directly callable (through instantiation stubs),
        // JIT should not generate shared generics aware call code and insert the secret argument again at the callsite.
        // Otherwise we would end up with two secret generic dictionary arguments (since the stub also provides one).
        //
        BOOL isCallSiteThatGoesThroughInstantiatingStub =
            (signatureKind == SK_VIRTUAL_CALLSITE &&
            !ftn->IsStatic() &&
            ftn->GetMethodTable()->IsInterface()) ||
            signatureKind == SK_STATIC_VIRTUAL_CODEPOINTER_CALLSITE;
        if (!isCallSiteThatGoesThroughInstantiatingStub)
            sigRet->callConv = (CorInfoCallConv) (sigRet->callConv | CORINFO_CALLCONV_PARAMTYPE);
    }

    // We want the calling convention bit to be consistant with the method attribute bit
    _ASSERTE( (IsMdStatic(ftn->GetAttrs()) == 0) == ((sigRet->callConv & CORINFO_CALLCONV_HASTHIS) != 0) );
}

/***********************************************************************/
// return the address of a pointer to a callable stub that will do the
// virtual or interface call
void CEEInfo::getCallInfo(
            CORINFO_RESOLVED_TOKEN * pResolvedToken,
            CORINFO_RESOLVED_TOKEN * pConstrainedResolvedToken,
            CORINFO_METHOD_HANDLE   callerHandle,
            CORINFO_CALLINFO_FLAGS  flags,
            CORINFO_CALL_INFO      *pResult /*out */)
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    } CONTRACTL_END;

    JIT_TO_EE_TRANSITION();

    _ASSERTE(CheckPointer(pResult));

    INDEBUG(memset(pResult, 0xCC, sizeof(*pResult)));

    pResult->stubLookup.lookupKind.needsRuntimeLookup = false;

    MethodDesc* pMD = (MethodDesc *)pResolvedToken->hMethod;
    TypeHandle th(pResolvedToken->hClass);

    _ASSERTE(pMD);
    _ASSERTE((size_t(pMD) & 0x1) == 0);

    // Spec says that a callvirt lookup ignores static methods. Since static methods
    // can't have the exact same signature as instance methods, a lookup that found
    // a static method would have never found an instance method.
    if (pMD->IsStatic() && (flags & CORINFO_CALLINFO_CALLVIRT))
    {
        EX_THROW(EEMessageException, (kMissingMethodException, IDS_EE_MISSING_METHOD, W("?")));
    }

    TypeHandle exactType = TypeHandle(pResolvedToken->hClass);

    TypeHandle constrainedType;
    if (pConstrainedResolvedToken != NULL)
    {
        constrainedType = TypeHandle(pConstrainedResolvedToken->hClass);
    }

    BOOL fIsStaticVirtualMethod = (pConstrainedResolvedToken != NULL && pMD->IsInterface() && pMD->IsStatic());

    BOOL fResolvedConstraint = FALSE;
    BOOL fForceUseRuntimeLookup = FALSE;
    BOOL fAbstractSVM = FALSE;

    // The below may need to mutate the constrained token, in which case we
    // switch to this local copy to avoid mutating the in argument.
    CORINFO_RESOLVED_TOKEN constrainedResolvedTokenCopy;

    MethodDesc * pMDAfterConstraintResolution = pMD;
    if (constrainedType.IsNull())
    {
        pResult->thisTransform = CORINFO_NO_THIS_TRANSFORM;
    }
    // <NICE> Things go wrong when this code path is used when verifying generic code.
    // It would be nice if we didn't go down this sort of code path when verifying but
    // not generating code. </NICE>
    else if (constrainedType.ContainsGenericVariables() || exactType.ContainsGenericVariables())
    {
        // <NICE> It shouldn't really matter what we do here - but the x86 JIT is annoyingly sensitive
        // about what we do, since it pretend generic variables are reference types and generates
        // an internal JIT tree even when just verifying generic code. </NICE>
        if (constrainedType.IsGenericVariable())
        {
            pResult->thisTransform = CORINFO_DEREF_THIS; // convert 'this' of type &T --> T
        }
        else if (constrainedType.IsValueType())
        {
            pResult->thisTransform = CORINFO_BOX_THIS; // convert 'this' of type &VC<T> --> boxed(VC<T>)
        }
        else
        {
            pResult->thisTransform = CORINFO_DEREF_THIS; // convert 'this' of type &C<T> --> C<T>
        }
    }
    else
    {
        // We have a "constrained." call.  Try a partial resolve of the constraint call.  Note that this
        // will not necessarily resolve the call exactly, since we might be compiling
        // shared generic code - it may just resolve it to a candidate suitable for
        // JIT compilation, and require a runtime lookup for the actual code pointer
        // to call.
        if (constrainedType.IsEnum())
        {
            // Optimize constrained calls to enum's GetHashCode method. TryResolveConstraintMethodApprox would return
            // null since the virtual method resolves to System.Enum's implementation and that's a reference type.
            // We can't do this for any other method since ToString and Equals have different semantics for enums
            // and their underlying type.
            if (pMD->GetSlot() == CoreLibBinder::GetMethod(METHOD__OBJECT__GET_HASH_CODE)->GetSlot())
            {
                // Pretend this was a "constrained. UnderlyingType" instruction prefix
                constrainedType = TypeHandle(CoreLibBinder::GetElementType(constrainedType.GetVerifierCorElementType()));

                constrainedResolvedTokenCopy = *pConstrainedResolvedToken;
                pConstrainedResolvedToken = &constrainedResolvedTokenCopy;

                // Native image signature encoder will use this field. It needs to match that pretended type, a bogus signature
                // would be produced otherwise.
                pConstrainedResolvedToken->hClass = (CORINFO_CLASS_HANDLE)constrainedType.AsPtr();

                // Clear the token and typespec because of they do not match hClass anymore.
                pConstrainedResolvedToken->token = mdTokenNil;
                pConstrainedResolvedToken->pTypeSpec = NULL;
            }
        }

        MethodDesc * directMethod = constrainedType.GetMethodTable()->TryResolveConstraintMethodApprox(
            exactType,
            pMD,
            &fForceUseRuntimeLookup);
        if (directMethod
#ifdef FEATURE_DEFAULT_INTERFACES
            && !directMethod->IsInterface() /* Could be a default interface method implementation */
#endif
            )
        {
            // Either
            //    1. no constraint resolution at compile time (!directMethod)
            // OR 2. no code sharing lookup in call
            // OR 3. we have resolved to an instantiating stub

            pMDAfterConstraintResolution = directMethod;
            _ASSERTE(!pMDAfterConstraintResolution->IsInterface());
            fResolvedConstraint = TRUE;
            pResult->thisTransform = CORINFO_NO_THIS_TRANSFORM;

            exactType = constrainedType;
        }
#ifdef FEATURE_DEFAULT_INTERFACES
        else if (directMethod && pMD->IsStatic())
        {
            if (directMethod->IsAbstract())
            {
                // This is the result when we call a SVM which is abstract, or re-abstracted
                directMethod = NULL;
                pResult->thisTransform = CORINFO_NO_THIS_TRANSFORM;
                fAbstractSVM = true;
            }
            else
            {
                // Default interface implementation of static virtual method
                pMDAfterConstraintResolution = directMethod;
                fResolvedConstraint = TRUE;
                pResult->thisTransform = CORINFO_NO_THIS_TRANSFORM;
                exactType = directMethod->GetMethodTable();
            }
        }
#endif
        else  if (constrainedType.IsValueType())
        {
            pResult->thisTransform = CORINFO_BOX_THIS;
        }
        else if (!fIsStaticVirtualMethod)
        {
            pResult->thisTransform = CORINFO_DEREF_THIS;
        }
        else
        {
            pResult->thisTransform = CORINFO_NO_THIS_TRANSFORM;
        }
    }

    //
    // Initialize callee context used for inlining and instantiation arguments
    //

    MethodDesc * pTargetMD = pMDAfterConstraintResolution;
    DWORD dwTargetMethodAttrs = pTargetMD->GetAttrs();

    pResult->exactContextNeedsRuntimeLookup = (fIsStaticVirtualMethod && !fResolvedConstraint && !constrainedType.IsNull() && constrainedType.IsCanonicalSubtype());

    if (pTargetMD->HasMethodInstantiation())
    {
        pResult->contextHandle = MAKE_METHODCONTEXT(pTargetMD);
        if (pTargetMD->GetMethodTable()->IsSharedByGenericInstantiations() || TypeHandle::IsCanonicalSubtypeInstantiation(pTargetMD->GetMethodInstantiation()))
        {
            pResult->exactContextNeedsRuntimeLookup = TRUE;
        }
    }
    else
    {
        if (!exactType.IsTypeDesc() && !pTargetMD->IsArray())
        {
            // Because of .NET's notion of base calls, exactType may point to a sub-class
            // of the actual class that defines pTargetMD.  If the JIT decides to inline, it is
            // important that they 'match', so we fix exactType here.
            exactType = pTargetMD->GetExactDeclaringType(exactType.AsMethodTable());
            _ASSERTE(!exactType.IsNull());
        }

        pResult->contextHandle = MAKE_CLASSCONTEXT(exactType.AsPtr());
        if (exactType.IsSharedByGenericInstantiations())
        {
            pResult->exactContextNeedsRuntimeLookup = TRUE;
        }

        // Use main method as the context as long as the methods are called on the same type
        if (pResult->exactContextNeedsRuntimeLookup &&
            pResolvedToken->tokenContext == METHOD_BEING_COMPILED_CONTEXT() &&
            constrainedType.IsNull() &&
            exactType == m_pMethodBeingCompiled->GetMethodTable() &&
            ((pResolvedToken->cbTypeSpec  == 0) || IsTypeSpecForTypicalInstantiation(SigPointer(pResolvedToken->pTypeSpec, pResolvedToken->cbTypeSpec))))
        {
            // The typespec signature should be only missing for dynamic methods
            _ASSERTE((pResolvedToken->cbTypeSpec != 0) || m_pMethodBeingCompiled->IsDynamicMethod());

            pResult->contextHandle = METHOD_BEING_COMPILED_CONTEXT();
        }
    }

    //
    // Determine whether to perform direct call
    //

    bool directCall = false;
    bool resolvedCallVirt = false;

    if ((flags & CORINFO_CALLINFO_LDFTN) && (!fIsStaticVirtualMethod || fResolvedConstraint))
    {
        // Since the ldvirtftn instruction resolves types
        // at run-time we do this earlier than ldftn. The
        // ldftn scenario is handled later when the fixed
        // address is requested by in the JIT.
        // See getFunctionFixedEntryPoint().
        //
        // Using ldftn or ldvirtftn on a Generic method
        // requires early type loading since instantiation
        // occurs at run-time as opposed to JIT time. The
        // GC special cases Generic types and relaxes the
        // loaded type constraint to permit Generic types
        // that are loaded with Canon as opposed to being
        // instantiated with an actual type.
        if ((flags & CORINFO_CALLINFO_CALLVIRT)
            || pTargetMD->HasMethodInstantiation())
        {
            pTargetMD->PrepareForUseAsAFunctionPointer();
        }

        directCall = true;
    }
    else
    // Static methods are always direct calls
    if (pTargetMD->IsStatic() && (!fIsStaticVirtualMethod || fResolvedConstraint))
    {
        directCall = true;
    }
    else
    if ((!fIsStaticVirtualMethod && !(flags & CORINFO_CALLINFO_CALLVIRT)) || fResolvedConstraint)
    {
        directCall = true;
    }
    else
    {
        bool devirt;
        if (pTargetMD->GetMethodTable()->IsInterface())
        {
            // Handle interface methods specially because the Sealed bit has no meaning on interfaces.
            devirt = !IsMdVirtual(dwTargetMethodAttrs);
        }
        else
        {
            devirt = !IsMdVirtual(dwTargetMethodAttrs) || IsMdFinal(dwTargetMethodAttrs) || pTargetMD->GetMethodTable()->IsSealed();
        }

        if (devirt)
        {
            resolvedCallVirt = true;
            directCall = true;
        }
    }

    if (directCall)
    {
        // Direct calls to abstract methods are not allowed
        if (IsMdAbstract(dwTargetMethodAttrs) &&
            // Compensate for always treating delegates as direct calls above
            !(((flags & CORINFO_CALLINFO_LDFTN) && (flags & CORINFO_CALLINFO_CALLVIRT) && !resolvedCallVirt))
            && !(IsMdStatic(dwTargetMethodAttrs) && fForceUseRuntimeLookup))
        {
            COMPlusThrowHR(COR_E_BADIMAGEFORMAT, BFA_BAD_IL);
        }

        bool allowInstParam = (flags & CORINFO_CALLINFO_ALLOWINSTPARAM);

        // If the target method is resolved via constrained static virtual dispatch
        // And it requires an instParam, we do not have the generic dictionary infrastructure
        // to load the correct generic context arg via EmbedGenericHandle.
        // Instead, force the call to go down the CORINFO_CALL_CODE_POINTER code path
        // which should have somewhat inferior performance. This should only actually happen in the case
        // of shared generic code calling a shared generic implementation method, which should be rare.
        //
        // An alternative design would be to add a new generic dictionary entry kind to hold the MethodDesc
        // of the constrained target instead, and use that in some circumstances; however, implementation of
        // that design requires refactoring variuos parts of the JIT interface as well as
        // TryResolveConstraintMethodApprox. In particular we would need to be abled to embed a constrained lookup
        // via EmbedGenericHandle, as well as decide in TryResolveConstraintMethodApprox if the call can be made
        // via a single use of CORINFO_CALL_CODE_POINTER, or would be better done with a CORINFO_CALL + embedded
        // constrained generic handle, or if there is a case where we would want to use both a CORINFO_CALL and
        // embedded constrained generic handle. Given the current expected high performance use case of this feature
        // which is generic numerics which will always resolve to exact valuetypes, it is not expected that
        // the complexity involved would be worth the risk. Other scenarios are not expected to be as performance
        // sensitive.
        if (IsMdStatic(dwTargetMethodAttrs) && constrainedType != NULL && pResult->exactContextNeedsRuntimeLookup)
        {
            allowInstParam = FALSE;
        }

        // Create instantiating stub if necessary
        if (!allowInstParam && pTargetMD->RequiresInstArg())
        {
            pTargetMD = MethodDesc::FindOrCreateAssociatedMethodDesc(pTargetMD,
                exactType.AsMethodTable(),
                FALSE /* forceBoxedEntryPoint */,
                pTargetMD->GetMethodInstantiation(),
                FALSE /* allowInstParam */);
        }

        // We don't allow a JIT to call the code directly if a runtime lookup is
        // needed. This is the case if
        //     1. the scan of the call token indicated that it involves code sharing
        // AND 2. the method is an instantiating stub
        //
        // In these cases the correct instantiating stub is only found via a runtime lookup.
        //
        // Note that most JITs don't call instantiating stubs directly if they can help it -
        // they call the underlying shared code and provide the type context parameter
        // explicitly. However
        //    (a) some JITs may call instantiating stubs (it makes the JIT simpler) and
        //    (b) if the method is a remote stub then the EE will force the
        //        call through an instantiating stub and
        //    (c) constraint calls that require runtime context lookup are never resolved
        //        to underlying shared generic code

        bool unresolvedLdVirtFtn = (flags & CORINFO_CALLINFO_LDFTN) && (flags & CORINFO_CALLINFO_CALLVIRT) && !resolvedCallVirt;

        if (((pResult->exactContextNeedsRuntimeLookup && pTargetMD->IsInstantiatingStub() && (!allowInstParam || fResolvedConstraint)) || fForceUseRuntimeLookup))
        {
            _ASSERTE(!m_pMethodBeingCompiled->IsDynamicMethod());

            pResult->kind = CORINFO_CALL_CODE_POINTER;

            DictionaryEntryKind entryKind;
            if (constrainedType.IsNull() || ((flags & CORINFO_CALLINFO_CALLVIRT) && !constrainedType.IsValueType()))
            {
                // For reference types, the constrained type does not affect method resolution on a callvirt, and if there is no
                // constraint, it doesn't effect it either
                entryKind = MethodEntrySlot;
            }
            else
            {
                // constrained. callvirt case where the constraint type is a valuetype
                // OR
                // constrained. call or constrained. ldftn case
                entryKind = ConstrainedMethodEntrySlot;
            }
            ComputeRuntimeLookupForSharedGenericToken(entryKind,
                                                        pResolvedToken,
                                                        pConstrainedResolvedToken,
                                                        pMD,
                                                        &pResult->codePointerLookup);
        }
        else
        {
            if (allowInstParam && pTargetMD->IsInstantiatingStub())
            {
                pTargetMD = pTargetMD->GetWrappedMethodDesc();
            }

            pResult->kind = CORINFO_CALL;
        }
        pResult->nullInstanceCheck = resolvedCallVirt;
    }
    // All virtual calls which take method instantiations must
    // currently be implemented by an indirect call via a runtime-lookup
    // function pointer
    else if (pTargetMD->HasMethodInstantiation() && !fIsStaticVirtualMethod)
    {
        pResult->kind = CORINFO_VIRTUALCALL_LDVIRTFTN;  // stub dispatch can't handle generic method calls yet
        pResult->nullInstanceCheck = TRUE;
    }
    // Non-interface dispatches go through the vtable.
    else if (!pTargetMD->IsInterface() && !fIsStaticVirtualMethod)
    {
        pResult->kind = CORINFO_VIRTUALCALL_VTABLE;
        pResult->nullInstanceCheck = TRUE;
    }
    else
    {
        // No need to null check - the dispatch code will deal with null this.
        pResult->nullInstanceCheck = FALSE;
#ifdef STUB_DISPATCH_PORTABLE
        pResult->kind = CORINFO_VIRTUALCALL_LDVIRTFTN;
#else // STUB_DISPATCH_PORTABLE
        pResult->kind = CORINFO_VIRTUALCALL_STUB;
        if (fIsStaticVirtualMethod)
        {
            pResult->kind = CORINFO_CALL_CODE_POINTER;
        }

        // We can't make stub calls when we need exact information
        // for interface calls from shared code.

        if (pResult->exactContextNeedsRuntimeLookup)
        {
            _ASSERTE(!m_pMethodBeingCompiled->IsDynamicMethod());

            ComputeRuntimeLookupForSharedGenericToken(fIsStaticVirtualMethod ? ConstrainedMethodEntrySlot : DispatchStubAddrSlot,
                                                        pResolvedToken,
                                                        pConstrainedResolvedToken,
                                                        pMD,
                                                        &pResult->stubLookup);
        }
        else
        {
            BYTE * indcell = NULL;

            // We shouldn't be using GetLoaderAllocator here because for LCG, we need to get the
            // VirtualCallStubManager from where the stub will be used.
            // For normal methods there is no difference.
            LoaderAllocator *pLoaderAllocator = m_pMethodBeingCompiled->GetLoaderAllocator();
            VirtualCallStubManager *pMgr = pLoaderAllocator->GetVirtualCallStubManager();

            PCODE addr = pMgr->GetCallStub(exactType, pTargetMD);

            // Now we want to indirect through a cell so that updates can take place atomically.
            if (m_pMethodBeingCompiled->IsLCGMethod())
            {
                // LCG methods should use recycled indcells to prevent leaks.
                indcell = pMgr->GenerateStubIndirection(addr, TRUE);

                // Add it to the per DM list so that we can recycle them when the resolver is finalized
                LCGMethodResolver *pResolver = m_pMethodBeingCompiled->AsDynamicMethodDesc()->GetLCGMethodResolver();
                pResolver->AddToUsedIndCellList(indcell);
            }
            else
            {
                // Normal methods should avoid recycled cells to preserve the locality of all indcells
                // used by one method.
                indcell = pMgr->GenerateStubIndirection(addr, FALSE);
            }

            // We use an indirect call
            pResult->stubLookup.constLookup.accessType = IAT_PVALUE;
            pResult->stubLookup.constLookup.addr = indcell;
        }
#endif // STUB_DISPATCH_PORTABLE
    }

    pResult->hMethod = CORINFO_METHOD_HANDLE(pTargetMD);

    pResult->accessAllowed = CORINFO_ACCESS_ALLOWED;
    MethodDesc* callerMethod = (MethodDesc*)callerHandle;
    if ((flags & CORINFO_CALLINFO_SECURITYCHECKS)
        && RequiresAccessCheck(pResolvedToken->tokenScope))
    {
        //Our type system doesn't always represent the target exactly with the MethodDesc.  In all cases,
        //carry around the parent MethodTable for both Caller and Callee.
        TypeHandle calleeTypeForSecurity = TypeHandle(pResolvedToken->hClass);
        MethodDesc * pCalleeForSecurity = pMD;

        MethodDesc * pCallerForSecurity = GetMethodForSecurity(callerHandle); //Should this be the open MD?

        if (pCallerForSecurity->HasClassOrMethodInstantiation())
        {
            _ASSERTE(!IsDynamicScope(pResolvedToken->tokenScope));

            SigTypeContext typeContext;
            SigTypeContext::InitTypeContext(pCallerForSecurity, &typeContext);
            _ASSERTE(!typeContext.IsEmpty());

            //If the caller is generic, load the open type and resolve the token again.  Use that for the access
            //checks.  If we don't do this then we can't tell the difference between:
            //
            //BadGeneric<T> containing a methodspec for InaccessibleType::member (illegal)
            //and
            //BadGeneric<T> containing a methodspec for !!0::member instantiated over InaccessibleType (legal)

            if (pResolvedToken->pTypeSpec != NULL)
            {
                SigPointer sigptr(pResolvedToken->pTypeSpec, pResolvedToken->cbTypeSpec);
                calleeTypeForSecurity = sigptr.GetTypeHandleThrowing((Module *)pResolvedToken->tokenScope, &typeContext);

                // typeHnd can be a variable type
                if (calleeTypeForSecurity.GetMethodTable() == NULL)
                {
                    COMPlusThrowHR(COR_E_BADIMAGEFORMAT, BFA_METHODDEF_PARENT_NO_MEMBERS);
                }
            }

            if (pCalleeForSecurity->IsArray())
            {
                // FindOrCreateAssociatedMethodDesc won't remap array method desc because of array base type
                // is not part of instantiation. We have to special case it.
                pCalleeForSecurity = calleeTypeForSecurity.GetMethodTable()->GetParallelMethodDesc(pCalleeForSecurity);
            }
            else if (pResolvedToken->pMethodSpec != NULL)
            {
                uint32_t nGenericMethodArgs = 0;
                CQuickBytes qbGenericMethodArgs;
                TypeHandle *genericMethodArgs = NULL;

                SigPointer sp(pResolvedToken->pMethodSpec, pResolvedToken->cbMethodSpec);

                BYTE etype;
                IfFailThrow(sp.GetByte(&etype));

                // Load the generic method instantiation
                THROW_BAD_FORMAT_MAYBE(etype == (BYTE)IMAGE_CEE_CS_CALLCONV_GENERICINST, 0, (Module *)pResolvedToken->tokenScope);

                IfFailThrow(sp.GetData(&nGenericMethodArgs));

                DWORD cbAllocSize = 0;
                if (!ClrSafeInt<DWORD>::multiply(nGenericMethodArgs, sizeof(TypeHandle), cbAllocSize))
                {
                    COMPlusThrowHR(COR_E_OVERFLOW);
                }

                genericMethodArgs = reinterpret_cast<TypeHandle *>(qbGenericMethodArgs.AllocThrows(cbAllocSize));

                for (uint32_t i = 0; i < nGenericMethodArgs; i++)
                {
                    genericMethodArgs[i] = sp.GetTypeHandleThrowing((Module *)pResolvedToken->tokenScope, &typeContext);
                    _ASSERTE (!genericMethodArgs[i].IsNull());
                    IfFailThrow(sp.SkipExactlyOne());
                }

                pCalleeForSecurity = MethodDesc::FindOrCreateAssociatedMethodDesc(pMD, calleeTypeForSecurity.GetMethodTable(), FALSE, Instantiation(genericMethodArgs, nGenericMethodArgs), FALSE);
            }
            else if (pResolvedToken->pTypeSpec != NULL)
            {
                pCalleeForSecurity = MethodDesc::FindOrCreateAssociatedMethodDesc(pMD, calleeTypeForSecurity.GetMethodTable(), FALSE, Instantiation(), TRUE);
            }
        }

        TypeHandle callerTypeForSecurity = TypeHandle(pCallerForSecurity->GetMethodTable());

        //Passed various link-time checks.  Now do access checks.

        BOOL doAccessCheck = TRUE;
        BOOL canAccessMethod = TRUE;
        AccessCheckOptions::AccessCheckType accessCheckType = AccessCheckOptions::kNormalAccessibilityChecks;
        DynamicResolver * pAccessContext = NULL;

        callerTypeForSecurity = TypeHandle(pCallerForSecurity->GetMethodTable());
        if (pCallerForSecurity->IsDynamicMethod())
        {
            doAccessCheck = ModifyCheckForDynamicMethod(pCallerForSecurity->AsDynamicMethodDesc()->GetResolver(),
                                                        &callerTypeForSecurity,
                                                        &accessCheckType, &pAccessContext);
        }

        pResult->accessAllowed = CORINFO_ACCESS_ALLOWED;

        if (doAccessCheck)
        {
            AccessCheckOptions accessCheckOptions(accessCheckType,
                                                  pAccessContext,
                                                  FALSE,
                                                  pCalleeForSecurity);

            _ASSERTE(pCallerForSecurity != NULL && callerTypeForSecurity != NULL);
            AccessCheckContext accessContext(pCallerForSecurity, callerTypeForSecurity.GetMethodTable());

            canAccessMethod = ClassLoader::CanAccess(&accessContext,
                                                     calleeTypeForSecurity.GetMethodTable(),
                                                     calleeTypeForSecurity.GetAssembly(),
                                                     pCalleeForSecurity->GetAttrs(),
                                                     pCalleeForSecurity,
                                                     NULL,
                                                     accessCheckOptions
                                                    );

            // If we were allowed access to the exact method, but it is on a type that has a type parameter
            // (for instance an array), we need to ensure that we also have access to the type parameter.
            if (canAccessMethod && calleeTypeForSecurity.HasTypeParam())
            {
                TypeHandle typeParam = calleeTypeForSecurity.GetTypeParam();
                while (typeParam.HasTypeParam())
                {
                    typeParam = typeParam.GetTypeParam();
                }

                _ASSERTE(pCallerForSecurity != NULL && callerTypeForSecurity != NULL);
                AccessCheckContext accessContext(pCallerForSecurity, callerTypeForSecurity.GetMethodTable());

                MethodTable* pTypeParamMT = typeParam.GetMethodTable();

                // No access check is need for Var, MVar, or FnPtr.
                if (pTypeParamMT != NULL)
                    canAccessMethod = ClassLoader::CanAccessClass(&accessContext,
                                                                  pTypeParamMT,
                                                                  typeParam.GetAssembly(),
                                                                  accessCheckOptions);
            }

            pResult->accessAllowed = canAccessMethod ? CORINFO_ACCESS_ALLOWED : CORINFO_ACCESS_ILLEGAL;
            if (!canAccessMethod)
            {
                //Check failed, fill in the throw exception helper.
                pResult->callsiteCalloutHelper.helperNum = CORINFO_HELP_METHOD_ACCESS_EXCEPTION;
                pResult->callsiteCalloutHelper.numArgs = 2;

                pResult->callsiteCalloutHelper.args[0].Set(CORINFO_METHOD_HANDLE(pCallerForSecurity));
                pResult->callsiteCalloutHelper.args[1].Set(CORINFO_METHOD_HANDLE(pCalleeForSecurity));
            }
        }
    }

    //We're pretty much done at this point.  Let's grab the rest of the information that the jit is going to
    //need.
    pResult->classFlags = getClassAttribsInternal(pResolvedToken->hClass);

    pResult->methodFlags = getMethodAttribsInternal(pResult->hMethod);

    SignatureKind signatureKind;
    if (flags & CORINFO_CALLINFO_CALLVIRT && !(pResult->kind == CORINFO_CALL))
    {
        signatureKind = SK_VIRTUAL_CALLSITE;
    }
    else if ((pResult->kind == CORINFO_CALL_CODE_POINTER) && IsMdVirtual(dwTargetMethodAttrs) && IsMdStatic(dwTargetMethodAttrs))
    {
        signatureKind = SK_STATIC_VIRTUAL_CODEPOINTER_CALLSITE;
    }
    else
    {
        signatureKind = SK_CALLSITE;
    }
    getMethodSigInternal(pResult->hMethod, &pResult->sig, (pResult->hMethod == pResolvedToken->hMethod) ? pResolvedToken->hClass : NULL, signatureKind);
    if (fIsStaticVirtualMethod && !fResolvedConstraint)
    {
        if (pResult->exactContextNeedsRuntimeLookup)
        {
            // Runtime lookup for static virtual methods always returns exact call addresses not requiring the instantiation argument
            pResult->sig.callConv = (CorInfoCallConv)(pResult->sig.callConv & ~CORINFO_CALLCONV_PARAMTYPE);
        }
        else
        {
            // Unresolved static virtual method in the absence of shared generics means
            // that the runtime needs to throw when reaching the call. SVM resolution within
            // shared generics is covered by the ConstrainedMethodEntrySlot dictionary entry.
            pResult->kind = CORINFO_CALL;
            pResult->accessAllowed = CORINFO_ACCESS_ILLEGAL;
            if (fAbstractSVM)
            {
                pResult->callsiteCalloutHelper.helperNum = CORINFO_HELP_THROW_ENTRYPOINT_NOT_FOUND_EXCEPTION;
            }
            else
            {
                pResult->callsiteCalloutHelper.helperNum = CORINFO_HELP_THROW_AMBIGUOUS_RESOLUTION_EXCEPTION;
            }
            pResult->callsiteCalloutHelper.numArgs = 3;
            pResult->callsiteCalloutHelper.args[0].methodHandle = (CORINFO_METHOD_HANDLE)pMD;
            pResult->callsiteCalloutHelper.args[0].argType = CORINFO_HELPER_ARG_TYPE_Method;
            pResult->callsiteCalloutHelper.args[1].classHandle = (CORINFO_CLASS_HANDLE)th.AsMethodTable();
            pResult->callsiteCalloutHelper.args[1].argType = CORINFO_HELPER_ARG_TYPE_Class;
            pResult->callsiteCalloutHelper.args[2].classHandle = (CORINFO_CLASS_HANDLE)constrainedType.AsMethodTable();
            pResult->callsiteCalloutHelper.args[2].argType = CORINFO_HELPER_ARG_TYPE_Class;
        }
    }

    pResult->wrapperDelegateInvoke = FALSE;

    if (m_pMethodBeingCompiled->IsDynamicMethod())
    {
        auto pMD = m_pMethodBeingCompiled->AsDynamicMethodDesc();
        if (pMD->IsILStub() && pMD->IsWrapperDelegateStub())
        {
            pResult->wrapperDelegateInvoke = TRUE;
        }
    }

    EE_TO_JIT_TRANSITION();
}


/***********************************************************************/
unsigned CEEInfo::getClassDomainID (CORINFO_CLASS_HANDLE clsHnd,
                                    void **ppIndirection)
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    } CONTRACTL_END;

    unsigned result = 0;

    if (ppIndirection != NULL)
        *ppIndirection = NULL;

    JIT_TO_EE_TRANSITION();

    TypeHandle  VMClsHnd(clsHnd);

    if (VMClsHnd.AsMethodTable()->IsDynamicStatics())
    {
        result = (unsigned)VMClsHnd.AsMethodTable()->GetModuleDynamicEntryID();
    }
    else
    {
        result = (unsigned)VMClsHnd.AsMethodTable()->GetClassIndex();
    }

    EE_TO_JIT_TRANSITION();

    return result;
}

//---------------------------------------------------------------------------------------
//
// Used by the JIT to determine whether the profiler is tracking object
// allocations
//
// Return Value:
//    bool indicating whether the profiler is tracking object allocations
//
// Notes:
//    Normally, a profiler would just directly call the inline helper to determine
//    whether the profiler set the relevant event flag (e.g.,
//    CORProfilerTrackAllocationsEnabled). However, this wrapper also asks whether we're
//    enabling the object allocated ETW event. If so,
//    we treat that the same as if the profiler requested allocation information, so that
//    the JIT will still use the profiling-friendly object allocation jit helper, so the
//    allocations can be tracked.
//

bool __stdcall TrackAllocationsEnabled()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    return (
        FALSE
#ifdef PROFILING_SUPPORTED
        || CORProfilerTrackAllocationsEnabled()
#endif // PROFILING_SUPPORTED
#ifdef FEATURE_EVENT_TRACE
        || ETW::TypeSystemLog::IsHeapAllocEventEnabled()
#endif // FEATURE_EVENT_TRACE
        );
}

/***********************************************************************/
CorInfoHelpFunc CEEInfo::getNewHelper(CORINFO_CLASS_HANDLE classHandle, bool* pHasSideEffects)
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    } CONTRACTL_END;

    CorInfoHelpFunc result = CORINFO_HELP_UNDEF;

    JIT_TO_EE_TRANSITION();

    TypeHandle  VMClsHnd(classHandle);

    if(VMClsHnd.IsTypeDesc())
    {
        COMPlusThrow(kInvalidOperationException,W("InvalidOperation_CantInstantiateFunctionPointer"));
    }

    if(VMClsHnd.IsAbstract())
    {
        COMPlusThrow(kInvalidOperationException,W("InvalidOperation_CantInstantiateAbstractClass"));
    }

    MethodTable* pMT = VMClsHnd.AsMethodTable();
    result = getNewHelperStatic(pMT, pHasSideEffects);

    _ASSERTE(result != CORINFO_HELP_UNDEF);

    EE_TO_JIT_TRANSITION();

    return result;
}

/***********************************************************************/
CorInfoHelpFunc CEEInfo::getNewHelperStatic(MethodTable * pMT, bool * pHasSideEffects)
{
    STANDARD_VM_CONTRACT;


    // Slow helper is the default
    CorInfoHelpFunc helper = CORINFO_HELP_NEWFAST;
    BOOL hasFinalizer = pMT->HasFinalizer();
    BOOL isComObjectType = pMT->IsComObjectType();

    if (isComObjectType)
    {
        *pHasSideEffects = true;
    }
    else
    {
        *pHasSideEffects = !!hasFinalizer;
    }

    if (isComObjectType)
    {
        // Use slow helper
        _ASSERTE(helper == CORINFO_HELP_NEWFAST);
    }
    else
    if ((pMT->GetBaseSize() >= LARGE_OBJECT_SIZE) ||
        hasFinalizer)
    {
        // Use slow helper
        _ASSERTE(helper == CORINFO_HELP_NEWFAST);
    }
    else
    // don't call the super-optimized one since that does not check
    // for GCStress
    if (GCStress<cfg_alloc>::IsEnabled())
    {
        // Use slow helper
        _ASSERTE(helper == CORINFO_HELP_NEWFAST);
    }
    else
#ifdef _LOGALLOC
#ifdef LOGGING
    // Super fast version doesn't do logging
    if (LoggingOn(LF_GCALLOC, LL_INFO10))
    {
        // Use slow helper
        _ASSERTE(helper == CORINFO_HELP_NEWFAST);
    }
    else
#endif // LOGGING
#endif // _LOGALLOC
    // Don't use the SFAST allocator when tracking object allocations,
    // so we don't have to instrument it.
    if (TrackAllocationsEnabled())
    {
        // Use slow helper
        _ASSERTE(helper == CORINFO_HELP_NEWFAST);
    }
    else
#ifdef FEATURE_64BIT_ALIGNMENT
    // @ARMTODO: Force all 8-byte alignment requiring allocations down one slow path. As performance
    // measurements dictate we can spread these out to faster, more specialized helpers later.
    if (pMT->RequiresAlign8())
    {
        // Use slow helper
        _ASSERTE(helper == CORINFO_HELP_NEWFAST);
    }
    else
#endif
    {
        // Use the fast helper when all conditions are met
        helper = CORINFO_HELP_NEWSFAST;
    }

#ifdef FEATURE_DOUBLE_ALIGNMENT_HINT
    // If we are use the fast allocator we also may need the
    // specialized varion for align8
    if (pMT->GetClass()->IsAlign8Candidate() &&
        (helper == CORINFO_HELP_NEWSFAST))
    {
        helper = CORINFO_HELP_NEWSFAST_ALIGN8;
    }
#endif // FEATURE_DOUBLE_ALIGNMENT_HINT

    return helper;
}

/***********************************************************************/
// <REVIEW> this only works for shared generic code because all the
// helpers are actually the same. If they were different then things might
// break because the same helper would end up getting used for different but
// representation-compatible arrays (e.g. one with a default constructor
// and one without) </REVIEW>
CorInfoHelpFunc CEEInfo::getNewArrHelper (CORINFO_CLASS_HANDLE arrayClsHnd)
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    } CONTRACTL_END;

    CorInfoHelpFunc result = CORINFO_HELP_UNDEF;

    JIT_TO_EE_TRANSITION();

    TypeHandle arrayType(arrayClsHnd);

    result = getNewArrHelperStatic(arrayType);

    _ASSERTE(result != CORINFO_HELP_UNDEF);

    EE_TO_JIT_TRANSITION();

    return result;
}

/***********************************************************************/
CorInfoHelpFunc CEEInfo::getNewArrHelperStatic(TypeHandle clsHnd)
{
    STANDARD_VM_CONTRACT;

    _ASSERTE(clsHnd.GetInternalCorElementType() == ELEMENT_TYPE_SZARRAY);

    if (GCStress<cfg_alloc>::IsEnabled())
    {
        return CORINFO_HELP_NEWARR_1_DIRECT;
    }

    CorInfoHelpFunc result = CORINFO_HELP_UNDEF;

    TypeHandle thElemType = clsHnd.GetArrayElementTypeHandle();
    CorElementType elemType = thElemType.GetInternalCorElementType();

    // This is if we're asked for newarr !0 when verifying generic code
    // Of course ideally you wouldn't even be generating code when
    // simply doing verification (we run the JIT importer in import-only
    // mode), but importing does more than one would like so we try to be
    // tolerant when asked for non-sensical helpers.
    if (CorTypeInfo::IsGenericVariable(elemType))
    {
        result = CORINFO_HELP_NEWARR_1_OBJ;
    }
    else if (CorTypeInfo::IsObjRef(elemType))
    {
        // It is an array of object refs
        result = CORINFO_HELP_NEWARR_1_OBJ;
    }
    else
    {
        // These cases always must use the slow helper
        if (
#ifdef FEATURE_64BIT_ALIGNMENT
            thElemType.RequiresAlign8() ||
#endif
            (elemType == ELEMENT_TYPE_VOID) ||
            LoggingOn(LF_GCALLOC, LL_INFO10) ||
            TrackAllocationsEnabled())
        {
            // Use the slow helper
            result = CORINFO_HELP_NEWARR_1_DIRECT;
        }
#ifdef FEATURE_DOUBLE_ALIGNMENT_HINT
        else if (elemType == ELEMENT_TYPE_R8)
        {
            // Use the Align8 fast helper
            result = CORINFO_HELP_NEWARR_1_ALIGN8;
        }
#endif
        else
        {
            // Yea, we can do it the fast way!
            result = CORINFO_HELP_NEWARR_1_VC;
        }
    }

    return result;
}

/***********************************************************************/
CorInfoHelpFunc CEEInfo::getCastingHelper(CORINFO_RESOLVED_TOKEN * pResolvedToken, bool fThrowing)
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    } CONTRACTL_END;

    CorInfoHelpFunc result = CORINFO_HELP_UNDEF;

    JIT_TO_EE_TRANSITION();

    result = getCastingHelperStatic(TypeHandle(pResolvedToken->hClass), fThrowing);

    EE_TO_JIT_TRANSITION();

    return result;
}

/***********************************************************************/
CorInfoHelpFunc CEEInfo::getCastingHelperStatic(TypeHandle clsHnd, bool fThrowing)
{
    STANDARD_VM_CONTRACT;

    // Slow helper is the default
    int helper = CORINFO_HELP_ISINSTANCEOFANY;

    if (clsHnd == TypeHandle(g_pCanonMethodTableClass))
    {
        // In shared code just use the catch-all helper for type variables, as the same
        // code may be used for interface/array/class instantiations
        //
        // We may be able to take advantage of constraints to select a specialized helper.
        // This optimizations does not seem to be warranted at the moment.
        _ASSERTE(helper == CORINFO_HELP_ISINSTANCEOFANY);
    }
    else
    if (!clsHnd.IsTypeDesc() && clsHnd.AsMethodTable()->HasVariance())
    {
        _ASSERTE(helper == CORINFO_HELP_ISINSTANCEOFANY);
    }
    else
    if (!clsHnd.IsTypeDesc() && clsHnd.AsMethodTable()->HasTypeEquivalence())
    {
        // If the type can be equivalent with something, use the slow helper
        // Note: if the type of the instance is the one marked as equivalent, it will be
        // caught by the fast helpers in the same way as they catch transparent proxies.
        _ASSERTE(helper == CORINFO_HELP_ISINSTANCEOFANY);
    }
    else
    if (clsHnd.IsInterface())
    {
        // If it is a non-variant interface, use the fast interface helper
        helper = CORINFO_HELP_ISINSTANCEOFINTERFACE;
    }
    else
    if (clsHnd.IsArray())
    {
        // If it is an array, use the fast array helper
        helper = CORINFO_HELP_ISINSTANCEOFARRAY;
    }
    else
    if (!clsHnd.IsTypeDesc() && !Nullable::IsNullableType(clsHnd))
    {
        // If it is a non-variant class, use the fast class helper
        helper = CORINFO_HELP_ISINSTANCEOFCLASS;
    }
    else
    {
        // Otherwise, use the slow helper
        _ASSERTE(helper == CORINFO_HELP_ISINSTANCEOFANY);
    }

    if (fThrowing)
    {
        const int delta = CORINFO_HELP_CHKCASTANY - CORINFO_HELP_ISINSTANCEOFANY;

        static_assert_no_msg(CORINFO_HELP_CHKCASTINTERFACE
            == CORINFO_HELP_ISINSTANCEOFINTERFACE + delta);
        static_assert_no_msg(CORINFO_HELP_CHKCASTARRAY
            == CORINFO_HELP_ISINSTANCEOFARRAY + delta);
        static_assert_no_msg(CORINFO_HELP_CHKCASTCLASS
            == CORINFO_HELP_ISINSTANCEOFCLASS + delta);

        helper += delta;
    }

    return (CorInfoHelpFunc)helper;
}

/***********************************************************************/
CorInfoHelpFunc CEEInfo::getSharedCCtorHelper(CORINFO_CLASS_HANDLE clsHnd)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_PREEMPTIVE;
    } CONTRACTL_END;

    CorInfoHelpFunc result = CORINFO_HELP_GETSHARED_NONGCSTATIC_BASE;

    JIT_TO_EE_TRANSITION_LEAF();

    TypeHandle cls(clsHnd);
    MethodTable* pMT = cls.AsMethodTable();

    if (pMT->IsDynamicStatics())
    {
        _ASSERTE(!cls.ContainsGenericVariables());
        _ASSERTE(pMT->GetModuleDynamicEntryID() != (unsigned) -1);

        result = CORINFO_HELP_CLASSINIT_SHARED_DYNAMICCLASS;
    }

    EE_TO_JIT_TRANSITION_LEAF();

    return result;
}

/***********************************************************************/
CorInfoHelpFunc CEEInfo::getUnBoxHelper(CORINFO_CLASS_HANDLE clsHnd)
{
    LIMITED_METHOD_CONTRACT;

    classMustBeLoadedBeforeCodeIsRun(clsHnd);

    TypeHandle VMClsHnd(clsHnd);
    if (Nullable::IsNullableType(VMClsHnd))
        return CORINFO_HELP_UNBOX_NULLABLE;

    return CORINFO_HELP_UNBOX;
}

/***********************************************************************/
CORINFO_OBJECT_HANDLE CEEInfo::getRuntimeTypePointer(CORINFO_CLASS_HANDLE clsHnd)
{
    CONTRACTL{
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    } CONTRACTL_END;

    CORINFO_OBJECT_HANDLE pointer = NULL;

    JIT_TO_EE_TRANSITION();

    TypeHandle typeHnd(clsHnd);
    if (!typeHnd.IsCanonicalSubtype() && typeHnd.IsManagedClassObjectPinned())
    {
        GCX_COOP();
        // ManagedClassObject is frozen here
        pointer = (CORINFO_OBJECT_HANDLE)OBJECTREFToObject(typeHnd.GetManagedClassObject());
        _ASSERT(GCHeapUtilities::GetGCHeap()->IsInFrozenSegment((Object*)pointer));
    }

    EE_TO_JIT_TRANSITION();

    return pointer;
}


/***********************************************************************/
bool CEEInfo::isObjectImmutable(CORINFO_OBJECT_HANDLE objHandle)
{
    CONTRACTL{
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    } CONTRACTL_END;

    _ASSERT(objHandle != NULL);

    bool isImmutable = false;

    JIT_TO_EE_TRANSITION();

    GCX_COOP();
    OBJECTREF obj = getObjectFromJitHandle(objHandle);
    MethodTable* type = obj->GetMethodTable();

    if (type->IsString() || type == g_pRuntimeTypeClass)
    {
        // These types are always immutable
        isImmutable = true;
    }
    else if (type->IsArray() && ((ArrayBase*)OBJECTREFToObject(obj))->GetComponentSize() == 0)
    {
        // Empty arrays are always immutable
        isImmutable = true;
    }
    else if (type->IsDelegate() || type->GetNumInstanceFields() == 0)
    {
        // Delegates and types without fields are always immutable
        isImmutable = true;
    }

    EE_TO_JIT_TRANSITION();

    return isImmutable;
}

/***********************************************************************/
bool CEEInfo::getStringChar(CORINFO_OBJECT_HANDLE obj, int index, uint16_t* value)
{
    CONTRACTL{
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    } CONTRACTL_END;

    _ASSERT(obj != NULL);

    bool result = false;

    JIT_TO_EE_TRANSITION();

    GCX_COOP();
    OBJECTREF objRef = getObjectFromJitHandle(obj);
    MethodTable* type = objRef->GetMethodTable();
    if (type->IsString())
    {
        STRINGREF strRef = (STRINGREF)objRef;
        if (strRef->GetStringLength() > (DWORD)index)
        {
            *value = strRef->GetBuffer()[index];
            result = true;
        }
    }

    EE_TO_JIT_TRANSITION();

    return result;
}

/***********************************************************************/
CORINFO_CLASS_HANDLE CEEInfo::getObjectType(CORINFO_OBJECT_HANDLE objHandle)
{
    CONTRACTL{
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    } CONTRACTL_END;

    _ASSERT(objHandle != NULL);

    CORINFO_CLASS_HANDLE handle = NULL;

    JIT_TO_EE_TRANSITION();

    GCX_COOP();
    OBJECTREF obj = getObjectFromJitHandle(objHandle);
    handle = (CORINFO_CLASS_HANDLE)obj->GetMethodTable();

    EE_TO_JIT_TRANSITION();

    return handle;
}

/***********************************************************************/
bool CEEInfo::getReadyToRunHelper(
        CORINFO_RESOLVED_TOKEN *        pResolvedToken,
        CORINFO_LOOKUP_KIND *           pGenericLookupKind,
        CorInfoHelpFunc                 id,
        CORINFO_CONST_LOOKUP *          pLookup
        )
{
    LIMITED_METHOD_CONTRACT;
    UNREACHABLE();      // only called during NGen
}

/***********************************************************************/
void CEEInfo::getReadyToRunDelegateCtorHelper(
        CORINFO_RESOLVED_TOKEN * pTargetMethod,
        mdToken                  targetConstraint,
        CORINFO_CLASS_HANDLE     delegateType,
        CORINFO_LOOKUP *   pLookup
        )
{
    LIMITED_METHOD_CONTRACT;
    UNREACHABLE();      // only called during NGen
}

/***********************************************************************/
// see code:Nullable#NullableVerification

CORINFO_CLASS_HANDLE  CEEInfo::getTypeForBox(CORINFO_CLASS_HANDLE  cls)
{
    LIMITED_METHOD_CONTRACT;

    TypeHandle VMClsHnd(cls);
    if (Nullable::IsNullableType(VMClsHnd)) {
        VMClsHnd = VMClsHnd.AsMethodTable()->GetInstantiation()[0];
    }
    return static_cast<CORINFO_CLASS_HANDLE>(VMClsHnd.AsPtr());
}

/***********************************************************************/
// see code:Nullable#NullableVerification
CorInfoHelpFunc CEEInfo::getBoxHelper(CORINFO_CLASS_HANDLE clsHnd)
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    } CONTRACTL_END;

    CorInfoHelpFunc result = CORINFO_HELP_UNDEF;

    JIT_TO_EE_TRANSITION();

    TypeHandle VMClsHnd(clsHnd);
    if (Nullable::IsNullableType(VMClsHnd))
    {
        result = CORINFO_HELP_BOX_NULLABLE;
    }
    else
    {
        if (VMClsHnd.IsTypeDesc() || !VMClsHnd.IsValueType())
            COMPlusThrow(kInvalidOperationException,W("InvalidOperation_TypeCannotBeBoxed"));

        // we shouldn't allow boxing of types that contains stack pointers
        // csc and vbc already disallow it.
        if (VMClsHnd.AsMethodTable()->IsByRefLike())
            COMPlusThrow(kInvalidProgramException,W("NotSupported_ByRefLike"));

        result = CORINFO_HELP_BOX;
    }

    EE_TO_JIT_TRANSITION();

    return result;
}

/***********************************************************************/
// registers a vararg sig & returns a class-specific cookie for it.

CORINFO_VARARGS_HANDLE CEEInfo::getVarArgsHandle(CORINFO_SIG_INFO *sig,
                                                 void **ppIndirection)
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    } CONTRACTL_END;

    CORINFO_VARARGS_HANDLE result = NULL;

    if (ppIndirection != NULL)
        *ppIndirection = NULL;

    JIT_TO_EE_TRANSITION();

    Module* module = GetModule(sig->scope);

    result = CORINFO_VARARGS_HANDLE(module->GetVASigCookie(Signature(sig->pSig, sig->cbSig)));

    EE_TO_JIT_TRANSITION();

    return result;
}

bool CEEInfo::canGetVarArgsHandle(CORINFO_SIG_INFO *sig)
{
    LIMITED_METHOD_CONTRACT;
    return true;
}

/***********************************************************************/
unsigned CEEInfo::getMethodHash (CORINFO_METHOD_HANDLE ftnHnd)
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    } CONTRACTL_END;

    unsigned result = 0;

    JIT_TO_EE_TRANSITION();

    MethodDesc* ftn = GetMethod(ftnHnd);

    result = (unsigned) ftn->GetStableHash();

    EE_TO_JIT_TRANSITION();

    return result;
}

/***********************************************************************/
size_t CEEInfo::printMethodName(CORINFO_METHOD_HANDLE ftnHnd, char* buffer, size_t bufferSize, size_t* pRequiredBufferSize)
{
    CONTRACTL {
        THROWS;
        GC_NOTRIGGER;
        MODE_PREEMPTIVE;
    } CONTRACTL_END;

    size_t bytesWritten = 0;

    JIT_TO_EE_TRANSITION();

    MethodDesc* ftn = GetMethod(ftnHnd);
    const char* ftnName = ftn->GetName();

    size_t len = strlen(ftnName);
    if (bufferSize > 0)
    {
        bytesWritten = min(len, bufferSize - 1);
        memcpy(buffer, ftnName, bytesWritten);
        buffer[bytesWritten] = '\0';
    }

    if (pRequiredBufferSize != NULL)
    {
        *pRequiredBufferSize = len + 1;
    }

    EE_TO_JIT_TRANSITION();

    return bytesWritten;
}

const char* CEEInfo::getMethodNameFromMetadata(CORINFO_METHOD_HANDLE ftnHnd, const char** className, const char** namespaceName, const char **enclosingClassName)
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    } CONTRACTL_END;

    const char* result = NULL;

    JIT_TO_EE_TRANSITION();

    if (className != NULL)
    {
        *className = NULL;
    }

    if (namespaceName != NULL)
    {
        *namespaceName = NULL;
    }

    if (enclosingClassName != NULL)
    {
        *enclosingClassName = NULL;
    }

    MethodDesc *ftn = GetMethod(ftnHnd);
    mdMethodDef token = ftn->GetMemberDef();

    if (!IsNilToken(token))
    {
        MethodTable* pMT = ftn->GetMethodTable();
        IMDInternalImport* pMDImport = pMT->GetMDImport();

        IfFailThrow(pMDImport->GetNameOfMethodDef(token, &result));
        if (className != NULL || namespaceName != NULL)
        {
            IfFailThrow(pMDImport->GetNameOfTypeDef(pMT->GetCl(), className, namespaceName));
        }
        // Query enclosingClassName when the method is in a nested class
        // and get the namespace of enclosing classes (nested class's namespace is empty)
        if ((enclosingClassName != NULL || namespaceName != NULL) && pMT->GetClass()->IsNested())
        {
            IfFailThrow(pMDImport->GetNameOfTypeDef(pMT->GetEnclosingCl(), enclosingClassName, namespaceName));
        }
    }

    EE_TO_JIT_TRANSITION();

    return result;
}

/*********************************************************************/
const char* CEEInfo::getClassNameFromMetadata(CORINFO_CLASS_HANDLE cls, const char** namespaceName)
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    } CONTRACTL_END;

    const char* result = NULL;
    const char* namespaceResult = NULL;

    JIT_TO_EE_TRANSITION();
    TypeHandle VMClsHnd(cls);

    if (!VMClsHnd.IsTypeDesc())
    {
        result = VMClsHnd.AsMethodTable()->GetFullyQualifiedNameInfo(&namespaceResult);
    }

    if (namespaceName != NULL)
    {
        *namespaceName = namespaceResult;
    }

    EE_TO_JIT_TRANSITION();

    return result;
}

/*********************************************************************/
CORINFO_CLASS_HANDLE CEEInfo::getTypeInstantiationArgument(CORINFO_CLASS_HANDLE cls, unsigned index)
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    } CONTRACTL_END;

    CORINFO_CLASS_HANDLE result = NULL;

    JIT_TO_EE_TRANSITION_LEAF();

    TypeHandle VMClsHnd(cls);
    Instantiation inst = VMClsHnd.GetInstantiation();
    TypeHandle typeArg = index < inst.GetNumArgs() ? inst[index] : NULL;
    result = CORINFO_CLASS_HANDLE(typeArg.AsPtr());

    EE_TO_JIT_TRANSITION_LEAF();

    return result;
}

/*********************************************************************/
bool CEEInfo::isIntrinsic(CORINFO_METHOD_HANDLE ftn)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_PREEMPTIVE;
    } CONTRACTL_END;

    bool ret = false;

    JIT_TO_EE_TRANSITION_LEAF();

    _ASSERTE(ftn);

    MethodDesc *pMD = (MethodDesc*)ftn;
    ret = pMD->IsIntrinsic();

    EE_TO_JIT_TRANSITION_LEAF();

    return ret;
}

bool CEEInfo::notifyMethodInfoUsage(CORINFO_METHOD_HANDLE ftn)
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    } CONTRACTL_END;

    JIT_TO_EE_TRANSITION();

    _ASSERTE(ftn);

#ifdef FEATURE_REJIT
    MethodDesc *pCallee = GetMethod(ftn);
    MethodDesc *pCaller = m_pMethodBeingCompiled;
    pCallee->GetModule()->AddInlining(pCaller, pCallee);
#endif // FEATURE_REJIT

    EE_TO_JIT_TRANSITION();

    return true;
}

/*********************************************************************/
uint32_t CEEInfo::getMethodAttribs (CORINFO_METHOD_HANDLE ftn)
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    } CONTRACTL_END;

    DWORD result = 0;

    JIT_TO_EE_TRANSITION();

    result = getMethodAttribsInternal(ftn);

    EE_TO_JIT_TRANSITION();

    return result;
}

/*********************************************************************/
DWORD CEEInfo::getMethodAttribsInternal (CORINFO_METHOD_HANDLE ftn)
{
    STANDARD_VM_CONTRACT;

/*
    returns method attribute flags (defined in corhdr.h)

    NOTE: This doesn't return certain method flags
    (mdAssem, mdFamANDAssem, mdFamORAssem, mdPrivateScope)
*/

    MethodDesc* pMD = GetMethod(ftn);

    if (pMD->IsLCGMethod())
    {
        return CORINFO_FLG_STATIC | CORINFO_FLG_DONT_INLINE;
    }

    DWORD result = 0;

    DWORD attribs = pMD->GetAttrs();

    if (IsMdStatic(attribs))
        result |= CORINFO_FLG_STATIC;
    if (pMD->IsSynchronized())
        result |= CORINFO_FLG_SYNCH;
    if (pMD->IsFCall())
        result |= CORINFO_FLG_NOGCCHECK;
    if (pMD->IsIntrinsic() || pMD->IsArray())
        result |= CORINFO_FLG_INTRINSIC;
    if (IsMdVirtual(attribs))
        result |= CORINFO_FLG_VIRTUAL;
    if (IsMdAbstract(attribs))
        result |= CORINFO_FLG_ABSTRACT;
    if (IsMdRTSpecialName(attribs))
    {
        LPCUTF8 pName = pMD->GetName();
        if (IsMdInstanceInitializer(attribs, pName) ||
            IsMdClassConstructor(attribs, pName))
            result |= CORINFO_FLG_CONSTRUCTOR;
    }

    //
    // See if we need to embed a .cctor call at the head of the
    // method body.
    //

    MethodTable* pMT = pMD->GetMethodTable();

    // method or class might have the final bit
    if (IsMdFinal(attribs) || pMT->IsSealed())
    {
        result |= CORINFO_FLG_FINAL;
    }

    if (pMD->IsEnCAddedMethod())
    {
        result |= CORINFO_FLG_EnC;
    }

    if (pMD->IsSharedByGenericInstantiations())
    {
        result |= CORINFO_FLG_SHAREDINST;
    }

    if (pMD->IsNDirect())
    {
        result |= CORINFO_FLG_PINVOKE;
    }

    if (IsMdRequireSecObject(attribs))
    {
        // Assume all methods marked as DynamicSecurity are
        // marked that way because they use StackCrawlMark to identify
        // the caller.
        // See comments in canInline or canTailCall
        result |= CORINFO_FLG_DONT_INLINE_CALLER;
    }

    // Check for the aggressive optimization directive. AggressiveOptimization only makes sense for IL methods.
    DWORD ilMethodImplAttribs = 0;
    if (pMD->IsIL())
    {
        ilMethodImplAttribs = pMD->GetImplAttrs();
        if (IsMiAggressiveOptimization(ilMethodImplAttribs))
        {
            result |= CORINFO_FLG_AGGRESSIVE_OPT;
        }
    }

    // Check for an inlining directive.
    if (pMD->IsNotInline())
    {
        /* Function marked as not inlineable */
        result |= CORINFO_FLG_DONT_INLINE;
    }
    // AggressiveInlining only makes sense for IL methods.
    else if (pMD->IsIL() && IsMiAggressiveInlining(ilMethodImplAttribs))
    {
        result |= CORINFO_FLG_FORCEINLINE;
    }

    if (pMT->IsDelegate() && ((DelegateEEClass*)(pMT->GetClass()))->GetInvokeMethod() == pMD)
    {
        // This is now used to emit efficient invoke code for any delegate invoke,
        // including multicast.
        result |= CORINFO_FLG_DELEGATE_INVOKE;
    }

    if (!g_pConfig->TieredCompilation_QuickJitForLoops())
    {
        result |= CORINFO_FLG_DISABLE_TIER0_FOR_LOOPS;
    }

    return result;
}

/*********************************************************************/
void CEEInfo::setMethodAttribs (
        CORINFO_METHOD_HANDLE ftnHnd,
        CorInfoMethodRuntimeFlags attribs)
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    } CONTRACTL_END;

    JIT_TO_EE_TRANSITION();

    MethodDesc* ftn = GetMethod(ftnHnd);

    if (attribs & CORINFO_FLG_BAD_INLINEE)
    {
        ftn->SetNotInline(true);
    }

    if (attribs & (CORINFO_FLG_SWITCHED_TO_OPTIMIZED | CORINFO_FLG_SWITCHED_TO_MIN_OPT))
    {
        PrepareCodeConfig *config = GetThread()->GetCurrentPrepareCodeConfig();
        if (config != nullptr)
        {
            if (attribs & CORINFO_FLG_SWITCHED_TO_MIN_OPT)
            {
                _ASSERTE(!ftn->IsJitOptimizationDisabled());
                config->SetJitSwitchedToMinOpt();
            }
#ifdef FEATURE_TIERED_COMPILATION
            else if (attribs & CORINFO_FLG_SWITCHED_TO_OPTIMIZED)
            {
                _ASSERTE(ftn->IsEligibleForTieredCompilation());
                config->SetJitSwitchedToOptimized();
            }
#endif
        }
    }

    EE_TO_JIT_TRANSITION();
}

/*********************************************************************/

void getMethodInfoILMethodHeaderHelper(
    COR_ILMETHOD_DECODER* header,
    CORINFO_METHOD_INFO* methInfo
    )
{
    LIMITED_METHOD_CONTRACT;

    methInfo->ILCode          = const_cast<BYTE*>(header->Code);
    methInfo->ILCodeSize      = header->GetCodeSize();
    methInfo->maxStack        = static_cast<unsigned short>(header->GetMaxStack());
    methInfo->EHcount         = static_cast<unsigned short>(header->EHCount());

    methInfo->options         =
        (CorInfoOptions)((header->GetFlags() & CorILMethod_InitLocals) ? CORINFO_OPT_INIT_LOCALS : 0) ;
}

mdToken FindGenericMethodArgTypeSpec(IMDInternalImport* pInternalImport)
{
    STANDARD_VM_CONTRACT;

    HENUMInternalHolder hEnumTypeSpecs(pInternalImport);
    mdToken token;

    static const BYTE signature[] = { ELEMENT_TYPE_MVAR, 0 };

    hEnumTypeSpecs.EnumAllInit(mdtTypeSpec);
    while (hEnumTypeSpecs.EnumNext(&token))
    {
        PCCOR_SIGNATURE pSig;
        ULONG cbSig;
        IfFailThrow(pInternalImport->GetTypeSpecFromToken(token, &pSig, &cbSig));
        if (cbSig == sizeof(signature) && memcmp(pSig, signature, cbSig) == 0)
            return token;
    }

    COMPlusThrowHR(COR_E_BADIMAGEFORMAT);
}

static void setILIntrinsicMethodInfo(CORINFO_METHOD_INFO* methInfo,uint8_t* ilcode, int ilsize, int maxstack)
{
    methInfo->ILCode = ilcode;
    methInfo->ILCodeSize = ilsize;
    methInfo->maxStack = maxstack;
    methInfo->EHcount = 0;
    methInfo->options = (CorInfoOptions)0;
}

/*********************************************************************

IL is the most efficient and portable way to implement certain low level methods
in CoreLib. Unfortunately, there is no good way to link IL into CoreLib today.
Until we find a good way to link IL into CoreLib, we will provide the IL implementation here.

- All IL intrinsincs are members of System.Runtime.CompilerServices.JitHelpers class
- All IL intrinsincs should be kept very simple. Implement the minimal reusable version of
unsafe construct and depend on inlining to do the rest.
- The C# implementation of the IL intrinsic should be good enough for functionalily. Everything should work
correctly (but slower) if the IL intrinsics are removed.

*********************************************************************/

bool getILIntrinsicImplementationForUnsafe(MethodDesc * ftn,
                                           CORINFO_METHOD_INFO * methInfo)
{
    STANDARD_VM_CONTRACT;

    _ASSERTE(CoreLibBinder::IsClass(ftn->GetMethodTable(), CLASS__UNSAFE));

    mdMethodDef tk = ftn->GetMemberDef();

    if (tk == CoreLibBinder::GetMethod(METHOD__UNSAFE__AS_POINTER)->GetMemberDef())
    {
        // Return the argument that was passed in.
        static const BYTE ilcode[] =
        {
            CEE_LDARG_0,
            CEE_CONV_U,
            CEE_RET
        };

        setILIntrinsicMethodInfo(methInfo,const_cast<BYTE*>(ilcode),sizeof(ilcode), 1);

        return true;
    }
    else if (tk == CoreLibBinder::GetMethod(METHOD__UNSAFE__BYREF_AS)->GetMemberDef() ||
             tk == CoreLibBinder::GetMethod(METHOD__UNSAFE__OBJECT_AS)->GetMemberDef() ||
             tk == CoreLibBinder::GetMethod(METHOD__UNSAFE__AS_REF_IN)->GetMemberDef())
    {
        // Return the argument that was passed in.
        static const BYTE ilcode[] =
        {
            CEE_LDARG_0,
            CEE_RET
        };

        setILIntrinsicMethodInfo(methInfo,const_cast<BYTE*>(ilcode),sizeof(ilcode), 1);

        return true;
    }
    else if (tk == CoreLibBinder::GetMethod(METHOD__UNSAFE__BYREF_ADD)->GetMemberDef() ||
             tk == CoreLibBinder::GetMethod(METHOD__UNSAFE__PTR_ADD)->GetMemberDef())
    {
        mdToken tokGenericArg = FindGenericMethodArgTypeSpec(CoreLibBinder::GetModule()->GetMDImport());

        static const BYTE ilcode[] =
        {
            CEE_LDARG_0,
            CEE_LDARG_1,
            CEE_PREFIX1, (CEE_SIZEOF & 0xFF), (BYTE)(tokGenericArg), (BYTE)(tokGenericArg >> 8), (BYTE)(tokGenericArg >> 16), (BYTE)(tokGenericArg >> 24),
            CEE_CONV_I,
            CEE_MUL,
            CEE_ADD,
            CEE_RET
        };

        setILIntrinsicMethodInfo(methInfo,const_cast<BYTE*>(ilcode),sizeof(ilcode), 3);

        return true;
    }
    else if (tk == CoreLibBinder::GetMethod(METHOD__UNSAFE__BYREF_INTPTR_ADD)->GetMemberDef() ||
             tk == CoreLibBinder::GetMethod(METHOD__UNSAFE__BYREF_UINTPTR_ADD)->GetMemberDef())
    {
        mdToken tokGenericArg = FindGenericMethodArgTypeSpec(CoreLibBinder::GetModule()->GetMDImport());

        static const BYTE ilcode[] =
        {
            CEE_LDARG_0,
            CEE_LDARG_1,
            CEE_PREFIX1, (CEE_SIZEOF & 0xFF), (BYTE)(tokGenericArg), (BYTE)(tokGenericArg >> 8), (BYTE)(tokGenericArg >> 16), (BYTE)(tokGenericArg >> 24),
            CEE_MUL,
            CEE_ADD,
            CEE_RET
        };

        setILIntrinsicMethodInfo(methInfo,const_cast<BYTE*>(ilcode),sizeof(ilcode), 3);

        return true;
    }
    else if (tk == CoreLibBinder::GetMethod(METHOD__UNSAFE__BYREF_INTPTR_ADD_BYTE_OFFSET)->GetMemberDef() ||
        tk == CoreLibBinder::GetMethod(METHOD__UNSAFE__BYREF_UINTPTR_ADD_BYTE_OFFSET)->GetMemberDef())
    {
        static const BYTE ilcode[] =
        {
            CEE_LDARG_0,
            CEE_LDARG_1,
            CEE_ADD,
            CEE_RET
        };

        setILIntrinsicMethodInfo(methInfo,const_cast<BYTE*>(ilcode),sizeof(ilcode), 2);

        return true;
    }
    else if (tk == CoreLibBinder::GetMethod(METHOD__UNSAFE__BYREF_ARE_SAME)->GetMemberDef())
    {
        // Compare the two arguments
        static const BYTE ilcode[] =
        {
            CEE_LDARG_0,
            CEE_LDARG_1,
            CEE_PREFIX1, (CEE_CEQ & 0xFF),
            CEE_RET
        };

        setILIntrinsicMethodInfo(methInfo,const_cast<BYTE*>(ilcode),sizeof(ilcode), 2);

        return true;
    }
    else if (tk == CoreLibBinder::GetMethod(METHOD__UNSAFE__PTR_BYREF_COPY)->GetMemberDef() ||
        tk == CoreLibBinder::GetMethod(METHOD__UNSAFE__BYREF_PTR_COPY)->GetMemberDef())
    {
        _ASSERTE(ftn->HasMethodInstantiation());
        Instantiation inst = ftn->GetMethodInstantiation();
        _ASSERTE(ftn->GetNumGenericMethodArgs() == 1);
        mdToken tokGenericArg = FindGenericMethodArgTypeSpec(CoreLibBinder::GetModule()->GetMDImport());

        static const BYTE ilcode[] =
        {
            CEE_LDARG_0,
            CEE_LDARG_1,
            CEE_LDOBJ, (BYTE)(tokGenericArg), (BYTE)(tokGenericArg >> 8), (BYTE)(tokGenericArg >> 16), (BYTE)(tokGenericArg >> 24),
            CEE_STOBJ, (BYTE)(tokGenericArg), (BYTE)(tokGenericArg >> 8), (BYTE)(tokGenericArg >> 16), (BYTE)(tokGenericArg >> 24),
            CEE_RET
        };

        setILIntrinsicMethodInfo(methInfo,const_cast<BYTE*>(ilcode),sizeof(ilcode), 3);

        return true;
    }
    else if (tk == CoreLibBinder::GetMethod(METHOD__UNSAFE__PTR_COPY_BLOCK)->GetMemberDef() ||
        tk == CoreLibBinder::GetMethod(METHOD__UNSAFE__BYREF_COPY_BLOCK)->GetMemberDef())
    {
        static const BYTE ilcode[] =
        {
            CEE_LDARG_0,
            CEE_LDARG_1,
            CEE_LDARG_2,
            CEE_PREFIX1, (CEE_CPBLK & 0xFF),
            CEE_RET
        };

        setILIntrinsicMethodInfo(methInfo,const_cast<BYTE*>(ilcode),sizeof(ilcode), 3);

        return true;
    }
    else if (tk == CoreLibBinder::GetMethod(METHOD__UNSAFE__PTR_COPY_BLOCK_UNALIGNED)->GetMemberDef() ||
        tk == CoreLibBinder::GetMethod(METHOD__UNSAFE__BYREF_COPY_BLOCK_UNALIGNED)->GetMemberDef())
    {
        static const BYTE ilcode[] =
        {
            CEE_LDARG_0,
            CEE_LDARG_1,
            CEE_LDARG_2,
            CEE_PREFIX1, (CEE_UNALIGNED & 0xFF), 0x01,
            CEE_PREFIX1, (CEE_CPBLK & 0xFF),
            CEE_RET
        };

        setILIntrinsicMethodInfo(methInfo,const_cast<BYTE*>(ilcode),sizeof(ilcode), 3);

        return true;
    }
    else if (tk == CoreLibBinder::GetMethod(METHOD__UNSAFE__BYREF_IS_ADDRESS_GREATER_THAN)->GetMemberDef())
    {
        // Compare the two arguments
        static const BYTE ilcode[] =
        {
            CEE_LDARG_0,
            CEE_LDARG_1,
            CEE_PREFIX1, (CEE_CGT_UN & 0xFF),
            CEE_RET
        };

        setILIntrinsicMethodInfo(methInfo,const_cast<BYTE*>(ilcode),sizeof(ilcode), 2);

        return true;
    }
    else if (tk == CoreLibBinder::GetMethod(METHOD__UNSAFE__BYREF_IS_ADDRESS_LESS_THAN)->GetMemberDef())
    {
        // Compare the two arguments
        static const BYTE ilcode[] =
        {
            CEE_LDARG_0,
            CEE_LDARG_1,
            CEE_PREFIX1, (CEE_CLT_UN & 0xFF),
            CEE_RET
        };

        setILIntrinsicMethodInfo(methInfo,const_cast<BYTE*>(ilcode),sizeof(ilcode), 2);

        return true;
    }
    else if (tk == CoreLibBinder::GetMethod(METHOD__UNSAFE__BYREF_NULLREF)->GetMemberDef())
    {
        static const BYTE ilcode[] =
        {
            CEE_LDC_I4_0,
            CEE_CONV_U,
            CEE_RET
        };

        setILIntrinsicMethodInfo(methInfo,const_cast<BYTE*>(ilcode),sizeof(ilcode), 1);

        return true;
    }
    else if (tk == CoreLibBinder::GetMethod(METHOD__UNSAFE__BYREF_IS_NULL)->GetMemberDef())
    {
        // 'ldnull' opcode would produce type o, and we can't compare & against o (ECMA-335, Table III.4).
        // However, we can compare & against native int, so we'll use that instead.

        static const BYTE ilcode[] =
        {
            CEE_LDARG_0,
            CEE_LDC_I4_0,
            CEE_CONV_U,
            CEE_PREFIX1, (CEE_CEQ & 0xFF),
            CEE_RET
        };

        setILIntrinsicMethodInfo(methInfo,const_cast<BYTE*>(ilcode),sizeof(ilcode), 2);

        return true;
    }
    else if (tk == CoreLibBinder::GetMethod(METHOD__UNSAFE__BYREF_INIT_BLOCK)->GetMemberDef() ||
            tk == CoreLibBinder::GetMethod(METHOD__UNSAFE__PTR_INIT_BLOCK)->GetMemberDef())
    {
        static const BYTE ilcode[] =
        {
            CEE_LDARG_0,
            CEE_LDARG_1,
            CEE_LDARG_2,
            CEE_PREFIX1, (CEE_INITBLK & 0xFF),
            CEE_RET
        };

        setILIntrinsicMethodInfo(methInfo,const_cast<BYTE*>(ilcode),sizeof(ilcode), 3);

        return true;
    }
    else if (tk == CoreLibBinder::GetMethod(METHOD__UNSAFE__BYREF_INIT_BLOCK_UNALIGNED)->GetMemberDef() ||
            tk == CoreLibBinder::GetMethod(METHOD__UNSAFE__PTR_INIT_BLOCK_UNALIGNED)->GetMemberDef())
    {
        static const BYTE ilcode[] =
        {
            CEE_LDARG_0,
            CEE_LDARG_1,
            CEE_LDARG_2,
            CEE_PREFIX1, (CEE_UNALIGNED & 0xFF), 0x01,
            CEE_PREFIX1, (CEE_INITBLK & 0xFF),
            CEE_RET
        };

        setILIntrinsicMethodInfo(methInfo,const_cast<BYTE*>(ilcode),sizeof(ilcode), 3);

        return true;
    }
    else if (tk == CoreLibBinder::GetMethod(METHOD__UNSAFE__BYREF_BYTE_OFFSET)->GetMemberDef())
    {
        static const BYTE ilcode[] =
        {
            CEE_LDARG_1,
            CEE_LDARG_0,
            CEE_SUB,
            CEE_RET
        };

        setILIntrinsicMethodInfo(methInfo,const_cast<BYTE*>(ilcode),sizeof(ilcode), 2);

        return true;
    }
    else if (tk == CoreLibBinder::GetMethod(METHOD__UNSAFE__BYREF_READ_UNALIGNED)->GetMemberDef() ||
             tk == CoreLibBinder::GetMethod(METHOD__UNSAFE__PTR_READ_UNALIGNED)->GetMemberDef())
    {
        _ASSERTE(ftn->HasMethodInstantiation());
        Instantiation inst = ftn->GetMethodInstantiation();
        _ASSERTE(ftn->GetNumGenericMethodArgs() == 1);
        mdToken tokGenericArg = FindGenericMethodArgTypeSpec(CoreLibBinder::GetModule()->GetMDImport());

        static const BYTE ilcode[] =
        {
            CEE_LDARG_0,
            CEE_PREFIX1, (CEE_UNALIGNED & 0xFF), 1,
            CEE_LDOBJ, (BYTE)(tokGenericArg), (BYTE)(tokGenericArg >> 8), (BYTE)(tokGenericArg >> 16), (BYTE)(tokGenericArg >> 24),
            CEE_RET
        };

        setILIntrinsicMethodInfo(methInfo,const_cast<BYTE*>(ilcode),sizeof(ilcode), 2);

        return true;
    }
    else if (tk == CoreLibBinder::GetMethod(METHOD__UNSAFE__BYREF_WRITE_UNALIGNED)->GetMemberDef() ||
             tk == CoreLibBinder::GetMethod(METHOD__UNSAFE__PTR_WRITE_UNALIGNED)->GetMemberDef())
    {
        _ASSERTE(ftn->HasMethodInstantiation());
        Instantiation inst = ftn->GetMethodInstantiation();
        _ASSERTE(ftn->GetNumGenericMethodArgs() == 1);
        mdToken tokGenericArg = FindGenericMethodArgTypeSpec(CoreLibBinder::GetModule()->GetMDImport());

        static const BYTE ilcode[] =
        {
            CEE_LDARG_0,
            CEE_LDARG_1,
            CEE_PREFIX1, (CEE_UNALIGNED & 0xFF), 1,
            CEE_STOBJ, (BYTE)(tokGenericArg), (BYTE)(tokGenericArg >> 8), (BYTE)(tokGenericArg >> 16), (BYTE)(tokGenericArg >> 24),
            CEE_RET
        };

        setILIntrinsicMethodInfo(methInfo,const_cast<BYTE*>(ilcode),sizeof(ilcode), 2);

        return true;
    }
    else if (tk == CoreLibBinder::GetMethod(METHOD__UNSAFE__READ)->GetMemberDef())
    {
        _ASSERTE(ftn->HasMethodInstantiation());
        Instantiation inst = ftn->GetMethodInstantiation();
        _ASSERTE(ftn->GetNumGenericMethodArgs() == 1);
        mdToken tokGenericArg = FindGenericMethodArgTypeSpec(CoreLibBinder::GetModule()->GetMDImport());

        static const BYTE ilcode[] =
        {
            CEE_LDARG_0,
            CEE_LDOBJ, (BYTE)(tokGenericArg), (BYTE)(tokGenericArg >> 8), (BYTE)(tokGenericArg >> 16), (BYTE)(tokGenericArg >> 24),
            CEE_RET
        };

        setILIntrinsicMethodInfo(methInfo,const_cast<BYTE*>(ilcode),sizeof(ilcode), 2);

        return true;
    }
    else if (tk == CoreLibBinder::GetMethod(METHOD__UNSAFE__SKIPINIT)->GetMemberDef())
    {
        static const BYTE ilcode[] =
        {
            CEE_RET
        };

        setILIntrinsicMethodInfo(methInfo,const_cast<BYTE*>(ilcode),sizeof(ilcode), 0);

        return true;
    }
    else if (tk == CoreLibBinder::GetMethod(METHOD__UNSAFE__BYREF_INT_SUBTRACT)->GetMemberDef() ||
             tk == CoreLibBinder::GetMethod(METHOD__UNSAFE__PTR_INT_SUBTRACT)->GetMemberDef())
    {
        mdToken tokGenericArg = FindGenericMethodArgTypeSpec(CoreLibBinder::GetModule()->GetMDImport());

        static const BYTE ilcode[] =
        {
            CEE_LDARG_0,
            CEE_LDARG_1,
            CEE_PREFIX1, (CEE_SIZEOF & 0xFF), (BYTE)(tokGenericArg), (BYTE)(tokGenericArg >> 8), (BYTE)(tokGenericArg >> 16), (BYTE)(tokGenericArg >> 24),
            CEE_CONV_I,
            CEE_MUL,
            CEE_SUB,
            CEE_RET
        };

        setILIntrinsicMethodInfo(methInfo,const_cast<BYTE*>(ilcode),sizeof(ilcode), 3);

        return true;
    }
    else if (tk == CoreLibBinder::GetMethod(METHOD__UNSAFE__BYREF_INTPTR_SUBTRACT)->GetMemberDef() ||
             tk == CoreLibBinder::GetMethod(METHOD__UNSAFE__BYREF_UINTPTR_SUBTRACT)->GetMemberDef())
    {
        mdToken tokGenericArg = FindGenericMethodArgTypeSpec(CoreLibBinder::GetModule()->GetMDImport());

        static const BYTE ilcode[] =
        {
            CEE_LDARG_0,
            CEE_LDARG_1,
            CEE_PREFIX1, (CEE_SIZEOF & 0xFF), (BYTE)(tokGenericArg), (BYTE)(tokGenericArg >> 8), (BYTE)(tokGenericArg >> 16), (BYTE)(tokGenericArg >> 24),
            CEE_MUL,
            CEE_SUB,
            CEE_RET
        };

        setILIntrinsicMethodInfo(methInfo,const_cast<BYTE*>(ilcode),sizeof(ilcode), 3);

        return true;
    }
    else if (tk == CoreLibBinder::GetMethod(METHOD__UNSAFE__BYREF_INTPTR_SUBTRACT_BYTE_OFFSET)->GetMemberDef() ||
        tk == CoreLibBinder::GetMethod(METHOD__UNSAFE__BYREF_UINTPTR_SUBTRACT_BYTE_OFFSET)->GetMemberDef())
    {
        static const BYTE ilcode[] =
        {
            CEE_LDARG_0,
            CEE_LDARG_1,
            CEE_SUB,
            CEE_RET
        };

        setILIntrinsicMethodInfo(methInfo,const_cast<BYTE*>(ilcode),sizeof(ilcode), 2);

        return true;
    }
    else if (tk == CoreLibBinder::GetMethod(METHOD__UNSAFE__WRITE)->GetMemberDef())
    {
        _ASSERTE(ftn->HasMethodInstantiation());
        Instantiation inst = ftn->GetMethodInstantiation();
        _ASSERTE(ftn->GetNumGenericMethodArgs() == 1);
        mdToken tokGenericArg = FindGenericMethodArgTypeSpec(CoreLibBinder::GetModule()->GetMDImport());

        static const BYTE ilcode[] =
        {
            CEE_LDARG_0,
            CEE_LDARG_1,
            CEE_STOBJ, (BYTE)(tokGenericArg), (BYTE)(tokGenericArg >> 8), (BYTE)(tokGenericArg >> 16), (BYTE)(tokGenericArg >> 24),
            CEE_RET
        };

        setILIntrinsicMethodInfo(methInfo,const_cast<BYTE*>(ilcode),sizeof(ilcode), 2);

        return true;
    }
    else if (tk == CoreLibBinder::GetMethod(METHOD__UNSAFE__UNBOX)->GetMemberDef())
    {
        _ASSERTE(ftn->HasMethodInstantiation());
        Instantiation inst = ftn->GetMethodInstantiation();
        _ASSERTE(ftn->GetNumGenericMethodArgs() == 1);
        mdToken tokGenericArg = FindGenericMethodArgTypeSpec(CoreLibBinder::GetModule()->GetMDImport());

        static const BYTE ilcode[] =
        {
            CEE_LDARG_0,
            CEE_UNBOX, (BYTE)(tokGenericArg), (BYTE)(tokGenericArg >> 8), (BYTE)(tokGenericArg >> 16), (BYTE)(tokGenericArg >> 24),
            CEE_RET
        };

        setILIntrinsicMethodInfo(methInfo,const_cast<BYTE*>(ilcode),sizeof(ilcode), 2);

        return true;
    }

    return false;
}

bool getILIntrinsicImplementationForInterlocked(MethodDesc * ftn,
                                                CORINFO_METHOD_INFO * methInfo)
{
    STANDARD_VM_CONTRACT;

    _ASSERTE(CoreLibBinder::IsClass(ftn->GetMethodTable(), CLASS__INTERLOCKED));

    // We are only interested if ftn's token and CompareExchange<T> token match
    if (ftn->GetMemberDef() != CoreLibBinder::GetMethod(METHOD__INTERLOCKED__COMPARE_EXCHANGE_T)->GetMemberDef())
        return false;

    // Get MethodDesc for non-generic System.Threading.Interlocked.CompareExchange()
    MethodDesc* cmpxchgObject = CoreLibBinder::GetMethod(METHOD__INTERLOCKED__COMPARE_EXCHANGE_OBJECT);

    // Setup up the body of the method
    static BYTE il[] = {
                          CEE_LDARG_0,
                          CEE_LDARG_1,
                          CEE_LDARG_2,
                          CEE_CALL,0,0,0,0,
                          CEE_RET
                        };

    // Get the token for non-generic System.Threading.Interlocked.CompareExchange(), and patch [target]
    mdMethodDef cmpxchgObjectToken = cmpxchgObject->GetMemberDef();
    il[4] = (BYTE)((int)cmpxchgObjectToken >> 0);
    il[5] = (BYTE)((int)cmpxchgObjectToken >> 8);
    il[6] = (BYTE)((int)cmpxchgObjectToken >> 16);
    il[7] = (BYTE)((int)cmpxchgObjectToken >> 24);

    // Initialize methInfo
    methInfo->ILCode = const_cast<BYTE*>(il);
    methInfo->ILCodeSize = sizeof(il);
    methInfo->maxStack = 3;
    methInfo->EHcount = 0;
    methInfo->options = (CorInfoOptions)0;

    return true;
}

bool IsBitwiseEquatable(TypeHandle typeHandle, MethodTable * methodTable)
{
    if (!methodTable->IsValueType() ||
        !CanCompareBitsOrUseFastGetHashCode(methodTable))
    {
        return false;
    }

    // CanCompareBitsOrUseFastGetHashCode checks for an object.Equals override.
    // We also need to check for an IEquatable<T> implementation.
    Instantiation inst(&typeHandle, 1);
    if (typeHandle.CanCastTo(TypeHandle(CoreLibBinder::GetClass(CLASS__IEQUATABLEGENERIC)).Instantiate(inst)))
    {
        return false;
    }

    return true;
}

bool getILIntrinsicImplementationForRuntimeHelpers(MethodDesc * ftn,
    CORINFO_METHOD_INFO * methInfo)
{
    STANDARD_VM_CONTRACT;

    _ASSERTE(CoreLibBinder::IsClass(ftn->GetMethodTable(), CLASS__RUNTIME_HELPERS));

    mdMethodDef tk = ftn->GetMemberDef();

    if (tk == CoreLibBinder::GetMethod(METHOD__RUNTIME_HELPERS__IS_REFERENCE_OR_CONTAINS_REFERENCES)->GetMemberDef())
    {
        _ASSERTE(ftn->HasMethodInstantiation());
        Instantiation inst = ftn->GetMethodInstantiation();

        _ASSERTE(ftn->GetNumGenericMethodArgs() == 1);
        TypeHandle typeHandle = inst[0];
        MethodTable * methodTable = typeHandle.GetMethodTable();

        static const BYTE returnTrue[] = { CEE_LDC_I4_1, CEE_RET };
        static const BYTE returnFalse[] = { CEE_LDC_I4_0, CEE_RET };

        if (!methodTable->IsValueType() || methodTable->ContainsPointers())
        {
            methInfo->ILCode = const_cast<BYTE*>(returnTrue);
        }
        else
        {
            methInfo->ILCode = const_cast<BYTE*>(returnFalse);
        }

        methInfo->ILCodeSize = sizeof(returnTrue);
        methInfo->maxStack = 1;
        methInfo->EHcount = 0;
        methInfo->options = (CorInfoOptions)0;
        return true;
    }

    if (tk == CoreLibBinder::GetMethod(METHOD__RUNTIME_HELPERS__IS_BITWISE_EQUATABLE)->GetMemberDef())
    {
        _ASSERTE(ftn->HasMethodInstantiation());
        Instantiation inst = ftn->GetMethodInstantiation();

        _ASSERTE(ftn->GetNumGenericMethodArgs() == 1);
        TypeHandle typeHandle = inst[0];
        MethodTable * methodTable = typeHandle.GetMethodTable();

        static const BYTE returnTrue[] = { CEE_LDC_I4_1, CEE_RET };
        static const BYTE returnFalse[] = { CEE_LDC_I4_0, CEE_RET };

        // Ideally we could detect automatically whether a type is trivially equatable
        // (i.e., its operator == could be implemented via memcmp). The best we can do
        // for now is hardcode a list of known supported types and then also include anything
        // that doesn't provide its own object.Equals override / IEquatable<T> implementation.
        // n.b. This doesn't imply that the type's CompareTo method can be memcmp-implemented,
        // as a method like CompareTo may need to take a type's signedness into account.

        if (methodTable == CoreLibBinder::GetClass(CLASS__BOOLEAN)
            || methodTable == CoreLibBinder::GetClass(CLASS__BYTE)
            || methodTable == CoreLibBinder::GetClass(CLASS__SBYTE)
            || methodTable == CoreLibBinder::GetClass(CLASS__CHAR)
            || methodTable == CoreLibBinder::GetClass(CLASS__INT16)
            || methodTable == CoreLibBinder::GetClass(CLASS__UINT16)
            || methodTable == CoreLibBinder::GetClass(CLASS__INT32)
            || methodTable == CoreLibBinder::GetClass(CLASS__UINT32)
            || methodTable == CoreLibBinder::GetClass(CLASS__INT64)
            || methodTable == CoreLibBinder::GetClass(CLASS__UINT64)
            || methodTable == CoreLibBinder::GetClass(CLASS__INTPTR)
            || methodTable == CoreLibBinder::GetClass(CLASS__UINTPTR)
            || methodTable == CoreLibBinder::GetClass(CLASS__RUNE)
            || methodTable->IsEnum()
            || IsBitwiseEquatable(typeHandle, methodTable))
        {
            methInfo->ILCode = const_cast<BYTE*>(returnTrue);
        }
        else
        {
            methInfo->ILCode = const_cast<BYTE*>(returnFalse);
        }

        methInfo->ILCodeSize = sizeof(returnTrue);
        methInfo->maxStack = 1;
        methInfo->EHcount = 0;
        methInfo->options = (CorInfoOptions)0;
        return true;
    }

    if (tk == CoreLibBinder::GetMethod(METHOD__RUNTIME_HELPERS__GET_METHOD_TABLE)->GetMemberDef())
    {
        mdToken tokRawData = CoreLibBinder::GetField(FIELD__RAW_DATA__DATA)->GetMemberDef();

        // In the CLR, an object is laid out as follows.
        // [ object_header || MethodTable* (64-bit pointer) || instance_data ]
        //                    ^                                ^-- ref <theObj>.firstField points here
        //                    `-- <theObj> reference (type O) points here
        //
        // So essentially what we want to do is to turn an object reference (type O) into a
        // native int&, then dereference it to get the MethodTable*. (Essentially, an object
        // reference is a MethodTable**.) Per ECMA-335, Sec. III.1.5, we can add
        // (but not subtract) a & and an int32 to produce a &. So we'll get a reference to
        // <theObj>.firstField (type &), then back up one pointer length to get a value of
        // essentially type (MethodTable*)&. Both of these are legal GC-trackable references
        // to <theObj>, regardless of <theObj>'s actual length.

        static BYTE ilcode[] = { CEE_LDARG_0,         // stack contains [ O ] = <theObj>
                                 CEE_LDFLDA,0,0,0,0,  // stack contains [ & ] = ref <theObj>.firstField
                                 CEE_LDC_I4_S,(BYTE)(-TARGET_POINTER_SIZE), // stack contains [ &, int32 ] = -IntPtr.Size
                                 CEE_ADD,             // stack contains [ & ] = ref <theObj>.methodTablePtr
                                 CEE_LDIND_I,         // stack contains [ native int ] = <theObj>.methodTablePtr
                                 CEE_RET };

        ilcode[2] = (BYTE)(tokRawData);
        ilcode[3] = (BYTE)(tokRawData >> 8);
        ilcode[4] = (BYTE)(tokRawData >> 16);
        ilcode[5] = (BYTE)(tokRawData >> 24);

        methInfo->ILCode = const_cast<BYTE*>(ilcode);
        methInfo->ILCodeSize = sizeof(ilcode);
        methInfo->maxStack = 2;
        methInfo->EHcount = 0;
        methInfo->options = (CorInfoOptions)0;
        return true;
    }

    if (tk == CoreLibBinder::GetMethod(METHOD__RUNTIME_HELPERS__ENUM_EQUALS)->GetMemberDef())
    {
        // Normally we would follow the above pattern and unconditionally replace the IL,
        // relying on generic type constraints to guarantee that it will only ever be instantiated
        // on the type/size of argument we expect.
        //
        // However C#/CLR does not support restricting a generic type to be an Enum, so the best
        // we can do is constrain it to be a value type.  This is fine for run time, since we only
        // ever create instantiations on 4 byte or less Enums. But during NGen we may compile instantiations
        // on other value types (to be specific, every value type instatiation of EqualityComparer
        // because of its TypeDependencyAttribute; here again we would like to restrict this to
        // 4 byte or less Enums but cannot).
        //
        // This IL is invalid for those instantiations, and replacing it would lead to all sorts of
        // errors at NGen time.  So we only replace it for instantiations where it would be valid,
        // leaving the others, which we should never execute, with the C# implementation of throwing.

        _ASSERTE(ftn->HasMethodInstantiation());
        Instantiation inst = ftn->GetMethodInstantiation();

        _ASSERTE(inst.GetNumArgs() == 1);
        CorElementType et = inst[0].GetVerifierCorElementType();
        if (et == ELEMENT_TYPE_I4 ||
            et == ELEMENT_TYPE_U4 ||
            et == ELEMENT_TYPE_I2 ||
            et == ELEMENT_TYPE_U2 ||
            et == ELEMENT_TYPE_I1 ||
            et == ELEMENT_TYPE_U1 ||
            et == ELEMENT_TYPE_I8 ||
            et == ELEMENT_TYPE_U8)
        {
            static const BYTE ilcode[] = { CEE_LDARG_0, CEE_LDARG_1, CEE_PREFIX1, (CEE_CEQ & 0xFF), CEE_RET };
            methInfo->ILCode = const_cast<BYTE*>(ilcode);
            methInfo->ILCodeSize = sizeof(ilcode);
            methInfo->maxStack = 2;
            methInfo->EHcount = 0;
            methInfo->options = (CorInfoOptions)0;
            return true;
        }
    }
    else if (tk == CoreLibBinder::GetMethod(METHOD__RUNTIME_HELPERS__ENUM_COMPARE_TO)->GetMemberDef())
    {
        // The comment above on why this is not an unconditional replacement.  This case handles
        // Enums backed by 8 byte values.

        _ASSERTE(ftn->HasMethodInstantiation());
        Instantiation inst = ftn->GetMethodInstantiation();

        _ASSERTE(inst.GetNumArgs() == 1);
        CorElementType et = inst[0].GetVerifierCorElementType();
        if (et == ELEMENT_TYPE_I4 ||
            et == ELEMENT_TYPE_U4 ||
            et == ELEMENT_TYPE_I2 ||
            et == ELEMENT_TYPE_U2 ||
            et == ELEMENT_TYPE_I1 ||
            et == ELEMENT_TYPE_U1 ||
            et == ELEMENT_TYPE_I8 ||
            et == ELEMENT_TYPE_U8)
        {
            static BYTE ilcode[8][9];

            TypeHandle thUnderlyingType = CoreLibBinder::GetElementType(et);

            TypeHandle thIComparable = TypeHandle(CoreLibBinder::GetClass(CLASS__ICOMPARABLEGENERIC)).Instantiate(Instantiation(&thUnderlyingType, 1));

            MethodDesc* pCompareToMD = thUnderlyingType.AsMethodTable()->GetMethodDescForInterfaceMethod(
                thIComparable, CoreLibBinder::GetMethod(METHOD__ICOMPARABLEGENERIC__COMPARE_TO), TRUE /* throwOnConflict */);

            // Call CompareTo method on the primitive type
            int tokCompareTo = pCompareToMD->GetMemberDef();

            unsigned int index = (et - ELEMENT_TYPE_I1);
            _ASSERTE(index < ARRAY_SIZE(ilcode));

            ilcode[index][0] = CEE_LDARGA_S;
            ilcode[index][1] = 0;
            ilcode[index][2] = CEE_LDARG_1;
            ilcode[index][3] = CEE_CALL;
            ilcode[index][4] = (BYTE)(tokCompareTo);
            ilcode[index][5] = (BYTE)(tokCompareTo >> 8);
            ilcode[index][6] = (BYTE)(tokCompareTo >> 16);
            ilcode[index][7] = (BYTE)(tokCompareTo >> 24);
            ilcode[index][8] = CEE_RET;

            methInfo->ILCode = const_cast<BYTE*>(ilcode[index]);
            methInfo->ILCodeSize = sizeof(ilcode[index]);
            methInfo->maxStack = 2;
            methInfo->EHcount = 0;
            methInfo->options = (CorInfoOptions)0;
            return true;
        }
    }

    return false;
}

bool getILIntrinsicImplementationForActivator(MethodDesc* ftn,
    CORINFO_METHOD_INFO* methInfo,
    SigPointer* pSig)
{
    STANDARD_VM_CONTRACT;

    _ASSERTE(CoreLibBinder::IsClass(ftn->GetMethodTable(), CLASS__ACTIVATOR));

    // We are only interested if ftn's token and CreateInstance<T> token match
    if (ftn->GetMemberDef() != CoreLibBinder::GetMethod(METHOD__ACTIVATOR__CREATE_INSTANCE_OF_T)->GetMemberDef())
        return false;

    _ASSERTE(ftn->HasMethodInstantiation());
    Instantiation inst = ftn->GetMethodInstantiation();

    _ASSERTE(ftn->GetNumGenericMethodArgs() == 1);
    TypeHandle typeHandle = inst[0];
    MethodTable* methodTable = typeHandle.GetMethodTable();

    if (!methodTable->IsValueType() || methodTable->HasDefaultConstructor())
        return false;

    // Replace the body with implementation that just returns "default"
    MethodDesc* createDefaultInstance = CoreLibBinder::GetMethod(METHOD__ACTIVATOR__CREATE_DEFAULT_INSTANCE_OF_T);
    COR_ILMETHOD_DECODER header(createDefaultInstance->GetILHeader(FALSE), createDefaultInstance->GetMDImport(), NULL);
    getMethodInfoILMethodHeaderHelper(&header, methInfo);
    *pSig = SigPointer(header.LocalVarSig, header.cbLocalVarSig);

    return true;
}

//---------------------------------------------------------------------------------------
//

class MethodInfoHelperContext final
{
public:
    MethodDesc* Method;
    COR_ILMETHOD_DECODER* Header;
    DynamicResolver* TransientResolver;

    MethodInfoHelperContext(MethodDesc* pMD, _In_opt_ COR_ILMETHOD_DECODER* header = NULL)
        : Method{ pMD }
        , Header{ header }
        , TransientResolver{}
    {
        LIMITED_METHOD_CONTRACT;
        _ASSERTE(pMD != NULL);
    }

    MethodInfoHelperContext(const MethodInfoHelperContext&) = delete;
    MethodInfoHelperContext(MethodInfoHelperContext&& other) = delete;

    ~MethodInfoHelperContext()
    {
        STANDARD_VM_CONTRACT;
        delete TransientResolver;
    }

    MethodInfoHelperContext& operator=(const MethodInfoHelperContext&) = delete;
    MethodInfoHelperContext& operator=(MethodInfoHelperContext&& other) = delete;

    bool HasTransientMethodDetails() const
    {
        LIMITED_METHOD_CONTRACT;
        return TransientResolver != NULL;
    }

    CORINFO_MODULE_HANDLE CreateScopeHandle() const
    {
        LIMITED_METHOD_CONTRACT;
        _ASSERTE(HasTransientMethodDetails());
        return MakeDynamicScope(TransientResolver);
    }

    // This operation transfers any ownership to a
    // TransientMethodDetails instance.
    TransientMethodDetails CreateTransientMethodDetails()
    {
        STANDARD_VM_CONTRACT;
        _ASSERTE(HasTransientMethodDetails());

        CORINFO_MODULE_HANDLE handle = CreateScopeHandle();
        TransientResolver = NULL;
        return TransientMethodDetails{ Method, Header, handle };
    }

    void UpdateWith(const TransientMethodDetails& details)
    {
        LIMITED_METHOD_CONTRACT;
        Header = details.Header;
        TransientResolver = details.Scope != NULL ? GetDynamicResolver(details.Scope) : NULL;
    }

    void TakeOwnership(TransientMethodDetails details)
    {
        LIMITED_METHOD_CONTRACT;
        UpdateWith(details);

        // Default initialize local instance since we are
        // taking ownership of the transient method details.
        details = {};
    }
};

static void getMethodInfoHelper(
    MethodInfoHelperContext& cxt,
    CORINFO_METHOD_INFO* methInfo,
    CORINFO_CONTEXT_HANDLE exactContext = NULL)
{
    STANDARD_VM_CONTRACT;
    _ASSERTE(methInfo != NULL);

    MethodDesc* ftn      = cxt.Method;
    methInfo->ftn        = (CORINFO_METHOD_HANDLE)ftn;
    methInfo->regionKind = CORINFO_REGION_JIT;

    CORINFO_MODULE_HANDLE scopeHnd = NULL;

    /* Grab information from the IL header */
    PCCOR_SIGNATURE pLocalSig   = NULL;
    uint32_t        cbLocalSig  = 0;

    // Having a header means there is backing IL for the method.
    if (NULL != cxt.Header)
    {
        scopeHnd = cxt.HasTransientMethodDetails()
            ? cxt.CreateScopeHandle()
            : GetScopeHandle(ftn);
        bool fILIntrinsic = false;

        MethodTable * pMT  = ftn->GetMethodTable();

        if (ftn->IsIntrinsic())
        {
            if (CoreLibBinder::IsClass(pMT, CLASS__UNSAFE))
            {
                fILIntrinsic = getILIntrinsicImplementationForUnsafe(ftn, methInfo);
            }
            else if (CoreLibBinder::IsClass(pMT, CLASS__INTERLOCKED))
            {
                fILIntrinsic = getILIntrinsicImplementationForInterlocked(ftn, methInfo);
            }
            else if (CoreLibBinder::IsClass(pMT, CLASS__RUNTIME_HELPERS))
            {
                fILIntrinsic = getILIntrinsicImplementationForRuntimeHelpers(ftn, methInfo);
            }
            else if (CoreLibBinder::IsClass(pMT, CLASS__ACTIVATOR))
            {
                SigPointer localSig;
                fILIntrinsic = getILIntrinsicImplementationForActivator(ftn, methInfo, &localSig);
                if (fILIntrinsic)
                {
                    localSig.GetSignature(&pLocalSig, &cbLocalSig);
                }
            }
        }

        if (!fILIntrinsic)
        {
            getMethodInfoILMethodHeaderHelper(cxt.Header, methInfo);
            pLocalSig = cxt.Header->LocalVarSig;
            cbLocalSig = cxt.Header->cbLocalVarSig;
        }
    }
    else if (ftn->IsDynamicMethod())
    {
        DynamicResolver* pResolver = ftn->AsDynamicMethodDesc()->GetResolver();
        scopeHnd = MakeDynamicScope(pResolver);

        unsigned int EHCount;
        methInfo->ILCode = pResolver->GetCodeInfo(&methInfo->ILCodeSize,
                                                  &methInfo->maxStack,
                                                  &methInfo->options,
                                                  &EHCount);
        methInfo->EHcount = (unsigned short)EHCount;
        SigPointer localSig = pResolver->GetLocalSig();
        localSig.GetSignature(&pLocalSig, &cbLocalSig);
    }
    else
    {
        if (!ftn->TryGenerateUnsafeAccessor(&cxt.TransientResolver, &cxt.Header))
            ThrowHR(COR_E_BADIMAGEFORMAT);

        scopeHnd = cxt.CreateScopeHandle();

        _ASSERTE(cxt.Header != NULL);
        getMethodInfoILMethodHeaderHelper(cxt.Header, methInfo);
        pLocalSig = cxt.Header->LocalVarSig;
        cbLocalSig = cxt.Header->cbLocalVarSig;
    }

    _ASSERTE(scopeHnd != NULL);
    methInfo->scope = scopeHnd;
    methInfo->options = (CorInfoOptions)(((UINT32)methInfo->options) |
                            ((ftn->AcquiresInstMethodTableFromThis() ? CORINFO_GENERICS_CTXT_FROM_THIS : 0) |
                             (ftn->RequiresInstMethodTableArg() ? CORINFO_GENERICS_CTXT_FROM_METHODTABLE : 0) |
                             (ftn->RequiresInstMethodDescArg() ? CORINFO_GENERICS_CTXT_FROM_METHODDESC : 0)));

    // EEJitManager::ResolveEHClause and CrawlFrame::GetExactGenericInstantiations
    // need to be able to get to CORINFO_GENERICS_CTXT_MASK if there are any
    // catch clauses like "try {} catch(MyException<T> e) {}".
    // Such constructs are rare, and having to extend the lifetime of variable
    // for such cases is reasonable

    if (methInfo->options & CORINFO_GENERICS_CTXT_MASK)
    {
#if defined(PROFILING_SUPPORTED)
        BOOL fProfilerRequiresGenericsContextForEnterLeave = FALSE;
        {
            BEGIN_PROFILER_CALLBACK(CORProfilerPresent());
            if ((&g_profControlBlock)->RequiresGenericsContextForEnterLeave())
            {
                fProfilerRequiresGenericsContextForEnterLeave = TRUE;
            }
            END_PROFILER_CALLBACK();
        }
        if (fProfilerRequiresGenericsContextForEnterLeave)
        {
            methInfo->options = CorInfoOptions(methInfo->options|CORINFO_GENERICS_CTXT_KEEP_ALIVE);
        }
#endif // defined(PROFILING_SUPPORTED)
    }

    PCCOR_SIGNATURE pSig = NULL;
    DWORD           cbSig = 0;
    ftn->GetSig(&pSig, &cbSig);

    SigTypeContext context;

    if (exactContext == NULL || exactContext == METHOD_BEING_COMPILED_CONTEXT())
    {
        SigTypeContext::InitTypeContext(ftn, &context);
    }
    else if (((size_t)exactContext & CORINFO_CONTEXTFLAGS_MASK) == CORINFO_CONTEXTFLAGS_METHOD)
    {
        MethodDesc* contextMD = (MethodDesc*)((size_t)exactContext & ~CORINFO_CONTEXTFLAGS_MASK);
        SigTypeContext::InitTypeContext(contextMD, &context);
    }
    else
    {
        _ASSERT(((size_t)exactContext & CORINFO_CONTEXTFLAGS_MASK) == CORINFO_CONTEXTFLAGS_CLASS);
        TypeHandle th = TypeHandle((CORINFO_CLASS_HANDLE)((size_t)exactContext & ~CORINFO_CONTEXTFLAGS_MASK));
        SigTypeContext::InitTypeContext(th, &context);
    }

    /* Fetch the method signature */
    // Type parameters in the signature should be instantiated according to the
    // class/method/array instantiation of ftnHnd
    ConvToJitSig(
        pSig,
        cbSig,
        methInfo->scope,
        mdTokenNil,
        &context,
        CONV_TO_JITSIG_FLAGS_NONE,
        &methInfo->args);

    // Shared generic or static per-inst methods and shared methods on generic structs
    // take an extra argument representing their instantiation
    if (ftn->RequiresInstArg())
        methInfo->args.callConv = (CorInfoCallConv)(methInfo->args.callConv | CORINFO_CALLCONV_PARAMTYPE);

    _ASSERTE((IsMdStatic(ftn->GetAttrs()) == 0) == ((methInfo->args.callConv & CORINFO_CALLCONV_HASTHIS) != 0));

    /* And its local variables */
    // Type parameters in the signature should be instantiated according to the
    // class/method/array instantiation of ftnHnd
    ConvToJitSig(
        pLocalSig,
        cbLocalSig,
        methInfo->scope,
        mdTokenNil,
        &context,
        CONV_TO_JITSIG_FLAGS_LOCALSIG,
        &methInfo->locals);
} // getMethodInfoHelper

//---------------------------------------------------------------------------------------
//
bool
CEEInfo::getMethodInfo(
    CORINFO_METHOD_HANDLE ftnHnd,
    CORINFO_METHOD_INFO * methInfo,
    CORINFO_CONTEXT_HANDLE context)
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    } CONTRACTL_END;

    bool result = false;

    JIT_TO_EE_TRANSITION();

    MethodDesc * ftn = GetMethod(ftnHnd);

    // Get the IL header
    MethodInfoHelperContext cxt{ ftn };
    if (ftn->IsDynamicMethod())
    {
        getMethodInfoHelper(cxt, methInfo, context);
        result = true;
    }
    else if (!ftn->IsWrapperStub() && ftn->HasILHeader())
    {
        COR_ILMETHOD_DECODER header(ftn->GetILHeader(TRUE), ftn->GetMDImport(), NULL);
        cxt.Header = &header;
        getMethodInfoHelper(cxt, methInfo, context);
        result = true;
    }
    else if (ftn->IsIL() && ftn->GetRVA() == 0)
    {
        // We will either find or create transient method details.
        _ASSERTE(!cxt.HasTransientMethodDetails());

        // IL methods with no RVA indicate there is no implementation defined in metadata.
        // Check if we previously generated details/implementation for this method.
        TransientMethodDetails* detailsMaybe = NULL;
        if (FindTransientMethodDetails(ftn, &detailsMaybe))
            cxt.UpdateWith(*detailsMaybe);

        getMethodInfoHelper(cxt, methInfo);

        // If we have transient method details we need to handle
        // the lifetime of the details.
        if (cxt.HasTransientMethodDetails())
        {
            // If we didn't find transient details, but we have them
            // after getting method info, store them for cleanup.
            if (detailsMaybe == NULL)
                AddTransientMethodDetails(cxt.CreateTransientMethodDetails());

            // Reset the context because ownership has either been
            // transferred or was found in this instance.
            cxt.UpdateWith({});
        }

        result = true;
    }

    if (result)
    {
        LOG((LF_JIT, LL_INFO100000, "Getting method info (possible inline) %s::%s%s\n",
            ftn->m_pszDebugClassName, ftn->m_pszDebugMethodName, ftn->m_pszDebugMethodSignature));
    }

    EE_TO_JIT_TRANSITION();

    return result;
}

bool CEEInfo::haveSameMethodDefinition(
    CORINFO_METHOD_HANDLE meth1Hnd,
    CORINFO_METHOD_HANDLE meth2Hnd)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_PREEMPTIVE;
    } CONTRACTL_END;

    bool result = false;

    JIT_TO_EE_TRANSITION_LEAF();

    MethodDesc* meth1 = GetMethod(meth1Hnd);
    MethodDesc* meth2 = GetMethod(meth2Hnd);
    result = meth1->HasSameMethodDefAs(meth2);

    EE_TO_JIT_TRANSITION_LEAF();

    return result;
}

/*************************************************************
 * Check if the caller and calle are in the same assembly
 * i.e. do not inline across assemblies
 *************************************************************/

CorInfoInline CEEInfo::canInline (CORINFO_METHOD_HANDLE hCaller,
                                  CORINFO_METHOD_HANDLE hCallee)
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    } CONTRACTL_END;

    CorInfoInline result = INLINE_PASS;  // By default we pass.
                                         // Do not set pass in the rest of the method.
    const char *  szFailReason = NULL;   // for reportInlineDecision

    JIT_TO_EE_TRANSITION();

    // This does not work in the multi-threaded case
#if 0
    // Caller should check this condition first
    _ASSERTE(!(CORINFO_FLG_DONT_INLINE & getMethodAttribsInternal(hCallee)));
#endif

    MethodDesc* pCaller = GetMethod(hCaller);
    MethodDesc* pCallee = GetMethod(hCallee);

    if (pCallee->IsNoMetadata())
    {
        result = INLINE_FAIL;
        szFailReason = "Inlinee is NoMetadata";
        goto exit;
    }

#ifdef DEBUGGING_SUPPORTED

    // If the callee wants debuggable code, don't allow it to be inlined

    {
        // Combining the next two lines, and eliminating jitDebuggerFlags, leads to bad codegen in x86 Release builds using Visual C++ 19.00.24215.1.
        CORJIT_FLAGS jitDebuggerFlags = GetDebuggerCompileFlags(pCallee->GetModule(), CORJIT_FLAGS());
        if (jitDebuggerFlags.IsSet(CORJIT_FLAGS::CORJIT_FLAG_DEBUG_CODE))
        {
            result = INLINE_NEVER;
            szFailReason = "Inlinee is debuggable";
            goto exit;
        }
    }
#endif

    // The original caller is the current method
    MethodDesc *  pOrigCaller;
    pOrigCaller = m_pMethodBeingCompiled;
    Module *      pOrigCallerModule;
    pOrigCallerModule = pOrigCaller->GetLoaderModule();

    if (pCallee->IsNotInline())
    {
        result = INLINE_NEVER;
        szFailReason = "Inlinee is marked as no inline";
        goto exit;
    }

    // Also check to see if the method requires a security object.  This means they call demand and
    // shouldn't be inlined.
    if (IsMdRequireSecObject(pCallee->GetAttrs()))
    {
        result = INLINE_NEVER;
        szFailReason = "Inlinee requires a security object (or contains StackCrawlMark)";
        goto exit;
    }

    // If the method is MethodImpl'd by another method within the same type, then we have
    // an issue that the importer will import the wrong body. In this case, we'll just
    // disallow inlining because getFunctionEntryPoint will do the right thing.
    {
        MethodDesc  *pMDDecl = pCallee;
        MethodTable *pMT     = pMDDecl->GetMethodTable();
        MethodDesc  *pMDImpl = pMT->MapMethodDeclToMethodImpl(pMDDecl);

        if (pMDDecl != pMDImpl)
        {
            result = INLINE_NEVER;
            szFailReason = "Inlinee is MethodImpl'd by another method within the same type";
            goto exit;
        }
    }

#ifdef PROFILING_SUPPORTED
    if (CORProfilerPresent())
    {
        // #rejit
        //
        // Currently the rejit path is the only path which sets this.
        // If we get more reasons to set this then we may need to change
        // the failure reason message or disambiguate them.
        if (!m_allowInlining)
        {
            result = INLINE_FAIL;
            szFailReason = "ReJIT request disabled inlining from caller";
            goto exit;
        }

        // If the profiler has set a mask preventing inlining, always return
        // false to the jit.
        if (CORProfilerDisableInlining())
        {
            result = INLINE_FAIL;
            szFailReason = "Profiler disabled inlining globally";
            goto exit;
        }

#if defined(FEATURE_REJIT) && !defined(DACCESS_COMPILE)
        if (CORProfilerEnableRejit())
        {
            CodeVersionManager* pCodeVersionManager = pCallee->GetCodeVersionManager();
            CodeVersionManager::LockHolder codeVersioningLockHolder;
            ILCodeVersion ilVersion = pCodeVersionManager->GetActiveILCodeVersion(pCallee);
            if (ilVersion.GetRejitState() != ILCodeVersion::kStateActive || !ilVersion.HasDefaultIL())
            {
                result = INLINE_FAIL;
                szFailReason = "ReJIT methods cannot be inlined.";
                goto exit;
            }
        }
#endif // defined(FEATURE_REJIT) && !defined(DACCESS_COMPILE)

        // If the profiler wishes to be notified of JIT events and the result from
        // the above tests will cause a function to be inlined, we need to tell the
        // profiler that this inlining is going to take place, and give them a
        // chance to prevent it.
        {
            BEGIN_PROFILER_CALLBACK(CORProfilerTrackJITInfo());
            if (pCaller->IsILStub() || pCallee->IsILStub())
            {
                // do nothing
            }
            else
            {
                BOOL fShouldInline;
                HRESULT hr = (&g_profControlBlock)->JITInlining(
                    (FunctionID)pCaller,
                    (FunctionID)pCallee,
                    &fShouldInline);

                if (SUCCEEDED(hr) && !fShouldInline)
                {
                    result = INLINE_FAIL;
                    szFailReason = "Profiler disabled inlining locally";
                    goto exit;
                }
            }
            END_PROFILER_CALLBACK();
        }
    }
#endif // PROFILING_SUPPORTED

exit: ;

    EE_TO_JIT_TRANSITION();

    if (dontInline(result))
    {
        // If you hit this assert, it means you added a new way to prevent inlining
        // without documenting it for ETW!
        _ASSERTE(szFailReason != NULL);
        reportInliningDecision(hCaller, hCallee, result, szFailReason);
    }

    return result;
}

void CEEInfo::beginInlining(CORINFO_METHOD_HANDLE inlinerHnd,
                            CORINFO_METHOD_HANDLE inlineeHnd)
{
    // do nothing
}

void CEEInfo::reportInliningDecision (CORINFO_METHOD_HANDLE inlinerHnd,
                                      CORINFO_METHOD_HANDLE inlineeHnd,
                                      CorInfoInline inlineResult,
                                      const char * reason)
{
    STATIC_CONTRACT_THROWS;
    STATIC_CONTRACT_GC_TRIGGERS;

    JIT_TO_EE_TRANSITION();

#ifdef _DEBUG
    if (LoggingOn(LF_JIT, LL_INFO100000))
    {
        SString currentMethodName;
        currentMethodName.AppendUTF8(m_pMethodBeingCompiled->GetModule()->GetPEAssembly()->GetSimpleName());
        currentMethodName.Append(L'/');
        TypeString::AppendMethodInternal(currentMethodName, m_pMethodBeingCompiled, TypeString::FormatBasic);

        SString inlineeMethodName;
        if (GetMethod(inlineeHnd))
        {
            inlineeMethodName.AppendUTF8(GetMethod(inlineeHnd)->GetModule()->GetPEAssembly()->GetSimpleName());
            inlineeMethodName.Append(L'/');
            TypeString::AppendMethodInternal(inlineeMethodName, GetMethod(inlineeHnd), TypeString::FormatBasic);
        }
        else
        {
            inlineeMethodName.AppendASCII( "<null>" );
        }

        SString inlinerMethodName;
        if (GetMethod(inlinerHnd))
        {
            inlinerMethodName.AppendUTF8(GetMethod(inlinerHnd)->GetModule()->GetPEAssembly()->GetSimpleName());
            inlinerMethodName.Append(L'/');
            TypeString::AppendMethodInternal(inlinerMethodName, GetMethod(inlinerHnd), TypeString::FormatBasic);
        }
        else
        {
            inlinerMethodName.AppendASCII("<null>");
        }

        if (dontInline(inlineResult))
        {
            LOG((LF_JIT, LL_INFO100000,
                 "While compiling '%s', inline of '%s' into '%s' failed because: '%s'.\n",
                 currentMethodName.GetUTF8(), inlineeMethodName.GetUTF8(),
                 inlinerMethodName.GetUTF8(), reason));
        }
        else if(inlineResult == INLINE_PASS)
        {
            LOG((LF_JIT, LL_INFO100000, "While compiling '%s', inline of '%s' into '%s' succeeded.\n",
                 currentMethodName.GetUTF8(), inlineeMethodName.GetUTF8(),
                 inlinerMethodName.GetUTF8()));

        }
    }
#endif //_DEBUG

    //I'm gonna duplicate this code because the format is slightly different.  And LoggingOn is debug only.
    if (ETW_TRACING_CATEGORY_ENABLED(MICROSOFT_WINDOWS_DOTNETRUNTIME_PROVIDER_DOTNET_Context,
                                     TRACE_LEVEL_VERBOSE,
                                     CLR_JITTRACING_KEYWORD) &&
        (inlineResult <= INLINE_PASS)) // Only report pass, and failure inliner information. The various informative reports such as INLINE_CHECK_CAN_INLINE_SUCCESS are not to be reported via ETW
    {
        SString methodBeingCompiledNames[3];
        SString inlinerNames[3];
        SString inlineeNames[3];
        MethodDesc * methodBeingCompiled = m_pMethodBeingCompiled;
#define GMI(pMD, strArray) \
        do { \
            if (pMD) { \
                (pMD)->GetMethodInfo((strArray)[0], (strArray)[1], (strArray)[2]); \
            } else {  \
                (strArray)[0].Set(W("<null>")); \
                (strArray)[1].Set(W("<null>")); \
                (strArray)[2].Set(W("<null>")); \
            } } while (0)

        GMI(methodBeingCompiled, methodBeingCompiledNames);
        GMI(GetMethod(inlinerHnd), inlinerNames);
        GMI(GetMethod(inlineeHnd), inlineeNames);
#undef GMI
        if (dontInline(inlineResult))
        {
            const char * str = (reason ? reason : "");
            SString strReason;
            strReason.SetUTF8(str);


            FireEtwMethodJitInliningFailed(methodBeingCompiledNames[0].GetUnicode(),
                                           methodBeingCompiledNames[1].GetUnicode(),
                                           methodBeingCompiledNames[2].GetUnicode(),
                                           inlinerNames[0].GetUnicode(),
                                           inlinerNames[1].GetUnicode(),
                                           inlinerNames[2].GetUnicode(),
                                           inlineeNames[0].GetUnicode(),
                                           inlineeNames[1].GetUnicode(),
                                           inlineeNames[2].GetUnicode(),
                                           inlineResult == INLINE_NEVER,
                                           strReason.GetUnicode(),
                                           GetClrInstanceId());
        }
        else if(inlineResult == INLINE_PASS)
        {
            FireEtwMethodJitInliningSucceeded(methodBeingCompiledNames[0].GetUnicode(),
                                              methodBeingCompiledNames[1].GetUnicode(),
                                              methodBeingCompiledNames[2].GetUnicode(),
                                              inlinerNames[0].GetUnicode(),
                                              inlinerNames[1].GetUnicode(),
                                              inlinerNames[2].GetUnicode(),
                                              inlineeNames[0].GetUnicode(),
                                              inlineeNames[1].GetUnicode(),
                                              inlineeNames[2].GetUnicode(),
                                              GetClrInstanceId());
        }

    }


#if defined FEATURE_REJIT && !defined(DACCESS_COMPILE)
    if(inlineResult == INLINE_PASS)
    {
        // We don't want to track the chain of methods, so intentionally use m_pMethodBeingCompiled
        // to just track the methods that pCallee is eventually inlined in
        MethodDesc *pCallee = GetMethod(inlineeHnd);
        MethodDesc *pCaller = m_pMethodBeingCompiled;
        pCallee->GetModule()->AddInlining(pCaller, pCallee);

        if (CORProfilerEnableRejit())
        {
            ModuleID modId = 0;
            mdMethodDef methodDef = mdMethodDefNil;
            BOOL shouldCallReJIT = FALSE;

            {
                // If ReJIT is enabled, there is a chance that a race happened where the profiler
                // requested a ReJIT on a method, but before the ReJIT occurred an inlining happened.
                // If we end up reporting an inlining on a method with non-default IL it means the race
                // happened and we need to manually request ReJIT for it since it was missed.
                CodeVersionManager* pCodeVersionManager = pCallee->GetCodeVersionManager();
                CodeVersionManager::LockHolder codeVersioningLockHolder;
                ILCodeVersion ilVersion = pCodeVersionManager->GetActiveILCodeVersion(pCallee);
                if (ilVersion.GetRejitState() != ILCodeVersion::kStateActive || !ilVersion.HasDefaultIL())
                {
                    shouldCallReJIT = TRUE;
                    modId = reinterpret_cast<ModuleID>(pCaller->GetModule());
                    methodDef = pCaller->GetMemberDef();
                    // Do Not call RequestReJIT inside this scope, calling RequestReJIT while holding the CodeVersionManager lock
                    // will cause deadlocks with other threads calling RequestReJIT since it tries to obtain the CodeVersionManager lock
                }
            }

            if (shouldCallReJIT)
            {
                _ASSERTE(modId != 0);
                _ASSERTE(methodDef != mdMethodDefNil);
                ReJitManager::RequestReJIT(1, &modId, &methodDef, static_cast<COR_PRF_REJIT_FLAGS>(0));
            }
        }
    }
#endif // defined FEATURE_REJIT && !defined(DACCESS_COMPILE)

    EE_TO_JIT_TRANSITION();
}


/*************************************************************
 * Similar to above, but perform check for tail call
 * eligibility. The callee can be passed as NULL if not known
 * (calli and callvirt).
 *************************************************************/

bool CEEInfo::canTailCall (CORINFO_METHOD_HANDLE hCaller,
                           CORINFO_METHOD_HANDLE hDeclaredCallee,
                           CORINFO_METHOD_HANDLE hExactCallee,
                           bool fIsTailPrefix)
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    } CONTRACTL_END;

    bool result = false;
    const char * szFailReason = NULL;

    JIT_TO_EE_TRANSITION();

    // See comments in canInline above.

    MethodDesc* pCaller = GetMethod(hCaller);
    MethodDesc* pDeclaredCallee = GetMethod(hDeclaredCallee);
    MethodDesc* pExactCallee = GetMethod(hExactCallee);

    _ASSERTE(pCaller->GetModule());
    _ASSERTE(pCaller->GetModule()->GetClassLoader());

    _ASSERTE((pExactCallee == NULL) || pExactCallee->GetModule());
    _ASSERTE((pExactCallee == NULL) || pExactCallee->GetModule()->GetClassLoader());

    if (!fIsTailPrefix)
    {
        mdMethodDef callerToken = pCaller->GetMemberDef();

        // Do not tailcall from the application entrypoint.
        // We want Main to be visible in stack traces.
        if (callerToken == pCaller->GetModule()->GetEntryPointToken())
        {
            result = false;
            szFailReason = "Caller is the entry point";
            goto exit;
        }

        if (!pCaller->IsNoMetadata())
        {
            // Do not tailcall from methods that are marked as noinline (people often use no-inline
            // to mean "I want to always see this method in stacktrace")
            DWORD dwImplFlags = 0;
            IfFailThrow(pCaller->GetMDImport()->GetMethodImplProps(callerToken, NULL, &dwImplFlags));

            if (IsMiNoInlining(dwImplFlags))
            {
                result = false;
                szFailReason = "Caller is marked as no inline";
                goto exit;
            }
        }

        // Methods with StackCrawlMark depend on finding their caller on the stack.
        // If we tail call one of these guys, they get confused.  For lack of
        // a better way of identifying them, we use DynamicSecurity attribute to identify
        // them. We have an assert in canInline that ensures all StackCrawlMark
        // methods are appropriately marked.
        //
        if ((pExactCallee != NULL) && IsMdRequireSecObject(pExactCallee->GetAttrs()))
        {
            result = false;
            szFailReason = "Callee might have a StackCrawlMark.LookForMyCaller";
            goto exit;
        }
    }


    result = true;

exit: ;

    EE_TO_JIT_TRANSITION();

    if (!result)
    {
        // If you hit this assert, it means you added a new way to prevent tail calls
        // without documenting it for ETW!
        _ASSERTE(szFailReason != NULL);
        reportTailCallDecision(hCaller, hExactCallee, fIsTailPrefix, TAILCALL_FAIL, szFailReason);
    }

    return result;
}

void CEEInfo::reportTailCallDecision (CORINFO_METHOD_HANDLE callerHnd,
                                     CORINFO_METHOD_HANDLE calleeHnd,
                                     bool fIsTailPrefix,
                                     CorInfoTailCall tailCallResult,
                                     const char * reason)
{
    STATIC_CONTRACT_THROWS;
    STATIC_CONTRACT_GC_TRIGGERS;

    JIT_TO_EE_TRANSITION();

    //put code here.  Make sure to report the method being compiled in addition to inliner and inlinee.
#ifdef _DEBUG
    if (LoggingOn(LF_JIT, LL_INFO100000))
    {
        SString currentMethodName;
        TypeString::AppendMethodInternal(currentMethodName, m_pMethodBeingCompiled,
                                         TypeString::FormatBasic);

        SString calleeMethodName;
        if (GetMethod(calleeHnd))
        {
            TypeString::AppendMethodInternal(calleeMethodName, GetMethod(calleeHnd),
                                             TypeString::FormatBasic);
        }
        else
        {
            calleeMethodName.AppendASCII( "<null>" );
        }

        SString callerMethodName;
        if (GetMethod(callerHnd))
        {
            TypeString::AppendMethodInternal(callerMethodName, GetMethod(callerHnd),
                                             TypeString::FormatBasic);
        }
        else
        {
            callerMethodName.AppendASCII( "<null>" );
        }
        if (tailCallResult == TAILCALL_FAIL)
        {
            LOG((LF_JIT, LL_INFO100000,
                 "While compiling '%s', %splicit tail call from '%s' to '%s' failed because: '%s'.\n",
                 currentMethodName.GetUTF8(), fIsTailPrefix ? "ex" : "im",
                 callerMethodName.GetUTF8(), calleeMethodName.GetUTF8(), reason));
        }
        else
        {
            static const char * const tailCallType[] = {
                "optimized tail call", "recursive loop", "helper assisted tailcall"
            };
            _ASSERTE(tailCallResult >= 0 && (size_t)tailCallResult < ARRAY_SIZE(tailCallType));
            LOG((LF_JIT, LL_INFO100000,
                 "While compiling '%s', %splicit tail call from '%s' to '%s' generated as a %s.\n",
                 currentMethodName.GetUTF8(), fIsTailPrefix ? "ex" : "im",
                 callerMethodName.GetUTF8(), calleeMethodName.GetUTF8(), tailCallType[tailCallResult]));

        }
    }
#endif //_DEBUG

    // I'm gonna duplicate this code because the format is slightly different.  And LoggingOn is debug only.
    if (ETW_TRACING_CATEGORY_ENABLED(MICROSOFT_WINDOWS_DOTNETRUNTIME_PROVIDER_DOTNET_Context,
                                     TRACE_LEVEL_VERBOSE,
                                     CLR_JITTRACING_KEYWORD))
    {
        SString methodBeingCompiledNames[3];
        SString callerNames[3];
        SString calleeNames[3];
        MethodDesc * methodBeingCompiled = m_pMethodBeingCompiled;
#define GMI(pMD, strArray) \
        do { \
            if (pMD) { \
                (pMD)->GetMethodInfo((strArray)[0], (strArray)[1], (strArray)[2]); \
            } else {  \
                (strArray)[0].Set(W("<null>")); \
                (strArray)[1].Set(W("<null>")); \
                (strArray)[2].Set(W("<null>")); \
            } } while (0)

        GMI(methodBeingCompiled, methodBeingCompiledNames);
        GMI(GetMethod(callerHnd), callerNames);
        GMI(GetMethod(calleeHnd), calleeNames);
#undef GMI
        if (tailCallResult == TAILCALL_FAIL)
        {
            const char * str = (reason ? reason : "");
            SString strReason;
            strReason.SetUTF8(str);

            FireEtwMethodJitTailCallFailed(methodBeingCompiledNames[0].GetUnicode(),
                                           methodBeingCompiledNames[1].GetUnicode(),
                                           methodBeingCompiledNames[2].GetUnicode(),
                                           callerNames[0].GetUnicode(),
                                           callerNames[1].GetUnicode(),
                                           callerNames[2].GetUnicode(),
                                           calleeNames[0].GetUnicode(),
                                           calleeNames[1].GetUnicode(),
                                           calleeNames[2].GetUnicode(),
                                           fIsTailPrefix,
                                           strReason.GetUnicode(),
                                           GetClrInstanceId());
        }
        else
        {
            FireEtwMethodJitTailCallSucceeded(methodBeingCompiledNames[0].GetUnicode(),
                                              methodBeingCompiledNames[1].GetUnicode(),
                                              methodBeingCompiledNames[2].GetUnicode(),
                                              callerNames[0].GetUnicode(),
                                              callerNames[1].GetUnicode(),
                                              callerNames[2].GetUnicode(),
                                              calleeNames[0].GetUnicode(),
                                              calleeNames[1].GetUnicode(),
                                              calleeNames[2].GetUnicode(),
                                              fIsTailPrefix,
                                              tailCallResult,
                                              GetClrInstanceId());
        }

    }


    EE_TO_JIT_TRANSITION();
}

static void getEHinfoHelper(
    CORINFO_METHOD_HANDLE   ftnHnd,
    unsigned                EHnumber,
    CORINFO_EH_CLAUSE*      clause,
    COR_ILMETHOD_DECODER*   pILHeader)
{
    STANDARD_VM_CONTRACT;

    _ASSERTE(CheckPointer(pILHeader->EH));
    _ASSERTE(EHnumber < pILHeader->EH->EHCount());

    COR_ILMETHOD_SECT_EH_CLAUSE_FAT ehClause;
    const COR_ILMETHOD_SECT_EH_CLAUSE_FAT* ehInfo;
    ehInfo = (COR_ILMETHOD_SECT_EH_CLAUSE_FAT*)pILHeader->EH->EHClause(EHnumber, &ehClause);

    clause->Flags = (CORINFO_EH_CLAUSE_FLAGS)ehInfo->GetFlags();
    clause->TryOffset = ehInfo->GetTryOffset();
    clause->TryLength = ehInfo->GetTryLength();
    clause->HandlerOffset = ehInfo->GetHandlerOffset();
    clause->HandlerLength = ehInfo->GetHandlerLength();
    if ((clause->Flags & CORINFO_EH_CLAUSE_FILTER) == 0)
        clause->ClassToken = ehInfo->GetClassToken();
    else
        clause->FilterOffset = ehInfo->GetFilterOffset();
}

/*********************************************************************/
// get individual exception handler
void CEEInfo::getEHinfo(
            CORINFO_METHOD_HANDLE ftnHnd,
            unsigned      EHnumber,
            CORINFO_EH_CLAUSE* clause)
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    } CONTRACTL_END;

    JIT_TO_EE_TRANSITION();

    MethodDesc * ftn          = GetMethod(ftnHnd);

    if (IsDynamicMethodHandle(ftnHnd))
    {
        GetMethod(ftnHnd)->AsDynamicMethodDesc()->GetResolver()->GetEHInfo(EHnumber, clause);
    }
    else
    {
        COR_ILMETHOD_DECODER header(ftn->GetILHeader(TRUE), ftn->GetMDImport(), NULL);
        getEHinfoHelper(ftnHnd, EHnumber, clause, &header);
    }

    EE_TO_JIT_TRANSITION();
}

//---------------------------------------------------------------------------------------
//
void
CEEInfo::getMethodSig(
    CORINFO_METHOD_HANDLE ftnHnd,
    CORINFO_SIG_INFO *    sigRet,
    CORINFO_CLASS_HANDLE  owner)
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    } CONTRACTL_END;

    JIT_TO_EE_TRANSITION();

    getMethodSigInternal(ftnHnd, sigRet, owner, SK_NOT_CALLSITE);

    EE_TO_JIT_TRANSITION();
}

//---------------------------------------------------------------------------------------
//
//@GENERICSVER: for a method desc in a typical instantiation of a generic class,
// this will return the typical instantiation of the generic class,
// but only provided type variables are never shared.
// The JIT verifier relies on this behaviour to extract the typical class from an instantiated method's typical method handle.
//
CORINFO_CLASS_HANDLE
CEEInfo::getMethodClass(
    CORINFO_METHOD_HANDLE methodHnd)
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    } CONTRACTL_END;

    CORINFO_CLASS_HANDLE result = NULL;

    JIT_TO_EE_TRANSITION();

    MethodDesc* method = GetMethod(methodHnd);

    if (method->IsDynamicMethod())
    {
        DynamicResolver::SecurityControlFlags securityControlFlags = DynamicResolver::Default;
        TypeHandle typeOwner;

        DynamicResolver* pResolver = method->AsDynamicMethodDesc()->GetResolver();
        pResolver->GetJitContext(&securityControlFlags, &typeOwner);

        if (!typeOwner.IsNull() && (method == pResolver->GetDynamicMethod()))
        {
            result = CORINFO_CLASS_HANDLE(typeOwner.AsPtr());
        }
    }

    if (result == NULL)
    {
        TypeHandle th = TypeHandle(method->GetMethodTable());

        result = CORINFO_CLASS_HANDLE(th.AsPtr());
    }

    EE_TO_JIT_TRANSITION();

    return result;
}

/*********************************************************************/
bool CEEInfo::isIntrinsicType(CORINFO_CLASS_HANDLE classHnd)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_PREEMPTIVE;
    } CONTRACTL_END;

    bool result = false;
    JIT_TO_EE_TRANSITION_LEAF();

    TypeHandle VMClsHnd(classHnd);
    PTR_MethodTable methodTable = VMClsHnd.GetMethodTable();
    result = methodTable->IsIntrinsicType();

    EE_TO_JIT_TRANSITION_LEAF();
    return result;
}

/*********************************************************************/
void CEEInfo::getMethodVTableOffset (CORINFO_METHOD_HANDLE methodHnd,
                                     unsigned * pOffsetOfIndirection,
                                     unsigned * pOffsetAfterIndirection,
                                     bool * isRelative)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_PREEMPTIVE;
    } CONTRACTL_END;

    JIT_TO_EE_TRANSITION_LEAF();

    MethodDesc* method = GetMethod(methodHnd);

    //@GENERICS: shouldn't be doing this for instantiated methods as they live elsewhere
    _ASSERTE(!method->HasMethodInstantiation());

    _ASSERTE(MethodTable::GetVtableOffset() < 256);  // a rough sanity check

    // better be in the vtable
    _ASSERTE(method->GetSlot() < method->GetMethodTable()->GetNumVirtuals());

    *pOffsetOfIndirection = MethodTable::GetVtableOffset() + MethodTable::GetIndexOfVtableIndirection(method->GetSlot()) * TARGET_POINTER_SIZE /* sizeof(MethodTable::VTableIndir_t) */;
    *pOffsetAfterIndirection = MethodTable::GetIndexAfterVtableIndirection(method->GetSlot()) * TARGET_POINTER_SIZE /* sizeof(MethodTable::VTableIndir2_t) */;
    *isRelative = false;

    EE_TO_JIT_TRANSITION_LEAF();
}

/*********************************************************************/
bool CEEInfo::resolveVirtualMethodHelper(CORINFO_DEVIRTUALIZATION_INFO * info)
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    } CONTRACTL_END;

    // Initialize OUT fields
    info->devirtualizedMethod = NULL;
    info->requiresInstMethodTableArg = false;
    info->exactContext = NULL;
    memset(&info->resolvedTokenDevirtualizedMethod, 0, sizeof(info->resolvedTokenDevirtualizedMethod));
    memset(&info->resolvedTokenDevirtualizedUnboxedMethod, 0, sizeof(info->resolvedTokenDevirtualizedUnboxedMethod));

    MethodDesc* pBaseMD = GetMethod(info->virtualMethod);
    MethodTable* pBaseMT = pBaseMD->GetMethodTable();

    // Method better be from a fully loaded class
    _ASSERTE(pBaseMT->IsFullyLoaded());

    //@GENERICS: shouldn't be doing this for instantiated methods as they live elsewhere
    _ASSERTE(!pBaseMD->HasMethodInstantiation());

    // Method better be virtual
    _ASSERTE(pBaseMD->IsVirtual());

    MethodDesc* pDevirtMD = nullptr;

    TypeHandle ObjClassHnd(info->objClass);
    MethodTable* pObjMT = ObjClassHnd.GetMethodTable();
    _ASSERTE(pObjMT->IsFullyLoaded());

    // Can't devirtualize from __Canon.
    if (ObjClassHnd == TypeHandle(g_pCanonMethodTableClass))
    {
        info->detail = CORINFO_DEVIRTUALIZATION_FAILED_CANON;
        return false;
    }

    if (pBaseMT->IsInterface())
    {

#ifdef FEATURE_COMINTEROP
        // Don't try and devirtualize com interface calls.
        if (pObjMT->IsComObjectType())
        {
            info->detail = CORINFO_DEVIRTUALIZATION_FAILED_COM;
            return false;
        }
#endif // FEATURE_COMINTEROP

        if (info->context != nullptr)
        {
            pBaseMT = GetTypeFromContext(info->context).GetMethodTable();
        }

        // Interface call devirtualization.
        //
        // We must ensure that pObjMT actually implements the
        // interface corresponding to pBaseMD.
        if (!pObjMT->CanCastToInterface(pBaseMT))
        {
            info->detail = CORINFO_DEVIRTUALIZATION_FAILED_CAST;
            return false;
        }

        // For generic interface methods we must have context to
        // safely devirtualize.
        if (info->context != nullptr)
        {
            // If the derived class is a shared class, make sure the
            // owner class is too.
            if (pObjMT->IsSharedByGenericInstantiations())
            {
                MethodTable* pCanonBaseMT = pBaseMT->GetCanonicalMethodTable();

                // Check to see if the derived class implements multiple variants of a matching interface.
                // If so, we cannot predict exactly which implementation is in use here.
                MethodTable::InterfaceMapIterator it = pObjMT->IterateInterfaceMap();
                int canonicallyMatchingInterfacesFound = 0;
                while (it.Next())
                {
                    if (it.GetInterface(pObjMT)->GetCanonicalMethodTable() == pCanonBaseMT)
                    {
                        canonicallyMatchingInterfacesFound++;
                        if (canonicallyMatchingInterfacesFound > 1)
                        {
                            // Multiple canonically identical interfaces found when attempting to devirtualize an inexact interface dispatch
                            info->detail = CORINFO_DEVIRTUALIZATION_MULTIPLE_IMPL;
                            return false;
                        }
                    }
                }
            }

            pDevirtMD = pObjMT->GetMethodDescForInterfaceMethod(TypeHandle(pBaseMT), pBaseMD, FALSE /* throwOnConflict */);
        }
        else if (!pBaseMD->HasClassOrMethodInstantiation())
        {
            pDevirtMD = pObjMT->GetMethodDescForInterfaceMethod(pBaseMD, FALSE /* throwOnConflict */);
        }

        if (pDevirtMD == nullptr)
        {
            info->detail = CORINFO_DEVIRTUALIZATION_FAILED_LOOKUP;
            return false;
        }

        // If we devirtualized into a default interface method on a generic type, we should actually return an
        // instantiating stub but this is not happening.
        // Making this work is tracked by https://github.com/dotnet/runtime/issues/9588
        if (pDevirtMD->GetMethodTable()->IsInterface() && pDevirtMD->HasClassInstantiation())
        {
            info->detail = CORINFO_DEVIRTUALIZATION_FAILED_DIM;
            return false;
        }
    }
    else
    {
        // Virtual call devirtualization.
        //
        // The derived class should be a subclass of the base class.
        MethodTable* pCheckMT = pObjMT;

        while (pCheckMT != nullptr)
        {
            if (pCheckMT->HasSameTypeDefAs(pBaseMT))
            {
                break;
            }

            pCheckMT = pCheckMT->GetParentMethodTable();
        }

        if (pCheckMT == nullptr)
        {
            info->detail = CORINFO_DEVIRTUALIZATION_FAILED_SUBCLASS;
            return false;
        }

        // The base method should be in the base vtable
        WORD slot = pBaseMD->GetSlot();
        _ASSERTE(slot < pBaseMT->GetNumVirtuals());

        // Fetch the method that would be invoked if the class were
        // exactly derived class. It is up to the jit to determine whether
        // directly calling this method is correct.
        pDevirtMD = pObjMT->GetMethodDescForSlot(slot);

        // If the derived method's slot does not match the vtable slot,
        // bail on devirtualization, as the method was installed into
        // the vtable slot via an explicit override and even if the
        // method is final, the slot may not be.
        //
        // Note the jit could still safely devirtualize if it had an exact
        // class, but such cases are likely rare.
        WORD dslot = pDevirtMD->GetSlot();

        if (dslot != slot)
        {
            info->detail = CORINFO_DEVIRTUALIZATION_FAILED_SLOT;
            return false;
        }
    }

    // Determine the exact class.
    //
    // We may fail to get an exact context if the method is a default
    // interface method. If so, we'll use the method's class.
    //
    MethodTable* pApproxMT = pDevirtMD->GetMethodTable();
    MethodTable* pExactMT = pApproxMT;

    if (pApproxMT->IsInterface())
    {
        // As noted above, we can't yet handle generic interfaces
        // with default methods.
        _ASSERTE(!pDevirtMD->HasClassInstantiation());

    }
    else
    {
        pExactMT = pDevirtMD->GetExactDeclaringType(pObjMT);
    }

    // Success! Pass back the results.
    //
    info->devirtualizedMethod = (CORINFO_METHOD_HANDLE) pDevirtMD;
    info->exactContext = MAKE_CLASSCONTEXT((CORINFO_CLASS_HANDLE) pExactMT);
    info->requiresInstMethodTableArg = false;
    info->detail = CORINFO_DEVIRTUALIZATION_SUCCESS;

    return true;
}

bool CEEInfo::resolveVirtualMethod(CORINFO_DEVIRTUALIZATION_INFO * info)
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    } CONTRACTL_END;

    bool result = false;

    JIT_TO_EE_TRANSITION();

    result = resolveVirtualMethodHelper(info);

    EE_TO_JIT_TRANSITION();

    return result;
}

/*********************************************************************/
CORINFO_METHOD_HANDLE CEEInfo::getUnboxedEntry(
    CORINFO_METHOD_HANDLE ftn,
    bool* requiresInstMethodTableArg)
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    } CONTRACTL_END;

    CORINFO_METHOD_HANDLE result = NULL;

    JIT_TO_EE_TRANSITION();

    MethodDesc* pMD = GetMethod(ftn);
    bool requiresInstMTArg = false;

    if (pMD->IsUnboxingStub())
    {
        MethodTable* pMT = pMD->GetMethodTable();
        MethodDesc* pUnboxedMD = pMT->GetUnboxedEntryPointMD(pMD);

        result = (CORINFO_METHOD_HANDLE)pUnboxedMD;
        requiresInstMTArg = !!pUnboxedMD->RequiresInstMethodTableArg();
    }

    *requiresInstMethodTableArg = requiresInstMTArg;

    EE_TO_JIT_TRANSITION();

    return result;
}

/*********************************************************************/
void CEEInfo::expandRawHandleIntrinsic(
    CORINFO_RESOLVED_TOKEN *        pResolvedToken,
    CORINFO_GENERICHANDLE_RESULT *  pResult)
{
    LIMITED_METHOD_CONTRACT;
    UNREACHABLE();      // only called with NativeAOT.
}

/*********************************************************************/
CORINFO_CLASS_HANDLE CEEInfo::getDefaultComparerClass(CORINFO_CLASS_HANDLE elemType)
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    } CONTRACTL_END;

    CORINFO_CLASS_HANDLE result = NULL;

    JIT_TO_EE_TRANSITION();

    result = getDefaultComparerClassHelper(elemType);

    EE_TO_JIT_TRANSITION();

    return result;
}

CORINFO_CLASS_HANDLE CEEInfo::getDefaultComparerClassHelper(CORINFO_CLASS_HANDLE elemType)
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    } CONTRACTL_END;

    TypeHandle elemTypeHnd(elemType);

    // Mirrors the logic in BCL's CompareHelpers.CreateDefaultComparer
    // And in compile.cpp's SpecializeComparer
    //
    // We need to find the appropriate instantiation
    Instantiation inst(&elemTypeHnd, 1);

    // Nullable<T>
    if (Nullable::IsNullableType(elemTypeHnd))
    {
        Instantiation nullableInst = elemTypeHnd.AsMethodTable()->GetInstantiation();
        TypeHandle resultTh = ((TypeHandle)CoreLibBinder::GetClass(CLASS__NULLABLE_COMPARER)).Instantiate(nullableInst);
        return CORINFO_CLASS_HANDLE(resultTh.GetMethodTable());
    }

    // We need to special case the Enum comparers based on their underlying type to avoid boxing
    if (elemTypeHnd.IsEnum())
    {
        TypeHandle resultTh = ((TypeHandle)CoreLibBinder::GetClass(CLASS__ENUM_COMPARER)).Instantiate(inst);
        return CORINFO_CLASS_HANDLE(resultTh.GetMethodTable());
    }

    if (elemTypeHnd.IsCanonicalSubtype())
    {
        return NULL;
    }

    // If T implements IComparable<T>
    if (elemTypeHnd.CanCastTo(TypeHandle(CoreLibBinder::GetClass(CLASS__ICOMPARABLEGENERIC)).Instantiate(inst)))
    {
        TypeHandle resultTh = ((TypeHandle)CoreLibBinder::GetClass(CLASS__GENERIC_COMPARER)).Instantiate(inst);
        return CORINFO_CLASS_HANDLE(resultTh.GetMethodTable());
    }

    // Default case
    TypeHandle resultTh = ((TypeHandle)CoreLibBinder::GetClass(CLASS__OBJECT_COMPARER)).Instantiate(inst);

    return CORINFO_CLASS_HANDLE(resultTh.GetMethodTable());
}

/*********************************************************************/
CORINFO_CLASS_HANDLE CEEInfo::getDefaultEqualityComparerClass(CORINFO_CLASS_HANDLE elemType)
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    } CONTRACTL_END;

    CORINFO_CLASS_HANDLE result = NULL;

    JIT_TO_EE_TRANSITION();

    result = getDefaultEqualityComparerClassHelper(elemType);

    EE_TO_JIT_TRANSITION();

    return result;
}

CORINFO_CLASS_HANDLE CEEInfo::getDefaultEqualityComparerClassHelper(CORINFO_CLASS_HANDLE elemType)
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    } CONTRACTL_END;

    // Mirrors the logic in BCL's CompareHelpers.CreateDefaultEqualityComparer
    // And in compile.cpp's SpecializeEqualityComparer
    TypeHandle elemTypeHnd(elemType);

    // Mirrors the logic in BCL's CompareHelpers.CreateDefaultComparer
    // And in compile.cpp's SpecializeComparer
    //
    // We need to find the appropriate instantiation
    Instantiation inst(&elemTypeHnd, 1);

    // Nullable<T>
    if (Nullable::IsNullableType(elemTypeHnd))
    {
        Instantiation nullableInst = elemTypeHnd.AsMethodTable()->GetInstantiation();
        TypeHandle resultTh = ((TypeHandle)CoreLibBinder::GetClass(CLASS__NULLABLE_EQUALITYCOMPARER)).Instantiate(nullableInst);
        return CORINFO_CLASS_HANDLE(resultTh.GetMethodTable());
    }

    // Enum
    //
    // We need to special case the Enum comparers based on their underlying type,
    // to avoid boxing and call the correct versions of GetHashCode.
    if (elemTypeHnd.IsEnum())
    {
        TypeHandle resultTh = ((TypeHandle)CoreLibBinder::GetClass(CLASS__ENUM_EQUALITYCOMPARER)).Instantiate(inst);
        return CORINFO_CLASS_HANDLE(resultTh.GetMethodTable());
    }

    if (elemTypeHnd.IsCanonicalSubtype())
    {
        return NULL;
    }

    // If T implements IEquatable<T>
    if (elemTypeHnd.CanCastTo(TypeHandle(CoreLibBinder::GetClass(CLASS__IEQUATABLEGENERIC)).Instantiate(inst)))
    {
        TypeHandle resultTh = ((TypeHandle)CoreLibBinder::GetClass(CLASS__GENERIC_EQUALITYCOMPARER)).Instantiate(inst);
        return CORINFO_CLASS_HANDLE(resultTh.GetMethodTable());
    }

    // Default case
    TypeHandle resultTh = ((TypeHandle)CoreLibBinder::GetClass(CLASS__OBJECT_EQUALITYCOMPARER)).Instantiate(inst);

    return CORINFO_CLASS_HANDLE(resultTh.GetMethodTable());
}

/*********************************************************************/
void CEEInfo::getFunctionEntryPoint(CORINFO_METHOD_HANDLE  ftnHnd,
                                    CORINFO_CONST_LOOKUP * pResult,
                                    CORINFO_ACCESS_FLAGS   accessFlags)
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    } CONTRACTL_END;

    void* ret = NULL;
    InfoAccessType accessType = IAT_VALUE;

    JIT_TO_EE_TRANSITION();

    MethodDesc * ftn = GetMethod(ftnHnd);
#if defined(FEATURE_GDBJIT)
    MethodDesc * orig_ftn = ftn;
#endif

    // Resolve methodImpl.
    ftn = ftn->GetMethodTable()->MapMethodDeclToMethodImpl(ftn);

    if (!ftn->IsFCall() && ftn->IsVersionableWithPrecode() && (ftn->GetPrecodeType() == PRECODE_FIXUP) && !ftn->IsPointingToStableNativeCode())
    {
        ret = ((FixupPrecode*)ftn->GetOrCreatePrecode())->GetTargetSlot();
        accessType = IAT_PVALUE;
    }
    else
    {
        ret = (void *)ftn->TryGetMultiCallableAddrOfCode(accessFlags);

        // TryGetMultiCallableAddrOfCode returns NULL if indirect access is desired
        if (ret == NULL)
        {
            // should never get here for EnC methods or if interception via remoting stub is required
            _ASSERTE(!ftn->InEnCEnabledModule());

            ret = (void *)ftn->GetAddrOfSlot();

            accessType = IAT_PVALUE;
        }
    }

#if defined(FEATURE_GDBJIT)
    CalledMethod * pCM = new CalledMethod(orig_ftn, ret, m_pCalledMethods);
    m_pCalledMethods = pCM;
#endif

    EE_TO_JIT_TRANSITION();

    _ASSERTE(ret != NULL);

    pResult->accessType = accessType;
    pResult->addr = ret;
}

/*********************************************************************/
void CEEInfo::getFunctionFixedEntryPoint(CORINFO_METHOD_HANDLE   ftn,
                                         bool isUnsafeFunctionPointer,
                                         CORINFO_CONST_LOOKUP *  pResult)
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    } CONTRACTL_END;

    JIT_TO_EE_TRANSITION();

    MethodDesc * pMD = GetMethod(ftn);

    if (isUnsafeFunctionPointer)
        pMD->PrepareForUseAsAFunctionPointer();

    pResult->accessType = IAT_VALUE;
    pResult->addr = (void*)pMD->GetMultiCallableAddrOfCode();

    EE_TO_JIT_TRANSITION();
}

/*********************************************************************/
size_t CEEInfo::printFieldName(CORINFO_FIELD_HANDLE fieldHnd, char* buffer, size_t bufferSize, size_t* pRequiredBufferSize)
{
    CONTRACTL {
        THROWS;
        GC_NOTRIGGER;
        MODE_PREEMPTIVE;
    } CONTRACTL_END;

    size_t bytesWritten = 0;
    JIT_TO_EE_TRANSITION();

    FieldDesc* field = (FieldDesc*) fieldHnd;
    const char* fieldName = field->GetName();

    size_t len = strlen(fieldName);
    if (bufferSize > 0)
    {
        bytesWritten = min(len, bufferSize - 1);
        memcpy(buffer, fieldName, len);
        buffer[bytesWritten] = '\0';
    }

    if (pRequiredBufferSize != NULL)
    {
        *pRequiredBufferSize = len + 1;
    }

    EE_TO_JIT_TRANSITION();

    return bytesWritten;
}

/*********************************************************************/
// Get the type that declares the field
CORINFO_CLASS_HANDLE CEEInfo::getFieldClass (CORINFO_FIELD_HANDLE fieldHnd)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_PREEMPTIVE;
    } CONTRACTL_END;

    CORINFO_CLASS_HANDLE result = NULL;

    JIT_TO_EE_TRANSITION_LEAF();

    FieldDesc* field = (FieldDesc*) fieldHnd;
    result = CORINFO_CLASS_HANDLE(field->GetApproxEnclosingMethodTable());

    EE_TO_JIT_TRANSITION_LEAF();

    return result;
}

/*********************************************************************/
// Returns the basic type of the field (not the type that declares the field)
//
// pTypeHnd - Optional. If not null then on return, for reference and value types,
//            *pTypeHnd will contain the normalized type of the field.
// owner - Optional. For resolving in a generic context

CorInfoType CEEInfo::getFieldType (CORINFO_FIELD_HANDLE fieldHnd,
                                   CORINFO_CLASS_HANDLE* pTypeHnd,
                                   CORINFO_CLASS_HANDLE owner)
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    } CONTRACTL_END;

    CorInfoType result = CORINFO_TYPE_UNDEF;

    JIT_TO_EE_TRANSITION();

    result = getFieldTypeInternal(fieldHnd, pTypeHnd, owner);

    EE_TO_JIT_TRANSITION();

    return result;
}

/*********************************************************************/
CorInfoType CEEInfo::getFieldTypeInternal (CORINFO_FIELD_HANDLE fieldHnd,
                                           CORINFO_CLASS_HANDLE* pTypeHnd,
                                           CORINFO_CLASS_HANDLE owner)
{
    STANDARD_VM_CONTRACT;

    if (pTypeHnd != nullptr)
    {
        *pTypeHnd = 0;
    }

    TypeHandle clsHnd = TypeHandle();
    FieldDesc* field = (FieldDesc*) fieldHnd;
    CorElementType type   = field->GetFieldType();
    if (!CorTypeInfo::IsPrimitiveType(type))
    {
        PCCOR_SIGNATURE sig;
        DWORD sigCount;
        CorCallingConvention conv;

        field->GetSig(&sig, &sigCount);

        conv = (CorCallingConvention)CorSigUncompressCallingConv(sig);
        _ASSERTE(isCallConv(conv, IMAGE_CEE_CS_CALLCONV_FIELD));

        SigPointer ptr(sig, sigCount);

        // For verifying code involving generics, use the class instantiation
        // of the optional owner (to provide exact, not representative,
        // type information)
        SigTypeContext typeContext(field, (TypeHandle)owner);

        clsHnd = ptr.GetTypeHandleThrowing(field->GetModule(), &typeContext);
        _ASSERTE(!clsHnd.IsNull());

        // I believe it doesn't make any diff. if this is GetInternalCorElementType
        // or GetSignatureCorElementType.
        type = clsHnd.GetSignatureCorElementType();
    }

    return CEEInfo::asCorInfoType(type, clsHnd, pTypeHnd);
}

/*********************************************************************/
unsigned CEEInfo::getFieldOffset (CORINFO_FIELD_HANDLE fieldHnd)
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    } CONTRACTL_END;

    unsigned result = (unsigned) -1;

    JIT_TO_EE_TRANSITION();

    FieldDesc* field = (FieldDesc*) fieldHnd;

    // GetOffset() does not include the size of Object
    result = field->GetOffset();

    // So if it is not a value class, add the Object into it
    if (field->IsStatic())
    {
        Module* pModule = field->GetModule();
        if (field->IsRVA() && pModule->IsRvaFieldTls(field->GetOffset()))
        {
            result = pModule->GetFieldTlsOffset(field->GetOffset());
        }
    }
    else if (!field->GetApproxEnclosingMethodTable()->IsValueType())
    {
        result += OBJECT_SIZE;
    }

    EE_TO_JIT_TRANSITION();

    return result;
}

/*********************************************************************/
uint32_t CEEInfo::getFieldThreadLocalStoreID(CORINFO_FIELD_HANDLE fieldHnd, void **ppIndirection)
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    } CONTRACTL_END;

    uint32_t result = 0;

    if (ppIndirection != NULL)
        *ppIndirection = NULL;

    JIT_TO_EE_TRANSITION();

    FieldDesc* field = (FieldDesc*) fieldHnd;
    Module* module = field->GetModule();

    _ASSERTE(field->IsRVA());       // Only RVA statics can be thread local
    _ASSERTE(module->IsRvaFieldTls(field->GetOffset()));

    result = module->GetTlsIndex();

    EE_TO_JIT_TRANSITION();

    return result;
}

void *CEEInfo::allocateArray(size_t cBytes)
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    } CONTRACTL_END;

    void * result = NULL;

    JIT_TO_EE_TRANSITION();

    result = new BYTE [cBytes];

    EE_TO_JIT_TRANSITION();

    return result;
}

void CEEInfo::freeArrayInternal(void* array)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_PREEMPTIVE;
    } CONTRACTL_END;

    delete [] ((BYTE*) array);
}

void CEEInfo::freeArray(void *array)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_PREEMPTIVE;
    } CONTRACTL_END;

    JIT_TO_EE_TRANSITION();

    freeArrayInternal(array);

    EE_TO_JIT_TRANSITION();
}

void CEEInfo::getBoundaries(CORINFO_METHOD_HANDLE ftn,
                               unsigned int *cILOffsets, uint32_t **pILOffsets,
                               ICorDebugInfo::BoundaryTypes *implicitBoundaries)
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    } CONTRACTL_END;

    JIT_TO_EE_TRANSITION();

#ifdef DEBUGGING_SUPPORTED
    if (g_pDebugInterface)
    {
        g_pDebugInterface->getBoundaries(GetMethod(ftn), cILOffsets, (DWORD**)pILOffsets,
                                     implicitBoundaries);
    }
    else
    {
        *cILOffsets = 0;
        *pILOffsets = NULL;
        *implicitBoundaries = ICorDebugInfo::DEFAULT_BOUNDARIES;
    }
#endif // DEBUGGING_SUPPORTED

    EE_TO_JIT_TRANSITION();
}

void CEEInfo::getVars(CORINFO_METHOD_HANDLE ftn, ULONG32 *cVars, ICorDebugInfo::ILVarInfo **vars,
                         bool *extendOthers)
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    } CONTRACTL_END;

    JIT_TO_EE_TRANSITION();

#ifdef DEBUGGING_SUPPORTED
    if (g_pDebugInterface)
    {
        g_pDebugInterface->getVars(GetMethod(ftn), cVars, vars, extendOthers);
    }
    else
    {
        *cVars = 0;
        *vars = NULL;

        // Just tell the JIT to extend everything.
        *extendOthers = true;
    }
#endif // DEBUGGING_SUPPORTED

    EE_TO_JIT_TRANSITION();
}

/*********************************************************************/
CORINFO_ARG_LIST_HANDLE CEEInfo::getArgNext(CORINFO_ARG_LIST_HANDLE args)
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    } CONTRACTL_END;

    CORINFO_ARG_LIST_HANDLE result = NULL;

    JIT_TO_EE_TRANSITION();

    SigPointer ptr((unsigned __int8*) args);
    IfFailThrow(ptr.SkipExactlyOne());

    result = (CORINFO_ARG_LIST_HANDLE) ptr.GetPtr();

    EE_TO_JIT_TRANSITION();

    return result;
}


/*********************************************************************/

CorInfoTypeWithMod CEEInfo::getArgType (
        CORINFO_SIG_INFO*       sig,
        CORINFO_ARG_LIST_HANDLE args,
        CORINFO_CLASS_HANDLE*   vcTypeRet
        )
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    } CONTRACTL_END;

    CorInfoTypeWithMod result = CorInfoTypeWithMod(CORINFO_TYPE_UNDEF);

    JIT_TO_EE_TRANSITION();

   _ASSERTE((BYTE*) sig->pSig <= (BYTE*) sig->args && (BYTE*) args < (BYTE*) sig->pSig + sig->cbSig);
   _ASSERTE((BYTE*) sig->args <= (BYTE*) args);
    INDEBUG(*vcTypeRet = CORINFO_CLASS_HANDLE((size_t)INVALID_POINTER_CC));

    SigPointer ptr((unsigned __int8*) args);
    CorElementType eType;
    IfFailThrow(ptr.PeekElemType(&eType));
    while (eType == ELEMENT_TYPE_PINNED)
    {
        result = CORINFO_TYPE_MOD_PINNED;
        IfFailThrow(ptr.GetElemType(NULL));
        IfFailThrow(ptr.PeekElemType(&eType));
    }

    // Now read off the "real" element type after taking any instantiations into consideration
    SigTypeContext typeContext;
    GetTypeContext(&sig->sigInst,&typeContext);

    Module* pModule = GetModule(sig->scope);

    CorElementType type = ptr.PeekElemTypeClosed(pModule, &typeContext);

    TypeHandle typeHnd = TypeHandle();
    switch (type) {
      case ELEMENT_TYPE_VAR :
      case ELEMENT_TYPE_MVAR :
      case ELEMENT_TYPE_VALUETYPE :
      case ELEMENT_TYPE_TYPEDBYREF :
      case ELEMENT_TYPE_INTERNAL :
      {
            typeHnd = ptr.GetTypeHandleThrowing(pModule, &typeContext);
            _ASSERTE(!typeHnd.IsNull());

            CorElementType normType = typeHnd.GetInternalCorElementType();

            // if we are looking up a value class, don't morph it to a refernece type
            // (This can only happen in illegal IL)
            if (!CorTypeInfo::IsObjRef(normType) || type != ELEMENT_TYPE_VALUETYPE)
            {
                type = normType;
            }
        }
        break;

    case ELEMENT_TYPE_PTR:
        // Load the type eagerly under debugger to make the eval work
        if (CORDisableJITOptimizations(pModule->GetDebuggerInfoBits()))
        {
            // NOTE: in some IJW cases, when the type pointed at is unmanaged,
            // the GetTypeHandle may fail, because there is no TypeDef for such type.
            // Usage of GetTypeHandleThrowing would lead to class load exception
            TypeHandle thPtr = ptr.GetTypeHandleNT(pModule, &typeContext);
            if(!thPtr.IsNull())
            {
                classMustBeLoadedBeforeCodeIsRun(CORINFO_CLASS_HANDLE(thPtr.AsPtr()));
            }
        }
        break;

    case ELEMENT_TYPE_VOID:
        // void is not valid in local sigs
        if (sig->flags & CORINFO_SIGFLAG_IS_LOCAL_SIG)
            COMPlusThrowHR(COR_E_INVALIDPROGRAM);
        break;

    case ELEMENT_TYPE_END:
           COMPlusThrowHR(COR_E_BADIMAGEFORMAT);
        break;

    default:
        break;
    }

    result = CorInfoTypeWithMod(result | CEEInfo::asCorInfoType(type, typeHnd, vcTypeRet));
    EE_TO_JIT_TRANSITION();

    return result;
}

// Now the implementation is only focused on the float fields info,
// while a struct-arg has no more than two fields and total size is no larger than two-pointer-size.
// These depends on the platform's ABI rules.
//
// The returned value's encoding details how a struct argument uses float registers:
// see the enum `StructFloatFieldInfoFlags`.
uint32_t CEEInfo::getLoongArch64PassStructInRegisterFlags(CORINFO_CLASS_HANDLE cls)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_PREEMPTIVE;
    } CONTRACTL_END;

    JIT_TO_EE_TRANSITION_LEAF();

    uint32_t size = STRUCT_NO_FLOAT_FIELD;

#if defined(TARGET_LOONGARCH64)
    size = (uint32_t)MethodTable::GetLoongArch64PassStructInRegisterFlags(cls);
#endif

    EE_TO_JIT_TRANSITION_LEAF();

    return size;
}

uint32_t CEEInfo::getRISCV64PassStructInRegisterFlags(CORINFO_CLASS_HANDLE cls)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_PREEMPTIVE;
    } CONTRACTL_END;

    JIT_TO_EE_TRANSITION_LEAF();

    uint32_t size = STRUCT_NO_FLOAT_FIELD;

#if defined(TARGET_RISCV64)
    size = (uint32_t)MethodTable::GetRiscV64PassStructInRegisterFlags(cls);
#endif // TARGET_RISCV64

    EE_TO_JIT_TRANSITION_LEAF();

    return size;
}

/*********************************************************************/

int CEEInfo::getExactClasses (
        CORINFO_CLASS_HANDLE  baseType,
        int                   maxExactClasses,
        CORINFO_CLASS_HANDLE* exactClsRet
        )
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    } CONTRACTL_END;

    int exactClassesCount = 0;

    JIT_TO_EE_TRANSITION();

    // This function is currently implemented only on NativeAOT
    // but can be implemented for CoreCLR as well (e.g. for internal types)

    EE_TO_JIT_TRANSITION();

    return exactClassesCount;
}

/*********************************************************************/

CORINFO_CLASS_HANDLE CEEInfo::getArgClass (
    CORINFO_SIG_INFO*       sig,
    CORINFO_ARG_LIST_HANDLE args
    )
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    } CONTRACTL_END;

    CORINFO_CLASS_HANDLE result = NULL;

    JIT_TO_EE_TRANSITION();

    // make certain we dont have a completely wacked out sig pointer
    _ASSERTE((BYTE*) sig->pSig <= (BYTE*) sig->args);
    _ASSERTE((BYTE*) sig->args <= (BYTE*) args && (BYTE*) args < &((BYTE*) sig->args)[0x10000*5]);

    Module* pModule = GetModule(sig->scope);

    SigPointer ptr((unsigned __int8*) args);

    CorElementType eType;
    IfFailThrow(ptr.PeekElemType(&eType));

    while (eType == ELEMENT_TYPE_PINNED)
    {
        IfFailThrow(ptr.GetElemType(NULL));
        IfFailThrow(ptr.PeekElemType(&eType));
    }
    // Now read off the "real" element type after taking any instantiations into consideration
    SigTypeContext typeContext;
    GetTypeContext(&sig->sigInst, &typeContext);
    CorElementType type = ptr.PeekElemTypeClosed(pModule, &typeContext);

    if (!CorTypeInfo::IsPrimitiveType(type)) {
        TypeHandle th = ptr.GetTypeHandleThrowing(pModule, &typeContext);
        result = CORINFO_CLASS_HANDLE(th.AsPtr());
    }

    EE_TO_JIT_TRANSITION();

    return result;
}

/*********************************************************************/

CorInfoHFAElemType CEEInfo::getHFAType(CORINFO_CLASS_HANDLE hClass)
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    } CONTRACTL_END;

    CorInfoHFAElemType result = CORINFO_HFA_ELEM_NONE;

    JIT_TO_EE_TRANSITION();

    TypeHandle VMClsHnd(hClass);

    result = VMClsHnd.GetHFAType();

    EE_TO_JIT_TRANSITION();

    return result;
}

namespace
{
    CorInfoCallConvExtension getUnmanagedCallConvForSig(CORINFO_MODULE_HANDLE mod, PCCOR_SIGNATURE pSig, DWORD cbSig, bool* pSuppressGCTransition)
    {
        STANDARD_VM_CONTRACT;

        SigParser parser(pSig, cbSig);
        uint32_t rawCallConv;
        if (FAILED(parser.GetCallingConv(&rawCallConv)))
        {
            COMPlusThrowHR(COR_E_BADIMAGEFORMAT);
        }
        switch ((CorCallingConvention)rawCallConv)
        {
        case IMAGE_CEE_CS_CALLCONV_DEFAULT:
            _ASSERTE_MSG(false, "bad callconv");
            return CorInfoCallConvExtension::Managed;
        case IMAGE_CEE_CS_CALLCONV_C:
            return CorInfoCallConvExtension::C;
        case IMAGE_CEE_CS_CALLCONV_STDCALL:
            return CorInfoCallConvExtension::Stdcall;
        case IMAGE_CEE_CS_CALLCONV_THISCALL:
            return CorInfoCallConvExtension::Thiscall;
        case IMAGE_CEE_CS_CALLCONV_FASTCALL:
            return CorInfoCallConvExtension::Fastcall;
        case IMAGE_CEE_CS_CALLCONV_UNMANAGED:
        {
            CallConvBuilder builder;
            UINT errorResID;
            HRESULT hr = CallConv::TryGetUnmanagedCallingConventionFromModOpt(mod, pSig, cbSig, &builder, &errorResID);

            if (FAILED(hr))
                COMPlusThrowHR(hr, errorResID);

            CorInfoCallConvExtension callConvLocal = builder.GetCurrentCallConv();
            if (callConvLocal == CallConvBuilder::UnsetValue)
            {
                callConvLocal = CallConv::GetDefaultUnmanagedCallingConvention();
            }

            *pSuppressGCTransition = builder.IsCurrentCallConvModSet(CallConvBuilder::CALL_CONV_MOD_SUPPRESSGCTRANSITION);
            return callConvLocal;
        }
        case IMAGE_CEE_CS_CALLCONV_NATIVEVARARG:
            return CorInfoCallConvExtension::C;
        default:
            _ASSERTE_MSG(false, "bad callconv");
            return CorInfoCallConvExtension::Managed;
        }
    }

    CorInfoCallConvExtension getUnmanagedCallConvForMethod(MethodDesc* pMD, bool* pSuppressGCTransition)
    {
        STANDARD_VM_CONTRACT;

        uint32_t methodCallConv;
        PCCOR_SIGNATURE pSig;
        DWORD cbSig;
        pMD->GetSig(&pSig, &cbSig);
        if (FAILED(SigParser(pSig, cbSig).GetCallingConv(&methodCallConv)))
        {
            COMPlusThrowHR(COR_E_BADIMAGEFORMAT);
        }

        if (methodCallConv == CORINFO_CALLCONV_DEFAULT || methodCallConv == CORINFO_CALLCONV_VARARG)
        {
            _ASSERTE(pMD->IsNDirect() || pMD->HasUnmanagedCallersOnlyAttribute());
            if (pMD->IsNDirect())
            {
                CorInfoCallConvExtension unmanagedCallConv;
                NDirect::GetCallingConvention_IgnoreErrors(pMD, &unmanagedCallConv, pSuppressGCTransition);
                return unmanagedCallConv;
            }
            else
            {
                CorInfoCallConvExtension unmanagedCallConv;
                if (CallConv::TryGetCallingConventionFromUnmanagedCallersOnly(pMD, &unmanagedCallConv))
                {
                    if (methodCallConv == IMAGE_CEE_CS_CALLCONV_VARARG)
                    {
                        return CorInfoCallConvExtension::C;
                    }
                    return unmanagedCallConv;
                }
                return CallConv::GetDefaultUnmanagedCallingConvention();
            }
        }
        else
        {
            return getUnmanagedCallConvForSig(GetScopeHandle(pMD->GetModule()), pSig, cbSig, pSuppressGCTransition);
        }
    }
}

/*********************************************************************/

    // return the entry point calling convention for any of the following
    // - a P/Invoke
    // - a method marked with UnmanagedCallersOnly
    // - a function pointer with the CORINFO_CALLCONV_UNMANAGED calling convention.
CorInfoCallConvExtension CEEInfo::getUnmanagedCallConv(CORINFO_METHOD_HANDLE method, CORINFO_SIG_INFO* callSiteSig, bool* pSuppressGCTransition)
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    } CONTRACTL_END;

    CorInfoCallConvExtension callConv = CorInfoCallConvExtension::Managed;

    JIT_TO_EE_TRANSITION();

    if (pSuppressGCTransition)
    {
        *pSuppressGCTransition = false;
    }

    if (method)
    {
        callConv = getUnmanagedCallConvForMethod(GetMethod(method), pSuppressGCTransition);
    }
    else
    {
        _ASSERTE(callSiteSig != nullptr);
        callConv = getUnmanagedCallConvForSig(callSiteSig->scope, callSiteSig->pSig, callSiteSig->cbSig, pSuppressGCTransition);
    }

    EE_TO_JIT_TRANSITION();

    return callConv;
}

/*********************************************************************/
bool CEEInfo::pInvokeMarshalingRequired(CORINFO_METHOD_HANDLE method, CORINFO_SIG_INFO* callSiteSig)
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    } CONTRACTL_END;

    bool result = false;

    JIT_TO_EE_TRANSITION();

    if (method == NULL)
    {
        // check the call site signature
        result = NDirect::MarshalingRequired(
                    NULL,
                    callSiteSig->pSig,
                    GetModule(callSiteSig->scope));
    }
    else
    {
        MethodDesc* ftn = GetMethod(method);
        _ASSERTE(ftn->IsNDirect());
        NDirectMethodDesc *pMD = (NDirectMethodDesc*)ftn;

#if defined(HAS_NDIRECT_IMPORT_PRECODE)
        if (pMD->IsVarArg())
        {
            // Varag P/Invoke must not be inlined because its NDirectMethodDesc
            // does not contain a meaningful stack size (it is call site specific).
            // See code:InlinedCallFrame.UpdateRegDisplay where this is needed.
            result = TRUE;
        }
        else if (pMD->MarshalingRequired())
        {
            // This is not a no-marshal signature.
            result = TRUE;
        }
        else
        {
            // This is a no-marshal non-vararg signature.
            result = FALSE;
        }
#else
        // Marshalling is required to lazy initialize the indirection cell
        // without NDirectImportPrecode.
        result = TRUE;
#endif
    }

    PrepareCodeConfig *config = GetThread()->GetCurrentPrepareCodeConfig();
    if (config != nullptr && config->IsForMulticoreJit())
    {
        bool suppressGCTransition = false;
        CorInfoCallConvExtension unmanagedCallConv = getUnmanagedCallConv(method, callSiteSig, &suppressGCTransition);

        if (suppressGCTransition)
        {
            // MultiCoreJit thread can't inline PInvoke with SuppressGCTransitionAttribute,
            // because it can't be resolved in mcj thread
            result = TRUE;
        }
    }

    EE_TO_JIT_TRANSITION();

    return result;
}

/*********************************************************************/
// Generate a cookie based on the signature that would needs to be passed
// to CORINFO_HELP_PINVOKE_CALLI
LPVOID CEEInfo::GetCookieForPInvokeCalliSig(CORINFO_SIG_INFO* szMetaSig,
                                            void **ppIndirection)
{
    WRAPPER_NO_CONTRACT;

    return getVarArgsHandle(szMetaSig, ppIndirection);
}

bool CEEInfo::canGetCookieForPInvokeCalliSig(CORINFO_SIG_INFO* szMetaSig)
{
    LIMITED_METHOD_CONTRACT;
    return true;
}


// Check any constraints on method type arguments
bool CEEInfo::satisfiesMethodConstraints(
    CORINFO_CLASS_HANDLE        parent,
    CORINFO_METHOD_HANDLE       method)
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    } CONTRACTL_END;

    bool result = false;

    JIT_TO_EE_TRANSITION();

    _ASSERTE(parent != NULL);
    _ASSERTE(method != NULL);
    result = !!GetMethod(method)->SatisfiesMethodConstraints(TypeHandle(parent));

    EE_TO_JIT_TRANSITION();

    return result;
}


/*********************************************************************/
// return address of fixup area for late-bound N/Direct calls.
void CEEInfo::getAddressOfPInvokeTarget(CORINFO_METHOD_HANDLE method,
                                        CORINFO_CONST_LOOKUP *pLookup)
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    } CONTRACTL_END;

    JIT_TO_EE_TRANSITION();

    MethodDesc* pMD = GetMethod(method);
    _ASSERTE(pMD->IsNDirect());

    NDirectMethodDesc* pNMD = reinterpret_cast<NDirectMethodDesc*>(pMD);

    void* pIndirection;
    if (NDirectMethodDesc::TryGetResolvedNDirectTarget(pNMD, &pIndirection))
    {
        pLookup->accessType = IAT_VALUE;
        pLookup->addr = pIndirection;
    }
    else
    {
        pLookup->accessType = IAT_PVALUE;
        pLookup->addr = (LPVOID)&(pNMD->ndirect.m_pNDirectTarget);
    }

    EE_TO_JIT_TRANSITION();
}

/*********************************************************************/
CORINFO_JUST_MY_CODE_HANDLE CEEInfo::getJustMyCodeHandle(
                CORINFO_METHOD_HANDLE       method,
                CORINFO_JUST_MY_CODE_HANDLE**ppIndirection)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_PREEMPTIVE;
    } CONTRACTL_END;

    CORINFO_JUST_MY_CODE_HANDLE result = NULL;

    if (ppIndirection)
        *ppIndirection = NULL;

    JIT_TO_EE_TRANSITION_LEAF();

    // Get the flag from the debugger.
    MethodDesc* ftn = GetMethod(method);
    DWORD * pFlagAddr = NULL;

    if (g_pDebugInterface)
    {
        pFlagAddr = g_pDebugInterface->GetJMCFlagAddr(ftn->GetModule());
    }

    result = (CORINFO_JUST_MY_CODE_HANDLE) pFlagAddr;

    EE_TO_JIT_TRANSITION_LEAF();

    return result;
}

/*********************************************************************/
void InlinedCallFrame::GetEEInfo(CORINFO_EE_INFO::InlinedCallFrameInfo *pInfo)
{
    LIMITED_METHOD_CONTRACT;

    pInfo->size                          = sizeof(GSCookie) + sizeof(InlinedCallFrame);

    pInfo->offsetOfGSCookie              = 0;
    pInfo->offsetOfFrameVptr             = sizeof(GSCookie);
    pInfo->offsetOfFrameLink             = sizeof(GSCookie) + Frame::GetOffsetOfNextLink();
    pInfo->offsetOfCallSiteSP            = sizeof(GSCookie) + offsetof(InlinedCallFrame, m_pCallSiteSP);
    pInfo->offsetOfCalleeSavedFP         = sizeof(GSCookie) + offsetof(InlinedCallFrame, m_pCalleeSavedFP);
    pInfo->offsetOfCallTarget            = sizeof(GSCookie) + offsetof(InlinedCallFrame, m_Datum);
    pInfo->offsetOfReturnAddress         = sizeof(GSCookie) + offsetof(InlinedCallFrame, m_pCallerReturnAddress);
#ifdef TARGET_ARM
    pInfo->offsetOfSPAfterProlog         = sizeof(GSCookie) + offsetof(InlinedCallFrame, m_pSPAfterProlog);
#endif // TARGET_ARM
}

CORINFO_OS getClrVmOs()
{
#ifdef TARGET_OSX
    return CORINFO_APPLE;
#elif defined(TARGET_UNIX)
    return CORINFO_UNIX;
#else
    return CORINFO_WINNT;
#endif
}

/*********************************************************************/
// Return details about EE internal data structures
void CEEInfo::getEEInfo(CORINFO_EE_INFO *pEEInfoOut)
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    } CONTRACTL_END;

    INDEBUG(memset(pEEInfoOut, 0xCC, sizeof(*pEEInfoOut)));

    JIT_TO_EE_TRANSITION();

    InlinedCallFrame::GetEEInfo(&pEEInfoOut->inlinedCallFrameInfo);

    // Offsets into the Thread structure
    pEEInfoOut->offsetOfThreadFrame = Thread::GetOffsetOfCurrentFrame();
    pEEInfoOut->offsetOfGCState     = Thread::GetOffsetOfGCFlag();

#ifndef CROSSBITNESS_COMPILE
    // The assertions must hold in every non-crossbitness scenario
    _ASSERTE(OFFSETOF__DelegateObject__target       == DelegateObject::GetOffsetOfTarget());
    _ASSERTE(OFFSETOF__DelegateObject__methodPtr    == DelegateObject::GetOffsetOfMethodPtr());
    _ASSERTE(OFFSETOF__DelegateObject__methodPtrAux == DelegateObject::GetOffsetOfMethodPtrAux());
    _ASSERTE(OFFSETOF__PtrArray__m_Array_           == PtrArray::GetDataOffset());
#endif

    // Delegate offsets
    pEEInfoOut->offsetOfDelegateInstance    = OFFSETOF__DelegateObject__target;
    pEEInfoOut->offsetOfDelegateFirstTarget = OFFSETOF__DelegateObject__methodPtr;

    // Wrapper delegate offsets
    pEEInfoOut->offsetOfWrapperDelegateIndirectCell = OFFSETOF__DelegateObject__methodPtrAux;

    pEEInfoOut->sizeOfReversePInvokeFrame = TARGET_POINTER_SIZE * READYTORUN_ReversePInvokeTransitionFrameSizeInPointerUnits;

    // The following assert doesn't work in cross-bitness scenarios since the pointer size differs.
#if (defined(TARGET_64BIT) && defined(HOST_64BIT)) || (defined(TARGET_32BIT) && defined(HOST_32BIT))
    _ASSERTE(sizeof(ReversePInvokeFrame) <= pEEInfoOut->sizeOfReversePInvokeFrame);
#endif

    pEEInfoOut->osPageSize = GetOsPageSize();
    pEEInfoOut->maxUncheckedOffsetForNullObject = MAX_UNCHECKED_OFFSET_FOR_NULL_OBJECT;
    pEEInfoOut->targetAbi = CORINFO_CORECLR_ABI;
    pEEInfoOut->osType = getClrVmOs();

    EE_TO_JIT_TRANSITION();
}

const char16_t * CEEInfo::getJitTimeLogFilename()
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    } CONTRACTL_END;

    LPCWSTR result = NULL;

    JIT_TO_EE_TRANSITION();
    result = CLRConfig::GetConfigValue(CLRConfig::INTERNAL_JitTimeLogFile);
    EE_TO_JIT_TRANSITION();

    return (const char16_t *)result;
}



    // Return details about EE internal data structures
uint32_t CEEInfo::getThreadTLSIndex(void **ppIndirection)
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    } CONTRACTL_END;

    uint32_t result = (uint32_t)-1;

    if (ppIndirection != NULL)
        *ppIndirection = NULL;

    return result;
}


int32_t * CEEInfo::getAddrOfCaptureThreadGlobal(void **ppIndirection)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_PREEMPTIVE;
    } CONTRACTL_END;

    int32_t * result = NULL;

    if (ppIndirection != NULL)
        *ppIndirection = NULL;

    JIT_TO_EE_TRANSITION_LEAF();

    result = (int32_t *)&g_TrapReturningThreads;

    EE_TO_JIT_TRANSITION_LEAF();

    return result;
}


// This method is called from CEEInfo::FilterException which
// is run as part of the SEH filter clause for the JIT.
// It is fatal to throw an exception while running a SEH filter clause
// so our contract is NOTHROW, NOTRIGGER.
//
LONG EEFilterException(struct _EXCEPTION_POINTERS *pExceptionPointers, void *unused)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
    } CONTRACTL_END;

    int result = 0;

    JIT_TO_EE_TRANSITION_LEAF();

    unsigned code = pExceptionPointers->ExceptionRecord->ExceptionCode;

#ifdef _DEBUG
    if (code == EXCEPTION_ACCESS_VIOLATION)
    {
        static int hit = 0;
        if (hit++ == 0)
        {
            _ASSERTE(!"Access violation while Jitting!");
            // If you set the debugger to catch access violations and 'go'
            // you will get back to the point at which the access violation occurred
            result = EXCEPTION_CONTINUE_EXECUTION;
        }
        else
        {
            result = EXCEPTION_CONTINUE_SEARCH;
        }
    }
    else
#endif // _DEBUG
    // No one should be catching breakpoint
    // Similarly the JIT doesn't know how to reset the guard page, so it shouldn't
    // be catching a hard stack overflow
    if (code == EXCEPTION_BREAKPOINT || code == EXCEPTION_SINGLE_STEP || code == EXCEPTION_STACK_OVERFLOW)
    {
        result = EXCEPTION_CONTINUE_SEARCH;
    }
    else if (!IsComPlusException(pExceptionPointers->ExceptionRecord))
    {
        result = EXCEPTION_EXECUTE_HANDLER;
    }
    else
    {
        GCX_COOP();

        // This is actually the LastThrown exception object.
        OBJECTREF throwable = CLRException::GetThrowableFromExceptionRecord(pExceptionPointers->ExceptionRecord);

        if (throwable != NULL)
        {
            struct
            {
                OBJECTREF oLastThrownObject;
            } _gc;

            ZeroMemory(&_gc, sizeof(_gc));

            // Setup the throwables
            _gc.oLastThrownObject = throwable;

            GCPROTECT_BEGIN(_gc);

            // Don't catch ThreadAbort and other uncatchable exceptions
            if (IsUncatchable(&_gc.oLastThrownObject))
                result = EXCEPTION_CONTINUE_SEARCH;
            else
                result = EXCEPTION_EXECUTE_HANDLER;

            GCPROTECT_END();
        }
    }

    EE_TO_JIT_TRANSITION_LEAF();

    return result;
}

// This code is called if FilterException chose to handle the exception.
void CEEInfo::HandleException(struct _EXCEPTION_POINTERS *pExceptionPointers)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
    } CONTRACTL_END;

    JIT_TO_EE_TRANSITION_LEAF();

    if (IsComPlusException(pExceptionPointers->ExceptionRecord))
    {
        GCX_COOP();

        // This is actually the LastThrown exception object.
        OBJECTREF throwable = CLRException::GetThrowableFromExceptionRecord(pExceptionPointers->ExceptionRecord);

        if (throwable != NULL)
        {
            struct
            {
                OBJECTREF oLastThrownObject;
                OBJECTREF oCurrentThrowable;
            } _gc;

            ZeroMemory(&_gc, sizeof(_gc));

            PTR_Thread pCurThread = GetThread();

            // Setup the throwables
            _gc.oLastThrownObject = throwable;

            // This will be NULL if no managed exception is active. Otherwise,
            // it will reference the active throwable.
            _gc.oCurrentThrowable = pCurThread->GetThrowable();

            GCPROTECT_BEGIN(_gc);

            // JIT does not use or reference managed exceptions at all and simply swallows them,
            // or lets them fly through so that they will either get caught in managed code, the VM
            // or will go unhandled.
            //
            // Blind swallowing of managed exceptions can break the semantic of "which exception handler"
            // gets to process the managed exception first. The expected handler is managed code exception
            // handler (e.g. COMPlusFrameHandler on x86 and ProcessCLRException on 64bit) which will setup
            // the exception tracker for the exception that will enable the expected sync between the
            // LastThrownObject (LTO), setup in RaiseTheExceptionInternalOnly, and the exception tracker.
            //
            // However, JIT can break this by swallowing the managed exception before managed code exception
            // handler gets a chance to setup an exception tracker for it. Since there is no cleanup
            // done for the swallowed exception as part of the unwind (because no exception tracker may have been setup),
            // we need to reset the LTO, if it is out of sync from the active throwable.
            //
            // Hence, check if the LastThrownObject and active-exception throwable are in sync or not.
            // If not, bring them in sync.
            //
            // Example
            // -------
            // It is possible that an exception was already in progress and while processing it (e.g.
            // invoking finally block), we invoked JIT that had another managed exception @ JIT-EE transition boundary
            // that is swallowed by the JIT before managed code exception handler sees it. This breaks the sync between
            // LTO and the active exception in the exception tracker.
            if (_gc.oCurrentThrowable != _gc.oLastThrownObject)
            {
                // Update the LTO.
                //
                // Note: Incase of OOM, this will get set to OOM instance.
                pCurThread->SafeSetLastThrownObject(_gc.oCurrentThrowable);
            }

            GCPROTECT_END();
        }
    }

    EE_TO_JIT_TRANSITION_LEAF();
}

CORINFO_MODULE_HANDLE CEEInfo::embedModuleHandle(CORINFO_MODULE_HANDLE handle,
                                                 void **ppIndirection)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_PREEMPTIVE;
        PRECONDITION(!IsDynamicScope(handle));
    }
    CONTRACTL_END;

    if (ppIndirection != NULL)
        *ppIndirection = NULL;

    JIT_TO_EE_TRANSITION_LEAF();

    EE_TO_JIT_TRANSITION_LEAF();

    return handle;
}

CORINFO_CLASS_HANDLE CEEInfo::embedClassHandle(CORINFO_CLASS_HANDLE handle,
                                               void **ppIndirection)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_PREEMPTIVE;
    }
    CONTRACTL_END;

    if (ppIndirection != NULL)
        *ppIndirection = NULL;

    JIT_TO_EE_TRANSITION_LEAF();

    EE_TO_JIT_TRANSITION_LEAF();

    return handle;
}

CORINFO_FIELD_HANDLE CEEInfo::embedFieldHandle(CORINFO_FIELD_HANDLE handle,
                                               void **ppIndirection)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_PREEMPTIVE;
    }
    CONTRACTL_END;

    if (ppIndirection != NULL)
        *ppIndirection = NULL;

    JIT_TO_EE_TRANSITION_LEAF();

    EE_TO_JIT_TRANSITION_LEAF();

    return handle;
}

CORINFO_METHOD_HANDLE CEEInfo::embedMethodHandle(CORINFO_METHOD_HANDLE handle,
                                                 void **ppIndirection)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_PREEMPTIVE;
    }
    CONTRACTL_END;

    if (ppIndirection != NULL)
        *ppIndirection = NULL;

    JIT_TO_EE_TRANSITION_LEAF();

    EE_TO_JIT_TRANSITION_LEAF();

    return handle;
}

/*********************************************************************/
void CEEInfo::setJitFlags(const CORJIT_FLAGS& jitFlags)
{
    LIMITED_METHOD_CONTRACT;

    m_jitFlags = jitFlags;
}

/*********************************************************************/
uint32_t CEEInfo::getJitFlags(CORJIT_FLAGS* jitFlags, uint32_t sizeInBytes)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_PREEMPTIVE;
    } CONTRACTL_END;

    JIT_TO_EE_TRANSITION_LEAF();

    _ASSERTE(sizeInBytes >= sizeof(m_jitFlags));
    *jitFlags = m_jitFlags;

    EE_TO_JIT_TRANSITION_LEAF();

    return sizeof(m_jitFlags);
}

/*********************************************************************/
#if !defined(TARGET_UNIX)

struct RunWithErrorTrapFilterParam
{
    ICorDynamicInfo* m_corInfo;
    void (*m_function)(void*);
    void* m_param;
    EXCEPTION_POINTERS m_exceptionPointers;
};

static LONG RunWithErrorTrapFilter(struct _EXCEPTION_POINTERS* exceptionPointers, void* theParam)
{
    WRAPPER_NO_CONTRACT;

    auto* param = reinterpret_cast<RunWithErrorTrapFilterParam*>(theParam);
    param->m_exceptionPointers = *exceptionPointers;
    return EEFilterException(exceptionPointers, nullptr);
}

#endif // !defined(TARGET_UNIX)

bool CEEInfo::runWithSPMIErrorTrap(void (*function)(void*), void* param)
{
    // No dynamic contract here because SEH is used
    STATIC_CONTRACT_THROWS;
    STATIC_CONTRACT_GC_TRIGGERS;
    STATIC_CONTRACT_MODE_PREEMPTIVE;

    // NOTE: the lack of JIT/EE transition markers in this method is intentional. Any
    //       transitions into the EE proper should occur either via JIT/EE
    //       interface calls made by `function`.

    // As we aren't SPMI, we don't need to do anything other than call the function.

    function(param);
    return true;
}

bool CEEInfo::runWithErrorTrap(void (*function)(void*), void* param)
{
    // No dynamic contract here because SEH is used
    STATIC_CONTRACT_THROWS;
    STATIC_CONTRACT_GC_TRIGGERS;
    STATIC_CONTRACT_MODE_PREEMPTIVE;

    // NOTE: the lack of JIT/EE transition markers in this method is intentional. Any
    //       transitions into the EE proper should occur either via the call to
    //       `EEFilterException` (which is appropriately marked) or via JIT/EE
    //       interface calls made by `function`.

    bool success = true;

#if !defined(TARGET_UNIX)

    RunWithErrorTrapFilterParam trapParam;
    trapParam.m_corInfo = this;
    trapParam.m_function = function;
    trapParam.m_param = param;

    PAL_TRY(RunWithErrorTrapFilterParam*, pTrapParam, &trapParam)
    {
        pTrapParam->m_function(pTrapParam->m_param);
    }
    PAL_EXCEPT_FILTER(RunWithErrorTrapFilter)
    {
        HandleException(&trapParam.m_exceptionPointers);
        success = false;
    }
    PAL_ENDTRY

#else // !defined(TARGET_UNIX)

    // We shouldn't need PAL_TRY on *nix: any exceptions that we are able to catch
    // ought to originate from the runtime itself and should be catchable inside of
    // EX_TRY/EX_CATCH, including emulated SEH exceptions.
    EX_TRY
    {
        function(param);
    }
    EX_CATCH
    {
        success = false;
    }
    EX_END_CATCH(RethrowTerminalExceptions);

#endif

    return success;
}

/*********************************************************************/
int CEEInfo::doAssert(const char* szFile, int iLine, const char* szExpr)
{
    STATIC_CONTRACT_THROWS;
    STATIC_CONTRACT_GC_TRIGGERS;
    STATIC_CONTRACT_MODE_PREEMPTIVE;
    STATIC_CONTRACT_DEBUG_ONLY;

    int result = 0;

    JIT_TO_EE_TRANSITION();


#ifdef _DEBUG
    BEGIN_DEBUG_ONLY_CODE;
    if (CLRConfig::GetConfigValue(CLRConfig::INTERNAL_JitThrowOnAssertionFailure) != 0)
    {
        SString output;
        output.Printf(
            "JIT assert failed:\n"
            "%s\n"
            "    File: %s Line: %d\n",
            szExpr, szFile, iLine);
        COMPlusThrowNonLocalized(kInvalidProgramException, output.GetUnicode());
    }

    result = _DbgBreakCheck(szFile, iLine, szExpr);
    END_DEBUG_ONLY_CODE;
#else // !_DEBUG
    result = 1;   // break into debugger
#endif // !_DEBUG


    EE_TO_JIT_TRANSITION();

    return result;
}

void CEEInfo::reportFatalError(CorJitResult result)
{
    STATIC_CONTRACT_THROWS;
    STATIC_CONTRACT_GC_TRIGGERS;
    STATIC_CONTRACT_MODE_PREEMPTIVE;

    JIT_TO_EE_TRANSITION_LEAF();

    STRESS_LOG2(LF_JIT,LL_ERROR, "Jit reported error 0x%x while compiling 0x%p\n",
                (int)result, (INT_PTR)getMethodBeingCompiled());

    EE_TO_JIT_TRANSITION_LEAF();
}

bool CEEInfo::logMsg(unsigned level, const char* fmt, va_list args)
{
    STATIC_CONTRACT_THROWS;
    STATIC_CONTRACT_GC_TRIGGERS;
    STATIC_CONTRACT_MODE_PREEMPTIVE;
    STATIC_CONTRACT_DEBUG_ONLY;

    bool result = false;

    JIT_TO_EE_TRANSITION_LEAF();

#ifdef LOGGING
    if (LoggingOn(LF_JIT, level))
    {
        LogSpewValist(LF_JIT, level, (char*) fmt, args);
        result = true;
    }
#endif // LOGGING

    EE_TO_JIT_TRANSITION_LEAF();

    return result;
}


/*********************************************************************/

void* CEEJitInfo::getHelperFtn(CorInfoHelpFunc    ftnNum,         /* IN  */
                               void **            ppIndirection)  /* OUT */
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_PREEMPTIVE;
    } CONTRACTL_END;

    void* result = NULL;

    if (ppIndirection != NULL)
        *ppIndirection = NULL;

    JIT_TO_EE_TRANSITION_LEAF();

    _ASSERTE(ftnNum < CORINFO_HELP_COUNT);

    void* pfnHelper = hlpFuncTable[ftnNum].pfnHelper;

    size_t dynamicFtnNum = ((size_t)pfnHelper - 1);
    if (dynamicFtnNum < DYNAMIC_CORINFO_HELP_COUNT)
    {
#ifdef _PREFAST_
#pragma warning(push)
#pragma warning(disable:26001) // "Bounds checked above using the underflow trick"
#endif /*_PREFAST_ */

#if defined(TARGET_AMD64)
        // To avoid using a jump stub we always call certain helpers using an indirect call.
        // Because when using a direct call and the target is father away than 2^31 bytes,
        // the direct call instead goes to a jump stub which jumps to the jit helper.
        // However in this process the jump stub will corrupt RAX.
        //
        // The set of helpers for which RAX must be preserved are profiler probes, CFG dispatcher
        // and the STOP_FOR_GC helper which maps to JIT_RareDisableHelper.
        // In the case of the STOP_FOR_GC helper RAX can be holding a function return value.
        //
        if (dynamicFtnNum == DYNAMIC_CORINFO_HELP_STOP_FOR_GC    ||
            dynamicFtnNum == DYNAMIC_CORINFO_HELP_PROF_FCN_ENTER ||
            dynamicFtnNum == DYNAMIC_CORINFO_HELP_PROF_FCN_LEAVE ||
            dynamicFtnNum == DYNAMIC_CORINFO_HELP_PROF_FCN_TAILCALL ||
            dynamicFtnNum == DYNAMIC_CORINFO_HELP_DISPATCH_INDIRECT_CALL)
        {
            _ASSERTE(ppIndirection != NULL);
            *ppIndirection = &hlpDynamicFuncTable[dynamicFtnNum].pfnHelper;
            return NULL;
        }
#endif

        // Check if we already have a cached address of the final target
        static LPVOID hlpFinalTierAddrTable[DYNAMIC_CORINFO_HELP_COUNT] = {};
        LPVOID finalTierAddr = hlpFinalTierAddrTable[dynamicFtnNum];
        if (finalTierAddr != NULL)
        {
            return finalTierAddr;
        }

        if (dynamicFtnNum == DYNAMIC_CORINFO_HELP_ISINSTANCEOFINTERFACE ||
            dynamicFtnNum == DYNAMIC_CORINFO_HELP_ISINSTANCEOFANY ||
            dynamicFtnNum == DYNAMIC_CORINFO_HELP_ISINSTANCEOFARRAY ||
            dynamicFtnNum == DYNAMIC_CORINFO_HELP_ISINSTANCEOFCLASS ||
            dynamicFtnNum == DYNAMIC_CORINFO_HELP_CHKCASTANY ||
            dynamicFtnNum == DYNAMIC_CORINFO_HELP_CHKCASTARRAY ||
            dynamicFtnNum == DYNAMIC_CORINFO_HELP_CHKCASTINTERFACE ||
            dynamicFtnNum == DYNAMIC_CORINFO_HELP_CHKCASTCLASS ||
            dynamicFtnNum == DYNAMIC_CORINFO_HELP_CHKCASTCLASS_SPECIAL ||
            dynamicFtnNum == DYNAMIC_CORINFO_HELP_UNBOX ||
            dynamicFtnNum == DYNAMIC_CORINFO_HELP_ARRADDR_ST ||
            dynamicFtnNum == DYNAMIC_CORINFO_HELP_LDELEMA_REF)
        {
            Precode* pPrecode = Precode::GetPrecodeFromEntryPoint((PCODE)hlpDynamicFuncTable[dynamicFtnNum].pfnHelper);
            _ASSERTE(pPrecode->GetType() == PRECODE_FIXUP);

            // Check if the target MethodDesc is already jitted to its final Tier
            // so we no longer need to use indirections and can emit a direct call instead.
            //
            // Avoid taking the lock for foreground jit compilations
            if (!GetAppDomain()->GetTieredCompilationManager()->IsTieringDelayActive())
            {
                MethodDesc* helperMD = pPrecode->GetMethodDesc();
                _ASSERT(helperMD != nullptr);

                CodeVersionManager* manager = helperMD->GetCodeVersionManager();
                NativeCodeVersion activeCodeVersion;

                {
                    // Get active code version under a lock
                    CodeVersionManager::LockHolder codeVersioningLockHolder;
                    activeCodeVersion = manager->GetActiveILCodeVersion(helperMD).GetActiveNativeCodeVersion(helperMD);
                }

                if (activeCodeVersion.IsFinalTier())
                {
                    finalTierAddr = (LPVOID)activeCodeVersion.GetNativeCode();
                    if (finalTierAddr != NULL)
                    {
                        // Cache it for future uses to avoid taking the lock again.
                        hlpFinalTierAddrTable[dynamicFtnNum] = finalTierAddr;
                        return finalTierAddr;
                    }
                }
            }

            *ppIndirection = ((FixupPrecode*)pPrecode)->GetTargetSlot();
            return NULL;
        }

        pfnHelper = hlpDynamicFuncTable[dynamicFtnNum].pfnHelper;

#ifdef _PREFAST_
#pragma warning(pop)
#endif /*_PREFAST_*/
    }

    _ASSERTE(pfnHelper);

    result = (LPVOID)GetEEFuncEntryPoint(pfnHelper);

    EE_TO_JIT_TRANSITION_LEAF();

    return result;
}

PCODE CEEJitInfo::getHelperFtnStatic(CorInfoHelpFunc ftnNum)
{
    LIMITED_METHOD_CONTRACT;

    void* pfnHelper = hlpFuncTable[ftnNum].pfnHelper;

    // If pfnHelper is an index into the dynamic helper table, it should be less
    // than DYNAMIC_CORINFO_HELP_COUNT.  In this case we need to find the actual pfnHelper
    // using an extra indirection.  Note the special case
    // where pfnHelper==0 where pfnHelper-1 will underflow and we will avoid the indirection.
    if (((size_t)pfnHelper - 1) < DYNAMIC_CORINFO_HELP_COUNT)
    {
        pfnHelper = hlpDynamicFuncTable[((size_t)pfnHelper - 1)].pfnHelper;
    }

    _ASSERTE(pfnHelper != NULL);

    return GetEEFuncEntryPoint(pfnHelper);
}

// Wrapper around CEEInfo::GetProfilingHandle.  The first time this is called for a
// method desc, it calls through to EEToProfInterfaceImpl::EEFunctionIDMappe and caches the
// result in CEEJitInfo::GetProfilingHandleCache.  Thereafter, this wrapper regurgitates the cached values
// rather than calling into CEEInfo::GetProfilingHandle each time.  This avoids
// making duplicate calls into the profiler's FunctionIDMapper callback.
void CEEJitInfo::GetProfilingHandle(bool                      *pbHookFunction,
                                    void                     **pProfilerHandle,
                                    bool                      *pbIndirectedHandles)
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    } CONTRACTL_END;

    _ASSERTE(pbHookFunction != NULL);
    _ASSERTE(pProfilerHandle != NULL);
    _ASSERTE(pbIndirectedHandles != NULL);

    if (!m_gphCache.m_bGphIsCacheValid)
    {
#ifdef PROFILING_SUPPORTED
        JIT_TO_EE_TRANSITION();

        // Cache not filled in, so make our first and only call to CEEInfo::GetProfilingHandle here

        // methods with no metadata behind cannot be exposed to tools expecting metadata (profiler, debugger...)
        // they shouldnever come here as they are called out in GetCompileFlag
        _ASSERTE(!m_pMethodBeingCompiled->IsNoMetadata());

        // We pass in the typical method definition to the function mapper because in
        // Whidbey all the profiling API transactions are done in terms of typical
        // method definitions not instantiations.
        BOOL bHookFunction = TRUE;
        void * profilerHandle = m_pMethodBeingCompiled;

        {
            BEGIN_PROFILER_CALLBACK(CORProfilerFunctionIDMapperEnabled());
            profilerHandle = (void *)(&g_profControlBlock)->EEFunctionIDMapper((FunctionID) m_pMethodBeingCompiled, &bHookFunction);
            END_PROFILER_CALLBACK();
        }

        m_gphCache.m_pvGphProfilerHandle = profilerHandle;
        m_gphCache.m_bGphHookFunction = (bHookFunction != FALSE);
        m_gphCache.m_bGphIsCacheValid = true;

        EE_TO_JIT_TRANSITION();
#endif //PROFILING_SUPPORTED
    }

    // Our cache of these values are bitfield bools, but the interface requires
    // bool.  So to avoid setting aside a staging area on the stack for these
    // values, we filled them in directly in the if (not cached yet) case.
    *pbHookFunction = (m_gphCache.m_bGphHookFunction != false);

    // At this point, the remaining values must be in the cache by now, so use them
    *pProfilerHandle = m_gphCache.m_pvGphProfilerHandle;

    //
    // This is the JIT case, which is never indirected.
    //
    *pbIndirectedHandles = false;
}

/*********************************************************************/
void CEEJitInfo::WriteCodeBytes()
{
    LIMITED_METHOD_CONTRACT;

#ifdef USE_INDIRECT_CODEHEADER
    if (m_pRealCodeHeader != NULL)
    {
        // Restore the read only version of the real code header
        m_CodeHeaderRW->SetRealCodeHeader(m_pRealCodeHeader);
        m_pRealCodeHeader = NULL;
    }
#endif // USE_INDIRECT_CODEHEADER

    if (m_CodeHeaderRW != m_CodeHeader)
    {
        ExecutableWriterHolder<void> codeWriterHolder((void *)m_CodeHeader, m_codeWriteBufferSize);
        memcpy(codeWriterHolder.GetRW(), m_CodeHeaderRW, m_codeWriteBufferSize);
    }
}

/*********************************************************************/
void CEEJitInfo::BackoutJitData(EEJitManager * jitMgr)
{
    CONTRACTL {
        NOTHROW;
        GC_TRIGGERS;
    } CONTRACTL_END;

    // The RemoveJitData call below requires the m_CodeHeader to be valid, so we need to write
    // the code bytes to the target memory location.
    WriteCodeBytes();

    CodeHeader* pCodeHeader = m_CodeHeader;
    if (pCodeHeader)
        jitMgr->RemoveJitData(pCodeHeader, m_GCinfo_len, m_EHinfo_len);
}

/*********************************************************************/
void CEEJitInfo::WriteCode(EEJitManager * jitMgr)
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
    } CONTRACTL_END;

#ifdef FEATURE_INTERPRETER
    // TODO: the InterpterCEEInfo doesn't support features about W^X.
    // see also #53173
    if (m_pCodeHeap == nullptr) return;
#endif

    WriteCodeBytes();

    // Now that the code header was written to the final location, publish the code via the nibble map
    jitMgr->NibbleMapSet(m_pCodeHeap, m_CodeHeader->GetCodeStartAddress(), TRUE);

#if defined(TARGET_AMD64)
    // Publish the new unwind information in a way that the ETW stack crawler can find
    _ASSERTE(m_usedUnwindInfos == m_totalUnwindInfos);
    UnwindInfoTable::PublishUnwindInfoForMethod(m_moduleBase, m_CodeHeader->GetUnwindInfo(0), m_totalUnwindInfos);
#endif // defined(TARGET_AMD64)

}


/*********************************************************************/
// Route jit information to the Jit Debug store.
void CEEJitInfo::setBoundaries(CORINFO_METHOD_HANDLE ftn, uint32_t cMap,
                               ICorDebugInfo::OffsetMapping *pMap)
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    } CONTRACTL_END;

    JIT_TO_EE_TRANSITION();

    // We receive ownership of the array
    _ASSERTE(m_pOffsetMapping == NULL && m_iOffsetMapping == 0);
    m_iOffsetMapping = cMap;
    m_pOffsetMapping = pMap;

    EE_TO_JIT_TRANSITION();
}

void CEEJitInfo::setVars(CORINFO_METHOD_HANDLE ftn, uint32_t cVars, ICorDebugInfo::NativeVarInfo *vars)
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    } CONTRACTL_END;

    JIT_TO_EE_TRANSITION();

    // We receive ownership of the array
    _ASSERTE(m_pNativeVarInfo == NULL && m_iNativeVarInfo == 0);
    m_iNativeVarInfo = cVars;
    m_pNativeVarInfo = vars;

    EE_TO_JIT_TRANSITION();
}

void CEEJitInfo::reportRichMappings(
        ICorDebugInfo::InlineTreeNode*    inlineTreeNodes,
        uint32_t                          numInlineTreeNodes,
        ICorDebugInfo::RichOffsetMapping* mappings,
        uint32_t                          numMappings)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_PREEMPTIVE;
    } CONTRACTL_END;

    JIT_TO_EE_TRANSITION();

    if (m_jitManager->IsStoringRichDebugInfo())
    {
        m_inlineTreeNodes = inlineTreeNodes;
        m_numInlineTreeNodes = numInlineTreeNodes;
        m_richOffsetMappings = mappings;
        m_numRichOffsetMappings = numMappings;
    }
    else
    {
        freeArrayInternal(inlineTreeNodes);
        freeArrayInternal(mappings);
    }

    EE_TO_JIT_TRANSITION();
}

void CEEJitInfo::setPatchpointInfo(PatchpointInfo* patchpointInfo)
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    } CONTRACTL_END;

    JIT_TO_EE_TRANSITION();

#ifdef FEATURE_ON_STACK_REPLACEMENT
    // We receive ownership of the array
    _ASSERTE(m_pPatchpointInfoFromJit == NULL);
    m_pPatchpointInfoFromJit = patchpointInfo;
#else
    UNREACHABLE();
#endif

    EE_TO_JIT_TRANSITION();
}

PatchpointInfo* CEEJitInfo::getOSRInfo(unsigned* ilOffset)
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    } CONTRACTL_END;

    PatchpointInfo* result = NULL;
    *ilOffset = 0;

    JIT_TO_EE_TRANSITION();

#ifdef FEATURE_ON_STACK_REPLACEMENT
    result = m_pPatchpointInfoFromRuntime;
    *ilOffset = m_ilOffset;
#endif

    EE_TO_JIT_TRANSITION();

    return result;
}

void CEEJitInfo::CompressDebugInfo()
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    } CONTRACTL_END;

#ifdef FEATURE_ON_STACK_REPLACEMENT
    PatchpointInfo* patchpointInfo = m_pPatchpointInfoFromJit;
#else
    PatchpointInfo* patchpointInfo = NULL;
#endif

    // Don't track JIT info for DynamicMethods.
    if (m_pMethodBeingCompiled->IsDynamicMethod() && !g_pConfig->GetTrackDynamicMethodDebugInfo())
    {
        _ASSERTE(patchpointInfo == NULL);
        return;
    }

    if ((m_iOffsetMapping == 0) && (m_iNativeVarInfo == 0) && (patchpointInfo == NULL) && (m_numInlineTreeNodes == 0) && (m_numRichOffsetMappings == 0))
        return;

    JIT_TO_EE_TRANSITION();

    EX_TRY
    {
        BOOL writeFlagByte = FALSE;
#ifdef FEATURE_ON_STACK_REPLACEMENT
        writeFlagByte = TRUE;
#endif
        if (m_jitManager->IsStoringRichDebugInfo())
            writeFlagByte = TRUE;

        PTR_BYTE pDebugInfo = CompressDebugInfo::CompressBoundariesAndVars(
            m_pOffsetMapping, m_iOffsetMapping,
            m_pNativeVarInfo, m_iNativeVarInfo,
            patchpointInfo,
            m_inlineTreeNodes, m_numInlineTreeNodes,
            m_richOffsetMappings, m_numRichOffsetMappings,
            writeFlagByte,
            m_pMethodBeingCompiled->GetLoaderAllocator()->GetLowFrequencyHeap());

        m_CodeHeaderRW->SetDebugInfo(pDebugInfo);
    }
    EX_CATCH
    {
        // Just ignore exceptions here. The debugger's structures will still be in a consistent state.
    }
    EX_END_CATCH(SwallowAllExceptions)

    EE_TO_JIT_TRANSITION();
}

void reservePersonalityRoutineSpace(uint32_t &unwindSize)
{
#if defined(TARGET_X86)
    // Do nothing
#elif defined(TARGET_AMD64)
    // Add space for personality routine, it must be 4-byte aligned.
    // Everything in the UNWIND_INFO up to the variable-sized UnwindCodes
    // array has already had its size included in unwindSize by the caller.
    unwindSize += sizeof(ULONG);

    // Note that the count of unwind codes (2 bytes each) is stored as a UBYTE
    // So the largest size could be 510 bytes, plus the header and language
    // specific stuff.  This can't overflow.

    _ASSERTE(FitsInU4(unwindSize + sizeof(ULONG)));
    unwindSize = (ULONG)(ALIGN_UP(unwindSize, sizeof(ULONG)));
#elif defined(TARGET_ARM) || defined(TARGET_ARM64)
    // The JIT passes in a 4-byte aligned block of unwind data.
    _ASSERTE(IS_ALIGNED(unwindSize, sizeof(ULONG)));

    // Add space for personality routine, it must be 4-byte aligned.
    unwindSize += sizeof(ULONG);
#elif defined(TARGET_LOONGARCH64)
    // The JIT passes in a 4-byte aligned block of unwind data.
    _ASSERTE(IS_ALIGNED(unwindSize, sizeof(ULONG)));

    // Add space for personality routine, it must be 4-byte aligned.
    unwindSize += sizeof(ULONG);
#elif defined(TARGET_RISCV64)
    // The JIT passes in a 4-byte aligned block of unwind data.
    _ASSERTE(IS_ALIGNED(unwindSize, sizeof(ULONG)));

    // Add space for personality routine, it must be 4-byte aligned.
    unwindSize += sizeof(ULONG);
#else
    PORTABILITY_ASSERT("reservePersonalityRoutineSpace");
#endif // !defined(TARGET_AMD64)

}
// Reserve memory for the method/funclet's unwind information.
// Note that this must be called before allocMem. It should be
// called once for the main method, once for every funclet, and
// once for every block of cold code for which allocUnwindInfo
// will be called.
//
// This is necessary because jitted code must allocate all the
// memory needed for the unwindInfo at the allocMem call.
// For prejitted code we split up the unwinding information into
// separate sections .rdata and .pdata.
//
void CEEJitInfo::reserveUnwindInfo(bool isFunclet, bool isColdCode, uint32_t unwindSize)
{
#ifdef FEATURE_EH_FUNCLETS
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_PREEMPTIVE;
    }
    CONTRACTL_END;

    JIT_TO_EE_TRANSITION_LEAF();

    CONSISTENCY_CHECK_MSG(!isColdCode, "Hot/Cold splitting is not supported in jitted code");
    _ASSERTE_MSG(m_theUnwindBlock == NULL,
        "reserveUnwindInfo() can only be called before allocMem(), but allocMem() has already been called. "
        "This may indicate the JIT has hit a NO_WAY assert after calling allocMem(), and is re-JITting. "
        "Set DOTNET_JitBreakOnBadCode=1 and rerun to get the real error.");

    uint32_t currentSize  = unwindSize;

    reservePersonalityRoutineSpace(currentSize);

    m_totalUnwindSize += currentSize;

    m_totalUnwindInfos++;

    EE_TO_JIT_TRANSITION_LEAF();
#else // FEATURE_EH_FUNCLETS
    LIMITED_METHOD_CONTRACT;
    // Dummy implementation to make cross-platform altjit work
#endif // FEATURE_EH_FUNCLETS
}

// Allocate and initialize the .rdata and .pdata for this method or
// funclet and get the block of memory needed for the machine specific
// unwind information (the info for crawling the stack frame).
// Note that allocMem must be called first.
//
// The pHotCode parameter points at the first byte of the code of the method
// The startOffset and endOffset are the region (main or funclet) that
// we are to allocate and create .rdata and .pdata for.
// The pUnwindBlock is copied and contains the .pdata unwind area
//
// Parameters:
//
//    pHotCode        main method code buffer, always filled in
//    pColdCode       always NULL for jitted code
//    startOffset     start of code block, relative to pHotCode
//    endOffset       end of code block, relative to pHotCode
//    unwindSize      size of unwind info pointed to by pUnwindBlock
//    pUnwindBlock    pointer to unwind info
//    funcKind        type of funclet (main method code, handler, filter)
//
void CEEJitInfo::allocUnwindInfo (
        uint8_t *           pHotCode,              /* IN */
        uint8_t *           pColdCode,             /* IN */
        uint32_t            startOffset,           /* IN */
        uint32_t            endOffset,             /* IN */
        uint32_t            unwindSize,            /* IN */
        uint8_t *           pUnwindBlock,          /* IN */
        CorJitFuncKind      funcKind               /* IN */
        )
{
#ifdef FEATURE_EH_FUNCLETS
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        PRECONDITION(m_theUnwindBlock != NULL);
        PRECONDITION(m_usedUnwindSize < m_totalUnwindSize);
        PRECONDITION(m_usedUnwindInfos < m_totalUnwindInfos);
        PRECONDITION(endOffset <= m_codeSize);
    } CONTRACTL_END;

    CONSISTENCY_CHECK_MSG(pColdCode == NULL, "Hot/Cold code splitting not supported for jitted code");

    JIT_TO_EE_TRANSITION();

    //
    // We add one callback-type dynamic function table per range section.
    // Therefore, the RUNTIME_FUNCTION info is always relative to the
    // image base contained in the dynamic function table, which happens
    // to be the LowAddress of the range section.  The JIT has no
    // knowledge of the range section, so it gives us offsets that are
    // relative to the beginning of the method (pHotCode) and we allocate
    // and initialize the RUNTIME_FUNCTION data and record its location
    // in this function.
    //

    if (funcKind != CORJIT_FUNC_ROOT)
    {
        // The main method should be emitted before funclets
        _ASSERTE(m_usedUnwindInfos > 0);
    }

    PT_RUNTIME_FUNCTION pRuntimeFunction = m_CodeHeaderRW->GetUnwindInfo(m_usedUnwindInfos);

    m_usedUnwindInfos++;

    // Make sure that the RUNTIME_FUNCTION is aligned on a DWORD sized boundary
    _ASSERTE(IS_ALIGNED(pRuntimeFunction, sizeof(DWORD)));


    size_t writeableOffset = (BYTE *)m_CodeHeaderRW - (BYTE *)m_CodeHeader;
    UNWIND_INFO * pUnwindInfo = (UNWIND_INFO *) &(m_theUnwindBlock[m_usedUnwindSize]);
    UNWIND_INFO * pUnwindInfoRW = (UNWIND_INFO *)((BYTE*)pUnwindInfo + writeableOffset);

    m_usedUnwindSize += unwindSize;

    reservePersonalityRoutineSpace(m_usedUnwindSize);

    _ASSERTE(m_usedUnwindSize <= m_totalUnwindSize);

    // Make sure that the UnwindInfo is aligned
    _ASSERTE(IS_ALIGNED(pUnwindInfo, sizeof(ULONG)));

    /* Calculate Image Relative offset to add to the jit generated unwind offsets */

    TADDR baseAddress = m_moduleBase;

    size_t currentCodeSizeT = (size_t)pHotCode - baseAddress;

    /* Check if currentCodeSizeT offset fits in 32-bits */
    if (!FitsInU4(currentCodeSizeT))
    {
        _ASSERTE(!"Bad currentCodeSizeT");
        COMPlusThrowHR(E_FAIL);
    }

    /* Check if EndAddress offset fits in 32-bit */
    if (!FitsInU4(currentCodeSizeT + endOffset))
    {
        _ASSERTE(!"Bad currentCodeSizeT");
        COMPlusThrowHR(E_FAIL);
    }

    unsigned currentCodeOffset = (unsigned) currentCodeSizeT;

    /* Calculate Unwind Info delta */
    size_t unwindInfoDeltaT = (size_t) pUnwindInfo - baseAddress;

    /* Check if unwindDeltaT offset fits in 32-bits */
    if (!FitsInU4(unwindInfoDeltaT))
    {
        _ASSERTE(!"Bad unwindInfoDeltaT");
        COMPlusThrowHR(E_FAIL);
    }

    unsigned unwindInfoDelta = (unsigned) unwindInfoDeltaT;

    RUNTIME_FUNCTION__SetBeginAddress(pRuntimeFunction, currentCodeOffset + startOffset);

#ifdef TARGET_AMD64
    pRuntimeFunction->EndAddress        = currentCodeOffset + endOffset;
#endif

    RUNTIME_FUNCTION__SetUnwindInfoAddress(pRuntimeFunction, unwindInfoDelta);

#ifdef _DEBUG
    if (funcKind != CORJIT_FUNC_ROOT)
    {
        // Check the new funclet doesn't overlap any existing funclet.

        for (ULONG iUnwindInfo = 0; iUnwindInfo < m_usedUnwindInfos - 1; iUnwindInfo++)
        {
            PT_RUNTIME_FUNCTION pOtherFunction = m_CodeHeaderRW->GetUnwindInfo(iUnwindInfo);
            _ASSERTE((   RUNTIME_FUNCTION__BeginAddress(pOtherFunction) >= RUNTIME_FUNCTION__EndAddress(pRuntimeFunction, baseAddress + writeableOffset)
                     || RUNTIME_FUNCTION__EndAddress(pOtherFunction, baseAddress + writeableOffset) <= RUNTIME_FUNCTION__BeginAddress(pRuntimeFunction)));
        }
    }
#endif // _DEBUG

    memcpy(pUnwindInfoRW, pUnwindBlock, unwindSize);

#if defined(TARGET_X86)

    // Do NOTHING

#elif defined(TARGET_AMD64)

    pUnwindInfoRW->Flags = UNW_FLAG_EHANDLER | UNW_FLAG_UHANDLER;

    ULONG * pPersonalityRoutineRW = (ULONG*)ALIGN_UP(&(pUnwindInfoRW->UnwindCode[pUnwindInfoRW->CountOfUnwindCodes]), sizeof(ULONG));
    *pPersonalityRoutineRW = ExecutionManager::GetCLRPersonalityRoutineValue();

#elif defined(TARGET_ARM64)

    *(LONG *)pUnwindInfoRW |= (1 << 20); // X bit

    ULONG * pPersonalityRoutineRW = (ULONG*)((BYTE *)pUnwindInfoRW + ALIGN_UP(unwindSize, sizeof(ULONG)));
    *pPersonalityRoutineRW = ExecutionManager::GetCLRPersonalityRoutineValue();

#elif defined(TARGET_ARM)

    *(LONG *)pUnwindInfoRW |= (1 << 20); // X bit

    ULONG * pPersonalityRoutineRW = (ULONG*)((BYTE *)pUnwindInfoRW + ALIGN_UP(unwindSize, sizeof(ULONG)));
    *pPersonalityRoutineRW = (TADDR)ProcessCLRException - baseAddress;

#elif defined(TARGET_LOONGARCH64)

    *(LONG *)pUnwindInfoRW |= (1 << 20); // X bit

    ULONG * pPersonalityRoutineRW = (ULONG*)((BYTE *)pUnwindInfoRW + ALIGN_UP(unwindSize, sizeof(ULONG)));
    *pPersonalityRoutineRW = ExecutionManager::GetCLRPersonalityRoutineValue();

#elif defined(TARGET_RISCV64)
    *(LONG *)pUnwindInfoRW |= (1 << 20); // X bit

    ULONG * pPersonalityRoutineRW = (ULONG*)((BYTE *)pUnwindInfoRW + ALIGN_UP(unwindSize, sizeof(ULONG)));
    *pPersonalityRoutineRW = ExecutionManager::GetCLRPersonalityRoutineValue();

#endif

    EE_TO_JIT_TRANSITION();
#else // FEATURE_EH_FUNCLETS
    LIMITED_METHOD_CONTRACT;
    // Dummy implementation to make cross-platform altjit work
#endif // FEATURE_EH_FUNCLETS
}

void CEEJitInfo::recordCallSite(uint32_t              instrOffset,
                                CORINFO_SIG_INFO *    callSig,
                                CORINFO_METHOD_HANDLE methodHandle)
{
    // Currently, only testing tools use this method. The EE itself doesn't need record this information.
    // N.B. The memory that callSig points to is managed by the JIT and isn't guaranteed to be around after
    // this function returns, so future implementations should copy the sig info if they want it to persist.
    LIMITED_METHOD_CONTRACT;
}

// This is a variant for AMD64 or other machines that
// cannot always hold the destination address in a 32-bit location
// A relocation is recorded if we are pre-jitting.
// A jump thunk may be inserted if we are jitting

void CEEJitInfo::recordRelocation(void * location,
                                  void * locationRW,
                                  void * target,
                                  WORD   fRelocType,
                                  INT32  addlDelta)
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    } CONTRACTL_END;

#ifdef HOST_64BIT
    JIT_TO_EE_TRANSITION();

    INT64 delta;

    switch (fRelocType)
    {
    case IMAGE_REL_BASED_DIR64:
        // Write 64-bits into location
        *((UINT64 *) locationRW) = (UINT64) target;
        break;

#ifdef TARGET_AMD64
    case IMAGE_REL_BASED_REL32:
        {
            target = (BYTE *)target + addlDelta;

            INT32 * fixupLocation = (INT32 *) location;
            INT32 * fixupLocationRW = (INT32 *) locationRW;
            BYTE * baseAddr = (BYTE *)fixupLocation + sizeof(INT32);

            delta  = (INT64)((BYTE *)target - baseAddr);

            //
            // Do we need to insert a jump stub to make the source reach the target?
            //
            // Note that we cannot stress insertion of jump stub by inserting it unconditionally. JIT records the relocations
            // for intra-module jumps and calls. It does not expect the register used by the jump stub to be trashed.
            //
            if (!FitsInI4(delta))
            {
                if (m_fAllowRel32)
                {
                    //
                    // When m_fAllowRel32 == TRUE, the JIT will use REL32s for both data addresses and direct code targets.
                    // Since we cannot tell what the relocation is for, we have to defensively retry.
                    //
                    m_fJumpStubOverflow = TRUE;
                    delta = 0;
                }
                else
                {
                    //
                    // When m_fAllowRel32 == FALSE, the JIT will use a REL32s for direct code targets only.
                    // Use jump stub.
                    //
                    delta = rel32UsingJumpStub(fixupLocation, (PCODE)target, m_pMethodBeingCompiled, NULL, false /* throwOnOutOfMemoryWithinRange */);
                    if (delta == 0)
                    {
                        // This forces the JIT to retry the method, which allows us to reserve more space for jump stubs and have a higher chance that
                        // we will find space for them.
                        m_fJumpStubOverflow = TRUE;
                    }

                    // Keep track of conservative estimate of how much memory may be needed by jump stubs. We will use it to reserve extra memory
                    // on retry to increase chances that the retry succeeds.
                    m_reserveForJumpStubs = max((size_t)0x400, m_reserveForJumpStubs + 0x10);
                }
            }

            LOG((LF_JIT, LL_INFO100000, "Encoded a PCREL32 at" FMT_ADDR "to" FMT_ADDR "+%d,  delta is 0x%04x\n",
                 DBG_ADDR(fixupLocation), DBG_ADDR(target), addlDelta, delta));

            // Write the 32-bits pc-relative delta into location
            *fixupLocationRW = (INT32) delta;
        }
        break;
#endif // TARGET_AMD64

#ifdef TARGET_ARM64
    case IMAGE_REL_ARM64_BRANCH26:   // 26 bit offset << 2 & sign ext, for B and BL
        {
            _ASSERTE(addlDelta == 0);

            PCODE branchTarget  = (PCODE) target;
            _ASSERTE((branchTarget & 0x3) == 0);   // the low two bits must be zero

            PCODE fixupLocation = (PCODE) location;
            PCODE fixupLocationRW = (PCODE) locationRW;
            _ASSERTE((fixupLocation & 0x3) == 0);  // the low two bits must be zero

            delta = (INT64)(branchTarget - fixupLocation);
            _ASSERTE((delta & 0x3) == 0);          // the low two bits must be zero

            UINT32 branchInstr = *((UINT32*) fixupLocationRW);
            branchInstr &= 0xFC000000;  // keep bits 31-26
            _ASSERTE((branchInstr & 0x7FFFFFFF) == 0x14000000);  // Must be B or BL

            //
            // Do we need to insert a jump stub to make the source reach the target?
            //
            //
            if (!FitsInRel28(delta))
            {
                // Use jump stub.
                //
                TADDR baseAddr = (TADDR)fixupLocation;
                TADDR loAddr   = baseAddr - 0x08000000;   // -2^27
                TADDR hiAddr   = baseAddr + 0x07FFFFFF;   // +2^27-1

                // Check for the wrap around cases
                if (loAddr > baseAddr)
                    loAddr = UINT64_MIN; // overflow
                if (hiAddr < baseAddr)
                    hiAddr = UINT64_MAX; // overflow

                PCODE jumpStubAddr = ExecutionManager::jumpStub(m_pMethodBeingCompiled,
                                                                (PCODE)  target,
                                                                (BYTE *) loAddr,
                                                                (BYTE *) hiAddr,
                                                                NULL,
                                                                false);

                // Keep track of conservative estimate of how much memory may be needed by jump stubs. We will use it to reserve extra memory
                // on retry to increase chances that the retry succeeds.
                m_reserveForJumpStubs = max((size_t)0x400, m_reserveForJumpStubs + 2*BACK_TO_BACK_JUMP_ALLOCATE_SIZE);

                if (jumpStubAddr == 0)
                {
                    // This forces the JIT to retry the method, which allows us to reserve more space for jump stubs and have a higher chance that
                    // we will find space for them.
                    m_fJumpStubOverflow = TRUE;
                    break;
                }

                delta = (INT64)(jumpStubAddr - fixupLocation);

                if (!FitsInRel28(delta))
                {
                    _ASSERTE(!"jump stub was not in expected range");
                    EEPOLICY_HANDLE_FATAL_ERROR(COR_E_EXECUTIONENGINE);
                }

                LOG((LF_JIT, LL_INFO100000, "Using JumpStub at" FMT_ADDR "that jumps to" FMT_ADDR "\n",
                     DBG_ADDR(jumpStubAddr), DBG_ADDR(target)));
            }

            LOG((LF_JIT, LL_INFO100000, "Encoded a BRANCH26 at" FMT_ADDR "to" FMT_ADDR ",  delta is 0x%04x\n",
                 DBG_ADDR(fixupLocation), DBG_ADDR(target), delta));

            _ASSERTE(FitsInRel28(delta));

            PutArm64Rel28((UINT32*) fixupLocationRW, (INT32)delta);
        }
        break;

    case IMAGE_REL_ARM64_PAGEBASE_REL21:
        {
            _ASSERTE(addlDelta == 0);

            // Write the 21 bits pc-relative page address into location.
            INT64 targetPage = (INT64)target & 0xFFFFFFFFFFFFF000LL;
            INT64 locationPage = (INT64)location & 0xFFFFFFFFFFFFF000LL;
            INT64 relPage = (INT64)(targetPage - locationPage);
            INT32 imm21 = (INT32)(relPage >> 12) & 0x1FFFFF;
            PutArm64Rel21((UINT32 *)locationRW, imm21);
        }
        break;

    case IMAGE_REL_ARM64_PAGEOFFSET_12A:
        {
            _ASSERTE(addlDelta == 0);

            // Write the 12 bits page offset into location.
            INT32 imm12 = (INT32)(SIZE_T)target & 0xFFFLL;
            PutArm64Rel12((UINT32 *)locationRW, imm12);
        }
        break;

#endif // TARGET_ARM64

    default:
        _ASSERTE(!"Unknown reloc type");
        break;
    }

    EE_TO_JIT_TRANSITION();
#else // HOST_64BIT
    JIT_TO_EE_TRANSITION_LEAF();

    // Nothing to do on 32-bit

    EE_TO_JIT_TRANSITION_LEAF();
#endif // HOST_64BIT
}

// Get a hint for whether the relocation kind to use for the target address.
// Note that this is currently a best-guess effort as we do not know exactly
// where the jitted code will end up at. Instead we try to keep executable code
// and static fields in a preferred memory region and base the decision on this
// region.
//
// If we guess wrong we will recover in recordRelocation if we notice that we
// cannot actually use the kind of reloc: in that case we will rejit the
// function and turn off the use of those relocs in the future. This scheme
// works based on two assumptions:
//
// 1) The JIT will ask about relocs only for memory that was allocated by the
//    loader heap in the preferred region.
// 2) The loader heap allocates memory in the preferred region in a circular fashion;
//    the region itself might be larger than 2 GB, but the current compilation should
//    only be hitting the preferred region within 2 GB.
//
// Under these assumptions we should only hit the "recovery" case once the
// preferred range is actually full.
WORD CEEJitInfo::getRelocTypeHint(void * target)
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    } CONTRACTL_END;

#ifdef TARGET_AMD64
    if (m_fAllowRel32)
    {
        if (ExecutableAllocator::IsPreferredExecutableRange(target))
            return IMAGE_REL_BASED_REL32;
    }
#endif // TARGET_AMD64

    // No hints
    return (WORD)-1;
}

uint32_t CEEJitInfo::getExpectedTargetArchitecture()
{
    LIMITED_METHOD_CONTRACT;

    return IMAGE_FILE_MACHINE_NATIVE;
}

void CEEInfo::JitProcessShutdownWork()
{
    LIMITED_METHOD_CONTRACT;

    EEJitManager* jitMgr = ExecutionManager::GetEEJitManager();

    // If we didn't load the JIT, there is no work to do.
    if (jitMgr->m_jit != NULL)
    {
        // Do the shutdown work.
        jitMgr->m_jit->ProcessShutdownWork(this);
    }

#ifdef ALLOW_SXS_JIT
    if (jitMgr->m_alternateJit != NULL)
    {
        jitMgr->m_alternateJit->ProcessShutdownWork(this);
    }
#endif // ALLOW_SXS_JIT
}

/*********************************************************************/
InfoAccessType CEEJitInfo::constructStringLiteral(CORINFO_MODULE_HANDLE scopeHnd,
                                                  mdToken metaTok,
                                                  void **ppValue)
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    } CONTRACTL_END;

    InfoAccessType result = IAT_PVALUE;

    JIT_TO_EE_TRANSITION();

    _ASSERTE(ppValue != NULL);

    if (IsDynamicScope(scopeHnd))
    {
        *ppValue = (LPVOID)GetDynamicResolver(scopeHnd)->ConstructStringLiteral(metaTok);
    }
    else
    {
        // If ConstructStringLiteral returns a pinned reference we can return it by value (IAT_VALUE)
        void* ppPinnedString = nullptr;
        void** ptr = (void**)ConstructStringLiteral(scopeHnd, metaTok, &ppPinnedString);

        if (ppPinnedString != nullptr)
        {
            *ppValue = ppPinnedString;
            result = IAT_VALUE;
        }
        else
        {
            *ppValue = (void*)ptr;
        }
    }

    EE_TO_JIT_TRANSITION();

    return result;
}

/*********************************************************************/
InfoAccessType CEEJitInfo::emptyStringLiteral(void ** ppValue)
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    } CONTRACTL_END;

    InfoAccessType result = IAT_PVALUE;

    JIT_TO_EE_TRANSITION();
    void* pinnedStr = nullptr;
    void* pinnedStrHandlePtr = StringObject::GetEmptyStringRefPtr(&pinnedStr);

    if (pinnedStr != nullptr)
    {
        *ppValue = pinnedStr;
        result = IAT_VALUE;
    }
    else
    {
        *ppValue = pinnedStrHandlePtr;
    }

    EE_TO_JIT_TRANSITION();

    return result;
}

bool CEEInfo::getStaticObjRefContent(OBJECTREF obj, uint8_t* buffer, bool ignoreMovableObjects)
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    } CONTRACTL_END;


    if (obj == NULL)
    {
        // GC handle is null
        memset(buffer, 0, sizeof(CORINFO_OBJECT_HANDLE));
        return true;
    }
    else if (!ignoreMovableObjects || GCHeapUtilities::GetGCHeap()->IsInFrozenSegment(OBJECTREFToObject(obj)))
    {
        CORINFO_OBJECT_HANDLE handle = getJitHandleForObject(obj);
        memcpy(buffer, &handle, sizeof(CORINFO_OBJECT_HANDLE));
        return true;
    }
    return false;
}

bool CEEInfo::getStaticFieldContent(CORINFO_FIELD_HANDLE fieldHnd, uint8_t* buffer, int bufferSize, int valueOffset, bool ignoreMovableObjects)
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    } CONTRACTL_END;

    _ASSERT(fieldHnd != NULL);
    _ASSERT(buffer != NULL);
    _ASSERT(bufferSize > 0);
    _ASSERT(valueOffset >= 0);

    bool result = false;

    JIT_TO_EE_TRANSITION();

    FieldDesc* field = (FieldDesc*)fieldHnd;
    _ASSERTE(field->IsStatic());

    MethodTable* pEnclosingMT = field->GetEnclosingMethodTable();
    _ASSERTE(!pEnclosingMT->IsSharedByGenericInstantiations());
    _ASSERTE(!pEnclosingMT->ContainsGenericVariables());

    // Allocate space for the local class if necessary, but don't trigger
    // class construction.
    DomainLocalModule* pLocalModule = pEnclosingMT->GetDomainLocalModule();
    pLocalModule->PopulateClass(pEnclosingMT);

    if (!field->IsThreadStatic() && pEnclosingMT->IsClassInited() && IsFdInitOnly(field->GetAttributes()))
    {
        if (field->IsObjRef())
        {
            GCX_COOP();

            _ASSERT(!field->IsRVA());
            _ASSERT(valueOffset == 0); // there is no point in returning a chunk of a gc handle
            _ASSERT((UINT)bufferSize == field->GetSize());

            result = getStaticObjRefContent(field->GetStaticOBJECTREF(), buffer, ignoreMovableObjects);
        }
        else
        {
            UINT size = field->GetSize();
            _ASSERTE(size > 0);

            if (size >= (UINT)bufferSize && valueOffset >= 0 && (UINT)valueOffset <= size - (UINT)bufferSize)
            {
                bool useMemcpy = false;

                // For structs containing GC pointers we want to make sure those GC pointers belong to FOH
                // so we expect valueOffset to be a real field offset (same for bufferSize)
                if (!field->IsRVA() && field->GetFieldType() == ELEMENT_TYPE_VALUETYPE)
                {
                    TypeHandle structType = field->GetFieldTypeHandleThrowing();
                    PTR_MethodTable structTypeMT = structType.AsMethodTable();
                    if (!structTypeMT->ContainsPointers())
                    {
                        // Fast-path: no GC pointers in the struct, we can use memcpy
                        useMemcpy = true;
                    }
                    else
                    {
                        // The struct contains GC pointer(s), but we still can use memcpy if we don't intersect with them.
                        unsigned numSlots = (structType.GetSize() + TARGET_POINTER_SIZE - 1) / TARGET_POINTER_SIZE;
                        CQuickBytes gcPtrs;
                        BYTE* ptr = static_cast<BYTE*>(gcPtrs.AllocThrows(numSlots));
                        CEEInfo::getClassGClayoutStatic(structType, ptr);

                        _ASSERT(numSlots > 0);

                        useMemcpy = true;
                        for (unsigned i = 0; i < numSlots; i++)
                        {
                            if (ptr[i] == TYPE_GC_NONE)
                            {
                                // Not a GC slot
                                continue;
                            }

                            const unsigned gcSlotBegin = i * TARGET_POINTER_SIZE;
                            const unsigned gcSlotEnd = gcSlotBegin + TARGET_POINTER_SIZE;

                            if (gcSlotBegin >= (unsigned)valueOffset && gcSlotEnd <= (unsigned)(valueOffset + bufferSize))
                            {
                                // GC slot intersects with our valueOffset + bufferSize - we can't use memcpy...
                                useMemcpy = false;

                                // ...unless we're interested in that GC slot's value itself
                                if (gcSlotBegin == (unsigned)valueOffset && gcSlotEnd == (unsigned)(valueOffset + bufferSize) && ptr[i] == TYPE_GC_REF)
                                {
                                    GCX_COOP();

                                    size_t baseAddr = (size_t)field->GetCurrentStaticAddress();

                                    _ASSERT((UINT)bufferSize == sizeof(CORINFO_OBJECT_HANDLE));
                                    result = getStaticObjRefContent(ObjectToOBJECTREF(*(Object**)((uint8_t*)baseAddr + gcSlotBegin)), buffer, ignoreMovableObjects);
                                }

                                // We had an intersection with a gc slot - no point in looking futher.
                                break;
                            }
                        }
                    }
                }
                else
                {
                    // RVA data, no gc pointers
                    useMemcpy = true;
                }

                if (useMemcpy)
                {
                    _ASSERT(!result);
                    result = true;
                    GCX_COOP();
                    size_t baseAddr = (size_t)field->GetCurrentStaticAddress();
                    _ASSERT(baseAddr != 0);
                    memcpy(buffer, (uint8_t*)baseAddr + valueOffset, bufferSize);
                }
            }
        }
    }

    EE_TO_JIT_TRANSITION();

    return result;
}

bool CEEInfo::getObjectContent(CORINFO_OBJECT_HANDLE handle, uint8_t* buffer, int bufferSize, int valueOffset)
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    } CONTRACTL_END;

    _ASSERT(handle != NULL);
    _ASSERT(buffer != NULL);
    _ASSERT(bufferSize > 0);
    _ASSERT(valueOffset >= 0);

    bool result = false;

    JIT_TO_EE_TRANSITION();

    GCX_COOP();
    OBJECTREF objRef = getObjectFromJitHandle(handle);
    _ASSERTE(objRef != NULL);

    // TODO: support types containing GC pointers
    if (bufferSize + valueOffset <= (int)objRef->GetSize())
    {
        Object* obj = OBJECTREFToObject(objRef);
        PTR_MethodTable type = obj->GetMethodTable();
        if (type->ContainsPointers())
        {
            // RuntimeType has a gc field (object m_keepAlive), but if the object is in a frozen segment
            // it means that field is always nullptr so we can read any part of the object:
            if (type == g_pRuntimeTypeClass && GCHeapUtilities::GetGCHeap()->IsInFrozenSegment(obj))
            {
                memcpy(buffer, (uint8_t*)obj + valueOffset, bufferSize);
                result = true;
            }
        }
        else
        {
            memcpy(buffer, (uint8_t*)obj + valueOffset, bufferSize);
            result = true;
        }
    }

    EE_TO_JIT_TRANSITION();

    return result;
}

/*********************************************************************/
CORINFO_CLASS_HANDLE CEEJitInfo::getStaticFieldCurrentClass(CORINFO_FIELD_HANDLE fieldHnd,
                                                            bool* pIsSpeculative)
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    } CONTRACTL_END;

    CORINFO_CLASS_HANDLE result = NULL;

    if (pIsSpeculative != NULL)
    {
        *pIsSpeculative = true;
    }

    JIT_TO_EE_TRANSITION();

    FieldDesc* field = (FieldDesc*) fieldHnd;
    bool isClassInitialized = false;

    // We're only interested in ref class typed static fields
    // where the field handle specifies a unique location.
    if (field->IsStatic() && field->IsObjRef() && !field->IsThreadStatic())
    {
        MethodTable* pEnclosingMT = field->GetEnclosingMethodTable();

        if (!pEnclosingMT->IsSharedByGenericInstantiations())
        {
            // Allocate space for the local class if necessary, but don't trigger
            // class construction.
            DomainLocalModule *pLocalModule = pEnclosingMT->GetDomainLocalModule();
            pLocalModule->PopulateClass(pEnclosingMT);

            GCX_COOP();

            OBJECTREF fieldObj = field->GetStaticOBJECTREF();
            VALIDATEOBJECTREF(fieldObj);

            // Check for initialization before looking at the value
            isClassInitialized = !!pEnclosingMT->IsClassInited();

            if (fieldObj != NULL)
            {
                MethodTable *pObjMT = fieldObj->GetMethodTable();

                // TODO: Check if the jit is allowed to embed this handle in jitted code.
                // Note for the initonly cases it probably won't embed.
                result = (CORINFO_CLASS_HANDLE) pObjMT;
            }
        }
    }

    // Did we find a class?
    if (result != NULL)
    {
        // Figure out what to report back.
        bool isResultImmutable = isClassInitialized && IsFdInitOnly(field->GetAttributes());

        if (pIsSpeculative != NULL)
        {
            // Caller is ok with potentially mutable results.
            *pIsSpeculative = !isResultImmutable;
        }
        else
        {
            // Caller only wants to see immutable results.
            if (!isResultImmutable)
            {
                result = NULL;
            }
        }
    }

    EE_TO_JIT_TRANSITION();

    return result;
}

/*********************************************************************/
static void *GetClassSync(MethodTable *pMT)
{
    STANDARD_VM_CONTRACT;

    GCX_COOP();

    OBJECTREF ref = pMT->GetManagedClassObject();
    return (void*)ref->GetSyncBlock()->GetMonitor();
}

/*********************************************************************/
void* CEEJitInfo::getMethodSync(CORINFO_METHOD_HANDLE ftnHnd,
                                void **ppIndirection)
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    } CONTRACTL_END;

    void * result = NULL;

    if (ppIndirection != NULL)
        *ppIndirection = NULL;

    JIT_TO_EE_TRANSITION();

    result = GetClassSync((GetMethod(ftnHnd))->GetMethodTable());

    EE_TO_JIT_TRANSITION();

    return result;
}

/*********************************************************************/
HRESULT CEEJitInfo::allocPgoInstrumentationBySchema(
            CORINFO_METHOD_HANDLE ftnHnd, /* IN */
            PgoInstrumentationSchema* pSchema, /* IN/OUT */
            uint32_t countSchemaItems, /* IN */
            uint8_t** pInstrumentationData /* OUT */
            )
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    } CONTRACTL_END;

    HRESULT hr = E_FAIL;

    JIT_TO_EE_TRANSITION();

    // We need to know the code size. Typically we can get the code size
    // from m_ILHeader. For dynamic methods, m_ILHeader will be NULL, so
    // for that case we need to use DynamicResolver to get the code size.

    unsigned codeSize = 0;
    if (m_pMethodBeingCompiled->IsDynamicMethod())
    {
        unsigned stackSize, ehSize;
        CorInfoOptions options;
        DynamicResolver * pResolver = m_pMethodBeingCompiled->AsDynamicMethodDesc()->GetResolver();
        pResolver->GetCodeInfo(&codeSize, &stackSize, &options, &ehSize);
    }
    else
    {
        codeSize = m_ILHeader->GetCodeSize();
    }

#ifdef FEATURE_PGO
    hr = PgoManager::allocPgoInstrumentationBySchema(m_pMethodBeingCompiled, pSchema, countSchemaItems, pInstrumentationData);
#else
    _ASSERTE(!"allocMethodBlockCounts not implemented on CEEJitInfo!");
    hr = E_NOTIMPL;
#endif // !FEATURE_PGO

    EE_TO_JIT_TRANSITION();

    return hr;
}

// Consider implementing getBBProfileData on CEEJitInfo.  This will allow us
// to use profile info in codegen for non zapped images.
HRESULT CEEJitInfo::getPgoInstrumentationResults(
            CORINFO_METHOD_HANDLE      ftnHnd,
            PgoInstrumentationSchema **pSchema,                    // pointer to the schema table which describes the instrumentation results (pointer will not remain valid after jit completes)
            uint32_t *                 pCountSchemaItems,          // pointer to the count schema items
            uint8_t **                 pInstrumentationData,       // pointer to the actual instrumentation data (pointer will not remain valid after jit completes)
            PgoSource *                pPgoSource                  // source of pgo data
            )
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    } CONTRACTL_END;

    HRESULT hr = E_FAIL;
    *pCountSchemaItems = 0;
    *pInstrumentationData = NULL;
    *pPgoSource = PgoSource::Unknown;

    JIT_TO_EE_TRANSITION();

#ifdef FEATURE_PGO

    MethodDesc* pMD = (MethodDesc*)ftnHnd;
    ComputedPgoData* pDataCur = m_foundPgoData;

    // Search linked list of previously found pgo information
    for (; pDataCur != nullptr; pDataCur = pDataCur->m_next)
    {
        if (pDataCur->m_pMD == pMD)
        {
            break;
        }
    }

    if (pDataCur == nullptr)
    {
        // If not found in previous list, gather it here, and add to linked list
        NewHolder<ComputedPgoData> newPgoData = new ComputedPgoData(pMD);
        newPgoData->m_next = m_foundPgoData;
        m_foundPgoData = newPgoData;
        newPgoData.SuppressRelease();

        newPgoData->m_hr = PgoManager::getPgoInstrumentationResults(pMD, &newPgoData->m_allocatedData, &newPgoData->m_schema,
            &newPgoData->m_cSchemaElems, &newPgoData->m_pInstrumentationData, &newPgoData->m_pgoSource);
        pDataCur = m_foundPgoData;
    }

    *pSchema = pDataCur->m_schema;
    *pCountSchemaItems = pDataCur->m_cSchemaElems;
    *pInstrumentationData = pDataCur->m_pInstrumentationData;
    *pPgoSource = pDataCur->m_pgoSource;
    hr = pDataCur->m_hr;
#else
    _ASSERTE(!"getPgoInstrumentationResults not implemented on CEEJitInfo!");
    hr = E_NOTIMPL;
#endif

    EE_TO_JIT_TRANSITION();

    return hr;
}

void CEEJitInfo::allocMem (AllocMemArgs *pArgs)
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    } CONTRACTL_END;

    JIT_TO_EE_TRANSITION();

    _ASSERTE(pArgs->coldCodeSize == 0);
    if (pArgs->coldCodeBlock)
    {
        pArgs->coldCodeBlock = NULL;
    }

    ULONG codeSize      = pArgs->hotCodeSize;
    void **codeBlock    = &pArgs->hotCodeBlock;
    void **codeBlockRW  = &pArgs->hotCodeBlockRW;

    S_SIZE_T totalSize = S_SIZE_T(codeSize);

    size_t roDataAlignment = sizeof(void*);
    if ((pArgs->flag & CORJIT_ALLOCMEM_FLG_RODATA_64BYTE_ALIGN)!= 0)
    {
        roDataAlignment = 64;
    }
    else if ((pArgs->flag & CORJIT_ALLOCMEM_FLG_RODATA_32BYTE_ALIGN)!= 0)
    {
        roDataAlignment = 32;
    }
    else if ((pArgs->flag & CORJIT_ALLOCMEM_FLG_RODATA_16BYTE_ALIGN)!= 0)
    {
        roDataAlignment = 16;
    }
    else if (pArgs->roDataSize >= 8)
    {
        roDataAlignment = 8;
    }
    if (pArgs->roDataSize > 0)
    {
        size_t codeAlignment = sizeof(void*);

        if ((pArgs->flag & CORJIT_ALLOCMEM_FLG_32BYTE_ALIGN) != 0)
        {
            codeAlignment = 32;
        }
        else if ((pArgs->flag & CORJIT_ALLOCMEM_FLG_16BYTE_ALIGN) != 0)
        {
            codeAlignment = 16;
        }
        totalSize.AlignUp(codeAlignment);

        if (roDataAlignment > codeAlignment) {
            // Add padding to align read-only data.
            totalSize += (roDataAlignment - codeAlignment);
        }
        totalSize += pArgs->roDataSize;
    }

#ifdef FEATURE_EH_FUNCLETS
    totalSize.AlignUp(sizeof(DWORD));
    totalSize += m_totalUnwindSize;
#endif

    _ASSERTE(m_CodeHeader == 0 &&
            // The jit-compiler sometimes tries to compile a method a second time
            // if it failed the first time. In such a situation, m_CodeHeader may
            // have already been assigned. Its OK to ignore this assert in such a
            // situation - we will leak some memory, but that is acceptable
            // since this should happen very rarely.
            "Note that this may fire if the JITCompiler tries to recompile a method");

    if( totalSize.IsOverflow() )
    {
        COMPlusThrowHR(CORJIT_OUTOFMEM);
    }

    if (ETW_EVENT_ENABLED(MICROSOFT_WINDOWS_DOTNETRUNTIME_PROVIDER_DOTNET_Context, MethodJitMemoryAllocatedForCode))
    {
        ULONGLONG ullMethodIdentifier = 0;
        ULONGLONG ullModuleID = 0;

        if (m_pMethodBeingCompiled)
        {
            Module* pModule = m_pMethodBeingCompiled->GetModule();
            ullModuleID = (ULONGLONG)(TADDR)pModule;
            ullMethodIdentifier = (ULONGLONG)m_pMethodBeingCompiled;
        }

        FireEtwMethodJitMemoryAllocatedForCode(ullMethodIdentifier, ullModuleID,
            pArgs->hotCodeSize + pArgs->coldCodeSize, pArgs->roDataSize, totalSize.Value(), pArgs->flag, GetClrInstanceId());
    }

    m_jitManager->allocCode(m_pMethodBeingCompiled, totalSize.Value(), GetReserveForJumpStubs(), pArgs->flag, &m_CodeHeader, &m_CodeHeaderRW, &m_codeWriteBufferSize, &m_pCodeHeap
#ifdef USE_INDIRECT_CODEHEADER
                          , &m_pRealCodeHeader
#endif
#ifdef FEATURE_EH_FUNCLETS
                          , m_totalUnwindInfos
#endif
                          );

#ifdef FEATURE_EH_FUNCLETS
    m_moduleBase = m_pCodeHeap->GetModuleBase();
#endif

    BYTE* current = (BYTE *)m_CodeHeader->GetCodeStartAddress();
    size_t writeableOffset = (BYTE *)m_CodeHeaderRW - (BYTE *)m_CodeHeader;

    *codeBlock = current;
    *codeBlockRW = current + writeableOffset;
    current += codeSize;

    if (pArgs->roDataSize > 0)
    {
        current = (BYTE *)ALIGN_UP(current, roDataAlignment);
        pArgs->roDataBlock = current;
        pArgs->roDataBlockRW = current + writeableOffset;
        current += pArgs->roDataSize;
    }
    else
    {
        pArgs->roDataBlock = NULL;
        pArgs->roDataBlockRW = NULL;
    }

#ifdef FEATURE_EH_FUNCLETS
    current = (BYTE *)ALIGN_UP(current, sizeof(DWORD));

    m_theUnwindBlock = current;
    current += m_totalUnwindSize;
#endif

    _ASSERTE((SIZE_T)(current - (BYTE *)m_CodeHeader->GetCodeStartAddress()) <= totalSize.Value());

#ifdef _DEBUG
    m_codeSize = codeSize;
#endif  // _DEBUG

    EE_TO_JIT_TRANSITION();
}

/*********************************************************************/
void * CEEJitInfo::allocGCInfo (size_t size)
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    } CONTRACTL_END;

    void * block = NULL;

    JIT_TO_EE_TRANSITION();

    _ASSERTE(m_CodeHeaderRW != 0);
    _ASSERTE(m_CodeHeaderRW->GetGCInfo() == 0);

#ifdef HOST_64BIT
    if (size & 0xFFFFFFFF80000000LL)
    {
        COMPlusThrowHR(CORJIT_OUTOFMEM);
    }
#endif // HOST_64BIT

    block = m_jitManager->allocGCInfo(m_CodeHeaderRW,(DWORD)size, &m_GCinfo_len);
    if (!block)
    {
        COMPlusThrowHR(CORJIT_OUTOFMEM);
    }

    _ASSERTE(m_CodeHeaderRW->GetGCInfo() != 0 && block == m_CodeHeaderRW->GetGCInfo());

    EE_TO_JIT_TRANSITION();

    return block;
}

/*********************************************************************/
void CEEJitInfo::setEHcount (
        unsigned      cEH)
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    } CONTRACTL_END;

    JIT_TO_EE_TRANSITION();

    _ASSERTE(cEH != 0);
    _ASSERTE(m_CodeHeaderRW != 0);
    _ASSERTE(m_CodeHeaderRW->GetEHInfo() == 0);

    EE_ILEXCEPTION* ret;
    ret = m_jitManager->allocEHInfo(m_CodeHeaderRW,cEH, &m_EHinfo_len);
    _ASSERTE(ret);      // allocEHInfo throws if there's not enough memory

    _ASSERTE(m_CodeHeaderRW->GetEHInfo() != 0 && m_CodeHeaderRW->GetEHInfo()->EHCount() == cEH);

    EE_TO_JIT_TRANSITION();
}

/*********************************************************************/
void CEEJitInfo::setEHinfo (
        unsigned      EHnumber,
        const CORINFO_EH_CLAUSE* clause)
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    } CONTRACTL_END;

    JIT_TO_EE_TRANSITION();

    // <REVISIT_TODO> Fix make the Code Manager EH clauses EH_INFO+</REVISIT_TODO>
    _ASSERTE(m_CodeHeaderRW->GetEHInfo() != 0 && EHnumber < m_CodeHeaderRW->GetEHInfo()->EHCount());

    EE_ILEXCEPTION_CLAUSE* pEHClause = m_CodeHeaderRW->GetEHInfo()->EHClause(EHnumber);

    pEHClause->TryStartPC     = clause->TryOffset;
    pEHClause->TryEndPC       = clause->TryLength;
    pEHClause->HandlerStartPC = clause->HandlerOffset;
    pEHClause->HandlerEndPC   = clause->HandlerLength;
    pEHClause->ClassToken     = clause->ClassToken;
    pEHClause->Flags          = (CorExceptionFlag)clause->Flags;

    LOG((LF_EH, LL_INFO1000000, "Setting EH clause #%d for %s::%s\n", EHnumber, m_pMethodBeingCompiled->m_pszDebugClassName, m_pMethodBeingCompiled->m_pszDebugMethodName));
    LOG((LF_EH, LL_INFO1000000, "    Flags         : 0x%08lx  ->  0x%08lx\n",            clause->Flags,         pEHClause->Flags));
    LOG((LF_EH, LL_INFO1000000, "    TryOffset     : 0x%08lx  ->  0x%08lx (startpc)\n",  clause->TryOffset,     pEHClause->TryStartPC));
    LOG((LF_EH, LL_INFO1000000, "    TryLength     : 0x%08lx  ->  0x%08lx (endpc)\n",    clause->TryLength,     pEHClause->TryEndPC));
    LOG((LF_EH, LL_INFO1000000, "    HandlerOffset : 0x%08lx  ->  0x%08lx\n",            clause->HandlerOffset, pEHClause->HandlerStartPC));
    LOG((LF_EH, LL_INFO1000000, "    HandlerLength : 0x%08lx  ->  0x%08lx\n",            clause->HandlerLength, pEHClause->HandlerEndPC));
    LOG((LF_EH, LL_INFO1000000, "    ClassToken    : 0x%08lx  ->  0x%08lx\n",            clause->ClassToken,    pEHClause->ClassToken));
    LOG((LF_EH, LL_INFO1000000, "    FilterOffset  : 0x%08lx  ->  0x%08lx\n",            clause->FilterOffset,  pEHClause->FilterOffset));

    if (m_pMethodBeingCompiled->IsDynamicMethod() &&
        ((pEHClause->Flags & COR_ILEXCEPTION_CLAUSE_FILTER) == 0) &&
        (clause->ClassToken != NULL))
    {
        MethodDesc * pMD; FieldDesc * pFD;
        m_pMethodBeingCompiled->AsDynamicMethodDesc()->GetResolver()->ResolveToken(clause->ClassToken, (TypeHandle *)&pEHClause->TypeHandle, &pMD, &pFD);
        SetHasCachedTypeHandle(pEHClause);
        LOG((LF_EH, LL_INFO1000000, "  CachedTypeHandle: 0x%08lx  ->  0x%08lx\n",        clause->ClassToken,    pEHClause->TypeHandle));
    }

    EE_TO_JIT_TRANSITION();
}

/*********************************************************************/
// get individual exception handler
void CEEJitInfo::getEHinfo(
                              CORINFO_METHOD_HANDLE  ftn,      /* IN  */
                              unsigned               EHnumber, /* IN  */
                              CORINFO_EH_CLAUSE*     clause)   /* OUT */
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    } CONTRACTL_END;

    JIT_TO_EE_TRANSITION();

    if (IsDynamicMethodHandle(ftn))
    {
        GetMethod(ftn)->AsDynamicMethodDesc()->GetResolver()->GetEHInfo(EHnumber, clause);
    }
    else
    {
        _ASSERTE(ftn == CORINFO_METHOD_HANDLE(m_pMethodBeingCompiled));  // For now only support if the method being jitted
        getEHinfoHelper(ftn, EHnumber, clause, m_ILHeader);
    }

    EE_TO_JIT_TRANSITION();
}




#ifdef FEATURE_INTERPRETER
static CorJitResult CompileMethodWithEtwWrapper(EEJitManager *jitMgr,
                                                      CEEInfo *comp,
                                                      struct CORINFO_METHOD_INFO *info,
                                                      unsigned flags,
                                                      BYTE **nativeEntry,
                                                      ULONG *nativeSizeOfCode)
{
    STATIC_CONTRACT_THROWS;
    STATIC_CONTRACT_GC_TRIGGERS;
    STATIC_CONTRACT_MODE_PREEMPTIVE;

    SString namespaceOrClassName, methodName, methodSignature;
    // Fire an ETW event to mark the beginning of JIT'ing
    ETW::MethodLog::MethodJitting(reinterpret_cast<MethodDesc*>(info->ftn), NULL, &namespaceOrClassName, &methodName, &methodSignature);

    CorJitResult ret = jitMgr->m_jit->compileMethod(comp, info, flags, nativeEntry, nativeSizeOfCode);

    // Logically, it would seem that the end-of-JITting ETW even should go here, but it must come after the native code has been
    // set for the given method desc, which happens in a caller.

    return ret;
}
#endif // FEATURE_INTERPRETER

//
// Helper function because can't have dtors in BEGIN_SO_TOLERANT_CODE.
//
CorJitResult invokeCompileMethodHelper(EEJitManager *jitMgr,
                                 CEEInfo *comp,
                                 struct CORINFO_METHOD_INFO *info,
                                 CORJIT_FLAGS jitFlags,
                                 BYTE **nativeEntry,
                                 uint32_t *nativeSizeOfCode)
{
    STATIC_CONTRACT_THROWS;
    STATIC_CONTRACT_GC_TRIGGERS;
    STATIC_CONTRACT_MODE_PREEMPTIVE;

    CorJitResult ret = CORJIT_SKIPPED;   // Note that CORJIT_SKIPPED is an error exit status code

#if defined(ALLOW_SXS_JIT)
    if (FAILED(ret) && jitMgr->m_alternateJit)
    {
        CORJIT_FLAGS altJitFlags = jitFlags;
        altJitFlags.Set(CORJIT_FLAGS::CORJIT_FLAG_ALT_JIT);
        comp->setJitFlags(altJitFlags);
        ret = jitMgr->m_alternateJit->compileMethod( comp,
                                                     info,
                                                     CORJIT_FLAGS::CORJIT_FLAG_CALL_GETJITFLAGS,
                                                     nativeEntry,
                                                     nativeSizeOfCode);

        // If we failed to jit, then fall back to the primary Jit.
        if (FAILED(ret))
        {
            ((CEEJitInfo*)comp)->BackoutJitData(jitMgr);
            ((CEEJitInfo*)comp)->ResetForJitRetry();
            ret = CORJIT_SKIPPED;
        }
    }
#endif // defined(ALLOW_SXS_JIT)
    comp->setJitFlags(jitFlags);

#ifdef FEATURE_INTERPRETER
    static ConfigDWORD s_InterpreterFallback;
    static ConfigDWORD s_ForceInterpreter;

    bool isInterpreterStub   = false;
    bool interpreterFallback = (s_InterpreterFallback.val(CLRConfig::INTERNAL_InterpreterFallback) != 0);
    bool forceInterpreter    = (s_ForceInterpreter.val(CLRConfig::INTERNAL_ForceInterpreter) != 0);

    if (interpreterFallback == false)
    {
        // If we're doing an "import_only" compilation, it's for verification, so don't interpret.
        // (We assume that importation is completely architecture-independent, or at least nearly so.)
        if (FAILED(ret) &&
            (forceInterpreter || !jitFlags.IsSet(CORJIT_FLAGS::CORJIT_FLAG_MAKEFINALCODE)))
        {
            if (SUCCEEDED(ret = Interpreter::GenerateInterpreterStub(comp, info, nativeEntry, nativeSizeOfCode)))
            {
                isInterpreterStub = true;
            }
        }
    }

    if (FAILED(ret) && jitMgr->m_jit)
    {
        ret = CompileMethodWithEtwWrapper(jitMgr,
                                          comp,
                                          info,
                                          CORJIT_FLAGS::CORJIT_FLAG_CALL_GETJITFLAGS,
                                          nativeEntry,
                                          nativeSizeOfCode);
    }

    if (interpreterFallback == true)
    {
        // If we're doing an "import_only" compilation, it's for verification, so don't interpret.
        // (We assume that importation is completely architecture-independent, or at least nearly so.)
        if (FAILED(ret) &&
            (forceInterpreter || !jitFlags.IsSet(CORJIT_FLAGS::CORJIT_FLAG_MAKEFINALCODE)))
        {
            if (SUCCEEDED(ret = Interpreter::GenerateInterpreterStub(comp, info, nativeEntry, nativeSizeOfCode)))
            {
                isInterpreterStub = true;
            }
        }
    }
#else
    if (FAILED(ret))
    {
        ret = jitMgr->m_jit->compileMethod( comp,
                                            info,
                                            CORJIT_FLAGS::CORJIT_FLAG_CALL_GETJITFLAGS,
                                            nativeEntry,
                                            nativeSizeOfCode);
    }
#endif // FEATURE_INTERPRETER

    // Cleanup any internal data structures allocated
    // such as IL code after a successful JIT compile
    // If the JIT fails we keep the IL around and will
    // try reJIT the same IL.  VSW 525059
    //
    if (SUCCEEDED(ret) && !((CEEJitInfo*)comp)->JitAgain())
    {
        ((CEEJitInfo*)comp)->CompressDebugInfo();

#ifdef FEATURE_INTERPRETER
        // We do this cleanup in the prestub, where we know whether the method
        // has been interpreted.
#else
        comp->MethodCompileComplete(info->ftn);
#endif // FEATURE_INTERPRETER
    }


#if defined(FEATURE_GDBJIT)
    bool isJittedEntry = SUCCEEDED(ret) && *nativeEntry != NULL;

#ifdef FEATURE_INTERPRETER
    isJittedEntry &= !isInterpreterStub;
#endif // FEATURE_INTERPRETER

    if (isJittedEntry)
    {
        CodeHeader* pCH = ((CodeHeader*)((PCODE)*nativeEntry & ~1)) - 1;
        pCH->SetCalledMethods((PTR_VOID)comp->GetCalledMethods());
    }
#endif

    return ret;
}


/*********************************************************************/
CorJitResult invokeCompileMethod(EEJitManager *jitMgr,
                                 CEEInfo *comp,
                                 struct CORINFO_METHOD_INFO *info,
                                 CORJIT_FLAGS jitFlags,
                                 BYTE **nativeEntry,
                                 uint32_t *nativeSizeOfCode)
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    } CONTRACTL_END;
    //
    // The JIT runs in preemptive mode
    //

    GCX_PREEMP();

    CorJitResult ret = invokeCompileMethodHelper(jitMgr, comp, info, jitFlags, nativeEntry, nativeSizeOfCode);

    //
    // Verify that we are still in preemptive mode when we return
    // from the JIT
    //

    _ASSERTE(GetThread()->PreemptiveGCDisabled() == FALSE);

    return ret;
}

/*********************************************************************/
// Figures out the compile flags that are used by both JIT and NGen

/* static */ CORJIT_FLAGS CEEInfo::GetBaseCompileFlags(MethodDesc * ftn)
{
     CONTRACTL {
        THROWS;
        GC_TRIGGERS;
    } CONTRACTL_END;

    //
    // Figure out the code quality flags
    //

    CORJIT_FLAGS flags;
    if (g_pConfig->JitFramed())
        flags.Set(CORJIT_FLAGS::CORJIT_FLAG_FRAMED);

    // Set flags based on method's ImplFlags.
    if (!ftn->IsNoMetadata())
    {
         DWORD dwImplFlags = 0;
         IfFailThrow(ftn->GetMDImport()->GetMethodImplProps(ftn->GetMemberDef(), NULL, &dwImplFlags));

         if (IsMiNoOptimization(dwImplFlags))
         {
             flags.Set(CORJIT_FLAGS::CORJIT_FLAG_MIN_OPT);
         }

         // Always emit frames for methods marked no-inline (see #define ETW_EBP_FRAMED in the JIT)
         if (IsMiNoInlining(dwImplFlags))
         {
             flags.Set(CORJIT_FLAGS::CORJIT_FLAG_FRAMED);
         }
    }

    if (ftn->HasUnmanagedCallersOnlyAttribute())
    {
        // If the stub was generated by the runtime, don't validate
        // it for UnmanagedCallersOnlyAttribute usage. There are cases
        // where the validation doesn't handle all of the cases we can
        // permit during stub generation (e.g. Vector2 returns).
        if (!ftn->IsILStub())
            COMDelegate::ThrowIfInvalidUnmanagedCallersOnlyUsage(ftn);

        flags.Set(CORJIT_FLAGS::CORJIT_FLAG_REVERSE_PINVOKE);

        // If we're a reverse IL stub, we need to use the TrackTransitions variant
        // so we have the target MethodDesc entrypoint to tell the debugger about.
        if (CORProfilerTrackTransitions() || ftn->IsILStub())
        {
            flags.Set(CORJIT_FLAGS::CORJIT_FLAG_TRACK_TRANSITIONS);
        }
    }

    return flags;
}

/*********************************************************************/
// Figures out (some of) the flags to use to compile the method
// Returns the new set to use

CORJIT_FLAGS GetDebuggerCompileFlags(Module* pModule, CORJIT_FLAGS flags)
{
    STANDARD_VM_CONTRACT;

    //Right now if we don't have a debug interface on CoreCLR, we can't generate debug info.  So, in those
    //cases don't attempt it.
    if (!g_pDebugInterface)
        return flags;

#ifdef DEBUGGING_SUPPORTED

    if (g_pConfig->GenDebuggableCode())
    {
        flags.Set(CORJIT_FLAGS::CORJIT_FLAG_DEBUG_CODE);
    }

#ifdef FEATURE_METADATA_UPDATER
    if (pModule->IsEditAndContinueEnabled())
    {
        flags.Set(CORJIT_FLAGS::CORJIT_FLAG_DEBUG_EnC);
    }
#endif // FEATURE_METADATA_UPDATER

    // Debug info is always tracked
    flags.Set(CORJIT_FLAGS::CORJIT_FLAG_DEBUG_INFO);
#endif // DEBUGGING_SUPPORTED

    if (CORDisableJITOptimizations(pModule->GetDebuggerInfoBits()))
    {
        flags.Set(CORJIT_FLAGS::CORJIT_FLAG_DEBUG_CODE);
    }

    return flags;
}

static CORJIT_FLAGS GetCompileFlags(PrepareCodeConfig* prepareConfig, MethodDesc* ftn, CORINFO_METHOD_INFO* methodInfo)
{
    STANDARD_VM_CONTRACT;
    _ASSERTE(prepareConfig != NULL);
    _ASSERTE(ftn != NULL);
    _ASSERTE(methodInfo->regionKind ==  CORINFO_REGION_JIT);

    CORJIT_FLAGS flags = prepareConfig->GetJitCompilationFlags();

    //
    // Get the compile flags that are shared between JIT and NGen
    //
    flags.Add(CEEInfo::GetBaseCompileFlags(ftn));

    //
    // Get CPU specific flags
    //
    flags.Add(ExecutionManager::GetEEJitManager()->GetCPUCompileFlags());

    //
    // Find the debugger and profiler related flags
    //

#ifdef DEBUGGING_SUPPORTED
    flags.Add(GetDebuggerCompileFlags(ftn->GetModule(), flags));
#endif

#ifdef PROFILING_SUPPORTED
    if (CORProfilerTrackEnterLeave() && !ftn->IsNoMetadata())
        flags.Set(CORJIT_FLAGS::CORJIT_FLAG_PROF_ENTERLEAVE);

    if (CORProfilerTrackTransitions())
        flags.Set(CORJIT_FLAGS::CORJIT_FLAG_PROF_NO_PINVOKE_INLINE);
#endif // PROFILING_SUPPORTED

    // Don't allow allocations on FOH from collectible contexts to avoid memory leaks
    if (!ftn->GetLoaderAllocator()->CanUnload())
    {
        flags.Set(CORJIT_FLAGS::CORJIT_FLAG_FROZEN_ALLOC_ALLOWED);
    }

    // Set optimization flags
    if (!flags.IsSet(CORJIT_FLAGS::CORJIT_FLAG_MIN_OPT))
    {
        unsigned optType = g_pConfig->GenOptimizeType();
        _ASSERTE(optType <= OPT_RANDOM);

        if (optType == OPT_RANDOM)
            optType = methodInfo->ILCodeSize % OPT_RANDOM;

        if (g_pConfig->JitMinOpts())
        {
            flags.Set(CORJIT_FLAGS::CORJIT_FLAG_MIN_OPT);
        }
        else
        {
            if (!flags.IsSet(CORJIT_FLAGS::CORJIT_FLAG_TIER0))
                flags.Set(CORJIT_FLAGS::CORJIT_FLAG_BBOPT);
        }

        if (optType == OPT_SIZE)
        {
            flags.Set(CORJIT_FLAGS::CORJIT_FLAG_SIZE_OPT);
        }
        else if (optType == OPT_SPEED)
        {
            flags.Set(CORJIT_FLAGS::CORJIT_FLAG_SPEED_OPT);
        }
    }

    if (IsDynamicScope(methodInfo->scope))
    {
        // no debug info available for IL stubs
        if (!g_pConfig->GetTrackDynamicMethodDebugInfo())
            flags.Clear(CORJIT_FLAGS::CORJIT_FLAG_DEBUG_INFO);

        DynamicResolver* pResolver = GetDynamicResolver(methodInfo->scope);
        flags.Add(pResolver->GetJitFlags());
    }

#ifdef ARM_SOFTFP
    flags.Set(CORJIT_FLAGS::CORJIT_FLAG_SOFTFP_ABI);
#endif // ARM_SOFTFP

#ifdef FEATURE_PGO

    if (CLRConfig::GetConfigValue(CLRConfig::INTERNAL_ReadPGOData) > 0)
    {
        flags.Set(CORJIT_FLAGS::CORJIT_FLAG_BBOPT);
    }
    else if (g_pConfig->TieredPGO() && flags.IsSet(CORJIT_FLAGS::CORJIT_FLAG_TIER1))
    {
        flags.Set(CORJIT_FLAGS::CORJIT_FLAG_BBOPT);
    }

#endif

    return flags;
}

// ********************************************************************

// Throw the right type of exception for the given JIT result

void ThrowExceptionForJit(HRESULT res)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;
    switch (res)
    {
        case CORJIT_OUTOFMEM:
            COMPlusThrowOM();
            break;

        case CORJIT_INTERNALERROR:
            COMPlusThrow(kInvalidProgramException, (UINT) IDS_EE_JIT_COMPILER_ERROR);
            break;

        case CORJIT_BADCODE:
        case CORJIT_IMPLLIMITATION:
        default:
            COMPlusThrow(kInvalidProgramException);
            break;
    }
 }

// ********************************************************************
//#define PERF_TRACK_METHOD_JITTIMES
#ifdef TARGET_AMD64
BOOL g_fAllowRel32 = TRUE;
#endif


// ********************************************************************
//                  README!!
// ********************************************************************

// The reason that this is named UnsafeJitFunction is that this helper
// method is not thread safe!  When multiple threads get in here for
// the same pMD, ALL of them MUST return the SAME value.
// To insure that this happens you must call MakeJitWorker.
// It creates a DeadlockAware list of methods being jitted and prevents us
// from trying to jit the same method more that once.
//
// Calls to this method that occur to check if inlining can occur on x86,
// are OK since they discard the return value of this method.
PCODE UnsafeJitFunction(PrepareCodeConfig* config,
                        _In_opt_ COR_ILMETHOD_DECODER* ILHeader,
                        _In_ CORJIT_FLAGS* pJitFlags,
                        _In_opt_ ULONG* pSizeOfCode)
{
    STANDARD_VM_CONTRACT;
    _ASSERTE(config != NULL);
    _ASSERTE(pJitFlags != NULL);

    NativeCodeVersion nativeCodeVersion = config->GetCodeVersion();
    MethodDesc* ftn = nativeCodeVersion.GetMethodDesc();

    PCODE ret = NULL;
    NormalizedTimer timer;
    int64_t c100nsTicksInJit = 0;

    COOPERATIVE_TRANSITION_BEGIN();

    timer.Start();

    EEJitManager *jitMgr = ExecutionManager::GetEEJitManager();
    if (!jitMgr->LoadJIT())
    {
#ifdef ALLOW_SXS_JIT
        if (!jitMgr->IsMainJitLoaded())
        {
            // Don't want to throw InvalidProgram from here.
            EEPOLICY_HANDLE_FATAL_ERROR_WITH_MESSAGE(COR_E_EXECUTIONENGINE, W("Failed to load JIT compiler"));
        }
        if (!jitMgr->IsAltJitLoaded())
        {
            // Don't want to throw InvalidProgram from here.
            EEPOLICY_HANDLE_FATAL_ERROR_WITH_MESSAGE(COR_E_EXECUTIONENGINE, W("Failed to load alternative JIT compiler"));
        }
#else // ALLOW_SXS_JIT
        // Don't want to throw InvalidProgram from here.
        EEPOLICY_HANDLE_FATAL_ERROR_WITH_MESSAGE(COR_E_EXECUTIONENGINE, W("Failed to load JIT compiler"));
#endif // ALLOW_SXS_JIT
    }

#ifdef _DEBUG
    // This is here so we can see the name and class easily in the debugger

    LPCUTF8 cls  = ftn->GetMethodTable()->GetDebugClassName();
    LPCUTF8 name = ftn->GetName();

    if (ftn->IsNoMetadata())
    {
        if (ftn->IsILStub())
        {
            LOG((LF_JIT, LL_INFO10000, "{ Jitting IL Stub }\n"));
        }
        else
        {
            LOG((LF_JIT, LL_INFO10000, "{ Jitting dynamic method }\n"));
        }
    }
    else
    {
        SString methodString;
        if (LoggingOn(LF_JIT, LL_INFO10000))
            TypeString::AppendMethodDebug(methodString, ftn);

        LOG((LF_JIT, LL_INFO10000, "{ Jitting method (%p) %s %s\n", ftn, methodString.GetUTF8(), ftn->m_pszDebugMethodSignature));
    }
#endif // _DEBUG

    MethodInfoHelperContext cxt{ ftn, ILHeader };
    CORINFO_METHOD_INFO methodInfo;
    getMethodInfoHelper(cxt, &methodInfo);

    // If it's generic then we can only enter through an instantiated MethodDesc
    _ASSERTE(!ftn->IsGenericMethodDefinition());

    // method attributes and signature are consistant
    _ASSERTE(!!ftn->IsStatic() == ((methodInfo.args.callConv & CORINFO_CALLCONV_HASTHIS) == 0));

    *pJitFlags = GetCompileFlags(config, ftn, &methodInfo);

#if defined(TARGET_AMD64) || defined(TARGET_ARM64)
    BOOL fForceJumpStubOverflow = FALSE;

#ifdef _DEBUG
    // Always exercise the overflow codepath with force relocs
    if (PEDecoder::GetForceRelocs())
        fForceJumpStubOverflow = TRUE;
#endif

#if defined(TARGET_AMD64)
    BOOL fAllowRel32 = (g_fAllowRel32 | fForceJumpStubOverflow) && g_pConfig->JitEnableOptionalRelocs();
#endif

    size_t reserveForJumpStubs = 0;

#endif // defined(TARGET_AMD64) || defined(TARGET_ARM64)

    while (true)
    {
        CEEJitInfo jitInfo(ftn, ILHeader, jitMgr, !pJitFlags->IsSet(CORJIT_FLAGS::CORJIT_FLAG_NO_INLINING));

#if defined(TARGET_AMD64) || defined(TARGET_ARM64)
#if defined(TARGET_AMD64)
        if (fForceJumpStubOverflow)
            jitInfo.SetJumpStubOverflow(fAllowRel32);
        jitInfo.SetAllowRel32(fAllowRel32);
#else
        if (fForceJumpStubOverflow)
            jitInfo.SetJumpStubOverflow(fForceJumpStubOverflow);
#endif
        jitInfo.SetReserveForJumpStubs(reserveForJumpStubs);
#endif // defined(TARGET_AMD64) || defined(TARGET_ARM64)

#ifdef FEATURE_ON_STACK_REPLACEMENT
        // If this is an OSR jit request, grab the OSR info so we can pass it to the jit
        if (pJitFlags->IsSet(CORJIT_FLAGS::CORJIT_FLAG_OSR))
        {
            unsigned ilOffset = 0;
            PatchpointInfo* patchpointInfo = nativeCodeVersion.GetOSRInfo(&ilOffset);
            jitInfo.SetOSRInfo(patchpointInfo, ilOffset);
        }
#endif // FEATURE_ON_STACK_REPLACEMENT

        if (cxt.HasTransientMethodDetails())
            jitInfo.AddTransientMethodDetails(cxt.CreateTransientMethodDetails());

        MethodDesc * pMethodForSecurity = jitInfo.GetMethodForSecurity(methodInfo.ftn);

        //Since the check could trigger a demand, we have to do this every time.
        //This is actually an overly complicated way to make sure that a method can access all its arguments
        //and its return type.
        AccessCheckOptions::AccessCheckType accessCheckType = AccessCheckOptions::kNormalAccessibilityChecks;
        TypeHandle ownerTypeForSecurity = TypeHandle(pMethodForSecurity->GetMethodTable());
        DynamicResolver *pAccessContext = NULL;
        BOOL doAccessCheck = TRUE;
        if (pMethodForSecurity->IsDynamicMethod())
        {
            doAccessCheck = ModifyCheckForDynamicMethod(pMethodForSecurity->AsDynamicMethodDesc()->GetResolver(),
                                                        &ownerTypeForSecurity,
                                                        &accessCheckType, &pAccessContext);
        }
        if (doAccessCheck)
        {
            AccessCheckOptions accessCheckOptions(accessCheckType,
                                                  pAccessContext,
                                                  TRUE /*Throw on error*/,
                                                  pMethodForSecurity);

            AccessCheckContext accessContext(pMethodForSecurity, ownerTypeForSecurity.GetMethodTable());

            // We now do an access check from pMethodForSecurity to pMethodForSecurity, its sole purpose is to
            // verify that pMethodForSecurity/ownerTypeForSecurity has access to all its parameters.

            // ownerTypeForSecurity.GetMethodTable() can be null if the pMethodForSecurity is a DynamicMethod
            // associated with a TypeDesc (Array, Ptr, Ref, or FnPtr). That doesn't make any sense, but we will
            // just do an access check from a NULL context which means only public types are accessible.
            if (!ClassLoader::CanAccess(&accessContext,
                                        ownerTypeForSecurity.GetMethodTable(),
                                        ownerTypeForSecurity.GetAssembly(),
                                        pMethodForSecurity->GetAttrs(),
                                        pMethodForSecurity,
                                        NULL,
                                        accessCheckOptions))
            {
                EX_THROW(EEMethodException, (pMethodForSecurity));
            }
        }

        CorJitResult res;
        PBYTE nativeEntry;
        uint32_t sizeOfCode;

        {
            GCX_COOP();

            /* There is a double indirection to call compileMethod  - can we
               improve this with the new structure? */

#ifdef PERF_TRACK_METHOD_JITTIMES
            //Because we're not calling QPC enough.  I'm not going to track times if we're just importing.
            LARGE_INTEGER methodJitTimeStart = {0};
            QueryPerformanceCounter (&methodJitTimeStart);

#endif
            LOG((LF_CORDB, LL_EVERYTHING, "Calling invokeCompileMethod...\n"));

            res = invokeCompileMethod(jitMgr,
                                      &jitInfo,
                                      &methodInfo,
                                      *pJitFlags,
                                      &nativeEntry,
                                      &sizeOfCode);

            LOG((LF_CORDB, LL_EVERYTHING, "Got through invokeCompileMethod\n"));

#if FEATURE_PERFMAP
            // Save the code size so that it can be reported to the perfmap.
            if (pSizeOfCode != NULL)
            {
                *pSizeOfCode = sizeOfCode;
            }
#endif

#ifdef PERF_TRACK_METHOD_JITTIMES
            //store the time in the string buffer.  Module name and token are unique enough.  Also, do not
            //capture importing time, just actual compilation time.
            {
                LARGE_INTEGER methodJitTimeStop;
                QueryPerformanceCounter(&methodJitTimeStop);

                SString moduleName;
                ftn->GetModule()->GetDomainAssembly()->GetPEAssembly()->GetPathOrCodeBase(moduleName);

                SString codeBase;
                codeBase.AppendPrintf("%s,0x%x,%d,%d\n",
                                 moduleName.GetUTF8(), //module name
                                 ftn->GetMemberDef(), //method token
                                 (unsigned)(methodJitTimeStop.QuadPart - methodJitTimeStart.QuadPart), //cycle count
                                 methodInfo.ILCodeSize //il size
                                );
                OutputDebugStringUtf8(codeBase.GetUTF8());
            }
#endif // PERF_TRACK_METHOD_JITTIMES

        }

        LOG((LF_JIT, LL_INFO10000, "Done Jitting method %s::%s  %s }\n",cls,name, ftn->m_pszDebugMethodSignature));

        if (SUCCEEDED(res))
        {
            jitInfo.WriteCode(jitMgr);
#if defined(DEBUGGING_SUPPORTED)
            //
            // Notify the debugger that we have successfully jitted the function
            //
            if (g_pDebugInterface)
            {
                if (!jitInfo.JitAgain())
                {
                    g_pDebugInterface->JITComplete(nativeCodeVersion, (TADDR)nativeEntry);
                }
            }
#endif // DEBUGGING_SUPPORTED
        }
        else
        {
            jitInfo.BackoutJitData(jitMgr);
            ThrowExceptionForJit(res);
        }

        if (!nativeEntry)
            COMPlusThrow(kInvalidProgramException);

#if (defined(TARGET_AMD64) || defined(TARGET_ARM64))
        if (jitInfo.IsJumpStubOverflow())
        {
            // Backout and try again with fAllowRel32 == FALSE.
            jitInfo.BackoutJitData(jitMgr);

#ifdef TARGET_AMD64
            // Disallow rel32 relocs in future.
            g_fAllowRel32 = FALSE;

            fAllowRel32 = FALSE;
#endif // TARGET_AMD64
#ifdef TARGET_ARM64
            fForceJumpStubOverflow = FALSE;
#endif // TARGET_ARM64

            reserveForJumpStubs = jitInfo.GetReserveForJumpStubs();

            // Get any transient method details and take ownership
            // from the JITInfo instance. We are going to be recreating
            // a new JITInfo and will reuse these details there.
            TransientMethodDetails details = jitInfo.RemoveTransientMethodDetails(ftn);
            cxt.TakeOwnership(std::move(details));
            continue;
        }
#endif // (TARGET_AMD64 || TARGET_ARM64)

        LOG((LF_JIT, LL_INFO10000,
            "Jitted Entry at" FMT_ADDR "method %s::%s %s\n", DBG_ADDR(nativeEntry),
             ftn->m_pszDebugClassName, ftn->m_pszDebugMethodName, ftn->m_pszDebugMethodSignature));

#ifdef _DEBUG
        LPCUTF8 pszDebugClassName = ftn->m_pszDebugClassName;
        LPCUTF8 pszDebugMethodName = ftn->m_pszDebugMethodName;
        LPCUTF8 pszDebugMethodSignature = ftn->m_pszDebugMethodSignature;
#elif 0
        LPCUTF8 pszNamespace;
        LPCUTF8 pszDebugClassName = ftn->GetMethodTable()->GetFullyQualifiedNameInfo(&pszNamespace);
        LPCUTF8 pszDebugMethodName = ftn->GetName();
        LPCUTF8 pszDebugMethodSignature = "";
#endif

        //DbgPrintf("Jitted Entry at" FMT_ADDR "method %s::%s %s size %08x\n", DBG_ADDR(nativeEntry),
        //          pszDebugClassName, pszDebugMethodName, pszDebugMethodSignature, sizeOfCode);

        // For dynamic method, the code memory may be reused, thus we are passing in the hasCodeExecutedBefore set to true
        ClrFlushInstructionCache(nativeEntry, sizeOfCode, /* hasCodeExecutedBefore */ true);
        ret = (PCODE)nativeEntry;

#ifdef TARGET_ARM
        ret |= THUMB_CODE;
#endif

        // We are done
        break;
    }

#ifdef _DEBUG
    static BOOL fHeartbeat = -1;

    if (fHeartbeat == -1)
        fHeartbeat = CLRConfig::GetConfigValue(CLRConfig::INTERNAL_JitHeartbeat);

    if (fHeartbeat)
        printf(".");
#endif // _DEBUG

    timer.Stop();
    c100nsTicksInJit = timer.Elapsed100nsTicks();

    InterlockedExchangeAdd64((LONG64*)&g_c100nsTicksInJit, c100nsTicksInJit);
    t_c100nsTicksInJitForThread += c100nsTicksInJit;

    InterlockedExchangeAdd64((LONG64*)&g_cbILJitted, methodInfo.ILCodeSize);
    t_cbILJittedForThread += methodInfo.ILCodeSize;

    InterlockedIncrement64((LONG64*)&g_cMethodsJitted);
    t_cMethodsJittedForThread++;

    COOPERATIVE_TRANSITION_END();
    return ret;
}

#ifdef FEATURE_READYTORUN
CorInfoHelpFunc MapReadyToRunHelper(ReadyToRunHelper helperNum)
{
    LIMITED_METHOD_CONTRACT;

    switch (helperNum)
    {
#define HELPER(readyToRunHelper, corInfoHelpFunc, flags) \
    case readyToRunHelper:                                  return corInfoHelpFunc;
#include "readytorunhelpers.h"

    case READYTORUN_HELPER_GetString:                       return CORINFO_HELP_STRCNS;

    default:                                                return CORINFO_HELP_UNDEF;
    }
}

void ComputeGCRefMap(MethodTable * pMT, BYTE * pGCRefMap, size_t cbGCRefMap)
{
    STANDARD_VM_CONTRACT;

    ZeroMemory(pGCRefMap, cbGCRefMap);

    if (!pMT->ContainsPointers())
        return;

    CGCDesc* map = CGCDesc::GetCGCDescFromMT(pMT);
    CGCDescSeries* cur = map->GetHighestSeries();
    CGCDescSeries* last = map->GetLowestSeries();
    DWORD size = pMT->GetBaseSize();
    _ASSERTE(cur >= last);

    do
    {
        // offset to embedded references in this series must be
        // adjusted by the VTable pointer, when in the unboxed state.
        size_t offset = cur->GetSeriesOffset() - TARGET_POINTER_SIZE;
        size_t offsetStop = offset + cur->GetSeriesSize() + size;
        while (offset < offsetStop)
        {
            size_t bit = offset / TARGET_POINTER_SIZE;

            size_t index = bit / 8;
            _ASSERTE(index < cbGCRefMap);
            pGCRefMap[index] |= (1 << (bit & 7));

            offset += TARGET_POINTER_SIZE;
        }
        cur--;
    } while (cur >= last);
}

//
// Type layout check verifies that there was no incompatible change in the value type layout.
// If there was one, we will fall back to JIT instead of using the pre-generated code from the ready to run image.
// This should be rare situation. Changes in value type layout not common.
//
// The following properties of the value type layout are checked:
// - Size
// - HFA-ness (on platform that support HFAs)
// - Alignment
// - Position of GC references
//
BOOL TypeLayoutCheck(MethodTable * pMT, PCCOR_SIGNATURE pBlob, BOOL printDiff)
{
    STANDARD_VM_CONTRACT;

    SigPointer p(pBlob);
    IfFailThrow(p.SkipExactlyOne());

    uint32_t dwFlags;
    IfFailThrow(p.GetData(&dwFlags));

    BOOL result = TRUE;

    // Size is checked unconditionally
    uint32_t dwExpectedSize;
    IfFailThrow(p.GetData(&dwExpectedSize));

    DWORD dwActualSize = pMT->GetNumInstanceFieldBytes();
    if (dwExpectedSize != dwActualSize)
    {
        if (printDiff)
        {
            result = FALSE;

            DefineFullyQualifiedNameForClass();
            printf("Type %s: expected size 0x%08x, actual size 0x%08x\n",
                GetFullyQualifiedNameForClass(pMT), dwExpectedSize, dwActualSize);
        }
        else
        {
            return FALSE;
        }
    }

#ifdef FEATURE_HFA
    if (dwFlags & READYTORUN_LAYOUT_HFA)
    {
        uint32_t dwExpectedHFAType;
        IfFailThrow(p.GetData(&dwExpectedHFAType));

        DWORD dwActualHFAType = pMT->GetHFAType();
        if (dwExpectedHFAType != dwActualHFAType)
        {
            if (printDiff)
            {
                result = FALSE;

                DefineFullyQualifiedNameForClass();
                printf("Type %s: expected HFA type %08x, actual %08x\n",
                    GetFullyQualifiedNameForClass(pMT), dwExpectedHFAType, dwActualHFAType);
            }
            else
            {
                return FALSE;
            }
        }
    }
    else
    {
        if (pMT->IsHFA())
        {
            if (printDiff)
            {
                result = FALSE;

                DefineFullyQualifiedNameForClass();
                printf("Type %s: type is HFA but READYTORUN_LAYOUT_HFA flag is not set\n",
                    GetFullyQualifiedNameForClass(pMT));
            }
            else
            {
                return FALSE;
            }
        }
    }
#else
    _ASSERTE(!(dwFlags & READYTORUN_LAYOUT_HFA));
#endif

    if (dwFlags & READYTORUN_LAYOUT_Alignment)
    {
        uint32_t dwExpectedAlignment = TARGET_POINTER_SIZE;
        if (!(dwFlags & READYTORUN_LAYOUT_Alignment_Native))
        {
            IfFailThrow(p.GetData(&dwExpectedAlignment));
        }

        DWORD dwActualAlignment = CEEInfo::getClassAlignmentRequirementStatic(pMT);
        if (dwExpectedAlignment != dwActualAlignment)
        {
            if (printDiff)
            {
                result = FALSE;

                DefineFullyQualifiedNameForClass();
                printf("Type %s: expected alignment 0x%08x, actual 0x%08x\n",
                    GetFullyQualifiedNameForClass(pMT), dwExpectedAlignment, dwActualAlignment);
            }
            else
            {
                return FALSE;
            }
        }

    }

    if (dwFlags & READYTORUN_LAYOUT_GCLayout)
    {
        if (dwFlags & READYTORUN_LAYOUT_GCLayout_Empty)
        {
            if (pMT->ContainsPointers())
            {
                if (printDiff)
                {
                    result = FALSE;

                    DefineFullyQualifiedNameForClass();
                    printf("Type %s contains pointers but READYTORUN_LAYOUT_GCLayout_Empty is set\n",
                        GetFullyQualifiedNameForClass(pMT));
                }
                else
                {
                    return FALSE;
                }
            }
        }
        else
        {
            size_t cbGCRefMap = (dwActualSize / TARGET_POINTER_SIZE + 7) / 8;
            _ASSERTE(cbGCRefMap > 0);

            BYTE * pGCRefMap = (BYTE *)_alloca(cbGCRefMap);

            ComputeGCRefMap(pMT, pGCRefMap, cbGCRefMap);

            if (memcmp(pGCRefMap, p.GetPtr(), cbGCRefMap) != 0)
            {
                if (printDiff)
                {
                    result = FALSE;

                    DefineFullyQualifiedNameForClass();
                    printf("Type %s: GC refmap content doesn't match\n",
                        GetFullyQualifiedNameForClass(pMT));
                }
                else
                {
                    return FALSE;
                }
            }
        }
    }

    return result;
}

#endif // FEATURE_READYTORUN

bool IsInstructionSetSupported(CORJIT_FLAGS jitFlags, ReadyToRunInstructionSet r2rInstructionSet)
{
    CORINFO_InstructionSet instructionSet = InstructionSetFromR2RInstructionSet(r2rInstructionSet);
    return jitFlags.IsSet(instructionSet);
}

BOOL LoadDynamicInfoEntry(Module *currentModule,
                          RVA fixupRva,
                          SIZE_T *entry,
                          BOOL mayUsePrecompiledNDirectMethods)
{
    STANDARD_VM_CONTRACT;

    PCCOR_SIGNATURE pBlob = currentModule->GetNativeFixupBlobData(fixupRva);

    BYTE kind = *pBlob++;

    ModuleBase * pInfoModule = currentModule;

    if (kind & ENCODE_MODULE_OVERRIDE)
    {
        pInfoModule = currentModule->GetModuleFromIndex(CorSigUncompressData(pBlob));
        kind &= ~ENCODE_MODULE_OVERRIDE;
    }

    MethodDesc * pMD = NULL;

    PCCOR_SIGNATURE pSig;
    DWORD cSig;

    size_t result = 0;

    switch (kind)
    {
    case ENCODE_MODULE_HANDLE:
        result = (size_t)pInfoModule;
        break;

    case ENCODE_TYPE_HANDLE:
    case ENCODE_TYPE_DICTIONARY:
        {
            TypeHandle th = ZapSig::DecodeType(currentModule, pInfoModule, pBlob);

            if (!th.IsTypeDesc())
            {
                if (currentModule->IsReadyToRun())
                {
                    // We do not emit activation fixups for version resilient references. Activate the target explicitly.
                    th.AsMethodTable()->EnsureInstanceActive();
                }
            }

            result = (size_t)th.AsPtr();
        }
        break;

    case ENCODE_METHOD_HANDLE:
    case ENCODE_METHOD_DICTIONARY:
        {
            MethodDesc * pMD = ZapSig::DecodeMethod(currentModule, pInfoModule, pBlob);

            if (currentModule->IsReadyToRun())
            {
                // We do not emit activation fixups for version resilient references. Activate the target explicitly.
                pMD->EnsureActive();
            }

            result = (size_t)pMD;
        }
        break;

    case ENCODE_FIELD_HANDLE:
        result = (size_t) ZapSig::DecodeField(currentModule, pInfoModule, pBlob);
        break;

    case ENCODE_STRING_HANDLE:
        {
            // We need to update strings atomically (due to NoStringInterning attribute). Note
            // that modules with string interning dont really need this, as the hash tables have
            // their own locking, but dont add more complexity for what will be the non common
            // case.

            // We will have to lock and update the entry. (this is really a double check, where
            // the first check is done in the caller of this function)
            DWORD rid = CorSigUncompressData(pBlob);
            if (rid == 0)
            {
                // Empty string
                result = (size_t)StringObject::GetEmptyStringRefPtr(nullptr);
            }
            else
            {
                result = (size_t) pInfoModule->ResolveStringRef(TokenFromRid(rid, mdtString));
            }
        }
        break;

    case ENCODE_VARARGS_SIG:
        {
            mdSignature token = TokenFromRid(
                                    CorSigUncompressData(pBlob),
                                    mdtSignature);

            IfFailThrow(pInfoModule->GetMDImport()->GetSigFromToken(token, &cSig, &pSig));

            goto VarArgs;
        }
        break;

    case ENCODE_VARARGS_METHODREF:
        {
            mdSignature token = TokenFromRid(
                                    CorSigUncompressData(pBlob),
                                    mdtMemberRef);

            LPCSTR szName_Ignore;
            IfFailThrow(pInfoModule->GetMDImport()->GetNameAndSigOfMemberRef(token, &pSig, &cSig, &szName_Ignore));

            goto VarArgs;
        }
        break;

    case ENCODE_VARARGS_METHODDEF:
        {
            mdSignature token = TokenFromRid(
                                    CorSigUncompressData(pBlob),
                                    mdtMethodDef);

            IfFailThrow(pInfoModule->GetMDImport()->GetSigOfMethodDef(token, &cSig, &pSig));
        }
        {
        VarArgs:
            result = (size_t) CORINFO_VARARGS_HANDLE(currentModule->GetVASigCookie(Signature(pSig, cSig)));
        }
        break;

    case ENCODE_METHOD_ENTRY_DEF_TOKEN:
        {
            mdToken MethodDef = TokenFromRid(CorSigUncompressData(pBlob), mdtMethodDef);
            _ASSERTE(pInfoModule->IsFullModule());
            pMD = MemberLoader::GetMethodDescFromMethodDef(static_cast<Module*>(pInfoModule), MethodDef, FALSE);

            pMD->PrepareForUseAsADependencyOfANativeImage();

            if (currentModule->IsReadyToRun())
            {
                // We do not emit activation fixups for version resilient references. Activate the target explicitly.
                pMD->EnsureActive();
            }

            goto MethodEntry;
        }

    case ENCODE_METHOD_ENTRY_REF_TOKEN:
        {
            SigTypeContext typeContext;
            mdToken MemberRef = TokenFromRid(CorSigUncompressData(pBlob), mdtMemberRef);
            FieldDesc * pFD = NULL;
            TypeHandle th;

            MemberLoader::GetDescFromMemberRef(pInfoModule, MemberRef, &pMD, &pFD, &typeContext, FALSE /* strict metadata checks */, &th);
            _ASSERTE(pMD != NULL);

            pMD->PrepareForUseAsADependencyOfANativeImage();

            if (currentModule->IsReadyToRun())
            {
                // We do not emit activation fixups for version resilient references. Activate the target explicitly.
                pMD->EnsureActive();
            }

            goto MethodEntry;
        }

    case ENCODE_METHOD_ENTRY:
        {
            pMD = ZapSig::DecodeMethod(currentModule, pInfoModule, pBlob);

            if (currentModule->IsReadyToRun())
            {
                // We do not emit activation fixups for version resilient references. Activate the target explicitly.
                pMD->EnsureActive();
            }

        MethodEntry:
            result = pMD->GetMultiCallableAddrOfCode(CORINFO_ACCESS_ANY);
        }
        break;

    case ENCODE_SYNC_LOCK:
        {
            TypeHandle th = ZapSig::DecodeType(currentModule, pInfoModule, pBlob);

            result = (size_t) GetClassSync(th.AsMethodTable());
        }
        break;

    case ENCODE_INDIRECT_PINVOKE_TARGET:
        {
            MethodDesc *pMethod = ZapSig::DecodeMethod(currentModule, pInfoModule, pBlob);

            _ASSERTE(pMethod->IsNDirect());
            NDirectMethodDesc *pMD = (NDirectMethodDesc*)pMethod;
            result = (size_t)(LPVOID)&(pMD->ndirect.m_pNDirectTarget);
        }
        break;

    case ENCODE_PINVOKE_TARGET:
        {
            if (mayUsePrecompiledNDirectMethods)
            {
                MethodDesc *pMethod = ZapSig::DecodeMethod(currentModule, pInfoModule, pBlob);

                _ASSERTE(pMethod->IsNDirect());
                result = (size_t)(LPVOID)NDirectMethodDesc::ResolveAndSetNDirectTarget((NDirectMethodDesc*)pMethod);
            }
            else
            {
                return FALSE;
            }
        }
        break;

#if defined(PROFILING_SUPPORTED)
    case ENCODE_PROFILING_HANDLE:
        {
            MethodDesc *pMethod = ZapSig::DecodeMethod(currentModule, pInfoModule, pBlob);

            // methods with no metadata behind cannot be exposed to tools expecting metadata (profiler, debugger...)
            // they shouldnever come here as they are called out in GetCompileFlag
            _ASSERTE(!pMethod->IsNoMetadata());

            FunctionID funId = (FunctionID)pMethod;

            BOOL bHookFunction = TRUE;
            CORINFO_PROFILING_HANDLE profilerHandle = (CORINFO_PROFILING_HANDLE)funId;

            {
                BEGIN_PROFILER_CALLBACK(CORProfilerFunctionIDMapperEnabled());
                profilerHandle = (CORINFO_PROFILING_HANDLE)(&g_profControlBlock)->EEFunctionIDMapper(funId, &bHookFunction);
                END_PROFILER_CALLBACK();
            }

            // Profiling handle is opaque token. It does not have to be aligned thus we can not store it in the same location as token.
            *(entry+kZapProfilingHandleImportValueIndexClientData) = (SIZE_T)profilerHandle;

            if (bHookFunction)
            {
                *(entry+kZapProfilingHandleImportValueIndexEnterAddr) = (SIZE_T)(void *)hlpDynamicFuncTable[DYNAMIC_CORINFO_HELP_PROF_FCN_ENTER].pfnHelper;
                *(entry+kZapProfilingHandleImportValueIndexLeaveAddr) = (SIZE_T)(void *)hlpDynamicFuncTable[DYNAMIC_CORINFO_HELP_PROF_FCN_LEAVE].pfnHelper;
                *(entry+kZapProfilingHandleImportValueIndexTailcallAddr) = (SIZE_T)(void *)hlpDynamicFuncTable[DYNAMIC_CORINFO_HELP_PROF_FCN_TAILCALL].pfnHelper;
            }
            else
            {
                *(entry+kZapProfilingHandleImportValueIndexEnterAddr) = (SIZE_T)(void *)JIT_ProfilerEnterLeaveTailcallStub;
                *(entry+kZapProfilingHandleImportValueIndexLeaveAddr) = (SIZE_T)(void *)JIT_ProfilerEnterLeaveTailcallStub;
                *(entry+kZapProfilingHandleImportValueIndexTailcallAddr) = (SIZE_T)(void *)JIT_ProfilerEnterLeaveTailcallStub;
            }
        }
        break;
#endif // PROFILING_SUPPORTED

    case ENCODE_FIELD_ADDRESS:
        {
            FieldDesc *pField = ZapSig::DecodeField(currentModule, pInfoModule, pBlob);

            pField->GetEnclosingMethodTable()->CheckRestore();

            // We can take address of RVA field only since ngened code is domain neutral
            _ASSERTE(pField->IsRVA());

            result = (size_t)pField->GetStaticAddressHandle(NULL);
        }
        break;

    case ENCODE_VIRTUAL_ENTRY_SLOT:
        {
            DWORD slot = CorSigUncompressData(pBlob);

            TypeHandle ownerType = ZapSig::DecodeType(currentModule, pInfoModule, pBlob);

            LOG((LF_ZAP, LL_INFO100000, "     Fixup stub dispatch\n"));

            VirtualCallStubManager * pMgr = currentModule->GetLoaderAllocator()->GetVirtualCallStubManager();

            // <REVISIT_TODO>
            // We should be generating a stub indirection here, but the zapper already uses one level
            // of indirection, i.e. we would have to return IAT_PPVALUE to the JIT, and on the whole the JITs
            // aren't quite set up to accept that. Furthermore the call sequences would be different - at
            // the moment an indirection cell uses "call [cell-addr]" on x86, and instead we would want the
            // euqivalent of "call [[call-addr]]".  This could perhaps be implemented as "call [eax]" </REVISIT_TODO>
            result = pMgr->GetCallStub(ownerType, slot);
        }
        break;

    case ENCODE_CLASS_ID_FOR_STATICS:
        {
            TypeHandle th = ZapSig::DecodeType(currentModule, pInfoModule, pBlob);

            MethodTable * pMT = th.AsMethodTable();
            if (pMT->IsDynamicStatics())
            {
                result = pMT->GetModuleDynamicEntryID();
            }
            else
            {
                result = pMT->GetClassIndex();
            }
        }
        break;

    case ENCODE_MODULE_ID_FOR_GENERIC_STATICS:
        {
            TypeHandle th = ZapSig::DecodeType(currentModule, pInfoModule, pBlob);

            MethodTable * pMT = th.AsMethodTable();

            result = pMT->GetModuleForStatics()->GetModuleID();
        }
        break;

#ifdef FEATURE_READYTORUN
    case ENCODE_READYTORUN_HELPER:
        {
            DWORD helperNum = CorSigUncompressData(pBlob);

            CorInfoHelpFunc corInfoHelpFunc = MapReadyToRunHelper((ReadyToRunHelper)helperNum);
            if (corInfoHelpFunc != CORINFO_HELP_UNDEF)
            {
                result = (size_t)CEEJitInfo::getHelperFtnStatic(corInfoHelpFunc);
            }
            else
            {
                switch (helperNum)
                {
                case READYTORUN_HELPER_Module:
                    {
                        _ASSERTE(pInfoModule->IsFullModule());
                        Module *fullModule = static_cast<Module*>(pInfoModule);
                        Module * pPrevious = InterlockedCompareExchangeT((Module **)entry, fullModule, NULL);
                        if (pPrevious != pInfoModule && pPrevious != NULL)
                            COMPlusThrowHR(COR_E_FILELOAD, IDS_NATIVE_IMAGE_CANNOT_BE_LOADED_MULTIPLE_TIMES, fullModule->GetPath());
                        return TRUE;
                    }
                    break;

                case READYTORUN_HELPER_GSCookie:
                    result = (size_t)GetProcessGSCookie();
                    break;

                case READYTORUN_HELPER_IndirectTrapThreads:
                    result = (size_t)&g_TrapReturningThreads;
                    break;

                case READYTORUN_HELPER_DelayLoad_MethodCall:
                    result = (size_t)GetEEFuncEntryPoint(DelayLoad_MethodCall);
                    break;

                case READYTORUN_HELPER_DelayLoad_Helper:
                    result = (size_t)GetEEFuncEntryPoint(DelayLoad_Helper);
                    break;

                case READYTORUN_HELPER_DelayLoad_Helper_Obj:
                    result = (size_t)GetEEFuncEntryPoint(DelayLoad_Helper_Obj);
                    break;

                case READYTORUN_HELPER_DelayLoad_Helper_ObjObj:
                    result = (size_t)GetEEFuncEntryPoint(DelayLoad_Helper_ObjObj);
                    break;

                default:
                    STRESS_LOG1(LF_ZAP, LL_WARNING, "Unknown READYTORUN_HELPER %d\n", helperNum);
                    _ASSERTE(!"Unknown READYTORUN_HELPER");
                    return FALSE;
                }
            }
        }
        break;

    case ENCODE_FIELD_OFFSET:
        {
            FieldDesc * pFD = ZapSig::DecodeField(currentModule, pInfoModule, pBlob);
            _ASSERTE(!pFD->IsStatic());
            _ASSERTE(!pFD->IsFieldOfValueType());

            DWORD dwOffset = (DWORD)sizeof(Object) + pFD->GetOffset();

            if (dwOffset > MAX_UNCHECKED_OFFSET_FOR_NULL_OBJECT)
                return FALSE;
            result = dwOffset;
        }
        break;

    case ENCODE_FIELD_BASE_OFFSET:
        {
            TypeHandle th = ZapSig::DecodeType(currentModule, pInfoModule, pBlob);

            MethodTable * pMT = th.AsMethodTable();
            _ASSERTE(!pMT->IsValueType());

            DWORD dwOffsetBase = ReadyToRunInfo::GetFieldBaseOffset(pMT);
            if (dwOffsetBase > MAX_UNCHECKED_OFFSET_FOR_NULL_OBJECT)
                return FALSE;
            result = dwOffsetBase;
        }
        break;

    case ENCODE_CHECK_TYPE_LAYOUT:
    case ENCODE_VERIFY_TYPE_LAYOUT:
        {
            TypeHandle th = ZapSig::DecodeType(currentModule, pInfoModule, pBlob);
            MethodTable * pMT = th.AsMethodTable();
            _ASSERTE(pMT->IsValueType());

            if (!TypeLayoutCheck(pMT, pBlob, /* printDiff */ kind == ENCODE_VERIFY_TYPE_LAYOUT))
            {
                if (kind == ENCODE_CHECK_TYPE_LAYOUT)
                {
                    return FALSE;
                }
                else
                {
                    // Verification failures are failfast events
                    DefineFullyQualifiedNameForClass();
                    SString fatalErrorString;
                    fatalErrorString.Printf("Verify_TypeLayout '%s' failed to verify type layout",
                        GetFullyQualifiedNameForClass(pMT));

#ifdef _DEBUG
                    {
                        _ASSERTE_MSG(false, fatalErrorString.GetUTF8());
                        // Run through the type layout logic again, after the assert, makes debugging easy
                        TypeLayoutCheck(pMT, pBlob, /* printDiff */ TRUE);
                    }
#endif

                    EEPOLICY_HANDLE_FATAL_ERROR_WITH_MESSAGE(-1, fatalErrorString.GetUnicode());
                    return FALSE;
                }
            }

            result = 1;
        }
        break;

    case ENCODE_CHECK_FIELD_OFFSET:
        {
            DWORD dwExpectedOffset = CorSigUncompressData(pBlob);

            FieldDesc * pFD = ZapSig::DecodeField(currentModule, pInfoModule, pBlob);
            _ASSERTE(!pFD->IsStatic());

            DWORD dwOffset = pFD->GetOffset();
            if (!pFD->IsFieldOfValueType())
                dwOffset += sizeof(Object);

            if (dwExpectedOffset != dwOffset)
                return FALSE;

            result = 1;
        }
        break;

    case ENCODE_VERIFY_FIELD_OFFSET:
        {
            DWORD baseOffset = CorSigUncompressData(pBlob);
            DWORD fieldOffset = CorSigUncompressData(pBlob);
            FieldDesc* pField = ZapSig::DecodeField(currentModule, pInfoModule, pBlob);
            MethodTable *pEnclosingMT = pField->GetApproxEnclosingMethodTable();
            pEnclosingMT->CheckRestore();
            DWORD actualFieldOffset = pField->GetOffset();
            if (!pField->IsStatic() && !pField->IsFieldOfValueType())
            {
                actualFieldOffset += sizeof(Object);
            }

            DWORD actualBaseOffset = 0;
            if (!pField->IsStatic() &&
                pEnclosingMT->GetParentMethodTable() != NULL &&
                !pEnclosingMT->IsValueType())
            {
                actualBaseOffset = ReadyToRunInfo::GetFieldBaseOffset(pEnclosingMT);
            }

            if (baseOffset == 0)
            {
                // Relative verification of just the field offset when the base class
                // is outside of the current R2R version bubble
                actualFieldOffset -= actualBaseOffset;
                actualBaseOffset = 0;
            }

            if ((fieldOffset != actualFieldOffset) || (baseOffset != actualBaseOffset))
            {
                // Verification failures are failfast events
                DefineFullyQualifiedNameForClass();

                SString fatalErrorString;
                fatalErrorString.Printf("Verify_FieldOffset '%s.%s' Field offset %d!=%d(actual) || baseOffset %d!=%d(actual)",
                    GetFullyQualifiedNameForClass(pEnclosingMT),
                    pField->GetName(),
                    fieldOffset,
                    actualFieldOffset,
                    baseOffset,
                    actualBaseOffset);
                _ASSERTE_MSG(false, fatalErrorString.GetUTF8());

                EEPOLICY_HANDLE_FATAL_ERROR_WITH_MESSAGE(-1, fatalErrorString.GetUnicode());
                return FALSE;
            }
            result = 1;
        }
        break;

    case ENCODE_VERIFY_VIRTUAL_FUNCTION_OVERRIDE:
    case ENCODE_CHECK_VIRTUAL_FUNCTION_OVERRIDE:
        {
            PCCOR_SIGNATURE updatedSignature = pBlob;

            ReadyToRunVirtualFunctionOverrideFlags flags = (ReadyToRunVirtualFunctionOverrideFlags)CorSigUncompressData(updatedSignature);

            SigTypeContext typeContext;    // empty context is OK: encoding should not contain type variables.
            ZapSig::Context zapSigContext(pInfoModule, (void *)currentModule, ZapSig::NormalTokens);
            MethodDesc *pDeclMethod = ZapSig::DecodeMethod(pInfoModule, updatedSignature, &typeContext, &zapSigContext, NULL, NULL, NULL, &updatedSignature, TRUE);
            TypeHandle thImpl = ZapSig::DecodeType(currentModule, pInfoModule, updatedSignature, CLASS_LOADED, &updatedSignature);

            MethodDesc *pImplMethodCompiler = NULL;

            if ((flags & READYTORUN_VIRTUAL_OVERRIDE_VirtualFunctionOverridden) != 0)
            {
                pImplMethodCompiler = ZapSig::DecodeMethod(currentModule, pInfoModule, updatedSignature);
            }

            MethodDesc *pImplMethodRuntime;
            if (pDeclMethod->IsInterface())
            {
                if (!thImpl.CanCastTo(pDeclMethod->GetMethodTable()))
                {
                    MethodTable *pInterfaceTypeCanonical = pDeclMethod->GetMethodTable()->GetCanonicalMethodTable();

                    // Its possible for the decl method to need to be found on the only canonically compatible interface on the owning type
                    MethodTable::InterfaceMapIterator it = thImpl.GetMethodTable()->IterateInterfaceMap();
                    while (it.Next())
                    {
                        MethodTable *pItfInMap = it.GetInterface(thImpl.GetMethodTable());
                        if (pInterfaceTypeCanonical == pItfInMap->GetCanonicalMethodTable())
                        {
                            pDeclMethod = MethodDesc::FindOrCreateAssociatedMethodDesc(pDeclMethod, pItfInMap, FALSE, pDeclMethod->GetMethodInstantiation(), FALSE, TRUE);
                            break;
                        }
                    }
                }
                DispatchSlot slot = thImpl.GetMethodTable()->FindDispatchSlotForInterfaceMD(pDeclMethod, /*throwOnConflict*/ FALSE);
                pImplMethodRuntime = slot.GetMethodDesc();
            }
            else
            {
                MethodTable *pCheckMT = thImpl.GetMethodTable();
                MethodTable *pBaseMT = pDeclMethod->GetMethodTable();
                WORD slot = pDeclMethod->GetSlot();

                while (pCheckMT != nullptr)
                {
                    if (pCheckMT->HasSameTypeDefAs(pBaseMT))
                    {
                        break;
                    }

                    pCheckMT = pCheckMT->GetParentMethodTable();
                }

                if (pCheckMT == nullptr)
                {
                    pImplMethodRuntime = NULL;
                }
                else if (IsMdFinal(pDeclMethod->GetAttrs()))
                {
                    pImplMethodRuntime = pDeclMethod;
                }
                else
                {
                    _ASSERTE(slot < pBaseMT->GetNumVirtuals());
                    pImplMethodRuntime = thImpl.GetMethodTable()->GetMethodDescForSlot(slot);
                }
            }

            if (pImplMethodRuntime != pImplMethodCompiler)
            {
                if (kind == ENCODE_CHECK_VIRTUAL_FUNCTION_OVERRIDE)
                {
                    return FALSE;
                }
                else
                {
                    // Verification failures are failfast events
                    DefineFullyQualifiedNameForClass();
                    SString methodNameDecl;
                    SString methodNameImplRuntime(W("(NULL)"));
                    SString methodNameImplCompiler(W("(NULL)"));

                    pDeclMethod->GetFullMethodInfo(methodNameDecl);

                    if (pImplMethodRuntime != NULL)
                    {
                        methodNameImplRuntime.Clear();
                        pImplMethodRuntime->GetFullMethodInfo(methodNameImplRuntime);
                    }

                    if (pImplMethodCompiler != NULL)
                    {
                        methodNameImplCompiler.Clear();
                        pImplMethodCompiler->GetFullMethodInfo(methodNameImplCompiler);
                    }

                    SString fatalErrorString;
                    fatalErrorString.Printf("Verify_VirtualFunctionOverride Decl Method '%s' on type '%s' is '%s'(actual) instead of expected '%s'(from compiler)",
                        methodNameDecl.GetUTF8(),
                        GetFullyQualifiedNameForClass(thImpl.GetMethodTable()),
                        methodNameImplRuntime.GetUTF8(),
                        methodNameImplCompiler.GetUTF8());

                    _ASSERTE_MSG(false, fatalErrorString.GetUTF8());
                    _ASSERTE(!IsDebuggerPresent() && "Stop on assert here instead of fatal error for ease of live debugging");

                    EEPOLICY_HANDLE_FATAL_ERROR_WITH_MESSAGE(-1, fatalErrorString.GetUnicode());
                    return FALSE;

                }
            }

            result = 1;
        }
        break;


    case ENCODE_CHECK_INSTRUCTION_SET_SUPPORT:
        {
            DWORD dwInstructionSetCount = CorSigUncompressData(pBlob);
            CORJIT_FLAGS corjitFlags = ExecutionManager::GetEEJitManager()->GetCPUCompileFlags();

            for (DWORD dwinstructionSetIndex = 0; dwinstructionSetIndex < dwInstructionSetCount; dwinstructionSetIndex++)
            {
                DWORD instructionSetEncoded = CorSigUncompressData(pBlob);
                bool mustInstructionSetBeSupported = !!(instructionSetEncoded & 1);
                ReadyToRunInstructionSet instructionSet = (ReadyToRunInstructionSet)(instructionSetEncoded >> 1);
                if (IsInstructionSetSupported(corjitFlags, instructionSet) != mustInstructionSetBeSupported)
                {
                    return FALSE;
                }
            }
            result = 1;
        }
        break;

    case ENCODE_CHECK_IL_BODY:
    case ENCODE_VERIFY_IL_BODY:
        {
            DWORD dwBlobSize = CorSigUncompressData(pBlob);
            const uint8_t *const pBlobStart = pBlob;
            pBlob += dwBlobSize;
            StackSArray<TypeHandle> types;
            DWORD cTypes = CorSigUncompressData(pBlob);
            bool fail = false;

            for (DWORD iType = 0; iType < cTypes && !fail; iType++)
            {
                if (kind == ENCODE_CHECK_IL_BODY)
                {
                    EX_TRY
                    {
                        types.Append(ZapSig::DecodeType(currentModule, pInfoModule, pBlob, CLASS_LOAD_APPROXPARENTS, &pBlob));
                    }
                    EX_CATCH
                    {
                        fail = true;
                    }
                    EX_END_CATCH(SwallowAllExceptions)

                }
                else
                {
                    types.Append(ZapSig::DecodeType(currentModule, pInfoModule, pBlob, CLASS_LOAD_APPROXPARENTS, &pBlob));
                }
            }

            MethodDesc *pMDCompare = NULL;

            if (!fail)
            {
                if (kind == ENCODE_CHECK_IL_BODY)
                {
                    EX_TRY
                    {
                        pMDCompare = ZapSig::DecodeMethod(currentModule, pInfoModule, pBlob);
                    }
                    EX_CATCH
                    {
                        fail = true;
                    }
                    EX_END_CATCH(SwallowAllExceptions)
                }
                else
                {
                    pMDCompare = ZapSig::DecodeMethod(currentModule, pInfoModule, pBlob);
                }
            }

            ReadyToRunStandaloneMethodMetadata *pMethodMetadata = NULL;
            if (!fail)
            {
                pMethodMetadata = GetReadyToRunStandaloneMethodMetadata(pMDCompare);
                if (pMethodMetadata == NULL)
                    fail = true;

                fail = fail || (pMethodMetadata->cByteData != dwBlobSize);
            }

            if (!fail)
            {
                fail = 0 != memcmp(pBlobStart, pMethodMetadata->pByteData, dwBlobSize);
            }

            if (!fail)
            {
                fail = cTypes != pMethodMetadata->cTypes;
            }

            if (!fail)
            {
                for (COUNT_T i = 0; i < cTypes && !fail; i++)
                {
                    fail = types[i] != pMethodMetadata->pTypes[i];
                }
            }

            if (!fail && !currentModule->GetReadyToRunInfo()->IsForbidProcessMoreILBodyFixups())
            {
                result = (size_t)pMDCompare;
            }
            else
            {
                if (kind == ENCODE_CHECK_IL_BODY || (!fail && currentModule->GetReadyToRunInfo()->IsForbidProcessMoreILBodyFixups()))
                {
                    return FALSE;
                }
                else
                {
                    DefineFullyQualifiedNameForClass();
                    SString methodName;
                    pMDCompare->GetFullMethodInfo(methodName);
                    void* compileTimeTypes = types.OpenRawBuffer();

                    int runtimeMethodDataSize = pMethodMetadata != NULL ? (int)pMethodMetadata->cByteData : 0;
                    void* runtimeMethodData = pMethodMetadata != NULL ? (void*)pMethodMetadata->pByteData : (void*)NULL;

                    int runtimeTypeCount = pMethodMetadata != NULL ? (int)pMethodMetadata->cTypes : 0;
                    void* runtimeTypeData = pMethodMetadata != NULL ? (void*)pMethodMetadata->pTypes : (void*)NULL;

                    SString fatalErrorString;
                    fatalErrorString.Printf("VERIFY_IL_BODY Method '%s' type '%s' does not match IL body expected DEBUGINFO MethodData {%d} {%p} RuntimeMethodData {%d} {%p} Types {%d} {%p} RuntimeTypes {%d} {%p}",
                        methodName.GetUTF8(),
                        GetFullyQualifiedNameForClass(pMDCompare->GetMethodTable()),
                        (int)dwBlobSize, pBlobStart,
                        runtimeMethodDataSize, runtimeMethodData,
                        (int)cTypes, compileTimeTypes,
                        runtimeTypeCount, runtimeTypeData
                        );

                    _ASSERTE_MSG(false, fatalErrorString.GetUTF8());
                    _ASSERTE(!IsDebuggerPresent() && "Stop on assert here instead of fatal error for ease of live debugging");

                    EEPOLICY_HANDLE_FATAL_ERROR_WITH_MESSAGE(-1, fatalErrorString.GetUnicode());
                    return FALSE;
                }
            }
            break;
        }
#endif // FEATURE_READYTORUN
    default:
        STRESS_LOG1(LF_ZAP, LL_WARNING, "Unknown FIXUP_BLOB_KIND %d\n", kind);
        _ASSERTE(!"Unknown FIXUP_BLOB_KIND");
        return FALSE;
    }

    MemoryBarrier();
    *entry = result;

    return TRUE;
}

bool CEEInfo::getTailCallHelpersInternal(CORINFO_RESOLVED_TOKEN* callToken,
                                         CORINFO_SIG_INFO* sig,
                                         CORINFO_GET_TAILCALL_HELPERS_FLAGS flags,
                                         CORINFO_TAILCALL_HELPERS* pResult)
{
    MethodDesc* pTargetMD = NULL;

    if (callToken != NULL)
    {
        pTargetMD = (MethodDesc*)callToken->hMethod;
        _ASSERTE(pTargetMD != NULL);

        if (pTargetMD->IsWrapperStub())
        {
            pTargetMD = pTargetMD->GetWrappedMethodDesc();
        }

        // We currently do not handle generating the proper call to managed
        // varargs methods.
        if (pTargetMD->IsVarArg())
        {
            return false;
        }
    }

    SigTypeContext typeCtx;
    GetTypeContext(&sig->sigInst, &typeCtx);

    MetaSig msig(sig->pSig, sig->cbSig, GetModule(sig->scope), &typeCtx);

    bool isCallvirt = (flags & CORINFO_TAILCALL_IS_CALLVIRT) != 0;
    bool isThisArgByRef = (flags & CORINFO_TAILCALL_THIS_ARG_IS_BYREF) != 0;

    MethodDesc* pStoreArgsMD;
    MethodDesc* pCallTargetMD;
    bool needsTarget;

    TailCallHelp::CreateTailCallHelperStubs(
        m_pMethodBeingCompiled, pTargetMD,
        msig, isCallvirt, isThisArgByRef, sig->hasTypeArg(),
        &pStoreArgsMD, &needsTarget,
        &pCallTargetMD);

    unsigned outFlags = 0;
    if (needsTarget)
    {
        outFlags |= CORINFO_TAILCALL_STORE_TARGET;
    }

    pResult->flags = (CORINFO_TAILCALL_HELPERS_FLAGS)outFlags;
    pResult->hStoreArgs = (CORINFO_METHOD_HANDLE)pStoreArgsMD;
    pResult->hCallTarget = (CORINFO_METHOD_HANDLE)pCallTargetMD;
    pResult->hDispatcher = (CORINFO_METHOD_HANDLE)TailCallHelp::GetOrLoadTailCallDispatcherMD();
    return true;
}

bool CEEInfo::getTailCallHelpers(CORINFO_RESOLVED_TOKEN* callToken,
                                 CORINFO_SIG_INFO* sig,
                                 CORINFO_GET_TAILCALL_HELPERS_FLAGS flags,
                                 CORINFO_TAILCALL_HELPERS* pResult)
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    } CONTRACTL_END;

    bool success = false;

    JIT_TO_EE_TRANSITION();

    success = getTailCallHelpersInternal(callToken, sig, flags, pResult);

    EE_TO_JIT_TRANSITION();

    return success;
}

bool CEEInfo::convertPInvokeCalliToCall(CORINFO_RESOLVED_TOKEN * pResolvedToken, bool fMustConvert)
{
    return false;
}

void CEEInfo::updateEntryPointForTailCall(CORINFO_CONST_LOOKUP* entryPoint)
{
    // No update necessary, all entry points are tail callable in runtime.
}

void CEEInfo::allocMem (AllocMemArgs *pArgs)
{
    LIMITED_METHOD_CONTRACT;
    UNREACHABLE();      // only called on derived class.
}

void CEEInfo::reserveUnwindInfo (
        bool                isFunclet,             /* IN */
        bool                isColdCode,            /* IN */
        uint32_t            unwindSize             /* IN */
        )
{
    LIMITED_METHOD_CONTRACT;
    UNREACHABLE();      // only called on derived class.
}

void CEEInfo::allocUnwindInfo (
        uint8_t *           pHotCode,              /* IN */
        uint8_t *           pColdCode,             /* IN */
        uint32_t            startOffset,           /* IN */
        uint32_t            endOffset,             /* IN */
        uint32_t            unwindSize,            /* IN */
        uint8_t *           pUnwindBlock,          /* IN */
        CorJitFuncKind      funcKind               /* IN */
        )
{
    LIMITED_METHOD_CONTRACT;
    UNREACHABLE();      // only called on derived class.
}

void * CEEInfo::allocGCInfo (
        size_t                  size        /* IN */
        )
{
    LIMITED_METHOD_CONTRACT;
    UNREACHABLE_RET();      // only called on derived class.
}

void CEEInfo::setEHcount (
        unsigned             cEH    /* IN */
        )
{
    LIMITED_METHOD_CONTRACT;
    UNREACHABLE();      // only called on derived class.
}

void CEEInfo::setEHinfo (
        unsigned             EHnumber,   /* IN  */
        const CORINFO_EH_CLAUSE *clause      /* IN */
        )
{
    LIMITED_METHOD_CONTRACT;
    UNREACHABLE();      // only called on derived class.
}

InfoAccessType CEEInfo::constructStringLiteral(CORINFO_MODULE_HANDLE scopeHnd,
                                               mdToken metaTok,
                                               void **ppValue)
{
    LIMITED_METHOD_CONTRACT;
    UNREACHABLE();      // only called on derived class.
}

InfoAccessType CEEInfo::emptyStringLiteral(void ** ppValue)
{
    LIMITED_METHOD_CONTRACT;
    UNREACHABLE();      // only called on derived class.
}

CORINFO_CLASS_HANDLE CEEInfo::getStaticFieldCurrentClass(CORINFO_FIELD_HANDLE fieldHnd,
                                                         bool* pIsSpeculative)
{
    LIMITED_METHOD_CONTRACT;
    UNREACHABLE();      // only called on derived class.
}

void* CEEInfo::getMethodSync(CORINFO_METHOD_HANDLE ftnHnd,
                             void **ppIndirection)
{
    LIMITED_METHOD_CONTRACT;
    UNREACHABLE();      // only called on derived class.
}

HRESULT CEEInfo::allocPgoInstrumentationBySchema(
            CORINFO_METHOD_HANDLE ftnHnd, /* IN */
            PgoInstrumentationSchema* pSchema, /* IN/OUT */
            uint32_t countSchemaItems, /* IN */
            uint8_t** pInstrumentationData /* OUT */
            )
{
    LIMITED_METHOD_CONTRACT;
    UNREACHABLE_RET();      // only called on derived class.
}

HRESULT CEEInfo::getPgoInstrumentationResults(
            CORINFO_METHOD_HANDLE      ftnHnd,
            PgoInstrumentationSchema **pSchema,                    // pointer to the schema table which describes the instrumentation results (pointer will not remain valid after jit completes)
            uint32_t *                 pCountSchemaItems,          // pointer to the count schema items
            uint8_t **                 pInstrumentationData,       // pointer to the actual instrumentation data (pointer will not remain valid after jit completes)
            PgoSource *                pPgoSource
            )
{
    LIMITED_METHOD_CONTRACT;
    UNREACHABLE_RET();      // only called on derived class.
}

void CEEInfo::recordCallSite(
        uint32_t              instrOffset,  /* IN */
        CORINFO_SIG_INFO *    callSig,      /* IN */
        CORINFO_METHOD_HANDLE methodHandle  /* IN */
        )
{
    LIMITED_METHOD_CONTRACT;
    UNREACHABLE();      // only called on derived class.
}

void CEEInfo::recordRelocation(
        void *                 location,   /* IN  */
        void *                 locationRW, /* IN  */
        void *                 target,     /* IN  */
        WORD                   fRelocType, /* IN  */
        INT32                  addlDelta /* IN  */
        )
{
    LIMITED_METHOD_CONTRACT;
    UNREACHABLE();      // only called on derived class.
}

WORD CEEInfo::getRelocTypeHint(void * target)
{
    LIMITED_METHOD_CONTRACT;
    UNREACHABLE_RET();      // only called on derived class.
}

uint32_t CEEInfo::getExpectedTargetArchitecture()
{
    LIMITED_METHOD_CONTRACT;

    return IMAGE_FILE_MACHINE_NATIVE;
}

void CEEInfo::setBoundaries(CORINFO_METHOD_HANDLE ftn, ULONG32 cMap,
                               ICorDebugInfo::OffsetMapping *pMap)
{
    LIMITED_METHOD_CONTRACT;
    UNREACHABLE();      // only called on derived class.
}

void CEEInfo::setVars(CORINFO_METHOD_HANDLE ftn, ULONG32 cVars, ICorDebugInfo::NativeVarInfo *vars)
{
    LIMITED_METHOD_CONTRACT;
    UNREACHABLE();      // only called on derived class.
}

void CEEInfo::reportRichMappings(
        ICorDebugInfo::InlineTreeNode*    inlineTreeNodes,
        uint32_t                          numInlineTreeNodes,
        ICorDebugInfo::RichOffsetMapping* mappings,
        uint32_t                          numMappings)
{
    LIMITED_METHOD_CONTRACT;
    UNREACHABLE();      // only called on derived class.
}

void CEEInfo::setPatchpointInfo(PatchpointInfo* patchpointInfo)
{
    LIMITED_METHOD_CONTRACT;
    UNREACHABLE();      // only called on derived class.
}

PatchpointInfo* CEEInfo::getOSRInfo(unsigned* ilOffset)
{
    LIMITED_METHOD_CONTRACT;
    UNREACHABLE();      // only called on derived class.
}

void* CEEInfo::getHelperFtn(CorInfoHelpFunc    ftnNum,         /* IN  */
                            void **            ppIndirection)  /* OUT */
{
    LIMITED_METHOD_CONTRACT;
    UNREACHABLE();      // only called on derived class.
}

void CEEInfo::GetProfilingHandle(bool                      *pbHookFunction,
                                 void                     **pProfilerHandle,
                                 bool                      *pbIndirectedHandles)
{
    LIMITED_METHOD_CONTRACT;
    UNREACHABLE();      // only called on derived class.
}

bool CEEInfo::notifyInstructionSetUsage(CORINFO_InstructionSet instructionSet,
                                        bool supportEnabled)
{
    LIMITED_METHOD_CONTRACT;
    // Do nothing. This api does not provide value in JIT scenarios and
    // crossgen does not utilize the api either.
    return supportEnabled;
}

#endif // !DACCESS_COMPILE

EECodeInfo::EECodeInfo()
{
    WRAPPER_NO_CONTRACT;

    m_codeAddress = NULL;

    m_pJM = NULL;
    m_pMD = NULL;
    m_relOffset = 0;

#ifdef FEATURE_EH_FUNCLETS
    m_pFunctionEntry = NULL;
#endif
}

void EECodeInfo::Init(PCODE codeAddress)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
    } CONTRACTL_END;

    Init(codeAddress, ExecutionManager::GetScanFlags());
}

void EECodeInfo::Init(PCODE codeAddress, ExecutionManager::ScanFlag scanFlag)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
    } CONTRACTL_END;

    m_codeAddress = codeAddress;

    RangeSection * pRS = ExecutionManager::FindCodeRange(codeAddress, scanFlag);
    if (pRS == NULL)
        goto Invalid;

    if (!pRS->_pjit->JitCodeToMethodInfo(pRS, codeAddress, &m_pMD, this))
        goto Invalid;

    m_pJM = pRS->_pjit;
    return;

Invalid:
    m_pJM = NULL;
    m_pMD = NULL;
    m_relOffset = 0;

#ifdef FEATURE_EH_FUNCLETS
    m_pFunctionEntry = NULL;
#endif
}

TADDR EECodeInfo::GetSavedMethodCode()
{
    CONTRACTL {
        // All EECodeInfo methods must be NOTHROW/GC_NOTRIGGER since they can
        // be used during GC.
        NOTHROW;
        GC_NOTRIGGER;
        HOST_NOCALLS;
        SUPPORTS_DAC;
    } CONTRACTL_END;
#ifndef HOST_64BIT
#if defined(HAVE_GCCOVER)

    PTR_GCCoverageInfo gcCover = GetNativeCodeVersion().GetGCCoverageInfo();
    _ASSERTE (!gcCover || GCStress<cfg_instr>::IsEnabled());
    if (GCStress<cfg_instr>::IsEnabled()
        && gcCover)
    {
        _ASSERTE(gcCover->savedCode);

        // Make sure we return the TADDR of savedCode here.  The byte array is not marshaled automatically.
        // The caller is responsible for any necessary marshaling.
        return PTR_TO_MEMBER_TADDR(GCCoverageInfo, gcCover, savedCode);
    }
#endif //defined(HAVE_GCCOVER)
#endif

    return GetStartAddress();
}

TADDR EECodeInfo::GetStartAddress()
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        HOST_NOCALLS;
        SUPPORTS_DAC;
    } CONTRACTL_END;

    return m_pJM->JitTokenToStartAddress(m_methodToken);
}

NativeCodeVersion EECodeInfo::GetNativeCodeVersion()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    PTR_MethodDesc pMD = PTR_MethodDesc(GetMethodDesc());
    if (pMD == NULL)
    {
        return NativeCodeVersion();
    }

#ifdef FEATURE_CODE_VERSIONING
    if (pMD->IsVersionable())
    {
        CodeVersionManager *pCodeVersionManager = pMD->GetCodeVersionManager();
        CodeVersionManager::LockHolder codeVersioningLockHolder;
        return pCodeVersionManager->GetNativeCodeVersion(pMD, PINSTRToPCODE(GetStartAddress()));
    }
#endif
    return NativeCodeVersion(pMD);
}

#if defined(FEATURE_EH_FUNCLETS)

// ----------------------------------------------------------------------------
// EECodeInfo::GetMainFunctionInfo
//
// Description:
//    Simple helper to transform a funclet's EECodeInfo into a parent function EECodeInfo.
//
// Return Value:
//    An EECodeInfo for the start of the main function body (offset 0).
//

EECodeInfo EECodeInfo::GetMainFunctionInfo()
{
    LIMITED_METHOD_CONTRACT;
    SUPPORTS_DAC;

    EECodeInfo result = *this;
    result.m_relOffset = 0;
    result.m_codeAddress = this->GetStartAddress();
    result.m_pFunctionEntry = NULL;

    return result;
}

PTR_RUNTIME_FUNCTION EECodeInfo::GetFunctionEntry()
{
    LIMITED_METHOD_CONTRACT;
    SUPPORTS_DAC;

    if (m_pFunctionEntry == NULL)
        m_pFunctionEntry = m_pJM->LazyGetFunctionEntry(this);
    return m_pFunctionEntry;
}

#if defined(TARGET_AMD64)

BOOL EECodeInfo::HasFrameRegister()
{
    LIMITED_METHOD_CONTRACT;

    PTR_RUNTIME_FUNCTION pFuncEntry = GetFunctionEntry();
    _ASSERTE(pFuncEntry != NULL);

    BOOL fHasFrameRegister = FALSE;
    PUNWIND_INFO pUnwindInfo = (PUNWIND_INFO)(GetModuleBase() + pFuncEntry->UnwindData);
    if (pUnwindInfo->FrameRegister != 0)
    {
        fHasFrameRegister = TRUE;
        _ASSERTE(pUnwindInfo->FrameRegister == kRBP);
    }

    return fHasFrameRegister;
}
#endif // defined(TARGET_AMD64)

#endif // defined(FEATURE_EH_FUNCLETS)


#if defined(TARGET_AMD64)
// ----------------------------------------------------------------------------
// EECodeInfo::GetUnwindInfoHelper
//
// Description:
//    Simple helper to return a pointer to the UNWIND_INFO given the offset to the unwind info.
//    On DAC builds, this function will read the memory from the target process and create a host copy.
//
// Arguments:
//    * unwindInfoOffset - This is the offset to the unwind info, relative to the beginning of the code heap
//        for jitted code or to the module base for ngned code. this->GetModuleBase() will return the correct
//        module base.
//
// Return Value:
//    Return a pointer to the UNWIND_INFO.  On DAC builds, this function will create a host copy of the
//    UNWIND_INFO and return a host pointer.  It will correctly read all of the memory for the variable-sized
//    unwind info.
//

UNWIND_INFO * EECodeInfo::GetUnwindInfoHelper(ULONG unwindInfoOffset)
{
#if defined(DACCESS_COMPILE)
    return DacGetUnwindInfo(static_cast<TADDR>(this->GetModuleBase() + unwindInfoOffset));
#else  // !DACCESS_COMPILE
    UNWIND_INFO * pUnwindInfo = reinterpret_cast<UNWIND_INFO *>(this->GetModuleBase() + unwindInfoOffset);
#if defined(TARGET_AMD64)
    if (pUnwindInfo->Flags & UNW_FLAG_CHAININFO)
    {
        unwindInfoOffset = ((PTR_RUNTIME_FUNCTION)(&(pUnwindInfo->UnwindCode)))->UnwindData;
        pUnwindInfo = reinterpret_cast<UNWIND_INFO *>(this->GetModuleBase() + unwindInfoOffset);
    }
#endif // TARGET_AMD64
    return pUnwindInfo;
#endif // !DACCESS_COMPILE
}

// ----------------------------------------------------------------------------
// EECodeInfo::GetFixedStackSize
//
// Description:
//    Return the fixed stack size of a specified managed method.  This function DOES NOT take current control
//    PC into account.  So the fixed stack size returned by this function is not valid in the prolog or
//    the epilog.
//
// Return Value:
//    Return the fixed stack size.
//
// Notes:
//    * For method with dynamic stack allocations, this function will return the fixed stack size on X64 (the
//        stack size immediately after the prolog), and it will return 0 on IA64. This difference is due to
//        the different unwind info encoding.
//

ULONG EECodeInfo::GetFixedStackSize()
{
    WRAPPER_NO_CONTRACT;
    SUPPORTS_DAC;

    ULONG uFixedStackSize = 0;

    ULONG uDummy = 0;
    GetOffsetsFromUnwindInfo(&uFixedStackSize, &uDummy);

    return uFixedStackSize;
}

#define kRBP    5
// The information returned by this method is only valid if we are not in a prolog or an epilog.
// Since this method is only used for the security stackwalk cache and EnC transition, this assumption is
// valid, since we cannot see these in a prolog or an epilog.
//
// The next assumption is that only rbp is used as a frame register in jitted code.  There is an
// assert below to guard this assumption.
void EECodeInfo::GetOffsetsFromUnwindInfo(ULONG* pRSPOffset, ULONG* pRBPOffset)
{
    LIMITED_METHOD_CONTRACT;
    SUPPORTS_DAC;

    _ASSERTE((pRSPOffset != NULL) && (pRBPOffset != NULL));

    // moduleBase is a target address.
    TADDR moduleBase = GetModuleBase();

    DWORD unwindInfo = RUNTIME_FUNCTION__GetUnwindInfoAddress(GetFunctionEntry());

    if ((unwindInfo & RUNTIME_FUNCTION_INDIRECT) != 0)
    {
        unwindInfo = RUNTIME_FUNCTION__GetUnwindInfoAddress(PTR_RUNTIME_FUNCTION(moduleBase + (unwindInfo & ~RUNTIME_FUNCTION_INDIRECT)));
    }

    UNWIND_INFO * pInfo = GetUnwindInfoHelper(unwindInfo);
    _ASSERTE((pInfo->Flags & UNW_FLAG_CHAININFO) == 0);

    // Either we are not using a frame pointer, or we are using rbp as the frame pointer.
    if ( (pInfo->FrameRegister != 0) && (pInfo->FrameRegister != kRBP) )
    {
        _ASSERTE(!"GetRbpOffset() - non-RBP frame pointer used, violating assumptions of the security stackwalk cache");
        DebugBreak();
    }

    // Walk the unwind info.
    ULONG StackOffset     = 0;
    ULONG StackSize       = 0;
    for (int i = 0; i < pInfo->CountOfUnwindCodes; i++)
    {
        ULONG UnwindOp = pInfo->UnwindCode[i].UnwindOp;
        ULONG OpInfo   = pInfo->UnwindCode[i].OpInfo;

        if (UnwindOp == UWOP_SAVE_NONVOL)
        {
            if (OpInfo == kRBP)
            {
                StackOffset = pInfo->UnwindCode[i+1].FrameOffset * 8;
            }
        }
        else if (UnwindOp == UWOP_SAVE_NONVOL_FAR)
        {
            if (OpInfo == kRBP)
            {
                StackOffset  =  pInfo->UnwindCode[i + 1].FrameOffset;
                StackOffset += (pInfo->UnwindCode[i + 2].FrameOffset << 16);
            }
        }
        else if (UnwindOp == UWOP_ALLOC_SMALL)
        {
            StackSize += (OpInfo * 8) + 8;
        }
        else if (UnwindOp == UWOP_ALLOC_LARGE)
        {
            ULONG IncrementalStackSize = pInfo->UnwindCode[i + 1].FrameOffset;
            if (OpInfo == 0)
            {
                IncrementalStackSize *= 8;
            }
            else
            {
                IncrementalStackSize += (pInfo->UnwindCode[i + 2].FrameOffset << 16);

                // This is a special opcode.  We need to increment the index by 1 in addition to the normal adjustments.
                i += 1;
            }
            StackSize += IncrementalStackSize;
        }
        else if (UnwindOp == UWOP_PUSH_NONVOL)
        {
            // Because of constraints on epilogs, this unwind opcode is always last in the unwind code array.
            // This means that StackSize has been initialized already when we first see this unwind opcode.
            // Note that the initial value of StackSize does not include the stack space used for pushes.
            // Thus, here we only need to increment StackSize 8 bytes at a time until we see the unwind code for "push rbp".
            if (OpInfo == kRBP)
            {
                StackOffset = StackSize;
            }

            StackSize += 8;
        }

        // Adjust the index into the unwind code array.
        i += UnwindOpExtraSlotTable[UnwindOp];
    }

    *pRSPOffset = StackSize + 8;        // add 8 for the return address
    *pRBPOffset = StackOffset;
}
#undef kRBP

ULONG EECodeInfo::GetFrameOffsetFromUnwindInfo()
{
    WRAPPER_NO_CONTRACT;

    SUPPORTS_DAC;

    // moduleBase is a target address.
    TADDR moduleBase = GetModuleBase();

    DWORD unwindInfo = RUNTIME_FUNCTION__GetUnwindInfoAddress(GetFunctionEntry());

    if ((unwindInfo & RUNTIME_FUNCTION_INDIRECT) != 0)
    {
        unwindInfo = RUNTIME_FUNCTION__GetUnwindInfoAddress(PTR_RUNTIME_FUNCTION(moduleBase + (unwindInfo & ~RUNTIME_FUNCTION_INDIRECT)));
    }

    UNWIND_INFO * pInfo = GetUnwindInfoHelper(unwindInfo);
    _ASSERTE((pInfo->Flags & UNW_FLAG_CHAININFO) == 0);

    // Either we are not using a frame pointer, or we are using rbp as the frame pointer.
    if ( (pInfo->FrameRegister != 0) && (pInfo->FrameRegister != kRBP) )
    {
        _ASSERTE(!"GetRbpOffset() - non-RBP frame pointer used, violating assumptions of the security stackwalk cache");
        DebugBreak();
    }

    ULONG frameOffset = pInfo->FrameOffset;
#ifdef UNIX_AMD64_ABI
    if ((frameOffset == 15) && (pInfo->UnwindCode[0].UnwindOp == UWOP_SET_FPREG_LARGE))
    {
        frameOffset = *(ULONG*)&pInfo->UnwindCode[1];
    }
#endif

    return frameOffset;
}


#if defined(_DEBUG) && defined(HAVE_GCCOVER)

LPVOID                EECodeInfo::findNextFunclet (LPVOID pvFuncletStart, SIZE_T cbCode, LPVOID *ppvFuncletEnd)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
    } CONTRACTL_END;

    while (cbCode > 0)
    {
        PT_RUNTIME_FUNCTION   pFunctionEntry;
        ULONGLONG           uImageBase;
#ifdef TARGET_UNIX
        EECodeInfo codeInfo;
        codeInfo.Init((PCODE)pvFuncletStart);
        pFunctionEntry = codeInfo.GetFunctionEntry();
        uImageBase = (ULONGLONG)codeInfo.GetModuleBase();
#else // !TARGET_UNIX
        //
        // This is GCStress debug only - use the slow OS APIs to enumerate funclets
        //

        pFunctionEntry = (PT_RUNTIME_FUNCTION) RtlLookupFunctionEntry((ULONGLONG)pvFuncletStart,
                              &uImageBase
                              AMD64_ARG(NULL)
                              );
#endif

        if (pFunctionEntry != NULL)
        {

            _ASSERTE((TADDR)pvFuncletStart == (TADDR)uImageBase + pFunctionEntry->BeginAddress);
            _ASSERTE((TADDR)uImageBase + pFunctionEntry->EndAddress <= (TADDR)pvFuncletStart + cbCode);
            *ppvFuncletEnd = (LPVOID)(uImageBase + pFunctionEntry->EndAddress);
            return (LPVOID)(uImageBase + pFunctionEntry->BeginAddress);
        }

        pvFuncletStart = (LPVOID)((TADDR)pvFuncletStart + 1);
        cbCode--;
    }

    return NULL;
}
#endif // defined(_DEBUG) && !defined(HAVE_GCCOVER)
#endif // defined(TARGET_AMD64)
