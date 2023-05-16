

/* this ALWAYS GENERATED file contains the definitions for the interfaces */


 /* File created by MIDL compiler version 8.01.0622 */
/* at Mon Jan 18 19:14:07 2038
 */
/* Compiler settings for C:/git/runtime/src/coreclr/inc/sospriv.idl:
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

#ifndef __sospriv_h__
#define __sospriv_h__

#if defined(_MSC_VER) && (_MSC_VER >= 1020)
#pragma once
#endif

/* Forward Declarations */

#ifndef __ISOSEnum_FWD_DEFINED__
#define __ISOSEnum_FWD_DEFINED__
typedef interface ISOSEnum ISOSEnum;

#endif  /* __ISOSEnum_FWD_DEFINED__ */


#ifndef __ISOSHandleEnum_FWD_DEFINED__
#define __ISOSHandleEnum_FWD_DEFINED__
typedef interface ISOSHandleEnum ISOSHandleEnum;

#endif  /* __ISOSHandleEnum_FWD_DEFINED__ */


#ifndef __ISOSStackRefErrorEnum_FWD_DEFINED__
#define __ISOSStackRefErrorEnum_FWD_DEFINED__
typedef interface ISOSStackRefErrorEnum ISOSStackRefErrorEnum;

#endif  /* __ISOSStackRefErrorEnum_FWD_DEFINED__ */


#ifndef __ISOSStackRefEnum_FWD_DEFINED__
#define __ISOSStackRefEnum_FWD_DEFINED__
typedef interface ISOSStackRefEnum ISOSStackRefEnum;

#endif  /* __ISOSStackRefEnum_FWD_DEFINED__ */


#ifndef __ISOSMemoryEnum_FWD_DEFINED__
#define __ISOSMemoryEnum_FWD_DEFINED__
typedef interface ISOSMemoryEnum ISOSMemoryEnum;

#endif 	/* __ISOSMemoryEnum_FWD_DEFINED__ */


#ifndef __ISOSDacInterface_FWD_DEFINED__
#define __ISOSDacInterface_FWD_DEFINED__
typedef interface ISOSDacInterface ISOSDacInterface;

#endif  /* __ISOSDacInterface_FWD_DEFINED__ */


#ifndef __ISOSDacInterface2_FWD_DEFINED__
#define __ISOSDacInterface2_FWD_DEFINED__
typedef interface ISOSDacInterface2 ISOSDacInterface2;

#endif  /* __ISOSDacInterface2_FWD_DEFINED__ */


#ifndef __ISOSDacInterface3_FWD_DEFINED__
#define __ISOSDacInterface3_FWD_DEFINED__
typedef interface ISOSDacInterface3 ISOSDacInterface3;

#endif  /* __ISOSDacInterface3_FWD_DEFINED__ */


#ifndef __ISOSDacInterface4_FWD_DEFINED__
#define __ISOSDacInterface4_FWD_DEFINED__
typedef interface ISOSDacInterface4 ISOSDacInterface4;

#endif  /* __ISOSDacInterface4_FWD_DEFINED__ */


#ifndef __ISOSDacInterface5_FWD_DEFINED__
#define __ISOSDacInterface5_FWD_DEFINED__
typedef interface ISOSDacInterface5 ISOSDacInterface5;

#endif  /* __ISOSDacInterface5_FWD_DEFINED__ */


#ifndef __ISOSDacInterface6_FWD_DEFINED__
#define __ISOSDacInterface6_FWD_DEFINED__
typedef interface ISOSDacInterface6 ISOSDacInterface6;

#endif  /* __ISOSDacInterface6_FWD_DEFINED__ */


#ifndef __ISOSDacInterface7_FWD_DEFINED__
#define __ISOSDacInterface7_FWD_DEFINED__
typedef interface ISOSDacInterface7 ISOSDacInterface7;

#endif  /* __ISOSDacInterface7_FWD_DEFINED__ */


#ifndef __ISOSDacInterface8_FWD_DEFINED__
#define __ISOSDacInterface8_FWD_DEFINED__
typedef interface ISOSDacInterface8 ISOSDacInterface8;

#endif  /* __ISOSDacInterface8_FWD_DEFINED__ */


#ifndef __ISOSDacInterface9_FWD_DEFINED__
#define __ISOSDacInterface9_FWD_DEFINED__
typedef interface ISOSDacInterface9 ISOSDacInterface9;

#endif  /* __ISOSDacInterface9_FWD_DEFINED__ */


#ifndef __ISOSDacInterface10_FWD_DEFINED__
#define __ISOSDacInterface10_FWD_DEFINED__
typedef interface ISOSDacInterface10 ISOSDacInterface10;

#endif  /* __ISOSDacInterface10_FWD_DEFINED__ */


/* header files for imported files */
#include "unknwn.h"
#include "xclrdata.h"

#ifdef __cplusplus
extern "C"{
#endif


/* interface __MIDL_itf_sospriv_0000_0000 */
/* [local] */

























#if 0
typedef ULONG64 CLRDATA_ADDRESS;

typedef int CONTEXT;

typedef int T_CONTEXT;

typedef int mdToken;

typedef unsigned int size_t;

typedef int ModuleMapType;

typedef int VCSHeapType;

#endif
typedef enum { TYPEDEFTOMETHODTABLE, TYPEREFTOMETHODTABLE } ModuleMapType;
typedef enum {IndcellHeap, LookupHeap, ResolveHeap, DispatchHeap, CacheEntryHeap, VtableHeap} VCSHeapType;
typedef enum {LoaderHeapKindNormal = 0, LoaderHeapKindExplicitControl = 1} LoaderHeapKind;
typedef enum {FreeUnknownRegion = 0, FreeGlobalHugeRegion = 1, FreeGlobalRegion = 2, FreeRegion = 3, FreeSohSegment = 4, FreeUohSegment = 5 } FreeRegionKind;
typedef void ( *MODULEMAPTRAVERSE )(
    UINT index,
    CLRDATA_ADDRESS methodTable,
    LPVOID token);

typedef void ( *VISITHEAP )(
    CLRDATA_ADDRESS blockData,
    size_t blockSize,
    BOOL blockIsCurrentBlock);

typedef BOOL ( *VISITRCWFORCLEANUP )(
    CLRDATA_ADDRESS RCW,
    CLRDATA_ADDRESS Context,
    CLRDATA_ADDRESS Thread,
    BOOL bIsFreeThreaded,
    LPVOID token);

typedef BOOL ( *DUMPEHINFO )(
    UINT clauseIndex,
    UINT totalClauses,
    struct DACEHInfo *pEHInfo,
    LPVOID token);

#ifndef _SOS_HandleData
#define _SOS_HandleData
typedef struct _SOSHandleData
    {
    CLRDATA_ADDRESS AppDomain;
    CLRDATA_ADDRESS Handle;
    CLRDATA_ADDRESS Secondary;
    unsigned int Type;
    BOOL StrongReference;
    unsigned int RefCount;
    unsigned int JupiterRefCount;
    BOOL IsPegged;
    }   SOSHandleData;

#endif //HandleData


extern RPC_IF_HANDLE __MIDL_itf_sospriv_0000_0000_v0_0_c_ifspec;
extern RPC_IF_HANDLE __MIDL_itf_sospriv_0000_0000_v0_0_s_ifspec;

#ifndef __ISOSEnum_INTERFACE_DEFINED__
#define __ISOSEnum_INTERFACE_DEFINED__

/* interface ISOSEnum */
/* [uuid][local][object] */


EXTERN_C const IID IID_ISOSEnum;

#if defined(__cplusplus) && !defined(CINTERFACE)

    MIDL_INTERFACE("286CA186-E763-4F61-9760-487D43AE4341")
    ISOSEnum : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE Skip(
            /* [in] */ unsigned int count) = 0;

        virtual HRESULT STDMETHODCALLTYPE Reset( void) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetCount(
            /* [out] */ unsigned int *pCount) = 0;

    };


#else   /* C style interface */

    typedef struct ISOSEnumVtbl
    {
        BEGIN_INTERFACE

        HRESULT ( STDMETHODCALLTYPE *QueryInterface )(
            ISOSEnum * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */
            _COM_Outptr_  void **ppvObject);

        ULONG ( STDMETHODCALLTYPE *AddRef )(
            ISOSEnum * This);

        ULONG ( STDMETHODCALLTYPE *Release )(
            ISOSEnum * This);

        HRESULT ( STDMETHODCALLTYPE *Skip )(
            ISOSEnum * This,
            /* [in] */ unsigned int count);

        HRESULT ( STDMETHODCALLTYPE *Reset )(
            ISOSEnum * This);

        HRESULT ( STDMETHODCALLTYPE *GetCount )(
            ISOSEnum * This,
            /* [out] */ unsigned int *pCount);

        END_INTERFACE
    } ISOSEnumVtbl;

    interface ISOSEnum
    {
        CONST_VTBL struct ISOSEnumVtbl *lpVtbl;
    };



#ifdef COBJMACROS


#define ISOSEnum_QueryInterface(This,riid,ppvObject)    \
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) )

#define ISOSEnum_AddRef(This)   \
    ( (This)->lpVtbl -> AddRef(This) )

#define ISOSEnum_Release(This)  \
    ( (This)->lpVtbl -> Release(This) )


#define ISOSEnum_Skip(This,count)   \
    ( (This)->lpVtbl -> Skip(This,count) )

#define ISOSEnum_Reset(This)    \
    ( (This)->lpVtbl -> Reset(This) )

#define ISOSEnum_GetCount(This,pCount)  \
    ( (This)->lpVtbl -> GetCount(This,pCount) )

#endif /* COBJMACROS */


#endif  /* C style interface */




#endif  /* __ISOSEnum_INTERFACE_DEFINED__ */


#ifndef __ISOSHandleEnum_INTERFACE_DEFINED__
#define __ISOSHandleEnum_INTERFACE_DEFINED__

/* interface ISOSHandleEnum */
/* [uuid][local][object] */


EXTERN_C const IID IID_ISOSHandleEnum;

#if defined(__cplusplus) && !defined(CINTERFACE)

    MIDL_INTERFACE("3E269830-4A2B-4301-8EE2-D6805B29B2FA")
    ISOSHandleEnum : public ISOSEnum
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE Next(
            /* [in] */ unsigned int count,
            /* [length_is][size_is][out] */ SOSHandleData handles[  ],
            /* [out] */ unsigned int *pNeeded) = 0;

    };


#else   /* C style interface */

    typedef struct ISOSHandleEnumVtbl
    {
        BEGIN_INTERFACE

        HRESULT ( STDMETHODCALLTYPE *QueryInterface )(
            ISOSHandleEnum * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */
            _COM_Outptr_  void **ppvObject);

        ULONG ( STDMETHODCALLTYPE *AddRef )(
            ISOSHandleEnum * This);

        ULONG ( STDMETHODCALLTYPE *Release )(
            ISOSHandleEnum * This);

        HRESULT ( STDMETHODCALLTYPE *Skip )(
            ISOSHandleEnum * This,
            /* [in] */ unsigned int count);

        HRESULT ( STDMETHODCALLTYPE *Reset )(
            ISOSHandleEnum * This);

        HRESULT ( STDMETHODCALLTYPE *GetCount )(
            ISOSHandleEnum * This,
            /* [out] */ unsigned int *pCount);

        HRESULT ( STDMETHODCALLTYPE *Next )(
            ISOSHandleEnum * This,
            /* [in] */ unsigned int count,
            /* [length_is][size_is][out] */ SOSHandleData handles[  ],
            /* [out] */ unsigned int *pNeeded);

        END_INTERFACE
    } ISOSHandleEnumVtbl;

    interface ISOSHandleEnum
    {
        CONST_VTBL struct ISOSHandleEnumVtbl *lpVtbl;
    };



#ifdef COBJMACROS


#define ISOSHandleEnum_QueryInterface(This,riid,ppvObject)  \
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) )

#define ISOSHandleEnum_AddRef(This) \
    ( (This)->lpVtbl -> AddRef(This) )

#define ISOSHandleEnum_Release(This)    \
    ( (This)->lpVtbl -> Release(This) )


#define ISOSHandleEnum_Skip(This,count) \
    ( (This)->lpVtbl -> Skip(This,count) )

#define ISOSHandleEnum_Reset(This)  \
    ( (This)->lpVtbl -> Reset(This) )

#define ISOSHandleEnum_GetCount(This,pCount)    \
    ( (This)->lpVtbl -> GetCount(This,pCount) )


#define ISOSHandleEnum_Next(This,count,handles,pNeeded) \
    ( (This)->lpVtbl -> Next(This,count,handles,pNeeded) )

#endif /* COBJMACROS */


#endif  /* C style interface */




#endif  /* __ISOSHandleEnum_INTERFACE_DEFINED__ */


/* interface __MIDL_itf_sospriv_0000_0002 */
/* [local] */

#ifndef _SOS_StackReference_
#define _SOS_StackReference_
typedef
enum SOSStackSourceType
    {
        SOS_StackSourceIP   = 0,
        SOS_StackSourceFrame    = ( SOS_StackSourceIP + 1 )
    }   SOSStackSourceType;

typedef
enum SOSRefFlags
    {
        SOSRefInterior  = 1,
        SOSRefPinned    = 2
    }   SOSRefFlags;

typedef struct _SOS_StackRefData
    {
    BOOL HasRegisterInformation;
    int Register;
    int Offset;
    CLRDATA_ADDRESS Address;
    CLRDATA_ADDRESS Object;
    unsigned int Flags;
    SOSStackSourceType SourceType;
    CLRDATA_ADDRESS Source;
    CLRDATA_ADDRESS StackPointer;
    }   SOSStackRefData;

typedef struct _SOS_StackRefError
    {
    SOSStackSourceType SourceType;
    CLRDATA_ADDRESS Source;
    CLRDATA_ADDRESS StackPointer;
    }   SOSStackRefError;

#endif // _SOS_StackReference_


