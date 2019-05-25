// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*****************************************************************************
 **                                                                         **
 ** Cor.h - general header for the Runtime.                                 **
 **                                                                         **
 *****************************************************************************/


#ifndef _COR_H_
#define _COR_H_

//*****************************************************************************
// Required includes
#include <ole2.h>                       // Definitions of OLE types.
#include <specstrings.h>
#include "corerror.h"

//*****************************************************************************

#ifdef __cplusplus
extern "C" {
#endif

// {BED7F4EA-1A96-11d2-8F08-00A0C9A6186D}
EXTERN_GUID(LIBID_ComPlusRuntime, 0xbed7f4ea, 0x1a96, 0x11d2, 0x8f, 0x8, 0x0, 0xa0, 0xc9, 0xa6, 0x18, 0x6d);

// {90883F05-3D28-11D2-8F17-00A0C9A6186D}
EXTERN_GUID(GUID_ExportedFromComPlus, 0x90883f05, 0x3d28, 0x11d2, 0x8f, 0x17, 0x0, 0xa0, 0xc9, 0xa6, 0x18, 0x6d);

// {0F21F359-AB84-41e8-9A78-36D110E6D2F9}
EXTERN_GUID(GUID_ManagedName, 0xf21f359, 0xab84, 0x41e8, 0x9a, 0x78, 0x36, 0xd1, 0x10, 0xe6, 0xd2, 0xf9);

// {54FC8F55-38DE-4703-9C4E-250351302B1C}
EXTERN_GUID(GUID_Function2Getter, 0x54fc8f55, 0x38de, 0x4703, 0x9c, 0x4e, 0x25, 0x3, 0x51, 0x30, 0x2b, 0x1c);

// CLSID_CorMetaDataDispenserRuntime: {1EC2DE53-75CC-11d2-9775-00A0C9B4D50C}
//  Dispenser coclass for version 1.5 and 2.0 meta data.  To get the "latest" bind
//  to CLSID_MetaDataDispenser.
EXTERN_GUID(CLSID_CorMetaDataDispenserRuntime, 0x1ec2de53, 0x75cc, 0x11d2, 0x97, 0x75, 0x0, 0xa0, 0xc9, 0xb4, 0xd5, 0xc);

// {CD2BC5C9-F452-4326-B714-F9C539D4DA58}
EXTERN_GUID(GUID_DispIdOverride, 0xcd2bc5c9, 0xf452, 0x4326, 0xb7, 0x14, 0xf9, 0xc5, 0x39, 0xd4, 0xda, 0x58);

// {B64784EB-D8D4-4d9b-9ACD-0E30806426F7}
EXTERN_GUID(GUID_ForceIEnumerable, 0xb64784eb, 0xd8d4, 0x4d9b, 0x9a, 0xcd, 0x0e, 0x30, 0x80, 0x64, 0x26, 0xf7);

// {2941FF83-88D8-4F73-B6A9-BDF8712D000D}
EXTERN_GUID(GUID_PropGetCA, 0x2941ff83, 0x88d8, 0x4f73, 0xb6, 0xa9, 0xbd, 0xf8, 0x71, 0x2d, 0x00, 0x0d);

// {29533527-3683-4364-ABC0-DB1ADD822FA2}
EXTERN_GUID(GUID_PropPutCA, 0x29533527, 0x3683, 0x4364, 0xab, 0xc0, 0xdb, 0x1a, 0xdd, 0x82, 0x2f, 0xa2);

// CLSID_CLR_v1_MetaData: {005023CA-72B1-11D3-9FC4-00C04F79A0A3}
//  Used to generate v1 metadata (for v1.0 and v1.1 CLR compatibility).
EXTERN_GUID(CLSID_CLR_v1_MetaData, 0x005023ca, 0x72b1, 0x11d3, 0x9f, 0xc4, 0x0, 0xc0, 0x4f, 0x79, 0xa0, 0xa3);

// CLSID_CLR_v2_MetaData: {EFEA471A-44FD-4862-9292-0C58D46E1F3A}
EXTERN_GUID(CLSID_CLR_v2_MetaData, 0xefea471a, 0x44fd, 0x4862, 0x92, 0x92, 0xc, 0x58, 0xd4, 0x6e, 0x1f, 0x3a);


// CLSID_CorMetaDataRuntime:
// This will can always be used to generate the "latest" metadata available.
#define CLSID_CorMetaDataRuntime CLSID_CLR_v2_MetaData


// {30FE7BE8-D7D9-11D2-9F80-00C04F79A0A3}
EXTERN_GUID(MetaDataCheckDuplicatesFor, 0x30fe7be8, 0xd7d9, 0x11d2, 0x9f, 0x80, 0x0, 0xc0, 0x4f, 0x79, 0xa0, 0xa3);

// {DE3856F8-D7D9-11D2-9F80-00C04F79A0A3}
EXTERN_GUID(MetaDataRefToDefCheck, 0xde3856f8, 0xd7d9, 0x11d2, 0x9f, 0x80, 0x0, 0xc0, 0x4f, 0x79, 0xa0, 0xa3);

// {E5D71A4C-D7DA-11D2-9F80-00C04F79A0A3}
EXTERN_GUID(MetaDataNotificationForTokenMovement, 0xe5d71a4c, 0xd7da, 0x11d2, 0x9f, 0x80, 0x0, 0xc0, 0x4f, 0x79, 0xa0, 0xa3);

// {2eee315c-d7db-11d2-9f80-00c04f79a0a3}
EXTERN_GUID(MetaDataSetUpdate, 0x2eee315c, 0xd7db, 0x11d2, 0x9f, 0x80, 0x0, 0xc0, 0x4f, 0x79, 0xa0, 0xa3);
#define MetaDataSetENC MetaDataSetUpdate

// Use this guid in SetOption to indicate if the import enumerator should skip over
// delete items or not. The default is yes.
//
// {79700F36-4AAC-11d3-84C3-009027868CB1}
EXTERN_GUID(MetaDataImportOption, 0x79700f36, 0x4aac, 0x11d3, 0x84, 0xc3, 0x0, 0x90, 0x27, 0x86, 0x8c, 0xb1);

// Use this guid in the SetOption if compiler wants to have MetaData API to take reader/writer lock
//
// {F7559806-F266-42ea-8C63-0ADB45E8B234}
EXTERN_GUID(MetaDataThreadSafetyOptions, 0xf7559806, 0xf266, 0x42ea, 0x8c, 0x63, 0xa, 0xdb, 0x45, 0xe8, 0xb2, 0x34);

// Use this guid in the SetOption if compiler wants error when some tokens are emitted out of order
// {1547872D-DC03-11d2-9420-0000F8083460}
EXTERN_GUID(MetaDataErrorIfEmitOutOfOrder, 0x1547872d, 0xdc03, 0x11d2, 0x94, 0x20, 0x0, 0x0, 0xf8, 0x8, 0x34, 0x60);

// Use this guid in the SetOption to indicate if the tlbimporter should generate the
// TCE adapters for COM connection point containers.
// {DCC9DE90-4151-11d3-88D6-00902754C43A}
EXTERN_GUID(MetaDataGenerateTCEAdapters, 0xdcc9de90, 0x4151, 0x11d3, 0x88, 0xd6, 0x0, 0x90, 0x27, 0x54, 0xc4, 0x3a);

// Use this guid in the SetOption to specifiy a non-default namespace for typelib import.
// {F17FF889-5A63-11d3-9FF2-00C04FF7431A}
EXTERN_GUID(MetaDataTypeLibImportNamespace, 0xf17ff889, 0x5a63, 0x11d3, 0x9f, 0xf2, 0x0, 0xc0, 0x4f, 0xf7, 0x43, 0x1a);

// Use this guid in the SetOption to specify the behavior of UnmarkAll. See CorLinkerOptions.
// {47E099B6-AE7C-4797-8317-B48AA645B8F9}
EXTERN_GUID(MetaDataLinkerOptions, 0x47e099b6, 0xae7c, 0x4797, 0x83, 0x17, 0xb4, 0x8a, 0xa6, 0x45, 0xb8, 0xf9);

// Use this guid in the SetOption to specify the runtime version stored in the CLR metadata.
// {47E099B7-AE7C-4797-8317-B48AA645B8F9}
EXTERN_GUID(MetaDataRuntimeVersion, 0x47e099b7, 0xae7c, 0x4797, 0x83, 0x17, 0xb4, 0x8a, 0xa6, 0x45, 0xb8, 0xf9);

// Use this guid in the SetOption to specify the behavior of the merger.
// {132D3A6E-B35D-464e-951A-42EFB9FB6601}
EXTERN_GUID(MetaDataMergerOptions, 0x132d3a6e, 0xb35d, 0x464e, 0x95, 0x1a, 0x42, 0xef, 0xb9, 0xfb, 0x66, 0x1);

// Use this guid in SetOption to disable optimizing module-local refs to defs
// {a55c0354-e91b-468b-8648-7cc31035d533}
EXTERN_GUID(MetaDataPreserveLocalRefs, 0xa55c0354, 0xe91b, 0x468b, 0x86, 0x48, 0x7c, 0xc3, 0x10, 0x35, 0xd5, 0x33);

interface IMetaDataImport;
interface IMetaDataAssemblyEmit;
interface IMetaDataAssemblyImport;
interface IMetaDataEmit;
interface ICeeGen;


typedef UNALIGNED void const *UVCP_CONSTANT;


// Constant for connection id and task id
#define INVALID_CONNECTION_ID   0x0
#define INVALID_TASK_ID         0x0 
#define MAX_CONNECTION_NAME     MAX_PATH


#define MAIN_CLR_MODULE_NAME_W        W("coreclr")
#define MAIN_CLR_MODULE_NAME_A         "coreclr"

#define MAIN_CLR_DLL_NAME_W           MAKEDLLNAME_W(MAIN_CLR_MODULE_NAME_W)
#define MAIN_CLR_DLL_NAME_A           MAKEDLLNAME_A(MAIN_CLR_MODULE_NAME_A)


#define MSCOREE_SHIM_W               MAIN_CLR_DLL_NAME_W
#define MSCOREE_SHIM_A               MAIN_CLR_DLL_NAME_A

#define SWITCHOUT_HANDLE_VALUE ((HANDLE)(LONG_PTR)-2)

//
// CoInitializeEE flags.
//
typedef enum tagCOINITEE
{
    COINITEE_DEFAULT        = 0x0,          // Default initialization mode. 
    COINITEE_DLL            = 0x1,          // Initialization mode for loading DLL. 
    COINITEE_MAIN           = 0x2           // Initialize prior to entering the main routine 
} COINITIEE;

//*****************************************************************************
//*****************************************************************************
//
// I L   &   F I L E   F O R M A T   D E C L A R A T I O N S
//
//*****************************************************************************
//*****************************************************************************


// 
#ifndef _WINDOWS_UPDATES_
#include <corhdr.h>
#endif // <windows.h> updates

//*****************************************************************************
//*****************************************************************************

// CLSID_Cor: {bee00000-ee77-11d0-a015-00c04fbbb884}
EXTERN_GUID(CLSID_Cor, 0xbee00010, 0xee77, 0x11d0, 0xa0, 0x15, 0x00, 0xc0, 0x4f, 0xbb, 0xb8, 0x84);

// CLSID_CorMetaDataDispenser: {E5CB7A31-7512-11d2-89CE-0080C792E5D8}
//  This is the "Master Dispenser", always guaranteed to be the most recent
//  dispenser on the machine.
EXTERN_GUID(CLSID_CorMetaDataDispenser, 0xe5cb7a31, 0x7512, 0x11d2, 0x89, 0xce, 0x0, 0x80, 0xc7, 0x92, 0xe5, 0xd8);


// CLSID_CorMetaDataDispenserReg: {435755FF-7397-11d2-9771-00A0C9B4D50C}
//  Dispenser coclass for version 1.0 meta data.  To get the "latest" bind
//  to CLSID_CorMetaDataDispenser.
EXTERN_GUID(CLSID_CorMetaDataDispenserReg, 0x435755ff, 0x7397, 0x11d2, 0x97, 0x71, 0x0, 0xa0, 0xc9, 0xb4, 0xd5, 0xc);


// CLSID_CorMetaDataReg: {87F3A1F5-7397-11d2-9771-00A0C9B4D50C}
// For COM+ Meta Data, Data Driven Registration
EXTERN_GUID(CLSID_CorMetaDataReg, 0x87f3a1f5, 0x7397, 0x11d2, 0x97, 0x71, 0x0, 0xa0, 0xc9, 0xb4, 0xd5, 0xc);


interface IMetaDataDispenser;

//-------------------------------------
//--- IMetaDataError
//-------------------------------------
// {B81FF171-20F3-11d2-8DCC-00A0C9B09C19}
EXTERN_GUID(IID_IMetaDataError, 0xb81ff171, 0x20f3, 0x11d2, 0x8d, 0xcc, 0x0, 0xa0, 0xc9, 0xb0, 0x9c, 0x19);

//---
#undef  INTERFACE
#define INTERFACE IMetaDataError
DECLARE_INTERFACE_(IMetaDataError, IUnknown)
{
    STDMETHOD(OnError)(HRESULT hrError, mdToken token) PURE;
};

//-------------------------------------
//--- IMapToken
//-------------------------------------
// IID_IMapToken: {06A3EA8B-0225-11d1-BF72-00C04FC31E12}
EXTERN_GUID(IID_IMapToken, 0x6a3ea8b, 0x225, 0x11d1, 0xbf, 0x72, 0x0, 0xc0, 0x4f, 0xc3, 0x1e, 0x12);

//---
#undef  INTERFACE
#define INTERFACE IMapToken
DECLARE_INTERFACE_(IMapToken, IUnknown)
{
    STDMETHOD(Map)(mdToken tkImp, mdToken tkEmit) PURE;
};

//-------------------------------------
//--- IMetaDataDispenser
//-------------------------------------
// {809C652E-7396-11D2-9771-00A0C9B4D50C}
EXTERN_GUID(IID_IMetaDataDispenser, 0x809c652e, 0x7396, 0x11d2, 0x97, 0x71, 0x00, 0xa0, 0xc9, 0xb4, 0xd5, 0x0c);

//---
#undef  INTERFACE
#define INTERFACE IMetaDataDispenser
DECLARE_INTERFACE_(IMetaDataDispenser, IUnknown)
{
    STDMETHOD(DefineScope)(                 // Return code.
        REFCLSID    rclsid,                 // [in] What version to create.
        DWORD       dwCreateFlags,          // [in] Flags on the create.
        REFIID      riid,                   // [in] The interface desired.
        IUnknown    **ppIUnk) PURE;         // [out] Return interface on success.

    STDMETHOD(OpenScope)(                   // Return code.
        LPCWSTR     szScope,                // [in] The scope to open.
        DWORD       dwOpenFlags,            // [in] Open mode flags.
        REFIID      riid,                   // [in] The interface desired.
        IUnknown    **ppIUnk) PURE;         // [out] Return interface on success.

    STDMETHOD(OpenScopeOnMemory)(           // Return code.
        LPCVOID     pData,                  // [in] Location of scope data.
        ULONG       cbData,                 // [in] Size of the data pointed to by pData.
        DWORD       dwOpenFlags,            // [in] Open mode flags.
        REFIID      riid,                   // [in] The interface desired.
        IUnknown    **ppIUnk) PURE;         // [out] Return interface on success.
};

//-------------------------------------
//--- IMetaDataEmit
//-------------------------------------
// {BA3FEE4C-ECB9-4e41-83B7-183FA41CD859}
EXTERN_GUID(IID_IMetaDataEmit, 0xba3fee4c, 0xecb9, 0x4e41, 0x83, 0xb7, 0x18, 0x3f, 0xa4, 0x1c, 0xd8, 0x59);

//---
#undef  INTERFACE
#define INTERFACE IMetaDataEmit
DECLARE_INTERFACE_(IMetaDataEmit, IUnknown)
{
    STDMETHOD(SetModuleProps)(              // S_OK or error.
        LPCWSTR     szName) PURE;           // [IN] If not NULL, the name of the module to set.

    STDMETHOD(Save)(                        // S_OK or error.
        LPCWSTR     szFile,                 // [IN] The filename to save to.
        DWORD       dwSaveFlags) PURE;      // [IN] Flags for the save.

    STDMETHOD(SaveToStream)(                // S_OK or error.
        IStream     *pIStream,              // [IN] A writable stream to save to.
        DWORD       dwSaveFlags) PURE;      // [IN] Flags for the save.

    STDMETHOD(GetSaveSize)(                 // S_OK or error.
        CorSaveSize fSave,                  // [IN] cssAccurate or cssQuick.
        DWORD       *pdwSaveSize) PURE;     // [OUT] Put the size here.

    STDMETHOD(DefineTypeDef)(               // S_OK or error.
        LPCWSTR     szTypeDef,              // [IN] Name of TypeDef
        DWORD       dwTypeDefFlags,         // [IN] CustomAttribute flags
        mdToken     tkExtends,              // [IN] extends this TypeDef or typeref 
        mdToken     rtkImplements[],        // [IN] Implements interfaces
        mdTypeDef   *ptd) PURE;             // [OUT] Put TypeDef token here

    STDMETHOD(DefineNestedType)(            // S_OK or error.
        LPCWSTR     szTypeDef,              // [IN] Name of TypeDef
        DWORD       dwTypeDefFlags,         // [IN] CustomAttribute flags
        mdToken     tkExtends,              // [IN] extends this TypeDef or typeref 
        mdToken     rtkImplements[],        // [IN] Implements interfaces
        mdTypeDef   tdEncloser,             // [IN] TypeDef token of the enclosing type.
        mdTypeDef   *ptd) PURE;             // [OUT] Put TypeDef token here

    STDMETHOD(SetHandler)(                  // S_OK.
        IUnknown    *pUnk) PURE;            // [IN] The new error handler.

    STDMETHOD(DefineMethod)(                // S_OK or error. 
        mdTypeDef   td,                     // Parent TypeDef
        LPCWSTR     szName,                 // Name of member
        DWORD       dwMethodFlags,          // Member attributes
        PCCOR_SIGNATURE pvSigBlob,          // [IN] point to a blob value of CLR signature 
        ULONG       cbSigBlob,              // [IN] count of bytes in the signature blob
        ULONG       ulCodeRVA,
        DWORD       dwImplFlags,
        mdMethodDef *pmd) PURE;             // Put member token here

    STDMETHOD(DefineMethodImpl)(            // S_OK or error.
        mdTypeDef   td,                     // [IN] The class implementing the method
        mdToken     tkBody,                 // [IN] Method body - MethodDef or MethodRef
        mdToken     tkDecl) PURE;           // [IN] Method declaration - MethodDef or MethodRef

    STDMETHOD(DefineTypeRefByName)(         // S_OK or error.
        mdToken     tkResolutionScope,      // [IN] ModuleRef, AssemblyRef or TypeRef.
        LPCWSTR     szName,                 // [IN] Name of the TypeRef.
        mdTypeRef   *ptr) PURE;             // [OUT] Put TypeRef token here.

    STDMETHOD(DefineImportType)(            // S_OK or error.
        IMetaDataAssemblyImport *pAssemImport,  // [IN] Assembly containing the TypeDef.
        const void  *pbHashValue,           // [IN] Hash Blob for Assembly.
        ULONG       cbHashValue,            // [IN] Count of bytes.
        IMetaDataImport *pImport,           // [IN] Scope containing the TypeDef.
        mdTypeDef   tdImport,               // [IN] The imported TypeDef.
        IMetaDataAssemblyEmit *pAssemEmit,  // [IN] Assembly into which the TypeDef is imported.
        mdTypeRef   *ptr) PURE;             // [OUT] Put TypeRef token here.

    STDMETHOD(DefineMemberRef)(             // S_OK or error
        mdToken     tkImport,               // [IN] ClassRef or ClassDef importing a member.
        LPCWSTR     szName,                 // [IN] member's name
        PCCOR_SIGNATURE pvSigBlob,          // [IN] point to a blob value of CLR signature 
        ULONG       cbSigBlob,              // [IN] count of bytes in the signature blob
        mdMemberRef *pmr) PURE;             // [OUT] memberref token

    STDMETHOD(DefineImportMember)(          // S_OK or error.
        IMetaDataAssemblyImport *pAssemImport,  // [IN] Assembly containing the Member.
        const void  *pbHashValue,           // [IN] Hash Blob for Assembly.
        ULONG       cbHashValue,            // [IN] Count of bytes.
        IMetaDataImport *pImport,           // [IN] Import scope, with member.
        mdToken     mbMember,               // [IN] Member in import scope.
        IMetaDataAssemblyEmit *pAssemEmit,  // [IN] Assembly into which the Member is imported.
        mdToken     tkParent,               // [IN] Classref or classdef in emit scope.
        mdMemberRef *pmr) PURE;             // [OUT] Put member ref here.

    STDMETHOD(DefineEvent) (
        mdTypeDef   td,                     // [IN] the class/interface on which the event is being defined 
        LPCWSTR     szEvent,                // [IN] Name of the event
        DWORD       dwEventFlags,           // [IN] CorEventAttr
        mdToken     tkEventType,            // [IN] a reference (mdTypeRef or mdTypeRef) to the Event class 
        mdMethodDef mdAddOn,                // [IN] required add method 
        mdMethodDef mdRemoveOn,             // [IN] required remove method
        mdMethodDef mdFire,                 // [IN] optional fire method
        mdMethodDef rmdOtherMethods[],      // [IN] optional array of other methods associate with the event
        mdEvent     *pmdEvent) PURE;        // [OUT] output event token 

    STDMETHOD(SetClassLayout) (
        mdTypeDef   td,                     // [IN] typedef 
        DWORD       dwPackSize,             // [IN] packing size specified as 1, 2, 4, 8, or 16 
        COR_FIELD_OFFSET rFieldOffsets[],   // [IN] array of layout specification
        ULONG       ulClassSize) PURE;      // [IN] size of the class

    STDMETHOD(DeleteClassLayout) (
        mdTypeDef   td) PURE;               // [IN] typedef whose layout is to be deleted.

    STDMETHOD(SetFieldMarshal) (
        mdToken     tk,                     // [IN] given a fieldDef or paramDef token
        PCCOR_SIGNATURE pvNativeType,       // [IN] native type specification
        ULONG       cbNativeType) PURE;     // [IN] count of bytes of pvNativeType

    STDMETHOD(DeleteFieldMarshal) (
        mdToken     tk) PURE;               // [IN] given a fieldDef or paramDef token

    STDMETHOD(DefinePermissionSet) (
        mdToken     tk,                     // [IN] the object to be decorated. 
        DWORD       dwAction,               // [IN] CorDeclSecurity.
        void const  *pvPermission,          // [IN] permission blob.
        ULONG       cbPermission,           // [IN] count of bytes of pvPermission. 
        mdPermission *ppm) PURE;            // [OUT] returned permission token. 

    STDMETHOD(SetRVA)(                      // S_OK or error.
        mdMethodDef md,                     // [IN] Method for which to set offset
        ULONG       ulRVA) PURE;            // [IN] The offset

    STDMETHOD(GetTokenFromSig)(             // S_OK or error.
        PCCOR_SIGNATURE pvSig,              // [IN] Signature to define.
        ULONG       cbSig,                  // [IN] Size of signature data. 
        mdSignature *pmsig) PURE;           // [OUT] returned signature token.

    STDMETHOD(DefineModuleRef)(             // S_OK or error.
        LPCWSTR     szName,                 // [IN] DLL name
        mdModuleRef *pmur) PURE;            // [OUT] returned

    // <TODO>@FUTURE:  This should go away once everyone starts using SetMemberRefProps.</TODO>
    STDMETHOD(SetParent)(                   // S_OK or error.
        mdMemberRef mr,                     // [IN] Token for the ref to be fixed up.
        mdToken     tk) PURE;               // [IN] The ref parent. 

    STDMETHOD(GetTokenFromTypeSpec)(        // S_OK or error.
        PCCOR_SIGNATURE pvSig,              // [IN] TypeSpec Signature to define.
        ULONG       cbSig,                  // [IN] Size of signature data. 
        mdTypeSpec *ptypespec) PURE;        // [OUT] returned TypeSpec token.

    STDMETHOD(SaveToMemory)(                // S_OK or error.
        void        *pbData,                // [OUT] Location to write data.
        ULONG       cbData) PURE;           // [IN] Max size of data buffer.

    STDMETHOD(DefineUserString)(            // Return code.
        LPCWSTR szString,                   // [IN] User literal string.
        ULONG       cchString,              // [IN] Length of string.
        mdString    *pstk) PURE;            // [OUT] String token.

    STDMETHOD(DeleteToken)(                 // Return code.
        mdToken     tkObj) PURE;            // [IN] The token to be deleted

    STDMETHOD(SetMethodProps)(              // S_OK or error.
        mdMethodDef md,                     // [IN] The MethodDef.
        DWORD       dwMethodFlags,          // [IN] Method attributes.
        ULONG       ulCodeRVA,              // [IN] Code RVA.
        DWORD       dwImplFlags) PURE;      // [IN] Impl flags.

    STDMETHOD(SetTypeDefProps)(             // S_OK or error.
        mdTypeDef   td,                     // [IN] The TypeDef.
        DWORD       dwTypeDefFlags,         // [IN] TypeDef flags.
        mdToken     tkExtends,              // [IN] Base TypeDef or TypeRef.
        mdToken     rtkImplements[]) PURE;  // [IN] Implemented interfaces.

    STDMETHOD(SetEventProps)(               // S_OK or error.
        mdEvent     ev,                     // [IN] The event token.
        DWORD       dwEventFlags,           // [IN] CorEventAttr.
        mdToken     tkEventType,            // [IN] A reference (mdTypeRef or mdTypeRef) to the Event class.
        mdMethodDef mdAddOn,                // [IN] Add method.
        mdMethodDef mdRemoveOn,             // [IN] Remove method.
        mdMethodDef mdFire,                 // [IN] Fire method.
        mdMethodDef rmdOtherMethods[]) PURE;// [IN] Array of other methods associate with the event.

    STDMETHOD(SetPermissionSetProps)(       // S_OK or error.
        mdToken     tk,                     // [IN] The object to be decorated.
        DWORD       dwAction,               // [IN] CorDeclSecurity.
        void const  *pvPermission,          // [IN] Permission blob.
        ULONG       cbPermission,           // [IN] Count of bytes of pvPermission.
        mdPermission *ppm) PURE;            // [OUT] Permission token.

    STDMETHOD(DefinePinvokeMap)(            // Return code.
        mdToken     tk,                     // [IN] FieldDef or MethodDef.
        DWORD       dwMappingFlags,         // [IN] Flags used for mapping.
        LPCWSTR     szImportName,           // [IN] Import name.
        mdModuleRef mrImportDLL) PURE;      // [IN] ModuleRef token for the target DLL.

    STDMETHOD(SetPinvokeMap)(               // Return code.
        mdToken     tk,                     // [IN] FieldDef or MethodDef.
        DWORD       dwMappingFlags,         // [IN] Flags used for mapping.
        LPCWSTR     szImportName,           // [IN] Import name.
        mdModuleRef mrImportDLL) PURE;      // [IN] ModuleRef token for the target DLL.

    STDMETHOD(DeletePinvokeMap)(            // Return code.
        mdToken     tk) PURE;               // [IN] FieldDef or MethodDef.

    // New CustomAttribute functions.
    STDMETHOD(DefineCustomAttribute)(       // Return code.
        mdToken     tkOwner,                // [IN] The object to put the value on.
        mdToken     tkCtor,                 // [IN] Constructor of the CustomAttribute type (MemberRef/MethodDef).
        void const  *pCustomAttribute,      // [IN] The custom value data.
        ULONG       cbCustomAttribute,      // [IN] The custom value data length.
        mdCustomAttribute *pcv) PURE;       // [OUT] The custom value token value on return.

    STDMETHOD(SetCustomAttributeValue)(     // Return code.
        mdCustomAttribute pcv,              // [IN] The custom value token whose value to replace.
        void const  *pCustomAttribute,      // [IN] The custom value data.
        ULONG       cbCustomAttribute) PURE;// [IN] The custom value data length.

    STDMETHOD(DefineField)(                 // S_OK or error. 
        mdTypeDef   td,                     // Parent TypeDef
        LPCWSTR     szName,                 // Name of member
        DWORD       dwFieldFlags,           // Member attributes
        PCCOR_SIGNATURE pvSigBlob,          // [IN] point to a blob value of CLR signature 
        ULONG       cbSigBlob,              // [IN] count of bytes in the signature blob
        DWORD       dwCPlusTypeFlag,        // [IN] flag for value type. selected ELEMENT_TYPE_*
        void const  *pValue,                // [IN] constant value
        ULONG       cchValue,               // [IN] size of constant value (string, in wide chars).
        mdFieldDef  *pmd) PURE;             // [OUT] Put member token here

    STDMETHOD(DefineProperty)( 
        mdTypeDef   td,                     // [IN] the class/interface on which the property is being defined
        LPCWSTR     szProperty,             // [IN] Name of the property
        DWORD       dwPropFlags,            // [IN] CorPropertyAttr 
        PCCOR_SIGNATURE pvSig,              // [IN] the required type signature 
        ULONG       cbSig,                  // [IN] the size of the type signature blob 
        DWORD       dwCPlusTypeFlag,        // [IN] flag for value type. selected ELEMENT_TYPE_*
        void const  *pValue,                // [IN] constant value
        ULONG       cchValue,               // [IN] size of constant value (string, in wide chars).
        mdMethodDef mdSetter,               // [IN] optional setter of the property 
        mdMethodDef mdGetter,               // [IN] optional getter of the property 
        mdMethodDef rmdOtherMethods[],      // [IN] an optional array of other methods
        mdProperty  *pmdProp) PURE;         // [OUT] output property token

    STDMETHOD(DefineParam)(
        mdMethodDef md,                     // [IN] Owning method
        ULONG       ulParamSeq,             // [IN] Which param 
        LPCWSTR     szName,                 // [IN] Optional param name 
        DWORD       dwParamFlags,           // [IN] Optional param flags
        DWORD       dwCPlusTypeFlag,        // [IN] flag for value type. selected ELEMENT_TYPE_*
        void const  *pValue,                // [IN] constant value
        ULONG       cchValue,               // [IN] size of constant value (string, in wide chars).
        mdParamDef  *ppd) PURE;             // [OUT] Put param token here

    STDMETHOD(SetFieldProps)(               // S_OK or error.
        mdFieldDef  fd,                     // [IN] The FieldDef.
        DWORD       dwFieldFlags,           // [IN] Field attributes.
        DWORD       dwCPlusTypeFlag,        // [IN] Flag for the value type, selected ELEMENT_TYPE_*
        void const  *pValue,                // [IN] Constant value.
        ULONG       cchValue) PURE;         // [IN] size of constant value (string, in wide chars).

    STDMETHOD(SetPropertyProps)(            // S_OK or error.
        mdProperty  pr,                     // [IN] Property token.
        DWORD       dwPropFlags,            // [IN] CorPropertyAttr.
        DWORD       dwCPlusTypeFlag,        // [IN] Flag for value type, selected ELEMENT_TYPE_*
        void const  *pValue,                // [IN] Constant value.
        ULONG       cchValue,               // [IN] size of constant value (string, in wide chars).
        mdMethodDef mdSetter,               // [IN] Setter of the property.
        mdMethodDef mdGetter,               // [IN] Getter of the property.
        mdMethodDef rmdOtherMethods[]) PURE;// [IN] Array of other methods.

    STDMETHOD(SetParamProps)(               // Return code.
        mdParamDef  pd,                     // [IN] Param token.
        LPCWSTR     szName,                 // [IN] Param name.
        DWORD       dwParamFlags,           // [IN] Param flags.
        DWORD       dwCPlusTypeFlag,        // [IN] Flag for value type. selected ELEMENT_TYPE_*.
        void const  *pValue,                // [OUT] Constant value.
        ULONG       cchValue) PURE;         // [IN] size of constant value (string, in wide chars).

    // Specialized Custom Attributes for security.
    STDMETHOD(DefineSecurityAttributeSet)(  // Return code.
        mdToken     tkObj,                  // [IN] Class or method requiring security attributes.
        COR_SECATTR rSecAttrs[],            // [IN] Array of security attribute descriptions.
        ULONG       cSecAttrs,              // [IN] Count of elements in above array.
        ULONG       *pulErrorAttr) PURE;    // [OUT] On error, index of attribute causing problem.

    STDMETHOD(ApplyEditAndContinue)(        // S_OK or error.
        IUnknown    *pImport) PURE;         // [IN] Metadata from the delta PE.

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

    STDMETHOD(SetMethodImplFlags)(          // [IN] S_OK or error.
        mdMethodDef md,                     // [IN] Method for which to set ImplFlags 
        DWORD       dwImplFlags) PURE;

    STDMETHOD(SetFieldRVA)(                 // [IN] S_OK or error.
        mdFieldDef  fd,                     // [IN] Field for which to set offset
        ULONG       ulRVA) PURE;            // [IN] The offset

    STDMETHOD(Merge)(                       // S_OK or error.
        IMetaDataImport *pImport,           // [IN] The scope to be merged.
        IMapToken   *pHostMapToken,         // [IN] Host IMapToken interface to receive token remap notification
        IUnknown    *pHandler) PURE;        // [IN] An object to receive to receive error notification.

    STDMETHOD(MergeEnd)() PURE;             // S_OK or error.

    // This interface is sealed.  Do not change, add, or remove anything.  Instead, derive a new iterface.

};      // IMetaDataEmit

//-------------------------------------
//--- IMetaDataEmit2
//-------------------------------------
// {F5DD9950-F693-42e6-830E-7B833E8146A9}
EXTERN_GUID(IID_IMetaDataEmit2, 0xf5dd9950, 0xf693, 0x42e6, 0x83, 0xe, 0x7b, 0x83, 0x3e, 0x81, 0x46, 0xa9);

//---
#undef  INTERFACE
#define INTERFACE IMetaDataEmit2
DECLARE_INTERFACE_(IMetaDataEmit2, IMetaDataEmit)
{
    STDMETHOD(DefineMethodSpec)(
        mdToken     tkParent,               // [IN] MethodDef or MemberRef
        PCCOR_SIGNATURE pvSigBlob,          // [IN] point to a blob value of COM+ signature 
        ULONG       cbSigBlob,              // [IN] count of bytes in the signature blob
        mdMethodSpec *pmi) PURE;            // [OUT] method instantiation token

    STDMETHOD(GetDeltaSaveSize)(            // S_OK or error.
        CorSaveSize fSave,                  // [IN] cssAccurate or cssQuick.
        DWORD       *pdwSaveSize) PURE;     // [OUT] Put the size here.

    STDMETHOD(SaveDelta)(                   // S_OK or error.
        LPCWSTR     szFile,                 // [IN] The filename to save to.
        DWORD       dwSaveFlags) PURE;      // [IN] Flags for the save.

    STDMETHOD(SaveDeltaToStream)(           // S_OK or error.
        IStream     *pIStream,              // [IN] A writable stream to save to.
        DWORD       dwSaveFlags) PURE;      // [IN] Flags for the save.

    STDMETHOD(SaveDeltaToMemory)(           // S_OK or error.
        void        *pbData,                // [OUT] Location to write data.
        ULONG       cbData) PURE;           // [IN] Max size of data buffer.

    STDMETHOD(DefineGenericParam)(          // S_OK or error.
        mdToken      tk,                    // [IN] TypeDef or MethodDef
        ULONG        ulParamSeq,            // [IN] Index of the type parameter
        DWORD        dwParamFlags,          // [IN] Flags, for future use (e.g. variance)
        LPCWSTR      szname,                // [IN] Name
        DWORD        reserved,              // [IN] For future use (e.g. non-type parameters)
        mdToken      rtkConstraints[],      // [IN] Array of type constraints (TypeDef,TypeRef,TypeSpec)
        mdGenericParam *pgp) PURE;          // [OUT] Put GenericParam token here

    STDMETHOD(SetGenericParamProps)(        // S_OK or error.
        mdGenericParam gp,                  // [IN] GenericParam
        DWORD        dwParamFlags,          // [IN] Flags, for future use (e.g. variance)
        LPCWSTR      szName,                // [IN] Optional name
        DWORD        reserved,              // [IN] For future use (e.g. non-type parameters)
        mdToken      rtkConstraints[]) PURE;// [IN] Array of type constraints (TypeDef,TypeRef,TypeSpec)
    
    STDMETHOD(ResetENCLog)() PURE;          // S_OK or error.

};

//-------------------------------------
//--- IMetaDataImport
//-------------------------------------
// {7DAC8207-D3AE-4c75-9B67-92801A497D44}
EXTERN_GUID(IID_IMetaDataImport, 0x7dac8207, 0xd3ae, 0x4c75, 0x9b, 0x67, 0x92, 0x80, 0x1a, 0x49, 0x7d, 0x44);

//---
#undef  INTERFACE
#define INTERFACE IMetaDataImport
DECLARE_INTERFACE_(IMetaDataImport, IUnknown)
{
    STDMETHOD_(void, CloseEnum)(HCORENUM hEnum) PURE;
    STDMETHOD(CountEnum)(HCORENUM hEnum, ULONG *pulCount) PURE;
    STDMETHOD(ResetEnum)(HCORENUM hEnum, ULONG ulPos) PURE;
    STDMETHOD(EnumTypeDefs)(HCORENUM *phEnum, mdTypeDef rTypeDefs[],
                            ULONG cMax, ULONG *pcTypeDefs) PURE;
    STDMETHOD(EnumInterfaceImpls)(HCORENUM *phEnum, mdTypeDef td,
                            mdInterfaceImpl rImpls[], ULONG cMax,
                            ULONG* pcImpls) PURE;
    STDMETHOD(EnumTypeRefs)(HCORENUM *phEnum, mdTypeRef rTypeRefs[],
                            ULONG cMax, ULONG* pcTypeRefs) PURE;

    STDMETHOD(FindTypeDefByName)(           // S_OK or error.
        LPCWSTR     szTypeDef,              // [IN] Name of the Type.
        mdToken     tkEnclosingClass,       // [IN] TypeDef/TypeRef for Enclosing class.
        mdTypeDef   *ptd) PURE;             // [OUT] Put the TypeDef token here.

    STDMETHOD(GetScopeProps)(               // S_OK or error.
      _Out_writes_to_opt_(cchName, *pchName)
        LPWSTR      szName,                 // [OUT] Put the name here.
        ULONG       cchName,                // [IN] Size of name buffer in wide chars.
        ULONG       *pchName,               // [OUT] Put size of name (wide chars) here.
        GUID        *pmvid) PURE;           // [OUT, OPTIONAL] Put MVID here.

    STDMETHOD(GetModuleFromScope)(          // S_OK.
        mdModule    *pmd) PURE;             // [OUT] Put mdModule token here.

    STDMETHOD(GetTypeDefProps)(             // S_OK or error.
        mdTypeDef   td,                     // [IN] TypeDef token for inquiry.
      _Out_writes_to_opt_(cchTypeDef, *pchTypeDef)
        LPWSTR      szTypeDef,              // [OUT] Put name here.
        ULONG       cchTypeDef,             // [IN] size of name buffer in wide chars.
        ULONG       *pchTypeDef,            // [OUT] put size of name (wide chars) here.
        DWORD       *pdwTypeDefFlags,       // [OUT] Put flags here.
        mdToken     *ptkExtends) PURE;      // [OUT] Put base class TypeDef/TypeRef here.

    STDMETHOD(GetInterfaceImplProps)(       // S_OK or error.
        mdInterfaceImpl iiImpl,             // [IN] InterfaceImpl token.
        mdTypeDef   *pClass,                // [OUT] Put implementing class token here.
        mdToken     *ptkIface) PURE;        // [OUT] Put implemented interface token here.
            
    STDMETHOD(GetTypeRefProps)(             // S_OK or error.
        mdTypeRef   tr,                     // [IN] TypeRef token.
        mdToken     *ptkResolutionScope,    // [OUT] Resolution scope, ModuleRef or AssemblyRef.
      _Out_writes_to_opt_(cchName, *pchName)
        LPWSTR      szName,                 // [OUT] Name of the TypeRef.
        ULONG       cchName,                // [IN] Size of buffer.
        ULONG       *pchName) PURE;         // [OUT] Size of Name.

    STDMETHOD(ResolveTypeRef)(mdTypeRef tr, REFIID riid, IUnknown **ppIScope, mdTypeDef *ptd) PURE;

    STDMETHOD(EnumMembers)(                 // S_OK, S_FALSE, or error. 
        HCORENUM    *phEnum,                // [IN|OUT] Pointer to the enum.
        mdTypeDef   cl,                     // [IN] TypeDef to scope the enumeration.
        mdToken     rMembers[],             // [OUT] Put MemberDefs here.
        ULONG       cMax,                   // [IN] Max MemberDefs to put.
        ULONG       *pcTokens) PURE;        // [OUT] Put # put here.

    STDMETHOD(EnumMembersWithName)(         // S_OK, S_FALSE, or error.
        HCORENUM    *phEnum,                // [IN|OUT] Pointer to the enum.
        mdTypeDef   cl,                     // [IN] TypeDef to scope the enumeration.
        LPCWSTR     szName,                 // [IN] Limit results to those with this name.
        mdToken     rMembers[],             // [OUT] Put MemberDefs here.
        ULONG       cMax,                   // [IN] Max MemberDefs to put.
        ULONG       *pcTokens) PURE;        // [OUT] Put # put here.

    STDMETHOD(EnumMethods)(                 // S_OK, S_FALSE, or error. 
        HCORENUM    *phEnum,                // [IN|OUT] Pointer to the enum.
        mdTypeDef   cl,                     // [IN] TypeDef to scope the enumeration.
        mdMethodDef rMethods[],             // [OUT] Put MethodDefs here.
        ULONG       cMax,                   // [IN] Max MethodDefs to put.
        ULONG       *pcTokens) PURE;        // [OUT] Put # put here.

    STDMETHOD(EnumMethodsWithName)(         // S_OK, S_FALSE, or error.
        HCORENUM    *phEnum,                // [IN|OUT] Pointer to the enum.
        mdTypeDef   cl,                     // [IN] TypeDef to scope the enumeration.
        LPCWSTR     szName,                 // [IN] Limit results to those with this name.
        mdMethodDef rMethods[],             // [OU] Put MethodDefs here.
        ULONG       cMax,                   // [IN] Max MethodDefs to put.
        ULONG       *pcTokens) PURE;        // [OUT] Put # put here.

    STDMETHOD(EnumFields)(                  // S_OK, S_FALSE, or error.
        HCORENUM    *phEnum,                // [IN|OUT] Pointer to the enum.
        mdTypeDef   cl,                     // [IN] TypeDef to scope the enumeration.
        mdFieldDef  rFields[],              // [OUT] Put FieldDefs here.
        ULONG       cMax,                   // [IN] Max FieldDefs to put.
        ULONG       *pcTokens) PURE;        // [OUT] Put # put here.

    STDMETHOD(EnumFieldsWithName)(          // S_OK, S_FALSE, or error.
        HCORENUM    *phEnum,                // [IN|OUT] Pointer to the enum.
        mdTypeDef   cl,                     // [IN] TypeDef to scope the enumeration.
        LPCWSTR     szName,                 // [IN] Limit results to those with this name.
        mdFieldDef  rFields[],              // [OUT] Put MemberDefs here.
        ULONG       cMax,                   // [IN] Max MemberDefs to put.
        ULONG       *pcTokens) PURE;        // [OUT] Put # put here.


    STDMETHOD(EnumParams)(                  // S_OK, S_FALSE, or error. 
        HCORENUM    *phEnum,                // [IN|OUT] Pointer to the enum.
        mdMethodDef mb,                     // [IN] MethodDef to scope the enumeration. 
        mdParamDef  rParams[],              // [OUT] Put ParamDefs here.
        ULONG       cMax,                   // [IN] Max ParamDefs to put.
        ULONG       *pcTokens) PURE;        // [OUT] Put # put here.

    STDMETHOD(EnumMemberRefs)(              // S_OK, S_FALSE, or error. 
        HCORENUM    *phEnum,                // [IN|OUT] Pointer to the enum.
        mdToken     tkParent,               // [IN] Parent token to scope the enumeration.
        mdMemberRef rMemberRefs[],          // [OUT] Put MemberRefs here.
        ULONG       cMax,                   // [IN] Max MemberRefs to put.
        ULONG       *pcTokens) PURE;        // [OUT] Put # put here.

    STDMETHOD(EnumMethodImpls)(             // S_OK, S_FALSE, or error
        HCORENUM    *phEnum,                // [IN|OUT] Pointer to the enum.
        mdTypeDef   td,                     // [IN] TypeDef to scope the enumeration.
        mdToken     rMethodBody[],          // [OUT] Put Method Body tokens here.
        mdToken     rMethodDecl[],          // [OUT] Put Method Declaration tokens here.
        ULONG       cMax,                   // [IN] Max tokens to put.
        ULONG       *pcTokens) PURE;        // [OUT] Put # put here.

    STDMETHOD(EnumPermissionSets)(          // S_OK, S_FALSE, or error. 
        HCORENUM    *phEnum,                // [IN|OUT] Pointer to the enum.
        mdToken     tk,                     // [IN] if !NIL, token to scope the enumeration.
        DWORD       dwActions,              // [IN] if !0, return only these actions.
        mdPermission rPermission[],         // [OUT] Put Permissions here.
        ULONG       cMax,                   // [IN] Max Permissions to put. 
        ULONG       *pcTokens) PURE;        // [OUT] Put # put here.

    STDMETHOD(FindMember)(
        mdTypeDef   td,                     // [IN] given typedef
        LPCWSTR     szName,                 // [IN] member name 
        PCCOR_SIGNATURE pvSigBlob,          // [IN] point to a blob value of CLR signature 
        ULONG       cbSigBlob,              // [IN] count of bytes in the signature blob
        mdToken     *pmb) PURE;             // [OUT] matching memberdef 

    STDMETHOD(FindMethod)(
        mdTypeDef   td,                     // [IN] given typedef
        LPCWSTR     szName,                 // [IN] member name 
        PCCOR_SIGNATURE pvSigBlob,          // [IN] point to a blob value of CLR signature 
        ULONG       cbSigBlob,              // [IN] count of bytes in the signature blob
        mdMethodDef *pmb) PURE;             // [OUT] matching memberdef 

    STDMETHOD(FindField)(
        mdTypeDef   td,                     // [IN] given typedef
        LPCWSTR     szName,                 // [IN] member name 
        PCCOR_SIGNATURE pvSigBlob,          // [IN] point to a blob value of CLR signature 
        ULONG       cbSigBlob,              // [IN] count of bytes in the signature blob
        mdFieldDef  *pmb) PURE;             // [OUT] matching memberdef 

    STDMETHOD(FindMemberRef)(
        mdTypeRef   td,                     // [IN] given typeRef
        LPCWSTR     szName,                 // [IN] member name 
        PCCOR_SIGNATURE pvSigBlob,          // [IN] point to a blob value of CLR signature 
        ULONG       cbSigBlob,              // [IN] count of bytes in the signature blob
        mdMemberRef *pmr) PURE;             // [OUT] matching memberref 

    STDMETHOD (GetMethodProps)( 
        mdMethodDef mb,                     // The method for which to get props.
        mdTypeDef   *pClass,                // Put method's class here. 
      _Out_writes_to_opt_(cchMethod, *pchMethod)
        LPWSTR      szMethod,               // Put method's name here.
        ULONG       cchMethod,              // Size of szMethod buffer in wide chars.
        ULONG       *pchMethod,             // Put actual size here 
        DWORD       *pdwAttr,               // Put flags here.
        PCCOR_SIGNATURE *ppvSigBlob,        // [OUT] point to the blob value of meta data
        ULONG       *pcbSigBlob,            // [OUT] actual size of signature blob
        ULONG       *pulCodeRVA,            // [OUT] codeRVA
        DWORD       *pdwImplFlags) PURE;    // [OUT] Impl. Flags

    STDMETHOD(GetMemberRefProps)(           // S_OK or error.
        mdMemberRef mr,                     // [IN] given memberref 
        mdToken     *ptk,                   // [OUT] Put classref or classdef here. 
      _Out_writes_to_opt_(cchMember, *pchMember)
        LPWSTR      szMember,               // [OUT] buffer to fill for member's name
        ULONG       cchMember,              // [IN] the count of char of szMember
        ULONG       *pchMember,             // [OUT] actual count of char in member name
        PCCOR_SIGNATURE *ppvSigBlob,        // [OUT] point to meta data blob value
        ULONG       *pbSig) PURE;           // [OUT] actual size of signature blob

    STDMETHOD(EnumProperties)(              // S_OK, S_FALSE, or error. 
        HCORENUM    *phEnum,                // [IN|OUT] Pointer to the enum.
        mdTypeDef   td,                     // [IN] TypeDef to scope the enumeration.
        mdProperty  rProperties[],          // [OUT] Put Properties here.
        ULONG       cMax,                   // [IN] Max properties to put.
        ULONG       *pcProperties) PURE;    // [OUT] Put # put here.

    STDMETHOD(EnumEvents)(                  // S_OK, S_FALSE, or error. 
        HCORENUM    *phEnum,                // [IN|OUT] Pointer to the enum.
        mdTypeDef   td,                     // [IN] TypeDef to scope the enumeration.
        mdEvent     rEvents[],              // [OUT] Put events here.
        ULONG       cMax,                   // [IN] Max events to put.
        ULONG       *pcEvents) PURE;        // [OUT] Put # put here.

    STDMETHOD(GetEventProps)(               // S_OK, S_FALSE, or error. 
        mdEvent     ev,                     // [IN] event token 
        mdTypeDef   *pClass,                // [OUT] typedef containing the event declarion.
        LPCWSTR     szEvent,                // [OUT] Event name 
        ULONG       cchEvent,               // [IN] the count of wchar of szEvent
        ULONG       *pchEvent,              // [OUT] actual count of wchar for event's name 
        DWORD       *pdwEventFlags,         // [OUT] Event flags.
        mdToken     *ptkEventType,          // [OUT] EventType class
        mdMethodDef *pmdAddOn,              // [OUT] AddOn method of the event
        mdMethodDef *pmdRemoveOn,           // [OUT] RemoveOn method of the event
        mdMethodDef *pmdFire,               // [OUT] Fire method of the event
        mdMethodDef rmdOtherMethod[],       // [OUT] other method of the event
        ULONG       cMax,                   // [IN] size of rmdOtherMethod
        ULONG       *pcOtherMethod) PURE;   // [OUT] total number of other method of this event 

    STDMETHOD(EnumMethodSemantics)(         // S_OK, S_FALSE, or error. 
        HCORENUM    *phEnum,                // [IN|OUT] Pointer to the enum.
        mdMethodDef mb,                     // [IN] MethodDef to scope the enumeration. 
        mdToken     rEventProp[],           // [OUT] Put Event/Property here.
        ULONG       cMax,                   // [IN] Max properties to put.
        ULONG       *pcEventProp) PURE;     // [OUT] Put # put here.

    STDMETHOD(GetMethodSemantics)(          // S_OK, S_FALSE, or error. 
        mdMethodDef mb,                     // [IN] method token
        mdToken     tkEventProp,            // [IN] event/property token.
        DWORD       *pdwSemanticsFlags) PURE; // [OUT] the role flags for the method/propevent pair 

    STDMETHOD(GetClassLayout) ( 
        mdTypeDef   td,                     // [IN] give typedef
        DWORD       *pdwPackSize,           // [OUT] 1, 2, 4, 8, or 16
        COR_FIELD_OFFSET rFieldOffset[],    // [OUT] field offset array 
        ULONG       cMax,                   // [IN] size of the array
        ULONG       *pcFieldOffset,         // [OUT] needed array size
        ULONG       *pulClassSize) PURE;        // [OUT] the size of the class

    STDMETHOD(GetFieldMarshal) (
        mdToken     tk,                     // [IN] given a field's memberdef
        PCCOR_SIGNATURE *ppvNativeType,     // [OUT] native type of this field
        ULONG       *pcbNativeType) PURE;   // [OUT] the count of bytes of *ppvNativeType

    STDMETHOD(GetRVA)(                      // S_OK or error.
        mdToken     tk,                     // Member for which to set offset
        ULONG       *pulCodeRVA,            // The offset
        DWORD       *pdwImplFlags) PURE;    // the implementation flags 

    STDMETHOD(GetPermissionSetProps) (
        mdPermission pm,                    // [IN] the permission token.
        DWORD       *pdwAction,             // [OUT] CorDeclSecurity.
        void const  **ppvPermission,        // [OUT] permission blob.
        ULONG       *pcbPermission) PURE;   // [OUT] count of bytes of pvPermission.

    STDMETHOD(GetSigFromToken)(             // S_OK or error.
        mdSignature mdSig,                  // [IN] Signature token.
        PCCOR_SIGNATURE *ppvSig,            // [OUT] return pointer to token.
        ULONG       *pcbSig) PURE;          // [OUT] return size of signature.

    STDMETHOD(GetModuleRefProps)(           // S_OK or error.
        mdModuleRef mur,                    // [IN] moduleref token.
      _Out_writes_to_opt_(cchName, *pchName)
        LPWSTR      szName,                 // [OUT] buffer to fill with the moduleref name.
        ULONG       cchName,                // [IN] size of szName in wide characters.
        ULONG       *pchName) PURE;         // [OUT] actual count of characters in the name.

    STDMETHOD(EnumModuleRefs)(              // S_OK or error.
        HCORENUM    *phEnum,                // [IN|OUT] pointer to the enum.
        mdModuleRef rModuleRefs[],          // [OUT] put modulerefs here.
        ULONG       cmax,                   // [IN] max memberrefs to put.
        ULONG       *pcModuleRefs) PURE;    // [OUT] put # put here.

    STDMETHOD(GetTypeSpecFromToken)(        // S_OK or error.
        mdTypeSpec typespec,                // [IN] TypeSpec token.
        PCCOR_SIGNATURE *ppvSig,            // [OUT] return pointer to TypeSpec signature
        ULONG       *pcbSig) PURE;          // [OUT] return size of signature.

    STDMETHOD(GetNameFromToken)(            // Not Recommended! May be removed!
        mdToken     tk,                     // [IN] Token to get name from.  Must have a name.
        MDUTF8CSTR  *pszUtf8NamePtr) PURE;  // [OUT] Return pointer to UTF8 name in heap.

    STDMETHOD(EnumUnresolvedMethods)(       // S_OK, S_FALSE, or error. 
        HCORENUM    *phEnum,                // [IN|OUT] Pointer to the enum.
        mdToken     rMethods[],             // [OUT] Put MemberDefs here.
        ULONG       cMax,                   // [IN] Max MemberDefs to put.
        ULONG       *pcTokens) PURE;        // [OUT] Put # put here.

    STDMETHOD(GetUserString)(               // S_OK or error.
        mdString    stk,                    // [IN] String token.
      _Out_writes_to_opt_(cchString, *pchString)
        LPWSTR      szString,               // [OUT] Copy of string.
        ULONG       cchString,              // [IN] Max chars of room in szString.
        ULONG       *pchString) PURE;       // [OUT] How many chars in actual string.

    STDMETHOD(GetPinvokeMap)(               // S_OK or error.
        mdToken     tk,                     // [IN] FieldDef or MethodDef.
        DWORD       *pdwMappingFlags,       // [OUT] Flags used for mapping.
      _Out_writes_to_opt_(cchImportName, *pchImportName)
        LPWSTR      szImportName,           // [OUT] Import name.
        ULONG       cchImportName,          // [IN] Size of the name buffer.
        ULONG       *pchImportName,         // [OUT] Actual number of characters stored.
        mdModuleRef *pmrImportDLL) PURE;    // [OUT] ModuleRef token for the target DLL.

    STDMETHOD(EnumSignatures)(              // S_OK or error.
        HCORENUM    *phEnum,                // [IN|OUT] pointer to the enum.
        mdSignature rSignatures[],          // [OUT] put signatures here.
        ULONG       cmax,                   // [IN] max signatures to put.
        ULONG       *pcSignatures) PURE;    // [OUT] put # put here.

    STDMETHOD(EnumTypeSpecs)(               // S_OK or error.
        HCORENUM    *phEnum,                // [IN|OUT] pointer to the enum.
        mdTypeSpec  rTypeSpecs[],           // [OUT] put TypeSpecs here.
        ULONG       cmax,                   // [IN] max TypeSpecs to put.
        ULONG       *pcTypeSpecs) PURE;     // [OUT] put # put here.

    STDMETHOD(EnumUserStrings)(             // S_OK or error.
        HCORENUM    *phEnum,                // [IN/OUT] pointer to the enum.
        mdString    rStrings[],             // [OUT] put Strings here.
        ULONG       cmax,                   // [IN] max Strings to put.
        ULONG       *pcStrings) PURE;       // [OUT] put # put here.

    STDMETHOD(GetParamForMethodIndex)(      // S_OK or error.
        mdMethodDef md,                     // [IN] Method token.
        ULONG       ulParamSeq,             // [IN] Parameter sequence.
        mdParamDef  *ppd) PURE;             // [IN] Put Param token here.

    STDMETHOD(EnumCustomAttributes)(        // S_OK or error.
        HCORENUM    *phEnum,                // [IN, OUT] COR enumerator.
        mdToken     tk,                     // [IN] Token to scope the enumeration, 0 for all.
        mdToken     tkType,                 // [IN] Type of interest, 0 for all.
        mdCustomAttribute rCustomAttributes[], // [OUT] Put custom attribute tokens here.
        ULONG       cMax,                   // [IN] Size of rCustomAttributes.
        ULONG       *pcCustomAttributes) PURE;  // [OUT, OPTIONAL] Put count of token values here.

    STDMETHOD(GetCustomAttributeProps)(     // S_OK or error.
        mdCustomAttribute cv,               // [IN] CustomAttribute token.
        mdToken     *ptkObj,                // [OUT, OPTIONAL] Put object token here.
        mdToken     *ptkType,               // [OUT, OPTIONAL] Put AttrType token here.
        void const  **ppBlob,               // [OUT, OPTIONAL] Put pointer to data here.
        ULONG       *pcbSize) PURE;         // [OUT, OPTIONAL] Put size of date here.

    STDMETHOD(FindTypeRef)(
        mdToken     tkResolutionScope,      // [IN] ModuleRef, AssemblyRef or TypeRef.
        LPCWSTR     szName,                 // [IN] TypeRef Name.
        mdTypeRef   *ptr) PURE;             // [OUT] matching TypeRef.

    STDMETHOD(GetMemberProps)(
        mdToken     mb,                     // The member for which to get props.
        mdTypeDef   *pClass,                // Put member's class here. 
      _Out_writes_to_opt_(cchMember, *pchMember)
        LPWSTR      szMember,               // Put member's name here.
        ULONG       cchMember,              // Size of szMember buffer in wide chars.
        ULONG       *pchMember,             // Put actual size here 
        DWORD       *pdwAttr,               // Put flags here.
        PCCOR_SIGNATURE *ppvSigBlob,        // [OUT] point to the blob value of meta data
        ULONG       *pcbSigBlob,            // [OUT] actual size of signature blob
        ULONG       *pulCodeRVA,            // [OUT] codeRVA
        DWORD       *pdwImplFlags,          // [OUT] Impl. Flags
        DWORD       *pdwCPlusTypeFlag,      // [OUT] flag for value type. selected ELEMENT_TYPE_*
        UVCP_CONSTANT *ppValue,             // [OUT] constant value 
        ULONG       *pcchValue) PURE;       // [OUT] size of constant string in chars, 0 for non-strings.

    STDMETHOD(GetFieldProps)(
        mdFieldDef  mb,                     // The field for which to get props.
        mdTypeDef   *pClass,                // Put field's class here.
      _Out_writes_to_opt_(cchField, *pchField)
        LPWSTR      szField,                // Put field's name here.
        ULONG       cchField,               // Size of szField buffer in wide chars.
        ULONG       *pchField,              // Put actual size here 
        DWORD       *pdwAttr,               // Put flags here.
        PCCOR_SIGNATURE *ppvSigBlob,        // [OUT] point to the blob value of meta data
        ULONG       *pcbSigBlob,            // [OUT] actual size of signature blob
        DWORD       *pdwCPlusTypeFlag,      // [OUT] flag for value type. selected ELEMENT_TYPE_*
        UVCP_CONSTANT *ppValue,             // [OUT] constant value 
        ULONG       *pcchValue) PURE;       // [OUT] size of constant string in chars, 0 for non-strings.

    STDMETHOD(GetPropertyProps)(            // S_OK, S_FALSE, or error. 
        mdProperty  prop,                   // [IN] property token
        mdTypeDef   *pClass,                // [OUT] typedef containing the property declarion. 
        LPCWSTR     szProperty,             // [OUT] Property name
        ULONG       cchProperty,            // [IN] the count of wchar of szProperty
        ULONG       *pchProperty,           // [OUT] actual count of wchar for property name
        DWORD       *pdwPropFlags,          // [OUT] property flags.
        PCCOR_SIGNATURE *ppvSig,            // [OUT] property type. pointing to meta data internal blob 
        ULONG       *pbSig,                 // [OUT] count of bytes in *ppvSig
        DWORD       *pdwCPlusTypeFlag,      // [OUT] flag for value type. selected ELEMENT_TYPE_*
        UVCP_CONSTANT *ppDefaultValue,      // [OUT] constant value 
        ULONG       *pcchDefaultValue,      // [OUT] size of constant string in chars, 0 for non-strings.
        mdMethodDef *pmdSetter,             // [OUT] setter method of the property
        mdMethodDef *pmdGetter,             // [OUT] getter method of the property
        mdMethodDef rmdOtherMethod[],       // [OUT] other method of the property
        ULONG       cMax,                   // [IN] size of rmdOtherMethod
        ULONG       *pcOtherMethod) PURE;   // [OUT] total number of other method of this property

    STDMETHOD(GetParamProps)(               // S_OK or error.
        mdParamDef  tk,                     // [IN]The Parameter.
        mdMethodDef *pmd,                   // [OUT] Parent Method token.
        ULONG       *pulSequence,           // [OUT] Parameter sequence.
      _Out_writes_to_opt_(cchName, *pchName)
        LPWSTR      szName,                 // [OUT] Put name here.
        ULONG       cchName,                // [OUT] Size of name buffer.
        ULONG       *pchName,               // [OUT] Put actual size of name here.
        DWORD       *pdwAttr,               // [OUT] Put flags here.
        DWORD       *pdwCPlusTypeFlag,      // [OUT] Flag for value type. selected ELEMENT_TYPE_*.
        UVCP_CONSTANT *ppValue,             // [OUT] Constant value.
        ULONG       *pcchValue) PURE;       // [OUT] size of constant string in chars, 0 for non-strings.

    STDMETHOD(GetCustomAttributeByName)(    // S_OK or error.
        mdToken     tkObj,                  // [IN] Object with Custom Attribute.
        LPCWSTR     szName,                 // [IN] Name of desired Custom Attribute.
        const void  **ppData,               // [OUT] Put pointer to data here.
        ULONG       *pcbData) PURE;         // [OUT] Put size of data here.

    STDMETHOD_(BOOL, IsValidToken)(         // True or False.
        mdToken     tk) PURE;               // [IN] Given token.

    STDMETHOD(GetNestedClassProps)(         // S_OK or error.
        mdTypeDef   tdNestedClass,          // [IN] NestedClass token.
        mdTypeDef   *ptdEnclosingClass) PURE; // [OUT] EnclosingClass token.

    STDMETHOD(GetNativeCallConvFromSig)(    // S_OK or error.
        void const  *pvSig,                 // [IN] Pointer to signature.
        ULONG       cbSig,                  // [IN] Count of signature bytes.
        ULONG       *pCallConv) PURE;       // [OUT] Put calling conv here (see CorPinvokemap).

    STDMETHOD(IsGlobal)(                    // S_OK or error.
        mdToken     pd,                     // [IN] Type, Field, or Method token.
        int         *pbGlobal) PURE;        // [OUT] Put 1 if global, 0 otherwise.

    // This interface is sealed.  Do not change, add, or remove anything.  Instead, derive a new iterface.

};      // IMetaDataImport

//-------------------------------------
//--- IMetaDataImport2
//-------------------------------------
// {FCE5EFA0-8BBA-4f8e-A036-8F2022B08466}
EXTERN_GUID(IID_IMetaDataImport2, 0xfce5efa0, 0x8bba, 0x4f8e, 0xa0, 0x36, 0x8f, 0x20, 0x22, 0xb0, 0x84, 0x66);

//---
#undef  INTERFACE
#define INTERFACE IMetaDataImport2
DECLARE_INTERFACE_(IMetaDataImport2, IMetaDataImport)
{
    STDMETHOD(EnumGenericParams)(
        HCORENUM    *phEnum,                // [IN|OUT] Pointer to the enum.
        mdToken      tk,                    // [IN] TypeDef or MethodDef whose generic parameters are requested
        mdGenericParam rGenericParams[],    // [OUT] Put GenericParams here.
        ULONG       cMax,                   // [IN] Max GenericParams to put.
        ULONG       *pcGenericParams) PURE; // [OUT] Put # put here.

    STDMETHOD(GetGenericParamProps)(        // S_OK or error.
        mdGenericParam gp,                  // [IN] GenericParam
        ULONG        *pulParamSeq,          // [OUT] Index of the type parameter
        DWORD        *pdwParamFlags,        // [OUT] Flags, for future use (e.g. variance)
        mdToken      *ptOwner,              // [OUT] Owner (TypeDef or MethodDef)
        DWORD       *reserved,              // [OUT] For future use (e.g. non-type parameters)
      _Out_writes_to_opt_(cchName, *pchName)
        LPWSTR       wzname,                // [OUT] Put name here
        ULONG        cchName,               // [IN] Size of buffer
        ULONG        *pchName) PURE;        // [OUT] Put size of name (wide chars) here.

    STDMETHOD(GetMethodSpecProps)(
        mdMethodSpec mi,                    // [IN] The method instantiation
        mdToken *tkParent,                  // [OUT] MethodDef or MemberRef
        PCCOR_SIGNATURE *ppvSigBlob,        // [OUT] point to the blob value of meta data
        ULONG       *pcbSigBlob) PURE;      // [OUT] actual size of signature blob

    STDMETHOD(EnumGenericParamConstraints)(
        HCORENUM    *phEnum,                // [IN|OUT] Pointer to the enum.
        mdGenericParam tk,                  // [IN] GenericParam whose constraints are requested
        mdGenericParamConstraint rGenericParamConstraints[],    // [OUT] Put GenericParamConstraints here.
        ULONG       cMax,                   // [IN] Max GenericParamConstraints to put.
        ULONG       *pcGenericParamConstraints) PURE; // [OUT] Put # put here.

    STDMETHOD(GetGenericParamConstraintProps)( // S_OK or error.
        mdGenericParamConstraint gpc,       // [IN] GenericParamConstraint
        mdGenericParam *ptGenericParam,     // [OUT] GenericParam that is constrained
        mdToken      *ptkConstraintType) PURE; // [OUT] TypeDef/Ref/Spec constraint

    STDMETHOD(GetPEKind)(                   // S_OK or error.
        DWORD* pdwPEKind,                   // [OUT] The kind of PE (0 - not a PE)
        DWORD* pdwMAchine) PURE;            // [OUT] Machine as defined in NT header

    STDMETHOD(GetVersionString)(            // S_OK or error.
      _Out_writes_to_opt_(ccBufSize, *pccBufSize)
        LPWSTR      pwzBuf,                 // [OUT] Put version string here.
        DWORD       ccBufSize,              // [IN] size of the buffer, in wide chars
        DWORD       *pccBufSize) PURE;      // [OUT] Size of the version string, wide chars, including terminating nul.

    STDMETHOD(EnumMethodSpecs)(
        HCORENUM    *phEnum,                // [IN|OUT] Pointer to the enum.
        mdToken      tk,                    // [IN] MethodDef or MemberRef whose MethodSpecs are requested
        mdMethodSpec rMethodSpecs[],        // [OUT] Put MethodSpecs here.
        ULONG       cMax,                   // [IN] Max tokens to put.
        ULONG       *pcMethodSpecs) PURE;   // [OUT] Put actual count here.

}; // IMetaDataImport2

//-------------------------------------
//--- IMetaDataFilter
//-------------------------------------
// {D0E80DD1-12D4-11d3-B39D-00C04FF81795}
EXTERN_GUID(IID_IMetaDataFilter, 0xd0e80dd1, 0x12d4, 0x11d3, 0xb3, 0x9d, 0x0, 0xc0, 0x4f, 0xf8, 0x17, 0x95);

//---
#undef  INTERFACE
#define INTERFACE IMetaDataFilter
DECLARE_INTERFACE_(IMetaDataFilter, IUnknown)
{
    STDMETHOD(UnmarkAll)() PURE;
    STDMETHOD(MarkToken)(mdToken tk) PURE;
    STDMETHOD(IsTokenMarked)(mdToken tk, BOOL *pIsMarked) PURE;
};


//-------------------------------------
//--- IHostFilter
//-------------------------------------
// {D0E80DD3-12D4-11d3-B39D-00C04FF81795}
EXTERN_GUID(IID_IHostFilter, 0xd0e80dd3, 0x12d4, 0x11d3, 0xb3, 0x9d, 0x0, 0xc0, 0x4f, 0xf8, 0x17, 0x95);

//---
#undef  INTERFACE
#define INTERFACE IHostFilter
DECLARE_INTERFACE_(IHostFilter, IUnknown)
{
    STDMETHOD(MarkToken)(mdToken tk) PURE;
};


//*****************************************************************************
// Assembly Declarations
//*****************************************************************************

typedef struct
{
    DWORD       dwOSPlatformId;         // Operating system platform.
    DWORD       dwOSMajorVersion;       // OS Major version.
    DWORD       dwOSMinorVersion;       // OS Minor version.
} OSINFO;


typedef struct
{
    USHORT      usMajorVersion;         // Major Version.
    USHORT      usMinorVersion;         // Minor Version.
    USHORT      usBuildNumber;          // Build Number.
    USHORT      usRevisionNumber;       // Revision Number.
    LPWSTR      szLocale;               // Locale.
    ULONG       cbLocale;               // [IN/OUT] Size of the buffer in wide chars/Actual size.
    DWORD       *rProcessor;            // Processor ID array.
    ULONG       ulProcessor;            // [IN/OUT] Size of the Processor ID array/Actual # of entries filled in.
    OSINFO      *rOS;                   // OSINFO array.
    ULONG       ulOS;                   // [IN/OUT]Size of the OSINFO array/Actual # of entries filled in.
} ASSEMBLYMETADATA;


// {211EF15B-5317-4438-B196-DEC87B887693}
EXTERN_GUID(IID_IMetaDataAssemblyEmit, 0x211ef15b, 0x5317, 0x4438, 0xb1, 0x96, 0xde, 0xc8, 0x7b, 0x88, 0x76, 0x93);

//---
#undef  INTERFACE
#define INTERFACE IMetaDataAssemblyEmit
DECLARE_INTERFACE_(IMetaDataAssemblyEmit, IUnknown)
{
    STDMETHOD(DefineAssembly)(              // S_OK or error.
        const void  *pbPublicKey,           // [IN] Public key of the assembly.
        ULONG       cbPublicKey,            // [IN] Count of bytes in the public key.
        ULONG       ulHashAlgId,            // [IN] Hash algorithm used to hash the files.
        LPCWSTR     szName,                 // [IN] Name of the assembly.
        const ASSEMBLYMETADATA *pMetaData,  // [IN] Assembly MetaData.
        DWORD       dwAssemblyFlags,        // [IN] Flags.
        mdAssembly  *pma) PURE;             // [OUT] Returned Assembly token.

    STDMETHOD(DefineAssemblyRef)(           // S_OK or error.
        const void  *pbPublicKeyOrToken,    // [IN] Public key or token of the assembly.
        ULONG       cbPublicKeyOrToken,     // [IN] Count of bytes in the public key or token.
        LPCWSTR     szName,                 // [IN] Name of the assembly being referenced.
        const ASSEMBLYMETADATA *pMetaData,  // [IN] Assembly MetaData.
        const void  *pbHashValue,           // [IN] Hash Blob.
        ULONG       cbHashValue,            // [IN] Count of bytes in the Hash Blob.
        DWORD       dwAssemblyRefFlags,     // [IN] Flags.
        mdAssemblyRef *pmdar) PURE;         // [OUT] Returned AssemblyRef token.

    STDMETHOD(DefineFile)(                  // S_OK or error.
        LPCWSTR     szName,                 // [IN] Name of the file.
        const void  *pbHashValue,           // [IN] Hash Blob.
        ULONG       cbHashValue,            // [IN] Count of bytes in the Hash Blob.
        DWORD       dwFileFlags,            // [IN] Flags.
        mdFile      *pmdf) PURE;            // [OUT] Returned File token.

    STDMETHOD(DefineExportedType)(          // S_OK or error.
        LPCWSTR     szName,                 // [IN] Name of the Com Type.
        mdToken     tkImplementation,       // [IN] mdFile or mdAssemblyRef or mdExportedType
        mdTypeDef   tkTypeDef,              // [IN] TypeDef token within the file.
        DWORD       dwExportedTypeFlags,    // [IN] Flags.
        mdExportedType   *pmdct) PURE;      // [OUT] Returned ExportedType token.

    STDMETHOD(DefineManifestResource)(      // S_OK or error.
        LPCWSTR     szName,                 // [IN] Name of the resource.
        mdToken     tkImplementation,       // [IN] mdFile or mdAssemblyRef that provides the resource.
        DWORD       dwOffset,               // [IN] Offset to the beginning of the resource within the file.
        DWORD       dwResourceFlags,        // [IN] Flags.
        mdManifestResource  *pmdmr) PURE;   // [OUT] Returned ManifestResource token.

    STDMETHOD(SetAssemblyProps)(            // S_OK or error.
        mdAssembly  pma,                    // [IN] Assembly token.
        const void  *pbPublicKey,           // [IN] Public key of the assembly.
        ULONG       cbPublicKey,            // [IN] Count of bytes in the public key.
        ULONG       ulHashAlgId,            // [IN] Hash algorithm used to hash the files.
        LPCWSTR     szName,                 // [IN] Name of the assembly.
        const ASSEMBLYMETADATA *pMetaData,  // [IN] Assembly MetaData.
        DWORD       dwAssemblyFlags) PURE;  // [IN] Flags.

    STDMETHOD(SetAssemblyRefProps)(         // S_OK or error.
        mdAssemblyRef ar,                   // [IN] AssemblyRefToken.
        const void  *pbPublicKeyOrToken,    // [IN] Public key or token of the assembly.
        ULONG       cbPublicKeyOrToken,     // [IN] Count of bytes in the public key or token.
        LPCWSTR     szName,                 // [IN] Name of the assembly being referenced.
        const ASSEMBLYMETADATA *pMetaData,  // [IN] Assembly MetaData.
        const void  *pbHashValue,           // [IN] Hash Blob.
        ULONG       cbHashValue,            // [IN] Count of bytes in the Hash Blob.
        DWORD       dwAssemblyRefFlags) PURE; // [IN] Token for Execution Location.

    STDMETHOD(SetFileProps)(                // S_OK or error.
        mdFile      file,                   // [IN] File token.
        const void  *pbHashValue,           // [IN] Hash Blob.
        ULONG       cbHashValue,            // [IN] Count of bytes in the Hash Blob.
        DWORD       dwFileFlags) PURE;      // [IN] Flags.

    STDMETHOD(SetExportedTypeProps)(        // S_OK or error.
        mdExportedType   ct,                // [IN] ExportedType token.
        mdToken     tkImplementation,       // [IN] mdFile or mdAssemblyRef or mdExportedType.
        mdTypeDef   tkTypeDef,              // [IN] TypeDef token within the file.
        DWORD       dwExportedTypeFlags) PURE;   // [IN] Flags.

    STDMETHOD(SetManifestResourceProps)(    // S_OK or error.
        mdManifestResource  mr,             // [IN] ManifestResource token.
        mdToken     tkImplementation,       // [IN] mdFile or mdAssemblyRef that provides the resource.
        DWORD       dwOffset,               // [IN] Offset to the beginning of the resource within the file.
        DWORD       dwResourceFlags) PURE;  // [IN] Flags.

};  // IMetaDataAssemblyEmit


// {EE62470B-E94B-424e-9B7C-2F00C9249F93}
EXTERN_GUID(IID_IMetaDataAssemblyImport, 0xee62470b, 0xe94b, 0x424e, 0x9b, 0x7c, 0x2f, 0x0, 0xc9, 0x24, 0x9f, 0x93);

//---
#undef  INTERFACE
#define INTERFACE IMetaDataAssemblyImport
DECLARE_INTERFACE_(IMetaDataAssemblyImport, IUnknown)
{
    STDMETHOD(GetAssemblyProps)(            // S_OK or error.
        mdAssembly  mda,                    // [IN] The Assembly for which to get the properties.
        const void  **ppbPublicKey,         // [OUT] Pointer to the public key.
        ULONG       *pcbPublicKey,          // [OUT] Count of bytes in the public key.
        ULONG       *pulHashAlgId,          // [OUT] Hash Algorithm.
        _Out_writes_to_opt_(cchName, *pchName) LPWSTR  szName, // [OUT] Buffer to fill with assembly's simply name.
        ULONG       cchName,                // [IN] Size of buffer in wide chars.
        ULONG       *pchName,               // [OUT] Actual # of wide chars in name.
        ASSEMBLYMETADATA *pMetaData,        // [OUT] Assembly MetaData.
        DWORD       *pdwAssemblyFlags) PURE;    // [OUT] Flags.

    STDMETHOD(GetAssemblyRefProps)(         // S_OK or error.
        mdAssemblyRef mdar,                 // [IN] The AssemblyRef for which to get the properties.
        const void  **ppbPublicKeyOrToken,  // [OUT] Pointer to the public key or token.
        ULONG       *pcbPublicKeyOrToken,   // [OUT] Count of bytes in the public key or token.
        _Out_writes_to_opt_(cchName, *pchName)LPWSTR szName, // [OUT] Buffer to fill with name.
        ULONG       cchName,                // [IN] Size of buffer in wide chars.
        ULONG       *pchName,               // [OUT] Actual # of wide chars in name.
        ASSEMBLYMETADATA *pMetaData,        // [OUT] Assembly MetaData.
        const void  **ppbHashValue,         // [OUT] Hash blob.
        ULONG       *pcbHashValue,          // [OUT] Count of bytes in the hash blob.
        DWORD       *pdwAssemblyRefFlags) PURE; // [OUT] Flags.

    STDMETHOD(GetFileProps)(                // S_OK or error.
        mdFile      mdf,                    // [IN] The File for which to get the properties.
        _Out_writes_to_opt_(cchName, *pchName) LPWSTR      szName, // [OUT] Buffer to fill with name.
        ULONG       cchName,                // [IN] Size of buffer in wide chars.
        ULONG       *pchName,               // [OUT] Actual # of wide chars in name.
        const void  **ppbHashValue,         // [OUT] Pointer to the Hash Value Blob.
        ULONG       *pcbHashValue,          // [OUT] Count of bytes in the Hash Value Blob.
        DWORD       *pdwFileFlags) PURE;    // [OUT] Flags.

    STDMETHOD(GetExportedTypeProps)(        // S_OK or error.
        mdExportedType   mdct,              // [IN] The ExportedType for which to get the properties.
        _Out_writes_to_opt_(cchName, *pchName) LPWSTR      szName, // [OUT] Buffer to fill with name.
        ULONG       cchName,                // [IN] Size of buffer in wide chars.
        ULONG       *pchName,               // [OUT] Actual # of wide chars in name.
        mdToken     *ptkImplementation,     // [OUT] mdFile or mdAssemblyRef or mdExportedType.
        mdTypeDef   *ptkTypeDef,            // [OUT] TypeDef token within the file.
        DWORD       *pdwExportedTypeFlags) PURE; // [OUT] Flags.

    STDMETHOD(GetManifestResourceProps)(    // S_OK or error.
        mdManifestResource  mdmr,           // [IN] The ManifestResource for which to get the properties.
        _Out_writes_to_opt_(cchName, *pchName)LPWSTR      szName,  // [OUT] Buffer to fill with name.
        ULONG       cchName,                // [IN] Size of buffer in wide chars.
        ULONG       *pchName,               // [OUT] Actual # of wide chars in name.
        mdToken     *ptkImplementation,     // [OUT] mdFile or mdAssemblyRef that provides the ManifestResource.
        DWORD       *pdwOffset,             // [OUT] Offset to the beginning of the resource within the file.
        DWORD       *pdwResourceFlags) PURE;// [OUT] Flags.

    STDMETHOD(EnumAssemblyRefs)(            // S_OK or error
        HCORENUM    *phEnum,                // [IN|OUT] Pointer to the enum.
        mdAssemblyRef rAssemblyRefs[],      // [OUT] Put AssemblyRefs here.
        ULONG       cMax,                   // [IN] Max AssemblyRefs to put.
        ULONG       *pcTokens) PURE;        // [OUT] Put # put here.

    STDMETHOD(EnumFiles)(                   // S_OK or error
        HCORENUM    *phEnum,                // [IN|OUT] Pointer to the enum.
        mdFile      rFiles[],               // [OUT] Put Files here.
        ULONG       cMax,                   // [IN] Max Files to put.
        ULONG       *pcTokens) PURE;        // [OUT] Put # put here.

    STDMETHOD(EnumExportedTypes)(           // S_OK or error
        HCORENUM    *phEnum,                // [IN|OUT] Pointer to the enum.
        mdExportedType   rExportedTypes[],  // [OUT] Put ExportedTypes here.
        ULONG       cMax,                   // [IN] Max ExportedTypes to put.
        ULONG       *pcTokens) PURE;        // [OUT] Put # put here.

    STDMETHOD(EnumManifestResources)(       // S_OK or error
        HCORENUM    *phEnum,                // [IN|OUT] Pointer to the enum.
        mdManifestResource  rManifestResources[],   // [OUT] Put ManifestResources here.
        ULONG       cMax,                   // [IN] Max Resources to put.
        ULONG       *pcTokens) PURE;        // [OUT] Put # put here.

    STDMETHOD(GetAssemblyFromScope)(        // S_OK or error
        mdAssembly  *ptkAssembly) PURE;     // [OUT] Put token here.

    STDMETHOD(FindExportedTypeByName)(      // S_OK or error
        LPCWSTR     szName,                 // [IN] Name of the ExportedType.
        mdToken     mdtExportedType,        // [IN] ExportedType for the enclosing class.
        mdExportedType   *ptkExportedType) PURE; // [OUT] Put the ExportedType token here.

    STDMETHOD(FindManifestResourceByName)(  // S_OK or error
        LPCWSTR     szName,                 // [IN] Name of the ManifestResource.
        mdManifestResource *ptkManifestResource) PURE;  // [OUT] Put the ManifestResource token here.

    STDMETHOD_(void, CloseEnum)(
        HCORENUM hEnum) PURE;               // Enum to be closed.

    STDMETHOD(FindAssembliesByName)(        // S_OK or error
        LPCWSTR  szAppBase,                 // [IN] optional - can be NULL
        LPCWSTR  szPrivateBin,              // [IN] optional - can be NULL
        LPCWSTR  szAssemblyName,            // [IN] required - this is the assembly you are requesting
        IUnknown *ppIUnk[],                 // [OUT] put IMetaDataAssemblyImport pointers here
        ULONG    cMax,                      // [IN] The max number to put
        ULONG    *pcAssemblies) PURE;       // [OUT] The number of assemblies returned.
};  // IMetaDataAssemblyImport


//*****************************************************************************
// End Assembly Declarations
//*****************************************************************************

//*****************************************************************************
// MetaData Validator Declarations
//*****************************************************************************

// Specifies the type of the module, PE file vs. .obj file.
typedef enum
{
    ValidatorModuleTypeInvalid      = 0x0,
    ValidatorModuleTypeMin          = 0x00000001,
    ValidatorModuleTypePE           = 0x00000001,
    ValidatorModuleTypeObj          = 0x00000002,
    ValidatorModuleTypeEnc          = 0x00000003,
    ValidatorModuleTypeIncr         = 0x00000004,
    ValidatorModuleTypeMax          = 0x00000004,
} CorValidatorModuleType;


// {4709C9C6-81FF-11D3-9FC7-00C04F79A0A3}
EXTERN_GUID(IID_IMetaDataValidate, 0x4709c9c6, 0x81ff, 0x11d3, 0x9f, 0xc7, 0x0, 0xc0, 0x4f, 0x79, 0xa0, 0xa3);

//---
#undef  INTERFACE
#define INTERFACE IMetaDataValidate
DECLARE_INTERFACE_(IMetaDataValidate, IUnknown)
{
    STDMETHOD(ValidatorInit)(               // S_OK or error.
        DWORD       dwModuleType,           // [IN] Specifies the type of the module.
        IUnknown    *pUnk) PURE;            // [IN] Validation error handler.

    STDMETHOD(ValidateMetaData)(            // S_OK or error.
        ) PURE;
};  // IMetaDataValidate

//*****************************************************************************
// End MetaData Validator Declarations
//*****************************************************************************

//*****************************************************************************
// IMetaDataDispenserEx declarations.
//*****************************************************************************

// {31BCFCE2-DAFB-11D2-9F81-00C04F79A0A3}
EXTERN_GUID(IID_IMetaDataDispenserEx, 0x31bcfce2, 0xdafb, 0x11d2, 0x9f, 0x81, 0x0, 0xc0, 0x4f, 0x79, 0xa0, 0xa3);

#undef  INTERFACE
#define INTERFACE IMetaDataDispenserEx
DECLARE_INTERFACE_(IMetaDataDispenserEx, IMetaDataDispenser)
{
    STDMETHOD(SetOption)(                   // Return code.
        REFGUID     optionid,               // [in] GUID for the option to be set.
        const VARIANT *value) PURE;         // [in] Value to which the option is to be set.

    STDMETHOD(GetOption)(                   // Return code.
        REFGUID     optionid,               // [in] GUID for the option to be set.
        VARIANT *pvalue) PURE;              // [out] Value to which the option is currently set.

    STDMETHOD(OpenScopeOnITypeInfo)(        // Return code.
        ITypeInfo   *pITI,                  // [in] ITypeInfo to open.
        DWORD       dwOpenFlags,            // [in] Open mode flags.
        REFIID      riid,                   // [in] The interface desired.
        IUnknown    **ppIUnk) PURE;         // [out] Return interface on success.

    STDMETHOD(GetCORSystemDirectory)(       // Return code.
       _Out_writes_to_opt_(cchBuffer, *pchBuffer)
         LPWSTR      szBuffer,              // [out] Buffer for the directory name
         DWORD       cchBuffer,             // [in] Size of the buffer
         DWORD*      pchBuffer) PURE;       // [OUT] Number of characters returned

    STDMETHOD(FindAssembly)(                // S_OK or error
        LPCWSTR  szAppBase,                 // [IN] optional - can be NULL
        LPCWSTR  szPrivateBin,              // [IN] optional - can be NULL
        LPCWSTR  szGlobalBin,               // [IN] optional - can be NULL
        LPCWSTR  szAssemblyName,            // [IN] required - this is the assembly you are requesting
        LPCWSTR  szName,                    // [OUT] buffer - to hold name 
        ULONG    cchName,                   // [IN] the name buffer's size
        ULONG    *pcName) PURE;             // [OUT] the number of characters returend in the buffer

    STDMETHOD(FindAssemblyModule)(          // S_OK or error
        LPCWSTR  szAppBase,                 // [IN] optional - can be NULL
        LPCWSTR  szPrivateBin,              // [IN] optional - can be NULL
        LPCWSTR  szGlobalBin,               // [IN] optional - can be NULL
        LPCWSTR  szAssemblyName,            // [IN] required - this is the assembly you are requesting
        LPCWSTR  szModuleName,              // [IN] required - the name of the module
      _Out_writes_to_opt_(cchName, *pcName)
        LPWSTR   szName,                    // [OUT] buffer - to hold name 
        ULONG    cchName,                   // [IN]  the name buffer's size
        ULONG    *pcName) PURE;             // [OUT] the number of characters returend in the buffer

};

//*****************************************************************************
//*****************************************************************************
//
// Registration declarations.  Will be replace by Services' Registration
//  implementation. 
//
//*****************************************************************************
//*****************************************************************************
// Various flags for use in installing a module or a composite
typedef enum 
{
    regNoCopy = 0x00000001,         // Don't copy files into destination
    regConfig = 0x00000002,         // Is a configuration
    regHasRefs = 0x00000004         // Has class references 
} CorRegFlags;

typedef GUID CVID;

typedef struct {
    short Major;
    short Minor;
    short Sub;
    short Build;
} CVStruct;


//*****************************************************************************
//*****************************************************************************
//
// CeeGen interfaces for generating in-memory Common Language Runtime files
//
//*****************************************************************************
//*****************************************************************************

typedef void *HCEESECTION;

typedef enum  {
    sdNone =        0,
    sdReadOnly =    IMAGE_SCN_MEM_READ | IMAGE_SCN_CNT_INITIALIZED_DATA,
    sdReadWrite =   sdReadOnly | IMAGE_SCN_MEM_WRITE,
    sdExecute =     IMAGE_SCN_MEM_READ | IMAGE_SCN_CNT_CODE | IMAGE_SCN_MEM_EXECUTE
} CeeSectionAttr;

//
// Relocation types.
//

typedef enum  {
    // generate only a section-relative reloc, nothing into .reloc section
    srRelocAbsolute,

    // generate a .reloc for a pointer sized location, 
    // This is transformed into BASED_HIGHLOW or BASED_DIR64 based on the platform
    srRelocHighLow      = 3,

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
    srRelocAbsolutePtr  = srRelocPtr + srRelocAbsolute,
    srRelocHighLowPtr   = srRelocPtr + srRelocHighLow,
    srRelocRelativePtr  = srRelocPtr + srRelocRelative,
    srRelocIA64Imm64Ptr = srRelocPtr + srRelocIA64Imm64,
    srRelocDir64Ptr     = srRelocPtr + srRelocDir64,

} CeeSectionRelocType;

typedef union  {
    USHORT highAdj;
} CeeSectionRelocExtra;

//-------------------------------------
//--- ICeeGen
//-------------------------------------
// {7ED1BDFF-8E36-11d2-9C56-00A0C9B7CC45}
EXTERN_GUID(IID_ICeeGen, 0x7ed1bdff, 0x8e36, 0x11d2, 0x9c, 0x56, 0x0, 0xa0, 0xc9, 0xb7, 0xcc, 0x45);

DECLARE_INTERFACE_(ICeeGen, IUnknown)
{
    STDMETHOD (EmitString) (
      _In_
        LPWSTR lpString,                    // [IN] String to emit
        ULONG *RVA) PURE;                   // [OUT] RVA for string emitted string

    STDMETHOD (GetString) (
        ULONG RVA,                          // [IN] RVA for string to return
      _Out_opt_
        LPWSTR *lpString) PURE;             // [OUT] Returned string

    STDMETHOD (AllocateMethodBuffer) (
        ULONG cchBuffer,                    // [IN] Length of buffer to create
        UCHAR **lpBuffer,                   // [OUT] Returned buffer
        ULONG *RVA) PURE;                   // [OUT] RVA for method 

    STDMETHOD (GetMethodBuffer) (
        ULONG RVA,                          // [IN] RVA for method to return
        UCHAR **lpBuffer) PURE;             // [OUT] Returned buffer

    STDMETHOD (GetIMapTokenIface) (
        IUnknown **pIMapToken) PURE;

    STDMETHOD (GenerateCeeFile) () PURE;

    STDMETHOD (GetIlSection) (
        HCEESECTION *section) PURE; 

    STDMETHOD (GetStringSection) (
        HCEESECTION *section) PURE; 

    STDMETHOD (AddSectionReloc) (
        HCEESECTION section, 
        ULONG offset, 
        HCEESECTION relativeTo, 
        CeeSectionRelocType relocType) PURE;

    // use these only if you have special section requirements not handled
    // by other APIs
    STDMETHOD (GetSectionCreate) (
        const char *name, 
        DWORD flags, 
        HCEESECTION *section) PURE; 

    STDMETHOD (GetSectionDataLen) (
        HCEESECTION section, 
        ULONG *dataLen) PURE;

    STDMETHOD (GetSectionBlock) (
        HCEESECTION section, 
        ULONG len, 
        ULONG align=1, 
        void **ppBytes=0) PURE; 

    STDMETHOD (TruncateSection) (
        HCEESECTION section, 
        ULONG len) PURE;

    STDMETHOD (GenerateCeeMemoryImage) (
        void **ppImage) PURE;

    STDMETHOD (ComputePointer) (
        HCEESECTION section, 
        ULONG RVA,                          // [IN] RVA for method to return
        UCHAR **lpBuffer) PURE;             // [OUT] Returned buffer

};

//*****************************************************************************
//*****************************************************************************
//
// End of CeeGen declarations.
//
//*****************************************************************************

//**********************************************************************
//**********************************************************************
//--- IMetaDataTables
//-------------------------------------
// This API isn't big endian friendly since it indexes directly into the memory that
// is stored in little endian format.
// {D8F579AB-402D-4b8e-82D9-5D63B1065C68}
EXTERN_GUID(IID_IMetaDataTables, 0xd8f579ab, 0x402d, 0x4b8e, 0x82, 0xd9, 0x5d, 0x63, 0xb1, 0x6, 0x5c, 0x68);

DECLARE_INTERFACE_(IMetaDataTables, IUnknown)
{
    STDMETHOD (GetStringHeapSize) (
        ULONG   *pcbStrings) PURE;          // [OUT] Size of the string heap.

    STDMETHOD (GetBlobHeapSize) (
        ULONG   *pcbBlobs) PURE;            // [OUT] Size of the Blob heap.

    STDMETHOD (GetGuidHeapSize) (
        ULONG   *pcbGuids) PURE;            // [OUT] Size of the Guid heap.

    STDMETHOD (GetUserStringHeapSize) (
        ULONG   *pcbBlobs) PURE;            // [OUT] Size of the User String heap.

    STDMETHOD (GetNumTables) (
        ULONG   *pcTables) PURE;            // [OUT] Count of tables.

    STDMETHOD (GetTableIndex) (
        ULONG   token,                      // [IN] Token for which to get table index.
        ULONG   *pixTbl) PURE;              // [OUT] Put table index here.

    STDMETHOD (GetTableInfo) (
        ULONG   ixTbl,                      // [IN] Which table.
        ULONG   *pcbRow,                    // [OUT] Size of a row, bytes.
        ULONG   *pcRows,                    // [OUT] Number of rows.
        ULONG   *pcCols,                    // [OUT] Number of columns in each row.
        ULONG   *piKey,                     // [OUT] Key column, or -1 if none.
        const char **ppName) PURE;          // [OUT] Name of the table.

    STDMETHOD (GetColumnInfo) (
        ULONG   ixTbl,                      // [IN] Which Table
        ULONG   ixCol,                      // [IN] Which Column in the table
        ULONG   *poCol,                     // [OUT] Offset of the column in the row.
        ULONG   *pcbCol,                    // [OUT] Size of a column, bytes.
        ULONG   *pType,                     // [OUT] Type of the column.
        const char **ppName) PURE;          // [OUT] Name of the Column.

    STDMETHOD (GetCodedTokenInfo) (
        ULONG   ixCdTkn,                    // [IN] Which kind of coded token.
        ULONG   *pcTokens,                  // [OUT] Count of tokens.
        ULONG   **ppTokens,                 // [OUT] List of tokens.
        const char **ppName) PURE;          // [OUT] Name of the CodedToken.

    STDMETHOD (GetRow) (
        ULONG   ixTbl,                      // [IN] Which table.
        ULONG   rid,                        // [IN] Which row.
        void    **ppRow) PURE;              // [OUT] Put pointer to row here.

    STDMETHOD (GetColumn) (
        ULONG   ixTbl,                      // [IN] Which table.
        ULONG   ixCol,                      // [IN] Which column.
        ULONG   rid,                        // [IN] Which row.
        ULONG   *pVal) PURE;                // [OUT] Put the column contents here.

    STDMETHOD (GetString) (
        ULONG   ixString,                   // [IN] Value from a string column.
        const char **ppString) PURE;        // [OUT] Put a pointer to the string here.

    STDMETHOD (GetBlob) (
        ULONG   ixBlob,                     // [IN] Value from a blob column.
        ULONG   *pcbData,                   // [OUT] Put size of the blob here.
        const void **ppData) PURE;          // [OUT] Put a pointer to the blob here.

    STDMETHOD (GetGuid) (
        ULONG   ixGuid,                     // [IN] Value from a guid column.
        const GUID **ppGUID) PURE;          // [OUT] Put a pointer to the GUID here.

    STDMETHOD (GetUserString) (
        ULONG   ixUserString,               // [IN] Value from a UserString column.
        ULONG   *pcbData,                   // [OUT] Put size of the UserString here.
        const void **ppData) PURE;          // [OUT] Put a pointer to the UserString here.

    STDMETHOD (GetNextString) (
        ULONG   ixString,                   // [IN] Value from a string column.
        ULONG   *pNext) PURE;               // [OUT] Put the index of the next string here.

    STDMETHOD (GetNextBlob) (
        ULONG   ixBlob,                     // [IN] Value from a blob column.
        ULONG   *pNext) PURE;               // [OUT] Put the index of the netxt blob here.

    STDMETHOD (GetNextGuid) (
        ULONG   ixGuid,                     // [IN] Value from a guid column.
        ULONG   *pNext) PURE;               // [OUT] Put the index of the next guid here.

    STDMETHOD (GetNextUserString) (
        ULONG   ixUserString,               // [IN] Value from a UserString column.
        ULONG   *pNext) PURE;               // [OUT] Put the index of the next user string here.

    // Interface is sealed.

};
// This API isn't big endian friendly since it indexes directly into the memory that
// is stored in little endian format.
// {BADB5F70-58DA-43a9-A1C6-D74819F19B15}
EXTERN_GUID(IID_IMetaDataTables2, 0xbadb5f70, 0x58da, 0x43a9, 0xa1, 0xc6, 0xd7, 0x48, 0x19, 0xf1, 0x9b, 0x15);

DECLARE_INTERFACE_(IMetaDataTables2, IMetaDataTables)
{
    STDMETHOD (GetMetaDataStorage) (        //@todo: name?
        const void **ppvMd,                 // [OUT] put pointer to MD section here (aka, 'BSJB').
        ULONG   *pcbMd) PURE;               // [OUT] put size of the stream here.

    STDMETHOD (GetMetaDataStreamInfo) (     // Get info about the MD stream.
        ULONG   ix,                         // [IN] Stream ordinal desired.
        const char **ppchName,              // [OUT] put pointer to stream name here.
        const void **ppv,                   // [OUT] put pointer to MD stream here.
        ULONG   *pcb) PURE;                 // [OUT] put size of the stream here.

}; // IMetaDataTables2

#ifdef _DEFINE_META_DATA_META_CONSTANTS
#ifndef _META_DATA_META_CONSTANTS_DEFINED
#define _META_DATA_META_CONSTANTS_DEFINED
const unsigned int iRidMax          = 63;
const unsigned int iCodedToken      = 64;   // base of coded tokens.
const unsigned int iCodedTokenMax   = 95;
const unsigned int iSHORT           = 96;   // fixed types.
const unsigned int iUSHORT          = 97;
const unsigned int iLONG            = 98;
const unsigned int iULONG           = 99;
const unsigned int iBYTE            = 100;
const unsigned int iSTRING          = 101;  // pool types.
const unsigned int iGUID            = 102;
const unsigned int iBLOB            = 103;

inline int IsRidType(ULONG ix) { return ix <= iRidMax; }
inline int IsCodedTokenType(ULONG ix) { return (ix >= iCodedToken) && (ix <= iCodedTokenMax); }
inline int IsRidOrToken(ULONG ix) { return ix <= iCodedTokenMax; }
inline int IsHeapType(ULONG ix) { return ix >= iSTRING; }
inline int IsFixedType(ULONG ix) { return (ix < iSTRING) && (ix > iCodedTokenMax); }
#endif
#endif

//**********************************************************************
// End of IMetaDataTables.
//**********************************************************************

//-------------------------------------
//--- IMetaDataInfo
//-------------------------------------
// {7998EA64-7F95-48B8-86FC-17CAF48BF5CB}
EXTERN_GUID(IID_IMetaDataInfo, 0x7998EA64, 0x7F95, 0x48B8, 0x86, 0xFC, 0x17, 0xCA, 0xF4, 0x8B, 0xF5, 0xCB);

//---
#undef  INTERFACE
#define INTERFACE IMetaDataInfo
DECLARE_INTERFACE_(IMetaDataInfo, IUnknown)
{
    // Return Values:
    //   S_OK               - All parameters are filled.
    //   COR_E_NOTSUPPORTED - The API is not supported for this particular scope (e.g. .obj files, scope 
    //                        opened without whole file via code:IMetaDataDispenser::OpenScopeOnMemory, etc.).
    //   E_INVALIDARG       - If NULL is passed as parameter.
   STDMETHOD(GetFileMapping)(
        const void ** ppvData,              // [out] Pointer to the start of the mapped file.
        ULONGLONG *   pcbData,              // [out] Size of the mapped memory region.
        DWORD *       pdwMappingType) PURE; // [out] Type of file mapping (code:CorFileMapping).
};  // class IMetaDataInfo


//-------------------------------------
//--- IMetaDataWinMDImport
//-------------------------------------
// {969EA0C5-964E-411B-A807-B0F3C2DFCBD4}
EXTERN_GUID(IID_IMetaDataWinMDImport, 0x969ea0c5, 0x964e, 0x411b, 0xa8, 0x7, 0xb0, 0xf3, 0xc2, 0xdf, 0xcb, 0xd4);

//---
#undef  INTERFACE
#define INTERFACE IMetaDataWinMDImport
DECLARE_INTERFACE_(IMetaDataWinMDImport, IUnknown)
{
       STDMETHOD(GetUntransformedTypeRefProps)( // S_OK or error.
        mdTypeRef   tr,                         // [IN] TypeRef token.
        mdToken     *ptkResolutionScope,        // [OUT] Resolution scope, ModuleRef or AssemblyRef.
      _Out_writes_to_opt_(cchName, *pchName)
        LPWSTR      szName,                     // [OUT] Name of the TypeRef.
        ULONG       cchName,                    // [IN] Size of buffer.
        ULONG       *pchName) PURE;             // [OUT] Size of Name.
};  // class IMetaDataWinMDImport

//**********************************************************************
//
// Predefined CustomAttribute and structures for these custom value
//
//**********************************************************************

//
// Native Link method custom value definitions. This is for N-direct support.
//

#define COR_NATIVE_LINK_CUSTOM_VALUE        L"COMPLUS_NativeLink"
#define COR_NATIVE_LINK_CUSTOM_VALUE_ANSI   "COMPLUS_NativeLink"

// count of chars for COR_NATIVE_LINK_CUSTOM_VALUE(_ANSI)
#define COR_NATIVE_LINK_CUSTOM_VALUE_CC     18

#include <pshpack1.h>
typedef struct 
{
    BYTE        m_linkType;       // see CorNativeLinkType below
    BYTE        m_flags;          // see CorNativeLinkFlags below
    mdMemberRef m_entryPoint;     // member ref token giving entry point, format is lib:entrypoint
} COR_NATIVE_LINK;
#include <poppack.h>

typedef enum 
{
    nltNone         = 1,    // none of the keywords are specified
    nltAnsi         = 2,    // ansi keyword specified
    nltUnicode      = 3,    // unicode keyword specified
    nltAuto         = 4,    // auto keyword specified
    nltMaxValue     = 7,    // used so we can assert how many bits are required for this enum
} CorNativeLinkType;

typedef enum 
{
    nlfNone         = 0x00,     // no flags 
    nlfLastError    = 0x01,     // setLastError keyword specified
    nlfNoMangle     = 0x02,     // nomangle keyword specified
    nlfMaxValue     = 0x03,     // used so we can assert how many bits are required for this enum
} CorNativeLinkFlags;

//
// Base class for security custom attributes.
//

#define COR_BASE_SECURITY_ATTRIBUTE_CLASS L"System.Security.Permissions.SecurityAttribute"
#define COR_BASE_SECURITY_ATTRIBUTE_CLASS_ANSI "System.Security.Permissions.SecurityAttribute"

//
// Name of custom attribute used to indicate that per-call security checks should
// be disabled for P/Invoke calls.
//

#define COR_SUPPRESS_UNMANAGED_CODE_CHECK_ATTRIBUTE L"System.Security.SuppressUnmanagedCodeSecurityAttribute"
#define COR_SUPPRESS_UNMANAGED_CODE_CHECK_ATTRIBUTE_ANSI "System.Security.SuppressUnmanagedCodeSecurityAttribute"

//
// Name of custom attribute tagged on module to indicate it contains
// unverifiable code.
//

#define COR_UNVER_CODE_ATTRIBUTE L"System.Security.UnverifiableCodeAttribute"
#define COR_UNVER_CODE_ATTRIBUTE_ANSI "System.Security.UnverifiableCodeAttribute"

//
// Name of custom attribute indicating that a method requires a security object
// slot on the caller's stack.
//

#define COR_REQUIRES_SECOBJ_ATTRIBUTE W("System.Security.DynamicSecurityMethodAttribute")
#define COR_REQUIRES_SECOBJ_ATTRIBUTE_ANSI "System.Security.DynamicSecurityMethodAttribute"

#define COR_COMPILERSERVICE_DISCARDABLEATTRIBUTE L"System.Runtime.CompilerServices.DiscardableAttribute"
#define COR_COMPILERSERVICE_DISCARDABLEATTRIBUTE_ASNI "System.Runtime.CompilerServices.DiscardableAttribute"


#ifdef __cplusplus
}

