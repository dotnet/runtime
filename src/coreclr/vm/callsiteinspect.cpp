// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "common.h"
#include "object.h"
#include "callsiteinspect.h"

namespace
{
    // Given a frame and value, get a reference to the object
    OBJECTREF GetOBJECTREFFromStack(
        _In_ FramedMethodFrame *frame,
        _In_ PVOID val,
        _In_ const CorElementType eType,
        _In_ TypeHandle ty,
        _In_ BOOL fIsByRef)
    {
        CONTRACT(OBJECTREF)
        {
            THROWS;
            GC_TRIGGERS;
            MODE_COOPERATIVE;
            PRECONDITION(CheckPointer(frame));
            PRECONDITION(CheckPointer(val));
        }
        CONTRACT_END;

        // Value types like Nullable<T> have special unboxing semantics
        if (eType == ELEMENT_TYPE_VALUETYPE)
        {
            // box the value class
            _ASSERTE(ty.GetMethodTable()->IsValueType() || ty.GetMethodTable()->IsEnum());

            MethodTable* pMT = ty.GetMethodTable();

            // What happens when the type contains a stack pointer?
            _ASSERTE(!pMT->IsByRefLike());

            PVOID* pVal = (PVOID *)val;
            if (!fIsByRef)
            {
                val = StackElemEndiannessFixup(val, pMT->GetNumInstanceFieldBytes());
                pVal = &val;
            }

            RETURN (pMT->FastBox(pVal));
        }

        switch (CorTypeInfo::GetGCType(eType))
        {
            case TYPE_GC_NONE:
            {
                if (ELEMENT_TYPE_PTR == eType)
                    COMPlusThrow(kNotSupportedException);

                MethodTable *pMT = CoreLibBinder::GetElementType(eType);

                OBJECTREF pObj = pMT->Allocate();
                if (fIsByRef)
                {
                    val = *((PVOID *)val);
                }
                else
                {
                    val = StackElemEndiannessFixup(val, CorTypeInfo::Size(eType));
                }

                void *pDest = pObj->UnBox();

#ifdef COM_STUBS_SEPARATE_FP_LOCATIONS
                if (!fIsByRef
                    && (ELEMENT_TYPE_R4 == eType || ELEMENT_TYPE_R8 == eType)
                    && frame != nullptr
                    && !TransitionBlock::IsStackArgumentOffset(static_cast<int>((TADDR) val - frame->GetTransitionBlock())))
                {
                    if (ELEMENT_TYPE_R4 == eType)
                        *(UINT32*)pDest = (UINT32)FPSpillToR4(val);
                    else
                        *(UINT64*)pDest = (UINT64)FPSpillToR8(val);
                }
                else
#endif // COM_STUBS_SEPARATE_FP_LOCATIONS
                {
                    memcpyNoGCRefs(pDest, val, CorTypeInfo::Size(eType));
                }

                RETURN (pObj);
            }

            case TYPE_GC_REF:
                if (fIsByRef)
                    val = *((PVOID *)val);
                RETURN (ObjectToOBJECTREF(*(Object **)val));

            default:
                COMPlusThrow(kInvalidOperationException, W("InvalidOperation_TypeCannotBeBoxed"));
        }
    }

    struct ArgDetails
    {
        int Offset;
        BOOL IsByRef;
        CorElementType ElementType;
        TypeHandle Type;
    };

    ArgDetails GetArgDetails(
        _In_ FramedMethodFrame *frame,
        _In_ ArgIterator &pArgIter)
    {
        CONTRACT(ArgDetails)
        {
            THROWS;
            GC_TRIGGERS;
            MODE_ANY;
            PRECONDITION(CheckPointer(frame));
        }
        CONTRACT_END;

        ArgDetails details{};
        details.Offset = pArgIter.GetNextOffset();
        details.ElementType = pArgIter.GetArgType();

#ifdef COM_STUBS_SEPARATE_FP_LOCATIONS
        // BUGBUG do we need to handle this?
        if ((ELEMENT_TYPE_R4 == details.ElementType || ELEMENT_TYPE_R8 == details.ElementType)
            && TransitionBlock::IsArgumentRegisterOffset(details.Offset))
        {
            int iFPArg = TransitionBlock::GetArgumentIndexFromOffset(details.Offset);
            details.Offset = static_cast<int>(frame->GetFPArgOffset(iFPArg));
        }
#endif // COM_STUBS_SEPARATE_FP_LOCATIONS

        // Get the TypeHandle for the argument's type.
        MetaSig *pSig = pArgIter.GetSig();
        details.Type = pSig->GetLastTypeHandleThrowing();

        if (details.ElementType == ELEMENT_TYPE_BYREF)
        {
            details.IsByRef = TRUE;
            // If this is a by-ref arg, GetOBJECTREFFromStack() will dereference "addr" to
            // get the real argument address. Dereferencing now will open a gc hole if "addr"
            // points into the gc heap, and we trigger gc between here and the point where
            // we return the arguments.

            TypeHandle tycopy;
            details.ElementType = pSig->GetByRefType(&tycopy);
            if (details.ElementType == ELEMENT_TYPE_VALUETYPE)
                details.Type = tycopy;
        }
#ifdef ENREGISTERED_PARAMTYPE_MAXSIZE
        else if (details.ElementType == ELEMENT_TYPE_VALUETYPE)
        {
            details.IsByRef = ArgIterator::IsArgPassedByRef(details.Type);
        }
#endif // ENREGISTERED_PARAMTYPE_MAXSIZE

        RETURN (details);
    }

