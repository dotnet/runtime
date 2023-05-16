// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "common.h"
#include "reflectioninvocation.h"
#include "invokeutil.h"
#include "object.h"
#include "class.h"
#include "method.hpp"
#include "typehandle.h"
#include "field.h"
#include "eeconfig.h"
#include "vars.hpp"
#include "jitinterface.h"
#include "contractimpl.h"
#include "virtualcallstub.h"
#include "comdelegate.h"
#include "generics.h"

#ifdef FEATURE_COMINTEROP
#include "interoputil.h"
#include "runtimecallablewrapper.h"
#endif

#include "dbginterface.h"
#include "argdestination.h"

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

    OBJECTREF rv = NULL; // not protected

    HELPER_METHOD_FRAME_BEGIN_RET_PROTECT(gc);
    // There can be no GC after this until the Object is returned.
    rv = InvokeUtil::GetFieldValue(gc.refField->GetField(), fieldType, &gc.target, declaringType, pDomainInitialized);
    HELPER_METHOD_FRAME_END();

    return OBJECTREFToObject(rv);
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
        if (valueType == TypeHandle(CoreLibBinder::GetClass(CLASS__INTPTR))) {
            //
            // it's an IntPtr, it's good.
        }
        //
        // it's a System.Reflection.Pointer object

        // void* assigns to any pointer. Otherwise the type of the pointer must match
        else if (!InvokeUtil::IsVoidPtr(targetType)) {
            if (!valueType.CanCastTo(targetType))
                ret = FALSE;
        }
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

