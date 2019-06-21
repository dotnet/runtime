// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// 
// File: ILMarshalers.h
// 

//


#include "common.h"
#ifdef FEATURE_COMINTEROP
#include "winstring.h"
#endif //FEATURE_COMINTEROP
#include "stubgen.h"
#include "binder.h"
#include "marshalnative.h"
#include "clrvarargs.h"
#ifdef FEATURE_COMINTEROP
#include "stdinterfaces.h"
#endif

#define LOCAL_NUM_UNUSED ((DWORD)-1)

class ILStubMarshalHome
{
public:
    typedef enum 
    {
        HomeType_Unspecified     = 0,
        HomeType_ILLocal         = 1,
        HomeType_ILArgument      = 2,
        HomeType_ILByrefLocal    = 3,
        HomeType_ILByrefArgument = 4
    } MarshalHomeType;

private:
    MarshalHomeType     m_homeType;
    DWORD               m_dwHomeIndex;
    
public:
    void InitHome(MarshalHomeType homeType, DWORD dwHomeIndex)
    {
        LIMITED_METHOD_CONTRACT;
        m_homeType = homeType;
        m_dwHomeIndex = dwHomeIndex;
    }
        
    void EmitLoadHome(ILCodeStream* pslILEmit)
    {
        CONTRACTL
        {
            THROWS;
            GC_TRIGGERS;
            MODE_ANY;
        }
        CONTRACTL_END;

        switch (m_homeType)
        {
            case HomeType_ILLocal:      pslILEmit->EmitLDLOC(m_dwHomeIndex); break;
            case HomeType_ILArgument:   pslILEmit->EmitLDARG(m_dwHomeIndex); break;
        
            default:
                UNREACHABLE_MSG("unexpected homeType passed to EmitLoadHome");
                break;
        }
    }

    void EmitLoadHomeAddr(ILCodeStream* pslILEmit)
    {
        CONTRACTL
        {
            THROWS;
            GC_TRIGGERS;
            MODE_ANY;
        }
        CONTRACTL_END;
        
        switch (m_homeType)
        {
            case HomeType_ILLocal:         pslILEmit->EmitLDLOCA(m_dwHomeIndex); break;
            case HomeType_ILArgument:      pslILEmit->EmitLDARGA(m_dwHomeIndex); break;
            case HomeType_ILByrefLocal:    pslILEmit->EmitLDLOC(m_dwHomeIndex);  break;
            case HomeType_ILByrefArgument: pslILEmit->EmitLDARG(m_dwHomeIndex);  break;

            default:
                UNREACHABLE_MSG("unexpected homeType passed to EmitLoadHomeAddr");
                break;
        }
    }        

    void EmitStoreHome(ILCodeStream* pslILEmit)
    {
        CONTRACTL
        {
            THROWS;
            GC_TRIGGERS;
            MODE_ANY;
        }
        CONTRACTL_END;
        
        switch (m_homeType)
        {
            case HomeType_ILLocal:      pslILEmit->EmitSTLOC(m_dwHomeIndex); break;
            case HomeType_ILArgument:   pslILEmit->EmitSTARG(m_dwHomeIndex); break;

            default:
                UNREACHABLE_MSG("unexpected homeType passed to EmitStoreHome");
                break;
        }
    }

    void EmitStoreHomeAddr(ILCodeStream* pslILEmit)
    {
        CONTRACTL
        {
            THROWS;
            GC_TRIGGERS;
            MODE_ANY;
        }
        CONTRACTL_END;
        
        switch (m_homeType)
        {
            case HomeType_ILByrefLocal:    pslILEmit->EmitSTLOC(m_dwHomeIndex); break;
            case HomeType_ILByrefArgument: pslILEmit->EmitSTARG(m_dwHomeIndex); break;

            default:
                UNREACHABLE_MSG("unexpected homeType passed to EmitStoreHomeAddr");
                break;
        }
    }

    void EmitCopyFromByrefArg(ILCodeStream* pslILEmit, LocalDesc* pManagedType, DWORD argidx)
    {
        CONTRACTL
        {
            THROWS;
            GC_TRIGGERS;
            MODE_ANY;
        }
        CONTRACTL_END;
        
        if (pManagedType->IsValueClass())
        {
            EmitLoadHomeAddr(pslILEmit);    // dest
            pslILEmit->EmitLDARG(argidx);   // src
            pslILEmit->EmitCPOBJ(pslILEmit->GetToken(pManagedType->InternalToken));
        }
        else
        {
            pslILEmit->EmitLDARG(argidx);
            pslILEmit->EmitLDIND_T(pManagedType);
            EmitStoreHome(pslILEmit);
        }
    }

    void EmitCopyToByrefArg(ILCodeStream* pslILEmit, LocalDesc* pManagedType, DWORD argidx)
    {
        CONTRACTL
        {
            THROWS;
            GC_TRIGGERS;
            MODE_ANY;
        }
        CONTRACTL_END;

        if (pManagedType->IsValueClass())
        {
            pslILEmit->EmitLDARG(argidx);   // dest
            EmitLoadHomeAddr(pslILEmit);    // src
            pslILEmit->EmitCPOBJ(pslILEmit->GetToken(pManagedType->InternalToken));
        }
        else
        {
            pslILEmit->EmitLDARG(argidx);
            EmitLoadHome(pslILEmit);
            pslILEmit->EmitSTIND_T(pManagedType);
        }
    }

    void EmitCopyToByrefArgWithNullCheck(ILCodeStream* pslILEmit, LocalDesc* pManagedType, DWORD argidx)
    {
        CONTRACTL
        {
            THROWS;
            GC_TRIGGERS;
            MODE_ANY;
        }
        CONTRACTL_END;

        ILCodeLabel* pNullRefLabel = pslILEmit->NewCodeLabel();

        // prevent null-ref exception by an explicit check
        pslILEmit->EmitLDARG(argidx);
        pslILEmit->EmitBRFALSE(pNullRefLabel);

        EmitCopyToByrefArg(pslILEmit, pManagedType, argidx);

        pslILEmit->EmitLabel(pNullRefLabel);
    }
};
        

class ILMarshaler 
{
protected:

#ifdef _DEBUG
    const static UINT   s_cbStackAllocThreshold = 128;
#else
    const static UINT   s_cbStackAllocThreshold = 2048;
#endif // _DEBUG

    OverrideProcArgs*   m_pargs;
    NDirectStubLinker*  m_pslNDirect;
    ILCodeStream*       m_pcsMarshal;
    ILCodeStream*       m_pcsUnmarshal;
    UINT                m_argidx;

    DWORD               m_dwMarshalFlags;

    ILStubMarshalHome   m_nativeHome;
    ILStubMarshalHome   m_managedHome;

    DWORD               m_dwMngdMarshalerLocalNum;

public:

    ILMarshaler() : 
        m_pslNDirect(NULL)
    {
    }

    virtual ~ILMarshaler()
    {
        LIMITED_METHOD_CONTRACT;
    }

    void SetNDirectStubLinker(NDirectStubLinker* pslNDirect)
    {
        LIMITED_METHOD_CONTRACT;
        CONSISTENCY_CHECK(NULL == m_pslNDirect);
        m_pslNDirect = pslNDirect;
    }

    void Init(ILCodeStream* pcsMarshal, 
            ILCodeStream* pcsUnmarshal,
            UINT argidx,
            DWORD dwMarshalFlags, 
            OverrideProcArgs* pargs)
    {
        LIMITED_METHOD_CONTRACT;
        CONSISTENCY_CHECK_MSG(m_pslNDirect != NULL, "please call SetNDirectStubLinker() before EmitMarshalArgument or EmitMarshalReturnValue");
        m_pcsMarshal = pcsMarshal;
        m_pcsUnmarshal = pcsUnmarshal;
        m_pargs = pargs;
        m_dwMarshalFlags = dwMarshalFlags;
        m_argidx = argidx;
        m_dwMngdMarshalerLocalNum = -1;
    }

protected:
    static inline bool IsCLRToNative(DWORD dwMarshalFlags)
    {
        LIMITED_METHOD_CONTRACT;
        return (0 != (dwMarshalFlags & MARSHAL_FLAG_CLR_TO_NATIVE));
    }
        
    static inline bool IsIn(DWORD dwMarshalFlags)
    {
        LIMITED_METHOD_CONTRACT;
        return (0 != (dwMarshalFlags & MARSHAL_FLAG_IN));
    }

    static inline bool IsOut(DWORD dwMarshalFlags)
    {
        LIMITED_METHOD_CONTRACT;
        return (0 != (dwMarshalFlags & MARSHAL_FLAG_OUT));
    }

    static inline bool IsByref(DWORD dwMarshalFlags)
    {
        LIMITED_METHOD_CONTRACT;
        return (0 != (dwMarshalFlags & MARSHAL_FLAG_BYREF));
    }

    static inline bool IsHresultSwap(DWORD dwMarshalFlags)
    {
        LIMITED_METHOD_CONTRACT;
        return (0 != (dwMarshalFlags & MARSHAL_FLAG_HRESULT_SWAP));
    }

    static inline bool IsRetval(DWORD dwMarshalFlags)
    {
        LIMITED_METHOD_CONTRACT;
        return (0 != (dwMarshalFlags & MARSHAL_FLAG_RETVAL));
    }

    static inline bool IsHiddenLengthParam(DWORD dwMarshalFlags)
    {
        LIMITED_METHOD_CONTRACT;
        return (0 != (dwMarshalFlags & MARSHAL_FLAG_HIDDENLENPARAM));
    }

    void EmitLoadManagedValue(ILCodeStream* pslILEmit)
    {
        WRAPPER_NO_CONTRACT;
        m_managedHome.EmitLoadHome(pslILEmit);
    }

    void EmitLoadNativeValue(ILCodeStream* pslILEmit)
    {
        WRAPPER_NO_CONTRACT;
        m_nativeHome.EmitLoadHome(pslILEmit);
    }

    void EmitLoadManagedHomeAddr(ILCodeStream* pslILEmit)
    {
        WRAPPER_NO_CONTRACT;
        m_managedHome.EmitLoadHomeAddr(pslILEmit);
    }

    void EmitLoadNativeHomeAddr(ILCodeStream* pslILEmit)
    {
        WRAPPER_NO_CONTRACT;
        m_nativeHome.EmitLoadHomeAddr(pslILEmit);
    }
        
    void EmitStoreManagedValue(ILCodeStream* pslILEmit)
    {
        WRAPPER_NO_CONTRACT;
        m_managedHome.EmitStoreHome(pslILEmit);
    }

    void EmitStoreManagedHomeAddr(ILCodeStream* pslILEmit)
    {
        WRAPPER_NO_CONTRACT;
        m_managedHome.EmitStoreHomeAddr(pslILEmit);
    }

    void EmitStoreNativeValue(ILCodeStream* pslILEmit)
    {
        WRAPPER_NO_CONTRACT;
        m_nativeHome.EmitStoreHome(pslILEmit);
    }

    void EmitStoreNativeHomeAddr(ILCodeStream* pslILEmit)
    {
        WRAPPER_NO_CONTRACT;
        m_nativeHome.EmitStoreHomeAddr(pslILEmit);
    }

public:

    virtual bool SupportsArgumentMarshal(DWORD dwMarshalFlags, UINT* pErrorResID)
    {
        LIMITED_METHOD_CONTRACT;
        return true;
    }

    virtual bool SupportsReturnMarshal(DWORD dwMarshalFlags, UINT* pErrorResID)
    {
        LIMITED_METHOD_CONTRACT;
        return true;
    }

    // True if marshaling creates data that could need cleanup.
    bool NeedsMarshalCleanupIndex()
    {
        WRAPPER_NO_CONTRACT;
        return (NeedsClearNative() || NeedsClearCLR());
    }

    // True if unmarshaling creates data that could need exception cleanup ("rollback").
    bool NeedsUnmarshalCleanupIndex()
    {
        WRAPPER_NO_CONTRACT;
        return (NeedsClearNative() && !IsCLRToNative(m_dwMarshalFlags));
    }

    void EmitMarshalArgument(
                ILCodeStream*   pcsMarshal, 
                ILCodeStream*   pcsUnmarshal, 
                UINT            argidx, 
                DWORD           dwMarshalFlags,
                OverrideProcArgs*  pargs)
    {
        STANDARD_VM_CONTRACT;

        Init(pcsMarshal, pcsUnmarshal, argidx, dwMarshalFlags, pargs);

        // We could create the marshaler in the marshal stream right before it's needed (i.e. within the try block),
        // or in the setup stream (outside of the try block). For managed-to-unmanaged marshaling it does not actually
        // make much difference except that using setup stream saves us from cleaning up already-marshaled arguments
        // in case of an exception. For unmanaged-to-managed, we may need to perform cleanup of the incoming arguments
        // before we were able to marshal them. Therefore this must not happen within the try block so we don't try
        // to use marshalers that have not been initialized. Potentially leaking unmanaged resources is by-design and
        // there's not much we can do about it (we cannot do cleanup if we cannot create the marshaler).
        EmitCreateMngdMarshaler(m_pslNDirect->GetSetupCodeStream());

        if (IsCLRToNative(dwMarshalFlags))
        {
            if (IsByref(dwMarshalFlags))
            {
                EmitMarshalArgumentCLRToNativeByref();
            }
            else
            {
                EmitMarshalArgumentCLRToNative();
            }
        }
        else
        {
            if (IsByref(dwMarshalFlags))
            {
                EmitMarshalArgumentNativeToCLRByref();
            }
            else
            {
                EmitMarshalArgumentNativeToCLR();
            }
        }
    }

#ifdef FEATURE_COMINTEROP
    void EmitMarshalHiddenLengthArgument(ILCodeStream *pcsMarshal,
                                         ILCodeStream *pcsUnmarshal,
                                         MarshalInfo *pArrayInfo,
                                         UINT arrayIndex,
                                         DWORD dwMarshalFlags,
                                         UINT hiddenArgIndex,
                                         OverrideProcArgs *pargs,
                                         __out DWORD *pdwHiddenLengthManagedHomeLocal,
                                         __out DWORD *pdwHiddenLengthNativeHomeLocal)
    {
        CONTRACTL
        {
            STANDARD_VM_CHECK;
            PRECONDITION(IsHiddenLengthParam(dwMarshalFlags));
        }
        CONTRACTL_END;

        Init(pcsMarshal, pcsUnmarshal, hiddenArgIndex, dwMarshalFlags, pargs);
        EmitCreateMngdMarshaler(m_pslNDirect->GetSetupCodeStream());

        // Create a local to be the home of the length parameter
        DWORD dwManagedLocalHome = m_pcsMarshal->NewLocal(GetManagedType());
        m_managedHome.InitHome(ILStubMarshalHome::HomeType_ILLocal, dwManagedLocalHome);
        *pdwHiddenLengthManagedHomeLocal = dwManagedLocalHome;

        // managed length = 0
        m_pcsMarshal->EmitLDC(0);
        m_pcsMarshal->EmitCONV_T(pArrayInfo->GetHiddenLengthParamElementType());
        m_pcsMarshal->EmitSTLOC(dwManagedLocalHome);

        // And a local to be the home of the marshaled length
        LocalDesc nativeArgType(GetNativeType());
        DWORD dwNativeHomeLocal = m_pcsMarshal->NewLocal(nativeArgType);
        if (IsByref(dwMarshalFlags))
        {
            nativeArgType.MakeByRef();
        }
        m_nativeHome.InitHome(ILStubMarshalHome::HomeType_ILLocal, dwNativeHomeLocal);
        *pdwHiddenLengthNativeHomeLocal = dwNativeHomeLocal;

        // Update the native signature to contain the new native parameter
        m_pcsMarshal->SetStubTargetArgType(&nativeArgType, false);

        if (IsCLRToNative(dwMarshalFlags))
        {
            // Load the length of the array into the local
            if (IsIn(dwMarshalFlags))
            {
                ILCodeLabel *pSkipGetLengthLabel = m_pcsMarshal->NewCodeLabel();
                m_pcsMarshal->EmitLDARG(arrayIndex);
                m_pcsMarshal->EmitBRFALSE(pSkipGetLengthLabel);

                m_pcsMarshal->EmitLDARG(arrayIndex);

                if (IsByref(dwMarshalFlags))
                {
                    // if (*array == null) goto pSkipGetLengthLabel
                    m_pcsMarshal->EmitLDIND_REF();
                    m_pcsMarshal->EmitBRFALSE(pSkipGetLengthLabel);
                    
                    // array = *array
                    m_pcsMarshal->EmitLDARG(arrayIndex);
                    m_pcsMarshal->EmitLDIND_REF();
                }

                m_pcsMarshal->EmitLDLEN();
                m_pcsMarshal->EmitCONV_T(pArrayInfo->GetHiddenLengthParamElementType());
                m_pcsMarshal->EmitSTLOC(dwManagedLocalHome);
                m_pcsMarshal->EmitLabel(pSkipGetLengthLabel);
            }

            if (IsByref(dwMarshalFlags))
            {
                EmitMarshalArgumentContentsCLRToNativeByref(true);
            }
            else
            {
                EmitMarshalArgumentContentsCLRToNative();
            }
        }
        else
        {
            // Load the length of the array into the local
            if (IsIn(dwMarshalFlags))
            {
                m_pcsMarshal->EmitLDARG(hiddenArgIndex);
                if (IsByref(dwMarshalFlags))
                {
                    LocalDesc nativeParamType(GetNativeType());
                    m_pcsMarshal->EmitLDIND_T(&nativeParamType);
                }
                m_pcsMarshal->EmitSTLOC(dwNativeHomeLocal);
            }

            if (IsByref(dwMarshalFlags))
            {
                EmitMarshalArgumentContentsNativeToCLRByref(true);
            }
            else
            {
                EmitMarshalArgumentContentsNativeToCLR();
            }

            // We can't copy the final length back to the parameter just yet, since we don't know what
            // local the array lives in.  Instead, we rely on the hidden length array marshaler to copy
            // the value into the out parameter for us.
        }
    }

#endif // FEATURE_COMINTEROP

