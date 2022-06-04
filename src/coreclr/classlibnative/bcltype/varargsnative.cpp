// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// File: VarArgsNative.cpp
//

//
// This module contains the implementation of the native methods for the
//  varargs class(es)..
//


#include "common.h"
#include "object.h"
#include "excep.h"
#include "frames.h"
#include "vars.hpp"
#include "varargsnative.h"

// Some platforms have additional alignment requirements for arguments. This function adjusts the given arg
// pointer to achieve such an alignment for the next argument on those platforms (otherwise it is a no-op).
// NOTE: the debugger has its own implementation of this algorithm in Debug\DI\RsType.cpp, CordbType::RequiresAlign8()
//       so if you change this implementation be sure to update the debugger's version as well.
static void AdjustArgPtrForAlignment(VARARGS *pData, unsigned cbArg)
{
#ifdef TARGET_ARM
    // Only 64-bit primitives or value types with embedded 64-bit primitives are aligned on 64-bit boundaries.
    if (cbArg < 8)
        return;

    // For the value type case we have to dig deeper and ask the typeloader whether the type requires
    // alignment.
    SigTypeContext typeContext; // This is an empty type context. This is OK because the vararg methods may not be generic.
    CorElementType et = pData->SigPtr.PeekElemTypeClosed(pData->ArgCookie->pModule, &typeContext);
    if (et == ELEMENT_TYPE_TYPEDBYREF)
    {
        return;
    }
    else
    if (et == ELEMENT_TYPE_VALUETYPE)
    {
        SigPointer tempSig(pData->SigPtr);
        TypeHandle valueType = tempSig.GetTypeHandleThrowing(pData->ArgCookie->pModule, &typeContext);
        if (!valueType.AsMethodTable()->RequiresAlign8())
            return;
    }
    else
    {
        // One of the primitive 64-bit types
    }
    pData->ArgPtr = (BYTE*)ALIGN_UP(pData->ArgPtr, 8);
#endif // TARGET_ARM
}

////////////////////////////////////////////////////////////////////////////////
// Initialize the basic info for processing a varargs parameter list.
////////////////////////////////////////////////////////////////////////////////
static void InitCommon(VARARGS *data, VASigCookie** cookie)
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(CheckPointer(data));
        PRECONDITION(CheckPointer(cookie));
    } CONTRACTL_END;

    // Save the cookie and a copy of the signature.
    data->ArgCookie = *cookie;
    data->SigPtr    = data->ArgCookie->signature.CreateSigPointer();

    // Skip the calling convention, get the # of args and skip the return type.
    uint32_t callConv;
    IfFailThrow(data->SigPtr.GetCallingConvInfo(&callConv));

    uint32_t sigData;
    IfFailThrow(data->SigPtr.GetData(&sigData));
    data->RemainingArgs = sigData;

    IfFailThrow(data->SigPtr.SkipExactlyOne());

    // Get a pointer to the cookie arg.
    data->ArgPtr = (BYTE *) cookie;

#if defined(TARGET_X86)
    //  STACK_GROWS_DOWN_ON_ARGS_WALK

    //   <return address>  ;; lower memory
    //   <varargs_cookie>         '\'
    //   <argN>                    '\'
    //                              += sizeOfArgs
    //                             /
    //   <arg1>                   /
    // * <this>            ;; if an instance method (note: <this> is usually passed in
    //                     ;; a register and wouldn't appear on the stack frame)
    //                     ;; higher memory
    //
    // When the stack grows down, ArgPtr always points to the end of the next
    // argument passed. So we initialize it to the address that is the
    // end of the first fixed arg (arg1) (marked with a '*').

    data->ArgPtr += data->ArgCookie->sizeOfArgs;
