// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
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
#include "comdelegate.h"
#include "ceeload.h"
#include "mlinfo.h"
#include "eeconfig.h"
#include "comutilnative.h"
#include "corhost.h"
#include "asmconstants.h"
#include "customattribute.h"
#include "ilstubcache.h"
#include "typeparse.h"
#include "typestring.h"
#include "sigbuilder.h"
#include "sigformat.h"
#include "strongnameholders.h"
#include "ecall.h"
#include "fieldmarshaler.h"
#include "pinvokeoverride.h"

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

// Specifies whether coreclr is embedded or standalone
extern bool g_coreclr_embedded;

// Specifies whether hostpolicy is embedded in executable or standalone
extern bool g_hostpolicy_embedded;

// remove when we get an updated SDK
#define LOAD_LIBRARY_SEARCH_DLL_LOAD_DIR 0x00000100
#define LOAD_LIBRARY_SEARCH_DEFAULT_DIRS 0x00001000

#ifndef DACCESS_COMPILE
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
#endif

StubSigDesc::StubSigDesc(MethodDesc *pMD, PInvokeStaticSigInfo* pSigInfo /*= NULL*/)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        SUPPORTS_DAC;
    }
    CONTRACTL_END;

    m_pMD = pMD;
    m_pMT = nullptr;
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

StubSigDesc::StubSigDesc(MethodDesc* pMD, Signature sig, Module* pModule)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        SUPPORTS_DAC;
        PRECONDITION(!sig.IsEmpty());
        PRECONDITION(pModule != NULL);
    }
    CONTRACTL_END;

    m_pMD = pMD;
    m_pMT = nullptr;
    m_sig = sig;
    m_pModule = pModule;

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

StubSigDesc::StubSigDesc(MethodTable* pMT, Signature sig, Module* pModule)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        SUPPORTS_DAC;
        PRECONDITION(!sig.IsEmpty());
        PRECONDITION(pModule != NULL);
    }
    CONTRACTL_END;

    m_pMD = nullptr;
    m_pMT = pMT;
    m_sig = sig;
    m_pModule = pModule;

    m_tkMethodDef = mdMethodDefNil;

    if (pMT != NULL)
    {
        SigTypeContext::InitTypeContext(pMT, &m_typeContext);
        m_pLoaderModule = pMT->GetLoaderModule();   // Used for ILStubCache selection and MethodTable creation.
    }
    else
    {
        m_pLoaderModule = m_pModule;
    }

    INDEBUG(InitDebugNames());
}

StubSigDesc::StubSigDesc(std::nullptr_t, Signature sig, Module* pModule)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        SUPPORTS_DAC;
        PRECONDITION(!sig.IsEmpty());
        PRECONDITION(pModule != NULL);
    }
    CONTRACTL_END;

    m_pMD = nullptr;
    m_pMT = nullptr;
    m_sig = sig;
    m_pModule = pModule;
    m_tkMethodDef = mdMethodDefNil;
    m_pLoaderModule = m_pModule;

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
    virtual void MarshalField(MarshalInfo* pInfo, UINT32 managedOffset, UINT32 nativeOffset, FieldDesc* pFieldDesc) = 0;

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
                DWORD dwStubFlags,
                int iLCIDParamIdx,
                MethodDesc* pTargetMD)
            : m_slIL(dwStubFlags, pStubModule, signature, pTypeContext, pTargetMD, iLCIDParamIdx)
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

    void MarshalField(MarshalInfo* pInfo, UINT32 managedOffset, UINT32 nativeOffset, FieldDesc* pFieldDesc)
    {
        CONTRACTL
        {
            STANDARD_VM_CHECK;
            PRECONDITION(CheckPointer(pInfo));
        }
        CONTRACTL_END;

        pInfo->GenerateFieldIL(&m_slIL, managedOffset, nativeOffset, pFieldDesc);
    }

#ifdef FEATURE_COMINTEROP
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

        if (SF_IsReverseStub(m_dwStubFlags))
        {
            if ((m_slIL.GetStubTargetCallingConv() & IMAGE_CEE_CS_CALLCONV_HASTHIS) == IMAGE_CEE_CS_CALLCONV_HASTHIS)
            {
                // the arg number will be incremented by LDARG if we are in an instance method
                _ASSERTE(argIdx > 0);
                argIdx--;
            }

            // call CultureInfo.get_CurrentCulture()
            pcs->EmitCALL(METHOD__CULTURE_INFO__GET_CURRENT_CULTURE, 0, 1);

            // save the current culture
            LocalDesc locDescCulture(CoreLibBinder::GetClass(CLASS__CULTURE_INFO));
            DWORD dwCultureLocalNum = pcs->NewLocal(locDescCulture);

            pcs->EmitSTLOC(dwCultureLocalNum);

            // set a new one based on the LCID passed from unmanaged
            pcs->EmitLDARG(argIdx);

            // call CultureInfo..ctor(lcid)
            // call CultureInfo.set_CurrentCulture(culture)
            pcs->EmitNEWOBJ(METHOD__CULTURE_INFO__INT_CTOR, 1);
            pcs->EmitCALL(METHOD__CULTURE_INFO__SET_CURRENT_CULTURE, 1, 0);

            // and restore the current one after the call
            m_slIL.SetCleanupNeeded();
            ILCodeStream *pcsCleanup = m_slIL.GetCleanupCodeStream();

            // call CultureInfo.set_CurrentCulture(original_culture)
            pcsCleanup->EmitLDLOC(dwCultureLocalNum);
            pcsCleanup->EmitCALL(METHOD__CULTURE_INFO__SET_CURRENT_CULTURE, 1, 0);
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
                // call CultureInfo.get_CurrentCulture()
                pcs->EmitCALL(METHOD__CULTURE_INFO__GET_CURRENT_CULTURE, 0, 1);

                //call CultureInfo.get_LCID(this)
                pcs->EmitCALL(METHOD__CULTURE_INFO__GET_ID, 1, 1);
            }
        }

        // add the extra arg to the unmanaged signature
        LocalDesc locDescNative(ELEMENT_TYPE_I4);
        pcs->SetStubTargetArgType(&locDescNative, false);
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

#ifndef TARGET_X86
            // we store the real managed argument stack size in the stub MethodDesc on non-X86
            UINT stackSize = pStubMD->SizeOfNativeArgStack();

            if (!FitsInU2(stackSize))
                COMPlusThrow(kMarshalDirectiveException, IDS_EE_SIGTOOCOMPLEX);

            pStubMD->AsDynamicMethodDesc()->SetNativeStackArgSize(static_cast<WORD>(stackSize));
#endif // TARGET_X86
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

    virtual void EmitExceptionHandler(LocalDesc* pNativeReturnType, LocalDesc* pManagedReturnType,
        ILCodeLabel** ppTryBeginLabel, ILCodeLabel** ppTryEndAndCatchBeginLabel, ILCodeLabel** ppCatchEndAndReturnLabel)
    {
#ifdef FEATURE_COMINTEROP
        STANDARD_VM_CONTRACT;

        ILCodeStream* pcsExceptionHandler = m_slIL.NewCodeStream(ILStubLinker::kExceptionHandler);
        *ppTryEndAndCatchBeginLabel = pcsExceptionHandler->NewCodeLabel(); // try ends at the same place the catch begins
        *ppCatchEndAndReturnLabel = pcsExceptionHandler->NewCodeLabel();   // catch ends at the same place we resume afterwards

        pcsExceptionHandler->EmitLEAVE(*ppCatchEndAndReturnLabel);
        pcsExceptionHandler->EmitLabel(*ppTryEndAndCatchBeginLabel);

        BYTE nativeReturnElemType = pNativeReturnType->ElementType[0];      // return type of the stub
        BYTE managedReturnElemType = pManagedReturnType->ElementType[0];    // return type of the mananged target

        bool returnTheHRESULT = SF_IsHRESULTSwapping(m_dwStubFlags) ||
                                    (managedReturnElemType == ELEMENT_TYPE_I4) ||
                                    (managedReturnElemType == ELEMENT_TYPE_U4);

        DWORD retvalLocalNum = m_slIL.GetReturnValueLocalNum();
        BinderMethodID getHRForException;
        getHRForException = METHOD__MARSHAL__GET_HR_FOR_EXCEPTION;

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
#endif // FEATURE_COMINTEROP
    }

#ifndef DACCESS_COMPILE
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

        bool hasTryCatchExceptionHandler = hasTryCatchForHRESULT || SF_IsStructMarshalStub(m_dwStubFlags);

#ifdef FEATURE_COMINTEROP
        if (hasTryCatchForHRESULT)
        {
            m_slIL.GetStubTargetReturnType(&nativeReturnType);
            m_slIL.GetStubReturnType(&managedReturnType);
        }
#endif // FEATURE_COMINTEROP

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
                CoreLibBinder::GetModule(),
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

        // <NOTE>
        // The profiler helpers below must be called immediately before and after the call to the target.
        // The debugger trace call helpers are invoked from StubRareDisableWorker
        // </NOTE>

#if defined(PROFILING_SUPPORTED)
        DWORD dwMethodDescLocalNum = (DWORD)-1;

        // Notify the profiler of call out of the runtime
        if (!SF_IsReverseCOMStub(m_dwStubFlags) && !SF_IsStructMarshalStub(m_dwStubFlags) && (CORProfilerTrackTransitions() || (!IsReadyToRunCompilation() && SF_IsNGENedStubForProfiling(m_dwStubFlags))))
        {
            dwMethodDescLocalNum = m_slIL.EmitProfilerBeginTransitionCallback(pcsDispatch, m_dwStubFlags);
            _ASSERTE(dwMethodDescLocalNum != (DWORD)-1);
        }
#endif // PROFILING_SUPPORTED

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

#if defined(TARGET_X86)
        if (SF_IsForwardDelegateStub(m_dwStubFlags))
        {
            // the delegate may have an intercept stub attached to its sync block so we should
            // prevent it from being garbage collected when the call is in progress
            pcsDispatch->EmitLoadThis();
            pcsDispatch->EmitCALL(METHOD__GC__KEEP_ALIVE, 1, 0);
        }
#endif // defined(TARGET_X86)

#ifdef VERIFY_HEAP
        if (SF_IsForwardStub(m_dwStubFlags) && g_pConfig->InteropValidatePinnedObjects())
        {
            // call StubHelpers.ValidateObject/StubHelpers.ValidateByref on pinned locals
            m_slIL.EmitObjectValidation(pcsDispatch, m_dwStubFlags);
        }
#endif // VERIFY_HEAP

#if defined(PROFILING_SUPPORTED)
        // Notify the profiler of return back into the runtime
        if (dwMethodDescLocalNum != (DWORD)-1)
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

                    pcsDispatch->EmitCALL(METHOD__STUBHELPERS__GET_COM_HR_EXCEPTION_OBJECT, 3, 1);
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
        else if (SF_IsStructMarshalStub(m_dwStubFlags))
        {
            // Struct marshal stubs don't actually call anything so they do not need the secrect parameter.
        }
#ifndef HOST_64BIT
        else if (SF_IsForwardDelegateStub(m_dwStubFlags))
        {
            // Forward delegate stubs get all the context they need in 'this' so they
            // don't use the secret parameter. Except for AMD64 where we use the secret
            // argument to pass the real target to the stub-for-host.
        }
#endif // !HOST_64BIT
        else
        {
            // All other IL stubs will need to use the secret parameter.
            jitFlags.Set(CORJIT_FLAGS::CORJIT_FLAG_PUBLISH_SECRET_PARAM);
        }

        if (SF_IsReverseStub(m_dwStubFlags))
        {
            SwapStubSignatures(pStubMD);
        }

        ILCodeLabel* pTryBeginLabel = nullptr;
        ILCodeLabel* pTryEndAndCatchBeginLabel = nullptr;
        ILCodeLabel* pCatchEndLabel = nullptr;
        if (hasTryCatchExceptionHandler)
        {
            EmitExceptionHandler(&nativeReturnType, &managedReturnType, &pTryBeginLabel, &pTryEndAndCatchBeginLabel, &pCatchEndLabel);
        }

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

        ILStubEHClause cleanupTryFinally{};
        m_slIL.GetCleanupFinallyOffsets(&cleanupTryFinally);

        ILStubEHClause tryCatchClause{};
        if (hasTryCatchExceptionHandler)
        {
            tryCatchClause.kind = ILStubEHClause::kTypedCatch;
            tryCatchClause.dwTryBeginOffset = pTryBeginLabel != nullptr ? (DWORD)pTryBeginLabel->GetCodeOffset() : 0;
            tryCatchClause.dwHandlerBeginOffset = ((DWORD)pTryEndAndCatchBeginLabel->GetCodeOffset());
            tryCatchClause.cbTryLength = tryCatchClause.dwHandlerBeginOffset - tryCatchClause.dwTryBeginOffset;
            tryCatchClause.cbHandlerLength = ((DWORD)pCatchEndLabel->GetCodeOffset()) - tryCatchClause.dwHandlerBeginOffset;
            tryCatchClause.dwTypeToken = pcsMarshal->GetToken(g_pObjectClass);
        }

        int nEHClauses = 0;

        if (tryCatchClause.cbHandlerLength != 0)
            nEHClauses++;

        if (cleanupTryFinally.cbHandlerLength != 0)
            nEHClauses++;

        if (nEHClauses > 0)
        {
            COR_ILMETHOD_SECT_EH* pEHSect = pResolver->AllocEHSect(nEHClauses);
            PopulateEHSect(pEHSect, nEHClauses, &cleanupTryFinally, &tryCatchClause);
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

            IMDInternalImport* pIMDI = CoreLibBinder::GetModule()->GetMDImport();

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
            if (tryCatchClause.cbHandlerLength != 0)
            {
                LOG((LF_STUBS, LL_INFO1000, "try_begin: 0x%04x try_end: 0x%04x catch_begin: 0x%04x catch_end: 0x%04x type_token: 0x%08x\n",
                    tryCatchClause.dwTryBeginOffset, tryCatchClause.dwTryBeginOffset + tryCatchClause.cbTryLength,
                    tryCatchClause.dwHandlerBeginOffset, tryCatchClause.dwHandlerBeginOffset + tryCatchClause.cbHandlerLength,
                    tryCatchClause.dwTypeToken));
            }

            LogILStubFlags(LF_STUBS, LL_INFO1000, m_dwStubFlags);

            m_slIL.LogILStub(jitFlags);
        }
        LOG((LF_STUBS, LL_INFO1000, "^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^\n"));
