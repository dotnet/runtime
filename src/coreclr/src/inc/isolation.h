//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

/* verify that the <rpcndr.h> version is high enough to compile this file*/
#ifndef __REQUIRED_RPCNDR_H_VERSION__
#define __REQUIRED_RPCNDR_H_VERSION__ 475
#endif

#include <specstrings.h>

#include "rpc.h"
#include "rpcndr.h"

#ifndef __RPCNDR_H_VERSION__
#error this stub requires an updated version of <rpcndr.h>
#endif // __RPCNDR_H_VERSION__

#ifndef COM_NO_WINDOWS_H
#include "windows.h"
#include "ole2.h"
#endif /*COM_NO_WINDOWS_H*/

#ifndef __isolation_h__
#define __isolation_h__

#if defined(_MSC_VER) && (_MSC_VER >= 1020)
#pragma once
#endif

/* Forward Declarations */ 

#ifndef __IReferenceIdentity_FWD_DEFINED__
#define __IReferenceIdentity_FWD_DEFINED__
typedef interface IReferenceIdentity IReferenceIdentity;
#endif 	/* __IReferenceIdentity_FWD_DEFINED__ */


#ifndef __IDefinitionIdentity_FWD_DEFINED__
#define __IDefinitionIdentity_FWD_DEFINED__
typedef interface IDefinitionIdentity IDefinitionIdentity;
#endif 	/* __IDefinitionIdentity_FWD_DEFINED__ */


#ifndef __IEnumIDENTITY_ATTRIBUTE_FWD_DEFINED__
#define __IEnumIDENTITY_ATTRIBUTE_FWD_DEFINED__
typedef interface IEnumIDENTITY_ATTRIBUTE IEnumIDENTITY_ATTRIBUTE;
#endif 	/* __IEnumIDENTITY_ATTRIBUTE_FWD_DEFINED__ */


#ifndef __IEnumDefinitionIdentity_FWD_DEFINED__
#define __IEnumDefinitionIdentity_FWD_DEFINED__
typedef interface IEnumDefinitionIdentity IEnumDefinitionIdentity;
#endif 	/* __IEnumDefinitionIdentity_FWD_DEFINED__ */


#ifndef __IEnumReferenceIdentity_FWD_DEFINED__
#define __IEnumReferenceIdentity_FWD_DEFINED__
typedef interface IEnumReferenceIdentity IEnumReferenceIdentity;
#endif 	/* __IEnumReferenceIdentity_FWD_DEFINED__ */


#ifndef __IDefinitionAppId_FWD_DEFINED__
#define __IDefinitionAppId_FWD_DEFINED__
typedef interface IDefinitionAppId IDefinitionAppId;
#endif 	/* __IDefinitionAppId_FWD_DEFINED__ */


#ifndef __IReferenceAppId_FWD_DEFINED__
#define __IReferenceAppId_FWD_DEFINED__
typedef interface IReferenceAppId IReferenceAppId;
#endif 	/* __IReferenceAppId_FWD_DEFINED__ */


#ifndef __IIdentityAuthority_FWD_DEFINED__
#define __IIdentityAuthority_FWD_DEFINED__
typedef interface IIdentityAuthority IIdentityAuthority;
#endif 	/* __IIdentityAuthority_FWD_DEFINED__ */


#ifndef __IAppIdAuthority_FWD_DEFINED__
#define __IAppIdAuthority_FWD_DEFINED__
typedef interface IAppIdAuthority IAppIdAuthority;
#endif 	/* __IAppIdAuthority_FWD_DEFINED__ */


#ifndef __IIdentityAuthority_FWD_DEFINED__
#define __IIdentityAuthority_FWD_DEFINED__
typedef interface IIdentityAuthority IIdentityAuthority;
#endif 	/* __IIdentityAuthority_FWD_DEFINED__ */


#ifndef __IAppIdAuthority_FWD_DEFINED__
#define __IAppIdAuthority_FWD_DEFINED__
typedef interface IAppIdAuthority IAppIdAuthority;
#endif 	/* __IAppIdAuthority_FWD_DEFINED__ */


#ifndef __IDefinitionIdentity_FWD_DEFINED__
#define __IDefinitionIdentity_FWD_DEFINED__
typedef interface IDefinitionIdentity IDefinitionIdentity;
#endif 	/* __IDefinitionIdentity_FWD_DEFINED__ */


#ifndef __IReferenceIdentity_FWD_DEFINED__
#define __IReferenceIdentity_FWD_DEFINED__
typedef interface IReferenceIdentity IReferenceIdentity;
#endif 	/* __IReferenceIdentity_FWD_DEFINED__ */


#ifndef __IDefinitionAppId_FWD_DEFINED__
#define __IDefinitionAppId_FWD_DEFINED__
typedef interface IDefinitionAppId IDefinitionAppId;
#endif 	/* __IDefinitionAppId_FWD_DEFINED__ */


#ifndef __IReferenceAppId_FWD_DEFINED__
#define __IReferenceAppId_FWD_DEFINED__
typedef interface IReferenceAppId IReferenceAppId;
#endif 	/* __IReferenceAppId_FWD_DEFINED__ */


#ifndef __IEnumIDENTITY_ATTRIBUTE_FWD_DEFINED__
#define __IEnumIDENTITY_ATTRIBUTE_FWD_DEFINED__
typedef interface IEnumIDENTITY_ATTRIBUTE IEnumIDENTITY_ATTRIBUTE;
#endif 	/* __IEnumIDENTITY_ATTRIBUTE_FWD_DEFINED__ */


#ifndef __IEnumDefinitionIdentity_FWD_DEFINED__
#define __IEnumDefinitionIdentity_FWD_DEFINED__
typedef interface IEnumDefinitionIdentity IEnumDefinitionIdentity;
#endif 	/* __IEnumDefinitionIdentity_FWD_DEFINED__ */


#ifndef __IEnumReferenceIdentity_FWD_DEFINED__
#define __IEnumReferenceIdentity_FWD_DEFINED__
typedef interface IEnumReferenceIdentity IEnumReferenceIdentity;
#endif 	/* __IEnumReferenceIdentity_FWD_DEFINED__ */


/* header files for imported files */
#include "unknwn.h"
#include "oaidl.h"
#include "ocidl.h"
#include "propidl.h"

#ifdef __cplusplus
extern "C"{
#endif 

_Success_(return != NULL)
_Ret_maybenull_
_Post_writable_byte_size_(size)
void * __RPC_USER MIDL_user_allocate(size_t size);
#pragma warning(suppress: 4985)		// Windows annotates with declspecs


typedef struct _IDENTITY_ATTRIBUTE
    {
    LPCWSTR pszNamespace;
    LPCWSTR pszName;
    LPCWSTR pszValue;
    } 	IDENTITY_ATTRIBUTE;

typedef struct _IDENTITY_ATTRIBUTE *PIDENTITY_ATTRIBUTE;

typedef const IDENTITY_ATTRIBUTE *PCIDENTITY_ATTRIBUTE;


#ifndef __IReferenceIdentity_INTERFACE_DEFINED__
#define __IReferenceIdentity_INTERFACE_DEFINED__

/* interface IReferenceIdentity */
/* [local][unique][uuid][object] */ 


EXTERN_C const IID IID_IReferenceIdentity;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("6eaf5ace-7917-4f3c-b129-e046a9704766")
    IReferenceIdentity : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE GetAttribute( 
            /* [unique][in] */ LPCWSTR pszNamespace,
            /* [in] */ LPCWSTR pszName,
            /* [retval][out] */ LPWSTR *ppszValue) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE SetAttribute( 
            /* [unique][in] */ LPCWSTR pszNamespace,
            /* [in] */ LPCWSTR pszName,
            /* [unique][in] */ LPCWSTR pszValue) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE EnumAttributes( 
            /* [retval][out] */ IEnumIDENTITY_ATTRIBUTE **ppIEnumIDENTITY_ATTRIBUTE) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE Clone( 
            /* [in] */ SIZE_T cDeltas,
            /* [size_is][in] */ const IDENTITY_ATTRIBUTE rgDeltas[  ],
            /* [retval][out] */ IReferenceIdentity **ppIReferenceIdentity) = 0;
        
    };
    
#else 	/* C style interface */

    typedef struct IReferenceIdentityVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            IReferenceIdentity * This,
            /* [in] */ REFIID riid,
            /* [iid_is][out] */ void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            IReferenceIdentity * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            IReferenceIdentity * This);
        
        HRESULT ( STDMETHODCALLTYPE *GetAttribute )( 
            IReferenceIdentity * This,
            /* [unique][in] */ LPCWSTR pszNamespace,
            /* [in] */ LPCWSTR pszName,
            /* [retval][out] */ LPWSTR *ppszValue);
        
        HRESULT ( STDMETHODCALLTYPE *SetAttribute )( 
            IReferenceIdentity * This,
            /* [unique][in] */ LPCWSTR pszNamespace,
            /* [in] */ LPCWSTR pszName,
            /* [unique][in] */ LPCWSTR pszValue);
        
        HRESULT ( STDMETHODCALLTYPE *EnumAttributes )( 
            IReferenceIdentity * This,
            /* [retval][out] */ IEnumIDENTITY_ATTRIBUTE **ppIEnumIDENTITY_ATTRIBUTE);
        
        HRESULT ( STDMETHODCALLTYPE *Clone )( 
            IReferenceIdentity * This,
            /* [in] */ SIZE_T cDeltas,
            /* [size_is][in] */ const IDENTITY_ATTRIBUTE rgDeltas[  ],
            /* [retval][out] */ IReferenceIdentity **ppIReferenceIdentity);
        
        END_INTERFACE
    } IReferenceIdentityVtbl;

    interface IReferenceIdentity
    {
        CONST_VTBL struct IReferenceIdentityVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define IReferenceIdentity_QueryInterface(This,riid,ppvObject)	\
    (This)->lpVtbl -> QueryInterface(This,riid,ppvObject)

#define IReferenceIdentity_AddRef(This)	\
    (This)->lpVtbl -> AddRef(This)

#define IReferenceIdentity_Release(This)	\
    (This)->lpVtbl -> Release(This)


#define IReferenceIdentity_GetAttribute(This,pszNamespace,pszName,ppszValue)	\
    (This)->lpVtbl -> GetAttribute(This,pszNamespace,pszName,ppszValue)

#define IReferenceIdentity_SetAttribute(This,pszNamespace,pszName,pszValue)	\
    (This)->lpVtbl -> SetAttribute(This,pszNamespace,pszName,pszValue)

#define IReferenceIdentity_EnumAttributes(This,ppIEnumIDENTITY_ATTRIBUTE)	\
    (This)->lpVtbl -> EnumAttributes(This,ppIEnumIDENTITY_ATTRIBUTE)

#define IReferenceIdentity_Clone(This,cDeltas,rgDeltas,ppIReferenceIdentity)	\
    (This)->lpVtbl -> Clone(This,cDeltas,rgDeltas,ppIReferenceIdentity)

#endif /* COBJMACROS */


#endif 	/* C style interface */



HRESULT STDMETHODCALLTYPE IReferenceIdentity_GetAttribute_Proxy( 
    IReferenceIdentity * This,
    /* [unique][in] */ LPCWSTR pszNamespace,
    /* [in] */ LPCWSTR pszName,
    /* [retval][out] */ LPWSTR *ppszValue);


void __RPC_STUB IReferenceIdentity_GetAttribute_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IReferenceIdentity_SetAttribute_Proxy( 
    IReferenceIdentity * This,
    /* [unique][in] */ LPCWSTR pszNamespace,
    /* [in] */ LPCWSTR pszName,
    /* [unique][in] */ LPCWSTR pszValue);


void __RPC_STUB IReferenceIdentity_SetAttribute_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IReferenceIdentity_EnumAttributes_Proxy( 
    IReferenceIdentity * This,
    /* [retval][out] */ IEnumIDENTITY_ATTRIBUTE **ppIEnumIDENTITY_ATTRIBUTE);


void __RPC_STUB IReferenceIdentity_EnumAttributes_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IReferenceIdentity_Clone_Proxy( 
    IReferenceIdentity * This,
    /* [in] */ SIZE_T cDeltas,
    /* [size_is][in] */ const IDENTITY_ATTRIBUTE rgDeltas[  ],
    /* [retval][out] */ IReferenceIdentity **ppIReferenceIdentity);


void __RPC_STUB IReferenceIdentity_Clone_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);