extern RPC_IF_HANDLE __MIDL_itf_sospriv_0000_0002_v0_0_c_ifspec;
extern RPC_IF_HANDLE __MIDL_itf_sospriv_0000_0002_v0_0_s_ifspec;

#ifndef _SOS_MemoryRegion_
#define _SOS_MemoryRegion_
typedef struct _SOSMemoryRegion
    {
    CLRDATA_ADDRESS Start;
    CLRDATA_ADDRESS Size;
    CLRDATA_ADDRESS ExtraData;
    int Heap;
    } 	SOSMemoryRegion;
#endif // _SOS_MemoryRegion_

#ifndef __ISOSStackRefErrorEnum_INTERFACE_DEFINED__
#define __ISOSStackRefErrorEnum_INTERFACE_DEFINED__

/* interface ISOSStackRefErrorEnum */
/* [uuid][local][object] */


EXTERN_C const IID IID_ISOSStackRefErrorEnum;

#if defined(__cplusplus) && !defined(CINTERFACE)

    MIDL_INTERFACE("774F4E1B-FB7B-491B-976D-A8130FE355E9")
    ISOSStackRefErrorEnum : public ISOSEnum
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE Next(
            /* [in] */ unsigned int count,
            /* [length_is][size_is][out] */ SOSStackRefError ref[  ],
            /* [out] */ unsigned int *pFetched) = 0;

    };


#else   /* C style interface */

    typedef struct ISOSStackRefErrorEnumVtbl
    {
        BEGIN_INTERFACE

        HRESULT ( STDMETHODCALLTYPE *QueryInterface )(
            ISOSStackRefErrorEnum * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */
            _COM_Outptr_  void **ppvObject);

        ULONG ( STDMETHODCALLTYPE *AddRef )(
            ISOSStackRefErrorEnum * This);

        ULONG ( STDMETHODCALLTYPE *Release )(
            ISOSStackRefErrorEnum * This);

        HRESULT ( STDMETHODCALLTYPE *Skip )(
            ISOSStackRefErrorEnum * This,
            /* [in] */ unsigned int count);

        HRESULT ( STDMETHODCALLTYPE *Reset )(
            ISOSStackRefErrorEnum * This);

        HRESULT ( STDMETHODCALLTYPE *GetCount )(
            ISOSStackRefErrorEnum * This,
            /* [out] */ unsigned int *pCount);

        HRESULT ( STDMETHODCALLTYPE *Next )(
            ISOSStackRefErrorEnum * This,
            /* [in] */ unsigned int count,
            /* [length_is][size_is][out] */ SOSStackRefError ref[  ],
            /* [out] */ unsigned int *pFetched);

        END_INTERFACE
    } ISOSStackRefErrorEnumVtbl;

    interface ISOSStackRefErrorEnum
    {
        CONST_VTBL struct ISOSStackRefErrorEnumVtbl *lpVtbl;
    };



#ifdef COBJMACROS


#define ISOSStackRefErrorEnum_QueryInterface(This,riid,ppvObject)   \
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) )

#define ISOSStackRefErrorEnum_AddRef(This)  \
    ( (This)->lpVtbl -> AddRef(This) )

#define ISOSStackRefErrorEnum_Release(This) \
    ( (This)->lpVtbl -> Release(This) )


#define ISOSStackRefErrorEnum_Skip(This,count)  \
    ( (This)->lpVtbl -> Skip(This,count) )

#define ISOSStackRefErrorEnum_Reset(This)   \
    ( (This)->lpVtbl -> Reset(This) )

#define ISOSStackRefErrorEnum_GetCount(This,pCount) \
    ( (This)->lpVtbl -> GetCount(This,pCount) )


#define ISOSStackRefErrorEnum_Next(This,count,ref,pFetched) \
    ( (This)->lpVtbl -> Next(This,count,ref,pFetched) )

#endif /* COBJMACROS */


#endif  /* C style interface */




#endif  /* __ISOSStackRefErrorEnum_INTERFACE_DEFINED__ */


    
#ifndef __ISOSMemoryEnum_INTERFACE_DEFINED__
#define __ISOSMemoryEnum_INTERFACE_DEFINED__

/* interface ISOSMemoryEnum */
/* [uuid][local][object] */ 


EXTERN_C const IID IID_ISOSMemoryEnum;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("E4B860EC-337A-40C0-A591-F09A9680690F")
    ISOSMemoryEnum : public ISOSEnum
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE Next( 
            /* [in] */ unsigned int count,
            /* [length_is][size_is][out] */ SOSMemoryRegion memRegion[  ],
            /* [out] */ unsigned int *pNeeded) = 0;
        
    };
    
    
#else 	/* C style interface */

    typedef struct ISOSMemoryEnumVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            ISOSMemoryEnum * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            ISOSMemoryEnum * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            ISOSMemoryEnum * This);
        
        HRESULT ( STDMETHODCALLTYPE *Skip )( 
            ISOSMemoryEnum * This,
            /* [in] */ unsigned int count);
        
        HRESULT ( STDMETHODCALLTYPE *Reset )( 
            ISOSMemoryEnum * This);
        
        HRESULT ( STDMETHODCALLTYPE *GetCount )( 
            ISOSMemoryEnum * This,
            /* [out] */ unsigned int *pCount);
        
        HRESULT ( STDMETHODCALLTYPE *Next )( 
            ISOSMemoryEnum * This,
            /* [in] */ unsigned int count,
            /* [length_is][size_is][out] */ SOSMemoryRegion memRegion[  ],
            /* [out] */ unsigned int *pNeeded);
        
        END_INTERFACE
    } ISOSMemoryEnumVtbl;

    interface ISOSMemoryEnum
    {
        CONST_VTBL struct ISOSMemoryEnumVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define ISOSMemoryEnum_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define ISOSMemoryEnum_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define ISOSMemoryEnum_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define ISOSMemoryEnum_Skip(This,count)	\
    ( (This)->lpVtbl -> Skip(This,count) ) 

#define ISOSMemoryEnum_Reset(This)	\
    ( (This)->lpVtbl -> Reset(This) ) 

#define ISOSMemoryEnum_GetCount(This,pCount)	\
    ( (This)->lpVtbl -> GetCount(This,pCount) ) 


#define ISOSMemoryEnum_Next(This,count,memRegion,pNeeded)	\
    ( (This)->lpVtbl -> Next(This,count,memRegion,pNeeded) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ISOSMemoryEnum_INTERFACE_DEFINED__ */


#ifndef __ISOSStackRefEnum_INTERFACE_DEFINED__
#define __ISOSStackRefEnum_INTERFACE_DEFINED__

/* interface ISOSStackRefEnum */
/* [uuid][local][object] */


EXTERN_C const IID IID_ISOSStackRefEnum;

#if defined(__cplusplus) && !defined(CINTERFACE)

    MIDL_INTERFACE("8FA642BD-9F10-4799-9AA3-512AE78C77EE")
    ISOSStackRefEnum : public ISOSEnum
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE Next(
            /* [in] */ unsigned int count,
            /* [length_is][size_is][out] */ SOSStackRefData ref[  ],
            /* [out] */ unsigned int *pFetched) = 0;

        virtual HRESULT STDMETHODCALLTYPE EnumerateErrors(
            /* [out] */ ISOSStackRefErrorEnum **ppEnum) = 0;

    };


#else   /* C style interface */

    typedef struct ISOSStackRefEnumVtbl
    {
        BEGIN_INTERFACE

        HRESULT ( STDMETHODCALLTYPE *QueryInterface )(
            ISOSStackRefEnum * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */
            _COM_Outptr_  void **ppvObject);

        ULONG ( STDMETHODCALLTYPE *AddRef )(
            ISOSStackRefEnum * This);

        ULONG ( STDMETHODCALLTYPE *Release )(
            ISOSStackRefEnum * This);

        HRESULT ( STDMETHODCALLTYPE *Skip )(
            ISOSStackRefEnum * This,
            /* [in] */ unsigned int count);

        HRESULT ( STDMETHODCALLTYPE *Reset )(
            ISOSStackRefEnum * This);

        HRESULT ( STDMETHODCALLTYPE *GetCount )(
            ISOSStackRefEnum * This,
            /* [out] */ unsigned int *pCount);

        HRESULT ( STDMETHODCALLTYPE *Next )(
            ISOSStackRefEnum * This,
            /* [in] */ unsigned int count,
            /* [length_is][size_is][out] */ SOSStackRefData ref[  ],
            /* [out] */ unsigned int *pFetched);

        HRESULT ( STDMETHODCALLTYPE *EnumerateErrors )(
            ISOSStackRefEnum * This,
            /* [out] */ ISOSStackRefErrorEnum **ppEnum);

        END_INTERFACE
    } ISOSStackRefEnumVtbl;

    interface ISOSStackRefEnum
    {
        CONST_VTBL struct ISOSStackRefEnumVtbl *lpVtbl;
    };



#ifdef COBJMACROS


#define ISOSStackRefEnum_QueryInterface(This,riid,ppvObject)    \
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) )

#define ISOSStackRefEnum_AddRef(This)   \
    ( (This)->lpVtbl -> AddRef(This) )

#define ISOSStackRefEnum_Release(This)  \
    ( (This)->lpVtbl -> Release(This) )


#define ISOSStackRefEnum_Skip(This,count)   \
    ( (This)->lpVtbl -> Skip(This,count) )

#define ISOSStackRefEnum_Reset(This)    \
    ( (This)->lpVtbl -> Reset(This) )

#define ISOSStackRefEnum_GetCount(This,pCount)  \
    ( (This)->lpVtbl -> GetCount(This,pCount) )


#define ISOSStackRefEnum_Next(This,count,ref,pFetched)  \
    ( (This)->lpVtbl -> Next(This,count,ref,pFetched) )

#define ISOSStackRefEnum_EnumerateErrors(This,ppEnum)   \
    ( (This)->lpVtbl -> EnumerateErrors(This,ppEnum) )

#endif /* COBJMACROS */


#endif  /* C style interface */




#endif  /* __ISOSStackRefEnum_INTERFACE_DEFINED__ */


#ifndef __ISOSDacInterface_INTERFACE_DEFINED__
#define __ISOSDacInterface_INTERFACE_DEFINED__

/* interface ISOSDacInterface */
/* [uuid][local][object] */


EXTERN_C const IID IID_ISOSDacInterface;

