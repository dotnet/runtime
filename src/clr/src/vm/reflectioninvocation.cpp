// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//
//

#include "common.h"
#include "reflectioninvocation.h"
#include "invokeutil.h"
#include "object.h"
#include "class.h"
#include "method.hpp"
#include "typehandle.h"
#include "field.h"
#include "security.h"
#ifdef FEATURE_REMOTING
#include "remoting.h"
#endif
#include "eeconfig.h"
#include "vars.hpp"
#include "jitinterface.h"
#include "contractimpl.h"
#include "virtualcallstub.h"
#include "comdelegate.h"
#include "constrainedexecutionregion.h"
#include "generics.h"

#ifdef FEATURE_COMINTEROP
#include "interoputil.h"
#include "runtimecallablewrapper.h"
#endif

#include "dbginterface.h"
#include "argdestination.h"

// these flags are defined in XXXInfo.cs and only those that are used are replicated here
#define INVOCATION_FLAGS_UNKNOWN                    0x00000000
#define INVOCATION_FLAGS_INITIALIZED                0x00000001

// it's used for both method and field to signify that no access is allowed
#define INVOCATION_FLAGS_NO_INVOKE                  0x00000002

#define INVOCATION_FLAGS_NEED_SECURITY              0x00000004

// because field and method are different we can reuse the same bits
//method
#define INVOCATION_FLAGS_IS_CTOR                    0x00000010
#define INVOCATION_FLAGS_RISKY_METHOD               0x00000020
#define INVOCATION_FLAGS_W8P_API                    0x00000040
#define INVOCATION_FLAGS_IS_DELEGATE_CTOR           0x00000080
#define INVOCATION_FLAGS_CONTAINS_STACK_POINTERS    0x00000100
// field
#define INVOCATION_FLAGS_SPECIAL_FIELD              0x00000010
#define INVOCATION_FLAGS_FIELD_SPECIAL_CAST         0x00000020

// temporary flag used for flagging invocation of method vs ctor
#define INVOCATION_FLAGS_CONSTRUCTOR_INVOKE         0x10000000

/**************************************************************************/
/* if the type handle 'th' is a byref to a nullable type, return the
   type handle to the nullable type in the byref.  Otherwise return 
   the null type handle  */
static TypeHandle NullableTypeOfByref(TypeHandle th) {
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        SO_TOLERANT;
        MODE_ANY;
    }
    CONTRACTL_END;

    if (th.GetVerifierCorElementType() != ELEMENT_TYPE_BYREF)
        return TypeHandle();
    
    TypeHandle subType = th.AsTypeDesc()->GetTypeParam();
    if (!Nullable::IsNullableType(subType))
        return TypeHandle();
            
    return subType;
}

static void TryDemand(DWORD whatPermission, RuntimeExceptionKind reKind, LPCWSTR wszTag) {
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;


    EX_TRY {
        Security::SpecialDemand(SSWT_LATEBOUND_LINKDEMAND, whatPermission);
    } 
    EX_CATCH {
        COMPlusThrow(reKind, wszTag);
    }
    EX_END_CATCH_UNREACHABLE
}

static void TryCallMethodWorker(MethodDescCallSite* pMethodCallSite, ARG_SLOT* args, Frame* pDebuggerCatchFrame)
{
    // Use static contracts b/c we have SEH.
    STATIC_CONTRACT_THROWS;
    STATIC_CONTRACT_GC_TRIGGERS;
    STATIC_CONTRACT_MODE_ANY;

    struct Param: public NotifyOfCHFFilterWrapperParam
    {
        MethodDescCallSite * pMethodCallSite;
        ARG_SLOT* args;
    } param;

    param.pFrame = pDebuggerCatchFrame;
    param.pMethodCallSite = pMethodCallSite;
    param.args = args;

    PAL_TRY(Param *, pParam, &param)
    {
        pParam->pMethodCallSite->CallWithValueTypes(pParam->args);
    }
    PAL_EXCEPT_FILTER(NotifyOfCHFFilterWrapper)
    {
        // Should never reach here b/c handler should always continue search.
        _ASSERTE(false);
    }
    PAL_ENDTRY
}

// Warning: This method has subtle differences from CallDescrWorkerReflectionWrapper
// In particular that one captures watson bucket data and corrupting exception severity,
// then transfers that data to the newly produced TargetInvocationException. This one
// doesn't take those same steps. 
//
static void TryCallMethod(MethodDescCallSite* pMethodCallSite, ARG_SLOT* args) {
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;

    OBJECTREF ppException = NULL;
    GCPROTECT_BEGIN(ppException);

    // The sole purpose of having this frame is to tell the debugger that we have a catch handler here 
    // which may swallow managed exceptions.  The debugger needs this in order to send a 
    // CatchHandlerFound (CHF) notification.
    FrameWithCookie<DebuggerU2MCatchHandlerFrame> catchFrame;
    EX_TRY {
        TryCallMethodWorker(pMethodCallSite, args, &catchFrame);
    } 
    EX_CATCH {
        ppException = GET_THROWABLE();
        _ASSERTE(ppException);
    }
    EX_END_CATCH(RethrowTransientExceptions)
    catchFrame.Pop();

    // It is important to re-throw outside the catch block because re-throwing will invoke
    // the jitter and managed code and will cause us to use more than the backout stack limit.
    if (ppException != NULL) 
    {
        // If we get here we need to throw an TargetInvocationException
        OBJECTREF except = InvokeUtil::CreateTargetExcept(&ppException);
        COMPlusThrow(except);
    }
    GCPROTECT_END();
}




FCIMPL5(Object*, RuntimeFieldHandle::GetValue, ReflectFieldObject *pFieldUNSAFE, Object *instanceUNSAFE, ReflectClassBaseObject *pFieldTypeUNSAFE, ReflectClassBaseObject *pDeclaringTypeUNSAFE, CLR_BOOL *pDomainInitialized) {
    CONTRACTL {
        FCALL_CHECK;
    }
    CONTRACTL_END;

    struct _gc
    {
        OBJECTREF target;
        REFLECTCLASSBASEREF pFieldType;
        REFLECTCLASSBASEREF pDeclaringType;
        REFLECTFIELDREF refField;
    }gc;

    gc.target = ObjectToOBJECTREF(instanceUNSAFE);
    gc.pFieldType = (REFLECTCLASSBASEREF)ObjectToOBJECTREF(pFieldTypeUNSAFE);
    gc.pDeclaringType = (REFLECTCLASSBASEREF)ObjectToOBJECTREF(pDeclaringTypeUNSAFE);
    gc.refField = (REFLECTFIELDREF)ObjectToOBJECTREF(pFieldUNSAFE);

    if ((gc.pFieldType == NULL) || (gc.refField == NULL))
        FCThrowRes(kArgumentNullException, W("Arg_InvalidHandle"));

    TypeHandle fieldType = gc.pFieldType->GetType();
    TypeHandle declaringType = (gc.pDeclaringType != NULL) ? gc.pDeclaringType->GetType() : TypeHandle();
    
    Assembly *pAssem;
    if (declaringType.IsNull())
    {
        // global field
        pAssem = gc.refField->GetField()->GetModule()->GetAssembly();
    }
    else
    {
        pAssem = declaringType.GetAssembly();
    }

    if (pAssem->IsIntrospectionOnly())
        FCThrowEx(kInvalidOperationException, IDS_EE_CODEEXECUTION_IN_INTROSPECTIVE_ASSEMBLY, NULL, NULL, NULL);

    // We should throw NotSupportedException here. 
    // But for backward compatibility we are throwing FieldAccessException instead.
    if (pAssem->IsDynamic() && !pAssem->HasRunAccess())
        FCThrow(kFieldAccessException);

    OBJECTREF rv = NULL; // not protected

    HELPER_METHOD_FRAME_BEGIN_RET_PROTECT(gc);
    // There can be no GC after this until the Object is returned.
    rv = InvokeUtil::GetFieldValue(gc.refField->GetField(), fieldType, &gc.target, declaringType, pDomainInitialized);
    HELPER_METHOD_FRAME_END();

    return OBJECTREFToObject(rv);
}
FCIMPLEND

FCIMPL5(void, ReflectionInvocation::PerformVisibilityCheckOnField, FieldDesc *pFieldDesc, Object *target, ReflectClassBaseObject *pDeclaringTypeUNSAFE, DWORD attr, DWORD invocationFlags) {
    CONTRACTL {
        FCALL_CHECK;
        PRECONDITION(CheckPointer(pFieldDesc));
        PRECONDITION(CheckPointer(pDeclaringTypeUNSAFE));
    }
    CONTRACTL_END;

#ifndef FEATURE_CORECLR
    // Security checks are expensive as they involve stack walking. Avoid them if we can.
    // In immersive we don't allow private reflection to framework code. So we need to perform
    // the access check even if all the domains on the stack are fully trusted.
    if (Security::AllDomainsOnStackFullyTrusted() && !AppX::IsAppXProcess())
        return;
#endif

    REFLECTCLASSBASEREF refDeclaringType = (REFLECTCLASSBASEREF)ObjectToOBJECTREF(pDeclaringTypeUNSAFE);

    TypeHandle declaringType = refDeclaringType->GetType();
    OBJECTREF targetObj = ObjectToOBJECTREF(target);

    HELPER_METHOD_FRAME_BEGIN_2(targetObj, refDeclaringType);

    if ((invocationFlags & INVOCATION_FLAGS_SPECIAL_FIELD) != 0) {
        // Verify that this is not a Final Field
        if (IsFdInitOnly(attr))
            TryDemand(SECURITY_SERIALIZATION, kFieldAccessException, W("Acc_ReadOnly"));
        if (IsFdHasFieldRVA(attr))
            TryDemand(SECURITY_SKIP_VER, kFieldAccessException, W("Acc_RvaStatic"));
    }

    if ((invocationFlags & INVOCATION_FLAGS_NEED_SECURITY) != 0) {
        // Verify the callee/caller access

        bool targetRemoted = FALSE;

#ifndef FEATURE_CORECLR
        targetRemoted = targetObj != NULL && InvokeUtil::IsTargetRemoted(pFieldDesc, targetObj->GetMethodTable());
#endif //FEATURE_CORECLR

        RefSecContext sCtx(InvokeUtil::GetInvocationAccessCheckType(targetRemoted));

        MethodTable* pInstanceMT = NULL;
        if (targetObj != NULL && !pFieldDesc->IsStatic()) {
            TypeHandle targetType = targetObj->GetTypeHandle();
            if (!targetType.IsTypeDesc())
                pInstanceMT = targetType.AsMethodTable();
        }

        // Perform the normal access check (caller vs field).
        InvokeUtil::CanAccessField(&sCtx,
                                   declaringType.GetMethodTable(),
                                   pInstanceMT,
                                   pFieldDesc);
    }
    HELPER_METHOD_FRAME_END();
}
FCIMPLEND

FCIMPL2(FC_BOOL_RET, ReflectionInvocation::CanValueSpecialCast, ReflectClassBaseObject *pValueTypeUNSAFE, ReflectClassBaseObject *pTargetTypeUNSAFE) {
    CONTRACTL {
        FCALL_CHECK;
        PRECONDITION(CheckPointer(pValueTypeUNSAFE));
        PRECONDITION(CheckPointer(pTargetTypeUNSAFE));
    }
    CONTRACTL_END;
    
    REFLECTCLASSBASEREF refValueType = (REFLECTCLASSBASEREF)ObjectToOBJECTREF(pValueTypeUNSAFE);
    REFLECTCLASSBASEREF refTargetType = (REFLECTCLASSBASEREF)ObjectToOBJECTREF(pTargetTypeUNSAFE);

    TypeHandle valueType = refValueType->GetType();
    TypeHandle targetType = refTargetType->GetType();

    // we are here only if the target type is a primitive, an enum or a pointer

    CorElementType targetCorElement = targetType.GetVerifierCorElementType();

    BOOL ret = TRUE;
    HELPER_METHOD_FRAME_BEGIN_RET_2(refValueType, refTargetType);
    // the field type is a pointer
    if (targetCorElement == ELEMENT_TYPE_PTR || targetCorElement == ELEMENT_TYPE_FNPTR) {
        // the object must be an IntPtr or a System.Reflection.Pointer
        if (valueType == TypeHandle(MscorlibBinder::GetClass(CLASS__INTPTR))) {
            //
            // it's an IntPtr, it's good. Demand SkipVerification and proceed

            Security::SpecialDemand(SSWT_LATEBOUND_LINKDEMAND, SECURITY_SKIP_VER);
        }
        //
        // it's a System.Reflection.Pointer object

        // void* assigns to any pointer. Otherwise the type of the pointer must match
        else if (!InvokeUtil::IsVoidPtr(targetType)) {
            if (!valueType.CanCastTo(targetType))
                ret = FALSE;
            else
                // demand SkipVerification and proceed
                Security::SpecialDemand(SSWT_LATEBOUND_LINKDEMAND, SECURITY_SKIP_VER);
        }
        else
            // demand SkipVerification and proceed
            Security::SpecialDemand(SSWT_LATEBOUND_LINKDEMAND, SECURITY_SKIP_VER);
    } else {
        // the field type is an enum or a primitive. To have any chance of assignement the object type must
        // be an enum or primitive as well.
        // So get the internal cor element and that must be the same or widen
        CorElementType valueCorElement = valueType.GetVerifierCorElementType();
        if (InvokeUtil::IsPrimitiveType(valueCorElement))
            ret = (InvokeUtil::CanPrimitiveWiden(targetCorElement, valueCorElement)) ? TRUE : FALSE;
        else
            ret = FALSE;
    }
    HELPER_METHOD_FRAME_END();
    FC_RETURN_BOOL(ret);
}
FCIMPLEND

FCIMPL3(Object*, ReflectionInvocation::AllocateValueType, ReflectClassBaseObject *pTargetTypeUNSAFE, Object *valueUNSAFE, CLR_BOOL fForceTypeChange) {
    CONTRACTL {
        FCALL_CHECK;
        PRECONDITION(CheckPointer(pTargetTypeUNSAFE));
        PRECONDITION(CheckPointer(valueUNSAFE, NULL_OK));
    }
    CONTRACTL_END;

    struct _gc
    {
        REFLECTCLASSBASEREF refTargetType;
        OBJECTREF value;
        OBJECTREF obj;
    }gc;

    gc.value = ObjectToOBJECTREF(valueUNSAFE);
    gc.obj = gc.value;
    gc.refTargetType = (REFLECTCLASSBASEREF)ObjectToOBJECTREF(pTargetTypeUNSAFE);

    TypeHandle targetType = gc.refTargetType->GetType();

    HELPER_METHOD_FRAME_BEGIN_RET_PROTECT(gc);
    CorElementType targetElementType = targetType.GetSignatureCorElementType();
    if (InvokeUtil::IsPrimitiveType(targetElementType) || targetElementType == ELEMENT_TYPE_VALUETYPE)
    {
        MethodTable* allocMT = targetType.AsMethodTable();
        if (gc.value != NULL)
        {
            // ignore the type of the incoming box if fForceTypeChange is set
            // and the target type is not nullable
            if (!fForceTypeChange || Nullable::IsNullableType(targetType))
                allocMT = gc.value->GetMethodTable();
        }

        // for null Nullable<T> we don't want a default value being created.  
        // just allow the null value to be passed, as it will be converted to 
        // a true nullable 
        if (!(gc.value == NULL && Nullable::IsNullableType(targetType)))
        {
            // boxed value type are 'read-only' in the sence that you can't
            // only the implementor of the value type can expose mutators.
            // To insure byrefs don't mutate value classes in place, we make
            // a copy (and if we were not given one, we create a null value type
            // instance.
            gc.obj = allocMT->Allocate();

            if (gc.value != NULL)
                    CopyValueClassUnchecked(gc.obj->UnBox(), gc.value->UnBox(), allocMT);
        }
    }

    HELPER_METHOD_FRAME_END();

    return OBJECTREFToObject(gc.obj);
}
FCIMPLEND

FCIMPL7(void, RuntimeFieldHandle::SetValue, ReflectFieldObject *pFieldUNSAFE, Object *targetUNSAFE, Object *valueUNSAFE, ReflectClassBaseObject *pFieldTypeUNSAFE, DWORD attr, ReflectClassBaseObject *pDeclaringTypeUNSAFE, CLR_BOOL *pDomainInitialized) {
    CONTRACTL {
        FCALL_CHECK;
    }
    CONTRACTL_END;

    struct _gc {
        OBJECTREF       target;
        OBJECTREF       value;
        REFLECTCLASSBASEREF fieldType;
        REFLECTCLASSBASEREF declaringType;
        REFLECTFIELDREF refField;
    } gc;

    gc.target   = ObjectToOBJECTREF(targetUNSAFE);
    gc.value    = ObjectToOBJECTREF(valueUNSAFE);
    gc.fieldType= (REFLECTCLASSBASEREF)ObjectToOBJECTREF(pFieldTypeUNSAFE);
    gc.declaringType= (REFLECTCLASSBASEREF)ObjectToOBJECTREF(pDeclaringTypeUNSAFE);
    gc.refField = (REFLECTFIELDREF)ObjectToOBJECTREF(pFieldUNSAFE);

    if ((gc.fieldType == NULL) || (gc.refField == NULL))
        FCThrowResVoid(kArgumentNullException, W("Arg_InvalidHandle"));
    
    TypeHandle fieldType = gc.fieldType->GetType();
    TypeHandle declaringType = gc.declaringType != NULL ? gc.declaringType->GetType() : TypeHandle();

    Assembly *pAssem;
    if (declaringType.IsNull())
    {
        // global field
        pAssem = gc.refField->GetField()->GetModule()->GetAssembly();
    }
    else
    {
        pAssem = declaringType.GetAssembly();
    }

    if (pAssem->IsIntrospectionOnly())
        FCThrowExVoid(kInvalidOperationException, IDS_EE_CODEEXECUTION_IN_INTROSPECTIVE_ASSEMBLY, NULL, NULL, NULL);

    // We should throw NotSupportedException here. 
    // But for backward compatibility we are throwing FieldAccessException instead.
    if (pAssem->IsDynamic() && !pAssem->HasRunAccess())
        FCThrowVoid(kFieldAccessException);

    FC_GC_POLL_NOT_NEEDED();

    FieldDesc* pFieldDesc = gc.refField->GetField();

    HELPER_METHOD_FRAME_BEGIN_PROTECT(gc);

    //TODO: cleanup this function
    InvokeUtil::SetValidField(fieldType.GetSignatureCorElementType(), fieldType, pFieldDesc, &gc.target, &gc.value, declaringType, pDomainInitialized);

    HELPER_METHOD_FRAME_END();
}
FCIMPLEND

