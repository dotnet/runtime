//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//


#include "common.h"
#include "corhdr.h"
#include "runtimehandles.h"
#include "object.h"
#include "class.h"
#include "method.hpp"
#include "typehandle.h"
#include "field.h"
#include "siginfo.hpp"
#include "clsload.hpp"
#include "typestring.h"
#include "typeparse.h"
#include "holder.h"
#include "codeman.h"
#include "corhlpr.h"
#include "jitinterface.h"
#include "stackprobe.h"
#include "eeconfig.h"
#include "eehash.h"
#include "objecthandle.h"
#include "interoputil.h"
#include "typedesc.h"
#include "virtualcallstub.h"
#include "contractimpl.h"
#include "dynamicmethod.h"
#include "peimagelayout.inl"
#include "security.h"
#include "eventtrace.h"
#include "invokeutil.h"


FCIMPL3(FC_BOOL_RET, Utf8String::EqualsCaseSensitive, LPCUTF8 szLhs, LPCUTF8 szRhs, INT32 stringNumBytes)
{
    CONTRACTL {
        FCALL_CHECK;
        PRECONDITION(CheckPointer(szLhs));
        PRECONDITION(CheckPointer(szRhs));
    }
    CONTRACTL_END;

    // Important: the string in pSsz isn't null terminated so the length must be used
    // when performing operations on the string.

    // At this point, both the left and right strings are guaranteed to have the
    // same length.
    FC_RETURN_BOOL(strncmp(szLhs, szRhs, stringNumBytes) == 0);
}
FCIMPLEND

BOOL QCALLTYPE Utf8String::EqualsCaseInsensitive(LPCUTF8 szLhs, LPCUTF8 szRhs, INT32 stringNumBytes)
{
    QCALL_CONTRACT;

    // Important: the string in pSsz isn't null terminated so the length must be used
    // when performing operations on the string.
    
    BOOL fStringsEqual = FALSE;
    
    BEGIN_QCALL;

    _ASSERTE(CheckPointer(szLhs));
    _ASSERTE(CheckPointer(szRhs));

    // At this point, both the left and right strings are guaranteed to have the
    // same length. 
    StackSString lhs(SString::Utf8, szLhs, stringNumBytes);
    StackSString rhs(SString::Utf8, szRhs, stringNumBytes);

    // We can use SString for simple case insensitive compares
    fStringsEqual = lhs.EqualsCaseInsensitive(rhs);

    END_QCALL;

    return fStringsEqual;
}

ULONG QCALLTYPE Utf8String::HashCaseInsensitive(LPCUTF8 sz, INT32 stringNumBytes)
{
    QCALL_CONTRACT;

    // Important: the string in pSsz isn't null terminated so the length must be used
    // when performing operations on the string.

    ULONG hashValue = 0;
    
    BEGIN_QCALL;

    StackSString str(SString::Utf8, sz, stringNumBytes);
    hashValue = str.HashCaseInsensitive();

    END_QCALL;

    return hashValue;
}

static BOOL CheckCAVisibilityFromDecoratedType(MethodTable* pCAMT, MethodDesc* pCACtor, MethodTable* pDecoratedMT, Module* pDecoratedModule)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(CheckPointer(pCAMT));
        PRECONDITION(CheckPointer(pCACtor, NULL_OK));
        PRECONDITION(CheckPointer(pDecoratedMT, NULL_OK));
        PRECONDITION(CheckPointer(pDecoratedModule));
    }
    CONTRACTL_END;

    DWORD dwAttr = mdPublic;

    if (pCACtor != NULL)
    {
        // Allowing a dangerous method to be called in custom attribute instantiation is, well, dangerous.
        // E.g. a malicious user can craft a custom attribute record that fools us into creating a DynamicMethod
        // object attached to typeof(System.Reflection.CustomAttribute) and thus gain access to mscorlib internals.
        if (InvokeUtil::IsDangerousMethod(pCACtor))
            return FALSE;

        _ASSERTE(pCACtor->IsCtor());

        dwAttr = pCACtor->GetAttrs();
    }
    
    StaticAccessCheckContext accessContext(NULL, pDecoratedMT, pDecoratedModule->GetAssembly());

    // Don't do transparency check here. Custom attributes have different transparency rules. 
    // The checks are done by AllowCriticalCustomAttributes and CheckLinktimeDemands in CustomAttribute.cs.
    return ClassLoader::CanAccess(
        &accessContext, 
        pCAMT,
        pCAMT->GetAssembly(), 
        dwAttr,
        pCACtor,
        NULL,
        *AccessCheckOptions::s_pNormalAccessChecks,
        FALSE,
        FALSE);
}

BOOL QCALLTYPE RuntimeMethodHandle::IsCAVisibleFromDecoratedType(
    EnregisteredTypeHandle  targetTypeHandle,
    MethodDesc *            pTargetCtor,
    EnregisteredTypeHandle  sourceTypeHandle,
    QCall::ModuleHandle     sourceModuleHandle)
{
    QCALL_CONTRACT;

    BOOL bResult = TRUE;

    BEGIN_QCALL;
    TypeHandle sourceHandle = TypeHandle::FromPtr(sourceTypeHandle);
    TypeHandle targetHandle = TypeHandle::FromPtr(targetTypeHandle);

    _ASSERTE((sourceHandle.IsNull() || !sourceHandle.IsTypeDesc()) &&
             !targetHandle.IsNull() &&
             !targetHandle.IsTypeDesc());

    if (sourceHandle.IsTypeDesc() ||
        targetHandle.IsNull() || 
        targetHandle.IsTypeDesc())
        COMPlusThrowArgumentNull(NULL, W("Arg_InvalidHandle"));

    bResult = CheckCAVisibilityFromDecoratedType(targetHandle.AsMethodTable(), pTargetCtor, sourceHandle.AsMethodTable(), sourceModuleHandle);
    END_QCALL;

    return bResult;
}

// static
BOOL QCALLTYPE RuntimeMethodHandle::IsSecurityCritical(MethodDesc *pMD)
{
    CONTRACTL
    {
        QCALL_CHECK;
        PRECONDITION(CheckPointer(pMD));
    }
    CONTRACTL_END;

    BOOL fIsCritical = TRUE;

    BEGIN_QCALL;

    if (pMD == NULL)
        COMPlusThrowArgumentNull(NULL, W("Arg_InvalidHandle"));

    fIsCritical = Security::IsMethodCritical(pMD);

    END_QCALL;

    return fIsCritical;
}

// static
BOOL QCALLTYPE RuntimeMethodHandle::IsSecuritySafeCritical(MethodDesc *pMD)
{
    CONTRACTL
    {
        QCALL_CHECK;
        PRECONDITION(CheckPointer(pMD));
    }
    CONTRACTL_END;

    BOOL fIsSafeCritical = TRUE;

    BEGIN_QCALL;

    if (pMD == NULL)
        COMPlusThrowArgumentNull(NULL, W("Arg_InvalidHandle"));

    fIsSafeCritical = Security::IsMethodSafeCritical(pMD);

    END_QCALL;

    return fIsSafeCritical;
}

// static
BOOL QCALLTYPE RuntimeMethodHandle::IsSecurityTransparent(MethodDesc *pMD)
{
    CONTRACTL
    {
        QCALL_CHECK;
        PRECONDITION(CheckPointer(pMD));
    }
    CONTRACTL_END;

    BOOL fIsTransparent = TRUE;

    BEGIN_QCALL;

    if (pMD == NULL)
        COMPlusThrowArgumentNull(NULL, W("Arg_InvalidHandle"));

    fIsTransparent = Security::IsMethodTransparent(pMD);

    END_QCALL;

    return fIsTransparent;
}

FCIMPL2(FC_BOOL_RET, RuntimeMethodHandle::IsTokenSecurityTransparent, ReflectModuleBaseObject *pModuleUNSAFE, INT32 tkToken) {
    CONTRACTL {
        FCALL_CHECK;
    }
    CONTRACTL_END;

    REFLECTMODULEBASEREF refModule = (REFLECTMODULEBASEREF)ObjectToOBJECTREF(pModuleUNSAFE);

    if(refModule == NULL)
        FCThrowRes(kArgumentNullException, W("Arg_InvalidHandle"));

    Module *pModule = refModule->GetModule();

    BOOL bIsSecurityTransparent = TRUE;
    
    HELPER_METHOD_FRAME_BEGIN_RET_1(refModule);
    {
        bIsSecurityTransparent = Security::IsTokenTransparent(pModule, tkToken);
    }
    HELPER_METHOD_FRAME_END();

    FC_RETURN_BOOL(bIsSecurityTransparent );

}
FCIMPLEND

static bool DoAttributeTransparencyChecks(Assembly *pAttributeAssembly, Assembly *pDecoratedAssembly)
{
    CONTRACTL
    {
        THROWS;
        MODE_COOPERATIVE;
        GC_TRIGGERS;
        PRECONDITION(CheckPointer(pAttributeAssembly));
        PRECONDITION(CheckPointer(pDecoratedAssembly));
    }
    CONTRACTL_END;

    // Do transparency checks - if both the decorated assembly and attribute use the v4 security model,
    // then we can do a direct transparency check.  However, if the decorated assembly uses the v2
    // security model, then we need to convert the security critical attribute to looking as though it
    // has a LinkDemand for full trust.
    const SecurityTransparencyBehavior *pTargetTransparency = pDecoratedAssembly->GetSecurityTransparencyBehavior();
    const SecurityTransparencyBehavior *pAttributeTransparency = pAttributeAssembly->GetSecurityTransparencyBehavior();

    // v2 transparency did not impose checks for using its custom attributes, so if the attribute is
    // defined in an assembly using the v2 transparency model then we don't need to do any
    // additional checks.
    if (pAttributeTransparency->DoAttributesRequireTransparencyChecks())
    {
        if (pTargetTransparency->CanTransparentCodeCallLinkDemandMethods() &&
            pAttributeTransparency->CanCriticalMembersBeConvertedToLinkDemand())
        {
            // We have a v4 critical attribute being applied to a v2 transparent target. Since v2
            // transparency doesn't understand externally visible critical attributes, we convert the
            // attribute to a LinkDemand for full trust.  v2 transparency did not convert
            // LinkDemands on its attributes into full demands so we do not do that second level of
            // conversion here either.
            Security::FullTrustLinkDemand(pDecoratedAssembly);
            return true;
        }
        else
        {
            // If we are here either the target of the attribute uses the v4 security model, or the
            // attribute itself uses the v2 model.  In these cases, we cannot perform a conversion of
            // the critical attribute into a LinkDemand, and we have an error condition.
            return false;
        }
    }

    return true;
}

FCIMPL3(void, RuntimeMethodHandle::CheckLinktimeDemands, ReflectMethodObject *pMethodUNSAFE, ReflectModuleBaseObject *pModuleUNSAFE, CLR_BOOL isDecoratedTargetSecurityTransparent) 
{
    CONTRACTL 
    {
        FCALL_CHECK;
        PRECONDITION(CheckPointer(pModuleUNSAFE));
        PRECONDITION(CheckPointer(pMethodUNSAFE));
    }
    CONTRACTL_END;

    REFLECTMETHODREF refMethod = (REFLECTMETHODREF)ObjectToOBJECTREF(pMethodUNSAFE);
    REFLECTMODULEBASEREF refModule = (REFLECTMODULEBASEREF)ObjectToOBJECTREF(pModuleUNSAFE);

    HELPER_METHOD_FRAME_BEGIN_2(refMethod, refModule);
    {
        MethodDesc *pCallee = refMethod->GetMethod(); // pCallee is the CA ctor or CA setter method
        Module *pDecoratedModule = refModule->GetModule();

        bool isAttributeSecurityCritical = Security::IsMethodCritical(pCallee) &&
                                           !Security::IsMethodSafeCritical(pCallee);

        if (isDecoratedTargetSecurityTransparent && isAttributeSecurityCritical)
        {
            if (!DoAttributeTransparencyChecks(pCallee->GetAssembly(), pDecoratedModule->GetAssembly()))
            {
                SecurityTransparent::ThrowMethodAccessException(pCallee);
            }
        }

#ifndef FEATURE_CORECLR    
        if (pCallee->RequiresLinktimeCheck())
        {
            Module *pModule = refModule->GetModule();
            Security::LinktimeCheckMethod(pDecoratedModule->GetAssembly(), pCallee);
        }
#endif // !FEATURE_CORECLR
    }
    HELPER_METHOD_FRAME_END();
}
FCIMPLEND

NOINLINE static ReflectClassBaseObject* GetRuntimeTypeHelper(LPVOID __me, TypeHandle typeHandle, OBJECTREF keepAlive)
{
    FC_INNER_PROLOG_NO_ME_SETUP();
    if (typeHandle.AsPtr() == NULL)
        return NULL;
    
    // RuntimeTypeHandle::GetRuntimeType has picked off the most common case, but does not cover array types.
    // Before we do the really heavy weight option of setting up a helper method frame, check if we have to. 
    OBJECTREF refType = typeHandle.GetManagedClassObjectFast();
    if (refType != NULL)
        return (ReflectClassBaseObject*)OBJECTREFToObject(refType);

    HELPER_METHOD_FRAME_BEGIN_RET_ATTRIB_1(Frame::FRAME_ATTR_EXACT_DEPTH|Frame::FRAME_ATTR_CAPTURE_DEPTH_2, keepAlive);
    refType = typeHandle.GetManagedClassObject();
    HELPER_METHOD_FRAME_END();

    FC_INNER_EPILOG();
    return (ReflectClassBaseObject*)OBJECTREFToObject(refType);
}

#define RETURN_CLASS_OBJECT(typeHandle, keepAlive) FC_INNER_RETURN(ReflectClassBaseObject*, GetRuntimeTypeHelper(__me, typeHandle, keepAlive))

NOINLINE ReflectModuleBaseObject* GetRuntimeModuleHelper(LPVOID __me, Module *pModule, OBJECTREF keepAlive)
{
    FC_INNER_PROLOG_NO_ME_SETUP();
    if (pModule == NULL)
        return NULL;
    
    DomainFile * pDomainFile = pModule->FindDomainFile(GetAppDomain());

    OBJECTREF refModule = (pDomainFile != NULL) ? pDomainFile->GetExposedModuleObjectIfExists() : NULL;

    if(refModule != NULL)
        return (ReflectModuleBaseObject*)OBJECTREFToObject(refModule);

    HELPER_METHOD_FRAME_BEGIN_RET_ATTRIB_1(Frame::FRAME_ATTR_EXACT_DEPTH|Frame::FRAME_ATTR_CAPTURE_DEPTH_2, keepAlive);
    refModule = pModule->GetExposedObject();
    HELPER_METHOD_FRAME_END();

    FC_INNER_EPILOG();
    return (ReflectModuleBaseObject*)OBJECTREFToObject(refModule);
}

NOINLINE AssemblyBaseObject* GetRuntimeAssemblyHelper(LPVOID __me, DomainAssembly *pAssembly, OBJECTREF keepAlive)
{
    FC_INNER_PROLOG_NO_ME_SETUP();
    if (pAssembly == NULL)
        return NULL;
    
    OBJECTREF refAssembly = (pAssembly != NULL) ? pAssembly->GetExposedAssemblyObjectIfExists() : NULL;

    if(refAssembly != NULL)
        return (AssemblyBaseObject*)OBJECTREFToObject(refAssembly);

    HELPER_METHOD_FRAME_BEGIN_RET_ATTRIB_1(Frame::FRAME_ATTR_EXACT_DEPTH|Frame::FRAME_ATTR_CAPTURE_DEPTH_2, keepAlive);
    refAssembly = pAssembly->GetExposedAssemblyObject();
    HELPER_METHOD_FRAME_END();

    FC_INNER_EPILOG();
    return (AssemblyBaseObject*)OBJECTREFToObject(refAssembly);
}


// This is the routine that is called by the 'typeof()' operator in C#.  It is one of the most commonly used
// reflection operations. This call should be optimized away in nearly all situations
FCIMPL1_V(ReflectClassBaseObject*, RuntimeTypeHandle::GetTypeFromHandle, FCALLRuntimeTypeHandle th)
{
    FCALL_CONTRACT;
    
    FCUnique(0x31);
    return FCALL_RTH_TO_REFLECTCLASS(th);
}
FCIMPLEND

FCIMPL1(ReflectClassBaseObject*, RuntimeTypeHandle::GetRuntimeType, EnregisteredTypeHandle th)
{
    FCALL_CONTRACT;
    
    TypeHandle typeHandle = TypeHandle::FromPtr(th);
    _ASSERTE(CheckPointer(typeHandle.AsPtr(), NULL_OK));
    if (typeHandle.AsPtr()!= NULL)
    {
        if (!typeHandle.IsTypeDesc())
        {
            OBJECTREF typePtr = typeHandle.AsMethodTable()->GetManagedClassObjectIfExists();
            if (typePtr != NULL)
            {
                return (ReflectClassBaseObject*)OBJECTREFToObject(typePtr);
            }
        }
    }
    else 
        return NULL;

    RETURN_CLASS_OBJECT(typeHandle, NULL);
}
FCIMPLEND

