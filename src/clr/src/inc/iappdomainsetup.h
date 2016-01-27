// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// 

// ********************************************************
// ********************************************************
// ********************************************************
//
// !!!! DON'T USE THIS FILE, IT WILL BE OBSOLETE SOON !!!!
//
// ********************************************************
// ********************************************************
// ********************************************************




#ifndef FEATURE_COMINTEROP
#error FEATURE_COMINTEROP is required for this file
#endif // FEATURE_COMINTEROP



#ifdef _MSC_VER
#pragma warning( disable: 4049 )  /* more than 64k source lines */
#endif

/* this ALWAYS GENERATED file contains the definitions for the interfaces */


 /* File created by MIDL compiler version 6.00.0338 */
/* at Wed Jan 17 16:59:41 2001
 */
/* Compiler settings for IAppDomainSetup.idl:
    Os, W1, Zp8, env=Win32 (32b run)
    protocol : dce , ms_ext, c_ext
    error checks: allocation ref bounds_check enum stub_data 
    VC __declspec() decoration level: 
         __declspec(uuid()), __declspec(selectany), __declspec(novtable)
         DECLSPEC_UUID(), MIDL_INTERFACE()
*/
//@@MIDL_FILE_HEADING(  )


/* verify that the <rpcndr.h> version is high enough to compile this file*/
#ifndef __REQUIRED_RPCNDR_H_VERSION__
#define __REQUIRED_RPCNDR_H_VERSION__ 440
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

#ifndef __IAppDomainSetup_h__
#define __IAppDomainSetup_h__

#if defined(_MSC_VER) && (_MSC_VER >= 1020)
#pragma once
#endif

/* Forward Declarations */ 

#ifndef __IAppDomainSetup_FWD_DEFINED__
#define __IAppDomainSetup_FWD_DEFINED__
typedef interface IAppDomainSetup IAppDomainSetup;
#endif 	/* __IAppDomainSetup_FWD_DEFINED__ */


/* header files for imported files */
#include "unknwn.h"