//A.CI work
FCIMPL1(Object*, RuntimeTypeHandle::Allocate, ReflectClassBaseObject* pTypeUNSAFE)  
{
    CONTRACTL {
        FCALL_CHECK;
        PRECONDITION(CheckPointer(pTypeUNSAFE));
    }
    CONTRACTL_END

    REFLECTCLASSBASEREF refType = (REFLECTCLASSBASEREF)ObjectToOBJECTREF(pTypeUNSAFE);
    TypeHandle type = refType->GetType();

        // Handle the nullable<T> special case
    if (Nullable::IsNullableType(type)) {
        return OBJECTREFToObject(Nullable::BoxedNullableNull(type));
    }

    OBJECTREF rv = NULL;
    HELPER_METHOD_FRAME_BEGIN_RET_1(refType);
    rv = AllocateObject(type.GetMethodTable());
    HELPER_METHOD_FRAME_END();
    return OBJECTREFToObject(rv);

}//Allocate
FCIMPLEND

FCIMPL6(Object*, RuntimeTypeHandle::CreateInstance, ReflectClassBaseObject* refThisUNSAFE,
                                                    CLR_BOOL publicOnly,
                                                    CLR_BOOL securityOff,
                                                    CLR_BOOL* pbCanBeCached,
                                                    MethodDesc** pConstructor,
                                                    CLR_BOOL *pbNeedSecurityCheck) {
    CONTRACTL {
        FCALL_CHECK;
        PRECONDITION(CheckPointer(refThisUNSAFE));
        PRECONDITION(CheckPointer(pbCanBeCached));
        PRECONDITION(CheckPointer(pbNeedSecurityCheck));
        PRECONDITION(CheckPointer(pConstructor));
        PRECONDITION(*pbCanBeCached == false);
        PRECONDITION(*pConstructor == NULL);
        PRECONDITION(*pbNeedSecurityCheck == true);
    }
    CONTRACTL_END;

    if (refThisUNSAFE == NULL) 
        FCThrow(kNullReferenceException);

    MethodDesc* pMeth;

    OBJECTREF           rv      = NULL;
    REFLECTCLASSBASEREF refThis = (REFLECTCLASSBASEREF)ObjectToOBJECTREF(refThisUNSAFE);
    TypeHandle thisTH = refThis->GetType();

    Assembly *pAssem = thisTH.GetAssembly();

    if (pAssem->IsIntrospectionOnly())
        FCThrowEx(kInvalidOperationException, IDS_EE_CODEEXECUTION_IN_INTROSPECTIVE_ASSEMBLY, NULL, NULL, NULL);

    if (pAssem->IsDynamic() && !pAssem->HasRunAccess())
        FCThrowRes(kNotSupportedException, W("NotSupported_DynamicAssemblyNoRunAccess"));

    HELPER_METHOD_FRAME_BEGIN_RET_2(rv, refThis);

    MethodTable* pVMT;
    bool bNeedAccessCheck;

    // Get the type information associated with refThis
    if (thisTH.IsNull() || thisTH.IsTypeDesc())
        COMPlusThrow(kMissingMethodException,W("Arg_NoDefCTor"));
        
    pVMT = thisTH.AsMethodTable();

    pVMT->EnsureInstanceActive();

    bNeedAccessCheck = false;

#ifdef FEATURE_COMINTEROP
    // If this is __ComObject then create the underlying COM object.
    if (IsComObjectClass(refThis->GetType())) {
#ifdef FEATURE_COMINTEROP_UNMANAGED_ACTIVATION
        SyncBlock* pSyncBlock = refThis->GetSyncBlock();

        void* pClassFactory = (void*)pSyncBlock->GetInteropInfo()->GetComClassFactory();
        if (!pClassFactory)
            COMPlusThrow(kInvalidComObjectException, IDS_EE_NO_BACKING_CLASS_FACTORY);

        // Check for the required permissions (SecurityPermission.UnmanagedCode),
        // since arbitrary unmanaged code in the class factory will execute below).
        Security::SpecialDemand(SSWT_LATEBOUND_LINKDEMAND, SECURITY_UNMANAGED_CODE);

        // create an instance of the Com Object
        rv = ((ComClassFactory*)pClassFactory)->CreateInstance(NULL);

#else // FEATURE_COMINTEROP_UNMANAGED_ACTIVATION

        COMPlusThrow(kInvalidComObjectException, IDS_EE_NO_BACKING_CLASS_FACTORY);

#endif // FEATURE_COMINTEROP_UNMANAGED_ACTIVATION
    }
    else
#endif // FEATURE_COMINTEROP
    {
        // If we are creating a COM object which has backing metadata we still
        // need to ensure that the caller has unmanaged code access permission.
        if (pVMT->IsComObjectType())
            Security::SpecialDemand(SSWT_LATEBOUND_LINKDEMAND, SECURITY_UNMANAGED_CODE);

        // if this is an abstract class then we will fail this
        if (pVMT->IsAbstract())  {
            if (pVMT->IsInterface())
                COMPlusThrow(kMissingMethodException,W("Acc_CreateInterface"));
            else
                COMPlusThrow(kMissingMethodException,W("Acc_CreateAbst"));
        }
        else if (pVMT->ContainsGenericVariables()) {
            COMPlusThrow(kArgumentException,W("Acc_CreateGeneric"));
        }
        
        if (pVMT->IsByRefLike())
            COMPlusThrow(kNotSupportedException, W("NotSupported_ByRefLike"));
        
        if (pVMT->IsSharedByGenericInstantiations())
            COMPlusThrow(kNotSupportedException, W("NotSupported_Type"));

        if (!pVMT->HasDefaultConstructor())
        {
            // We didn't find the parameterless constructor,
            //  if this is a Value class we can simply allocate one and return it

            if (!pVMT->IsValueType()) {
                COMPlusThrow(kMissingMethodException,W("Arg_NoDefCTor"));
            }

            if (!securityOff)
            {
#ifndef FEATURE_CORECLR
                // Security checks are expensive as they involve stack walking. Avoid them if we can.
                // In immersive we don't allow private reflection to framework code. So we need to perform
                // the access check even if all the domains on the stack are fully trusted.
                if (Security::AllDomainsOnStackFullyTrusted() && !AppX::IsAppXProcess())
                {
                    bNeedAccessCheck = false;
                }
                else
#endif //FEATURE_CORECLR
                {
                    // Public critical types cannot be accessed by transparent callers
                    bNeedAccessCheck = !pVMT->IsExternallyVisible() || Security::TypeRequiresTransparencyCheck(pVMT);
                }

                if (bNeedAccessCheck)
                {
                    RefSecContext sCtx(InvokeUtil::GetInvocationAccessCheckType());
                    InvokeUtil::CanAccessClass(&sCtx, pVMT, TRUE);
                }
            }

                // Handle the nullable<T> special case
            if (Nullable::IsNullableType(thisTH)) {
                rv = Nullable::BoxedNullableNull(thisTH);
            }
            else 
                rv = pVMT->Allocate();

            // Since no security checks will be performed on cached value types without default ctors,
            // we cannot cache those types that require access checks.
            // In fact, we don't even need to set pbNeedSecurityCheck to false here.
            if (!pVMT->Collectible() && !bNeedAccessCheck)
            {
                *pbCanBeCached = true;
                *pbNeedSecurityCheck = false;
            }
        }
        else // !pVMT->HasDefaultConstructor()
        {
            pMeth = pVMT->GetDefaultConstructor();
            
            // Validate the method can be called by this caller
            DWORD attr = pMeth->GetAttrs();

            if (!IsMdPublic(attr) && publicOnly)
                COMPlusThrow(kMissingMethodException,W("Arg_NoDefCTor"));

            if (!securityOff)
            {
                // If the type is critical or the constructor we're using is critical, we need to ensure that
                // the caller is allowed to invoke it.
                bool needsTransparencyCheck = Security::TypeRequiresTransparencyCheck(pVMT) ||
                                               (Security::IsMethodCritical(pMeth) && !Security::IsMethodSafeCritical(pMeth));

                // We also need to do a check if the method or type is not public
                bool needsVisibilityCheck = !IsMdPublic(attr) || !pVMT->IsExternallyVisible();

                // If the visiblity, transparency, or legacy LinkDemands on the type or constructor dictate that
                // we need to check the caller, then do that now.
                bNeedAccessCheck = needsTransparencyCheck ||
                    needsVisibilityCheck ||
                    pMeth->RequiresLinktimeCheck();

                if (bNeedAccessCheck)
                {
                    // this security context will be used in cast checking as well
                    RefSecContext sCtx(InvokeUtil::GetInvocationAccessCheckType());
                    InvokeUtil::CanAccessMethod(pMeth, pVMT, NULL, &sCtx);
                }
            }

            // We've got the class, lets allocate it and call the constructor
            OBJECTREF o;
            bool remoting = false;
        
#ifdef FEATURE_REMOTING          
            if (pVMT->IsTransparentProxy())
                COMPlusThrow(kMissingMethodException,W("NotSupported_Constructor"));

            if (pVMT->MayRequireManagedActivation())
            {
                o = CRemotingServices::CreateProxyOrObject(pVMT);
                remoting = true;
            }
            else
                o = AllocateObject(pVMT);

#else
            o = AllocateObject(pVMT);
#endif            
            GCPROTECT_BEGIN(o);

            MethodDescCallSite ctor(pMeth, &o);

            // Copy "this" pointer
            ARG_SLOT arg;
            if (pVMT->IsValueType())
                arg = PtrToArgSlot(o->UnBox());
            else
                arg = ObjToArgSlot(o);

            // Call the method
            TryCallMethod(&ctor, &arg);

            rv = o;
            GCPROTECT_END();

            // No need to set these if they cannot be cached
            if (!remoting && !pVMT->Collectible())
            {
                *pbCanBeCached = true;
                *pConstructor = pMeth;
                *pbNeedSecurityCheck = bNeedAccessCheck;
            }
        }
    }

    HELPER_METHOD_FRAME_END();
    return OBJECTREFToObject(rv);
}
FCIMPLEND

FCIMPL2(Object*, RuntimeTypeHandle::CreateInstanceForGenericType, ReflectClassBaseObject* pTypeUNSAFE, ReflectClassBaseObject* pParameterTypeUNSAFE) {
    FCALL_CONTRACT;

    struct _gc
    {
        OBJECTREF rv;
        REFLECTCLASSBASEREF refType;
        REFLECTCLASSBASEREF refParameterType;
    } gc;

    gc.rv = NULL;
    gc.refType = (REFLECTCLASSBASEREF)ObjectToOBJECTREF(pTypeUNSAFE);
    gc.refParameterType = (REFLECTCLASSBASEREF)ObjectToOBJECTREF(pParameterTypeUNSAFE);

    MethodDesc* pMeth;
    TypeHandle genericType = gc.refType->GetType();

    TypeHandle parameterHandle = gc.refParameterType->GetType();

    _ASSERTE (genericType.HasInstantiation());

    HELPER_METHOD_FRAME_BEGIN_RET_PROTECT(gc);

    TypeHandle instantiatedType = ((TypeHandle)genericType.GetCanonicalMethodTable()).Instantiate(Instantiation(&parameterHandle, 1));

    // Get the type information associated with refThis
    MethodTable* pVMT = instantiatedType.GetMethodTable();
    _ASSERTE (pVMT != 0 &&  !instantiatedType.IsTypeDesc());
    _ASSERTE(!(pVMT->GetAssembly()->IsDynamic() && !pVMT->GetAssembly()->HasRunAccess()));
    _ASSERTE( !pVMT->IsAbstract() ||! instantiatedType.ContainsGenericVariables());
    _ASSERTE(!pVMT->IsByRefLike() && pVMT->HasDefaultConstructor());

    pMeth = pVMT->GetDefaultConstructor();            
    MethodDescCallSite ctor(pMeth);

    // We've got the class, lets allocate it and call the constructor
#ifdef FEATURE_REMOTING      
    _ASSERTE(!pVMT->IsTransparentProxy());
    _ASSERTE(!pVMT->MayRequireManagedActivation());
#endif     
   
    // Nullables don't take this path, if they do we need special logic to make an instance
    _ASSERTE(!Nullable::IsNullableType(instantiatedType));
    gc.rv = instantiatedType.GetMethodTable()->Allocate();

    ARG_SLOT arg = ObjToArgSlot(gc.rv); 

    // Call the method
    TryCallMethod(&ctor, &arg);

    HELPER_METHOD_FRAME_END();
    return OBJECTREFToObject(gc.rv);
}
FCIMPLEND

NOINLINE FC_BOOL_RET IsInstanceOfTypeHelper(OBJECTREF obj, REFLECTCLASSBASEREF refType)
{
    FCALL_CONTRACT;

    BOOL canCast = false;

    FC_INNER_PROLOG(RuntimeTypeHandle::IsInstanceOfType);

    HELPER_METHOD_FRAME_BEGIN_RET_ATTRIB_2(Frame::FRAME_ATTR_EXACT_DEPTH|Frame::FRAME_ATTR_CAPTURE_DEPTH_2, obj, refType);
    canCast = ObjIsInstanceOf(OBJECTREFToObject(obj), refType->GetType());
    HELPER_METHOD_FRAME_END();

    FC_RETURN_BOOL(canCast);
}

FCIMPL2(FC_BOOL_RET, RuntimeTypeHandle::IsInstanceOfType, ReflectClassBaseObject* pTypeUNSAFE, Object *objectUNSAFE) {
    FCALL_CONTRACT;

    OBJECTREF obj = ObjectToOBJECTREF(objectUNSAFE);
    REFLECTCLASSBASEREF refType = (REFLECTCLASSBASEREF)ObjectToOBJECTREF(pTypeUNSAFE);

    // Null is not instance of anything in reflection world
    if (obj == NULL)
        FC_RETURN_BOOL(false);

    if (refType == NULL)
        FCThrowRes(kArgumentNullException, W("Arg_InvalidHandle"));    

    switch (ObjIsInstanceOfNoGC(objectUNSAFE, refType->GetType())) {
    case TypeHandle::CanCast:
        FC_RETURN_BOOL(true);
    case TypeHandle::CannotCast:
        FC_RETURN_BOOL(false);
    default:
        // fall through to the slow helper
        break;
    }

    FC_INNER_RETURN(FC_BOOL_RET, IsInstanceOfTypeHelper(obj, refType));
}
FCIMPLEND

FCIMPL1(DWORD, ReflectionInvocation::GetSpecialSecurityFlags, ReflectMethodObject *pMethodUNSAFE) {
    CONTRACTL {
        FCALL_CHECK;
    }
    CONTRACTL_END;
    
    DWORD dwFlags = 0;

    struct
    {
        REFLECTMETHODREF refMethod;
    }
    gc;

    gc.refMethod = (REFLECTMETHODREF)ObjectToOBJECTREF(pMethodUNSAFE);

    if (!gc.refMethod)
        FCThrowRes(kArgumentNullException, W("Arg_InvalidHandle"));

    MethodDesc* pMethod = gc.refMethod->GetMethod();

    HELPER_METHOD_FRAME_BEGIN_RET_PROTECT(gc);

    // this is an information that is critical for ctors, otherwise is not important
    // we get it here anyway to simplify code
    MethodTable *pMT = pMethod->GetMethodTable();
    _ASSERTE(pMT);

    // We should also check the return type here. 
    // Is there an easier way to get the return type of a method?
    MetaSig metaSig(pMethod);
    TypeHandle retTH = metaSig.GetRetTypeHandleThrowing();
    MethodTable *pRetMT = retTH.GetMethodTable();

    // If either the declaring type or the return type contains stack pointers (ByRef or typedbyref), 
    // the type cannot be boxed and thus cannot be invoked through reflection invocation.
    if ( pMT->IsByRefLike() || (pRetMT != NULL && pRetMT->IsByRefLike()) )
        dwFlags |= INVOCATION_FLAGS_CONTAINS_STACK_POINTERS;

    // Is this a call to a potentially dangerous method? (If so, we're going
    // to demand additional permission).
    if (InvokeUtil::IsDangerousMethod(pMethod))
        dwFlags |= INVOCATION_FLAGS_RISKY_METHOD;

    // Is there a link demand?
    if (pMethod->RequiresLinktimeCheck()) {
        dwFlags |= INVOCATION_FLAGS_NEED_SECURITY;
    }
    else
    if (Security::IsMethodCritical(pMethod) && !Security::IsMethodSafeCritical(pMethod)) {
        dwFlags |= INVOCATION_FLAGS_NEED_SECURITY;
    }

    HELPER_METHOD_FRAME_END();
    return dwFlags;
}
FCIMPLEND

#ifndef FEATURE_CORECLR

// Can not inline this function.
#ifdef _MSC_VER
__declspec(noinline)
#endif
void PerformSecurityCheckHelper(Object *targetUnsafe, MethodDesc *pMeth, MethodTable* pParentMT, DWORD dwFlags)
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;

        PRECONDITION(CheckPointer(pMeth));
    }
    CONTRACTL_END;

    OBJECTREF target (targetUnsafe);
    GCPROTECT_BEGIN (target);
    FrameWithCookie<DebuggerSecurityCodeMarkFrame> __dbgSecFrame;

    bool targetRemoted = false;

#ifndef FEATURE_CORECLR
    targetRemoted = target != NULL && InvokeUtil::IsTargetRemoted(pMeth, target->GetMethodTable());
#endif //FEATURE_CORECLR

    RefSecContext sCtx(InvokeUtil::GetInvocationAccessCheckType(targetRemoted));

    MethodTable* pInstanceMT = NULL;
    if (target != NULL) {
        if (!target->GetTypeHandle().IsTypeDesc())
            pInstanceMT = target->GetTypeHandle().AsMethodTable();
    }

#ifdef FEATURE_CORECLR
    if (dwFlags & (INVOCATION_FLAGS_RISKY_METHOD|INVOCATION_FLAGS_IS_DELEGATE_CTOR))
    {
        // On CoreCLR we assert that "dangerous" methods (see IsDangerousMethods) can only
        // be reflection-invoked by platform code (C or SC).

        // Also, for delegates, in desktop we used to demand unmanaged
        // code permission for this since it's hard to validate the target address.
        // Here we just restrict access to Critical code.
        MethodDesc *pCallerMD = sCtx.GetCallerMethod();

        if (pCallerMD && Security::IsMethodTransparent(pCallerMD))
        {
            ThrowMethodAccessException(pCallerMD, pMeth, FALSE, IDS_E_TRANSPARENT_REFLECTION);
        }
    }

    if (dwFlags & (INVOCATION_FLAGS_NEED_SECURITY|INVOCATION_FLAGS_CONSTRUCTOR_INVOKE))
