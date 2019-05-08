// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// 
// File: DllImport.h
//

#ifndef __dllimport_h__
#define __dllimport_h__

#include "util.hpp"

class ILStubHashBlob;
class NDirectStubParameters;
struct PInvokeStaticSigInfo;
class LoadLibErrorTracker;

// This structure groups together data that describe the signature for which a marshaling stub is being generated.
struct StubSigDesc
{
public:
    StubSigDesc(MethodDesc *pMD, PInvokeStaticSigInfo* pSigInfo = NULL);
    StubSigDesc(MethodDesc *pMD, Signature sig, Module *m_pModule);

    MethodDesc        *m_pMD;
    Signature          m_sig;
    Module            *m_pModule;
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
};

//=======================================================================
// Collects code and data pertaining to the NDirect interface.
//=======================================================================
class NDirect
{
    friend class NDirectMethodDesc;

public:
    //---------------------------------------------------------
    // One-time init
    //---------------------------------------------------------
    static void Init();

    //---------------------------------------------------------
    // Does a class or method have a NAT_L CustomAttribute?
    //
    // S_OK    = yes
    // S_FALSE = no
    // FAILED  = unknown because something failed.
    //---------------------------------------------------------
    static HRESULT HasNAT_LAttribute(IMDInternalImport *pInternalImport, mdToken token, DWORD dwMemberAttrs);

    static LPVOID NDirectGetEntryPoint(NDirectMethodDesc *pMD, HINSTANCE hMod);
    static NATIVE_LIBRARY_HANDLE LoadLibraryFromPath(LPCWSTR libraryPath, BOOL throwOnError);
    static NATIVE_LIBRARY_HANDLE LoadLibraryByName(LPCWSTR name, Assembly *callingAssembly, 
                                                   BOOL hasDllImportSearchPathFlags, DWORD dllImportSearchPathFlags, 
                                                   BOOL throwOnError);
    static HINSTANCE LoadLibraryModule(NDirectMethodDesc * pMD, LoadLibErrorTracker *pErrorTracker);
    static void FreeNativeLibrary(NATIVE_LIBRARY_HANDLE handle);
    static INT_PTR GetNativeLibraryExport(NATIVE_LIBRARY_HANDLE handle, LPCWSTR symbolName, BOOL throwOnError);

    static VOID NDirectLink(NDirectMethodDesc *pMD);

    // Either MD or signature & module must be given.
    static BOOL MarshalingRequired(MethodDesc *pMD, PCCOR_SIGNATURE pSig = NULL, Module *pModule = NULL);
    static void PopulateNDirectMethodDesc(NDirectMethodDesc* pNMD, PInvokeStaticSigInfo* pSigInfo, BOOL throwOnError = TRUE);

    static MethodDesc* CreateCLRToNativeILStub(
                    StubSigDesc*       pSigDesc,
                    CorNativeLinkType  nlType,
                    CorNativeLinkFlags nlFlags,
                    CorPinvokeMap      unmgdCallConv,
                    DWORD              dwStubFlags); // NDirectStubFlags
                    
#ifdef FEATURE_COMINTEROP
    static MethodDesc* CreateFieldAccessILStub(
                    PCCOR_SIGNATURE    szMetaSig,
                    DWORD              cbMetaSigSize,
                    Module*            pModule,
                    mdFieldDef         fd,
                    DWORD              dwStubFlags, // NDirectStubFlags
                    FieldDesc*         pFD);
#endif // FEATURE_COMINTEROP

    static MethodDesc* CreateCLRToNativeILStub(PInvokeStaticSigInfo* pSigInfo,
                             DWORD dwStubFlags,
                             MethodDesc* pMD);

