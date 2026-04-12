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

#include "interpexec.h"

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

        MethodDesc *pMD = pVMT->GetDefaultConstructor();
        UnmanagedCallersOnlyCaller defaultCtorInvoker{METHOD__RUNTIME_HELPERS__CALL_DEFAULT_CONSTRUCTOR};
        defaultCtorInvoker.InvokeThrowing(&newObj, pMD->GetSingleCallableAddrOfCode());

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

struct SkipStruct {
    SkipStruct(StackCrawlMark* mark, PTR_Thread thread) :
        pStackMark(mark)
#ifdef FEATURE_INTERPRETER
        // Since the interpreter has its own stack, we need to get a pointer which can be compared on the real
        // stack so that IsInCalleesFrames can work correctly.
        , stackMarkOnOSStack(ConvertStackMarkToPointerOnOSStack(thread, mark))
#endif
    {
    }
    StackCrawlMark* const pStackMark;
#ifdef FEATURE_INTERPRETER
    PTR_VOID const stackMarkOnOSStack;
#endif
    PTR_VOID GetStackMarkPointerToCheckAgainstStack()
    {
#ifdef FEATURE_INTERPRETER
        return stackMarkOnOSStack;
#else
        return (PTR_VOID)pStackMark;
#endif
    }
    MethodDesc*     pMeth = NULL;
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
    if (!frame->IsInCalleesFrames(pSkip->GetStackMarkPointerToCheckAgainstStack()))
        return SWA_CONTINUE;

    pSkip->pMeth = pFunc;
    return SWA_ABORT;
}

// Return the MethodInfo that represents the current method (two above this one)
extern "C" MethodDesc* QCALLTYPE MethodBase_GetCurrentMethod(QCall::StackCrawlMarkHandle stackMark) {

    QCALL_CONTRACT;

    MethodDesc* pRet = nullptr;

    BEGIN_QCALL;

    PTR_Thread pThread = GetThread();
    SkipStruct skip(stackMark, pThread);
    pThread->StackWalkFrames(SkipMethods, &skip, FUNCTIONSONLY | LIGHTUNWIND);

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
        *address = pField->GetStaticAddressHandle(NULL);
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

    if (!pMD->ShouldCallPrestub())
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

    if (pMD->IsWrapperStub())
    {
        if (pMD->ShouldCallPrestub())
            pMD->DoPrestub(NULL);
        pMD = pMD->GetWrappedMethodDesc();
    }

    if (pMD->IsAsyncThunkMethod())
    {
        if (pMD->ShouldCallPrestub())
            pMD->DoPrestub(NULL);
        pMD = pMD->GetAsyncVariant();
    }

    if (pMD->ShouldCallPrestub())
        pMD->DoPrestub(NULL);
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

#ifdef FEATURE_INTERPRETER
    InterpThreadContext* pInterpThreadContext = pThread->GetInterpThreadContext();
    if (pInterpThreadContext != nullptr)
    {
        // The interpreter has its own stack, so we need to check against that too.
#ifdef HOST_64BIT
        const UINT_PTR MinExecutionStackSize = 128 * 1024;
#else // !HOST_64BIT
        const UINT_PTR MinExecutionStackSize = 64 * 1024;
#endif // HOST_64BIT
        if (pInterpThreadContext->pStackPointer >= pInterpThreadContext->pStackEnd - MinExecutionStackSize)
        {
            FC_RETURN_BOOL(FALSE);
        }
    }
#endif // FEATURE_INTERPRETER

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
 * The 'allowByRefLike' parameter controls whether the type should be validated as not ByRefLike.
 * The 'fForGetUninitializedInstance' parameter controls the type of
 * exception that is thrown if a check fails.
 */
static void ValidateTypeAbleToBeInstantiated(
    TypeHandle typeHandle,
    bool allowByRefLike,
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
    _ASSERTE(pMT != NULL);

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
    if (!allowByRefLike && pMT->IsByRefLike())
    {
        COMPlusThrow(kNotSupportedException, W("NotSupported_ByRefLike"));
    }
}

/*
 * Given a RuntimeType, queries info on how to instantiate the type.
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

    ValidateTypeAbleToBeInstantiated(typeHandle, true /* allowByRefLike */, false /* fGetUninitializedObject */);

    MethodTable* pMT = typeHandle.AsMethodTable();
    _ASSERTE(pMT != NULL);

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

    // ByRefLike types can't be boxed (allocated as an uninitialized object).
    ValidateTypeAbleToBeInstantiated(type, false /* allowRefLike */, true /* fForGetUninitializedInstance */);

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
        static_assert(offsetof(MDDefaultValue, m_byteValue) == offsetof(MDDefaultValue, m_usValue));
        static_assert(offsetof(MDDefaultValue, m_ulValue) == offsetof(MDDefaultValue, m_ullValue));
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

    // ByRefLike types can't be boxed.
    ValidateTypeAbleToBeInstantiated(type, false /* allowRefLike */, true /* fForGetUninitializedInstance */);

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
