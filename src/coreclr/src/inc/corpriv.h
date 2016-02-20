// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ===========================================================================
// File: CORPRIV.H
//
// ===========================================================================

#ifndef _CORPRIV_H_
#define _CORPRIV_H_
#if _MSC_VER >= 1000
#pragma once
#endif // _MSC_VER >= 1000

// %%Includes: ---------------------------------------------------------------
// avoid taking DLL import hit on intra-DLL calls
#define NODLLIMPORT
#include <daccess.h>
#include "cor.h"
#include "corimage.h"
#include "metadata.h"
#include <sstring.h>
#include "peinformation.h"
//

#ifndef FEATURE_CORECLR
interface IILFingerprint;
interface IILFingerprintFactory;
#endif
interface IAssemblyName;

// PE images loaded through the runtime.
typedef struct _dummyCOR { BYTE b; } *HCORMODULE;

class UTSemReadWrite;

// Helper function to get a pointer to the Dispenser interface.
STDAPI MetaDataGetDispenser(            // Return HRESULT
    REFCLSID    rclsid,                 // The class to desired.
    REFIID      riid,                   // Interface wanted on class factory.
    LPVOID FAR  *ppv);                  // Return interface pointer here.

// Helper function to check whether policy allows accessing the file
STDAPI RuntimeCheckLocationAccess(LPCWSTR wszLocation);
STDAPI RuntimeIsNativeImageOptedOut(IAssemblyName* pAssemblyDef);

#ifdef FEATURE_VERSIONING
LocaleID RuntimeGetFileSystemLocale();
#endif // FEATURE_VERSIONING

BOOL RuntimeFileNotFound(HRESULT hr);

#ifdef FEATURE_LEGACYNETCF
BOOL RuntimeIsLegacyNetCF(DWORD adid);
#endif

// Helper function to get an Internal interface with an in-memory metadata section
STDAPI  GetMetaDataInternalInterface(
    LPVOID      pData,                  // [IN] in memory metadata section
    ULONG       cbData,                 // [IN] size of the metadata section
    DWORD       flags,                  // [IN] CorOpenFlags
    REFIID      riid,                   // [IN] desired interface
    void        **ppv);                 // [OUT] returned interface

// Helper function to get an internal scopeless interface given a scope.
STDAPI  GetMetaDataInternalInterfaceFromPublic(
    IUnknown    *pv,                    // [IN] Given interface
    REFIID      riid,                   // [IN] desired interface
    void        **ppv);                 // [OUT] returned interface

// Helper function to get an internal scopeless interface given a scope.
STDAPI  GetMetaDataPublicInterfaceFromInternal(
    void        *pv,                    // [IN] Given interface
    REFIID      riid,                   // [IN] desired interface
    void        **ppv);                 // [OUT] returned interface

// Converts an internal MD import API into the read/write version of this API.
// This could support edit and continue, or modification of the metadata at
// runtime (say for profiling).
STDAPI ConvertMDInternalImport(         // S_OK or error.
    IMDInternalImport *pIMD,            // [IN] The metadata to be updated.
    IMDInternalImport **ppIMD);         // [OUT] Put RW interface here.

STDAPI GetAssemblyMDInternalImport(     // Return code.
    LPCWSTR     szFileName,             // [IN] The scope to open.
    REFIID      riid,                   // [IN] The interface desired.
    IUnknown    **ppIUnk);              // [OUT] Return interface on success.

HRESULT GetAssemblyMDInternalImportFromImage(
    HCORMODULE hImage,                         //[IN] pointer to module handle to get the metadata from.
    REFIID riid,                               //[IN] The interface desired.
    IUnknown **ppIUnk);                        //[OUT] Return Interface on success.

STDAPI GetAssemblyMDInternalImportByStream( // Return code.
    IStream     *pIStream,              // [IN] The IStream for the file
    UINT64      AssemblyId,             // [IN] Unique Id for the assembly
    REFIID      riid,                   // [IN] The interface desired.
    IUnknown    **ppIUnk);              // [OUT] Return interface on success.