    static MethodDesc*      GetILStubMethodDesc(NDirectMethodDesc* pNMD, PInvokeStaticSigInfo* pSigInfo, DWORD dwNGenStubFlags);
    static MethodDesc*      GetStubMethodDesc(MethodDesc *pTargetMD, NDirectStubParameters* pParams, ILStubHashBlob* pHashParams, AllocMemTracker* pamTracker, bool& bILStubCreator, MethodDesc* pLastMD);
    static void             AddMethodDescChunkWithLockTaken(NDirectStubParameters* pParams, MethodDesc *pMD);
    static void             RemoveILStubCacheEntry(NDirectStubParameters* pParams, ILStubHashBlob* pHashParams);
    static ILStubHashBlob*  CreateHashBlob(NDirectStubParameters* pParams);
    static PCODE            GetStubForILStub(NDirectMethodDesc* pNMD, MethodDesc** ppStubMD, DWORD dwStubFlags);
    static PCODE            GetStubForILStub(MethodDesc* pMD, MethodDesc** ppStubMD, DWORD dwStubFlags);

    inline static ILStubCache*     GetILStubCache(NDirectStubParameters* pParams);

private:
    NDirect() {LIMITED_METHOD_CONTRACT;};     // prevent "new"'s on this class

    static NATIVE_LIBRARY_HANDLE LoadFromNativeDllSearchDirectories(LPCWSTR libName, DWORD flags, LoadLibErrorTracker *pErrorTracker);
    static NATIVE_LIBRARY_HANDLE LoadFromPInvokeAssemblyDirectory(Assembly *pAssembly, LPCWSTR libName, DWORD flags, LoadLibErrorTracker *pErrorTracker);
    static NATIVE_LIBRARY_HANDLE LoadLibraryModuleViaHost(NDirectMethodDesc * pMD, LPCWSTR wszLibName);
    static NATIVE_LIBRARY_HANDLE LoadLibraryModuleViaEvent(NDirectMethodDesc * pMD, LPCWSTR wszLibName);
    static NATIVE_LIBRARY_HANDLE LoadLibraryModuleViaCallback(NDirectMethodDesc * pMD, LPCWSTR wszLibName);
    static NATIVE_LIBRARY_HANDLE LoadLibraryModuleBySearch(NDirectMethodDesc * pMD, LoadLibErrorTracker * pErrorTracker, LPCWSTR wszLibName);
    static NATIVE_LIBRARY_HANDLE LoadLibraryModuleBySearch(Assembly *callingAssembly, BOOL searchAssemblyDirectory, DWORD dllImportSearchPathFlags, LoadLibErrorTracker * pErrorTracker, LPCWSTR wszLibName);

#if !defined(FEATURE_PAL)
    // Indicates if the OS supports the new secure LoadLibraryEx flags introduced in KB2533623
    static bool         s_fSecureLoadLibrarySupported;

public:
    static bool SecureLoadLibrarySupported()
    {
        LIMITED_METHOD_CONTRACT;
        return s_fSecureLoadLibrarySupported;
    }
#endif // !FEATURE_PAL
};

//----------------------------------------------------------------
// Flags passed to CreateNDirectStub that control stub generation
//----------------------------------------------------------------
enum NDirectStubFlags
{
    NDIRECTSTUB_FL_CONVSIGASVARARG          = 0x00000001,
    NDIRECTSTUB_FL_BESTFIT                  = 0x00000002,
    NDIRECTSTUB_FL_THROWONUNMAPPABLECHAR    = 0x00000004,
    NDIRECTSTUB_FL_NGENEDSTUB               = 0x00000008,
    NDIRECTSTUB_FL_DELEGATE                 = 0x00000010,
    NDIRECTSTUB_FL_DOHRESULTSWAPPING        = 0x00000020,
    NDIRECTSTUB_FL_REVERSE_INTEROP          = 0x00000040,
#ifdef FEATURE_COMINTEROP
    NDIRECTSTUB_FL_COM                      = 0x00000080,
#endif // FEATURE_COMINTEROP
    NDIRECTSTUB_FL_NGENEDSTUBFORPROFILING   = 0x00000100,
    NDIRECTSTUB_FL_GENERATEDEBUGGABLEIL     = 0x00000200,
    // unused                               = 0x00000400,
    NDIRECTSTUB_FL_UNMANAGED_CALLI          = 0x00000800,
    NDIRECTSTUB_FL_TRIGGERCCTOR             = 0x00001000,
#ifdef FEATURE_COMINTEROP
    NDIRECTSTUB_FL_FIELDGETTER              = 0x00002000, // COM->CLR field getter
    NDIRECTSTUB_FL_FIELDSETTER              = 0x00004000, // COM->CLR field setter
    NDIRECTSTUB_FL_WINRT                    = 0x00008000,
    NDIRECTSTUB_FL_WINRTDELEGATE            = 0x00010000,
    NDIRECTSTUB_FL_WINRTSHAREDGENERIC       = 0x00020000, // stub for methods on shared generic interfaces (only used in the forward direction)
    NDIRECTSTUB_FL_WINRTCTOR                = 0x00080000,
    NDIRECTSTUB_FL_WINRTCOMPOSITION         = 0x00100000, // set along with WINRTCTOR
    NDIRECTSTUB_FL_WINRTSTATIC              = 0x00200000,

