// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================
**
** File:    message.cpp
**
** Purpose: Encapsulates a function call frame into a message
**          object with an interface that can enumerate the
**          arguments of the message
**
**

===========================================================*/
#include "common.h"

#ifdef FEATURE_REMOTING

#include "comdelegate.h"
#include "excep.h"
#include "message.h"
#include "remoting.h"
#include "field.h"
#include "eeconfig.h"
#include "invokeutil.h"
#include "callingconvention.h"

//+----------------------------------------------------------------------------
//
//  Method:     CMessage::GetArgCount public
//
//  Synopsis:   Returns number of arguments in the method call
// 
//+----------------------------------------------------------------------------
FCIMPL1(INT32, CMessage::GetArgCount, MessageObject * pMessage)
{
    CONTRACTL
    {
        FCALL_CHECK;
        PRECONDITION(CheckPointer(pMessage));
    }
    CONTRACTL_END;

    LOG((LF_REMOTING, LL_INFO10, "CMessage::GetArgCount IN pMsg:0x%x\n", pMessage));

    // Get the frame pointer from the object
    MetaSig *pSig = pMessage->GetResetMetaSig();

    // scan the sig for the argument count
    INT32 ret = pSig->NumFixedArgs();

    if (pMessage->GetDelegateMD())
        ret -= 2;

    LOG((LF_REMOTING, LL_INFO10, "CMessage::GetArgCount OUT ret:0x%x\n", ret));
    return ret;
}
FCIMPLEND

//+----------------------------------------------------------------------------
//
//  Method:     CMessage::GetArg public
//
//  Synopsis:   Use to enumerate a call's arguments
// 
//+----------------------------------------------------------------------------
FCIMPL2(Object*, CMessage::GetArg, MessageObject* pMessageUNSAFE, INT32 argNum)
{
    CONTRACTL
    {
        FCALL_CHECK;
        PRECONDITION(pMessageUNSAFE != NULL);
    }
    CONTRACTL_END;

    struct _gc {
        OBJECTREF refRetVal;
        MESSAGEREF pMessage;
    } gc;

    gc.refRetVal = NULL;
    gc.pMessage = (MESSAGEREF) pMessageUNSAFE;

    HELPER_METHOD_FRAME_BEGIN_RET_PROTECT(gc);

    LOG((LF_REMOTING, LL_INFO10, "CMessage::GetArgCount IN\n"));

    MetaSig *pSig = gc.pMessage->GetResetMetaSig();

    if ((UINT)argNum >= pSig->NumFixedArgs())
        COMPlusThrow(kTargetParameterCountException);

    for (INT32 i = 0; i < argNum; i++)
        pSig->NextArg();

    BOOL fIsByRef = FALSE;
    CorElementType eType = pSig->NextArg();
    TypeHandle ty = TypeHandle();
    if (eType == ELEMENT_TYPE_BYREF)
    {
        fIsByRef = TRUE;
        TypeHandle tycopy;
        eType = pSig->GetByRefType(&tycopy);
        if (eType == ELEMENT_TYPE_VALUETYPE)
            ty = tycopy;
    }
    else
    {
        if (eType == ELEMENT_TYPE_VALUETYPE)
        {
            ty = pSig->GetLastTypeHandleThrowing();

#ifdef ENREGISTERED_PARAMTYPE_MAXSIZE
            if (ArgIterator::IsArgPassedByRef(ty))
                fIsByRef = TRUE;
#endif
        }
    }

    if (eType == ELEMENT_TYPE_PTR)
        COMPlusThrow(kRemotingException, W("Remoting_CantRemotePointerType"));

    GetObjectFromStack(&gc.refRetVal,
        GetStackPtr(argNum, gc.pMessage->GetFrame(), gc.pMessage->GetResetMetaSig()),
        eType,
        ty,        
        fIsByRef,
        gc.pMessage->GetFrame());

    LOG((LF_REMOTING, LL_INFO10, "CMessage::GetArg OUT\n"));

    HELPER_METHOD_FRAME_END();
    return OBJECTREFToObject(gc.refRetVal);
}
FCIMPLEND