FCIMPL1_V(EnregisteredTypeHandle, RuntimeTypeHandle::GetValueInternal, FCALLRuntimeTypeHandle RTH)
{
    FCALL_CONTRACT;

    if (FCALL_RTH_TO_REFLECTCLASS(RTH) == NULL)
        return 0;

    return FCALL_RTH_TO_REFLECTCLASS(RTH) ->GetType().AsPtr();
}
FCIMPLEND

// TypeEqualsHelper and TypeNotEqualsHelper are almost identical.
// Unfortunately we cannot combime them because they need to hardcode the caller's name
NOINLINE static BOOL TypeEqualSlow(OBJECTREF refL, OBJECTREF refR, LPVOID __me)
{
    BOOL ret = FALSE;

    FC_INNER_PROLOG_NO_ME_SETUP();

    _ASSERTE(refL != NULL && refR != NULL);

    HELPER_METHOD_FRAME_BEGIN_RET_ATTRIB_2(Frame::FRAME_ATTR_EXACT_DEPTH|Frame::FRAME_ATTR_CAPTURE_DEPTH_2, refL, refR);

    MethodDescCallSite TypeEqualsMethod(METHOD__OBJECT__EQUALS, &refL);

    ARG_SLOT args[] = 
    {
        ObjToArgSlot(refL),
        ObjToArgSlot(refR)
    };

    ret = TypeEqualsMethod.Call_RetBool(args);

    HELPER_METHOD_FRAME_END();

    FC_INNER_EPILOG();

    return ret;
}



#include <optsmallperfcritical.h>

FCIMPL2(FC_BOOL_RET, RuntimeTypeHandle::TypeEQ, Object* left, Object* right)
{
    FCALL_CONTRACT;

    OBJECTREF refL = (OBJECTREF)left;
    OBJECTREF refR = (OBJECTREF)right;

    if (refL == refR)
    {
        FC_RETURN_BOOL(TRUE);
    }

    if (!refL || !refR)
    {
        FC_RETURN_BOOL(FALSE);
    }

    if ((refL->GetMethodTable() == g_pRuntimeTypeClass || refR->GetMethodTable() == g_pRuntimeTypeClass))
    {
        // Quick path for negative common case
        FC_RETURN_BOOL(FALSE);
    }

    // The fast path didn't get us the result
    // Let's try the slow path: refL.Equals(refR);
    FC_INNER_RETURN(FC_BOOL_RET, (FC_BOOL_RET)(!!TypeEqualSlow(refL, refR, __me)));
}
FCIMPLEND

FCIMPL2(FC_BOOL_RET, RuntimeTypeHandle::TypeNEQ, Object* left, Object* right)
{
    FCALL_CONTRACT;

    OBJECTREF refL = (OBJECTREF)left;
    OBJECTREF refR = (OBJECTREF)right;

    if (refL == refR)
    {
        FC_RETURN_BOOL(FALSE);
    }

    if (!refL || !refR)
    {
        FC_RETURN_BOOL(TRUE);
    }

    if ((refL->GetMethodTable() == g_pRuntimeTypeClass || refR->GetMethodTable() == g_pRuntimeTypeClass))
    {
        // Quick path for negative common case
        FC_RETURN_BOOL(TRUE);
    }

    // The fast path didn't get us the result
    // Let's try the slow path: refL.Equals(refR);
    FC_INNER_RETURN(FC_BOOL_RET, (FC_BOOL_RET)(!TypeEqualSlow(refL, refR, __me)));
}
FCIMPLEND

#include <optdefault.h>



#ifndef FEATURE_CORECLR
FCIMPL2(FC_BOOL_RET, RuntimeTypeHandle::IsEquivalentTo, ReflectClassBaseObject *rtType1UNSAFE, ReflectClassBaseObject *rtType2UNSAFE)
{
    FCALL_CONTRACT;

    BOOL bResult = FALSE;

    REFLECTCLASSBASEREF rtType1 = (REFLECTCLASSBASEREF)ObjectToOBJECTREF(rtType1UNSAFE);
    REFLECTCLASSBASEREF rtType2 = (REFLECTCLASSBASEREF)ObjectToOBJECTREF(rtType2UNSAFE);

    HELPER_METHOD_FRAME_BEGIN_RET_2(rtType1, rtType2);
    if (rtType1 == NULL)
        COMPlusThrowArgumentNull(W("rtType1"));
    if (rtType2 == NULL)
        COMPlusThrowArgumentNull(W("rtType2"));

    bResult = rtType1->GetType().IsEquivalentTo(rtType2->GetType());
    HELPER_METHOD_FRAME_END();

    FC_RETURN_BOOL(bResult);
}
FCIMPLEND

FCIMPL1(FC_BOOL_RET, RuntimeTypeHandle::IsEquivalentType, ReflectClassBaseObject *rtTypeUNSAFE)
{
    FCALL_CONTRACT;

    BOOL bResult = FALSE;

    TypeHandle typeHandle = rtTypeUNSAFE->GetType();
    if (!typeHandle.IsTypeDesc())
        bResult = typeHandle.AsMethodTable()->GetClass()->IsEquivalentType();

    FC_RETURN_BOOL(bResult);
}
FCIMPLEND
#endif // !FEATURE_CORECLR

#ifdef FEATURE_COMINTEROP
FCIMPL1(FC_BOOL_RET, RuntimeTypeHandle::IsWindowsRuntimeObjectType, ReflectClassBaseObject *rtTypeUNSAFE)
{
    FCALL_CONTRACT;

    BOOL isWindowsRuntimeType = FALSE;

    TypeHandle typeHandle = rtTypeUNSAFE->GetType();
    MethodTable *pMT = typeHandle.GetMethodTable();

    if (pMT != NULL)
    {
        isWindowsRuntimeType = pMT->IsWinRTObjectType();
    }

    FC_RETURN_BOOL(isWindowsRuntimeType);
}
FCIMPLEND

FCIMPL1(FC_BOOL_RET, RuntimeTypeHandle::IsTypeExportedToWindowsRuntime, ReflectClassBaseObject *rtTypeUNSAFE)
{
    FCALL_CONTRACT;

    BOOL isExportedToWinRT = FALSE;

    TypeHandle typeHandle = rtTypeUNSAFE->GetType();
    MethodTable *pMT = typeHandle.GetMethodTable();

    if (pMT != NULL)
    {
        isExportedToWinRT = pMT->IsExportedToWinRT();
    }

    FC_RETURN_BOOL(isExportedToWinRT);
}
FCIMPLEND
#endif // FEATURE_COMINTEROP

NOINLINE static MethodDesc * RestoreMethodHelper(MethodDesc * pMethod, LPVOID __me)
{
    FC_INNER_PROLOG_NO_ME_SETUP();

    HELPER_METHOD_FRAME_BEGIN_RET_0();
    pMethod->CheckRestore();
    HELPER_METHOD_FRAME_END();

    FC_INNER_EPILOG();

    return pMethod;
}

FCIMPL1(MethodDesc *, RuntimeTypeHandle::GetFirstIntroducedMethod, ReflectClassBaseObject *pTypeUNSAFE) {
    CONTRACTL {
        FCALL_CHECK;
        PRECONDITION(CheckPointer(pTypeUNSAFE));
    }
    CONTRACTL_END;

    REFLECTCLASSBASEREF refType = (REFLECTCLASSBASEREF)ObjectToOBJECTREF(pTypeUNSAFE);
    TypeHandle typeHandle = refType->GetType();
    
    if (typeHandle.IsGenericVariable())
        FCThrowRes(kArgumentException, W("Arg_InvalidHandle"));
        
    if (typeHandle.IsTypeDesc()) {
        if (!typeHandle.IsArray()) 
            return NULL;
    }

    MethodTable* pMT = typeHandle.GetMethodTable();
    if (pMT == NULL)
        return NULL;

    MethodDesc* pMethod = MethodTable::IntroducedMethodIterator::GetFirst(pMT);

    // The only method that can show up here unrestored is instantiated methods. Check for it before performing the expensive IsRestored() check.
    if (pMethod != NULL && pMethod->GetClassification() == mcInstantiated && !pMethod->IsRestored()) {
        FC_INNER_RETURN(MethodDesc *, RestoreMethodHelper(pMethod, __me));
    }

    _ASSERTE(pMethod == NULL || pMethod->IsRestored());
    return pMethod;
}
FCIMPLEND

#include <optsmallperfcritical.h>
FCIMPL1(void, RuntimeTypeHandle::GetNextIntroducedMethod, MethodDesc ** ppMethod) {
    CONTRACTL {
        FCALL_CHECK;
        PRECONDITION(CheckPointer(ppMethod));
        PRECONDITION(CheckPointer(*ppMethod));
    }
    CONTRACTL_END;

    MethodDesc *pMethod = MethodTable::IntroducedMethodIterator::GetNext(*ppMethod);

    *ppMethod = pMethod;

    if (pMethod != NULL && pMethod->GetClassification() == mcInstantiated && !pMethod->IsRestored()) {
        FC_INNER_RETURN_VOID(RestoreMethodHelper(pMethod, __me));
    }

    _ASSERTE(pMethod == NULL || pMethod->IsRestored());
}
FCIMPLEND
#include <optdefault.h>

FCIMPL1(INT32, RuntimeTypeHandle::GetCorElementType, ReflectClassBaseObject *pTypeUNSAFE) {
    CONTRACTL {
        FCALL_CHECK;
    }
    CONTRACTL_END;

    REFLECTCLASSBASEREF refType = (REFLECTCLASSBASEREF)ObjectToOBJECTREF(pTypeUNSAFE);

    if (refType == NULL)
        FCThrowRes(kArgumentNullException, W("Arg_InvalidHandle"));

    return refType->GetType().GetSignatureCorElementType();
}
FCIMPLEND

FCIMPL1(AssemblyBaseObject*, RuntimeTypeHandle::GetAssembly, ReflectClassBaseObject *pTypeUNSAFE) {
    CONTRACTL {
        FCALL_CHECK;
    }
    CONTRACTL_END;

    REFLECTCLASSBASEREF refType = (REFLECTCLASSBASEREF)ObjectToOBJECTREF(pTypeUNSAFE);

    if (refType == NULL)
        FCThrowRes(kArgumentNullException, W("Arg_InvalidHandle"));

    DomainFile *pDomainFile = NULL;
    
        Module *pModule = refType->GetType().GetAssembly()->GetManifestModule();

            pDomainFile = pModule->FindDomainFile(GetAppDomain());
#ifdef FEATURE_LOADER_OPTIMIZATION        
        if (pDomainFile == NULL)
        {
            HELPER_METHOD_FRAME_BEGIN_RET_1(refType);
            
            pDomainFile = GetAppDomain()->LoadDomainNeutralModuleDependency(pModule, FILE_LOADED);

            HELPER_METHOD_FRAME_END();
        }
#endif // FEATURE_LOADER_OPTIMIZATION        


    FC_RETURN_ASSEMBLY_OBJECT((DomainAssembly *)pDomainFile, refType);
}
FCIMPLEND


FCIMPL1(FC_BOOL_RET, RuntimeFieldHandle::AcquiresContextFromThis, FieldDesc *pField)
{
    CONTRACTL {
        FCALL_CHECK;
        PRECONDITION(CheckPointer(pField));
    }
    CONTRACTL_END;

    FC_RETURN_BOOL(pField->IsSharedByGenericInstantiations());

}
FCIMPLEND

// static
BOOL QCALLTYPE RuntimeFieldHandle::IsSecurityCritical(FieldDesc *pFD)
{
    CONTRACTL
    {
        QCALL_CHECK;
        PRECONDITION(CheckPointer(pFD));
    }
    CONTRACTL_END;

    BOOL fIsCritical = FALSE;

    BEGIN_QCALL;

    fIsCritical = Security::IsFieldCritical(pFD);

    END_QCALL;

    return fIsCritical;
}

// static
BOOL QCALLTYPE RuntimeFieldHandle::IsSecuritySafeCritical(FieldDesc *pFD)
{
    CONTRACTL
    {
        QCALL_CHECK;
        PRECONDITION(CheckPointer(pFD));
    }
    CONTRACTL_END;

    BOOL fIsSafeCritical = FALSE;

    BEGIN_QCALL;

    fIsSafeCritical = Security::IsFieldSafeCritical(pFD);

    END_QCALL;

    return fIsSafeCritical;
}

// static
BOOL QCALLTYPE RuntimeFieldHandle::IsSecurityTransparent(FieldDesc *pFD)
{
    CONTRACTL
    {
        QCALL_CHECK;
        PRECONDITION(CheckPointer(pFD));
    }
    CONTRACTL_END;

    BOOL fIsTransparent = FALSE;

    BEGIN_QCALL;

    fIsTransparent = Security::IsFieldTransparent(pFD);

    END_QCALL;

    return fIsTransparent;
}

// static
void QCALLTYPE RuntimeFieldHandle::CheckAttributeAccess(FieldDesc *pFD, QCall::ModuleHandle pModule)
{
    CONTRACTL
    {
        QCALL_CHECK;
        PRECONDITION(CheckPointer(pFD));
        PRECONDITION(CheckPointer(pModule.m_pModule));
    }
    CONTRACTL_END;

    BEGIN_QCALL;

    if (Security::IsFieldCritical(pFD) && !Security::IsFieldSafeCritical(pFD))
    {
        GCX_COOP();

        if (!DoAttributeTransparencyChecks(pFD->GetModule()->GetAssembly(), pModule->GetAssembly()))
        {
            ThrowFieldAccessException(NULL, pFD, TRUE, IDS_E_CRITICAL_FIELD_ACCESS_DENIED);
        }
    }

    END_QCALL;
}

FCIMPL1(ReflectModuleBaseObject*, RuntimeTypeHandle::GetModule, ReflectClassBaseObject *pTypeUNSAFE) {
    CONTRACTL {
        FCALL_CHECK;
    }
    CONTRACTL_END;
    
    Module *result;

    REFLECTCLASSBASEREF refType = (REFLECTCLASSBASEREF)ObjectToOBJECTREF(pTypeUNSAFE);

    if (refType == NULL)
        FCThrowRes(kArgumentNullException, W("Arg_InvalidHandle"));

    BEGIN_SO_INTOLERANT_CODE_NOTHROW(GetThread(), FCThrow(kStackOverflowException));

    result = refType->GetType().GetModule();

    END_SO_INTOLERANT_CODE;

    FC_RETURN_MODULE_OBJECT(result, refType);
}
FCIMPLEND

FCIMPL1(ReflectClassBaseObject *, RuntimeTypeHandle::GetBaseType, ReflectClassBaseObject *pTypeUNSAFE) {
    CONTRACTL {
        FCALL_CHECK;
    }
    CONTRACTL_END;

    REFLECTCLASSBASEREF refType = (REFLECTCLASSBASEREF)ObjectToOBJECTREF(pTypeUNSAFE);

    if (refType == NULL)
        FCThrowRes(kArgumentNullException, W("Arg_InvalidHandle"));

    TypeHandle typeHandle = refType->GetType();
    
    if (typeHandle.IsGenericVariable())
        FCThrowRes(kArgumentException, W("Arg_InvalidHandle"));
        
    if (typeHandle.IsTypeDesc()) {
        if (!typeHandle.IsArray()) 
            return NULL;
    }
    
    RETURN_CLASS_OBJECT(typeHandle.GetParent(), refType);
}
FCIMPLEND
     
FCIMPL1(ReflectClassBaseObject *, RuntimeTypeHandle::GetElementType, ReflectClassBaseObject *pTypeUNSAFE) {
    CONTRACTL {
        FCALL_CHECK;
    }
    CONTRACTL_END;
    
    REFLECTCLASSBASEREF refType = (REFLECTCLASSBASEREF)ObjectToOBJECTREF(pTypeUNSAFE);

    if (refType == NULL)
        FCThrowRes(kArgumentNullException, W("Arg_InvalidHandle"));

    TypeHandle typeHandle = refType->GetType();

    if (!typeHandle.IsTypeDesc())
        return 0;   

    if (typeHandle.IsGenericVariable())
        return 0;

    TypeHandle typeReturn;

    if (typeHandle.IsArray()) 
        typeReturn = typeHandle.AsArray()->GetArrayElementTypeHandle();
    else
        typeReturn = typeHandle.AsTypeDesc()->GetTypeParam();

    RETURN_CLASS_OBJECT(typeReturn, refType);
}
FCIMPLEND
            
FCIMPL1(INT32, RuntimeTypeHandle::GetArrayRank, ReflectClassBaseObject *pTypeUNSAFE) {
    CONTRACTL {
        FCALL_CHECK;
        PRECONDITION(CheckPointer(pTypeUNSAFE));
        PRECONDITION(pTypeUNSAFE->GetType().IsArray());
    }
    CONTRACTL_END;

    REFLECTCLASSBASEREF refType = (REFLECTCLASSBASEREF)ObjectToOBJECTREF(pTypeUNSAFE);

    return (INT32)refType->GetType().AsArray()->GetRank();   
}
FCIMPLEND