    virtual void EmitSetupArgument(ILCodeStream* pslILEmit)
    {
        STANDARD_VM_CONTRACT;

        if (IsCLRToNative(m_dwMarshalFlags))
        {
            if (IsNativePassedByRef())
            {
                EmitLoadNativeHomeAddr(pslILEmit);
            }
            else
            {
                EmitLoadNativeValue(pslILEmit);
            }
        }
        else
        {
            if (IsManagedPassedByRef())
            {
                EmitLoadManagedHomeAddr(pslILEmit);
            }
            else
            {
                EmitLoadManagedValue(pslILEmit);
            }
        }
    }

    virtual void EmitMarshalReturnValue(
                ILCodeStream* pcsMarshal, 
                ILCodeStream* pcsUnmarshal,
                ILCodeStream* pcsDispatch,
                UINT argidx,
                UINT16 wNativeSize,
                DWORD dwMarshalFlags,
                OverrideProcArgs*  pargs)
    {
        STANDARD_VM_CONTRACT;

        Init(pcsMarshal, pcsUnmarshal, argidx, dwMarshalFlags, pargs);

        LocalDesc nativeType = GetNativeType();
        LocalDesc managedType = GetManagedType();
        
        bool byrefNativeReturn = false;
        CorElementType typ = ELEMENT_TYPE_VOID;
        UINT32 nativeSize = 0;
        bool nativeMethodIsMemberFunction = (CorInfoCallConv)m_pslNDirect->GetStubTargetCallingConv() == CORINFO_CALLCONV_THISCALL;
            
        // we need to convert value type return types to primitives as
        // JIT does not inline P/Invoke calls that return structures
        if (nativeType.IsValueClass())
        {
            if (wNativeSize == VARIABLESIZE)
            {
                // the unmanaged type size is variable
                nativeSize = m_pargs->m_pMT->GetNativeSize();
            }
            else
            {
                // the unmanaged type size is fixed
                nativeSize = wNativeSize;
            }

#if defined(PLATFORM_WINDOWS)
            // JIT32 and JIT64 (which is only used on the Windows Desktop CLR) has a problem generating
            // code for the pinvoke ILStubs which do a return using a struct type.  Therefore, we
            // change the signature of calli to return void and make the return buffer as first argument. 

            // For Windows, we need to use a return buffer for native member functions returning structures.
            // On Windows arm we need to respect HFAs and not use a return buffer if the return type is an HFA
            // for X86 Windows non-member functions we bash the return type from struct to U1, U2, U4 or U8
            // and use byrefNativeReturn for all other structs.
            if (nativeMethodIsMemberFunction)
            {
#ifdef _TARGET_ARM_
                byrefNativeReturn = !nativeType.InternalToken.GetMethodTable()->IsNativeHFA();
#else
                byrefNativeReturn = true;
#endif
            }
            else
            {
#ifdef _TARGET_X86_
                switch (nativeSize)
                {
                    case 1: typ = ELEMENT_TYPE_U1; break;
                    case 2: typ = ELEMENT_TYPE_U2; break;
                    case 4: typ = ELEMENT_TYPE_U4; break;
                    case 8: typ = ELEMENT_TYPE_U8; break;
                    default: byrefNativeReturn = true; break;
                }
#endif // _TARGET_X86_
            }
#endif // defined(PLATFORM_WINDOWS)

            // for UNIX_X86_ABI, we always need a return buffer argument for any size of structs.
#ifdef UNIX_X86_ABI
            byrefNativeReturn = true;
#endif
        }

        if (IsHresultSwap(dwMarshalFlags) || (byrefNativeReturn && (IsCLRToNative(m_dwMarshalFlags) || nativeMethodIsMemberFunction)))
        {
            LocalDesc extraParamType = nativeType;
            extraParamType.MakeByRef();

            m_pcsMarshal->SetStubTargetArgType(&extraParamType, false);
            if (byrefNativeReturn && !IsCLRToNative(m_dwMarshalFlags))
            {
                // If doing a native->managed call and returning a structure by-ref,
                // the native signature has an extra param for the struct return
                // than the managed signature. Adjust the target stack delta to account this extra
                // parameter.
                m_pslNDirect->AdjustTargetStackDeltaForExtraParam();
                // We also need to account for the lack of a return value in the native signature.
                // To do this, we adjust the stack delta again for the return parameter.
                m_pslNDirect->AdjustTargetStackDeltaForExtraParam();
            }
            
            if (IsHresultSwap(dwMarshalFlags))
            {
                // HRESULT swapping: the original return value is transformed into an extra
                // byref parameter and the target is expected to return an HRESULT
                m_pcsMarshal->SetStubTargetReturnType(ELEMENT_TYPE_I4);    // native method returns an HRESULT
            }
            else
            {
                // byref structure return: the original return value is transformed into an
                // extra byref parameter and the target is not expected to return anything
                //
                // note: we do this only for forward calls because [unmanaged calling conv.
                // uses byref return] implies [managed calling conv. uses byref return]
                m_pcsMarshal->SetStubTargetReturnType(ELEMENT_TYPE_VOID);
            }
        }
        else
        {
            if (typ != ELEMENT_TYPE_VOID)
            {
                // small structure return: the original return value is transformed into
                // ELEMENT_TYPE_U1, ELEMENT_TYPE_U2, ELEMENT_TYPE_U4, or ELEMENT_TYPE_U8
                m_pcsMarshal->SetStubTargetReturnType(typ);
            }
            else
            {
                m_pcsMarshal->SetStubTargetReturnType(&nativeType);
            }
        }
        
        m_managedHome.InitHome(ILStubMarshalHome::HomeType_ILLocal, m_pcsMarshal->NewLocal(managedType));
        m_nativeHome.InitHome(ILStubMarshalHome::HomeType_ILLocal, m_pcsMarshal->NewLocal(nativeType));

        EmitCreateMngdMarshaler(m_pcsMarshal);

        if (IsCLRToNative(dwMarshalFlags))
        {
            if (IsHresultSwap(dwMarshalFlags) || byrefNativeReturn)
            {
                EmitReInitNative(m_pcsMarshal);
                EmitLoadNativeHomeAddr(pcsDispatch);    // load up the byref native type as an extra arg
            }
            else
            {
                if (typ != ELEMENT_TYPE_VOID)
                {
                    // small structure forward: the returned integer is memcpy'd into native home
                    // of the structure

                    DWORD dwTempLocalNum = m_pcsUnmarshal->NewLocal(typ);
                    m_pcsUnmarshal->EmitSTLOC(dwTempLocalNum);
                            
                    // cpblk
                    m_nativeHome.EmitLoadHomeAddr(m_pcsUnmarshal);
                    m_pcsUnmarshal->EmitLDLOCA(dwTempLocalNum);
                    m_pcsUnmarshal->EmitLDC(nativeSize);
                    m_pcsUnmarshal->EmitCPBLK();
                }
                else
                {
                    EmitStoreNativeValue(m_pcsUnmarshal);
                }
            }

            if (NeedsMarshalCleanupIndex())
            {
                m_pslNDirect->EmitSetArgMarshalIndex(m_pcsUnmarshal, NDirectStubLinker::CLEANUP_INDEX_ARG0_MARSHAL + m_argidx);
            }

            EmitConvertSpaceAndContentsNativeToCLR(m_pcsUnmarshal);

            EmitCleanupCLRToNative();

            EmitLoadManagedValue(m_pcsUnmarshal);
        }
        else
        {
            EmitStoreManagedValue(m_pcsUnmarshal);

            if (NeedsMarshalCleanupIndex())
            {
                m_pslNDirect->EmitSetArgMarshalIndex(m_pcsUnmarshal, NDirectStubLinker::CLEANUP_INDEX_ARG0_MARSHAL + m_argidx);
            }

            if (IsHresultSwap(dwMarshalFlags))
            {
                // we have to skip unmarshaling return value into the HRESULT-swapped argument
                // if the argument came as NULL (otherwise we would leak unmanaged resources as
                // we have no way of passing them back to the caller)
                ILCodeLabel *pSkipConversionLabel = m_pcsUnmarshal->NewCodeLabel();

                m_pcsUnmarshal->EmitLDARG(argidx);
                m_pcsUnmarshal->EmitBRFALSE(pSkipConversionLabel);
                EmitConvertSpaceAndContentsCLRToNative(m_pcsUnmarshal);
                m_pcsUnmarshal->EmitLabel(pSkipConversionLabel);
            }
            else
            {
                EmitConvertSpaceAndContentsCLRToNative(m_pcsUnmarshal);
            }

            if (NeedsUnmarshalCleanupIndex())
            {
                // if an exception is thrown after this point, we will clean up the unmarshaled retval
                m_pslNDirect->EmitSetArgMarshalIndex(m_pcsUnmarshal, NDirectStubLinker::CLEANUP_INDEX_RETVAL_UNMARSHAL);
            }

            EmitCleanupNativeToCLR();

            if (IsHresultSwap(dwMarshalFlags))
            {
                // we tolerate NULL here mainly for backward compatibility reasons
                m_nativeHome.EmitCopyToByrefArgWithNullCheck(m_pcsUnmarshal, &nativeType, argidx);
                m_pcsUnmarshal->EmitLDC(S_OK);
            }
            else if (byrefNativeReturn && nativeMethodIsMemberFunction)
            {
                m_nativeHome.EmitCopyToByrefArg(m_pcsUnmarshal, &nativeType, argidx);
            }
            else
            {
                if (typ != ELEMENT_TYPE_VOID)
                {
                    // small structure return (reverse): native home of the structure is memcpy'd
                    // into the integer to be returned from the stub

                    DWORD dwTempLocalNum = m_pcsUnmarshal->NewLocal(typ);
                            
                    // cpblk
                    m_pcsUnmarshal->EmitLDLOCA(dwTempLocalNum);
                    m_nativeHome.EmitLoadHomeAddr(m_pcsUnmarshal);
                    m_pcsUnmarshal->EmitLDC(nativeSize);
                    m_pcsUnmarshal->EmitCPBLK();

                    m_pcsUnmarshal->EmitLDLOC(dwTempLocalNum);
                }
                else
                {
                    EmitLoadNativeValue(m_pcsUnmarshal);
                }
            }

            // make sure we free (and zero) the return value if an exception is thrown
            EmitExceptionCleanupNativeToCLR();
        }
    }        


protected:

    virtual void EmitCreateMngdMarshaler(ILCodeStream* pslILEmit)
    {
        LIMITED_METHOD_CONTRACT;
    }

    virtual void EmitLoadMngdMarshaler(ILCodeStream* pslILEmit)
    {
        CONTRACTL
        {
            THROWS;
            GC_TRIGGERS;
            MODE_ANY;
        }
        CONTRACTL_END;

        CONSISTENCY_CHECK((DWORD)-1 != m_dwMngdMarshalerLocalNum);
        pslILEmit->EmitLDLOC(m_dwMngdMarshalerLocalNum);
    }

    void EmitSetupSigAndDefaultHomesCLRToNative()
    {
        CONTRACTL
        {
            STANDARD_VM_CHECK;
            PRECONDITION(IsCLRToNative(m_dwMarshalFlags) && !IsByref(m_dwMarshalFlags));
        }
        CONTRACTL_END;

        LocalDesc nativeArgType = GetNativeType();
        DWORD     dwNativeHomeLocalNum = m_pcsMarshal->NewLocal(nativeArgType);
        m_pcsMarshal->SetStubTargetArgType(&nativeArgType);

        m_managedHome.InitHome(ILStubMarshalHome::HomeType_ILArgument, m_argidx);
        m_nativeHome.InitHome(ILStubMarshalHome::HomeType_ILLocal, dwNativeHomeLocalNum);
    }

    void EmitCleanupCLRToNativeTemp()
    {
        STANDARD_VM_CONTRACT;

        if (NeedsClearNative())
        {
            CONSISTENCY_CHECK(NeedsMarshalCleanupIndex());

            ILCodeStream* pcsCleanup = m_pslNDirect->GetCleanupCodeStream();
            ILCodeLabel*  pSkipClearNativeLabel = pcsCleanup->NewCodeLabel();

            m_pslNDirect->EmitCheckForArgCleanup(pcsCleanup,
                                                 NDirectStubLinker::CLEANUP_INDEX_ARG0_MARSHAL + m_argidx,
                                                 NDirectStubLinker::BranchIfNotMarshaled,
                                                 pSkipClearNativeLabel);

            EmitClearNativeTemp(pcsCleanup);
            pcsCleanup->EmitLabel(pSkipClearNativeLabel);
        }
    }

    void EmitCleanupCLRToNative()
    {
        STANDARD_VM_CONTRACT;

        if (NeedsClearNative())
        {
            CONSISTENCY_CHECK(NeedsMarshalCleanupIndex());

            ILCodeStream* pcsCleanup = m_pslNDirect->GetCleanupCodeStream();
            ILCodeLabel*  pSkipClearNativeLabel = pcsCleanup->NewCodeLabel();

            m_pslNDirect->EmitCheckForArgCleanup(pcsCleanup,
                                                 NDirectStubLinker::CLEANUP_INDEX_ARG0_MARSHAL + m_argidx,
                                                 NDirectStubLinker::BranchIfNotMarshaled,
                                                 pSkipClearNativeLabel);

            EmitClearNative(pcsCleanup);
            pcsCleanup->EmitLabel(pSkipClearNativeLabel);
        }
    }

