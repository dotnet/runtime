// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
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
    
    return LocalDesc(MscorlibBinder::GetClass(GetManagedTypeBinderID()));
}

LocalDesc ILReflectionObjectMarshaler::GetNativeType()
{
    LIMITED_METHOD_CONTRACT;
    
    return LocalDesc(ELEMENT_TYPE_I);
}

void ILReflectionObjectMarshaler::EmitConvertContentsCLRToNative(ILCodeStream* pslILEmit)
{
    STANDARD_VM_CONTRACT;
    
    int tokObject__m_handle = pslILEmit->GetToken(MscorlibBinder::GetField(GetObjectFieldID()));
    int tokStruct__m_object = 0;
    BinderFieldID structField = GetStructureFieldID();

    // This marshaler can generate code for marshaling an object containing a handle, and for 
    // marshaling a struct referring to an object containing a handle.
    if (structField != 0)
    {
        tokStruct__m_object = pslILEmit->GetToken(MscorlibBinder::GetField(structField));
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
        // keep the object alive across the call-out to native
        if (tokStruct__m_object != 0)
        {
            EmitLoadManagedHomeAddr(m_pcsUnmarshal);
            m_pcsUnmarshal->EmitLDFLD(tokStruct__m_object);
        }
        else
        {
            EmitLoadManagedValue(m_pcsUnmarshal);
        }
        m_pcsUnmarshal->EmitCALL(METHOD__GC__KEEP_ALIVE, 1, 0);
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
    
    pslILEmit->EmitLabel(pNullLabel);

    //
    // @TODO: is there a better way to do this?
    //
    if (IsCLRToNative(m_dwMarshalFlags))
    {
        // keep the delegate ref alive across the call-out to native
        EmitLoadManagedValue(m_pcsUnmarshal);
        m_pcsUnmarshal->EmitCALL(METHOD__GC__KEEP_ALIVE, 1, 0);
    }
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
    pslILEmit->EmitCALL(METHOD__MARSHAL__GET_DELEGATE_FOR_FUNCTION_POINTER, 2, 1); // Delegate System.Marshal.GetDelegateForFunctionPointer(IntPtr p, Type t)
    EmitStoreManagedValue(pslILEmit);

    pslILEmit->EmitLabel(pNullLabel);
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

bool ILWSTRMarshaler::NeedsClearNative()
{
    LIMITED_METHOD_CONTRACT;

    // will evaluate to true iff there is something CoTaskMemAlloc'ed that we need to free
    bool needsClear = (IsByref(m_dwMarshalFlags) && IsOut(m_dwMarshalFlags)) || IsRetval(m_dwMarshalFlags);
    
    // m_fCoMemoryAllocated => needsClear
    // (if we allocated the memory, we will free it; for byref [out] and retval we free memory allocated by the callee)
    _ASSERTE(!m_fCoMemoryAllocated || needsClear);

    return needsClear;
}

void ILWSTRMarshaler::EmitClearNative(ILCodeStream* pslILEmit)
{
    STANDARD_VM_CONTRACT;

    EmitLoadNativeValue(pslILEmit);
    // static void CoTaskMemFree(IntPtr ptr)
    pslILEmit->EmitCALL(METHOD__MARSHAL__FREE_CO_TASK_MEM, 1, 0);
}

void ILWSTRMarshaler::EmitClearNativeTemp(ILCodeStream* pslILEmit)
{
    LIMITED_METHOD_CONTRACT;
    UNREACHABLE_MSG("The string is either pinned or a copy is stack-allocated, NeedsClearNative should have returned false");
}

bool ILWSTRMarshaler::CanUsePinnedManagedString(DWORD dwMarshalFlags)
{
    LIMITED_METHOD_CONTRACT;
    return IsCLRToNative(dwMarshalFlags) && !IsByref(dwMarshalFlags) && IsIn(dwMarshalFlags) && !IsOut(dwMarshalFlags);
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

    INDEBUG(m_fCoMemoryAllocated = true);

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

void ILWSTRMarshaler::EmitConvertSpaceAndContentsCLRToNativeTemp(ILCodeStream* pslILEmit)
{
    STANDARD_VM_CONTRACT;

    if (CanUsePinnedManagedString(m_dwMarshalFlags))
    {
        LocalDesc locDesc = GetManagedType();
        locDesc.MakePinned();
        DWORD dwPinnedLocal = pslILEmit->NewLocal(locDesc);
        int fieldDef = pslILEmit->GetToken(MscorlibBinder::GetField(FIELD__STRING__M_FIRST_CHAR));
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

        if (g_pConfig->InteropLogArguments())
        {
            m_pslNDirect->EmitLogNativeArgument(pslILEmit, dwPinnedLocal);
        }

        pslILEmit->EmitLabel(pNullRefLabel);

    }
    else
    {
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

        pslILEmit->EmitLOCALLOC();              // @TODO: add a non-localloc path for large strings
        EmitStoreNativeValue(pslILEmit);

        EmitLoadManagedValue(pslILEmit);
        EmitLoadNativeValue(pslILEmit);

        // src, dst

        pslILEmit->EmitLDLOC(dwLengthLocalNum); // length
        
        // static void System.String.InternalCopy(String src, IntPtr dest,int len)
        pslILEmit->EmitCALL(METHOD__STRING__INTERNAL_COPY, 3, 0);
        pslILEmit->EmitLabel(pNullRefLabel);
    }
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
    return LocalDesc(MscorlibBinder::GetClass(CLASS__STRING_BUILDER));
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
    if (IsCLRToNative(m_dwMarshalFlags) && !IsByref(m_dwMarshalFlags))
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
    
    return LocalDesc(MscorlibBinder::GetClass(CLASS__STRING_BUILDER));
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
    if (IsCLRToNative(m_dwMarshalFlags) && !IsByref(m_dwMarshalFlags))
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

    DWORD dwTempNumBytesLocal = pslILEmit->NewLocal(ELEMENT_TYPE_I4);

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

    pslILEmit->EmitDUP();
    pslILEmit->EmitADD();

    // stack: StringBuilder cb

    pslILEmit->EmitSTLOC(dwTempNumBytesLocal);

    // stack: StringBuilder

    EmitLoadNativeValue(pslILEmit);
    pslILEmit->EmitLDLOC(dwTempNumBytesLocal);

    // stack: stringbuilder native_buffer cb

    // void System.Text.StringBuilder.InternalCopy(IntPtr dest,int len)
    pslILEmit->EmitCALL(METHOD__STRING_BUILDER__INTERNAL_COPY, 3, 0);

    //
    // null-terminate the native string
    //
    EmitLoadNativeValue(pslILEmit);
    pslILEmit->EmitLDLOC(dwTempNumBytesLocal);
    pslILEmit->EmitADD();
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
        // static int System.String.wcslen(char *ptr)
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
    // static int System.String.wcslen(char *ptr)
    pslILEmit->EmitCALL(METHOD__STRING__WCSLEN, 1, 1);
    
    // void System.Text.StringBuilder.ReplaceBuffer(char* newBuffer, int newLength);
    pslILEmit->EmitCALL(METHOD__STRING_BUILDER__REPLACE_BUFFER_INTERNAL, 3, 0);
    pslILEmit->EmitLabel(pNullRefLabel);
}        

LocalDesc ILCSTRBufferMarshaler::GetManagedType()
{
    STANDARD_VM_CONTRACT;
    
    return LocalDesc(MscorlibBinder::GetClass(CLASS__STRING_BUILDER));
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

    pslILEmit->EmitLDSFLD(pslILEmit->GetToken(MscorlibBinder::GetField(FIELD__MARSHAL__SYSTEM_MAX_DBCS_CHAR_SIZE)));
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
    if (IsCLRToNative(m_dwMarshalFlags) && !IsByref(m_dwMarshalFlags))
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

    mdToken     managedVCToken = pslILEmit->GetToken(m_pargs->m_pMT);
    
    EmitLoadNativeHomeAddr(pslILEmit);
    pslILEmit->EmitLDTOKEN(managedVCToken); // pMT
    pslILEmit->EmitCALL(METHOD__RT_TYPE_HANDLE__GETVALUEINTERNAL, 1, 1);
    pslILEmit->EmitCALL(METHOD__VALUECLASSMARSHALER__CLEAR_NATIVE, 2, 0);
}


void ILValueClassMarshaler::EmitConvertContentsCLRToNative(ILCodeStream* pslILEmit)
{
    STANDARD_VM_CONTRACT;

    mdToken     managedVCToken = pslILEmit->GetToken(m_pargs->m_pMT);

    EmitLoadNativeHomeAddr(pslILEmit);      // dst
    EmitLoadManagedHomeAddr(pslILEmit);     // src
    pslILEmit->EmitLDTOKEN(managedVCToken); // pMT
    pslILEmit->EmitCALL(METHOD__RT_TYPE_HANDLE__GETVALUEINTERNAL, 1, 1); // Convert RTH to IntPtr

    m_pslNDirect->LoadCleanupWorkList(pslILEmit);
    pslILEmit->EmitCALL(METHOD__VALUECLASSMARSHALER__CONVERT_TO_NATIVE, 4, 0);        // void ConvertToNative(IntPtr dst, IntPtr src, IntPtr pMT, ref CleanupWorkListElement pCleanupWorkList)
}

void ILValueClassMarshaler::EmitConvertContentsNativeToCLR(ILCodeStream* pslILEmit)
{
    STANDARD_VM_CONTRACT;

    mdToken     managedVCToken = pslILEmit->GetToken(m_pargs->m_pMT);

    EmitLoadManagedHomeAddr(pslILEmit);     // dst
    EmitLoadNativeHomeAddr(pslILEmit);      // src
    pslILEmit->EmitLDTOKEN(managedVCToken);                                 // pMT
    pslILEmit->EmitCALL(METHOD__RT_TYPE_HANDLE__GETVALUEINTERNAL, 1, 1);
    pslILEmit->EmitCALL(METHOD__VALUECLASSMARSHALER__CONVERT_TO_MANAGED, 3, 0);   // void ConvertToManaged(IntPtr dst, IntPtr src, IntPtr pMT)
}


#ifdef FEATURE_COMINTEROP
LocalDesc ILObjectMarshaler::GetNativeType()
{
    STANDARD_VM_CONTRACT;

    return LocalDesc(TypeHandle(MscorlibBinder::GetClass(CLASS__NATIVEVARIANT)));
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
    
    return LocalDesc(MscorlibBinder::GetClass(CLASS__DATE_TIME));
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

    return LocalDesc(TypeHandle(MscorlibBinder::GetClass(CLASS__CURRENCY)));
}

LocalDesc ILCurrencyMarshaler::GetManagedType()
{
    STANDARD_VM_CONTRACT;
    
    return LocalDesc(TypeHandle(MscorlibBinder::GetClass(CLASS__DECIMAL)));
}


void ILCurrencyMarshaler::EmitReInitNative(ILCodeStream* pslILEmit)
{
    STANDARD_VM_CONTRACT;

    EmitLoadNativeHomeAddr(pslILEmit);
    pslILEmit->EmitINITOBJ(pslILEmit->GetToken(TypeHandle(MscorlibBinder::GetClass(CLASS__CURRENCY))));
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
        pslILEmit->EmitCALL(METHOD__RT_TYPE_HANDLE__GETVALUEINTERNAL, 1, 1);
    }
    else
    {
        pslILEmit->EmitLoadNullPtr();
    }
    
    if (itfInfo.thClass.GetMethodTable())
    {
        pslILEmit->EmitLDTOKEN(pslILEmit->GetToken(itfInfo.thClass.GetMethodTable()));
        pslILEmit->EmitCALL(METHOD__RT_TYPE_HANDLE__GETVALUEINTERNAL, 1, 1);
    }
    else
    {
        pslILEmit->EmitLoadNullPtr();
    }
    pslILEmit->EmitLDC(itfInfo.dwFlags);

    // static IntPtr ConvertToNative(object objSrc, IntPtr itfMT, IntPtr classMT, int flags);
    pslILEmit->EmitCALL(METHOD__INTERFACEMARSHALER__CONVERT_TO_NATIVE, 4, 1);

    EmitStoreNativeValue(pslILEmit);

    if (IsCLRToNative(m_dwMarshalFlags) && 
        m_pargs->m_pMarshalInfo->IsWinRTScenario())
    {    
        // If we are calling from CLR into WinRT and we are passing an interface to WinRT, we need to
        // keep the object alive across unmanaged call because Jupiter might need to add this
        // RCW into their live tree and whatever CCWs referenced by this RCW could get collected
        // before the call to native, for example:
        //
        // Button btn = new Button();
        // btn.OnClick += ...
        // m_grid.Children.Add(btn)
        //
        // In this case, btn could be collected and takes the delegate CCW with it, before Children.add 
        // native method is called, and as a result Jupiter will add the neutered CCW into the tree
        //
        // The fix is to extend the lifetime of the argument across the call to native by doing a GC.KeepAlive
        // keep the delegate ref alive across the call-out to native
        EmitLoadManagedValue(m_pcsUnmarshal);
        m_pcsUnmarshal->EmitCALL(METHOD__GC__KEEP_ALIVE, 1, 0);
    }
}

void ILInterfaceMarshaler::EmitConvertContentsNativeToCLR(ILCodeStream* pslILEmit)
{
    STANDARD_VM_CONTRACT;

    ItfMarshalInfo itfInfo;
    m_pargs->m_pMarshalInfo->GetItfMarshalInfo(&itfInfo);

    // the helper may assign NULL to the home (see below)
    EmitLoadNativeHomeAddr(pslILEmit);
    
    if (IsCLRToNative(m_dwMarshalFlags) && m_pargs->m_pMarshalInfo->IsWinRTScenario())
    {
        // We are converting an interface pointer to object in a CLR->native stub which means
        // that the interface pointer has been AddRef'ed for us by the callee. If we end up
        // wrapping it with a new RCW, we can omit another AddRef/Release pair. Note that if
        // a new RCW is created the native home will be zeroed out by the helper so the call
        // to InterfaceMarshaler__ClearNative will become a no-op.

        // Note that we are only doing this for WinRT scenarios to reduce the risk of this change
        itfInfo.dwFlags |= ItfMarshalInfo::ITF_MARSHAL_SUPPRESS_ADDREF;
    }

    if (itfInfo.thItf.GetMethodTable())
    {
        pslILEmit->EmitLDTOKEN(pslILEmit->GetToken(itfInfo.thItf.GetMethodTable()));
        pslILEmit->EmitCALL(METHOD__RT_TYPE_HANDLE__GETVALUEINTERNAL, 1, 1);
    }
    else
    {
        pslILEmit->EmitLoadNullPtr();
    }
    
    if (itfInfo.thClass.GetMethodTable())
    {
        pslILEmit->EmitLDTOKEN(pslILEmit->GetToken(itfInfo.thClass.GetMethodTable()));
        pslILEmit->EmitCALL(METHOD__RT_TYPE_HANDLE__GETVALUEINTERNAL, 1, 1);
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

void ILVBByValStrWMarshaler::EmitConvertContentsCLRToNative(ILCodeStream* pslILEmit)
{
    STANDARD_VM_CONTRACT;

    ILCodeStream *pcsSetup = m_pslNDirect->GetSetupCodeStream();
    m_dwLocalBuffer = pcsSetup->NewLocal(ELEMENT_TYPE_I);
    pcsSetup->EmitLoadNullPtr();
    pcsSetup->EmitSTLOC(m_dwLocalBuffer);


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
    pslILEmit->EmitSTLOC(dwNumBytesLocal);      // len <- doesn't include size of the DWORD preceeding the string
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

    // <emtpy>

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

    pslILEmit->EmitLDNULL();            // this
    EmitLoadNativeValue(pslILEmit);     // ptr
    pslILEmit->EmitLDC(0);              // startIndex
    pslILEmit->EmitLDLOC(m_dwCCHLocal); // length

    // String CtorCharPtrStartLength(char *ptr, int startIndex, int length)
    // TODO Phase5: Why do we call this weirdo?
    pslILEmit->EmitCALL(METHOD__STRING__CTORF_CHARPTR_START_LEN, 4, 1);

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
    
    if (IsCLRToNative(m_dwMarshalFlags) && !IsByref(m_dwMarshalFlags))
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

#ifdef FEATURE_COMINTEROP

LocalDesc ILHSTRINGMarshaler::GetNativeType()
{
    LIMITED_METHOD_CONTRACT;
    return LocalDesc(ELEMENT_TYPE_I);	// HSTRING
}

LocalDesc ILHSTRINGMarshaler::GetManagedType()
{
    LIMITED_METHOD_CONTRACT;
    return LocalDesc(ELEMENT_TYPE_STRING);
}

bool ILHSTRINGMarshaler::NeedsClearNative()
{
    LIMITED_METHOD_CONTRACT;
    return true;
}

void ILHSTRINGMarshaler::EmitConvertContentsCLRToNative(ILCodeStream* pslILEmit)
{
    CONTRACTL
    {
        STANDARD_VM_CHECK;
        PRECONDITION(CheckPointer(pslILEmit));
    }
    CONTRACTL_END;

    // If we're only going into native code, then we can optimize and create a HSTRING reference over
    // the pinned System.String.  However, if the parameter will remain in native code as an out
    // value, then we need to create a real HSTRING.
    if (!IsOut(m_dwMarshalFlags) && !IsRetval(m_dwMarshalFlags))
    {
        EmitConvertCLRToHSTRINGReference(pslILEmit);
    }
    else
    {
        EmitConvertCLRToHSTRING(pslILEmit);
    }
}

void ILHSTRINGMarshaler::EmitConvertCLRToHSTRINGReference(ILCodeStream* pslILEmit)
{
    CONTRACTL
    {
        STANDARD_VM_CHECK;
        PRECONDITION(CheckPointer(pslILEmit));
        PRECONDITION(!IsOut(m_dwMarshalFlags));
        PRECONDITION(!IsRetval(m_dwMarshalFlags));
    }
    CONTRACTL_END;

    //
    // The general strategy for fast path marshaling a short lived System.String -> HSTRING is:
    //      1. Pin the System.String
    //      2. Create an HSTRING Reference over the pinned string
    //      3. Pass that reference to native code
    //

    // Local to hold the HSTRING_HEADER of the HSTRING reference
    MethodTable *pHStringHeaderMT = MscorlibBinder::GetClass(CLASS__HSTRING_HEADER_MANAGED);
    DWORD dwHStringHeaderLocal = pslILEmit->NewLocal(pHStringHeaderMT);

    // Local to hold the pinned input string
    LocalDesc pinnedStringDesc = GetManagedType();
    pinnedStringDesc.MakePinned();
    DWORD dwPinnedStringLocal = pslILEmit->NewLocal(pinnedStringDesc);

    // pinnedString = managed
    EmitLoadManagedValue(pslILEmit);
    pslILEmit->EmitSTLOC(dwPinnedStringLocal);

    // hstring = HSTRINGMarshaler.ConvertManagedToNativeReference(pinnedString, out HStringHeader)
    pslILEmit->EmitLDLOC(dwPinnedStringLocal);
    pslILEmit->EmitLDLOCA(dwHStringHeaderLocal);
    pslILEmit->EmitCALL(METHOD__HSTRINGMARSHALER__CONVERT_TO_NATIVE_REFERENCE, 2, 1);

    if (g_pConfig->InteropLogArguments())
    {
        m_pslNDirect->EmitLogNativeArgument(pslILEmit, dwPinnedStringLocal);
    }

    EmitStoreNativeValue(pslILEmit);
}

void ILHSTRINGMarshaler::EmitConvertCLRToHSTRING(ILCodeStream* pslILEmit)
{
    CONTRACTL
    {
        STANDARD_VM_CHECK;
        PRECONDITION(CheckPointer(pslILEmit));
    }
    CONTRACTL_END;

    // hstring = HSTRINGMarshaler.ConvertManagedToNative(managed);
    EmitLoadManagedValue(pslILEmit);
    pslILEmit->EmitCALL(METHOD__HSTRINGMARSHALER__CONVERT_TO_NATIVE, 1, 1);
    EmitStoreNativeValue(pslILEmit);
}

void ILHSTRINGMarshaler::EmitConvertContentsNativeToCLR(ILCodeStream* pslILEmit)
{
    STANDARD_VM_CONTRACT;

    //
    // To convert an HSTRING to a CLR String:
    //      1. WindowsGetStringRawBuffer() to get the raw string data
    //      2. WindowsGetStringLen() to get the string length
    //      3. Construct a System.String from these parameters
    //      4. Release the HSTRING
    //

    // string = HSTRINGMarshaler.ConvertNativeToManaged(native);
    EmitLoadNativeValue(pslILEmit);
    pslILEmit->EmitCALL(METHOD__HSTRINGMARSHALER__CONVERT_TO_MANAGED, 1, 1);
    EmitStoreManagedValue(pslILEmit);
}


void ILHSTRINGMarshaler::EmitClearNative(ILCodeStream* pslILEmit)
{
    STANDARD_VM_CONTRACT;

    // HStringMarshaler.ClearNative(hstring)
    EmitLoadNativeValue(pslILEmit);
    pslILEmit->EmitCALL(METHOD__HSTRINGMARSHALER__CLEAR_NATIVE, 1, 0);
}

#endif // FEATURE_COMINTEROP

LocalDesc ILCUTF8Marshaler::GetManagedType()
{
	LIMITED_METHOD_CONTRACT;

	return LocalDesc(ELEMENT_TYPE_STRING);
}

void ILCUTF8Marshaler::EmitConvertContentsCLRToNative(ILCodeStream* pslILEmit)
{
	STANDARD_VM_CONTRACT;

	DWORD dwUtf8MarshalFlags =
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
				 		
		// (String.Length + 1)
		// Characters would be # of characters + 1 in case left over high surrogate is ?
		EmitLoadManagedValue(pslILEmit);
		pslILEmit->EmitCALL(METHOD__STRING__GET_LENGTH, 1, 1);
		pslILEmit->EmitLDC(1);
		pslILEmit->EmitADD();

		// Max 3 bytes per char.
		// (String.Length + 1) * 3		
		pslILEmit->EmitLDC(3);
		pslILEmit->EmitMUL();

		// +1 for the 0x0 that we put in.
		// ((String.Length + 1) * 3) + 1
		pslILEmit->EmitLDC(1);
		pslILEmit->EmitADD();
				
		// BufSize = ( (String.Length+1) * 3) + 1
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

	// UTF8Marshaler.ConvertToNative(dwUtf8MarshalFlags,pManaged, pLocalBuffer)
	pslILEmit->EmitLDC(dwUtf8MarshalFlags);
	EmitLoadManagedValue(pslILEmit);

	if (m_dwLocalBuffer != LOCAL_NUM_UNUSED)
	{
		pslILEmit->EmitLDLOC(m_dwLocalBuffer);
	}
	else
	{
		pslILEmit->EmitLoadNullPtr();
	}

	pslILEmit->EmitCALL(METHOD__CUTF8MARSHALER__CONVERT_TO_NATIVE, 3, 1);

	EmitStoreNativeValue(pslILEmit);
}

void ILCUTF8Marshaler::EmitConvertContentsNativeToCLR(ILCodeStream* pslILEmit)
{
	STANDARD_VM_CONTRACT;

	EmitLoadNativeValue(pslILEmit);
	pslILEmit->EmitCALL(METHOD__CUTF8MARSHALER__CONVERT_TO_MANAGED, 1, 1);
	EmitStoreManagedValue(pslILEmit);
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
        pslILEmit->EmitLDSFLD(pslILEmit->GetToken(MscorlibBinder::GetField(FIELD__MARSHAL__SYSTEM_MAX_DBCS_CHAR_SIZE)));
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
    pslILEmit->EmitLDC(uNativeSize);
    pslILEmit->EmitCALL(METHOD__MARSHAL__ALLOC_CO_TASK_MEM, 1, 1);
    pslILEmit->EmitDUP();           // for INITBLK
    EmitStoreNativeValue(pslILEmit);

    // initialize local block we just allocated
    pslILEmit->EmitLDC(0);
    pslILEmit->EmitLDC(uNativeSize);
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

        pslILEmit->EmitLDC(uNativeSize);
        pslILEmit->EmitLOCALLOC();
        pslILEmit->EmitDUP();           // for INITBLK
        EmitStoreNativeValue(pslILEmit);

        // initialize local block we just allocated
        pslILEmit->EmitLDC(0);
        pslILEmit->EmitLDC(uNativeSize);
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
    pslILEmit->EmitCALL(METHOD__RT_TYPE_HANDLE__GETVALUEINTERNAL, 1, 1);
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

    EmitLoadManagedValue(pslILEmit);
    EmitLoadNativeValue(pslILEmit);

    m_pslNDirect->LoadCleanupWorkList(pslILEmit);

    // static void FmtClassUpdateNativeInternal(object obj, byte* pNative, IntPtr pOptionalCleanupList);

    pslILEmit->EmitCALL(METHOD__STUBHELPERS__FMT_CLASS_UPDATE_NATIVE_INTERNAL, 3, 0);
    pslILEmit->EmitLabel(pNullRefLabel);
}

void ILLayoutClassPtrMarshaler::EmitConvertContentsNativeToCLR(ILCodeStream* pslILEmit)
{
    STANDARD_VM_CONTRACT;
    
    ILCodeLabel* pNullRefLabel = pslILEmit->NewCodeLabel();
    
    EmitLoadManagedValue(pslILEmit);
    pslILEmit->EmitBRFALSE(pNullRefLabel);

    EmitLoadManagedValue(pslILEmit);
    EmitLoadNativeValue(pslILEmit);

    // static void FmtClassUpdateCLRInternal(object obj, byte* pNative);
    pslILEmit->EmitCALL(METHOD__STUBHELPERS__FMT_CLASS_UPDATE_CLR_INTERNAL, 2, 0);
    pslILEmit->EmitLabel(pNullRefLabel);
}

void ILLayoutClassPtrMarshaler::EmitClearNativeContents(ILCodeStream * pslILEmit)
{
    STANDARD_VM_CONTRACT;

    int tokManagedType = pslILEmit->GetToken(m_pargs->m_pMT);
    
    EmitLoadNativeValue(pslILEmit);
    pslILEmit->EmitLDTOKEN(tokManagedType);
    pslILEmit->EmitCALL(METHOD__RT_TYPE_HANDLE__GETVALUEINTERNAL, 1, 1);

    // static void LayoutDestroyNativeInternal(byte* pNative, IntPtr pMT);
    pslILEmit->EmitCALL(METHOD__STUBHELPERS__LAYOUT_DESTROY_NATIVE_INTERNAL, 2, 0);
}


void ILBlittablePtrMarshaler::EmitConvertContentsCLRToNative(ILCodeStream* pslILEmit)
{
    STANDARD_VM_CONTRACT;

    ILCodeLabel* pNullRefLabel = pslILEmit->NewCodeLabel();
    UINT uNativeSize = m_pargs->m_pMT->GetNativeSize();
    int fieldDef = pslILEmit->GetToken(MscorlibBinder::GetField(FIELD__RAW_DATA__DATA));

    EmitLoadNativeValue(pslILEmit);
    pslILEmit->EmitBRFALSE(pNullRefLabel);

    EmitLoadNativeValue(pslILEmit);                             // dest

    EmitLoadManagedValue(pslILEmit);
    pslILEmit->EmitLDFLDA(fieldDef);                            // src

    pslILEmit->EmitLDC(uNativeSize);                            // size
    
    pslILEmit->EmitCPBLK();
    pslILEmit->EmitLabel(pNullRefLabel);
}

void ILBlittablePtrMarshaler::EmitConvertContentsNativeToCLR(ILCodeStream* pslILEmit)
{
    STANDARD_VM_CONTRACT;
    
    ILCodeLabel* pNullRefLabel = pslILEmit->NewCodeLabel();
    UINT uNativeSize = m_pargs->m_pMT->GetNativeSize();
    int fieldDef = pslILEmit->GetToken(MscorlibBinder::GetField(FIELD__RAW_DATA__DATA));
    
    EmitLoadManagedValue(pslILEmit);
    pslILEmit->EmitBRFALSE(pNullRefLabel);

    EmitLoadManagedValue(pslILEmit);
    pslILEmit->EmitLDFLDA(fieldDef);                            // dest

    EmitLoadNativeValue(pslILEmit);                             // src

    pslILEmit->EmitLDC(uNativeSize);                            // size

    pslILEmit->EmitCPBLK();
    pslILEmit->EmitLabel(pNullRefLabel);
}

void ILBlittablePtrMarshaler::EmitMarshalArgumentCLRToNative()
{
    CONTRACTL
    {
        STANDARD_VM_CHECK;
        PRECONDITION(IsCLRToNative(m_dwMarshalFlags) && !IsByref(m_dwMarshalFlags));
    }
    CONTRACTL_END;

    EmitSetupSigAndDefaultHomesCLRToNative();

    //
    // marshal
    //

    ILCodeLabel* pSkipAddLabel = m_pcsMarshal->NewCodeLabel();
    LocalDesc managedTypePinned = GetManagedType();
    managedTypePinned.MakePinned();
    DWORD dwPinnedLocal = m_pcsMarshal->NewLocal(managedTypePinned);

    EmitLoadManagedValue(m_pcsMarshal);
    
    m_pcsMarshal->EmitSTLOC(dwPinnedLocal);
    m_pcsMarshal->EmitLDLOC(dwPinnedLocal);
    m_pcsMarshal->EmitCONV_U();
    m_pcsMarshal->EmitDUP();
    m_pcsMarshal->EmitBRFALSE(pSkipAddLabel);
    m_pcsMarshal->EmitLDC(Object::GetOffsetOfFirstField());
    m_pcsMarshal->EmitADD();
    m_pcsMarshal->EmitLabel(pSkipAddLabel);

    if (g_pConfig->InteropLogArguments())
    {
        m_pslNDirect->EmitLogNativeArgument(m_pcsMarshal, dwPinnedLocal);
    }

    EmitStoreNativeValue(m_pcsMarshal);
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
        mdFieldDef handleField = pcsDispatch->GetToken(MscorlibBinder::GetField(FIELD__HANDLE_REF__HANDLE));
        pcsDispatch->EmitLDARG(argidx);
        pcsDispatch->EmitLDFLD(handleField);

        mdFieldDef wrapperField = pcsUnmarshal->GetToken(MscorlibBinder::GetField(FIELD__HANDLE_REF__WRAPPER));
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

LocalDesc ILSafeHandleMarshaler::GetManagedType()
{
    STANDARD_VM_CONTRACT;

    return LocalDesc(MscorlibBinder::GetClass(CLASS__SAFE_HANDLE));
}

LocalDesc ILSafeHandleMarshaler::GetNativeType()
{
    LIMITED_METHOD_CONTRACT;

    return LocalDesc(ELEMENT_TYPE_I);
}

bool ILSafeHandleMarshaler::NeedsClearNative()
{
    LIMITED_METHOD_CONTRACT;
    return true;
}

void ILSafeHandleMarshaler::EmitClearNative(ILCodeStream* pslILEmit)
{
    STANDARD_VM_CONTRACT;

    _ASSERTE(IsCLRToNative(m_dwMarshalFlags) && !IsByref(m_dwMarshalFlags));

    // call StubHelpers::SafeHandleRelease
    EmitLoadManagedValue(pslILEmit);
    pslILEmit->EmitCALL(METHOD__STUBHELPERS__SAFE_HANDLE_RELEASE, 1, 0);
}

void ILSafeHandleMarshaler::EmitMarshalArgumentCLRToNative()
{
    CONTRACTL
    {
        STANDARD_VM_CHECK;
        PRECONDITION(IsCLRToNative(m_dwMarshalFlags) && !IsByref(m_dwMarshalFlags));
    }
    CONTRACTL_END;

    EmitSetupSigAndDefaultHomesCLRToNative();

    // by-value CLR-to-native SafeHandle is always passed in-only regardless of [In], [Out]
    // marshal and cleanup communicate via an extra local and are both emitted in this method

    // bool <dwHandleAddRefedLocalNum> = false
    ILCodeStream *pcsSetup = m_pslNDirect->GetSetupCodeStream();
    DWORD dwHandleAddRefedLocalNum = pcsSetup->NewLocal(ELEMENT_TYPE_BOOLEAN);
    
    pcsSetup->EmitLDC(0);
    pcsSetup->EmitSTLOC(dwHandleAddRefedLocalNum);

    // <nativeHandle> = StubHelpers::SafeHandleAddRef(<managedSH>, ref <dwHandleAddRefedLocalNum>)
    EmitLoadManagedValue(m_pcsMarshal);
    m_pcsMarshal->EmitLDLOCA(dwHandleAddRefedLocalNum);
    m_pcsMarshal->EmitCALL(METHOD__STUBHELPERS__SAFE_HANDLE_ADD_REF, 2, 1);
    EmitStoreNativeValue(m_pcsMarshal);

    // cleanup:
    // if (<dwHandleAddRefedLocalNum>) StubHelpers.SafeHandleRelease(<managedSH>)
    ILCodeStream *pcsCleanup = m_pslNDirect->GetCleanupCodeStream();
    ILCodeLabel *pSkipClearNativeLabel = pcsCleanup->NewCodeLabel();

    pcsCleanup->EmitLDLOC(dwHandleAddRefedLocalNum);
    pcsCleanup->EmitBRFALSE(pSkipClearNativeLabel);

    EmitClearNativeTemp(pcsCleanup);
    m_pslNDirect->SetCleanupNeeded();

    pcsCleanup->EmitLabel(pSkipClearNativeLabel);
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
        if (byref)
        {
            pslIL->SetStubTargetArgType(ELEMENT_TYPE_I);

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
            mdToken tkNativeHandleField = pslIL->GetToken(MscorlibBinder::GetField(FIELD__SAFE_HANDLE__HANDLE));

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
            pslILDispatch->EmitLDLOCA(dwNativeHandleLocal);

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
            // Avoid using the cleanup list in this common case for perf reasons (cleanup list is
            // unmanaged and destroying it means excessive managed<->native transitions; in addition,
            // as X86 IL stubs do not use interop frames, there's nothing protecting the cleanup list
            // and the SafeHandle references must be GC handles which does not help perf either).
            //
            // This code path generates calls to StubHelpers.SafeHandleAddRef and SafeHandleRelease.
            // NICE: Could SafeHandle.DangerousAddRef and DangerousRelease be implemented in managed?
            return HANDLEASNORMAL;
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
        MAKE_WIDEPTR_FROMUTF8(wzMethodName, COR_CTOR_METHOD_NAME);
        COMPlusThrowNonLocalized(kMissingMethodException, wzMethodName);
    }

    // 2) prealloc a safehandle
    MethodDesc* pMDCtor = pMT->GetDefaultConstructor();
    pslIL->EmitNEWOBJ(pslIL->GetToken(pMDCtor), 0);
    pslIL->EmitSTLOC(dwReturnHandleLocal);

    mdToken tkNativeHandleField = pslPostIL->GetToken(MscorlibBinder::GetField(FIELD__SAFE_HANDLE__HANDLE));

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
        pslILDispatch->EmitLDLOCA(dwReturnNativeHandleLocal);

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
        mdToken tkNativeHandleField = pslIL->GetToken(MscorlibBinder::GetField(FIELD__CRITICAL_HANDLE__HANDLE));

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
            pslILDispatch->EmitLDLOCA(dwNativeHandleLocal);

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

    mdToken tkNativeHandleField = pslPostIL->GetToken(MscorlibBinder::GetField(FIELD__CRITICAL_HANDLE__HANDLE));

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
        pslILDispatch->EmitLDLOCA(dwReturnNativeHandleLocal);

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
#ifdef _TARGET_X86_
        pslIL->SetStubTargetArgType(&locDesc);              // native type is the value type
        pslILDispatch->EmitLDLOC(dwNewValueTypeLocal);      // we load the local directly
#else
        pslIL->SetStubTargetArgType(ELEMENT_TYPE_I);        // native type is a pointer
        pslILDispatch->EmitLDLOCA(dwNewValueTypeLocal);
#endif

        return OVERRIDDEN;
    }
    else
    {
        // nothing to do but pass the value along
        // note that on x86 the argument comes by-value but is converted to pointer by the UM thunk
        // so that we don't make copies that would not be accounted for by copy ctors
        LocalDesc   locDesc(pargs->mm.m_pMT);
        locDesc.MakeCopyConstructedPointer();

        pslIL->SetStubTargetArgType(&locDesc);              // native type is a pointer
        pslILDispatch->EmitLDARG(argidx);

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
    
    return LocalDesc(MscorlibBinder::GetClass(CLASS__ARG_ITERATOR));
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

void ILArgIteratorMarshaler::EmitMarshalArgumentCLRToNative()
{
    CONTRACTL
    {
        STANDARD_VM_CHECK;
        PRECONDITION(IsCLRToNative(m_dwMarshalFlags) && !IsByref(m_dwMarshalFlags));
    }
    CONTRACTL_END;

    EmitSetupSigAndDefaultHomesCLRToNative();
    
    //
    // marshal
    //

    // Allocate enough memory for va_list
    DWORD dwVaListSizeLocal = m_pcsMarshal->NewLocal(LocalDesc(ELEMENT_TYPE_U4));
    EmitLoadManagedHomeAddr(m_pcsMarshal);
    m_pcsMarshal->EmitCALL(METHOD__STUBHELPERS__CALC_VA_LIST_SIZE, 1, 1);
    m_pcsMarshal->EmitSTLOC(dwVaListSizeLocal);    
    m_pcsMarshal->EmitLDLOC(dwVaListSizeLocal);
    m_pcsMarshal->EmitLOCALLOC();
    EmitStoreNativeValue(m_pcsMarshal);
    
    // void MarshalToUnmanagedVaListInternal(cbVaListSize, va_list, VARARGS* data)
    EmitLoadNativeValue(m_pcsMarshal);
    m_pcsMarshal->EmitLDLOC(dwVaListSizeLocal);
    EmitLoadManagedHomeAddr(m_pcsMarshal);
    m_pcsMarshal->EmitCALL(METHOD__STUBHELPERS__MARSHAL_TO_UNMANAGED_VA_LIST_INTERNAL, 3, 0);
}

void ILArgIteratorMarshaler::EmitMarshalArgumentNativeToCLR()
{
    CONTRACTL
    {
        STANDARD_VM_CHECK;
        PRECONDITION(!IsCLRToNative(m_dwMarshalFlags) && !IsByref(m_dwMarshalFlags));
    }
    CONTRACTL_END;

    EmitSetupSigAndDefaultHomesNativeToCLR();
    
    EmitLoadNativeValue(m_pcsMarshal);
    EmitLoadManagedHomeAddr(m_pcsMarshal);

    // void MarshalToManagedVaList(va_list va, VARARGS *dataout)
    m_pcsMarshal->EmitCALL(METHOD__STUBHELPERS__MARSHAL_TO_MANAGED_VA_LIST_INTERNAL, 2, 0);    
}


LocalDesc ILArrayWithOffsetMarshaler::GetNativeType()
{
    LIMITED_METHOD_CONTRACT;
    
    return LocalDesc(ELEMENT_TYPE_I);
}

LocalDesc ILArrayWithOffsetMarshaler::GetManagedType()
{
    STANDARD_VM_CONTRACT;
    
    return LocalDesc(MscorlibBinder::GetClass(CLASS__ARRAY_WITH_OFFSET));
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
    
    int tokArrayWithOffset_m_array = pslILEmit->GetToken(MscorlibBinder::GetField(FIELD__ARRAY_WITH_OFFSET__M_ARRAY));
    int tokArrayWithOffset_m_count = pslILEmit->GetToken(MscorlibBinder::GetField(FIELD__ARRAY_WITH_OFFSET__M_COUNT));
    
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

    int tokArrayWithOffset_m_offset = pslILEmit->GetToken(MscorlibBinder::GetField(FIELD__ARRAY_WITH_OFFSET__M_OFFSET));

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
    pslILEmit->EmitCALL(METHOD__ARRAY__GET_RAW_ARRAY_DATA, 1, 1);
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

    int tokArrayWithOffset_m_array = pslILEmit->GetToken(MscorlibBinder::GetField(FIELD__ARRAY_WITH_OFFSET__M_ARRAY));

    ILCodeLabel* pNullRefLabel = pslILEmit->NewCodeLabel();

    EmitLoadManagedValue(pslILEmit);
    pslILEmit->EmitLDFLD(tokArrayWithOffset_m_array);
    pslILEmit->EmitBRFALSE(pNullRefLabel);
    
    EmitLoadManagedValue(pslILEmit);
    pslILEmit->EmitLDFLD(tokArrayWithOffset_m_array);
    pslILEmit->EmitSTLOC(m_dwPinnedLocalNum);

    pslILEmit->EmitLDLOC(m_dwPinnedLocalNum);
    pslILEmit->EmitCALL(METHOD__ARRAY__GET_RAW_ARRAY_DATA, 1, 1);
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

void ILAsAnyMarshalerBase::EmitMarshalArgumentCLRToNative()
{
    CONTRACTL
    {
        STANDARD_VM_CHECK;
        PRECONDITION(IsCLRToNative(m_dwMarshalFlags) && !IsByref(m_dwMarshalFlags));
        CONSISTENCY_CHECK(LOCAL_NUM_UNUSED == m_dwMarshalerLocalNum);
    }
    CONTRACTL_END;

    EmitSetupSigAndDefaultHomesCLRToNative();

    BYTE inout      = (IsIn(m_dwMarshalFlags) ? ML_IN : 0) | (IsOut(m_dwMarshalFlags) ? ML_OUT : 0);
    BYTE fIsAnsi    = IsAnsi() ? 1 : 0;
    BYTE fBestFit   = m_pargs->m_pMarshalInfo->GetBestFitMapping();
    BYTE fThrow     = m_pargs->m_pMarshalInfo->GetThrowOnUnmappableChar();

    DWORD dwFlags = 0;
    
    dwFlags |= inout    << 24;
    dwFlags |= fIsAnsi  << 16;
    dwFlags |= fThrow   <<  8;
    dwFlags |= fBestFit <<  0;

    //
    // marshal
    //

    LocalDesc marshalerType(MscorlibBinder::GetClass(CLASS__ASANY_MARSHALER));
    m_dwMarshalerLocalNum = m_pcsMarshal->NewLocal(marshalerType);
    DWORD dwTmpLocalNum = m_pcsMarshal->NewLocal(ELEMENT_TYPE_I);

    m_pcsMarshal->EmitLDC(sizeof(MngdNativeArrayMarshaler));
    m_pcsMarshal->EmitLOCALLOC();
    m_pcsMarshal->EmitSTLOC(dwTmpLocalNum);

    // marshaler = new AsAnyMarshaler(local_buffer)
    m_pcsMarshal->EmitLDLOCA(m_dwMarshalerLocalNum);
    m_pcsMarshal->EmitINITOBJ(m_pcsMarshal->GetToken(marshalerType.InternalToken));

    m_pcsMarshal->EmitLDLOCA(m_dwMarshalerLocalNum);
    m_pcsMarshal->EmitLDLOC(dwTmpLocalNum);
    m_pcsMarshal->EmitCALL(METHOD__ASANY_MARSHALER__CTOR, 2, 0);

    // nativeValue = marshaler.ConvertToNative(managedValue, flags);
    m_pcsMarshal->EmitLDLOCA(m_dwMarshalerLocalNum);
    EmitLoadManagedValue(m_pcsMarshal);
    m_pcsMarshal->EmitLDC(dwFlags);
    m_pcsMarshal->EmitCALL(METHOD__ASANY_MARSHALER__CONVERT_TO_NATIVE, 3, 1);
    EmitStoreNativeValue(m_pcsMarshal);

    //
    // unmarshal
    //
    if (IsOut(m_dwMarshalFlags))
    {
        // marshaler.ConvertToManaged(managedValue, nativeValue)
        m_pcsUnmarshal->EmitLDLOCA(m_dwMarshalerLocalNum);
        EmitLoadManagedValue(m_pcsUnmarshal);
        EmitLoadNativeValue(m_pcsUnmarshal);
        m_pcsUnmarshal->EmitCALL(METHOD__ASANY_MARSHALER__CONVERT_TO_MANAGED, 3, 0);
    }

    //
    // cleanup
    //
    EmitCleanupCLRToNativeTemp();
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
    pslILEmit->EmitLDLOCA(m_dwMarshalerLocalNum);
    EmitLoadNativeValue(pslILEmit);
    pslILEmit->EmitCALL(METHOD__ASANY_MARSHALER__CLEAR_NATIVE, 2, 0);
}

// we can get away with putting the GetManagedType and GetNativeType on ILMngdMarshaler because
// currently it is only used for reference marshaling where this is appropriate.  If it became
// used for something else, we would want to move this down in the inheritence tree..
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

bool ILNativeArrayMarshaler::UsePinnedArraySpecialCase()
{
    if (IsCLRToNative(m_dwMarshalFlags) && !IsByref(m_dwMarshalFlags) && (NULL == OleVariant::GetMarshalerForVarType(m_pargs->na.m_vt, TRUE)))
    {
        return true;
    }

    return false;
}

void ILNativeArrayMarshaler::EmitCreateMngdMarshaler(ILCodeStream* pslILEmit)
{
    STANDARD_VM_CONTRACT;

    if (UsePinnedArraySpecialCase())
    {
        return;
    }
            
    m_dwMngdMarshalerLocalNum = pslILEmit->NewLocal(ELEMENT_TYPE_I);
        
    pslILEmit->EmitLDC(sizeof(MngdNativeArrayMarshaler));
    pslILEmit->EmitLOCALLOC();
    pslILEmit->EmitSTLOC(m_dwMngdMarshalerLocalNum);

    CREATE_MARSHALER_CARRAY_OPERANDS mops;
    m_pargs->m_pMarshalInfo->GetMops(&mops);

    pslILEmit->EmitLDLOC(m_dwMngdMarshalerLocalNum);

    pslILEmit->EmitLDTOKEN(pslILEmit->GetToken(mops.methodTable));
    pslILEmit->EmitCALL(METHOD__RT_TYPE_HANDLE__GETVALUEINTERNAL, 1, 1);

    DWORD dwFlags = mops.elementType;
    dwFlags |= (((DWORD)mops.bestfitmapping)        << 16);
    dwFlags |= (((DWORD)mops.throwonunmappablechar) << 24);
    
    if (!IsCLRToNative(m_dwMarshalFlags) && IsOut(m_dwMarshalFlags) && IsIn(m_dwMarshalFlags))
    {
        // Unmanaged->managed in/out is the only case where we expect the native buffer to contain valid data.
        _ASSERTE((dwFlags & MngdNativeArrayMarshaler::FLAG_NATIVE_DATA_VALID) == 0);
        dwFlags |= MngdNativeArrayMarshaler::FLAG_NATIVE_DATA_VALID;
    }

    pslILEmit->EmitLDC(dwFlags);
    
    pslILEmit->EmitCALL(METHOD__MNGD_NATIVE_ARRAY_MARSHALER__CREATE_MARSHALER, 3, 0);
}


void ILNativeArrayMarshaler::EmitMarshalArgumentCLRToNative()
{
    CONTRACTL
    {
        STANDARD_VM_CHECK;
        PRECONDITION(IsCLRToNative(m_dwMarshalFlags) && !IsByref(m_dwMarshalFlags));
    }
    CONTRACTL_END;

    if (UsePinnedArraySpecialCase())
    {
        //
        // Replicate ML_PINNEDISOMORPHICARRAY_C2N_EXPRESS behavior -- note that this
        // gives in/out semantics "for free" even if the app doesn't specify one or
        // the other.  Since there is no enforcement of this, apps blithely depend
        // on it.  
        //

        EmitSetupSigAndDefaultHomesCLRToNative();

        LocalDesc managedType = GetManagedType();
        managedType.MakePinned();

        DWORD dwPinnedLocal = m_pcsMarshal->NewLocal(managedType);
        ILCodeLabel* pNullRefLabel = m_pcsMarshal->NewCodeLabel();

        m_pcsMarshal->EmitLoadNullPtr();
        EmitStoreNativeValue(m_pcsMarshal);

        EmitLoadManagedValue(m_pcsMarshal);
        m_pcsMarshal->EmitBRFALSE(pNullRefLabel);        

        // COMPAT: We cannot generate the same code that the C# compiler generates for
        // a fixed() statement on an array since we need to provide a non-null value
        // for a 0-length array. For compat reasons, we need to preserve old behavior.
        // Additionally, we need to ensure that we do not pass non-null for a zero-length
        // array when interacting with GDI/GDI+ since they fail on null arrays but succeed
        // on 0-length arrays.
        EmitLoadManagedValue(m_pcsMarshal);
        m_pcsMarshal->EmitSTLOC(dwPinnedLocal);
        m_pcsMarshal->EmitLDLOC(dwPinnedLocal);
        m_pcsMarshal->EmitCONV_I();
        // Optimize marshalling by emitting the data ptr offset directly into the IL stream
        // instead of doing an FCall to recalulate it each time when possible.
        m_pcsMarshal->EmitLDC(ArrayBase::GetDataPtrOffset(m_pargs->m_pMarshalInfo->GetArrayElementTypeHandle().MakeSZArray().GetMethodTable()));
        m_pcsMarshal->EmitADD();
        EmitStoreNativeValue(m_pcsMarshal);

        if (g_pConfig->InteropLogArguments())
        {
            m_pslNDirect->EmitLogNativeArgument(m_pcsMarshal, dwPinnedLocal);
        }

        m_pcsMarshal->EmitLabel(pNullRefLabel);
    }
    else
    {
        ILMngdMarshaler::EmitMarshalArgumentCLRToNative();
    }
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
            int lcidParamIdx = m_pslNDirect->GetLCIDParamIdx();
    
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

void ILNativeArrayMarshaler::EmitNewSavedSizeArgLocal()
{
    STANDARD_VM_CONTRACT;

    _ASSERTE(m_dwSavedSizeArg == LOCAL_NUM_UNUSED);
    ILCodeStream *pcsSetup = m_pslNDirect->GetSetupCodeStream();
    m_dwSavedSizeArg = pcsSetup->NewLocal(ELEMENT_TYPE_I4);
    pcsSetup->EmitLDC(0);
    pcsSetup->EmitSTLOC(m_dwSavedSizeArg);
}

void ILNativeArrayMarshaler::EmitMarshalArgumentNativeToCLRByref()
{
    STANDARD_VM_CONTRACT;

    if (IsByref(m_dwMarshalFlags))
    {
        EmitNewSavedSizeArgLocal();
    }
    
    ILMngdMarshaler::EmitMarshalArgumentNativeToCLRByref();
}

void ILNativeArrayMarshaler::EmitMarshalArgumentCLRToNativeByref()
{
    STANDARD_VM_CONTRACT;

    if (IsByref(m_dwMarshalFlags))
    {
        EmitNewSavedSizeArgLocal();
    }
    
    ILMngdMarshaler::EmitMarshalArgumentCLRToNativeByref();
}


#ifndef CROSSGEN_COMPILE

FCIMPL3(void, MngdNativeArrayMarshaler::CreateMarshaler, MngdNativeArrayMarshaler* pThis, MethodTable* pMT, UINT32 dwFlags)
{
    FCALL_CONTRACT;

    // Don't check whether the input values are negative - passing negative size-controlling
    // arguments and compensating them with a positive SizeConst has always worked.
    pThis->m_pElementMT            = pMT;
    pThis->m_vt                    = (VARTYPE)(dwFlags);
    pThis->m_NativeDataValid       = (BYTE)((dwFlags & FLAG_NATIVE_DATA_VALID) != 0);
    dwFlags &= ~FLAG_NATIVE_DATA_VALID;
    pThis->m_BestFitMap            = (BYTE)(dwFlags >> 16);
    pThis->m_ThrowOnUnmappableChar = (BYTE)(dwFlags >> 24);
    pThis->m_Array                 = TypeHandle();
}
FCIMPLEND

FCIMPL3(void, MngdNativeArrayMarshaler::ConvertSpaceToNative, MngdNativeArrayMarshaler* pThis, OBJECTREF* pManagedHome, void** pNativeHome)
{
    FCALL_CONTRACT;

    HELPER_METHOD_FRAME_BEGIN_0();
   
    BASEARRAYREF arrayRef = (BASEARRAYREF) *pManagedHome;

    if (arrayRef == NULL)
    {
        *pNativeHome = NULL;
    }
    else
    {
        SIZE_T cElements = arrayRef->GetNumComponents();
        SIZE_T cbElement = OleVariant::GetElementSizeForVarType(pThis->m_vt, pThis->m_pElementMT);

        if (cbElement == 0)
            COMPlusThrow(kArgumentException, IDS_EE_COM_UNSUPPORTED_SIG);

        SIZE_T cbArray = cElements;
        if ( (!SafeMulSIZE_T(&cbArray, cbElement)) || cbArray > MAX_SIZE_FOR_INTEROP)
            COMPlusThrow(kArgumentException, IDS_EE_STRUCTARRAYTOOLARGE);

        *pNativeHome = CoTaskMemAlloc(cbArray);
        if (*pNativeHome == NULL)
            ThrowOutOfMemory();

        // initialize the array
        FillMemory(*pNativeHome, cbArray, 0);
    }

    HELPER_METHOD_FRAME_END();
}
FCIMPLEND
    
FCIMPL3(void, MngdNativeArrayMarshaler::ConvertContentsToNative, MngdNativeArrayMarshaler* pThis, OBJECTREF* pManagedHome, void** pNativeHome)
{
    FCALL_CONTRACT;

    HELPER_METHOD_FRAME_BEGIN_0();

    BASEARRAYREF* pArrayRef = (BASEARRAYREF *) pManagedHome;
    
    if (*pArrayRef != NULL)
    {
        const OleVariant::Marshaler* pMarshaler = OleVariant::GetMarshalerForVarType(pThis->m_vt, TRUE);
        SIZE_T cElements = (*pArrayRef)->GetNumComponents();
        if (pMarshaler == NULL || pMarshaler->ComToOleArray == NULL)
        {
            if ( (!SafeMulSIZE_T(&cElements, OleVariant::GetElementSizeForVarType(pThis->m_vt, pThis->m_pElementMT))) || cElements > MAX_SIZE_FOR_INTEROP)
                COMPlusThrow(kArgumentException, IDS_EE_STRUCTARRAYTOOLARGE);
    
            _ASSERTE(!GetTypeHandleForCVType(OleVariant::GetCVTypeForVarType(pThis->m_vt)).GetMethodTable()->ContainsPointers());
            memcpyNoGCRefs(*pNativeHome, (*pArrayRef)->GetDataPtr(), cElements);
        }
        else
        {
            pMarshaler->ComToOleArray(pArrayRef, *pNativeHome, pThis->m_pElementMT, pThis->m_BestFitMap, 
                                      pThis->m_ThrowOnUnmappableChar, pThis->m_NativeDataValid, cElements);
        }
    }
    HELPER_METHOD_FRAME_END();
}
FCIMPLEND
    
FCIMPL4(void, MngdNativeArrayMarshaler::ConvertSpaceToManaged, MngdNativeArrayMarshaler* pThis,
        OBJECTREF* pManagedHome, void** pNativeHome, INT32 cElements)
{
    FCALL_CONTRACT;

    HELPER_METHOD_FRAME_BEGIN_0();

    if (*pNativeHome == NULL)
    {
        SetObjectReference(pManagedHome, NULL);
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
        SetObjectReference(pManagedHome, AllocateSzArray(pThis->m_Array, cElements));
    }    
    HELPER_METHOD_FRAME_END();
}
FCIMPLEND
    
FCIMPL3(void, MngdNativeArrayMarshaler::ConvertContentsToManaged, MngdNativeArrayMarshaler* pThis, OBJECTREF* pManagedHome, void** pNativeHome)
{
    FCALL_CONTRACT;

    HELPER_METHOD_FRAME_BEGIN_0();
    
    if (*pNativeHome != NULL)
    {
        const OleVariant::Marshaler *pMarshaler = OleVariant::GetMarshalerForVarType(pThis->m_vt, TRUE);
    
        BASEARRAYREF* pArrayRef = (BASEARRAYREF*) pManagedHome;
    
        if (pMarshaler == NULL || pMarshaler->OleToComArray == NULL)
        {
            SIZE_T cElements = (*pArrayRef)->GetNumComponents();
            if ( (!SafeMulSIZE_T(&cElements, OleVariant::GetElementSizeForVarType(pThis->m_vt, pThis->m_pElementMT))) || cElements > MAX_SIZE_FOR_INTEROP)
                COMPlusThrow(kArgumentException, IDS_EE_STRUCTARRAYTOOLARGE);
    
                // If we are copying variants, strings, etc, we need to use write barrier
            _ASSERTE(!GetTypeHandleForCVType(OleVariant::GetCVTypeForVarType(pThis->m_vt)).GetMethodTable()->ContainsPointers());
            memcpyNoGCRefs((*pArrayRef)->GetDataPtr(), *pNativeHome, cElements);
        }
        else
        {
            pMarshaler->OleToComArray(*pNativeHome, pArrayRef, pThis->m_pElementMT);
        }
    }
    
    HELPER_METHOD_FRAME_END();
}
FCIMPLEND

FCIMPL3(void, MngdNativeArrayMarshaler::ClearNative, MngdNativeArrayMarshaler* pThis, void** pNativeHome, INT32 cElements)
{
    FCALL_CONTRACT;

    HELPER_METHOD_FRAME_BEGIN_0();
    
    if (*pNativeHome != NULL)
    {
        DoClearNativeContents(pThis, pNativeHome, cElements);
        CoTaskMemFree(*pNativeHome);
    }
    
    HELPER_METHOD_FRAME_END();
}
FCIMPLEND
    
FCIMPL3(void, MngdNativeArrayMarshaler::ClearNativeContents, MngdNativeArrayMarshaler* pThis, void** pNativeHome, INT32 cElements)
{
    FCALL_CONTRACT;

    HELPER_METHOD_FRAME_BEGIN_0();

    DoClearNativeContents(pThis, pNativeHome, cElements);
    
    HELPER_METHOD_FRAME_END();
}
FCIMPLEND

void MngdNativeArrayMarshaler::DoClearNativeContents(MngdNativeArrayMarshaler* pThis, void** pNativeHome, INT32 cElements)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;
    
    if (*pNativeHome != NULL)
    {
        const OleVariant::Marshaler *pMarshaler = OleVariant::GetMarshalerForVarType(pThis->m_vt, FALSE);

        if (pMarshaler != NULL && pMarshaler->ClearOleArray != NULL)
        {
            pMarshaler->ClearOleArray(*pNativeHome, cElements, pThis->m_pElementMT);
        }
    }
}

#endif // CROSSGEN_COMPILE


#ifdef FEATURE_COMINTEROP
void ILSafeArrayMarshaler::EmitCreateMngdMarshaler(ILCodeStream* pslILEmit)
{
    STANDARD_VM_CONTRACT;

    m_dwMngdMarshalerLocalNum = pslILEmit->NewLocal(ELEMENT_TYPE_I);
        
    pslILEmit->EmitLDC(sizeof(MngdSafeArrayMarshaler));
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
    pslILEmit->EmitCALL(METHOD__RT_TYPE_HANDLE__GETVALUEINTERNAL, 1, 1);
    pslILEmit->EmitLDC(m_pargs->m_pMarshalInfo->GetArrayRank());
    pslILEmit->EmitLDC(dwFlags);

    pslILEmit->EmitCALL(METHOD__MNGD_SAFE_ARRAY_MARSHALER__CREATE_MARSHALER, 4, 0);
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


#ifndef CROSSGEN_COMPILE

FCIMPL4(void, MngdSafeArrayMarshaler::CreateMarshaler, MngdSafeArrayMarshaler* pThis, MethodTable* pMT, UINT32 iRank, UINT32 dwFlags)
{
    FCALL_CONTRACT;

    pThis->m_pElementMT    = pMT;
    pThis->m_iRank         = iRank;
    pThis->m_vt            = (VARTYPE)dwFlags;
    pThis->m_fStatic       = (BYTE)(dwFlags >> 16);
    pThis->m_nolowerbounds = (BYTE)(dwFlags >> 24);
}
FCIMPLEND

FCIMPL3(void, MngdSafeArrayMarshaler::ConvertSpaceToNative, MngdSafeArrayMarshaler* pThis, OBJECTREF* pManagedHome, void** pNativeHome)
{
    FCALL_CONTRACT;

    if (pThis->m_fStatic & SCSF_IsStatic)
        return;
    
    HELPER_METHOD_FRAME_BEGIN_0();

    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(pThis->m_vt != VT_EMPTY);
        PRECONDITION(CheckPointer(pThis->m_pElementMT));
    }
    CONTRACTL_END;
    
    if (*pManagedHome != NULL)
    {
        *pNativeHome = (void *) OleVariant::CreateSafeArrayForArrayRef((BASEARRAYREF*) pManagedHome, pThis->m_vt, pThis->m_pElementMT);
    }
    else
    {
        *pNativeHome = NULL;
    }

    HELPER_METHOD_FRAME_END();
}
FCIMPLEND
    
FCIMPL4(void, MngdSafeArrayMarshaler::ConvertContentsToNative, MngdSafeArrayMarshaler* pThis, OBJECTREF* pManagedHome, void** pNativeHome, Object* pOriginalManagedUNSAFE)
{
    CONTRACTL
    {
        FCALL_CHECK;
        PRECONDITION(pThis->m_vt != VT_EMPTY);
        PRECONDITION(CheckPointer(pThis->m_pElementMT));
    }
    CONTRACTL_END;

    OBJECTREF pOriginalManaged = ObjectToOBJECTREF(pOriginalManagedUNSAFE);
    HELPER_METHOD_FRAME_BEGIN_1(pOriginalManaged);

    if ((pThis->m_fStatic & SCSF_IsStatic) &&
        (*pManagedHome != pOriginalManaged))
    {
        COMPlusThrow(kInvalidOperationException, IDS_INVALID_REDIM);
    }
   
    if (*pManagedHome != NULL)
    {
        OleVariant::MarshalSafeArrayForArrayRef((BASEARRAYREF *) pManagedHome,
                                                (SAFEARRAY*)*pNativeHome,
                                                pThis->m_vt,
                                                pThis->m_pElementMT,
                                                (pThis->m_fStatic & SCSF_NativeDataValid));
    }

    HELPER_METHOD_FRAME_END();
}
FCIMPLEND
    
FCIMPL3(void, MngdSafeArrayMarshaler::ConvertSpaceToManaged, MngdSafeArrayMarshaler* pThis, OBJECTREF* pManagedHome, void** pNativeHome)
{
    CONTRACTL
    {
        FCALL_CHECK;
        PRECONDITION(pThis->m_vt != VT_EMPTY);
        PRECONDITION(CheckPointer(pThis->m_pElementMT));
    }
    CONTRACTL_END;
    
    HELPER_METHOD_FRAME_BEGIN_0();

    if (*pNativeHome != NULL)
    {
        // If the managed array has a rank defined then make sure the rank of the
        // SafeArray matches the defined rank.
        if (pThis->m_iRank != -1)
        {
            int iSafeArrayRank = SafeArrayGetDim((SAFEARRAY*) *pNativeHome);
            if (pThis->m_iRank != iSafeArrayRank)
            {                    
                WCHAR strExpectedRank[64];
                WCHAR strActualRank[64];
                _ltow_s(pThis->m_iRank, strExpectedRank, COUNTOF(strExpectedRank), 10);
                _ltow_s(iSafeArrayRank, strActualRank, COUNTOF(strActualRank), 10);
                COMPlusThrow(kSafeArrayRankMismatchException, IDS_EE_SAFEARRAYRANKMISMATCH, strActualRank, strExpectedRank);
            }
        }
    
        if (pThis->m_nolowerbounds)
        {
            LONG lowerbound;
            if ( (SafeArrayGetDim( (SAFEARRAY*)*pNativeHome ) != 1) ||
                 (FAILED(SafeArrayGetLBound( (SAFEARRAY*)*pNativeHome, 1, &lowerbound))) ||
                 lowerbound != 0 )
            {
                COMPlusThrow(kSafeArrayRankMismatchException, IDS_EE_SAFEARRAYSZARRAYMISMATCH);
            }
        }
    
        SetObjectReference(pManagedHome,
            (OBJECTREF) OleVariant::CreateArrayRefForSafeArray((SAFEARRAY*) *pNativeHome,
                                                            pThis->m_vt,
                                                            pThis->m_pElementMT));
    }
    else
    {
        SetObjectReference(pManagedHome, NULL);
    }

    HELPER_METHOD_FRAME_END();
}
FCIMPLEND
    
FCIMPL3(void, MngdSafeArrayMarshaler::ConvertContentsToManaged, MngdSafeArrayMarshaler* pThis, OBJECTREF* pManagedHome, void** pNativeHome)
{
    CONTRACTL
    {
        FCALL_CHECK;
        PRECONDITION(pThis->m_vt != VT_EMPTY);
        PRECONDITION(CheckPointer(pThis->m_pElementMT));
    }
    CONTRACTL_END;

    SAFEARRAY* pNative = *(SAFEARRAY**)pNativeHome;
    HELPER_METHOD_FRAME_BEGIN_0();

    if (pNative && pNative->fFeatures & FADF_STATIC)
    {
        pThis->m_fStatic |= SCSF_IsStatic;
    }

    if (*pNativeHome != NULL)
    {
        OleVariant::MarshalArrayRefForSafeArray((SAFEARRAY*)*pNativeHome,
                                                (BASEARRAYREF *) pManagedHome,
                                                pThis->m_vt,
                                                pThis->m_pElementMT);
    }
    
    HELPER_METHOD_FRAME_END();
}
FCIMPLEND
    
FCIMPL3(void, MngdSafeArrayMarshaler::ClearNative, MngdSafeArrayMarshaler* pThis, OBJECTREF* pManagedHome, void** pNativeHome)
{
    FCALL_CONTRACT;

    if (pThis->m_fStatic & SCSF_IsStatic)
        return;

    HELPER_METHOD_FRAME_BEGIN_0();

    if (*pNativeHome != NULL)
    {
        GCX_PREEMP();
        _ASSERTE(GetModuleHandleA("oleaut32.dll") != NULL);
        // SafeArray has been created.  Oleaut32.dll must have been loaded.
        CONTRACT_VIOLATION(ThrowsViolation);
        SafeArrayDestroy((SAFEARRAY*)*pNativeHome);
    }
    
    HELPER_METHOD_FRAME_END();
}
FCIMPLEND

#endif // CROSSGEN_COMPILE


LocalDesc ILHiddenLengthArrayMarshaler::GetNativeType()
{
    LIMITED_METHOD_CONTRACT;
    return LocalDesc(ELEMENT_TYPE_I);
}

LocalDesc ILHiddenLengthArrayMarshaler::GetManagedType()
{
    LIMITED_METHOD_CONTRACT;

    return LocalDesc(ELEMENT_TYPE_OBJECT);
}

void ILHiddenLengthArrayMarshaler::EmitCreateMngdMarshaler(ILCodeStream* pslILEmit)
{
    STANDARD_VM_CONTRACT;

    if (!CanUsePinnedArray())
    {
        m_dwMngdMarshalerLocalNum = pslILEmit->NewLocal(ELEMENT_TYPE_I);
        
        pslILEmit->EmitLDC(sizeof(MngdHiddenLengthArrayMarshaler));
        pslILEmit->EmitLOCALLOC();
        pslILEmit->EmitSTLOC(m_dwMngdMarshalerLocalNum);

        MethodTable *pElementMT = m_pargs->m_pMarshalInfo->GetArrayElementTypeHandle().GetMethodTable();
        pslILEmit->EmitLDLOC(m_dwMngdMarshalerLocalNum);
        pslILEmit->EmitLDTOKEN(pslILEmit->GetToken(pElementMT));
        pslILEmit->EmitCALL(METHOD__RT_TYPE_HANDLE__GETVALUEINTERNAL, 1, 1);

        pslILEmit->EmitLDC(m_pargs->na.m_cbElementSize);
        pslILEmit->EmitLDC(m_pargs->na.m_vt);

        pslILEmit->EmitCALL(METHOD__MNGD_HIDDEN_LENGTH_ARRAY_MARSHALER__CREATE_MARSHALER, 4, 0);
    }
}

void ILHiddenLengthArrayMarshaler::EmitMarshalArgumentCLRToNative()
{
    STANDARD_VM_CONTRACT;

    // If we can pin the array, then do that rather than marshaling it in a more heavy weight way
    // Otherwise, fall back to doing a full marshal
    if (CanUsePinnedArray())
    {
        EmitSetupSigAndDefaultHomesCLRToNative();

        LocalDesc managedType = GetManagedType();
        managedType.MakePinned();
        DWORD dwPinnedLocal = m_pcsMarshal->NewLocal(managedType);

        ILCodeLabel* pMarshalDoneLabel = m_pcsMarshal->NewCodeLabel();

        // native = NULL
        m_pcsMarshal->EmitLoadNullPtr();
        EmitStoreNativeValue(m_pcsMarshal);

        // if (managed == null) goto MarshalDone
        EmitLoadManagedValue(m_pcsMarshal);
        m_pcsMarshal->EmitBRFALSE(pMarshalDoneLabel);

        // pinnedLocal = managed;
        EmitLoadManagedValue(m_pcsMarshal);
        m_pcsMarshal->EmitSTLOC(dwPinnedLocal);

        // native = pinnedLocal + dataOffset

        // COMPAT: We cannot generate the same code that the C# compiler generates for
        // a fixed() statement on an array since we need to provide a non-null value
        // for a 0-length array. For compat reasons, we need to preserve old behavior.
        EmitLoadManagedValue(m_pcsMarshal);
        m_pcsMarshal->EmitSTLOC(dwPinnedLocal);
        m_pcsMarshal->EmitLDLOC(dwPinnedLocal);
        m_pcsMarshal->EmitCONV_I();
        // Optimize marshalling by emitting the data ptr offset directly into the IL stream
        // instead of doing an FCall to recalulate it each time.
        m_pcsMarshal->EmitLDC(ArrayBase::GetDataPtrOffset(m_pargs->m_pMarshalInfo->GetArrayElementTypeHandle().MakeSZArray().GetMethodTable()));
        m_pcsMarshal->EmitADD();
        EmitStoreNativeValue(m_pcsMarshal);

        if (g_pConfig->InteropLogArguments())
        {
            m_pslNDirect->EmitLogNativeArgument(m_pcsMarshal, dwPinnedLocal);
        }

        // MarshalDone:
        m_pcsMarshal->EmitLabel(pMarshalDoneLabel);
    }
    else
    {
        ILMngdMarshaler::EmitMarshalArgumentCLRToNative();
    }

}

void ILHiddenLengthArrayMarshaler::EmitConvertSpaceNativeToCLR(ILCodeStream* pslILEmit)
{
    STANDARD_VM_CONTRACT;

    if (!CanUsePinnedArray())
    {
        EmitLoadMngdMarshaler(pslILEmit);
        EmitLoadManagedHomeAddr(pslILEmit);
        EmitLoadNativeHomeAddr(pslILEmit);
        EmitLoadNativeArrayLength(pslILEmit);
        
        // MngdHiddenLengthArrayMarshaler::ConvertSpaceToManaged
        pslILEmit->EmitCALL(pslILEmit->GetToken(GetConvertSpaceToManagedMethod()), 4, 0);
    }
}

void ILHiddenLengthArrayMarshaler::EmitConvertSpaceCLRToNative(ILCodeStream* pslILEmit)
{
    STANDARD_VM_CONTRACT;

    // If we're marshaling out to native code, then we need to set the length out parameter
    if (!IsCLRToNative(m_dwMarshalFlags))
    {
        if (IsByref(m_dwMarshalFlags) || IsRetval(m_dwMarshalFlags) || IsOut(m_dwMarshalFlags))
        {
            ILCodeLabel *pSkipGetLengthLabel = m_pcsMarshal->NewCodeLabel();

            // nativeLen = 0
            pslILEmit->EmitLDC(0);
            pslILEmit->EmitCONV_T(m_pargs->m_pMarshalInfo->GetHiddenLengthParamElementType());
            pslILEmit->EmitSTLOC(m_pargs->m_pMarshalInfo->GetHiddenLengthNativeHome());

            // if (array == null) goto SkipGetLength
            EmitLoadManagedValue(pslILEmit);
            pslILEmit->EmitBRFALSE(pSkipGetLengthLabel);

            // nativeLen = array.Length
            // SkipGetLength:
            EmitLoadManagedValue(pslILEmit);
            pslILEmit->EmitLDLEN();
            pslILEmit->EmitCONV_T(m_pargs->m_pMarshalInfo->GetHiddenLengthParamElementType());
            pslILEmit->EmitSTLOC(m_pargs->m_pMarshalInfo->GetHiddenLengthNativeHome());
            pslILEmit->EmitLabel(pSkipGetLengthLabel);

            // nativeLenParam = nativeLen
            LocalDesc nativeParamType(m_pargs->m_pMarshalInfo->GetHiddenLengthParamElementType());
            pslILEmit->EmitLDARG(m_pargs->m_pMarshalInfo->HiddenLengthParamIndex());
            pslILEmit->EmitLDLOC(m_pargs->m_pMarshalInfo->GetHiddenLengthNativeHome());
            pslILEmit->EmitSTIND_T(&nativeParamType);
        }
    }

    if (!CanUsePinnedArray())
    {
        ILMngdMarshaler::EmitConvertSpaceCLRToNative(pslILEmit);
    }
}

void ILHiddenLengthArrayMarshaler::EmitConvertContentsCLRToNative(ILCodeStream* pslILEmit)
{
    STANDARD_VM_CONTRACT;

    if (!CanUsePinnedArray())
    {
        if (m_pargs->na.m_vt == VTHACK_REDIRECTEDTYPE &&
            (m_pargs->na.m_redirectedTypeIndex == WinMDAdapter::RedirectedTypeIndex_System_Uri ||
             m_pargs->na.m_redirectedTypeIndex == WinMDAdapter::RedirectedTypeIndex_System_Collections_Specialized_NotifyCollectionChangedEventArgs ||
             m_pargs->na.m_redirectedTypeIndex == WinMDAdapter::RedirectedTypeIndex_System_ComponentModel_PropertyChangedEventArgs))
        {
            // System.Uri/NotifyCollectionChangedEventArgs don't live in mscorlib so there's no marshaling helper to call - inline the loop
            DWORD dwLoopCounterLocalNum = pslILEmit->NewLocal(ELEMENT_TYPE_I4);
            DWORD dwNativePtrLocalNum = pslILEmit->NewLocal(ELEMENT_TYPE_I);
            ILCodeLabel *pConditionLabel = pslILEmit->NewCodeLabel();
            ILCodeLabel *pLoopBodyLabel = pslILEmit->NewCodeLabel();

            // for (IntPtr ptr = pNative, int i = 0; ...
            pslILEmit->EmitLDC(0);
            pslILEmit->EmitSTLOC(dwLoopCounterLocalNum);
            EmitLoadNativeValue(pslILEmit);
            pslILEmit->EmitSTLOC(dwNativePtrLocalNum);
            pslILEmit->EmitBR(pConditionLabel);

            // *ptr = EmitConvertCLR*ToWinRT*(pManaged[i]);
            pslILEmit->EmitLabel(pLoopBodyLabel);
            pslILEmit->EmitLDLOC(dwNativePtrLocalNum);
            EmitLoadManagedValue(pslILEmit);
            pslILEmit->EmitLDLOC(dwLoopCounterLocalNum);
            pslILEmit->EmitLDELEM_REF();

            switch (m_pargs->na.m_redirectedTypeIndex)
            {
                case WinMDAdapter::RedirectedTypeIndex_System_Uri:
                    ILUriMarshaler::EmitConvertCLRUriToWinRTUri(pslILEmit, m_pargs->m_pMarshalInfo->GetModule()->GetLoaderAllocator());
                    break;

                case WinMDAdapter::RedirectedTypeIndex_System_Collections_Specialized_NotifyCollectionChangedEventArgs:
                    ILNCCEventArgsMarshaler::EmitConvertCLREventArgsToWinRTEventArgs(pslILEmit, m_pargs->m_pMarshalInfo->GetModule()->GetLoaderAllocator());
                    break;

                case WinMDAdapter::RedirectedTypeIndex_System_ComponentModel_PropertyChangedEventArgs:
                    ILPCEventArgsMarshaler::EmitConvertCLREventArgsToWinRTEventArgs(pslILEmit, m_pargs->m_pMarshalInfo->GetModule()->GetLoaderAllocator());
                    break;

                default: UNREACHABLE();
            }

            pslILEmit->EmitSTIND_I();

            // ... i++, ptr += IntPtr.Size ...
            pslILEmit->EmitLDLOC(dwLoopCounterLocalNum);
            pslILEmit->EmitLDC(1);
            pslILEmit->EmitADD();
            pslILEmit->EmitSTLOC(dwLoopCounterLocalNum);
            pslILEmit->EmitLDLOC(dwNativePtrLocalNum);
            pslILEmit->EmitLDC(sizeof(LPVOID));
            pslILEmit->EmitADD();
            pslILEmit->EmitSTLOC(dwNativePtrLocalNum);

            // ... i < pManaged.Length; ...
            pslILEmit->EmitLabel(pConditionLabel);
            pslILEmit->EmitLDLOC(dwLoopCounterLocalNum);
            EmitLoadNativeArrayLength(pslILEmit);
            pslILEmit->EmitBLT(pLoopBodyLabel);
        }            
        else
        {
            ILMngdMarshaler::EmitConvertContentsCLRToNative(pslILEmit);
        }
    }
}

void ILHiddenLengthArrayMarshaler::EmitConvertContentsNativeToCLR(ILCodeStream* pslILEmit)
{
    STANDARD_VM_CONTRACT;

    if (!CanUsePinnedArray())
    {
        if (m_pargs->na.m_vt == VTHACK_REDIRECTEDTYPE &&
            (m_pargs->na.m_redirectedTypeIndex == WinMDAdapter::RedirectedTypeIndex_System_Uri ||
             m_pargs->na.m_redirectedTypeIndex == WinMDAdapter::RedirectedTypeIndex_System_Collections_Specialized_NotifyCollectionChangedEventArgs ||
             m_pargs->na.m_redirectedTypeIndex == WinMDAdapter::RedirectedTypeIndex_System_ComponentModel_PropertyChangedEventArgs))
        {
            // System.Uri/NotifyCollectionChangedEventArgs don't live in mscorlib so there's no marshaling helper to call - inline the loop
            DWORD dwLoopCounterLocalNum = pslILEmit->NewLocal(ELEMENT_TYPE_I4);
            DWORD dwNativePtrLocalNum = pslILEmit->NewLocal(ELEMENT_TYPE_I);
            ILCodeLabel *pConditionLabel = pslILEmit->NewCodeLabel();
            ILCodeLabel *pLoopBodyLabel = pslILEmit->NewCodeLabel();

            // for (IntPtr ptr = pNative, int i = 0; ...
            pslILEmit->EmitLDC(0);
            pslILEmit->EmitSTLOC(dwLoopCounterLocalNum);
            EmitLoadNativeValue(pslILEmit);
            pslILEmit->EmitSTLOC(dwNativePtrLocalNum);
            pslILEmit->EmitBR(pConditionLabel);

            // pManaged[i] = EmitConvertWinRT*ToCLR*(*ptr);
            pslILEmit->EmitLabel(pLoopBodyLabel);
            EmitLoadManagedValue(pslILEmit);
            pslILEmit->EmitLDLOC(dwLoopCounterLocalNum);
            pslILEmit->EmitLDLOC(dwNativePtrLocalNum);
            pslILEmit->EmitLDIND_I();

            switch (m_pargs->na.m_redirectedTypeIndex)
            {
                case WinMDAdapter::RedirectedTypeIndex_System_Uri:
                    ILUriMarshaler::EmitConvertWinRTUriToCLRUri(pslILEmit, m_pargs->m_pMarshalInfo->GetModule()->GetLoaderAllocator());
                    break;

                case WinMDAdapter::RedirectedTypeIndex_System_Collections_Specialized_NotifyCollectionChangedEventArgs:
                    ILNCCEventArgsMarshaler::EmitConvertWinRTEventArgsToCLREventArgs(pslILEmit, m_pargs->m_pMarshalInfo->GetModule()->GetLoaderAllocator());
                    break;

                case WinMDAdapter::RedirectedTypeIndex_System_ComponentModel_PropertyChangedEventArgs:
                    ILPCEventArgsMarshaler::EmitConvertWinRTEventArgsToCLREventArgs(pslILEmit, m_pargs->m_pMarshalInfo->GetModule()->GetLoaderAllocator());
                    break;

                default: UNREACHABLE();
            }
            
            pslILEmit->EmitSTELEM_REF();

            // ... i++, ptr += IntPtr.Size)
            pslILEmit->EmitLDLOC(dwLoopCounterLocalNum);
            pslILEmit->EmitLDC(1);
            pslILEmit->EmitADD();
            pslILEmit->EmitSTLOC(dwLoopCounterLocalNum);
            pslILEmit->EmitLDLOC(dwNativePtrLocalNum);
            pslILEmit->EmitLDC(sizeof(LPVOID));
            pslILEmit->EmitADD();
            pslILEmit->EmitSTLOC(dwNativePtrLocalNum);

            // ... i < pManaged.Length; ...
            pslILEmit->EmitLabel(pConditionLabel);
            pslILEmit->EmitLDLOC(dwLoopCounterLocalNum);
            EmitLoadNativeArrayLength(pslILEmit);
            pslILEmit->EmitBLT(pLoopBodyLabel);
        }            
        else
        {
            ILMngdMarshaler::EmitConvertContentsNativeToCLR(pslILEmit);
        }
    }
}

void ILHiddenLengthArrayMarshaler::EmitClearNative(ILCodeStream* pslILEmit)
{
    STANDARD_VM_CONTRACT;

    EmitClearNativeContents(pslILEmit);

    if (!CanUsePinnedArray())
    {
        EmitLoadNativeValue(pslILEmit);
        pslILEmit->EmitCALL(pslILEmit->GetToken(GetClearNativeMethod()), 1, 0);
    }
}

void ILHiddenLengthArrayMarshaler::EmitClearNativeContents(ILCodeStream* pslILEmit)
{
    STANDARD_VM_CONTRACT;

    if (!CanUsePinnedArray())
    {
        MethodDesc *pMD = GetClearNativeContentsMethod();
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

            EmitLoadNativeHomeAddr(pslILEmit);
            EmitLoadNativeArrayLength(pslILEmit);
            pslILEmit->EmitCALL(pslILEmit->GetToken(pMD), numArgs, 0);
        }
    }
}

// Determine if we can simply pin the managed array, rather than doing a full marshal
bool ILHiddenLengthArrayMarshaler::CanUsePinnedArray()
{
    STANDARD_VM_CONTRACT;

    // If the array is only going from managed to native, and it contains only blittable data, and
    // we know where that data is located in the array then we can take the fast path
    if (!IsCLRToNative(m_dwMarshalFlags))
    {
        return false;
    }

    if (m_pargs->na.m_vt != VTHACK_BLITTABLERECORD)
    {
        return false;
    }

    if (IsByref(m_dwMarshalFlags))
    {
        return false;
    }

    if (!IsIn(m_dwMarshalFlags))
    {
        return false;
    }

    if (IsRetval(m_dwMarshalFlags))
    {
        return false;
    }

    return true;
}

void ILHiddenLengthArrayMarshaler::EmitLoadNativeArrayLength(ILCodeStream *pslILEmit)
{
    STANDARD_VM_CONTRACT;

    // For return values, the native length won't yet be marshaled back to its managed home
    // so it needs to be read directly
    if (IsRetval(m_dwMarshalFlags))
    {
        pslILEmit->EmitLDLOC(m_pargs->m_pMarshalInfo->GetHiddenLengthNativeHome());
    }
    else
    {
        pslILEmit->EmitLDLOC(m_pargs->m_pMarshalInfo->GetHiddenLengthManagedHome());
    }

    pslILEmit->EmitCONV_OVF_I4();
}

MethodDesc *ILHiddenLengthArrayMarshaler::GetConvertContentsToManagedMethod()
{
    STANDARD_VM_CONTRACT;

    if (m_pargs->na.m_vt == VTHACK_REDIRECTEDTYPE)
    {
        switch (m_pargs->na.m_redirectedTypeIndex)
        {
            case WinMDAdapter::RedirectedTypeIndex_System_DateTimeOffset:
                return MscorlibBinder::GetMethod(METHOD__MNGD_HIDDEN_LENGTH_ARRAY_MARSHALER__CONVERT_CONTENTS_TO_MANAGED_DATETIME);

            case WinMDAdapter::RedirectedTypeIndex_System_Type:
                return MscorlibBinder::GetMethod(METHOD__MNGD_HIDDEN_LENGTH_ARRAY_MARSHALER__CONVERT_CONTENTS_TO_MANAGED_TYPE);

            case WinMDAdapter::RedirectedTypeIndex_System_Exception:
                return MscorlibBinder::GetMethod(METHOD__MNGD_HIDDEN_LENGTH_ARRAY_MARSHALER__CONVERT_CONTENTS_TO_MANAGED_EXCEPTION);

            case WinMDAdapter::RedirectedTypeIndex_System_Nullable:
            {
                MethodDesc *pMD = MscorlibBinder::GetMethod(METHOD__MNGD_HIDDEN_LENGTH_ARRAY_MARSHALER__CONVERT_CONTENTS_TO_MANAGED_NULLABLE);
                return GetExactMarshalerMethod(pMD);
            }

            case WinMDAdapter::RedirectedTypeIndex_System_Collections_Generic_KeyValuePair:
            {
                MethodDesc *pMD = MscorlibBinder::GetMethod(METHOD__MNGD_HIDDEN_LENGTH_ARRAY_MARSHALER__CONVERT_CONTENTS_TO_MANAGED_KEYVALUEPAIR);
                return GetExactMarshalerMethod(pMD);
            }

            default:
                UNREACHABLE_MSG("Unrecognized redirected type.");
        }
    }
    return ILMngdMarshaler::GetConvertContentsToManagedMethod();
}

MethodDesc *ILHiddenLengthArrayMarshaler::GetConvertContentsToNativeMethod()
{
    STANDARD_VM_CONTRACT;

    if (m_pargs->na.m_vt == VTHACK_REDIRECTEDTYPE)
    {
        switch (m_pargs->na.m_redirectedTypeIndex)
        {
            case WinMDAdapter::RedirectedTypeIndex_System_DateTimeOffset:
                return MscorlibBinder::GetMethod(METHOD__MNGD_HIDDEN_LENGTH_ARRAY_MARSHALER__CONVERT_CONTENTS_TO_NATIVE_DATETIME);

            case WinMDAdapter::RedirectedTypeIndex_System_Type:
                return MscorlibBinder::GetMethod(METHOD__MNGD_HIDDEN_LENGTH_ARRAY_MARSHALER__CONVERT_CONTENTS_TO_NATIVE_TYPE);

            case WinMDAdapter::RedirectedTypeIndex_System_Exception:
                return MscorlibBinder::GetMethod(METHOD__MNGD_HIDDEN_LENGTH_ARRAY_MARSHALER__CONVERT_CONTENTS_TO_NATIVE_EXCEPTION);

            case WinMDAdapter::RedirectedTypeIndex_System_Nullable:
            {
                MethodDesc *pMD = MscorlibBinder::GetMethod(METHOD__MNGD_HIDDEN_LENGTH_ARRAY_MARSHALER__CONVERT_CONTENTS_TO_NATIVE_NULLABLE);
                return GetExactMarshalerMethod(pMD);
            }

            case WinMDAdapter::RedirectedTypeIndex_System_Collections_Generic_KeyValuePair:
            {
                MethodDesc *pMD = MscorlibBinder::GetMethod(METHOD__MNGD_HIDDEN_LENGTH_ARRAY_MARSHALER__CONVERT_CONTENTS_TO_NATIVE_KEYVALUEPAIR);
                return GetExactMarshalerMethod(pMD);
            }

            default:
                UNREACHABLE_MSG("Unrecognized redirected type.");
        }
    }
    return ILMngdMarshaler::GetConvertContentsToNativeMethod();
}

MethodDesc *ILHiddenLengthArrayMarshaler::GetClearNativeContentsMethod()
{
    switch (m_pargs->na.m_vt)
    {
        // HSTRINGs, interface pointers, and non-blittable structs need contents cleanup
        case VTHACK_HSTRING:
        case VTHACK_INSPECTABLE:
        case VTHACK_NONBLITTABLERECORD:
            break;

        // blittable structs don't need contents cleanup
        case VTHACK_BLITTABLERECORD:
            return NULL;

        case VTHACK_REDIRECTEDTYPE:
        {
            switch (m_pargs->na.m_redirectedTypeIndex)
            {
                // System.Type, Uri, Nullable, KeyValuePair, NCCEventArgs, and PCEventArgs need cleanup
                case WinMDAdapter::RedirectedTypeIndex_System_Type:
                    return MscorlibBinder::GetMethod(METHOD__MNGD_HIDDEN_LENGTH_ARRAY_MARSHALER__CLEAR_NATIVE_CONTENTS_TYPE);

                case WinMDAdapter::RedirectedTypeIndex_System_Uri:
                case WinMDAdapter::RedirectedTypeIndex_System_Nullable:
                case WinMDAdapter::RedirectedTypeIndex_System_Collections_Generic_KeyValuePair:
                case WinMDAdapter::RedirectedTypeIndex_System_Collections_Specialized_NotifyCollectionChangedEventArgs:
                case WinMDAdapter::RedirectedTypeIndex_System_ComponentModel_PropertyChangedEventArgs:
                    break;

                // other redirected types don't
                default:
                    return NULL;
            }
            break;
        }

        default:
            UNREACHABLE_MSG("Unexpected hidden-length array element VT");
    }

    return ILMngdMarshaler::GetClearNativeContentsMethod();
}

MethodDesc *ILHiddenLengthArrayMarshaler::GetExactMarshalerMethod(MethodDesc *pGenericMD)
{
    STANDARD_VM_CONTRACT;

    return MethodDesc::FindOrCreateAssociatedMethodDesc(
        pGenericMD,
        pGenericMD->GetMethodTable(),
        FALSE,                                 // forceBoxedEntryPoint
        m_pargs->m_pMarshalInfo->GetArrayElementTypeHandle().GetInstantiation(), // methodInst
        FALSE,                                 // allowInstParam
        TRUE);                                 // forceRemotableMethod
}

#ifndef CROSSGEN_COMPILE

FCIMPL4(void, MngdHiddenLengthArrayMarshaler::CreateMarshaler, MngdHiddenLengthArrayMarshaler* pThis, MethodTable* pMT, SIZE_T cbElementSize, UINT16 vt)
{
    FCALL_CONTRACT;

    pThis->m_pElementMT = pMT;
    pThis->m_cbElementSize = cbElementSize;
    pThis->m_vt = (VARTYPE)vt;
}
FCIMPLEND

FCIMPL3(void, MngdHiddenLengthArrayMarshaler::ConvertSpaceToNative, MngdHiddenLengthArrayMarshaler* pThis, OBJECTREF* pManagedHome, void** pNativeHome)
{
    FCALL_CONTRACT;

    BASEARRAYREF arrayRef = (BASEARRAYREF) *pManagedHome;
    HELPER_METHOD_FRAME_BEGIN_1(arrayRef);

    if (arrayRef == NULL)
    {
        *pNativeHome = NULL;
    }
    else
    {
        SIZE_T cbArray = pThis->GetArraySize(arrayRef->GetNumComponents());

        *pNativeHome = CoTaskMemAlloc(cbArray);
        if (*pNativeHome == NULL)
        {
            ThrowOutOfMemory();
        }

        // initialize the array
        FillMemory(*pNativeHome, cbArray, 0);
    }

    HELPER_METHOD_FRAME_END();
}
FCIMPLEND
    
FCIMPL3(void, MngdHiddenLengthArrayMarshaler::ConvertContentsToNative, MngdHiddenLengthArrayMarshaler* pThis, OBJECTREF* pManagedHome, void** pNativeHome)
{
    FCALL_CONTRACT;
    
    struct
    {
        PTRARRAYREF arrayRef;
        STRINGREF currentStringRef;
        OBJECTREF currentObjectRef;
    }
    gc;
    ZeroMemory(&gc, sizeof(gc));
    gc.arrayRef = (PTRARRAYREF)*pManagedHome;

    HELPER_METHOD_FRAME_BEGIN_PROTECT(gc);

    if (gc.arrayRef != NULL)
    {
        // There are these choices:
        //  * the array is made up of entirely blittable data, in which case we can directly copy it, 
        //  * it is an array of strings that need to be marshaled as HSTRING, 
        //  * it is an array of non-blittable structures
        //  * it is an array of interface pointers (interface, runtime class, delegate, System.Object)
        switch (pThis->m_vt)
        {
            case VTHACK_BLITTABLERECORD:
            {
                // Just do a raw memcpy into the array
                SIZE_T cbArray = pThis->GetArraySize(gc.arrayRef->GetNumComponents());
                memcpyNoGCRefs(*pNativeHome, gc.arrayRef->GetDataPtr(), cbArray);
                break;
            }

            case VTHACK_HSTRING:
            {
                // Marshal a string array as an array of HSTRINGs
                if (!WinRTSupported())
                {
                    COMPlusThrow(kPlatformNotSupportedException, W("PlatformNotSupported_WinRT"));
                }

                HSTRING *pDestinationStrings = reinterpret_cast<HSTRING *>(*pNativeHome);

                for (SIZE_T i = 0; i < gc.arrayRef->GetNumComponents(); ++i)
                {
                    gc.currentStringRef = (STRINGREF)gc.arrayRef->GetAt(i);
                    if (gc.currentStringRef == NULL)
                    {
                        StackSString ssIndex;
                        ssIndex.Printf(W("%d"), i);
                        COMPlusThrow(kMarshalDirectiveException, IDS_EE_BADMARSHALARRAY_NULL_HSTRING, ssIndex.GetUnicode());
                    }

                    IfFailThrow(WindowsCreateString(gc.currentStringRef->GetBuffer(), gc.currentStringRef->GetStringLength(), &(pDestinationStrings[i])));
                }
                break;
            }

            case VTHACK_NONBLITTABLERECORD:
            {
                BYTE *pNativeStart = reinterpret_cast<BYTE *>(*pNativeHome);
                SIZE_T managedOffset = ArrayBase::GetDataPtrOffset(gc.arrayRef->GetMethodTable());
                SIZE_T nativeOffset = 0;
                SIZE_T managedSize = gc.arrayRef->GetComponentSize();
                SIZE_T nativeSize = pThis->m_pElementMT->GetNativeSize();
                for (SIZE_T i = 0; i < gc.arrayRef->GetNumComponents(); ++i)
                {
                    LayoutUpdateNative(reinterpret_cast<LPVOID *>(&gc.arrayRef), managedOffset, pThis->m_pElementMT, pNativeStart + nativeOffset, NULL);
                    managedOffset += managedSize;
                    nativeOffset += nativeSize;
                }
                break;
            }

            case VTHACK_INSPECTABLE:
            {
                // interface pointers
                IUnknown **pDestinationIPs = reinterpret_cast<IUnknown **>(*pNativeHome);
                
                // If this turns out to be a perf issue, we can precompute the ItfMarshalInfo
                // and generate code that passes it to the marshaler at creation time.
                ItfMarshalInfo itfInfo;
                MarshalInfo::GetItfMarshalInfo(TypeHandle(pThis->m_pElementMT), TypeHandle(), FALSE, TRUE, MarshalInfo::MARSHAL_SCENARIO_WINRT, &itfInfo);

                for (SIZE_T i = 0; i < gc.arrayRef->GetNumComponents(); ++i)
                {
                    gc.currentObjectRef = gc.arrayRef->GetAt(i);
                    pDestinationIPs[i] = MarshalObjectToInterface(
                        &gc.currentObjectRef, 
                        itfInfo.thNativeItf.GetMethodTable(),  
                        itfInfo.thClass.GetMethodTable(),
                        itfInfo.dwFlags);
                }
                break;
            }

            default:
                UNREACHABLE_MSG("Unrecognized array element VARTYPE");

        }
    }
    HELPER_METHOD_FRAME_END();
}
FCIMPLEND
    
FCIMPL4(void, MngdHiddenLengthArrayMarshaler::ConvertSpaceToManaged, MngdHiddenLengthArrayMarshaler* pThis, OBJECTREF* pManagedHome, void** pNativeHome, INT32 cElements)
{
    FCALL_CONTRACT;

    HELPER_METHOD_FRAME_BEGIN_0();

    if (*pNativeHome == NULL)
    {
        SetObjectReference(pManagedHome, NULL);
    }
    else
    {
        TypeHandle elementType(pThis->m_pElementMT);
        TypeHandle arrayType = ClassLoader::LoadArrayTypeThrowing(elementType);
        SetObjectReference(pManagedHome, AllocateSzArray(arrayType, cElements));
    }

    HELPER_METHOD_FRAME_END();
}
FCIMPLEND
    
FCIMPL3(void, MngdHiddenLengthArrayMarshaler::ConvertContentsToManaged, MngdHiddenLengthArrayMarshaler* pThis, OBJECTREF* pManagedHome, void** pNativeHome)
{
    FCALL_CONTRACT;

    struct
    {
        PTRARRAYREF arrayRef;
        STRINGREF   stringRef;
        OBJECTREF   objectRef;
    }
    gc;
    ZeroMemory(&gc, sizeof(gc));
    gc.arrayRef = (PTRARRAYREF)*pManagedHome;

    HELPER_METHOD_FRAME_BEGIN_PROTECT(gc);

    if (*pNativeHome != NULL)
    {
        // There are these choices:
        //  * the array is made up of entirely blittable data, in which case we can directly copy it, 
        //  * it is an array of strings that need to be marshaled as HSTRING, 
        //  * it is an array of non-blittable structures
        //  * it is an array of interface pointers (interface, runtime class, delegate, System.Object)
        switch (pThis->m_vt)
        {
            case VTHACK_BLITTABLERECORD:
            {
                // Just do a raw memcpy into the array
                SIZE_T cbArray = pThis->GetArraySize(gc.arrayRef->GetNumComponents());
                memcpyNoGCRefs(gc.arrayRef->GetDataPtr(), *pNativeHome, cbArray);
                break;
            }

            case VTHACK_HSTRING:
            {
                // Strings are in HSRING format on the native side
                if (!WinRTSupported())
                {
                    COMPlusThrow(kPlatformNotSupportedException, W("PlatformNotSupported_WinRT"));
                }

                HSTRING *pSourceStrings = reinterpret_cast<HSTRING *>(*pNativeHome);

                for (SIZE_T i = 0; i < gc.arrayRef->GetNumComponents(); ++i)
                {
                    // NULL HSTRINGS are equivilent to empty strings
                    UINT32 cchString = 0;
                    LPCWSTR pwszString = W("");

                    if (pSourceStrings[i] != NULL)
                    {
                        pwszString = WindowsGetStringRawBuffer(pSourceStrings[i], &cchString);
                    }

                    gc.stringRef = StringObject::NewString(pwszString, cchString);
                    gc.arrayRef->SetAt(i, gc.stringRef);
                }
                break;
            }

            case VTHACK_NONBLITTABLERECORD:
            {
                // Defer to the field marshaler to handle structures
                BYTE *pNativeStart = reinterpret_cast<BYTE *>(*pNativeHome);
                SIZE_T managedOffset = ArrayBase::GetDataPtrOffset(gc.arrayRef->GetMethodTable());
                SIZE_T nativeOffset = 0;
                SIZE_T managedSize = gc.arrayRef->GetComponentSize();
                SIZE_T nativeSize = pThis->m_pElementMT->GetNativeSize();
                for (SIZE_T i = 0; i < gc.arrayRef->GetNumComponents(); ++i)
                {
                    LayoutUpdateCLR(reinterpret_cast<LPVOID *>(&gc.arrayRef), managedOffset, pThis->m_pElementMT, pNativeStart + nativeOffset);
                    managedOffset += managedSize;
                    nativeOffset += nativeSize;
                }
                break;
            }

            case VTHACK_INSPECTABLE:
            {
                // interface pointers
                IUnknown **pSourceIPs = reinterpret_cast<IUnknown **>(*pNativeHome);

                // If this turns out to be a perf issue, we can precompute the ItfMarshalInfo
                // and generate code that passes it to the marshaler at creation time.
                ItfMarshalInfo itfInfo;
                MarshalInfo::GetItfMarshalInfo(TypeHandle(pThis->m_pElementMT), TypeHandle(), FALSE, TRUE, MarshalInfo::MARSHAL_SCENARIO_WINRT, &itfInfo);

                for (SIZE_T i = 0; i < gc.arrayRef->GetNumComponents(); ++i)
                {
                    gc.objectRef = gc.arrayRef->GetAt(i);
                    UnmarshalObjectFromInterface(
                        &gc.objectRef,
                        &pSourceIPs[i],
                        itfInfo.thItf.GetMethodTable(),  
                        itfInfo.thClass.GetMethodTable(),
                        itfInfo.dwFlags);
                    gc.arrayRef->SetAt(i, gc.objectRef);
                }
                break;
            }

            default:
                UNREACHABLE_MSG("Unrecognized array element VARTYPE");
        }
    }
    
    HELPER_METHOD_FRAME_END();
}
FCIMPLEND

FCIMPL3(void, MngdHiddenLengthArrayMarshaler::ClearNativeContents, MngdHiddenLengthArrayMarshaler* pThis, void** pNativeHome, INT32 cElements)
{
    FCALL_CONTRACT;
    
    HELPER_METHOD_FRAME_BEGIN_0();

    if (*pNativeHome != NULL)
    {
        pThis->DoClearNativeContents(pNativeHome, cElements);
    }

    HELPER_METHOD_FRAME_END();
}
FCIMPLEND

#endif // CROSSGEN_COMPILE


SIZE_T MngdHiddenLengthArrayMarshaler::GetArraySize(SIZE_T elements)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;

    _ASSERTE_MSG(m_cbElementSize != 0, "You have to set the native size for your array element type");

    SIZE_T cbArray;

    if (!ClrSafeInt<SIZE_T>::multiply(elements, m_cbElementSize, cbArray))
    {
        COMPlusThrow(kArgumentException, IDS_EE_STRUCTARRAYTOOLARGE);
    }

    // This array size limit is carried over from the equivilent limit for other array marshaling code
    if (cbArray > MAX_SIZE_FOR_INTEROP)
    {
        COMPlusThrow(kArgumentException, IDS_EE_STRUCTARRAYTOOLARGE);
    }

    return cbArray;
}

#ifndef CROSSGEN_COMPILE
void MngdHiddenLengthArrayMarshaler::DoClearNativeContents(void** pNativeHome, INT32 cElements)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(pNativeHome != NULL);
    }
    CONTRACTL_END;

    VARTYPE vt = m_vt;
    if (vt == VTHACK_REDIRECTEDTYPE)
    {
        // the redirected types that use this helper are interface pointers on the WinRT side
        vt = VTHACK_INSPECTABLE;
    }

    switch (vt)
    {
        case VTHACK_HSTRING:
        {
            if (WinRTSupported())
            {
                HSTRING *pStrings = reinterpret_cast<HSTRING *>(*pNativeHome);
                for (INT32 i = 0; i < cElements; ++i)
                {
                    if (pStrings[i] != NULL)
                    {
                        WindowsDeleteString(pStrings[i]);
                    }
                }
            }
            break;
        }

        case VTHACK_NONBLITTABLERECORD:
        {
            SIZE_T cbArray = GetArraySize(cElements);
            BYTE *pNativeCurrent = reinterpret_cast<BYTE *>(*pNativeHome);
            BYTE *pNativeEnd = pNativeCurrent + cbArray;

            while (pNativeCurrent < pNativeEnd)
            {
                LayoutDestroyNative(pNativeCurrent, m_pElementMT);
                pNativeCurrent += m_pElementMT->GetNativeSize();
            }
            break;
        }
    
        case VTHACK_INSPECTABLE:
        {
            IInspectable **pIPs = reinterpret_cast<IInspectable **>(*pNativeHome);
            for (INT32 i = 0; i < cElements; ++i)
            {
                if (pIPs[i] != NULL)
                {
                    SafeRelease(pIPs[i]);
                }
            }
            break;
        }
            
        default:
            UNREACHABLE_MSG("Unexpected hidden-length array element VT");
    }    
}
#endif //CROSSGEN_COMPILE
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
    // allocate space for marshaler
    //

    m_dwMngdMarshalerLocalNum = pslILEmit->NewLocal(ELEMENT_TYPE_I);

    pslILEmit->EmitLDC(sizeof(MngdRefCustomMarshaler));
    pslILEmit->EmitLOCALLOC();
    pslILEmit->EmitSTLOC(m_dwMngdMarshalerLocalNum);

    pslILEmit->EmitLDLOC(m_dwMngdMarshalerLocalNum);    // arg to CreateMarshaler
    
    //
    // call CreateCustomMarshalerHelper
    //

    pslILEmit->EmitLDTOKEN(pslILEmit->GetToken(m_pargs->rcm.m_pMD));
    pslILEmit->EmitCALL(METHOD__METHOD_HANDLE__GETVALUEINTERNAL, 1, 1);

    pslILEmit->EmitLDC(m_pargs->rcm.m_paramToken);

    pslILEmit->EmitLDTOKEN(pslILEmit->GetToken(TypeHandle::FromPtr(m_pargs->rcm.m_hndManagedType)));
    pslILEmit->EmitCALL(METHOD__RT_TYPE_HANDLE__GETVALUEINTERNAL, 1, 1);

    pslILEmit->EmitCALL(METHOD__STUBHELPERS__CREATE_CUSTOM_MARSHALER_HELPER, 3, 1);  // arg to CreateMarshaler

    //
    // call MngdRefCustomMarshaler::CreateMarshaler
    //

    pslILEmit->EmitCALL(METHOD__MNGD_REF_CUSTOM_MARSHALER__CREATE_MARSHALER, 2, 0);
}


#ifndef CROSSGEN_COMPILE

FCIMPL2(void, MngdRefCustomMarshaler::CreateMarshaler, MngdRefCustomMarshaler* pThis, void* pCMHelper)
{
    FCALL_CONTRACT;

    pThis->m_pCMHelper = (CustomMarshalerHelper*)pCMHelper;
}
FCIMPLEND

    
FCIMPL3(void, MngdRefCustomMarshaler::ConvertContentsToNative, MngdRefCustomMarshaler* pThis, OBJECTREF* pManagedHome, void** pNativeHome)
{
    CONTRACTL
    {
        FCALL_CHECK;
        PRECONDITION(CheckPointer(pManagedHome));
    }
    CONTRACTL_END;
    
    HELPER_METHOD_FRAME_BEGIN_0();

    *pNativeHome = pThis->m_pCMHelper->InvokeMarshalManagedToNativeMeth(*pManagedHome);

    HELPER_METHOD_FRAME_END();
}
FCIMPLEND
    
    
FCIMPL3(void, MngdRefCustomMarshaler::ConvertContentsToManaged, MngdRefCustomMarshaler* pThis, OBJECTREF* pManagedHome, void** pNativeHome)
{
    CONTRACTL
    {
        FCALL_CHECK;
        PRECONDITION(CheckPointer(pManagedHome));
    }
    CONTRACTL_END;
    
    HELPER_METHOD_FRAME_BEGIN_0();

    SetObjectReference(pManagedHome, pThis->m_pCMHelper->InvokeMarshalNativeToManagedMeth(*pNativeHome));
    
    HELPER_METHOD_FRAME_END();
}
FCIMPLEND
    
FCIMPL3(void, MngdRefCustomMarshaler::ClearNative, MngdRefCustomMarshaler* pThis, OBJECTREF* pManagedHome, void** pNativeHome)
{
    FCALL_CONTRACT;

    HELPER_METHOD_FRAME_BEGIN_0();

    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    pThis->m_pCMHelper->InvokeCleanUpNativeMeth(*pNativeHome);
    
    HELPER_METHOD_FRAME_END();
}
FCIMPLEND
    
FCIMPL3(void, MngdRefCustomMarshaler::ClearManaged, MngdRefCustomMarshaler* pThis, OBJECTREF* pManagedHome, void** pNativeHome)
{
    CONTRACTL
    {
        FCALL_CHECK;
        PRECONDITION(CheckPointer(pManagedHome));
    }
    CONTRACTL_END;

    HELPER_METHOD_FRAME_BEGIN_0();
    
    pThis->m_pCMHelper->InvokeCleanUpManagedMeth(*pManagedHome);
    
    HELPER_METHOD_FRAME_END();
}
FCIMPLEND

#endif // CROSSGEN_COMPILE


#ifdef FEATURE_COMINTEROP

///////////////////////////////////////////////////////////////////////////////////////////////////
// ILUriMarshaler implementation
///////////////////////////////////////////////////////////////////////////////////////////////////

LocalDesc ILUriMarshaler::GetNativeType()
{
    LIMITED_METHOD_CONTRACT;
    return LocalDesc(ELEMENT_TYPE_I);
}

LocalDesc ILUriMarshaler::GetManagedType()
{
    STANDARD_VM_CONTRACT;;    
    LoaderAllocator* pLoader = m_pargs->m_pMarshalInfo->GetModule()->GetLoaderAllocator();
    TypeHandle  hndUriType = pLoader->GetMarshalingData()->GetUriMarshalingInfo()->GetSystemUriType();

    return LocalDesc(hndUriType); // System.Uri
}

bool ILUriMarshaler::NeedsClearNative()
{
    LIMITED_METHOD_CONTRACT;
    return true;
}

// Note that this method expects the CLR Uri on top of the evaluation stack and leaves the WinRT Uri there.
//static
void ILUriMarshaler::EmitConvertCLRUriToWinRTUri(ILCodeStream* pslILEmit, LoaderAllocator* pLoader)
{
    STANDARD_VM_CONTRACT;

    UriMarshalingInfo* marshalingInfo = pLoader->GetMarshalingData()->GetUriMarshalingInfo();

    ILCodeLabel *pNotNullLabel = pslILEmit->NewCodeLabel();
    ILCodeLabel *pDoneLabel = pslILEmit->NewCodeLabel();

    pslILEmit->EmitDUP();
    pslILEmit->EmitBRTRUE(pNotNullLabel);

    pslILEmit->EmitPOP();
    pslILEmit->EmitLoadNullPtr();
    pslILEmit->EmitBR(pDoneLabel);

    pslILEmit->EmitLabel(pNotNullLabel);

    // System.Uri.get_OriginalString()
    MethodDesc* pSystemUriOriginalStringMD = marshalingInfo->GetSystemUriOriginalStringMD();
    pslILEmit->EmitCALL(pslILEmit->GetToken(pSystemUriOriginalStringMD), 1, 1);

    pslILEmit->EmitCALL(METHOD__URIMARSHALER__CREATE_NATIVE_URI_INSTANCE, 1, 1);

    pslILEmit->EmitLabel(pDoneLabel);
}

void ILUriMarshaler::EmitConvertContentsCLRToNative(ILCodeStream* pslILEmit)
{
    STANDARD_VM_CONTRACT;

    EmitLoadManagedValue(pslILEmit);
    EmitConvertCLRUriToWinRTUri(pslILEmit, m_pargs->m_pMarshalInfo->GetModule()->GetLoaderAllocator());
    EmitStoreNativeValue(pslILEmit);
}

// Note that this method expects the WinRT Uri on top of the evaluation stack and leaves the CLR Uri there.
//static
void ILUriMarshaler::EmitConvertWinRTUriToCLRUri(ILCodeStream* pslILEmit, LoaderAllocator* pLoader)
{
    STANDARD_VM_CONTRACT;

    MethodDesc* pSystemUriCtorMD = pLoader->GetMarshalingData()->GetUriMarshalingInfo()->GetSystemUriCtorMD();

    ILCodeLabel *pNotNullLabel = pslILEmit->NewCodeLabel();
    ILCodeLabel *pDoneLabel = pslILEmit->NewCodeLabel();

    pslILEmit->EmitDUP();
    pslILEmit->EmitBRTRUE(pNotNullLabel);

    pslILEmit->EmitPOP();
    pslILEmit->EmitLDNULL();
    pslILEmit->EmitBR(pDoneLabel);

    pslILEmit->EmitLabel(pNotNullLabel);

    // string UriMarshaler.GetRawUriFromNative(IntPtr)
    pslILEmit->EmitCALL(METHOD__URIMARSHALER__GET_RAWURI_FROM_NATIVE, 1, 1);

    // System.Uri..ctor(string)
    pslILEmit->EmitNEWOBJ(pslILEmit->GetToken(pSystemUriCtorMD), 1);

    pslILEmit->EmitLabel(pDoneLabel);
}

void ILUriMarshaler::EmitConvertContentsNativeToCLR(ILCodeStream* pslILEmit)
{
    STANDARD_VM_CONTRACT;

    EmitLoadNativeValue(pslILEmit);
    EmitConvertWinRTUriToCLRUri(pslILEmit, m_pargs->m_pMarshalInfo->GetModule()->GetLoaderAllocator());
    EmitStoreManagedValue(pslILEmit);
}

void ILUriMarshaler::EmitClearNative(ILCodeStream* pslILEmit)
{
    STANDARD_VM_CONTRACT;
    EmitInterfaceClearNative(pslILEmit);
}

///////////////////////////////////////////////////////////////////////////////////////////////////
// ILNCCEventArgsMarshaler implementation
///////////////////////////////////////////////////////////////////////////////////////////////////

LocalDesc ILNCCEventArgsMarshaler::GetNativeType()
{
    LIMITED_METHOD_CONTRACT;
    return LocalDesc(ELEMENT_TYPE_I);
}

LocalDesc ILNCCEventArgsMarshaler::GetManagedType()
{
    STANDARD_VM_CONTRACT;;    
    
    LoaderAllocator *pLoader = m_pargs->m_pMarshalInfo->GetModule()->GetLoaderAllocator();
    TypeHandle  hndNCCEventArgType = pLoader->GetMarshalingData()->GetEventArgsMarshalingInfo()->GetSystemNCCEventArgsType();

    return LocalDesc(hndNCCEventArgType); // System.Collections.Specialized.NotifyCollectionChangedEventArgs
}

bool ILNCCEventArgsMarshaler::NeedsClearNative()
{
    LIMITED_METHOD_CONTRACT;
    return true;
}

// Note that this method expects the CLR NotifyCollectionChangedEventArgs on top of the evaluation stack and
// leaves the WinRT NotifyCollectionChangedEventArgs IP there.
//static
void ILNCCEventArgsMarshaler::EmitConvertCLREventArgsToWinRTEventArgs(ILCodeStream *pslILEmit, LoaderAllocator* pLoader)
{
    STANDARD_VM_CONTRACT;

    MethodDesc *pConvertMD = pLoader->GetMarshalingData()->GetEventArgsMarshalingInfo()->GetSystemNCCEventArgsToWinRTNCCEventArgsMD();

    // IntPtr System.Runtime.InteropServices.WindowsRuntime.NotifyCollectionChangedEventArgsMarshaler.ConvertToNative(NotifyCollectionChangedEventArgs)
    pslILEmit->EmitCALL(pslILEmit->GetToken(pConvertMD), 1, 1);
}

void ILNCCEventArgsMarshaler::EmitConvertContentsCLRToNative(ILCodeStream *pslILEmit)
{
    STANDARD_VM_CONTRACT;

    EmitLoadManagedValue(pslILEmit);
    EmitConvertCLREventArgsToWinRTEventArgs(pslILEmit, m_pargs->m_pMarshalInfo->GetModule()->GetLoaderAllocator());
    EmitStoreNativeValue(pslILEmit);
}

// Note that this method expects the WinRT NotifyCollectionChangedEventArgs on top of the evaluation stack and
// leaves the CLR NotifyCollectionChangedEventArgs there.
//static
void ILNCCEventArgsMarshaler::EmitConvertWinRTEventArgsToCLREventArgs(ILCodeStream* pslILEmit, LoaderAllocator* pLoader)
{
    STANDARD_VM_CONTRACT;

    MethodDesc *pConvertMD = pLoader->GetMarshalingData()->GetEventArgsMarshalingInfo()->GetWinRTNCCEventArgsToSystemNCCEventArgsMD();

    // NotifyCollectionChangedEventArgs System.Runtime.InteropServices.WindowsRuntime.NotifyCollectionChangedEventArgsMarshaler.ConvertToManaged(IntPtr)
    pslILEmit->EmitCALL(pslILEmit->GetToken(pConvertMD), 1, 1);
}

void ILNCCEventArgsMarshaler::EmitConvertContentsNativeToCLR(ILCodeStream* pslILEmit)
{
    STANDARD_VM_CONTRACT;

    EmitLoadNativeValue(pslILEmit);
    EmitConvertWinRTEventArgsToCLREventArgs(pslILEmit, m_pargs->m_pMarshalInfo->GetModule()->GetLoaderAllocator());
    EmitStoreManagedValue(pslILEmit);
}

void ILNCCEventArgsMarshaler::EmitClearNative(ILCodeStream* pslILEmit)
{
    STANDARD_VM_CONTRACT;
    EmitInterfaceClearNative(pslILEmit);
}

///////////////////////////////////////////////////////////////////////////////////////////////////
// ILPCEventArgsMarshaler implementation
///////////////////////////////////////////////////////////////////////////////////////////////////

LocalDesc ILPCEventArgsMarshaler::GetNativeType()
{
    LIMITED_METHOD_CONTRACT;
    return LocalDesc(ELEMENT_TYPE_I);
}

LocalDesc ILPCEventArgsMarshaler::GetManagedType()
{
    STANDARD_VM_CONTRACT;;    
    
    LoaderAllocator* pLoader = m_pargs->m_pMarshalInfo->GetModule()->GetLoaderAllocator();
    TypeHandle  hndPCEventArgType = pLoader->GetMarshalingData()->GetEventArgsMarshalingInfo()->GetSystemPCEventArgsType();

    return LocalDesc(hndPCEventArgType); // System.ComponentModel.PropertyChangedEventArgs
}

bool ILPCEventArgsMarshaler::NeedsClearNative()
{
    LIMITED_METHOD_CONTRACT;
    return true;
}

// Note that this method expects the CLR PropertyChangedEventArgs on top of the evaluation stack and
// leaves the WinRT PropertyChangedEventArgs IP there.
//static
void ILPCEventArgsMarshaler::EmitConvertCLREventArgsToWinRTEventArgs(ILCodeStream *pslILEmit, LoaderAllocator* pLoader)
{
    STANDARD_VM_CONTRACT;

    MethodDesc *pConvertMD = pLoader->GetMarshalingData()->GetEventArgsMarshalingInfo()->GetSystemPCEventArgsToWinRTPCEventArgsMD();

    // IntPtr System.Runtime.InteropServices.WindowsRuntime.PropertyChangedEventArgsMarshaler.ConvertToNative(PropertyChangedEventArgs)
    pslILEmit->EmitCALL(pslILEmit->GetToken(pConvertMD), 1, 1);
}

void ILPCEventArgsMarshaler::EmitConvertContentsCLRToNative(ILCodeStream *pslILEmit)
{
    STANDARD_VM_CONTRACT;

    EmitLoadManagedValue(pslILEmit);
    EmitConvertCLREventArgsToWinRTEventArgs(pslILEmit, m_pargs->m_pMarshalInfo->GetModule()->GetLoaderAllocator());
    EmitStoreNativeValue(pslILEmit);
}

// Note that this method expects the WinRT PropertyChangedEventArgs on top of the evaluation stack and
// leaves the CLR PropertyChangedEventArgs there.
//static
void ILPCEventArgsMarshaler::EmitConvertWinRTEventArgsToCLREventArgs(ILCodeStream* pslILEmit, LoaderAllocator* pLoader)
{
    STANDARD_VM_CONTRACT;

    MethodDesc *pConvertMD = pLoader->GetMarshalingData()->GetEventArgsMarshalingInfo()->GetWinRTPCEventArgsToSystemPCEventArgsMD();

    // PropertyChangedEventArgs System.Runtime.InteropServices.WindowsRuntime.PropertyChangedEventArgsMarshaler.ConvertToManaged(IntPtr)
    pslILEmit->EmitCALL(pslILEmit->GetToken(pConvertMD), 1, 1);
}

void ILPCEventArgsMarshaler::EmitConvertContentsNativeToCLR(ILCodeStream* pslILEmit)
{
    STANDARD_VM_CONTRACT;

    EmitLoadNativeValue(pslILEmit);
    EmitConvertWinRTEventArgsToCLREventArgs(pslILEmit, m_pargs->m_pMarshalInfo->GetModule()->GetLoaderAllocator());
    EmitStoreManagedValue(pslILEmit);
}

void ILPCEventArgsMarshaler::EmitClearNative(ILCodeStream* pslILEmit)
{
    STANDARD_VM_CONTRACT;
    EmitInterfaceClearNative(pslILEmit);
}

///////////////////////////////////////////////////////////////////////////////////////////////////
// ILDateTimeMarshaler implementation
///////////////////////////////////////////////////////////////////////////////////////////////////

LocalDesc ILDateTimeMarshaler::GetNativeType()
{
    STANDARD_VM_CONTRACT;;    
    return LocalDesc(MscorlibBinder::GetClass(CLASS__DATETIMENATIVE));
}

LocalDesc ILDateTimeMarshaler::GetManagedType()
{
    STANDARD_VM_CONTRACT;;    
    return LocalDesc(MscorlibBinder::GetClass(CLASS__DATE_TIME_OFFSET));
}

bool ILDateTimeMarshaler::NeedsClearNative()
{
    LIMITED_METHOD_CONTRACT;
    return false;
}

void ILDateTimeMarshaler::EmitConvertContentsCLRToNative(ILCodeStream* pslILEmit)
{
    CONTRACTL
    {
        STANDARD_VM_CHECK;
        PRECONDITION(CheckPointer(pslILEmit));
    }
    CONTRACTL_END;

    // DateTimeOffsetMarshaler.ConvertManagedToNative(ref managedDTO, out nativeTicks);
    EmitLoadManagedHomeAddr(pslILEmit);
    EmitLoadNativeHomeAddr(pslILEmit);    
    pslILEmit->EmitCALL(METHOD__DATETIMEOFFSETMARSHALER__CONVERT_TO_NATIVE, 2, 0);
}

void ILDateTimeMarshaler::EmitConvertContentsNativeToCLR(ILCodeStream* pslILEmit)
{
    STANDARD_VM_CONTRACT;

    // DateTimeOffsetMarshaler.ConvertNativeToManaged(out managedLocalDTO, ref nativeTicks);
    EmitLoadManagedHomeAddr(pslILEmit);
    EmitLoadNativeHomeAddr(pslILEmit);
    pslILEmit->EmitCALL(METHOD__DATETIMEOFFSETMARSHALER__CONVERT_TO_MANAGED, 2, 0);
}

void ILDateTimeMarshaler::EmitReInitNative(ILCodeStream* pslILEmit)
{
    STANDARD_VM_CONTRACT;

    EmitLoadNativeHomeAddr(pslILEmit);
    pslILEmit->EmitINITOBJ(pslILEmit->GetToken(MscorlibBinder::GetClass(CLASS__DATETIMENATIVE)));
}

///////////////////////////////////////////////////////////////////////////////////////////////////
// ILNullableMarshaler implementation
///////////////////////////////////////////////////////////////////////////////////////////////////

LocalDesc ILNullableMarshaler::GetNativeType()
{
    LIMITED_METHOD_CONTRACT;
    return LocalDesc(ELEMENT_TYPE_I);
}

LocalDesc ILNullableMarshaler::GetManagedType()
{
    LIMITED_METHOD_CONTRACT;;    
    return LocalDesc(m_pargs->m_pMT);
}

bool ILNullableMarshaler::NeedsClearNative()
{
    LIMITED_METHOD_CONTRACT;
    return true;
}

void ILNullableMarshaler::EmitConvertContentsCLRToNative(ILCodeStream* pslILEmit)
{
    CONTRACTL
    {
        STANDARD_VM_CHECK;
        PRECONDITION(CheckPointer(pslILEmit));
    }
    CONTRACTL_END;
    
    // pNative = NullableMarshaler<T>.ConvertToNative(ref pManaged);
    EmitLoadManagedHomeAddr(pslILEmit);

    MethodDesc *pMD = GetExactMarshalerMethod(MscorlibBinder::GetMethod(METHOD__NULLABLEMARSHALER__CONVERT_TO_NATIVE));
    pslILEmit->EmitCALL(pslILEmit->GetToken(pMD), 1, 1);

    EmitStoreNativeValue(pslILEmit);
}

void ILNullableMarshaler::EmitConvertContentsNativeToCLR(ILCodeStream* pslILEmit)
{
    STANDARD_VM_CONTRACT;

    // pManaged = NullableMarshaler.ConvertToManaged(pNative);
    EmitLoadNativeValue(pslILEmit);

    MethodDesc *pMD = GetExactMarshalerMethod(MscorlibBinder::GetMethod(METHOD__NULLABLEMARSHALER__CONVERT_TO_MANAGED));
    pslILEmit->EmitCALL(pslILEmit->GetToken(pMD), 1, 1);

    EmitStoreManagedValue(pslILEmit);
}

void ILNullableMarshaler::EmitClearNative(ILCodeStream* pslILEmit)
{
    STANDARD_VM_CONTRACT;
    EmitInterfaceClearNative(pslILEmit);
}

MethodDesc *ILNullableMarshaler::GetExactMarshalerMethod(MethodDesc *pGenericMD)
{
    STANDARD_VM_CONTRACT;

    return MethodDesc::FindOrCreateAssociatedMethodDesc(
        pGenericMD,
        pGenericMD->GetMethodTable(),
        FALSE,                              // forceBoxedEntryPoint
        m_pargs->m_pMT->GetInstantiation(), // methodInst
        FALSE,                              // allowInstParam
        TRUE);                              // forceRemotableMethod
}

///////////////////////////////////////////////////////////////////////////////////////////////////
// ILSystemTypeMarshaler implementation
///////////////////////////////////////////////////////////////////////////////////////////////////

LocalDesc ILSystemTypeMarshaler::GetNativeType()
{
    STANDARD_VM_CONTRACT;
    
    return LocalDesc(MscorlibBinder::GetClass(CLASS__TYPENAMENATIVE));
}

LocalDesc ILSystemTypeMarshaler::GetManagedType()
{
    STANDARD_VM_CONTRACT;
    
    return LocalDesc(MscorlibBinder::GetClass(CLASS__TYPE));
}

bool ILSystemTypeMarshaler::NeedsClearNative()
{
    LIMITED_METHOD_CONTRACT;
    return true;
}

void ILSystemTypeMarshaler::EmitConvertContentsCLRToNative(ILCodeStream * pslILEmit)
{
    STANDARD_VM_CONTRACT;

    // SystemTypeMarshaler.ConvertToNative(Type, pTypeName);    
    EmitLoadManagedValue(pslILEmit);
    EmitLoadNativeHomeAddr(pslILEmit);
    pslILEmit->EmitCALL(METHOD__SYSTEMTYPEMARSHALER__CONVERT_TO_NATIVE, 2, 0);
}

void ILSystemTypeMarshaler::EmitConvertContentsNativeToCLR(ILCodeStream * pslILEmit)
{
    STANDARD_VM_CONTRACT;
    
    // type = SystemTypeMarshaler.ConvertNativeToManaged(pTypeName, ref Type);
    EmitLoadNativeHomeAddr(pslILEmit);
    EmitLoadManagedHomeAddr(pslILEmit);
    pslILEmit->EmitCALL(METHOD__SYSTEMTYPEMARSHALER__CONVERT_TO_MANAGED, 2, 0);
}


void ILSystemTypeMarshaler::EmitClearNative(ILCodeStream * pslILEmit)
{
    STANDARD_VM_CONTRACT;
    
    // SystemTypeMarshaler.ClearNative(pTypeName)
    EmitLoadNativeHomeAddr(pslILEmit);
    pslILEmit->EmitCALL(METHOD__SYSTEMTYPEMARSHALER__CLEAR_NATIVE, 1, 0);
}

void ILSystemTypeMarshaler::EmitReInitNative(ILCodeStream * pslILEmit)
{
    EmitLoadNativeHomeAddr(pslILEmit);
    pslILEmit->EmitINITOBJ(pslILEmit->GetToken(MscorlibBinder::GetClass(CLASS__TYPENAMENATIVE)));
}

///////////////////////////////////////////////////////////////////////////////////////////////////
// ILHResultExceptionMarshaler implementation
///////////////////////////////////////////////////////////////////////////////////////////////////

LocalDesc ILHResultExceptionMarshaler::GetNativeType()
{
    LIMITED_METHOD_CONTRACT;
    return LocalDesc(ELEMENT_TYPE_I4);
}

LocalDesc ILHResultExceptionMarshaler::GetManagedType()
{
    LIMITED_METHOD_CONTRACT;
    _ASSERTE(m_pargs->m_pMT != NULL);
    return LocalDesc(m_pargs->m_pMT);
}

bool ILHResultExceptionMarshaler::NeedsClearNative()
{
    LIMITED_METHOD_CONTRACT;
    return false;
}

void ILHResultExceptionMarshaler::EmitConvertContentsCLRToNative(ILCodeStream* pslILEmit)
{
    CONTRACTL
    {
        STANDARD_VM_CHECK;
        PRECONDITION(CheckPointer(pslILEmit));
    }
    CONTRACTL_END;
    
    // int HResultExceptionMarshaler.ConvertManagedToNative(Exception);
    EmitLoadManagedValue(pslILEmit);
    pslILEmit->EmitCALL(METHOD__HRESULTEXCEPTIONMARSHALER__CONVERT_TO_NATIVE, 1, 1);
    EmitStoreNativeValue(pslILEmit);
}

void ILHResultExceptionMarshaler::EmitConvertContentsNativeToCLR(ILCodeStream* pslILEmit)
{
    CONTRACTL
    {
        STANDARD_VM_CHECK;
        PRECONDITION(CheckPointer(pslILEmit));
    }
    CONTRACTL_END;

    // Exception HResultExceptionMarshaler.ConvertNativeToManaged(int hr);
    EmitLoadNativeValue(pslILEmit);
    pslILEmit->EmitCALL(METHOD__HRESULTEXCEPTIONMARSHALER__CONVERT_TO_MANAGED, 1, 1);
    EmitStoreManagedValue(pslILEmit);
}

///////////////////////////////////////////////////////////////////////////////////////////////////
// ILKeyValuePairMarshaler implementation
///////////////////////////////////////////////////////////////////////////////////////////////////

LocalDesc ILKeyValuePairMarshaler::GetNativeType()
{
    LIMITED_METHOD_CONTRACT;
    return LocalDesc(ELEMENT_TYPE_I);
}

LocalDesc ILKeyValuePairMarshaler::GetManagedType()
{
    LIMITED_METHOD_CONTRACT;;    
    return LocalDesc(m_pargs->m_pMT);
}

bool ILKeyValuePairMarshaler::NeedsClearNative()
{
    LIMITED_METHOD_CONTRACT;
    return true;
}

void ILKeyValuePairMarshaler::EmitConvertContentsCLRToNative(ILCodeStream* pslILEmit)
{
    STANDARD_VM_CONTRACT;
    
    // Native = KeyValueMarshaler<K, V>.ConvertToNative([In] ref Managed);
    EmitLoadManagedHomeAddr(pslILEmit);

    MethodDesc *pMD = GetExactMarshalerMethod(MscorlibBinder::GetMethod(METHOD__KEYVALUEPAIRMARSHALER__CONVERT_TO_NATIVE));
    pslILEmit->EmitCALL(pslILEmit->GetToken(pMD), 1, 1);

    EmitStoreNativeValue(pslILEmit);    
}

void ILKeyValuePairMarshaler::EmitConvertContentsNativeToCLR(ILCodeStream* pslILEmit)
{
    STANDARD_VM_CONTRACT;

    // Managed = KeyValuePairMarshaler<K, V>.ConvertToManaged(Native);
    EmitLoadNativeValue(pslILEmit);

    MethodDesc *pMD = GetExactMarshalerMethod(MscorlibBinder::GetMethod(METHOD__KEYVALUEPAIRMARSHALER__CONVERT_TO_MANAGED));
    pslILEmit->EmitCALL(pslILEmit->GetToken(pMD), 1, 1);

    EmitStoreManagedValue(pslILEmit);    
}

void ILKeyValuePairMarshaler::EmitClearNative(ILCodeStream* pslILEmit)
{
    STANDARD_VM_CONTRACT;
    EmitInterfaceClearNative(pslILEmit);
}

MethodDesc *ILKeyValuePairMarshaler::GetExactMarshalerMethod(MethodDesc *pGenericMD)
{
    STANDARD_VM_CONTRACT;

    // KeyValuePairMarshaler methods are generic - find/create the exact method.
    return MethodDesc::FindOrCreateAssociatedMethodDesc(
        pGenericMD,
        pGenericMD->GetMethodTable(),
        FALSE,                              // forceBoxedEntryPoint
        m_pargs->m_pMT->GetInstantiation(), // methodInst
        FALSE,                              // allowInstParam
        TRUE);                              // forceRemotableMethod
}

#endif  // FEATURE_COMINTEROP
