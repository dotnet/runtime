// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// 
// File: StackBuilderSink.cpp
// 

//
// Purpose: Native implementation for System.Runtime.Remoting.Messaging.StackBuilderSink
//


#include "common.h"

#ifdef FEATURE_REMOTING

#include "excep.h"
#include "message.h"
#include "stackbuildersink.h"
#include "dbginterface.h"
#include "remoting.h"
#include "profilepriv.h"
#include "class.h"

struct ArgInfo
{
    PBYTE             dataLocation;
    INT32             dataSize;
    TypeHandle        dataTypeHandle;
    BYTE              dataType;
    BYTE              byref;
};


//+----------------------------------------------------------------------------
//
//  Method:     CStackBuilderSink::PrivateProcessMessage, public
//
//  Synopsis:   Builds the stack and calls an object
// 
//
//+----------------------------------------------------------------------------
FCIMPL5(Object*, CStackBuilderSink::PrivateProcessMessage,
                                        Object* pSBSinkUNSAFE,
                                        MethodDesc* pMD,
                                        PTRArray* pArgsUNSAFE,
                                        Object* pServerUNSAFE,
                                        PTRARRAYREF* ppVarOutParams)
{
    CONTRACTL
    {
        FCALL_CHECK;
        PRECONDITION(CheckPointer(pMD));
        PRECONDITION(!pMD->IsGenericMethodDefinition());
        PRECONDITION(pMD->IsRuntimeMethodHandle());
    }
    CONTRACTL_END;

    struct _gc
    {
        PTRARRAYREF pArgs;
        OBJECTREF pServer;
        OBJECTREF pSBSink;
        OBJECTREF ret;
    } gc;
    gc.pArgs = (PTRARRAYREF) pArgsUNSAFE;
    gc.pServer = (OBJECTREF) pServerUNSAFE;
    gc.pSBSink = (OBJECTREF) pSBSinkUNSAFE;
    gc.ret = NULL;

    HELPER_METHOD_FRAME_BEGIN_RET_PROTECT(gc);

    // pMD->IsStatic() is SO_INTOLERANT.
    // Either pServer is non-null or the method is static (but not both)
    _ASSERTE( (pServerUNSAFE!=NULL) == !(pMD->IsStatic()) );

    LOG((LF_REMOTING, LL_INFO10, "CStackBuilderSink::PrivateProcessMessage\n"));

    MethodDesc *pResolvedMD = pMD;
    // Check if this is an interface invoke, if yes, then we have to find the
    // real method descriptor on the class of the server object.
    if(pMD->GetMethodTable()->IsInterface())
    {
        _ASSERTE(gc.pServer != NULL);

        // NOTE: This method can trigger GC
        // The last parameter (true) causes the method to take into account that
        // the object passed in is a TP and try to resolve the interface MD into
        // a server MD anyway (normally the call short circuits thunking objects
        // and just returns the interface MD unchanged).
        MethodTable *pRealMT = gc.pServer->GetTrueMethodTable();

#ifdef FEATURE_COMINTEROP
        if (pRealMT->IsComObjectType())
            pResolvedMD = pRealMT->GetMethodDescForComInterfaceMethod(pMD, true);
        else
#endif // FEATURE_COMINTEROP
        {
            if (pRealMT->ImplementsInterface(pMD->GetMethodTable()))
            {
                pResolvedMD = pRealMT->GetMethodDescForInterfaceMethod(TypeHandle(pMD->GetMethodTable()), pMD);

                // If the method is generic then we have more work to do --
                // we'll get back the generic method descriptor and we'll have
                // to load the version with the right instantiation (get the
                // instantiation from the interface method).
                if (pResolvedMD->HasMethodInstantiation())
                {
                    _ASSERTE(pResolvedMD->IsGenericMethodDefinition());
                    _ASSERTE(pMD->GetNumGenericMethodArgs() == pResolvedMD->GetNumGenericMethodArgs());

                    pResolvedMD = MethodDesc::FindOrCreateAssociatedMethodDesc(pResolvedMD,
                                                                               pRealMT,
                                                                               FALSE,
                                                                               pMD->GetMethodInstantiation(),
                                                                               FALSE);

                    _ASSERTE(!pResolvedMD->ContainsGenericVariables());
                }
            }
            else
                pResolvedMD = NULL;
        }

        if(!pResolvedMD)
        {
            MAKE_WIDEPTR_FROMUTF8(wName, pMD->GetName());
            COMPlusThrow(kMissingMethodException, IDS_EE_MISSING_METHOD, wName);
        }
    }

    // <TODO>This looks a little dodgy for generics: pResolvedMD has been interface-resolved but not
    // virtual-resolved.  So we seem to be taking the signature of a 
    // half-resolved-virtual-call.  But the MetaSig
    // is only used for GC purposes, and thus is probably OK: although the
    // metadata for the signature of a call may be different
    // as we move to base classes, the instantiated version 
    // of the signature will still be the same
    // at both the callsite and the target). </TODO>
    MetaSig mSig(pResolvedMD);

    // get the target depending on whether the method is virtual or non-virtual
    // like a constructor, private or final method
    PCODE pTarget = pResolvedMD->GetCallTarget(&(gc.pServer));

    VASigCookie *pCookie = NULL;
    _ASSERTE(NULL != pTarget);

        // this function does the work
    ::CallDescrWithObjectArray(
            gc.pServer,
            pResolvedMD,
            //pRM,
            pTarget,
            &mSig,
            pCookie,
            gc.pArgs,
            &gc.ret,
            ppVarOutParams);

    LOG((LF_REMOTING, LL_INFO10, "CStackBuilderSink::PrivateProcessMessage OUT\n"));

    HELPER_METHOD_FRAME_END();
    
    return OBJECTREFToObject(gc.ret);
}
FCIMPLEND