#else
    //  STACK_GROWS_UP_ON_ARGS_WALK

    //   <this>	           ;; lower memory
    //   <varargs_cookie>  ;; if an instance method
    // * <arg1>
    //
    //   <argN>            ;; higher memory
    //
    // When the stack grows up, ArgPtr always points to the start of the next
    // argument passed. So we initialize it to the address marked with a '*',
    // which is the start of the first fixed arg (arg1).

    // Always skip over the varargs_cookie.
    const bool isValueType = false;
    const bool isFloatHfa = false;
    data->ArgPtr += StackElemSize(TARGET_POINTER_SIZE, isValueType, isFloatHfa);
#endif
}

////////////////////////////////////////////////////////////////////////////////
// After initialization advance the next argument pointer to the first optional
// argument.
////////////////////////////////////////////////////////////////////////////////
void AdvanceArgPtr(VARARGS *data)
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(CheckPointer(data));
    } CONTRACTL_END;

    // Advance to the first optional arg.
    while (data->RemainingArgs > 0)
    {
        if (data->SigPtr.AtSentinel())
            break;

        SigTypeContext      typeContext; // This is an empty type context.  This is OK because the vararg methods may not be generic
        TypeHandle thValueType;
        const unsigned cbRaw = data->SigPtr.SizeOf(data->ArgCookie->pModule, &typeContext, &thValueType);
        const bool isValueType = (!thValueType.IsNull() && thValueType.IsValueType());
        const bool isFloatHfa = false;
        unsigned cbArg = StackElemSize(cbRaw, isValueType, isFloatHfa);
#ifdef ENREGISTERED_PARAMTYPE_MAXSIZE
        if (ArgIterator::IsVarArgPassedByRef(cbRaw))
            cbArg = sizeof(void*);
#endif

        // Adjust the frame pointer and the signature info.
        AdjustArgPtrForAlignment(data, cbArg);
#ifdef STACK_GROWS_DOWN_ON_ARGS_WALK
        data->ArgPtr -= cbArg;
#else  // STACK_GROWS_UP_ON_ARGS_WALK
        data->ArgPtr += cbArg;
#endif // STACK_GROWS_**_ON_ARGS_WALK

        IfFailThrow(data->SigPtr.SkipExactlyOne());
        --data->RemainingArgs;
    }
} // AdvanceArgPtr

////////////////////////////////////////////////////////////////////////////////
// ArgIterator constructor that initializes the state to support iteration
// of the args starting at the first optional argument.
////////////////////////////////////////////////////////////////////////////////
FCIMPL2(void, VarArgsNative::Init, VARARGS* _this, LPVOID cookie)
{
    FCALL_CONTRACT;

    HELPER_METHOD_FRAME_BEGIN_0();

    _ASSERTE(_this != NULL);
    VARARGS* data = _this;
    if (cookie == 0)
        COMPlusThrow(kArgumentException, W("InvalidOperation_HandleIsNotInitialized"));

    VASigCookie* pCookie = *(VASigCookie**)(cookie);

    if (pCookie->signature.IsEmpty())
    {
        data->SigPtr = SigPointer(NULL, 0);
        data->ArgCookie = NULL;
        data->ArgPtr = (BYTE*)((VASigCookieEx*)pCookie)->m_pArgs;
    }
    else
    {
        // Use common code to pick the cookie apart and advance to the ...
        InitCommon(data, (VASigCookie**)cookie);
        AdvanceArgPtr(data);
    }

    HELPER_METHOD_FRAME_END();
}
FCIMPLEND