#endif // LOGGING

        //
        // Publish ETW events for IL stubs
        //

        // If the category and the event is enabled...
        if (ETW_EVENT_ENABLED(MICROSOFT_WINDOWS_DOTNETRUNTIME_PROVIDER_DOTNET_Context, ILStubGenerated))
        {
            EtwOnILStubGenerated(
                pStubMD,
                pbLocalSig,
                cbSig,
                jitFlags,
                &tryCatchClause,
                &cleanupTryFinally,
                maxStack,
                (DWORD)cbCode
                );
        }

    }

    //---------------------------------------------------------------------------------------
    //
    void
    EtwOnILStubGenerated(
        MethodDesc *    pStubMD,
        PCCOR_SIGNATURE pbLocalSig,
        DWORD           cbSig,
        CORJIT_FLAGS    jitFlags,
        ILStubEHClause * pConvertToHRTryCatchBounds,
        ILStubEHClause * pCleanupTryFinallyBounds,
        DWORD           maxStack,
        DWORD           cbCode)
    {
        STANDARD_VM_CONTRACT;

        //
        // Interop Method Information
        //
        MethodDesc *pTargetMD = m_slIL.GetTargetMD();
        SString strNamespaceOrClassName, strMethodName, strMethodSignature;
        UINT64 uModuleId = 0;

        if (pTargetMD)
        {
            pTargetMD->GetMethodInfoWithNewSig(strNamespaceOrClassName, strMethodName, strMethodSignature);
            uModuleId = (UINT64)pTargetMD->GetModule()->GetAddrModuleID();
        }

        //
        // Stub Method Signature
        //
        SString stubNamespaceOrClassName, stubMethodName, stubMethodSignature;
        pStubMD->GetMethodInfoWithNewSig(stubNamespaceOrClassName, stubMethodName, stubMethodSignature);

        IMDInternalImport *pStubImport = pStubMD->GetModule()->GetMDImport();

        CQuickBytes qbLocal;
        PrettyPrintSig(pbLocalSig, (DWORD)cbSig, NULL, &qbLocal,  pStubImport, NULL);

        SString strLocalSig(SString::Utf8, (LPCUTF8)qbLocal.Ptr());

        //
        // Native Signature
        //
        SString strNativeSignature(SString::Utf8);
        if (m_dwStubFlags & NDIRECTSTUB_FL_REVERSE_INTEROP)
        {
            // Reverse interop. Use StubSignature
            strNativeSignature = stubMethodSignature;
        }
        else
        {
            // Forward interop. Use StubTarget siganture
            PCCOR_SIGNATURE pCallTargetSig = GetStubTargetMethodSig();
            DWORD           cCallTargetSig = GetStubTargetMethodSigLength();

            CQuickBytes qbCallTargetSig;

            PrettyPrintSig(pCallTargetSig, cCallTargetSig, "", &qbCallTargetSig,  pStubImport, NULL);

            strNativeSignature.SetUTF8((LPCUTF8)qbCallTargetSig.Ptr());
        }

        //
        // Dump IL stub code
        //
        SString strILStubCode;
        strILStubCode.Preallocate(4096);    // Preallocate 4K bytes to avoid unnecessary growth

        SString codeSizeFormat;
        codeSizeFormat.LoadResource(CCompRC::Optional, IDS_EE_INTEROP_CODE_SIZE_COMMENT);
        strILStubCode.AppendPrintf(W("// %s\t%d (0x%04x)\n"), codeSizeFormat.GetUnicode(), cbCode, cbCode);
        strILStubCode.AppendPrintf(W(".maxstack %d \n"), maxStack);
        strILStubCode.AppendPrintf(W(".locals %s\n"), strLocalSig.GetUnicode());

        m_slIL.LogILStub(jitFlags, &strILStubCode);

        if (pConvertToHRTryCatchBounds->cbTryLength != 0 && pConvertToHRTryCatchBounds->cbHandlerLength != 0)
        {
            strILStubCode.AppendPrintf(
                W(".try IL_%04x to IL_%04x catch handler IL_%04x to IL_%04x\n"),
                pConvertToHRTryCatchBounds->dwTryBeginOffset,
                pConvertToHRTryCatchBounds->dwTryBeginOffset + pConvertToHRTryCatchBounds->cbTryLength,
                pConvertToHRTryCatchBounds->dwHandlerBeginOffset,
                pConvertToHRTryCatchBounds->dwHandlerBeginOffset + pConvertToHRTryCatchBounds->cbHandlerLength);
        }

        if (pCleanupTryFinallyBounds->cbTryLength != 0 && pCleanupTryFinallyBounds->cbHandlerLength != 0)
        {
            strILStubCode.AppendPrintf(
                W(".try IL_%04x to IL_%04x finally handler IL_%04x to IL_%04x\n"),
                pCleanupTryFinallyBounds->dwTryBeginOffset,
                pCleanupTryFinallyBounds->dwTryBeginOffset + pCleanupTryFinallyBounds->cbTryLength,
                pCleanupTryFinallyBounds->dwHandlerBeginOffset,
                pCleanupTryFinallyBounds->dwHandlerBeginOffset + pCleanupTryFinallyBounds->cbHandlerLength);
        }

        //
        // Fire the event
        //
        DWORD dwFlags = 0;
        if (m_dwStubFlags & NDIRECTSTUB_FL_REVERSE_INTEROP)
            dwFlags |= ETW_IL_STUB_FLAGS_REVERSE_INTEROP;
#ifdef FEATURE_COMINTEROP
        if (m_dwStubFlags & NDIRECTSTUB_FL_COM)
            dwFlags |= ETW_IL_STUB_FLAGS_COM_INTEROP;
#endif // FEATURE_COMINTEROP
        if (m_dwStubFlags & NDIRECTSTUB_FL_NGENEDSTUB)
            dwFlags |= ETW_IL_STUB_FLAGS_NGENED_STUB;
        if (m_dwStubFlags & NDIRECTSTUB_FL_DELEGATE)
            dwFlags |= ETW_IL_STUB_FLAGS_DELEGATE;
        if (m_dwStubFlags & NDIRECTSTUB_FL_CONVSIGASVARARG)
            dwFlags |= ETW_IL_STUB_FLAGS_VARARG;
        if (m_dwStubFlags & NDIRECTSTUB_FL_UNMANAGED_CALLI)
            dwFlags |= ETW_IL_STUB_FLAGS_UNMANAGED_CALLI;
        if (m_dwStubFlags & NDIRECTSTUB_FL_STRUCT_MARSHAL)
            dwFlags |= ETW_IL_STUB_FLAGS_STRUCT_MARSHAL;

        DWORD dwToken = 0;
        if (pTargetMD)
            dwToken = pTargetMD->GetMemberDef();


        //
        // Truncate string fields. Make sure the whole event is less than 64KB
        //
        TruncateUnicodeString(strNamespaceOrClassName, ETW_IL_STUB_EVENT_STRING_FIELD_MAXSIZE);
        TruncateUnicodeString(strMethodName,           ETW_IL_STUB_EVENT_STRING_FIELD_MAXSIZE);
        TruncateUnicodeString(strMethodSignature,      ETW_IL_STUB_EVENT_STRING_FIELD_MAXSIZE);
        TruncateUnicodeString(strNativeSignature,      ETW_IL_STUB_EVENT_STRING_FIELD_MAXSIZE);
        TruncateUnicodeString(stubMethodSignature,     ETW_IL_STUB_EVENT_STRING_FIELD_MAXSIZE);
        TruncateUnicodeString(strILStubCode,           ETW_IL_STUB_EVENT_CODE_STRING_FIELD_MAXSIZE);

        //
        // Fire ETW event
        //
        FireEtwILStubGenerated(
            GetClrInstanceId(),                         // ClrInstanceId
            uModuleId,                                  // ModuleIdentifier
            (UINT64)pStubMD,                            // StubMethodIdentifier
            dwFlags,                                    // StubFlags
            dwToken,                                    // ManagedInteropMethodToken
            strNamespaceOrClassName.GetUnicode(),       // ManagedInteropMethodNamespace
            strMethodName.GetUnicode(),                 // ManagedInteropMethodName
            strMethodSignature.GetUnicode(),            // ManagedInteropMethodSignature
            strNativeSignature.GetUnicode(),            // NativeSignature
            stubMethodSignature.GetUnicode(),           // StubMethodSigature
            strILStubCode.GetUnicode()                  // StubMethodILCode
            );
    } // EtwOnILStubGenerated
#endif // DACCESS_COMPILE

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
        LogOneFlag(dwStubFlags, NDIRECTSTUB_FL_STRUCT_MARSHAL,          "   NDIRECTSTUB_FL_STRUCT_MARSHAL\n", facility, level);
#ifdef FEATURE_COMINTEROP
        LogOneFlag(dwStubFlags, NDIRECTSTUB_FL_COM,                     "   NDIRECTSTUB_FL_COM\n", facility, level);
#endif // FEATURE_COMINTEROP
        LogOneFlag(dwStubFlags, NDIRECTSTUB_FL_NGENEDSTUBFORPROFILING,  "   NDIRECTSTUB_FL_NGENEDSTUBFORPROFILING\n", facility, level);
        LogOneFlag(dwStubFlags, NDIRECTSTUB_FL_GENERATEDEBUGGABLEIL,    "   NDIRECTSTUB_FL_GENERATEDEBUGGABLEIL\n", facility, level);
        LogOneFlag(dwStubFlags, NDIRECTSTUB_FL_UNMANAGED_CALLI,         "   NDIRECTSTUB_FL_UNMANAGED_CALLI\n", facility, level);
        LogOneFlag(dwStubFlags, NDIRECTSTUB_FL_TRIGGERCCTOR,            "   NDIRECTSTUB_FL_TRIGGERCCTOR\n", facility, level);
#ifdef FEATURE_COMINTEROP
        LogOneFlag(dwStubFlags, NDIRECTSTUB_FL_FIELDGETTER,             "   NDIRECTSTUB_FL_FIELDGETTER\n", facility, level);
        LogOneFlag(dwStubFlags, NDIRECTSTUB_FL_FIELDSETTER,             "   NDIRECTSTUB_FL_FIELDSETTER\n", facility, level);
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
            NDIRECTSTUB_FL_UNMANAGED_CALLI          |
            NDIRECTSTUB_FL_STRUCT_MARSHAL           |
            NDIRECTSTUB_FL_TRIGGERCCTOR             |
#ifdef FEATURE_COMINTEROP
            NDIRECTSTUB_FL_COM                      |
            NDIRECTSTUB_FL_COMLATEBOUND             |   // internal
            NDIRECTSTUB_FL_COMEVENTCALL             |   // internal
            NDIRECTSTUB_FL_FIELDGETTER              |
            NDIRECTSTUB_FL_FIELDSETTER              |
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

class StructMarshal_ILStubState : public ILStubState
{
public:

    StructMarshal_ILStubState(MethodTable* pMT, const Signature& signature, SigTypeContext* pTypeContext, DWORD dwStubFlags)
        : ILStubState(
            pMT->GetModule(),
            signature,
            pTypeContext,
            dwStubFlags,
            -1 /* We have no LCID parameter */,
            nullptr),
        m_nativeSize(pMT->GetNativeSize())
    {
        LIMITED_METHOD_CONTRACT;

    }

    void BeginEmit(DWORD dwStubFlags)
    {
        STANDARD_VM_CONTRACT;

        ILStubState::BeginEmit(dwStubFlags);

        ILCodeStream* pcsSetup = m_slIL.GetSetupCodeStream();
        ILCodeStream* pcsMarshal = m_slIL.GetMarshalCodeStream();
        ILCodeStream* pcsUnmarshal = m_slIL.GetUnmarshalCodeStream();
        ILCodeStream* pcsCleanup = m_slIL.GetCleanupCodeStream();

        pMarshalStartLabel = pcsSetup->NewCodeLabel();
        pCatchTrampolineStartLabel = pcsSetup->NewCodeLabel();
        pCatchTrampolineEndLabel = pcsSetup->NewCodeLabel();
        pUnmarshalStartLabel = pcsSetup->NewCodeLabel();
        pCleanupStartLabel = pcsSetup->NewCodeLabel();
        pReturnLabel = pcsSetup->NewCodeLabel();

        dwExceptionDispatchInfoLocal = pcsSetup->NewLocal(CoreLibBinder::GetClass(CLASS__EXCEPTION_DISPATCH_INFO));
        pcsSetup->EmitLDNULL();
        pcsSetup->EmitSTLOC(dwExceptionDispatchInfoLocal);

        pcsMarshal->EmitLabel(pMarshalStartLabel);
        pcsUnmarshal->EmitLabel(pUnmarshalStartLabel);
        pcsCleanup->EmitLabel(pCleanupStartLabel);

        // Initialize the native structure's memory so we can do a partial cleanup
        // if marshalling fails.
        pcsMarshal->EmitLDARG(StructMarshalStubs::NATIVE_STRUCT_ARGIDX);
        pcsMarshal->EmitLDC(0);
        pcsMarshal->EmitLDC(m_nativeSize);
        pcsMarshal->EmitINITBLK();
    }

    void FinishEmit(MethodDesc* pStubMD)
    {
        STANDARD_VM_CONTRACT;

        ILCodeStream* pcsSetup = m_slIL.GetSetupCodeStream();
        ILCodeStream* pcsMarshal = m_slIL.GetMarshalCodeStream();
        ILCodeStream* pcsUnmarshal = m_slIL.GetUnmarshalCodeStream();
        ILCodeStream* pcsDispatch = m_slIL.GetDispatchCodeStream();
        ILCodeStream* pcsCleanup = m_slIL.GetCleanupCodeStream();

        pcsSetup->EmitNOP("// marshal operation jump table {");
        pcsSetup->EmitLDARG(StructMarshalStubs::OPERATION_ARGIDX);
        pcsSetup->EmitLDC(StructMarshalStubs::MarshalOperation::Marshal);
        pcsSetup->EmitBEQ(pMarshalStartLabel);
        pcsSetup->EmitLDARG(StructMarshalStubs::OPERATION_ARGIDX);
        pcsSetup->EmitLDC(StructMarshalStubs::MarshalOperation::Unmarshal);
        pcsSetup->EmitBEQ(pUnmarshalStartLabel);
        pcsSetup->EmitLDARG(StructMarshalStubs::OPERATION_ARGIDX);
        pcsSetup->EmitLDC(StructMarshalStubs::MarshalOperation::Cleanup);
        pcsSetup->EmitBEQ(pCleanupStartLabel);
        pcsSetup->EmitNOP("// } marshal operation jump table");

        // Clear native memory after release so we don't leave anything dangling.
        pcsCleanup->EmitLDARG(StructMarshalStubs::NATIVE_STRUCT_ARGIDX);
        pcsCleanup->EmitLDC(0);
        pcsCleanup->EmitLDC(m_nativeSize);
        pcsCleanup->EmitINITBLK();

        pcsMarshal->EmitLEAVE(pReturnLabel);
        pcsMarshal->EmitLabel(pCatchTrampolineStartLabel);
        // WARNING: The ILStubLinker has no knowledge that the exception object is on the stack
        //          (because it is
        //          unaware that we've just entered a catch block), so we lie about the number of arguments
        //          (say the method takes one less) to rebalance the stack.
        pcsMarshal->EmitCALL(METHOD__EXCEPTION_DISPATCH_INFO__CAPTURE, 0, 1);
        pcsMarshal->EmitSTLOC(dwExceptionDispatchInfoLocal);
        pcsMarshal->EmitLEAVE(pCleanupStartLabel);
        pcsMarshal->EmitLabel(pCatchTrampolineEndLabel);

        pcsDispatch->EmitLabel(pReturnLabel);
        pcsDispatch->EmitRET();

        pcsUnmarshal->EmitRET();

        pcsCleanup->EmitLDLOC(dwExceptionDispatchInfoLocal);
        pcsCleanup->EmitBRFALSE(pReturnLabel);
        pcsCleanup->EmitLDLOC(dwExceptionDispatchInfoLocal);
        pcsCleanup->EmitCALL(METHOD__EXCEPTION_DISPATCH_INFO__THROW, 0, 0);
        pcsCleanup->EmitRET();

        ILStubState::FinishEmit(pStubMD);
    }

