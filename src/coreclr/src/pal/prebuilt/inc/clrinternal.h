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

#ifndef __clrinternal_h__
#define __clrinternal_h__

#if defined(_MSC_VER) && (_MSC_VER >= 1020)
#pragma once
#endif

/* Forward Declarations */ 

#ifndef __IExecutionEngine_FWD_DEFINED__
#define __IExecutionEngine_FWD_DEFINED__
typedef interface IExecutionEngine IExecutionEngine;

#endif 	/* __IExecutionEngine_FWD_DEFINED__ */


#ifndef __IEEMemoryManager_FWD_DEFINED__
#define __IEEMemoryManager_FWD_DEFINED__
typedef interface IEEMemoryManager IEEMemoryManager;

#endif 	/* __IEEMemoryManager_FWD_DEFINED__ */


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

#if 0
typedef struct _OSVERSIONINFOA
    {
    DWORD dwOSVersionInfoSize;
    DWORD dwMajorVersion;
    DWORD dwMinorVersion;
    DWORD dwBuildNumber;
    DWORD dwPlatformId;
    CHAR szCSDVersion[ 128 ];
    } 	OSVERSIONINFOA;

typedef struct _OSVERSIONINFOA *POSVERSIONINFOA;

typedef struct _OSVERSIONINFOA *LPOSVERSIONINFOA;

typedef struct _OSVERSIONINFOW
    {
    DWORD dwOSVersionInfoSize;
    DWORD dwMajorVersion;
    DWORD dwMinorVersion;
    DWORD dwBuildNumber;
    DWORD dwPlatformId;
    WCHAR szCSDVersion[ 128 ];
    } 	OSVERSIONINFOW;

typedef struct _OSVERSIONINFOW *POSVERSIONINFOW;

typedef struct _OSVERSIONINFOW *LPOSVERSIONINFOW;

typedef struct _OSVERSIONINFOW RTL_OSVERSIONINFOW;

typedef struct _OSVERSIONINFOW *PRTL_OSVERSIONINFOW;

typedef OSVERSIONINFOA OSVERSIONINFO;

typedef POSVERSIONINFOA POSVERSIONINFO;

typedef LPOSVERSIONINFOA LPOSVERSIONINFO;

#endif
EXTERN_GUID(IID_IExecutionEngine, 0x7AF02DAC, 0x2A33, 0x494b, 0xA0, 0x9F, 0x25, 0xE0, 0x0A, 0x93, 0xC6, 0xF8);
EXTERN_GUID(IID_IEEMemoryManager, 0x17713b61, 0xb59f, 0x4e13, 0xba, 0xaf, 0x91, 0x62, 0x3d, 0xc8, 0xad, 0xc0);
EXTERN_GUID(CLR_ID_V4_DESKTOP, 0x267f3989, 0xd786, 0x4b9a, 0x9a, 0xf6, 0xd1, 0x9e, 0x42, 0xd5, 0x57, 0xec);
EXTERN_GUID(CLR_ID_CORECLR, 0x8CB8E075, 0x0A91, 0x408E, 0x92, 0x28, 0xD6, 0x6E, 0x00, 0xA3, 0xBF, 0xF6 );
EXTERN_GUID(CLR_ID_PHONE_CLR, 0xE7237E9C, 0x31C0, 0x488C, 0xAD, 0x48, 0x32, 0x4D, 0x3E, 0x7E, 0xD9, 0x2A);
EXTERN_GUID(CLR_ID_ONECORE_CLR, 0xb1ee760d, 0x6c4a, 0x4533, 0xba, 0x41, 0x6f, 0x4f, 0x66, 0x1f, 0xab, 0xaf);
EXTERN_GUID(IID_IPrivateManagedExceptionReporting, 0xad76a023, 0x332d, 0x4298, 0x80, 0x01, 0x07, 0xaa, 0x93, 0x50, 0xdc, 0xa4);
typedef void *CRITSEC_COOKIE;

typedef void *EVENT_COOKIE;

typedef void *SEMAPHORE_COOKIE;

typedef void *MUTEX_COOKIE;

typedef /* [public][public] */ 
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

typedef VOID ( WINAPI *PTLS_CALLBACK_FUNCTION )(
    PVOID __MIDL____MIDL_itf_clrinternal_0000_00000000);