enum MDInternalImportFlags
{
    MDInternalImport_Default            = 0,
    MDInternalImport_NoCache            = 1, // Do not share/cached the results of opening the image
#ifdef FEATURE_PREJIT
    MDInternalImport_TrustedNativeImage = 2, // The image is a native image, and so its format can be trusted
    MDInternalImport_ILMetaData         = 4, // Open the IL metadata, even if this is a native image
    MDInternalImport_TrustedNativeImage_and_IL = MDInternalImport_TrustedNativeImage | MDInternalImport_ILMetaData,
    MDInternalImport_NativeImageInstall = 0x100, // The image is a native image that is being installed into NIC
#endif
    MDInternalImport_CheckLongPath   =8,	// also check long version of the path		
    MDInternalImport_CheckShortPath   =0x10,    // also check long version of the path		
    MDInternalImport_OnlyLookInCache    =0x20, // Only look in the cache. (If the cache does not have the image already loaded, return NULL)
};  // enum MDInternalImportFlags



STDAPI GetAssemblyMDInternalImportEx(     // Return code.
    LPCWSTR     szFileName,             // [IN] The scope to open.
    REFIID      riid,                   // [IN] The interface desired.
    MDInternalImportFlags flags,        // [in] Flags to control opening the assembly
    IUnknown    **ppIUnk,               // [OUT] Return interface on success.
    HANDLE      hFile = INVALID_HANDLE_VALUE);

STDAPI GetAssemblyMDInternalImportByStreamEx( // Return code.
    IStream     *pIStream,              // [IN] The IStream for the file
    UINT64      AssemblyId,             // [IN] Unique Id for the assembly
    REFIID      riid,                   // [IN] The interface desired.
    MDInternalImportFlags flags,        // [in] Flags to control opening the assembly
    IUnknown    **ppIUnk);              // [OUT] Return interface on success.


// Returns part of the "Zap string" which describes the properties of a native image

__success(SUCCEEDED(return))
STDAPI GetNativeImageDescription(
    __in_z LPCWSTR wzCustomString,                     // [IN] Custom string of the native image
    DWORD dwConfigMask,                         // [IN] Config mask of the native image
    __out_ecount_part_opt(*pdwLength,*pdwLength) LPWSTR pwzZapInfo,// [OUT] The description string. Can be NULL to find the size of buffer to allocate
    LPDWORD pdwLength);                         // [IN/OUT] Length of the pwzZapInfo buffer on IN.
                                                //          Number of WCHARs (including termintating NULL) on OUT


class CQuickBytes;


// predefined constant for parent token for global functions
#define     COR_GLOBAL_PARENT_TOKEN     TokenFromRid(1, mdtTypeDef)



//////////////////////////////////////////////////////////////////////////
//
//////////////////////////////////////////////////////////////////////////

// %%Interfaces: -------------------------------------------------------------

// interface IMetaDataHelper

// {AD93D71D-E1F2-11d1-9409-0000F8083460}
EXTERN_GUID(IID_IMetaDataHelper, 0xad93d71d, 0xe1f2, 0x11d1, 0x94, 0x9, 0x0, 0x0, 0xf8, 0x8, 0x34, 0x60);

#undef  INTERFACE
#define INTERFACE IMetaDataHelper
DECLARE_INTERFACE_(IMetaDataHelper, IUnknown)
{
    // helper functions
    // This function is exposing the ability to translate signature from a given
    // source scope to a given target scope.
    // 
    STDMETHOD(TranslateSigWithScope)(
        IMetaDataAssemblyImport *pAssemImport, // [IN] importing assembly interface
        const void  *pbHashValue,           // [IN] Hash Blob for Assembly.
        ULONG       cbHashValue,            // [IN] Count of bytes.
        IMetaDataImport *import,            // [IN] importing interface
        PCCOR_SIGNATURE pbSigBlob,          // [IN] signature in the importing scope
        ULONG       cbSigBlob,              // [IN] count of bytes of signature
        IMetaDataAssemblyEmit *pAssemEmit,  // [IN] emit assembly interface
        IMetaDataEmit *emit,                // [IN] emit interface
        PCOR_SIGNATURE pvTranslatedSig,     // [OUT] buffer to hold translated signature
        ULONG       cbTranslatedSigMax,
        ULONG       *pcbTranslatedSig) PURE;// [OUT] count of bytes in the translated signature

    STDMETHOD(GetMetadata)(
        ULONG       ulSelect,               // [IN] Selector.
        void        **ppData) PURE;         // [OUT] Put pointer to data here.

    STDMETHOD_(IUnknown *, GetCachedInternalInterface)(BOOL fWithLock) PURE;    // S_OK or error
    STDMETHOD(SetCachedInternalInterface)(IUnknown * pUnk) PURE;    // S_OK or error
    STDMETHOD_(UTSemReadWrite*, GetReaderWriterLock)() PURE;   // return the reader writer lock
    STDMETHOD(SetReaderWriterLock)(UTSemReadWrite * pSem) PURE;
};  // IMetaDataHelper