FCIMPL1(Object*, CMessage::GetArgs, MessageObject* pMessageUNSAFE)
{
    CONTRACTL
    {
        FCALL_CHECK;
        PRECONDITION(pMessageUNSAFE != NULL);
    }
    CONTRACTL_END;

    struct _gc {
        PTRARRAYREF refRetVal;
        MESSAGEREF pMessage;
        OBJECTREF arg;
    } gc;

    gc.refRetVal = NULL;
    gc.pMessage = (MESSAGEREF) pMessageUNSAFE;
    gc.arg = NULL;

    HELPER_METHOD_FRAME_BEGIN_RET_PROTECT(gc);

    LOG((LF_REMOTING, LL_INFO10, "CMessage::GetArgCount IN\n"));

    MetaSig *pSig = gc.pMessage->GetResetMetaSig();

    // scan the sig for the argument count
    INT32 numArgs = pSig->NumFixedArgs();
    if (gc.pMessage->GetDelegateMD())
        numArgs -= 2;

    // Allocate an object array
    gc.refRetVal = (PTRARRAYREF) AllocateObjectArray(numArgs, g_pObjectClass);

    ArgIterator iter(pSig);

    for (int index = 0; index < numArgs; index++)
    {
        BOOL fIsByRef = FALSE;
        CorElementType eType;
        PVOID addr;
        eType = pSig->PeekArg();
        addr = (LPBYTE) gc.pMessage->GetFrame()->GetTransitionBlock() + GetStackOffset(gc.pMessage->GetFrame(), &iter, pSig);

        TypeHandle ty = TypeHandle();
        if (eType == ELEMENT_TYPE_BYREF)
        {
            fIsByRef = TRUE;
            TypeHandle tycopy;
            // If this is a by-ref arg, GetObjectFromStack() will dereference "addr" to
            // get the real argument address. Dereferencing now will open a gc hole if "addr"
            // points into the gc heap, and we trigger gc between here and the point where
            // we return the arguments.
            //addr = *((PVOID *) addr);
            eType = pSig->GetByRefType(&tycopy);
            if (eType == ELEMENT_TYPE_VALUETYPE)
                ty = tycopy;
        }
        else
        {
            if (eType == ELEMENT_TYPE_VALUETYPE)
            {
                ty = pSig->GetLastTypeHandleThrowing();

#ifdef ENREGISTERED_PARAMTYPE_MAXSIZE
                if (ArgIterator::IsArgPassedByRef(ty))
                    fIsByRef = TRUE;
#endif
            }
        }

        if (eType == ELEMENT_TYPE_PTR)
            COMPlusThrow(kRemotingException, W("Remoting_CantRemotePointerType"));

        GetObjectFromStack(&gc.arg,
            addr,
            eType,
            ty,            
            fIsByRef,
            gc.pMessage->GetFrame());

        gc.refRetVal->SetAt(index, gc.arg);
    }

    LOG((LF_REMOTING, LL_INFO10, "CMessage::GetArgs OUT\n"));

    HELPER_METHOD_FRAME_END();
    return OBJECTREFToObject(gc.refRetVal);
}
FCIMPLEND

//static
void CMessage::GetObjectFromStack(OBJECTREF* ppDest, PVOID val, const CorElementType eType, TypeHandle ty, BOOL fIsByRef, FramedMethodFrame *pFrame)
{
    CONTRACT_VOID
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        INJECT_FAULT(COMPlusThrowOM());
        PRECONDITION(CheckPointer(ppDest));
        PRECONDITION(CheckPointer(val));
    }
    CONTRACT_END;

        // Value types like Nullable<T> have special unboxing semantics, 
        // 
    if (eType == ELEMENT_TYPE_VALUETYPE)
    {
        //
        // box the value class
        //

        _ASSERTE(ty.GetMethodTable()->IsValueType() || ty.GetMethodTable()->IsEnum());

        _ASSERTE(!GCHeap::GetGCHeap()->IsHeapPointer((BYTE *) ppDest) ||
             !"(pDest) can not point to GC Heap");
        MethodTable* pMT = ty.GetMethodTable();

        if (pMT->IsByRefLike())
            COMPlusThrow(kRemotingException, W("Remoting_TypeCantBeRemoted"));

        PVOID* pVal;
        if (fIsByRef)
            pVal = (PVOID *)val;
        else {
            val = StackElemEndianessFixup(val, pMT->GetNumInstanceFieldBytes());
            pVal = &val;
        }

        *ppDest = pMT->FastBox(pVal);
        RETURN;
     }

    switch (CorTypeInfo::GetGCType(eType))
    {
        case TYPE_GC_NONE:
        {
            if(ELEMENT_TYPE_PTR == eType)
            {
                COMPlusThrow(kNotSupportedException);
            }

            MethodTable *pMT = MscorlibBinder::GetElementType(eType);

            OBJECTREF pObj = pMT->Allocate();
            if (fIsByRef)
                val = *((PVOID *)val);
            else
                val = StackElemEndianessFixup(val, CorTypeInfo::Size(eType));

            void *pDest = pObj->UnBox();

#ifdef COM_STUBS_SEPARATE_FP_LOCATIONS
            if (   !fIsByRef
                && (   ELEMENT_TYPE_R4 == eType
                    || ELEMENT_TYPE_R8 == eType)
                && pFrame
                && !TransitionBlock::IsStackArgumentOffset(static_cast<int>((TADDR) val - pFrame->GetTransitionBlock())))
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

            *ppDest = pObj;
        }
        break;
        case TYPE_GC_REF:
            if (fIsByRef)
                val = *((PVOID *)val);
            *ppDest = ObjectToOBJECTREF(*(Object **)val);
            break;
        default:
            COMPlusThrow(kRemotingException, W("Remoting_TypeCantBeRemoted"));
    }

    RETURN;
}