////////////////////////////////////////////////////////////////////////////////
// ArgIterator constructor that initializes the state to support iteration
// of the args starting at the argument following the supplied argument pointer.
// Specifying NULL as the firstArg parameter causes it to start at the first
// argument to the call.
////////////////////////////////////////////////////////////////////////////////
FCIMPL3(
void,
VarArgsNative::Init2,
    VARARGS * _this,
    LPVOID    cookie,
    LPVOID    firstArg)
{
    FCALL_CONTRACT;

    HELPER_METHOD_FRAME_BEGIN_0();

    _ASSERTE(_this != NULL);
    VARARGS* data = _this;
    if (cookie == 0)
        COMPlusThrow(kArgumentException, W("InvalidOperation_HandleIsNotInitialized"));

    // Init most of the structure.
    InitCommon(data, (VASigCookie**)cookie);

    // If it is NULL, start at the first arg.
    if (firstArg != NULL)
    {
        //
        // The expectation made by VarArgsNative is that:
#ifdef STACK_GROWS_DOWN_ON_ARGS_WALK
        // data->ArgPtr points to the end of the next argument.
        //    <varargs_cookie>
        //    <argN>                   <-- data->ArgPtr after InitCommon
        //
        //    <argM 1st optional arg>
        // *@ <arg2>                   <-- firstArg, data->ArgPtr leaving Init2()
        //    <arg1>
        //    <this>            ;; if an instance method
        //                     ;; higher memory
        //
#else // STACK_GROWS_UP_ON_ARGS_WALK
        // data->ArgPtr points to the beginning of the next argument
        //    <this>            ;; if an instance method
        //    <varargs_cookie>
        //    <arg1>                     <-- data->ArgPtr after InitCommon
        // *  <arg2>                     <-- firstArg
        //  @ <argM - 1st optional arg>  <-- data->ArgPtr leaving Init2()
        //
        //    <argN>
        //                     ;; higher memory
#endif // STACK_GROWS_**_ON_ARGS_WALK
        // where * indicates the place on the stack that firstArg points upon
        // entry to Init2. We need to correct in the STACK_GROWS_UP... case since
        // we actually want to point at the argument after firstArg. This confusion
        // comes from the difference in expectation of whether ArgPtr is pointing
        // at the beginning or end of the argument on the stack.
        //
        // @ indicates where we want data->ArgPtr to point to after we're done with Init2
        //

        // Advance to the specified arg.
        while (data->RemainingArgs > 0)
        {
            if (data->SigPtr.AtSentinel())
            {
                COMPlusThrow(kArgumentException);
            }

            SigTypeContext typeContext; // This is an empty type context.  This is OK because the vararg methods may not be generic
            TypeHandle thValueType;
            unsigned cbRaw = data->SigPtr.SizeOf(data->ArgCookie->pModule,&typeContext, &thValueType);
            const bool isValueType = (!thValueType.IsNull() && thValueType.IsValueType());
            const bool isFloatHfa = false;
            unsigned cbArg = StackElemSize(cbRaw, isValueType, isFloatHfa);
#ifdef ENREGISTERED_PARAMTYPE_MAXSIZE
            if (ArgIterator::IsVarArgPassedByRef(cbRaw))
                cbArg = sizeof(void*);
#endif

            // Adjust the frame pointer and the signature info.
            AdjustArgPtrForAlignment(data, cbArg);
            IfFailThrow(data->SigPtr.SkipExactlyOne());
            data->RemainingArgs--;

#ifdef STACK_GROWS_DOWN_ON_ARGS_WALK
            data->ArgPtr -= cbArg;
            bool atFirstArg = (data->ArgPtr == firstArg);
#else  // STACK_GROWS_UP_ON_ARGS_WALK
            bool atFirstArg = (data->ArgPtr == firstArg);
            data->ArgPtr += cbArg;
#endif // STACK_GROWS_**_ON_ARGS_WALK

            if (atFirstArg)
                break;
        }
    }
    HELPER_METHOD_FRAME_END();
} // VarArgsNative::Init2
FCIMPLEND


////////////////////////////////////////////////////////////////////////////////
// Return the number of unprocessed args in the argument iterator.
////////////////////////////////////////////////////////////////////////////////
FCIMPL1(int, VarArgsNative::GetRemainingCount, VARARGS* _this)
{
    FCALL_CONTRACT;

    HELPER_METHOD_FRAME_BEGIN_RET_0();

    _ASSERTE(_this != NULL);
    if (!(_this->ArgCookie))
    {
        // this argiterator was created by marshaling from an unmanaged va_list -
        // can't do this operation
        COMPlusThrow(kNotSupportedException);
    }
    HELPER_METHOD_FRAME_END();
    return (_this->RemainingArgs);
}
FCIMPLEND


