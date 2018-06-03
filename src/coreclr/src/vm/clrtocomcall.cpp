// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// 
// File: CLRtoCOMCall.cpp
//

// 
// CLR to COM call support.
// 


#include "common.h"

#include "stublink.h"
#include "excep.h"
#include "clrtocomcall.h"
#include "siginfo.hpp"
#include "comcallablewrapper.h"
#include "runtimecallablewrapper.h"
#include "dllimport.h"
#include "mlinfo.h"
#include "eeconfig.h"
#include "corhost.h"
#include "reflectioninvocation.h"
#include "mdaassistants.h"
#include "sigbuilder.h"

#define DISPATCH_INVOKE_SLOT 6

#ifndef DACCESS_COMPILE

//
// dllimport.cpp
void CreateCLRToDispatchCOMStub(
            MethodDesc * pMD,
            DWORD        dwStubFlags             // NDirectStubFlags
            );

#ifndef CROSSGEN_COMPILE

PCODE TheGenericComplusCallStub()
{
    LIMITED_METHOD_CONTRACT;

    return GetEEFuncEntryPoint(GenericComPlusCallStub);
}

#endif //#ifndef CROSSGEN_COMPILE


ComPlusCallInfo *ComPlusCall::PopulateComPlusCallMethodDesc(MethodDesc* pMD, DWORD* pdwStubFlags)
{
    CONTRACTL
    {
        STANDARD_VM_CHECK;
        PRECONDITION(CheckPointer(pMD));
        PRECONDITION(CheckPointer(pdwStubFlags, NULL_OK));
    }
    CONTRACTL_END;

    MethodTable *pMT = pMD->GetMethodTable();
    MethodTable *pItfMT = NULL;

    // We are going to use this MethodDesc for a CLR->COM call
    g_IBCLogger.LogMethodCodeAccess(pMD);

    if (pMD->IsComPlusCall())
    {
        ComPlusCallMethodDesc *pCMD = (ComPlusCallMethodDesc *)pMD;
        if (pCMD->m_pComPlusCallInfo == NULL)
        {
            // We are going to write the m_pComPlusCallInfo field of the MethodDesc
            g_IBCLogger.LogMethodDescWriteAccess(pMD);
            EnsureWritablePages(pCMD);

            LoaderHeap *pHeap = pMD->GetLoaderAllocator()->GetHighFrequencyHeap();
            ComPlusCallInfo *pTemp = (ComPlusCallInfo *)(void *)pHeap->AllocMem(S_SIZE_T(sizeof(ComPlusCallInfo)));

            pTemp->InitStackArgumentSize();

            InterlockedCompareExchangeT(&pCMD->m_pComPlusCallInfo, pTemp, NULL);
        }
    }

    ComPlusCallInfo *pComInfo = ComPlusCallInfo::FromMethodDesc(pMD);
    _ASSERTE(pComInfo != NULL);
    EnsureWritablePages(pComInfo);

    BOOL fWinRTCtor = FALSE;
    BOOL fWinRTComposition = FALSE;
    BOOL fWinRTStatic = FALSE;
    BOOL fWinRTDelegate = FALSE;

    if (pMD->IsInterface())
    {
        pComInfo->m_cachedComSlot = pMD->GetComSlot();
        pItfMT = pMT;
        pComInfo->m_pInterfaceMT = pItfMT;
    }
    else if (pMT->IsWinRTDelegate())
    {
        pComInfo->m_cachedComSlot = ComMethodTable::GetNumExtraSlots(ifVtable);
        pItfMT = pMT;
        pComInfo->m_pInterfaceMT = pItfMT;

        fWinRTDelegate = TRUE;
    }
    else
    {
        BOOL fIsWinRTClass = (!pMT->IsInterface() && pMT->IsProjectedFromWinRT());
        MethodDesc *pItfMD;

        if (fIsWinRTClass && pMD->IsCtor())
        {
            // ctors on WinRT classes call factory interface methods
            pItfMD = GetWinRTFactoryMethodForCtor(pMD, &fWinRTComposition);
            fWinRTCtor = TRUE;
        }
        else if (fIsWinRTClass && pMD->IsStatic())
        {
            // static members of WinRT classes call static interface methods
            pItfMD = GetWinRTFactoryMethodForStatic(pMD);
            fWinRTStatic = TRUE;
        }
        else
        {
            pItfMD = pMD->GetInterfaceMD();
            if (pItfMD == NULL)
            {
                // the method does not implement any interface
                StackSString ssClassName;
                pMT->_GetFullyQualifiedNameForClass(ssClassName);
                StackSString ssMethodName(SString::Utf8, pMD->GetName());

                COMPlusThrow(kInvalidOperationException, IDS_EE_COMIMPORT_METHOD_NO_INTERFACE, ssMethodName.GetUnicode(), ssClassName.GetUnicode());
            }
        }

        pComInfo->m_cachedComSlot = pItfMD->GetComSlot();
        pItfMT = pItfMD->GetMethodTable();
        pComInfo->m_pInterfaceMT = pItfMT;
    }

    if (pdwStubFlags == NULL)
        return pComInfo;

    pMD->ComputeSuppressUnmanagedCodeAccessAttr(pMD->GetMDImport());

    //
    // Compute NDirectStubFlags
    //

    DWORD dwStubFlags = NDIRECTSTUB_FL_COM;

    // Determine if this is a special COM event call.
    BOOL fComEventCall = pItfMT->IsComEventItfType();

    // Determine if the call needs to do early bound to late bound convertion.
    BOOL fLateBound = !fComEventCall && pItfMT->IsInterface() && pItfMT->GetComInterfaceType() == ifDispatch;

    if (fLateBound)
        dwStubFlags |= NDIRECTSTUB_FL_COMLATEBOUND;

    if (fComEventCall)
        dwStubFlags |= NDIRECTSTUB_FL_COMEVENTCALL;

    bool fIsWinRT = (pItfMT->IsProjectedFromWinRT() || pItfMT->IsWinRTRedirectedDelegate());
    if (!fIsWinRT && pItfMT->IsWinRTRedirectedInterface(TypeHandle::Interop_ManagedToNative))
    {
        if (!pItfMT->HasInstantiation())
        {
            // non-generic redirected interface needs to keep its pre-4.5 classic COM interop
            // behavior so the IL stub will be special - it will conditionally tail-call to
            // the new WinRT marshaling routines
            dwStubFlags |= NDIRECTSTUB_FL_WINRTHASREDIRECTION;
        }
        else
        {
            fIsWinRT = true;
        }
    }

    if (fIsWinRT)
    {
        dwStubFlags |= NDIRECTSTUB_FL_WINRT;

        if (pMD->IsGenericComPlusCall())
            dwStubFlags |= NDIRECTSTUB_FL_WINRTSHAREDGENERIC;
    }

    if (fWinRTCtor)
    {
        dwStubFlags |= NDIRECTSTUB_FL_WINRTCTOR;

        if (fWinRTComposition)
            dwStubFlags |= NDIRECTSTUB_FL_WINRTCOMPOSITION;
    }

    if (fWinRTStatic)
        dwStubFlags |= NDIRECTSTUB_FL_WINRTSTATIC;

    if (fWinRTDelegate)
        dwStubFlags |= NDIRECTSTUB_FL_WINRTDELEGATE | NDIRECTSTUB_FL_WINRT;

    BOOL BestFit = TRUE;
    BOOL ThrowOnUnmappableChar = FALSE;

    // Marshaling is fully described by the parameter type in WinRT. BestFit custom attributes 
    // are not going to affect the marshaling behavior.
    if (!fIsWinRT)
    {
        ReadBestFitCustomAttribute(pMD, &BestFit, &ThrowOnUnmappableChar);
    }

    if (BestFit)
        dwStubFlags |= NDIRECTSTUB_FL_BESTFIT;

    if (ThrowOnUnmappableChar)
        dwStubFlags |= NDIRECTSTUB_FL_THROWONUNMAPPABLECHAR;

    //
    // fill in out param
    //
    *pdwStubFlags = dwStubFlags;
    
    return pComInfo;
}