    virtual void EmitMarshalArgumentCLRToNative()
    {
        CONTRACTL
        {
            STANDARD_VM_CHECK;
            PRECONDITION(IsCLRToNative(m_dwMarshalFlags) && !IsByref(m_dwMarshalFlags));
        }
        CONTRACTL_END;

        EmitSetupSigAndDefaultHomesCLRToNative();
        EmitMarshalArgumentContentsCLRToNative();
    }

    void EmitMarshalArgumentContentsCLRToNative()
    {
        CONTRACTL
        {
            STANDARD_VM_CHECK;
            PRECONDITION(IsCLRToNative(m_dwMarshalFlags) && !IsByref(m_dwMarshalFlags));
        }
        CONTRACTL_END;

        //
        // marshal
        //
        if (IsIn(m_dwMarshalFlags))
        {
            EmitConvertSpaceAndContentsCLRToNativeTemp(m_pcsMarshal);
        }
        else
        {
            EmitConvertSpaceCLRToNativeTemp(m_pcsMarshal);
        }

        //
        // unmarshal
        //
        if (IsOut(m_dwMarshalFlags))
        {
            if (IsIn(m_dwMarshalFlags))
            {
                EmitClearCLRContents(m_pcsUnmarshal);
            }
            EmitConvertContentsNativeToCLR(m_pcsUnmarshal);
        }

        EmitCleanupCLRToNativeTemp();
   }

    void EmitSetupSigAndDefaultHomesCLRToNativeByref(bool fBlittable = false)
    {
        CONTRACTL
        {
            STANDARD_VM_CHECK;
            PRECONDITION(IsCLRToNative(m_dwMarshalFlags) && IsByref(m_dwMarshalFlags));
        }
        CONTRACTL_END;

        LocalDesc nativeType = GetNativeType();
        LocalDesc managedType = GetManagedType();

        LocalDesc nativeArgType = nativeType;
        nativeArgType.MakeByRef();
        m_pcsMarshal->SetStubTargetArgType(&nativeArgType);

        if (fBlittable)
        {
            // we will not work with the actual data but only with a pointer to that data
            // (the managed and native type had better be the same if it's blittable)
            _ASSERTE(nativeType.ElementType[0] == managedType.ElementType[0]);

            // native home will keep the containing object pinned
            nativeType.MakeByRef();
            nativeType.MakePinned();

            m_managedHome.InitHome(ILStubMarshalHome::HomeType_ILByrefArgument, m_argidx);
            m_nativeHome.InitHome(ILStubMarshalHome::HomeType_ILByrefLocal, m_pcsMarshal->NewLocal(nativeType));
        }
        else
        {
            m_managedHome.InitHome(ILStubMarshalHome::HomeType_ILLocal, m_pcsMarshal->NewLocal(managedType));
            m_nativeHome.InitHome(ILStubMarshalHome::HomeType_ILLocal, m_pcsMarshal->NewLocal(nativeType));
        }
    }

    virtual void EmitMarshalArgumentCLRToNativeByref()
    {
        CONTRACTL
        {
            STANDARD_VM_CHECK;
            PRECONDITION(IsCLRToNative(m_dwMarshalFlags) && IsByref(m_dwMarshalFlags));
        }
        CONTRACTL_END;

        EmitSetupSigAndDefaultHomesCLRToNativeByref();
        EmitMarshalArgumentContentsCLRToNativeByref(false);
    }

    void EmitMarshalArgumentContentsCLRToNativeByref(bool managedHomeAlreadyInitialized)
    {
        CONTRACTL
        {
            STANDARD_VM_CHECK;
            PRECONDITION(IsCLRToNative(m_dwMarshalFlags) && IsByref(m_dwMarshalFlags));
        }
        CONTRACTL_END;

        LocalDesc managedType = GetManagedType();

        //
        // marshal
        //
        if (IsIn(m_dwMarshalFlags) && ! IsOut(m_dwMarshalFlags))
        {
            if (!managedHomeAlreadyInitialized)
            {
                m_managedHome.EmitCopyFromByrefArg(m_pcsMarshal, &managedType, m_argidx);
            }

            EmitConvertSpaceAndContentsCLRToNativeTemp(m_pcsMarshal);
        }
        else if (IsIn(m_dwMarshalFlags) && IsOut(m_dwMarshalFlags)) 
        {
            if (!managedHomeAlreadyInitialized)
            {
                m_managedHome.EmitCopyFromByrefArg(m_pcsMarshal, &managedType, m_argidx);
            }

            EmitConvertSpaceAndContentsCLRToNative(m_pcsMarshal);
        }
        else
        {
            EmitReInitNative(m_pcsMarshal);
        }

        //
        // unmarshal
        //
        if (IsOut(m_dwMarshalFlags))
        {
            EmitClearCLR(m_pcsUnmarshal);

            EmitConvertSpaceAndContentsNativeToCLR(m_pcsUnmarshal);
            
            if (!managedHomeAlreadyInitialized)
            {
                m_managedHome.EmitCopyToByrefArg(m_pcsUnmarshal, &managedType, m_argidx);
            }

            EmitCleanupCLRToNative();
        }
        else
        {
            EmitCleanupCLRToNativeTemp();
        }
        //
        // @TODO: ensure ReInitNative is called on [in,out] byref args when an exception occurs 
        //
    }

    void EmitSetupSigAndDefaultHomesNativeToCLR()
    {
        CONTRACTL
        {
            STANDARD_VM_CHECK;
            PRECONDITION(!IsCLRToNative(m_dwMarshalFlags) && !IsByref(m_dwMarshalFlags));
        }
        CONTRACTL_END;
        
        LocalDesc nativeArgType = GetNativeType();
        m_pcsMarshal->SetStubTargetArgType(&nativeArgType);

        m_managedHome.InitHome(ILStubMarshalHome::HomeType_ILLocal, m_pcsMarshal->NewLocal(GetManagedType()));
        m_nativeHome.InitHome(ILStubMarshalHome::HomeType_ILArgument, m_argidx);
    }

    void EmitCleanupNativeToCLR()
    {
        STANDARD_VM_CONTRACT;

        if (NeedsClearCLR())
        {
            CONSISTENCY_CHECK(NeedsMarshalCleanupIndex());

            ILCodeStream* pcsCleanup = m_pslNDirect->GetCleanupCodeStream();
            ILCodeLabel*  pSkipClearCLRLabel = pcsCleanup->NewCodeLabel();

            m_pslNDirect->EmitCheckForArgCleanup(pcsCleanup,
                                                 NDirectStubLinker::CLEANUP_INDEX_ARG0_MARSHAL + m_argidx,
                                                 NDirectStubLinker::BranchIfNotMarshaled,
                                                 pSkipClearCLRLabel);

            EmitClearCLR(pcsCleanup);
            pcsCleanup->EmitLabel(pSkipClearCLRLabel);
        }
    }

    // Emits cleanup code that runs only if an exception is thrown during execution of an IL stub (its try
    // block to be precise). The goal is to roll back allocations of native resources that may have already
    // happened to prevent leaks, and also clear output arguments to prevent passing out invalid data - most
    // importantly dangling pointers. The canonical example is an exception thrown during unmarshaling of
    // an argument at which point other arguments have already been unmarshaled.
    void EmitExceptionCleanupNativeToCLR()
    {
        STANDARD_VM_CONTRACT;

        _ASSERTE(IsRetval(m_dwMarshalFlags) || IsOut(m_dwMarshalFlags));

        LocalDesc nativeType = GetNativeType();
        ILCodeStream *pcsCleanup = m_pslNDirect->GetExceptionCleanupCodeStream();

        if (NeedsClearNative())
        {
            m_pslNDirect->SetExceptionCleanupNeeded();

            ILCodeLabel *pSkipCleanupLabel = pcsCleanup->NewCodeLabel();

            // if this is byref in/out and we have not marshaled this argument
            // yet, we need to populate the native home with the incoming value
            if (IsIn(m_dwMarshalFlags) && IsByref(m_dwMarshalFlags))
            {
                ILCodeLabel *pSkipCopyLabel = pcsCleanup->NewCodeLabel();

                CONSISTENCY_CHECK(NeedsMarshalCleanupIndex());
                m_pslNDirect->EmitCheckForArgCleanup(pcsCleanup,
                                                     NDirectStubLinker::CLEANUP_INDEX_ARG0_MARSHAL + m_argidx,
                                                     NDirectStubLinker::BranchIfMarshaled,
                                                     pSkipCopyLabel);

                pcsCleanup->EmitLDARG(m_argidx);
                pcsCleanup->EmitBRFALSE(pSkipCleanupLabel); // if the argument is NULL, skip cleanup completely

                m_nativeHome.EmitCopyFromByrefArg(pcsCleanup, &nativeType, m_argidx);

                pcsCleanup->EmitLabel(pSkipCopyLabel);
            }

            // if this is retval or out-only, the native home does not get initialized until we unmarshal it
            if (IsRetval(m_dwMarshalFlags) || !IsIn(m_dwMarshalFlags))
            {
                CONSISTENCY_CHECK(NeedsUnmarshalCleanupIndex());

                UINT uArgIdx = (IsRetval(m_dwMarshalFlags) ?
                    NDirectStubLinker::CLEANUP_INDEX_RETVAL_UNMARSHAL :
                    NDirectStubLinker::CLEANUP_INDEX_ARG0_UNMARSHAL + m_argidx);

                m_pslNDirect->EmitCheckForArgCleanup(pcsCleanup,
                                                     uArgIdx,
                                                     NDirectStubLinker::BranchIfNotMarshaled,
                                                     pSkipCleanupLabel);
            }

            // we know that native home needs to be cleaned up at this point
            if (IsRetval(m_dwMarshalFlags) || (IsOut(m_dwMarshalFlags) && IsByref(m_dwMarshalFlags)))
            {
                // we own the buffer - clear everything
                EmitClearNative(pcsCleanup);
            }
            else
            {
                // this is a caller supplied buffer - clear only its contents
                EmitClearNativeContents(pcsCleanup);
            }

            pcsCleanup->EmitLabel(pSkipCleanupLabel);
        }

        // if there is an output buffer, zero it out so the caller does not get pointer to already freed data
        if (!IsHiddenLengthParam(m_dwMarshalFlags))
        {
            if (IsRetval(m_dwMarshalFlags) || (IsOut(m_dwMarshalFlags) && IsByref(m_dwMarshalFlags)))
            {
                m_pslNDirect->SetExceptionCleanupNeeded();

                EmitReInitNative(pcsCleanup);
                if (IsHresultSwap(m_dwMarshalFlags) || IsOut(m_dwMarshalFlags))
                {
                    m_nativeHome.EmitCopyToByrefArgWithNullCheck(pcsCleanup, &nativeType, m_argidx);
                }
            }
        }
    }

    virtual void EmitMarshalArgumentNativeToCLR()
    {
        CONTRACTL
        {
            STANDARD_VM_CHECK;
            PRECONDITION(!IsCLRToNative(m_dwMarshalFlags) && !IsByref(m_dwMarshalFlags));
        }
        CONTRACTL_END;
                    
        EmitSetupSigAndDefaultHomesNativeToCLR();
        EmitMarshalArgumentContentsNativeToCLR();
    }

    void EmitMarshalArgumentContentsNativeToCLR()
    {
        CONTRACTL
        {
            STANDARD_VM_CHECK;
            PRECONDITION(!IsCLRToNative(m_dwMarshalFlags) && !IsByref(m_dwMarshalFlags));
        }
        CONTRACTL_END;

        //
        // marshal
        //
        if (IsIn(m_dwMarshalFlags))
        {
            EmitConvertSpaceAndContentsNativeToCLR(m_pcsMarshal);
        }
        else
        {
            EmitConvertSpaceNativeToCLR(m_pcsMarshal);
        }

        //
        // unmarshal
        //
        if (IsOut(m_dwMarshalFlags))
        {
            if (IsIn(m_dwMarshalFlags))
            {
                EmitClearNativeContents(m_pcsUnmarshal);
            }
            EmitConvertContentsCLRToNative(m_pcsUnmarshal);

            // make sure we free the argument if an exception is thrown
            EmitExceptionCleanupNativeToCLR();
        }
        EmitCleanupNativeToCLR();
    }

    void EmitSetupSigAndDefaultHomesNativeToCLRByref(bool fBlittable = false)
    {
        CONTRACTL
        {
            STANDARD_VM_CHECK;
            PRECONDITION(!IsCLRToNative(m_dwMarshalFlags) && IsByref(m_dwMarshalFlags));
        }
        CONTRACTL_END;
    
        LocalDesc nativeType = GetNativeType();
        LocalDesc managedType = GetManagedType();
        LocalDesc nativeArgType = nativeType;
        nativeArgType.MakeByRef();
        m_pcsMarshal->SetStubTargetArgType(&nativeArgType);

        if (fBlittable)
        {
            // we will not work with the actual data but only with a pointer to that data
            // (the managed and native type had better be the same if it's blittable)
            _ASSERTE(nativeType.ElementType[0] == managedType.ElementType[0]);

            managedType.MakeByRef();

            m_managedHome.InitHome(ILStubMarshalHome::HomeType_ILByrefLocal, m_pcsMarshal->NewLocal(managedType));
            m_nativeHome.InitHome(ILStubMarshalHome::HomeType_ILByrefArgument, m_argidx);
        }
        else
        {
            m_managedHome.InitHome(ILStubMarshalHome::HomeType_ILLocal, m_pcsMarshal->NewLocal(managedType));
            m_nativeHome.InitHome(ILStubMarshalHome::HomeType_ILLocal, m_pcsMarshal->NewLocal(nativeType));
        }
    }

    virtual void EmitMarshalArgumentNativeToCLRByref()
    {
        CONTRACTL
        {
            STANDARD_VM_CHECK;
            PRECONDITION(!IsCLRToNative(m_dwMarshalFlags) && IsByref(m_dwMarshalFlags));
        }
        CONTRACTL_END;
        
        EmitSetupSigAndDefaultHomesNativeToCLRByref();
        EmitMarshalArgumentContentsNativeToCLRByref(false);
    }

    void EmitMarshalArgumentContentsNativeToCLRByref(bool nativeHomeAlreadyInitialized)
    {
        CONTRACTL
        {
            STANDARD_VM_CHECK;
            PRECONDITION(!IsCLRToNative(m_dwMarshalFlags) && IsByref(m_dwMarshalFlags));
        }
        CONTRACTL_END;

        LocalDesc nativeType = GetNativeType();

        //
        // marshal
        //
        if (IsIn(m_dwMarshalFlags))
        {
            if (!nativeHomeAlreadyInitialized)
            {
                m_nativeHome.EmitCopyFromByrefArg(m_pcsMarshal, &nativeType, m_argidx);
            }

            EmitConvertSpaceAndContentsNativeToCLR(m_pcsMarshal);
        }
        else
        {
            // dereference the argument so we throw before calling the managed target - this is the fastest way
            // to check for NULL (we can still throw later if the pointer is invalid yet non-NULL but we cannot
            // detect this realiably - the memory may get unmapped etc., NULL check is the best we can do here)
            m_pcsMarshal->EmitLDARG(m_argidx);
            m_pcsMarshal->EmitLDIND_I1();
            m_pcsMarshal->EmitPOP();
        }
        
        //
        // unmarshal
        //
        if (IsOut(m_dwMarshalFlags))
        {
            if (IsIn(m_dwMarshalFlags))
            {
                EmitClearNative(m_pcsUnmarshal);
                EmitReInitNative(m_pcsUnmarshal);
            }

            EmitConvertSpaceAndContentsCLRToNative(m_pcsUnmarshal);

            if (!nativeHomeAlreadyInitialized)
            {
                m_nativeHome.EmitCopyToByrefArg(m_pcsUnmarshal, &nativeType, m_argidx);
            }

            // make sure we free and zero the by-ref argument if an exception is thrown
            EmitExceptionCleanupNativeToCLR();
        }

        EmitCleanupNativeToCLR();
    }