//+----------------------------------------------------------------------------
//
//  Method:     CMessage::PropagateOutParameters private
//
//  Synopsis:   Copy back data for in/out parameters and the return value
// 
//+----------------------------------------------------------------------------
FCIMPL3(void, CMessage::PropagateOutParameters, MessageObject* pMessageUNSAFE, ArrayBase* pOutPrmsUNSAFE, Object* RetValUNSAFE)
{
    CONTRACTL
    {
        FCALL_CHECK;
        PRECONDITION(pMessageUNSAFE != NULL);
    }
    CONTRACTL_END;

    struct _gc
    {
        MESSAGEREF pMessage;
        BASEARRAYREF pOutPrms;
        OBJECTREF RetVal;
        OBJECTREF param;
    } gc;
    gc.pMessage = (MESSAGEREF) pMessageUNSAFE;
    gc.pOutPrms = (BASEARRAYREF) pOutPrmsUNSAFE;
    gc.RetVal = (OBJECTREF) RetValUNSAFE;
    gc.param = NULL;
    HELPER_METHOD_FRAME_BEGIN_PROTECT(gc);

    LOG((LF_REMOTING, LL_INFO10, "CMessage::PropogateOutParameters IN\n"));

    // Retrieve the message's flags.
    INT32 flags = gc.pMessage->GetFlags();

    // Construct an ArgIterator from the message's frame and sig.
    MetaSig *pSig = gc.pMessage->GetResetMetaSig();
    FramedMethodFrame *pFrame = gc.pMessage->GetFrame();
    ArgIterator argit(pSig);

    // move into object to return to client

    // Propagate the return value only if the pMsg is not a Ctor message
    // Check if the return type has a return buffer associated with it
    if ((flags& MSGFLG_CTOR) == 0  && pSig->GetReturnType() != ELEMENT_TYPE_VOID)
    {
        if (argit.HasRetBuffArg())
        {
            // Copy from RetVal into the retBuff.
            INT64 retVal =  CopyOBJECTREFToStack(
                                *(void**)(pFrame->GetTransitionBlock() + argit.GetRetBuffArgOffset()),
                                &gc.RetVal,
                                pSig->GetReturnType(),
                                TypeHandle(),
                                pSig,
                                TRUE);  // copy class contents

            // Copy the return value
            *(ARG_SLOT *)(gc.pMessage->GetFrame()->GetReturnValuePtr()) = retVal;
        }
        else
        {
#ifdef ENREGISTERED_RETURNTYPE_MAXSIZE
            if (argit.HasNonStandardByvalReturn())
            {
                //
                // in these cases, we put the pointer to the return buffer into the frame's
                // return value slot
                //
                CopyOBJECTREFToStack(gc.pMessage->GetFrame()->GetReturnValuePtr(),
                                     &gc.RetVal,
                                     pSig->GetReturnType(),
                                     TypeHandle(),
                                     pSig,
                                     TRUE);                 // copy class contents
            }
            else
#endif // ENREGISTERED_RETURNTYPE_MAXSIZE
            {
                // There is no separate return buffer, the retVal should fit in
                // an INT64.
                INT64 retVal = CopyOBJECTREFToStack(
                                    NULL,                   //no return buff
                                    &gc.RetVal,
                                    pSig->GetReturnType(),
                                    TypeHandle(),
                                    pSig,
                                    FALSE);                 //don't copy class contents

                // Copy the return value
                *(ARG_SLOT *)(gc.pMessage->GetFrame()->GetReturnValuePtr()) = retVal;
            }
        }
    }

    // Refetch all the variables as GC could have happened after call to
    // CopyOBJECTREFToStack
    UINT32  cOutParams = (gc.pOutPrms != NULL) ? gc.pOutPrms->GetNumComponents() : 0;
    if (cOutParams > 0)
    {
        PVOID *argAddr;
        UINT32 i = 0;
        MetaSig syncSig(gc.pMessage->GetMethodDesc());
        MetaSig *pSyncSig = NULL;

        if (flags & MSGFLG_ENDINVOKE)
        {
            pSyncSig = &syncSig;
        }

        for (i=0; i<cOutParams; i++)
        {
            if (pSyncSig)
            {
                CorElementType typ = pSyncSig->NextArg();
                if (typ == ELEMENT_TYPE_END)
                {
                    break;
                }

                if (typ != ELEMENT_TYPE_BYREF)
                {
                    continue;
                }

                argAddr = (PVOID *)(pFrame->GetTransitionBlock() + argit.GetNextOffset());
            }
            else
            {
                int ofs = argit.GetNextOffset();
                if (ofs == TransitionBlock::InvalidOffset)
                {
                    break;
                }

                if (argit.GetArgType() != ELEMENT_TYPE_BYREF)
                {
                    continue;
                }

                argAddr = (PVOID *)(pFrame->GetTransitionBlock() + ofs);
            }

            TypeHandle ty = TypeHandle();
            CorElementType brType = pSig->GetByRefType(&ty);

            gc.param = ((OBJECTREF *) gc.pOutPrms->GetDataPtr())[i];

            CopyOBJECTREFToStack(
                *argAddr,
                &gc.param,
                brType,
                ty,
                pSig,
                ty.IsNull() ? FALSE : ty.IsValueType());
        }

    }

    HELPER_METHOD_FRAME_END();
}
FCIMPLEND

