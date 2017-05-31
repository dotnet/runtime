// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// 
// File: DllImport.cpp
//

//
// P/Invoke support.
// 


#include "common.h"

#include "vars.hpp"
#include "stublink.h"
#include "threads.h"
#include "excep.h"
#include "dllimport.h"
#include "method.hpp"
#include "siginfo.hpp"
#include "security.h"
#include "comdelegate.h"
#include "ceeload.h"
#include "mlinfo.h"
#include "eeconfig.h"
#include "comutilnative.h"
#include "corhost.h"
#include "asmconstants.h"
#include "mdaassistants.h"
#include "customattribute.h"
#include "ilstubcache.h"
#include "typeparse.h"
#include "sigbuilder.h"
#include "sigformat.h"
#include "strongnameholders.h"
#include "ecall.h"

#include <formattype.h>
#include "../md/compiler/custattr.h"

#ifdef FEATURE_COMINTEROP
#include "runtimecallablewrapper.h"
#include "clrtocomcall.h"
#endif // FEATURE_COMINTEROP

#ifdef FEATURE_PREJIT
#include "compile.h"
#endif // FEATURE_PREJIT

#include "eventtrace.h"


#include "clr/fs/path.h"
using namespace clr::fs;

// remove when we get an updated SDK
#define LOAD_LIBRARY_SEARCH_DLL_LOAD_DIR 0x00000100
#define LOAD_LIBRARY_SEARCH_DEFAULT_DIRS 0x00001000

void AppendEHClause(int nClauses, COR_ILMETHOD_SECT_EH * pEHSect, ILStubEHClause * pClause, int * pCurIdx)
{
    LIMITED_METHOD_CONTRACT;
    if (pClause->kind == ILStubEHClause::kNone)
        return;

    int idx = *pCurIdx;
    *pCurIdx = idx + 1;

    CorExceptionFlag flags;
    switch (pClause->kind)
    {
    case ILStubEHClause::kFinally: flags = COR_ILEXCEPTION_CLAUSE_FINALLY; break;
    case ILStubEHClause::kTypedCatch: flags = COR_ILEXCEPTION_CLAUSE_NONE; break;
    default:
        UNREACHABLE_MSG("unexpected ILStubEHClause kind");
    }
    _ASSERTE(idx < nClauses);
    pEHSect->Fat.Clauses[idx].Flags = flags;
    pEHSect->Fat.Clauses[idx].TryOffset = pClause->dwTryBeginOffset;
    pEHSect->Fat.Clauses[idx].TryLength = pClause->cbTryLength;
    pEHSect->Fat.Clauses[idx].HandlerOffset = pClause->dwHandlerBeginOffset;
    pEHSect->Fat.Clauses[idx].HandlerLength = pClause->cbHandlerLength;
    pEHSect->Fat.Clauses[idx].ClassToken = pClause->dwTypeToken;
}

VOID PopulateEHSect(COR_ILMETHOD_SECT_EH * pEHSect, int nClauses, ILStubEHClause * pOne, ILStubEHClause * pTwo)
{
    LIMITED_METHOD_CONTRACT;
    pEHSect->Fat.Kind       = (CorILMethod_Sect_EHTable | CorILMethod_Sect_FatFormat);
    pEHSect->Fat.DataSize   = COR_ILMETHOD_SECT_EH_FAT::Size(nClauses);

    int curIdx = 0;
    AppendEHClause(nClauses, pEHSect, pOne, &curIdx);
    AppendEHClause(nClauses, pEHSect, pTwo, &curIdx);
}

StubSigDesc::StubSigDesc(MethodDesc *pMD, PInvokeStaticSigInfo* pSigInfo /*= NULL*/)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        SUPPORTS_DAC;
    }
    CONTRACTL_END

    m_pMD = pMD;
    if (pSigInfo != NULL)
    {
        m_sig           = pSigInfo->GetSignature();
        m_pModule       = pSigInfo->GetModule();
    }
    else
    {
        _ASSERTE(pMD != NULL);
        m_sig           = pMD->GetSignature();
        m_pModule       = pMD->GetModule();         // Used for token resolution.
    }

    if (pMD != NULL)
    {
        m_tkMethodDef = pMD->GetMemberDef();
        SigTypeContext::InitTypeContext(pMD, &m_typeContext);
        m_pLoaderModule = pMD->GetLoaderModule();   // Used for ILStubCache selection and MethodTable creation.
    }
    else
    {
        m_tkMethodDef = mdMethodDefNil;
        m_pLoaderModule = m_pModule;
    }

    INDEBUG(InitDebugNames());
}

StubSigDesc::StubSigDesc(MethodDesc *pMD, Signature sig, Module *pModule)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        SUPPORTS_DAC;
        PRECONDITION(!sig.IsEmpty());
        PRECONDITION(pModule != NULL);
    }
    CONTRACTL_END

    m_pMD           = pMD;
    m_sig           = sig;
    m_pModule       = pModule;

    if (pMD != NULL)
    {
        m_tkMethodDef = pMD->GetMemberDef();
        SigTypeContext::InitTypeContext(pMD, &m_typeContext);
        m_pLoaderModule = pMD->GetLoaderModule();   // Used for ILStubCache selection and MethodTable creation.
    }
    else
    {
        m_tkMethodDef = mdMethodDefNil;
        m_pLoaderModule = m_pModule;
    }

    INDEBUG(InitDebugNames());
}

#ifndef DACCESS_COMPILE

class StubState
{
public:
    virtual void SetLastError(BOOL fSetLastError) = 0;
    virtual void BeginEmit(DWORD dwStubFlags) = 0;
    virtual void MarshalReturn(MarshalInfo* pInfo, int argOffset) = 0;
    virtual void MarshalArgument(MarshalInfo* pInfo, int argOffset, UINT nativeStackOffset) = 0;
    virtual void MarshalLCID(int argIdx) = 0;

#ifdef FEATURE_COMINTEROP
    virtual void MarshalHiddenLengthArgument(MarshalInfo *pInfo, BOOL isForReturnArray) = 0;
    virtual void MarshalFactoryReturn() = 0;
#endif // FEATURE_COMINTEROP

    virtual void EmitInvokeTarget(MethodDesc *pStubMD) = 0;

    virtual void FinishEmit(MethodDesc* pMD) = 0;

    virtual ~StubState()
    {
        LIMITED_METHOD_CONTRACT;
    }
};

class ILStubState : public StubState
{
protected:
    
    ILStubState(
                Module* pStubModule,
                const Signature &signature,
                SigTypeContext* pTypeContext,
                BOOL fTargetHasThis,
                BOOL fStubHasThis,
                DWORD dwStubFlags,
                int iLCIDParamIdx,
                MethodDesc* pTargetMD)
            : m_slIL(dwStubFlags, pStubModule, signature, pTypeContext, pTargetMD, iLCIDParamIdx, fTargetHasThis, fStubHasThis)
    {
        STANDARD_VM_CONTRACT;

        m_fSetLastError = 0;
    }

public:
    void SetLastError(BOOL fSetLastError)
    {
        LIMITED_METHOD_CONTRACT;

        m_fSetLastError = fSetLastError;
    }

    // We use three stub linkers to generate IL stubs.  The pre linker is the main one.  It does all the marshaling and
    // then calls the target method.  The post return linker is only used to unmarshal the return value after we return
    // from the target method.  The post linker handles all the unmarshaling for by ref arguments and clean-up.  It
    // also checks if we should throw an exception etc.
    //
    // Currently, we have two "emittable" ILCodeLabel's.  The first one is at the beginning of the pre linker.  This
    // label is used to emit code to declare and initialize clean-up flags.  Each argument which requires clean-up 
    // emits one flag.  This flag is set only after the marshaling is done, and it is checked before we do any clean-up
    // in the finally.
    //
    // The second "emittable" ILCodeLabel is at the beginning of the post linker.  It is used to emit code which is
    // not safe to run in the case of an exception.  The rest of the post linker is wrapped in a finally, and it contains
    // with the necessary clean-up which should be executed in both normal and exception cases.
    void BeginEmit(DWORD dwStubFlags)
    {
        WRAPPER_NO_CONTRACT;
        m_slIL.Begin(dwStubFlags);
        m_dwStubFlags = dwStubFlags;
    }

    void MarshalReturn(MarshalInfo* pInfo, int argOffset)
    {
        CONTRACTL
        {
            STANDARD_VM_CHECK;

            PRECONDITION(CheckPointer(pInfo));
        }
        CONTRACTL_END;

        pInfo->GenerateReturnIL(&m_slIL, argOffset,
                                SF_IsForwardStub(m_dwStubFlags),
                                SF_IsFieldGetterStub(m_dwStubFlags),
                                SF_IsHRESULTSwapping(m_dwStubFlags));
    }

    void MarshalArgument(MarshalInfo* pInfo, int argOffset, UINT nativeStackOffset)
    {
        CONTRACTL
        {
            STANDARD_VM_CHECK;
            PRECONDITION(CheckPointer(pInfo));
        }
        CONTRACTL_END;

        pInfo->GenerateArgumentIL(&m_slIL, argOffset, nativeStackOffset, SF_IsForwardStub(m_dwStubFlags));
    }

#ifdef FEATURE_COMINTEROP
    // Marshal the hidden length parameter for the managed parameter in pInfo
    virtual void MarshalHiddenLengthArgument(MarshalInfo *pInfo, BOOL isForReturnArray)
    {
        STANDARD_VM_CONTRACT;

        pInfo->MarshalHiddenLengthArgument(&m_slIL, SF_IsForwardStub(m_dwStubFlags), isForReturnArray);

        if (SF_IsReverseStub(m_dwStubFlags))
        {
            // Hidden length arguments appear explicitly in the native signature
            // however, they are not in the managed signature.
            m_slIL.AdjustTargetStackDeltaForExtraParam();
        }
    }

    void MarshalFactoryReturn()
    {
        CONTRACTL
        {
            STANDARD_VM_CHECK;
            PRECONDITION(SF_IsCOMStub(m_dwStubFlags));
            PRECONDITION(SF_IsWinRTCtorStub(m_dwStubFlags));
        }
        CONTRACTL_END;

        ILCodeStream *pcsSetup     = m_slIL.GetSetupCodeStream();
        ILCodeStream *pcsDispatch  = m_slIL.GetDispatchCodeStream();
        ILCodeStream *pcsUnmarshal = m_slIL.GetReturnUnmarshalCodeStream();
        ILCodeStream *pcsCleanup   = m_slIL.GetCleanupCodeStream();

        /*
        *    SETUP
        */

        // create a local to hold the returned pUnk and initialize to 0 in case the factory fails
        // and we try to release it during cleanup
        LocalDesc locDescFactoryRetVal(ELEMENT_TYPE_I);
        DWORD dwFactoryRetValLocalNum = pcsSetup->NewLocal(locDescFactoryRetVal);
        pcsSetup->EmitLoadNullPtr();
        pcsSetup->EmitSTLOC(dwFactoryRetValLocalNum);

        DWORD dwInnerIInspectableLocalNum = -1;
        DWORD dwOuterIInspectableLocalNum = -1;
        if (SF_IsWinRTCompositionStub(m_dwStubFlags))
        {
            // Create locals to store the outer and inner IInspectable values and initialize to null
            // Note that we do this in the setup stream so that we're guaranteed to have a null-initialized
            // value in the cleanup stream
            LocalDesc locDescOuterIInspectable(ELEMENT_TYPE_I);
            dwOuterIInspectableLocalNum = pcsSetup->NewLocal(locDescOuterIInspectable);
            pcsSetup->EmitLoadNullPtr();
            pcsSetup->EmitSTLOC(dwOuterIInspectableLocalNum);
            LocalDesc locDescInnerIInspectable(ELEMENT_TYPE_I);
            dwInnerIInspectableLocalNum = pcsSetup->NewLocal(locDescInnerIInspectable);
            pcsSetup->EmitLoadNullPtr();
            pcsSetup->EmitSTLOC(dwInnerIInspectableLocalNum);
        }

        /*
        *   DISPATCH
        */

        // For composition factories, add the two extra params
        if (SF_IsWinRTCompositionStub(m_dwStubFlags))
        {
            // Get outer IInspectable. The helper will return NULL if this is the "top-level" constructor,
            // and the appropriate outer pointer otherwise.
            pcsDispatch->EmitLoadThis();
            m_slIL.EmitLoadStubContext(pcsDispatch, m_dwStubFlags);
            pcsDispatch->EmitCALL(METHOD__STUBHELPERS__GET_OUTER_INSPECTABLE, 2, 1);
            pcsDispatch->EmitSTLOC(dwOuterIInspectableLocalNum);

            // load the outer IInspectable (3rd last argument)
            pcsDispatch->SetStubTargetArgType(ELEMENT_TYPE_I, false);
            pcsDispatch->EmitLDLOC(dwOuterIInspectableLocalNum);

            // pass pointer to where inner non-delegating IInspectable should be stored (2nd last argument)
            LocalDesc locDescInnerPtr(ELEMENT_TYPE_I);
            locDescInnerPtr.MakeByRef();
            pcsDispatch->SetStubTargetArgType(&locDescInnerPtr, false);
            pcsDispatch->EmitLDLOCA(dwInnerIInspectableLocalNum);
        }

        // pass pointer to the local to the factory method (last argument)
        locDescFactoryRetVal.MakeByRef();
        pcsDispatch->SetStubTargetArgType(&locDescFactoryRetVal, false);
        pcsDispatch->EmitLDLOCA(dwFactoryRetValLocalNum);

        /*
        *   UNMARSHAL
        */

        // Mark that the factory method has succesfully returned and so cleanup will be necessary after
        // this point.
        m_slIL.EmitSetArgMarshalIndex(pcsUnmarshal, NDirectStubLinker::CLEANUP_INDEX_RETVAL_UNMARSHAL);

        // associate the 'this' RCW with one of the returned interface pointers 
        pcsUnmarshal->EmitLoadThis();

        // now we need to find the right interface pointer to load
        if (dwInnerIInspectableLocalNum != -1)
        {
            // We may have a composition scenario
            ILCodeLabel* pNonCompositionLabel = pcsUnmarshal->NewCodeLabel();
            ILCodeLabel* pLoadedLabel = pcsUnmarshal->NewCodeLabel();

            // Did we pass an outer IInspectable?
            pcsUnmarshal->EmitLDLOC(dwOuterIInspectableLocalNum);
            pcsUnmarshal->EmitBRFALSE(pNonCompositionLabel);

            // yes, this is a composition scenario 
            {
                // ignore the delegating interface pointer (will be released in cleanup below) - we can 
                // re-create it by QI'ing the non-delegating one.
                // Note that using this could be useful in the future (avoids an extra QueryInterface call)
                // Just load the non-delegating interface pointer
                pcsUnmarshal->EmitLDLOCA(dwInnerIInspectableLocalNum);
                pcsUnmarshal->EmitBR(pLoadedLabel);
            }
            // else, no this is a non-composition scenario
            {
                pcsUnmarshal->EmitLabel(pNonCompositionLabel);

                // ignore the non-delegating interface pointer (which should be null, but will regardless get
                // cleaned up below in the event the factory doesn't follow the pattern properly).
                // Just load the regular delegating interface pointer
                pcsUnmarshal->EmitLDLOCA(dwFactoryRetValLocalNum);
            }

            pcsUnmarshal->EmitLabel(pLoadedLabel);
        }
        else
        {
            // Definitely can't be a composition scenario - use the only pointer we have
            pcsUnmarshal->EmitLDLOCA(dwFactoryRetValLocalNum);
        }

        pcsUnmarshal->EmitCALL(METHOD__MARSHAL__INITIALIZE_WRAPPER_FOR_WINRT, 2, 0);

        /*
        *   CLEANUP 
        */

        // release the returned interface pointer in the finally block
        m_slIL.SetCleanupNeeded();

        ILCodeLabel *pSkipCleanupLabel = pcsCleanup->NewCodeLabel();

        m_slIL.EmitCheckForArgCleanup(pcsCleanup,
                                      NDirectStubLinker::CLEANUP_INDEX_RETVAL_UNMARSHAL,
                                      NDirectStubLinker::BranchIfNotMarshaled,
                                      pSkipCleanupLabel);

        EmitInterfaceClearNative(pcsCleanup, dwFactoryRetValLocalNum);

        // Note that it's a no-op to pass NULL to Clear_Native, so we call it even though we don't 
        // know if we assigned to the inner/outer IInspectable
        if (dwInnerIInspectableLocalNum != -1)
        {
            EmitInterfaceClearNative(pcsCleanup, dwInnerIInspectableLocalNum);
        }
        if (dwOuterIInspectableLocalNum != -1)
        {
            EmitInterfaceClearNative(pcsCleanup, dwOuterIInspectableLocalNum);
        }

        pcsCleanup->EmitLabel(pSkipCleanupLabel);
    }

    static void EmitInterfaceClearNative(ILCodeStream* pcsEmit, DWORD dwLocalNum)
    {
        STANDARD_VM_CONTRACT;

        ILCodeLabel *pSkipClearNativeLabel = pcsEmit->NewCodeLabel();
        pcsEmit->EmitLDLOC(dwLocalNum);
        pcsEmit->EmitBRFALSE(pSkipClearNativeLabel);
        pcsEmit->EmitLDLOC(dwLocalNum);
        pcsEmit->EmitCALL(METHOD__INTERFACEMARSHALER__CLEAR_NATIVE, 1, 0);
        pcsEmit->EmitLabel(pSkipClearNativeLabel);
    }

#endif // FEATURE_COMINTEROP

    void MarshalLCID(int argIdx)
    {
        STANDARD_VM_CONTRACT;

        ILCodeStream* pcs = m_slIL.GetDispatchCodeStream();
        
#ifdef FEATURE_USE_LCID
        if (SF_IsReverseStub(m_dwStubFlags))
        {
            if ((m_slIL.GetStubTargetCallingConv() & IMAGE_CEE_CS_CALLCONV_HASTHIS) == IMAGE_CEE_CS_CALLCONV_HASTHIS)
            {
                // the arg number will be incremented by LDARG if we are in an instance method
                _ASSERTE(argIdx > 0);
                argIdx--;
            }

            LocalDesc locDescThread(MscorlibBinder::GetClass(CLASS__THREAD));
            DWORD dwThreadLocalNum = pcs->NewLocal(locDescThread);

            // call Thread.get_CurrentThread()
            pcs->EmitCALL(METHOD__THREAD__GET_CURRENT_THREAD, 0, 1);
            pcs->EmitDUP();
            pcs->EmitSTLOC(dwThreadLocalNum);

            // call current_thread.get_CurrentCulture()
            pcs->EmitCALL(METHOD__THREAD__GET_CULTURE, 1, 1);

            // save the current culture
            LocalDesc locDescCulture(MscorlibBinder::GetClass(CLASS__CULTURE_INFO));
            DWORD dwCultureLocalNum = pcs->NewLocal(locDescCulture);

            pcs->EmitSTLOC(dwCultureLocalNum);

            // set a new one based on the LCID passed from unmanaged
            pcs->EmitLDLOC(dwThreadLocalNum);
            pcs->EmitLDARG(argIdx);

            // call CultureInfo..ctor(lcid)
            // call current_thread.set_CurrentCulture(culture)
            pcs->EmitNEWOBJ(METHOD__CULTURE_INFO__INT_CTOR, 1);
            pcs->EmitCALL(METHOD__THREAD__SET_CULTURE, 2, 0);

            // and restore the current one after the call
            m_slIL.SetCleanupNeeded();
            ILCodeStream *pcsCleanup = m_slIL.GetCleanupCodeStream();

            // call current_thread.set_CurrentCulture(original_culture)
            pcsCleanup->EmitLDLOC(dwThreadLocalNum);
            pcsCleanup->EmitLDLOC(dwCultureLocalNum);
            pcsCleanup->EmitCALL(METHOD__THREAD__SET_CULTURE, 1, 1);

        }
        else
        {
            if (SF_IsCOMStub(m_dwStubFlags))
            {
                // We used to get LCID from current thread's culture here. The code
                // was replaced by the hardcoded LCID_ENGLISH_US as requested by VSTO.
                pcs->EmitLDC(0x0409); // LCID_ENGLISH_US
            }
            else
            {
                // call Thread.get_CurrentThread()
                // call current_thread.get_CurrentCulture()
                pcs->EmitCALL(METHOD__THREAD__GET_CURRENT_THREAD, 0, 1);
                pcs->EmitCALL(METHOD__THREAD__GET_CULTURE, 1, 1);

                //call CultureInfo.get_LCID(this)
                pcs->EmitCALL(METHOD__CULTURE_INFO__GET_ID, 1, 1);
            }
        }
#else // FEATURE_USE_LCID
        if (SF_IsForwardStub(m_dwStubFlags))
        {
            pcs->EmitLDC(0x0409); // LCID_ENGLISH_US
        }
#endif // FEATURE_USE_LCID

        // add the extra arg to the unmanaged signature
        LocalDesc locDescNative(ELEMENT_TYPE_I4);
        pcs->SetStubTargetArgType(&locDescNative, false);

        if (SF_IsReverseStub(m_dwStubFlags))
        {
            // reverse the effect of SetStubTargetArgType on the stack delta
            // (the LCID argument is explicitly passed from unmanaged but does not
            // show up in the managed signature in any way)
            m_slIL.AdjustTargetStackDeltaForExtraParam();
        }

    }

    void SwapStubSignatures(MethodDesc* pStubMD)
    {
        STANDARD_VM_CONTRACT;
        
        //
        // Since the stub handles native-to-managed transitions, we have to swap the
        // stub-state-calculated stub target sig with the stub sig itself.  This is 
        // because the stub target sig represents the native signature and the stub 
        // sig represents the managed signature.  
        //
        // The first step is to convert the managed signature to a module-independent
        // signature and then pass it off to SetStubTargetMethodSig.  Note that the
        // ILStubResolver will copy the sig, so we only need to make a temporary copy
        // of it.
        //
        SigBuilder sigBuilder;

        {
            SigPointer sigPtr(pStubMD->GetSig());
            sigPtr.ConvertToInternalSignature(pStubMD->GetModule(), NULL, &sigBuilder);
        }
        
        //
        // The second step is to reset the sig on the stub MethodDesc to be the 
        // stub-state-calculated stub target sig.
        //
        {
            //
            // make a domain-local copy of the sig so that this state can outlive the
            // compile time state.
            //
            DWORD           cbNewSig;
            PCCOR_SIGNATURE pNewSig;

            cbNewSig = GetStubTargetMethodSigLength();
            pNewSig  = (PCCOR_SIGNATURE)(void *)pStubMD->GetLoaderAllocator()->GetHighFrequencyHeap()->AllocMem(S_SIZE_T(cbNewSig));

            memcpyNoGCRefs((void *)pNewSig, GetStubTargetMethodSig(), cbNewSig);
            
            pStubMD->AsDynamicMethodDesc()->SetStoredMethodSig(pNewSig, cbNewSig);

            SigPointer  sigPtr(pNewSig, cbNewSig);
            ULONG       callConvInfo;
            IfFailThrow(sigPtr.GetCallingConvInfo(&callConvInfo));
            
            if (callConvInfo & CORINFO_CALLCONV_HASTHIS)
            {
                ((PTR_DynamicMethodDesc)pStubMD)->m_dwExtendedFlags &= ~mdStatic;
                pStubMD->ClearStatic();
            }
            else
            {
                ((PTR_DynamicMethodDesc)pStubMD)->m_dwExtendedFlags |= mdStatic;
                pStubMD->SetStatic();
            }

#ifndef _TARGET_X86_
            // we store the real managed argument stack size in the stub MethodDesc on non-X86
            UINT stackSize = pStubMD->SizeOfArgStack();

            if (!FitsInU2(stackSize))
                COMPlusThrow(kMarshalDirectiveException, IDS_EE_SIGTOOCOMPLEX);

            pStubMD->AsDynamicMethodDesc()->SetNativeStackArgSize(static_cast<WORD>(stackSize));
#endif // _TARGET_X86_
        }

        DWORD   cbTempModuleIndependentSigLength;
        BYTE *  pTempModuleIndependentSig = (BYTE *)sigBuilder.GetSignature(&cbTempModuleIndependentSigLength);

        // Finish it
        SetStubTargetMethodSig(pTempModuleIndependentSig, 
                               cbTempModuleIndependentSigLength);
    }

    void EmitInvokeTarget(MethodDesc *pStubMD)
    {
        STANDARD_VM_CONTRACT;

        m_slIL.DoNDirect(m_slIL.GetDispatchCodeStream(), m_dwStubFlags, pStubMD);
    }

#ifdef FEATURE_COMINTEROP
    void EmitExceptionHandler(LocalDesc* pNativeReturnType, LocalDesc* pManagedReturnType, 
        ILCodeLabel** ppTryEndAndCatchBeginLabel, ILCodeLabel ** ppCatchEndAndReturnLabel)
    {
        STANDARD_VM_CONTRACT;

        ILCodeStream* pcsExceptionHandler = m_slIL.NewCodeStream(ILStubLinker::kExceptionHandler);
        *ppTryEndAndCatchBeginLabel  = pcsExceptionHandler->NewCodeLabel();
        *ppCatchEndAndReturnLabel = pcsExceptionHandler->NewCodeLabel();

        pcsExceptionHandler->EmitLEAVE(*ppCatchEndAndReturnLabel);
        pcsExceptionHandler->EmitLabel(*ppTryEndAndCatchBeginLabel);

        BYTE nativeReturnElemType = pNativeReturnType->ElementType[0];      // return type of the stub
        BYTE managedReturnElemType = pManagedReturnType->ElementType[0];    // return type of the mananged target

        bool returnTheHRESULT = SF_IsHRESULTSwapping(m_dwStubFlags) || 
                                    (managedReturnElemType == ELEMENT_TYPE_I4) ||
                                    (managedReturnElemType == ELEMENT_TYPE_U4);

#ifdef MDA_SUPPORTED
        if (!returnTheHRESULT)
        {
            MdaExceptionSwallowedOnCallFromCom* mda = MDA_GET_ASSISTANT(ExceptionSwallowedOnCallFromCom);
            if (mda)
            {
                // on the stack: exception object, but the stub linker doesn't know it
                pcsExceptionHandler->EmitCALL(METHOD__STUBHELPERS__GET_STUB_CONTEXT, 0, 1);
                pcsExceptionHandler->EmitCALL(METHOD__STUBHELPERS__TRIGGER_EXCEPTION_SWALLOWED_MDA, 
                    1,  // WARNING: This method takes 2 input args, the exception object and the stub context.
                        //          But the ILStubLinker has no knowledge that the exception object is on the 
                        //          stack (because it is unaware that we've just entered a catch block), so we 
                        //          lie and claim that we only take one input argument.
                    1); // returns the exception object back
            }
        }
#endif // MDA_SUPPORTED

        DWORD retvalLocalNum = m_slIL.GetReturnValueLocalNum();
        BinderMethodID getHRForException;
        if (SF_IsWinRTStub(m_dwStubFlags))
        {
            getHRForException = METHOD__MARSHAL__GET_HR_FOR_EXCEPTION_WINRT;
        }
        else
        {
            getHRForException = METHOD__MARSHAL__GET_HR_FOR_EXCEPTION;
        }

        pcsExceptionHandler->EmitCALL(getHRForException, 
            0,  // WARNING: This method takes 1 input arg, the exception object.  But the ILStubLinker
                //          has no knowledge that the exception object is on the stack (because it is
                //          unaware that we've just entered a catch block), so we lie and claim that we
                //          don't take any input arguments.
            1);
        
        switch (nativeReturnElemType)
        {
        default:
            UNREACHABLE_MSG("Unexpected element type found on native return type.");
            break;
        case ELEMENT_TYPE_VOID:
            _ASSERTE(retvalLocalNum == (DWORD)-1);
            pcsExceptionHandler->EmitPOP();
            break;
        case ELEMENT_TYPE_I4:
        case ELEMENT_TYPE_U4:
            {
                if (!returnTheHRESULT)
                {
                    pcsExceptionHandler->EmitPOP();
                    pcsExceptionHandler->EmitLDC(0);
                    pcsExceptionHandler->EmitCONV_T((CorElementType)nativeReturnElemType);
                }
                _ASSERTE(retvalLocalNum != (DWORD)-1);
                pcsExceptionHandler->EmitSTLOC(retvalLocalNum);
            }
            break;
        case ELEMENT_TYPE_R4:
            pcsExceptionHandler->EmitPOP();
            pcsExceptionHandler->EmitLDC_R4(CLR_NAN_32);
            _ASSERTE(retvalLocalNum != (DWORD)-1);
            pcsExceptionHandler->EmitSTLOC(retvalLocalNum);
            break;
        case ELEMENT_TYPE_R8:
            pcsExceptionHandler->EmitPOP();
            pcsExceptionHandler->EmitLDC_R8(CLR_NAN_64);
            _ASSERTE(retvalLocalNum != (DWORD)-1);
            pcsExceptionHandler->EmitSTLOC(retvalLocalNum);
            break;
        case ELEMENT_TYPE_INTERNAL:
            {
                TypeHandle returnTypeHnd = pNativeReturnType->InternalToken;
                CONSISTENCY_CHECK(returnTypeHnd.IsValueType()); 
                _ASSERTE(retvalLocalNum != (DWORD)-1);
                pcsExceptionHandler->EmitLDLOCA(retvalLocalNum);
                pcsExceptionHandler->EmitINITOBJ(m_slIL.GetDispatchCodeStream()->GetToken(returnTypeHnd));
            }
            break;
        case ELEMENT_TYPE_BOOLEAN:
        case ELEMENT_TYPE_CHAR:
        case ELEMENT_TYPE_I1:
        case ELEMENT_TYPE_U1:
        case ELEMENT_TYPE_I2:
        case ELEMENT_TYPE_U2:
        case ELEMENT_TYPE_I8:
        case ELEMENT_TYPE_U8:
        case ELEMENT_TYPE_I:
        case ELEMENT_TYPE_U:
            pcsExceptionHandler->EmitPOP();
            pcsExceptionHandler->EmitLDC(0);
            pcsExceptionHandler->EmitCONV_T((CorElementType)nativeReturnElemType);
            _ASSERTE(retvalLocalNum != (DWORD)-1);
            pcsExceptionHandler->EmitSTLOC(retvalLocalNum);
            break;
        }

        pcsExceptionHandler->EmitLEAVE(*ppCatchEndAndReturnLabel);
        pcsExceptionHandler->EmitLabel(*ppCatchEndAndReturnLabel);
        if (nativeReturnElemType != ELEMENT_TYPE_VOID)
        {
            _ASSERTE(retvalLocalNum != (DWORD)-1);
            pcsExceptionHandler->EmitLDLOC(retvalLocalNum);
        }
        pcsExceptionHandler->EmitRET();
    }
#endif // FEATURE_COMINTEROP

