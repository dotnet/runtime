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

#ifndef __clrinternal_h__
#define __clrinternal_h__

#if defined(_MSC_VER) && (_MSC_VER >= 1020)
#pragma once
#endif

/* Forward Declarations */ 

#ifndef __IPrivateManagedExceptionReporting_FWD_DEFINED__
#define __IPrivateManagedExceptionReporting_FWD_DEFINED__
typedef interface IPrivateManagedExceptionReporting IPrivateManagedExceptionReporting;

#endif 	/* __IPrivateManagedExceptionReporting_FWD_DEFINED__ */


/* header files for imported files */
#include "unknwn.h"
#include "mscoree.h"

#ifdef __cplusplus
extern "C"{
#endif 


/* interface __MIDL_itf_clrinternal_0000_0000 */
/* [local] */ 

EXTERN_GUID(CLR_ID_V4_DESKTOP, 0x267f3989, 0xd786, 0x4b9a, 0x9a, 0xf6, 0xd1, 0x9e, 0x42, 0xd5, 0x57, 0xec);
EXTERN_GUID(CLR_ID_CORECLR, 0x8CB8E075, 0x0A91, 0x408E, 0x92, 0x28, 0xD6, 0x6E, 0x00, 0xA3, 0xBF, 0xF6 );
EXTERN_GUID(CLR_ID_PHONE_CLR, 0xE7237E9C, 0x31C0, 0x488C, 0xAD, 0x48, 0x32, 0x4D, 0x3E, 0x7E, 0xD9, 0x2A);
EXTERN_GUID(CLR_ID_ONECORE_CLR, 0xb1ee760d, 0x6c4a, 0x4533, 0xba, 0x41, 0x6f, 0x4f, 0x66, 0x1f, 0xab, 0xaf);
EXTERN_GUID(IID_IPrivateManagedExceptionReporting, 0xad76a023, 0x332d, 0x4298, 0x80, 0x01, 0x07, 0xaa, 0x93, 0x50, 0xdc, 0xa4);
typedef void *CRITSEC_COOKIE;

typedef /* [public] */ 
enum __MIDL___MIDL_itf_clrinternal_0000_0000_0001
    {
        CRST_DEFAULT	= 0,
        CRST_REENTRANCY	= 0x1,
        CRST_UNSAFE_SAMELEVEL	= 0x2,
        CRST_UNSAFE_COOPGC	= 0x4,
        CRST_UNSAFE_ANYMODE	= 0x8,
        CRST_DEBUGGER_THREAD	= 0x10,
        CRST_HOST_BREAKABLE	= 0x20,
        CRST_TAKEN_DURING_SHUTDOWN	= 0x80,
        CRST_GC_NOTRIGGER_WHEN_TAKEN	= 0x100,
        CRST_DEBUG_ONLY_CHECK_FORBID_SUSPEND_THREAD	= 0x200
    } 	CrstFlags;



extern RPC_IF_HANDLE __MIDL_itf_clrinternal_0000_0000_v0_0_c_ifspec;
extern RPC_IF_HANDLE __MIDL_itf_clrinternal_0000_0000_v0_0_s_ifspec;

#ifndef __IPrivateManagedExceptionReporting_INTERFACE_DEFINED__
#define __IPrivateManagedExceptionReporting_INTERFACE_DEFINED__

/* interface IPrivateManagedExceptionReporting */
/* [object][local][unique][helpstring][uuid] */ 


EXTERN_C const IID IID_IPrivateManagedExceptionReporting;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("AD76A023-332D-4298-8001-07AA9350DCA4")
    IPrivateManagedExceptionReporting : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE GetBucketParametersForCurrentException( 
            /* [out] */ BucketParameters *pParams) = 0;
        
    };
    
    
#else 	/* C style interface */

    typedef struct IPrivateManagedExceptionReportingVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            IPrivateManagedExceptionReporting * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            IPrivateManagedExceptionReporting * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            IPrivateManagedExceptionReporting * This);
        
        HRESULT ( STDMETHODCALLTYPE *GetBucketParametersForCurrentException )( 
            IPrivateManagedExceptionReporting * This,
            /* [out] */ BucketParameters *pParams);
        
        END_INTERFACE
    } IPrivateManagedExceptionReportingVtbl;

    interface IPrivateManagedExceptionReporting
    {
        CONST_VTBL struct IPrivateManagedExceptionReportingVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define IPrivateManagedExceptionReporting_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define IPrivateManagedExceptionReporting_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define IPrivateManagedExceptionReporting_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define IPrivateManagedExceptionReporting_GetBucketParametersForCurrentException(This,pParams)	\
    ( (This)->lpVtbl -> GetBucketParametersForCurrentException(This,pParams) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __IPrivateManagedExceptionReporting_INTERFACE_DEFINED__ */


/* Additional Prototypes for ALL interfaces */

/* end of Additional Prototypes */

#ifdef __cplusplus
}
#endif

#endif