INT64 CMessage::CopyOBJECTREFToStack(PVOID pvDest, OBJECTREF *pSrc, CorElementType typ, TypeHandle ty, MetaSig *pSig, BOOL fCopyClassContents)
{
    INT64 ret = 0;

    CONTRACT(INT64)
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        INJECT_FAULT(COMPlusThrowOM());
        PRECONDITION(CheckPointer(pvDest, NULL_OK));
        PRECONDITION(CheckPointer(pSrc));
        PRECONDITION(typ != ELEMENT_TYPE_VOID);
        PRECONDITION(CheckPointer(pSig));
    }
    CONTRACT_END;

    if (fCopyClassContents)
    {
        // We have to copy the contents of a value class to pvDest

        // write unboxed version back to memory provided by the client
        if (pvDest)
        {
            if (ty.IsNull())
                ty = pSig->GetRetTypeHandleThrowing();

            if (*pSrc == NULL && !Nullable::IsNullableType(ty))
                COMPlusThrow(kRemotingException, W("Remoting_Message_BadRetValOrOutArg"));

            ty.GetMethodTable()->UnBoxIntoUnchecked(pvDest, *pSrc);            

            // return the object so it can be stored in the frame and
            // propagated to the root set
            // pSrc may not be doubleword aligned!
            *(OBJECTREF*)&ret  = *pSrc;
        }
    }
    else
    {
        // We have either a real OBJECTREF or something that does not have
        // a return buffer associated

        // Check if it is an ObjectRef (from the GC heap)
        if (CorTypeInfo::IsObjRef(typ))
        {
            if ((*pSrc!=NULL) && ((*pSrc)->IsTransparentProxy()))
            {
                if (ty.IsNull())
                    ty = pSig->GetRetTypeHandleThrowing();

                // CheckCast ensures that the returned object (proxy) gets
                // refined to the level expected by the caller of the method
                if (!CRemotingServices::CheckCast(*pSrc, ty))
                    COMPlusThrow(kInvalidCastException, W("Arg_ObjObj"));
            }
            if (pvDest)
                SetObjectReferenceUnchecked((OBJECTREF *)pvDest, *pSrc);

            // pSrc may not be double-word aligned!
            *(OBJECTREF*)&ret  = *pSrc;
        }
        else
        {
            // Note: this assert includes VALUETYPE because for Enums
            // HasRetBuffArg() returns false since the normalized type is I4
            // so we end up here ... but GetReturnType() returns VALUETYPE
            // Almost all VALUETYPEs will go through the fCopyClassContents
            // codepath instead of here.
            // Also, IsPrimitiveType() does not check for IntPtr, UIntPtr etc
            // there is a note in siginfo.hpp about that ... hence we have
            // ELEMENT_TYPE_I, ELEMENT_TYPE_U.
            _ASSERTE(
                CorTypeInfo::IsPrimitiveType(typ)
                || (typ == ELEMENT_TYPE_VALUETYPE)
                || (typ == ELEMENT_TYPE_I)
                || (typ == ELEMENT_TYPE_U)
                || (typ == ELEMENT_TYPE_FNPTR)
                );

            // REVIEW: For a "ref int" arg, if a nasty sink replaces the boxed
            // int with a null OBJECTREF, this is where we check. We need to be
            // uniform in our policy w.r.t. this (throw v/s ignore)
            // The 'if' block above throws, CallFieldAccessor also has this
            // problem.
            if (*pSrc != NULL)
            {
                PVOID pvSrcData = (*pSrc)->GetData();
                int cbsize = gElementTypeInfo[typ].m_cbSize;
                INT64 retBuff;

                // ElementTypeInfo.m_cbSize can be less that zero for cases that need 
                // special handling (e.g. value types) to be sure of size (see 
                // siginfo.cpp).  Luckily, the type handle has the actual byte count,
                // so we look there for such cases.
                if (cbsize < 0)
                {
                    if (ty.IsNull())
                        ty = pSig->GetRetTypeHandleThrowing();

                    _ASSERTE(!ty.IsNull());
                    cbsize = ty.GetSize();
                    
                        // we are returning this value class in an INT64 so it better be small enough
                    _ASSERTE(cbsize <= (int) sizeof(INT64));
                        // Unbox it into a local buffer, This coveres the Nullable<T> case
                        // then do the endianness morph below
                     ty.GetMethodTable()->UnBoxIntoUnchecked(&retBuff, *pSrc);

                    pvSrcData = &retBuff;
                }

                if (pvDest)
                {
                    memcpyNoGCRefs(pvDest, pvSrcData, cbsize);
                }

                // need to sign-extend signed types
                bool fEndianessFixup = false;
                switch (typ) {                    
                case ELEMENT_TYPE_I1:
                    ret = *(INT8*)pvSrcData;
                    fEndianessFixup = true;
                    break;
                case ELEMENT_TYPE_I2:
                    ret = *(INT16*)pvSrcData;
                    fEndianessFixup = true;
                    break;
                case ELEMENT_TYPE_I4:
                    ret = *(INT32*)pvSrcData;
                    fEndianessFixup = true;
                    break;
                default:
                    memcpyNoGCRefs(StackElemEndianessFixup(&ret, cbsize), pvSrcData, cbsize);
                    break;
                }

#if !defined(_WIN64) && BIGENDIAN
                if (fEndianessFixup)
                    ret <<= 32;
#endif
            }
        }
    }

    RETURN(ret);
}

