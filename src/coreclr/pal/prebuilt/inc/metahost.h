

/* this ALWAYS GENERATED file contains the definitions for the interfaces */


 /* File created by MIDL compiler version 8.01.0622 */
/* at Mon Jan 18 19:14:07 2038
 */
/* Compiler settings for metahost.idl:
    Oicf, W1, Zp8, env=Win32 (32b run), target_arch=X86 8.01.0622 
    protocol : dce , ms_ext, c_ext, robust
    error checks: allocation ref bounds_check enum stub_data 
    VC __declspec() decoration level: 
         __declspec(uuid()), __declspec(selectany), __declspec(novtable)
         DECLSPEC_UUID(), MIDL_INTERFACE()
*/
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
#endif /* __RPCNDR_H_VERSION__ */

#ifndef COM_NO_WINDOWS_H
#include "windows.h"
#include "ole2.h"
#endif /*COM_NO_WINDOWS_H*/

#ifndef __metahost_h__
#define __metahost_h__

#if defined(_MSC_VER) && (_MSC_VER >= 1020)
#pragma once
#endif

/* Forward Declarations */ 

#ifndef __ICLRMetaHost_FWD_DEFINED__
#define __ICLRMetaHost_FWD_DEFINED__
typedef interface ICLRMetaHost ICLRMetaHost;

#endif 	/* __ICLRMetaHost_FWD_DEFINED__ */


#ifndef __ICLRDebuggingLibraryProvider_FWD_DEFINED__
#define __ICLRDebuggingLibraryProvider_FWD_DEFINED__
typedef interface ICLRDebuggingLibraryProvider ICLRDebuggingLibraryProvider;

#endif 	/* __ICLRDebuggingLibraryProvider_FWD_DEFINED__ */


#ifndef __ICLRDebuggingLibraryProvider2_FWD_DEFINED__
#define __ICLRDebuggingLibraryProvider2_FWD_DEFINED__
typedef interface ICLRDebuggingLibraryProvider2 ICLRDebuggingLibraryProvider2;

#endif 	/* __ICLRDebuggingLibraryProvider2_FWD_DEFINED__ */


#ifndef __ICLRDebugging_FWD_DEFINED__
#define __ICLRDebugging_FWD_DEFINED__
typedef interface ICLRDebugging ICLRDebugging;

#endif 	/* __ICLRDebugging_FWD_DEFINED__ */


#ifndef __ICLRRuntimeInfo_FWD_DEFINED__
#define __ICLRRuntimeInfo_FWD_DEFINED__
typedef interface ICLRRuntimeInfo ICLRRuntimeInfo;

#endif 	/* __ICLRRuntimeInfo_FWD_DEFINED__ */


/* header files for imported files */
#include "unknwn.h"
#include "oaidl.h"
#include "ocidl.h"
#include "mscoree.h"