////////////////////////////////////////////////////////////////////////////////
// Retrieve the type of the next argument without consuming it.
////////////////////////////////////////////////////////////////////////////////
FCIMPL1(void*, VarArgsNative::GetNextArgType, VARARGS* _this)
{
    FCALL_CONTRACT;

    TypedByRef  value;

    HELPER_METHOD_FRAME_BEGIN_RET_0();

    PREFIX_ASSUME(_this != NULL);
    VARARGS     data = *_this;

    if (!(_this->ArgCookie))
    {
        // this argiterator was created by marshaling from an unmanaged va_list -
        // can't do this operation
        COMPlusThrow(kNotSupportedException);
    }


    // Make sure there are remaining args.
    if (data.RemainingArgs == 0)
        COMPlusThrow(kInvalidOperationException, W("InvalidOperation_EnumEnded"));

    GetNextArgHelper(&data, &value, FALSE);
    HELPER_METHOD_FRAME_END();
    return value.type.AsPtr();
}
FCIMPLEND

////////////////////////////////////////////////////////////////////////////////
// Retrieve the next argument and return it in a TypedByRef and advance the
// next argument pointer.
////////////////////////////////////////////////////////////////////////////////
FCIMPL2(void, VarArgsNative::DoGetNextArg, VARARGS* _this, void * value)
{
    FCALL_CONTRACT;

    TypedByRef * result = (TypedByRef *)value;
    HELPER_METHOD_FRAME_BEGIN_0();
    GCPROTECT_BEGININTERIOR (result);

    _ASSERTE(_this != NULL);
    if (!(_this->ArgCookie))
    {
        // this argiterator was created by marshaling from an unmanaged va_list -
        // can't do this operation
        COMPlusThrow(kInvalidOperationException);
    }

    // Make sure there are remaining args.
    if (_this->RemainingArgs == 0)
        COMPlusThrow(kInvalidOperationException, W("InvalidOperation_EnumEnded"));

    GetNextArgHelper(_this, result, TRUE);
    GCPROTECT_END ();
    HELPER_METHOD_FRAME_END();
}
FCIMPLEND



////////////////////////////////////////////////////////////////////////////////
// Retrieve the next argument and return it in a TypedByRef and advance the
// next argument pointer.
////////////////////////////////////////////////////////////////////////////////
FCIMPL3(void, VarArgsNative::GetNextArg2, VARARGS* _this, void * value, ReflectClassBaseObject *pTypeUNSAFE)
{
    FCALL_CONTRACT;

    TypedByRef * result = (TypedByRef *)value;
    REFLECTCLASSBASEREF refType = (REFLECTCLASSBASEREF)ObjectToOBJECTREF(pTypeUNSAFE);

    if (refType == NULL)
        FCThrowResVoid(kArgumentNullException, W("Arg_InvalidHandle"));

    HELPER_METHOD_FRAME_BEGIN_1(refType);
    GCPROTECT_BEGININTERIOR (result);

    // IJW

    TypeHandle typehandle = refType->GetType();

    _ASSERTE(_this != NULL);
    unsigned size = 0;
    bool isValueType = false;

    CorElementType typ = typehandle.GetInternalCorElementType();
    if (CorTypeInfo::IsPrimitiveType(typ))
    {
        size = CorTypeInfo::Size(typ);
    }
    else if (typ == ELEMENT_TYPE_PTR)
    {
        size = sizeof(LPVOID);
    }
    else if (typ == ELEMENT_TYPE_VALUETYPE)
    {
        isValueType = true;
        size = typehandle.AsMethodTable()->GetNativeSize();
    }
    else
    {
        COMPlusThrow(kNotSupportedException, W("NotSupported_Type"));
    }
    const bool isFloatHfa = false;
    size = StackElemSize(size, isValueType, isFloatHfa);
    AdjustArgPtrForAlignment(_this, size);

#ifdef ENREGISTERED_PARAMTYPE_MAXSIZE
    if (ArgIterator::IsVarArgPassedByRef(size))
    {
        result->data = *(void**)_this->ArgPtr;
        size = sizeof(void*);
    }
    else
#endif
    {
        result->data = (void*)_this->ArgPtr;
    }

    result->type = typehandle;
    _this->ArgPtr += size;

    GCPROTECT_END ();
    HELPER_METHOD_FRAME_END();
}
FCIMPLEND