class ProfilerServerCallbackHolder
{
public:
    ProfilerServerCallbackHolder(Thread* pThread) : m_pThread(pThread)
    {
#ifdef PROFILING_SUPPORTED
        // If we're profiling, notify the profiler that we're about to invoke the remoting target
        {
            BEGIN_PIN_PROFILER(CORProfilerTrackRemoting());
            GCX_PREEMP();
            g_profControlBlock.pProfInterface->RemotingServerInvocationStarted();
            END_PIN_PROFILER();
        }
#endif // PROFILING_SUPPORTED
    }

    ~ProfilerServerCallbackHolder()
    {
#ifdef PROFILING_SUPPORTED
        // If profiling is active, tell profiler we've made the call, received the
        // return value, done any processing necessary, and now remoting is done.
        {
            BEGIN_PIN_PROFILER(CORProfilerTrackRemoting());
            GCX_PREEMP();
            g_profControlBlock.pProfInterface->RemotingServerInvocationReturned();
            END_PIN_PROFILER();
        }
#endif // PROFILING_SUPPORTED
    }

private:
    Thread* m_pThread;
};

//+----------------------------------------------------------------------------
//
//  Function:   CallDescrWithObjectArray, private
//
//  Synopsis:   Builds the stack from a object array and call the object
// 
//
// Note this function triggers GC and assumes that pServer, pArguments, pVarRet, and ppVarOutParams are
// all already protected!!
//+----------------------------------------------------------------------------
void CallDescrWithObjectArray(OBJECTREF& pServer,
                  //ReflectMethod *pRM,
                  MethodDesc *pMeth,
                  PCODE pTarget,
                  MetaSig* sig,
                  VASigCookie *pCookie,
                  PTRARRAYREF& pArgArray,
                  OBJECTREF *pVarRet,
                  PTRARRAYREF *ppVarOutParams)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        INJECT_FAULT(COMPlusThrowOM());
    }
    CONTRACTL_END;

    LOG((LF_REMOTING, LL_INFO10, "CallDescrWithObjectArray IN\n"));