#endif 	/* __IReferenceIdentity_INTERFACE_DEFINED__ */


#ifndef __IDefinitionIdentity_INTERFACE_DEFINED__
#define __IDefinitionIdentity_INTERFACE_DEFINED__

/* interface IDefinitionIdentity */
/* [local][unique][uuid][object] */ 


EXTERN_C const IID IID_IDefinitionIdentity;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("587bf538-4d90-4a3c-9ef1-58a200a8a9e7")
    IDefinitionIdentity : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE GetAttribute( 
            /* [unique][in] */ LPCWSTR pszNamespace,
            /* [in] */ LPCWSTR pszName,
            /* [retval][out] */ LPWSTR *ppszValue) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE SetAttribute( 
            /* [unique][in] */ LPCWSTR pszNamespace,
            /* [in] */ LPCWSTR pszName,
            /* [unique][in] */ LPCWSTR pszValue) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE EnumAttributes( 
            /* [retval][out] */ IEnumIDENTITY_ATTRIBUTE **ppIEAIA) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE Clone( 
            /* [in] */ SIZE_T cDeltas,
            /* [size_is][in] */ const IDENTITY_ATTRIBUTE prgDeltas[  ],
            /* [retval][out] */ IDefinitionIdentity **ppIDefinitionIdentity) = 0;
        
    };
    
#else 	/* C style interface */

    typedef struct IDefinitionIdentityVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            IDefinitionIdentity * This,
            /* [in] */ REFIID riid,
            /* [iid_is][out] */ void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            IDefinitionIdentity * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            IDefinitionIdentity * This);
        
        HRESULT ( STDMETHODCALLTYPE *GetAttribute )( 
            IDefinitionIdentity * This,
            /* [unique][in] */ LPCWSTR pszNamespace,
            /* [in] */ LPCWSTR pszName,
            /* [retval][out] */ LPWSTR *ppszValue);
        
        HRESULT ( STDMETHODCALLTYPE *SetAttribute )( 
            IDefinitionIdentity * This,
            /* [unique][in] */ LPCWSTR pszNamespace,
            /* [in] */ LPCWSTR pszName,
            /* [unique][in] */ LPCWSTR pszValue);
        
        HRESULT ( STDMETHODCALLTYPE *EnumAttributes )( 
            IDefinitionIdentity * This,
            /* [retval][out] */ IEnumIDENTITY_ATTRIBUTE **ppIEAIA);
        
        HRESULT ( STDMETHODCALLTYPE *Clone )( 
            IDefinitionIdentity * This,
            /* [in] */ SIZE_T cDeltas,
            /* [size_is][in] */ const IDENTITY_ATTRIBUTE prgDeltas[  ],
            /* [retval][out] */ IDefinitionIdentity **ppIDefinitionIdentity);
        
        END_INTERFACE
    } IDefinitionIdentityVtbl;

    interface IDefinitionIdentity
    {
        CONST_VTBL struct IDefinitionIdentityVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define IDefinitionIdentity_QueryInterface(This,riid,ppvObject)	\
    (This)->lpVtbl -> QueryInterface(This,riid,ppvObject)

#define IDefinitionIdentity_AddRef(This)	\
    (This)->lpVtbl -> AddRef(This)

#define IDefinitionIdentity_Release(This)	\
    (This)->lpVtbl -> Release(This)


#define IDefinitionIdentity_GetAttribute(This,pszNamespace,pszName,ppszValue)	\
    (This)->lpVtbl -> GetAttribute(This,pszNamespace,pszName,ppszValue)

#define IDefinitionIdentity_SetAttribute(This,pszNamespace,pszName,pszValue)	\
    (This)->lpVtbl -> SetAttribute(This,pszNamespace,pszName,pszValue)

#define IDefinitionIdentity_EnumAttributes(This,ppIEAIA)	\
    (This)->lpVtbl -> EnumAttributes(This,ppIEAIA)

#define IDefinitionIdentity_Clone(This,cDeltas,prgDeltas,ppIDefinitionIdentity)	\
    (This)->lpVtbl -> Clone(This,cDeltas,prgDeltas,ppIDefinitionIdentity)

#endif /* COBJMACROS */


#endif 	/* C style interface */



HRESULT STDMETHODCALLTYPE IDefinitionIdentity_GetAttribute_Proxy( 
    IDefinitionIdentity * This,
    /* [unique][in] */ LPCWSTR pszNamespace,
    /* [in] */ LPCWSTR pszName,
    /* [retval][out] */ LPWSTR *ppszValue);


void __RPC_STUB IDefinitionIdentity_GetAttribute_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IDefinitionIdentity_SetAttribute_Proxy( 
    IDefinitionIdentity * This,
    /* [unique][in] */ LPCWSTR pszNamespace,
    /* [in] */ LPCWSTR pszName,
    /* [unique][in] */ LPCWSTR pszValue);


void __RPC_STUB IDefinitionIdentity_SetAttribute_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IDefinitionIdentity_EnumAttributes_Proxy( 
    IDefinitionIdentity * This,
    /* [retval][out] */ IEnumIDENTITY_ATTRIBUTE **ppIEAIA);


void __RPC_STUB IDefinitionIdentity_EnumAttributes_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IDefinitionIdentity_Clone_Proxy( 
    IDefinitionIdentity * This,
    /* [in] */ SIZE_T cDeltas,
    /* [size_is][in] */ const IDENTITY_ATTRIBUTE prgDeltas[  ],
    /* [retval][out] */ IDefinitionIdentity **ppIDefinitionIdentity);


void __RPC_STUB IDefinitionIdentity_Clone_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);



#endif 	/* __IDefinitionIdentity_INTERFACE_DEFINED__ */


/* interface __MIDL_itf_isolation_0320 */
/* [local] */ 

typedef struct _IDENTITY_ATTRIBUTE_BLOB
    {
    DWORD ofsNamespace;
    DWORD ofsName;
    DWORD ofsValue;
    } 	IDENTITY_ATTRIBUTE_BLOB;

typedef struct _IDENTITY_ATTRIBUTE_BLOB *PIDENTITY_ATTRIBUTE_BLOB;



extern RPC_IF_HANDLE __MIDL_itf_isolation_0320_v0_0_c_ifspec;
extern RPC_IF_HANDLE __MIDL_itf_isolation_0320_v0_0_s_ifspec;

#ifndef __IEnumIDENTITY_ATTRIBUTE_INTERFACE_DEFINED__
#define __IEnumIDENTITY_ATTRIBUTE_INTERFACE_DEFINED__

/* interface IEnumIDENTITY_ATTRIBUTE */
/* [local][unique][uuid][object] */ 


EXTERN_C const IID IID_IEnumIDENTITY_ATTRIBUTE;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("9cdaae75-246e-4b00-a26d-b9aec137a3eb")
    IEnumIDENTITY_ATTRIBUTE : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE Next( 
            /* [in] */ ULONG celt,
            /* [length_is][size_is][out] */ IDENTITY_ATTRIBUTE rgAttributes[  ],
            /* [optional][out] */ ULONG *pceltWritten) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE CurrentIntoBuffer( 
            /* [in] */ SIZE_T cbAvailable,
            /* [length_is][size_is][out][in] */ BYTE pbData[  ],
            /* [out] */ SIZE_T *pcbUsed) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE Skip( 
            /* [in] */ ULONG celt) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE Reset( void) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE Clone( 
            /* [out] */ IEnumIDENTITY_ATTRIBUTE **ppIEnumIDENTITY_ATTRIBUTE) = 0;
        
    };
    
#else 	/* C style interface */

    typedef struct IEnumIDENTITY_ATTRIBUTEVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            IEnumIDENTITY_ATTRIBUTE * This,
            /* [in] */ REFIID riid,
            /* [iid_is][out] */ void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            IEnumIDENTITY_ATTRIBUTE * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            IEnumIDENTITY_ATTRIBUTE * This);
        
        HRESULT ( STDMETHODCALLTYPE *Next )( 
            IEnumIDENTITY_ATTRIBUTE * This,
            /* [in] */ ULONG celt,
            /* [length_is][size_is][out] */ IDENTITY_ATTRIBUTE rgAttributes[  ],
            /* [optional][out] */ ULONG *pceltWritten);
        
        HRESULT ( STDMETHODCALLTYPE *CurrentIntoBuffer )( 
            IEnumIDENTITY_ATTRIBUTE * This,
            /* [in] */ SIZE_T cbAvailable,
            /* [length_is][size_is][out][in] */ BYTE pbData[  ],
            /* [out] */ SIZE_T *pcbUsed);
        
        HRESULT ( STDMETHODCALLTYPE *Skip )( 
            IEnumIDENTITY_ATTRIBUTE * This,
            /* [in] */ ULONG celt);
        
        HRESULT ( STDMETHODCALLTYPE *Reset )( 
            IEnumIDENTITY_ATTRIBUTE * This);
        
        HRESULT ( STDMETHODCALLTYPE *Clone )( 
            IEnumIDENTITY_ATTRIBUTE * This,
            /* [out] */ IEnumIDENTITY_ATTRIBUTE **ppIEnumIDENTITY_ATTRIBUTE);
        
        END_INTERFACE
    } IEnumIDENTITY_ATTRIBUTEVtbl;

    interface IEnumIDENTITY_ATTRIBUTE
    {
        CONST_VTBL struct IEnumIDENTITY_ATTRIBUTEVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define IEnumIDENTITY_ATTRIBUTE_QueryInterface(This,riid,ppvObject)	\
    (This)->lpVtbl -> QueryInterface(This,riid,ppvObject)

#define IEnumIDENTITY_ATTRIBUTE_AddRef(This)	\
    (This)->lpVtbl -> AddRef(This)

#define IEnumIDENTITY_ATTRIBUTE_Release(This)	\
    (This)->lpVtbl -> Release(This)


#define IEnumIDENTITY_ATTRIBUTE_Next(This,celt,rgAttributes,pceltWritten)	\
    (This)->lpVtbl -> Next(This,celt,rgAttributes,pceltWritten)

#define IEnumIDENTITY_ATTRIBUTE_CurrentIntoBuffer(This,cbAvailable,pbData,pcbUsed)	\
    (This)->lpVtbl -> CurrentIntoBuffer(This,cbAvailable,pbData,pcbUsed)

#define IEnumIDENTITY_ATTRIBUTE_Skip(This,celt)	\
    (This)->lpVtbl -> Skip(This,celt)

#define IEnumIDENTITY_ATTRIBUTE_Reset(This)	\
    (This)->lpVtbl -> Reset(This)

#define IEnumIDENTITY_ATTRIBUTE_Clone(This,ppIEnumIDENTITY_ATTRIBUTE)	\
    (This)->lpVtbl -> Clone(This,ppIEnumIDENTITY_ATTRIBUTE)

#endif /* COBJMACROS */


#endif 	/* C style interface */



HRESULT STDMETHODCALLTYPE IEnumIDENTITY_ATTRIBUTE_Next_Proxy( 
    IEnumIDENTITY_ATTRIBUTE * This,
    /* [in] */ ULONG celt,
    /* [length_is][size_is][out] */ IDENTITY_ATTRIBUTE rgAttributes[  ],
    /* [optional][out] */ ULONG *pceltWritten);


void __RPC_STUB IEnumIDENTITY_ATTRIBUTE_Next_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IEnumIDENTITY_ATTRIBUTE_CurrentIntoBuffer_Proxy( 
    IEnumIDENTITY_ATTRIBUTE * This,
    /* [in] */ SIZE_T cbAvailable,
    /* [length_is][size_is][out][in] */ BYTE pbData[  ],
    /* [out] */ SIZE_T *pcbUsed);


void __RPC_STUB IEnumIDENTITY_ATTRIBUTE_CurrentIntoBuffer_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IEnumIDENTITY_ATTRIBUTE_Skip_Proxy( 
    IEnumIDENTITY_ATTRIBUTE * This,
    /* [in] */ ULONG celt);


void __RPC_STUB IEnumIDENTITY_ATTRIBUTE_Skip_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IEnumIDENTITY_ATTRIBUTE_Reset_Proxy( 
    IEnumIDENTITY_ATTRIBUTE * This);


void __RPC_STUB IEnumIDENTITY_ATTRIBUTE_Reset_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IEnumIDENTITY_ATTRIBUTE_Clone_Proxy( 
    IEnumIDENTITY_ATTRIBUTE * This,
    /* [out] */ IEnumIDENTITY_ATTRIBUTE **ppIEnumIDENTITY_ATTRIBUTE);


void __RPC_STUB IEnumIDENTITY_ATTRIBUTE_Clone_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);