    INT64 CopyOBJECTREFToStack(
        _In_ OBJECTREF *src,
        _In_opt_ PVOID pvDest,
        _In_ CorElementType typ,
        _In_ TypeHandle ty,
        _In_ MetaSig *pSig,
        _In_ BOOL fCopyClassContents)
    {
        // Use local to ensure proper alignment
        INT64 ret = 0;

        CONTRACT(INT64)
        {
            THROWS;
            GC_TRIGGERS;
            MODE_COOPERATIVE;
            INJECT_FAULT(COMPlusThrowOM());
            PRECONDITION(CheckPointer(pvDest, NULL_OK));
            PRECONDITION(CheckPointer(pSig));
            PRECONDITION(typ != ELEMENT_TYPE_VOID);
        }
        CONTRACT_END;

        if (fCopyClassContents)
        {
            // We have to copy the contents of a value class to pvDest

            // write unboxed version back to memory provided by the caller
            if (pvDest)
            {
                if (ty.IsNull())
                    ty = pSig->GetRetTypeHandleThrowing();

                _ASSERTE((*src) != NULL || Nullable::IsNullableType(ty));
#ifdef TARGET_UNIX
                // Unboxing on non-Windows ABIs must be special cased
                COMPlusThrowHR(COR_E_NOTSUPPORTED);
#else
                ty.GetMethodTable()->UnBoxIntoUnchecked(pvDest, (*src));
#endif

                // return the object so it can be stored in the frame and
                // propagated to the root set
                *(OBJECTREF*)&ret  = (*src);
            }
        }
        else if (CorTypeInfo::IsObjRef(typ))
        {
            // We have a real OBJECTREF

            // Check if it is an OBJECTREF (from the GC heap)
            if (pvDest)
                SetObjectReference((OBJECTREF *)pvDest, *src);

            *(OBJECTREF*)&ret = (*src);
        }
        else
        {
            // We have something that does not have a return buffer associated.

            // Note: this assert includes ELEMENT_TYPE_VALUETYPE because for enums,
            // ArgIterator::HasRetBuffArg() returns 'false'. This occurs because the
            // normalized type for enums is ELEMENT_TYPE_I4 even though
            // MetaSig::GetReturnType() returns ELEMENT_TYPE_VALUETYPE.
            // Almost all ELEMENT_TYPE_VALUETYPEs will go through the copy class
            // contents codepath above.
            // Also, CorTypeInfo::IsPrimitiveType() does not check for IntPtr, UIntPtr
            // hence we have ELEMENT_TYPE_I and ELEMENT_TYPE_U.
            _ASSERTE(
                CorTypeInfo::IsPrimitiveType(typ)
                || (typ == ELEMENT_TYPE_VALUETYPE)
                || (typ == ELEMENT_TYPE_I)
                || (typ == ELEMENT_TYPE_U)
                || (typ == ELEMENT_TYPE_FNPTR));

            // For a "ref int" arg, if a nasty sink replaces the boxed int with
            // a null OBJECTREF, this is where we check. We need to be uniform
            // in our policy w.r.t. this (throw vs ignore).
            // The branch above throws.
            if ((*src) != NULL)
            {
                PVOID srcData = (*src)->GetData();
                int cbsize = gElementTypeInfo[typ].m_cbSize;
                decltype(ret) retBuff;

                // ElementTypeInfo.m_cbSize can be less than zero for cases that need
                // special handling (e.g. value types) to be sure of the size (see siginfo.cpp).
                // The type handle has the actual byte count, so we look there for such cases.
                if (cbsize < 0)
                {
                    if (ty.IsNull())
                        ty = pSig->GetRetTypeHandleThrowing();

                    _ASSERTE(!ty.IsNull());
                    cbsize = ty.GetSize();

                    // Assert the value class fits in the buffer
                    _ASSERTE(cbsize <= (int) sizeof(retBuff));

                    // Unbox value into a local buffer, this covers the Nullable<T> case.
                    ty.GetMethodTable()->UnBoxIntoUnchecked(&retBuff, *src);

                    srcData = &retBuff;
                }

                if (pvDest)
                    memcpyNoGCRefs(pvDest, srcData, cbsize);

                // need to sign-extend signed types
                bool fEndiannessFixup = false;
                switch (typ)
                {
                case ELEMENT_TYPE_I1:
                    ret = *(INT8*)srcData;
                    fEndiannessFixup = true;
                    break;
                case ELEMENT_TYPE_I2:
                    ret = *(INT16*)srcData;
                    fEndiannessFixup = true;
                    break;
                case ELEMENT_TYPE_I4:
                    ret = *(INT32*)srcData;
                    fEndiannessFixup = true;
                    break;
                default:
                    memcpyNoGCRefs(StackElemEndiannessFixup(&ret, cbsize), srcData, cbsize);
                    break;
                }

#if !defined(HOST_64BIT) && BIGENDIAN
                if (fEndiannessFixup)
                    ret <<= 32;
#endif
            }
        }

        RETURN (ret);
    }
}