#endif 
    {

        if (dwFlags & INVOCATION_FLAGS_CONSTRUCTOR_INVOKE)
            InvokeUtil::CanAccessMethod(pMeth,
                                        pParentMT,
                                        pInstanceMT,
                                        &sCtx,
                                        TRUE /*fCriticalToFullDemand*/);
        else
            InvokeUtil::CanAccessMethod(pMeth,
                                        pParentMT,
                                        pInstanceMT,
                                        &sCtx,
                                        TRUE /*fCriticalToFullDemand*/,
                                        (dwFlags & INVOCATION_FLAGS_IS_CTOR) != 0 /*checkSkipVer*/);
    }

    __dbgSecFrame.Pop();
    GCPROTECT_END();
}

FCIMPL4(void, ReflectionInvocation::PerformSecurityCheck, Object *target, MethodDesc *pMeth, ReflectClassBaseObject *pParentUNSAFE, DWORD dwFlags) {
    CONTRACTL {
        FCALL_CHECK;
        PRECONDITION(CheckPointer(pMeth));
    }
    CONTRACTL_END;
    
#ifndef FEATURE_CORECLR
    // Security checks are expensive as they involve stack walking. Avoid them if we can.
    // In immersive we don't allow private reflection to framework code. So we need to perform
    // the access check even if all the domains on the stack are fully trusted.
    if (Security::AllDomainsOnStackFullyTrusted() && !AppX::IsAppXProcess())
        return;
#endif

    REFLECTCLASSBASEREF refParent = (REFLECTCLASSBASEREF)ObjectToOBJECTREF(pParentUNSAFE);

    HELPER_METHOD_FRAME_BEGIN_1(refParent);
    //CAUTION: PerformSecurityCheckHelper could trigger GC!
    
    TypeHandle parent = refParent != NULL ? refParent->GetType() : TypeHandle();
    PerformSecurityCheckHelper(target,pMeth,parent.GetMethodTable(),dwFlags);

    HELPER_METHOD_FRAME_END();
}
FCIMPLEND

#endif // FEATURE_CORECLR

/****************************************************************************/
/* boxed Nullable<T> are represented as a boxed T, so there is no unboxed
   Nullable<T> inside to point at by reference.  Because of this a byref
   parameters  of type Nullable<T> are copied out of the boxed instance
   (to a place on the stack), before the call is made (and this copy is
   pointed at).  After the call returns, this copy must be copied back to
   the original argument array.  ByRefToNullable, is a simple linked list
   that remembers what copy-backs are needed */

struct ByRefToNullable  {
    unsigned argNum;            // The argument number for this byrefNullable argument
    void* data;                 // The data to copy back to the ByRefNullable.  This points to the stack 
    TypeHandle type;            // The type of Nullable for this argument
    ByRefToNullable* next;      // list of these

    ByRefToNullable(unsigned aArgNum, void* aData, TypeHandle aType, ByRefToNullable* aNext) {
        argNum = aArgNum;
        data = aData;
        type = aType;
        next = aNext;
    }
};

void CallDescrWorkerReflectionWrapper(CallDescrData * pCallDescrData, Frame * pFrame)
{
    // Use static contracts b/c we have SEH.
    STATIC_CONTRACT_THROWS;
    STATIC_CONTRACT_GC_TRIGGERS;
    STATIC_CONTRACT_MODE_ANY;

    struct Param: public NotifyOfCHFFilterWrapperParam
    {
        CallDescrData * pCallDescrData;
    } param;

    param.pFrame = pFrame;
    param.pCallDescrData = pCallDescrData;

    PAL_TRY(Param *, pParam, &param)
    {
        CallDescrWorkerWithHandler(pParam->pCallDescrData);
    }
    PAL_EXCEPT_FILTER(ReflectionInvocationExceptionFilter)
    {
        // Should never reach here b/c handler should always continue search.
        _ASSERTE(false);
    }
    PAL_ENDTRY
} // CallDescrWorkerReflectionWrapper

OBJECTREF InvokeArrayConstructor(ArrayTypeDesc* arrayDesc, MethodDesc* pMeth, PTRARRAYREF* objs, int argCnt)
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;

    DWORD i;

    // If we're trying to create an array of pointers or function pointers,
    // check that the caller has skip verification permission.
    CorElementType et = arrayDesc->GetArrayElementTypeHandle().GetVerifierCorElementType();
    if (et == ELEMENT_TYPE_PTR || et == ELEMENT_TYPE_FNPTR)
        Security::SpecialDemand(SSWT_LATEBOUND_LINKDEMAND, SECURITY_SKIP_VER);

    // Validate the argCnt an the Rank. Also allow nested SZARRAY's.
    _ASSERTE(argCnt == (int) arrayDesc->GetRank() || argCnt == (int) arrayDesc->GetRank() * 2 ||
             arrayDesc->GetInternalCorElementType() == ELEMENT_TYPE_SZARRAY);

    // Validate all of the parameters.  These all typed as integers
    int allocSize = 0;
    if (!ClrSafeInt<int>::multiply(sizeof(INT32), argCnt, allocSize))
        COMPlusThrow(kArgumentException, IDS_EE_SIGTOOCOMPLEX);
        
    INT32* indexes = (INT32*) _alloca((size_t)allocSize);
    ZeroMemory(indexes, allocSize);

    for (i=0; i<(DWORD)argCnt; i++)
    {
        if (!(*objs)->m_Array[i])
            COMPlusThrowArgumentException(W("parameters"), W("Arg_NullIndex"));
        
        MethodTable* pMT = ((*objs)->m_Array[i])->GetMethodTable();
        CorElementType oType = TypeHandle(pMT).GetVerifierCorElementType();
        
        if (!InvokeUtil::IsPrimitiveType(oType) || !InvokeUtil::CanPrimitiveWiden(ELEMENT_TYPE_I4,oType))
            COMPlusThrow(kArgumentException,W("Arg_PrimWiden"));
        
        memcpy(&indexes[i],(*objs)->m_Array[i]->UnBox(),pMT->GetNumInstanceFieldBytes());
    }

    return AllocateArrayEx(TypeHandle(arrayDesc), indexes, argCnt);
}

static BOOL IsActivationNeededForMethodInvoke(MethodDesc * pMD)
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;

    // The activation for non-generic instance methods is covered by non-null "this pointer"
    if (!pMD->IsStatic() && !pMD->HasMethodInstantiation() && !pMD->IsInterface())
        return FALSE;

    // We need to activate each time for domain neutral types
    if (pMD->IsDomainNeutral())
        return TRUE;

    // We need to activate the instance at least once
    pMD->EnsureActive();
    return FALSE;
}

class ArgIteratorBaseForMethodInvoke
{
protected:
    SIGNATURENATIVEREF * m_ppNativeSig;

    FORCEINLINE CorElementType GetReturnType(TypeHandle * pthValueType)
    {
        WRAPPER_NO_CONTRACT;
        return (*pthValueType = (*m_ppNativeSig)->GetReturnTypeHandle()).GetInternalCorElementType();
    }

    FORCEINLINE CorElementType GetNextArgumentType(DWORD iArg, TypeHandle * pthValueType)
    {
        WRAPPER_NO_CONTRACT;
        return (*pthValueType = (*m_ppNativeSig)->GetArgumentAt(iArg)).GetInternalCorElementType();
    }

    FORCEINLINE void Reset()
    {
        LIMITED_METHOD_CONTRACT;
    }

public:
    BOOL HasThis()
    {
        LIMITED_METHOD_CONTRACT;
        return (*m_ppNativeSig)->HasThis();
    }

    BOOL HasParamType()
    {
        LIMITED_METHOD_CONTRACT;
        // param type methods are not supported for reflection invoke, so HasParamType is always false for them
        return FALSE;
    }

    BOOL IsVarArg()
    {
        LIMITED_METHOD_CONTRACT;
        // vararg methods are not supported for reflection invoke, so IsVarArg is always false for them
        return FALSE;
    }

    DWORD NumFixedArgs()
    {
        LIMITED_METHOD_CONTRACT;
        return (*m_ppNativeSig)->NumFixedArgs();
    }

#ifdef FEATURE_INTERPRETER
    BYTE CallConv()
    {
        LIMITED_METHOD_CONTRACT;
        return IMAGE_CEE_CS_CALLCONV_DEFAULT;
    }
#endif // FEATURE_INTERPRETER
};

class ArgIteratorForMethodInvoke : public ArgIteratorTemplate<ArgIteratorBaseForMethodInvoke>
{
public:
    ArgIteratorForMethodInvoke(SIGNATURENATIVEREF * ppNativeSig)
    {
        m_ppNativeSig = ppNativeSig;

        DWORD dwFlags = (*m_ppNativeSig)->GetArgIteratorFlags();

        // Use the cached values if they are available
        if (dwFlags & SIZE_OF_ARG_STACK_COMPUTED)
        {
            m_dwFlags = dwFlags;
            m_nSizeOfArgStack = (*m_ppNativeSig)->GetSizeOfArgStack();
            return;
        }

        //
        // Compute flags and stack argument size, and cache them for next invocation
        //

        ForceSigWalk();

        if (IsActivationNeededForMethodInvoke((*m_ppNativeSig)->GetMethod()))
        {
            m_dwFlags |= METHOD_INVOKE_NEEDS_ACTIVATION;
        }

        (*m_ppNativeSig)->SetSizeOfArgStack(m_nSizeOfArgStack);
        _ASSERTE((*m_ppNativeSig)->GetSizeOfArgStack() == m_nSizeOfArgStack);

        // This has to be last
        (*m_ppNativeSig)->SetArgIteratorFlags(m_dwFlags);
        _ASSERTE((*m_ppNativeSig)->GetArgIteratorFlags() == m_dwFlags);
    }

    BOOL IsActivationNeeded()
    {
        LIMITED_METHOD_CONTRACT;
        return (m_dwFlags & METHOD_INVOKE_NEEDS_ACTIVATION) != 0;
    }
};


void DECLSPEC_NORETURN ThrowInvokeMethodException(MethodDesc * pMethod, OBJECTREF targetException)
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;

    GCPROTECT_BEGIN(targetException);

#if defined(_DEBUG) && !defined(FEATURE_PAL)
#ifdef FEATURE_CORECLR
    if (IsWatsonEnabled())
#endif // FEATURE_CORECLR
    {
        if (!CLRException::IsPreallocatedExceptionObject(targetException))
        {
            // If the exception is not preallocated, we should be having the
            // watson buckets in the throwable already.
            if(!((EXCEPTIONREF)targetException)->AreWatsonBucketsPresent())
            {
                // If an exception is raised by the VM (e.g. type load exception by the JIT) and it comes
                // across the reflection invocation boundary before CLR's personality routine for managed
                // code has been invoked, then no buckets would be available for us at this point.
                //
                // Since we cannot assert this, better log it for diagnosis if required.
                LOG((LF_EH, LL_INFO100, "InvokeImpl - No watson buckets available - regular exception likely raised within VM and not seen by managed code.\n"));
            }
        }
        else
        {
            // Exception is preallocated.
            PTR_EHWatsonBucketTracker pUEWatsonBucketTracker = GetThread()->GetExceptionState()->GetUEWatsonBucketTracker();
            if ((IsThrowableThreadAbortException(targetException) && pUEWatsonBucketTracker->CapturedForThreadAbort())||
                (pUEWatsonBucketTracker->CapturedAtReflectionInvocation()))
            {
                // ReflectionInvocationExceptionFilter would have captured
                // the watson bucket details for preallocated exceptions
                // in the UE watson bucket tracker.

                if(pUEWatsonBucketTracker->RetrieveWatsonBuckets() == NULL)
                {
                    // See comment above
                    LOG((LF_EH, LL_INFO100, "InvokeImpl - No watson buckets available - preallocated exception likely raised within VM and not seen by managed code.\n"));
                }
            }
        }
    }
#endif // _DEBUG && !FEATURE_PAL

#ifdef FEATURE_CORRUPTING_EXCEPTIONS
    // Get the corruption severity of the exception that came in through reflection invocation.
    CorruptionSeverity severity = GetThread()->GetExceptionState()->GetLastActiveExceptionCorruptionSeverity();

    // Since we are dealing with an exception, set the flag indicating if the target of Reflection can handle exception or not.
    // This flag is used in CEHelper::CanIDispatchTargetHandleException.
    GetThread()->GetExceptionState()->SetCanReflectionTargetHandleException(CEHelper::CanMethodHandleException(severity, pMethod));
#endif // FEATURE_CORRUPTING_EXCEPTIONS

    OBJECTREF except = InvokeUtil::CreateTargetExcept(&targetException);

#ifndef FEATURE_PAL
#ifdef FEATURE_CORECLR
    if (IsWatsonEnabled())
#endif // FEATURE_CORECLR
    {
        struct 
        {
            OBJECTREF oExcept;
        } gcTIE;
        ZeroMemory(&gcTIE, sizeof(gcTIE));
        GCPROTECT_BEGIN(gcTIE);
        
        gcTIE.oExcept = except;

        _ASSERTE(!CLRException::IsPreallocatedExceptionObject(gcTIE.oExcept));
            
        // If the original exception was preallocated, then copy over the captured
        // watson buckets to the TargetInvocationException object, if available.
        //
        // We dont need to do this if the original exception was not preallocated
        // since it already contains the watson buckets inside the object.
        if (CLRException::IsPreallocatedExceptionObject(targetException))
        {
            PTR_EHWatsonBucketTracker pUEWatsonBucketTracker = GetThread()->GetExceptionState()->GetUEWatsonBucketTracker();
            BOOL fCopyWatsonBuckets = TRUE;
            PTR_VOID pBuckets = pUEWatsonBucketTracker->RetrieveWatsonBuckets();
            if (pBuckets != NULL)
            {
                // Copy the buckets to the exception object
                CopyWatsonBucketsToThrowable(pBuckets, gcTIE.oExcept);

                // Confirm that they are present.
                _ASSERTE(((EXCEPTIONREF)gcTIE.oExcept)->AreWatsonBucketsPresent());
            }
                
            // Clear the UE watson bucket tracker since the bucketing
            // details are now in the TargetInvocationException object.
            pUEWatsonBucketTracker->ClearWatsonBucketDetails();
        }

        // update "except" incase the reference to the object
        // was updated by the GC
        except = gcTIE.oExcept;
        GCPROTECT_END();
    }
#endif // !FEATURE_PAL

    // Since the original exception is inner of target invocation exception,
    // when TIE is seen to be raised for the first time, we will end up
    // using the inner exception buckets automatically.

    // Since VM is throwing the exception, we set it to use the same corruption severity
    // that the original exception came in with from reflection invocation.
    COMPlusThrow(except 
#ifdef FEATURE_CORRUPTING_EXCEPTIONS
        , severity
#endif // FEATURE_CORRUPTING_EXCEPTIONS
        );

    GCPROTECT_END();
}