#endif 	/* __IEnumIDENTITY_ATTRIBUTE_INTERFACE_DEFINED__ */


#ifndef __IEnumDefinitionIdentity_INTERFACE_DEFINED__
#define __IEnumDefinitionIdentity_INTERFACE_DEFINED__

/* interface IEnumDefinitionIdentity */
/* [local][unique][uuid][object] */ 


EXTERN_C const IID IID_IEnumDefinitionIdentity;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("f3549d9c-fc73-4793-9c00-1cd204254c0c")
    IEnumDefinitionIdentity : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE Next( 
            /* [in] */ ULONG celt,
            /* [length_is][size_is][out] */ IDefinitionIdentity *rgpIDefinitionIdentity[  ],
            /* [out] */ ULONG *pceltWritten) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE Skip( 
            /* [in] */ ULONG celt) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE Reset( void) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE Clone( 
            /* [out] */ IEnumDefinitionIdentity **ppIEnumDefinitionIdentity) = 0;
        
    };
    
#else 	/* C style interface */

    typedef struct IEnumDefinitionIdentityVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            IEnumDefinitionIdentity * This,
            /* [in] */ REFIID riid,
            /* [iid_is][out] */ void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            IEnumDefinitionIdentity * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            IEnumDefinitionIdentity * This);
        
        HRESULT ( STDMETHODCALLTYPE *Next )( 
            IEnumDefinitionIdentity * This,
            /* [in] */ ULONG celt,
            /* [length_is][size_is][out] */ IDefinitionIdentity *rgpIDefinitionIdentity[  ],
            /* [out] */ ULONG *pceltWritten);
        
        HRESULT ( STDMETHODCALLTYPE *Skip )( 
            IEnumDefinitionIdentity * This,
            /* [in] */ ULONG celt);
        
        HRESULT ( STDMETHODCALLTYPE *Reset )( 
            IEnumDefinitionIdentity * This);
        
        HRESULT ( STDMETHODCALLTYPE *Clone )( 
            IEnumDefinitionIdentity * This,
            /* [out] */ IEnumDefinitionIdentity **ppIEnumDefinitionIdentity);
        
        END_INTERFACE
    } IEnumDefinitionIdentityVtbl;

    interface IEnumDefinitionIdentity
    {
        CONST_VTBL struct IEnumDefinitionIdentityVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define IEnumDefinitionIdentity_QueryInterface(This,riid,ppvObject)	\
    (This)->lpVtbl -> QueryInterface(This,riid,ppvObject)

#define IEnumDefinitionIdentity_AddRef(This)	\
    (This)->lpVtbl -> AddRef(This)

#define IEnumDefinitionIdentity_Release(This)	\
    (This)->lpVtbl -> Release(This)


#define IEnumDefinitionIdentity_Next(This,celt,rgpIDefinitionIdentity,pceltWritten)	\
    (This)->lpVtbl -> Next(This,celt,rgpIDefinitionIdentity,pceltWritten)

#define IEnumDefinitionIdentity_Skip(This,celt)	\
    (This)->lpVtbl -> Skip(This,celt)

#define IEnumDefinitionIdentity_Reset(This)	\
    (This)->lpVtbl -> Reset(This)

#define IEnumDefinitionIdentity_Clone(This,ppIEnumDefinitionIdentity)	\
    (This)->lpVtbl -> Clone(This,ppIEnumDefinitionIdentity)

#endif /* COBJMACROS */


#endif 	/* C style interface */



HRESULT STDMETHODCALLTYPE IEnumDefinitionIdentity_Next_Proxy( 
    IEnumDefinitionIdentity * This,
    /* [in] */ ULONG celt,
    /* [length_is][size_is][out] */ IDefinitionIdentity *rgpIDefinitionIdentity[  ],
    /* [out] */ ULONG *pceltWritten);


void __RPC_STUB IEnumDefinitionIdentity_Next_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IEnumDefinitionIdentity_Skip_Proxy( 
    IEnumDefinitionIdentity * This,
    /* [in] */ ULONG celt);


void __RPC_STUB IEnumDefinitionIdentity_Skip_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IEnumDefinitionIdentity_Reset_Proxy( 
    IEnumDefinitionIdentity * This);


void __RPC_STUB IEnumDefinitionIdentity_Reset_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IEnumDefinitionIdentity_Clone_Proxy( 
    IEnumDefinitionIdentity * This,
    /* [out] */ IEnumDefinitionIdentity **ppIEnumDefinitionIdentity);


void __RPC_STUB IEnumDefinitionIdentity_Clone_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);



#endif 	/* __IEnumDefinitionIdentity_INTERFACE_DEFINED__ */


#ifndef __IEnumReferenceIdentity_INTERFACE_DEFINED__
#define __IEnumReferenceIdentity_INTERFACE_DEFINED__

/* interface IEnumReferenceIdentity */
/* [local][unique][uuid][object] */ 


EXTERN_C const IID IID_IEnumReferenceIdentity;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("b30352cf-23da-4577-9b3f-b4e6573be53b")
    IEnumReferenceIdentity : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE Next( 
            /* [in] */ ULONG celt,
            /* [length_is][size_is][out] */ IReferenceIdentity **prgpIReferenceIdentity,
            /* [out] */ ULONG *pceltWritten) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE Skip( 
            ULONG celt) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE Reset( void) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE Clone( 
            IEnumReferenceIdentity **ppIEnumReferenceIdentity) = 0;
        
    };
    
#else 	/* C style interface */

    typedef struct IEnumReferenceIdentityVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            IEnumReferenceIdentity * This,
            /* [in] */ REFIID riid,
            /* [iid_is][out] */ void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            IEnumReferenceIdentity * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            IEnumReferenceIdentity * This);
        
        HRESULT ( STDMETHODCALLTYPE *Next )( 
            IEnumReferenceIdentity * This,
            /* [in] */ ULONG celt,
            /* [length_is][size_is][out] */ IReferenceIdentity **prgpIReferenceIdentity,
            /* [out] */ ULONG *pceltWritten);
        
        HRESULT ( STDMETHODCALLTYPE *Skip )( 
            IEnumReferenceIdentity * This,
            ULONG celt);
        
        HRESULT ( STDMETHODCALLTYPE *Reset )( 
            IEnumReferenceIdentity * This);
        
        HRESULT ( STDMETHODCALLTYPE *Clone )( 
            IEnumReferenceIdentity * This,
            IEnumReferenceIdentity **ppIEnumReferenceIdentity);
        
        END_INTERFACE
    } IEnumReferenceIdentityVtbl;

    interface IEnumReferenceIdentity
    {
        CONST_VTBL struct IEnumReferenceIdentityVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define IEnumReferenceIdentity_QueryInterface(This,riid,ppvObject)	\
    (This)->lpVtbl -> QueryInterface(This,riid,ppvObject)

#define IEnumReferenceIdentity_AddRef(This)	\
    (This)->lpVtbl -> AddRef(This)

#define IEnumReferenceIdentity_Release(This)	\
    (This)->lpVtbl -> Release(This)


#define IEnumReferenceIdentity_Next(This,celt,prgpIReferenceIdentity,pceltWritten)	\
    (This)->lpVtbl -> Next(This,celt,prgpIReferenceIdentity,pceltWritten)

#define IEnumReferenceIdentity_Skip(This,celt)	\
    (This)->lpVtbl -> Skip(This,celt)

#define IEnumReferenceIdentity_Reset(This)	\
    (This)->lpVtbl -> Reset(This)

#define IEnumReferenceIdentity_Clone(This,ppIEnumReferenceIdentity)	\
    (This)->lpVtbl -> Clone(This,ppIEnumReferenceIdentity)

#endif /* COBJMACROS */


#endif 	/* C style interface */



HRESULT STDMETHODCALLTYPE IEnumReferenceIdentity_Next_Proxy( 
    IEnumReferenceIdentity * This,
    /* [in] */ ULONG celt,
    /* [length_is][size_is][out] */ IReferenceIdentity **prgpIReferenceIdentity,
    /* [out] */ ULONG *pceltWritten);


void __RPC_STUB IEnumReferenceIdentity_Next_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IEnumReferenceIdentity_Skip_Proxy( 
    IEnumReferenceIdentity * This,
    ULONG celt);


void __RPC_STUB IEnumReferenceIdentity_Skip_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IEnumReferenceIdentity_Reset_Proxy( 
    IEnumReferenceIdentity * This);


void __RPC_STUB IEnumReferenceIdentity_Reset_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IEnumReferenceIdentity_Clone_Proxy( 
    IEnumReferenceIdentity * This,
    IEnumReferenceIdentity **ppIEnumReferenceIdentity);


void __RPC_STUB IEnumReferenceIdentity_Clone_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);



#endif 	/* __IEnumReferenceIdentity_INTERFACE_DEFINED__ */


#ifndef __IDefinitionAppId_INTERFACE_DEFINED__
#define __IDefinitionAppId_INTERFACE_DEFINED__

/* interface IDefinitionAppId */
/* [local][unique][uuid][object] */ 