    void FinishEmit(MethodDesc* pStubMD)
    {
        STANDARD_VM_CONTRACT;

        ILCodeStream* pcsMarshal = m_slIL.GetMarshalCodeStream();
        ILCodeStream* pcsUnmarshal = m_slIL.GetUnmarshalCodeStream();
        ILCodeStream* pcsDispatch = m_slIL.GetDispatchCodeStream();
       
        if (SF_IsHRESULTSwapping(m_dwStubFlags) && m_slIL.StubHasVoidReturnType())
        {
            // if the return type is void, but we're doing HRESULT swapping, we
            // need to set the return type here.  Otherwise, the return value
            // marshaler will do this.
            pcsMarshal->SetStubTargetReturnType(ELEMENT_TYPE_I4);    // HRESULT

            if (SF_IsReverseStub(m_dwStubFlags))
            {
                // reverse interop needs to seed the return value if the
                // managed function returns void but we're doing hresult
                // swapping.
                pcsUnmarshal->EmitLDC(S_OK);
            }
        }

        LocalDesc nativeReturnType;
        LocalDesc managedReturnType;
        bool hasTryCatchForHRESULT = SF_IsReverseCOMStub(m_dwStubFlags) 
                                    && !SF_IsFieldGetterStub(m_dwStubFlags) 
                                    && !SF_IsFieldSetterStub(m_dwStubFlags);

#ifdef FEATURE_COMINTEROP
        if (hasTryCatchForHRESULT)
        {
            m_slIL.GetStubTargetReturnType(&nativeReturnType);
            m_slIL.GetStubReturnType(&managedReturnType);
        }
#endif // FEATURE_COMINTEROP

        if (SF_IsHRESULTSwapping(m_dwStubFlags) && SF_IsReverseStub(m_dwStubFlags))
        {
            m_slIL.AdjustTargetStackDeltaForReverseInteropHRESULTSwapping();
        }

        if (SF_IsForwardCOMStub(m_dwStubFlags))
        {
            // Compensate for the 'this' parameter.
            m_slIL.AdjustTargetStackDeltaForExtraParam();
        }

#if defined(_TARGET_X86_)
        // unmanaged CALLI will get an extra arg with the real target address if host hook is enabled
        if (SF_IsCALLIStub(m_dwStubFlags) && NDirect::IsHostHookEnabled())
        {
            pcsMarshal->SetStubTargetArgType(ELEMENT_TYPE_I, false);
        }
#endif // _TARGET_X86_

        // Don't touch target signatures from this point on otherwise it messes up the
        // cache in ILStubState::GetStubTargetMethodSig.

#ifdef _DEBUG
        {
            // The native and local signatures should not have any tokens.
            // All token references should have been converted to
            // ELEMENT_TYPE_INTERNAL.
            //
            // Note that MetaSig::GetReturnType and NextArg will normalize
            // ELEMENT_TYPE_INTERNAL back to CLASS or VALUETYPE.
            //
            // <TODO> need to recursively check ELEMENT_TYPE_FNPTR signatures </TODO>

            SigTypeContext typeContext;  // this is an empty type context: COM calls are guaranteed to not be generics.
            MetaSig nsig(
                GetStubTargetMethodSig(), 
                GetStubTargetMethodSigLength(), 
                MscorlibBinder::GetModule(), 
                &typeContext);

            CorElementType type;
            IfFailThrow(nsig.GetReturnProps().PeekElemType(&type));
            CONSISTENCY_CHECK(ELEMENT_TYPE_CLASS != type && ELEMENT_TYPE_VALUETYPE != type);

            while (ELEMENT_TYPE_END != (type = nsig.NextArg()))
            {
                IfFailThrow(nsig.GetArgProps().PeekElemType(&type));
                CONSISTENCY_CHECK(ELEMENT_TYPE_CLASS != type && ELEMENT_TYPE_VALUETYPE != type);
            }
        }
#endif // _DEBUG

#ifdef FEATURE_COMINTEROP
        if (SF_IsForwardCOMStub(m_dwStubFlags))
        {
#if defined(MDA_SUPPORTED)
            // We won't use this NGEN'ed stub if RaceOnRCWCleanup is enabled at run-time
            if (!SF_IsNGENedStub(m_dwStubFlags))
            {
                // This code may change the type of the frame we use, so it has to be run before the code below where we
                // retrieve the stack arg size based on the frame type.
                MdaRaceOnRCWCleanup* mda = MDA_GET_ASSISTANT(RaceOnRCWCleanup);
                if (mda)
                {
                    // Here we have to register the RCW of the "this" object to the RCWStack and schedule the clean-up for it.
                    // Emit a call to StubHelpers::StubRegisterRCW() and StubHelpers::StubUnregisterRCW() to do this.
                    m_slIL.EmitLoadRCWThis(pcsMarshal, m_dwStubFlags);
                    pcsMarshal->EmitCALL(METHOD__STUBHELPERS__STUB_REGISTER_RCW, 1, 0);

                    // We use an extra local to track whether we need to unregister the RCW on cleanup
                    ILCodeStream *pcsSetup = m_slIL.GetSetupCodeStream();
                    DWORD dwRCWRegisteredLocalNum = pcsSetup->NewLocal(ELEMENT_TYPE_BOOLEAN);
                    pcsSetup->EmitLDC(0);
                    pcsSetup->EmitSTLOC(dwRCWRegisteredLocalNum);

                    pcsMarshal->EmitLDC(1);
                    pcsMarshal->EmitSTLOC(dwRCWRegisteredLocalNum);

                    ILCodeStream *pcsCleanup = m_slIL.GetCleanupCodeStream();
                    ILCodeLabel *pSkipCleanupLabel = pcsCleanup->NewCodeLabel();
                    
                    m_slIL.SetCleanupNeeded();
                    pcsCleanup->EmitLDLOC(dwRCWRegisteredLocalNum);
                    pcsCleanup->EmitBRFALSE(pSkipCleanupLabel);

                    m_slIL.EmitLoadRCWThis(pcsCleanup, m_dwStubFlags);
                    pcsCleanup->EmitCALL(METHOD__STUBHELPERS__STUB_UNREGISTER_RCW, 1, 0);

                    pcsCleanup->EmitLabel(pSkipCleanupLabel);
                }
            }
#endif // MDA_SUPPORTED
        }
#endif // FEATURE_COMINTEROP

        // <NOTE>
        // The profiler helpers below must be called immediately before and after the call to the target.
        // The debugger trace call helpers are invoked from StubRareDisableWorker
        // </NOTE>

#if defined(PROFILING_SUPPORTED)
        DWORD dwMethodDescLocalNum = -1;

        // Notify the profiler of call out of the runtime
        if (!SF_IsReverseCOMStub(m_dwStubFlags) && (CORProfilerTrackTransitions() || SF_IsNGENedStubForProfiling(m_dwStubFlags)))
        {
            dwMethodDescLocalNum = m_slIL.EmitProfilerBeginTransitionCallback(pcsDispatch, m_dwStubFlags);
            _ASSERTE(dwMethodDescLocalNum != -1);
        }
#endif // PROFILING_SUPPORTED

#ifdef MDA_SUPPORTED
        if (SF_IsForwardStub(m_dwStubFlags) && !SF_IsNGENedStub(m_dwStubFlags) &&
            MDA_GET_ASSISTANT(GcManagedToUnmanaged))
        {
            m_slIL.EmitCallGcCollectForMDA(pcsDispatch, m_dwStubFlags);
        }
#endif // MDA_SUPPORTED

        // For CoreClr, clear the last error before calling the target that returns last error.
        // There isn't always a way to know the function have failed without checking last error,
        // in particular on Unix.
        if (m_fSetLastError && SF_IsForwardStub(m_dwStubFlags))
        {
            pcsDispatch->EmitCALL(METHOD__STUBHELPERS__CLEAR_LAST_ERROR, 0, 0);
        }

        // Invoke the target (calli, call method, call delegate, get/set field, etc.)
        EmitInvokeTarget(pStubMD);

        // Saving last error must be the first thing we do after returning from the target
        if (m_fSetLastError && SF_IsForwardStub(m_dwStubFlags))
        {
            pcsDispatch->EmitCALL(METHOD__STUBHELPERS__SET_LAST_ERROR, 0, 0);
        }

#if defined(_TARGET_X86_)
        if (SF_IsForwardDelegateStub(m_dwStubFlags))
        {
            // the delegate may have an intercept stub attached to its sync block so we should
            // prevent it from being garbage collected when the call is in progress
            pcsDispatch->EmitLoadThis();
            pcsDispatch->EmitCALL(METHOD__GC__KEEP_ALIVE, 1, 0);
        }
#endif // defined(_TARGET_X86_)

#ifdef MDA_SUPPORTED
        if (SF_IsForwardStub(m_dwStubFlags) && !SF_IsNGENedStub(m_dwStubFlags) &&
            MDA_GET_ASSISTANT(GcUnmanagedToManaged))
        {
            m_slIL.EmitCallGcCollectForMDA(pcsDispatch, m_dwStubFlags);
        }
#endif // MDA_SUPPORTED

#ifdef VERIFY_HEAP
        if (SF_IsForwardStub(m_dwStubFlags) && g_pConfig->InteropValidatePinnedObjects())
        {
            // call StubHelpers.ValidateObject/StubHelpers.ValidateByref on pinned locals
            m_slIL.EmitObjectValidation(pcsDispatch, m_dwStubFlags);
        }
#endif // VERIFY_HEAP

#if defined(PROFILING_SUPPORTED)
        // Notify the profiler of return back into the runtime
        if (dwMethodDescLocalNum != -1)
        {
            m_slIL.EmitProfilerEndTransitionCallback(pcsDispatch, m_dwStubFlags, dwMethodDescLocalNum);
        }
#endif // PROFILING_SUPPORTED

#ifdef FEATURE_COMINTEROP
        if (SF_IsForwardCOMStub(m_dwStubFlags))
        {
            // Make sure that the RCW stays alive for the duration of the call. Note that if we do HRESULT
            // swapping, we'll pass 'this' to GetCOMHRExceptionObject after returning from the target so
            // GC.KeepAlive is not necessary.
            if (!SF_IsHRESULTSwapping(m_dwStubFlags))
            {
                m_slIL.EmitLoadRCWThis(pcsDispatch, m_dwStubFlags);
                pcsDispatch->EmitCALL(METHOD__GC__KEEP_ALIVE, 1, 0);
            }
        }
#endif // FEATURE_COMINTEROP

        if (SF_IsHRESULTSwapping(m_dwStubFlags))
        {
            if (SF_IsForwardStub(m_dwStubFlags))
            {
                ILCodeLabel* pSkipThrowLabel = pcsDispatch->NewCodeLabel();

                pcsDispatch->EmitDUP();
                pcsDispatch->EmitLDC(0);
                pcsDispatch->EmitBGE(pSkipThrowLabel);

#ifdef FEATURE_COMINTEROP
                if (SF_IsCOMStub(m_dwStubFlags))
                {
                    m_slIL.EmitLoadStubContext(pcsDispatch, m_dwStubFlags);
                    m_slIL.EmitLoadRCWThis(pcsDispatch, m_dwStubFlags);

                    if (SF_IsWinRTStub(m_dwStubFlags))
                    {
                        pcsDispatch->EmitCALL(METHOD__STUBHELPERS__GET_COM_HR_EXCEPTION_OBJECT_WINRT, 3, 1);
                    }
                    else
                    {
                        pcsDispatch->EmitCALL(METHOD__STUBHELPERS__GET_COM_HR_EXCEPTION_OBJECT, 3, 1);
                    }
                }
                else
#endif // FEATURE_COMINTEROP
                {
                    pcsDispatch->EmitCALL(METHOD__STUBHELPERS__GET_HR_EXCEPTION_OBJECT, 1, 1);
                }
                
                pcsDispatch->EmitTHROW();
                pcsDispatch->EmitLDC(0);   // keep the IL stack balanced across the branch and the fall-through
                pcsDispatch->EmitLabel(pSkipThrowLabel);
                pcsDispatch->EmitPOP();
            }
        }

        m_slIL.End(m_dwStubFlags);
        if (!hasTryCatchForHRESULT) // we will 'leave' the try scope and then 'ret' from outside
        {
            pcsUnmarshal->EmitRET();
        }

        CORJIT_FLAGS jitFlags(CORJIT_FLAGS::CORJIT_FLAG_IL_STUB);
                
        if (m_slIL.HasInteropParamExceptionInfo())
        {
            // This code will not use the secret parameter, so we do not
            // tell the JIT to bother with it.
            m_slIL.ClearCode();
            m_slIL.GenerateInteropParamException(pcsMarshal);
        }
        else if (SF_IsFieldGetterStub(m_dwStubFlags) || SF_IsFieldSetterStub(m_dwStubFlags))
        {
            // Field access stubs are not shared and do not use the secret parameter.
        }
#ifndef _WIN64
        else if (SF_IsForwardDelegateStub(m_dwStubFlags) ||
                (SF_IsForwardCOMStub(m_dwStubFlags) && SF_IsWinRTDelegateStub(m_dwStubFlags)))
        {
            // Forward delegate stubs get all the context they need in 'this' so they
            // don't use the secret parameter. Except for AMD64 where we use the secret
            // argument to pass the real target to the stub-for-host.
        }
#endif // !_WIN64
        else
        {
            // All other IL stubs will need to use the secret parameter.
            jitFlags.Set(CORJIT_FLAGS::CORJIT_FLAG_PUBLISH_SECRET_PARAM);
        }

        if (SF_IsReverseStub(m_dwStubFlags))
        {
            SwapStubSignatures(pStubMD);
        }

        ILCodeLabel* pTryEndAndCatchBeginLabel = NULL; // try ends at the same place the catch begins
        ILCodeLabel* pCatchEndAndReturnLabel = NULL;   // catch ends at the same place we resume afterwards
#ifdef FEATURE_COMINTEROP
        if (hasTryCatchForHRESULT)
        {
            EmitExceptionHandler(&nativeReturnType, &managedReturnType, &pTryEndAndCatchBeginLabel, &pCatchEndAndReturnLabel);
        }
#endif // FEATURE_COMINTEROP

        UINT   maxStack;
        size_t cbCode;
        DWORD  cbSig;
        BYTE * pbBuffer;
        BYTE * pbLocalSig;

        cbCode = m_slIL.Link(&maxStack);
        cbSig = m_slIL.GetLocalSigSize();

        ILStubResolver *       pResolver = pStubMD->AsDynamicMethodDesc()->GetILStubResolver();
        COR_ILMETHOD_DECODER * pILHeader = pResolver->AllocGeneratedIL(cbCode, cbSig, maxStack);
        pbBuffer   = (BYTE *)pILHeader->Code;
        pbLocalSig = (BYTE *)pILHeader->LocalVarSig;
        _ASSERTE(cbSig == pILHeader->cbLocalVarSig);

        ILStubEHClause cleanupTryFinally = { 0 };
        ILStubEHClause convertToHRTryCatch = { 0 };
        m_slIL.GetCleanupFinallyOffsets(&cleanupTryFinally);

#ifdef FEATURE_COMINTEROP
        if (hasTryCatchForHRESULT)
        {
            convertToHRTryCatch.kind = ILStubEHClause::kTypedCatch;
            convertToHRTryCatch.dwTryBeginOffset = 0;
            convertToHRTryCatch.dwHandlerBeginOffset = ((DWORD)pTryEndAndCatchBeginLabel->GetCodeOffset());
            convertToHRTryCatch.cbTryLength = convertToHRTryCatch.dwHandlerBeginOffset - convertToHRTryCatch.dwTryBeginOffset;
            convertToHRTryCatch.cbHandlerLength = ((DWORD)pCatchEndAndReturnLabel->GetCodeOffset()) - convertToHRTryCatch.dwHandlerBeginOffset;
            convertToHRTryCatch.dwTypeToken = pcsDispatch->GetToken(g_pObjectClass);
        }
#endif // FEATURE_COMINTEROP

        int nEHClauses = 0;

        if (convertToHRTryCatch.cbHandlerLength != 0)
            nEHClauses++;

        if (cleanupTryFinally.cbHandlerLength != 0)
            nEHClauses++;

        if (nEHClauses > 0)
        {
            COR_ILMETHOD_SECT_EH* pEHSect = pResolver->AllocEHSect(nEHClauses);
            PopulateEHSect(pEHSect, nEHClauses, &cleanupTryFinally, &convertToHRTryCatch);
        }

        m_slIL.GenerateCode(pbBuffer, cbCode);
        m_slIL.GetLocalSig(pbLocalSig, cbSig);

        pResolver->SetJitFlags(jitFlags);

#ifdef LOGGING
        LOG((LF_STUBS, LL_INFO1000, "---------------------------------------------------------------------\n"));
        LOG((LF_STUBS, LL_INFO1000, "NDirect IL stub dump: %s::%s\n", pStubMD->m_pszDebugClassName, pStubMD->m_pszDebugMethodName));
        if (LoggingEnabled() && LoggingOn(LF_STUBS, LL_INFO1000))
        {
            CQuickBytes qbManaged;
            CQuickBytes qbLocal;

            PCCOR_SIGNATURE pManagedSig;
            ULONG           cManagedSig;

            IMDInternalImport* pIMDI = MscorlibBinder::GetModule()->GetMDImport();

            pStubMD->GetSig(&pManagedSig, &cManagedSig);

            PrettyPrintSig(pManagedSig,  cManagedSig, "*",  &qbManaged, pStubMD->GetMDImport(), NULL);
            PrettyPrintSig(pbLocalSig,   cbSig, NULL, &qbLocal,   pIMDI, NULL);

            LOG((LF_STUBS, LL_INFO1000, "incoming managed sig: %p: %s\n", pManagedSig, qbManaged.Ptr()));
            LOG((LF_STUBS, LL_INFO1000, "locals sig:           %p: %s\n", pbLocalSig+1, qbLocal.Ptr()));

            if (cleanupTryFinally.cbHandlerLength != 0)
            {
                LOG((LF_STUBS, LL_INFO1000, "try_begin: 0x%04x try_end: 0x%04x finally_begin: 0x%04x finally_end: 0x%04x \n", 
                    cleanupTryFinally.dwTryBeginOffset, cleanupTryFinally.dwTryBeginOffset + cleanupTryFinally.cbTryLength, 
                    cleanupTryFinally.dwHandlerBeginOffset, cleanupTryFinally.dwHandlerBeginOffset + cleanupTryFinally.cbHandlerLength));
            }
            if (convertToHRTryCatch.cbHandlerLength != 0)
            {
                LOG((LF_STUBS, LL_INFO1000, "try_begin: 0x%04x try_end: 0x%04x catch_begin: 0x%04x catch_end: 0x%04x type_token: 0x%08x\n", 
                    convertToHRTryCatch.dwTryBeginOffset, convertToHRTryCatch.dwTryBeginOffset + convertToHRTryCatch.cbTryLength, 
                    convertToHRTryCatch.dwHandlerBeginOffset, convertToHRTryCatch.dwHandlerBeginOffset + convertToHRTryCatch.cbHandlerLength,
                    convertToHRTryCatch.dwTypeToken));
            }

            LogILStubFlags(LF_STUBS, LL_INFO1000, m_dwStubFlags);

            m_slIL.LogILStub(jitFlags);
        }
        LOG((LF_STUBS, LL_INFO1000, "^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^\n"));
#endif // LOGGING
        
    }


#ifdef LOGGING
    //---------------------------------------------------------------------------------------
    // 
    static inline void LogOneFlag(DWORD flags, DWORD flag, LPCSTR str, DWORD facility, DWORD level)
    {
        LIMITED_METHOD_CONTRACT;
        if (flags & flag)
        {
            LOG((facility, level, str));
        }
    }

    static void LogILStubFlags(DWORD facility, DWORD level, DWORD dwStubFlags)
    {
        LIMITED_METHOD_CONTRACT;
        LOG((facility, level, "dwStubFlags: 0x%08x\n", dwStubFlags));
        LogOneFlag(dwStubFlags, NDIRECTSTUB_FL_CONVSIGASVARARG,         "   NDIRECTSTUB_FL_CONVSIGASVARARG\n", facility, level);
        LogOneFlag(dwStubFlags, NDIRECTSTUB_FL_BESTFIT,                 "   NDIRECTSTUB_FL_BESTFIT\n", facility, level);
        LogOneFlag(dwStubFlags, NDIRECTSTUB_FL_THROWONUNMAPPABLECHAR,   "   NDIRECTSTUB_FL_THROWONUNMAPPABLECHAR\n", facility, level);
        LogOneFlag(dwStubFlags, NDIRECTSTUB_FL_NGENEDSTUB,              "   NDIRECTSTUB_FL_NGENEDSTUB\n", facility, level);
        LogOneFlag(dwStubFlags, NDIRECTSTUB_FL_DELEGATE,                "   NDIRECTSTUB_FL_DELEGATE\n", facility, level);
        LogOneFlag(dwStubFlags, NDIRECTSTUB_FL_DOHRESULTSWAPPING,       "   NDIRECTSTUB_FL_DOHRESULTSWAPPING\n", facility, level);
        LogOneFlag(dwStubFlags, NDIRECTSTUB_FL_REVERSE_INTEROP,         "   NDIRECTSTUB_FL_REVERSE_INTEROP\n", facility, level);
#ifdef FEATURE_COMINTEROP
        LogOneFlag(dwStubFlags, NDIRECTSTUB_FL_COM,                     "   NDIRECTSTUB_FL_COM\n", facility, level);
#endif // FEATURE_COMINTEROP
        LogOneFlag(dwStubFlags, NDIRECTSTUB_FL_NGENEDSTUBFORPROFILING,  "   NDIRECTSTUB_FL_NGENEDSTUBFORPROFILING\n", facility, level);
        LogOneFlag(dwStubFlags, NDIRECTSTUB_FL_GENERATEDEBUGGABLEIL,    "   NDIRECTSTUB_FL_GENERATEDEBUGGABLEIL\n", facility, level);
        LogOneFlag(dwStubFlags, NDIRECTSTUB_FL_HASDECLARATIVESECURITY,  "   NDIRECTSTUB_FL_HASDECLARATIVESECURITY\n", facility, level);
        LogOneFlag(dwStubFlags, NDIRECTSTUB_FL_UNMANAGED_CALLI,         "   NDIRECTSTUB_FL_UNMANAGED_CALLI\n", facility, level);
        LogOneFlag(dwStubFlags, NDIRECTSTUB_FL_TRIGGERCCTOR,            "   NDIRECTSTUB_FL_TRIGGERCCTOR\n", facility, level);
#ifdef FEATURE_COMINTEROP
        LogOneFlag(dwStubFlags, NDIRECTSTUB_FL_FIELDGETTER,             "   NDIRECTSTUB_FL_FIELDGETTER\n", facility, level);
        LogOneFlag(dwStubFlags, NDIRECTSTUB_FL_FIELDSETTER,             "   NDIRECTSTUB_FL_FIELDSETTER\n", facility, level);
        LogOneFlag(dwStubFlags, NDIRECTSTUB_FL_WINRT,                   "   NDIRECTSTUB_FL_WINRT\n", facility, level);
        LogOneFlag(dwStubFlags, NDIRECTSTUB_FL_WINRTDELEGATE,           "   NDIRECTSTUB_FL_WINRTDELEGATE\n", facility, level);
        LogOneFlag(dwStubFlags, NDIRECTSTUB_FL_WINRTSHAREDGENERIC,      "   NDIRECTSTUB_FL_WINRTSHAREDGENERIC\n", facility, level);
        LogOneFlag(dwStubFlags, NDIRECTSTUB_FL_WINRTCTOR,               "   NDIRECTSTUB_FL_WINRTCTOR\n", facility, level);
        LogOneFlag(dwStubFlags, NDIRECTSTUB_FL_WINRTCOMPOSITION,        "   NDIRECTSTUB_FL_WINRTCOMPOSITION\n", facility, level);
        LogOneFlag(dwStubFlags, NDIRECTSTUB_FL_WINRTSTATIC,             "   NDIRECTSTUB_FL_WINRTSTATIC\n", facility, level);
        LogOneFlag(dwStubFlags, NDIRECTSTUB_FL_WINRTHASREDIRECTION,     "   NDIRECTSTUB_FL_WINRTHASREDIRECTION\n", facility, level);
#endif // FEATURE_COMINTEROP

        //
        // no need to log the internal flags, let's just assert what we expect to see...
        //
        CONSISTENCY_CHECK(!SF_IsCOMLateBoundStub(dwStubFlags));
        CONSISTENCY_CHECK(!SF_IsCOMEventCallStub(dwStubFlags));

        DWORD dwKnownMask = 
            NDIRECTSTUB_FL_CONVSIGASVARARG          |
            NDIRECTSTUB_FL_BESTFIT                  |
            NDIRECTSTUB_FL_THROWONUNMAPPABLECHAR    |
            NDIRECTSTUB_FL_NGENEDSTUB               |
            NDIRECTSTUB_FL_DELEGATE                 |
            NDIRECTSTUB_FL_DOHRESULTSWAPPING        |
            NDIRECTSTUB_FL_REVERSE_INTEROP          |
            NDIRECTSTUB_FL_NGENEDSTUBFORPROFILING   |
            NDIRECTSTUB_FL_GENERATEDEBUGGABLEIL     |
            NDIRECTSTUB_FL_HASDECLARATIVESECURITY   |
            NDIRECTSTUB_FL_UNMANAGED_CALLI          |
            NDIRECTSTUB_FL_TRIGGERCCTOR             |
#ifdef FEATURE_COMINTEROP
            NDIRECTSTUB_FL_COM                      |
            NDIRECTSTUB_FL_COMLATEBOUND             |   // internal
            NDIRECTSTUB_FL_COMEVENTCALL             |   // internal
            NDIRECTSTUB_FL_FIELDGETTER              |
            NDIRECTSTUB_FL_FIELDSETTER              |
            NDIRECTSTUB_FL_WINRT                    |
            NDIRECTSTUB_FL_WINRTDELEGATE            |
            NDIRECTSTUB_FL_WINRTCTOR                |
            NDIRECTSTUB_FL_WINRTCOMPOSITION         |
            NDIRECTSTUB_FL_WINRTSTATIC              |
            NDIRECTSTUB_FL_WINRTHASREDIRECTION      |
#endif // FEATURE_COMINTEROP
            NULL;

        DWORD dwUnknownFlags = dwStubFlags & ~dwKnownMask;
        if (0 != dwUnknownFlags)
        {
            LOG((facility, level, "UNKNOWN FLAGS: 0x%08x\n", dwUnknownFlags));
        }
    }
#endif // LOGGING

    PCCOR_SIGNATURE GetStubTargetMethodSig()
    {
        CONTRACT(PCCOR_SIGNATURE)
        {
            STANDARD_VM_CHECK;
            POSTCONDITION(CheckPointer(RETVAL, NULL_NOT_OK));
        }
        CONTRACT_END;

        BYTE *pb;

        if (!m_qbNativeFnSigBuffer.Size())
        {
            DWORD cb = m_slIL.GetStubTargetMethodSigSize();
            pb = (BYTE *)m_qbNativeFnSigBuffer.AllocThrows(cb);

            m_slIL.GetStubTargetMethodSig(pb, cb);
        }
        else
        {
            pb = (BYTE*)m_qbNativeFnSigBuffer.Ptr();
        }

        RETURN pb;
    }

    DWORD 
    GetStubTargetMethodSigLength()
    {
        WRAPPER_NO_CONTRACT;

        return m_slIL.GetStubTargetMethodSigSize();
    }

    void SetStubTargetMethodSig(PCCOR_SIGNATURE pSig, DWORD cSig)
    {
        WRAPPER_NO_CONTRACT;

        m_slIL.SetStubTargetMethodSig(pSig, cSig);
        m_qbNativeFnSigBuffer.Shrink(0);
    }
    
    TokenLookupMap* GetTokenLookupMap() { WRAPPER_NO_CONTRACT; return m_slIL.GetTokenLookupMap(); }

protected:
    CQuickBytes         m_qbNativeFnSigBuffer;
    NDirectStubLinker   m_slIL;
    BOOL                m_fSetLastError;
    DWORD               m_dwStubFlags;
};


class PInvoke_ILStubState : public ILStubState
{
public:
        
    PInvoke_ILStubState(Module* pStubModule, const Signature &signature, SigTypeContext *pTypeContext, DWORD dwStubFlags,
                        CorPinvokeMap unmgdCallConv, int iLCIDParamIdx, MethodDesc* pTargetMD)
        : ILStubState(
                pStubModule,
                signature,
                pTypeContext,
                TargetHasThis(dwStubFlags),
                StubHasThis(dwStubFlags),
                dwStubFlags,
                iLCIDParamIdx,
                pTargetMD)
    {
        STANDARD_VM_CONTRACT;

        if (SF_IsForwardStub(dwStubFlags))
        {
            m_slIL.SetCallingConvention(unmgdCallConv, SF_IsVarArgStub(dwStubFlags));
        }
    }

private:
    static BOOL TargetHasThis(DWORD dwStubFlags)
    {
        //
        // in reverse pinvoke on delegate, the managed target will 
        // have a 'this' pointer, but the unmanaged signature does 
        // not.
        //
        return SF_IsReverseDelegateStub(dwStubFlags);
    }

    static BOOL StubHasThis(DWORD dwStubFlags)
    {
        //
        // in forward pinvoke on a delegate, the stub will have a
        // 'this' pointer, but the unmanaged target will not.
        //
        return SF_IsForwardDelegateStub(dwStubFlags);
    }
};

#ifdef FEATURE_COMINTEROP
class CLRToCOM_ILStubState : public ILStubState
{
public:

    CLRToCOM_ILStubState(Module* pStubModule, const Signature &signature, SigTypeContext *pTypeContext, DWORD dwStubFlags,
                         int iLCIDParamIdx, MethodDesc* pTargetMD)
        : ILStubState(
                pStubModule,
                signature,
                pTypeContext,
                TRUE,
                !SF_IsWinRTStaticStub(dwStubFlags), // fStubHasThis
                dwStubFlags,
                iLCIDParamIdx,
                pTargetMD)
    {
        STANDARD_VM_CONTRACT;

        if (SF_IsForwardStub(dwStubFlags))
        {
            m_slIL.SetCallingConvention(pmCallConvStdcall, SF_IsVarArgStub(dwStubFlags));
        }
    }

    void BeginEmit(DWORD dwStubFlags)  // CLR to COM IL
    {
        STANDARD_VM_CONTRACT;

        ILStubState::BeginEmit(dwStubFlags);

        ILCodeStream *pcsDispatch = m_slIL.GetDispatchCodeStream();

        // add the 'this' COM IP parameter to the target CALLI
        m_slIL.GetMarshalCodeStream()->SetStubTargetArgType(ELEMENT_TYPE_I, false);

        // convert 'this' to COM IP and the target method entry point
        m_slIL.EmitLoadRCWThis(pcsDispatch, m_dwStubFlags);

#ifdef _WIN64
        if (SF_IsWinRTDelegateStub(m_dwStubFlags))
        {
            // write the stub context (EEImplMethodDesc representing the Invoke)
            // into the secret arg so it shows up in the InlinedCallFrame and can
            // be used by stub for host

            pcsDispatch->EmitCALL(METHOD__STUBHELPERS__GET_STUB_CONTEXT_ADDR, 0, 1);
            m_slIL.EmitLoadStubContext(pcsDispatch, dwStubFlags);
            pcsDispatch->EmitSTIND_I();
            pcsDispatch->EmitCALL(METHOD__STUBHELPERS__GET_STUB_CONTEXT, 0, 1);
        }
        else
#endif // _WIN64
        {
            m_slIL.EmitLoadStubContext(pcsDispatch, dwStubFlags);
        }

        pcsDispatch->EmitLDLOCA(m_slIL.GetTargetEntryPointLocalNum());

        BinderMethodID getCOMIPMethod;
        bool fDoPostCallIPCleanup = true;

        if (!SF_IsNGENedStub(dwStubFlags) && NDirect::IsHostHookEnabled())
        {
            // always use the non-optimized helper if we are hosted
            getCOMIPMethod = METHOD__STUBHELPERS__GET_COM_IP_FROM_RCW;
        }
        else if (SF_IsWinRTStub(dwStubFlags))
        {
            // WinRT uses optimized helpers
            if (SF_IsWinRTSharedGenericStub(dwStubFlags))
                getCOMIPMethod = METHOD__STUBHELPERS__GET_COM_IP_FROM_RCW_WINRT_SHARED_GENERIC;
            else if (SF_IsWinRTDelegateStub(dwStubFlags))
                getCOMIPMethod = METHOD__STUBHELPERS__GET_COM_IP_FROM_RCW_WINRT_DELEGATE;
            else
                getCOMIPMethod = METHOD__STUBHELPERS__GET_COM_IP_FROM_RCW_WINRT;

            // GetCOMIPFromRCW_WinRT, GetCOMIPFromRCW_WinRTSharedGeneric, and GetCOMIPFromRCW_WinRTDelegate
            // always cache the COM interface pointer so no post-call cleanup is needed
            fDoPostCallIPCleanup = false;
        }
        else
        {
            // classic COM interop uses the non-optimized helper
            getCOMIPMethod = METHOD__STUBHELPERS__GET_COM_IP_FROM_RCW;
        }

        DWORD dwIPRequiresCleanupLocalNum = (DWORD)-1;
        if (fDoPostCallIPCleanup)
        {
            dwIPRequiresCleanupLocalNum = pcsDispatch->NewLocal(ELEMENT_TYPE_BOOLEAN);
            pcsDispatch->EmitLDLOCA(dwIPRequiresCleanupLocalNum);

            // StubHelpers.GetCOMIPFromRCW(object objSrc, IntPtr pCPCMD, out IntPtr ppTarget, out bool pfNeedsRelease)
            pcsDispatch->EmitCALL(getCOMIPMethod, 4, 1);
        }
        else
        {
            // StubHelpers.GetCOMIPFromRCW_WinRT*(object objSrc, IntPtr pCPCMD, out IntPtr ppTarget)
            pcsDispatch->EmitCALL(getCOMIPMethod, 3, 1);
        }


        // save it because we'll need it to compute the CALLI target and release it
        pcsDispatch->EmitDUP();
        pcsDispatch->EmitSTLOC(m_slIL.GetTargetInterfacePointerLocalNum());

        if (fDoPostCallIPCleanup)
        {
            // make sure it's Release()'ed after the call
            m_slIL.SetCleanupNeeded();
            ILCodeStream *pcsCleanup = m_slIL.GetCleanupCodeStream();

            ILCodeLabel *pSkipThisCleanup = pcsCleanup->NewCodeLabel();

            // and if it requires cleanup (i.e. it's not taken from the RCW cache)
            pcsCleanup->EmitLDLOC(dwIPRequiresCleanupLocalNum);
            pcsCleanup->EmitBRFALSE(pSkipThisCleanup);

            pcsCleanup->EmitLDLOC(m_slIL.GetTargetInterfacePointerLocalNum());
            pcsCleanup->EmitCALL(METHOD__INTERFACEMARSHALER__CLEAR_NATIVE, 1, 0);
            pcsCleanup->EmitLabel(pSkipThisCleanup);
        }
    }
};