#ifdef _PREFAST_
#pragma warning(push)
#pragma warning(disable:6263) // "Suppress PREFast warning about _alloca in a loop"
    // _alloca is called within a loop in a number of places within this method
    // (as an ultra fast means of acquiring temporary storage). This can be a
    // problem in some scenarios (swiftly drive us to stack overflow). But in
    // this case the allocations are tightly bounded (by the number of arguments
    // in the target method) and the allocations themselves are small (no worse
    // really than calling the method an extra time).
#endif

    ByRefInfo *pByRefs = NULL;
    FrameWithCookie<ProtectValueClassFrame> *pProtectValueClassFrame = NULL;
    FrameWithCookie<ProtectByRefsFrame> *pProtectionFrame = NULL;
    UINT  nStackBytes = 0;
    LPBYTE pAlloc = 0;
    LPBYTE pTransitionBlock = 0;
    UINT32 numByRef = 0;
    //DWORD attr = pRM->dwFlags;
#ifdef _DEBUG
    //MethodDesc *pMD = pRM->pMethod;
#endif
    ValueClassInfo *pValueClasses = NULL;
    
    // check the calling convention

    BYTE callingconvention = sig->GetCallingConvention();
    if (!isCallConv(callingconvention, IMAGE_CEE_CS_CALLCONV_DEFAULT))
    {
        _ASSERTE(!"This calling convention is not supported.");
        COMPlusThrow(kInvalidProgramException);
    }

    // Make sure we are properly loaded
    CONSISTENCY_CHECK(GetAppDomain()->CheckCanExecuteManagedCode(pMeth));

    // Note this is redundant with the above but we do it anyway for safety
    pMeth->EnsureActive();

#ifdef DEBUGGING_SUPPORTED
    // debugger goo What does this do? can someone put a comment here?
    if (CORDebuggerTraceCall())
    {
        g_pDebugInterface->TraceCall((const BYTE *)pTarget);
    }