    NDIRECTSTUB_FL_WINRTHASREDIRECTION      = 0x00800000, // the stub may tail-call to a static stub in mscorlib, not shareable
#endif // FEATURE_COMINTEROP

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
    ILSTUB_ARRAYOP_ADDRESS               = 0x80000004,
#endif
#ifdef FEATURE_MULTICASTSTUB_AS_IL
    ILSTUB_MULTICASTDELEGATE_INVOKE      = 0x80000010,
#endif
#ifdef FEATURE_STUBS_AS_IL
    ILSTUB_UNBOXINGILSTUB                = 0x80000020,
    ILSTUB_INSTANTIATINGSTUB             = 0x80000040,
    ILSTUB_SECUREDELEGATE_INVOKE         = 0x80000080,
#endif
};

#ifdef FEATURE_COMINTEROP
#define COM_ONLY(x) (x)
#else // FEATURE_COMINTEROP
#define COM_ONLY(x) false
#endif // FEATURE_COMINTEROP

inline bool SF_IsVarArgStub            (DWORD dwStubFlags) { LIMITED_METHOD_CONTRACT; return (dwStubFlags < NDIRECTSTUB_FL_INVALID && 0 != (dwStubFlags & NDIRECTSTUB_FL_CONVSIGASVARARG)); }
inline bool SF_IsBestFit               (DWORD dwStubFlags) { LIMITED_METHOD_CONTRACT; return (dwStubFlags < NDIRECTSTUB_FL_INVALID && 0 != (dwStubFlags & NDIRECTSTUB_FL_BESTFIT)); }
inline bool SF_IsThrowOnUnmappableChar (DWORD dwStubFlags) { LIMITED_METHOD_CONTRACT; return (dwStubFlags < NDIRECTSTUB_FL_INVALID && 0 != (dwStubFlags & NDIRECTSTUB_FL_THROWONUNMAPPABLECHAR)); }
inline bool SF_IsNGENedStub            (DWORD dwStubFlags) { LIMITED_METHOD_CONTRACT; return (dwStubFlags < NDIRECTSTUB_FL_INVALID && 0 != (dwStubFlags & NDIRECTSTUB_FL_NGENEDSTUB)); }
inline bool SF_IsDelegateStub          (DWORD dwStubFlags) { LIMITED_METHOD_CONTRACT; return (dwStubFlags < NDIRECTSTUB_FL_INVALID && 0 != (dwStubFlags & NDIRECTSTUB_FL_DELEGATE)); }
inline bool SF_IsHRESULTSwapping       (DWORD dwStubFlags) { LIMITED_METHOD_CONTRACT; return (dwStubFlags < NDIRECTSTUB_FL_INVALID && 0 != (dwStubFlags & NDIRECTSTUB_FL_DOHRESULTSWAPPING)); }
inline bool SF_IsReverseStub           (DWORD dwStubFlags) { LIMITED_METHOD_CONTRACT; return (dwStubFlags < NDIRECTSTUB_FL_INVALID && 0 != (dwStubFlags & NDIRECTSTUB_FL_REVERSE_INTEROP)); }
inline bool SF_IsNGENedStubForProfiling(DWORD dwStubFlags) { LIMITED_METHOD_CONTRACT; return (dwStubFlags < NDIRECTSTUB_FL_INVALID && 0 != (dwStubFlags & NDIRECTSTUB_FL_NGENEDSTUBFORPROFILING)); }
inline bool SF_IsDebuggableStub        (DWORD dwStubFlags) { LIMITED_METHOD_CONTRACT; return (dwStubFlags < NDIRECTSTUB_FL_INVALID && 0 != (dwStubFlags & NDIRECTSTUB_FL_GENERATEDEBUGGABLEIL)); }
inline bool SF_IsCALLIStub             (DWORD dwStubFlags) { LIMITED_METHOD_CONTRACT; return (dwStubFlags < NDIRECTSTUB_FL_INVALID && 0 != (dwStubFlags & NDIRECTSTUB_FL_UNMANAGED_CALLI)); }
inline bool SF_IsStubWithCctorTrigger  (DWORD dwStubFlags) { LIMITED_METHOD_CONTRACT; return (dwStubFlags < NDIRECTSTUB_FL_INVALID && 0 != (dwStubFlags & NDIRECTSTUB_FL_TRIGGERCCTOR)); }
inline bool SF_IsForNumParamBytes      (DWORD dwStubFlags) { LIMITED_METHOD_CONTRACT; return (dwStubFlags < NDIRECTSTUB_FL_INVALID && 0 != (dwStubFlags & NDIRECTSTUB_FL_FOR_NUMPARAMBYTES)); }

