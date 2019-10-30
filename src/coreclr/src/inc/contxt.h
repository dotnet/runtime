// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/* this ALWAYS GENERATED file contains the definitions for the interfaces */


/* File created by MIDL compiler version 5.01.0164 */
/* at Mon May 01 14:39:38 2000
 */
/* Compiler settings for contxt.idl:
    Os (OptLev=s), W1, Zp8, env=Win32, ms_ext, c_ext
    error checks: allocation ref bounds_check enum stub_data 
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

#ifndef __contxt_h__
#define __contxt_h__

#ifdef __cplusplus
extern "C"{
#endif 

/* Forward Declarations */ 

#ifndef __IEnumContextProps_FWD_DEFINED__
#define __IEnumContextProps_FWD_DEFINED__
typedef interface IEnumContextProps IEnumContextProps;
#endif 	/* __IEnumContextProps_FWD_DEFINED__ */


#ifndef __IContext_FWD_DEFINED__
#define __IContext_FWD_DEFINED__
typedef interface IContext IContext;
#endif 	/* __IContext_FWD_DEFINED__ */


#ifndef __IContextMarshaler_FWD_DEFINED__
#define __IContextMarshaler_FWD_DEFINED__
typedef interface IContextMarshaler IContextMarshaler;
#endif 	/* __IContextMarshaler_FWD_DEFINED__ */


#ifndef __IObjContext_FWD_DEFINED__
#define __IObjContext_FWD_DEFINED__
typedef interface IObjContext IObjContext;
#endif 	/* __IObjContext_FWD_DEFINED__ */


#ifndef __IGetContextId_FWD_DEFINED__
#define __IGetContextId_FWD_DEFINED__
typedef interface IGetContextId IGetContextId;
#endif 	/* __IGetContextId_FWD_DEFINED__ */


#ifndef __IAggregator_FWD_DEFINED__
#define __IAggregator_FWD_DEFINED__
typedef interface IAggregator IAggregator;
#endif 	/* __IAggregator_FWD_DEFINED__ */


#ifndef __ICall_FWD_DEFINED__
#define __ICall_FWD_DEFINED__
typedef interface ICall ICall;
#endif 	/* __ICall_FWD_DEFINED__ */


#ifndef __IRpcCall_FWD_DEFINED__
#define __IRpcCall_FWD_DEFINED__
typedef interface IRpcCall IRpcCall;
#endif 	/* __IRpcCall_FWD_DEFINED__ */


#ifndef __ICallInfo_FWD_DEFINED__
#define __ICallInfo_FWD_DEFINED__
typedef interface ICallInfo ICallInfo;
#endif 	/* __ICallInfo_FWD_DEFINED__ */


#ifndef __IPolicy_FWD_DEFINED__
#define __IPolicy_FWD_DEFINED__
typedef interface IPolicy IPolicy;
#endif 	/* __IPolicy_FWD_DEFINED__ */


#ifndef __IPolicyAsync_FWD_DEFINED__
#define __IPolicyAsync_FWD_DEFINED__
typedef interface IPolicyAsync IPolicyAsync;
#endif 	/* __IPolicyAsync_FWD_DEFINED__ */


#ifndef __IPolicySet_FWD_DEFINED__
#define __IPolicySet_FWD_DEFINED__
typedef interface IPolicySet IPolicySet;
#endif 	/* __IPolicySet_FWD_DEFINED__ */


#ifndef __IComObjIdentity_FWD_DEFINED__
#define __IComObjIdentity_FWD_DEFINED__
typedef interface IComObjIdentity IComObjIdentity;
#endif 	/* __IComObjIdentity_FWD_DEFINED__ */


#ifndef __IPolicyMaker_FWD_DEFINED__
#define __IPolicyMaker_FWD_DEFINED__
typedef interface IPolicyMaker IPolicyMaker;
#endif 	/* __IPolicyMaker_FWD_DEFINED__ */


#ifndef __IExceptionNotification_FWD_DEFINED__
#define __IExceptionNotification_FWD_DEFINED__
typedef interface IExceptionNotification IExceptionNotification;
#endif 	/* __IExceptionNotification_FWD_DEFINED__ */


#ifndef __IMarshalEnvoy_FWD_DEFINED__
#define __IMarshalEnvoy_FWD_DEFINED__
typedef interface IMarshalEnvoy IMarshalEnvoy;
#endif 	/* __IMarshalEnvoy_FWD_DEFINED__ */


#ifndef __IWrapperInfo_FWD_DEFINED__
#define __IWrapperInfo_FWD_DEFINED__
typedef interface IWrapperInfo IWrapperInfo;
#endif 	/* __IWrapperInfo_FWD_DEFINED__ */


#ifndef __IComThreadingInfo_FWD_DEFINED__
#define __IComThreadingInfo_FWD_DEFINED__
typedef interface IComThreadingInfo IComThreadingInfo;
#endif 	/* __IComThreadingInfo_FWD_DEFINED__ */


#ifndef __IComDispatchInfo_FWD_DEFINED__
#define __IComDispatchInfo_FWD_DEFINED__
typedef interface IComDispatchInfo IComDispatchInfo;
#endif 	/* __IComDispatchInfo_FWD_DEFINED__ */


/* header files for imported files */
#include "wtypes.h"
#include "objidl.h"

void __RPC_FAR * __RPC_USER MIDL_user_allocate(size_t);
void __RPC_USER MIDL_user_free( void __RPC_FAR * ); 

/* interface __MIDL_itf_contxt_0000 */
/* [local] */ 

enum tagCONTEXTEVENT
    {	CONTEXTEVENT_NONE	= 0,
	CONTEXTEVENT_CALL	= 0x1,
	CONTEXTEVENT_ENTER	= 0x2,
	CONTEXTEVENT_LEAVE	= 0x4,
	CONTEXTEVENT_RETURN	= 0x8,
	CONTEXTEVENT_CALLFILLBUFFER	= 0x10,
	CONTEXTEVENT_ENTERWITHBUFFER	= 0x20,
	CONTEXTEVENT_LEAVEFILLBUFFER	= 0x40,
	CONTEXTEVENT_RETURNWITHBUFFER	= 0x80,
	CONTEXTEVENT_BEGINCALL	= 0x100,
	CONTEXTEVENT_BEGINENTER	= 0x200,
	CONTEXTEVENT_BEGINLEAVE	= 0x400,
	CONTEXTEVENT_BEGINRETURN	= 0x800,
	CONTEXTEVENT_FINISHCALL	= 0x1000,
	CONTEXTEVENT_FINISHENTER	= 0x2000,
	CONTEXTEVENT_FINISHLEAVE	= 0x4000,
	CONTEXTEVENT_FINISHRETURN	= 0x8000,
	CONTEXTEVENT_BEGINCALLFILLBUFFER	= 0x10000,
	CONTEXTEVENT_BEGINENTERWITHBUFFER	= 0x20000,
	CONTEXTEVENT_FINISHLEAVEFILLBUFFER	= 0x40000,
	CONTEXTEVENT_FINISHRETURNWITHBUFFER	= 0x80000,
	CONTEXTEVENT_LEAVEEXCEPTION	= 0x100000,
	CONTEXTEVENT_LEAVEEXCEPTIONFILLBUFFER	= 0x200000,
	CONTEXTEVENT_RETURNEXCEPTION	= 0x400000,
	CONTEXTEVENT_RETURNEXCEPTIONWITHBUFFER	= 0x800000,
	CONTEXTEVENT_ADDREFPOLICY	= 0x10000000,
	CONTEXTEVENT_RELEASEPOLICY	= 0x20000000
    };
typedef DWORD ContextEvent;


enum tagCPFLAGS
    {	CPFLAG_NONE	= 0,
	CPFLAG_PROPAGATE	= 0x1,
	CPFLAG_EXPOSE	= 0x2,
	CPFLAG_ENVOY	= 0x4,
	CPFLAG_MONITORSTUB	= 0x8,
	CPFLAG_MONITORPROXY	= 0x10,
	CPFLAG_DONTCOMPARE	= 0x20
    };
typedef DWORD CPFLAGS;

extern RPC_IF_HANDLE __MIDL_itf_contxt_0000_v0_0_c_ifspec;
extern RPC_IF_HANDLE __MIDL_itf_contxt_0000_v0_0_s_ifspec;

#ifndef __IEnumContextProps_INTERFACE_DEFINED__
#define __IEnumContextProps_INTERFACE_DEFINED__

/* interface IEnumContextProps */
/* [unique][uuid][object] */ 

typedef /* [unique] */ IEnumContextProps __RPC_FAR *LPENUMCONTEXTPROPS;


EXTERN_C const IID IID_IEnumContextProps;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("000001c1-0000-0000-C000-000000000046")
    IEnumContextProps : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE Next( 
            /* [in] */ ULONG celt,
            /* [length_is][size_is][out] */ ContextProperty __RPC_FAR *pContextProperties,
            /* [out] */ ULONG __RPC_FAR *pceltFetched) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE Skip( 
            /* [in] */ ULONG celt) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE Reset( void) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE Clone( 
            /* [out] */ IEnumContextProps __RPC_FAR *__RPC_FAR *ppEnumContextProps) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE Count( 
            /* [out] */ ULONG __RPC_FAR *pcelt) = 0;
        
    };
    
#else 	/* C style interface */

    typedef struct IEnumContextPropsVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE __RPC_FAR *QueryInterface )( 
            IEnumContextProps __RPC_FAR * This,
            /* [in] */ REFIID riid,
            /* [iid_is][out] */ void __RPC_FAR *__RPC_FAR *ppvObject);
        
        ULONG ( STDMETHODCALLTYPE __RPC_FAR *AddRef )( 
            IEnumContextProps __RPC_FAR * This);
        
        ULONG ( STDMETHODCALLTYPE __RPC_FAR *Release )( 
            IEnumContextProps __RPC_FAR * This);
        
        HRESULT ( STDMETHODCALLTYPE __RPC_FAR *Next )( 
            IEnumContextProps __RPC_FAR * This,
            /* [in] */ ULONG celt,
            /* [length_is][size_is][out] */ ContextProperty __RPC_FAR *pContextProperties,
            /* [out] */ ULONG __RPC_FAR *pceltFetched);
        
        HRESULT ( STDMETHODCALLTYPE __RPC_FAR *Skip )( 
            IEnumContextProps __RPC_FAR * This,
            /* [in] */ ULONG celt);
        
        HRESULT ( STDMETHODCALLTYPE __RPC_FAR *Reset )( 
            IEnumContextProps __RPC_FAR * This);
        
        HRESULT ( STDMETHODCALLTYPE __RPC_FAR *Clone )( 
            IEnumContextProps __RPC_FAR * This,
            /* [out] */ IEnumContextProps __RPC_FAR *__RPC_FAR *ppEnumContextProps);
        
        HRESULT ( STDMETHODCALLTYPE __RPC_FAR *Count )( 
            IEnumContextProps __RPC_FAR * This,
            /* [out] */ ULONG __RPC_FAR *pcelt);
        
        END_INTERFACE
    } IEnumContextPropsVtbl;

    interface IEnumContextProps
    {
        CONST_VTBL struct IEnumContextPropsVtbl __RPC_FAR *lpVtbl;
    };

    

#ifdef COBJMACROS


#define IEnumContextProps_QueryInterface(This,riid,ppvObject)	\
    (This)->lpVtbl -> QueryInterface(This,riid,ppvObject)

#define IEnumContextProps_AddRef(This)	\
    (This)->lpVtbl -> AddRef(This)

#define IEnumContextProps_Release(This)	\
    (This)->lpVtbl -> Release(This)


#define IEnumContextProps_Next(This,celt,pContextProperties,pceltFetched)	\
    (This)->lpVtbl -> Next(This,celt,pContextProperties,pceltFetched)

#define IEnumContextProps_Skip(This,celt)	\
    (This)->lpVtbl -> Skip(This,celt)

#define IEnumContextProps_Reset(This)	\
    (This)->lpVtbl -> Reset(This)

#define IEnumContextProps_Clone(This,ppEnumContextProps)	\
    (This)->lpVtbl -> Clone(This,ppEnumContextProps)

#define IEnumContextProps_Count(This,pcelt)	\
    (This)->lpVtbl -> Count(This,pcelt)

#endif /* COBJMACROS */


#endif 	/* C style interface */



HRESULT STDMETHODCALLTYPE IEnumContextProps_Next_Proxy( 
    IEnumContextProps __RPC_FAR * This,
    /* [in] */ ULONG celt,
    /* [length_is][size_is][out] */ ContextProperty __RPC_FAR *pContextProperties,
    /* [out] */ ULONG __RPC_FAR *pceltFetched);


void __RPC_STUB IEnumContextProps_Next_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IEnumContextProps_Skip_Proxy( 
    IEnumContextProps __RPC_FAR * This,
    /* [in] */ ULONG celt);


void __RPC_STUB IEnumContextProps_Skip_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IEnumContextProps_Reset_Proxy( 
    IEnumContextProps __RPC_FAR * This);


void __RPC_STUB IEnumContextProps_Reset_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IEnumContextProps_Clone_Proxy( 
    IEnumContextProps __RPC_FAR * This,
    /* [out] */ IEnumContextProps __RPC_FAR *__RPC_FAR *ppEnumContextProps);


void __RPC_STUB IEnumContextProps_Clone_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IEnumContextProps_Count_Proxy( 
    IEnumContextProps __RPC_FAR * This,
    /* [out] */ ULONG __RPC_FAR *pcelt);


void __RPC_STUB IEnumContextProps_Count_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);



#endif 	/* __IEnumContextProps_INTERFACE_DEFINED__ */


#ifndef __IContext_INTERFACE_DEFINED__
#define __IContext_INTERFACE_DEFINED__

/* interface IContext */
/* [unique][uuid][object][local] */ 


