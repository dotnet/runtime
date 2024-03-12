// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// File: ILMarshalers.cpp
//

//


#include "common.h"
#include "dllimport.h"
#include "mlinfo.h"
#include "ilmarshalers.h"
#include "olevariant.h"
#include "comdatetime.h"
#include "fieldmarshaler.h"

LocalDesc ILReflectionObjectMarshaler::GetManagedType()
{
    STANDARD_VM_CONTRACT;

    return LocalDesc(CoreLibBinder::GetClass(GetManagedTypeBinderID()));
}

LocalDesc ILReflectionObjectMarshaler::GetNativeType()
{
    LIMITED_METHOD_CONTRACT;

    return LocalDesc(ELEMENT_TYPE_I);
}

void ILReflectionObjectMarshaler::EmitConvertContentsCLRToNative(ILCodeStream* pslILEmit)
{
    STANDARD_VM_CONTRACT;

    int tokObject__m_handle = pslILEmit->GetToken(CoreLibBinder::GetField(GetObjectFieldID()));
    int tokStruct__m_object = 0;
    BinderFieldID structField = GetStructureFieldID();

    // This marshaler can generate code for marshaling an object containing a handle, and for
    // marshaling a struct referring to an object containing a handle.
    if (structField != 0)
    {
        tokStruct__m_object = pslILEmit->GetToken(CoreLibBinder::GetField(structField));
    }

    ILCodeLabel* pNullLabel = pslILEmit->NewCodeLabel();

    pslILEmit->EmitLoadNullPtr();
    EmitStoreNativeValue(pslILEmit);

    if (tokStruct__m_object != 0)
    {
        EmitLoadManagedHomeAddr(pslILEmit);
        pslILEmit->EmitLDFLD(tokStruct__m_object);
    }
    else
    {
        EmitLoadManagedValue(pslILEmit);
    }
    pslILEmit->EmitBRFALSE(pNullLabel);

    if (tokStruct__m_object != 0)
    {
        EmitLoadManagedHomeAddr(pslILEmit);
        pslILEmit->EmitLDFLD(tokStruct__m_object);
    }
    else
    {
        EmitLoadManagedValue(pslILEmit);
    }

    pslILEmit->EmitLDFLD(tokObject__m_handle);
    EmitStoreNativeValue(pslILEmit);

    pslILEmit->EmitLabel(pNullLabel);

    if (IsCLRToNative(m_dwMarshalFlags))
    {
        EmitKeepAliveManagedValue();
    }
}

void ILReflectionObjectMarshaler::EmitConvertContentsNativeToCLR(ILCodeStream* pslILEmit)
{
    STANDARD_VM_CONTRACT;

    COMPlusThrow(kTypeLoadException, IDS_EE_COM_UNSUPPORTED_SIG);
}

LocalDesc ILDelegateMarshaler::GetNativeType()
{
    LIMITED_METHOD_CONTRACT;

    return LocalDesc(ELEMENT_TYPE_I);
}

LocalDesc ILDelegateMarshaler::GetManagedType()
{
    LIMITED_METHOD_CONTRACT;

    return LocalDesc(m_pargs->m_pMT);
}

void ILDelegateMarshaler::EmitConvertContentsCLRToNative(ILCodeStream* pslILEmit)
{
    STANDARD_VM_CONTRACT;

    ILCodeLabel* pNullLabel = pslILEmit->NewCodeLabel();

    pslILEmit->EmitLoadNullPtr();
    EmitStoreNativeValue(pslILEmit);

    EmitLoadManagedValue(pslILEmit);
    pslILEmit->EmitBRFALSE(pNullLabel);

    EmitLoadManagedValue(pslILEmit);
    pslILEmit->EmitCALL(METHOD__MARSHAL__GET_FUNCTION_POINTER_FOR_DELEGATE, 1, 1);
    EmitStoreNativeValue(pslILEmit);

    if (IsCLRToNative(m_dwMarshalFlags))
    {
        EmitKeepAliveManagedValue();
    }

    pslILEmit->EmitLabel(pNullLabel);
}

void ILDelegateMarshaler::EmitConvertContentsNativeToCLR(ILCodeStream* pslILEmit)
{
    STANDARD_VM_CONTRACT;

    ILCodeLabel* pNullLabel = pslILEmit->NewCodeLabel();

    EmitLoadNativeValue(pslILEmit);
    pslILEmit->EmitBRFALSE(pNullLabel);

    EmitLoadNativeValue(pslILEmit);
    pslILEmit->EmitLDTOKEN(pslILEmit->GetToken(m_pargs->m_pMT));
    pslILEmit->EmitCALL(METHOD__TYPE__GET_TYPE_FROM_HANDLE, 1, 1); // Type System.Type.GetTypeFromHandle(RuntimeTypeHandle handle)

    // COMPAT: There is a subtle difference between argument and field marshaling with Delegate types.
    // During field marshaling, the plain Delegate type can be used even though that type doesn't
    // represent a concrete type. Argument marshaling doesn't permit this so we use the public
    // API which will validate that Delegate isn't directly used.
    if (IsFieldMarshal(m_dwMarshalFlags))
    {
        // Delegate System.Marshal.GetDelegateForFunctionPointerInternal(IntPtr p, Type t)
        pslILEmit->EmitCALL(METHOD__MARSHAL__GET_DELEGATE_FOR_FUNCTION_POINTER_INTERNAL, 2, 1);
        EmitStoreManagedValue(pslILEmit);

        // Field marshaling of delegates supports marshaling back null from native code.
        // COMPAT: Parameter and return value marshalling does not marshal back a null value.
        //    e.g. `extern static void SetNull(ref Action f);` <- f will not be null on return.
        ILCodeLabel* pFinishedLabel = pslILEmit->NewCodeLabel();
        pslILEmit->EmitBR(pFinishedLabel);
        pslILEmit->EmitLabel(pNullLabel);
        pslILEmit->EmitLDNULL();
        EmitStoreManagedValue(pslILEmit);
        pslILEmit->EmitLabel(pFinishedLabel);
    }
    else
    {
        // Delegate System.Marshal.GetDelegateForFunctionPointer(IntPtr p, Type t)
        pslILEmit->EmitCALL(METHOD__MARSHAL__GET_DELEGATE_FOR_FUNCTION_POINTER, 2, 1);
        EmitStoreManagedValue(pslILEmit);
        pslILEmit->EmitLabel(pNullLabel);
    }

}


LocalDesc ILBoolMarshaler::GetNativeType()
{
    LIMITED_METHOD_CONTRACT;

    return LocalDesc(GetNativeBoolElementType());
}

LocalDesc ILBoolMarshaler::GetManagedType()
{
    LIMITED_METHOD_CONTRACT;

    return LocalDesc(ELEMENT_TYPE_BOOLEAN);
}

void ILBoolMarshaler::EmitConvertContentsCLRToNative(ILCodeStream* pslILEmit)
{
    STANDARD_VM_CONTRACT;

    ILCodeLabel* pLoadFalseLabel = pslILEmit->NewCodeLabel();
    ILCodeLabel* pDoneLabel = pslILEmit->NewCodeLabel();


    int trueValue = GetNativeTrueValue();
    int falseValue = GetNativeFalseValue();

    EmitLoadManagedValue(pslILEmit);

    if (falseValue == 0 && trueValue == 1)
    {
        // this can be done without jumps
        pslILEmit->EmitLDC(0);
        pslILEmit->EmitCEQ();
        pslILEmit->EmitLDC(0);
        pslILEmit->EmitCEQ();
    }
    else
    {
        pslILEmit->EmitBRFALSE(pLoadFalseLabel);
        pslILEmit->EmitLDC(trueValue);
        pslILEmit->EmitBR(pDoneLabel);
#ifdef _DEBUG
        pslILEmit->EmitPOP();   // keep the simple stack level calculator happy
#endif // _DEBUG
        pslILEmit->EmitLabel(pLoadFalseLabel);
        pslILEmit->EmitLDC(falseValue);
        pslILEmit->EmitLabel(pDoneLabel);
    }

    EmitStoreNativeValue(pslILEmit);
}

void ILBoolMarshaler::EmitConvertContentsNativeToCLR(ILCodeStream* pslILEmit)
{
    STANDARD_VM_CONTRACT;

    int falseValue = GetNativeFalseValue();

    EmitLoadNativeValue(pslILEmit);

    pslILEmit->EmitLDC(falseValue);
    pslILEmit->EmitCEQ();
    pslILEmit->EmitLDC(0);
    pslILEmit->EmitCEQ();

    EmitStoreManagedValue(pslILEmit);
}


LocalDesc ILWSTRMarshaler::GetNativeType()
{
    LIMITED_METHOD_CONTRACT;

    //
    // pointer to value class
    //
    return LocalDesc(ELEMENT_TYPE_I);
}

LocalDesc ILWSTRMarshaler::GetManagedType()
{
    LIMITED_METHOD_CONTRACT;

    //
    // value class
    //
    return LocalDesc(ELEMENT_TYPE_STRING);
}

void ILWSTRMarshaler::EmitConvertSpaceCLRToNative(ILCodeStream* pslILEmit)
{
    LIMITED_METHOD_CONTRACT;
    UNREACHABLE_MSG("All paths to this function are covered by the EmitConvertSpaceAndContents* paths");
}

void ILWSTRMarshaler::EmitConvertContentsCLRToNative(ILCodeStream* pslILEmit)
{
    STANDARD_VM_CONTRACT;

    // This code path should only be called by an out marshalling. Other codepaths that convert a string to native
    // should all go through EmitConvertSpaceAndContentsCLRToNative
    _ASSERTE(IsOut(m_dwMarshalFlags) && !IsCLRToNative(m_dwMarshalFlags) && !IsByref(m_dwMarshalFlags));

    ILCodeLabel* pNullRefLabel = pslILEmit->NewCodeLabel();

    EmitLoadManagedValue(pslILEmit);
    pslILEmit->EmitBRFALSE(pNullRefLabel);

    EmitLoadManagedValue(pslILEmit);
    EmitLoadNativeValue(pslILEmit);

    EmitLoadManagedValue(pslILEmit);
    EmitCheckManagedStringLength(pslILEmit);

    // static void System.String.InternalCopy(String src, IntPtr dest,int len)
    pslILEmit->EmitCALL(METHOD__STRING__INTERNAL_COPY, 3, 0);
    pslILEmit->EmitLabel(pNullRefLabel);
}

void ILWSTRMarshaler::EmitConvertSpaceNativeToCLR(ILCodeStream* pslILEmit)
{
    STANDARD_VM_CONTRACT;
    // We currently don't marshal strings from the native to the CLR side in a Reverse-PInvoke unless
    // the parameter is explicitly annotated as an [In] parameter.
    pslILEmit->EmitLDNULL();
    EmitStoreManagedValue(pslILEmit);
}

void ILWSTRMarshaler::EmitConvertContentsNativeToCLR(ILCodeStream* pslILEmit)
{
    STANDARD_VM_CONTRACT;

    ILCodeLabel* pIsNullLabel = pslILEmit->NewCodeLabel();

    EmitLoadNativeValue(pslILEmit);
    pslILEmit->EmitBRFALSE(pIsNullLabel);

    EmitLoadNativeValue(pslILEmit);
    pslILEmit->EmitDUP();
    EmitCheckNativeStringLength(pslILEmit);
    pslILEmit->EmitPOP();       // pop num chars

    pslILEmit->EmitNEWOBJ(METHOD__STRING__CTOR_CHARPTR, 1);
    EmitStoreManagedValue(pslILEmit);

    pslILEmit->EmitLabel(pIsNullLabel);
}

//
// input stack:  0: managed string
// output stack: 0: (string_length+1) * sizeof(WCHAR)
//
void ILWSTRMarshaler::EmitCheckManagedStringLength(ILCodeStream* pslILEmit)
{
    STANDARD_VM_CONTRACT;

    // Note: The maximum size of managed string is under 2GB bytes. This cannot overflow.
    pslILEmit->EmitCALL(METHOD__STRING__GET_LENGTH, 1, 1);
    pslILEmit->EmitLDC(1);
    pslILEmit->EmitADD();
    pslILEmit->EmitDUP();
    pslILEmit->EmitADD();           // (length+1) * sizeof(WCHAR)
}

void ILWSTRMarshaler::EmitConvertSpaceAndContentsCLRToNative(ILCodeStream* pslILEmit)
{
    STANDARD_VM_CONTRACT;

    ILCodeLabel* pNullRefLabel = pslILEmit->NewCodeLabel();
    DWORD dwLengthLocalNum = pslILEmit->NewLocal(ELEMENT_TYPE_I4);

    pslILEmit->EmitLoadNullPtr();
    EmitStoreNativeValue(pslILEmit);

    EmitLoadManagedValue(pslILEmit);
    pslILEmit->EmitBRFALSE(pNullRefLabel);

    EmitLoadManagedValue(pslILEmit);
    EmitCheckManagedStringLength(pslILEmit);

    // cb

    pslILEmit->EmitDUP();
    pslILEmit->EmitSTLOC(dwLengthLocalNum);

    // cb

    // static IntPtr AllocCoTaskMem(int cb)
    pslILEmit->EmitCALL(METHOD__MARSHAL__ALLOC_CO_TASK_MEM, 1, 1);
    EmitStoreNativeValue(pslILEmit);

    EmitLoadManagedValue(pslILEmit);
    EmitLoadNativeValue(pslILEmit);

    // src, dst

    pslILEmit->EmitLDLOC(dwLengthLocalNum); // length

    // static void System.String.InternalCopy(String src, IntPtr dest,int len)
    pslILEmit->EmitCALL(METHOD__STRING__INTERNAL_COPY, 3, 0);
    pslILEmit->EmitLabel(pNullRefLabel);
}

void ILWSTRMarshaler::EmitMarshalViaPinning(ILCodeStream* pslILEmit)
{
    STANDARD_VM_CONTRACT;

    LocalDesc locDesc = GetManagedType();
    locDesc.MakePinned();
    DWORD dwPinnedLocal = pslILEmit->NewLocal(locDesc);
    int fieldDef = pslILEmit->GetToken(CoreLibBinder::GetField(FIELD__STRING__M_FIRST_CHAR));
    ILCodeLabel* pNullRefLabel = pslILEmit->NewCodeLabel();

    pslILEmit->EmitLoadNullPtr();
    EmitStoreNativeValue(pslILEmit);

    EmitLoadManagedValue(pslILEmit);
    pslILEmit->EmitBRFALSE(pNullRefLabel);

    EmitLoadManagedValue(pslILEmit);
    pslILEmit->EmitSTLOC(dwPinnedLocal);
    pslILEmit->EmitLDLOC(dwPinnedLocal);
    pslILEmit->EmitLDFLDA(fieldDef);
    EmitStoreNativeValue(pslILEmit);

    EmitLogNativeArgument(pslILEmit, dwPinnedLocal);

    pslILEmit->EmitLabel(pNullRefLabel);
}

void ILWSTRMarshaler::EmitConvertSpaceAndContentsCLRToNativeTemp(ILCodeStream* pslILEmit)
{
    STANDARD_VM_CONTRACT;

    ILCodeLabel* pNullRefLabel = pslILEmit->NewCodeLabel();
    DWORD dwLengthLocalNum = pslILEmit->NewLocal(ELEMENT_TYPE_I4);

    pslILEmit->EmitLoadNullPtr();
    EmitStoreNativeValue(pslILEmit);

    EmitLoadManagedValue(pslILEmit);
    pslILEmit->EmitBRFALSE(pNullRefLabel);

    EmitLoadManagedValue(pslILEmit);
    EmitCheckManagedStringLength(pslILEmit);

    pslILEmit->EmitSTLOC(dwLengthLocalNum);

    ILCodeLabel* pAllocRejoin = pslILEmit->NewCodeLabel();
    ILCodeLabel* pNoOptimize = pslILEmit->NewCodeLabel();
    m_dwLocalBuffer = pslILEmit->NewLocal(ELEMENT_TYPE_I);

    // LocalBuffer = 0
    pslILEmit->EmitLoadNullPtr();
    pslILEmit->EmitSTLOC(m_dwLocalBuffer);

    pslILEmit->EmitLDLOC(dwLengthLocalNum);
    // if (alloc_size_in_bytes > MAX_LOCAL_BUFFER_LENGTH) goto NoOptimize
    pslILEmit->EmitDUP();
    pslILEmit->EmitLDC(MAX_LOCAL_BUFFER_LENGTH);
    pslILEmit->EmitCGT_UN();
    pslILEmit->EmitBRTRUE(pNoOptimize);

    pslILEmit->EmitLOCALLOC();
    pslILEmit->EmitDUP();
    pslILEmit->EmitSTLOC(m_dwLocalBuffer);
    pslILEmit->EmitBR(pAllocRejoin);

    pslILEmit->EmitLabel(pNoOptimize);

    pslILEmit->EmitCALL(METHOD__MARSHAL__ALLOC_CO_TASK_MEM, 1, 1);
    pslILEmit->EmitLabel(pAllocRejoin);
    EmitStoreNativeValue(pslILEmit);

    EmitLoadManagedValue(pslILEmit);
    EmitLoadNativeValue(pslILEmit);

    // src, dst

    pslILEmit->EmitLDLOC(dwLengthLocalNum); // length

    // static void System.String.InternalCopy(String src, IntPtr dest,int len)
    pslILEmit->EmitCALL(METHOD__STRING__INTERNAL_COPY, 3, 0);
    pslILEmit->EmitLabel(pNullRefLabel);
}

//
// input stack:  0: native string
// output stack: 0: num chars, no null
//
void ILWSTRMarshaler::EmitCheckNativeStringLength(ILCodeStream* pslILEmit)
{
    STANDARD_VM_CONTRACT;

    pslILEmit->EmitCALL(METHOD__STRING__WCSLEN, 1, 1);
    pslILEmit->EmitDUP();
    pslILEmit->EmitCALL(METHOD__STUBHELPERS__CHECK_STRING_LENGTH, 1, 0);
}

LocalDesc ILOptimizedAllocMarshaler::GetNativeType()
{
    LIMITED_METHOD_CONTRACT;
    return LocalDesc(ELEMENT_TYPE_I);
}

bool ILOptimizedAllocMarshaler::NeedsClearNative()
{
    LIMITED_METHOD_CONTRACT;
    return true;
}

void ILOptimizedAllocMarshaler::EmitClearNative(ILCodeStream* pslILEmit)
{
    STANDARD_VM_CONTRACT;

    ILCodeLabel *pOptimize = NULL;

    if (m_dwLocalBuffer != LOCAL_NUM_UNUSED)
    {
        pOptimize = pslILEmit->NewCodeLabel();

        // if (m_dwLocalBuffer) goto Optimize
        pslILEmit->EmitLDLOC(m_dwLocalBuffer);
        pslILEmit->EmitBRTRUE(pOptimize);
    }

    EmitLoadNativeValue(pslILEmit);
    // static void m_idClearNative(IntPtr ptr)
    pslILEmit->EmitCALL(m_idClearNative, 1, 0);

    // Optimize:
    if (m_dwLocalBuffer != LOCAL_NUM_UNUSED)
    {
        pslILEmit->EmitLabel(pOptimize);
    }
}

LocalDesc ILUTF8BufferMarshaler::GetManagedType()
{
    STANDARD_VM_CONTRACT;
    return LocalDesc(CoreLibBinder::GetClass(CLASS__STRING_BUILDER));
}

void ILUTF8BufferMarshaler::EmitConvertSpaceCLRToNative(ILCodeStream* pslILEmit)
{
    STANDARD_VM_CONTRACT;

    ILCodeLabel* pNullRefLabel = pslILEmit->NewCodeLabel();

    pslILEmit->EmitLoadNullPtr();
    EmitStoreNativeValue(pslILEmit);

    EmitLoadManagedValue(pslILEmit);
    pslILEmit->EmitBRFALSE(pNullRefLabel);

    EmitLoadManagedValue(pslILEmit);
    // int System.Text.StringBuilder.get_Capacity()
    pslILEmit->EmitCALL(METHOD__STRING_BUILDER__GET_CAPACITY, 1, 1);
    pslILEmit->EmitDUP();

    // static void StubHelpers.CheckStringLength(int length)
    pslILEmit->EmitCALL(METHOD__STUBHELPERS__CHECK_STRING_LENGTH, 1, 0);

    // Max number of bytes for UTF8 string in BMP plane is ( StringBuilder.Capacity + 1 ) * 3 + 1
    // first +1 if the high surrogate is '?' and second +1 for null byte.

    // stack: capacity_in_bytes
    pslILEmit->EmitLDC(1);
    pslILEmit->EmitADD();

    // stack: capacity
    pslILEmit->EmitLDC(3);
    pslILEmit->EmitMUL();

    // stack: offset_of_null
    DWORD dwTmpOffsetOfSecretNull = pslILEmit->NewLocal(ELEMENT_TYPE_I4);
    pslILEmit->EmitDUP();
    pslILEmit->EmitSTLOC(dwTmpOffsetOfSecretNull); // make sure the stack is empty for localloc

    // make space for '\0'
    pslILEmit->EmitLDC(1);
    pslILEmit->EmitADD();

    // stack: alloc_size_in_bytes
    ILCodeLabel *pAllocRejoin = pslILEmit->NewCodeLabel();
    if (IsCLRToNative(m_dwMarshalFlags) && !IsByref(m_dwMarshalFlags) && !IsFieldMarshal(m_dwMarshalFlags))
    {
        ILCodeLabel *pNoOptimize = pslILEmit->NewCodeLabel();
        m_dwLocalBuffer = pslILEmit->NewLocal(ELEMENT_TYPE_I);

        // LocalBuffer = 0
        pslILEmit->EmitLoadNullPtr();
        pslILEmit->EmitSTLOC(m_dwLocalBuffer);

        // if (alloc_size_in_bytes > MAX_LOCAL_BUFFER_LENGTH) goto NoOptimize
        pslILEmit->EmitDUP();
        pslILEmit->EmitLDC(MAX_LOCAL_BUFFER_LENGTH);
        pslILEmit->EmitCGT_UN();
        pslILEmit->EmitBRTRUE(pNoOptimize);

        pslILEmit->EmitLOCALLOC();
        pslILEmit->EmitDUP();
        pslILEmit->EmitSTLOC(m_dwLocalBuffer);
        pslILEmit->EmitBR(pAllocRejoin);

        pslILEmit->EmitLabel(pNoOptimize);
    }

    // static IntPtr AllocCoTaskMem(int cb)
    pslILEmit->EmitCALL(METHOD__MARSHAL__ALLOC_CO_TASK_MEM, 1, 1);

    pslILEmit->EmitLabel(pAllocRejoin);

    // stack: native_addr

    pslILEmit->EmitDUP();
    EmitStoreNativeValue(pslILEmit);

    pslILEmit->EmitLDLOC(dwTmpOffsetOfSecretNull);

    // stack: native_addr offset_of_null
    pslILEmit->EmitADD();

    // stack: addr_of_null0
    pslILEmit->EmitLDC(0);
    pslILEmit->EmitSTIND_I1();

    pslILEmit->EmitLabel(pNullRefLabel);
}

void ILUTF8BufferMarshaler::EmitConvertContentsCLRToNative(ILCodeStream* pslILEmit)
{
    STANDARD_VM_CONTRACT;
    DWORD dwUtf8MarshalFlags =
        (m_pargs->m_pMarshalInfo->GetBestFitMapping() & 0xFF) |
        (m_pargs->m_pMarshalInfo->GetThrowOnUnmappableChar() << 8);

    // setup to call UTF8BufferMarshaler.ConvertToNative
    EmitLoadManagedValue(pslILEmit);
    EmitLoadNativeValue(pslILEmit);
    pslILEmit->EmitLDC(dwUtf8MarshalFlags);

    //ConvertToNative(StringBuilder sb,IntPtr pNativeBuffer, int flags)
    pslILEmit->EmitCALL(METHOD__UTF8BUFFERMARSHALER__CONVERT_TO_NATIVE, 3, 1);
    EmitStoreNativeValue(pslILEmit);
}

void ILUTF8BufferMarshaler::EmitConvertSpaceNativeToCLR(ILCodeStream* pslILEmit)
{
    STANDARD_VM_CONTRACT;

    ILCodeLabel* pNullRefLabel = pslILEmit->NewCodeLabel();

    EmitLoadNativeValue(pslILEmit);
    pslILEmit->EmitBRFALSE(pNullRefLabel);

    if (IsIn(m_dwMarshalFlags) || IsCLRToNative(m_dwMarshalFlags))
    {
        EmitLoadNativeValue(pslILEmit);
        // static int System.String.strlen(byte* ptr)
        pslILEmit->EmitCALL(METHOD__STRING__STRLEN, 1, 1);
    }
    else
    {
        // don't touch the native buffer in the native->CLR out-only case
        pslILEmit->EmitLDC(0);
    }
    // Convert to UTF8 and then call
    // System.Text.StringBuilder..ctor(int capacity)
    pslILEmit->EmitNEWOBJ(METHOD__STRING_BUILDER__CTOR_INT, 1);
    EmitStoreManagedValue(pslILEmit);
    pslILEmit->EmitLabel(pNullRefLabel);
}

void ILUTF8BufferMarshaler::EmitConvertContentsNativeToCLR(ILCodeStream* pslILEmit)
{
    STANDARD_VM_CONTRACT;

    EmitLoadManagedValue(pslILEmit);
    EmitLoadNativeValue(pslILEmit);

    //void UTF8BufferMarshaler.ConvertToManaged(StringBuilder sb, IntPtr pNative)
    pslILEmit->EmitCALL(METHOD__UTF8BUFFERMARSHALER__CONVERT_TO_MANAGED, 2, 0);
}


LocalDesc ILWSTRBufferMarshaler::GetManagedType()
{
    STANDARD_VM_CONTRACT;

    return LocalDesc(CoreLibBinder::GetClass(CLASS__STRING_BUILDER));
}

