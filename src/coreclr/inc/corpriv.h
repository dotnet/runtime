// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
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
//

interface IAssemblyName;

class UTSemReadWrite;

// Helper function to get a pointer to the Dispenser interface.
STDAPI MetaDataGetDispenser(            // Return HRESULT
    REFCLSID    rclsid,                 // The class to desired.
    REFIID      riid,                   // Interface wanted on class factory.
    LPVOID FAR  *ppv);                  // Return interface pointer here.

BOOL RuntimeFileNotFound(HRESULT hr);

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

STDAPI GetAssemblyMDInternalImportByStream( // Return code.
    IStream     *pIStream,              // [IN] The IStream for the file
    UINT64      AssemblyId,             // [IN] Unique Id for the assembly
    REFIID      riid,                   // [IN] The interface desired.
    IUnknown    **ppIUnk);              // [OUT] Return interface on success.


enum MDInternalImportFlags
{
    MDInternalImport_Default            = 0,
    MDInternalImport_NoCache            = 1, // Do not share/cached the results of opening the image
    // unused                           = 2,
    // unused                           = 4,
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
    _In_z_ LPCWSTR wzCustomString,                     // [IN] Custom string of the native image
    DWORD dwConfigMask,                         // [IN] Config mask of the native image
    _Out_writes_to_opt_(*pdwLength,*pdwLength) LPWSTR pwzZapInfo,// [OUT] The description string. Can be NULL to find the size of buffer to allocate
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

    // UNUSED = 0x3c,

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