// static
MethodDesc *ComPlusCall::GetWinRTFactoryMethodForCtor(MethodDesc *pMDCtor, BOOL *pComposition)
{
    CONTRACTL
    {
        STANDARD_VM_CHECK;
        PRECONDITION(CheckPointer(pMDCtor));
        PRECONDITION(pMDCtor->IsCtor());
    }
    CONTRACTL_END;

    MethodTable *pMT = pMDCtor->GetMethodTable();
    _ASSERTE(pMT->IsProjectedFromWinRT());

    // If someone is trying to access a WinRT attribute, block it since there is no actual implementation type
    MethodTable *pParentMT = pMT->GetParentMethodTable();
    if (pParentMT == MscorlibBinder::GetClass(CLASS__ATTRIBUTE))
    {
        DefineFullyQualifiedNameForClassW();
        COMPlusThrow(kInvalidOperationException, IDS_EE_WINRT_ATTRIBUTES_NOT_INVOKABLE, GetFullyQualifiedNameForClassW(pMT));
    }

    // build the expected factory method signature
    PCCOR_SIGNATURE pSig;
    DWORD cSig;
    pMDCtor->GetSig(&pSig, &cSig);
    SigParser ctorSig(pSig, cSig);
    
    ULONG numArgs;

    IfFailThrow(ctorSig.GetCallingConv(NULL)); // calling convention
    IfFailThrow(ctorSig.GetData(&numArgs));    // number of args
    IfFailThrow(ctorSig.SkipExactlyOne());     // skip return type

    // Get the class factory for the type
    WinRTClassFactory *pFactory = GetComClassFactory(pMT)->AsWinRTClassFactory();
    BOOL fComposition = pFactory->IsComposition();

    if (numArgs == 0 && !fComposition)
    {
        // this is a default ctor - it will use IActivationFactory::ActivateInstance
        return MscorlibBinder::GetMethod(METHOD__IACTIVATIONFACTORY__ACTIVATE_INSTANCE);
    }

    // Composition factory methods have two additional arguments 
    // For now a class has either composition factories or regular factories but never both.
    // In future versions it's possible we may want to allow a class to become unsealed, in 
    // which case we'll probably need to support both and change how we find factory methods.
    if (fComposition)
    {
        numArgs += 2;
    }

    SigBuilder sigBuilder;
    sigBuilder.AppendByte(IMAGE_CEE_CS_CALLCONV_HASTHIS);
    sigBuilder.AppendData(numArgs);

    // the return type is the class that declares the ctor
    sigBuilder.AppendElementType(ELEMENT_TYPE_INTERNAL);
    sigBuilder.AppendPointer(pMT);

    // parameter types are identical
    ctorSig.GetSignature(&pSig, &cSig);
    sigBuilder.AppendBlob((const PVOID)pSig, cSig);

    if (fComposition)
    {
        // in: outer IInspectable to delegate to, or null
        sigBuilder.AppendElementType(ELEMENT_TYPE_OBJECT);
    
        // out: non-delegating IInspectable for the created object
        sigBuilder.AppendElementType(ELEMENT_TYPE_BYREF);
        sigBuilder.AppendElementType(ELEMENT_TYPE_OBJECT);
    }

    pSig = (PCCOR_SIGNATURE)sigBuilder.GetSignature(&cSig);

    // ask the factory to find a matching method
    MethodDesc *pMD = pFactory->FindFactoryMethod(pSig, cSig, pMDCtor->GetModule());

    if (pMD == NULL)
    {
        // @TODO: Do we want a richer exception message?
        SString ctorMethodName(SString::Utf8, COR_CTOR_METHOD_NAME);
        COMPlusThrowNonLocalized(kMissingMethodException, ctorMethodName.GetUnicode());
    }

    if (pComposition != NULL)
    {
        *pComposition = fComposition;
    }

    return pMD;
}