EXTERN_C const IID IID_IContext;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("000001c0-0000-0000-C000-000000000046")
    IContext : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE SetProperty( 
            /* [in] */ REFGUID rpolicyId,
            /* [in] */ CPFLAGS flags,
            /* [in] */ IUnknown __RPC_FAR *pUnk) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE RemoveProperty( 
            /* [in] */ REFGUID rPolicyId) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetProperty( 
            /* [in] */ REFGUID rGuid,
            /* [out] */ CPFLAGS __RPC_FAR *pFlags,
            /* [out] */ IUnknown __RPC_FAR *__RPC_FAR *ppUnk) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE EnumContextProps( 
            /* [out] */ IEnumContextProps __RPC_FAR *__RPC_FAR *ppEnumContextProps) = 0;
        
    };
    
#else 	/* C style interface */

    typedef struct IContextVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE __RPC_FAR *QueryInterface )( 
            IContext __RPC_FAR * This,
            /* [in] */ REFIID riid,
            /* [iid_is][out] */ void __RPC_FAR *__RPC_FAR *ppvObject);
        
        ULONG ( STDMETHODCALLTYPE __RPC_FAR *AddRef )( 
            IContext __RPC_FAR * This);
        
        ULONG ( STDMETHODCALLTYPE __RPC_FAR *Release )( 
            IContext __RPC_FAR * This);
        
        HRESULT ( STDMETHODCALLTYPE __RPC_FAR *SetProperty )( 
            IContext __RPC_FAR * This,
            /* [in] */ REFGUID rpolicyId,
            /* [in] */ CPFLAGS flags,
            /* [in] */ IUnknown __RPC_FAR *pUnk);
        
        HRESULT ( STDMETHODCALLTYPE __RPC_FAR *RemoveProperty )( 
            IContext __RPC_FAR * This,
            /* [in] */ REFGUID rPolicyId);
        
        HRESULT ( STDMETHODCALLTYPE __RPC_FAR *GetProperty )( 
            IContext __RPC_FAR * This,
            /* [in] */ REFGUID rGuid,
            /* [out] */ CPFLAGS __RPC_FAR *pFlags,
            /* [out] */ IUnknown __RPC_FAR *__RPC_FAR *ppUnk);
        
        HRESULT ( STDMETHODCALLTYPE __RPC_FAR *EnumContextProps )( 
            IContext __RPC_FAR * This,
            /* [out] */ IEnumContextProps __RPC_FAR *__RPC_FAR *ppEnumContextProps);
        
        END_INTERFACE
    } IContextVtbl;

    interface IContext
    {
        CONST_VTBL struct IContextVtbl __RPC_FAR *lpVtbl;
    };

    

#ifdef COBJMACROS


#define IContext_QueryInterface(This,riid,ppvObject)	\
    (This)->lpVtbl -> QueryInterface(This,riid,ppvObject)

#define IContext_AddRef(This)	\
    (This)->lpVtbl -> AddRef(This)

#define IContext_Release(This)	\
    (This)->lpVtbl -> Release(This)


#define IContext_SetProperty(This,rpolicyId,flags,pUnk)	\
    (This)->lpVtbl -> SetProperty(This,rpolicyId,flags,pUnk)

#define IContext_RemoveProperty(This,rPolicyId)	\
    (This)->lpVtbl -> RemoveProperty(This,rPolicyId)

#define IContext_GetProperty(This,rGuid,pFlags,ppUnk)	\
    (This)->lpVtbl -> GetProperty(This,rGuid,pFlags,ppUnk)

#define IContext_EnumContextProps(This,ppEnumContextProps)	\
    (This)->lpVtbl -> EnumContextProps(This,ppEnumContextProps)

#endif /* COBJMACROS */


#endif 	/* C style interface */



HRESULT STDMETHODCALLTYPE IContext_SetProperty_Proxy( 
    IContext __RPC_FAR * This,
    /* [in] */ REFGUID rpolicyId,
    /* [in] */ CPFLAGS flags,
    /* [in] */ IUnknown __RPC_FAR *pUnk);


void __RPC_STUB IContext_SetProperty_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IContext_RemoveProperty_Proxy( 
    IContext __RPC_FAR * This,
    /* [in] */ REFGUID rPolicyId);


void __RPC_STUB IContext_RemoveProperty_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IContext_GetProperty_Proxy( 
    IContext __RPC_FAR * This,
    /* [in] */ REFGUID rGuid,
    /* [out] */ CPFLAGS __RPC_FAR *pFlags,
    /* [out] */ IUnknown __RPC_FAR *__RPC_FAR *ppUnk);


void __RPC_STUB IContext_GetProperty_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IContext_EnumContextProps_Proxy( 
    IContext __RPC_FAR * This,
    /* [out] */ IEnumContextProps __RPC_FAR *__RPC_FAR *ppEnumContextProps);


void __RPC_STUB IContext_EnumContextProps_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);



#endif 	/* __IContext_INTERFACE_DEFINED__ */


#ifndef __IContextMarshaler_INTERFACE_DEFINED__
#define __IContextMarshaler_INTERFACE_DEFINED__

/* interface IContextMarshaler */
/* [uuid][object][local] */ 

typedef /* [unique] */ IContextMarshaler __RPC_FAR *LPCTXMARSHALER;


EXTERN_C const IID IID_IContextMarshaler;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("000001D8-0000-0000-C000-000000000046")
    IContextMarshaler : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE GetMarshalSizeMax( 
            /* [in] */ REFIID riid,
            /* [unique][in] */ void __RPC_FAR *pv,
            /* [in] */ DWORD dwDestContext,
            /* [unique][in] */ void __RPC_FAR *pvDestContext,
            /* [in] */ DWORD mshlflags,
            /* [out] */ DWORD __RPC_FAR *pSize) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE MarshalInterface( 
            /* [unique][in] */ IStream __RPC_FAR *pStm,
            /* [in] */ REFIID riid,
            /* [unique][in] */ void __RPC_FAR *pv,
            /* [in] */ DWORD dwDestContext,
            /* [unique][in] */ void __RPC_FAR *pvDestContext,
            /* [in] */ DWORD mshlflags) = 0;
        
    };
    
#else 	/* C style interface */

    typedef struct IContextMarshalerVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE __RPC_FAR *QueryInterface )( 
            IContextMarshaler __RPC_FAR * This,
            /* [in] */ REFIID riid,
            /* [iid_is][out] */ void __RPC_FAR *__RPC_FAR *ppvObject);
        
        ULONG ( STDMETHODCALLTYPE __RPC_FAR *AddRef )( 
            IContextMarshaler __RPC_FAR * This);
        
        ULONG ( STDMETHODCALLTYPE __RPC_FAR *Release )( 
            IContextMarshaler __RPC_FAR * This);
        
        HRESULT ( STDMETHODCALLTYPE __RPC_FAR *GetMarshalSizeMax )( 
            IContextMarshaler __RPC_FAR * This,
            /* [in] */ REFIID riid,
            /* [unique][in] */ void __RPC_FAR *pv,
            /* [in] */ DWORD dwDestContext,
            /* [unique][in] */ void __RPC_FAR *pvDestContext,
            /* [in] */ DWORD mshlflags,
            /* [out] */ DWORD __RPC_FAR *pSize);
        
        HRESULT ( STDMETHODCALLTYPE __RPC_FAR *MarshalInterface )( 
            IContextMarshaler __RPC_FAR * This,
            /* [unique][in] */ IStream __RPC_FAR *pStm,
            /* [in] */ REFIID riid,
            /* [unique][in] */ void __RPC_FAR *pv,
            /* [in] */ DWORD dwDestContext,
            /* [unique][in] */ void __RPC_FAR *pvDestContext,
            /* [in] */ DWORD mshlflags);
        
        END_INTERFACE
    } IContextMarshalerVtbl;

    interface IContextMarshaler
    {
        CONST_VTBL struct IContextMarshalerVtbl __RPC_FAR *lpVtbl;
    };

    

#ifdef COBJMACROS


#define IContextMarshaler_QueryInterface(This,riid,ppvObject)	\
    (This)->lpVtbl -> QueryInterface(This,riid,ppvObject)

#define IContextMarshaler_AddRef(This)	\
    (This)->lpVtbl -> AddRef(This)

#define IContextMarshaler_Release(This)	\
    (This)->lpVtbl -> Release(This)


#define IContextMarshaler_GetMarshalSizeMax(This,riid,pv,dwDestContext,pvDestContext,mshlflags,pSize)	\
    (This)->lpVtbl -> GetMarshalSizeMax(This,riid,pv,dwDestContext,pvDestContext,mshlflags,pSize)

#define IContextMarshaler_MarshalInterface(This,pStm,riid,pv,dwDestContext,pvDestContext,mshlflags)	\
    (This)->lpVtbl -> MarshalInterface(This,pStm,riid,pv,dwDestContext,pvDestContext,mshlflags)

#endif /* COBJMACROS */


#endif 	/* C style interface */



HRESULT STDMETHODCALLTYPE IContextMarshaler_GetMarshalSizeMax_Proxy( 
    IContextMarshaler __RPC_FAR * This,
    /* [in] */ REFIID riid,
    /* [unique][in] */ void __RPC_FAR *pv,
    /* [in] */ DWORD dwDestContext,
    /* [unique][in] */ void __RPC_FAR *pvDestContext,
    /* [in] */ DWORD mshlflags,
    /* [out] */ DWORD __RPC_FAR *pSize);


void __RPC_STUB IContextMarshaler_GetMarshalSizeMax_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IContextMarshaler_MarshalInterface_Proxy( 
    IContextMarshaler __RPC_FAR * This,
    /* [unique][in] */ IStream __RPC_FAR *pStm,
    /* [in] */ REFIID riid,
    /* [unique][in] */ void __RPC_FAR *pv,
    /* [in] */ DWORD dwDestContext,
    /* [unique][in] */ void __RPC_FAR *pvDestContext,
    /* [in] */ DWORD mshlflags);


void __RPC_STUB IContextMarshaler_MarshalInterface_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);



#endif 	/* __IContextMarshaler_INTERFACE_DEFINED__ */


// Placing the following definition here rather than with the IObjContext stuff
// below is a temporary workaround to get around build problems where the system
// objidl.h now has a IObjContext section but has not made much public (all the
// interface methods are marked as reserved and the following typedef does not
// exist). Once the system objidl.h is updated again we can remove the entire
// section.
#ifndef __PFNCTXCALLBACK_HACK
#define __PFNCTXCALLBACK_HACK
typedef /* [ref] */ HRESULT ( __stdcall __RPC_FAR *PFNCTXCALLBACK )( 
    void __RPC_FAR *pParam);
#endif

#ifndef __IObjContext_INTERFACE_DEFINED__
#define __IObjContext_INTERFACE_DEFINED__

/* interface IObjContext */
/* [unique][uuid][object][local] */ 


EXTERN_C const IID IID_IObjContext;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("000001c6-0000-0000-C000-000000000046")
    IObjContext : public IContext
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE Freeze( void) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE DoCallback( 
            /* [in] */ PFNCTXCALLBACK pfnCallback,
            /* [in] */ void __RPC_FAR *pParam,
            /* [in] */ REFIID riid,
            /* [in] */ unsigned int iMethod) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE SetContextMarshaler( 
            /* [in] */ IContextMarshaler __RPC_FAR *pICM) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetContextMarshaler( 
            /* [out] */ IContextMarshaler __RPC_FAR *__RPC_FAR *pICM) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE SetContextFlags( 
            /* [in] */ DWORD dwFlags) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE ClearContextFlags( 
            /* [in] */ DWORD dwFlags) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetContextFlags( 
            /* [out] */ DWORD __RPC_FAR *pdwFlags) = 0;
        
    };
    
#else 	/* C style interface */

    typedef struct IObjContextVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE __RPC_FAR *QueryInterface )( 
            IObjContext __RPC_FAR * This,
            /* [in] */ REFIID riid,
            /* [iid_is][out] */ void __RPC_FAR *__RPC_FAR *ppvObject);
        
        ULONG ( STDMETHODCALLTYPE __RPC_FAR *AddRef )( 
            IObjContext __RPC_FAR * This);
        
        ULONG ( STDMETHODCALLTYPE __RPC_FAR *Release )( 
            IObjContext __RPC_FAR * This);
        
        HRESULT ( STDMETHODCALLTYPE __RPC_FAR *SetProperty )( 
            IObjContext __RPC_FAR * This,
            /* [in] */ REFGUID rpolicyId,
            /* [in] */ CPFLAGS flags,
            /* [in] */ IUnknown __RPC_FAR *pUnk);
        
        HRESULT ( STDMETHODCALLTYPE __RPC_FAR *RemoveProperty )( 
            IObjContext __RPC_FAR * This,
            /* [in] */ REFGUID rPolicyId);
        
        HRESULT ( STDMETHODCALLTYPE __RPC_FAR *GetProperty )( 
            IObjContext __RPC_FAR * This,
            /* [in] */ REFGUID rGuid,
            /* [out] */ CPFLAGS __RPC_FAR *pFlags,
            /* [out] */ IUnknown __RPC_FAR *__RPC_FAR *ppUnk);
        
        HRESULT ( STDMETHODCALLTYPE __RPC_FAR *EnumContextProps )( 
            IObjContext __RPC_FAR * This,
            /* [out] */ IEnumContextProps __RPC_FAR *__RPC_FAR *ppEnumContextProps);
        
        HRESULT ( STDMETHODCALLTYPE __RPC_FAR *Freeze )( 
            IObjContext __RPC_FAR * This);
        
        HRESULT ( STDMETHODCALLTYPE __RPC_FAR *DoCallback )( 
            IObjContext __RPC_FAR * This,
            /* [in] */ PFNCTXCALLBACK pfnCallback,
            /* [in] */ void __RPC_FAR *pParam,
            /* [in] */ REFIID riid,
            /* [in] */ unsigned int iMethod);
        
        HRESULT ( STDMETHODCALLTYPE __RPC_FAR *SetContextMarshaler )( 
            IObjContext __RPC_FAR * This,
            /* [in] */ IContextMarshaler __RPC_FAR *pICM);
        
        HRESULT ( STDMETHODCALLTYPE __RPC_FAR *GetContextMarshaler )( 
            IObjContext __RPC_FAR * This,
            /* [out] */ IContextMarshaler __RPC_FAR *__RPC_FAR *pICM);
        
        HRESULT ( STDMETHODCALLTYPE __RPC_FAR *SetContextFlags )( 
            IObjContext __RPC_FAR * This,
            /* [in] */ DWORD dwFlags);
        
        HRESULT ( STDMETHODCALLTYPE __RPC_FAR *ClearContextFlags )( 
            IObjContext __RPC_FAR * This,
            /* [in] */ DWORD dwFlags);
        
        HRESULT ( STDMETHODCALLTYPE __RPC_FAR *GetContextFlags )( 
            IObjContext __RPC_FAR * This,
            /* [out] */ DWORD __RPC_FAR *pdwFlags);
        
        END_INTERFACE
    } IObjContextVtbl;

    interface IObjContext
    {
        CONST_VTBL struct IObjContextVtbl __RPC_FAR *lpVtbl;
    };

    

