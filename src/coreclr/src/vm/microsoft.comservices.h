// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#pragma warning( disable: 4049 )  /* more than 64k source lines */

/* this ALWAYS GENERATED file contains the definitions for the interfaces */


 /* File created by MIDL compiler version 5.03.0280 */
/* at Mon Jul 17 19:19:10 2000
 */
/* Compiler settings for Z:\urt\inst\v1.x86chk\Microsoft.ComServices.idl:
    Os (OptLev=s), W1, Zp8, env=Win32 (32b run), ms_ext, c_ext
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

#ifndef __Microsoft2EComServices_h__
#define __Microsoft2EComServices_h__

/* Forward Declarations */ 

#ifndef __IRegistrationHelper_FWD_DEFINED__
#define __IRegistrationHelper_FWD_DEFINED__
typedef interface IRegistrationHelper IRegistrationHelper;
#endif 	/* __IRegistrationHelper_FWD_DEFINED__ */


#ifndef __RegistrationHelperTx_FWD_DEFINED__
#define __RegistrationHelperTx_FWD_DEFINED__

#ifdef __cplusplus
typedef class RegistrationHelperTx RegistrationHelperTx;
#else
typedef struct RegistrationHelperTx RegistrationHelperTx;
#endif /* __cplusplus */

#endif 	/* __RegistrationHelperTx_FWD_DEFINED__ */


#ifdef __cplusplus
extern "C"{
#endif 

void __RPC_FAR * __RPC_USER MIDL_user_allocate(size_t);
void __RPC_USER MIDL_user_free( void __RPC_FAR * ); 


#ifndef __Microsoft_ComServices_LIBRARY_DEFINED__
#define __Microsoft_ComServices_LIBRARY_DEFINED__

/* library Microsoft_ComServices */
/* [version][uuid] */ 


typedef /* [public][public][uuid] */  DECLSPEC_UUID("9D667CBC-FE79-3B45-AEBB-6303106B137A") 
enum __MIDL___MIDL_itf_Microsoft2EComServices_0000_0001
    {	InstallationFlags_Default	= 0,
	InstallationFlags_ExpectExistingTypeLib	= 1,
	InstallationFlags_CreateTargetApplication	= 2,
	InstallationFlags_FindOrCreateTargetApplication	= 4,
	InstallationFlags_ReconfigureExistingApplication	= 8,
	InstallationFlags_Register	= 256,
	InstallationFlags_Install	= 512,
	InstallationFlags_Configure	= 1024
    }	InstallationFlags;


EXTERN_C const IID LIBID_Microsoft_ComServices;

#ifndef __IRegistrationHelper_INTERFACE_DEFINED__
#define __IRegistrationHelper_INTERFACE_DEFINED__

/* interface IRegistrationHelper */
/* [object][custom][oleautomation][uuid] */ 


EXTERN_C const IID IID_IRegistrationHelper;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("55E3EA25-55CB-4650-8887-18E8D30BB4BC")
    IRegistrationHelper : public IUnknown
    {
    public:
        virtual HRESULT __stdcall InstallAssembly( 
            /* [in] */ BSTR assembly,
            /* [out][in] */ BSTR __RPC_FAR *application,
            /* [out][in] */ BSTR __RPC_FAR *tlb,
            /* [in] */ InstallationFlags installFlags) = 0;
        
        virtual HRESULT __stdcall RegisterAssembly( 
            /* [in] */ BSTR assembly,
            /* [out][in] */ BSTR __RPC_FAR *tlb) = 0;
        
        virtual HRESULT __stdcall ConfigureAssembly( 
            /* [in] */ BSTR assembly,
            /* [in] */ BSTR application) = 0;
        
        virtual HRESULT __stdcall UninstallAssembly( 
            /* [in] */ BSTR assembly,
            /* [in] */ BSTR application) = 0;
        
    };
    
#else 	/* C style interface */

    typedef struct IRegistrationHelperVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE __RPC_FAR *QueryInterface )( 
            IRegistrationHelper __RPC_FAR * This,
            /* [in] */ REFIID riid,
            /* [iid_is][out] */ void __RPC_FAR *__RPC_FAR *ppvObject);
        
        ULONG ( STDMETHODCALLTYPE __RPC_FAR *AddRef )( 
            IRegistrationHelper __RPC_FAR * This);
        
        ULONG ( STDMETHODCALLTYPE __RPC_FAR *Release )( 
            IRegistrationHelper __RPC_FAR * This);
        
        HRESULT ( __stdcall __RPC_FAR *InstallAssembly )( 
            IRegistrationHelper __RPC_FAR * This,
            /* [in] */ BSTR assembly,
            /* [out][in] */ BSTR __RPC_FAR *application,
            /* [out][in] */ BSTR __RPC_FAR *tlb,
            /* [in] */ InstallationFlags installFlags);
        
        HRESULT ( __stdcall __RPC_FAR *RegisterAssembly )( 
            IRegistrationHelper __RPC_FAR * This,
            /* [in] */ BSTR assembly,
            /* [out][in] */ BSTR __RPC_FAR *tlb);
        
        HRESULT ( __stdcall __RPC_FAR *ConfigureAssembly )( 
            IRegistrationHelper __RPC_FAR * This,
            /* [in] */ BSTR assembly,
            /* [in] */ BSTR application);
        
        HRESULT ( __stdcall __RPC_FAR *UninstallAssembly )( 
            IRegistrationHelper __RPC_FAR * This,
            /* [in] */ BSTR assembly,
            /* [in] */ BSTR application);
        
        END_INTERFACE
    } IRegistrationHelperVtbl;

    interface IRegistrationHelper
    {
        CONST_VTBL struct IRegistrationHelperVtbl __RPC_FAR *lpVtbl;
    };

    