#ifdef FEATURE_ARRAYSTUB_AS_IL
inline bool SF_IsArrayOpStub           (DWORD dwStubFlags) { LIMITED_METHOD_CONTRACT; return ((dwStubFlags == ILSTUB_ARRAYOP_GET) || 
                                                                                              (dwStubFlags == ILSTUB_ARRAYOP_SET) ||
                                                                                              (dwStubFlags == ILSTUB_ARRAYOP_ADDRESS)); }
#endif

#ifdef FEATURE_MULTICASTSTUB_AS_IL
inline bool SF_IsMulticastDelegateStub  (DWORD dwStubFlags) { LIMITED_METHOD_CONTRACT; return (dwStubFlags == ILSTUB_MULTICASTDELEGATE_INVOKE); }
#endif

#ifdef FEATURE_STUBS_AS_IL
inline bool SF_IsSecureDelegateStub  (DWORD dwStubFlags)    { LIMITED_METHOD_CONTRACT; return (dwStubFlags == ILSTUB_SECUREDELEGATE_INVOKE); }
inline bool SF_IsUnboxingILStub         (DWORD dwStubFlags) { LIMITED_METHOD_CONTRACT; return (dwStubFlags == ILSTUB_UNBOXINGILSTUB); }
inline bool SF_IsInstantiatingStub      (DWORD dwStubFlags) { LIMITED_METHOD_CONTRACT; return (dwStubFlags == ILSTUB_INSTANTIATINGSTUB); }
#endif