FCIMPL1(INT32, RuntimeTypeHandle::GetNumVirtuals, ReflectClassBaseObject *pTypeUNSAFE) {
    CONTRACTL {
        FCALL_CHECK;
    }
    CONTRACTL_END;
    
    REFLECTCLASSBASEREF refType = (REFLECTCLASSBASEREF)ObjectToOBJECTREF(pTypeUNSAFE);

    if (refType == NULL)
        FCThrowRes(kArgumentNullException, W("Arg_InvalidHandle"));

    TypeHandle typeHandle = refType->GetType();

    if (typeHandle.IsGenericVariable())
        FCThrowRes(kArgumentException, W("Arg_InvalidHandle"));
    
    MethodTable *pMT = typeHandle.GetMethodTable();

    if (pMT) 
        return (INT32)pMT->GetNumVirtuals();
    else
        return 0; //REVIEW: should this return the number of methods in Object?
}
FCIMPLEND

FCIMPL2(MethodDesc *, RuntimeTypeHandle::GetMethodAt, ReflectClassBaseObject *pTypeUNSAFE, INT32 slot) {
    CONTRACTL {
        FCALL_CHECK;
    }
    CONTRACTL_END;
    
    REFLECTCLASSBASEREF refType = (REFLECTCLASSBASEREF)ObjectToOBJECTREF(pTypeUNSAFE);

    if (refType == NULL)
        FCThrowRes(kArgumentNullException, W("Arg_InvalidHandle"));

    TypeHandle typeHandle = refType->GetType();

    MethodDesc* pRetMethod = NULL;

    if (typeHandle.IsGenericVariable())
        FCThrowRes(kArgumentException, W("Arg_InvalidHandle"));

    if (slot < 0 || slot >= (INT32)typeHandle.GetMethodTable()->GetNumVirtuals())
        FCThrowRes(kArgumentException, W("Arg_ArgumentOutOfRangeException"));      

    HELPER_METHOD_FRAME_BEGIN_RET_1(refType);
    pRetMethod = typeHandle.GetMethodTable()->GetMethodDescForSlot((DWORD)slot);
    HELPER_METHOD_FRAME_END();

    return pRetMethod;
}

FCIMPLEND

FCIMPL3(FC_BOOL_RET, RuntimeTypeHandle::GetFields, ReflectClassBaseObject *pTypeUNSAFE, INT32 **result, INT32 *pCount) {
    CONTRACTL {
        FCALL_CHECK;
    }
    CONTRACTL_END;
    
    REFLECTCLASSBASEREF refType = (REFLECTCLASSBASEREF)ObjectToOBJECTREF(pTypeUNSAFE);
    if (refType == NULL)
        FCThrowRes(kArgumentNullException, W("Arg_InvalidHandle"));

    TypeHandle typeHandle = refType->GetType();
    
    if (!pCount || !result)
        FCThrow(kArgumentNullException);

    if (typeHandle.IsGenericVariable())
        FCThrowRes(kArgumentException, W("Arg_InvalidHandle"));
    
    if (typeHandle.IsTypeDesc()) {
        *pCount = 0;
        FC_RETURN_BOOL(TRUE);
    }

    MethodTable *pMT= typeHandle.GetMethodTable();
    if (!pMT)
        FCThrowRes(kArgumentException, W("Arg_InvalidHandle"));

    BOOL retVal = FALSE;
    HELPER_METHOD_FRAME_BEGIN_RET_1(refType);
    // <TODO>Check this approximation - we may be losing exact type information </TODO>
    ApproxFieldDescIterator fdIterator(pMT, ApproxFieldDescIterator::ALL_FIELDS);
    INT32 count = (INT32)fdIterator.Count();

    if (count > *pCount) 
    {
        *pCount = count;
    } 
    else 
    {
        for(INT32 i = 0; i < count; i ++)
            result[i] = (INT32*)fdIterator.Next();
        
        *pCount = count;
        retVal = TRUE;
    }
    HELPER_METHOD_FRAME_END();
    FC_RETURN_BOOL(retVal);
}
FCIMPLEND

void QCALLTYPE RuntimeMethodHandle::ConstructInstantiation(MethodDesc * pMethod, DWORD format, QCall::StringHandleOnStack retString)
{
    QCALL_CONTRACT;

    BEGIN_QCALL;

    StackSString ss;
    TypeString::AppendInst(ss, pMethod->LoadMethodInstantiation(), format);
    retString.Set(ss);
    
    END_QCALL;
}

void QCALLTYPE RuntimeTypeHandle::ConstructName(EnregisteredTypeHandle pTypeHandle, DWORD format, QCall::StringHandleOnStack retString)
{
    QCALL_CONTRACT;
      
    BEGIN_QCALL;

    StackSString ss;    
    TypeString::AppendType(ss, TypeHandle::FromPtr(pTypeHandle), format);
    retString.Set(ss);

    END_QCALL;
}

PTRARRAYREF CopyRuntimeTypeHandles(TypeHandle * prgTH, FixupPointer<TypeHandle> * prgTH2, INT32 numTypeHandles, BinderClassID arrayElemType)
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;

    PTRARRAYREF refReturn = NULL;
    PTRARRAYREF refArray  = NULL;

    if (numTypeHandles == 0)
        return NULL;

    _ASSERTE((prgTH != NULL) || (prgTH2 != NULL));
    if (prgTH != NULL)
    {
        _ASSERTE(prgTH2 == NULL);
    }

    GCPROTECT_BEGIN(refArray);
    TypeHandle thRuntimeType = TypeHandle(MscorlibBinder::GetClass(arrayElemType));
    TypeHandle arrayHandle = ClassLoader::LoadArrayTypeThrowing(thRuntimeType, ELEMENT_TYPE_SZARRAY);
    refArray = (PTRARRAYREF)AllocateArrayEx(arrayHandle, &numTypeHandles, 1);

    for (INT32 i = 0; i < numTypeHandles; i++)
    {
        TypeHandle th;

        if (prgTH != NULL)
            th = prgTH[i];
        else
            th = prgTH2[i].GetValue();

        OBJECTREF refType = th.GetManagedClassObject();
        refArray->SetAt(i, refType);
    }

    refReturn = refArray;
    GCPROTECT_END();

    return refReturn;
}

void QCALLTYPE RuntimeTypeHandle::GetConstraints(EnregisteredTypeHandle pTypeHandle, QCall::ObjectHandleOnStack retTypeArray)
{
    QCALL_CONTRACT;

    TypeHandle* constraints = NULL;
    
    BEGIN_QCALL;
    
    TypeHandle typeHandle = TypeHandle::FromPtr(pTypeHandle);
    
    if (!typeHandle.IsGenericVariable())
        COMPlusThrow(kArgumentException, W("Arg_InvalidHandle"));

        TypeVarTypeDesc* pGenericVariable = typeHandle.AsGenericVariable();              
    
    DWORD dwCount;
    constraints = pGenericVariable->GetConstraints(&dwCount);

    GCX_COOP();
    retTypeArray.Set(CopyRuntimeTypeHandles(constraints, NULL, dwCount, CLASS__TYPE));

    END_QCALL;

    return;
}

FCIMPL1(PtrArray*, RuntimeTypeHandle::GetInterfaces, ReflectClassBaseObject *pTypeUNSAFE) {
    CONTRACTL {
        FCALL_CHECK;
    }
    CONTRACTL_END;
    
    REFLECTCLASSBASEREF refType = (REFLECTCLASSBASEREF)ObjectToOBJECTREF(pTypeUNSAFE);

    if (refType == NULL)
        FCThrowRes(kArgumentNullException, W("Arg_InvalidHandle"));

    TypeHandle typeHandle = refType->GetType();
    
  if (typeHandle.IsGenericVariable())
        FCThrowRes(kArgumentNullException, W("Arg_InvalidHandle"));
    
    INT32 ifaceCount = 0; 
  
    PTRARRAYREF refRetVal  = NULL;
    HELPER_METHOD_FRAME_BEGIN_RET_2(refRetVal, refType);
    {
        if (typeHandle.IsTypeDesc())
        {
            if (typeHandle.IsArray())
            {
                ifaceCount = typeHandle.GetMethodTable()->GetNumInterfaces();            
            }
            else
            {
                ifaceCount = 0;
            }
        }
        else
        {
            ifaceCount = typeHandle.GetMethodTable()->GetNumInterfaces();
        }

        // Allocate the array
        if (ifaceCount > 0)
        {            
            TypeHandle arrayHandle = ClassLoader::LoadArrayTypeThrowing(TypeHandle(g_pRuntimeTypeClass), ELEMENT_TYPE_SZARRAY);
            refRetVal = (PTRARRAYREF)AllocateArrayEx(arrayHandle, &ifaceCount, 1);
        
            // populate type array
            UINT i = 0;
            
            MethodTable::InterfaceMapIterator it = typeHandle.GetMethodTable()->IterateInterfaceMap();
            while (it.Next())
            {
                OBJECTREF refInterface = it.GetInterface()->GetManagedClassObject();
                refRetVal->SetAt(i, refInterface);
                _ASSERTE(refRetVal->GetAt(i) != NULL);
                i++;
            }
        }
    }
    HELPER_METHOD_FRAME_END();

    return (PtrArray*)OBJECTREFToObject(refRetVal);
}
FCIMPLEND

FCIMPL1(INT32, RuntimeTypeHandle::GetAttributes, ReflectClassBaseObject *pTypeUNSAFE) {
    CONTRACTL {
        FCALL_CHECK;
    }
    CONTRACTL_END;
    
    REFLECTCLASSBASEREF refType = (REFLECTCLASSBASEREF)ObjectToOBJECTREF(pTypeUNSAFE);

    if (refType == NULL)
        FCThrowRes(kArgumentNullException, W("Arg_InvalidHandle"));

    TypeHandle typeHandle = refType->GetType();
      
    if (typeHandle.IsTypeDesc()) {

        if (typeHandle.IsGenericVariable()) {
            return tdPublic;        
        }
    
        if (!typeHandle.IsArray()) 
            return 0;
    }

#ifdef FEATURE_COMINTEROP
    // __ComObject types are always public.
    if (IsComObjectClass(typeHandle))
        return (typeHandle.GetMethodTable()->GetAttrClass() & tdVisibilityMask) | tdPublic;
#endif // FEATURE_COMINTEROP

    INT32 ret = 0;
    
    ret = (INT32)typeHandle.GetMethodTable()->GetAttrClass();
    return ret;
}
FCIMPLEND

#ifdef FEATURE_REMOTING    
FCIMPL1(FC_BOOL_RET, RuntimeTypeHandle::IsContextful, ReflectClassBaseObject *pTypeUNSAFE) {
    CONTRACTL {
        FCALL_CHECK;
    }
    CONTRACTL_END;
    
    REFLECTCLASSBASEREF refType = (REFLECTCLASSBASEREF)ObjectToOBJECTREF(pTypeUNSAFE);

    if (refType == NULL)
        FCThrowRes(kArgumentNullException, W("Arg_InvalidHandle"));

    TypeHandle typeHandle = refType->GetType();
    
    if (typeHandle.IsTypeDesc())
        FC_RETURN_BOOL(FALSE);

    MethodTable* pMT= typeHandle.GetMethodTable();
    
    if (!pMT)
        FCThrowRes(kArgumentException, W("Arg_InvalidHandle"));

    FC_RETURN_BOOL(pMT->IsContextful());
}
FCIMPLEND
#endif

FCIMPL1(FC_BOOL_RET, RuntimeTypeHandle::IsValueType, ReflectClassBaseObject *pTypeUNSAFE)
{
    CONTRACTL {
        FCALL_CHECK;
    }
    CONTRACTL_END;
    
    REFLECTCLASSBASEREF refType = (REFLECTCLASSBASEREF)ObjectToOBJECTREF(pTypeUNSAFE);

    _ASSERTE(refType != NULL);

    TypeHandle typeHandle = refType->GetType();

    FC_RETURN_BOOL(typeHandle.IsValueType());
}
FCIMPLEND;

FCIMPL1(FC_BOOL_RET, RuntimeTypeHandle::IsInterface, ReflectClassBaseObject *pTypeUNSAFE)
{
    CONTRACTL {
        FCALL_CHECK;
    }
    CONTRACTL_END;
    
    REFLECTCLASSBASEREF refType = (REFLECTCLASSBASEREF)ObjectToOBJECTREF(pTypeUNSAFE);

    _ASSERTE(refType != NULL);

    TypeHandle typeHandle = refType->GetType();

    FC_RETURN_BOOL(typeHandle.IsInterface());
}
FCIMPLEND;

BOOL 
QCALLTYPE 
RuntimeTypeHandle::IsVisible(
    EnregisteredTypeHandle pTypeHandle)
{
    CONTRACTL
    {
        QCALL_CHECK;
    }
    CONTRACTL_END;
    
    BOOL fIsExternallyVisible = FALSE;
    
    BEGIN_QCALL;
    
    TypeHandle typeHandle = TypeHandle::FromPtr(pTypeHandle);

    _ASSERTE(!typeHandle.IsNull());
    
    fIsExternallyVisible = typeHandle.IsExternallyVisible();
    
    END_QCALL;
    
    return fIsExternallyVisible;
} // RuntimeTypeHandle::IsVisible

// static
BOOL QCALLTYPE RuntimeTypeHandle::IsSecurityCritical(EnregisteredTypeHandle pTypeHandle)
{
    CONTRACTL
    {
        QCALL_CHECK;
        PRECONDITION(CheckPointer(pTypeHandle));
    }
    CONTRACTL_END;

    BOOL fIsCritical = FALSE;

    BEGIN_QCALL;

    MethodTable *pMT = TypeHandle::FromPtr(pTypeHandle).GetMethodTable();
    if (pMT != NULL)
    {
        fIsCritical = Security::IsTypeCritical(pMT);
    }

    END_QCALL;

    return fIsCritical;
}

// static
BOOL QCALLTYPE RuntimeTypeHandle::IsSecuritySafeCritical(EnregisteredTypeHandle pTypeHandle)
{
    CONTRACTL
    {
        QCALL_CHECK;
        PRECONDITION(CheckPointer(pTypeHandle));
    }
    CONTRACTL_END;

    BOOL fIsSafeCritical = FALSE;

    BEGIN_QCALL;

    MethodTable *pMT = TypeHandle::FromPtr(pTypeHandle).GetMethodTable();
    if (pMT != NULL)
    {
        fIsSafeCritical = Security::IsTypeSafeCritical(pMT);
    }

    END_QCALL;

    return fIsSafeCritical;
}

// static
BOOL QCALLTYPE RuntimeTypeHandle::IsSecurityTransparent(EnregisteredTypeHandle pTypeHandle)
{
    CONTRACTL
    {
        QCALL_CHECK;
        PRECONDITION(CheckPointer(pTypeHandle));
    }
    CONTRACTL_END;

    BOOL fIsTransparent = TRUE;

    BEGIN_QCALL;

    MethodTable * pMT = TypeHandle::FromPtr(pTypeHandle).GetMethodTable();
    if (pMT != NULL)
    {
        fIsTransparent = Security::IsTypeTransparent(pMT);
    }
    
    END_QCALL;

    return fIsTransparent;
}
    
FCIMPL1(FC_BOOL_RET, RuntimeTypeHandle::HasProxyAttribute, ReflectClassBaseObject *pTypeUNSAFE) {
    CONTRACTL {
        FCALL_CHECK;
    }
    CONTRACTL_END;

    REFLECTCLASSBASEREF refType = (REFLECTCLASSBASEREF)ObjectToOBJECTREF(pTypeUNSAFE);

    if (refType == NULL)
        FCThrowRes(kArgumentNullException, W("Arg_InvalidHandle"));

    TypeHandle typeHandle = refType->GetType();
    
    // TODO: Justify this
    if (typeHandle.IsGenericVariable())
        FC_RETURN_BOOL(FALSE);
        
    if (typeHandle.IsTypeDesc()) {
        if (!typeHandle.IsArray()) 
            FC_RETURN_BOOL(FALSE);
    }  
    
    MethodTable* pMT= typeHandle.GetMethodTable();
    
    if (!pMT) 
        FCThrowRes(kArgumentException, W("Arg_InvalidHandle"));

    FC_RETURN_BOOL(pMT->GetClass()->HasRemotingProxyAttribute());
}
FCIMPLEND

FCIMPL2(FC_BOOL_RET, RuntimeTypeHandle::IsComObject, ReflectClassBaseObject *pTypeUNSAFE, CLR_BOOL isGenericCOM) {
#ifdef FEATURE_COMINTEROP
    CONTRACTL {
        FCALL_CHECK;
    }
    CONTRACTL_END;
    
    BOOL ret = FALSE;

    REFLECTCLASSBASEREF refType = (REFLECTCLASSBASEREF)ObjectToOBJECTREF(pTypeUNSAFE);

    if (refType == NULL)
        FCThrowRes(kArgumentNullException, W("Arg_InvalidHandle"));

    TypeHandle typeHandle = refType->GetType();

    HELPER_METHOD_FRAME_BEGIN_RET_1(refType);
    {
        if (isGenericCOM) 
            ret = IsComObjectClass(typeHandle);
        else
            ret = IsComWrapperClass(typeHandle);
    }
    HELPER_METHOD_FRAME_END();

    FC_RETURN_BOOL(ret);
#else
    CONTRACTL {
        DISABLED(NOTHROW);
        GC_NOTRIGGER;
        MODE_COOPERATIVE;
        PRECONDITION(CheckPointer(pTypeUNSAFE));
    }
    CONTRACTL_END;
    FCUnique(0x37);
    FC_RETURN_BOOL(FALSE);
#endif
}
FCIMPLEND