void ILWSTRBufferMarshaler::EmitConvertSpaceCLRToNative(ILCodeStream* pslILEmit)
{
    STANDARD_VM_CONTRACT;

    ILCodeLabel* pNullRefLabel = pslILEmit->NewCodeLabel();

    pslILEmit->EmitLoadNullPtr();
    EmitStoreNativeValue(pslILEmit);

    EmitLoadManagedValue(pslILEmit);
    pslILEmit->EmitBRFALSE(pNullRefLabel);

    EmitLoadManagedValue(pslILEmit);
    // int System.Text.StringBuilder.get_Capacity()
    pslILEmit->EmitCALL(METHOD__STRING_BUILDER__GET_CAPACITY, 1, 1);
    pslILEmit->EmitDUP();

    // static void StubHelpers.CheckStringLength(int length)
    pslILEmit->EmitCALL(METHOD__STUBHELPERS__CHECK_STRING_LENGTH, 1, 0);

    // stack: capacity

    pslILEmit->EmitLDC(2);
    pslILEmit->EmitMUL();

    // stack: capacity_in_bytes

    pslILEmit->EmitLDC(2);
    pslILEmit->EmitADD();

    // stack: offset_of_secret_null

    DWORD dwTmpOffsetOfSecretNull = pslILEmit->NewLocal(ELEMENT_TYPE_I4);
    pslILEmit->EmitDUP();
    pslILEmit->EmitSTLOC(dwTmpOffsetOfSecretNull); // make sure the stack is empty for localloc

    pslILEmit->EmitLDC(2);
    pslILEmit->EmitADD();

    // stack: alloc_size_in_bytes
    ILCodeLabel *pAllocRejoin = pslILEmit->NewCodeLabel();
    if (IsCLRToNative(m_dwMarshalFlags) && !IsByref(m_dwMarshalFlags) && !IsFieldMarshal(m_dwMarshalFlags))
    {
        ILCodeLabel *pNoOptimize = pslILEmit->NewCodeLabel();
        m_dwLocalBuffer = pslILEmit->NewLocal(ELEMENT_TYPE_I);

        // LocalBuffer = 0
        pslILEmit->EmitLoadNullPtr();
        pslILEmit->EmitSTLOC(m_dwLocalBuffer);

        // if (alloc_size_in_bytes > MAX_LOCAL_BUFFER_LENGTH) goto NoOptimize
        pslILEmit->EmitDUP();
        pslILEmit->EmitLDC(MAX_LOCAL_BUFFER_LENGTH);
        pslILEmit->EmitCGT_UN();
        pslILEmit->EmitBRTRUE(pNoOptimize);

        pslILEmit->EmitLOCALLOC();
        pslILEmit->EmitDUP();
        pslILEmit->EmitSTLOC(m_dwLocalBuffer);
        pslILEmit->EmitBR(pAllocRejoin);

        pslILEmit->EmitLabel(pNoOptimize);
    }

    // static IntPtr AllocCoTaskMem(int cb)
    pslILEmit->EmitCALL(METHOD__MARSHAL__ALLOC_CO_TASK_MEM, 1, 1);

    pslILEmit->EmitLabel(pAllocRejoin);

    // stack: native_addr

    pslILEmit->EmitDUP();
    EmitStoreNativeValue(pslILEmit);

    pslILEmit->EmitLDLOC(dwTmpOffsetOfSecretNull);

    // stack: offset_of_secret_null  native_addr

    pslILEmit->EmitADD();

    // stack: addr_of_secret_null

    pslILEmit->EmitLDC(0);

    // stack: addr_of_secret_null  0

    pslILEmit->EmitSTIND_I2();
    pslILEmit->EmitLabel(pNullRefLabel);
}

void ILWSTRBufferMarshaler::EmitConvertContentsCLRToNative(ILCodeStream* pslILEmit)
{
    STANDARD_VM_CONTRACT;

    DWORD dwTempNumCharsLocal = pslILEmit->NewLocal(ELEMENT_TYPE_I4);

    ILCodeLabel* pNullRefLabel = pslILEmit->NewCodeLabel();

    EmitLoadNativeValue(pslILEmit);
    pslILEmit->EmitBRFALSE(pNullRefLabel);

    EmitLoadManagedValue(pslILEmit);
    pslILEmit->EmitDUP();

    // stack: StringBuilder StringBuilder

    // int System.Text.StringBuilder.get_Length()
    pslILEmit->EmitCALL(METHOD__STRING_BUILDER__GET_LENGTH, 1, 1);

    // stack: StringBuilder length
    pslILEmit->EmitDUP();
    // static void StubHelpers.CheckStringLength(int length)
    pslILEmit->EmitCALL(METHOD__STUBHELPERS__CHECK_STRING_LENGTH, 1, 0);

    // stack: StringBuilder length

    pslILEmit->EmitSTLOC(dwTempNumCharsLocal);

    // stack: StringBuilder

    EmitLoadNativeValue(pslILEmit);
    pslILEmit->EmitLDLOC(dwTempNumCharsLocal);

    // stack: StringBuilder native_buffer length

    // void System.Text.StringBuilder.InternalCopy(IntPtr dest,int len)
    pslILEmit->EmitCALL(METHOD__STRING_BUILDER__INTERNAL_COPY, 3, 0);

    //
    // null-terminate the native string
    //
    EmitLoadNativeValue(pslILEmit);
    pslILEmit->EmitLDLOC(dwTempNumCharsLocal);
    pslILEmit->EmitDUP();
    pslILEmit->EmitADD(); // dwTempNumCharsLocal + dwTempNumCharsLocal
    pslILEmit->EmitADD(); // + native_buffer
    pslILEmit->EmitLDC(0);
    pslILEmit->EmitSTIND_I2();

    pslILEmit->EmitLabel(pNullRefLabel);
}

void ILWSTRBufferMarshaler::EmitConvertSpaceNativeToCLR(ILCodeStream* pslILEmit)
{
    STANDARD_VM_CONTRACT;

    ILCodeLabel* pNullRefLabel = pslILEmit->NewCodeLabel();

    EmitLoadNativeValue(pslILEmit);
    pslILEmit->EmitBRFALSE(pNullRefLabel);

    if (IsIn(m_dwMarshalFlags) || IsCLRToNative(m_dwMarshalFlags))
    {
        EmitLoadNativeValue(pslILEmit);
        // static int System.String.u16_strlen(char *ptr)
        pslILEmit->EmitCALL(METHOD__STRING__WCSLEN, 1, 1);
    }
    else
    {
        // don't touch the native buffer in the native->CLR out-only case
        pslILEmit->EmitLDC(0);
    }

    // System.Text.StringBuilder..ctor(int capacity)
    pslILEmit->EmitNEWOBJ(METHOD__STRING_BUILDER__CTOR_INT, 1);
    EmitStoreManagedValue(pslILEmit);

    pslILEmit->EmitLabel(pNullRefLabel);
}

void ILWSTRBufferMarshaler::EmitConvertContentsNativeToCLR(ILCodeStream* pslILEmit)
{
    STANDARD_VM_CONTRACT;

    ILCodeLabel* pNullRefLabel = pslILEmit->NewCodeLabel();

    EmitLoadNativeValue(pslILEmit);
    pslILEmit->EmitBRFALSE(pNullRefLabel);

    EmitLoadManagedValue(pslILEmit);
    EmitLoadNativeValue(pslILEmit);

    pslILEmit->EmitDUP();
    // static int System.String.u16_strlen(char *ptr)
    pslILEmit->EmitCALL(METHOD__STRING__WCSLEN, 1, 1);

    // void System.Text.StringBuilder.ReplaceBuffer(char* newBuffer, int newLength);
    pslILEmit->EmitCALL(METHOD__STRING_BUILDER__REPLACE_BUFFER_INTERNAL, 3, 0);
    pslILEmit->EmitLabel(pNullRefLabel);
}

LocalDesc ILCSTRBufferMarshaler::GetManagedType()
{
    STANDARD_VM_CONTRACT;

    return LocalDesc(CoreLibBinder::GetClass(CLASS__STRING_BUILDER));
}

void ILCSTRBufferMarshaler::EmitConvertSpaceCLRToNative(ILCodeStream* pslILEmit)
{
    STANDARD_VM_CONTRACT;

    ILCodeLabel* pNullRefLabel = pslILEmit->NewCodeLabel();

    pslILEmit->EmitLoadNullPtr();
    EmitStoreNativeValue(pslILEmit);

    EmitLoadManagedValue(pslILEmit);
    pslILEmit->EmitBRFALSE(pNullRefLabel);

    EmitLoadManagedValue(pslILEmit);
    // int System.Text.StringBuilder.get_Capacity()
    pslILEmit->EmitCALL(METHOD__STRING_BUILDER__GET_CAPACITY, 1, 1);
    pslILEmit->EmitDUP();

    // static void StubHelpers.CheckStringLength(int length)
    pslILEmit->EmitCALL(METHOD__STUBHELPERS__CHECK_STRING_LENGTH, 1, 0);

    // stack: capacity

    pslILEmit->EmitLDSFLD(pslILEmit->GetToken(CoreLibBinder::GetField(FIELD__MARSHAL__SYSTEM_MAX_DBCS_CHAR_SIZE)));
    pslILEmit->EmitMUL_OVF();

    // stack: capacity_in_bytes

    pslILEmit->EmitLDC(1);
    pslILEmit->EmitADD_OVF();

    // stack: offset_of_secret_null

    DWORD dwTmpOffsetOfSecretNull = pslILEmit->NewLocal(ELEMENT_TYPE_I4);
    pslILEmit->EmitDUP();
    pslILEmit->EmitSTLOC(dwTmpOffsetOfSecretNull); // make sure the stack is empty for localloc

    pslILEmit->EmitLDC(3);
    pslILEmit->EmitADD_OVF();

    // stack: alloc_size_in_bytes
    ILCodeLabel *pAllocRejoin = pslILEmit->NewCodeLabel();
    if (IsCLRToNative(m_dwMarshalFlags) && !IsByref(m_dwMarshalFlags) && !IsFieldMarshal(m_dwMarshalFlags))
    {
        ILCodeLabel *pNoOptimize = pslILEmit->NewCodeLabel();
        m_dwLocalBuffer = pslILEmit->NewLocal(ELEMENT_TYPE_I);

        // LocalBuffer = 0
        pslILEmit->EmitLoadNullPtr();
        pslILEmit->EmitSTLOC(m_dwLocalBuffer);

        // if (alloc_size_in_bytes > MAX_LOCAL_BUFFER_LENGTH) goto NoOptimize
        pslILEmit->EmitDUP();
        pslILEmit->EmitLDC(MAX_LOCAL_BUFFER_LENGTH);
        pslILEmit->EmitCGT_UN();
        pslILEmit->EmitBRTRUE(pNoOptimize);

        pslILEmit->EmitLOCALLOC();
        pslILEmit->EmitDUP();
        pslILEmit->EmitSTLOC(m_dwLocalBuffer);
        pslILEmit->EmitBR(pAllocRejoin);

        pslILEmit->EmitLabel(pNoOptimize);
    }

    // static IntPtr AllocCoTaskMem(int cb)
    pslILEmit->EmitCALL(METHOD__MARSHAL__ALLOC_CO_TASK_MEM, 1, 1);

    pslILEmit->EmitLabel(pAllocRejoin);

    // stack: native_addr

    pslILEmit->EmitDUP();
    EmitStoreNativeValue(pslILEmit);

    pslILEmit->EmitLDLOC(dwTmpOffsetOfSecretNull);

    // stack: native_addr offset_of_secret_null

    pslILEmit->EmitADD();

    // stack: addr_of_secret_null0

    pslILEmit->EmitDUP();
    pslILEmit->EmitLDC(0);
    pslILEmit->EmitSTIND_I1();

    // stack: addr_of_secret_null0

    pslILEmit->EmitDUP();
    pslILEmit->EmitLDC(1);
    pslILEmit->EmitADD();
    pslILEmit->EmitLDC(0);
    pslILEmit->EmitSTIND_I1();

    // stack: addr_of_secret_null0

    pslILEmit->EmitLDC(2);
    pslILEmit->EmitADD();
    pslILEmit->EmitLDC(0);
    pslILEmit->EmitSTIND_I1();

    pslILEmit->EmitLabel(pNullRefLabel);
}

void ILCSTRBufferMarshaler::EmitConvertContentsCLRToNative(ILCodeStream* pslILEmit)
{
    STANDARD_VM_CONTRACT;

    ILCodeLabel* pNullRefLabel = pslILEmit->NewCodeLabel();
    DWORD dwNumBytesLocalNum = pslILEmit->NewLocal(ELEMENT_TYPE_I4);
    DWORD dwSrcLocal = pslILEmit->NewLocal(ELEMENT_TYPE_OBJECT);

    EmitLoadNativeValue(pslILEmit);
    pslILEmit->EmitBRFALSE(pNullRefLabel);

    EmitLoadManagedValue(pslILEmit);
    // int System.Text.StringBuilder.get_Length()
    pslILEmit->EmitCALL(METHOD__STRING_BUILDER__GET_LENGTH, 1, 1);
    // static void StubHelpers.CheckStringLength(int length)
    pslILEmit->EmitCALL(METHOD__STUBHELPERS__CHECK_STRING_LENGTH, 1, 0);

    EmitLoadManagedValue(pslILEmit);
    // String System.Text.StringBuilder.ToString()
    pslILEmit->EmitCALL(METHOD__STRING_BUILDER__TO_STRING, 1, 1);
    pslILEmit->EmitLDC(m_pargs->m_pMarshalInfo->GetBestFitMapping());
    pslILEmit->EmitLDC(m_pargs->m_pMarshalInfo->GetThrowOnUnmappableChar());
    pslILEmit->EmitLDLOCA(dwNumBytesLocalNum);

    // static byte[] DoAnsiConversion(string str, bool fBestFit, bool fThrowOnUnmappableChar, out int cbLength)
    pslILEmit->EmitCALL(METHOD__ANSICHARMARSHALER__DO_ANSI_CONVERSION, 4, 1);
    pslILEmit->EmitSTLOC(dwSrcLocal);
    EmitLoadNativeValue(pslILEmit);             // pDest
    pslILEmit->EmitLDC(0);                      // destIndex
    pslILEmit->EmitLDLOC(dwSrcLocal);           // src[]
    pslILEmit->EmitLDC(0);                      // srcIndex
    pslILEmit->EmitLDLOC(dwNumBytesLocalNum);   // len

    // static void Memcpy(byte* pDest, int destIndex, byte[] src, int srcIndex, int len)
    pslILEmit->EmitCALL(METHOD__BUFFER__MEMCPY_PTRBYTE_ARRBYTE, 5, 0);

    // null terminate the string
    EmitLoadNativeValue(pslILEmit);
    pslILEmit->EmitLDLOC(dwNumBytesLocalNum);
    pslILEmit->EmitADD();
    pslILEmit->EmitLDC(0);
    pslILEmit->EmitSTIND_I1();

    pslILEmit->EmitLabel(pNullRefLabel);
}

void ILCSTRBufferMarshaler::EmitConvertSpaceNativeToCLR(ILCodeStream* pslILEmit)
{
    STANDARD_VM_CONTRACT;

    ILCodeLabel* pNullRefLabel = pslILEmit->NewCodeLabel();

    EmitLoadNativeValue(pslILEmit);
    pslILEmit->EmitBRFALSE(pNullRefLabel);

    if (IsIn(m_dwMarshalFlags) || IsCLRToNative(m_dwMarshalFlags))
    {
        EmitLoadNativeValue(pslILEmit);
        // static int System.String.strlen(byte* ptr)
        pslILEmit->EmitCALL(METHOD__STRING__STRLEN, 1, 1);
    }
    else
    {
        // don't touch the native buffer in the native->CLR out-only case
        pslILEmit->EmitLDC(0);
    }

    // System.Text.StringBuilder..ctor(int capacity)
    pslILEmit->EmitNEWOBJ(METHOD__STRING_BUILDER__CTOR_INT, 1);
    EmitStoreManagedValue(pslILEmit);

    pslILEmit->EmitLabel(pNullRefLabel);
}

void ILCSTRBufferMarshaler::EmitConvertContentsNativeToCLR(ILCodeStream* pslILEmit)
{
    STANDARD_VM_CONTRACT;

    ILCodeLabel* pNullRefLabel = pslILEmit->NewCodeLabel();

    EmitLoadNativeValue(pslILEmit);
    pslILEmit->EmitBRFALSE(pNullRefLabel);

    EmitLoadManagedValue(pslILEmit);
    EmitLoadNativeValue(pslILEmit);

    pslILEmit->EmitDUP();
    // static int System.String.strlen(byte* ptr)
    pslILEmit->EmitCALL(METHOD__STRING__STRLEN, 1, 1);

    // void System.Text.StringBuilder.ReplaceBuffer(sbyte* newBuffer, int newLength);
    pslILEmit->EmitCALL(METHOD__STRING_BUILDER__REPLACE_BUFFER_ANSI_INTERNAL, 3, 0);

    pslILEmit->EmitLabel(pNullRefLabel);
}



LocalDesc ILValueClassMarshaler::GetNativeType()
{
    STANDARD_VM_CONTRACT;

    return LocalDesc(TypeHandle(m_pargs->m_pMT).MakeNativeValueType());
}

LocalDesc ILValueClassMarshaler::GetManagedType()
{
    LIMITED_METHOD_CONTRACT;

    return LocalDesc(m_pargs->m_pMT);
}

void ILValueClassMarshaler::EmitReInitNative(ILCodeStream* pslILEmit)
{
    STANDARD_VM_CONTRACT;

    EmitLoadNativeHomeAddr(pslILEmit);
    pslILEmit->EmitINITOBJ(pslILEmit->GetToken(TypeHandle(m_pargs->m_pMT).MakeNativeValueType()));
}

bool ILValueClassMarshaler::NeedsClearNative()
{
    return true;
}

void ILValueClassMarshaler::EmitClearNative(ILCodeStream * pslILEmit)
{
    STANDARD_VM_CONTRACT;

    MethodDesc* pStructMarshalStub = NDirect::CreateStructMarshalILStub(m_pargs->m_pMT);

    EmitLoadManagedHomeAddr(pslILEmit);
    EmitLoadNativeHomeAddr(pslILEmit);
    pslILEmit->EmitLDC(StructMarshalStubs::MarshalOperation::Cleanup);
    EmitLoadCleanupWorkList(pslILEmit);

    pslILEmit->EmitCALL(pslILEmit->GetToken(pStructMarshalStub), 4, 0);
}


void ILValueClassMarshaler::EmitConvertContentsCLRToNative(ILCodeStream* pslILEmit)
{
    STANDARD_VM_CONTRACT;

    MethodDesc* pStructMarshalStub = NDirect::CreateStructMarshalILStub(m_pargs->m_pMT);

    EmitLoadManagedHomeAddr(pslILEmit);
    EmitLoadNativeHomeAddr(pslILEmit);
    pslILEmit->EmitLDC(StructMarshalStubs::MarshalOperation::Marshal);
    EmitLoadCleanupWorkList(pslILEmit);

    pslILEmit->EmitCALL(pslILEmit->GetToken(pStructMarshalStub), 4, 0);
}

void ILValueClassMarshaler::EmitConvertContentsNativeToCLR(ILCodeStream* pslILEmit)
{
    STANDARD_VM_CONTRACT;

    MethodDesc* pStructMarshalStub = NDirect::CreateStructMarshalILStub(m_pargs->m_pMT);

    EmitLoadManagedHomeAddr(pslILEmit);
    EmitLoadNativeHomeAddr(pslILEmit);
    pslILEmit->EmitLDC(StructMarshalStubs::MarshalOperation::Unmarshal);
    EmitLoadCleanupWorkList(pslILEmit);

    pslILEmit->EmitCALL(pslILEmit->GetToken(pStructMarshalStub), 4, 0);
}


#ifdef FEATURE_COMINTEROP
LocalDesc ILObjectMarshaler::GetNativeType()
{
    STANDARD_VM_CONTRACT;

    return LocalDesc(TypeHandle(CoreLibBinder::GetClass(CLASS__COMVARIANT)));
}

LocalDesc ILObjectMarshaler::GetManagedType()
{
    LIMITED_METHOD_CONTRACT;

    return LocalDesc(ELEMENT_TYPE_OBJECT);
}

void ILObjectMarshaler::EmitConvertContentsCLRToNative(ILCodeStream* pslILEmit)
{
    STANDARD_VM_CONTRACT;

    if (!IsCLRToNative(m_dwMarshalFlags) && IsByref(m_dwMarshalFlags) && IsIn(m_dwMarshalFlags))
    {
        // Keep the VARIANT as it is - the stubhelper will do a VT_BYREF check on it.
    }
    else
    {
        // V_VT(pDest) = VT_EMPTY
        EmitReInitNative(pslILEmit);
    }

    EmitLoadManagedValue(pslILEmit);                        // load src
    EmitLoadNativeHomeAddr(pslILEmit);                      // load dst
    pslILEmit->EmitCALL(METHOD__OBJECTMARSHALER__CONVERT_TO_NATIVE, 2, 0); // void ConvertToNative(object objSrc, IntPtr pDstVariant)
}

void ILObjectMarshaler::EmitConvertContentsNativeToCLR(ILCodeStream* pslILEmit)
{
    STANDARD_VM_CONTRACT;

    EmitLoadNativeHomeAddr(pslILEmit);
    pslILEmit->EmitCALL(METHOD__OBJECTMARSHALER__CONVERT_TO_MANAGED, 1, 1);  // object ConvertToManaged(IntPtr pSrcVariant);
    EmitStoreManagedValue(pslILEmit);
}

bool ILObjectMarshaler::NeedsClearNative()
{
    LIMITED_METHOD_CONTRACT;
    return true;
}

void ILObjectMarshaler::EmitClearNative(ILCodeStream* pslILEmit)
{
    STANDARD_VM_CONTRACT;

    if (!IsCLRToNative(m_dwMarshalFlags) && IsByref(m_dwMarshalFlags) && IsIn(m_dwMarshalFlags))
    {
        // We don't want to clear variants passed from native by-ref here as we
        // want to be able to detect the VT_BYREF case during backpropagation.

        // @TODO: We shouldn't be skipping the call if pslILEmit is ILStubLinker::kExceptionCleanup
        // because we always want to do real cleanup in this stream.
    }
    else
    {
        EmitLoadNativeHomeAddr(pslILEmit);
        pslILEmit->EmitCALL(METHOD__OBJECTMARSHALER__CLEAR_NATIVE, 1, 0);
    }
}

void ILObjectMarshaler::EmitReInitNative(ILCodeStream* pslILEmit)
{
    CONTRACTL
    {
        STANDARD_VM_CHECK;
        CONSISTENCY_CHECK(offsetof(VARIANT, vt) == 0);
    }
    CONTRACTL_END;

    if (!IsCLRToNative(m_dwMarshalFlags) && IsByref(m_dwMarshalFlags) && IsIn(m_dwMarshalFlags))
    {
        // We don't want to clear variants passed from native by-ref here as we
        // want to be able to detect the VT_BYREF case during backpropagation.
    }
    else
    {
        EmitLoadNativeHomeAddr(pslILEmit);
        pslILEmit->EmitLDC(VT_EMPTY);
        pslILEmit->EmitSTIND_I2();
    }
}
#endif // FEATURE_COMINTEROP

LocalDesc ILDateMarshaler::GetNativeType()
{
    LIMITED_METHOD_CONTRACT;

    return LocalDesc(ELEMENT_TYPE_R8);
}

LocalDesc ILDateMarshaler::GetManagedType()
{
    STANDARD_VM_CONTRACT;

    return LocalDesc(CoreLibBinder::GetClass(CLASS__DATE_TIME));
}

void ILDateMarshaler::EmitConvertContentsCLRToNative(ILCodeStream* pslILEmit)
{
    STANDARD_VM_CONTRACT;

    EmitLoadManagedValue(pslILEmit);
    // double ConvertToNative(INT64 managedDate)
    pslILEmit->EmitCALL(METHOD__DATEMARSHALER__CONVERT_TO_NATIVE, 1, 1);
    EmitStoreNativeValue(pslILEmit);
}

void ILDateMarshaler::EmitConvertContentsNativeToCLR(ILCodeStream* pslILEmit)
{
    STANDARD_VM_CONTRACT;

    // will call DateTime constructor on managed home
    EmitLoadManagedHomeAddr(pslILEmit);

    EmitLoadNativeValue(pslILEmit);
    // long ConvertToNative(double nativeData)
    pslILEmit->EmitCALL(METHOD__DATEMARSHALER__CONVERT_TO_MANAGED, 1, 1);

    pslILEmit->EmitCALL(METHOD__DATE_TIME__LONG_CTOR, 2, 0);
}

void ILDateMarshaler::EmitReInitNative(ILCodeStream* pslILEmit)
{
    STANDARD_VM_CONTRACT;

    // ldc.i4.0, conv.r8 is shorter than ldc.r8 0.0
    pslILEmit->EmitLDC(0);
    pslILEmit->EmitCONV_R8();
    EmitStoreNativeValue(pslILEmit);
}

LocalDesc ILCurrencyMarshaler::GetNativeType()
{
    STANDARD_VM_CONTRACT;

    return LocalDesc(TypeHandle(CoreLibBinder::GetClass(CLASS__CURRENCY)));
}

LocalDesc ILCurrencyMarshaler::GetManagedType()
{
    STANDARD_VM_CONTRACT;

    return LocalDesc(TypeHandle(CoreLibBinder::GetClass(CLASS__DECIMAL)));
}


void ILCurrencyMarshaler::EmitReInitNative(ILCodeStream* pslILEmit)
{
    STANDARD_VM_CONTRACT;

    EmitLoadNativeHomeAddr(pslILEmit);
    pslILEmit->EmitINITOBJ(pslILEmit->GetToken(TypeHandle(CoreLibBinder::GetClass(CLASS__CURRENCY))));
}