//*****************************************************************************
//*****************************************************************************
//
// C O M +   s i g n a t u r e   s u p p o r t
//
//*****************************************************************************
//*****************************************************************************

#ifndef FORCEINLINE
 #if _MSC_VER < 1200
   #define FORCEINLINE inline
 #else
   #define FORCEINLINE __forceinline
 #endif
#endif


// We need a version that is FORCEINLINE on retail and NOINLINE on debug

#ifndef DEBUG_NOINLINE
#if defined(_DEBUG)
#define DEBUG_NOINLINE NOINLINE
#else
#define DEBUG_NOINLINE
#endif
#endif

#ifndef NOINLINE
#ifdef _MSC_VER
#define NOINLINE __declspec(noinline)
#elif defined __GNUC__
#define NOINLINE __attribute__ ((noinline))
#else
#define NOINLINE
#endif
#endif // !NOINLINE

// return true if it is a primitive type, i.e. only need to store CorElementType
FORCEINLINE int CorIsPrimitiveType(CorElementType elementtype)
{
    return (elementtype < ELEMENT_TYPE_PTR || elementtype == ELEMENT_TYPE_I || elementtype == ELEMENT_TYPE_U);
}


// Return true if element type is a modifier, i.e. ELEMENT_TYPE_MODIFIER bits are 
// turned on. For now, it is checking for ELEMENT_TYPE_PTR and ELEMENT_TYPE_BYREF
// as well. This will be removed when we turn on ELEMENT_TYPE_MODIFIER bits for 
// these two enum members.
//
FORCEINLINE int CorIsModifierElementType(CorElementType elementtype)
{
    if (elementtype == ELEMENT_TYPE_PTR || elementtype == ELEMENT_TYPE_BYREF)
        return 1;
    return  (elementtype & ELEMENT_TYPE_MODIFIER);
}