EXTERN_C const IID IID_IDefinitionAppId;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("d91e12d8-98ed-47fa-9936-39421283d59b")
    IDefinitionAppId : public IUnknown
    {
    public:
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_SubscriptionId( 
            /* [retval][out] */ LPWSTR *ppszSubscription) = 0;
        
        virtual /* [propput] */ HRESULT STDMETHODCALLTYPE put_SubscriptionId( 
            /* [in] */ LPCWSTR pszSubscription) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_Codebase( 
            /* [retval][out] */ LPWSTR *ppszCodebase) = 0;
        
        virtual /* [propput] */ HRESULT STDMETHODCALLTYPE put_Codebase( 
            /* [in] */ LPCWSTR pszCodebase) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE EnumAppPath( 
            /* [out] */ IEnumDefinitionIdentity **ppIEnumDefinitionIdentity) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE SetAppPath( 
            /* [in] */ ULONG cIDefinitionIdentity,
            /* [size_is][in] */ IDefinitionIdentity *rgIDefinitionIdentity[  ]) = 0;
        
    };
    
#else 	/* C style interface */

    typedef struct IDefinitionAppIdVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            IDefinitionAppId * This,
            /* [in] */ REFIID riid,
            /* [iid_is][out] */ void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            IDefinitionAppId * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            IDefinitionAppId * This);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_SubscriptionId )( 
            IDefinitionAppId * This,
            /* [retval][out] */ LPWSTR *ppszSubscription);
        
        /* [propput] */ HRESULT ( STDMETHODCALLTYPE *put_SubscriptionId )( 
            IDefinitionAppId * This,
            /* [in] */ LPCWSTR pszSubscription);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_Codebase )( 
            IDefinitionAppId * This,
            /* [retval][out] */ LPWSTR *ppszCodebase);
        
        /* [propput] */ HRESULT ( STDMETHODCALLTYPE *put_Codebase )( 
            IDefinitionAppId * This,
            /* [in] */ LPCWSTR pszCodebase);
        
        HRESULT ( STDMETHODCALLTYPE *EnumAppPath )( 
            IDefinitionAppId * This,
            /* [out] */ IEnumDefinitionIdentity **ppIEnumDefinitionIdentity);
        
        HRESULT ( STDMETHODCALLTYPE *SetAppPath )( 
            IDefinitionAppId * This,
            /* [in] */ ULONG cIDefinitionIdentity,
            /* [size_is][in] */ IDefinitionIdentity *rgIDefinitionIdentity[  ]);
        
        END_INTERFACE
    } IDefinitionAppIdVtbl;

    interface IDefinitionAppId
    {
        CONST_VTBL struct IDefinitionAppIdVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define IDefinitionAppId_QueryInterface(This,riid,ppvObject)	\
    (This)->lpVtbl -> QueryInterface(This,riid,ppvObject)

#define IDefinitionAppId_AddRef(This)	\
    (This)->lpVtbl -> AddRef(This)

#define IDefinitionAppId_Release(This)	\
    (This)->lpVtbl -> Release(This)


#define IDefinitionAppId_get_SubscriptionId(This,ppszSubscription)	\
    (This)->lpVtbl -> get_SubscriptionId(This,ppszSubscription)

#define IDefinitionAppId_put_SubscriptionId(This,pszSubscription)	\
    (This)->lpVtbl -> put_SubscriptionId(This,pszSubscription)

#define IDefinitionAppId_get_Codebase(This,ppszCodebase)	\
    (This)->lpVtbl -> get_Codebase(This,ppszCodebase)

#define IDefinitionAppId_put_Codebase(This,pszCodebase)	\
    (This)->lpVtbl -> put_Codebase(This,pszCodebase)

#define IDefinitionAppId_EnumAppPath(This,ppIEnumDefinitionIdentity)	\
    (This)->lpVtbl -> EnumAppPath(This,ppIEnumDefinitionIdentity)

#define IDefinitionAppId_SetAppPath(This,cIDefinitionIdentity,rgIDefinitionIdentity)	\
    (This)->lpVtbl -> SetAppPath(This,cIDefinitionIdentity,rgIDefinitionIdentity)

#endif /* COBJMACROS */


#endif 	/* C style interface */



/* [propget] */ HRESULT STDMETHODCALLTYPE IDefinitionAppId_get_SubscriptionId_Proxy( 
    IDefinitionAppId * This,
    /* [retval][out] */ LPWSTR *ppszSubscription);


void __RPC_STUB IDefinitionAppId_get_SubscriptionId_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


/* [propput] */ HRESULT STDMETHODCALLTYPE IDefinitionAppId_put_SubscriptionId_Proxy( 
    IDefinitionAppId * This,
    /* [in] */ LPCWSTR pszSubscription);


void __RPC_STUB IDefinitionAppId_put_SubscriptionId_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IDefinitionAppId_get_Codebase_Proxy( 
    IDefinitionAppId * This,
    /* [retval][out] */ _Outptr_result_maybenull_ LPWSTR *ppszCodebase);


void __RPC_STUB IDefinitionAppId_get_Codebase_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


/* [propput] */ HRESULT STDMETHODCALLTYPE IDefinitionAppId_put_Codebase_Proxy( 
    IDefinitionAppId * This,
    /* [in] */ LPCWSTR pszCodebase);


void __RPC_STUB IDefinitionAppId_put_Codebase_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IDefinitionAppId_EnumAppPath_Proxy( 
    IDefinitionAppId * This,
    /* [out] */ IEnumDefinitionIdentity **ppIEnumDefinitionIdentity);


void __RPC_STUB IDefinitionAppId_EnumAppPath_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IDefinitionAppId_SetAppPath_Proxy( 
    IDefinitionAppId * This,
    /* [in] */ ULONG cIDefinitionIdentity,
    /* [size_is][in] */ IDefinitionIdentity *rgIDefinitionIdentity[  ]);


void __RPC_STUB IDefinitionAppId_SetAppPath_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);



#endif 	/* __IDefinitionAppId_INTERFACE_DEFINED__ */


#ifndef __IReferenceAppId_INTERFACE_DEFINED__
#define __IReferenceAppId_INTERFACE_DEFINED__

/* interface IReferenceAppId */
/* [local][unique][uuid][object] */ 


EXTERN_C const IID IID_IReferenceAppId;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("054f0bef-9e45-4363-8f5a-2f8e142d9a3b")
    IReferenceAppId : public IUnknown
    {
    public:
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_SubscriptionId( 
            /* [retval][out] */ LPWSTR *ppszSubscription) = 0;
        
        virtual /* [propput] */ HRESULT STDMETHODCALLTYPE put_SubscriptionId( 
            /* [in] */ LPCWSTR pszSubscription) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_Codebase( 
            /* [retval][out] */ LPWSTR *ppszCodebase) = 0;
        
        virtual /* [propput] */ HRESULT STDMETHODCALLTYPE put_Codebase( 
            /* [in] */ LPCWSTR pszCodebase) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE EnumAppPath( 
            /* [out] */ IEnumReferenceIdentity **ppIReferenceAppId) = 0;
        
    };
    
#else 	/* C style interface */

    typedef struct IReferenceAppIdVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            IReferenceAppId * This,
            /* [in] */ REFIID riid,
            /* [iid_is][out] */ void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            IReferenceAppId * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            IReferenceAppId * This);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_SubscriptionId )( 
            IReferenceAppId * This,
            /* [retval][out] */ LPWSTR *ppszSubscription);
        
        /* [propput] */ HRESULT ( STDMETHODCALLTYPE *put_SubscriptionId )( 
            IReferenceAppId * This,
            /* [in] */ LPCWSTR pszSubscription);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_Codebase )( 
            IReferenceAppId * This,
            /* [retval][out] */ LPWSTR *ppszCodebase);
        
        /* [propput] */ HRESULT ( STDMETHODCALLTYPE *put_Codebase )( 
            IReferenceAppId * This,
            /* [in] */ LPCWSTR pszCodebase);
        
        HRESULT ( STDMETHODCALLTYPE *EnumAppPath )( 
            IReferenceAppId * This,
            /* [out] */ IEnumReferenceIdentity **ppIReferenceAppId);
        
        END_INTERFACE
    } IReferenceAppIdVtbl;

    interface IReferenceAppId
    {
        CONST_VTBL struct IReferenceAppIdVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define IReferenceAppId_QueryInterface(This,riid,ppvObject)	\
    (This)->lpVtbl -> QueryInterface(This,riid,ppvObject)

#define IReferenceAppId_AddRef(This)	\
    (This)->lpVtbl -> AddRef(This)

#define IReferenceAppId_Release(This)	\
    (This)->lpVtbl -> Release(This)


#define IReferenceAppId_get_SubscriptionId(This,ppszSubscription)	\
    (This)->lpVtbl -> get_SubscriptionId(This,ppszSubscription)

#define IReferenceAppId_put_SubscriptionId(This,pszSubscription)	\
    (This)->lpVtbl -> put_SubscriptionId(This,pszSubscription)

#define IReferenceAppId_get_Codebase(This,ppszCodebase)	\
    (This)->lpVtbl -> get_Codebase(This,ppszCodebase)

#define IReferenceAppId_put_Codebase(This,pszCodebase)	\
    (This)->lpVtbl -> put_Codebase(This,pszCodebase)

#define IReferenceAppId_EnumAppPath(This,ppIReferenceAppId)	\
    (This)->lpVtbl -> EnumAppPath(This,ppIReferenceAppId)

#endif /* COBJMACROS */


#endif 	/* C style interface */



/* [propget] */ HRESULT STDMETHODCALLTYPE IReferenceAppId_get_SubscriptionId_Proxy( 
    IReferenceAppId * This,
    /* [retval][out] */ LPWSTR *ppszSubscription);


void __RPC_STUB IReferenceAppId_get_SubscriptionId_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


/* [propput] */ HRESULT STDMETHODCALLTYPE IReferenceAppId_put_SubscriptionId_Proxy( 
    IReferenceAppId * This,
    /* [in] */ LPCWSTR pszSubscription);


void __RPC_STUB IReferenceAppId_put_SubscriptionId_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IReferenceAppId_get_Codebase_Proxy( 
    IReferenceAppId * This,
    /* [retval][out] */ _Outptr_result_maybenull_ LPWSTR *ppszCodebase);


void __RPC_STUB IReferenceAppId_get_Codebase_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


/* [propput] */ HRESULT STDMETHODCALLTYPE IReferenceAppId_put_Codebase_Proxy( 
    IReferenceAppId * This,
    /* [in] */ LPCWSTR pszCodebase);


void __RPC_STUB IReferenceAppId_put_Codebase_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IReferenceAppId_EnumAppPath_Proxy( 
    IReferenceAppId * This,
    /* [out] */ IEnumReferenceIdentity **ppIReferenceAppId);


void __RPC_STUB IReferenceAppId_EnumAppPath_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);



#endif 	/* __IReferenceAppId_INTERFACE_DEFINED__ */


#ifndef __IIdentityAuthority_INTERFACE_DEFINED__
#define __IIdentityAuthority_INTERFACE_DEFINED__

/* interface IIdentityAuthority */
/* [local][unique][uuid][object] */ 

/* [v1_enum] */ 
enum _TEXT_TO_DEFINITION_IDENTITY_FLAGS
    {	TEXT_TO_DEFINITION_IDENTITY_FLAG_ALLOW_UNKNOWN_ATTRIBUTES_IN_NULL_NAMESPACE	= 0x1
    } ;
