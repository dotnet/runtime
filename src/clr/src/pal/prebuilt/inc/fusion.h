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




#ifndef PEKIND_ENUM_DEFINED
#define PEKIND_ENUM_DEFINED
typedef 
enum _tagPEKIND
    {
        peNone	= 0,
        peMSIL	= 0x1,
        peI386	= 0x2,
        peIA64	= 0x3,
        peAMD64	= 0x4,
        peARM	= 0x5,
        peARM64	= 0x6,
        peInvalid	= 0xffffffff
    } 	PEKIND;

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
        CANOF_PARSE_DISPLAY_NAME	= 0x1,
        CANOF_SET_DEFAULT_VALUES	= 0x2,
        CANOF_VERIFY_FRIEND_ASSEMBLYNAME	= 0x4,
        CANOF_PARSE_FRIEND_DISPLAY_NAME	= ( CANOF_PARSE_DISPLAY_NAME | CANOF_VERIFY_FRIEND_ASSEMBLYNAME ) 
    } 	CREATE_ASM_NAME_OBJ_FLAGS;

typedef /* [public] */ 
enum __MIDL_IAssemblyName_0002
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
enum __MIDL_IAssemblyName_0003
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

typedef /* [public] */ 
enum __MIDL_IAssemblyName_0004
    {
        ASM_CMPF_NAME	= 0x1,
        ASM_CMPF_MAJOR_VERSION	= 0x2,
        ASM_CMPF_MINOR_VERSION	= 0x4,
        ASM_CMPF_BUILD_NUMBER	= 0x8,
        ASM_CMPF_REVISION_NUMBER	= 0x10,
        ASM_CMPF_VERSION	= ( ( ( ASM_CMPF_MAJOR_VERSION | ASM_CMPF_MINOR_VERSION )  | ASM_CMPF_BUILD_NUMBER )  | ASM_CMPF_REVISION_NUMBER ) ,
        ASM_CMPF_PUBLIC_KEY_TOKEN	= 0x20,
        ASM_CMPF_CULTURE	= 0x40,
        ASM_CMPF_CUSTOM	= 0x80,
        ASM_CMPF_DEFAULT	= 0x100,
        ASM_CMPF_RETARGET	= 0x200,
        ASM_CMPF_ARCHITECTURE	= 0x400,
        ASM_CMPF_CONFIG_MASK	= 0x800,
        ASM_CMPF_MVID	= 0x1000,
        ASM_CMPF_SIGNATURE	= 0x2000,
        ASM_CMPF_CONTENT_TYPE	= 0x4000,
        ASM_CMPF_IL_ALL	= ( ( ( ASM_CMPF_NAME | ASM_CMPF_VERSION )  | ASM_CMPF_PUBLIC_KEY_TOKEN )  | ASM_CMPF_CULTURE ) ,
        ASM_CMPF_IL_NO_VERSION	= ( ( ASM_CMPF_NAME | ASM_CMPF_PUBLIC_KEY_TOKEN )  | ASM_CMPF_CULTURE ) 
    } 	ASM_CMP_FLAGS;


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
        
        virtual HRESULT STDMETHODCALLTYPE Finalize( void) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetDisplayName( 
            /* [annotation][out] */ 
            _Out_writes_opt_(*pccDisplayName)  LPOLESTR szDisplayName,
            /* [out][in] */ LPDWORD pccDisplayName,
            /* [in] */ DWORD dwDisplayFlags) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE Reserved( 
            /* [in] */ REFIID refIID,
            /* [in] */ IUnknown *pUnkReserved1,
            /* [in] */ IUnknown *pUnkReserved2,
            /* [in] */ LPCOLESTR szReserved,
            /* [in] */ LONGLONG llReserved,
            /* [in] */ LPVOID pvReserved,
            /* [in] */ DWORD cbReserved,
            /* [out] */ LPVOID *ppReserved) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetName( 
            /* [annotation][out][in] */ 
            _Inout_  LPDWORD lpcwBuffer,
            /* [annotation][out] */ 
            _Out_writes_opt_(*lpcwBuffer)  WCHAR *pwzName) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetVersion( 
            /* [out] */ LPDWORD pdwVersionHi,
            /* [out] */ LPDWORD pdwVersionLow) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE IsEqual( 
            /* [in] */ IAssemblyName *pName,
            /* [in] */ DWORD dwCmpFlags) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE Clone( 
            /* [out] */ IAssemblyName **pName) = 0;
        
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
        
        HRESULT ( STDMETHODCALLTYPE *Finalize )( 
            IAssemblyName * This);
        
        HRESULT ( STDMETHODCALLTYPE *GetDisplayName )( 
            IAssemblyName * This,
            /* [annotation][out] */ 
            _Out_writes_opt_(*pccDisplayName)  LPOLESTR szDisplayName,
            /* [out][in] */ LPDWORD pccDisplayName,
            /* [in] */ DWORD dwDisplayFlags);
        
        HRESULT ( STDMETHODCALLTYPE *Reserved )( 
            IAssemblyName * This,
            /* [in] */ REFIID refIID,
            /* [in] */ IUnknown *pUnkReserved1,
            /* [in] */ IUnknown *pUnkReserved2,
            /* [in] */ LPCOLESTR szReserved,
            /* [in] */ LONGLONG llReserved,
            /* [in] */ LPVOID pvReserved,
            /* [in] */ DWORD cbReserved,
            /* [out] */ LPVOID *ppReserved);
        
        HRESULT ( STDMETHODCALLTYPE *GetName )( 
            IAssemblyName * This,
            /* [annotation][out][in] */ 
            _Inout_  LPDWORD lpcwBuffer,
            /* [annotation][out] */ 
            _Out_writes_opt_(*lpcwBuffer)  WCHAR *pwzName);
        
        HRESULT ( STDMETHODCALLTYPE *GetVersion )( 
            IAssemblyName * This,
            /* [out] */ LPDWORD pdwVersionHi,
            /* [out] */ LPDWORD pdwVersionLow);
        
        HRESULT ( STDMETHODCALLTYPE *IsEqual )( 
            IAssemblyName * This,
            /* [in] */ IAssemblyName *pName,
            /* [in] */ DWORD dwCmpFlags);
        
        HRESULT ( STDMETHODCALLTYPE *Clone )( 
            IAssemblyName * This,
            /* [out] */ IAssemblyName **pName);
        
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