void ILCurrencyMarshaler::EmitConvertContentsCLRToNative(ILCodeStream* pslILEmit)
{
    STANDARD_VM_CONTRACT;

    EmitLoadNativeHomeAddr(pslILEmit);
    EmitLoadManagedValue(pslILEmit);

    pslILEmit->EmitCALL(METHOD__CURRENCY__DECIMAL_CTOR, 2, 0);
}

void ILCurrencyMarshaler::EmitConvertContentsNativeToCLR(ILCodeStream* pslILEmit)
{
    STANDARD_VM_CONTRACT;

    EmitLoadManagedHomeAddr(pslILEmit);
    EmitLoadNativeValue(pslILEmit);

    pslILEmit->EmitCALL(METHOD__DECIMAL__CURRENCY_CTOR, 2, 0);
}


#ifdef FEATURE_COMINTEROP
LocalDesc ILInterfaceMarshaler::GetNativeType()
{
    LIMITED_METHOD_CONTRACT;

    return LocalDesc(ELEMENT_TYPE_I);
}

LocalDesc ILInterfaceMarshaler::GetManagedType()
{
    LIMITED_METHOD_CONTRACT;

    return LocalDesc(ELEMENT_TYPE_OBJECT);
}

void ILInterfaceMarshaler::EmitConvertContentsCLRToNative(ILCodeStream* pslILEmit)
{
    STANDARD_VM_CONTRACT;

    ItfMarshalInfo itfInfo;
    m_pargs->m_pMarshalInfo->GetItfMarshalInfo(&itfInfo);

    EmitLoadManagedValue(pslILEmit);

    if (itfInfo.thNativeItf.GetMethodTable())
    {
        pslILEmit->EmitLDTOKEN(pslILEmit->GetToken(itfInfo.thNativeItf.GetMethodTable()));
        pslILEmit->EmitCALL(METHOD__RT_TYPE_HANDLE__TO_INTPTR, 1, 1);
    }
    else
    {
        pslILEmit->EmitLoadNullPtr();
    }

    if (itfInfo.thClass.GetMethodTable())
    {
        pslILEmit->EmitLDTOKEN(pslILEmit->GetToken(itfInfo.thClass.GetMethodTable()));
        pslILEmit->EmitCALL(METHOD__RT_TYPE_HANDLE__TO_INTPTR, 1, 1);
    }
    else
    {
        pslILEmit->EmitLoadNullPtr();
    }
    pslILEmit->EmitLDC(itfInfo.dwFlags);

    // static IntPtr ConvertToNative(object objSrc, IntPtr itfMT, IntPtr classMT, int flags);
    pslILEmit->EmitCALL(METHOD__INTERFACEMARSHALER__CONVERT_TO_NATIVE, 4, 1);

    EmitStoreNativeValue(pslILEmit);
}

void ILInterfaceMarshaler::EmitConvertContentsNativeToCLR(ILCodeStream* pslILEmit)
{
    STANDARD_VM_CONTRACT;

    ItfMarshalInfo itfInfo;
    m_pargs->m_pMarshalInfo->GetItfMarshalInfo(&itfInfo);

    // the helper may assign NULL to the home (see below)
    EmitLoadNativeHomeAddr(pslILEmit);

    if (itfInfo.thItf.GetMethodTable())
    {
        pslILEmit->EmitLDTOKEN(pslILEmit->GetToken(itfInfo.thItf.GetMethodTable()));
        pslILEmit->EmitCALL(METHOD__RT_TYPE_HANDLE__TO_INTPTR, 1, 1);
    }
    else
    {
        pslILEmit->EmitLoadNullPtr();
    }

    if (itfInfo.thClass.GetMethodTable())
    {
        pslILEmit->EmitLDTOKEN(pslILEmit->GetToken(itfInfo.thClass.GetMethodTable()));
        pslILEmit->EmitCALL(METHOD__RT_TYPE_HANDLE__TO_INTPTR, 1, 1);
    }
    else
    {
        pslILEmit->EmitLoadNullPtr();
    }
    pslILEmit->EmitLDC(itfInfo.dwFlags);

    // static object ConvertToManaged(IntPtr pUnk, IntPtr itfMT, IntPtr classMT, int flags);
    pslILEmit->EmitCALL(METHOD__INTERFACEMARSHALER__CONVERT_TO_MANAGED, 4, 1);

    EmitStoreManagedValue(pslILEmit);
}

bool ILInterfaceMarshaler::NeedsClearNative()
{
    LIMITED_METHOD_CONTRACT;
    return true;
}

void ILMarshaler::EmitInterfaceClearNative(ILCodeStream* pslILEmit)
{
    STANDARD_VM_CONTRACT;

    ILCodeLabel *pSkipClearNativeLabel = pslILEmit->NewCodeLabel();
    EmitLoadNativeValue(pslILEmit);
    pslILEmit->EmitBRFALSE(pSkipClearNativeLabel);
    EmitLoadNativeValue(pslILEmit);
    // static void ClearNative(IntPtr pUnk);
    pslILEmit->EmitCALL(METHOD__INTERFACEMARSHALER__CLEAR_NATIVE, 1, 0);
    pslILEmit->EmitLabel(pSkipClearNativeLabel);
}

void ILInterfaceMarshaler::EmitClearNative(ILCodeStream* pslILEmit)
{
    STANDARD_VM_CONTRACT;
    EmitInterfaceClearNative(pslILEmit);
}
#endif // FEATURE_COMINTEROP


LocalDesc ILAnsiCharMarshaler::GetNativeType()
{
    LIMITED_METHOD_CONTRACT;

    return LocalDesc(ELEMENT_TYPE_U1);
}

LocalDesc ILAnsiCharMarshaler::GetManagedType()
{
    LIMITED_METHOD_CONTRACT;

    return LocalDesc(ELEMENT_TYPE_CHAR);
}

void ILAnsiCharMarshaler::EmitConvertContentsCLRToNative(ILCodeStream* pslILEmit)
{
    STANDARD_VM_CONTRACT;

    EmitLoadManagedValue(pslILEmit);
    pslILEmit->EmitLDC(m_pargs->m_pMarshalInfo->GetBestFitMapping());
    pslILEmit->EmitLDC(m_pargs->m_pMarshalInfo->GetThrowOnUnmappableChar());
    pslILEmit->EmitCALL(METHOD__ANSICHARMARSHALER__CONVERT_TO_NATIVE, 3, 1);
    EmitStoreNativeValue(pslILEmit);
}

void ILAnsiCharMarshaler::EmitConvertContentsNativeToCLR(ILCodeStream* pslILEmit)
{
    STANDARD_VM_CONTRACT;

    EmitLoadNativeValue(pslILEmit);
    pslILEmit->EmitCALL(METHOD__ANSICHARMARSHALER__CONVERT_TO_MANAGED, 1, 1);
    EmitStoreManagedValue(pslILEmit);
}

#ifdef FEATURE_COMINTEROP
LocalDesc ILOleColorMarshaler::GetNativeType()
{
    LIMITED_METHOD_CONTRACT;

    return LocalDesc(ELEMENT_TYPE_I4);
}

LocalDesc ILOleColorMarshaler::GetManagedType()
{
    STANDARD_VM_CONTRACT;

    LoaderAllocator* pLoader = m_pargs->m_pMarshalInfo->GetModule()->GetLoaderAllocator();
    TypeHandle  hndColorType = pLoader->GetMarshalingData()->GetOleColorMarshalingInfo()->GetColorType();

    //
    // value class
    //
    return LocalDesc(hndColorType); // System.Drawing.Color
}

void ILOleColorMarshaler::EmitConvertContentsCLRToNative(ILCodeStream* pslILEmit)
{
    STANDARD_VM_CONTRACT;

    LoaderAllocator* pLoader = m_pargs->m_pMarshalInfo->GetModule()->GetLoaderAllocator();
    MethodDesc* pConvertMD = pLoader->GetMarshalingData()->GetOleColorMarshalingInfo()->GetSystemColorToOleColorMD();

    EmitLoadManagedValue(pslILEmit);
    // int System.Drawing.ColorTranslator.ToOle(System.Drawing.Color c)
    pslILEmit->EmitCALL(pslILEmit->GetToken(pConvertMD), 1, 1);
    EmitStoreNativeValue(pslILEmit);
}

void ILOleColorMarshaler::EmitConvertContentsNativeToCLR(ILCodeStream* pslILEmit)
{
    STANDARD_VM_CONTRACT;

    LoaderAllocator* pLoader = m_pargs->m_pMarshalInfo->GetModule()->GetLoaderAllocator();
    MethodDesc* pConvertMD = pLoader->GetMarshalingData()->GetOleColorMarshalingInfo()->GetOleColorToSystemColorMD();

    EmitLoadNativeValue(pslILEmit);
    // System.Drawing.Color System.Drawing.ColorTranslator.FromOle(int oleColor)
    pslILEmit->EmitCALL(pslILEmit->GetToken(pConvertMD), 1, 1);
    EmitStoreManagedValue(pslILEmit);
}

bool ILVBByValStrWMarshaler::SupportsArgumentMarshal(DWORD dwMarshalFlags, UINT* pErrorResID)
{
    LIMITED_METHOD_CONTRACT;
    if (IsCLRToNative(dwMarshalFlags) && IsByref(dwMarshalFlags) && IsIn(dwMarshalFlags) && IsOut(dwMarshalFlags))
    {
        return true;
    }

    *pErrorResID = IDS_EE_BADMARSHAL_VBBYVALSTRRESTRICTION;
    return false;
}

bool ILVBByValStrWMarshaler::SupportsReturnMarshal(DWORD dwMarshalFlags, UINT* pErrorResID)
{
    LIMITED_METHOD_CONTRACT;
    *pErrorResID = IDS_EE_BADMARSHAL_VBBYVALSTRRESTRICTION;
    return false;
}

LocalDesc ILVBByValStrWMarshaler::GetNativeType()
{
    LIMITED_METHOD_CONTRACT;

    return LocalDesc(ELEMENT_TYPE_I); // BSTR
}

LocalDesc ILVBByValStrWMarshaler::GetManagedType()
{
    LIMITED_METHOD_CONTRACT;

    return LocalDesc(ELEMENT_TYPE_STRING);
}

bool ILVBByValStrWMarshaler::IsNativePassedByRef()
{
    LIMITED_METHOD_CONTRACT;
    return false;
}

void ILVBByValStrWMarshaler::EmitSetupArgumentForMarshalling(ILCodeStream* pslILEmit)
{
    m_dwLocalBuffer = pslILEmit->NewLocal(ELEMENT_TYPE_I);
    pslILEmit->EmitLoadNullPtr();
    pslILEmit->EmitSTLOC(m_dwLocalBuffer);
}

void ILVBByValStrWMarshaler::EmitConvertContentsCLRToNative(ILCodeStream* pslILEmit)
{
    STANDARD_VM_CONTRACT;

    ILCodeLabel* pNullRefLabel = pslILEmit->NewCodeLabel();
    m_dwCCHLocal = pslILEmit->NewLocal(ELEMENT_TYPE_I4);
    DWORD dwNumBytesLocal = pslILEmit->NewLocal(ELEMENT_TYPE_I4);

    pslILEmit->EmitLoadNullPtr();
    EmitStoreNativeValue(pslILEmit);

    EmitLoadManagedValue(pslILEmit);
    pslILEmit->EmitBRFALSE(pNullRefLabel);

    EmitLoadManagedValue(pslILEmit);
    pslILEmit->EmitCALL(METHOD__STRING__GET_LENGTH, 1, 1);
    pslILEmit->EmitDUP();
    pslILEmit->EmitSTLOC(m_dwCCHLocal);

    // cch

    pslILEmit->EmitLDC(1);
    pslILEmit->EmitADD();
    pslILEmit->EmitDUP();
    pslILEmit->EmitCALL(METHOD__STUBHELPERS__CHECK_STRING_LENGTH, 1, 0);
    pslILEmit->EmitDUP();
    pslILEmit->EmitADD();           // (length+1) * sizeof(WCHAR)
    pslILEmit->EmitDUP();
    pslILEmit->EmitSTLOC(dwNumBytesLocal);      // len <- doesn't include size of the DWORD preceding the string
    pslILEmit->EmitLDC(sizeof(DWORD));
    pslILEmit->EmitADD();           // (length+1) * sizeof(WCHAR) + sizeof(DWORD)

    // cb

    ILCodeLabel* pNoOptimizeLabel = pslILEmit->NewCodeLabel();
    ILCodeLabel* pAllocRejoinLabel = pslILEmit->NewCodeLabel();
    pslILEmit->EmitDUP();
    pslILEmit->EmitLDC(MAX_LOCAL_BUFFER_LENGTH);
    pslILEmit->EmitCGT_UN();
    pslILEmit->EmitBRTRUE(pNoOptimizeLabel);

    pslILEmit->EmitLOCALLOC();
    pslILEmit->EmitBR(pAllocRejoinLabel);

    pslILEmit->EmitLabel(pNoOptimizeLabel);
    pslILEmit->EmitCALL(METHOD__MARSHAL__ALLOC_CO_TASK_MEM, 1, 1);
    pslILEmit->EmitDUP();
    pslILEmit->EmitSTLOC(m_dwLocalBuffer);

    pslILEmit->EmitLabel(pAllocRejoinLabel);
    pslILEmit->EmitDUP();
    pslILEmit->EmitLDLOC(m_dwCCHLocal);
    pslILEmit->EmitSTIND_I4();
    pslILEmit->EmitLDC(sizeof(DWORD));
    pslILEmit->EmitADD();
    EmitStoreNativeValue(pslILEmit);

    // <empty>

    EmitLoadManagedValue(pslILEmit);        // src
    EmitLoadNativeValue(pslILEmit);         // dest
    pslILEmit->EmitLDLOC(dwNumBytesLocal);  // len

    // static void System.String.InternalCopy(String src, IntPtr dest,int len)
    pslILEmit->EmitCALL(METHOD__STRING__INTERNAL_COPY, 3, 0);

    pslILEmit->EmitLabel(pNullRefLabel);
}

void ILVBByValStrWMarshaler::EmitConvertContentsNativeToCLR(ILCodeStream* pslILEmit)
{
    STANDARD_VM_CONTRACT;

    ILCodeLabel* pNullRefLabel = pslILEmit->NewCodeLabel();
    EmitLoadNativeValue(pslILEmit);
    pslILEmit->EmitBRFALSE(pNullRefLabel);

    EmitLoadNativeValue(pslILEmit);     // ptr
    pslILEmit->EmitLDC(0);              // startIndex
    pslILEmit->EmitLDLOC(m_dwCCHLocal); // length

    // String CtorCharPtrStartLength(char *ptr, int startIndex, int length)
    // TODO Phase5: Why do we call this weirdo?
    pslILEmit->EmitCALL(METHOD__STRING__CTORF_CHARPTR_START_LEN, 3, 1);

    EmitStoreManagedValue(pslILEmit);
    pslILEmit->EmitLabel(pNullRefLabel);
}


bool ILVBByValStrWMarshaler::NeedsClearNative()
{
    LIMITED_METHOD_CONTRACT;
    return true;
}

void ILVBByValStrWMarshaler::EmitClearNative(ILCodeStream* pslILEmit)
{
    STANDARD_VM_CONTRACT;

    ILCodeLabel* pExitLabel = pslILEmit->NewCodeLabel();
    pslILEmit->EmitLDLOC(m_dwLocalBuffer);
    pslILEmit->EmitBRFALSE(pExitLabel);
    pslILEmit->EmitLDLOC(m_dwLocalBuffer);
    pslILEmit->EmitCALL(METHOD__MARSHAL__FREE_CO_TASK_MEM, 1, 0);
    pslILEmit->EmitLabel(pExitLabel);
}


bool ILVBByValStrMarshaler::SupportsArgumentMarshal(DWORD dwMarshalFlags, UINT* pErrorResID)
{
    if (IsCLRToNative(dwMarshalFlags) && IsByref(dwMarshalFlags) && IsIn(dwMarshalFlags) && IsOut(dwMarshalFlags))
    {
        return true;
    }

    *pErrorResID = IDS_EE_BADMARSHAL_VBBYVALSTRRESTRICTION;
    return false;
}

bool ILVBByValStrMarshaler::SupportsReturnMarshal(DWORD dwMarshalFlags, UINT* pErrorResID)
{
    *pErrorResID = IDS_EE_BADMARSHAL_VBBYVALSTRRESTRICTION;
    return false;
}

LocalDesc ILVBByValStrMarshaler::GetNativeType()
{
    LIMITED_METHOD_CONTRACT;

    return LocalDesc(ELEMENT_TYPE_I); // BSTR
}

LocalDesc ILVBByValStrMarshaler::GetManagedType()
{
    LIMITED_METHOD_CONTRACT;

    return LocalDesc(ELEMENT_TYPE_STRING);
}

bool ILVBByValStrMarshaler::IsNativePassedByRef()
{
    LIMITED_METHOD_CONTRACT;
    return false;
}

void ILVBByValStrMarshaler::EmitConvertContentsCLRToNative(ILCodeStream* pslILEmit)
{
    STANDARD_VM_CONTRACT;

    m_dwCCHLocal = pslILEmit->NewLocal(ELEMENT_TYPE_I4);

    EmitLoadManagedValue(pslILEmit);
    pslILEmit->EmitLDC(m_pargs->m_pMarshalInfo->GetBestFitMapping());
    pslILEmit->EmitLDC(m_pargs->m_pMarshalInfo->GetThrowOnUnmappableChar());
    pslILEmit->EmitLDLOCA(m_dwCCHLocal);

    // static IntPtr ConvertToNative(string strManaged, bool fBestFit, bool fThrowOnUnmappableChar, ref int cch)
    pslILEmit->EmitCALL(METHOD__VBBYVALSTRMARSHALER__CONVERT_TO_NATIVE, 4, 1);

    EmitStoreNativeValue(pslILEmit);
}

void ILVBByValStrMarshaler::EmitConvertContentsNativeToCLR(ILCodeStream* pslILEmit)
{
    STANDARD_VM_CONTRACT;

    EmitLoadNativeValue(pslILEmit);     // pNative
    pslILEmit->EmitLDLOC(m_dwCCHLocal); // cch

    // static string ConvertToManaged(IntPtr pNative, int cch)
    pslILEmit->EmitCALL(METHOD__VBBYVALSTRMARSHALER__CONVERT_TO_MANAGED, 2, 1);

    EmitStoreManagedValue(pslILEmit);
}

bool ILVBByValStrMarshaler::NeedsClearNative()
{
    LIMITED_METHOD_CONTRACT;
    return true;
}

void ILVBByValStrMarshaler::EmitClearNative(ILCodeStream* pslILEmit)
{
    STANDARD_VM_CONTRACT;

    EmitLoadNativeValue(pslILEmit);     // pNative

    // static void ClearNative(IntPtr pNative);
    pslILEmit->EmitCALL(METHOD__VBBYVALSTRMARSHALER__CLEAR_NATIVE, 1, 0);
}
#endif // FEATURE_COMINTEROP

LocalDesc ILBSTRMarshaler::GetManagedType()
{
    LIMITED_METHOD_CONTRACT;

    return LocalDesc(ELEMENT_TYPE_STRING);
}

void ILBSTRMarshaler::EmitConvertContentsCLRToNative(ILCodeStream* pslILEmit)
{
    STANDARD_VM_CONTRACT;

    ILCodeLabel* pRejoinLabel = pslILEmit->NewCodeLabel();

    EmitLoadManagedValue(pslILEmit);

    if (IsCLRToNative(m_dwMarshalFlags) && !IsByref(m_dwMarshalFlags) && !IsFieldMarshal(m_dwMarshalFlags))
    {
        ILCodeLabel *pNoOptimizeLabel = pslILEmit->NewCodeLabel();
        m_dwLocalBuffer = pslILEmit->NewLocal(ELEMENT_TYPE_I);

        // LocalBuffer = 0
        pslILEmit->EmitLoadNullPtr();
        pslILEmit->EmitSTLOC(m_dwLocalBuffer);

        pslILEmit->EmitDUP();
        pslILEmit->EmitBRFALSE(pNoOptimizeLabel);

        // String.Length
        pslILEmit->EmitDUP();
        pslILEmit->EmitCALL(METHOD__STRING__GET_LENGTH, 1, 1);

        // if (length > (MAX_LOCAL_BUFFER_LENGTH - 6) / 2) goto NoOptimize
        pslILEmit->EmitLDC((MAX_LOCAL_BUFFER_LENGTH - 6) / 2); // number of Unicode characters - terminator - length dword
        pslILEmit->EmitCGT_UN();
        pslILEmit->EmitBRTRUE(pNoOptimizeLabel);

        // LocalBuffer = localloc[(String.Length * 2) + 6]
        pslILEmit->EmitCALL(METHOD__STRING__GET_LENGTH, 1, 1);
        pslILEmit->EmitLDC(2);
        pslILEmit->EmitMUL();
        pslILEmit->EmitLDC(7); // + length (4B) + terminator (2B) + possible trailing byte (1B)
        pslILEmit->EmitADD();

#ifdef _DEBUG
        // Save the buffer length
        DWORD dwTmpAllocSize = pslILEmit->NewLocal(ELEMENT_TYPE_I4);
        pslILEmit->EmitDUP();
        pslILEmit->EmitSTLOC(dwTmpAllocSize);
#endif // _DEBUG

        pslILEmit->EmitLOCALLOC();

#ifdef _DEBUG
        // Pass buffer length in the first DWORD so the helper is able to assert that
        // the buffer is large enough.
        pslILEmit->EmitDUP();
        pslILEmit->EmitLDLOC(dwTmpAllocSize);
        pslILEmit->EmitSTIND_I4();
#endif // _DEBUG

        pslILEmit->EmitSTLOC(m_dwLocalBuffer);

        // load string and LocalBuffer
        EmitLoadManagedValue(pslILEmit);
        pslILEmit->EmitLDLOC(m_dwLocalBuffer);
        pslILEmit->EmitBR(pRejoinLabel);

        pslILEmit->EmitLabel(pNoOptimizeLabel);
    }
    pslILEmit->EmitLoadNullPtr();

    pslILEmit->EmitLabel(pRejoinLabel);
    pslILEmit->EmitCALL(METHOD__BSTRMARSHALER__CONVERT_TO_NATIVE, 2, 1);
    EmitStoreNativeValue(pslILEmit);
}

void ILBSTRMarshaler::EmitConvertContentsNativeToCLR(ILCodeStream* pslILEmit)
{
    STANDARD_VM_CONTRACT;

    EmitLoadNativeValue(pslILEmit);
    pslILEmit->EmitCALL(METHOD__BSTRMARSHALER__CONVERT_TO_MANAGED, 1, 1);
    EmitStoreManagedValue(pslILEmit);
}

LocalDesc ILAnsiBSTRMarshaler::GetNativeType()
{
    LIMITED_METHOD_CONTRACT;

    return LocalDesc(ELEMENT_TYPE_I); // BSTR
}

LocalDesc ILAnsiBSTRMarshaler::GetManagedType()
{
    LIMITED_METHOD_CONTRACT;

    return LocalDesc(ELEMENT_TYPE_STRING);
}

void ILAnsiBSTRMarshaler::EmitConvertContentsCLRToNative(ILCodeStream* pslILEmit)
{
    STANDARD_VM_CONTRACT;

    DWORD dwAnsiMarshalFlags =
        (m_pargs->m_pMarshalInfo->GetBestFitMapping() & 0xFF) |
        (m_pargs->m_pMarshalInfo->GetThrowOnUnmappableChar() << 8);

    pslILEmit->EmitLDC(dwAnsiMarshalFlags);
    EmitLoadManagedValue(pslILEmit);
    pslILEmit->EmitCALL(METHOD__ANSIBSTRMARSHALER__CONVERT_TO_NATIVE, 2, 1);
    EmitStoreNativeValue(pslILEmit);
}

void ILAnsiBSTRMarshaler::EmitConvertContentsNativeToCLR(ILCodeStream* pslILEmit)
{
    STANDARD_VM_CONTRACT;

    EmitLoadNativeValue(pslILEmit);
    pslILEmit->EmitCALL(METHOD__ANSIBSTRMARSHALER__CONVERT_TO_MANAGED, 1, 1);
    EmitStoreManagedValue(pslILEmit);
}

bool ILAnsiBSTRMarshaler::NeedsClearNative()
{
    LIMITED_METHOD_CONTRACT;
    return true;
}

void ILAnsiBSTRMarshaler::EmitClearNative(ILCodeStream* pslILEmit)
{
    STANDARD_VM_CONTRACT;

    EmitLoadNativeValue(pslILEmit);
    pslILEmit->EmitCALL(METHOD__ANSIBSTRMARSHALER__CLEAR_NATIVE, 1, 0);
}

void ILFixedWSTRMarshaler::EmitConvertContentsCLRToNative(ILCodeStream* pslILEmit)
{
    STANDARD_VM_CONTRACT;

    ILCodeLabel* pNullRefLabel = pslILEmit->NewCodeLabel();
    ILCodeLabel* pFinishedLabel = pslILEmit->NewCodeLabel();

    EmitLoadManagedValue(pslILEmit);
    EmitLoadNativeHomeAddr(pslILEmit);
    pslILEmit->EmitLDC(m_pargs->fs.fixedStringLength);
    pslILEmit->EmitCALL(METHOD__FIXEDWSTRMARSHALER__CONVERT_TO_NATIVE, 3, 0);
}

void ILFixedWSTRMarshaler::EmitConvertContentsNativeToCLR(ILCodeStream* pslILEmit)
{
    STANDARD_VM_CONTRACT;

    EmitLoadNativeHomeAddr(pslILEmit);
    pslILEmit->EmitLDC(m_pargs->fs.fixedStringLength);
    pslILEmit->EmitCALL(METHOD__FIXEDWSTRMARSHALER__CONVERT_TO_MANAGED, 2, 1);
    EmitStoreManagedValue(pslILEmit);
}