// static
MethodDesc *ComPlusCall::GetWinRTFactoryMethodForStatic(MethodDesc *pMDStatic)
{
    CONTRACTL
    {
        STANDARD_VM_CHECK;
        PRECONDITION(CheckPointer(pMDStatic));
        PRECONDITION(pMDStatic->IsStatic());
    }
    CONTRACTL_END;

    MethodTable *pMT = pMDStatic->GetMethodTable();
    _ASSERTE(pMT->IsProjectedFromWinRT());

    // build the expected interface method signature
    PCCOR_SIGNATURE pSig;
    DWORD cSig;
    pMDStatic->GetSig(&pSig, &cSig);
    SigParser ctorSig(pSig, cSig);
    
    IfFailThrow(ctorSig.GetCallingConv(NULL)); // calling convention

    // use the "has this" calling convention because we're looking for an instance method
    SigBuilder sigBuilder;
    sigBuilder.AppendByte(IMAGE_CEE_CS_CALLCONV_HASTHIS);

    // return type and parameter types are identical
    ctorSig.GetSignature(&pSig, &cSig);
    sigBuilder.AppendBlob((const PVOID)pSig, cSig);

    pSig = (PCCOR_SIGNATURE)sigBuilder.GetSignature(&cSig);

    // ask the factory to find a matching method
    WinRTClassFactory *pFactory = GetComClassFactory(pMT)->AsWinRTClassFactory();
    MethodDesc *pMD = pFactory->FindStaticMethod(pMDStatic->GetName(), pSig, cSig, pMDStatic->GetModule());

    if (pMD == NULL)
    {
        // @TODO: Do we want a richer exception message?
        SString staticMethodName(SString::Utf8, pMDStatic->GetName());
        COMPlusThrowNonLocalized(kMissingMethodException, staticMethodName.GetUnicode());
    }

    return pMD;
}