FCIMPL1(LPCUTF8, RuntimeTypeHandle::GetUtf8Name, ReflectClassBaseObject* pTypeUNSAFE) {
    CONTRACTL {
        FCALL_CHECK;
    }
    CONTRACTL_END;
    
    REFLECTCLASSBASEREF refType = (REFLECTCLASSBASEREF)ObjectToOBJECTREF(pTypeUNSAFE);

    if (refType == NULL)
        FCThrowRes(kArgumentNullException, W("Arg_InvalidHandle"));

    TypeHandle typeHandle = refType->GetType();
    INT32 tkTypeDef = mdTypeDefNil;
    LPCUTF8 szName = NULL;

    if (typeHandle.IsGenericVariable())
        FCThrowRes(kArgumentException, W("Arg_InvalidHandle"));
        
    if (typeHandle.IsTypeDesc()) 
        FCThrowRes(kArgumentException, W("Arg_InvalidHandle"));

    MethodTable* pMT= typeHandle.GetMethodTable();
    
    if (pMT == NULL)
        FCThrowRes(kArgumentException, W("Arg_InvalidHandle"));

    tkTypeDef = (INT32)pMT->GetCl();
    
    if (IsNilToken(tkTypeDef))
        FCThrowRes(kArgumentException, W("Arg_InvalidHandle"));
    
    if (FAILED(pMT->GetMDImport()->GetNameOfTypeDef(tkTypeDef, &szName, NULL)))
    {
        FCThrowRes(kArgumentException, W("Arg_InvalidHandle"));
    }
    
    _ASSERTE(CheckPointer(szName, NULL_OK));
    
    return szName;
}
FCIMPLEND

FCIMPL1(INT32, RuntimeTypeHandle::GetToken, ReflectClassBaseObject *pTypeUNSAFE) {
    CONTRACTL {
        FCALL_CHECK;
    }
    CONTRACTL_END;

    REFLECTCLASSBASEREF refType = (REFLECTCLASSBASEREF)ObjectToOBJECTREF(pTypeUNSAFE);

    if (refType == NULL)
        FCThrowRes(kArgumentNullException, W("Arg_InvalidHandle"));

    TypeHandle typeHandle = refType->GetType();

    if (typeHandle.IsTypeDesc()) 
    {
        if (typeHandle.IsGenericVariable())
        {
            INT32 tkTypeDef = typeHandle.AsGenericVariable()->GetToken();
        
            _ASSERTE(!IsNilToken(tkTypeDef) && TypeFromToken(tkTypeDef) == mdtGenericParam);

            return tkTypeDef;
        }
        
        return mdTypeDefNil;
    }

    return  (INT32)typeHandle.AsMethodTable()->GetCl();
}
FCIMPLEND

PVOID QCALLTYPE RuntimeTypeHandle::GetGCHandle(EnregisteredTypeHandle pTypeHandle, INT32 handleType)
{
    QCALL_CONTRACT;
    
    OBJECTHANDLE objHandle = NULL;

    BEGIN_QCALL;
    
    GCX_COOP();

    TypeHandle th = TypeHandle::FromPtr(pTypeHandle);
    objHandle = th.GetDomain()->CreateTypedHandle(NULL, handleType);
    th.GetLoaderAllocator()->RegisterHandleForCleanup(objHandle);

    END_QCALL;

    return objHandle;
}

void QCALLTYPE RuntimeTypeHandle::VerifyInterfaceIsImplemented(EnregisteredTypeHandle pTypeHandle, EnregisteredTypeHandle pIFaceHandle)
{
    QCALL_CONTRACT;

    BEGIN_QCALL;

    TypeHandle typeHandle = TypeHandle::FromPtr(pTypeHandle);
    TypeHandle ifaceHandle = TypeHandle::FromPtr(pIFaceHandle);

    if (typeHandle.IsGenericVariable())
        COMPlusThrow(kArgumentException, W("Arg_InvalidHandle"));
    
    if (typeHandle.IsTypeDesc()) {
        if (!typeHandle.IsArray())
            COMPlusThrow(kArgumentException, W("Arg_NotFoundIFace"));
    }

    if (typeHandle.IsInterface())
        COMPlusThrow(kArgumentException, W("Argument_InterfaceMap"));

    if (!ifaceHandle.IsInterface())
        COMPlusThrow(kArgumentException, W("Arg_MustBeInterface"));

    // First try the cheap check, which amounts to iterating the interface map looking for
    // the ifaceHandle MethodTable.
    if (!typeHandle.GetMethodTable()->ImplementsInterface(ifaceHandle.AsMethodTable()))
    {   // If the cheap check fails, try the more expensive but complete check.
        if (!typeHandle.CanCastTo(ifaceHandle))
        {   // If the complete check fails, we're certain that this type
            // does not implement the interface specified.
        COMPlusThrow(kArgumentException, W("Arg_NotFoundIFace"));
        }
    }

    END_QCALL;
}

INT32 QCALLTYPE RuntimeTypeHandle::GetInterfaceMethodImplementationSlot(EnregisteredTypeHandle pTypeHandle, EnregisteredTypeHandle pOwner, MethodDesc * pMD)
{
    QCALL_CONTRACT;

    INT32 slotNumber = -1;

    BEGIN_QCALL;

    TypeHandle typeHandle = TypeHandle::FromPtr(pTypeHandle);
    TypeHandle thOwnerOfMD = TypeHandle::FromPtr(pOwner);

        // Ok to have INVALID_SLOT in the case where abstract class does not implement an interface method.
        // This case can not be reproed using C# "implements" all interface methods
        // with at least an abstract method. b19897_GetInterfaceMap_Abstract.exe tests this case.
        //@TODO:STUBDISPATCH: Don't need to track down the implementation, just the declaration, and this can
        //@TODO:              be done faster - just need to make a function FindDispatchDecl.
        DispatchSlot slot(typeHandle.GetMethodTable()->FindDispatchSlotForInterfaceMD(thOwnerOfMD, pMD));
    if (!slot.IsNull())
            slotNumber = slot.GetMethodDesc()->GetSlot();

    END_QCALL;
    
    return slotNumber;
    }
    
void QCALLTYPE RuntimeTypeHandle::GetDefaultConstructor(EnregisteredTypeHandle pTypeHandle, QCall::ObjectHandleOnStack retMethod)
{
    QCALL_CONTRACT;

    BEGIN_QCALL;
    
    MethodDesc* pCtor = NULL;
    
    TypeHandle typeHandle = TypeHandle::FromPtr(pTypeHandle);

    if (!typeHandle.IsTypeDesc())
    {
        MethodTable* pMethodTable = typeHandle.AsMethodTable();
        if (pMethodTable->HasDefaultConstructor())
            pCtor = pMethodTable->GetDefaultConstructor();
    }

    if (pCtor != NULL)
    {
        GCX_COOP();
        retMethod.Set(pCtor->GetStubMethodInfo());
    }
    END_QCALL;

    return;
}

FCIMPL1(ReflectMethodObject*, RuntimeTypeHandle::GetDeclaringMethod, ReflectClassBaseObject *pTypeUNSAFE) {
    CONTRACTL {
        FCALL_CHECK;
    }
    CONTRACTL_END;
    
    REFLECTCLASSBASEREF refType = (REFLECTCLASSBASEREF)ObjectToOBJECTREF(pTypeUNSAFE);

    if (refType == NULL)
        FCThrowRes(kArgumentNullException, W("Arg_InvalidHandle"));

    TypeHandle typeHandle = refType->GetType();;

    if (!typeHandle.IsTypeDesc())
        return NULL;
    
    TypeVarTypeDesc* pGenericVariable = typeHandle.AsGenericVariable();
    mdToken defToken = pGenericVariable->GetTypeOrMethodDef();
    if (TypeFromToken(defToken) != mdtMethodDef)
        return NULL;

    REFLECTMETHODREF pRet = NULL;
    HELPER_METHOD_FRAME_BEGIN_RET_0();
    MethodDesc * pMD = pGenericVariable->LoadOwnerMethod();
    pMD->CheckRestore();
    pRet = pMD->GetStubMethodInfo();
    HELPER_METHOD_FRAME_END();

    return (ReflectMethodObject*)OBJECTREFToObject(pRet);
}
FCIMPLEND

FCIMPL1(ReflectClassBaseObject*, RuntimeTypeHandle::GetDeclaringType, ReflectClassBaseObject *pTypeUNSAFE) {
    CONTRACTL {
        FCALL_CHECK;
    }
    CONTRACTL_END;
    
    TypeHandle retTypeHandle;

    BOOL fThrowException = FALSE;
    LPCWSTR argName = W("Arg_InvalidHandle");
    RuntimeExceptionKind reKind = kArgumentNullException;

    REFLECTCLASSBASEREF refType = (REFLECTCLASSBASEREF)ObjectToOBJECTREF(pTypeUNSAFE);

    if (refType == NULL)
        FCThrowRes(kArgumentNullException, W("Arg_InvalidHandle"));

    TypeHandle typeHandle = refType->GetType();

    MethodTable* pMT = NULL;
    mdTypeDef tkTypeDef = mdTokenNil;

    BEGIN_SO_INTOLERANT_CODE_NOTHROW(GetThread(), FCThrow(kStackOverflowException));
    if (typeHandle.IsTypeDesc()) {

        if (typeHandle.IsGenericVariable()) {
            TypeVarTypeDesc* pGenericVariable = typeHandle.AsGenericVariable();
            mdToken defToken = pGenericVariable->GetTypeOrMethodDef();
            
            // Try the fast way first (if the declaring type has been loaded already).
            if (TypeFromToken(defToken) == mdtMethodDef)
            {
                MethodDesc * retMethod = pGenericVariable->GetModule()->LookupMethodDef(defToken);
                if (retMethod != NULL)
                    retTypeHandle = retMethod->GetMethodTable();
            }
            else
            {
                retTypeHandle = pGenericVariable->GetModule()->LookupTypeDef(defToken);
            }

            if (!retTypeHandle.IsNull() && retTypeHandle.IsFullyLoaded())
                goto Exit;

            // OK, need to go the slow way and load the type first.
            HELPER_METHOD_FRAME_BEGIN_RET_1(refType);
            {
                if (TypeFromToken(defToken) == mdtMethodDef)
                {
                    retTypeHandle = pGenericVariable->LoadOwnerMethod()->GetMethodTable();
                }
                else
                {
                    retTypeHandle = pGenericVariable->LoadOwnerType();
                }
                retTypeHandle.CheckRestore();
            }
            HELPER_METHOD_FRAME_END();
            goto Exit;
        }
        if (!typeHandle.IsArray())
        {
            retTypeHandle = TypeHandle();
            goto Exit;
        }
    }
    
    pMT = typeHandle.GetMethodTable();

    if (pMT == NULL) 
    {
        fThrowException = TRUE;
        goto Exit;
    }

    if(!pMT->GetClass()->IsNested())
    {
        retTypeHandle = TypeHandle();
        goto Exit;
    }

    tkTypeDef = pMT->GetCl();
    
    if (FAILED(typeHandle.GetModule()->GetMDImport()->GetNestedClassProps(tkTypeDef, &tkTypeDef)))
    {
        fThrowException = TRUE;
        reKind = kBadImageFormatException;
        argName = NULL;
        goto Exit;
    }
    
    // Try the fast way first (if the declaring type has been loaded already).
    retTypeHandle = typeHandle.GetModule()->LookupTypeDef(tkTypeDef);
    if (retTypeHandle.IsNull())
    { 
         // OK, need to go the slow way and load the type first.
        HELPER_METHOD_FRAME_BEGIN_RET_1(refType);
        {
            retTypeHandle = ClassLoader::LoadTypeDefThrowing(typeHandle.GetModule(), tkTypeDef, 
                                                             ClassLoader::ThrowIfNotFound, 
                                                             ClassLoader::PermitUninstDefOrRef);
        }
        HELPER_METHOD_FRAME_END();
    }
Exit:

    END_SO_INTOLERANT_CODE;

    if (fThrowException)
    {
        FCThrowRes(reKind, argName);
    }

    RETURN_CLASS_OBJECT(retTypeHandle, refType);
  }
FCIMPLEND

FCIMPL2(FC_BOOL_RET, RuntimeTypeHandle::CanCastTo, ReflectClassBaseObject *pTypeUNSAFE, ReflectClassBaseObject *pTargetUNSAFE) {
    CONTRACTL {
        FCALL_CHECK;
    }
    CONTRACTL_END;
    
    
    REFLECTCLASSBASEREF refType = (REFLECTCLASSBASEREF)ObjectToOBJECTREF(pTypeUNSAFE);
    REFLECTCLASSBASEREF refTarget = (REFLECTCLASSBASEREF)ObjectToOBJECTREF(pTargetUNSAFE);

    if ((refType == NULL) || (refTarget == NULL)) 
        FCThrowRes(kArgumentNullException, W("Arg_InvalidHandle"));

    TypeHandle fromHandle = refType->GetType();
    TypeHandle toHandle = refTarget->GetType();

    BOOL iRetVal = 0;

    TypeHandle::CastResult r = fromHandle.CanCastToNoGC(toHandle);
    if (r == TypeHandle::MaybeCast)
    {
        HELPER_METHOD_FRAME_BEGIN_RET_2(refType, refTarget);
        iRetVal = fromHandle.CanCastTo(toHandle);
        HELPER_METHOD_FRAME_END();
    }
    else
    {
        iRetVal = (r == TypeHandle::CanCast);
    }

    // We allow T to be cast to Nullable<T>
    if (!iRetVal && Nullable::IsNullableType(toHandle) && !fromHandle.IsTypeDesc())
    {
        HELPER_METHOD_FRAME_BEGIN_RET_2(refType, refTarget);
        if (Nullable::IsNullableForType(toHandle, fromHandle.AsMethodTable())) 
        {
            iRetVal = TRUE;
        }
        HELPER_METHOD_FRAME_END();
    }
        
    FC_RETURN_BOOL(iRetVal);
}
FCIMPLEND

void QCALLTYPE RuntimeTypeHandle::GetTypeByNameUsingCARules(LPCWSTR pwzClassName, QCall::ModuleHandle pModule, QCall::ObjectHandleOnStack retType)
{
    QCALL_CONTRACT;
    
    TypeHandle typeHandle;

    BEGIN_QCALL;
        
    if (!pwzClassName)
        COMPlusThrowArgumentNull(W("className"),W("ArgumentNull_String"));

    typeHandle = TypeName::GetTypeUsingCASearchRules(pwzClassName, pModule->GetAssembly());

    GCX_COOP();
    retType.Set(typeHandle.GetManagedClassObject());

    END_QCALL;

    return;
}

void QCALLTYPE RuntimeTypeHandle::GetTypeByName(LPCWSTR pwzClassName, BOOL bThrowOnError, BOOL bIgnoreCase, BOOL bReflectionOnly,
                                                QCall::StackCrawlMarkHandle pStackMark, 
#ifdef FEATURE_HOSTED_BINDER
                                                ICLRPrivBinder * pPrivHostBinder,
#endif
                                                BOOL bLoadTypeFromPartialNameHack, QCall::ObjectHandleOnStack retType)
{
    QCALL_CONTRACT;
    
    TypeHandle typeHandle;
    
    BEGIN_QCALL;

    if (!pwzClassName)
            COMPlusThrowArgumentNull(W("className"),W("ArgumentNull_String"));

    GCX_COOP();
    {
        OBJECTREF keepAlive = NULL;

        // BEGIN_QCALL/END_QCALL define try/catch scopes for potential exceptions thrown when bThrowOnError is enabled.
        // Originally, in case of an exception the GCFrame was removed from the Thread's Frame chain in the catch block, in UnwindAndContinueRethrowHelperInsideCatch.
        // However, the catch block declared some local variables that overlapped the location of the now out of scope GCFrame and OBJECTREF, therefore corrupting
        // those values. Having the GCX_COOP/GCX_PREEMP switching GC modes, allowed a situation where in case of an exception, the thread would wait for a GC to complete
        // while still having the GCFrame in the Thread's Frame chain, but with a corrupt OBJECTREF due to stack location reuse in the catch block.
        // The solution is to force the removal of GCFrame (and the Frames above) from the Thread's Frame chain before entering the catch block, at the time of 
        // FrameWithCookieHolder's destruction.
        GCPROTECT_HOLDER(keepAlive);

        {
            GCX_PREEMP();
            typeHandle = TypeName::GetTypeManaged(pwzClassName, NULL, bThrowOnError, bIgnoreCase, bReflectionOnly, /*bProhibitAsmQualifiedName =*/ FALSE, pStackMark, bLoadTypeFromPartialNameHack, &keepAlive
#ifdef FEATURE_HOSTED_BINDER
                                                  , pPrivHostBinder
#endif
                );
        }

        if (!typeHandle.IsNull())
        {
            retType.Set(typeHandle.GetManagedClassObject());
        }
    }

    END_QCALL;

    return;
}