//+----------------------------------------------------------------------------
//
//  Method:     CMessage::GetReturnValue
//
//  Synopsis:   Pull return value off the stack
// 
//+----------------------------------------------------------------------------
FCIMPL1(Object*, CMessage::GetReturnValue, MessageObject* pMessageUNSAFE)
{
    CONTRACTL
    {
        FCALL_CHECK;
        PRECONDITION(pMessageUNSAFE != NULL);
    }
    CONTRACTL_END;

    struct _gc {
        OBJECTREF refRetVal;
        MESSAGEREF pMessage;
    } gc;

    gc.refRetVal = NULL;
    gc.pMessage = (MESSAGEREF) pMessageUNSAFE;

    HELPER_METHOD_FRAME_BEGIN_RET_PROTECT(gc);

    MetaSig* pSig = gc.pMessage->GetResetMetaSig();
    FramedMethodFrame* pFrame = gc.pMessage->GetFrame();

    ArgIterator argit(pSig);

    PVOID pvRet;
    if (argit.HasRetBuffArg())
    {
        pvRet = *(PVOID *)(pFrame->GetTransitionBlock() + argit.GetRetBuffArgOffset());
    }
    else
    {
        pvRet = pFrame->GetReturnValuePtr();
    }

    CorElementType eType = pSig->GetReturnType();
    TypeHandle ty;
    if (eType == ELEMENT_TYPE_VALUETYPE)
    {
        ty = pSig->GetRetTypeHandleThrowing();
    }
    else
    {
        ty = TypeHandle();
    }

    GetObjectFromStack(&gc.refRetVal,
        pvRet,
        eType,
        ty);

    HELPER_METHOD_FRAME_END();
    return OBJECTREFToObject(gc.refRetVal);
}
FCIMPLEND