FCIMPL4(Object*, RuntimeMethodHandle::InvokeMethod, 
    Object *target, PTRArray *objs, SignatureNative* pSigUNSAFE, CLR_BOOL fConstructor)
{
    FCALL_CONTRACT;

    struct {
        OBJECTREF target;
        PTRARRAYREF args;
        SIGNATURENATIVEREF pSig;
        OBJECTREF retVal;
    } gc;

    gc.target = ObjectToOBJECTREF(target);
    gc.args = (PTRARRAYREF)objs;
    gc.pSig = (SIGNATURENATIVEREF)pSigUNSAFE;
    gc.retVal = NULL;

    MethodDesc* pMeth = gc.pSig->GetMethod();
    TypeHandle ownerType = gc.pSig->GetDeclaringType();
   
    HELPER_METHOD_FRAME_BEGIN_RET_PROTECT(gc);

    Assembly *pAssem = pMeth->GetAssembly();

    if (pAssem->IsIntrospectionOnly())
        COMPlusThrow(kInvalidOperationException, IDS_EE_CODEEXECUTION_IN_INTROSPECTIVE_ASSEMBLY);

    // We should throw NotSupportedException here. 
    // But for backward compatibility we are throwing TargetException instead.
    if (pAssem->IsDynamic() && !pAssem->HasRunAccess())
        COMPlusThrow(kTargetException);

    if (ownerType.IsSharedByGenericInstantiations())
        COMPlusThrow(kNotSupportedException, W("NotSupported_Type"));
 
#ifdef _DEBUG 
    if (g_pConfig->ShouldInvokeHalt(pMeth))
    {
        _ASSERTE(!"InvokeHalt");
    }
#endif

    // Skip the activation optimization for remoting because of remoting proxy is not always activated.
    // It would be nice to clean this up and get remoting to always activate methodtable behind the proxy.
    BOOL fForceActivationForRemoting = FALSE;

    if (fConstructor)
    {
        // If we are invoking a constructor on an array then we must
        // handle this specially.  String objects allocate themselves
        // so they are a special case.
        if (ownerType.IsArray()) {
            gc.retVal = InvokeArrayConstructor(ownerType.AsArray(),
                                               pMeth,
                                               &gc.args,
                                               gc.pSig->NumFixedArgs());
            goto Done;
        }

        MethodTable * pMT = ownerType.AsMethodTable();

#ifdef FEATURE_REMOTING
        if (pMT->MayRequireManagedActivation())
        {
            gc.retVal = CRemotingServices::CreateProxyOrObject(pMT);
            fForceActivationForRemoting = TRUE;
        }
        else
#endif        
        {
            if (pMT != g_pStringClass)
                gc.retVal = pMT->Allocate();
        }
    }
    else
    {
#ifdef FEATURE_REMOTING
        if (gc.target != NULL)
        {
            fForceActivationForRemoting = gc.target->IsTransparentProxy();
        }
#endif
    }

    {
    ArgIteratorForMethodInvoke argit(&gc.pSig);

    if (argit.IsActivationNeeded() || fForceActivationForRemoting)
        pMeth->EnsureActive();
    CONSISTENCY_CHECK(pMeth->CheckActivated());

    UINT nStackBytes = argit.SizeOfFrameArgumentArray();

    // Note that SizeOfFrameArgumentArray does overflow checks with sufficient margin to prevent overflows here
    SIZE_T nAllocaSize = TransitionBlock::GetNegSpaceSize() + sizeof(TransitionBlock) + nStackBytes;

    Thread * pThread = GET_THREAD();

    // Make sure we have enough room on the stack for this. Note that we will need the stack amount twice - once to build the stack
    // and second time to actually make the call.
    INTERIOR_STACK_PROBE_FOR(pThread, 1 + static_cast<UINT>((2 * nAllocaSize) / OS_PAGE_SIZE) + static_cast<UINT>(HOLDER_CODE_NORMAL_STACK_LIMIT));

    LPBYTE pAlloc = (LPBYTE)_alloca(nAllocaSize);

    LPBYTE pTransitionBlock = pAlloc + TransitionBlock::GetNegSpaceSize();

    CallDescrData callDescrData;

    callDescrData.pSrc = pTransitionBlock + sizeof(TransitionBlock);
    callDescrData.numStackSlots = nStackBytes / STACK_ELEM_SIZE;
#ifdef CALLDESCR_ARGREGS
    callDescrData.pArgumentRegisters = (ArgumentRegisters*)(pTransitionBlock + TransitionBlock::GetOffsetOfArgumentRegisters());
#endif
#ifdef CALLDESCR_FPARGREGS
    callDescrData.pFloatArgumentRegisters = NULL;
#endif
#ifdef CALLDESCR_REGTYPEMAP
    callDescrData.dwRegTypeMap = 0;
#endif
    callDescrData.fpReturnSize = argit.GetFPReturnSize();

    // This is duplicated logic from MethodDesc::GetCallTarget
    PCODE pTarget;
    if (pMeth->IsVtableMethod())
    {
        pTarget = pMeth->GetSingleCallableAddrOfVirtualizedCode(&gc.target, ownerType);
    }
    else
    {
        pTarget = pMeth->GetSingleCallableAddrOfCode();
    }
    callDescrData.pTarget = pTarget;

    // Build the arguments on the stack

    GCStress<cfg_any>::MaybeTrigger();

    FrameWithCookie<ProtectValueClassFrame> *pProtectValueClassFrame = NULL;
    ValueClassInfo *pValueClasses = NULL;
    ByRefToNullable* byRefToNullables = NULL;

    // if we have the magic Value Class return, we need to allocate that class
    // and place a pointer to it on the stack.

    TypeHandle retTH = gc.pSig->GetReturnTypeHandle();
    BOOL fHasRetBuffArg = argit.HasRetBuffArg();
    CorElementType retType = retTH.GetInternalCorElementType();
    if (retType == ELEMENT_TYPE_VALUETYPE || fHasRetBuffArg) {
        gc.retVal = retTH.GetMethodTable()->Allocate();
    }

    // Copy "this" pointer
    if (!pMeth->IsStatic()) {
        PVOID pThisPtr;

        if (fConstructor)
        {
            // Copy "this" pointer: only unbox if type is value type and method is not unboxing stub
            if (ownerType.IsValueType() && !pMeth->IsUnboxingStub()) {
                // Note that we create a true boxed nullabe<T> and then convert it to a T below
                pThisPtr = gc.retVal->GetData();
            }
            else
                pThisPtr = OBJECTREFToObject(gc.retVal);
        }
        else
        if (!pMeth->GetMethodTable()->IsValueType())
            pThisPtr = OBJECTREFToObject(gc.target);
        else {
            if (pMeth->IsUnboxingStub())
                pThisPtr = OBJECTREFToObject(gc.target);
            else {
                    // Create a true boxed Nullable<T> and use that as the 'this' pointer.
                    // since what is passed in is just a boxed T
                MethodTable* pMT = pMeth->GetMethodTable();
                if (Nullable::IsNullableType(pMT)) {
                    OBJECTREF bufferObj = pMT->Allocate();
                    void* buffer = bufferObj->GetData();
                    Nullable::UnBox(buffer, gc.target, pMT);
                    pThisPtr = buffer;
                }
                else
                    pThisPtr = gc.target->UnBox();
            }
        }

        *((LPVOID*) (pTransitionBlock + argit.GetThisOffset())) = pThisPtr;
    }

    // NO GC AFTER THIS POINT. The object references in the method frame are not protected.
    //
    // We have already copied "this" pointer so we do not want GC to happen even sooner. Unfortunately, 
    // we may allocate in the process of copying this pointer that makes it hard to express using contracts.
    //
    // If an exception occurs a gc may happen but we are going to dump the stack anyway and we do 
    // not need to protect anything.

    PVOID pRetBufStackCopy = NULL;

    {
    BEGINFORBIDGC();
#ifdef _DEBUG
    GCForbidLoaderUseHolder forbidLoaderUse;
#endif

    // Take care of any return arguments
    if (fHasRetBuffArg)
    {
        // We stack-allocate this ret buff, to preserve the invariant that ret-buffs are always in the
        // caller's stack frame.  We'll copy into gc.retVal later.
        TypeHandle retTH = gc.pSig->GetReturnTypeHandle();
        MethodTable* pMT = retTH.GetMethodTable();
        if (pMT->IsStructRequiringStackAllocRetBuf())
        {
            SIZE_T sz = pMT->GetNumInstanceFieldBytes();
            pRetBufStackCopy = _alloca(sz);
            memset(pRetBufStackCopy, 0, sz);

            pValueClasses = new (_alloca(sizeof(ValueClassInfo))) ValueClassInfo(pRetBufStackCopy, pMT, pValueClasses);
            *((LPVOID*) (pTransitionBlock + argit.GetRetBuffArgOffset())) = pRetBufStackCopy;
        }
        else
        {
            PVOID pRetBuff = gc.retVal->GetData();
            *((LPVOID*) (pTransitionBlock + argit.GetRetBuffArgOffset())) = pRetBuff;
        }
    }

    // copy args
    UINT nNumArgs = gc.pSig->NumFixedArgs();
    for (UINT i = 0 ; i < nNumArgs; i++) {

        TypeHandle th = gc.pSig->GetArgumentAt(i);

        int ofs = argit.GetNextOffset();
        _ASSERTE(ofs != TransitionBlock::InvalidOffset);

#ifdef CALLDESCR_REGTYPEMAP
        FillInRegTypeMap(ofs, argit.GetArgType(), (BYTE *)&callDescrData.dwRegTypeMap);
#endif

#ifdef CALLDESCR_FPARGREGS
        // Under CALLDESCR_FPARGREGS -ve offsets indicate arguments in floating point registers. If we have at
        // least one such argument we point the call worker at the floating point area of the frame (we leave
        // it null otherwise since the worker can perform a useful optimization if it knows no floating point
        // registers need to be set up).

        if (TransitionBlock::HasFloatRegister(ofs, argit.GetArgLocDescForStructInRegs()) && 
            (callDescrData.pFloatArgumentRegisters == NULL))
        {
            callDescrData.pFloatArgumentRegisters = (FloatArgumentRegisters*) (pTransitionBlock +
                                                                               TransitionBlock::GetOffsetOfFloatArgumentRegisters());
        }
#endif

        UINT structSize = argit.GetArgSize();

        bool needsStackCopy = false;

        // A boxed Nullable<T> is represented as boxed T. So to pass a Nullable<T> by reference, 
        // we have to create a Nullable<T> on stack, copy the T into it, then pass it to the callee and
        // after returning from the call, copy the T out of the Nullable<T> back to the boxed T.
        TypeHandle nullableType = NullableTypeOfByref(th);
        if (!nullableType.IsNull()) {
            th = nullableType;
            structSize = th.GetSize();
            needsStackCopy = true;
        }
#ifdef ENREGISTERED_PARAMTYPE_MAXSIZE
        else if (argit.IsArgPassedByRef()) 
        {
            needsStackCopy = true;
        }
#endif

        ArgDestination argDest(pTransitionBlock, ofs, argit.GetArgLocDescForStructInRegs());

        if(needsStackCopy)
        {
            MethodTable * pMT = th.GetMethodTable();
            _ASSERTE(pMT && pMT->IsValueType());

            PVOID pArgDst = argDest.GetDestinationAddress();

            PVOID pStackCopy = _alloca(structSize);
            *(PVOID *)pArgDst = pStackCopy;
            pArgDst = pStackCopy;

            if (!nullableType.IsNull())
            {
                byRefToNullables = new(_alloca(sizeof(ByRefToNullable))) ByRefToNullable(i, pStackCopy, nullableType, byRefToNullables);
            }

            // save the info into ValueClassInfo
            if (pMT->ContainsPointers()) 
            {
                pValueClasses = new (_alloca(sizeof(ValueClassInfo))) ValueClassInfo(pStackCopy, pMT, pValueClasses);
            }

            // We need a new ArgDestination that points to the stack copy
            argDest = ArgDestination(pStackCopy, 0, NULL);
        }

        InvokeUtil::CopyArg(th, &(gc.args->m_Array[i]), &argDest);
    }

    ENDFORBIDGC();
    }

#ifdef FEATURE_CORRUPTING_EXCEPTIONS
    // By default, set the flag in TES indicating the reflection target can handle CSE.
    // This flag is used in CEHelper::CanIDispatchTargetHandleException.
    pThread->GetExceptionState()->SetCanReflectionTargetHandleException(TRUE);
#endif // FEATURE_CORRUPTING_EXCEPTIONS

    if (pValueClasses != NULL)
    {
        pProtectValueClassFrame = new (_alloca (sizeof (FrameWithCookie<ProtectValueClassFrame>))) 
            FrameWithCookie<ProtectValueClassFrame>(pThread, pValueClasses);
    }

    // The sole purpose of having this frame is to tell the debugger that we have a catch handler here 
    // which may swallow managed exceptions.  The debugger needs this in order to send a 
    // CatchHandlerFound (CHF) notification.
    FrameWithCookie<DebuggerU2MCatchHandlerFrame> catchFrame(pThread);

    // Call the method
    bool fExceptionThrown = false;
    EX_TRY_THREAD(pThread) {
        CallDescrWorkerReflectionWrapper(&callDescrData, &catchFrame);
    } EX_CATCH {
        // Rethrow transient exceptions for constructors for backward compatibility
        if (fConstructor && GET_EXCEPTION()->IsTransient())
        {
            EX_RETHROW;
        }

        // Abuse retval to store the exception object
        gc.retVal = GET_THROWABLE();
        _ASSERTE(gc.retVal);

        fExceptionThrown = true;
    } EX_END_CATCH(SwallowAllExceptions);

    catchFrame.Pop(pThread);

    // Now that we are safely out of the catch block, we can create and raise the
    // TargetInvocationException.
    if (fExceptionThrown)
    {
        ThrowInvokeMethodException(pMeth, gc.retVal);
    }

    // It is still illegal to do a GC here.  The return type might have/contain GC pointers.
    if (fConstructor)
    {
        // We have a special case for Strings...The object is returned...
        if (ownerType == TypeHandle(g_pStringClass)) {
            PVOID pReturnValue = &callDescrData.returnValue;
            gc.retVal = *(OBJECTREF *)pReturnValue;
        }

        // If it is a Nullable<T>, box it using Nullable<T> conventions.
        // TODO: this double allocates on constructions which is wasteful
        gc.retVal = Nullable::NormalizeBox(gc.retVal);
    }
    else
    if (retType == ELEMENT_TYPE_VALUETYPE)
    {
        _ASSERTE(gc.retVal != NULL);

        // if the structure is returned by value, then we need to copy in the boxed object
        // we have allocated for this purpose.
        if (!fHasRetBuffArg) 
        {
            CopyValueClass(gc.retVal->GetData(), &callDescrData.returnValue, gc.retVal->GetMethodTable(), gc.retVal->GetAppDomain());
        }
        else if (pRetBufStackCopy)
        {
            CopyValueClass(gc.retVal->GetData(), pRetBufStackCopy, gc.retVal->GetMethodTable(), gc.retVal->GetAppDomain());
        }
        // From here on out, it is OK to have GCs since the return object (which may have had
        // GC pointers has been put into a GC object and thus protected. 

            // TODO this creates two objects which is inefficient
            // If the return type is a Nullable<T> box it into the correct form
        gc.retVal = Nullable::NormalizeBox(gc.retVal);
    }
    else 
    {
        gc.retVal = InvokeUtil::CreateObject(retTH, &callDescrData.returnValue);
    }

    while (byRefToNullables != NULL) {
        OBJECTREF obj = Nullable::Box(byRefToNullables->data, byRefToNullables->type.GetMethodTable());
        SetObjectReference(&gc.args->m_Array[byRefToNullables->argNum], obj, gc.args->GetAppDomain());
        byRefToNullables = byRefToNullables->next;
    }

    if (pProtectValueClassFrame != NULL)
        pProtectValueClassFrame->Pop(pThread);

    END_INTERIOR_STACK_PROBE;
    }

Done:
    HELPER_METHOD_FRAME_END();

    return OBJECTREFToObject(gc.retVal);
}
FCIMPLEND

#ifdef FEATURE_SERIALIZATION
FCIMPL4(void, RuntimeMethodHandle::SerializationInvoke, 
    ReflectMethodObject *pMethodUNSAFE, Object* targetUNSAFE, Object* serializationInfoUNSAFE, struct StreamingContextData * pContext) {
    FCALL_CONTRACT;

    struct _gc
    {
        OBJECTREF       target;
        OBJECTREF       serializationInfo;
        REFLECTMETHODREF refMethod;
    } gc;

    gc.target               = (OBJECTREF)      targetUNSAFE;
    gc.serializationInfo    = (OBJECTREF)      serializationInfoUNSAFE;
    gc.refMethod = (REFLECTMETHODREF)ObjectToOBJECTREF(pMethodUNSAFE);

    MethodDesc* pMethod = pMethodUNSAFE->GetMethod();

    Assembly *pAssem = pMethod->GetAssembly();

    if (pAssem->IsIntrospectionOnly())
        FCThrowExVoid(kInvalidOperationException, IDS_EE_CODEEXECUTION_IN_INTROSPECTIVE_ASSEMBLY, NULL, NULL, NULL);

    if (pAssem->IsDynamic() && !pAssem->HasRunAccess())
        FCThrowResVoid(kNotSupportedException, W("NotSupported_DynamicAssemblyNoRunAccess"));

    HELPER_METHOD_FRAME_BEGIN_PROTECT(gc);

    {
        ARG_SLOT newArgs[3];

        // Nullable<T> does not support the ISerializable constructor, so we should never get here.  
        _ASSERTE(!Nullable::IsNullableType(gc.target->GetMethodTable()));

        if (pMethod == MscorlibBinder::GetMethod(METHOD__WINDOWS_IDENTITY__SERIALIZATION_CTOR))
        {
            // WindowsIdentity.ctor takes only one argument
            MethodDescCallSite method(pMethod, &gsig_IM_SerInfo_RetVoid, &gc.target);

            // NO GC AFTER THIS POINT
            // Copy "this" pointer: only unbox if type is value type and method is not unboxing stub
            if (pMethod->GetMethodTable()->IsValueType() && !pMethod->IsUnboxingStub())
                newArgs[0] = PtrToArgSlot(gc.target->UnBox());
            else
                newArgs[0] = ObjToArgSlot(gc.target);

            newArgs[1] = ObjToArgSlot(gc.serializationInfo);

            TryCallMethod(&method, newArgs);
        }
        else
        {
            //
            // Use hardcoded sig for performance
            //
            MethodDescCallSite method(pMethod, &gsig_IM_SerInfo_StrContext_RetVoid, &gc.target);

            // NO GC AFTER THIS POINT
            // Copy "this" pointer: only unbox if type is value type and method is not unboxing stub
            if (pMethod->GetMethodTable()->IsValueType() && !pMethod->IsUnboxingStub())
                newArgs[0] = PtrToArgSlot(gc.target->UnBox());
            else
                newArgs[0] = ObjToArgSlot(gc.target);

            newArgs[1] = ObjToArgSlot(gc.serializationInfo);

#ifdef _WIN64
            //
            // on win64 the struct does not fit in an ARG_SLOT, so we pass it by reference
            //
            static_assert_no_msg(sizeof(*pContext) > sizeof(ARG_SLOT));
            newArgs[2] = PtrToArgSlot(pContext);
#else // _WIN64
            //
            // on x86 the struct fits in an ARG_SLOT, so we pass it by value
            //
            static_assert_no_msg(sizeof(*pContext) == sizeof(ARG_SLOT));
            newArgs[2] = *(ARG_SLOT*)pContext;
#endif // _WIN64

            TryCallMethod(&method, newArgs);
        }
    }

    HELPER_METHOD_FRAME_END_POLL();
}
FCIMPLEND
#endif // FEATURE_SERIALIZATION

struct SkipStruct {
    StackCrawlMark* pStackMark;
    MethodDesc*     pMeth;
};

// This method is called by the GetMethod function and will crawl backward
//  up the stack for integer methods.
static StackWalkAction SkipMethods(CrawlFrame* frame, VOID* data) {
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;
    
    SkipStruct* pSkip = (SkipStruct*) data;

    MethodDesc *pFunc = frame->GetFunction();

    /* We asked to be called back only for functions */
    _ASSERTE(pFunc);

    // The check here is between the address of a local variable
    // (the stack mark) and a pointer to the EIP for a frame
    // (which is actually the pointer to the return address to the
    // function from the previous frame). So we'll actually notice
    // which frame the stack mark was in one frame later. This is
    // fine since we only implement LookForMyCaller.
    _ASSERTE(*pSkip->pStackMark == LookForMyCaller);
    if (!frame->IsInCalleesFrames(pSkip->pStackMark))
        return SWA_CONTINUE;

    if (pFunc->RequiresInstMethodDescArg())
    {
        pSkip->pMeth = (MethodDesc *) frame->GetParamTypeArg();
        if (pSkip->pMeth == NULL)
            pSkip->pMeth = pFunc;
    }
    else
        pSkip->pMeth = pFunc;
    return SWA_ABORT;
}

// Return the MethodInfo that represents the current method (two above this one)
FCIMPL1(ReflectMethodObject*, RuntimeMethodHandle::GetCurrentMethod, StackCrawlMark* stackMark) {
    FCALL_CONTRACT;
    REFLECTMETHODREF pRet = NULL;
    
    HELPER_METHOD_FRAME_BEGIN_RET_0();
    SkipStruct skip;
    skip.pStackMark = stackMark;
    skip.pMeth = 0;
    StackWalkFunctions(GetThread(), SkipMethods, &skip);

    // If C<Foo>.m<Bar> was called, the stack walker returns C<object>.m<object>. We cannot
    // get know that the instantiation used Foo or Bar at that point. So the next best thing
    // is to return C<T>.m<P> and that's what LoadTypicalMethodDefinition will do for us. 

    if (skip.pMeth != NULL)
        pRet = skip.pMeth->LoadTypicalMethodDefinition()->GetStubMethodInfo();
    else
        pRet = NULL;

    HELPER_METHOD_FRAME_END();
   
    return (ReflectMethodObject*)OBJECTREFToObject(pRet);
}
FCIMPLEND