#ifdef __cplusplus
extern "C"{
#endif 

void * __RPC_USER MIDL_user_allocate(size_t);
void __RPC_USER MIDL_user_free( void * ); 

#ifndef __IAppDomainSetup_INTERFACE_DEFINED__
#define __IAppDomainSetup_INTERFACE_DEFINED__

/* interface IAppDomainSetup */
/* [object][oleautomation][version][uuid] */ 


EXTERN_C const IID IID_IAppDomainSetup;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("27FFF232-A7A8-40DD-8D4A-734AD59FCD41")
    IAppDomainSetup : public IUnknown
    {
    public:
        virtual /* [propget] */ HRESULT __stdcall get_ApplicationBase( 
            /* [retval][out] */ BSTR *pRetVal) = 0;
        
        virtual /* [propput] */ HRESULT __stdcall put_ApplicationBase( 
            /* [in] */ BSTR pRetVal) = 0;
        
        virtual /* [propget] */ HRESULT __stdcall get_ApplicationName( 
            /* [retval][out] */ BSTR *pRetVal) = 0;
        
        virtual /* [propput] */ HRESULT __stdcall put_ApplicationName( 
            /* [in] */ BSTR pRetVal) = 0;
        
        virtual /* [propget] */ HRESULT __stdcall get_CachePath( 
            /* [retval][out] */ BSTR *pRetVal) = 0;
        
        virtual /* [propput] */ HRESULT __stdcall put_CachePath( 
            /* [in] */ BSTR pRetVal) = 0;
        
        virtual /* [propget] */ HRESULT __stdcall get_ConfigurationFile( 
            /* [retval][out] */ BSTR *pRetVal) = 0;
        
        virtual /* [propput] */ HRESULT __stdcall put_ConfigurationFile( 
            /* [in] */ BSTR pRetVal) = 0;
        
        virtual /* [propget] */ HRESULT __stdcall get_DynamicBase( 
            /* [retval][out] */ BSTR *pRetVal) = 0;
        
        virtual /* [propput] */ HRESULT __stdcall put_DynamicBase( 
            /* [in] */ BSTR pRetVal) = 0;
        
        virtual /* [propget] */ HRESULT __stdcall get_LicenseFile( 
            /* [retval][out] */ BSTR *pRetVal) = 0;
        
        virtual /* [propput] */ HRESULT __stdcall put_LicenseFile( 
            /* [in] */ BSTR pRetVal) = 0;
        
        virtual /* [propget] */ HRESULT __stdcall get_PrivateBinPath( 
            /* [retval][out] */ BSTR *pRetVal) = 0;
        
        virtual /* [propput] */ HRESULT __stdcall put_PrivateBinPath( 
            /* [in] */ BSTR pRetVal) = 0;
        
        virtual /* [propget] */ HRESULT __stdcall get_PrivateBinPathProbe( 
            /* [retval][out] */ BSTR *pRetVal) = 0;
        
        virtual /* [propput] */ HRESULT __stdcall put_PrivateBinPathProbe( 
            /* [in] */ BSTR pRetVal) = 0;
        
        virtual /* [propget] */ HRESULT __stdcall get_ShadowCopyDirectories( 
            /* [retval][out] */ BSTR *pRetVal) = 0;
        
        virtual /* [propput] */ HRESULT __stdcall put_ShadowCopyDirectories( 
            /* [in] */ BSTR pRetVal) = 0;
        
        virtual /* [propget] */ HRESULT __stdcall get_ShadowCopyFiles( 
            /* [retval][out] */ BSTR *pRetVal) = 0;
        
        virtual /* [propput] */ HRESULT __stdcall put_ShadowCopyFiles( 
            /* [in] */ BSTR pRetVal) = 0;
        
    };
    
#else 	/* C style interface */

    typedef struct IAppDomainSetupVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            IAppDomainSetup * This,
            /* [in] */ REFIID riid,
            /* [iid_is][out] */ void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            IAppDomainSetup * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            IAppDomainSetup * This);
        
        /* [propget] */ HRESULT ( __stdcall *get_ApplicationBase )( 
            IAppDomainSetup * This,
            /* [retval][out] */ BSTR *pRetVal);
        
        /* [propput] */ HRESULT ( __stdcall *put_ApplicationBase )( 
            IAppDomainSetup * This,
            /* [in] */ BSTR pRetVal);
        
        /* [propget] */ HRESULT ( __stdcall *get_ApplicationName )( 
            IAppDomainSetup * This,
            /* [retval][out] */ BSTR *pRetVal);
        
        /* [propput] */ HRESULT ( __stdcall *put_ApplicationName )( 
            IAppDomainSetup * This,
            /* [in] */ BSTR pRetVal);
        
        /* [propget] */ HRESULT ( __stdcall *get_CachePath )( 
            IAppDomainSetup * This,
            /* [retval][out] */ BSTR *pRetVal);
        
        /* [propput] */ HRESULT ( __stdcall *put_CachePath )( 
            IAppDomainSetup * This,
            /* [in] */ BSTR pRetVal);
        
        /* [propget] */ HRESULT ( __stdcall *get_ConfigurationFile )( 
            IAppDomainSetup * This,
            /* [retval][out] */ BSTR *pRetVal);
        
        /* [propput] */ HRESULT ( __stdcall *put_ConfigurationFile )( 
            IAppDomainSetup * This,
            /* [in] */ BSTR pRetVal);
        
        /* [propget] */ HRESULT ( __stdcall *get_DynamicBase )( 
            IAppDomainSetup * This,
            /* [retval][out] */ BSTR *pRetVal);
        
        /* [propput] */ HRESULT ( __stdcall *put_DynamicBase )( 
            IAppDomainSetup * This,
            /* [in] */ BSTR pRetVal);
        
        /* [propget] */ HRESULT ( __stdcall *get_LicenseFile )( 
            IAppDomainSetup * This,
            /* [retval][out] */ BSTR *pRetVal);
        
        /* [propput] */ HRESULT ( __stdcall *put_LicenseFile )( 
            IAppDomainSetup * This,
            /* [in] */ BSTR pRetVal);
        
        /* [propget] */ HRESULT ( __stdcall *get_PrivateBinPath )( 
            IAppDomainSetup * This,
            /* [retval][out] */ BSTR *pRetVal);
        
        /* [propput] */ HRESULT ( __stdcall *put_PrivateBinPath )( 
            IAppDomainSetup * This,
            /* [in] */ BSTR pRetVal);
        
        /* [propget] */ HRESULT ( __stdcall *get_PrivateBinPathProbe )( 
            IAppDomainSetup * This,
            /* [retval][out] */ BSTR *pRetVal);
        
        /* [propput] */ HRESULT ( __stdcall *put_PrivateBinPathProbe )( 
            IAppDomainSetup * This,
            /* [in] */ BSTR pRetVal);
        
        /* [propget] */ HRESULT ( __stdcall *get_ShadowCopyDirectories )( 
            IAppDomainSetup * This,
            /* [retval][out] */ BSTR *pRetVal);
        
        /* [propput] */ HRESULT ( __stdcall *put_ShadowCopyDirectories )( 
            IAppDomainSetup * This,
            /* [in] */ BSTR pRetVal);
        
        /* [propget] */ HRESULT ( __stdcall *get_ShadowCopyFiles )( 
            IAppDomainSetup * This,
            /* [retval][out] */ BSTR *pRetVal);
        
        /* [propput] */ HRESULT ( __stdcall *put_ShadowCopyFiles )( 
            IAppDomainSetup * This,
            /* [in] */ BSTR pRetVal);
        
        END_INTERFACE
    } IAppDomainSetupVtbl;

    interface IAppDomainSetup
    {
        CONST_VTBL struct IAppDomainSetupVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define IAppDomainSetup_QueryInterface(This,riid,ppvObject)	\
    (This)->lpVtbl -> QueryInterface(This,riid,ppvObject)

#define IAppDomainSetup_AddRef(This)	\
    (This)->lpVtbl -> AddRef(This)

#define IAppDomainSetup_Release(This)	\
    (This)->lpVtbl -> Release(This)


#define IAppDomainSetup_get_ApplicationBase(This,pRetVal)	\
    (This)->lpVtbl -> get_ApplicationBase(This,pRetVal)

#define IAppDomainSetup_put_ApplicationBase(This,pRetVal)	\
    (This)->lpVtbl -> put_ApplicationBase(This,pRetVal)

#define IAppDomainSetup_get_ApplicationName(This,pRetVal)	\
    (This)->lpVtbl -> get_ApplicationName(This,pRetVal)

#define IAppDomainSetup_put_ApplicationName(This,pRetVal)	\
    (This)->lpVtbl -> put_ApplicationName(This,pRetVal)

#define IAppDomainSetup_get_CachePath(This,pRetVal)	\
    (This)->lpVtbl -> get_CachePath(This,pRetVal)

#define IAppDomainSetup_put_CachePath(This,pRetVal)	\
    (This)->lpVtbl -> put_CachePath(This,pRetVal)

#define IAppDomainSetup_get_ConfigurationFile(This,pRetVal)	\
    (This)->lpVtbl -> get_ConfigurationFile(This,pRetVal)

#define IAppDomainSetup_put_ConfigurationFile(This,pRetVal)	\
    (This)->lpVtbl -> put_ConfigurationFile(This,pRetVal)

#define IAppDomainSetup_get_DynamicBase(This,pRetVal)	\
    (This)->lpVtbl -> get_DynamicBase(This,pRetVal)

#define IAppDomainSetup_put_DynamicBase(This,pRetVal)	\
    (This)->lpVtbl -> put_DynamicBase(This,pRetVal)

#define IAppDomainSetup_get_LicenseFile(This,pRetVal)	\
    (This)->lpVtbl -> get_LicenseFile(This,pRetVal)

#define IAppDomainSetup_put_LicenseFile(This,pRetVal)	\
    (This)->lpVtbl -> put_LicenseFile(This,pRetVal)

#define IAppDomainSetup_get_PrivateBinPath(This,pRetVal)	\
    (This)->lpVtbl -> get_PrivateBinPath(This,pRetVal)

#define IAppDomainSetup_put_PrivateBinPath(This,pRetVal)	\
    (This)->lpVtbl -> put_PrivateBinPath(This,pRetVal)

#define IAppDomainSetup_get_PrivateBinPathProbe(This,pRetVal)	\
    (This)->lpVtbl -> get_PrivateBinPathProbe(This,pRetVal)

#define IAppDomainSetup_put_PrivateBinPathProbe(This,pRetVal)	\
    (This)->lpVtbl -> put_PrivateBinPathProbe(This,pRetVal)

#define IAppDomainSetup_get_ShadowCopyDirectories(This,pRetVal)	\
    (This)->lpVtbl -> get_ShadowCopyDirectories(This,pRetVal)

#define IAppDomainSetup_put_ShadowCopyDirectories(This,pRetVal)	\
    (This)->lpVtbl -> put_ShadowCopyDirectories(This,pRetVal)

#define IAppDomainSetup_get_ShadowCopyFiles(This,pRetVal)	\
    (This)->lpVtbl -> get_ShadowCopyFiles(This,pRetVal)

#define IAppDomainSetup_put_ShadowCopyFiles(This,pRetVal)	\
    (This)->lpVtbl -> put_ShadowCopyFiles(This,pRetVal)

#endif /* COBJMACROS */


#endif 	/* C style interface */



/* [propget] */ HRESULT __stdcall IAppDomainSetup_get_ApplicationBase_Proxy( 
    IAppDomainSetup * This,
    /* [retval][out] */ BSTR *pRetVal);


void __RPC_STUB IAppDomainSetup_get_ApplicationBase_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


/* [propput] */ HRESULT __stdcall IAppDomainSetup_put_ApplicationBase_Proxy( 
    IAppDomainSetup * This,
    /* [in] */ BSTR pRetVal);


void __RPC_STUB IAppDomainSetup_put_ApplicationBase_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


/* [propget] */ HRESULT __stdcall IAppDomainSetup_get_ApplicationName_Proxy( 
    IAppDomainSetup * This,
    /* [retval][out] */ BSTR *pRetVal);


void __RPC_STUB IAppDomainSetup_get_ApplicationName_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


/* [propput] */ HRESULT __stdcall IAppDomainSetup_put_ApplicationName_Proxy( 
    IAppDomainSetup * This,
    /* [in] */ BSTR pRetVal);


void __RPC_STUB IAppDomainSetup_put_ApplicationName_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


/* [propget] */ HRESULT __stdcall IAppDomainSetup_get_CachePath_Proxy( 
    IAppDomainSetup * This,
    /* [retval][out] */ BSTR *pRetVal);


void __RPC_STUB IAppDomainSetup_get_CachePath_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


/* [propput] */ HRESULT __stdcall IAppDomainSetup_put_CachePath_Proxy( 
    IAppDomainSetup * This,
    /* [in] */ BSTR pRetVal);


void __RPC_STUB IAppDomainSetup_put_CachePath_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


/* [propget] */ HRESULT __stdcall IAppDomainSetup_get_ConfigurationFile_Proxy( 
    IAppDomainSetup * This,
    /* [retval][out] */ BSTR *pRetVal);


void __RPC_STUB IAppDomainSetup_get_ConfigurationFile_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


/* [propput] */ HRESULT __stdcall IAppDomainSetup_put_ConfigurationFile_Proxy( 
    IAppDomainSetup * This,
    /* [in] */ BSTR pRetVal);


void __RPC_STUB IAppDomainSetup_put_ConfigurationFile_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


/* [propget] */ HRESULT __stdcall IAppDomainSetup_get_DynamicBase_Proxy( 
    IAppDomainSetup * This,
    /* [retval][out] */ BSTR *pRetVal);


void __RPC_STUB IAppDomainSetup_get_DynamicBase_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


/* [propput] */ HRESULT __stdcall IAppDomainSetup_put_DynamicBase_Proxy( 
    IAppDomainSetup * This,
    /* [in] */ BSTR pRetVal);


void __RPC_STUB IAppDomainSetup_put_DynamicBase_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


/* [propget] */ HRESULT __stdcall IAppDomainSetup_get_LicenseFile_Proxy( 
    IAppDomainSetup * This,
    /* [retval][out] */ BSTR *pRetVal);


void __RPC_STUB IAppDomainSetup_get_LicenseFile_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


/* [propput] */ HRESULT __stdcall IAppDomainSetup_put_LicenseFile_Proxy( 
    IAppDomainSetup * This,
    /* [in] */ BSTR pRetVal);


void __RPC_STUB IAppDomainSetup_put_LicenseFile_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


/* [propget] */ HRESULT __stdcall IAppDomainSetup_get_PrivateBinPath_Proxy( 
    IAppDomainSetup * This,
    /* [retval][out] */ BSTR *pRetVal);


void __RPC_STUB IAppDomainSetup_get_PrivateBinPath_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


/* [propput] */ HRESULT __stdcall IAppDomainSetup_put_PrivateBinPath_Proxy( 
    IAppDomainSetup * This,
    /* [in] */ BSTR pRetVal);


void __RPC_STUB IAppDomainSetup_put_PrivateBinPath_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


/* [propget] */ HRESULT __stdcall IAppDomainSetup_get_PrivateBinPathProbe_Proxy( 
    IAppDomainSetup * This,
    /* [retval][out] */ BSTR *pRetVal);


void __RPC_STUB IAppDomainSetup_get_PrivateBinPathProbe_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


/* [propput] */ HRESULT __stdcall IAppDomainSetup_put_PrivateBinPathProbe_Proxy( 
    IAppDomainSetup * This,
    /* [in] */ BSTR pRetVal);


void __RPC_STUB IAppDomainSetup_put_PrivateBinPathProbe_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


/* [propget] */ HRESULT __stdcall IAppDomainSetup_get_ShadowCopyDirectories_Proxy( 
    IAppDomainSetup * This,
    /* [retval][out] */ BSTR *pRetVal);


void __RPC_STUB IAppDomainSetup_get_ShadowCopyDirectories_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


/* [propput] */ HRESULT __stdcall IAppDomainSetup_put_ShadowCopyDirectories_Proxy( 
    IAppDomainSetup * This,
    /* [in] */ BSTR pRetVal);


void __RPC_STUB IAppDomainSetup_put_ShadowCopyDirectories_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


/* [propget] */ HRESULT __stdcall IAppDomainSetup_get_ShadowCopyFiles_Proxy( 
    IAppDomainSetup * This,
    /* [retval][out] */ BSTR *pRetVal);


void __RPC_STUB IAppDomainSetup_get_ShadowCopyFiles_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


/* [propput] */ HRESULT __stdcall IAppDomainSetup_put_ShadowCopyFiles_Proxy( 
    IAppDomainSetup * This,
    /* [in] */ BSTR pRetVal);


void __RPC_STUB IAppDomainSetup_put_ShadowCopyFiles_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);



#endif 	/* __IAppDomainSetup_INTERFACE_DEFINED__ */


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