/* [v1_enum] */ 
enum _TEXT_TO_REFERENCE_IDENTITY_FLAGS
    {	TEXT_TO_REFERENCE_IDENTITY_FLAG_ALLOW_UNKNOWN_ATTRIBUTES_IN_NULL_NAMESPACE	= 0x1
    } ;
/* [v1_enum] */ 
enum _DEFINITION_IDENTITY_TO_TEXT_FLAGS
    {	DEFINITION_IDENTITY_TO_TEXT_FLAG_CANONICAL	= 0x1
    } ;
/* [v1_enum] */ 
enum _REFERENCE_IDENTITY_TO_TEXT_FLAGS
    {	REFERENCE_IDENTITY_TO_TEXT_FLAG_CANONICAL	= 0x1
    } ;
/* [v1_enum] */ 
enum _IIDENTITYAUTHORITY_DOES_DEFINITION_MATCH_REFERENCE_FLAGS
    {	IIDENTITYAUTHORITY_DOES_DEFINITION_MATCH_REFERENCE_FLAG_EXACT_MATCH_REQUIRED	= 0x1
    } ;
/* [v1_enum] */ 
enum _IIDENTITYAUTHORITY_DOES_TEXTUAL_DEFINITION_MATCH_TEXTUAL_REFERENCE_FLAGS
    {	IIDENTITYAUTHORITY_DOES_TEXTUAL_DEFINITION_MATCH_TEXTUAL_REFERENCE_FLAG_EXACT_MATCH_REQUIRED	= 0x1
    } ;

EXTERN_C const IID IID_IIdentityAuthority;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("261a6983-c35d-4d0d-aa5b-7867259e77bc")
    IIdentityAuthority : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE TextToDefinition( 
            /* [in] */ DWORD dwFlags,
            /* [in] */ LPCWSTR pszIdentity,
            /* [out] */ IDefinitionIdentity **ppIDefinitionIdentity) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE TextToReference( 
            /* [in] */ DWORD dwFlags,
            /* [in] */ LPCWSTR pszIdentity,
            /* [out] */ IReferenceIdentity **ppIReferenceIdentity) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE DefinitionToText( 
            /* [in] */ DWORD dwFlags,
            /* [in] */ IDefinitionIdentity *pIDefinitionIdentity,
            /* [out] */ LPWSTR *ppszFormattedIdentity) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE DefinitionToTextBuffer( 
            /* [in] */ DWORD dwFlags,
            /* [in] */ IDefinitionIdentity *pIDefinitionIdentity,
            /* [in] */ ULONG cchBufferSize,
            /* [length_is][size_is][out][in] */ WCHAR wchBuffer[  ],
            /* [out] */ ULONG *pcchBufferRequired) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE ReferenceToText( 
            /* [in] */ DWORD dwFlags,
            /* [in] */ IReferenceIdentity *pIReferenceIdentity,
            /* [out] */ LPWSTR *ppszFormattedIdentity) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE ReferenceToTextBuffer( 
            /* [in] */ DWORD dwFlags,
            /* [in] */ IReferenceIdentity *pIReferenceIdentity,
            /* [in] */ ULONG cchBufferSize,
            /* [length_is][size_is][out][in] */ WCHAR wchBuffer[  ],
            /* [out] */ ULONG *pcchBufferRequired) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE AreDefinitionsEqual( 
            /* [in] */ DWORD dwFlags,
            /* [in] */ IDefinitionIdentity *pDefinition1,
            /* [in] */ IDefinitionIdentity *pDefinition2,
            /* [out] */ BOOL *pfEqual) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE AreReferencesEqual( 
            /* [in] */ DWORD dwFlags,
            /* [in] */ IReferenceIdentity *pReference1,
            /* [in] */ IReferenceIdentity *pReference2,
            /* [out] */ BOOL *pfEqual) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE AreTextualDefinitionsEqual( 
            /* [in] */ DWORD dwFlags,
            /* [in] */ LPCWSTR pszIdentityLeft,
            /* [in] */ LPCWSTR pszIdentityRight,
            /* [out] */ BOOL *pfEqual) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE AreTextualReferencesEqual( 
            /* [in] */ DWORD dwFlags,
            /* [in] */ LPCWSTR pszIdentityLeft,
            /* [in] */ LPCWSTR pszIdentityRight,
            /* [out] */ BOOL *pfEqual) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE DoesDefinitionMatchReference( 
            /* [in] */ DWORD dwFlags,
            /* [in] */ IDefinitionIdentity *pIDefinitionIdentity,
            /* [in] */ IReferenceIdentity *pIReferenceIdentity,
            /* [out] */ BOOL *pfMatches) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE DoesTextualDefinitionMatchTextualReference( 
            /* [in] */ DWORD dwFlags,
            /* [in] */ LPCWSTR pszDefinition,
            /* [in] */ LPCWSTR pszReference,
            /* [out] */ BOOL *pfMatches) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE HashReference( 
            /* [in] */ DWORD dwFlags,
            /* [in] */ IReferenceIdentity *pIReferenceIdentity,
            /* [out] */ ULONGLONG *pullPseudoKey) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE HashDefinition( 
            /* [in] */ DWORD dwFlags,
            /* [in] */ IDefinitionIdentity *pIDefinitionIdentity,
            /* [out] */ ULONGLONG *pullPseudoKey) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GenerateDefinitionKey( 
            /* [in] */ DWORD dwFlags,
            /* [in] */ IDefinitionIdentity *pIDefinitionIdentity,
            /* [out] */ LPWSTR *ppszKeyForm) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GenerateReferenceKey( 
            /* [in] */ DWORD dwFlags,
            /* [in] */ IReferenceIdentity *pIReferenceIdentity,
            /* [out] */ LPWSTR *ppszKeyForm) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE CreateDefinition( 
            /* [retval][out] */ IDefinitionIdentity **ppNewIdentity) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE CreateReference( 
            /* [retval][out] */ IReferenceIdentity **ppNewIdentity) = 0;
        
    };
    
