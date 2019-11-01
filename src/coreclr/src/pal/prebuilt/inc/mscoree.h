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
#endif /* __RPCNDR_H_VERSION__ */

#ifndef COM_NO_WINDOWS_H
#include "windows.h"
#include "ole2.h"
#endif /*COM_NO_WINDOWS_H*/

#ifndef __mscoree_h__
#define __mscoree_h__

#if defined(_MSC_VER) && (_MSC_VER >= 1020)
#pragma once
#endif

/* Forward Declarations */ 

#ifndef __ICLRRuntimeHost_FWD_DEFINED__
#define __ICLRRuntimeHost_FWD_DEFINED__
typedef interface ICLRRuntimeHost ICLRRuntimeHost;

#endif 	/* __ICLRRuntimeHost_FWD_DEFINED__ */


#ifndef __ICLRRuntimeHost2_FWD_DEFINED__
#define __ICLRRuntimeHost2_FWD_DEFINED__
typedef interface ICLRRuntimeHost2 ICLRRuntimeHost2;

#endif 	/* __ICLRRuntimeHost2_FWD_DEFINED__ */


#ifndef __ICLRRuntimeHost4_FWD_DEFINED__
#define __ICLRRuntimeHost4_FWD_DEFINED__
typedef interface ICLRRuntimeHost4 ICLRRuntimeHost4;

#endif 	/* __ICLRRuntimeHost4_FWD_DEFINED__ */


/* header files for imported files */
#include "unknwn.h"