    virtual LocalDesc GetNativeType() = 0;
    virtual LocalDesc GetManagedType() = 0;

    //
    // Native-to-CLR
    //
    virtual void EmitConvertSpaceNativeToCLR(ILCodeStream* pslILEmit)
    {
        LIMITED_METHOD_CONTRACT;
    }
        
    virtual void EmitConvertContentsNativeToCLR(ILCodeStream* pslILEmit)
    {
        LIMITED_METHOD_CONTRACT;
    }

    virtual void EmitConvertSpaceAndContentsNativeToCLR(ILCodeStream* pslILEmit)
    {
        WRAPPER_NO_CONTRACT;
        EmitConvertSpaceNativeToCLR(pslILEmit);
        EmitConvertContentsNativeToCLR(pslILEmit);
    }


    //
    // CLR-to-Native
    //
    virtual void EmitConvertSpaceCLRToNative(ILCodeStream* pslILEmit)
    {
        LIMITED_METHOD_CONTRACT;
    }

    virtual void EmitConvertSpaceCLRToNativeTemp(ILCodeStream* pslILEmit)
    {
        LIMITED_METHOD_CONTRACT;
        EmitConvertSpaceCLRToNative(pslILEmit);
    }

    virtual void EmitConvertContentsCLRToNative(ILCodeStream* pslILEmit)
    {
        LIMITED_METHOD_CONTRACT;
    }
    
    virtual void EmitConvertSpaceAndContentsCLRToNative(ILCodeStream* pslILEmit)
    {
        STANDARD_VM_CONTRACT;
        EmitConvertSpaceCLRToNative(pslILEmit);
        EmitConvertContentsCLRToNative(pslILEmit);
    }
        
    virtual void EmitConvertSpaceAndContentsCLRToNativeTemp(ILCodeStream* pslILEmit)
    {
        WRAPPER_NO_CONTRACT;
        EmitConvertSpaceAndContentsCLRToNative(pslILEmit);
    }

    //
    // Misc
    //
    virtual void EmitClearCLRContents(ILCodeStream* pslILEmit)
    {
        LIMITED_METHOD_CONTRACT;
    }

    virtual bool NeedsClearNative()
    {
        LIMITED_METHOD_CONTRACT;
        return false;
    }

    virtual void EmitClearNative(ILCodeStream* pslILEmit)
    {
        LIMITED_METHOD_CONTRACT;
    }

    virtual void EmitClearNativeTemp(ILCodeStream* pslILEmit)
    {
        LIMITED_METHOD_CONTRACT;
        EmitClearNative(pslILEmit);
    }

    virtual void EmitClearNativeContents(ILCodeStream* pslILEmit)
    {
        LIMITED_METHOD_CONTRACT;
    }

    virtual bool NeedsClearCLR()
    {
        LIMITED_METHOD_CONTRACT;
        return false;
    }

    virtual void EmitClearCLR(ILCodeStream* pslILEmit)
    {
        LIMITED_METHOD_CONTRACT;
    }

    virtual void EmitReInitNative(ILCodeStream* pslILEmit)
    {
        STANDARD_VM_CONTRACT;

        // Friendly Reminder:
        // You should implement your own EmitReInitNative if your native type is a struct,
        // as the following instructions apparently won't work on value types and will trigger
        // an ASSERT in JIT
        _ASSERTE(!GetNativeType().IsValueClass());
        
        pslILEmit->EmitLDC(0);
        pslILEmit->EmitCONV_T(static_cast<CorElementType>(GetNativeType().ElementType[0]));

        EmitStoreNativeValue(pslILEmit);
    }

    virtual bool IsManagedPassedByRef()
    {
        LIMITED_METHOD_CONTRACT;
        return IsByref(m_dwMarshalFlags);
    }

    virtual bool IsNativePassedByRef()
    {
        LIMITED_METHOD_CONTRACT;
        return IsByref(m_dwMarshalFlags);
    }

    void EmitInterfaceClearNative(ILCodeStream* pslILEmit);

public:
    static MarshalerOverrideStatus ArgumentOverride(NDirectStubLinker* psl,
                                                    BOOL               byref,
                                                    BOOL               fin,
                                                    BOOL               fout,
                                                    BOOL               fManagedToNative,
                                                    OverrideProcArgs*  pargs,
                                                    UINT*              pResID,
                                                    UINT               argidx,
                                                    UINT               nativeStackOffset)
    {
        LIMITED_METHOD_CONTRACT;
        return HANDLEASNORMAL;
    }

    static MarshalerOverrideStatus ReturnOverride(NDirectStubLinker*  psl,
                                                  BOOL                fManagedToNative,
                                                  BOOL                fHresultSwap,
                                                  OverrideProcArgs*   pargs,
                                                  UINT*               pResID)
    {
        LIMITED_METHOD_CONTRACT;
        return HANDLEASNORMAL;
    }
};

        
class ILCopyMarshalerBase : public ILMarshaler
{
    virtual LocalDesc GetManagedType()
    {
        WRAPPER_NO_CONTRACT;
        return GetNativeType();
    }

    virtual void EmitConvertContentsCLRToNative(ILCodeStream* pslILEmit)
    {
        STANDARD_VM_CONTRACT;

        EmitLoadManagedValue(pslILEmit);
        EmitStoreNativeValue(pslILEmit);
    }

    virtual void EmitConvertContentsNativeToCLR(ILCodeStream* pslILEmit)
    {
        STANDARD_VM_CONTRACT;

        EmitLoadNativeValue(pslILEmit);
        EmitStoreManagedValue(pslILEmit);
    }

    //
    // It's very unforunate that x86 used ML_COPYPINNEDGCREF for byref args.
    // The result is that developers can get away with being lazy about their 
    // in/out semantics and often times in/out byref params are marked out-
    // only, but because of ML_COPYPINNEDGCREF, they get in/out behavior.  
    //
    // There are even lazier developers who use byref params to pass arrays.
    // Pinning ensures that the native side 'sees' the entire array even when
    // only reference to one element was passed.
    // 
    // This method was changed to pin instead of copy in Dev10 in order
    // to match the original ML behavior.
    //
    virtual void EmitMarshalArgumentCLRToNativeByref()
    {
        CONTRACTL
        {
            STANDARD_VM_CHECK;
            PRECONDITION(IsCLRToNative(m_dwMarshalFlags) && IsByref(m_dwMarshalFlags));
        }
        CONTRACTL_END;

        EmitSetupSigAndDefaultHomesCLRToNativeByref(true);
            
        //
        // marshal
        //
        EmitLoadManagedHomeAddr(m_pcsMarshal);
        EmitStoreNativeHomeAddr(m_pcsMarshal);
        
        //
        // no unmarshaling is necessary since we directly passed the pinned byref to native,
        // the argument is therefore automatically in/out
        //
    }

    //
    // Similarly to the other direction, ML used ML_COPYPINNEDGCREF on x86 to
    // directly pass the unmanaged pointer as a byref argument to managed code.
    // This also makes an observable difference (allows passing NULL, changes
    // made to the original value during the call are visible in managed).
    //
    // This method was changed to pass pointer instead of copy in Dev10 in order
    // to match the original ML behavior. Note that in this direction we don't
    // need to pin the pointer - if it is pointing to GC heap, it must have been
    // pinned on the way to unmanaged.
    //
    virtual void EmitMarshalArgumentNativeToCLRByref()
    {
        CONTRACTL
        {
            STANDARD_VM_CHECK;
            PRECONDITION(!IsCLRToNative(m_dwMarshalFlags) && IsByref(m_dwMarshalFlags));
        }
        CONTRACTL_END;

        EmitSetupSigAndDefaultHomesNativeToCLRByref(true);
            
        //
        // marshal
        //
        EmitLoadNativeHomeAddr(m_pcsMarshal);
        EmitStoreManagedHomeAddr(m_pcsMarshal);
        
        //
        // no unmarshaling is necessary since we directly passed the pointer to managed
        // as a byref, the argument is therefore automatically in/out
        //
    }
};

template <CorElementType ELEMENT_TYPE, class PROMOTED_ELEMENT>
class ILCopyMarshalerSimple : public ILCopyMarshalerBase
{
public:
    enum
    {
        c_fInOnly               = TRUE,
        c_nativeSize            = sizeof(PROMOTED_ELEMENT),
        c_CLRSize               = sizeof(PROMOTED_ELEMENT),
    };

    bool IsSmallValueTypeSpecialCase()
    {
        //
        // Special case for small value types that get
        // mapped to MARSHAL_TYPE_GENERIC_8 -- use the 
        // valuetype type so the JIT is happy.
        //
        
        return (ELEMENT_TYPE == 
#ifdef _WIN64        
                    ELEMENT_TYPE_I8
#else // _WIN64
                    ELEMENT_TYPE_I4
#endif // _WIN64
                    ) && (NULL != m_pargs->m_pMT);
    }
    
    bool NeedToPromoteTo8Bytes()
    {
        WRAPPER_NO_CONTRACT;

#if defined(_TARGET_AMD64_)
        // If the argument is passed by value, 
        if (!IsByref(m_dwMarshalFlags) && !IsRetval(m_dwMarshalFlags))
        {
            // and it is an I4 or an U4, 
            if ( (ELEMENT_TYPE == ELEMENT_TYPE_I4) ||
                 (ELEMENT_TYPE == ELEMENT_TYPE_U4) )
            {
                // and we are doing a managed-to-unmanaged call,
                if (IsCLRToNative(m_dwMarshalFlags))
                {
                    // then we have to promote the native argument type to an I8 or an U8.
                    return true;
                }
            }
        }
#endif // _TARGET_AMD64_

        return false;
    }

    CorElementType GetConversionType(CorElementType type)
    {
        LIMITED_METHOD_CONTRACT;

        // I4 <-> I8; U4 <-> U8
        if (type == ELEMENT_TYPE_I4)
        {
            return ELEMENT_TYPE_I8;
        }
        else if (type == ELEMENT_TYPE_U4)
        {
            return ELEMENT_TYPE_U8;
        }
        else
        {
            return ELEMENT_TYPE_END;
        }
    }

    void EmitTypePromotion(ILCodeStream* pslILEmit)
    {
        WRAPPER_NO_CONTRACT;

        CorElementType promotedType = GetConversionType(ELEMENT_TYPE);
        if (promotedType == ELEMENT_TYPE_I8)
        {
            pslILEmit->EmitCONV_I8();
        }
        else if (promotedType == ELEMENT_TYPE_U8)
        {
            pslILEmit->EmitCONV_U8();
        }
    }

    virtual LocalDesc GetNativeType()
    {
        WRAPPER_NO_CONTRACT;
        
        if (NeedToPromoteTo8Bytes())
        {
            return LocalDesc(GetConversionType(ELEMENT_TYPE));
        }
        else
        {
            return GetManagedType();
        }
    }

    virtual LocalDesc GetManagedType()
    {
        WRAPPER_NO_CONTRACT;

        if (IsSmallValueTypeSpecialCase())
        {
            return LocalDesc(m_pargs->m_pMT);
        }
        else
        {
            return LocalDesc(ELEMENT_TYPE);
        }
    }

    virtual void EmitConvertContentsCLRToNative(ILCodeStream* pslILEmit)
    {
        STANDARD_VM_CONTRACT;

        EmitLoadManagedValue(pslILEmit);
        if (NeedToPromoteTo8Bytes())
        {
            EmitTypePromotion(pslILEmit);
        }
        EmitStoreNativeValue(pslILEmit);
    }

    virtual void EmitReInitNative(ILCodeStream* pslILEmit)
    {
        STANDARD_VM_CONTRACT;

        if (IsSmallValueTypeSpecialCase())
        {
            EmitLoadNativeHomeAddr(pslILEmit);
            pslILEmit->EmitINITOBJ(pslILEmit->GetToken(m_pargs->m_pMT));
        }
        else
        {
            // ldc.i4.0, conv.i8/u8/r4/r8 is shorter than ldc.i8/r4/r8 0
            pslILEmit->EmitLDC(0);
            pslILEmit->EmitCONV_T(ELEMENT_TYPE);

            EmitStoreNativeValue(pslILEmit);
        }
    }
};

typedef ILCopyMarshalerSimple<ELEMENT_TYPE_I1, INT_PTR>  ILCopyMarshaler1;
typedef ILCopyMarshalerSimple<ELEMENT_TYPE_U1, UINT_PTR> ILCopyMarshalerU1;
typedef ILCopyMarshalerSimple<ELEMENT_TYPE_I2, INT_PTR>  ILCopyMarshaler2;
typedef ILCopyMarshalerSimple<ELEMENT_TYPE_U2, UINT_PTR> ILCopyMarshalerU2;
typedef ILCopyMarshalerSimple<ELEMENT_TYPE_I4, INT_PTR>  ILCopyMarshaler4;
typedef ILCopyMarshalerSimple<ELEMENT_TYPE_U4, UINT_PTR> ILCopyMarshalerU4;
typedef ILCopyMarshalerSimple<ELEMENT_TYPE_I8, INT64>    ILCopyMarshaler8;
typedef ILCopyMarshalerSimple<ELEMENT_TYPE_R4, float>    ILFloatMarshaler;
typedef ILCopyMarshalerSimple<ELEMENT_TYPE_R8, double>   ILDoubleMarshaler;

template <BinderClassID CLASS__ID, class PROMOTED_ELEMENT>
class ILCopyMarshalerKnownStruct : public ILCopyMarshalerBase
{
public:
    enum
    {
        c_fInOnly               = TRUE,
        c_nativeSize            = sizeof(PROMOTED_ELEMENT),
        c_CLRSize               = sizeof(PROMOTED_ELEMENT),
    };

    virtual void EmitReInitNative(ILCodeStream* pslILEmit)
    {
        STANDARD_VM_CONTRACT;
        
        EmitLoadNativeHomeAddr(pslILEmit);
        pslILEmit->EmitINITOBJ(pslILEmit->GetToken(MscorlibBinder::GetClass(CLASS__ID)));
    }

    virtual LocalDesc GetNativeType()
    {
        STANDARD_VM_CONTRACT;

        return LocalDesc(MscorlibBinder::GetClass(CLASS__ID));
    }
};

typedef ILCopyMarshalerKnownStruct<CLASS__DECIMAL, DECIMAL> ILDecimalMarshaler;
typedef ILCopyMarshalerKnownStruct<CLASS__GUID, GUID> ILGuidMarshaler;

class ILBlittableValueClassMarshaler : public ILCopyMarshalerBase
{
public:
    enum
    {
        c_fInOnly               = TRUE,
        c_nativeSize            = VARIABLESIZE,
        c_CLRSize               = VARIABLESIZE,
    };
        
    virtual void EmitReInitNative(ILCodeStream* pslILEmit)
    {
        STANDARD_VM_CONTRACT;
        
        EmitLoadNativeHomeAddr(pslILEmit);
        pslILEmit->EmitINITOBJ(pslILEmit->GetToken(m_pargs->m_pMT));
    }

    virtual LocalDesc GetNativeType()
    {
        LIMITED_METHOD_CONTRACT;
        
        return LocalDesc(m_pargs->m_pMT);
    }
};