MethodDesc* ComPlusCall::GetILStubMethodDesc(MethodDesc* pMD, DWORD dwStubFlags)
{
    STANDARD_VM_CONTRACT;

    if (SF_IsCOMLateBoundStub(dwStubFlags) || SF_IsCOMEventCallStub(dwStubFlags))
        return NULL;

    // Get the call signature information
    StubSigDesc sigDesc(pMD);

    return NDirect::CreateCLRToNativeILStub(
                    &sigDesc,
                    (CorNativeLinkType)0, 
                    (CorNativeLinkFlags)0, 
                    (CorPinvokeMap)0, 
                    dwStubFlags);
}


#ifndef CROSSGEN_COMPILE

PCODE ComPlusCall::GetStubForILStub(MethodDesc* pMD, MethodDesc** ppStubMD)
{
    STANDARD_VM_CONTRACT;

    _ASSERTE(pMD->IsComPlusCall() || pMD->IsGenericComPlusCall());

    ComPlusCallInfo *pComInfo = NULL;

    if (*ppStubMD != NULL)
    {
        // pStubMD, if provided, must be preimplemented.
        _ASSERTE((*ppStubMD)->IsPreImplemented());

        pComInfo = ComPlusCallInfo::FromMethodDesc(pMD);
        _ASSERTE(pComInfo != NULL);

        _ASSERTE((*ppStubMD) ==  pComInfo->m_pStubMD.GetValue());

        if (pComInfo->m_pInterfaceMT == NULL)
        {
            ComPlusCall::PopulateComPlusCallMethodDesc(pMD, NULL);
        }
        else
        {
            pComInfo->m_pInterfaceMT->CheckRestore();
        }

        if (pComInfo->m_pILStub == NULL)
        {
            PCODE pCode = JitILStub(*ppStubMD);
            InterlockedCompareExchangeT<PCODE>(EnsureWritablePages(pComInfo->GetAddrOfILStubField()), pCode, NULL);
        }
        else
        {
            // Pointer to pre-implemented code initialized at NGen-time
            _ASSERTE((*ppStubMD)->GetNativeCode() == pComInfo->m_pILStub);
        }
    }
    else
    {
        DWORD dwStubFlags; 
        pComInfo = ComPlusCall::PopulateComPlusCallMethodDesc(pMD, &dwStubFlags);

        if (!pComInfo->m_pStubMD.IsNull())
        {
            // Discard pre-implemented code
            PCODE pPreImplementedCode = pComInfo->m_pStubMD.GetValue()->GetNativeCode();
            InterlockedCompareExchangeT<PCODE>(pComInfo->GetAddrOfILStubField(), NULL, pPreImplementedCode);
        }

        *ppStubMD = ComPlusCall::GetILStubMethodDesc(pMD, dwStubFlags);

        if (*ppStubMD != NULL)
        {
            PCODE pCode = JitILStub(*ppStubMD);
            InterlockedCompareExchangeT<PCODE>(pComInfo->GetAddrOfILStubField(), pCode, NULL);
        }
        else
        {
            CreateCLRToDispatchCOMStub(pMD, dwStubFlags);
        }
    }

    PCODE pStub = NULL;

    if (*ppStubMD)
    {
        {
            pStub = *pComInfo->GetAddrOfILStubField();
        }
    }
    else
    {
        pStub = TheGenericComplusCallStub();
    }

    return pStub;
}