#ifdef COBJMACROS


#define IRegistrationHelper_QueryInterface(This,riid,ppvObject)	\
    (This)->lpVtbl -> QueryInterface(This,riid,ppvObject)

#define IRegistrationHelper_AddRef(This)	\
    (This)->lpVtbl -> AddRef(This)

#define IRegistrationHelper_Release(This)	\
    (This)->lpVtbl -> Release(This)


#define IRegistrationHelper_InstallAssembly(This,assembly,application,tlb,installFlags)	\
    (This)->lpVtbl -> InstallAssembly(This,assembly,application,tlb,installFlags)

#define IRegistrationHelper_RegisterAssembly(This,assembly,tlb)	\
    (This)->lpVtbl -> RegisterAssembly(This,assembly,tlb)

#define IRegistrationHelper_ConfigureAssembly(This,assembly,application)	\
    (This)->lpVtbl -> ConfigureAssembly(This,assembly,application)

#define IRegistrationHelper_UninstallAssembly(This,assembly,application)	\
    (This)->lpVtbl -> UninstallAssembly(This,assembly,application)

#endif /* COBJMACROS */


#endif 	/* C style interface */



HRESULT __stdcall IRegistrationHelper_InstallAssembly_Proxy( 
    IRegistrationHelper __RPC_FAR * This,
    /* [in] */ BSTR assembly,
    /* [out][in] */ BSTR __RPC_FAR *application,
    /* [out][in] */ BSTR __RPC_FAR *tlb,
    /* [in] */ InstallationFlags installFlags);


void __RPC_STUB IRegistrationHelper_InstallAssembly_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


HRESULT __stdcall IRegistrationHelper_RegisterAssembly_Proxy( 
    IRegistrationHelper __RPC_FAR * This,
    /* [in] */ BSTR assembly,
    /* [out][in] */ BSTR __RPC_FAR *tlb);


void __RPC_STUB IRegistrationHelper_RegisterAssembly_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


HRESULT __stdcall IRegistrationHelper_ConfigureAssembly_Proxy( 
    IRegistrationHelper __RPC_FAR * This,
    /* [in] */ BSTR assembly,
    /* [in] */ BSTR application);


void __RPC_STUB IRegistrationHelper_ConfigureAssembly_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


HRESULT __stdcall IRegistrationHelper_UninstallAssembly_Proxy( 
    IRegistrationHelper __RPC_FAR * This,
    /* [in] */ BSTR assembly,
    /* [in] */ BSTR application);


void __RPC_STUB IRegistrationHelper_UninstallAssembly_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);



#endif 	/* __IRegistrationHelper_INTERFACE_DEFINED__ */


EXTERN_C const CLSID CLSID_RegistrationHelperTx;

#ifdef __cplusplus

class DECLSPEC_UUID("89A86E7B-C229-4008-9BAA-2F5C8411D7E0")
RegistrationHelperTx;
#endif
#endif /* __Microsoft_ComServices_LIBRARY_DEFINED__ */

/* Additional Prototypes for ALL interfaces */

/* end of Additional Prototypes */

#ifdef __cplusplus
}
#endif

#endif