FCIMPL6(FC_BOOL_RET, RuntimeTypeHandle::SatisfiesConstraints, PTR_ReflectClassBaseObject pParamTypeUNSAFE, TypeHandle *typeContextArgs, INT32 typeContextCount, TypeHandle *methodContextArgs, INT32 methodContextCount, PTR_ReflectClassBaseObject pArgumentTypeUNSAFE);
{
    CONTRACTL {
        FCALL_CHECK;
        PRECONDITION(CheckPointer(typeContextArgs, NULL_OK));
        PRECONDITION(CheckPointer(methodContextArgs, NULL_OK));
    }
    CONTRACTL_END;
    
    REFLECTCLASSBASEREF refParamType = (REFLECTCLASSBASEREF)ObjectToOBJECTREF(pParamTypeUNSAFE);
    REFLECTCLASSBASEREF refArgumentType = (REFLECTCLASSBASEREF)ObjectToOBJECTREF(pArgumentTypeUNSAFE);

    TypeHandle thGenericParameter = refParamType->GetType();
    TypeHandle thGenericArgument = refArgumentType->GetType();
    BOOL bResult = FALSE; 
    SigTypeContext typeContext;

    Instantiation classInst;
    Instantiation methodInst;

    if (typeContextArgs != NULL)
    {
        classInst = Instantiation(typeContextArgs, typeContextCount);
    }
    
    if (methodContextArgs != NULL)
    {
        methodInst = Instantiation(methodContextArgs, methodContextCount);
    }

    SigTypeContext::InitTypeContext(classInst, methodInst, &typeContext);

    HELPER_METHOD_FRAME_BEGIN_RET_2(refParamType, refArgumentType);
    {
        bResult = thGenericParameter.AsGenericVariable()->SatisfiesConstraints(&typeContext, thGenericArgument);
    }
    HELPER_METHOD_FRAME_END();

    FC_RETURN_BOOL(bResult);      
}
FCIMPLEND

void QCALLTYPE RuntimeTypeHandle::GetInstantiation(EnregisteredTypeHandle pType, QCall::ObjectHandleOnStack retTypes, BOOL fAsRuntimeTypeArray)
{
    QCALL_CONTRACT;

    BEGIN_QCALL;

    TypeHandle typeHandle = TypeHandle::FromPtr(pType);
    Instantiation inst = typeHandle.GetInstantiation();
    GCX_COOP();
    retTypes.Set(CopyRuntimeTypeHandles(NULL, inst.GetRawArgs(), inst.GetNumArgs(), fAsRuntimeTypeArray ? CLASS__CLASS : CLASS__TYPE));
    END_QCALL;

    return;
}

void QCALLTYPE RuntimeTypeHandle::MakeArray(EnregisteredTypeHandle pTypeHandle, INT32 rank, QCall::ObjectHandleOnStack retType)
{
    QCALL_CONTRACT;

    TypeHandle arrayHandle;
    
    BEGIN_QCALL;
    arrayHandle = TypeHandle::FromPtr(pTypeHandle).MakeArray(rank);
    GCX_COOP();
    retType.Set(arrayHandle.GetManagedClassObject());
    END_QCALL;
    
    return;
}

void QCALLTYPE RuntimeTypeHandle::MakeSZArray(EnregisteredTypeHandle pTypeHandle, QCall::ObjectHandleOnStack retType)
{
    QCALL_CONTRACT;
    
    TypeHandle arrayHandle;
    
    BEGIN_QCALL;
    arrayHandle = TypeHandle::FromPtr(pTypeHandle).MakeSZArray();
    GCX_COOP();
    retType.Set(arrayHandle.GetManagedClassObject());
    END_QCALL;

    return;
}

void QCALLTYPE RuntimeTypeHandle::MakePointer(EnregisteredTypeHandle pTypeHandle, QCall::ObjectHandleOnStack retType)
{
    QCALL_CONTRACT;
    
    TypeHandle pointerHandle;
    
    BEGIN_QCALL;
    pointerHandle = TypeHandle::FromPtr(pTypeHandle).MakePointer();
    GCX_COOP();
    retType.Set(pointerHandle.GetManagedClassObject());
    END_QCALL;
    
    return;
}

void QCALLTYPE RuntimeTypeHandle::MakeByRef(EnregisteredTypeHandle pTypeHandle, QCall::ObjectHandleOnStack retType)
{
    QCALL_CONTRACT;
    
    TypeHandle byRefHandle;
    
    BEGIN_QCALL;
    byRefHandle = TypeHandle::FromPtr(pTypeHandle).MakeByRef();
    GCX_COOP();
    retType.Set(byRefHandle.GetManagedClassObject());
    END_QCALL;
    
    return;
}

BOOL QCALLTYPE RuntimeTypeHandle::IsCollectible(EnregisteredTypeHandle pTypeHandle)
{
    QCALL_CONTRACT;

    BOOL retVal = FALSE;

    BEGIN_QCALL;
    retVal = TypeHandle::FromPtr(pTypeHandle).GetLoaderAllocator()->IsCollectible();
    END_QCALL;

    return retVal;
}
    
void QCALLTYPE RuntimeTypeHandle::Instantiate(EnregisteredTypeHandle pTypeHandle, TypeHandle * pInstArray, INT32 cInstArray, QCall::ObjectHandleOnStack retType)
{
    QCALL_CONTRACT;
    
    TypeHandle type;

    BEGIN_QCALL;
    type = TypeHandle::FromPtr(pTypeHandle).Instantiate(Instantiation(pInstArray, cInstArray));
    GCX_COOP();
    retType.Set(type.GetManagedClassObject());
    END_QCALL;

    return;
}

void QCALLTYPE RuntimeTypeHandle::GetGenericTypeDefinition(EnregisteredTypeHandle pTypeHandle, QCall::ObjectHandleOnStack retType)
{
    QCALL_CONTRACT;
    
    TypeHandle typeDef;
    
    BEGIN_QCALL;
    
    TypeHandle genericType = TypeHandle::FromPtr(pTypeHandle);

    typeDef = ClassLoader::LoadTypeDefThrowing(genericType.GetModule(), 
                                                       genericType.GetMethodTable()->GetCl(),
                                                       ClassLoader::ThrowIfNotFound,
                                                       ClassLoader::PermitUninstDefOrRef);

    GCX_COOP();
    retType.Set(typeDef.GetManagedClassObject());

    END_QCALL;
    
    return;
}

FCIMPL2(FC_BOOL_RET, RuntimeTypeHandle::CompareCanonicalHandles, ReflectClassBaseObject *pLeftUNSAFE, ReflectClassBaseObject *pRightUNSAFE)
{
    FCALL_CONTRACT;
    
    REFLECTCLASSBASEREF refLeft = (REFLECTCLASSBASEREF)ObjectToOBJECTREF(pLeftUNSAFE);
    REFLECTCLASSBASEREF refRight = (REFLECTCLASSBASEREF)ObjectToOBJECTREF(pRightUNSAFE);

    if ((refLeft == NULL) || (refRight == NULL))
        FCThrowRes(kArgumentNullException, W("Arg_InvalidHandle"));

    FC_RETURN_BOOL(refLeft->GetType().GetCanonicalMethodTable() == refRight->GetType().GetCanonicalMethodTable());
}
FCIMPLEND

FCIMPL1(FC_BOOL_RET, RuntimeTypeHandle::HasInstantiation, PTR_ReflectClassBaseObject pTypeUNSAFE)
{
    FCALL_CONTRACT;       
    
    REFLECTCLASSBASEREF refType = (REFLECTCLASSBASEREF)ObjectToOBJECTREF(pTypeUNSAFE);

    if (refType == NULL)
        FCThrowRes(kArgumentNullException, W("Arg_InvalidHandle"));

    FC_RETURN_BOOL(refType->GetType().HasInstantiation());
}
FCIMPLEND

FCIMPL1(FC_BOOL_RET, RuntimeTypeHandle::IsGenericTypeDefinition, PTR_ReflectClassBaseObject pTypeUNSAFE)
{
    FCALL_CONTRACT;
    
    REFLECTCLASSBASEREF refType = (REFLECTCLASSBASEREF)ObjectToOBJECTREF(pTypeUNSAFE);

    if (refType == NULL)
        FCThrowRes(kArgumentNullException, W("Arg_InvalidHandle"));

    FC_RETURN_BOOL(refType->GetType().IsGenericTypeDefinition());
}
FCIMPLEND

FCIMPL1(FC_BOOL_RET, RuntimeTypeHandle::IsGenericVariable, PTR_ReflectClassBaseObject pTypeUNSAFE)
{
    FCALL_CONTRACT;
    
    REFLECTCLASSBASEREF refType = (REFLECTCLASSBASEREF)ObjectToOBJECTREF(pTypeUNSAFE);

    if (refType == NULL)
        FCThrowRes(kArgumentNullException, W("Arg_InvalidHandle"));

    FC_RETURN_BOOL(refType->GetType().IsGenericVariable());
}
FCIMPLEND

FCIMPL1(INT32, RuntimeTypeHandle::GetGenericVariableIndex, PTR_ReflectClassBaseObject pTypeUNSAFE)
{
    FCALL_CONTRACT;
    
    REFLECTCLASSBASEREF refType = (REFLECTCLASSBASEREF)ObjectToOBJECTREF(pTypeUNSAFE);

    if (refType == NULL)
        FCThrowRes(kArgumentNullException, W("Arg_InvalidHandle"));

    return (INT32)refType->GetType().AsGenericVariable()->GetIndex();
}
FCIMPLEND

FCIMPL1(FC_BOOL_RET, RuntimeTypeHandle::ContainsGenericVariables, PTR_ReflectClassBaseObject pTypeUNSAFE)
{
    FCALL_CONTRACT;

    REFLECTCLASSBASEREF refType = (REFLECTCLASSBASEREF)ObjectToOBJECTREF(pTypeUNSAFE);

    if (refType == NULL)
        FCThrowRes(kArgumentNullException, W("Arg_InvalidHandle"));

    FC_RETURN_BOOL(refType->GetType().ContainsGenericVariables());
}
FCIMPLEND

FCIMPL1(IMDInternalImport*, RuntimeTypeHandle::GetMetadataImport, ReflectClassBaseObject * pTypeUNSAFE)
{
    FCALL_CONTRACT;

    REFLECTCLASSBASEREF refType = (REFLECTCLASSBASEREF)ObjectToOBJECTREF(pTypeUNSAFE);

    if (refType == NULL)
        FCThrowRes(kArgumentNullException, W("Arg_InvalidHandle"));

    Module *pModule = refType->GetType().GetModule();

    return pModule->GetMDImport();
}
FCIMPLEND


//***********************************************************************************
//***********************************************************************************
//***********************************************************************************

void * QCALLTYPE RuntimeMethodHandle::GetFunctionPointer(MethodDesc * pMethod)
{
    QCALL_CONTRACT;
        
    void* funcPtr = 0;
    
    BEGIN_QCALL;

    funcPtr = (void*)pMethod->GetMultiCallableAddrOfCode();

    END_QCALL;

    return funcPtr;
}
    
FCIMPL1(LPCUTF8, RuntimeMethodHandle::GetUtf8Name, MethodDesc *pMethod) {
    CONTRACTL {
        FCALL_CHECK;
    }
    CONTRACTL_END;
    
    LPCUTF8 szName = NULL;
    
    if (!pMethod)
        FCThrowRes(kArgumentNullException, W("Arg_InvalidHandle"));
           
    szName = pMethod->GetName();

    _ASSERTE(CheckPointer(szName, NULL_OK));
    
    return szName;
}
FCIMPLEND

FCIMPL2(FC_BOOL_RET, RuntimeMethodHandle::MatchesNameHash, MethodDesc * pMethod, ULONG hash)
{
    FCALL_CONTRACT;

    FC_RETURN_BOOL(pMethod->MightHaveName(hash));
}
FCIMPLEND

FCIMPL1(StringObject*, RuntimeMethodHandle::GetName, MethodDesc *pMethod) {
    CONTRACTL {
        FCALL_CHECK;
    }
    CONTRACTL_END;
        
    if (!pMethod)
        FCThrowRes(kArgumentNullException, W("Arg_InvalidHandle"));
        
    STRINGREF refName = NULL;

    HELPER_METHOD_FRAME_BEGIN_RET_0();
    refName = StringObject::NewString(pMethod->GetName());
    HELPER_METHOD_FRAME_END();            
    
    return (StringObject*)OBJECTREFToObject(refName);
}
FCIMPLEND

FCIMPL1(INT32, RuntimeMethodHandle::GetAttributes, MethodDesc *pMethod) {
    CONTRACTL {
        FCALL_CHECK;
    }
    CONTRACTL_END;
    
    if (!pMethod)
        FCThrowRes(kArgumentNullException, W("Arg_InvalidHandle"));

    INT32 retVal = 0;        
    BEGIN_SO_INTOLERANT_CODE_NOTHROW(GetThread(), FCThrow(kStackOverflowException));
    retVal = (INT32)pMethod->GetAttrs();
    END_SO_INTOLERANT_CODE;
    return retVal;
}
FCIMPLEND

FCIMPL1(INT32, RuntimeMethodHandle::GetImplAttributes, ReflectMethodObject *pMethodUNSAFE) {
    CONTRACTL {
        FCALL_CHECK;
    }
    CONTRACTL_END;
    
    if (!pMethodUNSAFE)
        FCThrowRes(kArgumentNullException, W("Arg_InvalidHandle"));

    MethodDesc* pMethod = pMethodUNSAFE->GetMethod();
    INT32 attributes = 0;

    if (IsNilToken(pMethod->GetMemberDef()))
        return attributes;
    
    BEGIN_SO_INTOLERANT_CODE_NOTHROW(GetThread(), FCThrow(kStackOverflowException));
    {
        attributes = (INT32)pMethod->GetImplAttrs();
    }
    END_SO_INTOLERANT_CODE;

    return attributes;
}
FCIMPLEND
    

FCIMPL1(ReflectClassBaseObject*, RuntimeMethodHandle::GetDeclaringType, MethodDesc *pMethod) {
    CONTRACTL {
        FCALL_CHECK;
        PRECONDITION(CheckPointer(pMethod));
    }
    CONTRACTL_END;
    
    if (!pMethod)
        FCThrowRes(kArgumentNullException, W("Arg_InvalidHandle"));
        
    MethodTable *pMT = pMethod->GetMethodTable();
    TypeHandle declType(pMT);
    if (pMT->IsArray()) 
    {
        HELPER_METHOD_FRAME_BEGIN_RET_0();   
        
        // Load the TypeDesc for the array type.  Note the returned type is approximate, i.e.
        // if shared between reference array types then we will get object[] back.
        DWORD rank = pMT->GetRank();
        TypeHandle elemType = pMT->GetApproxArrayElementTypeHandle();
        declType = ClassLoader::LoadArrayTypeThrowing(elemType, pMT->GetInternalCorElementType(), rank);
        HELPER_METHOD_FRAME_END();            
    }
    RETURN_CLASS_OBJECT(declType, NULL);
}
FCIMPLEND

FCIMPL1(INT32, RuntimeMethodHandle::GetSlot, MethodDesc *pMethod) {
    CONTRACTL {
        FCALL_CHECK;
    }
    CONTRACTL_END;
    
    if (!pMethod)
        FCThrowRes(kArgumentNullException, W("Arg_InvalidHandle"));
        
    return (INT32)pMethod->GetSlot();
}
FCIMPLEND