#ifdef COBJMACROS


#define IObjContext_QueryInterface(This,riid,ppvObject)	\
    (This)->lpVtbl -> QueryInterface(This,riid,ppvObject)

#define IObjContext_AddRef(This)	\
    (This)->lpVtbl -> AddRef(This)

#define IObjContext_Release(This)	\
    (This)->lpVtbl -> Release(This)


#define IObjContext_SetProperty(This,rpolicyId,flags,pUnk)	\
    (This)->lpVtbl -> SetProperty(This,rpolicyId,flags,pUnk)

#define IObjContext_RemoveProperty(This,rPolicyId)	\
    (This)->lpVtbl -> RemoveProperty(This,rPolicyId)

#define IObjContext_GetProperty(This,rGuid,pFlags,ppUnk)	\
    (This)->lpVtbl -> GetProperty(This,rGuid,pFlags,ppUnk)

#define IObjContext_EnumContextProps(This,ppEnumContextProps)	\
    (This)->lpVtbl -> EnumContextProps(This,ppEnumContextProps)


#define IObjContext_Freeze(This)	\
    (This)->lpVtbl -> Freeze(This)

#define IObjContext_DoCallback(This,pfnCallback,pParam,riid,iMethod)	\
    (This)->lpVtbl -> DoCallback(This,pfnCallback,pParam,riid,iMethod)

#define IObjContext_SetContextMarshaler(This,pICM)	\
    (This)->lpVtbl -> SetContextMarshaler(This,pICM)

#define IObjContext_GetContextMarshaler(This,pICM)	\
    (This)->lpVtbl -> GetContextMarshaler(This,pICM)

#define IObjContext_SetContextFlags(This,dwFlags)	\
    (This)->lpVtbl -> SetContextFlags(This,dwFlags)

#define IObjContext_ClearContextFlags(This,dwFlags)	\
    (This)->lpVtbl -> ClearContextFlags(This,dwFlags)

#define IObjContext_GetContextFlags(This,pdwFlags)	\
    (This)->lpVtbl -> GetContextFlags(This,pdwFlags)

#endif /* COBJMACROS */


#endif 	/* C style interface */



HRESULT STDMETHODCALLTYPE IObjContext_Freeze_Proxy( 
    IObjContext __RPC_FAR * This);


void __RPC_STUB IObjContext_Freeze_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IObjContext_DoCallback_Proxy( 
    IObjContext __RPC_FAR * This,
    /* [in] */ PFNCTXCALLBACK pfnCallback,
    /* [in] */ void __RPC_FAR *pParam,
    /* [in] */ REFIID riid,
    /* [in] */ unsigned int iMethod);


void __RPC_STUB IObjContext_DoCallback_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IObjContext_SetContextMarshaler_Proxy( 
    IObjContext __RPC_FAR * This,
    /* [in] */ IContextMarshaler __RPC_FAR *pICM);


void __RPC_STUB IObjContext_SetContextMarshaler_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IObjContext_GetContextMarshaler_Proxy( 
    IObjContext __RPC_FAR * This,
    /* [out] */ IContextMarshaler __RPC_FAR *__RPC_FAR *pICM);


void __RPC_STUB IObjContext_GetContextMarshaler_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IObjContext_SetContextFlags_Proxy( 
    IObjContext __RPC_FAR * This,
    /* [in] */ DWORD dwFlags);


void __RPC_STUB IObjContext_SetContextFlags_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IObjContext_ClearContextFlags_Proxy( 
    IObjContext __RPC_FAR * This,
    /* [in] */ DWORD dwFlags);


void __RPC_STUB IObjContext_ClearContextFlags_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IObjContext_GetContextFlags_Proxy( 
    IObjContext __RPC_FAR * This,
    /* [out] */ DWORD __RPC_FAR *pdwFlags);


void __RPC_STUB IObjContext_GetContextFlags_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);



#endif 	/* __IObjContext_INTERFACE_DEFINED__ */


#ifndef __IGetContextId_INTERFACE_DEFINED__
#define __IGetContextId_INTERFACE_DEFINED__

/* interface IGetContextId */
/* [unique][uuid][object][local] */ 


EXTERN_C const IID IID_IGetContextId;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("000001dd-0000-0000-C000-000000000046")
    IGetContextId : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE GetContextId( 
            /* [out] */ GUID __RPC_FAR *pguidCtxtId) = 0;
        
    };
    
#else 	/* C style interface */

    typedef struct IGetContextIdVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE __RPC_FAR *QueryInterface )( 
            IGetContextId __RPC_FAR * This,
            /* [in] */ REFIID riid,
            /* [iid_is][out] */ void __RPC_FAR *__RPC_FAR *ppvObject);
        
        ULONG ( STDMETHODCALLTYPE __RPC_FAR *AddRef )( 
            IGetContextId __RPC_FAR * This);
        
        ULONG ( STDMETHODCALLTYPE __RPC_FAR *Release )( 
            IGetContextId __RPC_FAR * This);
        
        HRESULT ( STDMETHODCALLTYPE __RPC_FAR *GetContextId )( 
            IGetContextId __RPC_FAR * This,
            /* [out] */ GUID __RPC_FAR *pguidCtxtId);
        
        END_INTERFACE
    } IGetContextIdVtbl;

    interface IGetContextId
    {
        CONST_VTBL struct IGetContextIdVtbl __RPC_FAR *lpVtbl;
    };

    

#ifdef COBJMACROS


#define IGetContextId_QueryInterface(This,riid,ppvObject)	\
    (This)->lpVtbl -> QueryInterface(This,riid,ppvObject)

#define IGetContextId_AddRef(This)	\
    (This)->lpVtbl -> AddRef(This)

#define IGetContextId_Release(This)	\
    (This)->lpVtbl -> Release(This)


#define IGetContextId_GetContextId(This,pguidCtxtId)	\
    (This)->lpVtbl -> GetContextId(This,pguidCtxtId)

#endif /* COBJMACROS */


#endif 	/* C style interface */



HRESULT STDMETHODCALLTYPE IGetContextId_GetContextId_Proxy( 
    IGetContextId __RPC_FAR * This,
    /* [out] */ GUID __RPC_FAR *pguidCtxtId);


void __RPC_STUB IGetContextId_GetContextId_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);



#endif 	/* __IGetContextId_INTERFACE_DEFINED__ */


#ifndef __IAggregator_INTERFACE_DEFINED__
#define __IAggregator_INTERFACE_DEFINED__

/* interface IAggregator */
/* [unique][uuid][object][local] */ 

typedef /* [unique] */ IAggregator __RPC_FAR *IAGGREGATOR;


EXTERN_C const IID IID_IAggregator;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("000001d8-0000-0000-C000-000000000046")
    IAggregator : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE Aggregate( 
            /* [in] */ IUnknown __RPC_FAR *pInnerUnk) = 0;
        
    };
    
#else 	/* C style interface */

    typedef struct IAggregatorVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE __RPC_FAR *QueryInterface )( 
            IAggregator __RPC_FAR * This,
            /* [in] */ REFIID riid,
            /* [iid_is][out] */ void __RPC_FAR *__RPC_FAR *ppvObject);
        
        ULONG ( STDMETHODCALLTYPE __RPC_FAR *AddRef )( 
            IAggregator __RPC_FAR * This);
        
        ULONG ( STDMETHODCALLTYPE __RPC_FAR *Release )( 
            IAggregator __RPC_FAR * This);
        
        HRESULT ( STDMETHODCALLTYPE __RPC_FAR *Aggregate )( 
            IAggregator __RPC_FAR * This,
            /* [in] */ IUnknown __RPC_FAR *pInnerUnk);
        
        END_INTERFACE
    } IAggregatorVtbl;

    interface IAggregator
    {
        CONST_VTBL struct IAggregatorVtbl __RPC_FAR *lpVtbl;
    };

    

#ifdef COBJMACROS


#define IAggregator_QueryInterface(This,riid,ppvObject)	\
    (This)->lpVtbl -> QueryInterface(This,riid,ppvObject)

#define IAggregator_AddRef(This)	\
    (This)->lpVtbl -> AddRef(This)

#define IAggregator_Release(This)	\
    (This)->lpVtbl -> Release(This)


#define IAggregator_Aggregate(This,pInnerUnk)	\
    (This)->lpVtbl -> Aggregate(This,pInnerUnk)

#endif /* COBJMACROS */


#endif 	/* C style interface */



HRESULT STDMETHODCALLTYPE IAggregator_Aggregate_Proxy( 
    IAggregator __RPC_FAR * This,
    /* [in] */ IUnknown __RPC_FAR *pInnerUnk);


void __RPC_STUB IAggregator_Aggregate_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);



#endif 	/* __IAggregator_INTERFACE_DEFINED__ */


#ifndef __ICall_INTERFACE_DEFINED__
#define __ICall_INTERFACE_DEFINED__

/* interface ICall */
/* [unique][uuid][object][local] */ 

typedef /* [unique] */ ICall __RPC_FAR *LPCALL;


EXTERN_C const IID IID_ICall;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("000001d6-0000-0000-C000-000000000046")
    ICall : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE GetCallInfo( 
            /* [out] */ const void __RPC_FAR *__RPC_FAR *ppIdentity,
            /* [out] */ IID __RPC_FAR *piid,
            /* [out] */ DWORD __RPC_FAR *pdwMethod,
            /* [out] */ HRESULT __RPC_FAR *phr) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE Nullify( 
            /* [in] */ HRESULT hr) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetServerHR( 
            /* [out] */ HRESULT __RPC_FAR *phr) = 0;
        
    };
    
#else 	/* C style interface */

    typedef struct ICallVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE __RPC_FAR *QueryInterface )( 
            ICall __RPC_FAR * This,
            /* [in] */ REFIID riid,
            /* [iid_is][out] */ void __RPC_FAR *__RPC_FAR *ppvObject);
        
        ULONG ( STDMETHODCALLTYPE __RPC_FAR *AddRef )( 
            ICall __RPC_FAR * This);
        
        ULONG ( STDMETHODCALLTYPE __RPC_FAR *Release )( 
            ICall __RPC_FAR * This);
        
        HRESULT ( STDMETHODCALLTYPE __RPC_FAR *GetCallInfo )( 
            ICall __RPC_FAR * This,
            /* [out] */ const void __RPC_FAR *__RPC_FAR *ppIdentity,
            /* [out] */ IID __RPC_FAR *piid,
            /* [out] */ DWORD __RPC_FAR *pdwMethod,
            /* [out] */ HRESULT __RPC_FAR *phr);
        
        HRESULT ( STDMETHODCALLTYPE __RPC_FAR *Nullify )( 
            ICall __RPC_FAR * This,
            /* [in] */ HRESULT hr);
        
        HRESULT ( STDMETHODCALLTYPE __RPC_FAR *GetServerHR )( 
            ICall __RPC_FAR * This,
            /* [out] */ HRESULT __RPC_FAR *phr);
        
        END_INTERFACE
    } ICallVtbl;

    interface ICall
    {
        CONST_VTBL struct ICallVtbl __RPC_FAR *lpVtbl;
    };

    

#ifdef COBJMACROS


#define ICall_QueryInterface(This,riid,ppvObject)	\
    (This)->lpVtbl -> QueryInterface(This,riid,ppvObject)

#define ICall_AddRef(This)	\
    (This)->lpVtbl -> AddRef(This)

#define ICall_Release(This)	\
    (This)->lpVtbl -> Release(This)


#define ICall_GetCallInfo(This,ppIdentity,piid,pdwMethod,phr)	\
    (This)->lpVtbl -> GetCallInfo(This,ppIdentity,piid,pdwMethod,phr)

#define ICall_Nullify(This,hr)	\
    (This)->lpVtbl -> Nullify(This,hr)

#define ICall_GetServerHR(This,phr)	\
    (This)->lpVtbl -> GetServerHR(This,phr)

#endif /* COBJMACROS */


#endif 	/* C style interface */



HRESULT STDMETHODCALLTYPE ICall_GetCallInfo_Proxy( 
    ICall __RPC_FAR * This,
    /* [out] */ const void __RPC_FAR *__RPC_FAR *ppIdentity,
    /* [out] */ IID __RPC_FAR *piid,
    /* [out] */ DWORD __RPC_FAR *pdwMethod,
    /* [out] */ HRESULT __RPC_FAR *phr);


void __RPC_STUB ICall_GetCallInfo_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE ICall_Nullify_Proxy( 
    ICall __RPC_FAR * This,
    /* [in] */ HRESULT hr);


