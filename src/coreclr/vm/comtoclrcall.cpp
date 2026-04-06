// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
// COM to CLR call support.
//

#include "common.h"
#include <limits>

#include "vars.hpp"
#include "clrtypes.h"
#include "stublink.h"
#include "excep.h"
#include "comtoclrcall.h"
#include "cgensys.h"
#include "method.hpp"
#include "siginfo.hpp"
#include "comcallablewrapper.h"
#include "field.h"
#include "virtualcallstub.h"
#include "dllimport.h"
#include "mlinfo.h"
#include "dbginterface.h"
#include "sigbuilder.h"
#include "callconvbuilder.hpp"
#include "comdelegate.h"
#include "finalizerthread.h"

void ComCallMethodDesc::InitMethod(MethodDesc *pMD, MethodDesc *pInterfaceMD)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(CheckPointer(pMD));
        PRECONDITION(!pMD->IsAsyncMethod());
    }
    CONTRACTL_END;

    m_flags = 0;

    m_pMD = pMD;
    m_pInterfaceMD = PTR_MethodDesc(pInterfaceMD);
    m_pILStub = NULL;

    // Initialize the native type information size of native stack, native retval flags, etc).
    InitNativeInfo();
}

void ComCallMethodDesc::InitField(FieldDesc* pFD, BOOL isGetter)
{
    CONTRACTL
    {
        STANDARD_VM_CHECK;
        PRECONDITION(CheckPointer(pFD));
    }
    CONTRACTL_END;

    m_pFD = pFD;
    m_pILStub = NULL;

    m_flags = enum_IsFieldCall; // mark the attribute as a field
    m_flags |= isGetter ? enum_IsGetter : 0;

    // Initialize the native type information size of native stack, native retval flags, etc).
    InitNativeInfo();
};