// Given a compress byte (*pData), return the size of the uncompressed data.
inline ULONG CorSigUncompressedDataSize(
    PCCOR_SIGNATURE pData)
{
    if ((*pData & 0x80) == 0)
        return 1;
    else if ((*pData & 0xC0) == 0x80)
        return 2;
    else
        return 4;
}

/////////////////////////////////////////////////////////////////////////////////////////////
//
// Given a compressed integer(*pData), expand the compressed int to *pDataOut.
// Return value is the number of bytes that the integer occupies in the compressed format
// It is caller's responsibility to ensure pDataOut has at least 4 bytes to be written to.
//
// This function returns -1 if pass in with an incorrectly compressed data, such as
// (*pBytes & 0xE0) == 0XE0.
/////////////////////////////////////////////////////////////////////////////////////////////
inline ULONG CorSigUncompressBigData(
    PCCOR_SIGNATURE & pData)    // [IN,OUT] compressed data 
{
    ULONG res;

    // 1 byte data is handled in CorSigUncompressData
    //  _ASSERTE(*pData & 0x80);

    // Medium.
    if ((*pData & 0xC0) == 0x80)  // 10?? ????
    {
        res = (ULONG)((*pData++ & 0x3f) << 8);
        res |= *pData++;
    }
    else // 110? ???? 
    {
        res = (*pData++ & 0x1f) << 24;
        res |= *pData++ << 16;
        res |= *pData++ << 8;
        res |= *pData++;
    }
    return res; 
}
FORCEINLINE ULONG CorSigUncompressData(
    PCCOR_SIGNATURE & pData)    // [IN,OUT] compressed data 
{
    // Handle smallest data inline. 
    if ((*pData & 0x80) == 0x00)        // 0??? ????
        return *pData++;
    return CorSigUncompressBigData(pData);
}