static OBJECTREF DirectObjectFieldGet(FieldDesc *pField, TypeHandle fieldType, TypeHandle enclosingType, TypedByRef *pTarget, CLR_BOOL *pDomainInitialized) {
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;

        PRECONDITION(CheckPointer(pField));
    }
    CONTRACTL_END;
    
    OBJECTREF refRet;
    OBJECTREF objref = NULL;
    GCPROTECT_BEGIN(objref);
    if (!pField->IsStatic()) {
        objref = ObjectToOBJECTREF(*((Object**)pTarget->data));
    }

    InvokeUtil::ValidateObjectTarget(pField, enclosingType, &objref);
    refRet = InvokeUtil::GetFieldValue(pField, fieldType, &objref, enclosingType, pDomainInitialized);
    GCPROTECT_END();
    return refRet;
}

FCIMPL4(Object*, RuntimeFieldHandle::GetValueDirect, ReflectFieldObject *pFieldUNSAFE, ReflectClassBaseObject *pFieldTypeUNSAFE, TypedByRef *pTarget, ReflectClassBaseObject *pDeclaringTypeUNSAFE) {
    CONTRACTL {
        FCALL_CHECK;
    }
    CONTRACTL_END;

    struct 
    {
        REFLECTCLASSBASEREF refFieldType;
        REFLECTCLASSBASEREF refDeclaringType;
        REFLECTFIELDREF refField;
    }gc;
    gc.refFieldType = (REFLECTCLASSBASEREF)ObjectToOBJECTREF(pFieldTypeUNSAFE);
    gc.refDeclaringType = (REFLECTCLASSBASEREF)ObjectToOBJECTREF(pDeclaringTypeUNSAFE);
    gc.refField = (REFLECTFIELDREF)ObjectToOBJECTREF(pFieldUNSAFE);

    if ((gc.refFieldType == NULL) || (gc.refField == NULL))
        FCThrowRes(kArgumentNullException, W("Arg_InvalidHandle"));

    TypeHandle fieldType = gc.refFieldType->GetType();

    FieldDesc *pField = gc.refField->GetField();

    Assembly *pAssem = pField->GetModule()->GetAssembly();

    if (pAssem->IsIntrospectionOnly())
        FCThrowEx(kInvalidOperationException, IDS_EE_CODEEXECUTION_IN_INTROSPECTIVE_ASSEMBLY, NULL, NULL, NULL);

    // We should throw NotSupportedException here. 
    // But for backward compatibility we are throwing FieldAccessException instead.
    if (pAssem->IsDynamic() && !pAssem->HasRunAccess())
        FCThrow(kFieldAccessException);

    OBJECTREF refRet  = NULL;
    CorElementType fieldElType;

    HELPER_METHOD_FRAME_BEGIN_RET_PROTECT(gc);

    // Find the Object and its type
    TypeHandle targetType = pTarget->type;
    _ASSERTE(gc.refDeclaringType == NULL || !gc.refDeclaringType->GetType().IsTypeDesc());
    MethodTable *pEnclosingMT = (gc.refDeclaringType != NULL ? gc.refDeclaringType->GetType() : TypeHandle()).AsMethodTable();

    // Verify the callee/caller access
    if (!pField->IsPublic() || (pEnclosingMT != NULL && !pEnclosingMT->IsExternallyVisible()))
    {

        bool targetRemoted = false;

#ifndef FEATURE_CORECLR
        targetRemoted = !targetType.IsNull() && InvokeUtil::IsTargetRemoted(pField, targetType.AsMethodTable());
#endif //FEATURE_CORECLR

        RefSecContext sCtx(InvokeUtil::GetInvocationAccessCheckType(targetRemoted));

        MethodTable* pInstanceMT = NULL;
        if (!pField->IsStatic())
        {
            if (!targetType.IsTypeDesc())
                pInstanceMT = targetType.AsMethodTable();
        }

        //TODO: missing check that the field is consistent

        // Perform the normal access check (caller vs field).
        InvokeUtil::CanAccessField(&sCtx,
                                   pEnclosingMT,
                                   pInstanceMT,
                                   pField);
    }

    CLR_BOOL domainInitialized = FALSE;
    if (pField->IsStatic() || !targetType.IsValueType()) {
        refRet = DirectObjectFieldGet(pField, fieldType, TypeHandle(pEnclosingMT), pTarget, &domainInitialized);
        goto lExit;
    }

    // Validate that the target type can be cast to the type that owns this field info.
    if (!targetType.CanCastTo(TypeHandle(pEnclosingMT)))
        COMPlusThrowArgumentException(W("obj"), NULL);

    // This is a workaround because from the previous case we may end up with an
    //  Enum.  We want to process it here.
    // Get the value from the field
    void* p;
    fieldElType = fieldType.GetSignatureCorElementType();
    switch (fieldElType) {
    case ELEMENT_TYPE_VOID:
        _ASSERTE(!"Void used as Field Type!");
        COMPlusThrow(kInvalidProgramException);

    case ELEMENT_TYPE_BOOLEAN:  // boolean
    case ELEMENT_TYPE_I1:       // byte
    case ELEMENT_TYPE_U1:       // unsigned byte
    case ELEMENT_TYPE_I2:       // short
    case ELEMENT_TYPE_U2:       // unsigned short
    case ELEMENT_TYPE_CHAR:     // char
    case ELEMENT_TYPE_I4:       // int
    case ELEMENT_TYPE_U4:       // unsigned int
    case ELEMENT_TYPE_I:
    case ELEMENT_TYPE_U:
    case ELEMENT_TYPE_R4:       // float
    case ELEMENT_TYPE_I8:       // long
    case ELEMENT_TYPE_U8:       // unsigned long
    case ELEMENT_TYPE_R8:       // double
    case ELEMENT_TYPE_VALUETYPE:
        _ASSERTE(!fieldType.IsTypeDesc());
        p = ((BYTE*) pTarget->data) + pField->GetOffset();
        refRet = fieldType.AsMethodTable()->Box(p);
        break;

    case ELEMENT_TYPE_OBJECT:
    case ELEMENT_TYPE_CLASS:
    case ELEMENT_TYPE_SZARRAY:          // Single Dim, Zero
    case ELEMENT_TYPE_ARRAY:            // general array
        p = ((BYTE*) pTarget->data) + pField->GetOffset();
        refRet = ObjectToOBJECTREF(*(Object**) p);
        break;

    case ELEMENT_TYPE_PTR:
        {
            p = ((BYTE*) pTarget->data) + pField->GetOffset();

            refRet = InvokeUtil::CreatePointer(fieldType, *(void **)p);

            break;
        }

    default:
        _ASSERTE(!"Unknown Type");
        // this is really an impossible condition
        COMPlusThrow(kNotSupportedException);
    }

lExit: ;
    HELPER_METHOD_FRAME_END();
    return OBJECTREFToObject(refRet);
}
FCIMPLEND

static void DirectObjectFieldSet(FieldDesc *pField, TypeHandle fieldType, TypeHandle enclosingType, TypedByRef *pTarget, OBJECTREF *pValue, CLR_BOOL *pDomainInitialized) {
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    
        PRECONDITION(CheckPointer(pField));
        PRECONDITION(!fieldType.IsNull());
    }
    CONTRACTL_END;

    OBJECTREF objref = NULL;
    GCPROTECT_BEGIN(objref);
    if (!pField->IsStatic()) {
        objref = ObjectToOBJECTREF(*((Object**)pTarget->data));
    }
    // Validate the target/fld type relationship
    InvokeUtil::ValidateObjectTarget(pField, enclosingType, &objref);

    InvokeUtil::ValidField(fieldType, pValue);
    InvokeUtil::SetValidField(pField->GetFieldType(), fieldType, pField, &objref, pValue, enclosingType, pDomainInitialized);
    GCPROTECT_END();
}

FCIMPL5(void, RuntimeFieldHandle::SetValueDirect, ReflectFieldObject *pFieldUNSAFE, ReflectClassBaseObject *pFieldTypeUNSAFE, TypedByRef *pTarget, Object *valueUNSAFE, ReflectClassBaseObject *pContextTypeUNSAFE) {
    CONTRACTL {
        FCALL_CHECK;
    }
    CONTRACTL_END;

    struct _gc
    {
        OBJECTREF       oValue;
        REFLECTCLASSBASEREF pFieldType;
        REFLECTCLASSBASEREF pContextType;
        REFLECTFIELDREF refField;
    }gc;

    gc.oValue   = ObjectToOBJECTREF(valueUNSAFE);
    gc.pFieldType = (REFLECTCLASSBASEREF)ObjectToOBJECTREF(pFieldTypeUNSAFE);
    gc.pContextType = (REFLECTCLASSBASEREF)ObjectToOBJECTREF(pContextTypeUNSAFE);
    gc.refField = (REFLECTFIELDREF)ObjectToOBJECTREF(pFieldUNSAFE);

    if ((gc.pFieldType == NULL) || (gc.refField == NULL))
        FCThrowResVoid(kArgumentNullException, W("Arg_InvalidHandle"));

    TypeHandle fieldType = gc.pFieldType->GetType();
    TypeHandle contextType = (gc.pContextType != NULL) ? gc.pContextType->GetType() : NULL;

    FieldDesc *pField = gc.refField->GetField();

    Assembly *pAssem = pField->GetModule()->GetAssembly();

    if (pAssem->IsIntrospectionOnly())
        FCThrowExVoid(kInvalidOperationException, IDS_EE_CODEEXECUTION_IN_INTROSPECTIVE_ASSEMBLY, NULL, NULL, NULL);

    // We should throw NotSupportedException here. 
    // But for backward compatibility we are throwing FieldAccessException instead.
    if (pAssem->IsDynamic() && !pAssem->HasRunAccess())
        FCThrowVoid(kFieldAccessException);

    BYTE           *pDst = NULL;
    ARG_SLOT        value = NULL;
    CorElementType  fieldElType;

    HELPER_METHOD_FRAME_BEGIN_PROTECT(gc);

    // Find the Object and its type
    TypeHandle targetType = pTarget->type;
    MethodTable *pEnclosingMT = contextType.GetMethodTable();

    {
        // Verify that the value passed can be widened into the target
        InvokeUtil::ValidField(fieldType, &gc.oValue);

        // Verify that this is not a Final Field
        DWORD attr = pField->GetAttributes(); // should we cache?
        if (IsFdInitOnly(attr)) {
            TryDemand(SECURITY_SERIALIZATION, kFieldAccessException, W("Acc_ReadOnly"));
        }
        if (IsFdHasFieldRVA(attr)) {
            TryDemand(SECURITY_SKIP_VER, kFieldAccessException, W("Acc_RvaStatic"));
        }
        if (IsFdLiteral(attr))
            COMPlusThrow(kFieldAccessException,W("Acc_ReadOnly"));

        // Verify the callee/caller access
        if (!pField->IsPublic() || (pEnclosingMT != NULL && !pEnclosingMT->IsExternallyVisible())) 
        {
            // security and consistency checks

            bool targetRemoted = false;
#ifndef FEATURE_CORECLR
            targetRemoted = targetType.IsNull() && InvokeUtil::IsTargetRemoted(pField, targetType.AsMethodTable());
#endif //FEATURE_CORECLR

            RefSecContext sCtx(InvokeUtil::GetInvocationAccessCheckType(targetRemoted));

            MethodTable* pInstanceMT = NULL;
            if (!pField->IsStatic()) {
                if (!targetType.IsTypeDesc())
                    pInstanceMT = targetType.AsMethodTable();
            }

            //TODO: missing check that the field is consistent

            // Perform the normal access check (caller vs field).
            InvokeUtil::CanAccessField(&sCtx,
                                       pEnclosingMT,
                                       pInstanceMT,
                                       pField);
        }

    }

    CLR_BOOL domainInitialized = FALSE;
    if (pField->IsStatic() || !targetType.IsValueType()) {
        DirectObjectFieldSet(pField, fieldType, TypeHandle(pEnclosingMT), pTarget, &gc.oValue, &domainInitialized);
        goto lExit;
    }

    if (gc.oValue == NULL && fieldType.IsValueType() && !Nullable::IsNullableType(fieldType))
        COMPlusThrowArgumentNull(W("value"));

    // Validate that the target type can be cast to the type that owns this field info.
    if (!targetType.CanCastTo(TypeHandle(pEnclosingMT)))
        COMPlusThrowArgumentException(W("obj"), NULL);

    // Set the field
    fieldElType = fieldType.GetInternalCorElementType();
    if (ELEMENT_TYPE_BOOLEAN <= fieldElType && fieldElType <= ELEMENT_TYPE_R8) {
        CorElementType objType = gc.oValue->GetTypeHandle().GetInternalCorElementType();
        if (objType != fieldElType)
            InvokeUtil::CreatePrimitiveValue(fieldElType, objType, gc.oValue, &value);
        else
            value = *(ARG_SLOT*)gc.oValue->UnBox();
    }
    pDst = ((BYTE*) pTarget->data) + pField->GetOffset();

    switch (fieldElType) {
    case ELEMENT_TYPE_VOID:
        _ASSERTE(!"Void used as Field Type!");
        COMPlusThrow(kInvalidProgramException);

    case ELEMENT_TYPE_BOOLEAN:  // boolean
    case ELEMENT_TYPE_I1:       // byte
    case ELEMENT_TYPE_U1:       // unsigned byte
        VolatileStore((UINT8*)pDst, *(UINT8*)&value);
    break;

    case ELEMENT_TYPE_I2:       // short
    case ELEMENT_TYPE_U2:       // unsigned short
    case ELEMENT_TYPE_CHAR:     // char
        VolatileStore((UINT16*)pDst, *(UINT16*)&value);
    break;

    case ELEMENT_TYPE_I4:       // int
    case ELEMENT_TYPE_U4:       // unsigned int
    case ELEMENT_TYPE_R4:       // float
        VolatileStore((UINT32*)pDst, *(UINT32*)&value);
    break;

    case ELEMENT_TYPE_I8:       // long
    case ELEMENT_TYPE_U8:       // unsigned long
    case ELEMENT_TYPE_R8:       // double
        VolatileStore((UINT64*)pDst, *(UINT64*)&value);
    break;

    case ELEMENT_TYPE_I:
    {
        INT_PTR valuePtr = (INT_PTR) InvokeUtil::GetIntPtrValue(gc.oValue);
        VolatileStore((INT_PTR*) pDst, valuePtr);
    }    
    break;
    case ELEMENT_TYPE_U:
    {
        UINT_PTR valuePtr = (UINT_PTR) InvokeUtil::GetIntPtrValue(gc.oValue);
        VolatileStore((UINT_PTR*) pDst, valuePtr);
    }
    break;

    case ELEMENT_TYPE_PTR:      // pointers
        if (gc.oValue != 0) {
            value = 0;
            if (MscorlibBinder::IsClass(gc.oValue->GetMethodTable(), CLASS__POINTER)) {
                value = (size_t) InvokeUtil::GetPointerValue(gc.oValue);
#ifdef _MSC_VER
#pragma warning(disable: 4267) //work-around for compiler
#endif
                VolatileStore((size_t*) pDst, (size_t) value);
#ifdef _MSC_VER
#pragma warning(default: 4267)
#endif
                break;
            }
        }
    // drop through
    case ELEMENT_TYPE_FNPTR:
    {
        value = 0;
        if (gc.oValue != 0) {
            CorElementType objType = gc.oValue->GetTypeHandle().GetInternalCorElementType();
            InvokeUtil::CreatePrimitiveValue(objType, objType, gc.oValue, &value);
        }
#ifdef _MSC_VER
#pragma warning(disable: 4267) //work-around for compiler
#endif
        VolatileStore((size_t*) pDst, (size_t) value);
#ifdef _MSC_VER
#pragma warning(default: 4267)
#endif
    }
    break;

    case ELEMENT_TYPE_SZARRAY:          // Single Dim, Zero
    case ELEMENT_TYPE_ARRAY:            // General Array
    case ELEMENT_TYPE_CLASS:
    case ELEMENT_TYPE_OBJECT:
        SetObjectReferenceUnchecked((OBJECTREF*)pDst, gc.oValue);
    break;

    case ELEMENT_TYPE_VALUETYPE:
    {
        _ASSERTE(!fieldType.IsTypeDesc());
        MethodTable* pMT = fieldType.AsMethodTable();

        // If we have a null value then we must create an empty field
        if (gc.oValue == 0)
            InitValueClass(pDst, pMT);
        else {
            pMT->UnBoxIntoUnchecked(pDst, gc.oValue);
        }
    }
    break;

    default:
        _ASSERTE(!"Unknown Type");
        // this is really an impossible condition
        COMPlusThrow(kNotSupportedException);
    }

lExit: ;
    HELPER_METHOD_FRAME_END();
}
FCIMPLEND

void QCALLTYPE ReflectionInvocation::CompileMethod(MethodDesc * pMD)
{
    QCALL_CONTRACT;

    // Argument is checked on the managed side
    PRECONDITION(pMD != NULL);

    if (!pMD->IsPointingToPrestub())
        return;

    BEGIN_QCALL;
    pMD->DoPrestub(NULL);
    END_QCALL;
}

// This method triggers the class constructor for a give type
FCIMPL1(void, ReflectionInvocation::RunClassConstructor, ReflectClassBaseObject *pTypeUNSAFE)
{
    FCALL_CONTRACT;
    
    REFLECTCLASSBASEREF refType = (REFLECTCLASSBASEREF)ObjectToOBJECTREF(pTypeUNSAFE);

    if (refType == NULL)
        FCThrowArgumentVoidEx(kArgumentException, NULL, W("InvalidOperation_HandleIsNotInitialized"));

    TypeHandle typeHnd = refType->GetType();
    if (typeHnd.IsTypeDesc())
        return;

    MethodTable *pMT = typeHnd.AsMethodTable();

    Assembly *pAssem = pMT->GetAssembly();

    if (pAssem->IsIntrospectionOnly())
        FCThrowExVoid(kInvalidOperationException, IDS_EE_CODEEXECUTION_IN_INTROSPECTIVE_ASSEMBLY, NULL, NULL, NULL);

    if (pAssem->IsDynamic() && !pAssem->HasRunAccess())
    {
        FCThrowResVoid(kNotSupportedException, W("NotSupported_DynamicAssemblyNoRunAccess"));
    }

    if (!pMT->IsClassInited()) 
    {
        HELPER_METHOD_FRAME_BEGIN_1(refType);

        // We perform the access check only on CoreCLR for backward compatibility.
#ifdef FEATURE_CORECLR
        RefSecContext sCtx(InvokeUtil::GetInvocationAccessCheckType());
        InvokeUtil::CanAccessClass(&sCtx, pMT);
#endif //FEATURE_CORECLR

        pMT->CheckRestore();
        pMT->EnsureInstanceActive();
        pMT->CheckRunClassInitThrowing();

        HELPER_METHOD_FRAME_END();
    }
}
FCIMPLEND