inline bool SF_IsCOMStub               (DWORD dwStubFlags) { LIMITED_METHOD_CONTRACT; return COM_ONLY(dwStubFlags < NDIRECTSTUB_FL_INVALID && 0 != (dwStubFlags & NDIRECTSTUB_FL_COM)); }
inline bool SF_IsWinRTStub             (DWORD dwStubFlags) { LIMITED_METHOD_CONTRACT; return COM_ONLY(dwStubFlags < NDIRECTSTUB_FL_INVALID && 0 != (dwStubFlags & NDIRECTSTUB_FL_WINRT)); }
inline bool SF_IsCOMLateBoundStub      (DWORD dwStubFlags) { LIMITED_METHOD_CONTRACT; return COM_ONLY(dwStubFlags < NDIRECTSTUB_FL_INVALID && 0 != (dwStubFlags & NDIRECTSTUB_FL_COMLATEBOUND)); }
inline bool SF_IsCOMEventCallStub      (DWORD dwStubFlags) { LIMITED_METHOD_CONTRACT; return COM_ONLY(dwStubFlags < NDIRECTSTUB_FL_INVALID && 0 != (dwStubFlags & NDIRECTSTUB_FL_COMEVENTCALL)); }
inline bool SF_IsFieldGetterStub       (DWORD dwStubFlags) { LIMITED_METHOD_CONTRACT; return COM_ONLY(dwStubFlags < NDIRECTSTUB_FL_INVALID && 0 != (dwStubFlags & NDIRECTSTUB_FL_FIELDGETTER)); }
inline bool SF_IsFieldSetterStub       (DWORD dwStubFlags) { LIMITED_METHOD_CONTRACT; return COM_ONLY(dwStubFlags < NDIRECTSTUB_FL_INVALID && 0 != (dwStubFlags & NDIRECTSTUB_FL_FIELDSETTER)); }
inline bool SF_IsWinRTDelegateStub     (DWORD dwStubFlags) { LIMITED_METHOD_CONTRACT; return COM_ONLY(dwStubFlags < NDIRECTSTUB_FL_INVALID && 0 != (dwStubFlags & NDIRECTSTUB_FL_WINRTDELEGATE)); }
inline bool SF_IsWinRTCtorStub         (DWORD dwStubFlags) { LIMITED_METHOD_CONTRACT; return COM_ONLY(dwStubFlags < NDIRECTSTUB_FL_INVALID && 0 != (dwStubFlags & NDIRECTSTUB_FL_WINRTCTOR)); }
inline bool SF_IsWinRTCompositionStub  (DWORD dwStubFlags) { LIMITED_METHOD_CONTRACT; return COM_ONLY(dwStubFlags < NDIRECTSTUB_FL_INVALID && 0 != (dwStubFlags & NDIRECTSTUB_FL_WINRTCOMPOSITION)); }
inline bool SF_IsWinRTStaticStub       (DWORD dwStubFlags) { LIMITED_METHOD_CONTRACT; return COM_ONLY(dwStubFlags < NDIRECTSTUB_FL_INVALID && 0 != (dwStubFlags & NDIRECTSTUB_FL_WINRTSTATIC)); }
inline bool SF_IsWinRTSharedGenericStub(DWORD dwStubFlags) { LIMITED_METHOD_CONTRACT; return COM_ONLY(dwStubFlags < NDIRECTSTUB_FL_INVALID && 0 != (dwStubFlags & NDIRECTSTUB_FL_WINRTSHAREDGENERIC)); }
inline bool SF_IsWinRTHasRedirection   (DWORD dwStubFlags) { LIMITED_METHOD_CONTRACT; return COM_ONLY(dwStubFlags < NDIRECTSTUB_FL_INVALID && 0 != (dwStubFlags & NDIRECTSTUB_FL_WINRTHASREDIRECTION)); }

inline bool SF_IsSharedStub(DWORD dwStubFlags)
{
    WRAPPER_NO_CONTRACT;

    if (SF_IsWinRTHasRedirection(dwStubFlags))
    {
        // tail-call to a target-specific mscorlib routine is burned into the stub
        return false;
    }

    return !SF_IsFieldGetterStub(dwStubFlags) && !SF_IsFieldSetterStub(dwStubFlags);
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
}

enum ETW_IL_STUB_FLAGS
{
    ETW_IL_STUB_FLAGS_REVERSE_INTEROP       = 0x00000001,
    ETW_IL_STUB_FLAGS_COM_INTEROP           = 0x00000002,    
    ETW_IL_STUB_FLAGS_NGENED_STUB           = 0x00000004,
    ETW_IL_STUB_FLAGS_DELEGATE              = 0x00000008,    
    ETW_IL_STUB_FLAGS_VARARG                = 0x00000010,
    ETW_IL_STUB_FLAGS_UNMANAGED_CALLI       = 0x00000020
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
    enum ThrowOnError { THROW_ON_ERROR = TRUE, NO_THROW_ON_ERROR = FALSE };

public:     
    PInvokeStaticSigInfo() { LIMITED_METHOD_CONTRACT; }

    PInvokeStaticSigInfo(Signature sig, Module* pModule, ThrowOnError throwOnError = THROW_ON_ERROR);
    
    PInvokeStaticSigInfo(MethodDesc* pMdDelegate, ThrowOnError throwOnError = THROW_ON_ERROR);
    