class ILDelegateMarshaler : public ILMarshaler
{
public:
    enum
    {
        c_fInOnly               = TRUE,
        c_nativeSize            = sizeof(void *),
        c_CLRSize               = sizeof(OBJECTREF),
    };

protected:
    virtual LocalDesc GetNativeType();
    virtual LocalDesc GetManagedType();
    virtual void EmitConvertContentsCLRToNative(ILCodeStream* pslILEmit);
    virtual void EmitConvertContentsNativeToCLR(ILCodeStream* pslILEmit);
};

class ILReflectionObjectMarshaler : public ILMarshaler
{
public:
    enum
    {
        c_fInOnly               = TRUE,
        c_nativeSize            = sizeof(void *),
        c_CLRSize               = sizeof(OBJECTREF),
    };

protected:
    virtual LocalDesc GetManagedType();
    virtual LocalDesc GetNativeType();
    virtual void EmitConvertContentsCLRToNative(ILCodeStream* pslILEmit);
    virtual void EmitConvertContentsNativeToCLR(ILCodeStream* pslILEmit);
    virtual BinderFieldID GetStructureFieldID() {LIMITED_METHOD_CONTRACT; return (BinderFieldID)0;}
    virtual BinderFieldID GetObjectFieldID() = 0;
    virtual BinderClassID GetManagedTypeBinderID() = 0;
};

class ILRuntimeTypeHandleMarshaler : public ILReflectionObjectMarshaler
{
protected:
    virtual BinderFieldID GetStructureFieldID() { LIMITED_METHOD_CONTRACT; return FIELD__RT_TYPE_HANDLE__M_TYPE; }
    virtual BinderFieldID GetObjectFieldID() { LIMITED_METHOD_CONTRACT; return FIELD__CLASS__TYPEHANDLE; }
    virtual BinderClassID GetManagedTypeBinderID() { LIMITED_METHOD_CONTRACT; return CLASS__RT_TYPE_HANDLE; }
};

class ILRuntimeMethodHandleMarshaler : public ILReflectionObjectMarshaler
{
protected:
    virtual BinderFieldID GetStructureFieldID() { LIMITED_METHOD_CONTRACT; return FIELD__METHOD_HANDLE__METHOD; }
    virtual BinderFieldID GetObjectFieldID() { LIMITED_METHOD_CONTRACT; return FIELD__STUBMETHODINFO__HANDLE; }
    virtual BinderClassID GetManagedTypeBinderID() { LIMITED_METHOD_CONTRACT; return CLASS__METHOD_HANDLE; }
};

class ILRuntimeFieldHandleMarshaler : public ILReflectionObjectMarshaler
{
protected:
    virtual BinderFieldID GetStructureFieldID() { LIMITED_METHOD_CONTRACT; return FIELD__FIELD_HANDLE__M_FIELD; }
    virtual BinderFieldID GetObjectFieldID() { LIMITED_METHOD_CONTRACT; return FIELD__RT_FIELD_INFO__HANDLE; }
    virtual BinderClassID GetManagedTypeBinderID() { LIMITED_METHOD_CONTRACT; return CLASS__FIELD_HANDLE; }
};

class ILBoolMarshaler : public ILMarshaler
{
public:

    virtual CorElementType GetNativeBoolElementType() = 0;
    virtual int GetNativeTrueValue() = 0;
    virtual int GetNativeFalseValue() = 0;

protected:
    virtual LocalDesc GetNativeType();
    virtual LocalDesc GetManagedType();
    virtual void EmitConvertContentsCLRToNative(ILCodeStream* pslILEmit);
    virtual void EmitConvertContentsNativeToCLR(ILCodeStream* pslILEmit);
};

class ILWinBoolMarshaler : public ILBoolMarshaler
{
public:
    enum
    {
        c_fInOnly               = TRUE,
        c_nativeSize            = sizeof(BOOL),
        c_CLRSize               = sizeof(INT8),
    };
        
protected:    
    virtual CorElementType GetNativeBoolElementType()
    {
        LIMITED_METHOD_CONTRACT;
        return ELEMENT_TYPE_I4;
    }

    virtual int GetNativeTrueValue()
    {
        LIMITED_METHOD_CONTRACT;
        return 1;
    }
        
    virtual int GetNativeFalseValue()
    {
        LIMITED_METHOD_CONTRACT;
        return 0;
    }
};

class ILCBoolMarshaler : public ILBoolMarshaler
{
public:
    enum
    {
        c_fInOnly               = TRUE,
        c_nativeSize            = sizeof(BYTE),
        c_CLRSize               = sizeof(INT8),
    };

protected:
    virtual CorElementType GetNativeBoolElementType()
    {
        LIMITED_METHOD_CONTRACT;
        return ELEMENT_TYPE_I1;
    }

    virtual int GetNativeTrueValue()
    {
        LIMITED_METHOD_CONTRACT;
        return 1;
    }
                
    virtual int GetNativeFalseValue()
    {
        LIMITED_METHOD_CONTRACT;
        return 0;
    }
};

#ifdef FEATURE_COMINTEROP
class ILVtBoolMarshaler : public ILBoolMarshaler
{
public:    
    enum
    {
        c_fInOnly               = TRUE,
        c_nativeSize            = sizeof(VARIANT_BOOL),
        c_CLRSize               = sizeof(INT8),
    };

protected:    
    virtual CorElementType GetNativeBoolElementType()
    {
        LIMITED_METHOD_CONTRACT;
        return ELEMENT_TYPE_I2;
    }

    virtual int GetNativeTrueValue()
    {
        LIMITED_METHOD_CONTRACT;
        return VARIANT_TRUE;
    }

    virtual int GetNativeFalseValue()
    {
        LIMITED_METHOD_CONTRACT;
        return VARIANT_FALSE;
    }
};
#endif // FEATURE_COMINTEROP

class ILWSTRMarshaler : public ILMarshaler
{
public:
    enum
    {
        c_fInOnly               = FALSE,
        c_nativeSize            = sizeof(void *),
        c_CLRSize               = sizeof(OBJECTREF),
    };

#ifdef _DEBUG
    bool m_fCoMemoryAllocated;

    ILWSTRMarshaler()
    {
        LIMITED_METHOD_CONTRACT;
        m_fCoMemoryAllocated = false;
    }
#endif // _DEBUG

    
    virtual bool SupportsArgumentMarshal(DWORD dwMarshalFlags, UINT* pErrorResID)
    {
        if (IsOut(dwMarshalFlags) && !IsByref(dwMarshalFlags) && IsCLRToNative(dwMarshalFlags))
        {
            *pErrorResID = IDS_EE_BADMARSHAL_STRING_OUT;
            return false;
        }

        return true;
    }

protected:
    virtual LocalDesc GetNativeType();
    virtual LocalDesc GetManagedType();

    virtual void EmitConvertSpaceCLRToNative(ILCodeStream* pslILEmit);
    virtual void EmitConvertContentsCLRToNative(ILCodeStream* pslILEmit);
    virtual void EmitConvertSpaceAndContentsCLRToNative(ILCodeStream* pslILEmit);
    virtual void EmitConvertSpaceAndContentsCLRToNativeTemp(ILCodeStream* pslILEmit);

    virtual void EmitConvertSpaceNativeToCLR(ILCodeStream* pslILEmit);
    virtual void EmitConvertContentsNativeToCLR(ILCodeStream* pslILEmit);

    virtual bool NeedsClearNative();
    virtual void EmitClearNative(ILCodeStream* pslILEmit);
    virtual void EmitClearNativeTemp(ILCodeStream* pslILEmit);

    static bool CanUsePinnedManagedString(DWORD dwMarshalFlags);
    static void EmitCheckManagedStringLength(ILCodeStream* pslILEmit);
    static void EmitCheckNativeStringLength(ILCodeStream* pslILEmit);
};

// A marshaler that makes run-time decision based on argument size whether native space will
// be allocated using localloc or on the heap. The ctor argument is a heap free function.
class ILOptimizedAllocMarshaler : public ILMarshaler
{
public:
    ILOptimizedAllocMarshaler(BinderMethodID clearNat) :
        m_idClearNative(clearNat),
        m_dwLocalBuffer((DWORD)-1)
    {
        LIMITED_METHOD_CONTRACT;
    }

    virtual LocalDesc GetNativeType();
    virtual bool NeedsClearNative();
    virtual void EmitClearNative(ILCodeStream* pslILEmit);

protected:
    const BinderMethodID m_idClearNative;
    DWORD m_dwLocalBuffer;      // localloc'ed temp buffer variable or -1 if not used
};

class ILUTF8BufferMarshaler : public ILOptimizedAllocMarshaler
{
public:
	enum
	{
		c_fInOnly = FALSE,
		c_nativeSize = sizeof(void *),
		c_CLRSize = sizeof(OBJECTREF),
	};

	enum
	{
		// If required buffer length > MAX_LOCAL_BUFFER_LENGTH, don't optimize by allocating memory on stack
		MAX_LOCAL_BUFFER_LENGTH = MAX_PATH_FNAME + 1
	};

	ILUTF8BufferMarshaler() :
		ILOptimizedAllocMarshaler(METHOD__MARSHAL__FREE_CO_TASK_MEM)
	{
		LIMITED_METHOD_CONTRACT;
	}

	virtual LocalDesc GetManagedType();
	virtual void EmitConvertSpaceCLRToNative(ILCodeStream* pslILEmit);
	virtual void EmitConvertContentsCLRToNative(ILCodeStream* pslILEmit);
	virtual void EmitConvertSpaceNativeToCLR(ILCodeStream* pslILEmit);
	virtual void EmitConvertContentsNativeToCLR(ILCodeStream* pslILEmit);
};

class ILWSTRBufferMarshaler : public ILOptimizedAllocMarshaler
{
public:
    enum
    {
        c_fInOnly               = FALSE,
        c_nativeSize            = sizeof(void *),
        c_CLRSize               = sizeof(OBJECTREF),
    };

    enum
    {
        // If required buffer length > MAX_LOCAL_BUFFER_LENGTH, don't optimize by allocating memory on stack
        MAX_LOCAL_BUFFER_LENGTH = (MAX_PATH_FNAME + 1) * 2
    };

    ILWSTRBufferMarshaler() :
        ILOptimizedAllocMarshaler(METHOD__MARSHAL__FREE_CO_TASK_MEM)
    {
        LIMITED_METHOD_CONTRACT;
    }

    virtual LocalDesc GetManagedType();
    virtual void EmitConvertSpaceCLRToNative(ILCodeStream* pslILEmit);
    virtual void EmitConvertContentsCLRToNative(ILCodeStream* pslILEmit);
    virtual void EmitConvertSpaceNativeToCLR(ILCodeStream* pslILEmit);
    virtual void EmitConvertContentsNativeToCLR(ILCodeStream* pslILEmit);
};

class ILCSTRBufferMarshaler : public ILOptimizedAllocMarshaler
{
public:
    enum
    {
        c_fInOnly               = FALSE,
        c_nativeSize            = sizeof(void *),
        c_CLRSize               = sizeof(OBJECTREF),
    };

    enum
    {
        // If required buffer length > MAX_LOCAL_BUFFER_LENGTH, don't optimize by allocating memory on stack
        MAX_LOCAL_BUFFER_LENGTH = MAX_PATH_FNAME + 1
    };

    ILCSTRBufferMarshaler() :
        ILOptimizedAllocMarshaler(METHOD__MARSHAL__FREE_CO_TASK_MEM)
    {
        LIMITED_METHOD_CONTRACT;
    }

    virtual LocalDesc GetManagedType();
    virtual void EmitConvertSpaceCLRToNative(ILCodeStream* pslILEmit);
    virtual void EmitConvertContentsCLRToNative(ILCodeStream* pslILEmit);
    virtual void EmitConvertSpaceNativeToCLR(ILCodeStream* pslILEmit);
    virtual void EmitConvertContentsNativeToCLR(ILCodeStream* pslILEmit);
};

        
class ILHandleRefMarshaler : public ILMarshaler
{
    // Managed layout for SRI.HandleRef class
    struct HANDLEREF
    {
        OBJECTREF m_wrapper;
        LPVOID    m_handle;
    };

public:
    enum
    {
        c_fInOnly               = FALSE,
        c_nativeSize            = sizeof(LPVOID),
        c_CLRSize               = sizeof(HANDLEREF),
    };
        
    LocalDesc GetManagedType()
    {
        LIMITED_METHOD_CONTRACT;
        return LocalDesc();
    }
        
    LocalDesc GetNativeType()
    {
        LIMITED_METHOD_CONTRACT;
        return LocalDesc();
    }

    static MarshalerOverrideStatus ArgumentOverride(NDirectStubLinker* psl,
                                                    BOOL               byref,
                                                    BOOL               fin,
                                                    BOOL               fout,
                                                    BOOL               fManagedToNative,
                                                    OverrideProcArgs*  pargs,
                                                    UINT*              pResID,
                                                    UINT               argidx,
                                                    UINT               nativeStackOffset);

    static MarshalerOverrideStatus ReturnOverride(NDirectStubLinker* psl,
                                                  BOOL               fManagedToNative,
                                                  BOOL               fHresultSwap,
                                                  OverrideProcArgs*  pargs,
                                                  UINT*              pResID);
};

class ILSafeHandleMarshaler : public ILMarshaler
{
public:
    enum
    {
        c_fInOnly               = FALSE,
        c_nativeSize            = sizeof(LPVOID),
        c_CLRSize               = sizeof(SAFEHANDLE),
    };

    virtual LocalDesc GetManagedType();
    virtual LocalDesc GetNativeType();

    virtual bool NeedsClearNative();
    virtual void EmitClearNative(ILCodeStream* pslILEmit);

    virtual void EmitMarshalArgumentCLRToNative();

    static MarshalerOverrideStatus ArgumentOverride(NDirectStubLinker* psl,
                                                    BOOL               byref,
                                                    BOOL               fin,
                                                    BOOL               fout,
                                                    BOOL               fManagedToNative,
                                                    OverrideProcArgs*  pargs,
                                                    UINT*              pResID,
                                                    UINT               argidx,
                                                    UINT               nativeStackOffset);
        
    static MarshalerOverrideStatus ReturnOverride(NDirectStubLinker *psl,
                                                  BOOL        fManagedToNative,
                                                  BOOL        fHresultSwap,
                                                  OverrideProcArgs *pargs,
                                                  UINT       *pResID);
};


class ILCriticalHandleMarshaler : public ILMarshaler
{
public:
    enum
    {
        c_fInOnly               = FALSE,
        c_nativeSize            = sizeof(LPVOID),
        c_CLRSize               = sizeof(CRITICALHANDLE),
    };
        
public:

    LocalDesc GetManagedType()
    {
        LIMITED_METHOD_CONTRACT;
        return LocalDesc();
    }
    
    LocalDesc GetNativeType()
    {
        LIMITED_METHOD_CONTRACT;
        return LocalDesc();
    }
    
    static MarshalerOverrideStatus ArgumentOverride(NDirectStubLinker* psl,
                                                    BOOL               byref,
                                                    BOOL               fin,
                                                    BOOL               fout,
                                                    BOOL               fManagedToNative,
                                                    OverrideProcArgs*  pargs,
                                                    UINT*              pResID,
                                                    UINT               argidx,
                                                    UINT               nativeStackOffset);

    static MarshalerOverrideStatus ReturnOverride(NDirectStubLinker *psl,
                                                  BOOL        fManagedToNative,
                                                  BOOL        fHresultSwap,
                                                  OverrideProcArgs *pargs,
                                                  UINT       *pResID);
};