void __RPC_STUB ICall_Nullify_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE ICall_GetServerHR_Proxy( 
    ICall __RPC_FAR * This,
    /* [out] */ HRESULT __RPC_FAR *phr);


void __RPC_STUB ICall_GetServerHR_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);



#endif 	/* __ICall_INTERFACE_DEFINED__ */


#ifndef __IRpcCall_INTERFACE_DEFINED__
#define __IRpcCall_INTERFACE_DEFINED__

/* interface IRpcCall */
/* [unique][uuid][object][local] */ 

typedef /* [unique] */ IRpcCall __RPC_FAR *LPRPCCALL;


EXTERN_C const IID IID_IRpcCall;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("000001c5-0000-0000-C000-000000000046")
    IRpcCall : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE GetRpcOleMessage( 
            /* [out] */ RPCOLEMESSAGE __RPC_FAR *__RPC_FAR *ppMessage) = 0;
        
    };
    
#else 	/* C style interface */

    typedef struct IRpcCallVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE __RPC_FAR *QueryInterface )( 
            IRpcCall __RPC_FAR * This,
            /* [in] */ REFIID riid,
            /* [iid_is][out] */ void __RPC_FAR *__RPC_FAR *ppvObject);
        
        ULONG ( STDMETHODCALLTYPE __RPC_FAR *AddRef )( 
            IRpcCall __RPC_FAR * This);
        
        ULONG ( STDMETHODCALLTYPE __RPC_FAR *Release )( 
            IRpcCall __RPC_FAR * This);
        
        HRESULT ( STDMETHODCALLTYPE __RPC_FAR *GetRpcOleMessage )( 
            IRpcCall __RPC_FAR * This,
            /* [out] */ RPCOLEMESSAGE __RPC_FAR *__RPC_FAR *ppMessage);
        
        END_INTERFACE
    } IRpcCallVtbl;

    interface IRpcCall
    {
        CONST_VTBL struct IRpcCallVtbl __RPC_FAR *lpVtbl;
    };

    

#ifdef COBJMACROS


#define IRpcCall_QueryInterface(This,riid,ppvObject)	\
    (This)->lpVtbl -> QueryInterface(This,riid,ppvObject)

#define IRpcCall_AddRef(This)	\
    (This)->lpVtbl -> AddRef(This)

#define IRpcCall_Release(This)	\
    (This)->lpVtbl -> Release(This)


#define IRpcCall_GetRpcOleMessage(This,ppMessage)	\
    (This)->lpVtbl -> GetRpcOleMessage(This,ppMessage)

#endif /* COBJMACROS */


#endif 	/* C style interface */



HRESULT STDMETHODCALLTYPE IRpcCall_GetRpcOleMessage_Proxy( 
    IRpcCall __RPC_FAR * This,
    /* [out] */ RPCOLEMESSAGE __RPC_FAR *__RPC_FAR *ppMessage);


void __RPC_STUB IRpcCall_GetRpcOleMessage_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);



#endif 	/* __IRpcCall_INTERFACE_DEFINED__ */


/* interface __MIDL_itf_contxt_0083 */
/* [local] */ 

typedef 
enum _CALLSOURCE
    {	CALLSOURCE_CROSSAPT	= 0,
	CALLSOURCE_CROSSCTX	= 1
    }	CALLSOURCE;



extern RPC_IF_HANDLE __MIDL_itf_contxt_0083_v0_0_c_ifspec;
extern RPC_IF_HANDLE __MIDL_itf_contxt_0083_v0_0_s_ifspec;

#ifndef __ICallInfo_INTERFACE_DEFINED__
#define __ICallInfo_INTERFACE_DEFINED__

/* interface ICallInfo */
/* [unique][uuid][object][local] */ 


EXTERN_C const IID IID_ICallInfo;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("000001dc-0000-0000-C000-000000000046")
    ICallInfo : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE GetCallSource( 
            /* [out] */ CALLSOURCE __RPC_FAR *pCallSource) = 0;
        
    };
    
#else 	/* C style interface */

    typedef struct ICallInfoVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE __RPC_FAR *QueryInterface )( 
            ICallInfo __RPC_FAR * This,
            /* [in] */ REFIID riid,
            /* [iid_is][out] */ void __RPC_FAR *__RPC_FAR *ppvObject);
        
        ULONG ( STDMETHODCALLTYPE __RPC_FAR *AddRef )( 
            ICallInfo __RPC_FAR * This);
        
        ULONG ( STDMETHODCALLTYPE __RPC_FAR *Release )( 
            ICallInfo __RPC_FAR * This);
        
        HRESULT ( STDMETHODCALLTYPE __RPC_FAR *GetCallSource )( 
            ICallInfo __RPC_FAR * This,
            /* [out] */ CALLSOURCE __RPC_FAR *pCallSource);
        
        END_INTERFACE
    } ICallInfoVtbl;

    interface ICallInfo
    {
        CONST_VTBL struct ICallInfoVtbl __RPC_FAR *lpVtbl;
    };

    

#ifdef COBJMACROS


#define ICallInfo_QueryInterface(This,riid,ppvObject)	\
    (This)->lpVtbl -> QueryInterface(This,riid,ppvObject)

#define ICallInfo_AddRef(This)	\
    (This)->lpVtbl -> AddRef(This)

#define ICallInfo_Release(This)	\
    (This)->lpVtbl -> Release(This)


#define ICallInfo_GetCallSource(This,pCallSource)	\
    (This)->lpVtbl -> GetCallSource(This,pCallSource)

#endif /* COBJMACROS */


#endif 	/* C style interface */



HRESULT STDMETHODCALLTYPE ICallInfo_GetCallSource_Proxy( 
    ICallInfo __RPC_FAR * This,
    /* [out] */ CALLSOURCE __RPC_FAR *pCallSource);


void __RPC_STUB ICallInfo_GetCallSource_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);



#endif 	/* __ICallInfo_INTERFACE_DEFINED__ */


#ifndef __IPolicy_INTERFACE_DEFINED__
#define __IPolicy_INTERFACE_DEFINED__

/* interface IPolicy */
/* [unique][uuid][object][local] */ 


EXTERN_C const IID IID_IPolicy;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("000001c2-0000-0000-C000-000000000046")
    IPolicy : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE Call( 
            /* [in] */ ICall __RPC_FAR *pCall) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE Enter( 
            /* [in] */ ICall __RPC_FAR *pCall) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE Leave( 
            /* [in] */ ICall __RPC_FAR *pCall) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE Return( 
            /* [in] */ ICall __RPC_FAR *pCall) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE CallGetSize( 
            /* [in] */ ICall __RPC_FAR *pCall,
            /* [out] */ ULONG __RPC_FAR *pcb) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE CallFillBuffer( 
            /* [in] */ ICall __RPC_FAR *pCall,
            /* [in] */ void __RPC_FAR *pvBuf,
            /* [out] */ ULONG __RPC_FAR *pcb) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE EnterWithBuffer( 
            /* [in] */ ICall __RPC_FAR *pCall,
            /* [in] */ void __RPC_FAR *pvBuf,
            /* [in] */ ULONG cb) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE LeaveGetSize( 
            /* [in] */ ICall __RPC_FAR *pCall,
            /* [out] */ ULONG __RPC_FAR *pcb) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE LeaveFillBuffer( 
            /* [in] */ ICall __RPC_FAR *pCall,
            /* [in] */ void __RPC_FAR *pvBuf,
            /* [out] */ ULONG __RPC_FAR *pcb) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE ReturnWithBuffer( 
            /* [in] */ ICall __RPC_FAR *pCall,
            /* [in] */ void __RPC_FAR *pvBuf,
            /* [in] */ ULONG cb) = 0;
        
        virtual ULONG STDMETHODCALLTYPE AddRefPolicy( void) = 0;
        
        virtual ULONG STDMETHODCALLTYPE ReleasePolicy( void) = 0;
        
    };
    
#else 	/* C style interface */

    typedef struct IPolicyVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE __RPC_FAR *QueryInterface )( 
            IPolicy __RPC_FAR * This,
            /* [in] */ REFIID riid,
            /* [iid_is][out] */ void __RPC_FAR *__RPC_FAR *ppvObject);
        
        ULONG ( STDMETHODCALLTYPE __RPC_FAR *AddRef )( 
            IPolicy __RPC_FAR * This);
        
        ULONG ( STDMETHODCALLTYPE __RPC_FAR *Release )( 
            IPolicy __RPC_FAR * This);
        
        HRESULT ( STDMETHODCALLTYPE __RPC_FAR *Call )( 
            IPolicy __RPC_FAR * This,
            /* [in] */ ICall __RPC_FAR *pCall);
        
        HRESULT ( STDMETHODCALLTYPE __RPC_FAR *Enter )( 
            IPolicy __RPC_FAR * This,
            /* [in] */ ICall __RPC_FAR *pCall);
        
        HRESULT ( STDMETHODCALLTYPE __RPC_FAR *Leave )( 
            IPolicy __RPC_FAR * This,
            /* [in] */ ICall __RPC_FAR *pCall);
        
        HRESULT ( STDMETHODCALLTYPE __RPC_FAR *Return )( 
            IPolicy __RPC_FAR * This,
            /* [in] */ ICall __RPC_FAR *pCall);
        
        HRESULT ( STDMETHODCALLTYPE __RPC_FAR *CallGetSize )( 
            IPolicy __RPC_FAR * This,
            /* [in] */ ICall __RPC_FAR *pCall,
            /* [out] */ ULONG __RPC_FAR *pcb);
        
        HRESULT ( STDMETHODCALLTYPE __RPC_FAR *CallFillBuffer )( 
            IPolicy __RPC_FAR * This,
            /* [in] */ ICall __RPC_FAR *pCall,
            /* [in] */ void __RPC_FAR *pvBuf,
            /* [out] */ ULONG __RPC_FAR *pcb);
        
        HRESULT ( STDMETHODCALLTYPE __RPC_FAR *EnterWithBuffer )( 
            IPolicy __RPC_FAR * This,
            /* [in] */ ICall __RPC_FAR *pCall,
            /* [in] */ void __RPC_FAR *pvBuf,
            /* [in] */ ULONG cb);
        
        HRESULT ( STDMETHODCALLTYPE __RPC_FAR *LeaveGetSize )( 
            IPolicy __RPC_FAR * This,
            /* [in] */ ICall __RPC_FAR *pCall,
            /* [out] */ ULONG __RPC_FAR *pcb);
        
        HRESULT ( STDMETHODCALLTYPE __RPC_FAR *LeaveFillBuffer )( 
            IPolicy __RPC_FAR * This,
            /* [in] */ ICall __RPC_FAR *pCall,
            /* [in] */ void __RPC_FAR *pvBuf,
            /* [out] */ ULONG __RPC_FAR *pcb);
        
        HRESULT ( STDMETHODCALLTYPE __RPC_FAR *ReturnWithBuffer )( 
            IPolicy __RPC_FAR * This,
            /* [in] */ ICall __RPC_FAR *pCall,
            /* [in] */ void __RPC_FAR *pvBuf,
            /* [in] */ ULONG cb);
        
        ULONG ( STDMETHODCALLTYPE __RPC_FAR *AddRefPolicy )( 
            IPolicy __RPC_FAR * This);
        
        ULONG ( STDMETHODCALLTYPE __RPC_FAR *ReleasePolicy )( 
            IPolicy __RPC_FAR * This);
        
        END_INTERFACE
    } IPolicyVtbl;

    interface IPolicy
    {
        CONST_VTBL struct IPolicyVtbl __RPC_FAR *lpVtbl;
    };

    

#ifdef COBJMACROS


#define IPolicy_QueryInterface(This,riid,ppvObject)	\
    (This)->lpVtbl -> QueryInterface(This,riid,ppvObject)

#define IPolicy_AddRef(This)	\
    (This)->lpVtbl -> AddRef(This)

#define IPolicy_Release(This)	\
    (This)->lpVtbl -> Release(This)


#define IPolicy_Call(This,pCall)	\
    (This)->lpVtbl -> Call(This,pCall)

#define IPolicy_Enter(This,pCall)	\
    (This)->lpVtbl -> Enter(This,pCall)

#define IPolicy_Leave(This,pCall)	\
    (This)->lpVtbl -> Leave(This,pCall)

#define IPolicy_Return(This,pCall)	\
    (This)->lpVtbl -> Return(This,pCall)

#define IPolicy_CallGetSize(This,pCall,pcb)	\
    (This)->lpVtbl -> CallGetSize(This,pCall,pcb)

#define IPolicy_CallFillBuffer(This,pCall,pvBuf,pcb)	\
    (This)->lpVtbl -> CallFillBuffer(This,pCall,pvBuf,pcb)

#define IPolicy_EnterWithBuffer(This,pCall,pvBuf,cb)	\
    (This)->lpVtbl -> EnterWithBuffer(This,pCall,pvBuf,cb)

#define IPolicy_LeaveGetSize(This,pCall,pcb)	\
    (This)->lpVtbl -> LeaveGetSize(This,pCall,pcb)

#define IPolicy_LeaveFillBuffer(This,pCall,pvBuf,pcb)	\
    (This)->lpVtbl -> LeaveFillBuffer(This,pCall,pvBuf,pcb)

#define IPolicy_ReturnWithBuffer(This,pCall,pvBuf,cb)	\
    (This)->lpVtbl -> ReturnWithBuffer(This,pCall,pvBuf,cb)

#define IPolicy_AddRefPolicy(This)	\
    (This)->lpVtbl -> AddRefPolicy(This)

#define IPolicy_ReleasePolicy(This)	\
    (This)->lpVtbl -> ReleasePolicy(This)

#endif /* COBJMACROS */


#endif 	/* C style interface */



