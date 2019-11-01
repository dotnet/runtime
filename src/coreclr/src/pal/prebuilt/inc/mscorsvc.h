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

#ifndef __mscorsvc_h__
#define __mscorsvc_h__

#if defined(_MSC_VER) && (_MSC_VER >= 1020)
#pragma once
#endif

/* Forward Declarations */ 

#ifndef __ICorSvcDependencies_FWD_DEFINED__
#define __ICorSvcDependencies_FWD_DEFINED__
typedef interface ICorSvcDependencies ICorSvcDependencies;

#endif 	/* __ICorSvcDependencies_FWD_DEFINED__ */


#ifndef __ICorSvcWorker_FWD_DEFINED__
#define __ICorSvcWorker_FWD_DEFINED__
typedef interface ICorSvcWorker ICorSvcWorker;

#endif 	/* __ICorSvcWorker_FWD_DEFINED__ */


#ifndef __ICorSvcWorker2_FWD_DEFINED__
#define __ICorSvcWorker2_FWD_DEFINED__
typedef interface ICorSvcWorker2 ICorSvcWorker2;

#endif 	/* __ICorSvcWorker2_FWD_DEFINED__ */


#ifndef __ICorSvcWorker3_FWD_DEFINED__
#define __ICorSvcWorker3_FWD_DEFINED__
typedef interface ICorSvcWorker3 ICorSvcWorker3;

#endif 	/* __ICorSvcWorker3_FWD_DEFINED__ */


#ifndef __ICorSvcSetPrivateAttributes_FWD_DEFINED__
#define __ICorSvcSetPrivateAttributes_FWD_DEFINED__
typedef interface ICorSvcSetPrivateAttributes ICorSvcSetPrivateAttributes;

#endif 	/* __ICorSvcSetPrivateAttributes_FWD_DEFINED__ */


#ifndef __ICorSvcRepository_FWD_DEFINED__
#define __ICorSvcRepository_FWD_DEFINED__
typedef interface ICorSvcRepository ICorSvcRepository;

#endif 	/* __ICorSvcRepository_FWD_DEFINED__ */


#ifndef __ICorSvcAppX_FWD_DEFINED__
#define __ICorSvcAppX_FWD_DEFINED__
typedef interface ICorSvcAppX ICorSvcAppX;

#endif 	/* __ICorSvcAppX_FWD_DEFINED__ */


#ifndef __ICorSvcLogger_FWD_DEFINED__
#define __ICorSvcLogger_FWD_DEFINED__
typedef interface ICorSvcLogger ICorSvcLogger;

#endif 	/* __ICorSvcLogger_FWD_DEFINED__ */


#ifndef __ICorSvcPooledWorker_FWD_DEFINED__
#define __ICorSvcPooledWorker_FWD_DEFINED__
typedef interface ICorSvcPooledWorker ICorSvcPooledWorker;

#endif 	/* __ICorSvcPooledWorker_FWD_DEFINED__ */


#ifndef __ICorSvcBindToWorker_FWD_DEFINED__
#define __ICorSvcBindToWorker_FWD_DEFINED__
typedef interface ICorSvcBindToWorker ICorSvcBindToWorker;

#endif 	/* __ICorSvcBindToWorker_FWD_DEFINED__ */


#ifndef __ICorSvc_FWD_DEFINED__
#define __ICorSvc_FWD_DEFINED__
typedef interface ICorSvc ICorSvc;

#endif 	/* __ICorSvc_FWD_DEFINED__ */


#ifndef __ICompileProgressNotification_FWD_DEFINED__
#define __ICompileProgressNotification_FWD_DEFINED__
typedef interface ICompileProgressNotification ICompileProgressNotification;

#endif 	/* __ICompileProgressNotification_FWD_DEFINED__ */


#ifndef __ICompileProgressNotification2_FWD_DEFINED__
#define __ICompileProgressNotification2_FWD_DEFINED__
typedef interface ICompileProgressNotification2 ICompileProgressNotification2;

#endif 	/* __ICompileProgressNotification2_FWD_DEFINED__ */


#ifndef __ICorSvcInstaller_FWD_DEFINED__
#define __ICorSvcInstaller_FWD_DEFINED__
typedef interface ICorSvcInstaller ICorSvcInstaller;

#endif 	/* __ICorSvcInstaller_FWD_DEFINED__ */


#ifndef __ICorSvcAdvancedInstaller_FWD_DEFINED__
#define __ICorSvcAdvancedInstaller_FWD_DEFINED__
typedef interface ICorSvcAdvancedInstaller ICorSvcAdvancedInstaller;

#endif 	/* __ICorSvcAdvancedInstaller_FWD_DEFINED__ */


#ifndef __ICorSvcOptimizer_FWD_DEFINED__
#define __ICorSvcOptimizer_FWD_DEFINED__
typedef interface ICorSvcOptimizer ICorSvcOptimizer;

#endif 	/* __ICorSvcOptimizer_FWD_DEFINED__ */


#ifndef __ICorSvcOptimizer2_FWD_DEFINED__
#define __ICorSvcOptimizer2_FWD_DEFINED__
typedef interface ICorSvcOptimizer2 ICorSvcOptimizer2;

#endif 	/* __ICorSvcOptimizer2_FWD_DEFINED__ */


#ifndef __ICorSvcOptimizer3_FWD_DEFINED__
#define __ICorSvcOptimizer3_FWD_DEFINED__
typedef interface ICorSvcOptimizer3 ICorSvcOptimizer3;

#endif 	/* __ICorSvcOptimizer3_FWD_DEFINED__ */


#ifndef __ICorSvcManager_FWD_DEFINED__
#define __ICorSvcManager_FWD_DEFINED__
typedef interface ICorSvcManager ICorSvcManager;

#endif 	/* __ICorSvcManager_FWD_DEFINED__ */


#ifndef __ICorSvcManager2_FWD_DEFINED__
#define __ICorSvcManager2_FWD_DEFINED__
typedef interface ICorSvcManager2 ICorSvcManager2;

#endif 	/* __ICorSvcManager2_FWD_DEFINED__ */


#ifndef __ICorSvcSetLegacyServiceBehavior_FWD_DEFINED__
#define __ICorSvcSetLegacyServiceBehavior_FWD_DEFINED__
typedef interface ICorSvcSetLegacyServiceBehavior ICorSvcSetLegacyServiceBehavior;

#endif 	/* __ICorSvcSetLegacyServiceBehavior_FWD_DEFINED__ */


#ifndef __ICorSvcSetTaskBootTriggerState_FWD_DEFINED__
#define __ICorSvcSetTaskBootTriggerState_FWD_DEFINED__
typedef interface ICorSvcSetTaskBootTriggerState ICorSvcSetTaskBootTriggerState;

#endif 	/* __ICorSvcSetTaskBootTriggerState_FWD_DEFINED__ */


#ifndef __ICorSvcSetTaskDelayStartTriggerState_FWD_DEFINED__
#define __ICorSvcSetTaskDelayStartTriggerState_FWD_DEFINED__
typedef interface ICorSvcSetTaskDelayStartTriggerState ICorSvcSetTaskDelayStartTriggerState;

#endif 	/* __ICorSvcSetTaskDelayStartTriggerState_FWD_DEFINED__ */


/* header files for imported files */
#include "unknwn.h"

#ifdef __cplusplus
extern "C"{
#endif 


/* interface __MIDL_itf_mscorsvc_0000_0000 */
/* [local] */ 

#if 0
#endif
EXTERN_GUID(CLSID_CorSvcWorker, 0x8ed1a844, 0x32a7, 0x4a67, 0xba, 0x62, 0xf8, 0xd5, 0xaf, 0xdf, 0xf4, 0x60);
EXTERN_GUID(CLSID_CorSvcBindToWorker, 0x9f74fb09, 0x4221, 0x40b4, 0xae, 0x21, 0xae, 0xb6, 0xdf, 0xf2, 0x99, 0x4e);
STDAPI CorGetSvc(IUnknown **pIUnknown);


extern RPC_IF_HANDLE __MIDL_itf_mscorsvc_0000_0000_v0_0_c_ifspec;
extern RPC_IF_HANDLE __MIDL_itf_mscorsvc_0000_0000_v0_0_s_ifspec;


#ifndef __mscorsvc_LIBRARY_DEFINED__
#define __mscorsvc_LIBRARY_DEFINED__

/* library mscorsvc */
/* [helpstring][version][uuid] */ 

typedef /* [public][public][public][public][public][public][public][public] */ 
enum __MIDL___MIDL_itf_mscorsvc_0001_0004_0001
    {
        ScenarioDefault	= 0,
        ScenarioAll	= 0x1,
        ScenarioDebug	= 0x2,
        ScenarioProfile	= 0x8,
        ScenarioTuningDataCollection	= 0x10,
        ScenarioLegacy	= 0x20,
        ScenarioNgenLastRetry	= 0x10000,
        ScenarioAutoNGen	= 0x100000,
        ScenarioRepositoryOnly	= 0x200000
    } 	OptimizationScenario;

typedef /* [public] */ 
enum __MIDL___MIDL_itf_mscorsvc_0001_0004_0002
    {
        ScenarioEmitFixups	= 0x10000,
        ScenarioProfileInfo	= 0x20000
    } 	PrivateOptimizationScenario;

typedef struct _SvcWorkerPriority
    {
    DWORD dwPriorityClass;
    } 	SvcWorkerPriority;

typedef /* [public] */ 
enum __MIDL___MIDL_itf_mscorsvc_0001_0007_0001
    {
        DbgTypePdb	= 0x1
    } 	NGenPrivateAttributesFlags;

typedef struct _NGenPrivateAttributes
    {
    DWORD Flags;
    DWORD ZapStats;
    BSTR DbgDir;
    } 	NGenPrivateAttributes;

typedef /* [public][public] */ 
enum __MIDL___MIDL_itf_mscorsvc_0001_0008_0001
    {
        RepositoryDefault	= 0,
        MoveFromRepository	= 0x1,
        CopyToRepository	= 0x2,
        IgnoreRepository	= 0x4
    } 	RepositoryFlags;

typedef 
enum CorSvcLogLevel
    {
        LogLevel_Error	= 0,
        LogLevel_Warning	= ( LogLevel_Error + 1 ) ,
        LogLevel_Success	= ( LogLevel_Warning + 1 ) ,
        LogLevel_Info	= ( LogLevel_Success + 1 ) 
    } 	CorSvcLogLevel;


EXTERN_C const IID LIBID_mscorsvc;

#ifndef __ICorSvcDependencies_INTERFACE_DEFINED__
#define __ICorSvcDependencies_INTERFACE_DEFINED__

/* interface ICorSvcDependencies */
/* [unique][uuid][oleautomation][object] */ 


EXTERN_C const IID IID_ICorSvcDependencies;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("ddb34005-9ba3-4025-9554-f00a2df5dbf5")
    ICorSvcDependencies : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE GetAssemblyDependencies( 
            /* [in] */ BSTR pAssemblyName,
            /* [out] */ SAFEARRAY * *pDependencies,
            /* [out] */ DWORD *assemblyNGenSetting,
            /* [out] */ BSTR *pNativeImageIdentity,
            /* [out] */ BSTR *pAssemblyDisplayName,
            /* [out] */ SAFEARRAY * *pDependencyLoadSetting,
            /* [out] */ SAFEARRAY * *pDependencyNGenSetting) = 0;
        
    };
    
    
