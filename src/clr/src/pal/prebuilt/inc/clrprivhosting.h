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

#ifndef __CLRPrivHosting_h__
#define __CLRPrivHosting_h__

#if defined(_MSC_VER) && (_MSC_VER >= 1020)
#pragma once
#endif

/* Forward Declarations */ 

#ifndef __CLRPrivRuntime_FWD_DEFINED__
#define __CLRPrivRuntime_FWD_DEFINED__

#ifdef __cplusplus
typedef class CLRPrivRuntime CLRPrivRuntime;
#else
typedef struct CLRPrivRuntime CLRPrivRuntime;
#endif /* __cplusplus */

#endif 	/* __CLRPrivRuntime_FWD_DEFINED__ */


#ifndef __ICLRPrivRuntime_FWD_DEFINED__
#define __ICLRPrivRuntime_FWD_DEFINED__
typedef interface ICLRPrivRuntime ICLRPrivRuntime;

#endif 	/* __ICLRPrivRuntime_FWD_DEFINED__ */


/* header files for imported files */
#include "unknwn.h"
#include "clrprivbinding.h"

#ifdef __cplusplus
extern "C"{
#endif 


/* interface __MIDL_itf_CLRPrivHosting_0000_0000 */
/* [local] */ 




extern RPC_IF_HANDLE __MIDL_itf_CLRPrivHosting_0000_0000_v0_0_c_ifspec;
extern RPC_IF_HANDLE __MIDL_itf_CLRPrivHosting_0000_0000_v0_0_s_ifspec;


#ifndef __CLRPrivHosting_LIBRARY_DEFINED__
#define __CLRPrivHosting_LIBRARY_DEFINED__

/* library CLRPrivHosting */
/* [uuid] */ 


EXTERN_C const IID LIBID_CLRPrivHosting;

EXTERN_C const CLSID CLSID_CLRPrivRuntime;

#ifdef __cplusplus

class DECLSPEC_UUID("BC1B53A8-DCBC-43B2-BB17-1E4061447AE8")
CLRPrivRuntime;
#endif
#endif /* __CLRPrivHosting_LIBRARY_DEFINED__ */

#ifndef __ICLRPrivRuntime_INTERFACE_DEFINED__
#define __ICLRPrivRuntime_INTERFACE_DEFINED__

/* interface ICLRPrivRuntime */
/* [object][local][version][uuid] */ 


EXTERN_C const IID IID_ICLRPrivRuntime;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("BC1B53A8-DCBC-43B2-BB17-1E4061447AE9")
    ICLRPrivRuntime : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE GetInterface( 
            /* [in] */ REFCLSID rclsid,
            /* [in] */ REFIID riid,
            /* [retval][iid_is][out] */ LPVOID *ppUnk) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE CreateAppDomain( 
            /* [string][in] */ LPCWSTR pwzFriendlyName,
            /* [in] */ ICLRPrivBinder *pBinder,
            /* [retval][out] */ LPDWORD pdwAppDomainId) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE CreateDelegate( 
            /* [in] */ DWORD appDomainID,
            /* [string][in] */ LPCWSTR wszAssemblyName,
            /* [string][in] */ LPCWSTR wszClassName,
            /* [string][in] */ LPCWSTR wszMethodName,
            /* [retval][out] */ LPVOID *ppvDelegate) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE ExecuteMain( 
            /* [in] */ ICLRPrivBinder *pBinder,
            /* [retval][out] */ int *pRetVal) = 0;
        
    };
    
    
#else 	/* C style interface */

    typedef struct ICLRPrivRuntimeVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            ICLRPrivRuntime * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            ICLRPrivRuntime * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            ICLRPrivRuntime * This);
        
        HRESULT ( STDMETHODCALLTYPE *GetInterface )( 
            ICLRPrivRuntime * This,
            /* [in] */ REFCLSID rclsid,
            /* [in] */ REFIID riid,
            /* [retval][iid_is][out] */ LPVOID *ppUnk);
        
        HRESULT ( STDMETHODCALLTYPE *CreateAppDomain )( 
            ICLRPrivRuntime * This,
            /* [string][in] */ LPCWSTR pwzFriendlyName,
            /* [in] */ ICLRPrivBinder *pBinder,
            /* [retval][out] */ LPDWORD pdwAppDomainId);
        
        HRESULT ( STDMETHODCALLTYPE *CreateDelegate )( 
            ICLRPrivRuntime * This,
            /* [in] */ DWORD appDomainID,
            /* [string][in] */ LPCWSTR wszAssemblyName,
            /* [string][in] */ LPCWSTR wszClassName,
            /* [string][in] */ LPCWSTR wszMethodName,
            /* [retval][out] */ LPVOID *ppvDelegate);
        
        HRESULT ( STDMETHODCALLTYPE *ExecuteMain )( 
            ICLRPrivRuntime * This,
            /* [in] */ ICLRPrivBinder *pBinder,
            /* [retval][out] */ int *pRetVal);
        
        END_INTERFACE
    } ICLRPrivRuntimeVtbl;

    interface ICLRPrivRuntime
    {
        CONST_VTBL struct ICLRPrivRuntimeVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define ICLRPrivRuntime_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define ICLRPrivRuntime_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define ICLRPrivRuntime_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define ICLRPrivRuntime_GetInterface(This,rclsid,riid,ppUnk)	\
    ( (This)->lpVtbl -> GetInterface(This,rclsid,riid,ppUnk) ) 

#define ICLRPrivRuntime_CreateAppDomain(This,pwzFriendlyName,pBinder,pdwAppDomainId)	\
    ( (This)->lpVtbl -> CreateAppDomain(This,pwzFriendlyName,pBinder,pdwAppDomainId) ) 

#define ICLRPrivRuntime_CreateDelegate(This,appDomainID,wszAssemblyName,wszClassName,wszMethodName,ppvDelegate)	\
    ( (This)->lpVtbl -> CreateDelegate(This,appDomainID,wszAssemblyName,wszClassName,wszMethodName,ppvDelegate) ) 

#define ICLRPrivRuntime_ExecuteMain(This,pBinder,pRetVal)	\
    ( (This)->lpVtbl -> ExecuteMain(This,pBinder,pRetVal) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ICLRPrivRuntime_INTERFACE_DEFINED__ */


/* Additional Prototypes for ALL interfaces */

/* end of Additional Prototypes */

#ifdef __cplusplus
}
#endif

#endif


