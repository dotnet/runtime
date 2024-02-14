// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// File: DllImport.h
//

#ifndef __dllimport_h__
#define __dllimport_h__

#include "util.hpp"

struct PInvokeStaticSigInfo;

// This structure groups together data that describe the signature for which a marshaling stub is being generated.
struct StubSigDesc
{
public:
    StubSigDesc(MethodDesc* pMD);
    StubSigDesc(MethodDesc*  pMD, const Signature& sig, Module* pModule, Module* pLoaderModule = NULL);
    StubSigDesc(MethodTable* pMT, const Signature& sig, Module* pModule);
    StubSigDesc(const Signature& sig, Module* pModule);

    MethodDesc        *m_pMD;
    MethodTable       *m_pMT;
    Signature          m_sig;
    // Module to use for signature reading.
    Module            *m_pModule;
    // Module that owns any metadata that influences interop behavior.
    // This is usually the same as m_pModule, but can differ with vararg
    // P/Invokes, where the calling assembly's module is assigned to m_pModule
    // since the specific caller signature is defined in that assembly, not the
    // assembly that defined the P/Invoke.
    Module            *m_pMetadataModule;
    // Used for ILStubCache selection and MethodTable creation.
    Module            *m_pLoaderModule;
    mdMethodDef        m_tkMethodDef;
    SigTypeContext     m_typeContext;

#ifdef _DEBUG
    LPCUTF8            m_pDebugName;
    LPCUTF8            m_pDebugClassName;

    void InitDebugNames()
    {
        LIMITED_METHOD_CONTRACT;

        if (m_pMD != NULL)
        {
            m_pDebugName = m_pMD->m_pszDebugMethodName;
            m_pDebugClassName = m_pMD->m_pszDebugClassName;
        }
        else
        {
            m_pDebugName = NULL;
            m_pDebugClassName = NULL;
        }
    }
#endif // _DEBUG
    
#ifndef DACCESS_COMPILE
    void InitTypeContext(TypeHandle* classInst, DWORD classInstCount, TypeHandle* methodInst, DWORD methodInstCount)
    {
        LIMITED_METHOD_CONTRACT;

        m_typeContext = SigTypeContext(Instantiation(classInst, classInstCount), Instantiation(methodInst, methodInstCount));
    }
#endif
};

//=======================================================================
// Collects code and data pertaining to the NDirect interface.
//=======================================================================
class NDirect
{
public:
    // Get the calling convention and whether to suppress GC transition for a method by checking:
    //   - SuppressGCTransition attribute
    //   - For delegates: UnmanagedFunctionPointer attribute
    //   - For non-delegates: P/Invoke metadata
    //   - Any modopts encoded in the method signature
    // If no calling convention is specified, the default calling convention is returned
    // This function ignores any errors when reading attributes/metadata, treating them as
    // if no calling convention was specified through that mechanism.
    static void GetCallingConvention_IgnoreErrors(_In_ MethodDesc* pMD, _Out_opt_ CorInfoCallConvExtension* callConv, _Out_opt_ bool* suppressGCTransition);

    //---------------------------------------------------------
    // Does a class or method have a NAT_L CustomAttribute?
    //
    // S_OK    = yes
    // S_FALSE = no
    // FAILED  = unknown because something failed.
    //---------------------------------------------------------
    static HRESULT HasNAT_LAttribute(IMDInternalImport *pInternalImport, mdToken token, DWORD dwMemberAttrs);

    // Either MD or signature & module must be given.
    // Note: This method can be called at a time when the associated NDirectMethodDesc
    // has not been fully populated. This means the optimized path for this call is to rely
    // on the most basic P/Invoke metadata. An example when this can happen is when the JIT
    // is compiling a method containing a P/Invoke that is being considered for inlining.
    static BOOL MarshalingRequired(
        _In_opt_ MethodDesc* pMD,
        _In_opt_ PCCOR_SIGNATURE pSig = NULL,
        _In_opt_ Module* pModule = NULL,
        _In_opt_ SigTypeContext* pTypeContext = NULL,
        _In_ bool unmanagedCallersOnlyRequiresMarshalling = true);

    static void PopulateNDirectMethodDesc(_Inout_ NDirectMethodDesc* pNMD);
    static void InitializeSigInfoAndPopulateNDirectMethodDesc(_Inout_ NDirectMethodDesc* pNMD, _Inout_ PInvokeStaticSigInfo* pSigInfo);