#define IAssemblyName_Finalize(This)	\
    ( (This)->lpVtbl -> Finalize(This) ) 

#define IAssemblyName_GetDisplayName(This,szDisplayName,pccDisplayName,dwDisplayFlags)	\
    ( (This)->lpVtbl -> GetDisplayName(This,szDisplayName,pccDisplayName,dwDisplayFlags) ) 

#define IAssemblyName_Reserved(This,refIID,pUnkReserved1,pUnkReserved2,szReserved,llReserved,pvReserved,cbReserved,ppReserved)	\
    ( (This)->lpVtbl -> Reserved(This,refIID,pUnkReserved1,pUnkReserved2,szReserved,llReserved,pvReserved,cbReserved,ppReserved) ) 

#define IAssemblyName_GetName(This,lpcwBuffer,pwzName)	\
    ( (This)->lpVtbl -> GetName(This,lpcwBuffer,pwzName) ) 

#define IAssemblyName_GetVersion(This,pdwVersionHi,pdwVersionLow)	\
    ( (This)->lpVtbl -> GetVersion(This,pdwVersionHi,pdwVersionLow) ) 

#define IAssemblyName_IsEqual(This,pName,dwCmpFlags)	\
    ( (This)->lpVtbl -> IsEqual(This,pName,dwCmpFlags) ) 

#define IAssemblyName_Clone(This,pName)	\
    ( (This)->lpVtbl -> Clone(This,pName) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __IAssemblyName_INTERFACE_DEFINED__ */


/* interface __MIDL_itf_fusion_0000_0001 */
/* [local] */ 

STDAPI CreateAssemblyNameObject(LPASSEMBLYNAME *ppAssemblyNameObj, LPCWSTR szAssemblyName, DWORD dwFlags, LPVOID pvReserved);             


extern RPC_IF_HANDLE __MIDL_itf_fusion_0000_0001_v0_0_c_ifspec;
extern RPC_IF_HANDLE __MIDL_itf_fusion_0000_0001_v0_0_s_ifspec;

/* Additional Prototypes for ALL interfaces */

/* end of Additional Prototypes */

#ifdef __cplusplus
}
#endif

#endif