EXTERN_GUID(IID_IMetaDataEmitHelper, 0x5c240ae4, 0x1e09, 0x11d3, 0x94, 0x24, 0x0, 0x0, 0xf8, 0x8, 0x34, 0x60);

#undef  INTERFACE
#define INTERFACE IMetaDataEmitHelper
DECLARE_INTERFACE_(IMetaDataEmitHelper, IUnknown)
{
    // emit helper functions
    STDMETHOD(DefineMethodSemanticsHelper)(
        mdToken     tkAssociation,          // [IN] property or event token
        DWORD       dwFlags,                // [IN] semantics
        mdMethodDef md) PURE;               // [IN] method to associated with

    STDMETHOD(SetFieldLayoutHelper)(                // Return hresult.
        mdFieldDef  fd,                     // [IN] field to associate the layout info
        ULONG       ulOffset) PURE;         // [IN] the offset for the field

    STDMETHOD(DefineEventHelper) (
        mdTypeDef   td,                     // [IN] the class/interface on which the event is being defined
        LPCWSTR     szEvent,                // [IN] Name of the event
        DWORD       dwEventFlags,           // [IN] CorEventAttr
        mdToken     tkEventType,            // [IN] a reference (mdTypeRef or mdTypeRef) to the Event class
        mdEvent     *pmdEvent) PURE;        // [OUT] output event token

    STDMETHOD(AddDeclarativeSecurityHelper) (
        mdToken     tk,                     // [IN] Parent token (typedef/methoddef)
        DWORD       dwAction,               // [IN] Security action (CorDeclSecurity)
        void const  *pValue,                // [IN] Permission set blob
        DWORD       cbValue,                // [IN] Byte count of permission set blob
        mdPermission*pmdPermission) PURE;   // [OUT] Output permission token

    STDMETHOD(SetResolutionScopeHelper)(    // Return hresult.
        mdTypeRef   tr,                     // [IN] TypeRef record to update
        mdToken     rs) PURE;               // [IN] new ResolutionScope

    STDMETHOD(SetManifestResourceOffsetHelper)(  // Return hresult.
        mdManifestResource mr,              // [IN] The manifest token
        ULONG       ulOffset) PURE;         // [IN] new offset

    STDMETHOD(SetTypeParent)(               // Return hresult.
        mdTypeDef   td,                     // [IN] Type definition
        mdToken     tkExtends) PURE;        // [IN] parent type

    STDMETHOD(AddInterfaceImpl)(            // Return hresult.
        mdTypeDef   td,                     // [IN] Type definition
        mdToken     tkInterface) PURE;      // [IN] interface type

};  // IMetaDataEmitHelper

//////////////////////////////////////////////////////////////////////////////
// enum CorElementTypeZapSig defines some additional internal ELEMENT_TYPE's
// values that are only used by ZapSig signatures.
//////////////////////////////////////////////////////////////////////////////
typedef enum CorElementTypeZapSig
{
    // ZapSig encoding for ELEMENT_TYPE_VAR and ELEMENT_TYPE_MVAR. It is always followed
    // by the RID of a GenericParam token, encoded as a compressed integer.
    ELEMENT_TYPE_VAR_ZAPSIG = 0x3b,

    // ZapSig encoding for an array MethodTable to allow it to remain such after decoding
    // (rather than being transformed into the TypeHandle representing that array)
    //
    // The element is always followed by ELEMENT_TYPE_SZARRAY or ELEMENT_TYPE_ARRAY
    ELEMENT_TYPE_NATIVE_ARRAY_TEMPLATE_ZAPSIG = 0x3c,

    // ZapSig encoding for native value types in IL stubs. IL stub signatures may contain
    // ELEMENT_TYPE_INTERNAL followed by ParamTypeDesc with ELEMENT_TYPE_VALUETYPE element
    // type. It acts like a modifier to the underlying structure making it look like its
    // unmanaged view (size determined by unmanaged layout, blittable, no GC pointers).
    // 
    // ELEMENT_TYPE_NATIVE_VALUETYPE_ZAPSIG is used when encoding such types to NGEN images.
    // The signature looks like this: ET_NATIVE_VALUETYPE_ZAPSIG ET_VALUETYPE <token>.
    // See code:ZapSig.GetSignatureForTypeHandle and code:SigPointer.GetTypeHandleThrowing
    // where the encoding/decoding takes place.
    ELEMENT_TYPE_NATIVE_VALUETYPE_ZAPSIG = 0x3d,
    
    ELEMENT_TYPE_CANON_ZAPSIG            = 0x3e,     // zapsig encoding for [mscorlib]System.__Canon
    ELEMENT_TYPE_MODULE_ZAPSIG           = 0x3f,     // zapsig encoding for external module id#

} CorElementTypeZapSig;