class COMToCLR_ILStubState : public ILStubState
{
public:

    COMToCLR_ILStubState(Module* pStubModule, const Signature &signature, SigTypeContext *pTypeContext, DWORD dwStubFlags,
                         int iLCIDParamIdx, MethodDesc* pTargetMD)
        : ILStubState(
                pStubModule,
                signature,
                pTypeContext,
                TRUE,
                TRUE,
                dwStubFlags,
                iLCIDParamIdx,
                pTargetMD)
    {
        STANDARD_VM_CONTRACT;
    }

    void BeginEmit(DWORD dwStubFlags)  // COM to CLR IL
    {
        STANDARD_VM_CONTRACT;

        ILStubState::BeginEmit(dwStubFlags);

        if (SF_IsWinRTStaticStub(dwStubFlags))
        {
            // we are not loading 'this' because the target is static
            m_slIL.AdjustTargetStackDeltaForExtraParam();
        }
        else
        {
            // load this
            m_slIL.GetDispatchCodeStream()->EmitLoadThis();
        }
    }

    void MarshalFactoryReturn()
    {
        CONTRACTL
        {
            STANDARD_VM_CHECK;
            PRECONDITION(SF_IsWinRTCtorStub(m_dwStubFlags));
        }
        CONTRACTL_END;

        ILCodeStream *pcsSetup     = m_slIL.GetSetupCodeStream();
        ILCodeStream *pcsDispatch  = m_slIL.GetDispatchCodeStream();
        ILCodeStream *pcsUnmarshal = m_slIL.GetReturnUnmarshalCodeStream();
        ILCodeStream *pcsExCleanup = m_slIL.GetExceptionCleanupCodeStream();

        LocalDesc locDescFactoryRetVal(ELEMENT_TYPE_I);
        DWORD dwFactoryRetValLocalNum = pcsSetup->NewLocal(locDescFactoryRetVal);
        pcsSetup->EmitLoadNullPtr();
        pcsSetup->EmitSTLOC(dwFactoryRetValLocalNum);

        locDescFactoryRetVal.MakeByRef();

        // expect one additional argument - pointer to a location that receives the created instance
        DWORD dwRetValArgNum = pcsDispatch->SetStubTargetArgType(&locDescFactoryRetVal, false);
        m_slIL.AdjustTargetStackDeltaForExtraParam();

        // convert 'this' to an interface pointer corresponding to the default interface of this class
        pcsUnmarshal->EmitLoadThis();
        pcsUnmarshal->EmitCALL(METHOD__STUBHELPERS__GET_STUB_CONTEXT, 0, 1);
        pcsUnmarshal->EmitCALL(METHOD__STUBHELPERS__GET_WINRT_FACTORY_RETURN_VALUE, 2, 1);
        pcsUnmarshal->EmitSTLOC(dwFactoryRetValLocalNum);

        // assign it to the location pointed to by the argument
        pcsUnmarshal->EmitLDARG(dwRetValArgNum);
        pcsUnmarshal->EmitLDLOC(dwFactoryRetValLocalNum);
        pcsUnmarshal->EmitSTIND_I();

        // on exception, we want to release the IInspectable's and assign NULL to output locations
        m_slIL.SetExceptionCleanupNeeded();

        EmitInterfaceClearNative(pcsExCleanup, dwFactoryRetValLocalNum);

        // *retVal = NULL
        pcsExCleanup->EmitLDARG(dwRetValArgNum);
        pcsExCleanup->EmitLoadNullPtr();
        pcsExCleanup->EmitSTIND_I();

    }
};

class COMToCLRFieldAccess_ILStubState : public COMToCLR_ILStubState
{
public:

    COMToCLRFieldAccess_ILStubState(Module* pStubModule, const Signature &signature, SigTypeContext *pTypeContext,
                                    DWORD dwStubFlags, FieldDesc* pFD)
        : COMToCLR_ILStubState(
                pStubModule,
                signature,
                pTypeContext,
                dwStubFlags,
                -1,
                NULL)
    {
        STANDARD_VM_CONTRACT;

        _ASSERTE(pFD != NULL);
        m_pFD = pFD;
    }

    void EmitInvokeTarget(MethodDesc *pStubMD)
    {
        STANDARD_VM_CONTRACT;

        ILCodeStream* pcsDispatch = m_slIL.GetDispatchCodeStream();

        if (SF_IsFieldGetterStub(m_dwStubFlags))
        {
            pcsDispatch->EmitLDFLD(pcsDispatch->GetToken(m_pFD));
        }
        else
        {
            CONSISTENCY_CHECK(SF_IsFieldSetterStub(m_dwStubFlags));
            pcsDispatch->EmitSTFLD(pcsDispatch->GetToken(m_pFD));
        }
    }

protected:
    FieldDesc *m_pFD;
};
#endif // FEATURE_COMINTEROP


NDirectStubLinker::NDirectStubLinker(
            DWORD dwStubFlags,
            Module* pModule,
            const Signature &signature,
            SigTypeContext *pTypeContext,
            MethodDesc* pTargetMD,
            int  iLCIDParamIdx,
            BOOL fTargetHasThis, 
            BOOL fStubHasThis)
     : ILStubLinker(pModule, signature, pTypeContext, pTargetMD, fTargetHasThis, fStubHasThis, !SF_IsCOMStub(dwStubFlags)),
    m_pCleanupFinallyBeginLabel(NULL),
    m_pCleanupFinallyEndLabel(NULL),
    m_pSkipExceptionCleanupLabel(NULL),
#ifdef FEATURE_COMINTEROP
    m_dwWinRTFactoryObjectLocalNum(-1),
#endif // FEATURE_COMINTEROP
    m_fHasCleanupCode(FALSE),
    m_fHasExceptionCleanupCode(FALSE),
    m_fCleanupWorkListIsSetup(FALSE),
    m_dwThreadLocalNum(-1),
    m_dwCleanupWorkListLocalNum(-1),
    m_dwRetValLocalNum(-1),
    m_ErrorResID(-1),
    m_ErrorParamIdx(-1),
    m_iLCIDParamIdx(iLCIDParamIdx),
    m_dwStubFlags(dwStubFlags)
{
    STANDARD_VM_CONTRACT;


    m_pcsSetup              = NewCodeStream(ILStubLinker::kSetup);              // do any one-time setup work
    m_pcsMarshal            = NewCodeStream(ILStubLinker::kMarshal);            // marshals arguments
    m_pcsDispatch           = NewCodeStream(ILStubLinker::kDispatch);           // sets up arguments and makes call
    m_pcsRetUnmarshal       = NewCodeStream(ILStubLinker::kReturnUnmarshal);    // unmarshals return value
    m_pcsUnmarshal          = NewCodeStream(ILStubLinker::kUnmarshal);          // unmarshals arguments
    m_pcsExceptionCleanup   = NewCodeStream(ILStubLinker::kExceptionCleanup);   // MAY NOT THROW: goes in a finally and does exception-only cleanup
    m_pcsCleanup            = NewCodeStream(ILStubLinker::kCleanup);            // MAY NOT THROW: goes in a finally and does unconditional cleanup

    //
    // Add locals
    m_dwArgMarshalIndexLocalNum = NewLocal(ELEMENT_TYPE_I4);
    m_pcsMarshal->EmitLDC(0);
    m_pcsMarshal->EmitSTLOC(m_dwArgMarshalIndexLocalNum);
    
#ifdef FEATURE_COMINTEROP
    //
    // Forward COM interop needs a local to hold target interface pointer
    //
    if (SF_IsForwardCOMStub(m_dwStubFlags))
    {
        m_dwTargetEntryPointLocalNum = NewLocal(ELEMENT_TYPE_I);
        m_dwTargetInterfacePointerLocalNum = NewLocal(ELEMENT_TYPE_I);
        m_pcsSetup->EmitLoadNullPtr();
        m_pcsSetup->EmitSTLOC(m_dwTargetInterfacePointerLocalNum);
    }
#endif // FEATURE_COMINTEROP
}

void NDirectStubLinker::SetCallingConvention(CorPinvokeMap unmngCallConv, BOOL fIsVarArg)
{
    LIMITED_METHOD_CONTRACT;
    ULONG uNativeCallingConv = 0;

#if !defined(_TARGET_X86_)
    if (fIsVarArg)
    {
        // The JIT has to use a different calling convention for unmanaged vararg targets on 64-bit and ARM:
        // any float values must be duplicated in the corresponding general-purpose registers.
        uNativeCallingConv = CORINFO_CALLCONV_NATIVEVARARG;
    }
    else
#endif // !_TARGET_X86_
    {
        switch (unmngCallConv)
        {
            case pmCallConvCdecl:
                uNativeCallingConv = CORINFO_CALLCONV_C;
                break;
            case pmCallConvStdcall:
                uNativeCallingConv = CORINFO_CALLCONV_STDCALL;
                break;
            case pmCallConvThiscall:
                uNativeCallingConv = CORINFO_CALLCONV_THISCALL;
                break;
            default:
                _ASSERTE(!"Invalid calling convention.");
                uNativeCallingConv = CORINFO_CALLCONV_STDCALL;
                break;
        }
    }

    SetStubTargetCallingConv((CorCallingConvention)uNativeCallingConv);
}

void NDirectStubLinker::EmitSetArgMarshalIndex(ILCodeStream* pcsEmit, UINT uArgIdx)
{
    WRAPPER_NO_CONTRACT;

    // 
    // This sets our state local variable that tracks the progress of the stub execution.
    // In the finally block we test this variable to see what cleanup we need to do. The
    // variable starts with the value of 0 and is assigned the following values as the 
    // stub executes:
    //
    // CLEANUP_INDEX_ARG0_MARSHAL + 1               - 1st argument marshaled
    // CLEANUP_INDEX_ARG0_MARSHAL + 2               - 2nd argument marshaled
    // ...
    // CLEANUP_INDEX_ARG0_MARSHAL + n               - nth argument marshaled
    // CLEANUP_INDEX_RETVAL_UNMARSHAL + 1           - return value unmarshaled
    // CLEANUP_INDEX_ARG0_UNMARSHAL + 1             - 1st argument unmarshaled
    // CLEANUP_INDEX_ARG0_UNMARSHAL + 2             - 2nd argument unmarshaled
    // ...
    // CLEANUP_INDEX_ARG0_UNMARSHAL + n             - nth argument unmarshaled
    // CLEANUP_INDEX_ALL_DONE + 1                   - ran to completion, no exception thrown
    //
    // Note: There may be gaps, i.e. if say 2nd argument does not need cleanup, the
    // state variable will never be assigned the corresponding value. However, the
    // value must always monotonically increase so we can use <=, >, etc.
    // 

    pcsEmit->EmitLDC(uArgIdx + 1);
    pcsEmit->EmitSTLOC(m_dwArgMarshalIndexLocalNum);
}

void NDirectStubLinker::EmitCheckForArgCleanup(ILCodeStream* pcsEmit, UINT uArgIdx, ArgCleanupBranchKind branchKind, ILCodeLabel* pSkipCleanupLabel)
{
    STANDARD_VM_CONTRACT;

    SetCleanupNeeded();

    // See EmitSetArgMarshalIndex.
    pcsEmit->EmitLDLOC(m_dwArgMarshalIndexLocalNum);
    pcsEmit->EmitLDC(uArgIdx);

    switch (branchKind)
    {
        case BranchIfMarshaled:
        {
            // we branch to the label if the argument has been marshaled
            pcsEmit->EmitBGT(pSkipCleanupLabel);
            break;
        }

        case BranchIfNotMarshaled:
        {
            // we branch to the label if the argument has not been marshaled
            pcsEmit->EmitBLE(pSkipCleanupLabel);
            break;
        }

        default:
            UNREACHABLE();
    }
}

int NDirectStubLinker::GetLCIDParamIdx()
{
    LIMITED_METHOD_CONTRACT;
    return m_iLCIDParamIdx;
}

ILCodeStream* NDirectStubLinker::GetSetupCodeStream()
{
    LIMITED_METHOD_CONTRACT;
    return m_pcsSetup;
}

ILCodeStream* NDirectStubLinker::GetMarshalCodeStream()
{
    LIMITED_METHOD_CONTRACT;
    return m_pcsMarshal;
}

ILCodeStream* NDirectStubLinker::GetUnmarshalCodeStream()
{
    LIMITED_METHOD_CONTRACT;
    return m_pcsUnmarshal;
}

ILCodeStream* NDirectStubLinker::GetReturnUnmarshalCodeStream()
{
    LIMITED_METHOD_CONTRACT;
    return m_pcsRetUnmarshal;
}

ILCodeStream* NDirectStubLinker::GetDispatchCodeStream()
{
    LIMITED_METHOD_CONTRACT;
    return m_pcsDispatch;
}

ILCodeStream* NDirectStubLinker::GetCleanupCodeStream()
{
    LIMITED_METHOD_CONTRACT;
    return m_pcsCleanup;
}

ILCodeStream* NDirectStubLinker::GetExceptionCleanupCodeStream()
{
    LIMITED_METHOD_CONTRACT;
    return m_pcsExceptionCleanup;
}

void NDirectStubLinker::AdjustTargetStackDeltaForExtraParam()
{
    LIMITED_METHOD_CONTRACT;
    //
    // Compensate for the extra parameter.
    //
    m_iTargetStackDelta++;
}

void NDirectStubLinker::AdjustTargetStackDeltaForReverseInteropHRESULTSwapping()
{
    WRAPPER_NO_CONTRACT;
    //
    // In the case of reverse pinvoke, we build up the 'target'
    // signature as if it were normal forward pinvoke and then
    // switch that signature (representing the native sig) with
    // the stub's sig (representing the managed sig).  However,
    // as a side-effect, our calcualted target stack delta is 
    // wrong.  
    //
    // The only way that we support a different stack delta is
    // through hresult swapping.  So this code "undoes" the 
    // deltas that would have been applied in that case.  
    //

    if (StubHasVoidReturnType())
    {
        //
        // If the managed return type is void, undo the HRESULT 
        // return type added to our target sig for HRESULT swapping.
        // No extra argument will have been added because it makes
        // no sense to add an extry byref void argument.
        //
        m_iTargetStackDelta--;
    }
    else
    {
        //
        // no longer pop the extra byref argument from the stack
        //
        m_iTargetStackDelta++;
    }
}

void NDirectStubLinker::SetInteropParamExceptionInfo(UINT resID, UINT paramIdx)
{
    LIMITED_METHOD_CONTRACT;

    // only keep the first one
    if (HasInteropParamExceptionInfo())
    {
        return;
    }

    m_ErrorResID = resID;
    m_ErrorParamIdx = paramIdx;
}

bool NDirectStubLinker::HasInteropParamExceptionInfo()
{
    LIMITED_METHOD_CONTRACT;

    return !(((DWORD)-1 == m_ErrorResID) && ((DWORD)-1 == m_ErrorParamIdx));
}

void NDirectStubLinker::GenerateInteropParamException(ILCodeStream* pcsEmit)
{
    STANDARD_VM_CONTRACT;

    pcsEmit->EmitLDC(m_ErrorResID);
    pcsEmit->EmitLDC(m_ErrorParamIdx);
    pcsEmit->EmitCALL(METHOD__STUBHELPERS__THROW_INTEROP_PARAM_EXCEPTION, 2, 0);
    
    pcsEmit->EmitLDNULL();
    pcsEmit->EmitTHROW();
}

#ifdef FEATURE_COMINTEROP
DWORD NDirectStubLinker::GetTargetInterfacePointerLocalNum()
{
    LIMITED_METHOD_CONTRACT;
    CONSISTENCY_CHECK(m_dwTargetInterfacePointerLocalNum != (DWORD)-1);
    return m_dwTargetInterfacePointerLocalNum;
}
DWORD NDirectStubLinker::GetTargetEntryPointLocalNum()
{
    LIMITED_METHOD_CONTRACT;
    CONSISTENCY_CHECK(m_dwTargetEntryPointLocalNum != (DWORD)-1);
    return m_dwTargetEntryPointLocalNum;
}

void NDirectStubLinker::EmitLoadRCWThis(ILCodeStream *pcsEmit, DWORD dwStubFlags)
{
    STANDARD_VM_CONTRACT;

    if (SF_IsForwardStub(dwStubFlags) && 
        (SF_IsWinRTCtorStub(dwStubFlags) || SF_IsWinRTStaticStub(dwStubFlags)))
    {
        // WinRT ctor/static stubs make the call on the factory object instead of 'this'
        if (m_dwWinRTFactoryObjectLocalNum == (DWORD)-1)
        {
            m_dwWinRTFactoryObjectLocalNum = NewLocal(ELEMENT_TYPE_OBJECT);

            // get the factory object
            EmitLoadStubContext(m_pcsSetup, dwStubFlags);
            m_pcsSetup->EmitCALL(METHOD__STUBHELPERS__GET_WINRT_FACTORY_OBJECT, 1, 1);
            m_pcsSetup->EmitSTLOC(m_dwWinRTFactoryObjectLocalNum);
        }

        pcsEmit->EmitLDLOC(m_dwWinRTFactoryObjectLocalNum);
    }
    else
    {
        pcsEmit->EmitLoadThis();
    }
}
#endif // FEATURE_COMINTEROP

DWORD NDirectStubLinker::GetCleanupWorkListLocalNum()
{
    LIMITED_METHOD_CONTRACT;
    CONSISTENCY_CHECK(m_dwCleanupWorkListLocalNum != (DWORD)-1);
    return m_dwCleanupWorkListLocalNum;
}

DWORD NDirectStubLinker::GetThreadLocalNum()
{
    STANDARD_VM_CONTRACT;

    if (m_dwThreadLocalNum == (DWORD)-1)
    {
        // The local is created and initialized lazily when first asked.
        m_dwThreadLocalNum = NewLocal(ELEMENT_TYPE_I);
        m_pcsSetup->EmitCALL(METHOD__THREAD__INTERNAL_GET_CURRENT_THREAD, 0, 1);
        m_pcsSetup->EmitSTLOC(m_dwThreadLocalNum);
    }

    return m_dwThreadLocalNum;
}

DWORD NDirectStubLinker::GetReturnValueLocalNum()
{
    LIMITED_METHOD_CONTRACT;
    return m_dwRetValLocalNum;
}

BOOL NDirectStubLinker::IsCleanupNeeded()
{
    LIMITED_METHOD_CONTRACT;

    return (m_fHasCleanupCode || IsCleanupWorkListSetup());
}

BOOL NDirectStubLinker::IsExceptionCleanupNeeded()
{
    LIMITED_METHOD_CONTRACT;

    return m_fHasExceptionCleanupCode;
}

void NDirectStubLinker::InitCleanupCode()
{
    CONTRACTL
    {
        STANDARD_VM_CHECK;
        PRECONDITION(NULL == m_pCleanupFinallyBeginLabel);
    }
    CONTRACTL_END;

    m_pCleanupFinallyBeginLabel = NewCodeLabel();
    m_pcsExceptionCleanup->EmitLabel(m_pCleanupFinallyBeginLabel);
}

void NDirectStubLinker::InitExceptionCleanupCode()
{
    CONTRACTL
    {
        STANDARD_VM_CHECK;
        PRECONDITION(NULL == m_pSkipExceptionCleanupLabel);
    }
    CONTRACTL_END;

    SetCleanupNeeded();

    // we want to skip the entire exception cleanup if no exception has been thrown
    m_pSkipExceptionCleanupLabel = NewCodeLabel();
    EmitCheckForArgCleanup(m_pcsExceptionCleanup, CLEANUP_INDEX_ALL_DONE, BranchIfMarshaled, m_pSkipExceptionCleanupLabel);
}

void NDirectStubLinker::SetCleanupNeeded()
{
    WRAPPER_NO_CONTRACT;

    if (!m_fHasCleanupCode)
    {
        m_fHasCleanupCode = TRUE;
        InitCleanupCode();
    }
}

void NDirectStubLinker::SetExceptionCleanupNeeded()
{
    WRAPPER_NO_CONTRACT;

    if (!m_fHasExceptionCleanupCode)
    {
        m_fHasExceptionCleanupCode = TRUE;
        InitExceptionCleanupCode();
    }
}

void NDirectStubLinker::NeedsCleanupList()
{
    STANDARD_VM_CONTRACT;

    if (!IsCleanupWorkListSetup())
    {
        m_fCleanupWorkListIsSetup = TRUE;
        SetCleanupNeeded();

        // we setup a new local that will hold the cleanup work list
        LocalDesc desc(MscorlibBinder::GetClass(CLASS__CLEANUP_WORK_LIST));
        m_dwCleanupWorkListLocalNum = NewLocal(desc);
    }
}


BOOL NDirectStubLinker::IsCleanupWorkListSetup ()
{
    LIMITED_METHOD_CONTRACT;

    return m_fCleanupWorkListIsSetup;
}


void NDirectStubLinker::LoadCleanupWorkList(ILCodeStream* pcsEmit)
{
    STANDARD_VM_CONTRACT;

    NeedsCleanupList();
    pcsEmit->EmitLDLOCA(GetCleanupWorkListLocalNum());
}


void NDirectStubLinker::Begin(DWORD dwStubFlags)
{
    STANDARD_VM_CONTRACT;

#ifdef FEATURE_COMINTEROP
    if (SF_IsWinRTHasRedirection(dwStubFlags))
    {
        _ASSERTE(SF_IsForwardCOMStub(dwStubFlags));

        // The very first thing we need to do is check whether the call should be routed to
        // the marshaling stub for the corresponding projected WinRT interface. If so, we
        // tail-call there.
        m_pcsSetup->EmitLoadThis();
        EmitLoadStubContext(m_pcsSetup, dwStubFlags);
        m_pcsSetup->EmitCALL(METHOD__STUBHELPERS__SHOULD_CALL_WINRT_INTERFACE, 2, 1);

        ILCodeLabel *pNoRedirection = m_pcsSetup->NewCodeLabel();
        m_pcsSetup->EmitBRFALSE(pNoRedirection);

        MethodDesc *pAdapterMD = WinRTInterfaceRedirector::GetStubMethodForRedirectedInterfaceMethod(
            GetTargetMD(),
            TypeHandle::Interop_ManagedToNative);

        CONSISTENCY_CHECK(pAdapterMD != NULL && !pAdapterMD->HasMethodInstantiation());

        m_pcsSetup->EmitJMP(m_pcsSetup->GetToken(pAdapterMD));

        m_pcsSetup->EmitLabel(pNoRedirection);
    }
#endif // FEATURE_COMINTEROP

    if (SF_IsForwardStub(dwStubFlags))
    {

        if (SF_IsStubWithCctorTrigger(dwStubFlags))
        {
            EmitLoadStubContext(m_pcsSetup, dwStubFlags);
            m_pcsSetup->EmitCALL(METHOD__STUBHELPERS__INIT_DECLARING_TYPE, 1, 0);
        }
    }
    else
    {
#ifdef MDA_SUPPORTED
        if (!SF_IsNGENedStub(dwStubFlags) && MDA_GET_ASSISTANT(GcUnmanagedToManaged))
        {
            EmitCallGcCollectForMDA(m_pcsSetup, dwStubFlags);
        }
#endif // MDA_SUPPORTED

        if (SF_IsDelegateStub(dwStubFlags))
        {
#if defined(MDA_SUPPORTED)
            // GC was induced (gcUnmanagedToManagedMDA), arguments have been marshaled, and we are about
            // to touch the UMEntryThunk and extract the delegate target from it so this is the right time
            // to do the collected delegate MDA check.

            // The call to CheckCollectedDelegateMDA is emitted regardless of whether the MDA is on at the
            // moment. This is to avoid having to ignore NGENed stubs without the call just as we do for
            // the GC MDA (callbackOncollectedDelegateMDA is turned on under managed debugger by default
            // so the impact would be substantial). The helper bails out fast if the MDA is not enabled.
            EmitLoadStubContext(m_pcsDispatch, dwStubFlags);
            m_pcsDispatch->EmitCALL(METHOD__STUBHELPERS__CHECK_COLLECTED_DELEGATE_MDA, 1, 0);
#endif // MDA_SUPPORTED

            //
            // recover delegate object from UMEntryThunk

            EmitLoadStubContext(m_pcsDispatch, dwStubFlags); // load UMEntryThunk*
            
            m_pcsDispatch->EmitLDC(offsetof(UMEntryThunk, m_pObjectHandle));
            m_pcsDispatch->EmitADD();
            m_pcsDispatch->EmitLDIND_I();      // get OBJECTHANDLE
            m_pcsDispatch->EmitLDIND_REF();    // get Delegate object
            m_pcsDispatch->EmitLDFLD(GetToken(MscorlibBinder::GetField(FIELD__DELEGATE__TARGET)));
        }
    }

    m_pCleanupTryBeginLabel = NewCodeLabel();
    m_pcsMarshal->EmitLabel(m_pCleanupTryBeginLabel);
}

void NDirectStubLinker::End(DWORD dwStubFlags)
{
    STANDARD_VM_CONTRACT;

    ILCodeStream* pcs = m_pcsUnmarshal;

    bool hasTryCatchForHRESULT = SF_IsReverseCOMStub(dwStubFlags) 
                                    && !SF_IsFieldGetterStub(dwStubFlags) 
                                    && !SF_IsFieldSetterStub(dwStubFlags);

    //
    // Create a local for the return value and store the return value in it.
    //
    if (IsCleanupNeeded() || hasTryCatchForHRESULT)
    {
        // Save the return value if necessary, since the IL stack will be emptied when we leave a try block.
        LocalDesc locDescRetVal;
        if (SF_IsForwardStub(dwStubFlags))
        {
            GetStubReturnType(&locDescRetVal);
        }
        else
        {
            GetStubTargetReturnType(&locDescRetVal);
        }

        if (!( (locDescRetVal.cbType == 1) && (locDescRetVal.ElementType[0] == ELEMENT_TYPE_VOID) ))
        {
            m_dwRetValLocalNum = m_pcsRetUnmarshal->NewLocal(locDescRetVal);
            if (SF_IsReverseStub(dwStubFlags) && StubHasVoidReturnType())
            {
                // if the target returns void and we are doing HRESULT swapping, S_OK is loaded
                // in the unmarshal stream
                m_pcsUnmarshal->EmitSTLOC(m_dwRetValLocalNum);
            }
            else
            {
                // otherwise the return value is loaded in the return unmarshal stream
                m_pcsRetUnmarshal->EmitSTLOC(m_dwRetValLocalNum);
            }
        }
        else if (hasTryCatchForHRESULT && (locDescRetVal.ElementType[0] != ELEMENT_TYPE_VOID))
        {
            m_dwRetValLocalNum = m_pcsRetUnmarshal->NewLocal(locDescRetVal);
        }
    }

    //
    // Emit end-of-try and end-of-finally code for the try/finally
    //
    if (IsCleanupNeeded())
    {
        m_pCleanupFinallyEndLabel = NewCodeLabel();
        m_pCleanupTryEndLabel = NewCodeLabel();

        if (IsExceptionCleanupNeeded())
        {
            // if we made it here, no exception has been thrown
            EmitSetArgMarshalIndex(m_pcsUnmarshal, CLEANUP_INDEX_ALL_DONE);
        }

        // Emit a leave at the end of the try block.  If we have an outer try/catch, we need
        // to leave to the beginning of the ExceptionHandler code stream, which follows the 
        // Cleanup code stream.  If we don't, we can just leave to the tail end of the
        // Unmarshal code stream where we'll emit our RET.  

        ILCodeLabel* pLeaveTarget = m_pCleanupTryEndLabel;
        if (hasTryCatchForHRESULT)
        {
            pLeaveTarget = m_pCleanupFinallyEndLabel;
        }

        m_pcsUnmarshal->EmitLEAVE(pLeaveTarget);
        m_pcsUnmarshal->EmitLabel(m_pCleanupTryEndLabel);

        // Emit a call to destroy the clean-up list if needed.
        if (IsCleanupWorkListSetup())
        {
            LoadCleanupWorkList(m_pcsCleanup);
            m_pcsCleanup->EmitCALL(METHOD__STUBHELPERS__DESTROY_CLEANUP_LIST, 1, 0);
        }

        // Emit the endfinally.
        m_pcsCleanup->EmitENDFINALLY();
        m_pcsCleanup->EmitLabel(m_pCleanupFinallyEndLabel);
    }

#ifdef MDA_SUPPORTED
    if (SF_IsReverseStub(dwStubFlags) && !SF_IsNGENedStub(dwStubFlags) &&
        MDA_GET_ASSISTANT(GcManagedToUnmanaged))
    {
        EmitCallGcCollectForMDA(pcs, dwStubFlags);
    }
#endif // MDA_SUPPORTED

    if (IsExceptionCleanupNeeded())
    {
        m_pcsExceptionCleanup->EmitLabel(m_pSkipExceptionCleanupLabel);
    }

    // Reload the return value 
    if ((m_dwRetValLocalNum != (DWORD)-1) && !hasTryCatchForHRESULT)
    {
        pcs->EmitLDLOC(m_dwRetValLocalNum);
    }
}

