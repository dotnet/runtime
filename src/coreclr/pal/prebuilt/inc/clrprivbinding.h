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


#ifndef __ICLRPrivAssembly_FWD_DEFINED__
#define __ICLRPrivAssembly_FWD_DEFINED__
typedef interface ICLRPrivAssembly ICLRPrivAssembly;

#endif 	/* __ICLRPrivAssembly_FWD_DEFINED__ */


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
    
    MIDL_INTERFACE("2601F621-E462-404C-B299-3E1DE72F8542")
    ICLRPrivBinder : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE BindAssemblyByName( 
            /* [in] */ struct AssemblyNameData *pAssemblyNameData,
            /* [retval][out] */ ICLRPrivAssembly **ppAssembly) = 0;
        
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
            /* [retval][out] */ ICLRPrivAssembly **ppAssembly);
        
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


enum ASSEMBLY_IMAGE_TYPES
    {
        ASSEMBLY_IMAGE_TYPE_IL	= 0x1,
        ASSEMBLY_IMAGE_TYPE_NATIVE	= 0x2,
        ASSEMBLY_IMAGE_TYPE_DEFAULT	= 0x3,
        ASSEMBLY_IMAGE_TYPE_ASSEMBLY	= 0x4
    } ;


extern RPC_IF_HANDLE __MIDL_itf_clrprivbinding_0000_0001_v0_0_c_ifspec;
extern RPC_IF_HANDLE __MIDL_itf_clrprivbinding_0000_0001_v0_0_s_ifspec;

#ifndef __ICLRPrivAssembly_INTERFACE_DEFINED__
#define __ICLRPrivAssembly_INTERFACE_DEFINED__

/* interface ICLRPrivAssembly */
/* [object][local][version][uuid] */ 


EXTERN_C const IID IID_ICLRPrivAssembly;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("2601F621-E462-404C-B299-3E1DE72F8543")
    ICLRPrivAssembly : public ICLRPrivBinder
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE GetAvailableImageTypes( 
            /* [retval][out] */ LPDWORD pdwImageTypes) = 0;
        
    };
    
    
#else 	/* C style interface */

    typedef struct ICLRPrivAssemblyVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            ICLRPrivAssembly * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            ICLRPrivAssembly * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            ICLRPrivAssembly * This);
        
        HRESULT ( STDMETHODCALLTYPE *BindAssemblyByName )( 
            ICLRPrivAssembly * This,
            /* [in] */ struct AssemblyNameData *pAssemblyNameData,
            /* [retval][out] */ ICLRPrivAssembly **ppAssembly);
        
        HRESULT ( STDMETHODCALLTYPE *GetBinderID )( 
            ICLRPrivAssembly * This,
            /* [retval][out] */ UINT_PTR *pBinderId);
        
        HRESULT ( STDMETHODCALLTYPE *GetLoaderAllocator )( 
            ICLRPrivAssembly * This,
            /* [retval][out] */ LPVOID *pLoaderAllocator);
        
        HRESULT ( STDMETHODCALLTYPE *GetAvailableImageTypes )( 
            ICLRPrivAssembly * This,
            /* [retval][out] */ LPDWORD pdwImageTypes);
        
        END_INTERFACE
    } ICLRPrivAssemblyVtbl;

    interface ICLRPrivAssembly
    {
        CONST_VTBL struct ICLRPrivAssemblyVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define ICLRPrivAssembly_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define ICLRPrivAssembly_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define ICLRPrivAssembly_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define ICLRPrivAssembly_BindAssemblyByName(This,pAssemblyNameData,ppAssembly)	\
    ( (This)->lpVtbl -> BindAssemblyByName(This,pAssemblyNameData,ppAssembly) ) 

#define ICLRPrivAssembly_GetBinderID(This,pBinderId)	\
    ( (This)->lpVtbl -> GetBinderID(This,pBinderId) ) 

#define ICLRPrivAssembly_GetLoaderAllocator(This,pLoaderAllocator)	\
    ( (This)->lpVtbl -> GetLoaderAllocator(This,pLoaderAllocator) ) 


#define ICLRPrivAssembly_GetAvailableImageTypes(This,pdwImageTypes)	\
    ( (This)->lpVtbl -> GetAvailableImageTypes(This,pdwImageTypes) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ICLRPrivAssembly_INTERFACE_DEFINED__ */


/* Additional Prototypes for ALL interfaces */

/* end of Additional Prototypes */

#ifdef __cplusplus
}
#endif

#endif