    virtual void EmitExceptionHandler(LocalDesc* pNativeReturnType, LocalDesc* pManagedReturnType,
        ILCodeLabel** ppTryBeginLabel, ILCodeLabel** ppTryEndCatchBeginLabel, ILCodeLabel** ppCatchEndLabel)
    {
        *ppTryBeginLabel = pMarshalStartLabel;
        *ppTryEndCatchBeginLabel = pCatchTrampolineStartLabel;
        *ppCatchEndLabel = pCatchTrampolineEndLabel;
    }

private:
    ILCodeLabel* pMarshalStartLabel = nullptr;
    ILCodeLabel* pCatchTrampolineStartLabel = nullptr;
    ILCodeLabel* pCatchTrampolineEndLabel = nullptr;
    ILCodeLabel* pUnmarshalStartLabel = nullptr;
    ILCodeLabel* pCleanupStartLabel = nullptr;
    ILCodeLabel* pReturnLabel = nullptr;
    DWORD dwExceptionDispatchInfoLocal;

    UINT32 m_nativeSize;
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
                UpdateStubFlags(dwStubFlags, pTargetMD),
                iLCIDParamIdx,
                pTargetMD)
    {
        STANDARD_VM_CONTRACT;
        m_slIL.SetCallingConvention(unmgdCallConv, SF_IsVarArgStub(dwStubFlags));
    }

