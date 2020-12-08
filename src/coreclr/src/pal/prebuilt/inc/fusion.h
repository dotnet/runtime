// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.



/* this ALWAYS GENERATED file contains the definitions for the interfaces */


 /* File created by MIDL compiler version 8.01.0622 */
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

#ifndef __fusion_h__
#define __fusion_h__

#if defined(_MSC_VER) && (_MSC_VER >= 1020)
#pragma once
#endif

/* Forward Declarations */ 

#ifndef __IAssemblyName_FWD_DEFINED__
#define __IAssemblyName_FWD_DEFINED__
typedef interface IAssemblyName IAssemblyName;

#endif 	/* __IAssemblyName_FWD_DEFINED__ */


/* header files for imported files */
#include "objidl.h"

#ifdef __cplusplus
extern "C"{
#endif 


/* interface __MIDL_itf_fusion_0000_0000 */
/* [local] */ 


#ifdef _MSC_VER
#pragma comment(lib,"uuid.lib")
#endif

//---------------------------------------------------------------------------=
// Fusion Interfaces.

#ifdef _MSC_VER
#pragma once
#endif
typedef 
enum _tagAssemblyContentType
    {
        AssemblyContentType_Default	= 0,
        AssemblyContentType_WindowsRuntime	= 0x1,
        AssemblyContentType_Invalid	= 0xffffffff
    } 	AssemblyContentType;

// {CD193BC0-B4BC-11d2-9833-00C04FC31D2E}
EXTERN_GUID(IID_IAssemblyName, 0xCD193BC0, 0xB4BC, 0x11d2, 0x98, 0x33, 0x00, 0xC0, 0x4F, 0xC3, 0x1D, 0x2E);


extern RPC_IF_HANDLE __MIDL_itf_fusion_0000_0000_v0_0_c_ifspec;
extern RPC_IF_HANDLE __MIDL_itf_fusion_0000_0000_v0_0_s_ifspec;

#ifndef __IAssemblyName_INTERFACE_DEFINED__
#define __IAssemblyName_INTERFACE_DEFINED__

/* interface IAssemblyName */
/* [unique][uuid][object][local] */ 

typedef /* [unique] */ IAssemblyName *LPASSEMBLYNAME;

typedef /* [public] */ 
enum __MIDL_IAssemblyName_0001
    {
        ASM_NAME_PUBLIC_KEY	= 0,
        ASM_NAME_PUBLIC_KEY_TOKEN	= ( ASM_NAME_PUBLIC_KEY + 1 ) ,
        ASM_NAME_HASH_VALUE	= ( ASM_NAME_PUBLIC_KEY_TOKEN + 1 ) ,
        ASM_NAME_NAME	= ( ASM_NAME_HASH_VALUE + 1 ) ,
        ASM_NAME_MAJOR_VERSION	= ( ASM_NAME_NAME + 1 ) ,
        ASM_NAME_MINOR_VERSION	= ( ASM_NAME_MAJOR_VERSION + 1 ) ,
        ASM_NAME_BUILD_NUMBER	= ( ASM_NAME_MINOR_VERSION + 1 ) ,
        ASM_NAME_REVISION_NUMBER	= ( ASM_NAME_BUILD_NUMBER + 1 ) ,
        ASM_NAME_CULTURE	= ( ASM_NAME_REVISION_NUMBER + 1 ) ,
        ASM_NAME_PROCESSOR_ID_ARRAY	= ( ASM_NAME_CULTURE + 1 ) ,
        ASM_NAME_OSINFO_ARRAY	= ( ASM_NAME_PROCESSOR_ID_ARRAY + 1 ) ,
        ASM_NAME_HASH_ALGID	= ( ASM_NAME_OSINFO_ARRAY + 1 ) ,
        ASM_NAME_ALIAS	= ( ASM_NAME_HASH_ALGID + 1 ) ,
        ASM_NAME_CODEBASE_URL	= ( ASM_NAME_ALIAS + 1 ) ,
        ASM_NAME_CODEBASE_LASTMOD	= ( ASM_NAME_CODEBASE_URL + 1 ) ,
        ASM_NAME_NULL_PUBLIC_KEY	= ( ASM_NAME_CODEBASE_LASTMOD + 1 ) ,
        ASM_NAME_NULL_PUBLIC_KEY_TOKEN	= ( ASM_NAME_NULL_PUBLIC_KEY + 1 ) ,
        ASM_NAME_CUSTOM	= ( ASM_NAME_NULL_PUBLIC_KEY_TOKEN + 1 ) ,
        ASM_NAME_NULL_CUSTOM	= ( ASM_NAME_CUSTOM + 1 ) ,
        ASM_NAME_MVID	= ( ASM_NAME_NULL_CUSTOM + 1 ) ,
        ASM_NAME_FILE_MAJOR_VERSION	= ( ASM_NAME_MVID + 1 ) ,
        ASM_NAME_FILE_MINOR_VERSION	= ( ASM_NAME_FILE_MAJOR_VERSION + 1 ) ,
        ASM_NAME_FILE_BUILD_NUMBER	= ( ASM_NAME_FILE_MINOR_VERSION + 1 ) ,
        ASM_NAME_FILE_REVISION_NUMBER	= ( ASM_NAME_FILE_BUILD_NUMBER + 1 ) ,
        ASM_NAME_RETARGET	= ( ASM_NAME_FILE_REVISION_NUMBER + 1 ) ,
        ASM_NAME_SIGNATURE_BLOB	= ( ASM_NAME_RETARGET + 1 ) ,
        ASM_NAME_CONFIG_MASK	= ( ASM_NAME_SIGNATURE_BLOB + 1 ) ,
        ASM_NAME_ARCHITECTURE	= ( ASM_NAME_CONFIG_MASK + 1 ) ,
        ASM_NAME_CONTENT_TYPE	= ( ASM_NAME_ARCHITECTURE + 1 ) ,
        ASM_NAME_MAX_PARAMS	= ( ASM_NAME_CONTENT_TYPE + 1 ) 
    } 	ASM_NAME;

typedef /* [public] */ 
enum __MIDL_IAssemblyName_0002
    {
        ASM_DISPLAYF_VERSION	= 0x1,
        ASM_DISPLAYF_CULTURE	= 0x2,
        ASM_DISPLAYF_PUBLIC_KEY_TOKEN	= 0x4,
        ASM_DISPLAYF_PUBLIC_KEY	= 0x8,
        ASM_DISPLAYF_CUSTOM	= 0x10,
        ASM_DISPLAYF_PROCESSORARCHITECTURE	= 0x20,
        ASM_DISPLAYF_LANGUAGEID	= 0x40,
        ASM_DISPLAYF_RETARGET	= 0x80,
        ASM_DISPLAYF_CONFIG_MASK	= 0x100,
        ASM_DISPLAYF_MVID	= 0x200,
        ASM_DISPLAYF_CONTENT_TYPE	= 0x400,
        ASM_DISPLAYF_FULL	= ( ( ( ( ( ASM_DISPLAYF_VERSION | ASM_DISPLAYF_CULTURE )  | ASM_DISPLAYF_PUBLIC_KEY_TOKEN )  | ASM_DISPLAYF_RETARGET )  | ASM_DISPLAYF_PROCESSORARCHITECTURE )  | ASM_DISPLAYF_CONTENT_TYPE ) 
    } 	ASM_DISPLAY_FLAGS;


EXTERN_C const IID IID_IAssemblyName;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("CD193BC0-B4BC-11d2-9833-00C04FC31D2E")
    IAssemblyName : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE SetProperty( 
            /* [in] */ DWORD PropertyId,
            /* [in] */ const void *pvProperty,
            /* [in] */ DWORD cbProperty) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetProperty( 
            /* [in] */ DWORD PropertyId,
            /* [out] */ LPVOID pvProperty,
            /* [out][in] */ LPDWORD pcbProperty) = 0;
        
    };
    
    
