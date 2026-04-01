// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
// File: CLRtoCOMCall.cpp
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
#include "method.hpp"

CLRToCOMCallInfo *CLRToCOMCall::PopulateCLRToCOMCallMethodDesc(MethodDesc* pMD, DWORD* pdwStubFlags)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(CheckPointer(pMD));
        PRECONDITION(CheckPointer(pdwStubFlags, NULL_OK));
    }
    CONTRACTL_END;

    MethodTable *pMT = pMD->GetMethodTable();
    MethodTable *pItfMT = NULL;

    if (pMD->IsCLRToCOMCall())
    {
        CLRToCOMCallMethodDesc *pCMD = (CLRToCOMCallMethodDesc *)pMD;
        if (pCMD->m_pCLRToCOMCallInfo == NULL)
        {
            LoaderHeap *pHeap = pMD->GetLoaderAllocator()->GetHighFrequencyHeap();
            CLRToCOMCallInfo *pTemp = (CLRToCOMCallInfo *)(void *)pHeap->AllocMem(S_SIZE_T(sizeof(CLRToCOMCallInfo)));

#ifdef TARGET_X86
            pTemp->InitStackArgumentSize();
#endif // TARGET_X86

            InterlockedCompareExchangeT(&pCMD->m_pCLRToCOMCallInfo, pTemp, NULL);
        }
    }

    CLRToCOMCallInfo *pComInfo = CLRToCOMCallInfo::FromMethodDesc(pMD);
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
    // Compute PInvokeStubFlags
    //

    DWORD dwStubFlags = PINVOKESTUB_FL_COM;

    // Determine if this is a special COM event call.
    BOOL fComEventCall = pItfMT->IsComEventItfType();

    // Determine if the call needs to do early bound to late bound conversion.
    BOOL fLateBound = !fComEventCall && pItfMT->IsInterface() && pItfMT->GetComInterfaceType() == ifDispatch;

    if (fLateBound)
        dwStubFlags |= PINVOKESTUB_FL_COMLATEBOUND;

    if (fComEventCall)
        dwStubFlags |= PINVOKESTUB_FL_COMEVENTCALL;

    BOOL BestFit = TRUE;
    BOOL ThrowOnUnmappableChar = FALSE;

    ReadBestFitCustomAttribute(pMD, &BestFit, &ThrowOnUnmappableChar);

    if (BestFit)
        dwStubFlags |= PINVOKESTUB_FL_BESTFIT;

    if (ThrowOnUnmappableChar)
        dwStubFlags |= PINVOKESTUB_FL_THROWONUNMAPPABLECHAR;

    //
    // fill in out param
    //
    *pdwStubFlags = dwStubFlags;

    return pComInfo;
}