#if defined(__cplusplus) && !defined(CINTERFACE)

    MIDL_INTERFACE("436f00f2-b42a-4b9f-870c-e73db66ae930")
    ISOSDacInterface : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE GetThreadStoreData(
            struct DacpThreadStoreData *data) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetAppDomainStoreData(
            struct DacpAppDomainStoreData *data) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetAppDomainList(
            unsigned int count,
            CLRDATA_ADDRESS values[  ],
            unsigned int *pNeeded) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetAppDomainData(
            CLRDATA_ADDRESS addr,
            struct DacpAppDomainData *data) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetAppDomainName(
            CLRDATA_ADDRESS addr,
            unsigned int count,
            WCHAR *name,
            unsigned int *pNeeded) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetDomainFromContext(
            CLRDATA_ADDRESS context,
            CLRDATA_ADDRESS *domain) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetAssemblyList(
            CLRDATA_ADDRESS appDomain,
            int count,
            CLRDATA_ADDRESS values[  ],
            int *pNeeded) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetAssemblyData(
            CLRDATA_ADDRESS baseDomainPtr,
            CLRDATA_ADDRESS assembly,
            struct DacpAssemblyData *data) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetAssemblyName(
            CLRDATA_ADDRESS assembly,
            unsigned int count,
            WCHAR *name,
            unsigned int *pNeeded) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetModule(
            CLRDATA_ADDRESS addr,
            IXCLRDataModule **mod) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetModuleData(
            CLRDATA_ADDRESS moduleAddr,
            struct DacpModuleData *data) = 0;

        virtual HRESULT STDMETHODCALLTYPE TraverseModuleMap(
            ModuleMapType mmt,
            CLRDATA_ADDRESS moduleAddr,
            MODULEMAPTRAVERSE pCallback,
            LPVOID token) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetAssemblyModuleList(
            CLRDATA_ADDRESS assembly,
            unsigned int count,
            CLRDATA_ADDRESS modules[  ],
            unsigned int *pNeeded) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetILForModule(
            CLRDATA_ADDRESS moduleAddr,
            DWORD rva,
            CLRDATA_ADDRESS *il) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetThreadData(
            CLRDATA_ADDRESS thread,
            struct DacpThreadData *data) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetThreadFromThinlockID(
            UINT thinLockId,
            CLRDATA_ADDRESS *pThread) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetStackLimits(
            CLRDATA_ADDRESS threadPtr,
            CLRDATA_ADDRESS *lower,
            CLRDATA_ADDRESS *upper,
            CLRDATA_ADDRESS *fp) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetMethodDescData(
            CLRDATA_ADDRESS methodDesc,
            CLRDATA_ADDRESS ip,
            struct DacpMethodDescData *data,
            ULONG cRevertedRejitVersions,
            struct DacpReJitData *rgRevertedRejitData,
            ULONG *pcNeededRevertedRejitData) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetMethodDescPtrFromIP(
            CLRDATA_ADDRESS ip,
            CLRDATA_ADDRESS *ppMD) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetMethodDescName(
            CLRDATA_ADDRESS methodDesc,
            unsigned int count,
            WCHAR *name,
            unsigned int *pNeeded) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetMethodDescPtrFromFrame(
            CLRDATA_ADDRESS frameAddr,
            CLRDATA_ADDRESS *ppMD) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetMethodDescFromToken(
            CLRDATA_ADDRESS moduleAddr,
            mdToken token,
            CLRDATA_ADDRESS *methodDesc) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetMethodDescTransparencyData(
            CLRDATA_ADDRESS methodDesc,
            struct DacpMethodDescTransparencyData *data) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetCodeHeaderData(
            CLRDATA_ADDRESS ip,
            struct DacpCodeHeaderData *data) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetJitManagerList(
            unsigned int count,
            struct DacpJitManagerInfo *managers,
            unsigned int *pNeeded) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetJitHelperFunctionName(
            CLRDATA_ADDRESS ip,
            unsigned int count,
            char *name,
            unsigned int *pNeeded) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetJumpThunkTarget(
            T_CONTEXT *ctx,
            CLRDATA_ADDRESS *targetIP,
            CLRDATA_ADDRESS *targetMD) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetThreadpoolData(
            struct DacpThreadpoolData *data) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetWorkRequestData(
            CLRDATA_ADDRESS addrWorkRequest,
            struct DacpWorkRequestData *data) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetHillClimbingLogEntry(
            CLRDATA_ADDRESS addr,
            struct DacpHillClimbingLogEntry *data) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetObjectData(
            CLRDATA_ADDRESS objAddr,
            struct DacpObjectData *data) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetObjectStringData(
            CLRDATA_ADDRESS obj,
            unsigned int count,
            WCHAR *stringData,
            unsigned int *pNeeded) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetObjectClassName(
            CLRDATA_ADDRESS obj,
            unsigned int count,
            WCHAR *className,
            unsigned int *pNeeded) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetMethodTableName(
            CLRDATA_ADDRESS mt,
            unsigned int count,
            WCHAR *mtName,
            unsigned int *pNeeded) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetMethodTableData(
            CLRDATA_ADDRESS mt,
            struct DacpMethodTableData *data) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetMethodTableSlot(
            CLRDATA_ADDRESS mt,
            unsigned int slot,
            CLRDATA_ADDRESS *value) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetMethodTableFieldData(
            CLRDATA_ADDRESS mt,
            struct DacpMethodTableFieldData *data) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetMethodTableTransparencyData(
            CLRDATA_ADDRESS mt,
            struct DacpMethodTableTransparencyData *data) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetMethodTableForEEClass(
            CLRDATA_ADDRESS eeClass,
            CLRDATA_ADDRESS *value) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetFieldDescData(
            CLRDATA_ADDRESS fieldDesc,
            struct DacpFieldDescData *data) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetFrameName(
            CLRDATA_ADDRESS vtable,
            unsigned int count,
            WCHAR *frameName,
            unsigned int *pNeeded) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetPEFileBase(
            CLRDATA_ADDRESS addr,
            CLRDATA_ADDRESS *base) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetPEFileName(
            CLRDATA_ADDRESS addr,
            unsigned int count,
            WCHAR *fileName,
            unsigned int *pNeeded) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetGCHeapData(
            struct DacpGcHeapData *data) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetGCHeapList(
            unsigned int count,
            CLRDATA_ADDRESS heaps[  ],
            unsigned int *pNeeded) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetGCHeapDetails(
            CLRDATA_ADDRESS heap,
            struct DacpGcHeapDetails *details) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetGCHeapStaticData(
            struct DacpGcHeapDetails *data) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetHeapSegmentData(
            CLRDATA_ADDRESS seg,
            struct DacpHeapSegmentData *data) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetOOMData(
            CLRDATA_ADDRESS oomAddr,
            struct DacpOomData *data) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetOOMStaticData(
            struct DacpOomData *data) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetHeapAnalyzeData(
            CLRDATA_ADDRESS addr,
            struct DacpGcHeapAnalyzeData *data) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetHeapAnalyzeStaticData(
            struct DacpGcHeapAnalyzeData *data) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetDomainLocalModuleData(
            CLRDATA_ADDRESS addr,
            struct DacpDomainLocalModuleData *data) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetDomainLocalModuleDataFromAppDomain(
            CLRDATA_ADDRESS appDomainAddr,
            int moduleID,
            struct DacpDomainLocalModuleData *data) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetDomainLocalModuleDataFromModule(
            CLRDATA_ADDRESS moduleAddr,
            struct DacpDomainLocalModuleData *data) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetThreadLocalModuleData(
            CLRDATA_ADDRESS thread,
            unsigned int index,
            struct DacpThreadLocalModuleData *data) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetSyncBlockData(
            unsigned int number,
            struct DacpSyncBlockData *data) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetSyncBlockCleanupData(
            CLRDATA_ADDRESS addr,
            struct DacpSyncBlockCleanupData *data) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetHandleEnum(
            ISOSHandleEnum **ppHandleEnum) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetHandleEnumForTypes(
            unsigned int types[  ],
            unsigned int count,
            ISOSHandleEnum **ppHandleEnum) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetHandleEnumForGC(
            unsigned int gen,
            ISOSHandleEnum **ppHandleEnum) = 0;

        virtual HRESULT STDMETHODCALLTYPE TraverseEHInfo(
            CLRDATA_ADDRESS ip,
            DUMPEHINFO pCallback,
            LPVOID token) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetNestedExceptionData(
            CLRDATA_ADDRESS exception,
            CLRDATA_ADDRESS *exceptionObject,
            CLRDATA_ADDRESS *nextNestedException) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetStressLogAddress(
            CLRDATA_ADDRESS *stressLog) = 0;

        virtual HRESULT STDMETHODCALLTYPE TraverseLoaderHeap(
            CLRDATA_ADDRESS loaderHeapAddr,
            VISITHEAP pCallback) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetCodeHeapList(
            CLRDATA_ADDRESS jitManager,
            unsigned int count,
            struct DacpJitCodeHeapInfo *codeHeaps,
            unsigned int *pNeeded) = 0;

        virtual HRESULT STDMETHODCALLTYPE TraverseVirtCallStubHeap(
            CLRDATA_ADDRESS pAppDomain,
            VCSHeapType heaptype,
            VISITHEAP pCallback) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetUsefulGlobals(
            struct DacpUsefulGlobalsData *data) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetClrWatsonBuckets(
            CLRDATA_ADDRESS thread,
            void *pGenericModeBlock) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetTLSIndex(
            ULONG *pIndex) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetDacModuleHandle(
            HMODULE *phModule) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetRCWData(
            CLRDATA_ADDRESS addr,
            struct DacpRCWData *data) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetRCWInterfaces(
            CLRDATA_ADDRESS rcw,
            unsigned int count,
            struct DacpCOMInterfacePointerData *interfaces,
            unsigned int *pNeeded) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetCCWData(
            CLRDATA_ADDRESS ccw,
            struct DacpCCWData *data) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetCCWInterfaces(
            CLRDATA_ADDRESS ccw,
            unsigned int count,
            struct DacpCOMInterfacePointerData *interfaces,
            unsigned int *pNeeded) = 0;

        virtual HRESULT STDMETHODCALLTYPE TraverseRCWCleanupList(
            CLRDATA_ADDRESS cleanupListPtr,
            VISITRCWFORCLEANUP pCallback,
            LPVOID token) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetStackReferences(
            /* [in] */ DWORD osThreadID,
            /* [out] */ ISOSStackRefEnum **ppEnum) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetRegisterName(
            /* [in] */ int regName,
            /* [in] */ unsigned int count,
            /* [out] */ WCHAR *buffer,
            /* [out] */ unsigned int *pNeeded) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetThreadAllocData(
            CLRDATA_ADDRESS thread,
            struct DacpAllocData *data) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetHeapAllocData(
            unsigned int count,
            struct DacpGenerationAllocData *data,
            unsigned int *pNeeded) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetFailedAssemblyList(
            CLRDATA_ADDRESS appDomain,
            int count,
            CLRDATA_ADDRESS values[  ],
            unsigned int *pNeeded) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetPrivateBinPaths(
            CLRDATA_ADDRESS appDomain,
            int count,
            WCHAR *paths,
            unsigned int *pNeeded) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetAssemblyLocation(
            CLRDATA_ADDRESS assembly,
            int count,
            WCHAR *location,
            unsigned int *pNeeded) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetAppDomainConfigFile(
            CLRDATA_ADDRESS appDomain,
            int count,
            WCHAR *configFile,
            unsigned int *pNeeded) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetApplicationBase(
            CLRDATA_ADDRESS appDomain,
            int count,
            WCHAR *base,
            unsigned int *pNeeded) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetFailedAssemblyData(
            CLRDATA_ADDRESS assembly,
            unsigned int *pContext,
            HRESULT *pResult) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetFailedAssemblyLocation(
            CLRDATA_ADDRESS assesmbly,
            unsigned int count,
            WCHAR *location,
            unsigned int *pNeeded) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetFailedAssemblyDisplayName(
            CLRDATA_ADDRESS assembly,
            unsigned int count,
            WCHAR *name,
            unsigned int *pNeeded) = 0;

    };