extern RPC_IF_HANDLE __MIDL_itf_clrinternal_0000_0000_v0_0_c_ifspec;
extern RPC_IF_HANDLE __MIDL_itf_clrinternal_0000_0000_v0_0_s_ifspec;

#ifndef __IExecutionEngine_INTERFACE_DEFINED__
#define __IExecutionEngine_INTERFACE_DEFINED__

/* interface IExecutionEngine */
/* [object][local][unique][helpstring][uuid] */ 


EXTERN_C const IID IID_IExecutionEngine;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("7AF02DAC-2A33-494b-A09F-25E00A93C6F8")
    IExecutionEngine : public IUnknown
    {
    public:
        virtual void STDMETHODCALLTYPE TLS_AssociateCallback( 
            /* [in] */ DWORD slot,
            /* [in] */ PTLS_CALLBACK_FUNCTION callback) = 0;
        
        virtual PVOID *STDMETHODCALLTYPE TLS_GetDataBlock( void) = 0;
        
        virtual PVOID STDMETHODCALLTYPE TLS_GetValue( 
            /* [in] */ DWORD slot) = 0;
        
        virtual BOOL STDMETHODCALLTYPE TLS_CheckValue( 
            /* [in] */ DWORD slot,
            /* [out] */ PVOID *pValue) = 0;
        
        virtual void STDMETHODCALLTYPE TLS_SetValue( 
            /* [in] */ DWORD slot,
            /* [in] */ PVOID pData) = 0;
        
        virtual void STDMETHODCALLTYPE TLS_ThreadDetaching( void) = 0;
        
        virtual CRITSEC_COOKIE STDMETHODCALLTYPE CreateLock( 
            /* [in] */ LPCSTR szTag,
            /* [in] */ LPCSTR level,
            /* [in] */ CrstFlags flags) = 0;
        
        virtual void STDMETHODCALLTYPE DestroyLock( 
            /* [in] */ CRITSEC_COOKIE lock) = 0;
        
        virtual void STDMETHODCALLTYPE AcquireLock( 
            /* [in] */ CRITSEC_COOKIE lock) = 0;
        
        virtual void STDMETHODCALLTYPE ReleaseLock( 
            /* [in] */ CRITSEC_COOKIE lock) = 0;
        
        virtual EVENT_COOKIE STDMETHODCALLTYPE CreateAutoEvent( 
            /* [in] */ BOOL bInitialState) = 0;
        
        virtual EVENT_COOKIE STDMETHODCALLTYPE CreateManualEvent( 
            /* [in] */ BOOL bInitialState) = 0;
        
        virtual void STDMETHODCALLTYPE CloseEvent( 
            /* [in] */ EVENT_COOKIE event) = 0;
        
        virtual BOOL STDMETHODCALLTYPE ClrSetEvent( 
            /* [in] */ EVENT_COOKIE event) = 0;
        
        virtual BOOL STDMETHODCALLTYPE ClrResetEvent( 
            /* [in] */ EVENT_COOKIE event) = 0;
        
        virtual DWORD STDMETHODCALLTYPE WaitForEvent( 
            /* [in] */ EVENT_COOKIE event,
            /* [in] */ DWORD dwMilliseconds,
            /* [in] */ BOOL bAlertable) = 0;
        
        virtual DWORD STDMETHODCALLTYPE WaitForSingleObject( 
            /* [in] */ HANDLE handle,
            /* [in] */ DWORD dwMilliseconds) = 0;
        
        virtual SEMAPHORE_COOKIE STDMETHODCALLTYPE ClrCreateSemaphore( 
            /* [in] */ DWORD dwInitial,
            /* [in] */ DWORD dwMax) = 0;
        
        virtual void STDMETHODCALLTYPE ClrCloseSemaphore( 
            /* [in] */ SEMAPHORE_COOKIE semaphore) = 0;
        
        virtual DWORD STDMETHODCALLTYPE ClrWaitForSemaphore( 
            /* [in] */ SEMAPHORE_COOKIE semaphore,
            /* [in] */ DWORD dwMilliseconds,
            /* [in] */ BOOL bAlertable) = 0;
        
        virtual BOOL STDMETHODCALLTYPE ClrReleaseSemaphore( 
            /* [in] */ SEMAPHORE_COOKIE semaphore,
            /* [in] */ LONG lReleaseCount,
            /* [in] */ LONG *lpPreviousCount) = 0;
        
        virtual MUTEX_COOKIE STDMETHODCALLTYPE ClrCreateMutex( 
            /* [in] */ LPSECURITY_ATTRIBUTES lpMutexAttributes,
            /* [in] */ BOOL bInitialOwner,
            /* [in] */ LPCTSTR lpName) = 0;
        
        virtual DWORD STDMETHODCALLTYPE ClrWaitForMutex( 
            /* [in] */ MUTEX_COOKIE mutex,
            /* [in] */ DWORD dwMilliseconds,
            /* [in] */ BOOL bAlertable) = 0;
        
        virtual BOOL STDMETHODCALLTYPE ClrReleaseMutex( 
            /* [in] */ MUTEX_COOKIE mutex) = 0;
        
        virtual void STDMETHODCALLTYPE ClrCloseMutex( 
            /* [in] */ MUTEX_COOKIE mutex) = 0;
        
        virtual DWORD STDMETHODCALLTYPE ClrSleepEx( 
            /* [in] */ DWORD dwMilliseconds,
            /* [in] */ BOOL bAlertable) = 0;
        
        virtual BOOL STDMETHODCALLTYPE ClrAllocationDisallowed( void) = 0;
        
        virtual void STDMETHODCALLTYPE GetLastThrownObjectExceptionFromThread( 
            /* [out] */ void **ppvException) = 0;
        
    };
    
    