FCIMPL2(FC_BOOL_RET, CMessage::Dispatch, MessageObject* pMessageUNSAFE, Object* pServerUNSAFE)
{
    CONTRACTL
    {
        FCALL_CHECK;
        PRECONDITION(pMessageUNSAFE != NULL);
        PRECONDITION(pServerUNSAFE != NULL);
    }
    CONTRACTL_END;

    BOOL fDispatched = FALSE;
    MESSAGEREF pMessage = (MESSAGEREF) pMessageUNSAFE;
    OBJECTREF pServer = (OBJECTREF) pServerUNSAFE;
    HELPER_METHOD_FRAME_BEGIN_RET_2(pMessage, pServer);

    MetaSig *pSig = pMessage->GetResetMetaSig();

    if (pMessage->GetFlags() & (MSGFLG_BEGININVOKE | MSGFLG_ENDINVOKE | MSGFLG_ONEWAY))
    {
        fDispatched = FALSE;
        goto lExit;
    }

    {
    ArgIterator argit(pSig);

    UINT nStackBytes;
    MethodDesc *pMD;
    PCODE pTarget;

    nStackBytes = argit.SizeOfFrameArgumentArray();
    pMD = pMessage->GetMethodDesc();

    // Get the address of the code
    pTarget = pMD->GetCallTarget(&(pServer));

#ifdef PROFILING_SUPPORTED
    // If we're profiling, notify the profiler that we're about to invoke the remoting target
    {
        BEGIN_PIN_PROFILER(CORProfilerTrackRemoting());
        GCX_PREEMP();
        g_profControlBlock.pProfInterface->RemotingServerInvocationStarted();
        END_PIN_PROFILER();
    }        
#endif // PROFILING_SUPPORTED

#ifdef CALLDESCR_FPARGREGS
    // @TODO: This code badly needs refactorization. It's largely shared with MethodDesc::CallDescr in
    // method.cpp and CallDescrWithObjectArray in stackbuildersync.cpp.

    FloatArgumentRegisters *pFloatArgumentRegisters = NULL;

    // Iterate through all the args looking for floating point values that will be passed in (FP) argument
    // registers.
    int ofs;
    while ((ofs = argit.GetNextOffset()) != TransitionBlock::InvalidOffset)
    {
        if (TransitionBlock::HasFloatRegister(ofs, argit.GetArgLocDescForStructInRegs()))
        {
            // Found a floating point argument register. The first time we find this we point
            // pFloatArgumentRegisters to the part of the frame where these values were spilled (we don't do
            // this unconditionally since the call worker can optimize out the copy of the floating point
            // registers if none are involved at all.
            pFloatArgumentRegisters = (FloatArgumentRegisters*)(pMessage->GetFrame()->GetTransitionBlock() +
                                                                TransitionBlock::GetOffsetOfFloatArgumentRegisters());

            // That's all we need to do, CallDescrWorkerWithHandler will automatically pick up all the
            // floating point argument values now.
            break;
        }
    }
#endif // CALLDESCR_FPARGREGS

#if defined(CALLDESCR_REGTYPEMAP) || defined(COM_STUBS_SEPARATE_FP_LOCATIONS)
    DWORD_PTR   dwRegTypeMap    = 0;

    {
        int ofs;
        while ((ofs = argit.GetNextOffset()) != TransitionBlock::InvalidOffset)
        {
            int regArgNum = TransitionBlock::GetArgumentIndexFromOffset(ofs);

            if (regArgNum >= NUM_ARGUMENT_REGISTERS)
                break;

            CorElementType argTyp = argit.GetArgType();

#ifdef CALLDESCR_REGTYPEMAP
            FillInRegTypeMap(ofs, argTyp, (BYTE*)&dwRegTypeMap);
#endif

#ifdef COM_STUBS_SEPARATE_FP_LOCATIONS
            // If this is a floating point argument, it was stored in a
            // separate area of the frame.  Copy it into the argument array.
            if (ELEMENT_TYPE_R4 == argTyp || ELEMENT_TYPE_R8 == argTyp)
            {
                TADDR pTransitionBlock = pMessage->GetFrame()->GetTransitionBlock();

                PVOID pDest = (PVOID)(pTransitionBlock + ofs);
                PVOID pSrc = (PVOID)(pTransitionBlock + pMessage->GetFrame()->GetFPArgOffset(regArgNum));

                ARG_SLOT val;
                if (ELEMENT_TYPE_R4 == argTyp)
                    val = FPSpillToR4(pSrc);
                else
                    val = FPSpillToR8(pSrc);

                *(ARG_SLOT*)pDest = val;
            }
#endif
        }
    }
#endif // CALLDESCR_REGTYPEMAP || COM_STUBS_SEPARATE_FP_LOCATIONS

    CallDescrData callDescrData;

    callDescrData.pSrc = (BYTE*)pMessage->GetFrame()->GetTransitionBlock() + sizeof(TransitionBlock);
    callDescrData.numStackSlots = nStackBytes / STACK_ELEM_SIZE;
#ifdef CALLDESCR_ARGREGS
    callDescrData.pArgumentRegisters = (ArgumentRegisters*)(pMessage->GetFrame()->GetTransitionBlock() + TransitionBlock::GetOffsetOfArgumentRegisters());
#endif
#ifdef CALLDESCR_FPARGREGS
    callDescrData.pFloatArgumentRegisters = pFloatArgumentRegisters;
#endif
#ifdef CALLDESCR_REGTYPEMAP
    callDescrData.dwRegTypeMap = dwRegTypeMap;
#endif
    callDescrData.fpReturnSize = argit.GetFPReturnSize();
    callDescrData.pTarget = pTarget;

    CallDescrWorkerWithHandler(&callDescrData);

#ifdef PROFILING_SUPPORTED
    // If we're profiling, notify the profiler that we're about to invoke the remoting target
    {
        BEGIN_PIN_PROFILER(CORProfilerTrackRemoting());
        GCX_PREEMP();
        g_profControlBlock.pProfInterface->RemotingServerInvocationReturned();
        END_PIN_PROFILER();
    }
#endif // PROFILING_SUPPORTED

    memcpyNoGCRefs(pMessage->GetFrame()->GetReturnValuePtr(), &callDescrData.returnValue, sizeof(callDescrData.returnValue));

    fDispatched = TRUE;
    }

lExit: ;
    HELPER_METHOD_FRAME_END();
    FC_RETURN_BOOL(fDispatched);
}
FCIMPLEND

void CMessage::AppendAssemblyName(CQuickBytes &out, const CHAR* str)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
        INJECT_FAULT(COMPlusThrowOM());
        PRECONDITION(CheckPointer(str));
    }
    CONTRACTL_END;

    SIZE_T len = strlen(str) * sizeof(CHAR);
    SIZE_T oldSize = out.Size();
    out.ReSizeThrows(oldSize + len + 2);
    CHAR * cur = (CHAR *) ((BYTE *) out.Ptr() + oldSize - 1);
    if (*cur)
        cur++;

    *cur = ASSEMBLY_SEPARATOR_CHAR;
    memcpy(cur + 1, str, len);
    cur += (len + 1);
    *cur = '\0';
}