#else 	/* C style interface */

    typedef struct IAssemblyNameVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            IAssemblyName * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            IAssemblyName * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            IAssemblyName * This);
        
        HRESULT ( STDMETHODCALLTYPE *SetProperty )( 
            IAssemblyName * This,
            /* [in] */ DWORD PropertyId,
            /* [in] */ const void *pvProperty,
            /* [in] */ DWORD cbProperty);
        
        HRESULT ( STDMETHODCALLTYPE *GetProperty )( 
            IAssemblyName * This,
            /* [in] */ DWORD PropertyId,
            /* [out] */ LPVOID pvProperty,
            /* [out][in] */ LPDWORD pcbProperty);
        
        END_INTERFACE
    } IAssemblyNameVtbl;

    interface IAssemblyName
    {
        CONST_VTBL struct IAssemblyNameVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define IAssemblyName_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define IAssemblyName_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define IAssemblyName_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define IAssemblyName_SetProperty(This,PropertyId,pvProperty,cbProperty)	\
    ( (This)->lpVtbl -> SetProperty(This,PropertyId,pvProperty,cbProperty) ) 

#define IAssemblyName_GetProperty(This,PropertyId,pvProperty,pcbProperty)	\
    ( (This)->lpVtbl -> GetProperty(This,PropertyId,pvProperty,pcbProperty) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __IAssemblyName_INTERFACE_DEFINED__ */


/* Additional Prototypes for ALL interfaces */

/* end of Additional Prototypes */

#ifdef __cplusplus
}
#endif

#endif