HRESULT STDMETHODCALLTYPE IPolicy_Call_Proxy( 
    IPolicy __RPC_FAR * This,
    /* [in] */ ICall __RPC_FAR *pCall);


void __RPC_STUB IPolicy_Call_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IPolicy_Enter_Proxy( 
    IPolicy __RPC_FAR * This,
    /* [in] */ ICall __RPC_FAR *pCall);


void __RPC_STUB IPolicy_Enter_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IPolicy_Leave_Proxy( 
    IPolicy __RPC_FAR * This,
    /* [in] */ ICall __RPC_FAR *pCall);


void __RPC_STUB IPolicy_Leave_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IPolicy_Return_Proxy( 
    IPolicy __RPC_FAR * This,
    /* [in] */ ICall __RPC_FAR *pCall);


void __RPC_STUB IPolicy_Return_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IPolicy_CallGetSize_Proxy( 
    IPolicy __RPC_FAR * This,
    /* [in] */ ICall __RPC_FAR *pCall,
    /* [out] */ ULONG __RPC_FAR *pcb);


void __RPC_STUB IPolicy_CallGetSize_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IPolicy_CallFillBuffer_Proxy( 
    IPolicy __RPC_FAR * This,
    /* [in] */ ICall __RPC_FAR *pCall,
    /* [in] */ void __RPC_FAR *pvBuf,
    /* [out] */ ULONG __RPC_FAR *pcb);


void __RPC_STUB IPolicy_CallFillBuffer_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IPolicy_EnterWithBuffer_Proxy( 
    IPolicy __RPC_FAR * This,
    /* [in] */ ICall __RPC_FAR *pCall,
    /* [in] */ void __RPC_FAR *pvBuf,
    /* [in] */ ULONG cb);


void __RPC_STUB IPolicy_EnterWithBuffer_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IPolicy_LeaveGetSize_Proxy( 
    IPolicy __RPC_FAR * This,
    /* [in] */ ICall __RPC_FAR *pCall,
    /* [out] */ ULONG __RPC_FAR *pcb);


void __RPC_STUB IPolicy_LeaveGetSize_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IPolicy_LeaveFillBuffer_Proxy( 
    IPolicy __RPC_FAR * This,
    /* [in] */ ICall __RPC_FAR *pCall,
    /* [in] */ void __RPC_FAR *pvBuf,
    /* [out] */ ULONG __RPC_FAR *pcb);


void __RPC_STUB IPolicy_LeaveFillBuffer_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IPolicy_ReturnWithBuffer_Proxy( 
    IPolicy __RPC_FAR * This,
    /* [in] */ ICall __RPC_FAR *pCall,
    /* [in] */ void __RPC_FAR *pvBuf,
    /* [in] */ ULONG cb);


void __RPC_STUB IPolicy_ReturnWithBuffer_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


ULONG STDMETHODCALLTYPE IPolicy_AddRefPolicy_Proxy( 
    IPolicy __RPC_FAR * This);


void __RPC_STUB IPolicy_AddRefPolicy_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


ULONG STDMETHODCALLTYPE IPolicy_ReleasePolicy_Proxy( 
    IPolicy __RPC_FAR * This);


void __RPC_STUB IPolicy_ReleasePolicy_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);



#endif 	/* __IPolicy_INTERFACE_DEFINED__ */


#ifndef __IPolicyAsync_INTERFACE_DEFINED__
#define __IPolicyAsync_INTERFACE_DEFINED__

/* interface IPolicyAsync */
/* [unique][uuid][object][local] */ 


EXTERN_C const IID IID_IPolicyAsync;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("000001cd-0000-0000-C000-000000000046")
    IPolicyAsync : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE BeginCallGetSize( 
            /* [in] */ ICall __RPC_FAR *pCall,
            /* [out] */ ULONG __RPC_FAR *pcb) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE BeginCall( 
            /* [in] */ ICall __RPC_FAR *pCall) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE BeginCallFillBuffer( 
            /* [in] */ ICall __RPC_FAR *pCall,
            /* [in] */ void __RPC_FAR *pvBuf,
            /* [out] */ ULONG __RPC_FAR *pcb) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE BeginEnter( 
            /* [in] */ ICall __RPC_FAR *pCall) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE BeginEnterWithBuffer( 
            /* [in] */ ICall __RPC_FAR *pCall,
            /* [in] */ void __RPC_FAR *pvBuf,
            /* [in] */ ULONG cb) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE BeginLeave( 
            /* [in] */ ICall __RPC_FAR *pCall) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE BeginReturn( 
            /* [in] */ ICall __RPC_FAR *pCall) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE FinishCall( 
            /* [in] */ ICall __RPC_FAR *pCall) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE FinishEnter( 
            /* [in] */ ICall __RPC_FAR *pCall) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE FinishLeaveGetSize( 
            /* [in] */ ICall __RPC_FAR *pCall,
            /* [out] */ ULONG __RPC_FAR *pcb) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE FinishLeave( 
            /* [in] */ ICall __RPC_FAR *pCall) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE FinishLeaveFillBuffer( 
            /* [in] */ ICall __RPC_FAR *pCall,
            /* [in] */ void __RPC_FAR *pvBuf,
            /* [out] */ ULONG __RPC_FAR *pcb) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE FinishReturn( 
            /* [in] */ ICall __RPC_FAR *pCall) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE FinishReturnWithBuffer( 
            /* [in] */ ICall __RPC_FAR *pCall,
            /* [in] */ void __RPC_FAR *pvBuf,
            /* [in] */ ULONG cb) = 0;
        
    };
    
#else 	/* C style interface */

    typedef struct IPolicyAsyncVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE __RPC_FAR *QueryInterface )( 
            IPolicyAsync __RPC_FAR * This,
            /* [in] */ REFIID riid,
            /* [iid_is][out] */ void __RPC_FAR *__RPC_FAR *ppvObject);
        
        ULONG ( STDMETHODCALLTYPE __RPC_FAR *AddRef )( 
            IPolicyAsync __RPC_FAR * This);
        
        ULONG ( STDMETHODCALLTYPE __RPC_FAR *Release )( 
            IPolicyAsync __RPC_FAR * This);
        
        HRESULT ( STDMETHODCALLTYPE __RPC_FAR *BeginCallGetSize )( 
            IPolicyAsync __RPC_FAR * This,
            /* [in] */ ICall __RPC_FAR *pCall,
            /* [out] */ ULONG __RPC_FAR *pcb);
        
        HRESULT ( STDMETHODCALLTYPE __RPC_FAR *BeginCall )( 
            IPolicyAsync __RPC_FAR * This,
            /* [in] */ ICall __RPC_FAR *pCall);
        
        HRESULT ( STDMETHODCALLTYPE __RPC_FAR *BeginCallFillBuffer )( 
            IPolicyAsync __RPC_FAR * This,
            /* [in] */ ICall __RPC_FAR *pCall,
            /* [in] */ void __RPC_FAR *pvBuf,
            /* [out] */ ULONG __RPC_FAR *pcb);
        
        HRESULT ( STDMETHODCALLTYPE __RPC_FAR *BeginEnter )( 
            IPolicyAsync __RPC_FAR * This,
            /* [in] */ ICall __RPC_FAR *pCall);
        
        HRESULT ( STDMETHODCALLTYPE __RPC_FAR *BeginEnterWithBuffer )( 
            IPolicyAsync __RPC_FAR * This,
            /* [in] */ ICall __RPC_FAR *pCall,
            /* [in] */ void __RPC_FAR *pvBuf,
            /* [in] */ ULONG cb);
        
        HRESULT ( STDMETHODCALLTYPE __RPC_FAR *BeginLeave )( 
            IPolicyAsync __RPC_FAR * This,
            /* [in] */ ICall __RPC_FAR *pCall);
        
        HRESULT ( STDMETHODCALLTYPE __RPC_FAR *BeginReturn )( 
            IPolicyAsync __RPC_FAR * This,
            /* [in] */ ICall __RPC_FAR *pCall);
        
        HRESULT ( STDMETHODCALLTYPE __RPC_FAR *FinishCall )( 
            IPolicyAsync __RPC_FAR * This,
            /* [in] */ ICall __RPC_FAR *pCall);
        
        HRESULT ( STDMETHODCALLTYPE __RPC_FAR *FinishEnter )( 
            IPolicyAsync __RPC_FAR * This,
            /* [in] */ ICall __RPC_FAR *pCall);
        
        HRESULT ( STDMETHODCALLTYPE __RPC_FAR *FinishLeaveGetSize )( 
            IPolicyAsync __RPC_FAR * This,
            /* [in] */ ICall __RPC_FAR *pCall,
            /* [out] */ ULONG __RPC_FAR *pcb);
        
        HRESULT ( STDMETHODCALLTYPE __RPC_FAR *FinishLeave )( 
            IPolicyAsync __RPC_FAR * This,
            /* [in] */ ICall __RPC_FAR *pCall);
        
        HRESULT ( STDMETHODCALLTYPE __RPC_FAR *FinishLeaveFillBuffer )( 
            IPolicyAsync __RPC_FAR * This,
            /* [in] */ ICall __RPC_FAR *pCall,
            /* [in] */ void __RPC_FAR *pvBuf,
            /* [out] */ ULONG __RPC_FAR *pcb);
        
        HRESULT ( STDMETHODCALLTYPE __RPC_FAR *FinishReturn )( 
            IPolicyAsync __RPC_FAR * This,
            /* [in] */ ICall __RPC_FAR *pCall);
        
        HRESULT ( STDMETHODCALLTYPE __RPC_FAR *FinishReturnWithBuffer )( 
            IPolicyAsync __RPC_FAR * This,
            /* [in] */ ICall __RPC_FAR *pCall,
            /* [in] */ void __RPC_FAR *pvBuf,
            /* [in] */ ULONG cb);
        
        END_INTERFACE
    } IPolicyAsyncVtbl;

    interface IPolicyAsync
    {
        CONST_VTBL struct IPolicyAsyncVtbl __RPC_FAR *lpVtbl;
    };

    

#ifdef COBJMACROS


#define IPolicyAsync_QueryInterface(This,riid,ppvObject)	\
    (This)->lpVtbl -> QueryInterface(This,riid,ppvObject)

#define IPolicyAsync_AddRef(This)	\
    (This)->lpVtbl -> AddRef(This)

#define IPolicyAsync_Release(This)	\
    (This)->lpVtbl -> Release(This)


#define IPolicyAsync_BeginCallGetSize(This,pCall,pcb)	\
    (This)->lpVtbl -> BeginCallGetSize(This,pCall,pcb)

#define IPolicyAsync_BeginCall(This,pCall)	\
    (This)->lpVtbl -> BeginCall(This,pCall)

#define IPolicyAsync_BeginCallFillBuffer(This,pCall,pvBuf,pcb)	\
    (This)->lpVtbl -> BeginCallFillBuffer(This,pCall,pvBuf,pcb)

#define IPolicyAsync_BeginEnter(This,pCall)	\
    (This)->lpVtbl -> BeginEnter(This,pCall)

#define IPolicyAsync_BeginEnterWithBuffer(This,pCall,pvBuf,cb)	\
    (This)->lpVtbl -> BeginEnterWithBuffer(This,pCall,pvBuf,cb)

#define IPolicyAsync_BeginLeave(This,pCall)	\
    (This)->lpVtbl -> BeginLeave(This,pCall)

#define IPolicyAsync_BeginReturn(This,pCall)	\
    (This)->lpVtbl -> BeginReturn(This,pCall)

#define IPolicyAsync_FinishCall(This,pCall)	\
    (This)->lpVtbl -> FinishCall(This,pCall)

#define IPolicyAsync_FinishEnter(This,pCall)	\
    (This)->lpVtbl -> FinishEnter(This,pCall)

#define IPolicyAsync_FinishLeaveGetSize(This,pCall,pcb)	\
    (This)->lpVtbl -> FinishLeaveGetSize(This,pCall,pcb)

#define IPolicyAsync_FinishLeave(This,pCall)	\
    (This)->lpVtbl -> FinishLeave(This,pCall)

#define IPolicyAsync_FinishLeaveFillBuffer(This,pCall,pvBuf,pcb)	\
    (This)->lpVtbl -> FinishLeaveFillBuffer(This,pCall,pvBuf,pcb)

#define IPolicyAsync_FinishReturn(This,pCall)	\
    (This)->lpVtbl -> FinishReturn(This,pCall)

#define IPolicyAsync_FinishReturnWithBuffer(This,pCall,pvBuf,cb)	\
    (This)->lpVtbl -> FinishReturnWithBuffer(This,pCall,pvBuf,cb)

#endif /* COBJMACROS */


#endif 	/* C style interface */



HRESULT STDMETHODCALLTYPE IPolicyAsync_BeginCallGetSize_Proxy( 
    IPolicyAsync __RPC_FAR * This,
    /* [in] */ ICall __RPC_FAR *pCall,
    /* [out] */ ULONG __RPC_FAR *pcb);


void __RPC_STUB IPolicyAsync_BeginCallGetSize_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IPolicyAsync_BeginCall_Proxy( 
    IPolicyAsync __RPC_FAR * This,
    /* [in] */ ICall __RPC_FAR *pCall);


void __RPC_STUB IPolicyAsync_BeginCall_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IPolicyAsync_BeginCallFillBuffer_Proxy( 
    IPolicyAsync __RPC_FAR * This,
    /* [in] */ ICall __RPC_FAR *pCall,
    /* [in] */ void __RPC_FAR *pvBuf,
    /* [out] */ ULONG __RPC_FAR *pcb);


void __RPC_STUB IPolicyAsync_BeginCallFillBuffer_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IPolicyAsync_BeginEnter_Proxy( 
    IPolicyAsync __RPC_FAR * This,
    /* [in] */ ICall __RPC_FAR *pCall);


