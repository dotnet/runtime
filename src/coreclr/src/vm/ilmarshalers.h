// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
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
    typedef enum : byte
    {
        HomeType_Unspecified     = 0,
        HomeType_ILLocal         = 1,
        HomeType_ILArgument      = 2,
        HomeType_ILByrefLocal    = 3,
        HomeType_ILByrefArgument = 4
    } MarshalHomeType;

private:
    DWORD               m_dwHomeIndex;
    LocalDesc           m_locDesc;
    MarshalHomeType     m_homeType;
    bool                m_hasTypeInfo = false;
    bool                m_unalignedIndirectStore;

    void EmitUnalignedPrefixIfNeeded(ILCodeStream* pslILEmit)
    {
        if (m_unalignedIndirectStore)
        {
            pslILEmit->EmitUNALIGNED(1);
        }
    }

public:
    void InitHome(MarshalHomeType homeType, DWORD dwHomeIndex, LocalDesc* pLocDesc = nullptr, bool unalignedIndirectStore = false)
    {
        LIMITED_METHOD_CONTRACT;

        m_homeType = homeType;
        m_dwHomeIndex = dwHomeIndex;
        if (pLocDesc != nullptr)
        {
            m_hasTypeInfo = true;
            m_locDesc = *pLocDesc;
        }
        m_unalignedIndirectStore = unalignedIndirectStore;
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
            case HomeType_ILByrefLocal:
                CONSISTENCY_CHECK_MSG(m_hasTypeInfo, "Cannot load or store the value into a ILStub byref home without type information.");
                pslILEmit->EmitLDLOC(m_dwHomeIndex);
                EmitUnalignedPrefixIfNeeded(pslILEmit);
                pslILEmit->EmitLDIND_T(&m_locDesc);
                break;
            case HomeType_ILByrefArgument:
                CONSISTENCY_CHECK_MSG(m_hasTypeInfo, "Cannot load or store the value into a ILStub byref home without type information.");
                pslILEmit->EmitLDARG(m_dwHomeIndex);
                EmitUnalignedPrefixIfNeeded(pslILEmit);
                pslILEmit->EmitLDIND_T(&m_locDesc);
                break;

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
            case HomeType_ILByrefLocal:
            {
                CONSISTENCY_CHECK_MSG(m_hasTypeInfo, "Cannot load or store the value into a ILStub byref home without type information.");
                DWORD swapLocal = pslILEmit->NewLocal(m_locDesc);
                pslILEmit->EmitSTLOC(swapLocal);
                pslILEmit->EmitLDLOC(m_dwHomeIndex);
                pslILEmit->EmitLDLOC(swapLocal);
                EmitUnalignedPrefixIfNeeded(pslILEmit);
                pslILEmit->EmitSTIND_T(&m_locDesc);
                break;
            }
            case HomeType_ILByrefArgument:
            {
                CONSISTENCY_CHECK_MSG(m_hasTypeInfo, "Cannot load or store the value into a ILStub byref home without type information.");
                DWORD swapLocal = pslILEmit->NewLocal(m_locDesc);
                pslILEmit->EmitSTLOC(swapLocal);
                pslILEmit->EmitLDARG(m_dwHomeIndex);
                pslILEmit->EmitLDLOC(swapLocal);
                EmitUnalignedPrefixIfNeeded(pslILEmit);
                pslILEmit->EmitSTIND_T(&m_locDesc);
                break;
            }


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
            EmitUnalignedPrefixIfNeeded(pslILEmit);
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
            EmitUnalignedPrefixIfNeeded(pslILEmit);
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
    UINT                m_argidx;
    DWORD               m_dwMarshalFlags;
    DWORD               m_dwMngdMarshalerLocalNum;

private:
    NDirectStubLinker* m_pslNDirect;
    ILCodeStream*       m_pcsMarshal;
    ILCodeStream*       m_pcsUnmarshal;
    ILStubMarshalHome   m_nativeHome;
    ILStubMarshalHome   m_managedHome;

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

private:
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
        m_dwMngdMarshalerLocalNum = LOCAL_NUM_UNUSED;
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

    static inline bool IsInMemberFunction(DWORD dwMarshalFlags)
    {
        LIMITED_METHOD_CONTRACT;
        return (0 != (dwMarshalFlags & MARSHAL_FLAG_IN_MEMBER_FUNCTION));
    }

    static inline bool IsFieldMarshal(DWORD dwMarshalFlags)
    {
        LIMITED_METHOD_CONTRACT;
        return (0 != (dwMarshalFlags & MARSHAL_FLAG_FIELD));
    }

    static void EmitLoadNativeLocalAddrForByRefDispatch(ILCodeStream* pslILEmit, DWORD local)
    {
        WRAPPER_NO_CONTRACT;
        pslILEmit->EmitLDLOCA(local);

        // Convert the loaded local containing a native address
        // into a non-GC type for the byref case.
        pslILEmit->EmitCONV_I();
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

    void EmitLoadNativeHomeAddrForByRefDispatch(ILCodeStream* pslILEmit)
    {
        WRAPPER_NO_CONTRACT;
        EmitLoadNativeHomeAddr(pslILEmit);

        // Convert the loaded value containing a native address
        // into a non-GC type for the byref case.
        pslILEmit->EmitCONV_I();
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

    void EmitLogNativeArgument(ILCodeStream* pslILEmit, DWORD dwPinnedLocal)
    {
        WRAPPER_NO_CONTRACT;
        if (g_pConfig->InteropLogArguments())
        {
            m_pslNDirect->EmitLogNativeArgument(pslILEmit, dwPinnedLocal);
        }
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

    virtual bool SupportsFieldMarshal(UINT* pErrorResID)
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

    void EmitMarshalField(
                ILCodeStream*   pcsMarshal,
                ILCodeStream*   pcsUnmarshal,
                UINT            argidx,
                UINT32          managedOffset,
                UINT32          nativeOffset,
                OverrideProcArgs*  pargs)
    {
        STANDARD_VM_CONTRACT;

        // Struct marshaling stubs are always in, and out
        // since we generate a single stub for all three operations (managed->native, native->managed, cleanup)
        // we set the clr-to-native flag so the marshal phase is CLR->Native and the unmarshal phase is Native->CLR
        Init(pcsMarshal, pcsUnmarshal, argidx, MARSHAL_FLAG_IN | MARSHAL_FLAG_OUT | MARSHAL_FLAG_CLR_TO_NATIVE | MARSHAL_FLAG_FIELD, pargs);

        EmitCreateMngdMarshaler(m_pslNDirect->GetSetupCodeStream());

        EmitSetupArgumentForMarshalling(m_pslNDirect->GetSetupCodeStream());

        EmitSetupDefaultHomesForField(
            m_pslNDirect->GetSetupCodeStream(),
            managedOffset,
            nativeOffset);

        EmitMarshalFieldSpaceAndContents();
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
        // Some marshalers have a managed marshaler for the general path but can pin on the fast path.
        // If we're in a scenario where this marshaler can pin on a by-value managed->native call,
        // we know that we don't need a managed marshaler since we will just pin.
        if (!CanMarshalViaPinning())
        {
            EmitCreateMngdMarshaler(m_pslNDirect->GetSetupCodeStream());
        }

        EmitSetupArgumentForMarshalling(m_pslNDirect->GetSetupCodeStream());

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

    void EmitSetupArgumentForDispatch(ILCodeStream* pslILEmit)
    {
        STANDARD_VM_CONTRACT;

        if (IsCLRToNative(m_dwMarshalFlags))
        {
            if (IsNativePassedByRef())
            {
                EmitLoadNativeHomeAddrForByRefDispatch(pslILEmit);
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

    void EmitMarshalReturnValue(
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
        bool nativeMethodIsMemberFunction = IsInMemberFunction(dwMarshalFlags);

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

#if defined(TARGET_WINDOWS)
            // JIT32 and JIT64 (which is only used on the Windows Desktop CLR) has a problem generating
            // code for the pinvoke ILStubs which do a return using a struct type.  Therefore, we
            // change the signature of calli to return void and make the return buffer as first argument.

            // For Windows, we need to use a return buffer for native member functions returning structures.
            // On Windows arm we need to respect HFAs and not use a return buffer if the return type is an HFA
            // for X86 Windows non-member functions we bash the return type from struct to U1, U2, U4 or U8
            // and use byrefNativeReturn for all other structs.
            if (nativeMethodIsMemberFunction)
            {
#ifdef TARGET_ARM
                byrefNativeReturn = !nativeType.InternalToken.GetMethodTable()->IsNativeHFA();
#else
                byrefNativeReturn = true;
#endif
            }
            else
            {
#ifdef TARGET_X86
                switch (nativeSize)
                {
                    case 1: typ = ELEMENT_TYPE_U1; break;
                    case 2: typ = ELEMENT_TYPE_U2; break;
                    case 4: typ = ELEMENT_TYPE_U4; break;
                    case 8: typ = ELEMENT_TYPE_U8; break;
                    default: byrefNativeReturn = true; break;
                }
#endif // TARGET_X86
            }
#endif // defined(TARGET_WINDOWS)

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
                EmitLoadNativeHomeAddrForByRefDispatch(pcsDispatch);    // load up the byref native type as an extra arg
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

    void EmitLoadMngdMarshaler(ILCodeStream* pslILEmit)
    {
        CONTRACTL
        {
            THROWS;
            GC_TRIGGERS;
            MODE_ANY;
        }
        CONTRACTL_END;

        CONSISTENCY_CHECK(LOCAL_NUM_UNUSED != m_dwMngdMarshalerLocalNum);
        pslILEmit->EmitLDLOC(m_dwMngdMarshalerLocalNum);
    }

    void EmitLoadMngdMarshalerAddr(ILCodeStream* pslILEmit)
    {
        CONTRACTL
        {
            THROWS;
            GC_TRIGGERS;
            MODE_ANY;
        }
        CONTRACTL_END;

        CONSISTENCY_CHECK(LOCAL_NUM_UNUSED != m_dwMngdMarshalerLocalNum);
        pslILEmit->EmitLDLOCA(m_dwMngdMarshalerLocalNum);
    }

    void EmitLoadCleanupWorkList(ILCodeStream* pslILEmit)
    {
        m_pslNDirect->LoadCleanupWorkList(pslILEmit);
    }

    int GetLCIDParamIndex()
    {
        return m_pslNDirect->GetLCIDParamIdx();
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

    void EmitMarshalArgumentCLRToNative()
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

        if (CanMarshalViaPinning())
        {
            // If we can marshal via pinning, all we need to do to marshal is pin.
            EmitMarshalViaPinning(m_pcsMarshal);
        }
        else
        {
            //
            // marshal
            //
            if (IsIn(m_dwMarshalFlags) || AlwaysConvertByValContentsCLRToNative())
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

    void EmitSetupDefaultHomesForField(ILCodeStream* pcsSetup, UINT32 managedOffset, UINT32 nativeOffset)
    {
        LocalDesc managedType(GetManagedType());
        LocalDesc managedFieldTypeByRef(GetManagedType());
        managedFieldTypeByRef.MakeByRef();

        m_managedHome.InitHome(ILStubMarshalHome::HomeType_ILByrefLocal, pcsSetup->NewLocal(managedFieldTypeByRef), &managedType, /* unalignedIndirectStore */ true);

        LocalDesc nativeType(GetNativeType());
        LocalDesc nativeFieldTypeByRef(GetNativeType());
        nativeFieldTypeByRef.MakeByRef();

        m_nativeHome.InitHome(ILStubMarshalHome::HomeType_ILByrefLocal, pcsSetup->NewLocal(nativeFieldTypeByRef), &nativeType, /* unalignedIndirectStore */ true);

        pcsSetup->EmitNOP("// field setup {");
        pcsSetup->EmitNOP("// managed field setup {");
        pcsSetup->EmitLDARG(StructMarshalStubs::MANAGED_STRUCT_ARGIDX);
        pcsSetup->EmitLDC(managedOffset);
        pcsSetup->EmitADD();
        EmitStoreManagedHomeAddr(pcsSetup);
        pcsSetup->EmitNOP("// } managed field setup");
        pcsSetup->EmitNOP("// native field setup {");

        pcsSetup->EmitLDARG(StructMarshalStubs::NATIVE_STRUCT_ARGIDX);
        pcsSetup->EmitLDC(nativeOffset);
        pcsSetup->EmitADD();
        EmitStoreNativeHomeAddr(pcsSetup);
        pcsSetup->EmitNOP("// } native field setup");
        pcsSetup->EmitNOP("// } field setup");
    }

    virtual void EmitMarshalFieldSpaceAndContents()
    {
        STANDARD_VM_CONTRACT;

        EmitConvertSpaceAndContentsCLRToNative(m_pcsMarshal);
        EmitConvertSpaceAndContentsNativeToCLR(m_pcsUnmarshal);
        if (NeedsClearNative())
        {
            ILCodeStream* pcsCleanup = m_pslNDirect->GetCleanupCodeStream();
            EmitClearNative(pcsCleanup);
        }
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

    void EmitMarshalArgumentAddressCLRToNative()
    {
        EmitLoadManagedHomeAddr(m_pcsMarshal);
        EmitStoreNativeHomeAddr(m_pcsMarshal);
    }

    void EmitMarshalArgumentAddressNativeToCLR()
    {
        EmitLoadNativeHomeAddr(m_pcsMarshal);
        EmitStoreManagedHomeAddr(m_pcsMarshal);
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

    void EmitMarshalArgumentNativeToCLR()
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

    virtual void EmitSetupArgumentForMarshalling(ILCodeStream* pslILEmit)
    {
        LIMITED_METHOD_CONTRACT;
    }

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

    // Emits marshalling code to allocate space and convert a value from a CLR value to a native value.
    // Usable in situations where temporary (i.e. pinned or stack-allocated) space is usable.
    // For marshalling scenarios that require heap-allocated space, call EmitConvertSpaceAndContentsCLRToNative.
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

    virtual bool CanMarshalViaPinning()
    {
        LIMITED_METHOD_CONTRACT;
        return false;
    }

    virtual void EmitMarshalViaPinning(ILCodeStream* pslILEmit)
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

    bool IsManagedPassedByRef()
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

    virtual bool AlwaysConvertByValContentsCLRToNative()
    {
        LIMITED_METHOD_CONTRACT;
        return false;
    }

    virtual void EmitLoadValueToKeepAlive(ILCodeStream* pslILEmit)
    {
        LIMITED_METHOD_CONTRACT;
        EmitLoadManagedValue(pslILEmit);
    }

    void EmitKeepAliveManagedValue()
    {
        // Don't use the cleanup work list to avoid any extra allocations.
        m_pslNDirect->SetCleanupNeeded();

        ILCodeStream* pslILEmit = m_pslNDirect->GetCleanupCodeStream();

        ILCodeLabel* pNoManagedValueLabel = nullptr;
        if (IsFieldMarshal(m_dwMarshalFlags))
        {
            pNoManagedValueLabel = pslILEmit->NewCodeLabel();
            pslILEmit->EmitLDARG(StructMarshalStubs::MANAGED_STRUCT_ARGIDX);
            pslILEmit->EmitBRFALSE(pNoManagedValueLabel);
        }

        EmitLoadValueToKeepAlive(pslILEmit);
        pslILEmit->EmitCALL(METHOD__GC__KEEP_ALIVE, 1, 0);

        if (IsFieldMarshal(m_dwMarshalFlags))
        {
            pslILEmit->EmitLabel(pNoManagedValueLabel);
        }
    }

public:

    // Extension point to allow a marshaler to conditionally override all of the ILMarshaler logic with its own or block marshalling when marshalling an argument.
    // See MarshalInfo::GetArgumentOverrideProc for the implementation.
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

    // Extension point to allow a marshaler to conditionally override all of the ILMarshaler logic with its own or block marshalling when marshalling a return value.
    // See MarshalInfo::GetReturnOverrideProc for the implementation.
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
    LocalDesc GetManagedType() override
    {
        WRAPPER_NO_CONTRACT;
        return GetNativeType();
    }

    void EmitConvertContentsCLRToNative(ILCodeStream* pslILEmit) override
    {
        STANDARD_VM_CONTRACT;

        EmitLoadManagedValue(pslILEmit);
        EmitStoreNativeValue(pslILEmit);
    }

    void EmitConvertContentsNativeToCLR(ILCodeStream* pslILEmit) override
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
    void EmitMarshalArgumentCLRToNativeByref() override
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
        EmitMarshalArgumentAddressCLRToNative();

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
    void EmitMarshalArgumentNativeToCLRByref() override
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
        EmitMarshalArgumentAddressNativeToCLR();

        //
        // no unmarshaling is necessary since we directly passed the pointer to managed
        // as a byref, the argument is therefore automatically in/out
        //
    }
};

template <CorElementType ELEMENT_TYPE, class PROMOTED_ELEMENT, UINT32 NATIVE_SIZE>
class ILCopyMarshalerSimple : public ILCopyMarshalerBase
{
public:
    enum
    {
        c_fInOnly               = TRUE,
        c_nativeSize            = NATIVE_SIZE,
    };

    bool IsSmallValueTypeSpecialCase()
    {
        //
        // Special case for small value types that get
        // mapped to MARSHAL_TYPE_GENERIC_8 -- use the
        // valuetype type so the JIT is happy.
        //

        return (ELEMENT_TYPE ==
#ifdef TARGET_64BIT
                    ELEMENT_TYPE_I8
#else // TARGET_64BIT
                    ELEMENT_TYPE_I4
#endif // TARGET_64BIT
                    ) && (NULL != m_pargs->m_pMT);
    }

    bool NeedToPromoteTo8Bytes()
    {
        WRAPPER_NO_CONTRACT;

#if defined(TARGET_AMD64)
        // If the argument is passed by value,
        if (!IsByref(m_dwMarshalFlags) && !IsRetval(m_dwMarshalFlags) && !IsFieldMarshal(m_dwMarshalFlags))
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
#endif // TARGET_AMD64

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

typedef ILCopyMarshalerSimple<ELEMENT_TYPE_I1, INT_PTR,  TARGET_POINTER_SIZE> ILCopyMarshaler1;
typedef ILCopyMarshalerSimple<ELEMENT_TYPE_U1, UINT_PTR, TARGET_POINTER_SIZE> ILCopyMarshalerU1;
typedef ILCopyMarshalerSimple<ELEMENT_TYPE_I2, INT_PTR,  TARGET_POINTER_SIZE> ILCopyMarshaler2;
typedef ILCopyMarshalerSimple<ELEMENT_TYPE_U2, UINT_PTR, TARGET_POINTER_SIZE> ILCopyMarshalerU2;
typedef ILCopyMarshalerSimple<ELEMENT_TYPE_I4, INT_PTR,  TARGET_POINTER_SIZE> ILCopyMarshaler4;
typedef ILCopyMarshalerSimple<ELEMENT_TYPE_U4, UINT_PTR, TARGET_POINTER_SIZE> ILCopyMarshalerU4;
typedef ILCopyMarshalerSimple<ELEMENT_TYPE_I8, INT64,    8>                   ILCopyMarshaler8;
typedef ILCopyMarshalerSimple<ELEMENT_TYPE_R4, float,    4>                   ILFloatMarshaler;
typedef ILCopyMarshalerSimple<ELEMENT_TYPE_R8, double,   8>                   ILDoubleMarshaler;

template <BinderClassID CLASS__ID, class PROMOTED_ELEMENT>
class ILCopyMarshalerKnownStruct : public ILCopyMarshalerBase
{
public:
    enum
    {
        c_fInOnly               = TRUE,
        c_nativeSize            = sizeof(PROMOTED_ELEMENT),
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
        c_nativeSize            = TARGET_POINTER_SIZE,
    };

protected:
    LocalDesc GetNativeType() override;
    LocalDesc GetManagedType() override;
    void EmitConvertContentsCLRToNative(ILCodeStream* pslILEmit) override;
    void EmitConvertContentsNativeToCLR(ILCodeStream* pslILEmit) override;
};

class ILReflectionObjectMarshaler : public ILMarshaler
{
public:
    enum
    {
        c_fInOnly               = TRUE,
        c_nativeSize            = TARGET_POINTER_SIZE,
    };

protected:
    LocalDesc GetManagedType() override;
    LocalDesc GetNativeType() override;
    void EmitConvertContentsCLRToNative(ILCodeStream* pslILEmit) override;
    void EmitConvertContentsNativeToCLR(ILCodeStream* pslILEmit) override;
    virtual BinderFieldID GetStructureFieldID() { LIMITED_METHOD_CONTRACT; return (BinderFieldID)0; }
    virtual BinderFieldID GetObjectFieldID() = 0;
    virtual BinderClassID GetManagedTypeBinderID() = 0;
    void EmitLoadValueToKeepAlive(ILCodeStream* pslILEmit) override
    {
        BinderFieldID structField = GetStructureFieldID();

        // This marshaler can generate code for marshaling an object containing a handle, and for
        // marshaling a struct referring to an object containing a handle.
        if (structField != 0)
        {
            int tokStruct__m_object = pslILEmit->GetToken(MscorlibBinder::GetField(structField));
            EmitLoadManagedHomeAddr(pslILEmit);
            pslILEmit->EmitLDFLD(tokStruct__m_object);
        }
        else
        {
            EmitLoadManagedValue(pslILEmit);
        }
    }
};

class ILRuntimeTypeHandleMarshaler : public ILReflectionObjectMarshaler
{
protected:
    BinderFieldID GetStructureFieldID() override { LIMITED_METHOD_CONTRACT; return FIELD__RT_TYPE_HANDLE__M_TYPE; }
    BinderFieldID GetObjectFieldID() override { LIMITED_METHOD_CONTRACT; return FIELD__CLASS__TYPEHANDLE; }
    BinderClassID GetManagedTypeBinderID() override { LIMITED_METHOD_CONTRACT; return CLASS__RT_TYPE_HANDLE; }
};

class ILRuntimeMethodHandleMarshaler : public ILReflectionObjectMarshaler
{
protected:
    BinderFieldID GetStructureFieldID() override { LIMITED_METHOD_CONTRACT; return FIELD__METHOD_HANDLE__METHOD; }
    BinderFieldID GetObjectFieldID() override { LIMITED_METHOD_CONTRACT; return FIELD__STUBMETHODINFO__HANDLE; }
    BinderClassID GetManagedTypeBinderID() override { LIMITED_METHOD_CONTRACT; return CLASS__METHOD_HANDLE; }
};

class ILRuntimeFieldHandleMarshaler : public ILReflectionObjectMarshaler
{
protected:
    BinderFieldID GetStructureFieldID() override { LIMITED_METHOD_CONTRACT; return FIELD__FIELD_HANDLE__M_FIELD; }
    BinderFieldID GetObjectFieldID() override { LIMITED_METHOD_CONTRACT; return FIELD__RT_FIELD_INFO__HANDLE; }
    BinderClassID GetManagedTypeBinderID() override { LIMITED_METHOD_CONTRACT; return CLASS__FIELD_HANDLE; }
};

class ILBoolMarshaler : public ILMarshaler
{
public:

    virtual CorElementType GetNativeBoolElementType() = 0;
    virtual int GetNativeTrueValue() = 0;
    virtual int GetNativeFalseValue() = 0;

protected:
    LocalDesc GetNativeType() override;
    LocalDesc GetManagedType() override;
    void EmitConvertContentsCLRToNative(ILCodeStream* pslILEmit) override;
    void EmitConvertContentsNativeToCLR(ILCodeStream* pslILEmit) override;
};

class ILWinBoolMarshaler : public ILBoolMarshaler
{
public:
    enum
    {
        c_fInOnly               = TRUE,
        c_nativeSize            = sizeof(BOOL),
    };

protected:
    CorElementType GetNativeBoolElementType() override
    {
        LIMITED_METHOD_CONTRACT;
        return ELEMENT_TYPE_I4;
    }

    int GetNativeTrueValue() override
    {
        LIMITED_METHOD_CONTRACT;
        return 1;
    }

    int GetNativeFalseValue() override
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
    };

protected:
    CorElementType GetNativeBoolElementType() override
    {
        LIMITED_METHOD_CONTRACT;
        return ELEMENT_TYPE_I1;
    }

    int GetNativeTrueValue() override
    {
        LIMITED_METHOD_CONTRACT;
        return 1;
    }

    int GetNativeFalseValue() override
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
    };

protected:
    CorElementType GetNativeBoolElementType() override
    {
        LIMITED_METHOD_CONTRACT;
        return ELEMENT_TYPE_I2;
    }

    int GetNativeTrueValue() override
    {
        LIMITED_METHOD_CONTRACT;
        return VARIANT_TRUE;
    }

    int GetNativeFalseValue() override
    {
        LIMITED_METHOD_CONTRACT;
        return VARIANT_FALSE;
    }
};
#endif // FEATURE_COMINTEROP

// A marshaler that makes run-time decision based on argument size whether native space will
// be allocated using localloc or on the heap. The ctor argument is a heap free function.
class ILOptimizedAllocMarshaler : public ILMarshaler
{
public:
    ILOptimizedAllocMarshaler(BinderMethodID clearNat) :
        m_idClearNative(clearNat),
        m_dwLocalBuffer(LOCAL_NUM_UNUSED)
    {
        LIMITED_METHOD_CONTRACT;
    }

    LocalDesc GetNativeType() override;
    bool NeedsClearNative() override;
    void EmitClearNative(ILCodeStream* pslILEmit) override;

protected:
    const BinderMethodID m_idClearNative;
    DWORD m_dwLocalBuffer;      // localloc'ed temp buffer variable or LOCAL_NUM_UNUSED if not used
};

class ILUTF8BufferMarshaler : public ILOptimizedAllocMarshaler
{
public:
	enum
	{
		c_fInOnly = FALSE,
		c_nativeSize = TARGET_POINTER_SIZE,
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

	LocalDesc GetManagedType() override;
	void EmitConvertSpaceCLRToNative(ILCodeStream* pslILEmit) override;
	void EmitConvertContentsCLRToNative(ILCodeStream* pslILEmit) override;
	void EmitConvertSpaceNativeToCLR(ILCodeStream* pslILEmit) override;
	void EmitConvertContentsNativeToCLR(ILCodeStream* pslILEmit) override;
};

class ILWSTRBufferMarshaler : public ILOptimizedAllocMarshaler
{
public:
    enum
    {
        c_fInOnly               = FALSE,
        c_nativeSize            = TARGET_POINTER_SIZE,
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

    LocalDesc GetManagedType() override;
    void EmitConvertSpaceCLRToNative(ILCodeStream* pslILEmit) override;
    void EmitConvertContentsCLRToNative(ILCodeStream* pslILEmit) override;
    void EmitConvertSpaceNativeToCLR(ILCodeStream* pslILEmit) override;
    void EmitConvertContentsNativeToCLR(ILCodeStream* pslILEmit) override;
};

class ILCSTRBufferMarshaler : public ILOptimizedAllocMarshaler
{
public:
    enum
    {
        c_fInOnly               = FALSE,
        c_nativeSize            = TARGET_POINTER_SIZE,
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

    LocalDesc GetManagedType() override;
    void EmitConvertSpaceCLRToNative(ILCodeStream* pslILEmit) override;
    void EmitConvertContentsCLRToNative(ILCodeStream* pslILEmit) override;
    void EmitConvertSpaceNativeToCLR(ILCodeStream* pslILEmit) override;
    void EmitConvertContentsNativeToCLR(ILCodeStream* pslILEmit) override;
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
        c_nativeSize            = TARGET_POINTER_SIZE,
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

    virtual bool SupportsFieldMarshal(UINT* pErrorResID)
    {
        LIMITED_METHOD_CONTRACT;
        return false;
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
        c_nativeSize            = TARGET_POINTER_SIZE,
    };

    LocalDesc GetManagedType() override
    {
        LIMITED_METHOD_CONTRACT;
        return LocalDesc(MscorlibBinder::GetClass(CLASS__SAFE_HANDLE));
    }

    LocalDesc GetNativeType() override
    {
        LIMITED_METHOD_CONTRACT;
        return LocalDesc(ELEMENT_TYPE_I);
    }

    void EmitConvertContentsCLRToNative(ILCodeStream* pslILEmit) override;
    void EmitConvertContentsNativeToCLR(ILCodeStream* pslILEmit) override;

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
        c_nativeSize            = TARGET_POINTER_SIZE,
    };

public:

    LocalDesc GetManagedType() override
    {
        LIMITED_METHOD_CONTRACT;
        return LocalDesc(MscorlibBinder::GetClass(CLASS__CRITICAL_HANDLE));
    }

    LocalDesc GetNativeType() override
    {
        LIMITED_METHOD_CONTRACT;
        return LocalDesc(ELEMENT_TYPE_I);
    }

    void EmitConvertContentsCLRToNative(ILCodeStream* pslILEmit) override;
    void EmitConvertContentsNativeToCLR(ILCodeStream* pslILEmit) override;

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
    };

protected:
    LocalDesc GetNativeType() override;
    LocalDesc GetManagedType() override;
    void EmitReInitNative(ILCodeStream* pslILEmit) override;
    bool NeedsClearNative() override;
    void EmitClearNative(ILCodeStream * pslILEmit) override;
    void EmitConvertContentsCLRToNative(ILCodeStream* pslILEmit) override;
    void EmitConvertContentsNativeToCLR(ILCodeStream* pslILEmit) override;
};

#ifdef FEATURE_COMINTEROP
class ILObjectMarshaler : public ILMarshaler
{
public:
    enum
    {
        c_fInOnly               = TRUE,
        c_nativeSize            = TARGET_POINTER_SIZE * 2 + 8, // sizeof(VARIANT)
    };

protected:
    LocalDesc GetNativeType() override;
    LocalDesc GetManagedType() override;
    void EmitConvertContentsCLRToNative(ILCodeStream* pslILEmit) override;
    void EmitConvertContentsNativeToCLR(ILCodeStream* pslILEmit) override;
    bool NeedsClearNative() override;
    void EmitClearNative(ILCodeStream* pslILEmit) override;
    void EmitReInitNative(ILCodeStream* pslILEmit) override;
};
#endif // FEATURE_COMINTEROP

class ILDateMarshaler : public ILMarshaler
{
public:
    enum
    {
        c_fInOnly               = TRUE,
        c_nativeSize            = sizeof(DATE),
    };

protected:
    LocalDesc GetNativeType() override;
    LocalDesc GetManagedType() override;
    void EmitConvertContentsCLRToNative(ILCodeStream* pslILEmit) override;
    void EmitConvertContentsNativeToCLR(ILCodeStream* pslILEmit) override;
    void EmitReInitNative(ILCodeStream* pslILEmit) override;
};


class ILCurrencyMarshaler : public ILMarshaler
{
public:
    enum
    {
        c_fInOnly               = TRUE,
        c_nativeSize            = sizeof(CURRENCY),
    };

protected:
    LocalDesc GetNativeType() override;
    LocalDesc GetManagedType() override;
    void EmitReInitNative(ILCodeStream* pslILEmit) override;
    void EmitConvertContentsCLRToNative(ILCodeStream* pslILEmit) override;
    void EmitConvertContentsNativeToCLR(ILCodeStream* pslILEmit) override;
};


#ifdef FEATURE_COMINTEROP
class ILInterfaceMarshaler : public ILMarshaler
{
public:
    enum
    {
        c_fInOnly               = TRUE,
        c_nativeSize            = TARGET_POINTER_SIZE,
    };

protected:
    LocalDesc GetNativeType() override;
    LocalDesc GetManagedType() override;
    void EmitConvertContentsCLRToNative(ILCodeStream* pslILEmit) override;
    void EmitConvertContentsNativeToCLR(ILCodeStream* pslILEmit) override;
    bool NeedsClearNative() override;
    void EmitClearNative(ILCodeStream* pslILEmit) override;
};
#endif // FEATURE_COMINTEROP


class ILAnsiCharMarshaler : public ILMarshaler
{
public:
    enum
    {
        c_fInOnly               = TRUE,
        c_nativeSize            = sizeof(UINT8),
    };

protected:
    LocalDesc GetNativeType() override;
    LocalDesc GetManagedType() override;
    void EmitConvertContentsCLRToNative(ILCodeStream* pslILEmit) override;
    void EmitConvertContentsNativeToCLR(ILCodeStream* pslILEmit) override;
};


template <BinderClassID CLASS__ID, class ELEMENT>
class ILValueClassPtrMarshaler : public ILMarshaler
{
public:
    enum
    {
        c_fInOnly               = TRUE,
        c_nativeSize            = TARGET_POINTER_SIZE,
    };

    bool SupportsFieldMarshal(UINT* pErrorResID) override
    {
        LIMITED_METHOD_CONTRACT;
        return false;
    }

protected:
    LocalDesc GetNativeType() override
    {
        LIMITED_METHOD_CONTRACT;

        //
        // pointer to value class
        //
        return LocalDesc(ELEMENT_TYPE_I);
    }

    LocalDesc GetManagedType() override
    {
        STANDARD_VM_CONTRACT;

        //
        // value class
        //
        return LocalDesc(MscorlibBinder::GetClass(CLASS__ID));
    }

    bool NeedsClearNative() override
    {
        LIMITED_METHOD_CONTRACT;
        return (IsByref(m_dwMarshalFlags) && IsOut(m_dwMarshalFlags));
    }

    void EmitClearNative(ILCodeStream* pslILEmit) override
    {
        STANDARD_VM_CONTRACT;

        EmitLoadNativeValue(pslILEmit);
        // static void CoTaskMemFree(IntPtr ptr)
        pslILEmit->EmitCALL(METHOD__MARSHAL__FREE_CO_TASK_MEM, 1, 0);
    }

    void EmitConvertSpaceCLRToNative(ILCodeStream* pslILEmit) override
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

    void EmitConvertContentsCLRToNative(ILCodeStream* pslILEmit) override
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

    void EmitConvertContentsNativeToCLR(ILCodeStream* pslILEmit) override
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
    };

protected:
    LocalDesc GetNativeType() override;
    LocalDesc GetManagedType() override;
    void EmitConvertContentsCLRToNative(ILCodeStream* pslILEmit) override;
    void EmitConvertContentsNativeToCLR(ILCodeStream* pslILEmit) override;
};

class ILVBByValStrWMarshaler : public ILMarshaler
{
public:
    enum
    {
        c_fInOnly               = FALSE,
        c_nativeSize            = TARGET_POINTER_SIZE,
    };

    enum
    {
        // If required buffer length > MAX_LOCAL_BUFFER_LENGTH, don't optimize by allocating memory on stack
        MAX_LOCAL_BUFFER_LENGTH = (MAX_PATH_FNAME + 1) * 2 + sizeof(DWORD)
    };


    ILVBByValStrWMarshaler() :
        m_dwCCHLocal(LOCAL_NUM_UNUSED)
       ,m_dwLocalBuffer(LOCAL_NUM_UNUSED)
    {
        LIMITED_METHOD_CONTRACT;
    }

    bool SupportsArgumentMarshal(DWORD dwMarshalFlags, UINT* pErrorResID) override;
    bool SupportsReturnMarshal(DWORD dwMarshalFlags, UINT* pErrorResID) override;

protected:
    LocalDesc GetNativeType() override;
    LocalDesc GetManagedType() override;
    void EmitSetupArgumentForMarshalling(ILCodeStream* pslILEmit) override;
    void EmitConvertContentsCLRToNative(ILCodeStream* pslILEmit) override;
    void EmitConvertContentsNativeToCLR(ILCodeStream* pslILEmit) override;
    bool NeedsClearNative() override;
    void EmitClearNative(ILCodeStream* pslILEmit) override;
    bool IsNativePassedByRef() override;

    DWORD m_dwCCHLocal;
    DWORD m_dwLocalBuffer;
};

class ILVBByValStrMarshaler : public ILMarshaler
{
public:
    enum
    {
        c_fInOnly               = FALSE,
        c_nativeSize            = TARGET_POINTER_SIZE,
    };

    ILVBByValStrMarshaler() :
        m_dwCCHLocal(LOCAL_NUM_UNUSED)
    {
        LIMITED_METHOD_CONTRACT;
    }

    bool SupportsArgumentMarshal(DWORD dwMarshalFlags, UINT* pErrorResID) override;
    bool SupportsReturnMarshal(DWORD dwMarshalFlags, UINT* pErrorResID) override;

protected:
    LocalDesc GetNativeType() override;
    LocalDesc GetManagedType() override;
    void EmitConvertContentsCLRToNative(ILCodeStream* pslILEmit) override;
    void EmitConvertContentsNativeToCLR(ILCodeStream* pslILEmit) override;
    bool NeedsClearNative() override;
    void EmitClearNative(ILCodeStream* pslILEmit) override;
    bool IsNativePassedByRef() override;

    DWORD m_dwCCHLocal;
};
#endif // FEATURE_COMINTEROP


class ILCUTF8Marshaler : public ILOptimizedAllocMarshaler
{
public:
	enum
	{
		c_fInOnly = TRUE,
		c_nativeSize = TARGET_POINTER_SIZE,
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
	LocalDesc GetManagedType() override;
	void EmitConvertContentsCLRToNative(ILCodeStream* pslILEmit) override;
	void EmitConvertContentsNativeToCLR(ILCodeStream* pslILEmit) override;
};

class ILWSTRMarshaler : public ILOptimizedAllocMarshaler
{
public:
    enum
    {
        c_fInOnly               = FALSE,
        c_nativeSize            = TARGET_POINTER_SIZE,
    };

    enum
    {
        // If required buffer length > MAX_LOCAL_BUFFER_LENGTH, don't optimize by allocating memory on stack
        MAX_LOCAL_BUFFER_LENGTH = (MAX_PATH_FNAME + 1) * 2
    };

    ILWSTRMarshaler()
        :ILOptimizedAllocMarshaler(METHOD__MARSHAL__FREE_CO_TASK_MEM)
    {
        LIMITED_METHOD_CONTRACT;
    }


    bool SupportsArgumentMarshal(DWORD dwMarshalFlags, UINT* pErrorResID) override
    {
        if (IsOut(dwMarshalFlags) && !IsByref(dwMarshalFlags) && IsCLRToNative(dwMarshalFlags))
        {
            *pErrorResID = IDS_EE_BADMARSHAL_STRING_OUT;
            return false;
        }

        return true;
    }

protected:
    LocalDesc GetNativeType() override;
    LocalDesc GetManagedType() override;

    void EmitConvertSpaceCLRToNative(ILCodeStream* pslILEmit) override;
    void EmitConvertContentsCLRToNative(ILCodeStream* pslILEmit) override;
    void EmitConvertSpaceAndContentsCLRToNative(ILCodeStream* pslILEmit) override;
    void EmitConvertSpaceAndContentsCLRToNativeTemp(ILCodeStream* pslILEmit) override;

    void EmitConvertSpaceNativeToCLR(ILCodeStream* pslILEmit) override;
    void EmitConvertContentsNativeToCLR(ILCodeStream* pslILEmit) override;

    bool CanMarshalViaPinning() override
    {
        LIMITED_METHOD_CONTRACT;
        return IsCLRToNative(m_dwMarshalFlags) && !IsByref(m_dwMarshalFlags) && IsIn(m_dwMarshalFlags) && !IsOut(m_dwMarshalFlags);
    }
    void EmitMarshalViaPinning(ILCodeStream* pslILEmit) override;

    static void EmitCheckManagedStringLength(ILCodeStream* pslILEmit);
    static void EmitCheckNativeStringLength(ILCodeStream* pslILEmit);
};

class ILCSTRMarshaler : public ILOptimizedAllocMarshaler
{
public:
    enum
    {
        c_fInOnly               = TRUE,
        c_nativeSize            = TARGET_POINTER_SIZE,
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
    LocalDesc GetManagedType() override;
    void EmitConvertContentsCLRToNative(ILCodeStream* pslILEmit) override;
    void EmitConvertContentsNativeToCLR(ILCodeStream* pslILEmit) override;
};

class ILBSTRMarshaler : public ILOptimizedAllocMarshaler
{
public:
    enum
    {
        c_fInOnly               = TRUE,
        c_nativeSize            = TARGET_POINTER_SIZE,
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
    LocalDesc GetManagedType() override;
    void EmitConvertContentsCLRToNative(ILCodeStream* pslILEmit) override;
    void EmitConvertContentsNativeToCLR(ILCodeStream* pslILEmit) override;
};

class ILAnsiBSTRMarshaler : public ILMarshaler
{
public:
    enum
    {
        c_fInOnly               = TRUE,
        c_nativeSize            = TARGET_POINTER_SIZE,
    };

protected:
    LocalDesc GetNativeType() override;
    LocalDesc GetManagedType() override;
    void EmitConvertContentsCLRToNative(ILCodeStream* pslILEmit) override;
    void EmitConvertContentsNativeToCLR(ILCodeStream* pslILEmit) override;
    bool NeedsClearNative() override;
    void EmitClearNative(ILCodeStream* pslILEmit) override;
};

class ILFixedWSTRMarshaler : public ILMarshaler
{
public:
    enum
    {
        c_fInOnly = FALSE,
        c_nativeSize = VARIABLESIZE,
    };

    bool SupportsArgumentMarshal(DWORD dwMarshalFlags, UINT* pErrorResID) override
    {
        LIMITED_METHOD_CONTRACT;
        return false;
    }

    bool SupportsReturnMarshal(DWORD dwMarshalFlags, UINT* pErrorResID) override
    {
        LIMITED_METHOD_CONTRACT;
        return false;
    }

protected:
    LocalDesc GetNativeType() override
    {
        return LocalDesc(ELEMENT_TYPE_I2);
    }

    LocalDesc GetManagedType() override
    {
        return LocalDesc(ELEMENT_TYPE_STRING);
    }

    void EmitConvertContentsCLRToNative(ILCodeStream* pslILEmit) override;
    void EmitConvertContentsNativeToCLR(ILCodeStream* pslILEmit) override;
};

class ILFixedCSTRMarshaler : public ILMarshaler
{
public:
    enum
    {
        c_fInOnly = FALSE,
        c_nativeSize = VARIABLESIZE,
    };

    bool SupportsArgumentMarshal(DWORD dwMarshalFlags, UINT* pErrorResID) override
    {
        LIMITED_METHOD_CONTRACT;
        return false;
    }

    bool SupportsReturnMarshal(DWORD dwMarshalFlags, UINT* pErrorResID) override
    {
        LIMITED_METHOD_CONTRACT;
        return false;
    }

protected:
    LocalDesc GetNativeType() override
    {
        return LocalDesc(ELEMENT_TYPE_I1);
    }

    LocalDesc GetManagedType() override
    {
        return LocalDesc(ELEMENT_TYPE_STRING);
    }

    void EmitConvertContentsCLRToNative(ILCodeStream* pslILEmit) override;
    void EmitConvertContentsNativeToCLR(ILCodeStream* pslILEmit) override;
};

class ILLayoutClassPtrMarshalerBase : public ILMarshaler
{
public:
    enum
    {
        c_nativeSize            = TARGET_POINTER_SIZE,
    };

protected:
    LocalDesc GetNativeType() override;
    LocalDesc GetManagedType() override;
    void EmitConvertSpaceCLRToNative(ILCodeStream* pslILEmit) override;
    void EmitConvertSpaceCLRToNativeTemp(ILCodeStream* pslILEmit) override;
    void EmitConvertSpaceAndContentsCLRToNativeTemp(ILCodeStream* pslILEmit) override;
    void EmitConvertSpaceNativeToCLR(ILCodeStream* pslILEmit) override;
    bool NeedsClearNative() override;
    void EmitClearNative(ILCodeStream* pslILEmit) override;
    void EmitClearNativeTemp(ILCodeStream* pslILEmit) override;
};

class ILLayoutClassPtrMarshaler : public ILLayoutClassPtrMarshalerBase
{
public:
    enum
    {
        c_fInOnly = FALSE,
    };

protected:
    void EmitConvertContentsCLRToNative(ILCodeStream* pslILEmit) override;
    void EmitConvertContentsNativeToCLR(ILCodeStream* pslILEmit) override;
    void EmitClearNativeContents(ILCodeStream* pslILEmit) override;
};

class ILBlittablePtrMarshaler : public ILLayoutClassPtrMarshalerBase
{
public:
    enum
    {
        c_fInOnly = FALSE,
    };

protected:
    void EmitConvertContentsCLRToNative(ILCodeStream* pslILEmit) override;
    void EmitConvertContentsNativeToCLR(ILCodeStream* pslILEmit) override;
    bool CanMarshalViaPinning() override;
    void EmitMarshalViaPinning(ILCodeStream* pslILEmit) override;
};

class ILLayoutClassMarshaler : public ILMarshaler
{
public:
    enum
    {
        c_fInOnly = FALSE,
        c_nativeSize = VARIABLESIZE,
    };

    LocalDesc GetNativeType() override
    {
        return LocalDesc(ELEMENT_TYPE_I);
    }

    LocalDesc GetManagedType() override
    {
        return LocalDesc(m_pargs->m_pMT);
    }

protected:
    void EmitConvertContentsCLRToNative(ILCodeStream* pslILEmit) override;
    void EmitConvertSpaceNativeToCLR(ILCodeStream* pslILEmit) override;
    void EmitConvertContentsNativeToCLR(ILCodeStream* pslILEmit) override;
    void EmitClearNativeContents(ILCodeStream* pslILEmit) override;
    void EmitClearNative(ILCodeStream* pslILEmit) override
    {
        EmitClearNativeContents(pslILEmit);
    }
};

class ILBlittableLayoutClassMarshaler : public ILMarshaler
{
public:
    enum
    {
        c_fInOnly = FALSE,
        c_nativeSize = VARIABLESIZE,
    };

    LocalDesc GetNativeType() override
    {
        return LocalDesc(ELEMENT_TYPE_I);
    }

    LocalDesc GetManagedType() override
    {
        return LocalDesc(m_pargs->m_pMT);
    }

protected:
    void EmitConvertContentsCLRToNative(ILCodeStream* pslILEmit) override;
    void EmitConvertContentsNativeToCLR(ILCodeStream* pslILEmit) override;
};

class ILBlittableValueClassWithCopyCtorMarshaler : public ILMarshaler
{
public:
    enum
    {
        c_fInOnly               = TRUE,
        c_nativeSize            = VARIABLESIZE,
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
#ifdef TARGET_64BIT
        c_nativeSize            = 24, // sizeof(va_list)
#else
        c_nativeSize            = 4,  // sizeof(va_list)
#endif
    };

protected:
    LocalDesc GetNativeType() override;
    LocalDesc GetManagedType() override;
    bool SupportsArgumentMarshal(DWORD dwMarshalFlags, UINT* pErrorResID) override;
    void EmitConvertSpaceAndContentsCLRToNative(ILCodeStream* pslILEmit) override;
    void EmitConvertContentsNativeToCLR(ILCodeStream* pslILEmit) override;

private:
    DWORD m_dwVaListSizeLocalNum;
};

class ILArrayWithOffsetMarshaler : public ILMarshaler
{
public:
    enum
    {
        c_fInOnly               = FALSE,
        c_nativeSize            = TARGET_POINTER_SIZE,
    };

    ILArrayWithOffsetMarshaler() :
        m_dwCountLocalNum(LOCAL_NUM_UNUSED),
        m_dwOffsetLocalNum(LOCAL_NUM_UNUSED),
        m_dwPinnedLocalNum(LOCAL_NUM_UNUSED)
    {
        LIMITED_METHOD_CONTRACT;
    }

protected:
    LocalDesc GetNativeType() override;
    LocalDesc GetManagedType() override;
    bool SupportsArgumentMarshal(DWORD dwMarshalFlags, UINT* pErrorResID) override;

    void EmitConvertSpaceAndContentsCLRToNativeTemp(ILCodeStream* pslILEmit) override;
    void EmitConvertContentsNativeToCLR(ILCodeStream* pslILEmit) override;
    void EmitClearNativeTemp(ILCodeStream* pslILEmit) override;

private:
    DWORD m_dwCountLocalNum;
    DWORD m_dwOffsetLocalNum;
    DWORD m_dwPinnedLocalNum;
};

class ILAsAnyMarshalerBase : public ILMarshaler
{
public:
    enum
    {
        c_nativeSize            = TARGET_POINTER_SIZE,
    };

protected:

    virtual bool IsAnsi() const = 0;
    LocalDesc GetNativeType() override final;
    LocalDesc GetManagedType() override final;
    void EmitCreateMngdMarshaler(ILCodeStream* pslILEmit) override final;
    bool SupportsArgumentMarshal(DWORD dwMarshalFlags, UINT* pErrorResID) override final;
    bool SupportsReturnMarshal(DWORD dwMarshalFlags, UINT* pErrorResID) override final;
    void EmitConvertContentsCLRToNative(ILCodeStream* pslILEmit) override final;
    void EmitConvertContentsNativeToCLR(ILCodeStream* pslILEmit) override final;
    bool NeedsClearNative() override final;
    void EmitClearNativeTemp(ILCodeStream* pslILEmit) override final;
    bool AlwaysConvertByValContentsCLRToNative() override final
    {
        LIMITED_METHOD_CONTRACT;
        return true;
    }

    bool SupportsFieldMarshal(UINT* pErrorResID) override
    {
        LIMITED_METHOD_CONTRACT;
        return false;
    }
private:
    // These flags correspond to System.StubHelpers.AsAnyMarshaler.AsAnyFlags.In and Out respectively.
    // We have to pre-calculate the flags and emit them into the IL stream since the AsAny marshalers
    // are effectively lazily resolved based on the runtime type of the object.
    static const BYTE ML_IN  = 0x10;
    static const BYTE ML_OUT = 0x20;

    DWORD GetAsAnyFlags() const
    {
        BYTE inout = (IsIn(m_dwMarshalFlags) ? ML_IN : 0) | (IsOut(m_dwMarshalFlags) ? ML_OUT : 0);
        BYTE fIsAnsi = IsAnsi() ? 1 : 0;
        BYTE fBestFit = m_pargs->m_pMarshalInfo->GetBestFitMapping();
        BYTE fThrow = m_pargs->m_pMarshalInfo->GetThrowOnUnmappableChar();

        DWORD dwFlags = 0;

        dwFlags |= inout << 24;
        dwFlags |= fIsAnsi << 16;
        dwFlags |= fThrow << 8;
        dwFlags |= fBestFit << 0;
        return dwFlags;
    }
};

class ILAsAnyWMarshaler : public ILAsAnyMarshalerBase
{
public:
    enum
    {
        c_fInOnly               = FALSE,
    };

protected:
    bool IsAnsi() const override
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
    bool IsAnsi() const override
    {
        return true;
    }
};


class ILMngdMarshaler : public ILMarshaler
{
public:
    enum
    {
        c_nativeSize            = TARGET_POINTER_SIZE,
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
    LocalDesc GetNativeType() override;
    LocalDesc GetManagedType() override;

    void EmitCreateMngdMarshaler(ILCodeStream* pslILEmit) override = 0;

    virtual void EmitCallMngdMarshalerMethod(ILCodeStream* pslILEmit, MethodDesc *pMD);

    void EmitConvertSpaceNativeToCLR(ILCodeStream* pslILEmit) override
    {
        WRAPPER_NO_CONTRACT;
        EmitCallMngdMarshalerMethod(pslILEmit, GetConvertSpaceToManagedMethod());
    }

    void EmitConvertContentsNativeToCLR(ILCodeStream* pslILEmit) override
    {
        WRAPPER_NO_CONTRACT;
        EmitCallMngdMarshalerMethod(pslILEmit, GetConvertContentsToManagedMethod());
    }

    void EmitConvertSpaceCLRToNative(ILCodeStream* pslILEmit) override
    {
        WRAPPER_NO_CONTRACT;
        EmitCallMngdMarshalerMethod(pslILEmit, GetConvertSpaceToNativeMethod());
    }

    void EmitConvertContentsCLRToNative(ILCodeStream* pslILEmit) override
    {
        WRAPPER_NO_CONTRACT;
        EmitCallMngdMarshalerMethod(pslILEmit, GetConvertContentsToNativeMethod());
    }

    bool NeedsClearNative() override
    {
        LIMITED_METHOD_CONTRACT;

        if (NULL != GetClearNativeMethod())
        {
            return true;
        }

        return false;
    }

    void EmitClearNative(ILCodeStream* pslILEmit) override
    {
        WRAPPER_NO_CONTRACT;
        ILCodeLabel* pNoManagedValueLabel = nullptr;
        if (IsFieldMarshal(m_dwMarshalFlags))
        {
            pNoManagedValueLabel = pslILEmit->NewCodeLabel();
            pslILEmit->EmitLDARG(StructMarshalStubs::MANAGED_STRUCT_ARGIDX);
            pslILEmit->EmitBRFALSE(pNoManagedValueLabel);
        }

        EmitCallMngdMarshalerMethod(pslILEmit, GetClearNativeMethod());

        if (IsFieldMarshal(m_dwMarshalFlags))
        {
            pslILEmit->EmitLabel(pNoManagedValueLabel);
        }
    }

    void EmitClearNativeContents(ILCodeStream* pslILEmit) override
    {
        WRAPPER_NO_CONTRACT;
        ILCodeLabel* pNoManagedValueLabel = nullptr;
        if (IsFieldMarshal(m_dwMarshalFlags))
        {
            pNoManagedValueLabel = pslILEmit->NewCodeLabel();
            pslILEmit->EmitLDARG(StructMarshalStubs::MANAGED_STRUCT_ARGIDX);
            pslILEmit->EmitBRFALSE(pNoManagedValueLabel);
        }

        EmitCallMngdMarshalerMethod(pslILEmit, GetClearNativeContentsMethod());

        if (IsFieldMarshal(m_dwMarshalFlags))
        {
            pslILEmit->EmitLabel(pNoManagedValueLabel);
        }
    }

    bool NeedsClearCLR() override
    {
        LIMITED_METHOD_CONTRACT;

        if (NULL != GetClearManagedMethod())
        {
            return true;
        }

        return false;
    }

    void EmitClearCLR(ILCodeStream* pslILEmit) override
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

    bool CanMarshalViaPinning() override;

    void EmitMarshalViaPinning(ILCodeStream* pslILEmit) override;
    void EmitSetupArgumentForMarshalling(ILCodeStream* pslILEmit) override;
    void EmitConvertSpaceNativeToCLR(ILCodeStream* pslILEmit) override;
    void EmitConvertSpaceCLRToNative(ILCodeStream* pslILEmit) override;
    void EmitClearNative(ILCodeStream* pslILEmit) override;
    void EmitClearNativeContents(ILCodeStream* pslILEmit) override;

    bool SupportsFieldMarshal(UINT* pErrorResID) override
    {
        LIMITED_METHOD_CONTRACT;
        return false;
    }
protected:

    BOOL CheckSizeParamIndexArg(const CREATE_MARSHALER_CARRAY_OPERANDS &mops, CorElementType *pElementType);

    // Calculate element count and load it on evaluation stack
    void EmitLoadElementCount(ILCodeStream* pslILEmit);

    void EmitCreateMngdMarshaler(ILCodeStream* pslILEmit) override;

    void EmitLoadNativeSize(ILCodeStream* pslILEmit);
    void EmitNewSavedSizeArgLocal(ILCodeStream* pslILEmit);

private :
    DWORD m_dwSavedSizeArg;
};

class MngdNativeArrayMarshaler
{
public:
    static FCDECL4(void, CreateMarshaler,           MngdNativeArrayMarshaler* pThis, MethodTable* pMT, UINT32 dwFlags, PCODE pManagedMarshaler);
    static FCDECL3(void, ConvertSpaceToNative,      MngdNativeArrayMarshaler* pThis, OBJECTREF* pManagedHome, void** pNativeHome);
    static FCDECL3(void, ConvertContentsToNative,   MngdNativeArrayMarshaler* pThis, OBJECTREF* pManagedHome, void** pNativeHome);
    static FCDECL4(void, ConvertSpaceToManaged,     MngdNativeArrayMarshaler* pThis, OBJECTREF* pManagedHome, void** pNativeHome, INT32 cElements);
    static FCDECL3(void, ConvertContentsToManaged,  MngdNativeArrayMarshaler* pThis, OBJECTREF* pManagedHome, void** pNativeHome);
    static FCDECL4(void, ClearNative,               MngdNativeArrayMarshaler* pThis, OBJECTREF* pManagedHome, void** pNativeHome, INT32 cElements);
    static FCDECL4(void, ClearNativeContents,       MngdNativeArrayMarshaler* pThis, OBJECTREF* pManagedHome, void** pNativeHome, INT32 cElements);

    static void DoClearNativeContents(MngdNativeArrayMarshaler* pThis, OBJECTREF* pManagedHome, void** pNativeHome, INT32 cElements);
    enum
    {
        FLAG_NATIVE_DATA_VALID = 0x40000000
    };

    MethodTable*            m_pElementMT;
    TypeHandle              m_Array;
    PCODE                   m_pManagedMarshaler;
    BOOL                    m_NativeDataValid;
    BOOL                    m_BestFitMap;
    BOOL                    m_ThrowOnUnmappableChar;
    VARTYPE                 m_vt;
};

class ILFixedArrayMarshaler : public ILMngdMarshaler
{
public:
    enum
    {
        c_nativeSize = VARIABLESIZE,
        c_fInOnly = FALSE
    };

    ILFixedArrayMarshaler() :
        ILMngdMarshaler(
            METHOD__MNGD_FIXED_ARRAY_MARSHALER__CONVERT_SPACE_TO_MANAGED,
            METHOD__MNGD_FIXED_ARRAY_MARSHALER__CONVERT_CONTENTS_TO_MANAGED,
            METHOD__MNGD_FIXED_ARRAY_MARSHALER__CONVERT_SPACE_TO_NATIVE,
            METHOD__MNGD_FIXED_ARRAY_MARSHALER__CONVERT_CONTENTS_TO_NATIVE,
            METHOD__MNGD_FIXED_ARRAY_MARSHALER__CLEAR_NATIVE_CONTENTS,
            METHOD__MNGD_FIXED_ARRAY_MARSHALER__CLEAR_NATIVE_CONTENTS,
            METHOD__NIL
        )
    {
        LIMITED_METHOD_CONTRACT;
    }

    bool SupportsArgumentMarshal(DWORD dwMarshalFlags, UINT* pErrorResID) override
    {
        LIMITED_METHOD_CONTRACT;
        return false;
    }

    bool SupportsReturnMarshal(DWORD dwMarshalFlags, UINT* pErrorResID) override
    {
        LIMITED_METHOD_CONTRACT;
        return false;
    }

protected:

    void EmitCreateMngdMarshaler(ILCodeStream* pslILEmit) override;
};

class MngdFixedArrayMarshaler
{
public:
    static FCDECL5(void, CreateMarshaler,           MngdFixedArrayMarshaler* pThis, MethodTable* pMT, UINT32 dwFlags, UINT32 cElements, PCODE pManagedElementMarshaler);
    static FCDECL3(void, ConvertSpaceToNative,      MngdFixedArrayMarshaler* pThis, OBJECTREF* pManagedHome, void* pNativeHome);
    static FCDECL3(void, ConvertContentsToNative,   MngdFixedArrayMarshaler* pThis, OBJECTREF* pManagedHome, void* pNativeHome);
    static FCDECL3(void, ConvertSpaceToManaged,     MngdFixedArrayMarshaler* pThis, OBJECTREF* pManagedHome, void* pNativeHome);
    static FCDECL3(void, ConvertContentsToManaged,  MngdFixedArrayMarshaler* pThis, OBJECTREF* pManagedHome, void* pNativeHome);
    static FCDECL3(void, ClearNativeContents,       MngdFixedArrayMarshaler* pThis, OBJECTREF* pManagedHome, void* pNativeHome);

    enum
    {
        FLAG_NATIVE_DATA_VALID = 0x40000000
    };

    MethodTable* m_pElementMT;
    PCODE        m_pManagedElementMarshaler;
    TypeHandle   m_Array;
    BOOL         m_NativeDataValid;
    BOOL         m_BestFitMap;
    BOOL         m_ThrowOnUnmappableChar;
    VARTYPE      m_vt;
    UINT32       m_cElements;
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
        m_dwOriginalManagedLocalNum(LOCAL_NUM_UNUSED)
    {
        LIMITED_METHOD_CONTRACT;
    }

protected:

    void EmitCreateMngdMarshaler(ILCodeStream* pslILEmit) override;
    void EmitConvertContentsNativeToCLR(ILCodeStream* pslILEmit) override;
    void EmitConvertContentsCLRToNative(ILCodeStream* pslILEmit) override;

    void EmitReInitNative(ILCodeStream* pslILEmit) override
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
    static FCDECL5(void, CreateMarshaler,           MngdSafeArrayMarshaler* pThis, MethodTable* pMT, UINT32 iRank, UINT32 dwFlags, PCODE pManagedMarshaler);
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
    PCODE           m_pManagedMarshaler;
    int             m_iRank;
    VARTYPE         m_vt;
    BYTE            m_fStatic;     // StaticCheckStateFlags
    BYTE            m_nolowerbounds;
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
    void EmitCreateMngdMarshaler(ILCodeStream* pslILEmit) override;
};

class MngdRefCustomMarshaler
{
public:
    static FCDECL2(void, CreateMarshaler,           MngdRefCustomMarshaler* pThis, void* pCMHelper);
    static FCDECL3(void, ConvertContentsToNative,   MngdRefCustomMarshaler* pThis, OBJECTREF* pManagedHome, void** pNativeHome);
    static FCDECL3(void, ConvertContentsToManaged,  MngdRefCustomMarshaler* pThis, OBJECTREF* pManagedHome, void** pNativeHome);
    static FCDECL3(void, ClearNative,               MngdRefCustomMarshaler* pThis, OBJECTREF* pManagedHome, void** pNativeHome);
    static FCDECL3(void, ClearManaged,              MngdRefCustomMarshaler* pThis, OBJECTREF* pManagedHome, void** pNativeHome);

    CustomMarshalerHelper*  m_pCMHelper;
};