FCIMPL3(Object *, SignatureNative::GetCustomModifiers, SignatureNative* pSignatureUNSAFE, 
    INT32 parameter, CLR_BOOL fRequired)
{   
    CONTRACTL {
        FCALL_CHECK;
    }
    CONTRACTL_END;

    struct 
    {
        SIGNATURENATIVEREF pSig;
        PTRARRAYREF retVal;
    } gc;

    gc.pSig = (SIGNATURENATIVEREF)pSignatureUNSAFE;
    gc.retVal = NULL;

    HELPER_METHOD_FRAME_BEGIN_RET_PROTECT(gc);
    {
        
        BYTE callConv = *(BYTE*)gc.pSig->GetCorSig();
        SigTypeContext typeContext;
        gc.pSig->GetTypeContext(&typeContext);
        MetaSig sig(gc.pSig->GetCorSig(), 
                    gc.pSig->GetCorSigSize(),
                    gc.pSig->GetModule(), 
                    &typeContext,
                    (callConv & IMAGE_CEE_CS_CALLCONV_MASK) == IMAGE_CEE_CS_CALLCONV_FIELD ? MetaSig::sigField : MetaSig::sigMember);
        _ASSERTE(callConv == sig.GetCallingConventionInfo());                 

        SigPointer argument(NULL, 0);

        PRECONDITION(sig.GetCallingConvention() != IMAGE_CEE_CS_CALLCONV_FIELD || parameter == 1);

        if (parameter == 0) 
        {
            argument = sig.GetReturnProps();
        }
        else
        {
            for(INT32 i = 0; i < parameter; i++)
                sig.NextArg();

            argument = sig.GetArgProps();
        }
        
        //if (parameter < 0 || parameter > (INT32)sig.NumFixedArgs())
        //    FCThrowResVoid(kArgumentNullException, W("Arg_ArgumentOutOfRangeException")); 
        
        SigPointer sp = argument;
        Module* pModule = sig.GetModule();
        INT32 cMods = 0;
        CorElementType cmodType;

        CorElementType cmodTypeExpected = fRequired ? ELEMENT_TYPE_CMOD_REQD : ELEMENT_TYPE_CMOD_OPT;
        
        // Discover the number of required and optional custom modifiers.   
        while(TRUE)
        {
            BYTE data;
            IfFailThrow(sp.GetByte(&data));
            cmodType = (CorElementType)data;
            
            if (cmodType == ELEMENT_TYPE_CMOD_REQD || cmodType == ELEMENT_TYPE_CMOD_OPT)
            {
                if (cmodType == cmodTypeExpected)
                {
                    cMods ++;
                }
            }        
            else if (cmodType != ELEMENT_TYPE_SENTINEL) 
            {
                break;        
            }
            
            IfFailThrow(sp.GetToken(NULL));
        }

        // Reset sp and populate the arrays for the required and optional custom 
        // modifiers now that we know how long they should be. 
        sp = argument;

        MethodTable *pMT = MscorlibBinder::GetClass(CLASS__TYPE);
        TypeHandle arrayHandle = ClassLoader::LoadArrayTypeThrowing(TypeHandle(pMT), ELEMENT_TYPE_SZARRAY);

        gc.retVal = (PTRARRAYREF) AllocateArrayEx(arrayHandle, &cMods, 1);

        while(cMods != 0)
        {
            BYTE data;
            IfFailThrow(sp.GetByte(&data));
            cmodType = (CorElementType)data;

            mdToken token;
            IfFailThrow(sp.GetToken(&token));

            if (cmodType == cmodTypeExpected)
            {
                TypeHandle th = ClassLoader::LoadTypeDefOrRefOrSpecThrowing(pModule, token, 
                                                                            &typeContext,
                                                                            ClassLoader::ThrowIfNotFound, 
                                                                            ClassLoader::FailIfUninstDefOrRef);        
        
                OBJECTREF refType = th.GetManagedClassObject();
                gc.retVal->SetAt(--cMods, refType);
            }
        }    
    }  
    HELPER_METHOD_FRAME_END();

    return OBJECTREFToObject(gc.retVal);
}
FCIMPLEND

FCIMPL1(INT32, RuntimeMethodHandle::GetMethodDef, ReflectMethodObject *pMethodUNSAFE) {
    CONTRACTL {
        FCALL_CHECK;
    }
    CONTRACTL_END;
    
    if (!pMethodUNSAFE)
        FCThrowRes(kArgumentNullException, W("Arg_InvalidHandle"));

    MethodDesc* pMethod = pMethodUNSAFE->GetMethod();

    if (pMethod->HasMethodInstantiation())
    {
        HELPER_METHOD_FRAME_BEGIN_RET_1(pMethodUNSAFE);
        {         
            pMethod = pMethod->StripMethodInstantiation();
        }
        HELPER_METHOD_FRAME_END();
    }

    INT32 tkMethodDef = (INT32)pMethod->GetMemberDef();
    _ASSERTE(TypeFromToken(tkMethodDef) == mdtMethodDef);
    
    if (IsNilToken(tkMethodDef) || TypeFromToken(tkMethodDef) != mdtMethodDef)
        return mdMethodDefNil;
    
    return tkMethodDef;
}
FCIMPLEND

FCIMPL6(void, SignatureNative::GetSignature,
    SignatureNative* pSignatureNativeUNSAFE, 
    PCCOR_SIGNATURE pCorSig, DWORD cCorSig,
    FieldDesc *pFieldDesc, ReflectMethodObject *pMethodUNSAFE, ReflectClassBaseObject *pDeclaringTypeUNSAFE) {
    CONTRACTL {
        FCALL_CHECK;
        PRECONDITION(pDeclaringTypeUNSAFE || pMethodUNSAFE->GetMethod()->IsDynamicMethod());
        PRECONDITION(CheckPointer(pCorSig, NULL_OK));
        PRECONDITION(CheckPointer(pMethodUNSAFE, NULL_OK));
        PRECONDITION(CheckPointer(pFieldDesc, NULL_OK));
    }
    CONTRACTL_END;

    struct
    {
        REFLECTCLASSBASEREF refDeclaringType;
        REFLECTMETHODREF refMethod;
        SIGNATURENATIVEREF pSig;
    } gc;

    gc.refDeclaringType = (REFLECTCLASSBASEREF)ObjectToOBJECTREF(pDeclaringTypeUNSAFE);
    gc.refMethod = (REFLECTMETHODREF)ObjectToOBJECTREF(pMethodUNSAFE);
    gc.pSig = (SIGNATURENATIVEREF)pSignatureNativeUNSAFE;

    MethodDesc *pMethod;
    TypeHandle declType;

    if (gc.refDeclaringType == NULL)
    {
        // for dynamic method, see precondition
        pMethod = gc.refMethod->GetMethod();
        declType = pMethod->GetMethodTable();
    }
    else
    {
        pMethod = gc.refMethod != NULL ? gc.refMethod->GetMethod() : NULL;
        declType = gc.refDeclaringType->GetType();
    }

    HELPER_METHOD_FRAME_BEGIN_PROTECT(gc);
    {        
        Module* pModule = declType.GetModule();
        
        if (pMethod)
        {
            pMethod->GetSig(&pCorSig, &cCorSig);
            if (pMethod->GetClassification() == mcInstantiated)
            {
                LoaderAllocator *pLoaderAllocator = pMethod->GetLoaderAllocator();
                if (pLoaderAllocator->IsCollectible())
                    gc.pSig->SetKeepAlive(pLoaderAllocator->GetExposedObject());
            }
        }
        else if (pFieldDesc)
            pFieldDesc->GetSig(&pCorSig, &cCorSig);
        
        gc.pSig->m_sig = pCorSig;    
        gc.pSig->m_cSig = cCorSig;    
        gc.pSig->m_pMethod = pMethod;    

        REFLECTCLASSBASEREF refDeclType = (REFLECTCLASSBASEREF)declType.GetManagedClassObject();
        gc.pSig->SetDeclaringType(refDeclType);

        PREFIX_ASSUME(pCorSig!= NULL);
        BYTE callConv = *(BYTE*)pCorSig;
        SigTypeContext typeContext;
        if (pMethod)
            SigTypeContext::InitTypeContext(
                pMethod, declType.GetClassOrArrayInstantiation(), pMethod->LoadMethodInstantiation(), &typeContext);
        else
            SigTypeContext::InitTypeContext(declType, &typeContext);
        MetaSig msig(pCorSig, cCorSig, pModule, &typeContext,
            (callConv & IMAGE_CEE_CS_CALLCONV_MASK) == IMAGE_CEE_CS_CALLCONV_FIELD ? MetaSig::sigField : MetaSig::sigMember);

        if (callConv == IMAGE_CEE_CS_CALLCONV_FIELD)
        {            
            msig.NextArgNormalized();

            OBJECTREF refRetType = msig.GetLastTypeHandleThrowing().GetManagedClassObject();
            gc.pSig->SetReturnType(refRetType);
        }
        else
        {
            gc.pSig->SetCallingConvention(msig.GetCallingConventionInfo());

            OBJECTREF refRetType = msig.GetRetTypeHandleThrowing().GetManagedClassObject();
            gc.pSig->SetReturnType(refRetType);

            INT32 nArgs = msig.NumFixedArgs();
            TypeHandle arrayHandle = ClassLoader::LoadArrayTypeThrowing(TypeHandle(g_pRuntimeTypeClass), ELEMENT_TYPE_SZARRAY);

            PTRARRAYREF ptrArrayarguments = (PTRARRAYREF) AllocateArrayEx(arrayHandle, &nArgs, 1);
            gc.pSig->SetArgumentArray(ptrArrayarguments);

            for (INT32 i = 0; i < nArgs; i++) 
            {
                msig.NextArg();

                OBJECTREF refArgType = msig.GetLastTypeHandleThrowing().GetManagedClassObject();
                gc.pSig->SetArgument(i, refArgType);
            }

            _ASSERTE(gc.pSig->m_returnType != NULL);
        }
    }
    HELPER_METHOD_FRAME_END();
}
FCIMPLEND

FCIMPL2(FC_BOOL_RET, SignatureNative::CompareSig, SignatureNative* pLhsUNSAFE, SignatureNative* pRhsUNSAFE)
{
    FCALL_CONTRACT;
    
    INT32 ret = 0;

    struct
    {
        SIGNATURENATIVEREF pLhs;
        SIGNATURENATIVEREF pRhs;
    } gc;

    gc.pLhs = (SIGNATURENATIVEREF)pLhsUNSAFE;
    gc.pRhs = (SIGNATURENATIVEREF)pRhsUNSAFE;

    HELPER_METHOD_FRAME_BEGIN_RET_PROTECT(gc);
    {
        ret = MetaSig::CompareMethodSigs(
            gc.pLhs->GetCorSig(), gc.pLhs->GetCorSigSize(), gc.pLhs->GetModule(), NULL, 
            gc.pRhs->GetCorSig(), gc.pRhs->GetCorSigSize(), gc.pRhs->GetModule(), NULL);    
    }
    HELPER_METHOD_FRAME_END();
    FC_RETURN_BOOL(ret);
}
FCIMPLEND

#if FEATURE_LEGACYNETCF
FCIMPL4(FC_BOOL_RET, SignatureNative::CompareSigForAppCompat, SignatureNative* pLhsUNSAFE, ReflectClassBaseObject * pTypeLhsUNSAFE, SignatureNative* pRhsUNSAFE, ReflectClassBaseObject * pTypeRhsUNSAFE)
{
    FCALL_CONTRACT;
    
    INT32 ret = 0;

    struct
    {
        SIGNATURENATIVEREF pLhs;
        REFLECTCLASSBASEREF refTypeLhs;
        SIGNATURENATIVEREF pRhs;
        REFLECTCLASSBASEREF refTypeRhs;
    } gc;

    gc.pLhs = (SIGNATURENATIVEREF)pLhsUNSAFE;
    gc.refTypeLhs = (REFLECTCLASSBASEREF)ObjectToOBJECTREF(pTypeLhsUNSAFE);
    gc.pRhs = (SIGNATURENATIVEREF)pRhsUNSAFE;
    gc.refTypeRhs = (REFLECTCLASSBASEREF)ObjectToOBJECTREF(pTypeRhsUNSAFE);

    if ((gc.refTypeLhs == NULL) || (gc.refTypeRhs == NULL))
        FCThrowRes(kArgumentNullException, W("Arg_InvalidHandle"));

    TypeHandle typeHandle1 = gc.refTypeLhs->GetType();
    TypeHandle typeHandle2 = gc.refTypeRhs->GetType();

    // The type contexts will be used in substituting formal type arguments in generic types.
    SigTypeContext typeContext1(typeHandle1);
    SigTypeContext typeContext2(typeHandle2);

    HELPER_METHOD_FRAME_BEGIN_RET_PROTECT(gc);
    {
        MetaSig metaSig1(gc.pLhs->GetCorSig(), gc.pLhs->GetCorSigSize(), gc.pLhs->GetModule(), &typeContext1);
        MetaSig metaSig2(gc.pRhs->GetCorSig(), gc.pRhs->GetCorSigSize(), gc.pRhs->GetModule(), &typeContext2);

        ret = MetaSig::CompareMethodSigs(metaSig1, metaSig2, FALSE);
    }
    HELPER_METHOD_FRAME_END();
    FC_RETURN_BOOL(ret);
}
FCIMPLEND
#endif

void QCALLTYPE RuntimeMethodHandle::GetMethodInstantiation(MethodDesc * pMethod, QCall::ObjectHandleOnStack retTypes, BOOL fAsRuntimeTypeArray)
{
    QCALL_CONTRACT;
            
    BEGIN_QCALL;
    Instantiation inst = pMethod->LoadMethodInstantiation();

    GCX_COOP();
    retTypes.Set(CopyRuntimeTypeHandles(NULL, inst.GetRawArgs(), inst.GetNumArgs(), fAsRuntimeTypeArray ? CLASS__CLASS : CLASS__TYPE));
    END_QCALL;

    return;
}
    
FCIMPL1(FC_BOOL_RET, RuntimeMethodHandle::HasMethodInstantiation, MethodDesc * pMethod)
{
    FCALL_CONTRACT;

    FC_RETURN_BOOL(pMethod->HasMethodInstantiation());
}
FCIMPLEND

FCIMPL1(FC_BOOL_RET, RuntimeMethodHandle::IsGenericMethodDefinition, MethodDesc * pMethod)
{
    FCALL_CONTRACT;

    FC_RETURN_BOOL(pMethod->IsGenericMethodDefinition());
}
FCIMPLEND

FCIMPL1(FC_BOOL_RET, RuntimeMethodHandle::IsDynamicMethod, MethodDesc * pMethod)
{
    FCALL_CONTRACT;

    FC_RETURN_BOOL(pMethod->IsNoMetadata());
}
FCIMPLEND

FCIMPL1(Object*, RuntimeMethodHandle::GetResolver, MethodDesc * pMethod)
{
    FCALL_CONTRACT;

    if (!pMethod)
        FCThrowRes(kArgumentNullException, W("Arg_InvalidHandle"));

    OBJECTREF resolver = NULL;
    if (pMethod->IsLCGMethod())
    {
        resolver = pMethod->AsDynamicMethodDesc()->GetLCGMethodResolver()->GetManagedResolver();
    }
    return OBJECTREFToObject(resolver);
}
FCIMPLEND

void QCALLTYPE RuntimeMethodHandle::Destroy(MethodDesc * pMethod)
{
    QCALL_CONTRACT;

    BEGIN_QCALL;

    if (pMethod == NULL)
        COMPlusThrowArgumentNull(NULL, W("Arg_InvalidHandle"));
    
    DynamicMethodDesc* pDynamicMethodDesc = pMethod->AsDynamicMethodDesc();

    GCX_COOP();

    // Destroy should be called only if the managed part is gone.
    _ASSERTE(OBJECTREFToObject(pDynamicMethodDesc->GetLCGMethodResolver()->GetManagedResolver()) == NULL);

    // Fire Unload Dynamic Method Event here
    ETW::MethodLog::DynamicMethodDestroyed(pMethod);

    pDynamicMethodDesc->Destroy();

    END_QCALL;
    }
    
FCIMPL1(FC_BOOL_RET, RuntimeMethodHandle::IsTypicalMethodDefinition, ReflectMethodObject *pMethodUNSAFE)
{
    FCALL_CONTRACT;

    if (!pMethodUNSAFE)
        FCThrowRes(kArgumentNullException, W("Arg_InvalidHandle"));

    MethodDesc* pMethod = pMethodUNSAFE->GetMethod();

    FC_RETURN_BOOL(pMethod->IsTypicalMethodDefinition());
}
FCIMPLEND
    
void QCALLTYPE RuntimeMethodHandle::GetTypicalMethodDefinition(MethodDesc * pMethod, QCall::ObjectHandleOnStack refMethod)
    {            
    QCALL_CONTRACT;

    BEGIN_QCALL;
#ifdef _DEBUG
    {
        GCX_COOP();
        _ASSERTE(((ReflectMethodObject *)(*refMethod.m_ppObject))->GetMethod() == pMethod);
    }
#endif
    MethodDesc *pMethodTypical = pMethod->LoadTypicalMethodDefinition();
    if (pMethodTypical != pMethod)
    {
        GCX_COOP();
        refMethod.Set(pMethodTypical->GetStubMethodInfo());
    }
    END_QCALL;
    
    return;
}

void QCALLTYPE RuntimeMethodHandle::StripMethodInstantiation(MethodDesc * pMethod, QCall::ObjectHandleOnStack refMethod)
    {            
    QCALL_CONTRACT;

    BEGIN_QCALL;

    if (!pMethod)
        COMPlusThrowArgumentNull(NULL, W("Arg_InvalidHandle"));

#ifdef _DEBUG
    {
        GCX_COOP();
        _ASSERTE(((ReflectMethodObject *)(*refMethod.m_ppObject))->GetMethod() == pMethod);
    }
#endif
    MethodDesc *pMethodStripped = pMethod->StripMethodInstantiation();
    if (pMethodStripped != pMethod)
    {
        GCX_COOP();
        refMethod.Set(pMethodStripped->GetStubMethodInfo());
    }
    END_QCALL;

    return;
}