//+----------------------------------------------------------------------------
//
//  Method:     CMessage::GetAsyncBeginInfo
//
//  Synopsis:   Pull the AsyncBeginInfo object from an async call
// 
//+----------------------------------------------------------------------------
FCIMPL3(void, CMessage::GetAsyncBeginInfo, MessageObject* pMessageUNSAFE, OBJECTREF* ppACBD, OBJECTREF* ppState)
{
    CONTRACTL
    {
        FCALL_CHECK;
        PRECONDITION(pMessageUNSAFE != NULL);
    }
    CONTRACTL_END;

    MESSAGEREF pMessage = (MESSAGEREF) pMessageUNSAFE;
    _ASSERTE(pMessage->GetFlags() & MSGFLG_BEGININVOKE);

    HELPER_METHOD_FRAME_BEGIN_1(pMessage);

    LOG((LF_REMOTING, LL_INFO10, "CMessage::GetAsyncBeginInfo IN\n"));

    if (pMessage == NULL)
        COMPlusThrow(kNullReferenceException, W("NullReference_This"));

    MetaSig *pSig = pMessage->GetResetMetaSig();

    FramedMethodFrame * pFrame = pMessage->GetFrame();
    ArgIterator argit(pSig);

    if ((ppACBD != NULL) || (ppState != NULL))
    {
        int ofs;
        int last = TransitionBlock::InvalidOffset, secondtolast = TransitionBlock::InvalidOffset;
        while ((ofs = argit.GetNextOffset()) != TransitionBlock::InvalidOffset)
        {
            secondtolast = last;
            last = ofs;
        }
        _ASSERTE(secondtolast != TransitionBlock::InvalidOffset);
        if (secondtolast != TransitionBlock::InvalidOffset && ppACBD != NULL)
            SetObjectReferenceUnchecked(ppACBD, ObjectToOBJECTREF(*(Object **)(pFrame->GetTransitionBlock() + secondtolast)));
        PREFIX_ASSUME(last != TransitionBlock::InvalidOffset);
        if (ppState != NULL)
            SetObjectReferenceUnchecked(ppState, ObjectToOBJECTREF(*(Object **)(pFrame->GetTransitionBlock() + last)));
    }

    HELPER_METHOD_FRAME_END();
}
FCIMPLEND

//+----------------------------------------------------------------------------
//
//  Method:     CMessage::GetAsyncResult
//
//  Synopsis:   Pull the AsyncResult from an async call
// 
//+----------------------------------------------------------------------------
FCIMPL1(LPVOID, CMessage::GetAsyncResult, MessageObject* pMessageUNSAFE)
{
    CONTRACTL
    {
        FCALL_CHECK;
        PRECONDITION(pMessageUNSAFE != NULL);
    }
    CONTRACTL_END;

    LPVOID retVal = NULL;
    MESSAGEREF pMessage = (MESSAGEREF) pMessageUNSAFE;
    _ASSERTE(pMessage->GetFlags() & MSGFLG_ENDINVOKE);

    HELPER_METHOD_FRAME_BEGIN_RET_1(pMessage);

    LOG((LF_REMOTING, LL_INFO10, "CMessage::GetAsyncResult IN\n"));

    retVal = GetLastArgument(&pMessage);

    HELPER_METHOD_FRAME_END();
    return retVal;
}
FCIMPLEND

//+----------------------------------------------------------------------------
//
//  Method:     CMessage::GetAsyncObject
//
//  Synopsis:   Pull the AsyncObject from an async call
// 
//+----------------------------------------------------------------------------
FCIMPL1(Object*, CMessage::GetAsyncObject, MessageObject* pMessageUNSAFE)
{
    CONTRACTL
    {
        FCALL_CHECK;
        PRECONDITION(pMessageUNSAFE != NULL);
    }
    CONTRACTL_END;

    Object* pobjRetVal = NULL;
    MESSAGEREF pMessage = (MESSAGEREF) pMessageUNSAFE;
    HELPER_METHOD_FRAME_BEGIN_RET_1(pMessage);

    LOG((LF_REMOTING, LL_INFO10, "CMessage::GetAsyncObject IN\n"));

    FramedMethodFrame *pFrame = pMessage->GetFrame();
    MetaSig *pSig = pMessage->GetResetMetaSig();
    ArgIterator argit(pSig);

    pobjRetVal = *(Object**)(pFrame->GetTransitionBlock() + argit.GetThisOffset());

    HELPER_METHOD_FRAME_END();
    return pobjRetVal;
}
FCIMPLEND

//+----------------------------------------------------------------------------
//
//  Method:     CMessage::GetLastArgument private
//
//  Synopsis:   Pull the last argument of 4 bytes off the stack
// 
//+----------------------------------------------------------------------------
LPVOID CMessage::GetLastArgument(MESSAGEREF *pMsg)
{
    CONTRACT(LPVOID)
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(CheckPointer(pMsg));
        POSTCONDITION(*pMsg != NULL); // CheckPointer doesn't seem to work here.
        POSTCONDITION(CheckPointer(RETVAL, NULL_OK));
    }
    CONTRACT_END;

    FramedMethodFrame *pFrame = (*pMsg)->GetFrame();
    MetaSig *pSig = (*pMsg)->GetResetMetaSig();

    ArgIterator argit(pSig);
    int arg;
    int backadder = TransitionBlock::InvalidOffset;
    while ((arg = argit.GetNextOffset()) != TransitionBlock::InvalidOffset)
        backadder = arg;

    _ASSERTE(backadder != TransitionBlock::InvalidOffset);

    RETURN *(LPVOID *)(pFrame->GetTransitionBlock() + backadder);
}

