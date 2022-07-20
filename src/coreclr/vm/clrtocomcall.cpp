// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
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
#include "comdelegate.h"
#include "comcallablewrapper.h"
#include "runtimecallablewrapper.h"
#include "dllimport.h"
#include "mlinfo.h"
#include "eeconfig.h"
#include "corhost.h"
#include "reflectioninvocation.h"
#include "sigbuilder.h"
#include "callconvbuilder.hpp"
#include "callsiteinspect.h"

#define DISPATCH_INVOKE_SLOT 6

#ifndef DACCESS_COMPILE

//
// dllimport.cpp
void CreateCLRToDispatchCOMStub(
            MethodDesc * pMD,
            DWORD        dwStubFlags             // NDirectStubFlags
            );


PCODE TheGenericComplusCallStub()
{
    LIMITED_METHOD_CONTRACT;

    return GetEEFuncEntryPoint(GenericComPlusCallStub);
}



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

    if (pMD->IsComPlusCall())
    {
        ComPlusCallMethodDesc *pCMD = (ComPlusCallMethodDesc *)pMD;
        if (pCMD->m_pComPlusCallInfo == NULL)
        {
            LoaderHeap *pHeap = pMD->GetLoaderAllocator()->GetHighFrequencyHeap();
            ComPlusCallInfo *pTemp = (ComPlusCallInfo *)(void *)pHeap->AllocMem(S_SIZE_T(sizeof(ComPlusCallInfo)));

            pTemp->InitStackArgumentSize();

            InterlockedCompareExchangeT(&pCMD->m_pComPlusCallInfo, pTemp, NULL);
        }
    }

    ComPlusCallInfo *pComInfo = ComPlusCallInfo::FromMethodDesc(pMD);
    _ASSERTE(pComInfo != NULL);

    if (pMD->IsInterface())
    {
        pComInfo->m_cachedComSlot = pMD->GetComSlot();
        pItfMT = pMT;
        pComInfo->m_pInterfaceMT = pItfMT;
    }
    else
    {
        MethodDesc *pItfMD;

        pItfMD = pMD->GetInterfaceMD();
        if (pItfMD == NULL)
        {
            // the method does not implement any interface
            StackSString ssClassName;
            pMT->_GetFullyQualifiedNameForClass(ssClassName);
            StackSString ssMethodName(SString::Utf8, pMD->GetName());

            COMPlusThrow(kInvalidOperationException, IDS_EE_COMIMPORT_METHOD_NO_INTERFACE, ssMethodName.GetUnicode(), ssClassName.GetUnicode());
        }

        pComInfo->m_cachedComSlot = pItfMD->GetComSlot();
        pItfMT = pItfMD->GetMethodTable();
        pComInfo->m_pInterfaceMT = pItfMT;
    }

    if (pdwStubFlags == NULL)
        return pComInfo;

    //
    // Compute NDirectStubFlags
    //

    DWORD dwStubFlags = NDIRECTSTUB_FL_COM;

    // Determine if this is a special COM event call.
    BOOL fComEventCall = pItfMT->IsComEventItfType();

    // Determine if the call needs to do early bound to late bound conversion.
    BOOL fLateBound = !fComEventCall && pItfMT->IsInterface() && pItfMT->GetComInterfaceType() == ifDispatch;

    if (fLateBound)
        dwStubFlags |= NDIRECTSTUB_FL_COMLATEBOUND;

    if (fComEventCall)
        dwStubFlags |= NDIRECTSTUB_FL_COMEVENTCALL;

    BOOL BestFit = TRUE;
    BOOL ThrowOnUnmappableChar = FALSE;

    ReadBestFitCustomAttribute(pMD, &BestFit, &ThrowOnUnmappableChar);

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
                    CallConv::GetDefaultUnmanagedCallingConvention(),
                    dwStubFlags);
}