    static MethodDesc* CreateCLRToNativeILStub(
                    StubSigDesc*             pSigDesc,
                    CorNativeLinkType        nlType,
                    CorNativeLinkFlags       nlFlags,
                    CorInfoCallConvExtension unmgdCallConv,
                    DWORD                    dwStubFlags); // NDirectStubFlags

#ifdef FEATURE_COMINTEROP
    static MethodDesc* CreateFieldAccessILStub(
                    PCCOR_SIGNATURE    szMetaSig,
                    DWORD              cbMetaSigSize,
                    Module*            pModule,
                    mdFieldDef         fd,
                    DWORD              dwStubFlags, // NDirectStubFlags
                    FieldDesc*         pFD);
#endif // FEATURE_COMINTEROP

    static MethodDesc* CreateStructMarshalILStub(MethodTable* pMT);
    static PCODE GetEntryPointForStructMarshalStub(MethodTable* pMT);

    static MethodDesc* CreateCLRToNativeILStub(PInvokeStaticSigInfo* pSigInfo,
                             DWORD dwStubFlags,
                             MethodDesc* pMD);

    static MethodDesc*      GetILStubMethodDesc(NDirectMethodDesc* pNMD, PInvokeStaticSigInfo* pSigInfo, DWORD dwStubFlags);
    static PCODE            GetStubForILStub(NDirectMethodDesc* pNMD, MethodDesc** ppStubMD, DWORD dwStubFlags);
    static PCODE            GetStubForILStub(MethodDesc* pMD, MethodDesc** ppStubMD, DWORD dwStubFlags);

private:
    NDirect() {LIMITED_METHOD_CONTRACT;};     // prevent "new"'s on this class
};

//----------------------------------------------------------------
// Flags passed to CreateNDirectStub that control stub generation
//----------------------------------------------------------------
enum NDirectStubFlags
{
    NDIRECTSTUB_FL_CONVSIGASVARARG          = 0x00000001,
    NDIRECTSTUB_FL_BESTFIT                  = 0x00000002,
    NDIRECTSTUB_FL_THROWONUNMAPPABLECHAR    = 0x00000004,
    // unused                               = 0x00000008,
    NDIRECTSTUB_FL_DELEGATE                 = 0x00000010,
    NDIRECTSTUB_FL_DOHRESULTSWAPPING        = 0x00000020,
    NDIRECTSTUB_FL_REVERSE_INTEROP          = 0x00000040,
#ifdef FEATURE_COMINTEROP
    NDIRECTSTUB_FL_COM                      = 0x00000080,
#endif // FEATURE_COMINTEROP
    // unused                               = 0x00000100,
    NDIRECTSTUB_FL_GENERATEDEBUGGABLEIL     = 0x00000200,
    NDIRECTSTUB_FL_STRUCT_MARSHAL           = 0x00000400,
    NDIRECTSTUB_FL_UNMANAGED_CALLI          = 0x00000800,
    // unused                               = 0x00001000,
#ifdef FEATURE_COMINTEROP
    NDIRECTSTUB_FL_FIELDGETTER              = 0x00002000, // COM->CLR field getter
    NDIRECTSTUB_FL_FIELDSETTER              = 0x00004000, // COM->CLR field setter
#endif // FEATURE_COMINTEROP
    NDIRECTSTUB_FL_SUPPRESSGCTRANSITION     = 0x00008000,
    NDIRECTSTUB_FL_STUB_HAS_THIS            = 0x00010000,
    NDIRECTSTUB_FL_TARGET_HAS_THIS          = 0x00020000,
    NDIRECTSTUB_FL_CHECK_PENDING_EXCEPTION  = 0x00040000,
    // unused                               = 0x00080000,
    // unused                               = 0x00100000,
    // unused                               = 0x00200000,
    // unused                               = 0x00400000,
    // unused                               = 0x00800000,

    // internal flags -- these won't ever show up in an NDirectStubHashBlob
    NDIRECTSTUB_FL_FOR_NUMPARAMBYTES        = 0x10000000,   // do just enough to return the right value from Marshal.NumParamBytes

#ifdef FEATURE_COMINTEROP
    NDIRECTSTUB_FL_COMLATEBOUND             = 0x20000000,   // we use a generic stub for late bound calls
    NDIRECTSTUB_FL_COMEVENTCALL             = 0x40000000,   // we use a generic stub for event calls
#endif // FEATURE_COMINTEROP

    // Note: The upper half of the range is reserved for ILStubTypes enum
    NDIRECTSTUB_FL_MASK                     = 0x7FFFFFFF,
    NDIRECTSTUB_FL_INVALID                  = 0x80000000,
};