#else 	/* C style interface */

    typedef struct IIdentityAuthorityVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            IIdentityAuthority * This,
            /* [in] */ REFIID riid,
            /* [iid_is][out] */ void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            IIdentityAuthority * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            IIdentityAuthority * This);
        
        HRESULT ( STDMETHODCALLTYPE *TextToDefinition )( 
            IIdentityAuthority * This,
            /* [in] */ DWORD dwFlags,
            /* [in] */ LPCWSTR pszIdentity,
            /* [out] */ IDefinitionIdentity **ppIDefinitionIdentity);
        
        HRESULT ( STDMETHODCALLTYPE *TextToReference )( 
            IIdentityAuthority * This,
            /* [in] */ DWORD dwFlags,
            /* [in] */ LPCWSTR pszIdentity,
            /* [out] */ IReferenceIdentity **ppIReferenceIdentity);
        
        HRESULT ( STDMETHODCALLTYPE *DefinitionToText )( 
            IIdentityAuthority * This,
            /* [in] */ DWORD dwFlags,
            /* [in] */ IDefinitionIdentity *pIDefinitionIdentity,
            /* [out] */ LPWSTR *ppszFormattedIdentity);
        
        HRESULT ( STDMETHODCALLTYPE *DefinitionToTextBuffer )( 
            IIdentityAuthority * This,
            /* [in] */ DWORD dwFlags,
            /* [in] */ IDefinitionIdentity *pIDefinitionIdentity,
            /* [in] */ ULONG cchBufferSize,
            /* [length_is][size_is][out][in] */ WCHAR wchBuffer[  ],
            /* [out] */ ULONG *pcchBufferRequired);
        
        HRESULT ( STDMETHODCALLTYPE *ReferenceToText )( 
            IIdentityAuthority * This,
            /* [in] */ DWORD dwFlags,
            /* [in] */ IReferenceIdentity *pIReferenceIdentity,
            /* [out] */ LPWSTR *ppszFormattedIdentity);
        
        HRESULT ( STDMETHODCALLTYPE *ReferenceToTextBuffer )( 
            IIdentityAuthority * This,
            /* [in] */ DWORD dwFlags,
            /* [in] */ IReferenceIdentity *pIReferenceIdentity,
            /* [in] */ ULONG cchBufferSize,
            /* [length_is][size_is][out][in] */ WCHAR wchBuffer[  ],
            /* [out] */ ULONG *pcchBufferRequired);
        
        HRESULT ( STDMETHODCALLTYPE *AreDefinitionsEqual )( 
            IIdentityAuthority * This,
            /* [in] */ DWORD dwFlags,
            /* [in] */ IDefinitionIdentity *pDefinition1,
            /* [in] */ IDefinitionIdentity *pDefinition2,
            /* [out] */ BOOL *pfEqual);
        
        HRESULT ( STDMETHODCALLTYPE *AreReferencesEqual )( 
            IIdentityAuthority * This,
            /* [in] */ DWORD dwFlags,
            /* [in] */ IReferenceIdentity *pReference1,
            /* [in] */ IReferenceIdentity *pReference2,
            /* [out] */ BOOL *pfEqual);
        
        HRESULT ( STDMETHODCALLTYPE *AreTextualDefinitionsEqual )( 
            IIdentityAuthority * This,
            /* [in] */ DWORD dwFlags,
            /* [in] */ LPCWSTR pszIdentityLeft,
            /* [in] */ LPCWSTR pszIdentityRight,
            /* [out] */ BOOL *pfEqual);
        
        HRESULT ( STDMETHODCALLTYPE *AreTextualReferencesEqual )( 
            IIdentityAuthority * This,
            /* [in] */ DWORD dwFlags,
            /* [in] */ LPCWSTR pszIdentityLeft,
            /* [in] */ LPCWSTR pszIdentityRight,
            /* [out] */ BOOL *pfEqual);
        
        HRESULT ( STDMETHODCALLTYPE *DoesDefinitionMatchReference )( 
            IIdentityAuthority * This,
            /* [in] */ DWORD dwFlags,
            /* [in] */ IDefinitionIdentity *pIDefinitionIdentity,
            /* [in] */ IReferenceIdentity *pIReferenceIdentity,
            /* [out] */ BOOL *pfMatches);
        
        HRESULT ( STDMETHODCALLTYPE *DoesTextualDefinitionMatchTextualReference )( 
            IIdentityAuthority * This,
            /* [in] */ DWORD dwFlags,
            /* [in] */ LPCWSTR pszDefinition,
            /* [in] */ LPCWSTR pszReference,
            /* [out] */ BOOL *pfMatches);
        
        HRESULT ( STDMETHODCALLTYPE *HashReference )( 
            IIdentityAuthority * This,
            /* [in] */ DWORD dwFlags,
            /* [in] */ IReferenceIdentity *pIReferenceIdentity,
            /* [out] */ ULONGLONG *pullPseudoKey);
        
        HRESULT ( STDMETHODCALLTYPE *HashDefinition )( 
            IIdentityAuthority * This,
            /* [in] */ DWORD dwFlags,
            /* [in] */ IDefinitionIdentity *pIDefinitionIdentity,
            /* [out] */ ULONGLONG *pullPseudoKey);
        
        HRESULT ( STDMETHODCALLTYPE *GenerateDefinitionKey )( 
            IIdentityAuthority * This,
            /* [in] */ DWORD dwFlags,
            /* [in] */ IDefinitionIdentity *pIDefinitionIdentity,
            /* [out] */ LPWSTR *ppszKeyForm);
        
        HRESULT ( STDMETHODCALLTYPE *GenerateReferenceKey )( 
            IIdentityAuthority * This,
            /* [in] */ DWORD dwFlags,
            /* [in] */ IReferenceIdentity *pIReferenceIdentity,
            /* [out] */ LPWSTR *ppszKeyForm);
        
        HRESULT ( STDMETHODCALLTYPE *CreateDefinition )( 
            IIdentityAuthority * This,
            /* [retval][out] */ IDefinitionIdentity **ppNewIdentity);
        
        HRESULT ( STDMETHODCALLTYPE *CreateReference )( 
            IIdentityAuthority * This,
            /* [retval][out] */ IReferenceIdentity **ppNewIdentity);
        
        END_INTERFACE
    } IIdentityAuthorityVtbl;

    interface IIdentityAuthority
    {
        CONST_VTBL struct IIdentityAuthorityVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define IIdentityAuthority_QueryInterface(This,riid,ppvObject)	\
    (This)->lpVtbl -> QueryInterface(This,riid,ppvObject)

#define IIdentityAuthority_AddRef(This)	\
    (This)->lpVtbl -> AddRef(This)

#define IIdentityAuthority_Release(This)	\
    (This)->lpVtbl -> Release(This)


#define IIdentityAuthority_TextToDefinition(This,dwFlags,pszIdentity,ppIDefinitionIdentity)	\
    (This)->lpVtbl -> TextToDefinition(This,dwFlags,pszIdentity,ppIDefinitionIdentity)

#define IIdentityAuthority_TextToReference(This,dwFlags,pszIdentity,ppIReferenceIdentity)	\
    (This)->lpVtbl -> TextToReference(This,dwFlags,pszIdentity,ppIReferenceIdentity)

#define IIdentityAuthority_DefinitionToText(This,dwFlags,pIDefinitionIdentity,ppszFormattedIdentity)	\
    (This)->lpVtbl -> DefinitionToText(This,dwFlags,pIDefinitionIdentity,ppszFormattedIdentity)

#define IIdentityAuthority_DefinitionToTextBuffer(This,dwFlags,pIDefinitionIdentity,cchBufferSize,wchBuffer,pcchBufferRequired)	\
    (This)->lpVtbl -> DefinitionToTextBuffer(This,dwFlags,pIDefinitionIdentity,cchBufferSize,wchBuffer,pcchBufferRequired)

#define IIdentityAuthority_ReferenceToText(This,dwFlags,pIReferenceIdentity,ppszFormattedIdentity)	\
    (This)->lpVtbl -> ReferenceToText(This,dwFlags,pIReferenceIdentity,ppszFormattedIdentity)

#define IIdentityAuthority_ReferenceToTextBuffer(This,dwFlags,pIReferenceIdentity,cchBufferSize,wchBuffer,pcchBufferRequired)	\
    (This)->lpVtbl -> ReferenceToTextBuffer(This,dwFlags,pIReferenceIdentity,cchBufferSize,wchBuffer,pcchBufferRequired)

#define IIdentityAuthority_AreDefinitionsEqual(This,dwFlags,pDefinition1,pDefinition2,pfEqual)	\
    (This)->lpVtbl -> AreDefinitionsEqual(This,dwFlags,pDefinition1,pDefinition2,pfEqual)

#define IIdentityAuthority_AreReferencesEqual(This,dwFlags,pReference1,pReference2,pfEqual)	\
    (This)->lpVtbl -> AreReferencesEqual(This,dwFlags,pReference1,pReference2,pfEqual)

#define IIdentityAuthority_AreTextualDefinitionsEqual(This,dwFlags,pszIdentityLeft,pszIdentityRight,pfEqual)	\
    (This)->lpVtbl -> AreTextualDefinitionsEqual(This,dwFlags,pszIdentityLeft,pszIdentityRight,pfEqual)

#define IIdentityAuthority_AreTextualReferencesEqual(This,dwFlags,pszIdentityLeft,pszIdentityRight,pfEqual)	\
    (This)->lpVtbl -> AreTextualReferencesEqual(This,dwFlags,pszIdentityLeft,pszIdentityRight,pfEqual)

#define IIdentityAuthority_DoesDefinitionMatchReference(This,dwFlags,pIDefinitionIdentity,pIReferenceIdentity,pfMatches)	\
    (This)->lpVtbl -> DoesDefinitionMatchReference(This,dwFlags,pIDefinitionIdentity,pIReferenceIdentity,pfMatches)

#define IIdentityAuthority_DoesTextualDefinitionMatchTextualReference(This,dwFlags,pszDefinition,pszReference,pfMatches)	\
    (This)->lpVtbl -> DoesTextualDefinitionMatchTextualReference(This,dwFlags,pszDefinition,pszReference,pfMatches)

#define IIdentityAuthority_HashReference(This,dwFlags,pIReferenceIdentity,pullPseudoKey)	\
    (This)->lpVtbl -> HashReference(This,dwFlags,pIReferenceIdentity,pullPseudoKey)

#define IIdentityAuthority_HashDefinition(This,dwFlags,pIDefinitionIdentity,pullPseudoKey)	\
    (This)->lpVtbl -> HashDefinition(This,dwFlags,pIDefinitionIdentity,pullPseudoKey)

#define IIdentityAuthority_GenerateDefinitionKey(This,dwFlags,pIDefinitionIdentity,ppszKeyForm)	\
    (This)->lpVtbl -> GenerateDefinitionKey(This,dwFlags,pIDefinitionIdentity,ppszKeyForm)

#define IIdentityAuthority_GenerateReferenceKey(This,dwFlags,pIReferenceIdentity,ppszKeyForm)	\
    (This)->lpVtbl -> GenerateReferenceKey(This,dwFlags,pIReferenceIdentity,ppszKeyForm)

#define IIdentityAuthority_CreateDefinition(This,ppNewIdentity)	\
    (This)->lpVtbl -> CreateDefinition(This,ppNewIdentity)

#define IIdentityAuthority_CreateReference(This,ppNewIdentity)	\
    (This)->lpVtbl -> CreateReference(This,ppNewIdentity)

#endif /* COBJMACROS */


#endif 	/* C style interface */



HRESULT STDMETHODCALLTYPE IIdentityAuthority_TextToDefinition_Proxy( 
    IIdentityAuthority * This,
    /* [in] */ DWORD dwFlags,
    /* [in] */ LPCWSTR pszIdentity,
    /* [out] */ IDefinitionIdentity **ppIDefinitionIdentity);


void __RPC_STUB IIdentityAuthority_TextToDefinition_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IIdentityAuthority_TextToReference_Proxy( 
    IIdentityAuthority * This,
    /* [in] */ DWORD dwFlags,
    /* [in] */ LPCWSTR pszIdentity,
    /* [out] */ IReferenceIdentity **ppIReferenceIdentity);


void __RPC_STUB IIdentityAuthority_TextToReference_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IIdentityAuthority_DefinitionToText_Proxy( 
    IIdentityAuthority * This,
    /* [in] */ DWORD dwFlags,
    /* [in] */ IDefinitionIdentity *pIDefinitionIdentity,
    /* [out] */ _Outptr_result_maybenull_ LPWSTR *ppszFormattedIdentity);


void __RPC_STUB IIdentityAuthority_DefinitionToText_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IIdentityAuthority_DefinitionToTextBuffer_Proxy( 
    IIdentityAuthority * This,
    /* [in] */ DWORD dwFlags,
    /* [in] */ IDefinitionIdentity *pIDefinitionIdentity,
    /* [in] */ ULONG cchBufferSize,
    /* [length_is][size_is][out][in] */ WCHAR wchBuffer[  ],
    /* [out] */ ULONG *pcchBufferRequired);


void __RPC_STUB IIdentityAuthority_DefinitionToTextBuffer_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IIdentityAuthority_ReferenceToText_Proxy( 
    IIdentityAuthority * This,
    /* [in] */ DWORD dwFlags,
    /* [in] */ IReferenceIdentity *pIReferenceIdentity,
    /* [out] */ _Outptr_result_maybenull_ LPWSTR *ppszFormattedIdentity);


void __RPC_STUB IIdentityAuthority_ReferenceToText_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IIdentityAuthority_ReferenceToTextBuffer_Proxy( 
    IIdentityAuthority * This,
    /* [in] */ DWORD dwFlags,
    /* [in] */ IReferenceIdentity *pIReferenceIdentity,
    /* [in] */ ULONG cchBufferSize,
    /* [length_is][size_is][out][in] */ WCHAR wchBuffer[  ],
    /* [out] */ ULONG *pcchBufferRequired);


void __RPC_STUB IIdentityAuthority_ReferenceToTextBuffer_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IIdentityAuthority_AreDefinitionsEqual_Proxy( 
    IIdentityAuthority * This,
    /* [in] */ DWORD dwFlags,
    /* [in] */ IDefinitionIdentity *pDefinition1,
    /* [in] */ IDefinitionIdentity *pDefinition2,
    /* [out] */ BOOL *pfEqual);


void __RPC_STUB IIdentityAuthority_AreDefinitionsEqual_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IIdentityAuthority_AreReferencesEqual_Proxy( 
    IIdentityAuthority * This,
    /* [in] */ DWORD dwFlags,
    /* [in] */ IReferenceIdentity *pReference1,
    /* [in] */ IReferenceIdentity *pReference2,
    /* [out] */ BOOL *pfEqual);


void __RPC_STUB IIdentityAuthority_AreReferencesEqual_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IIdentityAuthority_AreTextualDefinitionsEqual_Proxy( 
    IIdentityAuthority * This,
    /* [in] */ DWORD dwFlags,
    /* [in] */ LPCWSTR pszIdentityLeft,
    /* [in] */ LPCWSTR pszIdentityRight,
    /* [out] */ BOOL *pfEqual);


void __RPC_STUB IIdentityAuthority_AreTextualDefinitionsEqual_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IIdentityAuthority_AreTextualReferencesEqual_Proxy( 
    IIdentityAuthority * This,
    /* [in] */ DWORD dwFlags,
    /* [in] */ LPCWSTR pszIdentityLeft,
    /* [in] */ LPCWSTR pszIdentityRight,
    /* [out] */ BOOL *pfEqual);


void __RPC_STUB IIdentityAuthority_AreTextualReferencesEqual_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IIdentityAuthority_DoesDefinitionMatchReference_Proxy( 
    IIdentityAuthority * This,
    /* [in] */ DWORD dwFlags,
    /* [in] */ IDefinitionIdentity *pIDefinitionIdentity,
    /* [in] */ IReferenceIdentity *pIReferenceIdentity,
    /* [out] */ BOOL *pfMatches);


void __RPC_STUB IIdentityAuthority_DoesDefinitionMatchReference_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IIdentityAuthority_DoesTextualDefinitionMatchTextualReference_Proxy( 
    IIdentityAuthority * This,
    /* [in] */ DWORD dwFlags,
    /* [in] */ LPCWSTR pszDefinition,
    /* [in] */ LPCWSTR pszReference,
    /* [out] */ BOOL *pfMatches);


void __RPC_STUB IIdentityAuthority_DoesTextualDefinitionMatchTextualReference_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IIdentityAuthority_HashReference_Proxy( 
    IIdentityAuthority * This,
    /* [in] */ DWORD dwFlags,
    /* [in] */ IReferenceIdentity *pIReferenceIdentity,
    /* [out] */ ULONGLONG *pullPseudoKey);


void __RPC_STUB IIdentityAuthority_HashReference_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IIdentityAuthority_HashDefinition_Proxy( 
    IIdentityAuthority * This,
    /* [in] */ DWORD dwFlags,
    /* [in] */ IDefinitionIdentity *pIDefinitionIdentity,
    /* [out] */ ULONGLONG *pullPseudoKey);


void __RPC_STUB IIdentityAuthority_HashDefinition_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IIdentityAuthority_GenerateDefinitionKey_Proxy( 
    IIdentityAuthority * This,
    /* [in] */ DWORD dwFlags,
    /* [in] */ IDefinitionIdentity *pIDefinitionIdentity,
    /* [out] */ _Outptr_result_maybenull_ LPWSTR *ppszKeyForm);


void __RPC_STUB IIdentityAuthority_GenerateDefinitionKey_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IIdentityAuthority_GenerateReferenceKey_Proxy( 
    IIdentityAuthority * This,
    /* [in] */ DWORD dwFlags,
    /* [in] */ IReferenceIdentity *pIReferenceIdentity,
    /* [out] */ _Outptr_result_maybenull_ LPWSTR *ppszKeyForm);


void __RPC_STUB IIdentityAuthority_GenerateReferenceKey_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IIdentityAuthority_CreateDefinition_Proxy( 
    IIdentityAuthority * This,
    /* [retval][out] */ IDefinitionIdentity **ppNewIdentity);


void __RPC_STUB IIdentityAuthority_CreateDefinition_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IIdentityAuthority_CreateReference_Proxy( 
    IIdentityAuthority * This,
    /* [retval][out] */ IReferenceIdentity **ppNewIdentity);


void __RPC_STUB IIdentityAuthority_CreateReference_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);