inline HRESULT CorSigUncompressData(// return S_OK or E_BADIMAGEFORMAT if the signature is bad 
    PCCOR_SIGNATURE pData,          // [IN] compressed data
    DWORD           len,            // [IN] length of the signature
    ULONG *         pDataOut,       // [OUT] the expanded *pData
    ULONG *         pDataLen)       // [OUT] length of the expanded *pData
{
    HRESULT hr = S_OK;
    BYTE const  *pBytes = reinterpret_cast<BYTE const*>(pData); 

    // Smallest.
    if ((*pBytes & 0x80) == 0x00)       // 0??? ????
    {
        if (len < 1)
        {
            *pDataOut = 0;
            *pDataLen = 0;
            hr = META_E_BAD_SIGNATURE;
        }
        else
        {
            *pDataOut = *pBytes;
            *pDataLen = 1; 
        }
    }
    // Medium.
    else if ((*pBytes & 0xC0) == 0x80)  // 10?? ????
    {
        if (len < 2)
        {
            *pDataOut = 0;
            *pDataLen = 0;
            hr = META_E_BAD_SIGNATURE;
        }
        else
        {
            *pDataOut = (ULONG)(((*pBytes & 0x3f) << 8 | *(pBytes+1)));
            *pDataLen = 2; 
        }
    }
    else if ((*pBytes & 0xE0) == 0xC0)      // 110? ????
    {
        if (len < 4)
        {
            *pDataOut = 0;
            *pDataLen = 0;
            hr = META_E_BAD_SIGNATURE;
        }
        else
        {
            *pDataOut = (ULONG)(((*pBytes & 0x1f) << 24 | *(pBytes+1) << 16 | *(pBytes+2) << 8 | *(pBytes+3)));
            *pDataLen = 4; 
        }
    }
    else // We don't recognize this encoding
    {
        *pDataOut = 0;
        *pDataLen = 0;
        hr = META_E_BAD_SIGNATURE;
    }
    
    return hr;
}