void CallsiteInspect::GetCallsiteArgs(
        _In_ CallsiteDetails &callsite,
        _Outptr_ PTRARRAYREF *args,
        _Outptr_ BOOLARRAYREF *argsIsByRef,
        _Outptr_ PTRARRAYREF *argsTypes)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(CheckPointer(args));
        PRECONDITION(CheckPointer(argsIsByRef));
        PRECONDITION(CheckPointer(argsTypes));
    }
    CONTRACTL_END;

    struct _gc
    {
        PTRARRAYREF Args;
        PTRARRAYREF ArgsTypes;
        BOOLARRAYREF ArgsIsByRef;
        OBJECTREF CurrArgType;
        OBJECTREF CurrArg;
    } gc;
    ZeroMemory(&gc, sizeof(gc));
    GCPROTECT_BEGIN(gc);
    {
        // Ensure the sig is in a known state
        callsite.MetaSig.Reset();

        // scan the sig for the argument count
        INT32 numArgs = callsite.MetaSig.NumFixedArgs();
        if (callsite.IsDelegate)
            numArgs -= 2; // Delegates have 2 implicit additional arguments

        // Allocate all needed arrays for callsite arg details
        gc.Args = (PTRARRAYREF)AllocateObjectArray(numArgs, g_pObjectClass);
        MethodTable *typeMT = CoreLibBinder::GetClass(CLASS__TYPE);
        gc.ArgsTypes = (PTRARRAYREF)AllocateObjectArray(numArgs, typeMT);
        gc.ArgsIsByRef = (BOOLARRAYREF)AllocatePrimitiveArray(ELEMENT_TYPE_BOOLEAN, numArgs);

        ArgIterator iter{ &callsite.MetaSig };
        for (int index = 0; index < numArgs; index++)
        {
            ArgDetails details = GetArgDetails(callsite.Frame, iter);
            PVOID addr = (LPBYTE)callsite.Frame->GetTransitionBlock() + details.Offset;

            // How do we handle pointer types?
            _ASSERTE(details.ElementType != ELEMENT_TYPE_PTR);

            gc.CurrArg = GetOBJECTREFFromStack(
                callsite.Frame,
                addr,
                details.ElementType,
                details.Type,
                details.IsByRef);

            // Store argument
            gc.Args->SetAt(index, gc.CurrArg);

            // Record the argument's type
            gc.CurrArgType = details.Type.GetManagedClassObject();
            _ASSERTE(gc.CurrArgType != NULL);
            gc.ArgsTypes->SetAt(index, gc.CurrArgType);

            // Record if the argument is ByRef
            *((UCHAR*)gc.ArgsIsByRef->GetDataPtr() + index) = (!!details.IsByRef);
        }
    }
    GCPROTECT_END();

    // Return details
    *args = gc.Args;
    *argsTypes = gc.ArgsTypes;
    *argsIsByRef = gc.ArgsIsByRef;
}