#else 	/* C style interface */

    typedef struct IExecutionEngineVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            IExecutionEngine * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            IExecutionEngine * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            IExecutionEngine * This);
        
        void ( STDMETHODCALLTYPE *TLS_AssociateCallback )( 
            IExecutionEngine * This,
            /* [in] */ DWORD slot,
            /* [in] */ PTLS_CALLBACK_FUNCTION callback);
        
        PVOID *( STDMETHODCALLTYPE *TLS_GetDataBlock )( 
            IExecutionEngine * This);
        
        PVOID ( STDMETHODCALLTYPE *TLS_GetValue )( 
            IExecutionEngine * This,
            /* [in] */ DWORD slot);
        
        BOOL ( STDMETHODCALLTYPE *TLS_CheckValue )( 
            IExecutionEngine * This,
            /* [in] */ DWORD slot,
            /* [out] */ PVOID *pValue);
        
        void ( STDMETHODCALLTYPE *TLS_SetValue )( 
            IExecutionEngine * This,
            /* [in] */ DWORD slot,
            /* [in] */ PVOID pData);
        
        void ( STDMETHODCALLTYPE *TLS_ThreadDetaching )( 
            IExecutionEngine * This);
        
        CRITSEC_COOKIE ( STDMETHODCALLTYPE *CreateLock )( 
            IExecutionEngine * This,
            /* [in] */ LPCSTR szTag,
            /* [in] */ LPCSTR level,
            /* [in] */ CrstFlags flags);
        
        void ( STDMETHODCALLTYPE *DestroyLock )( 
            IExecutionEngine * This,
            /* [in] */ CRITSEC_COOKIE lock);
        
        void ( STDMETHODCALLTYPE *AcquireLock )( 
            IExecutionEngine * This,
            /* [in] */ CRITSEC_COOKIE lock);
        
        void ( STDMETHODCALLTYPE *ReleaseLock )( 
            IExecutionEngine * This,
            /* [in] */ CRITSEC_COOKIE lock);
        
        EVENT_COOKIE ( STDMETHODCALLTYPE *CreateAutoEvent )( 
            IExecutionEngine * This,
            /* [in] */ BOOL bInitialState);
        
        EVENT_COOKIE ( STDMETHODCALLTYPE *CreateManualEvent )( 
            IExecutionEngine * This,
            /* [in] */ BOOL bInitialState);
        
        void ( STDMETHODCALLTYPE *CloseEvent )( 
            IExecutionEngine * This,
            /* [in] */ EVENT_COOKIE event);
        
        BOOL ( STDMETHODCALLTYPE *ClrSetEvent )( 
            IExecutionEngine * This,
            /* [in] */ EVENT_COOKIE event);
        
        BOOL ( STDMETHODCALLTYPE *ClrResetEvent )( 
            IExecutionEngine * This,
            /* [in] */ EVENT_COOKIE event);
        
        DWORD ( STDMETHODCALLTYPE *WaitForEvent )( 
            IExecutionEngine * This,
            /* [in] */ EVENT_COOKIE event,
            /* [in] */ DWORD dwMilliseconds,
            /* [in] */ BOOL bAlertable);
        
        DWORD ( STDMETHODCALLTYPE *WaitForSingleObject )( 
            IExecutionEngine * This,
            /* [in] */ HANDLE handle,
            /* [in] */ DWORD dwMilliseconds);
        
        SEMAPHORE_COOKIE ( STDMETHODCALLTYPE *ClrCreateSemaphore )( 
            IExecutionEngine * This,
            /* [in] */ DWORD dwInitial,
            /* [in] */ DWORD dwMax);
        
        void ( STDMETHODCALLTYPE *ClrCloseSemaphore )( 
            IExecutionEngine * This,
            /* [in] */ SEMAPHORE_COOKIE semaphore);
        
        DWORD ( STDMETHODCALLTYPE *ClrWaitForSemaphore )( 
            IExecutionEngine * This,
            /* [in] */ SEMAPHORE_COOKIE semaphore,
            /* [in] */ DWORD dwMilliseconds,
            /* [in] */ BOOL bAlertable);
        
        BOOL ( STDMETHODCALLTYPE *ClrReleaseSemaphore )( 
            IExecutionEngine * This,
            /* [in] */ SEMAPHORE_COOKIE semaphore,
            /* [in] */ LONG lReleaseCount,
            /* [in] */ LONG *lpPreviousCount);
        
        MUTEX_COOKIE ( STDMETHODCALLTYPE *ClrCreateMutex )( 
            IExecutionEngine * This,
            /* [in] */ LPSECURITY_ATTRIBUTES lpMutexAttributes,
            /* [in] */ BOOL bInitialOwner,
            /* [in] */ LPCTSTR lpName);
        
        DWORD ( STDMETHODCALLTYPE *ClrWaitForMutex )( 
            IExecutionEngine * This,
            /* [in] */ MUTEX_COOKIE mutex,
            /* [in] */ DWORD dwMilliseconds,
            /* [in] */ BOOL bAlertable);
        
        BOOL ( STDMETHODCALLTYPE *ClrReleaseMutex )( 
            IExecutionEngine * This,
            /* [in] */ MUTEX_COOKIE mutex);
        
        void ( STDMETHODCALLTYPE *ClrCloseMutex )( 
            IExecutionEngine * This,
            /* [in] */ MUTEX_COOKIE mutex);
        
        DWORD ( STDMETHODCALLTYPE *ClrSleepEx )( 
            IExecutionEngine * This,
            /* [in] */ DWORD dwMilliseconds,
            /* [in] */ BOOL bAlertable);
        
        BOOL ( STDMETHODCALLTYPE *ClrAllocationDisallowed )( 
            IExecutionEngine * This);
        
        void ( STDMETHODCALLTYPE *GetLastThrownObjectExceptionFromThread )( 
            IExecutionEngine * This,
            /* [out] */ void **ppvException);
        
        END_INTERFACE
    } IExecutionEngineVtbl;

    interface IExecutionEngine
    {
        CONST_VTBL struct IExecutionEngineVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define IExecutionEngine_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define IExecutionEngine_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define IExecutionEngine_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define IExecutionEngine_TLS_AssociateCallback(This,slot,callback)	\
    ( (This)->lpVtbl -> TLS_AssociateCallback(This,slot,callback) ) 

#define IExecutionEngine_TLS_GetDataBlock(This)	\
    ( (This)->lpVtbl -> TLS_GetDataBlock(This) ) 

#define IExecutionEngine_TLS_GetValue(This,slot)	\
    ( (This)->lpVtbl -> TLS_GetValue(This,slot) ) 

#define IExecutionEngine_TLS_CheckValue(This,slot,pValue)	\
    ( (This)->lpVtbl -> TLS_CheckValue(This,slot,pValue) ) 

#define IExecutionEngine_TLS_SetValue(This,slot,pData)	\
    ( (This)->lpVtbl -> TLS_SetValue(This,slot,pData) ) 

#define IExecutionEngine_TLS_ThreadDetaching(This)	\
    ( (This)->lpVtbl -> TLS_ThreadDetaching(This) ) 

#define IExecutionEngine_CreateLock(This,szTag,level,flags)	\
    ( (This)->lpVtbl -> CreateLock(This,szTag,level,flags) ) 

#define IExecutionEngine_DestroyLock(This,lock)	\
    ( (This)->lpVtbl -> DestroyLock(This,lock) ) 

#define IExecutionEngine_AcquireLock(This,lock)	\
    ( (This)->lpVtbl -> AcquireLock(This,lock) ) 

#define IExecutionEngine_ReleaseLock(This,lock)	\
    ( (This)->lpVtbl -> ReleaseLock(This,lock) ) 

#define IExecutionEngine_CreateAutoEvent(This,bInitialState)	\
    ( (This)->lpVtbl -> CreateAutoEvent(This,bInitialState) ) 

#define IExecutionEngine_CreateManualEvent(This,bInitialState)	\
    ( (This)->lpVtbl -> CreateManualEvent(This,bInitialState) ) 

#define IExecutionEngine_CloseEvent(This,event)	\
    ( (This)->lpVtbl -> CloseEvent(This,event) ) 

#define IExecutionEngine_ClrSetEvent(This,event)	\
    ( (This)->lpVtbl -> ClrSetEvent(This,event) ) 

#define IExecutionEngine_ClrResetEvent(This,event)	\
    ( (This)->lpVtbl -> ClrResetEvent(This,event) ) 

#define IExecutionEngine_WaitForEvent(This,event,dwMilliseconds,bAlertable)	\
    ( (This)->lpVtbl -> WaitForEvent(This,event,dwMilliseconds,bAlertable) ) 

#define IExecutionEngine_WaitForSingleObject(This,handle,dwMilliseconds)	\
    ( (This)->lpVtbl -> WaitForSingleObject(This,handle,dwMilliseconds) ) 

#define IExecutionEngine_ClrCreateSemaphore(This,dwInitial,dwMax)	\
    ( (This)->lpVtbl -> ClrCreateSemaphore(This,dwInitial,dwMax) ) 

#define IExecutionEngine_ClrCloseSemaphore(This,semaphore)	\
    ( (This)->lpVtbl -> ClrCloseSemaphore(This,semaphore) ) 

#define IExecutionEngine_ClrWaitForSemaphore(This,semaphore,dwMilliseconds,bAlertable)	\
    ( (This)->lpVtbl -> ClrWaitForSemaphore(This,semaphore,dwMilliseconds,bAlertable) ) 

#define IExecutionEngine_ClrReleaseSemaphore(This,semaphore,lReleaseCount,lpPreviousCount)	\
    ( (This)->lpVtbl -> ClrReleaseSemaphore(This,semaphore,lReleaseCount,lpPreviousCount) ) 

#define IExecutionEngine_ClrCreateMutex(This,lpMutexAttributes,bInitialOwner,lpName)	\
    ( (This)->lpVtbl -> ClrCreateMutex(This,lpMutexAttributes,bInitialOwner,lpName) ) 

#define IExecutionEngine_ClrWaitForMutex(This,mutex,dwMilliseconds,bAlertable)	\
    ( (This)->lpVtbl -> ClrWaitForMutex(This,mutex,dwMilliseconds,bAlertable) ) 

#define IExecutionEngine_ClrReleaseMutex(This,mutex)	\
    ( (This)->lpVtbl -> ClrReleaseMutex(This,mutex) ) 

#define IExecutionEngine_ClrCloseMutex(This,mutex)	\
    ( (This)->lpVtbl -> ClrCloseMutex(This,mutex) ) 

#define IExecutionEngine_ClrSleepEx(This,dwMilliseconds,bAlertable)	\
    ( (This)->lpVtbl -> ClrSleepEx(This,dwMilliseconds,bAlertable) ) 

#define IExecutionEngine_ClrAllocationDisallowed(This)	\
    ( (This)->lpVtbl -> ClrAllocationDisallowed(This) ) 

#define IExecutionEngine_GetLastThrownObjectExceptionFromThread(This,ppvException)	\
    ( (This)->lpVtbl -> GetLastThrownObjectExceptionFromThread(This,ppvException) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __IExecutionEngine_INTERFACE_DEFINED__ */


/* interface __MIDL_itf_clrinternal_0000_0001 */
/* [local] */ 

#if !defined(_WINNT_) && !defined(_NTMMAPI_)
typedef void *PMEMORY_BASIC_INFORMATION;

#endif


extern RPC_IF_HANDLE __MIDL_itf_clrinternal_0000_0001_v0_0_c_ifspec;
extern RPC_IF_HANDLE __MIDL_itf_clrinternal_0000_0001_v0_0_s_ifspec;

#ifndef __IEEMemoryManager_INTERFACE_DEFINED__
#define __IEEMemoryManager_INTERFACE_DEFINED__

/* interface IEEMemoryManager */
/* [object][local][unique][helpstring][uuid] */ 


EXTERN_C const IID IID_IEEMemoryManager;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("17713B61-B59F-4e13-BAAF-91623DC8ADC0")
    IEEMemoryManager : public IUnknown
    {
    public:
        virtual LPVOID STDMETHODCALLTYPE ClrVirtualAlloc( 
            /* [in] */ LPVOID lpAddress,
            /* [in] */ SIZE_T dwSize,
            /* [in] */ DWORD flAllocationType,
            /* [in] */ DWORD flProtect) = 0;
        
        virtual BOOL STDMETHODCALLTYPE ClrVirtualFree( 
            /* [in] */ LPVOID lpAddress,
            /* [in] */ SIZE_T dwSize,
            /* [in] */ DWORD dwFreeType) = 0;
        
        virtual SIZE_T STDMETHODCALLTYPE ClrVirtualQuery( 
            /* [in] */ const void *lpAddress,
            /* [in] */ PMEMORY_BASIC_INFORMATION lpBuffer,
            /* [in] */ SIZE_T dwLength) = 0;
        
        virtual BOOL STDMETHODCALLTYPE ClrVirtualProtect( 
            /* [in] */ LPVOID lpAddress,
            /* [in] */ SIZE_T dwSize,
            /* [in] */ DWORD flNewProtect,
            /* [in] */ DWORD *lpflOldProtect) = 0;
        
        virtual HANDLE STDMETHODCALLTYPE ClrGetProcessHeap( void) = 0;
        
        virtual HANDLE STDMETHODCALLTYPE ClrHeapCreate( 
            /* [in] */ DWORD flOptions,
            /* [in] */ SIZE_T dwInitialSize,
            /* [in] */ SIZE_T dwMaximumSize) = 0;
        
        virtual BOOL STDMETHODCALLTYPE ClrHeapDestroy( 
            /* [in] */ HANDLE hHeap) = 0;
        
        virtual LPVOID STDMETHODCALLTYPE ClrHeapAlloc( 
            /* [in] */ HANDLE hHeap,
            /* [in] */ DWORD dwFlags,
            /* [in] */ SIZE_T dwBytes) = 0;
        
        virtual BOOL STDMETHODCALLTYPE ClrHeapFree( 
            /* [in] */ HANDLE hHeap,
            /* [in] */ DWORD dwFlags,
            /* [in] */ LPVOID lpMem) = 0;
        
        virtual BOOL STDMETHODCALLTYPE ClrHeapValidate( 
            /* [in] */ HANDLE hHeap,
            /* [in] */ DWORD dwFlags,
            /* [in] */ const void *lpMem) = 0;
        
        virtual HANDLE STDMETHODCALLTYPE ClrGetProcessExecutableHeap( void) = 0;
        
    };
    
    
#else 	/* C style interface */

    typedef struct IEEMemoryManagerVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            IEEMemoryManager * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            IEEMemoryManager * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            IEEMemoryManager * This);
        
        LPVOID ( STDMETHODCALLTYPE *ClrVirtualAlloc )( 
            IEEMemoryManager * This,
            /* [in] */ LPVOID lpAddress,
            /* [in] */ SIZE_T dwSize,
            /* [in] */ DWORD flAllocationType,
            /* [in] */ DWORD flProtect);
        
        BOOL ( STDMETHODCALLTYPE *ClrVirtualFree )( 
            IEEMemoryManager * This,
            /* [in] */ LPVOID lpAddress,
            /* [in] */ SIZE_T dwSize,
            /* [in] */ DWORD dwFreeType);
        
        SIZE_T ( STDMETHODCALLTYPE *ClrVirtualQuery )( 
            IEEMemoryManager * This,
            /* [in] */ const void *lpAddress,
            /* [in] */ PMEMORY_BASIC_INFORMATION lpBuffer,
            /* [in] */ SIZE_T dwLength);
        
        BOOL ( STDMETHODCALLTYPE *ClrVirtualProtect )( 
            IEEMemoryManager * This,
            /* [in] */ LPVOID lpAddress,
            /* [in] */ SIZE_T dwSize,
            /* [in] */ DWORD flNewProtect,
            /* [in] */ DWORD *lpflOldProtect);
        
        HANDLE ( STDMETHODCALLTYPE *ClrGetProcessHeap )( 
            IEEMemoryManager * This);
        
        HANDLE ( STDMETHODCALLTYPE *ClrHeapCreate )( 
            IEEMemoryManager * This,
            /* [in] */ DWORD flOptions,
            /* [in] */ SIZE_T dwInitialSize,
            /* [in] */ SIZE_T dwMaximumSize);
        
        BOOL ( STDMETHODCALLTYPE *ClrHeapDestroy )( 
            IEEMemoryManager * This,
            /* [in] */ HANDLE hHeap);
        
        LPVOID ( STDMETHODCALLTYPE *ClrHeapAlloc )( 
            IEEMemoryManager * This,
            /* [in] */ HANDLE hHeap,
            /* [in] */ DWORD dwFlags,
            /* [in] */ SIZE_T dwBytes);
        
        BOOL ( STDMETHODCALLTYPE *ClrHeapFree )( 
            IEEMemoryManager * This,
            /* [in] */ HANDLE hHeap,
            /* [in] */ DWORD dwFlags,
            /* [in] */ LPVOID lpMem);
        
        BOOL ( STDMETHODCALLTYPE *ClrHeapValidate )( 
            IEEMemoryManager * This,
            /* [in] */ HANDLE hHeap,
            /* [in] */ DWORD dwFlags,
            /* [in] */ const void *lpMem);
        
        HANDLE ( STDMETHODCALLTYPE *ClrGetProcessExecutableHeap )( 
            IEEMemoryManager * This);
        
        END_INTERFACE
    } IEEMemoryManagerVtbl;

    interface IEEMemoryManager
    {
        CONST_VTBL struct IEEMemoryManagerVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define IEEMemoryManager_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define IEEMemoryManager_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define IEEMemoryManager_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define IEEMemoryManager_ClrVirtualAlloc(This,lpAddress,dwSize,flAllocationType,flProtect)	\
    ( (This)->lpVtbl -> ClrVirtualAlloc(This,lpAddress,dwSize,flAllocationType,flProtect) ) 

#define IEEMemoryManager_ClrVirtualFree(This,lpAddress,dwSize,dwFreeType)	\
    ( (This)->lpVtbl -> ClrVirtualFree(This,lpAddress,dwSize,dwFreeType) ) 

#define IEEMemoryManager_ClrVirtualQuery(This,lpAddress,lpBuffer,dwLength)	\
    ( (This)->lpVtbl -> ClrVirtualQuery(This,lpAddress,lpBuffer,dwLength) ) 

#define IEEMemoryManager_ClrVirtualProtect(This,lpAddress,dwSize,flNewProtect,lpflOldProtect)	\
    ( (This)->lpVtbl -> ClrVirtualProtect(This,lpAddress,dwSize,flNewProtect,lpflOldProtect) ) 

#define IEEMemoryManager_ClrGetProcessHeap(This)	\
    ( (This)->lpVtbl -> ClrGetProcessHeap(This) ) 

#define IEEMemoryManager_ClrHeapCreate(This,flOptions,dwInitialSize,dwMaximumSize)	\
    ( (This)->lpVtbl -> ClrHeapCreate(This,flOptions,dwInitialSize,dwMaximumSize) ) 

#define IEEMemoryManager_ClrHeapDestroy(This,hHeap)	\
    ( (This)->lpVtbl -> ClrHeapDestroy(This,hHeap) ) 

#define IEEMemoryManager_ClrHeapAlloc(This,hHeap,dwFlags,dwBytes)	\
    ( (This)->lpVtbl -> ClrHeapAlloc(This,hHeap,dwFlags,dwBytes) ) 

#define IEEMemoryManager_ClrHeapFree(This,hHeap,dwFlags,lpMem)	\
    ( (This)->lpVtbl -> ClrHeapFree(This,hHeap,dwFlags,lpMem) ) 

#define IEEMemoryManager_ClrHeapValidate(This,hHeap,dwFlags,lpMem)	\
    ( (This)->lpVtbl -> ClrHeapValidate(This,hHeap,dwFlags,lpMem) ) 

#define IEEMemoryManager_ClrGetProcessExecutableHeap(This)	\
    ( (This)->lpVtbl -> ClrGetProcessExecutableHeap(This) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __IEEMemoryManager_INTERFACE_DEFINED__ */


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