#else   /* C style interface */

    typedef struct ISOSDacInterfaceVtbl
    {
        BEGIN_INTERFACE

        HRESULT ( STDMETHODCALLTYPE *QueryInterface )(
            ISOSDacInterface * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */
            _COM_Outptr_  void **ppvObject);

        ULONG ( STDMETHODCALLTYPE *AddRef )(
            ISOSDacInterface * This);

        ULONG ( STDMETHODCALLTYPE *Release )(
            ISOSDacInterface * This);

        HRESULT ( STDMETHODCALLTYPE *GetThreadStoreData )(
            ISOSDacInterface * This,
            struct DacpThreadStoreData *data);

        HRESULT ( STDMETHODCALLTYPE *GetAppDomainStoreData )(
            ISOSDacInterface * This,
            struct DacpAppDomainStoreData *data);

        HRESULT ( STDMETHODCALLTYPE *GetAppDomainList )(
            ISOSDacInterface * This,
            unsigned int count,
            CLRDATA_ADDRESS values[  ],
            unsigned int *pNeeded);

        HRESULT ( STDMETHODCALLTYPE *GetAppDomainData )(
            ISOSDacInterface * This,
            CLRDATA_ADDRESS addr,
            struct DacpAppDomainData *data);

        HRESULT ( STDMETHODCALLTYPE *GetAppDomainName )(
            ISOSDacInterface * This,
            CLRDATA_ADDRESS addr,
            unsigned int count,
            WCHAR *name,
            unsigned int *pNeeded);

        HRESULT ( STDMETHODCALLTYPE *GetDomainFromContext )(
            ISOSDacInterface * This,
            CLRDATA_ADDRESS context,
            CLRDATA_ADDRESS *domain);

        HRESULT ( STDMETHODCALLTYPE *GetAssemblyList )(
            ISOSDacInterface * This,
            CLRDATA_ADDRESS appDomain,
            int count,
            CLRDATA_ADDRESS values[  ],
            int *pNeeded);

        HRESULT ( STDMETHODCALLTYPE *GetAssemblyData )(
            ISOSDacInterface * This,
            CLRDATA_ADDRESS baseDomainPtr,
            CLRDATA_ADDRESS assembly,
            struct DacpAssemblyData *data);

        HRESULT ( STDMETHODCALLTYPE *GetAssemblyName )(
            ISOSDacInterface * This,
            CLRDATA_ADDRESS assembly,
            unsigned int count,
            WCHAR *name,
            unsigned int *pNeeded);

        HRESULT ( STDMETHODCALLTYPE *GetModule )(
            ISOSDacInterface * This,
            CLRDATA_ADDRESS addr,
            IXCLRDataModule **mod);

        HRESULT ( STDMETHODCALLTYPE *GetModuleData )(
            ISOSDacInterface * This,
            CLRDATA_ADDRESS moduleAddr,
            struct DacpModuleData *data);

        HRESULT ( STDMETHODCALLTYPE *TraverseModuleMap )(
            ISOSDacInterface * This,
            ModuleMapType mmt,
            CLRDATA_ADDRESS moduleAddr,
            MODULEMAPTRAVERSE pCallback,
            LPVOID token);

        HRESULT ( STDMETHODCALLTYPE *GetAssemblyModuleList )(
            ISOSDacInterface * This,
            CLRDATA_ADDRESS assembly,
            unsigned int count,
            CLRDATA_ADDRESS modules[  ],
            unsigned int *pNeeded);

        HRESULT ( STDMETHODCALLTYPE *GetILForModule )(
            ISOSDacInterface * This,
            CLRDATA_ADDRESS moduleAddr,
            DWORD rva,
            CLRDATA_ADDRESS *il);

        HRESULT ( STDMETHODCALLTYPE *GetThreadData )(
            ISOSDacInterface * This,
            CLRDATA_ADDRESS thread,
            struct DacpThreadData *data);

        HRESULT ( STDMETHODCALLTYPE *GetThreadFromThinlockID )(
            ISOSDacInterface * This,
            UINT thinLockId,
            CLRDATA_ADDRESS *pThread);

        HRESULT ( STDMETHODCALLTYPE *GetStackLimits )(
            ISOSDacInterface * This,
            CLRDATA_ADDRESS threadPtr,
            CLRDATA_ADDRESS *lower,
            CLRDATA_ADDRESS *upper,
            CLRDATA_ADDRESS *fp);

        HRESULT ( STDMETHODCALLTYPE *GetMethodDescData )(
            ISOSDacInterface * This,
            CLRDATA_ADDRESS methodDesc,
            CLRDATA_ADDRESS ip,
            struct DacpMethodDescData *data,
            ULONG cRevertedRejitVersions,
            struct DacpReJitData *rgRevertedRejitData,
            ULONG *pcNeededRevertedRejitData);

        HRESULT ( STDMETHODCALLTYPE *GetMethodDescPtrFromIP )(
            ISOSDacInterface * This,
            CLRDATA_ADDRESS ip,
            CLRDATA_ADDRESS *ppMD);

        HRESULT ( STDMETHODCALLTYPE *GetMethodDescName )(
            ISOSDacInterface * This,
            CLRDATA_ADDRESS methodDesc,
            unsigned int count,
            WCHAR *name,
            unsigned int *pNeeded);

        HRESULT ( STDMETHODCALLTYPE *GetMethodDescPtrFromFrame )(
            ISOSDacInterface * This,
            CLRDATA_ADDRESS frameAddr,
            CLRDATA_ADDRESS *ppMD);

        HRESULT ( STDMETHODCALLTYPE *GetMethodDescFromToken )(
            ISOSDacInterface * This,
            CLRDATA_ADDRESS moduleAddr,
            mdToken token,
            CLRDATA_ADDRESS *methodDesc);

        HRESULT ( STDMETHODCALLTYPE *GetMethodDescTransparencyData )(
            ISOSDacInterface * This,
            CLRDATA_ADDRESS methodDesc,
            struct DacpMethodDescTransparencyData *data);

        HRESULT ( STDMETHODCALLTYPE *GetCodeHeaderData )(
            ISOSDacInterface * This,
            CLRDATA_ADDRESS ip,
            struct DacpCodeHeaderData *data);

        HRESULT ( STDMETHODCALLTYPE *GetJitManagerList )(
            ISOSDacInterface * This,
            unsigned int count,
            struct DacpJitManagerInfo *managers,
            unsigned int *pNeeded);

        HRESULT ( STDMETHODCALLTYPE *GetJitHelperFunctionName )(
            ISOSDacInterface * This,
            CLRDATA_ADDRESS ip,
            unsigned int count,
            char *name,
            unsigned int *pNeeded);

        HRESULT ( STDMETHODCALLTYPE *GetJumpThunkTarget )(
            ISOSDacInterface * This,
            T_CONTEXT *ctx,
            CLRDATA_ADDRESS *targetIP,
            CLRDATA_ADDRESS *targetMD);

        HRESULT ( STDMETHODCALLTYPE *GetThreadpoolData )(
            ISOSDacInterface * This,
            struct DacpThreadpoolData *data);

        HRESULT ( STDMETHODCALLTYPE *GetWorkRequestData )(
            ISOSDacInterface * This,
            CLRDATA_ADDRESS addrWorkRequest,
            struct DacpWorkRequestData *data);

        HRESULT ( STDMETHODCALLTYPE *GetHillClimbingLogEntry )(
            ISOSDacInterface * This,
            CLRDATA_ADDRESS addr,
            struct DacpHillClimbingLogEntry *data);

        HRESULT ( STDMETHODCALLTYPE *GetObjectData )(
            ISOSDacInterface * This,
            CLRDATA_ADDRESS objAddr,
            struct DacpObjectData *data);

        HRESULT ( STDMETHODCALLTYPE *GetObjectStringData )(
            ISOSDacInterface * This,
            CLRDATA_ADDRESS obj,
            unsigned int count,
            WCHAR *stringData,
            unsigned int *pNeeded);

        HRESULT ( STDMETHODCALLTYPE *GetObjectClassName )(
            ISOSDacInterface * This,
            CLRDATA_ADDRESS obj,
            unsigned int count,
            WCHAR *className,
            unsigned int *pNeeded);

        HRESULT ( STDMETHODCALLTYPE *GetMethodTableName )(
            ISOSDacInterface * This,
            CLRDATA_ADDRESS mt,
            unsigned int count,
            WCHAR *mtName,
            unsigned int *pNeeded);

        HRESULT ( STDMETHODCALLTYPE *GetMethodTableData )(
            ISOSDacInterface * This,
            CLRDATA_ADDRESS mt,
            struct DacpMethodTableData *data);

        HRESULT ( STDMETHODCALLTYPE *GetMethodTableSlot )(
            ISOSDacInterface * This,
            CLRDATA_ADDRESS mt,
            unsigned int slot,
            CLRDATA_ADDRESS *value);

        HRESULT ( STDMETHODCALLTYPE *GetMethodTableFieldData )(
            ISOSDacInterface * This,
            CLRDATA_ADDRESS mt,
            struct DacpMethodTableFieldData *data);

        HRESULT ( STDMETHODCALLTYPE *GetMethodTableTransparencyData )(
            ISOSDacInterface * This,
            CLRDATA_ADDRESS mt,
            struct DacpMethodTableTransparencyData *data);

        HRESULT ( STDMETHODCALLTYPE *GetMethodTableForEEClass )(
            ISOSDacInterface * This,
            CLRDATA_ADDRESS eeClass,
            CLRDATA_ADDRESS *value);

        HRESULT ( STDMETHODCALLTYPE *GetFieldDescData )(
            ISOSDacInterface * This,
            CLRDATA_ADDRESS fieldDesc,
            struct DacpFieldDescData *data);

        HRESULT ( STDMETHODCALLTYPE *GetFrameName )(
            ISOSDacInterface * This,
            CLRDATA_ADDRESS vtable,
            unsigned int count,
            WCHAR *frameName,
            unsigned int *pNeeded);

        HRESULT ( STDMETHODCALLTYPE *GetPEFileBase )(
            ISOSDacInterface * This,
            CLRDATA_ADDRESS addr,
            CLRDATA_ADDRESS *base);

        HRESULT ( STDMETHODCALLTYPE *GetPEFileName )(
            ISOSDacInterface * This,
            CLRDATA_ADDRESS addr,
            unsigned int count,
            WCHAR *fileName,
            unsigned int *pNeeded);

        HRESULT ( STDMETHODCALLTYPE *GetGCHeapData )(
            ISOSDacInterface * This,
            struct DacpGcHeapData *data);

        HRESULT ( STDMETHODCALLTYPE *GetGCHeapList )(
            ISOSDacInterface * This,
            unsigned int count,
            CLRDATA_ADDRESS heaps[  ],
            unsigned int *pNeeded);

        HRESULT ( STDMETHODCALLTYPE *GetGCHeapDetails )(
            ISOSDacInterface * This,
            CLRDATA_ADDRESS heap,
            struct DacpGcHeapDetails *details);

        HRESULT ( STDMETHODCALLTYPE *GetGCHeapStaticData )(
            ISOSDacInterface * This,
            struct DacpGcHeapDetails *data);

        HRESULT ( STDMETHODCALLTYPE *GetHeapSegmentData )(
            ISOSDacInterface * This,
            CLRDATA_ADDRESS seg,
            struct DacpHeapSegmentData *data);

        HRESULT ( STDMETHODCALLTYPE *GetOOMData )(
            ISOSDacInterface * This,
            CLRDATA_ADDRESS oomAddr,
            struct DacpOomData *data);

        HRESULT ( STDMETHODCALLTYPE *GetOOMStaticData )(
            ISOSDacInterface * This,
            struct DacpOomData *data);

        HRESULT ( STDMETHODCALLTYPE *GetHeapAnalyzeData )(
            ISOSDacInterface * This,
            CLRDATA_ADDRESS addr,
            struct DacpGcHeapAnalyzeData *data);

        HRESULT ( STDMETHODCALLTYPE *GetHeapAnalyzeStaticData )(
            ISOSDacInterface * This,
            struct DacpGcHeapAnalyzeData *data);

        HRESULT ( STDMETHODCALLTYPE *GetDomainLocalModuleData )(
            ISOSDacInterface * This,
            CLRDATA_ADDRESS addr,
            struct DacpDomainLocalModuleData *data);

        HRESULT ( STDMETHODCALLTYPE *GetDomainLocalModuleDataFromAppDomain )(
            ISOSDacInterface * This,
            CLRDATA_ADDRESS appDomainAddr,
            int moduleID,
            struct DacpDomainLocalModuleData *data);

        HRESULT ( STDMETHODCALLTYPE *GetDomainLocalModuleDataFromModule )(
            ISOSDacInterface * This,
            CLRDATA_ADDRESS moduleAddr,
            struct DacpDomainLocalModuleData *data);

        HRESULT ( STDMETHODCALLTYPE *GetThreadLocalModuleData )(
            ISOSDacInterface * This,
            CLRDATA_ADDRESS thread,
            unsigned int index,
            struct DacpThreadLocalModuleData *data);

        HRESULT ( STDMETHODCALLTYPE *GetSyncBlockData )(
            ISOSDacInterface * This,
            unsigned int number,
            struct DacpSyncBlockData *data);

        HRESULT ( STDMETHODCALLTYPE *GetSyncBlockCleanupData )(
            ISOSDacInterface * This,
            CLRDATA_ADDRESS addr,
            struct DacpSyncBlockCleanupData *data);

        HRESULT ( STDMETHODCALLTYPE *GetHandleEnum )(
            ISOSDacInterface * This,
            ISOSHandleEnum **ppHandleEnum);

        HRESULT ( STDMETHODCALLTYPE *GetHandleEnumForTypes )(
            ISOSDacInterface * This,
            unsigned int types[  ],
            unsigned int count,
            ISOSHandleEnum **ppHandleEnum);

        HRESULT ( STDMETHODCALLTYPE *GetHandleEnumForGC )(
            ISOSDacInterface * This,
            unsigned int gen,
            ISOSHandleEnum **ppHandleEnum);

        HRESULT ( STDMETHODCALLTYPE *TraverseEHInfo )(
            ISOSDacInterface * This,
            CLRDATA_ADDRESS ip,
            DUMPEHINFO pCallback,
            LPVOID token);

        HRESULT ( STDMETHODCALLTYPE *GetNestedExceptionData )(
            ISOSDacInterface * This,
            CLRDATA_ADDRESS exception,
            CLRDATA_ADDRESS *exceptionObject,
            CLRDATA_ADDRESS *nextNestedException);

        HRESULT ( STDMETHODCALLTYPE *GetStressLogAddress )(
            ISOSDacInterface * This,
            CLRDATA_ADDRESS *stressLog);

        HRESULT ( STDMETHODCALLTYPE *TraverseLoaderHeap )(
            ISOSDacInterface * This,
            CLRDATA_ADDRESS loaderHeapAddr,
            VISITHEAP pCallback);

        HRESULT ( STDMETHODCALLTYPE *GetCodeHeapList )(
            ISOSDacInterface * This,
            CLRDATA_ADDRESS jitManager,
            unsigned int count,
            struct DacpJitCodeHeapInfo *codeHeaps,
            unsigned int *pNeeded);

        HRESULT ( STDMETHODCALLTYPE *TraverseVirtCallStubHeap )(
            ISOSDacInterface * This,
            CLRDATA_ADDRESS pAppDomain,
            VCSHeapType heaptype,
            VISITHEAP pCallback);

        HRESULT ( STDMETHODCALLTYPE *GetUsefulGlobals )(
            ISOSDacInterface * This,
            struct DacpUsefulGlobalsData *data);

        HRESULT ( STDMETHODCALLTYPE *GetClrWatsonBuckets )(
            ISOSDacInterface * This,
            CLRDATA_ADDRESS thread,
            void *pGenericModeBlock);

        HRESULT ( STDMETHODCALLTYPE *GetTLSIndex )(
            ISOSDacInterface * This,
            ULONG *pIndex);

        HRESULT ( STDMETHODCALLTYPE *GetDacModuleHandle )(
            ISOSDacInterface * This,
            HMODULE *phModule);

        HRESULT ( STDMETHODCALLTYPE *GetRCWData )(
            ISOSDacInterface * This,
            CLRDATA_ADDRESS addr,
            struct DacpRCWData *data);

        HRESULT ( STDMETHODCALLTYPE *GetRCWInterfaces )(
            ISOSDacInterface * This,
            CLRDATA_ADDRESS rcw,
            unsigned int count,
            struct DacpCOMInterfacePointerData *interfaces,
            unsigned int *pNeeded);

        HRESULT ( STDMETHODCALLTYPE *GetCCWData )(
            ISOSDacInterface * This,
            CLRDATA_ADDRESS ccw,
            struct DacpCCWData *data);

        HRESULT ( STDMETHODCALLTYPE *GetCCWInterfaces )(
            ISOSDacInterface * This,
            CLRDATA_ADDRESS ccw,
            unsigned int count,
            struct DacpCOMInterfacePointerData *interfaces,
            unsigned int *pNeeded);

        HRESULT ( STDMETHODCALLTYPE *TraverseRCWCleanupList )(
            ISOSDacInterface * This,
            CLRDATA_ADDRESS cleanupListPtr,
            VISITRCWFORCLEANUP pCallback,
            LPVOID token);

        HRESULT ( STDMETHODCALLTYPE *GetStackReferences )(
            ISOSDacInterface * This,
            /* [in] */ DWORD osThreadID,
            /* [out] */ ISOSStackRefEnum **ppEnum);

        HRESULT ( STDMETHODCALLTYPE *GetRegisterName )(
            ISOSDacInterface * This,
            /* [in] */ int regName,
            /* [in] */ unsigned int count,
            /* [out] */ WCHAR *buffer,
            /* [out] */ unsigned int *pNeeded);

        HRESULT ( STDMETHODCALLTYPE *GetThreadAllocData )(
            ISOSDacInterface * This,
            CLRDATA_ADDRESS thread,
            struct DacpAllocData *data);

        HRESULT ( STDMETHODCALLTYPE *GetHeapAllocData )(
            ISOSDacInterface * This,
            unsigned int count,
            struct DacpGenerationAllocData *data,
            unsigned int *pNeeded);

        HRESULT ( STDMETHODCALLTYPE *GetFailedAssemblyList )(
            ISOSDacInterface * This,
            CLRDATA_ADDRESS appDomain,
            int count,
            CLRDATA_ADDRESS values[  ],
            unsigned int *pNeeded);

        HRESULT ( STDMETHODCALLTYPE *GetPrivateBinPaths )(
            ISOSDacInterface * This,
            CLRDATA_ADDRESS appDomain,
            int count,
            WCHAR *paths,
            unsigned int *pNeeded);

        HRESULT ( STDMETHODCALLTYPE *GetAssemblyLocation )(
            ISOSDacInterface * This,
            CLRDATA_ADDRESS assembly,
            int count,
            WCHAR *location,
            unsigned int *pNeeded);

        HRESULT ( STDMETHODCALLTYPE *GetAppDomainConfigFile )(
            ISOSDacInterface * This,
            CLRDATA_ADDRESS appDomain,
            int count,
            WCHAR *configFile,
            unsigned int *pNeeded);

        HRESULT ( STDMETHODCALLTYPE *GetApplicationBase )(
            ISOSDacInterface * This,
            CLRDATA_ADDRESS appDomain,
            int count,
            WCHAR *base,
            unsigned int *pNeeded);

        HRESULT ( STDMETHODCALLTYPE *GetFailedAssemblyData )(
            ISOSDacInterface * This,
            CLRDATA_ADDRESS assembly,
            unsigned int *pContext,
            HRESULT *pResult);

        HRESULT ( STDMETHODCALLTYPE *GetFailedAssemblyLocation )(
            ISOSDacInterface * This,
            CLRDATA_ADDRESS assesmbly,
            unsigned int count,
            WCHAR *location,
            unsigned int *pNeeded);

        HRESULT ( STDMETHODCALLTYPE *GetFailedAssemblyDisplayName )(
            ISOSDacInterface * This,
            CLRDATA_ADDRESS assembly,
            unsigned int count,
            WCHAR *name,
            unsigned int *pNeeded);

        END_INTERFACE
    } ISOSDacInterfaceVtbl;

    interface ISOSDacInterface
    {
        CONST_VTBL struct ISOSDacInterfaceVtbl *lpVtbl;
    };