I4ARRAYREF SetUpWrapperInfo(MethodDesc *pMD)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        INJECT_FAULT(COMPlusThrowOM());
        PRECONDITION(CheckPointer(pMD));
    }
    CONTRACTL_END;

    MetaSig msig(pMD);
    int numArgs = msig.NumFixedArgs();

    I4ARRAYREF WrapperTypeArr = NULL;

    GCPROTECT_BEGIN(WrapperTypeArr)
    {
        //
        // Allocate the array of wrapper types.
        //

        WrapperTypeArr = (I4ARRAYREF)AllocatePrimitiveArray(ELEMENT_TYPE_I4, numArgs);

        GCX_PREEMP();

        // Collects ParamDef information in an indexed array where element 0 represents
        // the return type.
        mdParamDef *params = (mdParamDef*)_alloca((numArgs+1) * sizeof(mdParamDef));
        CollateParamTokens(msig.GetModule()->GetMDImport(), pMD->GetMemberDef(), numArgs, params);


        //
        // Look up the best fit mapping info via Assembly & Interface level attributes
        //

        BOOL BestFit = TRUE;
        BOOL ThrowOnUnmappableChar = FALSE;
        ReadBestFitCustomAttribute(pMD, &BestFit, &ThrowOnUnmappableChar);

        //
        // Determine the wrapper type of the arguments.
        //

        int iParam = 1;
        CorElementType mtype;
        while (ELEMENT_TYPE_END != (mtype = msig.NextArg()))
        {
            //
            // Set up the marshaling info for the parameter.
            //

            MarshalInfo Info(msig.GetModule(), msig.GetArgProps(), msig.GetSigTypeContext(), params[iParam],
                             MarshalInfo::MARSHAL_SCENARIO_COMINTEROP, (CorNativeLinkType)0, (CorNativeLinkFlags)0,
                             TRUE, iParam, numArgs, BestFit, ThrowOnUnmappableChar, FALSE, pMD, TRUE
    #ifdef _DEBUG
                             , pMD->m_pszDebugMethodName, pMD->m_pszDebugClassName, iParam
    #endif
                             );

            DispatchWrapperType wrapperType = Info.GetDispWrapperType();

            {
                GCX_COOP();

                //
                // Based on the MarshalInfo, set the wrapper type.
                //

                *((DWORD*)WrapperTypeArr->GetDataPtr() + iParam - 1) = wrapperType;
            }

            //
            // Increase the argument index.
            //

            iParam++;
        }
    }
    GCPROTECT_END();

    return WrapperTypeArr;
}