enum ILStubTypes
{
    ILSTUB_INVALID                       = 0x80000000,
#ifdef FEATURE_ARRAYSTUB_AS_IL
    ILSTUB_ARRAYOP_GET                   = 0x80000001,
    ILSTUB_ARRAYOP_SET                   = 0x80000002,
    ILSTUB_ARRAYOP_ADDRESS               = 0x80000003,
#endif
#ifdef FEATURE_MULTICASTSTUB_AS_IL
    ILSTUB_MULTICASTDELEGATE_INVOKE      = 0x80000004,
#endif
#ifdef FEATURE_INSTANTIATINGSTUB_AS_IL
    ILSTUB_UNBOXINGILSTUB                = 0x80000005,
    ILSTUB_INSTANTIATINGSTUB             = 0x80000006,
#endif
    ILSTUB_WRAPPERDELEGATE_INVOKE        = 0x80000007,
    ILSTUB_TAILCALL_STOREARGS            = 0x80000008,
    ILSTUB_TAILCALL_CALLTARGET           = 0x80000009,
    ILSTUB_STATIC_VIRTUAL_DISPATCH_STUB  = 0x8000000A,
};

#ifdef FEATURE_COMINTEROP
#define COM_ONLY(x) (x)
#else // FEATURE_COMINTEROP
#define COM_ONLY(x) false
#endif // FEATURE_COMINTEROP

inline bool SF_IsVarArgStub            (DWORD dwStubFlags) { LIMITED_METHOD_CONTRACT; return (dwStubFlags < NDIRECTSTUB_FL_INVALID && 0 != (dwStubFlags & NDIRECTSTUB_FL_CONVSIGASVARARG)); }
inline bool SF_IsBestFit               (DWORD dwStubFlags) { LIMITED_METHOD_CONTRACT; return (dwStubFlags < NDIRECTSTUB_FL_INVALID && 0 != (dwStubFlags & NDIRECTSTUB_FL_BESTFIT)); }
inline bool SF_IsThrowOnUnmappableChar (DWORD dwStubFlags) { LIMITED_METHOD_CONTRACT; return (dwStubFlags < NDIRECTSTUB_FL_INVALID && 0 != (dwStubFlags & NDIRECTSTUB_FL_THROWONUNMAPPABLECHAR)); }
inline bool SF_IsDelegateStub          (DWORD dwStubFlags) { LIMITED_METHOD_CONTRACT; return (dwStubFlags < NDIRECTSTUB_FL_INVALID && 0 != (dwStubFlags & NDIRECTSTUB_FL_DELEGATE)); }
inline bool SF_IsHRESULTSwapping       (DWORD dwStubFlags) { LIMITED_METHOD_CONTRACT; return (dwStubFlags < NDIRECTSTUB_FL_INVALID && 0 != (dwStubFlags & NDIRECTSTUB_FL_DOHRESULTSWAPPING)); }
inline bool SF_IsReverseStub           (DWORD dwStubFlags) { LIMITED_METHOD_CONTRACT; return (dwStubFlags < NDIRECTSTUB_FL_INVALID && 0 != (dwStubFlags & NDIRECTSTUB_FL_REVERSE_INTEROP)); }
inline bool SF_IsDebuggableStub        (DWORD dwStubFlags) { LIMITED_METHOD_CONTRACT; return (dwStubFlags < NDIRECTSTUB_FL_INVALID && 0 != (dwStubFlags & NDIRECTSTUB_FL_GENERATEDEBUGGABLEIL)); }
inline bool SF_IsCALLIStub             (DWORD dwStubFlags) { LIMITED_METHOD_CONTRACT; return (dwStubFlags < NDIRECTSTUB_FL_INVALID && 0 != (dwStubFlags & NDIRECTSTUB_FL_UNMANAGED_CALLI)); }
inline bool SF_IsForNumParamBytes      (DWORD dwStubFlags) { LIMITED_METHOD_CONTRACT; return (dwStubFlags < NDIRECTSTUB_FL_INVALID && 0 != (dwStubFlags & NDIRECTSTUB_FL_FOR_NUMPARAMBYTES)); }
inline bool SF_IsStructMarshalStub     (DWORD dwStubFlags) { LIMITED_METHOD_CONTRACT; return (dwStubFlags < NDIRECTSTUB_FL_INVALID && 0 != (dwStubFlags & NDIRECTSTUB_FL_STRUCT_MARSHAL)); }
inline bool SF_IsCheckPendingException (DWORD dwStubFlags) { LIMITED_METHOD_CONTRACT; return (dwStubFlags < NDIRECTSTUB_FL_INVALID && 0 != (dwStubFlags & NDIRECTSTUB_FL_CHECK_PENDING_EXCEPTION)); }

inline bool SF_IsVirtualStaticMethodDispatchStub(DWORD dwStubFlags) { LIMITED_METHOD_CONTRACT; return dwStubFlags == ILSTUB_STATIC_VIRTUAL_DISPATCH_STUB; }