// Initialize the member's native type information (size of native stack, native retval flags, etc).
// It is unfortunate that we have to touch all this metadata at creation time. The reason for this
// is that we need to know size of the native stack to be able to return back to unmanaged code in
// case ComPrestub fails. If it fails because the target appdomain has already been unloaded, it is
// too late to make this computation - the metadata is no longer available.
void ComCallMethodDesc::InitNativeInfo()
{
    CONTRACT_VOID
    {
        STANDARD_VM_CHECK;
        PRECONDITION(!IsNativeInfoInitialized());
    }
    CONTRACT_END;

    EX_TRY
    {
#ifdef TARGET_X86
        m_StackBytes = (UINT16)-1;
        // On x86, this method has to compute size of arguments because we need to know size of the native stack
        // to be able to return back to unmanaged code
        UINT16 nativeArgSize;
#endif

        if (IsFieldCall())
        {
            FieldDesc          *pFD = GetFieldDesc();
            _ASSERTE(pFD != NULL);

#ifdef _DEBUG
            LPCUTF8             szDebugName = pFD->GetDebugName();
            LPCUTF8             szDebugClassName = pFD->GetEnclosingMethodTable()->GetDebugClassName();

            if (g_pConfig->ShouldBreakOnComToClrNativeInfoInit(szDebugName))
                CONSISTENCY_CHECK_MSGF(false, ("BreakOnComToClrNativeInfoInit: '%s' ", szDebugName));
#endif // _DEBUG

            MetaSig fsig(pFD);
            fsig.NextArg();

            // Look up the best fit mapping info via Assembly & Interface level attributes
            BOOL BestFit = TRUE;
            BOOL ThrowOnUnmappableChar = FALSE;
            ReadBestFitCustomAttribute(fsig.GetModule(), pFD->GetEnclosingMethodTable()->GetCl(), &BestFit, &ThrowOnUnmappableChar);

            MarshalInfo info(fsig.GetModule(), fsig.GetArgProps(), fsig.GetSigTypeContext(), pFD->GetMemberDef(), MarshalInfo::MARSHAL_SCENARIO_COMINTEROP,
                             (CorNativeLinkType)0, (CorNativeLinkFlags)0,
                             FALSE, 0, fsig.NumFixedArgs(), BestFit, ThrowOnUnmappableChar, FALSE, NULL, FALSE
#ifdef _DEBUG
                             , szDebugName, szDebugClassName, 0
#endif
                             );

            if (info.GetMarshalType() == MarshalInfo::MARSHAL_TYPE_UNKNOWN)
                info.ThrowTypeLoadExceptionForInvalidFieldMarshal(pFD, info.GetErrorResourceId());

#ifdef TARGET_X86
            if (IsFieldGetter())
            {
                // getter takes 'this' and the output argument by-ref
                nativeArgSize = sizeof(void *) + sizeof(void *);
            }
            else
            {
                info.SetupArgumentSizes();

                // setter takes 'this' and the input argument by-value
                nativeArgSize = sizeof(void *) + info.GetNativeArgSize();
            }
#endif // TARGET_X86

            // Field calls always return HRESULTs.
            m_flags |= enum_NativeHResultRetVal;
        }
        else
        {
            MethodDesc *pMD = GetCallMethodDesc();
            _ASSERTE(!pMD->IsAsyncMethod()); // Async methods should never have a ComCallMethodDesc.

#ifdef _DEBUG
            LPCUTF8         szDebugName = pMD->m_pszDebugMethodName;
            LPCUTF8         szDebugClassName = pMD->m_pszDebugClassName;

            if (g_pConfig->ShouldBreakOnComToClrNativeInfoInit(szDebugName))
                CONSISTENCY_CHECK_MSGF(false, ("BreakOnComToClrNativeInfoInit: '%s' ", szDebugName));
#endif // _DEBUG

            MethodTable * pMT = pMD->GetMethodTable();
            IMDInternalImport * pInternalImport = pMT->GetMDImport();

            mdMethodDef md = pMD->GetMemberDef();

            ULONG ulCodeRVA;
            DWORD dwImplFlags;
            IfFailThrow(pInternalImport->GetMethodImplProps(md, &ulCodeRVA, &dwImplFlags));

            // Determine if we need to do HRESULT munging for this method.
            BOOL fPreserveSig = IsMiPreserveSig(dwImplFlags);

#ifndef TARGET_X86
            if (!fPreserveSig)
            {
                // PreserveSig=false methods always return HRESULTs.
                m_flags |= enum_NativeHResultRetVal;
                goto Done;
            }
#endif

            MetaSig msig(pMD);

#ifndef TARGET_X86
            if (msig.IsReturnTypeVoid())
            {
                // The method has a void return type on the native side.
                m_flags |= enum_NativeVoidRetVal;
                goto Done;
            }
#endif

            // Look up the best fit mapping info via Assembly & Interface level attributes
            BOOL BestFit = TRUE;
            BOOL ThrowOnUnmappableChar = FALSE;
            ReadBestFitCustomAttribute(pMD, &BestFit, &ThrowOnUnmappableChar);

            int numArgs = msig.NumFixedArgs();

            // Collects ParamDef information in an indexed array where element 0 represents
            // the return type.
            mdParamDef *params = (mdParamDef*)_alloca((numArgs+1) * sizeof(mdParamDef));
            CollateParamTokens(pInternalImport, md, numArgs, params);

#ifdef TARGET_X86
            // If this is a method call then check to see if we need to do LCID conversion.
            int iLCIDArg = GetLCIDParameterIndex(pMD);
            if (iLCIDArg != -1)
                iLCIDArg++;

            nativeArgSize = sizeof(void*);

            int iArg = 1;
            CorElementType mtype;
            while (ELEMENT_TYPE_END != (mtype = msig.NextArg()))
            {
                // Check to see if this is the parameter after which we need to read the LCID from.
                if (iArg == iLCIDArg)
                    nativeArgSize += (UINT16)StackElemSize(sizeof(LCID));

                MarshalInfo info(msig.GetModule(), msig.GetArgProps(), msig.GetSigTypeContext(), params[iArg],
                                 MarshalInfo::MARSHAL_SCENARIO_COMINTEROP,
                                 (CorNativeLinkType)0, (CorNativeLinkFlags)0,
                                 TRUE, iArg, numArgs, BestFit, ThrowOnUnmappableChar, FALSE, pMD, FALSE
#ifdef _DEBUG
                                 , szDebugName, szDebugClassName, iArg
#endif
                                 );

                if (info.GetMarshalType() == MarshalInfo::MARSHAL_TYPE_UNKNOWN)
                {
                    nativeArgSize += (UINT16)StackElemSize(sizeof(LPVOID));
                    m_flags |= enum_HasMarshalError;
                }
                else
                {
                    info.SetupArgumentSizes();

                    nativeArgSize += info.GetNativeArgSize();
                }

                ++iArg;
            }

            // Check to see if this is the parameter after which we need to read the LCID from.
            if (iArg == iLCIDArg)
                nativeArgSize += (UINT16)StackElemSize(sizeof(LCID));
#endif // TARGET_X86


            //
            // Return value
            //

#ifndef TARGET_X86
            // Handled above
            _ASSERTE(!msig.IsReturnTypeVoid());
#else
            if (msig.IsReturnTypeVoid())
            {
                if (!fPreserveSig)
                {
                    // PreserveSig=false methods always return HRESULTs.
                    m_flags |= enum_NativeHResultRetVal;
                }
                else
                {
                    // The method has a void return type on the native side.
                    m_flags |= enum_NativeVoidRetVal;
                }

                goto Done;
            }
#endif // TARGET_X86

            {
                MarshalInfo info(msig.GetModule(), msig.GetReturnProps(), msig.GetSigTypeContext(), params[0],
                                    MarshalInfo::MARSHAL_SCENARIO_COMINTEROP,
                                    (CorNativeLinkType)0, (CorNativeLinkFlags)0,
                                    FALSE, 0, numArgs, BestFit, ThrowOnUnmappableChar, FALSE, pMD, FALSE
#ifdef _DEBUG
                                ,szDebugName, szDebugClassName, 0
#endif
                );

#ifndef TARGET_X86
                // Handled above
                _ASSERTE(fPreserveSig);
#else
                if (!fPreserveSig)
                {
                    // PreserveSig=false methods always return HRESULTs.
                    m_flags |= enum_NativeHResultRetVal;

                    // count the output by-ref argument
                    nativeArgSize += sizeof(void *);

                    goto Done;
                }
#endif // TARGET_X86

                // Ignore the secret return buffer argument - we don't allow returning
                // structures by value in COM interop.
                if (info.IsFpuReturn())
                {
                    if (info.GetMarshalType() == MarshalInfo::MARSHAL_TYPE_FLOAT)
                    {
                        m_flags |= enum_NativeR4Retval;
                    }
                    else
                    {
                        _ASSERTE(info.GetMarshalType() == MarshalInfo::MARSHAL_TYPE_DOUBLE);
                        m_flags |= enum_NativeR8Retval;
                    }
                }
                else
                {
                    CorElementType returnType = msig.GetReturnType();
                    if (returnType == ELEMENT_TYPE_I4 || returnType == ELEMENT_TYPE_U4)
                    {
                        // If the method is PreserveSig=true and returns either an I4 or an U4, then we
                        // will assume the users wants to return an HRESULT in case of failure.
                        m_flags |= enum_NativeHResultRetVal;
                    }
                    else if (info.GetMarshalType() == MarshalInfo::MARSHAL_TYPE_DATE)
                    {
                        // DateTime is returned as an OLEAUT DATE which is actually an R8.
                        m_flags |= enum_NativeR8Retval;
                    }
                    else
                    {
                        // The method doesn't return an FP value nor should we treat it as returning
                        // an HRESULT so we will return 0 in case of failure.
                        m_flags |= enum_NativeBoolRetVal;
                    }
                }
            }
        }

Done:

#ifdef TARGET_X86
        // The above algorithm to compute nativeArgSize is x86-specific. We will compute
        // the correct value later for other platforms.
        m_StackBytes = nativeArgSize;
#endif

        m_flags |= enum_NativeInfoInitialized;
    }
    EX_SWALLOW_NONTRANSIENT
    RETURN;
}