#endif 	/* __IIdentityAuthority_INTERFACE_DEFINED__ */


#ifndef __IAppIdAuthority_INTERFACE_DEFINED__
#define __IAppIdAuthority_INTERFACE_DEFINED__

/* interface IAppIdAuthority */
/* [local][unique][uuid][object] */ 

/* [v1_enum] */ 
enum IAPPIDAUTHORITY_ARE_DEFINITIONS_EQUAL_FLAGS
    {	IAPPIDAUTHORITY_ARE_DEFINITIONS_EQUAL_FLAG_IGNORE_VERSION	= 0x1
    } ;
/* [v1_enum] */ 
enum IAPPIDAUTHORITY_ARE_REFERENCES_EQUAL_FLAGS
    {	IAPPIDAUTHORITY_ARE_REFERENCES_EQUAL_FLAG_IGNORE_VERSION	= 0x1
    } ;

EXTERN_C const IID IID_IAppIdAuthority;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("8c87810c-2541-4f75-b2d0-9af515488e23")
    IAppIdAuthority : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE TextToDefinition( 
            /* [in] */ DWORD dwFlags,
            /* [in] */ LPCWSTR pszIdentity,
            /* [out] */ IDefinitionAppId **ppIDefinitionAppId) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE TextToReference( 
            /* [in] */ DWORD dwFlags,
            /* [in] */ LPCWSTR pszIdentity,
            /* [out] */ IReferenceAppId **ppIReferenceAppId) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE DefinitionToText( 
            /* [in] */ DWORD dwFlags,
            /* [in] */ IDefinitionAppId *pIDefinitionAppId,
            /* [out] */ LPWSTR *ppszFormattedIdentity) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE ReferenceToText( 
            /* [in] */ DWORD dwFlags,
            /* [in] */ IReferenceAppId *pIReferenceAppId,
            /* [out] */ LPWSTR *ppszFormattedIdentity) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE AreDefinitionsEqual( 
            /* [in] */ DWORD dwFlags,
            /* [in] */ IDefinitionAppId *pDefinition1,
            /* [in] */ IDefinitionAppId *pDefinition2,
            /* [out] */ BOOL *pfAreEqual) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE AreReferencesEqual( 
            /* [in] */ DWORD dwFlags,
            /* [in] */ IReferenceAppId *pReference1,
            /* [in] */ IReferenceAppId *pReference2,
            /* [out] */ BOOL *pfAreEqual) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE AreTextualDefinitionsEqual( 
            /* [in] */ DWORD dwFlags,
            /* [in] */ LPCWSTR pszAppIdLeft,
            /* [in] */ LPCWSTR pszAppIdRight,
            /* [out] */ BOOL *pfAreEqual) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE AreTextualReferencesEqual( 
            /* [in] */ DWORD dwFlags,
            /* [in] */ LPCWSTR pszAppIdLeft,
            /* [in] */ LPCWSTR pszAppIdRight,
            /* [out] */ BOOL *pfAreEqual) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE DoesDefinitionMatchReference( 
            /* [in] */ DWORD dwFlags,
            /* [in] */ IDefinitionAppId *pIDefinitionIdentity,
            /* [in] */ IReferenceAppId *pIReferenceIdentity,
            /* [out] */ BOOL *pfMatches) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE DoesTextualDefinitionMatchTextualReference( 
            /* [in] */ DWORD dwFlags,
            /* [in] */ LPCWSTR pszDefinition,
            /* [in] */ LPCWSTR pszReference,
            /* [out] */ BOOL *pfMatches) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE HashReference( 
            /* [in] */ DWORD dwFlags,
            /* [in] */ IReferenceAppId *pIReferenceIdentity,
            /* [out] */ ULONGLONG *pullPseudoKey) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE HashDefinition( 
            /* [in] */ DWORD dwFlags,
            /* [in] */ IDefinitionAppId *pIDefinitionIdentity,
            /* [out] */ ULONGLONG *pullPseudoKey) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GenerateDefinitionKey( 
            /* [in] */ DWORD dwFlags,
            /* [in] */ IDefinitionAppId *pIDefinitionIdentity,
            /* [out] */ LPWSTR *ppszKeyForm) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GenerateReferenceKey( 
            /* [in] */ DWORD dwFlags,
            /* [in] */ IReferenceAppId *pIReferenceIdentity,
            /* [out] */ LPWSTR *ppszKeyForm) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE CreateDefinition( 
            /* [retval][out] */ IDefinitionAppId **ppNewIdentity) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE CreateReference( 
            /* [retval][out] */ IReferenceAppId **ppNewIdentity) = 0;
        
    };
    