UINT32 CLRToCOMEventCallWorker(ComPlusMethodFrame* pFrame, ComPlusCallMethodDesc *pMD)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(CheckPointer(pFrame));
        PRECONDITION(CheckPointer(pMD));
    }
    CONTRACTL_END;

    struct _gc {
        OBJECTREF EventProviderTypeObj;
        OBJECTREF EventProviderObj;
        OBJECTREF ThisObj;
    } gc;
    ZeroMemory(&gc, sizeof(gc));
    

    LOG((LF_STUBS, LL_INFO1000, "Calling CLRToCOMEventCallWorker %s::%s \n", pMD->m_pszDebugClassName, pMD->m_pszDebugMethodName));

    // Retrieve the method table and the method desc of the call.
    MethodDesc *pEvProvMD = pMD->GetEventProviderMD();
    MethodTable *pEvProvMT = pEvProvMD->GetMethodTable();

    GCPROTECT_BEGIN(gc)
    {
        // Retrieve the exposed type object for event provider.
        gc.EventProviderTypeObj = pEvProvMT->GetManagedClassObject();
        gc.ThisObj = pFrame->GetThis();

        MethodDescCallSite getEventProvider(METHOD__COM_OBJECT__GET_EVENT_PROVIDER, &gc.ThisObj);

        // Retrieve the event provider for the event interface type.
        ARG_SLOT GetEventProviderArgs[] =
        {
            ObjToArgSlot(gc.ThisObj),
            ObjToArgSlot(gc.EventProviderTypeObj)
        };

        gc.EventProviderObj = getEventProvider.Call_RetOBJECTREF(GetEventProviderArgs);

        // Set up an arg iterator to retrieve the arguments from the frame.
        MetaSig mSig(pMD);
        ArgIterator ArgItr(&mSig);

        // Make the call on the event provider method desc.
        MethodDescCallSite eventProvider(pEvProvMD, &gc.EventProviderObj);

        // Retrieve the event handler passed in.
        OBJECTREF EventHandlerObj = *(OBJECTREF*)(pFrame->GetTransitionBlock() + ArgItr.GetNextOffset());
       
        ARG_SLOT EventMethArgs[] =
        {
            ObjToArgSlot(gc.EventProviderObj),
            ObjToArgSlot(EventHandlerObj)
        };

        //
        // If this can ever return something bigger than an INT64 byval
        // then this code is broken.  Currently, however, it cannot.
        //
        *(ARG_SLOT *)(pFrame->GetReturnValuePtr()) = eventProvider.Call_RetArgSlot(EventMethArgs);

        // The COM event call worker does not support value returned in
        // floating point registers.
        _ASSERTE(ArgItr.GetFPReturnSize() == 0);
    }
    GCPROTECT_END();

    // tell the asm stub that we are not returning an FP type
    return 0;
}


// calls that propagate from CLR to COM

#pragma optimize( "y", off )
/*static*/
UINT32 STDCALL CLRToCOMWorker(TransitionBlock * pTransitionBlock, ComPlusCallMethodDesc * pMD)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        ENTRY_POINT;
        PRECONDITION(CheckPointer(pTransitionBlock, NULL_NOT_OK));
    }
    CONTRACTL_END;

    UINT32 returnValue = 0;

    // This must happen before the UnC handler is setup.  Otherwise, an exception will
    // cause the UnC handler to pop this frame, leaving a GC hole a mile wide.

    MAKE_CURRENT_THREAD_AVAILABLE();

    FrameWithCookie<ComPlusMethodFrame> frame(pTransitionBlock, pMD);
    ComPlusMethodFrame * pFrame = &frame;

    //we need to zero out the return value buffer because we will report it during GC
#ifdef ENREGISTERED_RETURNTYPE_MAXSIZE
    ZeroMemory (pFrame->GetReturnValuePtr(), ENREGISTERED_RETURNTYPE_MAXSIZE);
#else
    *(ARG_SLOT *)pFrame->GetReturnValuePtr() = 0;
#endif

    // Link frame into the chain.
    pFrame->Push(CURRENT_THREAD);

    INSTALL_UNWIND_AND_CONTINUE_HANDLER

    _ASSERTE(pMD->IsComPlusCall());

    // Make sure we have been properly loaded here
    CONSISTENCY_CHECK(GetAppDomain()->CheckCanExecuteManagedCode(pMD));

    // Retrieve the interface method table.
    MethodTable *pItfMT = pMD->GetInterfaceMethodTable();

    // If the interface is a COM event call, then delegate to the CLRToCOMEventCallWorker.
    if (pItfMT->IsComEventItfType())
    {
        returnValue = CLRToCOMEventCallWorker(pFrame, pMD);
    }
    else
    {
        LOG((LF_STUBS, LL_INFO1000, "Calling CLRToCOMWorker %s::%s \n", pMD->m_pszDebugClassName, pMD->m_pszDebugMethodName));

        CONSISTENCY_CHECK_MSG(false, "Should not get here when using IL stubs.");
    }

    UNINSTALL_UNWIND_AND_CONTINUE_HANDLER;

    pFrame->Pop(CURRENT_THREAD);

    return returnValue;
}