typedef enum CorCallingConventionInternal
{
    // IL stub signatures containing types that need to be restored have the highest
    // bit of the calling convention set.
    IMAGE_CEE_CS_CALLCONV_NEEDSRESTORE   = 0x80,

} CorCallingConventionInternal;

//////////////////////////////////////////////////////////////////////////
// Obsoleted ELEMENT_TYPE values which are not supported anymore.
// They are not part of CLI ECMA spec, they were only experimental before v1.0 RTM.
// They are needed for indexing arrays initialized using file:corTypeInfo.h
//    0x17 ... VALUEARRAY <type> <bound>
//    0x1a ... CPU native floating-point type
//////////////////////////////////////////////////////////////////////////
#define ELEMENT_TYPE_VALUEARRAY_UNSUPPORTED ((CorElementType) 0x17)
#define ELEMENT_TYPE_R_UNSUPPORTED          ((CorElementType) 0x1a)

// Use this guid in the SetOption if Reflection.Emit wants to control size of the initially allocated 
// MetaData. See values: code:CorMetaDataInitialSize.
// 
// {2675b6bf-f504-4cb4-a4d5-084eea770ddc}
EXTERN_GUID(MetaDataInitialSize, 0x2675b6bf, 0xf504, 0x4cb4, 0xa4, 0xd5, 0x08, 0x4e, 0xea, 0x77, 0x0d, 0xdc);

// Allowed values for code:MetaDataInitialSize option.
typedef enum CorMetaDataInitialSize
{
    MDInitialSizeDefault = 0, 
    MDInitialSizeMinimal = 1
} CorMetaDataInitialSize;

// Internal extension of open flags code:CorOpenFlags
typedef enum CorOpenFlagsInternal
{
#ifdef FEATURE_METADATA_LOAD_TRUSTED_IMAGES
    // Flag code:ofTrustedImage is used by mscordbi.dll, therefore defined in file:CorPriv.h
    ofTrustedImage = ofReserved3    // We trust this PE file (we are willing to do a LoadLibrary on it).
                                    // It is optional and only an (VM) optimization - typically for NGEN images 
                                    // opened by debugger.
#endif
} CorOpenFlagsInternal;

#ifdef FEATURE_METADATA_LOAD_TRUSTED_IMAGES
#define IsOfTrustedImage(x)     ((x) & ofTrustedImage)
#endif

// %%Classes: ----------------------------------------------------------------
#ifndef offsetof
#define offsetof(s,f)   ((ULONG)(&((s*)0)->f))
#endif
#ifndef lengthof
#define lengthof(rg)    (sizeof(rg)/sizeof(rg[0]))
#endif

#define COR_MODULE_CLASS    "<Module>"
#define COR_WMODULE_CLASS   W("<Module>")

STDAPI RuntimeOpenImage(LPCWSTR pszFileName, HCORMODULE* hHandle);
STDAPI RuntimeOpenImageInternal(LPCWSTR pszFileName, HCORMODULE* hHandle,
                                DWORD *pdwLength, MDInternalImportFlags flags, HANDLE hFile = INVALID_HANDLE_VALUE);