#ifdef COBJMACROS


#define ISOSDacInterface_QueryInterface(This,riid,ppvObject)    \
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) )

#define ISOSDacInterface_AddRef(This)   \
    ( (This)->lpVtbl -> AddRef(This) )

#define ISOSDacInterface_Release(This)  \
    ( (This)->lpVtbl -> Release(This) )


#define ISOSDacInterface_GetThreadStoreData(This,data)  \
    ( (This)->lpVtbl -> GetThreadStoreData(This,data) )

#define ISOSDacInterface_GetAppDomainStoreData(This,data)   \
    ( (This)->lpVtbl -> GetAppDomainStoreData(This,data) )

#define ISOSDacInterface_GetAppDomainList(This,count,values,pNeeded)    \
    ( (This)->lpVtbl -> GetAppDomainList(This,count,values,pNeeded) )

#define ISOSDacInterface_GetAppDomainData(This,addr,data)   \
    ( (This)->lpVtbl -> GetAppDomainData(This,addr,data) )

#define ISOSDacInterface_GetAppDomainName(This,addr,count,name,pNeeded) \
    ( (This)->lpVtbl -> GetAppDomainName(This,addr,count,name,pNeeded) )

#define ISOSDacInterface_GetDomainFromContext(This,context,domain)  \
    ( (This)->lpVtbl -> GetDomainFromContext(This,context,domain) )

#define ISOSDacInterface_GetAssemblyList(This,appDomain,count,values,pNeeded)   \
    ( (This)->lpVtbl -> GetAssemblyList(This,appDomain,count,values,pNeeded) )

#define ISOSDacInterface_GetAssemblyData(This,baseDomainPtr,assembly,data)  \
    ( (This)->lpVtbl -> GetAssemblyData(This,baseDomainPtr,assembly,data) )

#define ISOSDacInterface_GetAssemblyName(This,assembly,count,name,pNeeded)  \
    ( (This)->lpVtbl -> GetAssemblyName(This,assembly,count,name,pNeeded) )

#define ISOSDacInterface_GetModule(This,addr,mod)   \
    ( (This)->lpVtbl -> GetModule(This,addr,mod) )

#define ISOSDacInterface_GetModuleData(This,moduleAddr,data)    \
    ( (This)->lpVtbl -> GetModuleData(This,moduleAddr,data) )

#define ISOSDacInterface_TraverseModuleMap(This,mmt,moduleAddr,pCallback,token) \
    ( (This)->lpVtbl -> TraverseModuleMap(This,mmt,moduleAddr,pCallback,token) )

#define ISOSDacInterface_GetAssemblyModuleList(This,assembly,count,modules,pNeeded) \
    ( (This)->lpVtbl -> GetAssemblyModuleList(This,assembly,count,modules,pNeeded) )

#define ISOSDacInterface_GetILForModule(This,moduleAddr,rva,il) \
    ( (This)->lpVtbl -> GetILForModule(This,moduleAddr,rva,il) )

#define ISOSDacInterface_GetThreadData(This,thread,data)    \
    ( (This)->lpVtbl -> GetThreadData(This,thread,data) )

#define ISOSDacInterface_GetThreadFromThinlockID(This,thinLockId,pThread)   \
    ( (This)->lpVtbl -> GetThreadFromThinlockID(This,thinLockId,pThread) )

#define ISOSDacInterface_GetStackLimits(This,threadPtr,lower,upper,fp)  \
    ( (This)->lpVtbl -> GetStackLimits(This,threadPtr,lower,upper,fp) )

#define ISOSDacInterface_GetMethodDescData(This,methodDesc,ip,data,cRevertedRejitVersions,rgRevertedRejitData,pcNeededRevertedRejitData)    \
    ( (This)->lpVtbl -> GetMethodDescData(This,methodDesc,ip,data,cRevertedRejitVersions,rgRevertedRejitData,pcNeededRevertedRejitData) )

#define ISOSDacInterface_GetMethodDescPtrFromIP(This,ip,ppMD)   \
    ( (This)->lpVtbl -> GetMethodDescPtrFromIP(This,ip,ppMD) )

#define ISOSDacInterface_GetMethodDescName(This,methodDesc,count,name,pNeeded)  \
    ( (This)->lpVtbl -> GetMethodDescName(This,methodDesc,count,name,pNeeded) )

#define ISOSDacInterface_GetMethodDescPtrFromFrame(This,frameAddr,ppMD) \
    ( (This)->lpVtbl -> GetMethodDescPtrFromFrame(This,frameAddr,ppMD) )

#define ISOSDacInterface_GetMethodDescFromToken(This,moduleAddr,token,methodDesc)   \
    ( (This)->lpVtbl -> GetMethodDescFromToken(This,moduleAddr,token,methodDesc) )

#define ISOSDacInterface_GetMethodDescTransparencyData(This,methodDesc,data)    \
    ( (This)->lpVtbl -> GetMethodDescTransparencyData(This,methodDesc,data) )

#define ISOSDacInterface_GetCodeHeaderData(This,ip,data)    \
    ( (This)->lpVtbl -> GetCodeHeaderData(This,ip,data) )

#define ISOSDacInterface_GetJitManagerList(This,count,managers,pNeeded) \
    ( (This)->lpVtbl -> GetJitManagerList(This,count,managers,pNeeded) )

#define ISOSDacInterface_GetJitHelperFunctionName(This,ip,count,name,pNeeded)   \
    ( (This)->lpVtbl -> GetJitHelperFunctionName(This,ip,count,name,pNeeded) )

#define ISOSDacInterface_GetJumpThunkTarget(This,ctx,targetIP,targetMD) \
    ( (This)->lpVtbl -> GetJumpThunkTarget(This,ctx,targetIP,targetMD) )

#define ISOSDacInterface_GetThreadpoolData(This,data)   \
    ( (This)->lpVtbl -> GetThreadpoolData(This,data) )

#define ISOSDacInterface_GetWorkRequestData(This,addrWorkRequest,data)  \
    ( (This)->lpVtbl -> GetWorkRequestData(This,addrWorkRequest,data) )

#define ISOSDacInterface_GetHillClimbingLogEntry(This,addr,data)    \
    ( (This)->lpVtbl -> GetHillClimbingLogEntry(This,addr,data) )

#define ISOSDacInterface_GetObjectData(This,objAddr,data)   \
    ( (This)->lpVtbl -> GetObjectData(This,objAddr,data) )

#define ISOSDacInterface_GetObjectStringData(This,obj,count,stringData,pNeeded) \
    ( (This)->lpVtbl -> GetObjectStringData(This,obj,count,stringData,pNeeded) )

#define ISOSDacInterface_GetObjectClassName(This,obj,count,className,pNeeded)   \
    ( (This)->lpVtbl -> GetObjectClassName(This,obj,count,className,pNeeded) )

#define ISOSDacInterface_GetMethodTableName(This,mt,count,mtName,pNeeded)   \
    ( (This)->lpVtbl -> GetMethodTableName(This,mt,count,mtName,pNeeded) )

#define ISOSDacInterface_GetMethodTableData(This,mt,data)   \
    ( (This)->lpVtbl -> GetMethodTableData(This,mt,data) )

#define ISOSDacInterface_GetMethodTableSlot(This,mt,slot,value) \
    ( (This)->lpVtbl -> GetMethodTableSlot(This,mt,slot,value) )

#define ISOSDacInterface_GetMethodTableFieldData(This,mt,data)  \
    ( (This)->lpVtbl -> GetMethodTableFieldData(This,mt,data) )

#define ISOSDacInterface_GetMethodTableTransparencyData(This,mt,data)   \
    ( (This)->lpVtbl -> GetMethodTableTransparencyData(This,mt,data) )

#define ISOSDacInterface_GetMethodTableForEEClass(This,eeClass,value)   \
    ( (This)->lpVtbl -> GetMethodTableForEEClass(This,eeClass,value) )

#define ISOSDacInterface_GetFieldDescData(This,fieldDesc,data)  \
    ( (This)->lpVtbl -> GetFieldDescData(This,fieldDesc,data) )

#define ISOSDacInterface_GetFrameName(This,vtable,count,frameName,pNeeded)  \
    ( (This)->lpVtbl -> GetFrameName(This,vtable,count,frameName,pNeeded) )

#define ISOSDacInterface_GetPEFileBase(This,addr,base)  \
    ( (This)->lpVtbl -> GetPEFileBase(This,addr,base) )

#define ISOSDacInterface_GetPEFileName(This,addr,count,fileName,pNeeded)    \
    ( (This)->lpVtbl -> GetPEFileName(This,addr,count,fileName,pNeeded) )

#define ISOSDacInterface_GetGCHeapData(This,data)   \
    ( (This)->lpVtbl -> GetGCHeapData(This,data) )

#define ISOSDacInterface_GetGCHeapList(This,count,heaps,pNeeded)    \
    ( (This)->lpVtbl -> GetGCHeapList(This,count,heaps,pNeeded) )

#define ISOSDacInterface_GetGCHeapDetails(This,heap,details)    \
    ( (This)->lpVtbl -> GetGCHeapDetails(This,heap,details) )

#define ISOSDacInterface_GetGCHeapStaticData(This,data) \
    ( (This)->lpVtbl -> GetGCHeapStaticData(This,data) )

#define ISOSDacInterface_GetHeapSegmentData(This,seg,data)  \
    ( (This)->lpVtbl -> GetHeapSegmentData(This,seg,data) )

#define ISOSDacInterface_GetOOMData(This,oomAddr,data)  \
    ( (This)->lpVtbl -> GetOOMData(This,oomAddr,data) )

#define ISOSDacInterface_GetOOMStaticData(This,data)    \
    ( (This)->lpVtbl -> GetOOMStaticData(This,data) )

#define ISOSDacInterface_GetHeapAnalyzeData(This,addr,data) \
    ( (This)->lpVtbl -> GetHeapAnalyzeData(This,addr,data) )

#define ISOSDacInterface_GetHeapAnalyzeStaticData(This,data)    \
    ( (This)->lpVtbl -> GetHeapAnalyzeStaticData(This,data) )

#define ISOSDacInterface_GetDomainLocalModuleData(This,addr,data)   \
    ( (This)->lpVtbl -> GetDomainLocalModuleData(This,addr,data) )

#define ISOSDacInterface_GetDomainLocalModuleDataFromAppDomain(This,appDomainAddr,moduleID,data)    \
    ( (This)->lpVtbl -> GetDomainLocalModuleDataFromAppDomain(This,appDomainAddr,moduleID,data) )

#define ISOSDacInterface_GetDomainLocalModuleDataFromModule(This,moduleAddr,data)   \
    ( (This)->lpVtbl -> GetDomainLocalModuleDataFromModule(This,moduleAddr,data) )

#define ISOSDacInterface_GetThreadLocalModuleData(This,thread,index,data)   \
    ( (This)->lpVtbl -> GetThreadLocalModuleData(This,thread,index,data) )

#define ISOSDacInterface_GetSyncBlockData(This,number,data) \
    ( (This)->lpVtbl -> GetSyncBlockData(This,number,data) )

#define ISOSDacInterface_GetSyncBlockCleanupData(This,addr,data)    \
    ( (This)->lpVtbl -> GetSyncBlockCleanupData(This,addr,data) )

#define ISOSDacInterface_GetHandleEnum(This,ppHandleEnum)   \
    ( (This)->lpVtbl -> GetHandleEnum(This,ppHandleEnum) )

#define ISOSDacInterface_GetHandleEnumForTypes(This,types,count,ppHandleEnum)   \
    ( (This)->lpVtbl -> GetHandleEnumForTypes(This,types,count,ppHandleEnum) )

#define ISOSDacInterface_GetHandleEnumForGC(This,gen,ppHandleEnum)  \
    ( (This)->lpVtbl -> GetHandleEnumForGC(This,gen,ppHandleEnum) )

#define ISOSDacInterface_TraverseEHInfo(This,ip,pCallback,token)    \
    ( (This)->lpVtbl -> TraverseEHInfo(This,ip,pCallback,token) )

#define ISOSDacInterface_GetNestedExceptionData(This,exception,exceptionObject,nextNestedException) \
    ( (This)->lpVtbl -> GetNestedExceptionData(This,exception,exceptionObject,nextNestedException) )

#define ISOSDacInterface_GetStressLogAddress(This,stressLog)    \
    ( (This)->lpVtbl -> GetStressLogAddress(This,stressLog) )

#define ISOSDacInterface_TraverseLoaderHeap(This,loaderHeapAddr,pCallback)  \
    ( (This)->lpVtbl -> TraverseLoaderHeap(This,loaderHeapAddr,pCallback) )

#define ISOSDacInterface_GetCodeHeapList(This,jitManager,count,codeHeaps,pNeeded)   \
    ( (This)->lpVtbl -> GetCodeHeapList(This,jitManager,count,codeHeaps,pNeeded) )

#define ISOSDacInterface_TraverseVirtCallStubHeap(This,pAppDomain,heaptype,pCallback)   \
    ( (This)->lpVtbl -> TraverseVirtCallStubHeap(This,pAppDomain,heaptype,pCallback) )