PCODE ComPlusCall::GetStubForILStub(MethodDesc* pMD, MethodDesc** ppStubMD)
{
    STANDARD_VM_CONTRACT;

    _ASSERTE(pMD->IsComPlusCall() || pMD->IsGenericComPlusCall());
    _ASSERTE(*ppStubMD == NULL);

    DWORD dwStubFlags;
    ComPlusCallInfo* pComInfo = ComPlusCall::PopulateComPlusCallMethodDesc(pMD, &dwStubFlags);

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

CallsiteDetails CreateCallsiteDetails(_In_ FramedMethodFrame *pFrame)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(CheckPointer(pFrame));
    }
    CONTRACTL_END;

    MethodDesc *pMD = pFrame->GetFunction();
    _ASSERTE(!pMD->ContainsGenericVariables() && pMD->IsRuntimeMethodHandle());

    const BOOL fIsDelegate = pMD->GetMethodTable()->IsDelegate();
    _ASSERTE(!fIsDelegate && pMD->IsRuntimeMethodHandle());

    MethodDesc *pDelegateMD = nullptr;
    INT32 callsiteFlags = CallsiteDetails::None;
    if (fIsDelegate)
    {
        // Gather details on the delegate itself
        DelegateEEClass* delegateCls = (DelegateEEClass*)pMD->GetMethodTable()->GetClass();
        _ASSERTE(pFrame->GetThis()->GetMethodTable()->IsDelegate());

        if (pMD == delegateCls->m_pBeginInvokeMethod)
        {
            callsiteFlags |= CallsiteDetails::BeginInvoke;
        }
        else
        {
            _ASSERTE(pMD == delegateCls->m_pEndInvokeMethod);
            callsiteFlags |= CallsiteDetails::EndInvoke;
        }

        pDelegateMD = pMD;

        // Get at the underlying method desc for this frame
        pMD = COMDelegate::GetMethodDesc(pFrame->GetThis());
        _ASSERTE(pDelegateMD != nullptr
            && pMD != nullptr
            && !pMD->ContainsGenericVariables()
            && pMD->IsRuntimeMethodHandle());
    }

    if (pMD->IsCtor())
        callsiteFlags |= CallsiteDetails::Ctor;

    Signature signature;
    Module *pModule;
    SigTypeContext typeContext;

    if (fIsDelegate)
    {
        _ASSERTE(pDelegateMD != nullptr);
        signature = pDelegateMD->GetSignature();
        pModule = pDelegateMD->GetModule();

        // If the delegate is generic, pDelegateMD may not represent the exact instantiation so we recover it from 'this'.
        SigTypeContext::InitTypeContext(pFrame->GetThis()->GetMethodTable()->GetInstantiation(), Instantiation{}, &typeContext);
    }
    else if (pMD->IsVarArg())
    {
        VASigCookie *pVACookie = pFrame->GetVASigCookie();
        signature = pVACookie->signature;
        pModule = pVACookie->pModule;
        SigTypeContext::InitTypeContext(&typeContext);
    }
    else
    {
        // COM doesn't support generics so the type is obvious
        TypeHandle actualType = TypeHandle{ pMD->GetMethodTable() };

        signature = pMD->GetSignature();
        pModule = pMD->GetModule();
        SigTypeContext::InitTypeContext(pMD, actualType, &typeContext);
    }

    _ASSERTE(!signature.IsEmpty() && pModule != nullptr);

    // Create details
    return CallsiteDetails{ { signature, pModule, &typeContext }, pFrame, pMD, fIsDelegate };
}