void ILFixedCSTRMarshaler::EmitConvertContentsCLRToNative(ILCodeStream* pslILEmit)
{
    STANDARD_VM_CONTRACT;

    DWORD dwAnsiMarshalFlags =
        (m_pargs->m_pMarshalInfo->GetBestFitMapping() & 0xFF) |
        (m_pargs->m_pMarshalInfo->GetThrowOnUnmappableChar() << 8);

    // CSTRMarshaler.ConvertFixedToNative dwAnsiMarshalFlags, pManaged, homeAddr, fixedLength
    pslILEmit->EmitLDC(dwAnsiMarshalFlags);
    EmitLoadManagedValue(pslILEmit);
    EmitLoadNativeHomeAddr(pslILEmit);
    pslILEmit->EmitLDC(m_pargs->fs.fixedStringLength);
    pslILEmit->EmitCALL(METHOD__CSTRMARSHALER__CONVERT_FIXED_TO_NATIVE, 4, 0);
}

void ILFixedCSTRMarshaler::EmitConvertContentsNativeToCLR(ILCodeStream* pslILEmit)
{
    STANDARD_VM_CONTRACT;

    EmitLoadNativeHomeAddr(pslILEmit);
    pslILEmit->EmitLDC(m_pargs->fs.fixedStringLength);
    pslILEmit->EmitCALL(METHOD__CSTRMARSHALER__CONVERT_FIXED_TO_MANAGED, 2, 1);
    EmitStoreManagedValue(pslILEmit);
}

LocalDesc ILCUTF8Marshaler::GetManagedType()
{
    LIMITED_METHOD_CONTRACT;
    return LocalDesc(ELEMENT_TYPE_STRING);
}

LocalDesc ILCUTF8Marshaler::GetNativeType()
{
    LIMITED_METHOD_CONTRACT;
    return LocalDesc(ELEMENT_TYPE_I);
}

bool ILCUTF8Marshaler::NeedsClearNative()
{
    LIMITED_METHOD_CONTRACT;
    return true;
}

void ILCUTF8Marshaler::EmitConvertContentsCLRToNative(ILCodeStream* pslILEmit)
{
    STANDARD_VM_CONTRACT;

    bool bPassByValueInOnly = IsIn(m_dwMarshalFlags) && !IsOut(m_dwMarshalFlags) && !IsByref(m_dwMarshalFlags);
    if (bPassByValueInOnly)
    {
        if (m_dwInstance == LOCAL_NUM_UNUSED)
            m_dwInstance = pslILEmit->NewLocal(LocalDesc(CoreLibBinder::GetClass(CLASS__UTF8STRINGMARSHALLER_IN)));

        DWORD dwBuffer = pslILEmit->NewLocal(ELEMENT_TYPE_I);
        pslILEmit->EmitLDC(LOCAL_BUFFER_LENGTH);
        pslILEmit->EmitLOCALLOC();
        pslILEmit->EmitSTLOC(dwBuffer);

        // Load the marshaller instance.
        pslILEmit->EmitLDLOCA(m_dwInstance);

        // Argument 1
        EmitLoadManagedValue(pslILEmit);

        // Argument 2
        // Create ReadOnlySpan<byte> from the stack-allocated buffer
        pslILEmit->EmitLDLOC(dwBuffer);
        pslILEmit->EmitLDC(LOCAL_BUFFER_LENGTH);
        TypeHandle thByte = CoreLibBinder::GetClass(CLASS__BYTE);
        MethodDesc* pSpanCtor = MethodDesc::FindOrCreateAssociatedMethodDesc(CoreLibBinder::GetMethod(METHOD__SPAN__CTOR_PTR_INT),
            TypeHandle(CoreLibBinder::GetClass(CLASS__SPAN)).Instantiate(Instantiation(&thByte, 1)).AsMethodTable(),
            FALSE, Instantiation(), FALSE);
        pslILEmit->EmitNEWOBJ(pslILEmit->GetToken(pSpanCtor), 2);
        pslILEmit->EmitCALL(METHOD__UTF8STRINGMARSHALLER_IN__FROM_MANAGED, 2, 0);

        pslILEmit->EmitLDLOCA(m_dwInstance);
        pslILEmit->EmitCALL(METHOD__UTF8STRINGMARSHALLER_IN__TO_UNMANAGED, 1, 1);
    }
    else
    {
        EmitLoadManagedValue(pslILEmit);
        pslILEmit->EmitCALL(METHOD__UTF8STRINGMARSHALLER__CONVERT_TO_UNMANAGED, 1, 1);
    }

    EmitStoreNativeValue(pslILEmit);
}

void ILCUTF8Marshaler::EmitConvertContentsNativeToCLR(ILCodeStream* pslILEmit)
{
    STANDARD_VM_CONTRACT;

    EmitLoadNativeValue(pslILEmit);
    pslILEmit->EmitCALL(METHOD__UTF8STRINGMARSHALLER__CONVERT_TO_MANAGED, 1, 1);
    EmitStoreManagedValue(pslILEmit);
}

void ILCUTF8Marshaler::EmitClearNative(ILCodeStream* pslILEmit)
{
    STANDARD_VM_CONTRACT;

    if (m_dwInstance != LOCAL_NUM_UNUSED)
    {
        pslILEmit->EmitLDLOCA(m_dwInstance);
        pslILEmit->EmitCALL(METHOD__UTF8STRINGMARSHALLER_IN__FREE, 0, 0);
    }
    else
    {
        EmitLoadNativeValue(pslILEmit);
        pslILEmit->EmitCALL(METHOD__UTF8STRINGMARSHALLER__FREE, 1, 0);
    }
}

LocalDesc ILCSTRMarshaler::GetManagedType()
{
    LIMITED_METHOD_CONTRACT;

    return LocalDesc(ELEMENT_TYPE_STRING);
}

void ILCSTRMarshaler::EmitConvertContentsCLRToNative(ILCodeStream* pslILEmit)
{
    STANDARD_VM_CONTRACT;

    DWORD dwAnsiMarshalFlags =
        (m_pargs->m_pMarshalInfo->GetBestFitMapping() & 0xFF) |
        (m_pargs->m_pMarshalInfo->GetThrowOnUnmappableChar() << 8);

    bool bPassByValueInOnly = IsIn(m_dwMarshalFlags) && !IsOut(m_dwMarshalFlags) && !IsByref(m_dwMarshalFlags);
    if (bPassByValueInOnly)
    {
        DWORD dwBufSize = pslILEmit->NewLocal(ELEMENT_TYPE_I4);
        m_dwLocalBuffer = pslILEmit->NewLocal(ELEMENT_TYPE_I);

        // LocalBuffer = 0
        pslILEmit->EmitLoadNullPtr();
        pslILEmit->EmitSTLOC(m_dwLocalBuffer);

        ILCodeLabel* pNoOptimize = pslILEmit->NewCodeLabel();

        // if == NULL, goto NoOptimize
        EmitLoadManagedValue(pslILEmit);
        pslILEmit->EmitBRFALSE(pNoOptimize);

        // String.Length + 2
        EmitLoadManagedValue(pslILEmit);
        pslILEmit->EmitCALL(METHOD__STRING__GET_LENGTH, 1, 1);
        pslILEmit->EmitLDC(2);
        pslILEmit->EmitADD();

        // (String.Length + 2) * GetMaxDBCSCharByteSize()
        pslILEmit->EmitLDSFLD(pslILEmit->GetToken(CoreLibBinder::GetField(FIELD__MARSHAL__SYSTEM_MAX_DBCS_CHAR_SIZE)));
        pslILEmit->EmitMUL_OVF();

        // BufSize = (String.Length + 2) * GetMaxDBCSCharByteSize()
        pslILEmit->EmitSTLOC(dwBufSize);

        // if (MAX_LOCAL_BUFFER_LENGTH < BufSize ) goto NoOptimize
        pslILEmit->EmitLDC(MAX_LOCAL_BUFFER_LENGTH);
        pslILEmit->EmitLDLOC(dwBufSize);
        pslILEmit->EmitCLT();
        pslILEmit->EmitBRTRUE(pNoOptimize);

        // LocalBuffer = localloc(BufSize);
        pslILEmit->EmitLDLOC(dwBufSize);
        pslILEmit->EmitLOCALLOC();
        pslILEmit->EmitSTLOC(m_dwLocalBuffer);

        // NoOptimize:
        pslILEmit->EmitLabel(pNoOptimize);
    }

    // CSTRMarshaler.ConvertToNative pManaged, dwAnsiMarshalFlags, pLocalBuffer
    pslILEmit->EmitLDC(dwAnsiMarshalFlags);
    EmitLoadManagedValue(pslILEmit);

    if (m_dwLocalBuffer != LOCAL_NUM_UNUSED)
    {
        pslILEmit->EmitLDLOC(m_dwLocalBuffer);
    }
    else
    {
        pslILEmit->EmitLoadNullPtr();
    }

    pslILEmit->EmitCALL(METHOD__CSTRMARSHALER__CONVERT_TO_NATIVE, 3, 1);

    EmitStoreNativeValue(pslILEmit);
}

void ILCSTRMarshaler::EmitConvertContentsNativeToCLR(ILCodeStream* pslILEmit)
{
    STANDARD_VM_CONTRACT;

    EmitLoadNativeValue(pslILEmit);
    pslILEmit->EmitCALL(METHOD__CSTRMARSHALER__CONVERT_TO_MANAGED, 1, 1);
    EmitStoreManagedValue(pslILEmit);
}

LocalDesc ILLayoutClassPtrMarshalerBase::GetNativeType()
{
    LIMITED_METHOD_CONTRACT;

    return LocalDesc(ELEMENT_TYPE_I); // ptr to struct
}

LocalDesc ILLayoutClassPtrMarshalerBase::GetManagedType()
{
    LIMITED_METHOD_CONTRACT;

    return LocalDesc(m_pargs->m_pMT);
}

void ILLayoutClassPtrMarshalerBase::EmitConvertSpaceCLRToNative(ILCodeStream* pslILEmit)
{
    STANDARD_VM_CONTRACT;

    ILCodeLabel* pNullRefLabel = pslILEmit->NewCodeLabel();
    UINT uNativeSize = m_pargs->m_pMT->GetNativeSize();

    pslILEmit->EmitLoadNullPtr();
    EmitStoreNativeValue(pslILEmit);

    EmitLoadManagedValue(pslILEmit);
    pslILEmit->EmitBRFALSE(pNullRefLabel);

    ILCodeLabel* pTypeMismatchedLabel = pslILEmit->NewCodeLabel();
    bool emittedTypeCheck = EmitExactTypeCheck(pslILEmit, pTypeMismatchedLabel);
    DWORD sizeLocal = pslILEmit->NewLocal(LocalDesc(ELEMENT_TYPE_I4));

    pslILEmit->EmitLDC(uNativeSize);
    if (emittedTypeCheck)
    {
        ILCodeLabel* pHaveSizeLabel = pslILEmit->NewCodeLabel();
        pslILEmit->EmitBR(pHaveSizeLabel);
        pslILEmit->EmitLabel(pTypeMismatchedLabel);
        EmitLoadManagedValue(pslILEmit);
        pslILEmit->EmitCALL(METHOD__OBJECT__GET_TYPE, 1, 1);
        pslILEmit->EmitCALL(METHOD__MARSHAL__SIZEOF_TYPE, 1, 1);
        pslILEmit->EmitLabel(pHaveSizeLabel);
    }
    pslILEmit->EmitSTLOC(sizeLocal);
    pslILEmit->EmitLDLOC(sizeLocal);
    pslILEmit->EmitCALL(METHOD__MARSHAL__ALLOC_CO_TASK_MEM, 1, 1);
    pslILEmit->EmitDUP();           // for INITBLK
    EmitStoreNativeValue(pslILEmit);

    // initialize local block we just allocated
    pslILEmit->EmitLDC(0);
    pslILEmit->EmitLDLOC(sizeLocal);
    pslILEmit->EmitINITBLK();

    pslILEmit->EmitLabel(pNullRefLabel);
}

void ILLayoutClassPtrMarshalerBase::EmitConvertSpaceCLRToNativeTemp(ILCodeStream* pslILEmit)
{
    STANDARD_VM_CONTRACT;

    UINT uNativeSize = m_pargs->m_pMT->GetNativeSize();
    if (uNativeSize > s_cbStackAllocThreshold)
    {
        EmitConvertSpaceCLRToNative(pslILEmit);
    }
    else
    {
        ILCodeLabel* pNullRefLabel = pslILEmit->NewCodeLabel();

        pslILEmit->EmitLoadNullPtr();
        EmitStoreNativeValue(pslILEmit);

        EmitLoadManagedValue(pslILEmit);
        pslILEmit->EmitBRFALSE(pNullRefLabel);

        ILCodeLabel* pTypeMismatchedLabel = pslILEmit->NewCodeLabel();
        bool emittedTypeCheck = EmitExactTypeCheck(pslILEmit, pTypeMismatchedLabel);
        DWORD sizeLocal = pslILEmit->NewLocal(LocalDesc(ELEMENT_TYPE_I4));

        pslILEmit->EmitLDC(uNativeSize);
        if (emittedTypeCheck)
        {
            ILCodeLabel* pHaveSizeLabel = pslILEmit->NewCodeLabel();
            pslILEmit->EmitBR(pHaveSizeLabel);
            pslILEmit->EmitLabel(pTypeMismatchedLabel);
            EmitLoadManagedValue(pslILEmit);
            pslILEmit->EmitCALL(METHOD__OBJECT__GET_TYPE, 1, 1);
            pslILEmit->EmitCALL(METHOD__MARSHAL__SIZEOF_TYPE, 1, 1);
            pslILEmit->EmitLabel(pHaveSizeLabel);
        }
        pslILEmit->EmitSTLOC(sizeLocal);
        pslILEmit->EmitLDLOC(sizeLocal);

        pslILEmit->EmitLOCALLOC();
        pslILEmit->EmitDUP();           // for INITBLK
        EmitStoreNativeValue(pslILEmit);

        // initialize local block we just allocated
        pslILEmit->EmitLDC(0);
        pslILEmit->EmitLDLOC(sizeLocal);
        pslILEmit->EmitINITBLK();

        pslILEmit->EmitLabel(pNullRefLabel);
    }
}

void ILLayoutClassPtrMarshalerBase::EmitConvertSpaceAndContentsCLRToNativeTemp(ILCodeStream* pslILEmit)
{
    STANDARD_VM_CONTRACT;

    EmitConvertSpaceCLRToNativeTemp(pslILEmit);
    EmitConvertContentsCLRToNative(pslILEmit);
}

void ILLayoutClassPtrMarshalerBase::EmitConvertSpaceNativeToCLR(ILCodeStream* pslILEmit)
{
    STANDARD_VM_CONTRACT;

    ILCodeLabel* pNullRefLabel = pslILEmit->NewCodeLabel();

    EmitLoadNativeValue(pslILEmit);
    pslILEmit->EmitBRFALSE(pNullRefLabel);

    pslILEmit->EmitLDTOKEN(pslILEmit->GetToken(m_pargs->m_pMT));
    pslILEmit->EmitCALL(METHOD__RT_TYPE_HANDLE__TO_INTPTR, 1, 1);
    // static object AllocateInternal(IntPtr typeHandle);
    pslILEmit->EmitCALL(METHOD__STUBHELPERS__ALLOCATE_INTERNAL, 1, 1);
    EmitStoreManagedValue(pslILEmit);
    pslILEmit->EmitLabel(pNullRefLabel);
}


bool ILLayoutClassPtrMarshalerBase::NeedsClearNative()
{
    LIMITED_METHOD_CONTRACT;
    return true;
}

void ILLayoutClassPtrMarshalerBase::EmitClearNative(ILCodeStream* pslILEmit)
{
    STANDARD_VM_CONTRACT;

    ILCodeLabel* pNullRefLabel = pslILEmit->NewCodeLabel();

    EmitLoadNativeValue(pslILEmit);
    pslILEmit->EmitBRFALSE(pNullRefLabel);

    EmitClearNativeContents(pslILEmit);
    EmitLoadNativeValue(pslILEmit);
    pslILEmit->EmitCALL(METHOD__MARSHAL__FREE_CO_TASK_MEM, 1, 0);

    pslILEmit->EmitLabel(pNullRefLabel);
}

void ILLayoutClassPtrMarshalerBase::EmitClearNativeTemp(ILCodeStream* pslILEmit)
{
    STANDARD_VM_CONTRACT;

    UINT uNativeSize = m_pargs->m_pMT->GetNativeSize();
    if (uNativeSize > s_cbStackAllocThreshold)
    {
        EmitClearNative(pslILEmit);
    }
    else
    {
        ILCodeLabel* pNullRefLabel = pslILEmit->NewCodeLabel();
        EmitLoadNativeValue(pslILEmit);
        pslILEmit->EmitBRFALSE(pNullRefLabel);

        EmitClearNativeContents(pslILEmit);

        pslILEmit->EmitLabel(pNullRefLabel);
    }
}

bool ILLayoutClassPtrMarshalerBase::EmitExactTypeCheck(ILCodeStream* pslILEmit, ILCodeLabel* isNotMatchingTypeLabel)
{
    STANDARD_VM_CONTRACT;

    if (m_pargs->m_pMT->IsSealed())
    {
        // If the provided type cannot be derived from, then we don't need to emit the type check.
        return false;
    }
    EmitLoadManagedValue(pslILEmit);
    pslILEmit->EmitCALL(METHOD__OBJECT__GET_TYPE, 1, 1);
    pslILEmit->EmitLDTOKEN(pslILEmit->GetToken(m_pargs->m_pMT));
    pslILEmit->EmitCALL(METHOD__TYPE__GET_TYPE_FROM_HANDLE, 1, 1);
    pslILEmit->EmitCALLVIRT(pslILEmit->GetToken(CoreLibBinder::GetMethod(METHOD__OBJECT__EQUALS)), 1, 1);
    pslILEmit->EmitBRFALSE(isNotMatchingTypeLabel);

    return true;
}

void ILLayoutClassPtrMarshaler::EmitConvertContentsCLRToNative(ILCodeStream* pslILEmit)
{
    STANDARD_VM_CONTRACT;

    ILCodeLabel* pNullRefLabel = pslILEmit->NewCodeLabel();
    UINT uNativeSize = m_pargs->m_pMT->GetNativeSize();

    EmitLoadNativeValue(pslILEmit);
    pslILEmit->EmitBRFALSE(pNullRefLabel);

    EmitLoadNativeValue(pslILEmit);
    pslILEmit->EmitLDC(0);
    pslILEmit->EmitLDC(uNativeSize);
    pslILEmit->EmitINITBLK();

    ILCodeLabel* isNotMatchingTypeLabel = pslILEmit->NewCodeLabel();
    bool emittedTypeCheck = EmitExactTypeCheck(pslILEmit, isNotMatchingTypeLabel);

    MethodDesc* pStructMarshalStub = NDirect::CreateStructMarshalILStub(m_pargs->m_pMT);

    EmitLoadManagedValue(pslILEmit);
    pslILEmit->EmitCALL(METHOD__RUNTIME_HELPERS__GET_RAW_DATA, 1, 1);
    EmitLoadNativeValue(pslILEmit);
    pslILEmit->EmitLDC(StructMarshalStubs::MarshalOperation::Marshal);
    EmitLoadCleanupWorkList(pslILEmit);

    pslILEmit->EmitCALL(pslILEmit->GetToken(pStructMarshalStub), 4, 0);

    if (emittedTypeCheck)
    {
        pslILEmit->EmitBR(pNullRefLabel);

        pslILEmit->EmitLabel(isNotMatchingTypeLabel);
        EmitLoadManagedValue(pslILEmit);
        EmitLoadNativeValue(pslILEmit);
        pslILEmit->EmitLDC(0);
        pslILEmit->EmitCALL(METHOD__MARSHAL__STRUCTURE_TO_PTR, 3, 0);
    }

    pslILEmit->EmitLabel(pNullRefLabel);
}

void ILLayoutClassPtrMarshaler::EmitConvertContentsNativeToCLR(ILCodeStream* pslILEmit)
{
    STANDARD_VM_CONTRACT;

    ILCodeLabel* pNullRefLabel = pslILEmit->NewCodeLabel();

    EmitLoadManagedValue(pslILEmit);
    pslILEmit->EmitBRFALSE(pNullRefLabel);

    ILCodeLabel* isNotMatchingTypeLabel = pslILEmit->NewCodeLabel();
    bool emittedTypeCheck = EmitExactTypeCheck(pslILEmit, isNotMatchingTypeLabel);

    MethodDesc* pStructMarshalStub = NDirect::CreateStructMarshalILStub(m_pargs->m_pMT);

    EmitLoadManagedValue(pslILEmit);
    pslILEmit->EmitCALL(METHOD__RUNTIME_HELPERS__GET_RAW_DATA, 1, 1);
    EmitLoadNativeValue(pslILEmit);
    pslILEmit->EmitLDC(StructMarshalStubs::MarshalOperation::Unmarshal);
    EmitLoadCleanupWorkList(pslILEmit);

    pslILEmit->EmitCALL(pslILEmit->GetToken(pStructMarshalStub), 4, 0);
    if (emittedTypeCheck)
    {
        pslILEmit->EmitBR(pNullRefLabel);

        pslILEmit->EmitLabel(isNotMatchingTypeLabel);
        EmitLoadNativeValue(pslILEmit);
        EmitLoadManagedValue(pslILEmit);
        pslILEmit->EmitCALL(METHOD__MARSHAL__PTR_TO_STRUCTURE, 2, 0);
    }
    pslILEmit->EmitLabel(pNullRefLabel);
}

void ILLayoutClassPtrMarshaler::EmitClearNativeContents(ILCodeStream * pslILEmit)
{
    STANDARD_VM_CONTRACT;

    ILCodeLabel* isNotMatchingTypeLabel = pslILEmit->NewCodeLabel();
    ILCodeLabel* cleanedUpLabel = pslILEmit->NewCodeLabel();
    bool emittedTypeCheck = EmitExactTypeCheck(pslILEmit, isNotMatchingTypeLabel);

    MethodDesc* pStructMarshalStub = NDirect::CreateStructMarshalILStub(m_pargs->m_pMT);

    EmitLoadManagedValue(pslILEmit);
    pslILEmit->EmitCALL(METHOD__RUNTIME_HELPERS__GET_RAW_DATA, 1, 1);
    EmitLoadNativeValue(pslILEmit);
    pslILEmit->EmitLDC(StructMarshalStubs::MarshalOperation::Cleanup);
    EmitLoadCleanupWorkList(pslILEmit);

    pslILEmit->EmitCALL(pslILEmit->GetToken(pStructMarshalStub), 4, 0);

    if (emittedTypeCheck)
    {
        pslILEmit->EmitBR(cleanedUpLabel);

        pslILEmit->EmitLabel(isNotMatchingTypeLabel);
        EmitLoadNativeValue(pslILEmit);
        EmitLoadManagedValue(pslILEmit);
        pslILEmit->EmitCALL(METHOD__OBJECT__GET_TYPE, 1, 1);
        pslILEmit->EmitCALL(METHOD__MARSHAL__DESTROY_STRUCTURE, 2, 0);
    }

    pslILEmit->EmitLabel(cleanedUpLabel);
}


void ILBlittablePtrMarshaler::EmitConvertContentsCLRToNative(ILCodeStream* pslILEmit)
{
    STANDARD_VM_CONTRACT;

    ILCodeLabel* pNullRefLabel = pslILEmit->NewCodeLabel();
    UINT uNativeSize = m_pargs->m_pMT->GetNativeSize();
    int fieldDef = pslILEmit->GetToken(CoreLibBinder::GetField(FIELD__RAW_DATA__DATA));

    EmitLoadNativeValue(pslILEmit);
    pslILEmit->EmitBRFALSE(pNullRefLabel);

    ILCodeLabel* isNotMatchingTypeLabel = pslILEmit->NewCodeLabel();
    bool emittedTypeCheck = EmitExactTypeCheck(pslILEmit, isNotMatchingTypeLabel);

    EmitLoadNativeValue(pslILEmit);                             // dest

    EmitLoadManagedValue(pslILEmit);
    pslILEmit->EmitLDFLDA(fieldDef);                            // src

    pslILEmit->EmitLDC(uNativeSize);                            // size

    pslILEmit->EmitCPBLK();

    if (emittedTypeCheck)
    {
        pslILEmit->EmitBR(pNullRefLabel);

        pslILEmit->EmitLabel(isNotMatchingTypeLabel);
        EmitLoadManagedValue(pslILEmit);
        EmitLoadNativeValue(pslILEmit);
        pslILEmit->EmitLDC(0);
        pslILEmit->EmitCALL(METHOD__MARSHAL__STRUCTURE_TO_PTR, 3, 0);
    }
    pslILEmit->EmitLabel(pNullRefLabel);
}

void ILBlittablePtrMarshaler::EmitConvertContentsNativeToCLR(ILCodeStream* pslILEmit)
{
    STANDARD_VM_CONTRACT;

    ILCodeLabel* pNullRefLabel = pslILEmit->NewCodeLabel();
    UINT uNativeSize = m_pargs->m_pMT->GetNativeSize();
    int fieldDef = pslILEmit->GetToken(CoreLibBinder::GetField(FIELD__RAW_DATA__DATA));

    EmitLoadManagedValue(pslILEmit);
    pslILEmit->EmitBRFALSE(pNullRefLabel);

    ILCodeLabel* isNotMatchingTypeLabel = pslILEmit->NewCodeLabel();
    bool emittedTypeCheck = EmitExactTypeCheck(pslILEmit, isNotMatchingTypeLabel);

    EmitLoadManagedValue(pslILEmit);
    pslILEmit->EmitLDFLDA(fieldDef);                            // dest

    EmitLoadNativeValue(pslILEmit);                             // src

    pslILEmit->EmitLDC(uNativeSize);                            // size

    pslILEmit->EmitCPBLK();

    if (emittedTypeCheck)
    {
        pslILEmit->EmitBR(pNullRefLabel);

        pslILEmit->EmitLabel(isNotMatchingTypeLabel);
        EmitLoadNativeValue(pslILEmit);
        EmitLoadManagedValue(pslILEmit);
        pslILEmit->EmitCALL(METHOD__MARSHAL__PTR_TO_STRUCTURE, 2, 0);
    }

    pslILEmit->EmitLabel(pNullRefLabel);
}