#define ISOSDacInterface_GetUsefulGlobals(This,data)    \
    ( (This)->lpVtbl -> GetUsefulGlobals(This,data) )

#define ISOSDacInterface_GetClrWatsonBuckets(This,thread,pGenericModeBlock) \
    ( (This)->lpVtbl -> GetClrWatsonBuckets(This,thread,pGenericModeBlock) )

#define ISOSDacInterface_GetTLSIndex(This,pIndex)   \
    ( (This)->lpVtbl -> GetTLSIndex(This,pIndex) )

#define ISOSDacInterface_GetDacModuleHandle(This,phModule)  \
    ( (This)->lpVtbl -> GetDacModuleHandle(This,phModule) )

#define ISOSDacInterface_GetRCWData(This,addr,data) \
    ( (This)->lpVtbl -> GetRCWData(This,addr,data) )

#define ISOSDacInterface_GetRCWInterfaces(This,rcw,count,interfaces,pNeeded)    \
    ( (This)->lpVtbl -> GetRCWInterfaces(This,rcw,count,interfaces,pNeeded) )

#define ISOSDacInterface_GetCCWData(This,ccw,data)  \
    ( (This)->lpVtbl -> GetCCWData(This,ccw,data) )

#define ISOSDacInterface_GetCCWInterfaces(This,ccw,count,interfaces,pNeeded)    \
    ( (This)->lpVtbl -> GetCCWInterfaces(This,ccw,count,interfaces,pNeeded) )

#define ISOSDacInterface_TraverseRCWCleanupList(This,cleanupListPtr,pCallback,token)    \
    ( (This)->lpVtbl -> TraverseRCWCleanupList(This,cleanupListPtr,pCallback,token) )

#define ISOSDacInterface_GetStackReferences(This,osThreadID,ppEnum) \
    ( (This)->lpVtbl -> GetStackReferences(This,osThreadID,ppEnum) )

#define ISOSDacInterface_GetRegisterName(This,regName,count,buffer,pNeeded) \
    ( (This)->lpVtbl -> GetRegisterName(This,regName,count,buffer,pNeeded) )

#define ISOSDacInterface_GetThreadAllocData(This,thread,data)   \
    ( (This)->lpVtbl -> GetThreadAllocData(This,thread,data) )

#define ISOSDacInterface_GetHeapAllocData(This,count,data,pNeeded)  \
    ( (This)->lpVtbl -> GetHeapAllocData(This,count,data,pNeeded) )

#define ISOSDacInterface_GetFailedAssemblyList(This,appDomain,count,values,pNeeded) \
    ( (This)->lpVtbl -> GetFailedAssemblyList(This,appDomain,count,values,pNeeded) )

#define ISOSDacInterface_GetPrivateBinPaths(This,appDomain,count,paths,pNeeded) \
    ( (This)->lpVtbl -> GetPrivateBinPaths(This,appDomain,count,paths,pNeeded) )

#define ISOSDacInterface_GetAssemblyLocation(This,assembly,count,location,pNeeded)  \
    ( (This)->lpVtbl -> GetAssemblyLocation(This,assembly,count,location,pNeeded) )

#define ISOSDacInterface_GetAppDomainConfigFile(This,appDomain,count,configFile,pNeeded)    \
    ( (This)->lpVtbl -> GetAppDomainConfigFile(This,appDomain,count,configFile,pNeeded) )

#define ISOSDacInterface_GetApplicationBase(This,appDomain,count,base,pNeeded)  \
    ( (This)->lpVtbl -> GetApplicationBase(This,appDomain,count,base,pNeeded) )

#define ISOSDacInterface_GetFailedAssemblyData(This,assembly,pContext,pResult)  \
    ( (This)->lpVtbl -> GetFailedAssemblyData(This,assembly,pContext,pResult) )

#define ISOSDacInterface_GetFailedAssemblyLocation(This,assesmbly,count,location,pNeeded)   \
    ( (This)->lpVtbl -> GetFailedAssemblyLocation(This,assesmbly,count,location,pNeeded) )

#define ISOSDacInterface_GetFailedAssemblyDisplayName(This,assembly,count,name,pNeeded) \
    ( (This)->lpVtbl -> GetFailedAssemblyDisplayName(This,assembly,count,name,pNeeded) )

#endif /* COBJMACROS */


#endif  /* C style interface */




#endif  /* __ISOSDacInterface_INTERFACE_DEFINED__ */


#ifndef __ISOSDacInterface2_INTERFACE_DEFINED__
#define __ISOSDacInterface2_INTERFACE_DEFINED__

/* interface ISOSDacInterface2 */
/* [uuid][local][object] */


EXTERN_C const IID IID_ISOSDacInterface2;

#if defined(__cplusplus) && !defined(CINTERFACE)

    MIDL_INTERFACE("A16026EC-96F4-40BA-87FB-5575986FB7AF")
    ISOSDacInterface2 : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE GetObjectExceptionData(
            CLRDATA_ADDRESS objAddr,
            struct DacpExceptionObjectData *data) = 0;

        virtual HRESULT STDMETHODCALLTYPE IsRCWDCOMProxy(
            CLRDATA_ADDRESS rcwAddr,
            BOOL *isDCOMProxy) = 0;

    };


#else   /* C style interface */

    typedef struct ISOSDacInterface2Vtbl
    {
        BEGIN_INTERFACE

        HRESULT ( STDMETHODCALLTYPE *QueryInterface )(
            ISOSDacInterface2 * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */
            _COM_Outptr_  void **ppvObject);

        ULONG ( STDMETHODCALLTYPE *AddRef )(
            ISOSDacInterface2 * This);

        ULONG ( STDMETHODCALLTYPE *Release )(
            ISOSDacInterface2 * This);

        HRESULT ( STDMETHODCALLTYPE *GetObjectExceptionData )(
            ISOSDacInterface2 * This,
            CLRDATA_ADDRESS objAddr,
            struct DacpExceptionObjectData *data);

        HRESULT ( STDMETHODCALLTYPE *IsRCWDCOMProxy )(
            ISOSDacInterface2 * This,
            CLRDATA_ADDRESS rcwAddr,
            BOOL *isDCOMProxy);

        END_INTERFACE
    } ISOSDacInterface2Vtbl;

    interface ISOSDacInterface2
    {
        CONST_VTBL struct ISOSDacInterface2Vtbl *lpVtbl;
    };



#ifdef COBJMACROS


#define ISOSDacInterface2_QueryInterface(This,riid,ppvObject)   \
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) )

#define ISOSDacInterface2_AddRef(This)  \
    ( (This)->lpVtbl -> AddRef(This) )

#define ISOSDacInterface2_Release(This) \
    ( (This)->lpVtbl -> Release(This) )


#define ISOSDacInterface2_GetObjectExceptionData(This,objAddr,data) \
    ( (This)->lpVtbl -> GetObjectExceptionData(This,objAddr,data) )

#define ISOSDacInterface2_IsRCWDCOMProxy(This,rcwAddr,isDCOMProxy)  \
    ( (This)->lpVtbl -> IsRCWDCOMProxy(This,rcwAddr,isDCOMProxy) )

#endif /* COBJMACROS */


#endif  /* C style interface */




#endif  /* __ISOSDacInterface2_INTERFACE_DEFINED__ */


#ifndef __ISOSDacInterface3_INTERFACE_DEFINED__
#define __ISOSDacInterface3_INTERFACE_DEFINED__

/* interface ISOSDacInterface3 */
/* [uuid][local][object] */


EXTERN_C const IID IID_ISOSDacInterface3;

#if defined(__cplusplus) && !defined(CINTERFACE)

    MIDL_INTERFACE("B08C5CDC-FD8A-49C5-AB38-5FEEF35235B4")
    ISOSDacInterface3 : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE GetGCInterestingInfoData(
            CLRDATA_ADDRESS interestingInfoAddr,
            struct DacpGCInterestingInfoData *data) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetGCInterestingInfoStaticData(
            struct DacpGCInterestingInfoData *data) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetGCGlobalMechanisms(
            size_t *globalMechanisms) = 0;

    };


#else   /* C style interface */

    typedef struct ISOSDacInterface3Vtbl
    {
        BEGIN_INTERFACE

        HRESULT ( STDMETHODCALLTYPE *QueryInterface )(
            ISOSDacInterface3 * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */
            _COM_Outptr_  void **ppvObject);

        ULONG ( STDMETHODCALLTYPE *AddRef )(
            ISOSDacInterface3 * This);

        ULONG ( STDMETHODCALLTYPE *Release )(
            ISOSDacInterface3 * This);

        HRESULT ( STDMETHODCALLTYPE *GetGCInterestingInfoData )(
            ISOSDacInterface3 * This,
            CLRDATA_ADDRESS interestingInfoAddr,
            struct DacpGCInterestingInfoData *data);

        HRESULT ( STDMETHODCALLTYPE *GetGCInterestingInfoStaticData )(
            ISOSDacInterface3 * This,
            struct DacpGCInterestingInfoData *data);

        HRESULT ( STDMETHODCALLTYPE *GetGCGlobalMechanisms )(
            ISOSDacInterface3 * This,
            size_t *globalMechanisms);

        END_INTERFACE
    } ISOSDacInterface3Vtbl;

    interface ISOSDacInterface3
    {
        CONST_VTBL struct ISOSDacInterface3Vtbl *lpVtbl;
    };



#ifdef COBJMACROS


#define ISOSDacInterface3_QueryInterface(This,riid,ppvObject)   \
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) )

#define ISOSDacInterface3_AddRef(This)  \
    ( (This)->lpVtbl -> AddRef(This) )

#define ISOSDacInterface3_Release(This) \
    ( (This)->lpVtbl -> Release(This) )


#define ISOSDacInterface3_GetGCInterestingInfoData(This,interestingInfoAddr,data)   \
    ( (This)->lpVtbl -> GetGCInterestingInfoData(This,interestingInfoAddr,data) )

#define ISOSDacInterface3_GetGCInterestingInfoStaticData(This,data) \
    ( (This)->lpVtbl -> GetGCInterestingInfoStaticData(This,data) )

#define ISOSDacInterface3_GetGCGlobalMechanisms(This,globalMechanisms)  \
    ( (This)->lpVtbl -> GetGCGlobalMechanisms(This,globalMechanisms) )

#endif /* COBJMACROS */


#endif  /* C style interface */




#endif  /* __ISOSDacInterface3_INTERFACE_DEFINED__ */


#ifndef __ISOSDacInterface4_INTERFACE_DEFINED__
#define __ISOSDacInterface4_INTERFACE_DEFINED__

/* interface ISOSDacInterface4 */
/* [uuid][local][object] */


EXTERN_C const IID IID_ISOSDacInterface4;

#if defined(__cplusplus) && !defined(CINTERFACE)

    MIDL_INTERFACE("74B9D34C-A612-4B07-93DD-5462178FCE11")
    ISOSDacInterface4 : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE GetClrNotification(
            CLRDATA_ADDRESS arguments[  ],
            int count,
            int *pNeeded) = 0;

    };


#else   /* C style interface */

    typedef struct ISOSDacInterface4Vtbl
    {
        BEGIN_INTERFACE

        HRESULT ( STDMETHODCALLTYPE *QueryInterface )(
            ISOSDacInterface4 * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */
            _COM_Outptr_  void **ppvObject);

        ULONG ( STDMETHODCALLTYPE *AddRef )(
            ISOSDacInterface4 * This);

        ULONG ( STDMETHODCALLTYPE *Release )(
            ISOSDacInterface4 * This);

        HRESULT ( STDMETHODCALLTYPE *GetClrNotification )(
            ISOSDacInterface4 * This,
            CLRDATA_ADDRESS arguments[  ],
            int count,
            int *pNeeded);

        END_INTERFACE
    } ISOSDacInterface4Vtbl;

    interface ISOSDacInterface4
    {
        CONST_VTBL struct ISOSDacInterface4Vtbl *lpVtbl;
    };



#ifdef COBJMACROS


#define ISOSDacInterface4_QueryInterface(This,riid,ppvObject)   \
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) )

#define ISOSDacInterface4_AddRef(This)  \
    ( (This)->lpVtbl -> AddRef(This) )

#define ISOSDacInterface4_Release(This) \
    ( (This)->lpVtbl -> Release(This) )


#define ISOSDacInterface4_GetClrNotification(This,arguments,count,pNeeded)  \
    ( (This)->lpVtbl -> GetClrNotification(This,arguments,count,pNeeded) )

#endif /* COBJMACROS */


#endif  /* C style interface */




#endif  /* __ISOSDacInterface4_INTERFACE_DEFINED__ */


#ifndef __ISOSDacInterface5_INTERFACE_DEFINED__
#define __ISOSDacInterface5_INTERFACE_DEFINED__

/* interface ISOSDacInterface5 */
/* [uuid][local][object] */


EXTERN_C const IID IID_ISOSDacInterface5;

#if defined(__cplusplus) && !defined(CINTERFACE)

    MIDL_INTERFACE("127d6abe-6c86-4e48-8e7b-220781c58101")
    ISOSDacInterface5 : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE GetTieredVersions(
            CLRDATA_ADDRESS methodDesc,
            int rejitId,
            struct DacpTieredVersionData *nativeCodeAddrs,
            int cNativeCodeAddrs,
            int *pcNativeCodeAddrs) = 0;

    };


#else   /* C style interface */

    typedef struct ISOSDacInterface5Vtbl
    {
        BEGIN_INTERFACE

        HRESULT ( STDMETHODCALLTYPE *QueryInterface )(
            ISOSDacInterface5 * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */
            _COM_Outptr_  void **ppvObject);

        ULONG ( STDMETHODCALLTYPE *AddRef )(
            ISOSDacInterface5 * This);

        ULONG ( STDMETHODCALLTYPE *Release )(
            ISOSDacInterface5 * This);

        HRESULT ( STDMETHODCALLTYPE *GetTieredVersions )(
            ISOSDacInterface5 * This,
            CLRDATA_ADDRESS methodDesc,
            int rejitId,
            struct DacpTieredVersionData *nativeCodeAddrs,
            int cNativeCodeAddrs,
            int *pcNativeCodeAddrs);

        END_INTERFACE
    } ISOSDacInterface5Vtbl;

    interface ISOSDacInterface5
    {
        CONST_VTBL struct ISOSDacInterface5Vtbl *lpVtbl;
    };