////////////////////////////////////////////////////////////////////////////////
// This is a helper that uses a VARARGS tracking data structure to retrieve
// the next argument out of a varargs function call.  This does not check if
// there are any args remaining (it assumes it has been checked).
////////////////////////////////////////////////////////////////////////////////
void
VarArgsNative::GetNextArgHelper(
    VARARGS *    data,
    TypedByRef * value,
    BOOL         fData)
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(CheckPointer(data));
        PRECONDITION(CheckPointer(value));
    } CONTRACTL_END;

    GCPROTECT_BEGININTERIOR (value);
    CorElementType elemType;

    _ASSERTE(data->RemainingArgs != 0);

    SigTypeContext typeContext; // This is an empty type context.  This is OK because the vararg methods may not be generic
    TypeHandle thValueType;
    const unsigned cbRaw = data->SigPtr.SizeOf(data->ArgCookie->pModule,&typeContext, &thValueType);
    const bool isValueType = (!thValueType.IsNull() && thValueType.IsValueType());
    const bool isFloatHfa = false;
    unsigned cbArg = StackElemSize(cbRaw, isValueType, isFloatHfa);
    AdjustArgPtrForAlignment(data, cbArg);

    // Get a pointer to the beginning of the argument.
#ifdef STACK_GROWS_DOWN_ON_ARGS_WALK
    data->ArgPtr -= cbArg;
#endif

    // Assume the ref pointer points directly at the arg on the stack.
    void* origArgPtr = data->ArgPtr;
    value->data = origArgPtr;

#ifndef STACK_GROWS_DOWN_ON_ARGS_WALK
    data->ArgPtr += cbArg;
#endif // STACK_GROWS_**_ON_ARGS_WALK