void NDirectStubLinker::DoNDirect(ILCodeStream *pcsEmit, DWORD dwStubFlags, MethodDesc * pStubMD)
{
    STANDARD_VM_CONTRACT;
    if (SF_IsForwardStub(dwStubFlags)) // managed-to-native
    {

        if (SF_IsDelegateStub(dwStubFlags)) // delegate invocation
        {
            // get the delegate unmanaged target - we call a helper instead of just grabbing
            // the _methodPtrAux field because we may need to intercept the call for host, MDA, etc.
            pcsEmit->EmitLoadThis();
#ifdef _WIN64
            // on AMD64 GetDelegateTarget will return address of the generic stub for host when we are hosted
            // and update the secret argument with real target - the secret arg will be embedded in the
            // InlinedCallFrame by the JIT and fetched via TLS->Thread->Frame->Datum by the stub for host
            pcsEmit->EmitCALL(METHOD__STUBHELPERS__GET_STUB_CONTEXT_ADDR, 0, 1);
#else // _WIN64
            // we don't need to do this on x86 because stub for host is generated dynamically per target
            pcsEmit->EmitLDNULL();
#endif // _WIN64
            pcsEmit->EmitCALL(METHOD__STUBHELPERS__GET_DELEGATE_TARGET, 2, 1);
        }
        else // direct invocation
        {
            if (SF_IsCALLIStub(dwStubFlags)) // unmanaged CALLI
            {
                // if we ever NGEN CALLI stubs, this would have to be done differently
                _ASSERTE(!SF_IsNGENedStub(dwStubFlags));

#ifndef CROSSGEN_COMPILE

#ifdef _TARGET_X86_

                {
                    // for managed-to-unmanaged CALLI that requires marshaling, the target is passed
                    // as the secret argument to the stub by GenericPInvokeCalliHelper (asmhelpers.asm)
                    EmitLoadStubContext(pcsEmit, dwStubFlags);
                }


#else // _TARGET_X86_

                {
                    // the secret arg has been shifted to left and ORed with 1 (see code:GenericPInvokeCalliHelper)
                    EmitLoadStubContext(pcsEmit, dwStubFlags);
#ifndef _TARGET_ARM_
                    pcsEmit->EmitLDC(1);
                    pcsEmit->EmitSHR_UN();
#endif
                }

#endif // _TARGET_X86_

#endif // CROSSGEN_COMPILE
            }
            else
#ifdef FEATURE_COMINTEROP
            if (!SF_IsCOMStub(dwStubFlags)) // forward P/Invoke
#endif // FEATURE_COMINTEROP
            {
                EmitLoadStubContext(pcsEmit, dwStubFlags);

                {
                    // Perf: inline the helper for now
                    //pcsEmit->EmitCALL(METHOD__STUBHELPERS__GET_NDIRECT_TARGET, 1, 1);
                    pcsEmit->EmitLDC(offsetof(NDirectMethodDesc, ndirect.m_pWriteableData));
                    pcsEmit->EmitADD();
                    pcsEmit->EmitLDIND_I();
                    pcsEmit->EmitLDIND_I();
                }
            }
#ifdef FEATURE_COMINTEROP
            else
            {
                // this is a CLR -> COM call
                // the target has been computed by StubHelpers::GetCOMIPFromRCW
                pcsEmit->EmitLDLOC(m_dwTargetEntryPointLocalNum);
            }
#endif // FEATURE_COMINTEROP
        }
    }
    else // native-to-managed
    {
        if (SF_IsDelegateStub(dwStubFlags)) // reverse P/Invoke via delegate
        {
            int tokDelegate_methodPtr = pcsEmit->GetToken(MscorlibBinder::GetField(FIELD__DELEGATE__METHOD_PTR));

            EmitLoadStubContext(pcsEmit, dwStubFlags);
            pcsEmit->EmitLDC(offsetof(UMEntryThunk, m_pObjectHandle));
            pcsEmit->EmitADD();
            pcsEmit->EmitLDIND_I();                    // Get OBJECTHANDLE
            pcsEmit->EmitLDIND_REF();                  // Get Delegate object
            pcsEmit->EmitLDFLD(tokDelegate_methodPtr); // get _methodPtr 
        }
#ifdef FEATURE_COMINTEROP
        else if (SF_IsCOMStub(dwStubFlags)) // COM -> CLR call
        {
            // managed target is passed directly in the secret argument
            EmitLoadStubContext(pcsEmit, dwStubFlags);
        }
#endif // FEATURE_COMINTEROP
        else // direct reverse P/Invoke (CoreCLR hosting)
        {
            EmitLoadStubContext(pcsEmit, dwStubFlags);
            CONSISTENCY_CHECK(0 == offsetof(UMEntryThunk, m_pManagedTarget)); // if this changes, just add back the EmitLDC/EmitADD below
            // pcsEmit->EmitLDC(offsetof(UMEntryThunk, m_pManagedTarget));
            // pcsEmit->EmitADD();
            pcsEmit->EmitLDIND_I();  // Get UMEntryThunk::m_pManagedTarget
        }
    }

    // For managed-to-native calls, the rest of the work is done by the JIT. It will
    // erect InlinedCallFrame, flip GC mode, and use the specified calling convention
    // to call the target. For native-to-managed calls, this is an ordinary managed
    // CALLI and nothing special happens.
    pcsEmit->EmitCALLI(TOKEN_ILSTUB_TARGET_SIG, 0, m_iTargetStackDelta);
}

void NDirectStubLinker::EmitLogNativeArgument(ILCodeStream* pslILEmit, DWORD dwPinnedLocal)
{
    STANDARD_VM_CONTRACT;

    if (SF_IsForwardPInvokeStub(m_dwStubFlags) && !SF_IsForwardDelegateStub(m_dwStubFlags))
    {
        // get the secret argument via intrinsic
        pslILEmit->EmitCALL(METHOD__STUBHELPERS__GET_STUB_CONTEXT, 0, 1);
    }
    else
    {
        // no secret argument
        pslILEmit->EmitLoadNullPtr();
    }

    pslILEmit->EmitLDLOC(dwPinnedLocal);

    pslILEmit->EmitCALL(METHOD__STUBHELPERS__LOG_PINNED_ARGUMENT, 2, 0);
}

void NDirectStubLinker::GetCleanupFinallyOffsets(ILStubEHClause * pClause)
{
    CONTRACTL
    {
        STANDARD_VM_CHECK;
        PRECONDITION(CheckPointer(pClause));
    }
    CONTRACTL_END;

    if (m_pCleanupFinallyEndLabel)
    {
        _ASSERTE(m_pCleanupFinallyBeginLabel);
        _ASSERTE(m_pCleanupTryBeginLabel);
        _ASSERTE(m_pCleanupTryEndLabel);

        pClause->kind = ILStubEHClause::kFinally;
        pClause->dwTryBeginOffset      = (DWORD)m_pCleanupTryBeginLabel->GetCodeOffset();
        pClause->cbTryLength           = (DWORD)m_pCleanupTryEndLabel->GetCodeOffset() - pClause->dwTryBeginOffset;
        pClause->dwHandlerBeginOffset  = (DWORD)m_pCleanupFinallyBeginLabel->GetCodeOffset();
        pClause->cbHandlerLength       = (DWORD)m_pCleanupFinallyEndLabel->GetCodeOffset() - pClause->dwHandlerBeginOffset;
    }
}

void NDirectStubLinker::ClearCode()
{
    WRAPPER_NO_CONTRACT;
    ILStubLinker::ClearCode();
    
    m_pCleanupTryBeginLabel = 0;
    m_pCleanupTryEndLabel = 0;
    m_pCleanupFinallyBeginLabel = 0;
    m_pCleanupFinallyEndLabel = 0;
}

#ifdef PROFILING_SUPPORTED
DWORD NDirectStubLinker::EmitProfilerBeginTransitionCallback(ILCodeStream* pcsEmit, DWORD dwStubFlags)
{
    STANDARD_VM_CONTRACT;

    if (SF_IsForwardDelegateStub(dwStubFlags) || SF_IsCALLIStub(dwStubFlags))
    {
        // secret argument does not contain MD nor UMEntryThunk
        pcsEmit->EmitLoadNullPtr();
    }
    else
    {
        EmitLoadStubContext(pcsEmit, dwStubFlags);
    }

    if (SF_IsForwardStub(dwStubFlags))
    {
        pcsEmit->EmitLDLOC(GetThreadLocalNum());
    }
    else
    {
        // we use a null pThread to indicate reverse interop
        pcsEmit->EmitLDC(NULL);
    }
        
    // In the unmanaged delegate case, we need the "this" object to retrieve the MD 
    // in StubHelpers::ProfilerEnterCallback().
    if (SF_IsDelegateStub(dwStubFlags))
    {
        if (SF_IsForwardStub(dwStubFlags))
        {
            pcsEmit->EmitLoadThis();
        }
        else
        {
            EmitLoadStubContext(pcsEmit, dwStubFlags); // load UMEntryThunk*
            pcsEmit->EmitLDC(offsetof(UMEntryThunk, m_pObjectHandle));
            pcsEmit->EmitADD();
            pcsEmit->EmitLDIND_I();      // get OBJECTHANDLE
            pcsEmit->EmitLDIND_REF();    // get Delegate object
        }
    }
    else
    {
        pcsEmit->EmitLDC(NULL);
    }
    pcsEmit->EmitCALL(METHOD__STUBHELPERS__PROFILER_BEGIN_TRANSITION_CALLBACK, 3, 1);

    // Store the MD for StubHelpers::ProfilerLeaveCallback().
    DWORD dwMethodDescLocalNum = pcsEmit->NewLocal(ELEMENT_TYPE_I);
    pcsEmit->EmitSTLOC(dwMethodDescLocalNum);
    return dwMethodDescLocalNum;
}

void NDirectStubLinker::EmitProfilerEndTransitionCallback(ILCodeStream* pcsEmit, DWORD dwStubFlags, DWORD dwMethodDescLocalNum)
{
    STANDARD_VM_CONTRACT;

    pcsEmit->EmitLDLOC(dwMethodDescLocalNum);
    if (SF_IsReverseStub(dwStubFlags))
    {
        // we use a null pThread to indicate reverse interop
        pcsEmit->EmitLDC(NULL);
    }
    else
    {
        pcsEmit->EmitLDLOC(GetThreadLocalNum());
    }
    pcsEmit->EmitCALL(METHOD__STUBHELPERS__PROFILER_END_TRANSITION_CALLBACK, 2, 0);
}
#endif // PROFILING_SUPPPORTED

#ifdef VERIFY_HEAP
void NDirectStubLinker::EmitValidateLocal(ILCodeStream* pcsEmit, DWORD dwLocalNum, bool fIsByref, DWORD dwStubFlags)
{
    STANDARD_VM_CONTRACT;

    pcsEmit->EmitLDLOC(dwLocalNum);

    if (SF_IsDelegateStub(dwStubFlags))
    {
        pcsEmit->EmitLoadNullPtr();
        pcsEmit->EmitLoadThis();
    }
    else if (SF_IsCALLIStub(dwStubFlags))
    {
        pcsEmit->EmitLoadNullPtr();
        pcsEmit->EmitLDNULL();
    }
    else
    {
        // P/Invoke, CLR->COM
        EmitLoadStubContext(pcsEmit, dwStubFlags);
        pcsEmit->EmitLDNULL();
    }

    if (fIsByref)
    {
        // StubHelpers.ValidateByref(byref, pMD, pThis)
        pcsEmit->EmitCALL(METHOD__STUBHELPERS__VALIDATE_BYREF, 3, 0);
    }
    else
    {
        // StubHelpers.ValidateObject(obj, pMD, pThis)
        pcsEmit->EmitCALL(METHOD__STUBHELPERS__VALIDATE_OBJECT, 3, 0);
    }
}

void NDirectStubLinker::EmitObjectValidation(ILCodeStream* pcsEmit, DWORD dwStubFlags)
{
    STANDARD_VM_CONTRACT;

    // generate validation callouts for pinned locals
    CQuickBytes qbLocalSig;
    DWORD cbSig = GetLocalSigSize();
    
    qbLocalSig.AllocThrows(cbSig);
    PCOR_SIGNATURE pSig = (PCOR_SIGNATURE)qbLocalSig.Ptr();

    GetLocalSig(pSig, cbSig);
    SigPointer ptr(pSig, cbSig);

    IfFailThrow(ptr.GetData(NULL)); // IMAGE_CEE_CS_CALLCONV_LOCAL_SIG

    ULONG numLocals;
    IfFailThrow(ptr.GetData(&numLocals));

    for (ULONG i = 0; i < numLocals; i++)
    {
        BYTE modifier;
        IfFailThrow(ptr.PeekByte(&modifier));
        if (modifier == ELEMENT_TYPE_PINNED)
        {
            IfFailThrow(ptr.GetByte(NULL));
            IfFailThrow(ptr.PeekByte(&modifier));
            EmitValidateLocal(pcsEmit, i, (modifier == ELEMENT_TYPE_BYREF), dwStubFlags);
        }

        IfFailThrow(ptr.SkipExactlyOne());
    }
}
#endif // VERIFY_HEAP

// Loads the 'secret argument' passed to the stub.
void NDirectStubLinker::EmitLoadStubContext(ILCodeStream* pcsEmit, DWORD dwStubFlags)
{
    STANDARD_VM_CONTRACT;

    CONSISTENCY_CHECK(!SF_IsForwardDelegateStub(dwStubFlags));
    CONSISTENCY_CHECK(!SF_IsFieldGetterStub(dwStubFlags) && !SF_IsFieldSetterStub(dwStubFlags));

#ifdef FEATURE_COMINTEROP
    if (SF_IsWinRTDelegateStub(dwStubFlags) && SF_IsForwardStub(dwStubFlags))
    {
        // we have the delegate 'this' but we need the EEImpl/Instantiated 'Invoke' MD pointer
        // (Delegate.GetInvokeMethod does not return exact instantiated MD so we call our own helper)
        pcsEmit->EmitLoadThis();
        pcsEmit->EmitCALL(METHOD__STUBHELPERS__GET_DELEGATE_INVOKE_METHOD, 1, 1);
    }
    else
#endif // FEATURE_COMINTEROP
    {
        // get the secret argument via intrinsic
        pcsEmit->EmitCALL(METHOD__STUBHELPERS__GET_STUB_CONTEXT, 0, 1);
    }
}

#ifdef MDA_SUPPORTED
void NDirectStubLinker::EmitCallGcCollectForMDA(ILCodeStream *pcsEmit, DWORD dwStubFlags)
{
    STANDARD_VM_CONTRACT;

    ILCodeLabel *pSkipGcLabel = NULL;

    if (SF_IsForwardPInvokeStub(dwStubFlags) &&
        !SF_IsDelegateStub(dwStubFlags) &&
        !SF_IsCALLIStub(dwStubFlags))
    {
        // don't call GC if this is a QCall
        EmitLoadStubContext(pcsEmit, dwStubFlags);
        pcsEmit->EmitCALL(METHOD__STUBHELPERS__IS_QCALL, 1, 1);

        pSkipGcLabel = pcsEmit->NewCodeLabel();
        pcsEmit->EmitBRTRUE(pSkipGcLabel);
    }

    pcsEmit->EmitCALL(METHOD__STUBHELPERS__TRIGGER_GC_FOR_MDA, 0, 0);

    if (pSkipGcLabel != NULL)
    {
        pcsEmit->EmitLabel(pSkipGcLabel);
    }
}
#endif // MDA_SUPPORTED

#ifdef FEATURE_COMINTEROP

class DispatchStubState : public StubState // For CLR-to-COM late-bound/eventing calls
{
public:
    DispatchStubState()
        : m_dwStubFlags(0),
          m_lateBoundFlags(0)
    {
        WRAPPER_NO_CONTRACT;
    }

    void SetLastError(BOOL fSetLastError)
    {
        LIMITED_METHOD_CONTRACT;

        CONSISTENCY_CHECK(!fSetLastError);
    }

    void BeginEmit(DWORD dwStubFlags)
    {
        LIMITED_METHOD_CONTRACT;

        CONSISTENCY_CHECK(SF_IsCOMStub(dwStubFlags));
        m_dwStubFlags = dwStubFlags;
    }

    void MarshalReturn(MarshalInfo* pInfo, int argOffset)
    {
        CONTRACTL
        {
            STANDARD_VM_CHECK;

            PRECONDITION(CheckPointer(pInfo));
        }
        CONTRACTL_END;
    }

    void MarshalArgument(MarshalInfo* pInfo, int argOffset, UINT nativeStackOffset)
    {
        CONTRACTL
        {
            STANDARD_VM_CHECK;
            PRECONDITION(CheckPointer(pInfo));
        }
        CONTRACTL_END;

        if (SF_IsCOMLateBoundStub(m_dwStubFlags) && pInfo->GetDispWrapperType() != 0)
        {
            m_lateBoundFlags |= ComPlusCallInfo::kRequiresArgumentWrapping;
        }
    }

    void MarshalLCID(int argIdx)
    {
        LIMITED_METHOD_CONTRACT;
    }

#ifdef FEATURE_COMINTEROP
    void MarshalHiddenLengthArgument(MarshalInfo *, BOOL)
    {
        LIMITED_METHOD_CONTRACT;
    }
    void MarshalFactoryReturn()
    {
        LIMITED_METHOD_CONTRACT;
        UNREACHABLE();
    }
#endif // FEATURE_COMINTEROP

    void EmitInvokeTarget(MethodDesc *pStubMD)
    {
        LIMITED_METHOD_CONTRACT;
        UNREACHABLE_MSG("Should never come to DispatchStubState::EmitInvokeTarget");
    }

    void FinishEmit(MethodDesc *pMD)
    {
        STANDARD_VM_CONTRACT;

        // set flags directly on the interop MD
        _ASSERTE(pMD->IsComPlusCall());

        ((ComPlusCallMethodDesc *)pMD)->SetLateBoundFlags(m_lateBoundFlags);
    }

protected:
    DWORD        m_dwStubFlags;
    BYTE         m_lateBoundFlags; // ComPlusCallMethodDesc::Flags
};

#endif // FEATURE_COMINTEROP


void PInvokeStaticSigInfo::PreInit(Module* pModule, MethodTable * pMT)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;
    
    // initialize data members
    m_wFlags = 0;
    m_pModule = pModule;
    m_callConv = (CorPinvokeMap)0;
    SetBestFitMapping (TRUE);
    SetThrowOnUnmappableChar (FALSE);
    SetLinkFlags (nlfNone);
    SetCharSet (nltAnsi);
    m_error = 0;

    // assembly/type level m_bestFit & m_bThrowOnUnmappableChar
    BOOL bBestFit;
    BOOL bThrowOnUnmappableChar;

    if (pMT != NULL)
    {
        EEClass::GetBestFitMapping(pMT, &bBestFit, &bThrowOnUnmappableChar);
    }
    else
    {
        ReadBestFitCustomAttribute(m_pModule->GetMDImport(), mdTypeDefNil, &bBestFit, &bThrowOnUnmappableChar);
    }

    SetBestFitMapping (bBestFit);
    SetThrowOnUnmappableChar (bThrowOnUnmappableChar);
}

void PInvokeStaticSigInfo::PreInit(MethodDesc* pMD)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    PreInit(pMD->GetModule(), pMD->GetMethodTable());
    SetIsStatic (pMD->IsStatic());
    m_sig = pMD->GetSignature();
    if (pMD->IsEEImpl())
    {
        CONSISTENCY_CHECK(pMD->GetMethodTable()->IsDelegate());
        SetIsDelegateInterop(TRUE);
    }
}

PInvokeStaticSigInfo::PInvokeStaticSigInfo(
    MethodDesc* pMD, LPCUTF8 *pLibName, LPCUTF8 *pEntryPointName, ThrowOnError throwOnError) 
{ 
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;
    
    DllImportInit(pMD, pLibName, pEntryPointName);

    if (throwOnError)
        ReportErrors();
}

PInvokeStaticSigInfo::PInvokeStaticSigInfo(MethodDesc* pMD, ThrowOnError throwOnError)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;

        PRECONDITION(CheckPointer(pMD));
    }
    CONTRACTL_END;

    HRESULT hr = S_OK;

    MethodTable * pMT = pMD->GetMethodTable();

    if (!pMT->IsDelegate())
    {
        DllImportInit(pMD, NULL, NULL);
        return;
    }

    // initialize data members to defaults
    PreInit(pMD);

    // System.Runtime.InteropServices.UnmanagedFunctionPointerAttribute
    BYTE* pData = NULL;
    LONG cData = 0;
    CorPinvokeMap callConv = (CorPinvokeMap)0;

    HRESULT hRESULT = pMT->GetMDImport()->GetCustomAttributeByName(
        pMT->GetCl(), g_UnmanagedFunctionPointerAttribute, (const VOID **)(&pData), (ULONG *)&cData);
    IfFailThrow(hRESULT);
    if(cData != 0)
    {
        CustomAttributeParser ca(pData, cData);

        CaArg args[1];
        args[0].InitEnum(SERIALIZATION_TYPE_I4, (ULONG)m_callConv);

        IfFailGo(ParseKnownCaArgs(ca, args, lengthof(args)));

        enum UnmanagedFunctionPointerNamedArgs
        {
            MDA_CharSet,
            MDA_BestFitMapping,
            MDA_ThrowOnUnmappableChar,
            MDA_SetLastError,
            MDA_PreserveSig,
            MDA_Last,
        };

        CaNamedArg namedArgs[MDA_Last];
        namedArgs[MDA_CharSet].InitI4FieldEnum("CharSet", "System.Runtime.InteropServices.CharSet", (ULONG)GetCharSet());
        namedArgs[MDA_BestFitMapping].InitBoolField("BestFitMapping", (ULONG)GetBestFitMapping());
        namedArgs[MDA_ThrowOnUnmappableChar].InitBoolField("ThrowOnUnmappableChar", (ULONG)GetThrowOnUnmappableChar());
        namedArgs[MDA_SetLastError].InitBoolField("SetLastError", 0);
        namedArgs[MDA_PreserveSig].InitBoolField("PreserveSig", 0);

        IfFailGo(ParseKnownCaNamedArgs(ca, namedArgs, lengthof(namedArgs)));

        callConv = (CorPinvokeMap)(args[0].val.u4 << 8);
        CorNativeLinkType nlt = (CorNativeLinkType)0;

        // XXX Tue 07/19/2005
        // Keep in sync with the handling of CorPInvokeMap in
        // PInvokeStaticSigInfo::DllImportInit.
        switch( namedArgs[MDA_CharSet].val.u4 )
        {
        case 0:
        case nltAnsi:
            nlt = nltAnsi; break;
        case nltUnicode:
        case nltAuto:   // Since Win9x isn't supported anymore, nltAuto always represents unicode strings.
            nlt = nltUnicode; break;
        default:
            hr = E_FAIL; goto ErrExit;
        }
        SetCharSet ( nlt );
        SetBestFitMapping (namedArgs[MDA_BestFitMapping].val.u1);
        SetThrowOnUnmappableChar (namedArgs[MDA_ThrowOnUnmappableChar].val.u1);
        if (namedArgs[MDA_SetLastError].val.u1) 
            SetLinkFlags ((CorNativeLinkFlags)(nlfLastError | GetLinkFlags()));
        if (namedArgs[MDA_PreserveSig].val.u1)
            SetLinkFlags ((CorNativeLinkFlags)(nlfNoMangle | GetLinkFlags()));
    }

            
ErrExit:    
    if (hr != S_OK)
        SetError(IDS_EE_NDIRECT_BADNATL);   

    InitCallConv(callConv, pMD->IsVarArg()); 

    if (throwOnError)
        ReportErrors();   
}

PInvokeStaticSigInfo::PInvokeStaticSigInfo(
    Signature sig, Module* pModule, ThrowOnError throwOnError)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;

        PRECONDITION(CheckPointer(pModule));
    }
    CONTRACTL_END;

    PreInit(pModule, NULL);
    m_sig = sig;
    SetIsStatic (!(MetaSig::GetCallingConvention(pModule, sig) & IMAGE_CEE_CS_CALLCONV_HASTHIS));
    InitCallConv((CorPinvokeMap)0, FALSE);
    
    if (throwOnError)
        ReportErrors();    
}

void PInvokeStaticSigInfo::DllImportInit(MethodDesc* pMD, LPCUTF8 *ppLibName, LPCUTF8 *ppEntryPointName)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;

        PRECONDITION(CheckPointer(pMD));

        // These preconditions to prevent multithreaded regression 
        // where pMD->ndirect.m_szLibName was passed in directly, cleared 
        // by this API, then accessed on another thread before being reset here.
        PRECONDITION(CheckPointer(ppLibName, NULL_OK) && (!ppLibName || *ppLibName == NULL)); 
        PRECONDITION(CheckPointer(ppEntryPointName, NULL_OK) && (!ppEntryPointName || *ppEntryPointName == NULL));
    }
    CONTRACTL_END;

    // initialize data members to defaults
    PreInit(pMD);

    // System.Runtime.InteropServices.DllImportAttribute
    IMDInternalImport  *pInternalImport = pMD->GetMDImport();
    CorPinvokeMap mappingFlags = pmMaxValue;
    mdModuleRef modref = mdModuleRefNil;
    if (FAILED(pInternalImport->GetPinvokeMap(pMD->GetMemberDef(), (DWORD*)&mappingFlags, ppEntryPointName, &modref)))
    {
        InitCallConv((CorPinvokeMap)0, pMD->IsVarArg());
        return;
    }
    
    // out parameter pEntryPointName
    if (ppEntryPointName && *ppEntryPointName == NULL)
        *ppEntryPointName = pMD->GetName();
    
    // out parameter pLibName
    if (ppLibName != NULL)
    {
        if (FAILED(pInternalImport->GetModuleRefProps(modref, ppLibName)))
        {
            SetError(IDS_CLASSLOAD_BADFORMAT);
            return;
        }
    }
    
    // m_callConv
    InitCallConv((CorPinvokeMap)(mappingFlags & pmCallConvMask), pMD->IsVarArg());
    
    // m_bestFit
    CorPinvokeMap bestFitMask = (CorPinvokeMap)(mappingFlags & pmBestFitMask);
    if (bestFitMask == pmBestFitEnabled)
        SetBestFitMapping (TRUE);
    else if (bestFitMask == pmBestFitDisabled)
        SetBestFitMapping (FALSE);

    // m_bThrowOnUnmappableChar
    CorPinvokeMap unmappableMask = (CorPinvokeMap)(mappingFlags & pmThrowOnUnmappableCharMask);
    if (unmappableMask == pmThrowOnUnmappableCharEnabled)
        SetThrowOnUnmappableChar (TRUE);
    else if (unmappableMask == pmThrowOnUnmappableCharDisabled)
        SetThrowOnUnmappableChar (FALSE);

    // inkFlags : CorPinvoke -> CorNativeLinkFlags
    if (mappingFlags & pmSupportsLastError)
        SetLinkFlags ((CorNativeLinkFlags)(GetLinkFlags() | nlfLastError));
    if (mappingFlags & pmNoMangle)
        SetLinkFlags ((CorNativeLinkFlags)(GetLinkFlags() | nlfNoMangle));

    // XXX Tue 07/19/2005
    // Keep in sync with the handling of CorNativeLinkType in
    // PInvokeStaticSigInfo::PInvokeStaticSigInfo.
    
    // charset : CorPinvoke -> CorNativeLinkType
    CorPinvokeMap charSetMask = (CorPinvokeMap)(mappingFlags & (pmCharSetNotSpec | pmCharSetAnsi | pmCharSetUnicode | pmCharSetAuto));
    if (charSetMask == pmCharSetNotSpec || charSetMask == pmCharSetAnsi)
    {
        SetCharSet (nltAnsi);
    }
    else if (charSetMask == pmCharSetUnicode || charSetMask == pmCharSetAuto)
    {
        // Since Win9x isn't supported anymore, pmCharSetAuto always represents unicode strings.
        SetCharSet (nltUnicode);
    }
    else
    {
        SetError(IDS_EE_NDIRECT_BADNATL);
    }
}

inline CorPinvokeMap GetDefaultCallConv(BOOL bIsVarArg)
{
#ifdef PLATFORM_UNIX
    return pmCallConvCdecl;
#else // PLATFORM_UNIX
    return bIsVarArg ? pmCallConvCdecl : pmCallConvStdcall;
#endif // !PLATFORM_UNIX
}

void PInvokeStaticSigInfo::InitCallConv(CorPinvokeMap callConv, BOOL bIsVarArg)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END

    // Convert WinAPI methods to either StdCall or CDecl based on if they are varargs or not.
    if (callConv == pmCallConvWinapi)
        callConv = GetDefaultCallConv(bIsVarArg);

    CorPinvokeMap sigCallConv = (CorPinvokeMap)0;
    BOOL fSuccess = MetaSig::GetUnmanagedCallingConvention(m_pModule, m_sig.GetRawSig(), m_sig.GetRawSigLen(), &sigCallConv);

    if (!fSuccess)
    {
        SetError(IDS_EE_NDIRECT_BADNATL); //Bad metadata format
    }

    // Do the same WinAPI to StdCall or CDecl for the signature calling convention as well. We need
    // to do this before we check to make sure the PInvoke map calling convention and the 
    // signature calling convention match for compatibility reasons.
    if (sigCallConv == pmCallConvWinapi)
        sigCallConv = GetDefaultCallConv(bIsVarArg);

    if (callConv != 0 && sigCallConv != 0 && callConv != sigCallConv)
        SetError(IDS_EE_NDIRECT_BADNATL_CALLCONV);

    if (callConv == 0 && sigCallConv == 0)
        m_callConv = GetDefaultCallConv(bIsVarArg);
    else if (callConv != 0)
        m_callConv = callConv;
    else
        m_callConv = sigCallConv;

    if (bIsVarArg && m_callConv != pmCallConvCdecl)
        SetError(IDS_EE_NDIRECT_BADNATL_VARARGS_CALLCONV);
}

void PInvokeStaticSigInfo::ReportErrors()
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    if (m_error != 0)
        COMPlusThrow(kTypeLoadException, m_error);      
}


//---------------------------------------------------------
// Does a class or method have a NAT_L CustomAttribute?
//
// S_OK    = yes
// S_FALSE = no
// FAILED  = unknown because something failed.
//---------------------------------------------------------
/*static*/
HRESULT NDirect::HasNAT_LAttribute(IMDInternalImport *pInternalImport, mdToken token, DWORD dwMemberAttrs)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;

        PRECONDITION(CheckPointer(pInternalImport));
        PRECONDITION(TypeFromToken(token) == mdtMethodDef);
    }
    CONTRACTL_END;

    // Check method flags first before trying to find the custom value
    if (!IsReallyMdPinvokeImpl(dwMemberAttrs))
        return S_FALSE;

    DWORD   mappingFlags;
    LPCSTR  pszImportName;
    mdModuleRef modref;

    if (SUCCEEDED(pInternalImport->GetPinvokeMap(token, &mappingFlags, &pszImportName, &modref)))
        return S_OK;

    return S_FALSE;
}