// This method triggers the module constructor for a give module
FCIMPL1(void, ReflectionInvocation::RunModuleConstructor, ReflectModuleBaseObject *pModuleUNSAFE) {
    FCALL_CONTRACT;
    
    REFLECTMODULEBASEREF refModule = (REFLECTMODULEBASEREF)ObjectToOBJECTREF(pModuleUNSAFE);

    if(refModule == NULL)
        FCThrowArgumentVoidEx(kArgumentException, NULL, W("InvalidOperation_HandleIsNotInitialized"));

    Module *pModule = refModule->GetModule();

    Assembly *pAssem = pModule->GetAssembly();

    if (pAssem->IsIntrospectionOnly())
        FCThrowExVoid(kInvalidOperationException, IDS_EE_CODEEXECUTION_IN_INTROSPECTIVE_ASSEMBLY, NULL, NULL, NULL);

    if (pAssem->IsDynamic() && !pAssem->HasRunAccess())
        FCThrowResVoid(kNotSupportedException, W("NotSupported_DynamicAssemblyNoRunAccess"));

    DomainFile *pDomainFile = pModule->FindDomainFile(GetAppDomain());
    if (pDomainFile==NULL || !pDomainFile->IsActive())
    {
        HELPER_METHOD_FRAME_BEGIN_1(refModule);
        if(pDomainFile==NULL)
            pDomainFile=pModule->GetDomainFile();
        pDomainFile->EnsureActive();
        HELPER_METHOD_FRAME_END();
    }
}
FCIMPLEND

#ifndef FEATURE_CORECLR
// This method triggers a given method to be jitted
FCIMPL3(void, ReflectionInvocation::PrepareMethod, ReflectMethodObject* pMethodUNSAFE, TypeHandle *pInstantiation, UINT32 cInstantiation)
{
    CONTRACTL {
        FCALL_CHECK;
        PRECONDITION(CheckPointer(pMethodUNSAFE, NULL_OK));
        PRECONDITION(CheckPointer(pInstantiation, NULL_OK));
    }
    CONTRACTL_END;
    
    REFLECTMETHODREF refMethod = (REFLECTMETHODREF)ObjectToOBJECTREF(pMethodUNSAFE);

    if (refMethod == NULL)
        FCThrowArgumentVoidEx(kArgumentException, NULL, W("InvalidOperation_HandleIsNotInitialized"));

    MethodDesc *pMD = refMethod->GetMethod();
    
    HELPER_METHOD_FRAME_BEGIN_1(refMethod);

    if (pMD->IsAbstract())
        COMPlusThrowArgumentNull(W("method"), W("Argument_CannotPrepareAbstract"));

    pMD->CheckRestore();

    MethodTable * pExactMT = pMD->GetMethodTable();
    if (pInstantiation != NULL)
    {
        // We were handed an instantiation, check that the method expects it and the right number of types has been provided (the
        // caller supplies one array containing the class instantiation immediately followed by the method instantiation).
        if (cInstantiation != (pMD->GetNumGenericMethodArgs() + pMD->GetNumGenericClassArgs()))
            COMPlusThrow(kArgumentException, W("Argument_InvalidGenericInstantiation"));

        // We need to find the actual class and/or method instantiations, even though we've been passed them. This is an issue of
        // lifetime -- the instantiation passed in will go away at some point whereas preparation of the method has the potential to
        // persist a copy of the instantiation pointer. By finding the actual instantiation we get a stable pointer whose lifetime
        // is at least as long as the data generated by preparation.

        // Check we've got a reasonable looking instantiation.
        if (!Generics::CheckInstantiation(Instantiation(pInstantiation, cInstantiation)))
            COMPlusThrow(kArgumentException, W("Argument_InvalidGenericInstantiation"));
        for (ULONG i = 0; i < cInstantiation; i++)
            if (pInstantiation[i].ContainsGenericVariables())
                COMPlusThrow(kArgumentException, W("Argument_InvalidGenericInstantiation"));

        // Load the exact type of the method if it needs to be instantiated (because it's a generic type definition, e.g. C<T>, or a
        // shared type instantiation, e.g. C<Object>).
        if (pExactMT->IsGenericTypeDefinition() || pExactMT->IsSharedByGenericInstantiations())
        {
            TypeHandle thExactType = ClassLoader::LoadGenericInstantiationThrowing(pMD->GetModule(),
                                                                                   pMD->GetMethodTable()->GetCl(),
                                                                                   Instantiation(pInstantiation, pMD->GetNumGenericClassArgs()));
            pExactMT = thExactType.AsMethodTable();
        }

        // As for the class we might need to find a method desc with an exact instantiation if the one we have is too vague.
        // Note: IsGenericMethodDefinition implies ContainsGenericVariables so there's no need to check it separately.
        if (pMD->IsSharedByGenericInstantiations() || pMD->ContainsGenericVariables())
            pMD = MethodDesc::FindOrCreateAssociatedMethodDesc(pMD,
                                                               pExactMT,
                                                               FALSE,
                                                               Instantiation(&pInstantiation[pMD->GetNumGenericClassArgs()], pMD->GetNumGenericMethodArgs()),
                                                               FALSE);
    }
    else
    {
        // No instantiation provided, the method better not be expecting one.

        // Methods that are generic definitions (e.g. C.Foo<U>) and those that are shared (e.g. C<Object>.Foo, C.Foo<Object>) need
        // extra instantiation data.
        // Note: IsGenericMethodDefinition implies ContainsGenericVariables so there's no need to check it separately.
        if (pMD->IsSharedByGenericInstantiations() || pMD->ContainsGenericVariables())
            COMPlusThrow(kArgumentException, W("Argument_InvalidGenericInstantiation"));

        // The rest of the cases (non-generics related methods, instantiating stubs, methods instantiated over non-shared types
        // etc.) should be able to provide their instantiation for us as necessary.
    }

    // Go prepare the method at the specified instantiation.
    PrepareMethodDesc(pMD, pExactMT->GetInstantiation(), pMD->GetMethodInstantiation());

    HELPER_METHOD_FRAME_END();
}
FCIMPLEND

// This method triggers a given delegate to be prepared.  This involves preparing the
// delegate's Invoke method and preparing the target of that Invoke.  In the case of
// a multi-cast delegate, we rely on the fact that each individual component was prepared
// prior to the Combine.  If our event sinks perform the Combine, this is always true.
// If the client calls Combine himself, he is responsible for his own preparation.
FCIMPL1(void, ReflectionInvocation::PrepareDelegate, Object* delegateUNSAFE)
{
    CONTRACTL {
        FCALL_CHECK;
        PRECONDITION(CheckPointer(delegateUNSAFE, NULL_OK));
    }
    CONTRACTL_END;
    
    if (delegateUNSAFE == NULL)
        return;

    OBJECTREF delegate = ObjectToOBJECTREF(delegateUNSAFE);
    HELPER_METHOD_FRAME_BEGIN_1(delegate);

    PrepareDelegateHelper(&delegate, FALSE);

    HELPER_METHOD_FRAME_END();
}
FCIMPLEND
#endif // !FEATURE_CORECLR

FCIMPL1(void, ReflectionInvocation::PrepareContractedDelegate, Object * delegateUNSAFE)
{
    CONTRACTL {
        FCALL_CHECK;
        PRECONDITION(CheckPointer(delegateUNSAFE, NULL_OK));
    }
    CONTRACTL_END;
    
    if (delegateUNSAFE == NULL)
        return;

    OBJECTREF delegate = ObjectToOBJECTREF(delegateUNSAFE);
    HELPER_METHOD_FRAME_BEGIN_1(delegate);

    PrepareDelegateHelper(&delegate, TRUE);

    HELPER_METHOD_FRAME_END();
}
FCIMPLEND

void ReflectionInvocation::PrepareDelegateHelper(OBJECTREF *pDelegate, BOOL onlyContractedMethod)
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(CheckPointer(pDelegate));
        PRECONDITION(CheckPointer(OBJECTREFToObject(*pDelegate)));
    }
    CONTRACTL_END;
    
    // Make sure the delegate subsystem itself is prepared.
    // Force the immediate creation of any global stubs required. This is platform specific.
#ifdef _TARGET_X86_
    {
        GCX_PREEMP();
        COMDelegate::TheDelegateInvokeStub();
    }
#endif

    MethodDesc *pMDTarget = COMDelegate::GetMethodDesc(*pDelegate);
    MethodDesc *pMDInvoke = COMDelegate::FindDelegateInvokeMethod((*pDelegate)->GetMethodTable());

    // If someone does give us a multicast delegate, then both MDs will be the same -- they
    // will both be the Delegate's Invoke member.  Normally, pMDTarget points at the method
    // the delegate is wrapping, of course.
    if (pMDTarget == pMDInvoke)
    {
        pMDTarget->CheckRestore();

        // The invoke method itself is never generic, but the delegate class itself might be.
        PrepareMethodDesc(pMDInvoke,
                          pMDInvoke->GetExactClassInstantiation((*pDelegate)->GetTypeHandle()),
                          Instantiation(),
                          onlyContractedMethod);
    }
    else
    {
        pMDTarget->CheckRestore();
        pMDInvoke->CheckRestore();

        // Prepare the eventual target method first.

        // Load the exact type of the method if it needs to be instantiated (because it's a generic type definition, e.g. C<T>, or a
        // shared type instantiation, e.g. C<Object>).
        MethodTable *pExactMT = pMDTarget->GetMethodTable();
        if (pExactMT->IsGenericTypeDefinition() || pExactMT->IsSharedByGenericInstantiations())
        {
            OBJECTREF targetObj = COMDelegate::GetTargetObject(*pDelegate);

#ifdef FEATURE_REMOTING
            // We prepare the delegate for the sole purpose of reliability (CER).
            // If the target is a transparent proxy, we cannot guarantee reliability anyway.
            if (CRemotingServices::IsTransparentProxy(OBJECTREFToObject(targetObj)))
                return;
#endif //FEATURE_REMOTING

            pExactMT = targetObj->GetMethodTable();
        }


        // For delegates with generic target methods it must be the case that we are passed an instantiating stub -- there's no
        // other way the necessary method instantiation information can be passed to us.
        // The target MD may be shared by generic instantiations as long as it does not require extra instantiation arguments.
        // We have the actual target object so we can extract the exact class instantiation from it.
        _ASSERTE(!pMDTarget->RequiresInstArg() &&
                 !pMDTarget->ContainsGenericVariables());

        PrepareMethodDesc(pMDTarget,
                          pMDTarget->GetExactClassInstantiation(TypeHandle(pExactMT)),
                          pMDTarget->GetMethodInstantiation(),
                          onlyContractedMethod);

        // Now prepare the delegate invoke method.
        // The invoke method itself is never generic, but the delegate class itself might be.
        PrepareMethodDesc(pMDInvoke,
                          pMDInvoke->GetExactClassInstantiation((*pDelegate)->GetTypeHandle()),
                          Instantiation(),
                          onlyContractedMethod);
    }
}

FCIMPL0(void, ReflectionInvocation::ProbeForSufficientStack)
{
    FCALL_CONTRACT;

#ifdef FEATURE_STACK_PROBE
    // probe for our entry point amount and throw if not enough stack
    RetailStackProbe(ADJUST_PROBE(DEFAULT_ENTRY_PROBE_AMOUNT));
#else
    FCUnique(0x69);
#endif

}
FCIMPLEND

// This method checks to see if there is sufficient stack to execute the average Framework method.
// If there is not, then it throws System.InsufficientExecutionStackException. The limit for each
// thread is precomputed when the thread is created.
FCIMPL0(void, ReflectionInvocation::EnsureSufficientExecutionStack)
{
    FCALL_CONTRACT;

    Thread *pThread = GetThread();

    // We use the address of a local variable as our "current stack pointer", which is 
    // plenty close enough for the purposes of this method.
    UINT_PTR current = reinterpret_cast<UINT_PTR>(&pThread);
    UINT_PTR limit = pThread->GetCachedStackSufficientExecutionLimit();

    if (current < limit)
    {
        FCThrowVoid(kInsufficientExecutionStackException);
    }
}
FCIMPLEND

#ifdef FEATURE_CORECLR
// As with EnsureSufficientExecutionStack, this method checks and returns whether there is 
// sufficient stack to execute the average Framework method, but rather than throwing,
// it simply returns a Boolean: true for sufficient stack space, otherwise false.
FCIMPL0(FC_BOOL_RET, ReflectionInvocation::TryEnsureSufficientExecutionStack)
{
	FCALL_CONTRACT;

	Thread *pThread = GetThread();

	// Same logic as EnsureSufficientExecutionStack
	UINT_PTR current = reinterpret_cast<UINT_PTR>(&pThread);
	UINT_PTR limit = pThread->GetCachedStackSufficientExecutionLimit();

	FC_RETURN_BOOL(current >= limit);
}
FCIMPLEND
#endif // FEATURE_CORECLR

struct ECWGCFContext
{
    BOOL fHandled;
    Frame *pStartFrame;
};

// Crawl the stack looking for Thread Abort related information (whether we're executing inside a CER or an error handling clauses
// of some sort).
StackWalkAction ECWGCFCrawlCallBack(CrawlFrame* pCf, void* data)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    ECWGCFContext *pData = (ECWGCFContext *)data;

    Frame *pFrame = pCf->GetFrame();
    if (pFrame && pFrame->GetFunction() != NULL && pFrame != pData->pStartFrame)
    {
        // We walk through a transition frame, but it is not our start frame.
        // This means ExecuteCodeWithGuarantee is not at the bottom of stack.
        pData->fHandled = TRUE;
        return SWA_ABORT;
    }

    MethodDesc *pMD = pCf->GetFunction();

    // Non-method frames don't interest us.
    if (pMD == NULL)
        return SWA_CONTINUE;

    if (!pMD->GetModule()->IsSystem())
    {
        // We walk through some user code.  This means that ExecuteCodeWithGuarantee is not at the bottom of stack.
        pData->fHandled = TRUE;
        return SWA_ABORT;
    }

    return SWA_CONTINUE;
}

struct ECWGC_Param
{
    BOOL fExceptionThrownInTryCode;
    BOOL fStackOverflow;
    struct ECWGC_GC *gc;
    ECWGC_Param()
    {
        fExceptionThrownInTryCode = FALSE;
        fStackOverflow = FALSE;
    }
};

LONG SODetectionFilter(EXCEPTION_POINTERS *ep, void* pv)
{
    WRAPPER_NO_CONTRACT;
    DefaultCatchFilterParam param(COMPLUS_EXCEPTION_EXECUTE_HANDLER);
    if (DefaultCatchFilter(ep, &param) == EXCEPTION_CONTINUE_EXECUTION)
    {
        return EXCEPTION_CONTINUE_EXECUTION;
    }

    // Record the fact that an exception occurred while running the try code.
    ECWGC_Param *pParam= (ECWGC_Param *)pv;
    pParam->fExceptionThrownInTryCode = TRUE;

    // We unwind the stack only in the case of a stack overflow.
    if (ep->ExceptionRecord->ExceptionCode == STATUS_STACK_OVERFLOW)
    {
        pParam->fStackOverflow = TRUE;
        return EXCEPTION_EXECUTE_HANDLER;
    }

    return EXCEPTION_CONTINUE_SEARCH;
}

struct ECWGC_GC
{
    DELEGATEREF     codeDelegate;
    DELEGATEREF     backoutDelegate;
    OBJECTREF       userData;
};

void ExecuteCodeWithGuaranteedCleanupBackout(ECWGC_GC *gc, BOOL fExceptionThrownInTryCode)
{
    // We need to prevent thread aborts from occuring for the duration of the call to the backout code. 
    // Once we enter managed code, the CER will take care of it as well; however without this holder, 
    // MethodDesc::Call would raise a thread abort exception if the thread is currently requesting one.
    ThreadPreventAbortHolder preventAbort;

#ifdef _DEBUG
    // We have prevented abort on this thread.  Normally we don't allow 
    // a thread to enter managed code if abort is prevented.  But here the code
    // requires the thread not be aborted.
    Thread::DisableAbortCheckHolder dach;
#endif

    GCX_COOP();

    PREPARE_NONVIRTUAL_CALLSITE_USING_METHODDESC(g_pExecuteBackoutCodeHelperMethod);

    DECLARE_ARGHOLDER_ARRAY(args, 3);

    args[ARGNUM_0] = OBJECTREF_TO_ARGHOLDER(gc->backoutDelegate);
    args[ARGNUM_1] = OBJECTREF_TO_ARGHOLDER(gc->userData);
    args[ARGNUM_2] = DWORD_TO_ARGHOLDER(fExceptionThrownInTryCode);

    CRITICAL_CALLSITE;
    CALL_MANAGED_METHOD_NORET(args);
}

void ExecuteCodeWithGuaranteedCleanupHelper (ECWGC_GC *gc)
{
    STATIC_CONTRACT_THROWS;
    STATIC_CONTRACT_MODE_COOPERATIVE;

    ECWGC_Param param;
    param.gc = gc;

    PAL_TRY(ECWGC_Param *, pParamOuter, &param)
    {
        PAL_TRY(ECWGC_Param *, pParam, pParamOuter)
        {
            PREPARE_NONVIRTUAL_CALLSITE_USING_CODE(pParam->gc->codeDelegate->GetMethodPtr());

            DECLARE_ARGHOLDER_ARRAY(args, 2);

            args[ARGNUM_0] = OBJECTREF_TO_ARGHOLDER(pParam->gc->codeDelegate->GetTarget());
            args[ARGNUM_1] = OBJECTREF_TO_ARGHOLDER(pParam->gc->userData);

            CALL_MANAGED_METHOD_NORET(args);
        }
        PAL_EXCEPT_FILTER(SODetectionFilter)
        {
        }
        PAL_ENDTRY;

        if (pParamOuter->fStackOverflow)
        {
            GCX_COOP_NO_DTOR();
        }
    }
    PAL_FINALLY
    {
        ExecuteCodeWithGuaranteedCleanupBackout(gc, param.fExceptionThrownInTryCode);
    }
    PAL_ENDTRY;

#ifdef FEATURE_STACK_PROBE
    if (param.fStackOverflow)   
        COMPlusThrowSO();
#else
    //This will not be set as clr to managed transition code will terminate the
    //process if there is an SO before SODetectionFilter() is called.
    _ASSERTE(!param.fStackOverflow);
#endif
}