STDAPI RuntimeOpenImageByStream(IStream* pIStream, UINT64 AssemblyId, DWORD dwModuleId,
                                HCORMODULE* hHandle, DWORD *pdwLength, MDInternalImportFlags flags);

#ifndef FEATURE_CORECLR
// NOTE: Performance critical codepaths should cache the result of this function.
STDAPI RuntimeGetILFingerprintForPath(LPCWSTR path, IILFingerprint **ppFingerprint);
STDAPI RuntimeCreateCachingILFingerprintFactory(IILFingerprintFactory **ppILFingerprintFactory);
#endif //!FEATURE_CORECLR
void   RuntimeAddRefHandle(HCORMODULE hHandle);
STDAPI RuntimeReleaseHandle(HCORMODULE hHandle);
STDAPI RuntimeGetImageBase(HCORMODULE hHandle, LPVOID* base, BOOL bMapped, COUNT_T* dwSize);
STDAPI RuntimeGetImageKind(HCORMODULE hHandle, DWORD* pdwKind, DWORD* pdwMachine);
STDAPI RuntimeOSHandle(HCORMODULE hHandle, HMODULE* hModule);
STDAPI RuntimeGetAssemblyStrongNameHashForModule(HCORMODULE       hModule,
                                                 IMetaDataImport *pMDimport,
                                                 BYTE            *pbSNHash,
                                                 DWORD           *pcbSNHash);
STDAPI RuntimeGetMDInternalImport(HCORMODULE hHandle, 
                                  MDInternalImportFlags flags,
                                  IMDInternalImport** ppMDImport);

FORCEINLINE 
void ReleaseHCorModule(HCORMODULE hModule)
{
    HRESULT hr = RuntimeReleaseHandle(hModule);
    _ASSERTE(SUCCEEDED(hr));
}

typedef Wrapper<HCORMODULE, DoNothing<HCORMODULE>, ReleaseHCorModule, (UINT_PTR) NULL> HCORMODULEHolder;


// ===========================================================================
// ISNAssemblySignature (similar to IAssemblySignature in V1)
//
// This is a private interface that allows querying of the strong name
// signature.
// This can be used for (strong-named) assemblies added to the GAC as
// a unique identifier.
//

// {848845BC-0C4A-42e3-8915-DC850112443D}
EXTERN_GUID(IID_ISNAssemblySignature, 0x848845BC, 0x0C4A, 0x42e3, 0x89, 0x15, 0xDC, 0x85, 0x01, 0x12, 0x44, 0x3D);
        
#undef  INTERFACE
#define INTERFACE ISNAssemblySignature
DECLARE_INTERFACE_(ISNAssemblySignature, IUnknown)
{
    // Returns the strong-name signature if the assembly is strong-name-signed
    // Returns the MVID if the assembly is delay-signed.
    // Fails if the assembly is not signed at all.
    STDMETHOD(GetSNAssemblySignature) (
        BYTE  *pbSig,    // [IN, OUT] Buffer to write signature     
        DWORD *pcbSig    // [IN, OUT] Size of buffer, bytes written 
    ) PURE;
};

//-------------------------------------
//--- ICeeGenInternal
//-------------------------------------
// {9fd3c7af-dc4e-4b9b-be22-9cf8cc577489}
EXTERN_GUID(IID_ICeeGenInternal, 0x9fd3c7af, 0xdc4e, 0x4b9b, 0xbe, 0x22, 0x9c, 0xf8, 0xcc, 0x57, 0x74, 0x89);

#undef  INTERFACE
#define INTERFACE ICeeGenInternal
DECLARE_INTERFACE_(ICeeGenInternal, IUnknown)
{
    STDMETHOD (SetInitialGrowth) (DWORD growth) PURE;
};

// ===========================================================================
#ifdef FEATURE_PREJIT
// ===========================================================================

#define CLR_OPTIMIZATION_SERVICE_DLL_NAME W("mscorsvc.dll")
#define CLR_OPT_SVC_ENTRY_POINT "CorGetSvc"

// Use the default JIT compiler
#define DEFAULT_NGEN_COMPILER_DLL_NAME W("clrjit.dll")


struct CORCOMPILE_ASSEMBLY_SIGNATURE;