// Either MD or signature & module must be given.
/*static*/
BOOL NDirect::MarshalingRequired(MethodDesc *pMD, PCCOR_SIGNATURE pSig /*= NULL*/, Module *pModule /*= NULL*/)
{
    CONTRACTL
    {
        STANDARD_VM_CHECK;
        PRECONDITION(pMD != NULL || (pSig != NULL && pModule != NULL));
    }
    CONTRACTL_END;

    // As a by-product, when returning FALSE we will also set the native stack size to the MD if it's
    // an NDirectMethodDesc. This number is needed to link the P/Invoke (it determines the @n entry
    // point name suffix and affects alignment thunk generation on the Mac). If this method returns
    // TRUE, the stack size will be set when building the marshaling IL stub.
    DWORD dwStackSize = 0;
    CorPinvokeMap callConv = (CorPinvokeMap)0;

    if (pMD != NULL)
    {
        if (pMD->IsNDirect() || pMD->IsComPlusCall())
        {
            // HRESULT swapping is handled by stub
            if ((pMD->GetImplAttrs() & miPreserveSig) == 0)
                return TRUE;
        }

        // SetLastError is handled by stub
        PInvokeStaticSigInfo sigInfo(pMD);
        if (sigInfo.GetLinkFlags() & nlfLastError)
            return TRUE;

        // LCID argument is handled by stub
        if (GetLCIDParameterIndex(pMD) != -1)
            return TRUE;

        // making sure that cctor has run may be handled by stub
        if (pMD->IsNDirect() && ((NDirectMethodDesc *)pMD)->IsClassConstructorTriggeredByILStub())
            return TRUE;

        callConv = sigInfo.GetCallConv();
    }

    if (pSig == NULL)
    {
        PREFIX_ASSUME(pMD != NULL);

        pSig = pMD->GetSig();
        pModule = pMD->GetModule();
    }

    // Check to make certain that the signature only contains types that marshal trivially
    SigPointer ptr(pSig);
    IfFailThrow(ptr.GetCallingConvInfo(NULL));
    ULONG numArgs;
    IfFailThrow(ptr.GetData(&numArgs));
    numArgs++;   // +1 for return type

    // We'll need to parse parameter native types
    mdParamDef *pParamTokenArray = (mdParamDef *)_alloca(numArgs * sizeof(mdParamDef));
    IMDInternalImport *pMDImport = pModule->GetMDImport();

    SigTypeContext emptyTypeContext;

    mdMethodDef methodToken = mdMethodDefNil;
    if (pMD != NULL)
    {
        methodToken = pMD->GetMemberDef();
    }
    CollateParamTokens(pMDImport, methodToken, numArgs - 1, pParamTokenArray);

    for (ULONG i = 0; i < numArgs; i++)
    {
        SigPointer arg = ptr;
        CorElementType type;
        IfFailThrow(arg.PeekElemType(&type));

        switch (type)
        {
            case ELEMENT_TYPE_PTR:
            {
                IfFailThrow(arg.GetElemType(NULL)); // skip ELEMENT_TYPE_PTR
                IfFailThrow(arg.PeekElemType(&type));

                if (type == ELEMENT_TYPE_VALUETYPE)
                {
                    if ((arg.HasCustomModifier(pModule,
                                              "Microsoft.VisualC.NeedsCopyConstructorModifier",
                                              ELEMENT_TYPE_CMOD_REQD)) ||
                        (arg.HasCustomModifier(pModule,
                                              "System.Runtime.CompilerServices.IsCopyConstructed",
                                              ELEMENT_TYPE_CMOD_REQD)))
                    {
                        return TRUE;
                    }
                }
                if (i > 0) dwStackSize += sizeof(SLOT);
                break;
            }

            case ELEMENT_TYPE_INTERNAL:

                // this check is not functional in DAC and provides no security against a malicious dump
                // the DAC is prepared to receive an invalid type handle
#ifndef DACCESS_COMPILE
                if (pModule->IsSigInIL(arg.GetPtr()))
                    THROW_BAD_FORMAT(BFA_BAD_SIGNATURE, (Module*)pModule);
#endif

                /* Fall thru */

            case ELEMENT_TYPE_VALUETYPE:
            {
                TypeHandle hndArgType = arg.GetTypeHandleThrowing(pModule, &emptyTypeContext);

                // JIT can handle internal blittable value types
                if (!hndArgType.IsBlittable() && !hndArgType.IsEnum())
                {
                    return TRUE;
                }

                // return value is fine as long as it can be normalized to an integer
                if (i == 0)
                {
                    CorElementType normalizedType = hndArgType.GetInternalCorElementType();
                    if (normalizedType == ELEMENT_TYPE_VALUETYPE)
                    {
                        // it is a structure even after normalization
                        return TRUE;
                    }
                }
                else
                {
                    dwStackSize += StackElemSize(hndArgType.GetSize());
                }
                break;
            }
            
            case ELEMENT_TYPE_BOOLEAN:
            case ELEMENT_TYPE_CHAR:
            {
                // Bool requires marshaling
                // Char may require marshaling (MARSHAL_TYPE_ANSICHAR)
                return TRUE;
            }

            default:
            {
                if (CorTypeInfo::IsPrimitiveType(type) || type == ELEMENT_TYPE_FNPTR)
                {
                    if (i > 0) dwStackSize += StackElemSize(CorTypeInfo::Size(type));
                }
                else
                {
                    // other non-primitive type - requires marshaling
                    return TRUE;
                }
            }
        }

        // check for explicit MarshalAs
        NativeTypeParamInfo paramInfo;

        if (pParamTokenArray[i] != mdParamDefNil)
        {
            if (!ParseNativeTypeInfo(pParamTokenArray[i], pMDImport, &paramInfo) ||
                paramInfo.m_NativeType != NATIVE_TYPE_DEFAULT)
            {
                // Presence of MarshalAs does not necessitate marshaling (it could as well be the default
                // for the type), but it's a good enough heuristic. We definitely don't want to duplicate
                // the logic from code:MarshalInfo.MarshalInfo here.
                return TRUE;
            }
        }

        IfFailThrow(ptr.SkipExactlyOne());
    }

    if (!FitsInU2(dwStackSize))
        return TRUE;

    // do not set the stack size for varargs - the number is call site specific
    if (pMD != NULL && !pMD->IsVarArg())
    {
        if (pMD->IsNDirect())
        {
            ((NDirectMethodDesc *)pMD)->SetStackArgumentSize(static_cast<WORD>(dwStackSize), callConv);
        }
#ifdef FEATURE_COMINTEROP
        else if (pMD->IsComPlusCall())
        {
            // calling convention is always stdcall
            ((ComPlusCallMethodDesc *)pMD)->SetStackArgumentSize(static_cast<WORD>(dwStackSize));
        }
#endif // FEATURE_COMINTEROP
    }

    return FALSE;
}


// factorization of CreateNDirectStubWorker
static MarshalInfo::MarshalType DoMarshalReturnValue(MetaSig&           msig,
                                                     mdParamDef*        params,
                                                     CorNativeLinkType  nlType,
                                                     CorNativeLinkFlags nlFlags,
                                                     UINT               argidx,  // this is used for reverse pinvoke hresult swapping
                                                     StubState*         pss,
                                                     BOOL               fThisCall,
                                                     int                argOffset,
                                                     DWORD              dwStubFlags,
                                                     MethodDesc         *pMD,
                                                     UINT&              nativeStackOffset,
                                                     bool&              fStubNeedsCOM,
                                                     int                nativeArgIndex
                                                     DEBUG_ARG(LPCUTF8  pDebugName)
                                                     DEBUG_ARG(LPCUTF8  pDebugClassName)
                                                     )
{
    CONTRACTL
    {
        STANDARD_VM_CHECK;

        PRECONDITION(CheckPointer(params));
        PRECONDITION(CheckPointer(pss));
        PRECONDITION(CheckPointer(pMD, NULL_OK));
    }
    CONTRACTL_END;

    MarshalInfo::MarshalType marshalType = (MarshalInfo::MarshalType) 0xcccccccc;

    MarshalInfo::MarshalScenario ms;
#ifdef FEATURE_COMINTEROP
    if (SF_IsCOMStub(dwStubFlags))
    {
        if (SF_IsWinRTStub(dwStubFlags))
            ms = MarshalInfo::MARSHAL_SCENARIO_WINRT;
        else
            ms = MarshalInfo::MARSHAL_SCENARIO_COMINTEROP;
    }
    else
#endif // FEATURE_COMINTEROP
    {
        ms = MarshalInfo::MARSHAL_SCENARIO_NDIRECT;
    }

#ifdef FEATURE_COMINTEROP
    if (SF_IsWinRTCtorStub(dwStubFlags))
    {
        _ASSERTE(msig.GetReturnType() == ELEMENT_TYPE_VOID);
        _ASSERTE(SF_IsHRESULTSwapping(dwStubFlags));
        
        pss->MarshalFactoryReturn();
        nativeStackOffset += sizeof(LPVOID);
        if (SF_IsWinRTCompositionStub(dwStubFlags))
        {
            nativeStackOffset += 2 * sizeof(LPVOID);
        }
    }
    else
#endif // FEATURE_COMINTEROP
    if (msig.GetReturnType() != ELEMENT_TYPE_VOID)
    {
        MarshalInfo returnInfo(msig.GetModule(),
                                msig.GetReturnProps(),
                                msig.GetSigTypeContext(),
                                params[0],
                                ms,
                                nlType,
                                nlFlags,
                                FALSE,
                                argidx,
                                msig.NumFixedArgs(),
                                SF_IsBestFit(dwStubFlags),
                                SF_IsThrowOnUnmappableChar(dwStubFlags),
                                TRUE,
                                pMD,
                                TRUE
                                DEBUG_ARG(pDebugName)
                                DEBUG_ARG(pDebugClassName)
                                DEBUG_ARG(0)
                                );

        marshalType = returnInfo.GetMarshalType();

        fStubNeedsCOM |= returnInfo.MarshalerRequiresCOM();

#ifdef FEATURE_COMINTEROP
        if (marshalType == MarshalInfo::MARSHAL_TYPE_HIDDENLENGTHARRAY)
        {
            // Hidden length arrays are only valid with HRESULT swapping
            if (!SF_IsHRESULTSwapping(dwStubFlags))
            {
                COMPlusThrow(kMarshalDirectiveException, IDS_EE_COM_UNSUPPORTED_SIG);
            }

            // We should be safe to cast here - giant signatures will fail to marashal later with IDS_EE_SIGTOOCOMPLEX
            returnInfo.SetHiddenLengthParamIndex(static_cast<UINT16>(nativeArgIndex));

            // Inject the hidden argument so that it winds up at the end of the method signature
            pss->MarshalHiddenLengthArgument(&returnInfo, TRUE);
            nativeStackOffset += returnInfo.GetHiddenLengthParamStackSize();

            if (SF_IsReverseStub(dwStubFlags))
            {
                ++argOffset;
            }
        }

        if (SF_IsCOMStub(dwStubFlags))
        {
            if (marshalType == MarshalInfo::MARSHAL_TYPE_VALUECLASS ||
                marshalType == MarshalInfo::MARSHAL_TYPE_BLITTABLEVALUECLASS ||
                marshalType == MarshalInfo::MARSHAL_TYPE_GUID ||
                marshalType == MarshalInfo::MARSHAL_TYPE_DECIMAL)
            {
#ifndef _TARGET_X86_
                // We cannot optimize marshalType to MARSHAL_TYPE_GENERIC_* because the JIT works with exact types
                // and would refuse to compile the stub if it implicitly converted between scalars and value types (also see
                // code:MarshalInfo.MarhalInfo where we do the optimization on x86). We want to throw only if the structure
                // is too big to be returned in registers.
                if (marshalType != MarshalInfo::MARSHAL_TYPE_BLITTABLEVALUECLASS ||
                    IsUnmanagedValueTypeReturnedByRef(returnInfo.GetNativeArgSize()))
#endif // _TARGET_X86_
                {
                    if (!SF_IsHRESULTSwapping(dwStubFlags) && !SF_IsCOMLateBoundStub(dwStubFlags))
                    {
                        // Note that this limitation is very likely not needed anymore and could be lifted if we care.
                        COMPlusThrow(kMarshalDirectiveException, IDS_EE_COM_UNSUPPORTED_SIG);
                    }
                }

                pss->MarshalReturn(&returnInfo, argOffset);
            }
            else
            {
                // We don't support native methods that return VARIANTs directly.
                if (marshalType == MarshalInfo::MARSHAL_TYPE_OBJECT)
                {
                    if (!SF_IsHRESULTSwapping(dwStubFlags) && !SF_IsCOMLateBoundStub(dwStubFlags))
                    {
                        COMPlusThrow(kMarshalDirectiveException, IDS_EE_COM_UNSUPPORTED_SIG);
                    }
                }

                pss->MarshalReturn(&returnInfo, argOffset);
            }
        }
        else
#endif // FEATURE_COMINTEROP
        {
            if (marshalType > MarshalInfo::MARSHAL_TYPE_DOUBLE && IsUnsupportedValueTypeReturn(msig))
            {
                if (marshalType == MarshalInfo::MARSHAL_TYPE_BLITTABLEVALUECLASS
                        || marshalType == MarshalInfo::MARSHAL_TYPE_GUID
                        || marshalType == MarshalInfo::MARSHAL_TYPE_DECIMAL
#ifdef FEATURE_COMINTEROP                    
                        || marshalType == MarshalInfo::MARSHAL_TYPE_DATETIME
#endif // FEATURE_COMINTEROP
                   )
                {
                    if (SF_IsHRESULTSwapping(dwStubFlags))
                    {
                        // V1 restriction: we could implement this but it's late in the game to do so.
                        COMPlusThrow(kMarshalDirectiveException, IDS_EE_NDIRECT_UNSUPPORTED_SIG);
                    }
                }
                else if (marshalType == MarshalInfo::MARSHAL_TYPE_HANDLEREF)
                {
                    COMPlusThrow(kMarshalDirectiveException, IDS_EE_BADMARSHAL_HANDLEREFRESTRICTION);
                }
                else
                {
                    COMPlusThrow(kMarshalDirectiveException, IDS_EE_NDIRECT_UNSUPPORTED_SIG);
                }
            }

#ifdef FEATURE_COMINTEROP
            if (marshalType == MarshalInfo::MARSHAL_TYPE_OBJECT && !SF_IsHRESULTSwapping(dwStubFlags))
            {
                // No support for returning variants. This is a V1 restriction, due to the late date,
                // don't want to add the special-case code to support this in light of low demand.
                COMPlusThrow(kMarshalDirectiveException, IDS_EE_NOVARIANTRETURN);
            }
#endif // FEATURE_COMINTEROP

            pss->MarshalReturn(&returnInfo, argOffset);
        }
    }

    return marshalType;
}

static inline UINT GetStackOffsetFromStackSize(UINT stackSize, bool fThisCall)
{
    LIMITED_METHOD_CONTRACT;
#ifdef _TARGET_X86_
    if (fThisCall)
    {
        // -1 means that the argument is not on the stack
        return (stackSize >= sizeof(SLOT) ? (stackSize - sizeof(SLOT)) : (UINT)-1);
    }
#endif // _TARGET_X86_
    return stackSize;
}

#ifdef FEATURE_COMINTEROP

struct HiddenParameterInfo
{
    MarshalInfo *pManagedParam;     // Managed parameter which required the hidden parameter
    int          nativeIndex;       // 0 based index into the native method signature where the hidden parameter should be injected
};

// Get the indexes of any hidden length parameters to be marshaled for the method
//
// At return, each value in the ppParamIndexes array is a 0 based index into the native method signature where
// the length parameter for a hidden length array should be passed.  The MarshalInfo objects will also be
// updated such that they all have explicit marshaling information.
//
// The caller is responsible for freeing the memory pointed to by ppParamIndexes
void CheckForHiddenParameters(DWORD cParamMarshalInfo,
                              __in_ecount(cParamMarshalInfo) MarshalInfo *pParamMarshalInfo,
                              __out DWORD *pcHiddenNativeParameters,
                              __out HiddenParameterInfo **ppHiddenNativeParameters)
{
    CONTRACTL
    {
        STANDARD_VM_CHECK;
        PRECONDITION(CheckPointer(pParamMarshalInfo));
        PRECONDITION(CheckPointer(pcHiddenNativeParameters));
        PRECONDITION(CheckPointer(ppHiddenNativeParameters));
    }
    CONTRACTL_END;

    NewArrayHolder<HiddenParameterInfo> hiddenParamInfo(new HiddenParameterInfo[cParamMarshalInfo]);
    DWORD foundInfoCount = 0;

    for (DWORD iParam = 0; iParam < cParamMarshalInfo; ++iParam)
    {
        // Look for hidden length arrays, which all require additional parameters to be added
        if (pParamMarshalInfo[iParam].GetMarshalType() == MarshalInfo::MARSHAL_TYPE_HIDDENLENGTHARRAY)
        {
            DWORD currentNativeIndex = iParam + foundInfoCount;

            // The location of the length parameter is implicitly just before the array pointer.
            // We'll give it our current index, and bumping the found count will push us back a slot.

            // We should be safe to cast here - giant signatures will fail to marashal later with IDS_EE_SIGTOOCOMPLEX
            pParamMarshalInfo[iParam].SetHiddenLengthParamIndex(static_cast<UINT16>(currentNativeIndex));

            hiddenParamInfo[foundInfoCount].nativeIndex = pParamMarshalInfo[iParam].HiddenLengthParamIndex();
            hiddenParamInfo[foundInfoCount].pManagedParam = &(pParamMarshalInfo[iParam]);
            ++foundInfoCount;
        }
    }

    *pcHiddenNativeParameters = foundInfoCount;
    *ppHiddenNativeParameters = hiddenParamInfo.Extract();
}

bool IsHiddenParameter(int nativeArgIndex,
                       DWORD cHiddenParameters,
                       __in_ecount(cHiddenParameters) HiddenParameterInfo *pHiddenParameters,
                       __out HiddenParameterInfo **ppHiddenParameterInfo)
{
    CONTRACTL
    {
        STANDARD_VM_CHECK;
        PRECONDITION(cHiddenParameters == 0 || CheckPointer(pHiddenParameters));
        PRECONDITION(CheckPointer(ppHiddenParameterInfo));
    }
    CONTRACTL_END;

    *ppHiddenParameterInfo = NULL;

    for (DWORD i = 0; i < cHiddenParameters; ++i)
    {
        _ASSERTE(pHiddenParameters[i].nativeIndex != -1);
        if (pHiddenParameters[i].nativeIndex == nativeArgIndex)
        {
            *ppHiddenParameterInfo = &(pHiddenParameters[i]);
            return true;
        }
    }

    return false;
}

#endif // FEATURE_COMINTEROP

//---------------------------------------------------------
// Creates a new stub for a N/Direct call. Return refcount is 1.
// Note that this function may now throw if it fails to create
// a stub.
//---------------------------------------------------------
static void CreateNDirectStubWorker(StubState*         pss,
                                    StubSigDesc*       pSigDesc,
                                    CorNativeLinkType  nlType,
                                    CorNativeLinkFlags nlFlags,
                                    CorPinvokeMap      unmgdCallConv,
                                    DWORD              dwStubFlags,
                                    MethodDesc         *pMD,
                                    mdParamDef*        pParamTokenArray,
                                    int                iLCIDArg
                                    )
{
    CONTRACTL
    {
        STANDARD_VM_CHECK;

        PRECONDITION(CheckPointer(pss));
        PRECONDITION(CheckPointer(pSigDesc));
        PRECONDITION(CheckPointer(pMD, NULL_OK));
        PRECONDITION(!pMD || pMD->IsILStub() || (0 != pMD->GetMethodTable()->IsDelegate()) == SF_IsDelegateStub(dwStubFlags));
    }
    CONTRACTL_END;

    SF_ConsistencyCheck(dwStubFlags);

#ifdef _DEBUG
    if (g_pConfig->ShouldBreakOnInteropStubSetup(pSigDesc->m_pDebugName))
        CONSISTENCY_CHECK_MSGF(false, ("BreakOnInteropStubSetup: '%s' ", pSigDesc->m_pDebugName));
#endif // _DEBUG

    Stub* pstub = NULL;

    if (SF_IsCOMStub(dwStubFlags))
    {
        _ASSERTE(0 == nlType);
        _ASSERTE(0 == nlFlags);
        _ASSERTE(0 == unmgdCallConv);
    }
    else
    {
        _ASSERTE(nlType == nltAnsi || nlType == nltUnicode);
    }
    Module *pModule = pSigDesc->m_pModule;

    //
    // Set up signature walking objects.
    //

    MetaSig msig(pSigDesc->m_sig, 
                 pModule, 
                 &pSigDesc->m_typeContext);

    if (SF_IsVarArgStub(dwStubFlags))
        msig.SetTreatAsVarArg();

    bool fThisCall = (unmgdCallConv == pmCallConvThiscall);

    pss->SetLastError(nlFlags & nlfLastError);

    // This has been in the product since forward P/Invoke via delegates was
    // introduced. It's wrong, but please keep it for backward compatibility.
    if (SF_IsDelegateStub(dwStubFlags))
        pss->SetLastError(TRUE);

    pss->BeginEmit(dwStubFlags);

    if (-1 != iLCIDArg)
    {
        // LCID is not supported on WinRT
        _ASSERTE(!SF_IsWinRTStub(dwStubFlags));

        // The code to handle the LCID  will call MarshalLCID before calling MarshalArgument
        // on the argument the LCID should go after. So we just bump up the index here.
        iLCIDArg++;
    }

    int numArgs = msig.NumFixedArgs();

    // thiscall must have at least one parameter (the "this")
    if (fThisCall && numArgs == 0)
        COMPlusThrow(kMarshalDirectiveException, IDS_EE_NDIRECT_BADNATL_THISCALL);

    //
    // Now, emit the IL.
    //

    int argOffset = 0;

    MarshalInfo::MarshalType marshalType = (MarshalInfo::MarshalType) 0xcccccccc;

    //
    // Marshal the return value.
    //

    UINT nativeStackSize = (SF_IsCOMStub(dwStubFlags) ? sizeof(SLOT) : 0);
    bool fHasCopyCtorArgs = false;
    bool fStubNeedsCOM = SF_IsCOMStub(dwStubFlags);
    
    // Normally we would like this to be false so that we use the correct signature 
    // in the IL_STUB, (i.e if it returns a value class then the signature will use that)
    // When this bool is true we change the return type to void and explicitly add a
    // return buffer argument as the first argument.
    BOOL fMarshalReturnValueFirst = false;
    
    // We can only change fMarshalReturnValueFirst to true when we are NOT doing HRESULT-swapping!
    //
    if (!SF_IsHRESULTSwapping(dwStubFlags))
    {

#if defined(_TARGET_X86_) || defined(_TARGET_ARM_)
        // JIT32 has problems in generating code for pinvoke ILStubs which do a return in return buffer.
        // Therefore instead we change the signature of calli to return void and make the return buffer as first
        // argument. This matches the ABI i.e. return buffer is passed as first arg. So native target will get the
        // return buffer in correct register.
        // The return structure secret arg comes first, however byvalue return is processed at
        // the end because it could be the HRESULT-swapped argument which always comes last.

#ifdef UNIX_X86_ABI
        // For functions with value type class, managed and unmanaged calling convention differ
        fMarshalReturnValueFirst = HasRetBuffArgUnmanagedFixup(&msig);
#else // UNIX_X86_ABI
        fMarshalReturnValueFirst = HasRetBuffArg(&msig);
#endif // UNIX_X86_ABI

#endif // defined(_TARGET_X86_) || defined(_TARGET_ARM_)

    }
    
    if (fMarshalReturnValueFirst)
    {
        marshalType = DoMarshalReturnValue(msig,
                                           pParamTokenArray,
                                           nlType,
                                           nlFlags,
                                           0,
                                           pss,
                                           fThisCall,
                                           argOffset,
                                           dwStubFlags,
                                           pMD,
                                           nativeStackSize,
                                           fStubNeedsCOM,
                                           0
                                           DEBUG_ARG(pSigDesc->m_pDebugName)
                                           DEBUG_ARG(pSigDesc->m_pDebugClassName)
                                           );

        if (marshalType == MarshalInfo::MARSHAL_TYPE_DATE ||
            marshalType == MarshalInfo::MARSHAL_TYPE_CURRENCY ||
            marshalType == MarshalInfo::MARSHAL_TYPE_ARRAYWITHOFFSET ||
            marshalType == MarshalInfo::MARSHAL_TYPE_HANDLEREF ||
            marshalType == MarshalInfo::MARSHAL_TYPE_ARGITERATOR
#ifdef FEATURE_COMINTEROP
         || marshalType == MarshalInfo::MARSHAL_TYPE_OLECOLOR
#endif // FEATURE_COMINTEROP
            )
        {
            // These are special non-blittable types returned by-ref in managed,
            // but marshaled as primitive values returned by-value in unmanaged.
        }
        else
        {
            // This is an ordinary value type - see if it is returned by-ref.
            MethodTable *pRetMT = msig.GetRetTypeHandleThrowing().AsMethodTable();
            if (IsUnmanagedValueTypeReturnedByRef(pRetMT->GetNativeSize()))
            {
                nativeStackSize += sizeof(LPVOID);
            }
        }
    }

    //
    // Marshal the arguments
    //
    MarshalInfo::MarshalScenario ms;
#ifdef FEATURE_COMINTEROP
    if (SF_IsCOMStub(dwStubFlags))
    {
        if (SF_IsWinRTStub(dwStubFlags))
            ms = MarshalInfo::MARSHAL_SCENARIO_WINRT;
        else
            ms = MarshalInfo::MARSHAL_SCENARIO_COMINTEROP;
    }
    else
#endif // FEATURE_COMINTEROP
    {
        ms = MarshalInfo::MARSHAL_SCENARIO_NDIRECT;
    }

    // Build up marshaling information for each of the method's parameters
    SIZE_T cbParamMarshalInfo;
    if (!ClrSafeInt<SIZE_T>::multiply(sizeof(MarshalInfo), numArgs, cbParamMarshalInfo))
    {
        COMPlusThrowHR(COR_E_OVERFLOW);
    }

    NewArrayHolder<BYTE> pbParamMarshalInfo(new BYTE[cbParamMarshalInfo]);
    MarshalInfo *pParamMarshalInfo = reinterpret_cast<MarshalInfo *>(pbParamMarshalInfo.GetValue());

    MetaSig paramInfoMSig(msig);
    for (int i = 0; i < numArgs; ++i)
    {
        paramInfoMSig.NextArg();
        new(&(pParamMarshalInfo[i])) MarshalInfo(paramInfoMSig.GetModule(),
                                                 paramInfoMSig.GetArgProps(),
                                                 paramInfoMSig.GetSigTypeContext(),
                                                 pParamTokenArray[i + 1],
                                                 ms,
                                                 nlType,
                                                 nlFlags,
                                                 TRUE,
                                                 i + 1,
                                                 numArgs,
                                                 SF_IsBestFit(dwStubFlags),
                                                 SF_IsThrowOnUnmappableChar(dwStubFlags),
                                                 TRUE,
                                                 pMD,
                                                 TRUE
                                                 DEBUG_ARG(pSigDesc->m_pDebugName)
                                                 DEBUG_ARG(pSigDesc->m_pDebugClassName)
                                                 DEBUG_ARG(i + 1));
    }

#ifdef FEATURE_COMINTEROP
    // Check to see if we need to inject any additional hidden parameters
    DWORD cHiddenNativeParameters;
    NewArrayHolder<HiddenParameterInfo> pHiddenNativeParameters;
    CheckForHiddenParameters(numArgs, pParamMarshalInfo, &cHiddenNativeParameters, &pHiddenNativeParameters);

    // Hidden parameters and LCID do not mix
    _ASSERTE(!(cHiddenNativeParameters > 0 && iLCIDArg != -1));
#endif // FEATURE_COMINTEROP

    // Marshal the parameters
    int argidx = 1;
    int nativeArgIndex = 0;
    while (argidx <= numArgs)
    {
#ifdef FEATURE_COMINTEROP
        HiddenParameterInfo *pHiddenParameter;
        // Check to see if we need to inject a hidden parameter
        if (IsHiddenParameter(nativeArgIndex, cHiddenNativeParameters, pHiddenNativeParameters, &pHiddenParameter))
        {
            pss->MarshalHiddenLengthArgument(pHiddenParameter->pManagedParam, FALSE);
            nativeStackSize += pHiddenParameter->pManagedParam->GetHiddenLengthParamStackSize();

            if (SF_IsReverseStub(dwStubFlags))
            {
                ++argOffset;
            }
        }
        else
#endif // FEATURE_COMINTEROP
        {
            //
            // Check to see if this is the parameter after which we need to insert the LCID.
            //
            if (argidx == iLCIDArg)
            {
                pss->MarshalLCID(argidx);
                nativeStackSize += sizeof(LPVOID);

                if (SF_IsReverseStub(dwStubFlags))
                    argOffset++;
            }

            msig.NextArg();

            MarshalInfo &info = pParamMarshalInfo[argidx - 1];

#ifdef FEATURE_COMINTEROP
            // For the hidden-length array, length parameters must occur before the parameter containing the array pointer
            _ASSERTE(info.GetMarshalType() != MarshalInfo::MARSHAL_TYPE_HIDDENLENGTHARRAY || nativeArgIndex > info.HiddenLengthParamIndex());
#endif // FEATURE_COMINTEROP

            pss->MarshalArgument(&info, argOffset, GetStackOffsetFromStackSize(nativeStackSize, fThisCall));
            nativeStackSize += info.GetNativeArgSize();

            fStubNeedsCOM |= info.MarshalerRequiresCOM();

            if (fThisCall && argidx == 1)
            {
                // make sure that the first parameter is enregisterable
                if (info.GetNativeArgSize() > sizeof(SLOT))
                    COMPlusThrow(kMarshalDirectiveException, IDS_EE_NDIRECT_BADNATL_THISCALL);
            }


            argidx++;
        }
        
        ++nativeArgIndex;
    }

    // Check to see if this is the parameter after which we need to insert the LCID.
    if (argidx == iLCIDArg)
    {
        pss->MarshalLCID(argidx);
        nativeStackSize += sizeof(LPVOID);

        if (SF_IsReverseStub(dwStubFlags))
            argOffset++;
    }

    if (!fMarshalReturnValueFirst)
    {
        // This could be a HRESULT-swapped argument so it must come last.
        marshalType = DoMarshalReturnValue(msig,
                             pParamTokenArray,
                             nlType,
                             nlFlags,
                             argidx,
                             pss,
                             fThisCall,
                             argOffset,
                             dwStubFlags,
                             pMD,
                             nativeStackSize,
                             fStubNeedsCOM,
                             nativeArgIndex
                             DEBUG_ARG(pSigDesc->m_pDebugName)
                             DEBUG_ARG(pSigDesc->m_pDebugClassName)
                             );

        // If the return value is a SafeHandle or CriticalHandle, mark the stub method.
        // Interop methods that use this stub will have an implicit reliability contract
        // (see code:TAStackCrawlCallBack).
        if (!SF_IsHRESULTSwapping(dwStubFlags))
        {
            if (marshalType == MarshalInfo::MARSHAL_TYPE_SAFEHANDLE ||
                marshalType == MarshalInfo::MARSHAL_TYPE_CRITICALHANDLE)
            {
                if (pMD->IsDynamicMethod())
                    pMD->AsDynamicMethodDesc()->SetUnbreakable(true);
            }
        }
    }

    if (SF_IsHRESULTSwapping(dwStubFlags))
    {
        if (msig.GetReturnType() != ELEMENT_TYPE_VOID)
            nativeStackSize += sizeof(LPVOID);
    }

    if (pMD->IsDynamicMethod())
    {
        // Set the native stack size to the IL stub MD. It is needed for alignment
        // thunk generation on the Mac and stdcall name decoration on Windows.
        // We do not store it directly in the interop MethodDesc here because due 
        // to sharing we come here only for the first call with given signature and 
        // the target MD may even be NULL.

#ifdef _TARGET_X86_
        if (fThisCall)
        {
            _ASSERTE(nativeStackSize >= sizeof(SLOT));
            nativeStackSize -= sizeof(SLOT);
        }
#else // _TARGET_X86_
        //
        // The algorithm to compute nativeStackSize on the fly is x86-specific.
        // Recompute the correct size for other platforms from the stub signature.
        //
        if (SF_IsForwardStub(dwStubFlags))
        {
            // It would be nice to compute the correct value for forward stubs too.
            // The value is only used in MarshalNative::NumParamBytes right now,
            // and changing what MarshalNative::NumParamBytes returns is 
            // a potential breaking change.
        }
        else
        {
            // native stack size is updated in code:ILStubState.SwapStubSignatures
        }
#endif // _TARGET_X86_

        if (!FitsInU2(nativeStackSize))
            COMPlusThrow(kMarshalDirectiveException, IDS_EE_SIGTOOCOMPLEX);

        DynamicMethodDesc *pDMD = pMD->AsDynamicMethodDesc();

        pDMD->SetNativeStackArgSize(static_cast<WORD>(nativeStackSize));
        pDMD->SetHasCopyCtorArgs(fHasCopyCtorArgs);
        pDMD->SetStubNeedsCOMStarted(fStubNeedsCOM);
    }

    // FinishEmit needs to know the native stack arg size so we call it after the number
    // has been set in the stub MD (code:DynamicMethodDesc.SetNativeStackArgSize)
    pss->FinishEmit(pMD);
}

class NDirectStubHashBlob : public ILStubHashBlobBase
{
public:
    Module*     m_pModule;

    WORD        m_unmgdCallConv;
    BYTE        m_nlType;                   // C_ASSERTS are in NDirect::CreateHashBlob
    BYTE        m_nlFlags;

    DWORD       m_StubFlags;