#ifdef __cplusplus
extern "C"{
#endif 


/* interface __MIDL_itf_metahost_0000_0000 */
/* [local] */ 

#include <winapifamily.h>
STDAPI CLRCreateInstance(REFCLSID clsid, REFIID riid, /*iid_is(riid)*/ LPVOID *ppInterface);
EXTERN_GUID(IID_ICLRMetaHost, 0xD332DB9E, 0xB9B3, 0x4125, 0x82, 0x07, 0xA1, 0x48, 0x84, 0xF5, 0x32, 0x16);
EXTERN_GUID(CLSID_CLRMetaHost, 0x9280188d, 0xe8e, 0x4867, 0xb3, 0xc, 0x7f, 0xa8, 0x38, 0x84, 0xe8, 0xde);
EXTERN_GUID(IID_ICLRDebugging, 0xd28f3c5a, 0x9634, 0x4206, 0xa5, 0x9, 0x47, 0x75, 0x52, 0xee, 0xfb, 0x10);
EXTERN_GUID(CLSID_CLRDebugging, 0xbacc578d, 0xfbdd, 0x48a4, 0x96, 0x9f, 0x2, 0xd9, 0x32, 0xb7, 0x46, 0x34);
EXTERN_GUID(IID_ICLRRuntimeInfo, 0xBD39D1D2, 0xBA2F, 0x486a, 0x89, 0xB0, 0xB4, 0xB0, 0xCB, 0x46, 0x68, 0x91);
EXTERN_GUID(IID_ICLRDebuggingLibraryProvider, 0x3151c08d, 0x4d09, 0x4f9b, 0x88, 0x38, 0x28, 0x80, 0xbf, 0x18, 0xfe, 0x51);
EXTERN_GUID(IID_ICLRDebuggingLibraryProvider2, 0xE04E2FF1, 0xDCFD, 0x45D5, 0xBC, 0xD1, 0x16, 0xFF, 0xF2, 0xFA, 0xF7, 0xBA);

typedef HRESULT ( __stdcall *CallbackThreadSetFnPtr )( void);

typedef HRESULT ( __stdcall *CallbackThreadUnsetFnPtr )( void);

typedef void ( __stdcall *RuntimeLoadedCallbackFnPtr )( 
    ICLRRuntimeInfo *pRuntimeInfo,
    CallbackThreadSetFnPtr pfnCallbackThreadSet,
    CallbackThreadUnsetFnPtr pfnCallbackThreadUnset);



extern RPC_IF_HANDLE __MIDL_itf_metahost_0000_0000_v0_0_c_ifspec;
extern RPC_IF_HANDLE __MIDL_itf_metahost_0000_0000_v0_0_s_ifspec;

#ifndef __ICLRMetaHost_INTERFACE_DEFINED__
#define __ICLRMetaHost_INTERFACE_DEFINED__

/* interface ICLRMetaHost */
/* [object][local][helpstring][version][uuid] */ 


EXTERN_C const IID IID_ICLRMetaHost;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("D332DB9E-B9B3-4125-8207-A14884F53216")
    ICLRMetaHost : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE GetRuntime( 
            /* [in] */ LPCWSTR pwzVersion,
            /* [in] */ REFIID riid,
            /* [retval][iid_is][out] */ LPVOID *ppRuntime) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetVersionFromFile( 
            /* [in] */ LPCWSTR pwzFilePath,
            /* [annotation][size_is][out] */ 
            _Out_writes_all_(*pcchBuffer)  LPWSTR pwzBuffer,
            /* [out][in] */ DWORD *pcchBuffer) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE EnumerateInstalledRuntimes( 
            /* [retval][out] */ IEnumUnknown **ppEnumerator) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE EnumerateLoadedRuntimes( 
            /* [in] */ HANDLE hndProcess,
            /* [retval][out] */ IEnumUnknown **ppEnumerator) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE RequestRuntimeLoadedNotification( 
            /* [in] */ RuntimeLoadedCallbackFnPtr pCallbackFunction) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE QueryLegacyV2RuntimeBinding( 
            /* [in] */ REFIID riid,
            /* [retval][iid_is][out] */ LPVOID *ppUnk) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE ExitProcess( 
            /* [in] */ INT32 iExitCode) = 0;
        
    };
    
    