class ILValueClassMarshaler : public ILMarshaler
{
public:
    enum
    {
        c_fInOnly               = TRUE,
        c_nativeSize            = VARIABLESIZE,
        c_CLRSize               = VARIABLESIZE,
    };

protected:
    virtual LocalDesc GetNativeType();
    virtual LocalDesc GetManagedType();
    virtual void EmitReInitNative(ILCodeStream* pslILEmit);
    virtual bool NeedsClearNative();
    virtual void EmitClearNative(ILCodeStream * pslILEmit);
    virtual void EmitConvertContentsCLRToNative(ILCodeStream* pslILEmit);
    virtual void EmitConvertContentsNativeToCLR(ILCodeStream* pslILEmit);
};
        
#ifdef FEATURE_COMINTEROP
class ILObjectMarshaler : public ILMarshaler
{
public:
    enum
    {
        c_fInOnly               = TRUE,
        c_CLRSize               = sizeof(OBJECTREF),
        c_nativeSize            = sizeof(VARIANT),
    };

protected:
    virtual LocalDesc GetNativeType();
    virtual LocalDesc GetManagedType();
    virtual void EmitConvertContentsCLRToNative(ILCodeStream* pslILEmit);
    virtual void EmitConvertContentsNativeToCLR(ILCodeStream* pslILEmit);
    virtual bool NeedsClearNative();
    virtual void EmitClearNative(ILCodeStream* pslILEmit);
    virtual void EmitReInitNative(ILCodeStream* pslILEmit);
};
#endif // FEATURE_COMINTEROP

class ILDateMarshaler : public ILMarshaler
{
public:
    enum
    {
        c_fInOnly               = TRUE,
        c_nativeSize            = sizeof(DATE),
        c_CLRSize               = sizeof(INT64),
    };
                
protected:
    virtual LocalDesc GetNativeType();
    virtual LocalDesc GetManagedType();
    virtual void EmitConvertContentsCLRToNative(ILCodeStream* pslILEmit);
    virtual void EmitConvertContentsNativeToCLR(ILCodeStream* pslILEmit);
    virtual void EmitReInitNative(ILCodeStream* pslILEmit);
};
                

class ILCurrencyMarshaler : public ILMarshaler
{
public:
    enum
    {
        c_fInOnly               = TRUE,
        c_nativeSize            = sizeof(CURRENCY),
        c_CLRSize               = sizeof(DECIMAL),
    };

protected:    
    virtual LocalDesc GetNativeType();
    virtual LocalDesc GetManagedType();
    virtual void EmitReInitNative(ILCodeStream* pslILEmit);
    virtual void EmitConvertContentsCLRToNative(ILCodeStream* pslILEmit);
    virtual void EmitConvertContentsNativeToCLR(ILCodeStream* pslILEmit);
};


#ifdef FEATURE_COMINTEROP
class ILInterfaceMarshaler : public ILMarshaler
{
public:
    enum
    {
        c_fInOnly               = TRUE,
        c_nativeSize            = sizeof(void *),
        c_CLRSize               = sizeof(OBJECTREF),
    };

protected:    
    virtual LocalDesc GetNativeType();
    virtual LocalDesc GetManagedType();
    virtual void EmitConvertContentsCLRToNative(ILCodeStream* pslILEmit);
    virtual void EmitConvertContentsNativeToCLR(ILCodeStream* pslILEmit);
    virtual bool NeedsClearNative();
    virtual void EmitClearNative(ILCodeStream* pslILEmit);
};
#endif // FEATURE_COMINTEROP


class ILAnsiCharMarshaler : public ILMarshaler
{
public:
    enum
    {
        c_fInOnly               = TRUE,
        c_nativeSize            = sizeof(UINT8),
        c_CLRSize               = sizeof(UINT16),
    };

protected:
    virtual LocalDesc GetNativeType();
    virtual LocalDesc GetManagedType();
    virtual void EmitConvertContentsCLRToNative(ILCodeStream* pslILEmit);
    virtual void EmitConvertContentsNativeToCLR(ILCodeStream* pslILEmit);
};


template <BinderClassID CLASS__ID, class ELEMENT>
class ILValueClassPtrMarshaler : public ILMarshaler
{
public:
    enum
    {
        c_fInOnly               = TRUE,
        c_nativeSize            = sizeof(ELEMENT *),
        c_CLRSize               = sizeof(ELEMENT),
    };

protected:
    virtual LocalDesc GetNativeType()
    {
        LIMITED_METHOD_CONTRACT;

        //
        // pointer to value class
        //
        return LocalDesc(ELEMENT_TYPE_I);
    }

    virtual LocalDesc GetManagedType()
    {
        STANDARD_VM_CONTRACT;

        //
        // value class
        //
        return LocalDesc(MscorlibBinder::GetClass(CLASS__ID));
    }

    virtual bool NeedsClearNative()
    {
        LIMITED_METHOD_CONTRACT;
        return (IsByref(m_dwMarshalFlags) && IsOut(m_dwMarshalFlags));
    }

    virtual void EmitClearNative(ILCodeStream* pslILEmit)
    {
        STANDARD_VM_CONTRACT;

        EmitLoadNativeValue(pslILEmit);
        // static void CoTaskMemFree(IntPtr ptr)
        pslILEmit->EmitCALL(METHOD__MARSHAL__FREE_CO_TASK_MEM, 1, 0);
    }

    virtual void EmitConvertSpaceCLRToNative(ILCodeStream* pslILEmit)
    {
        STANDARD_VM_CONTRACT;

        if (NeedsClearNative())
        {
            pslILEmit->EmitLDC(sizeof(ELEMENT));
            pslILEmit->EmitCONV_U();
            // static IntPtr CoTaskMemAlloc(UIntPtr cb)
            pslILEmit->EmitCALL(METHOD__MARSHAL__ALLOC_CO_TASK_MEM, 1, 1);
            EmitStoreNativeValue(pslILEmit);
        }
    }

    virtual void EmitConvertContentsCLRToNative(ILCodeStream* pslILEmit)
    {
        STANDARD_VM_CONTRACT;

        if (NeedsClearNative())
        {
            EmitLoadNativeValue(pslILEmit);     // dest
            EmitLoadManagedHomeAddr(pslILEmit); // src
            pslILEmit->EmitCPOBJ(pslILEmit->GetToken(MscorlibBinder::GetClass(CLASS__ID)));
        }
        else
        {
            EmitLoadManagedHomeAddr(pslILEmit);
            EmitStoreNativeValue(pslILEmit);
        }
    }

    virtual void EmitConvertContentsNativeToCLR(ILCodeStream* pslILEmit)
    {
        STANDARD_VM_CONTRACT;

        int tokType = pslILEmit->GetToken(MscorlibBinder::GetClass(CLASS__ID));
        ILCodeLabel *pNullLabel = pslILEmit->NewCodeLabel();
        ILCodeLabel *pJoinLabel = pslILEmit->NewCodeLabel();

        EmitLoadNativeValue(pslILEmit);
        pslILEmit->EmitBRFALSE(pNullLabel);

        // the incoming pointer is non-null -> dereference it and copy the struct
        EmitLoadManagedHomeAddr(pslILEmit); // dest
        EmitLoadNativeValue(pslILEmit);     // src
        pslILEmit->EmitCPOBJ(tokType);

        pslILEmit->EmitBR(pJoinLabel);

        // the incoming pointer is null -> just initobj (i.e. zero) the struct
        pslILEmit->EmitLabel(pNullLabel);
        
        EmitLoadManagedHomeAddr(pslILEmit);
        pslILEmit->EmitINITOBJ(tokType);

        pslILEmit->EmitLabel(pJoinLabel);
    }
};

typedef ILValueClassPtrMarshaler<CLASS__GUID, GUID> ILGuidPtrMarshaler;
typedef ILValueClassPtrMarshaler<CLASS__DECIMAL, DECIMAL> ILDecimalPtrMarshaler;
        
#ifdef FEATURE_COMINTEROP        
class ILOleColorMarshaler : public ILMarshaler
{
public:
    enum
    {
        c_fInOnly               = TRUE,
        c_nativeSize            = sizeof(OLE_COLOR),
        c_CLRSize               = sizeof(SYSTEMCOLOR),
    };

protected:
    virtual LocalDesc GetNativeType();
    virtual LocalDesc GetManagedType();
    virtual void EmitConvertContentsCLRToNative(ILCodeStream* pslILEmit);
    virtual void EmitConvertContentsNativeToCLR(ILCodeStream* pslILEmit);
};

class ILVBByValStrWMarshaler : public ILMarshaler
{
public:
    enum
    {
        c_fInOnly               = FALSE,
        c_nativeSize            = sizeof(BSTR),
        c_CLRSize               = sizeof(OBJECTREF*),
    };

    enum
    {
        // If required buffer length > MAX_LOCAL_BUFFER_LENGTH, don't optimize by allocating memory on stack
        MAX_LOCAL_BUFFER_LENGTH = (MAX_PATH_FNAME + 1) * 2 + sizeof(DWORD)
    };


    ILVBByValStrWMarshaler() : 
        m_dwCCHLocal(-1)
       ,m_dwLocalBuffer(-1)
    {
        LIMITED_METHOD_CONTRACT;
    }

    virtual bool SupportsArgumentMarshal(DWORD dwMarshalFlags, UINT* pErrorResID);
    virtual bool SupportsReturnMarshal(DWORD dwMarshalFlags, UINT* pErrorResID);

protected:
    virtual LocalDesc GetNativeType();
    virtual LocalDesc GetManagedType();
    virtual void EmitConvertContentsCLRToNative(ILCodeStream* pslILEmit);
    virtual void EmitConvertContentsNativeToCLR(ILCodeStream* pslILEmit);
    virtual bool NeedsClearNative();
    virtual void EmitClearNative(ILCodeStream* pslILEmit);
    virtual bool IsNativePassedByRef();
        
    DWORD m_dwCCHLocal;
    DWORD m_dwLocalBuffer;
};

class ILVBByValStrMarshaler : public ILMarshaler
{
public:
    enum
    {
        c_fInOnly               = FALSE,
        c_nativeSize            = sizeof(LPSTR),
        c_CLRSize               = sizeof(OBJECTREF *),
    };

    ILVBByValStrMarshaler() :
        m_dwCCHLocal(-1)
    {
        LIMITED_METHOD_CONTRACT;
    }

    virtual bool SupportsArgumentMarshal(DWORD dwMarshalFlags, UINT* pErrorResID);
    virtual bool SupportsReturnMarshal(DWORD dwMarshalFlags, UINT* pErrorResID);

protected:
    virtual LocalDesc GetNativeType();
    virtual LocalDesc GetManagedType();
    virtual void EmitConvertContentsCLRToNative(ILCodeStream* pslILEmit);
    virtual void EmitConvertContentsNativeToCLR(ILCodeStream* pslILEmit);
    virtual bool NeedsClearNative();
    virtual void EmitClearNative(ILCodeStream* pslILEmit);
    virtual bool IsNativePassedByRef();

    DWORD m_dwCCHLocal;
};

class ILHSTRINGMarshaler : public ILMarshaler
{
public:
    enum
    {
        c_fInOnly               = FALSE,
        c_nativeSize            = sizeof(HSTRING),
        c_CLRSize               = sizeof(OBJECTREF),
    };

protected:
    virtual LocalDesc GetNativeType();
    virtual LocalDesc GetManagedType();

    virtual void EmitConvertContentsCLRToNative(ILCodeStream* pslILEmit);
    void EmitConvertCLRToHSTRINGReference(ILCodeStream* pslILEmit);
    void EmitConvertCLRToHSTRING(ILCodeStream* pslILEmit);

    virtual void EmitConvertContentsNativeToCLR(ILCodeStream* pslILEmit);

    virtual bool NeedsClearNative();
    virtual void EmitClearNative(ILCodeStream* pslILEmit);
};
#endif // FEATURE_COMINTEROP


class ILCUTF8Marshaler : public ILOptimizedAllocMarshaler
{
public:
	enum
	{
		c_fInOnly = TRUE,
		c_nativeSize = sizeof(void *),
		c_CLRSize = sizeof(OBJECTREF),
	};

	enum
	{
		// If required buffer length > MAX_LOCAL_BUFFER_LENGTH, don't optimize by allocating memory on stack
		MAX_LOCAL_BUFFER_LENGTH = MAX_PATH_FNAME + 1
	};

	ILCUTF8Marshaler() :
		ILOptimizedAllocMarshaler(METHOD__CSTRMARSHALER__CLEAR_NATIVE)
	{
		LIMITED_METHOD_CONTRACT;
	}

protected:
	virtual LocalDesc GetManagedType();
	virtual void EmitConvertContentsCLRToNative(ILCodeStream* pslILEmit);
	virtual void EmitConvertContentsNativeToCLR(ILCodeStream* pslILEmit);
};



class ILCSTRMarshaler : public ILOptimizedAllocMarshaler
{
public:
    enum
    {
        c_fInOnly               = TRUE,
        c_nativeSize            = sizeof(void *),
        c_CLRSize               = sizeof(OBJECTREF),
    };

    enum
    {
        // If required buffer length > MAX_LOCAL_BUFFER_LENGTH, don't optimize by allocating memory on stack
        MAX_LOCAL_BUFFER_LENGTH = MAX_PATH_FNAME + 1
    };

    ILCSTRMarshaler() :
        ILOptimizedAllocMarshaler(METHOD__CSTRMARSHALER__CLEAR_NATIVE)
    {
        LIMITED_METHOD_CONTRACT;
    }

protected:    
    virtual LocalDesc GetManagedType();
    virtual void EmitConvertContentsCLRToNative(ILCodeStream* pslILEmit);
    virtual void EmitConvertContentsNativeToCLR(ILCodeStream* pslILEmit);
};

class ILBSTRMarshaler : public ILOptimizedAllocMarshaler
{
public:
    enum
    {
        c_fInOnly               = TRUE,
        c_nativeSize            = sizeof(void *),
        c_CLRSize               = sizeof(OBJECTREF),
    };

    enum
    {
        // If required buffer length > MAX_LOCAL_BUFFER_LENGTH, don't optimize by allocating memory on stack
        MAX_LOCAL_BUFFER_LENGTH = (MAX_PATH_FNAME + 1) * 2 + 4
    };

    ILBSTRMarshaler() :
        ILOptimizedAllocMarshaler(METHOD__BSTRMARSHALER__CLEAR_NATIVE)
    {
        LIMITED_METHOD_CONTRACT;
    }

protected:    
    virtual LocalDesc GetManagedType();
    virtual void EmitConvertContentsCLRToNative(ILCodeStream* pslILEmit);
    virtual void EmitConvertContentsNativeToCLR(ILCodeStream* pslILEmit);
};

class ILAnsiBSTRMarshaler : public ILMarshaler
{
public:
    enum
    {
        c_fInOnly               = TRUE,
        c_nativeSize            = sizeof(void *),
        c_CLRSize               = sizeof(OBJECTREF),
    };

protected:    
    virtual LocalDesc GetNativeType();
    virtual LocalDesc GetManagedType();
    virtual void EmitConvertContentsCLRToNative(ILCodeStream* pslILEmit);
    virtual void EmitConvertContentsNativeToCLR(ILCodeStream* pslILEmit);
    virtual bool NeedsClearNative();
    virtual void EmitClearNative(ILCodeStream* pslILEmit);
};