    PInvokeStaticSigInfo(MethodDesc* pMD, LPCUTF8 *pLibName, LPCUTF8 *pEntryPointName, ThrowOnError throwOnError = THROW_ON_ERROR);

public:     
    void ReportErrors();
    
private:
    void InitCallConv(CorPinvokeMap callConv, BOOL bIsVarArg);
    void DllImportInit(MethodDesc* pMD, LPCUTF8 *pLibName, LPCUTF8 *pEntryPointName);
    void PreInit(Module* pModule, MethodTable *pClass);
    void PreInit(MethodDesc* pMD);
    void SetError(WORD error) { if (!m_error) m_error = error; }
    void BestGuessNDirectDefaults(MethodDesc* pMD);

public:     
    DWORD GetStubFlags() 
    { 
        WRAPPER_NO_CONTRACT;
        return (GetThrowOnUnmappableChar() ? NDIRECTSTUB_FL_THROWONUNMAPPABLECHAR : 0) | 
               (GetBestFitMapping() ? NDIRECTSTUB_FL_BESTFIT : 0) | 
               (IsDelegateInterop() ? NDIRECTSTUB_FL_DELEGATE : 0);
    }
    Module* GetModule() { LIMITED_METHOD_CONTRACT; return m_pModule; }
    BOOL IsStatic() { LIMITED_METHOD_CONTRACT; return m_wFlags & PINVOKE_STATIC_SIGINFO_IS_STATIC; }
    void SetIsStatic (BOOL isStatic) 
    { 
        LIMITED_METHOD_CONTRACT; 
        if (isStatic) 
            m_wFlags |= PINVOKE_STATIC_SIGINFO_IS_STATIC; 
        else 
            m_wFlags &= ~PINVOKE_STATIC_SIGINFO_IS_STATIC; 
    }
    BOOL GetThrowOnUnmappableChar() { LIMITED_METHOD_CONTRACT; return m_wFlags & PINVOKE_STATIC_SIGINFO_THROW_ON_UNMAPPABLE_CHAR; }
    void SetThrowOnUnmappableChar (BOOL throwOnUnmappableChar) 
    { 
        LIMITED_METHOD_CONTRACT; 
        if (throwOnUnmappableChar) 
            m_wFlags |= PINVOKE_STATIC_SIGINFO_THROW_ON_UNMAPPABLE_CHAR; 
        else 
            m_wFlags &= ~PINVOKE_STATIC_SIGINFO_THROW_ON_UNMAPPABLE_CHAR; 
    }
    BOOL GetBestFitMapping() { LIMITED_METHOD_CONTRACT; return m_wFlags & PINVOKE_STATIC_SIGINFO_BEST_FIT; }
    void SetBestFitMapping (BOOL bestFit) 
    { 
        LIMITED_METHOD_CONTRACT; 
        if (bestFit) 
            m_wFlags |= PINVOKE_STATIC_SIGINFO_BEST_FIT; 
        else 
            m_wFlags &= ~PINVOKE_STATIC_SIGINFO_BEST_FIT; 
    }
    BOOL IsDelegateInterop() { LIMITED_METHOD_CONTRACT; return m_wFlags & PINVOKE_STATIC_SIGINFO_IS_DELEGATE_INTEROP; } 
    void SetIsDelegateInterop (BOOL delegateInterop) 
    { 
        LIMITED_METHOD_CONTRACT; 
        if (delegateInterop) 
            m_wFlags |= PINVOKE_STATIC_SIGINFO_IS_DELEGATE_INTEROP; 
        else 
            m_wFlags &= ~PINVOKE_STATIC_SIGINFO_IS_DELEGATE_INTEROP; 
    }
    CorPinvokeMap GetCallConv() { LIMITED_METHOD_CONTRACT; return m_callConv; }
    Signature GetSignature() { LIMITED_METHOD_CONTRACT; return m_sig; }
    
private:
    Module* m_pModule;
    Signature m_sig;
    CorPinvokeMap m_callConv;
    WORD m_error;

    enum 
    {
        PINVOKE_STATIC_SIGINFO_IS_STATIC = 0x0001,
        PINVOKE_STATIC_SIGINFO_THROW_ON_UNMAPPABLE_CHAR = 0x0002,
        PINVOKE_STATIC_SIGINFO_BEST_FIT = 0x0004,

        COR_NATIVE_LINK_TYPE_MASK = 0x0038,  // 0000 0000 0011 1000  <--- These 3 1's make the link type mask
        
