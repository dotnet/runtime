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

#ifndef __CLRPrivBinding_h__
#define __CLRPrivBinding_h__

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


#ifndef __ICLRPrivResource_FWD_DEFINED__
#define __ICLRPrivResource_FWD_DEFINED__
typedef interface ICLRPrivResource ICLRPrivResource;

#endif 	/* __ICLRPrivResource_FWD_DEFINED__ */


#ifndef __ICLRPrivResourcePath_FWD_DEFINED__
#define __ICLRPrivResourcePath_FWD_DEFINED__
typedef interface ICLRPrivResourcePath ICLRPrivResourcePath;

#endif 	/* __ICLRPrivResourcePath_FWD_DEFINED__ */


#ifndef __ICLRPrivResourceStream_FWD_DEFINED__
#define __ICLRPrivResourceStream_FWD_DEFINED__
typedef interface ICLRPrivResourceStream ICLRPrivResourceStream;

#endif 	/* __ICLRPrivResourceStream_FWD_DEFINED__ */


#ifndef __ICLRPrivResourceHMODULE_FWD_DEFINED__
#define __ICLRPrivResourceHMODULE_FWD_DEFINED__
typedef interface ICLRPrivResourceHMODULE ICLRPrivResourceHMODULE;

#endif 	/* __ICLRPrivResourceHMODULE_FWD_DEFINED__ */


#ifndef __ICLRPrivResourceAssembly_FWD_DEFINED__
#define __ICLRPrivResourceAssembly_FWD_DEFINED__
typedef interface ICLRPrivResourceAssembly ICLRPrivResourceAssembly;

#endif 	/* __ICLRPrivResourceAssembly_FWD_DEFINED__ */


#ifndef __ICLRPrivAssemblyInfo_FWD_DEFINED__
#define __ICLRPrivAssemblyInfo_FWD_DEFINED__
typedef interface ICLRPrivAssemblyInfo ICLRPrivAssemblyInfo;

#endif 	/* __ICLRPrivAssemblyInfo_FWD_DEFINED__ */


#ifndef __ICLRPrivAssemblyID_WinRT_FWD_DEFINED__
#define __ICLRPrivAssemblyID_WinRT_FWD_DEFINED__
typedef interface ICLRPrivAssemblyID_WinRT ICLRPrivAssemblyID_WinRT;

#endif 	/* __ICLRPrivAssemblyID_WinRT_FWD_DEFINED__ */


#ifndef __ICLRPrivWinRtTypeBinder_FWD_DEFINED__
#define __ICLRPrivWinRtTypeBinder_FWD_DEFINED__
typedef interface ICLRPrivWinRtTypeBinder ICLRPrivWinRtTypeBinder;

#endif 	/* __ICLRPrivWinRtTypeBinder_FWD_DEFINED__ */


/* header files for imported files */
#include "unknwn.h"
#include "objidl.h"
#include "fusion.h"

#ifdef __cplusplus
extern "C"{
#endif 


/* interface __MIDL_itf_CLRPrivBinding_0000_0000 */
/* [local] */ 








typedef LPCSTR LPCUTF8;



extern RPC_IF_HANDLE __MIDL_itf_CLRPrivBinding_0000_0000_v0_0_c_ifspec;
extern RPC_IF_HANDLE __MIDL_itf_CLRPrivBinding_0000_0000_v0_0_s_ifspec;

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
            /* [in] */ IAssemblyName *pAssemblyName,
            /* [retval][out] */ ICLRPrivAssembly **ppAssembly) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetBinderID( 
            /* [retval][out] */ UINT_PTR *pBinderId) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetLoaderAllocator(
            /* [retval][out] */ LPVOID* pLoaderAllocator) = 0;
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
            /* [in] */ IAssemblyName *pAssemblyName,
            /* [retval][out] */ ICLRPrivAssembly **ppAssembly);
        
        HRESULT ( STDMETHODCALLTYPE *GetBinderID )( 
            ICLRPrivBinder * This,
            /* [retval][out] */ UINT_PTR *pBinderId);
        
        HRESULT(STDMETHODCALLTYPE *GetLoaderAllocator)(
            ICLRPrivBinder * This,
            /* [retval][out] */ LPVOID *pLoaderAllocator) = 0;

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


#define ICLRPrivBinder_BindAssemblyByName(This,pAssemblyName,ppAssembly)	\
    ( (This)->lpVtbl -> BindAssemblyByName(This,pAssemblyName,ppAssembly) ) 