bool ILBlittablePtrMarshaler::CanMarshalViaPinning()
{
    // [COMPAT] For correctness, we can't marshal via pinning if we might need to marshal differently at runtime.
    // See calls to EmitExactTypeCheck where we check the runtime type of the object being marshalled.
    // However, we previously supported pinning non-sealed blittable classes, even though that could result
    // in some data still being unmarshalled if a subclass is provided. This optimization is incorrect,
    // but libraries like NAudio have taken a hard dependency on this incorrect behavior, so we need to preserve it.
    return IsCLRToNative(m_dwMarshalFlags) &&
        !IsByref(m_dwMarshalFlags) &&
        !IsFieldMarshal(m_dwMarshalFlags);
}

void ILBlittablePtrMarshaler::EmitMarshalViaPinning(ILCodeStream* pslILEmit)
{
    STANDARD_VM_CONTRACT;

    ILCodeLabel* pSkipAddLabel = pslILEmit->NewCodeLabel();
    LocalDesc managedTypePinned = GetManagedType();
    managedTypePinned.MakePinned();
    DWORD dwPinnedLocal = pslILEmit->NewLocal(managedTypePinned);

    EmitLoadManagedValue(pslILEmit);

    pslILEmit->EmitSTLOC(dwPinnedLocal);
    pslILEmit->EmitLDLOC(dwPinnedLocal);
    pslILEmit->EmitCONV_U();
    pslILEmit->EmitDUP();
    pslILEmit->EmitBRFALSE(pSkipAddLabel);
    pslILEmit->EmitLDC(Object::GetOffsetOfFirstField());
    pslILEmit->EmitADD();
    pslILEmit->EmitLabel(pSkipAddLabel);

    EmitLogNativeArgument(pslILEmit, dwPinnedLocal);

    EmitStoreNativeValue(pslILEmit);
}

void ILLayoutClassMarshaler::EmitConvertContentsCLRToNative(ILCodeStream* pslILEmit)
{
    STANDARD_VM_CONTRACT;

    ILCodeLabel* pNullRefLabel = pslILEmit->NewCodeLabel();
    UINT uNativeSize = m_pargs->m_pMT->GetNativeSize();


    EmitLoadNativeHomeAddr(pslILEmit);
    pslILEmit->EmitLDC(0);
    pslILEmit->EmitLDC(uNativeSize);
    pslILEmit->EmitINITBLK();

    EmitLoadManagedValue(pslILEmit);
    pslILEmit->EmitBRFALSE(pNullRefLabel);


    MethodDesc* pStructMarshalStub = NDirect::CreateStructMarshalILStub(m_pargs->m_pMT);

    EmitLoadManagedValue(pslILEmit);
    pslILEmit->EmitCALL(METHOD__RUNTIME_HELPERS__GET_RAW_DATA, 1, 1);
    EmitLoadNativeHomeAddr(pslILEmit);
    pslILEmit->EmitLDC(StructMarshalStubs::MarshalOperation::Marshal);
    EmitLoadCleanupWorkList(pslILEmit);

    pslILEmit->EmitCALL(pslILEmit->GetToken(pStructMarshalStub), 4, 0);
    pslILEmit->EmitLabel(pNullRefLabel);
}

void ILLayoutClassMarshaler::EmitConvertSpaceNativeToCLR(ILCodeStream* pslILEmit)
{
    pslILEmit->EmitLDTOKEN(pslILEmit->GetToken(m_pargs->m_pMT));
    pslILEmit->EmitCALL(METHOD__TYPE__GET_TYPE_FROM_HANDLE, 1, 1);
    pslILEmit->EmitCALL(METHOD__RUNTIME_HELPERS__GET_UNINITIALIZED_OBJECT, 1, 1);
    EmitStoreManagedValue(pslILEmit);
}

void ILLayoutClassMarshaler::EmitConvertContentsNativeToCLR(ILCodeStream* pslILEmit)
{
    STANDARD_VM_CONTRACT;

    MethodDesc* pStructMarshalStub = NDirect::CreateStructMarshalILStub(m_pargs->m_pMT);

    EmitLoadManagedValue(pslILEmit);
    pslILEmit->EmitCALL(METHOD__RUNTIME_HELPERS__GET_RAW_DATA, 1, 1);
    EmitLoadNativeHomeAddr(pslILEmit);
    pslILEmit->EmitLDC(StructMarshalStubs::MarshalOperation::Unmarshal);
    EmitLoadCleanupWorkList(pslILEmit);

    pslILEmit->EmitCALL(pslILEmit->GetToken(pStructMarshalStub), 4, 0);
}

void ILLayoutClassMarshaler::EmitClearNativeContents(ILCodeStream* pslILEmit)
{
    STANDARD_VM_CONTRACT;

    MethodDesc* pStructMarshalStub = NDirect::CreateStructMarshalILStub(m_pargs->m_pMT);

    EmitLoadManagedValue(pslILEmit);
    pslILEmit->EmitCALL(METHOD__RUNTIME_HELPERS__GET_RAW_DATA, 1, 1);
    EmitLoadNativeHomeAddr(pslILEmit);
    pslILEmit->EmitLDC(StructMarshalStubs::MarshalOperation::Cleanup);
    EmitLoadCleanupWorkList(pslILEmit);

    pslILEmit->EmitCALL(pslILEmit->GetToken(pStructMarshalStub), 4, 0);
}


void ILBlittableLayoutClassMarshaler::EmitConvertContentsCLRToNative(ILCodeStream* pslILEmit)
{
    STANDARD_VM_CONTRACT;

    ILCodeLabel* pNullRefLabel = pslILEmit->NewCodeLabel();
    UINT uNativeSize = m_pargs->m_pMT->GetNativeSize();
    int fieldDef = pslILEmit->GetToken(CoreLibBinder::GetField(FIELD__RAW_DATA__DATA));

    EmitLoadNativeHomeAddr(pslILEmit);
    pslILEmit->EmitLDC(0);
    pslILEmit->EmitLDC(uNativeSize);
    pslILEmit->EmitINITBLK();

    EmitLoadManagedValue(pslILEmit);
    pslILEmit->EmitBRFALSE(pNullRefLabel);

    EmitLoadNativeHomeAddr(pslILEmit);                             // dest

    EmitLoadManagedValue(pslILEmit);
    pslILEmit->EmitLDFLDA(fieldDef);                            // src

    pslILEmit->EmitLDC(uNativeSize);                            // size

    pslILEmit->EmitCPBLK();
    pslILEmit->EmitLabel(pNullRefLabel);
}

void ILBlittableLayoutClassMarshaler::EmitConvertContentsNativeToCLR(ILCodeStream* pslILEmit)
{
    STANDARD_VM_CONTRACT;

    UINT uNativeSize = m_pargs->m_pMT->GetNativeSize();
    int fieldDef = pslILEmit->GetToken(CoreLibBinder::GetField(FIELD__RAW_DATA__DATA));

    pslILEmit->EmitLDTOKEN(pslILEmit->GetToken(m_pargs->m_pMT));
    pslILEmit->EmitCALL(METHOD__TYPE__GET_TYPE_FROM_HANDLE, 1, 1);
    pslILEmit->EmitCALL(METHOD__RUNTIME_HELPERS__GET_UNINITIALIZED_OBJECT, 1, 1);
    EmitStoreManagedValue(pslILEmit);

    EmitLoadManagedValue(pslILEmit);
    pslILEmit->EmitLDFLDA(fieldDef);                            // dest

    EmitLoadNativeHomeAddr(pslILEmit);                             // src

    pslILEmit->EmitLDC(uNativeSize);                            // size

    pslILEmit->EmitCPBLK();
}

MarshalerOverrideStatus ILHandleRefMarshaler::ArgumentOverride(NDirectStubLinker* psl,
                                                BOOL               byref,
                                                BOOL               fin,
                                                BOOL               fout,
                                                BOOL               fManagedToNative,
                                                OverrideProcArgs*  pargs,
                                                UINT*              pResID,
                                                UINT               argidx,
                                                UINT               nativeStackOffset)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    ILCodeStream* pcsMarshal    = psl->GetMarshalCodeStream();
    ILCodeStream* pcsDispatch   = psl->GetDispatchCodeStream();
    ILCodeStream* pcsUnmarshal  = psl->GetUnmarshalCodeStream();

    if (fManagedToNative && !byref)
    {
        pcsMarshal->SetStubTargetArgType(ELEMENT_TYPE_I);


        // HandleRefs are valuetypes, so pinning is not needed.
        // The argument address is on the stack and will not move.
        mdFieldDef handleField = pcsDispatch->GetToken(CoreLibBinder::GetField(FIELD__HANDLE_REF__HANDLE));
        pcsDispatch->EmitLDARG(argidx);
        pcsDispatch->EmitLDFLD(handleField);

        mdFieldDef wrapperField = pcsUnmarshal->GetToken(CoreLibBinder::GetField(FIELD__HANDLE_REF__WRAPPER));
        pcsUnmarshal->EmitLDARG(argidx);
        pcsUnmarshal->EmitLDFLD(wrapperField);
        pcsUnmarshal->EmitCALL(METHOD__GC__KEEP_ALIVE, 1, 0);

        return OVERRIDDEN;
    }
    else
    {
        *pResID = IDS_EE_BADMARSHAL_HANDLEREFRESTRICTION;
        return DISALLOWED;
    }
}

MarshalerOverrideStatus ILHandleRefMarshaler::ReturnOverride(NDirectStubLinker* psl,
                                              BOOL               fManagedToNative,
                                              BOOL               fHresultSwap,
                                              OverrideProcArgs*  pargs,
                                              UINT*              pResID)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    *pResID = IDS_EE_BADMARSHAL_HANDLEREFRESTRICTION;
    return DISALLOWED;
}

void ILSafeHandleMarshaler::EmitConvertContentsCLRToNative(ILCodeStream* pslILEmit)
{
    _ASSERTE(IsFieldMarshal(m_dwMarshalFlags));

    EmitLoadManagedValue(pslILEmit);
    EmitLoadCleanupWorkList(pslILEmit);
    pslILEmit->EmitCALL(METHOD__HANDLE_MARSHALER__CONVERT_SAFEHANDLE_TO_NATIVE, 2, 1);
    EmitStoreNativeValue(pslILEmit);
}

void ILSafeHandleMarshaler::EmitConvertContentsNativeToCLR(ILCodeStream* pslILEmit)
{
    _ASSERTE(IsFieldMarshal(m_dwMarshalFlags));

    ILCodeLabel* successLabel = pslILEmit->NewCodeLabel();
    ILCodeLabel* failureLabel = pslILEmit->NewCodeLabel();

    EmitLoadManagedValue(pslILEmit);
    pslILEmit->EmitBRFALSE(failureLabel);
    EmitLoadManagedValue(pslILEmit);
    pslILEmit->EmitLDFLD(pslILEmit->GetToken(CoreLibBinder::GetField(FIELD__SAFE_HANDLE__HANDLE)));
    EmitLoadNativeValue(pslILEmit);
    pslILEmit->EmitBEQ(successLabel);
    pslILEmit->EmitLabel(failureLabel);
    pslILEmit->EmitCALL(METHOD__HANDLE_MARSHALER__THROW_SAFEHANDLE_FIELD_CHANGED, 0, 0);
    pslILEmit->EmitLabel(successLabel);
}

MarshalerOverrideStatus ILSafeHandleMarshaler::ArgumentOverride(NDirectStubLinker* psl,
                                                BOOL               byref,
                                                BOOL               fin,
                                                BOOL               fout,
                                                BOOL               fManagedToNative,
                                                OverrideProcArgs*  pargs,
                                                UINT*              pResID,
                                                UINT               argidx,
                                                UINT               nativeStackOffset)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    ILCodeStream* pslIL         = psl->GetMarshalCodeStream();
    ILCodeStream* pslILDispatch = psl->GetDispatchCodeStream();

    if (fManagedToNative)
    {
        pslIL->SetStubTargetArgType(ELEMENT_TYPE_I);
        if (byref)
        {
            // The specific SafeHandle subtype we're dealing with here.
            MethodTable *pHandleType = pargs->m_pMT;

            // Out SafeHandle parameters must not be abstract.
            if (fout && pHandleType->IsAbstract())
            {
                *pResID = IDS_EE_BADMARSHAL_ABSTRACTOUTSAFEHANDLE;
                return DISALLOWED;
            }

            // We rely on the SafeHandle having a default constructor.
            if (!pHandleType->HasDefaultConstructor())
            {
                MAKE_WIDEPTR_FROMUTF8(wzMethodName, COR_CTOR_METHOD_NAME);
                COMPlusThrowNonLocalized(kMissingMethodException, wzMethodName);
            }

            // Grab the token for the native handle field embedded inside the SafeHandle. We'll be using it to direct access the
            // native handle later.
            mdToken tkNativeHandleField = pslIL->GetToken(CoreLibBinder::GetField(FIELD__SAFE_HANDLE__HANDLE));

            // The high level logic (note that the parameter may be in, out or both):
            // 1) If this is an input parameter we need to AddRef the SafeHandle and schedule a Release cleanup item.
            // 2) If this is an output parameter we need to preallocate a SafeHandle to wrap the new native handle value. We
            //    must allocate this before the native call to avoid a failure point when we already have a native resource
            //    allocated. We must allocate a new SafeHandle even if we have one on input since both input and output native
            //    handles need to be tracked and released by a SafeHandle.
            // 3) Initialize a local IntPtr that will be passed to the native call. If we have an input SafeHandle the value
            //    comes from there otherwise we get it from the new SafeHandle (which is guaranteed to be initialized to an
            //    invalid handle value).
            // 4) If this is a out parameter we also store the original handle value (that we just computed above) in a local
            //    variable.
            // 5) After the native call, if this is an output parameter and the handle value we passed to native differs from
            //    the local copy we made then the new handle value is written into the output SafeHandle and that SafeHandle
            //    is propagated back to the caller.

            // Locals:
            DWORD           dwInputHandleLocal     = 0; // The input safe handle (in only)
            DWORD           dwOutputHandleLocal    = 0; // The output safe handle (out only)
            DWORD           dwOldNativeHandleLocal = 0; // The original native handle value for comparison (out only)
            DWORD           dwNativeHandleLocal;    // The input (and possibly updated) native handle value

            if (fin)
            {
                LocalDesc locInputHandle(pHandleType);
                dwInputHandleLocal = pslIL->NewLocal(locInputHandle);
            }
            if (fout)
            {
                LocalDesc locOutputHandle(pHandleType);
                dwOutputHandleLocal = pslIL->NewLocal(locOutputHandle);

                dwOldNativeHandleLocal = pslIL->NewLocal(ELEMENT_TYPE_I);
            }

            dwNativeHandleLocal = pslIL->NewLocal(ELEMENT_TYPE_I);

            // Call StubHelpers.AddToCleanupList to atomically AddRef incoming SafeHandle and schedule a cleanup work item to
            // perform Release after the call. The helper also returns the native handle value to us so take the opportunity
            // to store this in the NativeHandle local we've allocated.
            if (fin)
            {
                pslIL->EmitLDARG(argidx);
                pslIL->EmitLDIND_REF();

                pslIL->EmitSTLOC(dwInputHandleLocal);

                // Release the original input SafeHandle after the call.
                psl->LoadCleanupWorkList(pslIL);
                pslIL->EmitLDLOC(dwInputHandleLocal);

                // This is realiable, i.e. the cleanup will happen if and only if the SH was actually AddRef'ed.
                pslIL->EmitCALL(METHOD__STUBHELPERS__ADD_TO_CLEANUP_LIST_SAFEHANDLE, 2, 1);

                pslIL->EmitSTLOC(dwNativeHandleLocal);

            }

            // For output parameters we need to allocate a new SafeHandle to hold the result.
            if (fout)
            {
                MethodDesc* pMDCtor = pHandleType->GetDefaultConstructor();
                pslIL->EmitNEWOBJ(pslIL->GetToken(pMDCtor), 0);
                pslIL->EmitSTLOC(dwOutputHandleLocal);

                // If we didn't provide an input handle then we initialize the NativeHandle local with the (initially invalid)
                // handle field set up inside the output handle by the constructor.
                if (!fin)
                {
                    pslIL->EmitLDLOC(dwOutputHandleLocal);
                    pslIL->EmitLDFLD(tkNativeHandleField);
                    pslIL->EmitSTLOC(dwNativeHandleLocal);
                }

                // Remember the handle value we start out with so we know whether to back propagate after the native call.
                pslIL->EmitLDLOC(dwNativeHandleLocal);
                pslIL->EmitSTLOC(dwOldNativeHandleLocal);
            }

            // Leave the address of the native handle local as the argument to the native method.
            EmitLoadNativeLocalAddrForByRefDispatch(pslILDispatch, dwNativeHandleLocal);

            // On the output side we only backpropagate the native handle into the output SafeHandle and the output SafeHandle
            // to the caller if the native handle actually changed (otherwise we can end up with two SafeHandles wrapping the
            // same native handle, which is bad).
            if (fout)
            {
                // We will use cleanup stream to avoid leaking the handle on thread abort.
                psl->EmitSetArgMarshalIndex(pslIL, NDirectStubLinker::CLEANUP_INDEX_ARG0_MARSHAL + argidx);

                psl->SetCleanupNeeded();
                ILCodeStream *pslCleanupIL = psl->GetCleanupCodeStream();

                ILCodeLabel *pDoneLabel = pslCleanupIL->NewCodeLabel();

                psl->EmitCheckForArgCleanup(pslCleanupIL,
                                            NDirectStubLinker::CLEANUP_INDEX_ARG0_MARSHAL + argidx,
                                            NDirectStubLinker::BranchIfNotMarshaled,
                                            pDoneLabel);

                // If this is an [in, out] handle check if the native handles have changed. If not we're finished.
                if (fin)
                {
                    pslCleanupIL->EmitLDLOC(dwNativeHandleLocal);
                    pslCleanupIL->EmitLDLOC(dwOldNativeHandleLocal);
                    pslCleanupIL->EmitCEQ();
                    pslCleanupIL->EmitBRTRUE(pDoneLabel);
                }

                // Propagate the native handle into the output SafeHandle.
                pslCleanupIL->EmitLDLOC(dwOutputHandleLocal);
                pslCleanupIL->EmitLDLOC(dwNativeHandleLocal);
                pslCleanupIL->EmitSTFLD(tkNativeHandleField);

                // Propagate the output SafeHandle back to the caller.
                pslCleanupIL->EmitLDARG(argidx);
                pslCleanupIL->EmitLDLOC(dwOutputHandleLocal);
                pslCleanupIL->EmitSTIND_REF();

                pslCleanupIL->EmitLabel(pDoneLabel);
            }
        }
        else
        {
            // Don't use the CleanupWorkList here.
            // We can't afford allocating for every SafeHandle by-value argument.
            // Instead, duplicate what the CleanupWorkList does as raw IL instructions here.
            psl->SetCleanupNeeded();
            ILCodeStream* pslSetup = psl->GetSetupCodeStream();
            ILCodeStream* pslILCleanup = psl->GetCleanupCodeStream();

            DWORD dwNativeHandle = pslIL->NewLocal(ELEMENT_TYPE_I);
            DWORD dwAddRefd = pslIL->NewLocal(ELEMENT_TYPE_BOOLEAN);

            pslSetup->EmitLDC(0);
            pslSetup->EmitSTLOC(dwAddRefd);

            pslIL->EmitLDARG(argidx);
            pslIL->EmitLDLOCA(dwAddRefd);
            pslIL->EmitCALL(METHOD__STUBHELPERS__SAFE_HANDLE_ADD_REF, 2, 1);
            pslIL->EmitSTLOC(dwNativeHandle);

            pslILDispatch->EmitLDLOC(dwNativeHandle);

            pslILCleanup->EmitLDLOC(dwAddRefd);
            ILCodeLabel* pAfterReleaseLabel = pslILCleanup->NewCodeLabel();
            pslILCleanup->EmitBRFALSE(pAfterReleaseLabel);
            pslILCleanup->EmitLDARG(argidx);
            pslILCleanup->EmitCALL(METHOD__STUBHELPERS__SAFE_HANDLE_RELEASE, 1, 0);
            pslILCleanup->EmitLabel(pAfterReleaseLabel);
        }

        return OVERRIDDEN;
    }
    else
    {
        *pResID = IDS_EE_BADMARSHAL_SAFEHANDLENATIVETOCOM;
        return DISALLOWED;
    }
}

//---------------------------------------------------------------------------------------
//
MarshalerOverrideStatus
ILSafeHandleMarshaler::ReturnOverride(
    NDirectStubLinker * psl,
    BOOL                fManagedToNative,
    BOOL                fHresultSwap,
    OverrideProcArgs *  pargs,
    UINT       *        pResID)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(CheckPointer(psl));
        PRECONDITION(CheckPointer(pargs));
        PRECONDITION(CheckPointer(pResID));
    }
    CONTRACTL_END;

    ILCodeStream * pslIL         = psl->GetMarshalCodeStream();
    ILCodeStream * pslPostIL     = psl->GetReturnUnmarshalCodeStream();
    ILCodeStream * pslILDispatch = psl->GetDispatchCodeStream();

    if (!fManagedToNative)
    {
        *pResID = IDS_EE_BADMARSHAL_RETURNSHCOMTONATIVE;
        return DISALLOWED;
    }

    // Returned SafeHandle parameters must not be abstract.
    if (pargs->m_pMT->IsAbstract())
    {
        *pResID = IDS_EE_BADMARSHAL_ABSTRACTRETSAFEHANDLE;
        return DISALLOWED;
    }

    // 1) create local for new safehandle
    // 2) prealloc a safehandle
    // 3) create local to hold returned handle
    // 4) [byref] add byref IntPtr to native sig
    // 5) [byref] pass address of local as last arg
    // 6) store return value in safehandle

    // 1) create local for new safehandle
    MethodTable * pMT    = pargs->m_pMT;
    LocalDesc     locDescReturnHandle(pMT);
    DWORD         dwReturnHandleLocal;

    dwReturnHandleLocal = pslIL->NewLocal(locDescReturnHandle);

    if (!pMT->HasDefaultConstructor())
    {
        COMPlusThrowNonLocalized(kMissingMethodException, COR_CTOR_METHOD_NAME_W);
    }

    // 2) prealloc a safehandle
    MethodDesc* pMDCtor = pMT->GetDefaultConstructor();
    pslIL->EmitNEWOBJ(pslIL->GetToken(pMDCtor), 0);
    pslIL->EmitSTLOC(dwReturnHandleLocal);

    mdToken tkNativeHandleField = pslPostIL->GetToken(CoreLibBinder::GetField(FIELD__SAFE_HANDLE__HANDLE));

    // 3) create local to hold returned handle
    DWORD dwReturnNativeHandleLocal = pslIL->NewLocal(ELEMENT_TYPE_I);

    if (fHresultSwap)
    {
        // initialize the native handle
        pslIL->EmitLDLOC(dwReturnHandleLocal);
        pslIL->EmitLDFLD(tkNativeHandleField);
        pslIL->EmitSTLOC(dwReturnNativeHandleLocal);

        pslIL->SetStubTargetReturnType(ELEMENT_TYPE_I4);    // native method returns an HRESULT

        // 4) [byref] add byref IntPtr to native sig
        locDescReturnHandle.ElementType[0]  = ELEMENT_TYPE_BYREF;
        locDescReturnHandle.ElementType[1]  = ELEMENT_TYPE_I;
        locDescReturnHandle.cbType          = 2;
        pslIL->SetStubTargetArgType(&locDescReturnHandle, false);   // extra arg is a byref IntPtr

        // 5) [byref] pass address of local as last arg
        EmitLoadNativeLocalAddrForByRefDispatch(pslILDispatch, dwReturnNativeHandleLocal);

        // We will use cleanup stream to avoid leaking the handle on thread abort.
        psl->EmitSetArgMarshalIndex(pslIL, NDirectStubLinker::CLEANUP_INDEX_RETVAL_UNMARSHAL);

        psl->SetCleanupNeeded();
        ILCodeStream *pslCleanupIL = psl->GetCleanupCodeStream();
        ILCodeLabel *pDoneLabel = pslCleanupIL->NewCodeLabel();

        psl->EmitCheckForArgCleanup(pslCleanupIL,
                                    NDirectStubLinker::CLEANUP_INDEX_RETVAL_UNMARSHAL,
                                    NDirectStubLinker::BranchIfNotMarshaled,
                                    pDoneLabel);

        // 6) store return value in safehandle
        pslCleanupIL->EmitLDLOC(dwReturnHandleLocal);
        pslCleanupIL->EmitLDLOC(dwReturnNativeHandleLocal);
        pslCleanupIL->EmitSTFLD(tkNativeHandleField);
        pslCleanupIL->EmitLabel(pDoneLabel);

        pslPostIL->EmitLDLOC(dwReturnHandleLocal);
    }
    else
    {
        pslIL->SetStubTargetReturnType(ELEMENT_TYPE_I);
        pslPostIL->EmitSTLOC(dwReturnNativeHandleLocal);

        // 6) store return value in safehandle
        // The thread abort logic knows that it must not interrupt the stub so we will
        // always be able to execute this sequence after returning from the call.
        pslPostIL->EmitLDLOC(dwReturnHandleLocal);
        pslPostIL->EmitLDLOC(dwReturnNativeHandleLocal);
        pslPostIL->EmitSTFLD(tkNativeHandleField);
        pslPostIL->EmitLDLOC(dwReturnHandleLocal);
    }

    return OVERRIDDEN;
} // ILSafeHandleMarshaler::ReturnOverride