class ILLayoutClassPtrMarshalerBase : public ILMarshaler
{
public:
    enum
    {
        c_nativeSize            = sizeof(void *),
        c_CLRSize               = sizeof(OBJECTREF),
    };

protected:    
    virtual LocalDesc GetNativeType();
    virtual LocalDesc GetManagedType();
    virtual void EmitConvertSpaceCLRToNative(ILCodeStream* pslILEmit);
    virtual void EmitConvertSpaceCLRToNativeTemp(ILCodeStream* pslILEmit);
    virtual void EmitConvertSpaceAndContentsCLRToNativeTemp(ILCodeStream* pslILEmit);
    virtual void EmitConvertSpaceNativeToCLR(ILCodeStream* pslILEmit);
    virtual bool NeedsClearNative();
    virtual void EmitClearNative(ILCodeStream* pslILEmit);
    virtual void EmitClearNativeTemp(ILCodeStream* pslILEmit);
};

class ILLayoutClassPtrMarshaler : public ILLayoutClassPtrMarshalerBase
{
public:
    enum
    {
        c_fInOnly               = FALSE,
    };
        
protected:    
    virtual void EmitConvertContentsCLRToNative(ILCodeStream* pslILEmit);
    virtual void EmitConvertContentsNativeToCLR(ILCodeStream* pslILEmit);
    virtual void EmitClearNativeContents(ILCodeStream * pslILEmit);
};

class ILBlittablePtrMarshaler : public ILLayoutClassPtrMarshalerBase
{
public:
    enum
    {
        c_fInOnly               = FALSE,
    };
            
protected:    
    virtual void EmitMarshalArgumentCLRToNative();
    virtual void EmitConvertContentsCLRToNative(ILCodeStream* pslILEmit);
    virtual void EmitConvertContentsNativeToCLR(ILCodeStream* pslILEmit);
};

class ILBlittableValueClassWithCopyCtorMarshaler : public ILMarshaler
{
public:
    enum
    {
        c_fInOnly               = TRUE,
        c_nativeSize            = VARIABLESIZE,
        c_CLRSize               = sizeof(OBJECTREF),
    };

    LocalDesc GetManagedType()
    {
        LIMITED_METHOD_CONTRACT;
        return LocalDesc();
    }

    LocalDesc GetNativeType()
    {
        LIMITED_METHOD_CONTRACT;
        return LocalDesc();
    }

    static MarshalerOverrideStatus ArgumentOverride(NDirectStubLinker* psl,
                                            BOOL               byref,
                                            BOOL               fin,
                                            BOOL               fout,
                                            BOOL               fManagedToNative,
                                            OverrideProcArgs*  pargs,
                                            UINT*              pResID,
                                            UINT               argidx,
                                            UINT               nativeStackOffset);


};

class ILArgIteratorMarshaler : public ILMarshaler
{
public:
    enum
    {
        c_fInOnly               = TRUE,
        c_nativeSize            = sizeof(va_list),
        c_CLRSize               = sizeof(VARARGS),
    };

protected:
    virtual LocalDesc GetNativeType();
    virtual LocalDesc GetManagedType();
    virtual bool SupportsArgumentMarshal(DWORD dwMarshalFlags, UINT* pErrorResID);
    virtual void EmitMarshalArgumentCLRToNative();
    virtual void EmitMarshalArgumentNativeToCLR();
};
        
class ILArrayWithOffsetMarshaler : public ILMarshaler
{
public:
    enum
    {
        c_fInOnly               = FALSE,
        c_nativeSize            = sizeof(LPVOID),
        c_CLRSize               = sizeof(ArrayWithOffsetData),
    };

    ILArrayWithOffsetMarshaler() : 
        m_dwCountLocalNum(-1),
        m_dwOffsetLocalNum(-1),
        m_dwPinnedLocalNum(-1)
    {
        LIMITED_METHOD_CONTRACT;
    }

protected:
    virtual LocalDesc GetNativeType();
    virtual LocalDesc GetManagedType();
    virtual bool SupportsArgumentMarshal(DWORD dwMarshalFlags, UINT* pErrorResID);

    virtual void EmitConvertSpaceAndContentsCLRToNativeTemp(ILCodeStream* pslILEmit);
    virtual void EmitConvertContentsNativeToCLR(ILCodeStream* pslILEmit);
    virtual void EmitClearNativeTemp(ILCodeStream* pslILEmit);

        
    DWORD m_dwCountLocalNum;
    DWORD m_dwOffsetLocalNum;
    DWORD m_dwPinnedLocalNum;
};

class ILAsAnyMarshalerBase : public ILMarshaler
{
public:
    enum
    {
        c_nativeSize            = sizeof(void *),
        c_CLRSize               = sizeof(OBJECTREF),
    };

    ILAsAnyMarshalerBase() :
        m_dwMarshalerLocalNum(-1)
    {
        LIMITED_METHOD_CONTRACT;
    }

protected:
    static const BYTE ML_IN  = 0x10;
    static const BYTE ML_OUT = 0x20;

    virtual bool IsAnsi() = 0;
    virtual LocalDesc GetNativeType();
    virtual LocalDesc GetManagedType();
    virtual bool SupportsArgumentMarshal(DWORD dwMarshalFlags, UINT* pErrorResID);
    virtual bool SupportsReturnMarshal(DWORD dwMarshalFlags, UINT* pErrorResID);
    virtual void EmitMarshalArgumentCLRToNative();
    virtual bool NeedsClearNative();
    virtual void EmitClearNativeTemp(ILCodeStream* pslILEmit);

    DWORD m_dwMarshalerLocalNum;
};

class ILAsAnyWMarshaler : public ILAsAnyMarshalerBase
{
public:
    enum
    {
        c_fInOnly               = FALSE,
    };

protected:
    virtual bool IsAnsi() 
    {
        return false;
    }
};

class ILAsAnyAMarshaler : public ILAsAnyMarshalerBase
{
public:
    enum
    {
        c_fInOnly               = FALSE,
    };

protected:
    virtual bool IsAnsi() 
    {
        return true;
    }
};


class ILMngdMarshaler : public ILMarshaler
{
public:
    enum
    {
        c_nativeSize            = sizeof(void *),
        c_CLRSize               = sizeof(OBJECTREF),
    };

    ILMngdMarshaler(BinderMethodID space2Man, 
                    BinderMethodID contents2Man, 
                    BinderMethodID space2Nat, 
                    BinderMethodID contents2Nat, 
                    BinderMethodID clearNat, 
                    BinderMethodID clearNatContents,
                    BinderMethodID clearMan) :
        m_idConvertSpaceToManaged(space2Man),
        m_idConvertContentsToManaged(contents2Man),
        m_idConvertSpaceToNative(space2Nat),
        m_idConvertContentsToNative(contents2Nat),
        m_idClearNative(clearNat),
        m_idClearNativeContents(clearNatContents),
        m_idClearManaged(clearMan)
    {
        LIMITED_METHOD_CONTRACT;
    }
    
protected:    
    virtual LocalDesc GetNativeType();
    virtual LocalDesc GetManagedType();

    virtual void EmitCreateMngdMarshaler(ILCodeStream* pslILEmit) = 0;

    virtual void EmitCallMngdMarshalerMethod(ILCodeStream* pslILEmit, MethodDesc *pMD);

    virtual void EmitConvertSpaceNativeToCLR(ILCodeStream* pslILEmit)
    {
        WRAPPER_NO_CONTRACT;
        EmitCallMngdMarshalerMethod(pslILEmit, GetConvertSpaceToManagedMethod());
    }
    
    virtual void EmitConvertContentsNativeToCLR(ILCodeStream* pslILEmit)
    {
        WRAPPER_NO_CONTRACT;
        EmitCallMngdMarshalerMethod(pslILEmit, GetConvertContentsToManagedMethod());
    }
    
    virtual void EmitConvertSpaceCLRToNative(ILCodeStream* pslILEmit)
    {
        WRAPPER_NO_CONTRACT;
        EmitCallMngdMarshalerMethod(pslILEmit, GetConvertSpaceToNativeMethod());
    }
    
    virtual void EmitConvertContentsCLRToNative(ILCodeStream* pslILEmit)
    {
        WRAPPER_NO_CONTRACT;
        EmitCallMngdMarshalerMethod(pslILEmit, GetConvertContentsToNativeMethod());
    }

    virtual bool NeedsClearNative()
    {
        LIMITED_METHOD_CONTRACT;

        if (NULL != GetClearNativeMethod())
        {
            return true;
        }
            
        return false;
    }
    
    virtual void EmitClearNative(ILCodeStream* pslILEmit)
    {
        WRAPPER_NO_CONTRACT;
        EmitCallMngdMarshalerMethod(pslILEmit, GetClearNativeMethod());
    }
    
    virtual void EmitClearNativeContents(ILCodeStream* pslILEmit)
    {
        WRAPPER_NO_CONTRACT;
        EmitCallMngdMarshalerMethod(pslILEmit, GetClearNativeContentsMethod());
    }

    
    virtual bool NeedsClearCLR()
    {
        LIMITED_METHOD_CONTRACT;

        if (NULL != GetClearManagedMethod())
        {
            return true;
        }
            
        return false;
    }

    virtual void EmitClearCLR(ILCodeStream* pslILEmit)
    {
        WRAPPER_NO_CONTRACT;
        EmitCallMngdMarshalerMethod(pslILEmit, GetClearManagedMethod());
    }

    virtual MethodDesc *GetConvertSpaceToManagedMethod()    { WRAPPER_NO_CONTRACT; return (m_idConvertSpaceToManaged    == METHOD__NIL ? NULL : MscorlibBinder::GetMethod(m_idConvertSpaceToManaged));    }
    virtual MethodDesc *GetConvertContentsToManagedMethod() { WRAPPER_NO_CONTRACT; return (m_idConvertContentsToManaged == METHOD__NIL ? NULL : MscorlibBinder::GetMethod(m_idConvertContentsToManaged)); }
    virtual MethodDesc *GetConvertSpaceToNativeMethod()     { WRAPPER_NO_CONTRACT; return (m_idConvertSpaceToNative     == METHOD__NIL ? NULL : MscorlibBinder::GetMethod(m_idConvertSpaceToNative));     }
    virtual MethodDesc *GetConvertContentsToNativeMethod()  { WRAPPER_NO_CONTRACT; return (m_idConvertContentsToNative  == METHOD__NIL ? NULL : MscorlibBinder::GetMethod(m_idConvertContentsToNative));  }
    virtual MethodDesc *GetClearNativeMethod()              { WRAPPER_NO_CONTRACT; return (m_idClearNative              == METHOD__NIL ? NULL : MscorlibBinder::GetMethod(m_idClearNative));              }
    virtual MethodDesc *GetClearNativeContentsMethod()      { WRAPPER_NO_CONTRACT; return (m_idClearNativeContents      == METHOD__NIL ? NULL : MscorlibBinder::GetMethod(m_idClearNativeContents));      }
    virtual MethodDesc *GetClearManagedMethod()             { WRAPPER_NO_CONTRACT; return (m_idClearManaged             == METHOD__NIL ? NULL : MscorlibBinder::GetMethod(m_idClearManaged));             }

    const BinderMethodID m_idConvertSpaceToManaged;
    const BinderMethodID m_idConvertContentsToManaged;
    const BinderMethodID m_idConvertSpaceToNative;
    const BinderMethodID m_idConvertContentsToNative;
    const BinderMethodID m_idClearNative;
    const BinderMethodID m_idClearNativeContents;
    const BinderMethodID m_idClearManaged;
};

class ILNativeArrayMarshaler : public ILMngdMarshaler
{
public:    
    enum
    {
        c_fInOnly               = FALSE,
    };

    ILNativeArrayMarshaler() : 
        ILMngdMarshaler(
            METHOD__MNGD_NATIVE_ARRAY_MARSHALER__CONVERT_SPACE_TO_MANAGED,
            METHOD__MNGD_NATIVE_ARRAY_MARSHALER__CONVERT_CONTENTS_TO_MANAGED,
            METHOD__MNGD_NATIVE_ARRAY_MARSHALER__CONVERT_SPACE_TO_NATIVE,
            METHOD__MNGD_NATIVE_ARRAY_MARSHALER__CONVERT_CONTENTS_TO_NATIVE,
            METHOD__MNGD_NATIVE_ARRAY_MARSHALER__CLEAR_NATIVE,
            METHOD__MNGD_NATIVE_ARRAY_MARSHALER__CLEAR_NATIVE_CONTENTS,
            METHOD__NIL
            )
    {
        LIMITED_METHOD_CONTRACT;
        m_dwSavedSizeArg = LOCAL_NUM_UNUSED;
    }

    virtual void EmitMarshalArgumentCLRToNative();
    virtual void EmitConvertSpaceNativeToCLR(ILCodeStream* pslILEmit);
    virtual void EmitConvertSpaceCLRToNative(ILCodeStream* pslILEmit);
    virtual void EmitClearNative(ILCodeStream* pslILEmit);    
    virtual void EmitClearNativeContents(ILCodeStream* pslILEmit);
    virtual void EmitMarshalArgumentNativeToCLRByref();
    virtual void EmitMarshalArgumentCLRToNativeByref();
    
protected:
    
    bool UsePinnedArraySpecialCase();
    
    BOOL CheckSizeParamIndexArg(const CREATE_MARSHALER_CARRAY_OPERANDS &mops, CorElementType *pElementType);
    
    // Calculate element count and load it on evaluation stack
    void EmitLoadElementCount(ILCodeStream* pslILEmit);    

    virtual void EmitCreateMngdMarshaler(ILCodeStream* pslILEmit);

    void EmitLoadNativeSize(ILCodeStream* pslILEmit);
    void EmitNewSavedSizeArgLocal();
    
private :
    DWORD m_dwSavedSizeArg;                 
};

class MngdNativeArrayMarshaler
{
public:
    static FCDECL3(void, CreateMarshaler,           MngdNativeArrayMarshaler* pThis, MethodTable* pMT, UINT32 dwFlags);
    static FCDECL3(void, ConvertSpaceToNative,      MngdNativeArrayMarshaler* pThis, OBJECTREF* pManagedHome, void** pNativeHome);
    static FCDECL3(void, ConvertContentsToNative,   MngdNativeArrayMarshaler* pThis, OBJECTREF* pManagedHome, void** pNativeHome);
    static FCDECL4(void, ConvertSpaceToManaged,     MngdNativeArrayMarshaler* pThis, OBJECTREF* pManagedHome, void** pNativeHome, INT32 cElements);
    static FCDECL3(void, ConvertContentsToManaged,  MngdNativeArrayMarshaler* pThis, OBJECTREF* pManagedHome, void** pNativeHome);
    static FCDECL3(void, ClearNative,               MngdNativeArrayMarshaler* pThis, void** pNativeHome, INT32 cElements);
    static FCDECL3(void, ClearNativeContents,       MngdNativeArrayMarshaler* pThis, void** pNativeHome, INT32 cElements);

    static void DoClearNativeContents(MngdNativeArrayMarshaler* pThis, void** pNativeHome, INT32 cElements);
        
    enum
    {
        FLAG_NATIVE_DATA_VALID = 0x40000000
    };

    MethodTable*            m_pElementMT;
    TypeHandle              m_Array;
    BOOL                    m_NativeDataValid;
    BOOL                    m_BestFitMap;
    BOOL                    m_ThrowOnUnmappableChar;
    VARTYPE                 m_vt;
};


#ifdef FEATURE_COMINTEROP
class ILSafeArrayMarshaler : public ILMngdMarshaler
{
public:    
    enum
    {
        c_fInOnly               = FALSE,
    };