TryAgain:
    switch (elemType = data->SigPtr.PeekElemTypeClosed(data->ArgCookie->pModule, &typeContext))
    {
        case ELEMENT_TYPE_BOOLEAN:
        case ELEMENT_TYPE_I1:
        case ELEMENT_TYPE_U1:
#if BIGENDIAN
        if ( origArgPtr == value->data ) {
            value->data = (BYTE*)origArgPtr + (sizeof(void*)-1);
        }
#endif
        value->type = CoreLibBinder::GetElementType(elemType);
        break;

        case ELEMENT_TYPE_I2:
        case ELEMENT_TYPE_U2:
        case ELEMENT_TYPE_CHAR:
#if BIGENDIAN
        if ( origArgPtr == value->data ) {
            value->data = (BYTE*)origArgPtr + (sizeof(void*)-2);
        }
#endif
        value->type = CoreLibBinder::GetElementType(elemType);
        break;

        case ELEMENT_TYPE_I4:
        case ELEMENT_TYPE_U4:
        case ELEMENT_TYPE_R4:
        case ELEMENT_TYPE_STRING:
        case ELEMENT_TYPE_I:
        case ELEMENT_TYPE_U:
        value->type = CoreLibBinder::GetElementType(elemType);
        break;

        case ELEMENT_TYPE_I8:
        case ELEMENT_TYPE_U8:
        case ELEMENT_TYPE_R8:
        value->type = CoreLibBinder::GetElementType(elemType);
#if !defined(HOST_64BIT) && (DATA_ALIGNMENT > 4)
        if ( fData && origArgPtr == value->data ) {
            // allocate an aligned copy of the value
            value->data = value->type.AsMethodTable()->Box(origArgPtr, FALSE)->UnBox();
        }
#endif
        break;

        case ELEMENT_TYPE_PTR:
            value->type = data->SigPtr.GetTypeHandleThrowing(data->ArgCookie->pModule, &typeContext);
            break;

        case ELEMENT_TYPE_BYREF:
            // Check if we have already processed a by-ref.
            if (value->data != origArgPtr)
            {
                _ASSERTE(!"Can't have a ByRef of a ByRef");
                COMPlusThrow(kNotSupportedException, W("NotSupported_Type"));
            }

            // Dereference the argument to remove the indirection of the ByRef.
            value->data = *((void **) value->data);

            // Consume and discard the element type.
            IfFailThrow(data->SigPtr.GetElemType(NULL));
            goto TryAgain;

        case ELEMENT_TYPE_VALUETYPE:

#ifdef ENREGISTERED_PARAMTYPE_MAXSIZE
            if (origArgPtr == value->data && ArgIterator::IsVarArgPassedByRef(cbRaw))
            {
                // Adjust the arg pointer so only one word has been skipped
                data->ArgPtr = (BYTE *)origArgPtr + sizeof(void*);
                // Dereference the argument because the valuetype is passed by reference
                value->data = *((void **) origArgPtr);
            }
#endif

#if BIGENDIAN
            // Adjust the pointer for small valuetypes
            if (origArgPtr == value->data) {
                value->data = StackElemEndianessFixup(origArgPtr, cbRaw);
            }
#endif

            FALLTHROUGH;

        case ELEMENT_TYPE_CLASS: {
            value->type = data->SigPtr.GetTypeHandleThrowing(data->ArgCookie->pModule, &typeContext);

            if (value->type.AsMethodTable()->IsByRefLike())
            {
                COMPlusThrow(kNotSupportedException, W("NotSupported_Type"));
            }

            if (elemType == ELEMENT_TYPE_CLASS && value->type.AsMethodTable()->IsValueType())
                value->type = g_pObjectClass;
            } break;

        case ELEMENT_TYPE_TYPEDBYREF:
            if (value->data != origArgPtr)
            {
                //<TODO>@todo: Is this really an error?</TODO>
                _ASSERTE(!"Can't have a ByRef of a TypedByRef");
                COMPlusThrow(kNotSupportedException, W("NotSupported_Type"));
            }
#ifdef ENREGISTERED_PARAMTYPE_MAXSIZE
            if (ArgIterator::IsVarArgPassedByRef(sizeof(TypedByRef)))
            {
                // Adjust the arg pointer so only one word has been skipped
                data->ArgPtr = (BYTE *)origArgPtr + sizeof(void *);
                // Dereference the argument because the valuetypes are always passed by reference
                value->data = *((void **)origArgPtr);
            }
#endif // ENREGISTERED_PARAMTYPE_MAXSIZE
            // Load the TypedByRef
            value->type = ((TypedByRef*)value->data)->type;
            value->data = ((TypedByRef*)value->data)->data;
            break;

        case ELEMENT_TYPE_SZARRAY:
        case ELEMENT_TYPE_ARRAY:
            {
                value->type = data->SigPtr.GetTypeHandleThrowing(data->ArgCookie->pModule,&typeContext);

                break;
            }

        case ELEMENT_TYPE_FNPTR:
        case ELEMENT_TYPE_OBJECT:
            _ASSERTE(!"Not implemented");
            COMPlusThrow(kNotSupportedException);
            break;

        case ELEMENT_TYPE_SENTINEL:
        default:
            _ASSERTE(!"Should be unreachable");
            COMPlusThrow(kNotSupportedException, W("NotSupported_Type"));
            break;
    }

    // Update the tracking stuff to move past the argument.
    --data->RemainingArgs;
    IfFailThrow(data->SigPtr.SkipExactlyOne());
    GCPROTECT_END ();
} // VarArgsNative::GetNextArgHelper
