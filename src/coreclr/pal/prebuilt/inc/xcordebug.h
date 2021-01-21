

/* this ALWAYS GENERATED file contains the definitions for the interfaces */


 /* File created by MIDL compiler version 8.01.0622 */
/* at Mon Jan 18 19:14:07 2038
 */
/* Compiler settings for F:/Dev/coreclr/inc/xcordebug.idl:
    Oicf, W1, Zp8, env=Win32 (32b run), target_arch=X86 8.01.0622 
    protocol : dce , ms_ext, c_ext, robust
    error checks: allocation ref bounds_check enum stub_data 
    VC __declspec() decoration level: 
         __declspec(uuid()), __declspec(selectany), __declspec(novtable)
         DECLSPEC_UUID(), MIDL_INTERFACE()
*/
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

#ifndef __xcordebug_h__
#define __xcordebug_h__

#if defined(_MSC_VER) && (_MSC_VER >= 1020)
#pragma once
#endif

/* Forward Declarations */ 

#ifndef __ICorDebugProcess4_FWD_DEFINED__
#define __ICorDebugProcess4_FWD_DEFINED__
typedef interface ICorDebugProcess4 ICorDebugProcess4;

#endif 	/* __ICorDebugProcess4_FWD_DEFINED__ */


/* header files for imported files */
#include "cordebug.h"

#ifdef __cplusplus
extern "C"{
#endif 


#ifndef __ICorDebugProcess4_INTERFACE_DEFINED__
#define __ICorDebugProcess4_INTERFACE_DEFINED__

/* interface ICorDebugProcess4 */
/* [unique][uuid][local][object] */ 


EXTERN_C const IID IID_ICorDebugProcess4;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("E930C679-78AF-4953-8AB7-B0AABF0F9F80")
    ICorDebugProcess4 : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE Filter( 
            /* [size_is][length_is][in] */ const BYTE pRecord[  ],
            /* [in] */ DWORD countBytes,
            /* [in] */ CorDebugRecordFormat format,
            /* [in] */ DWORD dwFlags,
            /* [in] */ DWORD dwThreadId,
            /* [in] */ ICorDebugManagedCallback *pCallback,
            /* [out][in] */ CORDB_CONTINUE_STATUS *pContinueStatus) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE ProcessStateChanged( 
            /* [in] */ CorDebugStateChange eChange) = 0;
        
    };
    
    
#else 	/* C style interface */

    typedef struct ICorDebugProcess4Vtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            ICorDebugProcess4 * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            ICorDebugProcess4 * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            ICorDebugProcess4 * This);
        
        HRESULT ( STDMETHODCALLTYPE *Filter )( 
            ICorDebugProcess4 * This,
            /* [size_is][length_is][in] */ const BYTE pRecord[  ],
            /* [in] */ DWORD countBytes,
            /* [in] */ CorDebugRecordFormat format,
            /* [in] */ DWORD dwFlags,
            /* [in] */ DWORD dwThreadId,
            /* [in] */ ICorDebugManagedCallback *pCallback,
            /* [out][in] */ CORDB_CONTINUE_STATUS *pContinueStatus);
        
        HRESULT ( STDMETHODCALLTYPE *ProcessStateChanged )( 
            ICorDebugProcess4 * This,
            /* [in] */ CorDebugStateChange eChange);
        
        END_INTERFACE
    } ICorDebugProcess4Vtbl;

    interface ICorDebugProcess4
    {
        CONST_VTBL struct ICorDebugProcess4Vtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define ICorDebugProcess4_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define ICorDebugProcess4_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define ICorDebugProcess4_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define ICorDebugProcess4_Filter(This,pRecord,countBytes,format,dwFlags,dwThreadId,pCallback,pContinueStatus)	\
    ( (This)->lpVtbl -> Filter(This,pRecord,countBytes,format,dwFlags,dwThreadId,pCallback,pContinueStatus) ) 

#define ICorDebugProcess4_ProcessStateChanged(This,eChange)	\
    ( (This)->lpVtbl -> ProcessStateChanged(This,eChange) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ICorDebugProcess4_INTERFACE_DEFINED__ */


/* Additional Prototypes for ALL interfaces */

/* end of Additional Prototypes */

#ifdef __cplusplus
}
#endif

#endif