#ifdef FEATURE_ARRAYSTUB_AS_IL
inline bool SF_IsArrayOpStub           (DWORD dwStubFlags) { LIMITED_METHOD_CONTRACT; return ((dwStubFlags == ILSTUB_ARRAYOP_GET) ||
                                                                                              (dwStubFlags == ILSTUB_ARRAYOP_SET) ||
                                                                                              (dwStubFlags == ILSTUB_ARRAYOP_ADDRESS)); }
#endif

#ifdef FEATURE_MULTICASTSTUB_AS_IL
inline bool SF_IsMulticastDelegateStub  (DWORD dwStubFlags) { LIMITED_METHOD_CONTRACT; return (dwStubFlags == ILSTUB_MULTICASTDELEGATE_INVOKE); }
#endif

inline bool SF_IsWrapperDelegateStub    (DWORD dwStubFlags) { LIMITED_METHOD_CONTRACT; return (dwStubFlags == ILSTUB_WRAPPERDELEGATE_INVOKE); }
#ifdef FEATURE_INSTANTIATINGSTUB_AS_IL
inline bool SF_IsUnboxingILStub         (DWORD dwStubFlags) { LIMITED_METHOD_CONTRACT; return (dwStubFlags == ILSTUB_UNBOXINGILSTUB); }
inline bool SF_IsInstantiatingStub      (DWORD dwStubFlags) { LIMITED_METHOD_CONTRACT; return (dwStubFlags == ILSTUB_INSTANTIATINGSTUB); }
#endif
inline bool SF_IsTailCallStoreArgsStub  (DWORD dwStubFlags) { LIMITED_METHOD_CONTRACT; return (dwStubFlags == ILSTUB_TAILCALL_STOREARGS); }
inline bool SF_IsTailCallCallTargetStub (DWORD dwStubFlags) { LIMITED_METHOD_CONTRACT; return (dwStubFlags == ILSTUB_TAILCALL_CALLTARGET); }

inline bool SF_IsCOMStub               (DWORD dwStubFlags) { LIMITED_METHOD_CONTRACT; return COM_ONLY(dwStubFlags < NDIRECTSTUB_FL_INVALID && 0 != (dwStubFlags & NDIRECTSTUB_FL_COM)); }
inline bool SF_IsCOMLateBoundStub      (DWORD dwStubFlags) { LIMITED_METHOD_CONTRACT; return COM_ONLY(dwStubFlags < NDIRECTSTUB_FL_INVALID && 0 != (dwStubFlags & NDIRECTSTUB_FL_COMLATEBOUND)); }
inline bool SF_IsCOMEventCallStub      (DWORD dwStubFlags) { LIMITED_METHOD_CONTRACT; return COM_ONLY(dwStubFlags < NDIRECTSTUB_FL_INVALID && 0 != (dwStubFlags & NDIRECTSTUB_FL_COMEVENTCALL)); }
inline bool SF_IsFieldGetterStub       (DWORD dwStubFlags) { LIMITED_METHOD_CONTRACT; return COM_ONLY(dwStubFlags < NDIRECTSTUB_FL_INVALID && 0 != (dwStubFlags & NDIRECTSTUB_FL_FIELDGETTER)); }
inline bool SF_IsFieldSetterStub       (DWORD dwStubFlags) { LIMITED_METHOD_CONTRACT; return COM_ONLY(dwStubFlags < NDIRECTSTUB_FL_INVALID && 0 != (dwStubFlags & NDIRECTSTUB_FL_FIELDSETTER)); }

inline bool SF_IsSharedStub(DWORD dwStubFlags)
{
    WRAPPER_NO_CONTRACT;

    if (SF_IsTailCallStoreArgsStub(dwStubFlags) || SF_IsTailCallCallTargetStub(dwStubFlags))
    {
        return false;
    }

    if (SF_IsFieldGetterStub(dwStubFlags) || SF_IsFieldSetterStub(dwStubFlags))
    {
        return false;
    }

    return true;
}

inline bool SF_IsForwardStub             (DWORD dwStubFlags) { WRAPPER_NO_CONTRACT; return !SF_IsReverseStub(dwStubFlags); }

inline bool SF_IsForwardPInvokeStub      (DWORD dwStubFlags) { WRAPPER_NO_CONTRACT; return (!SF_IsCOMStub(dwStubFlags) && SF_IsForwardStub(dwStubFlags)); }
inline bool SF_IsReversePInvokeStub      (DWORD dwStubFlags) { WRAPPER_NO_CONTRACT; return (!SF_IsCOMStub(dwStubFlags) && SF_IsReverseStub(dwStubFlags)); }

inline bool SF_IsForwardCOMStub          (DWORD dwStubFlags) { WRAPPER_NO_CONTRACT; return (SF_IsCOMStub(dwStubFlags) && SF_IsForwardStub(dwStubFlags)); }
inline bool SF_IsReverseCOMStub          (DWORD dwStubFlags) { WRAPPER_NO_CONTRACT; return (SF_IsCOMStub(dwStubFlags) && SF_IsReverseStub(dwStubFlags)); }