// In the VM there might be more than one MethodDescs for a "method"
// examples are methods on generic types which may have additional instantiating stubs
//          and methods on value types which may have additional unboxing stubs.
//
// For generic methods we always hand out an instantiating stub except for a generic method definition
// For non-generic methods on generic types we need an instantiating stub if it's one of the following
//  - static method on a generic class
//  - static or instance method on a generic interface
//  - static or instance method on a generic value type
// The Reflection policy is to always hand out instantiating stubs in these cases
//
// For methods on non-generic value types we can use either the cannonical method or the unboxing stub
// The Reflection policy is to always hand out unboxing stubs if the methods are virtual methods
// The reason for this is that in the current implementation of the class loader, the v-table slots for 
// those methods point to unboxing stubs already. Note that this is just a implementation choice
// that might change in the future. But we should always keep this Reflection policy an invariant.
// 
// For virtual methods on generic value types (intersection of the two cases), reflection will hand
// out an unboxing instantiating stub
// 
// GetInstantiatingStub is called to: 
// 1. create an InstantiatedMethodDesc for a generic method when calling BindGenericArguments() on a generic
//    method. In this case instArray will not be null.
// 2. create an InstantiatedMethodDesc for a method in a generic class. In this case instArray will be null.
// 3. create an UnboxingStub for a method in a value type. In this case instArray will be null.
// For case 2 and 3, an instantiating stub or unboxing stub might not be needed in which case the original 
// MethodDesc is returned.
FCIMPL3(MethodDesc*, RuntimeMethodHandle::GetStubIfNeeded,
    MethodDesc *pMethod,
    ReflectClassBaseObject *pTypeUNSAFE,
    PtrArray* instArrayUNSAFE)
{
    CONTRACTL {
        FCALL_CHECK;
    }
    CONTRACTL_END;

    REFLECTCLASSBASEREF refType = (REFLECTCLASSBASEREF)ObjectToOBJECTREF(pTypeUNSAFE);
    PTRARRAYREF instArray = (PTRARRAYREF)ObjectToOBJECTREF(instArrayUNSAFE);

    if (refType == NULL)
        FCThrowRes(kArgumentNullException, W("Arg_InvalidHandle"));

    TypeHandle instType = refType->GetType();
    MethodDesc *pNewMethod = pMethod;

    // error conditions
    if (!pMethod)
        FCThrowRes(kArgumentException, W("Arg_InvalidHandle"));

    if (instType.IsNull())
        FCThrowRes(kArgumentNullException, W("Arg_InvalidHandle"));
    
    // Perf optimization: this logic is actually duplicated in FindOrCreateAssociatedMethodDescForReflection, but since it
    // is the more common case it's worth the duplicate check here to avoid the helper method frame
    if ( instArray == NULL &&
         ( pMethod->HasMethodInstantiation() || 
           ( !instType.IsValueType() && 
             ( !instType.HasInstantiation() || instType.IsGenericTypeDefinition() ) ) ) )
    {
        return pNewMethod;
    }

    HELPER_METHOD_FRAME_BEGIN_RET_2(refType, instArray);
    {
        TypeHandle *inst = NULL;
        DWORD ntypars = 0;

        if (instArray != NULL) 
        {
            ntypars = instArray->GetNumComponents();    

            size_t size = ntypars * sizeof(TypeHandle);
            if ((size / sizeof(TypeHandle)) != ntypars) // uint over/underflow
                COMPlusThrow(kArgumentException);
            inst = (TypeHandle*) _alloca(size);        

            for (DWORD i = 0; i < ntypars; i++) 
            {
                REFLECTCLASSBASEREF instRef = (REFLECTCLASSBASEREF)instArray->GetAt(i);

                if (instRef == NULL)
                    COMPlusThrowArgumentNull(W("inst"), W("ArgumentNull_ArrayElement"));

                inst[i] = instRef->GetType();
            }
        }

        pNewMethod = MethodDesc::FindOrCreateAssociatedMethodDescForReflection(pMethod, instType, Instantiation(inst, ntypars));
    }
    HELPER_METHOD_FRAME_END();

    return pNewMethod;
}
FCIMPLEND

        
FCIMPL2(MethodDesc*, RuntimeMethodHandle::GetMethodFromCanonical, MethodDesc *pMethod, ReflectClassBaseObject *pTypeUNSAFE)
{
    CONTRACTL {
        FCALL_CHECK;
        PRECONDITION(CheckPointer(pMethod));
    }
    CONTRACTL_END;
    
    REFLECTCLASSBASEREF refType = (REFLECTCLASSBASEREF)ObjectToOBJECTREF(pTypeUNSAFE);

    TypeHandle instType = refType->GetType();
    MethodDesc* pMDescInCanonMT = instType.GetMethodTable()->GetParallelMethodDesc(pMethod);

    return pMDescInCanonMT;
}
FCIMPLEND


FCIMPL2(MethodBody *, RuntimeMethodHandle::GetMethodBody, ReflectMethodObject *pMethodUNSAFE, ReflectClassBaseObject *pDeclaringTypeUNSAFE)
{      
    CONTRACTL 
    {
        FCALL_CHECK;
    }
    CONTRACTL_END;

    struct _gc
    {
        METHODBODYREF MethodBodyObj;
        EXCEPTIONHANDLINGCLAUSEREF EHClauseObj;
        LOCALVARIABLEINFOREF LocalVariableInfoObj;
        U1ARRAYREF                  U1Array;
        BASEARRAYREF                TempArray;
        REFLECTCLASSBASEREF         declaringType;
        REFLECTMETHODREF            refMethod;
    } gc;

    gc.MethodBodyObj = NULL;
    gc.EHClauseObj = NULL;
    gc.LocalVariableInfoObj = NULL;
    gc.U1Array              = NULL;
    gc.TempArray            = NULL;
    gc.declaringType        = (REFLECTCLASSBASEREF)ObjectToOBJECTREF(pDeclaringTypeUNSAFE);
    gc.refMethod = (REFLECTMETHODREF)ObjectToOBJECTREF(pMethodUNSAFE);


    if (!gc.refMethod)
        FCThrowRes(kArgumentNullException, W("Arg_InvalidHandle"));

    MethodDesc* pMethod = gc.refMethod->GetMethod();

    TypeHandle declaringType = gc.declaringType == NULL ? TypeHandle() : gc.declaringType->GetType();

    if (!pMethod->IsIL())
        return NULL;

    HELPER_METHOD_FRAME_BEGIN_RET_PROTECT(gc);
    {
        MethodDesc *pMethodIL = pMethod;
        if (pMethod->IsWrapperStub())
            pMethodIL = pMethod->GetWrappedMethodDesc();
        
        COR_ILMETHOD* pILHeader = pMethodIL->GetILHeader();
        
        if (pILHeader)
        {
            MethodTable * pExceptionHandlingClauseMT = MscorlibBinder::GetClass(CLASS__EH_CLAUSE);
            TypeHandle thEHClauseArray = ClassLoader::LoadArrayTypeThrowing(TypeHandle(pExceptionHandlingClauseMT), ELEMENT_TYPE_SZARRAY);

            MethodTable * pLocalVariableMT = MscorlibBinder::GetClass(CLASS__LOCAL_VARIABLE_INFO);
            TypeHandle thLocalVariableArray = ClassLoader::LoadArrayTypeThrowing(TypeHandle(pLocalVariableMT), ELEMENT_TYPE_SZARRAY);

            Module* pModule = pMethod->GetModule();
            COR_ILMETHOD_DECODER::DecoderStatus status;
            COR_ILMETHOD_DECODER header(pILHeader, pModule->GetMDImport(), &status);

            if (status != COR_ILMETHOD_DECODER::SUCCESS)
            {
                if (status == COR_ILMETHOD_DECODER::VERIFICATION_ERROR)
                {
                    // Throw a verification HR
                    COMPlusThrowHR(COR_E_VERIFICATION);
                }
                else
                {
                    COMPlusThrowHR(COR_E_BADIMAGEFORMAT);
                }
            }

            gc.MethodBodyObj = (METHODBODYREF)AllocateObject(MscorlibBinder::GetClass(CLASS__METHOD_BODY));
            
            gc.MethodBodyObj->m_maxStackSize = header.GetMaxStack();
            gc.MethodBodyObj->m_initLocals = !!(header.GetFlags() & CorILMethod_InitLocals);

            if (header.IsFat())
                gc.MethodBodyObj->m_localVarSigToken = header.GetLocalVarSigTok();
            else
                gc.MethodBodyObj->m_localVarSigToken = 0;

            // Allocate the array of IL and fill it in from the method header.
            BYTE* pIL = const_cast<BYTE*>(header.Code);
            COUNT_T cIL = header.GetCodeSize();
            gc.U1Array  = (U1ARRAYREF) AllocatePrimitiveArray(ELEMENT_TYPE_U1, cIL);

            SetObjectReference((OBJECTREF*)&gc.MethodBodyObj->m_IL, gc.U1Array, GetAppDomain());
            memcpyNoGCRefs(gc.MethodBodyObj->m_IL->GetDataPtr(), pIL, cIL);

            // Allocate the array of exception clauses.
            INT32 cEh = (INT32)header.EHCount();
            const COR_ILMETHOD_SECT_EH* ehInfo = header.EH;
            gc.TempArray = (BASEARRAYREF) AllocateArrayEx(thEHClauseArray, &cEh, 1);

            SetObjectReference((OBJECTREF*)&gc.MethodBodyObj->m_exceptionClauses, gc.TempArray, GetAppDomain());
            
            for (INT32 i = 0; i < cEh; i++)
            {                    
                COR_ILMETHOD_SECT_EH_CLAUSE_FAT ehBuff; 
                const COR_ILMETHOD_SECT_EH_CLAUSE_FAT* ehClause = 
                    (const COR_ILMETHOD_SECT_EH_CLAUSE_FAT*)ehInfo->EHClause(i, &ehBuff); 

                gc.EHClauseObj = (EXCEPTIONHANDLINGCLAUSEREF) AllocateObject(pExceptionHandlingClauseMT);

                gc.EHClauseObj->m_flags = ehClause->GetFlags();  
                gc.EHClauseObj->m_tryOffset = ehClause->GetTryOffset();
                gc.EHClauseObj->m_tryLength = ehClause->GetTryLength();
                gc.EHClauseObj->m_handlerOffset = ehClause->GetHandlerOffset();
                gc.EHClauseObj->m_handlerLength = ehClause->GetHandlerLength();
                
                if ((ehClause->GetFlags() & COR_ILEXCEPTION_CLAUSE_FILTER) == 0)
                    gc.EHClauseObj->m_catchToken = ehClause->GetClassToken();
                else
                    gc.EHClauseObj->m_filterOffset = ehClause->GetFilterOffset();
                
                gc.MethodBodyObj->m_exceptionClauses->SetAt(i, (OBJECTREF) gc.EHClauseObj);
                SetObjectReference((OBJECTREF*)&(gc.EHClauseObj->m_methodBody), (OBJECTREF)gc.MethodBodyObj, GetAppDomain());
            }     
           
            if (header.LocalVarSig != NULL)
            {
                SigTypeContext sigTypeContext(pMethod, declaringType, pMethod->LoadMethodInstantiation());
                MetaSig metaSig(header.LocalVarSig, 
                                header.cbLocalVarSig, 
                                pModule, 
                                &sigTypeContext, 
                                MetaSig::sigLocalVars);
                INT32 cLocals = metaSig.NumFixedArgs();
                gc.TempArray  = (BASEARRAYREF) AllocateArrayEx(thLocalVariableArray, &cLocals, 1);
                SetObjectReference((OBJECTREF*)&gc.MethodBodyObj->m_localVariables, gc.TempArray, GetAppDomain());

                for (INT32 i = 0; i < cLocals; i ++)
                {
                    gc.LocalVariableInfoObj = (LOCALVARIABLEINFOREF)AllocateObject(pLocalVariableMT);

                    gc.LocalVariableInfoObj->m_localIndex = i;
                    
                    metaSig.NextArg();

                    CorElementType eType;
                    IfFailThrow(metaSig.GetArgProps().PeekElemType(&eType));
                    if (ELEMENT_TYPE_PINNED == eType)
                        gc.LocalVariableInfoObj->m_bIsPinned = TRUE;

                    TypeHandle  tempType= metaSig.GetArgProps().GetTypeHandleThrowing(pModule, &sigTypeContext);       
                    OBJECTREF refLocalType = tempType.GetManagedClassObject();
                    gc.LocalVariableInfoObj->SetType(refLocalType);
                    gc.MethodBodyObj->m_localVariables->SetAt(i, (OBJECTREF) gc.LocalVariableInfoObj);
                }        
            }
            else
            {
                INT32 cLocals = 0;
                gc.TempArray  = (BASEARRAYREF) AllocateArrayEx(thLocalVariableArray, &cLocals, 1);
                SetObjectReference((OBJECTREF*)&gc.MethodBodyObj->m_localVariables, gc.TempArray, GetAppDomain());
            }
        }
    }
    HELPER_METHOD_FRAME_END();

    return (MethodBody*)OBJECTREFToObject(gc.MethodBodyObj);
}
FCIMPLEND

FCIMPL1(FC_BOOL_RET, RuntimeMethodHandle::IsConstructor, MethodDesc *pMethod)
{
    CONTRACTL {
        FCALL_CHECK;
        PRECONDITION(CheckPointer(pMethod));
    }
    CONTRACTL_END;

    BOOL ret = FALSE;
    BEGIN_SO_INTOLERANT_CODE_NOTHROW(GetThread(), FCThrow(kStackOverflowException));
    ret = (BOOL)pMethod->IsClassConstructorOrCtor();
    END_SO_INTOLERANT_CODE;
    FC_RETURN_BOOL(ret);
}
FCIMPLEND

FCIMPL1(Object*, RuntimeMethodHandle::GetLoaderAllocator, MethodDesc *pMethod)
{
    CONTRACTL {
        FCALL_CHECK;
    }
    CONTRACTL_END;

    OBJECTREF loaderAllocator = NULL;

    if (!pMethod)
        FCThrowRes(kArgumentNullException, W("Arg_InvalidHandle"));

    HELPER_METHOD_FRAME_BEGIN_RET_PROTECT(loaderAllocator);

    LoaderAllocator *pLoaderAllocator = pMethod->GetLoaderAllocator();
    loaderAllocator = pLoaderAllocator->GetExposedObject();

    HELPER_METHOD_FRAME_END();

    return OBJECTREFToObject(loaderAllocator);
}
FCIMPLEND

//*********************************************************************************************
//*********************************************************************************************
//*********************************************************************************************

FCIMPL1(StringObject*, RuntimeFieldHandle::GetName, ReflectFieldObject *pFieldUNSAFE) {
    CONTRACTL {
        FCALL_CHECK;
    }
    CONTRACTL_END;
    
    REFLECTFIELDREF refField = (REFLECTFIELDREF)ObjectToOBJECTREF(pFieldUNSAFE);
    if (!refField)
        FCThrowRes(kArgumentNullException, W("Arg_InvalidHandle"));
        
    FieldDesc *pField = refField->GetField();
    
    STRINGREF refString = NULL;
    HELPER_METHOD_FRAME_BEGIN_RET_1(refField);
    {
        refString = StringObject::NewString(pField->GetName());
    }
    HELPER_METHOD_FRAME_END();
    return (StringObject*)OBJECTREFToObject(refString);
}
FCIMPLEND
    
FCIMPL1(LPCUTF8, RuntimeFieldHandle::GetUtf8Name, FieldDesc *pField) {
    CONTRACTL {
        FCALL_CHECK;
        PRECONDITION(CheckPointer(pField));
    }
    CONTRACTL_END;
    
    LPCUTF8    szFieldName;
    
    if (FAILED(pField->GetName_NoThrow(&szFieldName)))
    {
        FCThrow(kBadImageFormatException);
    }
    return szFieldName;
}
FCIMPLEND

FCIMPL2(FC_BOOL_RET, RuntimeFieldHandle::MatchesNameHash, FieldDesc * pField, ULONG hash)
{
    FCALL_CONTRACT;

    FC_RETURN_BOOL(pField->MightHaveName(hash));
}
FCIMPLEND

FCIMPL1(INT32, RuntimeFieldHandle::GetAttributes, FieldDesc *pField) {
    CONTRACTL {
        FCALL_CHECK;
    }
    CONTRACTL_END;
    
    if (!pField)
        FCThrowRes(kArgumentNullException, W("Arg_InvalidHandle"));

    INT32 ret = 0;
    BEGIN_SO_INTOLERANT_CODE_NOTHROW(GetThread(), FCThrow(kStackOverflowException));
    ret = (INT32)pField->GetAttributes();
    END_SO_INTOLERANT_CODE;
    return ret;
}
FCIMPLEND
    