void CallsiteInspect::PropagateOutParametersBackToCallsite(
    _In_ PTRARRAYREF outArgs,
    _In_ OBJECTREF retVal,
    _In_ CallsiteDetails &callsite)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;

    struct _gc
    {
        OBJECTREF RetVal;
        PTRARRAYREF OutArgs;
        OBJECTREF CurrArg;
    } gc;
    ZeroMemory(&gc, sizeof(gc));
    gc.OutArgs = outArgs;
    gc.RetVal = retVal;
    GCPROTECT_BEGIN(gc);
    {
        FramedMethodFrame *frame = callsite.Frame;
        const INT32 flags = callsite.Flags;
        MetaSig *pSig = &callsite.MetaSig;
        pSig->Reset(); // Ensure the sig is in a known state

        // Construct an ArgIterator from the sig
        ArgIterator argit{ pSig };

        // Propagate the return value only if the call is not a constructor call
        // and the return type is non-void
        if ((flags & CallsiteDetails::Ctor) == 0
            && pSig->GetReturnType() != ELEMENT_TYPE_VOID)
        {
            if (argit.HasRetBuffArg())
            {
                // Copy from RetVal into the retBuff.
                INT64 retVal =  CopyOBJECTREFToStack(
                                    &gc.RetVal,
                                    *(void**)(frame->GetTransitionBlock() + argit.GetRetBuffArgOffset()),
                                    pSig->GetReturnType(),
                                    TypeHandle{},
                                    pSig,
                                    TRUE /* should copy */);

                // Copy the return value
                *(ARG_SLOT *)(frame->GetReturnValuePtr()) = retVal;
            }
#ifdef ENREGISTERED_RETURNTYPE_MAXSIZE
            else if (argit.HasNonStandardByvalReturn())
            {
                // In these cases, put the pointer to the return buffer into
                // the frame's return value slot.
                CopyOBJECTREFToStack(
                    &gc.RetVal,
                    frame->GetReturnValuePtr(),
                    pSig->GetReturnType(),
                    TypeHandle(),
                    pSig,
                    TRUE /* should copy */);
            }
#endif // ENREGISTERED_RETURNTYPE_MAXSIZE
            else
            {
                // There is no separate return buffer,
                // the retVal should fit in an INT64.
                INT64 retVal = CopyOBJECTREFToStack(
                                    &gc.RetVal,
                                    nullptr,
                                    pSig->GetReturnType(),
                                    TypeHandle{},
                                    pSig,
                                    FALSE /* should copy */);

                // Copy the return value
                *(ARG_SLOT *)(frame->GetReturnValuePtr()) = retVal;
            }
        }

        // Refetch all the variables as GC could have happened
        // after copying the return value.
        UINT32 cOutArgs = (gc.OutArgs != NULL) ? gc.OutArgs->GetNumComponents() : 0;
        if (cOutArgs > 0)
        {
            MetaSig syncSig{ callsite.MethodDesc };
            MetaSig *pSyncSig = nullptr;

            if (flags & CallsiteDetails::EndInvoke)
                pSyncSig = &syncSig;

            PVOID *argAddr;
            for (UINT32 i = 0; i < cOutArgs; ++i)
            {
                // Determine the address of the argument
                if (pSyncSig)
                {
                    CorElementType typ = pSyncSig->NextArg();
                    if (typ == ELEMENT_TYPE_END)
                        break;

                    if (typ != ELEMENT_TYPE_BYREF)
                        continue;

                    argAddr = reinterpret_cast<PVOID *>(frame->GetTransitionBlock() + argit.GetNextOffset());
                }
                else
                {
                    int ofs = argit.GetNextOffset();
                    if (ofs == TransitionBlock::InvalidOffset)
                        break;

                    if (argit.GetArgType() != ELEMENT_TYPE_BYREF)
                        continue;

                    argAddr = reinterpret_cast<PVOID *>(frame->GetTransitionBlock() + ofs);
                }

                TypeHandle ty;
                CorElementType brType = pSig->GetByRefType(&ty);

                gc.CurrArg = gc.OutArgs->GetAt(i);
                CopyOBJECTREFToStack(
                    &gc.CurrArg,
                    *argAddr,
                    brType,
                    ty,
                    pSig,
                    ty.IsNull() ? FALSE : ty.IsValueType());
            }
        }
    }
    GCPROTECT_END();
}