inline bool SF_IsForwardDelegateStub     (DWORD dwStubFlags) { WRAPPER_NO_CONTRACT; return (SF_IsDelegateStub(dwStubFlags) && SF_IsForwardStub(dwStubFlags)); }
inline bool SF_IsReverseDelegateStub     (DWORD dwStubFlags) { WRAPPER_NO_CONTRACT; return (SF_IsDelegateStub(dwStubFlags) && SF_IsReverseStub(dwStubFlags)); }

#undef COM_ONLY

inline void SF_ConsistencyCheck(DWORD dwStubFlags)
{
    LIMITED_METHOD_CONTRACT;

    // Late bound and event calls imply COM
    CONSISTENCY_CHECK(!(SF_IsCOMLateBoundStub(dwStubFlags) && !SF_IsCOMStub(dwStubFlags)));
    CONSISTENCY_CHECK(!(SF_IsCOMEventCallStub(dwStubFlags) && !SF_IsCOMStub(dwStubFlags)));

    // Field accessors imply reverse COM
    CONSISTENCY_CHECK(!(SF_IsFieldGetterStub(dwStubFlags) && !SF_IsReverseCOMStub(dwStubFlags)));
    CONSISTENCY_CHECK(!(SF_IsFieldSetterStub(dwStubFlags) && !SF_IsReverseCOMStub(dwStubFlags)));

    // Field accessors are always HRESULT swapping
    CONSISTENCY_CHECK(!(SF_IsFieldGetterStub(dwStubFlags) && !SF_IsHRESULTSwapping(dwStubFlags)));
    CONSISTENCY_CHECK(!(SF_IsFieldSetterStub(dwStubFlags) && !SF_IsHRESULTSwapping(dwStubFlags)));

    // Delegate stubs are not COM
    CONSISTENCY_CHECK(!(SF_IsDelegateStub(dwStubFlags) && SF_IsCOMStub(dwStubFlags)));

    // Struct marshal stubs are not COM or HRESULT swapping stubs
    CONSISTENCY_CHECK(!(SF_IsStructMarshalStub(dwStubFlags) && SF_IsCOMStub(dwStubFlags)));
    CONSISTENCY_CHECK(!(SF_IsStructMarshalStub(dwStubFlags) && SF_IsHRESULTSwapping(dwStubFlags)));
    CONSISTENCY_CHECK(!(SF_IsStructMarshalStub(dwStubFlags) && SF_IsReverseCOMStub(dwStubFlags)));
}

enum ETW_IL_STUB_FLAGS
{
    ETW_IL_STUB_FLAGS_REVERSE_INTEROP       = 0x00000001,
    ETW_IL_STUB_FLAGS_COM_INTEROP           = 0x00000002,
    ETW_IL_STUB_FLAGS_NGENED_STUB           = 0x00000004,
    ETW_IL_STUB_FLAGS_DELEGATE              = 0x00000008,
    ETW_IL_STUB_FLAGS_VARARG                = 0x00000010,
    ETW_IL_STUB_FLAGS_UNMANAGED_CALLI       = 0x00000020,
    ETW_IL_STUB_FLAGS_STRUCT_MARSHAL        = 0x00000040
};

//---------------------------------------------------------
// PInvoke has three flavors: DllImport M->U, Delegate M->U and Delegate U->M
// Each flavor uses rougly the same mechanism to marshal and place the call and so
// each flavor supports roughly the same switches. Those switches which can be
// statically determined via CAs (DllImport, UnmanagedFunctionPointer,
// BestFitMappingAttribute, etc) or via MetaSig are parsed and unified by this
// class. There are two flavors of constructor, one for NDirectMethodDescs and one
// for Delegates.
//---------------------------------------------------------
struct PInvokeStaticSigInfo
{
public:
    PInvokeStaticSigInfo() { LIMITED_METHOD_CONTRACT; }

    PInvokeStaticSigInfo(_In_ const Signature& sig, _In_ Module* pModule);

    PInvokeStaticSigInfo(_In_ MethodDesc* pMdDelegate);