#ifdef __cplusplus
extern "C"{
#endif 


/* interface __MIDL_itf_mscoree_0000_0000 */
/* [local] */ 

#define DECLARE_DEPRECATED 
#define DEPRECATED_CLR_STDAPI STDAPI

struct IActivationFactory;

struct IHostControl;

struct ICLRControl;

EXTERN_GUID(CLSID_ComCallUnmarshalV4, 0x45fb4600,0xe6e8,0x4928,0xb2,0x5e,0x50,0x47,0x6f,0xf7,0x94,0x25);
EXTERN_GUID(IID_ICLRRuntimeHost, 0x90F1A06C, 0x7712, 0x4762, 0x86, 0xB5, 0x7A, 0x5E, 0xBA, 0x6B, 0xDB, 0x02);
EXTERN_GUID(IID_ICLRRuntimeHost2, 0x712AB73F, 0x2C22, 0x4807, 0xAD, 0x7E, 0xF5, 0x01, 0xD7, 0xb7, 0x2C, 0x2D);
EXTERN_GUID(IID_ICLRRuntimeHost4, 0x64F6D366, 0xD7C2, 0x4F1F, 0xB4, 0xB2, 0xE8, 0x16, 0x0C, 0xAC, 0x43, 0xAF);
typedef HRESULT  (STDAPICALLTYPE *FnGetCLRRuntimeHost)(REFIID riid, IUnknown **pUnk);
typedef HRESULT ( __stdcall *FExecuteInAppDomainCallback )( 
    void *cookie);

typedef /* [public][public] */ 
enum __MIDL___MIDL_itf_mscoree_0000_0000_0001
    {
        STARTUP_CONCURRENT_GC	= 0x1,
        STARTUP_LOADER_OPTIMIZATION_MASK	= ( 0x3 << 1 ) ,
        STARTUP_LOADER_OPTIMIZATION_SINGLE_DOMAIN	= ( 0x1 << 1 ) ,
        STARTUP_LOADER_OPTIMIZATION_MULTI_DOMAIN	= ( 0x2 << 1 ) ,
        STARTUP_LOADER_OPTIMIZATION_MULTI_DOMAIN_HOST	= ( 0x3 << 1 ) ,
        STARTUP_LOADER_SAFEMODE	= 0x10,
        STARTUP_LOADER_SETPREFERENCE	= 0x100,
        STARTUP_SERVER_GC	= 0x1000,
        STARTUP_HOARD_GC_VM	= 0x2000,
        STARTUP_SINGLE_VERSION_HOSTING_INTERFACE	= 0x4000,
        STARTUP_LEGACY_IMPERSONATION	= 0x10000,
        STARTUP_DISABLE_COMMITTHREADSTACK	= 0x20000,
        STARTUP_ALWAYSFLOW_IMPERSONATION	= 0x40000,
        STARTUP_TRIM_GC_COMMIT	= 0x80000,
        STARTUP_ETW	= 0x100000,
        STARTUP_ARM	= 0x400000,
        STARTUP_SINGLE_APPDOMAIN	= 0x800000,
        STARTUP_APPX_APP_MODEL	= 0x1000000,
        STARTUP_DISABLE_RANDOMIZED_STRING_HASHING	= 0x2000000
    } 	STARTUP_FLAGS;

typedef /* [public] */ 
enum __MIDL___MIDL_itf_mscoree_0000_0000_0002
    {
        APPDOMAIN_SECURITY_DEFAULT	= 0,
        APPDOMAIN_SECURITY_SANDBOXED	= 0x1,
        APPDOMAIN_SECURITY_FORBID_CROSSAD_REVERSE_PINVOKE	= 0x2,
        APPDOMAIN_IGNORE_UNHANDLED_EXCEPTIONS	= 0x4,
        APPDOMAIN_FORCE_TRIVIAL_WAIT_OPERATIONS	= 0x8,
        APPDOMAIN_ENABLE_PINVOKE_AND_CLASSIC_COMINTEROP	= 0x10,
        APPDOMAIN_ENABLE_PLATFORM_SPECIFIC_APPS	= 0x40,
        APPDOMAIN_ENABLE_ASSEMBLY_LOADFILE	= 0x80,
        APPDOMAIN_DISABLE_TRANSPARENCY_ENFORCEMENT	= 0x100
    } 	APPDOMAIN_SECURITY_FLAGS;

typedef /* [public] */ 
enum __MIDL___MIDL_itf_mscoree_0000_0000_0003
    {
        WAIT_MSGPUMP	= 0x1,
        WAIT_ALERTABLE	= 0x2,
        WAIT_NOTINDEADLOCK	= 0x4
    } 	WAIT_OPTION;

typedef 
enum ETaskType
    {
        TT_DEBUGGERHELPER	= 0x1,
        TT_GC	= 0x2,
        TT_FINALIZER	= 0x4,
        TT_THREADPOOL_TIMER	= 0x8,
        TT_THREADPOOL_GATE	= 0x10,
        TT_THREADPOOL_WORKER	= 0x20,
        TT_THREADPOOL_IOCOMPLETION	= 0x40,
        TT_ADUNLOAD	= 0x80,
        TT_USER	= 0x100,
        TT_THREADPOOL_WAIT	= 0x200,
        TT_UNKNOWN	= 0x80000000
    } 	ETaskType;

typedef /* [public] */ 
enum __MIDL___MIDL_itf_mscoree_0000_0000_0004
    {
        DUMP_FLAVOR_Mini	= 0,
        DUMP_FLAVOR_CriticalCLRState	= 1,
        DUMP_FLAVOR_NonHeapCLRState	= 2,
        DUMP_FLAVOR_Default	= DUMP_FLAVOR_Mini
    } 	ECustomDumpFlavor;

#define	BucketParamsCount	( 10 )

#define	BucketParamLength	( 255 )

typedef /* [public] */ 
enum __MIDL___MIDL_itf_mscoree_0000_0000_0005
    {
        Parameter1	= 0,
        Parameter2	= ( Parameter1 + 1 ) ,
        Parameter3	= ( Parameter2 + 1 ) ,
        Parameter4	= ( Parameter3 + 1 ) ,
        Parameter5	= ( Parameter4 + 1 ) ,
        Parameter6	= ( Parameter5 + 1 ) ,
        Parameter7	= ( Parameter6 + 1 ) ,
        Parameter8	= ( Parameter7 + 1 ) ,
        Parameter9	= ( Parameter8 + 1 ) ,
        InvalidBucketParamIndex	= ( Parameter9 + 1 ) 
    } 	BucketParameterIndex;

typedef struct _BucketParameters
    {
    BOOL fInited;
    WCHAR pszEventTypeName[ 255 ];
    WCHAR pszParams[ 10 ][ 255 ];
    } 	BucketParameters;

typedef /* [public] */ 
enum __MIDL___MIDL_itf_mscoree_0000_0000_0006
    {
        OPR_ThreadAbort	= 0,
        OPR_ThreadRudeAbortInNonCriticalRegion	= ( OPR_ThreadAbort + 1 ) ,
        OPR_ThreadRudeAbortInCriticalRegion	= ( OPR_ThreadRudeAbortInNonCriticalRegion + 1 ) ,
        OPR_AppDomainUnload	= ( OPR_ThreadRudeAbortInCriticalRegion + 1 ) ,
        OPR_AppDomainRudeUnload	= ( OPR_AppDomainUnload + 1 ) ,
        OPR_ProcessExit	= ( OPR_AppDomainRudeUnload + 1 ) ,
        OPR_FinalizerRun	= ( OPR_ProcessExit + 1 ) ,
        MaxClrOperation	= ( OPR_FinalizerRun + 1 ) 
    } 	EClrOperation;

typedef /* [public] */ 
enum __MIDL___MIDL_itf_mscoree_0000_0000_0007
    {
        FAIL_NonCriticalResource	= 0,
        FAIL_CriticalResource	= ( FAIL_NonCriticalResource + 1 ) ,
        FAIL_FatalRuntime	= ( FAIL_CriticalResource + 1 ) ,
        FAIL_OrphanedLock	= ( FAIL_FatalRuntime + 1 ) ,
        FAIL_StackOverflow	= ( FAIL_OrphanedLock + 1 ) ,
        FAIL_AccessViolation	= ( FAIL_StackOverflow + 1 ) ,
        FAIL_CodeContract	= ( FAIL_AccessViolation + 1 ) ,
        MaxClrFailure	= ( FAIL_CodeContract + 1 ) 
    } 	EClrFailure;

typedef /* [public] */ 
enum __MIDL___MIDL_itf_mscoree_0000_0000_0008
    {
        eRuntimeDeterminedPolicy	= 0,
        eHostDeterminedPolicy	= ( eRuntimeDeterminedPolicy + 1 ) 
    } 	EClrUnhandledException;

typedef /* [public] */ 
enum __MIDL___MIDL_itf_mscoree_0000_0000_0009
    {
        eNoAction	= 0,
        eThrowException	= ( eNoAction + 1 ) ,
        eAbortThread	= ( eThrowException + 1 ) ,
        eRudeAbortThread	= ( eAbortThread + 1 ) ,
        eUnloadAppDomain	= ( eRudeAbortThread + 1 ) ,
        eRudeUnloadAppDomain	= ( eUnloadAppDomain + 1 ) ,
        eExitProcess	= ( eRudeUnloadAppDomain + 1 ) ,
        eFastExitProcess	= ( eExitProcess + 1 ) ,
        eRudeExitProcess	= ( eFastExitProcess + 1 ) ,
        MaxPolicyAction	= (eRudeExitProcess + 1 )
    } 	EPolicyAction;



extern RPC_IF_HANDLE __MIDL_itf_mscoree_0000_0000_v0_0_c_ifspec;
extern RPC_IF_HANDLE __MIDL_itf_mscoree_0000_0000_v0_0_s_ifspec;

#ifndef __ICLRRuntimeHost_INTERFACE_DEFINED__
#define __ICLRRuntimeHost_INTERFACE_DEFINED__

/* interface ICLRRuntimeHost */
/* [object][local][unique][helpstring][version][uuid] */ 


EXTERN_C const IID IID_ICLRRuntimeHost;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("90F1A06C-7712-4762-86B5-7A5EBA6BDB02")
    ICLRRuntimeHost : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE Start( void) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE Stop( void) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE SetHostControl( 
            /* [in] */ IHostControl *pHostControl) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetCLRControl( 
            /* [out] */ ICLRControl **pCLRControl) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE UnloadAppDomain( 
            /* [in] */ DWORD dwAppDomainId,
            /* [in] */ BOOL fWaitUntilDone) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE ExecuteInAppDomain( 
            /* [in] */ DWORD dwAppDomainId,
            /* [in] */ FExecuteInAppDomainCallback pCallback,
            /* [in] */ void *cookie) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetCurrentAppDomainId( 
            /* [out] */ DWORD *pdwAppDomainId) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE ExecuteApplication( 
            /* [in] */ LPCWSTR pwzAppFullName,
            /* [in] */ DWORD dwManifestPaths,
            /* [in] */ LPCWSTR *ppwzManifestPaths,
            /* [in] */ DWORD dwActivationData,
            /* [in] */ LPCWSTR *ppwzActivationData,
            /* [out] */ int *pReturnValue) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE ExecuteInDefaultAppDomain( 
            /* [in] */ LPCWSTR pwzAssemblyPath,
            /* [in] */ LPCWSTR pwzTypeName,
            /* [in] */ LPCWSTR pwzMethodName,
            /* [in] */ LPCWSTR pwzArgument,
            /* [out] */ DWORD *pReturnValue) = 0;
        
    };
    
    