void __RPC_STUB IPolicyAsync_BeginEnter_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IPolicyAsync_BeginEnterWithBuffer_Proxy( 
    IPolicyAsync __RPC_FAR * This,
    /* [in] */ ICall __RPC_FAR *pCall,
    /* [in] */ void __RPC_FAR *pvBuf,
    /* [in] */ ULONG cb);


void __RPC_STUB IPolicyAsync_BeginEnterWithBuffer_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IPolicyAsync_BeginLeave_Proxy( 
    IPolicyAsync __RPC_FAR * This,
    /* [in] */ ICall __RPC_FAR *pCall);


void __RPC_STUB IPolicyAsync_BeginLeave_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IPolicyAsync_BeginReturn_Proxy( 
    IPolicyAsync __RPC_FAR * This,
    /* [in] */ ICall __RPC_FAR *pCall);


void __RPC_STUB IPolicyAsync_BeginReturn_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IPolicyAsync_FinishCall_Proxy( 
    IPolicyAsync __RPC_FAR * This,
    /* [in] */ ICall __RPC_FAR *pCall);


void __RPC_STUB IPolicyAsync_FinishCall_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IPolicyAsync_FinishEnter_Proxy( 
    IPolicyAsync __RPC_FAR * This,
    /* [in] */ ICall __RPC_FAR *pCall);


void __RPC_STUB IPolicyAsync_FinishEnter_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IPolicyAsync_FinishLeaveGetSize_Proxy( 
    IPolicyAsync __RPC_FAR * This,
    /* [in] */ ICall __RPC_FAR *pCall,
    /* [out] */ ULONG __RPC_FAR *pcb);


void __RPC_STUB IPolicyAsync_FinishLeaveGetSize_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IPolicyAsync_FinishLeave_Proxy( 
    IPolicyAsync __RPC_FAR * This,
    /* [in] */ ICall __RPC_FAR *pCall);


void __RPC_STUB IPolicyAsync_FinishLeave_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IPolicyAsync_FinishLeaveFillBuffer_Proxy( 
    IPolicyAsync __RPC_FAR * This,
    /* [in] */ ICall __RPC_FAR *pCall,
    /* [in] */ void __RPC_FAR *pvBuf,
    /* [out] */ ULONG __RPC_FAR *pcb);


void __RPC_STUB IPolicyAsync_FinishLeaveFillBuffer_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IPolicyAsync_FinishReturn_Proxy( 
    IPolicyAsync __RPC_FAR * This,
    /* [in] */ ICall __RPC_FAR *pCall);


void __RPC_STUB IPolicyAsync_FinishReturn_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IPolicyAsync_FinishReturnWithBuffer_Proxy( 
    IPolicyAsync __RPC_FAR * This,
    /* [in] */ ICall __RPC_FAR *pCall,
    /* [in] */ void __RPC_FAR *pvBuf,
    /* [in] */ ULONG cb);


void __RPC_STUB IPolicyAsync_FinishReturnWithBuffer_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);



#endif 	/* __IPolicyAsync_INTERFACE_DEFINED__ */


#ifndef __IPolicySet_INTERFACE_DEFINED__
#define __IPolicySet_INTERFACE_DEFINED__

/* interface IPolicySet */
/* [unique][uuid][object][local] */ 


EXTERN_C const IID IID_IPolicySet;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("000001c3-0000-0000-C000-000000000046")
    IPolicySet : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE AddPolicy( 
            /* [in] */ ContextEvent ctxEvent,
            /* [in] */ REFGUID rguid,
            /* [in] */ IPolicy __RPC_FAR *pPolicy) = 0;
        
    };
    
#else 	/* C style interface */

    typedef struct IPolicySetVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE __RPC_FAR *QueryInterface )( 
            IPolicySet __RPC_FAR * This,
            /* [in] */ REFIID riid,
            /* [iid_is][out] */ void __RPC_FAR *__RPC_FAR *ppvObject);
        
        ULONG ( STDMETHODCALLTYPE __RPC_FAR *AddRef )( 
            IPolicySet __RPC_FAR * This);
        
        ULONG ( STDMETHODCALLTYPE __RPC_FAR *Release )( 
            IPolicySet __RPC_FAR * This);
        
        HRESULT ( STDMETHODCALLTYPE __RPC_FAR *AddPolicy )( 
            IPolicySet __RPC_FAR * This,
            /* [in] */ ContextEvent ctxEvent,
            /* [in] */ REFGUID rguid,
            /* [in] */ IPolicy __RPC_FAR *pPolicy);
        
        END_INTERFACE
    } IPolicySetVtbl;

    interface IPolicySet
    {
        CONST_VTBL struct IPolicySetVtbl __RPC_FAR *lpVtbl;
    };

    

#ifdef COBJMACROS


#define IPolicySet_QueryInterface(This,riid,ppvObject)	\
    (This)->lpVtbl -> QueryInterface(This,riid,ppvObject)

#define IPolicySet_AddRef(This)	\
    (This)->lpVtbl -> AddRef(This)

#define IPolicySet_Release(This)	\
    (This)->lpVtbl -> Release(This)


#define IPolicySet_AddPolicy(This,ctxEvent,rguid,pPolicy)	\
    (This)->lpVtbl -> AddPolicy(This,ctxEvent,rguid,pPolicy)

#endif /* COBJMACROS */


#endif 	/* C style interface */



HRESULT STDMETHODCALLTYPE IPolicySet_AddPolicy_Proxy( 
    IPolicySet __RPC_FAR * This,
    /* [in] */ ContextEvent ctxEvent,
    /* [in] */ REFGUID rguid,
    /* [in] */ IPolicy __RPC_FAR *pPolicy);


void __RPC_STUB IPolicySet_AddPolicy_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);



#endif 	/* __IPolicySet_INTERFACE_DEFINED__ */


#ifndef __IComObjIdentity_INTERFACE_DEFINED__
#define __IComObjIdentity_INTERFACE_DEFINED__

/* interface IComObjIdentity */
/* [unique][uuid][object][local] */ 


EXTERN_C const IID IID_IComObjIdentity;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("000001d7-0000-0000-C000-000000000046")
    IComObjIdentity : public IUnknown
    {
    public:
        virtual BOOL STDMETHODCALLTYPE IsServer( void) = 0;
        
        virtual BOOL STDMETHODCALLTYPE IsDeactivated( void) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetIdentity( 
            /* [out] */ IUnknown __RPC_FAR *__RPC_FAR *ppUnk) = 0;
        
    };
    
#else 	/* C style interface */

    typedef struct IComObjIdentityVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE __RPC_FAR *QueryInterface )( 
            IComObjIdentity __RPC_FAR * This,
            /* [in] */ REFIID riid,
            /* [iid_is][out] */ void __RPC_FAR *__RPC_FAR *ppvObject);
        
        ULONG ( STDMETHODCALLTYPE __RPC_FAR *AddRef )( 
            IComObjIdentity __RPC_FAR * This);
        
        ULONG ( STDMETHODCALLTYPE __RPC_FAR *Release )( 
            IComObjIdentity __RPC_FAR * This);
        
        BOOL ( STDMETHODCALLTYPE __RPC_FAR *IsServer )( 
            IComObjIdentity __RPC_FAR * This);
        
        BOOL ( STDMETHODCALLTYPE __RPC_FAR *IsDeactivated )( 
            IComObjIdentity __RPC_FAR * This);
        
        HRESULT ( STDMETHODCALLTYPE __RPC_FAR *GetIdentity )( 
            IComObjIdentity __RPC_FAR * This,
            /* [out] */ IUnknown __RPC_FAR *__RPC_FAR *ppUnk);
        
        END_INTERFACE
    } IComObjIdentityVtbl;

    interface IComObjIdentity
    {
        CONST_VTBL struct IComObjIdentityVtbl __RPC_FAR *lpVtbl;
    };

    

#ifdef COBJMACROS


#define IComObjIdentity_QueryInterface(This,riid,ppvObject)	\
    (This)->lpVtbl -> QueryInterface(This,riid,ppvObject)

#define IComObjIdentity_AddRef(This)	\
    (This)->lpVtbl -> AddRef(This)

#define IComObjIdentity_Release(This)	\
    (This)->lpVtbl -> Release(This)


#define IComObjIdentity_IsServer(This)	\
    (This)->lpVtbl -> IsServer(This)

#define IComObjIdentity_IsDeactivated(This)	\
    (This)->lpVtbl -> IsDeactivated(This)

#define IComObjIdentity_GetIdentity(This,ppUnk)	\
    (This)->lpVtbl -> GetIdentity(This,ppUnk)

#endif /* COBJMACROS */


#endif 	/* C style interface */



BOOL STDMETHODCALLTYPE IComObjIdentity_IsServer_Proxy( 
    IComObjIdentity __RPC_FAR * This);


void __RPC_STUB IComObjIdentity_IsServer_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


BOOL STDMETHODCALLTYPE IComObjIdentity_IsDeactivated_Proxy( 
    IComObjIdentity __RPC_FAR * This);


void __RPC_STUB IComObjIdentity_IsDeactivated_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IComObjIdentity_GetIdentity_Proxy( 
    IComObjIdentity __RPC_FAR * This,
    /* [out] */ IUnknown __RPC_FAR *__RPC_FAR *ppUnk);


void __RPC_STUB IComObjIdentity_GetIdentity_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);



#endif 	/* __IComObjIdentity_INTERFACE_DEFINED__ */


#ifndef __IPolicyMaker_INTERFACE_DEFINED__
#define __IPolicyMaker_INTERFACE_DEFINED__

/* interface IPolicyMaker */
/* [unique][uuid][object][local] */ 


EXTERN_C const IID IID_IPolicyMaker;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("000001c4-0000-0000-C000-000000000046")
    IPolicyMaker : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE AddClientPoliciesToSet( 
            /* [in] */ IPolicySet __RPC_FAR *pPS,
            /* [in] */ IContext __RPC_FAR *pClientContext,
            /* [in] */ IContext __RPC_FAR *pServerContext) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE AddEnvoyPoliciesToSet( 
            /* [in] */ IPolicySet __RPC_FAR *pPS,
            /* [in] */ IContext __RPC_FAR *pClientContext,
            /* [in] */ IContext __RPC_FAR *pServerContext) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE AddServerPoliciesToSet( 
            /* [in] */ IPolicySet __RPC_FAR *pPS,
            /* [in] */ IContext __RPC_FAR *pClientContext,
            /* [in] */ IContext __RPC_FAR *pServerContext) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE Freeze( 
            /* [in] */ IObjContext __RPC_FAR *pObjContext) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE CreateStub( 
            /* [in] */ IComObjIdentity __RPC_FAR *pID) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE DestroyStub( 
            /* [in] */ IComObjIdentity __RPC_FAR *pID) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE CreateProxy( 
            /* [in] */ IComObjIdentity __RPC_FAR *pID) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE DestroyProxy( 
            /* [in] */ IComObjIdentity __RPC_FAR *pID) = 0;
        
    };
    
#else 	/* C style interface */

    typedef struct IPolicyMakerVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE __RPC_FAR *QueryInterface )( 
            IPolicyMaker __RPC_FAR * This,
            /* [in] */ REFIID riid,
            /* [iid_is][out] */ void __RPC_FAR *__RPC_FAR *ppvObject);
        
        ULONG ( STDMETHODCALLTYPE __RPC_FAR *AddRef )( 
            IPolicyMaker __RPC_FAR * This);
        
        ULONG ( STDMETHODCALLTYPE __RPC_FAR *Release )( 
            IPolicyMaker __RPC_FAR * This);
        
        HRESULT ( STDMETHODCALLTYPE __RPC_FAR *AddClientPoliciesToSet )( 
            IPolicyMaker __RPC_FAR * This,
            /* [in] */ IPolicySet __RPC_FAR *pPS,
            /* [in] */ IContext __RPC_FAR *pClientContext,
            /* [in] */ IContext __RPC_FAR *pServerContext);
        
        HRESULT ( STDMETHODCALLTYPE __RPC_FAR *AddEnvoyPoliciesToSet )( 
            IPolicyMaker __RPC_FAR * This,
            /* [in] */ IPolicySet __RPC_FAR *pPS,
            /* [in] */ IContext __RPC_FAR *pClientContext,
            /* [in] */ IContext __RPC_FAR *pServerContext);
        
        HRESULT ( STDMETHODCALLTYPE __RPC_FAR *AddServerPoliciesToSet )( 
            IPolicyMaker __RPC_FAR * This,
            /* [in] */ IPolicySet __RPC_FAR *pPS,
            /* [in] */ IContext __RPC_FAR *pClientContext,
            /* [in] */ IContext __RPC_FAR *pServerContext);
        
        HRESULT ( STDMETHODCALLTYPE __RPC_FAR *Freeze )( 
            IPolicyMaker __RPC_FAR * This,
            /* [in] */ IObjContext __RPC_FAR *pObjContext);
        
        HRESULT ( STDMETHODCALLTYPE __RPC_FAR *CreateStub )( 
            IPolicyMaker __RPC_FAR * This,
            /* [in] */ IComObjIdentity __RPC_FAR *pID);
        
        HRESULT ( STDMETHODCALLTYPE __RPC_FAR *DestroyStub )( 
            IPolicyMaker __RPC_FAR * This,
            /* [in] */ IComObjIdentity __RPC_FAR *pID);
        
        HRESULT ( STDMETHODCALLTYPE __RPC_FAR *CreateProxy )( 
            IPolicyMaker __RPC_FAR * This,
            /* [in] */ IComObjIdentity __RPC_FAR *pID);
        
        HRESULT ( STDMETHODCALLTYPE __RPC_FAR *DestroyProxy )( 
            IPolicyMaker __RPC_FAR * This,
            /* [in] */ IComObjIdentity __RPC_FAR *pID);
        
        END_INTERFACE
    } IPolicyMakerVtbl;

    interface IPolicyMaker
    {
        CONST_VTBL struct IPolicyMakerVtbl __RPC_FAR *lpVtbl;
    };

    