    PInvokeStaticSigInfo(_In_ MethodDesc* pMD, _Outptr_opt_ LPCUTF8* pLibName, _Outptr_opt_ LPCUTF8* pEntryPointName);

private:
    void ThrowError(_In_ UINT errorResourceID);
    void InitCallConv(_In_ CorInfoCallConvExtension callConv, _In_ MethodDesc* pMD);
    void InitCallConv(_In_ CorInfoCallConvExtension callConv, _In_ BOOL bIsVarArg);
    void DllImportInit(_In_ MethodDesc* pMD, _Outptr_opt_ LPCUTF8* pLibName, _Outptr_opt_ LPCUTF8* pEntryPointName);
    void PreInit(_In_ Module* pModule, _In_ MethodTable* pClass);
    void PreInit(_In_ MethodDesc* pMD);

private:
    enum
    {
        PINVOKE_STATIC_SIGINFO_SUPPRESS_GC_TRANSITION = 0x0001,
        PINVOKE_STATIC_SIGINFO_THROW_ON_UNMAPPABLE_CHAR = 0x0002,
        PINVOKE_STATIC_SIGINFO_BEST_FIT = 0x0004,

        COR_NATIVE_LINK_TYPE_MASK = 0x0038,  // 0000 0000 0011 1000  <--- These 3 1's make the link type mask

        COR_NATIVE_LINK_FLAGS_MASK = 0x00C0, //0000 0000 1100 0000  <---- These 2 bits make up the link flags

        PINVOKE_STATIC_SIGINFO_IS_DELEGATE_INTEROP = 0x0100,

    };
    #define COR_NATIVE_LINK_TYPE_SHIFT 3 // Keep in synch with above mask
    #define COR_NATIVE_LINK_FLAGS_SHIFT 6  // Keep in synch with above mask

public: // public getters
    DWORD GetStubFlags() const
    {
        WRAPPER_NO_CONTRACT;
        DWORD flags = 0;
        if (GetThrowOnUnmappableChar())
            flags |= NDIRECTSTUB_FL_THROWONUNMAPPABLECHAR;

        if (GetBestFitMapping())
            flags |= NDIRECTSTUB_FL_BESTFIT;

        if (IsDelegateInterop())
            flags |= NDIRECTSTUB_FL_DELEGATE;

        if (ShouldSuppressGCTransition())
            flags |= NDIRECTSTUB_FL_SUPPRESSGCTRANSITION;

        return flags;
    }
    Module* GetModule() const { LIMITED_METHOD_CONTRACT; return m_pModule; }
    BOOL IsDelegateInterop() const { LIMITED_METHOD_CONTRACT; return m_wFlags & PINVOKE_STATIC_SIGINFO_IS_DELEGATE_INTEROP; }
    CorInfoCallConvExtension GetCallConv() const { LIMITED_METHOD_CONTRACT; return m_callConv; }
    Signature GetSignature() const { LIMITED_METHOD_CONTRACT; return m_sig; }
    CorNativeLinkType GetCharSet() const { LIMITED_METHOD_CONTRACT; return (CorNativeLinkType)((m_wFlags & COR_NATIVE_LINK_TYPE_MASK) >> COR_NATIVE_LINK_TYPE_SHIFT); }
    CorNativeLinkFlags GetLinkFlags() const { LIMITED_METHOD_CONTRACT; return (CorNativeLinkFlags)((m_wFlags & COR_NATIVE_LINK_FLAGS_MASK) >> COR_NATIVE_LINK_FLAGS_SHIFT); }

public: // private getters
    BOOL GetThrowOnUnmappableChar() const { LIMITED_METHOD_CONTRACT; return m_wFlags & PINVOKE_STATIC_SIGINFO_THROW_ON_UNMAPPABLE_CHAR; }
    BOOL GetBestFitMapping() const { LIMITED_METHOD_CONTRACT; return m_wFlags & PINVOKE_STATIC_SIGINFO_BEST_FIT; }
    BOOL ShouldSuppressGCTransition() const { LIMITED_METHOD_CONTRACT; return m_wFlags & PINVOKE_STATIC_SIGINFO_SUPPRESS_GC_TRANSITION; }

private: // setters
    void SetThrowOnUnmappableChar(BOOL throwOnUnmappableChar)
    {
        LIMITED_METHOD_CONTRACT;
        if (throwOnUnmappableChar)
            m_wFlags |= PINVOKE_STATIC_SIGINFO_THROW_ON_UNMAPPABLE_CHAR;
        else
            m_wFlags &= ~PINVOKE_STATIC_SIGINFO_THROW_ON_UNMAPPABLE_CHAR;
    }
    void SetBestFitMapping(BOOL bestFit)
    {
        LIMITED_METHOD_CONTRACT;
        if (bestFit)
            m_wFlags |= PINVOKE_STATIC_SIGINFO_BEST_FIT;
        else
            m_wFlags &= ~PINVOKE_STATIC_SIGINFO_BEST_FIT;
    }
    void SetShouldSuppressGCTransition(BOOL suppress)
    {
        LIMITED_METHOD_CONTRACT;
        if (suppress)
            m_wFlags |= PINVOKE_STATIC_SIGINFO_SUPPRESS_GC_TRANSITION;
        else
            m_wFlags &= ~PINVOKE_STATIC_SIGINFO_SUPPRESS_GC_TRANSITION;
    }
    void SetIsDelegateInterop(BOOL delegateInterop)
    {
        LIMITED_METHOD_CONTRACT;
        if (delegateInterop)
            m_wFlags |= PINVOKE_STATIC_SIGINFO_IS_DELEGATE_INTEROP;
        else
            m_wFlags &= ~PINVOKE_STATIC_SIGINFO_IS_DELEGATE_INTEROP;
    }
    void SetCharSet(CorNativeLinkType linktype)
    {
        LIMITED_METHOD_CONTRACT;
        _ASSERTE( linktype == (linktype & (COR_NATIVE_LINK_TYPE_MASK >> COR_NATIVE_LINK_TYPE_SHIFT)));
        // Clear out the old value first
        m_wFlags &= (~COR_NATIVE_LINK_TYPE_MASK);
        // Then set the given value
        m_wFlags |= (linktype << COR_NATIVE_LINK_TYPE_SHIFT);
    }
    void SetLinkFlags(CorNativeLinkFlags linkflags)
    {
        LIMITED_METHOD_CONTRACT;
        _ASSERTE( linkflags == (linkflags & (COR_NATIVE_LINK_FLAGS_MASK >> COR_NATIVE_LINK_FLAGS_SHIFT)));
        // Clear out the old value first
        m_wFlags &= (~COR_NATIVE_LINK_FLAGS_MASK);
        // Then set the given value
        m_wFlags |= (linkflags << COR_NATIVE_LINK_FLAGS_SHIFT);
    }

private:
    Module* m_pModule;
    Signature m_sig;
    CorInfoCallConvExtension m_callConv;
    WORD m_wFlags;
};