//
// ExecuteCodeWithGuaranteedCleanup ensures that we will call the backout code delegate even if an SO occurs. We do this by calling the 
// try delegate from within an EX_TRY/EX_CATCH block that will catch any thrown exceptions and thus cause the stack to be unwound. This 
// guarantees that the backout delegate is called with at least DEFAULT_ENTRY_PROBE_SIZE pages of stack. After the backout delegate is called, 
// we re-raise any exceptions that occurred inside the try delegate. Note that any CER that uses large or arbitraty amounts of stack in 
// it's try block must use ExecuteCodeWithGuaranteedCleanup. 
//
// ExecuteCodeWithGuaranteedCleanup also guarantees that the backount code will be run before any filters higher up on the stack. This
// is important to prevent security exploits.
//
FCIMPL3(void, ReflectionInvocation::ExecuteCodeWithGuaranteedCleanup, Object* codeDelegateUNSAFE, Object* backoutDelegateUNSAFE, Object* userDataUNSAFE)
{
    CONTRACTL {
        FCALL_CHECK;
        PRECONDITION(CheckPointer(codeDelegateUNSAFE, NULL_OK));
        PRECONDITION(CheckPointer(backoutDelegateUNSAFE, NULL_OK));
        PRECONDITION(CheckPointer(userDataUNSAFE, NULL_OK));
    }
    CONTRACTL_END;

    ECWGC_GC gc;

    gc.codeDelegate = (DELEGATEREF)ObjectToOBJECTREF(codeDelegateUNSAFE);
    gc.backoutDelegate = (DELEGATEREF)ObjectToOBJECTREF(backoutDelegateUNSAFE);
    gc.userData = ObjectToOBJECTREF(userDataUNSAFE);

    HELPER_METHOD_FRAME_BEGIN_PROTECT(gc);

    if (gc.codeDelegate == NULL)
        COMPlusThrowArgumentNull(W("code"));
    if (gc.backoutDelegate == NULL)
        COMPlusThrowArgumentNull(W("backoutCode"));

    if (!IsCompilationProcess())
    {
        // Delegates are prepared as part of the ngen process, so only prepare the backout 
        // delegate for non-ngen processes. 
        PrepareDelegateHelper((OBJECTREF *)&gc.backoutDelegate, FALSE);

        // Make sure the managed backout code helper function has been prepared before we 
        // attempt to run the backout code.
        PrepareMethodDesc(g_pExecuteBackoutCodeHelperMethod, Instantiation(), Instantiation(), FALSE, TRUE);
    }

    ExecuteCodeWithGuaranteedCleanupHelper(&gc);

    HELPER_METHOD_FRAME_END();
}
FCIMPLEND


FCIMPL4(void, ReflectionInvocation::MakeTypedReference, TypedByRef * value, Object* targetUNSAFE, ArrayBase* fldsUNSAFE, ReflectClassBaseObject *pFieldTypeUNSAFE)
{
    CONTRACTL {
        FCALL_CHECK;
        PRECONDITION(CheckPointer(targetUNSAFE));
        PRECONDITION(CheckPointer(fldsUNSAFE));
    }
    CONTRACTL_END;
    
    DWORD offset = 0;

    struct _gc
    {
        OBJECTREF   target;
        BASEARRAYREF flds;
        REFLECTCLASSBASEREF refFieldType;
    } gc;
    gc.target  = (OBJECTREF)   targetUNSAFE;
    gc.flds   = (BASEARRAYREF) fldsUNSAFE;
    gc.refFieldType = (REFLECTCLASSBASEREF)ObjectToOBJECTREF(pFieldTypeUNSAFE);

    TypeHandle fieldType = gc.refFieldType->GetType();

    HELPER_METHOD_FRAME_BEGIN_PROTECT(gc);
    GCPROTECT_BEGININTERIOR (value)

    DWORD cnt = gc.flds->GetNumComponents();
    FieldDesc** fields = (FieldDesc**)gc.flds->GetDataPtr();
    for (DWORD i = 0; i < cnt; i++) {
        FieldDesc* pField = fields[i];
        offset += pField->GetOffset();
    }

        // Fields already are prohibted from having ArgIterator and RuntimeArgumentHandles
    _ASSERTE(!gc.target->GetTypeHandle().GetMethodTable()->IsByRefLike());

    // Create the ByRef
    value->data = ((BYTE *)(gc.target->GetAddress() + offset)) + sizeof(Object);
    value->type = fieldType;

    GCPROTECT_END();
    HELPER_METHOD_FRAME_END();
}
FCIMPLEND

FCIMPL2(void, ReflectionInvocation::SetTypedReference, TypedByRef * target, Object* objUNSAFE) {
    FCALL_CONTRACT;
    
    // <TODO>@TODO: We fixed serious bugs in this method very late in the endgame
    // for V1 RTM. So it was decided to disable this API (nobody would seem to
    // be using it anyway). If this API is enabled again, the implementation should 
    // be similar to COMArrayInfo::SetValue.
    // </TODO>
    HELPER_METHOD_FRAME_BEGIN_0();
    COMPlusThrow(kNotSupportedException);
    HELPER_METHOD_FRAME_END();
}
FCIMPLEND


// This is an internal helper function to TypedReference class.
// It extracts the object from the typed reference.
FCIMPL1(Object*, ReflectionInvocation::TypedReferenceToObject, TypedByRef * value) {
    FCALL_CONTRACT;
    
    OBJECTREF       Obj = NULL;

    TypeHandle th(value->type);

    if (th.IsNull())
        FCThrowRes(kArgumentNullException, W("ArgumentNull_TypedRefType"));

    MethodTable* pMT = th.GetMethodTable();
    PREFIX_ASSUME(NULL != pMT);

    if (pMT->IsValueType())
    {
        // value->data is protected by the caller
    HELPER_METHOD_FRAME_BEGIN_RET_1(Obj);

        Obj = pMT->Box(value->data);

        HELPER_METHOD_FRAME_END();
    }
    else {
        Obj = ObjectToOBJECTREF(*((Object**)value->data));
    }

    return OBJECTREFToObject(Obj);
}
FCIMPLEND

#ifdef _DEBUG
FCIMPL1(FC_BOOL_RET, ReflectionInvocation::IsAddressInStack, void * ptr)
{
    FCALL_CONTRACT;
    FC_RETURN_BOOL(Thread::IsAddressInCurrentStack(ptr));
}
FCIMPLEND
#endif

FCIMPL2_IV(Object*, ReflectionInvocation::CreateEnum, ReflectClassBaseObject *pTypeUNSAFE, INT64 value) {
    FCALL_CONTRACT;
    
    REFLECTCLASSBASEREF refType = (REFLECTCLASSBASEREF)ObjectToOBJECTREF(pTypeUNSAFE);

    TypeHandle typeHandle = refType->GetType();
    _ASSERTE(typeHandle.IsEnum());
    OBJECTREF obj = NULL;
    HELPER_METHOD_FRAME_BEGIN_RET_1(refType);
    MethodTable *pEnumMT = typeHandle.AsMethodTable();
    obj = pEnumMT->Box(ArgSlotEndianessFixup ((ARG_SLOT*)&value,
                                             pEnumMT->GetNumInstanceFieldBytes()));

    HELPER_METHOD_FRAME_END();
    return OBJECTREFToObject(obj);
}
FCIMPLEND

#ifdef FEATURE_COMINTEROP

static void TryGetClassFromProgID(STRINGREF className, STRINGREF server, OBJECTREF* pRefClass, DWORD bThrowOnError) {
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;

    EX_TRY
    {
        // NOTE: this call enables GC
        GetComClassFromProgID(className, server, pRefClass);
    }
    EX_CATCH
    {
        if (bThrowOnError)
        {
            EX_RETHROW;
        }
    }
    EX_END_CATCH(SwallowAllExceptions)
}

// GetClassFromProgID
// This method will return a Class object for a COM Classic object based
//  upon its ProgID.  The COM Classic object is found and a wrapper object created
FCIMPL3(Object*, ReflectionInvocation::GetClassFromProgID, StringObject* classNameUNSAFE, 
                                                           StringObject* serverUNSAFE, 
                                                           CLR_BOOL bThrowOnError) {
    FCALL_CONTRACT;
    
    REFLECTCLASSBASEREF refClass    = NULL;
    STRINGREF           className   = (STRINGREF) classNameUNSAFE;
    STRINGREF           server      = (STRINGREF) serverUNSAFE;

    HELPER_METHOD_FRAME_BEGIN_RET_2(className, server);

    GCPROTECT_BEGIN(refClass)

    // Since we will be returning a type that represents a COM component, we need
    // to make sure COM is started before we return it.
    EnsureComStarted();
    
    // Make sure a prog id was provided
    if (className == NULL)
        COMPlusThrowArgumentNull(W("progID"),W("ArgumentNull_String"));

    TryGetClassFromProgID(className, server, (OBJECTREF*) &refClass, bThrowOnError);
    GCPROTECT_END();

    HELPER_METHOD_FRAME_END();
    return OBJECTREFToObject(refClass);
}
FCIMPLEND

static void TryGetClassFromCLSID(GUID clsid, STRINGREF server, OBJECTREF* pRefClass, DWORD bThrowOnError) {
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;
    
    EX_TRY
    {
        // NOTE: this call enables GC
        GetComClassFromCLSID(clsid, server, pRefClass);
    }
    EX_CATCH
    {
        if (bThrowOnError)
        {
            EX_RETHROW;
        }
    }
    EX_END_CATCH(SwallowAllExceptions)
}

// GetClassFromCLSID
// This method will return a Class object for a COM Classic object based
//  upon its ProgID.  The COM Classic object is found and a wrapper object created
FCIMPL3(Object*, ReflectionInvocation::GetClassFromCLSID, GUID clsid, StringObject* serverUNSAFE, CLR_BOOL bThrowOnError) {
    FCALL_CONTRACT;
    
    struct _gc {
        REFLECTCLASSBASEREF refClass;
        STRINGREF           server;
    } gc;

    gc.refClass = NULL;
    gc.server   = (STRINGREF) serverUNSAFE;

    HELPER_METHOD_FRAME_BEGIN_RET_PROTECT(gc.server);

    // Since we will be returning a type that represents a COM component, we need
    // to make sure COM is started before we return it.
    EnsureComStarted();
    
    TryGetClassFromCLSID(clsid, gc.server, (OBJECTREF*) &gc.refClass, bThrowOnError);

    HELPER_METHOD_FRAME_END();
    return OBJECTREFToObject(gc.refClass);
}
FCIMPLEND


FCIMPL8(Object*, ReflectionInvocation::InvokeDispMethod, ReflectClassBaseObject* refThisUNSAFE, 
                                                         StringObject* nameUNSAFE, 
                                                         INT32 invokeAttr, 
                                                         Object* targetUNSAFE, 
                                                         PTRArray* argsUNSAFE, 
                                                         PTRArray* byrefModifiersUNSAFE, 
                                                         LCID lcid, 
                                                         PTRArray* namedParametersUNSAFE) {
    FCALL_CONTRACT;
    
    struct _gc
    {
        REFLECTCLASSBASEREF refThis;
        STRINGREF           name;
        OBJECTREF           target;
        PTRARRAYREF         args;
        PTRARRAYREF         byrefModifiers;
        PTRARRAYREF         namedParameters;
        OBJECTREF           RetObj;
    } gc;

    gc.refThis          = (REFLECTCLASSBASEREF) refThisUNSAFE;
    gc.name             = (STRINGREF)           nameUNSAFE;
    gc.target           = (OBJECTREF)           targetUNSAFE;
    gc.args             = (PTRARRAYREF)         argsUNSAFE;
    gc.byrefModifiers   = (PTRARRAYREF)         byrefModifiersUNSAFE;
    gc.namedParameters  = (PTRARRAYREF)         namedParametersUNSAFE;
    gc.RetObj           = NULL;

    HELPER_METHOD_FRAME_BEGIN_RET_PROTECT(gc);

    _ASSERTE(gc.target != NULL);
    _ASSERTE(gc.target->GetMethodTable()->IsComObjectType());

    // Unless security is turned off, we need to validate that the calling code
    // has unmanaged code access privilege.
    Security::SpecialDemand(SSWT_LATEBOUND_LINKDEMAND, SECURITY_UNMANAGED_CODE);

    WORD flags = 0;
    if (invokeAttr & BINDER_InvokeMethod)
        flags |= DISPATCH_METHOD;
    if (invokeAttr & BINDER_GetProperty)
        flags |= DISPATCH_PROPERTYGET;
    if (invokeAttr & BINDER_SetProperty)
        flags = DISPATCH_PROPERTYPUT | DISPATCH_PROPERTYPUTREF;
    if (invokeAttr & BINDER_PutDispProperty)
        flags = DISPATCH_PROPERTYPUT;
    if (invokeAttr & BINDER_PutRefDispProperty)
        flags = DISPATCH_PROPERTYPUTREF;
    if (invokeAttr & BINDER_CreateInstance)
        flags = DISPATCH_CONSTRUCT;

    IUInvokeDispMethod(&gc.refThis,
                        &gc.target,
                        (OBJECTREF*)&gc.name,
                        NULL,
                        (OBJECTREF*)&gc.args,
                        (OBJECTREF*)&gc.byrefModifiers,
                        (OBJECTREF*)&gc.namedParameters,
                        &gc.RetObj,
                        lcid,
                        flags,
                        invokeAttr & BINDER_IgnoreReturn,
                        invokeAttr & BINDER_IgnoreCase);

    HELPER_METHOD_FRAME_END();
    return OBJECTREFToObject(gc.RetObj);
}
FCIMPLEND
#endif  // FEATURE_COMINTEROP

FCIMPL2(void, ReflectionInvocation::GetGUID, ReflectClassBaseObject* refThisUNSAFE, GUID * result) {
    FCALL_CONTRACT;
    
    REFLECTCLASSBASEREF refThis = (REFLECTCLASSBASEREF) refThisUNSAFE;

    HELPER_METHOD_FRAME_BEGIN_1(refThis);
    GCPROTECT_BEGININTERIOR (result);

    if (result == NULL || refThis == NULL)
        COMPlusThrow(kNullReferenceException);

    TypeHandle type = refThis->GetType();
    if (type.IsTypeDesc()) {
        memset(result,0,sizeof(GUID));
        goto lExit;
    }

#ifdef FEATURE_COMINTEROP
    if (IsComObjectClass(type))
    {
        SyncBlock* pSyncBlock = refThis->GetSyncBlock();

#ifdef FEATURE_COMINTEROP_UNMANAGED_ACTIVATION
        ComClassFactory* pComClsFac = pSyncBlock->GetInteropInfo()->GetComClassFactory();
        if (pComClsFac)
        {
            memcpyNoGCRefs(result, &pComClsFac->m_rclsid, sizeof(GUID));
        }
        else
#endif // FEATURE_COMINTEROP_UNMANAGED_ACTIVATION
        {
            memset(result, 0, sizeof(GUID));
        }
    
        goto lExit;
    }
#endif // FEATURE_COMINTEROP

    GUID guid;
    type.AsMethodTable()->GetGuid(&guid, TRUE);
    memcpyNoGCRefs(result, &guid, sizeof(GUID));

lExit: ;
    GCPROTECT_END();
    HELPER_METHOD_FRAME_END();
}
FCIMPLEND

//*************************************************************************************************
//*************************************************************************************************
//*************************************************************************************************
//      ReflectionSerialization
//*************************************************************************************************
//*************************************************************************************************
//*************************************************************************************************
FCIMPL1(Object*, ReflectionSerialization::GetUninitializedObject, ReflectClassBaseObject* objTypeUNSAFE) {
    FCALL_CONTRACT;
    
    OBJECTREF           retVal  = NULL;
    REFLECTCLASSBASEREF objType = (REFLECTCLASSBASEREF) objTypeUNSAFE;

    HELPER_METHOD_FRAME_BEGIN_RET_NOPOLL();

    if (objType == NULL) {
        COMPlusThrowArgumentNull(W("type"), W("ArgumentNull_Type"));
    }

    TypeHandle type = objType->GetType();

    // Don't allow arrays, pointers, byrefs or function pointers.
    if (type.IsTypeDesc())
        COMPlusThrow(kArgumentException, W("Argument_InvalidValue"));

    MethodTable *pMT = type.GetMethodTable();
    PREFIX_ASSUME(pMT != NULL);

    //We don't allow unitialized strings.
    if (pMT == g_pStringClass) {
        COMPlusThrow(kArgumentException, W("Argument_NoUninitializedStrings"));
    }

    // if this is an abstract class or an interface type then we will
    //  fail this
    if (pMT->IsAbstract()) {
        COMPlusThrow(kMemberAccessException,W("Acc_CreateAbst"));
    }
    else if (pMT->ContainsGenericVariables()) {
        COMPlusThrow(kMemberAccessException,W("Acc_CreateGeneric"));
    }
    // Never allow allocation of generics actually instantiated over __Canon
    else if (pMT->IsSharedByGenericInstantiations()) {
        COMPlusThrow(kNotSupportedException, W("NotSupported_Type"));
    }
    
    // Never allow the allocation of an unitialized ContextBoundObject derived type, these must always be created with a paired
    // transparent proxy or the jit will get confused.
#ifdef FEATURE_REMOTING    
    if (pMT->IsContextful())
        COMPlusThrow(kNotSupportedException, W("NotSupported_ManagedActivation"));
#endif

#ifdef FEATURE_COMINTEROP
    // Also do not allow allocation of uninitialized RCWs (COM objects).
    if (pMT->IsComObjectType())
        COMPlusThrow(kNotSupportedException, W("NotSupported_ManagedActivation"));
#endif // FEATURE_COMINTEROP

    // If it is a nullable, return the underlying type instead.  
    if (Nullable::IsNullableType(pMT)) 
        pMT = pMT->GetInstantiation()[0].GetMethodTable();
 
    retVal = pMT->Allocate();

    HELPER_METHOD_FRAME_END();
    return OBJECTREFToObject(retVal);
}
FCIMPLEND