private:
    static DWORD UpdateStubFlags(DWORD dwStubFlags, MethodDesc* pTargetMD)
    {
        if (TargetHasThis(dwStubFlags))
        {
            dwStubFlags |= NDIRECTSTUB_FL_TARGET_HAS_THIS;
        }
        if (StubHasThis(dwStubFlags))
        {
            dwStubFlags |= NDIRECTSTUB_FL_STUB_HAS_THIS;
        }
        if (TargetSuppressGCTransition(dwStubFlags, pTargetMD))
        {
            dwStubFlags |= NDIRECTSTUB_FL_SUPPRESSGCTRANSITION;
        }
        return dwStubFlags;
    }

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

    static BOOL TargetSuppressGCTransition(DWORD dwStubFlags, MethodDesc* pTargetMD)
    {
        return SF_IsForwardStub(dwStubFlags) && pTargetMD && pTargetMD->ShouldSuppressGCTransition();
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
                dwStubFlags | NDIRECTSTUB_FL_STUB_HAS_THIS | NDIRECTSTUB_FL_TARGET_HAS_THIS,
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

        m_slIL.EmitLoadStubContext(pcsDispatch, dwStubFlags);

        pcsDispatch->EmitLDLOCA(m_slIL.GetTargetEntryPointLocalNum());

        DWORD dwIPRequiresCleanupLocalNum = pcsDispatch->NewLocal(ELEMENT_TYPE_BOOLEAN);
        pcsDispatch->EmitLDLOCA(dwIPRequiresCleanupLocalNum);

        // StubHelpers.GetCOMIPFromRCW(object objSrc, IntPtr pCPCMD, out IntPtr ppTarget, out bool pfNeedsRelease)
        pcsDispatch->EmitCALL(METHOD__STUBHELPERS__GET_COM_IP_FROM_RCW, 4, 1);

        // save it because we'll need it to compute the CALLI target and release it
        pcsDispatch->EmitDUP();
        pcsDispatch->EmitSTLOC(m_slIL.GetTargetInterfacePointerLocalNum());

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
                dwStubFlags | NDIRECTSTUB_FL_STUB_HAS_THIS | NDIRECTSTUB_FL_TARGET_HAS_THIS,
                iLCIDParamIdx,
                pTargetMD)
    {
        STANDARD_VM_CONTRACT;
    }

    void BeginEmit(DWORD dwStubFlags)  // COM to CLR IL
    {
        STANDARD_VM_CONTRACT;

        ILStubState::BeginEmit(dwStubFlags);

        m_slIL.GetDispatchCodeStream()->EmitLoadThis();
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

ILStubLinkerFlags GetILStubLinkerFlagsForNDirectStubFlags(NDirectStubFlags flags)
{
    DWORD result = ILSTUB_LINKER_FLAG_NONE;
    if (!SF_IsCOMStub(flags))
    {
        result |= ILSTUB_LINKER_FLAG_NDIRECT;
    }
    if (SF_IsReverseStub(flags))
    {
        result |= ILSTUB_LINKER_FLAG_REVERSE;
    }
    if (flags & NDIRECTSTUB_FL_SUPPRESSGCTRANSITION)
    {
        result |= ILSTUB_LINKER_FLAG_SUPPRESSGCTRANSITION;
    }
    if (flags & NDIRECTSTUB_FL_STUB_HAS_THIS)
    {
        result |= ILSTUB_LINKER_FLAG_STUB_HAS_THIS;
    }
    if (flags & NDIRECTSTUB_FL_TARGET_HAS_THIS)
    {
        result |= ILSTUB_LINKER_FLAG_TARGET_HAS_THIS;
    }
    return (ILStubLinkerFlags)result;
}

NDirectStubLinker::NDirectStubLinker(
            DWORD dwStubFlags,
            Module* pModule,
            const Signature &signature,
            SigTypeContext *pTypeContext,
            MethodDesc* pTargetMD,
            int  iLCIDParamIdx)
     : ILStubLinker(pModule, signature, pTypeContext, pTargetMD, GetILStubLinkerFlagsForNDirectStubFlags((NDirectStubFlags)dwStubFlags)),
    m_pCleanupFinallyBeginLabel(NULL),
    m_pCleanupFinallyEndLabel(NULL),
    m_pSkipExceptionCleanupLabel(NULL),
    m_fHasCleanupCode(FALSE),
    m_fHasExceptionCleanupCode(FALSE),
    m_fCleanupWorkListIsSetup(FALSE),
    m_targetHasThis((dwStubFlags & NDIRECTSTUB_FL_TARGET_HAS_THIS) != 0),
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

#if !defined(TARGET_X86)
    if (fIsVarArg)
    {
        // The JIT has to use a different calling convention for unmanaged vararg targets on 64-bit and ARM:
        // any float values must be duplicated in the corresponding general-purpose registers.
        uNativeCallingConv = IMAGE_CEE_CS_CALLCONV_NATIVEVARARG;
    }
    else
#endif // !TARGET_X86
    {
        switch (unmngCallConv)
        {
            case pmCallConvCdecl:
                uNativeCallingConv = IMAGE_CEE_CS_CALLCONV_C;
                break;
            case pmCallConvStdcall:
                uNativeCallingConv = IMAGE_CEE_CS_CALLCONV_STDCALL;
                break;
            case pmCallConvThiscall:
                uNativeCallingConv = IMAGE_CEE_CS_CALLCONV_THISCALL;
                break;
            default:
                _ASSERTE(!"Invalid calling convention.");
                uNativeCallingConv = IMAGE_CEE_CS_CALLCONV_STDCALL;
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

    pcsEmit->EmitLoadThis();
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
        LocalDesc desc(CoreLibBinder::GetClass(CLASS__CLEANUP_WORK_LIST_ELEMENT));
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

    if (SF_IsStructMarshalStub(m_dwStubFlags))
    {
        pcsEmit->EmitLDARG(StructMarshalStubs::CLEANUP_WORK_LIST_ARGIDX);
    }
    else
    {
        NeedsCleanupList();
        pcsEmit->EmitLDLOCA(GetCleanupWorkListLocalNum());
    }
}


void NDirectStubLinker::Begin(DWORD dwStubFlags)
{
    STANDARD_VM_CONTRACT;

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
        if (SF_IsDelegateStub(dwStubFlags))
        {
            //
            // recover delegate object from UMEntryThunk

            EmitLoadStubContext(m_pcsDispatch, dwStubFlags); // load UMEntryThunk*

            m_pcsDispatch->EmitLDC(offsetof(UMEntryThunk, m_pObjectHandle));
            m_pcsDispatch->EmitADD();
            m_pcsDispatch->EmitLDIND_I();      // get OBJECTHANDLE
            m_pcsDispatch->EmitLDIND_REF();    // get Delegate object
            m_pcsDispatch->EmitLDFLD(GetToken(CoreLibBinder::GetField(FIELD__DELEGATE__TARGET)));
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
    if ((IsCleanupNeeded() || hasTryCatchForHRESULT) && !SF_IsStructMarshalStub(dwStubFlags))
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
    if (IsCleanupNeeded() && !SF_IsStructMarshalStub(dwStubFlags))
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

    if (IsExceptionCleanupNeeded())
    {
        m_pcsExceptionCleanup->EmitLabel(m_pSkipExceptionCleanupLabel);
    }

    // Reload the return value
    if ((m_dwRetValLocalNum != (DWORD)-1) && !hasTryCatchForHRESULT && !SF_IsStructMarshalStub(dwStubFlags))
    {
        pcs->EmitLDLOC(m_dwRetValLocalNum);
    }
}

void NDirectStubLinker::DoNDirect(ILCodeStream *pcsEmit, DWORD dwStubFlags, MethodDesc * pStubMD)
{
    STANDARD_VM_CONTRACT;

    if (SF_IsStructMarshalStub(dwStubFlags))
    {
        // Struct marshal stubs do not call anything, so this is a no-op
        return;
    }

    if (SF_IsForwardStub(dwStubFlags)) // managed-to-native
    {

        if (SF_IsDelegateStub(dwStubFlags)) // delegate invocation
        {
            // get the delegate unmanaged target - we call a helper instead of just grabbing
            // the _methodPtrAux field because we may need to intercept the call for host, etc.
            pcsEmit->EmitLoadThis();
#ifdef TARGET_64BIT
            // on AMD64 GetDelegateTarget will return address of the generic stub for host when we are hosted
            // and update the secret argument with real target - the secret arg will be embedded in the
            // InlinedCallFrame by the JIT and fetched via TLS->Thread->Frame->Datum by the stub for host
            pcsEmit->EmitCALL(METHOD__STUBHELPERS__GET_STUB_CONTEXT_ADDR, 0, 1);
#else // !TARGET_64BIT
            // we don't need to do this on x86 because stub for host is generated dynamically per target
            pcsEmit->EmitLDNULL();
#endif // !TARGET_64BIT
            pcsEmit->EmitCALL(METHOD__STUBHELPERS__GET_DELEGATE_TARGET, 2, 1);
        }
        else // direct invocation
        {
            if (SF_IsCALLIStub(dwStubFlags)) // unmanaged CALLI
            {
                // if we ever NGEN CALLI stubs, this would have to be done differently
                _ASSERTE(!SF_IsNGENedStub(dwStubFlags));

                // for managed-to-unmanaged CALLI that requires marshaling, the target is passed
                // as the secret argument to the stub by GenericPInvokeCalliHelper (asmhelpers.asm)
                EmitLoadStubContext(pcsEmit, dwStubFlags);
#ifdef TARGET_64BIT
                // the secret arg has been shifted to left and ORed with 1 (see code:GenericPInvokeCalliHelper)
                pcsEmit->EmitLDC(1);
                pcsEmit->EmitSHR_UN();
#endif
            }
            else
#ifdef FEATURE_COMINTEROP
            if (!SF_IsCOMStub(dwStubFlags)) // forward P/Invoke
#endif // FEATURE_COMINTEROP
            {
                EmitLoadStubContext(pcsEmit, dwStubFlags);
                // pcsEmit->EmitCALL(METHOD__STUBHELPERS__GET_NDIRECT_TARGET, 1, 1);

#ifdef _DEBUG // There are five more pointer values for _DEBUG
                _ASSERTE(offsetof(NDirectMethodDesc, ndirect.m_pWriteableData) == sizeof(void*) * 8 + 8);
                pcsEmit->EmitLDC(TARGET_POINTER_SIZE * 8 + 8); // offsetof(NDirectMethodDesc, ndirect.m_pWriteableData)
#else // _DEBUG
                _ASSERTE(offsetof(NDirectMethodDesc, ndirect.m_pWriteableData) == sizeof(void*) * 3 + 8);
                pcsEmit->EmitLDC(TARGET_POINTER_SIZE * 3 + 8); // offsetof(NDirectMethodDesc, ndirect.m_pWriteableData)
#endif // _DEBUG
                pcsEmit->EmitADD();

                if (decltype(NDirectMethodDesc::ndirect.m_pWriteableData)::isRelative)
                {
                    pcsEmit->EmitDUP();
                }

                pcsEmit->EmitLDIND_I();

                if (decltype(NDirectMethodDesc::ndirect.m_pWriteableData)::isRelative)
                {
                    pcsEmit->EmitADD();
                }

                pcsEmit->EmitLDIND_I();
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
            int tokDelegate_methodPtr = pcsEmit->GetToken(CoreLibBinder::GetField(FIELD__DELEGATE__METHOD_PTR));

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

#ifndef DACCESS_COMPILE
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
#endif // DACCESS_COMPILE

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
        pcsEmit->EmitLoadNullPtr();
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
        pcsEmit->EmitLDNULL();
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
        pcsEmit->EmitLoadNullPtr();
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
    // get the secret argument via intrinsic
    pcsEmit->EmitCALL(METHOD__STUBHELPERS__GET_STUB_CONTEXT, 0, 1);
}

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

    void MarshalField(MarshalInfo* pInfo, UINT32 managedOffset, UINT32 nativeOffset, FieldDesc* pFieldDesc)
    {
        LIMITED_METHOD_CONTRACT;
        UNREACHABLE();
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
        ReadBestFitCustomAttribute(m_pModule, mdTypeDefNil, &bBestFit, &bThrowOnUnmappableChar);
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
    MethodDesc* pMD, LPCUTF8 *pLibName, LPCUTF8 *pEntryPointName)
{
    STANDARD_VM_CONTRACT;

    DllImportInit(pMD, pLibName, pEntryPointName);

    ReportErrors();
}

PInvokeStaticSigInfo::PInvokeStaticSigInfo(MethodDesc* pMD, ThrowOnError throwOnError)
{
    CONTRACTL
    {
        STANDARD_VM_CHECK;
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

    HRESULT hRESULT = pMT->GetCustomAttribute(
        WellKnownAttribute::UnmanagedFunctionPointer, (const VOID **)(&pData), (ULONG *)&cData);
    IfFailThrow(hRESULT);
    if (cData != 0)
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
            MDA_Last,
        };

        CaNamedArg namedArgs[MDA_Last];
        namedArgs[MDA_CharSet].InitI4FieldEnum("CharSet", "System.Runtime.InteropServices.CharSet", (ULONG)GetCharSet());
        namedArgs[MDA_BestFitMapping].InitBoolField("BestFitMapping", (ULONG)GetBestFitMapping());
        namedArgs[MDA_ThrowOnUnmappableChar].InitBoolField("ThrowOnUnmappableChar", (ULONG)GetThrowOnUnmappableChar());
        namedArgs[MDA_SetLastError].InitBoolField("SetLastError", 0);

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
            nlt = nltUnicode; break;
        case nltAuto:
#ifdef TARGET_WINDOWS
            nlt = nltUnicode;
#else
            nlt = nltAnsi; // We don't have a utf8 charset in metadata yet, but ANSI == UTF-8 off-Windows
#endif
        break;
        default:
            hr = E_FAIL; goto ErrExit;
        }
        SetCharSet ( nlt );
        SetBestFitMapping (namedArgs[MDA_BestFitMapping].val.u1);
        SetThrowOnUnmappableChar (namedArgs[MDA_ThrowOnUnmappableChar].val.u1);
        if (namedArgs[MDA_SetLastError].val.u1)
            SetLinkFlags ((CorNativeLinkFlags)(nlfLastError | GetLinkFlags()));
    }


ErrExit:
    if (hr != S_OK)
        SetError(IDS_EE_NDIRECT_BADNATL);

    InitCallConv(callConv, pMD->IsVarArg());

    if (throwOnError)
        ReportErrors();
}

PInvokeStaticSigInfo::PInvokeStaticSigInfo(
    Signature sig, Module* pModule)
{
    CONTRACTL
    {
        STANDARD_VM_CHECK;

        PRECONDITION(CheckPointer(pModule));
    }
    CONTRACTL_END;

    PreInit(pModule, NULL);
    m_sig = sig;
    SetIsStatic (!(MetaSig::GetCallingConvention(pModule, sig) & IMAGE_CEE_CS_CALLCONV_HASTHIS));
    InitCallConv((CorPinvokeMap)0, FALSE);

    ReportErrors();
}

void PInvokeStaticSigInfo::DllImportInit(MethodDesc* pMD, LPCUTF8 *ppLibName, LPCUTF8 *ppEntryPointName)
{
    CONTRACTL
    {
        STANDARD_VM_CHECK;

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
#if !defined(CROSSGEN_COMPILE) // IJW
        // The guessing heuristic has been broken with NGen for a long time since we stopped loading
        // images at NGen time using full LoadLibrary. The DLL references are not resolved correctly
        // without full LoadLibrary.
        //
        // Disable the heuristic consistently during NGen so that it does not kick in by accident.
        if (!IsCompilationProcess())
            BestGuessNDirectDefaults(pMD);
#endif

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

    // Keep in sync with the handling of CorNativeLinkType in
    // PInvokeStaticSigInfo::PInvokeStaticSigInfo.

    // charset : CorPinvoke -> CorNativeLinkType
    CorPinvokeMap charSetMask = (CorPinvokeMap)(mappingFlags & (pmCharSetNotSpec | pmCharSetAnsi | pmCharSetUnicode | pmCharSetAuto));
    if (charSetMask == pmCharSetNotSpec || charSetMask == pmCharSetAnsi)
    {
        SetCharSet (nltAnsi);
    }
    else if (charSetMask == pmCharSetUnicode)
    {
        SetCharSet (nltUnicode);
    }
    else if (charSetMask == pmCharSetAuto)
    {
#ifdef TARGET_WINDOWS
        SetCharSet(nltUnicode);
#else
        SetCharSet(nltAnsi); // We don't have a utf8 charset in metadata yet, but ANSI == UTF-8 off-Windows
#endif
    }
    else
    {
        SetError(IDS_EE_NDIRECT_BADNATL);
    }
}

#if !defined(CROSSGEN_COMPILE) // IJW

// This function would work, but be unused on Unix. Ifdefing out to avoid build errors due to the unused function.
#if !defined (TARGET_UNIX)
static LPBYTE FollowIndirect(LPBYTE pTarget)
{
    CONTRACT(LPBYTE)
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        POSTCONDITION(CheckPointer(RETVAL, NULL_OK));
    }
    CONTRACT_END;

    LPBYTE pRet = NULL;

    EX_TRY
    {
        AVInRuntimeImplOkayHolder AVOkay;

#ifdef TARGET_X86
        if (pTarget != NULL && !(pTarget[0] != 0xff || pTarget[1] != 0x25))
        {
            pRet = **(LPBYTE**)(pTarget + 2);
        }
#elif defined(TARGET_AMD64)
        if (pTarget != NULL && !(pTarget[0] != 0xff || pTarget[1] != 0x25))
        {
            INT64 rva = *(INT32*)(pTarget + 2);
            pRet = *(LPBYTE*)(pTarget + 6 + rva);
        }
#endif
    }
    EX_CATCH
    {
        // Catch AVs here.
    }
    EX_END_CATCH(SwallowAllExceptions);

    RETURN pRet;
}
#endif // !TARGET_UNIX

BOOL HeuristicDoesThisLookLikeAGetLastErrorCall(LPBYTE pTarget)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

#if !defined(TARGET_UNIX)
    static LPBYTE pGetLastError = NULL;
    if (!pGetLastError)
    {
        // No need to use a holder here, since no cleanup is necessary.
        HMODULE hMod = WszGetModuleHandle(WINDOWS_KERNEL32_DLLNAME_W);
        if (hMod)
        {
            pGetLastError = (LPBYTE)GetProcAddress(hMod, "GetLastError");
            if (!pGetLastError)
            {
                // This should never happen but better to be cautious.
                pGetLastError = (LPBYTE)-1;
            }
        }
        else
        {
            // We failed to get the module handle for kernel32.dll. This is almost impossible
            // however better to err on the side of caution.
            pGetLastError = (LPBYTE)-1;
        }
    }

    if (pTarget == pGetLastError)
        return TRUE;

    if (pTarget == NULL)
        return FALSE;

    LPBYTE pTarget2 = FollowIndirect(pTarget);
    if (pTarget2)
    {
        // jmp [xxxx] - could be an import thunk
        return pTarget2 == pGetLastError;
    }
#endif // !TARGET_UNIX

    return FALSE;
}

DWORD STDMETHODCALLTYPE FalseGetLastError()
{
    WRAPPER_NO_CONTRACT;

    return GetThread()->m_dwLastError;
}

void PInvokeStaticSigInfo::BestGuessNDirectDefaults(MethodDesc* pMD)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    if (!pMD->IsNDirect())
        return;

    NDirectMethodDesc* pMDD = (NDirectMethodDesc*)pMD;

    if (!pMDD->IsEarlyBound())
        return;

    LPVOID pTarget = NULL;

    // NOTE: If we get inside this block, and this is a call to GetLastError,
    //  then InitEarlyBoundNDirectTarget has not been run yet.
    if (pMDD->NDirectTargetIsImportThunk())
    {
        // Get the unmanaged callsite.
        pTarget = (LPVOID)pMDD->GetModule()->GetInternalPInvokeTarget(pMDD->GetRVA());

        // If this is a call to GetLastError, then we haven't overwritten m_pNativeNDirectTarget yet.
        if (HeuristicDoesThisLookLikeAGetLastErrorCall((LPBYTE)pTarget))
            pTarget = (BYTE*)FalseGetLastError;
    }
    else
    {
        pTarget = pMDD->GetNativeNDirectTarget();
    }
}

#endif // !CROSSGEN_COMPILE

inline CorPinvokeMap GetDefaultCallConv(BOOL bIsVarArg)
{
#ifdef TARGET_UNIX
    return pmCallConvCdecl;
#else // TARGET_UNIX
    return bIsVarArg ? pmCallConvCdecl : pmCallConvStdcall;
#endif // !TARGET_UNIX
}

namespace
{
    bool TryConvertCallConvValueToPInvokeCallConv(_In_ BYTE callConv, _Out_ CorPinvokeMap *pPinvokeMapOut)
    {
        LIMITED_METHOD_CONTRACT;

        switch (callConv)
        {
        case IMAGE_CEE_CS_CALLCONV_C:
            *pPinvokeMapOut = pmCallConvCdecl;
            return true;
        case IMAGE_CEE_CS_CALLCONV_STDCALL:
            *pPinvokeMapOut = pmCallConvStdcall;
            return true;
        case IMAGE_CEE_CS_CALLCONV_THISCALL:
            *pPinvokeMapOut = pmCallConvThiscall;
            return true;
        case IMAGE_CEE_CS_CALLCONV_FASTCALL:
            *pPinvokeMapOut = pmCallConvFastcall;
            return true;
        }

        return false;
    }

    HRESULT GetUnmanagedPInvokeCallingConvention(
        _In_ Module *pModule,
        _In_ PCCOR_SIGNATURE pSig,
        _In_ ULONG cSig,
        _Out_ CorPinvokeMap *pPinvokeMapOut,
        _Out_ UINT *errorResID)
    {
        STANDARD_VM_CONTRACT;

        CorUnmanagedCallingConvention callConvMaybe;
        bool suppressGCTransition;
        HRESULT hr = MetaSig::TryGetUnmanagedCallingConventionFromModOpt(GetScopeHandle(pModule), pSig, cSig, &callConvMaybe, &suppressGCTransition, errorResID);
        if (hr != S_OK)
            return hr;

        if (!TryConvertCallConvValueToPInvokeCallConv(callConvMaybe, pPinvokeMapOut))
            return S_FALSE;

        return hr;
    }
}

void PInvokeStaticSigInfo::InitCallConv(CorPinvokeMap callConv, BOOL bIsVarArg)
{
    STANDARD_VM_CONTRACT;

    // Convert WinAPI methods to either StdCall or CDecl based on if they are varargs or not.
    if (callConv == pmCallConvWinapi)
        callConv = GetDefaultCallConv(bIsVarArg);

    CorPinvokeMap sigCallConv = (CorPinvokeMap)0;
    UINT errorResID;
    HRESULT hr = GetUnmanagedPInvokeCallingConvention(m_pModule, m_sig.GetRawSig(), m_sig.GetRawSigLen(), &sigCallConv, &errorResID);
    if (FAILED(hr))
    {
        // Set an error message specific to P/Invokes or UnmanagedFunction for bad format.
        SetError(hr == COR_E_BADIMAGEFORMAT ? IDS_EE_NDIRECT_BADNATL : errorResID);
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
BOOL NDirect::MarshalingRequired(
    _In_opt_ MethodDesc* pMD,
    _In_opt_ PCCOR_SIGNATURE pSig,
    _In_opt_ Module* pModule,
    _In_ bool unmanagedCallersOnlyRequiresMarshalling)
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

        if (pMD->IsNDirect())
        {
            // A P/Invoke marked with UnmanagedCallersOnlyAttribute
            // doesn't technically require marshalling. However, we
            // don't support a DllImport with this attribute and we
            // error out during IL Stub generation so we indicate that
            // when checking if an IL Stub is needed.
            //
            // Callers can indicate the check doesn't need to be performed.
            if (unmanagedCallersOnlyRequiresMarshalling && pMD->HasUnmanagedCallersOnlyAttribute())
                return TRUE;

            NDirectMethodDesc* pNMD = (NDirectMethodDesc*)pMD;
            // Make sure running cctor can be handled by stub
            if (pNMD->IsClassConstructorTriggeredByILStub())
                return TRUE;
        }

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
                if (i > 0) dwStackSize += TARGET_POINTER_SIZE;
                break;
            }

            case ELEMENT_TYPE_INTERNAL:

                // this check is not functional in DAC and provides no security against a malicious dump
                // the DAC is prepared to receive an invalid type handle
#ifndef DACCESS_COMPILE
                if (pModule->IsSigInIL(arg.GetPtr()))
                    THROW_BAD_FORMAT(BFA_BAD_SIGNATURE, (Module*)pModule);
#endif

                FALLTHROUGH;

            case ELEMENT_TYPE_VALUETYPE:
            {
                TypeHandle hndArgType = arg.GetTypeHandleThrowing(pModule, &emptyTypeContext);

                // JIT can handle internal blittable value types
                if (!hndArgType.IsBlittable() && !hndArgType.IsEnum())
                {
                    return TRUE;
                }

#ifdef FEATURE_READYTORUN_COMPILER
                if (IsReadyToRunCompilation())
                {
                    if (!hndArgType.AsMethodTable()->IsLayoutInCurrentVersionBubble())
                        return TRUE;
                }
#endif
                if (i > 0)
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
        ms = MarshalInfo::MARSHAL_SCENARIO_COMINTEROP;
    }
    else
#endif // FEATURE_COMINTEROP
    {
        ms = MarshalInfo::MARSHAL_SCENARIO_NDIRECT;
    }

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
        if (SF_IsCOMStub(dwStubFlags))
        {
            // We don't support native methods that return VARIANTs, non-blittable structs, GUIDs, or DECIMALs directly.
            if (marshalType == MarshalInfo::MARSHAL_TYPE_OBJECT ||
                marshalType == MarshalInfo::MARSHAL_TYPE_VALUECLASS ||
                marshalType == MarshalInfo::MARSHAL_TYPE_GUID ||
                marshalType == MarshalInfo::MARSHAL_TYPE_DECIMAL)
            {
                if (!SF_IsHRESULTSwapping(dwStubFlags) && !SF_IsCOMLateBoundStub(dwStubFlags))
                {
                    COMPlusThrow(kMarshalDirectiveException, IDS_EE_COM_UNSUPPORTED_SIG);
                }
            }

            pss->MarshalReturn(&returnInfo, argOffset);
        }
        else
#endif // FEATURE_COMINTEROP
        {
            if (marshalType == MarshalInfo::MARSHAL_TYPE_BLITTABLEVALUECLASS
                    || marshalType == MarshalInfo::MARSHAL_TYPE_VALUECLASS
                    || marshalType == MarshalInfo::MARSHAL_TYPE_GUID
                    || marshalType == MarshalInfo::MARSHAL_TYPE_DECIMAL
                )
            {
                if (SF_IsHRESULTSwapping(dwStubFlags))
                {
                    // V1 restriction: we could implement this but it's late in the game to do so.
                    COMPlusThrow(kMarshalDirectiveException, IDS_EE_NDIRECT_UNSUPPORTED_SIG);
                }
            }
            else if (marshalType == MarshalInfo::MARSHAL_TYPE_CURRENCY
                    || marshalType == MarshalInfo::MARSHAL_TYPE_ARRAYWITHOFFSET
                    || marshalType == MarshalInfo::MARSHAL_TYPE_ARGITERATOR
#ifdef FEATURE_COMINTEROP
                    || marshalType == MarshalInfo::MARSHAL_TYPE_OLECOLOR
#endif // FEATURE_COMINTEROP
            )
            {
                // Each of these types are non-blittable and according to its managed size should be returned in a return buffer on x86 in stdcall.
                // However, its native size is small enough to be returned by-value.
                // We don't know the native type representation early enough to get this correct, so we throw an exception here.
                COMPlusThrow(kMarshalDirectiveException, IDS_EE_NDIRECT_UNSUPPORTED_SIG);
            }
            else if (IsUnsupportedTypedrefReturn(msig))
            {
                COMPlusThrow(kMarshalDirectiveException, IDS_EE_NDIRECT_UNSUPPORTED_SIG);
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
#ifdef TARGET_X86
    if (fThisCall)
    {
        // -1 means that the argument is not on the stack
        return (stackSize >= TARGET_POINTER_SIZE ? (stackSize - TARGET_POINTER_SIZE) : (UINT)-1);
    }
#endif // TARGET_X86
    return stackSize;
}

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
    UINT nativeStackSize = (SF_IsCOMStub(dwStubFlags) ? TARGET_POINTER_SIZE : 0);
    bool fStubNeedsCOM = SF_IsCOMStub(dwStubFlags);

    //
    // Marshal the arguments
    //
    MarshalInfo::MarshalScenario ms;
#ifdef FEATURE_COMINTEROP
    if (SF_IsCOMStub(dwStubFlags))
    {
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

    // Marshal the parameters
    int argidx = 1;
    int nativeArgIndex = 0;

    while (argidx <= numArgs)
    {
        //
        // Check to see if this is the parameter after which we need to insert the LCID.
        //
        if (argidx == iLCIDArg)
        {
            pss->MarshalLCID(argidx);
            nativeStackSize += TARGET_POINTER_SIZE;

            if (SF_IsReverseStub(dwStubFlags))
                argOffset++;
        }

        msig.NextArg();

        MarshalInfo &info = pParamMarshalInfo[argidx - 1];

        pss->MarshalArgument(&info, argOffset, GetStackOffsetFromStackSize(nativeStackSize, fThisCall));
        nativeStackSize += info.GetNativeArgSize();

        fStubNeedsCOM |= info.MarshalerRequiresCOM();

        if (fThisCall && argidx == 1)
        {
            // make sure that the first parameter is enregisterable
            if (info.GetNativeArgSize() > TARGET_POINTER_SIZE)
                COMPlusThrow(kMarshalDirectiveException, IDS_EE_NDIRECT_BADNATL_THISCALL);
        }

        argidx++;

        ++nativeArgIndex;
    }

    // Check to see if this is the parameter after which we need to insert the LCID.
    if (argidx == iLCIDArg)
    {
        pss->MarshalLCID(argidx);
        nativeStackSize += TARGET_POINTER_SIZE;

        if (SF_IsReverseStub(dwStubFlags))
            argOffset++;
    }

    marshalType = DoMarshalReturnValue(msig,
                            pParamTokenArray,
                            nlType,
                            nlFlags,
                            argidx,
                            pss,
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

    if (SF_IsHRESULTSwapping(dwStubFlags))
    {
        if (msig.GetReturnType() != ELEMENT_TYPE_VOID)
            nativeStackSize += TARGET_POINTER_SIZE;
    }

    if (pMD->IsDynamicMethod())
    {
        // Set the native stack size to the IL stub MD. It is needed for alignment
        // thunk generation on the Mac and stdcall name decoration on Windows.
        // We do not store it directly in the interop MethodDesc here because due
        // to sharing we come here only for the first call with given signature and
        // the target MD may even be NULL.

#ifdef TARGET_X86
        if (fThisCall)
        {
            _ASSERTE(nativeStackSize >= TARGET_POINTER_SIZE);
            nativeStackSize -= TARGET_POINTER_SIZE;
        }
#endif // TARGET_X86

        if (!FitsInU2(nativeStackSize))
            COMPlusThrow(kMarshalDirectiveException, IDS_EE_SIGTOOCOMPLEX);

        DynamicMethodDesc *pDMD = pMD->AsDynamicMethodDesc();

        pDMD->SetNativeStackArgSize(static_cast<WORD>(nativeStackSize));
        pDMD->SetStubNeedsCOMStarted(fStubNeedsCOM);
    }

    // FinishEmit needs to know the native stack arg size so we call it after the number
    // has been set in the stub MD (code:DynamicMethodDesc.SetNativeStackArgSize)
    pss->FinishEmit(pMD);
}

static void CreateStructStub(ILStubState* pss,
    StubSigDesc* pSigDesc,
    MethodTable* pMT,
    DWORD dwStubFlags,
    MethodDesc* pMD)
{
    CONTRACTL
    {
        STANDARD_VM_CHECK;
        PRECONDITION(CheckPointer(pss));
        PRECONDITION(CheckPointer(pSigDesc));
        PRECONDITION(CheckPointer(pMD, NULL_OK));
        PRECONDITION(!pMD || pMD->IsILStub());
        PRECONDITION(SF_IsStructMarshalStub(dwStubFlags));
    }
    CONTRACTL_END;

    SF_ConsistencyCheck(dwStubFlags);

#ifdef _DEBUG
    if (g_pConfig->ShouldBreakOnInteropStubSetup(pSigDesc->m_pDebugName))
        CONSISTENCY_CHECK_MSGF(false, ("BreakOnInteropStubSetup: '%s' ", pSigDesc->m_pDebugName));
#endif // _DEBUG

    Module* pModule = pSigDesc->m_pModule;


    pss->SetLastError(false);

    pss->BeginEmit(dwStubFlags);

    // Marshal the fields
    MarshalInfo::MarshalScenario ms = MarshalInfo::MARSHAL_SCENARIO_FIELD;

    EEClassNativeLayoutInfo const* pNativeLayoutInfo = pMT->GetNativeLayoutInfo();

    int numFields = pNativeLayoutInfo->GetNumFields();
    // Build up marshaling information for each of the method's parameters
    SIZE_T cbFieldMarshalInfo;
    if (!ClrSafeInt<SIZE_T>::multiply(sizeof(MarshalInfo), numFields, cbFieldMarshalInfo))
    {
        COMPlusThrowHR(COR_E_OVERFLOW);
    }

    CorNativeLinkType nlType = pMT->GetCharSet();

    NativeFieldDescriptor const* pFieldDescriptors = pNativeLayoutInfo->GetNativeFieldDescriptors();

    for (int i = 0; i < numFields; ++i)
    {
        NativeFieldDescriptor const& nativeFieldDescriptor = pFieldDescriptors[i];
        PTR_FieldDesc pFD = nativeFieldDescriptor.GetFieldDesc();
        SigPointer fieldSig = pFD->GetSigPointer();
        // The first byte in a field signature is always 0x6 per ECMA 335. Skip over this byte to get to the rest of the signature for the MarshalInfo constructor.
        (void)fieldSig.GetByte(nullptr);
        SigTypeContext context(pFD, TypeHandle(pMT));

        MarshalInfo mlInfo(pFD->GetModule(),
            fieldSig,
            &context,
            pFD->GetMemberDef(),
            ms,
            nlType,
            nlfNone,
            TRUE,
            i + 1,
            numFields,
            SF_IsBestFit(dwStubFlags),
            SF_IsThrowOnUnmappableChar(dwStubFlags),
            TRUE,
            pMD,
            TRUE
            DEBUG_ARG(pSigDesc->m_pDebugName)
            DEBUG_ARG(pSigDesc->m_pDebugClassName)
            DEBUG_ARG(-1 /* field */));

        pss->MarshalField(&mlInfo, pFD->GetOffset(), nativeFieldDescriptor.GetExternalOffset(), pFD);
    }

    if (pMD->IsDynamicMethod())
    {
        DynamicMethodDesc* pDMD = pMD->AsDynamicMethodDesc();
        pDMD->SetNativeStackArgSize(4 * TARGET_POINTER_SIZE); // The native stack arg size is constant since the signature for struct stubs is constant.
    }

    // FinishEmit needs to know the native stack arg size so we call it after the number
    // has been set in the stub MD (code:DynamicMethodDesc.SetNativeStackArgSize)
    pss->FinishEmit(pMD);
}

class NDirectStubHashBlob : public ILStubHashBlobBase
{
public:
    Module*     m_pModule;
    MethodTable* m_pMT;

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

    pBlob->m_pMT = pParams->m_pMT;
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
static void CreateNDirectStubAccessMetadata(
                StubSigDesc*    pSigDesc,       // IN
                CorPinvokeMap   unmgdCallConv,  // IN
                DWORD*          pdwStubFlags,   // IN/OUT
                int*            piLCIDArg,      // OUT
                int*            pNumArgs        // OUT
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

    int lcidArg = -1;

    // Check if we have a MethodDesc to query for additional data.
    if (pSigDesc->m_pMD != NULL)
    {
        MethodDesc* pMD = pSigDesc->m_pMD;

        // P/Invoke marked with UnmanagedCallersOnlyAttribute is not
        // presently supported.
        if (pMD->HasUnmanagedCallersOnlyAttribute())
            COMPlusThrow(kNotSupportedException, IDS_EE_NDIRECT_UNSUPPORTED_SIG);

        // Check to see if we need to do LCID conversion.
        lcidArg = GetLCIDParameterIndex(pMD);
        if (lcidArg != -1 && lcidArg > (*pNumArgs))
            COMPlusThrow(kIndexOutOfRangeException, IDS_EE_INVALIDLCIDPARAM);
    }

    (*piLCIDArg) = lcidArg;

    if (SF_IsCOMStub(*pdwStubFlags))
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

void NDirect::PopulateNDirectMethodDesc(NDirectMethodDesc* pNMD, PInvokeStaticSigInfo* pSigInfo)
{
    if (pNMD->IsSynchronized())
        COMPlusThrow(kTypeLoadException, IDS_EE_NOSYNCHRONIZED);

    WORD ndirectflags = 0;
    if (pNMD->MethodDesc::IsVarArg())
        ndirectflags |= NDirectMethodDesc::kVarArgs;

    LPCUTF8 szLibName = NULL, szEntryPointName = NULL;
    new (pSigInfo) PInvokeStaticSigInfo(pNMD, &szLibName, &szEntryPointName);

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

    if (pNMD->GetLoaderModule()->IsSystem() && (strcmp(szLibName, "QCall") == 0))
    {
        ndirectflags |= NDirectMethodDesc::kIsQCall;
    }
    else
    {
        pNMD->ndirect.m_pszLibName.SetValueMaybeNull(szLibName);
        pNMD->ndirect.m_pszEntrypointName.SetValueMaybeNull(szEntryPointName);
    }

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

    //
    // Find out if we have the attribute
    //
    const void *pBytes;
    ULONG cbBytes;

    // Support v-table forward classic COM interop calls only
    if (SF_IsCOMStub(dwStubFlags) && SF_IsForwardStub(dwStubFlags))
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
            hr = pTargetMD->GetCustomAttribute(
                WellKnownAttribute::ManagedToNativeComInteropStub,
                &pBytes,
                &cbBytes);

            if (FAILED(hr))
                RETURN hr;
            // GetCustomAttribute returns S_FALSE when it cannot find the attribute but nothing fails...
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

    AccessCheckContext accessContext(pTargetMD, pTargetMT);

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
                         int                iLCIDArg,
                         bool*              pGeneratedNewStub = nullptr
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
    MethodTable*    pTargetMT = pSigDesc->m_pMT;
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
                               iLCIDArg,
                               pSigDesc->m_pMT
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

                // We have the entry lock we need to use, so we can release the global lock.
                pILStubLock.Release();

                {
                    // The holder will free the allocated MethodDesc and restore the ILStubCache
                    // if exception happen. The reason to get the holder again is to
                    ILStubCreatorHelperHolder pGenILHolder(&ilStubCreatorHelper);

                    if (!pEntryLock.DeadlockAwareAcquire())
                    {
                        // the IL generation is not recursive.
                        // However, we can encounter a recursive situation when attempting to
                        // marshal a struct containing a layout class containing another struct.
                        // Throw an exception here instead of asserting.
                        if (SF_IsStructMarshalStub(dwStubFlags))
                        {
                            _ASSERTE(pSigDesc->m_pMT != nullptr);
                            StackSString strTypeName;
                            TypeString::AppendType(strTypeName, TypeHandle(pSigDesc->m_pMT));
                            COMPlusThrow(kTypeLoadException, IDS_CANNOT_MARSHAL_RECURSIVE_DEF, strTypeName.GetUnicode());
                        }
                        UNREACHABLE_MSG("unexpected deadlock in IL stub generation!");
                    }

                    if (SF_IsSharedStub(params.m_dwStubFlags))
                    {
                        // We need to re-acquire the lock in case we need to get a new pStubMD
                        // in the case that the owner of the shared stub was destroyed.
                        pILStubLock.Acquire();

                        // Assure that pStubMD we have now has not been destroyed by other threads
                        pGenILHolder->GetStubMethodDesc();

                        while (pStubMD != pGenILHolder->GetStubMD())
                        {
                            pStubMD = pGenILHolder->GetStubMD();

                            pEntry.Assign(ListLockEntry::Find(pILStubLock, pStubMD, "il stub gen lock"));
                            pEntryLock.Assign(pEntry, FALSE);

                            // We have the entry lock we need to use, so we can release the global lock.
                            pILStubLock.Release();

                            if (!pEntryLock.DeadlockAwareAcquire())
                            {
                                // the IL generation is not recursive.
                                // However, we can encounter a recursive situation when attempting to
                                // marshal a struct containing a layout class containing another struct.
                                // Throw an exception here instead of asserting.
                                if (SF_IsStructMarshalStub(dwStubFlags))
                                {
                                    _ASSERTE(pSigDesc->m_pMT != nullptr);
                                    StackSString strTypeName;
                                    TypeString::AppendType(strTypeName, TypeHandle(pSigDesc->m_pMT));
                                    COMPlusThrow(kTypeLoadException, IDS_CANNOT_MARSHAL_RECURSIVE_DEF, strTypeName.GetUnicode());
                                }
                                UNREACHABLE_MSG("unexpected deadlock in IL stub generation!");
                            }

                            pILStubLock.Acquire();

                            pGenILHolder->GetStubMethodDesc();
                        }
                    }

                    for (;;)
                    {
                        // We have the entry lock now, we can release the global lock
                        pILStubLock.Release();

                        _ASSERTE(pEntryLock.GetValue()->HasLock());

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

                            if (SF_IsStructMarshalStub(dwStubFlags))
                            {
                                CreateStructStub(pss, pSigDesc, pTargetMT, dwStubFlags, pStubMD);
                            }
                            else
                            {
                                CreateNDirectStubWorker(pss,
                                                        pSigDesc,
                                                        nlType,
                                                        nlFlags,
                                                        unmgdCallConv,
                                                        dwStubFlags,
                                                        pStubMD,
                                                        pParamTokenArray,
                                                        iLCIDArg);
                            }


                            pResolver->SetTokenLookupMap(pss->GetTokenLookupMap());

                            pResolver->SetStubTargetMethodSig(
                                pss->GetStubTargetMethodSig(),
                                pss->GetStubTargetMethodSigLength());

                            // we successfully generated the IL stub
                            sgh.SuppressRelease();
                        }

                        if (pGeneratedNewStub)
                        {
                            *pGeneratedNewStub = true;
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

#if defined(TARGET_X86)
    if (SF_IsForwardStub(dwStubFlags) && pTargetMD != NULL && !pTargetMD->IsVarArg())
    {
        // copy the stack arg byte count from the stub MD to the target MD - this number is computed
        // during stub generation and is copied to all target MDs that share the stub
        // (we don't set it for varargs - the number is call site specific)
        // also copy the "takes parameters with copy constructors" flag which is needed to generate
        // appropriate intercept stub

        WORD cbStackArgSize = pStubMD->AsDynamicMethodDesc()->GetNativeStackArgSize();
        if (pTargetMD->IsNDirect())
        {
            NDirectMethodDesc *pTargetNMD = (NDirectMethodDesc *)pTargetMD;

            pTargetNMD->SetStackArgumentSize(cbStackArgSize, (CorPinvokeMap)0);
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
                }
            }
        }
#endif // FEATURE_COMINTEROP
    }
#endif // defined(TARGET_X86)

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

    MethodDesc *pMD = pSigDesc->m_pMD;

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

    StubSigDesc sigDesc(nullptr, Signature(szMetaSig, cbMetaSigSize), pModule);

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

#ifndef DACCESS_COMPILE

MethodDesc* NDirect::CreateStructMarshalILStub(MethodTable* pMT)
{
    CONTRACT(MethodDesc*)
    {
        STANDARD_VM_CHECK;

        PRECONDITION(CheckPointer(pMT));
        POSTCONDITION(CheckPointer(RETVAL));
    }
    CONTRACT_END;

    if (IsReadyToRunCompilation())
    {
        // We don't support emitting struct marshalling IL stubs into R2R images.
        ThrowHR(E_FAIL);
    }

    DWORD dwStubFlags = NDIRECTSTUB_FL_STRUCT_MARSHAL;

    BOOL bestFit, throwOnUnmappableChar;

    ReadBestFitCustomAttribute(pMT->GetModule(), pMT->GetCl(), &bestFit, &throwOnUnmappableChar);

    if (bestFit == TRUE)
    {
        dwStubFlags |= NDIRECTSTUB_FL_BESTFIT;
    }
    if (throwOnUnmappableChar == TRUE)
    {
        dwStubFlags |= NDIRECTSTUB_FL_THROWONUNMAPPABLECHAR;
    }

    // ValueClass signature:
    // void (ref Struct managedData, native Struct* nativeData, int marshalAction, ref CleanupWorkListElement cwl)
    // LayoutClass signature:
    // void (ref byte managedData, byte* nativeData, int marshalAction, ref CleanupWorkListElement cwl)
    constexpr int numParamTokens = 1;
    mdParamDef pParamTokenArray[numParamTokens];
    pParamTokenArray[0] = (mdParamDef)pMT->GetCl();

    FunctionSigBuilder sigBuilder;

    sigBuilder.SetCallingConv(IMAGE_CEE_CS_CALLCONV_DEFAULT);
    LocalDesc returnType(ELEMENT_TYPE_VOID);
    sigBuilder.SetReturnType(&returnType);


    if (pMT->IsValueType())
    {
        LocalDesc managedParameter(pMT);
        managedParameter.MakeByRef();
        sigBuilder.NewArg(&managedParameter);

        LocalDesc nativeValueType(TypeHandle{ pMT }.MakeNativeValueType());
        nativeValueType.MakePointer();
        sigBuilder.NewArg(&nativeValueType);
    }
    else
    {
        LocalDesc byteRef(ELEMENT_TYPE_I1);
        byteRef.MakeByRef();
        sigBuilder.NewArg(&byteRef);
        LocalDesc bytePtr(ELEMENT_TYPE_I1);
        bytePtr.MakePointer();
        sigBuilder.NewArg(&bytePtr);
    }

    LocalDesc i4(ELEMENT_TYPE_I4);
    sigBuilder.NewArg(&i4);

    LocalDesc cleanupWorkList(CoreLibBinder::GetClass(CLASS__CLEANUP_WORK_LIST_ELEMENT));
    cleanupWorkList.MakeByRef();
    sigBuilder.NewArg(&cleanupWorkList);

    DWORD cbMetaSigSize = sigBuilder.GetSigSize();
    AllocMemHolder<BYTE> szMetaSig(pMT->GetLoaderAllocator()->GetHighFrequencyHeap()->AllocMem(S_SIZE_T(cbMetaSigSize)));
    sigBuilder.GetSig(szMetaSig, cbMetaSigSize);

    StubSigDesc sigDesc(pMT, Signature(szMetaSig, cbMetaSigSize), pMT->GetModule());

#ifdef _DEBUG
    sigDesc.m_pDebugName = "Struct Marshalling Stub";
    sigDesc.m_pDebugClassName = pMT->GetDebugClassName();
#endif // _DEBUG

    Signature signature(szMetaSig, cbMetaSigSize);

    NewHolder<ILStubState> pStubState = new StructMarshal_ILStubState(pMT, signature, &sigDesc.m_typeContext, dwStubFlags);

    bool generatedNewStub = false;

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
        -1,
        &generatedNewStub);

    if (generatedNewStub) // If we generated a new stub, we need to keep the signature we created allocated.
    {
        szMetaSig.SuppressRelease();
    }

    RETURN pStubMD;
}

PCODE NDirect::GetEntryPointForStructMarshalStub(MethodTable* pMT)
{
    LIMITED_METHOD_CONTRACT;

    MethodDesc* pMD = CreateStructMarshalILStub(pMT);

    _ASSERTE(pMD != nullptr);

    return pMD->GetMultiCallableAddrOfCode();
}

#endif // DACCESS_COMPILE

MethodDesc* NDirect::CreateCLRToNativeILStub(PInvokeStaticSigInfo* pSigInfo,
                         DWORD dwStubFlags,
                         MethodDesc* pMD)
{
    STANDARD_VM_CONTRACT;

    StubSigDesc sigDesc(pMD, pSigInfo);

    return CreateCLRToNativeILStub(&sigDesc,
                                    pSigInfo->GetCharSet(),
                                    pSigInfo->GetLinkFlags(),
                                    pSigInfo->GetCallConv(),
                                    pSigInfo->GetStubFlags() | dwStubFlags);
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

#ifdef FEATURE_COMINTEROP
    if (SF_IsReverseCOMStub(dwStubFlags))
    {
#ifdef FEATURE_PREJIT
        // reverse COM stubs live in a hash table
        StubMethodHashTable *pHash = pMD->GetLoaderModule()->GetStubMethodHashTable();
        return (pHash == NULL ? NULL : pHash->FindMethodDesc(pMD));
#else
        return NULL;
#endif
    }
    else
#endif // FEATURE_COMINTEROP
    if (pMD->IsNDirect())
    {
        NDirectMethodDesc* pNMD = (NDirectMethodDesc*)pMD;
        return pNMD->ndirect.m_pStubMD.GetValueMaybeNull();
    }
#ifdef FEATURE_COMINTEROP
    else if (pMD->IsComPlusCall() || pMD->IsGenericComPlusCall())
    {
        ComPlusCallInfo *pComInfo = ComPlusCallInfo::FromMethodDesc(pMD);
        return (pComInfo == NULL ? NULL : pComInfo->m_pStubMD.GetValueMaybeNull());
    }
#endif // FEATURE_COMINTEROP
    else if (pMD->IsEEImpl())
    {
        DelegateEEClass *pClass = (DelegateEEClass *)pMD->GetClass();
        if (SF_IsReverseStub(dwStubFlags))
        {
            return pClass->m_pReverseStubMD;
        }
        else
        {
            return pClass->m_pForwardStubMD;
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
        NDirect::PopulateNDirectMethodDesc(pNMD, &sigInfo);

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

    if (pNMD->IsEarlyBound())
    {
        pNMD->InitEarlyBoundNDirectTarget();
    }
    else
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

            pCode = pStubMD->PrepareInitialCode();

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
            SetupGcCoverage(NativeCodeVersion(pStubMD), (BYTE*)pCode);
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
        *ppStubMD = pStubMD;

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
LPVOID NDirect::NDirectGetEntryPoint(NDirectMethodDesc *pMD, NATIVE_LIBRARY_HANDLE hMod)
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

    NDirectWriteableData* pWriteableData = GetWriteableData();
    g_IBCLogger.LogNDirectCodeAccess(this);
    pWriteableData->m_pNDirectTarget = pTarget;
}



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

    VOID TrackErrorCode()
    {
        LIMITED_METHOD_CONTRACT;

        DWORD priority;

#ifdef TARGET_UNIX

        SetMessage(PAL_GetLoadLibraryError());
#else

        DWORD dwLastError = GetLastError();

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
#endif
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

    SString& GetMessage()
    {
        return m_message;
    }

    void DECLSPEC_NORETURN Throw(SString &libraryNameOrPath)
    {
        STANDARD_VM_CONTRACT;

#if defined(__APPLE__)
        COMPlusThrow(kDllNotFoundException, IDS_EE_NDIRECT_LOADLIB_MAC, libraryNameOrPath.GetUnicode(), GetMessage());
#elif defined(TARGET_UNIX)
        COMPlusThrow(kDllNotFoundException, IDS_EE_NDIRECT_LOADLIB_LINUX, libraryNameOrPath.GetUnicode(), GetMessage());
#else // __APPLE__
        HRESULT theHRESULT = GetHR();
        if (theHRESULT == HRESULT_FROM_WIN32(ERROR_BAD_EXE_FORMAT))
        {
            COMPlusThrow(kBadImageFormatException);
        }
        else
        {
            SString hrString;
            GetHRMsg(theHRESULT, hrString);
            COMPlusThrow(kDllNotFoundException, IDS_EE_NDIRECT_LOADLIB_WIN, libraryNameOrPath.GetUnicode(), hrString);
        }
#endif // TARGET_UNIX

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

    void SetMessage(LPCSTR message)
    {
        m_message = SString(SString::Utf8, message);
    }

    HRESULT m_hr;
    DWORD   m_priorityOfLastError;
    SString  m_message;
};  // class LoadLibErrorTracker

// Load the library directly and return the raw system handle
static NATIVE_LIBRARY_HANDLE LocalLoadLibraryHelper( LPCWSTR name, DWORD flags, LoadLibErrorTracker *pErrorTracker )
{
    STANDARD_VM_CONTRACT;

    NATIVE_LIBRARY_HANDLE hmod = NULL;

#ifndef TARGET_UNIX

    if ((flags & 0xFFFFFF00) != 0)
    {
        hmod = CLRLoadLibraryEx(name, NULL, flags & 0xFFFFFF00);
        if (hmod != NULL)
        {
            return hmod;
        }

        DWORD dwLastError = GetLastError();
        if (dwLastError != ERROR_INVALID_PARAMETER)
        {
            pErrorTracker->TrackErrorCode();
            return hmod;
        }
    }

    hmod = CLRLoadLibraryEx(name, NULL, flags & 0xFF);

#else // !TARGET_UNIX
    hmod = PAL_LoadLibraryDirect(name);
#endif // !TARGET_UNIX

    if (hmod == NULL)
    {
        pErrorTracker->TrackErrorCode();
    }

    return hmod;
}

#define TOLOWER(a) (((a) >= W('A') && (a) <= W('Z')) ? (W('a') + (a - W('A'))) : (a))
#define TOHEX(a)   ((a)>=10 ? W('a')+(a)-10 : W('0')+(a))

#ifdef TARGET_UNIX
#define PLATFORM_SHARED_LIB_SUFFIX_W PAL_SHLIB_SUFFIX_W
#define PLATFORM_SHARED_LIB_PREFIX_W PAL_SHLIB_PREFIX_W
#else // !TARGET_UNIX
#define PLATFORM_SHARED_LIB_SUFFIX_W W(".dll")
#define PLATFORM_SHARED_LIB_PREFIX_W W("")
#endif // !TARGET_UNIX

// The Bit 0x2 has different semantics in DllImportSearchPath and LoadLibraryExA flags.
// In DllImportSearchPath enum, bit 0x2 represents SearchAssemblyDirectory -- which is performed by CLR.
// Unlike other bits in this enum, this bit shouldn't be directly passed on to LoadLibrary()
#define DLLIMPORTSEARCHPATH_ASSEMBLYDIRECTORY 0x2

// DllImportSearchPathFlags is a special enumeration, whose values are tied closely with LoadLibrary flags.
// There is no "default" value DllImportSearchPathFlags. In the absence of DllImportSearchPath attribute,
// CoreCLR's LoadLibrary implementation uses the following defaults.
// Other implementations of LoadLibrary callbacks/events are free to use other default conventions.
void GetDefaultDllImportSearchPathFlags(DWORD *dllImportSearchPathFlags, BOOL *searchAssemblyDirectory)
{
    STANDARD_VM_CONTRACT;

    *searchAssemblyDirectory = TRUE;
    *dllImportSearchPathFlags = 0;
}

// If a module has the DefaultDllImportSearchPathsAttribute, get DllImportSearchPathFlags from it, and return true.
// Otherwise, get CoreCLR's default value for DllImportSearchPathFlags, and return false.
BOOL GetDllImportSearchPathFlags(Module *pModule, DWORD *dllImportSearchPathFlags, BOOL *searchAssemblyDirectory)
{
    STANDARD_VM_CONTRACT;

    if (pModule->HasDefaultDllImportSearchPathsAttribute())
    {
        *dllImportSearchPathFlags = pModule->DefaultDllImportSearchPathsAttributeCachedValue();
        *searchAssemblyDirectory = pModule->DllImportSearchAssemblyDirectory();
        return TRUE;
    }

    GetDefaultDllImportSearchPathFlags(dllImportSearchPathFlags, searchAssemblyDirectory);
    return FALSE;
}

// If a pInvoke has the DefaultDllImportSearchPathsAttribute, get DllImportSearchPathFlags from it, and returns true.
// Otherwise, if the containing assembly has the DefaultDllImportSearchPathsAttribute, get DllImportSearchPathFlags from it, and returns true.
// Otherwise, get CoreCLR's default value for DllImportSearchPathFlags, and return false.
BOOL GetDllImportSearchPathFlags(NDirectMethodDesc * pMD, DWORD *dllImportSearchPathFlags, BOOL *searchAssemblyDirectory)
{
    STANDARD_VM_CONTRACT;

    if (pMD->HasDefaultDllImportSearchPathsAttribute())
    {
        *dllImportSearchPathFlags = pMD->DefaultDllImportSearchPathsAttributeCachedValue();
        *searchAssemblyDirectory = pMD->DllImportSearchAssemblyDirectory();
        return TRUE;
    }

    return GetDllImportSearchPathFlags(pMD->GetModule(), dllImportSearchPathFlags, searchAssemblyDirectory);
}

// static
NATIVE_LIBRARY_HANDLE NDirect::LoadLibraryFromPath(LPCWSTR libraryPath, BOOL throwOnError)
{
    CONTRACTL
    {
        STANDARD_VM_CHECK;
        PRECONDITION(CheckPointer(libraryPath));
    }
    CONTRACTL_END;

    LoadLibErrorTracker errorTracker;
    const NATIVE_LIBRARY_HANDLE hmod =
        LocalLoadLibraryHelper(libraryPath, GetLoadWithAlteredSearchPathFlag(), &errorTracker);

    if (throwOnError && (hmod == nullptr))
    {
        SString libraryPathSString(libraryPath);
        errorTracker.Throw(libraryPathSString);
    }
    return hmod;
}

// static
void NDirect::FreeNativeLibrary(NATIVE_LIBRARY_HANDLE handle)
{
    STANDARD_VM_CONTRACT;
    _ASSERTE(handle != NULL);

#ifndef TARGET_UNIX
    BOOL retVal = FreeLibrary(handle);
#else // !TARGET_UNIX
    BOOL retVal = PAL_FreeLibraryDirect(handle);
#endif // !TARGET_UNIX

    if (retVal == 0)
        COMPlusThrow(kInvalidOperationException, W("Arg_InvalidOperationException"));
}

//static
INT_PTR NDirect::GetNativeLibraryExport(NATIVE_LIBRARY_HANDLE handle, LPCWSTR symbolName, BOOL throwOnError)
{
    CONTRACTL
    {
        STANDARD_VM_CHECK;
        PRECONDITION(CheckPointer(handle));
        PRECONDITION(CheckPointer(symbolName));
    }
    CONTRACTL_END;

    MAKE_UTF8PTR_FROMWIDE(lpstr, symbolName);

#ifndef TARGET_UNIX
    INT_PTR address = reinterpret_cast<INT_PTR>(GetProcAddress((HMODULE)handle, lpstr));
    if ((address == NULL) && throwOnError)
        COMPlusThrow(kEntryPointNotFoundException, IDS_EE_NDIRECT_GETPROCADDR_WIN_DLL, symbolName);
#else // !TARGET_UNIX
    INT_PTR address = reinterpret_cast<INT_PTR>(PAL_GetProcAddressDirect(handle, lpstr));
    if ((address == NULL) && throwOnError)
        COMPlusThrow(kEntryPointNotFoundException, IDS_EE_NDIRECT_GETPROCADDR_UNIX_SO, symbolName);
#endif // !TARGET_UNIX

    return address;
}

namespace
{
#ifndef TARGET_UNIX
    BOOL IsWindowsAPISet(PCWSTR wszLibName)
    {
        STANDARD_VM_CONTRACT;

        // This is replicating quick check from the OS implementation of api sets.
        return SString::_wcsnicmp(wszLibName, W("api-"), 4) == 0 ||
               SString::_wcsnicmp(wszLibName, W("ext-"), 4) == 0;
    }
#endif // !TARGET_UNIX

    NATIVE_LIBRARY_HANDLE LoadNativeLibraryViaAssemblyLoadContext(Assembly * pAssembly, PCWSTR wszLibName)
    {
        STANDARD_VM_CONTRACT;

#ifndef TARGET_UNIX
        if (IsWindowsAPISet(wszLibName))
        {
            // Prevent Overriding of Windows API sets.
            return NULL;
        }
#endif // !TARGET_UNIX

        NATIVE_LIBRARY_HANDLE hmod = NULL;
        AppDomain* pDomain = GetAppDomain();
        CLRPrivBinderCoreCLR *pTPABinder = pDomain->GetTPABinderContext();

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

        //Step 1: If the assembly was not bound using TPA,
        //        Call System.Runtime.Loader.AssemblyLoadContext.ResolveUnmanagedDll to give
        //        The custom assembly context a chance to load the unmanaged dll.

        GCX_COOP();

        STRINGREF pUnmanagedDllName;
        pUnmanagedDllName = StringObject::NewString(wszLibName);

        GCPROTECT_BEGIN(pUnmanagedDllName);

        // Get the pointer to the managed assembly load context
        INT_PTR ptrManagedAssemblyLoadContext = ((CLRPrivBinderAssemblyLoadContext *)pCurrentBinder)->GetManagedAssemblyLoadContext();

        // Prepare to invoke  System.Runtime.Loader.AssemblyLoadContext.ResolveUnmanagedDll method.
        PREPARE_NONVIRTUAL_CALLSITE(METHOD__ASSEMBLYLOADCONTEXT__RESOLVEUNMANAGEDDLL);
        DECLARE_ARGHOLDER_ARRAY(args, 2);
        args[ARGNUM_0]  = STRINGREF_TO_ARGHOLDER(pUnmanagedDllName);
        args[ARGNUM_1]  = PTR_TO_ARGHOLDER(ptrManagedAssemblyLoadContext);

        // Make the call
        CALL_MANAGED_METHOD(hmod, NATIVE_LIBRARY_HANDLE, args);

        GCPROTECT_END();

        return hmod;
    }

    // Return the AssemblyLoadContext for an assembly
    INT_PTR GetManagedAssemblyLoadContext(Assembly* pAssembly)
    {
        STANDARD_VM_CONTRACT;

        PTR_ICLRPrivBinder pBindingContext = pAssembly->GetManifestFile()->GetBindingContext();
        if (pBindingContext == NULL)
        {
            // GetBindingContext() returns NULL for System.Private.CoreLib
            return NULL;
        }

        UINT_PTR assemblyBinderID = 0;
        IfFailThrow(pBindingContext->GetBinderID(&assemblyBinderID));

        AppDomain *pDomain = GetAppDomain();
        ICLRPrivBinder *pCurrentBinder = reinterpret_cast<ICLRPrivBinder *>(assemblyBinderID);

        // The code here deals with two implementations of ICLRPrivBinder interface:
        //    - CLRPrivBinderCoreCLR for the TPA binder in the default ALC, and
        //    - CLRPrivBinderAssemblyLoadContext for custom ALCs.
        // in order obtain the associated ALC handle.
        INT_PTR ptrManagedAssemblyLoadContext = AreSameBinderInstance(pCurrentBinder, pDomain->GetTPABinderContext())
            ? ((CLRPrivBinderCoreCLR *)pCurrentBinder)->GetManagedAssemblyLoadContext()
            : ((CLRPrivBinderAssemblyLoadContext *)pCurrentBinder)->GetManagedAssemblyLoadContext();

        return ptrManagedAssemblyLoadContext;
    }

    NATIVE_LIBRARY_HANDLE LoadNativeLibraryViaAssemblyLoadContextEvent(Assembly * pAssembly, PCWSTR wszLibName)
    {
        STANDARD_VM_CONTRACT;

        INT_PTR ptrManagedAssemblyLoadContext = GetManagedAssemblyLoadContext(pAssembly);
        if (ptrManagedAssemblyLoadContext == NULL)
        {
            return NULL;
        }

        NATIVE_LIBRARY_HANDLE hmod = NULL;

        GCX_COOP();

        struct {
            STRINGREF DllName;
            OBJECTREF AssemblyRef;
        } gc = { NULL, NULL };

        GCPROTECT_BEGIN(gc);

        gc.DllName = StringObject::NewString(wszLibName);
        gc.AssemblyRef = pAssembly->GetExposedObject();

        // Prepare to invoke  System.Runtime.Loader.AssemblyLoadContext.ResolveUnmanagedDllUsingEvent method
        // While ResolveUnmanagedDllUsingEvent() could compute the AssemblyLoadContext using the AssemblyRef
        // argument, it will involve another pInvoke to the runtime. So AssemblyLoadContext is passed in
        // as an additional argument.
        PREPARE_NONVIRTUAL_CALLSITE(METHOD__ASSEMBLYLOADCONTEXT__RESOLVEUNMANAGEDDLLUSINGEVENT);
        DECLARE_ARGHOLDER_ARRAY(args, 3);
        args[ARGNUM_0] = STRINGREF_TO_ARGHOLDER(gc.DllName);
        args[ARGNUM_1] = OBJECTREF_TO_ARGHOLDER(gc.AssemblyRef);
        args[ARGNUM_2] = PTR_TO_ARGHOLDER(ptrManagedAssemblyLoadContext);

        // Make the call
        CALL_MANAGED_METHOD(hmod, NATIVE_LIBRARY_HANDLE, args);

        GCPROTECT_END();

        return hmod;
    }

    NATIVE_LIBRARY_HANDLE LoadNativeLibraryViaDllImportResolver(NDirectMethodDesc * pMD, LPCWSTR wszLibName)
    {
        STANDARD_VM_CONTRACT;

        if (pMD->GetModule()->IsSystem())
        {
            // Don't attempt to callback on Corelib itself.
            // The LoadLibrary callback stub is managed code that requires CoreLib
            return NULL;
        }

        DWORD dllImportSearchPathFlags;
        BOOL searchAssemblyDirectory;
        BOOL hasDllImportSearchPathFlags = GetDllImportSearchPathFlags(pMD, &dllImportSearchPathFlags, &searchAssemblyDirectory);
        dllImportSearchPathFlags |= searchAssemblyDirectory ? DLLIMPORTSEARCHPATH_ASSEMBLYDIRECTORY : 0;

        Assembly* pAssembly = pMD->GetMethodTable()->GetAssembly();
        NATIVE_LIBRARY_HANDLE handle = NULL;

        GCX_COOP();

        struct {
            STRINGREF libNameRef;
            OBJECTREF assemblyRef;
        } gc = { NULL, NULL };

        GCPROTECT_BEGIN(gc);

        gc.libNameRef = StringObject::NewString(wszLibName);
        gc.assemblyRef = pAssembly->GetExposedObject();

        PREPARE_NONVIRTUAL_CALLSITE(METHOD__NATIVELIBRARY__LOADLIBRARYCALLBACKSTUB);
        DECLARE_ARGHOLDER_ARRAY(args, 4);
        args[ARGNUM_0] = STRINGREF_TO_ARGHOLDER(gc.libNameRef);
        args[ARGNUM_1] = OBJECTREF_TO_ARGHOLDER(gc.assemblyRef);
        args[ARGNUM_2] = BOOL_TO_ARGHOLDER(hasDllImportSearchPathFlags);
        args[ARGNUM_3] = DWORD_TO_ARGHOLDER(dllImportSearchPathFlags);

         // Make the call
        CALL_MANAGED_METHOD(handle, NATIVE_LIBRARY_HANDLE, args);
        GCPROTECT_END();

        return handle;
    }

    // Try to load the module alongside the assembly where the PInvoke was declared.
    NATIVE_LIBRARY_HANDLE LoadFromPInvokeAssemblyDirectory(Assembly *pAssembly, LPCWSTR libName, DWORD flags, LoadLibErrorTracker *pErrorTracker)
    {
        STANDARD_VM_CONTRACT;

        NATIVE_LIBRARY_HANDLE hmod = NULL;

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
    NATIVE_LIBRARY_HANDLE LoadFromNativeDllSearchDirectories(LPCWSTR libName, DWORD flags, LoadLibErrorTracker *pErrorTracker)
    {
        STANDARD_VM_CONTRACT;

        NATIVE_LIBRARY_HANDLE hmod = NULL;
        AppDomain* pDomain = GetAppDomain();

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

#ifdef TARGET_UNIX
    const int MaxVariationCount = 4;
    void DetermineLibNameVariations(const WCHAR** libNameVariations, int* numberOfVariations, const SString& libName, bool libNameIsRelativePath)
    {
        // Supported lib name variations
        static auto NameFmt = W("%.0s%s%.0s");
        static auto PrefixNameFmt = W("%s%s%.0s");
        static auto NameSuffixFmt = W("%.0s%s%s");
        static auto PrefixNameSuffixFmt = W("%s%s%s");

        _ASSERTE(*numberOfVariations >= MaxVariationCount);

        int varCount = 0;
        if (!libNameIsRelativePath)
        {
            libNameVariations[varCount++] = NameFmt;
        }
        else
        {
            // We check if the suffix is contained in the name, because on Linux it is common to append
            // a version number to the library name (e.g. 'libicuuc.so.57').
            bool containsSuffix = false;
            SString::CIterator it = libName.Begin();
            if (libName.Find(it, PLATFORM_SHARED_LIB_SUFFIX_W))
            {
                it += COUNTOF(PLATFORM_SHARED_LIB_SUFFIX_W);
                containsSuffix = it == libName.End() || *it == (WCHAR)'.';
            }

            // If the path contains a path delimiter, we don't add a prefix
            it = libName.Begin();
            bool containsDelim = libName.Find(it, DIRECTORY_SEPARATOR_STR_W);

            if (containsSuffix)
            {
                libNameVariations[varCount++] = NameFmt;

                if (!containsDelim)
                    libNameVariations[varCount++] = PrefixNameFmt;

                libNameVariations[varCount++] = NameSuffixFmt;

                if (!containsDelim)
                    libNameVariations[varCount++] = PrefixNameSuffixFmt;
            }
            else
            {
                libNameVariations[varCount++] = NameSuffixFmt;

                if (!containsDelim)
                    libNameVariations[varCount++] = PrefixNameSuffixFmt;

                libNameVariations[varCount++] = NameFmt;

                if (!containsDelim)
                    libNameVariations[varCount++] = PrefixNameFmt;
            }
        }

        *numberOfVariations = varCount;
    }
#else // TARGET_UNIX
    const int MaxVariationCount = 2;
    void DetermineLibNameVariations(const WCHAR** libNameVariations, int* numberOfVariations, const SString& libName, bool libNameIsRelativePath)
    {
        // Supported lib name variations
        static auto NameFmt = W("%.0s%s%.0s");
        static auto NameSuffixFmt = W("%.0s%s%s");

        _ASSERTE(*numberOfVariations >= MaxVariationCount);

        int varCount = 0;

        // The purpose of following code is to workaround LoadLibrary limitation:
        // LoadLibrary won't append extension if filename itself contains '.'. Thus it will break the following scenario:
        // [DllImport("A.B")] // The full name for file is "A.B.dll". This is common code pattern for cross-platform PInvoke
        // The workaround for above scenario is to call LoadLibrary with "A.B" first, if it fails, then call LoadLibrary with "A.B.dll"
        auto it = libName.Begin();
        if (!libNameIsRelativePath ||
            !libName.Find(it, W('.')) ||
            libName.EndsWith(W(".")) ||
            libName.EndsWithCaseInsensitive(W(".dll")) ||
            libName.EndsWithCaseInsensitive(W(".exe")))
        {
            // Follow LoadLibrary rules in MSDN doc: https://msdn.microsoft.com/en-us/library/windows/desktop/ms684175(v=vs.85).aspx
            // If the string specifies a full path, the function searches only that path for the module.
            // If the string specifies a module name without a path and the file name extension is omitted, the function appends the default library extension .dll to the module name.
            // To prevent the function from appending .dll to the module name, include a trailing point character (.) in the module name string.
            libNameVariations[varCount++] = NameFmt;
        }
        else
        {
            libNameVariations[varCount++] = NameFmt;
            libNameVariations[varCount++] = NameSuffixFmt;
        }

        *numberOfVariations = varCount;
    }
#endif // TARGET_UNIX

    // Search for the library and variants of its name in probing directories.
    NATIVE_LIBRARY_HANDLE LoadNativeLibraryBySearch(Assembly *callingAssembly,
                                                    BOOL searchAssemblyDirectory, DWORD dllImportSearchPathFlags,
                                                    LoadLibErrorTracker * pErrorTracker, LPCWSTR wszLibName)
    {
        STANDARD_VM_CONTRACT;

        NATIVE_LIBRARY_HANDLE hmod = NULL;

#if defined(FEATURE_CORESYSTEM) && !defined(TARGET_UNIX)
        // Try to go straight to System32 for Windows API sets. This is replicating quick check from
        // the OS implementation of api sets.
        if (IsWindowsAPISet(wszLibName))
        {
            hmod = LocalLoadLibraryHelper(wszLibName, LOAD_LIBRARY_SEARCH_SYSTEM32, pErrorTracker);
            if (hmod != NULL)
            {
                return hmod;
            }
        }
#endif // FEATURE_CORESYSTEM && !TARGET_UNIX

        if (g_hostpolicy_embedded)
        {
#ifdef TARGET_WINDOWS
            if (wcscmp(wszLibName, W("hostpolicy.dll")) == 0)
            {
                return WszGetModuleHandle(NULL);
            }
#else
            if (wcscmp(wszLibName, W("libhostpolicy")) == 0)
            {
                return PAL_LoadLibraryDirect(NULL);
            }
#endif
        }

        AppDomain* pDomain = GetAppDomain();
        DWORD loadWithAlteredPathFlags = GetLoadWithAlteredSearchPathFlag();
        bool libNameIsRelativePath = Path::IsRelative(wszLibName);

        // P/Invokes are often declared with variations on the actual library name.
        // For example, it's common to leave off the extension/suffix of the library
        // even if it has one, or to leave off a prefix like "lib" even if it has one
        // (both of these are typically done to smooth over cross-platform differences).
        // We try to dlopen with such variations on the original.
        const WCHAR* prefixSuffixCombinations[MaxVariationCount] = {};
        int numberOfVariations = COUNTOF(prefixSuffixCombinations);
        DetermineLibNameVariations(prefixSuffixCombinations, &numberOfVariations, wszLibName, libNameIsRelativePath);
        for (int i = 0; i < numberOfVariations; i++)
        {
            SString currLibNameVariation;
            currLibNameVariation.Printf(prefixSuffixCombinations[i], PLATFORM_SHARED_LIB_PREFIX_W, wszLibName, PLATFORM_SHARED_LIB_SUFFIX_W);

            // NATIVE_DLL_SEARCH_DIRECTORIES set by host is considered well known path
            hmod = LoadFromNativeDllSearchDirectories(currLibNameVariation, loadWithAlteredPathFlags, pErrorTracker);
            if (hmod != NULL)
            {
                return hmod;
            }

            if (!libNameIsRelativePath)
            {
                DWORD flags = loadWithAlteredPathFlags;
                if ((dllImportSearchPathFlags & LOAD_LIBRARY_SEARCH_DLL_LOAD_DIR) != 0)
                {
                    // LOAD_LIBRARY_SEARCH_DLL_LOAD_DIR is the only flag affecting absolute path. Don't OR the flags
                    // unconditionally as all absolute path P/Invokes could then lose LOAD_WITH_ALTERED_SEARCH_PATH.
                    flags |= dllImportSearchPathFlags;
                }

                hmod = LocalLoadLibraryHelper(currLibNameVariation, flags, pErrorTracker);
                if (hmod != NULL)
                {
                    return hmod;
                }
            }
            else if ((callingAssembly != nullptr) && searchAssemblyDirectory)
            {
                hmod = LoadFromPInvokeAssemblyDirectory(callingAssembly, currLibNameVariation, loadWithAlteredPathFlags | dllImportSearchPathFlags, pErrorTracker);
                if (hmod != NULL)
                {
                    return hmod;
                }
            }

            hmod = LocalLoadLibraryHelper(currLibNameVariation, dllImportSearchPathFlags, pErrorTracker);
            if (hmod != NULL)
            {
                return hmod;
            }
        }

        // This may be an assembly name
        // Format is "fileName, assemblyDisplayName"
        MAKE_UTF8PTR_FROMWIDE(szLibName, wszLibName);
        char *szComma = strchr(szLibName, ',');
        if (szComma)
        {
            *szComma = '\0';
            // Trim white spaces
            while (COMCharacter::nativeIsWhiteSpace(*(++szComma)));

            AssemblySpec spec;
            if (SUCCEEDED(spec.Init(szComma)))
            {
                // Need to perform case insensitive hashing.
                SString moduleName(SString::Utf8, szLibName);
                moduleName.LowerCase();

                StackScratchBuffer buffer;
                szLibName = (LPSTR)moduleName.GetUTF8(buffer);

                Assembly *pAssembly = spec.LoadAssembly(FILE_LOADED);
                Module *pModule = pAssembly->FindModuleByName(szLibName);

                hmod = LocalLoadLibraryHelper(pModule->GetPath(), loadWithAlteredPathFlags | dllImportSearchPathFlags, pErrorTracker);
            }
        }

        return hmod;
    }

    NATIVE_LIBRARY_HANDLE LoadNativeLibraryBySearch(NDirectMethodDesc *pMD, LoadLibErrorTracker *pErrorTracker, PCWSTR wszLibName)
    {
        STANDARD_VM_CONTRACT;

        BOOL searchAssemblyDirectory;
        DWORD dllImportSearchPathFlags;

        GetDllImportSearchPathFlags(pMD, &dllImportSearchPathFlags, &searchAssemblyDirectory);

        Assembly *pAssembly = pMD->GetMethodTable()->GetAssembly();
        return LoadNativeLibraryBySearch(pAssembly, searchAssemblyDirectory, dllImportSearchPathFlags, pErrorTracker, wszLibName);
    }
}

// static
NATIVE_LIBRARY_HANDLE NDirect::LoadLibraryByName(LPCWSTR libraryName, Assembly *callingAssembly,
    BOOL hasDllImportSearchFlags, DWORD dllImportSearchFlags,
    BOOL throwOnError)
{
    CONTRACTL
    {
        STANDARD_VM_CHECK;
        PRECONDITION(CheckPointer(libraryName));
        PRECONDITION(CheckPointer(callingAssembly));
    }
    CONTRACTL_END;

    NATIVE_LIBRARY_HANDLE hmod = nullptr;

    // Resolve using the AssemblyLoadContext.LoadUnmanagedDll implementation
    hmod = LoadNativeLibraryViaAssemblyLoadContext(callingAssembly, libraryName);
    if (hmod != nullptr)
        return hmod;

    // Check if a default dllImportSearchPathFlags was passed in. If so, use that value.
    // Otherwise, check if the assembly has the DefaultDllImportSearchPathsAttribute attribute.
    // If so, use that value.
    BOOL searchAssemblyDirectory;
    DWORD dllImportSearchPathFlags;
    if (hasDllImportSearchFlags)
    {
        dllImportSearchPathFlags = dllImportSearchFlags & ~DLLIMPORTSEARCHPATH_ASSEMBLYDIRECTORY;
        searchAssemblyDirectory = dllImportSearchFlags & DLLIMPORTSEARCHPATH_ASSEMBLYDIRECTORY;

    }
    else
    {
        GetDllImportSearchPathFlags(callingAssembly->GetManifestModule(),
                                    &dllImportSearchPathFlags, &searchAssemblyDirectory);
    }

    LoadLibErrorTracker errorTracker;
    hmod = LoadNativeLibraryBySearch(callingAssembly, searchAssemblyDirectory, dllImportSearchPathFlags, &errorTracker, libraryName);
    if (hmod != nullptr)
        return hmod;

    // Resolve using the AssemblyLoadContext.ResolvingUnmanagedDll event
    hmod = LoadNativeLibraryViaAssemblyLoadContextEvent(callingAssembly, libraryName);
    if (hmod != nullptr)
        return hmod;

    if (throwOnError)
    {
        SString libraryPathSString(libraryName);
        errorTracker.Throw(libraryPathSString);
    }

    return hmod;
}

NATIVE_LIBRARY_HANDLE NDirect::LoadNativeLibrary(NDirectMethodDesc * pMD, LoadLibErrorTracker * pErrorTracker)
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

    PREFIX_ASSUME( name != NULL );
    MAKE_WIDEPTR_FROMUTF8( wszLibName, name );

    NativeLibraryHandleHolder hmod = LoadNativeLibraryViaDllImportResolver(pMD, wszLibName);
    if (hmod != NULL)
    {
        return hmod.Extract();
    }

    AppDomain* pDomain = GetAppDomain();
    Assembly* pAssembly = pMD->GetMethodTable()->GetAssembly();

    hmod = LoadNativeLibraryViaAssemblyLoadContext(pAssembly, wszLibName);
    if (hmod != NULL)
    {
        return hmod.Extract();
    }

    hmod = pDomain->FindUnmanagedImageInCache(wszLibName);
    if (hmod != NULL)
    {
       return hmod.Extract();
    }

    hmod = LoadNativeLibraryBySearch(pMD, pErrorTracker, wszLibName);
    if (hmod != NULL)
    {
        // If we have a handle add it to the cache.
        pDomain->AddUnmanagedImageToCache(wszLibName, hmod);
        return hmod.Extract();
    }

    hmod = LoadNativeLibraryViaAssemblyLoadContextEvent(pAssembly, wszLibName);
    if (hmod != NULL)
    {
        return hmod.Extract();
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

    {
        LPVOID pvTarget = (LPVOID)PInvokeOverride::GetMethodImpl(pMD->GetLibNameRaw(), pMD->GetEntrypointName());
        if (pvTarget != NULL)
        {
            pMD->SetNDirectTarget(pvTarget);
            return;
        }
    }

    LoadLibErrorTracker errorTracker;

    BOOL fSuccess = FALSE;
    NATIVE_LIBRARY_HANDLE hmod = LoadNativeLibrary( pMD, &errorTracker );
    if ( hmod )
    {
        LPVOID pvTarget = NDirectGetEntryPoint(pMD, hmod);
        if (pvTarget)
        {
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
        if (WszMultiByteToWideChar(CP_UTF8, 0, (LPCSTR)pMD->GetEntrypointName(), -1, wszEPName, sizeof(wszEPName)/sizeof(WCHAR)) == 0)
        {
            wszEPName[0] = W('?');
            wszEPName[1] = W('\0');
        }
#ifdef TARGET_UNIX
        COMPlusThrow(kEntryPointNotFoundException, IDS_EE_NDIRECT_GETPROCADDRESS_UNIX, ssLibName.GetUnicode(), wszEPName);
#else
        COMPlusThrow(kEntryPointNotFoundException, IDS_EE_NDIRECT_GETPROCADDRESS_WIN, ssLibName.GetUnicode(), wszEPName);
#endif
    }
}

void MarshalStructViaILStub(MethodDesc* pStubMD, void* pManagedData, void* pNativeData, StructMarshalStubs::MarshalOperation operation, void** ppCleanupWorkList /* = nullptr */)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(CheckPointer(pStubMD));
    }
    CONTRACTL_END;

    ARG_SLOT args[] =
    {
        PtrToArgSlot(pManagedData),
        PtrToArgSlot(pNativeData),
        (ARG_SLOT)operation,
        PtrToArgSlot(ppCleanupWorkList)
    };

    MethodDescCallSite callSite(pStubMD);

    callSite.Call(args);
}

void MarshalStructViaILStubCode(PCODE pStubCode, void* pManagedData, void* pNativeData, StructMarshalStubs::MarshalOperation operation, void** ppCleanupWorkList /* = nullptr */)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(pStubCode != NULL);
    }
    CONTRACTL_END;

    PREPARE_NONVIRTUAL_CALLSITE_USING_CODE(pStubCode);
    DECLARE_ARGHOLDER_ARRAY(args, 4);
    args[ARGNUM_0] = PTR_TO_ARGHOLDER(pManagedData);
    args[ARGNUM_1] = PTR_TO_ARGHOLDER(pNativeData);
    args[ARGNUM_2] = DWORD_TO_ARGHOLDER(operation);
    args[ARGNUM_3] = PTR_TO_ARGHOLDER(ppCleanupWorkList);

    CALL_MANAGED_METHOD_NORET(args);
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
    }
    CONTRACTL_END;

    INSTALL_MANAGED_EXCEPTION_DISPATCHER;
    // this function is called by CLR to native assembly stubs which are called by
    // managed code as a result, we need an unwind and continue handler to translate
    // any of our internal exceptions into managed exceptions.
    INSTALL_UNWIND_AND_CONTINUE_HANDLER;

    if (pMD->IsEarlyBound())
    {
        if (!pMD->IsZapped())
        {
            // we need the MD to be populated in case we decide to build an intercept
            // stub to wrap the target in InitEarlyBoundNDirectTarget
            PInvokeStaticSigInfo sigInfo;
            NDirect::PopulateNDirectMethodDesc(pMD, &sigInfo);
        }

        pMD->InitEarlyBoundNDirectTarget();
    }
    else
    {
        //
        // Otherwise we're in an inlined pinvoke late bound MD
        //
        INDEBUG(Thread *pThread = GetThread());
        {
            _ASSERTE(pMD->ShouldSuppressGCTransition()
                || pThread->GetFrame()->GetVTablePtr() == InlinedCallFrame::GetMethodFrameVPtr());

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
        BYTE callConv = MetaSig::GetCallingConvention(pVASigCookie->pModule, signature);

        // Unmanaged calling convention indicates modopt should be read
        if (callConv == IMAGE_CEE_CS_CALLCONV_UNMANAGED)
        {
            CorUnmanagedCallingConvention callConvMaybe;
            UINT errorResID;
            bool suppressGCTransition = false;
            HRESULT hr = MetaSig::TryGetUnmanagedCallingConventionFromModOpt(GetScopeHandle(pVASigCookie->pModule), signature.GetRawSig(), signature.GetRawSigLen(), &callConvMaybe, &suppressGCTransition, &errorResID);
            if (FAILED(hr))
                COMPlusThrowHR(hr, errorResID);

            if (hr == S_OK)
            {
                callConv = callConvMaybe;
            }
            else
            {
                callConv = MetaSig::GetDefaultUnmanagedCallingConvention();
            }

            if (suppressGCTransition)
            {
                dwStubFlags |= NDIRECTSTUB_FL_SUPPRESSGCTRANSITION;
            }
        }

        if (!TryConvertCallConvValueToPInvokeCallConv(callConv, &unmgdCallConv))
            COMPlusThrow(kTypeLoadException, IDS_INVALID_PINVOKE_CALLCONV);

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
// if it is larger than size. "..." will be appended if it is truncated.
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