namespace
{
    void PopulateComCallMethodDesc(ComCallMethodDesc *pCMD, DWORD *pdwStubFlags)
    {
        CONTRACTL
        {
            STANDARD_VM_CHECK;
            PRECONDITION(CheckPointer(pCMD));
            PRECONDITION(CheckPointer(pdwStubFlags));
        }
        CONTRACTL_END;

        DWORD dwStubFlags = PINVOKESTUB_FL_COM | PINVOKESTUB_FL_REVERSE_INTEROP;

        BOOL BestFit               = TRUE;
        BOOL ThrowOnUnmappableChar = FALSE;

        if (pCMD->IsFieldCall())
        {
            if (pCMD->IsFieldGetter())
                dwStubFlags |= PINVOKESTUB_FL_FIELDGETTER;
            else
                dwStubFlags |= PINVOKESTUB_FL_FIELDSETTER;

            FieldDesc *pFD = pCMD->GetFieldDesc();
            _ASSERTE(IsMemberVisibleFromCom(pFD->GetApproxEnclosingMethodTable(), pFD->GetMemberDef(), mdTokenNil) && "Calls are not permitted on this member since it isn't visible from COM. The only way you can have reached this code path is if your native interface doesn't match the managed interface.");

            MethodTable *pMT = pFD->GetEnclosingMethodTable();
            ReadBestFitCustomAttribute(pMT->GetModule(), pMT->GetCl(), &BestFit, &ThrowOnUnmappableChar);
        }
        else
        {
            MethodDesc *pMD = pCMD->GetCallMethodDesc();
            _ASSERTE(IsMethodVisibleFromCom(pMD) && "Calls are not permitted on this member since it isn't visible from COM. The only way you can have reached this code path is if your native interface doesn't match the managed interface.");

            ReadBestFitCustomAttribute(pMD, &BestFit, &ThrowOnUnmappableChar);
        }

        if (BestFit)
            dwStubFlags |= PINVOKESTUB_FL_BESTFIT;

        if (ThrowOnUnmappableChar)
            dwStubFlags |= PINVOKESTUB_FL_THROWONUNMAPPABLECHAR;

        //
        // fill in out param
        //
        *pdwStubFlags = dwStubFlags;
    }

