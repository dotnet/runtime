// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
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
#include "security.h"
#include "securitymeta.h"
#include "dllimport.h"
#include "gc.h"
#include "comdelegate.h"
#include "jitperf.h" // to track jit perf
#include "corprof.h"
#include "eeprofinterfaces.h"
#ifdef FEATURE_REMOTING
#include "remoting.h" // create context bound and remote class instances
#endif
#include "perfcounters.h"
#ifdef PROFILING_SUPPORTED
#include "proftoeeinterfaceimpl.h"
#include "eetoprofinterfaceimpl.h"
#include "eetoprofinterfaceimpl.inl"
#include "profilepriv.h"
#endif
#include "tls.h"
#include "ecall.h"
#include "generics.h"
#include "typestring.h"
#include "stackprobe.h"
#include "typedesc.h"
#include "genericdict.h"
#include "array.h"
#include "debuginfostore.h"
#include "constrainedexecutionregion.h"
#include "security.h"
#include "safemath.h"
#include "runtimehandles.h"
#include "sigbuilder.h"
#include "openum.h"
#ifdef HAVE_GCCOVER
#include "gccover.h"
#endif // HAVE_GCCOVER

#include "mdaassistants.h"

#ifdef FEATURE_PREJIT
#include "compile.h"
#include "corcompile.h"
#endif // FEATURE_PREJIT


#ifdef FEATURE_INTERPRETER
#include "interpreter.h"
#endif // FEATURE_INTERPRETER

// The Stack Overflow probe takes place in the COOPERATIVE_TRANSITION_BEGIN() macro
//

#define JIT_TO_EE_TRANSITION()          MAKE_CURRENT_THREAD_AVAILABLE_EX(m_pThread);                \
                                        _ASSERTE(CURRENT_THREAD == GetThread());                    \
                                        INSTALL_UNWIND_AND_CONTINUE_HANDLER_NO_PROBE;               \
                                        COOPERATIVE_TRANSITION_BEGIN();                             \
                                        START_NON_JIT_PERF();

#define EE_TO_JIT_TRANSITION()          STOP_NON_JIT_PERF();                                        \
                                        COOPERATIVE_TRANSITION_END();                               \
                                        UNINSTALL_UNWIND_AND_CONTINUE_HANDLER_NO_PROBE;

#define JIT_TO_EE_TRANSITION_LEAF()
#define EE_TO_JIT_TRANSITION_LEAF()


#if defined(CROSSGEN_COMPILE)
static const char *const hlpNameTable[CORINFO_HELP_COUNT] = {
#define JITHELPER(code, pfnHelper, sig) #code,
#include "jithelpers.h"
};
#endif

#ifdef DACCESS_COMPILE

// The real definitions are in jithelpers.cpp. However, those files are not included in the DAC build.
// Hence, we add them here.
GARY_IMPL(VMHELPDEF, hlpFuncTable, CORINFO_HELP_COUNT);
GARY_IMPL(VMHELPDEF, hlpDynamicFuncTable, DYNAMIC_CORINFO_HELP_COUNT);

#else // DACCESS_COMPILE

/*********************************************************************/

#if defined(ENABLE_PERF_COUNTERS)
LARGE_INTEGER g_lastTimeInJitCompilation;
#endif

BOOL canReplaceMethodOnStack(MethodDesc* pReplaced, MethodDesc* pDeclaredReplacer, MethodDesc* pExactReplacer);

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
BOOL ModifyCheckForDynamicMethod(DynamicResolver *pResolver,
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
        *pAccessCheckType = AccessCheckOptions::kRestrictedMemberAccess;

#ifdef FEATURE_CORECLR
        // For compatibility, don't do transparency checks from dynamic methods in FT CoreCLR.
        if (GetAppDomain()->GetSecurityDescriptor()->IsFullyTrusted())
            *pAccessCheckType = AccessCheckOptions::kRestrictedMemberAccessNoTransparency;
#endif // FEATURE_CORECLR

#ifdef FEATURE_COMPRESSEDSTACK
        if (dwSecurityFlags & DynamicResolver::HasCreationContext)
            *ppAccessContext = pResolver;
#endif // FEATURE_COMPRESSEDSTACK
    }
    else
    {
#ifdef FEATURE_CORECLR
        // For compatibility, don't do transparency checks from dynamic methods in FT CoreCLR.
        if (GetAppDomain()->GetSecurityDescriptor()->IsFullyTrusted())
            *pAccessCheckType = AccessCheckOptions::kNormalAccessNoTransparency;
#endif // FEATURE_CORECLR
    }

    return doAccessCheck;
}

/*****************************************************************************/

// Initialize from data we passed across to the JIT
inline static void GetTypeContext(const CORINFO_SIG_INST *info, SigTypeContext *pTypeContext)
{
    LIMITED_METHOD_CONTRACT;
    SigTypeContext::InitTypeContext(
        Instantiation((TypeHandle *) info->classInst, info->classInstCount), 
        Instantiation((TypeHandle *) info->methInst, info->methInstCount), 
        pTypeContext);
}

static MethodDesc* GetMethodFromContext(CORINFO_CONTEXT_HANDLE context)
{
    LIMITED_METHOD_CONTRACT;
    if (((size_t) context & CORINFO_CONTEXTFLAGS_MASK) == CORINFO_CONTEXTFLAGS_CLASS)
    {
        return NULL;
    }
    else
    {
        return GetMethod((CORINFO_METHOD_HANDLE)((size_t) context & ~CORINFO_CONTEXTFLAGS_MASK));
    }
}

static TypeHandle GetTypeFromContext(CORINFO_CONTEXT_HANDLE context)
{
    LIMITED_METHOD_CONTRACT;
    if (((size_t) context & CORINFO_CONTEXTFLAGS_MASK) == CORINFO_CONTEXTFLAGS_CLASS)
    {
        return TypeHandle((CORINFO_CLASS_HANDLE) ((size_t) context & ~CORINFO_CONTEXTFLAGS_MASK));
    }
    else
    {
        MethodTable * pMT = GetMethodFromContext(context)->GetMethodTable();
        return TypeHandle(pMT);
    }
}

// Initialize from a context parameter passed to the JIT and back.  This is a parameter
// that indicates which method is being jitted.

inline static void GetTypeContext(CORINFO_CONTEXT_HANDLE context, SigTypeContext *pTypeContext)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        SO_TOLERANT;
        MODE_ANY;
        PRECONDITION(context != NULL);
    }
    CONTRACTL_END;
    if (GetMethodFromContext(context))
    {
        SigTypeContext::InitTypeContext(GetMethodFromContext(context), pTypeContext);
    }
    else
    {
        SigTypeContext::InitTypeContext(GetTypeFromContext(context), pTypeContext);
    }
}

static BOOL ContextIsShared(CORINFO_CONTEXT_HANDLE context)
{
    LIMITED_METHOD_CONTRACT;
    MethodDesc *pContextMD = GetMethodFromContext(context);
    if (pContextMD != NULL)
    {
        return pContextMD->IsSharedByGenericInstantiations();
    }
    else
    {
        // Type handle contexts are non-shared and are used for inlining of
        // non-generic methods in generic classes
        return FALSE;
    }
}

// Returns true if context is providing any generic variables
static BOOL ContextIsInstantiated(CORINFO_CONTEXT_HANDLE context)
{
    LIMITED_METHOD_CONTRACT;
    if (GetMethodFromContext(context))
    {
        return GetMethodFromContext(context)->HasClassOrMethodInstantiation();
    }
    else
    {
        return GetTypeFromContext(context).HasInstantiation();
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
        // handle means it is is a primitive.
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

    CorInfoType res = (eeType < ELEMENT_TYPE_MAX) ? ((CorInfoType) map[eeType]) : CORINFO_TYPE_UNDEF;

    if (clsRet)
        *clsRet = CORINFO_CLASS_HANDLE(typeHndUpdated.AsPtr());

    RETURN res;
}


inline static CorInfoType toJitType(TypeHandle typeHnd, CORINFO_CLASS_HANDLE *clsRet = NULL)
{
    WRAPPER_NO_CONTRACT;
    return CEEInfo::asCorInfoType(typeHnd.GetInternalCorElementType(), typeHnd, clsRet);
}

#ifdef _DEBUG
void DebugSecurityCalloutStress(CORINFO_METHOD_HANDLE methodBeingCompiledHnd,
                                CorInfoIsAccessAllowedResult& currentAnswer,
                                CorInfoSecurityRuntimeChecks& currentRuntimeChecks)
{
    WRAPPER_NO_CONTRACT;
    if (currentAnswer != CORINFO_ACCESS_ALLOWED)
    {
        return;
    }
    static ConfigDWORD AlwaysInsertCallout;
    switch (AlwaysInsertCallout.val(CLRConfig::INTERNAL_Security_AlwaysInsertCallout))
    {
    case 0: //No stress
        return;
    case 1: //Always
        break;
    default: //2 (or anything else), do so half the time
        if (((size_t(methodBeingCompiledHnd) / sizeof(void*)) % 64) < 32)
            return;
    }
    //Do the stress
    currentAnswer = CORINFO_ACCESS_RUNTIME_CHECK;
    currentRuntimeChecks = CORINFO_ACCESS_SECURITY_NONE;
}
#else
#define DebugSecurityCalloutStress(a, b, c) do {} while(0)
#endif //_DEBUG

void CheckForEquivalenceAndLoadTypeBeforeCodeIsRun(Module *pModule, mdToken token, Module *pDefModule, mdToken defToken, const SigParser *ptr, SigTypeContext *pTypeContext, void *pData)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        SO_INTOLERANT;
    }
    CONTRACTL_END;

    if (IsTypeDefEquivalent(defToken, pDefModule))
    {
        SigPointer sigPtr(*ptr);
        TypeHandle th = sigPtr.GetTypeHandleThrowing(pModule, pTypeContext);
        ((ICorDynamicInfo *)pData)->classMustBeLoadedBeforeCodeIsRun(CORINFO_CLASS_HANDLE(th.AsPtr()));
    }
}

inline static void TypeEquivalenceFixupSpecificationHelper(ICorDynamicInfo * pCorInfo, MethodDesc *pMD)
{
    STANDARD_VM_CONTRACT;

    // A fixup is necessary to ensure that the parameters to the method are loaded before the method
    // is called. In these cases we will not perform the appropriate loading when we load parameter
    // types because with type equivalence, the parameter types at the call site do not necessarily
    // match that those in the actual function. (They must be equivalent, but not necessarily the same.)
    // In non-ngen scenarios this code here will force the types to be loaded directly by the call to
    // HasTypeEquivalentStructParameters.
    if (!pMD->IsVirtual())
    {
        if (pMD->HasTypeEquivalentStructParameters())
        {
            if (IsCompilationProcess())
                pMD->WalkValueTypeParameters(pMD->GetMethodTable(), CheckForEquivalenceAndLoadTypeBeforeCodeIsRun, pCorInfo);
        }
    }
    else
    {
        if (pMD->GetMethodTable()->DependsOnEquivalentOrForwardedStructs())
        {
            if (pMD->HasTypeEquivalentStructParameters())
                pCorInfo->classMustBeLoadedBeforeCodeIsRun((CORINFO_CLASS_HANDLE)pMD->GetMethodTable());
        }
    }
}

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
//static
void 
CEEInfo::ConvToJitSig(
    PCCOR_SIGNATURE       pSig, 
    DWORD                 cbSig, 
    CORINFO_MODULE_HANDLE scopeHnd, 
    mdToken               token, 
    CORINFO_SIG_INFO *    sigRet,
    MethodDesc *          pContextMD,
    bool                  localSig,
    TypeHandle            contextType)
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
    } CONTRACTL_END;

    SigTypeContext typeContext;

    if (pContextMD)
    {
        SigTypeContext::InitTypeContext(pContextMD, contextType, &typeContext);
    }
    else
    {
        SigTypeContext::InitTypeContext(contextType, &typeContext);
    }

    _ASSERTE(CORINFO_CALLCONV_DEFAULT == (CorInfoCallConv) IMAGE_CEE_CS_CALLCONV_DEFAULT);
    _ASSERTE(CORINFO_CALLCONV_VARARG == (CorInfoCallConv) IMAGE_CEE_CS_CALLCONV_VARARG);
    _ASSERTE(CORINFO_CALLCONV_MASK == (CorInfoCallConv) IMAGE_CEE_CS_CALLCONV_MASK);
    _ASSERTE(CORINFO_CALLCONV_HASTHIS == (CorInfoCallConv) IMAGE_CEE_CS_CALLCONV_HASTHIS);

    TypeHandle typeHnd = TypeHandle();

    sigRet->pSig = pSig;
    sigRet->cbSig = cbSig;
    sigRet->retTypeClass = 0;
    sigRet->retTypeSigClass = 0;
    sigRet->scope = scopeHnd;
    sigRet->token = token;
    sigRet->sigInst.classInst = (CORINFO_CLASS_HANDLE *) typeContext.m_classInst.GetRawArgs();
    sigRet->sigInst.classInstCount = (unsigned) typeContext.m_classInst.GetNumArgs();
    sigRet->sigInst.methInst = (CORINFO_CLASS_HANDLE *) typeContext.m_methodInst.GetRawArgs();
    sigRet->sigInst.methInstCount = (unsigned) typeContext.m_methodInst.GetNumArgs();

    SigPointer sig(pSig, cbSig);

    if (!localSig)
    {
        // This is a method signature which includes calling convention, return type, 
        // arguments, etc

        _ASSERTE(!sig.IsNull());
        Module * module = GetModule(scopeHnd);
        sigRet->flags = 0;

        ULONG data;
        IfFailThrow(sig.GetCallingConvInfo(&data));
        sigRet->callConv = (CorInfoCallConv) data;
        // Skip number of type arguments
        if (sigRet->callConv & IMAGE_CEE_CS_CALLCONV_GENERIC)
          IfFailThrow(sig.GetData(NULL));

        ULONG numArgs;
        IfFailThrow(sig.GetData(&numArgs));
        if (numArgs != (unsigned short) numArgs)
            COMPlusThrowHR(COR_E_INVALIDPROGRAM);

        sigRet->numArgs = (unsigned short) numArgs;

        CorElementType type = sig.PeekElemTypeClosed(module, &typeContext);

        if (!CorTypeInfo::IsPrimitiveType(type))
        {
            typeHnd = sig.GetTypeHandleThrowing(module, &typeContext);
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

        sigRet->callConv = CORINFO_CALLCONV_DEFAULT;
        sigRet->retType = CORINFO_TYPE_VOID;
        sigRet->flags   = CORINFO_SIGFLAG_IS_LOCAL_SIG;
        sigRet->numArgs = 0;
        if (!sig.IsNull())
        {
            ULONG callConv;
            IfFailThrow(sig.GetCallingConvInfo(&callConv));
            if (callConv != IMAGE_CEE_CS_CALLCONV_LOCAL_SIG)
            {
                COMPlusThrowHR(COR_E_BADIMAGEFORMAT, BFA_CALLCONV_NOT_LOCAL_SIG);
            }

            ULONG numArgs;
            IfFailThrow(sig.GetData(&numArgs));
            
            if (numArgs != (unsigned short) numArgs)
                COMPlusThrowHR(COR_E_INVALIDPROGRAM);

            sigRet->numArgs = (unsigned short) numArgs;
        }

        sigRet->args = (CORINFO_ARG_LIST_HANDLE)sig.GetPtr();
    }

    _ASSERTE(SigInfoFlagsAreValid(sigRet));
} // CEEInfo::ConvToJitSig

//---------------------------------------------------------------------------------------
// 
CORINFO_CLASS_HANDLE CEEInfo::getTokenTypeAsHandle (CORINFO_RESOLVED_TOKEN * pResolvedToken)
{
    CONTRACTL {
        SO_TOLERANT;
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

    tokenType = CORINFO_CLASS_HANDLE(MscorlibBinder::GetClass(classID));

    EE_TO_JIT_TRANSITION();

    return tokenType;
}

/*********************************************************************/
size_t CEEInfo::findNameOfToken (
            CORINFO_MODULE_HANDLE       scopeHnd,
            mdToken                     metaTOK,
            __out_ecount (FQNameCapacity)  char * szFQName,
            size_t FQNameCapacity)
{
    CONTRACTL {
        SO_TOLERANT;
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    } CONTRACTL_END;

    size_t NameLen = 0;

    JIT_TO_EE_TRANSITION();

    if (IsDynamicScope(scopeHnd))
    {
        strncpy_s (szFQName, FQNameCapacity, "DynamicToken", FQNameCapacity - 1);
        NameLen = strlen (szFQName);
    }
    else
    {
        Module* module = (Module *)scopeHnd;
        NameLen = findNameOfToken(module, metaTOK, szFQName, FQNameCapacity);
    }
        
    EE_TO_JIT_TRANSITION();

    return NameLen;
}

CorInfoCanSkipVerificationResult CEEInfo::canSkipMethodVerification(CORINFO_METHOD_HANDLE ftnHnd)
{
    CONTRACTL {
        SO_TOLERANT;
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    } CONTRACTL_END;

    CorInfoCanSkipVerificationResult canSkipVerif = CORINFO_VERIFICATION_CANNOT_SKIP;

    JIT_TO_EE_TRANSITION();

    MethodDesc* pMD = GetMethod(ftnHnd);


#ifdef _DEBUG
    if (g_pConfig->IsVerifierOff())
    {
        canSkipVerif = CORINFO_VERIFICATION_CAN_SKIP;
    }
    else
#endif // _DEBUG
    {
        canSkipVerif = Security::JITCanSkipVerification(pMD);
    }

    EE_TO_JIT_TRANSITION();

    return canSkipVerif;

}

/*********************************************************************/
BOOL CEEInfo::shouldEnforceCallvirtRestriction(
        CORINFO_MODULE_HANDLE scopeHnd)
{
    LIMITED_METHOD_CONTRACT;
    // see vsw 599197
    // verification rule added in whidbey requiring virtual methods
    // to be called via callvirt except if certain other rules are
    // obeyed.

    if (g_pConfig->LegacyVirtualMethodCallVerification())
        return false;
    else 
        return true;
       
}

#ifdef FEATURE_READYTORUN_COMPILER

// Returns true if assemblies are in the same version bubble
// Right now each assembly is in its own version bubble.
// If the need arises (i.e. performance issues) we will define sets of assemblies (e.g. all app assemblies)
// The main point is that all this logic is concentrated in one place.

bool IsInSameVersionBubble(Assembly * current, Assembly * target)
{
    LIMITED_METHOD_CONTRACT;

    // trivial case: current and target are identical
    if (current == target)
        return true;

    return false;
}

// Returns true if the assemblies defining current and target are in the same version bubble
static bool IsInSameVersionBubble(MethodDesc* pCurMD, MethodDesc *pTargetMD)
{
    LIMITED_METHOD_CONTRACT;
    if (IsInSameVersionBubble(pCurMD->GetModule()->GetAssembly(),
                              pTargetMD->GetModule()->GetAssembly()))
    {
        return true;
    }
    if (IsReadyToRunCompilation())
    {
        if (pTargetMD->GetModule()->GetMDImport()->GetCustomAttributeByName(pTargetMD->GetMemberDef(),
                NONVERSIONABLE_TYPE, NULL, NULL) == S_OK)
        {
            return true;
        }
    }
    return false;

}

#endif // FEATURE_READYTORUN_COMPILER


/*********************************************************************/
CorInfoCanSkipVerificationResult CEEInfo::canSkipVerification(
        CORINFO_MODULE_HANDLE moduleHnd)
{
    CONTRACTL {
        SO_TOLERANT;
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    } CONTRACTL_END;

    CorInfoCanSkipVerificationResult canSkipVerif = CORINFO_VERIFICATION_CANNOT_SKIP;

    JIT_TO_EE_TRANSITION();

    Assembly * pAssem = GetModule(moduleHnd)->GetAssembly();

#ifdef _DEBUG
    if (g_pConfig->IsVerifierOff())
    {
        canSkipVerif = CORINFO_VERIFICATION_CAN_SKIP;
    }
    else
#endif // _DEBUG
    {
        //
        // fQuickCheckOnly is set only by calls from Zapper::CompileAssembly
        // because that allows us make a determination for the most
        // common full trust scenarios (local machine) without actually
        // resolving policy and bringing in a whole list of assembly
        // dependencies.
        //
        // The scenario of interest here is determing whether or not an
        // assembly MVID comparison is enough when loading an NGEN'd
        // assembly or if a full binary hash comparison must be done.
        //

        DomainAssembly * pAssembly = pAssem->GetDomainAssembly();
        canSkipVerif = Security::JITCanSkipVerification(pAssembly);
    }

    EE_TO_JIT_TRANSITION();

    return canSkipVerif;
}

/*********************************************************************/
// Checks if the given metadata token is valid
BOOL CEEInfo::isValidToken (
        CORINFO_MODULE_HANDLE       module,
        mdToken                     metaTOK)
{
    CONTRACTL {
        SO_TOLERANT;
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    } CONTRACTL_END;

    BOOL result = FALSE;

    JIT_TO_EE_TRANSITION();

    if (IsDynamicScope(module))
    {
        // No explicit token validation for dynamic code. Validation is
        // side-effect of token resolution.
        result = TRUE;
    }
    else
    {
        result = ((Module *)module)->GetMDImport()->IsValidToken(metaTOK);
    }

    EE_TO_JIT_TRANSITION();

    return result;
}

/*********************************************************************/
// Checks if the given metadata token is valid StringRef
BOOL CEEInfo::isValidStringRef (
        CORINFO_MODULE_HANDLE       module,
        mdToken                     metaTOK)
{
    CONTRACTL {
        SO_TOLERANT;
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    } CONTRACTL_END;

    BOOL result = FALSE;

    JIT_TO_EE_TRANSITION();

    if (IsDynamicScope(module))
    {
        result = GetDynamicResolver(module)->IsValidStringRef(metaTOK);
    }
    else
    {
        result = ((Module *)module)->CheckStringRef(metaTOK);
        if (result)
        {
            DWORD dwCharCount;    
            LPCWSTR pString;
            result = (!FAILED(((Module *)module)->GetMDImport()->GetUserString(metaTOK, &dwCharCount, NULL, &pString)) &&
                     pString != NULL);
        }
    }

    EE_TO_JIT_TRANSITION();

    return result;
}

/* static */
size_t CEEInfo::findNameOfToken (Module* module,
                                                 mdToken metaTOK,
                                                 __out_ecount (FQNameCapacity) char * szFQName,
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
        SO_TOLERANT;
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
    CHECK_MSG(scopeHnd != NULL, "Illegal null scope");
    CHECK_MSG(((size_t) context & ~CORINFO_CONTEXTFLAGS_MASK) != NULL, "Illegal null context");
    if (((size_t) context & CORINFO_CONTEXTFLAGS_MASK) == CORINFO_CONTEXTFLAGS_CLASS)
    {
        TypeHandle handle((CORINFO_CLASS_HANDLE) ((size_t) context & ~CORINFO_CONTEXTFLAGS_MASK));
        CHECK_MSG(handle.GetModule() == GetModule(scopeHnd), "Inconsistent scope and context");
    }
    else
    {
        MethodDesc* handle = (MethodDesc*) ((size_t) context & ~CORINFO_CONTEXTFLAGS_MASK);
        CHECK_MSG(handle->GetModule() == GetModule(scopeHnd), "Inconsistent scope and context");
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
        SO_TOLERANT;
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
                ScanTokenForDynamicScope(pResolvedToken, th, pMD);
            }
        }
        else
        if (pFD != NULL)
        {
            if ((tkType != mdtFieldDef) && (tkType != mdtMemberRef))
                ThrowBadTokenException(pResolvedToken);
            if ((tokenType & CORINFO_TOKENKIND_Field) == 0)
                ThrowBadTokenException(pResolvedToken);
            if (th.IsNull())
                th = pFD->GetApproxEnclosingMethodTable();

            if (pFD->IsStatic() && (tokenType != CORINFO_TOKENKIND_Ldtoken))
            {
                ScanTokenForDynamicScope(pResolvedToken, th);
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
                ScanTokenForDynamicScope(pResolvedToken, th);
            }
        }

        _ASSERTE((pMD == NULL) || (pFD == NULL));
        _ASSERTE(!th.IsNull());

        // "PermitUninstDefOrRef" check
        if ((tokenType != CORINFO_TOKENKIND_Ldtoken) && th.ContainsGenericVariables())
        {
            COMPlusThrow(kInvalidProgramException);
        }

        // The JIT always wants to see normalized typedescs for arrays
        if (!th.IsTypeDesc() && th.AsMethodTable()->IsArray())
        {
            MethodTable * pMT = th.AsMethodTable();
        
            // Load the TypeDesc for the array type.
            DWORD rank = pMT->GetRank();
            TypeHandle elemType = pMT->GetApproxArrayElementTypeHandle();
            th = ClassLoader::LoadArrayTypeThrowing(elemType, pMT->GetInternalCorElementType(), rank);
        }
    }
    else
    {
        unsigned metaTOK = pResolvedToken->token;
        Module * pModule = (Module *)pResolvedToken->tokenScope;

        switch (TypeFromToken(metaTOK))
        {
        case mdtModuleRef:
            if ((tokenType & CORINFO_TOKENKIND_Class) == 0)
                ThrowBadTokenException(pResolvedToken);
        
            {
                DomainFile *pTargetModule = pModule->LoadModule(GetAppDomain(), metaTOK, FALSE /* loadResources */);
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

                IfFailThrow(pModule->GetMDImport()->GetTypeSpecFromToken(metaTOK, &pResolvedToken->pTypeSpec, &pResolvedToken->cbTypeSpec));
                
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
                    &th, TRUE, &pResolvedToken->pTypeSpec, &pResolvedToken->cbTypeSpec);

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
                    &th, TRUE, &pResolvedToken->pTypeSpec, &pResolvedToken->cbTypeSpec, &pResolvedToken->pMethodSpec, &pResolvedToken->cbMethodSpec);
            }
            break;

        default:
            ThrowBadTokenException(pResolvedToken);
        }

        //
        // Module dependency tracking
        //
        if (pMD != NULL)
        {
            ScanToken(pModule, pResolvedToken, th, pMD);
        }
        else
        if (pFD != NULL)
        {
            if (pFD->IsStatic())
                ScanToken(pModule, pResolvedToken, th);
        }
        else
        {
            // It should not be required to trigger the modules cctors for ldtoken, it is done for backward compatibility only.
            if (tokenType == CORINFO_TOKENKIND_Box || tokenType == CORINFO_TOKENKIND_Constrained || tokenType == CORINFO_TOKENKIND_Ldtoken)
                ScanToken(pModule, pResolvedToken, th);
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
// We have a few frequently used constants in mscorlib that are defined as 
// readonly static fields for historic reasons. Check for them here and 
// allow them to be treated as actual constants by the JIT.
static CORINFO_FIELD_ACCESSOR getFieldIntrinsic(FieldDesc * field)
{
    STANDARD_VM_CONTRACT;

    if (MscorlibBinder::GetField(FIELD__STRING__EMPTY) == field)
    {
        return CORINFO_FIELD_INTRINSIC_EMPTY_STRING;
    }
    else
    if ((MscorlibBinder::GetField(FIELD__INTPTR__ZERO) == field) ||
        (MscorlibBinder::GetField(FIELD__UINTPTR__ZERO) == field))
    {
        return CORINFO_FIELD_INTRINSIC_ZERO;
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
    if (!pFieldMT->HasClassConstructor() && !pFieldMT->HasBoxedRegularStatics())
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
    IN_WIN32(default:)
        helper = CORINFO_HELP_GETFIELD32;
        break;
    case ELEMENT_TYPE_I8:
    case ELEMENT_TYPE_U8:
    IN_WIN64(default:)
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
void CEEInfo::getFieldInfo (CORINFO_RESOLVED_TOKEN * pResolvedToken,
                            CORINFO_METHOD_HANDLE  callerHandle,
                            CORINFO_ACCESS_FLAGS   flags, 
                            CORINFO_FIELD_INFO    *pResult
                           )
{
    CONTRACTL {
        SO_TOLERANT;
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
            }

            // We are not going through a helper. The constructor has to be triggered explicitly.
            if (!pFieldMT->IsClassPreInited())
                fieldFlags |= CORINFO_FLG_FIELD_INITCLASS;
        }
        else
        if (pField->IsContextStatic())
        {
            fieldAccessor = CORINFO_FIELD_STATIC_ADDR_HELPER;

            pResult->helper = CORINFO_HELP_GETSTATICFIELDADDR_CONTEXT;
        }
        else
        {
            // Regular or thread static
            CORINFO_FIELD_ACCESSOR intrinsicAccessor;

            if (pField->GetFieldType() == ELEMENT_TYPE_VALUETYPE)
                fieldFlags |= CORINFO_FLG_FIELD_STATIC_IN_HEAP;

            if (pFieldMT->IsSharedByGenericInstantiations())
            {
                fieldAccessor = CORINFO_FIELD_STATIC_GENERICS_STATIC_HELPER;

                pResult->helper = getGenericStaticsHelper(pField);
            }
            else
            if (pFieldMT->GetModule()->IsSystem() && (flags & CORINFO_ACCESS_GET) &&
                (intrinsicAccessor = getFieldIntrinsic(pField)) != (CORINFO_FIELD_ACCESSOR)-1)
            {
                // Intrinsics
                fieldAccessor = intrinsicAccessor;
            }
            else
            if (// Domain neutral access.
                m_pMethodBeingCompiled->IsDomainNeutral() || m_pMethodBeingCompiled->IsZapped() || IsCompilingForNGen() ||
                // Static fields are not pinned in collectible types. We will always access 
                // them using a helper since the address cannot be embeded into the code.
                pFieldMT->Collectible() ||
                // We always treat accessing thread statics as if we are in domain neutral code.
                pField->IsThreadStatic()
                )
            {
                fieldAccessor = CORINFO_FIELD_STATIC_SHARED_STATIC_HELPER;

                pResult->helper = getSharedStaticsHelper(pField, pFieldMT);
            }
            else
            {
                fieldAccessor = CORINFO_FIELD_STATIC_ADDRESS;

                // We are not going through a helper. The constructor has to be triggered explicitly.
                if (!pFieldMT->IsClassPreInited())
                    fieldFlags |= CORINFO_FLG_FIELD_INITCLASS;
            }
        }

        //
        // Currently, we only this optimization for regular statics, but it
        // looks like it may be permissible to do this optimization for
        // thread statics as well.
        //
        if ((flags & CORINFO_ACCESS_ADDRESS) &&
            !pField->IsThreadStatic() &&
            !pField->IsContextStatic() &&
            (fieldAccessor != CORINFO_FIELD_STATIC_TLS))
        {
            fieldFlags |= CORINFO_FLG_FIELD_SAFESTATIC_BYREF_RETURN;
        }
    }
    else
    {
        BOOL fInstanceHelper = FALSE;

#if CHECK_APP_DOMAIN_LEAKS
        if (g_pConfig->EnableFullDebug()
            && pField->IsDangerousAppDomainAgileField()
            && CorTypeInfo::IsObjRef(pField->GetFieldType()))
        {
            //
            // In a checked field with all checks turned on, we use a helper to enforce the app domain
            // agile invariant.
            //
            // <REVISIT_TODO>@todo: we'd like to check this for value type fields as well - we
            // just need to add some code to iterate through the fields for
            // references during the assignment.
            // </REVISIT_TODO>
            fInstanceHelper = TRUE;
        }
        else
#endif // CHECK_APP_DOMAIN_LEAKS
#ifdef FEATURE_REMOTING    
        // are we a contextful class? (approxMT is OK to use here)
        if (pFieldMT->IsContextful())
        {
            // Allow the JIT to optimize special cases 

            // If the caller is states that we have a 'this reference'
            // and he is also willing to unwrap it himself
            // then we won't require a helper call.
            if (!(flags & CORINFO_ACCESS_THIS  )  ||
                !(flags & CORINFO_ACCESS_UNWRAP))
            {
                // Normally a helper call is required.
                fInstanceHelper = TRUE;
            }
        }
        // are we a marshaled by ref class? (approxMT is OK to use here)
        else if (pFieldMT->IsMarshaledByRef())
        {
            // Allow the JIT to optimize special cases 

            // If the caller is states that we have a 'this reference'
            // then we won't require a helper call.
            if (!(flags & CORINFO_ACCESS_THIS))
            {
                // Normally a helper call is required.
                fInstanceHelper = TRUE;
            }
        }
#endif // FEATURE_REMOTING

        if (fInstanceHelper)
        {
            if (flags & CORINFO_ACCESS_ADDRESS)
            {
                fieldAccessor = CORINFO_FIELD_INSTANCE_ADDR_HELPER;

                pResult->helper = CORINFO_HELP_GETFIELDADDR;
            }
            else
            {
                fieldAccessor = CORINFO_FIELD_INSTANCE_HELPER;

                pResult->helper = getInstanceFieldHelper(pField, flags);
            }
        }
        else
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
            pResult->offset += sizeof(Object);
        }
    }

    // TODO: This is touching metadata. Can we avoid it?
    DWORD fieldAttribs = pField->GetAttributes();

    if (IsFdFamily(fieldAttribs))
        fieldFlags |= CORINFO_FLG_FIELD_PROTECTED;

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
        StaticAccessCheckContext accessContext(pCallerForSecurity, callerTypeForSecurity.GetMethodTable());

        BOOL canAccess = ClassLoader::CanAccess(
           &accessContext,
           fieldTypeForSecurity.GetMethodTable(),
           fieldTypeForSecurity.GetAssembly(),
           fieldAttribs,
           NULL,
           (flags & CORINFO_ACCESS_INIT_ARRAY) ? NULL : pField, // For InitializeArray, we don't need tocheck the type of the field.
           accessCheckOptions,
           FALSE /*checkTargetMethodTransparency*/,
           TRUE  /*checkTargetTypeTransparency*/);

        if (!canAccess)
        {
            //Set up the throw helper
            pResult->accessAllowed = CORINFO_ACCESS_ILLEGAL;

            pResult->accessCalloutHelper.helperNum = CORINFO_HELP_FIELD_ACCESS_EXCEPTION;
            pResult->accessCalloutHelper.numArgs = 2;

            pResult->accessCalloutHelper.args[0].Set(CORINFO_METHOD_HANDLE(pCallerForSecurity));
            pResult->accessCalloutHelper.args[1].Set(CORINFO_FIELD_HANDLE(pField));

            if (IsCompilingForNGen())
            {
                //see code:CEEInfo::getCallInfo for more information.
                if (pCallerForSecurity->ContainsGenericVariables())
                    COMPlusThrowNonLocalized(kNotSupportedException, W("Cannot embed generic MethodDesc"));
            }
        }
        else
        {
            CorInfoIsAccessAllowedResult isAccessAllowed = CORINFO_ACCESS_ALLOWED;
            CorInfoSecurityRuntimeChecks runtimeChecks = CORINFO_ACCESS_SECURITY_NONE;

            DebugSecurityCalloutStress(getMethodBeingCompiled(), isAccessAllowed, runtimeChecks);
            if (isAccessAllowed == CORINFO_ACCESS_RUNTIME_CHECK)
            {
                pResult->accessAllowed = isAccessAllowed;
                //Explain the callback to the JIT.
                pResult->accessCalloutHelper.helperNum = CORINFO_HELP_FIELD_ACCESS_CHECK;
                pResult->accessCalloutHelper.numArgs = 3;

                pResult->accessCalloutHelper.args[0].Set(CORINFO_METHOD_HANDLE(pCallerForSecurity));

                /* REVISIT_TODO Wed 4/8/2009
                 * This field handle is not useful on its own.  We also need to embed the enclosing class
                 * handle.
                 */
                pResult->accessCalloutHelper.args[1].Set(CORINFO_FIELD_HANDLE(pField));

                pResult->accessCalloutHelper.args[2].Set(runtimeChecks);

                if (IsCompilingForNGen())
                {
                    //see code:CEEInfo::getCallInfo for more information.
                    if (pCallerForSecurity->ContainsGenericVariables())
                        COMPlusThrowNonLocalized(kNotSupportedException, W("Cannot embed generic MethodDesc"));
                }
            }
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
        SO_TOLERANT;
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
        SO_TOLERANT;
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    } CONTRACTL_END;

    JIT_TO_EE_TRANSITION();

    PCCOR_SIGNATURE       pSig = NULL;
    DWORD                 cbSig = 0;

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
            IfFailThrow(module->GetMDImport()->GetNameAndSigOfMemberRef(sigMethTok, &pSig, &cbSig, &szName));
            
            // Defs have already been checked by the loader for validity
            // However refs need to be checked.
            if (!Security::CanSkipVerification(module->GetDomainAssembly()))
            {
                // Can pass 0 for the flags, since it is only used for defs.
                IfFailThrow(validateTokenSig(sigMethTok, pSig, cbSig, 0, module->GetMDImport()));
            }
        }
        else if (TypeFromToken(sigMethTok) == mdtMethodDef)
        {
            IfFailThrow(module->GetMDImport()->GetSigOfMethodDef(sigMethTok, &cbSig, &pSig));
        }
    }

    CEEInfo::ConvToJitSig(
        pSig, 
        cbSig, 
        scopeHnd, 
        sigMethTok, 
        sigRet, 
        GetMethodFromContext(context), 
        false,
        GetTypeFromContext(context));
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
        SO_TOLERANT;
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    } CONTRACTL_END;

    JIT_TO_EE_TRANSITION();

    PCCOR_SIGNATURE       pSig = NULL;
    DWORD                 cbSig = 0;

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
            &cbSig, 
            &pSig));
    }

    CEEInfo::ConvToJitSig(
        pSig, 
        cbSig, 
        scopeHnd, 
        sigTok, 
        sigRet, 
        GetMethodFromContext(context), 
        false,
        GetTypeFromContext(context));
    
    EE_TO_JIT_TRANSITION();
} // CEEInfo::findSig