#else 	/* C style interface */

    typedef struct ICorSvcDependenciesVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            ICorSvcDependencies * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            ICorSvcDependencies * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            ICorSvcDependencies * This);
        
        HRESULT ( STDMETHODCALLTYPE *GetAssemblyDependencies )( 
            ICorSvcDependencies * This,
            /* [in] */ BSTR pAssemblyName,
            /* [out] */ SAFEARRAY * *pDependencies,
            /* [out] */ DWORD *assemblyNGenSetting,
            /* [out] */ BSTR *pNativeImageIdentity,
            /* [out] */ BSTR *pAssemblyDisplayName,
            /* [out] */ SAFEARRAY * *pDependencyLoadSetting,
            /* [out] */ SAFEARRAY * *pDependencyNGenSetting);
        
        END_INTERFACE
    } ICorSvcDependenciesVtbl;

    interface ICorSvcDependencies
    {
        CONST_VTBL struct ICorSvcDependenciesVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define ICorSvcDependencies_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define ICorSvcDependencies_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define ICorSvcDependencies_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define ICorSvcDependencies_GetAssemblyDependencies(This,pAssemblyName,pDependencies,assemblyNGenSetting,pNativeImageIdentity,pAssemblyDisplayName,pDependencyLoadSetting,pDependencyNGenSetting)	\
    ( (This)->lpVtbl -> GetAssemblyDependencies(This,pAssemblyName,pDependencies,assemblyNGenSetting,pNativeImageIdentity,pAssemblyDisplayName,pDependencyLoadSetting,pDependencyNGenSetting) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ICorSvcDependencies_INTERFACE_DEFINED__ */


#ifndef __ICorSvcWorker_INTERFACE_DEFINED__
#define __ICorSvcWorker_INTERFACE_DEFINED__

/* interface ICorSvcWorker */
/* [unique][uuid][oleautomation][object] */ 


EXTERN_C const IID IID_ICorSvcWorker;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("d1047bc2-67c0-400c-a94c-e64446a67fbe")
    ICorSvcWorker : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE SetPriority( 
            /* [in] */ SvcWorkerPriority priority) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE OptimizeAssembly( 
            /* [in] */ BSTR pAssemblyName,
            /* [in] */ BSTR pApplicationName,
            /* [in] */ OptimizationScenario scenario,
            /* [in] */ SAFEARRAY * loadAlwaysList,
            /* [in] */ SAFEARRAY * loadSometimesList,
            /* [in] */ SAFEARRAY * loadNeverList,
            /* [out] */ BSTR *pNativeImageIdentity) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE DeleteNativeImage( 
            /* [in] */ BSTR pAssemblyName,
            /* [in] */ BSTR pNativeImage) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE DisplayNativeImages( 
            /* [in] */ BSTR pAssemblyName) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetCorSvcDependencies( 
            /* [in] */ BSTR pApplicationName,
            /* [in] */ OptimizationScenario scenario,
            /* [out] */ ICorSvcDependencies **pCorSvcDependencies) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE Stop( void) = 0;
        
    };
    
    
#else 	/* C style interface */

    typedef struct ICorSvcWorkerVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            ICorSvcWorker * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            ICorSvcWorker * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            ICorSvcWorker * This);
        
        HRESULT ( STDMETHODCALLTYPE *SetPriority )( 
            ICorSvcWorker * This,
            /* [in] */ SvcWorkerPriority priority);
        
        HRESULT ( STDMETHODCALLTYPE *OptimizeAssembly )( 
            ICorSvcWorker * This,
            /* [in] */ BSTR pAssemblyName,
            /* [in] */ BSTR pApplicationName,
            /* [in] */ OptimizationScenario scenario,
            /* [in] */ SAFEARRAY * loadAlwaysList,
            /* [in] */ SAFEARRAY * loadSometimesList,
            /* [in] */ SAFEARRAY * loadNeverList,
            /* [out] */ BSTR *pNativeImageIdentity);
        
        HRESULT ( STDMETHODCALLTYPE *DeleteNativeImage )( 
            ICorSvcWorker * This,
            /* [in] */ BSTR pAssemblyName,
            /* [in] */ BSTR pNativeImage);
        
        HRESULT ( STDMETHODCALLTYPE *DisplayNativeImages )( 
            ICorSvcWorker * This,
            /* [in] */ BSTR pAssemblyName);
        
        HRESULT ( STDMETHODCALLTYPE *GetCorSvcDependencies )( 
            ICorSvcWorker * This,
            /* [in] */ BSTR pApplicationName,
            /* [in] */ OptimizationScenario scenario,
            /* [out] */ ICorSvcDependencies **pCorSvcDependencies);
        
        HRESULT ( STDMETHODCALLTYPE *Stop )( 
            ICorSvcWorker * This);
        
        END_INTERFACE
    } ICorSvcWorkerVtbl;

    interface ICorSvcWorker
    {
        CONST_VTBL struct ICorSvcWorkerVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define ICorSvcWorker_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define ICorSvcWorker_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define ICorSvcWorker_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define ICorSvcWorker_SetPriority(This,priority)	\
    ( (This)->lpVtbl -> SetPriority(This,priority) ) 

#define ICorSvcWorker_OptimizeAssembly(This,pAssemblyName,pApplicationName,scenario,loadAlwaysList,loadSometimesList,loadNeverList,pNativeImageIdentity)	\
    ( (This)->lpVtbl -> OptimizeAssembly(This,pAssemblyName,pApplicationName,scenario,loadAlwaysList,loadSometimesList,loadNeverList,pNativeImageIdentity) ) 

#define ICorSvcWorker_DeleteNativeImage(This,pAssemblyName,pNativeImage)	\
    ( (This)->lpVtbl -> DeleteNativeImage(This,pAssemblyName,pNativeImage) ) 

#define ICorSvcWorker_DisplayNativeImages(This,pAssemblyName)	\
    ( (This)->lpVtbl -> DisplayNativeImages(This,pAssemblyName) ) 

#define ICorSvcWorker_GetCorSvcDependencies(This,pApplicationName,scenario,pCorSvcDependencies)	\
    ( (This)->lpVtbl -> GetCorSvcDependencies(This,pApplicationName,scenario,pCorSvcDependencies) ) 

#define ICorSvcWorker_Stop(This)	\
    ( (This)->lpVtbl -> Stop(This) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ICorSvcWorker_INTERFACE_DEFINED__ */


#ifndef __ICorSvcWorker2_INTERFACE_DEFINED__
#define __ICorSvcWorker2_INTERFACE_DEFINED__

/* interface ICorSvcWorker2 */
/* [unique][uuid][oleautomation][object] */ 


EXTERN_C const IID IID_ICorSvcWorker2;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("f3358a7d-0061-4776-880e-a2f21b9ef93e")
    ICorSvcWorker2 : public ICorSvcWorker
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE CreatePdb( 
            /* [in] */ BSTR pAssemblyName,
            /* [in] */ BSTR pAppBaseOrConfig,
            /* [in] */ OptimizationScenario scenario,
            /* [in] */ BSTR pNativeImagePath,
            /* [in] */ BSTR pPdbPath) = 0;
        
    };
    
    
#else 	/* C style interface */

    typedef struct ICorSvcWorker2Vtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            ICorSvcWorker2 * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            ICorSvcWorker2 * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            ICorSvcWorker2 * This);
        
        HRESULT ( STDMETHODCALLTYPE *SetPriority )( 
            ICorSvcWorker2 * This,
            /* [in] */ SvcWorkerPriority priority);
        
        HRESULT ( STDMETHODCALLTYPE *OptimizeAssembly )( 
            ICorSvcWorker2 * This,
            /* [in] */ BSTR pAssemblyName,
            /* [in] */ BSTR pApplicationName,
            /* [in] */ OptimizationScenario scenario,
            /* [in] */ SAFEARRAY * loadAlwaysList,
            /* [in] */ SAFEARRAY * loadSometimesList,
            /* [in] */ SAFEARRAY * loadNeverList,
            /* [out] */ BSTR *pNativeImageIdentity);
        
        HRESULT ( STDMETHODCALLTYPE *DeleteNativeImage )( 
            ICorSvcWorker2 * This,
            /* [in] */ BSTR pAssemblyName,
            /* [in] */ BSTR pNativeImage);
        
        HRESULT ( STDMETHODCALLTYPE *DisplayNativeImages )( 
            ICorSvcWorker2 * This,
            /* [in] */ BSTR pAssemblyName);
        
        HRESULT ( STDMETHODCALLTYPE *GetCorSvcDependencies )( 
            ICorSvcWorker2 * This,
            /* [in] */ BSTR pApplicationName,
            /* [in] */ OptimizationScenario scenario,
            /* [out] */ ICorSvcDependencies **pCorSvcDependencies);
        
        HRESULT ( STDMETHODCALLTYPE *Stop )( 
            ICorSvcWorker2 * This);
        
        HRESULT ( STDMETHODCALLTYPE *CreatePdb )( 
            ICorSvcWorker2 * This,
            /* [in] */ BSTR pAssemblyName,
            /* [in] */ BSTR pAppBaseOrConfig,
            /* [in] */ OptimizationScenario scenario,
            /* [in] */ BSTR pNativeImagePath,
            /* [in] */ BSTR pPdbPath);
        
        END_INTERFACE
    } ICorSvcWorker2Vtbl;

    interface ICorSvcWorker2
    {
        CONST_VTBL struct ICorSvcWorker2Vtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define ICorSvcWorker2_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define ICorSvcWorker2_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define ICorSvcWorker2_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define ICorSvcWorker2_SetPriority(This,priority)	\
    ( (This)->lpVtbl -> SetPriority(This,priority) ) 

#define ICorSvcWorker2_OptimizeAssembly(This,pAssemblyName,pApplicationName,scenario,loadAlwaysList,loadSometimesList,loadNeverList,pNativeImageIdentity)	\
    ( (This)->lpVtbl -> OptimizeAssembly(This,pAssemblyName,pApplicationName,scenario,loadAlwaysList,loadSometimesList,loadNeverList,pNativeImageIdentity) ) 

#define ICorSvcWorker2_DeleteNativeImage(This,pAssemblyName,pNativeImage)	\
    ( (This)->lpVtbl -> DeleteNativeImage(This,pAssemblyName,pNativeImage) ) 

#define ICorSvcWorker2_DisplayNativeImages(This,pAssemblyName)	\
    ( (This)->lpVtbl -> DisplayNativeImages(This,pAssemblyName) ) 

#define ICorSvcWorker2_GetCorSvcDependencies(This,pApplicationName,scenario,pCorSvcDependencies)	\
    ( (This)->lpVtbl -> GetCorSvcDependencies(This,pApplicationName,scenario,pCorSvcDependencies) ) 

#define ICorSvcWorker2_Stop(This)	\
    ( (This)->lpVtbl -> Stop(This) ) 


#define ICorSvcWorker2_CreatePdb(This,pAssemblyName,pAppBaseOrConfig,scenario,pNativeImagePath,pPdbPath)	\
    ( (This)->lpVtbl -> CreatePdb(This,pAssemblyName,pAppBaseOrConfig,scenario,pNativeImagePath,pPdbPath) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ICorSvcWorker2_INTERFACE_DEFINED__ */


#ifndef __ICorSvcWorker3_INTERFACE_DEFINED__
#define __ICorSvcWorker3_INTERFACE_DEFINED__

/* interface ICorSvcWorker3 */
/* [unique][uuid][oleautomation][object] */ 


EXTERN_C const IID IID_ICorSvcWorker3;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("DC516615-47BE-477e-8B55-C5ABE0D76B8F")
    ICorSvcWorker3 : public ICorSvcWorker2
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE CreatePdb2( 
            /* [in] */ BSTR pAssemblyName,
            /* [in] */ BSTR pAppBaseOrConfig,
            /* [in] */ OptimizationScenario scenario,
            /* [in] */ BSTR pNativeImagePath,
            /* [in] */ BSTR pPdbPath,
            /* [in] */ BOOL pdbLines,
            /* [in] */ BSTR managedPdbSearchPath) = 0;
        
    };
    
    
#else 	/* C style interface */

    typedef struct ICorSvcWorker3Vtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            ICorSvcWorker3 * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            ICorSvcWorker3 * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            ICorSvcWorker3 * This);
        
        HRESULT ( STDMETHODCALLTYPE *SetPriority )( 
            ICorSvcWorker3 * This,
            /* [in] */ SvcWorkerPriority priority);
        
        HRESULT ( STDMETHODCALLTYPE *OptimizeAssembly )( 
            ICorSvcWorker3 * This,
            /* [in] */ BSTR pAssemblyName,
            /* [in] */ BSTR pApplicationName,
            /* [in] */ OptimizationScenario scenario,
            /* [in] */ SAFEARRAY * loadAlwaysList,
            /* [in] */ SAFEARRAY * loadSometimesList,
            /* [in] */ SAFEARRAY * loadNeverList,
            /* [out] */ BSTR *pNativeImageIdentity);
        
        HRESULT ( STDMETHODCALLTYPE *DeleteNativeImage )( 
            ICorSvcWorker3 * This,
            /* [in] */ BSTR pAssemblyName,
            /* [in] */ BSTR pNativeImage);
        
        HRESULT ( STDMETHODCALLTYPE *DisplayNativeImages )( 
            ICorSvcWorker3 * This,
            /* [in] */ BSTR pAssemblyName);
        
        HRESULT ( STDMETHODCALLTYPE *GetCorSvcDependencies )( 
            ICorSvcWorker3 * This,
            /* [in] */ BSTR pApplicationName,
            /* [in] */ OptimizationScenario scenario,
            /* [out] */ ICorSvcDependencies **pCorSvcDependencies);
        
        HRESULT ( STDMETHODCALLTYPE *Stop )( 
            ICorSvcWorker3 * This);
        
        HRESULT ( STDMETHODCALLTYPE *CreatePdb )( 
            ICorSvcWorker3 * This,
            /* [in] */ BSTR pAssemblyName,
            /* [in] */ BSTR pAppBaseOrConfig,
            /* [in] */ OptimizationScenario scenario,
            /* [in] */ BSTR pNativeImagePath,
            /* [in] */ BSTR pPdbPath);
        
        HRESULT ( STDMETHODCALLTYPE *CreatePdb2 )( 
            ICorSvcWorker3 * This,
            /* [in] */ BSTR pAssemblyName,
            /* [in] */ BSTR pAppBaseOrConfig,
            /* [in] */ OptimizationScenario scenario,
            /* [in] */ BSTR pNativeImagePath,
            /* [in] */ BSTR pPdbPath,
            /* [in] */ BOOL pdbLines,
            /* [in] */ BSTR managedPdbSearchPath);
        
        END_INTERFACE
    } ICorSvcWorker3Vtbl;

    interface ICorSvcWorker3
    {
        CONST_VTBL struct ICorSvcWorker3Vtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define ICorSvcWorker3_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define ICorSvcWorker3_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define ICorSvcWorker3_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define ICorSvcWorker3_SetPriority(This,priority)	\
    ( (This)->lpVtbl -> SetPriority(This,priority) ) 

#define ICorSvcWorker3_OptimizeAssembly(This,pAssemblyName,pApplicationName,scenario,loadAlwaysList,loadSometimesList,loadNeverList,pNativeImageIdentity)	\
    ( (This)->lpVtbl -> OptimizeAssembly(This,pAssemblyName,pApplicationName,scenario,loadAlwaysList,loadSometimesList,loadNeverList,pNativeImageIdentity) ) 

#define ICorSvcWorker3_DeleteNativeImage(This,pAssemblyName,pNativeImage)	\
    ( (This)->lpVtbl -> DeleteNativeImage(This,pAssemblyName,pNativeImage) ) 

#define ICorSvcWorker3_DisplayNativeImages(This,pAssemblyName)	\
    ( (This)->lpVtbl -> DisplayNativeImages(This,pAssemblyName) ) 

#define ICorSvcWorker3_GetCorSvcDependencies(This,pApplicationName,scenario,pCorSvcDependencies)	\
    ( (This)->lpVtbl -> GetCorSvcDependencies(This,pApplicationName,scenario,pCorSvcDependencies) ) 

#define ICorSvcWorker3_Stop(This)	\
    ( (This)->lpVtbl -> Stop(This) ) 


#define ICorSvcWorker3_CreatePdb(This,pAssemblyName,pAppBaseOrConfig,scenario,pNativeImagePath,pPdbPath)	\
    ( (This)->lpVtbl -> CreatePdb(This,pAssemblyName,pAppBaseOrConfig,scenario,pNativeImagePath,pPdbPath) ) 


#define ICorSvcWorker3_CreatePdb2(This,pAssemblyName,pAppBaseOrConfig,scenario,pNativeImagePath,pPdbPath,pdbLines,managedPdbSearchPath)	\
    ( (This)->lpVtbl -> CreatePdb2(This,pAssemblyName,pAppBaseOrConfig,scenario,pNativeImagePath,pPdbPath,pdbLines,managedPdbSearchPath) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ICorSvcWorker3_INTERFACE_DEFINED__ */


#ifndef __ICorSvcSetPrivateAttributes_INTERFACE_DEFINED__
#define __ICorSvcSetPrivateAttributes_INTERFACE_DEFINED__

/* interface ICorSvcSetPrivateAttributes */
/* [unique][uuid][oleautomation][object] */ 


EXTERN_C const IID IID_ICorSvcSetPrivateAttributes;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("b18e0b40-c089-4350-8328-066c668bccc2")
    ICorSvcSetPrivateAttributes : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE SetNGenPrivateAttributes( 
            /* [in] */ NGenPrivateAttributes ngenPrivateAttributes) = 0;
        
    };
    
    
#else 	/* C style interface */

    typedef struct ICorSvcSetPrivateAttributesVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            ICorSvcSetPrivateAttributes * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            ICorSvcSetPrivateAttributes * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            ICorSvcSetPrivateAttributes * This);
        
        HRESULT ( STDMETHODCALLTYPE *SetNGenPrivateAttributes )( 
            ICorSvcSetPrivateAttributes * This,
            /* [in] */ NGenPrivateAttributes ngenPrivateAttributes);
        
        END_INTERFACE
    } ICorSvcSetPrivateAttributesVtbl;

    interface ICorSvcSetPrivateAttributes
    {
        CONST_VTBL struct ICorSvcSetPrivateAttributesVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define ICorSvcSetPrivateAttributes_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define ICorSvcSetPrivateAttributes_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define ICorSvcSetPrivateAttributes_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define ICorSvcSetPrivateAttributes_SetNGenPrivateAttributes(This,ngenPrivateAttributes)	\
    ( (This)->lpVtbl -> SetNGenPrivateAttributes(This,ngenPrivateAttributes) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ICorSvcSetPrivateAttributes_INTERFACE_DEFINED__ */


#ifndef __ICorSvcRepository_INTERFACE_DEFINED__
#define __ICorSvcRepository_INTERFACE_DEFINED__

/* interface ICorSvcRepository */
/* [unique][uuid][oleautomation][object] */ 


EXTERN_C const IID IID_ICorSvcRepository;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("d5346658-b5fd-4353-9647-07ad4783d5a0")
    ICorSvcRepository : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE SetRepository( 
            /* [in] */ BSTR pRepositoryDir,
            /* [in] */ RepositoryFlags repositoryFlags) = 0;
        
    };
    
    
#else 	/* C style interface */

    typedef struct ICorSvcRepositoryVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            ICorSvcRepository * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            ICorSvcRepository * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            ICorSvcRepository * This);
        
        HRESULT ( STDMETHODCALLTYPE *SetRepository )( 
            ICorSvcRepository * This,
            /* [in] */ BSTR pRepositoryDir,
            /* [in] */ RepositoryFlags repositoryFlags);
        
        END_INTERFACE
    } ICorSvcRepositoryVtbl;

    interface ICorSvcRepository
    {
        CONST_VTBL struct ICorSvcRepositoryVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define ICorSvcRepository_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define ICorSvcRepository_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define ICorSvcRepository_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define ICorSvcRepository_SetRepository(This,pRepositoryDir,repositoryFlags)	\
    ( (This)->lpVtbl -> SetRepository(This,pRepositoryDir,repositoryFlags) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ICorSvcRepository_INTERFACE_DEFINED__ */


#ifndef __ICorSvcAppX_INTERFACE_DEFINED__
#define __ICorSvcAppX_INTERFACE_DEFINED__

/* interface ICorSvcAppX */
/* [unique][uuid][object] */ 


EXTERN_C const IID IID_ICorSvcAppX;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("5c814791-559e-4f7f-83ce-184a4ccbae24")
    ICorSvcAppX : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE SetPackage( 
            /* [in] */ BSTR pPackageFullName) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE SetLocalAppDataDirectory( 
            /* [in] */ BSTR pLocalAppDataDirectory) = 0;
        
    };
    
    
#else 	/* C style interface */

    typedef struct ICorSvcAppXVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            ICorSvcAppX * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            ICorSvcAppX * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            ICorSvcAppX * This);
        
        HRESULT ( STDMETHODCALLTYPE *SetPackage )( 
            ICorSvcAppX * This,
            /* [in] */ BSTR pPackageFullName);
        
        HRESULT ( STDMETHODCALLTYPE *SetLocalAppDataDirectory )( 
            ICorSvcAppX * This,
            /* [in] */ BSTR pLocalAppDataDirectory);
        
        END_INTERFACE
    } ICorSvcAppXVtbl;

    interface ICorSvcAppX
    {
        CONST_VTBL struct ICorSvcAppXVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define ICorSvcAppX_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define ICorSvcAppX_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define ICorSvcAppX_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define ICorSvcAppX_SetPackage(This,pPackageFullName)	\
    ( (This)->lpVtbl -> SetPackage(This,pPackageFullName) ) 

#define ICorSvcAppX_SetLocalAppDataDirectory(This,pLocalAppDataDirectory)	\
    ( (This)->lpVtbl -> SetLocalAppDataDirectory(This,pLocalAppDataDirectory) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ICorSvcAppX_INTERFACE_DEFINED__ */


#ifndef __ICorSvcLogger_INTERFACE_DEFINED__
#define __ICorSvcLogger_INTERFACE_DEFINED__

/* interface ICorSvcLogger */
/* [unique][uuid][oleautomation][object] */ 


EXTERN_C const IID IID_ICorSvcLogger;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("d189ff1a-e266-4f13-9637-4b9522279ffc")
    ICorSvcLogger : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE Log( 
            /* [in] */ CorSvcLogLevel logLevel,
            /* [in] */ BSTR message) = 0;
        
    };
    
    
#else 	/* C style interface */

    typedef struct ICorSvcLoggerVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            ICorSvcLogger * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            ICorSvcLogger * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            ICorSvcLogger * This);
        
        HRESULT ( STDMETHODCALLTYPE *Log )( 
            ICorSvcLogger * This,
            /* [in] */ CorSvcLogLevel logLevel,
            /* [in] */ BSTR message);
        
        END_INTERFACE
    } ICorSvcLoggerVtbl;

    interface ICorSvcLogger
    {
        CONST_VTBL struct ICorSvcLoggerVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define ICorSvcLogger_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define ICorSvcLogger_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define ICorSvcLogger_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define ICorSvcLogger_Log(This,logLevel,message)	\
    ( (This)->lpVtbl -> Log(This,logLevel,message) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ICorSvcLogger_INTERFACE_DEFINED__ */


#ifndef __ICorSvcPooledWorker_INTERFACE_DEFINED__
#define __ICorSvcPooledWorker_INTERFACE_DEFINED__

/* interface ICorSvcPooledWorker */
/* [unique][uuid][oleautomation][object] */ 


EXTERN_C const IID IID_ICorSvcPooledWorker;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("0631e7e2-6046-4fde-8b6d-a09b64fda6f3")
    ICorSvcPooledWorker : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE CanReuseProcess( 
            /* [in] */ OptimizationScenario scenario,
            /* [in] */ ICorSvcLogger *pCorSvcLogger,
            /* [out] */ BOOL *pCanContinue) = 0;
        
    };
    
    
#else 	/* C style interface */

    typedef struct ICorSvcPooledWorkerVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            ICorSvcPooledWorker * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            ICorSvcPooledWorker * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            ICorSvcPooledWorker * This);
        
        HRESULT ( STDMETHODCALLTYPE *CanReuseProcess )( 
            ICorSvcPooledWorker * This,
            /* [in] */ OptimizationScenario scenario,
            /* [in] */ ICorSvcLogger *pCorSvcLogger,
            /* [out] */ BOOL *pCanContinue);
        
        END_INTERFACE
    } ICorSvcPooledWorkerVtbl;

    interface ICorSvcPooledWorker
    {
        CONST_VTBL struct ICorSvcPooledWorkerVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define ICorSvcPooledWorker_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define ICorSvcPooledWorker_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define ICorSvcPooledWorker_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define ICorSvcPooledWorker_CanReuseProcess(This,scenario,pCorSvcLogger,pCanContinue)	\
    ( (This)->lpVtbl -> CanReuseProcess(This,scenario,pCorSvcLogger,pCanContinue) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ICorSvcPooledWorker_INTERFACE_DEFINED__ */


#ifndef __ICorSvcBindToWorker_INTERFACE_DEFINED__
#define __ICorSvcBindToWorker_INTERFACE_DEFINED__

/* interface ICorSvcBindToWorker */
/* [unique][uuid][oleautomation][object] */ 


EXTERN_C const IID IID_ICorSvcBindToWorker;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("5c6fb596-4828-4ed5-b9dd-293dad736fb5")
    ICorSvcBindToWorker : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE BindToRuntimeWorker( 
            /* [in] */ BSTR pRuntimeVersion,
            /* [in] */ DWORD ParentProcessID,
            /* [in] */ BSTR pInterruptEventName,
            /* [in] */ ICorSvcLogger *pCorSvcLogger,
            /* [out] */ ICorSvcWorker **pCorSvcWorker) = 0;
        
    };
    
    
#else 	/* C style interface */

    typedef struct ICorSvcBindToWorkerVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            ICorSvcBindToWorker * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            ICorSvcBindToWorker * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            ICorSvcBindToWorker * This);
        
        HRESULT ( STDMETHODCALLTYPE *BindToRuntimeWorker )( 
            ICorSvcBindToWorker * This,
            /* [in] */ BSTR pRuntimeVersion,
            /* [in] */ DWORD ParentProcessID,
            /* [in] */ BSTR pInterruptEventName,
            /* [in] */ ICorSvcLogger *pCorSvcLogger,
            /* [out] */ ICorSvcWorker **pCorSvcWorker);
        
        END_INTERFACE
    } ICorSvcBindToWorkerVtbl;

    interface ICorSvcBindToWorker
    {
        CONST_VTBL struct ICorSvcBindToWorkerVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define ICorSvcBindToWorker_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define ICorSvcBindToWorker_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define ICorSvcBindToWorker_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define ICorSvcBindToWorker_BindToRuntimeWorker(This,pRuntimeVersion,ParentProcessID,pInterruptEventName,pCorSvcLogger,pCorSvcWorker)	\
    ( (This)->lpVtbl -> BindToRuntimeWorker(This,pRuntimeVersion,ParentProcessID,pInterruptEventName,pCorSvcLogger,pCorSvcWorker) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ICorSvcBindToWorker_INTERFACE_DEFINED__ */

#endif /* __mscorsvc_LIBRARY_DEFINED__ */

/* interface __MIDL_itf_mscorsvc_0000_0001 */
/* [local] */ 

typedef /* [public][public] */ 
enum __MIDL___MIDL_itf_mscorsvc_0000_0001_0001
    {
        Service_NoAction	= -1,
        Service_Start	= 0,
        Service_Stop	= 0x1,
        Service_Pause	= 0x2,
        Service_Continue	= 0x3,
        Service_Interrogate	= 0x4,
        Service_StartPaused	= 0x5
    } 	ControlServiceAction;

typedef struct _COR_SERVICE_STATUS
    {
    WCHAR sServiceName[ 64 ];
    DWORD dwServiceType;
    DWORD dwCurrentState;
    DWORD dwControlsAccepted;
    DWORD dwWin32ExitCode;
    DWORD dwServiceSpecificExitCode;
    DWORD dwCheckPoint;
    DWORD dwWaitHint;
    } 	COR_SERVICE_STATUS;

typedef struct _COR_SERVICE_STATUS *LPCOR_SERVICE_STATUS;

typedef struct _ServiceOptions
    {
    BOOL RunAsWindowsService;
    BOOL RunAsPrivateRuntime;
    BOOL StartPaused;
    } 	ServiceOptions;



extern RPC_IF_HANDLE __MIDL_itf_mscorsvc_0000_0001_v0_0_c_ifspec;
extern RPC_IF_HANDLE __MIDL_itf_mscorsvc_0000_0001_v0_0_s_ifspec;

#ifndef __ICorSvc_INTERFACE_DEFINED__
#define __ICorSvc_INTERFACE_DEFINED__

/* interface ICorSvc */
/* [unique][uuid][object] */ 


EXTERN_C const IID IID_ICorSvc;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("3eef5ff0-3680-4f20-8a8f-9051aca66b22")
    ICorSvc : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE GetServiceManagerInterface( 
            /* [in] */ IUnknown **pIUnknown) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE InstallService( void) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE UninstallService( void) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE ControlService( 
            /* [in] */ ControlServiceAction Action,
            /* [out] */ LPCOR_SERVICE_STATUS lpServiceStatus) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE RunService( 
            /* [in] */ ServiceOptions options) = 0;
        
    };
    
    
#else 	/* C style interface */

    typedef struct ICorSvcVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            ICorSvc * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            ICorSvc * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            ICorSvc * This);
        
        HRESULT ( STDMETHODCALLTYPE *GetServiceManagerInterface )( 
            ICorSvc * This,
            /* [in] */ IUnknown **pIUnknown);
        
        HRESULT ( STDMETHODCALLTYPE *InstallService )( 
            ICorSvc * This);
        
        HRESULT ( STDMETHODCALLTYPE *UninstallService )( 
            ICorSvc * This);
        
        HRESULT ( STDMETHODCALLTYPE *ControlService )( 
            ICorSvc * This,
            /* [in] */ ControlServiceAction Action,
            /* [out] */ LPCOR_SERVICE_STATUS lpServiceStatus);
        
        HRESULT ( STDMETHODCALLTYPE *RunService )( 
            ICorSvc * This,
            /* [in] */ ServiceOptions options);
        
        END_INTERFACE
    } ICorSvcVtbl;

    interface ICorSvc
    {
        CONST_VTBL struct ICorSvcVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define ICorSvc_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define ICorSvc_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define ICorSvc_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define ICorSvc_GetServiceManagerInterface(This,pIUnknown)	\
    ( (This)->lpVtbl -> GetServiceManagerInterface(This,pIUnknown) ) 

#define ICorSvc_InstallService(This)	\
    ( (This)->lpVtbl -> InstallService(This) ) 

#define ICorSvc_UninstallService(This)	\
    ( (This)->lpVtbl -> UninstallService(This) ) 

#define ICorSvc_ControlService(This,Action,lpServiceStatus)	\
    ( (This)->lpVtbl -> ControlService(This,Action,lpServiceStatus) ) 

#define ICorSvc_RunService(This,options)	\
    ( (This)->lpVtbl -> RunService(This,options) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ICorSvc_INTERFACE_DEFINED__ */


#ifndef __ICompileProgressNotification_INTERFACE_DEFINED__
#define __ICompileProgressNotification_INTERFACE_DEFINED__

/* interface ICompileProgressNotification */
/* [unique][uuid][object] */ 


EXTERN_C const IID IID_ICompileProgressNotification;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("01c10030-6c81-4671-bd51-14b184c673b2")
    ICompileProgressNotification : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE CompileStarted( 
            /* [in] */ DWORD cAssembliesToCompile,
            /* [in] */ DWORD cTimeEstimate) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE ProgressNotification( 
            /* [in] */ DWORD cAssembly,
            /* [in] */ BSTR pAssemblyName,
            /* [in] */ BOOL isStartNotification,
            /* [in] */ HRESULT hrResult,
            /* [in] */ BSTR errorExplanation,
            /* [in] */ DWORD cTimeRemainingEstimate) = 0;
        
    };
    
    
#else 	/* C style interface */

    typedef struct ICompileProgressNotificationVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            ICompileProgressNotification * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            ICompileProgressNotification * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            ICompileProgressNotification * This);
        
        HRESULT ( STDMETHODCALLTYPE *CompileStarted )( 
            ICompileProgressNotification * This,
            /* [in] */ DWORD cAssembliesToCompile,
            /* [in] */ DWORD cTimeEstimate);
        
        HRESULT ( STDMETHODCALLTYPE *ProgressNotification )( 
            ICompileProgressNotification * This,
            /* [in] */ DWORD cAssembly,
            /* [in] */ BSTR pAssemblyName,
            /* [in] */ BOOL isStartNotification,
            /* [in] */ HRESULT hrResult,
            /* [in] */ BSTR errorExplanation,
            /* [in] */ DWORD cTimeRemainingEstimate);
        
        END_INTERFACE
    } ICompileProgressNotificationVtbl;

    interface ICompileProgressNotification
    {
        CONST_VTBL struct ICompileProgressNotificationVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define ICompileProgressNotification_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define ICompileProgressNotification_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define ICompileProgressNotification_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define ICompileProgressNotification_CompileStarted(This,cAssembliesToCompile,cTimeEstimate)	\
    ( (This)->lpVtbl -> CompileStarted(This,cAssembliesToCompile,cTimeEstimate) ) 

#define ICompileProgressNotification_ProgressNotification(This,cAssembly,pAssemblyName,isStartNotification,hrResult,errorExplanation,cTimeRemainingEstimate)	\
    ( (This)->lpVtbl -> ProgressNotification(This,cAssembly,pAssemblyName,isStartNotification,hrResult,errorExplanation,cTimeRemainingEstimate) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ICompileProgressNotification_INTERFACE_DEFINED__ */


#ifndef __ICompileProgressNotification2_INTERFACE_DEFINED__
#define __ICompileProgressNotification2_INTERFACE_DEFINED__

/* interface ICompileProgressNotification2 */
/* [unique][uuid][object] */ 


EXTERN_C const IID IID_ICompileProgressNotification2;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("98E5BDE2-E9A0-4ADE-9CB2-6CD06FDB1A85")
    ICompileProgressNotification2 : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE CompileStarted( 
            /* [in] */ DWORD cAssembliesToCompile,
            /* [in] */ DWORD cTimeEstimate,
            /* [in] */ DWORD threadID) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE ProgressNotification( 
            /* [in] */ DWORD cAssembly,
            /* [in] */ BSTR pAssemblyName,
            /* [in] */ BOOL isStartNotification,
            /* [in] */ HRESULT hrResult,
            /* [in] */ BSTR errorExplanation,
            /* [in] */ DWORD cTimeRemainingEstimate,
            /* [in] */ DWORD threadID) = 0;
        
    };
    
    
#else 	/* C style interface */

    typedef struct ICompileProgressNotification2Vtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            ICompileProgressNotification2 * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            ICompileProgressNotification2 * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            ICompileProgressNotification2 * This);
        
        HRESULT ( STDMETHODCALLTYPE *CompileStarted )( 
            ICompileProgressNotification2 * This,
            /* [in] */ DWORD cAssembliesToCompile,
            /* [in] */ DWORD cTimeEstimate,
            /* [in] */ DWORD threadID);
        
        HRESULT ( STDMETHODCALLTYPE *ProgressNotification )( 
            ICompileProgressNotification2 * This,
            /* [in] */ DWORD cAssembly,
            /* [in] */ BSTR pAssemblyName,
            /* [in] */ BOOL isStartNotification,
            /* [in] */ HRESULT hrResult,
            /* [in] */ BSTR errorExplanation,
            /* [in] */ DWORD cTimeRemainingEstimate,
            /* [in] */ DWORD threadID);
        
        END_INTERFACE
    } ICompileProgressNotification2Vtbl;

    interface ICompileProgressNotification2
    {
        CONST_VTBL struct ICompileProgressNotification2Vtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define ICompileProgressNotification2_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define ICompileProgressNotification2_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define ICompileProgressNotification2_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define ICompileProgressNotification2_CompileStarted(This,cAssembliesToCompile,cTimeEstimate,threadID)	\
    ( (This)->lpVtbl -> CompileStarted(This,cAssembliesToCompile,cTimeEstimate,threadID) ) 

#define ICompileProgressNotification2_ProgressNotification(This,cAssembly,pAssemblyName,isStartNotification,hrResult,errorExplanation,cTimeRemainingEstimate,threadID)	\
    ( (This)->lpVtbl -> ProgressNotification(This,cAssembly,pAssemblyName,isStartNotification,hrResult,errorExplanation,cTimeRemainingEstimate,threadID) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ICompileProgressNotification2_INTERFACE_DEFINED__ */


/* interface __MIDL_itf_mscorsvc_0000_0004 */
/* [local] */ 

typedef /* [public][public] */ 
enum __MIDL___MIDL_itf_mscorsvc_0000_0004_0001
    {
        DefaultOptimizeFlags	= 0,
        TolerateCompilationFailures	= 0x1,
        OptimizeNGenQueueOnly	= 0x2
    } 	OptimizeFlags;



extern RPC_IF_HANDLE __MIDL_itf_mscorsvc_0000_0004_v0_0_c_ifspec;
extern RPC_IF_HANDLE __MIDL_itf_mscorsvc_0000_0004_v0_0_s_ifspec;

#ifndef __ICorSvcInstaller_INTERFACE_DEFINED__
#define __ICorSvcInstaller_INTERFACE_DEFINED__

/* interface ICorSvcInstaller */
/* [unique][uuid][object] */ 


EXTERN_C const IID IID_ICorSvcInstaller;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("0523feee-eb0e-4857-b2aa-db787521d077")
    ICorSvcInstaller : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE Install( 
            /* [in] */ BSTR path) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE Uninstall( 
            /* [in] */ BSTR path) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE Optimize( 
            /* [in] */ ICompileProgressNotification *pCompileProgressNotification,
            /* [in] */ OptimizeFlags optimizeFlags) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE SetLogger( 
            /* [in] */ ICorSvcLogger *pCorSvcLogger) = 0;
        
    };
    
    
#else 	/* C style interface */

    typedef struct ICorSvcInstallerVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            ICorSvcInstaller * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            ICorSvcInstaller * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            ICorSvcInstaller * This);
        
        HRESULT ( STDMETHODCALLTYPE *Install )( 
            ICorSvcInstaller * This,
            /* [in] */ BSTR path);
        
        HRESULT ( STDMETHODCALLTYPE *Uninstall )( 
            ICorSvcInstaller * This,
            /* [in] */ BSTR path);
        
        HRESULT ( STDMETHODCALLTYPE *Optimize )( 
            ICorSvcInstaller * This,
            /* [in] */ ICompileProgressNotification *pCompileProgressNotification,
            /* [in] */ OptimizeFlags optimizeFlags);
        
        HRESULT ( STDMETHODCALLTYPE *SetLogger )( 
            ICorSvcInstaller * This,
            /* [in] */ ICorSvcLogger *pCorSvcLogger);
        
        END_INTERFACE
    } ICorSvcInstallerVtbl;

    interface ICorSvcInstaller
    {
        CONST_VTBL struct ICorSvcInstallerVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define ICorSvcInstaller_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define ICorSvcInstaller_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define ICorSvcInstaller_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define ICorSvcInstaller_Install(This,path)	\
    ( (This)->lpVtbl -> Install(This,path) ) 

#define ICorSvcInstaller_Uninstall(This,path)	\
    ( (This)->lpVtbl -> Uninstall(This,path) ) 

#define ICorSvcInstaller_Optimize(This,pCompileProgressNotification,optimizeFlags)	\
    ( (This)->lpVtbl -> Optimize(This,pCompileProgressNotification,optimizeFlags) ) 

#define ICorSvcInstaller_SetLogger(This,pCorSvcLogger)	\
    ( (This)->lpVtbl -> SetLogger(This,pCorSvcLogger) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ICorSvcInstaller_INTERFACE_DEFINED__ */


/* interface __MIDL_itf_mscorsvc_0000_0005 */
/* [local] */ 

typedef /* [public][public][public][public][public] */ 
enum __MIDL___MIDL_itf_mscorsvc_0000_0005_0001
    {
        DefaultFlags	= 0,
        AllowPartialNames	= 0x1,
        KeepPriority	= 0x2,
        NoRoot	= 0x4
    } 	GeneralFlags;

typedef /* [public][public][public][public][public] */ 
enum __MIDL___MIDL_itf_mscorsvc_0000_0005_0002
    {
        Priority_None	= -1,
        Priority_0	= 0,
        Priority_1	= 0x1,
        Priority_2	= 0x2,
        Priority_3	= 0x3,
        Priority_Default	= Priority_3,
        Priority_Lowest	= Priority_3,
        Priority_LowestAggressive	= Priority_2,
        Priority_Highest	= Priority_0,
        Priority_Highest_Root	= Priority_1,
        Priority_Lowest_Root	= Priority_3
    } 	PriorityLevel;



extern RPC_IF_HANDLE __MIDL_itf_mscorsvc_0000_0005_v0_0_c_ifspec;
extern RPC_IF_HANDLE __MIDL_itf_mscorsvc_0000_0005_v0_0_s_ifspec;

#ifndef __ICorSvcAdvancedInstaller_INTERFACE_DEFINED__
#define __ICorSvcAdvancedInstaller_INTERFACE_DEFINED__

/* interface ICorSvcAdvancedInstaller */
/* [unique][uuid][object] */ 


EXTERN_C const IID IID_ICorSvcAdvancedInstaller;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("0871fb80-3ea0-47cc-9b51-d92e2aee75db")
    ICorSvcAdvancedInstaller : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE Install( 
            /* [in] */ BSTR path,
            /* [in] */ OptimizationScenario optScenario,
            /* [in] */ BSTR config,
            /* [in] */ GeneralFlags generalFlags,
            /* [in] */ PriorityLevel priorityLevel) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE Uninstall( 
            /* [in] */ BSTR path,
            /* [in] */ OptimizationScenario optScenario,
            /* [in] */ BSTR config,
            /* [in] */ GeneralFlags generalFlags) = 0;
        
    };
    
    
#else 	/* C style interface */

    typedef struct ICorSvcAdvancedInstallerVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            ICorSvcAdvancedInstaller * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            ICorSvcAdvancedInstaller * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            ICorSvcAdvancedInstaller * This);
        
        HRESULT ( STDMETHODCALLTYPE *Install )( 
            ICorSvcAdvancedInstaller * This,
            /* [in] */ BSTR path,
            /* [in] */ OptimizationScenario optScenario,
            /* [in] */ BSTR config,
            /* [in] */ GeneralFlags generalFlags,
            /* [in] */ PriorityLevel priorityLevel);
        
        HRESULT ( STDMETHODCALLTYPE *Uninstall )( 
            ICorSvcAdvancedInstaller * This,
            /* [in] */ BSTR path,
            /* [in] */ OptimizationScenario optScenario,
            /* [in] */ BSTR config,
            /* [in] */ GeneralFlags generalFlags);
        
        END_INTERFACE
    } ICorSvcAdvancedInstallerVtbl;

    interface ICorSvcAdvancedInstaller
    {
        CONST_VTBL struct ICorSvcAdvancedInstallerVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define ICorSvcAdvancedInstaller_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define ICorSvcAdvancedInstaller_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define ICorSvcAdvancedInstaller_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define ICorSvcAdvancedInstaller_Install(This,path,optScenario,config,generalFlags,priorityLevel)	\
    ( (This)->lpVtbl -> Install(This,path,optScenario,config,generalFlags,priorityLevel) ) 

#define ICorSvcAdvancedInstaller_Uninstall(This,path,optScenario,config,generalFlags)	\
    ( (This)->lpVtbl -> Uninstall(This,path,optScenario,config,generalFlags) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ICorSvcAdvancedInstaller_INTERFACE_DEFINED__ */


/* interface __MIDL_itf_mscorsvc_0000_0006 */
/* [local] */ 

typedef /* [public][public][public] */ 
enum __MIDL___MIDL_itf_mscorsvc_0000_0006_0001
    {
        UpdateDefault	= 0,
        Force	= 0x1,
        PostReboot	= 0x2
    } 	UpdateFlags;



extern RPC_IF_HANDLE __MIDL_itf_mscorsvc_0000_0006_v0_0_c_ifspec;
extern RPC_IF_HANDLE __MIDL_itf_mscorsvc_0000_0006_v0_0_s_ifspec;

#ifndef __ICorSvcOptimizer_INTERFACE_DEFINED__
#define __ICorSvcOptimizer_INTERFACE_DEFINED__

/* interface ICorSvcOptimizer */
/* [unique][uuid][object] */ 


EXTERN_C const IID IID_ICorSvcOptimizer;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("94af0ec4-c10d-45d4-a625-d68d1b02a396")
    ICorSvcOptimizer : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE Update( 
            /* [in] */ BSTR path,
            /* [in] */ UpdateFlags updateFlags,
            /* [in] */ GeneralFlags generalFlags) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE Display( 
            /* [in] */ BSTR path,
            /* [in] */ GeneralFlags generalFlags) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE ScheduleWork( 
            /* [in] */ PriorityLevel priorityLevel) = 0;
        
    };
    
    
#else 	/* C style interface */

    typedef struct ICorSvcOptimizerVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            ICorSvcOptimizer * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            ICorSvcOptimizer * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            ICorSvcOptimizer * This);
        
        HRESULT ( STDMETHODCALLTYPE *Update )( 
            ICorSvcOptimizer * This,
            /* [in] */ BSTR path,
            /* [in] */ UpdateFlags updateFlags,
            /* [in] */ GeneralFlags generalFlags);
        
        HRESULT ( STDMETHODCALLTYPE *Display )( 
            ICorSvcOptimizer * This,
            /* [in] */ BSTR path,
            /* [in] */ GeneralFlags generalFlags);
        
        HRESULT ( STDMETHODCALLTYPE *ScheduleWork )( 
            ICorSvcOptimizer * This,
            /* [in] */ PriorityLevel priorityLevel);
        
        END_INTERFACE
    } ICorSvcOptimizerVtbl;

    interface ICorSvcOptimizer
    {
        CONST_VTBL struct ICorSvcOptimizerVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define ICorSvcOptimizer_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define ICorSvcOptimizer_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define ICorSvcOptimizer_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define ICorSvcOptimizer_Update(This,path,updateFlags,generalFlags)	\
    ( (This)->lpVtbl -> Update(This,path,updateFlags,generalFlags) ) 

#define ICorSvcOptimizer_Display(This,path,generalFlags)	\
    ( (This)->lpVtbl -> Display(This,path,generalFlags) ) 

#define ICorSvcOptimizer_ScheduleWork(This,priorityLevel)	\
    ( (This)->lpVtbl -> ScheduleWork(This,priorityLevel) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ICorSvcOptimizer_INTERFACE_DEFINED__ */


#ifndef __ICorSvcOptimizer2_INTERFACE_DEFINED__
#define __ICorSvcOptimizer2_INTERFACE_DEFINED__

/* interface ICorSvcOptimizer2 */
/* [unique][uuid][object] */ 


EXTERN_C const IID IID_ICorSvcOptimizer2;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("ee3b09c2-0110-4b6e-a73f-a3d6562f98ab")
    ICorSvcOptimizer2 : public ICorSvcOptimizer
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE CreatePdb( 
            /* [in] */ BSTR nativeImagePath,
            /* [in] */ BSTR pdbPath) = 0;
        
    };
    
    
#else 	/* C style interface */

    typedef struct ICorSvcOptimizer2Vtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            ICorSvcOptimizer2 * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            ICorSvcOptimizer2 * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            ICorSvcOptimizer2 * This);
        
        HRESULT ( STDMETHODCALLTYPE *Update )( 
            ICorSvcOptimizer2 * This,
            /* [in] */ BSTR path,
            /* [in] */ UpdateFlags updateFlags,
            /* [in] */ GeneralFlags generalFlags);
        
        HRESULT ( STDMETHODCALLTYPE *Display )( 
            ICorSvcOptimizer2 * This,
            /* [in] */ BSTR path,
            /* [in] */ GeneralFlags generalFlags);
        
        HRESULT ( STDMETHODCALLTYPE *ScheduleWork )( 
            ICorSvcOptimizer2 * This,
            /* [in] */ PriorityLevel priorityLevel);
        
        HRESULT ( STDMETHODCALLTYPE *CreatePdb )( 
            ICorSvcOptimizer2 * This,
            /* [in] */ BSTR nativeImagePath,
            /* [in] */ BSTR pdbPath);
        
        END_INTERFACE
    } ICorSvcOptimizer2Vtbl;

    interface ICorSvcOptimizer2
    {
        CONST_VTBL struct ICorSvcOptimizer2Vtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define ICorSvcOptimizer2_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define ICorSvcOptimizer2_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define ICorSvcOptimizer2_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define ICorSvcOptimizer2_Update(This,path,updateFlags,generalFlags)	\
    ( (This)->lpVtbl -> Update(This,path,updateFlags,generalFlags) ) 

#define ICorSvcOptimizer2_Display(This,path,generalFlags)	\
    ( (This)->lpVtbl -> Display(This,path,generalFlags) ) 

#define ICorSvcOptimizer2_ScheduleWork(This,priorityLevel)	\
    ( (This)->lpVtbl -> ScheduleWork(This,priorityLevel) ) 


#define ICorSvcOptimizer2_CreatePdb(This,nativeImagePath,pdbPath)	\
    ( (This)->lpVtbl -> CreatePdb(This,nativeImagePath,pdbPath) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ICorSvcOptimizer2_INTERFACE_DEFINED__ */


#ifndef __ICorSvcOptimizer3_INTERFACE_DEFINED__
#define __ICorSvcOptimizer3_INTERFACE_DEFINED__

/* interface ICorSvcOptimizer3 */
/* [unique][uuid][object] */ 


EXTERN_C const IID IID_ICorSvcOptimizer3;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("6EED164F-61EE-4a07-ABE8-670F92B4B7A9")
    ICorSvcOptimizer3 : public ICorSvcOptimizer2
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE CreatePdb2( 
            /* [in] */ BSTR nativeImagePath,
            /* [in] */ BSTR pdbPath,
            /* [in] */ BOOL pdbLines,
            /* [in] */ BSTR managedPdbSearchPath) = 0;
        
    };
    
    
#else 	/* C style interface */

    typedef struct ICorSvcOptimizer3Vtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            ICorSvcOptimizer3 * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            ICorSvcOptimizer3 * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            ICorSvcOptimizer3 * This);
        
        HRESULT ( STDMETHODCALLTYPE *Update )( 
            ICorSvcOptimizer3 * This,
            /* [in] */ BSTR path,
            /* [in] */ UpdateFlags updateFlags,
            /* [in] */ GeneralFlags generalFlags);
        
        HRESULT ( STDMETHODCALLTYPE *Display )( 
            ICorSvcOptimizer3 * This,
            /* [in] */ BSTR path,
            /* [in] */ GeneralFlags generalFlags);
        
        HRESULT ( STDMETHODCALLTYPE *ScheduleWork )( 
            ICorSvcOptimizer3 * This,
            /* [in] */ PriorityLevel priorityLevel);
        
        HRESULT ( STDMETHODCALLTYPE *CreatePdb )( 
            ICorSvcOptimizer3 * This,
            /* [in] */ BSTR nativeImagePath,
            /* [in] */ BSTR pdbPath);
        
        HRESULT ( STDMETHODCALLTYPE *CreatePdb2 )( 
            ICorSvcOptimizer3 * This,
            /* [in] */ BSTR nativeImagePath,
            /* [in] */ BSTR pdbPath,
            /* [in] */ BOOL pdbLines,
            /* [in] */ BSTR managedPdbSearchPath);
        
        END_INTERFACE
    } ICorSvcOptimizer3Vtbl;

    interface ICorSvcOptimizer3
    {
        CONST_VTBL struct ICorSvcOptimizer3Vtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define ICorSvcOptimizer3_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define ICorSvcOptimizer3_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define ICorSvcOptimizer3_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define ICorSvcOptimizer3_Update(This,path,updateFlags,generalFlags)	\
    ( (This)->lpVtbl -> Update(This,path,updateFlags,generalFlags) ) 

#define ICorSvcOptimizer3_Display(This,path,generalFlags)	\
    ( (This)->lpVtbl -> Display(This,path,generalFlags) ) 

#define ICorSvcOptimizer3_ScheduleWork(This,priorityLevel)	\
    ( (This)->lpVtbl -> ScheduleWork(This,priorityLevel) ) 


#define ICorSvcOptimizer3_CreatePdb(This,nativeImagePath,pdbPath)	\
    ( (This)->lpVtbl -> CreatePdb(This,nativeImagePath,pdbPath) ) 


#define ICorSvcOptimizer3_CreatePdb2(This,nativeImagePath,pdbPath,pdbLines,managedPdbSearchPath)	\
    ( (This)->lpVtbl -> CreatePdb2(This,nativeImagePath,pdbPath,pdbLines,managedPdbSearchPath) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ICorSvcOptimizer3_INTERFACE_DEFINED__ */


/* interface __MIDL_itf_mscorsvc_0000_0009 */
/* [local] */ 

typedef /* [public][public] */ 
enum __MIDL___MIDL_itf_mscorsvc_0000_0009_0001
    {
        NewWorkAvailable	= 0,
        ClientWorkStart	= 0x1,
        ClientWorkDone	= 0x2,
        UpdatePostReboot	= 0x3,
        NewWorkAvailableWithDelay	= 0x4
    } 	ServiceNotification;



extern RPC_IF_HANDLE __MIDL_itf_mscorsvc_0000_0009_v0_0_c_ifspec;
extern RPC_IF_HANDLE __MIDL_itf_mscorsvc_0000_0009_v0_0_s_ifspec;

#ifndef __ICorSvcManager_INTERFACE_DEFINED__
#define __ICorSvcManager_INTERFACE_DEFINED__

/* interface ICorSvcManager */
/* [unique][uuid][object] */ 


EXTERN_C const IID IID_ICorSvcManager;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("8f416a48-d663-4a7e-9732-fbca3fc46ea8")
    ICorSvcManager : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE ScheduleWorkForSinglePriorityLevel( 
            /* [in] */ PriorityLevel priorityLevel,
            /* [in] */ BSTR pInterruptEventName,
            /* [out] */ BOOL *pWorkScheduled) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE Optimize( 
            /* [in] */ DWORD dwWorkerPriorityClass,
            /* [in] */ ICompileProgressNotification *pCompileProgressNotification,
            /* [in] */ BSTR pInterruptEventName) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE NotifyService( 
            ServiceNotification notification) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE IsWorkAvailable( 
            /* [in] */ PriorityLevel priorityLevel,
            /* [out] */ BOOL *pWorkAvailable) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE Update( 
            /* [in] */ UpdateFlags updateFlags,
            /* [in] */ BSTR pInterruptEventName) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE SetSvcLogger( 
            /* [in] */ ICorSvcLogger *pCorSvcLogger) = 0;
        
    };
    
    
#else 	/* C style interface */

    typedef struct ICorSvcManagerVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            ICorSvcManager * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            ICorSvcManager * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            ICorSvcManager * This);
        
        HRESULT ( STDMETHODCALLTYPE *ScheduleWorkForSinglePriorityLevel )( 
            ICorSvcManager * This,
            /* [in] */ PriorityLevel priorityLevel,
            /* [in] */ BSTR pInterruptEventName,
            /* [out] */ BOOL *pWorkScheduled);
        
        HRESULT ( STDMETHODCALLTYPE *Optimize )( 
            ICorSvcManager * This,
            /* [in] */ DWORD dwWorkerPriorityClass,
            /* [in] */ ICompileProgressNotification *pCompileProgressNotification,
            /* [in] */ BSTR pInterruptEventName);
        
        HRESULT ( STDMETHODCALLTYPE *NotifyService )( 
            ICorSvcManager * This,
            ServiceNotification notification);
        
        HRESULT ( STDMETHODCALLTYPE *IsWorkAvailable )( 
            ICorSvcManager * This,
            /* [in] */ PriorityLevel priorityLevel,
            /* [out] */ BOOL *pWorkAvailable);
        
        HRESULT ( STDMETHODCALLTYPE *Update )( 
            ICorSvcManager * This,
            /* [in] */ UpdateFlags updateFlags,
            /* [in] */ BSTR pInterruptEventName);
        
        HRESULT ( STDMETHODCALLTYPE *SetSvcLogger )( 
            ICorSvcManager * This,
            /* [in] */ ICorSvcLogger *pCorSvcLogger);
        
        END_INTERFACE
    } ICorSvcManagerVtbl;

    interface ICorSvcManager
    {
        CONST_VTBL struct ICorSvcManagerVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define ICorSvcManager_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define ICorSvcManager_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define ICorSvcManager_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define ICorSvcManager_ScheduleWorkForSinglePriorityLevel(This,priorityLevel,pInterruptEventName,pWorkScheduled)	\
    ( (This)->lpVtbl -> ScheduleWorkForSinglePriorityLevel(This,priorityLevel,pInterruptEventName,pWorkScheduled) ) 

#define ICorSvcManager_Optimize(This,dwWorkerPriorityClass,pCompileProgressNotification,pInterruptEventName)	\
    ( (This)->lpVtbl -> Optimize(This,dwWorkerPriorityClass,pCompileProgressNotification,pInterruptEventName) ) 

#define ICorSvcManager_NotifyService(This,notification)	\
    ( (This)->lpVtbl -> NotifyService(This,notification) ) 

#define ICorSvcManager_IsWorkAvailable(This,priorityLevel,pWorkAvailable)	\
    ( (This)->lpVtbl -> IsWorkAvailable(This,priorityLevel,pWorkAvailable) ) 

#define ICorSvcManager_Update(This,updateFlags,pInterruptEventName)	\
    ( (This)->lpVtbl -> Update(This,updateFlags,pInterruptEventName) ) 

#define ICorSvcManager_SetSvcLogger(This,pCorSvcLogger)	\
    ( (This)->lpVtbl -> SetSvcLogger(This,pCorSvcLogger) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ICorSvcManager_INTERFACE_DEFINED__ */


#ifndef __ICorSvcManager2_INTERFACE_DEFINED__
#define __ICorSvcManager2_INTERFACE_DEFINED__

/* interface ICorSvcManager2 */
/* [unique][uuid][object] */ 


EXTERN_C const IID IID_ICorSvcManager2;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("29626056-8031-441b-affa-7a82480058b3")
    ICorSvcManager2 : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE SetRuntimeVersion( 
            BSTR version) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE SetPackageMoniker( 
            BSTR moniker) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE SetLocalAppData( 
            BSTR directory) = 0;
        
    };
    
    
#else 	/* C style interface */

    typedef struct ICorSvcManager2Vtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            ICorSvcManager2 * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            ICorSvcManager2 * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            ICorSvcManager2 * This);
        
        HRESULT ( STDMETHODCALLTYPE *SetRuntimeVersion )( 
            ICorSvcManager2 * This,
            BSTR version);
        
        HRESULT ( STDMETHODCALLTYPE *SetPackageMoniker )( 
            ICorSvcManager2 * This,
            BSTR moniker);
        
        HRESULT ( STDMETHODCALLTYPE *SetLocalAppData )( 
            ICorSvcManager2 * This,
            BSTR directory);
        
        END_INTERFACE
    } ICorSvcManager2Vtbl;

    interface ICorSvcManager2
    {
        CONST_VTBL struct ICorSvcManager2Vtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define ICorSvcManager2_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define ICorSvcManager2_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define ICorSvcManager2_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define ICorSvcManager2_SetRuntimeVersion(This,version)	\
    ( (This)->lpVtbl -> SetRuntimeVersion(This,version) ) 

#define ICorSvcManager2_SetPackageMoniker(This,moniker)	\
    ( (This)->lpVtbl -> SetPackageMoniker(This,moniker) ) 

#define ICorSvcManager2_SetLocalAppData(This,directory)	\
    ( (This)->lpVtbl -> SetLocalAppData(This,directory) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ICorSvcManager2_INTERFACE_DEFINED__ */


#ifndef __ICorSvcSetLegacyServiceBehavior_INTERFACE_DEFINED__
#define __ICorSvcSetLegacyServiceBehavior_INTERFACE_DEFINED__

/* interface ICorSvcSetLegacyServiceBehavior */
/* [unique][uuid][object] */ 


EXTERN_C const IID IID_ICorSvcSetLegacyServiceBehavior;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("35e5d609-ec3d-4fc2-9ba2-5f99e42ff42f")
    ICorSvcSetLegacyServiceBehavior : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE SetLegacyServiceBehavior( void) = 0;
        
    };
    
    
#else 	/* C style interface */

    typedef struct ICorSvcSetLegacyServiceBehaviorVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            ICorSvcSetLegacyServiceBehavior * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            ICorSvcSetLegacyServiceBehavior * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            ICorSvcSetLegacyServiceBehavior * This);
        
        HRESULT ( STDMETHODCALLTYPE *SetLegacyServiceBehavior )( 
            ICorSvcSetLegacyServiceBehavior * This);
        
        END_INTERFACE
    } ICorSvcSetLegacyServiceBehaviorVtbl;

    interface ICorSvcSetLegacyServiceBehavior
    {
        CONST_VTBL struct ICorSvcSetLegacyServiceBehaviorVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define ICorSvcSetLegacyServiceBehavior_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define ICorSvcSetLegacyServiceBehavior_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define ICorSvcSetLegacyServiceBehavior_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define ICorSvcSetLegacyServiceBehavior_SetLegacyServiceBehavior(This)	\
    ( (This)->lpVtbl -> SetLegacyServiceBehavior(This) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ICorSvcSetLegacyServiceBehavior_INTERFACE_DEFINED__ */


#ifndef __ICorSvcSetTaskBootTriggerState_INTERFACE_DEFINED__
#define __ICorSvcSetTaskBootTriggerState_INTERFACE_DEFINED__

/* interface ICorSvcSetTaskBootTriggerState */
/* [unique][uuid][object] */ 


EXTERN_C const IID IID_ICorSvcSetTaskBootTriggerState;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("115466A4-7005-4CA3-971F-01F0A2C8EF09")
    ICorSvcSetTaskBootTriggerState : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE SetTaskBootTriggerState( 
            BOOL bEnabled) = 0;
        
    };
    
    
#else 	/* C style interface */

    typedef struct ICorSvcSetTaskBootTriggerStateVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            ICorSvcSetTaskBootTriggerState * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            ICorSvcSetTaskBootTriggerState * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            ICorSvcSetTaskBootTriggerState * This);
        
        HRESULT ( STDMETHODCALLTYPE *SetTaskBootTriggerState )( 
            ICorSvcSetTaskBootTriggerState * This,
            BOOL bEnabled);
        
        END_INTERFACE
    } ICorSvcSetTaskBootTriggerStateVtbl;

    interface ICorSvcSetTaskBootTriggerState
    {
        CONST_VTBL struct ICorSvcSetTaskBootTriggerStateVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define ICorSvcSetTaskBootTriggerState_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define ICorSvcSetTaskBootTriggerState_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define ICorSvcSetTaskBootTriggerState_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define ICorSvcSetTaskBootTriggerState_SetTaskBootTriggerState(This,bEnabled)	\
    ( (This)->lpVtbl -> SetTaskBootTriggerState(This,bEnabled) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ICorSvcSetTaskBootTriggerState_INTERFACE_DEFINED__ */


#ifndef __ICorSvcSetTaskDelayStartTriggerState_INTERFACE_DEFINED__
#define __ICorSvcSetTaskDelayStartTriggerState_INTERFACE_DEFINED__

/* interface ICorSvcSetTaskDelayStartTriggerState */
/* [unique][uuid][object] */ 


EXTERN_C const IID IID_ICorSvcSetTaskDelayStartTriggerState;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("261DD1E3-F07E-4B8D-B54E-F26889413626")
    ICorSvcSetTaskDelayStartTriggerState : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE SetTaskDelayStartTriggerState( 
            BOOL bEnabled) = 0;
        
    };
    
    
#else 	/* C style interface */

    typedef struct ICorSvcSetTaskDelayStartTriggerStateVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            ICorSvcSetTaskDelayStartTriggerState * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            ICorSvcSetTaskDelayStartTriggerState * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            ICorSvcSetTaskDelayStartTriggerState * This);
        
        HRESULT ( STDMETHODCALLTYPE *SetTaskDelayStartTriggerState )( 
            ICorSvcSetTaskDelayStartTriggerState * This,
            BOOL bEnabled);
        
        END_INTERFACE
    } ICorSvcSetTaskDelayStartTriggerStateVtbl;

    interface ICorSvcSetTaskDelayStartTriggerState
    {
        CONST_VTBL struct ICorSvcSetTaskDelayStartTriggerStateVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define ICorSvcSetTaskDelayStartTriggerState_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define ICorSvcSetTaskDelayStartTriggerState_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define ICorSvcSetTaskDelayStartTriggerState_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define ICorSvcSetTaskDelayStartTriggerState_SetTaskDelayStartTriggerState(This,bEnabled)	\
    ( (This)->lpVtbl -> SetTaskDelayStartTriggerState(This,bEnabled) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ICorSvcSetTaskDelayStartTriggerState_INTERFACE_DEFINED__ */


/* Additional Prototypes for ALL interfaces */

unsigned long             __RPC_USER  BSTR_UserSize(     unsigned long *, unsigned long            , BSTR * ); 
unsigned char * __RPC_USER  BSTR_UserMarshal(  unsigned long *, unsigned char *, BSTR * ); 
unsigned char * __RPC_USER  BSTR_UserUnmarshal(unsigned long *, unsigned char *, BSTR * ); 
void                      __RPC_USER  BSTR_UserFree(     unsigned long *, BSTR * ); 

/* end of Additional Prototypes */

#ifdef __cplusplus
}
#endif

#endif