#include "stubgen.h"

#ifndef DACCESS_COMPILE
class NDirectStubLinker : public ILStubLinker
{
public:
    NDirectStubLinker(
                DWORD dwStubFlags,
                Module* pModule,
                const Signature &signature,
                SigTypeContext *pTypeContext,
                MethodDesc* pTargetMD,
                int  iLCIDParamIdx);

    void    SetCallingConvention(CorInfoCallConvExtension unmngCallConv, BOOL fIsVarArg);

    void    Begin(DWORD dwStubFlags);
    void    End(DWORD dwStubFlags);
    void    DoNDirect(ILCodeStream *pcsEmit, DWORD dwStubFlags, MethodDesc * pStubMD);
    void    EmitLogNativeArgument(ILCodeStream* pslILEmit, DWORD dwPinnedLocal);
    void    LoadCleanupWorkList(ILCodeStream* pcsEmit);
#ifdef PROFILING_SUPPORTED
    DWORD   EmitProfilerBeginTransitionCallback(ILCodeStream* pcsEmit, DWORD dwStubFlags);
    void    EmitProfilerEndTransitionCallback(ILCodeStream* pcsEmit, DWORD dwStubFlags, DWORD dwMethodDescLocalNum);
#endif
#ifdef VERIFY_HEAP
    void    EmitValidateLocal(ILCodeStream* pcsEmit, DWORD dwLocalNum, bool fIsByref, DWORD dwStubFlags);
    void    EmitObjectValidation(ILCodeStream* pcsEmit, DWORD dwStubFlags);
#endif // VERIFY_HEAP
    void    EmitLoadStubContext(ILCodeStream* pcsEmit, DWORD dwStubFlags);
    void    GenerateInteropParamException(ILCodeStream* pcsEmit);
    void    NeedsCleanupList();

#ifdef FEATURE_COMINTEROP
    DWORD   GetTargetInterfacePointerLocalNum();
    DWORD   GetTargetEntryPointLocalNum();
    void    EmitLoadRCWThis(ILCodeStream *pcsEmit, DWORD dwStubFlags);
#endif // FEATURE_COMINTEROP
    DWORD   GetCleanupWorkListLocalNum();
    DWORD   GetThreadLocalNum();
    DWORD   GetReturnValueLocalNum();
    void    SetCleanupNeeded();
    void    SetExceptionCleanupNeeded();
    BOOL    IsCleanupWorkListSetup();
    void    GetCleanupFinallyOffsets(ILStubEHClause * pClause);

    void    SetInteropParamExceptionInfo(UINT resID, UINT paramIdx);
    bool    HasInteropParamExceptionInfo();
    bool    TargetHasThis()
    {
        return m_targetHasThis == TRUE;
    }

    void ClearCode();

