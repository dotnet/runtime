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

#ifndef __gchost_h__
#define __gchost_h__

#if defined(_MSC_VER) && (_MSC_VER >= 1020)
#pragma once
#endif

/* Forward Declarations */ 

#ifndef __IDummyDoNotUse_FWD_DEFINED__
#define __IDummyDoNotUse_FWD_DEFINED__
typedef interface IDummyDoNotUse IDummyDoNotUse;

#endif 	/* __IDummyDoNotUse_FWD_DEFINED__ */


/* header files for imported files */
#include "unknwn.h"

#ifdef __cplusplus
extern "C"{
#endif 


/* interface __MIDL_itf_gchost_0000_0000 */
/* [local] */ 

typedef struct _COR_GC_STATS
    {
    ULONG Flags;
    SIZE_T ExplicitGCCount;
    SIZE_T GenCollectionsTaken[ 3 ];
    SIZE_T CommittedKBytes;
    SIZE_T ReservedKBytes;
    SIZE_T Gen0HeapSizeKBytes;
    SIZE_T Gen1HeapSizeKBytes;
    SIZE_T Gen2HeapSizeKBytes;
    SIZE_T LargeObjectHeapSizeKBytes;
    SIZE_T KBytesPromotedFromGen0;
    SIZE_T KBytesPromotedFromGen1;
    } 	COR_GC_STATS;

/*
 * WARNING - This is a dummy interface that should never be used.
 * The code is written this way because Midl requires a CoClass, Interface, etc... that generates
 * a guid.  Removing the IGCHost interface removes the only guid
 * This option was selected because ifdefs are not simple to implement for excluding files in SOURCES
*/


extern RPC_IF_HANDLE __MIDL_itf_gchost_0000_0000_v0_0_c_ifspec;
extern RPC_IF_HANDLE __MIDL_itf_gchost_0000_0000_v0_0_s_ifspec;

#ifndef __IDummyDoNotUse_INTERFACE_DEFINED__
#define __IDummyDoNotUse_INTERFACE_DEFINED__

/* interface IDummyDoNotUse */
/* [local][unique][uuid][object] */ 


EXTERN_C const IID IID_IDummyDoNotUse;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("F9423916-2A35-4f03-9EE9-DDAFA3C8AEE0")
    IDummyDoNotUse : public IUnknown
    {
    public:
    };
    
    
#else 	/* C style interface */

    typedef struct IDummyDoNotUseVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            IDummyDoNotUse * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            IDummyDoNotUse * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            IDummyDoNotUse * This);
        
        END_INTERFACE
    } IDummyDoNotUseVtbl;

    interface IDummyDoNotUse
    {
        CONST_VTBL struct IDummyDoNotUseVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define IDummyDoNotUse_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define IDummyDoNotUse_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define IDummyDoNotUse_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __IDummyDoNotUse_INTERFACE_DEFINED__ */


/* Additional Prototypes for ALL interfaces */

/* end of Additional Prototypes */

#ifdef __cplusplus
}
#endif

#endif