//---------------------------------------------------------------------------------------
// 
unsigned 
CEEInfo::getClassSize(
    CORINFO_CLASS_HANDLE clsHnd)
{
    CONTRACTL {
        SO_TOLERANT;
        NOTHROW;
        GC_NOTRIGGER;
        MODE_PREEMPTIVE;
    } CONTRACTL_END;

    unsigned result = 0;

    JIT_TO_EE_TRANSITION_LEAF();

    TypeHandle VMClsHnd(clsHnd);
    result = VMClsHnd.GetSize();

    EE_TO_JIT_TRANSITION_LEAF();

    return result;
}

unsigned CEEInfo::getClassAlignmentRequirement(CORINFO_CLASS_HANDLE type, BOOL fDoubleAlignHint)
{
    CONTRACTL {
        SO_TOLERANT;
        NOTHROW;
        GC_NOTRIGGER;
        MODE_PREEMPTIVE;
    } CONTRACTL_END;

    // Default alignment is sizeof(void*)
    unsigned result = sizeof(void*);

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
    unsigned result = sizeof(void*);

    MethodTable * pMT = clsHnd.GetMethodTable();
    if (pMT == NULL)
        return result;

    if (pMT->HasLayout())
    {
        EEClassLayoutInfo* pInfo = pMT->GetLayoutInfo();

        if (clsHnd.IsNativeValueType())
        {
            // if it's the unmanaged view of the managed type, we always use the unmanaged alignment requirement
            result = pInfo->m_LargestAlignmentRequirementOfAllMembers;
        }
        else
        if (pInfo->IsManagedSequential())
        {
            _ASSERTE(!pMT->ContainsPointers());

            // if it's managed sequential, we use the managed alignment requirement
            result = pInfo->m_ManagedLargestAlignmentRequirementOfAllMembers;
        }
        else if (pInfo->IsBlittable())
        {
            _ASSERTE(!pMT->ContainsPointers());

            // if it's blittable, we use the unmanaged alignment requirement
            result = pInfo->m_LargestAlignmentRequirementOfAllMembers;
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
        SO_TOLERANT;
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

mdMethodDef
CEEInfo::getMethodDefFromMethod(CORINFO_METHOD_HANDLE hMethod)
{
    CONTRACTL {
        SO_TOLERANT;
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

BOOL CEEInfo::checkMethodModifier(CORINFO_METHOD_HANDLE hMethod,
                                  LPCSTR modifier,
                                  BOOL fOptional)
{
    CONTRACTL {
        SO_TOLERANT;
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    } CONTRACTL_END;

    BOOL result = FALSE;

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

/*********************************************************************/
unsigned CEEInfo::getClassGClayout (CORINFO_CLASS_HANDLE clsHnd, BYTE* gcPtrs)
{
    CONTRACTL {
        SO_TOLERANT;
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    } CONTRACTL_END;

    unsigned result = 0;

    JIT_TO_EE_TRANSITION();

    TypeHandle VMClsHnd(clsHnd);

    MethodTable* pMT = VMClsHnd.GetMethodTable();

    if (pMT == g_TypedReferenceMT)
    {
        gcPtrs[0] = TYPE_GC_BYREF;
        gcPtrs[1] = TYPE_GC_NONE;
        result = 1;
    }
    else if (VMClsHnd.IsNativeValueType())
    {
        // native value types have no GC pointers
        result = 0;
        memset(gcPtrs, TYPE_GC_NONE,
               (VMClsHnd.GetSize() + sizeof(void*) -1)/ sizeof(void*));
    }
    else
    {
        _ASSERTE(pMT->IsValueType());
        _ASSERTE(sizeof(BYTE) == 1);

        // assume no GC pointers at first
        result = 0;
        memset(gcPtrs, TYPE_GC_NONE,
               (VMClsHnd.GetSize() + sizeof(void*) -1)/ sizeof(void*));

        // walk the GC descriptors, turning on the correct bits
        if (pMT->ContainsPointers())
        {
            CGCDesc* map = CGCDesc::GetCGCDescFromMT(pMT);
            CGCDescSeries * pByValueSeries = map->GetLowestSeries();

            for (SIZE_T i = 0; i < map->GetNumSeries(); i++)
            {
                // Get offset into the value class of the first pointer field (includes a +Object)
                size_t cbSeriesSize = pByValueSeries->GetSeriesSize() + pMT->GetBaseSize();
                size_t cbOffset = pByValueSeries->GetSeriesOffset() - sizeof(Object);

                _ASSERTE (cbOffset % sizeof(void*) == 0);
                _ASSERTE (cbSeriesSize % sizeof(void*) == 0);

                result += (unsigned) (cbSeriesSize / sizeof(void*));
                memset(&gcPtrs[cbOffset/sizeof(void*)], TYPE_GC_REF, cbSeriesSize / sizeof(void*));

                pByValueSeries++;
            }
        }
    }

    EE_TO_JIT_TRANSITION();

    return result;
}

// returns the enregister info for a struct based on type of fields, alignment, etc.
bool CEEInfo::getSystemVAmd64PassStructInRegisterDescriptor(
                                                /*IN*/  CORINFO_CLASS_HANDLE structHnd,
                                                /*OUT*/ SYSTEMV_AMD64_CORINFO_STRUCT_REG_PASSING_DESCRIPTOR* structPassInRegDescPtr)
{
    CONTRACTL {
        SO_TOLERANT;
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    } CONTRACTL_END;

#if defined(FEATURE_UNIX_AMD64_STRUCT_PASSING_ITF)
    JIT_TO_EE_TRANSITION();

    _ASSERTE(structPassInRegDescPtr != nullptr);
    TypeHandle th(structHnd);

    structPassInRegDescPtr->passedInRegisters = false;
    
    // Make sure this is a value type.
    if (th.IsValueType())
    {
        _ASSERTE((CorInfoType2UnixAmd64Classification(th.GetInternalCorElementType()) == SystemVClassificationTypeStruct) ||
                 (CorInfoType2UnixAmd64Classification(th.GetInternalCorElementType()) == SystemVClassificationTypeTypedReference));

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

        bool canPassInRegisters = useNativeLayout ? methodTablePtr->GetLayoutInfo()->IsNativeStructPassedInRegisters()
                                                : methodTablePtr->IsRegPassedStruct();
        if (canPassInRegisters)
        {
            SystemVStructRegisterPassingHelper helper((unsigned int)th.GetSize());
            bool result = methodTablePtr->ClassifyEightBytes(&helper, 0, 0, useNativeLayout);

            // The answer must be true at this point.
            _ASSERTE(result);
            structPassInRegDescPtr->passedInRegisters = true;

            structPassInRegDescPtr->eightByteCount = helper.eightByteCount;
            _ASSERTE(structPassInRegDescPtr->eightByteCount <= CLR_SYSTEMV_MAX_EIGHTBYTES_COUNT_TO_PASS_IN_REGISTERS);

            for (unsigned int i = 0; i < CLR_SYSTEMV_MAX_EIGHTBYTES_COUNT_TO_PASS_IN_REGISTERS; i++)
            {
                structPassInRegDescPtr->eightByteClassifications[i] = helper.eightByteClassifications[i];
                structPassInRegDescPtr->eightByteSizes[i] = helper.eightByteSizes[i];
                structPassInRegDescPtr->eightByteOffsets[i] = helper.eightByteOffsets[i];
            }
        }

        _ASSERTE(structPassInRegDescPtr->passedInRegisters == canPassInRegisters);
    }

    EE_TO_JIT_TRANSITION();

    return true;
#else // !defined(FEATURE_UNIX_AMD64_STRUCT_PASSING_ITF)
    return false;
#endif // !defined(FEATURE_UNIX_AMD64_STRUCT_PASSING_ITF)
}

/*********************************************************************/
unsigned CEEInfo::getClassNumInstanceFields (CORINFO_CLASS_HANDLE clsHnd)
{
    CONTRACTL {
        SO_TOLERANT;
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
        SO_TOLERANT;
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


CORINFO_LOOKUP_KIND CEEInfo::getLocationOfThisType(CORINFO_METHOD_HANDLE context)
{
    CONTRACTL {
        SO_TOLERANT;
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    } CONTRACTL_END;

    CORINFO_LOOKUP_KIND result;

    /* Initialize fields of result for debug build warning */
    result.needsRuntimeLookup = false;
    result.runtimeLookupKind  = CORINFO_LOOKUP_THISOBJ;

    JIT_TO_EE_TRANSITION();

    MethodDesc *pContextMD = GetMethod(context);

    // If the method table is not shared, then return CONST
    if (!pContextMD->GetMethodTable()->IsSharedByGenericInstantiations())
    {
        result.needsRuntimeLookup = false;
    }
    else
    {
        result.needsRuntimeLookup = true;

        // If we've got a vtable extra argument, go through that
        if (pContextMD->RequiresInstMethodTableArg())
        {
            result.runtimeLookupKind = CORINFO_LOOKUP_CLASSPARAM;
        }
        // If we've got an object, go through its vtable
        else if (pContextMD->AcquiresInstMethodTableFromThis())
        {
            result.runtimeLookupKind = CORINFO_LOOKUP_THISOBJ;
        }
        // Otherwise go through the method-desc argument
        else
        {
            _ASSERTE(pContextMD->RequiresInstMethodDescArg());
            result.runtimeLookupKind = CORINFO_LOOKUP_METHODPARAM;
        }
    }

    EE_TO_JIT_TRANSITION();

    return result;
}

CORINFO_METHOD_HANDLE CEEInfo::GetDelegateCtor(
                                        CORINFO_METHOD_HANDLE methHnd,
                                        CORINFO_CLASS_HANDLE clsHnd,
                                        CORINFO_METHOD_HANDLE targetMethodHnd,
                                        DelegateCtorArgs *pCtorData)
{
    CONTRACTL {
        SO_TOLERANT;
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    } CONTRACTL_END;

    if (isVerifyOnly())
    {
        // No sense going through the optimized case just for verification and it can cause issues parsing
        // uninstantiated generic signatures.
        return methHnd;
    }

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
        SO_TOLERANT;
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
            BOOL                     fEmbedParent,
            CORINFO_GENERICHANDLE_RESULT *pResult)
{
    CONTRACTL {
        SO_TOLERANT;
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
            (pMD->GetMethodTable()->IsSharedByGenericInstantiations() || TypeHandle::IsCanonicalSubtypeInstantiation(methodInst));
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

            if (!pDeclaringMD->GetMethodTable()->HasSameTypeDefAs(th.GetMethodTable()))
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
        // even for standalone generic variables that show up as __Cannon here.
        fRuntimeLookup = th.IsCanonicalSubtype();
    }

    _ASSERTE(pResult->compileTimeHandle);

    if (fRuntimeLookup 
            // Handle invalid IL - see comment in code:CEEInfo::ComputeRuntimeLookupForSharedGenericToken
            && ContextIsShared(pResolvedToken->tokenContext))
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

void CEEInfo::ScanForModuleDependencies(Module* pModule, SigPointer psig)
{
    STANDARD_VM_CONTRACT;

    _ASSERTE(pModule && !pModule->IsSystem());

    CorElementType eType;
    IfFailThrow(psig.GetElemType(&eType));
    
    switch (eType)
    {
        case ELEMENT_TYPE_GENERICINST:
        {
            ScanForModuleDependencies(pModule,psig);
            IfFailThrow(psig.SkipExactlyOne());

            ULONG ntypars;
            IfFailThrow(psig.GetData(&ntypars));
            for (ULONG i = 0; i < ntypars; i++)
            {
              ScanForModuleDependencies(pModule,psig);
              IfFailThrow(psig.SkipExactlyOne());
            }
            break;
        }

        case ELEMENT_TYPE_VALUETYPE:
        case ELEMENT_TYPE_CLASS:
        {
            mdToken tk;
            IfFailThrow(psig.GetToken(&tk));
            if (TypeFromToken(tk) ==  mdtTypeRef)
            {
                Module * pTypeDefModule;
                mdToken tkTypeDef;

                if  (ClassLoader::ResolveTokenToTypeDefThrowing(pModule, tk, &pTypeDefModule, &tkTypeDef))
                    break;

                if (!pTypeDefModule->IsSystem() && (pModule != pTypeDefModule))
                {
                    m_pOverride->addActiveDependency((CORINFO_MODULE_HANDLE)pModule, (CORINFO_MODULE_HANDLE)pTypeDefModule);
                }
            }
            break;
        }

        default:
            break;
    }
}

void CEEInfo::ScanMethodSpec(Module * pModule, PCCOR_SIGNATURE pMethodSpec, ULONG cbMethodSpec)
{
    STANDARD_VM_CONTRACT;

    SigPointer sp(pMethodSpec, cbMethodSpec);

    BYTE etype;
    IfFailThrow(sp.GetByte(&etype));

    _ASSERT(etype == (BYTE)IMAGE_CEE_CS_CALLCONV_GENERICINST);

    ULONG nGenericMethodArgs;
    IfFailThrow(sp.GetData(&nGenericMethodArgs));

    for (ULONG i = 0; i < nGenericMethodArgs; i++)
    {
        ScanForModuleDependencies(pModule,sp);
        IfFailThrow(sp.SkipExactlyOne());
    }
}

BOOL CEEInfo::ScanTypeSpec(Module * pModule, PCCOR_SIGNATURE pTypeSpec, ULONG cbTypeSpec)
{
    STANDARD_VM_CONTRACT;

    SigPointer sp(pTypeSpec, cbTypeSpec);

    CorElementType eType;
    IfFailThrow(sp.GetElemType(&eType));

    // Filter out non-instantiated types and typedescs (typevars, arrays, ...)
    if (eType != ELEMENT_TYPE_GENERICINST)
    {
        // Scanning of the parent chain is required for reference types only.
        // Note that the parent chain MUST NOT be scanned for instantiated 
        // generic variables because of they are not a real dependencies.
        return (eType == ELEMENT_TYPE_CLASS);
    }

    IfFailThrow(sp.SkipExactlyOne());

    ULONG ntypars;
    IfFailThrow(sp.GetData(&ntypars));

    for (ULONG i = 0; i < ntypars; i++)
    {
        ScanForModuleDependencies(pModule,sp);
        IfFailThrow(sp.SkipExactlyOne());
    }

    return TRUE;
}

void CEEInfo::ScanInstantiation(Module * pModule, Instantiation inst)
{
    STANDARD_VM_CONTRACT;

    for (DWORD i = 0; i < inst.GetNumArgs(); i++)
    {
        TypeHandle th = inst[i];
        if (th.IsTypeDesc())
            continue;

        MethodTable * pMT = th.AsMethodTable();

        Module * pDefModule = pMT->GetModule();

        if (!pDefModule->IsSystem() && (pModule != pDefModule))
        {
            m_pOverride->addActiveDependency((CORINFO_MODULE_HANDLE)pModule, (CORINFO_MODULE_HANDLE)pDefModule);
        }

        if (pMT->HasInstantiation())
        {
            ScanInstantiation(pModule, pMT->GetInstantiation());
        }
    }
}

//
// ScanToken is used to track triggers for creation of per-AppDomain state instead, including allocations required for statics and
// triggering of module cctors.
//
// The basic rule is: There should be no possibility of a shared module that is "active" to have a direct call into a  module that 
// is not "active". And we don't want to intercept every call during runtime, so during compile time we track static calls and 
// everything that can result in new virtual calls.
//
// The current algoritm (scan the parent type chain and instantiation variables) is more than enough to maintain this invariant.
// One could come up with a more efficient algorithm that still maintains the invariant, but it may introduce backward compatibility
// issues.
//
// For efficiency, the implementation leverages the loaded types as much as possible. Unfortunately, we still have to go back to
// metadata when the generic variables could have been substituted via generic context.
//
void CEEInfo::ScanToken(Module * pModule, CORINFO_RESOLVED_TOKEN * pResolvedToken, TypeHandle th, MethodDesc * pMD)
{
    STANDARD_VM_CONTRACT;

    if (pModule->IsSystem())
        return;

    if (isVerifyOnly())
        return;

    //
    // Scan method instantiation
    //
    if (pMD != NULL && pResolvedToken->pMethodSpec != NULL)
    {
        if (ContextIsInstantiated(pResolvedToken->tokenContext))
        {
            ScanMethodSpec(pModule, pResolvedToken->pMethodSpec, pResolvedToken->cbMethodSpec);
        }
        else
        {
            ScanInstantiation(pModule, pMD->GetMethodInstantiation());
        }
    }

    if (th.IsTypeDesc())
        return;

    MethodTable * pMT = th.AsMethodTable();

    //
    // Scan type instantiation
    //
    if (pResolvedToken->pTypeSpec != NULL)
    {
        if (ContextIsInstantiated(pResolvedToken->tokenContext))
        {
            if (!ScanTypeSpec(pModule, pResolvedToken->pTypeSpec, pResolvedToken->cbTypeSpec))
                return;
        }
        else
        {
            ScanInstantiation(pModule, pMT->GetInstantiation());
        }
    }

    //
    // Scan chain of parent types
    //
    for (;;)
    {
        Module * pDefModule = pMT->GetModule();
        if (pDefModule->IsSystem())
            break;

        if (pModule != pDefModule)
        {
            m_pOverride->addActiveDependency((CORINFO_MODULE_HANDLE)pModule, (CORINFO_MODULE_HANDLE)pDefModule);
        }

        MethodTable * pParentMT = pMT->GetParentMethodTable();
        if (pParentMT == NULL)
            break;

        if (pParentMT->HasInstantiation())
        {
            IMDInternalImport* pInternalImport = pDefModule->GetMDImport();

            mdToken tkParent;
            IfFailThrow(pInternalImport->GetTypeDefProps(pMT->GetCl(), NULL, &tkParent));

            if (TypeFromToken(tkParent) == mdtTypeSpec)
            {
                PCCOR_SIGNATURE pTypeSpec;
                ULONG           cbTypeSpec;
                IfFailThrow(pInternalImport->GetTypeSpecFromToken(tkParent, &pTypeSpec, &cbTypeSpec));

                ScanTypeSpec(pDefModule, pTypeSpec, cbTypeSpec);
            }
        }

        pMT = pParentMT;
    }
}

void CEEInfo::ScanTokenForDynamicScope(CORINFO_RESOLVED_TOKEN * pResolvedToken, TypeHandle th, MethodDesc * pMD)
{
    STANDARD_VM_CONTRACT;

    if (m_pMethodBeingCompiled->IsLCGMethod())
    {
        // The dependency tracking for LCG is irrelevant. Perform immediate activation.
        if (pMD != NULL && pMD->HasMethodInstantiation())
            pMD->EnsureActive();
        if (!th.IsTypeDesc())
            th.AsMethodTable()->EnsureInstanceActive();
        return;
    }

    // Stubs-as-IL have to do regular dependency tracking because they can be shared cross-domain.
    Module * pModule = GetDynamicResolver(pResolvedToken->tokenScope)->GetDynamicMethod()->GetModule();
    ScanToken(pModule, pResolvedToken, th, pMD);
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

// Check that the instantation is <!/!!0, ..., !/!!(n-1)>
static BOOL IsSignatureForTypicalInstantiation(SigPointer sigptr, CorElementType varType, ULONG ntypars)
{
    STANDARD_VM_CONTRACT;

    for (ULONG i = 0; i < ntypars; i++)
    {
        CorElementType type;
        IfFailThrow(sigptr.GetElemType(&type));
        if (type != varType)
            return FALSE;

        ULONG data;
        IfFailThrow(sigptr.GetData(&data));
                    
        if (data != i)
             return FALSE;
    }

    return TRUE;
}

// Check that methodSpec instantiation is <!!0, ..., !!(n-1)>
static BOOL IsMethodSpecForTypicalInstantation(SigPointer sigptr)
{
    STANDARD_VM_CONTRACT;

    BYTE etype;
    IfFailThrow(sigptr.GetByte(&etype));
    _ASSERTE(etype == (BYTE)IMAGE_CEE_CS_CALLCONV_GENERICINST);

    ULONG ntypars;
    IfFailThrow(sigptr.GetData(&ntypars));

    return IsSignatureForTypicalInstantiation(sigptr, ELEMENT_TYPE_MVAR, ntypars);
}

// Check that typeSpec instantiation is <!0, ..., !(n-1)>
static BOOL IsTypeSpecForTypicalInstantiation(SigPointer sigptr)
{
    STANDARD_VM_CONTRACT;

    CorElementType type;
    IfFailThrow(sigptr.GetElemType(&type));
    if (type != ELEMENT_TYPE_GENERICINST)
        return FALSE;

    IfFailThrow(sigptr.SkipExactlyOne());

    ULONG ntypars;
    IfFailThrow(sigptr.GetData(&ntypars));

    return IsSignatureForTypicalInstantiation(sigptr, ELEMENT_TYPE_VAR, ntypars);
}

void CEEInfo::ComputeRuntimeLookupForSharedGenericToken(DictionaryEntryKind entryKind,
                                                        CORINFO_RESOLVED_TOKEN * pResolvedToken,
                                                        CORINFO_RESOLVED_TOKEN * pConstrainedResolvedToken /* for ConstrainedMethodEntrySlot */,
                                                        MethodDesc * pTemplateMD /* for method-based slots */,
                                                        CORINFO_LOOKUP *pResultLookup)
{
    CONTRACTL {
        STANDARD_VM_CHECK;
        PRECONDITION(CheckPointer(pResultLookup));
    } CONTRACTL_END;

    // We should never get here when we are only verifying
    _ASSERTE(!isVerifyOnly());

    pResultLookup->lookupKind.needsRuntimeLookup = true;

    CORINFO_RUNTIME_LOOKUP *pResult = &pResultLookup->runtimeLookup;
    pResult->signature = NULL;

    // Unless we decide otherwise, just do the lookup via a helper function
    pResult->indirections = CORINFO_USEHELPER;

    MethodDesc *pContextMD = GetMethodFromContext(pResolvedToken->tokenContext);
    MethodTable *pContextMT = pContextMD->GetMethodTable();

    // Do not bother computing the runtime lookup if we are inlining. The JIT is going
    // to abort the inlining attempt anyway.
    if (pContextMD != m_pMethodBeingCompiled)
    {
        return;
    }

    // There is a pathological case where invalid IL refereces __Canon type directly, but there is no dictionary availabled to store the lookup. 
    // All callers of ComputeRuntimeLookupForSharedGenericToken have to filter out this case. We can't do much about it here.
    _ASSERTE(pContextMD->IsSharedByGenericInstantiations());

    BOOL fInstrument = FALSE;

#ifdef FEATURE_NATIVE_IMAGE_GENERATION
    // This will make sure that when IBC logging is turned on we will go through a version
    // of JIT_GenericHandle which logs the access. Note that we still want the dictionaries
    // to be populated to prepopulate the types at NGen time.
    if (IsCompilingForNGen() &&
        GetAppDomain()->ToCompilationDomain()->m_fForceInstrument)
    {
        fInstrument = TRUE;
    }
#endif // FEATURE_NATIVE_IMAGE_GENERATION

    // If we've got a  method type parameter of any kind then we must look in the method desc arg
    if (pContextMD->RequiresInstMethodDescArg())
    {
        pResultLookup->lookupKind.runtimeLookupKind = CORINFO_LOOKUP_METHODPARAM;
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
#ifdef FEATURE_PREJIT
                pResult->testForFixup = 1;
#else
                pResult->testForFixup = 0;
#endif
                pResult->offsets[0] = offsetof(InstantiatedMethodDesc, m_pPerInstInfo);

                ULONG data;
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
            if (!IsMethodSpecForTypicalInstantation(SigPointer(pResolvedToken->pMethodSpec, pResolvedToken->cbMethodSpec)))
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
            pResult->testForFixup = 0;

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
            pResultLookup->lookupKind.runtimeLookupKind = CORINFO_LOOKUP_CLASSPARAM;
            pResult->helper = fInstrument ? CORINFO_HELP_RUNTIMEHANDLE_CLASS_LOG : CORINFO_HELP_RUNTIMEHANDLE_CLASS;
        }
        // If we've got an object, go through its vtable
        else 
        {
            _ASSERTE(pContextMD->AcquiresInstMethodTableFromThis());
            pResultLookup->lookupKind.runtimeLookupKind = CORINFO_LOOKUP_THISOBJ;
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
#ifdef FEATURE_PREJIT
                pResult->testForFixup = 1;
#else
                pResult->testForFixup = 0;
#endif
                pResult->offsets[0] = MethodTable::GetOffsetOfPerInstInfo();
                pResult->offsets[1] = sizeof(TypeHandle*) * (pContextMT->GetNumDicts()-1);
                ULONG data;
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
                pResult->testForFixup = 0;

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
        // fall through

    case TypeHandleSlot:
        {
            if (pResolvedToken->tokenType == CORINFO_TOKENKIND_Newarr)
                sigBuilder.AppendElementType(ELEMENT_TYPE_SZARRAY);

            // Note that we can come here with pResolvedToken->pTypeSpec == NULL for invalid IL that 
            // directly references __Cannon
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
        // fall through

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

            mdMethodDef methodToken               = pTemplateMD->GetMemberDef_NoLogging();
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
            if (entryKind == DispatchStubAddrSlot)
            {
                // Encode the method for dispatch stub using slot to avoid touching the interface method MethodDesc at runtime

                // There should be no other flags set if we are encoding the method using slot for virtual stub dispatch
                _ASSERTE(methodFlags == 0);

                methodFlags |= ENCODE_METHOD_SIG_SlotInsteadOfToken;
            }
            else
            if (!pTemplateMD->GetModule()->IsInCurrentVersionBubble())
            {
                // Using a method defined in another version bubble. We can assume the slot number is stable only for real interface methods.
                if (!pTemplateMD->GetMethodTable()->IsInterface() || pTemplateMD->IsStatic() || pTemplateMD->HasMethodInstantiation())
                {
                    _ASSERTE(!"References to non-interface methods not yet supported in version resilient images");
                    IfFailThrow(E_FAIL);
                }
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
                
                DWORD nGenericMethodArgs;
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

    // It's a method dictionary lookup
    if (pResultLookup->lookupKind.runtimeLookupKind == CORINFO_LOOKUP_METHODPARAM)
    {
        _ASSERTE(pContextMD != NULL);
        _ASSERTE(pContextMD->HasMethodInstantiation());

        if (DictionaryLayout::FindToken(pContextMD->GetLoaderAllocator(), pContextMD->GetNumGenericMethodArgs(), pContextMD->GetDictionaryLayout(), pResult, &sigBuilder, 1))
        {
            pResult->testForNull = 1;
            pResult->testForFixup = 0;

            // Indirect through dictionary table pointer in InstantiatedMethodDesc
            pResult->offsets[0] = offsetof(InstantiatedMethodDesc, m_pPerInstInfo);
        }
    }

    // It's a class dictionary lookup (CORINFO_LOOKUP_CLASSPARAM or CORINFO_LOOKUP_THISOBJ)
    else
    {
        if (DictionaryLayout::FindToken(pContextMT->GetLoaderAllocator(), pContextMT->GetNumGenericArgs(), pContextMT->GetClass()->GetDictionaryLayout(), pResult, &sigBuilder, 2))
        {
            pResult->testForNull = 1;
            pResult->testForFixup = 0;

            // Indirect through dictionary table pointer in vtable
            pResult->offsets[0] = MethodTable::GetOffsetOfPerInstInfo();

            // Next indirect through the dictionary appropriate to this instantiated type
            pResult->offsets[1] = sizeof(TypeHandle*) * (pContextMT->GetNumDicts()-1);
        }
    }
}



/*********************************************************************/
const char* CEEInfo::getClassName (CORINFO_CLASS_HANDLE clsHnd)
{
    CONTRACTL {
        SO_TOLERANT;
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    } CONTRACTL_END;

    const char* result = NULL;

    JIT_TO_EE_TRANSITION();

    TypeHandle VMClsHnd(clsHnd);
    MethodTable* pMT = VMClsHnd.GetMethodTable();
    if (pMT == NULL)
    {
        result = "";
    }
    else
    {
#ifdef _DEBUG
        result = pMT->GetDebugClassName();
#else // !_DEBUG
        // since this is for diagnostic purposes only,
        // give up on the namespace, as we don't have a buffer to concat it
        // also note this won't show array class names.
        LPCUTF8 nameSpace;
        result = pMT->GetFullyQualifiedNameInfo(&nameSpace);
#endif
    }

    EE_TO_JIT_TRANSITION();

    return result;
}

/***********************************************************************/
const char* CEEInfo::getHelperName (CorInfoHelpFunc ftnNum)
{
    CONTRACTL {
        SO_TOLERANT;
        NOTHROW;
        GC_NOTRIGGER;
        MODE_PREEMPTIVE;
        PRECONDITION(ftnNum >= 0 && ftnNum < CORINFO_HELP_COUNT);
    } CONTRACTL_END;

    const char* result = NULL;

    JIT_TO_EE_TRANSITION_LEAF();

#ifdef CROSSGEN_COMPILE
    result = hlpNameTable[ftnNum];
#else
#ifdef _DEBUG
    result = hlpFuncTable[ftnNum].name;
#else
    result = "AnyJITHelper";
#endif
#endif

    EE_TO_JIT_TRANSITION_LEAF();

    return result;
}


/*********************************************************************/
int CEEInfo::appendClassName(__deref_inout_ecount(*pnBufLen) WCHAR** ppBuf,
                             int* pnBufLen,
                             CORINFO_CLASS_HANDLE    clsHnd,
                             BOOL fNamespace,
                             BOOL fFullInst,
                             BOOL fAssembly)
{
    CONTRACTL {
        SO_TOLERANT;
        MODE_PREEMPTIVE;
        THROWS;
        GC_TRIGGERS;
    } CONTRACTL_END;

    int nLen = 0;

    JIT_TO_EE_TRANSITION();

    TypeHandle th(clsHnd);
    StackSString ss;
    TypeString::AppendType(ss,th,
                           (fNamespace ? TypeString::FormatNamespace : 0) |
                           (fFullInst ? TypeString::FormatFullInst : 0) |
                           (fAssembly ? TypeString::FormatAssembly : 0));
    const WCHAR* szString = ss.GetUnicode();
    nLen = (int)wcslen(szString);
    if (*pnBufLen > 0)
    {
    wcscpy_s(*ppBuf, *pnBufLen, szString );
    (*ppBuf)[(*pnBufLen) - 1] = W('\0');
    (*ppBuf) += nLen;
    (*pnBufLen) -= nLen;
    }

    EE_TO_JIT_TRANSITION();

    return nLen;
}

/*********************************************************************/
CORINFO_MODULE_HANDLE CEEInfo::getClassModule(CORINFO_CLASS_HANDLE clsHnd)
{
    CONTRACTL {
        SO_TOLERANT;
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
        SO_TOLERANT;
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
        SO_TOLERANT;
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
        SO_TOLERANT;
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
        SO_TOLERANT;
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
        SO_TOLERANT;
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
BOOL CEEInfo::isValueClass(CORINFO_CLASS_HANDLE clsHnd)
{
    CONTRACTL {
        SO_TOLERANT;
        NOTHROW;
        GC_NOTRIGGER;
        MODE_PREEMPTIVE;
    } CONTRACTL_END;

    BOOL ret = FALSE;

    JIT_TO_EE_TRANSITION_LEAF();

    _ASSERTE(clsHnd);

    // Note that clsHnd.IsValueType() would not return what the JIT expects
    // for corner cases like ELEMENT_TYPE_FNPTR
    TypeHandle VMClsHnd(clsHnd);
    MethodTable * pMT = VMClsHnd.GetMethodTable();
    ret = (pMT != NULL) ? pMT->IsValueType() : 0;

    EE_TO_JIT_TRANSITION_LEAF();

    return ret;
}

/*********************************************************************/
// If this method returns true, JIT will do optimization to inline the check for
//     GetClassFromHandle(handle) == obj.GetType()
//
// This will enable to use directly the typehandle instead of going through getClassByHandle
BOOL CEEInfo::canInlineTypeCheckWithObjectVTable (CORINFO_CLASS_HANDLE clsHnd)
{
    CONTRACTL {
        SO_TOLERANT;
        NOTHROW;
        GC_NOTRIGGER;
        MODE_PREEMPTIVE;
    } CONTRACTL_END;

    BOOL ret = FALSE;

    JIT_TO_EE_TRANSITION_LEAF();

    _ASSERTE(clsHnd);

    TypeHandle VMClsHnd(clsHnd);

    if (VMClsHnd.IsTypeDesc())
    {
        // We can't do this optimization for arrays because of the object methodtable is template methodtable
        ret = FALSE;
    }
    else
    if (VMClsHnd.AsMethodTable()->IsMarshaledByRef())
    {
        // We can't do this optimization for marshalbyrefs because of the object methodtable can be transparent proxy
        ret = FALSE;
    }
    else
    if (VMClsHnd.AsMethodTable()->IsInterface())
    {
        // Object.GetType() should not ever return interface. However, WCF custom remoting proxy does it. Disable this
        // optimization for interfaces so that (autogenerated) code that compares Object.GetType() with interface type works 
        // as expected for WCF custom remoting proxy. Note that this optimization is still not going to work well for custom
        // remoting proxies that are even more broken than the WCF one, e.g. returning random non-marshalbyref types 
        // from Object.GetType().
        ret = FALSE;
    }
    else
    if (VMClsHnd == TypeHandle(g_pCanonMethodTableClass))   
    {
        // We can't do this optimization in shared generics code because of we do not know what the actual type is going to be.
        // (It can be array, marshalbyref, etc.)
        ret = FALSE;
    }
    else
    {
        // It is safe to perform this optimization
        ret = TRUE;
    }

    EE_TO_JIT_TRANSITION_LEAF();

    return(ret);
}

/*********************************************************************/
DWORD CEEInfo::getClassAttribs (CORINFO_CLASS_HANDLE clsHnd)
{
    CONTRACTL {
        SO_TOLERANT;
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    } CONTRACTL_END;

    // <REVISIT_TODO>@todo FIX need to really fetch the class atributes.  at present
    // we don't need to because the JIT only cares in the case of COM classes</REVISIT_TODO>
    DWORD ret = 0;

    JIT_TO_EE_TRANSITION();

    ret = getClassAttribsInternal(clsHnd);

    EE_TO_JIT_TRANSITION();

    return ret;
}


/*********************************************************************/
BOOL CEEInfo::isStructRequiringStackAllocRetBuf(CORINFO_CLASS_HANDLE clsHnd)
{
    CONTRACTL {
        SO_TOLERANT;
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    } CONTRACTL_END;

    BOOL ret = 0;

    JIT_TO_EE_TRANSITION_LEAF();

    TypeHandle     VMClsHnd(clsHnd);
    MethodTable * pMT = VMClsHnd.GetMethodTable();
    ret = (pMT != NULL && pMT->IsStructRequiringStackAllocRetBuf());

    EE_TO_JIT_TRANSITION_LEAF();

    return ret;
}

/*********************************************************************/
DWORD CEEInfo::getClassAttribsInternal (CORINFO_CLASS_HANDLE clsHnd)
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

        if (pMT->IsValueType())
        {
            ret |= CORINFO_FLG_VALUECLASS;

            if (pMT->ContainsStackPtr())
                ret |= CORINFO_FLG_CONTAINS_STACK_PTR;

            if ((pClass->IsNotTightlyPacked() && (!pClass->IsManagedSequential() || pClass->HasExplicitSize())) ||
                pMT == g_TypedReferenceMT ||
                VMClsHnd.IsNativeValueType())
            {
                ret |= CORINFO_FLG_CUSTOMLAYOUT;
            }

            if (pClass->IsUnsafeValueClass())
                ret |= CORINFO_FLG_UNSAFE_VALUECLASS;
        }
        if (pClass->HasExplicitFieldOffsetLayout() && pClass->HasOverLayedField())
            ret |= CORINFO_FLG_OVERLAPPING_FIELDS;
        if (VMClsHnd.IsCanonicalSubtype())
            ret |= CORINFO_FLG_SHAREDINST;

        if (pMT->HasVariance())
            ret |= CORINFO_FLG_VARIANCE;

        if (pMT->IsContextful())
            ret |= CORINFO_FLG_CONTEXTFUL;

        if (pMT->IsMarshaledByRef())
            ret |= CORINFO_FLG_MARSHAL_BYREF;

        if (pMT->ContainsPointers())
            ret |= CORINFO_FLG_CONTAINS_GC_PTR;

        if (pMT->IsDelegate())
            ret |= CORINFO_FLG_DELEGATE;

        if (pClass->IsBeforeFieldInit())
        {
            if (IsReadyToRunCompilation() && !pMT->GetModule()->IsInCurrentVersionBubble())
            {
                // For version resiliency do not allow hoisting static constructors out of loops
            }
            else
            {
                ret |= CORINFO_FLG_BEFOREFIELDINIT;
            }
        }

        if (pClass->IsAbstract())
            ret |= CORINFO_FLG_ABSTRACT;

        if (pClass->IsSealed())
            ret |= CORINFO_FLG_FINAL;
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
            CORINFO_CONTEXT_HANDLE  context,
            BOOL                    speculative)
{
    CONTRACTL {
        SO_TOLERANT;
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    } CONTRACTL_END;

    DWORD result = CORINFO_INITCLASS_NOT_REQUIRED;

    JIT_TO_EE_TRANSITION();
    {

    // Do not bother figuring out the initialization if we are only verifying the method.
    if (isVerifyOnly())
    {
        result = CORINFO_INITCLASS_NOT_REQUIRED;
        goto exit;
    }

    FieldDesc * pFD = (FieldDesc *)field;
    _ASSERTE(pFD == NULL || pFD->IsStatic());

    MethodDesc * pMD = (MethodDesc *)method;

    TypeHandle typeToInitTH = (pFD != NULL) ? pFD->GetEnclosingMethodTable() : GetTypeFromContext(context);

    MethodDesc *methodBeingCompiled = m_pMethodBeingCompiled;

    BOOL fMethodDomainNeutral = methodBeingCompiled->IsDomainNeutral() || methodBeingCompiled->IsZapped() || IsCompilingForNGen();

    MethodTable *pTypeToInitMT = typeToInitTH.AsMethodTable();

    // This should be the most common early-out case.
    if (fMethodDomainNeutral)
    {
        if (pTypeToInitMT->IsClassPreInited())
        {
            result = CORINFO_INITCLASS_NOT_REQUIRED;
            goto exit;
        }
    }
    else
    {
#ifdef CROSSGEN_COMPILE
        _ASSERTE(FALSE);
#else // CROSSGEN_COMPILE
        if (pTypeToInitMT->IsClassInited())
        {
            // If the type is initialized there really is nothing to do.
            result = CORINFO_INITCLASS_INITIALIZED;
            goto exit;
        }
#endif // CROSSGEN_COMPILE
    }

    if (pTypeToInitMT->IsGlobalClass())
    {
        // For both jitted and ngen code the global class is always considered initialized
        result = CORINFO_INITCLASS_NOT_REQUIRED;
        goto exit;
    }

    bool fIgnoreBeforeFieldInit = false;

    if (pFD == NULL)
    {
        if (!fIgnoreBeforeFieldInit && pTypeToInitMT->GetClass()->IsBeforeFieldInit())
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
        if (!pMD->IsCtor() && !pTypeToInitMT->IsValueType())
        {
            // For instance methods of types with precise-initialization
            // semantics, we can assume that the .ctor triggerred the
            // type initialization.
            // This does not hold for NULL "this" object. However, the spec does
            // not require that case to work.
            result = CORINFO_INITCLASS_NOT_REQUIRED;
            goto exit;
        }
    }

    if (pTypeToInitMT->IsSharedByGenericInstantiations())
    {
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
        _ASSERTE(fIgnoreBeforeFieldInit || !pTypeToInitMT->GetClass()->IsBeforeFieldInit());

        // Note that jit has both methods the same if asking whether to emit cctor
        // for a given method's code (as opposed to inlining codegen).
        if (context != MAKE_METHODCONTEXT(methodBeingCompiled) && pTypeToInitMT == methodBeingCompiled->GetMethodTable())
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
        if (!pTypeToInitMT->IsValueType() && !pTypeToInitMT->GetClass()->IsBeforeFieldInit())
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

    if (fMethodDomainNeutral)
    {
        // Well, because of code sharing we can't do anything at coge generation time.
        // We have to do it at runtime.
        result = CORINFO_INITCLASS_USE_HELPER;
        goto exit;
    }

#ifndef CROSSGEN_COMPILE
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

#ifdef FEATURE_MULTICOREJIT
    // Once multicore JIT is enabled in an AppDomain by calling SetProfileRoot, always use helper function to call class init, for consistency
    if (! GetAppDomain()->GetMulticoreJitManager().AllowCCtorsToRunDuringJITing())
    {
        result = CORINFO_INITCLASS_USE_HELPER;

        goto exit;
    }
#endif

    // To preserve consistent behavior between ngen and not-ngenned states, do not eagerly
    // run class constructors for autongennable code.
    if (pTypeToInitMT->RunCCTorAsIfNGenImageExists())
    {
        result = CORINFO_INITCLASS_USE_HELPER;
        goto exit;
    }

    if (!pTypeToInitMT->GetClass()->IsBeforeFieldInit())
    {
        // Do not inline the access if we cannot initialize the class. Chances are that the class will get
        // initialized by the time the access is jitted.
        result = CORINFO_INITCLASS_USE_HELPER | CORINFO_INITCLASS_DONT_INLINE;
        goto exit;
    }

    if (speculative)
    {
        // Tell the JIT that we may be able to initialize the class when asked to.
        result = CORINFO_INITCLASS_SPECULATIVE;
        goto exit;
    }

    //
    // We cannot run the class constructor without first activating the
    // module containing the class.  However, since the current module
    // we are compiling inside is not active, we don't want to do this.
    //
    // This should be an unusal case since normally the method's module should
    // be active during jitting.
    //
    // @TODO: We should check IsActivating() instead of IsActive() since we may
    // be running the Module::.cctor(). The assembly is not marked as active
    // until then.
    if (!methodBeingCompiled->GetLoaderModule()->GetDomainFile()->IsActive())
    {
        result = CORINFO_INITCLASS_USE_HELPER;
        goto exit;
    }

    //
    // Run the .cctor
    //

    EX_TRY
    {
        pTypeToInitMT->CheckRunClassInitThrowing();
    }
    EX_CATCH
    {
    } EX_END_CATCH(SwallowAllExceptions);

    if (pTypeToInitMT->IsClassInited())
    {
        result = CORINFO_INITCLASS_INITIALIZED;
        goto exit;
    }
#endif // CROSSGEN_COMPILE

    // Do not inline the access if we were unable to initialize the class. Chances are that the class will get
    // initialized by the time the access is jitted.
    result = (CORINFO_INITCLASS_USE_HELPER | CORINFO_INITCLASS_DONT_INLINE);

    }
exit: ;
    EE_TO_JIT_TRANSITION();

    return (CorInfoInitClassResult)result;
}



void CEEInfo::classMustBeLoadedBeforeCodeIsRun (CORINFO_CLASS_HANDLE typeToLoadHnd)
{
    CONTRACTL {
        SO_TOLERANT;
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
        SO_TOLERANT;
        NOTHROW;
        GC_NOTRIGGER;
        MODE_PREEMPTIVE;
    } CONTRACTL_END;

    JIT_TO_EE_TRANSITION_LEAF();

    MethodDesc *pMD = (MethodDesc*) methHnd;

    // MethodDescs returned to JIT at runtime are always fully loaded. Verify that it is the case.
    _ASSERTE(pMD->IsRestored() && pMD->GetMethodTable()->IsFullyLoaded());

    EE_TO_JIT_TRANSITION_LEAF();
}

/*********************************************************************/
CORINFO_METHOD_HANDLE CEEInfo::mapMethodDeclToMethodImpl(CORINFO_METHOD_HANDLE methHnd)
{
    CONTRACTL {
        SO_TOLERANT;
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
        SO_TOLERANT;
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
        result = CORINFO_CLASS_HANDLE(MscorlibBinder::GetClass(CLASS__TYPE_HANDLE));
        break;
    case CLASSID_FIELD_HANDLE:
        result = CORINFO_CLASS_HANDLE(MscorlibBinder::GetClass(CLASS__FIELD_HANDLE));
        break;
    case CLASSID_METHOD_HANDLE:
        result = CORINFO_CLASS_HANDLE(MscorlibBinder::GetClass(CLASS__METHOD_HANDLE));
        break;
    case CLASSID_ARGUMENT_HANDLE:
        result = CORINFO_CLASS_HANDLE(g_ArgumentHandleMT);
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
        SO_TOLERANT;
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    } CONTRACTL_END;

    CorInfoType result = CORINFO_TYPE_UNDEF;

    JIT_TO_EE_TRANSITION();

    TypeHandle th(clsHnd);
    _ASSERTE (!th.IsGenericVariable());

    MethodTable    *pMT = th.GetMethodTable();
    PREFIX_ASSUME(pMT != NULL);

    // Is it a non primitive struct such as
    // RuntimeTypeHandle, RuntimeMethodHandle, RuntimeArgHandle?
    if (pMT->IsValueType() &&
        !pMT->IsTruePrimitive()  &&
        !pMT->IsEnum())
    {
        // default value CORINFO_TYPE_UNDEF is what we want
    }
    else
    {
        switch (th.GetInternalCorElementType())
        {
        case ELEMENT_TYPE_I1:
        case ELEMENT_TYPE_U1:
        case ELEMENT_TYPE_BOOLEAN:
            result = asCorInfoType(ELEMENT_TYPE_I1);
            break;

        case ELEMENT_TYPE_I2:
        case ELEMENT_TYPE_U2:
        case ELEMENT_TYPE_CHAR:
            result = asCorInfoType(ELEMENT_TYPE_I2);
            break;

        case ELEMENT_TYPE_I4:
        case ELEMENT_TYPE_U4:
            result = asCorInfoType(ELEMENT_TYPE_I4);
            break;

        case ELEMENT_TYPE_I8:
        case ELEMENT_TYPE_U8:
            result = asCorInfoType(ELEMENT_TYPE_I8);
            break;

        case ELEMENT_TYPE_I:
        case ELEMENT_TYPE_U:
            result = asCorInfoType(ELEMENT_TYPE_I);
            break;

        case ELEMENT_TYPE_R4:
            result = asCorInfoType(ELEMENT_TYPE_R4);
            break;

        case ELEMENT_TYPE_R8:
            result = asCorInfoType(ELEMENT_TYPE_R8);
            break;

        case ELEMENT_TYPE_VOID:
            result = asCorInfoType(ELEMENT_TYPE_VOID);
            break;

        case ELEMENT_TYPE_PTR:
        case ELEMENT_TYPE_FNPTR:
            result = asCorInfoType(ELEMENT_TYPE_PTR);
            break;

        default:
            break;
        }
    }

    EE_TO_JIT_TRANSITION();

    return result;
}


void CEEInfo::getGSCookie(GSCookie * pCookieVal, GSCookie ** ppCookieVal)
{
    CONTRACTL {
        SO_TOLERANT;
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
BOOL CEEInfo::canCast(
        CORINFO_CLASS_HANDLE        child,
        CORINFO_CLASS_HANDLE        parent)
{
    CONTRACTL {
        SO_TOLERANT;
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    } CONTRACTL_END;

    BOOL result = FALSE;

    JIT_TO_EE_TRANSITION();

    result = ((TypeHandle)child).CanCastTo((TypeHandle)parent);

    EE_TO_JIT_TRANSITION();

    return result;
}

/*********************************************************************/
// TRUE if cls1 and cls2 are considered equivalent types.
BOOL CEEInfo::areTypesEquivalent(
        CORINFO_CLASS_HANDLE        cls1,
        CORINFO_CLASS_HANDLE        cls2)
{
    CONTRACTL {
        SO_TOLERANT;
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    } CONTRACTL_END;

    BOOL result = FALSE;

    JIT_TO_EE_TRANSITION();

    result = ((TypeHandle)cls1).IsEquivalentTo((TypeHandle)cls2);

    EE_TO_JIT_TRANSITION();

    return result;
}

/*********************************************************************/
// returns is the intersection of cls1 and cls2.
CORINFO_CLASS_HANDLE CEEInfo::mergeClasses(
        CORINFO_CLASS_HANDLE        cls1,
        CORINFO_CLASS_HANDLE        cls2)
{
    CONTRACTL {
        SO_TOLERANT;
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    } CONTRACTL_END;

    CORINFO_CLASS_HANDLE result = NULL;

    JIT_TO_EE_TRANSITION();

    TypeHandle merged = TypeHandle::MergeTypeHandlesToCommonParent(TypeHandle(cls1), TypeHandle(cls2));
#ifdef _DEBUG
    {
        //Make sure the merge is reflexive in the cases we "support".
        TypeHandle hnd1 = TypeHandle(cls1);
        TypeHandle hnd2 = TypeHandle(cls2);
        TypeHandle reflexive = TypeHandle::MergeTypeHandlesToCommonParent(hnd2, hnd1);

        //If both sides are classes than either they have a common non-interface parent (in which case it is
        //reflexive)
        //OR they share a common interface, and it can be order dependent (if they share multiple interfaces
        //in common)
        if (!hnd1.IsInterface() && !hnd2.IsInterface())
        {
            if (merged.IsInterface())
            {
                _ASSERTE(reflexive.IsInterface());
            }
            else
            {
                _ASSERTE(merged == reflexive);
            }
        }
        //Both results must either be interfaces or classes.  They cannot be mixed.
        _ASSERTE((!!merged.IsInterface()) == (!!reflexive.IsInterface()));

        //If the result of the merge was a class, then the result of the reflexive merge was the same class.
        if (!merged.IsInterface())
        {
            _ASSERTE(merged == reflexive);
        }

        //If both sides are arrays, then the result is either an array or g_pArrayClass.  The above is
        //actually true about the element type for references types, but I think that that is a little
        //excessive for sanity.
        if (hnd1.IsArray() && hnd2.IsArray())
        {
            _ASSERTE((merged.IsArray() && reflexive.IsArray())
                     || ((merged == g_pArrayClass) && (reflexive == g_pArrayClass)));
        }

        //Can I assert anything about generic variables?

        //The results must always be assignable
        _ASSERTE(hnd1.CanCastTo(merged) && hnd2.CanCastTo(merged) && hnd1.CanCastTo(reflexive)
                 && hnd2.CanCastTo(reflexive));
    }
#endif
    result = CORINFO_CLASS_HANDLE(merged.AsPtr());

    EE_TO_JIT_TRANSITION();
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
        SO_TOLERANT;
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
        SO_TOLERANT;
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

    // BYREF, ARRAY types
    if (th.IsTypeDesc())
    {
        retType = th.AsTypeDesc()->GetTypeParam();
    }
    else
    {
        // <REVISIT_TODO> we really should not have this case.  arrays type handles
        // used in the JIT interface should never be ordinary method tables,
        // indeed array type handles should really never be ordinary MTs
        // at all.  Perhaps we should assert !th.IsTypeDesc() && th.AsMethodTable().IsArray()? </REVISIT_TODO>
        MethodTable* pMT= th.AsMethodTable();
        if (pMT->IsArray())
            retType = pMT->GetApproxArrayElementTypeHandle();
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
// Check any constraints on class type arguments
BOOL CEEInfo::satisfiesClassConstraints(CORINFO_CLASS_HANDLE cls)
{
    CONTRACTL {
        SO_TOLERANT;
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    } CONTRACTL_END;

    BOOL result = FALSE;

    JIT_TO_EE_TRANSITION();

    _ASSERTE(cls != NULL);
    result = TypeHandle(cls).SatisfiesClassConstraints();

    EE_TO_JIT_TRANSITION();

    return result;
}

/*********************************************************************/
// Check if this is a single dimensional array type
BOOL CEEInfo::isSDArray(CORINFO_CLASS_HANDLE  cls)
{
    CONTRACTL {
        SO_TOLERANT;
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    } CONTRACTL_END;

    BOOL result = FALSE;

    JIT_TO_EE_TRANSITION();

    TypeHandle th(cls);

    _ASSERTE(!th.IsNull());

    if (th.IsArrayType())
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
        SO_TOLERANT;
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    } CONTRACTL_END;

    unsigned result = 0;

    JIT_TO_EE_TRANSITION();

    TypeHandle th(cls);

    _ASSERTE(!th.IsNull());

    if (th.IsArrayType())
    {
        // Lots of code used to think that System.Array's methodtable returns TRUE for IsArray(). It doesn't.
        _ASSERTE(th != TypeHandle(g_pArrayClass));

        result = th.GetPossiblySharedArrayMethodTable()->GetRank();
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
            DWORD                       size
            )
{
    CONTRACTL {
        SO_TOLERANT;
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    } CONTRACTL_END;

    void * result = NULL;

    JIT_TO_EE_TRANSITION();

    FieldDesc* pField = (FieldDesc*) field;

    if (!pField                    ||
        !pField->IsRVA()           ||
        (pField->LoadSize() < size)
#ifdef FEATURE_NATIVE_IMAGE_GENERATION
        // This will make sure that when IBC logging is on, the array initialization happens thru 
        // COMArrayInfo::InitializeArray. This gives a place to put the IBC probe that can help
        // separate hold and cold RVA blobs.
        || (IsCompilingForNGen() &&
            GetAppDomain()->ToCompilationDomain()->m_fForceInstrument)
#endif // FEATURE_NATIVE_IMAGE_GENERATION
        )
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
        SO_TOLERANT;
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
        StaticAccessCheckContext accessContext(pCallerForSecurity, callerTypeForSecurity.GetMethodTable());

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

        if (IsCompilingForNGen())
        {
            //see code:CEEInfo::getCallInfo for more information.
            if (pCallerForSecurity->ContainsGenericVariables() || pCalleeForSecurity.ContainsGenericVariables())
                COMPlusThrowNonLocalized(kNotSupportedException, W("Cannot embed generic TypeHandle"));
        }
    }

    if (isAccessAllowed == CORINFO_ACCESS_ALLOWED)
    {
        //Finally let's get me some transparency checks.
        CorInfoSecurityRuntimeChecks runtimeChecks = CORINFO_ACCESS_SECURITY_NONE; 


        DebugSecurityCalloutStress(getMethodBeingCompiled(), isAccessAllowed,
                                   runtimeChecks);

        if (isAccessAllowed != CORINFO_ACCESS_ALLOWED)
        {
            _ASSERTE(isAccessAllowed == CORINFO_ACCESS_RUNTIME_CHECK);
            //Well, time for the runtime helper
            pAccessHelper->helperNum = CORINFO_HELP_CLASS_ACCESS_CHECK;
            pAccessHelper->numArgs = 3;

            pAccessHelper->args[0].Set(CORINFO_METHOD_HANDLE(pCallerForSecurity));
            pAccessHelper->args[1].Set(CORINFO_CLASS_HANDLE(pCalleeForSecurity.AsPtr()));
            pAccessHelper->args[2].Set(runtimeChecks);

            if (IsCompilingForNGen())
            {
                //see code:CEEInfo::getCallInfo for more information.
                if (pCallerForSecurity->ContainsGenericVariables() || pCalleeForSecurity.ContainsGenericVariables())
                    COMPlusThrowNonLocalized(kNotSupportedException, W("Cannot embed generic TypeHandle"));
            }
        }
    }

    EE_TO_JIT_TRANSITION();
    return isAccessAllowed;
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
        SO_TOLERANT;
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    } CONTRACTL_END;

    JIT_TO_EE_TRANSITION();

    _ASSERTE(CheckPointer(pResult));

    INDEBUG(memset(pResult, 0xCC, sizeof(*pResult)));

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


    if (pMD == g_pPrepareConstrainedRegionsMethod && !isVerifyOnly())
    {
        MethodDesc * methodFromContext = GetMethodFromContext(pResolvedToken->tokenContext);

        if (methodFromContext != NULL && methodFromContext->IsIL())
        {
            SigTypeContext typeContext;
            GetTypeContext(pResolvedToken->tokenContext, &typeContext);

            // If the method whose context we're in is attempting a call to PrepareConstrainedRegions() then we've found the root
            // method in a Constrained Execution Region (CER). Prepare the call graph of the critical parts of that method now so
            // they won't fail because of us at runtime.
            MethodCallGraphPreparer mcgp(methodFromContext, &typeContext, false, false);
            bool fMethodHasCallsWithinExplicitCer = mcgp.Run();
            if (! g_pConfig->ProbeForStackOverflow() || ! fMethodHasCallsWithinExplicitCer)
            {
                // if the method does not contain any CERs that call out, we can optimize the probe away
                pMD = MscorlibBinder::GetMethod(METHOD__RUNTIME_HELPERS__PREPARE_CONSTRAINED_REGIONS_NOOP);
            }
        }
    }

    TypeHandle exactType = TypeHandle(pResolvedToken->hClass);

    TypeHandle constrainedType;
    if ((flags & CORINFO_CALLINFO_CALLVIRT) && (pConstrainedResolvedToken != NULL))
    {
        constrainedType = TypeHandle(pConstrainedResolvedToken->hClass);
    }

    BOOL fResolvedConstraint = FALSE;
    BOOL fForceUseRuntimeLookup = FALSE;

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
        MethodDesc * directMethod = constrainedType.GetMethodTable()->TryResolveConstraintMethodApprox(
            exactType, 
            pMD, 
            &fForceUseRuntimeLookup);
        if (directMethod)
        {
            // Either
            //    1. no constraint resolution at compile time (!directMethod)
            // OR 2. no code sharing lookup in call
            // OR 3. we have have resolved to an instantiating stub

            pMDAfterConstraintResolution = directMethod;
            _ASSERTE(!pMDAfterConstraintResolution->IsInterface());
            fResolvedConstraint = TRUE;
            pResult->thisTransform = CORINFO_NO_THIS_TRANSFORM;

            exactType = constrainedType;
        }
        else  if (constrainedType.IsValueType())
        {
            pResult->thisTransform = CORINFO_BOX_THIS;
        }
        else
        {
            pResult->thisTransform = CORINFO_DEREF_THIS;
        }
    }

    //
    // Initialize callee context used for inlining and instantiation arguments
    //

    MethodDesc * pTargetMD = pMDAfterConstraintResolution;

    if (pTargetMD->HasMethodInstantiation())
    {
        pResult->contextHandle = MAKE_METHODCONTEXT(pTargetMD);
        pResult->exactContextNeedsRuntimeLookup = pTargetMD->GetMethodTable()->IsSharedByGenericInstantiations() || TypeHandle::IsCanonicalSubtypeInstantiation(pTargetMD->GetMethodInstantiation());
    }
    else
    {
        if (!exactType.IsTypeDesc())
        {
            // Because of .NET's notion of base calls, exactType may point to a sub-class
            // of the actual class that defines pTargetMD.  If the JIT decides to inline, it is
            // important that they 'match', so we fix exactType here.
#ifdef FEATURE_READYTORUN_COMPILER
            if (IsReadyToRunCompilation() &&
                !isVerifyOnly() && 
                !IsInSameVersionBubble((MethodDesc*)callerHandle, pTargetMD))
            {
                // For version resilient code we can only inline within the same version bubble;
                // we "repair" the precise types only for those callees.
                // The above condition needs to stay in sync with CEEInfo::canInline
            }
            else
#endif
            {
         
                exactType = pTargetMD->GetExactDeclaringType(exactType.AsMethodTable());
                _ASSERTE(!exactType.IsNull());
            }
        }

        pResult->contextHandle = MAKE_CLASSCONTEXT(exactType.AsPtr());
        pResult->exactContextNeedsRuntimeLookup = exactType.IsSharedByGenericInstantiations();
    }

    //
    // Determine whether to perform direct call
    //

    bool directCall = false;
    bool resolvedCallVirt = false;
    bool callVirtCrossingVersionBubble = false;


    // Delegate targets are always treated as direct calls here. (It would be nice to clean it up...).
    if (flags & CORINFO_CALLINFO_LDFTN)
    {
        if (m_pOverride != NULL)
            TypeEquivalenceFixupSpecificationHelper(m_pOverride, pTargetMD);
        directCall = true;
    }
    else
    // Static methods are always direct calls
    if (pTargetMD->IsStatic())
    {
        directCall = true;
    }
    else
    // Force all interface calls to be interpreted as if they are virtual.
    if (pTargetMD->GetMethodTable()->IsInterface())
    {
        directCall = false;
    }
    else
    if (!(flags & CORINFO_CALLINFO_CALLVIRT) || fResolvedConstraint)
    {
        directCall = true;
    }
    else
    {
        bool devirt;

#ifdef FEATURE_READYTORUN_COMPILER

        // if we are generating version resilient code
        // AND
        //    caller/callee are in different version bubbles
        // we have to apply more restrictive rules
        // These rules are related to the "inlining rules" as far as the
        // boundaries of a version bubble are concerned.

        if (IsReadyToRunCompilation() &&
            !isVerifyOnly() &&
            !IsInSameVersionBubble((MethodDesc*)callerHandle, pTargetMD)
           )
        {
            // For version resiliency we won't de-virtualize all final/sealed method calls.  Because during a 
            // servicing event it is legal to unseal a method or type.
            //
            // Note that it is safe to devirtualize in the following cases, since a servicing event cannot later modify it
            //  1) Callvirt on a virtual final method of a value type - since value types are sealed types as per ECMA spec
            //  2) Delegate.Invoke() - since a Delegate is a sealed class as per ECMA spec
            //  3) JIT intrinsics - since they have pre-defined behavior
            devirt = pTargetMD->GetMethodTable()->IsValueType() ||
                     (pTargetMD->GetMethodTable()->IsDelegate() && ((DelegateEEClass*)(pTargetMD->GetMethodTable()->GetClass()))->m_pInvokeMethod == pMD) ||
                     (pTargetMD->IsFCall() && ECall::GetIntrinsicID(pTargetMD) != CORINFO_INTRINSIC_Illegal);

            callVirtCrossingVersionBubble = true;
        }
        else
#endif
        {
            DWORD dwMethodAttrs = pTargetMD->GetAttrs();
            devirt = !IsMdVirtual(dwMethodAttrs) || IsMdFinal(dwMethodAttrs) || pTargetMD->GetMethodTable()->IsSealed();
        }

        if (devirt)
        {
            // We can't allow generic remotable methods to be considered resolved, it leads to a non-instantiating method desc being
            // passed to the remoting stub. The easiest way to deal with these is to force them through the virtual code path.
            // It is actually good to do this deoptimization for all remotable methods since remoting interception via vtable dispatch 
            // is faster then remoting interception via thunk
            if (!pTargetMD->IsRemotingInterceptedViaVirtualDispatch() /* || !pTargetMD->HasMethodInstantiation() */)
            {
                resolvedCallVirt = true;
                directCall = true;
            }
        }
    }

    if (directCall)
    {
        bool allowInstParam = (flags & CORINFO_CALLINFO_ALLOWINSTPARAM)
            // See code:IsRemotingInterceptedViaPrestub on why we need need to disallow inst param for remoting.
            && !( pTargetMD->MayBeRemotingIntercepted() && !pTargetMD->IsVtableMethod() );

        // Create instantiating stub if necesary
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

        if (((pResult->exactContextNeedsRuntimeLookup && pTargetMD->IsInstantiatingStub() && (!allowInstParam || fResolvedConstraint)) || fForceUseRuntimeLookup)
                // Handle invalid IL - see comment in code:CEEInfo::ComputeRuntimeLookupForSharedGenericToken
                && ContextIsShared(pResolvedToken->tokenContext))
        {
            _ASSERTE(!m_pMethodBeingCompiled->IsDynamicMethod());
            pResult->kind = CORINFO_CALL_CODE_POINTER;

            // For reference types, the constrained type does not affect method resolution
            DictionaryEntryKind entryKind = (!constrainedType.IsNull() && constrainedType.IsValueType()) ? ConstrainedMethodEntrySlot : MethodEntrySlot;

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

            if (IsReadyToRunCompilation())
            {
                // Compensate for always treating delegates as direct calls above
                if ((flags & CORINFO_CALLINFO_LDFTN) && (flags & CORINFO_CALLINFO_CALLVIRT) && !resolvedCallVirt)
                {
                   pResult->kind = CORINFO_VIRTUALCALL_LDVIRTFTN;
                }
            }
        }
        pResult->nullInstanceCheck = resolvedCallVirt;
    }
    // All virtual calls which take method instantiations must
    // currently be implemented by an indirect call via a runtime-lookup
    // function pointer
    else if (pTargetMD->HasMethodInstantiation())
    {
        pResult->kind = CORINFO_VIRTUALCALL_LDVIRTFTN;  // stub dispatch can't handle generic method calls yet
        pResult->nullInstanceCheck = TRUE;
    }
    // Non-interface dispatches go through the vtable
    else if (!pTargetMD->IsInterface() && !IsReadyToRunCompilation())
    {
        pResult->kind = CORINFO_VIRTUALCALL_VTABLE;
        pResult->nullInstanceCheck = TRUE;
    }
    else
    {
        if (IsReadyToRunCompilation())
        {
            // Insert explicit null checks for cross-version bubble non-interface calls. 
            // It is required to handle null checks properly for non-virtual <-> virtual change between versions
            pResult->nullInstanceCheck = !!(callVirtCrossingVersionBubble && !pTargetMD->IsInterface());
        }
        else
        {
            // No need to null check - the dispatch code will deal with null this.
            pResult->nullInstanceCheck = FALSE;
        }
#ifdef STUB_DISPATCH_PORTABLE
        pResult->kind = CORINFO_VIRTUALCALL_LDVIRTFTN;
#else // STUB_DISPATCH_PORTABLE
        pResult->kind = CORINFO_VIRTUALCALL_STUB;

        // We can't make stub calls when we need exact information
        // for interface calls from shared code.

        if (// If the token is not shared then we don't need a runtime lookup
            pResult->exactContextNeedsRuntimeLookup
            // Handle invalid IL - see comment in code:CEEInfo::ComputeRuntimeLookupForSharedGenericToken
            && ContextIsShared(pResolvedToken->tokenContext))
        {
            _ASSERTE(!m_pMethodBeingCompiled->IsDynamicMethod());
            
            ComputeRuntimeLookupForSharedGenericToken(DispatchStubAddrSlot,
                                                        pResolvedToken,
                                                        NULL,
                                                        pMD,
                                                        &pResult->stubLookup);
        }
        else
        {
            pResult->stubLookup.lookupKind.needsRuntimeLookup = false;

            BYTE * indcell = NULL;

            if (!(flags & CORINFO_CALLINFO_KINDONLY) && !isVerifyOnly())
            {
#ifndef CROSSGEN_COMPILE
                // We shouldn't be using GetLoaderAllocator here because for LCG, we need to get the 
                // VirtualCallStubManager from where the stub will be used. 
                // For normal methods there is no difference.
                LoaderAllocator *pLoaderAllocator = m_pMethodBeingCompiled->GetLoaderAllocatorForCode();
                VirtualCallStubManager *pMgr = pLoaderAllocator->GetVirtualCallStubManager();

                PCODE addr = pMgr->GetCallStub(exactType, pTargetMD);
                _ASSERTE(pMgr->isStub(addr));
                
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
#else // CROSSGEN_COMPILE
                // This path should be unreachable during crossgen
                _ASSERTE(false);
#endif // CROSSGEN_COMPILE
            }

            // We use an indirect call
            pResult->stubLookup.constLookup.accessType = IAT_PVALUE;
            pResult->stubLookup.constLookup.addr = indcell;
        }
#endif // STUB_DISPATCH_PORTABLE
    }

    pResult->hMethod = CORINFO_METHOD_HANDLE(pTargetMD);

    pResult->accessAllowed = CORINFO_ACCESS_ALLOWED;
    if ((flags & CORINFO_CALLINFO_SECURITYCHECKS) &&
        !((MethodDesc *)callerHandle)->IsILStub()) // IL stubs can access everything, don't bother doing access checks
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
            else
            if (pResolvedToken->pMethodSpec != NULL)
            {
                DWORD nGenericMethodArgs = 0;
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

                for (DWORD i = 0; i < nGenericMethodArgs; i++)
                {
                    genericMethodArgs[i] = sp.GetTypeHandleThrowing((Module *)pResolvedToken->tokenScope, &typeContext);
                    _ASSERTE (!genericMethodArgs[i].IsNull());
                    IfFailThrow(sp.SkipExactlyOne());
                }

                pCalleeForSecurity = MethodDesc::FindOrCreateAssociatedMethodDesc(pMD, calleeTypeForSecurity.GetMethodTable(), FALSE, Instantiation(genericMethodArgs, nGenericMethodArgs), FALSE);
            }
            else
            if (pResolvedToken->pTypeSpec != NULL)
            {
                pCalleeForSecurity = MethodDesc::FindOrCreateAssociatedMethodDesc(pMD, calleeTypeForSecurity.GetMethodTable(), FALSE, Instantiation(), TRUE);
            }
        }

        TypeHandle callerTypeForSecurity = TypeHandle(pCallerForSecurity->GetMethodTable());

        //This just throws.
        if (pCalleeForSecurity->RequiresLinktimeCheck())
        {
#ifdef FEATURE_CORECLR
            //hostProtectionAttribute(HPA) can be removed for coreclr mscorlib.dll
            //So if the call to LinktimeCheckMethod() is only b'coz of HPA then skip it
            if (!pCalleeForSecurity->RequiresLinkTimeCheckHostProtectionOnly())
#endif
                Security::LinktimeCheckMethod(pCallerForSecurity->GetAssembly(), pCalleeForSecurity);
        }

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
            StaticAccessCheckContext accessContext(pCallerForSecurity, callerTypeForSecurity.GetMethodTable());

            canAccessMethod = ClassLoader::CanAccess(&accessContext,
                                                     calleeTypeForSecurity.GetMethodTable(),
                                                     calleeTypeForSecurity.GetAssembly(),
                                                     pCalleeForSecurity->GetAttrs(),
                                                     pCalleeForSecurity,
                                                     NULL,
                                                     accessCheckOptions,
#ifdef FEATURE_CORECLR
                                                     FALSE,
#else
                                                     TRUE,
#endif //FEATURE_CORECLR
                                                     TRUE
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
                StaticAccessCheckContext accessContext(pCallerForSecurity, callerTypeForSecurity.GetMethodTable());

                MethodTable* pTypeParamMT = typeParam.GetMethodTable();

                // No accees check is need for Var, MVar, or FnPtr.
                if (pTypeParamMT != NULL)
                    canAccessMethod = ClassLoader::CanAccessClassForExtraChecks(&accessContext,
                                                                                pTypeParamMT,
                                                                                typeParam.GetAssembly(),
                                                                                accessCheckOptions,
                                                                                TRUE);
            }

            pResult->accessAllowed = canAccessMethod ? CORINFO_ACCESS_ALLOWED : CORINFO_ACCESS_ILLEGAL;
            if (!canAccessMethod)
            {
                //Check failed, fill in the throw exception helper.
                pResult->callsiteCalloutHelper.helperNum = CORINFO_HELP_METHOD_ACCESS_EXCEPTION;
                pResult->callsiteCalloutHelper.numArgs = 2;

                pResult->callsiteCalloutHelper.args[0].Set(CORINFO_METHOD_HANDLE(pCallerForSecurity));
                pResult->callsiteCalloutHelper.args[1].Set(CORINFO_METHOD_HANDLE(pCalleeForSecurity));

                //We now embed open instantiations in a few places for security callouts (since you can only
                //do the security check on the open instantiation).  We throw these methods out in
                //TriageMethodForZap.  In addition, NGen has problems referencing them properly.  Just throw out the whole
                //method and rejit at runtime.
                if (IsCompilingForNGen())
                {
                    if (pCallerForSecurity->ContainsGenericVariables()
                        || pCalleeForSecurity->ContainsGenericVariables())
                    {
                        COMPlusThrowNonLocalized(kNotSupportedException, W("Cannot embed generic MethodDesc"));
                    }
                }
            }

            //Only do this if we're allowed to access the method under any circumstance.
            if (canAccessMethod)
            {
                BOOL fNeedsTransparencyCheck = TRUE;

#ifdef FEATURE_CORECLR
                // All LCG methods are transparent in CoreCLR. When we switch from PT
                // to FT most user assemblies will become opportunistically critical.
                // If a LCG method calls a method in such an assembly it will stop working.
                // To avoid this we allow LCG methods to call user critical code in FT.
                // There is no security concern because the domain is fully trusted anyway.
                // There is nothing the LCG method can do that user code cannot do directly.
                // This is also consistent with the desktop where a transparent->critical
                // access will be converted to a demand and succeed in FT if the caller is
                // level1 and the target is level2. 
                // See also AccessCheckOptions::DemandMemberAccess.
                if (GetAppDomain()->GetSecurityDescriptor()->IsFullyTrusted() && pCallerForSecurity->IsLCGMethod())
                    fNeedsTransparencyCheck = FALSE;
#endif // FEATURE_CORECLR

                if (fNeedsTransparencyCheck)
                {
                    CorInfoSecurityRuntimeChecks runtimeChecks = CORINFO_ACCESS_SECURITY_NONE;

                    // See if transparency requires the runtime check too
                    CorInfoIsAccessAllowedResult isCallAllowedResult = 
                        Security::RequiresTransparentAssemblyChecks(pCallerForSecurity, pCalleeForSecurity, NULL);

                    if (isCallAllowedResult != CORINFO_ACCESS_ALLOWED)
                        runtimeChecks = CORINFO_ACCESS_SECURITY_TRANSPARENCY;

                    DebugSecurityCalloutStress(getMethodBeingCompiled(), isCallAllowedResult, runtimeChecks);

                    if (isCallAllowedResult == CORINFO_ACCESS_RUNTIME_CHECK)
                    {
                        pResult->accessAllowed = CORINFO_ACCESS_RUNTIME_CHECK;
                        //Explain the callback to the JIT.
                        pResult->callsiteCalloutHelper.helperNum = CORINFO_HELP_METHOD_ACCESS_CHECK;
                        pResult->callsiteCalloutHelper.numArgs = 4;

                        pResult->callsiteCalloutHelper.args[0].Set(CORINFO_METHOD_HANDLE(pCallerForSecurity));
                        pResult->callsiteCalloutHelper.args[1].Set(CORINFO_METHOD_HANDLE(pCalleeForSecurity));
                        pResult->callsiteCalloutHelper.args[2].Set(CORINFO_CLASS_HANDLE(calleeTypeForSecurity.AsPtr()));
                        pResult->callsiteCalloutHelper.args[3].Set(runtimeChecks);

                        if (IsCompilingForNGen())
                        {
                            //see code:CEEInfo::getCallInfo for more information.
                            if (pCallerForSecurity->ContainsGenericVariables()
                                || pCalleeForSecurity->ContainsGenericVariables())
                            {
                                COMPlusThrowNonLocalized(kNotSupportedException, W("Cannot embed generic MethodDesc"));
                            }
                        }
                    }
                    else
                    {
                        _ASSERTE(pResult->accessAllowed == CORINFO_ACCESS_ALLOWED);
                        _ASSERTE(isCallAllowedResult == CORINFO_ACCESS_ALLOWED);
                    }
                }
            }
        }

    }

    //We're pretty much done at this point.  Let's grab the rest of the information that the jit is going to
    //need.
    pResult->classFlags = getClassAttribsInternal(pResolvedToken->hClass);

    pResult->methodFlags = getMethodAttribsInternal(pResult->hMethod);
    getMethodSigInternal(pResult->hMethod, &pResult->sig, (pResult->hMethod == pResolvedToken->hMethod) ? pResolvedToken->hClass : NULL);

    if (flags & CORINFO_CALLINFO_VERIFICATION)
    {
        if (pResult->hMethod != pResolvedToken->hMethod)
        {
            pResult->verMethodFlags = getMethodAttribsInternal(pResolvedToken->hMethod);
            getMethodSigInternal(pResolvedToken->hMethod, &pResult->verSig, pResolvedToken->hClass);
        }
        else
        {
            pResult->verMethodFlags = pResult->methodFlags;
            pResult->verSig = pResult->sig;
        }
    }

    EE_TO_JIT_TRANSITION();
}

BOOL CEEInfo::canAccessFamily(CORINFO_METHOD_HANDLE hCaller,
                              CORINFO_CLASS_HANDLE hInstanceType)
{
    WRAPPER_NO_CONTRACT;

    BOOL ret = FALSE;

    //Since this is only for verification, I don't need to do the demand.
    JIT_TO_EE_TRANSITION();

    TypeHandle targetType = TypeHandle(hInstanceType);
    TypeHandle accessingType = TypeHandle(GetMethod(hCaller)->GetMethodTable());
    AccessCheckOptions::AccessCheckType accessCheckOptions = AccessCheckOptions::kNormalAccessibilityChecks;
    DynamicResolver* pIgnored;
    BOOL doCheck = TRUE;
    if (GetMethod(hCaller)->IsDynamicMethod())
    {
        //If this is a DynamicMethod, perform the check from the type to which the DynamicMethod was
        //attached.
        //
        //If this is a dynamic method, don't do this check.  If they specified SkipVisibilityChecks
        //(ModifyCheckForDynamicMethod returned false), we should obviously skip the check for the C++
        //protected rule (since we skipped all the other visibility checks).  If they specified
        //RestrictedSkipVisibilityChecks, then they're a "free" DynamicMethod.  This check is meaningless
        //(i.e.  it would always fail).  We've already done a demand for access to the member.  Let that be
        //enough.
        doCheck = ModifyCheckForDynamicMethod(GetMethod(hCaller)->AsDynamicMethodDesc()->GetResolver(),
                                              &accessingType, &accessCheckOptions, &pIgnored);
        if (accessCheckOptions == AccessCheckOptions::kRestrictedMemberAccess 
#ifdef FEATURE_CORECLR
            || accessCheckOptions == AccessCheckOptions::kRestrictedMemberAccessNoTransparency
#endif //FEATURE_CORECLR
            )
            doCheck = FALSE;
    }

    if (doCheck)
    {
        ret = ClassLoader::CanAccessFamilyVerification(accessingType, targetType);
    }
    else
    {
        ret = TRUE;
    }

    EE_TO_JIT_TRANSITION();
    return ret;
}
void CEEInfo::ThrowExceptionForHelper(const CORINFO_HELPER_DESC * throwHelper)
{
    CONTRACTL {
        SO_TOLERANT;
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    } CONTRACTL_END;

    JIT_TO_EE_TRANSITION();

    _ASSERTE(throwHelper->args[0].argType == CORINFO_HELPER_ARG_TYPE_Method);
    MethodDesc *pCallerMD = GetMethod(throwHelper->args[0].methodHandle);

    StaticAccessCheckContext accessContext(pCallerMD);

    switch (throwHelper->helperNum)
    {
    case CORINFO_HELP_METHOD_ACCESS_EXCEPTION:
        {
            _ASSERTE(throwHelper->args[1].argType == CORINFO_HELPER_ARG_TYPE_Method);
            ThrowMethodAccessException(&accessContext, GetMethod(throwHelper->args[1].methodHandle));
        }
        break;
    case CORINFO_HELP_FIELD_ACCESS_EXCEPTION:
        {
            _ASSERTE(throwHelper->args[1].argType == CORINFO_HELPER_ARG_TYPE_Field);
            ThrowFieldAccessException(&accessContext, reinterpret_cast<FieldDesc *>(throwHelper->args[1].fieldHandle));
        }
        break;
    case CORINFO_HELP_CLASS_ACCESS_EXCEPTION:
        {
            _ASSERTE(throwHelper->args[1].argType == CORINFO_HELPER_ARG_TYPE_Class);
            TypeHandle typeHnd(throwHelper->args[1].classHandle);
            ThrowTypeAccessException(&accessContext, typeHnd.GetMethodTable());
        }
        break;

    default:
        _ASSERTE(!"Unknown access exception type");
    }
    EE_TO_JIT_TRANSITION();
}


BOOL CEEInfo::isRIDClassDomainID(CORINFO_CLASS_HANDLE cls)
{
    CONTRACTL {
        SO_TOLERANT;
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    } CONTRACTL_END;

    BOOL result = FALSE;

    JIT_TO_EE_TRANSITION();

    TypeHandle  VMClsHnd(cls);

    result = !VMClsHnd.AsMethodTable()->IsDynamicStatics();

    EE_TO_JIT_TRANSITION();

    return result;
}


/***********************************************************************/
unsigned CEEInfo::getClassDomainID (CORINFO_CLASS_HANDLE clsHnd,
                                    void **ppIndirection)
{
    CONTRACTL {
        SO_TOLERANT;
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
// Used by the JIT to determine whether the profiler or IBC is tracking object
// allocations
//
// Return Value:
//    bool indicating whether the profiler or IBC is tracking object allocations
//
// Notes:
//    Normally, a profiler would just directly call the inline helper to determine
//    whether the profiler set the relevant event flag (e.g.,
//    CORProfilerTrackAllocationsEnabled). However, this wrapper also asks whether we're
//    running for IBC instrumentation or enabling the object allocated ETW event. If so,
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
        (g_IBCLogger.InstrEnabled() != FALSE)
#ifdef PROFILING_SUPPORTED
        || CORProfilerTrackAllocationsEnabled()
#endif // PROFILING_SUPPORTED
#ifdef FEATURE_EVENT_TRACE
        || ETW::TypeSystemLog::IsHeapAllocEventEnabled()
#endif // FEATURE_EVENT_TRACE
        );
}

/***********************************************************************/
CorInfoHelpFunc CEEInfo::getNewHelper(CORINFO_RESOLVED_TOKEN * pResolvedToken, CORINFO_METHOD_HANDLE callerHandle)
{
    CONTRACTL {
        SO_TOLERANT;
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    } CONTRACTL_END;

    CorInfoHelpFunc result = CORINFO_HELP_UNDEF;

    JIT_TO_EE_TRANSITION();

    TypeHandle  VMClsHnd(pResolvedToken->hClass);

    if(VMClsHnd.IsTypeDesc())
    {
        COMPlusThrow(kInvalidOperationException,W("InvalidOperation_CantInstantiateFunctionPointer"));
    }

    if(VMClsHnd.IsAbstract())
    {
        COMPlusThrow(kInvalidOperationException,W("InvalidOperation_CantInstantiateAbstractClass"));
    }

    MethodTable* pMT = VMClsHnd.AsMethodTable();
#ifdef FEATURE_COMINTEROP
    if (pMT->IsComObjectType() && !GetMethod(callerHandle)->GetModule()->GetSecurityDescriptor()->CanCallUnmanagedCode())
    {
        // Caller does not have permission to make interop calls. Generate a
        // special helper that will throw a security exception when called.
        result = CORINFO_HELP_SEC_UNMGDCODE_EXCPT;
    }
    else
#endif // FEATURE_COMINTEROP
    {
        result = getNewHelperStatic(pMT);
    }

    _ASSERTE(result != CORINFO_HELP_UNDEF);
        
    EE_TO_JIT_TRANSITION();

    return result;
}

/***********************************************************************/
CorInfoHelpFunc CEEInfo::getNewHelperStatic(MethodTable * pMT)
{
    STANDARD_VM_CONTRACT;

#ifdef FEATURE_REMOTING
    if (pMT->MayRequireManagedActivation())
    {
        return CORINFO_HELP_NEW_CROSSCONTEXT;
    }
#endif

    // Slow helper is the default
    CorInfoHelpFunc helper = CORINFO_HELP_NEWFAST;

#ifdef FEATURE_REMOTING
    // We shouldn't get here with a COM object (they're all potentially
    // remotable, so they're covered by the case above).
    _ASSERTE(!pMT->IsComObjectType() || pMT->IsWinRTObjectType());
#endif

    if (pMT->IsComObjectType())
    {
        // Use slow helper
        _ASSERTE(helper == CORINFO_HELP_NEWFAST);
    }
    else
    if ((pMT->GetBaseSize() >= LARGE_OBJECT_SIZE) || 
        pMT->HasFinalizer())
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
    // If we are use the the fast allocator we also may need the 
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
        SO_TOLERANT;
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

    ArrayTypeDesc* arrayTypeDesc = clsHnd.AsArray();
    _ASSERTE(arrayTypeDesc->GetInternalCorElementType() == ELEMENT_TYPE_SZARRAY);

    if (GCStress<cfg_alloc>::IsEnabled())
    {
        return CORINFO_HELP_NEWARR_1_DIRECT;
    }

    CorInfoHelpFunc result = CORINFO_HELP_UNDEF;

    TypeHandle thElemType = arrayTypeDesc->GetTypeParam();
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
        SO_TOLERANT;
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    } CONTRACTL_END;

    if (isVerifyOnly())
        return fThrowing ? CORINFO_HELP_CHKCASTANY : CORINFO_HELP_ISINSTANCEOFANY;

    CorInfoHelpFunc result = CORINFO_HELP_UNDEF;

    JIT_TO_EE_TRANSITION();

    bool fClassMustBeRestored;
    result = getCastingHelperStatic(TypeHandle(pResolvedToken->hClass), fThrowing, &fClassMustBeRestored);
    if (fClassMustBeRestored && m_pOverride != NULL)
        m_pOverride->classMustBeLoadedBeforeCodeIsRun(pResolvedToken->hClass);

    EE_TO_JIT_TRANSITION();

    return result;
}

/***********************************************************************/
CorInfoHelpFunc CEEInfo::getCastingHelperStatic(TypeHandle clsHnd, bool fThrowing, bool * pfClassMustBeRestored)
{
    STANDARD_VM_CONTRACT;

    // Slow helper is the default
    int helper = CORINFO_HELP_ISINSTANCEOFANY;

    *pfClassMustBeRestored = false;

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
        // Casting to variant type requires the type to be fully loaded
        *pfClassMustBeRestored = true;

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
        if (clsHnd.AsArray()->GetInternalCorElementType() != ELEMENT_TYPE_SZARRAY)
        {
            // Casting to multidimensional array type requires restored pointer to EEClass to fetch rank
            *pfClassMustBeRestored = true;
        }

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

#ifdef FEATURE_PREJIT
    BOOL t1, t2, forceInstr;
    SystemDomain::GetCompilationOverrides(&t1, &t2, &forceInstr);
    if (forceInstr)
    {
        // If we're compiling for instrumentation, use the slowest but instrumented cast helper
        helper = CORINFO_HELP_ISINSTANCEOFANY;
    }
#endif

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
        SO_TOLERANT;
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

    if (m_pOverride != NULL)
        m_pOverride->classMustBeLoadedBeforeCodeIsRun(clsHnd);

    TypeHandle VMClsHnd(clsHnd);
    if (Nullable::IsNullableType(VMClsHnd))
        return CORINFO_HELP_UNBOX_NULLABLE;
    
    return CORINFO_HELP_UNBOX;
}

/***********************************************************************/
void CEEInfo::getReadyToRunHelper(
        CORINFO_RESOLVED_TOKEN * pResolvedToken,
        CorInfoHelpFunc          id,
        CORINFO_CONST_LOOKUP *   pLookup
        )
{
    LIMITED_METHOD_CONTRACT;
    UNREACHABLE();      // only called during NGen
}

/***********************************************************************/
void CEEInfo::getReadyToRunDelegateCtorHelper(
        CORINFO_RESOLVED_TOKEN * pTargetMethod,
        CORINFO_CLASS_HANDLE     delegateType,
        CORINFO_CONST_LOOKUP *   pLookup
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
        SO_TOLERANT;
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
        // Dev10 718281 - This has been functionally broken fora very long time (at least 2.0).
        // The recent addition of the check for stack pointers has caused it to now AV instead
        // of gracefully failing with an InvalidOperationException. Since nobody has noticed
        // it being broken, we are choosing not to invest to fix it, and instead explicitly
        // breaking it and failing early and consistently.
        if(VMClsHnd.IsTypeDesc())
        {
            COMPlusThrow(kInvalidOperationException,W("InvalidOperation_TypeCannotBeBoxed"));
        }

        // we shouldn't allow boxing of types that contains stack pointers
        // csc and vbc already disallow it.
        if (VMClsHnd.AsMethodTable()->ContainsStackPtr())
            COMPlusThrow(kInvalidProgramException);

        result = CORINFO_HELP_BOX;
    }
    
    EE_TO_JIT_TRANSITION();

    return result;
}

/***********************************************************************/
CorInfoHelpFunc CEEInfo::getSecurityPrologHelper(CORINFO_METHOD_HANDLE ftn)
{
    CONTRACTL {
        SO_TOLERANT;
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    } CONTRACTL_END;

    CorInfoHelpFunc result = CORINFO_HELP_UNDEF;

    JIT_TO_EE_TRANSITION();

#ifdef FEATURE_NATIVE_IMAGE_GENERATION
    // This will make sure that when IBC logging is on, we call the slow helper with IBC probe
    if (IsCompilingForNGen() &&
        GetAppDomain()->ToCompilationDomain()->m_fForceInstrument)
    {
        result = CORINFO_HELP_SECURITY_PROLOG_FRAMED;
    }
#endif // FEATURE_NATIVE_IMAGE_GENERATION

    if (result == CORINFO_HELP_UNDEF)
    {
        result = CORINFO_HELP_SECURITY_PROLOG;
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
        SO_TOLERANT;
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
        SO_TOLERANT;
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
const char* CEEInfo::getMethodName (CORINFO_METHOD_HANDLE ftnHnd, const char** scopeName)
{
    CONTRACTL {
        SO_TOLERANT;
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    } CONTRACTL_END;

    const char* result = NULL;

    JIT_TO_EE_TRANSITION();

    MethodDesc *ftn;

    ftn = GetMethod(ftnHnd);

    if (scopeName != 0)
    {
        if (ftn->IsLCGMethod())
        {
            *scopeName = "DynamicClass";
        }
        else if (ftn->IsILStub())
        {
            *scopeName = ILStubResolver::GetStubClassName(ftn);
        }
        else
        {
            MethodTable * pMT = ftn->GetMethodTable();
#if defined(_DEBUG) 
#ifdef FEATURE_SYMDIFF
            if (CLRConfig::GetConfigValue(CLRConfig::INTERNAL_SymDiffDump))
            {
                if (pMT->IsArray())
                {
                    ssClsNameBuff.Clear();
                    ssClsNameBuff.SetUTF8(pMT->GetDebugClassName());
                }
                else
                    pMT->_GetFullyQualifiedNameForClassNestedAware(ssClsNameBuff);
            }
            else
            {
#endif
                // Calling _GetFullyQualifiedNameForClass in chk build is very expensive
                // since it construct the class name everytime we call this method. In chk
                // builds we already have a cheaper way to get the class name -
                // GetDebugClassName - which doesn't calculate the class name everytime.
                // This results in huge saving in Ngen time for checked builds. 
                ssClsNameBuff.Clear();
                ssClsNameBuff.SetUTF8(pMT->GetDebugClassName());

#ifdef FEATURE_SYMDIFF
            }
#endif            
            // Append generic instantiation at the end
            Instantiation inst = pMT->GetInstantiation();
            if (!inst.IsEmpty())
                TypeString::AppendInst(ssClsNameBuff, inst);
                
            *scopeName = ssClsNameBuff.GetUTF8(ssClsNameBuffScratch);
#else // !_DEBUG
            // since this is for diagnostic purposes only,
            // give up on the namespace, as we don't have a buffer to concat it
            // also note this won't show array class names.
            LPCUTF8 nameSpace;
            *scopeName= pMT->GetFullyQualifiedNameInfo(&nameSpace);
#endif // !_DEBUG
        }
    }

    result = ftn->GetName();

    EE_TO_JIT_TRANSITION();
    
    return result;
}

/*********************************************************************/
DWORD CEEInfo::getMethodAttribs (CORINFO_METHOD_HANDLE ftn)
{
    CONTRACTL {
        SO_TOLERANT;
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
#ifndef CROSSGEN_COMPILE
#ifdef FEATURE_COMPRESSEDSTACK
        if(SecurityStackWalk::MethodIsAnonymouslyHostedDynamicMethodWithCSToEvaluate(pMD))
        {
            return CORINFO_FLG_STATIC | CORINFO_FLG_DONT_INLINE | CORINFO_FLG_SECURITYCHECK;
        }
#endif // FEATURE_COMPRESSEDSTACK
#endif // !CROSSGEN_COMPILE

        return CORINFO_FLG_STATIC | CORINFO_FLG_DONT_INLINE | CORINFO_FLG_NOSECURITYWRAP;
    }

    DWORD result = 0;

    // <REVISIT_TODO>@todo: can we git rid of CORINFO_FLG_ stuff and just include cor.h?</REVISIT_TODO>

    DWORD attribs = pMD->GetAttrs();

    if (IsMdFamily(attribs))
        result |= CORINFO_FLG_PROTECTED;
    if (IsMdStatic(attribs))
        result |= CORINFO_FLG_STATIC;
    if (pMD->IsSynchronized())
        result |= CORINFO_FLG_SYNCH;
    if (pMD->IsFCallOrIntrinsic())
        result |= CORINFO_FLG_NOGCCHECK | CORINFO_FLG_INTRINSIC;
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

    if (!pMD->IsInterceptedForDeclSecurity())
    {
        result |= CORINFO_FLG_NOSECURITYWRAP;
    }


    // Check for an inlining directive.
    if (pMD->IsNotInline())
    {
        /* Function marked as not inlineable */
        result |= CORINFO_FLG_DONT_INLINE;

        if (pMD->IsIL() && (IsMdRequireSecObject(attribs) ||
            (pMD->GetModule()->IsSystem() && IsMiNoInlining(pMD->GetImplAttrs()))))
        {
            // Assume all methods marked as NoInline inside mscorlib are
            // marked that way because they use StackCrawlMark to identify
            // the caller (not just the security info).
            // See comments in canInline or canTailCall
            result |= CORINFO_FLG_DONT_INLINE_CALLER;
        }
    }

    // AggressiveInlining only makes sense for IL methods.
    else if (pMD->IsIL() && IsMiAggressiveInlining(pMD->GetImplAttrs()))
    {
        result |= CORINFO_FLG_FORCEINLINE;
    }


    if (!pMD->IsRuntimeSupplied())
    {
        if (IsMdRequireSecObject(attribs))
        {
            result |= CORINFO_FLG_SECURITYCHECK;
        }
    }

    if (pMT->IsDelegate() && ((DelegateEEClass*)(pMT->GetClass()))->m_pInvokeMethod == pMD)
    {
        // This is now used to emit efficient invoke code for any delegate invoke,
        // including multicast.
        result |= CORINFO_FLG_DELEGATE_INVOKE;
    }

    return result;
}

/*********************************************************************/
void CEEInfo::setMethodAttribs (
        CORINFO_METHOD_HANDLE ftnHnd,
        CorInfoMethodRuntimeFlags attribs)
{
    CONTRACTL {
        SO_TOLERANT;
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    } CONTRACTL_END;

    JIT_TO_EE_TRANSITION();

    MethodDesc* ftn = GetMethod(ftnHnd);

    if (attribs & CORINFO_FLG_BAD_INLINEE)
    {
        BOOL fCacheInliningHint = TRUE;

#ifdef FEATURE_NATIVE_IMAGE_GENERATION
        if (IsCompilationProcess())
        {
            // Since we are running managed code during NGen the inlining hint may be 
            // changing underneeth us as the code is JITed. We need to prevent the inlining
            // hints from changing once we start to use them to place IL in the image.
            if (!g_pCEECompileInfo->IsCachingOfInliningHintsEnabled())
            {
                fCacheInliningHint = FALSE;
            }
            else
            {
                // Don't cache inlining hints inside mscorlib during NGen of other assemblies,
                // since mscorlib is loaded domain neutral and will survive worker process recycling,
                // causing determinism problems.
                Module * pModule = ftn->GetModule();
                if (pModule->IsSystem() && pModule->HasNativeImage())
                {
                    fCacheInliningHint = FALSE;
                }
            }
        }
#endif

        if (fCacheInliningHint)
        {
            ftn->SetNotInline(true);
        }
    }

    // Both CORINFO_FLG_UNVERIFIABLE and CORINFO_FLG_VERIFIABLE cannot be set
    _ASSERTE(!(attribs & CORINFO_FLG_UNVERIFIABLE) || 
             !(attribs & CORINFO_FLG_VERIFIABLE  ));

    if (attribs & CORINFO_FLG_VERIFIABLE)
        ftn->SetIsVerified(TRUE);
    else if (attribs & CORINFO_FLG_UNVERIFIABLE)
        ftn->SetIsVerified(FALSE);

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

/*********************************************************************

IL is the most efficient and portable way to implement certain low level methods 
in mscorlib.dll. Unfortunately, there is no good way to link IL into mscorlib.dll today.
Until we find a good way to link IL into mscorlib.dll, we will provide the IL implementation here.

- All IL intrinsincs are members of System.Runtime.CompilerServices.JitHelpers class
- All IL intrinsincs should be kept very simple. Implement the minimal reusable version of 
unsafe construct and depend on inlining to do the rest.
- The C# implementation of the IL intrinsic should be good enough for functionalily. Everything should work 
correctly (but slower) if the IL intrinsics are removed.

*********************************************************************/

bool getILIntrinsicImplementation(MethodDesc * ftn,
                                  CORINFO_METHOD_INFO * methInfo)
{
    STANDARD_VM_CONTRACT;

    // Precondition: ftn is a method in mscorlib 
    _ASSERTE(ftn->GetModule()->IsSystem());

    mdMethodDef tk = ftn->GetMemberDef();

    // Compare tokens to cover all generic instantiations
    // The body of the first method is simply ret Arg0. The second one first casts the arg to I4.

    if (tk == MscorlibBinder::GetMethod(METHOD__JIT_HELPERS__UNSAFE_CAST)->GetMemberDef())
    {
        // Return the argument that was passed in.
        static const BYTE ilcode[] = { CEE_LDARG_0, CEE_RET };
        methInfo->ILCode = const_cast<BYTE*>(ilcode);
        methInfo->ILCodeSize = sizeof(ilcode);
        methInfo->maxStack = 1;
        methInfo->EHcount = 0;
        methInfo->options = (CorInfoOptions)0;
        return true;
    }
    else if (tk == MscorlibBinder::GetMethod(METHOD__JIT_HELPERS__UNSAFE_CAST_TO_STACKPTR)->GetMemberDef())
    {
        // Return the argument that was passed in converted to IntPtr
        static const BYTE ilcode[] = { CEE_LDARG_0, CEE_CONV_I, CEE_RET };
        methInfo->ILCode = const_cast<BYTE*>(ilcode);
        methInfo->ILCodeSize = sizeof(ilcode);
        methInfo->maxStack = 1;
        methInfo->EHcount = 0;
        methInfo->options = (CorInfoOptions)0;
        return true;
    }
    else if (tk == MscorlibBinder::GetMethod(METHOD__JIT_HELPERS__UNSAFE_ENUM_CAST)->GetMemberDef()) 
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
            et == ELEMENT_TYPE_U1)
        {
            // Cast to I4 and return the argument that was passed in.
            static const BYTE ilcode[] = { CEE_LDARG_0, CEE_CONV_I4, CEE_RET };
            methInfo->ILCode = const_cast<BYTE*>(ilcode);
            methInfo->ILCodeSize = sizeof(ilcode);
            methInfo->maxStack = 1;
            methInfo->EHcount = 0;
            methInfo->options = (CorInfoOptions)0;
            return true;
        }
    }
    else if (tk == MscorlibBinder::GetMethod(METHOD__JIT_HELPERS__UNSAFE_ENUM_CAST_LONG)->GetMemberDef()) 
    {
        // The the comment above on why this is is not an unconditional replacement.  This case handles
        // Enums backed by 8 byte values.

        _ASSERTE(ftn->HasMethodInstantiation());
        Instantiation inst = ftn->GetMethodInstantiation();

        _ASSERTE(inst.GetNumArgs() == 1);
        CorElementType et = inst[0].GetVerifierCorElementType();
        if (et == ELEMENT_TYPE_I8 ||
            et == ELEMENT_TYPE_U8)
        {
            // Cast to I8 and return the argument that was passed in.
            static const BYTE ilcode[] = { CEE_LDARG_0, CEE_CONV_I8, CEE_RET };
            methInfo->ILCode = const_cast<BYTE*>(ilcode);
            methInfo->ILCodeSize = sizeof(ilcode);
            methInfo->maxStack = 1;
            methInfo->EHcount = 0;
            methInfo->options = (CorInfoOptions)0;
            return true;
        }
    }

    return false;
}

bool getILIntrinsicImplementationForVolatile(MethodDesc * ftn,
                                             CORINFO_METHOD_INFO * methInfo)
{
    STANDARD_VM_CONTRACT;

    //
    // This replaces the implementations of Volatile.* in mscorlib with more efficient ones.
    // We do this because we cannot otherwise express these in C#.  What we *want* to do is
    // to treat the byref args to these methods as "volatile."  In pseudo-C#, this would look
    // like:
    //
    //   int Read(ref volatile int location)
    //   {
    //       return location;
    //   }
    //
    // However, C# does not yet provide a way to declare a byref as "volatile."  So instead,
    // we substitute raw IL bodies for these methods that use the correct volatile instructions.
    //

    // Precondition: ftn is a method in mscorlib in the System.Threading.Volatile class
    _ASSERTE(ftn->GetModule()->IsSystem());
    _ASSERTE(MscorlibBinder::IsClass(ftn->GetMethodTable(), CLASS__VOLATILE));
    _ASSERTE(strcmp(ftn->GetMethodTable()->GetClass()->GetDebugClassName(), "System.Threading.Volatile") == 0);

    const size_t VolatileMethodBodySize = 6;

    struct VolatileMethodImpl
    {
        BinderMethodID methodId;
        BYTE body[VolatileMethodBodySize];
    };

#define VOLATILE_IMPL(type, loadinst, storeinst) \
    { \
        METHOD__VOLATILE__READ_##type, \
        { \
            CEE_LDARG_0, \
            CEE_PREFIX1, (CEE_VOLATILE & 0xFF), \
            loadinst, \
            CEE_NOP, /*pad to VolatileMethodBodySize bytes*/ \
            CEE_RET \
        } \
    }, \
    { \
        METHOD__VOLATILE__WRITE_##type, \
        { \
            CEE_LDARG_0, \
            CEE_LDARG_1, \
            CEE_PREFIX1, (CEE_VOLATILE & 0xFF), \
            storeinst, \
            CEE_RET \
        } \
    },

    static const VolatileMethodImpl volatileImpls[] =
    {
        VOLATILE_IMPL(T,       CEE_LDIND_REF, CEE_STIND_REF)
        VOLATILE_IMPL(Bool,    CEE_LDIND_I1,  CEE_STIND_I1)
        VOLATILE_IMPL(Int,     CEE_LDIND_I4,  CEE_STIND_I4)
        VOLATILE_IMPL(IntPtr,  CEE_LDIND_I,   CEE_STIND_I)
        VOLATILE_IMPL(UInt,    CEE_LDIND_U4,  CEE_STIND_I4)
        VOLATILE_IMPL(UIntPtr, CEE_LDIND_I,   CEE_STIND_I)
        VOLATILE_IMPL(SByt,    CEE_LDIND_I1,  CEE_STIND_I1)
        VOLATILE_IMPL(Byte,    CEE_LDIND_U1,  CEE_STIND_I1)
        VOLATILE_IMPL(Shrt,    CEE_LDIND_I2,  CEE_STIND_I2)
        VOLATILE_IMPL(UShrt,   CEE_LDIND_U2,  CEE_STIND_I2)
        VOLATILE_IMPL(Flt,     CEE_LDIND_R4,  CEE_STIND_R4)

        //
        // Ordinary volatile loads and stores only guarantee atomicity for pointer-sized (or smaller) data.
        // So, on 32-bit platforms we must use Interlocked operations instead for the 64-bit types.
        // The implementation in mscorlib already does this, so we will only substitute a new
        // IL body if we're running on a 64-bit platform.
        //
        IN_WIN64(VOLATILE_IMPL(Long,  CEE_LDIND_I8, CEE_STIND_I8))
        IN_WIN64(VOLATILE_IMPL(ULong, CEE_LDIND_I8, CEE_STIND_I8))
        IN_WIN64(VOLATILE_IMPL(Dbl,   CEE_LDIND_R8, CEE_STIND_R8))
    };

    mdMethodDef md = ftn->GetMemberDef();
    for (unsigned i = 0; i < NumItems(volatileImpls); i++)
    {
        if (md == MscorlibBinder::GetMethod(volatileImpls[i].methodId)->GetMemberDef())
        {
            methInfo->ILCode = const_cast<BYTE*>(volatileImpls[i].body);
            methInfo->ILCodeSize = VolatileMethodBodySize;
            methInfo->maxStack = 2;
            methInfo->EHcount = 0;
            methInfo->options = (CorInfoOptions)0;
            return true;
        }
    }

    return false;
}

bool getILIntrinsicImplementationForInterlocked(MethodDesc * ftn,
                                                CORINFO_METHOD_INFO * methInfo)
{
    STANDARD_VM_CONTRACT;

    // Precondition: ftn is a method in mscorlib in the System.Threading.Interlocked class
    _ASSERTE(ftn->GetModule()->IsSystem());
    _ASSERTE(MscorlibBinder::IsClass(ftn->GetMethodTable(), CLASS__INTERLOCKED));

    // We are only interested if ftn's token and CompareExchange<T> token match
    if (ftn->GetMemberDef() != MscorlibBinder::GetMethod(METHOD__INTERLOCKED__COMPARE_EXCHANGE_T)->GetMemberDef())
        return false;       

    // Get MethodDesc for System.Threading.Interlocked.CompareExchangeFast()
    MethodDesc* cmpxchgFast = MscorlibBinder::GetMethod(METHOD__INTERLOCKED__COMPARE_EXCHANGE_OBJECT);

    // The MethodDesc lookup must not fail, and it should have the name "CompareExchangeFast"
    _ASSERTE(cmpxchgFast != NULL);
    _ASSERTE(strcmp(cmpxchgFast->GetName(), "CompareExchange") == 0);

    // Setup up the body of the method
    static BYTE il[] = {
                          CEE_LDARG_0,
                          CEE_LDARG_1,
                          CEE_LDARG_2,
                          CEE_CALL,0,0,0,0, 
                          CEE_RET
                        };

    // Get the token for System.Threading.Interlocked.CompareExchangeFast(), and patch [target]
    mdMethodDef cmpxchgFastToken = cmpxchgFast->GetMemberDef();
    il[4] = (BYTE)((int)cmpxchgFastToken >> 0);
    il[5] = (BYTE)((int)cmpxchgFastToken >> 8);
    il[6] = (BYTE)((int)cmpxchgFastToken >> 16);
    il[7] = (BYTE)((int)cmpxchgFastToken >> 24);

    // Initialize methInfo
    methInfo->ILCode = const_cast<BYTE*>(il);
    methInfo->ILCodeSize = sizeof(il);
    methInfo->maxStack = 3;
    methInfo->EHcount = 0;
    methInfo->options = (CorInfoOptions)0;

    return true;
}

//---------------------------------------------------------------------------------------
// 
//static
void 
getMethodInfoHelper(
    MethodDesc *           ftn, 
    CORINFO_METHOD_HANDLE  ftnHnd, 
    COR_ILMETHOD_DECODER * header, 
    CORINFO_METHOD_INFO *  methInfo)
{
    STANDARD_VM_CONTRACT;

    _ASSERTE(ftn == GetMethod(ftnHnd));

    methInfo->ftn             = ftnHnd;
    methInfo->scope           = GetScopeHandle(ftn);
    methInfo->regionKind      = CORINFO_REGION_JIT;
    //
    // For Jitted code the regionKind is JIT;
    // For Ngen-ed code the zapper will set this to HOT or COLD, if we  
    // are using IBC data to partition methods into Hot/Cold regions

    /* Grab information from the IL header */

    PCCOR_SIGNATURE pLocalSig = NULL;
    DWORD           cbLocalSig = 0;

    if (NULL != header)
    {
        bool fILIntrinsic = false;

        MethodTable * pMT  = ftn->GetMethodTable();
      
        if (MscorlibBinder::IsClass(pMT, CLASS__JIT_HELPERS))
        {
            fILIntrinsic = getILIntrinsicImplementation(ftn, methInfo);
        }
        else if (MscorlibBinder::IsClass(pMT, CLASS__INTERLOCKED))
        {
            fILIntrinsic = getILIntrinsicImplementationForInterlocked(ftn, methInfo);
        }
        else if (MscorlibBinder::IsClass(pMT, CLASS__VOLATILE))
        {
            fILIntrinsic = getILIntrinsicImplementationForVolatile(ftn, methInfo);
        }

        if (!fILIntrinsic)
        {
            getMethodInfoILMethodHeaderHelper(header, methInfo);
            pLocalSig = header->LocalVarSig;
            cbLocalSig = header->cbLocalVarSig;
        }
    }
    else
    {
        _ASSERTE(ftn->IsDynamicMethod());

        DynamicResolver * pResolver = ftn->AsDynamicMethodDesc()->GetResolver();        
        unsigned int EHCount;
        methInfo->ILCode = pResolver->GetCodeInfo(&methInfo->ILCodeSize,
                                                  &methInfo->maxStack,
                                                  &methInfo->options,
                                                  &EHCount);
        methInfo->EHcount = (unsigned short)EHCount;
        SigPointer localSig = pResolver->GetLocalSig();
        localSig.GetSignature(&pLocalSig, &cbLocalSig);
    }

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
            BEGIN_PIN_PROFILER(CORProfilerPresent());
            if (g_profControlBlock.pProfInterface->RequiresGenericsContextForEnterLeave())
            {
                fProfilerRequiresGenericsContextForEnterLeave = TRUE;
            }
            END_PIN_PROFILER();
        }
        if (fProfilerRequiresGenericsContextForEnterLeave)
        {
            methInfo->options = CorInfoOptions(methInfo->options|CORINFO_GENERICS_CTXT_KEEP_ALIVE);
        }
        else
#endif // defined(PROFILING_SUPPORTED)
        {
            // Check all the exception clauses

            if (ftn->IsDynamicMethod())
            {
                // @TODO: how do we detect the need to mark this flag?
            }
            else
            {
                COR_ILMETHOD_SECT_EH_CLAUSE_FAT ehClause;

                for (unsigned i = 0; i < methInfo->EHcount; i++)
                {
                    const COR_ILMETHOD_SECT_EH_CLAUSE_FAT* ehInfo =
                            (COR_ILMETHOD_SECT_EH_CLAUSE_FAT*)header->EH->EHClause(i, &ehClause);

                    // Is it a typed catch clause?
                    if (ehInfo->GetFlags() != COR_ILEXCEPTION_CLAUSE_NONE)
                        continue;

                    // Check if we catch "C<T>" ?

                    DWORD catchTypeToken = ehInfo->GetClassToken();
                    if (TypeFromToken(catchTypeToken) != mdtTypeSpec)
                        continue;

                    PCCOR_SIGNATURE pSig;
                    ULONG cSig;
                    IfFailThrow(ftn->GetMDImport()->GetTypeSpecFromToken(catchTypeToken, &pSig, &cSig));
                    
                    SigPointer psig(pSig, cSig);

                    SigTypeContext sigTypeContext(ftn);
                    if (psig.IsPolyType(&sigTypeContext) & hasSharableVarsMask)
                    {
                        methInfo->options = CorInfoOptions(methInfo->options|CORINFO_GENERICS_CTXT_KEEP_ALIVE);
                        break;
                    }
                }
            }
        }
    }

    PCCOR_SIGNATURE pSig = NULL;
    DWORD           cbSig = 0;
    ftn->GetSig(&pSig, &cbSig);
    
    /* Fetch the method signature */
    // Type parameters in the signature should be instantiated according to the
    // class/method/array instantiation of ftnHnd
    CEEInfo::ConvToJitSig(
        pSig, 
        cbSig, 
        GetScopeHandle(ftn), 
        mdTokenNil, 
        &methInfo->args, 
        ftn, 
        false);

    // Shared generic or static per-inst methods and shared methods on generic structs
    // take an extra argument representing their instantiation
    if (ftn->RequiresInstArg())
        methInfo->args.callConv = (CorInfoCallConv)(methInfo->args.callConv | CORINFO_CALLCONV_PARAMTYPE);

    _ASSERTE((IsMdStatic(ftn->GetAttrs()) == 0) == ((methInfo->args.callConv & CORINFO_CALLCONV_HASTHIS) != 0));

    /* And its local variables */
    // Type parameters in the signature should be instantiated according to the
    // class/method/array instantiation of ftnHnd
    CEEInfo::ConvToJitSig(
        pLocalSig, 
        cbLocalSig, 
        GetScopeHandle(ftn), 
        mdTokenNil, 
        &methInfo->locals, 
        ftn, 
        true);
} // getMethodInfoHelper

//---------------------------------------------------------------------------------------
// 
bool 
CEEInfo::getMethodInfo(
    CORINFO_METHOD_HANDLE ftnHnd, 
    CORINFO_METHOD_INFO * methInfo)
{
    CONTRACTL {
        SO_TOLERANT;
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    } CONTRACTL_END;

    bool result = false;

    JIT_TO_EE_TRANSITION();

    MethodDesc * ftn = GetMethod(ftnHnd);

    if (!ftn->IsDynamicMethod() && (!ftn->IsIL() || !ftn->GetRVA() || ftn->IsWrapperStub()))
    {
    /* Return false if not IL or has no code */
        result = false;
    }
    else
    {
        /* Get the IL header */
        /* <REVISIT_TODO>TODO: canInline already did validation, however, we do it again
           here because NGEN uses this function without calling canInline
           It would be nice to avoid this redundancy </REVISIT_TODO>*/
        Module* pModule = ftn->GetModule();

        bool    verify = !Security::CanSkipVerification(ftn);

        if (ftn->IsDynamicMethod())
        {
            getMethodInfoHelper(ftn, ftnHnd, NULL, methInfo);
        }
        else
        {
            COR_ILMETHOD_DECODER::DecoderStatus status = COR_ILMETHOD_DECODER::SUCCESS;
            COR_ILMETHOD_DECODER header(ftn->GetILHeader(TRUE), ftn->GetMDImport(), verify ? &status : NULL);

            // If we get a verification error then we try to demand SkipVerification for the module
            if (status == COR_ILMETHOD_DECODER::VERIFICATION_ERROR &&
                Security::CanSkipVerification(pModule->GetDomainAssembly()))
            {
                status = COR_ILMETHOD_DECODER::SUCCESS;
            }

            if (status != COR_ILMETHOD_DECODER::SUCCESS)
            {
                if (status == COR_ILMETHOD_DECODER::VERIFICATION_ERROR)
                {
                    // Throw a verification HR
                    COMPlusThrowHR(COR_E_VERIFICATION);
                }
                else
                {
                    COMPlusThrowHR(COR_E_BADIMAGEFORMAT, BFA_BAD_IL);
                }
            }

            getMethodInfoHelper(ftn, ftnHnd, &header, methInfo);
        }

        LOG((LF_JIT, LL_INFO100000, "Getting method info (possible inline) %s::%s%s\n",
            ftn->m_pszDebugClassName, ftn->m_pszDebugMethodName, ftn->m_pszDebugMethodSignature));

        result = true;
    }

    EE_TO_JIT_TRANSITION();

    return result;
}

#ifdef _DEBUG

/************************************************************************
    Return true when ftn contains a local of type CLASS__STACKCRAWMARK
*/

bool containsStackCrawlMarkLocal(MethodDesc* ftn)
{
    STANDARD_VM_CONTRACT;
    
    COR_ILMETHOD* ilHeader = ftn->GetILHeader();
    _ASSERTE(ilHeader);

    COR_ILMETHOD_DECODER header(ilHeader, ftn->GetMDImport(), NULL);

    if (header.LocalVarSig == NULL)
        return NULL;

    SigPointer ptr(header.LocalVarSig, header.cbLocalVarSig);

    IfFailThrow(ptr.GetData(NULL)); // IMAGE_CEE_CS_CALLCONV_LOCAL_SIG

    ULONG numLocals;
    IfFailThrow(ptr.GetData(&numLocals));

    for(ULONG i = 0; i < numLocals; i++)
    {
        CorElementType eType;
        IfFailThrow(ptr.PeekElemType(&eType));
        if (eType != ELEMENT_TYPE_VALUETYPE)
        {
            IfFailThrow(ptr.SkipExactlyOne());
            continue;
        }

        IfFailThrow(ptr.GetElemType(NULL));

        mdToken token;
        IfFailThrow(ptr.GetToken(&token));

        // We are inside mscorlib - simple token match is sufficient
        if (token == MscorlibBinder::GetClass(CLASS__STACKCRAWMARK)->GetCl())
            return TRUE;
    }

    return FALSE;
}

#endif

/*************************************************************
 * Check if the caller and calle are in the same assembly
 * i.e. do not inline across assemblies
 *************************************************************/

CorInfoInline CEEInfo::canInline (CORINFO_METHOD_HANDLE hCaller,
                                  CORINFO_METHOD_HANDLE hCallee,
                                  DWORD*                pRestrictions)
{
    CONTRACTL {
        SO_TOLERANT;
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    } CONTRACTL_END;

    CorInfoInline result = INLINE_PASS;  // By default we pass.  
                                         // Do not set pass in the rest of the method.
    DWORD         dwRestrictions = 0;    // By default, no restrictions
    const char *  szFailReason = NULL;   // for reportInlineDecision

    JIT_TO_EE_TRANSITION();

    // This does not work in the multi-threaded case
#if 0
    // Caller should check this condition first
    _ASSERTE(!(CORINFO_FLG_DONT_INLINE & getMethodAttribsInternal(hCallee)));
#endif

    // Returns TRUE: if caller and callee are from the same assembly or the callee
    //               is part of the system assembly.
    //
    // If the caller and callee have the same Critical state and the same Grant (and refuse) sets, then the
    // callee may always be inlined into the caller.
    //
    // If they differ, then the callee is marked as INLINE_RESPECT_BOUNDARY.  The Jit may only inline the
    // callee when any of the following are true.
    //  1) the callee is a leaf method.
    //  2) the callee does not call any Boundary Methods.
    //
    // Conceptually, a Boundary method is a method that needs to accurately find the permissions of its
    // caller.  Boundary methods are:
    //
    //  1) A method that calls anything that creates a StackCrawlMark to look for its caller.  In this code
    //      this is approximated as "in mscorlib and is marked as NoInlining".
    //  2) A method that calls a method which calls Demand.  These methods must be marked as
    //      IsMdRequireSecObject.
    //  3) Calls anything that is virtual.  This is because the virtual method could be #1 or #2.
    //
    // In CoreCLR, all public Critical methods of mscorlib are considered Boundary Method

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

    if (GetDebuggerCompileFlags(pCallee->GetModule(), 0) & CORJIT_FLG_DEBUG_CODE)
    {
        result = INLINE_NEVER;
        szFailReason = "Inlinee is debuggable";
        goto exit;
    }
#endif

    // The orginal caller is the current method
    MethodDesc *  pOrigCaller;
    pOrigCaller = m_pMethodBeingCompiled;
    Module *      pOrigCallerModule;
    pOrigCallerModule = pOrigCaller->GetLoaderModule();

    // Prevent recursive compiling/inlining/verifying
    if (pOrigCaller != pCallee)
    {
        //  The Inliner may not do code verification.
        //  So never inline anything that is unverifiable / bad code.
        if (!Security::CanSkipVerification(pCallee))
        {
            // Inlinee needs to be verifiable
            if (!pCallee->IsVerifiable())
            {
                result = INLINE_NEVER;
                szFailReason = "Inlinee is not verifiable";
                goto exit;
            }
        }
    }

    // We check this here as the call to MethodDesc::IsVerifiable()
    // may set CORINFO_FLG_DONT_INLINE.
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
        szFailReason = "Inlinee requires a security object (calls Demand/Assert/Deny)";
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

    //
    // Perform the Cross-Assembly inlining checks
    // 
    {
        Module *    pCalleeModule   = pCallee->GetModule();

#ifdef FEATURE_PREJIT
        Assembly *  pCalleeAssembly = pCalleeModule->GetAssembly();

#ifdef _DEBUG
        //
        // Make sure that all methods with StackCrawlMark are marked as non-inlineable
        //
        if (pCalleeAssembly->IsSystem())
        {
            _ASSERTE(!containsStackCrawlMarkLocal(pCallee));
        }
#endif

        // To allow for servicing of Ngen images we want to disable most 
        // Cross-Assembly inlining except for the cases that we explicitly allow.
        // 
        if (IsCompilingForNGen())
        {
            // This is an canInline call at Ngen time 
            //
            //
            Assembly *  pOrigCallerAssembly = pOrigCallerModule->GetAssembly();

            if (pCalleeAssembly == pOrigCallerAssembly)
            {
                // Within the same assembly
                // we can freely inline with no restrictions
            }
            else
            {
#ifdef FEATURE_READYTORUN_COMPILER
                // No inlinining for version resilient code except if in the same version bubble
                // If this condition changes, please make the corresponding change
                // in getCallInfo, too.
                if (IsReadyToRunCompilation() &&
                    !isVerifyOnly() &&
                    !IsInSameVersionBubble(pCaller, pCallee)
                   )
                {
                    result = INLINE_NEVER;
                    szFailReason = "Cross-module inlining in version resilient code";
                    goto exit;
                }
#endif
            }
        }
#endif  // FEATURE_PREJIT

        if (!canReplaceMethodOnStack(pCallee, NULL, pCaller))
        {
            dwRestrictions |= INLINE_RESPECT_BOUNDARY;
        }

        // TODO: We can probably be smarter here if the caller is jitted, as we will
        // know for sure if the inlinee has really no string interning active (currently
        // it's only on in the ngen case (besides requiring the attribute)), but this is getting
        // too subtle. Will only do if somebody screams about it, as bugs here are going to
        // be tough to find
        if ((pOrigCallerModule != pCalleeModule) &&  pCalleeModule->IsNoStringInterning())
        {
            dwRestrictions |= INLINE_NO_CALLEE_LDSTR;
        }

        // The remoting interception can be skipped only if the call is on same this pointer
        if (pCallee->MayBeRemotingIntercepted())
        {
            dwRestrictions |= INLINE_SAME_THIS;
        }
    }

#ifdef PROFILING_SUPPORTED
    if (CORProfilerPresent())
    {
        // #rejit
        // 
        // See if rejit-specific flags for the caller disable inlining
        if ((ReJitManager::GetCurrentReJitFlags(pCaller) &
            COR_PRF_CODEGEN_DISABLE_INLINING) != 0)
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

        // If the profiler wishes to be notified of JIT events and the result from
        // the above tests will cause a function to be inlined, we need to tell the
        // profiler that this inlining is going to take place, and give them a
        // chance to prevent it.
        {
            BEGIN_PIN_PROFILER(CORProfilerTrackJITInfo());
            if (pCaller->IsILStub() || pCallee->IsILStub())
            {
                // do nothing
            }
            else
            {
                BOOL fShouldInline;

                HRESULT hr = g_profControlBlock.pProfInterface->JITInlining(
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
            END_PIN_PROFILER();
        }
    }
#endif // PROFILING_SUPPORTED


#ifdef PROFILING_SUPPORTED
    if (CORProfilerPresent())
    {
        // #rejit
        // 
        // See if rejit-specific flags for the caller disable inlining
        if ((ReJitManager::GetCurrentReJitFlags(pCaller) &
            COR_PRF_CODEGEN_DISABLE_INLINING) != 0)
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

        // If the profiler wishes to be notified of JIT events and the result from
        // the above tests will cause a function to be inlined, we need to tell the
        // profiler that this inlining is going to take place, and give them a
        // chance to prevent it.
        {
            BEGIN_PIN_PROFILER(CORProfilerTrackJITInfo());
            if (pCaller->IsILStub() || pCallee->IsILStub())
            {
                // do nothing
            }
            else
            {
                BOOL fShouldInline;

                HRESULT hr = g_profControlBlock.pProfInterface->JITInlining(
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
            END_PIN_PROFILER();
        }
    }
#endif // PROFILING_SUPPORTED

exit: ;

    EE_TO_JIT_TRANSITION();

    if (result == INLINE_PASS && dwRestrictions)
    {
        if (pRestrictions)
        {
            *pRestrictions = dwRestrictions;
        }
        else
        {
            // If the jitter didn't want to know about restrictions, it shouldn't be inlining
            result = INLINE_FAIL;
            szFailReason = "Inlinee has restrictions the JIT doesn't want";
        }
    }
    else
    {
        if (pRestrictions)
        {
            // Denied inlining, makes no sense to pass out restrictions,
            *pRestrictions = 0;
        }
    }

    if (dontInline(result))
    {
        // If you hit this assert, it means you added a new way to prevent inlining
        // without documenting it for ETW!
        _ASSERTE(szFailReason != NULL);
        reportInliningDecision(hCaller, hCallee, result, szFailReason);
    }

    return result;
}

void CEEInfo::reportInliningDecision (CORINFO_METHOD_HANDLE inlinerHnd,
                                      CORINFO_METHOD_HANDLE inlineeHnd,
                                      CorInfoInline inlineResult,
                                      const char * reason)
{
    STATIC_CONTRACT_THROWS;
    STATIC_CONTRACT_GC_TRIGGERS;
    STATIC_CONTRACT_SO_TOLERANT;

    JIT_TO_EE_TRANSITION();

#ifdef _DEBUG
    if (LoggingOn(LF_JIT, LL_INFO100000))
    {
        SString currentMethodName;
        currentMethodName.AppendUTF8(m_pMethodBeingCompiled->GetModule_NoLogging()->GetFile()->GetSimpleName());
        currentMethodName.Append(L'/');
        TypeString::AppendMethodInternal(currentMethodName, m_pMethodBeingCompiled, TypeString::FormatBasic);

        SString inlineeMethodName;
        if (GetMethod(inlineeHnd))
        {
            inlineeMethodName.AppendUTF8(GetMethod(inlineeHnd)->GetModule_NoLogging()->GetFile()->GetSimpleName());
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
            inlinerMethodName.AppendUTF8(GetMethod(inlinerHnd)->GetModule_NoLogging()->GetFile()->GetSimpleName());
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
                 "While compiling '%S', inline of '%S' into '%S' failed because: '%s'.\n",
                 currentMethodName.GetUnicode(), inlineeMethodName.GetUnicode(),
                 inlinerMethodName.GetUnicode(), reason));
        }
        else
        {
            LOG((LF_JIT, LL_INFO100000, "While compiling '%S', inline of '%S' into '%S' succeeded.\n",
                 currentMethodName.GetUnicode(), inlineeMethodName.GetUnicode(),
                 inlinerMethodName.GetUnicode()));

        }
    }
#endif //_DEBUG

    //I'm gonna duplicate this code because the format is slightly different.  And LoggingOn is debug only.
    if (ETW_TRACING_CATEGORY_ENABLED(MICROSOFT_WINDOWS_DOTNETRUNTIME_PROVIDER_Context, 
                                     TRACE_LEVEL_VERBOSE,
                                     CLR_JITTRACING_KEYWORD))
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
                                           str,
                                           GetClrInstanceId());
        }
        else
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

    EE_TO_JIT_TRANSITION();
}


/*************************************************************
This loads the (formal) declared constraints on the class and method type parameters, 
and detects (but does not itself reject) circularities among the class type parameters 
and (separately) method type parameters. 

It must be called whenever we verify a typical method, ie any method (generic or
nongeneric) in a typical class. It must be called for non-generic methods too, 
because their bodies may still mention class type parameters which will need to
have their formal constraints loaded in order to perform type compatibility tests.

We have to rule out cycles like "C<U,T> where T:U, U:T" only to avoid looping 
in the verifier (ie the T.CanCast(A) would loop calling U.CanCast(A) then 
T.CanCastTo(A) etc.). Since the JIT only tries to walk the hierarchy from a type
a parameter when verifying, it should be safe to JIT unverified, but trusted, 
instantiations even in the presence of cycle constraints.
@TODO: It should be possible (and easy) to detect cycles much earlier on by
directly inspecting the metadata. All you have to do is check that, for each
of the n type parameters to a class or method there is no path of length n 
obtained by following naked type parameter constraints of the same kind. 
This can be detected by looking directly at metadata, without actually loading
the typehandles for the naked type parameters.
 *************************************************************/

void CEEInfo::initConstraintsForVerification(CORINFO_METHOD_HANDLE hMethod,
                                             BOOL *pfHasCircularClassConstraints,
                                             BOOL *pfHasCircularMethodConstraints)
{
    CONTRACTL {
        SO_TOLERANT;
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        PRECONDITION(CheckPointer(pfHasCircularClassConstraints));
        PRECONDITION(CheckPointer(pfHasCircularMethodConstraints));
    } CONTRACTL_END;

    *pfHasCircularClassConstraints  = FALSE;
    *pfHasCircularMethodConstraints = FALSE;

    JIT_TO_EE_TRANSITION();

    MethodDesc* pMethod = GetMethod(hMethod);
    if (pMethod->IsTypicalMethodDefinition())
    {
        // Force a load of the constraints on the type parameters, detecting cyclic bounds
        pMethod->LoadConstraintsForTypicalMethodDefinition(pfHasCircularClassConstraints,pfHasCircularMethodConstraints);
    }

    EE_TO_JIT_TRANSITION();
}

/*************************************************************
 * Check if a method to be compiled is an instantiation
 * of generic code that has already been verified.
 * Three possible return values (see corinfo.h)
 *************************************************************/

CorInfoInstantiationVerification  
    CEEInfo::isInstantiationOfVerifiedGeneric(CORINFO_METHOD_HANDLE hMethod)
{
    CONTRACTL {
        SO_TOLERANT;
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    } CONTRACTL_END;

    CorInfoInstantiationVerification result = INSTVER_NOT_INSTANTIATION;

    JIT_TO_EE_TRANSITION();

    MethodDesc * pMethod = GetMethod(hMethod);

    if (!(pMethod->HasClassOrMethodInstantiation()))
    {
        result = INSTVER_NOT_INSTANTIATION;
        goto exit;
    }

    if (pMethod->IsTypicalMethodDefinition())
    {
        result = INSTVER_NOT_INSTANTIATION;
        goto exit;
    }

    result = pMethod->IsVerifiable() ? INSTVER_GENERIC_PASSED_VERIFICATION
                                     : INSTVER_GENERIC_FAILED_VERIFICATION;

 exit: ;

    EE_TO_JIT_TRANSITION();

    return result;
}

// This function returns true if we can replace pReplaced on the stack with
// pReplacer.  In the case of inlining this means that pReplaced is the inlinee
// and pReplacer is the inliner.  In the case of tail calling, pReplacer is the
// tail callee and pReplaced is the tail caller.
//
// It's possible for pReplacer to be NULL.  This means that it's an unresolved
// callvirt for a tail call.  This is legal, but we make the static decision
// based on pReplaced only (assuming that pReplacer is from a different
// assembly that is a different partial trust).
//
// The general logic is this:
//   1) You can replace anything that is full trust (since full trust doesn't
//      cause a demand to fail).
//   2) You can coalesce all stack frames that have the same permission set
//      down to a single stack frame.
//
// You'll see three patterns in the code below:
//   1) There is only one permission set per assembly
//   2) Comparing grant sets is prohibitively expensive.  Therefore we use the
//      the fact that, "a homogenous app domain has only one partial trust
//      permission set" to infer that two grant sets are equal.
//   3) Refuse sets are rarely used and too complex to handle correctly, so
//      they generally just torpedo all of the logic in here.
//
BOOL canReplaceMethodOnStack(MethodDesc* pReplaced, MethodDesc* pDeclaredReplacer, MethodDesc* pExactReplacer)
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE; //Called from PREEMPTIVE functions
    } CONTRACTL_END;

    OBJECTREF refused = NULL;
    Assembly * pReplacedAssembly = pReplaced->GetAssembly();

    _ASSERTE(Security::IsResolved(pReplacedAssembly));

    // The goal of this code is to ensure that we never allow a unique non-full
    // trust grant set to be eliminated from the stack.

    Assembly * pReplacerAssembly = NULL;
    if (pExactReplacer != NULL)
    {
        pReplacerAssembly = pExactReplacer->GetAssembly();
        _ASSERTE(Security::IsResolved(pReplacerAssembly));

        // If two methods are from the same assembly, they must have the same grant set.
        if (pReplacerAssembly == pReplacedAssembly)
        {
            // When both methods are in the same assembly, then it is always safe to
            // coalesce them for the purposes of security.
            return TRUE;
        }
    }

    if ( pDeclaredReplacer != NULL &&
         pReplacedAssembly->GetDomainAssembly() == GetAppDomain()->GetAnonymouslyHostedDynamicMethodsAssembly() &&
         SystemDomain::IsReflectionInvocationMethod(pDeclaredReplacer) )
    {
        // When an anonymously hosted dynamic method invokes a method through reflection invocation,
        // the dynamic method is the true caller. If we replace it on the stack we would be doing 
        // security check against its caller rather than the dynamic method itself. 
        // We should do this check against pDeclaredReplacer rather than pExactReplacer because the
        // latter is NULL is the former if virtual, e.g. MethodInfo.Invoke(...).
        return FALSE;
    }

    // It is always safe to remove a full trust stack frame from the stack.
    IAssemblySecurityDescriptor * pReplacedDesc = pReplacedAssembly->GetSecurityDescriptor();

#ifdef FEATURE_APTCA
    if (GetAppDomain()->IsCompilationDomain())
    {
        // If we're NGENing assemblies, we don't want to inline code out of a conditionally APTCA assembly,
        // since we need to ensure that the dependency is loaded and checked to ensure that it is condtional
        // APTCA.  We only need to do this if the replaced caller is transparent, since a critical caller
        // will be allowed to use the conditional APTCA disabled assembly anyway.
        if (pReplacedAssembly != pReplacerAssembly && Security::IsMethodTransparent(pReplaced))
        {
            ModuleSecurityDescriptor *pReplacedMSD = ModuleSecurityDescriptor::GetModuleSecurityDescriptor(pReplacedAssembly);
            if (pReplacedMSD->GetTokenFlags() & TokenSecurityDescriptorFlags_ConditionalAPTCA)
            {
                return FALSE;
            }
        }
    }
#endif // FEATURE_APTCA

    if (pReplacedDesc->IsFullyTrusted())
    {
        GCX_COOP(); // Required for GetGrantedPermissionSet
        (void)pReplacedDesc->GetGrantedPermissionSet(&refused);
        if (refused != NULL)
        {
            // This is full trust with a Refused set.  That means that it is partial
            // trust.  However, even in a homogeneous app domain, it could be a
            // different partial trust from any other partial trust, and since
            // pExactReplacer is either unknown or from a different assembly, we assume
            // the worst: that is is a different partial trust.
            return FALSE;
        }
        return TRUE;
    }

    // pReplaced is partial trust and pExactReplacer is either unknown or from a
    // different assembly than pReplaced.

    if (pExactReplacer == NULL)
    {
        // This is the unresolved callvirt case.  Since we're partial trust,
        // we can't tail call.
        return FALSE;
    }

    // We're replacing a partial trust stack frame.  We can only do this with a
    // matching grant set. We know pReplaced is partial trust.  Make sure both
    // pExactReplacer and pReplaced are the same partial trust.
    IAssemblySecurityDescriptor * pReplacerDesc = pReplacerAssembly->GetSecurityDescriptor();
    if (pReplacerDesc->IsFullyTrusted())
    {
        return FALSE; // Replacing partial trust with full trust.
    }

    // At this point both pExactReplacer and pReplaced are partial trust.  We can
    // only do this if the grant sets are equal.  Since comparing grant sets
    // requires calling up into managed code, we will infer that the two grant
    // sets are equal if the domain is homogeneous.
    IApplicationSecurityDescriptor * adSec = GetAppDomain()->GetSecurityDescriptor();
    if (adSec->IsHomogeneous())
    {
        // We're homogeneous, but the two descriptors could have refused sets.
        // Bail if they do.
        GCX_COOP(); // Required for GetGrantedPermissionSet
        (void)pReplacedDesc->GetGrantedPermissionSet(&refused);
        if (refused != NULL)
        {
            return FALSE;
        }

        (void)pReplacerDesc->GetGrantedPermissionSet(&refused);
        if (refused != NULL)
            return FALSE;

        return TRUE;
    }

    // pExactReplacer and pReplaced are from 2 different assemblies.  Both are partial
    // trust, and the app domain is not homogeneous, so we just have to
    // assume that they have different grant or refuse sets, and thus cannot
    // safely be replaced.
    return FALSE;
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
        SO_TOLERANT;
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

    // If the caller is the static constructor (.cctor) of a class which has a ComImport base class
    // somewhere up the class hierarchy, then we cannot make the call into a tailcall.  See
    // RegisterObjectCreationCallback() in ExtensibleClassFactory.cpp for more information.
    if (pCaller->IsClassConstructor() &&
        pCaller->GetMethodTable()->IsComObjectType())
    {
        result = false;
        szFailReason = "Caller is  ComImport .cctor";
        goto exit;
    }

    // TailCalls will throw off security stackwalking logic when there is a declarative Assert
    // Note that this check will also include declarative demands.  It's OK to do a tailcall in
    // those cases, but we currently don't have a way to check only for declarative Asserts.
    if (pCaller->IsInterceptedForDeclSecurity())
    {
        result = false;
        szFailReason = "Caller has declarative security";
        goto exit;
    }

    // The jit already checks and doesn't allow the tail caller to use imperative security.
    _ASSERTE(pCaller->IsRuntimeSupplied() || !IsMdRequireSecObject(pCaller->GetAttrs()));

    if (!canReplaceMethodOnStack(pCaller, pDeclaredCallee, pExactCallee))
    {
        result = false;
        szFailReason = "Different security";
        goto exit;
    }

    if (!fIsTailPrefix)        
    {
        mdMethodDef callerToken = pCaller->GetMemberDef();

        // We don't want to tailcall the entrypoint for an application; JIT64 will sometimes
        // do this for simple entrypoints and it results in a rather confusing debugging 
        // experience.
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
        // a better way of identifying them, we look for methods marked as NoInlining
        // inside mscorlib (StackCrawlMark is private), and assume it is one of these
        // methods.  We have an assert in canInline that ensures all StackCrawlMark
        // methods are appropriately marked.
        //
        // NOTE that this is *NOT* a security issue because we check to ensure that
        // the callee has the *SAME* security properties as the caller, it just might
        // be from a different assembly which messes up APIs like Type.GetType, which
        // for back-compat uses the assembly of it's caller to resolve unqualified
        // typenames.
        if ((pExactCallee != NULL) && pExactCallee->GetModule()->IsSystem() && pExactCallee->IsIL())
        {
            if (IsMiNoInlining(pExactCallee->GetImplAttrs()))
            {
                result = false;
                szFailReason = "Callee might have a StackCrawlMark.LookForMyCaller";
                goto exit;
            }
        }
    }

    // We cannot tail call from a root CER method, the thread abort algorithm to
    // detect CERs depends on seeing such methods on the stack.
    if (IsCerRootMethod(pCaller))
    {
        result = false;
        szFailReason = "Caller is a CER root";
        goto exit;
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
    STATIC_CONTRACT_SO_TOLERANT;

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
                 "While compiling '%S', %Splicit tail call from '%S' to '%S' failed because: '%s'.\n",
                 currentMethodName.GetUnicode(), fIsTailPrefix ? W("ex") : W("im"),
                 callerMethodName.GetUnicode(), calleeMethodName.GetUnicode(), reason));
        }
        else
        {
            static const char * const tailCallType[] = {
                "optimized tail call", "recursive loop", "helper assisted tailcall"
            };
            _ASSERTE(tailCallResult >= 0 && (size_t)tailCallResult < sizeof(tailCallType) / sizeof(tailCallType[0]));
            LOG((LF_JIT, LL_INFO100000,
                 "While compiling '%S', %Splicit tail call from '%S' to '%S' generated as a %s.\n",
                 currentMethodName.GetUnicode(), fIsTailPrefix ? W("ex") : W("im"),
                 callerMethodName.GetUnicode(), calleeMethodName.GetUnicode(), tailCallType[tailCallResult]));

        }
    }
#endif //_DEBUG

    // I'm gonna duplicate this code because the format is slightly different.  And LoggingOn is debug only.
    if (ETW_TRACING_CATEGORY_ENABLED(MICROSOFT_WINDOWS_DOTNETRUNTIME_PROVIDER_Context, 
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
                                           str,
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

void CEEInfo::getEHinfoHelper(
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
        SO_TOLERANT;
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
        SO_TOLERANT;
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    } CONTRACTL_END;

    JIT_TO_EE_TRANSITION();

    getMethodSigInternal(ftnHnd, sigRet, owner);

    EE_TO_JIT_TRANSITION();
}

//---------------------------------------------------------------------------------------
// 
void 
CEEInfo::getMethodSigInternal(
    CORINFO_METHOD_HANDLE ftnHnd, 
    CORINFO_SIG_INFO *    sigRet, 
    CORINFO_CLASS_HANDLE  owner)
{
    STANDARD_VM_CONTRACT;

    MethodDesc * ftn = GetMethod(ftnHnd);
    
    PCCOR_SIGNATURE pSig = NULL;
    DWORD           cbSig = 0;
    ftn->GetSig(&pSig, &cbSig);

    // Type parameters in the signature are instantiated
    // according to the class/method/array instantiation of ftnHnd and owner
    CEEInfo::ConvToJitSig(
        pSig, 
        cbSig, 
        GetScopeHandle(ftn), 
        mdTokenNil, 
        sigRet, 
        ftn, 
        false, 
        (TypeHandle)owner);

    //@GENERICS:
    // Shared generic methods and shared methods on generic structs take an extra argument representing their instantiation
    if (ftn->RequiresInstArg())
    {
        sigRet->callConv = (CorInfoCallConv) (sigRet->callConv | CORINFO_CALLCONV_PARAMTYPE);
    }

    // We want the calling convention bit to be consistant with the method attribute bit
    _ASSERTE( (IsMdStatic(ftn->GetAttrs()) == 0) == ((sigRet->callConv & CORINFO_CALLCONV_HASTHIS) != 0) );
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
        SO_TOLERANT;
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

/***********************************************************************/
CORINFO_MODULE_HANDLE CEEInfo::getMethodModule (CORINFO_METHOD_HANDLE methodHnd)
{
    CONTRACTL {
        SO_TOLERANT;
        NOTHROW;
        GC_NOTRIGGER;
        MODE_PREEMPTIVE;
    } CONTRACTL_END;

    CORINFO_MODULE_HANDLE result = NULL;

    JIT_TO_EE_TRANSITION_LEAF();

    MethodDesc* method = GetMethod(methodHnd);

    if (method->IsDynamicMethod())
    {
        // this should never be called, thus the assert, I don't know if the (non existent) caller
        // expects the Module or the scope
        UNREACHABLE();
    }
    else
    {
        result = (CORINFO_MODULE_HANDLE) method->GetModule();
    }
    
    EE_TO_JIT_TRANSITION_LEAF();

    return result;
}

/*********************************************************************/
CorInfoIntrinsics CEEInfo::getIntrinsicID(CORINFO_METHOD_HANDLE methodHnd,
                                          bool * pMustExpand)
{
    CONTRACTL {
        SO_TOLERANT;
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    } CONTRACTL_END;

    CorInfoIntrinsics result = CORINFO_INTRINSIC_Illegal;

    JIT_TO_EE_TRANSITION();

    if (pMustExpand != NULL)
    {
        *pMustExpand = false;
    }

    MethodDesc* method = GetMethod(methodHnd);

    if (method->IsArray())
    {
        ArrayMethodDesc * arrMethod = (ArrayMethodDesc *)method;
        result = arrMethod->GetIntrinsicID();
    }
    else
    if (method->IsFCall())
    {
        result = ECall::GetIntrinsicID(method);
    }

    EE_TO_JIT_TRANSITION();

    return result;
}

/*********************************************************************/
bool CEEInfo::isInSIMDModule(CORINFO_CLASS_HANDLE classHnd)
{
CONTRACTL {
        SO_TOLERANT;
        NOTHROW;
        GC_NOTRIGGER;
        MODE_PREEMPTIVE;
    } CONTRACTL_END;

    bool result = false;
    JIT_TO_EE_TRANSITION_LEAF();

    TypeHandle VMClsHnd(classHnd);
    if (VMClsHnd.GetMethodTable()->GetAssembly()->IsSIMDVectorAssembly())
    {
        result = true;
    }
    EE_TO_JIT_TRANSITION_LEAF();

    return result;
}

/*********************************************************************/
void CEEInfo::getMethodVTableOffset (CORINFO_METHOD_HANDLE methodHnd,
                                     unsigned * pOffsetOfIndirection,
                                     unsigned * pOffsetAfterIndirection)
{
    CONTRACTL {
        SO_TOLERANT;
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

    *pOffsetOfIndirection = MethodTable::GetVtableOffset() + MethodTable::GetIndexOfVtableIndirection(method->GetSlot()) * sizeof(PTR_PCODE);
    *pOffsetAfterIndirection = MethodTable::GetIndexAfterVtableIndirection(method->GetSlot()) * sizeof(PCODE);

    EE_TO_JIT_TRANSITION_LEAF();
}

/*********************************************************************/
void CEEInfo::getFunctionEntryPoint(CORINFO_METHOD_HANDLE  ftnHnd,
                                    CORINFO_CONST_LOOKUP * pResult,
                                    CORINFO_ACCESS_FLAGS   accessFlags)
{
    CONTRACTL {
        SO_TOLERANT;
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    } CONTRACTL_END;

    void* ret = NULL;
    InfoAccessType accessType = IAT_VALUE;

    JIT_TO_EE_TRANSITION();

    MethodDesc * ftn = GetMethod(ftnHnd);

    // Resolve methodImpl.
    ftn = ftn->GetMethodTable()->MapMethodDeclToMethodImpl(ftn);

    ret = (void *)ftn->TryGetMultiCallableAddrOfCode(accessFlags);

    // TryGetMultiCallableAddrOfCode returns NULL if indirect access is desired
    if (ret == NULL)
    {
        // should never get here for EnC methods or if interception via remoting stub is required
        _ASSERTE(!ftn->IsEnCMethod());

        _ASSERTE((accessFlags & CORINFO_ACCESS_THIS) || !ftn->IsRemotingInterceptedViaVirtualDispatch());

        ret = ftn->GetAddrOfSlot();
        accessType = IAT_PVALUE;
    }

    EE_TO_JIT_TRANSITION();

    _ASSERTE(ret != NULL);

    pResult->accessType = accessType;
    pResult->addr = ret;
}

/*********************************************************************/
void CEEInfo::getFunctionFixedEntryPoint(CORINFO_METHOD_HANDLE   ftn,
                                         CORINFO_CONST_LOOKUP *  pResult)
{
    CONTRACTL {
        SO_TOLERANT;
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    } CONTRACTL_END;

    JIT_TO_EE_TRANSITION();

    MethodDesc * pMD = GetMethod(ftn);

    pResult->accessType = IAT_VALUE;


#ifndef CROSSGEN_COMPILE
    // If LDFTN target has [NativeCallable] attribute , then create a UMEntryThunk.
    if (pMD->HasNativeCallableAttribute())
    {
        pResult->addr = (void*)COMDelegate::ConvertToCallback(pMD);
    }
    else
#endif //CROSSGEN_COMPILE
    {
        pResult->addr = (void *)pMD->GetMultiCallableAddrOfCode();
    }
    EE_TO_JIT_TRANSITION();
}

/*********************************************************************/
const char* CEEInfo::getFieldName (CORINFO_FIELD_HANDLE fieldHnd, const char** scopeName)
{
    CONTRACTL {
        SO_TOLERANT;
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    } CONTRACTL_END;

    const char* result = NULL;

    JIT_TO_EE_TRANSITION();

    FieldDesc* field = (FieldDesc*) fieldHnd;
    if (scopeName != 0)
    {
        TypeHandle t = TypeHandle(field->GetApproxEnclosingMethodTable());
        *scopeName = "";
        if (!t.IsNull())
        {
#ifdef _DEBUG
            t.GetName(ssClsNameBuff);
            *scopeName = ssClsNameBuff.GetUTF8(ssClsNameBuffScratch);
#else // !_DEBUG
            // since this is for diagnostic purposes only,
            // give up on the namespace, as we don't have a buffer to concat it
            // also note this won't show array class names.
            LPCUTF8 nameSpace;
            *scopeName= t.GetMethodTable()->GetFullyQualifiedNameInfo(&nameSpace);
#endif // !_DEBUG
        }
    }

    result = field->GetName();

    EE_TO_JIT_TRANSITION();

    return result;
}

/*********************************************************************/
// Get the type that declares the field
CORINFO_CLASS_HANDLE CEEInfo::getFieldClass (CORINFO_FIELD_HANDLE fieldHnd)
{
    CONTRACTL {
        SO_TOLERANT;
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
// Returns the basic type of the field (not the the type that declares the field)
//
// pTypeHnd - On return, for reference and value types, *pTypeHnd will contain 
//            the normalized type of the field.
// owner - Optional. For resolving in a generic context

CorInfoType CEEInfo::getFieldType (CORINFO_FIELD_HANDLE fieldHnd, 
                                   CORINFO_CLASS_HANDLE* pTypeHnd, 
                                   CORINFO_CLASS_HANDLE owner)
{
    CONTRACTL {
        SO_TOLERANT;
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

    *pTypeHnd = 0;

    TypeHandle clsHnd = TypeHandle();
    FieldDesc* field = (FieldDesc*) fieldHnd;
    CorElementType type   = field->GetFieldType();

    // <REVISIT_TODO>TODO should not burn the time to do this for anything but Value Classes</REVISIT_TODO>
    _ASSERTE(type != ELEMENT_TYPE_BYREF);

    // For verifying code involving generics, use the class instantiation
    // of the optional owner (to provide exact, not representative,
    // type information)
    SigTypeContext typeContext(field, (TypeHandle) owner);

    if (!CorTypeInfo::IsPrimitiveType(type))
    {
        PCCOR_SIGNATURE sig;
        DWORD sigCount;
        CorCallingConvention conv;

        field->GetSig(&sig, &sigCount);

         conv = (CorCallingConvention) CorSigUncompressCallingConv(sig);
        _ASSERTE(isCallConv(conv, IMAGE_CEE_CS_CALLCONV_FIELD));

        SigPointer ptr(sig, sigCount);

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
        SO_TOLERANT;
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
        result += sizeof(Object);
    }

    EE_TO_JIT_TRANSITION();

    return result;
}

/*********************************************************************/
bool CEEInfo::isWriteBarrierHelperRequired(CORINFO_FIELD_HANDLE field)
{
    CONTRACTL {
        SO_TOLERANT;
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    } CONTRACTL_END;

    bool fHelperRequired = false;

    JIT_TO_EE_TRANSITION();

    FieldDesc * pField = (FieldDesc *)field;

    // TODO: jit64 should be switched to the same plan as the i386 jits - use
    // getClassGClayout to figure out the need for writebarrier helper, and inline the copying.
    // Once this happens, USE_WRITE_BARRIER_HELPERS and CORINFO_FLG_WRITE_BARRIER_HELPER can be removed.
    CorElementType type = pField->GetFieldType();

    if(CorTypeInfo::IsObjRef(type))
        fHelperRequired = true;
    else if (type == ELEMENT_TYPE_VALUETYPE)
    {
        TypeHandle th = pField->GetFieldTypeHandleThrowing();
        _ASSERTE(!th.IsNull());
        if(th.GetMethodTable()->ContainsPointers())
            fHelperRequired = true;
    }

    EE_TO_JIT_TRANSITION();

    return fHelperRequired;
}

/*********************************************************************/
DWORD CEEInfo::getFieldThreadLocalStoreID(CORINFO_FIELD_HANDLE fieldHnd, void **ppIndirection)
{
    CONTRACTL {
        SO_TOLERANT;
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    } CONTRACTL_END;

    DWORD result = 0;

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

void *CEEInfo::allocateArray(ULONG cBytes)
{
    CONTRACTL {
        SO_TOLERANT;
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

void CEEInfo::freeArray(void *array)
{
    CONTRACTL {
        SO_TOLERANT;
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    } CONTRACTL_END;

    JIT_TO_EE_TRANSITION();

    delete [] ((BYTE*) array);

    EE_TO_JIT_TRANSITION();
}

void CEEInfo::getBoundaries(CORINFO_METHOD_HANDLE ftn,
                               unsigned int *cILOffsets, DWORD **pILOffsets,
                               ICorDebugInfo::BoundaryTypes *implicitBoundaries)
{
    CONTRACTL {
        SO_TOLERANT;
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    } CONTRACTL_END;

    JIT_TO_EE_TRANSITION();

#ifdef DEBUGGING_SUPPORTED
    if (g_pDebugInterface && !IsCompilationProcess())
    {
        g_pDebugInterface->getBoundaries(GetMethod(ftn), cILOffsets, pILOffsets,
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
        SO_TOLERANT;
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    } CONTRACTL_END;

    JIT_TO_EE_TRANSITION();

#ifdef DEBUGGING_SUPPORTED
    if (g_pDebugInterface && !IsCompilationProcess())
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
        SO_TOLERANT;
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
        SO_TOLERANT;
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
            // (This can only happen in illegal IL
            if (!CorTypeInfo::IsObjRef(normType) || type != ELEMENT_TYPE_VALUETYPE)
            {
                type = normType;
            }
        }
        break;

    case ELEMENT_TYPE_PTR:
        // Load the type eagerly under debugger to make the eval work
        if (!isVerifyOnly() && CORDisableJITOptimizations(pModule->GetDebuggerInfoBits()))
        {
            // NOTE: in some IJW cases, when the type pointed at is unmanaged,
            // the GetTypeHandle may fail, because there is no TypeDef for such type.
            // Usage of GetTypeHandleThrowing would lead to class load exception
            TypeHandle thPtr = ptr.GetTypeHandleNT(pModule, &typeContext);
            if(!thPtr.IsNull())
            {
                m_pOverride->classMustBeLoadedBeforeCodeIsRun(CORINFO_CLASS_HANDLE(thPtr.AsPtr()));
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

/*********************************************************************/

CORINFO_CLASS_HANDLE CEEInfo::getArgClass (
    CORINFO_SIG_INFO*       sig,
    CORINFO_ARG_LIST_HANDLE args
    )
{
    CONTRACTL {
        SO_TOLERANT;
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

CorInfoType CEEInfo::getHFAType(CORINFO_CLASS_HANDLE hClass)
{
    CONTRACTL {
        SO_TOLERANT;
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    } CONTRACTL_END;

    CorInfoType result = CORINFO_TYPE_UNDEF;

#ifdef FEATURE_HFA
    JIT_TO_EE_TRANSITION();

    TypeHandle VMClsHnd(hClass);

    result = asCorInfoType(VMClsHnd.GetHFAType());
    
    EE_TO_JIT_TRANSITION();
#endif

    return result;
}

/*********************************************************************/

    // return the unmanaged calling convention for a PInvoke
CorInfoUnmanagedCallConv CEEInfo::getUnmanagedCallConv(CORINFO_METHOD_HANDLE method)
{
    CONTRACTL {
        SO_TOLERANT;
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    } CONTRACTL_END;

    CorInfoUnmanagedCallConv result = CORINFO_UNMANAGED_CALLCONV_UNKNOWN;

    JIT_TO_EE_TRANSITION();

    MethodDesc* pMD = NULL;
    pMD = GetMethod(method);
    _ASSERTE(pMD->IsNDirect());

#ifdef _TARGET_X86_
    EX_TRY
    {
        PInvokeStaticSigInfo sigInfo(pMD, PInvokeStaticSigInfo::NO_THROW_ON_ERROR);

        switch (sigInfo.GetCallConv()) {
            case pmCallConvCdecl:
                result = CORINFO_UNMANAGED_CALLCONV_C;
                break;
            case pmCallConvStdcall:
                result = CORINFO_UNMANAGED_CALLCONV_STDCALL;
                break;
            case pmCallConvThiscall:
                result = CORINFO_UNMANAGED_CALLCONV_THISCALL;
                break;
            default:
                result = CORINFO_UNMANAGED_CALLCONV_UNKNOWN;
        }
    }
    EX_CATCH
    {
        result = CORINFO_UNMANAGED_CALLCONV_UNKNOWN;
    }
    EX_END_CATCH(SwallowAllExceptions)
#else // !_TARGET_X86_
    //
    // we have only one calling convention
    //
    result = CORINFO_UNMANAGED_CALLCONV_STDCALL;
#endif // !_TARGET_X86_

    EE_TO_JIT_TRANSITION();
    
    return result;
}

/*********************************************************************/
BOOL NDirectMethodDesc::ComputeMarshalingRequired()
{
    WRAPPER_NO_CONTRACT;

    return NDirect::MarshalingRequired(this);
}

/*********************************************************************/
BOOL CEEInfo::pInvokeMarshalingRequired(CORINFO_METHOD_HANDLE method, CORINFO_SIG_INFO* callSiteSig)
{
    CONTRACTL {
        SO_TOLERANT;
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    } CONTRACTL_END;

    BOOL result = FALSE;

    JIT_TO_EE_TRANSITION();

    if (method != 0)
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
    else
    {
        // check the call site signature
        result = NDirect::MarshalingRequired(
                    GetMethod(method),
                    callSiteSig->pSig,
                    GetModule(callSiteSig->scope));
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
BOOL CEEInfo::satisfiesMethodConstraints(
    CORINFO_CLASS_HANDLE        parent,
    CORINFO_METHOD_HANDLE       method)
{
    CONTRACTL {
        SO_TOLERANT;
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    } CONTRACTL_END;

    BOOL result = FALSE;

    JIT_TO_EE_TRANSITION();

    _ASSERTE(parent != NULL);
    _ASSERTE(method != NULL);
    result = GetMethod(method)->SatisfiesMethodConstraints(TypeHandle(parent));

    EE_TO_JIT_TRANSITION();

    return result;
}



/*********************************************************************/
// Given a delegate target class, a target method parent class,  a  target method,
// a delegate class, check if the method signature is compatible with the Invoke method of the delegate
// (under the typical instantiation of any free type variables in the memberref signatures).
//
// objCls should be NULL if the target object is NULL
//@GENERICSVER: new (suitable for generics)
BOOL CEEInfo::isCompatibleDelegate(
            CORINFO_CLASS_HANDLE        objCls,
            CORINFO_CLASS_HANDLE        methodParentCls,
            CORINFO_METHOD_HANDLE       method,
            CORINFO_CLASS_HANDLE        delegateCls,
            BOOL*                       pfIsOpenDelegate)
{
    CONTRACTL {
        SO_TOLERANT;
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    } CONTRACTL_END;

    BOOL result = FALSE;

    JIT_TO_EE_TRANSITION();

    _ASSERTE(method != NULL);
    _ASSERTE(delegateCls != NULL);

    TypeHandle delegateClsHnd = (TypeHandle) delegateCls;

    _ASSERTE(delegateClsHnd.GetMethodTable()->IsDelegate());

    TypeHandle methodParentHnd = (TypeHandle) (methodParentCls);
    MethodDesc* pMDFtn = GetMethod(method);
    TypeHandle objClsHnd(objCls);

    EX_TRY
    {
      result = COMDelegate::ValidateCtor(objClsHnd, methodParentHnd, pMDFtn, delegateClsHnd, pfIsOpenDelegate);
    }
    EX_CATCH
    {
    }
    EX_END_CATCH(SwallowAllExceptions)

    EE_TO_JIT_TRANSITION();

    return result;
}

// Determines whether the delegate creation obeys security transparency rules
BOOL CEEInfo::isDelegateCreationAllowed (
        CORINFO_CLASS_HANDLE        delegateHnd,
        CORINFO_METHOD_HANDLE       calleeHnd)
{
    CONTRACTL {
        SO_TOLERANT;
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    } CONTRACTL_END;

    BOOL isCallAllowed = FALSE;

    JIT_TO_EE_TRANSITION();

    TypeHandle delegateType(delegateHnd);
    MethodDesc* pCallee = GetMethod(calleeHnd);

    isCallAllowed = COMDelegate::ValidateSecurityTransparency(pCallee, delegateType.AsMethodTable());

    EE_TO_JIT_TRANSITION();

    return isCallAllowed;
}

/*********************************************************************/
    // return the unmanaged target *if method has already been prelinked.*
void* CEEInfo::getPInvokeUnmanagedTarget(CORINFO_METHOD_HANDLE method,
                                                    void **ppIndirection)
{
    CONTRACTL {
        SO_TOLERANT;
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    } CONTRACTL_END;

    void* result = NULL;

    if (ppIndirection != NULL)
        *ppIndirection = NULL;

#ifndef CROSSGEN_COMPILE
    JIT_TO_EE_TRANSITION();

    MethodDesc* ftn = GetMethod(method);
    _ASSERTE(ftn->IsNDirect());
    NDirectMethodDesc *pMD = (NDirectMethodDesc*)ftn;

    if (pMD->NDirectTargetIsImportThunk())
    {
#ifdef FEATURE_MIXEDMODE // IJW
        if (pMD->IsEarlyBound()
#ifdef FEATURE_MULTICOREJIT
            // Bug 126723: Calling ClassInit in multicore JIT background thread, return NULL
            // When multicore JIT is enabled (StartProfile called), calling managed code is not allowed in the background thread
            && GetAppDomain()->GetMulticoreJitManager().AllowCCtorsToRunDuringJITing()
#endif
            )
        {
            EX_TRY
            {
                pMD->InitEarlyBoundNDirectTarget();
                _ASSERTE(!pMD->NDirectTargetIsImportThunk());
                result = pMD->GetNDirectTarget();
            }
            EX_CATCH
            {
            }
            EX_END_CATCH(SwallowAllExceptions)
        }
#endif // FEATURE_MIXEDMODE
    }
    else
    {
        result = pMD->GetNDirectTarget();
    }

    EE_TO_JIT_TRANSITION();
#endif // CROSSGEN_COMPILE

    return result;
}

/*********************************************************************/
    // return address of fixup area for late-bound N/Direct calls.
void* CEEInfo::getAddressOfPInvokeFixup(CORINFO_METHOD_HANDLE method,
                                        void **ppIndirection)
{
    CONTRACTL {
        SO_TOLERANT;
        NOTHROW;
        GC_NOTRIGGER;
        MODE_PREEMPTIVE;
    } CONTRACTL_END;

    void * result = NULL;

    if (ppIndirection != NULL)
        *ppIndirection = NULL;

    JIT_TO_EE_TRANSITION_LEAF();

    MethodDesc* ftn = GetMethod(method);
    _ASSERTE(ftn->IsNDirect());
    NDirectMethodDesc *pMD = (NDirectMethodDesc*)ftn;

    result = (LPVOID)&(pMD->GetWriteableData()->m_pNDirectTarget);

    EE_TO_JIT_TRANSITION_LEAF();

    return result;
}

/*********************************************************************/
    // return address of fixup area for late-bound N/Direct calls.
void CEEInfo::getAddressOfPInvokeTarget(CORINFO_METHOD_HANDLE method,
                                        CORINFO_CONST_LOOKUP *pLookup)
{
    WRAPPER_NO_CONTRACT;

    void *pIndirection;
    pLookup->accessType = IAT_PVALUE;
    pLookup->addr = getAddressOfPInvokeFixup(method, &pIndirection);
    _ASSERTE(pIndirection == NULL);
}

/*********************************************************************/
CORINFO_JUST_MY_CODE_HANDLE CEEInfo::getJustMyCodeHandle(
                CORINFO_METHOD_HANDLE       method,
                CORINFO_JUST_MY_CODE_HANDLE**ppIndirection)
{
    CONTRACTL {
        SO_TOLERANT;
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
}

/*********************************************************************/
// Return details about EE internal data structures 
void CEEInfo::getEEInfo(CORINFO_EE_INFO *pEEInfoOut)
{
    CONTRACTL {
        SO_TOLERANT;
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

    // Delegate offsets
    pEEInfoOut->offsetOfDelegateInstance    = DelegateObject::GetOffsetOfTarget();
    pEEInfoOut->offsetOfDelegateFirstTarget = DelegateObject::GetOffsetOfMethodPtr();

    // Remoting offsets
    pEEInfoOut->offsetOfTransparentProxyRP = TransparentProxyObject::GetOffsetOfRP();
    pEEInfoOut->offsetOfRealProxyServer    = RealProxyObject::GetOffsetOfServerObject();

    pEEInfoOut->offsetOfObjArrayData       = (DWORD)PtrArray::GetDataOffset();

    OSVERSIONINFO   sVerInfo;
    sVerInfo.dwOSVersionInfoSize = sizeof(OSVERSIONINFO);
    GetOSVersion(&sVerInfo);

    pEEInfoOut->osType = CORINFO_WINNT;

    pEEInfoOut->osMajor = sVerInfo.dwMajorVersion;
    pEEInfoOut->osMinor = sVerInfo.dwMinorVersion;
    pEEInfoOut->osBuild = sVerInfo.dwBuildNumber;

    EE_TO_JIT_TRANSITION();
}

LPCWSTR CEEInfo::getJitTimeLogFilename()
{
    CONTRACTL {
        SO_TOLERANT;
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    } CONTRACTL_END;

    LPCWSTR result = NULL;

    JIT_TO_EE_TRANSITION();
    result = CLRConfig::GetConfigValue(CLRConfig::INTERNAL_JitTimeLogFile);
    EE_TO_JIT_TRANSITION();

    return result;
}



    // Return details about EE internal data structures
DWORD CEEInfo::getThreadTLSIndex(void **ppIndirection)
{
    CONTRACTL {
        SO_TOLERANT;
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    } CONTRACTL_END;

    DWORD result = (DWORD)-1;

    if (ppIndirection != NULL)
        *ppIndirection = NULL;

    JIT_TO_EE_TRANSITION();

#if !defined(CROSSGEN_COMPILE) && !defined(FEATURE_IMPLICIT_TLS)
    result = GetThreadTLSIndex();

    // The JIT can use the optimized TLS access only if the runtime is using it as well.
    //  (This is necessaryto make managed code work well under appverifier.)
    if (GetTLSAccessMode(result) == TLSACCESS_GENERIC)
        result = (DWORD)-1;
#endif

    EE_TO_JIT_TRANSITION();

    return result;
}

const void * CEEInfo::getInlinedCallFrameVptr(void **ppIndirection)
{
    CONTRACTL {
        SO_TOLERANT;
        NOTHROW;
        GC_NOTRIGGER;
        MODE_PREEMPTIVE;
    } CONTRACTL_END;

    void * result = NULL;

    if (ppIndirection != NULL)
        *ppIndirection = NULL;

    JIT_TO_EE_TRANSITION_LEAF();

#ifndef CROSSGEN_COMPILE
    result = (void*)InlinedCallFrame::GetMethodFrameVPtr();
#else
    result = (void*)0x43210;
#endif

    EE_TO_JIT_TRANSITION_LEAF();

    return result;
}


SIZE_T * CEEInfo::getAddrModuleDomainID(CORINFO_MODULE_HANDLE   module)
{
    CONTRACTL {
        SO_TOLERANT;
        NOTHROW;
        GC_NOTRIGGER;
        MODE_PREEMPTIVE;
    } CONTRACTL_END;

    SIZE_T * result = NULL;

    JIT_TO_EE_TRANSITION_LEAF();

    Module* pModule = GetModule(module);

    result = pModule->GetAddrModuleID();

    EE_TO_JIT_TRANSITION_LEAF();

    return result;
}

LONG * CEEInfo::getAddrOfCaptureThreadGlobal(void **ppIndirection)
{
    CONTRACTL {
        SO_TOLERANT;
        NOTHROW;
        GC_NOTRIGGER;
        MODE_PREEMPTIVE;
    } CONTRACTL_END;

    LONG * result = NULL;

    if (ppIndirection != NULL)
        *ppIndirection = NULL;

    JIT_TO_EE_TRANSITION_LEAF();

    result = (LONG *)&g_TrapReturningThreads;

    EE_TO_JIT_TRANSITION_LEAF();

    return result;
}



HRESULT CEEInfo::GetErrorHRESULT(struct _EXCEPTION_POINTERS *pExceptionPointers)
{
    CONTRACTL {
        SO_TOLERANT;
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
    } CONTRACTL_END;

    HRESULT hr = S_OK;

    //This function is called from the JIT64 exception filter during PEVerify.  Because it is a filter, it
    //can be "called" from a NOTHROW region in the case of StackOverflow.  Security::MapToHR throws
    //internally, but it catches all exceptions.  Therefore, none of the children can cause an exception to
    //percolate out of this function (except for Stack Overflow).  Obviously I can't explain most of this to
    //the Contracts system, and I can't add this CONTRACT_VIOLATION to the filter in Jit64.
    CONTRACT_VIOLATION(ThrowsViolation);

    JIT_TO_EE_TRANSITION();

    GCX_COOP();

    OBJECTREF throwable = GetThread()->LastThrownObject();
    hr = GetExceptionHResult(throwable);

    EE_TO_JIT_TRANSITION();

    return hr;
}


ULONG CEEInfo::GetErrorMessage(__inout_ecount(bufferLength) LPWSTR buffer, ULONG bufferLength)
{
    CONTRACTL {
        SO_TOLERANT;
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    } CONTRACTL_END;

    ULONG result = 0;

#ifndef CROSSGEN_COMPILE
    JIT_TO_EE_TRANSITION();

    GCX_COOP();

    OBJECTREF throwable = GetThread()->LastThrownObject();

    if (throwable != NULL)
    {
        EX_TRY
        {
            result = GetExceptionMessage(throwable, buffer, bufferLength);
        }
        EX_CATCH
        {
        }
        EX_END_CATCH(SwallowAllExceptions)
    }

    EE_TO_JIT_TRANSITION();
#endif

    return result;
}

// This method is called from CEEInfo::FilterException which
// is run as part of the SEH filter clause for the JIT.
// It is fatal to throw an exception while running a SEH filter clause
// so our contract is NOTHROW, NOTRIGGER.
//
int CEEInfo::FilterException(struct _EXCEPTION_POINTERS *pExceptionPointers)
{
    CONTRACTL {
        SO_TOLERANT;
        NOTHROW;
        GC_NOTRIGGER;
    } CONTRACTL_END;

    int result = 0;

    JIT_TO_EE_TRANSITION_LEAF();

    VALIDATE_BACKOUT_STACK_CONSUMPTION;

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
#ifdef CROSSGEN_COMPILE
    else
    {
        result = EXCEPTION_EXECUTE_HANDLER;
    }
#else
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
#endif

    EE_TO_JIT_TRANSITION_LEAF();

    return result;
}

// This code is called if FilterException chose to handle the exception.
void CEEInfo::HandleException(struct _EXCEPTION_POINTERS *pExceptionPointers)
{
    CONTRACTL {
        SO_TOLERANT;
        NOTHROW;
        GC_NOTRIGGER;
    } CONTRACTL_END;

    JIT_TO_EE_TRANSITION_LEAF();

#ifndef CROSSGEN_COMPILE
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
#endif

    EE_TO_JIT_TRANSITION_LEAF();
}

void ThrowExceptionForJit(HRESULT res);

void CEEInfo::ThrowExceptionForJitResult(
        HRESULT result)
{
    CONTRACTL {
        SO_TOLERANT;
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    } CONTRACTL_END;

    JIT_TO_EE_TRANSITION();

    if (!SUCCEEDED(result))
        ThrowExceptionForJit(result);

    EE_TO_JIT_TRANSITION();
}


CORINFO_MODULE_HANDLE CEEInfo::embedModuleHandle(CORINFO_MODULE_HANDLE handle,
                                                 void **ppIndirection)
{
    CONTRACTL {
        SO_TOLERANT;
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
        SO_TOLERANT;
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
        SO_TOLERANT;
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
        SO_TOLERANT;
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
DWORD CEEInfo::getJitFlags(CORJIT_FLAGS* jitFlags, DWORD sizeInBytes)
{
    CONTRACTL {
        SO_TOLERANT;
        NOTHROW;
        GC_NOTRIGGER;
        MODE_PREEMPTIVE;
    } CONTRACTL_END;

    JIT_TO_EE_TRANSITION_LEAF();

    EE_TO_JIT_TRANSITION_LEAF();

    return 0;
}

/*********************************************************************/
IEEMemoryManager* CEEInfo::getMemoryManager()
{
    CONTRACTL {
        SO_TOLERANT;
        NOTHROW;
        GC_NOTRIGGER;
        MODE_PREEMPTIVE;
    } CONTRACTL_END;

    IEEMemoryManager* result = NULL;

    JIT_TO_EE_TRANSITION_LEAF();

    result = GetEEMemoryManager();

    EE_TO_JIT_TRANSITION_LEAF();

    return result;
}

/*********************************************************************/
int CEEInfo::doAssert(const char* szFile, int iLine, const char* szExpr)
{
    STATIC_CONTRACT_SO_TOLERANT;
    STATIC_CONTRACT_THROWS;
    STATIC_CONTRACT_GC_TRIGGERS;
    STATIC_CONTRACT_MODE_PREEMPTIVE;
    STATIC_CONTRACT_DEBUG_ONLY;

    int result = 0;

    JIT_TO_EE_TRANSITION();

#ifdef CROSSGEN_COMPILE
    ThrowHR(COR_E_INVALIDPROGRAM);
#else

#ifdef _DEBUG
    BEGIN_DEBUG_ONLY_CODE;
    result = _DbgBreakCheck(szFile, iLine, szExpr);
    END_DEBUG_ONLY_CODE;
#else // !_DEBUG
    result = 1;   // break into debugger
#endif // !_DEBUG

#endif

    EE_TO_JIT_TRANSITION();

    return result;
}

void CEEInfo::reportFatalError(CorJitResult result)
{
    STATIC_CONTRACT_SO_TOLERANT;
    STATIC_CONTRACT_THROWS;
    STATIC_CONTRACT_GC_TRIGGERS;
    STATIC_CONTRACT_MODE_PREEMPTIVE;

    JIT_TO_EE_TRANSITION_LEAF();

    STRESS_LOG2(LF_JIT,LL_ERROR, "Jit reported error 0x%x while compiling 0x%p\n",
                (int)result, (INT_PTR)getMethodBeingCompiled());

    EE_TO_JIT_TRANSITION_LEAF();
}

BOOL CEEInfo::logMsg(unsigned level, const char* fmt, va_list args)
{
    STATIC_CONTRACT_SO_TOLERANT;
    STATIC_CONTRACT_THROWS;
    STATIC_CONTRACT_GC_TRIGGERS;
    STATIC_CONTRACT_MODE_PREEMPTIVE;
    STATIC_CONTRACT_DEBUG_ONLY;

    BOOL result = FALSE;

    JIT_TO_EE_TRANSITION_LEAF();

#ifdef LOGGING
    if (LoggingOn(LF_JIT, level))
    {
        LogSpewValist(LF_JIT, level, (char*) fmt, args);
        result = TRUE;
    }
#endif // LOGGING

    EE_TO_JIT_TRANSITION_LEAF();

    return result;
}

void CEEInfo::yieldExecution()
{
    WRAPPER_NO_CONTRACT;
    // DDR: 17066 - Performance degrade 
    // The JIT should not give up it's time slice when we are not hosted
    if (CLRTaskHosted())
    {
        // SwitchToTask forces the current thread to give up quantum, while a host can decide what
        // to do with Sleep if the current thread has not run out of quantum yet.
        ClrSleepEx(0, FALSE);
    }
}


#ifndef CROSSGEN_COMPILE

/*********************************************************************/

void* CEEJitInfo::getHelperFtn(CorInfoHelpFunc    ftnNum,         /* IN  */
                               void **            ppIndirection)  /* OUT */
{
    CONTRACTL {
        SO_TOLERANT;
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

#if defined(_TARGET_AMD64_)
        // To avoid using a jump stub we always call certain helpers using an indirect call.
        // Because when using a direct call and the target is father away than 2^31 bytes,
        // the direct call instead goes to a jump stub which jumps to the jit helper.
        // However in this process the jump stub will corrupt RAX.
        //
        // The set of helpers for which RAX must be preserved are the profiler probes
        // and the STOP_FOR_GC helper which maps to JIT_RareDisableHelper.
        // In the case of the STOP_FOR_GC helper RAX can be holding a function return value.
        //
        if (dynamicFtnNum == DYNAMIC_CORINFO_HELP_STOP_FOR_GC    ||
            dynamicFtnNum == DYNAMIC_CORINFO_HELP_PROF_FCN_ENTER ||
            dynamicFtnNum == DYNAMIC_CORINFO_HELP_PROF_FCN_LEAVE ||
            dynamicFtnNum == DYNAMIC_CORINFO_HELP_PROF_FCN_TAILCALL)
        {
            _ASSERTE(ppIndirection != NULL);
            *ppIndirection = &hlpDynamicFuncTable[dynamicFtnNum].pfnHelper;
            return NULL;
        }
#endif

#if defined(ENABLE_FAST_GCPOLL_HELPER)
        //always call this indirectly so that we can swap GC Poll helpers.
        if (dynamicFtnNum == DYNAMIC_CORINFO_HELP_POLL_GC)
        {
            _ASSERTE(ppIndirection != NULL);
            *ppIndirection = &hlpDynamicFuncTable[dynamicFtnNum].pfnHelper;
            return NULL;
        }
#endif

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

void CEEJitInfo::addActiveDependency(CORINFO_MODULE_HANDLE moduleFrom,CORINFO_MODULE_HANDLE moduleTo)
{
    CONTRACTL
    {
        STANDARD_VM_CHECK;
        PRECONDITION(CheckPointer(moduleFrom));
        PRECONDITION(!IsDynamicScope(moduleFrom));
        PRECONDITION(CheckPointer(moduleTo));
        PRECONDITION(!IsDynamicScope(moduleTo));
        PRECONDITION(moduleFrom != moduleTo);
    }
    CONTRACTL_END;

    // This is only called internaly. JIT-EE transition is not needed.
    // JIT_TO_EE_TRANSITION();

    Module *dependency = (Module *)moduleTo;
    _ASSERTE(!dependency->IsSystem());

    if (m_pMethodBeingCompiled->IsLCGMethod())
    {
        // The context module of the m_pMethodBeingCompiled is irrelevant.  Rather than tracking
        // the dependency, we just do immediate activation.
        dependency->EnsureActive();
    }
    else
    {
#ifdef FEATURE_LOADER_OPTIMIZATION
        Module *context = (Module *)moduleFrom;

        // Record active dependency for loader.
        context->AddActiveDependency(dependency, FALSE);
#else
        dependency->EnsureActive();
#endif
    }

    // EE_TO_JIT_TRANSITION();
}


// Wrapper around CEEInfo::GetProfilingHandle.  The first time this is called for a
// method desc, it calls through to EEToProfInterfaceImpl::EEFunctionIDMappe and caches the
// result in CEEJitInfo::GetProfilingHandleCache.  Thereafter, this wrapper regurgitates the cached values
// rather than calling into CEEInfo::GetProfilingHandle each time.  This avoids
// making duplicate calls into the profiler's FunctionIDMapper callback.
void CEEJitInfo::GetProfilingHandle(BOOL                      *pbHookFunction,
                                    void                     **pProfilerHandle,
                                    BOOL                      *pbIndirectedHandles)
{
    CONTRACTL {
        SO_TOLERANT;
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
            BEGIN_PIN_PROFILER(CORProfilerFunctionIDMapperEnabled());
            profilerHandle = (void *)g_profControlBlock.pProfInterface->EEFunctionIDMapper((FunctionID) m_pMethodBeingCompiled, &bHookFunction);
            END_PIN_PROFILER();
        }

        m_gphCache.m_pvGphProfilerHandle = profilerHandle;
        m_gphCache.m_bGphHookFunction = (bHookFunction != FALSE);
        m_gphCache.m_bGphIsCacheValid = true;

        EE_TO_JIT_TRANSITION();
#endif //PROFILING_SUPPORTED
    }
        
    // Our cache of these values are bitfield bools, but the interface requires
    // BOOL.  So to avoid setting aside a staging area on the stack for these
    // values, we filled them in directly in the if (not cached yet) case.
    *pbHookFunction = (m_gphCache.m_bGphHookFunction != false);

    // At this point, the remaining values must be in the cache by now, so use them  
    *pProfilerHandle = m_gphCache.m_pvGphProfilerHandle;

    //
    // This is the JIT case, which is never indirected.
    //
    *pbIndirectedHandles = FALSE;
}

/*********************************************************************/
void CEEJitInfo::BackoutJitData(EEJitManager * jitMgr)
{
    CONTRACTL {
        NOTHROW;
        GC_TRIGGERS;
    } CONTRACTL_END;

    CodeHeader* pCodeHeader = GetCodeHeader();
    if (pCodeHeader)
        jitMgr->RemoveJitData(pCodeHeader, m_GCinfo_len, m_EHinfo_len);
}

/*********************************************************************/
// Route jit information to the Jit Debug store.
void CEEJitInfo::setBoundaries(CORINFO_METHOD_HANDLE ftn, ULONG32 cMap,
                               ICorDebugInfo::OffsetMapping *pMap)
{
    CONTRACTL {
        SO_TOLERANT;
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

void CEEJitInfo::setVars(CORINFO_METHOD_HANDLE ftn, ULONG32 cVars, ICorDebugInfo::NativeVarInfo *vars)
{
    CONTRACTL {
        SO_TOLERANT;
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

void CEEJitInfo::CompressDebugInfo()
{
    CONTRACTL {
        SO_TOLERANT;
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    } CONTRACTL_END;

    // Don't track JIT info for DynamicMethods.
    if (m_pMethodBeingCompiled->IsDynamicMethod())
        return;

    if (m_iOffsetMapping == 0 && m_iNativeVarInfo == 0)
        return;

    JIT_TO_EE_TRANSITION();

    EX_TRY
    {
        PTR_BYTE pDebugInfo = CompressDebugInfo::CompressBoundariesAndVars(
            m_pOffsetMapping, m_iOffsetMapping,
            m_pNativeVarInfo, m_iNativeVarInfo,
            NULL, 
            m_pMethodBeingCompiled->GetLoaderAllocator()->GetLowFrequencyHeap());

        GetCodeHeader()->SetDebugInfo(pDebugInfo);
    }
    EX_CATCH
    {
        // Just ignore exceptions here. The debugger's structures will still be in a consistent state.
    }
    EX_END_CATCH(SwallowAllExceptions)

    EE_TO_JIT_TRANSITION();
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
void CEEJitInfo::reserveUnwindInfo(BOOL isFunclet, BOOL isColdCode, ULONG unwindSize)
{
#ifdef WIN64EXCEPTIONS
    CONTRACTL {
        SO_TOLERANT;
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
        "Set COMPlus_JitBreakOnBadCode=1 and rerun to get the real error.");

    ULONG currentSize  = unwindSize;

#if defined(_TARGET_AMD64_)
    // Add space for personality routine, it must be 4-byte aligned.
    // Everything in the UNWIND_INFO up to the variable-sized UnwindCodes
    // array has already had its size included in unwindSize by the caller.
    currentSize += sizeof(ULONG);

    // Note that the count of unwind codes (2 bytes each) is stored as a UBYTE
    // So the largest size could be 510 bytes, plus the header and language
    // specific stuff.  This can't overflow.

    _ASSERTE(FitsInU4(currentSize + sizeof(ULONG)));
    currentSize = (ULONG)(ALIGN_UP(currentSize, sizeof(ULONG)));
#elif defined(_TARGET_ARM_) || defined(_TARGET_ARM64_)
    // The JIT passes in a 4-byte aligned block of unwind data.
    _ASSERTE(IS_ALIGNED(currentSize, sizeof(ULONG)));

    // Add space for personality routine, it must be 4-byte aligned.
    currentSize += sizeof(ULONG);
#else
    PORTABILITY_ASSERT("CEEJitInfo::reserveUnwindInfo");
#endif // !defined(_TARGET_AMD64_)

    m_totalUnwindSize += currentSize;

    m_totalUnwindInfos++;

    EE_TO_JIT_TRANSITION_LEAF();
#else // WIN64EXCEPTIONS
    LIMITED_METHOD_CONTRACT;
    // Dummy implementation to make cross-platform altjit work
#endif // WIN64EXCEPTIONS
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
        BYTE *              pHotCode,              /* IN */
        BYTE *              pColdCode,             /* IN */
        ULONG               startOffset,           /* IN */
        ULONG               endOffset,             /* IN */
        ULONG               unwindSize,            /* IN */
        BYTE *              pUnwindBlock,          /* IN */
        CorJitFuncKind      funcKind               /* IN */
        )
{
#ifdef WIN64EXCEPTIONS
    CONTRACTL {
        SO_TOLERANT;
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

    PT_RUNTIME_FUNCTION pRuntimeFunction = m_CodeHeader->GetUnwindInfo(m_usedUnwindInfos);
    m_usedUnwindInfos++;

    // Make sure that the RUNTIME_FUNCTION is aligned on a DWORD sized boundary
    _ASSERTE(IS_ALIGNED(pRuntimeFunction, sizeof(DWORD)));

    UNWIND_INFO * pUnwindInfo = (UNWIND_INFO *) &(m_theUnwindBlock[m_usedUnwindSize]);
    m_usedUnwindSize += unwindSize;

#if defined(_TARGET_AMD64_)
    // Add space for personality routine, it must be 4-byte aligned.
    // Everything in the UNWIND_INFO up to the variable-sized UnwindCodes
    // array has already had its size included in unwindSize by the caller.
    m_usedUnwindSize += sizeof(ULONG);

    // Note that the count of unwind codes (2 bytes each) is stored as a UBYTE
    // So the largest size could be 510 bytes, plus the header and language
    // specific stuff.  This can't overflow.

    _ASSERTE(FitsInU4(m_usedUnwindSize + sizeof(ULONG)));
    m_usedUnwindSize = (ULONG)(ALIGN_UP(m_usedUnwindSize,sizeof(ULONG)));
#elif defined(_TARGET_ARM_) || defined(_TARGET_ARM64_)
    // The JIT passes in a 4-byte aligned block of unwind data.
    _ASSERTE(IS_ALIGNED(m_usedUnwindSize, sizeof(ULONG)));

    // Add space for personality routine, it must be 4-byte aligned.
    m_usedUnwindSize += sizeof(ULONG);
#else
    PORTABILITY_ASSERT("CEEJitInfo::reserveUnwindInfo");
#endif

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

#if defined(_TARGET_X86_) || defined(_TARGET_AMD64_)
    pRuntimeFunction->EndAddress        = currentCodeOffset + endOffset;
#endif

    RUNTIME_FUNCTION__SetUnwindInfoAddress(pRuntimeFunction, unwindInfoDelta);

#ifdef _DEBUG
    if (funcKind != CORJIT_FUNC_ROOT)
    {
        // Check the the new funclet doesn't overlap any existing funclet.

        for (ULONG iUnwindInfo = 0; iUnwindInfo < m_usedUnwindInfos - 1; iUnwindInfo++)
        {
            PT_RUNTIME_FUNCTION pOtherFunction = m_CodeHeader->GetUnwindInfo(iUnwindInfo);
            _ASSERTE((   RUNTIME_FUNCTION__BeginAddress(pOtherFunction) >= RUNTIME_FUNCTION__EndAddress(pRuntimeFunction, baseAddress)
                     || RUNTIME_FUNCTION__EndAddress(pOtherFunction, baseAddress) <= RUNTIME_FUNCTION__BeginAddress(pRuntimeFunction)));
        }
    }
#endif // _DEBUG

#if defined(_TARGET_AMD64_)

    /* Copy the UnwindBlock */
    memcpy(pUnwindInfo, pUnwindBlock, unwindSize);

    pUnwindInfo->Flags = UNW_FLAG_EHANDLER | UNW_FLAG_UHANDLER;

    ULONG * pPersonalityRoutine = (ULONG*)ALIGN_UP(&(pUnwindInfo->UnwindCode[pUnwindInfo->CountOfUnwindCodes]), sizeof(ULONG));
    *pPersonalityRoutine = ExecutionManager::GetCLRPersonalityRoutineValue();

#elif defined(_TARGET_ARM64_)

    /* Copy the UnwindBlock */
    memcpy(pUnwindInfo, pUnwindBlock, unwindSize);

    *(LONG *)pUnwindInfo |= (1 << 20); // X bit

    ULONG * pPersonalityRoutine = (ULONG*)((BYTE *)pUnwindInfo + ALIGN_UP(unwindSize, sizeof(ULONG)));
    *pPersonalityRoutine = ExecutionManager::GetCLRPersonalityRoutineValue();

#elif defined(_TARGET_ARM_)

    /* Copy the UnwindBlock */
    memcpy(pUnwindInfo, pUnwindBlock, unwindSize);

    *(LONG *)pUnwindInfo |= (1 << 20); // X bit

    ULONG * pPersonalityRoutine = (ULONG*)((BYTE *)pUnwindInfo + ALIGN_UP(unwindSize, sizeof(ULONG)));
    *pPersonalityRoutine = (TADDR)ProcessCLRException - baseAddress;
#endif

#if defined(_TARGET_AMD64_)
    // Publish the new unwind information in a way that the ETW stack crawler can find
    if (m_usedUnwindInfos == m_totalUnwindInfos)
        UnwindInfoTable::PublishUnwindInfoForMethod(baseAddress, m_CodeHeader->GetUnwindInfo(0), m_totalUnwindInfos);
#endif // defined(_TARGET_AMD64_)

    EE_TO_JIT_TRANSITION();
#else // WIN64EXCEPTIONS
    LIMITED_METHOD_CONTRACT;
    // Dummy implementation to make cross-platform altjit work
#endif // WIN64EXCEPTIONS
}

void CEEJitInfo::recordCallSite(ULONG                 instrOffset,
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
                                  void * target,
                                  WORD   fRelocType,
                                  WORD   slot,
                                  INT32  addlDelta)
{
    CONTRACTL {
        SO_TOLERANT;
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    } CONTRACTL_END;

#ifdef _WIN64
    JIT_TO_EE_TRANSITION();

    INT64 delta;

    switch (fRelocType)
    {
    case IMAGE_REL_BASED_DIR64:
        // Write 64-bits into location
        *((UINT64 *) ((BYTE *) location + slot)) = (UINT64) target;
        break;

#ifdef _TARGET_AMD64_
    case IMAGE_REL_BASED_REL32:
        {
            target = (BYTE *)target + addlDelta;

            INT32 * fixupLocation = (INT32 *) ((BYTE *) location + slot);
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
                    m_fRel32Overflow = TRUE;
                    delta = 0;
                }
                else
                {
                    //
                    // When m_fAllowRel32 == FALSE, the JIT will use a REL32s for direct code targets only.
                    // Use jump stub.
                    // 
                    delta = rel32UsingJumpStub(fixupLocation, (PCODE)target, m_pMethodBeingCompiled);
                }
            }

            LOG((LF_JIT, LL_INFO100000, "Encoded a PCREL32 at" FMT_ADDR "to" FMT_ADDR "+%d,  delta is 0x%04x\n",
                 DBG_ADDR(fixupLocation), DBG_ADDR(target), addlDelta, delta));

            // Write the 32-bits pc-relative delta into location
            *fixupLocation = (INT32) delta;
        }
        break;
#endif // _TARGET_AMD64_

#ifdef _TARGET_ARM64_
    case IMAGE_REL_ARM64_BRANCH26:   // 26 bit offset << 2 & sign ext, for B and BL
        {
            _ASSERTE(slot == 0);
            _ASSERTE(addlDelta == 0);

            PCODE branchTarget  = (PCODE) target;
            _ASSERTE((branchTarget & 0x3) == 0);   // the low two bits must be zero

            PCODE fixupLocation = (PCODE) location;
            _ASSERTE((fixupLocation & 0x3) == 0);  // the low two bits must be zero

            delta = (INT64)(branchTarget - fixupLocation);
            _ASSERTE((delta & 0x3) == 0);          // the low two bits must be zero

            UINT32 branchInstr = *((UINT32*) fixupLocation);
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
                                                                (BYTE *) hiAddr);

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

            PutArm64Rel28((UINT32*) fixupLocation, (INT32)delta);
        }
        break;
#endif // _TARGET_ARM64_

    default:
        _ASSERTE(!"Unknown reloc type");
        break;
    }

    EE_TO_JIT_TRANSITION();
#else // _WIN64
    JIT_TO_EE_TRANSITION_LEAF();

    // Nothing to do on 32-bit

    EE_TO_JIT_TRANSITION_LEAF();
#endif // _WIN64
}

WORD CEEJitInfo::getRelocTypeHint(void * target)
{
    CONTRACTL {
        SO_TOLERANT;
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    } CONTRACTL_END;

#ifdef _TARGET_AMD64_
    if (m_fAllowRel32)
    {
        // The JIT calls this method for data addresses only. It always uses REL32s for direct code targets.
        if (IsPreferredExecutableRange(target))
            return IMAGE_REL_BASED_REL32;
    }
#endif // _TARGET_AMD64_

    // No hints
    return (WORD)-1;
}

void CEEJitInfo::getModuleNativeEntryPointRange(void** pStart, void** pEnd)
{
    CONTRACTL {
        SO_TOLERANT;
        NOTHROW;
        GC_NOTRIGGER;
        MODE_PREEMPTIVE;
    }
    CONTRACTL_END;

    JIT_TO_EE_TRANSITION_LEAF();

    *pStart = *pEnd = 0;

    EE_TO_JIT_TRANSITION_LEAF();
}

DWORD CEEJitInfo::getExpectedTargetArchitecture()
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
        SO_TOLERANT;
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
        *ppValue = (LPVOID)ConstructStringLiteral(scopeHnd, metaTok); // throws
    }

    EE_TO_JIT_TRANSITION();

    return result;
}

/*********************************************************************/
InfoAccessType CEEJitInfo::emptyStringLiteral(void ** ppValue)
{
    CONTRACTL {
        SO_TOLERANT;
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    } CONTRACTL_END;

    InfoAccessType result = IAT_PVALUE;

    if(NingenEnabled())
    {
        *ppValue = NULL;
        return result;
    }

    JIT_TO_EE_TRANSITION();
    *ppValue = StringObject::GetEmptyStringRefPtr();
    EE_TO_JIT_TRANSITION();

    return result;
}

/*********************************************************************/
void* CEEJitInfo::getFieldAddress(CORINFO_FIELD_HANDLE fieldHnd,
                                  void **ppIndirection)
{
    CONTRACTL {
        SO_TOLERANT;
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    } CONTRACTL_END;

    void *result = NULL;

    if (ppIndirection != NULL)
        *ppIndirection = NULL;

    // Do not bother with initialization if we are only verifying the method.
    if (isVerifyOnly())
    {
        return (void *)0x10;
    }

    JIT_TO_EE_TRANSITION();

    FieldDesc* field = (FieldDesc*) fieldHnd;

    MethodTable* pMT = field->GetEnclosingMethodTable();

    _ASSERTE(!pMT->ContainsGenericVariables());

    // We must not call here for statics of collectible types.
    _ASSERTE(!pMT->Collectible());

    void *base = NULL;

    if (!field->IsRVA())
    {
        // <REVISIT_TODO>@todo: assert that the current method being compiled is unshared</REVISIT_TODO>

        // Allocate space for the local class if necessary, but don't trigger
        // class construction.
        DomainLocalModule *pLocalModule = pMT->GetDomainLocalModule();
        pLocalModule->PopulateClass(pMT);

        GCX_COOP();

        base = (void *) field->GetBase();
    }

    result = field->GetStaticAddressHandle(base);

    EE_TO_JIT_TRANSITION();

    return result;
}

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
        SO_TOLERANT;
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
HRESULT CEEJitInfo::allocBBProfileBuffer (
    ULONG                         count,
    ICorJitInfo::ProfileBuffer ** profileBuffer
    )
{
    CONTRACTL {
        SO_TOLERANT;
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    } CONTRACTL_END;

    HRESULT hr = E_FAIL;

    JIT_TO_EE_TRANSITION();

#ifdef FEATURE_PREJIT

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
    
    *profileBuffer = m_pMethodBeingCompiled->GetLoaderModule()->AllocateProfileBuffer(m_pMethodBeingCompiled->GetMemberDef(), count, codeSize);
    hr = (*profileBuffer ? S_OK : E_OUTOFMEMORY);
#else // FEATURE_PREJIT
    _ASSERTE(!"allocBBProfileBuffer not implemented on CEEJitInfo!");
    hr = E_NOTIMPL;
#endif // !FEATURE_PREJIT

    EE_TO_JIT_TRANSITION();
    
    return hr;
}

// Consider implementing getBBProfileData on CEEJitInfo.  This will allow us
// to use profile info in codegen for non zapped images.
HRESULT CEEJitInfo::getBBProfileData (
    CORINFO_METHOD_HANDLE         ftnHnd,
    ULONG *                       size,
    ICorJitInfo::ProfileBuffer ** profileBuffer,
    ULONG *                       numRuns
    )
{
    LIMITED_METHOD_CONTRACT;
    _ASSERTE(!"getBBProfileData not implemented on CEEJitInfo!");
    return E_NOTIMPL;
}

void CEEJitInfo::allocMem (
    ULONG               hotCodeSize,    /* IN */
    ULONG               coldCodeSize,   /* IN */
    ULONG               roDataSize,     /* IN */
    ULONG               xcptnsCount,    /* IN */
    CorJitAllocMemFlag  flag,           /* IN */
    void **             hotCodeBlock,   /* OUT */
    void **             coldCodeBlock,  /* OUT */
    void **             roDataBlock     /* OUT */
            )
{
    CONTRACTL {
        SO_TOLERANT;
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    } CONTRACTL_END;

    JIT_TO_EE_TRANSITION();

    _ASSERTE(coldCodeSize == 0);
    if (coldCodeBlock)
    {
        *coldCodeBlock = NULL;
    }

    ULONG codeSize      = hotCodeSize;
    void **codeBlock    = hotCodeBlock;

    S_SIZE_T totalSize = S_SIZE_T(codeSize);

    size_t roDataAlignment = sizeof(void*);
    if ((flag & CORJIT_ALLOCMEM_FLG_RODATA_16BYTE_ALIGN)!= 0)
    {
        roDataAlignment = 16;
    }
    else if (roDataSize >= 8)
    {
        roDataAlignment = 8;
    }
    if (roDataSize > 0)
    {
        size_t codeAlignment = ((flag & CORJIT_ALLOCMEM_FLG_16BYTE_ALIGN)!= 0)
                               ? 16 : sizeof(void*);
        totalSize.AlignUp(codeAlignment);
        if (roDataAlignment > codeAlignment) {
            // Add padding to align read-only data.
            totalSize += (roDataAlignment - codeAlignment);
        }
        totalSize += roDataSize;
    }

#ifdef WIN64EXCEPTIONS
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

    m_CodeHeader = m_jitManager->allocCode(m_pMethodBeingCompiled, totalSize.Value(), flag
#ifdef WIN64EXCEPTIONS
                                           , m_totalUnwindInfos
                                           , &m_moduleBase
#endif
                                           );

    BYTE* current = (BYTE *)m_CodeHeader->GetCodeStartAddress();

    *codeBlock = current;
    current += codeSize;

    if (roDataSize > 0)
    {
        current = (BYTE *)ALIGN_UP(current, roDataAlignment);
        *roDataBlock = current;
        current += roDataSize;
    }
    else
    {
        *roDataBlock = NULL;
    }

#ifdef WIN64EXCEPTIONS
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
        SO_TOLERANT;
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    } CONTRACTL_END;

    void * block = NULL;

    JIT_TO_EE_TRANSITION();

    _ASSERTE(m_CodeHeader != 0);
    _ASSERTE(m_CodeHeader->GetGCInfo() == 0);

#ifdef _WIN64
    if (size & 0xFFFFFFFF80000000LL)
    {
        COMPlusThrowHR(CORJIT_OUTOFMEM);
    }
#endif // _WIN64

    block = m_jitManager->allocGCInfo(m_CodeHeader,(DWORD)size, &m_GCinfo_len);
    if (!block)
    {
        COMPlusThrowHR(CORJIT_OUTOFMEM);
    }

    _ASSERTE(m_CodeHeader->GetGCInfo() != 0 && block == m_CodeHeader->GetGCInfo());

    EE_TO_JIT_TRANSITION();

    return block;
}

/*********************************************************************/
void CEEJitInfo::setEHcount (
        unsigned      cEH)
{
    CONTRACTL {
        SO_TOLERANT;
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    } CONTRACTL_END;

    JIT_TO_EE_TRANSITION();

    _ASSERTE(cEH != 0);
    _ASSERTE(m_CodeHeader != 0);
    _ASSERTE(m_CodeHeader->GetEHInfo() == 0);

    EE_ILEXCEPTION* ret;
    ret = m_jitManager->allocEHInfo(m_CodeHeader,cEH, &m_EHinfo_len);
    _ASSERTE(ret);      // allocEHInfo throws if there's not enough memory

    _ASSERTE(m_CodeHeader->GetEHInfo() != 0 && m_CodeHeader->GetEHInfo()->EHCount() == cEH);

    EE_TO_JIT_TRANSITION();
}

/*********************************************************************/
void CEEJitInfo::setEHinfo (
        unsigned      EHnumber,
        const CORINFO_EH_CLAUSE* clause)
{
    CONTRACTL {
        SO_TOLERANT;
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    } CONTRACTL_END;

    JIT_TO_EE_TRANSITION();

    // <REVISIT_TODO> Fix make the Code Manager EH clauses EH_INFO+</REVISIT_TODO>
    _ASSERTE(m_CodeHeader->GetEHInfo() != 0 && EHnumber < m_CodeHeader->GetEHInfo()->EHCount());

    EE_ILEXCEPTION_CLAUSE* pEHClause = m_CodeHeader->GetEHInfo()->EHClause(EHnumber);

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
        SO_TOLERANT;
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
#endif // CROSSGEN_COMPILE

#ifdef CROSSGEN_COMPILE
EXTERN_C ICorJitCompiler* __stdcall getJit();
#endif

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
    STATIC_CONTRACT_SO_INTOLERANT;

    SString namespaceOrClassName, methodName, methodSignature;
    // Fire an ETW event to mark the beginning of JIT'ing
    ETW::MethodLog::MethodJitting(reinterpret_cast<MethodDesc*>(info->ftn), &namespaceOrClassName, &methodName, &methodSignature);

    CorJitResult ret = jitMgr->m_jit->compileMethod(comp, info, flags, nativeEntry, nativeSizeOfCode);

    // Logically, it would seem that the end-of-JITting ETW even should go here, but it must come after the native code has been
    // set for the given method desc, which happens in a caller.

    return ret;
}
#endif // FEATURE_INTERPRETER

//
// Helper function because can't have dtors in BEGIN_SO_TOLERANT_CODE
// flags2 is not passed on to the JIT (yet) through the JITInterface.
// It is extra flags that can be passed on within the VM.
//
CorJitResult invokeCompileMethodHelper(EEJitManager *jitMgr,
                                 CEEInfo *comp,
                                 struct CORINFO_METHOD_INFO *info,
                                 unsigned flags,
                                 unsigned flags2,
                                 BYTE **nativeEntry,
                                 ULONG *nativeSizeOfCode)
{
    STATIC_CONTRACT_THROWS;
    STATIC_CONTRACT_GC_TRIGGERS;
    STATIC_CONTRACT_MODE_PREEMPTIVE;
    STATIC_CONTRACT_SO_INTOLERANT;

    CorJitResult ret = CORJIT_SKIPPED;   // Note that CORJIT_SKIPPED is an error exit status code

#ifdef FEATURE_STACK_SAMPLING
    // SO_INTOLERANT due to init affecting global state.
    static ConfigDWORD s_stackSamplingEnabled;
    bool samplingEnabled = (s_stackSamplingEnabled.val(CLRConfig::UNSUPPORTED_StackSamplingEnabled) != 0);
#endif

    BEGIN_SO_TOLERANT_CODE(GetThread());

#ifdef CROSSGEN_COMPILE
    ret = getJit()->compileMethod( comp,
                                   info,
                                   flags,
                                   nativeEntry,
                                   nativeSizeOfCode);

#else // CROSSGEN_COMPILE

#ifdef ALLOW_SXS_JIT
    if (FAILED(ret) && jitMgr->m_alternateJit
#ifdef FEATURE_STACK_SAMPLING
        && (!samplingEnabled || (flags2 & CORJIT_FLG2_SAMPLING_JIT_BACKGROUND))
#endif
       )
    {
        ret = jitMgr->m_alternateJit->compileMethod( comp,
                                                     info,
                                                     flags,
                                                     nativeEntry,
                                                     nativeSizeOfCode );

#ifdef FEATURE_STACK_SAMPLING
        if (flags2 & CORJIT_FLG2_SAMPLING_JIT_BACKGROUND)
        {
            // Don't bother with failures if we couldn't collect a trace.
            ret = CORJIT_OK;
        }
#endif // FEATURE_STACK_SAMPLING

        // If we failed to jit, then fall back to the primary Jit.
        if (FAILED(ret))
        {
            // Consider adding this call:
            //      ((CEEJitInfo*)comp)->BackoutJitData(jitMgr);
            ((CEEJitInfo*)comp)->ResetForJitRetry();
            ret = CORJIT_SKIPPED;
        }
    }
#endif // ALLOW_SXS_JIT

#ifdef FEATURE_INTERPRETER
    static ConfigDWORD s_InterpreterFallback;

    bool interpreterFallback = (s_InterpreterFallback.val(CLRConfig::INTERNAL_InterpreterFallback) != 0);

    if (interpreterFallback == false)
    {
        // If we're doing an "import_only" compilation, it's for verification, so don't interpret.
        // (We assume that importation is completely architecture-independent, or at least nearly so.)
        if (FAILED(ret) && (flags & (CORJIT_FLG_IMPORT_ONLY | CORJIT_FLG_MAKEFINALCODE)) == 0)
        {
            ret = Interpreter::GenerateInterpreterStub(comp, info, nativeEntry, nativeSizeOfCode);
        }
    }
    
    if (FAILED(ret) && jitMgr->m_jit)
    {
        ret = CompileMethodWithEtwWrapper(jitMgr, 
                                          comp,
                                          info,
                                          flags,
                                          nativeEntry,
                                          nativeSizeOfCode);
    }

    if (interpreterFallback == true)
    {
        // If we're doing an "import_only" compilation, it's for verification, so don't interpret.
        // (We assume that importation is completely architecture-independent, or at least nearly so.)
        if (FAILED(ret) && (flags & (CORJIT_FLG_IMPORT_ONLY | CORJIT_FLG_MAKEFINALCODE)) == 0)
        {
            ret = Interpreter::GenerateInterpreterStub(comp, info, nativeEntry, nativeSizeOfCode);
        }
    }
#else
    if (FAILED(ret))
    {
        ret = jitMgr->m_jit->compileMethod( comp,
                                            info,
                                            flags,
                                            nativeEntry,
                                            nativeSizeOfCode);
    }
#endif // FEATURE_INTERPRETER

    // Cleanup any internal data structures allocated 
    // such as IL code after a successfull JIT compile
    // If the JIT fails we keep the IL around and will
    // try reJIT the same IL.  VSW 525059
    //
    if (SUCCEEDED(ret) && !(flags & CORJIT_FLG_IMPORT_ONLY) && !((CEEJitInfo*)comp)->JitAgain())
    {
        ((CEEJitInfo*)comp)->CompressDebugInfo();

#ifdef FEATURE_INTERPRETER
        // We do this cleanup in the prestub, where we know whether the method
        // has been interpreted.
#else
        comp->MethodCompileComplete(info->ftn);
#endif // FEATURE_INTERPRETER
    }

#endif // CROSSGEN_COMPILE

    END_SO_TOLERANT_CODE;

    return ret;
}


/*********************************************************************/
CorJitResult invokeCompileMethod(EEJitManager *jitMgr,
                                 CEEInfo *comp,
                                 struct CORINFO_METHOD_INFO *info,
                                 unsigned flags,
                                 unsigned flags2,
                                 BYTE **nativeEntry,
                                 ULONG *nativeSizeOfCode)
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

    CorJitResult ret = invokeCompileMethodHelper(jitMgr, comp, info, flags, flags2, nativeEntry, nativeSizeOfCode);

    //
    // Verify that we are still in preemptive mode when we return
    // from the JIT
    //

    _ASSERTE(GetThread()->PreemptiveGCDisabled() == FALSE);

    return ret;
}

CorJitFlag GetCompileFlagsIfGenericInstantiation(
        CORINFO_METHOD_HANDLE method,
        CorJitFlag compileFlags,
        ICorJitInfo * pCorJitInfo,
        BOOL * raiseVerificationException,
        BOOL * unverifiableGenericCode);

CorJitResult CallCompileMethodWithSEHWrapper(EEJitManager *jitMgr,
                                CEEInfo *comp,
                                struct CORINFO_METHOD_INFO *info,
                                unsigned flags,
                                unsigned flags2,
                                BYTE **nativeEntry,
                                ULONG *nativeSizeOfCode,
                                MethodDesc *ftn)
{
    // no dynamic contract here because SEH is used, with a finally clause
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_TRIGGERS;

    LOG((LF_CORDB, LL_EVERYTHING, "CallCompileMethodWithSEHWrapper called...\n"));

    struct Param
    {
        EEJitManager *jitMgr;
        CEEInfo *comp;
        struct CORINFO_METHOD_INFO *info;
        unsigned flags;
        unsigned flags2;
        BYTE **nativeEntry;
        ULONG *nativeSizeOfCode;
        MethodDesc *ftn;
        CorJitResult res;
    }; Param param;
    param.jitMgr = jitMgr;
    param.comp = comp;
    param.info = info;
    param.flags = flags;
    param.flags2 = flags2;
    param.nativeEntry = nativeEntry;
    param.nativeSizeOfCode = nativeSizeOfCode;
    param.ftn = ftn;
    param.res = CORJIT_INTERNALERROR;

    PAL_TRY(Param *, pParam, &param)
    {
        //
        // Call out to the JIT-compiler
        //

        pParam->res = invokeCompileMethod( pParam->jitMgr,
                                           pParam->comp,
                                           pParam->info,
                                           pParam->flags,
                                           pParam->flags2,
                                           pParam->nativeEntry,
                                           pParam->nativeSizeOfCode);
    }
    PAL_FINALLY
    {
#if defined(DEBUGGING_SUPPORTED) && !defined(CROSSGEN_COMPILE)
        if (!(flags & (CORJIT_FLG_IMPORT_ONLY | CORJIT_FLG_MCJIT_BACKGROUND))
#ifdef FEATURE_STACK_SAMPLING
            && !(flags2 & CORJIT_FLG2_SAMPLING_JIT_BACKGROUND)
#endif // FEATURE_STACK_SAMPLING
           )
        {
            //
            // Notify the debugger that we have successfully jitted the function
            //
            if (ftn->HasNativeCode())
            {
                //
                // Nothing to do here (don't need to notify the debugger
                // because the function has already been successfully jitted)
                //
                // This is the case where we aborted the jit because of a deadlock cycle
                // in initClass.  
                //
            }
            else
            {
                if (g_pDebugInterface)
                {
                    if (param.res == CORJIT_OK && !((CEEJitInfo*)param.comp)->JitAgain())
                    {
                        g_pDebugInterface->JITComplete(ftn, (TADDR) *nativeEntry);
                    }
                }
            }
        }
#endif // DEBUGGING_SUPPORTED && !CROSSGEN_COMPILE
    }
    PAL_ENDTRY

    return param.res;
}

/*********************************************************************/
// Figures out the compile flags that are used by both JIT and NGen

/* static */ DWORD CEEInfo::GetBaseCompileFlags(MethodDesc * ftn)
{
     CONTRACTL {
        THROWS;
        GC_TRIGGERS;
    } CONTRACTL_END;

    //
    // Figure out the code quality flags
    //

    DWORD flags = 0;
    if (g_pConfig->JitFramed())
        flags |= CORJIT_FLG_FRAMED;
    if (g_pConfig->JitAlignLoops())
        flags |= CORJIT_FLG_ALIGN_LOOPS;
    if (ReJitManager::IsReJITEnabled() || g_pConfig->AddRejitNops())
        flags |= CORJIT_FLG_PROF_REJIT_NOPS;
#ifdef _TARGET_X86_
    if (g_pConfig->PInvokeRestoreEsp(ftn->GetModule()->IsPreV4Assembly()))
        flags |= CORJIT_FLG_PINVOKE_RESTORE_ESP;
#endif // _TARGET_X86_

    //See if we should instruct the JIT to emit calls to JIT_PollGC for thread suspension.  If we have a
    //non-default value in the EE Config, then use that.  Otherwise select the platform specific default.
#ifdef FEATURE_ENABLE_GCPOLL
    EEConfig::GCPollType pollType = g_pConfig->GetGCPollType();
    if (EEConfig::GCPOLL_TYPE_POLL == pollType)
        flags |= CORJIT_FLG_GCPOLL_CALLS;
    else if (EEConfig::GCPOLL_TYPE_INLINE == pollType)
        flags |= CORJIT_FLG_GCPOLL_INLINE;
#endif //FEATURE_ENABLE_GCPOLL

    // Set flags based on method's ImplFlags.
    if (!ftn->IsNoMetadata())
    {
         DWORD dwImplFlags = 0;
         IfFailThrow(ftn->GetMDImport()->GetMethodImplProps(ftn->GetMemberDef(), NULL, &dwImplFlags));
        
         if (IsMiNoOptimization(dwImplFlags))
         {
             flags |= CORJIT_FLG_MIN_OPT;
         }

         // Always emit frames for methods marked no-inline (see #define ETW_EBP_FRAMED in the JIT)
         if (IsMiNoInlining(dwImplFlags))
         {
             flags |= CORJIT_FLG_FRAMED;
         }
    }

    return flags;
}

/*********************************************************************/
// Figures out (some of) the flags to use to compile the method
// Returns the new set to use

DWORD GetDebuggerCompileFlags(Module* pModule, DWORD flags)
{
    STANDARD_VM_CONTRACT;

#ifdef FEATURE_CORECLR
    //Right now if we don't have a debug interface on CoreCLR, we can't generate debug info.  So, in those
    //cases don't attempt it.
    if (!g_pDebugInterface)
        return flags;
#endif //FEATURE_CORECLR

#ifdef DEBUGGING_SUPPORTED

#ifdef _DEBUG
    if (g_pConfig->GenDebuggableCode())
        flags |= CORJIT_FLG_DEBUG_CODE;
#endif // _DEBUG

#ifdef EnC_SUPPORTED
    if (pModule->IsEditAndContinueEnabled())
    {
        flags |= CORJIT_FLG_DEBUG_EnC;
    }
#endif // EnC_SUPPORTED

    // Debug info is always tracked
    flags |= CORJIT_FLG_DEBUG_INFO;
#endif // DEBUGGING_SUPPORTED

    if (CORDisableJITOptimizations(pModule->GetDebuggerInfoBits()))
    {
        flags |= CORJIT_FLG_DEBUG_CODE;
    }

    if (flags & CORJIT_FLG_IMPORT_ONLY)
    {
        // If we are only verifying the method, dont need any debug info and this
        // prevents getVars()/getBoundaries() from being called unnecessarily.
        flags &= ~(CORJIT_FLG_DEBUG_INFO|CORJIT_FLG_DEBUG_CODE);
    }

    return flags;
}

CorJitFlag GetCompileFlags(MethodDesc * ftn, DWORD flags, CORINFO_METHOD_INFO * methodInfo)
{
    STANDARD_VM_CONTRACT;

    _ASSERTE(methodInfo->regionKind ==  CORINFO_REGION_JIT);

    //
    // Get the compile flags that are shared between JIT and NGen
    //
    flags |= CEEInfo::GetBaseCompileFlags(ftn);

    //
    // Get CPU specific flags
    //
    if ((flags & CORJIT_FLG_IMPORT_ONLY) == 0)
    {
        flags |= ExecutionManager::GetEEJitManager()->GetCPUCompileFlags();
    }

    //
    // Find the debugger and profiler related flags
    //

#ifdef DEBUGGING_SUPPORTED
    flags |= GetDebuggerCompileFlags(ftn->GetModule(), flags);
#endif

#ifdef PROFILING_SUPPORTED
    if (CORProfilerTrackEnterLeave()
        && !ftn->IsNoMetadata()
       )
        flags |= CORJIT_FLG_PROF_ENTERLEAVE;

    if (CORProfilerTrackTransitions())
        flags |= CORJIT_FLG_PROF_NO_PINVOKE_INLINE;
#endif // PROFILING_SUPPORTED

    // Set optimization flags
    if (0 == (flags & CORJIT_FLG_MIN_OPT))
    {
        unsigned optType = g_pConfig->GenOptimizeType();
        _ASSERTE(optType <= OPT_RANDOM);

        if (optType == OPT_RANDOM)
            optType = methodInfo->ILCodeSize % OPT_RANDOM;

        if (g_pConfig->JitMinOpts())
            flags |= CORJIT_FLG_MIN_OPT;

        const static unsigned optTypeFlags[] =
        {
            0,                      // OPT_BLENDED
            CORJIT_FLG_SIZE_OPT,    // OPT_CODE_SIZE
            CORJIT_FLG_SPEED_OPT    // OPT_CODE_SPEED
        };

        _ASSERTE(optType < OPT_RANDOM);
        _ASSERTE((sizeof(optTypeFlags)/sizeof(optTypeFlags[0])) == OPT_RANDOM);
        flags |= optTypeFlags[optType];
    }

    //
    // Verification flags
    //

#ifdef _DEBUG
    if (g_pConfig->IsJitVerificationDisabled())
        flags |= CORJIT_FLG_SKIP_VERIFICATION;
#endif // _DEBUG

    if ((flags & CORJIT_FLG_IMPORT_ONLY) == 0 && 
        Security::CanSkipVerification(ftn))
        flags |= CORJIT_FLG_SKIP_VERIFICATION;

    if (ftn->IsILStub())
    {
        flags |= CORJIT_FLG_SKIP_VERIFICATION;

        // no debug info available for IL stubs
        flags &= ~CORJIT_FLG_DEBUG_INFO;
    }

    return (CorJitFlag)flags;
}

#if defined(_WIN64)
//The implementation of Jit64 prevents it from both inlining and verifying at the same time.  This causes a
//perf problem for code that adopts Transparency.  This code attempts to enable inlining in spite of that
//limitation in that scenario.
//
//This only works for real methods.  If the method isn't IsIL, then IsVerifiable will AV.  That would be a
//bad thing (TM).
BOOL IsTransparentMethodSafeToSkipVerification(CorJitFlag flags, MethodDesc * ftn)
{
    STANDARD_VM_CONTRACT;

    BOOL ret = FALSE;
    if (!(flags & CORJIT_FLG_IMPORT_ONLY) && !(flags & CORJIT_FLG_SKIP_VERIFICATION)
           && Security::IsMethodTransparent(ftn) &&
               ((ftn->IsIL() && !ftn->IsUnboxingStub()) ||
                   (ftn->IsDynamicMethod() && !ftn->IsILStub())))
    {
        EX_TRY
        {
            //Verify the method
            ret = ftn->IsVerifiable();
        }
        EX_CATCH
        {
            //If the jit throws an exception, do not let it leak out of here.  For example, we can sometimes
            //get an IPE that we could recover from in the Jit (i.e. invalid local in a method with skip
            //verification).
        }
        EX_END_CATCH(RethrowTerminalExceptions)
    }
    return ret;
}
#else
#define IsTransparentMethodSafeToSkipVerification(flags,ftn) (FALSE)
#endif //_WIN64

/*********************************************************************/
// We verify generic code once and for all using the typical open type,
// and then no instantiations need to be verified.  If verification
// failed, then we need to throw an exception whenever we try
// to compile a real instantiation

CorJitFlag GetCompileFlagsIfGenericInstantiation(
        CORINFO_METHOD_HANDLE method,
        CorJitFlag compileFlags,
        ICorJitInfo * pCorJitInfo,
        BOOL * raiseVerificationException,
        BOOL * unverifiableGenericCode)
{
    STANDARD_VM_CONTRACT;

    *raiseVerificationException = FALSE;
    *unverifiableGenericCode = FALSE;

    // If we have already decided to skip verification, keep on going.
    if (compileFlags & CORJIT_FLG_SKIP_VERIFICATION)
        return compileFlags;

    CorInfoInstantiationVerification ver = pCorJitInfo->isInstantiationOfVerifiedGeneric(method);

    switch(ver)
    {
    case INSTVER_NOT_INSTANTIATION:
        // Non-generic, or open instantiation of a generic type/method
        if (IsTransparentMethodSafeToSkipVerification(compileFlags, (MethodDesc*)method))
            compileFlags = (CorJitFlag)(compileFlags | CORJIT_FLG_SKIP_VERIFICATION);
        return compileFlags;

    case INSTVER_GENERIC_PASSED_VERIFICATION:
        // If the typical instantiation is verifiable, there is no need
        // to verify the concrete instantiations
        return (CorJitFlag)(compileFlags | CORJIT_FLG_SKIP_VERIFICATION);

    case INSTVER_GENERIC_FAILED_VERIFICATION:

        *unverifiableGenericCode = TRUE;

        // The generic method is not verifiable.
        // Check if it has SkipVerification permission
        MethodDesc * pGenMethod = GetMethod(method)->LoadTypicalMethodDefinition();

        CORINFO_METHOD_HANDLE genMethodHandle = CORINFO_METHOD_HANDLE(pGenMethod);

        CorInfoCanSkipVerificationResult canSkipVer;
        canSkipVer = pCorJitInfo->canSkipMethodVerification(genMethodHandle);
        
        switch(canSkipVer)
        {

#ifdef FEATURE_PREJIT
            case CORINFO_VERIFICATION_DONT_JIT:
            {
                // Transparent code could be partial trust, but we don't know at NGEN time.
                // This is the flag that NGEN passes to the JIT to tell it to give-up if it
                // hits unverifiable code.  Since we've already hit unverifiable code,
                // there's no point in starting the JIT, just to have it give up, so we
                // give up here.
                _ASSERTE(compileFlags & CORJIT_FLG_PREJIT);
                *raiseVerificationException = TRUE;
                return (CorJitFlag)-1; // This value will not be used
            }
#else // FEATURE_PREJIT
            // Need to have this case here to keep the MAC build happy
            case CORINFO_VERIFICATION_DONT_JIT:
            {
                _ASSERTE(!"We should never get here");
                return compileFlags;
            }
#endif // FEATURE_PREJIT

            case CORINFO_VERIFICATION_CANNOT_SKIP:
            {
                // For unverifiable generic code without SkipVerification permission,
                // we cannot ask the compiler to emit CORINFO_HELP_VERIFICATION in
                // unverifiable branches as the compiler cannot determine the unverifiable
                // branches while compiling the concrete instantiation. Instead,
                // just throw a VerificationException right away.
                *raiseVerificationException = TRUE;
                return (CorJitFlag)-1; // This value will not be used
            }

            case CORINFO_VERIFICATION_CAN_SKIP:
            {
                return (CorJitFlag)(compileFlags | CORJIT_FLG_SKIP_VERIFICATION);
            }

            case CORINFO_VERIFICATION_RUNTIME_CHECK:
            {
                // Compile the method without CORJIT_FLG_SKIP_VERIFICATION.
                // The compiler will know to add a call to
                // CORINFO_HELP_VERIFICATION_RUNTIME_CHECK, and then to skip verification.
                return compileFlags;
            }
        }
    }

    _ASSERTE(!"We should never get here");
    return compileFlags;
}

// ********************************************************************

// Throw the right type of exception for the given JIT result

void ThrowExceptionForJit(HRESULT res)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        SO_INTOLERANT;
        MODE_ANY;
    }
    CONTRACTL_END;
    switch (res)
    {
        case CORJIT_OUTOFMEM:
            COMPlusThrowOM();              
            break; 
            
#ifdef _TARGET_X86_
        // Currently, only x86 JIT returns adequate error codes. The x86 JIT is also the
        // JIT that has more limitations and given that to get this message for 64 bit
        // is going to require some code churn (either changing their EH handlers or
        // fixing the 3 or 4 code sites they have that return CORJIT_INTERNALERROR independently
        // of the error, the least risk fix is making this x86 only.
        case CORJIT_INTERNALERROR:
            COMPlusThrow(kInvalidProgramException, (UINT) IDS_EE_JIT_COMPILER_ERROR);
            break;   
#endif

        case CORJIT_BADCODE:
        default:                    
            COMPlusThrow(kInvalidProgramException);                                            
            break;
    }
 }

// ********************************************************************
#ifdef _DEBUG
LONG g_JitCount = 0;
#endif

//#define PERF_TRACK_METHOD_JITTIMES
#ifdef _TARGET_AMD64_
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

PCODE UnsafeJitFunction(MethodDesc* ftn, COR_ILMETHOD_DECODER* ILHeader,
                        DWORD flags, DWORD flags2, ULONG * pSizeOfCode)
{
    STANDARD_VM_CONTRACT;

    PCODE ret = NULL;

    COOPERATIVE_TRANSITION_BEGIN();

#ifdef FEATURE_PREJIT

    if (g_pConfig->RequireZaps() == EEConfig::REQUIRE_ZAPS_ALL &&
        ftn->GetModule()->GetDomainFile()->IsZapRequired() &&
        PartialNGenStressPercentage() == 0 && 
#ifdef FEATURE_STACK_SAMPLING
        !(flags2 & CORJIT_FLG2_SAMPLING_JIT_BACKGROUND) &&
#endif
        !(flags & CORJIT_FLG_IMPORT_ONLY))
    {
        StackSString ss(SString::Ascii, "ZapRequire: JIT compiler invoked for ");
        TypeString::AppendMethodInternal(ss, ftn);

#ifdef _DEBUG
        // Assert as some test may not check their error codes well. So throwing an
        // exception may not cause a test failure (as it should).
        StackScratchBuffer scratch;
        DbgAssertDialog(__FILE__, __LINE__, (char*)ss.GetUTF8(scratch));
#endif // _DEBUG

        COMPlusThrowNonLocalized(kFileNotFoundException, ss.GetUnicode());
    }

#endif // FEATURE_PREJIT

#ifndef CROSSGEN_COMPILE
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
#endif // CROSSGEN_COMPILE

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

        LOG((LF_JIT, LL_INFO10000, "{ Jitting method (%p) %S %s\n", ftn, methodString.GetUnicode(), ftn->m_pszDebugMethodSignature));
    }

#if 0
    if (!SString::_stricmp(cls,"ENC") &&
       (!SString::_stricmp(name,"G")))
    {
       static count = 0;
       count++;
       if (count > 0)
            DebugBreak();
    }
#endif // 0
#endif // _DEBUG

    CORINFO_METHOD_HANDLE ftnHnd = (CORINFO_METHOD_HANDLE)ftn;
    CORINFO_METHOD_INFO methodInfo;

    getMethodInfoHelper(ftn, ftnHnd, ILHeader, &methodInfo);

    // If it's generic then we can only enter through an instantiated md (unless we're just verifying it)
    _ASSERTE((flags & CORJIT_FLG_IMPORT_ONLY) != 0 || !ftn->IsGenericMethodDefinition());

    // If it's an instance method then it must not be entered from a generic class
    _ASSERTE((flags & CORJIT_FLG_IMPORT_ONLY) != 0 || ftn->IsStatic() ||
             ftn->GetNumGenericClassArgs() == 0 || ftn->HasClassInstantiation());

    // method attributes and signature are consistant
    _ASSERTE(!!ftn->IsStatic() == ((methodInfo.args.callConv & CORINFO_CALLCONV_HASTHIS) == 0));

    flags = GetCompileFlags(ftn, flags, &methodInfo);

#ifdef _DEBUG
    if (!(flags & CORJIT_FLG_SKIP_VERIFICATION))
    {
        SString methodString;
        if (LoggingOn(LF_VERIFIER, LL_INFO100))
            TypeString::AppendMethodDebug(methodString, ftn);

        LOG((LF_VERIFIER, LL_INFO100, "{ Will verify method (%p) %S %s\n", ftn, methodString.GetUnicode(), ftn->m_pszDebugMethodSignature));
    }
#endif //_DEBUG

#ifdef _TARGET_AMD64_
    BOOL fForceRel32Overflow = FALSE;

#ifdef _DEBUG
    // Always exercise the overflow codepath with force relocs
    if (PEDecoder::GetForceRelocs())
        fForceRel32Overflow = TRUE;
#endif

    BOOL fAllowRel32 = g_fAllowRel32 | fForceRel32Overflow;

    // For determinism, never try to use the REL32 in compilation process
    if (IsCompilationProcess())
    {
        fForceRel32Overflow = FALSE;
        fAllowRel32 = FALSE;
    }
#endif // _TARGET_AMD64_

    for (;;)
    {
#ifndef CROSSGEN_COMPILE
        CEEJitInfo jitInfo(ftn, ILHeader, jitMgr, (flags & CORJIT_FLG_IMPORT_ONLY) != 0);
#else
        // This path should be only ever used for verification in crossgen and so we should not need EEJitManager
        _ASSERTE((flags & CORJIT_FLG_IMPORT_ONLY) != 0);
        CEEInfo jitInfo(ftn, true);
        EEJitManager *jitMgr = NULL;
#endif

#if defined(_TARGET_AMD64_) && !defined(CROSSGEN_COMPILE)
        if (fForceRel32Overflow)
            jitInfo.SetRel32Overflow(fAllowRel32);
        jitInfo.SetAllowRel32(fAllowRel32);
#endif

        MethodDesc * pMethodForSecurity = jitInfo.GetMethodForSecurity(ftnHnd);

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

            StaticAccessCheckContext accessContext(pMethodForSecurity, ownerTypeForSecurity.GetMethodTable());

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
                                        accessCheckOptions,
                                        TRUE /*Check method transparency*/,
                                        TRUE /*Check type transparency*/))
            {
                EX_THROW(EEMethodException, (pMethodForSecurity));
            }
        }

        BOOL raiseVerificationException, unverifiableGenericCode;

        flags = GetCompileFlagsIfGenericInstantiation(
                    ftnHnd,
                    (CorJitFlag)flags,
                    &jitInfo,
                    &raiseVerificationException, 
                    &unverifiableGenericCode);

        if (raiseVerificationException)
            COMPlusThrow(kVerificationException);

        CorJitResult res;
        PBYTE nativeEntry;
        ULONG sizeOfCode;

        {
            GCX_COOP();

            /* There is a double indirection to call compileMethod  - can we
               improve this with the new structure? */

#ifdef PERF_TRACK_METHOD_JITTIMES
            //Because we're not calling QPC enough.  I'm not going to track times if we're just importing.
            LARGE_INTEGER methodJitTimeStart = {0};
            if (!(flags & CORJIT_FLG_IMPORT_ONLY))
                QueryPerformanceCounter (&methodJitTimeStart);

#endif
#if defined(ENABLE_PERF_COUNTERS)
            START_JIT_PERF();
#endif

#if defined(ENABLE_PERF_COUNTERS)
            LARGE_INTEGER CycleStart;
            QueryPerformanceCounter (&CycleStart);
#endif // defined(ENABLE_PERF_COUNTERS)

            // Note on debuggerTrackInfo arg: if we're only importing (ie, verifying/
            // checking to make sure we could JIT, but not actually generating code (
            // eg, for inlining), then DON'T TELL THE DEBUGGER about this.
            res = CallCompileMethodWithSEHWrapper(jitMgr,
                                                  &jitInfo,
                                                  &methodInfo,
                                                  flags,
                                                  flags2,
                                                  &nativeEntry,
                                                  &sizeOfCode,
                                                  (MethodDesc*)ftn);
            LOG((LF_CORDB, LL_EVERYTHING, "Got through CallCompile MethodWithSEHWrapper\n"));

#if FEATURE_PERFMAP
            // Save the code size so that it can be reported to the perfmap.
            if (pSizeOfCode != NULL)
            {
                *pSizeOfCode = sizeOfCode;
            }
#endif

#if defined(ENABLE_PERF_COUNTERS)
            LARGE_INTEGER CycleStop;
            QueryPerformanceCounter(&CycleStop);
            GetPerfCounters().m_Jit.timeInJitBase = GetPerfCounters().m_Jit.timeInJit;
            GetPerfCounters().m_Jit.timeInJit += static_cast<DWORD>(CycleStop.QuadPart - CycleStart.QuadPart);
            GetPerfCounters().m_Jit.cMethodsJitted++;
            GetPerfCounters().m_Jit.cbILJitted+=methodInfo.ILCodeSize;

#endif // defined(ENABLE_PERF_COUNTERS)

#if defined(ENABLE_PERF_COUNTERS)
            STOP_JIT_PERF();
#endif

#ifdef PERF_TRACK_METHOD_JITTIMES
            //store the time in the string buffer.  Module name and token are unique enough.  Also, do not
            //capture importing time, just actual compilation time.
            if (!(flags & CORJIT_FLG_IMPORT_ONLY))
            {
                LARGE_INTEGER methodJitTimeStop;
                QueryPerformanceCounter(&methodJitTimeStop);
                SString codeBase;
                ftn->GetModule()->GetDomainFile()->GetFile()->GetCodeBaseOrName(codeBase);
                codeBase.AppendPrintf(W(",0x%x,%d,%d\n"),
                                 //(const WCHAR *)codeBase, //module name
                                 ftn->GetMemberDef(), //method token
                                 (unsigned)(methodJitTimeStop.QuadPart - methodJitTimeStart.QuadPart), //cycle count
                                 methodInfo.ILCodeSize //il size
                                );
                WszOutputDebugString((const WCHAR*)codeBase);
            }
#endif // PERF_TRACK_METHOD_JITTIMES

        }

        LOG((LF_JIT, LL_INFO10000, "Done Jitting method %s::%s  %s }\n",cls,name, ftn->m_pszDebugMethodSignature));

        if (!SUCCEEDED(res))
        {
            COUNTER_ONLY(GetPerfCounters().m_Jit.cJitFailures++);

#ifndef CROSSGEN_COMPILE
            jitInfo.BackoutJitData(jitMgr);
#endif

            ThrowExceptionForJit(res);
        }

        if (flags & CORJIT_FLG_IMPORT_ONLY)
        {
            // The method must been processed by the verifier. Note that it may
            // either have been marked as verifiable or unverifiable.
            // ie. IsVerified() does not imply IsVerifiable()
            _ASSERTE(ftn->IsVerified());

            // We are done
            break;
        }

        if (!nativeEntry)
            COMPlusThrow(kInvalidProgramException);

#if defined(_TARGET_AMD64_) && !defined(CROSSGEN_COMPILE)
        if (jitInfo.IsRel32Overflow())
        {
            // Backout and try again with fAllowRel32 == FALSE.
            jitInfo.BackoutJitData(jitMgr);

            // Disallow rel32 relocs in future.
            g_fAllowRel32 = FALSE;

            _ASSERTE(fAllowRel32 != FALSE);
            fAllowRel32 = FALSE;
            continue;
        }
#endif // _TARGET_AMD64_ && !CROSSGEN_COMPILE

        LOG((LF_JIT, LL_INFO10000,
            "Jitted Entry at" FMT_ADDR "method %s::%s %s\n", DBG_ADDR(nativeEntry),
             ftn->m_pszDebugClassName, ftn->m_pszDebugMethodName, ftn->m_pszDebugMethodSignature));

#if defined(FEATURE_CORESYSTEM)

#ifdef _DEBUG
        LPCUTF8 pszDebugClassName = ftn->m_pszDebugClassName;
        LPCUTF8 pszDebugMethodName = ftn->m_pszDebugMethodName;
        LPCUTF8 pszDebugMethodSignature = ftn->m_pszDebugMethodSignature;
#else
        LPCUTF8 pszNamespace;
        LPCUTF8 pszDebugClassName = ftn->GetMethodTable()->GetFullyQualifiedNameInfo(&pszNamespace);
        LPCUTF8 pszDebugMethodName = ftn->GetName();
        LPCUTF8 pszDebugMethodSignature = "";
#endif

        //DbgPrintf("Jitted Entry at" FMT_ADDR "method %s::%s %s size %08x\n", DBG_ADDR(nativeEntry),
        //          pszDebugClassName, pszDebugMethodName, pszDebugMethodSignature, sizeOfCode);
#endif

        ClrFlushInstructionCache(nativeEntry, sizeOfCode); 
        ret = (PCODE)nativeEntry;

#ifdef _TARGET_ARM_
        ret |= THUMB_CODE;
#endif

        // We are done
        break;
    }

#ifdef _DEBUG
    FastInterlockIncrement(&g_JitCount);
    static BOOL fHeartbeat = -1;

    if (fHeartbeat == -1)
        fHeartbeat = CLRConfig::GetConfigValue(CLRConfig::INTERNAL_JitHeartbeat);

    if (fHeartbeat)
        printf(".");
#endif // _DEBUG

    COOPERATIVE_TRANSITION_END();
    return ret;
}

extern "C" unsigned __stdcall PartialNGenStressPercentage()
{
    LIMITED_METHOD_CONTRACT;
#ifndef _DEBUG 
    return 0;
#else // _DEBUG
    static ConfigDWORD partialNGenStress;
    DWORD partialNGenStressVal = partialNGenStress.val(CLRConfig::INTERNAL_partialNGenStress);
    _ASSERTE(partialNGenStressVal <= 100);
    return partialNGenStressVal;
#endif // _DEBUG
}

#ifdef FEATURE_PREJIT
/*********************************************************************/

//
// Table loading functions
//
void Module::LoadHelperTable()
{
    STANDARD_VM_CONTRACT;

#ifndef CROSSGEN_COMPILE
    COUNT_T tableSize;
    BYTE * table = (BYTE *) GetNativeImage()->GetNativeHelperTable(&tableSize);

    if (tableSize == 0)
        return;

    EnsureWritableExecutablePages(table, tableSize);

    BYTE * curEntry   = table;
    BYTE * tableEnd   = table + tableSize;

#ifdef LOGGING
    int iEntryNumber = 0;
#endif // LOGGING

    //
    // Fill in helpers
    //

    while (curEntry < tableEnd)
    {
        DWORD dwHelper = *(DWORD *)curEntry;

        int iHelper = (USHORT)dwHelper;
        _ASSERTE(iHelper < CORINFO_HELP_COUNT);

        LOG((LF_JIT, LL_INFO1000000, "JIT helper %3d (%-40s: table @ %p, size 0x%x, entry %3d @ %p, pfnHelper %p)\n",
            iHelper, hlpFuncTable[iHelper].name, table, tableSize, iEntryNumber, curEntry, hlpFuncTable[iHelper].pfnHelper));

#if defined(ENABLE_FAST_GCPOLL_HELPER)
        // The fast GC poll helper works by calling indirect through a pointer that points to either
        // JIT_PollGC or JIT_PollGC_Nop, based on whether we need to poll or not. The JIT_PollGC_Nop
        // version is just a "ret". The pointer is stored in hlpDynamicFuncTable[DYNAMIC_CORINFO_HELP_POLL_GC].
        // See EnableJitGCPoll() and DisableJitGCPoll().
        // In NGEN images, we generate a direct call to the helper table. Here, we replace that with
        // an indirect jump through the pointer in hlpDynamicFuncTable[DYNAMIC_CORINFO_HELP_POLL_GC].
        if (iHelper == CORINFO_HELP_POLL_GC)
        {
            LOG((LF_JIT, LL_INFO1000000, "JIT helper CORINFO_HELP_POLL_GC (%d); emitting indirect jump to 0x%x\n",
                CORINFO_HELP_POLL_GC, &hlpDynamicFuncTable[DYNAMIC_CORINFO_HELP_POLL_GC].pfnHelper));

            emitJumpInd(curEntry, &hlpDynamicFuncTable[DYNAMIC_CORINFO_HELP_POLL_GC].pfnHelper);
            curEntry = curEntry + HELPER_TABLE_ENTRY_LEN;
        }
        else
#endif // ENABLE_FAST_GCPOLL_HELPER
        {
            PCODE pfnHelper = CEEJitInfo::getHelperFtnStatic((CorInfoHelpFunc)iHelper);

            if (dwHelper & CORCOMPILE_HELPER_PTR)
            {
                //
                // Indirection cell
                //

                *(TADDR *)curEntry = pfnHelper;

                curEntry = curEntry + sizeof(TADDR);
            }
            else
            {
                //
                // Jump thunk
                //

#if defined(_TARGET_AMD64_)
                *curEntry = X86_INSTR_JMP_REL32;
                *(INT32 *)(curEntry + 1) = rel32UsingJumpStub((INT32 *)(curEntry + 1), pfnHelper, NULL, GetLoaderAllocator());
#elif defined (_TARGET_ARM64_)
                _ASSERTE(!"ARM64:NYI");      
#else // all other platforms
                emitJump(curEntry, (LPVOID)pfnHelper);
                _ASSERTE(HELPER_TABLE_ENTRY_LEN >= JUMP_ALLOCATE_SIZE);
#endif

                curEntry = curEntry + HELPER_TABLE_ENTRY_LEN;
            }
        }
#ifdef LOGGING
        // Note that some table entries are sizeof(TADDR) in length, and some are HELPER_TABLE_ENTRY_LEN in length
        ++iEntryNumber;
#endif // LOGGING
    }

    ClrFlushInstructionCache(table, tableSize);
#endif // CROSSGEN_COMPILE
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
        size_t offset = cur->GetSeriesOffset() - sizeof(void*);
        size_t offsetStop = offset + cur->GetSeriesSize() + size;
        while (offset < offsetStop)
        {
            size_t bit = offset / sizeof(void *);

            size_t index = bit / 8;
            _ASSERTE(index < cbGCRefMap);
            pGCRefMap[index] |= (1 << (bit & 7));

            offset += sizeof(void *);
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
BOOL TypeLayoutCheck(MethodTable * pMT, PCCOR_SIGNATURE pBlob)
{
    STANDARD_VM_CONTRACT;

    SigPointer p(pBlob);
    IfFailThrow(p.SkipExactlyOne());

    DWORD dwFlags;
    IfFailThrow(p.GetData(&dwFlags));

    // Size is checked unconditionally
    DWORD dwExpectedSize;
    IfFailThrow(p.GetData(&dwExpectedSize));

    DWORD dwActualSize = pMT->GetNumInstanceFieldBytes();
    if (dwExpectedSize != dwActualSize)
        return FALSE;

#ifdef FEATURE_HFA
    if (dwFlags & READYTORUN_LAYOUT_HFA)
    {
        DWORD dwExpectedHFAType;
        IfFailThrow(p.GetData(&dwExpectedHFAType));

        DWORD dwActualHFAType = pMT->GetHFAType();
        if (dwExpectedHFAType != dwActualHFAType)
            return FALSE;
    }
    else
    {
        if (pMT->IsHFA())
            return FALSE;
    }
#else
    _ASSERTE(!(dwFlags & READYTORUN_LAYOUT_HFA));
#endif

    if (dwFlags & READYTORUN_LAYOUT_Alignment)
    {
        DWORD dwExpectedAlignment = sizeof(void *);
        if (!(dwFlags & READYTORUN_LAYOUT_Alignment_Native))
        {
            IfFailThrow(p.GetData(&dwExpectedAlignment));
        }

        DWORD dwActualAlignment = CEEInfo::getClassAlignmentRequirementStatic(pMT);
        if (dwExpectedAlignment != dwActualAlignment)
            return FALSE;

    }

    if (dwFlags & READYTORUN_LAYOUT_GCLayout)
    {
        if (dwFlags & READYTORUN_LAYOUT_GCLayout_Empty)
        {
            if (pMT->ContainsPointers())
                return FALSE;
        }
        else
        {
            size_t cbGCRefMap = (dwActualSize / sizeof(TADDR) + 7) / 8;
            _ASSERTE(cbGCRefMap > 0);

            BYTE * pGCRefMap = (BYTE *)_alloca(cbGCRefMap);

            ComputeGCRefMap(pMT, pGCRefMap, cbGCRefMap);

            if (memcmp(pGCRefMap, p.GetPtr(), cbGCRefMap) != 0)
                return FALSE;
        }
    }

    return TRUE;
}

#endif // FEATURE_READYTORUN

BOOL LoadDynamicInfoEntry(Module *currentModule,
                          RVA fixupRva,
                          SIZE_T *entry)
{
    STANDARD_VM_CONTRACT;

    PCCOR_SIGNATURE pBlob = currentModule->GetNativeFixupBlobData(fixupRva);

    BYTE kind = *pBlob++;

    Module * pInfoModule = currentModule;

    if (kind & ENCODE_MODULE_OVERRIDE)
    {
        pInfoModule = currentModule->GetModuleFromIndex(CorSigUncompressData(pBlob));
        kind &= ~ENCODE_MODULE_OVERRIDE;
    }

    MethodDesc * pMD = NULL;

    PCCOR_SIGNATURE pSig;
    DWORD cSig;

    mdSignature token;

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
                else
                {
#ifdef FEATURE_WINMD_RESILIENT
                    // We do not emit activation fixups for version resilient references. Activate the target explicitly.
                    th.AsMethodTable()->EnsureInstanceActive();
#endif
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

#ifndef CROSSGEN_COMPILE
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
                result = (size_t)StringObject::GetEmptyStringRefPtr();
            }
            else
            {
                CrstHolder ch(pInfoModule->GetFixupCrst());

                if (!CORCOMPILE_IS_POINTER_TAGGED(*entry) && (*entry != NULL))
                {
                    // We lost the race, just return
                    return TRUE;
                }

                // For generic instantiations compiled into the ngen image of some other
                // client assembly, we need to ensure that we intern the string
                // in the defining assembly.
                bool mayNeedToSyncWithFixups = pInfoModule != currentModule;

                result = (size_t) pInfoModule->ResolveStringRef(TokenFromRid(rid, mdtString), currentModule->GetDomain(), mayNeedToSyncWithFixups);
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
            token = TokenFromRid(
                        CorSigUncompressData(pBlob),
                        mdtMethodDef);

            IfFailThrow(pInfoModule->GetMDImport()->GetSigOfMethodDef(token, &cSig, &pSig));

        VarArgs:
            result = (size_t) CORINFO_VARARGS_HANDLE(currentModule->GetVASigCookie(Signature(pSig, cSig)));
        }
        break;

        // ENCODE_METHOD_NATIVECALLABLE_HANDLE is same as ENCODE_METHOD_ENTRY_DEF_TOKEN 
        // except for AddrOfCode
    case ENCODE_METHOD_NATIVE_ENTRY:
    case ENCODE_METHOD_ENTRY_DEF_TOKEN:
        {
            mdToken MethodDef = TokenFromRid(CorSigUncompressData(pBlob), mdtMethodDef);
            pMD = MemberLoader::GetMethodDescFromMethodDef(pInfoModule, MethodDef, FALSE);

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
            else
            {
#ifdef FEATURE_WINMD_RESILIENT
                // We do not emit activation fixups for version resilient references. Activate the target explicitly.
                pMD->EnsureActive();
#endif
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
            if (kind == ENCODE_METHOD_NATIVE_ENTRY)
            {
                result = COMDelegate::ConvertToCallback(pMD);
            }
            else
            {
                result = pMD->GetMultiCallableAddrOfCode(CORINFO_ACCESS_ANY);
            }

        #ifndef _TARGET_ARM_
            if (CORCOMPILE_IS_PCODE_TAGGED(result))
            {
                // There is a rare case where the function entrypoint may not be aligned. This could happen only for FCalls, 
                // only on x86 and only if we failed to hardbind the fcall (e.g. ngen image for mscorlib.dll does not exist 
                // and /nodependencies flag for ngen was used). The function entrypoints should be aligned in all other cases.
                //
                // We will wrap the unaligned method entrypoint by funcptr stub with aligned entrypoint.
                _ASSERTE(pMD->IsFCall());
                result = pMD->GetLoaderAllocator()->GetFuncPtrStubs()->GetFuncPtrStub(pMD);
            }
        #endif
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
            result = (size_t)(LPVOID)&(pMD->GetWriteableData()->m_pNDirectTarget);
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
                BEGIN_PIN_PROFILER(CORProfilerFunctionIDMapperEnabled());
                profilerHandle = (CORINFO_PROFILING_HANDLE) g_profControlBlock.pProfInterface->EEFunctionIDMapper(funId, &bHookFunction);
                END_PIN_PROFILER();
            }

            // Profiling handle is opaque token. It does not have to be aligned thus we can not store it in the same location as token.
            *EnsureWritablePages(entry+kZapProfilingHandleImportValueIndexClientData) = (SIZE_T)profilerHandle;

            if (bHookFunction)
            {
                *EnsureWritablePages(entry+kZapProfilingHandleImportValueIndexEnterAddr) = (SIZE_T)(void *)hlpDynamicFuncTable[DYNAMIC_CORINFO_HELP_PROF_FCN_ENTER].pfnHelper;
                *EnsureWritablePages(entry+kZapProfilingHandleImportValueIndexLeaveAddr) = (SIZE_T)(void *)hlpDynamicFuncTable[DYNAMIC_CORINFO_HELP_PROF_FCN_LEAVE].pfnHelper;
                *EnsureWritablePages(entry+kZapProfilingHandleImportValueIndexTailcallAddr) = (SIZE_T)(void *)hlpDynamicFuncTable[DYNAMIC_CORINFO_HELP_PROF_FCN_TAILCALL].pfnHelper;
            }
            else
            {
                *EnsureWritablePages(entry+kZapProfilingHandleImportValueIndexEnterAddr) = (SIZE_T)(void *)JIT_ProfilerEnterLeaveTailcallStub;
                *EnsureWritablePages(entry+kZapProfilingHandleImportValueIndexLeaveAddr) = (SIZE_T)(void *)JIT_ProfilerEnterLeaveTailcallStub;
                *EnsureWritablePages(entry+kZapProfilingHandleImportValueIndexTailcallAddr) = (SIZE_T)(void *)JIT_ProfilerEnterLeaveTailcallStub;
            }
        }
        break;
#endif // PROFILING_SUPPORTED

    case ENCODE_STATIC_FIELD_ADDRESS:
        {
            FieldDesc *pField = ZapSig::DecodeField(currentModule, pInfoModule, pBlob);

            pField->GetEnclosingMethodTable()->CheckRestore();

            // We can take address of RVA field only since ngened code is domain neutral
            _ASSERTE(pField->IsRVA());

            // Field address is not aligned thus we can not store it in the same location as token.
            *EnsureWritablePages(entry+1) = (size_t)pField->GetStaticAddressHandle(NULL);
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

    case ENCODE_MODULE_ID_FOR_STATICS:
        {
            result = pInfoModule->GetModuleID();
        }
        break;

    case ENCODE_MODULE_ID_FOR_GENERIC_STATICS:
        {
            TypeHandle th = ZapSig::DecodeType(currentModule, pInfoModule, pBlob);

            MethodTable * pMT = th.AsMethodTable();

            result = pMT->GetModuleForStatics()->GetModuleID();
        }
        break;

    case ENCODE_ACTIVE_DEPENDENCY:
        {
            Module* pModule = currentModule->GetModuleFromIndex(CorSigUncompressData(pBlob));

            STRESS_LOG3(LF_ZAP,LL_INFO10000,"Modules are: %08x,%08x,%08x",currentModule,pInfoModule,pModule);
            pInfoModule->AddActiveDependency(pModule, FALSE);
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
                        Module * pPrevious = InterlockedCompareExchangeT(EnsureWritablePages((Module **)entry), pInfoModule, NULL);
                        if (pPrevious != pInfoModule && pPrevious != NULL)
                            COMPlusThrowHR(COR_E_FILELOAD, IDS_NATIVE_IMAGE_CANNOT_BE_LOADED_MULTIPLE_TIMES, pInfoModule->GetPath());
                        return TRUE;
                    }
                    break;

                case READYTORUN_HELPER_GSCookie:
                    result = (size_t)GetProcessGSCookie();
                    break;

                case READYTORUN_HELPER_DelayLoad_MethodCall:
                    result = (size_t)DelayLoad_MethodCall;
                    break;

                case READYTORUN_HELPER_DelayLoad_Helper:
                    result = (size_t)DelayLoad_Helper;
                    break;

                case READYTORUN_HELPER_DelayLoad_Helper_Obj:
                    result = (size_t)DelayLoad_Helper_Obj;
                    break;

                case READYTORUN_HELPER_DelayLoad_Helper_ObjObj:
                    result = (size_t)DelayLoad_Helper_ObjObj;
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
        {
            TypeHandle th = ZapSig::DecodeType(currentModule, pInfoModule, pBlob);
            MethodTable * pMT = th.AsMethodTable();
            _ASSERTE(pMT->IsValueType());

            if (!TypeLayoutCheck(pMT, pBlob))
                return FALSE;

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
#endif // FEATURE_READYTORUN

#endif // CROSSGEN_COMPILE

    default:
        STRESS_LOG1(LF_ZAP, LL_WARNING, "Unknown FIXUP_BLOB_KIND %d\n", kind);
        _ASSERTE(!"Unknown FIXUP_BLOB_KIND");
        return FALSE;
    }

    MemoryBarrier();
    *EnsureWritablePages(entry) = result;

    return TRUE;
}
#endif // FEATURE_PREJIT

void* CEEInfo::getTailCallCopyArgsThunk(CORINFO_SIG_INFO       *pSig,
                                        CorInfoHelperTailCallSpecialHandling flags)
{
    CONTRACTL {
        SO_TOLERANT;
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    } CONTRACTL_END;

    void * ftn = NULL;

#if defined(_TARGET_AMD64_) || defined(_TARGET_ARM_)

    JIT_TO_EE_TRANSITION();

    Stub* pStub = CPUSTUBLINKER::CreateTailCallCopyArgsThunk(pSig, flags);
        
    ftn = (void*)pStub->GetEntryPoint();

    EE_TO_JIT_TRANSITION();

#endif // _TARGET_AMD64_ || _TARGET_ARM_

    return ftn;
}

void CEEInfo::allocMem (
        ULONG               hotCodeSize,    /* IN */
        ULONG               coldCodeSize,   /* IN */
        ULONG               roDataSize,     /* IN */
        ULONG               xcptnsCount,    /* IN */
        CorJitAllocMemFlag  flag,           /* IN */
        void **             hotCodeBlock,   /* OUT */
        void **             coldCodeBlock,  /* OUT */
        void **             roDataBlock     /* OUT */
        )
{
    LIMITED_METHOD_CONTRACT;
    UNREACHABLE();      // only called on derived class.
}

void CEEInfo::reserveUnwindInfo (
        BOOL                isFunclet,             /* IN */
        BOOL                isColdCode,            /* IN */
        ULONG               unwindSize             /* IN */
        )
{
    LIMITED_METHOD_CONTRACT;
    UNREACHABLE();      // only called on derived class.
}

void CEEInfo::allocUnwindInfo (
        BYTE *              pHotCode,              /* IN */
        BYTE *              pColdCode,             /* IN */
        ULONG               startOffset,           /* IN */
        ULONG               endOffset,             /* IN */
        ULONG               unwindSize,            /* IN */
        BYTE *              pUnwindBlock,          /* IN */
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
    _ASSERTE(isVerifyOnly());
    *ppValue = (void *)0x10;
    return IAT_PVALUE;
}

void* CEEInfo::getFieldAddress(CORINFO_FIELD_HANDLE fieldHnd,
                                  void **ppIndirection)
{
    LIMITED_METHOD_CONTRACT;
    _ASSERTE(isVerifyOnly());
    if (ppIndirection != NULL)
        *ppIndirection = NULL;
    return (void *)0x10;
}

void* CEEInfo::getMethodSync(CORINFO_METHOD_HANDLE ftnHnd,
                             void **ppIndirection)
{
    LIMITED_METHOD_CONTRACT;
    UNREACHABLE();      // only called on derived class.
}

HRESULT CEEInfo::allocBBProfileBuffer (
        ULONG                 count,           // The number of basic blocks that we have
        ProfileBuffer **      profileBuffer
        )
{
    LIMITED_METHOD_CONTRACT;
    UNREACHABLE_RET();      // only called on derived class.
}

HRESULT CEEInfo::getBBProfileData(
        CORINFO_METHOD_HANDLE ftnHnd,
        ULONG *               count,           // The number of basic blocks that we have
        ProfileBuffer **      profileBuffer,
        ULONG *               numRuns
        )
{
    LIMITED_METHOD_CONTRACT;
    UNREACHABLE_RET();      // only called on derived class.
}


void CEEInfo::recordCallSite(
        ULONG                 instrOffset,  /* IN */
        CORINFO_SIG_INFO *    callSig,      /* IN */
        CORINFO_METHOD_HANDLE methodHandle  /* IN */
        )
{
    LIMITED_METHOD_CONTRACT;
    UNREACHABLE();      // only called on derived class.
}

void CEEInfo::recordRelocation(
        void *                 location,   /* IN  */
        void *                 target,     /* IN  */
        WORD                   fRelocType, /* IN  */
        WORD                   slotNum,  /* IN  */
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

void CEEInfo::getModuleNativeEntryPointRange(
        void ** pStart, /* OUT */
        void ** pEnd    /* OUT */
        )
{
    LIMITED_METHOD_CONTRACT;
    UNREACHABLE();      // only called on derived class.
}

DWORD CEEInfo::getExpectedTargetArchitecture()
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

void* CEEInfo::getHelperFtn(CorInfoHelpFunc    ftnNum,         /* IN  */
                            void **            ppIndirection)  /* OUT */
{
    LIMITED_METHOD_CONTRACT;
    UNREACHABLE();      // only called on derived class.
}

// Active dependency helpers
void CEEInfo::addActiveDependency(CORINFO_MODULE_HANDLE moduleFrom,CORINFO_MODULE_HANDLE moduleTo)
{
    LIMITED_METHOD_CONTRACT;
    UNREACHABLE();      // only called on derived class.
}

void CEEInfo::GetProfilingHandle(BOOL                      *pbHookFunction,
                                 void                     **pProfilerHandle,
                                 BOOL                      *pbIndirectedHandles)
{
    LIMITED_METHOD_CONTRACT;
    UNREACHABLE();      // only called on derived class.
}

#endif // !DACCESS_COMPILE

EECodeInfo::EECodeInfo()
{
    WRAPPER_NO_CONTRACT;

    m_codeAddress = NULL;

    m_pJM = NULL;
    m_pMD = NULL;
    m_relOffset = 0;

#ifdef WIN64EXCEPTIONS
    m_pFunctionEntry = NULL;
#endif
}

void EECodeInfo::Init(PCODE codeAddress)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        SO_TOLERANT;
    } CONTRACTL_END;

    Init(codeAddress, ExecutionManager::GetScanFlags());
}

void EECodeInfo::Init(PCODE codeAddress, ExecutionManager::ScanFlag scanFlag)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        SO_TOLERANT;
    } CONTRACTL_END;

    m_codeAddress = codeAddress;

    RangeSection * pRS = ExecutionManager::FindCodeRange(codeAddress, scanFlag);
    if (pRS == NULL)
        goto Invalid;

    if (!pRS->pjit->JitCodeToMethodInfo(pRS, codeAddress, &m_pMD, this))
        goto Invalid;

    m_pJM = pRS->pjit;
    return;

Invalid:
    m_pJM = NULL;
    m_pMD = NULL;
    m_relOffset = 0;

#ifdef WIN64EXCEPTIONS
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
#ifndef _WIN64
#if defined(HAVE_GCCOVER)
    _ASSERTE (!m_pMD->m_GcCover || GCStress<cfg_instr>::IsEnabled());
    if (GCStress<cfg_instr>::IsEnabled()
        && m_pMD->m_GcCover)
    {
        _ASSERTE(m_pMD->m_GcCover->savedCode);

        // Make sure we return the TADDR of savedCode here.  The byte array is not marshaled automatically.
        // The caller is responsible for any necessary marshaling.
        return PTR_TO_MEMBER_TADDR(GCCoverageInfo, m_pMD->m_GcCover, savedCode);
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

#if defined(WIN64EXCEPTIONS)

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

#if defined(_TARGET_AMD64_)

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
#endif // defined(_TARGET_AMD64_)

#endif // defined(WIN64EXCEPTIONS)


#if defined(_TARGET_AMD64_)
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
    return reinterpret_cast<UNWIND_INFO *>(this->GetModuleBase() + unwindInfoOffset);
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
// Since this method is only used for the security stackwalk cache, this assumption is valid, since
// we cannot make a call in a prolog or an epilog.
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
    if (pInfo->Flags & UNW_FLAG_CHAININFO)
    {
        _ASSERTE(!"GetRbpOffset() - chained unwind info used, violating assumptions of the security stackwalk cache");
        DebugBreak();
    }

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
            // Note that the intial value of StackSize does not include the stack space used for pushes.
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
#ifdef FEATURE_PAL
        EECodeInfo codeInfo;
        codeInfo.Init((PCODE)pvFuncletStart);
        pFunctionEntry = codeInfo.GetFunctionEntry();
        uImageBase = (ULONGLONG)codeInfo.GetModuleBase();
#else // !FEATURE_PAL
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
#ifdef FEATURE_PREJIT
            // workaround: Check for indirect entry that is generated for cold part of main method body.
            if ((TADDR)pvFuncletStart < (TADDR)uImageBase + pFunctionEntry->BeginAddress ||
                (TADDR)uImageBase + pFunctionEntry->EndAddress <= (TADDR)pvFuncletStart)
            {
                Module * pZapModule = ExecutionManager::FindZapModule((TADDR)pvFuncletStart);
                NGenLayoutInfo * pLayoutInfo = pZapModule->GetNGenLayoutInfo();

                int ColdFunctionIndex = NativeUnwindInfoLookupTable::LookupUnwindInfoForMethod((DWORD)((TADDR)pvFuncletStart - uImageBase),
                                                                               pLayoutInfo->m_pRuntimeFunctions[2],
                                                                               0, pLayoutInfo->m_nRuntimeFunctions[2] - 1);

                pFunctionEntry = pLayoutInfo->m_pRuntimeFunctions[2] + ColdFunctionIndex;
            }
#endif

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
#endif // defined(_TARGET_AMD64_)