inline ULONG CorSigUncompressData(      // return number of bytes of that compressed data occupied in pData 
    PCCOR_SIGNATURE pData,              // [IN] compressed data 
    ULONG       *pDataOut)              // [OUT] the expanded *pData
{
    ULONG dwSizeOfData = 0;
    
    // We don't know how big the signature is, so we'll just say that it's big enough
    if (FAILED(CorSigUncompressData(pData, 0xff, pDataOut, &dwSizeOfData)))
    {
        *pDataOut = 0;
        return (ULONG)-1;
    }
    
    return dwSizeOfData;
}


#if !defined(SELECTANY)
#if defined(__GNUC__)
    #define SELECTANY extern __attribute__((weak))
#else
    #define SELECTANY extern __declspec(selectany)
#endif
#endif

SELECTANY const mdToken g_tkCorEncodeToken[4] ={mdtTypeDef, mdtTypeRef, mdtTypeSpec, mdtBaseType};

// uncompress a token
inline mdToken CorSigUncompressToken(   // return the token.
    PCCOR_SIGNATURE &pData)             // [IN,OUT] compressed data 
{
    mdToken tk;
    mdToken tkType;

    tk = CorSigUncompressData(pData);
    tkType = g_tkCorEncodeToken[tk & 0x3];
    tk = TokenFromRid(tk >> 2, tkType); 
    return tk;
}


