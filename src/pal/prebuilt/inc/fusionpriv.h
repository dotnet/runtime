// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.



/* this ALWAYS GENERATED file contains the definitions for the interfaces */


 /* File created by MIDL compiler version 8.00.0603 */
/* @@MIDL_FILE_HEADING(  ) */

#pragma warning( disable: 4049 )  /* more than 64k source lines */


/* verify that the <rpcndr.h> version is high enough to compile this file*/
#ifndef __REQUIRED_RPCNDR_H_VERSION__
#define __REQUIRED_RPCNDR_H_VERSION__ 475
#endif

#include "rpc.h"
#include "rpcndr.h"

#ifndef __RPCNDR_H_VERSION__
#error this stub requires an updated version of <rpcndr.h>
#endif // __RPCNDR_H_VERSION__

#ifndef COM_NO_WINDOWS_H
#include "windows.h"
#include "ole2.h"
#endif /*COM_NO_WINDOWS_H*/

#ifndef __fusionpriv_h__
#define __fusionpriv_h__

#if defined(_MSC_VER) && (_MSC_VER >= 1020)
#pragma once
#endif

/* Forward Declarations */ 

#ifndef __IHistoryAssembly_FWD_DEFINED__
#define __IHistoryAssembly_FWD_DEFINED__
typedef interface IHistoryAssembly IHistoryAssembly;

#endif 	/* __IHistoryAssembly_FWD_DEFINED__ */


#ifndef __IHistoryReader_FWD_DEFINED__
#define __IHistoryReader_FWD_DEFINED__
typedef interface IHistoryReader IHistoryReader;

#endif 	/* __IHistoryReader_FWD_DEFINED__ */


#ifndef __IFusionBindLog_FWD_DEFINED__
#define __IFusionBindLog_FWD_DEFINED__
typedef interface IFusionBindLog IFusionBindLog;

#endif 	/* __IFusionBindLog_FWD_DEFINED__ */


#ifndef __IAssemblyManifestImport_FWD_DEFINED__
#define __IAssemblyManifestImport_FWD_DEFINED__
typedef interface IAssemblyManifestImport IAssemblyManifestImport;

#endif 	/* __IAssemblyManifestImport_FWD_DEFINED__ */


#ifndef __IApplicationContext_FWD_DEFINED__
#define __IApplicationContext_FWD_DEFINED__
typedef interface IApplicationContext IApplicationContext;

#endif 	/* __IApplicationContext_FWD_DEFINED__ */


#ifndef __IAssemblyNameBinder_FWD_DEFINED__
#define __IAssemblyNameBinder_FWD_DEFINED__
typedef interface IAssemblyNameBinder IAssemblyNameBinder;

#endif 	/* __IAssemblyNameBinder_FWD_DEFINED__ */


#ifndef __IAssembly_FWD_DEFINED__
#define __IAssembly_FWD_DEFINED__
typedef interface IAssembly IAssembly;

#endif 	/* __IAssembly_FWD_DEFINED__ */


#ifndef __IAssemblyBindingClosureEnumerator_FWD_DEFINED__
#define __IAssemblyBindingClosureEnumerator_FWD_DEFINED__
typedef interface IAssemblyBindingClosureEnumerator IAssemblyBindingClosureEnumerator;

#endif 	/* __IAssemblyBindingClosureEnumerator_FWD_DEFINED__ */


#ifndef __IAssemblyBindingClosure_FWD_DEFINED__
#define __IAssemblyBindingClosure_FWD_DEFINED__
typedef interface IAssemblyBindingClosure IAssemblyBindingClosure;

#endif 	/* __IAssemblyBindingClosure_FWD_DEFINED__ */


#ifndef __IAssemblyBindSink_FWD_DEFINED__
#define __IAssemblyBindSink_FWD_DEFINED__
typedef interface IAssemblyBindSink IAssemblyBindSink;

#endif 	/* __IAssemblyBindSink_FWD_DEFINED__ */


#ifndef __IAssemblyBinding_FWD_DEFINED__
#define __IAssemblyBinding_FWD_DEFINED__
typedef interface IAssemblyBinding IAssemblyBinding;

#endif 	/* __IAssemblyBinding_FWD_DEFINED__ */


#ifndef __IAssemblyModuleImport_FWD_DEFINED__
#define __IAssemblyModuleImport_FWD_DEFINED__
typedef interface IAssemblyModuleImport IAssemblyModuleImport;

#endif 	/* __IAssemblyModuleImport_FWD_DEFINED__ */


#ifndef __IAssemblyScavenger_FWD_DEFINED__
#define __IAssemblyScavenger_FWD_DEFINED__
typedef interface IAssemblyScavenger IAssemblyScavenger;

#endif 	/* __IAssemblyScavenger_FWD_DEFINED__ */


#ifndef __ICodebaseList_FWD_DEFINED__
#define __ICodebaseList_FWD_DEFINED__
typedef interface ICodebaseList ICodebaseList;

#endif 	/* __ICodebaseList_FWD_DEFINED__ */


#ifndef __IDownloadMgr_FWD_DEFINED__
#define __IDownloadMgr_FWD_DEFINED__
typedef interface IDownloadMgr IDownloadMgr;

#endif 	/* __IDownloadMgr_FWD_DEFINED__ */


#ifndef __IHostAssembly_FWD_DEFINED__
#define __IHostAssembly_FWD_DEFINED__
typedef interface IHostAssembly IHostAssembly;

#endif 	/* __IHostAssembly_FWD_DEFINED__ */


#ifndef __IHostAssemblyModuleImport_FWD_DEFINED__
#define __IHostAssemblyModuleImport_FWD_DEFINED__
typedef interface IHostAssemblyModuleImport IHostAssemblyModuleImport;

#endif 	/* __IHostAssemblyModuleImport_FWD_DEFINED__ */


/* header files for imported files */
#include "objidl.h"
#include "oleidl.h"
#include "fusion.h"

#ifdef __cplusplus
extern "C"{
#endif 


/* interface __MIDL_itf_fusionpriv_0000_0000 */
/* [local] */ 

//=--------------------------------------------------------------------------=
// fusionpriv.h
//=--------------------------------------------------------------------------=

//
// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF
// ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO
// THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A
// PARTICULAR PURPOSE.
//=--------------------------------------------------------------------------=

#ifdef _MSC_VER
#pragma comment(lib,"uuid.lib")
#endif

//---------------------------------------------------------------------------=
// Fusion Interfaces.

#if defined(_CLR_BLD) && !defined(FEATURE_FUSION)
#error FEATURE_FUSION is not enabled, please do not include fusionpriv.h
#endif
#ifdef _MSC_VER
#pragma once
#endif














struct IMetaDataAssemblyImport;

EXTERN_C const IID IID_IApplicationContext;       
EXTERN_C const IID IID_IAssembly;           
EXTERN_C const IID IID_IAssemblyBindSink;   
EXTERN_C const IID IID_IAssemblyBinding;   
EXTERN_C const IID IID_IAssemblyManifestImport;
EXTERN_C const IID IID_IAssemblyModuleImport;  
EXTERN_C const IID IID_IHistoryAssembly;      
EXTERN_C const IID IID_IHistoryReader;      
EXTERN_C const IID IID_IMetaDataAssemblyImportControl;      
EXTERN_C const IID IID_IAssemblyScavenger;  
EXTERN_C const IID IID_IHostAssembly; 
EXTERN_C const IID IID_IHostAssemblyModuleImport; 
typedef /* [public] */ 
enum __MIDL___MIDL_itf_fusionpriv_0000_0000_0001
    {
        ASM_BINDF_NONE	= 0,
        ASM_BINDF_FORCE_CACHE_INSTALL	= 0x1,
        ASM_BINDF_RFS_INTEGRITY_CHECK	= 0x2,
        ASM_BINDF_RFS_MODULE_CHECK	= 0x4,
        ASM_BINDF_BINPATH_PROBE_ONLY	= 0x8,
        ASM_BINDF_PARENT_ASM_HINT	= 0x20,
        ASM_BINDF_DISALLOW_APPLYPUBLISHERPOLICY	= 0x40,
        ASM_BINDF_DISALLOW_APPBINDINGREDIRECTS	= 0x80,
        ASM_BINDF_DISABLE_FX_UNIFICATION	= 0x100,
        ASM_BINDF_DO_NOT_PROBE_NATIVE_IMAGE	= 0x200,
        ASM_BINDF_DISABLE_DOWNLOAD	= 0x400,
        ASM_BINDF_INSPECTION_ONLY	= 0x800,
        ASM_BINDF_DISALLOW_APP_BASE_PROBING	= 0x1000,
        ASM_BINDF_SUPPRESS_SECURITY_CHECKS	= 0x2000
    } 	ASM_BIND_FLAGS;

typedef 
enum tagDEVOVERRIDEMODE
    {
        DEVOVERRIDE_LOCAL	= 0x1,
        DEVOVERRIDE_GLOBAL	= 0x2
    } 	DEVOVERRIDEMODE;

typedef 
enum tagWALK_LEVEL
    {
        LEVEL_STARTING	= 0,
        LEVEL_WINRTCHECK	= ( LEVEL_STARTING + 1 ) ,
        LEVEL_GACCHECK	= ( LEVEL_WINRTCHECK + 1 ) ,
        LEVEL_COMPLETE	= ( LEVEL_GACCHECK + 1 ) ,
        LEVEL_FXPREDICTED	= ( LEVEL_COMPLETE + 1 ) ,
        LEVEL_FXPROBED	= ( LEVEL_FXPREDICTED + 1 ) 
    } 	WALK_LEVEL;



extern RPC_IF_HANDLE __MIDL_itf_fusionpriv_0000_0000_v0_0_c_ifspec;
extern RPC_IF_HANDLE __MIDL_itf_fusionpriv_0000_0000_v0_0_s_ifspec;

#ifndef __IHistoryAssembly_INTERFACE_DEFINED__
#define __IHistoryAssembly_INTERFACE_DEFINED__

/* interface IHistoryAssembly */
/* [unique][uuid][object][local] */ 


EXTERN_C const IID IID_IHistoryAssembly;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("e6096a07-e188-4a49-8d50-2a0172a0d205")
    IHistoryAssembly : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE GetAssemblyName( 
            /* [annotation][out] */ 
            __out  LPWSTR wzAsmName,
            /* [annotation][out][in] */ 
            __inout  DWORD *pdwSize) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetPublicKeyToken( 
            /* [annotation][out] */ 
            __out  LPWSTR wzPublicKeyToken,
            /* [annotation][out][in] */ 
            __inout  DWORD *pdwSize) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetCulture( 
            /* [annotation][out] */ 
            __out  LPWSTR wzCulture,
            /* [annotation][out][in] */ 
            __inout  DWORD *pdwSize) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetReferenceVersion( 
            /* [annotation][out] */ 
            __out  LPWSTR wzVerRef,
            /* [annotation][out][in] */ 
            __inout  DWORD *pdwSize) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetActivationDate( 
            /* [annotation][out] */ 
            __out  LPWSTR wzActivationDate,
            /* [annotation][out][in] */ 
            __inout  DWORD *pdwSize) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetAppCfgVersion( 
            /* [annotation][out] */ 
            __out  LPWSTR pwzVerAppCfg,
            /* [annotation][out][in] */ 
            __inout  DWORD *pdwSize) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetPublisherCfgVersion( 
            /* [annotation][out] */ 
            __out  LPWSTR pwzVerPublisherCfg,
            /* [annotation][out][in] */ 
            __inout  DWORD *pdwSize) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetAdminCfgVersion( 
            /* [annotation][out] */ 
            __out  LPWSTR pwzAdminCfg,
            /* [annotation][out][in] */ 
            __inout  DWORD *pdwSize) = 0;
        
    };
    
    