    MethodDesc* GetILStubMethodDesc(MethodDesc *pCallMD, DWORD dwStubFlags)
    {
        CONTRACTL
        {
            STANDARD_VM_CHECK;
            PRECONDITION(CheckPointer(pCallMD));
            PRECONDITION(SF_IsReverseCOMStub(dwStubFlags));
        }
        CONTRACTL_END;

        // Get the call signature information
        StubSigDesc sigDesc(pCallMD);

        return PInvoke::CreateCLRToNativeILStub(&sigDesc,
                                                (CorNativeLinkType)0,
                                                (CorNativeLinkFlags)0,
                                                CallConv::GetDefaultUnmanagedCallingConvention(),
                                                dwStubFlags);
    }

    MethodDesc* GetILStubMethodDesc(FieldDesc *pFD, DWORD dwStubFlags)
    {
        CONTRACTL
        {
            STANDARD_VM_CHECK;
            PRECONDITION(CheckPointer(pFD));
            PRECONDITION(SF_IsFieldGetterStub(dwStubFlags) || SF_IsFieldSetterStub(dwStubFlags));
        }
        CONTRACTL_END;

        PCCOR_SIGNATURE pSig;
        DWORD           cSig;

        // Get the field signature information
        pFD->GetSig(&pSig, &cSig);

        return PInvoke::CreateFieldAccessILStub(pSig,
                                                cSig,
                                                pFD->GetModule(),
                                                pFD->GetMemberDef(),
                                                dwStubFlags,
                                                pFD);
    }
}

PCODE ComCallMethodDesc::CreateCOMToCLRStub(DWORD dwStubFlags, MethodDesc **ppStubMD)
{
    CONTRACT(PCODE)
    {
        STANDARD_VM_CHECK;
        PRECONDITION(CheckPointer(ppStubMD));
        POSTCONDITION(CheckPointer(*ppStubMD));
        POSTCONDITION(RETVAL != NULL);
    }
    CONTRACT_END;

    MethodDesc * pStubMD;

    if (IsFieldCall())
    {
        FieldDesc *pFD = GetFieldDesc();
        pStubMD = GetILStubMethodDesc(pFD, dwStubFlags);
    }
    else
    {
        // if this represents a ctor or static, use the class method (i.e. the actual ctor or static)
        MethodDesc *pMD = GetCallMethodDesc();
        pStubMD = GetILStubMethodDesc(pMD, dwStubFlags);
    }

    *ppStubMD = pStubMD;

    _ASSERTE(pStubMD->IsILStub());

#ifdef TARGET_X86
    // make sure our native stack computation in code:ComCallMethodDesc.InitNativeInfo is right
    _ASSERTE(HasMarshalError() || !pStubMD->IsILStub() || pStubMD->AsDynamicMethodDesc()->GetNativeStackArgSize() == m_StackBytes);
    m_StackBytes = pStubMD->AsDynamicMethodDesc()->GetNativeStackArgSize();
#endif // TARGET_X86

    RETURN JitILStub(pStubMD);
}

PLATFORM_THREAD_LOCAL HRESULT t_ComPreStubLastHResult;