    ELEMENT_TYPE_CANON_ZAPSIG            = 0x3e,     // zapsig encoding for System.__Canon
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

#define COR_MODULE_CLASS    "<Module>"
#define COR_WMODULE_CLASS   W("<Module>")

//*****************************************************************************
//*****************************************************************************
//
// CeeGen interfaces for generating in-memory Common Language Runtime files
//
//*****************************************************************************
//*****************************************************************************

typedef void* HCEESECTION;

typedef enum {
    sdNone = 0,
    sdReadOnly = IMAGE_SCN_MEM_READ | IMAGE_SCN_CNT_INITIALIZED_DATA,
    sdReadWrite = sdReadOnly | IMAGE_SCN_MEM_WRITE,
    sdExecute = IMAGE_SCN_MEM_READ | IMAGE_SCN_CNT_CODE | IMAGE_SCN_MEM_EXECUTE
} CeeSectionAttr;

//
// Relocation types.
//

typedef enum {
    // generate only a section-relative reloc, nothing into .reloc section
    srRelocAbsolute,

    // generate a .reloc for a pointer sized location,
    // This is transformed into BASED_HIGHLOW or BASED_DIR64 based on the platform
    srRelocHighLow = 3,

    // generate a .reloc for the top 16-bits of a 32 bit number, where the
    // bottom 16 bits are included in the next word in the .reloc table
    srRelocHighAdj,     // Never Used

    // generate a token map relocation, nothing into .reloc section
    srRelocMapToken,

    // relative address fixup
    srRelocRelative,

    // Generate only a section-relative reloc, nothing into .reloc
    // section.  This reloc is relative to the file position of the
    // section, not the section's virtual address.
    srRelocFilePos,

    // code relative address fixup
    srRelocCodeRelative,

    // generate a .reloc for a 64 bit address in an ia64 movl instruction
    srRelocIA64Imm64,

    // generate a .reloc for a 64 bit address
    srRelocDir64,

    // generate a .reloc for a 25-bit PC relative address in an ia64 br.call instruction
    srRelocIA64PcRel25,

    // generate a .reloc for a 64-bit PC relative address in an ia64 brl.call instruction
    srRelocIA64PcRel64,

    // generate a 30-bit section-relative reloc, used for tagged pointer values
    srRelocAbsoluteTagged,


    // A sentinel value to help ensure any additions to this enum are reflected
    // in PEWriter.cpp's RelocName array.
    srRelocSentinel,

    // Flags that can be used with the above reloc types

    // do not emit base reloc
    srNoBaseReloc = 0x4000,

    // pre-fixup contents of memory are ptr rather than a section offset
    srRelocPtr = 0x8000,

    // legal enums which include the Ptr flag
    srRelocAbsolutePtr = srRelocPtr + srRelocAbsolute,
    srRelocHighLowPtr = srRelocPtr + srRelocHighLow,
    srRelocRelativePtr = srRelocPtr + srRelocRelative,
    srRelocIA64Imm64Ptr = srRelocPtr + srRelocIA64Imm64,
    srRelocDir64Ptr = srRelocPtr + srRelocDir64,

} CeeSectionRelocType;

typedef union {
    USHORT highAdj;
} CeeSectionRelocExtra;

//-------------------------------------
//--- ICeeGenInternal
//-------------------------------------
// {8C26FC02-BE39-476D-B835-E17EDD120246}
EXTERN_GUID(IID_ICeeGenInternal, 0x8c26fc02, 0xbe39, 0x476d, 0xb8, 0x35, 0xe1, 0x7e, 0xdd, 0x12, 0x2, 0x46);
#undef  INTERFACE
#define INTERFACE ICeeGenInternal
DECLARE_INTERFACE_(ICeeGenInternal, IUnknown)
{
    STDMETHOD(EmitString) (
        _In_
        LPWSTR lpString,                    // [IN] String to emit
        ULONG * RVA) PURE;                   // [OUT] RVA for string emitted string

    STDMETHOD(GetString) (
        ULONG RVA,                          // [IN] RVA for string to return
        _Out_opt_
        LPWSTR * lpString) PURE;             // [OUT] Returned string

    STDMETHOD(AllocateMethodBuffer) (
        ULONG cchBuffer,                    // [IN] Length of buffer to create
        UCHAR * *lpBuffer,                   // [OUT] Returned buffer
        ULONG * RVA) PURE;                   // [OUT] RVA for method

    STDMETHOD(GetMethodBuffer) (
        ULONG RVA,                          // [IN] RVA for method to return
        UCHAR * *lpBuffer) PURE;             // [OUT] Returned buffer

    STDMETHOD(GetIMapTokenIface) (
        IUnknown * *pIMapToken) PURE;

    STDMETHOD(GenerateCeeFile) () PURE;

    STDMETHOD(GetIlSection) (
        HCEESECTION * section) PURE;

    STDMETHOD(GetStringSection) (
        HCEESECTION * section) PURE;

    STDMETHOD(AddSectionReloc) (
        HCEESECTION section,
        ULONG offset,
        HCEESECTION relativeTo,
        CeeSectionRelocType relocType) PURE;

    // use these only if you have special section requirements not handled
    // by other APIs
    STDMETHOD(GetSectionCreate) (
        const char* name,
        DWORD flags,
        HCEESECTION * section) PURE;

    STDMETHOD(GetSectionDataLen) (
        HCEESECTION section,
        ULONG * dataLen) PURE;

    STDMETHOD(GetSectionBlock) (
        HCEESECTION section,
        ULONG len,
        ULONG align = 1,
        void** ppBytes = 0) PURE;

    STDMETHOD(ComputePointer) (
        HCEESECTION section,
        ULONG RVA,                          // [IN] RVA for method to return
        UCHAR * *lpBuffer) PURE;             // [OUT] Returned buffer

    STDMETHOD(SetInitialGrowth) (DWORD growth) PURE;
};

//
// IGetIMDInternalImport
//
// Private interface exposed by
//    AssemblyMDInternalImport - gives us access to the internally stored IMDInternalImport*.
//
//    RegMeta - supports the internal GetMetaDataInternalInterfaceFromPublic() "api".
//
// {92B2FEF9-F7F5-420d-AD42-AECEEE10A1EF}
EXTERN_GUID(IID_IGetIMDInternalImport, 0x92b2fef9, 0xf7f5, 0x420d, 0xad, 0x42, 0xae, 0xce, 0xee, 0x10, 0xa1, 0xef);
#undef  INTERFACE
#define INTERFACE IGetIMDInternalImport
DECLARE_INTERFACE_(IGetIMDInternalImport, IUnknown)
{
    STDMETHOD(GetIMDInternalImport) (
        IMDInternalImport ** ppIMDInternalImport   // [OUT] Buffer to receive IMDInternalImport*
    ) PURE;
};

#endif  // _CORPRIV_H_