    INT32       m_iLCIDArg;
    INT32       m_nParams;
    BYTE        m_rgbSigAndParamData[1];
    // (dwParamAttr, cbNativeType)          // length: number of parameters
    // NativeTypeBlob                       // length: number of parameters
    // BYTE     m_rgbSigData[];             // length: determined by sig walk
};

// For better performance and less memory fragmentation,
// I'm using structure here to avoid allocating 3 different arrays.
struct ParamInfo
{
    DWORD dwParamAttr;
    ULONG cbNativeType;
    PCCOR_SIGNATURE pvNativeType;
};  

ILStubHashBlob* NDirect::CreateHashBlob(NDirectStubParameters* pParams)
{
    STANDARD_VM_CONTRACT;

    NDirectStubHashBlob*    pBlob;

    IMDInternalImport* pInternalImport = pParams->m_pModule->GetMDImport();

    CQuickBytes paramInfoBytes;
    paramInfoBytes.AllocThrows(sizeof(ParamInfo)*pParams->m_nParamTokens);
    ParamInfo *paramInfos = (ParamInfo *)paramInfoBytes.Ptr();
    ::ZeroMemory(paramInfos, sizeof(ParamInfo) * pParams->m_nParamTokens);

    size_t cbNativeTypeTotal = 0;
    
    //
    // Collect information for function parameters
    //     
    for (int idx = 0; idx < pParams->m_nParamTokens; idx++)
    {
        mdParamDef token = pParams->m_pParamTokenArray[idx];
        if (TypeFromToken(token) == mdtParamDef && mdParamDefNil != token)
        {
            USHORT usSequence_Ignore;       // We don't need usSequence in the hash as the param array is already sorted
            LPCSTR szParamName_Ignore;
            IfFailThrow(pInternalImport->GetParamDefProps(token, &usSequence_Ignore, &paramInfos[idx].dwParamAttr, &szParamName_Ignore));

            if (paramInfos[idx].dwParamAttr & pdHasFieldMarshal)
            {
                IfFailThrow(pInternalImport->GetFieldMarshal(token, &paramInfos[idx].pvNativeType, &paramInfos[idx].cbNativeType));
                cbNativeTypeTotal += paramInfos[idx].cbNativeType;
            }
        }
    }

    SigPointer sigPtr = pParams->m_sig.CreateSigPointer();
    
    // note that ConvertToInternalSignature also resolves generics so different instantiations will get different
    // hash blobs for methods that have generic parameters in their signature
    SigBuilder sigBuilder;
    sigPtr.ConvertToInternalSignature(pParams->m_pModule, pParams->m_pTypeContext, &sigBuilder, /* bSkipCustomModifier = */ FALSE);

    DWORD cbSig;
    PVOID pSig = sigBuilder.GetSignature(&cbSig);

    //
    // Build hash blob for IL stub sharing
    //
    S_SIZE_T cbSizeOfBlob = S_SIZE_T(offsetof(NDirectStubHashBlob, m_rgbSigAndParamData)) +
                            S_SIZE_T(sizeof(ULONG)) * S_SIZE_T(pParams->m_nParamTokens) +   // Parameter attributes
                            S_SIZE_T(sizeof(DWORD)) * S_SIZE_T(pParams->m_nParamTokens) +   // Native type blob size
                            S_SIZE_T(cbNativeTypeTotal) +                                   // Native type blob data
                            S_SIZE_T(cbSig);                                                // Signature
                            
    if (cbSizeOfBlob.IsOverflow())
        COMPlusThrowHR(COR_E_OVERFLOW);

    static_assert_no_msg(nltMaxValue   <= 0xFF);
    static_assert_no_msg(nlfMaxValue   <= 0xFF);
    static_assert_no_msg(pmMaxValue    <= 0xFFFF);

    NewArrayHolder<BYTE> pBytes = new BYTE[cbSizeOfBlob.Value()];
    // zero out the hash bytes to ensure all bit fields are deterministically set
    ZeroMemory(pBytes, cbSizeOfBlob.Value());
    pBlob = (NDirectStubHashBlob*)(BYTE*)pBytes;

    pBlob->m_pModule                = NULL;

    if (SF_IsNGENedStub(pParams->m_dwStubFlags))
    {
        // don't share across modules if we are ngening the stub
        pBlob->m_pModule = pParams->m_pModule;
    }

    pBlob->m_cbSizeOfBlob           = cbSizeOfBlob.Value();
    pBlob->m_unmgdCallConv          = static_cast<WORD>(pParams->m_unmgdCallConv);
    pBlob->m_nlType                 = static_cast<BYTE>(pParams->m_nlType);
    pBlob->m_nlFlags                = static_cast<BYTE>(pParams->m_nlFlags & ~nlfNoMangle); // this flag does not affect the stub
    pBlob->m_iLCIDArg               = pParams->m_iLCIDArg;

    pBlob->m_StubFlags              = pParams->m_dwStubFlags;
    pBlob->m_nParams                = pParams->m_nParamTokens;

    BYTE* pBlobParams               = &pBlob->m_rgbSigAndParamData[0];

    //
    // Write (dwParamAttr, cbNativeType) for parameters
    //
    // Note that these need to be aligned and it is why they are written before the byte blobs
    // I'm putting asserts here so that it will assert even in non-IA64 platforms to catch bugs
    //
    _ASSERTE((DWORD_PTR)pBlobParams % sizeof(DWORD) == 0);
    _ASSERTE(sizeof(DWORD) == sizeof(ULONG));
    
    for (int i = 0; i < pParams->m_nParamTokens; ++i)
    {
        // We only care about In/Out/HasFieldMarshal
        // Other attr are about optional/default values which are not used in marshalling,
        // but only used in compilers        
        *((DWORD *)pBlobParams) = paramInfos[i].dwParamAttr & (pdIn | pdOut | pdHasFieldMarshal);
        pBlobParams += sizeof(DWORD);

        *((ULONG *)pBlobParams) = paramInfos[i].cbNativeType;
        pBlobParams += sizeof(ULONG);
    }

    //
    // Write native type blob for parameters
    //
    for (int i = 0; i < pParams->m_nParamTokens; ++i)
    {
        memcpy(pBlobParams, paramInfos[i].pvNativeType, paramInfos[i].cbNativeType);
        pBlobParams += paramInfos[i].cbNativeType;
    }
    
    //
    // Copy signature
    // 
    memcpy(pBlobParams, pSig, cbSig);

    // Verify that we indeed have reached the end
    _ASSERTE(pBlobParams + cbSig == (BYTE *)pBlob + cbSizeOfBlob.Value());

    pBytes.SuppressRelease();
    return (ILStubHashBlob*)pBlob;
}

// static inline
ILStubCache* NDirect::GetILStubCache(NDirectStubParameters* pParams)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    // Use the m_pLoaderModule instead of m_pModule
    // They could be different for methods on generic types.
    return pParams->m_pLoaderModule->GetILStubCache();
}

// static
MethodDesc* NDirect::GetStubMethodDesc(
    MethodDesc *pTargetMD,
    NDirectStubParameters* pParams,
    ILStubHashBlob* pHashParams,
    AllocMemTracker* pamTracker,
    bool& bILStubCreator,
    MethodDesc* pLastMD)
{
    CONTRACT(MethodDesc*)
    {
        STANDARD_VM_CHECK;

        PRECONDITION(CheckPointer(pParams));
        PRECONDITION(!pParams->m_sig.IsEmpty());
        PRECONDITION(CheckPointer(pParams->m_pModule));
        PRECONDITION(CheckPointer(pTargetMD, NULL_OK));
        POSTCONDITION(CheckPointer(RETVAL));
    }
    CONTRACT_END;

    MethodDesc*     pMD;

    ILStubCache* pCache = NDirect::GetILStubCache(pParams);

    pMD = pCache->GetStubMethodDesc(pTargetMD,
                                    pHashParams, 
                                    pParams->m_dwStubFlags, 
                                    pParams->m_pModule, 
                                    pParams->m_sig.GetRawSig(),
                                    pParams->m_sig.GetRawSigLen(),
                                    pamTracker,
                                    bILStubCreator,
                                    pLastMD);

    RETURN pMD;
}


// static
void NDirect::RemoveILStubCacheEntry(NDirectStubParameters* pParams, ILStubHashBlob* pHashParams)
{
    CONTRACTL
    {
        STANDARD_VM_CHECK;

        PRECONDITION(CheckPointer(pParams));
        PRECONDITION(CheckPointer(pHashParams));
        PRECONDITION(!pParams->m_sig.IsEmpty());
        PRECONDITION(CheckPointer(pParams->m_pModule));
    }
    CONTRACTL_END;

    LOG((LF_STUBS, LL_INFO1000, "Exception happened when generating IL of stub clr!CreateInteropILStub StubMD: %p, HashBlob: %p \n", pParams, pHashParams));

    ILStubCache* pCache = NDirect::GetILStubCache(pParams);

    pCache->DeleteEntry(pHashParams);
}

// static
void NDirect::AddMethodDescChunkWithLockTaken(NDirectStubParameters* pParams, MethodDesc *pMD)
{
    CONTRACTL
    {
        STANDARD_VM_CHECK;

        PRECONDITION(CheckPointer(pParams));
        PRECONDITION(!pParams->m_sig.IsEmpty());
        PRECONDITION(CheckPointer(pParams->m_pModule));
    }
    CONTRACTL_END;

    ILStubCache* pCache = NDirect::GetILStubCache(pParams);

    pCache->AddMethodDescChunkWithLockTaken(pMD);
}

//
// Additional factorization of CreateNDirectStub.  This hoists all the metadata accesses
// into one location so that we can leave CreateNDirectStubWorker to just generate the 
// IL.  This allows us to cache a stub based on the inputs to CreateNDirectStubWorker
// instead of having to generate the IL first before doing the caching.
//
void CreateNDirectStubAccessMetadata(StubSigDesc*       pSigDesc,       // IN
                                     CorPinvokeMap      unmgdCallConv,  // IN
                                     DWORD*             pdwStubFlags,   // IN/OUT
                                     int*               piLCIDArg,      // OUT
                                     int*               pNumArgs        // OUT
                                     )
{
    STANDARD_VM_CONTRACT;

    if (SF_IsCOMStub(*pdwStubFlags))
    {
        _ASSERTE(0 == unmgdCallConv);
    }
    else
    {
        if (unmgdCallConv != pmCallConvStdcall &&
            unmgdCallConv != pmCallConvCdecl &&
            unmgdCallConv != pmCallConvThiscall)
        {
            COMPlusThrow(kTypeLoadException, IDS_INVALID_PINVOKE_CALLCONV);
        }
    }
    
#ifdef FEATURE_COMINTEROP
    if (SF_IsDelegateStub(*pdwStubFlags))
    {
        _ASSERTE(!SF_IsWinRTStub(*pdwStubFlags));
        if (pSigDesc->m_pMD->GetMethodTable()->IsProjectedFromWinRT())
        {
            // We do not allow P/Invoking via WinRT delegates to better segregate WinRT
            // from classic interop scenarios.
            COMPlusThrow(kMarshalDirectiveException, IDS_EE_DELEGATEPINVOKE_WINRT);
        }
    }
#endif // FEATURE_COMINTEROP

    MetaSig msig(pSigDesc->m_sig, 
                 pSigDesc->m_pModule, 
                 &pSigDesc->m_typeContext);

    if (SF_IsVarArgStub(*pdwStubFlags))
        msig.SetTreatAsVarArg();

    (*pNumArgs) = msig.NumFixedArgs();
    
    IMDInternalImport* pInternalImport = pSigDesc->m_pModule->GetMDImport();

    _ASSERTE(!SF_IsHRESULTSwapping(*pdwStubFlags));

    mdMethodDef md = pSigDesc->m_tkMethodDef;
    if (md != mdMethodDefNil)
    {
        DWORD           dwDescrOffset;
        DWORD           dwImplFlags;
        IfFailThrow(pInternalImport->GetMethodImplProps(
            md, 
            &dwDescrOffset, 
            &dwImplFlags));
        
#ifdef FEATURE_COMINTEROP
        if (SF_IsWinRTStub(*pdwStubFlags))
        {
            // All WinRT methods do HRESULT swapping
            if (IsMiPreserveSig(dwImplFlags))
            {
                COMPlusThrow(kMarshalDirectiveException, IDS_EE_PRESERVESIG_WINRT);
            }

            (*pdwStubFlags) |= NDIRECTSTUB_FL_DOHRESULTSWAPPING;
        }
        else
#endif // FEATURE_COMINTEROP
        if (SF_IsReverseStub(*pdwStubFlags))
        {
            // only COM-to-CLR call supports hresult swapping in the reverse direction
            if (SF_IsCOMStub(*pdwStubFlags) && !IsMiPreserveSig(dwImplFlags))
            {
                (*pdwStubFlags) |= NDIRECTSTUB_FL_DOHRESULTSWAPPING;
            }
        }
        else
        {
            // fwd pinvoke, fwd com interop support hresult swapping.
            // delegate to an unmanaged method does not.
            if (!IsMiPreserveSig(dwImplFlags) && !SF_IsDelegateStub(*pdwStubFlags))
            {
                (*pdwStubFlags) |= NDIRECTSTUB_FL_DOHRESULTSWAPPING;
            }
        }
    }

    if (pSigDesc->m_pMD != NULL)
    {
        (*piLCIDArg) = GetLCIDParameterIndex(pSigDesc->m_pMD);
    }
    else
    {
        (*piLCIDArg) = -1;
    }

    // Check to see if we need to do LCID conversion.
    if ((*piLCIDArg) != -1 && (*piLCIDArg) > (*pNumArgs))
    {
        COMPlusThrow(kIndexOutOfRangeException, IDS_EE_INVALIDLCIDPARAM);
    }

    if (SF_IsCOMStub(*pdwStubFlags) && !SF_IsWinRTStaticStub(*pdwStubFlags))
    {
        CONSISTENCY_CHECK(msig.HasThis());
    }
    else
    {
        if (msig.HasThis() && !SF_IsDelegateStub(*pdwStubFlags))
        {
            COMPlusThrow(kInvalidProgramException, VLDTR_E_FMD_PINVOKENOTSTATIC);
        }
    }
}

void NDirect::PopulateNDirectMethodDesc(NDirectMethodDesc* pNMD, PInvokeStaticSigInfo* pSigInfo, BOOL throwOnError /*= TRUE*/)
{
    if (pNMD->IsSynchronized() && throwOnError)
        COMPlusThrow(kTypeLoadException, IDS_EE_NOSYNCHRONIZED);

    WORD ndirectflags = 0;
    if (pNMD->MethodDesc::IsVarArg())
        ndirectflags |= NDirectMethodDesc::kVarArgs;

    LPCUTF8 szLibName = NULL, szEntryPointName = NULL;
    new (pSigInfo) PInvokeStaticSigInfo(pNMD, &szLibName, &szEntryPointName,
        (throwOnError ? PInvokeStaticSigInfo::THROW_ON_ERROR : PInvokeStaticSigInfo::NO_THROW_ON_ERROR));

    if (pSigInfo->GetCharSet() == nltAnsi)
        ndirectflags |= NDirectMethodDesc::kNativeAnsi;

    CorNativeLinkFlags linkflags = pSigInfo->GetLinkFlags();    
    if (linkflags & nlfLastError)
        ndirectflags |= NDirectMethodDesc::kLastError;
    if (linkflags & nlfNoMangle)
        ndirectflags |= NDirectMethodDesc::kNativeNoMangle;
    
    CorPinvokeMap callConv = pSigInfo->GetCallConv();
    if (callConv == pmCallConvStdcall)
        ndirectflags |= NDirectMethodDesc::kStdCall;
    if (callConv == pmCallConvThiscall)
        ndirectflags |= NDirectMethodDesc::kThisCall;

    if (pNMD->GetLoaderModule()->IsSystem() && strcmp(szLibName, "QCall") == 0)
    {
        ndirectflags |= NDirectMethodDesc::kIsQCall;
    }
    else
    {
        EnsureWritablePages(&pNMD->ndirect);
        pNMD->ndirect.m_pszLibName.SetValueMaybeNull(szLibName);
        pNMD->ndirect.m_pszEntrypointName.SetValueMaybeNull(szEntryPointName);
    }

#ifdef _TARGET_X86_
    if (ndirectflags & NDirectMethodDesc::kStdCall)
    {
        // Compute the kStdCallWithRetBuf flag which is needed at link time for entry point mangling.
        MetaSig msig(pNMD);
        ArgIterator argit(&msig);
        if (argit.HasRetBuffArg())
        {
            MethodTable *pRetMT = msig.GetRetTypeHandleThrowing().AsMethodTable();
            if (IsUnmanagedValueTypeReturnedByRef(pRetMT->GetNativeSize()))
            {
                ndirectflags |= NDirectMethodDesc::kStdCallWithRetBuf;
            }
        }
    }
#endif // _TARGET_X86_

    // Call this exactly ONCE per thread. Do not publish incomplete prestub flags
    // or you will introduce a race condition.
    pNMD->InterlockedSetNDirectFlags(ndirectflags);
}

#ifdef FEATURE_COMINTEROP
// Find the MethodDesc of the predefined IL stub method by either
// 1) looking at redirected adapter interfaces, OR
// 2) looking at special attributes for the specific interop scenario (specified by dwStubFlags).
// Currently only ManagedToNativeComInteropStubAttribute is supported.
// It returns NULL if no such attribute(s) can be found.  
// But if the attribute is found and is invalid, or something went wrong in the looking up
// process, an exception will be thrown. If everything goes well, you'll get the MethodDesc
// of the stub method
HRESULT FindPredefinedILStubMethod(MethodDesc *pTargetMD, DWORD dwStubFlags, MethodDesc **ppRetStubMD)
{
    CONTRACT(HRESULT)
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(CheckPointer(pTargetMD));
        PRECONDITION(CheckPointer(ppRetStubMD));
        PRECONDITION(*ppRetStubMD == NULL);
    }
    CONTRACT_END;

    HRESULT hr;

    MethodTable *pTargetMT = pTargetMD->GetMethodTable();

    // Check if this is a redirected interface - we have static stubs in mscorlib for those.
    if (SF_IsForwardCOMStub(dwStubFlags) && pTargetMT->IsInterface())
    {

        // Redirect generic redirected interfaces to the corresponding adapter methods in mscorlib
        if (pTargetMT->HasInstantiation())
        {
            MethodDesc *pAdapterMD = WinRTInterfaceRedirector::GetStubMethodForRedirectedInterfaceMethod(pTargetMD, TypeHandle::Interop_ManagedToNative);
            if (pAdapterMD != NULL)
            {
                *ppRetStubMD = pAdapterMD;
                return S_OK;
            }
        }
    }

    //
    // Find out if we have the attribute
    //    
    const void *pBytes;
    ULONG cbBytes;

    // Support v-table forward classic COM interop calls only
    if (SF_IsCOMStub(dwStubFlags) && SF_IsForwardStub(dwStubFlags) && !SF_IsWinRTStub(dwStubFlags))
    {
        if (pTargetMT->HasInstantiation())
        {
            // ManagedToNativeComInteropStubAttribute is not supported with generics
            return E_FAIL;
        }

        if (pTargetMD->IsFCall())
        {
            // ManagedToNativeComInteropStubAttribute is not supported on FCalls (i.e. methods on legacy
            // interfaces forwarded to CustomMarshalers.dll such as IEnumerable::GetEnumerator)
            return E_FAIL;
        }
        _ASSERTE(pTargetMD->IsComPlusCall());
        
        if (pTargetMD->IsInterface())
        {
            _ASSERTE(!pTargetMD->GetAssembly()->IsWinMD());
            hr = pTargetMD->GetMDImport()->GetCustomAttributeByName(
                pTargetMD->GetMemberDef(),
                FORWARD_INTEROP_STUB_METHOD_TYPE,
                &pBytes,
                &cbBytes);
                
            if (FAILED(hr)) 
                RETURN hr;
            // GetCustomAttributeByName returns S_FALSE when it cannot find the attribute but nothing fails...
            // Translate that to E_FAIL
            else if (hr == S_FALSE)
                RETURN E_FAIL;               
        }
        else
        {
            // We are dealing with the class, use the interface MD instead
            // After second thought I believe we don't need to check the class MD.
            // We can think stubs as part of public interface, and if the interface is public,
            // the stubs should also be accessible
            MethodDesc *pInterfaceMD = pTargetMD->GetInterfaceMD();
            if (pInterfaceMD)
            {
                hr = FindPredefinedILStubMethod(pInterfaceMD, dwStubFlags, ppRetStubMD);
                RETURN hr;
            }
            else
                RETURN E_FAIL;
        }
    }
    else
        RETURN E_FAIL;
        
    //
    // Parse the attribute
    //
    CustomAttributeParser parser(pBytes, cbBytes);
    IfFailRet(parser.SkipProlog());

    LPCUTF8 pTypeName;
    ULONG cbTypeName;
    IfFailRet(parser.GetNonEmptyString(&pTypeName, &cbTypeName));

    LPCUTF8 pMethodName;
    ULONG cbMethodName;
    IfFailRet(parser.GetNonEmptyString(&pMethodName, &cbMethodName));

    StackSString typeName(SString::Utf8, pTypeName, cbTypeName);
    StackSString methodName(SString::Utf8, pMethodName, cbMethodName);

    //
    // Retrieve the type
    //
    TypeHandle stubClassType;
    stubClassType = TypeName::GetTypeUsingCASearchRules(typeName.GetUnicode(), pTargetMT->GetAssembly());

    MethodTable *pStubClassMT = stubClassType.AsMethodTable();

    StackSString stubClassName;
    pStubClassMT->_GetFullyQualifiedNameForClassNestedAware(stubClassName);
    
    StackSString targetInterfaceName;
    pTargetMT->_GetFullyQualifiedNameForClassNestedAware(targetInterfaceName);
    
    // Restrict to same assembly only to reduce test cost
    if (stubClassType.GetAssembly() != pTargetMT->GetAssembly())
    {
        COMPlusThrow(
            kArgumentException, 
            IDS_EE_INTEROP_STUB_CA_MUST_BE_WITHIN_SAME_ASSEMBLY,
            stubClassName.GetUnicode(),
            targetInterfaceName.GetUnicode()
            );
    }

    if (stubClassType.HasInstantiation())
    {
        COMPlusThrow(
            kArgumentException, 
            IDS_EE_INTEROP_STUB_CA_STUB_CLASS_MUST_NOT_BE_GENERIC,
            stubClassName.GetUnicode()
            );
    }
    
    if (stubClassType.IsInterface())
    {
        COMPlusThrow(
            kArgumentException, 
            IDS_EE_INTEROP_STUB_CA_STUB_CLASS_MUST_NOT_BE_INTERFACE,
            stubClassName.GetUnicode()
            );
    }
    
    //
    // Locate the MethodDesc for the stub method
    //
    MethodDesc *pStubMD = NULL;

    {
        PCCOR_SIGNATURE pTargetSig = NULL;
        DWORD pcTargetSig = 0;
        
        SigTypeContext typeContext; // NO generics supported

        pTargetMD->GetSig(&pTargetSig, &pcTargetSig);
        
        MetaSig msig(pTargetSig, 
                     pcTargetSig,
                     pTargetMD->GetModule(), 
                     &typeContext);
        _ASSERTE(msig.HasThis());
        
        SigBuilder stubSigBuilder;

        //
        // Append calling Convention, NumOfArgs + 1,
        //
        stubSigBuilder.AppendByte(msig.GetCallingConvention() & ~IMAGE_CEE_CS_CALLCONV_HASTHIS);
        stubSigBuilder.AppendData(msig.NumFixedArgs() + 1);

        //
        // Append return type
        //
        SigPointer pReturn = msig.GetReturnProps();
        LPBYTE pReturnTypeBegin = (LPBYTE)pReturn.GetPtr();
        IfFailThrow(pReturn.SkipExactlyOne());
        LPBYTE pReturnTypeEnd = (LPBYTE)pReturn.GetPtr();
        
        stubSigBuilder.AppendBlob(pReturnTypeBegin, pReturnTypeEnd - pReturnTypeBegin);

        //
        // Append 'this'
        //
        stubSigBuilder.AppendElementType(ELEMENT_TYPE_CLASS);            
        stubSigBuilder.AppendToken(pTargetMT->GetCl());

        //
        // Copy rest of the arguments
        //
        if (msig.NextArg() != ELEMENT_TYPE_END)
        {
            SigPointer pFirstArg = msig.GetArgProps();
            LPBYTE pArgBegin = (LPBYTE) pFirstArg.GetPtr();
            LPBYTE pArgEnd = (LPBYTE) pTargetSig + pcTargetSig;

            stubSigBuilder.AppendBlob(pArgBegin, pArgEnd - pArgBegin);
        }

        //
        // Allocate new memory and copy over
        //
        DWORD pcStubSig = 0;
        PCCOR_SIGNATURE pStubSig = (PCCOR_SIGNATURE) stubSigBuilder.GetSignature(&pcStubSig);

        //
        // Find method using name + signature
        //
        StackScratchBuffer buffer;
        LPCUTF8 szMethodNameUTF8 = methodName.GetUTF8(buffer);
        pStubMD = MemberLoader::FindMethod(stubClassType.GetMethodTable(),
            szMethodNameUTF8, 
            pStubSig,
            pcStubSig,
            pTargetMT->GetModule());
            
        if (pStubMD == NULL)
        {
            CQuickBytes qbSig;
            
            PrettyPrintSig(
                pStubSig,
                pcStubSig,
                szMethodNameUTF8,
                &qbSig,
                pTargetMD->GetMDImport(),
                NULL);

            // Unfortunately the PrettyPrintSig doesn't print 'static' when the function is static
            // so we need to append 'static' here. No need to localize
            SString signature(SString::Utf8, (LPCUTF8)"static ");
            signature.AppendUTF8((LPCUTF8) qbSig.Ptr());
            
            COMPlusThrow(
                kMissingMethodException, 
                IDS_EE_INTEROP_STUB_CA_STUB_METHOD_MISSING,
                signature.GetUnicode(),
                stubClassName.GetUnicode()
                );       
       
        }
    }

    //
    // Check the Stub MD    
    //
    
    // Verify that the target interop method can call the stub method

    _ASSERTE(pTargetMD != NULL);

    StaticAccessCheckContext accessContext(pTargetMD, pTargetMT);

    if (!ClassLoader::CanAccess(
            &accessContext, 
            pStubClassMT,
            stubClassType.GetAssembly(), 
            pStubMD->GetAttrs(),
            pStubMD,
            NULL))
    {
        StackSString interopMethodName(SString::Utf8, pTargetMD->GetName());
        
        COMPlusThrow(
            kMethodAccessException, 
            IDS_EE_INTEROP_STUB_CA_NO_ACCESS_TO_STUB_METHOD,
            interopMethodName.GetUnicode(),
            methodName.GetUnicode()
            );
    }

    // The FindMethod call will make sure that it is static by matching signature.
    // So there is no need to check and throw
    _ASSERTE(pStubMD->IsStatic());
    
    *ppRetStubMD = pStubMD;

    RETURN S_OK;
}
#endif // FEATURE_COMINTEROP

MethodDesc* CreateInteropILStub(
                         ILStubState*       pss,
                         StubSigDesc*       pSigDesc,
                         CorNativeLinkType  nlType,
                         CorNativeLinkFlags nlFlags,
                         CorPinvokeMap      unmgdCallConv,
                         DWORD              dwStubFlags,            // NDirectStubFlags
                         int                nParamTokens,
                         mdParamDef*        pParamTokenArray,
                         int                iLCIDArg
                           )
{
    CONTRACT(MethodDesc*)
    {
        STANDARD_VM_CHECK;

        PRECONDITION(CheckPointer(pSigDesc));
        POSTCONDITION(CheckPointer(RETVAL));
    }
    CONTRACT_END;


    ///////////////////////////////
    //
    // MethodDesc creation 
    //
    ///////////////////////////////
    
    MethodDesc*     pStubMD = NULL;

    Module*         pModule = pSigDesc->m_pModule;
    Module*         pLoaderModule = pSigDesc->m_pLoaderModule;
    MethodDesc*     pTargetMD = pSigDesc->m_pMD;
    //
    // pTargetMD may be null in the case of calli pinvoke 
    // and vararg pinvoke.
    //

#ifdef FEATURE_COMINTEROP
    //
    // Try to locate predefined IL stub either defined in user code or hardcoded in CLR
    // If there is one, use the pointed method as the stub.
    // Skip pTargetMD == NULL case for reverse interop calls
    //
    if (pTargetMD && SUCCEEDED(FindPredefinedILStubMethod(pTargetMD, dwStubFlags, &pStubMD)))
    {
#ifndef CROSSGEN_COMPILE
        // We are about to execute method in pStubMD which could be in another module.
        // Call EnsureActive before make the call
        // This cannot be done during NGEN/PEVerify (in PASSIVE_DOMAIN) so I've moved it here
        pStubMD->EnsureActive();

        if (pStubMD->IsPreImplemented())
            RestoreNGENedStub(pStubMD);
#endif

        RETURN pStubMD;
    }
#endif // FEATURE_COMINTEROP

    // Otherwise, fall back to generating IL stub on-the-fly
    NDirectStubParameters    params(pSigDesc->m_sig,
                               &pSigDesc->m_typeContext,
                               pModule,
                               pLoaderModule,
                               nlType,
                               nlFlags,
                               unmgdCallConv,
                               dwStubFlags,
                               nParamTokens,
                               pParamTokenArray,
                               iLCIDArg
                               );

    // The following two ILStubCreatorHelperHolder are to recover the status when an
    // exception happen during the generation of the IL stubs. We need to free the
    // memory allocated and restore the ILStubCache.
    //
    // The following block is logically divided into two phases. The first phase is 
    // CreateOrGet IL Stub phase which we take a domain level lock. The second phase
    // is IL generation phase which we take a MethodDesc level lock. Taking two locks
    // is mainly designed for performance.
    //
    // ilStubCreatorHelper contains an instance of AllocMemTracker which tracks the
    // allocated memory during the creation of MethodDesc so that we are able to remove
    // them when releasing the ILStubCreatorHelperHolder or destructing ILStubCreatorHelper

    // When removing IL Stub from Cache, we have a constraint that only the thread which
    // creates the stub can remove it. Otherwise, any thread hits cache and gets the stub will
    // remove it from cache if OOM occurs

    {
        ILStubCreatorHelper ilStubCreatorHelper(pTargetMD, &params);

        // take the domain level lock
        ListLockHolder pILStubLock(pLoaderModule->GetDomain()->GetILStubGenLock());

        {
            // The holder will free the allocated MethodDesc and restore the ILStubCache
            // if exception happen.
            ILStubCreatorHelperHolder pCreateOrGetStubHolder(&ilStubCreatorHelper);
            pStubMD = pCreateOrGetStubHolder->GetStubMD();

            ///////////////////////////////
            //
            // IL generation
            //
            ///////////////////////////////

            {
                // take the MethodDesc level locker
                ListLockEntryHolder pEntry(ListLockEntry::Find(pILStubLock, pStubMD, "il stub gen lock"));

                ListLockEntryLockHolder pEntryLock(pEntry, FALSE);

                // We can release the holder for the first phase now
                pCreateOrGetStubHolder.SuppressRelease();

                {
                    // The holder will free the allocated MethodDesc and restore the ILStubCache
                    // if exception happen. The reason to get the holder again is to 
                    ILStubCreatorHelperHolder pGenILHolder(&ilStubCreatorHelper);

                    if (!pEntryLock.DeadlockAwareAcquire())
                    {
                        // the IL generation is not recursive!
                        UNREACHABLE_MSG("unexpected deadlock in IL stub generation!");
                    }

                    if (SF_IsSharedStub(params.m_dwStubFlags))
                    {
                        // Assure that pStubMD we have now has not been destroyed by other threads
                        pGenILHolder->GetStubMethodDesc();

                        while (pStubMD != pGenILHolder->GetStubMD())
                        {
                            pStubMD = pGenILHolder->GetStubMD();

                            pEntry.Assign(ListLockEntry::Find(pILStubLock, pStubMD, "il stub gen lock"));
                            pEntryLock.Assign(pEntry, FALSE);

                            if (!pEntryLock.DeadlockAwareAcquire())
                            {
                                // the IL generation is not recursive!
                                UNREACHABLE_MSG("unexpected deadlock in IL stub generation!");
                            }

                            pGenILHolder->GetStubMethodDesc();
                        }
                    }

                    for (;;)
                    {
                        // We have the entry lock now, we can release the global lock
                        pILStubLock.Release();

                        if (pEntry->m_hrResultCode != S_FALSE)
                        {
                            // We came in to generate the IL but someone 
                            // beat us so there's nothing to do
                            break;
                        }

                        ILStubResolver* pResolver = pStubMD->AsDynamicMethodDesc()->GetILStubResolver();

                        CONSISTENCY_CHECK((NULL == pResolver->GetStubMethodDesc()) || (pStubMD == pResolver->GetStubMethodDesc()));

                        if (pResolver->IsILGenerated())
                        {
                            // this stub already has its IL generated
                            break;
                        }

                        //
                        // Check that the stub signature and MethodDesc are compatible.  The JIT
                        // interface functions depend on this.
                        //
                        
                        {
                            SigPointer ptr = pSigDesc->m_sig.CreateSigPointer();

                            ULONG callConvInfo;
                            IfFailThrow(ptr.GetCallingConvInfo(&callConvInfo));

                            BOOL fSigIsStatic = !(callConvInfo & IMAGE_CEE_CS_CALLCONV_HASTHIS);

                            // CreateNDirectStubWorker will throw an exception for these cases.
                            BOOL fCanHaveThis = SF_IsDelegateStub(dwStubFlags) || SF_IsCOMStub(dwStubFlags);

                            if (fSigIsStatic || fCanHaveThis)
                            {
                                CONSISTENCY_CHECK(pStubMD->IsStatic() == (DWORD)fSigIsStatic);
                            }
                        }

                        {
                            ILStubGenHolder sgh(pResolver);

                            pResolver->SetStubMethodDesc(pStubMD);
                            pResolver->SetStubTargetMethodDesc(pTargetMD);

                            CreateNDirectStubWorker(pss,
                                                    pSigDesc,
                                                    nlType,
                                                    nlFlags,
                                                    unmgdCallConv,
                                                    dwStubFlags,
                                                    pStubMD,
                                                    pParamTokenArray,
                                                    iLCIDArg);

                            pResolver->SetTokenLookupMap(pss->GetTokenLookupMap());

                            pResolver->SetStubTargetMethodSig(
                                pss->GetStubTargetMethodSig(), 
                                pss->GetStubTargetMethodSigLength());

                            // we successfully generated the IL stub
                            sgh.SuppressRelease();
                        }

                        pEntry->m_hrResultCode = S_OK;
                        break;
                    }

                    // Link the MethodDesc onto the method table with the lock taken
                    NDirect::AddMethodDescChunkWithLockTaken(&params, pStubMD);

                    pGenILHolder.SuppressRelease();
                }
            }
        }
        ilStubCreatorHelper.SuppressRelease();
    }

#if defined(_TARGET_X86_)
    if (SF_IsForwardStub(dwStubFlags) && pTargetMD != NULL && !pTargetMD->IsVarArg())
    {
        // copy the stack arg byte count from the stub MD to the target MD - this number is computed
        // during stub generation and is copied to all target MDs that share the stub
        // (we don't set it for varargs - the number is call site specific)
        // also copy the "takes parameters with copy constructors" flag which is needed to generate
        // appropriate intercept stub

        WORD cbStackArgSize = pStubMD->AsDynamicMethodDesc()->GetNativeStackArgSize();
        BOOL fHasCopyCtorArgs = pStubMD->AsDynamicMethodDesc()->HasCopyCtorArgs();

        if (pTargetMD->IsNDirect())
        {
            NDirectMethodDesc *pTargetNMD = (NDirectMethodDesc *)pTargetMD;
            
            pTargetNMD->SetStackArgumentSize(cbStackArgSize, (CorPinvokeMap)0);
            pTargetNMD->SetHasCopyCtorArgs(fHasCopyCtorArgs);
        }
#ifdef FEATURE_COMINTEROP
        else
        {
            if (SF_IsCOMStub(dwStubFlags))
            {
                ComPlusCallInfo *pComInfo = ComPlusCallInfo::FromMethodDesc(pTargetMD);

                if (pComInfo != NULL)
                {
                    pComInfo->SetStackArgumentSize(cbStackArgSize);
                    pComInfo->SetHasCopyCtorArgs(fHasCopyCtorArgs);
                }
            }
        }
#endif // FEATURE_COMINTEROP
    }
#endif // defined(_TARGET_X86_)

    RETURN pStubMD;
}