UINT32 CLRToCOMLateBoundWorker(
    _In_ ComPlusMethodFrame *pFrame,
    _In_ ComPlusCallMethodDesc *pMD)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        INJECT_FAULT(COMPlusThrowOM());
        PRECONDITION(CheckPointer(pFrame));
        PRECONDITION(CheckPointer(pMD));
    }
    CONTRACTL_END;

    HRESULT hr;

    LOG((LF_STUBS, LL_INFO1000, "Calling CLRToCOMLateBoundWorker %s::%s \n", pMD->m_pszDebugClassName, pMD->m_pszDebugMethodName));

    // Retrieve the method table and the method desc of the call.
    MethodTable *pItfMT = pMD->GetInterfaceMethodTable();
    ComPlusCallMethodDesc *pItfMD = pMD;

    // Make sure this is only called on IDispatch only interfaces.
    _ASSERTE(pItfMT->GetComInterfaceType() == ifDispatch);

    // If this is a method impl MD then we need to retrieve the actual interface MD that
    // this is a method impl for.
    // REVISIT_TODO: Stop using ComSlot to convert method impls to interface MD
    // _ASSERTE(pMD->m_pComPlusCallInfo->m_cachedComSlot == 7);
    if (!pMD->GetMethodTable()->IsInterface())
    {
        const unsigned cbExtraSlots = 7;
        pItfMD = (ComPlusCallMethodDesc*)pItfMT->GetMethodDescForSlot(pMD->m_pComPlusCallInfo->m_cachedComSlot - cbExtraSlots);
        CONSISTENCY_CHECK(pMD->GetInterfaceMD() == pItfMD);
    }

    // Token of member to call
    mdToken tkMember;
    DWORD binderFlags = BINDER_AllLookup;

    // Property details
    mdProperty propToken;
    LPCUTF8 strMemberName;
    ULONG uSemantic;

    // See if there is property information for this member.
    hr = pItfMT->GetModule()->GetPropertyInfoForMethodDef(pItfMD->GetMemberDef(), &propToken, &strMemberName, &uSemantic);
    if (hr != S_OK)
    {
        // Non-property method
        strMemberName = pItfMD->GetName();
        tkMember = pItfMD->GetMemberDef();
        binderFlags |= BINDER_InvokeMethod;
    }
    else
    {
        // Property accessor
        tkMember = propToken;

        // Determine which type of accessor we are dealing with.
        switch (uSemantic)
        {
        case msGetter:
        {
            // INVOKE_PROPERTYGET
            binderFlags |= BINDER_GetProperty;
            break;
        }

        case msSetter:
        {
            // INVOKE_PROPERTYPUT or INVOKE_PROPERTYPUTREF
            ULONG cAssoc;
            ASSOCIATE_RECORD* pAssoc;

            IMDInternalImport *pMDImport = pItfMT->GetMDImport();

            // Retrieve all the associates.
            HENUMInternalHolder henum{ pMDImport };
            henum.EnumAssociateInit(propToken);

            cAssoc = henum.EnumGetCount();
            _ASSERTE(cAssoc > 0);

            ULONG allocSize = cAssoc * sizeof(*pAssoc);
            if (allocSize < cAssoc)
                COMPlusThrowHR(COR_E_OVERFLOW);

            pAssoc = (ASSOCIATE_RECORD*)_alloca((size_t)allocSize);
            IfFailThrow(pMDImport->GetAllAssociates(&henum, pAssoc, cAssoc));

            // Check to see if there is both a set and an other. If this is the case
            // then the setter is a INVOKE_PROPERTYPUTREF otherwise we will make it a
            // INVOKE_PROPERTYPUT | INVOKE_PROPERTYPUTREF.
            bool propHasOther = false;
            for (ULONG i = 0; i < cAssoc; i++)
            {
                if (pAssoc[i].m_dwSemantics == msOther)
                {
                    propHasOther = true;
                    break;
                }
            }

            if (propHasOther)
            {
                // There is both a INVOKE_PROPERTYPUT and a INVOKE_PROPERTYPUTREF for this
                // property. Therefore be specific and make this invoke a INVOKE_PROPERTYPUTREF.
                binderFlags |= BINDER_PutRefDispProperty;
            }
            else
            {
                // Only a setter so make the invoke a set which maps to
                // INVOKE_PROPERTYPUT | INVOKE_PROPERTYPUTREF.
                binderFlags = BINDER_SetProperty;
            }
            break;
        }

        case msOther:
        {
            // INVOKE_PROPERTYPUT
            binderFlags |= BINDER_PutDispProperty;
            break;
        }

        default:
        {
            _ASSERTE(!"Invalid method semantic!");
        }
        }
    }

    // If the method has a void return type, then set the IgnoreReturn binding flag.
    if (pItfMD->IsVoid())
        binderFlags |= BINDER_IgnoreReturn;

    UINT32 fpRetSize = 0;

    struct
    {
        OBJECTREF MemberName;
        OBJECTREF ItfTypeObj;
        PTRARRAYREF Args;
        BOOLARRAYREF ArgsIsByRef;
        PTRARRAYREF ArgsTypes;
        OBJECTREF ArgsWrapperTypes;
        OBJECTREF RetValType;
        OBJECTREF RetVal;
    } gc;
    ZeroMemory(&gc, sizeof(gc));
    GCPROTECT_BEGIN(gc);
    {
        // Retrieve the exposed type object for the interface.
        gc.ItfTypeObj = pItfMT->GetManagedClassObject();

        // Retrieve the name of the target member. If the member
        // has a DISPID then use that to optimize the invoke.
        DISPID dispId = DISPID_UNKNOWN;
        hr = pItfMD->GetMDImport()->GetDispIdOfMemberDef(tkMember, (ULONG*)&dispId);
        if (hr == S_OK)
        {
            WCHAR strTmp[ARRAY_SIZE(DISPID_NAME_FORMAT_STRING) + MaxUnsigned32BitDecString];
            _snwprintf_s(strTmp, ARRAY_SIZE(strTmp), _TRUNCATE, DISPID_NAME_FORMAT_STRING, dispId);
            gc.MemberName = StringObject::NewString(strTmp);
        }
        else
        {
            gc.MemberName = StringObject::NewString(strMemberName);
        }

        CallsiteDetails callsite = CreateCallsiteDetails(pFrame);

        // Arguments
        CallsiteInspect::GetCallsiteArgs(callsite, &gc.Args, &gc.ArgsIsByRef, &gc.ArgsTypes);

        // If call requires object wrapping, set up the array of wrapper types.
        if (pMD->RequiresArgumentWrapping())
            gc.ArgsWrapperTypes = SetUpWrapperInfo(pItfMD);

        // Return type
        TypeHandle retValHandle = callsite.MetaSig.GetRetTypeHandleThrowing();
        gc.RetValType = retValHandle.GetManagedClassObject();

        // the return value is written into the Frame's neginfo, so we don't
        // need to return it directly. We can just have the stub do that work.
        // However, the stub needs to know what type of FP return this is, if
        // any, so we return the return size info as the return value.
        if (callsite.MetaSig.HasFPReturn())
        {
            callsite.MetaSig.Reset();
            ArgIterator argit{ &callsite.MetaSig };
            fpRetSize = argit.GetFPReturnSize();
            _ASSERTE(fpRetSize > 0);
        }

        // Create a call site for the invoke
        MethodDescCallSite forwardCallToInvoke(METHOD__CLASS__FORWARD_CALL_TO_INVOKE, &gc.ItfTypeObj);

        // Prepare the arguments that will be passed to the method.
        ARG_SLOT invokeArgs[] =
        {
            ObjToArgSlot(gc.ItfTypeObj),
            ObjToArgSlot(gc.MemberName),
            (ARG_SLOT)binderFlags,
            ObjToArgSlot(pFrame->GetThis()),
            ObjToArgSlot(gc.Args),
            ObjToArgSlot(gc.ArgsIsByRef),
            ObjToArgSlot(gc.ArgsWrapperTypes),
            ObjToArgSlot(gc.ArgsTypes),
            ObjToArgSlot(gc.RetValType)
        };

        // Invoke the method
        gc.RetVal = forwardCallToInvoke.CallWithValueTypes_RetOBJECTREF(invokeArgs);

        // Ensure all outs and return values are moved back to the current callsite
        CallsiteInspect::PropagateOutParametersBackToCallsite(gc.Args, gc.RetVal, callsite);
    }
    GCPROTECT_END();

    return fpRetSize;
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
    else if (pItfMT->GetComInterfaceType() == ifDispatch)
    {
        // If the interface is a Dispatch only interface then convert the early bound
        // call to a late bound call.
        returnValue = CLRToCOMLateBoundWorker(pFrame, pMD);
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

#endif // #ifndef DACCESS_COMPILE

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

    Thread* thread = GetThreadNULLOk();
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

#if defined(HOST_64BIT)
    // Interop debugging is currently not supported on WIN64, so we always return FALSE.
    // The result is that you can't step into an unmanaged frame or step out to one.  You
    // also can't step a breakpoint in one.
    return FALSE;
#endif // HOST_64BIT

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

#ifdef TARGET_X86

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
        size_t thunkSize = (numStackBytes == 0) ? 1 : 3;
        pRetThunk = (LPVOID)dummyAmTracker.Track(SystemDomain::GetGlobalLoaderAllocator()->GetExecutableHeap()->AllocMem(S_SIZE_T(thunkSize)));

        ExecutableWriterHolder<BYTE> thunkWriterHolder((BYTE *)pRetThunk, thunkSize);
        BYTE *pThunkRW = thunkWriterHolder.GetRW();

        if (numStackBytes == 0)
        {
            pThunkRW[0] = 0xc3;
        }
        else
        {
            pThunkRW[0] = 0xc2;
            *(USHORT *)&pThunkRW[1] = (USHORT)numStackBytes;
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

#endif // TARGET_X86