inline ULONG CorSigUncompressToken( // return number of bytes of that compressed data occupied in pData 
    PCCOR_SIGNATURE pData,          // [IN] compressed data 
    mdToken *       pToken)         // [OUT] the expanded *pData
{
    ULONG   cb;
    mdToken tk;
    mdToken tkType;

    cb = CorSigUncompressData(pData, (ULONG *)&tk); 
    tkType = g_tkCorEncodeToken[tk & 0x3];
    tk = TokenFromRid(tk >> 2, tkType); 
    *pToken = tk;
    return cb;
}

inline HRESULT CorSigUncompressToken(
    PCCOR_SIGNATURE pData,          // [IN] compressed data 
    DWORD           dwLen,          // [IN] Remaining length of sigature
    mdToken *       pToken,         // [OUT] the expanded *pData
    DWORD *         dwTokenLength)  // [OUT] The length of the token in the sigature
{
    mdToken tk;
    mdToken tkType;

    HRESULT hr = CorSigUncompressData(pData, dwLen, (ULONG *)&tk, dwTokenLength);

    if (SUCCEEDED(hr))
    {
        tkType = g_tkCorEncodeToken[tk & 0x3];
        tk = TokenFromRid(tk >> 2, tkType); 
        *pToken = tk;
    }
    else
    {
        *pToken = mdTokenNil;
    }
    return hr;
}