void ILCriticalHandleMarshaler::EmitConvertContentsCLRToNative(ILCodeStream* pslILEmit)
{
    _ASSERTE(IsFieldMarshal(m_dwMarshalFlags));

    EmitLoadManagedValue(pslILEmit);
    pslILEmit->EmitLDFLD(pslILEmit->GetToken(CoreLibBinder::GetField(FIELD__CRITICAL_HANDLE__HANDLE)));
    EmitStoreNativeValue(pslILEmit);
}

void ILCriticalHandleMarshaler::EmitConvertContentsNativeToCLR(ILCodeStream* pslILEmit)
{
    _ASSERTE(IsFieldMarshal(m_dwMarshalFlags));

    ILCodeLabel* successLabel = pslILEmit->NewCodeLabel();
    ILCodeLabel* failureLabel = pslILEmit->NewCodeLabel();

    EmitLoadManagedValue(pslILEmit);
    pslILEmit->EmitBRFALSE(failureLabel);
    EmitLoadManagedValue(pslILEmit);
    pslILEmit->EmitLDFLD(pslILEmit->GetToken(CoreLibBinder::GetField(FIELD__CRITICAL_HANDLE__HANDLE)));
    EmitLoadNativeValue(pslILEmit);
    pslILEmit->EmitBEQ(successLabel);
    pslILEmit->EmitLabel(failureLabel);
    pslILEmit->EmitCALL(METHOD__HANDLE_MARSHALER__THROW_CRITICALHANDLE_FIELD_CHANGED, 0, 0);
    pslILEmit->EmitLabel(successLabel);
}

//---------------------------------------------------------------------------------------
//
MarshalerOverrideStatus ILCriticalHandleMarshaler::ArgumentOverride(NDirectStubLinker* psl,
                                                BOOL               byref,
                                                BOOL               fin,
                                                BOOL               fout,
                                                BOOL               fManagedToNative,
                                                OverrideProcArgs*  pargs,
                                                UINT*              pResID,
                                                UINT               argidx,
                                                UINT               nativeStackOffset)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    ILCodeStream* pslIL         = psl->GetMarshalCodeStream();
    ILCodeStream* pslPostIL     = psl->GetUnmarshalCodeStream();
    ILCodeStream* pslILDispatch = psl->GetDispatchCodeStream();

    if (fManagedToNative)
    {
        pslIL->SetStubTargetArgType(ELEMENT_TYPE_I);

        // Grab the token for the native handle field embedded inside the CriticalHandle. We'll be using it to direct access
        // the native handle later.
        mdToken tkNativeHandleField = pslIL->GetToken(CoreLibBinder::GetField(FIELD__CRITICAL_HANDLE__HANDLE));

        if (byref)
        {
            // The specific CriticalHandle subtype we're dealing with here.
            MethodTable *pHandleType = pargs->m_pMT;

            // Out CriticalHandle parameters must not be abstract.
            if (fout && pHandleType->IsAbstract())
            {
                *pResID = IDS_EE_BADMARSHAL_ABSTRACTOUTCRITICALHANDLE;
                return DISALLOWED;
            }

            // We rely on the CriticalHandle having a default constructor.
            if (!pHandleType->HasDefaultConstructor())
            {
                MAKE_WIDEPTR_FROMUTF8(wzMethodName, COR_CTOR_METHOD_NAME);
                COMPlusThrowNonLocalized(kMissingMethodException, wzMethodName);
            }

            // The high level logic (note that the parameter may be in, out or both):
            // 1) If this is an output parameter we need to preallocate a CriticalHandle to wrap the new native handle value. We
            //    must allocate this before the native call to avoid a failure point when we already have a native resource
            //    allocated. We must allocate a new CriticalHandle even if we have one on input since both input and output native
            //    handles need to be tracked and released by a CriticalHandle.
            // 2) Initialize a local IntPtr that will be passed to the native call. If we have an input CriticalHandle the value
            //    comes from there otherwise we get it from the new CriticalHandle (which is guaranteed to be initialized to an
            //    invalid handle value).
            // 3) If this is a out parameter we also store the original handle value (that we just computed above) in a local
            //    variable.
            // 4) After the native call, if this is an output parameter and the handle value we passed to native differs from
            //    the local copy we made then the new handle value is written into the output CriticalHandle and that
            //    CriticalHandle is propagated back to the caller.

            // Locals:
            LocalDesc       locOutputHandle;
            DWORD           dwOutputHandleLocal    = 0; // The output critical handle (out only)
            DWORD           dwOldNativeHandleLocal = 0; // The original native handle value for comparison (out only)
            DWORD           dwNativeHandleLocal;    // The input (and possibly updated) native handle value

            if (fout)
            {
                locOutputHandle.ElementType[0]  = ELEMENT_TYPE_INTERNAL;
                locOutputHandle.cbType          = 1;
                locOutputHandle.InternalToken   = pHandleType;

                dwOutputHandleLocal = pslIL->NewLocal(locOutputHandle);

                dwOldNativeHandleLocal = pslIL->NewLocal(ELEMENT_TYPE_I);
            }

            dwNativeHandleLocal = pslIL->NewLocal(ELEMENT_TYPE_I);


            // If we have an input CriticalHandle then initialize our NativeHandle local with it.
            if (fin)
            {
                pslIL->EmitLDARG(argidx);
                pslIL->EmitLDIND_REF();
                pslIL->EmitLDFLD(tkNativeHandleField);
                pslIL->EmitSTLOC(dwNativeHandleLocal);
            }

            // For output parameters we need to allocate a new CriticalHandle to hold the result.
            if (fout)
            {
                MethodDesc* pMDCtor = pHandleType->GetDefaultConstructor();
                pslIL->EmitNEWOBJ(pslIL->GetToken(pMDCtor), 0);
                pslIL->EmitSTLOC(dwOutputHandleLocal);

                // If we didn't provide an input handle then we initialize the NativeHandle local with the (initially invalid)
                // handle field set up inside the output handle by the constructor.
                if (!fin)
                {
                    pslIL->EmitLDLOC(dwOutputHandleLocal);
                    pslIL->EmitLDFLD(tkNativeHandleField);
                    pslIL->EmitSTLOC(dwNativeHandleLocal);
                }

                // Remember the handle value we start out with so we know whether to back propagate after the native call.
                pslIL->EmitLDLOC(dwNativeHandleLocal);
                pslIL->EmitSTLOC(dwOldNativeHandleLocal);
            }

            // Leave the address of the native handle local as the argument to the native method.
            EmitLoadNativeLocalAddrForByRefDispatch(pslILDispatch, dwNativeHandleLocal);

            if (fin)
            {
                // prevent the CriticalHandle from being finalized during the call-out to native
                pslPostIL->EmitLDARG(argidx);
                pslPostIL->EmitLDIND_REF();
                pslPostIL->EmitCALL(METHOD__GC__KEEP_ALIVE, 1, 0);
            }

            // On the output side we only backpropagate the native handle into the output CriticalHandle and the output
            // CriticalHandle to the caller if the native handle actually changed (otherwise we can end up with two
            // CriticalHandles wrapping the same native handle, which is bad).
            if (fout)
            {
                // We will use cleanup stream to avoid leaking the handle on thread abort.
                psl->EmitSetArgMarshalIndex(pslIL, NDirectStubLinker::CLEANUP_INDEX_ARG0_MARSHAL + argidx);

                psl->SetCleanupNeeded();
                ILCodeStream *pslCleanupIL = psl->GetCleanupCodeStream();

                ILCodeLabel *pDoneLabel = pslCleanupIL->NewCodeLabel();

                psl->EmitCheckForArgCleanup(pslCleanupIL,
                                            NDirectStubLinker::CLEANUP_INDEX_ARG0_MARSHAL + argidx,
                                            NDirectStubLinker::BranchIfNotMarshaled,
                                            pDoneLabel);

                // If this is an [in, out] handle check if the native handles have changed. If not we're finished.
                if (fin)
                {
                    pslCleanupIL->EmitLDLOC(dwNativeHandleLocal);
                    pslCleanupIL->EmitLDLOC(dwOldNativeHandleLocal);
                    pslCleanupIL->EmitCEQ();
                    pslCleanupIL->EmitBRTRUE(pDoneLabel);
                }

                // Propagate the native handle into the output CriticalHandle.
                pslCleanupIL->EmitLDLOC(dwOutputHandleLocal);
                pslCleanupIL->EmitLDLOC(dwNativeHandleLocal);
                pslCleanupIL->EmitSTFLD(tkNativeHandleField);

                // Propagate the output CriticalHandle back to the caller.
                pslCleanupIL->EmitLDARG(argidx);
                pslCleanupIL->EmitLDLOC(dwOutputHandleLocal);
                pslCleanupIL->EmitSTIND_REF();

                pslCleanupIL->EmitLabel(pDoneLabel);
            }
        }
        else
        {
            pslILDispatch->EmitLDARG(argidx);
            pslILDispatch->EmitLDFLD(tkNativeHandleField);

            // prevent the CriticalHandle from being finalized during the call-out to native
            pslPostIL->EmitLDARG(argidx);
            pslPostIL->EmitCALL(METHOD__GC__KEEP_ALIVE, 1, 0);
        }

        return OVERRIDDEN;
    }
    else
    {
        *pResID = IDS_EE_BADMARSHAL_CRITICALHANDLENATIVETOCOM;
        return DISALLOWED;
    }
}

//---------------------------------------------------------------------------------------
//
MarshalerOverrideStatus
ILCriticalHandleMarshaler::ReturnOverride(
    NDirectStubLinker * psl,
    BOOL                fManagedToNative,
    BOOL                fHresultSwap,
    OverrideProcArgs *  pargs,
    UINT       *        pResID)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(CheckPointer(psl));
        PRECONDITION(CheckPointer(pargs));
        PRECONDITION(CheckPointer(pResID));
    }
    CONTRACTL_END;

    if (!fManagedToNative)
    {
        *pResID = IDS_EE_BADMARSHAL_RETURNSHCOMTONATIVE;
        return DISALLOWED;
    }

    // Returned CriticalHandle parameters must not be abstract.
    if (pargs->m_pMT->IsAbstract())
    {
        *pResID = IDS_EE_BADMARSHAL_ABSTRACTRETCRITICALHANDLE;
        return DISALLOWED;
    }

    ILCodeStream * pslIL         = psl->GetMarshalCodeStream();
    ILCodeStream * pslPostIL     = psl->GetReturnUnmarshalCodeStream();
    ILCodeStream * pslILDispatch = psl->GetDispatchCodeStream();

    // 1) create local for new criticalhandle
    // 2) prealloc a criticalhandle
    // 3) create local to hold returned handle
    // 4) [byref] add byref IntPtr to native sig
    // 5) [byref] pass address of local as last arg
    // 6) store return value in criticalhandle

    // 1) create local for new criticalhandle
    MethodTable * pMT = pargs->m_pMT;
    LocalDesc     locDescReturnHandle(pMT);
    DWORD         dwReturnHandleLocal;

    dwReturnHandleLocal = pslIL->NewLocal(locDescReturnHandle);

    if (!pMT->HasDefaultConstructor())
    {
        MAKE_WIDEPTR_FROMUTF8(wzMethodName, COR_CTOR_METHOD_NAME);
        COMPlusThrowNonLocalized(kMissingMethodException, wzMethodName);
    }

    // 2) prealloc a criticalhandle
    MethodDesc * pMDCtor = pMT->GetDefaultConstructor();
    pslIL->EmitNEWOBJ(pslIL->GetToken(pMDCtor), 0);
    pslIL->EmitSTLOC(dwReturnHandleLocal);

    mdToken tkNativeHandleField = pslPostIL->GetToken(CoreLibBinder::GetField(FIELD__CRITICAL_HANDLE__HANDLE));

    // 3) create local to hold returned handle
    DWORD dwReturnNativeHandleLocal = pslIL->NewLocal(ELEMENT_TYPE_I);

    if (fHresultSwap)
    {
        // initialize the native handle
        pslIL->EmitLDLOC(dwReturnHandleLocal);
        pslIL->EmitLDFLD(tkNativeHandleField);
        pslIL->EmitSTLOC(dwReturnNativeHandleLocal);

        pslIL->SetStubTargetReturnType(ELEMENT_TYPE_I4);    // native method returns an HRESULT

        // 4) [byref] add byref IntPtr to native sig
        locDescReturnHandle.ElementType[0]  = ELEMENT_TYPE_BYREF;
        locDescReturnHandle.ElementType[1]  = ELEMENT_TYPE_I;
        locDescReturnHandle.cbType          = 2;
        pslIL->SetStubTargetArgType(&locDescReturnHandle, false);   // extra arg is a byref IntPtr

        // 5) [byref] pass address of local as last arg
        EmitLoadNativeLocalAddrForByRefDispatch(pslILDispatch, dwReturnNativeHandleLocal);

        // We will use cleanup stream to avoid leaking the handle on thread abort.
        psl->EmitSetArgMarshalIndex(pslIL, NDirectStubLinker::CLEANUP_INDEX_RETVAL_UNMARSHAL);

        psl->SetCleanupNeeded();
        ILCodeStream *pslCleanupIL = psl->GetCleanupCodeStream();
        ILCodeLabel *pDoneLabel = pslCleanupIL->NewCodeLabel();

        // 6) store return value in criticalhandle
        psl->EmitCheckForArgCleanup(pslCleanupIL,
                                    NDirectStubLinker::CLEANUP_INDEX_RETVAL_UNMARSHAL,
                                    NDirectStubLinker::BranchIfNotMarshaled,
                                    pDoneLabel);

        pslCleanupIL->EmitLDLOC(dwReturnHandleLocal);
        pslCleanupIL->EmitLDLOC(dwReturnNativeHandleLocal);
        pslCleanupIL->EmitSTFLD(tkNativeHandleField);
        pslCleanupIL->EmitLabel(pDoneLabel);

        pslPostIL->EmitLDLOC(dwReturnHandleLocal);
    }
    else
    {
        pslIL->SetStubTargetReturnType(ELEMENT_TYPE_I);
        pslPostIL->EmitSTLOC(dwReturnNativeHandleLocal);

        // 6) store return value in criticalhandle
        // The thread abort logic knows that it must not interrupt the stub so we will
        // always be able to execute this sequence after returning from the call.
        pslPostIL->EmitLDLOC(dwReturnHandleLocal);
        pslPostIL->EmitLDLOC(dwReturnNativeHandleLocal);
        pslPostIL->EmitSTFLD(tkNativeHandleField);
        pslPostIL->EmitLDLOC(dwReturnHandleLocal);
    }

    return OVERRIDDEN;
} // ILCriticalHandleMarshaler::ReturnOverride

MarshalerOverrideStatus ILBlittableValueClassWithCopyCtorMarshaler::ArgumentOverride(NDirectStubLinker* psl,
                                                BOOL               byref,
                                                BOOL               fin,
                                                BOOL               fout,
                                                BOOL               fManagedToNative,
                                                OverrideProcArgs*  pargs,
                                                UINT*              pResID,
                                                UINT               argidx,
                                                UINT               nativeStackOffset)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    ILCodeStream* pslIL         = psl->GetMarshalCodeStream();
    ILCodeStream* pslILDispatch = psl->GetDispatchCodeStream();

    if (byref)
    {
        *pResID = IDS_EE_BADMARSHAL_COPYCTORRESTRICTION;
        return DISALLOWED;
    }

    if (fManagedToNative)
    {
        // 1) create new native value type local
        // 2) run new->CopyCtor(old)
        // 3) run old->Dtor()

        LocalDesc   locDesc(pargs->mm.m_pMT);

        DWORD       dwNewValueTypeLocal;

        // Step 1
        dwNewValueTypeLocal = pslIL->NewLocal(locDesc);

        // Step 2
        if (pargs->mm.m_pCopyCtor)
        {
            // Managed copy constructor has signature of CopyCtor(T* new, T old);
            pslIL->EmitLDLOCA(dwNewValueTypeLocal);
            pslIL->EmitLDARG(argidx);
            pslIL->EmitCALL(pslIL->GetToken(pargs->mm.m_pCopyCtor), 2, 0);
        }
        else
        {
            pslIL->EmitLDARG(argidx);
            pslIL->EmitLDOBJ(pslIL->GetToken(pargs->mm.m_pMT));
            pslIL->EmitSTLOC(dwNewValueTypeLocal);
        }

        // Step 3
        if (pargs->mm.m_pDtor)
        {
            // Managed destructor has signature of Destructor(T old);
            pslIL->EmitLDARG(argidx);
            pslIL->EmitCALL(pslIL->GetToken(pargs->mm.m_pDtor), 1, 0);
        }
#ifdef TARGET_X86
        pslIL->SetStubTargetArgType(&locDesc);              // native type is the value type
        pslILDispatch->EmitLDLOC(dwNewValueTypeLocal);      // we load the local directly
#else
        pslIL->SetStubTargetArgType(ELEMENT_TYPE_I);        // native type is a pointer
        EmitLoadNativeLocalAddrForByRefDispatch(pslILDispatch, dwNewValueTypeLocal);
#endif

        return OVERRIDDEN;
    }
    else
    {
        // nothing to do but pass the value along
        // note that on x86 the argument comes by-value
        // but on other platforms it comes by-reference
#ifdef TARGET_X86
        LocalDesc locDesc(pargs->mm.m_pMT);
        pslIL->SetStubTargetArgType(&locDesc);

        DWORD       dwNewValueTypeLocal;
        dwNewValueTypeLocal = pslIL->NewLocal(locDesc);
        pslILDispatch->EmitLDARG(argidx);
        pslILDispatch->EmitSTLOC(dwNewValueTypeLocal);
        pslILDispatch->EmitLDLOCA(dwNewValueTypeLocal);
#else
        LocalDesc   locDesc(pargs->mm.m_pMT);
        locDesc.MakePointer();

        pslIL->SetStubTargetArgType(&locDesc);
        pslILDispatch->EmitLDARG(argidx);
#endif

        return OVERRIDDEN;
    }
}

LocalDesc ILArgIteratorMarshaler::GetNativeType()
{
    LIMITED_METHOD_CONTRACT;

    return LocalDesc(ELEMENT_TYPE_I); // va_list
}

LocalDesc ILArgIteratorMarshaler::GetManagedType()
{
    STANDARD_VM_CONTRACT;

    return LocalDesc(CoreLibBinder::GetClass(CLASS__ARG_ITERATOR));
}

bool ILArgIteratorMarshaler::SupportsArgumentMarshal(DWORD dwMarshalFlags, UINT* pErrorResID)
{
    LIMITED_METHOD_CONTRACT;

    if (IsByref(dwMarshalFlags))
    {
        *pErrorResID = IDS_EE_BADMARSHAL_ARGITERATORRESTRICTION;
        return false;
    }

    return true;
}

void ILArgIteratorMarshaler::EmitConvertSpaceAndContentsCLRToNative(ILCodeStream* pslILEmit)
{
    STANDARD_VM_CONTRACT;

    // Allocate enough memory for va_list
    DWORD dwVaListSizeLocal = pslILEmit->NewLocal(LocalDesc(ELEMENT_TYPE_U4));
    EmitLoadManagedHomeAddr(pslILEmit);
    pslILEmit->EmitCALL(METHOD__STUBHELPERS__CALC_VA_LIST_SIZE, 1, 1);
    pslILEmit->EmitSTLOC(dwVaListSizeLocal);
    pslILEmit->EmitLDLOC(dwVaListSizeLocal);
    pslILEmit->EmitLOCALLOC();
    EmitStoreNativeValue(pslILEmit);

    // void MarshalToUnmanagedVaListInternal(va_list, uint vaListSize, VARARGS* data)
    EmitLoadNativeValue(pslILEmit);
    pslILEmit->EmitLDLOC(dwVaListSizeLocal);
    EmitLoadManagedHomeAddr(pslILEmit);
    pslILEmit->EmitCALL(METHOD__STUBHELPERS__MARSHAL_TO_UNMANAGED_VA_LIST_INTERNAL, 3, 0);
}

void ILArgIteratorMarshaler::EmitConvertContentsNativeToCLR(ILCodeStream* pslILEmit)
{
    EmitLoadNativeValue(pslILEmit);
    EmitLoadManagedHomeAddr(pslILEmit);

    // void MarshalToManagedVaList(va_list va, VARARGS *dataout)
    pslILEmit->EmitCALL(METHOD__STUBHELPERS__MARSHAL_TO_MANAGED_VA_LIST_INTERNAL, 2, 0);
}

LocalDesc ILArrayWithOffsetMarshaler::GetNativeType()
{
    LIMITED_METHOD_CONTRACT;

    return LocalDesc(ELEMENT_TYPE_I);
}

LocalDesc ILArrayWithOffsetMarshaler::GetManagedType()
{
    STANDARD_VM_CONTRACT;

    return LocalDesc(CoreLibBinder::GetClass(CLASS__ARRAY_WITH_OFFSET));
}

bool ILArrayWithOffsetMarshaler::SupportsArgumentMarshal(DWORD dwMarshalFlags, UINT* pErrorResID)
{
    LIMITED_METHOD_CONTRACT;

    if (IsCLRToNative(dwMarshalFlags) && !IsByref(dwMarshalFlags) && IsIn(dwMarshalFlags) && IsOut(dwMarshalFlags))
    {
        return true;
    }

    *pErrorResID = IDS_EE_BADMARSHAL_AWORESTRICTION;

    return false;
}

void ILArrayWithOffsetMarshaler::EmitConvertSpaceAndContentsCLRToNativeTemp(ILCodeStream* pslILEmit)
{
    CONTRACTL
    {
        STANDARD_VM_CHECK;

        CONSISTENCY_CHECK(LOCAL_NUM_UNUSED == m_dwCountLocalNum);
        CONSISTENCY_CHECK(LOCAL_NUM_UNUSED == m_dwOffsetLocalNum);
        CONSISTENCY_CHECK(LOCAL_NUM_UNUSED == m_dwPinnedLocalNum);
    }
    CONTRACTL_END;

    int tokArrayWithOffset_m_array = pslILEmit->GetToken(CoreLibBinder::GetField(FIELD__ARRAY_WITH_OFFSET__M_ARRAY));
    int tokArrayWithOffset_m_count = pslILEmit->GetToken(CoreLibBinder::GetField(FIELD__ARRAY_WITH_OFFSET__M_COUNT));

    ILCodeLabel* pNonNullLabel = pslILEmit->NewCodeLabel();
    ILCodeLabel* pSlowAllocPathLabel = pslILEmit->NewCodeLabel();
    ILCodeLabel* pDoneLabel = pslILEmit->NewCodeLabel();

    m_dwCountLocalNum = pslILEmit->NewLocal(ELEMENT_TYPE_I4);

    //
    // Convert the space
    //

    EmitLoadManagedValue(pslILEmit);
    pslILEmit->EmitLDFLD(tokArrayWithOffset_m_array);
    pslILEmit->EmitBRTRUE(pNonNullLabel);

    pslILEmit->EmitLoadNullPtr();
    pslILEmit->EmitBR(pDoneLabel);
    pslILEmit->EmitLabel(pNonNullLabel);

    EmitLoadManagedValue(pslILEmit);
    pslILEmit->EmitLDFLD(tokArrayWithOffset_m_count);
    pslILEmit->EmitDUP();
    pslILEmit->EmitSTLOC(m_dwCountLocalNum);
    pslILEmit->EmitDUP();
    pslILEmit->EmitLDC(s_cbStackAllocThreshold);
    pslILEmit->EmitCGT_UN();
    pslILEmit->EmitBRTRUE(pSlowAllocPathLabel);

    // localloc
    pslILEmit->EmitLOCALLOC();

    pslILEmit->EmitBR(pDoneLabel);
    pslILEmit->EmitLabel(pSlowAllocPathLabel);

    // AllocCoTaskMem
    pslILEmit->EmitCALL(METHOD__MARSHAL__ALLOC_CO_TASK_MEM, 1, 1);

    pslILEmit->EmitLabel(pDoneLabel);
    EmitStoreNativeValue(pslILEmit);

    //
    // Convert the contents
    //

    int tokArrayWithOffset_m_offset = pslILEmit->GetToken(CoreLibBinder::GetField(FIELD__ARRAY_WITH_OFFSET__M_OFFSET));

    ILCodeLabel* pNullRefLabel = pslILEmit->NewCodeLabel();

    LocalDesc locDescPinned;
    locDescPinned.cbType = 2;
    locDescPinned.ElementType[0] = ELEMENT_TYPE_PINNED;
    locDescPinned.ElementType[1] = ELEMENT_TYPE_OBJECT;
    m_dwPinnedLocalNum = pslILEmit->NewLocal(locDescPinned);
    m_dwOffsetLocalNum = pslILEmit->NewLocal(ELEMENT_TYPE_I4);

    EmitLoadManagedValue(pslILEmit);
    pslILEmit->EmitLDFLD(tokArrayWithOffset_m_array);
    pslILEmit->EmitBRFALSE(pNullRefLabel);

    EmitLoadManagedValue(pslILEmit);
    pslILEmit->EmitLDFLD(tokArrayWithOffset_m_array);
    pslILEmit->EmitSTLOC(m_dwPinnedLocalNum);

    EmitLoadNativeValue(pslILEmit);                 // dest

    pslILEmit->EmitLDLOC(m_dwPinnedLocalNum);
    pslILEmit->EmitCALL(METHOD__MEMORY_MARSHAL__GET_ARRAY_DATA_REFERENCE_MDARRAY, 1, 1);
    pslILEmit->EmitCONV_I();

    EmitLoadManagedValue(pslILEmit);
    pslILEmit->EmitLDFLD(tokArrayWithOffset_m_offset);
    pslILEmit->EmitDUP();
    pslILEmit->EmitSTLOC(m_dwOffsetLocalNum);
    pslILEmit->EmitADD();                           // src
    pslILEmit->EmitLDLOC(m_dwCountLocalNum);        // len

    // static void Memcpy(byte* dest, byte* src, int len)
    pslILEmit->EmitCALL(METHOD__BUFFER__MEMCPY, 3, 0);

    pslILEmit->EmitLDNULL();
    pslILEmit->EmitSTLOC(m_dwPinnedLocalNum);

    pslILEmit->EmitLabel(pNullRefLabel);
}

