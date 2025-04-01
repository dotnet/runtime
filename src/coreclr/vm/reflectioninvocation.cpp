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

extern "C" void QCALLTYPE RuntimeFieldHandle_GetValue(FieldDesc* fieldDesc, QCall::ObjectHandleOnStack instance, QCall::TypeHandle fieldType, QCall::TypeHandle declaringType, BOOL* pIsClassInitialized, QCall::ObjectHandleOnStack result)
{
    QCALL_CONTRACT;

    BEGIN_QCALL;

    GCX_COOP();

    OBJECTREF target = instance.Get();
    GCPROTECT_BEGIN(target);

    // There can be no GC after this until the Object is returned.
    result.Set(InvokeUtil::GetFieldValue(fieldDesc, fieldType.AsTypeHandle(), &target, declaringType.AsTypeHandle(), pIsClassInitialized));

    GCPROTECT_END();
    END_QCALL;
}

extern "C" void QCALLTYPE RuntimeFieldHandle_SetValue(FieldDesc* fieldDesc, QCall::ObjectHandleOnStack instance, QCall::ObjectHandleOnStack value, QCall::TypeHandle fieldType, QCall::TypeHandle declaringType, BOOL* pIsClassInitialized)
{
    QCALL_CONTRACT;

    BEGIN_QCALL;

    GCX_COOP();

    struct
    {
        OBJECTREF target;
        OBJECTREF value;
    } gc;
    gc.target = instance.Get();
    gc.value = value.Get();
    GCPROTECT_BEGIN(gc);

    TypeHandle fieldTypeHandle = fieldType.AsTypeHandle();
    InvokeUtil::SetValidField(fieldTypeHandle.GetVerifierCorElementType(), fieldTypeHandle, fieldDesc, &gc.target, &gc.value, declaringType.AsTypeHandle(), pIsClassInitialized);

    GCPROTECT_END();
    END_QCALL;
}