#ifdef TARGET_X86
PLATFORM_THREAD_LOCAL UINT t_ComPreStubLastStackBytes;

extern "C" HRESULT __stdcall ComPreStubGetLastHResult()
{
    LIMITED_METHOD_CONTRACT;
    return t_ComPreStubLastHResult;
}

extern "C" UINT __stdcall ComPreStubGetLastStackBytes()
{
    LIMITED_METHOD_CONTRACT;
    return t_ComPreStubLastStackBytes;
}

extern "C" int ComStubReturnHResult();

extern "C" BOOL ComStubReturnBool();

extern "C" float ComStubReturnR4NaN();

extern "C" double ComStubReturnR8NaN();

extern "C" void ComStubReturnVoid();
#else
namespace
{
    int ComStubReturnHResult()
    {
        LIMITED_METHOD_CONTRACT;
        return t_ComPreStubLastHResult;
    }

    BOOL ComStubReturnBool()
    {
        LIMITED_METHOD_CONTRACT;
        return FALSE;
    }

    float ComStubReturnR4NaN()
    {
        LIMITED_METHOD_CONTRACT;
        // COMPAT: Use -qNaN as our canonical NaN value.
        return -std::numeric_limits<float>::quiet_NaN();
    }

    double ComStubReturnR8NaN()
    {
        LIMITED_METHOD_CONTRACT;
        // COMPAT: Use -qNaN as our canonical NaN value.
        return -std::numeric_limits<double>::quiet_NaN();
    }

    void ComStubReturnVoid()
    {
        LIMITED_METHOD_CONTRACT;
        return;
    }
}
#endif

namespace
{
    PCODE GetReturnStubForComCallMethodDesc(ComCallMethodDesc *pCMD, HRESULT hr)
    {
        LIMITED_METHOD_CONTRACT;

#ifdef TARGET_X86
            t_ComPreStubLastStackBytes = pCMD->GetNumStackBytes();
#endif

        if (pCMD->IsNativeHResultRetVal())
        {
            t_ComPreStubLastHResult = hr;
            return (PCODE)&ComStubReturnHResult;
        }
        else if (pCMD->IsNativeBoolRetVal())
        {
            return (PCODE)&ComStubReturnBool;
        }
        else if (pCMD->IsNativeR4RetVal())
        {
            return (PCODE)&ComStubReturnR4NaN;
        }
        else if (pCMD->IsNativeR8RetVal())
        {
            return (PCODE)&ComStubReturnR8NaN;
        }
        else
        {
            return (PCODE)&ComStubReturnVoid;
        }
    }
}

PCODE ComCallUMThunkMarshInfo::GetReturnStubForHResult(HRESULT hr)
{
    LIMITED_METHOD_CONTRACT;

    return GetReturnStubForComCallMethodDesc(m_pCMD, hr);
}

PCODE ComCallUMThunkMarshInfo::RunTimeInit(bool* pCanSkipPreStub)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    // We can't skip the prestub as we need to ensure that we always
    // return E_OUTOFMEMORY if we are called on a new thread and can't set up
    // the managed thread object.
    *pCanSkipPreStub = FALSE;

    if (IsCompletelyInited())
    {
        // The stub is already set up, so we can just return.
        return GetILStubEntry();
    }

    PCODE pStub = NULL;

    // Transition to cooperative GC mode before we start setting up the stub.
    GCX_COOP();

    OBJECTREF pThrowable = NULL;
    GCPROTECT_BEGIN(pThrowable)
    {
        EX_TRY
        {
            GCX_PREEMP();

            DWORD             dwStubFlags;

            PopulateComCallMethodDesc(m_pCMD, &dwStubFlags);

            MethodDesc *pStubMD;
            pStub = SetILStubEntry(m_pCMD->CreateCOMToCLRStub(dwStubFlags, &pStubMD));
        }
        EX_CATCH
        {
            pThrowable = GET_THROWABLE();
        }
        EX_END_CATCH

        if (pThrowable != NULL)
        {
            // Transform the exception into an HRESULT. This also sets up
            // an IErrorInfo on the current thread for the exception.
            HRESULT hr = SetupErrorInfo(pThrowable);
            pThrowable = NULL;

            return GetReturnStubForComCallMethodDesc(m_pCMD, hr);
        }
    }
    GCPROTECT_END();

    return pStub;
}