void ILArrayWithOffsetMarshaler::EmitConvertContentsNativeToCLR(ILCodeStream* pslILEmit)
{
    CONTRACTL
    {
        STANDARD_VM_CHECK;

        CONSISTENCY_CHECK(LOCAL_NUM_UNUSED != m_dwCountLocalNum);
        CONSISTENCY_CHECK(LOCAL_NUM_UNUSED != m_dwOffsetLocalNum);
        CONSISTENCY_CHECK(LOCAL_NUM_UNUSED != m_dwPinnedLocalNum);
    }
    CONTRACTL_END;

    int tokArrayWithOffset_m_array = pslILEmit->GetToken(CoreLibBinder::GetField(FIELD__ARRAY_WITH_OFFSET__M_ARRAY));

    ILCodeLabel* pNullRefLabel = pslILEmit->NewCodeLabel();

    EmitLoadManagedValue(pslILEmit);
    pslILEmit->EmitLDFLD(tokArrayWithOffset_m_array);
    pslILEmit->EmitBRFALSE(pNullRefLabel);

    EmitLoadManagedValue(pslILEmit);
    pslILEmit->EmitLDFLD(tokArrayWithOffset_m_array);
    pslILEmit->EmitSTLOC(m_dwPinnedLocalNum);

    pslILEmit->EmitLDLOC(m_dwPinnedLocalNum);
    pslILEmit->EmitCALL(METHOD__MEMORY_MARSHAL__GET_ARRAY_DATA_REFERENCE_MDARRAY, 1, 1);
    pslILEmit->EmitCONV_I();

    pslILEmit->EmitLDLOC(m_dwOffsetLocalNum);
    pslILEmit->EmitADD();                           // dest

    EmitLoadNativeValue(pslILEmit);                 // src

    pslILEmit->EmitLDLOC(m_dwCountLocalNum);        // len

    // static void Memcpy(byte* dest, byte* src, int len)
    pslILEmit->EmitCALL(METHOD__BUFFER__MEMCPY, 3, 0);

    pslILEmit->EmitLDNULL();
    pslILEmit->EmitSTLOC(m_dwPinnedLocalNum);

    pslILEmit->EmitLabel(pNullRefLabel);
}

void ILArrayWithOffsetMarshaler::EmitClearNativeTemp(ILCodeStream* pslILEmit)
{
    STANDARD_VM_CONTRACT;

    ILCodeLabel* pDoneLabel = pslILEmit->NewCodeLabel();

    pslILEmit->EmitLDLOC(m_dwCountLocalNum);
    pslILEmit->EmitLDC(s_cbStackAllocThreshold);
    pslILEmit->EmitCGT_UN();
    pslILEmit->EmitBRFALSE(pDoneLabel);

    // CoTaskMemFree
    EmitLoadNativeValue(pslILEmit);
    pslILEmit->EmitCALL(METHOD__MARSHAL__FREE_CO_TASK_MEM, 1, 0);

    pslILEmit->EmitLabel(pDoneLabel);
}

LocalDesc ILAsAnyMarshalerBase::GetNativeType()
{
    LIMITED_METHOD_CONTRACT;

    return LocalDesc(ELEMENT_TYPE_I);
}

LocalDesc ILAsAnyMarshalerBase::GetManagedType()
{
    LIMITED_METHOD_CONTRACT;

    return LocalDesc(ELEMENT_TYPE_OBJECT);
}

bool ILAsAnyMarshalerBase::SupportsArgumentMarshal(DWORD dwMarshalFlags, UINT* pErrorResID)
{
    WRAPPER_NO_CONTRACT;

    if (IsCLRToNative(dwMarshalFlags) && !IsByref(dwMarshalFlags))
    {
        return true;
    }

    *pErrorResID = IDS_EE_BADMARSHAL_ASANYRESTRICTION;
    return false;
}

bool ILAsAnyMarshalerBase::SupportsReturnMarshal(DWORD dwMarshalFlags, UINT* pErrorResID)
{
    LIMITED_METHOD_CONTRACT;
    *pErrorResID = IDS_EE_BADMARSHAL_ASANYRESTRICTION;
    return false;
}

void ILAsAnyMarshalerBase::EmitCreateMngdMarshaler(ILCodeStream* pslILEmit)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        CONSISTENCY_CHECK(LOCAL_NUM_UNUSED == m_dwMngdMarshalerLocalNum);
    }
    CONTRACTL_END;

    LocalDesc marshalerType(CoreLibBinder::GetClass(CLASS__ASANY_MARSHALER));
    m_dwMngdMarshalerLocalNum = pslILEmit->NewLocal(marshalerType);
    DWORD dwTmpLocalNum = pslILEmit->NewLocal(ELEMENT_TYPE_I);

    pslILEmit->EmitLDC(sizeof(MngdNativeArrayMarshaler));
    pslILEmit->EmitLOCALLOC();
    pslILEmit->EmitSTLOC(dwTmpLocalNum);

    // marshaler = new AsAnyMarshaler(local_buffer)
    pslILEmit->EmitLDLOCA(m_dwMngdMarshalerLocalNum);
    pslILEmit->EmitINITOBJ(pslILEmit->GetToken(marshalerType.InternalToken));

    pslILEmit->EmitLDLOCA(m_dwMngdMarshalerLocalNum);
    pslILEmit->EmitLDLOC(dwTmpLocalNum);
    pslILEmit->EmitCALL(METHOD__ASANY_MARSHALER__CTOR, 2, 0);
}

void ILAsAnyMarshalerBase::EmitConvertContentsCLRToNative(ILCodeStream* pslILEmit)
{
    // nativeValue = marshaler.ConvertToNative(managedValue, flags);
    EmitLoadMngdMarshalerAddr(pslILEmit);
    EmitLoadManagedValue(pslILEmit);
    pslILEmit->EmitLDC(GetAsAnyFlags());
    pslILEmit->EmitCALL(METHOD__ASANY_MARSHALER__CONVERT_TO_NATIVE, 3, 1);
    EmitStoreNativeValue(pslILEmit);
}

void ILAsAnyMarshalerBase::EmitConvertContentsNativeToCLR(ILCodeStream* pslILEmit)
{
    // marshaler.ConvertToManaged(managedValue, nativeValue)
    EmitLoadMngdMarshalerAddr(pslILEmit);
    EmitLoadManagedValue(pslILEmit);
    EmitLoadNativeValue(pslILEmit);
    pslILEmit->EmitCALL(METHOD__ASANY_MARSHALER__CONVERT_TO_MANAGED, 3, 0);
}


bool ILAsAnyMarshalerBase::NeedsClearNative()
{
    LIMITED_METHOD_CONTRACT;
    return true;
}

void ILAsAnyMarshalerBase::EmitClearNativeTemp(ILCodeStream* pslILEmit)
{
    STANDARD_VM_CONTRACT;

    // marshaler.ClearNative(nativeHome)
    EmitLoadMngdMarshalerAddr(pslILEmit);
    EmitLoadNativeValue(pslILEmit);
    pslILEmit->EmitCALL(METHOD__ASANY_MARSHALER__CLEAR_NATIVE, 2, 0);
}

// we can get away with putting the GetManagedType and GetNativeType on ILMngdMarshaler because
// currently it is only used for reference marshaling where this is appropriate.  If it became
// used for something else, we would want to move this down in the inheritance tree..
LocalDesc ILMngdMarshaler::GetNativeType()
{
    LIMITED_METHOD_CONTRACT;

    return LocalDesc(ELEMENT_TYPE_I);
}

LocalDesc ILMngdMarshaler::GetManagedType()
{
    LIMITED_METHOD_CONTRACT;

    return LocalDesc(ELEMENT_TYPE_OBJECT);
}

void ILMngdMarshaler::EmitCallMngdMarshalerMethod(ILCodeStream* pslILEmit, MethodDesc *pMD)
{
    STANDARD_VM_CONTRACT;

    if (pMD != NULL)
    {
        MetaSig sig(pMD);
        UINT numArgs = sig.NumFixedArgs();

        if (numArgs == 3)
        {
            EmitLoadMngdMarshaler(pslILEmit);
        }
        else
        {
            _ASSERTE(numArgs == 2);
        }

        EmitLoadManagedHomeAddr(pslILEmit);
        EmitLoadNativeHomeAddr(pslILEmit);

        pslILEmit->EmitCALL(pslILEmit->GetToken(pMD), numArgs, 0);
    }
}

void ILNativeArrayMarshaler::EmitCreateMngdMarshaler(ILCodeStream* pslILEmit)
{
    STANDARD_VM_CONTRACT;

    m_dwMngdMarshalerLocalNum = pslILEmit->NewLocal(ELEMENT_TYPE_I);

    pslILEmit->EmitLDC(sizeof(MngdNativeArrayMarshaler));
    pslILEmit->EmitLOCALLOC();
    pslILEmit->EmitSTLOC(m_dwMngdMarshalerLocalNum);

    CREATE_MARSHALER_CARRAY_OPERANDS mops;
    m_pargs->m_pMarshalInfo->GetMops(&mops);

    pslILEmit->EmitLDLOC(m_dwMngdMarshalerLocalNum);

    pslILEmit->EmitLDTOKEN(pslILEmit->GetToken(mops.methodTable));
    pslILEmit->EmitCALL(METHOD__RT_TYPE_HANDLE__TO_INTPTR, 1, 1);

    DWORD dwFlags = mops.elementType;
    dwFlags |= (((DWORD)mops.bestfitmapping)        << 16);
    dwFlags |= (((DWORD)mops.throwonunmappablechar) << 24);

    pslILEmit->EmitLDC(dwFlags);

    if (!IsCLRToNative(m_dwMarshalFlags) && IsOut(m_dwMarshalFlags) && IsIn(m_dwMarshalFlags))
    {
        pslILEmit->EmitLDC(1); // true
    }
    else
    {
        pslILEmit->EmitLDC(0); // false
    }

    if (mops.elementType == VT_RECORD && !mops.methodTable->IsBlittable())
    {
        pslILEmit->EmitLDFTN(pslILEmit->GetToken(NDirect::CreateStructMarshalILStub(mops.methodTable)));
    }
    else
    {
        pslILEmit->EmitLoadNullPtr();
    }

    pslILEmit->EmitCALL(METHOD__MNGD_NATIVE_ARRAY_MARSHALER__CREATE_MARSHALER, 5, 0);
}

bool ILNativeArrayMarshaler::CanMarshalViaPinning()
{
    // We can't pin an array if we have a marshaler for the var type
    // or if we can't get a method-table representing the array (how we determine the offset to pin).
    return IsCLRToNative(m_dwMarshalFlags) && !IsByref(m_dwMarshalFlags) && (NULL != m_pargs->na.m_pArrayMT) && (NULL == OleVariant::GetMarshalerForVarType(m_pargs->na.m_vt, TRUE));
}

void ILNativeArrayMarshaler::EmitMarshalViaPinning(ILCodeStream* pslILEmit)
{
    CONTRACTL
    {
        STANDARD_VM_CHECK;
        PRECONDITION(CanMarshalViaPinning());
    }
    CONTRACTL_END;

    //
    // Replicate ML_PINNEDISOMORPHICARRAY_C2N_EXPRESS behavior -- note that this
    // gives in/out semantics "for free" even if the app doesn't specify one or
    // the other.  Since there is no enforcement of this, apps blithely depend
    // on it.
    //

    LocalDesc managedType = GetManagedType();
    managedType.MakePinned();

    DWORD dwPinnedLocal = pslILEmit->NewLocal(managedType);
    ILCodeLabel* pNullRefLabel = pslILEmit->NewCodeLabel();

    pslILEmit->EmitLoadNullPtr();
    EmitStoreNativeValue(pslILEmit);

    EmitLoadManagedValue(pslILEmit);
    pslILEmit->EmitBRFALSE(pNullRefLabel);

    // COMPAT: We cannot generate the same code that the C# compiler generates for
    // a fixed() statement on an array since we need to provide a non-null value
    // for a 0-length array. For compat reasons, we need to preserve old behavior.
    // Additionally, we need to ensure that we do not pass non-null for a zero-length
    // array when interacting with GDI/GDI+ since they fail on null arrays but succeed
    // on 0-length arrays.
    EmitLoadManagedValue(pslILEmit);
    pslILEmit->EmitSTLOC(dwPinnedLocal);
    pslILEmit->EmitLDLOC(dwPinnedLocal);
    pslILEmit->EmitCONV_I();
    // Optimize marshalling by emitting the data ptr offset directly into the IL stream
    // instead of doing an FCall to recalulate it each time when possible.
    pslILEmit->EmitLDC(ArrayBase::GetDataPtrOffset(m_pargs->na.m_pArrayMT));
    pslILEmit->EmitADD();
    EmitStoreNativeValue(pslILEmit);

    EmitLogNativeArgument(pslILEmit, dwPinnedLocal);

    pslILEmit->EmitLabel(pNullRefLabel);
}

//
// Peek at the SizeParamIndex argument
// 1) See if the SizeParamIndex argument is being passed by ref
// 2) Get the element type of SizeParamIndex argument
//
BOOL ILNativeArrayMarshaler::CheckSizeParamIndexArg(
    const CREATE_MARSHALER_CARRAY_OPERANDS &mops,
    CorElementType *pElementType)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(m_pargs != NULL);
        PRECONDITION(m_pargs->m_pMarshalInfo != NULL);
    }
    CONTRACTL_END;

    MethodDesc *pMD = m_pargs->m_pMarshalInfo->GetMethodDesc();
    _ASSERT(pMD);

    Module *pModule = m_pargs->m_pMarshalInfo->GetModule();
    _ASSERT(pModule);

    SigTypeContext emptyTypeContext;  // this is an empty type context: ndirect and COM calls are guaranteed to not be generics.
    MetaSig msig(pMD->GetSignature(),
                 pModule,
                 &emptyTypeContext);

    //
    // Go to the SizeParamIndex argument
    // Note that we already have check in place to make sure SizeParamIndex is within range
    //
    if (msig.HasExplicitThis())
        msig.SkipArg();

    for (int i = 0; i < mops.countParamIdx; ++i)
        msig.SkipArg();

    msig.NextArg();

    SigPointer sigPointer = msig.GetArgProps();

    // Peek into the SizeParamIndex argument
    CorElementType elementType;
    IfFailThrow(sigPointer.PeekElemType(&elementType));

    if (elementType != ELEMENT_TYPE_BYREF)
    {
        if (elementType == ELEMENT_TYPE_STRING  ||
            elementType == ELEMENT_TYPE_ARRAY   ||
            elementType == ELEMENT_TYPE_FNPTR   ||
            elementType == ELEMENT_TYPE_OBJECT  ||
            elementType == ELEMENT_TYPE_SZARRAY ||
            elementType == ELEMENT_TYPE_TYPEDBYREF)
        {
            COMPlusThrow(kMarshalDirectiveException, IDS_EE_SIZECONTROLBADTYPE);
        }

        *pElementType = elementType;
        return FALSE;
    }

    // Get the real type
    IfFailThrow(sigPointer.GetElemType(NULL));
    IfFailThrow(sigPointer.PeekElemType(&elementType));

    // All the integral types are supported
    switch(elementType)
    {
        case ELEMENT_TYPE_I1:
        case ELEMENT_TYPE_U1:
        case ELEMENT_TYPE_I2:
        case ELEMENT_TYPE_U2:
        case ELEMENT_TYPE_I4:
        case ELEMENT_TYPE_U4:
        case ELEMENT_TYPE_I8:
        case ELEMENT_TYPE_U8:
        case ELEMENT_TYPE_I:
        case ELEMENT_TYPE_U:
            break;

        default :
            COMPlusThrow(kMarshalDirectiveException, IDS_EE_SIZECONTROLBADTYPE);
    }

    *pElementType = elementType;
    return TRUE;
}

//
// Calculate the number of elements and load it into stack
//
void ILNativeArrayMarshaler::EmitLoadElementCount(ILCodeStream* pslILEmit)
{
    STANDARD_VM_CONTRACT;

    //
    // Determine the element count and load into evaluation stack
    //
    CREATE_MARSHALER_CARRAY_OPERANDS mops;
    m_pargs->m_pMarshalInfo->GetMops(&mops);

    if (mops.multiplier != 0)
    {
        //
        // SizeParamIndex arg fix up for LCID
        //
        unsigned countParamIdx = mops.countParamIdx;
        if (!IsCLRToNative(m_dwMarshalFlags))
        {
            int lcidParamIdx = GetLCIDParamIndex();

            if (lcidParamIdx >= 0 && (unsigned)lcidParamIdx <= countParamIdx)
            {
                // the LCID is injected before the count parameter so the index
                // has to be incremented to get the unmanaged parameter number
                countParamIdx++;
            }
        }

        //
        // Load SizeParamIndex argument
        //
        pslILEmit->EmitLDARG(countParamIdx);

        //
        // By-Ref support
        //

        // Is the SizeParamIndex points to a by-ref parameter?
        CorElementType sizeParamIndexArgType;
        if (CheckSizeParamIndexArg(mops, &sizeParamIndexArgType))
        {
            // Load the by-ref parameter
            switch (sizeParamIndexArgType)
            {
                case ELEMENT_TYPE_I1:
                    pslILEmit->EmitLDIND_I1();
                    break;

                case ELEMENT_TYPE_U1:
                    pslILEmit->EmitLDIND_U1();
                    break;

                case ELEMENT_TYPE_I2:
                    pslILEmit->EmitLDIND_I2();
                    break;

                case ELEMENT_TYPE_U2:
                    pslILEmit->EmitLDIND_U2();
                    break;

                case ELEMENT_TYPE_I4:
                    pslILEmit->EmitLDIND_I4();
                    break;

                case ELEMENT_TYPE_U4:
                    pslILEmit->EmitLDIND_U4();
                    break;

                case ELEMENT_TYPE_U8:
                case ELEMENT_TYPE_I8:
                    pslILEmit->EmitLDIND_I8();
                    break;

                case ELEMENT_TYPE_I:
                case ELEMENT_TYPE_U:
                    pslILEmit->EmitLDIND_I();
                    break;

                default :
                    // Should not go here because we should've thrown exception
                    _ASSERT(FALSE);
            }

        }

        pslILEmit->EmitCONV_OVF_I4();

        // multiplier * arg + additive
        pslILEmit->EmitLDC(mops.multiplier);
        pslILEmit->EmitMUL_OVF();
        pslILEmit->EmitLDC(mops.additive);
        pslILEmit->EmitADD_OVF();
    }
    else
    {
        pslILEmit->EmitLDC((int)mops.additive);
    }
}

void ILNativeArrayMarshaler::EmitConvertSpaceNativeToCLR(ILCodeStream* pslILEmit)
{
    STANDARD_VM_CONTRACT;

    EmitLoadMngdMarshaler(pslILEmit);
    EmitLoadManagedHomeAddr(pslILEmit);
    EmitLoadNativeHomeAddr(pslILEmit);

    if (IsByref(m_dwMarshalFlags))
    {
        //
        // Reset the element count just in case there is an exception thrown in the code emitted by
        // EmitLoadElementCount. The best thing we can do here is to avoid a crash.
        //
        _ASSERTE(m_dwSavedSizeArg != LOCAL_NUM_UNUSED);
        pslILEmit->EmitLDC(0);
        pslILEmit->EmitSTLOC(m_dwSavedSizeArg);
    }

    // Dynamically calculate element count using SizeParamIndex argument
    EmitLoadElementCount(pslILEmit);

    if (IsByref(m_dwMarshalFlags))
    {
        //
        // Save the native array size before converting it to managed and load it again
        //
        _ASSERTE(m_dwSavedSizeArg != LOCAL_NUM_UNUSED);
        pslILEmit->EmitSTLOC(m_dwSavedSizeArg);
        pslILEmit->EmitLDLOC(m_dwSavedSizeArg);
    }

    // MngdNativeArrayMarshaler::ConvertSpaceToManaged
    pslILEmit->EmitCALL(pslILEmit->GetToken(GetConvertSpaceToManagedMethod()), 4, 0);
}

void ILNativeArrayMarshaler::EmitConvertSpaceCLRToNative(ILCodeStream* pslILEmit)
{
    STANDARD_VM_CONTRACT;

    if (IsByref(m_dwMarshalFlags))
    {
        _ASSERTE(m_dwSavedSizeArg != LOCAL_NUM_UNUSED);

        //
        // Save the array size before converting it to native
        //
        EmitLoadManagedValue(pslILEmit);
        ILCodeLabel *pManagedHomeIsNull = pslILEmit->NewCodeLabel();
        pslILEmit->EmitBRFALSE(pManagedHomeIsNull);
        EmitLoadManagedValue(pslILEmit);
        pslILEmit->EmitLDLEN();
        pslILEmit->EmitSTLOC(m_dwSavedSizeArg);
        pslILEmit->EmitLabel(pManagedHomeIsNull);
    }


    ILMngdMarshaler::EmitConvertSpaceCLRToNative(pslILEmit);
}

void ILNativeArrayMarshaler::EmitClearNative(ILCodeStream* pslILEmit)
{
    STANDARD_VM_CONTRACT;

    EmitLoadMngdMarshaler(pslILEmit);
    EmitLoadNativeHomeAddr(pslILEmit);
    EmitLoadNativeSize(pslILEmit);

    pslILEmit->EmitCALL(pslILEmit->GetToken(GetClearNativeMethod()), 3, 0);
}

void ILNativeArrayMarshaler::EmitLoadNativeSize(ILCodeStream* pslILEmit)
{
    STANDARD_VM_CONTRACT;

   if (IsByref(m_dwMarshalFlags))
    {
        _ASSERT(m_dwSavedSizeArg != LOCAL_NUM_UNUSED);
        pslILEmit->EmitLDLOC(m_dwSavedSizeArg);
    }
    else
    {
        pslILEmit->EmitLDC(0);
        EmitLoadManagedValue(pslILEmit);
        ILCodeLabel *pManagedHomeIsNull = pslILEmit->NewCodeLabel();
        pslILEmit->EmitBRFALSE(pManagedHomeIsNull);
        pslILEmit->EmitPOP();                       // Pop the 0 on the stack
        EmitLoadManagedValue(pslILEmit);
        pslILEmit->EmitLDLEN();
        pslILEmit->EmitCONV_OVF_I4();
        pslILEmit->EmitLabel(pManagedHomeIsNull);   // Keep the 0 on the stack
    }
}

void ILNativeArrayMarshaler::EmitClearNativeContents(ILCodeStream* pslILEmit)
{
    STANDARD_VM_CONTRACT;

    EmitLoadMngdMarshaler(pslILEmit);
    EmitLoadNativeHomeAddr(pslILEmit);
    EmitLoadNativeSize(pslILEmit);

    pslILEmit->EmitCALL(pslILEmit->GetToken(GetClearNativeContentsMethod()), 3, 0);
}

void ILNativeArrayMarshaler::EmitSetupArgumentForMarshalling(ILCodeStream* pslILEmit)
{
    if (IsByref(m_dwMarshalFlags))
    {
        EmitNewSavedSizeArgLocal(pslILEmit);
    }
}

void ILNativeArrayMarshaler::EmitNewSavedSizeArgLocal(ILCodeStream* pslILEmit)
{
    STANDARD_VM_CONTRACT;

    _ASSERTE(m_dwSavedSizeArg == LOCAL_NUM_UNUSED);
    m_dwSavedSizeArg = pslILEmit->NewLocal(ELEMENT_TYPE_I4);
    pslILEmit->EmitLDC(0);
    pslILEmit->EmitSTLOC(m_dwSavedSizeArg);
}

extern "C" void QCALLTYPE MngdNativeArrayMarshaler_ConvertSpaceToNative(MngdNativeArrayMarshaler* pThis, QCall::ObjectHandleOnStack pManagedHome, void** pNativeHome)
{
    QCALL_CONTRACT;

    BEGIN_QCALL;

    GCX_COOP();

    if (pManagedHome.Get() == NULL)
    {
        *pNativeHome = NULL;
    }
    else
    {
        SIZE_T cElements = ((BASEARRAYREF)pManagedHome.Get())->GetNumComponents();
        SIZE_T cbElement = OleVariant::GetElementSizeForVarType(pThis->m_vt, pThis->m_pElementMT);

        if (cbElement == 0)
            COMPlusThrow(kArgumentException, IDS_EE_COM_UNSUPPORTED_SIG);

        GCX_PREEMP();

        SIZE_T cbArray;
        if ( (!ClrSafeInt<SIZE_T>::multiply(cElements, cbElement, cbArray)) || cbArray > MAX_SIZE_FOR_INTEROP)
            COMPlusThrow(kArgumentException, IDS_EE_STRUCTARRAYTOOLARGE);

        *pNativeHome = CoTaskMemAlloc(cbArray);

        if (*pNativeHome == NULL)
            ThrowOutOfMemory();

        // initialize the array
        FillMemory(*pNativeHome, cbArray, 0);
    }

    END_QCALL;
}