//
// IGetIMDInternalImport
//
// Private interface exposed by
//    AssemblyMDInternalImport - gives us access to the internally stored IMDInternalImport*.
//
//    RegMeta, WinMDImport - supports the internal GetMetaDataInternalInterfaceFromPublic() "api".
//
// {92B2FEF9-F7F5-420d-AD42-AECEEE10A1EF}
EXTERN_GUID(IID_IGetIMDInternalImport,  0x92b2fef9, 0xf7f5, 0x420d, 0xad, 0x42, 0xae, 0xce, 0xee, 0x10, 0xa1, 0xef);
#undef  INTERFACE
#define INTERFACE IGetIMDInternalImport
DECLARE_INTERFACE_(IGetIMDInternalImport, IUnknown)
{
    STDMETHOD(GetIMDInternalImport) (
        IMDInternalImport ** ppIMDInternalImport   // [OUT] Buffer to receive IMDInternalImport*
    ) PURE;
};



#ifndef DACCESS_COMPILE

/* --------------------------------------------------------------------------- *
 * NGen logger
 * --------------------------------------------------------------------------- */
 #include "mscorsvc.h"
 
struct ICorSvcLogger;
class SvcLogger
{
public:

    SvcLogger();
    ~SvcLogger();
    void ReleaseLogger();
    void SetSvcLogger(ICorSvcLogger *pCorSvcLoggerArg);
    BOOL HasSvcLogger();
    ICorSvcLogger* GetSvcLogger();
    void Printf(const CHAR *format, ...);
    void SvcPrintf(const CHAR *format, ...);
    void Printf(const WCHAR *format, ...);
    void Printf(CorSvcLogLevel logLevel, const WCHAR *format, ...);
    void SvcPrintf(const WCHAR *format, ...);
    void Log(const WCHAR *message, CorSvcLogLevel logLevel = LogLevel_Warning);
    //Need to add this to allocate StackSString, as we don't want static class 

private:

    void LogHelper(SString s, CorSvcLogLevel logLevel = LogLevel_Success);
    //instantiations that need VM services like contracts in dllmain.
    void CheckInit();

    StackSString* pss;
    ICorSvcLogger *pCorSvcLogger;
};  // class SvcLogger

SvcLogger *GetSvcLogger();
BOOL       HasSvcLogger();
#endif // #ifndef DACCESS_COMPILE

// ===========================================================================
#endif // #ifdef FEATURE_PREJIT 
// ===========================================================================

struct CORCOMPILE_ASSEMBLY_SIGNATURE;
struct CORCOMPILE_VERSION_INFO;
struct CORCOMPILE_DEPENDENCY;
typedef GUID CORCOMPILE_NGEN_SIGNATURE;

#ifdef FEATURE_FUSION
//**********************************************************************
// Gets the dependancies of a native image. If these change, then
// the native image cannot be used.
//
// IMetaDataImport::GetAssemblyRefProps() can be used to obtain information about
// the mdAssemblyRefs.
//*****************************************************************************

// {814C9E35-3F3F-4975-977A-371F0A878AC7}
EXTERN_GUID(IID_INativeImageDependency, 0x814c9e35, 0x3f3f, 0x4975, 0x97, 0x7a, 0x37, 0x1f, 0xa, 0x87, 0x8a, 0xc7);

DECLARE_INTERFACE_(INativeImageDependency, IUnknown)
{
    // Get the referenced assembly
    STDMETHOD (GetILAssemblyRef) (
        mdAssemblyRef * pAssemblyRef        // [OUT]
        ) PURE;

    // Get the post-policy assembly actually used
    STDMETHOD (GetILAssemblyDef) (
        mdAssemblyRef * ppAssemblyDef,          // [OUT]
        CORCOMPILE_ASSEMBLY_SIGNATURE * pSign   // [OUT]
        ) PURE;

    // Get the native image corresponding to GetILAssemblyDef() IF
    // there is a hard-bound (directly-referenced) native dependancy
    //
    // We do not need the configStrig because configStrings have to
    // be an exact part. Any partial matches are factored out into GetConfigMask()
    STDMETHOD (GetNativeAssemblyDef) (
        CORCOMPILE_NGEN_SIGNATURE * pNativeSign // [OUT] INVALID_NGEN_SIGNATURE if there is no hard-bound dependancy
        ) PURE;

    // Get PEKIND of the referenced assembly
    STDMETHOD (GetPEKind) (
        PEKIND * CorPEKind                      // [OUT]
        ) PURE;

};  // INativeImageDependency

