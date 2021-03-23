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
#endif // __RPCNDR_H_VERSION__

#ifndef COM_NO_WINDOWS_H
#include "windows.h"
#include "ole2.h"
#endif /*COM_NO_WINDOWS_H*/

#ifndef __corpub_h__
#define __corpub_h__

#if defined(_MSC_VER) && (_MSC_VER >= 1020)
#pragma once
#endif

/* Forward Declarations */ 

#ifndef __CorpubPublish_FWD_DEFINED__
#define __CorpubPublish_FWD_DEFINED__

#ifdef __cplusplus
typedef class CorpubPublish CorpubPublish;
#else
typedef struct CorpubPublish CorpubPublish;
#endif /* __cplusplus */

#endif 	/* __CorpubPublish_FWD_DEFINED__ */


#ifndef __ICorPublish_FWD_DEFINED__
#define __ICorPublish_FWD_DEFINED__
typedef interface ICorPublish ICorPublish;

#endif 	/* __ICorPublish_FWD_DEFINED__ */


#ifndef __ICorPublishEnum_FWD_DEFINED__
#define __ICorPublishEnum_FWD_DEFINED__
typedef interface ICorPublishEnum ICorPublishEnum;

#endif 	/* __ICorPublishEnum_FWD_DEFINED__ */


#ifndef __ICorPublishProcess_FWD_DEFINED__
#define __ICorPublishProcess_FWD_DEFINED__
typedef interface ICorPublishProcess ICorPublishProcess;

#endif 	/* __ICorPublishProcess_FWD_DEFINED__ */


#ifndef __ICorPublishAppDomain_FWD_DEFINED__
#define __ICorPublishAppDomain_FWD_DEFINED__
typedef interface ICorPublishAppDomain ICorPublishAppDomain;

#endif 	/* __ICorPublishAppDomain_FWD_DEFINED__ */


#ifndef __ICorPublishProcessEnum_FWD_DEFINED__
#define __ICorPublishProcessEnum_FWD_DEFINED__
typedef interface ICorPublishProcessEnum ICorPublishProcessEnum;

#endif 	/* __ICorPublishProcessEnum_FWD_DEFINED__ */


#ifndef __ICorPublishAppDomainEnum_FWD_DEFINED__
#define __ICorPublishAppDomainEnum_FWD_DEFINED__
typedef interface ICorPublishAppDomainEnum ICorPublishAppDomainEnum;

#endif 	/* __ICorPublishAppDomainEnum_FWD_DEFINED__ */


/* header files for imported files */
#include "unknwn.h"

#ifdef __cplusplus
extern "C"{
#endif 


/* interface __MIDL_itf_corpub_0000_0000 */
/* [local] */ 

#if 0
#endif
typedef /* [public][public] */ 
enum __MIDL___MIDL_itf_corpub_0000_0000_0001
    {
        COR_PUB_MANAGEDONLY	= 0x1
    } 	COR_PUB_ENUMPROCESS;

#pragma warning(push)
#pragma warning(disable:28718)    





#pragma warning(pop)


extern RPC_IF_HANDLE __MIDL_itf_corpub_0000_0000_v0_0_c_ifspec;
extern RPC_IF_HANDLE __MIDL_itf_corpub_0000_0000_v0_0_s_ifspec;


#ifndef __CorpubProcessLib_LIBRARY_DEFINED__
#define __CorpubProcessLib_LIBRARY_DEFINED__

/* library CorpubProcessLib */
/* [helpstring][version][uuid] */ 


EXTERN_C const IID LIBID_CorpubProcessLib;

EXTERN_C const CLSID CLSID_CorpubPublish;

#ifdef __cplusplus

class DECLSPEC_UUID("047a9a40-657e-11d3-8d5b-00104b35e7ef")
CorpubPublish;
#endif
#endif /* __CorpubProcessLib_LIBRARY_DEFINED__ */

#ifndef __ICorPublish_INTERFACE_DEFINED__
#define __ICorPublish_INTERFACE_DEFINED__

/* interface ICorPublish */
/* [local][unique][uuid][object] */ 


EXTERN_C const IID IID_ICorPublish;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("9613A0E7-5A68-11d3-8F84-00A0C9B4D50C")
    ICorPublish : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE EnumProcesses( 
            /* [in] */ COR_PUB_ENUMPROCESS Type,
            /* [out] */ ICorPublishProcessEnum **ppIEnum) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetProcess( 
            /* [in] */ unsigned int pid,
            /* [out] */ ICorPublishProcess **ppProcess) = 0;
        
    };
    
    