#endif // DEBUGGING_SUPPORTED

    Thread * pThread = GetThread();

    {
        ProfilerServerCallbackHolder profilerHolder(pThread);

    ArgIterator argit(sig);

    // Create a fake FramedMethodFrame on the stack.
    nStackBytes = argit.SizeOfFrameArgumentArray();

    UINT32 cbAlloc = 0;
    if (!ClrSafeInt<UINT32>::addition(TransitionBlock::GetNegSpaceSize(), sizeof(TransitionBlock), cbAlloc))
        COMPlusThrow(kArgumentException);
    if (!ClrSafeInt<UINT32>::addition(cbAlloc, nStackBytes, cbAlloc))
        COMPlusThrow(kArgumentException);

    pAlloc = (LPBYTE)_alloca(cbAlloc);
    pTransitionBlock = pAlloc + TransitionBlock::GetNegSpaceSize();

    // cycle through the parameters and see if there are byrefs
    BOOL   fHasByRefs = FALSE;

    //if (attr & RM_ATTR_BYREF_FLAG_SET)
    //    fHasByRefs = attr & RM_ATTR_HAS_BYREF_ARG;
    //else
    {
        sig->Reset();
        CorElementType typ;
        while ((typ = sig->NextArg()) != ELEMENT_TYPE_END)
        {
            if (typ == ELEMENT_TYPE_BYREF)
            {
                fHasByRefs = TRUE;
                //attr |= RM_ATTR_HAS_BYREF_ARG;
                break;
            }
        }
        //attr |= RM_ATTR_BYREF_FLAG_SET;
        //pRM->dwFlags = attr;
        sig->Reset();
    }

    UINT32 nFixedArgs = sig->NumFixedArgs();
    // To avoid any security problems with integer overflow/underflow we're
    // going to validate the number of args here (we're about to _alloca an
    // array of ArgInfo structures and maybe a managed object array as well
    // based on this count).
    _ASSERTE(sizeof(Object*) <= sizeof(ArgInfo));
    UINT32 nMaxArgs = UINT32_MAX / sizeof(ArgInfo);
    if (nFixedArgs > nMaxArgs)
        COMPlusThrow(kArgumentException);

    // if there are byrefs allocate and array for the out parameters
    if (fHasByRefs)
    {
        *ppVarOutParams = PTRARRAYREF(AllocateObjectArray(sig->NumFixedArgs(), g_pObjectClass));

        // Null out the array
        memset(&(*ppVarOutParams)->m_Array, 0, sizeof(OBJECTREF) * sig->NumFixedArgs());
    }

    OBJECTREF *ppThis = NULL;
    
    if (sig->HasThis())
    {
        ppThis = (OBJECTREF*)(pTransitionBlock + argit.GetThisOffset());

        // *ppThis is not GC protected. It will be set to the right value
        // after all object allocations are made.
        *ppThis = NULL;
    }

    // if we have the Value Class return, we need to allocate that class and place a pointer to it on the stack.

    *pVarRet = NULL;
    TypeHandle retType = sig->GetRetTypeHandleThrowing();
    // Note that we want the unnormalized (signature) type because GetStackObject
    // boxes as the element type, which if we normalized it would loose information.
    CorElementType retElemType = sig->GetReturnType(); 

    // The MethodTable pointer of the return type, if that's a struct
    MethodTable* pStructRetTypeMT = NULL;
    
        // Allocate a boxed struct instance to hold the return value in any case.
    if (retElemType == ELEMENT_TYPE_VALUETYPE) 
    {
        pStructRetTypeMT = retType.GetMethodTable();
        *pVarRet = pStructRetTypeMT->Allocate();
    }
    else  {
        _ASSERTE(!argit.HasRetBuffArg());
    }

#ifdef CALLDESCR_REGTYPEMAP
    UINT64  dwRegTypeMap    = 0;
    BYTE*   pMap            = (BYTE*)&dwRegTypeMap;
#endif // CALLDESCR_REGTYPEMAP

#ifdef CALLDESCR_FPARGREGS
    FloatArgumentRegisters *pFloatArgumentRegisters = NULL;
#endif // CALLDESCR_FPARGREGS

    // gather data about the parameters by iterating over the sig:
    UINT32 arg = 0;
    int    ofs = 0;

    // REVIEW: need to use actual arg count if VarArgs are supported
    ArgInfo* pArgInfoStart = (ArgInfo*) _alloca(nFixedArgs*sizeof(ArgInfo));

#ifdef _DEBUG
    // We expect to write useful data over every part of this so need
    // not do this in retail!
    memset((void *)pArgInfoStart, 0, sizeof(ArgInfo)*nFixedArgs);
#endif

    for (; TransitionBlock::InvalidOffset != (ofs = argit.GetNextOffset()); arg++)
    {
        CONSISTENCY_CHECK(arg < nFixedArgs);
        ArgInfo* pArgInfo = pArgInfoStart + arg;

#ifdef CALLDESCR_REGTYPEMAP
        FillInRegTypeMap(ofs, argit.GetArgType(), pMap);
#endif

#ifdef CALLDESCR_FPARGREGS
        // Under CALLDESCR_FPARGREGS we can have arguments in floating point registers. If we have at
        // least one such argument we point the call worker at the floating point area of the frame (we leave
        // it null otherwise since the worker can perform a useful optimization if it knows no floating point
        // registers need to be set up).
        if (TransitionBlock::HasFloatRegister(ofs, argit.GetArgLocDescForStructInRegs()) && 
            (pFloatArgumentRegisters == NULL))
        {
            pFloatArgumentRegisters = (FloatArgumentRegisters*)(pTransitionBlock +
                                                                TransitionBlock::GetOffsetOfFloatArgumentRegisters());
        }
#endif

        if (argit.GetArgType() == ELEMENT_TYPE_BYREF)
        {
            TypeHandle ty = TypeHandle();
            CorElementType brType = sig->GetByRefType(&ty);
            if (CorIsPrimitiveType(brType))
            {
                pArgInfo->dataSize = gElementTypeInfo[brType].m_cbSize;
            }
            else if (ty.IsValueType())
            {
                pArgInfo->dataSize = ty.GetMethodTable()->GetNumInstanceFieldBytes();
                numByRef ++;
            }
            else
            {
                pArgInfo->dataSize = sizeof(Object *);
                numByRef ++;
            }

            ByRefInfo *brInfo = (ByRefInfo *) _alloca(offsetof(ByRefInfo,data) + pArgInfo->dataSize);
            brInfo->argIndex = arg;
            brInfo->typ = brType;
            brInfo->typeHandle = ty;
            brInfo->pNext = pByRefs;
            pByRefs = brInfo;
            pArgInfo->dataLocation = (BYTE*)brInfo->data;
            *((void**)(pTransitionBlock + ofs)) = (void*)pArgInfo->dataLocation;
            pArgInfo->dataTypeHandle = ty;
            pArgInfo->dataType = static_cast<BYTE>(brType);
            pArgInfo->byref = TRUE;
        }
        else
        {
            pArgInfo->dataLocation = pTransitionBlock + ofs;
            pArgInfo->dataSize = argit.GetArgSize();
            pArgInfo->dataTypeHandle = sig->GetLastTypeHandleThrowing(); // this may cause GC!
            pArgInfo->dataType = (BYTE)argit.GetArgType();
            pArgInfo->byref = FALSE;
        }
    }


    if (ppThis)
    {
        // If this isn't a value class, verify the objectref
#ifdef _DEBUG
        //if (pMD->GetMethodTable()->IsValueType() == FALSE)
        //{
        //    VALIDATEOBJECTREF(pServer);
        //}
#endif //_DEBUG
        *ppThis = pServer;
    }

    PVOID pRetBufStackData = NULL;
    
    if (argit.HasRetBuffArg())
    {
        // If the return buffer *must* be a stack-allocated object, allocate it.
        PVOID pRetBufData = NULL;
        if (pStructRetTypeMT->IsStructRequiringStackAllocRetBuf())
        {
            SIZE_T sz = pStructRetTypeMT->GetNumInstanceFieldBytes();
            pRetBufData = pRetBufStackData = _alloca(sz);
            memset(pRetBufData, 0, sz);
            pValueClasses = new (_alloca(sizeof(ValueClassInfo))) ValueClassInfo(pRetBufStackData, pStructRetTypeMT, pValueClasses);
        }
        else
        {
            pRetBufData = (*pVarRet)->GetData();
        }
        *((LPVOID*) (pTransitionBlock + argit.GetRetBuffArgOffset())) = pRetBufData;
    }

    // There should be no GC when we fill up the stack with parameters, as we don't protect them
    // Assignment of "*ppThis" above triggers the point where we become unprotected.
    {
        GCX_FORBID();

        PBYTE             dataLocation;
        INT32             dataSize;
        TypeHandle        dataTypeHandle;
        BYTE              dataType;

        OBJECTREF* pArguments = pArgArray->m_Array;
        UINT32 i;
        for (i=0; i<nFixedArgs; i++)
        {
            ArgInfo* pArgInfo = pArgInfoStart + i;

            dataSize = pArgInfo->dataSize;
            dataLocation = pArgInfo->dataLocation;
            dataTypeHandle = pArgInfo->dataTypeHandle;
            dataType = pArgInfo->dataType;

                // Nullable<T> needs special treatment, even if it is 1, 2, 4, or 8 bytes
            if (dataType == ELEMENT_TYPE_VALUETYPE)
                goto DEFAULT_CASE;

            switch (dataSize)
            {
                case 1:
                    // This "if" statement is necessary to make the assignement big-endian aware
                    if (pArgInfo->byref)
                        *((INT8*)dataLocation) = *((INT8*)pArguments[i]->GetData());
                    else
                        *(StackElemType*)dataLocation = (StackElemType)*((INT8*)pArguments[i]->GetData());
                    break;
                case 2:
                    // This "if" statement is necessary to make the assignement big-endian aware
                    if (pArgInfo->byref)
                    *((INT16*)dataLocation) = *((INT16*)pArguments[i]->GetData());
                    else
                        *(StackElemType*)dataLocation = (StackElemType)*((INT16*)pArguments[i]->GetData());
                    break;
                case 4:
#ifndef _WIN64
                    if ((dataType == ELEMENT_TYPE_STRING)  ||
                        (dataType == ELEMENT_TYPE_OBJECT)  ||
                        (dataType == ELEMENT_TYPE_CLASS)   ||
                        (dataType == ELEMENT_TYPE_SZARRAY) ||
                        (dataType == ELEMENT_TYPE_ARRAY))
                    {
                        *(OBJECTREF *)dataLocation = pArguments[i];
                    }
                    else
                    {
                        *(StackElemType*)dataLocation = (StackElemType)*((INT32*)pArguments[i]->GetData());
                    }
#else // !_WIN64
                    // This "if" statement is necessary to make the assignement big-endian aware
                    if (pArgInfo->byref)
                        *(INT32*)dataLocation = *((INT32*)pArguments[i]->GetData());
                    else
                        *(StackElemType*)dataLocation = (StackElemType)*((INT32*)pArguments[i]->GetData());
#endif // !_WIN64
                    break;

                case 8:
#ifdef _WIN64
                    if ((dataType == ELEMENT_TYPE_STRING)  ||
                        (dataType == ELEMENT_TYPE_OBJECT)  ||
                        (dataType == ELEMENT_TYPE_CLASS)   ||
                        (dataType == ELEMENT_TYPE_SZARRAY) ||
                        (dataType == ELEMENT_TYPE_ARRAY))
                    {
                        *(OBJECTREF *)dataLocation = pArguments[i];
                    }
                    else
                    {
                        *((INT64*)dataLocation) = *((INT64*)pArguments[i]->GetData());
                    }
#else // _WIN64
                    *((INT64*)dataLocation) = *((INT64*)pArguments[i]->GetData());
#endif // _WIN64
                    break;

                default:
                {
                DEFAULT_CASE:
                    MethodTable * pMT = dataTypeHandle.GetMethodTable();

#ifdef ENREGISTERED_PARAMTYPE_MAXSIZE
                    // We do not need to allocate a buffer if the argument is already passed by reference.
                    if (!pArgInfo->byref && ArgIterator::IsArgPassedByRef(dataTypeHandle))
                    {
                        PVOID pvArg = _alloca(dataSize);
                        pMT->UnBoxIntoUnchecked(pvArg, pArguments[i]);
                        *(PVOID*)dataLocation = pvArg;

                        pValueClasses = new (_alloca(sizeof(ValueClassInfo))) ValueClassInfo(pvArg, pMT, pValueClasses);
                    }
                    else
#endif
                    {
                        pMT->UnBoxIntoUnchecked(dataLocation, pArguments[i]);
                    }
                }
            }
        }

#ifdef _DEBUG
        // Should not be using this any more
        memset((void *)pArgInfoStart, 0, sizeof(ArgInfo)*nFixedArgs);
#endif

        // if there were byrefs, push a protection frame
        if (pByRefs && numByRef > 0)
        {
            char *pBuffer = (char*)_alloca (sizeof (FrameWithCookie<ProtectByRefsFrame>));
            pProtectionFrame = new (pBuffer) FrameWithCookie<ProtectByRefsFrame>(pThread, pByRefs);
        }

        // If there were any value classes that must be protected by the
        // caller, push a ProtectValueClassFrame.
        if (pValueClasses)
        {
            char *pBuffer = (char*)_alloca (sizeof (FrameWithCookie<ProtectValueClassFrame>));
            pProtectValueClassFrame = new (pBuffer) FrameWithCookie<ProtectValueClassFrame>(pThread, pValueClasses);
        }

    } // GCX_FORBID

    UINT fpReturnSize = argit.GetFPReturnSize();

    CallDescrData callDescrData;

    callDescrData.pSrc = pTransitionBlock + sizeof(TransitionBlock);
    callDescrData.numStackSlots = nStackBytes / STACK_ELEM_SIZE;
#ifdef CALLDESCR_ARGREGS
    callDescrData.pArgumentRegisters = (ArgumentRegisters*)(pTransitionBlock + TransitionBlock::GetOffsetOfArgumentRegisters());
#endif
#ifdef CALLDESCR_FPARGREGS
    callDescrData.pFloatArgumentRegisters = pFloatArgumentRegisters;
#endif
#ifdef CALLDESCR_REGTYPEMAP
    callDescrData.dwRegTypeMap = dwRegTypeMap;
#endif
    callDescrData.fpReturnSize = fpReturnSize;
    callDescrData.pTarget = pTarget;

    CallDescrWorkerWithHandler(&callDescrData);

        // It is still illegal to do a GC here.  The return type might have/contain GC pointers.
    if (retElemType == ELEMENT_TYPE_VALUETYPE) 
    {
        _ASSERTE(*pVarRet != NULL);            // we have already allocated a return object
        PVOID pVarRetData = (*pVarRet)->GetData();

        // If the return result was passed back in registers, then copy it into the return object
        if (!argit.HasRetBuffArg())
        {
            CopyValueClass(pVarRetData, &callDescrData.returnValue, (*pVarRet)->GetMethodTable(), (*pVarRet)->GetAppDomain());
        }
        else if (pRetBufStackData != NULL)
        {
            // Copy the stack-allocated ret buff to the heap-allocated one.
            CopyValueClass(pVarRetData, pRetBufStackData, (*pVarRet)->GetMethodTable(), (*pVarRet)->GetAppDomain());
        }

        // If the return is a Nullable<T>, box it using Nullable<T> conventions.
        // TODO: this double allocates on constructions which is wasteful
        if (!retType.IsNull())
            *pVarRet = Nullable::NormalizeBox(*pVarRet);
    }
    else 
        CMessage::GetObjectFromStack(pVarRet, &callDescrData.returnValue, retElemType, retType);

        // You can now do GCs if you want to

    if (pProtectValueClassFrame)
        pProtectValueClassFrame->Pop(pThread);

    // extract the out args from the byrefs
    if (pByRefs)
    {
        do
        {
            // Always extract the data ptr every time we enter this loop because
            // calls to GetObjectFromStack below can cause a GC.
            // Even this is not enough, because that we are passing a pointer to GC heap
            // to GetObjectFromStack .  If GC happens, nobody is protecting the passed in pointer.

            OBJECTREF pTmp = NULL;
            void* dataLocation = pByRefs->data;
            CMessage::GetObjectFromStack(&pTmp, &dataLocation, pByRefs->typ, pByRefs->typeHandle, TRUE);
            (*ppVarOutParams)->SetAt(pByRefs->argIndex, pTmp);
            pByRefs = pByRefs->pNext;
        }
        while (pByRefs);

        if (pProtectionFrame)
            pProtectionFrame->Pop(pThread);
    }

    }  // ProfilerServerCallbackHolder

#ifdef _PREFAST_
#pragma warning(pop)
#endif

    LOG((LF_REMOTING, LL_INFO10, "CallDescrWithObjectArray OUT\n"));
}

#endif // FEATURE_REMOTING