#define ICLRPrivBinder_GetBinderID(This,pBinderId)	\
    ( (This)->lpVtbl -> GetBinderID(This,pBinderId) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ICLRPrivBinder_INTERFACE_DEFINED__ */


/* interface __MIDL_itf_CLRPrivBinding_0000_0001 */
/* [local] */ 


enum ASSEMBLY_IMAGE_TYPES
    {
        ASSEMBLY_IMAGE_TYPE_IL	= 0x1,
        ASSEMBLY_IMAGE_TYPE_NATIVE	= 0x2,
        ASSEMBLY_IMAGE_TYPE_DEFAULT	= 0x3,
        ASSEMBLY_IMAGE_TYPE_ASSEMBLY	= 0x4
    } ;


extern RPC_IF_HANDLE __MIDL_itf_CLRPrivBinding_0000_0001_v0_0_c_ifspec;
extern RPC_IF_HANDLE __MIDL_itf_CLRPrivBinding_0000_0001_v0_0_s_ifspec;

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
        virtual HRESULT STDMETHODCALLTYPE IsShareable( 
            /* [retval][out] */ BOOL *pbIsShareable) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetAvailableImageTypes( 
            /* [retval][out] */ LPDWORD pdwImageTypes) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetImageResource( 
            /* [in] */ DWORD dwImageType,
            /* [out] */ DWORD *pdwImageType,
            /* [retval][out] */ ICLRPrivResource **ppIResource) = 0;
        
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
            /* [in] */ IAssemblyName *pAssemblyName,
            /* [retval][out] */ ICLRPrivAssembly **ppAssembly);
        
        HRESULT ( STDMETHODCALLTYPE *GetBinderID )( 
            ICLRPrivAssembly * This,
            /* [retval][out] */ UINT_PTR *pBinderId);
        
        HRESULT ( STDMETHODCALLTYPE *IsShareable )( 
            ICLRPrivAssembly * This,
            /* [retval][out] */ BOOL *pbIsShareable);
        
        HRESULT ( STDMETHODCALLTYPE *GetAvailableImageTypes )( 
            ICLRPrivAssembly * This,
            /* [retval][out] */ LPDWORD pdwImageTypes);
        
        HRESULT ( STDMETHODCALLTYPE *GetImageResource )( 
            ICLRPrivAssembly * This,
            /* [in] */ DWORD dwImageType,
            /* [out] */ DWORD *pdwImageType,
            /* [retval][out] */ ICLRPrivResource **ppIResource);
        
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


#define ICLRPrivAssembly_BindAssemblyByName(This,pAssemblyName,ppAssembly)	\
    ( (This)->lpVtbl -> BindAssemblyByName(This,pAssemblyName,ppAssembly) ) 

#define ICLRPrivAssembly_GetBinderID(This,pBinderId)	\
    ( (This)->lpVtbl -> GetBinderID(This,pBinderId) ) 


#define ICLRPrivAssembly_IsShareable(This,pbIsShareable)	\
    ( (This)->lpVtbl -> IsShareable(This,pbIsShareable) ) 

#define ICLRPrivAssembly_GetAvailableImageTypes(This,pdwImageTypes)	\
    ( (This)->lpVtbl -> GetAvailableImageTypes(This,pdwImageTypes) ) 

#define ICLRPrivAssembly_GetImageResource(This,dwImageType,pdwImageType,ppIResource)	\
    ( (This)->lpVtbl -> GetImageResource(This,dwImageType,pdwImageType,ppIResource) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ICLRPrivAssembly_INTERFACE_DEFINED__ */


#ifndef __ICLRPrivResource_INTERFACE_DEFINED__
#define __ICLRPrivResource_INTERFACE_DEFINED__

/* interface ICLRPrivResource */
/* [object][local][version][uuid] */ 


EXTERN_C const IID IID_ICLRPrivResource;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("2601F621-E462-404C-B299-3E1DE72F8547")
    ICLRPrivResource : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE GetResourceType( 
            /* [retval][out] */ IID *pIID) = 0;
        
    };
    
    