MethodDesc* NDirect::CreateCLRToNativeILStub(
                StubSigDesc*       pSigDesc,
                CorNativeLinkType  nlType,
                CorNativeLinkFlags nlFlags,
                CorPinvokeMap      unmgdCallConv,
                DWORD              dwStubFlags) // NDirectStubFlags
{
    CONTRACT(MethodDesc*)
    {
        STANDARD_VM_CHECK;

        PRECONDITION(CheckPointer(pSigDesc));
        POSTCONDITION(CheckPointer(RETVAL));
    }
    CONTRACT_END;

    int         iLCIDArg = 0;
    int         numArgs = 0;
    int         numParamTokens = 0;
    mdParamDef* pParamTokenArray = NULL;

    CreateNDirectStubAccessMetadata(pSigDesc,
                                    unmgdCallConv,
                                    &dwStubFlags,
                                    &iLCIDArg,
                                    &numArgs);
    
    Module *pModule = pSigDesc->m_pModule;
    numParamTokens = numArgs + 1;
    pParamTokenArray = (mdParamDef*)_alloca(numParamTokens * sizeof(mdParamDef));
    CollateParamTokens(pModule->GetMDImport(), pSigDesc->m_tkMethodDef, numArgs, pParamTokenArray);

    // for interop vectors that have declarative security, we need
    //      to update the stub flags to ensure a unique stub hash
    //      is generated based on the marshalling signature AND
    //      any declarative security.
    // IMPORTANT: This will only inject the security callouts for
    //      interop functionality which has a non-null target MethodDesc.
    //      Currently, this is known to exclude things like native
    //      function ptrs. It is assumed that if the target is not
    //      attribute'able for metadata, then it cannot have declarative
    //      security - and that the target is not attributable if it was
    //      not passed to this function.
    MethodDesc *pMD = pSigDesc->m_pMD;
    if (pMD != NULL && SF_IsForwardStub(dwStubFlags))
    {
        // In an AppX process there is only one fully trusted AppDomain, so there is never any need to insert
        // a security callout on the stubs.
        if (!AppX::IsAppXProcess())
        {
#ifdef FEATURE_COMINTEROP
            if (pMD->IsComPlusCall() || pMD->IsGenericComPlusCall())
            {
                // To preserve Whidbey behavior, we only enforce the implicit demand for
                // unmanaged code permission.
                MethodTable* pMT = ComPlusCallInfo::FromMethodDesc(pMD)->m_pInterfaceMT;
                if (pMT->ClassRequiresUnmanagedCodeCheck() &&
                    !pMD->HasSuppressUnmanagedCodeAccessAttr())
                {
                    dwStubFlags |= NDIRECTSTUB_FL_HASDECLARATIVESECURITY;
                }
            }
            else
#endif // FEATURE_COMPINTEROP
            if (pMD->IsInterceptedForDeclSecurity())
            {
                dwStubFlags |= NDIRECTSTUB_FL_HASDECLARATIVESECURITY;
            }
        }
    }

    NewHolder<ILStubState> pStubState;

#ifdef FEATURE_COMINTEROP
    if (SF_IsCOMStub(dwStubFlags))
    {
        if (SF_IsReverseStub(dwStubFlags))
        {
            pStubState = new COMToCLR_ILStubState(pModule, pSigDesc->m_sig, &pSigDesc->m_typeContext, dwStubFlags, iLCIDArg, pMD);
        }
        else
        {
            pStubState = new CLRToCOM_ILStubState(pModule, pSigDesc->m_sig, &pSigDesc->m_typeContext, dwStubFlags, iLCIDArg, pMD);
        }
    }
    else
#endif
    {
        pStubState = new PInvoke_ILStubState(pModule, pSigDesc->m_sig, &pSigDesc->m_typeContext, dwStubFlags, unmgdCallConv, iLCIDArg, pMD);
    }

    MethodDesc* pStubMD;
    pStubMD = CreateInteropILStub(
                pStubState,
                pSigDesc,
                nlType,
                nlFlags,
                unmgdCallConv,
                dwStubFlags,
                numParamTokens,
                pParamTokenArray,
                iLCIDArg);



    RETURN pStubMD;
}

#ifdef FEATURE_COMINTEROP
MethodDesc* NDirect::CreateFieldAccessILStub(
                PCCOR_SIGNATURE    szMetaSig,
                DWORD              cbMetaSigSize,
                Module*            pModule,
                mdFieldDef         fd,
                DWORD              dwStubFlags, // NDirectStubFlags
                FieldDesc*         pFD)
{
    CONTRACT(MethodDesc*)
    {
        STANDARD_VM_CHECK;

        PRECONDITION(CheckPointer(szMetaSig));
        PRECONDITION(CheckPointer(pModule));
        PRECONDITION(CheckPointer(pFD, NULL_OK));
        PRECONDITION(SF_IsFieldGetterStub(dwStubFlags) || SF_IsFieldSetterStub(dwStubFlags));
        POSTCONDITION(CheckPointer(RETVAL));
    }
    CONTRACT_END;

    int numArgs = (SF_IsFieldSetterStub(dwStubFlags) ? 1 : 0);
    int numParamTokens = numArgs + 1;

    // make sure we capture marshaling metadata
    mdParamDef* pParamTokenArray = (mdParamDef *)_alloca(numParamTokens * sizeof(mdParamDef));
    pParamTokenArray[0] = mdParamDefNil;
    pParamTokenArray[numArgs] = (mdParamDef)fd;

    // fields are never preserve-sig
    dwStubFlags |= NDIRECTSTUB_FL_DOHRESULTSWAPPING;

    // convert field signature to getter/setter signature
    SigBuilder sigBuilder;

    sigBuilder.AppendData(IMAGE_CEE_CS_CALLCONV_DEFAULT | IMAGE_CEE_CS_CALLCONV_HASTHIS);
    sigBuilder.AppendData(numArgs);

    if (SF_IsFieldSetterStub(dwStubFlags))
    {
        // managed setter returns void
        sigBuilder.AppendElementType(ELEMENT_TYPE_VOID);
    }

    CONSISTENCY_CHECK(*szMetaSig == IMAGE_CEE_CS_CALLCONV_FIELD);

    sigBuilder.AppendBlob((const PVOID)(szMetaSig + 1), cbMetaSigSize - 1);
    szMetaSig = (PCCOR_SIGNATURE)sigBuilder.GetSignature(&cbMetaSigSize);

    StubSigDesc sigDesc(NULL, Signature(szMetaSig, cbMetaSigSize), pModule);

#ifdef _DEBUG
    sigDesc.m_pDebugName = pFD->GetDebugName();
    sigDesc.m_pDebugClassName = pFD->GetEnclosingMethodTable()->GetDebugClassName();
#endif // _DEBUG

    Signature signature(szMetaSig, cbMetaSigSize);
    NewHolder<ILStubState> pStubState = new COMToCLRFieldAccess_ILStubState(pModule, signature, &sigDesc.m_typeContext, dwStubFlags, pFD);

    MethodDesc* pStubMD;
    pStubMD = CreateInteropILStub(
                pStubState,
                &sigDesc,
                (CorNativeLinkType)0,
                (CorNativeLinkFlags)0,
                (CorPinvokeMap)0,
                dwStubFlags,
                numParamTokens,
                pParamTokenArray,
                -1);

    RETURN pStubMD;
}
#endif // FEATURE_COMINTEROP

MethodDesc* NDirect::CreateCLRToNativeILStub(PInvokeStaticSigInfo* pSigInfo,
                         DWORD dwStubFlags,
                         MethodDesc* pMD)
{
    STANDARD_VM_CONTRACT;
    
    StubSigDesc sigDesc(pMD, pSigInfo);

    if (SF_IsWinRTDelegateStub(dwStubFlags))
    {
        _ASSERTE(pMD->IsEEImpl());

        return CreateCLRToNativeILStub(&sigDesc,
                                       (CorNativeLinkType)0,
                                       (CorNativeLinkFlags)0,
                                       (CorPinvokeMap)0,
                                       (pSigInfo->GetStubFlags() | dwStubFlags) & ~NDIRECTSTUB_FL_DELEGATE);
    }
    else
    {
        return CreateCLRToNativeILStub(&sigDesc,
                                       pSigInfo->GetCharSet(), 
                                       pSigInfo->GetLinkFlags(), 
                                       pSigInfo->GetCallConv(), 
                                       pSigInfo->GetStubFlags() | dwStubFlags);
    }
}

MethodDesc* NDirect::GetILStubMethodDesc(NDirectMethodDesc* pNMD, PInvokeStaticSigInfo* pSigInfo, DWORD dwStubFlags)
{
    STANDARD_VM_CONTRACT;

    MethodDesc* pStubMD = NULL;

    if (!pNMD->IsVarArgs() || SF_IsForNumParamBytes(dwStubFlags))
    {
        if (pNMD->IsClassConstructorTriggeredByILStub())
        {
            dwStubFlags |= NDIRECTSTUB_FL_TRIGGERCCTOR;
        }

        pStubMD = CreateCLRToNativeILStub(
            pSigInfo, 
            dwStubFlags & ~NDIRECTSTUB_FL_FOR_NUMPARAMBYTES, 
            pNMD);
    }

    return pStubMD;
}

MethodDesc* GetStubMethodDescFromInteropMethodDesc(MethodDesc* pMD, DWORD dwStubFlags)
{
    STANDARD_VM_CONTRACT;

    BOOL fGcMdaEnabled = FALSE;
#ifdef MDA_SUPPORTED
    if (MDA_GET_ASSISTANT(GcManagedToUnmanaged) || MDA_GET_ASSISTANT(GcUnmanagedToManaged))
    {
        // We never generate checks for these MDAs to NGEN'ed stubs so if they are
        // enabled, a new stub must be generated (the perf impact is huge anyway).
        fGcMdaEnabled = TRUE;
    }
#endif // MDA_SUPPORTED

#ifdef FEATURE_COMINTEROP
    if (SF_IsReverseCOMStub(dwStubFlags))
    {
        if (fGcMdaEnabled)
            return NULL;

        // reverse COM stubs live in a hash table
        StubMethodHashTable *pHash = pMD->GetLoaderModule()->GetStubMethodHashTable();
        return (pHash == NULL ? NULL : pHash->FindMethodDesc(pMD));
    }
    else
#endif // FEATURE_COMINTEROP
    if (pMD->IsNDirect())
    {
        NDirectMethodDesc* pNMD = (NDirectMethodDesc*)pMD;
        return ((fGcMdaEnabled && !pNMD->IsQCall()) ? NULL : pNMD->ndirect.m_pStubMD.GetValueMaybeNull());
    }
#ifdef FEATURE_COMINTEROP
    else if (pMD->IsComPlusCall() || pMD->IsGenericComPlusCall())
    {
#ifdef MDA_SUPPORTED
        if (MDA_GET_ASSISTANT(RaceOnRCWCleanup))
        {
            // we never generate this callout to NGEN'ed stubs
            return NULL;
        }
#endif // MDA_SUPPORTED

        if (NDirect::IsHostHookEnabled())
        {
            MethodTable *pMT = pMD->GetMethodTable();
            if (pMT->IsProjectedFromWinRT() || pMT->IsWinRTRedirectedInterface(TypeHandle::Interop_ManagedToNative))
            {
                // WinRT NGENed stubs are optimized for the non-hosted scenario and
                // must be rejected if we are hosted.
                return NULL;
            }
        }

        if (fGcMdaEnabled)
            return NULL;

        ComPlusCallInfo *pComInfo = ComPlusCallInfo::FromMethodDesc(pMD);
        return (pComInfo == NULL ? NULL : pComInfo->m_pStubMD.GetValueMaybeNull());
    }
#endif // FEATURE_COMINTEROP
    else if (pMD->IsEEImpl())
    {
        if (fGcMdaEnabled)
            return NULL;

        DelegateEEClass *pClass = (DelegateEEClass *)pMD->GetClass();
        if (SF_IsReverseStub(dwStubFlags))
        {
            return pClass->m_pReverseStubMD;
        }
        else
        {
#ifdef FEATURE_COMINTEROP
            if (SF_IsWinRTDelegateStub(dwStubFlags))
            {
                if (NDirect::IsHostHookEnabled() && pMD->GetMethodTable()->IsProjectedFromWinRT())
                {
                    // WinRT NGENed stubs are optimized for the non-hosted scenario and
                    // must be rejected if we are hosted.
                    return NULL;
                }

                return pClass->m_pComPlusCallInfo->m_pStubMD.GetValueMaybeNull();
            }
            else
#endif // FEATURE_COMINTEROP
            {
                return pClass->m_pForwardStubMD;
            }
        }
    }
    else if (pMD->IsIL())
    {
        // these are currently only created at runtime, not at NGEN time
        return NULL;
    }
    else
    {
        UNREACHABLE_MSG("unexpected type of MethodDesc");
    }
}

#ifndef CROSSGEN_COMPILE

PCODE NDirect::GetStubForILStub(MethodDesc* pManagedMD, MethodDesc** ppStubMD, DWORD dwStubFlags)
{
    CONTRACT(PCODE)
    {
        STANDARD_VM_CHECK;

        PRECONDITION(CheckPointer(pManagedMD));
        POSTCONDITION(RETVAL != NULL);
    }
    CONTRACT_END;

    // pStubMD, if provided, must be preimplemented.
    CONSISTENCY_CHECK( (*ppStubMD == NULL) || (*ppStubMD)->IsPreImplemented() );

    if (NULL == *ppStubMD)
    {
        PInvokeStaticSigInfo sigInfo(pManagedMD);
        *ppStubMD = NDirect::CreateCLRToNativeILStub(&sigInfo, dwStubFlags, pManagedMD);
    }

    RETURN JitILStub(*ppStubMD);
}

PCODE NDirect::GetStubForILStub(NDirectMethodDesc* pNMD, MethodDesc** ppStubMD, DWORD dwStubFlags)
{
    STANDARD_VM_CONTRACT;

    PCODE pStub = NULL;

    // pStubMD, if provided, must be preimplemented.
    CONSISTENCY_CHECK( (*ppStubMD == NULL) || (*ppStubMD)->IsPreImplemented() );

    if (NULL == *ppStubMD)
    {
        PInvokeStaticSigInfo sigInfo;
        NDirect::PopulateNDirectMethodDesc(pNMD, &sigInfo, /* throwOnError = */ !SF_IsForNumParamBytes(dwStubFlags));

        *ppStubMD = NDirect::GetILStubMethodDesc(pNMD, &sigInfo, dwStubFlags);
    }

    if (SF_IsForNumParamBytes(dwStubFlags))
        return NULL;

    if (*ppStubMD)
    {
        pStub = JitILStub(*ppStubMD);
    }
    else
    {
        CONSISTENCY_CHECK(pNMD->IsVarArgs());
        
        //
        // varargs goes through vararg NDirect stub
        //
        pStub = TheVarargNDirectStub(pNMD->HasRetBuffArg());
    }

    {
        NDirectLink(pNMD);
    }

    //
    // NOTE: there is a race in updating this MethodDesc.  We depend on all 
    // threads getting back the same DynamicMethodDesc for a particular
    // NDirectMethodDesc, in that case, the locking around the actual JIT
    // operation will prevent the code from being jitted more than once.  
    // By the time we get here, all threads get the same address of code 
    // back from the JIT operation and they all just fill in the same value
    // here.
    //
    // In the NGEN case, all threads will get the same preimplemented code
    // address much like the JIT case.
    //

    return pStub;
}

PCODE JitILStub(MethodDesc* pStubMD)
{
    STANDARD_VM_CONTRACT;

    PCODE pCode = pStubMD->GetNativeCode();

    if (pCode == NULL)
    {
        ///////////////////////////////
        //
        // Code generation
        //
        ///////////////////////////////


        if (pStubMD->IsDynamicMethod())
        {
            //
            // A dynamically generated IL stub
            //
            
            CORJIT_FLAGS jitFlags = pStubMD->AsDynamicMethodDesc()->GetILStubResolver()->GetJitFlags();
            pCode = pStubMD->MakeJitWorker(NULL, jitFlags);

            _ASSERTE(pCode == pStubMD->GetNativeCode());            
        }
        else
        {     
            //
            // A static IL stub that is pointing to a static method in user assembly
            // Compile it and return the native code
            //

            // This returns the stable entry point
            pCode = pStubMD->DoPrestub(NULL);

            _ASSERTE(pCode == pStubMD->GetStableEntryPoint());            
        }        
    }

    if (!pStubMD->IsDynamicMethod()) 
    {
        // We need an entry point that can be called multiple times
        pCode = pStubMD->GetMultiCallableAddrOfCode();
    }

    return pCode;
}

MethodDesc* RestoreNGENedStub(MethodDesc* pStubMD)
{
    CONTRACTL
    {
        STANDARD_VM_CHECK;
        PRECONDITION(CheckPointer(pStubMD));
    }
    CONTRACTL_END;

#ifdef FEATURE_PREJIT
    pStubMD->CheckRestore();

    PCODE pCode = pStubMD->GetPreImplementedCode();
    if (pCode != NULL)
    {
        TADDR pFixupList = pStubMD->GetFixupList();
        if (pFixupList != NULL)
        {
            Module* pZapModule = pStubMD->GetZapModule();
            _ASSERTE(pZapModule != NULL);
            if (!pZapModule->FixupDelayList(pFixupList))
            {
                _ASSERTE(!"FixupDelayList failed");
                ThrowHR(COR_E_BADIMAGEFORMAT);
            }
        }

#if defined(HAVE_GCCOVER)
        if (GCStress<cfg_instr_ngen>::IsEnabled())
            SetupGcCoverage(pStubMD, (BYTE*) pCode);
#endif // HAVE_GCCOVER

    }
    else
    {
        // We only pass a non-NULL pStubMD to GetStubForILStub() below if pStubMD is preimplemeneted.
        pStubMD = NULL;
    }
#endif // FEATURE_PREJIT

    return pStubMD;
}

PCODE GetStubForInteropMethod(MethodDesc* pMD, DWORD dwStubFlags, MethodDesc **ppStubMD)
{
    CONTRACT(PCODE)
    {
        STANDARD_VM_CHECK;

        PRECONDITION(CheckPointer(pMD));
        PRECONDITION(pMD->IsNDirect() || pMD->IsComPlusCall() || pMD->IsGenericComPlusCall() || pMD->IsEEImpl() || pMD->IsIL());
    }
    CONTRACT_END;

    PCODE                   pStub = NULL;
    MethodDesc*             pStubMD = NULL;

    pStubMD = GetStubMethodDescFromInteropMethodDesc(pMD, dwStubFlags);
    if (pStubMD != NULL)
    {
        pStubMD = RestoreNGENedStub(pStubMD);
    }

    if ((NULL == pStubMD) && (SF_IsNGENedStub(dwStubFlags)))
    {
        // Return NULL -- the caller asked only for an ngened stub and 
        // one does not exist, so don't do any more work.
        CONSISTENCY_CHECK(pStub == NULL);
    }
    else
    if (pMD->IsNDirect())
    {
        NDirectMethodDesc* pNMD = (NDirectMethodDesc*)pMD;
        pStub = NDirect::GetStubForILStub(pNMD, &pStubMD, dwStubFlags);
    }
#ifdef FEATURE_COMINTEROP
    else
    if (pMD->IsComPlusCall() || pMD->IsGenericComPlusCall())
    {
        pStub = ComPlusCall::GetStubForILStub(pMD, &pStubMD);
    }
#endif // FEATURE_COMINTEROP
    else
    if (pMD->IsEEImpl())
    {
        CONSISTENCY_CHECK(pMD->GetMethodTable()->IsDelegate());
        EEImplMethodDesc* pDelegateMD = (EEImplMethodDesc*)pMD;
        pStub = COMDelegate::GetStubForILStub(pDelegateMD, &pStubMD, dwStubFlags);
    }
    else
    if (pMD->IsIL())
    {
        CONSISTENCY_CHECK(SF_IsReverseStub(dwStubFlags));
        pStub = NDirect::GetStubForILStub(pMD, &pStubMD, dwStubFlags);
    }
    else
    {
        UNREACHABLE_MSG("unexpected MethodDesc type");
    }

    if (pStubMD != NULL && pStubMD->IsILStub() && pStubMD->AsDynamicMethodDesc()->IsStubNeedsCOMStarted())
    {
        // the stub uses COM so make sure that it is started
        EnsureComStarted();
    }

    if (ppStubMD != NULL)
        *EnsureWritablePages(ppStubMD) = pStubMD;

    RETURN pStub;
}

#ifdef FEATURE_COMINTEROP
void CreateCLRToDispatchCOMStub(
            MethodDesc *    pMD,
            DWORD           dwStubFlags)             // NDirectStubFlags
{
    CONTRACTL
    {
        STANDARD_VM_CHECK;

        PRECONDITION(CheckPointer(pMD));
    }
    CONTRACTL_END;

    _ASSERTE(SF_IsCOMLateBoundStub(dwStubFlags) || SF_IsCOMEventCallStub(dwStubFlags));

    // If we are dealing with a COM event call, then we need to initialize the
    // COM event call information.
    if (SF_IsCOMEventCallStub(dwStubFlags))
    {
        _ASSERTE(pMD->IsComPlusCall()); //  no generic COM eventing
        ((ComPlusCallMethodDesc *)pMD)->InitComEventCallInfo();
    }

    // Get the call signature information
    StubSigDesc sigDesc(pMD);

    int         iLCIDArg = 0;
    int         numArgs = 0;
    int         numParamTokens = 0;
    mdParamDef* pParamTokenArray = NULL;

    CreateNDirectStubAccessMetadata(&sigDesc,
                                    (CorPinvokeMap)0,
                                    &dwStubFlags,
                                    &iLCIDArg,
                                    &numArgs);

    numParamTokens = numArgs + 1;
    pParamTokenArray = (mdParamDef*)_alloca(numParamTokens * sizeof(mdParamDef));
    CollateParamTokens(sigDesc.m_pModule->GetMDImport(), sigDesc.m_tkMethodDef, numArgs, pParamTokenArray);

    DispatchStubState MyStubState;

    CreateNDirectStubWorker(&MyStubState,
                            &sigDesc,
                            (CorNativeLinkType)0,
                            (CorNativeLinkFlags)0,
                            (CorPinvokeMap)0,
                            dwStubFlags | NDIRECTSTUB_FL_COM,
                            pMD,
                            pParamTokenArray,
                            iLCIDArg);

    _ASSERTE(pMD->IsComPlusCall()); // no generic disp-calls
    ((ComPlusCallMethodDesc *)pMD)->InitRetThunk();
}


#endif // FEATURE_COMINTEROP

/*static*/
LPVOID NDirect::NDirectGetEntryPoint(NDirectMethodDesc *pMD, HINSTANCE hMod)
{
    // GetProcAddress cannot be called while preemptive GC is disabled.
    // It requires the OS to take the loader lock.
    CONTRACT(LPVOID)
    {
        STANDARD_VM_CHECK;
        PRECONDITION(CheckPointer(pMD));
        POSTCONDITION(CheckPointer(RETVAL, NULL_OK));
    }
    CONTRACT_END;

    g_IBCLogger.LogNDirectCodeAccess(pMD);

#ifdef MDA_SUPPORTED
    MDA_TRIGGER_ASSISTANT(PInvokeLog, LogPInvoke(pMD, hMod));
#endif

    RETURN pMD->FindEntryPoint(hMod);
}

VOID NDirectMethodDesc::SetNDirectTarget(LPVOID pTarget)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;

        PRECONDITION(IsNDirect());
        PRECONDITION(pTarget != NULL);
    }
    CONTRACTL_END;

    Stub *pInterceptStub = NULL;

    BOOL fHook = FALSE;

    // Host hooks are not supported for Mac CoreCLR.
    if (NDirect::IsHostHookEnabled())
    {
#ifdef _WIN64
        // we will call CallNeedsHostHook on every invocation for back compat
        fHook = TRUE;
#else // _WIN64
        fHook = CallNeedsHostHook((size_t)pTarget);
#endif // _WIN64

#ifdef _DEBUG
        if (g_pConfig->ShouldGenerateStubForHost())
        {
            fHook = TRUE;
        }
#endif
    }

#ifdef _TARGET_X86_


#ifdef MDA_SUPPORTED
    if (!IsQCall() && MDA_GET_ASSISTANT(PInvokeStackImbalance))
    {
        pInterceptStub = GenerateStubForMDA(pTarget, pInterceptStub, fHook);
    }
#endif // MDA_SUPPORTED


#endif // _TARGET_X86_


    NDirectWriteableData* pWriteableData = GetWriteableData();
    EnsureWritablePages(pWriteableData);
    g_IBCLogger.LogNDirectCodeAccess(this);

    if (pInterceptStub != NULL WIN64_ONLY(|| fHook))
    {
        ndirect.m_pNativeNDirectTarget = pTarget;
        
#if defined(_TARGET_X86_)
        pTarget = (PVOID)pInterceptStub->GetEntryPoint();

        LPVOID oldTarget = GetNDirectImportThunkGlue()->GetEntrypoint();
        if (FastInterlockCompareExchangePointer(&pWriteableData->m_pNDirectTarget, pTarget,
                                                oldTarget) != oldTarget)
        {
            pInterceptStub->DecRef();
        }
#else
        _ASSERTE(pInterceptStub == NULL); // we don't intercept for anything else than host on !_TARGET_X86_
#endif
    }
    else
    {
        pWriteableData->m_pNDirectTarget = pTarget;
    }
}



#if defined(_TARGET_X86_) && defined(MDA_SUPPORTED)
EXTERN_C VOID __stdcall PInvokeStackImbalanceWorker(StackImbalanceCookie *pSICookie, DWORD dwPostESP)
{
    STATIC_CONTRACT_THROWS;
    STATIC_CONTRACT_GC_TRIGGERS;
    STATIC_CONTRACT_MODE_PREEMPTIVE; // we've already switched to preemptive

    // make sure we restore the original Win32 last error before leaving this function - we are
    // called right after returning from the P/Invoke target and the error has not been saved yet
    BEGIN_PRESERVE_LAST_ERROR;

    MdaPInvokeStackImbalance* pProbe = MDA_GET_ASSISTANT(PInvokeStackImbalance);

    // This MDA must be active if we generated a call to PInvokeStackImbalanceHelper
    _ASSERTE(pProbe);

    pProbe->CheckStack(pSICookie, dwPostESP);

    END_PRESERVE_LAST_ERROR;
}
#endif // _TARGET_X86_ && MDA_SUPPORTED


// Preserving good error info from DllImport-driven LoadLibrary is tricky because we keep loading from different places
// if earlier loads fail and those later loads obliterate error codes.
//
// This tracker object will keep track of the error code in accordance to priority:
//
//   low-priority:      unknown error code (should never happen)
//   medium-priority:   dll not found
//   high-priority:     dll found but error during loading
//   
// We will overwrite the previous load's error code only if the new error code is higher priority.
//

class LoadLibErrorTracker
{
private:
    static const DWORD const_priorityNotFound     = 10;
    static const DWORD const_priorityAccessDenied = 20;
    static const DWORD const_priorityCouldNotLoad = 99999;
public:
    LoadLibErrorTracker()
    {
        LIMITED_METHOD_CONTRACT;
        m_hr = E_FAIL;
        m_priorityOfLastError = 0;
    }