FORCEINLINE ULONG CorSigUncompressCallingConv(
    PCCOR_SIGNATURE & pData)    // [IN,OUT] Compressed data
{
    return *pData++;
}

FORCEINLINE HRESULT CorSigUncompressCallingConv(
    PCCOR_SIGNATURE pData,      // [IN] Signature
    DWORD           dwLen,      // [IN] Length of signature
    ULONG *         data)       // [OUT] Compressed data
{
    if (dwLen > 0)
    {
        *data = *pData;
        return S_OK;
    }
    else
    {
        *data = 0;
        return META_E_BAD_SIGNATURE;
    }
}


enum {
    SIGN_MASK_ONEBYTE  = 0xffffffc0,        // Mask the same size as the missing bits.
    SIGN_MASK_TWOBYTE  = 0xffffe000,        // Mask the same size as the missing bits.
    SIGN_MASK_FOURBYTE = 0xf0000000,        // Mask the same size as the missing bits.
};

// uncompress a signed integer
inline ULONG CorSigUncompressSignedInt( // return number of bytes of that compressed data occupied in pData
    PCCOR_SIGNATURE pData,              // [IN] compressed data 
    int *           pInt)               // [OUT] the expanded *pInt 
{
    ULONG cb;
    ULONG ulSigned;
    ULONG iData;

    cb = CorSigUncompressData(pData, &iData);
    if (cb == (ULONG) -1) return cb;
    ulSigned = iData & 0x1; 
    iData = iData >> 1; 
    if (ulSigned)
    {
        if (cb == 1)
        {
            iData |= SIGN_MASK_ONEBYTE; 
        }
        else if (cb == 2)
        {
            iData |= SIGN_MASK_TWOBYTE; 
        }
        else
        {
            iData |= SIGN_MASK_FOURBYTE;
        }
    }
    *pInt = (int)iData;
    return cb;
}