//*****************************************************************************
//
// Fusion uses IFusionNativeImageInfo to obtain (and cache) informaton
// about a native image being installed into the native image cache.
// This allows Fusion to bind directly to native images
// without requiring (expensively) binding to the IL assembly first.
//
// IMetaDataAssemblyImport can be queried for this interface
//
//*****************************************************************************
// {0EA273D0-B4DA-4008-A60D-8D6EFFDD6E91}
EXTERN_GUID(IID_INativeImageInstallInfo, 0xea273d0, 0xb4da, 0x4008, 0xa6, 0xd, 0x8d, 0x6e, 0xff, 0xdd, 0x6e, 0x91);

DECLARE_INTERFACE_(INativeImageInstallInfo, IUnknown)
{
    // Signature of the ngen image
    // This matches the argument type of INativeImageDependency::GetNativeAssemblyDef
    
    STDMETHOD (GetSignature) (
        CORCOMPILE_NGEN_SIGNATURE * pNgenSign // [OUT]
        ) PURE;


    // CLR timestamp, CPU, compile options, OS type and other attributes of the
    // NI image. This can be used to verify that the NI image was built
    // with the running CLR.

    STDMETHOD (GetVersionInfo) (
        CORCOMPILE_VERSION_INFO * pVersionInfo // [OUT]
        ) PURE;
            

    // Signature of the source IL assembly. This can be used to
    // verify that the IL image matches a candidate ngen image.
    // This matches the argument type of IAssemblyRuntimeSignature::CheckSignature
    //

    STDMETHOD (GetILSignature) (
        CORCOMPILE_ASSEMBLY_SIGNATURE * pILSign // [OUT]
        ) PURE;

    // A partial match is allowed for the current NativeImage to be valid
    
    STDMETHOD (GetConfigMask) (
        DWORD * pConfigMask // [OUT]
        ) PURE;

    //
    // Dependancy assemblies. The native image is only valid
    // if the dependancies have not changed.
    //

    STDMETHOD (EnumDependencies) (
        HCORENUM * phEnum,                  // [IN/OUT] - Pointer to the enum
        INativeImageDependency *rDeps[],    // [OUT]
        ULONG cMax,                         // [IN] Max dependancies to enumerate in this iteration
        DWORD * pdwCount                    // [OUT] - Number of dependancies actually enumerated
        ) PURE;


    // Retrieve a specific dependency by the ngen signature.

    STDMETHOD (GetDependency) (
        const CORCOMPILE_NGEN_SIGNATURE *pcngenSign,  // [IN] ngenSig of dependency you want
        CORCOMPILE_DEPENDENCY           *pDep         // [OUT] matching dependency
        ) PURE;
    
};  // INativeImageInstallInfo

//*****************************************************************************
//
// Runtime callback made by Fusion into the CLR to determine if the NativeAssembly
// can be used. The pUnkBindSink argument of CAssemblyName::BindToObject() can
// be queried for this interface
//
//*****************************************************************************
// {065AA013-9BDC-447c-922F-FEE929908447}
EXTERN_GUID(IID_INativeImageEvaluate, 0x65aa013, 0x9bdc, 0x447c, 0x92, 0x2f, 0xfe, 0xe9, 0x29, 0x90, 0x84, 0x47);

#ifdef _PREFAST_
#pragma warning(push)
#pragma warning(disable:28718) 
#endif //_PREFAST_

interface IAssembly;

#ifdef _PREFAST_
#pragma warning(pop)
#endif //_PREFAST_


DECLARE_INTERFACE_(INativeImageEvaluate, IUnknown)
{
    // This will be called before the assemblies are actually loaded.
    //
    // Returns S_FALSE if the native-image cannot be used.
    
    STDMETHOD (Evaluate) (
        IAssembly *pILAssembly,             // [IN] IL assembly in question
        IAssembly *pNativeAssembly,         // [IN] NGen image we are trying to use for pILAssembly 
        BYTE * pbCachedData,                // [IN] Data cached when the native-image was generated
        DWORD dwDataSize                    // [IN] Size of the pbCachedData buffer
        ) PURE;
};  // INativeImageEvaluate

#endif // FEATURE_FUSION