    enum
    {
        CLEANUP_INDEX_ARG0_MARSHAL     = 0x00000000,  // cleanup index of the first argument (marshal and retval unmarshal stream)
        CLEANUP_INDEX_RETVAL_UNMARSHAL = 0x3fffffff,  // cleanup index of the return value (retval unmarshal stream)
        CLEANUP_INDEX_ARG0_UNMARSHAL   = 0x40000000,  // cleanup index of the first argument (unmarshal stream)
        CLEANUP_INDEX_ALL_DONE         = 0x7ffffffe   // everything was successfully marshaled and unmarshaled, no exception thrown
    };

    enum ArgCleanupBranchKind
    {
        BranchIfMarshaled,
        BranchIfNotMarshaled
    };

    void    EmitSetArgMarshalIndex(ILCodeStream* pcsEmit, UINT uArgIdx);
    void    EmitCheckForArgCleanup(ILCodeStream* pcsEmit, UINT uArgIdx, ArgCleanupBranchKind branchKind, ILCodeLabel* pSkipCleanupLabel);

    int     GetLCIDParamIdx();

    ILCodeStream* GetSetupCodeStream();
    ILCodeStream* GetMarshalCodeStream();
    ILCodeStream* GetUnmarshalCodeStream();
    ILCodeStream* GetReturnUnmarshalCodeStream();
    ILCodeStream* GetDispatchCodeStream();
    ILCodeStream* GetCleanupCodeStream();
    ILCodeStream* GetExceptionCleanupCodeStream();

protected:
    BOOL            IsCleanupNeeded();
    BOOL            IsExceptionCleanupNeeded();
    void            InitCleanupCode();
    void            InitExceptionCleanupCode();



    ILCodeStream*   m_pcsSetup;
    ILCodeStream*   m_pcsMarshal;
    ILCodeStream*   m_pcsDispatch;
    ILCodeStream*   m_pcsRetUnmarshal;
    ILCodeStream*   m_pcsUnmarshal;
    ILCodeStream*   m_pcsExceptionCleanup;
    ILCodeStream*   m_pcsCleanup;


    ILCodeLabel*        m_pCleanupTryBeginLabel;
    ILCodeLabel*        m_pCleanupTryEndLabel;
    ILCodeLabel*        m_pCleanupFinallyBeginLabel;
    ILCodeLabel*        m_pCleanupFinallyEndLabel;
    ILCodeLabel*        m_pSkipExceptionCleanupLabel;

#ifdef FEATURE_COMINTEROP
    DWORD               m_dwTargetInterfacePointerLocalNum;
    DWORD               m_dwTargetEntryPointLocalNum;
#endif // FEATURE_COMINTEROP

    BOOL                m_fHasCleanupCode;
    BOOL                m_fHasExceptionCleanupCode;
    BOOL                m_fCleanupWorkListIsSetup;
    BOOL                m_targetHasThis;
    DWORD               m_dwThreadLocalNum;                 // managed-to-native only
    DWORD               m_dwArgMarshalIndexLocalNum;
    DWORD               m_dwCleanupWorkListLocalNum;
    DWORD               m_dwRetValLocalNum;


    UINT                m_ErrorResID;
    UINT                m_ErrorParamIdx;
    int                 m_iLCIDParamIdx;

    DWORD               m_dwStubFlags;
};
#endif // DACCESS_COMPILE

// This attempts to guess whether a target is an API call that uses SetLastError to communicate errors.
BOOL HeuristicDoesThisLookLikeAGetLastErrorCall(LPBYTE pTarget);
DWORD STDMETHODCALLTYPE FalseGetLastError();

PCODE GetILStubForCalli(VASigCookie *pVASigCookie, MethodDesc *pMD);

PCODE JitILStub(MethodDesc* pStubMD);
PCODE GetStubForInteropMethod(MethodDesc* pMD, DWORD dwStubFlags = 0);

#ifdef FEATURE_COMINTEROP
// Resolve and return the predefined IL stub method
HRESULT FindPredefinedILStubMethod(MethodDesc *pTargetMD, DWORD dwStubFlags, MethodDesc **ppRetStubMD);
#endif // FEATURE_COMINTEROP

#ifndef DACCESS_COMPILE
void MarshalStructViaILStub(MethodDesc* pStubMD, void* pManagedData, void* pNativeData, StructMarshalStubs::MarshalOperation operation, void** ppCleanupWorkList = nullptr);
void MarshalStructViaILStubCode(PCODE pStubCode, void* pManagedData, void* pNativeData, StructMarshalStubs::MarshalOperation operation, void** ppCleanupWorkList = nullptr);
#endif // DACCESS_COMPILE

//
// Limit length of string field in IL stub ETW events so that the whole
// IL stub ETW events won't exceed 64KB
//
#define ETW_IL_STUB_EVENT_STRING_FIELD_MAXSIZE      (1024)
#define ETW_IL_STUB_EVENT_CODE_STRING_FIELD_MAXSIZE (1024*32)

#endif // __dllimport_h__