#ifdef COBJMACROS


#define IPolicyMaker_QueryInterface(This,riid,ppvObject)	\
    (This)->lpVtbl -> QueryInterface(This,riid,ppvObject)

#define IPolicyMaker_AddRef(This)	\
    (This)->lpVtbl -> AddRef(This)

#define IPolicyMaker_Release(This)	\
    (This)->lpVtbl -> Release(This)


#define IPolicyMaker_AddClientPoliciesToSet(This,pPS,pClientContext,pServerContext)	\
    (This)->lpVtbl -> AddClientPoliciesToSet(This,pPS,pClientContext,pServerContext)

#define IPolicyMaker_AddEnvoyPoliciesToSet(This,pPS,pClientContext,pServerContext)	\
    (This)->lpVtbl -> AddEnvoyPoliciesToSet(This,pPS,pClientContext,pServerContext)

#define IPolicyMaker_AddServerPoliciesToSet(This,pPS,pClientContext,pServerContext)	\
    (This)->lpVtbl -> AddServerPoliciesToSet(This,pPS,pClientContext,pServerContext)

#define IPolicyMaker_Freeze(This,pObjContext)	\
    (This)->lpVtbl -> Freeze(This,pObjContext)

#define IPolicyMaker_CreateStub(This,pID)	\
    (This)->lpVtbl -> CreateStub(This,pID)

#define IPolicyMaker_DestroyStub(This,pID)	\
    (This)->lpVtbl -> DestroyStub(This,pID)

#define IPolicyMaker_CreateProxy(This,pID)	\
    (This)->lpVtbl -> CreateProxy(This,pID)

#define IPolicyMaker_DestroyProxy(This,pID)	\
    (This)->lpVtbl -> DestroyProxy(This,pID)

#endif /* COBJMACROS */


#endif 	/* C style interface */



HRESULT STDMETHODCALLTYPE IPolicyMaker_AddClientPoliciesToSet_Proxy( 
    IPolicyMaker __RPC_FAR * This,
    /* [in] */ IPolicySet __RPC_FAR *pPS,
    /* [in] */ IContext __RPC_FAR *pClientContext,
    /* [in] */ IContext __RPC_FAR *pServerContext);


void __RPC_STUB IPolicyMaker_AddClientPoliciesToSet_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IPolicyMaker_AddEnvoyPoliciesToSet_Proxy( 
    IPolicyMaker __RPC_FAR * This,
    /* [in] */ IPolicySet __RPC_FAR *pPS,
    /* [in] */ IContext __RPC_FAR *pClientContext,
    /* [in] */ IContext __RPC_FAR *pServerContext);


void __RPC_STUB IPolicyMaker_AddEnvoyPoliciesToSet_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IPolicyMaker_AddServerPoliciesToSet_Proxy( 
    IPolicyMaker __RPC_FAR * This,
    /* [in] */ IPolicySet __RPC_FAR *pPS,
    /* [in] */ IContext __RPC_FAR *pClientContext,
    /* [in] */ IContext __RPC_FAR *pServerContext);


void __RPC_STUB IPolicyMaker_AddServerPoliciesToSet_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IPolicyMaker_Freeze_Proxy( 
    IPolicyMaker __RPC_FAR * This,
    /* [in] */ IObjContext __RPC_FAR *pObjContext);


void __RPC_STUB IPolicyMaker_Freeze_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IPolicyMaker_CreateStub_Proxy( 
    IPolicyMaker __RPC_FAR * This,
    /* [in] */ IComObjIdentity __RPC_FAR *pID);


void __RPC_STUB IPolicyMaker_CreateStub_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IPolicyMaker_DestroyStub_Proxy( 
    IPolicyMaker __RPC_FAR * This,
    /* [in] */ IComObjIdentity __RPC_FAR *pID);


void __RPC_STUB IPolicyMaker_DestroyStub_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IPolicyMaker_CreateProxy_Proxy( 
    IPolicyMaker __RPC_FAR * This,
    /* [in] */ IComObjIdentity __RPC_FAR *pID);


void __RPC_STUB IPolicyMaker_CreateProxy_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IPolicyMaker_DestroyProxy_Proxy( 
    IPolicyMaker __RPC_FAR * This,
    /* [in] */ IComObjIdentity __RPC_FAR *pID);


void __RPC_STUB IPolicyMaker_DestroyProxy_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);



#endif 	/* __IPolicyMaker_INTERFACE_DEFINED__ */


#ifndef __IExceptionNotification_INTERFACE_DEFINED__
#define __IExceptionNotification_INTERFACE_DEFINED__

/* interface IExceptionNotification */
/* [unique][uuid][object][local] */ 


EXTERN_C const IID IID_IExceptionNotification;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("000001db-0000-0000-C000-000000000046")
    IExceptionNotification : public IUnknown
    {
    public:
        virtual void STDMETHODCALLTYPE ServerException( 
            /* [in] */ void __RPC_FAR *pExcepPtrs) = 0;
        
    };
    
#else 	/* C style interface */

    typedef struct IExceptionNotificationVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE __RPC_FAR *QueryInterface )( 
            IExceptionNotification __RPC_FAR * This,
            /* [in] */ REFIID riid,
            /* [iid_is][out] */ void __RPC_FAR *__RPC_FAR *ppvObject);
        
        ULONG ( STDMETHODCALLTYPE __RPC_FAR *AddRef )( 
            IExceptionNotification __RPC_FAR * This);
        
        ULONG ( STDMETHODCALLTYPE __RPC_FAR *Release )( 
            IExceptionNotification __RPC_FAR * This);
        
        void ( STDMETHODCALLTYPE __RPC_FAR *ServerException )( 
            IExceptionNotification __RPC_FAR * This,
            /* [in] */ void __RPC_FAR *pExcepPtrs);
        
        END_INTERFACE
    } IExceptionNotificationVtbl;

    interface IExceptionNotification
    {
        CONST_VTBL struct IExceptionNotificationVtbl __RPC_FAR *lpVtbl;
    };

    

#ifdef COBJMACROS


#define IExceptionNotification_QueryInterface(This,riid,ppvObject)	\
    (This)->lpVtbl -> QueryInterface(This,riid,ppvObject)

#define IExceptionNotification_AddRef(This)	\
    (This)->lpVtbl -> AddRef(This)

#define IExceptionNotification_Release(This)	\
    (This)->lpVtbl -> Release(This)


#define IExceptionNotification_ServerException(This,pExcepPtrs)	\
    (This)->lpVtbl -> ServerException(This,pExcepPtrs)

#endif /* COBJMACROS */


#endif 	/* C style interface */



void STDMETHODCALLTYPE IExceptionNotification_ServerException_Proxy( 
    IExceptionNotification __RPC_FAR * This,
    /* [in] */ void __RPC_FAR *pExcepPtrs);


void __RPC_STUB IExceptionNotification_ServerException_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);



#endif 	/* __IExceptionNotification_INTERFACE_DEFINED__ */


#ifndef __IMarshalEnvoy_INTERFACE_DEFINED__
#define __IMarshalEnvoy_INTERFACE_DEFINED__

/* interface IMarshalEnvoy */
/* [unique][uuid][object][local] */ 


EXTERN_C const IID IID_IMarshalEnvoy;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("000001c8-0000-0000-C000-000000000046")
    IMarshalEnvoy : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE GetEnvoyUnmarshalClass( 
            /* [in] */ DWORD dwDestContext,
            /* [out] */ CLSID __RPC_FAR *pClsid) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetEnvoySizeMax( 
            /* [in] */ DWORD dwDestContext,
            /* [out] */ DWORD __RPC_FAR *pcb) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE MarshalEnvoy( 
            /* [in] */ IStream __RPC_FAR *pStream,
            /* [in] */ DWORD dwDestContext) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE UnmarshalEnvoy( 
            /* [in] */ IStream __RPC_FAR *pStream,
            /* [in] */ REFIID riid,
            /* [iid_is][out] */ void __RPC_FAR *__RPC_FAR *ppunk) = 0;
        
    };
    
#else 	/* C style interface */

    typedef struct IMarshalEnvoyVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE __RPC_FAR *QueryInterface )( 
            IMarshalEnvoy __RPC_FAR * This,
            /* [in] */ REFIID riid,
            /* [iid_is][out] */ void __RPC_FAR *__RPC_FAR *ppvObject);
        
        ULONG ( STDMETHODCALLTYPE __RPC_FAR *AddRef )( 
            IMarshalEnvoy __RPC_FAR * This);
        
        ULONG ( STDMETHODCALLTYPE __RPC_FAR *Release )( 
            IMarshalEnvoy __RPC_FAR * This);
        
        HRESULT ( STDMETHODCALLTYPE __RPC_FAR *GetEnvoyUnmarshalClass )( 
            IMarshalEnvoy __RPC_FAR * This,
            /* [in] */ DWORD dwDestContext,
            /* [out] */ CLSID __RPC_FAR *pClsid);
        
        HRESULT ( STDMETHODCALLTYPE __RPC_FAR *GetEnvoySizeMax )( 
            IMarshalEnvoy __RPC_FAR * This,
            /* [in] */ DWORD dwDestContext,
            /* [out] */ DWORD __RPC_FAR *pcb);
        
        HRESULT ( STDMETHODCALLTYPE __RPC_FAR *MarshalEnvoy )( 
            IMarshalEnvoy __RPC_FAR * This,
            /* [in] */ IStream __RPC_FAR *pStream,
            /* [in] */ DWORD dwDestContext);
        
        HRESULT ( STDMETHODCALLTYPE __RPC_FAR *UnmarshalEnvoy )( 
            IMarshalEnvoy __RPC_FAR * This,
            /* [in] */ IStream __RPC_FAR *pStream,
            /* [in] */ REFIID riid,
            /* [iid_is][out] */ void __RPC_FAR *__RPC_FAR *ppunk);
        
        END_INTERFACE
    } IMarshalEnvoyVtbl;

    interface IMarshalEnvoy
    {
        CONST_VTBL struct IMarshalEnvoyVtbl __RPC_FAR *lpVtbl;
    };

    

#ifdef COBJMACROS


#define IMarshalEnvoy_QueryInterface(This,riid,ppvObject)	\
    (This)->lpVtbl -> QueryInterface(This,riid,ppvObject)

#define IMarshalEnvoy_AddRef(This)	\
    (This)->lpVtbl -> AddRef(This)

#define IMarshalEnvoy_Release(This)	\
    (This)->lpVtbl -> Release(This)


#define IMarshalEnvoy_GetEnvoyUnmarshalClass(This,dwDestContext,pClsid)	\
    (This)->lpVtbl -> GetEnvoyUnmarshalClass(This,dwDestContext,pClsid)

#define IMarshalEnvoy_GetEnvoySizeMax(This,dwDestContext,pcb)	\
    (This)->lpVtbl -> GetEnvoySizeMax(This,dwDestContext,pcb)

#define IMarshalEnvoy_MarshalEnvoy(This,pStream,dwDestContext)	\
    (This)->lpVtbl -> MarshalEnvoy(This,pStream,dwDestContext)

#define IMarshalEnvoy_UnmarshalEnvoy(This,pStream,riid,ppunk)	\
    (This)->lpVtbl -> UnmarshalEnvoy(This,pStream,riid,ppunk)

#endif /* COBJMACROS */


#endif 	/* C style interface */



HRESULT STDMETHODCALLTYPE IMarshalEnvoy_GetEnvoyUnmarshalClass_Proxy( 
    IMarshalEnvoy __RPC_FAR * This,
    /* [in] */ DWORD dwDestContext,
    /* [out] */ CLSID __RPC_FAR *pClsid);


void __RPC_STUB IMarshalEnvoy_GetEnvoyUnmarshalClass_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IMarshalEnvoy_GetEnvoySizeMax_Proxy( 
    IMarshalEnvoy __RPC_FAR * This,
    /* [in] */ DWORD dwDestContext,
    /* [out] */ DWORD __RPC_FAR *pcb);


void __RPC_STUB IMarshalEnvoy_GetEnvoySizeMax_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IMarshalEnvoy_MarshalEnvoy_Proxy( 
    IMarshalEnvoy __RPC_FAR * This,
    /* [in] */ IStream __RPC_FAR *pStream,
    /* [in] */ DWORD dwDestContext);


void __RPC_STUB IMarshalEnvoy_MarshalEnvoy_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IMarshalEnvoy_UnmarshalEnvoy_Proxy( 
    IMarshalEnvoy __RPC_FAR * This,
    /* [in] */ IStream __RPC_FAR *pStream,
    /* [in] */ REFIID riid,
    /* [iid_is][out] */ void __RPC_FAR *__RPC_FAR *ppunk);


void __RPC_STUB IMarshalEnvoy_UnmarshalEnvoy_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);



#endif 	/* __IMarshalEnvoy_INTERFACE_DEFINED__ */


#ifndef __IWrapperInfo_INTERFACE_DEFINED__
#define __IWrapperInfo_INTERFACE_DEFINED__

/* interface IWrapperInfo */
/* [unique][uuid][object][local] */ 


EXTERN_C const IID IID_IWrapperInfo;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("5052f924-7ab8-11d3-b93f-00c04f990176")
    IWrapperInfo : public IUnknown
    {
    public:
        virtual void STDMETHODCALLTYPE SetMapping( 
            void __RPC_FAR *pv) = 0;
        
        virtual void __RPC_FAR *STDMETHODCALLTYPE GetMapping( void) = 0;
        
        virtual IObjContext __RPC_FAR *STDMETHODCALLTYPE GetServerObjectContext( void) = 0;
        
        virtual IUnknown __RPC_FAR *STDMETHODCALLTYPE GetServerObject( void) = 0;
        
    };
    