#else 	/* C style interface */

    typedef struct ICLRRuntimeHostVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            ICLRRuntimeHost * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            ICLRRuntimeHost * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            ICLRRuntimeHost * This);
        
        HRESULT ( STDMETHODCALLTYPE *Start )( 
            ICLRRuntimeHost * This);
        
        HRESULT ( STDMETHODCALLTYPE *Stop )( 
            ICLRRuntimeHost * This);
        
        HRESULT ( STDMETHODCALLTYPE *SetHostControl )( 
            ICLRRuntimeHost * This,
            /* [in] */ IHostControl *pHostControl);
        
        HRESULT ( STDMETHODCALLTYPE *GetCLRControl )( 
            ICLRRuntimeHost * This,
            /* [out] */ ICLRControl **pCLRControl);
        
        HRESULT ( STDMETHODCALLTYPE *UnloadAppDomain )( 
            ICLRRuntimeHost * This,
            /* [in] */ DWORD dwAppDomainId,
            /* [in] */ BOOL fWaitUntilDone);
        
        HRESULT ( STDMETHODCALLTYPE *ExecuteInAppDomain )( 
            ICLRRuntimeHost * This,
            /* [in] */ DWORD dwAppDomainId,
            /* [in] */ FExecuteInAppDomainCallback pCallback,
            /* [in] */ void *cookie);
        
        HRESULT ( STDMETHODCALLTYPE *GetCurrentAppDomainId )( 
            ICLRRuntimeHost * This,
            /* [out] */ DWORD *pdwAppDomainId);
        
        HRESULT ( STDMETHODCALLTYPE *ExecuteApplication )( 
            ICLRRuntimeHost * This,
            /* [in] */ LPCWSTR pwzAppFullName,
            /* [in] */ DWORD dwManifestPaths,
            /* [in] */ LPCWSTR *ppwzManifestPaths,
            /* [in] */ DWORD dwActivationData,
            /* [in] */ LPCWSTR *ppwzActivationData,
            /* [out] */ int *pReturnValue);
        
        HRESULT ( STDMETHODCALLTYPE *ExecuteInDefaultAppDomain )( 
            ICLRRuntimeHost * This,
            /* [in] */ LPCWSTR pwzAssemblyPath,
            /* [in] */ LPCWSTR pwzTypeName,
            /* [in] */ LPCWSTR pwzMethodName,
            /* [in] */ LPCWSTR pwzArgument,
            /* [out] */ DWORD *pReturnValue);
        
        END_INTERFACE
    } ICLRRuntimeHostVtbl;

    interface ICLRRuntimeHost
    {
        CONST_VTBL struct ICLRRuntimeHostVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define ICLRRuntimeHost_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define ICLRRuntimeHost_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define ICLRRuntimeHost_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define ICLRRuntimeHost_Start(This)	\
    ( (This)->lpVtbl -> Start(This) ) 

#define ICLRRuntimeHost_Stop(This)	\
    ( (This)->lpVtbl -> Stop(This) ) 

#define ICLRRuntimeHost_SetHostControl(This,pHostControl)	\
    ( (This)->lpVtbl -> SetHostControl(This,pHostControl) ) 

#define ICLRRuntimeHost_GetCLRControl(This,pCLRControl)	\
    ( (This)->lpVtbl -> GetCLRControl(This,pCLRControl) ) 

#define ICLRRuntimeHost_UnloadAppDomain(This,dwAppDomainId,fWaitUntilDone)	\
    ( (This)->lpVtbl -> UnloadAppDomain(This,dwAppDomainId,fWaitUntilDone) ) 

#define ICLRRuntimeHost_ExecuteInAppDomain(This,dwAppDomainId,pCallback,cookie)	\
    ( (This)->lpVtbl -> ExecuteInAppDomain(This,dwAppDomainId,pCallback,cookie) ) 

#define ICLRRuntimeHost_GetCurrentAppDomainId(This,pdwAppDomainId)	\
    ( (This)->lpVtbl -> GetCurrentAppDomainId(This,pdwAppDomainId) ) 

#define ICLRRuntimeHost_ExecuteApplication(This,pwzAppFullName,dwManifestPaths,ppwzManifestPaths,dwActivationData,ppwzActivationData,pReturnValue)	\
    ( (This)->lpVtbl -> ExecuteApplication(This,pwzAppFullName,dwManifestPaths,ppwzManifestPaths,dwActivationData,ppwzActivationData,pReturnValue) ) 

#define ICLRRuntimeHost_ExecuteInDefaultAppDomain(This,pwzAssemblyPath,pwzTypeName,pwzMethodName,pwzArgument,pReturnValue)	\
    ( (This)->lpVtbl -> ExecuteInDefaultAppDomain(This,pwzAssemblyPath,pwzTypeName,pwzMethodName,pwzArgument,pReturnValue) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ICLRRuntimeHost_INTERFACE_DEFINED__ */


#ifndef __ICLRRuntimeHost2_INTERFACE_DEFINED__
#define __ICLRRuntimeHost2_INTERFACE_DEFINED__

/* interface ICLRRuntimeHost2 */
/* [local][unique][helpstring][version][uuid][object] */ 


EXTERN_C const IID IID_ICLRRuntimeHost2;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("712AB73F-2C22-4807-AD7E-F501D7B72C2D")
    ICLRRuntimeHost2 : public ICLRRuntimeHost
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE CreateAppDomainWithManager( 
            /* [in] */ LPCWSTR wszFriendlyName,
            /* [in] */ DWORD dwFlags,
            /* [in] */ LPCWSTR wszAppDomainManagerAssemblyName,
            /* [in] */ LPCWSTR wszAppDomainManagerTypeName,
            /* [in] */ int nProperties,
            /* [in] */ LPCWSTR *pPropertyNames,
            /* [in] */ LPCWSTR *pPropertyValues,
            /* [out] */ DWORD *pAppDomainID) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE CreateDelegate( 
            /* [in] */ DWORD appDomainID,
            /* [in] */ LPCWSTR wszAssemblyName,
            /* [in] */ LPCWSTR wszClassName,
            /* [in] */ LPCWSTR wszMethodName,
            /* [out] */ INT_PTR *fnPtr) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE Authenticate( 
            /* [in] */ ULONGLONG authKey) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE RegisterMacEHPort( void) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE SetStartupFlags( 
            /* [in] */ STARTUP_FLAGS dwFlags) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE DllGetActivationFactory( 
            /* [in] */ DWORD appDomainID,
            /* [in] */ LPCWSTR wszTypeName,
            /* [out] */ IActivationFactory **factory) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE ExecuteAssembly( 
            /* [in] */ DWORD dwAppDomainId,
            /* [in] */ LPCWSTR pwzAssemblyPath,
            /* [in] */ int argc,
            /* [in] */ LPCWSTR *argv,
            /* [out] */ DWORD *pReturnValue) = 0;
        
    };
    
    
#else 	/* C style interface */

    typedef struct ICLRRuntimeHost2Vtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            ICLRRuntimeHost2 * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            ICLRRuntimeHost2 * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            ICLRRuntimeHost2 * This);
        
        HRESULT ( STDMETHODCALLTYPE *Start )( 
            ICLRRuntimeHost2 * This);
        
        HRESULT ( STDMETHODCALLTYPE *Stop )( 
            ICLRRuntimeHost2 * This);
        
        HRESULT ( STDMETHODCALLTYPE *SetHostControl )( 
            ICLRRuntimeHost2 * This,
            /* [in] */ IHostControl *pHostControl);
        
        HRESULT ( STDMETHODCALLTYPE *GetCLRControl )( 
            ICLRRuntimeHost2 * This,
            /* [out] */ ICLRControl **pCLRControl);
        
        HRESULT ( STDMETHODCALLTYPE *UnloadAppDomain )( 
            ICLRRuntimeHost2 * This,
            /* [in] */ DWORD dwAppDomainId,
            /* [in] */ BOOL fWaitUntilDone);
        
        HRESULT ( STDMETHODCALLTYPE *ExecuteInAppDomain )( 
            ICLRRuntimeHost2 * This,
            /* [in] */ DWORD dwAppDomainId,
            /* [in] */ FExecuteInAppDomainCallback pCallback,
            /* [in] */ void *cookie);
        
        HRESULT ( STDMETHODCALLTYPE *GetCurrentAppDomainId )( 
            ICLRRuntimeHost2 * This,
            /* [out] */ DWORD *pdwAppDomainId);
        
        HRESULT ( STDMETHODCALLTYPE *ExecuteApplication )( 
            ICLRRuntimeHost2 * This,
            /* [in] */ LPCWSTR pwzAppFullName,
            /* [in] */ DWORD dwManifestPaths,
            /* [in] */ LPCWSTR *ppwzManifestPaths,
            /* [in] */ DWORD dwActivationData,
            /* [in] */ LPCWSTR *ppwzActivationData,
            /* [out] */ int *pReturnValue);
        
        HRESULT ( STDMETHODCALLTYPE *ExecuteInDefaultAppDomain )( 
            ICLRRuntimeHost2 * This,
            /* [in] */ LPCWSTR pwzAssemblyPath,
            /* [in] */ LPCWSTR pwzTypeName,
            /* [in] */ LPCWSTR pwzMethodName,
            /* [in] */ LPCWSTR pwzArgument,
            /* [out] */ DWORD *pReturnValue);
        
        HRESULT ( STDMETHODCALLTYPE *CreateAppDomainWithManager )( 
            ICLRRuntimeHost2 * This,
            /* [in] */ LPCWSTR wszFriendlyName,
            /* [in] */ DWORD dwFlags,
            /* [in] */ LPCWSTR wszAppDomainManagerAssemblyName,
            /* [in] */ LPCWSTR wszAppDomainManagerTypeName,
            /* [in] */ int nProperties,
            /* [in] */ LPCWSTR *pPropertyNames,
            /* [in] */ LPCWSTR *pPropertyValues,
            /* [out] */ DWORD *pAppDomainID);
        
        HRESULT ( STDMETHODCALLTYPE *CreateDelegate )( 
            ICLRRuntimeHost2 * This,
            /* [in] */ DWORD appDomainID,
            /* [in] */ LPCWSTR wszAssemblyName,
            /* [in] */ LPCWSTR wszClassName,
            /* [in] */ LPCWSTR wszMethodName,
            /* [out] */ INT_PTR *fnPtr);
        
        HRESULT ( STDMETHODCALLTYPE *Authenticate )( 
            ICLRRuntimeHost2 * This,
            /* [in] */ ULONGLONG authKey);
        
        HRESULT ( STDMETHODCALLTYPE *RegisterMacEHPort )( 
            ICLRRuntimeHost2 * This);
        
        HRESULT ( STDMETHODCALLTYPE *SetStartupFlags )( 
            ICLRRuntimeHost2 * This,
            /* [in] */ STARTUP_FLAGS dwFlags);
        
        HRESULT ( STDMETHODCALLTYPE *DllGetActivationFactory )( 
            ICLRRuntimeHost2 * This,
            /* [in] */ DWORD appDomainID,
            /* [in] */ LPCWSTR wszTypeName,
            /* [out] */ IActivationFactory **factory);
        
        HRESULT ( STDMETHODCALLTYPE *ExecuteAssembly )( 
            ICLRRuntimeHost2 * This,
            /* [in] */ DWORD dwAppDomainId,
            /* [in] */ LPCWSTR pwzAssemblyPath,
            /* [in] */ int argc,
            /* [in] */ LPCWSTR *argv,
            /* [out] */ DWORD *pReturnValue);
        
        END_INTERFACE
    } ICLRRuntimeHost2Vtbl;

    interface ICLRRuntimeHost2
    {
        CONST_VTBL struct ICLRRuntimeHost2Vtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define ICLRRuntimeHost2_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define ICLRRuntimeHost2_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define ICLRRuntimeHost2_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define ICLRRuntimeHost2_Start(This)	\
    ( (This)->lpVtbl -> Start(This) ) 

#define ICLRRuntimeHost2_Stop(This)	\
    ( (This)->lpVtbl -> Stop(This) ) 

#define ICLRRuntimeHost2_SetHostControl(This,pHostControl)	\
    ( (This)->lpVtbl -> SetHostControl(This,pHostControl) ) 

#define ICLRRuntimeHost2_GetCLRControl(This,pCLRControl)	\
    ( (This)->lpVtbl -> GetCLRControl(This,pCLRControl) ) 

#define ICLRRuntimeHost2_UnloadAppDomain(This,dwAppDomainId,fWaitUntilDone)	\
    ( (This)->lpVtbl -> UnloadAppDomain(This,dwAppDomainId,fWaitUntilDone) ) 

#define ICLRRuntimeHost2_ExecuteInAppDomain(This,dwAppDomainId,pCallback,cookie)	\
    ( (This)->lpVtbl -> ExecuteInAppDomain(This,dwAppDomainId,pCallback,cookie) ) 

#define ICLRRuntimeHost2_GetCurrentAppDomainId(This,pdwAppDomainId)	\
    ( (This)->lpVtbl -> GetCurrentAppDomainId(This,pdwAppDomainId) ) 

#define ICLRRuntimeHost2_ExecuteApplication(This,pwzAppFullName,dwManifestPaths,ppwzManifestPaths,dwActivationData,ppwzActivationData,pReturnValue)	\
    ( (This)->lpVtbl -> ExecuteApplication(This,pwzAppFullName,dwManifestPaths,ppwzManifestPaths,dwActivationData,ppwzActivationData,pReturnValue) ) 

#define ICLRRuntimeHost2_ExecuteInDefaultAppDomain(This,pwzAssemblyPath,pwzTypeName,pwzMethodName,pwzArgument,pReturnValue)	\
    ( (This)->lpVtbl -> ExecuteInDefaultAppDomain(This,pwzAssemblyPath,pwzTypeName,pwzMethodName,pwzArgument,pReturnValue) ) 


#define ICLRRuntimeHost2_CreateAppDomainWithManager(This,wszFriendlyName,dwFlags,wszAppDomainManagerAssemblyName,wszAppDomainManagerTypeName,nProperties,pPropertyNames,pPropertyValues,pAppDomainID)	\
    ( (This)->lpVtbl -> CreateAppDomainWithManager(This,wszFriendlyName,dwFlags,wszAppDomainManagerAssemblyName,wszAppDomainManagerTypeName,nProperties,pPropertyNames,pPropertyValues,pAppDomainID) ) 

#define ICLRRuntimeHost2_CreateDelegate(This,appDomainID,wszAssemblyName,wszClassName,wszMethodName,fnPtr)	\
    ( (This)->lpVtbl -> CreateDelegate(This,appDomainID,wszAssemblyName,wszClassName,wszMethodName,fnPtr) ) 

#define ICLRRuntimeHost2_Authenticate(This,authKey)	\
    ( (This)->lpVtbl -> Authenticate(This,authKey) ) 

#define ICLRRuntimeHost2_RegisterMacEHPort(This)	\
    ( (This)->lpVtbl -> RegisterMacEHPort(This) ) 

#define ICLRRuntimeHost2_SetStartupFlags(This,dwFlags)	\
    ( (This)->lpVtbl -> SetStartupFlags(This,dwFlags) ) 

#define ICLRRuntimeHost2_DllGetActivationFactory(This,appDomainID,wszTypeName,factory)	\
    ( (This)->lpVtbl -> DllGetActivationFactory(This,appDomainID,wszTypeName,factory) ) 

#define ICLRRuntimeHost2_ExecuteAssembly(This,dwAppDomainId,pwzAssemblyPath,argc,argv,pReturnValue)	\
    ( (This)->lpVtbl -> ExecuteAssembly(This,dwAppDomainId,pwzAssemblyPath,argc,argv,pReturnValue) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ICLRRuntimeHost2_INTERFACE_DEFINED__ */


#ifndef __ICLRRuntimeHost4_INTERFACE_DEFINED__
#define __ICLRRuntimeHost4_INTERFACE_DEFINED__

/* interface ICLRRuntimeHost4 */
/* [local][unique][helpstring][version][uuid][object] */ 


EXTERN_C const IID IID_ICLRRuntimeHost4;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("64F6D366-D7C2-4F1F-B4B2-E8160CAC43AF")
    ICLRRuntimeHost4 : public ICLRRuntimeHost2
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE UnloadAppDomain2( 
            /* [in] */ DWORD dwAppDomainId,
            /* [in] */ BOOL fWaitUntilDone,
            /* [out] */ int *pLatchedExitCode) = 0;
        
    };
    
    
#else 	/* C style interface */

    typedef struct ICLRRuntimeHost4Vtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            ICLRRuntimeHost4 * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            ICLRRuntimeHost4 * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            ICLRRuntimeHost4 * This);
        
        HRESULT ( STDMETHODCALLTYPE *Start )( 
            ICLRRuntimeHost4 * This);
        
        HRESULT ( STDMETHODCALLTYPE *Stop )( 
            ICLRRuntimeHost4 * This);
        
        HRESULT ( STDMETHODCALLTYPE *SetHostControl )( 
            ICLRRuntimeHost4 * This,
            /* [in] */ IHostControl *pHostControl);
        
        HRESULT ( STDMETHODCALLTYPE *GetCLRControl )( 
            ICLRRuntimeHost4 * This,
            /* [out] */ ICLRControl **pCLRControl);
        
        HRESULT ( STDMETHODCALLTYPE *UnloadAppDomain )( 
            ICLRRuntimeHost4 * This,
            /* [in] */ DWORD dwAppDomainId,
            /* [in] */ BOOL fWaitUntilDone);
        
        HRESULT ( STDMETHODCALLTYPE *ExecuteInAppDomain )( 
            ICLRRuntimeHost4 * This,
            /* [in] */ DWORD dwAppDomainId,
            /* [in] */ FExecuteInAppDomainCallback pCallback,
            /* [in] */ void *cookie);
        
        HRESULT ( STDMETHODCALLTYPE *GetCurrentAppDomainId )( 
            ICLRRuntimeHost4 * This,
            /* [out] */ DWORD *pdwAppDomainId);
        
        HRESULT ( STDMETHODCALLTYPE *ExecuteApplication )( 
            ICLRRuntimeHost4 * This,
            /* [in] */ LPCWSTR pwzAppFullName,
            /* [in] */ DWORD dwManifestPaths,
            /* [in] */ LPCWSTR *ppwzManifestPaths,
            /* [in] */ DWORD dwActivationData,
            /* [in] */ LPCWSTR *ppwzActivationData,
            /* [out] */ int *pReturnValue);
        
        HRESULT ( STDMETHODCALLTYPE *ExecuteInDefaultAppDomain )( 
            ICLRRuntimeHost4 * This,
            /* [in] */ LPCWSTR pwzAssemblyPath,
            /* [in] */ LPCWSTR pwzTypeName,
            /* [in] */ LPCWSTR pwzMethodName,
            /* [in] */ LPCWSTR pwzArgument,
            /* [out] */ DWORD *pReturnValue);
        
        HRESULT ( STDMETHODCALLTYPE *CreateAppDomainWithManager )( 
            ICLRRuntimeHost4 * This,
            /* [in] */ LPCWSTR wszFriendlyName,
            /* [in] */ DWORD dwFlags,
            /* [in] */ LPCWSTR wszAppDomainManagerAssemblyName,
            /* [in] */ LPCWSTR wszAppDomainManagerTypeName,
            /* [in] */ int nProperties,
            /* [in] */ LPCWSTR *pPropertyNames,
            /* [in] */ LPCWSTR *pPropertyValues,
            /* [out] */ DWORD *pAppDomainID);
        
        HRESULT ( STDMETHODCALLTYPE *CreateDelegate )( 
            ICLRRuntimeHost4 * This,
            /* [in] */ DWORD appDomainID,
            /* [in] */ LPCWSTR wszAssemblyName,
            /* [in] */ LPCWSTR wszClassName,
            /* [in] */ LPCWSTR wszMethodName,
            /* [out] */ INT_PTR *fnPtr);
        
        HRESULT ( STDMETHODCALLTYPE *Authenticate )( 
            ICLRRuntimeHost4 * This,
            /* [in] */ ULONGLONG authKey);
        
        HRESULT ( STDMETHODCALLTYPE *RegisterMacEHPort )( 
            ICLRRuntimeHost4 * This);
        
        HRESULT ( STDMETHODCALLTYPE *SetStartupFlags )( 
            ICLRRuntimeHost4 * This,
            /* [in] */ STARTUP_FLAGS dwFlags);
        
        HRESULT ( STDMETHODCALLTYPE *DllGetActivationFactory )( 
            ICLRRuntimeHost4 * This,
            /* [in] */ DWORD appDomainID,
            /* [in] */ LPCWSTR wszTypeName,
            /* [out] */ IActivationFactory **factory);
        
        HRESULT ( STDMETHODCALLTYPE *ExecuteAssembly )( 
            ICLRRuntimeHost4 * This,
            /* [in] */ DWORD dwAppDomainId,
            /* [in] */ LPCWSTR pwzAssemblyPath,
            /* [in] */ int argc,
            /* [in] */ LPCWSTR *argv,
            /* [out] */ DWORD *pReturnValue);
        
        HRESULT ( STDMETHODCALLTYPE *UnloadAppDomain2 )( 
            ICLRRuntimeHost4 * This,
            /* [in] */ DWORD dwAppDomainId,
            /* [in] */ BOOL fWaitUntilDone,
            /* [out] */ int *pLatchedExitCode);
        
        END_INTERFACE
    } ICLRRuntimeHost4Vtbl;

    interface ICLRRuntimeHost4
    {
        CONST_VTBL struct ICLRRuntimeHost4Vtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define ICLRRuntimeHost4_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define ICLRRuntimeHost4_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define ICLRRuntimeHost4_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define ICLRRuntimeHost4_Start(This)	\
    ( (This)->lpVtbl -> Start(This) ) 

#define ICLRRuntimeHost4_Stop(This)	\
    ( (This)->lpVtbl -> Stop(This) ) 

#define ICLRRuntimeHost4_SetHostControl(This,pHostControl)	\
    ( (This)->lpVtbl -> SetHostControl(This,pHostControl) ) 

#define ICLRRuntimeHost4_GetCLRControl(This,pCLRControl)	\
    ( (This)->lpVtbl -> GetCLRControl(This,pCLRControl) ) 

#define ICLRRuntimeHost4_UnloadAppDomain(This,dwAppDomainId,fWaitUntilDone)	\
    ( (This)->lpVtbl -> UnloadAppDomain(This,dwAppDomainId,fWaitUntilDone) ) 

#define ICLRRuntimeHost4_ExecuteInAppDomain(This,dwAppDomainId,pCallback,cookie)	\
    ( (This)->lpVtbl -> ExecuteInAppDomain(This,dwAppDomainId,pCallback,cookie) ) 

#define ICLRRuntimeHost4_GetCurrentAppDomainId(This,pdwAppDomainId)	\
    ( (This)->lpVtbl -> GetCurrentAppDomainId(This,pdwAppDomainId) ) 

#define ICLRRuntimeHost4_ExecuteApplication(This,pwzAppFullName,dwManifestPaths,ppwzManifestPaths,dwActivationData,ppwzActivationData,pReturnValue)	\
    ( (This)->lpVtbl -> ExecuteApplication(This,pwzAppFullName,dwManifestPaths,ppwzManifestPaths,dwActivationData,ppwzActivationData,pReturnValue) ) 

#define ICLRRuntimeHost4_ExecuteInDefaultAppDomain(This,pwzAssemblyPath,pwzTypeName,pwzMethodName,pwzArgument,pReturnValue)	\
    ( (This)->lpVtbl -> ExecuteInDefaultAppDomain(This,pwzAssemblyPath,pwzTypeName,pwzMethodName,pwzArgument,pReturnValue) ) 


#define ICLRRuntimeHost4_CreateAppDomainWithManager(This,wszFriendlyName,dwFlags,wszAppDomainManagerAssemblyName,wszAppDomainManagerTypeName,nProperties,pPropertyNames,pPropertyValues,pAppDomainID)	\
    ( (This)->lpVtbl -> CreateAppDomainWithManager(This,wszFriendlyName,dwFlags,wszAppDomainManagerAssemblyName,wszAppDomainManagerTypeName,nProperties,pPropertyNames,pPropertyValues,pAppDomainID) ) 

#define ICLRRuntimeHost4_CreateDelegate(This,appDomainID,wszAssemblyName,wszClassName,wszMethodName,fnPtr)	\
    ( (This)->lpVtbl -> CreateDelegate(This,appDomainID,wszAssemblyName,wszClassName,wszMethodName,fnPtr) ) 

#define ICLRRuntimeHost4_Authenticate(This,authKey)	\
    ( (This)->lpVtbl -> Authenticate(This,authKey) ) 

#define ICLRRuntimeHost4_RegisterMacEHPort(This)	\
    ( (This)->lpVtbl -> RegisterMacEHPort(This) ) 

#define ICLRRuntimeHost4_SetStartupFlags(This,dwFlags)	\
    ( (This)->lpVtbl -> SetStartupFlags(This,dwFlags) ) 

#define ICLRRuntimeHost4_DllGetActivationFactory(This,appDomainID,wszTypeName,factory)	\
    ( (This)->lpVtbl -> DllGetActivationFactory(This,appDomainID,wszTypeName,factory) ) 

#define ICLRRuntimeHost4_ExecuteAssembly(This,dwAppDomainId,pwzAssemblyPath,argc,argv,pReturnValue)	\
    ( (This)->lpVtbl -> ExecuteAssembly(This,dwAppDomainId,pwzAssemblyPath,argc,argv,pReturnValue) ) 


#define ICLRRuntimeHost4_UnloadAppDomain2(This,dwAppDomainId,fWaitUntilDone,pLatchedExitCode)	\
    ( (This)->lpVtbl -> UnloadAppDomain2(This,dwAppDomainId,fWaitUntilDone,pLatchedExitCode) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ICLRRuntimeHost4_INTERFACE_DEFINED__ */


/* interface __MIDL_itf_mscoree_0000_0003 */
/* [local] */ 

#undef DEPRECATED_CLR_STDAPI
#undef DECLARE_DEPRECATED
#undef DEPRECATED_CLR_API_MESG


extern RPC_IF_HANDLE __MIDL_itf_mscoree_0000_0003_v0_0_c_ifspec;
extern RPC_IF_HANDLE __MIDL_itf_mscoree_0000_0003_v0_0_s_ifspec;

/* Additional Prototypes for ALL interfaces */

/* end of Additional Prototypes */

#ifdef __cplusplus
}
#endif

#endif