#else 	/* C style interface */

    typedef struct ICLRMetaHostVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            ICLRMetaHost * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            ICLRMetaHost * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            ICLRMetaHost * This);
        
        HRESULT ( STDMETHODCALLTYPE *GetRuntime )( 
            ICLRMetaHost * This,
            /* [in] */ LPCWSTR pwzVersion,
            /* [in] */ REFIID riid,
            /* [retval][iid_is][out] */ LPVOID *ppRuntime);
        
        HRESULT ( STDMETHODCALLTYPE *GetVersionFromFile )( 
            ICLRMetaHost * This,
            /* [in] */ LPCWSTR pwzFilePath,
            /* [annotation][size_is][out] */ 
            _Out_writes_all_(*pcchBuffer)  LPWSTR pwzBuffer,
            /* [out][in] */ DWORD *pcchBuffer);
        
        HRESULT ( STDMETHODCALLTYPE *EnumerateInstalledRuntimes )( 
            ICLRMetaHost * This,
            /* [retval][out] */ IEnumUnknown **ppEnumerator);
        
        HRESULT ( STDMETHODCALLTYPE *EnumerateLoadedRuntimes )( 
            ICLRMetaHost * This,
            /* [in] */ HANDLE hndProcess,
            /* [retval][out] */ IEnumUnknown **ppEnumerator);
        
        HRESULT ( STDMETHODCALLTYPE *RequestRuntimeLoadedNotification )( 
            ICLRMetaHost * This,
            /* [in] */ RuntimeLoadedCallbackFnPtr pCallbackFunction);
        
        HRESULT ( STDMETHODCALLTYPE *QueryLegacyV2RuntimeBinding )( 
            ICLRMetaHost * This,
            /* [in] */ REFIID riid,
            /* [retval][iid_is][out] */ LPVOID *ppUnk);
        
        HRESULT ( STDMETHODCALLTYPE *ExitProcess )( 
            ICLRMetaHost * This,
            /* [in] */ INT32 iExitCode);
        
        END_INTERFACE
    } ICLRMetaHostVtbl;

    interface ICLRMetaHost
    {
        CONST_VTBL struct ICLRMetaHostVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define ICLRMetaHost_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define ICLRMetaHost_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define ICLRMetaHost_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define ICLRMetaHost_GetRuntime(This,pwzVersion,riid,ppRuntime)	\
    ( (This)->lpVtbl -> GetRuntime(This,pwzVersion,riid,ppRuntime) ) 

#define ICLRMetaHost_GetVersionFromFile(This,pwzFilePath,pwzBuffer,pcchBuffer)	\
    ( (This)->lpVtbl -> GetVersionFromFile(This,pwzFilePath,pwzBuffer,pcchBuffer) ) 

#define ICLRMetaHost_EnumerateInstalledRuntimes(This,ppEnumerator)	\
    ( (This)->lpVtbl -> EnumerateInstalledRuntimes(This,ppEnumerator) ) 

#define ICLRMetaHost_EnumerateLoadedRuntimes(This,hndProcess,ppEnumerator)	\
    ( (This)->lpVtbl -> EnumerateLoadedRuntimes(This,hndProcess,ppEnumerator) ) 

#define ICLRMetaHost_RequestRuntimeLoadedNotification(This,pCallbackFunction)	\
    ( (This)->lpVtbl -> RequestRuntimeLoadedNotification(This,pCallbackFunction) ) 

#define ICLRMetaHost_QueryLegacyV2RuntimeBinding(This,riid,ppUnk)	\
    ( (This)->lpVtbl -> QueryLegacyV2RuntimeBinding(This,riid,ppUnk) ) 

#define ICLRMetaHost_ExitProcess(This,iExitCode)	\
    ( (This)->lpVtbl -> ExitProcess(This,iExitCode) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ICLRMetaHost_INTERFACE_DEFINED__ */


/* interface __MIDL_itf_metahost_0000_0001 */
/* [local] */ 

typedef struct _CLR_DEBUGGING_VERSION
    {
    WORD wStructVersion;
    WORD wMajor;
    WORD wMinor;
    WORD wBuild;
    WORD wRevision;
    } 	CLR_DEBUGGING_VERSION;

typedef /* [public][public] */ 
enum __MIDL___MIDL_itf_metahost_0000_0001_0001
    {
        CLR_DEBUGGING_MANAGED_EVENT_PENDING	= 1,
        CLR_DEBUGGING_MANAGED_EVENT_DEBUGGER_LAUNCH	= 2
    } 	CLR_DEBUGGING_PROCESS_FLAGS;



extern RPC_IF_HANDLE __MIDL_itf_metahost_0000_0001_v0_0_c_ifspec;
extern RPC_IF_HANDLE __MIDL_itf_metahost_0000_0001_v0_0_s_ifspec;

#ifndef __ICLRDebuggingLibraryProvider_INTERFACE_DEFINED__
#define __ICLRDebuggingLibraryProvider_INTERFACE_DEFINED__

/* interface ICLRDebuggingLibraryProvider */
/* [object][local][helpstring][version][uuid] */ 


EXTERN_C const IID IID_ICLRDebuggingLibraryProvider;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("3151C08D-4D09-4f9b-8838-2880BF18FE51")
    ICLRDebuggingLibraryProvider : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE ProvideLibrary( 
            /* [in] */ const WCHAR *pwszFileName,
            /* [in] */ DWORD dwTimestamp,
            /* [in] */ DWORD dwSizeOfImage,
            /* [out] */ HMODULE *phModule) = 0;
        
    };
    
    
#else 	/* C style interface */

    typedef struct ICLRDebuggingLibraryProviderVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            ICLRDebuggingLibraryProvider * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            ICLRDebuggingLibraryProvider * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            ICLRDebuggingLibraryProvider * This);
        
        HRESULT ( STDMETHODCALLTYPE *ProvideLibrary )( 
            ICLRDebuggingLibraryProvider * This,
            /* [in] */ const WCHAR *pwszFileName,
            /* [in] */ DWORD dwTimestamp,
            /* [in] */ DWORD dwSizeOfImage,
            /* [out] */ HMODULE *phModule);
        
        END_INTERFACE
    } ICLRDebuggingLibraryProviderVtbl;

    interface ICLRDebuggingLibraryProvider
    {
        CONST_VTBL struct ICLRDebuggingLibraryProviderVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define ICLRDebuggingLibraryProvider_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define ICLRDebuggingLibraryProvider_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define ICLRDebuggingLibraryProvider_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define ICLRDebuggingLibraryProvider_ProvideLibrary(This,pwszFileName,dwTimestamp,dwSizeOfImage,phModule)	\
    ( (This)->lpVtbl -> ProvideLibrary(This,pwszFileName,dwTimestamp,dwSizeOfImage,phModule) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ICLRDebuggingLibraryProvider_INTERFACE_DEFINED__ */


#ifndef __ICLRDebuggingLibraryProvider2_INTERFACE_DEFINED__
#define __ICLRDebuggingLibraryProvider2_INTERFACE_DEFINED__

/* interface ICLRDebuggingLibraryProvider2 */
/* [object][local][helpstring][version][uuid] */ 


EXTERN_C const IID IID_ICLRDebuggingLibraryProvider2;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("E04E2FF1-DCFD-45D5-BCD1-16FFF2FAF7BA")
    ICLRDebuggingLibraryProvider2 : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE ProvideLibrary2( 
            /* [in] */ const WCHAR *pwszFileName,
            /* [in] */ DWORD dwTimestamp,
            /* [in] */ DWORD dwSizeOfImage,
            /* [out] */ LPWSTR *ppResolvedModulePath) = 0;
        
    };
    
    
#else 	/* C style interface */

    typedef struct ICLRDebuggingLibraryProvider2Vtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            ICLRDebuggingLibraryProvider2 * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            ICLRDebuggingLibraryProvider2 * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            ICLRDebuggingLibraryProvider2 * This);
        
        HRESULT ( STDMETHODCALLTYPE *ProvideLibrary2 )( 
            ICLRDebuggingLibraryProvider2 * This,
            /* [in] */ const WCHAR *pwszFileName,
            /* [in] */ DWORD dwTimestamp,
            /* [in] */ DWORD dwSizeOfImage,
            /* [out] */ LPWSTR *ppResolvedModulePath);
        
        END_INTERFACE
    } ICLRDebuggingLibraryProvider2Vtbl;

    interface ICLRDebuggingLibraryProvider2
    {
        CONST_VTBL struct ICLRDebuggingLibraryProvider2Vtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define ICLRDebuggingLibraryProvider2_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define ICLRDebuggingLibraryProvider2_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define ICLRDebuggingLibraryProvider2_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define ICLRDebuggingLibraryProvider2_ProvideLibrary2(This,pwszFileName,dwTimestamp,dwSizeOfImage,ppResolvedModulePath)	\
    ( (This)->lpVtbl -> ProvideLibrary2(This,pwszFileName,dwTimestamp,dwSizeOfImage,ppResolvedModulePath) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ICLRDebuggingLibraryProvider2_INTERFACE_DEFINED__ */


#ifndef __ICLRDebugging_INTERFACE_DEFINED__
#define __ICLRDebugging_INTERFACE_DEFINED__

/* interface ICLRDebugging */
/* [object][local][helpstring][version][uuid] */ 


EXTERN_C const IID IID_ICLRDebugging;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("D28F3C5A-9634-4206-A509-477552EEFB10")
    ICLRDebugging : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE OpenVirtualProcess( 
            /* [in] */ ULONG64 moduleBaseAddress,
            /* [in] */ IUnknown *pDataTarget,
            /* [in] */ ICLRDebuggingLibraryProvider *pLibraryProvider,
            /* [in] */ CLR_DEBUGGING_VERSION *pMaxDebuggerSupportedVersion,
            /* [in] */ REFIID riidProcess,
            /* [iid_is][out] */ IUnknown **ppProcess,
            /* [out][in] */ CLR_DEBUGGING_VERSION *pVersion,
            /* [out] */ CLR_DEBUGGING_PROCESS_FLAGS *pdwFlags) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE CanUnloadNow( 
            HMODULE hModule) = 0;
        
    };
    
    
#else 	/* C style interface */

    typedef struct ICLRDebuggingVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            ICLRDebugging * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            ICLRDebugging * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            ICLRDebugging * This);
        
        HRESULT ( STDMETHODCALLTYPE *OpenVirtualProcess )( 
            ICLRDebugging * This,
            /* [in] */ ULONG64 moduleBaseAddress,
            /* [in] */ IUnknown *pDataTarget,
            /* [in] */ ICLRDebuggingLibraryProvider *pLibraryProvider,
            /* [in] */ CLR_DEBUGGING_VERSION *pMaxDebuggerSupportedVersion,
            /* [in] */ REFIID riidProcess,
            /* [iid_is][out] */ IUnknown **ppProcess,
            /* [out][in] */ CLR_DEBUGGING_VERSION *pVersion,
            /* [out] */ CLR_DEBUGGING_PROCESS_FLAGS *pdwFlags);
        
        HRESULT ( STDMETHODCALLTYPE *CanUnloadNow )( 
            ICLRDebugging * This,
            HMODULE hModule);
        
        END_INTERFACE
    } ICLRDebuggingVtbl;

    interface ICLRDebugging
    {
        CONST_VTBL struct ICLRDebuggingVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define ICLRDebugging_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define ICLRDebugging_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define ICLRDebugging_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define ICLRDebugging_OpenVirtualProcess(This,moduleBaseAddress,pDataTarget,pLibraryProvider,pMaxDebuggerSupportedVersion,riidProcess,ppProcess,pVersion,pdwFlags)	\
    ( (This)->lpVtbl -> OpenVirtualProcess(This,moduleBaseAddress,pDataTarget,pLibraryProvider,pMaxDebuggerSupportedVersion,riidProcess,ppProcess,pVersion,pdwFlags) ) 

#define ICLRDebugging_CanUnloadNow(This,hModule)	\
    ( (This)->lpVtbl -> CanUnloadNow(This,hModule) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ICLRDebugging_INTERFACE_DEFINED__ */


#ifndef __ICLRRuntimeInfo_INTERFACE_DEFINED__
#define __ICLRRuntimeInfo_INTERFACE_DEFINED__

/* interface ICLRRuntimeInfo */
/* [object][local][helpstring][version][uuid] */ 


EXTERN_C const IID IID_ICLRRuntimeInfo;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("BD39D1D2-BA2F-486a-89B0-B4B0CB466891")
    ICLRRuntimeInfo : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE GetVersionString( 
            /* [annotation][size_is][out] */ 
            _Out_writes_all_opt_(*pcchBuffer)  LPWSTR pwzBuffer,
            /* [out][in] */ DWORD *pcchBuffer) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetRuntimeDirectory( 
            /* [annotation][size_is][out] */ 
            _Out_writes_all_(*pcchBuffer)  LPWSTR pwzBuffer,
            /* [out][in] */ DWORD *pcchBuffer) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE IsLoaded( 
            /* [in] */ HANDLE hndProcess,
            /* [retval][out] */ BOOL *pbLoaded) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE LoadErrorString( 
            /* [in] */ UINT iResourceID,
            /* [annotation][size_is][out] */ 
            _Out_writes_all_(*pcchBuffer)  LPWSTR pwzBuffer,
            /* [out][in] */ DWORD *pcchBuffer,
            /* [lcid][in] */ LONG iLocaleID) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE LoadLibrary( 
            /* [in] */ LPCWSTR pwzDllName,
            /* [retval][out] */ HMODULE *phndModule) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetProcAddress( 
            /* [in] */ LPCSTR pszProcName,
            /* [retval][out] */ LPVOID *ppProc) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetInterface( 
            /* [in] */ REFCLSID rclsid,
            /* [in] */ REFIID riid,
            /* [retval][iid_is][out] */ LPVOID *ppUnk) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE IsLoadable( 
            /* [retval][out] */ BOOL *pbLoadable) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE SetDefaultStartupFlags( 
            /* [in] */ DWORD dwStartupFlags,
            /* [in] */ LPCWSTR pwzHostConfigFile) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetDefaultStartupFlags( 
            /* [out] */ DWORD *pdwStartupFlags,
            /* [annotation][size_is][out] */ 
            _Out_writes_all_opt_(*pcchHostConfigFile)  LPWSTR pwzHostConfigFile,
            /* [out][in] */ DWORD *pcchHostConfigFile) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE BindAsLegacyV2Runtime( void) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE IsStarted( 
            /* [out] */ BOOL *pbStarted,
            /* [out] */ DWORD *pdwStartupFlags) = 0;
        
    };
    
    
#else 	/* C style interface */

    typedef struct ICLRRuntimeInfoVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            ICLRRuntimeInfo * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            ICLRRuntimeInfo * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            ICLRRuntimeInfo * This);
        
        HRESULT ( STDMETHODCALLTYPE *GetVersionString )( 
            ICLRRuntimeInfo * This,
            /* [annotation][size_is][out] */ 
            _Out_writes_all_opt_(*pcchBuffer)  LPWSTR pwzBuffer,
            /* [out][in] */ DWORD *pcchBuffer);
        
        HRESULT ( STDMETHODCALLTYPE *GetRuntimeDirectory )( 
            ICLRRuntimeInfo * This,
            /* [annotation][size_is][out] */ 
            _Out_writes_all_(*pcchBuffer)  LPWSTR pwzBuffer,
            /* [out][in] */ DWORD *pcchBuffer);
        
        HRESULT ( STDMETHODCALLTYPE *IsLoaded )( 
            ICLRRuntimeInfo * This,
            /* [in] */ HANDLE hndProcess,
            /* [retval][out] */ BOOL *pbLoaded);
        
        HRESULT ( STDMETHODCALLTYPE *LoadErrorString )( 
            ICLRRuntimeInfo * This,
            /* [in] */ UINT iResourceID,
            /* [annotation][size_is][out] */ 
            _Out_writes_all_(*pcchBuffer)  LPWSTR pwzBuffer,
            /* [out][in] */ DWORD *pcchBuffer,
            /* [lcid][in] */ LONG iLocaleID);
        
        HRESULT ( STDMETHODCALLTYPE *LoadLibrary )( 
            ICLRRuntimeInfo * This,
            /* [in] */ LPCWSTR pwzDllName,
            /* [retval][out] */ HMODULE *phndModule);
        
        HRESULT ( STDMETHODCALLTYPE *GetProcAddress )( 
            ICLRRuntimeInfo * This,
            /* [in] */ LPCSTR pszProcName,
            /* [retval][out] */ LPVOID *ppProc);
        
        HRESULT ( STDMETHODCALLTYPE *GetInterface )( 
            ICLRRuntimeInfo * This,
            /* [in] */ REFCLSID rclsid,
            /* [in] */ REFIID riid,
            /* [retval][iid_is][out] */ LPVOID *ppUnk);
        
        HRESULT ( STDMETHODCALLTYPE *IsLoadable )( 
            ICLRRuntimeInfo * This,
            /* [retval][out] */ BOOL *pbLoadable);
        
        HRESULT ( STDMETHODCALLTYPE *SetDefaultStartupFlags )( 
            ICLRRuntimeInfo * This,
            /* [in] */ DWORD dwStartupFlags,
            /* [in] */ LPCWSTR pwzHostConfigFile);
        
        HRESULT ( STDMETHODCALLTYPE *GetDefaultStartupFlags )( 
            ICLRRuntimeInfo * This,
            /* [out] */ DWORD *pdwStartupFlags,
            /* [annotation][size_is][out] */ 
            _Out_writes_all_opt_(*pcchHostConfigFile)  LPWSTR pwzHostConfigFile,
            /* [out][in] */ DWORD *pcchHostConfigFile);
        
        HRESULT ( STDMETHODCALLTYPE *BindAsLegacyV2Runtime )( 
            ICLRRuntimeInfo * This);
        
        HRESULT ( STDMETHODCALLTYPE *IsStarted )( 
            ICLRRuntimeInfo * This,
            /* [out] */ BOOL *pbStarted,
            /* [out] */ DWORD *pdwStartupFlags);
        
        END_INTERFACE
    } ICLRRuntimeInfoVtbl;

    interface ICLRRuntimeInfo
    {
        CONST_VTBL struct ICLRRuntimeInfoVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define ICLRRuntimeInfo_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define ICLRRuntimeInfo_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define ICLRRuntimeInfo_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define ICLRRuntimeInfo_GetVersionString(This,pwzBuffer,pcchBuffer)	\
    ( (This)->lpVtbl -> GetVersionString(This,pwzBuffer,pcchBuffer) ) 

#define ICLRRuntimeInfo_GetRuntimeDirectory(This,pwzBuffer,pcchBuffer)	\
    ( (This)->lpVtbl -> GetRuntimeDirectory(This,pwzBuffer,pcchBuffer) ) 

#define ICLRRuntimeInfo_IsLoaded(This,hndProcess,pbLoaded)	\
    ( (This)->lpVtbl -> IsLoaded(This,hndProcess,pbLoaded) ) 

#define ICLRRuntimeInfo_LoadErrorString(This,iResourceID,pwzBuffer,pcchBuffer,iLocaleID)	\
    ( (This)->lpVtbl -> LoadErrorString(This,iResourceID,pwzBuffer,pcchBuffer,iLocaleID) ) 

#define ICLRRuntimeInfo_LoadLibrary(This,pwzDllName,phndModule)	\
    ( (This)->lpVtbl -> LoadLibrary(This,pwzDllName,phndModule) ) 

#define ICLRRuntimeInfo_GetProcAddress(This,pszProcName,ppProc)	\
    ( (This)->lpVtbl -> GetProcAddress(This,pszProcName,ppProc) ) 

#define ICLRRuntimeInfo_GetInterface(This,rclsid,riid,ppUnk)	\
    ( (This)->lpVtbl -> GetInterface(This,rclsid,riid,ppUnk) ) 

#define ICLRRuntimeInfo_IsLoadable(This,pbLoadable)	\
    ( (This)->lpVtbl -> IsLoadable(This,pbLoadable) ) 

#define ICLRRuntimeInfo_SetDefaultStartupFlags(This,dwStartupFlags,pwzHostConfigFile)	\
    ( (This)->lpVtbl -> SetDefaultStartupFlags(This,dwStartupFlags,pwzHostConfigFile) ) 

#define ICLRRuntimeInfo_GetDefaultStartupFlags(This,pdwStartupFlags,pwzHostConfigFile,pcchHostConfigFile)	\
    ( (This)->lpVtbl -> GetDefaultStartupFlags(This,pdwStartupFlags,pwzHostConfigFile,pcchHostConfigFile) ) 

#define ICLRRuntimeInfo_BindAsLegacyV2Runtime(This)	\
    ( (This)->lpVtbl -> BindAsLegacyV2Runtime(This) ) 

#define ICLRRuntimeInfo_IsStarted(This,pbStarted,pdwStartupFlags)	\
    ( (This)->lpVtbl -> IsStarted(This,pbStarted,pdwStartupFlags) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ICLRRuntimeInfo_INTERFACE_DEFINED__ */


/* Additional Prototypes for ALL interfaces */

/* end of Additional Prototypes */

#ifdef __cplusplus
}
#endif

#endif


