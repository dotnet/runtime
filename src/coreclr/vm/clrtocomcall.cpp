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
#include "ilstubresolver.h"

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
    COR_ILMETHOD_DECODER* CreateEventCallIL(MethodDesc* pMD, ILStubResolver* pResolver)
    {
        STANDARD_VM_CONTRACT;

        _ASSERTE(pMD->IsCLRToCOMCall());

        CLRToCOMCallInfo* pComInfo = CLRToCOMCallInfo::FromMethodDesc(pMD);

        _ASSERTE(pComInfo->m_pEventProviderMD != NULL);

        MethodDesc *pEvProvMD = pComInfo->m_pEventProviderMD;
        MethodTable *pEvProvMT = pEvProvMD->GetMethodTable();

        SigTypeContext typeContext;
        ILStubLinker stubLinker(pMD->GetModule(), pMD->GetSignature(), &typeContext, pEvProvMD, ILStubLinkerFlags::ILSTUB_LINKER_FLAG_STUB_HAS_THIS);

        ILCodeStream* pCode = stubLinker.NewCodeStream(ILStubLinker::kDispatch);

        pCode->EmitLoadThis();
        pCode->EmitLDTOKEN(pCode->GetToken(pEvProvMT));
        pCode->EmitCALL(METHOD__TYPE__GET_TYPE_FROM_HANDLE, 1, 1);
        pCode->EmitCALL(METHOD__COM_OBJECT__GET_EVENT_PROVIDER, 2, 1);
        pCode->EmitLDARG(0);
        pCode->EmitCALL(pCode->GetToken(pEvProvMD), 2, 1);
        pCode->EmitRET();

        return pResolver->FinalizeILStub(&stubLinker);
    }
}

COR_ILMETHOD_DECODER* CLRToCOMCall::CreateCLRToCOMCallMethodIL(MethodDesc* pMD, DynamicResolver** ppResolver)
{
    STANDARD_VM_CONTRACT;

    _ASSERTE(pMD != NULL);
    _ASSERTE(pMD->IsCLRToCOMCall());
    _ASSERTE(ppResolver != NULL);

    DWORD dwStubFlags;
    CLRToCOMCall::PopulateCLRToCOMCallMethodDesc(pMD, &dwStubFlags);

    // The generated code always uses COM, so make sure that it is started.
    EnsureComStarted();

    NewHolder<ILStubResolver> pResolver = new ILStubResolver();
    pResolver->SetStubMethodDesc(pMD);

    COR_ILMETHOD_DECODER* pIL;

    // COM event stubs are very simple and don't go through any marshalling logic.
    if (SF_IsCOMEventCallStub(dwStubFlags))
    {
        ((CLRToCOMCallMethodDesc *)pMD)->InitComEventCallInfo();
        pIL = CreateEventCallIL(pMD, pResolver);
    }
    else
    {
        pIL = PInvoke::CreateCLRToCOMMarshallingIL(pMD, dwStubFlags, pResolver);
    }

    *ppResolver = pResolver.Extract();
    return pIL;
}

MethodDesc* CLRToCOMCall::GetPredefinedILStubMethod(MethodDesc* pMD)
{
    STANDARD_VM_CONTRACT;

    _ASSERTE(pMD->IsCLRToCOMCall());

    DWORD dwStubFlags;
    CLRToCOMCall::PopulateCLRToCOMCallMethodDesc(pMD, &dwStubFlags);

    // Predefined IL stubs are never used for COM event calls.
    if (SF_IsCOMEventCallStub(dwStubFlags))
        return NULL;

    MethodDesc* pStubMD = NULL;
    if (FAILED(FindPredefinedILStubMethod(pMD, dwStubFlags, &pStubMD)))
        return NULL;

    // We are about to execute the method in pStubMD which could be in another module.
    // Call EnsureActive before making the call.
    pStubMD->EnsureActive();

    // The generated code always uses COM, so make sure that it is started.
    EnsureComStarted();

    return pStubMD;
}