extern "C" void QCALLTYPE MngdNativeArrayMarshaler_ConvertContentsToNative(MngdNativeArrayMarshaler* pThis, QCall::ObjectHandleOnStack pManagedHome, void** pNativeHome)
{
    QCALL_CONTRACT;

    BEGIN_QCALL;

    GCX_COOP();

    BASEARRAYREF arrayRef = NULL;
    GCPROTECT_BEGIN(arrayRef);
    arrayRef = (BASEARRAYREF)pManagedHome.Get();

    if (arrayRef != NULL)
    {
        const OleVariant::Marshaler* pMarshaler = OleVariant::GetMarshalerForVarType(pThis->m_vt, TRUE);
        SIZE_T cElements = arrayRef->GetNumComponents();
        if (pMarshaler == NULL || pMarshaler->ComToOleArray == NULL)
        {
            if ( (!ClrSafeInt<SIZE_T>::multiply(cElements, OleVariant::GetElementSizeForVarType(pThis->m_vt, pThis->m_pElementMT), cElements)) || cElements > MAX_SIZE_FOR_INTEROP)
                COMPlusThrow(kArgumentException, IDS_EE_STRUCTARRAYTOOLARGE);

            _ASSERTE(!GetTypeHandleForCVType(OleVariant::GetCVTypeForVarType(pThis->m_vt)).GetMethodTable()->ContainsPointers());
            memcpyNoGCRefs(*pNativeHome, arrayRef->GetDataPtr(), cElements);
        }
        else
        {
            pMarshaler->ComToOleArray(&arrayRef, *pNativeHome, pThis->m_pElementMT, pThis->m_BestFitMap,
                                      pThis->m_ThrowOnUnmappableChar, pThis->m_NativeDataValid, cElements, pThis->m_pManagedMarshaler);
        }
    }

    GCPROTECT_END();

    END_QCALL;
}

extern "C" void QCALLTYPE MngdNativeArrayMarshaler_ConvertSpaceToManaged(MngdNativeArrayMarshaler* pThis,
        QCall::ObjectHandleOnStack managedHome, void** pNativeHome, INT32 cElements)
{
    QCALL_CONTRACT;

    BEGIN_QCALL;
    GCX_COOP();

    if (*pNativeHome == NULL)
    {
        managedHome.Set(NULL);
    }
    else
    {
        // <TODO>@todo: lookup this class before marshal time</TODO>
        if (pThis->m_Array.IsNull())
        {
            // Get proper array class name & type
            pThis->m_Array = OleVariant::GetArrayForVarType(pThis->m_vt, TypeHandle(pThis->m_pElementMT));
            if (pThis->m_Array.IsNull())
                COMPlusThrow(kTypeLoadException);
        }
        //
        // Allocate array
        //
        managedHome.Set(AllocateSzArray(pThis->m_Array, cElements));
    }

    END_QCALL;
}

extern "C" void QCALLTYPE MngdNativeArrayMarshaler_ConvertContentsToManaged(MngdNativeArrayMarshaler* pThis, QCall::ObjectHandleOnStack pManagedHome, void** pNativeHome)
{
    QCALL_CONTRACT;

    BEGIN_QCALL;

    GCX_COOP();

    BASEARRAYREF arrayRef = NULL;
    GCPROTECT_BEGIN(arrayRef);
    arrayRef = (BASEARRAYREF)pManagedHome.Get();

    if (*pNativeHome != NULL)
    {
        const OleVariant::Marshaler *pMarshaler = OleVariant::GetMarshalerForVarType(pThis->m_vt, TRUE);

        if (pMarshaler == NULL || pMarshaler->OleToComArray == NULL)
        {
            SIZE_T cElements;
            if ( (!ClrSafeInt<SIZE_T>::multiply(arrayRef->GetNumComponents(), OleVariant::GetElementSizeForVarType(pThis->m_vt, pThis->m_pElementMT), cElements)) || cElements > MAX_SIZE_FOR_INTEROP)
                COMPlusThrow(kArgumentException, IDS_EE_STRUCTARRAYTOOLARGE);

                // If we are copying variants, strings, etc, we need to use write barrier
            _ASSERTE(!GetTypeHandleForCVType(OleVariant::GetCVTypeForVarType(pThis->m_vt)).GetMethodTable()->ContainsPointers());
            memcpyNoGCRefs(arrayRef->GetDataPtr(), *pNativeHome, cElements);
        }
        else
        {
            pMarshaler->OleToComArray(*pNativeHome, &arrayRef, pThis->m_pElementMT, pThis->m_pManagedMarshaler);
        }
    }

    GCPROTECT_END();
    END_QCALL;
}

extern "C" void QCALLTYPE MngdNativeArrayMarshaler_ClearNativeContents(MngdNativeArrayMarshaler* pThis, void** pNativeHome, INT32 cElements)
{
    QCALL_CONTRACT;
    BEGIN_QCALL;
    GCX_COOP();

    if (*pNativeHome != NULL)
    {
        const OleVariant::Marshaler *pMarshaler = OleVariant::GetMarshalerForVarType(pThis->m_vt, FALSE);

        if (pMarshaler != NULL && pMarshaler->ClearOleArray != NULL)
        {
            pMarshaler->ClearOleArray(*pNativeHome, cElements, pThis->m_pElementMT, pThis->m_pManagedMarshaler);
        }
    }

    END_QCALL;
}

void ILFixedArrayMarshaler::EmitCreateMngdMarshaler(ILCodeStream* pslILEmit)
{
    STANDARD_VM_CONTRACT;

    m_dwMngdMarshalerLocalNum = pslILEmit->NewLocal(ELEMENT_TYPE_I);

    pslILEmit->EmitLDC(sizeof(MngdFixedArrayMarshaler));
    pslILEmit->EmitLOCALLOC();
    pslILEmit->EmitSTLOC(m_dwMngdMarshalerLocalNum);

    CREATE_MARSHALER_CARRAY_OPERANDS mops;
    m_pargs->m_pMarshalInfo->GetMops(&mops);

    pslILEmit->EmitLDLOC(m_dwMngdMarshalerLocalNum);

    pslILEmit->EmitLDTOKEN(pslILEmit->GetToken(mops.methodTable));
    pslILEmit->EmitCALL(METHOD__RT_TYPE_HANDLE__TO_INTPTR, 1, 1);

    DWORD dwFlags = mops.elementType;
    dwFlags |= (((DWORD)mops.bestfitmapping) << 16);
    dwFlags |= (((DWORD)mops.throwonunmappablechar) << 24);

    pslILEmit->EmitLDC(dwFlags);

    pslILEmit->EmitLDC(mops.additive);

    if (mops.elementType == VT_RECORD && !mops.methodTable->IsBlittable())
    {
        pslILEmit->EmitLDFTN(pslILEmit->GetToken(NDirect::CreateStructMarshalILStub(mops.methodTable)));
    }
    else
    {
        pslILEmit->EmitLoadNullPtr();
    }

    pslILEmit->EmitCALL(METHOD__MNGD_FIXED_ARRAY_MARSHALER__CREATE_MARSHALER, 5, 0);
}

extern "C" void QCALLTYPE MngdFixedArrayMarshaler_ConvertContentsToNative(MngdFixedArrayMarshaler* pThis, QCall::ObjectHandleOnStack pManagedHome, void* pNativeHome)
{
    QCALL_CONTRACT;
    BEGIN_QCALL;

    GCX_COOP();

    BASEARRAYREF arrayRef = NULL;
    GCPROTECT_BEGIN(arrayRef);
    arrayRef = (BASEARRAYREF)pManagedHome.Get();

    if (pThis->m_vt == VTHACK_ANSICHAR)
    {
        SIZE_T nativeSize = sizeof(CHAR) * pThis->m_cElements;

        if (arrayRef == NULL)
        {
            FillMemory(pNativeHome, nativeSize, 0);
        }
        else
        {
            InternalWideToAnsi((const WCHAR*)arrayRef->GetDataPtr(),
                pThis->m_cElements,
                (CHAR*)pNativeHome,
                (int)nativeSize,
                pThis->m_BestFitMap,
                pThis->m_ThrowOnUnmappableChar);
        }
    }
    else
    {
        SIZE_T cbElement = OleVariant::GetElementSizeForVarType(pThis->m_vt, pThis->m_pElementMT);
        SIZE_T nativeSize = cbElement * pThis->m_cElements;

        if (arrayRef == NULL)
        {
            FillMemory(pNativeHome, nativeSize, 0);
        }
        else
        {

            const OleVariant::Marshaler* pMarshaler = OleVariant::GetMarshalerForVarType(pThis->m_vt, TRUE);
            SIZE_T cElements = arrayRef->GetNumComponents();
            if (pMarshaler == NULL || pMarshaler->ComToOleArray == NULL)
            {
                _ASSERTE(!GetTypeHandleForCVType(OleVariant::GetCVTypeForVarType(pThis->m_vt)).GetMethodTable()->ContainsPointers());
                memcpyNoGCRefs(pNativeHome, arrayRef->GetDataPtr(), nativeSize);
            }
            else
            {
                pMarshaler->ComToOleArray(&arrayRef, pNativeHome, pThis->m_pElementMT, pThis->m_BestFitMap,
                    pThis->m_ThrowOnUnmappableChar, FALSE, pThis->m_cElements, pThis->m_pManagedElementMarshaler);
            }
        }
    }

    GCPROTECT_END();
    END_QCALL;
}

extern "C" void QCALLTYPE MngdFixedArrayMarshaler_ConvertSpaceToManaged(MngdFixedArrayMarshaler* pThis,
    QCall::ObjectHandleOnStack pManagedHome, void* pNativeHome)
{
    QCALL_CONTRACT;

    BEGIN_QCALL;

    GCX_COOP();

    // <TODO>@todo: lookup this class before marshal time</TODO>
    if (pThis->m_Array.IsNull())
    {
        // Get proper array class name & type
        pThis->m_Array = OleVariant::GetArrayForVarType(pThis->m_vt, TypeHandle(pThis->m_pElementMT));
        if (pThis->m_Array.IsNull())
            COMPlusThrow(kTypeLoadException);
    }
    //
    // Allocate array
    //

    OBJECTREF arrayRef = AllocateSzArray(pThis->m_Array, pThis->m_cElements);
    pManagedHome.Set(arrayRef);

    END_QCALL;
}

extern "C" void QCALLTYPE MngdFixedArrayMarshaler_ConvertContentsToManaged(MngdFixedArrayMarshaler* pThis, QCall::ObjectHandleOnStack pManagedHome, void* pNativeHome)
{
    QCALL_CONTRACT;
    BEGIN_QCALL;

    GCX_COOP();

    BASEARRAYREF arrayRef = NULL;

    GCPROTECT_BEGIN(arrayRef);
    arrayRef = (BASEARRAYREF)pManagedHome.Get();

    if (pThis->m_vt == VTHACK_ANSICHAR)
    {
        MultiByteToWideChar(CP_ACP,
            MB_PRECOMPOSED,
            (const CHAR*)pNativeHome,
            pThis->m_cElements * sizeof(CHAR), // size, in bytes, of in buffer
            (WCHAR*)(arrayRef->GetDataPtr()),
            pThis->m_cElements);               // size, in WCHAR's of outbuffer
    }
    else
    {
        const OleVariant::Marshaler* pMarshaler = OleVariant::GetMarshalerForVarType(pThis->m_vt, TRUE);

        SIZE_T cbElement = OleVariant::GetElementSizeForVarType(pThis->m_vt, pThis->m_pElementMT);
        SIZE_T nativeSize = cbElement * pThis->m_cElements;


        if (pMarshaler == NULL || pMarshaler->OleToComArray == NULL)
        {
            // If we are copying variants, strings, etc, we need to use write barrier
            _ASSERTE(!GetTypeHandleForCVType(OleVariant::GetCVTypeForVarType(pThis->m_vt)).GetMethodTable()->ContainsPointers());
            memcpyNoGCRefs(arrayRef->GetDataPtr(), pNativeHome, nativeSize);
        }
        else
        {
            pMarshaler->OleToComArray(pNativeHome, &arrayRef, pThis->m_pElementMT, pThis->m_pManagedElementMarshaler);
        }
    }

    GCPROTECT_END();
    END_QCALL;
}

extern "C" void QCALLTYPE MngdFixedArrayMarshaler_ClearNativeContents(MngdFixedArrayMarshaler* pThis, void* pNativeHome)
{
    QCALL_CONTRACT;
    BEGIN_QCALL;
    GCX_COOP();

    const OleVariant::Marshaler* pMarshaler = OleVariant::GetMarshalerForVarType(pThis->m_vt, FALSE);

    if (pMarshaler != NULL && pMarshaler->ClearOleArray != NULL)
    {
        pMarshaler->ClearOleArray(pNativeHome, pThis->m_cElements, pThis->m_pElementMT, pThis->m_pManagedElementMarshaler);
    }

    END_QCALL;
}

#ifdef FEATURE_COMINTEROP
void ILSafeArrayMarshaler::EmitCreateMngdMarshaler(ILCodeStream* pslILEmit)
{
    STANDARD_VM_CONTRACT;

    m_dwMngdMarshalerLocalNum = pslILEmit->NewLocal(ELEMENT_TYPE_I);

    _ASSERTE(sizeof(MngdSafeArrayMarshaler) == sizeof(void*) * 2 + 8);
    pslILEmit->EmitLDC(TARGET_POINTER_SIZE * 2 + 8); // sizeof(MngdSafeArrayMarshaler)
    pslILEmit->EmitLOCALLOC();
    pslILEmit->EmitSTLOC(m_dwMngdMarshalerLocalNum);

    CREATE_MARSHALER_CARRAY_OPERANDS mops;
    m_pargs->m_pMarshalInfo->GetMops(&mops);

    DWORD dwFlags = mops.elementType;
    BYTE  fStatic = 0;

    if (NeedsCheckForStatic())
    {
        fStatic |= MngdSafeArrayMarshaler::SCSF_CheckForStatic;
    }

    if (!IsCLRToNative(m_dwMarshalFlags) && IsOut(m_dwMarshalFlags) && IsIn(m_dwMarshalFlags))
    {
        // Unmanaged->managed in/out is the only case where we expect the native buffer to contain valid data.
        fStatic |= MngdSafeArrayMarshaler::SCSF_NativeDataValid;
    }

    dwFlags |= fStatic << 16;
    dwFlags |= ((BYTE)!!m_pargs->m_pMarshalInfo->GetNoLowerBounds()) << 24;

    pslILEmit->EmitLDLOC(m_dwMngdMarshalerLocalNum);
    pslILEmit->EmitLDTOKEN(pslILEmit->GetToken(mops.methodTable));
    pslILEmit->EmitCALL(METHOD__RT_TYPE_HANDLE__TO_INTPTR, 1, 1);
    pslILEmit->EmitLDC(m_pargs->m_pMarshalInfo->GetArrayRank());
    pslILEmit->EmitLDC(dwFlags);

    if (mops.elementType == VT_RECORD && !mops.methodTable->IsBlittable())
    {
        pslILEmit->EmitLDFTN(pslILEmit->GetToken(NDirect::CreateStructMarshalILStub(mops.methodTable)));
    }
    else
    {
        pslILEmit->EmitLoadNullPtr();
    }

    pslILEmit->EmitCALL(METHOD__MNGD_SAFE_ARRAY_MARSHALER__CREATE_MARSHALER, 5, 0);
}

void ILSafeArrayMarshaler::EmitConvertContentsNativeToCLR(ILCodeStream* pslILEmit)
{
    STANDARD_VM_CONTRACT;

    ILMngdMarshaler::EmitConvertContentsNativeToCLR(pslILEmit);

    if (NeedsCheckForStatic())
    {
        CONSISTENCY_CHECK(-1 == m_dwOriginalManagedLocalNum);
        m_dwOriginalManagedLocalNum = pslILEmit->NewLocal(ELEMENT_TYPE_OBJECT);
        EmitLoadManagedValue(pslILEmit);
        pslILEmit->EmitSTLOC(m_dwOriginalManagedLocalNum);
    }
}

void ILSafeArrayMarshaler::EmitConvertContentsCLRToNative(ILCodeStream* pslILEmit)
{
    STANDARD_VM_CONTRACT;

    EmitLoadMngdMarshaler(pslILEmit);
    EmitLoadManagedHomeAddr(pslILEmit);
    EmitLoadNativeHomeAddr(pslILEmit);
    if (NeedsCheckForStatic())
    {
        CONSISTENCY_CHECK(-1 != m_dwOriginalManagedLocalNum);
        pslILEmit->EmitLDLOC(m_dwOriginalManagedLocalNum);
    }
    else
    {
        pslILEmit->EmitLDNULL();
    }
    pslILEmit->EmitCALL(METHOD__MNGD_SAFE_ARRAY_MARSHALER__CONVERT_CONTENTS_TO_NATIVE, 4, 0);
}

extern "C" void QCALLTYPE MngdSafeArrayMarshaler_CreateMarshaler(MngdSafeArrayMarshaler* pThis, MethodTable* pMT, UINT32 iRank, UINT32 dwFlags, PCODE pManagedMarshaler)
{
    QCALL_CONTRACT_NO_GC_TRANSITION;

    pThis->m_pElementMT    = pMT;
    pThis->m_iRank         = iRank;
    pThis->m_vt            = (VARTYPE)dwFlags;
    pThis->m_fStatic       = (BYTE)(dwFlags >> 16);
    pThis->m_nolowerbounds = (BYTE)(dwFlags >> 24);
    pThis->m_pManagedMarshaler = pManagedMarshaler;
}

extern "C" void QCALLTYPE MngdSafeArrayMarshaler_ConvertSpaceToNative(MngdSafeArrayMarshaler* pThis, QCall::ObjectHandleOnStack pManagedHome, void** pNativeHome)
{
    CONTRACTL
    {
        QCALL_CHECK;
        PRECONDITION(pThis->m_vt != VT_EMPTY);
        PRECONDITION(CheckPointer(pThis->m_pElementMT));
    }
    CONTRACTL_END;

    if (pThis->m_fStatic & MngdSafeArrayMarshaler::SCSF_IsStatic)
        return;

    BEGIN_QCALL;

    GCX_COOP();

    BASEARRAYREF arrayRef = NULL;
    GCPROTECT_BEGIN(arrayRef);
    arrayRef = (BASEARRAYREF)pManagedHome.Get();

    if (arrayRef != NULL)
    {
        *pNativeHome = (void *) OleVariant::CreateSafeArrayForArrayRef(&arrayRef, pThis->m_vt, pThis->m_pElementMT);
    }
    else
    {
        *pNativeHome = NULL;
    }

    GCPROTECT_END();

    END_QCALL;
}

extern "C" void QCALLTYPE MngdSafeArrayMarshaler_ConvertContentsToNative(MngdSafeArrayMarshaler* pThis, QCall::ObjectHandleOnStack pManagedHome, void** pNativeHome, QCall::ObjectHandleOnStack pOriginalManaged)
{
    CONTRACTL
    {
        QCALL_CHECK;
        PRECONDITION(pThis->m_vt != VT_EMPTY);
        PRECONDITION(CheckPointer(pThis->m_pElementMT));
    }
    CONTRACTL_END;

    BEGIN_QCALL;
    GCX_COOP();

    struct
    {
        OBJECTREF originalManaged;
        BASEARRAYREF arrayRef;
    } gc = {NULL, NULL};

    GCPROTECT_BEGIN(gc);

    gc.originalManaged = pOriginalManaged.Get();
    gc.arrayRef = (BASEARRAYREF)pManagedHome.Get();

    if ((pThis->m_fStatic & MngdSafeArrayMarshaler::SCSF_IsStatic) &&
        (gc.arrayRef != gc.originalManaged))
    {
        COMPlusThrow(kInvalidOperationException, IDS_INVALID_REDIM);
    }

    if (gc.arrayRef != NULL)
    {
        OleVariant::MarshalSafeArrayForArrayRef(&gc.arrayRef,
                                                (SAFEARRAY*)*pNativeHome,
                                                pThis->m_vt,
                                                pThis->m_pElementMT,
                                                pThis->m_pManagedMarshaler,
                                                (pThis->m_fStatic & MngdSafeArrayMarshaler::SCSF_NativeDataValid));
    }

    GCPROTECT_END();
    END_QCALL;
}

extern "C" void QCALLTYPE MngdSafeArrayMarshaler_ConvertSpaceToManaged(MngdSafeArrayMarshaler* pThis, QCall::ObjectHandleOnStack pManagedHome, void** pNativeHome)
{
    CONTRACTL
    {
        QCALL_CHECK;
        PRECONDITION(pThis->m_vt != VT_EMPTY);
        PRECONDITION(CheckPointer(pThis->m_pElementMT));
    }
    CONTRACTL_END;

    BEGIN_QCALL;
    GCX_COOP();

    if (*pNativeHome != NULL)
    {
        SAFEARRAY* nativeSafeArray = (SAFEARRAY*) *pNativeHome;

        // If the managed array has a rank defined then make sure the rank of the
        // SafeArray matches the defined rank.
        if (pThis->m_iRank != -1)
        {
            int iSafeArrayRank = SafeArrayGetDim(nativeSafeArray);
            if (pThis->m_iRank != iSafeArrayRank)
            {
                WCHAR strExpectedRank[64];
                WCHAR strActualRank[64];
                _ltow_s(pThis->m_iRank, strExpectedRank, ARRAY_SIZE(strExpectedRank), 10);
                _ltow_s(iSafeArrayRank, strActualRank, ARRAY_SIZE(strActualRank), 10);
                COMPlusThrow(kSafeArrayRankMismatchException, IDS_EE_SAFEARRAYRANKMISMATCH, strActualRank, strExpectedRank);
            }
        }

        if (pThis->m_nolowerbounds)
        {
            LONG lowerbound;
            if ( (SafeArrayGetDim(nativeSafeArray) != 1) ||
                 (FAILED(SafeArrayGetLBound(nativeSafeArray, 1, &lowerbound))) ||
                 lowerbound != 0 )
            {
                COMPlusThrow(kSafeArrayRankMismatchException, IDS_EE_SAFEARRAYSZARRAYMISMATCH);
            }
        }

        OBJECTREF arrayRef = (OBJECTREF) OleVariant::CreateArrayRefForSafeArray(nativeSafeArray,
                                                            pThis->m_vt,
                                                            pThis->m_pElementMT);

        pManagedHome.Set(arrayRef);
    }
    else
    {
        pManagedHome.Set(NULL);
    }

    END_QCALL;
}

extern "C" void QCALLTYPE MngdSafeArrayMarshaler_ConvertContentsToManaged(MngdSafeArrayMarshaler* pThis, QCall::ObjectHandleOnStack pManagedHome, void** pNativeHome)
{
    CONTRACTL
    {
        QCALL_CHECK;
        PRECONDITION(pThis->m_vt != VT_EMPTY);
        PRECONDITION(CheckPointer(pThis->m_pElementMT));
    }
    CONTRACTL_END;

    BEGIN_QCALL;
    GCX_COOP();

    SAFEARRAY* pNative = *(SAFEARRAY**)pNativeHome;
    BASEARRAYREF arrayRef = NULL;

    GCPROTECT_BEGIN(arrayRef);
    arrayRef = (BASEARRAYREF) pManagedHome.Get();

    if (pNative && pNative->fFeatures & FADF_STATIC)
    {
        pThis->m_fStatic |= MngdSafeArrayMarshaler::SCSF_IsStatic;
    }

    if (*pNativeHome != NULL)
    {
        OleVariant::MarshalArrayRefForSafeArray(pNative,
                                                &arrayRef,
                                                pThis->m_vt,
                                                pThis->m_pManagedMarshaler,
                                                pThis->m_pElementMT);
    }

    GCPROTECT_END();
    END_QCALL;
}

extern "C" void QCALLTYPE MngdSafeArrayMarshaler_ClearNative(MngdSafeArrayMarshaler* pThis, void** pNativeHome)
{
    QCALL_CONTRACT;

    if (pThis->m_fStatic & MngdSafeArrayMarshaler::SCSF_IsStatic)
        return;

    BEGIN_QCALL;

    if (*pNativeHome != NULL)
    {
        SafeArrayDestroy((SAFEARRAY*)*pNativeHome);
    }

    END_QCALL;
}

#endif // FEATURE_COMINTEROP

void ILReferenceCustomMarshaler::EmitCreateMngdMarshaler(ILCodeStream* pslILEmit)
{
    CONTRACTL
    {
        STANDARD_VM_CHECK;
        PRECONDITION(-1 == m_dwMngdMarshalerLocalNum);
    }
    CONTRACTL_END;

    //
    // allocate space for marshaler state
    //

    m_dwMngdMarshalerLocalNum = pslILEmit->NewLocal(LocalDesc(CoreLibBinder::GetClass(CLASS__ICUSTOM_MARSHALER)));

    //
    // call CreateCustomMarshalerHelper
    //

    pslILEmit->EmitLDTOKEN(pslILEmit->GetToken(m_pargs->rcm.m_pMD));
    pslILEmit->EmitCALL(METHOD__METHOD_HANDLE__TO_INTPTR, 1, 1);

    pslILEmit->EmitLDC(m_pargs->rcm.m_paramToken);

    pslILEmit->EmitLDTOKEN(pslILEmit->GetToken(TypeHandle::FromPtr(m_pargs->rcm.m_hndManagedType)));
    pslILEmit->EmitCALL(METHOD__RT_TYPE_HANDLE__TO_INTPTR, 1, 1);

    pslILEmit->EmitCALL(METHOD__STUBHELPERS__CREATE_CUSTOM_MARSHALER_HELPER, 3, 1);  // Create the CustomMarshalerHelper

    // Get the managed ICustomMarshaler object from the helper
    pslILEmit->EmitCALL(METHOD__MNGD_REF_CUSTOM_MARSHALER__GET_MARSHALER, 1, 1);

    pslILEmit->EmitSTLOC(m_dwMngdMarshalerLocalNum); // Store the ICustomMarshaler as our marshaler state
}