// uncompress encoded element type
FORCEINLINE CorElementType CorSigUncompressElementType( // Element type
    PCCOR_SIGNATURE & pData)                            // [IN,OUT] Compressed data 
{
    return (CorElementType)*pData++;
}

inline ULONG CorSigUncompressElementType(   // Return number of bytes of that compressed data occupied in pData
    PCCOR_SIGNATURE  pData,                 // [IN] Compressed data 
    CorElementType * pElementType)          // [OUT] The expanded *pData
{
    *pElementType = (CorElementType)(*pData & 0x7f);
    return 1;
}


/////////////////////////////////////////////////////////////////////////////////////////////
//
// Given an uncompressed unsigned integer (iLen), Store it to pDataOut in a compressed format.
// Return value is the number of bytes that the integer occupies in the compressed format.
// It is caller's responsibilityt to ensure *pDataOut has at least 4 bytes to write to.
//
// Note that this function returns -1 if iLen is too big to be compressed. We currently can
// only represent to 0x1FFFFFFF.
//
/////////////////////////////////////////////////////////////////////////////////////////////
inline ULONG CorSigCompressData(    // return number of bytes that compressed form of iLen will take
    ULONG  iLen,                    // [IN] given uncompressed data
    void * pDataOut)                // [OUT] buffer where iLen will be compressed and stored.
{
    BYTE *pBytes = reinterpret_cast<BYTE *>(pDataOut);
    
    if (iLen <= 0x7F)
    {
        *pBytes = BYTE(iLen);
        return 1;
    }
    
    if (iLen <= 0x3FFF)
    {
        *pBytes     = BYTE((iLen >> 8) | 0x80);
        *(pBytes+1) = BYTE(iLen & 0xff);
        return 2;
    }
    
    if (iLen <= 0x1FFFFFFF)
    {
        *pBytes     = BYTE((iLen >> 24) | 0xC0);
        *(pBytes+1) = BYTE((iLen >> 16) & 0xff);
        *(pBytes+2) = BYTE((iLen >> 8)  & 0xff);
        *(pBytes+3) = BYTE(iLen & 0xff);
        return 4;
    }
    return (ULONG) -1;
}

// compress a token
// The least significant bit of the first compress byte will indicate the token type.
//
inline ULONG CorSigCompressToken(   // return number of bytes that compressed form of the token will take
    mdToken  tk,                    // [IN] given token
    void *   pDataOut)              // [OUT] buffer where the token will be compressed and stored.
{
    RID     rid = RidFromToken(tk); 
    ULONG32 ulTyp = TypeFromToken(tk);
    
    if (rid > 0x3FFFFFF)
        // token is too big to be compressed
        return (ULONG) -1;
    
    rid = (rid << 2);
    
    // TypeDef is encoded with low bits 00
    // TypeRef is encoded with low bits 01
    // TypeSpec is encoded with low bits 10
    // BaseType is encoded with low bit 11
    //
    if (ulTyp == g_tkCorEncodeToken[1])
    {
        // make the last two bits 01
        rid |= 0x1;
    }
    else if (ulTyp == g_tkCorEncodeToken[2])
    {
        // make last two bits 0
        rid |= 0x2;
    }
    else if (ulTyp == g_tkCorEncodeToken[3])
    {
        rid |= 0x3;
    }
    return CorSigCompressData((ULONG)rid, pDataOut);
}

// compress a signed integer
// The least significant bit of the first compress byte will be the signed bit.
//
inline ULONG CorSigCompressSignedInt(   // return number of bytes that compressed form of iData will take
    int    iData,                       // [IN] given integer
    void * pDataOut)                    // [OUT] buffer where iLen will be compressed and stored.
{
    ULONG isSigned = 0;
    BYTE *pBytes = reinterpret_cast<BYTE *>(pDataOut);
    
    if (iData < 0)
        isSigned = 0x1;
    
    // Note that we cannot use code:CorSigCompressData to pack the iData value, because of negative values 
    // like: 0xffffe000 (-8192) which has to be encoded as 1 in 2 bytes, i.e. 0x81 0x00
    // However CorSigCompressedData would store value 1 as 1 byte: 0x01
    if ((iData & SIGN_MASK_ONEBYTE) == 0 || (iData & SIGN_MASK_ONEBYTE) == SIGN_MASK_ONEBYTE)
    {
        iData = (int)((iData & ~SIGN_MASK_ONEBYTE) << 1 | isSigned);
        //_ASSERTE(iData <= 0x7f);
        *pBytes = BYTE(iData);
        return 1;
    }
    else if ((iData & SIGN_MASK_TWOBYTE) == 0 || (iData & SIGN_MASK_TWOBYTE) == SIGN_MASK_TWOBYTE)
    {
        iData = (int)((iData & ~SIGN_MASK_TWOBYTE) << 1 | isSigned);
        //_ASSERTE(iData <= 0x3fff);
        *pBytes       = BYTE((iData >> 8) | 0x80);
        *(pBytes + 1) = BYTE(iData & 0xff);
        return 2;
    }
    else if ((iData & SIGN_MASK_FOURBYTE) == 0 || (iData & SIGN_MASK_FOURBYTE) == SIGN_MASK_FOURBYTE)
    {
        iData = (int)((iData & ~SIGN_MASK_FOURBYTE) << 1 | isSigned);
        //_ASSERTE(iData <= 0x1FFFFFFF);
        *pBytes       = BYTE((iData >> 24) | 0xC0);
        *(pBytes + 1) = BYTE((iData >> 16) & 0xff);
        *(pBytes + 2) = BYTE((iData >> 8)  & 0xff);
        *(pBytes + 3) = BYTE(iData & 0xff);
        return 4;
    }
    // Out of compressable range
    return (ULONG)-1;
} // CorSigCompressSignedInt


// uncompress encoded element type
inline ULONG CorSigCompressElementType( // return number of bytes of that compressed data occupied in pData
    CorElementType et,                  // [OUT] the expanded *pData 
    void *         pData)               // [IN] compressed data
{
    BYTE *pBytes = (BYTE *)(pData);
    
    *pBytes = BYTE(et);
    return 1;
}

// Compress a pointer (used for internal element types only, never for persisted
// signatures).
inline ULONG CorSigCompressPointer( // return number of bytes of that compressed data occupied
    void * pvPointer,               // [IN] given uncompressed data
    void * pData)                   // [OUT] buffer where iLen will be compressed and stored.
{
    *((void * UNALIGNED *)pData) = pvPointer;
    return sizeof(void *);
}

// Uncompress a pointer (see above for comments).
inline ULONG CorSigUncompressPointer(   // return number of bytes of that compressed data occupied
    PCCOR_SIGNATURE pData,              // [IN] compressed data
    void **         ppvPointer)         // [OUT] the expanded *pData
{
    *ppvPointer = *(void * const UNALIGNED *)pData;
    return sizeof(void *);
}

#endif  // __cplusplus

#endif // _COR_H_
// EOF =======================================================================
