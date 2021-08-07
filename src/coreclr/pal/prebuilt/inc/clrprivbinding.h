// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.



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

#ifndef __clrprivbinding_h__
#define __clrprivbinding_h__

#if defined(_MSC_VER) && (_MSC_VER >= 1020)
#pragma once
#endif

/* Forward Declarations */ 

#ifndef __ICLRPrivBinder_FWD_DEFINED__
#define __ICLRPrivBinder_FWD_DEFINED__
typedef interface ICLRPrivBinder ICLRPrivBinder;

#endif 	/* __ICLRPrivBinder_FWD_DEFINED__ */

/* header files for imported files */
#include "unknwn.h"
#include "objidl.h"

#ifdef __cplusplus
extern "C"{
#endif 


/* interface __MIDL_itf_clrprivbinding_0000_0000 */
/* [local] */ 





extern RPC_IF_HANDLE __MIDL_itf_clrprivbinding_0000_0000_v0_0_c_ifspec;
extern RPC_IF_HANDLE __MIDL_itf_clrprivbinding_0000_0000_v0_0_s_ifspec;

#ifndef __ICLRPrivBinder_INTERFACE_DEFINED__
#define __ICLRPrivBinder_INTERFACE_DEFINED__

/* interface ICLRPrivBinder */
/* [object][local][version][uuid] */ 


EXTERN_C const IID IID_ICLRPrivBinder;

#if defined(__cplusplus) && !defined(CINTERFACE)

namespace BINDER_SPACE
{
    class Assembly;
};

    MIDL_INTERFACE("2601F621-E462-404C-B299-3E1DE72F8542")
    ICLRPrivBinder : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE BindAssemblyByName( 
            /* [in] */ struct AssemblyNameData *pAssemblyNameData,
            /* [retval][out] */ BINDER_SPACE::Assembly **ppAssembly) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetBinderID( 
            /* [retval][out] */ UINT_PTR *pBinderId) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetLoaderAllocator( 
            /* [retval][out] */ LPVOID *pLoaderAllocator) = 0;
        
    };
    
    
#else 	/* C style interface */

    typedef struct ICLRPrivBinderVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            ICLRPrivBinder * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            ICLRPrivBinder * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            ICLRPrivBinder * This);
        
        HRESULT ( STDMETHODCALLTYPE *BindAssemblyByName )( 
            ICLRPrivBinder * This,
            /* [in] */ struct AssemblyNameData *pAssemblyNameData,
            /* [retval][out] */ BINDER_SPACE::Assembly **ppAssembly);
        
        HRESULT ( STDMETHODCALLTYPE *GetBinderID )( 
            ICLRPrivBinder * This,
            /* [retval][out] */ UINT_PTR *pBinderId);
        
        HRESULT ( STDMETHODCALLTYPE *GetLoaderAllocator )( 
            ICLRPrivBinder * This,
            /* [retval][out] */ LPVOID *pLoaderAllocator);
        
        END_INTERFACE
    } ICLRPrivBinderVtbl;

    interface ICLRPrivBinder
    {
        CONST_VTBL struct ICLRPrivBinderVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define ICLRPrivBinder_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define ICLRPrivBinder_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define ICLRPrivBinder_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define ICLRPrivBinder_BindAssemblyByName(This,pAssemblyNameData,ppAssembly)	\
    ( (This)->lpVtbl -> BindAssemblyByName(This,pAssemblyNameData,ppAssembly) ) 

#define ICLRPrivBinder_GetBinderID(This,pBinderId)	\
    ( (This)->lpVtbl -> GetBinderID(This,pBinderId) ) 

#define ICLRPrivBinder_GetLoaderAllocator(This,pLoaderAllocator)	\
    ( (This)->lpVtbl -> GetLoaderAllocator(This,pLoaderAllocator) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ICLRPrivBinder_INTERFACE_DEFINED__ */


/* interface __MIDL_itf_clrprivbinding_0000_0001 */
/* [local] */ 


/* Additional Prototypes for ALL interfaces */

/* end of Additional Prototypes */

#ifdef __cplusplus
}
#endif

#endif