//+----------------------------------------------------------------------------
//
//  Method:     CMessage::DebugOut public
//
//  Synopsis:   temp Debug out until the classlibs have one.
// 
//+----------------------------------------------------------------------------
FCIMPL1(void, CMessage::DebugOut, StringObject* pOutUNSAFE)
{
    CONTRACTL
    {
        FCALL_CHECK;
        PRECONDITION(pOutUNSAFE != NULL);
    }
    CONTRACTL_END;

#ifdef _DEBUG
    STRINGREF pOut = (STRINGREF) pOutUNSAFE;
    HELPER_METHOD_FRAME_BEGIN_1(pOut);

    static int fMessageDebugOut = 0;

    if (fMessageDebugOut == 0)
        fMessageDebugOut = CLRConfig::GetConfigValue(CLRConfig::INTERNAL_MessageDebugOut) ? 1 : -1;

    if (fMessageDebugOut == 1)
        WszOutputDebugString(pOut->GetBuffer());

    HELPER_METHOD_FRAME_END();
#endif

    FCUnique(0x76);
}
FCIMPLEND

//+----------------------------------------------------------------------------
//
//  Method:     CMessage::HasVarArgs public
//
//  Synopsis:   Return TRUE if the method is a VarArgs Method
// 
//+----------------------------------------------------------------------------
FCIMPL1(FC_BOOL_RET, CMessage::HasVarArgs, MessageObject * pMessage)
{
    CONTRACTL
    {
        FCALL_CHECK;
        PRECONDITION(CheckPointer(pMessage));
    }
    CONTRACTL_END;

    BOOL result;

    // Need entire path to be SO_TOLERANT or put a Hard SO probe here as
    // no failure path.
    CONTRACT_VIOLATION(SOToleranceViolation);

    result = pMessage->GetMethodDesc()->IsVarArg();

    FC_RETURN_BOOL(result);
}
FCIMPLEND


//static
int CMessage::GetStackOffset (FramedMethodFrame *pFrame, ArgIterator *pArgIter, MetaSig *pSig)
{
    CONTRACT(int)
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        INJECT_FAULT(COMPlusThrowOM());
        PRECONDITION(CheckPointer(pFrame));
        PRECONDITION(CheckPointer(pArgIter));
        PRECONDITION(CheckPointer(pSig));
    }
    CONTRACT_END;

    LOG((LF_REMOTING, LL_INFO100,
         "CMessage::GetStackOffset pFrame:0x%x, pArgIter:0x%x\n",
         pFrame, pArgIter));

    int ret = pArgIter->GetNextOffset();

#ifdef COM_STUBS_SEPARATE_FP_LOCATIONS
    int typ = pArgIter->GetArgType();
    // REVISIT_TODO do we need to handle this?
    if ((ELEMENT_TYPE_R4 == typ || ELEMENT_TYPE_R8 == typ) &&
        TransitionBlock::IsArgumentRegisterOffset(ret))
    {
        int iFPArg = TransitionBlock::GetArgumentIndexFromOffset(ret);

        ret = static_cast<int>(pFrame->GetFPArgOffset(iFPArg));
    }
#endif // COM_STUBS_SEPARATE_FP_LOCATIONS

    RETURN ret;
}


//+----------------------------------------------------------------------------
//
//  Method:     CMessage::GetStackPtr private
//
//  Synopsis:   Figure out where on the stack a parameter is stored
//
//  Parameters: ndx     - the parameter index (zero-based)
//              pFrame  - stack frame pointer (FramedMethodFrame)
//              pSig    - method signature, used to determine parameter sizes
// 
//
//<REVISIT_TODO>
//  CODEWORK:   Currently we assume all parameters to be 32-bit intrinsics
//              or 32-bit pointers.  Value classes are not handled correctly.
//</REVISIT_TODO>
//+----------------------------------------------------------------------------
PVOID CMessage::GetStackPtr(INT32 ndx, FramedMethodFrame *pFrame, MetaSig *pSig)
{
    CONTRACT(PVOID)
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        INJECT_FAULT(COMPlusThrowOM());
        PRECONDITION(CheckPointer(pFrame));
        PRECONDITION(CheckPointer(pSig));
        POSTCONDITION(CheckPointer(RETVAL, NULL_OK));
    }
    CONTRACT_END;

    LOG((LF_REMOTING, LL_INFO100,
         "CMessage::GetStackPtr IN ndx:0x%x, pFrame:0x%x, pSig:0x%x\n",
         ndx, pFrame, pSig));

    ArgIterator iter(pSig);
    PVOID ret = NULL;

    // <REVISIT_TODO>CODEWORK:: detect and optimize for sequential access</REVISIT_TODO>
    _ASSERTE((UINT)ndx < pSig->NumFixedArgs());
    for (int i=0; i<=ndx; i++)
        ret = (BYTE*)pFrame->GetTransitionBlock() + GetStackOffset(pFrame, &iter, pSig);

    RETURN ret;
}

#endif //FEATURE_REMOTING