#pragma optimize( "", on )

#endif // CROSSGEN_COMPILE
#endif // #ifndef DACCESS_COMPILE

#ifndef CROSSGEN_COMPILE
//---------------------------------------------------------
// Debugger support for ComPlusMethodFrame
//---------------------------------------------------------
TADDR ComPlusCall::GetFrameCallIP(FramedMethodFrame *frame)
{
    CONTRACT (TADDR)
    {
        WRAPPER(THROWS);
        WRAPPER(GC_TRIGGERS);
        MODE_ANY;
        PRECONDITION(CheckPointer(frame));
        POSTCONDITION(CheckPointer((void*)RETVAL, NULL_OK));
    }
    CONTRACT_END;

    ComPlusCallMethodDesc *pCMD = dac_cast<PTR_ComPlusCallMethodDesc>(frame->GetFunction());
    MethodTable *pItfMT = pCMD->GetInterfaceMethodTable();
    TADDR ip = NULL;
#ifndef DACCESS_COMPILE
    SafeComHolder<IUnknown> pUnk   = NULL;
#endif

    _ASSERTE(pCMD->IsComPlusCall());

    // Note: if this is a COM event call, then the call will be delegated to a different object. The logic below will
    // fail with an invalid cast error. For V1, we just won't step into those.
    if (pItfMT->IsComEventItfType())
        RETURN NULL;

    //
    // This is called from some strange places - from
    // unmanaged code, from managed code, from the debugger
    // helper thread.  Make sure we can deal with this object
    // ref.
    //

#ifndef DACCESS_COMPILE
    
    Thread* thread = GetThread();
    if (thread == NULL)
    {
        //
        // This is being called from the debug helper thread.
        // Unfortunately this doesn't bode well for the COM+ IP
        // mapping code - it expects to be called from the appropriate
        // context.
        //
        // This context-naive code will work for most cases.
        //
        // It toggles the GC mode, tries to setup a thread, etc, right after our
        // verification that we have no Thread object above. This needs to be fixed properly in Beta 2. This is a work
        // around for Beta 1, which is just to #if 0 the code out and return NULL.
        //
        pUnk = NULL;
    }
    else
    {
        GCX_COOP();

        OBJECTREF *pOref = frame->GetThisPtr();
        pUnk = ComObject::GetComIPFromRCWThrowing(pOref, pItfMT);
    }

    if (pUnk != NULL)
    {
        if (pItfMT->GetComInterfaceType() == ifDispatch)
            ip = (TADDR)(*(void ***)(IUnknown*)pUnk)[DISPATCH_INVOKE_SLOT];
        else
            ip = (TADDR)(*(void ***)(IUnknown*)pUnk)[pCMD->m_pComPlusCallInfo->m_cachedComSlot];
    }

#else
    DacNotImpl();
#endif // #ifndef DACCESS_COMPILE

    RETURN ip;
}

void ComPlusMethodFrame::GetUnmanagedCallSite(TADDR* ip,
                                              TADDR* returnIP,
                                              TADDR* returnSP)
{
    CONTRACTL
    {
        WRAPPER(THROWS);
        WRAPPER(GC_TRIGGERS);
        MODE_ANY;
        PRECONDITION(CheckPointer(ip, NULL_OK));
        PRECONDITION(CheckPointer(returnIP, NULL_OK));
        PRECONDITION(CheckPointer(returnSP, NULL_OK));
    }
    CONTRACTL_END;

    LOG((LF_CORDB, LL_INFO100000, "ComPlusMethodFrame::GetUnmanagedCallSite\n"));

    if (ip != NULL)
        *ip = ComPlusCall::GetFrameCallIP(this);

    TADDR retSP = NULL;
    // We can't assert retSP here because the debugger may actually call this function even when 
    // the frame is not fully initiailzed.  It is ok because the debugger has code to handle this 
    // case.  However, other callers may not be tolerant of this case, so we should push this assert 
    // to the callers
    //_ASSERTE(retSP != NULL);

    if (returnIP != NULL)
    {
        *returnIP = retSP ? *(TADDR*)retSP : NULL;
    }

    if (returnSP != NULL)
    {
        *returnSP = retSP;
    }

}