#else 	/* C style interface */

    typedef struct IAppIdAuthorityVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            IAppIdAuthority * This,
            /* [in] */ REFIID riid,
            /* [iid_is][out] */ void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            IAppIdAuthority * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            IAppIdAuthority * This);
        
        HRESULT ( STDMETHODCALLTYPE *TextToDefinition )( 
            IAppIdAuthority * This,
            /* [in] */ DWORD dwFlags,
            /* [in] */ LPCWSTR pszIdentity,
            /* [out] */ IDefinitionAppId **ppIDefinitionAppId);
        
        HRESULT ( STDMETHODCALLTYPE *TextToReference )( 
            IAppIdAuthority * This,
            /* [in] */ DWORD dwFlags,
            /* [in] */ LPCWSTR pszIdentity,
            /* [out] */ IReferenceAppId **ppIReferenceAppId);
        
        HRESULT ( STDMETHODCALLTYPE *DefinitionToText )( 
            IAppIdAuthority * This,
            /* [in] */ DWORD dwFlags,
            /* [in] */ IDefinitionAppId *pIDefinitionAppId,
            /* [out] */ LPWSTR *ppszFormattedIdentity);
        
        HRESULT ( STDMETHODCALLTYPE *ReferenceToText )( 
            IAppIdAuthority * This,
            /* [in] */ DWORD dwFlags,
            /* [in] */ IReferenceAppId *pIReferenceAppId,
            /* [out] */ LPWSTR *ppszFormattedIdentity);
        
        HRESULT ( STDMETHODCALLTYPE *AreDefinitionsEqual )( 
            IAppIdAuthority * This,
            /* [in] */ DWORD dwFlags,
            /* [in] */ IDefinitionAppId *pDefinition1,
            /* [in] */ IDefinitionAppId *pDefinition2,
            /* [out] */ BOOL *pfAreEqual);
        
        HRESULT ( STDMETHODCALLTYPE *AreReferencesEqual )( 
            IAppIdAuthority * This,
            /* [in] */ DWORD dwFlags,
            /* [in] */ IReferenceAppId *pReference1,
            /* [in] */ IReferenceAppId *pReference2,
            /* [out] */ BOOL *pfAreEqual);
        
        HRESULT ( STDMETHODCALLTYPE *AreTextualDefinitionsEqual )( 
            IAppIdAuthority * This,
            /* [in] */ DWORD dwFlags,
            /* [in] */ LPCWSTR pszAppIdLeft,
            /* [in] */ LPCWSTR pszAppIdRight,
            /* [out] */ BOOL *pfAreEqual);
        
        HRESULT ( STDMETHODCALLTYPE *AreTextualReferencesEqual )( 
            IAppIdAuthority * This,
            /* [in] */ DWORD dwFlags,
            /* [in] */ LPCWSTR pszAppIdLeft,
            /* [in] */ LPCWSTR pszAppIdRight,
            /* [out] */ BOOL *pfAreEqual);
        
        HRESULT ( STDMETHODCALLTYPE *DoesDefinitionMatchReference )( 
            IAppIdAuthority * This,
            /* [in] */ DWORD dwFlags,
            /* [in] */ IDefinitionAppId *pIDefinitionIdentity,
            /* [in] */ IReferenceAppId *pIReferenceIdentity,
            /* [out] */ BOOL *pfMatches);
        
        HRESULT ( STDMETHODCALLTYPE *DoesTextualDefinitionMatchTextualReference )( 
            IAppIdAuthority * This,
            /* [in] */ DWORD dwFlags,
            /* [in] */ LPCWSTR pszDefinition,
            /* [in] */ LPCWSTR pszReference,
            /* [out] */ BOOL *pfMatches);
        
        HRESULT ( STDMETHODCALLTYPE *HashReference )( 
            IAppIdAuthority * This,
            /* [in] */ DWORD dwFlags,
            /* [in] */ IReferenceAppId *pIReferenceIdentity,
            /* [out] */ ULONGLONG *pullPseudoKey);
        
        HRESULT ( STDMETHODCALLTYPE *HashDefinition )( 
            IAppIdAuthority * This,
            /* [in] */ DWORD dwFlags,
            /* [in] */ IDefinitionAppId *pIDefinitionIdentity,
            /* [out] */ ULONGLONG *pullPseudoKey);
        
        HRESULT ( STDMETHODCALLTYPE *GenerateDefinitionKey )( 
            IAppIdAuthority * This,
            /* [in] */ DWORD dwFlags,
            /* [in] */ IDefinitionAppId *pIDefinitionIdentity,
            /* [out] */ LPWSTR *ppszKeyForm);
        
        HRESULT ( STDMETHODCALLTYPE *GenerateReferenceKey )( 
            IAppIdAuthority * This,
            /* [in] */ DWORD dwFlags,
            /* [in] */ IReferenceAppId *pIReferenceIdentity,
            /* [out] */ LPWSTR *ppszKeyForm);
        
        HRESULT ( STDMETHODCALLTYPE *CreateDefinition )( 
            IAppIdAuthority * This,
            /* [retval][out] */ IDefinitionAppId **ppNewIdentity);
        
        HRESULT ( STDMETHODCALLTYPE *CreateReference )( 
            IAppIdAuthority * This,
            /* [retval][out] */ IReferenceAppId **ppNewIdentity);
        
        END_INTERFACE
    } IAppIdAuthorityVtbl;

    interface IAppIdAuthority
    {
        CONST_VTBL struct IAppIdAuthorityVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define IAppIdAuthority_QueryInterface(This,riid,ppvObject)	\
    (This)->lpVtbl -> QueryInterface(This,riid,ppvObject)

#define IAppIdAuthority_AddRef(This)	\
    (This)->lpVtbl -> AddRef(This)

#define IAppIdAuthority_Release(This)	\
    (This)->lpVtbl -> Release(This)


#define IAppIdAuthority_TextToDefinition(This,dwFlags,pszIdentity,ppIDefinitionAppId)	\
    (This)->lpVtbl -> TextToDefinition(This,dwFlags,pszIdentity,ppIDefinitionAppId)

#define IAppIdAuthority_TextToReference(This,dwFlags,pszIdentity,ppIReferenceAppId)	\
    (This)->lpVtbl -> TextToReference(This,dwFlags,pszIdentity,ppIReferenceAppId)

#define IAppIdAuthority_DefinitionToText(This,dwFlags,pIDefinitionAppId,ppszFormattedIdentity)	\
    (This)->lpVtbl -> DefinitionToText(This,dwFlags,pIDefinitionAppId,ppszFormattedIdentity)

#define IAppIdAuthority_ReferenceToText(This,dwFlags,pIReferenceAppId,ppszFormattedIdentity)	\
    (This)->lpVtbl -> ReferenceToText(This,dwFlags,pIReferenceAppId,ppszFormattedIdentity)

#define IAppIdAuthority_AreDefinitionsEqual(This,dwFlags,pDefinition1,pDefinition2,pfAreEqual)	\
    (This)->lpVtbl -> AreDefinitionsEqual(This,dwFlags,pDefinition1,pDefinition2,pfAreEqual)

#define IAppIdAuthority_AreReferencesEqual(This,dwFlags,pReference1,pReference2,pfAreEqual)	\
    (This)->lpVtbl -> AreReferencesEqual(This,dwFlags,pReference1,pReference2,pfAreEqual)

#define IAppIdAuthority_AreTextualDefinitionsEqual(This,dwFlags,pszAppIdLeft,pszAppIdRight,pfAreEqual)	\
    (This)->lpVtbl -> AreTextualDefinitionsEqual(This,dwFlags,pszAppIdLeft,pszAppIdRight,pfAreEqual)

#define IAppIdAuthority_AreTextualReferencesEqual(This,dwFlags,pszAppIdLeft,pszAppIdRight,pfAreEqual)	\
    (This)->lpVtbl -> AreTextualReferencesEqual(This,dwFlags,pszAppIdLeft,pszAppIdRight,pfAreEqual)

#define IAppIdAuthority_DoesDefinitionMatchReference(This,dwFlags,pIDefinitionIdentity,pIReferenceIdentity,pfMatches)	\
    (This)->lpVtbl -> DoesDefinitionMatchReference(This,dwFlags,pIDefinitionIdentity,pIReferenceIdentity,pfMatches)

#define IAppIdAuthority_DoesTextualDefinitionMatchTextualReference(This,dwFlags,pszDefinition,pszReference,pfMatches)	\
    (This)->lpVtbl -> DoesTextualDefinitionMatchTextualReference(This,dwFlags,pszDefinition,pszReference,pfMatches)

#define IAppIdAuthority_HashReference(This,dwFlags,pIReferenceIdentity,pullPseudoKey)	\
    (This)->lpVtbl -> HashReference(This,dwFlags,pIReferenceIdentity,pullPseudoKey)

#define IAppIdAuthority_HashDefinition(This,dwFlags,pIDefinitionIdentity,pullPseudoKey)	\
    (This)->lpVtbl -> HashDefinition(This,dwFlags,pIDefinitionIdentity,pullPseudoKey)

#define IAppIdAuthority_GenerateDefinitionKey(This,dwFlags,pIDefinitionIdentity,ppszKeyForm)	\
    (This)->lpVtbl -> GenerateDefinitionKey(This,dwFlags,pIDefinitionIdentity,ppszKeyForm)

#define IAppIdAuthority_GenerateReferenceKey(This,dwFlags,pIReferenceIdentity,ppszKeyForm)	\
    (This)->lpVtbl -> GenerateReferenceKey(This,dwFlags,pIReferenceIdentity,ppszKeyForm)

#define IAppIdAuthority_CreateDefinition(This,ppNewIdentity)	\
    (This)->lpVtbl -> CreateDefinition(This,ppNewIdentity)

#define IAppIdAuthority_CreateReference(This,ppNewIdentity)	\
    (This)->lpVtbl -> CreateReference(This,ppNewIdentity)

#endif /* COBJMACROS */


#endif 	/* C style interface */



HRESULT STDMETHODCALLTYPE IAppIdAuthority_TextToDefinition_Proxy( 
    IAppIdAuthority * This,
    /* [in] */ DWORD dwFlags,
    /* [in] */ LPCWSTR pszIdentity,
    /* [out] */ IDefinitionAppId **ppIDefinitionAppId);


void __RPC_STUB IAppIdAuthority_TextToDefinition_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IAppIdAuthority_TextToReference_Proxy( 
    IAppIdAuthority * This,
    /* [in] */ DWORD dwFlags,
    /* [in] */ LPCWSTR pszIdentity,
    /* [out] */ IReferenceAppId **ppIReferenceAppId);


void __RPC_STUB IAppIdAuthority_TextToReference_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IAppIdAuthority_DefinitionToText_Proxy( 
    IAppIdAuthority * This,
    /* [in] */ DWORD dwFlags,
    /* [in] */ IDefinitionAppId *pIDefinitionAppId,
    /* [out] */ _Outptr_result_maybenull_ LPWSTR *ppszFormattedIdentity);


void __RPC_STUB IAppIdAuthority_DefinitionToText_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IAppIdAuthority_ReferenceToText_Proxy( 
    IAppIdAuthority * This,
    /* [in] */ DWORD dwFlags,
    /* [in] */ IReferenceAppId *pIReferenceAppId,
    /* [out] */ _Outptr_result_maybenull_ LPWSTR *ppszFormattedIdentity);


void __RPC_STUB IAppIdAuthority_ReferenceToText_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IAppIdAuthority_AreDefinitionsEqual_Proxy( 
    IAppIdAuthority * This,
    /* [in] */ DWORD dwFlags,
    /* [in] */ IDefinitionAppId *pDefinition1,
    /* [in] */ IDefinitionAppId *pDefinition2,
    /* [out] */ BOOL *pfAreEqual);


void __RPC_STUB IAppIdAuthority_AreDefinitionsEqual_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IAppIdAuthority_AreReferencesEqual_Proxy( 
    IAppIdAuthority * This,
    /* [in] */ DWORD dwFlags,
    /* [in] */ IReferenceAppId *pReference1,
    /* [in] */ IReferenceAppId *pReference2,
    /* [out] */ BOOL *pfAreEqual);


void __RPC_STUB IAppIdAuthority_AreReferencesEqual_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IAppIdAuthority_AreTextualDefinitionsEqual_Proxy( 
    IAppIdAuthority * This,
    /* [in] */ DWORD dwFlags,
    /* [in] */ LPCWSTR pszAppIdLeft,
    /* [in] */ LPCWSTR pszAppIdRight,
    /* [out] */ BOOL *pfAreEqual);


void __RPC_STUB IAppIdAuthority_AreTextualDefinitionsEqual_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IAppIdAuthority_AreTextualReferencesEqual_Proxy( 
    IAppIdAuthority * This,
    /* [in] */ DWORD dwFlags,
    /* [in] */ LPCWSTR pszAppIdLeft,
    /* [in] */ LPCWSTR pszAppIdRight,
    /* [out] */ BOOL *pfAreEqual);


void __RPC_STUB IAppIdAuthority_AreTextualReferencesEqual_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IAppIdAuthority_DoesDefinitionMatchReference_Proxy( 
    IAppIdAuthority * This,
    /* [in] */ DWORD dwFlags,
    /* [in] */ IDefinitionAppId *pIDefinitionIdentity,
    /* [in] */ IReferenceAppId *pIReferenceIdentity,
    /* [out] */ BOOL *pfMatches);


void __RPC_STUB IAppIdAuthority_DoesDefinitionMatchReference_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IAppIdAuthority_DoesTextualDefinitionMatchTextualReference_Proxy( 
    IAppIdAuthority * This,
    /* [in] */ DWORD dwFlags,
    /* [in] */ LPCWSTR pszDefinition,
    /* [in] */ LPCWSTR pszReference,
    /* [out] */ BOOL *pfMatches);


void __RPC_STUB IAppIdAuthority_DoesTextualDefinitionMatchTextualReference_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IAppIdAuthority_HashReference_Proxy( 
    IAppIdAuthority * This,
    /* [in] */ DWORD dwFlags,
    /* [in] */ IReferenceAppId *pIReferenceIdentity,
    /* [out] */ ULONGLONG *pullPseudoKey);


void __RPC_STUB IAppIdAuthority_HashReference_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IAppIdAuthority_HashDefinition_Proxy( 
    IAppIdAuthority * This,
    /* [in] */ DWORD dwFlags,
    /* [in] */ IDefinitionAppId *pIDefinitionIdentity,
    /* [out] */ ULONGLONG *pullPseudoKey);


void __RPC_STUB IAppIdAuthority_HashDefinition_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IAppIdAuthority_GenerateDefinitionKey_Proxy( 
    IAppIdAuthority * This,
    /* [in] */ DWORD dwFlags,
    /* [in] */ IDefinitionAppId *pIDefinitionIdentity,
    /* [out] */ _Outptr_result_maybenull_ LPWSTR *ppszKeyForm);


void __RPC_STUB IAppIdAuthority_GenerateDefinitionKey_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IAppIdAuthority_GenerateReferenceKey_Proxy( 
    IAppIdAuthority * This,
    /* [in] */ DWORD dwFlags,
    /* [in] */ IReferenceAppId *pIReferenceIdentity,
    /* [out] */ _Outptr_result_maybenull_ LPWSTR *ppszKeyForm);


void __RPC_STUB IAppIdAuthority_GenerateReferenceKey_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IAppIdAuthority_CreateDefinition_Proxy( 
    IAppIdAuthority * This,
    /* [retval][out] */ IDefinitionAppId **ppNewIdentity);


void __RPC_STUB IAppIdAuthority_CreateDefinition_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IAppIdAuthority_CreateReference_Proxy( 
    IAppIdAuthority * This,
    /* [retval][out] */ IReferenceAppId **ppNewIdentity);


void __RPC_STUB IAppIdAuthority_CreateReference_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);



#endif 	/* __IAppIdAuthority_INTERFACE_DEFINED__ */


/* [local] */ HRESULT __stdcall GetAppIdAuthority( 
    /* [out] */ IAppIdAuthority **ppIAppIdAuthority);

/* [local] */ HRESULT __stdcall GetIdentityAuthority( 
    /* [out] */ IIdentityAuthority **ppIIdentityAuthority);


#ifdef __cplusplus
}
#endif

#endif