        COR_NATIVE_LINK_FLAGS_MASK = 0x00C0, //0000 0000 1100 0000  <---- These 2 bits make up the link flags

        PINVOKE_STATIC_SIGINFO_IS_DELEGATE_INTEROP = 0x0100,
        
    };
    #define COR_NATIVE_LINK_TYPE_SHIFT 3 // Keep in synch with above mask
    #define COR_NATIVE_LINK_FLAGS_SHIFT 6  // Keep in synch with above mask
    WORD m_wFlags;

  public:
    CorNativeLinkType GetCharSet() { LIMITED_METHOD_CONTRACT; return (CorNativeLinkType)((m_wFlags & COR_NATIVE_LINK_TYPE_MASK) >> COR_NATIVE_LINK_TYPE_SHIFT); }
    CorNativeLinkFlags GetLinkFlags() { LIMITED_METHOD_CONTRACT; return (CorNativeLinkFlags)((m_wFlags & COR_NATIVE_LINK_FLAGS_MASK) >> COR_NATIVE_LINK_FLAGS_SHIFT); }
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
};


#include "stubgen.h"

class NDirectStubLinker : public ILStubLinker
{
public:
    NDirectStubLinker(
                DWORD dwStubFlags, 
                Module* pModule,
                const Signature &signature,
                SigTypeContext *pTypeContext,
                MethodDesc* pTargetMD,
                int  iLCIDParamIdx,
                BOOL fTargetHasThis,
                BOOL fStubHasThis);

    void    SetCallingConvention(CorPinvokeMap unmngCallConv, BOOL fIsVarArg);

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
    void    AdjustTargetStackDeltaForReverseInteropHRESULTSwapping();
    void    AdjustTargetStackDeltaForExtraParam();

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
    DWORD               m_dwWinRTFactoryObjectLocalNum;
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

// This attempts to guess whether a target is an API call that uses SetLastError to communicate errors.
BOOL HeuristicDoesThisLooksLikeAnApiCall(LPBYTE pTarget);
BOOL HeuristicDoesThisLookLikeAGetLastErrorCall(LPBYTE pTarget);
DWORD STDMETHODCALLTYPE FalseGetLastError();

class NDirectStubParameters
{
public:

    NDirectStubParameters(Signature          sig,
                          SigTypeContext*    pTypeContext,
                          Module*            pModule,
                          Module*            pLoaderModule,
                          CorNativeLinkType  nlType,
                          CorNativeLinkFlags nlFlags,
                          CorPinvokeMap      unmgdCallConv,
                          DWORD              dwStubFlags,  // NDirectStubFlags
                          int                nParamTokens,
                          mdParamDef*        pParamTokenArray,
                          int                iLCIDArg
                          ) :
        m_sig(sig),
        m_pTypeContext(pTypeContext),
        m_pModule(pModule),
        m_pLoaderModule(pLoaderModule),
        m_pParamTokenArray(pParamTokenArray),
        m_unmgdCallConv(unmgdCallConv),
        m_nlType(nlType),
        m_nlFlags(nlFlags),
        m_dwStubFlags(dwStubFlags),
        m_iLCIDArg(iLCIDArg),
        m_nParamTokens(nParamTokens)
    {
        LIMITED_METHOD_CONTRACT;
    }