BOOL ComPlusMethodFrame::TraceFrame(Thread *thread, BOOL fromPatch,
                                    TraceDestination *trace, REGDISPLAY *regs)
{
    CONTRACTL
    {
        WRAPPER(THROWS);
        WRAPPER(GC_TRIGGERS);
        MODE_ANY;
        PRECONDITION(CheckPointer(thread));
        PRECONDITION(CheckPointer(trace));
    }
    CONTRACTL_END;

    //
    // Get the call site info
    //

#if defined(_WIN64)
    // Interop debugging is currently not supported on WIN64, so we always return FALSE.
    // The result is that you can't step into an unmanaged frame or step out to one.  You
    // also can't step a breakpoint in one.
    return FALSE;
#endif // _WIN64

    TADDR ip, returnIP, returnSP;
    GetUnmanagedCallSite(&ip, &returnIP, &returnSP);

    //
    // If we've already made the call, we can't trace any more.
    //
    // !!! Note that this test isn't exact.
    //

    if (!fromPatch &&
        (dac_cast<TADDR>(thread->GetFrame()) != dac_cast<TADDR>(this) ||
         !thread->m_fPreemptiveGCDisabled ||
         *PTR_TADDR(returnSP) == returnIP))
    {
        LOG((LF_CORDB, LL_INFO10000, "ComPlusMethodFrame::TraceFrame: can't trace...\n"));
        return FALSE;
    }

    //
    // Otherwise, return the unmanaged destination.
    //

    trace->InitForUnmanaged(ip);

    LOG((LF_CORDB, LL_INFO10000,
         "ComPlusMethodFrame::TraceFrame: ip=0x%p\n", ip));

    return TRUE;
}
#endif //CROSSGEN_COMPILE

#ifdef _TARGET_X86_

#ifndef DACCESS_COMPILE

CrstStatic   ComPlusCall::s_RetThunkCacheCrst;
SHash<ComPlusCall::RetThunkSHashTraits> *ComPlusCall::s_pRetThunkCache = NULL;

// One time init.
void ComPlusCall::Init()
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    s_RetThunkCacheCrst.Init(CrstRetThunkCache);
}

LPVOID ComPlusCall::GetRetThunk(UINT numStackBytes)
{
    STANDARD_VM_CONTRACT;

    LPVOID pRetThunk = NULL;
    CrstHolder crst(&s_RetThunkCacheCrst);

    // Lazily allocate the ret thunk cache.
    if (s_pRetThunkCache == NULL)
        s_pRetThunkCache = new SHash<RetThunkSHashTraits>();

    const RetThunkCacheElement *pElement = s_pRetThunkCache->LookupPtr(numStackBytes);
    if (pElement != NULL)
    {
        pRetThunk = pElement->m_pRetThunk;
    }
    else
    {
        // cache miss -> create a new thunk
        AllocMemTracker dummyAmTracker;
        pRetThunk = (LPVOID)dummyAmTracker.Track(SystemDomain::GetGlobalLoaderAllocator()->GetExecutableHeap()->AllocMem(S_SIZE_T((numStackBytes == 0) ? 1 : 3)));

        BYTE *pThunk = (BYTE *)pRetThunk;
        if (numStackBytes == 0)
        {
            pThunk[0] = 0xc3;
        }
        else
        {
            pThunk[0] = 0xc2;
            *(USHORT *)&pThunk[1] = (USHORT)numStackBytes;
        }

        // add it to the cache
        RetThunkCacheElement element;
        element.m_cbStack = numStackBytes;
        element.m_pRetThunk = pRetThunk;
        s_pRetThunkCache->Add(element);

        dummyAmTracker.SuppressRelease();
    }

    return pRetThunk;
}

#endif // !DACCESS_COMPILE

#endif // _TARGET_X86_