FCIMPL1(ReflectClassBaseObject*, RuntimeFieldHandle::GetApproxDeclaringType, FieldDesc *pField) {
    CONTRACTL {
        FCALL_CHECK;
    }
    CONTRACTL_END;
    
    if (!pField)
        FCThrowRes(kArgumentNullException, W("Arg_InvalidHandle"));
        
    TypeHandle th = TypeHandle(pField->GetApproxEnclosingMethodTable());  // <REVISIT_TODO> this needs to be checked - see bug 184355 </REVISIT_TODO>
    RETURN_CLASS_OBJECT(th, NULL);
}
FCIMPLEND

FCIMPL1(INT32, RuntimeFieldHandle::GetToken, ReflectFieldObject *pFieldUNSAFE) {
    CONTRACTL {
        FCALL_CHECK;
    }
    CONTRACTL_END;

    REFLECTFIELDREF refField = (REFLECTFIELDREF)ObjectToOBJECTREF(pFieldUNSAFE);
    if (!refField)
        FCThrowRes(kArgumentNullException, W("Arg_InvalidHandle"));
        
    FieldDesc *pField = refField->GetField();

    INT32 tkFieldDef = (INT32)pField->GetMemberDef();
    _ASSERTE(!IsNilToken(tkFieldDef) || tkFieldDef == mdFieldDefNil);
    return tkFieldDef;
}
FCIMPLEND

FCIMPL2(FieldDesc*, RuntimeFieldHandle::GetStaticFieldForGenericType, FieldDesc *pField, ReflectClassBaseObject *pDeclaringTypeUNSAFE)
{
    CONTRACTL {
        FCALL_CHECK;
    }
    CONTRACTL_END;

    REFLECTCLASSBASEREF refDeclaringType = (REFLECTCLASSBASEREF)ObjectToOBJECTREF(pDeclaringTypeUNSAFE);

    if ((refDeclaringType == NULL) || (pField == NULL))
        FCThrowRes(kArgumentNullException, W("Arg_InvalidHandle"));

    TypeHandle declaringType = refDeclaringType->GetType();
    
    if (!pField)
        FCThrowRes(kArgumentNullException, W("Arg_InvalidHandle"));
    if (declaringType.IsTypeDesc())
        FCThrowRes(kArgumentNullException, W("Arg_InvalidHandle"));
    MethodTable *pMT = declaringType.AsMethodTable();

    _ASSERTE(pField->IsStatic());
    if (pMT->HasGenericsStaticsInfo())
        pField = pMT->GetFieldDescByIndex(pField->GetApproxEnclosingMethodTable()->GetIndexForFieldDesc(pField));
    _ASSERTE(!pField->IsSharedByGenericInstantiations());
    _ASSERTE(pField->GetEnclosingMethodTable() == pMT);

    return pField;
}
FCIMPLEND

FCIMPL1(ReflectModuleBaseObject*, AssemblyHandle::GetManifestModule, AssemblyBaseObject* pAssemblyUNSAFE) {
    FCALL_CONTRACT;

    ASSEMBLYREF refAssembly = (ASSEMBLYREF)ObjectToOBJECTREF(pAssemblyUNSAFE);
    
    if (refAssembly == NULL)
        FCThrowRes(kArgumentNullException, W("Arg_InvalidHandle"));

    DomainAssembly *pAssembly = refAssembly->GetDomainAssembly();
    Assembly* currentAssembly = pAssembly->GetCurrentAssembly();

    if (currentAssembly == NULL)
        return NULL;

    Module *pModule = currentAssembly->GetManifestModule();
    DomainFile * pDomainFile = pModule->FindDomainFile(GetAppDomain());

#ifdef _DEBUG
    OBJECTREF orModule;
    
    HELPER_METHOD_FRAME_BEGIN_RET_1(refAssembly);
    orModule = (pDomainFile != NULL) ? pDomainFile->GetExposedModuleObjectIfExists() : NULL;
    if (orModule == NULL)
        orModule = pModule->GetExposedObject();
#else
    OBJECTREF orModule = (pDomainFile != NULL) ? pDomainFile->GetExposedModuleObjectIfExists() : NULL;
    if (orModule != NULL)
        return (ReflectModuleBaseObject*)OBJECTREFToObject(orModule);

    HELPER_METHOD_FRAME_BEGIN_RET_1(refAssembly);
    orModule = pModule->GetExposedObject();
#endif

    HELPER_METHOD_FRAME_END();
    return (ReflectModuleBaseObject*)OBJECTREFToObject(orModule);

}
FCIMPLEND

FCIMPL1(INT32, AssemblyHandle::GetToken, AssemblyBaseObject* pAssemblyUNSAFE) {
    FCALL_CONTRACT;
    
    ASSEMBLYREF refAssembly = (ASSEMBLYREF)ObjectToOBJECTREF(pAssemblyUNSAFE);
    
    if (refAssembly == NULL)
        FCThrowRes(kArgumentNullException, W("Arg_InvalidHandle"));

    DomainAssembly *pAssembly = refAssembly->GetDomainAssembly();
    mdAssembly token = mdAssemblyNil;
    
    IMDInternalImport *mdImport = pAssembly->GetCurrentAssembly()->GetManifestImport();
    
    if (mdImport != 0)
    {
        if (FAILED(mdImport->GetAssemblyFromScope(&token)))
        {
            FCThrow(kBadImageFormatException);
        }
    }
    
    return token;
}
FCIMPLEND

#ifdef FEATURE_APTCA
FCIMPL2(FC_BOOL_RET, AssemblyHandle::AptcaCheck, AssemblyBaseObject* pTargetAssemblyUNSAFE,  AssemblyBaseObject* pSourceAssemblyUNSAFE) 
{
    FCALL_CONTRACT;

    ASSEMBLYREF refTargetAssembly = (ASSEMBLYREF)ObjectToOBJECTREF(pTargetAssemblyUNSAFE);
    ASSEMBLYREF refSourceAssembly = (ASSEMBLYREF)ObjectToOBJECTREF(pSourceAssemblyUNSAFE);
    
    if ((refTargetAssembly == NULL) || (refSourceAssembly == NULL))
        FCThrowRes(kArgumentNullException, W("Arg_InvalidHandle"));

    DomainAssembly *pTargetAssembly = refTargetAssembly->GetDomainAssembly();
    DomainAssembly *pSourceAssembly = refSourceAssembly->GetDomainAssembly();
    
    if (pTargetAssembly == pSourceAssembly)
        FC_RETURN_BOOL(TRUE);

    BOOL bResult = TRUE;
    
    HELPER_METHOD_FRAME_BEGIN_RET_2(refSourceAssembly, refTargetAssembly);
    {
        bResult = ( pTargetAssembly->GetAssembly()->AllowUntrustedCaller() || // target assembly allows untrusted callers unconditionally
                    pSourceAssembly->GetSecurityDescriptor()->IsFullyTrusted());
    }
    HELPER_METHOD_FRAME_END();

    FC_RETURN_BOOL(bResult);
}
FCIMPLEND
#endif // FEATURE_APTCA
    
void QCALLTYPE ModuleHandle::GetPEKind(QCall::ModuleHandle pModule, DWORD* pdwPEKind, DWORD* pdwMachine)
{
    QCALL_CONTRACT;

    BEGIN_QCALL;
    pModule->GetFile()->GetPEKindAndMachine(pdwPEKind, pdwMachine);
    END_QCALL;
}

FCIMPL1(INT32, ModuleHandle::GetMDStreamVersion, ReflectModuleBaseObject * pModuleUNSAFE)
{
    FCALL_CONTRACT;

    REFLECTMODULEBASEREF refModule = (REFLECTMODULEBASEREF)ObjectToOBJECTREF(pModuleUNSAFE);

    if (refModule == NULL)
        FCThrowRes(kArgumentNullException, W("Arg_InvalidHandle"));

    Module *pModule = refModule->GetModule();
    
    if (pModule->IsResource())
        return 0;
    
    return pModule->GetMDImport()->GetMetadataStreamVersion();   
}
FCIMPLEND

void QCALLTYPE ModuleHandle::GetModuleType(QCall::ModuleHandle pModule, QCall::ObjectHandleOnStack retType)
{
    QCALL_CONTRACT;

    TypeHandle globalTypeHandle = TypeHandle();
    
    BEGIN_QCALL;
    
        EX_TRY
        {          
            globalTypeHandle = TypeHandle(pModule->GetGlobalMethodTable());
        }
        EX_SWALLOW_NONTRANSIENT;

        if (!globalTypeHandle.IsNull())
        {
            GCX_COOP();
            retType.Set(globalTypeHandle.GetManagedClassObject());
        }

    END_QCALL;

    return;
}

FCIMPL1(INT32, ModuleHandle::GetToken, ReflectModuleBaseObject * pModuleUNSAFE) {
    CONTRACTL {
        FCALL_CHECK;
    }
    CONTRACTL_END;

    REFLECTMODULEBASEREF refModule = (REFLECTMODULEBASEREF)ObjectToOBJECTREF(pModuleUNSAFE);

    if (refModule == NULL)
        FCThrowRes(kArgumentNullException, W("Arg_InvalidHandle"));

    Module *pModule = refModule->GetModule();
    
    if (pModule->IsResource())
        return mdModuleNil;
    
    return pModule->GetMDImport()->GetModuleFromScope();
}
FCIMPLEND

FCIMPL1(IMDInternalImport*, ModuleHandle::GetMetadataImport, ReflectModuleBaseObject * pModuleUNSAFE)
{
    FCALL_CONTRACT;

    REFLECTMODULEBASEREF refModule = (REFLECTMODULEBASEREF)ObjectToOBJECTREF(pModuleUNSAFE);

    if (refModule == NULL)
        FCThrowRes(kArgumentNullException, W("Arg_InvalidHandle"));

    Module *pModule = refModule->GetModule();

    if (pModule->IsResource())
        return NULL;

    return pModule->GetMDImport();
}
FCIMPLEND

BOOL QCALLTYPE ModuleHandle::ContainsPropertyMatchingHash(QCall::ModuleHandle pModule, INT32 tkProperty, ULONG hash)
{
    QCALL_CONTRACT;

    BOOL fContains = TRUE;

    BEGIN_QCALL;

    fContains = pModule->MightContainMatchingProperty(tkProperty, hash);

    END_QCALL;

    return fContains;
}

void QCALLTYPE ModuleHandle::ResolveType(QCall::ModuleHandle pModule, INT32 tkType, TypeHandle *typeArgs, INT32 typeArgsCount, TypeHandle *methodArgs, INT32 methodArgsCount, QCall::ObjectHandleOnStack retType)
{
    QCALL_CONTRACT;

    TypeHandle typeHandle;
    
    BEGIN_QCALL;
    
    _ASSERTE(!IsNilToken(tkType));

    SigTypeContext typeContext(Instantiation(typeArgs, typeArgsCount), Instantiation(methodArgs, methodArgsCount));
        typeHandle = ClassLoader::LoadTypeDefOrRefOrSpecThrowing(pModule, tkType, &typeContext, 
                                                          ClassLoader::ThrowIfNotFound, 
                                                          ClassLoader::PermitUninstDefOrRef);

    GCX_COOP();
    retType.Set(typeHandle.GetManagedClassObject());

    END_QCALL;

    return;
}

MethodDesc *QCALLTYPE ModuleHandle::ResolveMethod(QCall::ModuleHandle pModule, INT32 tkMemberRef, TypeHandle *typeArgs, INT32 typeArgsCount, TypeHandle *methodArgs, INT32 methodArgsCount)
{
    QCALL_CONTRACT;

    MethodDesc* pMD = NULL;
    
    BEGIN_QCALL;

    _ASSERTE(!IsNilToken(tkMemberRef));

    BOOL strictMetadataChecks = (TypeFromToken(tkMemberRef) == mdtMethodSpec);

    SigTypeContext typeContext(Instantiation(typeArgs, typeArgsCount), Instantiation(methodArgs, methodArgsCount));
    pMD = MemberLoader::GetMethodDescFromMemberDefOrRefOrSpec(pModule, tkMemberRef, &typeContext, strictMetadataChecks, FALSE);

    // This will get us the instantiating or unboxing stub if needed
    pMD = MethodDesc::FindOrCreateAssociatedMethodDescForReflection(pMD, pMD->GetMethodTable(), pMD->GetMethodInstantiation());

    END_QCALL;

    return pMD;
}

void QCALLTYPE ModuleHandle::ResolveField(QCall::ModuleHandle pModule, INT32 tkMemberRef, TypeHandle *typeArgs, INT32 typeArgsCount, TypeHandle *methodArgs, INT32 methodArgsCount, QCall::ObjectHandleOnStack retField)
{
    QCALL_CONTRACT;

    FieldDesc* pField = NULL;
    
    BEGIN_QCALL;

    _ASSERTE(!IsNilToken(tkMemberRef));

    SigTypeContext typeContext(Instantiation(typeArgs, typeArgsCount), Instantiation(methodArgs, methodArgsCount));
    pField = MemberLoader::GetFieldDescFromMemberDefOrRef(pModule, tkMemberRef, &typeContext, FALSE);
    GCX_COOP();
    retField.Set(pField->GetStubFieldInfo());

    END_QCALL;

    return;
}

void QCALLTYPE ModuleHandle::GetAssembly(QCall::ModuleHandle pModule, QCall::ObjectHandleOnStack retAssembly)
{
    QCALL_CONTRACT;

    DomainAssembly *pAssembly = NULL;

    BEGIN_QCALL;
    pAssembly = pModule->GetDomainAssembly();

    GCX_COOP();
    retAssembly.Set(pAssembly->GetExposedAssemblyObject());
    END_QCALL;

    return;
}

FCIMPL5(ReflectMethodObject*, ModuleHandle::GetDynamicMethod, ReflectMethodObject *pMethodUNSAFE, ReflectModuleBaseObject *pModuleUNSAFE, StringObject *name, U1Array *sig,  Object *resolver) {
    CONTRACTL {
        FCALL_CHECK;
        PRECONDITION(CheckPointer(name));
        PRECONDITION(CheckPointer(sig));
    }
    CONTRACTL_END;
    
    DynamicMethodDesc *pNewMD = NULL;

    struct
    {
        STRINGREF nameRef;
        OBJECTREF resolverRef;
        OBJECTREF methodRef;
        REFLECTMETHODREF retMethod;
        REFLECTMODULEBASEREF refModule;
    } gc;
    gc.nameRef = (STRINGREF)name;
    gc.resolverRef = (OBJECTREF)resolver;
    gc.methodRef = ObjectToOBJECTREF(pMethodUNSAFE);
    gc.retMethod = NULL;
    gc.refModule = (REFLECTMODULEBASEREF)ObjectToOBJECTREF(pModuleUNSAFE);

    if (gc.refModule == NULL)
        FCThrowRes(kArgumentNullException, W("Arg_InvalidHandle"));

    Module *pModule = gc.refModule->GetModule();

    HELPER_METHOD_FRAME_BEGIN_RET_PROTECT(gc);
    
    DomainFile *pDomainModule = pModule->GetDomainFile();

    U1ARRAYREF dataArray = (U1ARRAYREF)sig;
    DWORD sigSize = dataArray->GetNumComponents();
    NewHolder<BYTE> pSig(new BYTE[sigSize]);
    memcpy(pSig, dataArray->GetDataPtr(), sigSize);

    DWORD length = gc.nameRef->GetStringLength();
    NewArrayHolder<char> pName(new char[(length + 1) * 2]);
    pName[0] = '\0';
    length = WszWideCharToMultiByte(CP_UTF8, 0, gc.nameRef->GetBuffer(), length, pName, (length + 1) * 2 - sizeof(char), NULL, NULL);
    if (length)
        pName[length / sizeof(char)] = '\0';

    DynamicMethodTable *pMTForDynamicMethods = pDomainModule->GetDynamicMethodTable();
    pNewMD = pMTForDynamicMethods->GetDynamicMethod(pSig, sigSize, pName);
    _ASSERTE(pNewMD != NULL);
    // pNewMD now owns pSig and pName.
    pSig.SuppressRelease();
    pName.SuppressRelease();

    // create a handle to hold the resolver objectref
    OBJECTHANDLE resolverHandle = pDomainModule->GetAppDomain()->CreateLongWeakHandle(gc.resolverRef);
    pNewMD->GetLCGMethodResolver()->SetManagedResolver(resolverHandle);
    gc.retMethod = pNewMD->GetStubMethodInfo();
    gc.retMethod->SetKeepAlive(gc.resolverRef);

    LoaderAllocator *pLoaderAllocator = pModule->GetLoaderAllocator();

    if (pLoaderAllocator->IsCollectible())
        pLoaderAllocator->AddReference();
   
    HELPER_METHOD_FRAME_END();

    return (ReflectMethodObject*)OBJECTREFToObject(gc.retMethod);
}
FCIMPLEND

void QCALLTYPE RuntimeMethodHandle::GetCallerType(QCall::StackCrawlMarkHandle pStackMark, QCall::ObjectHandleOnStack retType)
{ 
    QCALL_CONTRACT;

    BEGIN_QCALL;
    GCX_COOP();
    MethodTable *pMT = NULL;

    pMT = SystemDomain::GetCallersType(pStackMark);

    if (pMT != NULL)
        retType.Set(pMT->GetManagedClassObject());

    END_QCALL;

    return;
}