//**********************************************************************
// Internal versions of shim functions for use by the CLR.

STDAPI LoadLibraryShimInternal(
    LPCWSTR szDllName,
    LPCWSTR szVersion,
    LPVOID pvReserved,
    HMODULE *phModDll);

STDAPI GetCORSystemDirectoryInternaL(
    SString& pBuffer
      );

//LONGPATH:TODO: Remove this once Desktop usage has been removed 
STDAPI GetCORSystemDirectoryInternal(
    __out_ecount_part_opt(cchBuffer, *pdwLength) LPWSTR pBuffer,
    DWORD  cchBuffer,
    __out_opt DWORD* pdwLength
    );

STDAPI GetCORVersionInternal(
    __out_ecount_z_opt(cchBuffer) LPWSTR pBuffer, 
                                  DWORD  cchBuffer,
    __out                         DWORD *pdwLength);

STDAPI GetRequestedRuntimeInfoInternal(LPCWSTR pExe, 
                               LPCWSTR pwszVersion,
                               LPCWSTR pConfigurationFile, 
                               DWORD startupFlags,
                               DWORD runtimeInfoFlags, 
                               __out_ecount_opt(dwDirectory) LPWSTR pDirectory,
                               DWORD dwDirectory, 
                               __out_opt DWORD *pdwDirectoryLength, 
                               __out_ecount_opt(cchBuffer) LPWSTR pVersion, 
                               DWORD cchBuffer, 
                               __out_opt DWORD* pdwLength);


#ifdef FEATURE_PREJIT

//**********************************************************************
// Access to native image validation logic in the runtime.
//
// An interface only a mother could live as this logic should really be encapsulated in
// the native binder. But for historical reasons, it lives in the VM directory
// and is shared by the desktop and coreclr's which have separate native binders.
// Hence, this interface inherits a lot of "baggage."

#ifdef FEATURE_FUSION
interface IFusionBindLog;
interface IAssemblyName;
#endif // FEATURE_FUSION


// A small shim around PEAssemblies/IBindResult that allow us to write Fusion/CLR-agnostic code
// for logging native bind failures to the Fusion log/CLR log.
//
// These objects are stack-based and non-thread-safe. They are created for the duration of a single RuntimeVerify call.
// The methods are expected to compute their data lazily as they are only used in bind failures or in checked builds.
//
// This class also exposes the IFusionBindLog pointer. This isn't really the appropriate place to expose that but 
// it serves to avoid compiling references to IFUsionBindLog into code that doesn't define FEATURE_FUSION.
class LoggableAssembly
{
  public:
    virtual SString         DisplayString() = 0;        // Returns an unspecified representation suitable for injecting into log messages.   
#ifdef FEATURE_FUSION
    virtual IAssemblyName*  FusionAssemblyName() = 0;   // Can return NULL. Caller must NOT release result.
    virtual IFusionBindLog* FusionBindLog() = 0;        // Can return NULL. Caller must NOT release result.
#endif // FEATURE_FUSION
};


// Validates that an NI matches the running CLR, OS, CPU, etc.
BOOL RuntimeVerifyNativeImageVersion(const CORCOMPILE_VERSION_INFO *pVerInfo, LoggableAssembly *pLogAsm);

// Validates that an NI matches the required flavor (debug, instrumented, etc.)
BOOL RuntimeVerifyNativeImageFlavor(const CORCOMPILE_VERSION_INFO *pVerInfo, LoggableAssembly *pLogAsm);

// Validates that a hard-dep matches the a parent NI's compile-time hard-dep.
BOOL RuntimeVerifyNativeImageDependency(const CORCOMPILE_NGEN_SIGNATURE &ngenSigExpected,
                                        const CORCOMPILE_VERSION_INFO *pActual,
                                        LoggableAssembly              *pLogAsm);

BOOL RuntimeVerifyNativeImageDependency(const CORCOMPILE_DEPENDENCY   *pExpected,
                                        const CORCOMPILE_VERSION_INFO *pActual,
                                        LoggableAssembly              *pLogAsm);

#endif // FEATURE_PREJIT



#ifndef FEATURE_CORECLR

#include "iilfingerprint.h"

#endif //!FEATURE_CORECLR

#endif  // _CORPRIV_H_
// EOF =======================================================================