/// <summary>
///  Allocate the value type and copy the optional value into it.
/// </summary>
FCIMPL2(Object*, ReflectionInvocation::AllocateValueType, ReflectClassBaseObject *pTargetTypeUNSAFE, Object *valueUNSAFE) {
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

    HELPER_METHOD_FRAME_BEGIN_RET_PROTECT(gc);

    TypeHandle targetType = gc.refTargetType->GetType();

    // This method is only intended for value types; it is not called directly by any public APIs
    // so we don't expect validation issues here.
    _ASSERTE(targetType.IsValueType());

    MethodTable* allocMT = targetType.AsMethodTable();
    _ASSERTE(!allocMT->IsByRefLike());

    gc.obj = allocMT->Allocate();
    _ASSERTE(gc.obj != NULL);

    if (gc.value != NULL) {
        _ASSERTE(allocMT->IsEquivalentTo(gc.value->GetMethodTable()));
        CopyValueClass(gc.obj->UnBox(), gc.value->UnBox(), allocMT);
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

    FC_GC_POLL_NOT_NEEDED();

    FieldDesc* pFieldDesc = gc.refField->GetField();

    HELPER_METHOD_FRAME_BEGIN_PROTECT(gc);

    InvokeUtil::SetValidField(fieldType.GetVerifierCorElementType(), fieldType, pFieldDesc, &gc.target, &gc.value, declaringType, pDomainInitialized);

    HELPER_METHOD_FRAME_END();
}
FCIMPLEND

extern "C" void QCALLTYPE RuntimeTypeHandle_CreateInstanceForAnotherGenericParameter(
    QCall::TypeHandle pTypeHandle,
    TypeHandle* pInstArray,
    INT32 cInstArray,
    QCall::ObjectHandleOnStack pInstantiatedObject
)
{
    CONTRACTL{
        QCALL_CHECK;
        PRECONDITION(!pTypeHandle.AsTypeHandle().IsNull());
        PRECONDITION(cInstArray >= 0);
        PRECONDITION(cInstArray == 0 || pInstArray != NULL);
    }
    CONTRACTL_END;

    TypeHandle genericType = pTypeHandle.AsTypeHandle();

    BEGIN_QCALL;

    _ASSERTE (genericType.HasInstantiation());

    TypeHandle instantiatedType = ((TypeHandle)genericType.GetCanonicalMethodTable()).Instantiate(Instantiation(pInstArray, (DWORD)cInstArray));

    // Get the type information associated with refThis
    MethodTable* pVMT = instantiatedType.GetMethodTable();
    _ASSERTE (pVMT != 0 &&  !instantiatedType.IsTypeDesc());
    _ASSERTE( !pVMT->IsAbstract() ||! instantiatedType.ContainsGenericVariables());
    _ASSERTE(!pVMT->IsByRefLike() && pVMT->HasDefaultConstructor());

    // We've got the class, lets allocate it and call the constructor

    // Nullables don't take this path, if they do we need special logic to make an instance
    _ASSERTE(!Nullable::IsNullableType(instantiatedType));

    {
        GCX_COOP();

        OBJECTREF newObj = instantiatedType.GetMethodTable()->Allocate();
        GCPROTECT_BEGIN(newObj);
        CallDefaultConstructor(newObj);
        GCPROTECT_END();

        pInstantiatedObject.Set(newObj);
    }

    END_QCALL;
}

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

    switch (ObjIsInstanceOfCached(objectUNSAFE, refType->GetType())) {
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

static OBJECTREF InvokeArrayConstructor(TypeHandle th, PVOID* args, int argCnt)
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;

    // Validate the argCnt an the Rank. Also allow nested SZARRAY's.
    _ASSERTE(argCnt == (int) th.GetRank() || argCnt == (int) th.GetRank() * 2 ||
             th.GetInternalCorElementType() == ELEMENT_TYPE_SZARRAY);

    // Validate all of the parameters.  These all typed as integers
    int allocSize = 0;
    if (!ClrSafeInt<int>::multiply(sizeof(INT32), argCnt, allocSize))
        COMPlusThrow(kArgumentException, IDS_EE_SIGTOOCOMPLEX);

    INT32* indexes = (INT32*) _alloca((size_t)allocSize);
    ZeroMemory(indexes, allocSize);
    MethodTable* pMT = CoreLibBinder::GetElementType(ELEMENT_TYPE_I4);

    for (DWORD i=0; i<(DWORD)argCnt; i++)
    {
        _ASSERTE(args[i] != NULL);

        INT32 size = *(INT32*)args[i];
        ARG_SLOT value = size;
        memcpyNoGCRefs(indexes + i, ArgSlotEndiannessFixup(&value, sizeof(INT32)), sizeof(INT32));
    }

    return AllocateArrayEx(th, indexes, argCnt);
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

    // We need to activate the instance at least once
    pMD->EnsureActive();
    return FALSE;
}

class ArgIteratorBaseForMethodInvoke
{
protected:
    SIGNATURENATIVEREF * m_ppNativeSig;
    bool m_fHasThis;

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

    FORCEINLINE BOOL IsRegPassedStruct(MethodTable* pMT)
    {
        return pMT->IsRegPassedStruct();
    }

public:
    BOOL HasThis()
    {
        LIMITED_METHOD_CONTRACT;
        return m_fHasThis;
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
    ArgIteratorForMethodInvoke(SIGNATURENATIVEREF * ppNativeSig, BOOL fCtorOfVariableSizedObject)
    {
        m_ppNativeSig = ppNativeSig;

        m_fHasThis = (*m_ppNativeSig)->HasThis() && !fCtorOfVariableSizedObject;

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

FCIMPL4(Object*, RuntimeMethodHandle::InvokeMethod,
    Object *target,
    PVOID* args, // An array of byrefs
    SignatureNative* pSigUNSAFE,
    CLR_BOOL fConstructor)
{
    FCALL_CONTRACT;

    struct {
        OBJECTREF target;
        SIGNATURENATIVEREF pSig;
        OBJECTREF retVal;
    } gc;

    gc.target = ObjectToOBJECTREF(target);
    gc.pSig = (SIGNATURENATIVEREF)pSigUNSAFE;
    gc.retVal = NULL;

    MethodDesc* pMeth = gc.pSig->GetMethod();
    TypeHandle ownerType = gc.pSig->GetDeclaringType();

    HELPER_METHOD_FRAME_BEGIN_RET_PROTECT(gc);

    if (ownerType.IsSharedByGenericInstantiations())
    {
        COMPlusThrow(kNotSupportedException, W("NotSupported_Type"));
    }

#ifdef _DEBUG
    if (g_pConfig->ShouldInvokeHalt(pMeth))
    {
        _ASSERTE(!"InvokeHalt");
    }
#endif

    BOOL fCtorOfVariableSizedObject = FALSE;

    if (fConstructor)
    {
        // If we are invoking a constructor on an array then we must
        // handle this specially.
        if (ownerType.IsArray()) {
            gc.retVal = InvokeArrayConstructor(ownerType,
                                               args,
                                               gc.pSig->NumFixedArgs());
            goto Done;
        }

        // Variable sized objects, like String instances, allocate themselves
        // so they are a special case.
        MethodTable * pMT = ownerType.AsMethodTable();
        fCtorOfVariableSizedObject = pMT->HasComponentSize();
        if (!fCtorOfVariableSizedObject)
            gc.retVal = pMT->Allocate();
    }

    {
    ArgIteratorForMethodInvoke argit(&gc.pSig, fCtorOfVariableSizedObject);

    if (argit.IsActivationNeeded())
        pMeth->EnsureActive();
    CONSISTENCY_CHECK(pMeth->CheckActivated());

    UINT nStackBytes = argit.SizeOfFrameArgumentArray();

    // Note that SizeOfFrameArgumentArray does overflow checks with sufficient margin to prevent overflows here
    SIZE_T nAllocaSize = TransitionBlock::GetNegSpaceSize() + sizeof(TransitionBlock) + nStackBytes;

    Thread * pThread = GET_THREAD();

    LPBYTE pAlloc = (LPBYTE)_alloca(nAllocaSize);

    LPBYTE pTransitionBlock = pAlloc + TransitionBlock::GetNegSpaceSize();

    CallDescrData callDescrData;

    callDescrData.pSrc = pTransitionBlock + sizeof(TransitionBlock);
    _ASSERTE((nStackBytes % TARGET_POINTER_SIZE) == 0);
    callDescrData.numStackSlots = nStackBytes / TARGET_POINTER_SIZE;
#ifdef CALLDESCR_ARGREGS
    callDescrData.pArgumentRegisters = (ArgumentRegisters*)(pTransitionBlock + TransitionBlock::GetOffsetOfArgumentRegisters());
#endif
#ifdef CALLDESCR_RETBUFFARGREG
    callDescrData.pRetBuffArg = (UINT64*)(pTransitionBlock + TransitionBlock::GetOffsetOfRetBuffArgReg());
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

    // if we have the magic Value Class return, we need to allocate that class
    // and place a pointer to it on the stack.

    BOOL hasRefReturnAndNeedsBoxing = FALSE; // Indicates that the method has a BYREF return type and the target type needs to be copied into a preallocated boxed object.

    TypeHandle retTH = gc.pSig->GetReturnTypeHandle();

    TypeHandle refReturnTargetTH;  // Valid only if retType == ELEMENT_TYPE_BYREF. Caches the TypeHandle of the byref target.
    BOOL fHasRetBuffArg = argit.HasRetBuffArg();
    CorElementType retType = retTH.GetSignatureCorElementType();
    BOOL hasValueTypeReturn = retTH.IsValueType() && retType != ELEMENT_TYPE_VOID;
    _ASSERTE(hasValueTypeReturn || !fHasRetBuffArg); // only valuetypes are returned via a return buffer.
    if (hasValueTypeReturn) {
        gc.retVal = retTH.GetMethodTable()->Allocate();
    }
    else if (retType == ELEMENT_TYPE_BYREF)
    {
        refReturnTargetTH = retTH.AsTypeDesc()->GetTypeParam();

        // If the target of the byref is a value type, we need to preallocate a boxed object to hold the managed return value.
        if (refReturnTargetTH.IsValueType())
        {
            _ASSERTE(refReturnTargetTH.GetSignatureCorElementType() != ELEMENT_TYPE_VOID); // Managed Reflection layer has a bouncer for "ref void" returns.
            hasRefReturnAndNeedsBoxing = TRUE;
            gc.retVal = refReturnTargetTH.GetMethodTable()->Allocate();
        }
    }

    // Copy "this" pointer
    if (!pMeth->IsStatic() && !fCtorOfVariableSizedObject) {
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
        else if (!pMeth->GetMethodTable()->IsValueType())
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

    {
    BEGINFORBIDGC();
#ifdef _DEBUG
    GCForbidLoaderUseHolder forbidLoaderUse;
#endif

    // Take care of any return arguments
    if (fHasRetBuffArg)
    {
        PVOID pRetBuff = gc.retVal->GetData();
        *((LPVOID*) (pTransitionBlock + argit.GetRetBuffArgOffset())) = pRetBuff;
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
        ArgDestination argDest(pTransitionBlock, ofs, argit.GetArgLocDescForStructInRegs());

#ifdef ENREGISTERED_PARAMTYPE_MAXSIZE
        if (argit.IsArgPassedByRef())
        {
            MethodTable* pMT = th.GetMethodTable();
            _ASSERTE(pMT && pMT->IsValueType());

            PVOID pArgDst = argDest.GetDestinationAddress();

            PVOID pStackCopy = _alloca(structSize);
            *(PVOID *)pArgDst = pStackCopy;

            // save the info into ValueClassInfo
            if (pMT->ContainsPointers())
            {
                pValueClasses = new (_alloca(sizeof(ValueClassInfo))) ValueClassInfo(pStackCopy, pMT, pValueClasses);
            }

            // We need a new ArgDestination that points to the stack copy
            argDest = ArgDestination(pStackCopy, 0, NULL);
        }
#endif

        InvokeUtil::CopyArg(th, args[i], &argDest);
    }

    ENDFORBIDGC();
    }

    if (pValueClasses != NULL)
    {
        pProtectValueClassFrame = new (_alloca (sizeof (FrameWithCookie<ProtectValueClassFrame>)))
            FrameWithCookie<ProtectValueClassFrame>(pThread, pValueClasses);
    }

    // Call the method
    CallDescrWorkerWithHandler(&callDescrData);

    // It is still illegal to do a GC here.  The return type might have/contain GC pointers.
    if (fConstructor)
    {
        // We have a special case for Strings...The object is returned...
        if (fCtorOfVariableSizedObject) {
            PVOID pReturnValue = &callDescrData.returnValue;
            gc.retVal = *(OBJECTREF *)pReturnValue;
        }

        // If it is a Nullable<T>, box it using Nullable<T> conventions.
        // TODO: this double allocates on constructions which is wasteful
        gc.retVal = Nullable::NormalizeBox(gc.retVal);
    }
    else
    if (hasValueTypeReturn || hasRefReturnAndNeedsBoxing)
    {
        _ASSERTE(gc.retVal != NULL);

        if (hasRefReturnAndNeedsBoxing)
        {
            // Method has BYREF return and the target type is one that needs boxing. We need to copy into the boxed object we have allocated for this purpose.
            LPVOID pReturnedReference = *(LPVOID*)&callDescrData.returnValue;
            if (pReturnedReference == NULL)
            {
                COMPlusThrow(kNullReferenceException, W("NullReference_InvokeNullRefReturned"));
            }
            CopyValueClass(gc.retVal->GetData(), pReturnedReference, gc.retVal->GetMethodTable());
        }
        // if the structure is returned by value, then we need to copy in the boxed object
        // we have allocated for this purpose.
        else if (!fHasRetBuffArg)
        {
            CopyValueClass(gc.retVal->GetData(), &callDescrData.returnValue, gc.retVal->GetMethodTable());
        }
        // From here on out, it is OK to have GCs since the return object (which may have had
        // GC pointers has been put into a GC object and thus protected.

        // TODO this creates two objects which is inefficient
        // If the return type is a Nullable<T> box it into the correct form
        gc.retVal = Nullable::NormalizeBox(gc.retVal);
    }
    else if (retType == ELEMENT_TYPE_BYREF)
    {
        // WARNING: pReturnedReference is an unprotected inner reference so we must not trigger a GC until the referenced value has been safely captured.
        LPVOID pReturnedReference = *(LPVOID*)&callDescrData.returnValue;
        if (pReturnedReference == NULL)
        {
            COMPlusThrow(kNullReferenceException, W("NullReference_InvokeNullRefReturned"));
        }

        gc.retVal = InvokeUtil::CreateObjectAfterInvoke(refReturnTargetTH, pReturnedReference);
    }
    else
    {
        gc.retVal = InvokeUtil::CreateObjectAfterInvoke(retTH, &callDescrData.returnValue);
    }

    if (pProtectValueClassFrame != NULL)
        pProtectValueClassFrame->Pop(pThread);

    }

Done:
    ;
    HELPER_METHOD_FRAME_END();

    return OBJECTREFToObject(gc.retVal);
}
FCIMPLEND

/// <summary>
/// Convert a boxed value of {T} (which is either {T} or null) to a true boxed Nullable{T}.
/// </summary>
FCIMPL2(Object*, RuntimeMethodHandle::ReboxToNullable, Object* pBoxedValUNSAFE, ReflectClassBaseObject *pDestUNSAFE)
{
    FCALL_CONTRACT;

    struct {
        OBJECTREF pBoxed;
        REFLECTCLASSBASEREF destType;
        OBJECTREF retVal;
    } gc;

    gc.pBoxed = ObjectToOBJECTREF(pBoxedValUNSAFE);
    gc.destType = (REFLECTCLASSBASEREF)ObjectToOBJECTREF(pDestUNSAFE);
    gc.retVal = NULL;

    HELPER_METHOD_FRAME_BEGIN_RET_PROTECT(gc);

    MethodTable* destMT = gc.destType->GetType().AsMethodTable();

    gc.retVal = destMT->Allocate();
    void* buffer = gc.retVal->GetData();
    BOOL result = Nullable::UnBox(buffer, gc.pBoxed, destMT);
    _ASSERTE(result == TRUE);

    HELPER_METHOD_FRAME_END();

    return OBJECTREFToObject(gc.retVal);
}
FCIMPLEND

/// <summary>
/// For a true boxed Nullable{T}, re-box to a boxed {T} or null, otherwise just return the input.
/// </summary>
FCIMPL1(Object*, RuntimeMethodHandle::ReboxFromNullable, Object* pBoxedValUNSAFE)
{
    FCALL_CONTRACT;

    struct {
        OBJECTREF pBoxed;
        OBJECTREF retVal;
    } gc;

    if (pBoxedValUNSAFE == NULL)
        return NULL;

    gc.pBoxed = ObjectToOBJECTREF(pBoxedValUNSAFE);
    MethodTable* retMT = gc.pBoxed->GetMethodTable();
    if (!Nullable::IsNullableType(retMT))
        return pBoxedValUNSAFE;

    gc.retVal = NULL;

    HELPER_METHOD_FRAME_BEGIN_RET_PROTECT(gc);
    gc.retVal = Nullable::Box(gc.pBoxed->GetData(), retMT);
    HELPER_METHOD_FRAME_END();

    return OBJECTREFToObject(gc.retVal);
}
FCIMPLEND

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

    OBJECTREF refRet  = NULL;
    CorElementType fieldElType;

    HELPER_METHOD_FRAME_BEGIN_RET_PROTECT(gc);

    // Find the Object and its type
    TypeHandle targetType = pTarget->type;
    _ASSERTE(gc.refDeclaringType == NULL || !gc.refDeclaringType->GetType().IsTypeDesc());
    MethodTable *pEnclosingMT = (gc.refDeclaringType != NULL ? gc.refDeclaringType->GetType() : TypeHandle()).AsMethodTable();

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
        p = ((BYTE*) pTarget->data) + pField->GetOffset();
        refRet = InvokeUtil::CreatePointer(fieldType, *(void **)p);
        break;

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

    BYTE           *pDst = NULL;
    ARG_SLOT        value = NULL;
    CorElementType  fieldElType;

    HELPER_METHOD_FRAME_BEGIN_PROTECT(gc);

    // Find the Object and its type
    TypeHandle targetType = pTarget->type;
    MethodTable *pEnclosingMT = contextType.GetMethodTable();

    // Verify that the value passed can be widened into the target
    InvokeUtil::ValidField(fieldType, &gc.oValue);

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
            if (CoreLibBinder::IsClass(gc.oValue->GetMethodTable(), CLASS__POINTER)) {
                value = (SIZE_T) InvokeUtil::GetPointerValue(gc.oValue);
                VolatileStore((SIZE_T*) pDst, (SIZE_T) value);
                break;
            }
        }
    FALLTHROUGH;
    case ELEMENT_TYPE_FNPTR:
    {
        value = 0;
        if (gc.oValue != 0) {
            CorElementType objType = gc.oValue->GetTypeHandle().GetInternalCorElementType();
            InvokeUtil::CreatePrimitiveValue(objType, objType, gc.oValue, &value);
        }
        VolatileStore((SIZE_T*) pDst, (SIZE_T) value);
    }
    break;

    case ELEMENT_TYPE_SZARRAY:          // Single Dim, Zero
    case ELEMENT_TYPE_ARRAY:            // General Array
    case ELEMENT_TYPE_CLASS:
    case ELEMENT_TYPE_OBJECT:
        SetObjectReference((OBJECTREF*)pDst, gc.oValue);
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

extern "C" void QCALLTYPE ReflectionInvocation_CompileMethod(MethodDesc * pMD)
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
extern "C" void QCALLTYPE ReflectionInvocation_RunClassConstructor(QCall::TypeHandle pType)
{
    QCALL_CONTRACT;

    TypeHandle typeHnd = pType.AsTypeHandle();
    if (typeHnd.IsTypeDesc())
        return;

    MethodTable *pMT = typeHnd.AsMethodTable();
    if (pMT->IsClassInited())
        return;

    BEGIN_QCALL;
    pMT->CheckRestore();
    pMT->EnsureInstanceActive();
    pMT->CheckRunClassInitThrowing();
    END_QCALL;
}

// This method triggers the module constructor for a given module
extern "C" void QCALLTYPE ReflectionInvocation_RunModuleConstructor(QCall::ModuleHandle pModule)
{
    QCALL_CONTRACT;

    DomainAssembly *pDomainAssembly = pModule->GetDomainAssembly();
    if (pDomainAssembly != NULL && pDomainAssembly->IsActive())
        return;

    BEGIN_QCALL;
    pDomainAssembly->EnsureActive();
    END_QCALL;
}

static void PrepareMethodHelper(MethodDesc * pMD)
{
    STANDARD_VM_CONTRACT;

    pMD->EnsureActive();

    if (pMD->IsPointingToPrestub())
        pMD->DoPrestub(NULL);

    if (pMD->IsWrapperStub())
    {
        pMD = pMD->GetWrappedMethodDesc();
        if (pMD->IsPointingToPrestub())
            pMD->DoPrestub(NULL);
    }
}

// This method triggers a given method to be jitted. CoreCLR implementation of this method triggers jiting of the given method only.
// It does not walk a subset of callgraph to provide CER guarantees.
extern "C" void QCALLTYPE ReflectionInvocation_PrepareMethod(MethodDesc *pMD, TypeHandle *pInstantiation, UINT32 cInstantiation)
{
    CONTRACTL {
        QCALL_CHECK;
        PRECONDITION(pMD != NULL);
        PRECONDITION(CheckPointer(pInstantiation, NULL_OK));
    }
    CONTRACTL_END;

    BEGIN_QCALL;

    if (pMD->IsAbstract())
        COMPlusThrow(kArgumentException, W("Argument_CannotPrepareAbstract"));

    MethodTable * pExactMT = pMD->GetMethodTable();
    if (pInstantiation != NULL)
    {
        // We were handed an instantiation, check that the method expects it and the right number of types has been provided (the
        // caller supplies one array containing the class instantiation immediately followed by the method instantiation).
        if (cInstantiation != (pMD->GetNumGenericMethodArgs() + pMD->GetNumGenericClassArgs()))
            COMPlusThrow(kArgumentException, W("Argument_InvalidGenericInstantiation"));

        // Check we've got a reasonable looking instantiation.
        if (!Generics::CheckInstantiation(Instantiation(pInstantiation, cInstantiation)))
            COMPlusThrow(kArgumentException, W("Argument_InvalidGenericInstantiation"));
        for (ULONG i = 0; i < cInstantiation; i++)
            if (pInstantiation[i].ContainsGenericVariables())
                COMPlusThrow(kArgumentException, W("Argument_InvalidGenericInstantiation"));

        TypeHandle thExactType = ClassLoader::LoadGenericInstantiationThrowing(pMD->GetModule(),
                                                                               pMD->GetMethodTable()->GetCl(),
                                                                               Instantiation(pInstantiation, pMD->GetNumGenericClassArgs()));
        pExactMT = thExactType.AsMethodTable();

        pMD = MethodDesc::FindOrCreateAssociatedMethodDesc(pMD,
                                                           pExactMT,
                                                           FALSE,
                                                           Instantiation(&pInstantiation[pMD->GetNumGenericClassArgs()], pMD->GetNumGenericMethodArgs()),
                                                           FALSE);
    }

    if (pMD->ContainsGenericVariables())
        COMPlusThrow(kArgumentException, W("Argument_InvalidGenericInstantiation"));

    PrepareMethodHelper(pMD);

    END_QCALL;
}

// This method triggers target of a given method to be jitted. CoreCLR implementation of this method triggers jiting
// of the given method only. It does not walk a subset of callgraph to provide CER guarantees.
// In the case of a multi-cast delegate, we rely on the fact that each individual component
// was prepared prior to the Combine.
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

    MethodDesc *pMD = COMDelegate::GetMethodDesc(delegate);

    GCX_PREEMP();
    PrepareMethodHelper(pMD);

    HELPER_METHOD_FRAME_END();
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

FCIMPL2_IV(Object*, ReflectionInvocation::CreateEnum, ReflectClassBaseObject *pTypeUNSAFE, INT64 value) {
    FCALL_CONTRACT;

    REFLECTCLASSBASEREF refType = (REFLECTCLASSBASEREF)ObjectToOBJECTREF(pTypeUNSAFE);

    TypeHandle typeHandle = refType->GetType();
    _ASSERTE(typeHandle.IsEnum());
    OBJECTREF obj = NULL;
    HELPER_METHOD_FRAME_BEGIN_RET_1(refType);
    MethodTable *pEnumMT = typeHandle.AsMethodTable();
    obj = pEnumMT->Box(ArgSlotEndiannessFixup ((ARG_SLOT*)&value,
                                             pEnumMT->GetNumInstanceFieldBytes()));

    HELPER_METHOD_FRAME_END();
    return OBJECTREFToObject(obj);
}
FCIMPLEND

#ifdef FEATURE_COMINTEROP
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
    if (type.IsTypeDesc() || type.IsArray()) {
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

/*
 * Given a TypeHandle, validates whether it's legal to construct a real
 * instance of that type. Throws an exception if the instantiation would
 * be illegal; e.g., type is void or a pointer or an open generic. This
 * doesn't guarantee that a ctor will succeed, only that the VM is able
 * to support an instance of this type on the heap.
 * ==========
 * The 'fForGetUninitializedInstance' parameter controls the type of
 * exception that is thrown if a check fails.
 */
void RuntimeTypeHandle::ValidateTypeAbleToBeInstantiated(
    TypeHandle typeHandle,
    bool fGetUninitializedObject)
{
    STANDARD_VM_CONTRACT;

    // Don't allow void
    if (typeHandle.GetSignatureCorElementType() == ELEMENT_TYPE_VOID)
    {
        COMPlusThrow(kArgumentException, W("NotSupported_Type"));
    }

    // Don't allow arrays, pointers, byrefs, or function pointers
    if (typeHandle.IsTypeDesc() || typeHandle.IsArray())
    {
        COMPlusThrow(fGetUninitializedObject ? kArgumentException : kMissingMethodException, W("NotSupported_Type"));
    }

    MethodTable* pMT = typeHandle.AsMethodTable();
    PREFIX_ASSUME(pMT != NULL);

    // Don't allow creating instances of delegates
    if (pMT->IsDelegate())
    {
        COMPlusThrow(kArgumentException, W("NotSupported_Type"));
    }

    // Don't allow string or string-like (variable length) types.
    if (pMT->HasComponentSize())
    {
        COMPlusThrow(fGetUninitializedObject ? kArgumentException : kMissingMethodException, W("Argument_NoUninitializedStrings"));
    }

    // Don't allow abstract classes or interface types
    if (pMT->IsAbstract())
    {
        RuntimeExceptionKind exKind = fGetUninitializedObject ? kMemberAccessException : kMissingMethodException;
        if (pMT->IsInterface())
            COMPlusThrow(exKind, W("Acc_CreateInterface"));
        else
            COMPlusThrow(exKind, W("Acc_CreateAbst"));
    }

    // Don't allow generic variables (e.g., the 'T' from List<T>)
    // or open generic types (List<>).
    if (typeHandle.ContainsGenericVariables())
    {
        COMPlusThrow(kMemberAccessException, W("Acc_CreateGeneric"));
    }

    // Don't allow generics instantiated over __Canon
    if (pMT->IsSharedByGenericInstantiations())
    {
        COMPlusThrow(kNotSupportedException, W("NotSupported_Type"));
    }

    // Don't allow ref structs
    if (pMT->IsByRefLike())
    {
        COMPlusThrow(kNotSupportedException, W("NotSupported_ByRefLike"));
    }
}

/*
 * Given a RuntimeType, queries info on how to instantiate the object.
 * pRuntimeType - [required] the RuntimeType object
 * ppfnAllocator - [required, null-init] fnptr to the allocator
 *                 mgd sig: void* -> object
 * pvAllocatorFirstArg - [required, null-init] first argument to the allocator
 *                       (normally, but not always, the MethodTable*)
 * ppfnCtor - [required, null-init] the instance's parameterless ctor,
 *            mgd sig object -> void, or null if no ctor is needed for this type
 * pfCtorIsPublic - [required, null-init] whether the parameterless ctor is public
 * ==========
 * This method will not run the type's static cctor.
 * This method will not allocate an instance of the target type.
 */
extern "C" void QCALLTYPE RuntimeTypeHandle_GetActivationInfo(
    QCall::ObjectHandleOnStack pRuntimeType,
    PCODE* ppfnAllocator,
    void** pvAllocatorFirstArg,
    PCODE* ppfnCtor,
    BOOL* pfCtorIsPublic
)
{
    CONTRACTL{
        QCALL_CHECK;
        PRECONDITION(CheckPointer(ppfnAllocator));
        PRECONDITION(CheckPointer(pvAllocatorFirstArg));
        PRECONDITION(CheckPointer(ppfnCtor));
        PRECONDITION(CheckPointer(pfCtorIsPublic));
        PRECONDITION(*ppfnAllocator == NULL);
        PRECONDITION(*pvAllocatorFirstArg == NULL);
        PRECONDITION(*ppfnCtor == NULL);
        PRECONDITION(*pfCtorIsPublic == FALSE);
    }
    CONTRACTL_END;

    TypeHandle typeHandle = NULL;

    BEGIN_QCALL;

    {
        GCX_COOP();

        // We need to take the RuntimeType itself rather than the RuntimeTypeHandle,
        // as the COM CLSID is stored in the RuntimeType object's sync block, and we
        // might need to pull it out later in this method.
        typeHandle = ((REFLECTCLASSBASEREF)pRuntimeType.Get())->GetType();
    }

    RuntimeTypeHandle::ValidateTypeAbleToBeInstantiated(typeHandle, false /* fGetUninitializedObject */);

    MethodTable* pMT = typeHandle.AsMethodTable();
    PREFIX_ASSUME(pMT != NULL);

#ifdef FEATURE_COMINTEROP
    // COM allocation can involve the __ComObject base type (with attached CLSID) or a
    // VM-implemented [ComImport] class. For CreateInstance, the flowchart is:
    //   - For __ComObject,
    //     .. on Windows, bypass normal newobj logic and use ComClassFactory::CreateInstance.
    //     .. on non-Windows, treat as a normal class, type has no special handling in VM.
    //   - For [ComImport] class, treat as a normal class. VM will replace default
    //     ctor with COM activation logic on supported platforms, else ctor itself will PNSE.
    // IsComObjectClass is the correct way to check for __ComObject specifically
    if (IsComObjectClass(typeHandle))
    {
        void* pClassFactory = NULL;

#ifdef FEATURE_COMINTEROP_UNMANAGED_ACTIVATION
        {
            // Need to enter cooperative mode to manipulate OBJECTREFs
            GCX_COOP();
            SyncBlock* pSyncBlock = pRuntimeType.Get()->GetSyncBlock();
            pClassFactory = (void*)pSyncBlock->GetInteropInfo()->GetComClassFactory();
        }
#endif // FEATURE_COMINTEROP_UNMANAGED_ACTIVATION

        if (pClassFactory == NULL)
        {
            // no factory *or* unmanaged activation is not enabled in this runtime
            COMPlusThrow(kInvalidComObjectException, IDS_EE_NO_BACKING_CLASS_FACTORY);
        }

        // managed sig: ComClassFactory* -> object (via FCALL)
        *ppfnAllocator = CoreLibBinder::GetMethod(METHOD__RT_TYPE_HANDLE__ALLOCATECOMOBJECT)->GetMultiCallableAddrOfCode();
        *pvAllocatorFirstArg = pClassFactory;
        *ppfnCtor = NULL; // no ctor call needed; activation handled entirely by the allocator
        *pfCtorIsPublic = TRUE; // no ctor call needed => assume 'public' equivalent
    }
    else
#endif // FEATURE_COMINTEROP
    if (pMT->IsNullable())
    {
        // CreateInstance returns null given Nullable<T>
        *ppfnAllocator = NULL;
        *pvAllocatorFirstArg = NULL;
        *ppfnCtor = NULL;
        *pfCtorIsPublic = TRUE; // no ctor call needed => assume 'public' equivalent
    }
    else
    {
        // managed sig: MethodTable* -> object (via JIT helper)
        bool fHasSideEffectsUnused;
        *ppfnAllocator = CEEJitInfo::getHelperFtnStatic(CEEInfo::getNewHelperStatic(pMT, &fHasSideEffectsUnused));
        *pvAllocatorFirstArg = pMT;

        if (pMT->HasDefaultConstructor())
        {
            // managed sig: object -> void
            // for ctors on value types, lookup boxed entry point stub
            MethodDesc* pMD = pMT->GetDefaultConstructor(pMT->IsValueType() /* forceBoxedEntryPoint */);
            _ASSERTE(pMD != NULL);

            PCODE pCode = pMD->GetMultiCallableAddrOfCode();
            _ASSERTE(pCode != NULL);

            *ppfnCtor = pCode;
            *pfCtorIsPublic = pMD->IsPublic();
        }
        else if (pMT->IsValueType())
        {
            *ppfnCtor = NULL; // no ctor call needed; we're creating a boxed default(T)
            *pfCtorIsPublic = TRUE; // no ctor call needed => assume 'public' equivalent
        }
        else
        {
            // reference type with no parameterless ctor - we can't instantiate this
            COMPlusThrow(kMissingMethodException, W("Arg_NoDefCTorWithoutTypeName"));
        }
    }

    pMT->EnsureInstanceActive();

    END_QCALL;
}

/*
 * Given a ComClassFactory*, calls the COM allocator
 * and returns a RCW.
 */
FCIMPL1(Object*, RuntimeTypeHandle::AllocateComObject,
    void* pClassFactory)
{
    CONTRACTL{
        FCALL_CHECK;
        PRECONDITION(CheckPointer(pClassFactory));
    }
    CONTRACTL_END;

    OBJECTREF rv = NULL;
    bool allocated = false;

    HELPER_METHOD_FRAME_BEGIN_RET_1(rv);

#ifdef FEATURE_COMINTEROP
#ifdef FEATURE_COMINTEROP_UNMANAGED_ACTIVATION
    {
        if (pClassFactory != NULL)
        {
            rv = ((ComClassFactory*)pClassFactory)->CreateInstance(NULL);
            allocated = true;
        }
    }
#endif // FEATURE_COMINTEROP_UNMANAGED_ACTIVATION
#endif // FEATURE_COMINTEROP

    if (!allocated)
    {
#ifdef FEATURE_COMINTEROP
        COMPlusThrow(kInvalidComObjectException, IDS_EE_NO_BACKING_CLASS_FACTORY);
#else // FEATURE_COMINTEROP
        COMPlusThrow(kPlatformNotSupportedException, IDS_EE_NO_BACKING_CLASS_FACTORY);
#endif // FEATURE_COMINTEROP
    }

    HELPER_METHOD_FRAME_END();
    return OBJECTREFToObject(rv);
}
FCIMPLEND

//*************************************************************************************************
//*************************************************************************************************
//*************************************************************************************************
//      ReflectionSerialization
//*************************************************************************************************
//*************************************************************************************************
//*************************************************************************************************
extern "C" void QCALLTYPE ReflectionSerialization_GetUninitializedObject(QCall::TypeHandle pType, QCall::ObjectHandleOnStack retObject)
{
    QCALL_CONTRACT;

    BEGIN_QCALL;

    TypeHandle type = pType.AsTypeHandle();

    RuntimeTypeHandle::ValidateTypeAbleToBeInstantiated(type, true /* fForGetUninitializedInstance */);

    MethodTable* pMT = type.AsMethodTable();

#ifdef FEATURE_COMINTEROP
    // Also do not allow allocation of uninitialized RCWs (COM objects).
    if (pMT->IsComObjectType())
        COMPlusThrow(kNotSupportedException, W("NotSupported_ManagedActivation"));
#endif // FEATURE_COMINTEROP

    // If it is a nullable, return the underlying type instead.
    if (pMT->IsNullable())
        pMT = pMT->GetInstantiation()[0].GetMethodTable();

    {
        GCX_COOP();
        // Allocation will invoke any precise static cctors as needed.
        retObject.Set(pMT->Allocate());
    }

    END_QCALL;
}

//*************************************************************************************************
//*************************************************************************************************
//*************************************************************************************************
//      ReflectionEnum
//*************************************************************************************************
//*************************************************************************************************
//*************************************************************************************************

FCIMPL1(INT32, ReflectionEnum::InternalGetCorElementType, MethodTable* pMT) {
    FCALL_CONTRACT;

    _ASSERTE(pMT->IsEnum());

    // MethodTable::GetInternalCorElementType has unnecessary overhead for enums
    // Call EEClass::GetInternalCorElementType directly to avoid it
    return pMT->GetClass_NoLogging()->GetInternalCorElementType();
}
FCIMPLEND

//*******************************************************************************
struct TempEnumValue
{
    LPCUTF8 name;
    UINT64 value;
};

extern "C" void QCALLTYPE Enum_GetValuesAndNames(QCall::TypeHandle pEnumType, QCall::ObjectHandleOnStack pReturnValues, QCall::ObjectHandleOnStack pReturnNames, BOOL fGetNames)
{
    QCALL_CONTRACT;

    BEGIN_QCALL;

    TypeHandle th = pEnumType.AsTypeHandle();
    _ASSERTE(th.IsEnum());

    MethodTable *pMT = th.AsMethodTable();

    IMDInternalImport *pImport = pMT->GetMDImport();

    StackSArray<TempEnumValue> temps;

    HENUMInternalHolder fieldEnum(pImport);
    fieldEnum.EnumInit(mdtFieldDef, pMT->GetCl());

    CorElementType type = pMT->GetClass()->GetInternalCorElementType();

    // For underlying types that are signed integers, replace them with their
    // unsigned counterparts, as expected by the managed Enum implementation.
    // See the comment in Enum.cs for an explanation.
    switch (type)
    {
        case ELEMENT_TYPE_I1: type = ELEMENT_TYPE_U1; break;
        case ELEMENT_TYPE_I2: type = ELEMENT_TYPE_U2; break;
        case ELEMENT_TYPE_I4: type = ELEMENT_TYPE_U4; break;
        case ELEMENT_TYPE_I8: type = ELEMENT_TYPE_U8; break;
        case ELEMENT_TYPE_I:  type = ELEMENT_TYPE_U;  break;
        default: break;
    }

    mdFieldDef field;
    while (pImport->EnumNext(&fieldEnum, &field))
    {
        DWORD dwFlags;
        IfFailThrow(pImport->GetFieldDefProps(field, &dwFlags));
        if (!IsFdStatic(dwFlags))
            continue;

        TempEnumValue temp;

        if (fGetNames)
            IfFailThrow(pImport->GetNameOfFieldDef(field, &temp.name));

        MDDefaultValue defaultValue = { };
        IfFailThrow(pImport->GetDefaultValue(field, &defaultValue));

        // The following code assumes that the address of all union members is the same.
        static_assert_no_msg(offsetof(MDDefaultValue, m_byteValue) == offsetof(MDDefaultValue, m_usValue));
        static_assert_no_msg(offsetof(MDDefaultValue, m_ulValue) == offsetof(MDDefaultValue, m_ullValue));
        temp.value = defaultValue.m_ullValue;

        temps.Append(temp);
    }

    TempEnumValue * pTemps = &(temps[0]);
    DWORD cFields = temps.GetCount();

    {
        GCX_COOP();

        struct gc {
            BASEARRAYREF values;
            PTRARRAYREF names;
        } gc;
        gc.values = NULL;
        gc.names = NULL;

        GCPROTECT_BEGIN(gc);

        {
            gc.values = (BASEARRAYREF) AllocatePrimitiveArray(type, cFields);

            BYTE* pToValues = gc.values->GetDataPtr();
            size_t elementSize = gc.values->GetComponentSize();

            for (DWORD i = 0; i < cFields; i++, pToValues += elementSize) {
                memcpyNoGCRefs(pToValues, &pTemps[i].value, elementSize);
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

    ret = pMT->Box(ArgSlotEndiannessFixup((ARG_SLOT*)&value, pMT->GetNumInstanceFieldBytes()));

    HELPER_METHOD_FRAME_END();
    return OBJECTREFToObject(ret);
}
FCIMPLEND