namespace
{
    MethodDesc* CreateEventCallStub(MethodDesc* pMD)
    {
        STANDARD_VM_CONTRACT;

        _ASSERTE(pMD->IsCLRToCOMCall());

        CLRToCOMCallInfo* pComInfo = CLRToCOMCallInfo::FromMethodDesc(pMD);

        _ASSERTE(pComInfo->m_pEventProviderMD != NULL);

        MethodDesc *pEvProvMD = pComInfo->m_pEventProviderMD;
        MethodTable *pEvProvMT = pEvProvMD->GetMethodTable();

        FunctionSigBuilder sigBuilder;
        sigBuilder.SetCallingConv((CorCallingConvention)IMAGE_CEE_CS_CALLCONV_DEFAULT_HASTHIS);

        LocalDesc obj(ELEMENT_TYPE_OBJECT);
        sigBuilder.NewArg(&obj);

        MetaSig sig(pEvProvMD);
        LocalDesc retType(sig.GetRetTypeHandleThrowing());
        sigBuilder.SetReturnType(&retType);

        DWORD cbMetaSigSize = sigBuilder.GetSigSize();
        AllocMemHolder<BYTE> szMetaSig(pMD->GetMethodTable()->GetLoaderAllocator()->GetHighFrequencyHeap()->AllocMem(S_SIZE_T(cbMetaSigSize)));
        sigBuilder.GetSig(szMetaSig, cbMetaSigSize);

        Signature signature(szMetaSig, cbMetaSigSize);
        SigTypeContext typeContext;

        ILStubLinker stubLinker(pMD->GetModule(), signature, &typeContext, pEvProvMD, ILStubLinkerFlags::ILSTUB_LINKER_FLAG_STUB_HAS_THIS);

        ILCodeStream* pCode = stubLinker.NewCodeStream(ILStubLinker::kDispatch);

        pCode->EmitLoadThis();
        pCode->EmitLDTOKEN(pCode->GetToken(pEvProvMT));
        pCode->EmitCALL(METHOD__TYPE__GET_TYPE_FROM_HANDLE, 1, 1);
        pCode->EmitCALL(METHOD__COM_OBJECT__GET_EVENT_PROVIDER, 2, 1);
        pCode->EmitLDARG(0);
        pCode->EmitCALL(pCode->GetToken(pEvProvMD), 2, 1);
        pCode->EmitRET();

        MethodDesc* pStubMD = ILStubCache::CreateAndLinkNewILStubMethodDesc(
            pMD->GetLoaderAllocator(),
            pMD->GetMethodTable(),
            PINVOKESTUB_FL_COMEVENTCALL,
            pMD->GetModule(),
            szMetaSig,
            cbMetaSigSize,
            &typeContext,
            &stubLinker
        );

#if defined(FEATURE_DYNAMIC_METHOD_HAS_NATIVE_STACK_ARG_SIZE)
        if (pStubMD->IsDynamicMethod())
        {
            DynamicMethodDesc* pDMD = pStubMD->AsDynamicMethodDesc();
            pDMD->SetNativeStackArgSize(2 * TARGET_POINTER_SIZE); // The native stack arg size is constant since the signature for struct stubs is constant.
        }
#endif // FEATURE_DYNAMIC_METHOD_HAS_NATIVE_STACK_ARG_SIZE

        szMetaSig.SuppressRelease();

        return pStubMD;
    }

    MethodDesc* GetILStubMethodDesc(MethodDesc* pMD, DWORD dwStubFlags)
    {
        STANDARD_VM_CONTRACT;

        // COM event stubs are very simple and don't go through any marshalling logic.
        // We generate them as a regular IL stub outside of the P/Invoke system.
        if (SF_IsCOMEventCallStub(dwStubFlags))
        {
            _ASSERTE(pMD->IsCLRToCOMCall()); //  no generic COM eventing
            ((CLRToCOMCallMethodDesc *)pMD)->InitComEventCallInfo();
            return CreateEventCallStub(pMD);
        }

        // Get the call signature information
        StubSigDesc sigDesc(pMD);

        return PInvoke::CreateCLRToNativeILStub(
                        &sigDesc,
                        (CorNativeLinkType)0,
                        (CorNativeLinkFlags)0,
                        CallConv::GetDefaultUnmanagedCallingConvention(),
                        dwStubFlags);
    }
}

PCODE CLRToCOMCall::GetStubForILStub(MethodDesc* pMD, MethodDesc** ppStubMD)
{
    STANDARD_VM_CONTRACT;

    _ASSERTE(pMD->IsCLRToCOMCall());
    _ASSERTE(*ppStubMD == NULL);

    DWORD dwStubFlags;
    CLRToCOMCallInfo* pComInfo = CLRToCOMCall::PopulateCLRToCOMCallMethodDesc(pMD, &dwStubFlags);

    *ppStubMD = GetILStubMethodDesc(pMD, dwStubFlags);

    PCODE pCode = JitILStub(*ppStubMD);
    InterlockedCompareExchangeT<PCODE>(pComInfo->GetAddrOfILStubField(), pCode, NULL);

    return *pComInfo->GetAddrOfILStubField();
}