#else 	/* C style interface */

    typedef struct IWrapperInfoVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE __RPC_FAR *QueryInterface )( 
            IWrapperInfo __RPC_FAR * This,
            /* [in] */ REFIID riid,
            /* [iid_is][out] */ void __RPC_FAR *__RPC_FAR *ppvObject);
        
        ULONG ( STDMETHODCALLTYPE __RPC_FAR *AddRef )( 
            IWrapperInfo __RPC_FAR * This);
        
        ULONG ( STDMETHODCALLTYPE __RPC_FAR *Release )( 
            IWrapperInfo __RPC_FAR * This);
        
        void ( STDMETHODCALLTYPE __RPC_FAR *SetMapping )( 
            IWrapperInfo __RPC_FAR * This,
            void __RPC_FAR *pv);
        
        void __RPC_FAR *( STDMETHODCALLTYPE __RPC_FAR *GetMapping )( 
            IWrapperInfo __RPC_FAR * This);
        
        IObjContext __RPC_FAR *( STDMETHODCALLTYPE __RPC_FAR *GetServerObjectContext )( 
            IWrapperInfo __RPC_FAR * This);
        
        IUnknown __RPC_FAR *( STDMETHODCALLTYPE __RPC_FAR *GetServerObject )( 
            IWrapperInfo __RPC_FAR * This);
        
        END_INTERFACE
    } IWrapperInfoVtbl;

    interface IWrapperInfo
    {
        CONST_VTBL struct IWrapperInfoVtbl __RPC_FAR *lpVtbl;
    };

    

#ifdef COBJMACROS


#define IWrapperInfo_QueryInterface(This,riid,ppvObject)	\
    (This)->lpVtbl -> QueryInterface(This,riid,ppvObject)

#define IWrapperInfo_AddRef(This)	\
    (This)->lpVtbl -> AddRef(This)

#define IWrapperInfo_Release(This)	\
    (This)->lpVtbl -> Release(This)


#define IWrapperInfo_SetMapping(This,pv)	\
    (This)->lpVtbl -> SetMapping(This,pv)

#define IWrapperInfo_GetMapping(This)	\
    (This)->lpVtbl -> GetMapping(This)

#define IWrapperInfo_GetServerObjectContext(This)	\
    (This)->lpVtbl -> GetServerObjectContext(This)

#define IWrapperInfo_GetServerObject(This)	\
    (This)->lpVtbl -> GetServerObject(This)

#endif /* COBJMACROS */


#endif 	/* C style interface */



void STDMETHODCALLTYPE IWrapperInfo_SetMapping_Proxy( 
    IWrapperInfo __RPC_FAR * This,
    void __RPC_FAR *pv);


void __RPC_STUB IWrapperInfo_SetMapping_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


void __RPC_FAR *STDMETHODCALLTYPE IWrapperInfo_GetMapping_Proxy( 
    IWrapperInfo __RPC_FAR * This);


void __RPC_STUB IWrapperInfo_GetMapping_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


IObjContext __RPC_FAR *STDMETHODCALLTYPE IWrapperInfo_GetServerObjectContext_Proxy( 
    IWrapperInfo __RPC_FAR * This);


void __RPC_STUB IWrapperInfo_GetServerObjectContext_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


IUnknown __RPC_FAR *STDMETHODCALLTYPE IWrapperInfo_GetServerObject_Proxy( 
    IWrapperInfo __RPC_FAR * This);


void __RPC_STUB IWrapperInfo_GetServerObject_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);



#endif 	/* __IWrapperInfo_INTERFACE_DEFINED__ */


/* interface __MIDL_itf_contxt_0092 */
/* [local] */ 


typedef DWORD APARTMENTID;



extern RPC_IF_HANDLE __MIDL_itf_contxt_0092_v0_0_c_ifspec;
extern RPC_IF_HANDLE __MIDL_itf_contxt_0092_v0_0_s_ifspec;

#ifndef __IComThreadingInfo_INTERFACE_DEFINED__
#define __IComThreadingInfo_INTERFACE_DEFINED__

/* interface IComThreadingInfo */
/* [unique][uuid][object][local] */ 


EXTERN_C const IID IID_IComThreadingInfo;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("000001ce-0000-0000-C000-000000000046")
    IComThreadingInfo : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE GetCurrentApartmentType( 
            /* [out] */ APTTYPE __RPC_FAR *pAptType) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetCurrentThreadType( 
            /* [out] */ THDTYPE __RPC_FAR *pThreadType) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetCurrentLogicalThreadId( 
            /* [out] */ GUID __RPC_FAR *pguidLogicalThreadId) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE SetCurrentLogicalThreadId( 
            /* [in] */ REFGUID rguid) = 0;
        
    };
    
#else 	/* C style interface */

    typedef struct IComThreadingInfoVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE __RPC_FAR *QueryInterface )( 
            IComThreadingInfo __RPC_FAR * This,
            /* [in] */ REFIID riid,
            /* [iid_is][out] */ void __RPC_FAR *__RPC_FAR *ppvObject);
        
        ULONG ( STDMETHODCALLTYPE __RPC_FAR *AddRef )( 
            IComThreadingInfo __RPC_FAR * This);
        
        ULONG ( STDMETHODCALLTYPE __RPC_FAR *Release )( 
            IComThreadingInfo __RPC_FAR * This);
        
        HRESULT ( STDMETHODCALLTYPE __RPC_FAR *GetCurrentApartmentType )( 
            IComThreadingInfo __RPC_FAR * This,
            /* [out] */ APTTYPE __RPC_FAR *pAptType);
        
        HRESULT ( STDMETHODCALLTYPE __RPC_FAR *GetCurrentThreadType )( 
            IComThreadingInfo __RPC_FAR * This,
            /* [out] */ THDTYPE __RPC_FAR *pThreadType);
        
        HRESULT ( STDMETHODCALLTYPE __RPC_FAR *GetCurrentLogicalThreadId )( 
            IComThreadingInfo __RPC_FAR * This,
            /* [out] */ GUID __RPC_FAR *pguidLogicalThreadId);
        
        HRESULT ( STDMETHODCALLTYPE __RPC_FAR *SetCurrentLogicalThreadId )( 
            IComThreadingInfo __RPC_FAR * This,
            /* [in] */ REFGUID rguid);
        
        END_INTERFACE
    } IComThreadingInfoVtbl;

    interface IComThreadingInfo
    {
        CONST_VTBL struct IComThreadingInfoVtbl __RPC_FAR *lpVtbl;
    };

    

#ifdef COBJMACROS


#define IComThreadingInfo_QueryInterface(This,riid,ppvObject)	\
    (This)->lpVtbl -> QueryInterface(This,riid,ppvObject)

#define IComThreadingInfo_AddRef(This)	\
    (This)->lpVtbl -> AddRef(This)

#define IComThreadingInfo_Release(This)	\
    (This)->lpVtbl -> Release(This)


#define IComThreadingInfo_GetCurrentApartmentType(This,pAptType)	\
    (This)->lpVtbl -> GetCurrentApartmentType(This,pAptType)

#define IComThreadingInfo_GetCurrentThreadType(This,pThreadType)	\
    (This)->lpVtbl -> GetCurrentThreadType(This,pThreadType)

#define IComThreadingInfo_GetCurrentLogicalThreadId(This,pguidLogicalThreadId)	\
    (This)->lpVtbl -> GetCurrentLogicalThreadId(This,pguidLogicalThreadId)

#define IComThreadingInfo_SetCurrentLogicalThreadId(This,rguid)	\
    (This)->lpVtbl -> SetCurrentLogicalThreadId(This,rguid)

#endif /* COBJMACROS */


#endif 	/* C style interface */



HRESULT STDMETHODCALLTYPE IComThreadingInfo_GetCurrentApartmentType_Proxy( 
    IComThreadingInfo __RPC_FAR * This,
    /* [out] */ APTTYPE __RPC_FAR *pAptType);


void __RPC_STUB IComThreadingInfo_GetCurrentApartmentType_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IComThreadingInfo_GetCurrentThreadType_Proxy( 
    IComThreadingInfo __RPC_FAR * This,
    /* [out] */ THDTYPE __RPC_FAR *pThreadType);


void __RPC_STUB IComThreadingInfo_GetCurrentThreadType_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IComThreadingInfo_GetCurrentLogicalThreadId_Proxy( 
    IComThreadingInfo __RPC_FAR * This,
    /* [out] */ GUID __RPC_FAR *pguidLogicalThreadId);


void __RPC_STUB IComThreadingInfo_GetCurrentLogicalThreadId_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IComThreadingInfo_SetCurrentLogicalThreadId_Proxy( 
    IComThreadingInfo __RPC_FAR * This,
    /* [in] */ REFGUID rguid);


void __RPC_STUB IComThreadingInfo_SetCurrentLogicalThreadId_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);



#endif 	/* __IComThreadingInfo_INTERFACE_DEFINED__ */


#ifndef __IComDispatchInfo_INTERFACE_DEFINED__
#define __IComDispatchInfo_INTERFACE_DEFINED__

/* interface IComDispatchInfo */
/* [unique][uuid][object][local] */ 


EXTERN_C const IID IID_IComDispatchInfo;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("000001d9-0000-0000-C000-000000000046")
    IComDispatchInfo : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE EnableComInits( 
            /* [out] */ void __RPC_FAR *__RPC_FAR *ppvCookie) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE DisableComInits( 
            /* [in] */ void __RPC_FAR *pvCookie) = 0;
        
    };
    
#else 	/* C style interface */

    typedef struct IComDispatchInfoVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE __RPC_FAR *QueryInterface )( 
            IComDispatchInfo __RPC_FAR * This,
            /* [in] */ REFIID riid,
            /* [iid_is][out] */ void __RPC_FAR *__RPC_FAR *ppvObject);
        
        ULONG ( STDMETHODCALLTYPE __RPC_FAR *AddRef )( 
            IComDispatchInfo __RPC_FAR * This);
        
        ULONG ( STDMETHODCALLTYPE __RPC_FAR *Release )( 
            IComDispatchInfo __RPC_FAR * This);
        
        HRESULT ( STDMETHODCALLTYPE __RPC_FAR *EnableComInits )( 
            IComDispatchInfo __RPC_FAR * This,
            /* [out] */ void __RPC_FAR *__RPC_FAR *ppvCookie);
        
        HRESULT ( STDMETHODCALLTYPE __RPC_FAR *DisableComInits )( 
            IComDispatchInfo __RPC_FAR * This,
            /* [in] */ void __RPC_FAR *pvCookie);
        
        END_INTERFACE
    } IComDispatchInfoVtbl;

    interface IComDispatchInfo
    {
        CONST_VTBL struct IComDispatchInfoVtbl __RPC_FAR *lpVtbl;
    };

    

#ifdef COBJMACROS


#define IComDispatchInfo_QueryInterface(This,riid,ppvObject)	\
    (This)->lpVtbl -> QueryInterface(This,riid,ppvObject)

#define IComDispatchInfo_AddRef(This)	\
    (This)->lpVtbl -> AddRef(This)

#define IComDispatchInfo_Release(This)	\
    (This)->lpVtbl -> Release(This)


#define IComDispatchInfo_EnableComInits(This,ppvCookie)	\
    (This)->lpVtbl -> EnableComInits(This,ppvCookie)

#define IComDispatchInfo_DisableComInits(This,pvCookie)	\
    (This)->lpVtbl -> DisableComInits(This,pvCookie)

#endif /* COBJMACROS */


#endif 	/* C style interface */



HRESULT STDMETHODCALLTYPE IComDispatchInfo_EnableComInits_Proxy( 
    IComDispatchInfo __RPC_FAR * This,
    /* [out] */ void __RPC_FAR *__RPC_FAR *ppvCookie);


void __RPC_STUB IComDispatchInfo_EnableComInits_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IComDispatchInfo_DisableComInits_Proxy( 
    IComDispatchInfo __RPC_FAR * This,
    /* [in] */ void __RPC_FAR *pvCookie);


void __RPC_STUB IComDispatchInfo_DisableComInits_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);



#endif 	/* __IComDispatchInfo_INTERFACE_DEFINED__ */


/* interface __MIDL_itf_contxt_0094 */
/* [local] */ 

typedef DWORD HActivator;

STDAPI CoCreateObjectInContext(IUnknown *pUnk, IObjContext *pObjectCtx, REFIID riid, void **ppv);
STDAPI CoGetApartmentID(APTTYPE dAptType, HActivator* pAptID);
STDAPI CoDeactivateObject(IUnknown *pUnk, IUnknown **ppCookie);
STDAPI CoReactivateObject(IUnknown *pUnk, IUnknown *pCookie);
#define MSHLFLAGS_NO_IEC      0x8  // don't use IExternalConnextion
#define MSHLFLAGS_NO_IMARSHAL 0x10 // don't use IMarshal
#define CONTEXTFLAGS_FROZEN         0x01 // Frozen context
#define CONTEXTFLAGS_ALLOWUNAUTH    0x02 // Allow unauthenticated calls
#define CONTEXTFLAGS_ENVOYCONTEXT   0x04 // Envoy context
#define CONTEXTFLAGS_DEFAULTCONTEXT 0x08 // Default context
#define CONTEXTFLAGS_STATICCONTEXT  0x10 // Static context
#define CONTEXTFLAGS_INPROPTABLE    0x20 // Is in property table
#define CONTEXTFLAGS_INDESTRUCTOR   0x40 // Is in destructor
#define CONTEXTFLAGS_URTPROPPRESENT 0x80 // CLR property added


extern RPC_IF_HANDLE __MIDL_itf_contxt_0094_v0_0_c_ifspec;
extern RPC_IF_HANDLE __MIDL_itf_contxt_0094_v0_0_s_ifspec;

/* Additional Prototypes for ALL interfaces */

/* end of Additional Prototypes */

#ifdef __cplusplus
}
#endif

#endif