#ifdef COBJMACROS


#define ISOSDacInterface5_QueryInterface(This,riid,ppvObject)   \
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) )

#define ISOSDacInterface5_AddRef(This)  \
    ( (This)->lpVtbl -> AddRef(This) )

#define ISOSDacInterface5_Release(This) \
    ( (This)->lpVtbl -> Release(This) )


#define ISOSDacInterface5_GetTieredVersions(This,methodDesc,rejitId,nativeCodeAddrs,cNativeCodeAddrs,pcNativeCodeAddrs) \
    ( (This)->lpVtbl -> GetTieredVersions(This,methodDesc,rejitId,nativeCodeAddrs,cNativeCodeAddrs,pcNativeCodeAddrs) )

#endif /* COBJMACROS */


#endif  /* C style interface */




#endif  /* __ISOSDacInterface5_INTERFACE_DEFINED__ */


#ifndef __ISOSDacInterface6_INTERFACE_DEFINED__
#define __ISOSDacInterface6_INTERFACE_DEFINED__

/* interface ISOSDacInterface6 */
/* [uuid][local][object] */


EXTERN_C const IID IID_ISOSDacInterface6;

#if defined(__cplusplus) && !defined(CINTERFACE)

    MIDL_INTERFACE("11206399-4B66-4EDB-98EA-85654E59AD45")
    ISOSDacInterface6 : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE GetMethodTableCollectibleData(
            CLRDATA_ADDRESS mt,
            struct DacpMethodTableCollectibleData *data) = 0;

    };


#else   /* C style interface */

    typedef struct ISOSDacInterface6Vtbl
    {
        BEGIN_INTERFACE

        HRESULT ( STDMETHODCALLTYPE *QueryInterface )(
            ISOSDacInterface6 * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */
            _COM_Outptr_  void **ppvObject);

        ULONG ( STDMETHODCALLTYPE *AddRef )(
            ISOSDacInterface6 * This);

        ULONG ( STDMETHODCALLTYPE *Release )(
            ISOSDacInterface6 * This);

        HRESULT ( STDMETHODCALLTYPE *GetMethodTableCollectibleData )(
            ISOSDacInterface6 * This,
            CLRDATA_ADDRESS mt,
            struct DacpMethodTableCollectibleData *data);

        END_INTERFACE
    } ISOSDacInterface6Vtbl;

    interface ISOSDacInterface6
    {
        CONST_VTBL struct ISOSDacInterface6Vtbl *lpVtbl;
    };



#ifdef COBJMACROS


#define ISOSDacInterface6_QueryInterface(This,riid,ppvObject)   \
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) )

#define ISOSDacInterface6_AddRef(This)  \
    ( (This)->lpVtbl -> AddRef(This) )

#define ISOSDacInterface6_Release(This) \
    ( (This)->lpVtbl -> Release(This) )


#define ISOSDacInterface6_GetMethodTableCollectibleData(This,mt,data)   \
    ( (This)->lpVtbl -> GetMethodTableCollectibleData(This,mt,data) )

#endif /* COBJMACROS */


#endif  /* C style interface */




#endif  /* __ISOSDacInterface6_INTERFACE_DEFINED__ */


#ifndef __ISOSDacInterface7_INTERFACE_DEFINED__
#define __ISOSDacInterface7_INTERFACE_DEFINED__

/* interface ISOSDacInterface7 */
/* [uuid][local][object] */


EXTERN_C const IID IID_ISOSDacInterface7;

#if defined(__cplusplus) && !defined(CINTERFACE)

    MIDL_INTERFACE("c1020dde-fe98-4536-a53b-f35a74c327eb")
    ISOSDacInterface7 : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE GetPendingReJITID(
            CLRDATA_ADDRESS methodDesc,
            int *pRejitId) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetReJITInformation(
            CLRDATA_ADDRESS methodDesc,
            int rejitId,
            struct DacpReJitData2 *pRejitData) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetProfilerModifiedILInformation(
            CLRDATA_ADDRESS methodDesc,
            struct DacpProfilerILData *pILData) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetMethodsWithProfilerModifiedIL(
            CLRDATA_ADDRESS mod,
            CLRDATA_ADDRESS *methodDescs,
            int cMethodDescs,
            int *pcMethodDescs) = 0;

    };


#else   /* C style interface */

    typedef struct ISOSDacInterface7Vtbl
    {
        BEGIN_INTERFACE

        HRESULT ( STDMETHODCALLTYPE *QueryInterface )(
            ISOSDacInterface7 * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */
            _COM_Outptr_  void **ppvObject);

        ULONG ( STDMETHODCALLTYPE *AddRef )(
            ISOSDacInterface7 * This);

        ULONG ( STDMETHODCALLTYPE *Release )(
            ISOSDacInterface7 * This);

        HRESULT ( STDMETHODCALLTYPE *GetPendingReJITID )(
            ISOSDacInterface7 * This,
            CLRDATA_ADDRESS methodDesc,
            int *pRejitId);

        HRESULT ( STDMETHODCALLTYPE *GetReJITInformation )(
            ISOSDacInterface7 * This,
            CLRDATA_ADDRESS methodDesc,
            int rejitId,
            struct DacpReJitData2 *pRejitData);

        HRESULT ( STDMETHODCALLTYPE *GetProfilerModifiedILInformation )(
            ISOSDacInterface7 * This,
            CLRDATA_ADDRESS methodDesc,
            struct DacpProfilerILData *pILData);

        HRESULT ( STDMETHODCALLTYPE *GetMethodsWithProfilerModifiedIL )(
            ISOSDacInterface7 * This,
            CLRDATA_ADDRESS mod,
            CLRDATA_ADDRESS *methodDescs,
            int cMethodDescs,
            int *pcMethodDescs);

        END_INTERFACE
    } ISOSDacInterface7Vtbl;

    interface ISOSDacInterface7
    {
        CONST_VTBL struct ISOSDacInterface7Vtbl *lpVtbl;
    };



#ifdef COBJMACROS


#define ISOSDacInterface7_QueryInterface(This,riid,ppvObject)   \
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) )

#define ISOSDacInterface7_AddRef(This)  \
    ( (This)->lpVtbl -> AddRef(This) )

#define ISOSDacInterface7_Release(This) \
    ( (This)->lpVtbl -> Release(This) )


#define ISOSDacInterface7_GetPendingReJITID(This,methodDesc,pRejitId)   \
    ( (This)->lpVtbl -> GetPendingReJITID(This,methodDesc,pRejitId) )

#define ISOSDacInterface7_GetReJITInformation(This,methodDesc,rejitId,pRejitData)   \
    ( (This)->lpVtbl -> GetReJITInformation(This,methodDesc,rejitId,pRejitData) )

#define ISOSDacInterface7_GetProfilerModifiedILInformation(This,methodDesc,pILData) \
    ( (This)->lpVtbl -> GetProfilerModifiedILInformation(This,methodDesc,pILData) )

#define ISOSDacInterface7_GetMethodsWithProfilerModifiedIL(This,mod,methodDescs,cMethodDescs,pcMethodDescs) \
    ( (This)->lpVtbl -> GetMethodsWithProfilerModifiedIL(This,mod,methodDescs,cMethodDescs,pcMethodDescs) )

#endif /* COBJMACROS */


#endif  /* C style interface */




#endif  /* __ISOSDacInterface7_INTERFACE_DEFINED__ */


#ifndef __ISOSDacInterface8_INTERFACE_DEFINED__
#define __ISOSDacInterface8_INTERFACE_DEFINED__

/* interface ISOSDacInterface8 */
/* [uuid][local][object] */


EXTERN_C const IID IID_ISOSDacInterface8;

#if defined(__cplusplus) && !defined(CINTERFACE)

    MIDL_INTERFACE("c12f35a9-e55c-4520-a894-b3dc5165dfce")
    ISOSDacInterface8 : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE GetNumberGenerations(
            unsigned int *pGenerations) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetGenerationTable(
            unsigned int cGenerations,
            struct DacpGenerationData *pGenerationData,
            unsigned int *pNeeded) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetFinalizationFillPointers(
            unsigned int cFillPointers,
            CLRDATA_ADDRESS *pFinalizationFillPointers,
            unsigned int *pNeeded) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetGenerationTableSvr(
            CLRDATA_ADDRESS heapAddr,
            unsigned int cGenerations,
            struct DacpGenerationData *pGenerationData,
            unsigned int *pNeeded) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetFinalizationFillPointersSvr(
            CLRDATA_ADDRESS heapAddr,
            unsigned int cFillPointers,
            CLRDATA_ADDRESS *pFinalizationFillPointers,
            unsigned int *pNeeded) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetAssemblyLoadContext(
            CLRDATA_ADDRESS methodTable,
            CLRDATA_ADDRESS* assemblyLoadContext) = 0;
    };


#else   /* C style interface */

    typedef struct ISOSDacInterface8Vtbl
    {
        BEGIN_INTERFACE

        HRESULT ( STDMETHODCALLTYPE *QueryInterface )(
            ISOSDacInterface8 * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */
            _COM_Outptr_  void **ppvObject);

        ULONG ( STDMETHODCALLTYPE *AddRef )(
            ISOSDacInterface8 * This);

        ULONG ( STDMETHODCALLTYPE *Release )(
            ISOSDacInterface8 * This);

        HRESULT ( STDMETHODCALLTYPE *GetNumberGenerations )(
            ISOSDacInterface8 * This,
            unsigned int *pGenerations);

        HRESULT ( STDMETHODCALLTYPE *GetGenerationTable )(
            ISOSDacInterface8 * This,
            unsigned int cGenerations,
            struct DacpGenerationData *pGenerationData,
            unsigned int *pNeeded);

        HRESULT ( STDMETHODCALLTYPE *GetFinalizationFillPointers )(
            ISOSDacInterface8 * This,
            unsigned int cFillPointers,
            CLRDATA_ADDRESS *pFinalizationFillPointers,
            unsigned int *pNeeded);

        HRESULT ( STDMETHODCALLTYPE *GetGenerationTableSvr )(
            ISOSDacInterface8 * This,
            CLRDATA_ADDRESS heapAddr,
            unsigned int cGenerations,
            struct DacpGenerationData *pGenerationData,
            unsigned int *pNeeded);

        HRESULT ( STDMETHODCALLTYPE *GetFinalizationFillPointersSvr )(
            ISOSDacInterface8 * This,
            CLRDATA_ADDRESS heapAddr,
            unsigned int cFillPointers,
            CLRDATA_ADDRESS *pFinalizationFillPointers,
            unsigned int *pNeeded);

        HRESULT ( STDMETHODCALLTYPE *GetAssemblyLoadContext )(
            ISOSDacInterface8 * This,
            CLRDATA_ADDRESS methodTable,
            CLRDATA_ADDRESS *assemblyLoadContext);
           
        END_INTERFACE
    } ISOSDacInterface8Vtbl;

    interface ISOSDacInterface8
    {
        CONST_VTBL struct ISOSDacInterface8Vtbl *lpVtbl;
    };


#ifdef COBJMACROS


#define ISOSDacInterface8_QueryInterface(This,riid,ppvObject)   \
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) )

#define ISOSDacInterface8_AddRef(This)  \
    ( (This)->lpVtbl -> AddRef(This) )

#define ISOSDacInterface8_Release(This) \
    ( (This)->lpVtbl -> Release(This) )


#define ISOSDacInterface8_GetNumberGenerations(This,pGenerations)   \
    ( (This)->lpVtbl -> GetNumberGenerations(This,pGenerations) )

#define ISOSDacInterface8_GetGenerationTable(This,cGenerations,pGenerationData,pNeeded) \
    ( (This)->lpVtbl -> GetGenerationTable(This,cGenerations,pGenerationData,pNeeded) )

#define ISOSDacInterface8_GetFinalizationFillPointers(This,cFillPointers,pFinalizationFillPointers,pNeeded) \
    ( (This)->lpVtbl -> GetFinalizationFillPointers(This,cFillPointers,pFinalizationFillPointers,pNeeded) )

#define ISOSDacInterface8_GetGenerationTableSvr(This,heapAddr,cGenerations,pGenerationData,pNeeded) \
    ( (This)->lpVtbl -> GetGenerationTableSvr(This,heapAddr,cGenerations,pGenerationData,pNeeded) )

#define ISOSDacInterface8_GetFinalizationFillPointersSvr(This,heapAddr,cFillPointers,pFinalizationFillPointers,pNeeded) \
    ( (This)->lpVtbl -> GetFinalizationFillPointersSvr(This,heapAddr,cFillPointers,pFinalizationFillPointers,pNeeded) )

#define ISOSDacInterface8_GetAssemblyLoadContext(This,methodTable,assemblyLoadContext) \
    ( (This)->lpVtbl -> GetAssemblyLoadContext(This,methodTable,assemblyLoadContext) )

#endif /* COBJMACROS */


#endif  /* C style interface */




#endif  /* __ISOSDacInterface8_INTERFACE_DEFINED__ */


/* interface __MIDL_itf_sospriv_0000_0012 */
/* [local] */ 

#define SOS_BREAKING_CHANGE_VERSION 3


extern RPC_IF_HANDLE __MIDL_itf_sospriv_0000_0012_v0_0_c_ifspec;
extern RPC_IF_HANDLE __MIDL_itf_sospriv_0000_0012_v0_0_s_ifspec;

#ifndef __ISOSDacInterface9_INTERFACE_DEFINED__
#define __ISOSDacInterface9_INTERFACE_DEFINED__

/* interface ISOSDacInterface9 */
/* [uuid][local][object] */ 