FCIMPL1(Object*, ReflectionSerialization::GetSafeUninitializedObject, ReflectClassBaseObject* objTypeUNSAFE) {
    FCALL_CONTRACT;
    
    OBJECTREF           retVal  = NULL;
    REFLECTCLASSBASEREF objType = (REFLECTCLASSBASEREF) objTypeUNSAFE;

    HELPER_METHOD_FRAME_BEGIN_RET_1(objType);
    
    if (objType == NULL) 
        COMPlusThrowArgumentNull(W("type"), W("ArgumentNull_Type"));

    TypeHandle type = objType->GetType();

    // Don't allow arrays, pointers, byrefs or function pointers.
    if (type.IsTypeDesc())
        COMPlusThrow(kArgumentException, W("Argument_InvalidValue"));

    MethodTable *pMT = type.GetMethodTable();
    PREFIX_ASSUME(pMT != NULL);

    //We don't allow unitialized strings.
    if (pMT == g_pStringClass) 
        COMPlusThrow(kArgumentException, W("Argument_NoUninitializedStrings"));


    // if this is an abstract class or an interface type then we will
    //  fail this
    if (pMT->IsAbstract())
        COMPlusThrow(kMemberAccessException,W("Acc_CreateAbst"));
    else if (pMT->ContainsGenericVariables()) {
        COMPlusThrow(kMemberAccessException,W("Acc_CreateGeneric"));
    }

    // Never allow the allocation of an unitialized ContextBoundObject derived type, these must always be created with a paired
    // transparent proxy or the jit will get confused.
#ifdef FEATURE_REMOTING        
    if (pMT->IsContextful())
        COMPlusThrow(kNotSupportedException, W("NotSupported_ManagedActivation"));
#endif    

#ifdef FEATURE_COMINTEROP
    // Also do not allow allocation of uninitialized RCWs (COM objects).
    if (pMT->IsComObjectType())
        COMPlusThrow(kNotSupportedException, W("NotSupported_ManagedActivation"));
#endif // FEATURE_COMINTEROP

#ifdef FEATURE_APTCA
    if (!pMT->GetAssembly()->AllowUntrustedCaller()) {
        OBJECTREF permSet = NULL;
        Security::GetPermissionInstance(&permSet, SECURITY_FULL_TRUST);
        Security::DemandSet(SSWT_LATEBOUND_LINKDEMAND, permSet);
    }
#endif // FEATURE_APTCA

#ifdef FEATURE_CAS_POLICY 
    if (pMT->GetClass()->RequiresLinktimeCheck()) {
        OBJECTREF refClassNonCasDemands = NULL;
        OBJECTREF refClassCasDemands = NULL;

        refClassCasDemands = TypeSecurityDescriptor::GetLinktimePermissions(pMT, &refClassNonCasDemands);

        if (refClassCasDemands != NULL)
            Security::DemandSet(SSWT_LATEBOUND_LINKDEMAND, refClassCasDemands);

    }
#endif // FEATURE_CAS_POLICY

    // If it is a nullable, return the underlying type instead.  
    if (Nullable::IsNullableType(pMT)) 
        pMT = pMT->GetInstantiation()[0].GetMethodTable();
 
    retVal = pMT->Allocate();

    HELPER_METHOD_FRAME_END();
    return OBJECTREFToObject(retVal);
}
FCIMPLEND

FCIMPL0(FC_BOOL_RET, ReflectionSerialization::GetEnableUnsafeTypeForwarders)
{
    FCALL_CONTRACT;
    FC_RETURN_BOOL(CLRConfig::GetConfigValue(CLRConfig::EXTERNAL_Serialization_UnsafeTypeForwarding));
}
FCIMPLEND


//*************************************************************************************************
//*************************************************************************************************
//*************************************************************************************************
//      ReflectionEnum
//*************************************************************************************************
//*************************************************************************************************
//*************************************************************************************************

FCIMPL1(Object *, ReflectionEnum::InternalGetEnumUnderlyingType, ReflectClassBaseObject *target) {
    FCALL_CONTRACT;
    
    VALIDATEOBJECT(target);
    TypeHandle th = target->GetType();
    if (!th.IsEnum())
        FCThrowArgument(NULL, NULL);

    OBJECTREF result = NULL;

    HELPER_METHOD_FRAME_BEGIN_RET_0();
    MethodTable *pMT = MscorlibBinder::GetElementType(th.AsMethodTable()->GetInternalCorElementType());
    result = pMT->GetManagedClassObject();
    HELPER_METHOD_FRAME_END();

    return OBJECTREFToObject(result);
}
FCIMPLEND

FCIMPL1(INT32, ReflectionEnum::InternalGetCorElementType, Object *pRefThis) {
    FCALL_CONTRACT;
    
    VALIDATEOBJECT(pRefThis);
    if (pRefThis == NULL)
        FCThrowArgumentNull(NULL);

    return pRefThis->GetMethodTable()->GetInternalCorElementType();
}
FCIMPLEND

//*******************************************************************************
struct TempEnumValue
{
    LPCUTF8 name;
    UINT64 value;
};

//*******************************************************************************
class TempEnumValueSorter : public CQuickSort<TempEnumValue>
{
public:
    TempEnumValueSorter(TempEnumValue *pArray, SSIZE_T iCount)
        : CQuickSort<TempEnumValue>(pArray, iCount) { LIMITED_METHOD_CONTRACT; }

    int Compare(TempEnumValue *pFirst, TempEnumValue *pSecond)
    {
        LIMITED_METHOD_CONTRACT;

        if (pFirst->value == pSecond->value)
            return 0;
        if (pFirst->value > pSecond->value)
            return 1;
        else
            return -1;
    }
};

void QCALLTYPE ReflectionEnum::GetEnumValuesAndNames(EnregisteredTypeHandle pEnumType, QCall::ObjectHandleOnStack pReturnValues, QCall::ObjectHandleOnStack pReturnNames, BOOL fGetNames)
{
    QCALL_CONTRACT;

    BEGIN_QCALL;

    TypeHandle th = TypeHandle::FromPtr(pEnumType);

    if (!th.IsEnum())
        COMPlusThrow(kArgumentException, W("Arg_MustBeEnum"));

    MethodTable *pMT = th.AsMethodTable();

    IMDInternalImport *pImport = pMT->GetMDImport();

    StackSArray<TempEnumValue> temps;
    UINT64 previousValue = 0;

    HENUMInternalHolder fieldEnum(pImport);
    fieldEnum.EnumInit(mdtFieldDef, pMT->GetCl());

    //
    // Note that we're fine treating signed types as unsigned, because all we really
    // want to do is sort them based on a convenient strong ordering.
    //

    BOOL sorted = TRUE;

    CorElementType type = pMT->GetInternalCorElementType();

    mdFieldDef field;
    while (pImport->EnumNext(&fieldEnum, &field))
    {
        DWORD dwFlags;
        IfFailThrow(pImport->GetFieldDefProps(field, &dwFlags));
        if (IsFdStatic(dwFlags))
        {
            TempEnumValue temp;

            if (fGetNames)
                IfFailThrow(pImport->GetNameOfFieldDef(field, &temp.name));

            UINT64 value = 0;

            MDDefaultValue defaultValue;
            IfFailThrow(pImport->GetDefaultValue(field, &defaultValue));

            // The following code assumes that the address of all union members is the same.
            static_assert_no_msg(offsetof(MDDefaultValue, m_byteValue) == offsetof(MDDefaultValue, m_usValue));
            static_assert_no_msg(offsetof(MDDefaultValue, m_ulValue) == offsetof(MDDefaultValue, m_ullValue));
            PVOID pValue = &defaultValue.m_byteValue;

            switch (type) {
            case ELEMENT_TYPE_I1:
                value = *((INT8 *)pValue);
                break;

            case ELEMENT_TYPE_U1:
            case ELEMENT_TYPE_BOOLEAN:
                value = *((UINT8 *)pValue);
                break;

            case ELEMENT_TYPE_I2:
                value = *((INT16 *)pValue);
                break;

            case ELEMENT_TYPE_U2:
            case ELEMENT_TYPE_CHAR:
                value = *((UINT16 *)pValue);
                break;

            case ELEMENT_TYPE_I4:
            IN_WIN32(case ELEMENT_TYPE_I:)
                value = *((INT32 *)pValue);
                break;

            case ELEMENT_TYPE_U4:
            IN_WIN32(case ELEMENT_TYPE_U:)
                value = *((UINT32 *)pValue);
                break;

            case ELEMENT_TYPE_I8:
            case ELEMENT_TYPE_U8:
            IN_WIN64(case ELEMENT_TYPE_I:)
            IN_WIN64(case ELEMENT_TYPE_U:)
                value = *((INT64 *)pValue);
                break;

            default:
                break;
            }

            temp.value = value;

            //
            // Check to see if we are already sorted.  This may seem extraneous, but is
            // actually probably the normal case.
            //

            if (previousValue > value)
                sorted = FALSE;
            previousValue = value;

            temps.Append(temp);
        }
    }

    TempEnumValue * pTemps = &(temps[0]);
    DWORD cFields = temps.GetCount();

    if (!sorted)
    {
        TempEnumValueSorter sorter(pTemps, cFields);
        sorter.Sort();
    }

    {
        GCX_COOP();

        struct gc {
            I8ARRAYREF values;
            PTRARRAYREF names;
        } gc;
        gc.values = NULL;
        gc.names = NULL;

        GCPROTECT_BEGIN(gc);

        {
            gc.values = (I8ARRAYREF) AllocatePrimitiveArray(ELEMENT_TYPE_U8, cFields);

            INT64 *pToValues = gc.values->GetDirectPointerToNonObjectElements();

            for (DWORD i = 0; i < cFields; i++) {
                pToValues[i] = pTemps[i].value;
            }

            pReturnValues.Set(gc.values);
        }
 
        if (fGetNames)
        {
            gc.names = (PTRARRAYREF) AllocateObjectArray(cFields, g_pStringClass);

            for (DWORD i = 0; i < cFields; i++) {
                STRINGREF str = StringObject::NewString(pTemps[i].name);
                gc.names->SetAt(i, str);
            }

            pReturnNames.Set(gc.names);
        }

        GCPROTECT_END();
    }

    END_QCALL;
}

FCIMPL2_IV(Object*, ReflectionEnum::InternalBoxEnum, ReflectClassBaseObject* target, INT64 value) {
    FCALL_CONTRACT;
    
    VALIDATEOBJECT(target);
    OBJECTREF ret = NULL;

    MethodTable* pMT = target->GetType().AsMethodTable();
    HELPER_METHOD_FRAME_BEGIN_RET_0();

    ret = pMT->Box(ArgSlotEndianessFixup((ARG_SLOT*)&value, pMT->GetNumInstanceFieldBytes()));

    HELPER_METHOD_FRAME_END();
    return OBJECTREFToObject(ret);
}
FCIMPLEND

//*************************************************************************************************
//*************************************************************************************************
//*************************************************************************************************
//      ReflectionBinder
//*************************************************************************************************
//*************************************************************************************************
//*************************************************************************************************

FCIMPL2(FC_BOOL_RET, ReflectionBinder::DBCanConvertPrimitive, ReflectClassBaseObject* source, ReflectClassBaseObject* target) {
    FCALL_CONTRACT;
    
    VALIDATEOBJECT(source);
    VALIDATEOBJECT(target);

    CorElementType tSRC = source->GetType().GetSignatureCorElementType();
    CorElementType tTRG = target->GetType().GetSignatureCorElementType();

    FC_RETURN_BOOL(InvokeUtil::IsPrimitiveType(tTRG) && InvokeUtil::CanPrimitiveWiden(tTRG, tSRC));
}
FCIMPLEND

FCIMPL2(FC_BOOL_RET, ReflectionBinder::DBCanConvertObjectPrimitive, Object* sourceObj, ReflectClassBaseObject* target) {
    FCALL_CONTRACT;
    
    VALIDATEOBJECT(sourceObj);
    VALIDATEOBJECT(target);

    if (sourceObj == 0)
        FC_RETURN_BOOL(true);

    TypeHandle th(sourceObj->GetMethodTable());
    CorElementType tSRC = th.GetVerifierCorElementType();

    CorElementType tTRG = target->GetType().GetSignatureCorElementType();
    FC_RETURN_BOOL(InvokeUtil::IsPrimitiveType(tTRG) && InvokeUtil::CanPrimitiveWiden(tTRG, tSRC));
}
FCIMPLEND

FCIMPL2(FC_BOOL_RET, ReflectionEnum::InternalEquals, Object *pRefThis, Object* pRefTarget)
{
    FCALL_CONTRACT;

    VALIDATEOBJECT(pRefThis);
    BOOL ret = false;
    if (pRefTarget == NULL) {
        FC_RETURN_BOOL(ret);
    }

    if( pRefThis == pRefTarget)
        FC_RETURN_BOOL(true);

    //Make sure we are comparing same type.
    MethodTable* pMTThis = pRefThis->GetMethodTable();
    _ASSERTE(!pMTThis->IsArray());  // bunch of assumptions about arrays wrong.
    if ( pMTThis != pRefTarget->GetMethodTable()) {
        FC_RETURN_BOOL(ret);
    }

    void * pThis = pRefThis->UnBox();
    void * pTarget = pRefTarget->UnBox();
    switch (pMTThis->GetNumInstanceFieldBytes()) {
    case 1:
        ret = (*(UINT8*)pThis == *(UINT8*)pTarget);
        break;
    case 2:
        ret = (*(UINT16*)pThis == *(UINT16*)pTarget);
        break;
    case 4:
        ret = (*(UINT32*)pThis == *(UINT32*)pTarget);
        break;
    case 8:
        ret = (*(UINT64*)pThis == *(UINT64*)pTarget);
        break;
    default:
        // should not reach here.
        UNREACHABLE_MSG("Incorrect Enum Type size!");
        break;
    }

    FC_RETURN_BOOL(ret);
}
FCIMPLEND

// preform (this & flags) != flags
FCIMPL2(FC_BOOL_RET, ReflectionEnum::InternalHasFlag, Object *pRefThis, Object* pRefFlags)
{
    FCALL_CONTRACT;

    VALIDATEOBJECT(pRefThis);

    BOOL cmp = false;

    _ASSERTE(pRefFlags != NULL); // Enum.cs would have thrown ArgumentNullException before calling into InternalHasFlag

    VALIDATEOBJECT(pRefFlags);

    void * pThis = pRefThis->UnBox();
    void * pFlags = pRefFlags->UnBox();

    MethodTable* pMTThis = pRefThis->GetMethodTable();

    _ASSERTE(!pMTThis->IsArray());  // bunch of assumptions about arrays wrong.
    _ASSERTE(pMTThis->GetNumInstanceFieldBytes() == pRefFlags->GetMethodTable()->GetNumInstanceFieldBytes()); // Enum.cs verifies that the types are Equivalent

    switch (pMTThis->GetNumInstanceFieldBytes()) {
    case 1:
        cmp = ((*(UINT8*)pThis & *(UINT8*)pFlags) == *(UINT8*)pFlags);
        break;
    case 2:
        cmp = ((*(UINT16*)pThis & *(UINT16*)pFlags) == *(UINT16*)pFlags);
        break;
    case 4:
        cmp = ((*(UINT32*)pThis & *(UINT32*)pFlags) == *(UINT32*)pFlags);
        break;
    case 8:
        cmp = ((*(UINT64*)pThis & *(UINT64*)pFlags) == *(UINT64*)pFlags);
        break;
    default:
        // should not reach here.
        UNREACHABLE_MSG("Incorrect Enum Type size!");
        break;
    }

    FC_RETURN_BOOL(cmp);
}
FCIMPLEND

// compare two boxed enums using their underlying enum type
FCIMPL2(int, ReflectionEnum::InternalCompareTo, Object *pRefThis, Object* pRefTarget)
{
    FCALL_CONTRACT;

    const int retIncompatibleMethodTables = 2;  // indicates that the method tables did not match
    const int retInvalidEnumType = 3; // indicates that the enum was of an unknown/unsupported unerlying type

    VALIDATEOBJECT(pRefThis);
    
    if (pRefTarget == NULL) {
        return 1; // all values are greater than null
    }

    if( pRefThis == pRefTarget)
        return 0;

    VALIDATEOBJECT(pRefTarget);

    //Make sure we are comparing same type.
    MethodTable* pMTThis = pRefThis->GetMethodTable();

    _ASSERTE(pMTThis->IsEnum());  

    if ( pMTThis != pRefTarget->GetMethodTable()) {
        return retIncompatibleMethodTables;   // error case, types incompatible
    }

    void * pThis = pRefThis->UnBox();
    void * pTarget = pRefTarget->UnBox();

    #define CMPEXPR(x1,x2) ((x1) == (x2)) ? 0 : ((x1) < (x2)) ? -1 : 1

    switch (pMTThis->GetInternalCorElementType()) {

    case ELEMENT_TYPE_I1:
        {
            INT8 i1 = *(INT8*)pThis;
            INT8 i2 = *(INT8*)pTarget;

            return CMPEXPR(i1,i2);
        }
        break;

    case ELEMENT_TYPE_I2:
        {
            INT16 i1 = *(INT16*)pThis;
            INT16 i2 = *(INT16*)pTarget;

            return CMPEXPR(i1,i2);
        }
        break;

        
    case ELEMENT_TYPE_I4:
    IN_WIN32(case ELEMENT_TYPE_I:)
        {
            INT32 i1 = *(INT32*)pThis;
            INT32 i2 = *(INT32*)pTarget;

            return CMPEXPR(i1,i2);
        }
        break;
     

    case ELEMENT_TYPE_I8:
    IN_WIN64(case ELEMENT_TYPE_I:)
        {
            INT64 i1 = *(INT64*)pThis;
            INT64 i2 = *(INT64*)pTarget;

            return CMPEXPR(i1,i2);
        }
        break;
    
    case ELEMENT_TYPE_BOOLEAN:
        {
            bool b1 = !!*(UINT8 *)pThis;
            bool b2 = !!*(UINT8 *)pTarget;

            return CMPEXPR(b1,b2);
        }
        break;

    case ELEMENT_TYPE_U1:
        {
            UINT8 u1 = *(UINT8 *)pThis;
            UINT8 u2 = *(UINT8 *)pTarget;

            return CMPEXPR(u1,u2);
        }
        break;
        
    case ELEMENT_TYPE_U2:
    case ELEMENT_TYPE_CHAR:
        {
            UINT16 u1 = *(UINT16 *)pThis;
            UINT16 u2 = *(UINT16 *)pTarget;

            return CMPEXPR(u1,u2);
        }
        break;

    case ELEMENT_TYPE_U4:
    IN_WIN32(case ELEMENT_TYPE_U:)
        {
            UINT32 u1 = *(UINT32 *)pThis;
            UINT32 u2 = *(UINT32 *)pTarget;

            return CMPEXPR(u1,u2);
        }
        break;

    case ELEMENT_TYPE_U8:
    IN_WIN64(case ELEMENT_TYPE_U:)
        {
            UINT64 u1 = *(UINT64*)pThis;
            UINT64 u2 = *(UINT64*)pTarget;

            return CMPEXPR(u1,u2);
        }
        break;

    case ELEMENT_TYPE_R4:
        {
            static_assert_no_msg(sizeof(float) == 4);

            float f1 = *(float*)pThis;
            float f2 = *(float*)pTarget;

            return CMPEXPR(f1,f2);
        }
        break;
        
    case ELEMENT_TYPE_R8:
        {
            static_assert_no_msg(sizeof(double) == 8);

            double d1 = *(double*)pThis;
            double d2 = *(double*)pTarget;

            return CMPEXPR(d1,d2);
        }
        break;

    default:
        break;
    }
   
    return retInvalidEnumType; // second error case -- unsupported enum type
}
FCIMPLEND