    Signature           m_sig;
    SigTypeContext*     m_pTypeContext;
    Module*             m_pModule;
    Module*             m_pLoaderModule;
    mdParamDef*         m_pParamTokenArray;
    CorPinvokeMap       m_unmgdCallConv;
    CorNativeLinkType   m_nlType;
    CorNativeLinkFlags  m_nlFlags;
    DWORD               m_dwStubFlags;
    int                 m_iLCIDArg;
    int                 m_nParamTokens;
};

PCODE GetILStubForCalli(VASigCookie *pVASigCookie, MethodDesc *pMD);

MethodDesc *GetStubMethodDescFromInteropMethodDesc(MethodDesc* pMD, DWORD dwStubFlags);
PCODE JitILStub(MethodDesc* pStubMD);
MethodDesc *RestoreNGENedStub(MethodDesc* pStubMD);
PCODE GetStubForInteropMethod(MethodDesc* pMD, DWORD dwStubFlags = 0, MethodDesc **ppStubMD = NULL);

#ifdef FEATURE_COMINTEROP
// Resolve and return the predefined IL stub method
HRESULT FindPredefinedILStubMethod(MethodDesc *pTargetMD, DWORD dwStubFlags, MethodDesc **ppRetStubMD);
#endif // FEATURE_COMINTEROP

// 
// Limit length of string field in IL stub ETW events so that the whole
// IL stub ETW events won't exceed 64KB
//
#define ETW_IL_STUB_EVENT_STRING_FIELD_MAXSIZE      (1024)
#define ETW_IL_STUB_EVENT_CODE_STRING_FIELD_MAXSIZE (1024*32)

class SString;

//
// Truncates a SString by first converting it to unicode and truncate it 
// if it is larger than size. "..." will be appened if it is truncated.
//
void TruncateUnicodeString(SString &string, COUNT_T bufSize);

//=======================================================================
// ILStubCreatorHelper
// The class is used as a helper class in CreateInteropILStub. It mainly
// puts two methods NDirect::GetStubMethodDesc and NDirect::RemoveILStubCacheEntry
// into a holder. See CreateInteropILStub for more information
//=======================================================================
class ILStubCreatorHelper
{
public:
    ILStubCreatorHelper(MethodDesc *pTargetMD,
                        NDirectStubParameters* pParams
                        ) :
        m_pTargetMD(pTargetMD),
        m_pParams(pParams),
        m_pStubMD(NULL),
        m_bILStubCreator(false)
    {
        STANDARD_VM_CONTRACT;
        m_pHashParams = NDirect::CreateHashBlob(m_pParams);
    }

    ~ILStubCreatorHelper()
    {
        CONTRACTL
        {
            THROWS;
            GC_TRIGGERS;
            MODE_ANY;
        }
        CONTRACTL_END;

        RemoveILStubCacheEntry();
    }

    inline void GetStubMethodDesc()
    {
        WRAPPER_NO_CONTRACT;

        m_pStubMD = NDirect::GetStubMethodDesc(m_pTargetMD, m_pParams, m_pHashParams, &m_amTracker, m_bILStubCreator, m_pStubMD);
    }

    inline void RemoveILStubCacheEntry()
    {
        WRAPPER_NO_CONTRACT;
        
        if (m_bILStubCreator)
        {
            NDirect::RemoveILStubCacheEntry(m_pParams, m_pHashParams);
            m_bILStubCreator = false;
        }
    }

    inline MethodDesc* GetStubMD()
    {
        LIMITED_METHOD_CONTRACT;
        return m_pStubMD;
    }

    inline void SuppressRelease()
    {
        WRAPPER_NO_CONTRACT;
        m_bILStubCreator = false;
        m_amTracker.SuppressRelease();
    }

    DEBUG_NOINLINE static void HolderEnter(ILStubCreatorHelper *pThis)
    {
        WRAPPER_NO_CONTRACT;
        ANNOTATION_SPECIAL_HOLDER_CALLER_NEEDS_DYNAMIC_CONTRACT;
        pThis->GetStubMethodDesc();
    }

    DEBUG_NOINLINE static void HolderLeave(ILStubCreatorHelper *pThis)
    {
        WRAPPER_NO_CONTRACT;
        ANNOTATION_SPECIAL_HOLDER_CALLER_NEEDS_DYNAMIC_CONTRACT;
        pThis->RemoveILStubCacheEntry();
    }

private:
    MethodDesc*                      m_pTargetMD;
    NDirectStubParameters*           m_pParams;
    NewArrayHolder<ILStubHashBlob>   m_pHashParams;
    AllocMemTracker*                 m_pAmTracker;
    MethodDesc*                      m_pStubMD;
    AllocMemTracker                  m_amTracker;
    bool                             m_bILStubCreator;     // Only the creator can remove the ILStub from the Cache
};  //ILStubCreatorHelper

typedef Wrapper<ILStubCreatorHelper*, ILStubCreatorHelper::HolderEnter, ILStubCreatorHelper::HolderLeave> ILStubCreatorHelperHolder;

#endif // __dllimport_h__