EXTERN_C const IID IID_ISOSDacInterface9;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("4eca42d8-7e7b-4c8a-a116-7bfbf6929267")
    ISOSDacInterface9 : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE GetBreakingChangeVersion( 
            int *pVersion) = 0;
        
    };
    
    
#else  /* C style interface */

    typedef struct ISOSDacInterface9Vtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            ISOSDacInterface9 * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            ISOSDacInterface9 * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            ISOSDacInterface9 * This);
        
        HRESULT ( STDMETHODCALLTYPE *GetBreakingChangeVersion )( 
            ISOSDacInterface9 * This,
            int *pVersion);
        
        END_INTERFACE
    } ISOSDacInterface9Vtbl;

    interface ISOSDacInterface9
    {
        CONST_VTBL struct ISOSDacInterface9Vtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define ISOSDacInterface9_QueryInterface(This,riid,ppvObject) \
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define ISOSDacInterface9_AddRef(This) \
    ( (This)->lpVtbl -> AddRef(This) ) 

#define ISOSDacInterface9_Release(This) \
    ( (This)->lpVtbl -> Release(This) ) 


#define ISOSDacInterface9_GetBreakingChangeVersion(This,pVersion) \
    ( (This)->lpVtbl -> GetBreakingChangeVersion(This,pVersion) ) 

#endif /* COBJMACROS */


#endif  /* C style interface */


#endif  /* __ISOSDacInterface9_INTERFACE_DEFINED__ */


#ifndef __ISOSDacInterface10_INTERFACE_DEFINED__
#define __ISOSDacInterface10_INTERFACE_DEFINED__

/* interface ISOSDacInterface10 */
/* [uuid][local][object] */ 


EXTERN_C const IID IID_ISOSDacInterface10;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("90B8FCC3-7251-4B0A-AE3D-5C13A67EC9AA")
    ISOSDacInterface10 : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE GetObjectComWrappersData( 
            CLRDATA_ADDRESS objAddr,
            CLRDATA_ADDRESS *rcw,
            unsigned int count,
            CLRDATA_ADDRESS *mowList,
            unsigned int *pNeeded) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE IsComWrappersCCW( 
            CLRDATA_ADDRESS ccw,
            BOOL *isComWrappersCCW) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetComWrappersCCWData( 
            CLRDATA_ADDRESS ccw,
            CLRDATA_ADDRESS *managedObject,
            int *refCount) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE IsComWrappersRCW( 
            CLRDATA_ADDRESS rcw,
            BOOL *isComWrappersRCW) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetComWrappersRCWData( 
            CLRDATA_ADDRESS rcw,
            CLRDATA_ADDRESS *identity) = 0;
        
    };
    
    
#else   /* C style interface */

    typedef struct ISOSDacInterface10Vtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            ISOSDacInterface10 * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            ISOSDacInterface10 * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            ISOSDacInterface10 * This);
        
        HRESULT ( STDMETHODCALLTYPE *GetObjectComWrappersData )( 
            ISOSDacInterface10 * This,
            CLRDATA_ADDRESS objAddr,
            CLRDATA_ADDRESS *rcw,
            unsigned int count,
            CLRDATA_ADDRESS *mowList,
            unsigned int *pNeeded);
        
        HRESULT ( STDMETHODCALLTYPE *IsComWrappersCCW )( 
            ISOSDacInterface10 * This,
            CLRDATA_ADDRESS ccw,
            BOOL *isComWrappersCCW);
        
        HRESULT ( STDMETHODCALLTYPE *GetComWrappersCCWData )( 
            ISOSDacInterface10 * This,
            CLRDATA_ADDRESS ccw,
            CLRDATA_ADDRESS *managedObject,
            int *refCount);
        
        HRESULT ( STDMETHODCALLTYPE *IsComWrappersRCW )( 
            ISOSDacInterface10 * This,
            CLRDATA_ADDRESS rcw,
            BOOL *isComWrappersRCW);
        
        HRESULT ( STDMETHODCALLTYPE *GetComWrappersRCWData )( 
            ISOSDacInterface10 * This,
            CLRDATA_ADDRESS rcw,
            CLRDATA_ADDRESS *identity);
        
        END_INTERFACE
    } ISOSDacInterface10Vtbl;

    interface ISOSDacInterface10
    {
        CONST_VTBL struct ISOSDacInterface10Vtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define ISOSDacInterface10_QueryInterface(This,riid,ppvObject)  \
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define ISOSDacInterface10_AddRef(This) \
    ( (This)->lpVtbl -> AddRef(This) ) 

#define ISOSDacInterface10_Release(This)    \
    ( (This)->lpVtbl -> Release(This) ) 


#define ISOSDacInterface10_GetObjectComWrappersData(This,objAddr,rcw,count,mowList,pNeeded) \
    ( (This)->lpVtbl -> GetObjectComWrappersData(This,objAddr,rcw,count,mowList,pNeeded) ) 

#define ISOSDacInterface10_IsComWrappersCCW(This,ccw,isComWrappersCCW)  \
    ( (This)->lpVtbl -> IsComWrappersCCW(This,ccw,isComWrappersCCW) ) 

#define ISOSDacInterface10_GetComWrappersCCWData(This,ccw,managedObject,refCount)   \
    ( (This)->lpVtbl -> GetComWrappersCCWData(This,ccw,managedObject,refCount) ) 

#define ISOSDacInterface10_IsComWrappersRCW(This,rcw,isComWrappersRCW)  \
    ( (This)->lpVtbl -> IsComWrappersRCW(This,rcw,isComWrappersRCW) ) 

#define ISOSDacInterface10_GetComWrappersRCWData(This,rcw,identity) \
    ( (This)->lpVtbl -> GetComWrappersRCWData(This,rcw,identity) ) 

#endif /* COBJMACROS */


#endif  /* C style interface */




#endif  /* __ISOSDacInterface10_INTERFACE_DEFINED__ */

#ifndef __ISOSDacInterface11_INTERFACE_DEFINED__
#define __ISOSDacInterface11_INTERFACE_DEFINED__

/* interface ISOSDacInterface11 */
/* [uuid][local][object] */ 


EXTERN_C const IID IID_ISOSDacInterface11;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("96BA1DB9-14CD-4492-8065-1CAAECF6E5CF")
    ISOSDacInterface11 : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE IsTrackedType( 
            CLRDATA_ADDRESS objAddr,
            BOOL *isTrackedType,
            BOOL *hasTaggedMemory) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetTaggedMemory( 
            CLRDATA_ADDRESS objAddr,
            CLRDATA_ADDRESS *taggedMemory,
            size_t *taggedMemorySizeInBytes) = 0;
        
    };
    
    
#else  /* C style interface */

    typedef struct ISOSDacInterface11Vtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            ISOSDacInterface11 * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            ISOSDacInterface11 * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            ISOSDacInterface11 * This);
        
        HRESULT ( STDMETHODCALLTYPE *IsTrackedType )( 
            ISOSDacInterface11 * This,
            CLRDATA_ADDRESS objAddr,
            BOOL *isTrackedType,
            BOOL *hasTaggedMemory);
        
        HRESULT ( STDMETHODCALLTYPE *GetTaggedMemory )( 
            ISOSDacInterface11 * This,
            CLRDATA_ADDRESS objAddr,
            CLRDATA_ADDRESS *taggedMemory,
            size_t *taggedMemorySizeInBytes);
        
        END_INTERFACE
    } ISOSDacInterface11Vtbl;

    interface ISOSDacInterface11
    {
        CONST_VTBL struct ISOSDacInterface11Vtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define ISOSDacInterface11_QueryInterface(This,riid,ppvObject) \
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define ISOSDacInterface11_AddRef(This) \
    ( (This)->lpVtbl -> AddRef(This) ) 

#define ISOSDacInterface11_Release(This) \
    ( (This)->lpVtbl -> Release(This) ) 


#define ISOSDacInterface11_IsTrackedType(This,objAddr,isTrackedType,hasTaggedMemory) \
    ( (This)->lpVtbl -> IsTrackedType(This,objAddr,isTrackedType,hasTaggedMemory) ) 

#define ISOSDacInterface11_GetTaggedMemory(This,objAddr,taggedMemory,taggedMemorySizeInBytes) \
    ( (This)->lpVtbl -> GetTaggedMemory(This,objAddr,taggedMemory,taggedMemorySizeInBytes) ) 

#endif /* COBJMACROS */


#endif  /* C style interface */

#endif  /* __ISOSDacInterface11_INTERFACE_DEFINED__ */

#ifndef __ISOSDacInterface12_INTERFACE_DEFINED__
#define __ISOSDacInterface12_INTERFACE_DEFINED__

/* interface ISOSDacInterface12 */
/* [uuid][local][object] */ 


EXTERN_C const IID IID_ISOSDacInterface12;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("1b93bacc-8ca4-432d-943a-3e6e7ec0b0a3")
    ISOSDacInterface12 : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE GetGlobalAllocationContext( 
            CLRDATA_ADDRESS *allocPtr,
            CLRDATA_ADDRESS *allocLimit) = 0;
        
    };
    
    
#else 	/* C style interface */

    typedef struct ISOSDacInterface12Vtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            ISOSDacInterface12 * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            ISOSDacInterface12 * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            ISOSDacInterface12 * This);
        
        HRESULT ( STDMETHODCALLTYPE *GetGlobalAllocationContext )( 
            ISOSDacInterface12 * This,
            CLRDATA_ADDRESS *allocPtr,
            CLRDATA_ADDRESS *allocLimit);
        
        END_INTERFACE
    } ISOSDacInterface12Vtbl;

    interface ISOSDacInterface12
    {
        CONST_VTBL struct ISOSDacInterface12Vtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define ISOSDacInterface12_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define ISOSDacInterface12_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define ISOSDacInterface12_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define ISOSDacInterface12_GetGlobalAllocationContext(This,allocPtr,allocLimit)	\
    ( (This)->lpVtbl -> GetGlobalAllocationContext(This,allocPtr,allocLimit) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ISOSDacInterface12_INTERFACE_DEFINED__ */

    
#ifndef __ISOSDacInterface13_INTERFACE_DEFINED__
#define __ISOSDacInterface13_INTERFACE_DEFINED__

/* interface ISOSDacInterface13 */
/* [uuid][local][object] */ 


EXTERN_C const IID IID_ISOSDacInterface13;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("3176a8ed-597b-4f54-a71f-83695c6a8c5e")
    ISOSDacInterface13 : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE TraverseLoaderHeap( 
            CLRDATA_ADDRESS loaderHeapAddr,
            LoaderHeapKind kind,
            VISITHEAP pCallback) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetDomainLoaderAllocator( 
            CLRDATA_ADDRESS domainAddress,
            CLRDATA_ADDRESS *pLoaderAllocator) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetLoaderAllocatorHeapNames( 
            int count,
            const char **ppNames,
            int *pNeeded) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetLoaderAllocatorHeaps( 
            CLRDATA_ADDRESS loaderAllocator,
            int count,
            CLRDATA_ADDRESS *pLoaderHeaps,
            LoaderHeapKind *pKinds,
            int *pNeeded) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetHandleTableMemoryRegions( 
            ISOSMemoryEnum **ppEnum) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetGCBookkeepingMemoryRegions( 
            ISOSMemoryEnum **ppEnum) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetGCFreeRegions( 
            ISOSMemoryEnum **ppEnum) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE LockedFlush( void) = 0;
    };
    
    
#else 	/* C style interface */


    typedef struct ISOSDacInterface13Vtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            ISOSDacInterface13 * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            ISOSDacInterface13 * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            ISOSDacInterface13 * This);
        
        HRESULT ( STDMETHODCALLTYPE *TraverseLoaderHeap )( 
            ISOSDacInterface13 * This,
            CLRDATA_ADDRESS loaderHeapAddr,
            LoaderHeapKind kind,
            VISITHEAP pCallback);
        
        HRESULT ( STDMETHODCALLTYPE *GetDomainLoaderAllocator )( 
            ISOSDacInterface13 * This,
            CLRDATA_ADDRESS domainAddress,
            CLRDATA_ADDRESS *pLoaderAllocator);
        
        HRESULT ( STDMETHODCALLTYPE *GetLoaderAllocatorHeapNames )( 
            ISOSDacInterface13 * This,
            int count,
            const unsigned char **ppNames,
            int *pNeeded);
        
        HRESULT ( STDMETHODCALLTYPE *GetLoaderAllocatorHeaps )( 
            ISOSDacInterface13 * This,
            CLRDATA_ADDRESS loaderAllocator,
            int count,
            CLRDATA_ADDRESS *pLoaderHeaps,
            LoaderHeapKind *pKinds,
            int *pNeeded);
        
        HRESULT ( STDMETHODCALLTYPE *GetHandleTableMemoryRegions )( 
            ISOSDacInterface13 * This,
            ISOSMemoryEnum **ppEnum);
        
        HRESULT ( STDMETHODCALLTYPE *GetGCBookkeepingMemoryRegions )( 
            ISOSDacInterface13 * This,
            ISOSMemoryEnum **ppEnum);
        
        HRESULT ( STDMETHODCALLTYPE *GetGCFreeRegions )( 
            ISOSDacInterface13 * This,
            ISOSMemoryEnum **ppEnum);
        
        HRESULT ( STDMETHODCALLTYPE *LockedFlush )( 
            ISOSDacInterface13 * This);
        
        END_INTERFACE
    } ISOSDacInterface13Vtbl;

    interface ISOSDacInterface13
    {
        CONST_VTBL struct ISOSDacInterface13Vtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define ISOSDacInterface13_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define ISOSDacInterface13_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define ISOSDacInterface13_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define ISOSDacInterface13_TraverseLoaderHeap(This,loaderHeapAddr,kind,pCallback)	\
    ( (This)->lpVtbl -> TraverseLoaderHeap(This,loaderHeapAddr,kind,pCallback) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ISOSDacInterface13_INTERFACE_DEFINED__ */


/* Additional Prototypes for ALL interfaces */

/* end of Additional Prototypes */

#ifdef __cplusplus
}
#endif

#endif