    VOID TrackErrorCode(DWORD dwLastError)
    {
        LIMITED_METHOD_CONTRACT;

        DWORD priority;

        switch (dwLastError)
        {
            case ERROR_FILE_NOT_FOUND:
            case ERROR_PATH_NOT_FOUND:
            case ERROR_MOD_NOT_FOUND:
            case ERROR_DLL_NOT_FOUND:
                priority = const_priorityNotFound;
                break;

            // If we can't access a location, we can't know if the dll's there or if it's good.
            // Still, this is probably more unusual (and thus of more interest) than a dll-not-found
            // so give it an intermediate priority.
            case ERROR_ACCESS_DENIED:
                priority = const_priorityAccessDenied;

            // Assume all others are "dll found but couldn't load."
            default:
                priority = const_priorityCouldNotLoad;
                break;
        }

        UpdateHR(priority, HRESULT_FROM_WIN32(dwLastError));
    }

    // Sets the error code to HRESULT as could not load DLL
    void TrackHR_CouldNotLoad(HRESULT hr)
    {
        UpdateHR(const_priorityCouldNotLoad, hr);
    }
    
    HRESULT GetHR()
    {
        return m_hr;
    }

    void DECLSPEC_NORETURN Throw(SString &libraryNameOrPath)
    {
        STANDARD_VM_CONTRACT;

        HRESULT theHRESULT = GetHR();
        if (theHRESULT == HRESULT_FROM_WIN32(ERROR_BAD_EXE_FORMAT))
        {
            COMPlusThrow(kBadImageFormatException);
        }
        else
        {
            SString hrString;
            GetHRMsg(theHRESULT, hrString);
            COMPlusThrow(kDllNotFoundException, IDS_EE_NDIRECT_LOADLIB, libraryNameOrPath.GetUnicode(), hrString);
        }

        __UNREACHABLE();
    }

private:
    void UpdateHR(DWORD priority, HRESULT hr)
    {
        if (priority > m_priorityOfLastError)
        {
            m_hr                  = hr;
            m_priorityOfLastError = priority;
        }
    }

    HRESULT m_hr;
    DWORD   m_priorityOfLastError;
};  // class LoadLibErrorTracker

//  Local helper function for the LoadLibraryModule function below
static HMODULE LocalLoadLibraryHelper( LPCWSTR name, DWORD flags, LoadLibErrorTracker *pErrorTracker )
{
    STANDARD_VM_CONTRACT;

    HMODULE hmod = NULL;

#ifndef FEATURE_PAL

    if ((flags & 0xFFFFFF00) != 0
#ifndef FEATURE_CORESYSTEM
        && NDirect::SecureLoadLibrarySupported()
#endif // !FEATURE_CORESYSTEM
        )
    {
        hmod = CLRLoadLibraryEx(name, NULL, flags & 0xFFFFFF00);
        if(hmod != NULL)
        {
            return hmod;
        }

        DWORD dwLastError = GetLastError();
        if (dwLastError != ERROR_INVALID_PARAMETER)
        {
            pErrorTracker->TrackErrorCode(dwLastError);
            return hmod;
        }
    }

    hmod = CLRLoadLibraryEx(name, NULL, flags & 0xFF);
    
#else // !FEATURE_PAL
    hmod = CLRLoadLibrary(name);
#endif // !FEATURE_PAL
        
    if (hmod == NULL)
    {
        pErrorTracker->TrackErrorCode(GetLastError());
    }
    
    return hmod;
}

//  Local helper function for the LoadLibraryFromPath function below
static HMODULE LocalLoadLibraryDirectHelper(LPCWSTR name, DWORD flags, LoadLibErrorTracker *pErrorTracker)
{
    STANDARD_VM_CONTRACT;

#ifndef FEATURE_PAL
    return LocalLoadLibraryHelper(name, flags, pErrorTracker);
#else // !FEATURE_PAL
    // Load the library directly, and don't register it yet with PAL. The system library handle is required here, not the PAL
    // handle. The system library handle is registered with PAL to get a PAL handle in LoadLibraryModuleViaHost().
    HMODULE hmod = PAL_LoadLibraryDirect(name);

    if (hmod == NULL)
    {
        pErrorTracker->TrackErrorCode(GetLastError());
    }

    return hmod;
#endif // !FEATURE_PAL
}

#if !defined(FEATURE_PAL)
bool         NDirect::s_fSecureLoadLibrarySupported = false;
#endif

#define TOLOWER(a) (((a) >= W('A') && (a) <= W('Z')) ? (W('a') + (a - W('A'))) : (a))
#define TOHEX(a)   ((a)>=10 ? W('a')+(a)-10 : W('0')+(a))

// static
HMODULE NDirect::LoadLibraryFromPath(LPCWSTR libraryPath)
{
    STANDARD_VM_CONTRACT;

    LoadLibErrorTracker errorTracker;
    const HMODULE systemModuleHandle =
        LocalLoadLibraryDirectHelper(libraryPath, GetLoadWithAlteredSearchPathFlag(), &errorTracker);
    if (systemModuleHandle == nullptr)
    {
        SString libraryPathSString(libraryPath);
        errorTracker.Throw(libraryPathSString);
    }
    return systemModuleHandle;
}

/* static */
HMODULE NDirect::LoadLibraryModuleViaHost(NDirectMethodDesc * pMD, AppDomain* pDomain, const wchar_t* wszLibName)
{
    STANDARD_VM_CONTRACT;
    //Dynamic Pinvoke Support:
    //Check if we  need to provide the host a chance to provide the unmanaged dll 

#ifndef PLATFORM_UNIX
    // Prevent Overriding of Windows API sets.
    // This is replicating quick check from the OS implementation of api sets.
    if (SString::_wcsnicmp(wszLibName, W("api-"), 4) == 0 || SString::_wcsnicmp(wszLibName, W("ext-"), 4) == 0)
    {
        return NULL;
    }
#endif

    LPVOID hmod = NULL;
    CLRPrivBinderCoreCLR *pTPABinder = pDomain->GetTPABinderContext();
    Assembly* pAssembly = pMD->GetMethodTable()->GetAssembly();
   
    PEFile *pManifestFile = pAssembly->GetManifestFile();
    PTR_ICLRPrivBinder pBindingContext = pManifestFile->GetBindingContext();

    //Step 0: Check if  the assembly was bound using TPA. 
    //        The Binding Context can be null or an overridden TPA context
    if (pBindingContext == NULL)
    {
        // If we do not have any binder associated, then return to the default resolution mechanism.
        return NULL;
    }    

    UINT_PTR assemblyBinderID = 0;
    IfFailThrow(pBindingContext->GetBinderID(&assemblyBinderID));
        
    ICLRPrivBinder *pCurrentBinder = reinterpret_cast<ICLRPrivBinder *>(assemblyBinderID);

    // For assemblies bound via TPA binder, we should use the standard mechanism to make the pinvoke call.
    if (AreSameBinderInstance(pCurrentBinder, pTPABinder))
    {
        return NULL;
    }

#ifdef FEATURE_COMINTEROP
    CLRPrivBinderWinRT *pWinRTBinder = pDomain->GetWinRtBinder();
    if (AreSameBinderInstance(pCurrentBinder, pWinRTBinder))
    {
        // We could be here when a non-WinRT assembly load is triggerred by a winmd (e.g. System.Runtime being loaded due to
        // types being referenced from Windows.Foundation.Winmd) or when dealing with a winmd (which is bound using WinRT binder).
        //
        // For this, we should use the standard mechanism to make pinvoke call as well.
        return NULL;
    }
#endif // FEATURE_COMINTEROP
    
    //Step 1: If the assembly was not bound using TPA,
    //        Call System.Runtime.Loader.AssemblyLoadContext.ResolveUnamanagedDll to give
    //        The custom assembly context a chance to load the unmanaged dll.
    
    GCX_COOP();
            
    STRINGREF pUnmanagedDllName;
    pUnmanagedDllName = StringObject::NewString(wszLibName);
        
    GCPROTECT_BEGIN(pUnmanagedDllName);

    // Get the pointer to the managed assembly load context
    INT_PTR ptrManagedAssemblyLoadContext = ((CLRPrivBinderAssemblyLoadContext *)pCurrentBinder)->GetManagedAssemblyLoadContext();

    // Prepare to invoke  System.Runtime.Loader.AssemblyLoadContext.ResolveUnamanagedDll method.
    PREPARE_NONVIRTUAL_CALLSITE(METHOD__ASSEMBLYLOADCONTEXT__RESOLVEUNMANAGEDDLL);
    DECLARE_ARGHOLDER_ARRAY(args, 2);
    args[ARGNUM_0]  = STRINGREF_TO_ARGHOLDER(pUnmanagedDllName);
    args[ARGNUM_1]  = PTR_TO_ARGHOLDER(ptrManagedAssemblyLoadContext);

    // Make the call
    CALL_MANAGED_METHOD(hmod,LPVOID,args);

    GCPROTECT_END();

#ifdef FEATURE_PAL
    if (hmod != nullptr)
    {
        // Register the system library handle with PAL and get a PAL library handle
        hmod = PAL_RegisterLibraryDirect(hmod, wszLibName);
    }
#endif // FEATURE_PAL

    return (HMODULE)hmod;
}

// Try to load the module alongside the assembly where the PInvoke was declared.
HMODULE NDirect::LoadFromPInvokeAssemblyDirectory(Assembly *pAssembly, LPCWSTR libName, DWORD flags, LoadLibErrorTracker *pErrorTracker)
{
    STANDARD_VM_CONTRACT;

    HMODULE hmod = NULL;

    SString path = pAssembly->GetManifestFile()->GetPath();

    SString::Iterator lastPathSeparatorIter = path.End();
    if (PEAssembly::FindLastPathSeparator(path, lastPathSeparatorIter))
    {
        lastPathSeparatorIter++;
        path.Truncate(lastPathSeparatorIter);

        path.Append(libName);
        hmod = LocalLoadLibraryHelper(path, flags, pErrorTracker);
    }

    return hmod;
}

// Try to load the module from the native DLL search directories
HMODULE NDirect::LoadFromNativeDllSearchDirectories(AppDomain* pDomain, LPCWSTR libName, DWORD flags, LoadLibErrorTracker *pErrorTracker)
{
    STANDARD_VM_CONTRACT;

    HMODULE hmod = NULL;

    if (pDomain->HasNativeDllSearchDirectories())
    {
        AppDomain::PathIterator pathIter = pDomain->IterateNativeDllSearchDirectories();
        while (hmod == NULL && pathIter.Next())
        {
            SString qualifiedPath(*(pathIter.GetPath()));
            qualifiedPath.Append(libName);
            if (!Path::IsRelative(qualifiedPath))
            {
                hmod = LocalLoadLibraryHelper(qualifiedPath, flags, pErrorTracker);
            }
        }
    }

    return hmod;
}

HINSTANCE NDirect::LoadLibraryModule(NDirectMethodDesc * pMD, LoadLibErrorTracker * pErrorTracker)
{
    CONTRACTL
    {
        STANDARD_VM_CHECK;
        PRECONDITION( CheckPointer( pMD ) );
    }
    CONTRACTL_END;
    
    LPCUTF8 name = pMD->GetLibName();
    if ( !name || !*name )
        return NULL;
    
    ModuleHandleHolder hmod;

    DWORD loadWithAlteredPathFlags = GetLoadWithAlteredSearchPathFlag();

    PREFIX_ASSUME( name != NULL );
    MAKE_WIDEPTR_FROMUTF8( wszLibName, name );

    AppDomain* pDomain = GetAppDomain();

    // AssemblyLoadContext is not supported in AppX mode and thus,
    // we should not perform PInvoke resolution via it when operating in
    // AppX mode.
    if (!AppX::IsAppXProcess())
    {
        hmod = LoadLibraryModuleViaHost(pMD, pDomain, wszLibName);
    }
    
    
    if(hmod == NULL)
    {
       hmod = pDomain->FindUnmanagedImageInCache(wszLibName);
    }

    if(hmod != NULL)
    {
       return hmod.Extract();
    }

#ifdef FEATURE_PAL
    // In the PAL version of CoreCLR, the CLR module itself exports the functionality
    // that the Windows version obtains from kernel32 and friends.  In order to avoid
    // picking up the wrong instance, we perform this redirection first.
    // This is also true for CoreSystem builds, where mscorlib p/invokes are forwarded through coreclr
    // itself so we can control CoreSystem library/API name re-mapping from one central location.
    if (SString::_wcsicmp(wszLibName, MAIN_CLR_MODULE_NAME_W) == 0)
        hmod = GetCLRModule();
#endif // FEATURE_PAL

#if defined(FEATURE_CORESYSTEM) && !defined(PLATFORM_UNIX)
    if (hmod == NULL)
    {
        // Try to go straight to System32 for Windows API sets. This is replicating quick check from
        // the OS implementation of api sets.
        if (SString::_wcsnicmp(wszLibName, W("api-"), 4) == 0 || SString::_wcsnicmp(wszLibName, W("ext-"), 4) == 0)
        {
            hmod = LocalLoadLibraryHelper(wszLibName, LOAD_LIBRARY_SEARCH_SYSTEM32, pErrorTracker);
        }
    }
#endif // FEATURE_CORESYSTEM && !FEATURE_PAL

    if (hmod == NULL)
    {
        // NATIVE_DLL_SEARCH_DIRECTORIES set by host is considered well known path 
        hmod = LoadFromNativeDllSearchDirectories(pDomain, wszLibName, loadWithAlteredPathFlags, pErrorTracker);
    }

    DWORD dllImportSearchPathFlag = 0;
    BOOL searchAssemblyDirectory = TRUE;
    bool libNameIsRelativePath = Path::IsRelative(wszLibName);
    if (hmod == NULL)
    {
        // First checks if the method has DefaultDllImportSearchPathsAttribute. If method has the attribute
        // then dllImportSearchPathFlag is set to its value.
        // Otherwise checks if the assembly has the attribute. 
        // If assembly has the attribute then flag ise set to its value.
        BOOL attributeIsFound = FALSE;

        if (pMD->HasDefaultDllImportSearchPathsAttribute())
        {
            dllImportSearchPathFlag = pMD->DefaultDllImportSearchPathsAttributeCachedValue();
            searchAssemblyDirectory = pMD->DllImportSearchAssemblyDirectory();
            attributeIsFound = TRUE;
        }
        else 
        {
            Module * pModule = pMD->GetModule();

            if(pModule->HasDefaultDllImportSearchPathsAttribute())
            {
                dllImportSearchPathFlag = pModule->DefaultDllImportSearchPathsAttributeCachedValue();
                searchAssemblyDirectory = pModule->DllImportSearchAssemblyDirectory();
                attributeIsFound = TRUE;
            }
        }


        if (!libNameIsRelativePath)
        {
            DWORD flags = loadWithAlteredPathFlags;
            if ((dllImportSearchPathFlag & LOAD_LIBRARY_SEARCH_DLL_LOAD_DIR) != 0)
            {
                // LOAD_LIBRARY_SEARCH_DLL_LOAD_DIR is the only flag affecting absolute path. Don't OR the flags
                // unconditionally as all absolute path P/Invokes could then lose LOAD_WITH_ALTERED_SEARCH_PATH.
                flags |= dllImportSearchPathFlag;
            }

            hmod = LocalLoadLibraryHelper(wszLibName, flags, pErrorTracker);
        }
        else if (searchAssemblyDirectory)
        {
            Assembly* pAssembly = pMD->GetMethodTable()->GetAssembly();
            hmod = LoadFromPInvokeAssemblyDirectory(pAssembly, wszLibName, loadWithAlteredPathFlags | dllImportSearchPathFlag, pErrorTracker);

        }
    }

    // This call searches the application directory instead of the location for the library.
    if (hmod == NULL)
    {
        hmod = LocalLoadLibraryHelper(wszLibName, dllImportSearchPathFlag, pErrorTracker);
    }

    // This may be an assembly name
    if (!hmod)
    {
        // Format is "fileName, assemblyDisplayName"
        MAKE_UTF8PTR_FROMWIDE(szLibName, wszLibName);
        char *szComma = strchr(szLibName, ',');
        if (szComma)
        {
            *szComma = '\0';
            while (COMCharacter::nativeIsWhiteSpace(*(++szComma)));

            AssemblySpec spec;
            if (SUCCEEDED(spec.Init(szComma)))
            {
                // Need to perform case insensitive hashing.
                CQuickBytes qbLC;
                {
                    UTF8_TO_LOWER_CASE(szLibName, qbLC);
                    szLibName = (LPUTF8) qbLC.Ptr();
                }

                Assembly *pAssembly = spec.LoadAssembly(FILE_LOADED);
                Module *pModule = pAssembly->FindModuleByName(szLibName);

                hmod = LocalLoadLibraryHelper(pModule->GetPath(), loadWithAlteredPathFlags | dllImportSearchPathFlag, pErrorTracker);
            }
        }
    }

#ifdef FEATURE_PAL
    if (hmod == NULL)
    {
        // P/Invokes are often declared with variations on the actual library name.
        // For example, it's common to leave off the extension/suffix of the library
        // even if it has one, or to leave off a prefix like "lib" even if it has one
        // (both of these are typically done to smooth over cross-platform differences). 
        // We try to dlopen with such variations on the original.
        const char* const prefixSuffixCombinations[] =
        {
            "%s%s%s",     // prefix+name+suffix
            "%.0s%s%s",   // name+suffix
            "%s%s%.0s",   // prefix+name
        };

        const int NUMBER_OF_LIB_NAME_VARIATIONS = COUNTOF(prefixSuffixCombinations);

        // Try to load from places we tried above, but this time with variations on the
        // name including the prefix, suffix, and both.
        for (int i = 0; i < NUMBER_OF_LIB_NAME_VARIATIONS; i++)
        {
            SString currLibNameVariation;
            currLibNameVariation.Printf(prefixSuffixCombinations[i], PAL_SHLIB_PREFIX, name, PAL_SHLIB_SUFFIX);

            if (libNameIsRelativePath && searchAssemblyDirectory)
            {
                Assembly *pAssembly = pMD->GetMethodTable()->GetAssembly();
                hmod = LoadFromPInvokeAssemblyDirectory(pAssembly, currLibNameVariation, loadWithAlteredPathFlags | dllImportSearchPathFlag, pErrorTracker);
                if (hmod != NULL)
                    break;
            }

            hmod = LoadFromNativeDllSearchDirectories(pDomain, currLibNameVariation, loadWithAlteredPathFlags, pErrorTracker);
            if (hmod != NULL)
                break;

            hmod = LocalLoadLibraryHelper(currLibNameVariation, dllImportSearchPathFlag, pErrorTracker);
            if (hmod != NULL)
                break;
        }
    }
#endif // FEATURE_PAL

    // After all this, if we have a handle add it to the cache.
    if (hmod)
    {
        pDomain->AddUnmanagedImageToCache(wszLibName, hmod);
    }

    return hmod.Extract();
}

//---------------------------------------------------------
// Loads the DLL and finds the procaddress for an N/Direct call.
//---------------------------------------------------------
/* static */
VOID NDirect::NDirectLink(NDirectMethodDesc *pMD)
{
    CONTRACTL
    {
        STANDARD_VM_CHECK;
        PRECONDITION(CheckPointer(pMD));
    }
    CONTRACTL_END;

    //
    // On the phone, we only allow platform assemblies to define pinvokes
    // unless the host has asked us otherwise.
    //

    if (pMD->IsClassConstructorTriggeredAtLinkTime())
    {
        pMD->GetMethodTable()->CheckRunClassInitThrowing();
    }

    if (pMD->IsQCall())
    {
        LPVOID pvTarget = pMD->ndirect.m_pNativeNDirectTarget;

        // Do not repeat the lookup if the QCall was hardbound during ngen
        if (pvTarget == NULL)
        {
            pvTarget = ECall::GetQCallImpl(pMD);
        }
        else
        {
            _ASSERTE(pvTarget == ECall::GetQCallImpl(pMD));
        }

        pMD->SetNDirectTarget(pvTarget);
        return;
    }

    // Loading unmanaged dlls can trigger dllmains which certainly count as code execution!
    pMD->EnsureActive();

    LoadLibErrorTracker errorTracker;

    BOOL fSuccess = FALSE;
    HINSTANCE hmod = LoadLibraryModule( pMD, &errorTracker );
    if ( hmod )
    {
        LPVOID pvTarget = NDirectGetEntryPoint(pMD, hmod);
        if (pvTarget)
        {

#ifdef MDA_SUPPORTED
            MdaInvalidOverlappedToPinvoke *pOverlapCheck = MDA_GET_ASSISTANT(InvalidOverlappedToPinvoke);
            if (pOverlapCheck && pOverlapCheck->ShouldHook(pMD))
            {
                LPVOID pNewTarget = pOverlapCheck->Register(hmod,pvTarget);
                if (pNewTarget)
                {
                    pvTarget = pNewTarget;
                }
            }
#endif
            pMD->SetNDirectTarget(pvTarget);
            fSuccess = TRUE;
        }
    }

    if (!fSuccess)
    {
        if (pMD->GetLibName() == NULL)
            COMPlusThrow(kEntryPointNotFoundException, IDS_EE_NDIRECT_GETPROCADDRESS_NONAME);
        
        StackSString ssLibName(SString::Utf8, pMD->GetLibName());

        if (!hmod)
        {
            errorTracker.Throw(ssLibName);
        }

        WCHAR wszEPName[50];
        if(WszMultiByteToWideChar(CP_UTF8, 0, (LPCSTR)pMD->GetEntrypointName(), -1, wszEPName, sizeof(wszEPName)/sizeof(WCHAR)) == 0)
        {
            wszEPName[0] = W('?');
            wszEPName[1] = W('\0');
        }

        COMPlusThrow(kEntryPointNotFoundException, IDS_EE_NDIRECT_GETPROCADDRESS, ssLibName.GetUnicode(), wszEPName);
    }
}


//---------------------------------------------------------
// One-time init
//---------------------------------------------------------
/*static*/ void NDirect::Init()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        INJECT_FAULT(COMPlusThrowOM());
    }
    CONTRACTL_END;

#if !defined(FEATURE_PAL)
    // Check if the OS supports the new secure LoadLibraryEx flags introduced in KB2533623
    HMODULE hMod = CLRGetModuleHandle(WINDOWS_KERNEL32_DLLNAME_W);
    _ASSERTE(hMod != NULL);

    if (GetProcAddress(hMod, "AddDllDirectory") != NULL)
    {
        // The AddDllDirectory export was added in KB2533623 together with the new flag support
        s_fSecureLoadLibrarySupported = true;
    }
#endif // !FEATURE_PAL
}


//==========================================================================
// This function is reached only via NDirectImportThunk. It's purpose
// is to ensure that the target DLL is fully loaded and ready to run.
//
// FUN FACTS: Though this function is actually entered in unmanaged mode,
// it can reenter managed mode and throw a COM+ exception if the DLL linking
// fails.
//==========================================================================


EXTERN_C LPVOID STDCALL NDirectImportWorker(NDirectMethodDesc* pMD)
{
    LPVOID ret = NULL;

    BEGIN_PRESERVE_LAST_ERROR;

    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        SO_TOLERANT;
    }
    CONTRACTL_END;

    INSTALL_MANAGED_EXCEPTION_DISPATCHER;
    // this function is called by CLR to native assembly stubs which are called by 
    // managed code as a result, we need an unwind and continue handler to translate 
    // any of our internal exceptions into managed exceptions.
    INSTALL_UNWIND_AND_CONTINUE_HANDLER;

    {
        //
        // Otherwise we're in an inlined pinvoke late bound MD
        //
        INDEBUG(Thread *pThread = GetThread());
        {
            _ASSERTE(pThread->GetFrame()->GetVTablePtr() == InlinedCallFrame::GetMethodFrameVPtr());

            CONSISTENCY_CHECK(pMD->IsNDirect());
            //
            // With IL stubs, we don't have to do anything but ensure the DLL is loaded.
            //

            if (!pMD->IsZapped())
            {
                PInvokeStaticSigInfo sigInfo;
                NDirect::PopulateNDirectMethodDesc(pMD, &sigInfo);
            }
            else
            {
                // must have been populated at NGEN time
                _ASSERTE(pMD->GetLibName() != NULL);
            }

            pMD->CheckRestore();

            NDirect::NDirectLink(pMD);
        }
    }

    ret = pMD->GetNDirectTarget();

    UNINSTALL_UNWIND_AND_CONTINUE_HANDLER;
    UNINSTALL_MANAGED_EXCEPTION_DISPATCHER;

    END_PRESERVE_LAST_ERROR;

    return ret;
}

//===========================================================================
//  Support for Pinvoke Calli instruction
//
//===========================================================================

EXTERN_C void STDCALL VarargPInvokeStubWorker(TransitionBlock * pTransitionBlock, VASigCookie *pVASigCookie, MethodDesc *pMD)
{
    BEGIN_PRESERVE_LAST_ERROR;

    STATIC_CONTRACT_THROWS;
    STATIC_CONTRACT_GC_TRIGGERS;
    STATIC_CONTRACT_MODE_COOPERATIVE;
    STATIC_CONTRACT_ENTRY_POINT;

    MAKE_CURRENT_THREAD_AVAILABLE();

#ifdef _DEBUG
    Thread::ObjectRefFlush(CURRENT_THREAD);
#endif

    FrameWithCookie<PrestubMethodFrame> frame(pTransitionBlock, pMD);
    PrestubMethodFrame * pFrame = &frame;

    pFrame->Push(CURRENT_THREAD);

    _ASSERTE(pVASigCookie == pFrame->GetVASigCookie());
    _ASSERTE(pMD == pFrame->GetFunction());

    GetILStubForCalli(pVASigCookie, pMD);

    pFrame->Pop(CURRENT_THREAD);

    END_PRESERVE_LAST_ERROR;
}

EXTERN_C void STDCALL GenericPInvokeCalliStubWorker(TransitionBlock * pTransitionBlock, VASigCookie * pVASigCookie, PCODE pUnmanagedTarget)
{
    BEGIN_PRESERVE_LAST_ERROR;

    STATIC_CONTRACT_THROWS;
    STATIC_CONTRACT_GC_TRIGGERS;
    STATIC_CONTRACT_MODE_COOPERATIVE;
    STATIC_CONTRACT_ENTRY_POINT;

    MAKE_CURRENT_THREAD_AVAILABLE();

#ifdef _DEBUG
    Thread::ObjectRefFlush(CURRENT_THREAD);
#endif

    FrameWithCookie<PInvokeCalliFrame> frame(pTransitionBlock, pVASigCookie, pUnmanagedTarget);
    PInvokeCalliFrame * pFrame = &frame;

    pFrame->Push(CURRENT_THREAD);

    _ASSERTE(pVASigCookie == pFrame->GetVASigCookie());

    GetILStubForCalli(pVASigCookie, NULL);

    pFrame->Pop(CURRENT_THREAD);

    END_PRESERVE_LAST_ERROR;
}

PCODE GetILStubForCalli(VASigCookie *pVASigCookie, MethodDesc *pMD)
{
    CONTRACT(PCODE)
    {
        THROWS;
        GC_TRIGGERS;
        ENTRY_POINT;
        MODE_ANY;
        PRECONDITION(CheckPointer(pVASigCookie));
        PRECONDITION(CheckPointer(pMD, NULL_OK));
        POSTCONDITION(RETVAL != NULL);
    }
    CONTRACT_END;

    PCODE pTempILStub = NULL;

    INSTALL_MANAGED_EXCEPTION_DISPATCHER;
    // this function is called by CLR to native assembly stubs which are called by 
    // managed code as a result, we need an unwind and continue handler to translate 
    // any of our internal exceptions into managed exceptions.
    INSTALL_UNWIND_AND_CONTINUE_HANDLER;

    // Force a GC if the stress level is high enough
    GCStress<cfg_any>::MaybeTrigger();

    GCX_PREEMP();

    Signature signature = pVASigCookie->signature;
    CorPinvokeMap unmgdCallConv = pmNoMangle;
    
    DWORD dwStubFlags = NDIRECTSTUB_FL_BESTFIT;
    
    // The MethodDesc pointer may in fact be the unmanaged target, see PInvokeStubs.asm.
    if (pMD == NULL || (UINT_PTR)pMD & 0x1)
    {
        pMD = NULL;
        dwStubFlags |= NDIRECTSTUB_FL_UNMANAGED_CALLI;

        // need to convert the CALLI signature to stub signature with managed calling convention
        switch (MetaSig::GetCallingConvention(pVASigCookie->pModule, pVASigCookie->signature))
        {
            case IMAGE_CEE_CS_CALLCONV_C:
                    unmgdCallConv = pmCallConvCdecl;
                    break;
            case IMAGE_CEE_CS_CALLCONV_STDCALL:
                    unmgdCallConv = pmCallConvStdcall;
                    break;
            case IMAGE_CEE_CS_CALLCONV_THISCALL:
                    unmgdCallConv = pmCallConvThiscall;
                    break;
            case IMAGE_CEE_CS_CALLCONV_FASTCALL:
                    unmgdCallConv = pmCallConvFastcall;
                    break;
            default:
                    COMPlusThrow(kTypeLoadException, IDS_INVALID_PINVOKE_CALLCONV);
        }

        LoaderHeap *pHeap = pVASigCookie->pModule->GetLoaderAllocator()->GetHighFrequencyHeap();
        PCOR_SIGNATURE new_sig = (PCOR_SIGNATURE)(void *)pHeap->AllocMem(S_SIZE_T(signature.GetRawSigLen()));
        CopyMemory(new_sig, signature.GetRawSig(), signature.GetRawSigLen());
        
        // make the stub IMAGE_CEE_CS_CALLCONV_DEFAULT
        *new_sig &= ~IMAGE_CEE_CS_CALLCONV_MASK;
        *new_sig |= IMAGE_CEE_CS_CALLCONV_DEFAULT;

        signature = Signature(new_sig, signature.GetRawSigLen());
    }
    else
    {
        _ASSERTE(pMD->IsNDirect());
        dwStubFlags |= NDIRECTSTUB_FL_CONVSIGASVARARG;

        // vararg P/Invoke must be cdecl
        unmgdCallConv = pmCallConvCdecl;

        if (((NDirectMethodDesc *)pMD)->IsClassConstructorTriggeredByILStub())
        {
            dwStubFlags |= NDIRECTSTUB_FL_TRIGGERCCTOR;
        }
    }

    mdMethodDef md;
    CorNativeLinkFlags nlFlags;
    CorNativeLinkType  nlType;

    if (pMD != NULL)
    {
        PInvokeStaticSigInfo sigInfo(pMD);

        md = pMD->GetMemberDef();
        nlFlags = sigInfo.GetLinkFlags();
        nlType  = sigInfo.GetCharSet();
    }
    else
    {
        md = mdMethodDefNil;
        nlFlags = nlfNone;
        nlType  = nltAnsi;
    }

    StubSigDesc sigDesc(pMD, signature, pVASigCookie->pModule);

    MethodDesc* pStubMD = NDirect::CreateCLRToNativeILStub(&sigDesc,
                                    nlType, 
                                    nlFlags, 
                                    unmgdCallConv,
                                    dwStubFlags);

    pTempILStub = JitILStub(pStubMD);

    InterlockedCompareExchangeT<PCODE>(&pVASigCookie->pNDirectILStub,
                                                    pTempILStub,
                                                    NULL);

    UNINSTALL_UNWIND_AND_CONTINUE_HANDLER;
    UNINSTALL_MANAGED_EXCEPTION_DISPATCHER;

    RETURN pVASigCookie->pNDirectILStub;
}

#endif // CROSSGEN_COMPILE

#endif // #ifndef DACCESS_COMPILE

//
// Truncates a SString by first converting it to unicode and truncate it 
// if it is larger than size. "..." will be appened if it is truncated.
//
void TruncateUnicodeString(SString &string, COUNT_T bufSize)
{
    string.Normalize();
    if ((string.GetCount() + 1) * sizeof(WCHAR) > bufSize)
    {
        _ASSERTE(bufSize / sizeof(WCHAR) > 4);
        string.Truncate(string.Begin() + bufSize / sizeof(WCHAR) - 4);
        string.Append(W("..."));
    }
}