#else 	/* C style interface */

    typedef struct ICorPublishVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            ICorPublish * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            ICorPublish * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            ICorPublish * This);
        
        HRESULT ( STDMETHODCALLTYPE *EnumProcesses )( 
            ICorPublish * This,
            /* [in] */ COR_PUB_ENUMPROCESS Type,
            /* [out] */ ICorPublishProcessEnum **ppIEnum);
        
        HRESULT ( STDMETHODCALLTYPE *GetProcess )( 
            ICorPublish * This,
            /* [in] */ unsigned int pid,
            /* [out] */ ICorPublishProcess **ppProcess);
        
        END_INTERFACE
    } ICorPublishVtbl;

    interface ICorPublish
    {
        CONST_VTBL struct ICorPublishVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define ICorPublish_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define ICorPublish_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define ICorPublish_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define ICorPublish_EnumProcesses(This,Type,ppIEnum)	\
    ( (This)->lpVtbl -> EnumProcesses(This,Type,ppIEnum) ) 

#define ICorPublish_GetProcess(This,pid,ppProcess)	\
    ( (This)->lpVtbl -> GetProcess(This,pid,ppProcess) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ICorPublish_INTERFACE_DEFINED__ */


#ifndef __ICorPublishEnum_INTERFACE_DEFINED__
#define __ICorPublishEnum_INTERFACE_DEFINED__

/* interface ICorPublishEnum */
/* [local][unique][uuid][object] */ 


EXTERN_C const IID IID_ICorPublishEnum;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("C0B22967-5A69-11d3-8F84-00A0C9B4D50C")
    ICorPublishEnum : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE Skip( 
            /* [in] */ ULONG celt) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE Reset( void) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE Clone( 
            /* [out] */ ICorPublishEnum **ppEnum) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetCount( 
            /* [out] */ ULONG *pcelt) = 0;
        
    };
    
    
#else 	/* C style interface */

    typedef struct ICorPublishEnumVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            ICorPublishEnum * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            ICorPublishEnum * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            ICorPublishEnum * This);
        
        HRESULT ( STDMETHODCALLTYPE *Skip )( 
            ICorPublishEnum * This,
            /* [in] */ ULONG celt);
        
        HRESULT ( STDMETHODCALLTYPE *Reset )( 
            ICorPublishEnum * This);
        
        HRESULT ( STDMETHODCALLTYPE *Clone )( 
            ICorPublishEnum * This,
            /* [out] */ ICorPublishEnum **ppEnum);
        
        HRESULT ( STDMETHODCALLTYPE *GetCount )( 
            ICorPublishEnum * This,
            /* [out] */ ULONG *pcelt);
        
        END_INTERFACE
    } ICorPublishEnumVtbl;

    interface ICorPublishEnum
    {
        CONST_VTBL struct ICorPublishEnumVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define ICorPublishEnum_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define ICorPublishEnum_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define ICorPublishEnum_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define ICorPublishEnum_Skip(This,celt)	\
    ( (This)->lpVtbl -> Skip(This,celt) ) 

#define ICorPublishEnum_Reset(This)	\
    ( (This)->lpVtbl -> Reset(This) ) 

#define ICorPublishEnum_Clone(This,ppEnum)	\
    ( (This)->lpVtbl -> Clone(This,ppEnum) ) 

#define ICorPublishEnum_GetCount(This,pcelt)	\
    ( (This)->lpVtbl -> GetCount(This,pcelt) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ICorPublishEnum_INTERFACE_DEFINED__ */


/* interface __MIDL_itf_corpub_0000_0003 */
/* [local] */ 

#pragma warning(push)
#pragma warning(disable:28718)


extern RPC_IF_HANDLE __MIDL_itf_corpub_0000_0003_v0_0_c_ifspec;
extern RPC_IF_HANDLE __MIDL_itf_corpub_0000_0003_v0_0_s_ifspec;

#ifndef __ICorPublishProcess_INTERFACE_DEFINED__
#define __ICorPublishProcess_INTERFACE_DEFINED__

/* interface ICorPublishProcess */
/* [local][unique][uuid][object] */ 


EXTERN_C const IID IID_ICorPublishProcess;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("18D87AF1-5A6A-11d3-8F84-00A0C9B4D50C")
    ICorPublishProcess : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE IsManaged( 
            /* [out] */ BOOL *pbManaged) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE EnumAppDomains( 
            /* [out] */ ICorPublishAppDomainEnum **ppEnum) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetProcessID( 
            /* [out] */ unsigned int *pid) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetDisplayName( 
            /* [in] */ ULONG32 cchName,
            /* [out] */ ULONG32 *pcchName,
            /* [length_is][size_is][out] */ WCHAR *szName) = 0;
        
    };
    
    
#else 	/* C style interface */

    typedef struct ICorPublishProcessVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            ICorPublishProcess * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            ICorPublishProcess * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            ICorPublishProcess * This);
        
        HRESULT ( STDMETHODCALLTYPE *IsManaged )( 
            ICorPublishProcess * This,
            /* [out] */ BOOL *pbManaged);
        
        HRESULT ( STDMETHODCALLTYPE *EnumAppDomains )( 
            ICorPublishProcess * This,
            /* [out] */ ICorPublishAppDomainEnum **ppEnum);
        
        HRESULT ( STDMETHODCALLTYPE *GetProcessID )( 
            ICorPublishProcess * This,
            /* [out] */ unsigned int *pid);
        
        HRESULT ( STDMETHODCALLTYPE *GetDisplayName )( 
            ICorPublishProcess * This,
            /* [in] */ ULONG32 cchName,
            /* [out] */ ULONG32 *pcchName,
            /* [length_is][size_is][out] */ WCHAR *szName);
        
        END_INTERFACE
    } ICorPublishProcessVtbl;

    interface ICorPublishProcess
    {
        CONST_VTBL struct ICorPublishProcessVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define ICorPublishProcess_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define ICorPublishProcess_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define ICorPublishProcess_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define ICorPublishProcess_IsManaged(This,pbManaged)	\
    ( (This)->lpVtbl -> IsManaged(This,pbManaged) ) 

#define ICorPublishProcess_EnumAppDomains(This,ppEnum)	\
    ( (This)->lpVtbl -> EnumAppDomains(This,ppEnum) ) 

#define ICorPublishProcess_GetProcessID(This,pid)	\
    ( (This)->lpVtbl -> GetProcessID(This,pid) ) 

#define ICorPublishProcess_GetDisplayName(This,cchName,pcchName,szName)	\
    ( (This)->lpVtbl -> GetDisplayName(This,cchName,pcchName,szName) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ICorPublishProcess_INTERFACE_DEFINED__ */


/* interface __MIDL_itf_corpub_0000_0004 */
/* [local] */ 

#pragma warning(pop)
#pragma warning(push)
#pragma warning(disable:28718)


extern RPC_IF_HANDLE __MIDL_itf_corpub_0000_0004_v0_0_c_ifspec;
extern RPC_IF_HANDLE __MIDL_itf_corpub_0000_0004_v0_0_s_ifspec;

#ifndef __ICorPublishAppDomain_INTERFACE_DEFINED__
#define __ICorPublishAppDomain_INTERFACE_DEFINED__

/* interface ICorPublishAppDomain */
/* [local][unique][uuid][object] */ 


EXTERN_C const IID IID_ICorPublishAppDomain;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("D6315C8F-5A6A-11d3-8F84-00A0C9B4D50C")
    ICorPublishAppDomain : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE GetID( 
            /* [out] */ ULONG32 *puId) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetName( 
            /* [in] */ ULONG32 cchName,
            /* [out] */ ULONG32 *pcchName,
            /* [length_is][size_is][out] */ WCHAR *szName) = 0;
        
    };
    
    
#else 	/* C style interface */

    typedef struct ICorPublishAppDomainVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            ICorPublishAppDomain * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            ICorPublishAppDomain * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            ICorPublishAppDomain * This);
        
        HRESULT ( STDMETHODCALLTYPE *GetID )( 
            ICorPublishAppDomain * This,
            /* [out] */ ULONG32 *puId);
        
        HRESULT ( STDMETHODCALLTYPE *GetName )( 
            ICorPublishAppDomain * This,
            /* [in] */ ULONG32 cchName,
            /* [out] */ ULONG32 *pcchName,
            /* [length_is][size_is][out] */ WCHAR *szName);
        
        END_INTERFACE
    } ICorPublishAppDomainVtbl;

    interface ICorPublishAppDomain
    {
        CONST_VTBL struct ICorPublishAppDomainVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define ICorPublishAppDomain_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define ICorPublishAppDomain_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define ICorPublishAppDomain_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define ICorPublishAppDomain_GetID(This,puId)	\
    ( (This)->lpVtbl -> GetID(This,puId) ) 

#define ICorPublishAppDomain_GetName(This,cchName,pcchName,szName)	\
    ( (This)->lpVtbl -> GetName(This,cchName,pcchName,szName) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ICorPublishAppDomain_INTERFACE_DEFINED__ */


/* interface __MIDL_itf_corpub_0000_0005 */
/* [local] */ 

#pragma warning(pop)


extern RPC_IF_HANDLE __MIDL_itf_corpub_0000_0005_v0_0_c_ifspec;
extern RPC_IF_HANDLE __MIDL_itf_corpub_0000_0005_v0_0_s_ifspec;

#ifndef __ICorPublishProcessEnum_INTERFACE_DEFINED__
#define __ICorPublishProcessEnum_INTERFACE_DEFINED__

/* interface ICorPublishProcessEnum */
/* [local][unique][uuid][object] */ 


EXTERN_C const IID IID_ICorPublishProcessEnum;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("A37FBD41-5A69-11d3-8F84-00A0C9B4D50C")
    ICorPublishProcessEnum : public ICorPublishEnum
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE Next( 
            /* [in] */ ULONG celt,
            /* [length_is][size_is][out] */ ICorPublishProcess **objects,
            /* [out] */ ULONG *pceltFetched) = 0;
        
    };
    
    
#else 	/* C style interface */

    typedef struct ICorPublishProcessEnumVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            ICorPublishProcessEnum * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            ICorPublishProcessEnum * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            ICorPublishProcessEnum * This);
        
        HRESULT ( STDMETHODCALLTYPE *Skip )( 
            ICorPublishProcessEnum * This,
            /* [in] */ ULONG celt);
        
        HRESULT ( STDMETHODCALLTYPE *Reset )( 
            ICorPublishProcessEnum * This);
        
        HRESULT ( STDMETHODCALLTYPE *Clone )( 
            ICorPublishProcessEnum * This,
            /* [out] */ ICorPublishEnum **ppEnum);
        
        HRESULT ( STDMETHODCALLTYPE *GetCount )( 
            ICorPublishProcessEnum * This,
            /* [out] */ ULONG *pcelt);
        
        HRESULT ( STDMETHODCALLTYPE *Next )( 
            ICorPublishProcessEnum * This,
            /* [in] */ ULONG celt,
            /* [length_is][size_is][out] */ ICorPublishProcess **objects,
            /* [out] */ ULONG *pceltFetched);
        
        END_INTERFACE
    } ICorPublishProcessEnumVtbl;

    interface ICorPublishProcessEnum
    {
        CONST_VTBL struct ICorPublishProcessEnumVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define ICorPublishProcessEnum_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define ICorPublishProcessEnum_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define ICorPublishProcessEnum_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define ICorPublishProcessEnum_Skip(This,celt)	\
    ( (This)->lpVtbl -> Skip(This,celt) ) 

#define ICorPublishProcessEnum_Reset(This)	\
    ( (This)->lpVtbl -> Reset(This) ) 

#define ICorPublishProcessEnum_Clone(This,ppEnum)	\
    ( (This)->lpVtbl -> Clone(This,ppEnum) ) 

#define ICorPublishProcessEnum_GetCount(This,pcelt)	\
    ( (This)->lpVtbl -> GetCount(This,pcelt) ) 


#define ICorPublishProcessEnum_Next(This,celt,objects,pceltFetched)	\
    ( (This)->lpVtbl -> Next(This,celt,objects,pceltFetched) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ICorPublishProcessEnum_INTERFACE_DEFINED__ */


#ifndef __ICorPublishAppDomainEnum_INTERFACE_DEFINED__
#define __ICorPublishAppDomainEnum_INTERFACE_DEFINED__

/* interface ICorPublishAppDomainEnum */
/* [local][unique][uuid][object] */ 


EXTERN_C const IID IID_ICorPublishAppDomainEnum;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("9F0C98F5-5A6A-11d3-8F84-00A0C9B4D50C")
    ICorPublishAppDomainEnum : public ICorPublishEnum
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE Next( 
            /* [in] */ ULONG celt,
            /* [length_is][size_is][out] */ ICorPublishAppDomain **objects,
            /* [out] */ ULONG *pceltFetched) = 0;
        
    };
    
    
#else 	/* C style interface */

    typedef struct ICorPublishAppDomainEnumVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            ICorPublishAppDomainEnum * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            ICorPublishAppDomainEnum * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            ICorPublishAppDomainEnum * This);
        
        HRESULT ( STDMETHODCALLTYPE *Skip )( 
            ICorPublishAppDomainEnum * This,
            /* [in] */ ULONG celt);
        
        HRESULT ( STDMETHODCALLTYPE *Reset )( 
            ICorPublishAppDomainEnum * This);
        
        HRESULT ( STDMETHODCALLTYPE *Clone )( 
            ICorPublishAppDomainEnum * This,
            /* [out] */ ICorPublishEnum **ppEnum);
        
        HRESULT ( STDMETHODCALLTYPE *GetCount )( 
            ICorPublishAppDomainEnum * This,
            /* [out] */ ULONG *pcelt);
        
        HRESULT ( STDMETHODCALLTYPE *Next )( 
            ICorPublishAppDomainEnum * This,
            /* [in] */ ULONG celt,
            /* [length_is][size_is][out] */ ICorPublishAppDomain **objects,
            /* [out] */ ULONG *pceltFetched);
        
        END_INTERFACE
    } ICorPublishAppDomainEnumVtbl;

    interface ICorPublishAppDomainEnum
    {
        CONST_VTBL struct ICorPublishAppDomainEnumVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define ICorPublishAppDomainEnum_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define ICorPublishAppDomainEnum_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define ICorPublishAppDomainEnum_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define ICorPublishAppDomainEnum_Skip(This,celt)	\
    ( (This)->lpVtbl -> Skip(This,celt) ) 

#define ICorPublishAppDomainEnum_Reset(This)	\
    ( (This)->lpVtbl -> Reset(This) ) 

#define ICorPublishAppDomainEnum_Clone(This,ppEnum)	\
    ( (This)->lpVtbl -> Clone(This,ppEnum) ) 

#define ICorPublishAppDomainEnum_GetCount(This,pcelt)	\
    ( (This)->lpVtbl -> GetCount(This,pcelt) ) 


#define ICorPublishAppDomainEnum_Next(This,celt,objects,pceltFetched)	\
    ( (This)->lpVtbl -> Next(This,celt,objects,pceltFetched) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ICorPublishAppDomainEnum_INTERFACE_DEFINED__ */


/* Additional Prototypes for ALL interfaces */

/* end of Additional Prototypes */

#ifdef __cplusplus
}
#endif

#endif