#else 	/* C style interface */

    typedef struct IHistoryAssemblyVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            IHistoryAssembly * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            IHistoryAssembly * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            IHistoryAssembly * This);
        
        HRESULT ( STDMETHODCALLTYPE *GetAssemblyName )( 
            IHistoryAssembly * This,
            /* [annotation][out] */ 
            __out  LPWSTR wzAsmName,
            /* [annotation][out][in] */ 
            __inout  DWORD *pdwSize);
        
        HRESULT ( STDMETHODCALLTYPE *GetPublicKeyToken )( 
            IHistoryAssembly * This,
            /* [annotation][out] */ 
            __out  LPWSTR wzPublicKeyToken,
            /* [annotation][out][in] */ 
            __inout  DWORD *pdwSize);
        
        HRESULT ( STDMETHODCALLTYPE *GetCulture )( 
            IHistoryAssembly * This,
            /* [annotation][out] */ 
            __out  LPWSTR wzCulture,
            /* [annotation][out][in] */ 
            __inout  DWORD *pdwSize);
        
        HRESULT ( STDMETHODCALLTYPE *GetReferenceVersion )( 
            IHistoryAssembly * This,
            /* [annotation][out] */ 
            __out  LPWSTR wzVerRef,
            /* [annotation][out][in] */ 
            __inout  DWORD *pdwSize);
        
        HRESULT ( STDMETHODCALLTYPE *GetActivationDate )( 
            IHistoryAssembly * This,
            /* [annotation][out] */ 
            __out  LPWSTR wzActivationDate,
            /* [annotation][out][in] */ 
            __inout  DWORD *pdwSize);
        
        HRESULT ( STDMETHODCALLTYPE *GetAppCfgVersion )( 
            IHistoryAssembly * This,
            /* [annotation][out] */ 
            __out  LPWSTR pwzVerAppCfg,
            /* [annotation][out][in] */ 
            __inout  DWORD *pdwSize);
        
        HRESULT ( STDMETHODCALLTYPE *GetPublisherCfgVersion )( 
            IHistoryAssembly * This,
            /* [annotation][out] */ 
            __out  LPWSTR pwzVerPublisherCfg,
            /* [annotation][out][in] */ 
            __inout  DWORD *pdwSize);
        
        HRESULT ( STDMETHODCALLTYPE *GetAdminCfgVersion )( 
            IHistoryAssembly * This,
            /* [annotation][out] */ 
            __out  LPWSTR pwzAdminCfg,
            /* [annotation][out][in] */ 
            __inout  DWORD *pdwSize);
        
        END_INTERFACE
    } IHistoryAssemblyVtbl;

    interface IHistoryAssembly
    {
        CONST_VTBL struct IHistoryAssemblyVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define IHistoryAssembly_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define IHistoryAssembly_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define IHistoryAssembly_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define IHistoryAssembly_GetAssemblyName(This,wzAsmName,pdwSize)	\
    ( (This)->lpVtbl -> GetAssemblyName(This,wzAsmName,pdwSize) ) 

#define IHistoryAssembly_GetPublicKeyToken(This,wzPublicKeyToken,pdwSize)	\
    ( (This)->lpVtbl -> GetPublicKeyToken(This,wzPublicKeyToken,pdwSize) ) 

#define IHistoryAssembly_GetCulture(This,wzCulture,pdwSize)	\
    ( (This)->lpVtbl -> GetCulture(This,wzCulture,pdwSize) ) 

#define IHistoryAssembly_GetReferenceVersion(This,wzVerRef,pdwSize)	\
    ( (This)->lpVtbl -> GetReferenceVersion(This,wzVerRef,pdwSize) ) 

#define IHistoryAssembly_GetActivationDate(This,wzActivationDate,pdwSize)	\
    ( (This)->lpVtbl -> GetActivationDate(This,wzActivationDate,pdwSize) ) 

#define IHistoryAssembly_GetAppCfgVersion(This,pwzVerAppCfg,pdwSize)	\
    ( (This)->lpVtbl -> GetAppCfgVersion(This,pwzVerAppCfg,pdwSize) ) 

#define IHistoryAssembly_GetPublisherCfgVersion(This,pwzVerPublisherCfg,pdwSize)	\
    ( (This)->lpVtbl -> GetPublisherCfgVersion(This,pwzVerPublisherCfg,pdwSize) ) 

#define IHistoryAssembly_GetAdminCfgVersion(This,pwzAdminCfg,pdwSize)	\
    ( (This)->lpVtbl -> GetAdminCfgVersion(This,pwzAdminCfg,pdwSize) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __IHistoryAssembly_INTERFACE_DEFINED__ */


#ifndef __IHistoryReader_INTERFACE_DEFINED__
#define __IHistoryReader_INTERFACE_DEFINED__

/* interface IHistoryReader */
/* [unique][uuid][object][local] */ 


EXTERN_C const IID IID_IHistoryReader;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("1d23df4d-a1e2-4b8b-93d6-6ea3dc285a54")
    IHistoryReader : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE GetFilePath( 
            /* [annotation][out] */ 
            __out  LPWSTR wzFilePath,
            /* [annotation][out][in] */ 
            __inout  DWORD *pdwSize) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetApplicationName( 
            /* [annotation][out] */ 
            __out  LPWSTR wzAppName,
            /* [annotation][out][in] */ 
            __inout  DWORD *pdwSize) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetEXEModulePath( 
            /* [annotation][out] */ 
            __out  LPWSTR wzExePath,
            /* [annotation][out][in] */ 
            __inout  DWORD *pdwSize) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetNumActivations( 
            /* [out] */ DWORD *pdwNumActivations) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetActivationDate( 
            /* [in] */ DWORD dwIdx,
            /* [out] */ FILETIME *pftDate) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetRunTimeVersion( 
            /* [in] */ FILETIME *pftActivationDate,
            /* [annotation][out] */ 
            __out  LPWSTR wzRunTimeVersion,
            /* [annotation][out][in] */ 
            __inout  DWORD *pdwSize) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetNumAssemblies( 
            /* [in] */ FILETIME *pftActivationDate,
            /* [out] */ DWORD *pdwNumAsms) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetHistoryAssembly( 
            /* [in] */ FILETIME *pftActivationDate,
            /* [in] */ DWORD dwIdx,
            /* [out] */ IHistoryAssembly **ppHistAsm) = 0;
        
    };
    
    
#else 	/* C style interface */

    typedef struct IHistoryReaderVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            IHistoryReader * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            IHistoryReader * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            IHistoryReader * This);
        
        HRESULT ( STDMETHODCALLTYPE *GetFilePath )( 
            IHistoryReader * This,
            /* [annotation][out] */ 
            __out  LPWSTR wzFilePath,
            /* [annotation][out][in] */ 
            __inout  DWORD *pdwSize);
        
        HRESULT ( STDMETHODCALLTYPE *GetApplicationName )( 
            IHistoryReader * This,
            /* [annotation][out] */ 
            __out  LPWSTR wzAppName,
            /* [annotation][out][in] */ 
            __inout  DWORD *pdwSize);
        
        HRESULT ( STDMETHODCALLTYPE *GetEXEModulePath )( 
            IHistoryReader * This,
            /* [annotation][out] */ 
            __out  LPWSTR wzExePath,
            /* [annotation][out][in] */ 
            __inout  DWORD *pdwSize);
        
        HRESULT ( STDMETHODCALLTYPE *GetNumActivations )( 
            IHistoryReader * This,
            /* [out] */ DWORD *pdwNumActivations);
        
        HRESULT ( STDMETHODCALLTYPE *GetActivationDate )( 
            IHistoryReader * This,
            /* [in] */ DWORD dwIdx,
            /* [out] */ FILETIME *pftDate);
        
        HRESULT ( STDMETHODCALLTYPE *GetRunTimeVersion )( 
            IHistoryReader * This,
            /* [in] */ FILETIME *pftActivationDate,
            /* [annotation][out] */ 
            __out  LPWSTR wzRunTimeVersion,
            /* [annotation][out][in] */ 
            __inout  DWORD *pdwSize);
        
        HRESULT ( STDMETHODCALLTYPE *GetNumAssemblies )( 
            IHistoryReader * This,
            /* [in] */ FILETIME *pftActivationDate,
            /* [out] */ DWORD *pdwNumAsms);
        
        HRESULT ( STDMETHODCALLTYPE *GetHistoryAssembly )( 
            IHistoryReader * This,
            /* [in] */ FILETIME *pftActivationDate,
            /* [in] */ DWORD dwIdx,
            /* [out] */ IHistoryAssembly **ppHistAsm);
        
        END_INTERFACE
    } IHistoryReaderVtbl;

    interface IHistoryReader
    {
        CONST_VTBL struct IHistoryReaderVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define IHistoryReader_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define IHistoryReader_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define IHistoryReader_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define IHistoryReader_GetFilePath(This,wzFilePath,pdwSize)	\
    ( (This)->lpVtbl -> GetFilePath(This,wzFilePath,pdwSize) ) 

#define IHistoryReader_GetApplicationName(This,wzAppName,pdwSize)	\
    ( (This)->lpVtbl -> GetApplicationName(This,wzAppName,pdwSize) ) 

#define IHistoryReader_GetEXEModulePath(This,wzExePath,pdwSize)	\
    ( (This)->lpVtbl -> GetEXEModulePath(This,wzExePath,pdwSize) ) 

#define IHistoryReader_GetNumActivations(This,pdwNumActivations)	\
    ( (This)->lpVtbl -> GetNumActivations(This,pdwNumActivations) ) 

#define IHistoryReader_GetActivationDate(This,dwIdx,pftDate)	\
    ( (This)->lpVtbl -> GetActivationDate(This,dwIdx,pftDate) ) 

#define IHistoryReader_GetRunTimeVersion(This,pftActivationDate,wzRunTimeVersion,pdwSize)	\
    ( (This)->lpVtbl -> GetRunTimeVersion(This,pftActivationDate,wzRunTimeVersion,pdwSize) ) 

#define IHistoryReader_GetNumAssemblies(This,pftActivationDate,pdwNumAsms)	\
    ( (This)->lpVtbl -> GetNumAssemblies(This,pftActivationDate,pdwNumAsms) ) 

#define IHistoryReader_GetHistoryAssembly(This,pftActivationDate,dwIdx,ppHistAsm)	\
    ( (This)->lpVtbl -> GetHistoryAssembly(This,pftActivationDate,dwIdx,ppHistAsm) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __IHistoryReader_INTERFACE_DEFINED__ */


/* interface __MIDL_itf_fusionpriv_0000_0002 */
/* [local] */ 

typedef /* [public][public][public] */ 
enum __MIDL___MIDL_itf_fusionpriv_0000_0002_0001
    {
        LOADCTX_TYPE_DEFAULT	= 0,
        LOADCTX_TYPE_LOADFROM	= ( LOADCTX_TYPE_DEFAULT + 1 ) ,
        LOADCTX_TYPE_UNKNOWN	= ( LOADCTX_TYPE_LOADFROM + 1 ) ,
        LOADCTX_TYPE_HOSTED	= ( LOADCTX_TYPE_UNKNOWN + 1 ) 
    } 	LOADCTX_TYPE;

#define FUSION_BIND_LOG_CATEGORY_DEFAULT       0
#define FUSION_BIND_LOG_CATEGORY_NGEN          1
#define FUSION_BIND_LOG_CATEGORY_MAX           2


extern RPC_IF_HANDLE __MIDL_itf_fusionpriv_0000_0002_v0_0_c_ifspec;
extern RPC_IF_HANDLE __MIDL_itf_fusionpriv_0000_0002_v0_0_s_ifspec;

#ifndef __IFusionBindLog_INTERFACE_DEFINED__
#define __IFusionBindLog_INTERFACE_DEFINED__

/* interface IFusionBindLog */
/* [unique][uuid][object][local] */ 


EXTERN_C const IID IID_IFusionBindLog;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("67E9F87D-8B8A-4a90-9D3E-85ED5B2DCC83")
    IFusionBindLog : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE SetResultCode( 
            /* [in] */ DWORD dwLogCategory,
            /* [in] */ HRESULT hr) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetResultCode( 
            /* [in] */ DWORD dwLogCategory,
            /* [out] */ HRESULT *pHr) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetBindLog( 
            /* [in] */ DWORD dwDetailLevel,
            /* [in] */ DWORD dwLogCategory,
            /* [annotation][out] */ 
            __out_opt  LPWSTR pwzDebugLog,
            /* [annotation][out][in] */ 
            __inout  DWORD *pcbDebugLog) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE LogMessage( 
            /* [in] */ DWORD dwDetailLevel,
            /* [in] */ DWORD dwLogCategory,
            /* [in] */ LPCWSTR pwzDebugLog) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE Flush( 
            /* [in] */ DWORD dwDetailLevel,
            /* [in] */ DWORD dwLogCategory) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetBindingID( 
            /* [out] */ ULONGLONG *pullBindingID) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE ETWTraceLogMessage( 
            /* [in] */ DWORD dwETWLogCategory,
            /* [in] */ IAssemblyName *pAsm) = 0;
        
    };
    
    
#else 	/* C style interface */

    typedef struct IFusionBindLogVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            IFusionBindLog * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            IFusionBindLog * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            IFusionBindLog * This);
        
        HRESULT ( STDMETHODCALLTYPE *SetResultCode )( 
            IFusionBindLog * This,
            /* [in] */ DWORD dwLogCategory,
            /* [in] */ HRESULT hr);
        
        HRESULT ( STDMETHODCALLTYPE *GetResultCode )( 
            IFusionBindLog * This,
            /* [in] */ DWORD dwLogCategory,
            /* [out] */ HRESULT *pHr);
        
        HRESULT ( STDMETHODCALLTYPE *GetBindLog )( 
            IFusionBindLog * This,
            /* [in] */ DWORD dwDetailLevel,
            /* [in] */ DWORD dwLogCategory,
            /* [annotation][out] */ 
            __out_opt  LPWSTR pwzDebugLog,
            /* [annotation][out][in] */ 
            __inout  DWORD *pcbDebugLog);
        
        HRESULT ( STDMETHODCALLTYPE *LogMessage )( 
            IFusionBindLog * This,
            /* [in] */ DWORD dwDetailLevel,
            /* [in] */ DWORD dwLogCategory,
            /* [in] */ LPCWSTR pwzDebugLog);
        
        HRESULT ( STDMETHODCALLTYPE *Flush )( 
            IFusionBindLog * This,
            /* [in] */ DWORD dwDetailLevel,
            /* [in] */ DWORD dwLogCategory);
        
        HRESULT ( STDMETHODCALLTYPE *GetBindingID )( 
            IFusionBindLog * This,
            /* [out] */ ULONGLONG *pullBindingID);
        
        HRESULT ( STDMETHODCALLTYPE *ETWTraceLogMessage )( 
            IFusionBindLog * This,
            /* [in] */ DWORD dwETWLogCategory,
            /* [in] */ IAssemblyName *pAsm);
        
        END_INTERFACE
    } IFusionBindLogVtbl;

    interface IFusionBindLog
    {
        CONST_VTBL struct IFusionBindLogVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define IFusionBindLog_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define IFusionBindLog_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define IFusionBindLog_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define IFusionBindLog_SetResultCode(This,dwLogCategory,hr)	\
    ( (This)->lpVtbl -> SetResultCode(This,dwLogCategory,hr) ) 

#define IFusionBindLog_GetResultCode(This,dwLogCategory,pHr)	\
    ( (This)->lpVtbl -> GetResultCode(This,dwLogCategory,pHr) ) 

#define IFusionBindLog_GetBindLog(This,dwDetailLevel,dwLogCategory,pwzDebugLog,pcbDebugLog)	\
    ( (This)->lpVtbl -> GetBindLog(This,dwDetailLevel,dwLogCategory,pwzDebugLog,pcbDebugLog) ) 

#define IFusionBindLog_LogMessage(This,dwDetailLevel,dwLogCategory,pwzDebugLog)	\
    ( (This)->lpVtbl -> LogMessage(This,dwDetailLevel,dwLogCategory,pwzDebugLog) ) 

#define IFusionBindLog_Flush(This,dwDetailLevel,dwLogCategory)	\
    ( (This)->lpVtbl -> Flush(This,dwDetailLevel,dwLogCategory) ) 

#define IFusionBindLog_GetBindingID(This,pullBindingID)	\
    ( (This)->lpVtbl -> GetBindingID(This,pullBindingID) ) 

#define IFusionBindLog_ETWTraceLogMessage(This,dwETWLogCategory,pAsm)	\
    ( (This)->lpVtbl -> ETWTraceLogMessage(This,dwETWLogCategory,pAsm) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __IFusionBindLog_INTERFACE_DEFINED__ */


#ifndef __IAssemblyManifestImport_INTERFACE_DEFINED__
#define __IAssemblyManifestImport_INTERFACE_DEFINED__

/* interface IAssemblyManifestImport */
/* [unique][uuid][object][local] */ 

typedef /* [unique] */ IAssemblyManifestImport *LPASSEMBLY_MANIFEST_IMPORT;


EXTERN_C const IID IID_IAssemblyManifestImport;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("de9a68ba-0fa2-11d3-94aa-00c04fc308ff")
    IAssemblyManifestImport : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE GetAssemblyNameDef( 
            /* [out] */ IAssemblyName **ppAssemblyName) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetNextAssemblyNameRef( 
            /* [in] */ DWORD nIndex,
            /* [out] */ IAssemblyName **ppAssemblyName) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetNextAssemblyModule( 
            /* [in] */ DWORD nIndex,
            /* [out] */ IAssemblyModuleImport **ppImport) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetModuleByName( 
            /* [in] */ LPCOLESTR szModuleName,
            /* [out] */ IAssemblyModuleImport **ppModImport) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetManifestModulePath( 
            /* [annotation][size_is][out] */ 
            __out_ecount_full(*pccModulePath)  LPOLESTR szModulePath,
            /* [out][in] */ LPDWORD pccModulePath) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetInternalMDImport( 
            /* [out] */ IMetaDataAssemblyImport **ppMDImport) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE LoadDataFromMDImport( 
            /* [in] */ IMetaDataAssemblyImport *ppMDImport) = 0;
        
    };
    
    
#else 	/* C style interface */

    typedef struct IAssemblyManifestImportVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            IAssemblyManifestImport * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            IAssemblyManifestImport * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            IAssemblyManifestImport * This);
        
        HRESULT ( STDMETHODCALLTYPE *GetAssemblyNameDef )( 
            IAssemblyManifestImport * This,
            /* [out] */ IAssemblyName **ppAssemblyName);
        
        HRESULT ( STDMETHODCALLTYPE *GetNextAssemblyNameRef )( 
            IAssemblyManifestImport * This,
            /* [in] */ DWORD nIndex,
            /* [out] */ IAssemblyName **ppAssemblyName);
        
        HRESULT ( STDMETHODCALLTYPE *GetNextAssemblyModule )( 
            IAssemblyManifestImport * This,
            /* [in] */ DWORD nIndex,
            /* [out] */ IAssemblyModuleImport **ppImport);
        
        HRESULT ( STDMETHODCALLTYPE *GetModuleByName )( 
            IAssemblyManifestImport * This,
            /* [in] */ LPCOLESTR szModuleName,
            /* [out] */ IAssemblyModuleImport **ppModImport);
        
        HRESULT ( STDMETHODCALLTYPE *GetManifestModulePath )( 
            IAssemblyManifestImport * This,
            /* [annotation][size_is][out] */ 
            __out_ecount_full(*pccModulePath)  LPOLESTR szModulePath,
            /* [out][in] */ LPDWORD pccModulePath);
        
        HRESULT ( STDMETHODCALLTYPE *GetInternalMDImport )( 
            IAssemblyManifestImport * This,
            /* [out] */ IMetaDataAssemblyImport **ppMDImport);
        
        HRESULT ( STDMETHODCALLTYPE *LoadDataFromMDImport )( 
            IAssemblyManifestImport * This,
            /* [in] */ IMetaDataAssemblyImport *ppMDImport);
        
        END_INTERFACE
    } IAssemblyManifestImportVtbl;

    interface IAssemblyManifestImport
    {
        CONST_VTBL struct IAssemblyManifestImportVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define IAssemblyManifestImport_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define IAssemblyManifestImport_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define IAssemblyManifestImport_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define IAssemblyManifestImport_GetAssemblyNameDef(This,ppAssemblyName)	\
    ( (This)->lpVtbl -> GetAssemblyNameDef(This,ppAssemblyName) ) 

#define IAssemblyManifestImport_GetNextAssemblyNameRef(This,nIndex,ppAssemblyName)	\
    ( (This)->lpVtbl -> GetNextAssemblyNameRef(This,nIndex,ppAssemblyName) ) 

#define IAssemblyManifestImport_GetNextAssemblyModule(This,nIndex,ppImport)	\
    ( (This)->lpVtbl -> GetNextAssemblyModule(This,nIndex,ppImport) ) 

#define IAssemblyManifestImport_GetModuleByName(This,szModuleName,ppModImport)	\
    ( (This)->lpVtbl -> GetModuleByName(This,szModuleName,ppModImport) ) 

#define IAssemblyManifestImport_GetManifestModulePath(This,szModulePath,pccModulePath)	\
    ( (This)->lpVtbl -> GetManifestModulePath(This,szModulePath,pccModulePath) ) 

#define IAssemblyManifestImport_GetInternalMDImport(This,ppMDImport)	\
    ( (This)->lpVtbl -> GetInternalMDImport(This,ppMDImport) ) 

#define IAssemblyManifestImport_LoadDataFromMDImport(This,ppMDImport)	\
    ( (This)->lpVtbl -> LoadDataFromMDImport(This,ppMDImport) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __IAssemblyManifestImport_INTERFACE_DEFINED__ */


#ifndef __IApplicationContext_INTERFACE_DEFINED__
#define __IApplicationContext_INTERFACE_DEFINED__

/* interface IApplicationContext */
/* [unique][uuid][object][local] */ 

// App context configuration variables
#define ACTAG_APP_BASE_URL            L"APPBASE"
#define ACTAG_MACHINE_CONFIG          L"MACHINE_CONFIG"
#define ACTAG_APP_PRIVATE_BINPATH     L"PRIVATE_BINPATH"
#define ACTAG_APP_SHARED_BINPATH      L"SHARED_BINPATH"
#define ACTAG_APP_SNAPSHOT_ID         L"SNAPSHOT_ID"
#define ACTAG_APP_CONFIG_FILE         L"APP_CONFIG_FILE"
#define ACTAG_APP_ID                  L"APPLICATION_ID"
#define ACTAG_APP_SHADOW_COPY_DIRS    L"SHADOW_COPY_DIRS"
#define ACTAG_APP_DYNAMIC_BASE        L"DYNAMIC_BASE"
#define ACTAG_APP_CACHE_BASE          L"CACHE_BASE"
#define ACTAG_APP_NAME                L"APP_NAME"
#define ACTAG_DEV_PATH                L"DEV_PATH"
#define ACTAG_HOST_CONFIG_FILE        L"HOST_CONFIG"
#define ACTAG_SXS_ACTIVATION_CONTEXT  L"SXS"
#define ACTAG_APP_CFG_LOCAL_FILEPATH  L"APP_CFG_LOCAL_FILEPATH"
#define ACTAG_ZAP_STRING              L"ZAP_STRING"
#define ACTAG_ZAP_CONFIG_FLAGS        L"ZAP_CONFIG_FLAGS"
#define ACTAG_APP_DOMAIN_ID           L"APPDOMAIN_ID"
#define ACTAG_APP_CONFIG_BLOB         L"APP_CONFIG_BLOB"
#define ACTAG_FX_ONLY                 L"FX_ONLY"
// App context flag overrides
#define ACTAG_FORCE_CACHE_INSTALL     L"FORCE_CACHE_INSTALL"
#define ACTAG_RFS_INTEGRITY_CHECK     L"RFS_INTEGRITY_CHECK"
#define ACTAG_RFS_MODULE_CHECK        L"RFS_MODULE_CHECK"
#define ACTAG_BINPATH_PROBE_ONLY      L"BINPATH_PROBE_ONLY"
#define ACTAG_DISALLOW_APPLYPUBLISHERPOLICY  L"DISALLOW_APP"
#define ACTAG_DISALLOW_APP_BINDING_REDIRECTS  L"DISALLOW_APP_REDIRECTS"
#define ACTAG_DISALLOW_APP_BASE_PROBING L"DISALLOW_APP_BASE_PROBING"
#define ACTAG_CODE_DOWNLOAD_DISABLED  L"CODE_DOWNLOAD_DISABLED"
#define ACTAG_DISABLE_FX_ASM_UNIFICATION  L"DISABLE_FX_ASM_UNIFICATION"
typedef /* [unique] */ IApplicationContext *LPAPPLICATIONCONTEXT;

typedef /* [public] */ 
enum __MIDL_IApplicationContext_0001
    {
        APP_CTX_FLAGS_INTERFACE	= 0x1
    } 	APP_FLAGS;


EXTERN_C const IID IID_IApplicationContext;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("7c23ff90-33af-11d3-95da-00a024a85b51")
    IApplicationContext : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE SetContextNameObject( 
            /* [in] */ LPASSEMBLYNAME pName) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetContextNameObject( 
            /* [out] */ LPASSEMBLYNAME *ppName) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE Set( 
            /* [in] */ LPCOLESTR szName,
            /* [in] */ LPVOID pvValue,
            /* [in] */ DWORD cbValue,
            /* [in] */ DWORD dwFlags) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE Get( 
            /* [in] */ LPCOLESTR szName,
            /* [out] */ LPVOID pvValue,
            /* [out][in] */ LPDWORD pcbValue,
            /* [in] */ DWORD dwFlags) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetDynamicDirectory( 
            /* [annotation][out] */ 
            __out_ecount_opt(*pdwSize)  LPWSTR wzDynamicDir,
            /* [out][in] */ LPDWORD pdwSize) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetAppCacheDirectory( 
            /* [annotation][out] */ 
            __out_ecount_opt(*pdwSize)  LPWSTR wzAppCacheDir,
            /* [out][in] */ LPDWORD pdwSize) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE RegisterKnownAssembly( 
            /* [in] */ IAssemblyName *pName,
            /* [in] */ LPCWSTR pwzAsmURL,
            /* [out] */ IAssembly **ppAsmOut) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE PrefetchAppConfigFile( void) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetAssemblyBindingClosure( 
            /* [in] */ IUnknown *pUnk,
            /* [in] */ LPCWSTR pwzNativeImagePath,
            /* [out] */ IAssemblyBindingClosure **ppAsmClosure) = 0;
        
    };
    
    
#else 	/* C style interface */

    typedef struct IApplicationContextVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            IApplicationContext * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            IApplicationContext * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            IApplicationContext * This);
        
        HRESULT ( STDMETHODCALLTYPE *SetContextNameObject )( 
            IApplicationContext * This,
            /* [in] */ LPASSEMBLYNAME pName);
        
        HRESULT ( STDMETHODCALLTYPE *GetContextNameObject )( 
            IApplicationContext * This,
            /* [out] */ LPASSEMBLYNAME *ppName);
        
        HRESULT ( STDMETHODCALLTYPE *Set )( 
            IApplicationContext * This,
            /* [in] */ LPCOLESTR szName,
            /* [in] */ LPVOID pvValue,
            /* [in] */ DWORD cbValue,
            /* [in] */ DWORD dwFlags);
        
        HRESULT ( STDMETHODCALLTYPE *Get )( 
            IApplicationContext * This,
            /* [in] */ LPCOLESTR szName,
            /* [out] */ LPVOID pvValue,
            /* [out][in] */ LPDWORD pcbValue,
            /* [in] */ DWORD dwFlags);
        
        HRESULT ( STDMETHODCALLTYPE *GetDynamicDirectory )( 
            IApplicationContext * This,
            /* [annotation][out] */ 
            __out_ecount_opt(*pdwSize)  LPWSTR wzDynamicDir,
            /* [out][in] */ LPDWORD pdwSize);
        
        HRESULT ( STDMETHODCALLTYPE *GetAppCacheDirectory )( 
            IApplicationContext * This,
            /* [annotation][out] */ 
            __out_ecount_opt(*pdwSize)  LPWSTR wzAppCacheDir,
            /* [out][in] */ LPDWORD pdwSize);
        
        HRESULT ( STDMETHODCALLTYPE *RegisterKnownAssembly )( 
            IApplicationContext * This,
            /* [in] */ IAssemblyName *pName,
            /* [in] */ LPCWSTR pwzAsmURL,
            /* [out] */ IAssembly **ppAsmOut);
        
        HRESULT ( STDMETHODCALLTYPE *PrefetchAppConfigFile )( 
            IApplicationContext * This);
        
        HRESULT ( STDMETHODCALLTYPE *GetAssemblyBindingClosure )( 
            IApplicationContext * This,
            /* [in] */ IUnknown *pUnk,
            /* [in] */ LPCWSTR pwzNativeImagePath,
            /* [out] */ IAssemblyBindingClosure **ppAsmClosure);
        
        END_INTERFACE
    } IApplicationContextVtbl;

    interface IApplicationContext
    {
        CONST_VTBL struct IApplicationContextVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define IApplicationContext_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define IApplicationContext_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define IApplicationContext_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define IApplicationContext_SetContextNameObject(This,pName)	\
    ( (This)->lpVtbl -> SetContextNameObject(This,pName) ) 

#define IApplicationContext_GetContextNameObject(This,ppName)	\
    ( (This)->lpVtbl -> GetContextNameObject(This,ppName) ) 

#define IApplicationContext_Set(This,szName,pvValue,cbValue,dwFlags)	\
    ( (This)->lpVtbl -> Set(This,szName,pvValue,cbValue,dwFlags) ) 

#define IApplicationContext_Get(This,szName,pvValue,pcbValue,dwFlags)	\
    ( (This)->lpVtbl -> Get(This,szName,pvValue,pcbValue,dwFlags) ) 

#define IApplicationContext_GetDynamicDirectory(This,wzDynamicDir,pdwSize)	\
    ( (This)->lpVtbl -> GetDynamicDirectory(This,wzDynamicDir,pdwSize) ) 

#define IApplicationContext_GetAppCacheDirectory(This,wzAppCacheDir,pdwSize)	\
    ( (This)->lpVtbl -> GetAppCacheDirectory(This,wzAppCacheDir,pdwSize) ) 

#define IApplicationContext_RegisterKnownAssembly(This,pName,pwzAsmURL,ppAsmOut)	\
    ( (This)->lpVtbl -> RegisterKnownAssembly(This,pName,pwzAsmURL,ppAsmOut) ) 

#define IApplicationContext_PrefetchAppConfigFile(This)	\
    ( (This)->lpVtbl -> PrefetchAppConfigFile(This) ) 

#define IApplicationContext_GetAssemblyBindingClosure(This,pUnk,pwzNativeImagePath,ppAsmClosure)	\
    ( (This)->lpVtbl -> GetAssemblyBindingClosure(This,pUnk,pwzNativeImagePath,ppAsmClosure) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __IApplicationContext_INTERFACE_DEFINED__ */


#ifndef __IAssemblyNameBinder_INTERFACE_DEFINED__
#define __IAssemblyNameBinder_INTERFACE_DEFINED__

/* interface IAssemblyNameBinder */
/* [unique][uuid][object][local] */ 


EXTERN_C const IID IID_IAssemblyNameBinder;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("56972d9d-0f6c-47de-a038-e82d5de3a777")
    IAssemblyNameBinder : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE BindToObject( 
            /* [in] */ REFIID refIID,
            /* [in] */ IUnknown *pUnkSink,
            /* [in] */ IUnknown *pUnkContext,
            /* [in] */ LPCOLESTR szCodeBase,
            /* [in] */ LONGLONG llFlags,
            /* [in] */ LPVOID pParentAssembly,
            /* [in] */ DWORD cbReserved,
            /* [out] */ LPVOID *ppv,
            /* [out] */ LPVOID *ppvNI) = 0;
        
    };
    
    
#else 	/* C style interface */

    typedef struct IAssemblyNameBinderVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            IAssemblyNameBinder * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            IAssemblyNameBinder * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            IAssemblyNameBinder * This);
        
        HRESULT ( STDMETHODCALLTYPE *BindToObject )( 
            IAssemblyNameBinder * This,
            /* [in] */ REFIID refIID,
            /* [in] */ IUnknown *pUnkSink,
            /* [in] */ IUnknown *pUnkContext,
            /* [in] */ LPCOLESTR szCodeBase,
            /* [in] */ LONGLONG llFlags,
            /* [in] */ LPVOID pParentAssembly,
            /* [in] */ DWORD cbReserved,
            /* [out] */ LPVOID *ppv,
            /* [out] */ LPVOID *ppvNI);
        
        END_INTERFACE
    } IAssemblyNameBinderVtbl;

    interface IAssemblyNameBinder
    {
        CONST_VTBL struct IAssemblyNameBinderVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define IAssemblyNameBinder_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define IAssemblyNameBinder_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define IAssemblyNameBinder_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define IAssemblyNameBinder_BindToObject(This,refIID,pUnkSink,pUnkContext,szCodeBase,llFlags,pParentAssembly,cbReserved,ppv,ppvNI)	\
    ( (This)->lpVtbl -> BindToObject(This,refIID,pUnkSink,pUnkContext,szCodeBase,llFlags,pParentAssembly,cbReserved,ppv,ppvNI) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __IAssemblyNameBinder_INTERFACE_DEFINED__ */


#ifndef __IAssembly_INTERFACE_DEFINED__
#define __IAssembly_INTERFACE_DEFINED__

/* interface IAssembly */
/* [unique][uuid][object][local] */ 

typedef /* [unique] */ IAssembly *LPASSEMBLY;

#define ASMLOC_LOCATION_MASK          0x0000001B
#define ASMLOC_UNKNOWN                0x00000000
#define ASMLOC_GAC                    0x00000001
#define ASMLOC_DOWNLOAD_CACHE         0x00000002
#define ASMLOC_RUN_FROM_SOURCE        0x00000003
#define ASMLOC_CODEBASE_HINT          0x00000004
#define ASMLOC_ZAP                    0x00000008
#define ASMLOC_DEV_OVERRIDE           0x00000010

EXTERN_C const IID IID_IAssembly;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("ff08d7d4-04c2-11d3-94aa-00c04fc308ff")
    IAssembly : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE GetAssemblyNameDef( 
            /* [out] */ IAssemblyName **ppAssemblyName) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetNextAssemblyNameRef( 
            /* [in] */ DWORD nIndex,
            /* [out] */ IAssemblyName **ppAssemblyName) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetNextAssemblyModule( 
            /* [in] */ DWORD nIndex,
            /* [out] */ IAssemblyModuleImport **ppModImport) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetModuleByName( 
            /* [in] */ LPCOLESTR szModuleName,
            /* [out] */ IAssemblyModuleImport **ppModImport) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetManifestModulePath( 
            /* [annotation][size_is][out] */ 
            __out_ecount_full_opt(*pccModulePath)  LPOLESTR szModulePath,
            /* [out][in] */ LPDWORD pccModulePath) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetAssemblyPath( 
            /* [annotation][size_is][out] */ 
            __out_ecount_full_opt(*lpcwBuffer)  LPOLESTR pStr,
            /* [out][in] */ LPDWORD lpcwBuffer) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetAssemblyLocation( 
            /* [out] */ DWORD *pdwAsmLocation) = 0;
        
        virtual LOADCTX_TYPE STDMETHODCALLTYPE GetFusionLoadContext( void) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetNextHardBoundDependency( 
            /* [in] */ DWORD dwIndex,
            /* [out] */ IAssembly **ppILAsm,
            /* [out] */ IAssembly **ppNIAsm) = 0;
        
    };
    
    
#else 	/* C style interface */

    typedef struct IAssemblyVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            IAssembly * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            IAssembly * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            IAssembly * This);
        
        HRESULT ( STDMETHODCALLTYPE *GetAssemblyNameDef )( 
            IAssembly * This,
            /* [out] */ IAssemblyName **ppAssemblyName);
        
        HRESULT ( STDMETHODCALLTYPE *GetNextAssemblyNameRef )( 
            IAssembly * This,
            /* [in] */ DWORD nIndex,
            /* [out] */ IAssemblyName **ppAssemblyName);
        
        HRESULT ( STDMETHODCALLTYPE *GetNextAssemblyModule )( 
            IAssembly * This,
            /* [in] */ DWORD nIndex,
            /* [out] */ IAssemblyModuleImport **ppModImport);
        
        HRESULT ( STDMETHODCALLTYPE *GetModuleByName )( 
            IAssembly * This,
            /* [in] */ LPCOLESTR szModuleName,
            /* [out] */ IAssemblyModuleImport **ppModImport);
        
        HRESULT ( STDMETHODCALLTYPE *GetManifestModulePath )( 
            IAssembly * This,
            /* [annotation][size_is][out] */ 
            __out_ecount_full_opt(*pccModulePath)  LPOLESTR szModulePath,
            /* [out][in] */ LPDWORD pccModulePath);
        
        HRESULT ( STDMETHODCALLTYPE *GetAssemblyPath )( 
            IAssembly * This,
            /* [annotation][size_is][out] */ 
            __out_ecount_full_opt(*lpcwBuffer)  LPOLESTR pStr,
            /* [out][in] */ LPDWORD lpcwBuffer);
        
        HRESULT ( STDMETHODCALLTYPE *GetAssemblyLocation )( 
            IAssembly * This,
            /* [out] */ DWORD *pdwAsmLocation);
        
        LOADCTX_TYPE ( STDMETHODCALLTYPE *GetFusionLoadContext )( 
            IAssembly * This);
        
        HRESULT ( STDMETHODCALLTYPE *GetNextHardBoundDependency )( 
            IAssembly * This,
            /* [in] */ DWORD dwIndex,
            /* [out] */ IAssembly **ppILAsm,
            /* [out] */ IAssembly **ppNIAsm);
        
        END_INTERFACE
    } IAssemblyVtbl;

    interface IAssembly
    {
        CONST_VTBL struct IAssemblyVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define IAssembly_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define IAssembly_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define IAssembly_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define IAssembly_GetAssemblyNameDef(This,ppAssemblyName)	\
    ( (This)->lpVtbl -> GetAssemblyNameDef(This,ppAssemblyName) ) 

#define IAssembly_GetNextAssemblyNameRef(This,nIndex,ppAssemblyName)	\
    ( (This)->lpVtbl -> GetNextAssemblyNameRef(This,nIndex,ppAssemblyName) ) 

#define IAssembly_GetNextAssemblyModule(This,nIndex,ppModImport)	\
    ( (This)->lpVtbl -> GetNextAssemblyModule(This,nIndex,ppModImport) ) 

#define IAssembly_GetModuleByName(This,szModuleName,ppModImport)	\
    ( (This)->lpVtbl -> GetModuleByName(This,szModuleName,ppModImport) ) 

#define IAssembly_GetManifestModulePath(This,szModulePath,pccModulePath)	\
    ( (This)->lpVtbl -> GetManifestModulePath(This,szModulePath,pccModulePath) ) 

#define IAssembly_GetAssemblyPath(This,pStr,lpcwBuffer)	\
    ( (This)->lpVtbl -> GetAssemblyPath(This,pStr,lpcwBuffer) ) 

#define IAssembly_GetAssemblyLocation(This,pdwAsmLocation)	\
    ( (This)->lpVtbl -> GetAssemblyLocation(This,pdwAsmLocation) ) 

#define IAssembly_GetFusionLoadContext(This)	\
    ( (This)->lpVtbl -> GetFusionLoadContext(This) ) 

#define IAssembly_GetNextHardBoundDependency(This,dwIndex,ppILAsm,ppNIAsm)	\
    ( (This)->lpVtbl -> GetNextHardBoundDependency(This,dwIndex,ppILAsm,ppNIAsm) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __IAssembly_INTERFACE_DEFINED__ */


#ifndef __IAssemblyBindingClosureEnumerator_INTERFACE_DEFINED__
#define __IAssemblyBindingClosureEnumerator_INTERFACE_DEFINED__

/* interface IAssemblyBindingClosureEnumerator */
/* [unique][uuid][object][local] */ 


EXTERN_C const IID IID_IAssemblyBindingClosureEnumerator;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("b3f1e4ed-cb09-4b85-9a1b-6809582f1ebc")
    IAssemblyBindingClosureEnumerator : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE GetNextAssemblyPath( 
            /* [out] */ LPCOLESTR *ppPath,
            /* [out] */ LPCOLESTR *ppniPath) = 0;
        
    };
    
    
#else 	/* C style interface */

    typedef struct IAssemblyBindingClosureEnumeratorVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            IAssemblyBindingClosureEnumerator * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            IAssemblyBindingClosureEnumerator * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            IAssemblyBindingClosureEnumerator * This);
        
        HRESULT ( STDMETHODCALLTYPE *GetNextAssemblyPath )( 
            IAssemblyBindingClosureEnumerator * This,
            /* [out] */ LPCOLESTR *ppPath,
            /* [out] */ LPCOLESTR *ppniPath);
        
        END_INTERFACE
    } IAssemblyBindingClosureEnumeratorVtbl;

    interface IAssemblyBindingClosureEnumerator
    {
        CONST_VTBL struct IAssemblyBindingClosureEnumeratorVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define IAssemblyBindingClosureEnumerator_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define IAssemblyBindingClosureEnumerator_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define IAssemblyBindingClosureEnumerator_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define IAssemblyBindingClosureEnumerator_GetNextAssemblyPath(This,ppPath,ppniPath)	\
    ( (This)->lpVtbl -> GetNextAssemblyPath(This,ppPath,ppniPath) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __IAssemblyBindingClosureEnumerator_INTERFACE_DEFINED__ */


#ifndef __IAssemblyBindingClosure_INTERFACE_DEFINED__
#define __IAssemblyBindingClosure_INTERFACE_DEFINED__

/* interface IAssemblyBindingClosure */
/* [unique][uuid][object][local] */ 


EXTERN_C const IID IID_IAssemblyBindingClosure;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("415c226a-e513-41ba-9651-9c48e97aa5de")
    IAssemblyBindingClosure : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE IsAllAssembliesInGAC( void) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE IsEqual( 
            /* [in] */ IAssemblyBindingClosure *pAssemblyClosure) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetNextFailureAssembly( 
            /* [in] */ DWORD dwIndex,
            /* [out] */ IAssemblyName **ppName,
            /* [out] */ HRESULT *pHResult) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE EnsureWalked( 
            /* [in] */ IUnknown *pStartingAssembly,
            /* [in] */ IApplicationContext *pAppCtx,
            /* [in] */ WALK_LEVEL level) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE EnumerateAssemblies( 
            /* [out] */ IAssemblyBindingClosureEnumerator **ppEnumerator) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE HasBeenWalked( 
            /* [in] */ WALK_LEVEL level) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE MayHaveUnknownDependencies( void) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE AddProfilerAssemblyReference( 
            /* [in] */ LPVOID pbPublicKeyOrToken,
            /* [in] */ ULONG cbPublicKeyOrToken,
            /* [in] */ LPCWSTR szName,
            /* [in] */ LPVOID pMetaData,
            /* [in] */ void *pbHashValue,
            /* [in] */ ULONG cbHashValue,
            /* [in] */ DWORD dwAssemblyRefFlags,
            /* [in] */ struct AssemblyReferenceClosureWalkContextForProfAPI *pContext) = 0;
        
    };
    
    
#else 	/* C style interface */

    typedef struct IAssemblyBindingClosureVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            IAssemblyBindingClosure * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            IAssemblyBindingClosure * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            IAssemblyBindingClosure * This);
        
        HRESULT ( STDMETHODCALLTYPE *IsAllAssembliesInGAC )( 
            IAssemblyBindingClosure * This);
        
        HRESULT ( STDMETHODCALLTYPE *IsEqual )( 
            IAssemblyBindingClosure * This,
            /* [in] */ IAssemblyBindingClosure *pAssemblyClosure);
        
        HRESULT ( STDMETHODCALLTYPE *GetNextFailureAssembly )( 
            IAssemblyBindingClosure * This,
            /* [in] */ DWORD dwIndex,
            /* [out] */ IAssemblyName **ppName,
            /* [out] */ HRESULT *pHResult);
        
        HRESULT ( STDMETHODCALLTYPE *EnsureWalked )( 
            IAssemblyBindingClosure * This,
            /* [in] */ IUnknown *pStartingAssembly,
            /* [in] */ IApplicationContext *pAppCtx,
            /* [in] */ WALK_LEVEL level);
        
        HRESULT ( STDMETHODCALLTYPE *EnumerateAssemblies )( 
            IAssemblyBindingClosure * This,
            /* [out] */ IAssemblyBindingClosureEnumerator **ppEnumerator);
        
        HRESULT ( STDMETHODCALLTYPE *HasBeenWalked )( 
            IAssemblyBindingClosure * This,
            /* [in] */ WALK_LEVEL level);
        
        HRESULT ( STDMETHODCALLTYPE *MayHaveUnknownDependencies )( 
            IAssemblyBindingClosure * This);
        
        HRESULT ( STDMETHODCALLTYPE *AddProfilerAssemblyReference )( 
            IAssemblyBindingClosure * This,
            /* [in] */ LPVOID pbPublicKeyOrToken,
            /* [in] */ ULONG cbPublicKeyOrToken,
            /* [in] */ LPCWSTR szName,
            /* [in] */ LPVOID pMetaData,
            /* [in] */ void *pbHashValue,
            /* [in] */ ULONG cbHashValue,
            /* [in] */ DWORD dwAssemblyRefFlags,
            /* [in] */ struct AssemblyReferenceClosureWalkContextForProfAPI *pContext);
        
        END_INTERFACE
    } IAssemblyBindingClosureVtbl;

    interface IAssemblyBindingClosure
    {
        CONST_VTBL struct IAssemblyBindingClosureVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define IAssemblyBindingClosure_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define IAssemblyBindingClosure_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define IAssemblyBindingClosure_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define IAssemblyBindingClosure_IsAllAssembliesInGAC(This)	\
    ( (This)->lpVtbl -> IsAllAssembliesInGAC(This) ) 

#define IAssemblyBindingClosure_IsEqual(This,pAssemblyClosure)	\
    ( (This)->lpVtbl -> IsEqual(This,pAssemblyClosure) ) 

#define IAssemblyBindingClosure_GetNextFailureAssembly(This,dwIndex,ppName,pHResult)	\
    ( (This)->lpVtbl -> GetNextFailureAssembly(This,dwIndex,ppName,pHResult) ) 

#define IAssemblyBindingClosure_EnsureWalked(This,pStartingAssembly,pAppCtx,level)	\
    ( (This)->lpVtbl -> EnsureWalked(This,pStartingAssembly,pAppCtx,level) ) 

#define IAssemblyBindingClosure_EnumerateAssemblies(This,ppEnumerator)	\
    ( (This)->lpVtbl -> EnumerateAssemblies(This,ppEnumerator) ) 

#define IAssemblyBindingClosure_HasBeenWalked(This,level)	\
    ( (This)->lpVtbl -> HasBeenWalked(This,level) ) 

#define IAssemblyBindingClosure_MayHaveUnknownDependencies(This)	\
    ( (This)->lpVtbl -> MayHaveUnknownDependencies(This) ) 

#define IAssemblyBindingClosure_AddProfilerAssemblyReference(This,pbPublicKeyOrToken,cbPublicKeyOrToken,szName,pMetaData,pbHashValue,cbHashValue,dwAssemblyRefFlags,pContext)	\
    ( (This)->lpVtbl -> AddProfilerAssemblyReference(This,pbPublicKeyOrToken,cbPublicKeyOrToken,szName,pMetaData,pbHashValue,cbHashValue,dwAssemblyRefFlags,pContext) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __IAssemblyBindingClosure_INTERFACE_DEFINED__ */


#ifndef __IAssemblyBindSink_INTERFACE_DEFINED__
#define __IAssemblyBindSink_INTERFACE_DEFINED__

/* interface IAssemblyBindSink */
/* [unique][uuid][object][local] */ 

typedef /* [unique] */ IAssemblyBindSink *LPASSEMBLY_BIND_SINK;

typedef struct _tagFusionBindInfo
    {
    IFusionBindLog *pdbglog;
    IAssemblyName *pNamePolicy;
    DWORD dwPoliciesApplied;
    } 	FusionBindInfo;

typedef /* [public] */ 
enum __MIDL_IAssemblyBindSink_0001
    {
        ASM_NOTIFICATION_START	= 0,
        ASM_NOTIFICATION_PROGRESS	= ( ASM_NOTIFICATION_START + 1 ) ,
        ASM_NOTIFICATION_SUSPEND	= ( ASM_NOTIFICATION_PROGRESS + 1 ) ,
        ASM_NOTIFICATION_ATTEMPT_NEXT_CODEBASE	= ( ASM_NOTIFICATION_SUSPEND + 1 ) ,
        ASM_NOTIFICATION_BIND_INFO	= ( ASM_NOTIFICATION_ATTEMPT_NEXT_CODEBASE + 1 ) ,
        ASM_NOTIFICATION_DONE	= ( ASM_NOTIFICATION_BIND_INFO + 1 ) ,
        ASM_NOTIFICATION_NATIVE_IMAGE_DONE	= ( ASM_NOTIFICATION_DONE + 1 ) 
    } 	ASM_NOTIFICATION;


EXTERN_C const IID IID_IAssemblyBindSink;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("af0bc960-0b9a-11d3-95ca-00a024a85b51")
    IAssemblyBindSink : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE OnProgress( 
            /* [in] */ DWORD dwNotification,
            /* [in] */ HRESULT hrNotification,
            /* [in] */ LPCWSTR szNotification,
            /* [in] */ DWORD dwProgress,
            /* [in] */ DWORD dwProgressMax,
            /* [in] */ LPVOID pvBindInfo,
            /* [in] */ IUnknown *pUnk) = 0;
        
    };
    
    
#else 	/* C style interface */

    typedef struct IAssemblyBindSinkVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            IAssemblyBindSink * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            IAssemblyBindSink * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            IAssemblyBindSink * This);
        
        HRESULT ( STDMETHODCALLTYPE *OnProgress )( 
            IAssemblyBindSink * This,
            /* [in] */ DWORD dwNotification,
            /* [in] */ HRESULT hrNotification,
            /* [in] */ LPCWSTR szNotification,
            /* [in] */ DWORD dwProgress,
            /* [in] */ DWORD dwProgressMax,
            /* [in] */ LPVOID pvBindInfo,
            /* [in] */ IUnknown *pUnk);
        
        END_INTERFACE
    } IAssemblyBindSinkVtbl;

    interface IAssemblyBindSink
    {
        CONST_VTBL struct IAssemblyBindSinkVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define IAssemblyBindSink_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define IAssemblyBindSink_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define IAssemblyBindSink_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define IAssemblyBindSink_OnProgress(This,dwNotification,hrNotification,szNotification,dwProgress,dwProgressMax,pvBindInfo,pUnk)	\
    ( (This)->lpVtbl -> OnProgress(This,dwNotification,hrNotification,szNotification,dwProgress,dwProgressMax,pvBindInfo,pUnk) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __IAssemblyBindSink_INTERFACE_DEFINED__ */


#ifndef __IAssemblyBinding_INTERFACE_DEFINED__
#define __IAssemblyBinding_INTERFACE_DEFINED__

/* interface IAssemblyBinding */
/* [unique][uuid][object][local] */ 

typedef /* [unique] */ IAssemblyBinding *LPASSEMBLY_BINDINDING;


EXTERN_C const IID IID_IAssemblyBinding;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("cfe52a80-12bd-11d3-95ca-00a024a85b51")
    IAssemblyBinding : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE Control( 
            /* [in] */ HRESULT hrControl) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE DoDefaultUI( 
            /* [in] */ HWND hWnd,
            /* [in] */ DWORD dwFlags) = 0;
        
    };
    
    
#else 	/* C style interface */

    typedef struct IAssemblyBindingVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            IAssemblyBinding * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            IAssemblyBinding * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            IAssemblyBinding * This);
        
        HRESULT ( STDMETHODCALLTYPE *Control )( 
            IAssemblyBinding * This,
            /* [in] */ HRESULT hrControl);
        
        HRESULT ( STDMETHODCALLTYPE *DoDefaultUI )( 
            IAssemblyBinding * This,
            /* [in] */ HWND hWnd,
            /* [in] */ DWORD dwFlags);
        
        END_INTERFACE
    } IAssemblyBindingVtbl;

    interface IAssemblyBinding
    {
        CONST_VTBL struct IAssemblyBindingVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define IAssemblyBinding_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define IAssemblyBinding_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define IAssemblyBinding_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define IAssemblyBinding_Control(This,hrControl)	\
    ( (This)->lpVtbl -> Control(This,hrControl) ) 

#define IAssemblyBinding_DoDefaultUI(This,hWnd,dwFlags)	\
    ( (This)->lpVtbl -> DoDefaultUI(This,hWnd,dwFlags) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __IAssemblyBinding_INTERFACE_DEFINED__ */


#ifndef __IAssemblyModuleImport_INTERFACE_DEFINED__
#define __IAssemblyModuleImport_INTERFACE_DEFINED__

/* interface IAssemblyModuleImport */
/* [unique][uuid][object][local] */ 

typedef /* [unique] */ IAssemblyModuleImport *LPASSEMBLY_MODULE_IMPORT;


EXTERN_C const IID IID_IAssemblyModuleImport;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("da0cd4b0-1117-11d3-95ca-00a024a85b51")
    IAssemblyModuleImport : public IStream
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE GetModuleName( 
            /* [annotation][size_is][out] */ 
            __out_ecount_full_opt(*pccModuleName)  LPOLESTR szModuleName,
            /* [out][in] */ LPDWORD pccModuleName) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetHashAlgId( 
            /* [out] */ LPDWORD pdwHashAlgId) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetHashValue( 
            /* [size_is][out] */ BYTE *pbHashValue,
            /* [out][in] */ LPDWORD pcbHashValue) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetFlags( 
            /* [out] */ LPDWORD pdwFlags) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetModulePath( 
            /* [annotation][size_is][out] */ 
            __out_ecount_full_opt(*pccModulePath)  LPOLESTR szModulePath,
            /* [out][in] */ LPDWORD pccModulePath) = 0;
        
        virtual BOOL STDMETHODCALLTYPE IsAvailable( void) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE BindToObject( 
            /* [in] */ IAssemblyBindSink *pBindSink,
            /* [in] */ IApplicationContext *pAppCtx,
            /* [in] */ LONGLONG llFlags,
            /* [out] */ LPVOID *ppv) = 0;
        
    };
    
    
#else 	/* C style interface */

    typedef struct IAssemblyModuleImportVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            IAssemblyModuleImport * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            IAssemblyModuleImport * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            IAssemblyModuleImport * This);
        
        /* [local] */ HRESULT ( STDMETHODCALLTYPE *Read )( 
            IAssemblyModuleImport * This,
            /* [annotation] */ 
            _Out_writes_bytes_to_(cb, *pcbRead)  void *pv,
            /* [annotation][in] */ 
            _In_  ULONG cb,
            /* [annotation] */ 
            _Out_opt_  ULONG *pcbRead);
        
        /* [local] */ HRESULT ( STDMETHODCALLTYPE *Write )( 
            IAssemblyModuleImport * This,
            /* [annotation] */ 
            _In_reads_bytes_(cb)  const void *pv,
            /* [annotation][in] */ 
            _In_  ULONG cb,
            /* [annotation] */ 
            _Out_opt_  ULONG *pcbWritten);
        
        /* [local] */ HRESULT ( STDMETHODCALLTYPE *Seek )( 
            IAssemblyModuleImport * This,
            /* [in] */ LARGE_INTEGER dlibMove,
            /* [in] */ DWORD dwOrigin,
            /* [annotation] */ 
            _Out_opt_  ULARGE_INTEGER *plibNewPosition);
        
        HRESULT ( STDMETHODCALLTYPE *SetSize )( 
            IAssemblyModuleImport * This,
            /* [in] */ ULARGE_INTEGER libNewSize);
        
        /* [local] */ HRESULT ( STDMETHODCALLTYPE *CopyTo )( 
            IAssemblyModuleImport * This,
            /* [annotation][unique][in] */ 
            _In_  IStream *pstm,
            /* [in] */ ULARGE_INTEGER cb,
            /* [annotation] */ 
            _Out_opt_  ULARGE_INTEGER *pcbRead,
            /* [annotation] */ 
            _Out_opt_  ULARGE_INTEGER *pcbWritten);
        
        HRESULT ( STDMETHODCALLTYPE *Commit )( 
            IAssemblyModuleImport * This,
            /* [in] */ DWORD grfCommitFlags);
        
        HRESULT ( STDMETHODCALLTYPE *Revert )( 
            IAssemblyModuleImport * This);
        
        HRESULT ( STDMETHODCALLTYPE *LockRegion )( 
            IAssemblyModuleImport * This,
            /* [in] */ ULARGE_INTEGER libOffset,
            /* [in] */ ULARGE_INTEGER cb,
            /* [in] */ DWORD dwLockType);
        
        HRESULT ( STDMETHODCALLTYPE *UnlockRegion )( 
            IAssemblyModuleImport * This,
            /* [in] */ ULARGE_INTEGER libOffset,
            /* [in] */ ULARGE_INTEGER cb,
            /* [in] */ DWORD dwLockType);
        
        HRESULT ( STDMETHODCALLTYPE *Stat )( 
            IAssemblyModuleImport * This,
            /* [out] */ STATSTG *pstatstg,
            /* [in] */ DWORD grfStatFlag);
        
        HRESULT ( STDMETHODCALLTYPE *Clone )( 
            IAssemblyModuleImport * This,
            /* [out] */ IStream **ppstm);
        
        HRESULT ( STDMETHODCALLTYPE *GetModuleName )( 
            IAssemblyModuleImport * This,
            /* [annotation][size_is][out] */ 
            __out_ecount_full_opt(*pccModuleName)  LPOLESTR szModuleName,
            /* [out][in] */ LPDWORD pccModuleName);
        
        HRESULT ( STDMETHODCALLTYPE *GetHashAlgId )( 
            IAssemblyModuleImport * This,
            /* [out] */ LPDWORD pdwHashAlgId);
        
        HRESULT ( STDMETHODCALLTYPE *GetHashValue )( 
            IAssemblyModuleImport * This,
            /* [size_is][out] */ BYTE *pbHashValue,
            /* [out][in] */ LPDWORD pcbHashValue);
        
        HRESULT ( STDMETHODCALLTYPE *GetFlags )( 
            IAssemblyModuleImport * This,
            /* [out] */ LPDWORD pdwFlags);
        
        HRESULT ( STDMETHODCALLTYPE *GetModulePath )( 
            IAssemblyModuleImport * This,
            /* [annotation][size_is][out] */ 
            __out_ecount_full_opt(*pccModulePath)  LPOLESTR szModulePath,
            /* [out][in] */ LPDWORD pccModulePath);
        
        BOOL ( STDMETHODCALLTYPE *IsAvailable )( 
            IAssemblyModuleImport * This);
        
        HRESULT ( STDMETHODCALLTYPE *BindToObject )( 
            IAssemblyModuleImport * This,
            /* [in] */ IAssemblyBindSink *pBindSink,
            /* [in] */ IApplicationContext *pAppCtx,
            /* [in] */ LONGLONG llFlags,
            /* [out] */ LPVOID *ppv);
        
        END_INTERFACE
    } IAssemblyModuleImportVtbl;

    interface IAssemblyModuleImport
    {
        CONST_VTBL struct IAssemblyModuleImportVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define IAssemblyModuleImport_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define IAssemblyModuleImport_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define IAssemblyModuleImport_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define IAssemblyModuleImport_Read(This,pv,cb,pcbRead)	\
    ( (This)->lpVtbl -> Read(This,pv,cb,pcbRead) ) 

#define IAssemblyModuleImport_Write(This,pv,cb,pcbWritten)	\
    ( (This)->lpVtbl -> Write(This,pv,cb,pcbWritten) ) 


#define IAssemblyModuleImport_Seek(This,dlibMove,dwOrigin,plibNewPosition)	\
    ( (This)->lpVtbl -> Seek(This,dlibMove,dwOrigin,plibNewPosition) ) 

#define IAssemblyModuleImport_SetSize(This,libNewSize)	\
    ( (This)->lpVtbl -> SetSize(This,libNewSize) ) 

#define IAssemblyModuleImport_CopyTo(This,pstm,cb,pcbRead,pcbWritten)	\
    ( (This)->lpVtbl -> CopyTo(This,pstm,cb,pcbRead,pcbWritten) ) 

#define IAssemblyModuleImport_Commit(This,grfCommitFlags)	\
    ( (This)->lpVtbl -> Commit(This,grfCommitFlags) ) 

#define IAssemblyModuleImport_Revert(This)	\
    ( (This)->lpVtbl -> Revert(This) ) 

#define IAssemblyModuleImport_LockRegion(This,libOffset,cb,dwLockType)	\
    ( (This)->lpVtbl -> LockRegion(This,libOffset,cb,dwLockType) ) 

#define IAssemblyModuleImport_UnlockRegion(This,libOffset,cb,dwLockType)	\
    ( (This)->lpVtbl -> UnlockRegion(This,libOffset,cb,dwLockType) ) 

#define IAssemblyModuleImport_Stat(This,pstatstg,grfStatFlag)	\
    ( (This)->lpVtbl -> Stat(This,pstatstg,grfStatFlag) ) 

#define IAssemblyModuleImport_Clone(This,ppstm)	\
    ( (This)->lpVtbl -> Clone(This,ppstm) ) 


#define IAssemblyModuleImport_GetModuleName(This,szModuleName,pccModuleName)	\
    ( (This)->lpVtbl -> GetModuleName(This,szModuleName,pccModuleName) ) 

#define IAssemblyModuleImport_GetHashAlgId(This,pdwHashAlgId)	\
    ( (This)->lpVtbl -> GetHashAlgId(This,pdwHashAlgId) ) 

#define IAssemblyModuleImport_GetHashValue(This,pbHashValue,pcbHashValue)	\
    ( (This)->lpVtbl -> GetHashValue(This,pbHashValue,pcbHashValue) ) 

#define IAssemblyModuleImport_GetFlags(This,pdwFlags)	\
    ( (This)->lpVtbl -> GetFlags(This,pdwFlags) ) 

#define IAssemblyModuleImport_GetModulePath(This,szModulePath,pccModulePath)	\
    ( (This)->lpVtbl -> GetModulePath(This,szModulePath,pccModulePath) ) 

#define IAssemblyModuleImport_IsAvailable(This)	\
    ( (This)->lpVtbl -> IsAvailable(This) ) 

#define IAssemblyModuleImport_BindToObject(This,pBindSink,pAppCtx,llFlags,ppv)	\
    ( (This)->lpVtbl -> BindToObject(This,pBindSink,pAppCtx,llFlags,ppv) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __IAssemblyModuleImport_INTERFACE_DEFINED__ */


#ifndef __IAssemblyScavenger_INTERFACE_DEFINED__
#define __IAssemblyScavenger_INTERFACE_DEFINED__

/* interface IAssemblyScavenger */
/* [unique][uuid][object][local] */ 


EXTERN_C const IID IID_IAssemblyScavenger;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("21b8916c-f28e-11d2-a473-00ccff8ef448")
    IAssemblyScavenger : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE ScavengeAssemblyCache( void) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetCacheDiskQuotas( 
            /* [out] */ DWORD *pdwZapQuotaInGAC,
            /* [out] */ DWORD *pdwDownloadQuotaAdmin,
            /* [out] */ DWORD *pdwDownloadQuotaUser) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE SetCacheDiskQuotas( 
            /* [in] */ DWORD dwZapQuotaInGAC,
            /* [in] */ DWORD dwDownloadQuotaAdmin,
            /* [in] */ DWORD dwDownloadQuotaUser) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetCurrentCacheUsage( 
            /* [out] */ DWORD *dwZapUsage,
            /* [out] */ DWORD *dwDownloadUsage) = 0;
        
    };
    
    
#else 	/* C style interface */

    typedef struct IAssemblyScavengerVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            IAssemblyScavenger * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            IAssemblyScavenger * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            IAssemblyScavenger * This);
        
        HRESULT ( STDMETHODCALLTYPE *ScavengeAssemblyCache )( 
            IAssemblyScavenger * This);
        
        HRESULT ( STDMETHODCALLTYPE *GetCacheDiskQuotas )( 
            IAssemblyScavenger * This,
            /* [out] */ DWORD *pdwZapQuotaInGAC,
            /* [out] */ DWORD *pdwDownloadQuotaAdmin,
            /* [out] */ DWORD *pdwDownloadQuotaUser);
        
        HRESULT ( STDMETHODCALLTYPE *SetCacheDiskQuotas )( 
            IAssemblyScavenger * This,
            /* [in] */ DWORD dwZapQuotaInGAC,
            /* [in] */ DWORD dwDownloadQuotaAdmin,
            /* [in] */ DWORD dwDownloadQuotaUser);
        
        HRESULT ( STDMETHODCALLTYPE *GetCurrentCacheUsage )( 
            IAssemblyScavenger * This,
            /* [out] */ DWORD *dwZapUsage,
            /* [out] */ DWORD *dwDownloadUsage);
        
        END_INTERFACE
    } IAssemblyScavengerVtbl;

    interface IAssemblyScavenger
    {
        CONST_VTBL struct IAssemblyScavengerVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define IAssemblyScavenger_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define IAssemblyScavenger_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define IAssemblyScavenger_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define IAssemblyScavenger_ScavengeAssemblyCache(This)	\
    ( (This)->lpVtbl -> ScavengeAssemblyCache(This) ) 

#define IAssemblyScavenger_GetCacheDiskQuotas(This,pdwZapQuotaInGAC,pdwDownloadQuotaAdmin,pdwDownloadQuotaUser)	\
    ( (This)->lpVtbl -> GetCacheDiskQuotas(This,pdwZapQuotaInGAC,pdwDownloadQuotaAdmin,pdwDownloadQuotaUser) ) 

#define IAssemblyScavenger_SetCacheDiskQuotas(This,dwZapQuotaInGAC,dwDownloadQuotaAdmin,dwDownloadQuotaUser)	\
    ( (This)->lpVtbl -> SetCacheDiskQuotas(This,dwZapQuotaInGAC,dwDownloadQuotaAdmin,dwDownloadQuotaUser) ) 

#define IAssemblyScavenger_GetCurrentCacheUsage(This,dwZapUsage,dwDownloadUsage)	\
    ( (This)->lpVtbl -> GetCurrentCacheUsage(This,dwZapUsage,dwDownloadUsage) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __IAssemblyScavenger_INTERFACE_DEFINED__ */


#ifndef __ICodebaseList_INTERFACE_DEFINED__
#define __ICodebaseList_INTERFACE_DEFINED__

/* interface ICodebaseList */
/* [unique][uuid][object][local] */ 


EXTERN_C const IID IID_ICodebaseList;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("D8FB9BD6-3969-11d3-B4AF-00C04F8ECB26")
    ICodebaseList : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE AddCodebase( 
            /* [in] */ LPCWSTR wzCodebase,
            /* [in] */ DWORD dwFlags) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE RemoveCodebase( 
            /* [in] */ DWORD dwIndex) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE RemoveAll( void) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetCount( 
            /* [out] */ DWORD *pdwCount) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetCodebase( 
            /* [in] */ DWORD dwIndex,
            /* [out] */ DWORD *pdwFlags,
            /* [annotation][out] */ 
            __out_ecount_opt(*pcbCodebase)  LPWSTR wzCodebase,
            /* [out][in] */ DWORD *pcbCodebase) = 0;
        
    };
    
    
#else 	/* C style interface */

    typedef struct ICodebaseListVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            ICodebaseList * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            ICodebaseList * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            ICodebaseList * This);
        
        HRESULT ( STDMETHODCALLTYPE *AddCodebase )( 
            ICodebaseList * This,
            /* [in] */ LPCWSTR wzCodebase,
            /* [in] */ DWORD dwFlags);
        
        HRESULT ( STDMETHODCALLTYPE *RemoveCodebase )( 
            ICodebaseList * This,
            /* [in] */ DWORD dwIndex);
        
        HRESULT ( STDMETHODCALLTYPE *RemoveAll )( 
            ICodebaseList * This);
        
        HRESULT ( STDMETHODCALLTYPE *GetCount )( 
            ICodebaseList * This,
            /* [out] */ DWORD *pdwCount);
        
        HRESULT ( STDMETHODCALLTYPE *GetCodebase )( 
            ICodebaseList * This,
            /* [in] */ DWORD dwIndex,
            /* [out] */ DWORD *pdwFlags,
            /* [annotation][out] */ 
            __out_ecount_opt(*pcbCodebase)  LPWSTR wzCodebase,
            /* [out][in] */ DWORD *pcbCodebase);
        
        END_INTERFACE
    } ICodebaseListVtbl;

    interface ICodebaseList
    {
        CONST_VTBL struct ICodebaseListVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define ICodebaseList_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define ICodebaseList_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define ICodebaseList_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define ICodebaseList_AddCodebase(This,wzCodebase,dwFlags)	\
    ( (This)->lpVtbl -> AddCodebase(This,wzCodebase,dwFlags) ) 

#define ICodebaseList_RemoveCodebase(This,dwIndex)	\
    ( (This)->lpVtbl -> RemoveCodebase(This,dwIndex) ) 

#define ICodebaseList_RemoveAll(This)	\
    ( (This)->lpVtbl -> RemoveAll(This) ) 

#define ICodebaseList_GetCount(This,pdwCount)	\
    ( (This)->lpVtbl -> GetCount(This,pdwCount) ) 

#define ICodebaseList_GetCodebase(This,dwIndex,pdwFlags,wzCodebase,pcbCodebase)	\
    ( (This)->lpVtbl -> GetCodebase(This,dwIndex,pdwFlags,wzCodebase,pcbCodebase) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ICodebaseList_INTERFACE_DEFINED__ */


#ifndef __IDownloadMgr_INTERFACE_DEFINED__
#define __IDownloadMgr_INTERFACE_DEFINED__

/* interface IDownloadMgr */
/* [unique][uuid][object][local] */ 


EXTERN_C const IID IID_IDownloadMgr;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("0A6F16F8-ACD7-11d3-B4ED-00C04F8ECB26")
    IDownloadMgr : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE PreDownloadCheck( 
            /* [out] */ void **ppv,
            /* [out] */ void **ppvNI) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE DoSetup( 
            /* [in] */ LPCWSTR wzSourceUrl,
            /* [in] */ LPCWSTR wzFilePath,
            /* [in] */ const FILETIME *pftLastMod,
            /* [out] */ IUnknown **ppUnk,
            /* [out] */ IUnknown **ppAsmNI) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE ProbeFailed( 
            /* [out] */ IUnknown **ppUnk) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE IsDuplicate( 
            /* [out] */ IDownloadMgr *ppDLMgr) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE LogResult( void) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE DownloadEnabled( 
            /* [out] */ BOOL *pbEnabled) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetBindInfo( 
            /* [out] */ FusionBindInfo *pBindInfo) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE CacheBindingResult( 
            /* [in] */ HRESULT hrResult) = 0;
        
    };
    
    
#else 	/* C style interface */

    typedef struct IDownloadMgrVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            IDownloadMgr * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            IDownloadMgr * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            IDownloadMgr * This);
        
        HRESULT ( STDMETHODCALLTYPE *PreDownloadCheck )( 
            IDownloadMgr * This,
            /* [out] */ void **ppv,
            /* [out] */ void **ppvNI);
        
        HRESULT ( STDMETHODCALLTYPE *DoSetup )( 
            IDownloadMgr * This,
            /* [in] */ LPCWSTR wzSourceUrl,
            /* [in] */ LPCWSTR wzFilePath,
            /* [in] */ const FILETIME *pftLastMod,
            /* [out] */ IUnknown **ppUnk,
            /* [out] */ IUnknown **ppAsmNI);
        
        HRESULT ( STDMETHODCALLTYPE *ProbeFailed )( 
            IDownloadMgr * This,
            /* [out] */ IUnknown **ppUnk);
        
        HRESULT ( STDMETHODCALLTYPE *IsDuplicate )( 
            IDownloadMgr * This,
            /* [out] */ IDownloadMgr *ppDLMgr);
        
        HRESULT ( STDMETHODCALLTYPE *LogResult )( 
            IDownloadMgr * This);
        
        HRESULT ( STDMETHODCALLTYPE *DownloadEnabled )( 
            IDownloadMgr * This,
            /* [out] */ BOOL *pbEnabled);
        
        HRESULT ( STDMETHODCALLTYPE *GetBindInfo )( 
            IDownloadMgr * This,
            /* [out] */ FusionBindInfo *pBindInfo);
        
        HRESULT ( STDMETHODCALLTYPE *CacheBindingResult )( 
            IDownloadMgr * This,
            /* [in] */ HRESULT hrResult);
        
        END_INTERFACE
    } IDownloadMgrVtbl;

    interface IDownloadMgr
    {
        CONST_VTBL struct IDownloadMgrVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define IDownloadMgr_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define IDownloadMgr_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define IDownloadMgr_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define IDownloadMgr_PreDownloadCheck(This,ppv,ppvNI)	\
    ( (This)->lpVtbl -> PreDownloadCheck(This,ppv,ppvNI) ) 

#define IDownloadMgr_DoSetup(This,wzSourceUrl,wzFilePath,pftLastMod,ppUnk,ppAsmNI)	\
    ( (This)->lpVtbl -> DoSetup(This,wzSourceUrl,wzFilePath,pftLastMod,ppUnk,ppAsmNI) ) 

#define IDownloadMgr_ProbeFailed(This,ppUnk)	\
    ( (This)->lpVtbl -> ProbeFailed(This,ppUnk) ) 

#define IDownloadMgr_IsDuplicate(This,ppDLMgr)	\
    ( (This)->lpVtbl -> IsDuplicate(This,ppDLMgr) ) 

#define IDownloadMgr_LogResult(This)	\
    ( (This)->lpVtbl -> LogResult(This) ) 

#define IDownloadMgr_DownloadEnabled(This,pbEnabled)	\
    ( (This)->lpVtbl -> DownloadEnabled(This,pbEnabled) ) 

#define IDownloadMgr_GetBindInfo(This,pBindInfo)	\
    ( (This)->lpVtbl -> GetBindInfo(This,pBindInfo) ) 

#define IDownloadMgr_CacheBindingResult(This,hrResult)	\
    ( (This)->lpVtbl -> CacheBindingResult(This,hrResult) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __IDownloadMgr_INTERFACE_DEFINED__ */


#ifndef __IHostAssembly_INTERFACE_DEFINED__
#define __IHostAssembly_INTERFACE_DEFINED__

/* interface IHostAssembly */
/* [unique][uuid][object][local] */ 

typedef /* [unique] */ IHostAssembly *LPHOSTASSEMBLY;


EXTERN_C const IID IID_IHostAssembly;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("711f7c2d-8234-4505-b02f-7554f46cbf29")
    IHostAssembly : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE GetAssemblyNameDef( 
            /* [out] */ IAssemblyName **ppAssemblyName) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetNextAssemblyNameRef( 
            /* [in] */ DWORD nIndex,
            /* [out] */ IAssemblyName **ppAssemblyName) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetNextAssemblyModule( 
            /* [in] */ DWORD nIndex,
            /* [out] */ IHostAssemblyModuleImport **ppModImport) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetModuleByName( 
            /* [in] */ LPCOLESTR szModuleName,
            /* [out] */ IHostAssemblyModuleImport **ppModImport) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetAssemblyStream( 
            /* [out] */ IStream **ppStreamAsm) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetAssemblyId( 
            /* [out] */ UINT64 *pAssemblyId) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetAssemblyDebugStream( 
            /* [out] */ IStream **ppDebugStream) = 0;
        
        virtual LOADCTX_TYPE STDMETHODCALLTYPE GetFusionLoadContext( void) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetAssemblyContext( 
            /* [out] */ UINT64 *pdwAssemblyContext) = 0;
        
    };
    
    
#else 	/* C style interface */

    typedef struct IHostAssemblyVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            IHostAssembly * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            IHostAssembly * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            IHostAssembly * This);
        
        HRESULT ( STDMETHODCALLTYPE *GetAssemblyNameDef )( 
            IHostAssembly * This,
            /* [out] */ IAssemblyName **ppAssemblyName);
        
        HRESULT ( STDMETHODCALLTYPE *GetNextAssemblyNameRef )( 
            IHostAssembly * This,
            /* [in] */ DWORD nIndex,
            /* [out] */ IAssemblyName **ppAssemblyName);
        
        HRESULT ( STDMETHODCALLTYPE *GetNextAssemblyModule )( 
            IHostAssembly * This,
            /* [in] */ DWORD nIndex,
            /* [out] */ IHostAssemblyModuleImport **ppModImport);
        
        HRESULT ( STDMETHODCALLTYPE *GetModuleByName )( 
            IHostAssembly * This,
            /* [in] */ LPCOLESTR szModuleName,
            /* [out] */ IHostAssemblyModuleImport **ppModImport);
        
        HRESULT ( STDMETHODCALLTYPE *GetAssemblyStream )( 
            IHostAssembly * This,
            /* [out] */ IStream **ppStreamAsm);
        
        HRESULT ( STDMETHODCALLTYPE *GetAssemblyId )( 
            IHostAssembly * This,
            /* [out] */ UINT64 *pAssemblyId);
        
        HRESULT ( STDMETHODCALLTYPE *GetAssemblyDebugStream )( 
            IHostAssembly * This,
            /* [out] */ IStream **ppDebugStream);
        
        LOADCTX_TYPE ( STDMETHODCALLTYPE *GetFusionLoadContext )( 
            IHostAssembly * This);
        
        HRESULT ( STDMETHODCALLTYPE *GetAssemblyContext )( 
            IHostAssembly * This,
            /* [out] */ UINT64 *pdwAssemblyContext);
        
        END_INTERFACE
    } IHostAssemblyVtbl;

    interface IHostAssembly
    {
        CONST_VTBL struct IHostAssemblyVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define IHostAssembly_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define IHostAssembly_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define IHostAssembly_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define IHostAssembly_GetAssemblyNameDef(This,ppAssemblyName)	\
    ( (This)->lpVtbl -> GetAssemblyNameDef(This,ppAssemblyName) ) 

#define IHostAssembly_GetNextAssemblyNameRef(This,nIndex,ppAssemblyName)	\
    ( (This)->lpVtbl -> GetNextAssemblyNameRef(This,nIndex,ppAssemblyName) ) 

#define IHostAssembly_GetNextAssemblyModule(This,nIndex,ppModImport)	\
    ( (This)->lpVtbl -> GetNextAssemblyModule(This,nIndex,ppModImport) ) 

#define IHostAssembly_GetModuleByName(This,szModuleName,ppModImport)	\
    ( (This)->lpVtbl -> GetModuleByName(This,szModuleName,ppModImport) ) 

#define IHostAssembly_GetAssemblyStream(This,ppStreamAsm)	\
    ( (This)->lpVtbl -> GetAssemblyStream(This,ppStreamAsm) ) 

#define IHostAssembly_GetAssemblyId(This,pAssemblyId)	\
    ( (This)->lpVtbl -> GetAssemblyId(This,pAssemblyId) ) 

#define IHostAssembly_GetAssemblyDebugStream(This,ppDebugStream)	\
    ( (This)->lpVtbl -> GetAssemblyDebugStream(This,ppDebugStream) ) 

#define IHostAssembly_GetFusionLoadContext(This)	\
    ( (This)->lpVtbl -> GetFusionLoadContext(This) ) 

#define IHostAssembly_GetAssemblyContext(This,pdwAssemblyContext)	\
    ( (This)->lpVtbl -> GetAssemblyContext(This,pdwAssemblyContext) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __IHostAssembly_INTERFACE_DEFINED__ */


#ifndef __IHostAssemblyModuleImport_INTERFACE_DEFINED__
#define __IHostAssemblyModuleImport_INTERFACE_DEFINED__

/* interface IHostAssemblyModuleImport */
/* [unique][uuid][object][local] */ 

typedef /* [unique] */ IHostAssemblyModuleImport *LPHOSTASSEMBLY_MODULE_IMPORT;


EXTERN_C const IID IID_IHostAssemblyModuleImport;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("b6f2729d-6c0f-4944-b692-e5a2ce2c6e7a")
    IHostAssemblyModuleImport : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE GetModuleName( 
            /* [annotation][size_is][out] */ 
            __out_ecount_full(*pccModuleName)  LPOLESTR szModuleName,
            /* [out][in] */ LPDWORD pccModuleName) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetModuleStream( 
            /* [out] */ IStream **ppStreamModule) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetModuleId( 
            /* [out] */ DWORD *pdwModuleId) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetModuleDebugStream( 
            /* [out] */ IStream **ppDebugStream) = 0;
        
    };
    
    
#else 	/* C style interface */

    typedef struct IHostAssemblyModuleImportVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            IHostAssemblyModuleImport * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            IHostAssemblyModuleImport * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            IHostAssemblyModuleImport * This);
        
        HRESULT ( STDMETHODCALLTYPE *GetModuleName )( 
            IHostAssemblyModuleImport * This,
            /* [annotation][size_is][out] */ 
            __out_ecount_full(*pccModuleName)  LPOLESTR szModuleName,
            /* [out][in] */ LPDWORD pccModuleName);
        
        HRESULT ( STDMETHODCALLTYPE *GetModuleStream )( 
            IHostAssemblyModuleImport * This,
            /* [out] */ IStream **ppStreamModule);
        
        HRESULT ( STDMETHODCALLTYPE *GetModuleId )( 
            IHostAssemblyModuleImport * This,
            /* [out] */ DWORD *pdwModuleId);
        
        HRESULT ( STDMETHODCALLTYPE *GetModuleDebugStream )( 
            IHostAssemblyModuleImport * This,
            /* [out] */ IStream **ppDebugStream);
        
        END_INTERFACE
    } IHostAssemblyModuleImportVtbl;

    interface IHostAssemblyModuleImport
    {
        CONST_VTBL struct IHostAssemblyModuleImportVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define IHostAssemblyModuleImport_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define IHostAssemblyModuleImport_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define IHostAssemblyModuleImport_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define IHostAssemblyModuleImport_GetModuleName(This,szModuleName,pccModuleName)	\
    ( (This)->lpVtbl -> GetModuleName(This,szModuleName,pccModuleName) ) 

#define IHostAssemblyModuleImport_GetModuleStream(This,ppStreamModule)	\
    ( (This)->lpVtbl -> GetModuleStream(This,ppStreamModule) ) 

#define IHostAssemblyModuleImport_GetModuleId(This,pdwModuleId)	\
    ( (This)->lpVtbl -> GetModuleId(This,pdwModuleId) ) 

#define IHostAssemblyModuleImport_GetModuleDebugStream(This,ppDebugStream)	\
    ( (This)->lpVtbl -> GetModuleDebugStream(This,ppDebugStream) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __IHostAssemblyModuleImport_INTERFACE_DEFINED__ */


/* interface __MIDL_itf_fusionpriv_0000_0017 */
/* [local] */ 

STDAPI CreateHistoryReader(LPCWSTR wzFilePath, IHistoryReader **ppHistReader);
STDAPI LookupHistoryAssembly(LPCWSTR pwzFilePath, FILETIME *pftActivationDate, LPCWSTR pwzAsmName, LPCWSTR pwzPublicKeyToken, LPCWSTR wzCulture, LPCWSTR pwzVerRef, IHistoryAssembly **pHistAsm);
STDAPI GetHistoryFileDirectory(__out_ecount_opt(*pdwSize) LPWSTR wzDir, DWORD *pdwSize);
STDAPI PreBindAssembly(IApplicationContext *pAppCtx, IAssemblyName *pName, IAssembly *pAsmParent, IAssemblyName **ppNamePostPolicy, LPVOID pvReserved); 
STDAPI CreateApplicationContext(IAssemblyName *pName, LPAPPLICATIONCONTEXT *ppCtx);             
STDAPI IsRetargetableAssembly(IAssemblyName *pName, BOOL *pbIsRetargetable);             
STDAPI IsOptionallyRetargetableAssembly(IAssemblyName *pName, BOOL *pbIsRetargetable);             
#define EXPLICITBIND_FLAGS_NON_BINDABLE          0x0
#define EXPLICITBIND_FLAGS_EXE                   0x1


extern RPC_IF_HANDLE __MIDL_itf_fusionpriv_0000_0017_v0_0_c_ifspec;
extern RPC_IF_HANDLE __MIDL_itf_fusionpriv_0000_0017_v0_0_s_ifspec;

/* Additional Prototypes for ALL interfaces */

/* end of Additional Prototypes */

#ifdef __cplusplus
}
#endif

#endif