    ILSafeArrayMarshaler() : 
        ILMngdMarshaler(
            METHOD__MNGD_SAFE_ARRAY_MARSHALER__CONVERT_SPACE_TO_MANAGED,
            METHOD__MNGD_SAFE_ARRAY_MARSHALER__CONVERT_CONTENTS_TO_MANAGED,
            METHOD__MNGD_SAFE_ARRAY_MARSHALER__CONVERT_SPACE_TO_NATIVE,
            METHOD__MNGD_SAFE_ARRAY_MARSHALER__CONVERT_CONTENTS_TO_NATIVE,
            METHOD__MNGD_SAFE_ARRAY_MARSHALER__CLEAR_NATIVE,
            METHOD__NIL,
            METHOD__NIL
            ), 
        m_dwOriginalManagedLocalNum(-1)
    {
        LIMITED_METHOD_CONTRACT;
    }
    
protected:

    virtual void EmitCreateMngdMarshaler(ILCodeStream* pslILEmit);
    virtual void EmitConvertContentsNativeToCLR(ILCodeStream* pslILEmit);
    virtual void EmitConvertContentsCLRToNative(ILCodeStream* pslILEmit);

    virtual void EmitReInitNative(ILCodeStream* pslILEmit)
    {
        CONTRACTL
        {
            THROWS;
            GC_TRIGGERS;
            MODE_ANY;
        }
        CONTRACTL_END;
        if (NeedsCheckForStatic() && pslILEmit->GetStreamType() != ILStubLinker::kExceptionCleanup)
        {
            // Keep the original value in native home as we are not going to allocate a new
            // one. If we cleared it here, we wouldn't be able to ConvertContentsToNative.
            // Always perform the real re-init in the ExceptionCleanup stream so the caller
            // doesn't get back garbage.
        }
        else
        {
            ILMngdMarshaler::EmitReInitNative(pslILEmit);
        }
    }

    bool NeedsCheckForStatic()
    {
        WRAPPER_NO_CONTRACT;
        return IsByref(m_dwMarshalFlags) && !IsCLRToNative(m_dwMarshalFlags) && IsIn(m_dwMarshalFlags) && IsOut(m_dwMarshalFlags);
    }

    DWORD m_dwOriginalManagedLocalNum;
};

class MngdSafeArrayMarshaler
{
public:
    static FCDECL4(void, CreateMarshaler,           MngdSafeArrayMarshaler* pThis, MethodTable* pMT, UINT32 iRank, UINT32 dwFlags);
    static FCDECL3(void, ConvertSpaceToNative,      MngdSafeArrayMarshaler* pThis, OBJECTREF* pManagedHome, void** pNativeHome);
    static FCDECL4(void, ConvertContentsToNative,   MngdSafeArrayMarshaler* pThis, OBJECTREF* pManagedHome, void** pNativeHome, Object* pOriginalManagedUNSAFE);
    static FCDECL3(void, ConvertSpaceToManaged,     MngdSafeArrayMarshaler* pThis, OBJECTREF* pManagedHome, void** pNativeHome);
    static FCDECL3(void, ConvertContentsToManaged,  MngdSafeArrayMarshaler* pThis, OBJECTREF* pManagedHome, void** pNativeHome);
    static FCDECL3(void, ClearNative,               MngdSafeArrayMarshaler* pThis, OBJECTREF* pManagedHome, void** pNativeHome);

    enum StaticCheckStateFlags
    {
        SCSF_CheckForStatic = 1,
        SCSF_IsStatic = 2,
        SCSF_NativeDataValid = 4
    };

    MethodTable*    m_pElementMT;
    int             m_iRank;
    VARTYPE         m_vt;
    BYTE            m_fStatic;     // StaticCheckStateFlags
    BYTE            m_nolowerbounds;
};

class ILHiddenLengthArrayMarshaler : public ILMngdMarshaler
{
    friend class MngdHiddenLengthArrayMarshaler;

public:
    enum
    {
        c_nativeSize            = sizeof(LPVOID),
        c_CLRSize               = sizeof(OBJECTREF),
        c_fInOnly               = FALSE,
    };

    ILHiddenLengthArrayMarshaler() :
        ILMngdMarshaler(METHOD__MNGD_HIDDEN_LENGTH_ARRAY_MARSHALER__CONVERT_SPACE_TO_MANAGED,
                        METHOD__MNGD_HIDDEN_LENGTH_ARRAY_MARSHALER__CONVERT_CONTENTS_TO_MANAGED,
                        METHOD__MNGD_HIDDEN_LENGTH_ARRAY_MARSHALER__CONVERT_SPACE_TO_NATIVE,
                        METHOD__MNGD_HIDDEN_LENGTH_ARRAY_MARSHALER__CONVERT_CONTENTS_TO_NATIVE,
                        METHOD__MARSHAL__FREE_CO_TASK_MEM,
                        METHOD__MNGD_HIDDEN_LENGTH_ARRAY_MARSHALER__CLEAR_NATIVE_CONTENTS,
                        METHOD__NIL)
    {
        LIMITED_METHOD_CONTRACT;
        m_dwMngdMarshalerLocalNum = -1;
    }

protected:
    virtual LocalDesc GetNativeType();
    virtual LocalDesc GetManagedType();

    virtual void EmitCreateMngdMarshaler(ILCodeStream* pslILEmit);
    virtual void EmitMarshalArgumentCLRToNative();
    virtual void EmitConvertSpaceCLRToNative(ILCodeStream* pslILEmit);
    virtual void EmitConvertSpaceNativeToCLR(ILCodeStream* pslILEmit);
    virtual void EmitConvertContentsCLRToNative(ILCodeStream* pslILEmit);
    virtual void EmitConvertContentsNativeToCLR(ILCodeStream* pslILEmit);
    virtual void EmitClearNative(ILCodeStream* pslILEmit);    
    virtual void EmitClearNativeContents(ILCodeStream* pslILEmit);

private:
    bool CanUsePinnedArray();
    void EmitLoadNativeArrayLength(ILCodeStream *pslILEmit);

    virtual MethodDesc *GetConvertContentsToManagedMethod();
    virtual MethodDesc *GetConvertContentsToNativeMethod();
    virtual MethodDesc *GetClearNativeContentsMethod();

    MethodDesc *GetExactMarshalerMethod(MethodDesc *pGenericMD);
};

class MngdHiddenLengthArrayMarshaler
{
public:
    static FCDECL4(void, CreateMarshaler,           MngdHiddenLengthArrayMarshaler* pThis, MethodTable* pMT, SIZE_T cbElement, UINT16 vt);
    static FCDECL3(void, ConvertSpaceToNative,      MngdHiddenLengthArrayMarshaler* pThis, OBJECTREF* pManagedHome, void** pNativeHome);
    static FCDECL3(void, ConvertContentsToNative,   MngdHiddenLengthArrayMarshaler* pThis, OBJECTREF* pManagedHome, void** pNativeHome);
    static FCDECL4(void, ConvertSpaceToManaged,     MngdHiddenLengthArrayMarshaler* pThis, OBJECTREF* pManagedHome, void** pNativeHome, INT32 cElements);
    static FCDECL3(void, ConvertContentsToManaged,  MngdHiddenLengthArrayMarshaler* pThis, OBJECTREF* pManagedHome, void** pNativeHome);
    static FCDECL3(void, ClearNativeContents,       MngdHiddenLengthArrayMarshaler* pThis, void** pNativeHome, INT32 cElements);


private:
    SIZE_T GetArraySize(SIZE_T elements);
    void DoClearNativeContents(void** pNativeHome, INT32 cElements);

private:
    MethodTable                         *m_pElementMT;
    SIZE_T                               m_cbElementSize;
    VARTYPE                              m_vt;
};
#endif // FEATURE_COMINTEROP


class ILReferenceCustomMarshaler : public ILMngdMarshaler
{
public:    
    enum
    {
        c_fInOnly               = FALSE,
    };

    ILReferenceCustomMarshaler() : 
        ILMngdMarshaler(
            METHOD__NIL,
            METHOD__MNGD_REF_CUSTOM_MARSHALER__CONVERT_CONTENTS_TO_MANAGED,
            METHOD__NIL,
            METHOD__MNGD_REF_CUSTOM_MARSHALER__CONVERT_CONTENTS_TO_NATIVE,
            METHOD__MNGD_REF_CUSTOM_MARSHALER__CLEAR_NATIVE,
            METHOD__NIL,
            METHOD__MNGD_REF_CUSTOM_MARSHALER__CLEAR_MANAGED
            )
    {
        LIMITED_METHOD_CONTRACT;
    }
        
protected:
    virtual void EmitCreateMngdMarshaler(ILCodeStream* pslILEmit);
};

class MngdRefCustomMarshaler
{
public:
    static FCDECL2(void, CreateMarshaler,           MngdRefCustomMarshaler* pThis, void* pCMHelper);
    static FCDECL3(void, ConvertContentsToNative,   MngdRefCustomMarshaler* pThis, OBJECTREF* pManagedHome, void** pNativeHome);
    static FCDECL3(void, ConvertContentsToManaged,  MngdRefCustomMarshaler* pThis, OBJECTREF* pManagedHome, void** pNativeHome);
    static FCDECL3(void, ClearNative,               MngdRefCustomMarshaler* pThis, OBJECTREF* pManagedHome, void** pNativeHome);
    static FCDECL3(void, ClearManaged,              MngdRefCustomMarshaler* pThis, OBJECTREF* pManagedHome, void** pNativeHome);

    static void DoClearNativeContents(MngdRefCustomMarshaler* pThis, OBJECTREF* pManagedHome, void** pNativeHome);

    CustomMarshalerHelper*  m_pCMHelper;
};


#ifdef FEATURE_COMINTEROP
class ILUriMarshaler : public ILMarshaler
{
public:
    enum
    {
        c_fInOnly               = TRUE,
        c_nativeSize            = sizeof(LPVOID),
        c_CLRSize               = sizeof(OBJECTREF),
    };

    static void EmitConvertCLRUriToWinRTUri(ILCodeStream* pslILEmit, LoaderAllocator* pLoader);
    static void EmitConvertWinRTUriToCLRUri(ILCodeStream* pslILEmit, LoaderAllocator* pLoader);

protected:
    virtual LocalDesc GetNativeType();
    virtual LocalDesc GetManagedType();

    virtual void EmitConvertContentsCLRToNative(ILCodeStream* pslILEmit);    
    virtual void EmitConvertContentsNativeToCLR(ILCodeStream* pslILEmit);

    virtual bool NeedsClearNative();
    void EmitClearNative(ILCodeStream* pslILEmit);
};

class ILNCCEventArgsMarshaler : public ILMarshaler
{
public:
    enum
    {
        c_fInOnly               = TRUE,
        c_nativeSize            = sizeof(LPVOID),
        c_CLRSize               = sizeof(OBJECTREF),
    };

    static void EmitConvertCLREventArgsToWinRTEventArgs(ILCodeStream* pslILEmit, LoaderAllocator* pLoader);
    static void EmitConvertWinRTEventArgsToCLREventArgs(ILCodeStream* pslILEmit, LoaderAllocator* pLoader);

protected:
    virtual LocalDesc GetNativeType();
    virtual LocalDesc GetManagedType();

    virtual void EmitConvertContentsCLRToNative(ILCodeStream* pslILEmit);    
    virtual void EmitConvertContentsNativeToCLR(ILCodeStream* pslILEmit);

    virtual bool NeedsClearNative();
    void EmitClearNative(ILCodeStream* pslILEmit);
};

class ILPCEventArgsMarshaler : public ILMarshaler
{
public:
    enum
    {
        c_fInOnly               = TRUE,
        c_nativeSize            = sizeof(LPVOID),
        c_CLRSize               = sizeof(OBJECTREF),
    };

    static void EmitConvertCLREventArgsToWinRTEventArgs(ILCodeStream* pslILEmit, LoaderAllocator* pLoader);
    static void EmitConvertWinRTEventArgsToCLREventArgs(ILCodeStream* pslILEmit, LoaderAllocator* pLoader);

protected:
    virtual LocalDesc GetNativeType();
    virtual LocalDesc GetManagedType();

    virtual void EmitConvertContentsCLRToNative(ILCodeStream* pslILEmit);    
    virtual void EmitConvertContentsNativeToCLR(ILCodeStream* pslILEmit);

    virtual bool NeedsClearNative();
    void EmitClearNative(ILCodeStream* pslILEmit);
};

class ILDateTimeMarshaler : public ILMarshaler
{
public:
    enum
    {
        c_fInOnly               = FALSE,
        c_nativeSize            = sizeof(INT64),  // = sizeof(Windows::Foundation::DateTime)
        c_CLRSize               = VARIABLESIZE,
    };

protected:
    virtual LocalDesc GetNativeType();
    virtual LocalDesc GetManagedType();

    virtual void EmitConvertContentsCLRToNative(ILCodeStream* pslILEmit);    
    virtual void EmitConvertContentsNativeToCLR(ILCodeStream* pslILEmit);

    virtual bool NeedsClearNative();
    virtual void EmitReInitNative(ILCodeStream* pslILEmit);
};

class ILNullableMarshaler : public ILMarshaler
{
public:
    enum
    {
        c_fInOnly               = FALSE,
        c_nativeSize            = sizeof(LPVOID),
        c_CLRSize               = VARIABLESIZE,
    };
                
protected:
    virtual LocalDesc GetNativeType();
    virtual LocalDesc GetManagedType();
    virtual bool NeedsClearNative();
    virtual void EmitConvertContentsCLRToNative(ILCodeStream* pslILEmit);
    virtual void EmitConvertContentsNativeToCLR(ILCodeStream* pslILEmit);    
    virtual void EmitClearNative(ILCodeStream* pslILEmit);    

private:
    MethodDesc *GetExactMarshalerMethod(MethodDesc *pGenericMD);
};

class ILSystemTypeMarshaler : public ILMarshaler
{
public:
    enum
    {
        c_fInOnly           = FALSE, 
        c_nativeSize        = sizeof(TypeNameNative),
        c_CLRSize           = sizeof(OBJECTREF)
    };

protected:
    virtual LocalDesc GetNativeType();
    virtual LocalDesc GetManagedType();

    virtual void EmitConvertContentsCLRToNative(ILCodeStream * pslILEmit);
    virtual void EmitConvertContentsNativeToCLR(ILCodeStream * pslILEmit);

    virtual bool NeedsClearNative();
    virtual void EmitClearNative(ILCodeStream * pslILEmit);
    virtual void EmitReInitNative(ILCodeStream * pslILEmit);
};

class ILHResultExceptionMarshaler : public ILMarshaler
{
public:
    enum
    {
        c_fInOnly               = FALSE,
        c_nativeSize            = sizeof(INT32),  // = sizeof(Windows::Foundation::HResult)
        c_CLRSize               = sizeof(OBJECTREF),
    };

protected:
    virtual LocalDesc GetNativeType();
    virtual LocalDesc GetManagedType();

    virtual void EmitConvertContentsCLRToNative(ILCodeStream* pslILEmit);    
    virtual void EmitConvertContentsNativeToCLR(ILCodeStream* pslILEmit);

    virtual bool NeedsClearNative();
};

class ILKeyValuePairMarshaler : public ILMarshaler
{
public:
    enum
    {
        c_fInOnly               = FALSE,
        c_nativeSize            = sizeof(LPVOID),
        c_CLRSize               = VARIABLESIZE,
    };
                
protected:
    virtual LocalDesc GetNativeType();
    virtual LocalDesc GetManagedType();
    virtual bool NeedsClearNative();
    virtual void EmitConvertContentsCLRToNative(ILCodeStream* pslILEmit);
    virtual void EmitConvertContentsNativeToCLR(ILCodeStream* pslILEmit);
    virtual void EmitClearNative(ILCodeStream* pslILEmit);

private:
    MethodDesc *GetExactMarshalerMethod(MethodDesc *pGenericMD);
};

#endif // FEATURE_COMINTEROP