#else 	/* C style interface */

    typedef struct ICLRPrivResourceVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            ICLRPrivResource * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            ICLRPrivResource * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            ICLRPrivResource * This);
        
        HRESULT ( STDMETHODCALLTYPE *GetResourceType )( 
            ICLRPrivResource * This,
            /* [retval][out] */ IID *pIID);
        
        END_INTERFACE
    } ICLRPrivResourceVtbl;

    interface ICLRPrivResource
    {
        CONST_VTBL struct ICLRPrivResourceVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define ICLRPrivResource_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define ICLRPrivResource_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define ICLRPrivResource_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define ICLRPrivResource_GetResourceType(This,pIID)	\
    ( (This)->lpVtbl -> GetResourceType(This,pIID) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ICLRPrivResource_INTERFACE_DEFINED__ */


#ifndef __ICLRPrivResourcePath_INTERFACE_DEFINED__
#define __ICLRPrivResourcePath_INTERFACE_DEFINED__

/* interface ICLRPrivResourcePath */
/* [object][local][version][uuid] */ 


EXTERN_C const IID IID_ICLRPrivResourcePath;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("2601F621-E462-404C-B299-3E1DE72F8544")
    ICLRPrivResourcePath : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE GetPath( 
            /* [in] */ DWORD cchBuffer,
            /* [out] */ LPDWORD pcchBuffer,
            /* [optional][string][length_is][size_is][out] */ LPWSTR wzBuffer) = 0;
        
    };
    
    
#else 	/* C style interface */

    typedef struct ICLRPrivResourcePathVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            ICLRPrivResourcePath * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            ICLRPrivResourcePath * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            ICLRPrivResourcePath * This);
        
        HRESULT ( STDMETHODCALLTYPE *GetPath )( 
            ICLRPrivResourcePath * This,
            /* [in] */ DWORD cchBuffer,
            /* [out] */ LPDWORD pcchBuffer,
            /* [optional][string][length_is][size_is][out] */ LPWSTR wzBuffer);
        
        END_INTERFACE
    } ICLRPrivResourcePathVtbl;

    interface ICLRPrivResourcePath
    {
        CONST_VTBL struct ICLRPrivResourcePathVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define ICLRPrivResourcePath_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define ICLRPrivResourcePath_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define ICLRPrivResourcePath_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define ICLRPrivResourcePath_GetPath(This,cchBuffer,pcchBuffer,wzBuffer)	\
    ( (This)->lpVtbl -> GetPath(This,cchBuffer,pcchBuffer,wzBuffer) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ICLRPrivResourcePath_INTERFACE_DEFINED__ */


#ifndef __ICLRPrivResourceStream_INTERFACE_DEFINED__
#define __ICLRPrivResourceStream_INTERFACE_DEFINED__

/* interface ICLRPrivResourceStream */
/* [object][local][version][uuid] */ 


EXTERN_C const IID IID_ICLRPrivResourceStream;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("2601F621-E462-404C-B299-3E1DE72F8545")
    ICLRPrivResourceStream : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE GetStream( 
            /* [in] */ REFIID riid,
            /* [retval][iid_is][out] */ LPVOID *ppvStream) = 0;
        
    };
    
    
#else 	/* C style interface */

    typedef struct ICLRPrivResourceStreamVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            ICLRPrivResourceStream * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            ICLRPrivResourceStream * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            ICLRPrivResourceStream * This);
        
        HRESULT ( STDMETHODCALLTYPE *GetStream )( 
            ICLRPrivResourceStream * This,
            /* [in] */ REFIID riid,
            /* [retval][iid_is][out] */ LPVOID *ppvStream);
        
        END_INTERFACE
    } ICLRPrivResourceStreamVtbl;

    interface ICLRPrivResourceStream
    {
        CONST_VTBL struct ICLRPrivResourceStreamVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define ICLRPrivResourceStream_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define ICLRPrivResourceStream_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define ICLRPrivResourceStream_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define ICLRPrivResourceStream_GetStream(This,riid,ppvStream)	\
    ( (This)->lpVtbl -> GetStream(This,riid,ppvStream) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ICLRPrivResourceStream_INTERFACE_DEFINED__ */


#ifndef __ICLRPrivResourceHMODULE_INTERFACE_DEFINED__
#define __ICLRPrivResourceHMODULE_INTERFACE_DEFINED__

/* interface ICLRPrivResourceHMODULE */
/* [object][local][version][uuid] */ 


EXTERN_C const IID IID_ICLRPrivResourceHMODULE;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("2601F621-E462-404C-B299-3E1DE72F8546")
    ICLRPrivResourceHMODULE : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE GetHMODULE( 
            /* [retval][out] */ HMODULE *phModule) = 0;
        
    };
    
    
#else 	/* C style interface */

    typedef struct ICLRPrivResourceHMODULEVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            ICLRPrivResourceHMODULE * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            ICLRPrivResourceHMODULE * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            ICLRPrivResourceHMODULE * This);
        
        HRESULT ( STDMETHODCALLTYPE *GetHMODULE )( 
            ICLRPrivResourceHMODULE * This,
            /* [retval][out] */ HMODULE *phModule);
        
        END_INTERFACE
    } ICLRPrivResourceHMODULEVtbl;

    interface ICLRPrivResourceHMODULE
    {
        CONST_VTBL struct ICLRPrivResourceHMODULEVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define ICLRPrivResourceHMODULE_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define ICLRPrivResourceHMODULE_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define ICLRPrivResourceHMODULE_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define ICLRPrivResourceHMODULE_GetHMODULE(This,phModule)	\
    ( (This)->lpVtbl -> GetHMODULE(This,phModule) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ICLRPrivResourceHMODULE_INTERFACE_DEFINED__ */


#ifndef __ICLRPrivResourceAssembly_INTERFACE_DEFINED__
#define __ICLRPrivResourceAssembly_INTERFACE_DEFINED__

/* interface ICLRPrivResourceAssembly */
/* [object][local][version][uuid] */ 


EXTERN_C const IID IID_ICLRPrivResourceAssembly;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("8d2d3cc9-1249-4ad4-977d-b772bd4e8a94")
    ICLRPrivResourceAssembly : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE GetAssembly( 
            /* [retval][out] */ LPVOID *pAssembly) = 0;
        
    };
    
    
#else 	/* C style interface */

    typedef struct ICLRPrivResourceAssemblyVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            ICLRPrivResourceAssembly * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            ICLRPrivResourceAssembly * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            ICLRPrivResourceAssembly * This);
        
        HRESULT ( STDMETHODCALLTYPE *GetAssembly )( 
            ICLRPrivResourceAssembly * This,
            /* [retval][out] */ LPVOID *pAssembly);
        
        END_INTERFACE
    } ICLRPrivResourceAssemblyVtbl;

    interface ICLRPrivResourceAssembly
    {
        CONST_VTBL struct ICLRPrivResourceAssemblyVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define ICLRPrivResourceAssembly_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define ICLRPrivResourceAssembly_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define ICLRPrivResourceAssembly_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define ICLRPrivResourceAssembly_GetAssembly(This,pAssembly)	\
    ( (This)->lpVtbl -> GetAssembly(This,pAssembly) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ICLRPrivResourceAssembly_INTERFACE_DEFINED__ */


#ifndef __ICLRPrivAssemblyInfo_INTERFACE_DEFINED__
#define __ICLRPrivAssemblyInfo_INTERFACE_DEFINED__

/* interface ICLRPrivAssemblyInfo */
/* [object][local][version][uuid] */ 


EXTERN_C const IID IID_ICLRPrivAssemblyInfo;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("5653946E-800B-48B7-8B09-B1B879B54F68")
    ICLRPrivAssemblyInfo : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE GetAssemblyName( 
            /* [in] */ DWORD cchBuffer,
            /* [out] */ LPDWORD pcchBuffer,
            /* [optional][string][out] */ LPWSTR wzBuffer) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetAssemblyVersion( 
            /* [out] */ USHORT *pMajor,
            /* [out] */ USHORT *pMinor,
            /* [out] */ USHORT *pBuild,
            /* [out] */ USHORT *pRevision) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetAssemblyPublicKey( 
            /* [in] */ DWORD cbBuffer,
            /* [out] */ LPDWORD pcbBuffer,
            /* [optional][length_is][size_is][out] */ BYTE *pbBuffer) = 0;
        
    };
    
    
#else 	/* C style interface */

    typedef struct ICLRPrivAssemblyInfoVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            ICLRPrivAssemblyInfo * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            ICLRPrivAssemblyInfo * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            ICLRPrivAssemblyInfo * This);
        
        HRESULT ( STDMETHODCALLTYPE *GetAssemblyName )( 
            ICLRPrivAssemblyInfo * This,
            /* [in] */ DWORD cchBuffer,
            /* [out] */ LPDWORD pcchBuffer,
            /* [optional][string][out] */ LPWSTR wzBuffer);
        
        HRESULT ( STDMETHODCALLTYPE *GetAssemblyVersion )( 
            ICLRPrivAssemblyInfo * This,
            /* [out] */ USHORT *pMajor,
            /* [out] */ USHORT *pMinor,
            /* [out] */ USHORT *pBuild,
            /* [out] */ USHORT *pRevision);
        
        HRESULT ( STDMETHODCALLTYPE *GetAssemblyPublicKey )( 
            ICLRPrivAssemblyInfo * This,
            /* [in] */ DWORD cbBuffer,
            /* [out] */ LPDWORD pcbBuffer,
            /* [optional][length_is][size_is][out] */ BYTE *pbBuffer);
        
        END_INTERFACE
    } ICLRPrivAssemblyInfoVtbl;

    interface ICLRPrivAssemblyInfo
    {
        CONST_VTBL struct ICLRPrivAssemblyInfoVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define ICLRPrivAssemblyInfo_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define ICLRPrivAssemblyInfo_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define ICLRPrivAssemblyInfo_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define ICLRPrivAssemblyInfo_GetAssemblyName(This,cchBuffer,pcchBuffer,wzBuffer)	\
    ( (This)->lpVtbl -> GetAssemblyName(This,cchBuffer,pcchBuffer,wzBuffer) ) 

#define ICLRPrivAssemblyInfo_GetAssemblyVersion(This,pMajor,pMinor,pBuild,pRevision)	\
    ( (This)->lpVtbl -> GetAssemblyVersion(This,pMajor,pMinor,pBuild,pRevision) ) 

#define ICLRPrivAssemblyInfo_GetAssemblyPublicKey(This,cbBuffer,pcbBuffer,pbBuffer)	\
    ( (This)->lpVtbl -> GetAssemblyPublicKey(This,cbBuffer,pcbBuffer,pbBuffer) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ICLRPrivAssemblyInfo_INTERFACE_DEFINED__ */


#ifndef __ICLRPrivAssemblyID_WinRT_INTERFACE_DEFINED__
#define __ICLRPrivAssemblyID_WinRT_INTERFACE_DEFINED__

/* interface ICLRPrivAssemblyID_WinRT */
/* [object][local][version][uuid] */ 


EXTERN_C const IID IID_ICLRPrivAssemblyID_WinRT;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("4372D277-9906-4FED-BF53-30C0B4010896")
    ICLRPrivAssemblyID_WinRT : public IUnknown
    {
    public:
    };
    
    
#else 	/* C style interface */

    typedef struct ICLRPrivAssemblyID_WinRTVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            ICLRPrivAssemblyID_WinRT * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            ICLRPrivAssemblyID_WinRT * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            ICLRPrivAssemblyID_WinRT * This);
        
        END_INTERFACE
    } ICLRPrivAssemblyID_WinRTVtbl;

    interface ICLRPrivAssemblyID_WinRT
    {
        CONST_VTBL struct ICLRPrivAssemblyID_WinRTVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define ICLRPrivAssemblyID_WinRT_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define ICLRPrivAssemblyID_WinRT_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define ICLRPrivAssemblyID_WinRT_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ICLRPrivAssemblyID_WinRT_INTERFACE_DEFINED__ */


#ifndef __ICLRPrivWinRtTypeBinder_INTERFACE_DEFINED__
#define __ICLRPrivWinRtTypeBinder_INTERFACE_DEFINED__

/* interface ICLRPrivWinRtTypeBinder */
/* [object][local][version][uuid] */ 


EXTERN_C const IID IID_ICLRPrivWinRtTypeBinder;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("6DE2A085-EFF4-4078-9F60-B9D366736398")
    ICLRPrivWinRtTypeBinder : public IUnknown
    {
    public:
        virtual void *STDMETHODCALLTYPE FindAssemblyForWinRtTypeIfLoaded( 
            void *pAppDomain,
            LPCUTF8 szNamespace,
            LPCUTF8 szClassName) = 0;
        
    };
    
    
#else 	/* C style interface */

    typedef struct ICLRPrivWinRtTypeBinderVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            ICLRPrivWinRtTypeBinder * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            ICLRPrivWinRtTypeBinder * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            ICLRPrivWinRtTypeBinder * This);
        
        void *( STDMETHODCALLTYPE *FindAssemblyForWinRtTypeIfLoaded )( 
            ICLRPrivWinRtTypeBinder * This,
            void *pAppDomain,
            LPCUTF8 szNamespace,
            LPCUTF8 szClassName);
        
        END_INTERFACE
    } ICLRPrivWinRtTypeBinderVtbl;

    interface ICLRPrivWinRtTypeBinder
    {
        CONST_VTBL struct ICLRPrivWinRtTypeBinderVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define ICLRPrivWinRtTypeBinder_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define ICLRPrivWinRtTypeBinder_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define ICLRPrivWinRtTypeBinder_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define ICLRPrivWinRtTypeBinder_FindAssemblyForWinRtTypeIfLoaded(This,pAppDomain,szNamespace,szClassName)	\
    ( (This)->lpVtbl -> FindAssemblyForWinRtTypeIfLoaded(This,pAppDomain,szNamespace,szClassName) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ICLRPrivWinRtTypeBinder_INTERFACE_DEFINED__ */


/* Additional Prototypes for ALL interfaces */

/* end of Additional Prototypes */

#ifdef __cplusplus
}
#endif

#endif