extern "C" void QCALLTYPE RuntimeTypeHandle_CreateInstanceForAnotherGenericParameter(
    QCall::TypeHandle pTypeHandle,
    TypeHandle* pInstArray,
    INT32 cInstArray,
    QCall::ObjectHandleOnStack pInstantiatedObject
)
{
    CONTRACTL
    {
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

extern "C" void QCALLTYPE RuntimeTypeHandle_InternalAlloc(MethodTable* pMT, QCall::ObjectHandleOnStack allocated)
{
    QCALL_CONTRACT;

    _ASSERTE(pMT != NULL);

    BEGIN_QCALL;

    GCX_COOP();

    allocated.Set(pMT->Allocate());

    END_QCALL;
}

extern "C" void QCALLTYPE RuntimeTypeHandle_InternalAllocNoChecks(MethodTable* pMT, QCall::ObjectHandleOnStack allocated)
{
    QCALL_CONTRACT;

    _ASSERTE(pMT != NULL);

    BEGIN_QCALL;

    GCX_COOP();

    allocated.Set(pMT->AllocateNoChecks());

    END_QCALL;
}

static OBJECTREF InvokeArrayConstructor(TypeHandle th, PVOID* args, int argCnt)
{
    CONTRACTL
    {
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
    CONTRACTL
    {
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

extern "C" void QCALLTYPE RuntimeMethodHandle_InvokeMethod(
    QCall::ObjectHandleOnStack target,
    PVOID* args, // An array of byrefs
    QCall::ObjectHandleOnStack pSig,
    BOOL fConstructor,
    QCall::ObjectHandleOnStack result)
{
    QCALL_CONTRACT;

    BEGIN_QCALL;

    GCX_COOP();

    struct
    {
        OBJECTREF target;
        SIGNATURENATIVEREF pSig;
        OBJECTREF retVal;
    } gc;
    gc.target = NULL;
    gc.pSig = NULL;
    gc.retVal = NULL;
    GCPROTECT_BEGIN(gc);
    gc.target = target.Get();
    gc.pSig = (SIGNATURENATIVEREF)pSig.Get();

    MethodDesc* pMeth = gc.pSig->GetMethod();
    TypeHandle ownerType = gc.pSig->GetDeclaringType();

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

    ProtectValueClassFrame *pProtectValueClassFrame = NULL;
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

    // Allocate a local buffer for the return buffer if necessary
    PVOID pLocalRetBuf = nullptr;

    {
    BEGINFORBIDGC();
#ifdef _DEBUG
    GCForbidLoaderUseHolder forbidLoaderUse;
#endif

    // Take care of any return arguments
    if (fHasRetBuffArg)
    {
        _ASSERT(hasValueTypeReturn);
        PTR_MethodTable pMT = retTH.GetMethodTable();
        size_t localRetBufSize = retTH.GetSize();

        // Allocate a local buffer. The invoked method will write the return value to this
        // buffer which will be copied to gc.retVal later.
        pLocalRetBuf = _alloca(localRetBufSize);
        ZeroMemory(pLocalRetBuf, localRetBufSize);
        *((LPVOID*) (pTransitionBlock + argit.GetRetBuffArgOffset())) = pLocalRetBuf;
        if (pMT->ContainsGCPointers())
        {
            pValueClasses = new (_alloca(sizeof(ValueClassInfo))) ValueClassInfo(pLocalRetBuf, pMT, pValueClasses);
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
            if (pMT->ContainsGCPointers())
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
        pProtectValueClassFrame = new (_alloca (sizeof (ProtectValueClassFrame)))
            ProtectValueClassFrame(pThread, pValueClasses);
    }

    // Call the method
    CallDescrWorkerWithHandler(&callDescrData);

    if (fHasRetBuffArg)
    {
        // Copy the return value from the return buffer to the object
        if (retTH.GetMethodTable()->ContainsGCPointers())
        {
            memmoveGCRefs(gc.retVal->GetData(), pLocalRetBuf, retTH.GetSize());
        }
        else
        {
            memcpyNoGCRefs(gc.retVal->GetData(), pLocalRetBuf, retTH.GetSize());
        }
    }

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
#if defined(TARGET_RISCV64) || defined(TARGET_LOONGARCH64)
            if (callDescrData.fpReturnSize != FpStruct::UseIntCallConv)
            {
                FpStructInRegistersInfo info = argit.GetReturnFpStructInRegistersInfo();
                bool hasPointers = gc.retVal->GetMethodTable()->ContainsGCPointers();
                CopyReturnedFpStructFromRegisters(gc.retVal->GetData(), callDescrData.returnValue, info, hasPointers);
            }
            else
#endif // defined(TARGET_RISCV64) || defined(TARGET_LOONGARCH64)
            {
                CopyValueClass(gc.retVal->GetData(), &callDescrData.returnValue, gc.retVal->GetMethodTable());
            }
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
    result.Set(gc.retVal);

    GCPROTECT_END();

    END_QCALL;
}

struct SkipStruct {
    StackCrawlMark* pStackMark;
    MethodDesc*     pMeth;
};

// This method is called by the GetMethod function and will crawl backward
//  up the stack for integer methods.
static StackWalkAction SkipMethods(CrawlFrame* frame, VOID* data) {
    CONTRACTL
    {
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

    pSkip->pMeth = pFunc;
    return SWA_ABORT;
}

// Return the MethodInfo that represents the current method (two above this one)
extern "C" MethodDesc* QCALLTYPE MethodBase_GetCurrentMethod(QCall::StackCrawlMarkHandle stackMark) {

    QCALL_CONTRACT;

    MethodDesc* pRet = nullptr;

    BEGIN_QCALL;

    SkipStruct skip;
    skip.pStackMark = stackMark;
    skip.pMeth = 0;
    GetThread()->StackWalkFrames(SkipMethods, &skip, FUNCTIONSONLY | LIGHTUNWIND);

    // If C<Foo>.m<Bar> was called, the stack walker returns C<__Canon>.m<__Canon>. We cannot
    // get know that the instantiation used Foo or Bar at that point. So the next best thing
    // is to return C<T>.m<P> and that's what LoadTypicalMethodDefinition will do for us.

    if (skip.pMeth != NULL)
        pRet = skip.pMeth->LoadTypicalMethodDefinition();

    END_QCALL;

    return pRet;
}

static OBJECTREF DirectObjectFieldGet(FieldDesc *pField, TypeHandle fieldType, TypeHandle enclosingType, TypedByRef *pTarget)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(CheckPointer(pField));
    }
    CONTRACTL_END;

    OBJECTREF refRet;
    OBJECTREF objref = NULL;
    GCPROTECT_BEGIN(objref);
    if (!pField->IsStatic())
        objref = ObjectToOBJECTREF(*((Object**)pTarget->data));

    InvokeUtil::ValidateObjectTarget(pField, enclosingType, &objref);

    BOOL isClassInitialized = FALSE;
    refRet = InvokeUtil::GetFieldValue(pField, fieldType, &objref, enclosingType, &isClassInitialized);
    GCPROTECT_END();
    return refRet;
}

extern "C" void QCALLTYPE RuntimeFieldHandle_GetValueDirect(FieldDesc* fieldDesc, TypedByRef *pTarget, QCall::TypeHandle fieldTypeHandle, QCall::TypeHandle declaringTypeHandle, QCall::ObjectHandleOnStack result)
{
    QCALL_CONTRACT;

    BEGIN_QCALL;

    GCX_COOP();

    CorElementType fieldElType;
    TypeHandle fieldType = fieldTypeHandle.AsTypeHandle();

    // Find the Object and its type
    TypeHandle targetType = pTarget->type;
    MethodTable* pEnclosingMT = declaringTypeHandle.AsTypeHandle().AsMethodTable();

    if (fieldDesc->IsStatic() || !targetType.IsValueType())
    {
        result.Set(DirectObjectFieldGet(fieldDesc, fieldType, TypeHandle(pEnclosingMT), pTarget));
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
    switch (fieldElType)
    {
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
        p = ((BYTE*) pTarget->data) + fieldDesc->GetOffset();
        result.Set(fieldType.AsMethodTable()->Box(p));
        break;

    case ELEMENT_TYPE_OBJECT:
    case ELEMENT_TYPE_CLASS:
    case ELEMENT_TYPE_SZARRAY:          // Single Dim, Zero
    case ELEMENT_TYPE_ARRAY:            // general array
        p = ((BYTE*) pTarget->data) + fieldDesc->GetOffset();
        result.Set(ObjectToOBJECTREF(*(Object**) p));
        break;

    case ELEMENT_TYPE_PTR:
        p = ((BYTE*) pTarget->data) + fieldDesc->GetOffset();
        result.Set(InvokeUtil::CreatePointer(fieldType, *(void **)p));
        break;

    default:
        _ASSERTE(!"Unknown Type");
        // this is really an impossible condition
        COMPlusThrow(kNotSupportedException);
    }

lExit: ;
    END_QCALL;
}

static void DirectObjectFieldSet(FieldDesc *pField, TypeHandle fieldType, TypeHandle enclosingType, TypedByRef *pTarget, OBJECTREF *pValue)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;

        PRECONDITION(CheckPointer(pField));
        PRECONDITION(!fieldType.IsNull());
    }
    CONTRACTL_END;

    OBJECTREF objref = NULL;
    GCPROTECT_BEGIN(objref);
    if (!pField->IsStatic())
        objref = ObjectToOBJECTREF(*((Object**)pTarget->data));

    // Validate the target/fld type relationship
    InvokeUtil::ValidateObjectTarget(pField, enclosingType, &objref);

    BOOL isClassInitialized = FALSE;
    InvokeUtil::SetValidField(pField->GetFieldType(), fieldType, pField, &objref, pValue, enclosingType, &isClassInitialized);
    GCPROTECT_END();
}

extern "C" void QCALLTYPE RuntimeFieldHandle_SetValueDirect(FieldDesc* fieldDesc, TypedByRef *pTarget, QCall::ObjectHandleOnStack newValue, QCall::TypeHandle fieldTypeHandle, QCall::TypeHandle declaringType)
{
    QCALL_CONTRACT;

    BEGIN_QCALL;

    GCX_COOP();

    // The ARG_SLOT is used for primitive values.
    ARG_SLOT value = 0;
    CorElementType fieldElType = ELEMENT_TYPE_END;
    BYTE* pDst = NULL;

    TypeHandle fieldType = fieldTypeHandle.AsTypeHandle();
    TypeHandle contextType = declaringType.AsTypeHandle();

    struct
    {
        OBJECTREF Value;
    } gc;
    gc.Value = newValue.Get();
    GCPROTECT_BEGIN(gc);

    // Find the Object and its type
    TypeHandle targetType = pTarget->type;
    MethodTable *pEnclosingMT = contextType.GetMethodTable();

    // Verify that the value passed can be widened into the target
    InvokeUtil::ValidField(fieldType, &gc.Value);

    if (fieldDesc->IsStatic() || !targetType.IsValueType())
    {
        DirectObjectFieldSet(fieldDesc, fieldType, TypeHandle(pEnclosingMT), pTarget, &gc.Value);
        goto lExit;
    }

    if (gc.Value == NULL && fieldType.IsValueType() && !Nullable::IsNullableType(fieldType))
        COMPlusThrowArgumentNull(W("value"));

    // Validate that the target type can be cast to the type that owns this field info.
    if (!targetType.CanCastTo(TypeHandle(pEnclosingMT)))
        COMPlusThrowArgumentException(W("obj"), NULL);

    // Set the field
    fieldElType = fieldType.GetInternalCorElementType();
    if (ELEMENT_TYPE_BOOLEAN <= fieldElType && fieldElType <= ELEMENT_TYPE_R8)
    {
        CorElementType objType = gc.Value->GetTypeHandle().GetInternalCorElementType();
        if (objType != fieldElType)
        {
            InvokeUtil::CreatePrimitiveValue(fieldElType, objType, gc.Value, &value);
        }
        else
        {
            value = *(ARG_SLOT*)gc.Value->UnBox();
        }
    }

    pDst = ((BYTE*) pTarget->data) + fieldDesc->GetOffset();
    switch (fieldElType)
    {
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
        INT_PTR valuePtr = (INT_PTR) InvokeUtil::GetIntPtrValue(gc.Value);
        VolatileStore((INT_PTR*) pDst, valuePtr);
    }
    break;
    case ELEMENT_TYPE_U:
    {
        UINT_PTR valuePtr = (UINT_PTR) InvokeUtil::GetIntPtrValue(gc.Value);
        VolatileStore((UINT_PTR*) pDst, valuePtr);
    }
    break;

    case ELEMENT_TYPE_PTR:      // pointers
        if (gc.Value != NULL)
        {
            value = 0;
            if (CoreLibBinder::IsClass(gc.Value->GetMethodTable(), CLASS__POINTER))
            {
                value = (SIZE_T) InvokeUtil::GetPointerValue(gc.Value);
                VolatileStore((SIZE_T*) pDst, (SIZE_T) value);
                break;
            }
        }
    FALLTHROUGH;
    case ELEMENT_TYPE_FNPTR:
    {
        value = 0;
        if (gc.Value != NULL)
        {
            CorElementType objType = gc.Value->GetTypeHandle().GetInternalCorElementType();
            InvokeUtil::CreatePrimitiveValue(objType, objType, gc.Value, &value);
        }
        VolatileStore((SIZE_T*) pDst, (SIZE_T) value);
    }
    break;

    case ELEMENT_TYPE_SZARRAY:          // Single Dim, Zero
    case ELEMENT_TYPE_ARRAY:            // General Array
    case ELEMENT_TYPE_CLASS:
    case ELEMENT_TYPE_OBJECT:
        SetObjectReference((OBJECTREF*)pDst, gc.Value);
    break;

    case ELEMENT_TYPE_VALUETYPE:
    {
        _ASSERTE(!fieldType.IsTypeDesc());
        MethodTable* pMT = fieldType.AsMethodTable();

        // If we have a null value then we must create an empty field
        if (gc.Value == NULL)
        {
            InitValueClass(pDst, pMT);
        }
        else
        {
            pMT->UnBoxIntoUnchecked(pDst, gc.Value);
        }
    }
    break;

    default:
        _ASSERTE(!"Unknown Type");
        // this is really an impossible condition
        COMPlusThrow(kNotSupportedException);
    }

lExit: ;
    GCPROTECT_END();
    END_QCALL;
}

static bool IsFastPathSupportedHelper(FieldDesc* pFieldDesc)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        PRECONDITION(CheckPointer(pFieldDesc));
    }
    CONTRACTL_END;

    return !pFieldDesc->IsThreadStatic() &&
        !pFieldDesc->IsEnCNew() &&
        !(pFieldDesc->IsCollectible() && pFieldDesc->IsStatic());
}

FCIMPL1(FC_BOOL_RET, RuntimeFieldHandle::IsFastPathSupported, ReflectFieldObject *pFieldUNSAFE)
{
    FCALL_CONTRACT;

    REFLECTFIELDREF refField = (REFLECTFIELDREF)ObjectToOBJECTREF(pFieldUNSAFE);
    _ASSERTE(refField != NULL);

    FieldDesc* pFieldDesc = refField->GetField();
    return IsFastPathSupportedHelper(pFieldDesc) ? TRUE : FALSE;
}
FCIMPLEND

FCIMPL1(INT32, RuntimeFieldHandle::GetInstanceFieldOffset, ReflectFieldObject *pFieldUNSAFE)
{
    CONTRACTL
    {
        FCALL_CHECK;
        PRECONDITION(CheckPointer(pFieldUNSAFE));
    }
    CONTRACTL_END;

    REFLECTFIELDREF refField = (REFLECTFIELDREF)ObjectToOBJECTREF(pFieldUNSAFE);
    _ASSERTE(refField != NULL);

    FieldDesc* pFieldDesc = refField->GetField();
    _ASSERTE(!pFieldDesc->IsStatic());

    // IsFastPathSupported needs to checked before calling this method.
    _ASSERTE(IsFastPathSupportedHelper(pFieldDesc));

    return pFieldDesc->GetOffset();
}
FCIMPLEND

FCIMPL1(void*, RuntimeFieldHandle::GetStaticFieldAddress, ReflectFieldObject *pFieldUNSAFE)
{
    CONTRACTL
    {
        FCALL_CHECK;
        PRECONDITION(CheckPointer(pFieldUNSAFE));
    }
    CONTRACTL_END;

    REFLECTFIELDREF refField = (REFLECTFIELDREF)ObjectToOBJECTREF(pFieldUNSAFE);
    _ASSERTE(refField != NULL);

    FieldDesc* pFieldDesc = refField->GetField();
    _ASSERTE(pFieldDesc->IsStatic());

    // IsFastPathSupported needs to checked before calling this method.
    _ASSERTE(IsFastPathSupportedHelper(pFieldDesc));

    if (pFieldDesc->IsRVA())
    {
        Module* pModule = pFieldDesc->GetModule();
        return pModule->GetRvaField(pFieldDesc->GetOffset());
    }
    else
    {
        PTR_BYTE base = pFieldDesc->GetBase();
        return PTR_VOID(base + pFieldDesc->GetOffset());
    }
}
FCIMPLEND

// Returns the address of the EnC instance field in the object (This is an interior
// pointer and the caller has to use it appropriately) or an EnC static field.
extern "C" void* QCALLTYPE RuntimeFieldHandle_GetEnCFieldAddr(QCall::ObjectHandleOnStack target, FieldDesc* pFD)
{
    CONTRACTL
    {
        QCALL_CHECK;
        PRECONDITION(pFD != NULL);
    }
    CONTRACTL_END;

    void* ret = NULL;

    BEGIN_QCALL;

    GCX_COOP();

    // Only handling EnC
    _ASSERTE(pFD->IsEnCNew());

    // If the field is static, or if the object is non-null, get the address of the field.
    if (pFD->IsStatic() || target.Get() != NULL)
        ret = pFD->GetAddress(OBJECTREFToObject(target.Get()));

    END_QCALL;

    return ret;
}

extern "C" BOOL QCALLTYPE RuntimeFieldHandle_GetRVAFieldInfo(FieldDesc* pField, void** address, UINT* size)
{
    QCALL_CONTRACT;

    BOOL ret = FALSE;

    BEGIN_QCALL;

    if (pField != NULL && pField->IsRVA())
    {
        Module* pModule = pField->GetModule();
        *address = pModule->GetRvaField(pField->GetOffset());
        *size = pField->LoadSize();

        ret = TRUE;
    }

    END_QCALL;

    return ret;
}

extern "C" void QCALLTYPE RuntimeFieldHandle_GetFieldDataReference(FieldDesc* pField, QCall::ObjectHandleOnStack instance, QCall::ByteRefOnStack fieldDataRef)
{
    CONTRACTL
    {
        QCALL_CHECK;
        PRECONDITION(pField != NULL);
    }
    CONTRACTL_END;

    BEGIN_QCALL;

    GCX_COOP();
    _ASSERTE(instance.Get() != NULL);

    fieldDataRef.Set((BYTE*)pField->GetInstanceAddress(instance.Get()));

    END_QCALL;
}

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
    // The ContainsGenericVariables check is to preserve back-compat where we assume the generic type is already initialized
    if (pMT->IsClassInited() || pMT->ContainsGenericVariables())
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

    Assembly *pAssembly = pModule->GetAssembly();
    if (pAssembly != NULL && pAssembly->IsActive())
        return;

    BEGIN_QCALL;
    pAssembly->EnsureActive();
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
    CONTRACTL
    {
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

// This method triggers target of a given method to be jitted.
// In the case of a multi-cast delegate, we rely on the fact that each individual component
// was prepared prior to the Combine.
extern "C" void QCALLTYPE ReflectionInvocation_PrepareDelegate(QCall::ObjectHandleOnStack delegate)
{
    QCALL_CONTRACT;

    BEGIN_QCALL;

    MethodDesc* pMD = NULL;
    {
        GCX_COOP();
        pMD = COMDelegate::GetMethodDesc(delegate.Get());
    }

    PrepareMethodHelper(pMD);

    END_QCALL;
}

// This method checks and returns whether there is sufficient stack to execute the
// average Framework method, but rather than throwing, it simply returns a
// Boolean: true for sufficient stack space, otherwise false.
FCIMPL0(FC_BOOL_RET, ReflectionInvocation::TryEnsureSufficientExecutionStack)
{
	FCALL_CONTRACT;

	Thread *pThread = GetThread();

    // We use the address of a local variable as our "current stack pointer", which is
    // plenty close enough for the purposes of this method.
	UINT_PTR current = reinterpret_cast<UINT_PTR>(&pThread);
	UINT_PTR limit = pThread->GetCachedStackSufficientExecutionLimit();

	FC_RETURN_BOOL(current >= limit);
}
FCIMPLEND

#ifdef FEATURE_COMINTEROP
extern "C" void QCALLTYPE ReflectionInvocation_InvokeDispMethod(
    QCall::ObjectHandleOnStack type,
    QCall::ObjectHandleOnStack name,
    INT32 invokeAttr,
    QCall::ObjectHandleOnStack target,
    QCall::ObjectHandleOnStack args,
    QCall::ObjectHandleOnStack byrefModifiers,
    LCID lcid,
    QCall::ObjectHandleOnStack namedParameters,
    QCall::ObjectHandleOnStack result)
{
    QCALL_CONTRACT;

    BEGIN_QCALL;

    GCX_COOP();
    struct
    {
        REFLECTCLASSBASEREF refThis;
        STRINGREF           name;
        OBJECTREF           target;
        PTRARRAYREF         args;
        PTRARRAYREF         byrefModifiers;
        PTRARRAYREF         namedParameters;
        OBJECTREF           RetObj;
    } gc;
    gc.refThis          = (REFLECTCLASSBASEREF) type.Get();
    gc.name             = (STRINGREF)           name.Get();
    gc.target           = (OBJECTREF)           target.Get();
    gc.args             = (PTRARRAYREF)         args.Get();
    gc.byrefModifiers   = (PTRARRAYREF)         byrefModifiers.Get();
    gc.namedParameters  = (PTRARRAYREF)         namedParameters.Get();
    gc.RetObj           = NULL;
    GCPROTECT_BEGIN(gc);

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

    result.Set(gc.RetObj);
    GCPROTECT_END();
    END_QCALL;
}

extern "C" void QCALLTYPE ReflectionInvocation_GetComObjectGuid(QCall::ObjectHandleOnStack type, GUID* result)
{
    CONTRACTL
    {
        QCALL_CHECK;
        PRECONDITION(result != NULL);
    }
    CONTRACTL_END;

    BEGIN_QCALL;

#ifdef FEATURE_COMINTEROP_UNMANAGED_ACTIVATION
    SyncBlock* pSyncBlock;
    {
        GCX_COOP();
        _ASSERTE(IsComObjectClass(TypeHandle{ type.Get()->GetMethodTable() }));
        pSyncBlock = type.Get()->GetSyncBlock();
    }
    ComClassFactory* pComClsFac = pSyncBlock->GetInteropInfo()->GetComClassFactory();
    if (pComClsFac != NULL)
    {
        memcpyNoGCRefs(result, &pComClsFac->m_rclsid, sizeof(GUID));
    }
    else
#endif // FEATURE_COMINTEROP_UNMANAGED_ACTIVATION
    {
        memset(result, 0, sizeof(GUID));
    }

    END_QCALL;
}
#endif // FEATURE_COMINTEROP

extern "C" void QCALLTYPE ReflectionInvocation_GetGuid(MethodTable* pMT, GUID* result)
{
    CONTRACTL
    {
        QCALL_CHECK;
        PRECONDITION(pMT != NULL);
        PRECONDITION(result != NULL);
    }
    CONTRACTL_END;

    BEGIN_QCALL;

    pMT->GetGuid(result, /* bGenerateIfNotFound */ TRUE);

    END_QCALL;
}

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
    PCODE* ppfnRefCtor,
    PCODE* ppfnValueCtor,
    BOOL* pfCtorIsPublic
)
{
    CONTRACTL
    {
        QCALL_CHECK;
        PRECONDITION(CheckPointer(ppfnAllocator));
        PRECONDITION(CheckPointer(pvAllocatorFirstArg));
        PRECONDITION(CheckPointer(ppfnRefCtor));
        PRECONDITION(CheckPointer(ppfnValueCtor));
        PRECONDITION(CheckPointer(pfCtorIsPublic));
        PRECONDITION(*ppfnAllocator == NULL);
        PRECONDITION(*pvAllocatorFirstArg == NULL);
        PRECONDITION(*ppfnRefCtor == NULL);
        PRECONDITION(*ppfnValueCtor == NULL);
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
        *ppfnRefCtor = NULL; // no ctor call needed; activation handled entirely by the allocator
        *ppfnValueCtor = NULL; // no value ctor for reference type
        *pfCtorIsPublic = TRUE; // no ctor call needed => assume 'public' equivalent
    }
    else
#endif // FEATURE_COMINTEROP
    if (pMT->IsNullable())
    {
        // CreateInstance returns null given Nullable<T>
        *ppfnAllocator = (PCODE)NULL;
        *pvAllocatorFirstArg = NULL;
        *ppfnRefCtor = (PCODE)NULL;
        *ppfnValueCtor = (PCODE)NULL;
        *pfCtorIsPublic = TRUE; // no ctor call needed => assume 'public' equivalent
    }
    else
    {
        // managed sig: MethodTable* -> object (via JIT helper)
        bool fHasSideEffectsUnused;
        *ppfnAllocator = CEEJitInfo::getHelperFtnStatic(CEEInfo::getNewHelperStatic(pMT, &fHasSideEffectsUnused));
        *pvAllocatorFirstArg = pMT;

        BOOL isValueType = pMT->IsValueType();
        if (pMT->HasDefaultConstructor())
        {
            // managed sig: object -> void
            // for ctors on value types, lookup boxed entry point stub
            MethodDesc* pMD = pMT->GetDefaultConstructor(isValueType /* forceBoxedEntryPoint */);
            _ASSERTE(pMD != NULL);

            PCODE pCode = pMD->GetMultiCallableAddrOfCode();
            _ASSERTE(pCode != (PCODE)NULL);

            *ppfnRefCtor = pCode;
            *pfCtorIsPublic = pMD->IsPublic();

            // If we have a value type, get the non-boxing entry point too.
            if (isValueType)
            {
                pMD = pMT->GetDefaultConstructor(FALSE /* forceBoxedEntryPoint */);
                _ASSERTE(pMD != NULL);
                pCode = pMD->GetMultiCallableAddrOfCode();
                _ASSERTE(pCode != (PCODE)NULL);
                *ppfnValueCtor = pCode;
            }
        }
        else if (isValueType)
        {
            *ppfnRefCtor = (PCODE)NULL; // no ctor call needed; we're creating a boxed default(T)
            *ppfnValueCtor = (PCODE)NULL;
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

#ifdef FEATURE_COMINTEROP
/*
 * Given a ComClassFactory*, calls the COM allocator
 * and returns a RCW.
 */
extern "C" void QCALLTYPE RuntimeTypeHandle_AllocateComObject(void* pClassFactory, QCall::ObjectHandleOnStack result)
{
    QCALL_CONTRACT;

    BEGIN_QCALL;

#ifdef FEATURE_COMINTEROP_UNMANAGED_ACTIVATION
    if (pClassFactory == NULL)
        COMPlusThrow(kInvalidComObjectException, IDS_EE_NO_BACKING_CLASS_FACTORY);

    {
        GCX_COOP();
        result.Set(((ComClassFactory*)pClassFactory)->CreateInstance(NULL));
    }
#else
    COMPlusThrow(kPlatformNotSupportedException, IDS_EE_NO_BACKING_CLASS_FACTORY);
#endif // FEATURE_COMINTEROP_UNMANAGED_ACTIVATION

    END_QCALL;
}
#endif // FEATURE_COMINTEROP

//*************************************************************************************************
//*************************************************************************************************
//*************************************************************************************************
//      ReflectionSerialization
//*************************************************************************************************
//*************************************************************************************************
//*************************************************************************************************
extern "C" void QCALLTYPE ReflectionSerialization_GetCreateUninitializedObjectInfo(
    QCall::TypeHandle pType,
    PCODE* ppfnAllocator,
    void** pvAllocatorFirstArg)
{
    CONTRACTL
    {
        QCALL_CHECK;
        PRECONDITION(CheckPointer(ppfnAllocator));
        PRECONDITION(CheckPointer(pvAllocatorFirstArg));
        PRECONDITION(*ppfnAllocator == NULL);
        PRECONDITION(*pvAllocatorFirstArg == NULL);
    }
    CONTRACTL_END;

    BEGIN_QCALL;

    TypeHandle type = pType.AsTypeHandle();

    RuntimeTypeHandle::ValidateTypeAbleToBeInstantiated(type, true /* fForGetUninitializedInstance */);

    MethodTable* pMT = type.AsMethodTable();

#ifdef FEATURE_COMINTEROP
    // Also do not allow allocation of uninitialized RCWs (COM objects).
    if (pMT->IsComObjectType())
        COMPlusThrow(kNotSupportedException, W("NotSupported_ManagedActivation"));
#endif // FEATURE_COMINTEROP

    // If it is a nullable, return the allocator for the underlying type instead.
    if (pMT->IsNullable())
        pMT = pMT->GetInstantiation()[0].GetMethodTable();

    bool fHasSideEffectsUnused;
    *ppfnAllocator = CEEJitInfo::getHelperFtnStatic(CEEInfo::getNewHelperStatic(pMT, &fHasSideEffectsUnused));
    *pvAllocatorFirstArg = pMT;

    pMT->EnsureInstanceActive();

    if (pMT->HasPreciseInitCctors())
    {
        pMT->CheckRunClassInitAsIfConstructingThrowing();
    }

    END_QCALL;
}

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
        _ASSERTE(defaultValue.m_bType != ELEMENT_TYPE_STRING); // Strings in metadata are little-endian.

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

        struct {
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

extern "C" int32_t QCALLTYPE ReflectionInvocation_SizeOf(QCall::TypeHandle pType)
{
    QCALL_CONTRACT_NO_GC_TRANSITION;

    TypeHandle handle = pType.AsTypeHandle();

    // -1 is the same sentinel value returned by GetSize for an invalid type.
    if (handle.ContainsGenericVariables())
        return -1;

    return handle.GetSize();
}

extern "C" void QCALLTYPE ReflectionInvocation_GetBoxInfo(
    QCall::TypeHandle pType,
    PCODE* ppfnAllocator,
    void** pvAllocatorFirstArg,
    int32_t* pValueOffset,
    uint32_t* pValueSize)
{
    CONTRACTL
    {
        QCALL_CHECK;
        PRECONDITION(CheckPointer(ppfnAllocator));
        PRECONDITION(CheckPointer(pvAllocatorFirstArg));
        PRECONDITION(*ppfnAllocator == NULL);
        PRECONDITION(*pvAllocatorFirstArg == NULL);
    }
    CONTRACTL_END;

    BEGIN_QCALL;

    TypeHandle type = pType.AsTypeHandle();

    RuntimeTypeHandle::ValidateTypeAbleToBeInstantiated(type, true /* fForGetUninitializedInstance */);

    MethodTable* pMT = type.AsMethodTable();

    _ASSERTE(pMT->IsValueType() || pMT->IsNullable() || pMT->IsEnum() || pMT->IsTruePrimitive());

    *pValueOffset = 0;

    // If it is a nullable, return the allocator for the underlying type instead.
    if (pMT->IsNullable())
    {
        *pValueOffset = Nullable::GetValueAddrOffset(pMT);
        pMT = pMT->GetInstantiation()[0].GetMethodTable();
    }

    bool fHasSideEffectsUnused;
    *ppfnAllocator = CEEJitInfo::getHelperFtnStatic(CEEInfo::getNewHelperStatic(pMT, &fHasSideEffectsUnused));
    *pvAllocatorFirstArg = pMT;
    *pValueSize = pMT->GetNumInstanceFieldBytes();

    pMT->EnsureInstanceActive();

    END_QCALL;
}
