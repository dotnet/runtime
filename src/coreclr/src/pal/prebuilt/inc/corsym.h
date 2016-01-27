// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.



/* this ALWAYS GENERATED file contains the definitions for the interfaces */


 /* File created by MIDL compiler version 8.00.0601 */
/* @@MIDL_FILE_HEADING(  ) */

#pragma warning( disable: 4049 )  /* more than 64k source lines */


/* verify that the <rpcndr.h> version is high enough to compile this file*/
#ifndef __REQUIRED_RPCNDR_H_VERSION__
#define __REQUIRED_RPCNDR_H_VERSION__ 475
#endif

/* verify that the <rpcsal.h> version is high enough to compile this file*/
#ifndef __REQUIRED_RPCSAL_H_VERSION__
#define __REQUIRED_RPCSAL_H_VERSION__ 100
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

#ifndef __corsym_h__
#define __corsym_h__

#if defined(_MSC_VER) && (_MSC_VER >= 1020)
#pragma once
#endif

/* Forward Declarations */ 

#ifndef __CorSymWriter_deprecated_FWD_DEFINED__
#define __CorSymWriter_deprecated_FWD_DEFINED__

#ifdef __cplusplus
typedef class CorSymWriter_deprecated CorSymWriter_deprecated;
#else
typedef struct CorSymWriter_deprecated CorSymWriter_deprecated;
#endif /* __cplusplus */

#endif 	/* __CorSymWriter_deprecated_FWD_DEFINED__ */


#ifndef __CorSymReader_deprecated_FWD_DEFINED__
#define __CorSymReader_deprecated_FWD_DEFINED__

#ifdef __cplusplus
typedef class CorSymReader_deprecated CorSymReader_deprecated;
#else
typedef struct CorSymReader_deprecated CorSymReader_deprecated;
#endif /* __cplusplus */

#endif 	/* __CorSymReader_deprecated_FWD_DEFINED__ */


#ifndef __CorSymBinder_deprecated_FWD_DEFINED__
#define __CorSymBinder_deprecated_FWD_DEFINED__

#ifdef __cplusplus
typedef class CorSymBinder_deprecated CorSymBinder_deprecated;
#else
typedef struct CorSymBinder_deprecated CorSymBinder_deprecated;
#endif /* __cplusplus */

#endif 	/* __CorSymBinder_deprecated_FWD_DEFINED__ */


#ifndef __CorSymWriter_SxS_FWD_DEFINED__
#define __CorSymWriter_SxS_FWD_DEFINED__

#ifdef __cplusplus
typedef class CorSymWriter_SxS CorSymWriter_SxS;
#else
typedef struct CorSymWriter_SxS CorSymWriter_SxS;
#endif /* __cplusplus */

#endif 	/* __CorSymWriter_SxS_FWD_DEFINED__ */


#ifndef __CorSymReader_SxS_FWD_DEFINED__
#define __CorSymReader_SxS_FWD_DEFINED__

#ifdef __cplusplus
typedef class CorSymReader_SxS CorSymReader_SxS;
#else
typedef struct CorSymReader_SxS CorSymReader_SxS;
#endif /* __cplusplus */

#endif 	/* __CorSymReader_SxS_FWD_DEFINED__ */


#ifndef __CorSymBinder_SxS_FWD_DEFINED__
#define __CorSymBinder_SxS_FWD_DEFINED__

#ifdef __cplusplus
typedef class CorSymBinder_SxS CorSymBinder_SxS;
#else
typedef struct CorSymBinder_SxS CorSymBinder_SxS;
#endif /* __cplusplus */

#endif 	/* __CorSymBinder_SxS_FWD_DEFINED__ */


#ifndef __ISymUnmanagedBinder_FWD_DEFINED__
#define __ISymUnmanagedBinder_FWD_DEFINED__
typedef interface ISymUnmanagedBinder ISymUnmanagedBinder;

#endif 	/* __ISymUnmanagedBinder_FWD_DEFINED__ */


#ifndef __ISymUnmanagedBinder2_FWD_DEFINED__
#define __ISymUnmanagedBinder2_FWD_DEFINED__
typedef interface ISymUnmanagedBinder2 ISymUnmanagedBinder2;

#endif 	/* __ISymUnmanagedBinder2_FWD_DEFINED__ */


#ifndef __ISymUnmanagedBinder3_FWD_DEFINED__
#define __ISymUnmanagedBinder3_FWD_DEFINED__
typedef interface ISymUnmanagedBinder3 ISymUnmanagedBinder3;

#endif 	/* __ISymUnmanagedBinder3_FWD_DEFINED__ */


#ifndef __ISymUnmanagedDispose_FWD_DEFINED__
#define __ISymUnmanagedDispose_FWD_DEFINED__
typedef interface ISymUnmanagedDispose ISymUnmanagedDispose;

#endif 	/* __ISymUnmanagedDispose_FWD_DEFINED__ */


#ifndef __ISymUnmanagedDocument_FWD_DEFINED__
#define __ISymUnmanagedDocument_FWD_DEFINED__
typedef interface ISymUnmanagedDocument ISymUnmanagedDocument;

#endif 	/* __ISymUnmanagedDocument_FWD_DEFINED__ */


#ifndef __ISymUnmanagedDocumentWriter_FWD_DEFINED__
#define __ISymUnmanagedDocumentWriter_FWD_DEFINED__
typedef interface ISymUnmanagedDocumentWriter ISymUnmanagedDocumentWriter;

#endif 	/* __ISymUnmanagedDocumentWriter_FWD_DEFINED__ */


#ifndef __ISymUnmanagedMethod_FWD_DEFINED__
#define __ISymUnmanagedMethod_FWD_DEFINED__
typedef interface ISymUnmanagedMethod ISymUnmanagedMethod;

#endif 	/* __ISymUnmanagedMethod_FWD_DEFINED__ */


#ifndef __ISymENCUnmanagedMethod_FWD_DEFINED__
#define __ISymENCUnmanagedMethod_FWD_DEFINED__
typedef interface ISymENCUnmanagedMethod ISymENCUnmanagedMethod;

#endif 	/* __ISymENCUnmanagedMethod_FWD_DEFINED__ */


#ifndef __ISymUnmanagedNamespace_FWD_DEFINED__
#define __ISymUnmanagedNamespace_FWD_DEFINED__
typedef interface ISymUnmanagedNamespace ISymUnmanagedNamespace;

#endif 	/* __ISymUnmanagedNamespace_FWD_DEFINED__ */


#ifndef __ISymUnmanagedReader_FWD_DEFINED__
#define __ISymUnmanagedReader_FWD_DEFINED__
typedef interface ISymUnmanagedReader ISymUnmanagedReader;

#endif 	/* __ISymUnmanagedReader_FWD_DEFINED__ */


#ifndef __ISymUnmanagedSourceServerModule_FWD_DEFINED__
#define __ISymUnmanagedSourceServerModule_FWD_DEFINED__
typedef interface ISymUnmanagedSourceServerModule ISymUnmanagedSourceServerModule;

#endif 	/* __ISymUnmanagedSourceServerModule_FWD_DEFINED__ */


#ifndef __ISymUnmanagedENCUpdate_FWD_DEFINED__
#define __ISymUnmanagedENCUpdate_FWD_DEFINED__
typedef interface ISymUnmanagedENCUpdate ISymUnmanagedENCUpdate;

#endif 	/* __ISymUnmanagedENCUpdate_FWD_DEFINED__ */


#ifndef __ISymUnmanagedReaderSymbolSearchInfo_FWD_DEFINED__
#define __ISymUnmanagedReaderSymbolSearchInfo_FWD_DEFINED__
typedef interface ISymUnmanagedReaderSymbolSearchInfo ISymUnmanagedReaderSymbolSearchInfo;

#endif 	/* __ISymUnmanagedReaderSymbolSearchInfo_FWD_DEFINED__ */


#ifndef __ISymUnmanagedScope_FWD_DEFINED__
#define __ISymUnmanagedScope_FWD_DEFINED__
typedef interface ISymUnmanagedScope ISymUnmanagedScope;

#endif 	/* __ISymUnmanagedScope_FWD_DEFINED__ */


#ifndef __ISymUnmanagedConstant_FWD_DEFINED__
#define __ISymUnmanagedConstant_FWD_DEFINED__
typedef interface ISymUnmanagedConstant ISymUnmanagedConstant;

#endif 	/* __ISymUnmanagedConstant_FWD_DEFINED__ */


#ifndef __ISymUnmanagedScope2_FWD_DEFINED__
#define __ISymUnmanagedScope2_FWD_DEFINED__
typedef interface ISymUnmanagedScope2 ISymUnmanagedScope2;

#endif 	/* __ISymUnmanagedScope2_FWD_DEFINED__ */


#ifndef __ISymUnmanagedVariable_FWD_DEFINED__
#define __ISymUnmanagedVariable_FWD_DEFINED__
typedef interface ISymUnmanagedVariable ISymUnmanagedVariable;

#endif 	/* __ISymUnmanagedVariable_FWD_DEFINED__ */


#ifndef __ISymUnmanagedSymbolSearchInfo_FWD_DEFINED__
#define __ISymUnmanagedSymbolSearchInfo_FWD_DEFINED__
typedef interface ISymUnmanagedSymbolSearchInfo ISymUnmanagedSymbolSearchInfo;

#endif 	/* __ISymUnmanagedSymbolSearchInfo_FWD_DEFINED__ */


#ifndef __ISymUnmanagedWriter_FWD_DEFINED__
#define __ISymUnmanagedWriter_FWD_DEFINED__
typedef interface ISymUnmanagedWriter ISymUnmanagedWriter;

#endif 	/* __ISymUnmanagedWriter_FWD_DEFINED__ */


#ifndef __ISymUnmanagedWriter2_FWD_DEFINED__
#define __ISymUnmanagedWriter2_FWD_DEFINED__
typedef interface ISymUnmanagedWriter2 ISymUnmanagedWriter2;

#endif 	/* __ISymUnmanagedWriter2_FWD_DEFINED__ */


#ifndef __ISymUnmanagedWriter3_FWD_DEFINED__
#define __ISymUnmanagedWriter3_FWD_DEFINED__
typedef interface ISymUnmanagedWriter3 ISymUnmanagedWriter3;

#endif 	/* __ISymUnmanagedWriter3_FWD_DEFINED__ */


#ifndef __ISymUnmanagedWriter4_FWD_DEFINED__
#define __ISymUnmanagedWriter4_FWD_DEFINED__
typedef interface ISymUnmanagedWriter4 ISymUnmanagedWriter4;

#endif 	/* __ISymUnmanagedWriter4_FWD_DEFINED__ */


#ifndef __ISymUnmanagedWriter5_FWD_DEFINED__
#define __ISymUnmanagedWriter5_FWD_DEFINED__
typedef interface ISymUnmanagedWriter5 ISymUnmanagedWriter5;

#endif 	/* __ISymUnmanagedWriter5_FWD_DEFINED__ */


#ifndef __ISymUnmanagedReader2_FWD_DEFINED__
#define __ISymUnmanagedReader2_FWD_DEFINED__
typedef interface ISymUnmanagedReader2 ISymUnmanagedReader2;

#endif 	/* __ISymUnmanagedReader2_FWD_DEFINED__ */


#ifndef __ISymNGenWriter_FWD_DEFINED__
#define __ISymNGenWriter_FWD_DEFINED__
typedef interface ISymNGenWriter ISymNGenWriter;

#endif 	/* __ISymNGenWriter_FWD_DEFINED__ */


#ifndef __ISymNGenWriter2_FWD_DEFINED__
#define __ISymNGenWriter2_FWD_DEFINED__
typedef interface ISymNGenWriter2 ISymNGenWriter2;

#endif 	/* __ISymNGenWriter2_FWD_DEFINED__ */


#ifndef __ISymUnmanagedAsyncMethodPropertiesWriter_FWD_DEFINED__
#define __ISymUnmanagedAsyncMethodPropertiesWriter_FWD_DEFINED__
typedef interface ISymUnmanagedAsyncMethodPropertiesWriter ISymUnmanagedAsyncMethodPropertiesWriter;

#endif 	/* __ISymUnmanagedAsyncMethodPropertiesWriter_FWD_DEFINED__ */


#ifndef __ISymUnmanagedAsyncMethod_FWD_DEFINED__
#define __ISymUnmanagedAsyncMethod_FWD_DEFINED__
typedef interface ISymUnmanagedAsyncMethod ISymUnmanagedAsyncMethod;

#endif 	/* __ISymUnmanagedAsyncMethod_FWD_DEFINED__ */


#ifdef __cplusplus
extern "C"{
#endif 


/* interface __MIDL_itf_corsym_0000_0000 */
/* [local] */ 

#if 0
typedef typedef unsigned int UINT32;
;

typedef mdToken mdTypeDef;

typedef mdToken mdMethodDef;

typedef typedef ULONG_PTR SIZE_T;
;

#endif
#ifndef __CORHDR_H__
typedef mdToken mdSignature;

#endif
#pragma once
#pragma once
#pragma region Input Buffer SAL 1 compatibility macros
#pragma endregion Input Buffer SAL 1 compatibility macros
#pragma once
#pragma once
EXTERN_GUID(CorSym_LanguageType_C, 0x63a08714, 0xfc37, 0x11d2, 0x90, 0x4c, 0x0, 0xc0, 0x4f, 0xa3, 0x02, 0xa1);
EXTERN_GUID(CorSym_LanguageType_CPlusPlus, 0x3a12d0b7, 0xc26c, 0x11d0, 0xb4, 0x42, 0x0, 0xa0, 0x24, 0x4a, 0x1d, 0xd2);
EXTERN_GUID(CorSym_LanguageType_CSharp, 0x3f5162f8, 0x07c6, 0x11d3, 0x90, 0x53, 0x0, 0xc0, 0x4f, 0xa3, 0x02, 0xa1);
EXTERN_GUID(CorSym_LanguageType_Basic, 0x3a12d0b8, 0xc26c, 0x11d0, 0xb4, 0x42, 0x0, 0xa0, 0x24, 0x4a, 0x1d, 0xd2);
EXTERN_GUID(CorSym_LanguageType_Java, 0x3a12d0b4, 0xc26c, 0x11d0, 0xb4, 0x42, 0x0, 0xa0, 0x24, 0x4a, 0x1d, 0xd2);
EXTERN_GUID(CorSym_LanguageType_Cobol, 0xaf046cd1, 0xd0e1, 0x11d2, 0x97, 0x7c, 0x0, 0xa0, 0xc9, 0xb4, 0xd5, 0xc);
EXTERN_GUID(CorSym_LanguageType_Pascal, 0xaf046cd2, 0xd0e1, 0x11d2, 0x97, 0x7c, 0x0, 0xa0, 0xc9, 0xb4, 0xd5, 0xc);
EXTERN_GUID(CorSym_LanguageType_ILAssembly, 0xaf046cd3, 0xd0e1, 0x11d2, 0x97, 0x7c, 0x0, 0xa0, 0xc9, 0xb4, 0xd5, 0xc);
EXTERN_GUID(CorSym_LanguageType_JScript, 0x3a12d0b6, 0xc26c, 0x11d0, 0xb4, 0x42, 0x00, 0xa0, 0x24, 0x4a, 0x1d, 0xd2);
EXTERN_GUID(CorSym_LanguageType_SMC, 0xd9b9f7b, 0x6611, 0x11d3, 0xbd, 0x2a, 0x0, 0x0, 0xf8, 0x8, 0x49, 0xbd);
EXTERN_GUID(CorSym_LanguageType_MCPlusPlus, 0x4b35fde8, 0x07c6, 0x11d3, 0x90, 0x53, 0x0, 0xc0, 0x4f, 0xa3, 0x02, 0xa1);
EXTERN_GUID(CorSym_LanguageVendor_Microsoft, 0x994b45c4, 0xe6e9, 0x11d2, 0x90, 0x3f, 0x00, 0xc0, 0x4f, 0xa3, 0x02, 0xa1);
EXTERN_GUID(CorSym_DocumentType_Text, 0x5a869d0b, 0x6611, 0x11d3, 0xbd, 0x2a, 0x0, 0x0, 0xf8, 0x8, 0x49, 0xbd);
EXTERN_GUID(CorSym_DocumentType_MC, 0xeb40cb65, 0x3c1f, 0x4352, 0x9d, 0x7b, 0xba, 0xf, 0xc4, 0x7a, 0x9d, 0x77);
EXTERN_GUID(CorSym_SourceHash_MD5,  0x406ea660, 0x64cf, 0x4c82, 0xb6, 0xf0, 0x42, 0xd4, 0x81, 0x72, 0xa7, 0x99);
EXTERN_GUID(CorSym_SourceHash_SHA1, 0xff1816ec, 0xaa5e, 0x4d10, 0x87, 0xf7, 0x6f, 0x49, 0x63, 0x83, 0x34, 0x60);












typedef 
enum CorSymAddrKind
    {
        ADDR_IL_OFFSET	= 1,
        ADDR_NATIVE_RVA	= 2,
        ADDR_NATIVE_REGISTER	= 3,
        ADDR_NATIVE_REGREL	= 4,
        ADDR_NATIVE_OFFSET	= 5,
        ADDR_NATIVE_REGREG	= 6,
        ADDR_NATIVE_REGSTK	= 7,
        ADDR_NATIVE_STKREG	= 8,
        ADDR_BITFIELD	= 9,
        ADDR_NATIVE_ISECTOFFSET	= 10
    } 	CorSymAddrKind;

typedef 
enum CorSymVarFlag
    {
        VAR_IS_COMP_GEN	= 1
    } 	CorSymVarFlag;



extern RPC_IF_HANDLE __MIDL_itf_corsym_0000_0000_v0_0_c_ifspec;
extern RPC_IF_HANDLE __MIDL_itf_corsym_0000_0000_v0_0_s_ifspec;


#ifndef __CorSymLib_LIBRARY_DEFINED__
#define __CorSymLib_LIBRARY_DEFINED__

/* library CorSymLib */
/* [helpstring][version][uuid] */ 


EXTERN_C const IID LIBID_CorSymLib;

EXTERN_C const CLSID CLSID_CorSymWriter_deprecated;

#ifdef __cplusplus

class DECLSPEC_UUID("108296C1-281E-11d3-BD22-0000F80849BD")
CorSymWriter_deprecated;
#endif

EXTERN_C const CLSID CLSID_CorSymReader_deprecated;

#ifdef __cplusplus

class DECLSPEC_UUID("108296C2-281E-11d3-BD22-0000F80849BD")
CorSymReader_deprecated;
#endif

EXTERN_C const CLSID CLSID_CorSymBinder_deprecated;

#ifdef __cplusplus

class DECLSPEC_UUID("AA544D41-28CB-11d3-BD22-0000F80849BD")
CorSymBinder_deprecated;
#endif

EXTERN_C const CLSID CLSID_CorSymWriter_SxS;

#ifdef __cplusplus

class DECLSPEC_UUID("0AE2DEB0-F901-478b-BB9F-881EE8066788")
CorSymWriter_SxS;
#endif

EXTERN_C const CLSID CLSID_CorSymReader_SxS;

#ifdef __cplusplus

class DECLSPEC_UUID("0A3976C5-4529-4ef8-B0B0-42EED37082CD")
CorSymReader_SxS;
#endif

EXTERN_C const CLSID CLSID_CorSymBinder_SxS;

#ifdef __cplusplus

class DECLSPEC_UUID("0A29FF9E-7F9C-4437-8B11-F424491E3931")
CorSymBinder_SxS;
#endif
#endif /* __CorSymLib_LIBRARY_DEFINED__ */

#ifndef __ISymUnmanagedBinder_INTERFACE_DEFINED__
#define __ISymUnmanagedBinder_INTERFACE_DEFINED__

/* interface ISymUnmanagedBinder */
/* [unique][uuid][object] */ 


EXTERN_C const IID IID_ISymUnmanagedBinder;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("AA544D42-28CB-11d3-BD22-0000F80849BD")
    ISymUnmanagedBinder : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE GetReaderForFile( 
            /* [in] */ __RPC__in_opt IUnknown *importer,
            /* [in] */ __RPC__in const WCHAR *fileName,
            /* [in] */ __RPC__in const WCHAR *searchPath,
            /* [retval][out] */ __RPC__deref_out_opt ISymUnmanagedReader **pRetVal) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetReaderFromStream( 
            /* [in] */ __RPC__in_opt IUnknown *importer,
            /* [in] */ __RPC__in_opt IStream *pstream,
            /* [retval][out] */ __RPC__deref_out_opt ISymUnmanagedReader **pRetVal) = 0;
        
    };
    
    
#else 	/* C style interface */

    typedef struct ISymUnmanagedBinderVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            __RPC__in ISymUnmanagedBinder * This,
            /* [in] */ __RPC__in REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            __RPC__in ISymUnmanagedBinder * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            __RPC__in ISymUnmanagedBinder * This);
        
        HRESULT ( STDMETHODCALLTYPE *GetReaderForFile )( 
            __RPC__in ISymUnmanagedBinder * This,
            /* [in] */ __RPC__in_opt IUnknown *importer,
            /* [in] */ __RPC__in const WCHAR *fileName,
            /* [in] */ __RPC__in const WCHAR *searchPath,
            /* [retval][out] */ __RPC__deref_out_opt ISymUnmanagedReader **pRetVal);
        
        HRESULT ( STDMETHODCALLTYPE *GetReaderFromStream )( 
            __RPC__in ISymUnmanagedBinder * This,
            /* [in] */ __RPC__in_opt IUnknown *importer,
            /* [in] */ __RPC__in_opt IStream *pstream,
            /* [retval][out] */ __RPC__deref_out_opt ISymUnmanagedReader **pRetVal);
        
        END_INTERFACE
    } ISymUnmanagedBinderVtbl;

    interface ISymUnmanagedBinder
    {
        CONST_VTBL struct ISymUnmanagedBinderVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define ISymUnmanagedBinder_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define ISymUnmanagedBinder_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define ISymUnmanagedBinder_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define ISymUnmanagedBinder_GetReaderForFile(This,importer,fileName,searchPath,pRetVal)	\
    ( (This)->lpVtbl -> GetReaderForFile(This,importer,fileName,searchPath,pRetVal) ) 

#define ISymUnmanagedBinder_GetReaderFromStream(This,importer,pstream,pRetVal)	\
    ( (This)->lpVtbl -> GetReaderFromStream(This,importer,pstream,pRetVal) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ISymUnmanagedBinder_INTERFACE_DEFINED__ */


/* interface __MIDL_itf_corsym_0000_0002 */
/* [local] */ 

typedef 
enum CorSymSearchPolicyAttributes
    {
        AllowRegistryAccess	= 0x1,
        AllowSymbolServerAccess	= 0x2,
        AllowOriginalPathAccess	= 0x4,
        AllowReferencePathAccess	= 0x8
    } 	CorSymSearchPolicyAttributes;



extern RPC_IF_HANDLE __MIDL_itf_corsym_0000_0002_v0_0_c_ifspec;
extern RPC_IF_HANDLE __MIDL_itf_corsym_0000_0002_v0_0_s_ifspec;

#ifndef __ISymUnmanagedBinder2_INTERFACE_DEFINED__
#define __ISymUnmanagedBinder2_INTERFACE_DEFINED__

/* interface ISymUnmanagedBinder2 */
/* [unique][uuid][object] */ 


EXTERN_C const IID IID_ISymUnmanagedBinder2;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("ACCEE350-89AF-4ccb-8B40-1C2C4C6F9434")
    ISymUnmanagedBinder2 : public ISymUnmanagedBinder
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE GetReaderForFile2( 
            /* [in] */ __RPC__in_opt IUnknown *importer,
            /* [in] */ __RPC__in const WCHAR *fileName,
            /* [in] */ __RPC__in const WCHAR *searchPath,
            /* [in] */ ULONG32 searchPolicy,
            /* [retval][out] */ __RPC__deref_out_opt ISymUnmanagedReader **pRetVal) = 0;
        
    };
    
    
#else 	/* C style interface */

    typedef struct ISymUnmanagedBinder2Vtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            __RPC__in ISymUnmanagedBinder2 * This,
            /* [in] */ __RPC__in REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            __RPC__in ISymUnmanagedBinder2 * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            __RPC__in ISymUnmanagedBinder2 * This);
        
        HRESULT ( STDMETHODCALLTYPE *GetReaderForFile )( 
            __RPC__in ISymUnmanagedBinder2 * This,
            /* [in] */ __RPC__in_opt IUnknown *importer,
            /* [in] */ __RPC__in const WCHAR *fileName,
            /* [in] */ __RPC__in const WCHAR *searchPath,
            /* [retval][out] */ __RPC__deref_out_opt ISymUnmanagedReader **pRetVal);
        
        HRESULT ( STDMETHODCALLTYPE *GetReaderFromStream )( 
            __RPC__in ISymUnmanagedBinder2 * This,
            /* [in] */ __RPC__in_opt IUnknown *importer,
            /* [in] */ __RPC__in_opt IStream *pstream,
            /* [retval][out] */ __RPC__deref_out_opt ISymUnmanagedReader **pRetVal);
        
        HRESULT ( STDMETHODCALLTYPE *GetReaderForFile2 )( 
            __RPC__in ISymUnmanagedBinder2 * This,
            /* [in] */ __RPC__in_opt IUnknown *importer,
            /* [in] */ __RPC__in const WCHAR *fileName,
            /* [in] */ __RPC__in const WCHAR *searchPath,
            /* [in] */ ULONG32 searchPolicy,
            /* [retval][out] */ __RPC__deref_out_opt ISymUnmanagedReader **pRetVal);
        
        END_INTERFACE
    } ISymUnmanagedBinder2Vtbl;

    interface ISymUnmanagedBinder2
    {
        CONST_VTBL struct ISymUnmanagedBinder2Vtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define ISymUnmanagedBinder2_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define ISymUnmanagedBinder2_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define ISymUnmanagedBinder2_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define ISymUnmanagedBinder2_GetReaderForFile(This,importer,fileName,searchPath,pRetVal)	\
    ( (This)->lpVtbl -> GetReaderForFile(This,importer,fileName,searchPath,pRetVal) ) 

#define ISymUnmanagedBinder2_GetReaderFromStream(This,importer,pstream,pRetVal)	\
    ( (This)->lpVtbl -> GetReaderFromStream(This,importer,pstream,pRetVal) ) 


#define ISymUnmanagedBinder2_GetReaderForFile2(This,importer,fileName,searchPath,searchPolicy,pRetVal)	\
    ( (This)->lpVtbl -> GetReaderForFile2(This,importer,fileName,searchPath,searchPolicy,pRetVal) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ISymUnmanagedBinder2_INTERFACE_DEFINED__ */


#ifndef __ISymUnmanagedBinder3_INTERFACE_DEFINED__
#define __ISymUnmanagedBinder3_INTERFACE_DEFINED__

/* interface ISymUnmanagedBinder3 */
/* [unique][uuid][object] */ 


EXTERN_C const IID IID_ISymUnmanagedBinder3;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("28AD3D43-B601-4d26-8A1B-25F9165AF9D7")
    ISymUnmanagedBinder3 : public ISymUnmanagedBinder2
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE GetReaderFromCallback( 
            /* [in] */ __RPC__in_opt IUnknown *importer,
            /* [in] */ __RPC__in const WCHAR *fileName,
            /* [in] */ __RPC__in const WCHAR *searchPath,
            /* [in] */ ULONG32 searchPolicy,
            /* [in] */ __RPC__in_opt IUnknown *callback,
            /* [retval][out] */ __RPC__deref_out_opt ISymUnmanagedReader **pRetVal) = 0;
        
    };
    
    
#else 	/* C style interface */

    typedef struct ISymUnmanagedBinder3Vtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            __RPC__in ISymUnmanagedBinder3 * This,
            /* [in] */ __RPC__in REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            __RPC__in ISymUnmanagedBinder3 * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            __RPC__in ISymUnmanagedBinder3 * This);
        
        HRESULT ( STDMETHODCALLTYPE *GetReaderForFile )( 
            __RPC__in ISymUnmanagedBinder3 * This,
            /* [in] */ __RPC__in_opt IUnknown *importer,
            /* [in] */ __RPC__in const WCHAR *fileName,
            /* [in] */ __RPC__in const WCHAR *searchPath,
            /* [retval][out] */ __RPC__deref_out_opt ISymUnmanagedReader **pRetVal);
        
        HRESULT ( STDMETHODCALLTYPE *GetReaderFromStream )( 
            __RPC__in ISymUnmanagedBinder3 * This,
            /* [in] */ __RPC__in_opt IUnknown *importer,
            /* [in] */ __RPC__in_opt IStream *pstream,
            /* [retval][out] */ __RPC__deref_out_opt ISymUnmanagedReader **pRetVal);
        
        HRESULT ( STDMETHODCALLTYPE *GetReaderForFile2 )( 
            __RPC__in ISymUnmanagedBinder3 * This,
            /* [in] */ __RPC__in_opt IUnknown *importer,
            /* [in] */ __RPC__in const WCHAR *fileName,
            /* [in] */ __RPC__in const WCHAR *searchPath,
            /* [in] */ ULONG32 searchPolicy,
            /* [retval][out] */ __RPC__deref_out_opt ISymUnmanagedReader **pRetVal);
        
        HRESULT ( STDMETHODCALLTYPE *GetReaderFromCallback )( 
            __RPC__in ISymUnmanagedBinder3 * This,
            /* [in] */ __RPC__in_opt IUnknown *importer,
            /* [in] */ __RPC__in const WCHAR *fileName,
            /* [in] */ __RPC__in const WCHAR *searchPath,
            /* [in] */ ULONG32 searchPolicy,
            /* [in] */ __RPC__in_opt IUnknown *callback,
            /* [retval][out] */ __RPC__deref_out_opt ISymUnmanagedReader **pRetVal);
        
        END_INTERFACE
    } ISymUnmanagedBinder3Vtbl;

    interface ISymUnmanagedBinder3
    {
        CONST_VTBL struct ISymUnmanagedBinder3Vtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define ISymUnmanagedBinder3_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define ISymUnmanagedBinder3_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define ISymUnmanagedBinder3_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define ISymUnmanagedBinder3_GetReaderForFile(This,importer,fileName,searchPath,pRetVal)	\
    ( (This)->lpVtbl -> GetReaderForFile(This,importer,fileName,searchPath,pRetVal) ) 

#define ISymUnmanagedBinder3_GetReaderFromStream(This,importer,pstream,pRetVal)	\
    ( (This)->lpVtbl -> GetReaderFromStream(This,importer,pstream,pRetVal) ) 


#define ISymUnmanagedBinder3_GetReaderForFile2(This,importer,fileName,searchPath,searchPolicy,pRetVal)	\
    ( (This)->lpVtbl -> GetReaderForFile2(This,importer,fileName,searchPath,searchPolicy,pRetVal) ) 


#define ISymUnmanagedBinder3_GetReaderFromCallback(This,importer,fileName,searchPath,searchPolicy,callback,pRetVal)	\
    ( (This)->lpVtbl -> GetReaderFromCallback(This,importer,fileName,searchPath,searchPolicy,callback,pRetVal) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ISymUnmanagedBinder3_INTERFACE_DEFINED__ */


/* interface __MIDL_itf_corsym_0000_0004 */
/* [local] */ 

static const int E_SYM_DESTROYED = MAKE_HRESULT(1, FACILITY_ITF, 0xdead);


extern RPC_IF_HANDLE __MIDL_itf_corsym_0000_0004_v0_0_c_ifspec;
extern RPC_IF_HANDLE __MIDL_itf_corsym_0000_0004_v0_0_s_ifspec;

#ifndef __ISymUnmanagedDispose_INTERFACE_DEFINED__
#define __ISymUnmanagedDispose_INTERFACE_DEFINED__

/* interface ISymUnmanagedDispose */
/* [unique][uuid][object] */ 


EXTERN_C const IID IID_ISymUnmanagedDispose;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("969708D2-05E5-4861-A3B0-96E473CDF63F")
    ISymUnmanagedDispose : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE Destroy( void) = 0;
        
    };
    
    
#else 	/* C style interface */

    typedef struct ISymUnmanagedDisposeVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            __RPC__in ISymUnmanagedDispose * This,
            /* [in] */ __RPC__in REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            __RPC__in ISymUnmanagedDispose * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            __RPC__in ISymUnmanagedDispose * This);
        
        HRESULT ( STDMETHODCALLTYPE *Destroy )( 
            __RPC__in ISymUnmanagedDispose * This);
        
        END_INTERFACE
    } ISymUnmanagedDisposeVtbl;

    interface ISymUnmanagedDispose
    {
        CONST_VTBL struct ISymUnmanagedDisposeVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define ISymUnmanagedDispose_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define ISymUnmanagedDispose_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define ISymUnmanagedDispose_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define ISymUnmanagedDispose_Destroy(This)	\
    ( (This)->lpVtbl -> Destroy(This) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ISymUnmanagedDispose_INTERFACE_DEFINED__ */


#ifndef __ISymUnmanagedDocument_INTERFACE_DEFINED__
#define __ISymUnmanagedDocument_INTERFACE_DEFINED__

/* interface ISymUnmanagedDocument */
/* [unique][uuid][object] */ 


EXTERN_C const IID IID_ISymUnmanagedDocument;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("40DE4037-7C81-3E1E-B022-AE1ABFF2CA08")
    ISymUnmanagedDocument : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE GetURL( 
            /* [in] */ ULONG32 cchUrl,
            /* [out] */ __RPC__out ULONG32 *pcchUrl,
            /* [length_is][size_is][out] */ __RPC__out_ecount_part(cchUrl, *pcchUrl) WCHAR szUrl[  ]) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetDocumentType( 
            /* [retval][out] */ __RPC__out GUID *pRetVal) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetLanguage( 
            /* [retval][out] */ __RPC__out GUID *pRetVal) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetLanguageVendor( 
            /* [retval][out] */ __RPC__out GUID *pRetVal) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetCheckSumAlgorithmId( 
            /* [retval][out] */ __RPC__out GUID *pRetVal) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetCheckSum( 
            /* [in] */ ULONG32 cData,
            /* [out] */ __RPC__out ULONG32 *pcData,
            /* [length_is][size_is][out] */ __RPC__out_ecount_part(cData, *pcData) BYTE data[  ]) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE FindClosestLine( 
            /* [in] */ ULONG32 line,
            /* [retval][out] */ __RPC__out ULONG32 *pRetVal) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE HasEmbeddedSource( 
            /* [retval][out] */ __RPC__out BOOL *pRetVal) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetSourceLength( 
            /* [retval][out] */ __RPC__out ULONG32 *pRetVal) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetSourceRange( 
            /* [in] */ ULONG32 startLine,
            /* [in] */ ULONG32 startColumn,
            /* [in] */ ULONG32 endLine,
            /* [in] */ ULONG32 endColumn,
            /* [in] */ ULONG32 cSourceBytes,
            /* [out] */ __RPC__out ULONG32 *pcSourceBytes,
            /* [length_is][size_is][out] */ __RPC__out_ecount_part(cSourceBytes, *pcSourceBytes) BYTE source[  ]) = 0;
        
    };
    
    
#else 	/* C style interface */

    typedef struct ISymUnmanagedDocumentVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            __RPC__in ISymUnmanagedDocument * This,
            /* [in] */ __RPC__in REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            __RPC__in ISymUnmanagedDocument * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            __RPC__in ISymUnmanagedDocument * This);
        
        HRESULT ( STDMETHODCALLTYPE *GetURL )( 
            __RPC__in ISymUnmanagedDocument * This,
            /* [in] */ ULONG32 cchUrl,
            /* [out] */ __RPC__out ULONG32 *pcchUrl,
            /* [length_is][size_is][out] */ __RPC__out_ecount_part(cchUrl, *pcchUrl) WCHAR szUrl[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *GetDocumentType )( 
            __RPC__in ISymUnmanagedDocument * This,
            /* [retval][out] */ __RPC__out GUID *pRetVal);
        
        HRESULT ( STDMETHODCALLTYPE *GetLanguage )( 
            __RPC__in ISymUnmanagedDocument * This,
            /* [retval][out] */ __RPC__out GUID *pRetVal);
        
        HRESULT ( STDMETHODCALLTYPE *GetLanguageVendor )( 
            __RPC__in ISymUnmanagedDocument * This,
            /* [retval][out] */ __RPC__out GUID *pRetVal);
        
        HRESULT ( STDMETHODCALLTYPE *GetCheckSumAlgorithmId )( 
            __RPC__in ISymUnmanagedDocument * This,
            /* [retval][out] */ __RPC__out GUID *pRetVal);
        
        HRESULT ( STDMETHODCALLTYPE *GetCheckSum )( 
            __RPC__in ISymUnmanagedDocument * This,
            /* [in] */ ULONG32 cData,
            /* [out] */ __RPC__out ULONG32 *pcData,
            /* [length_is][size_is][out] */ __RPC__out_ecount_part(cData, *pcData) BYTE data[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *FindClosestLine )( 
            __RPC__in ISymUnmanagedDocument * This,
            /* [in] */ ULONG32 line,
            /* [retval][out] */ __RPC__out ULONG32 *pRetVal);
        
        HRESULT ( STDMETHODCALLTYPE *HasEmbeddedSource )( 
            __RPC__in ISymUnmanagedDocument * This,
            /* [retval][out] */ __RPC__out BOOL *pRetVal);
        
        HRESULT ( STDMETHODCALLTYPE *GetSourceLength )( 
            __RPC__in ISymUnmanagedDocument * This,
            /* [retval][out] */ __RPC__out ULONG32 *pRetVal);
        
        HRESULT ( STDMETHODCALLTYPE *GetSourceRange )( 
            __RPC__in ISymUnmanagedDocument * This,
            /* [in] */ ULONG32 startLine,
            /* [in] */ ULONG32 startColumn,
            /* [in] */ ULONG32 endLine,
            /* [in] */ ULONG32 endColumn,
            /* [in] */ ULONG32 cSourceBytes,
            /* [out] */ __RPC__out ULONG32 *pcSourceBytes,
            /* [length_is][size_is][out] */ __RPC__out_ecount_part(cSourceBytes, *pcSourceBytes) BYTE source[  ]);
        
        END_INTERFACE
    } ISymUnmanagedDocumentVtbl;

    interface ISymUnmanagedDocument
    {
        CONST_VTBL struct ISymUnmanagedDocumentVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define ISymUnmanagedDocument_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define ISymUnmanagedDocument_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define ISymUnmanagedDocument_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define ISymUnmanagedDocument_GetURL(This,cchUrl,pcchUrl,szUrl)	\
    ( (This)->lpVtbl -> GetURL(This,cchUrl,pcchUrl,szUrl) ) 

#define ISymUnmanagedDocument_GetDocumentType(This,pRetVal)	\
    ( (This)->lpVtbl -> GetDocumentType(This,pRetVal) ) 

#define ISymUnmanagedDocument_GetLanguage(This,pRetVal)	\
    ( (This)->lpVtbl -> GetLanguage(This,pRetVal) ) 

#define ISymUnmanagedDocument_GetLanguageVendor(This,pRetVal)	\
    ( (This)->lpVtbl -> GetLanguageVendor(This,pRetVal) ) 

#define ISymUnmanagedDocument_GetCheckSumAlgorithmId(This,pRetVal)	\
    ( (This)->lpVtbl -> GetCheckSumAlgorithmId(This,pRetVal) ) 

#define ISymUnmanagedDocument_GetCheckSum(This,cData,pcData,data)	\
    ( (This)->lpVtbl -> GetCheckSum(This,cData,pcData,data) ) 

#define ISymUnmanagedDocument_FindClosestLine(This,line,pRetVal)	\
    ( (This)->lpVtbl -> FindClosestLine(This,line,pRetVal) ) 

#define ISymUnmanagedDocument_HasEmbeddedSource(This,pRetVal)	\
    ( (This)->lpVtbl -> HasEmbeddedSource(This,pRetVal) ) 

#define ISymUnmanagedDocument_GetSourceLength(This,pRetVal)	\
    ( (This)->lpVtbl -> GetSourceLength(This,pRetVal) ) 

#define ISymUnmanagedDocument_GetSourceRange(This,startLine,startColumn,endLine,endColumn,cSourceBytes,pcSourceBytes,source)	\
    ( (This)->lpVtbl -> GetSourceRange(This,startLine,startColumn,endLine,endColumn,cSourceBytes,pcSourceBytes,source) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ISymUnmanagedDocument_INTERFACE_DEFINED__ */


#ifndef __ISymUnmanagedDocumentWriter_INTERFACE_DEFINED__
#define __ISymUnmanagedDocumentWriter_INTERFACE_DEFINED__

/* interface ISymUnmanagedDocumentWriter */
/* [unique][uuid][object] */ 


EXTERN_C const IID IID_ISymUnmanagedDocumentWriter;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("B01FAFEB-C450-3A4D-BEEC-B4CEEC01E006")
    ISymUnmanagedDocumentWriter : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE SetSource( 
            /* [in] */ ULONG32 sourceSize,
            /* [size_is][in] */ __RPC__in_ecount_full(sourceSize) BYTE source[  ]) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE SetCheckSum( 
            /* [in] */ GUID algorithmId,
            /* [in] */ ULONG32 checkSumSize,
            /* [size_is][in] */ __RPC__in_ecount_full(checkSumSize) BYTE checkSum[  ]) = 0;
        
    };
    
    
#else 	/* C style interface */

    typedef struct ISymUnmanagedDocumentWriterVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            __RPC__in ISymUnmanagedDocumentWriter * This,
            /* [in] */ __RPC__in REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            __RPC__in ISymUnmanagedDocumentWriter * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            __RPC__in ISymUnmanagedDocumentWriter * This);
        
        HRESULT ( STDMETHODCALLTYPE *SetSource )( 
            __RPC__in ISymUnmanagedDocumentWriter * This,
            /* [in] */ ULONG32 sourceSize,
            /* [size_is][in] */ __RPC__in_ecount_full(sourceSize) BYTE source[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *SetCheckSum )( 
            __RPC__in ISymUnmanagedDocumentWriter * This,
            /* [in] */ GUID algorithmId,
            /* [in] */ ULONG32 checkSumSize,
            /* [size_is][in] */ __RPC__in_ecount_full(checkSumSize) BYTE checkSum[  ]);
        
        END_INTERFACE
    } ISymUnmanagedDocumentWriterVtbl;

    interface ISymUnmanagedDocumentWriter
    {
        CONST_VTBL struct ISymUnmanagedDocumentWriterVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define ISymUnmanagedDocumentWriter_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define ISymUnmanagedDocumentWriter_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define ISymUnmanagedDocumentWriter_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define ISymUnmanagedDocumentWriter_SetSource(This,sourceSize,source)	\
    ( (This)->lpVtbl -> SetSource(This,sourceSize,source) ) 

#define ISymUnmanagedDocumentWriter_SetCheckSum(This,algorithmId,checkSumSize,checkSum)	\
    ( (This)->lpVtbl -> SetCheckSum(This,algorithmId,checkSumSize,checkSum) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ISymUnmanagedDocumentWriter_INTERFACE_DEFINED__ */


#ifndef __ISymUnmanagedMethod_INTERFACE_DEFINED__
#define __ISymUnmanagedMethod_INTERFACE_DEFINED__

/* interface ISymUnmanagedMethod */
/* [unique][uuid][object] */ 


EXTERN_C const IID IID_ISymUnmanagedMethod;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("B62B923C-B500-3158-A543-24F307A8B7E1")
    ISymUnmanagedMethod : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE GetToken( 
            /* [retval][out] */ __RPC__out mdMethodDef *pToken) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetSequencePointCount( 
            /* [retval][out] */ __RPC__out ULONG32 *pRetVal) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetRootScope( 
            /* [retval][out] */ __RPC__deref_out_opt ISymUnmanagedScope **pRetVal) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetScopeFromOffset( 
            /* [in] */ ULONG32 offset,
            /* [retval][out] */ __RPC__deref_out_opt ISymUnmanagedScope **pRetVal) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetOffset( 
            /* [in] */ __RPC__in_opt ISymUnmanagedDocument *document,
            /* [in] */ ULONG32 line,
            /* [in] */ ULONG32 column,
            /* [retval][out] */ __RPC__out ULONG32 *pRetVal) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetRanges( 
            /* [in] */ __RPC__in_opt ISymUnmanagedDocument *document,
            /* [in] */ ULONG32 line,
            /* [in] */ ULONG32 column,
            /* [in] */ ULONG32 cRanges,
            /* [out] */ __RPC__out ULONG32 *pcRanges,
            /* [length_is][size_is][out] */ __RPC__out_ecount_part(cRanges, *pcRanges) ULONG32 ranges[  ]) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetParameters( 
            /* [in] */ ULONG32 cParams,
            /* [out] */ __RPC__out ULONG32 *pcParams,
            /* [length_is][size_is][out] */ __RPC__out_ecount_part(cParams, *pcParams) ISymUnmanagedVariable *params[  ]) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetNamespace( 
            /* [out] */ __RPC__deref_out_opt ISymUnmanagedNamespace **pRetVal) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetSourceStartEnd( 
            /* [in] */ __RPC__in_ecount_full(2) ISymUnmanagedDocument *docs[ 2 ],
            /* [in] */ __RPC__in_ecount_full(2) ULONG32 lines[ 2 ],
            /* [in] */ __RPC__in_ecount_full(2) ULONG32 columns[ 2 ],
            /* [out] */ __RPC__out BOOL *pRetVal) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetSequencePoints( 
            /* [in] */ ULONG32 cPoints,
            /* [out] */ __RPC__out ULONG32 *pcPoints,
            /* [size_is][in] */ __RPC__in_ecount_full(cPoints) ULONG32 offsets[  ],
            /* [size_is][in] */ __RPC__in_ecount_full(cPoints) ISymUnmanagedDocument *documents[  ],
            /* [size_is][in] */ __RPC__in_ecount_full(cPoints) ULONG32 lines[  ],
            /* [size_is][in] */ __RPC__in_ecount_full(cPoints) ULONG32 columns[  ],
            /* [size_is][in] */ __RPC__in_ecount_full(cPoints) ULONG32 endLines[  ],
            /* [size_is][in] */ __RPC__in_ecount_full(cPoints) ULONG32 endColumns[  ]) = 0;
        
    };
    
    
#else 	/* C style interface */

    typedef struct ISymUnmanagedMethodVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            __RPC__in ISymUnmanagedMethod * This,
            /* [in] */ __RPC__in REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            __RPC__in ISymUnmanagedMethod * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            __RPC__in ISymUnmanagedMethod * This);
        
        HRESULT ( STDMETHODCALLTYPE *GetToken )( 
            __RPC__in ISymUnmanagedMethod * This,
            /* [retval][out] */ __RPC__out mdMethodDef *pToken);
        
        HRESULT ( STDMETHODCALLTYPE *GetSequencePointCount )( 
            __RPC__in ISymUnmanagedMethod * This,
            /* [retval][out] */ __RPC__out ULONG32 *pRetVal);
        
        HRESULT ( STDMETHODCALLTYPE *GetRootScope )( 
            __RPC__in ISymUnmanagedMethod * This,
            /* [retval][out] */ __RPC__deref_out_opt ISymUnmanagedScope **pRetVal);
        
        HRESULT ( STDMETHODCALLTYPE *GetScopeFromOffset )( 
            __RPC__in ISymUnmanagedMethod * This,
            /* [in] */ ULONG32 offset,
            /* [retval][out] */ __RPC__deref_out_opt ISymUnmanagedScope **pRetVal);
        
        HRESULT ( STDMETHODCALLTYPE *GetOffset )( 
            __RPC__in ISymUnmanagedMethod * This,
            /* [in] */ __RPC__in_opt ISymUnmanagedDocument *document,
            /* [in] */ ULONG32 line,
            /* [in] */ ULONG32 column,
            /* [retval][out] */ __RPC__out ULONG32 *pRetVal);
        
        HRESULT ( STDMETHODCALLTYPE *GetRanges )( 
            __RPC__in ISymUnmanagedMethod * This,
            /* [in] */ __RPC__in_opt ISymUnmanagedDocument *document,
            /* [in] */ ULONG32 line,
            /* [in] */ ULONG32 column,
            /* [in] */ ULONG32 cRanges,
            /* [out] */ __RPC__out ULONG32 *pcRanges,
            /* [length_is][size_is][out] */ __RPC__out_ecount_part(cRanges, *pcRanges) ULONG32 ranges[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *GetParameters )( 
            __RPC__in ISymUnmanagedMethod * This,
            /* [in] */ ULONG32 cParams,
            /* [out] */ __RPC__out ULONG32 *pcParams,
            /* [length_is][size_is][out] */ __RPC__out_ecount_part(cParams, *pcParams) ISymUnmanagedVariable *params[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *GetNamespace )( 
            __RPC__in ISymUnmanagedMethod * This,
            /* [out] */ __RPC__deref_out_opt ISymUnmanagedNamespace **pRetVal);
        
        HRESULT ( STDMETHODCALLTYPE *GetSourceStartEnd )( 
            __RPC__in ISymUnmanagedMethod * This,
            /* [in] */ __RPC__in_ecount_full(2) ISymUnmanagedDocument *docs[ 2 ],
            /* [in] */ __RPC__in_ecount_full(2) ULONG32 lines[ 2 ],
            /* [in] */ __RPC__in_ecount_full(2) ULONG32 columns[ 2 ],
            /* [out] */ __RPC__out BOOL *pRetVal);
        
        HRESULT ( STDMETHODCALLTYPE *GetSequencePoints )( 
            __RPC__in ISymUnmanagedMethod * This,
            /* [in] */ ULONG32 cPoints,
            /* [out] */ __RPC__out ULONG32 *pcPoints,
            /* [size_is][in] */ __RPC__in_ecount_full(cPoints) ULONG32 offsets[  ],
            /* [size_is][in] */ __RPC__in_ecount_full(cPoints) ISymUnmanagedDocument *documents[  ],
            /* [size_is][in] */ __RPC__in_ecount_full(cPoints) ULONG32 lines[  ],
            /* [size_is][in] */ __RPC__in_ecount_full(cPoints) ULONG32 columns[  ],
            /* [size_is][in] */ __RPC__in_ecount_full(cPoints) ULONG32 endLines[  ],
            /* [size_is][in] */ __RPC__in_ecount_full(cPoints) ULONG32 endColumns[  ]);
        
        END_INTERFACE
    } ISymUnmanagedMethodVtbl;

    interface ISymUnmanagedMethod
    {
        CONST_VTBL struct ISymUnmanagedMethodVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define ISymUnmanagedMethod_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define ISymUnmanagedMethod_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define ISymUnmanagedMethod_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define ISymUnmanagedMethod_GetToken(This,pToken)	\
    ( (This)->lpVtbl -> GetToken(This,pToken) ) 

#define ISymUnmanagedMethod_GetSequencePointCount(This,pRetVal)	\
    ( (This)->lpVtbl -> GetSequencePointCount(This,pRetVal) ) 

#define ISymUnmanagedMethod_GetRootScope(This,pRetVal)	\
    ( (This)->lpVtbl -> GetRootScope(This,pRetVal) ) 

#define ISymUnmanagedMethod_GetScopeFromOffset(This,offset,pRetVal)	\
    ( (This)->lpVtbl -> GetScopeFromOffset(This,offset,pRetVal) ) 

#define ISymUnmanagedMethod_GetOffset(This,document,line,column,pRetVal)	\
    ( (This)->lpVtbl -> GetOffset(This,document,line,column,pRetVal) ) 

#define ISymUnmanagedMethod_GetRanges(This,document,line,column,cRanges,pcRanges,ranges)	\
    ( (This)->lpVtbl -> GetRanges(This,document,line,column,cRanges,pcRanges,ranges) ) 

#define ISymUnmanagedMethod_GetParameters(This,cParams,pcParams,params)	\
    ( (This)->lpVtbl -> GetParameters(This,cParams,pcParams,params) ) 

#define ISymUnmanagedMethod_GetNamespace(This,pRetVal)	\
    ( (This)->lpVtbl -> GetNamespace(This,pRetVal) ) 

#define ISymUnmanagedMethod_GetSourceStartEnd(This,docs,lines,columns,pRetVal)	\
    ( (This)->lpVtbl -> GetSourceStartEnd(This,docs,lines,columns,pRetVal) ) 

#define ISymUnmanagedMethod_GetSequencePoints(This,cPoints,pcPoints,offsets,documents,lines,columns,endLines,endColumns)	\
    ( (This)->lpVtbl -> GetSequencePoints(This,cPoints,pcPoints,offsets,documents,lines,columns,endLines,endColumns) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ISymUnmanagedMethod_INTERFACE_DEFINED__ */


#ifndef __ISymENCUnmanagedMethod_INTERFACE_DEFINED__
#define __ISymENCUnmanagedMethod_INTERFACE_DEFINED__

/* interface ISymENCUnmanagedMethod */
/* [unique][uuid][object] */ 


EXTERN_C const IID IID_ISymENCUnmanagedMethod;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("85E891DA-A631-4c76-ACA2-A44A39C46B8C")
    ISymENCUnmanagedMethod : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE GetFileNameFromOffset( 
            /* [in] */ ULONG32 dwOffset,
            /* [in] */ ULONG32 cchName,
            /* [out] */ __RPC__out ULONG32 *pcchName,
            /* [length_is][size_is][out] */ __RPC__out_ecount_part(cchName, *pcchName) WCHAR szName[  ]) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetLineFromOffset( 
            /* [in] */ ULONG32 dwOffset,
            /* [out] */ __RPC__out ULONG32 *pline,
            /* [out] */ __RPC__out ULONG32 *pcolumn,
            /* [out] */ __RPC__out ULONG32 *pendLine,
            /* [out] */ __RPC__out ULONG32 *pendColumn,
            /* [out] */ __RPC__out ULONG32 *pdwStartOffset) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetDocumentsForMethodCount( 
            /* [retval][out] */ __RPC__out ULONG32 *pRetVal) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetDocumentsForMethod( 
            /* [in] */ ULONG32 cDocs,
            /* [out] */ __RPC__out ULONG32 *pcDocs,
            /* [size_is][in] */ __RPC__in_ecount_full(cDocs) ISymUnmanagedDocument *documents[  ]) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetSourceExtentInDocument( 
            /* [in] */ __RPC__in_opt ISymUnmanagedDocument *document,
            /* [out] */ __RPC__out ULONG32 *pstartLine,
            /* [out] */ __RPC__out ULONG32 *pendLine) = 0;
        
    };
    
    
#else 	/* C style interface */

    typedef struct ISymENCUnmanagedMethodVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            __RPC__in ISymENCUnmanagedMethod * This,
            /* [in] */ __RPC__in REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            __RPC__in ISymENCUnmanagedMethod * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            __RPC__in ISymENCUnmanagedMethod * This);
        
        HRESULT ( STDMETHODCALLTYPE *GetFileNameFromOffset )( 
            __RPC__in ISymENCUnmanagedMethod * This,
            /* [in] */ ULONG32 dwOffset,
            /* [in] */ ULONG32 cchName,
            /* [out] */ __RPC__out ULONG32 *pcchName,
            /* [length_is][size_is][out] */ __RPC__out_ecount_part(cchName, *pcchName) WCHAR szName[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *GetLineFromOffset )( 
            __RPC__in ISymENCUnmanagedMethod * This,
            /* [in] */ ULONG32 dwOffset,
            /* [out] */ __RPC__out ULONG32 *pline,
            /* [out] */ __RPC__out ULONG32 *pcolumn,
            /* [out] */ __RPC__out ULONG32 *pendLine,
            /* [out] */ __RPC__out ULONG32 *pendColumn,
            /* [out] */ __RPC__out ULONG32 *pdwStartOffset);
        
        HRESULT ( STDMETHODCALLTYPE *GetDocumentsForMethodCount )( 
            __RPC__in ISymENCUnmanagedMethod * This,
            /* [retval][out] */ __RPC__out ULONG32 *pRetVal);
        
        HRESULT ( STDMETHODCALLTYPE *GetDocumentsForMethod )( 
            __RPC__in ISymENCUnmanagedMethod * This,
            /* [in] */ ULONG32 cDocs,
            /* [out] */ __RPC__out ULONG32 *pcDocs,
            /* [size_is][in] */ __RPC__in_ecount_full(cDocs) ISymUnmanagedDocument *documents[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *GetSourceExtentInDocument )( 
            __RPC__in ISymENCUnmanagedMethod * This,
            /* [in] */ __RPC__in_opt ISymUnmanagedDocument *document,
            /* [out] */ __RPC__out ULONG32 *pstartLine,
            /* [out] */ __RPC__out ULONG32 *pendLine);
        
        END_INTERFACE
    } ISymENCUnmanagedMethodVtbl;

    interface ISymENCUnmanagedMethod
    {
        CONST_VTBL struct ISymENCUnmanagedMethodVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define ISymENCUnmanagedMethod_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define ISymENCUnmanagedMethod_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define ISymENCUnmanagedMethod_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define ISymENCUnmanagedMethod_GetFileNameFromOffset(This,dwOffset,cchName,pcchName,szName)	\
    ( (This)->lpVtbl -> GetFileNameFromOffset(This,dwOffset,cchName,pcchName,szName) ) 

#define ISymENCUnmanagedMethod_GetLineFromOffset(This,dwOffset,pline,pcolumn,pendLine,pendColumn,pdwStartOffset)	\
    ( (This)->lpVtbl -> GetLineFromOffset(This,dwOffset,pline,pcolumn,pendLine,pendColumn,pdwStartOffset) ) 

#define ISymENCUnmanagedMethod_GetDocumentsForMethodCount(This,pRetVal)	\
    ( (This)->lpVtbl -> GetDocumentsForMethodCount(This,pRetVal) ) 

#define ISymENCUnmanagedMethod_GetDocumentsForMethod(This,cDocs,pcDocs,documents)	\
    ( (This)->lpVtbl -> GetDocumentsForMethod(This,cDocs,pcDocs,documents) ) 

#define ISymENCUnmanagedMethod_GetSourceExtentInDocument(This,document,pstartLine,pendLine)	\
    ( (This)->lpVtbl -> GetSourceExtentInDocument(This,document,pstartLine,pendLine) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ISymENCUnmanagedMethod_INTERFACE_DEFINED__ */


#ifndef __ISymUnmanagedNamespace_INTERFACE_DEFINED__
#define __ISymUnmanagedNamespace_INTERFACE_DEFINED__

/* interface ISymUnmanagedNamespace */
/* [unique][uuid][object] */ 


EXTERN_C const IID IID_ISymUnmanagedNamespace;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("0DFF7289-54F8-11d3-BD28-0000F80849BD")
    ISymUnmanagedNamespace : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE GetName( 
            /* [in] */ ULONG32 cchName,
            /* [out] */ __RPC__out ULONG32 *pcchName,
            /* [length_is][size_is][out] */ __RPC__out_ecount_part(cchName, *pcchName) WCHAR szName[  ]) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetNamespaces( 
            /* [in] */ ULONG32 cNameSpaces,
            /* [out] */ __RPC__out ULONG32 *pcNameSpaces,
            /* [length_is][size_is][out] */ __RPC__out_ecount_part(cNameSpaces, *pcNameSpaces) ISymUnmanagedNamespace *namespaces[  ]) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetVariables( 
            /* [in] */ ULONG32 cVars,
            /* [out] */ __RPC__out ULONG32 *pcVars,
            /* [length_is][size_is][out] */ __RPC__out_ecount_part(cVars, *pcVars) ISymUnmanagedVariable *pVars[  ]) = 0;
        
    };
    
    
#else 	/* C style interface */

    typedef struct ISymUnmanagedNamespaceVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            __RPC__in ISymUnmanagedNamespace * This,
            /* [in] */ __RPC__in REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            __RPC__in ISymUnmanagedNamespace * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            __RPC__in ISymUnmanagedNamespace * This);
        
        HRESULT ( STDMETHODCALLTYPE *GetName )( 
            __RPC__in ISymUnmanagedNamespace * This,
            /* [in] */ ULONG32 cchName,
            /* [out] */ __RPC__out ULONG32 *pcchName,
            /* [length_is][size_is][out] */ __RPC__out_ecount_part(cchName, *pcchName) WCHAR szName[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *GetNamespaces )( 
            __RPC__in ISymUnmanagedNamespace * This,
            /* [in] */ ULONG32 cNameSpaces,
            /* [out] */ __RPC__out ULONG32 *pcNameSpaces,
            /* [length_is][size_is][out] */ __RPC__out_ecount_part(cNameSpaces, *pcNameSpaces) ISymUnmanagedNamespace *namespaces[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *GetVariables )( 
            __RPC__in ISymUnmanagedNamespace * This,
            /* [in] */ ULONG32 cVars,
            /* [out] */ __RPC__out ULONG32 *pcVars,
            /* [length_is][size_is][out] */ __RPC__out_ecount_part(cVars, *pcVars) ISymUnmanagedVariable *pVars[  ]);
        
        END_INTERFACE
    } ISymUnmanagedNamespaceVtbl;

    interface ISymUnmanagedNamespace
    {
        CONST_VTBL struct ISymUnmanagedNamespaceVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define ISymUnmanagedNamespace_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define ISymUnmanagedNamespace_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define ISymUnmanagedNamespace_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define ISymUnmanagedNamespace_GetName(This,cchName,pcchName,szName)	\
    ( (This)->lpVtbl -> GetName(This,cchName,pcchName,szName) ) 

#define ISymUnmanagedNamespace_GetNamespaces(This,cNameSpaces,pcNameSpaces,namespaces)	\
    ( (This)->lpVtbl -> GetNamespaces(This,cNameSpaces,pcNameSpaces,namespaces) ) 

#define ISymUnmanagedNamespace_GetVariables(This,cVars,pcVars,pVars)	\
    ( (This)->lpVtbl -> GetVariables(This,cVars,pcVars,pVars) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ISymUnmanagedNamespace_INTERFACE_DEFINED__ */


#ifndef __ISymUnmanagedReader_INTERFACE_DEFINED__
#define __ISymUnmanagedReader_INTERFACE_DEFINED__

/* interface ISymUnmanagedReader */
/* [unique][uuid][object] */ 


EXTERN_C const IID IID_ISymUnmanagedReader;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("B4CE6286-2A6B-3712-A3B7-1EE1DAD467B5")
    ISymUnmanagedReader : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE GetDocument( 
            /* [in] */ __RPC__in WCHAR *url,
            /* [in] */ GUID language,
            /* [in] */ GUID languageVendor,
            /* [in] */ GUID documentType,
            /* [retval][out] */ __RPC__deref_out_opt ISymUnmanagedDocument **pRetVal) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetDocuments( 
            /* [in] */ ULONG32 cDocs,
            /* [out] */ __RPC__out ULONG32 *pcDocs,
            /* [length_is][size_is][out] */ __RPC__out_ecount_part(cDocs, *pcDocs) ISymUnmanagedDocument *pDocs[  ]) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetUserEntryPoint( 
            /* [retval][out] */ __RPC__out mdMethodDef *pToken) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetMethod( 
            /* [in] */ mdMethodDef token,
            /* [retval][out] */ __RPC__deref_out_opt ISymUnmanagedMethod **pRetVal) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetMethodByVersion( 
            /* [in] */ mdMethodDef token,
            /* [in] */ int version,
            /* [retval][out] */ __RPC__deref_out_opt ISymUnmanagedMethod **pRetVal) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetVariables( 
            /* [in] */ mdToken parent,
            /* [in] */ ULONG32 cVars,
            /* [out] */ __RPC__out ULONG32 *pcVars,
            /* [length_is][size_is][out] */ __RPC__out_ecount_part(cVars, *pcVars) ISymUnmanagedVariable *pVars[  ]) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetGlobalVariables( 
            /* [in] */ ULONG32 cVars,
            /* [out] */ __RPC__out ULONG32 *pcVars,
            /* [length_is][size_is][out] */ __RPC__out_ecount_part(cVars, *pcVars) ISymUnmanagedVariable *pVars[  ]) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetMethodFromDocumentPosition( 
            /* [in] */ __RPC__in_opt ISymUnmanagedDocument *document,
            /* [in] */ ULONG32 line,
            /* [in] */ ULONG32 column,
            /* [retval][out] */ __RPC__deref_out_opt ISymUnmanagedMethod **pRetVal) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetSymAttribute( 
            /* [in] */ mdToken parent,
            /* [in] */ __RPC__in WCHAR *name,
            /* [in] */ ULONG32 cBuffer,
            /* [out] */ __RPC__out ULONG32 *pcBuffer,
            /* [length_is][size_is][out] */ __RPC__out_ecount_part(cBuffer, *pcBuffer) BYTE buffer[  ]) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetNamespaces( 
            /* [in] */ ULONG32 cNameSpaces,
            /* [out] */ __RPC__out ULONG32 *pcNameSpaces,
            /* [length_is][size_is][out] */ __RPC__out_ecount_part(cNameSpaces, *pcNameSpaces) ISymUnmanagedNamespace *namespaces[  ]) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE Initialize( 
            /* [in] */ __RPC__in_opt IUnknown *importer,
            /* [in] */ __RPC__in const WCHAR *filename,
            /* [in] */ __RPC__in const WCHAR *searchPath,
            /* [in] */ __RPC__in_opt IStream *pIStream) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE UpdateSymbolStore( 
            /* [in] */ __RPC__in const WCHAR *filename,
            /* [in] */ __RPC__in_opt IStream *pIStream) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE ReplaceSymbolStore( 
            /* [in] */ __RPC__in const WCHAR *filename,
            /* [in] */ __RPC__in_opt IStream *pIStream) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetSymbolStoreFileName( 
            /* [in] */ ULONG32 cchName,
            /* [out] */ __RPC__out ULONG32 *pcchName,
            /* [length_is][size_is][out] */ __RPC__out_ecount_part(cchName, *pcchName) WCHAR szName[  ]) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetMethodsFromDocumentPosition( 
            /* [in] */ __RPC__in_opt ISymUnmanagedDocument *document,
            /* [in] */ ULONG32 line,
            /* [in] */ ULONG32 column,
            /* [in] */ ULONG32 cMethod,
            /* [out] */ __RPC__out ULONG32 *pcMethod,
            /* [length_is][size_is][out] */ __RPC__out_ecount_part(cMethod, *pcMethod) ISymUnmanagedMethod *pRetVal[  ]) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetDocumentVersion( 
            /* [in] */ __RPC__in_opt ISymUnmanagedDocument *pDoc,
            /* [out] */ __RPC__out int *version,
            /* [out] */ __RPC__out BOOL *pbCurrent) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetMethodVersion( 
            /* [in] */ __RPC__in_opt ISymUnmanagedMethod *pMethod,
            /* [out] */ __RPC__out int *version) = 0;
        
    };
    
    
#else 	/* C style interface */

    typedef struct ISymUnmanagedReaderVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            __RPC__in ISymUnmanagedReader * This,
            /* [in] */ __RPC__in REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            __RPC__in ISymUnmanagedReader * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            __RPC__in ISymUnmanagedReader * This);
        
        HRESULT ( STDMETHODCALLTYPE *GetDocument )( 
            __RPC__in ISymUnmanagedReader * This,
            /* [in] */ __RPC__in WCHAR *url,
            /* [in] */ GUID language,
            /* [in] */ GUID languageVendor,
            /* [in] */ GUID documentType,
            /* [retval][out] */ __RPC__deref_out_opt ISymUnmanagedDocument **pRetVal);
        
        HRESULT ( STDMETHODCALLTYPE *GetDocuments )( 
            __RPC__in ISymUnmanagedReader * This,
            /* [in] */ ULONG32 cDocs,
            /* [out] */ __RPC__out ULONG32 *pcDocs,
            /* [length_is][size_is][out] */ __RPC__out_ecount_part(cDocs, *pcDocs) ISymUnmanagedDocument *pDocs[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *GetUserEntryPoint )( 
            __RPC__in ISymUnmanagedReader * This,
            /* [retval][out] */ __RPC__out mdMethodDef *pToken);
        
        HRESULT ( STDMETHODCALLTYPE *GetMethod )( 
            __RPC__in ISymUnmanagedReader * This,
            /* [in] */ mdMethodDef token,
            /* [retval][out] */ __RPC__deref_out_opt ISymUnmanagedMethod **pRetVal);
        
        HRESULT ( STDMETHODCALLTYPE *GetMethodByVersion )( 
            __RPC__in ISymUnmanagedReader * This,
            /* [in] */ mdMethodDef token,
            /* [in] */ int version,
            /* [retval][out] */ __RPC__deref_out_opt ISymUnmanagedMethod **pRetVal);
        
        HRESULT ( STDMETHODCALLTYPE *GetVariables )( 
            __RPC__in ISymUnmanagedReader * This,
            /* [in] */ mdToken parent,
            /* [in] */ ULONG32 cVars,
            /* [out] */ __RPC__out ULONG32 *pcVars,
            /* [length_is][size_is][out] */ __RPC__out_ecount_part(cVars, *pcVars) ISymUnmanagedVariable *pVars[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *GetGlobalVariables )( 
            __RPC__in ISymUnmanagedReader * This,
            /* [in] */ ULONG32 cVars,
            /* [out] */ __RPC__out ULONG32 *pcVars,
            /* [length_is][size_is][out] */ __RPC__out_ecount_part(cVars, *pcVars) ISymUnmanagedVariable *pVars[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *GetMethodFromDocumentPosition )( 
            __RPC__in ISymUnmanagedReader * This,
            /* [in] */ __RPC__in_opt ISymUnmanagedDocument *document,
            /* [in] */ ULONG32 line,
            /* [in] */ ULONG32 column,
            /* [retval][out] */ __RPC__deref_out_opt ISymUnmanagedMethod **pRetVal);
        
        HRESULT ( STDMETHODCALLTYPE *GetSymAttribute )( 
            __RPC__in ISymUnmanagedReader * This,
            /* [in] */ mdToken parent,
            /* [in] */ __RPC__in WCHAR *name,
            /* [in] */ ULONG32 cBuffer,
            /* [out] */ __RPC__out ULONG32 *pcBuffer,
            /* [length_is][size_is][out] */ __RPC__out_ecount_part(cBuffer, *pcBuffer) BYTE buffer[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *GetNamespaces )( 
            __RPC__in ISymUnmanagedReader * This,
            /* [in] */ ULONG32 cNameSpaces,
            /* [out] */ __RPC__out ULONG32 *pcNameSpaces,
            /* [length_is][size_is][out] */ __RPC__out_ecount_part(cNameSpaces, *pcNameSpaces) ISymUnmanagedNamespace *namespaces[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *Initialize )( 
            __RPC__in ISymUnmanagedReader * This,
            /* [in] */ __RPC__in_opt IUnknown *importer,
            /* [in] */ __RPC__in const WCHAR *filename,
            /* [in] */ __RPC__in const WCHAR *searchPath,
            /* [in] */ __RPC__in_opt IStream *pIStream);
        
        HRESULT ( STDMETHODCALLTYPE *UpdateSymbolStore )( 
            __RPC__in ISymUnmanagedReader * This,
            /* [in] */ __RPC__in const WCHAR *filename,
            /* [in] */ __RPC__in_opt IStream *pIStream);
        
        HRESULT ( STDMETHODCALLTYPE *ReplaceSymbolStore )( 
            __RPC__in ISymUnmanagedReader * This,
            /* [in] */ __RPC__in const WCHAR *filename,
            /* [in] */ __RPC__in_opt IStream *pIStream);
        
        HRESULT ( STDMETHODCALLTYPE *GetSymbolStoreFileName )( 
            __RPC__in ISymUnmanagedReader * This,
            /* [in] */ ULONG32 cchName,
            /* [out] */ __RPC__out ULONG32 *pcchName,
            /* [length_is][size_is][out] */ __RPC__out_ecount_part(cchName, *pcchName) WCHAR szName[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *GetMethodsFromDocumentPosition )( 
            __RPC__in ISymUnmanagedReader * This,
            /* [in] */ __RPC__in_opt ISymUnmanagedDocument *document,
            /* [in] */ ULONG32 line,
            /* [in] */ ULONG32 column,
            /* [in] */ ULONG32 cMethod,
            /* [out] */ __RPC__out ULONG32 *pcMethod,
            /* [length_is][size_is][out] */ __RPC__out_ecount_part(cMethod, *pcMethod) ISymUnmanagedMethod *pRetVal[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *GetDocumentVersion )( 
            __RPC__in ISymUnmanagedReader * This,
            /* [in] */ __RPC__in_opt ISymUnmanagedDocument *pDoc,
            /* [out] */ __RPC__out int *version,
            /* [out] */ __RPC__out BOOL *pbCurrent);
        
        HRESULT ( STDMETHODCALLTYPE *GetMethodVersion )( 
            __RPC__in ISymUnmanagedReader * This,
            /* [in] */ __RPC__in_opt ISymUnmanagedMethod *pMethod,
            /* [out] */ __RPC__out int *version);
        
        END_INTERFACE
    } ISymUnmanagedReaderVtbl;

    interface ISymUnmanagedReader
    {
        CONST_VTBL struct ISymUnmanagedReaderVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define ISymUnmanagedReader_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define ISymUnmanagedReader_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define ISymUnmanagedReader_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define ISymUnmanagedReader_GetDocument(This,url,language,languageVendor,documentType,pRetVal)	\
    ( (This)->lpVtbl -> GetDocument(This,url,language,languageVendor,documentType,pRetVal) ) 

#define ISymUnmanagedReader_GetDocuments(This,cDocs,pcDocs,pDocs)	\
    ( (This)->lpVtbl -> GetDocuments(This,cDocs,pcDocs,pDocs) ) 

#define ISymUnmanagedReader_GetUserEntryPoint(This,pToken)	\
    ( (This)->lpVtbl -> GetUserEntryPoint(This,pToken) ) 

#define ISymUnmanagedReader_GetMethod(This,token,pRetVal)	\
    ( (This)->lpVtbl -> GetMethod(This,token,pRetVal) ) 

#define ISymUnmanagedReader_GetMethodByVersion(This,token,version,pRetVal)	\
    ( (This)->lpVtbl -> GetMethodByVersion(This,token,version,pRetVal) ) 

#define ISymUnmanagedReader_GetVariables(This,parent,cVars,pcVars,pVars)	\
    ( (This)->lpVtbl -> GetVariables(This,parent,cVars,pcVars,pVars) ) 

#define ISymUnmanagedReader_GetGlobalVariables(This,cVars,pcVars,pVars)	\
    ( (This)->lpVtbl -> GetGlobalVariables(This,cVars,pcVars,pVars) ) 

#define ISymUnmanagedReader_GetMethodFromDocumentPosition(This,document,line,column,pRetVal)	\
    ( (This)->lpVtbl -> GetMethodFromDocumentPosition(This,document,line,column,pRetVal) ) 

#define ISymUnmanagedReader_GetSymAttribute(This,parent,name,cBuffer,pcBuffer,buffer)	\
    ( (This)->lpVtbl -> GetSymAttribute(This,parent,name,cBuffer,pcBuffer,buffer) ) 

#define ISymUnmanagedReader_GetNamespaces(This,cNameSpaces,pcNameSpaces,namespaces)	\
    ( (This)->lpVtbl -> GetNamespaces(This,cNameSpaces,pcNameSpaces,namespaces) ) 

#define ISymUnmanagedReader_Initialize(This,importer,filename,searchPath,pIStream)	\
    ( (This)->lpVtbl -> Initialize(This,importer,filename,searchPath,pIStream) ) 

#define ISymUnmanagedReader_UpdateSymbolStore(This,filename,pIStream)	\
    ( (This)->lpVtbl -> UpdateSymbolStore(This,filename,pIStream) ) 

#define ISymUnmanagedReader_ReplaceSymbolStore(This,filename,pIStream)	\
    ( (This)->lpVtbl -> ReplaceSymbolStore(This,filename,pIStream) ) 

#define ISymUnmanagedReader_GetSymbolStoreFileName(This,cchName,pcchName,szName)	\
    ( (This)->lpVtbl -> GetSymbolStoreFileName(This,cchName,pcchName,szName) ) 

#define ISymUnmanagedReader_GetMethodsFromDocumentPosition(This,document,line,column,cMethod,pcMethod,pRetVal)	\
    ( (This)->lpVtbl -> GetMethodsFromDocumentPosition(This,document,line,column,cMethod,pcMethod,pRetVal) ) 

#define ISymUnmanagedReader_GetDocumentVersion(This,pDoc,version,pbCurrent)	\
    ( (This)->lpVtbl -> GetDocumentVersion(This,pDoc,version,pbCurrent) ) 

#define ISymUnmanagedReader_GetMethodVersion(This,pMethod,version)	\
    ( (This)->lpVtbl -> GetMethodVersion(This,pMethod,version) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ISymUnmanagedReader_INTERFACE_DEFINED__ */


#ifndef __ISymUnmanagedSourceServerModule_INTERFACE_DEFINED__
#define __ISymUnmanagedSourceServerModule_INTERFACE_DEFINED__

/* interface ISymUnmanagedSourceServerModule */
/* [unique][uuid][object] */ 


EXTERN_C const IID IID_ISymUnmanagedSourceServerModule;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("997DD0CC-A76F-4c82-8D79-EA87559D27AD")
    ISymUnmanagedSourceServerModule : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE GetSourceServerData( 
            /* [out] */ __RPC__out ULONG *pDataByteCount,
            /* [size_is][size_is][out] */ __RPC__deref_out_ecount_full_opt(*pDataByteCount) BYTE **ppData) = 0;
        
    };
    
    
#else 	/* C style interface */

    typedef struct ISymUnmanagedSourceServerModuleVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            __RPC__in ISymUnmanagedSourceServerModule * This,
            /* [in] */ __RPC__in REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            __RPC__in ISymUnmanagedSourceServerModule * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            __RPC__in ISymUnmanagedSourceServerModule * This);
        
        HRESULT ( STDMETHODCALLTYPE *GetSourceServerData )( 
            __RPC__in ISymUnmanagedSourceServerModule * This,
            /* [out] */ __RPC__out ULONG *pDataByteCount,
            /* [size_is][size_is][out] */ __RPC__deref_out_ecount_full_opt(*pDataByteCount) BYTE **ppData);
        
        END_INTERFACE
    } ISymUnmanagedSourceServerModuleVtbl;

    interface ISymUnmanagedSourceServerModule
    {
        CONST_VTBL struct ISymUnmanagedSourceServerModuleVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define ISymUnmanagedSourceServerModule_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define ISymUnmanagedSourceServerModule_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define ISymUnmanagedSourceServerModule_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define ISymUnmanagedSourceServerModule_GetSourceServerData(This,pDataByteCount,ppData)	\
    ( (This)->lpVtbl -> GetSourceServerData(This,pDataByteCount,ppData) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ISymUnmanagedSourceServerModule_INTERFACE_DEFINED__ */


#ifndef __ISymUnmanagedENCUpdate_INTERFACE_DEFINED__
#define __ISymUnmanagedENCUpdate_INTERFACE_DEFINED__

/* interface ISymUnmanagedENCUpdate */
/* [unique][uuid][object] */ 

typedef struct _SYMLINEDELTA
    {
    mdMethodDef mdMethod;
    INT32 delta;
    } 	SYMLINEDELTA;


EXTERN_C const IID IID_ISymUnmanagedENCUpdate;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("E502D2DD-8671-4338-8F2A-FC08229628C4")
    ISymUnmanagedENCUpdate : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE UpdateSymbolStore2( 
            /* [in] */ __RPC__in_opt IStream *pIStream,
            /* [in] */ __RPC__in SYMLINEDELTA *pDeltaLines,
            /* [in] */ ULONG cDeltaLines) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetLocalVariableCount( 
            /* [in] */ mdMethodDef mdMethodToken,
            /* [out] */ __RPC__out ULONG *pcLocals) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetLocalVariables( 
            /* [in] */ mdMethodDef mdMethodToken,
            /* [in] */ ULONG cLocals,
            /* [length_is][size_is][out] */ __RPC__out_ecount_part(cLocals, *pceltFetched) ISymUnmanagedVariable *rgLocals[  ],
            /* [out] */ __RPC__out ULONG *pceltFetched) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE InitializeForEnc( void) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE UpdateMethodLines( 
            /* [in] */ mdMethodDef mdMethodToken,
            /* [size_is][in] */ __RPC__in_ecount_full(cDeltas) INT32 *pDeltas,
            /* [in] */ ULONG cDeltas) = 0;
        
    };
    
    
#else 	/* C style interface */

    typedef struct ISymUnmanagedENCUpdateVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            __RPC__in ISymUnmanagedENCUpdate * This,
            /* [in] */ __RPC__in REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            __RPC__in ISymUnmanagedENCUpdate * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            __RPC__in ISymUnmanagedENCUpdate * This);
        
        HRESULT ( STDMETHODCALLTYPE *UpdateSymbolStore2 )( 
            __RPC__in ISymUnmanagedENCUpdate * This,
            /* [in] */ __RPC__in_opt IStream *pIStream,
            /* [in] */ __RPC__in SYMLINEDELTA *pDeltaLines,
            /* [in] */ ULONG cDeltaLines);
        
        HRESULT ( STDMETHODCALLTYPE *GetLocalVariableCount )( 
            __RPC__in ISymUnmanagedENCUpdate * This,
            /* [in] */ mdMethodDef mdMethodToken,
            /* [out] */ __RPC__out ULONG *pcLocals);
        
        HRESULT ( STDMETHODCALLTYPE *GetLocalVariables )( 
            __RPC__in ISymUnmanagedENCUpdate * This,
            /* [in] */ mdMethodDef mdMethodToken,
            /* [in] */ ULONG cLocals,
            /* [length_is][size_is][out] */ __RPC__out_ecount_part(cLocals, *pceltFetched) ISymUnmanagedVariable *rgLocals[  ],
            /* [out] */ __RPC__out ULONG *pceltFetched);
        
        HRESULT ( STDMETHODCALLTYPE *InitializeForEnc )( 
            __RPC__in ISymUnmanagedENCUpdate * This);
        
        HRESULT ( STDMETHODCALLTYPE *UpdateMethodLines )( 
            __RPC__in ISymUnmanagedENCUpdate * This,
            /* [in] */ mdMethodDef mdMethodToken,
            /* [size_is][in] */ __RPC__in_ecount_full(cDeltas) INT32 *pDeltas,
            /* [in] */ ULONG cDeltas);
        
        END_INTERFACE
    } ISymUnmanagedENCUpdateVtbl;

    interface ISymUnmanagedENCUpdate
    {
        CONST_VTBL struct ISymUnmanagedENCUpdateVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define ISymUnmanagedENCUpdate_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define ISymUnmanagedENCUpdate_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define ISymUnmanagedENCUpdate_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define ISymUnmanagedENCUpdate_UpdateSymbolStore2(This,pIStream,pDeltaLines,cDeltaLines)	\
    ( (This)->lpVtbl -> UpdateSymbolStore2(This,pIStream,pDeltaLines,cDeltaLines) ) 

#define ISymUnmanagedENCUpdate_GetLocalVariableCount(This,mdMethodToken,pcLocals)	\
    ( (This)->lpVtbl -> GetLocalVariableCount(This,mdMethodToken,pcLocals) ) 

#define ISymUnmanagedENCUpdate_GetLocalVariables(This,mdMethodToken,cLocals,rgLocals,pceltFetched)	\
    ( (This)->lpVtbl -> GetLocalVariables(This,mdMethodToken,cLocals,rgLocals,pceltFetched) ) 

#define ISymUnmanagedENCUpdate_InitializeForEnc(This)	\
    ( (This)->lpVtbl -> InitializeForEnc(This) ) 

#define ISymUnmanagedENCUpdate_UpdateMethodLines(This,mdMethodToken,pDeltas,cDeltas)	\
    ( (This)->lpVtbl -> UpdateMethodLines(This,mdMethodToken,pDeltas,cDeltas) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ISymUnmanagedENCUpdate_INTERFACE_DEFINED__ */


#ifndef __ISymUnmanagedReaderSymbolSearchInfo_INTERFACE_DEFINED__
#define __ISymUnmanagedReaderSymbolSearchInfo_INTERFACE_DEFINED__

/* interface ISymUnmanagedReaderSymbolSearchInfo */
/* [unique][uuid][object] */ 


EXTERN_C const IID IID_ISymUnmanagedReaderSymbolSearchInfo;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("20D9645D-03CD-4e34-9C11-9848A5B084F1")
    ISymUnmanagedReaderSymbolSearchInfo : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE GetSymbolSearchInfoCount( 
            /* [out] */ __RPC__out ULONG32 *pcSearchInfo) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetSymbolSearchInfo( 
            /* [in] */ ULONG32 cSearchInfo,
            /* [out] */ __RPC__out ULONG32 *pcSearchInfo,
            /* [length_is][size_is][out] */ __RPC__out_ecount_part(cSearchInfo, *pcSearchInfo) ISymUnmanagedSymbolSearchInfo **rgpSearchInfo) = 0;
        
    };
    
    
#else 	/* C style interface */

    typedef struct ISymUnmanagedReaderSymbolSearchInfoVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            __RPC__in ISymUnmanagedReaderSymbolSearchInfo * This,
            /* [in] */ __RPC__in REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            __RPC__in ISymUnmanagedReaderSymbolSearchInfo * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            __RPC__in ISymUnmanagedReaderSymbolSearchInfo * This);
        
        HRESULT ( STDMETHODCALLTYPE *GetSymbolSearchInfoCount )( 
            __RPC__in ISymUnmanagedReaderSymbolSearchInfo * This,
            /* [out] */ __RPC__out ULONG32 *pcSearchInfo);
        
        HRESULT ( STDMETHODCALLTYPE *GetSymbolSearchInfo )( 
            __RPC__in ISymUnmanagedReaderSymbolSearchInfo * This,
            /* [in] */ ULONG32 cSearchInfo,
            /* [out] */ __RPC__out ULONG32 *pcSearchInfo,
            /* [length_is][size_is][out] */ __RPC__out_ecount_part(cSearchInfo, *pcSearchInfo) ISymUnmanagedSymbolSearchInfo **rgpSearchInfo);
        
        END_INTERFACE
    } ISymUnmanagedReaderSymbolSearchInfoVtbl;

    interface ISymUnmanagedReaderSymbolSearchInfo
    {
        CONST_VTBL struct ISymUnmanagedReaderSymbolSearchInfoVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define ISymUnmanagedReaderSymbolSearchInfo_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define ISymUnmanagedReaderSymbolSearchInfo_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define ISymUnmanagedReaderSymbolSearchInfo_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define ISymUnmanagedReaderSymbolSearchInfo_GetSymbolSearchInfoCount(This,pcSearchInfo)	\
    ( (This)->lpVtbl -> GetSymbolSearchInfoCount(This,pcSearchInfo) ) 

#define ISymUnmanagedReaderSymbolSearchInfo_GetSymbolSearchInfo(This,cSearchInfo,pcSearchInfo,rgpSearchInfo)	\
    ( (This)->lpVtbl -> GetSymbolSearchInfo(This,cSearchInfo,pcSearchInfo,rgpSearchInfo) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ISymUnmanagedReaderSymbolSearchInfo_INTERFACE_DEFINED__ */


#ifndef __ISymUnmanagedScope_INTERFACE_DEFINED__
#define __ISymUnmanagedScope_INTERFACE_DEFINED__

/* interface ISymUnmanagedScope */
/* [unique][uuid][object] */ 


EXTERN_C const IID IID_ISymUnmanagedScope;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("68005D0F-B8E0-3B01-84D5-A11A94154942")
    ISymUnmanagedScope : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE GetMethod( 
            /* [retval][out] */ __RPC__deref_out_opt ISymUnmanagedMethod **pRetVal) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetParent( 
            /* [retval][out] */ __RPC__deref_out_opt ISymUnmanagedScope **pRetVal) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetChildren( 
            /* [in] */ ULONG32 cChildren,
            /* [out] */ __RPC__out ULONG32 *pcChildren,
            /* [length_is][size_is][out] */ __RPC__out_ecount_part(cChildren, *pcChildren) ISymUnmanagedScope *children[  ]) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetStartOffset( 
            /* [retval][out] */ __RPC__out ULONG32 *pRetVal) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetEndOffset( 
            /* [retval][out] */ __RPC__out ULONG32 *pRetVal) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetLocalCount( 
            /* [retval][out] */ __RPC__out ULONG32 *pRetVal) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetLocals( 
            /* [in] */ ULONG32 cLocals,
            /* [out] */ __RPC__out ULONG32 *pcLocals,
            /* [length_is][size_is][out] */ __RPC__out_ecount_part(cLocals, *pcLocals) ISymUnmanagedVariable *locals[  ]) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetNamespaces( 
            /* [in] */ ULONG32 cNameSpaces,
            /* [out] */ __RPC__out ULONG32 *pcNameSpaces,
            /* [length_is][size_is][out] */ __RPC__out_ecount_part(cNameSpaces, *pcNameSpaces) ISymUnmanagedNamespace *namespaces[  ]) = 0;
        
    };
    
    
#else 	/* C style interface */

    typedef struct ISymUnmanagedScopeVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            __RPC__in ISymUnmanagedScope * This,
            /* [in] */ __RPC__in REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            __RPC__in ISymUnmanagedScope * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            __RPC__in ISymUnmanagedScope * This);
        
        HRESULT ( STDMETHODCALLTYPE *GetMethod )( 
            __RPC__in ISymUnmanagedScope * This,
            /* [retval][out] */ __RPC__deref_out_opt ISymUnmanagedMethod **pRetVal);
        
        HRESULT ( STDMETHODCALLTYPE *GetParent )( 
            __RPC__in ISymUnmanagedScope * This,
            /* [retval][out] */ __RPC__deref_out_opt ISymUnmanagedScope **pRetVal);
        
        HRESULT ( STDMETHODCALLTYPE *GetChildren )( 
            __RPC__in ISymUnmanagedScope * This,
            /* [in] */ ULONG32 cChildren,
            /* [out] */ __RPC__out ULONG32 *pcChildren,
            /* [length_is][size_is][out] */ __RPC__out_ecount_part(cChildren, *pcChildren) ISymUnmanagedScope *children[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *GetStartOffset )( 
            __RPC__in ISymUnmanagedScope * This,
            /* [retval][out] */ __RPC__out ULONG32 *pRetVal);
        
        HRESULT ( STDMETHODCALLTYPE *GetEndOffset )( 
            __RPC__in ISymUnmanagedScope * This,
            /* [retval][out] */ __RPC__out ULONG32 *pRetVal);
        
        HRESULT ( STDMETHODCALLTYPE *GetLocalCount )( 
            __RPC__in ISymUnmanagedScope * This,
            /* [retval][out] */ __RPC__out ULONG32 *pRetVal);
        
        HRESULT ( STDMETHODCALLTYPE *GetLocals )( 
            __RPC__in ISymUnmanagedScope * This,
            /* [in] */ ULONG32 cLocals,
            /* [out] */ __RPC__out ULONG32 *pcLocals,
            /* [length_is][size_is][out] */ __RPC__out_ecount_part(cLocals, *pcLocals) ISymUnmanagedVariable *locals[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *GetNamespaces )( 
            __RPC__in ISymUnmanagedScope * This,
            /* [in] */ ULONG32 cNameSpaces,
            /* [out] */ __RPC__out ULONG32 *pcNameSpaces,
            /* [length_is][size_is][out] */ __RPC__out_ecount_part(cNameSpaces, *pcNameSpaces) ISymUnmanagedNamespace *namespaces[  ]);
        
        END_INTERFACE
    } ISymUnmanagedScopeVtbl;

    interface ISymUnmanagedScope
    {
        CONST_VTBL struct ISymUnmanagedScopeVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define ISymUnmanagedScope_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define ISymUnmanagedScope_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define ISymUnmanagedScope_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define ISymUnmanagedScope_GetMethod(This,pRetVal)	\
    ( (This)->lpVtbl -> GetMethod(This,pRetVal) ) 

#define ISymUnmanagedScope_GetParent(This,pRetVal)	\
    ( (This)->lpVtbl -> GetParent(This,pRetVal) ) 

#define ISymUnmanagedScope_GetChildren(This,cChildren,pcChildren,children)	\
    ( (This)->lpVtbl -> GetChildren(This,cChildren,pcChildren,children) ) 

#define ISymUnmanagedScope_GetStartOffset(This,pRetVal)	\
    ( (This)->lpVtbl -> GetStartOffset(This,pRetVal) ) 

#define ISymUnmanagedScope_GetEndOffset(This,pRetVal)	\
    ( (This)->lpVtbl -> GetEndOffset(This,pRetVal) ) 

#define ISymUnmanagedScope_GetLocalCount(This,pRetVal)	\
    ( (This)->lpVtbl -> GetLocalCount(This,pRetVal) ) 

#define ISymUnmanagedScope_GetLocals(This,cLocals,pcLocals,locals)	\
    ( (This)->lpVtbl -> GetLocals(This,cLocals,pcLocals,locals) ) 

#define ISymUnmanagedScope_GetNamespaces(This,cNameSpaces,pcNameSpaces,namespaces)	\
    ( (This)->lpVtbl -> GetNamespaces(This,cNameSpaces,pcNameSpaces,namespaces) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ISymUnmanagedScope_INTERFACE_DEFINED__ */


#ifndef __ISymUnmanagedConstant_INTERFACE_DEFINED__
#define __ISymUnmanagedConstant_INTERFACE_DEFINED__

/* interface ISymUnmanagedConstant */
/* [unique][uuid][object] */ 


EXTERN_C const IID IID_ISymUnmanagedConstant;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("48B25ED8-5BAD-41bc-9CEE-CD62FABC74E9")
    ISymUnmanagedConstant : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE GetName( 
            /* [in] */ ULONG32 cchName,
            /* [out] */ __RPC__out ULONG32 *pcchName,
            /* [length_is][size_is][out] */ __RPC__out_ecount_part(cchName, *pcchName) WCHAR szName[  ]) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetValue( 
            __RPC__in VARIANT *pValue) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetSignature( 
            /* [in] */ ULONG32 cSig,
            /* [out] */ __RPC__out ULONG32 *pcSig,
            /* [length_is][size_is][out] */ __RPC__out_ecount_part(cSig, *pcSig) BYTE sig[  ]) = 0;
        
    };
    
    
#else 	/* C style interface */

    typedef struct ISymUnmanagedConstantVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            __RPC__in ISymUnmanagedConstant * This,
            /* [in] */ __RPC__in REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            __RPC__in ISymUnmanagedConstant * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            __RPC__in ISymUnmanagedConstant * This);
        
        HRESULT ( STDMETHODCALLTYPE *GetName )( 
            __RPC__in ISymUnmanagedConstant * This,
            /* [in] */ ULONG32 cchName,
            /* [out] */ __RPC__out ULONG32 *pcchName,
            /* [length_is][size_is][out] */ __RPC__out_ecount_part(cchName, *pcchName) WCHAR szName[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *GetValue )( 
            __RPC__in ISymUnmanagedConstant * This,
            __RPC__in VARIANT *pValue);
        
        HRESULT ( STDMETHODCALLTYPE *GetSignature )( 
            __RPC__in ISymUnmanagedConstant * This,
            /* [in] */ ULONG32 cSig,
            /* [out] */ __RPC__out ULONG32 *pcSig,
            /* [length_is][size_is][out] */ __RPC__out_ecount_part(cSig, *pcSig) BYTE sig[  ]);
        
        END_INTERFACE
    } ISymUnmanagedConstantVtbl;

    interface ISymUnmanagedConstant
    {
        CONST_VTBL struct ISymUnmanagedConstantVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define ISymUnmanagedConstant_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define ISymUnmanagedConstant_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define ISymUnmanagedConstant_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define ISymUnmanagedConstant_GetName(This,cchName,pcchName,szName)	\
    ( (This)->lpVtbl -> GetName(This,cchName,pcchName,szName) ) 

#define ISymUnmanagedConstant_GetValue(This,pValue)	\
    ( (This)->lpVtbl -> GetValue(This,pValue) ) 

#define ISymUnmanagedConstant_GetSignature(This,cSig,pcSig,sig)	\
    ( (This)->lpVtbl -> GetSignature(This,cSig,pcSig,sig) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ISymUnmanagedConstant_INTERFACE_DEFINED__ */


#ifndef __ISymUnmanagedScope2_INTERFACE_DEFINED__
#define __ISymUnmanagedScope2_INTERFACE_DEFINED__

/* interface ISymUnmanagedScope2 */
/* [unique][uuid][object] */ 


EXTERN_C const IID IID_ISymUnmanagedScope2;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("AE932FBA-3FD8-4dba-8232-30A2309B02DB")
    ISymUnmanagedScope2 : public ISymUnmanagedScope
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE GetConstantCount( 
            /* [retval][out] */ __RPC__out ULONG32 *pRetVal) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetConstants( 
            /* [in] */ ULONG32 cConstants,
            /* [out] */ __RPC__out ULONG32 *pcConstants,
            /* [length_is][size_is][out] */ __RPC__out_ecount_part(cConstants, *pcConstants) ISymUnmanagedConstant *constants[  ]) = 0;
        
    };
    
    
#else 	/* C style interface */

    typedef struct ISymUnmanagedScope2Vtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            __RPC__in ISymUnmanagedScope2 * This,
            /* [in] */ __RPC__in REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            __RPC__in ISymUnmanagedScope2 * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            __RPC__in ISymUnmanagedScope2 * This);
        
        HRESULT ( STDMETHODCALLTYPE *GetMethod )( 
            __RPC__in ISymUnmanagedScope2 * This,
            /* [retval][out] */ __RPC__deref_out_opt ISymUnmanagedMethod **pRetVal);
        
        HRESULT ( STDMETHODCALLTYPE *GetParent )( 
            __RPC__in ISymUnmanagedScope2 * This,
            /* [retval][out] */ __RPC__deref_out_opt ISymUnmanagedScope **pRetVal);
        
        HRESULT ( STDMETHODCALLTYPE *GetChildren )( 
            __RPC__in ISymUnmanagedScope2 * This,
            /* [in] */ ULONG32 cChildren,
            /* [out] */ __RPC__out ULONG32 *pcChildren,
            /* [length_is][size_is][out] */ __RPC__out_ecount_part(cChildren, *pcChildren) ISymUnmanagedScope *children[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *GetStartOffset )( 
            __RPC__in ISymUnmanagedScope2 * This,
            /* [retval][out] */ __RPC__out ULONG32 *pRetVal);
        
        HRESULT ( STDMETHODCALLTYPE *GetEndOffset )( 
            __RPC__in ISymUnmanagedScope2 * This,
            /* [retval][out] */ __RPC__out ULONG32 *pRetVal);
        
        HRESULT ( STDMETHODCALLTYPE *GetLocalCount )( 
            __RPC__in ISymUnmanagedScope2 * This,
            /* [retval][out] */ __RPC__out ULONG32 *pRetVal);
        
        HRESULT ( STDMETHODCALLTYPE *GetLocals )( 
            __RPC__in ISymUnmanagedScope2 * This,
            /* [in] */ ULONG32 cLocals,
            /* [out] */ __RPC__out ULONG32 *pcLocals,
            /* [length_is][size_is][out] */ __RPC__out_ecount_part(cLocals, *pcLocals) ISymUnmanagedVariable *locals[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *GetNamespaces )( 
            __RPC__in ISymUnmanagedScope2 * This,
            /* [in] */ ULONG32 cNameSpaces,
            /* [out] */ __RPC__out ULONG32 *pcNameSpaces,
            /* [length_is][size_is][out] */ __RPC__out_ecount_part(cNameSpaces, *pcNameSpaces) ISymUnmanagedNamespace *namespaces[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *GetConstantCount )( 
            __RPC__in ISymUnmanagedScope2 * This,
            /* [retval][out] */ __RPC__out ULONG32 *pRetVal);
        
        HRESULT ( STDMETHODCALLTYPE *GetConstants )( 
            __RPC__in ISymUnmanagedScope2 * This,
            /* [in] */ ULONG32 cConstants,
            /* [out] */ __RPC__out ULONG32 *pcConstants,
            /* [length_is][size_is][out] */ __RPC__out_ecount_part(cConstants, *pcConstants) ISymUnmanagedConstant *constants[  ]);
        
        END_INTERFACE
    } ISymUnmanagedScope2Vtbl;

    interface ISymUnmanagedScope2
    {
        CONST_VTBL struct ISymUnmanagedScope2Vtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define ISymUnmanagedScope2_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define ISymUnmanagedScope2_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define ISymUnmanagedScope2_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define ISymUnmanagedScope2_GetMethod(This,pRetVal)	\
    ( (This)->lpVtbl -> GetMethod(This,pRetVal) ) 

#define ISymUnmanagedScope2_GetParent(This,pRetVal)	\
    ( (This)->lpVtbl -> GetParent(This,pRetVal) ) 

#define ISymUnmanagedScope2_GetChildren(This,cChildren,pcChildren,children)	\
    ( (This)->lpVtbl -> GetChildren(This,cChildren,pcChildren,children) ) 

#define ISymUnmanagedScope2_GetStartOffset(This,pRetVal)	\
    ( (This)->lpVtbl -> GetStartOffset(This,pRetVal) ) 

#define ISymUnmanagedScope2_GetEndOffset(This,pRetVal)	\
    ( (This)->lpVtbl -> GetEndOffset(This,pRetVal) ) 

#define ISymUnmanagedScope2_GetLocalCount(This,pRetVal)	\
    ( (This)->lpVtbl -> GetLocalCount(This,pRetVal) ) 

#define ISymUnmanagedScope2_GetLocals(This,cLocals,pcLocals,locals)	\
    ( (This)->lpVtbl -> GetLocals(This,cLocals,pcLocals,locals) ) 

#define ISymUnmanagedScope2_GetNamespaces(This,cNameSpaces,pcNameSpaces,namespaces)	\
    ( (This)->lpVtbl -> GetNamespaces(This,cNameSpaces,pcNameSpaces,namespaces) ) 


#define ISymUnmanagedScope2_GetConstantCount(This,pRetVal)	\
    ( (This)->lpVtbl -> GetConstantCount(This,pRetVal) ) 

#define ISymUnmanagedScope2_GetConstants(This,cConstants,pcConstants,constants)	\
    ( (This)->lpVtbl -> GetConstants(This,cConstants,pcConstants,constants) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ISymUnmanagedScope2_INTERFACE_DEFINED__ */


#ifndef __ISymUnmanagedVariable_INTERFACE_DEFINED__
#define __ISymUnmanagedVariable_INTERFACE_DEFINED__

/* interface ISymUnmanagedVariable */
/* [unique][uuid][object] */ 


EXTERN_C const IID IID_ISymUnmanagedVariable;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("9F60EEBE-2D9A-3F7C-BF58-80BC991C60BB")
    ISymUnmanagedVariable : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE GetName( 
            /* [in] */ ULONG32 cchName,
            /* [out] */ __RPC__out ULONG32 *pcchName,
            /* [length_is][size_is][out] */ __RPC__out_ecount_part(cchName, *pcchName) WCHAR szName[  ]) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetAttributes( 
            /* [retval][out] */ __RPC__out ULONG32 *pRetVal) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetSignature( 
            /* [in] */ ULONG32 cSig,
            /* [out] */ __RPC__out ULONG32 *pcSig,
            /* [length_is][size_is][out] */ __RPC__out_ecount_part(cSig, *pcSig) BYTE sig[  ]) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetAddressKind( 
            /* [retval][out] */ __RPC__out ULONG32 *pRetVal) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetAddressField1( 
            /* [retval][out] */ __RPC__out ULONG32 *pRetVal) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetAddressField2( 
            /* [retval][out] */ __RPC__out ULONG32 *pRetVal) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetAddressField3( 
            /* [retval][out] */ __RPC__out ULONG32 *pRetVal) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetStartOffset( 
            /* [retval][out] */ __RPC__out ULONG32 *pRetVal) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetEndOffset( 
            /* [retval][out] */ __RPC__out ULONG32 *pRetVal) = 0;
        
    };
    
    
#else 	/* C style interface */

    typedef struct ISymUnmanagedVariableVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            __RPC__in ISymUnmanagedVariable * This,
            /* [in] */ __RPC__in REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            __RPC__in ISymUnmanagedVariable * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            __RPC__in ISymUnmanagedVariable * This);
        
        HRESULT ( STDMETHODCALLTYPE *GetName )( 
            __RPC__in ISymUnmanagedVariable * This,
            /* [in] */ ULONG32 cchName,
            /* [out] */ __RPC__out ULONG32 *pcchName,
            /* [length_is][size_is][out] */ __RPC__out_ecount_part(cchName, *pcchName) WCHAR szName[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *GetAttributes )( 
            __RPC__in ISymUnmanagedVariable * This,
            /* [retval][out] */ __RPC__out ULONG32 *pRetVal);
        
        HRESULT ( STDMETHODCALLTYPE *GetSignature )( 
            __RPC__in ISymUnmanagedVariable * This,
            /* [in] */ ULONG32 cSig,
            /* [out] */ __RPC__out ULONG32 *pcSig,
            /* [length_is][size_is][out] */ __RPC__out_ecount_part(cSig, *pcSig) BYTE sig[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *GetAddressKind )( 
            __RPC__in ISymUnmanagedVariable * This,
            /* [retval][out] */ __RPC__out ULONG32 *pRetVal);
        
        HRESULT ( STDMETHODCALLTYPE *GetAddressField1 )( 
            __RPC__in ISymUnmanagedVariable * This,
            /* [retval][out] */ __RPC__out ULONG32 *pRetVal);
        
        HRESULT ( STDMETHODCALLTYPE *GetAddressField2 )( 
            __RPC__in ISymUnmanagedVariable * This,
            /* [retval][out] */ __RPC__out ULONG32 *pRetVal);
        
        HRESULT ( STDMETHODCALLTYPE *GetAddressField3 )( 
            __RPC__in ISymUnmanagedVariable * This,
            /* [retval][out] */ __RPC__out ULONG32 *pRetVal);
        
        HRESULT ( STDMETHODCALLTYPE *GetStartOffset )( 
            __RPC__in ISymUnmanagedVariable * This,
            /* [retval][out] */ __RPC__out ULONG32 *pRetVal);
        
        HRESULT ( STDMETHODCALLTYPE *GetEndOffset )( 
            __RPC__in ISymUnmanagedVariable * This,
            /* [retval][out] */ __RPC__out ULONG32 *pRetVal);
        
        END_INTERFACE
    } ISymUnmanagedVariableVtbl;

    interface ISymUnmanagedVariable
    {
        CONST_VTBL struct ISymUnmanagedVariableVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define ISymUnmanagedVariable_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define ISymUnmanagedVariable_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define ISymUnmanagedVariable_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define ISymUnmanagedVariable_GetName(This,cchName,pcchName,szName)	\
    ( (This)->lpVtbl -> GetName(This,cchName,pcchName,szName) ) 

#define ISymUnmanagedVariable_GetAttributes(This,pRetVal)	\
    ( (This)->lpVtbl -> GetAttributes(This,pRetVal) ) 

#define ISymUnmanagedVariable_GetSignature(This,cSig,pcSig,sig)	\
    ( (This)->lpVtbl -> GetSignature(This,cSig,pcSig,sig) ) 

#define ISymUnmanagedVariable_GetAddressKind(This,pRetVal)	\
    ( (This)->lpVtbl -> GetAddressKind(This,pRetVal) ) 

#define ISymUnmanagedVariable_GetAddressField1(This,pRetVal)	\
    ( (This)->lpVtbl -> GetAddressField1(This,pRetVal) ) 

#define ISymUnmanagedVariable_GetAddressField2(This,pRetVal)	\
    ( (This)->lpVtbl -> GetAddressField2(This,pRetVal) ) 

#define ISymUnmanagedVariable_GetAddressField3(This,pRetVal)	\
    ( (This)->lpVtbl -> GetAddressField3(This,pRetVal) ) 

#define ISymUnmanagedVariable_GetStartOffset(This,pRetVal)	\
    ( (This)->lpVtbl -> GetStartOffset(This,pRetVal) ) 

#define ISymUnmanagedVariable_GetEndOffset(This,pRetVal)	\
    ( (This)->lpVtbl -> GetEndOffset(This,pRetVal) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ISymUnmanagedVariable_INTERFACE_DEFINED__ */


#ifndef __ISymUnmanagedSymbolSearchInfo_INTERFACE_DEFINED__
#define __ISymUnmanagedSymbolSearchInfo_INTERFACE_DEFINED__

/* interface ISymUnmanagedSymbolSearchInfo */
/* [unique][uuid][object] */ 


EXTERN_C const IID IID_ISymUnmanagedSymbolSearchInfo;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("F8B3534A-A46B-4980-B520-BEC4ACEABA8F")
    ISymUnmanagedSymbolSearchInfo : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE GetSearchPathLength( 
            /* [out] */ __RPC__out ULONG32 *pcchPath) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetSearchPath( 
            /* [in] */ ULONG32 cchPath,
            /* [out] */ __RPC__out ULONG32 *pcchPath,
            /* [length_is][size_is][out] */ __RPC__out_ecount_part(cchPath, *pcchPath) WCHAR szPath[  ]) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetHRESULT( 
            /* [out] */ __RPC__out HRESULT *phr) = 0;
        
    };
    
    
#else 	/* C style interface */

    typedef struct ISymUnmanagedSymbolSearchInfoVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            __RPC__in ISymUnmanagedSymbolSearchInfo * This,
            /* [in] */ __RPC__in REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            __RPC__in ISymUnmanagedSymbolSearchInfo * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            __RPC__in ISymUnmanagedSymbolSearchInfo * This);
        
        HRESULT ( STDMETHODCALLTYPE *GetSearchPathLength )( 
            __RPC__in ISymUnmanagedSymbolSearchInfo * This,
            /* [out] */ __RPC__out ULONG32 *pcchPath);
        
        HRESULT ( STDMETHODCALLTYPE *GetSearchPath )( 
            __RPC__in ISymUnmanagedSymbolSearchInfo * This,
            /* [in] */ ULONG32 cchPath,
            /* [out] */ __RPC__out ULONG32 *pcchPath,
            /* [length_is][size_is][out] */ __RPC__out_ecount_part(cchPath, *pcchPath) WCHAR szPath[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *GetHRESULT )( 
            __RPC__in ISymUnmanagedSymbolSearchInfo * This,
            /* [out] */ __RPC__out HRESULT *phr);
        
        END_INTERFACE
    } ISymUnmanagedSymbolSearchInfoVtbl;

    interface ISymUnmanagedSymbolSearchInfo
    {
        CONST_VTBL struct ISymUnmanagedSymbolSearchInfoVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define ISymUnmanagedSymbolSearchInfo_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define ISymUnmanagedSymbolSearchInfo_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define ISymUnmanagedSymbolSearchInfo_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define ISymUnmanagedSymbolSearchInfo_GetSearchPathLength(This,pcchPath)	\
    ( (This)->lpVtbl -> GetSearchPathLength(This,pcchPath) ) 

#define ISymUnmanagedSymbolSearchInfo_GetSearchPath(This,cchPath,pcchPath,szPath)	\
    ( (This)->lpVtbl -> GetSearchPath(This,cchPath,pcchPath,szPath) ) 

#define ISymUnmanagedSymbolSearchInfo_GetHRESULT(This,phr)	\
    ( (This)->lpVtbl -> GetHRESULT(This,phr) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ISymUnmanagedSymbolSearchInfo_INTERFACE_DEFINED__ */


#ifndef __ISymUnmanagedWriter_INTERFACE_DEFINED__
#define __ISymUnmanagedWriter_INTERFACE_DEFINED__

/* interface ISymUnmanagedWriter */
/* [unique][uuid][object] */ 


EXTERN_C const IID IID_ISymUnmanagedWriter;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("ED14AA72-78E2-4884-84E2-334293AE5214")
    ISymUnmanagedWriter : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE DefineDocument( 
            /* [in] */ __RPC__in const WCHAR *url,
            /* [in] */ __RPC__in const GUID *language,
            /* [in] */ __RPC__in const GUID *languageVendor,
            /* [in] */ __RPC__in const GUID *documentType,
            /* [retval][out] */ __RPC__deref_out_opt ISymUnmanagedDocumentWriter **pRetVal) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE SetUserEntryPoint( 
            /* [in] */ mdMethodDef entryMethod) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE OpenMethod( 
            /* [in] */ mdMethodDef method) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE CloseMethod( void) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE OpenScope( 
            /* [in] */ ULONG32 startOffset,
            /* [retval][out] */ __RPC__out ULONG32 *pRetVal) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE CloseScope( 
            /* [in] */ ULONG32 endOffset) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE SetScopeRange( 
            /* [in] */ ULONG32 scopeID,
            /* [in] */ ULONG32 startOffset,
            /* [in] */ ULONG32 endOffset) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE DefineLocalVariable( 
            /* [in] */ __RPC__in const WCHAR *name,
            /* [in] */ ULONG32 attributes,
            /* [in] */ ULONG32 cSig,
            /* [size_is][in] */ __RPC__in_ecount_full(cSig) unsigned char signature[  ],
            /* [in] */ ULONG32 addrKind,
            /* [in] */ ULONG32 addr1,
            /* [in] */ ULONG32 addr2,
            /* [in] */ ULONG32 addr3,
            /* [in] */ ULONG32 startOffset,
            /* [in] */ ULONG32 endOffset) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE DefineParameter( 
            /* [in] */ __RPC__in const WCHAR *name,
            /* [in] */ ULONG32 attributes,
            /* [in] */ ULONG32 sequence,
            /* [in] */ ULONG32 addrKind,
            /* [in] */ ULONG32 addr1,
            /* [in] */ ULONG32 addr2,
            /* [in] */ ULONG32 addr3) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE DefineField( 
            /* [in] */ mdTypeDef parent,
            /* [in] */ __RPC__in const WCHAR *name,
            /* [in] */ ULONG32 attributes,
            /* [in] */ ULONG32 cSig,
            /* [size_is][in] */ __RPC__in_ecount_full(cSig) unsigned char signature[  ],
            /* [in] */ ULONG32 addrKind,
            /* [in] */ ULONG32 addr1,
            /* [in] */ ULONG32 addr2,
            /* [in] */ ULONG32 addr3) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE DefineGlobalVariable( 
            /* [in] */ __RPC__in const WCHAR *name,
            /* [in] */ ULONG32 attributes,
            /* [in] */ ULONG32 cSig,
            /* [size_is][in] */ __RPC__in_ecount_full(cSig) unsigned char signature[  ],
            /* [in] */ ULONG32 addrKind,
            /* [in] */ ULONG32 addr1,
            /* [in] */ ULONG32 addr2,
            /* [in] */ ULONG32 addr3) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE Close( void) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE SetSymAttribute( 
            /* [in] */ mdToken parent,
            /* [in] */ __RPC__in const WCHAR *name,
            /* [in] */ ULONG32 cData,
            /* [size_is][in] */ __RPC__in_ecount_full(cData) unsigned char data[  ]) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE OpenNamespace( 
            /* [in] */ __RPC__in const WCHAR *name) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE CloseNamespace( void) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE UsingNamespace( 
            /* [in] */ __RPC__in const WCHAR *fullName) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE SetMethodSourceRange( 
            /* [in] */ __RPC__in_opt ISymUnmanagedDocumentWriter *startDoc,
            /* [in] */ ULONG32 startLine,
            /* [in] */ ULONG32 startColumn,
            /* [in] */ __RPC__in_opt ISymUnmanagedDocumentWriter *endDoc,
            /* [in] */ ULONG32 endLine,
            /* [in] */ ULONG32 endColumn) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE Initialize( 
            /* [in] */ __RPC__in_opt IUnknown *emitter,
            /* [in] */ __RPC__in const WCHAR *filename,
            /* [in] */ __RPC__in_opt IStream *pIStream,
            /* [in] */ BOOL fFullBuild) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetDebugInfo( 
            /* [out][in] */ __RPC__inout IMAGE_DEBUG_DIRECTORY *pIDD,
            /* [in] */ DWORD cData,
            /* [out] */ __RPC__out DWORD *pcData,
            /* [length_is][size_is][out] */ __RPC__out_ecount_part(cData, *pcData) BYTE data[  ]) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE DefineSequencePoints( 
            /* [in] */ __RPC__in_opt ISymUnmanagedDocumentWriter *document,
            /* [in] */ ULONG32 spCount,
            /* [size_is][in] */ __RPC__in_ecount_full(spCount) ULONG32 offsets[  ],
            /* [size_is][in] */ __RPC__in_ecount_full(spCount) ULONG32 lines[  ],
            /* [size_is][in] */ __RPC__in_ecount_full(spCount) ULONG32 columns[  ],
            /* [size_is][in] */ __RPC__in_ecount_full(spCount) ULONG32 endLines[  ],
            /* [size_is][in] */ __RPC__in_ecount_full(spCount) ULONG32 endColumns[  ]) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE RemapToken( 
            /* [in] */ mdToken oldToken,
            /* [in] */ mdToken newToken) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE Initialize2( 
            /* [in] */ __RPC__in_opt IUnknown *emitter,
            /* [in] */ __RPC__in const WCHAR *tempfilename,
            /* [in] */ __RPC__in_opt IStream *pIStream,
            /* [in] */ BOOL fFullBuild,
            /* [in] */ __RPC__in const WCHAR *finalfilename) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE DefineConstant( 
            /* [in] */ __RPC__in const WCHAR *name,
            /* [in] */ VARIANT value,
            /* [in] */ ULONG32 cSig,
            /* [size_is][in] */ __RPC__in_ecount_full(cSig) unsigned char signature[  ]) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE Abort( void) = 0;
        
    };
    
    
#else 	/* C style interface */

    typedef struct ISymUnmanagedWriterVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            __RPC__in ISymUnmanagedWriter * This,
            /* [in] */ __RPC__in REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            __RPC__in ISymUnmanagedWriter * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            __RPC__in ISymUnmanagedWriter * This);
        
        HRESULT ( STDMETHODCALLTYPE *DefineDocument )( 
            __RPC__in ISymUnmanagedWriter * This,
            /* [in] */ __RPC__in const WCHAR *url,
            /* [in] */ __RPC__in const GUID *language,
            /* [in] */ __RPC__in const GUID *languageVendor,
            /* [in] */ __RPC__in const GUID *documentType,
            /* [retval][out] */ __RPC__deref_out_opt ISymUnmanagedDocumentWriter **pRetVal);
        
        HRESULT ( STDMETHODCALLTYPE *SetUserEntryPoint )( 
            __RPC__in ISymUnmanagedWriter * This,
            /* [in] */ mdMethodDef entryMethod);
        
        HRESULT ( STDMETHODCALLTYPE *OpenMethod )( 
            __RPC__in ISymUnmanagedWriter * This,
            /* [in] */ mdMethodDef method);
        
        HRESULT ( STDMETHODCALLTYPE *CloseMethod )( 
            __RPC__in ISymUnmanagedWriter * This);
        
        HRESULT ( STDMETHODCALLTYPE *OpenScope )( 
            __RPC__in ISymUnmanagedWriter * This,
            /* [in] */ ULONG32 startOffset,
            /* [retval][out] */ __RPC__out ULONG32 *pRetVal);
        
        HRESULT ( STDMETHODCALLTYPE *CloseScope )( 
            __RPC__in ISymUnmanagedWriter * This,
            /* [in] */ ULONG32 endOffset);
        
        HRESULT ( STDMETHODCALLTYPE *SetScopeRange )( 
            __RPC__in ISymUnmanagedWriter * This,
            /* [in] */ ULONG32 scopeID,
            /* [in] */ ULONG32 startOffset,
            /* [in] */ ULONG32 endOffset);
        
        HRESULT ( STDMETHODCALLTYPE *DefineLocalVariable )( 
            __RPC__in ISymUnmanagedWriter * This,
            /* [in] */ __RPC__in const WCHAR *name,
            /* [in] */ ULONG32 attributes,
            /* [in] */ ULONG32 cSig,
            /* [size_is][in] */ __RPC__in_ecount_full(cSig) unsigned char signature[  ],
            /* [in] */ ULONG32 addrKind,
            /* [in] */ ULONG32 addr1,
            /* [in] */ ULONG32 addr2,
            /* [in] */ ULONG32 addr3,
            /* [in] */ ULONG32 startOffset,
            /* [in] */ ULONG32 endOffset);
        
        HRESULT ( STDMETHODCALLTYPE *DefineParameter )( 
            __RPC__in ISymUnmanagedWriter * This,
            /* [in] */ __RPC__in const WCHAR *name,
            /* [in] */ ULONG32 attributes,
            /* [in] */ ULONG32 sequence,
            /* [in] */ ULONG32 addrKind,
            /* [in] */ ULONG32 addr1,
            /* [in] */ ULONG32 addr2,
            /* [in] */ ULONG32 addr3);
        
        HRESULT ( STDMETHODCALLTYPE *DefineField )( 
            __RPC__in ISymUnmanagedWriter * This,
            /* [in] */ mdTypeDef parent,
            /* [in] */ __RPC__in const WCHAR *name,
            /* [in] */ ULONG32 attributes,
            /* [in] */ ULONG32 cSig,
            /* [size_is][in] */ __RPC__in_ecount_full(cSig) unsigned char signature[  ],
            /* [in] */ ULONG32 addrKind,
            /* [in] */ ULONG32 addr1,
            /* [in] */ ULONG32 addr2,
            /* [in] */ ULONG32 addr3);
        
        HRESULT ( STDMETHODCALLTYPE *DefineGlobalVariable )( 
            __RPC__in ISymUnmanagedWriter * This,
            /* [in] */ __RPC__in const WCHAR *name,
            /* [in] */ ULONG32 attributes,
            /* [in] */ ULONG32 cSig,
            /* [size_is][in] */ __RPC__in_ecount_full(cSig) unsigned char signature[  ],
            /* [in] */ ULONG32 addrKind,
            /* [in] */ ULONG32 addr1,
            /* [in] */ ULONG32 addr2,
            /* [in] */ ULONG32 addr3);
        
        HRESULT ( STDMETHODCALLTYPE *Close )( 
            __RPC__in ISymUnmanagedWriter * This);
        
        HRESULT ( STDMETHODCALLTYPE *SetSymAttribute )( 
            __RPC__in ISymUnmanagedWriter * This,
            /* [in] */ mdToken parent,
            /* [in] */ __RPC__in const WCHAR *name,
            /* [in] */ ULONG32 cData,
            /* [size_is][in] */ __RPC__in_ecount_full(cData) unsigned char data[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *OpenNamespace )( 
            __RPC__in ISymUnmanagedWriter * This,
            /* [in] */ __RPC__in const WCHAR *name);
        
        HRESULT ( STDMETHODCALLTYPE *CloseNamespace )( 
            __RPC__in ISymUnmanagedWriter * This);
        
        HRESULT ( STDMETHODCALLTYPE *UsingNamespace )( 
            __RPC__in ISymUnmanagedWriter * This,
            /* [in] */ __RPC__in const WCHAR *fullName);
        
        HRESULT ( STDMETHODCALLTYPE *SetMethodSourceRange )( 
            __RPC__in ISymUnmanagedWriter * This,
            /* [in] */ __RPC__in_opt ISymUnmanagedDocumentWriter *startDoc,
            /* [in] */ ULONG32 startLine,
            /* [in] */ ULONG32 startColumn,
            /* [in] */ __RPC__in_opt ISymUnmanagedDocumentWriter *endDoc,
            /* [in] */ ULONG32 endLine,
            /* [in] */ ULONG32 endColumn);
        
        HRESULT ( STDMETHODCALLTYPE *Initialize )( 
            __RPC__in ISymUnmanagedWriter * This,
            /* [in] */ __RPC__in_opt IUnknown *emitter,
            /* [in] */ __RPC__in const WCHAR *filename,
            /* [in] */ __RPC__in_opt IStream *pIStream,
            /* [in] */ BOOL fFullBuild);
        
        HRESULT ( STDMETHODCALLTYPE *GetDebugInfo )( 
            __RPC__in ISymUnmanagedWriter * This,
            /* [out][in] */ __RPC__inout IMAGE_DEBUG_DIRECTORY *pIDD,
            /* [in] */ DWORD cData,
            /* [out] */ __RPC__out DWORD *pcData,
            /* [length_is][size_is][out] */ __RPC__out_ecount_part(cData, *pcData) BYTE data[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *DefineSequencePoints )( 
            __RPC__in ISymUnmanagedWriter * This,
            /* [in] */ __RPC__in_opt ISymUnmanagedDocumentWriter *document,
            /* [in] */ ULONG32 spCount,
            /* [size_is][in] */ __RPC__in_ecount_full(spCount) ULONG32 offsets[  ],
            /* [size_is][in] */ __RPC__in_ecount_full(spCount) ULONG32 lines[  ],
            /* [size_is][in] */ __RPC__in_ecount_full(spCount) ULONG32 columns[  ],
            /* [size_is][in] */ __RPC__in_ecount_full(spCount) ULONG32 endLines[  ],
            /* [size_is][in] */ __RPC__in_ecount_full(spCount) ULONG32 endColumns[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *RemapToken )( 
            __RPC__in ISymUnmanagedWriter * This,
            /* [in] */ mdToken oldToken,
            /* [in] */ mdToken newToken);
        
        HRESULT ( STDMETHODCALLTYPE *Initialize2 )( 
            __RPC__in ISymUnmanagedWriter * This,
            /* [in] */ __RPC__in_opt IUnknown *emitter,
            /* [in] */ __RPC__in const WCHAR *tempfilename,
            /* [in] */ __RPC__in_opt IStream *pIStream,
            /* [in] */ BOOL fFullBuild,
            /* [in] */ __RPC__in const WCHAR *finalfilename);
        
        HRESULT ( STDMETHODCALLTYPE *DefineConstant )( 
            __RPC__in ISymUnmanagedWriter * This,
            /* [in] */ __RPC__in const WCHAR *name,
            /* [in] */ VARIANT value,
            /* [in] */ ULONG32 cSig,
            /* [size_is][in] */ __RPC__in_ecount_full(cSig) unsigned char signature[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *Abort )( 
            __RPC__in ISymUnmanagedWriter * This);
        
        END_INTERFACE
    } ISymUnmanagedWriterVtbl;

    interface ISymUnmanagedWriter
    {
        CONST_VTBL struct ISymUnmanagedWriterVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define ISymUnmanagedWriter_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define ISymUnmanagedWriter_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define ISymUnmanagedWriter_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define ISymUnmanagedWriter_DefineDocument(This,url,language,languageVendor,documentType,pRetVal)	\
    ( (This)->lpVtbl -> DefineDocument(This,url,language,languageVendor,documentType,pRetVal) ) 

#define ISymUnmanagedWriter_SetUserEntryPoint(This,entryMethod)	\
    ( (This)->lpVtbl -> SetUserEntryPoint(This,entryMethod) ) 

#define ISymUnmanagedWriter_OpenMethod(This,method)	\
    ( (This)->lpVtbl -> OpenMethod(This,method) ) 

#define ISymUnmanagedWriter_CloseMethod(This)	\
    ( (This)->lpVtbl -> CloseMethod(This) ) 

#define ISymUnmanagedWriter_OpenScope(This,startOffset,pRetVal)	\
    ( (This)->lpVtbl -> OpenScope(This,startOffset,pRetVal) ) 

#define ISymUnmanagedWriter_CloseScope(This,endOffset)	\
    ( (This)->lpVtbl -> CloseScope(This,endOffset) ) 

#define ISymUnmanagedWriter_SetScopeRange(This,scopeID,startOffset,endOffset)	\
    ( (This)->lpVtbl -> SetScopeRange(This,scopeID,startOffset,endOffset) ) 

#define ISymUnmanagedWriter_DefineLocalVariable(This,name,attributes,cSig,signature,addrKind,addr1,addr2,addr3,startOffset,endOffset)	\
    ( (This)->lpVtbl -> DefineLocalVariable(This,name,attributes,cSig,signature,addrKind,addr1,addr2,addr3,startOffset,endOffset) ) 

#define ISymUnmanagedWriter_DefineParameter(This,name,attributes,sequence,addrKind,addr1,addr2,addr3)	\
    ( (This)->lpVtbl -> DefineParameter(This,name,attributes,sequence,addrKind,addr1,addr2,addr3) ) 

#define ISymUnmanagedWriter_DefineField(This,parent,name,attributes,cSig,signature,addrKind,addr1,addr2,addr3)	\
    ( (This)->lpVtbl -> DefineField(This,parent,name,attributes,cSig,signature,addrKind,addr1,addr2,addr3) ) 

#define ISymUnmanagedWriter_DefineGlobalVariable(This,name,attributes,cSig,signature,addrKind,addr1,addr2,addr3)	\
    ( (This)->lpVtbl -> DefineGlobalVariable(This,name,attributes,cSig,signature,addrKind,addr1,addr2,addr3) ) 

#define ISymUnmanagedWriter_Close(This)	\
    ( (This)->lpVtbl -> Close(This) ) 

#define ISymUnmanagedWriter_SetSymAttribute(This,parent,name,cData,data)	\
    ( (This)->lpVtbl -> SetSymAttribute(This,parent,name,cData,data) ) 

#define ISymUnmanagedWriter_OpenNamespace(This,name)	\
    ( (This)->lpVtbl -> OpenNamespace(This,name) ) 

#define ISymUnmanagedWriter_CloseNamespace(This)	\
    ( (This)->lpVtbl -> CloseNamespace(This) ) 

#define ISymUnmanagedWriter_UsingNamespace(This,fullName)	\
    ( (This)->lpVtbl -> UsingNamespace(This,fullName) ) 

#define ISymUnmanagedWriter_SetMethodSourceRange(This,startDoc,startLine,startColumn,endDoc,endLine,endColumn)	\
    ( (This)->lpVtbl -> SetMethodSourceRange(This,startDoc,startLine,startColumn,endDoc,endLine,endColumn) ) 

#define ISymUnmanagedWriter_Initialize(This,emitter,filename,pIStream,fFullBuild)	\
    ( (This)->lpVtbl -> Initialize(This,emitter,filename,pIStream,fFullBuild) ) 

#define ISymUnmanagedWriter_GetDebugInfo(This,pIDD,cData,pcData,data)	\
    ( (This)->lpVtbl -> GetDebugInfo(This,pIDD,cData,pcData,data) ) 

#define ISymUnmanagedWriter_DefineSequencePoints(This,document,spCount,offsets,lines,columns,endLines,endColumns)	\
    ( (This)->lpVtbl -> DefineSequencePoints(This,document,spCount,offsets,lines,columns,endLines,endColumns) ) 

#define ISymUnmanagedWriter_RemapToken(This,oldToken,newToken)	\
    ( (This)->lpVtbl -> RemapToken(This,oldToken,newToken) ) 

#define ISymUnmanagedWriter_Initialize2(This,emitter,tempfilename,pIStream,fFullBuild,finalfilename)	\
    ( (This)->lpVtbl -> Initialize2(This,emitter,tempfilename,pIStream,fFullBuild,finalfilename) ) 

#define ISymUnmanagedWriter_DefineConstant(This,name,value,cSig,signature)	\
    ( (This)->lpVtbl -> DefineConstant(This,name,value,cSig,signature) ) 

#define ISymUnmanagedWriter_Abort(This)	\
    ( (This)->lpVtbl -> Abort(This) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ISymUnmanagedWriter_INTERFACE_DEFINED__ */


#ifndef __ISymUnmanagedWriter2_INTERFACE_DEFINED__
#define __ISymUnmanagedWriter2_INTERFACE_DEFINED__

/* interface ISymUnmanagedWriter2 */
/* [unique][uuid][object] */ 


EXTERN_C const IID IID_ISymUnmanagedWriter2;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("0B97726E-9E6D-4f05-9A26-424022093CAA")
    ISymUnmanagedWriter2 : public ISymUnmanagedWriter
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE DefineLocalVariable2( 
            /* [in] */ __RPC__in const WCHAR *name,
            /* [in] */ ULONG32 attributes,
            /* [in] */ mdSignature sigToken,
            /* [in] */ ULONG32 addrKind,
            /* [in] */ ULONG32 addr1,
            /* [in] */ ULONG32 addr2,
            /* [in] */ ULONG32 addr3,
            /* [in] */ ULONG32 startOffset,
            /* [in] */ ULONG32 endOffset) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE DefineGlobalVariable2( 
            /* [in] */ __RPC__in const WCHAR *name,
            /* [in] */ ULONG32 attributes,
            /* [in] */ mdSignature sigToken,
            /* [in] */ ULONG32 addrKind,
            /* [in] */ ULONG32 addr1,
            /* [in] */ ULONG32 addr2,
            /* [in] */ ULONG32 addr3) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE DefineConstant2( 
            /* [in] */ __RPC__in const WCHAR *name,
            /* [in] */ VARIANT value,
            /* [in] */ mdSignature sigToken) = 0;
        
    };
    
    
#else 	/* C style interface */

    typedef struct ISymUnmanagedWriter2Vtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            __RPC__in ISymUnmanagedWriter2 * This,
            /* [in] */ __RPC__in REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            __RPC__in ISymUnmanagedWriter2 * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            __RPC__in ISymUnmanagedWriter2 * This);
        
        HRESULT ( STDMETHODCALLTYPE *DefineDocument )( 
            __RPC__in ISymUnmanagedWriter2 * This,
            /* [in] */ __RPC__in const WCHAR *url,
            /* [in] */ __RPC__in const GUID *language,
            /* [in] */ __RPC__in const GUID *languageVendor,
            /* [in] */ __RPC__in const GUID *documentType,
            /* [retval][out] */ __RPC__deref_out_opt ISymUnmanagedDocumentWriter **pRetVal);
        
        HRESULT ( STDMETHODCALLTYPE *SetUserEntryPoint )( 
            __RPC__in ISymUnmanagedWriter2 * This,
            /* [in] */ mdMethodDef entryMethod);
        
        HRESULT ( STDMETHODCALLTYPE *OpenMethod )( 
            __RPC__in ISymUnmanagedWriter2 * This,
            /* [in] */ mdMethodDef method);
        
        HRESULT ( STDMETHODCALLTYPE *CloseMethod )( 
            __RPC__in ISymUnmanagedWriter2 * This);
        
        HRESULT ( STDMETHODCALLTYPE *OpenScope )( 
            __RPC__in ISymUnmanagedWriter2 * This,
            /* [in] */ ULONG32 startOffset,
            /* [retval][out] */ __RPC__out ULONG32 *pRetVal);
        
        HRESULT ( STDMETHODCALLTYPE *CloseScope )( 
            __RPC__in ISymUnmanagedWriter2 * This,
            /* [in] */ ULONG32 endOffset);
        
        HRESULT ( STDMETHODCALLTYPE *SetScopeRange )( 
            __RPC__in ISymUnmanagedWriter2 * This,
            /* [in] */ ULONG32 scopeID,
            /* [in] */ ULONG32 startOffset,
            /* [in] */ ULONG32 endOffset);
        
        HRESULT ( STDMETHODCALLTYPE *DefineLocalVariable )( 
            __RPC__in ISymUnmanagedWriter2 * This,
            /* [in] */ __RPC__in const WCHAR *name,
            /* [in] */ ULONG32 attributes,
            /* [in] */ ULONG32 cSig,
            /* [size_is][in] */ __RPC__in_ecount_full(cSig) unsigned char signature[  ],
            /* [in] */ ULONG32 addrKind,
            /* [in] */ ULONG32 addr1,
            /* [in] */ ULONG32 addr2,
            /* [in] */ ULONG32 addr3,
            /* [in] */ ULONG32 startOffset,
            /* [in] */ ULONG32 endOffset);
        
        HRESULT ( STDMETHODCALLTYPE *DefineParameter )( 
            __RPC__in ISymUnmanagedWriter2 * This,
            /* [in] */ __RPC__in const WCHAR *name,
            /* [in] */ ULONG32 attributes,
            /* [in] */ ULONG32 sequence,
            /* [in] */ ULONG32 addrKind,
            /* [in] */ ULONG32 addr1,
            /* [in] */ ULONG32 addr2,
            /* [in] */ ULONG32 addr3);
        
        HRESULT ( STDMETHODCALLTYPE *DefineField )( 
            __RPC__in ISymUnmanagedWriter2 * This,
            /* [in] */ mdTypeDef parent,
            /* [in] */ __RPC__in const WCHAR *name,
            /* [in] */ ULONG32 attributes,
            /* [in] */ ULONG32 cSig,
            /* [size_is][in] */ __RPC__in_ecount_full(cSig) unsigned char signature[  ],
            /* [in] */ ULONG32 addrKind,
            /* [in] */ ULONG32 addr1,
            /* [in] */ ULONG32 addr2,
            /* [in] */ ULONG32 addr3);
        
        HRESULT ( STDMETHODCALLTYPE *DefineGlobalVariable )( 
            __RPC__in ISymUnmanagedWriter2 * This,
            /* [in] */ __RPC__in const WCHAR *name,
            /* [in] */ ULONG32 attributes,
            /* [in] */ ULONG32 cSig,
            /* [size_is][in] */ __RPC__in_ecount_full(cSig) unsigned char signature[  ],
            /* [in] */ ULONG32 addrKind,
            /* [in] */ ULONG32 addr1,
            /* [in] */ ULONG32 addr2,
            /* [in] */ ULONG32 addr3);
        
        HRESULT ( STDMETHODCALLTYPE *Close )( 
            __RPC__in ISymUnmanagedWriter2 * This);
        
        HRESULT ( STDMETHODCALLTYPE *SetSymAttribute )( 
            __RPC__in ISymUnmanagedWriter2 * This,
            /* [in] */ mdToken parent,
            /* [in] */ __RPC__in const WCHAR *name,
            /* [in] */ ULONG32 cData,
            /* [size_is][in] */ __RPC__in_ecount_full(cData) unsigned char data[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *OpenNamespace )( 
            __RPC__in ISymUnmanagedWriter2 * This,
            /* [in] */ __RPC__in const WCHAR *name);
        
        HRESULT ( STDMETHODCALLTYPE *CloseNamespace )( 
            __RPC__in ISymUnmanagedWriter2 * This);
        
        HRESULT ( STDMETHODCALLTYPE *UsingNamespace )( 
            __RPC__in ISymUnmanagedWriter2 * This,
            /* [in] */ __RPC__in const WCHAR *fullName);
        
        HRESULT ( STDMETHODCALLTYPE *SetMethodSourceRange )( 
            __RPC__in ISymUnmanagedWriter2 * This,
            /* [in] */ __RPC__in_opt ISymUnmanagedDocumentWriter *startDoc,
            /* [in] */ ULONG32 startLine,
            /* [in] */ ULONG32 startColumn,
            /* [in] */ __RPC__in_opt ISymUnmanagedDocumentWriter *endDoc,
            /* [in] */ ULONG32 endLine,
            /* [in] */ ULONG32 endColumn);
        
        HRESULT ( STDMETHODCALLTYPE *Initialize )( 
            __RPC__in ISymUnmanagedWriter2 * This,
            /* [in] */ __RPC__in_opt IUnknown *emitter,
            /* [in] */ __RPC__in const WCHAR *filename,
            /* [in] */ __RPC__in_opt IStream *pIStream,
            /* [in] */ BOOL fFullBuild);
        
        HRESULT ( STDMETHODCALLTYPE *GetDebugInfo )( 
            __RPC__in ISymUnmanagedWriter2 * This,
            /* [out][in] */ __RPC__inout IMAGE_DEBUG_DIRECTORY *pIDD,
            /* [in] */ DWORD cData,
            /* [out] */ __RPC__out DWORD *pcData,
            /* [length_is][size_is][out] */ __RPC__out_ecount_part(cData, *pcData) BYTE data[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *DefineSequencePoints )( 
            __RPC__in ISymUnmanagedWriter2 * This,
            /* [in] */ __RPC__in_opt ISymUnmanagedDocumentWriter *document,
            /* [in] */ ULONG32 spCount,
            /* [size_is][in] */ __RPC__in_ecount_full(spCount) ULONG32 offsets[  ],
            /* [size_is][in] */ __RPC__in_ecount_full(spCount) ULONG32 lines[  ],
            /* [size_is][in] */ __RPC__in_ecount_full(spCount) ULONG32 columns[  ],
            /* [size_is][in] */ __RPC__in_ecount_full(spCount) ULONG32 endLines[  ],
            /* [size_is][in] */ __RPC__in_ecount_full(spCount) ULONG32 endColumns[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *RemapToken )( 
            __RPC__in ISymUnmanagedWriter2 * This,
            /* [in] */ mdToken oldToken,
            /* [in] */ mdToken newToken);
        
        HRESULT ( STDMETHODCALLTYPE *Initialize2 )( 
            __RPC__in ISymUnmanagedWriter2 * This,
            /* [in] */ __RPC__in_opt IUnknown *emitter,
            /* [in] */ __RPC__in const WCHAR *tempfilename,
            /* [in] */ __RPC__in_opt IStream *pIStream,
            /* [in] */ BOOL fFullBuild,
            /* [in] */ __RPC__in const WCHAR *finalfilename);
        
        HRESULT ( STDMETHODCALLTYPE *DefineConstant )( 
            __RPC__in ISymUnmanagedWriter2 * This,
            /* [in] */ __RPC__in const WCHAR *name,
            /* [in] */ VARIANT value,
            /* [in] */ ULONG32 cSig,
            /* [size_is][in] */ __RPC__in_ecount_full(cSig) unsigned char signature[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *Abort )( 
            __RPC__in ISymUnmanagedWriter2 * This);
        
        HRESULT ( STDMETHODCALLTYPE *DefineLocalVariable2 )( 
            __RPC__in ISymUnmanagedWriter2 * This,
            /* [in] */ __RPC__in const WCHAR *name,
            /* [in] */ ULONG32 attributes,
            /* [in] */ mdSignature sigToken,
            /* [in] */ ULONG32 addrKind,
            /* [in] */ ULONG32 addr1,
            /* [in] */ ULONG32 addr2,
            /* [in] */ ULONG32 addr3,
            /* [in] */ ULONG32 startOffset,
            /* [in] */ ULONG32 endOffset);
        
        HRESULT ( STDMETHODCALLTYPE *DefineGlobalVariable2 )( 
            __RPC__in ISymUnmanagedWriter2 * This,
            /* [in] */ __RPC__in const WCHAR *name,
            /* [in] */ ULONG32 attributes,
            /* [in] */ mdSignature sigToken,
            /* [in] */ ULONG32 addrKind,
            /* [in] */ ULONG32 addr1,
            /* [in] */ ULONG32 addr2,
            /* [in] */ ULONG32 addr3);
        
        HRESULT ( STDMETHODCALLTYPE *DefineConstant2 )( 
            __RPC__in ISymUnmanagedWriter2 * This,
            /* [in] */ __RPC__in const WCHAR *name,
            /* [in] */ VARIANT value,
            /* [in] */ mdSignature sigToken);
        
        END_INTERFACE
    } ISymUnmanagedWriter2Vtbl;

    interface ISymUnmanagedWriter2
    {
        CONST_VTBL struct ISymUnmanagedWriter2Vtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define ISymUnmanagedWriter2_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define ISymUnmanagedWriter2_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define ISymUnmanagedWriter2_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define ISymUnmanagedWriter2_DefineDocument(This,url,language,languageVendor,documentType,pRetVal)	\
    ( (This)->lpVtbl -> DefineDocument(This,url,language,languageVendor,documentType,pRetVal) ) 

#define ISymUnmanagedWriter2_SetUserEntryPoint(This,entryMethod)	\
    ( (This)->lpVtbl -> SetUserEntryPoint(This,entryMethod) ) 

#define ISymUnmanagedWriter2_OpenMethod(This,method)	\
    ( (This)->lpVtbl -> OpenMethod(This,method) ) 

#define ISymUnmanagedWriter2_CloseMethod(This)	\
    ( (This)->lpVtbl -> CloseMethod(This) ) 

#define ISymUnmanagedWriter2_OpenScope(This,startOffset,pRetVal)	\
    ( (This)->lpVtbl -> OpenScope(This,startOffset,pRetVal) ) 

#define ISymUnmanagedWriter2_CloseScope(This,endOffset)	\
    ( (This)->lpVtbl -> CloseScope(This,endOffset) ) 

#define ISymUnmanagedWriter2_SetScopeRange(This,scopeID,startOffset,endOffset)	\
    ( (This)->lpVtbl -> SetScopeRange(This,scopeID,startOffset,endOffset) ) 

#define ISymUnmanagedWriter2_DefineLocalVariable(This,name,attributes,cSig,signature,addrKind,addr1,addr2,addr3,startOffset,endOffset)	\
    ( (This)->lpVtbl -> DefineLocalVariable(This,name,attributes,cSig,signature,addrKind,addr1,addr2,addr3,startOffset,endOffset) ) 

#define ISymUnmanagedWriter2_DefineParameter(This,name,attributes,sequence,addrKind,addr1,addr2,addr3)	\
    ( (This)->lpVtbl -> DefineParameter(This,name,attributes,sequence,addrKind,addr1,addr2,addr3) ) 

#define ISymUnmanagedWriter2_DefineField(This,parent,name,attributes,cSig,signature,addrKind,addr1,addr2,addr3)	\
    ( (This)->lpVtbl -> DefineField(This,parent,name,attributes,cSig,signature,addrKind,addr1,addr2,addr3) ) 

#define ISymUnmanagedWriter2_DefineGlobalVariable(This,name,attributes,cSig,signature,addrKind,addr1,addr2,addr3)	\
    ( (This)->lpVtbl -> DefineGlobalVariable(This,name,attributes,cSig,signature,addrKind,addr1,addr2,addr3) ) 

#define ISymUnmanagedWriter2_Close(This)	\
    ( (This)->lpVtbl -> Close(This) ) 

#define ISymUnmanagedWriter2_SetSymAttribute(This,parent,name,cData,data)	\
    ( (This)->lpVtbl -> SetSymAttribute(This,parent,name,cData,data) ) 

#define ISymUnmanagedWriter2_OpenNamespace(This,name)	\
    ( (This)->lpVtbl -> OpenNamespace(This,name) ) 

#define ISymUnmanagedWriter2_CloseNamespace(This)	\
    ( (This)->lpVtbl -> CloseNamespace(This) ) 

#define ISymUnmanagedWriter2_UsingNamespace(This,fullName)	\
    ( (This)->lpVtbl -> UsingNamespace(This,fullName) ) 

#define ISymUnmanagedWriter2_SetMethodSourceRange(This,startDoc,startLine,startColumn,endDoc,endLine,endColumn)	\
    ( (This)->lpVtbl -> SetMethodSourceRange(This,startDoc,startLine,startColumn,endDoc,endLine,endColumn) ) 

#define ISymUnmanagedWriter2_Initialize(This,emitter,filename,pIStream,fFullBuild)	\
    ( (This)->lpVtbl -> Initialize(This,emitter,filename,pIStream,fFullBuild) ) 

#define ISymUnmanagedWriter2_GetDebugInfo(This,pIDD,cData,pcData,data)	\
    ( (This)->lpVtbl -> GetDebugInfo(This,pIDD,cData,pcData,data) ) 

#define ISymUnmanagedWriter2_DefineSequencePoints(This,document,spCount,offsets,lines,columns,endLines,endColumns)	\
    ( (This)->lpVtbl -> DefineSequencePoints(This,document,spCount,offsets,lines,columns,endLines,endColumns) ) 

#define ISymUnmanagedWriter2_RemapToken(This,oldToken,newToken)	\
    ( (This)->lpVtbl -> RemapToken(This,oldToken,newToken) ) 

#define ISymUnmanagedWriter2_Initialize2(This,emitter,tempfilename,pIStream,fFullBuild,finalfilename)	\
    ( (This)->lpVtbl -> Initialize2(This,emitter,tempfilename,pIStream,fFullBuild,finalfilename) ) 

#define ISymUnmanagedWriter2_DefineConstant(This,name,value,cSig,signature)	\
    ( (This)->lpVtbl -> DefineConstant(This,name,value,cSig,signature) ) 

#define ISymUnmanagedWriter2_Abort(This)	\
    ( (This)->lpVtbl -> Abort(This) ) 


#define ISymUnmanagedWriter2_DefineLocalVariable2(This,name,attributes,sigToken,addrKind,addr1,addr2,addr3,startOffset,endOffset)	\
    ( (This)->lpVtbl -> DefineLocalVariable2(This,name,attributes,sigToken,addrKind,addr1,addr2,addr3,startOffset,endOffset) ) 

#define ISymUnmanagedWriter2_DefineGlobalVariable2(This,name,attributes,sigToken,addrKind,addr1,addr2,addr3)	\
    ( (This)->lpVtbl -> DefineGlobalVariable2(This,name,attributes,sigToken,addrKind,addr1,addr2,addr3) ) 

#define ISymUnmanagedWriter2_DefineConstant2(This,name,value,sigToken)	\
    ( (This)->lpVtbl -> DefineConstant2(This,name,value,sigToken) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ISymUnmanagedWriter2_INTERFACE_DEFINED__ */


#ifndef __ISymUnmanagedWriter3_INTERFACE_DEFINED__
#define __ISymUnmanagedWriter3_INTERFACE_DEFINED__

/* interface ISymUnmanagedWriter3 */
/* [unique][uuid][object] */ 


EXTERN_C const IID IID_ISymUnmanagedWriter3;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("12F1E02C-1E05-4B0E-9468-EBC9D1BB040F")
    ISymUnmanagedWriter3 : public ISymUnmanagedWriter2
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE OpenMethod2( 
            /* [in] */ mdMethodDef method,
            /* [in] */ ULONG32 isect,
            /* [in] */ ULONG32 offset) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE Commit( void) = 0;
        
    };
    
    
#else 	/* C style interface */

    typedef struct ISymUnmanagedWriter3Vtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            __RPC__in ISymUnmanagedWriter3 * This,
            /* [in] */ __RPC__in REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            __RPC__in ISymUnmanagedWriter3 * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            __RPC__in ISymUnmanagedWriter3 * This);
        
        HRESULT ( STDMETHODCALLTYPE *DefineDocument )( 
            __RPC__in ISymUnmanagedWriter3 * This,
            /* [in] */ __RPC__in const WCHAR *url,
            /* [in] */ __RPC__in const GUID *language,
            /* [in] */ __RPC__in const GUID *languageVendor,
            /* [in] */ __RPC__in const GUID *documentType,
            /* [retval][out] */ __RPC__deref_out_opt ISymUnmanagedDocumentWriter **pRetVal);
        
        HRESULT ( STDMETHODCALLTYPE *SetUserEntryPoint )( 
            __RPC__in ISymUnmanagedWriter3 * This,
            /* [in] */ mdMethodDef entryMethod);
        
        HRESULT ( STDMETHODCALLTYPE *OpenMethod )( 
            __RPC__in ISymUnmanagedWriter3 * This,
            /* [in] */ mdMethodDef method);
        
        HRESULT ( STDMETHODCALLTYPE *CloseMethod )( 
            __RPC__in ISymUnmanagedWriter3 * This);
        
        HRESULT ( STDMETHODCALLTYPE *OpenScope )( 
            __RPC__in ISymUnmanagedWriter3 * This,
            /* [in] */ ULONG32 startOffset,
            /* [retval][out] */ __RPC__out ULONG32 *pRetVal);
        
        HRESULT ( STDMETHODCALLTYPE *CloseScope )( 
            __RPC__in ISymUnmanagedWriter3 * This,
            /* [in] */ ULONG32 endOffset);
        
        HRESULT ( STDMETHODCALLTYPE *SetScopeRange )( 
            __RPC__in ISymUnmanagedWriter3 * This,
            /* [in] */ ULONG32 scopeID,
            /* [in] */ ULONG32 startOffset,
            /* [in] */ ULONG32 endOffset);
        
        HRESULT ( STDMETHODCALLTYPE *DefineLocalVariable )( 
            __RPC__in ISymUnmanagedWriter3 * This,
            /* [in] */ __RPC__in const WCHAR *name,
            /* [in] */ ULONG32 attributes,
            /* [in] */ ULONG32 cSig,
            /* [size_is][in] */ __RPC__in_ecount_full(cSig) unsigned char signature[  ],
            /* [in] */ ULONG32 addrKind,
            /* [in] */ ULONG32 addr1,
            /* [in] */ ULONG32 addr2,
            /* [in] */ ULONG32 addr3,
            /* [in] */ ULONG32 startOffset,
            /* [in] */ ULONG32 endOffset);
        
        HRESULT ( STDMETHODCALLTYPE *DefineParameter )( 
            __RPC__in ISymUnmanagedWriter3 * This,
            /* [in] */ __RPC__in const WCHAR *name,
            /* [in] */ ULONG32 attributes,
            /* [in] */ ULONG32 sequence,
            /* [in] */ ULONG32 addrKind,
            /* [in] */ ULONG32 addr1,
            /* [in] */ ULONG32 addr2,
            /* [in] */ ULONG32 addr3);
        
        HRESULT ( STDMETHODCALLTYPE *DefineField )( 
            __RPC__in ISymUnmanagedWriter3 * This,
            /* [in] */ mdTypeDef parent,
            /* [in] */ __RPC__in const WCHAR *name,
            /* [in] */ ULONG32 attributes,
            /* [in] */ ULONG32 cSig,
            /* [size_is][in] */ __RPC__in_ecount_full(cSig) unsigned char signature[  ],
            /* [in] */ ULONG32 addrKind,
            /* [in] */ ULONG32 addr1,
            /* [in] */ ULONG32 addr2,
            /* [in] */ ULONG32 addr3);
        
        HRESULT ( STDMETHODCALLTYPE *DefineGlobalVariable )( 
            __RPC__in ISymUnmanagedWriter3 * This,
            /* [in] */ __RPC__in const WCHAR *name,
            /* [in] */ ULONG32 attributes,
            /* [in] */ ULONG32 cSig,
            /* [size_is][in] */ __RPC__in_ecount_full(cSig) unsigned char signature[  ],
            /* [in] */ ULONG32 addrKind,
            /* [in] */ ULONG32 addr1,
            /* [in] */ ULONG32 addr2,
            /* [in] */ ULONG32 addr3);
        
        HRESULT ( STDMETHODCALLTYPE *Close )( 
            __RPC__in ISymUnmanagedWriter3 * This);
        
        HRESULT ( STDMETHODCALLTYPE *SetSymAttribute )( 
            __RPC__in ISymUnmanagedWriter3 * This,
            /* [in] */ mdToken parent,
            /* [in] */ __RPC__in const WCHAR *name,
            /* [in] */ ULONG32 cData,
            /* [size_is][in] */ __RPC__in_ecount_full(cData) unsigned char data[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *OpenNamespace )( 
            __RPC__in ISymUnmanagedWriter3 * This,
            /* [in] */ __RPC__in const WCHAR *name);
        
        HRESULT ( STDMETHODCALLTYPE *CloseNamespace )( 
            __RPC__in ISymUnmanagedWriter3 * This);
        
        HRESULT ( STDMETHODCALLTYPE *UsingNamespace )( 
            __RPC__in ISymUnmanagedWriter3 * This,
            /* [in] */ __RPC__in const WCHAR *fullName);
        
        HRESULT ( STDMETHODCALLTYPE *SetMethodSourceRange )( 
            __RPC__in ISymUnmanagedWriter3 * This,
            /* [in] */ __RPC__in_opt ISymUnmanagedDocumentWriter *startDoc,
            /* [in] */ ULONG32 startLine,
            /* [in] */ ULONG32 startColumn,
            /* [in] */ __RPC__in_opt ISymUnmanagedDocumentWriter *endDoc,
            /* [in] */ ULONG32 endLine,
            /* [in] */ ULONG32 endColumn);
        
        HRESULT ( STDMETHODCALLTYPE *Initialize )( 
            __RPC__in ISymUnmanagedWriter3 * This,
            /* [in] */ __RPC__in_opt IUnknown *emitter,
            /* [in] */ __RPC__in const WCHAR *filename,
            /* [in] */ __RPC__in_opt IStream *pIStream,
            /* [in] */ BOOL fFullBuild);
        
        HRESULT ( STDMETHODCALLTYPE *GetDebugInfo )( 
            __RPC__in ISymUnmanagedWriter3 * This,
            /* [out][in] */ __RPC__inout IMAGE_DEBUG_DIRECTORY *pIDD,
            /* [in] */ DWORD cData,
            /* [out] */ __RPC__out DWORD *pcData,
            /* [length_is][size_is][out] */ __RPC__out_ecount_part(cData, *pcData) BYTE data[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *DefineSequencePoints )( 
            __RPC__in ISymUnmanagedWriter3 * This,
            /* [in] */ __RPC__in_opt ISymUnmanagedDocumentWriter *document,
            /* [in] */ ULONG32 spCount,
            /* [size_is][in] */ __RPC__in_ecount_full(spCount) ULONG32 offsets[  ],
            /* [size_is][in] */ __RPC__in_ecount_full(spCount) ULONG32 lines[  ],
            /* [size_is][in] */ __RPC__in_ecount_full(spCount) ULONG32 columns[  ],
            /* [size_is][in] */ __RPC__in_ecount_full(spCount) ULONG32 endLines[  ],
            /* [size_is][in] */ __RPC__in_ecount_full(spCount) ULONG32 endColumns[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *RemapToken )( 
            __RPC__in ISymUnmanagedWriter3 * This,
            /* [in] */ mdToken oldToken,
            /* [in] */ mdToken newToken);
        
        HRESULT ( STDMETHODCALLTYPE *Initialize2 )( 
            __RPC__in ISymUnmanagedWriter3 * This,
            /* [in] */ __RPC__in_opt IUnknown *emitter,
            /* [in] */ __RPC__in const WCHAR *tempfilename,
            /* [in] */ __RPC__in_opt IStream *pIStream,
            /* [in] */ BOOL fFullBuild,
            /* [in] */ __RPC__in const WCHAR *finalfilename);
        
        HRESULT ( STDMETHODCALLTYPE *DefineConstant )( 
            __RPC__in ISymUnmanagedWriter3 * This,
            /* [in] */ __RPC__in const WCHAR *name,
            /* [in] */ VARIANT value,
            /* [in] */ ULONG32 cSig,
            /* [size_is][in] */ __RPC__in_ecount_full(cSig) unsigned char signature[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *Abort )( 
            __RPC__in ISymUnmanagedWriter3 * This);
        
        HRESULT ( STDMETHODCALLTYPE *DefineLocalVariable2 )( 
            __RPC__in ISymUnmanagedWriter3 * This,
            /* [in] */ __RPC__in const WCHAR *name,
            /* [in] */ ULONG32 attributes,
            /* [in] */ mdSignature sigToken,
            /* [in] */ ULONG32 addrKind,
            /* [in] */ ULONG32 addr1,
            /* [in] */ ULONG32 addr2,
            /* [in] */ ULONG32 addr3,
            /* [in] */ ULONG32 startOffset,
            /* [in] */ ULONG32 endOffset);
        
        HRESULT ( STDMETHODCALLTYPE *DefineGlobalVariable2 )( 
            __RPC__in ISymUnmanagedWriter3 * This,
            /* [in] */ __RPC__in const WCHAR *name,
            /* [in] */ ULONG32 attributes,
            /* [in] */ mdSignature sigToken,
            /* [in] */ ULONG32 addrKind,
            /* [in] */ ULONG32 addr1,
            /* [in] */ ULONG32 addr2,
            /* [in] */ ULONG32 addr3);
        
        HRESULT ( STDMETHODCALLTYPE *DefineConstant2 )( 
            __RPC__in ISymUnmanagedWriter3 * This,
            /* [in] */ __RPC__in const WCHAR *name,
            /* [in] */ VARIANT value,
            /* [in] */ mdSignature sigToken);
        
        HRESULT ( STDMETHODCALLTYPE *OpenMethod2 )( 
            __RPC__in ISymUnmanagedWriter3 * This,
            /* [in] */ mdMethodDef method,
            /* [in] */ ULONG32 isect,
            /* [in] */ ULONG32 offset);
        
        HRESULT ( STDMETHODCALLTYPE *Commit )( 
            __RPC__in ISymUnmanagedWriter3 * This);
        
        END_INTERFACE
    } ISymUnmanagedWriter3Vtbl;

    interface ISymUnmanagedWriter3
    {
        CONST_VTBL struct ISymUnmanagedWriter3Vtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define ISymUnmanagedWriter3_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define ISymUnmanagedWriter3_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define ISymUnmanagedWriter3_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define ISymUnmanagedWriter3_DefineDocument(This,url,language,languageVendor,documentType,pRetVal)	\
    ( (This)->lpVtbl -> DefineDocument(This,url,language,languageVendor,documentType,pRetVal) ) 

#define ISymUnmanagedWriter3_SetUserEntryPoint(This,entryMethod)	\
    ( (This)->lpVtbl -> SetUserEntryPoint(This,entryMethod) ) 

#define ISymUnmanagedWriter3_OpenMethod(This,method)	\
    ( (This)->lpVtbl -> OpenMethod(This,method) ) 

#define ISymUnmanagedWriter3_CloseMethod(This)	\
    ( (This)->lpVtbl -> CloseMethod(This) ) 

#define ISymUnmanagedWriter3_OpenScope(This,startOffset,pRetVal)	\
    ( (This)->lpVtbl -> OpenScope(This,startOffset,pRetVal) ) 

#define ISymUnmanagedWriter3_CloseScope(This,endOffset)	\
    ( (This)->lpVtbl -> CloseScope(This,endOffset) ) 

#define ISymUnmanagedWriter3_SetScopeRange(This,scopeID,startOffset,endOffset)	\
    ( (This)->lpVtbl -> SetScopeRange(This,scopeID,startOffset,endOffset) ) 

#define ISymUnmanagedWriter3_DefineLocalVariable(This,name,attributes,cSig,signature,addrKind,addr1,addr2,addr3,startOffset,endOffset)	\
    ( (This)->lpVtbl -> DefineLocalVariable(This,name,attributes,cSig,signature,addrKind,addr1,addr2,addr3,startOffset,endOffset) ) 

#define ISymUnmanagedWriter3_DefineParameter(This,name,attributes,sequence,addrKind,addr1,addr2,addr3)	\
    ( (This)->lpVtbl -> DefineParameter(This,name,attributes,sequence,addrKind,addr1,addr2,addr3) ) 

#define ISymUnmanagedWriter3_DefineField(This,parent,name,attributes,cSig,signature,addrKind,addr1,addr2,addr3)	\
    ( (This)->lpVtbl -> DefineField(This,parent,name,attributes,cSig,signature,addrKind,addr1,addr2,addr3) ) 

#define ISymUnmanagedWriter3_DefineGlobalVariable(This,name,attributes,cSig,signature,addrKind,addr1,addr2,addr3)	\
    ( (This)->lpVtbl -> DefineGlobalVariable(This,name,attributes,cSig,signature,addrKind,addr1,addr2,addr3) ) 

#define ISymUnmanagedWriter3_Close(This)	\
    ( (This)->lpVtbl -> Close(This) ) 

#define ISymUnmanagedWriter3_SetSymAttribute(This,parent,name,cData,data)	\
    ( (This)->lpVtbl -> SetSymAttribute(This,parent,name,cData,data) ) 

#define ISymUnmanagedWriter3_OpenNamespace(This,name)	\
    ( (This)->lpVtbl -> OpenNamespace(This,name) ) 

#define ISymUnmanagedWriter3_CloseNamespace(This)	\
    ( (This)->lpVtbl -> CloseNamespace(This) ) 

#define ISymUnmanagedWriter3_UsingNamespace(This,fullName)	\
    ( (This)->lpVtbl -> UsingNamespace(This,fullName) ) 

#define ISymUnmanagedWriter3_SetMethodSourceRange(This,startDoc,startLine,startColumn,endDoc,endLine,endColumn)	\
    ( (This)->lpVtbl -> SetMethodSourceRange(This,startDoc,startLine,startColumn,endDoc,endLine,endColumn) ) 

#define ISymUnmanagedWriter3_Initialize(This,emitter,filename,pIStream,fFullBuild)	\
    ( (This)->lpVtbl -> Initialize(This,emitter,filename,pIStream,fFullBuild) ) 

#define ISymUnmanagedWriter3_GetDebugInfo(This,pIDD,cData,pcData,data)	\
    ( (This)->lpVtbl -> GetDebugInfo(This,pIDD,cData,pcData,data) ) 

#define ISymUnmanagedWriter3_DefineSequencePoints(This,document,spCount,offsets,lines,columns,endLines,endColumns)	\
    ( (This)->lpVtbl -> DefineSequencePoints(This,document,spCount,offsets,lines,columns,endLines,endColumns) ) 

#define ISymUnmanagedWriter3_RemapToken(This,oldToken,newToken)	\
    ( (This)->lpVtbl -> RemapToken(This,oldToken,newToken) ) 

#define ISymUnmanagedWriter3_Initialize2(This,emitter,tempfilename,pIStream,fFullBuild,finalfilename)	\
    ( (This)->lpVtbl -> Initialize2(This,emitter,tempfilename,pIStream,fFullBuild,finalfilename) ) 

#define ISymUnmanagedWriter3_DefineConstant(This,name,value,cSig,signature)	\
    ( (This)->lpVtbl -> DefineConstant(This,name,value,cSig,signature) ) 

#define ISymUnmanagedWriter3_Abort(This)	\
    ( (This)->lpVtbl -> Abort(This) ) 


#define ISymUnmanagedWriter3_DefineLocalVariable2(This,name,attributes,sigToken,addrKind,addr1,addr2,addr3,startOffset,endOffset)	\
    ( (This)->lpVtbl -> DefineLocalVariable2(This,name,attributes,sigToken,addrKind,addr1,addr2,addr3,startOffset,endOffset) ) 

#define ISymUnmanagedWriter3_DefineGlobalVariable2(This,name,attributes,sigToken,addrKind,addr1,addr2,addr3)	\
    ( (This)->lpVtbl -> DefineGlobalVariable2(This,name,attributes,sigToken,addrKind,addr1,addr2,addr3) ) 

#define ISymUnmanagedWriter3_DefineConstant2(This,name,value,sigToken)	\
    ( (This)->lpVtbl -> DefineConstant2(This,name,value,sigToken) ) 


#define ISymUnmanagedWriter3_OpenMethod2(This,method,isect,offset)	\
    ( (This)->lpVtbl -> OpenMethod2(This,method,isect,offset) ) 

#define ISymUnmanagedWriter3_Commit(This)	\
    ( (This)->lpVtbl -> Commit(This) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ISymUnmanagedWriter3_INTERFACE_DEFINED__ */


#ifndef __ISymUnmanagedWriter4_INTERFACE_DEFINED__
#define __ISymUnmanagedWriter4_INTERFACE_DEFINED__

/* interface ISymUnmanagedWriter4 */
/* [unique][uuid][object] */ 


EXTERN_C const IID IID_ISymUnmanagedWriter4;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("BC7E3F53-F458-4C23-9DBD-A189E6E96594")
    ISymUnmanagedWriter4 : public ISymUnmanagedWriter3
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE GetDebugInfoWithPadding( 
            /* [out][in] */ __RPC__inout IMAGE_DEBUG_DIRECTORY *pIDD,
            /* [in] */ DWORD cData,
            /* [out] */ __RPC__out DWORD *pcData,
            /* [length_is][size_is][out] */ __RPC__out_ecount_part(cData, *pcData) BYTE data[  ]) = 0;
        
    };
    
    
#else 	/* C style interface */

    typedef struct ISymUnmanagedWriter4Vtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            __RPC__in ISymUnmanagedWriter4 * This,
            /* [in] */ __RPC__in REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            __RPC__in ISymUnmanagedWriter4 * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            __RPC__in ISymUnmanagedWriter4 * This);
        
        HRESULT ( STDMETHODCALLTYPE *DefineDocument )( 
            __RPC__in ISymUnmanagedWriter4 * This,
            /* [in] */ __RPC__in const WCHAR *url,
            /* [in] */ __RPC__in const GUID *language,
            /* [in] */ __RPC__in const GUID *languageVendor,
            /* [in] */ __RPC__in const GUID *documentType,
            /* [retval][out] */ __RPC__deref_out_opt ISymUnmanagedDocumentWriter **pRetVal);
        
        HRESULT ( STDMETHODCALLTYPE *SetUserEntryPoint )( 
            __RPC__in ISymUnmanagedWriter4 * This,
            /* [in] */ mdMethodDef entryMethod);
        
        HRESULT ( STDMETHODCALLTYPE *OpenMethod )( 
            __RPC__in ISymUnmanagedWriter4 * This,
            /* [in] */ mdMethodDef method);
        
        HRESULT ( STDMETHODCALLTYPE *CloseMethod )( 
            __RPC__in ISymUnmanagedWriter4 * This);
        
        HRESULT ( STDMETHODCALLTYPE *OpenScope )( 
            __RPC__in ISymUnmanagedWriter4 * This,
            /* [in] */ ULONG32 startOffset,
            /* [retval][out] */ __RPC__out ULONG32 *pRetVal);
        
        HRESULT ( STDMETHODCALLTYPE *CloseScope )( 
            __RPC__in ISymUnmanagedWriter4 * This,
            /* [in] */ ULONG32 endOffset);
        
        HRESULT ( STDMETHODCALLTYPE *SetScopeRange )( 
            __RPC__in ISymUnmanagedWriter4 * This,
            /* [in] */ ULONG32 scopeID,
            /* [in] */ ULONG32 startOffset,
            /* [in] */ ULONG32 endOffset);
        
        HRESULT ( STDMETHODCALLTYPE *DefineLocalVariable )( 
            __RPC__in ISymUnmanagedWriter4 * This,
            /* [in] */ __RPC__in const WCHAR *name,
            /* [in] */ ULONG32 attributes,
            /* [in] */ ULONG32 cSig,
            /* [size_is][in] */ __RPC__in_ecount_full(cSig) unsigned char signature[  ],
            /* [in] */ ULONG32 addrKind,
            /* [in] */ ULONG32 addr1,
            /* [in] */ ULONG32 addr2,
            /* [in] */ ULONG32 addr3,
            /* [in] */ ULONG32 startOffset,
            /* [in] */ ULONG32 endOffset);
        
        HRESULT ( STDMETHODCALLTYPE *DefineParameter )( 
            __RPC__in ISymUnmanagedWriter4 * This,
            /* [in] */ __RPC__in const WCHAR *name,
            /* [in] */ ULONG32 attributes,
            /* [in] */ ULONG32 sequence,
            /* [in] */ ULONG32 addrKind,
            /* [in] */ ULONG32 addr1,
            /* [in] */ ULONG32 addr2,
            /* [in] */ ULONG32 addr3);
        
        HRESULT ( STDMETHODCALLTYPE *DefineField )( 
            __RPC__in ISymUnmanagedWriter4 * This,
            /* [in] */ mdTypeDef parent,
            /* [in] */ __RPC__in const WCHAR *name,
            /* [in] */ ULONG32 attributes,
            /* [in] */ ULONG32 cSig,
            /* [size_is][in] */ __RPC__in_ecount_full(cSig) unsigned char signature[  ],
            /* [in] */ ULONG32 addrKind,
            /* [in] */ ULONG32 addr1,
            /* [in] */ ULONG32 addr2,
            /* [in] */ ULONG32 addr3);
        
        HRESULT ( STDMETHODCALLTYPE *DefineGlobalVariable )( 
            __RPC__in ISymUnmanagedWriter4 * This,
            /* [in] */ __RPC__in const WCHAR *name,
            /* [in] */ ULONG32 attributes,
            /* [in] */ ULONG32 cSig,
            /* [size_is][in] */ __RPC__in_ecount_full(cSig) unsigned char signature[  ],
            /* [in] */ ULONG32 addrKind,
            /* [in] */ ULONG32 addr1,
            /* [in] */ ULONG32 addr2,
            /* [in] */ ULONG32 addr3);
        
        HRESULT ( STDMETHODCALLTYPE *Close )( 
            __RPC__in ISymUnmanagedWriter4 * This);
        
        HRESULT ( STDMETHODCALLTYPE *SetSymAttribute )( 
            __RPC__in ISymUnmanagedWriter4 * This,
            /* [in] */ mdToken parent,
            /* [in] */ __RPC__in const WCHAR *name,
            /* [in] */ ULONG32 cData,
            /* [size_is][in] */ __RPC__in_ecount_full(cData) unsigned char data[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *OpenNamespace )( 
            __RPC__in ISymUnmanagedWriter4 * This,
            /* [in] */ __RPC__in const WCHAR *name);
        
        HRESULT ( STDMETHODCALLTYPE *CloseNamespace )( 
            __RPC__in ISymUnmanagedWriter4 * This);
        
        HRESULT ( STDMETHODCALLTYPE *UsingNamespace )( 
            __RPC__in ISymUnmanagedWriter4 * This,
            /* [in] */ __RPC__in const WCHAR *fullName);
        
        HRESULT ( STDMETHODCALLTYPE *SetMethodSourceRange )( 
            __RPC__in ISymUnmanagedWriter4 * This,
            /* [in] */ __RPC__in_opt ISymUnmanagedDocumentWriter *startDoc,
            /* [in] */ ULONG32 startLine,
            /* [in] */ ULONG32 startColumn,
            /* [in] */ __RPC__in_opt ISymUnmanagedDocumentWriter *endDoc,
            /* [in] */ ULONG32 endLine,
            /* [in] */ ULONG32 endColumn);
        
        HRESULT ( STDMETHODCALLTYPE *Initialize )( 
            __RPC__in ISymUnmanagedWriter4 * This,
            /* [in] */ __RPC__in_opt IUnknown *emitter,
            /* [in] */ __RPC__in const WCHAR *filename,
            /* [in] */ __RPC__in_opt IStream *pIStream,
            /* [in] */ BOOL fFullBuild);
        
        HRESULT ( STDMETHODCALLTYPE *GetDebugInfo )( 
            __RPC__in ISymUnmanagedWriter4 * This,
            /* [out][in] */ __RPC__inout IMAGE_DEBUG_DIRECTORY *pIDD,
            /* [in] */ DWORD cData,
            /* [out] */ __RPC__out DWORD *pcData,
            /* [length_is][size_is][out] */ __RPC__out_ecount_part(cData, *pcData) BYTE data[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *DefineSequencePoints )( 
            __RPC__in ISymUnmanagedWriter4 * This,
            /* [in] */ __RPC__in_opt ISymUnmanagedDocumentWriter *document,
            /* [in] */ ULONG32 spCount,
            /* [size_is][in] */ __RPC__in_ecount_full(spCount) ULONG32 offsets[  ],
            /* [size_is][in] */ __RPC__in_ecount_full(spCount) ULONG32 lines[  ],
            /* [size_is][in] */ __RPC__in_ecount_full(spCount) ULONG32 columns[  ],
            /* [size_is][in] */ __RPC__in_ecount_full(spCount) ULONG32 endLines[  ],
            /* [size_is][in] */ __RPC__in_ecount_full(spCount) ULONG32 endColumns[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *RemapToken )( 
            __RPC__in ISymUnmanagedWriter4 * This,
            /* [in] */ mdToken oldToken,
            /* [in] */ mdToken newToken);
        
        HRESULT ( STDMETHODCALLTYPE *Initialize2 )( 
            __RPC__in ISymUnmanagedWriter4 * This,
            /* [in] */ __RPC__in_opt IUnknown *emitter,
            /* [in] */ __RPC__in const WCHAR *tempfilename,
            /* [in] */ __RPC__in_opt IStream *pIStream,
            /* [in] */ BOOL fFullBuild,
            /* [in] */ __RPC__in const WCHAR *finalfilename);
        
        HRESULT ( STDMETHODCALLTYPE *DefineConstant )( 
            __RPC__in ISymUnmanagedWriter4 * This,
            /* [in] */ __RPC__in const WCHAR *name,
            /* [in] */ VARIANT value,
            /* [in] */ ULONG32 cSig,
            /* [size_is][in] */ __RPC__in_ecount_full(cSig) unsigned char signature[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *Abort )( 
            __RPC__in ISymUnmanagedWriter4 * This);
        
        HRESULT ( STDMETHODCALLTYPE *DefineLocalVariable2 )( 
            __RPC__in ISymUnmanagedWriter4 * This,
            /* [in] */ __RPC__in const WCHAR *name,
            /* [in] */ ULONG32 attributes,
            /* [in] */ mdSignature sigToken,
            /* [in] */ ULONG32 addrKind,
            /* [in] */ ULONG32 addr1,
            /* [in] */ ULONG32 addr2,
            /* [in] */ ULONG32 addr3,
            /* [in] */ ULONG32 startOffset,
            /* [in] */ ULONG32 endOffset);
        
        HRESULT ( STDMETHODCALLTYPE *DefineGlobalVariable2 )( 
            __RPC__in ISymUnmanagedWriter4 * This,
            /* [in] */ __RPC__in const WCHAR *name,
            /* [in] */ ULONG32 attributes,
            /* [in] */ mdSignature sigToken,
            /* [in] */ ULONG32 addrKind,
            /* [in] */ ULONG32 addr1,
            /* [in] */ ULONG32 addr2,
            /* [in] */ ULONG32 addr3);
        
        HRESULT ( STDMETHODCALLTYPE *DefineConstant2 )( 
            __RPC__in ISymUnmanagedWriter4 * This,
            /* [in] */ __RPC__in const WCHAR *name,
            /* [in] */ VARIANT value,
            /* [in] */ mdSignature sigToken);
        
        HRESULT ( STDMETHODCALLTYPE *OpenMethod2 )( 
            __RPC__in ISymUnmanagedWriter4 * This,
            /* [in] */ mdMethodDef method,
            /* [in] */ ULONG32 isect,
            /* [in] */ ULONG32 offset);
        
        HRESULT ( STDMETHODCALLTYPE *Commit )( 
            __RPC__in ISymUnmanagedWriter4 * This);
        
        HRESULT ( STDMETHODCALLTYPE *GetDebugInfoWithPadding )( 
            __RPC__in ISymUnmanagedWriter4 * This,
            /* [out][in] */ __RPC__inout IMAGE_DEBUG_DIRECTORY *pIDD,
            /* [in] */ DWORD cData,
            /* [out] */ __RPC__out DWORD *pcData,
            /* [length_is][size_is][out] */ __RPC__out_ecount_part(cData, *pcData) BYTE data[  ]);
        
        END_INTERFACE
    } ISymUnmanagedWriter4Vtbl;

    interface ISymUnmanagedWriter4
    {
        CONST_VTBL struct ISymUnmanagedWriter4Vtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define ISymUnmanagedWriter4_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define ISymUnmanagedWriter4_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define ISymUnmanagedWriter4_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define ISymUnmanagedWriter4_DefineDocument(This,url,language,languageVendor,documentType,pRetVal)	\
    ( (This)->lpVtbl -> DefineDocument(This,url,language,languageVendor,documentType,pRetVal) ) 

#define ISymUnmanagedWriter4_SetUserEntryPoint(This,entryMethod)	\
    ( (This)->lpVtbl -> SetUserEntryPoint(This,entryMethod) ) 

#define ISymUnmanagedWriter4_OpenMethod(This,method)	\
    ( (This)->lpVtbl -> OpenMethod(This,method) ) 

#define ISymUnmanagedWriter4_CloseMethod(This)	\
    ( (This)->lpVtbl -> CloseMethod(This) ) 

#define ISymUnmanagedWriter4_OpenScope(This,startOffset,pRetVal)	\
    ( (This)->lpVtbl -> OpenScope(This,startOffset,pRetVal) ) 

#define ISymUnmanagedWriter4_CloseScope(This,endOffset)	\
    ( (This)->lpVtbl -> CloseScope(This,endOffset) ) 

#define ISymUnmanagedWriter4_SetScopeRange(This,scopeID,startOffset,endOffset)	\
    ( (This)->lpVtbl -> SetScopeRange(This,scopeID,startOffset,endOffset) ) 

#define ISymUnmanagedWriter4_DefineLocalVariable(This,name,attributes,cSig,signature,addrKind,addr1,addr2,addr3,startOffset,endOffset)	\
    ( (This)->lpVtbl -> DefineLocalVariable(This,name,attributes,cSig,signature,addrKind,addr1,addr2,addr3,startOffset,endOffset) ) 

#define ISymUnmanagedWriter4_DefineParameter(This,name,attributes,sequence,addrKind,addr1,addr2,addr3)	\
    ( (This)->lpVtbl -> DefineParameter(This,name,attributes,sequence,addrKind,addr1,addr2,addr3) ) 

#define ISymUnmanagedWriter4_DefineField(This,parent,name,attributes,cSig,signature,addrKind,addr1,addr2,addr3)	\
    ( (This)->lpVtbl -> DefineField(This,parent,name,attributes,cSig,signature,addrKind,addr1,addr2,addr3) ) 

#define ISymUnmanagedWriter4_DefineGlobalVariable(This,name,attributes,cSig,signature,addrKind,addr1,addr2,addr3)	\
    ( (This)->lpVtbl -> DefineGlobalVariable(This,name,attributes,cSig,signature,addrKind,addr1,addr2,addr3) ) 

#define ISymUnmanagedWriter4_Close(This)	\
    ( (This)->lpVtbl -> Close(This) ) 

#define ISymUnmanagedWriter4_SetSymAttribute(This,parent,name,cData,data)	\
    ( (This)->lpVtbl -> SetSymAttribute(This,parent,name,cData,data) ) 

#define ISymUnmanagedWriter4_OpenNamespace(This,name)	\
    ( (This)->lpVtbl -> OpenNamespace(This,name) ) 

#define ISymUnmanagedWriter4_CloseNamespace(This)	\
    ( (This)->lpVtbl -> CloseNamespace(This) ) 

#define ISymUnmanagedWriter4_UsingNamespace(This,fullName)	\
    ( (This)->lpVtbl -> UsingNamespace(This,fullName) ) 

#define ISymUnmanagedWriter4_SetMethodSourceRange(This,startDoc,startLine,startColumn,endDoc,endLine,endColumn)	\
    ( (This)->lpVtbl -> SetMethodSourceRange(This,startDoc,startLine,startColumn,endDoc,endLine,endColumn) ) 

#define ISymUnmanagedWriter4_Initialize(This,emitter,filename,pIStream,fFullBuild)	\
    ( (This)->lpVtbl -> Initialize(This,emitter,filename,pIStream,fFullBuild) ) 

#define ISymUnmanagedWriter4_GetDebugInfo(This,pIDD,cData,pcData,data)	\
    ( (This)->lpVtbl -> GetDebugInfo(This,pIDD,cData,pcData,data) ) 

#define ISymUnmanagedWriter4_DefineSequencePoints(This,document,spCount,offsets,lines,columns,endLines,endColumns)	\
    ( (This)->lpVtbl -> DefineSequencePoints(This,document,spCount,offsets,lines,columns,endLines,endColumns) ) 

#define ISymUnmanagedWriter4_RemapToken(This,oldToken,newToken)	\
    ( (This)->lpVtbl -> RemapToken(This,oldToken,newToken) ) 

#define ISymUnmanagedWriter4_Initialize2(This,emitter,tempfilename,pIStream,fFullBuild,finalfilename)	\
    ( (This)->lpVtbl -> Initialize2(This,emitter,tempfilename,pIStream,fFullBuild,finalfilename) ) 

#define ISymUnmanagedWriter4_DefineConstant(This,name,value,cSig,signature)	\
    ( (This)->lpVtbl -> DefineConstant(This,name,value,cSig,signature) ) 

#define ISymUnmanagedWriter4_Abort(This)	\
    ( (This)->lpVtbl -> Abort(This) ) 


#define ISymUnmanagedWriter4_DefineLocalVariable2(This,name,attributes,sigToken,addrKind,addr1,addr2,addr3,startOffset,endOffset)	\
    ( (This)->lpVtbl -> DefineLocalVariable2(This,name,attributes,sigToken,addrKind,addr1,addr2,addr3,startOffset,endOffset) ) 

#define ISymUnmanagedWriter4_DefineGlobalVariable2(This,name,attributes,sigToken,addrKind,addr1,addr2,addr3)	\
    ( (This)->lpVtbl -> DefineGlobalVariable2(This,name,attributes,sigToken,addrKind,addr1,addr2,addr3) ) 

#define ISymUnmanagedWriter4_DefineConstant2(This,name,value,sigToken)	\
    ( (This)->lpVtbl -> DefineConstant2(This,name,value,sigToken) ) 


#define ISymUnmanagedWriter4_OpenMethod2(This,method,isect,offset)	\
    ( (This)->lpVtbl -> OpenMethod2(This,method,isect,offset) ) 

#define ISymUnmanagedWriter4_Commit(This)	\
    ( (This)->lpVtbl -> Commit(This) ) 


#define ISymUnmanagedWriter4_GetDebugInfoWithPadding(This,pIDD,cData,pcData,data)	\
    ( (This)->lpVtbl -> GetDebugInfoWithPadding(This,pIDD,cData,pcData,data) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ISymUnmanagedWriter4_INTERFACE_DEFINED__ */


#ifndef __ISymUnmanagedWriter5_INTERFACE_DEFINED__
#define __ISymUnmanagedWriter5_INTERFACE_DEFINED__

/* interface ISymUnmanagedWriter5 */
/* [unique][uuid][object] */ 


EXTERN_C const IID IID_ISymUnmanagedWriter5;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("DCF7780D-BDE9-45DF-ACFE-21731A32000C")
    ISymUnmanagedWriter5 : public ISymUnmanagedWriter4
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE OpenMapTokensToSourceSpans( void) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE CloseMapTokensToSourceSpans( void) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE MapTokenToSourceSpan( 
            /* [in] */ mdToken token,
            /* [in] */ __RPC__in_opt ISymUnmanagedDocumentWriter *document,
            /* [in] */ ULONG32 line,
            /* [in] */ ULONG32 column,
            /* [in] */ ULONG32 endLine,
            /* [in] */ ULONG32 endColumn) = 0;
        
    };
    
    
#else 	/* C style interface */

    typedef struct ISymUnmanagedWriter5Vtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            __RPC__in ISymUnmanagedWriter5 * This,
            /* [in] */ __RPC__in REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            __RPC__in ISymUnmanagedWriter5 * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            __RPC__in ISymUnmanagedWriter5 * This);
        
        HRESULT ( STDMETHODCALLTYPE *DefineDocument )( 
            __RPC__in ISymUnmanagedWriter5 * This,
            /* [in] */ __RPC__in const WCHAR *url,
            /* [in] */ __RPC__in const GUID *language,
            /* [in] */ __RPC__in const GUID *languageVendor,
            /* [in] */ __RPC__in const GUID *documentType,
            /* [retval][out] */ __RPC__deref_out_opt ISymUnmanagedDocumentWriter **pRetVal);
        
        HRESULT ( STDMETHODCALLTYPE *SetUserEntryPoint )( 
            __RPC__in ISymUnmanagedWriter5 * This,
            /* [in] */ mdMethodDef entryMethod);
        
        HRESULT ( STDMETHODCALLTYPE *OpenMethod )( 
            __RPC__in ISymUnmanagedWriter5 * This,
            /* [in] */ mdMethodDef method);
        
        HRESULT ( STDMETHODCALLTYPE *CloseMethod )( 
            __RPC__in ISymUnmanagedWriter5 * This);
        
        HRESULT ( STDMETHODCALLTYPE *OpenScope )( 
            __RPC__in ISymUnmanagedWriter5 * This,
            /* [in] */ ULONG32 startOffset,
            /* [retval][out] */ __RPC__out ULONG32 *pRetVal);
        
        HRESULT ( STDMETHODCALLTYPE *CloseScope )( 
            __RPC__in ISymUnmanagedWriter5 * This,
            /* [in] */ ULONG32 endOffset);
        
        HRESULT ( STDMETHODCALLTYPE *SetScopeRange )( 
            __RPC__in ISymUnmanagedWriter5 * This,
            /* [in] */ ULONG32 scopeID,
            /* [in] */ ULONG32 startOffset,
            /* [in] */ ULONG32 endOffset);
        
        HRESULT ( STDMETHODCALLTYPE *DefineLocalVariable )( 
            __RPC__in ISymUnmanagedWriter5 * This,
            /* [in] */ __RPC__in const WCHAR *name,
            /* [in] */ ULONG32 attributes,
            /* [in] */ ULONG32 cSig,
            /* [size_is][in] */ __RPC__in_ecount_full(cSig) unsigned char signature[  ],
            /* [in] */ ULONG32 addrKind,
            /* [in] */ ULONG32 addr1,
            /* [in] */ ULONG32 addr2,
            /* [in] */ ULONG32 addr3,
            /* [in] */ ULONG32 startOffset,
            /* [in] */ ULONG32 endOffset);
        
        HRESULT ( STDMETHODCALLTYPE *DefineParameter )( 
            __RPC__in ISymUnmanagedWriter5 * This,
            /* [in] */ __RPC__in const WCHAR *name,
            /* [in] */ ULONG32 attributes,
            /* [in] */ ULONG32 sequence,
            /* [in] */ ULONG32 addrKind,
            /* [in] */ ULONG32 addr1,
            /* [in] */ ULONG32 addr2,
            /* [in] */ ULONG32 addr3);
        
        HRESULT ( STDMETHODCALLTYPE *DefineField )( 
            __RPC__in ISymUnmanagedWriter5 * This,
            /* [in] */ mdTypeDef parent,
            /* [in] */ __RPC__in const WCHAR *name,
            /* [in] */ ULONG32 attributes,
            /* [in] */ ULONG32 cSig,
            /* [size_is][in] */ __RPC__in_ecount_full(cSig) unsigned char signature[  ],
            /* [in] */ ULONG32 addrKind,
            /* [in] */ ULONG32 addr1,
            /* [in] */ ULONG32 addr2,
            /* [in] */ ULONG32 addr3);
        
        HRESULT ( STDMETHODCALLTYPE *DefineGlobalVariable )( 
            __RPC__in ISymUnmanagedWriter5 * This,
            /* [in] */ __RPC__in const WCHAR *name,
            /* [in] */ ULONG32 attributes,
            /* [in] */ ULONG32 cSig,
            /* [size_is][in] */ __RPC__in_ecount_full(cSig) unsigned char signature[  ],
            /* [in] */ ULONG32 addrKind,
            /* [in] */ ULONG32 addr1,
            /* [in] */ ULONG32 addr2,
            /* [in] */ ULONG32 addr3);
        
        HRESULT ( STDMETHODCALLTYPE *Close )( 
            __RPC__in ISymUnmanagedWriter5 * This);
        
        HRESULT ( STDMETHODCALLTYPE *SetSymAttribute )( 
            __RPC__in ISymUnmanagedWriter5 * This,
            /* [in] */ mdToken parent,
            /* [in] */ __RPC__in const WCHAR *name,
            /* [in] */ ULONG32 cData,
            /* [size_is][in] */ __RPC__in_ecount_full(cData) unsigned char data[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *OpenNamespace )( 
            __RPC__in ISymUnmanagedWriter5 * This,
            /* [in] */ __RPC__in const WCHAR *name);
        
        HRESULT ( STDMETHODCALLTYPE *CloseNamespace )( 
            __RPC__in ISymUnmanagedWriter5 * This);
        
        HRESULT ( STDMETHODCALLTYPE *UsingNamespace )( 
            __RPC__in ISymUnmanagedWriter5 * This,
            /* [in] */ __RPC__in const WCHAR *fullName);
        
        HRESULT ( STDMETHODCALLTYPE *SetMethodSourceRange )( 
            __RPC__in ISymUnmanagedWriter5 * This,
            /* [in] */ __RPC__in_opt ISymUnmanagedDocumentWriter *startDoc,
            /* [in] */ ULONG32 startLine,
            /* [in] */ ULONG32 startColumn,
            /* [in] */ __RPC__in_opt ISymUnmanagedDocumentWriter *endDoc,
            /* [in] */ ULONG32 endLine,
            /* [in] */ ULONG32 endColumn);
        
        HRESULT ( STDMETHODCALLTYPE *Initialize )( 
            __RPC__in ISymUnmanagedWriter5 * This,
            /* [in] */ __RPC__in_opt IUnknown *emitter,
            /* [in] */ __RPC__in const WCHAR *filename,
            /* [in] */ __RPC__in_opt IStream *pIStream,
            /* [in] */ BOOL fFullBuild);
        
        HRESULT ( STDMETHODCALLTYPE *GetDebugInfo )( 
            __RPC__in ISymUnmanagedWriter5 * This,
            /* [out][in] */ __RPC__inout IMAGE_DEBUG_DIRECTORY *pIDD,
            /* [in] */ DWORD cData,
            /* [out] */ __RPC__out DWORD *pcData,
            /* [length_is][size_is][out] */ __RPC__out_ecount_part(cData, *pcData) BYTE data[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *DefineSequencePoints )( 
            __RPC__in ISymUnmanagedWriter5 * This,
            /* [in] */ __RPC__in_opt ISymUnmanagedDocumentWriter *document,
            /* [in] */ ULONG32 spCount,
            /* [size_is][in] */ __RPC__in_ecount_full(spCount) ULONG32 offsets[  ],
            /* [size_is][in] */ __RPC__in_ecount_full(spCount) ULONG32 lines[  ],
            /* [size_is][in] */ __RPC__in_ecount_full(spCount) ULONG32 columns[  ],
            /* [size_is][in] */ __RPC__in_ecount_full(spCount) ULONG32 endLines[  ],
            /* [size_is][in] */ __RPC__in_ecount_full(spCount) ULONG32 endColumns[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *RemapToken )( 
            __RPC__in ISymUnmanagedWriter5 * This,
            /* [in] */ mdToken oldToken,
            /* [in] */ mdToken newToken);
        
        HRESULT ( STDMETHODCALLTYPE *Initialize2 )( 
            __RPC__in ISymUnmanagedWriter5 * This,
            /* [in] */ __RPC__in_opt IUnknown *emitter,
            /* [in] */ __RPC__in const WCHAR *tempfilename,
            /* [in] */ __RPC__in_opt IStream *pIStream,
            /* [in] */ BOOL fFullBuild,
            /* [in] */ __RPC__in const WCHAR *finalfilename);
        
        HRESULT ( STDMETHODCALLTYPE *DefineConstant )( 
            __RPC__in ISymUnmanagedWriter5 * This,
            /* [in] */ __RPC__in const WCHAR *name,
            /* [in] */ VARIANT value,
            /* [in] */ ULONG32 cSig,
            /* [size_is][in] */ __RPC__in_ecount_full(cSig) unsigned char signature[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *Abort )( 
            __RPC__in ISymUnmanagedWriter5 * This);
        
        HRESULT ( STDMETHODCALLTYPE *DefineLocalVariable2 )( 
            __RPC__in ISymUnmanagedWriter5 * This,
            /* [in] */ __RPC__in const WCHAR *name,
            /* [in] */ ULONG32 attributes,
            /* [in] */ mdSignature sigToken,
            /* [in] */ ULONG32 addrKind,
            /* [in] */ ULONG32 addr1,
            /* [in] */ ULONG32 addr2,
            /* [in] */ ULONG32 addr3,
            /* [in] */ ULONG32 startOffset,
            /* [in] */ ULONG32 endOffset);
        
        HRESULT ( STDMETHODCALLTYPE *DefineGlobalVariable2 )( 
            __RPC__in ISymUnmanagedWriter5 * This,
            /* [in] */ __RPC__in const WCHAR *name,
            /* [in] */ ULONG32 attributes,
            /* [in] */ mdSignature sigToken,
            /* [in] */ ULONG32 addrKind,
            /* [in] */ ULONG32 addr1,
            /* [in] */ ULONG32 addr2,
            /* [in] */ ULONG32 addr3);
        
        HRESULT ( STDMETHODCALLTYPE *DefineConstant2 )( 
            __RPC__in ISymUnmanagedWriter5 * This,
            /* [in] */ __RPC__in const WCHAR *name,
            /* [in] */ VARIANT value,
            /* [in] */ mdSignature sigToken);
        
        HRESULT ( STDMETHODCALLTYPE *OpenMethod2 )( 
            __RPC__in ISymUnmanagedWriter5 * This,
            /* [in] */ mdMethodDef method,
            /* [in] */ ULONG32 isect,
            /* [in] */ ULONG32 offset);
        
        HRESULT ( STDMETHODCALLTYPE *Commit )( 
            __RPC__in ISymUnmanagedWriter5 * This);
        
        HRESULT ( STDMETHODCALLTYPE *GetDebugInfoWithPadding )( 
            __RPC__in ISymUnmanagedWriter5 * This,
            /* [out][in] */ __RPC__inout IMAGE_DEBUG_DIRECTORY *pIDD,
            /* [in] */ DWORD cData,
            /* [out] */ __RPC__out DWORD *pcData,
            /* [length_is][size_is][out] */ __RPC__out_ecount_part(cData, *pcData) BYTE data[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *OpenMapTokensToSourceSpans )( 
            __RPC__in ISymUnmanagedWriter5 * This);
        
        HRESULT ( STDMETHODCALLTYPE *CloseMapTokensToSourceSpans )( 
            __RPC__in ISymUnmanagedWriter5 * This);
        
        HRESULT ( STDMETHODCALLTYPE *MapTokenToSourceSpan )( 
            __RPC__in ISymUnmanagedWriter5 * This,
            /* [in] */ mdToken token,
            /* [in] */ __RPC__in_opt ISymUnmanagedDocumentWriter *document,
            /* [in] */ ULONG32 line,
            /* [in] */ ULONG32 column,
            /* [in] */ ULONG32 endLine,
            /* [in] */ ULONG32 endColumn);
        
        END_INTERFACE
    } ISymUnmanagedWriter5Vtbl;

    interface ISymUnmanagedWriter5
    {
        CONST_VTBL struct ISymUnmanagedWriter5Vtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define ISymUnmanagedWriter5_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define ISymUnmanagedWriter5_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define ISymUnmanagedWriter5_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define ISymUnmanagedWriter5_DefineDocument(This,url,language,languageVendor,documentType,pRetVal)	\
    ( (This)->lpVtbl -> DefineDocument(This,url,language,languageVendor,documentType,pRetVal) ) 

#define ISymUnmanagedWriter5_SetUserEntryPoint(This,entryMethod)	\
    ( (This)->lpVtbl -> SetUserEntryPoint(This,entryMethod) ) 

#define ISymUnmanagedWriter5_OpenMethod(This,method)	\
    ( (This)->lpVtbl -> OpenMethod(This,method) ) 

#define ISymUnmanagedWriter5_CloseMethod(This)	\
    ( (This)->lpVtbl -> CloseMethod(This) ) 

#define ISymUnmanagedWriter5_OpenScope(This,startOffset,pRetVal)	\
    ( (This)->lpVtbl -> OpenScope(This,startOffset,pRetVal) ) 

#define ISymUnmanagedWriter5_CloseScope(This,endOffset)	\
    ( (This)->lpVtbl -> CloseScope(This,endOffset) ) 

#define ISymUnmanagedWriter5_SetScopeRange(This,scopeID,startOffset,endOffset)	\
    ( (This)->lpVtbl -> SetScopeRange(This,scopeID,startOffset,endOffset) ) 

#define ISymUnmanagedWriter5_DefineLocalVariable(This,name,attributes,cSig,signature,addrKind,addr1,addr2,addr3,startOffset,endOffset)	\
    ( (This)->lpVtbl -> DefineLocalVariable(This,name,attributes,cSig,signature,addrKind,addr1,addr2,addr3,startOffset,endOffset) ) 

#define ISymUnmanagedWriter5_DefineParameter(This,name,attributes,sequence,addrKind,addr1,addr2,addr3)	\
    ( (This)->lpVtbl -> DefineParameter(This,name,attributes,sequence,addrKind,addr1,addr2,addr3) ) 

#define ISymUnmanagedWriter5_DefineField(This,parent,name,attributes,cSig,signature,addrKind,addr1,addr2,addr3)	\
    ( (This)->lpVtbl -> DefineField(This,parent,name,attributes,cSig,signature,addrKind,addr1,addr2,addr3) ) 

#define ISymUnmanagedWriter5_DefineGlobalVariable(This,name,attributes,cSig,signature,addrKind,addr1,addr2,addr3)	\
    ( (This)->lpVtbl -> DefineGlobalVariable(This,name,attributes,cSig,signature,addrKind,addr1,addr2,addr3) ) 

#define ISymUnmanagedWriter5_Close(This)	\
    ( (This)->lpVtbl -> Close(This) ) 

#define ISymUnmanagedWriter5_SetSymAttribute(This,parent,name,cData,data)	\
    ( (This)->lpVtbl -> SetSymAttribute(This,parent,name,cData,data) ) 

#define ISymUnmanagedWriter5_OpenNamespace(This,name)	\
    ( (This)->lpVtbl -> OpenNamespace(This,name) ) 

#define ISymUnmanagedWriter5_CloseNamespace(This)	\
    ( (This)->lpVtbl -> CloseNamespace(This) ) 

#define ISymUnmanagedWriter5_UsingNamespace(This,fullName)	\
    ( (This)->lpVtbl -> UsingNamespace(This,fullName) ) 

#define ISymUnmanagedWriter5_SetMethodSourceRange(This,startDoc,startLine,startColumn,endDoc,endLine,endColumn)	\
    ( (This)->lpVtbl -> SetMethodSourceRange(This,startDoc,startLine,startColumn,endDoc,endLine,endColumn) ) 

#define ISymUnmanagedWriter5_Initialize(This,emitter,filename,pIStream,fFullBuild)	\
    ( (This)->lpVtbl -> Initialize(This,emitter,filename,pIStream,fFullBuild) ) 

#define ISymUnmanagedWriter5_GetDebugInfo(This,pIDD,cData,pcData,data)	\
    ( (This)->lpVtbl -> GetDebugInfo(This,pIDD,cData,pcData,data) ) 

#define ISymUnmanagedWriter5_DefineSequencePoints(This,document,spCount,offsets,lines,columns,endLines,endColumns)	\
    ( (This)->lpVtbl -> DefineSequencePoints(This,document,spCount,offsets,lines,columns,endLines,endColumns) ) 

#define ISymUnmanagedWriter5_RemapToken(This,oldToken,newToken)	\
    ( (This)->lpVtbl -> RemapToken(This,oldToken,newToken) ) 

#define ISymUnmanagedWriter5_Initialize2(This,emitter,tempfilename,pIStream,fFullBuild,finalfilename)	\
    ( (This)->lpVtbl -> Initialize2(This,emitter,tempfilename,pIStream,fFullBuild,finalfilename) ) 

#define ISymUnmanagedWriter5_DefineConstant(This,name,value,cSig,signature)	\
    ( (This)->lpVtbl -> DefineConstant(This,name,value,cSig,signature) ) 

#define ISymUnmanagedWriter5_Abort(This)	\
    ( (This)->lpVtbl -> Abort(This) ) 


#define ISymUnmanagedWriter5_DefineLocalVariable2(This,name,attributes,sigToken,addrKind,addr1,addr2,addr3,startOffset,endOffset)	\
    ( (This)->lpVtbl -> DefineLocalVariable2(This,name,attributes,sigToken,addrKind,addr1,addr2,addr3,startOffset,endOffset) ) 

#define ISymUnmanagedWriter5_DefineGlobalVariable2(This,name,attributes,sigToken,addrKind,addr1,addr2,addr3)	\
    ( (This)->lpVtbl -> DefineGlobalVariable2(This,name,attributes,sigToken,addrKind,addr1,addr2,addr3) ) 

#define ISymUnmanagedWriter5_DefineConstant2(This,name,value,sigToken)	\
    ( (This)->lpVtbl -> DefineConstant2(This,name,value,sigToken) ) 


#define ISymUnmanagedWriter5_OpenMethod2(This,method,isect,offset)	\
    ( (This)->lpVtbl -> OpenMethod2(This,method,isect,offset) ) 

#define ISymUnmanagedWriter5_Commit(This)	\
    ( (This)->lpVtbl -> Commit(This) ) 


#define ISymUnmanagedWriter5_GetDebugInfoWithPadding(This,pIDD,cData,pcData,data)	\
    ( (This)->lpVtbl -> GetDebugInfoWithPadding(This,pIDD,cData,pcData,data) ) 


#define ISymUnmanagedWriter5_OpenMapTokensToSourceSpans(This)	\
    ( (This)->lpVtbl -> OpenMapTokensToSourceSpans(This) ) 

#define ISymUnmanagedWriter5_CloseMapTokensToSourceSpans(This)	\
    ( (This)->lpVtbl -> CloseMapTokensToSourceSpans(This) ) 

#define ISymUnmanagedWriter5_MapTokenToSourceSpan(This,token,document,line,column,endLine,endColumn)	\
    ( (This)->lpVtbl -> MapTokenToSourceSpan(This,token,document,line,column,endLine,endColumn) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ISymUnmanagedWriter5_INTERFACE_DEFINED__ */


#ifndef __ISymUnmanagedReader2_INTERFACE_DEFINED__
#define __ISymUnmanagedReader2_INTERFACE_DEFINED__

/* interface ISymUnmanagedReader2 */
/* [unique][uuid][object] */ 


EXTERN_C const IID IID_ISymUnmanagedReader2;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("A09E53B2-2A57-4cca-8F63-B84F7C35D4AA")
    ISymUnmanagedReader2 : public ISymUnmanagedReader
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE GetMethodByVersionPreRemap( 
            /* [in] */ mdMethodDef token,
            /* [in] */ int version,
            /* [retval][out] */ __RPC__deref_out_opt ISymUnmanagedMethod **pRetVal) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetSymAttributePreRemap( 
            /* [in] */ mdToken parent,
            /* [in] */ __RPC__in WCHAR *name,
            /* [in] */ ULONG32 cBuffer,
            /* [out] */ __RPC__out ULONG32 *pcBuffer,
            /* [length_is][size_is][out] */ __RPC__out_ecount_part(cBuffer, *pcBuffer) BYTE buffer[  ]) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetMethodsInDocument( 
            /* [in] */ __RPC__in_opt ISymUnmanagedDocument *document,
            /* [in] */ ULONG32 cMethod,
            /* [out] */ __RPC__out ULONG32 *pcMethod,
            /* [length_is][size_is][out] */ __RPC__out_ecount_part(cMethod, *pcMethod) ISymUnmanagedMethod *pRetVal[  ]) = 0;
        
    };
    
    
#else 	/* C style interface */

    typedef struct ISymUnmanagedReader2Vtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            __RPC__in ISymUnmanagedReader2 * This,
            /* [in] */ __RPC__in REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            __RPC__in ISymUnmanagedReader2 * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            __RPC__in ISymUnmanagedReader2 * This);
        
        HRESULT ( STDMETHODCALLTYPE *GetDocument )( 
            __RPC__in ISymUnmanagedReader2 * This,
            /* [in] */ __RPC__in WCHAR *url,
            /* [in] */ GUID language,
            /* [in] */ GUID languageVendor,
            /* [in] */ GUID documentType,
            /* [retval][out] */ __RPC__deref_out_opt ISymUnmanagedDocument **pRetVal);
        
        HRESULT ( STDMETHODCALLTYPE *GetDocuments )( 
            __RPC__in ISymUnmanagedReader2 * This,
            /* [in] */ ULONG32 cDocs,
            /* [out] */ __RPC__out ULONG32 *pcDocs,
            /* [length_is][size_is][out] */ __RPC__out_ecount_part(cDocs, *pcDocs) ISymUnmanagedDocument *pDocs[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *GetUserEntryPoint )( 
            __RPC__in ISymUnmanagedReader2 * This,
            /* [retval][out] */ __RPC__out mdMethodDef *pToken);
        
        HRESULT ( STDMETHODCALLTYPE *GetMethod )( 
            __RPC__in ISymUnmanagedReader2 * This,
            /* [in] */ mdMethodDef token,
            /* [retval][out] */ __RPC__deref_out_opt ISymUnmanagedMethod **pRetVal);
        
        HRESULT ( STDMETHODCALLTYPE *GetMethodByVersion )( 
            __RPC__in ISymUnmanagedReader2 * This,
            /* [in] */ mdMethodDef token,
            /* [in] */ int version,
            /* [retval][out] */ __RPC__deref_out_opt ISymUnmanagedMethod **pRetVal);
        
        HRESULT ( STDMETHODCALLTYPE *GetVariables )( 
            __RPC__in ISymUnmanagedReader2 * This,
            /* [in] */ mdToken parent,
            /* [in] */ ULONG32 cVars,
            /* [out] */ __RPC__out ULONG32 *pcVars,
            /* [length_is][size_is][out] */ __RPC__out_ecount_part(cVars, *pcVars) ISymUnmanagedVariable *pVars[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *GetGlobalVariables )( 
            __RPC__in ISymUnmanagedReader2 * This,
            /* [in] */ ULONG32 cVars,
            /* [out] */ __RPC__out ULONG32 *pcVars,
            /* [length_is][size_is][out] */ __RPC__out_ecount_part(cVars, *pcVars) ISymUnmanagedVariable *pVars[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *GetMethodFromDocumentPosition )( 
            __RPC__in ISymUnmanagedReader2 * This,
            /* [in] */ __RPC__in_opt ISymUnmanagedDocument *document,
            /* [in] */ ULONG32 line,
            /* [in] */ ULONG32 column,
            /* [retval][out] */ __RPC__deref_out_opt ISymUnmanagedMethod **pRetVal);
        
        HRESULT ( STDMETHODCALLTYPE *GetSymAttribute )( 
            __RPC__in ISymUnmanagedReader2 * This,
            /* [in] */ mdToken parent,
            /* [in] */ __RPC__in WCHAR *name,
            /* [in] */ ULONG32 cBuffer,
            /* [out] */ __RPC__out ULONG32 *pcBuffer,
            /* [length_is][size_is][out] */ __RPC__out_ecount_part(cBuffer, *pcBuffer) BYTE buffer[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *GetNamespaces )( 
            __RPC__in ISymUnmanagedReader2 * This,
            /* [in] */ ULONG32 cNameSpaces,
            /* [out] */ __RPC__out ULONG32 *pcNameSpaces,
            /* [length_is][size_is][out] */ __RPC__out_ecount_part(cNameSpaces, *pcNameSpaces) ISymUnmanagedNamespace *namespaces[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *Initialize )( 
            __RPC__in ISymUnmanagedReader2 * This,
            /* [in] */ __RPC__in_opt IUnknown *importer,
            /* [in] */ __RPC__in const WCHAR *filename,
            /* [in] */ __RPC__in const WCHAR *searchPath,
            /* [in] */ __RPC__in_opt IStream *pIStream);
        
        HRESULT ( STDMETHODCALLTYPE *UpdateSymbolStore )( 
            __RPC__in ISymUnmanagedReader2 * This,
            /* [in] */ __RPC__in const WCHAR *filename,
            /* [in] */ __RPC__in_opt IStream *pIStream);
        
        HRESULT ( STDMETHODCALLTYPE *ReplaceSymbolStore )( 
            __RPC__in ISymUnmanagedReader2 * This,
            /* [in] */ __RPC__in const WCHAR *filename,
            /* [in] */ __RPC__in_opt IStream *pIStream);
        
        HRESULT ( STDMETHODCALLTYPE *GetSymbolStoreFileName )( 
            __RPC__in ISymUnmanagedReader2 * This,
            /* [in] */ ULONG32 cchName,
            /* [out] */ __RPC__out ULONG32 *pcchName,
            /* [length_is][size_is][out] */ __RPC__out_ecount_part(cchName, *pcchName) WCHAR szName[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *GetMethodsFromDocumentPosition )( 
            __RPC__in ISymUnmanagedReader2 * This,
            /* [in] */ __RPC__in_opt ISymUnmanagedDocument *document,
            /* [in] */ ULONG32 line,
            /* [in] */ ULONG32 column,
            /* [in] */ ULONG32 cMethod,
            /* [out] */ __RPC__out ULONG32 *pcMethod,
            /* [length_is][size_is][out] */ __RPC__out_ecount_part(cMethod, *pcMethod) ISymUnmanagedMethod *pRetVal[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *GetDocumentVersion )( 
            __RPC__in ISymUnmanagedReader2 * This,
            /* [in] */ __RPC__in_opt ISymUnmanagedDocument *pDoc,
            /* [out] */ __RPC__out int *version,
            /* [out] */ __RPC__out BOOL *pbCurrent);
        
        HRESULT ( STDMETHODCALLTYPE *GetMethodVersion )( 
            __RPC__in ISymUnmanagedReader2 * This,
            /* [in] */ __RPC__in_opt ISymUnmanagedMethod *pMethod,
            /* [out] */ __RPC__out int *version);
        
        HRESULT ( STDMETHODCALLTYPE *GetMethodByVersionPreRemap )( 
            __RPC__in ISymUnmanagedReader2 * This,
            /* [in] */ mdMethodDef token,
            /* [in] */ int version,
            /* [retval][out] */ __RPC__deref_out_opt ISymUnmanagedMethod **pRetVal);
        
        HRESULT ( STDMETHODCALLTYPE *GetSymAttributePreRemap )( 
            __RPC__in ISymUnmanagedReader2 * This,
            /* [in] */ mdToken parent,
            /* [in] */ __RPC__in WCHAR *name,
            /* [in] */ ULONG32 cBuffer,
            /* [out] */ __RPC__out ULONG32 *pcBuffer,
            /* [length_is][size_is][out] */ __RPC__out_ecount_part(cBuffer, *pcBuffer) BYTE buffer[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *GetMethodsInDocument )( 
            __RPC__in ISymUnmanagedReader2 * This,
            /* [in] */ __RPC__in_opt ISymUnmanagedDocument *document,
            /* [in] */ ULONG32 cMethod,
            /* [out] */ __RPC__out ULONG32 *pcMethod,
            /* [length_is][size_is][out] */ __RPC__out_ecount_part(cMethod, *pcMethod) ISymUnmanagedMethod *pRetVal[  ]);
        
        END_INTERFACE
    } ISymUnmanagedReader2Vtbl;

    interface ISymUnmanagedReader2
    {
        CONST_VTBL struct ISymUnmanagedReader2Vtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define ISymUnmanagedReader2_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define ISymUnmanagedReader2_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define ISymUnmanagedReader2_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define ISymUnmanagedReader2_GetDocument(This,url,language,languageVendor,documentType,pRetVal)	\
    ( (This)->lpVtbl -> GetDocument(This,url,language,languageVendor,documentType,pRetVal) ) 

#define ISymUnmanagedReader2_GetDocuments(This,cDocs,pcDocs,pDocs)	\
    ( (This)->lpVtbl -> GetDocuments(This,cDocs,pcDocs,pDocs) ) 

#define ISymUnmanagedReader2_GetUserEntryPoint(This,pToken)	\
    ( (This)->lpVtbl -> GetUserEntryPoint(This,pToken) ) 

#define ISymUnmanagedReader2_GetMethod(This,token,pRetVal)	\
    ( (This)->lpVtbl -> GetMethod(This,token,pRetVal) ) 

#define ISymUnmanagedReader2_GetMethodByVersion(This,token,version,pRetVal)	\
    ( (This)->lpVtbl -> GetMethodByVersion(This,token,version,pRetVal) ) 

#define ISymUnmanagedReader2_GetVariables(This,parent,cVars,pcVars,pVars)	\
    ( (This)->lpVtbl -> GetVariables(This,parent,cVars,pcVars,pVars) ) 

#define ISymUnmanagedReader2_GetGlobalVariables(This,cVars,pcVars,pVars)	\
    ( (This)->lpVtbl -> GetGlobalVariables(This,cVars,pcVars,pVars) ) 

#define ISymUnmanagedReader2_GetMethodFromDocumentPosition(This,document,line,column,pRetVal)	\
    ( (This)->lpVtbl -> GetMethodFromDocumentPosition(This,document,line,column,pRetVal) ) 

#define ISymUnmanagedReader2_GetSymAttribute(This,parent,name,cBuffer,pcBuffer,buffer)	\
    ( (This)->lpVtbl -> GetSymAttribute(This,parent,name,cBuffer,pcBuffer,buffer) ) 

#define ISymUnmanagedReader2_GetNamespaces(This,cNameSpaces,pcNameSpaces,namespaces)	\
    ( (This)->lpVtbl -> GetNamespaces(This,cNameSpaces,pcNameSpaces,namespaces) ) 

#define ISymUnmanagedReader2_Initialize(This,importer,filename,searchPath,pIStream)	\
    ( (This)->lpVtbl -> Initialize(This,importer,filename,searchPath,pIStream) ) 

#define ISymUnmanagedReader2_UpdateSymbolStore(This,filename,pIStream)	\
    ( (This)->lpVtbl -> UpdateSymbolStore(This,filename,pIStream) ) 

#define ISymUnmanagedReader2_ReplaceSymbolStore(This,filename,pIStream)	\
    ( (This)->lpVtbl -> ReplaceSymbolStore(This,filename,pIStream) ) 

#define ISymUnmanagedReader2_GetSymbolStoreFileName(This,cchName,pcchName,szName)	\
    ( (This)->lpVtbl -> GetSymbolStoreFileName(This,cchName,pcchName,szName) ) 

#define ISymUnmanagedReader2_GetMethodsFromDocumentPosition(This,document,line,column,cMethod,pcMethod,pRetVal)	\
    ( (This)->lpVtbl -> GetMethodsFromDocumentPosition(This,document,line,column,cMethod,pcMethod,pRetVal) ) 

#define ISymUnmanagedReader2_GetDocumentVersion(This,pDoc,version,pbCurrent)	\
    ( (This)->lpVtbl -> GetDocumentVersion(This,pDoc,version,pbCurrent) ) 

#define ISymUnmanagedReader2_GetMethodVersion(This,pMethod,version)	\
    ( (This)->lpVtbl -> GetMethodVersion(This,pMethod,version) ) 


#define ISymUnmanagedReader2_GetMethodByVersionPreRemap(This,token,version,pRetVal)	\
    ( (This)->lpVtbl -> GetMethodByVersionPreRemap(This,token,version,pRetVal) ) 

#define ISymUnmanagedReader2_GetSymAttributePreRemap(This,parent,name,cBuffer,pcBuffer,buffer)	\
    ( (This)->lpVtbl -> GetSymAttributePreRemap(This,parent,name,cBuffer,pcBuffer,buffer) ) 

#define ISymUnmanagedReader2_GetMethodsInDocument(This,document,cMethod,pcMethod,pRetVal)	\
    ( (This)->lpVtbl -> GetMethodsInDocument(This,document,cMethod,pcMethod,pRetVal) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ISymUnmanagedReader2_INTERFACE_DEFINED__ */


#ifndef __ISymNGenWriter_INTERFACE_DEFINED__
#define __ISymNGenWriter_INTERFACE_DEFINED__

/* interface ISymNGenWriter */
/* [unique][uuid][object] */ 


EXTERN_C const IID IID_ISymNGenWriter;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("d682fd12-43de-411c-811b-be8404cea126")
    ISymNGenWriter : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE AddSymbol( 
            /* [in] */ __RPC__in BSTR pSymbol,
            /* [in] */ USHORT iSection,
            /* [in] */ ULONGLONG rva) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE AddSection( 
            /* [in] */ USHORT iSection,
            /* [in] */ USHORT flags,
            /* [in] */ long offset,
            /* [in] */ long cb) = 0;
        
    };
    
    
#else 	/* C style interface */

    typedef struct ISymNGenWriterVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            __RPC__in ISymNGenWriter * This,
            /* [in] */ __RPC__in REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            __RPC__in ISymNGenWriter * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            __RPC__in ISymNGenWriter * This);
        
        HRESULT ( STDMETHODCALLTYPE *AddSymbol )( 
            __RPC__in ISymNGenWriter * This,
            /* [in] */ __RPC__in BSTR pSymbol,
            /* [in] */ USHORT iSection,
            /* [in] */ ULONGLONG rva);
        
        HRESULT ( STDMETHODCALLTYPE *AddSection )( 
            __RPC__in ISymNGenWriter * This,
            /* [in] */ USHORT iSection,
            /* [in] */ USHORT flags,
            /* [in] */ long offset,
            /* [in] */ long cb);
        
        END_INTERFACE
    } ISymNGenWriterVtbl;

    interface ISymNGenWriter
    {
        CONST_VTBL struct ISymNGenWriterVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define ISymNGenWriter_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define ISymNGenWriter_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define ISymNGenWriter_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define ISymNGenWriter_AddSymbol(This,pSymbol,iSection,rva)	\
    ( (This)->lpVtbl -> AddSymbol(This,pSymbol,iSection,rva) ) 

#define ISymNGenWriter_AddSection(This,iSection,flags,offset,cb)	\
    ( (This)->lpVtbl -> AddSection(This,iSection,flags,offset,cb) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ISymNGenWriter_INTERFACE_DEFINED__ */


#ifndef __ISymNGenWriter2_INTERFACE_DEFINED__
#define __ISymNGenWriter2_INTERFACE_DEFINED__

/* interface ISymNGenWriter2 */
/* [unique][uuid][local][object] */ 


EXTERN_C const IID IID_ISymNGenWriter2;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("B029E51B-4C55-4fe2-B993-9F7BC1F10DB4")
    ISymNGenWriter2 : public ISymNGenWriter
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE OpenModW( 
            /* [in] */ const wchar_t *wszModule,
            /* [in] */ const wchar_t *wszObjFile,
            /* [out] */ BYTE **ppmod) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE CloseMod( 
            /* [in] */ BYTE *pmod) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE ModAddSymbols( 
            /* [in] */ BYTE *pmod,
            /* [in] */ BYTE *pbSym,
            /* [in] */ long cb) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE ModAddSecContribEx( 
            /* [in] */ BYTE *pmod,
            /* [in] */ USHORT isect,
            /* [in] */ long off,
            /* [in] */ long cb,
            /* [in] */ ULONG dwCharacteristics,
            /* [in] */ DWORD dwDataCrc,
            /* [in] */ DWORD dwRelocCrc) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE QueryPDBNameExW( 
            /* [size_is][out] */ wchar_t wszPDB[  ],
            /* [in] */ SIZE_T cchMax) = 0;
        
    };
    
    
#else 	/* C style interface */

    typedef struct ISymNGenWriter2Vtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            ISymNGenWriter2 * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            ISymNGenWriter2 * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            ISymNGenWriter2 * This);
        
        HRESULT ( STDMETHODCALLTYPE *AddSymbol )( 
            ISymNGenWriter2 * This,
            /* [in] */ BSTR pSymbol,
            /* [in] */ USHORT iSection,
            /* [in] */ ULONGLONG rva);
        
        HRESULT ( STDMETHODCALLTYPE *AddSection )( 
            ISymNGenWriter2 * This,
            /* [in] */ USHORT iSection,
            /* [in] */ USHORT flags,
            /* [in] */ long offset,
            /* [in] */ long cb);
        
        HRESULT ( STDMETHODCALLTYPE *OpenModW )( 
            ISymNGenWriter2 * This,
            /* [in] */ const wchar_t *wszModule,
            /* [in] */ const wchar_t *wszObjFile,
            /* [out] */ BYTE **ppmod);
        
        HRESULT ( STDMETHODCALLTYPE *CloseMod )( 
            ISymNGenWriter2 * This,
            /* [in] */ BYTE *pmod);
        
        HRESULT ( STDMETHODCALLTYPE *ModAddSymbols )( 
            ISymNGenWriter2 * This,
            /* [in] */ BYTE *pmod,
            /* [in] */ BYTE *pbSym,
            /* [in] */ long cb);
        
        HRESULT ( STDMETHODCALLTYPE *ModAddSecContribEx )( 
            ISymNGenWriter2 * This,
            /* [in] */ BYTE *pmod,
            /* [in] */ USHORT isect,
            /* [in] */ long off,
            /* [in] */ long cb,
            /* [in] */ ULONG dwCharacteristics,
            /* [in] */ DWORD dwDataCrc,
            /* [in] */ DWORD dwRelocCrc);
        
        HRESULT ( STDMETHODCALLTYPE *QueryPDBNameExW )( 
            ISymNGenWriter2 * This,
            /* [size_is][out] */ wchar_t wszPDB[  ],
            /* [in] */ SIZE_T cchMax);
        
        END_INTERFACE
    } ISymNGenWriter2Vtbl;

    interface ISymNGenWriter2
    {
        CONST_VTBL struct ISymNGenWriter2Vtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define ISymNGenWriter2_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define ISymNGenWriter2_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define ISymNGenWriter2_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define ISymNGenWriter2_AddSymbol(This,pSymbol,iSection,rva)	\
    ( (This)->lpVtbl -> AddSymbol(This,pSymbol,iSection,rva) ) 

#define ISymNGenWriter2_AddSection(This,iSection,flags,offset,cb)	\
    ( (This)->lpVtbl -> AddSection(This,iSection,flags,offset,cb) ) 


#define ISymNGenWriter2_OpenModW(This,wszModule,wszObjFile,ppmod)	\
    ( (This)->lpVtbl -> OpenModW(This,wszModule,wszObjFile,ppmod) ) 

#define ISymNGenWriter2_CloseMod(This,pmod)	\
    ( (This)->lpVtbl -> CloseMod(This,pmod) ) 

#define ISymNGenWriter2_ModAddSymbols(This,pmod,pbSym,cb)	\
    ( (This)->lpVtbl -> ModAddSymbols(This,pmod,pbSym,cb) ) 

#define ISymNGenWriter2_ModAddSecContribEx(This,pmod,isect,off,cb,dwCharacteristics,dwDataCrc,dwRelocCrc)	\
    ( (This)->lpVtbl -> ModAddSecContribEx(This,pmod,isect,off,cb,dwCharacteristics,dwDataCrc,dwRelocCrc) ) 

#define ISymNGenWriter2_QueryPDBNameExW(This,wszPDB,cchMax)	\
    ( (This)->lpVtbl -> QueryPDBNameExW(This,wszPDB,cchMax) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ISymNGenWriter2_INTERFACE_DEFINED__ */


#ifndef __ISymUnmanagedAsyncMethodPropertiesWriter_INTERFACE_DEFINED__
#define __ISymUnmanagedAsyncMethodPropertiesWriter_INTERFACE_DEFINED__

/* interface ISymUnmanagedAsyncMethodPropertiesWriter */
/* [unique][uuid][object] */ 


EXTERN_C const IID IID_ISymUnmanagedAsyncMethodPropertiesWriter;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("FC073774-1739-4232-BD56-A027294BEC15")
    ISymUnmanagedAsyncMethodPropertiesWriter : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE DefineKickoffMethod( 
            /* [in] */ mdToken kickoffMethod) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE DefineCatchHandlerILOffset( 
            /* [in] */ ULONG32 catchHandlerOffset) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE DefineAsyncStepInfo( 
            /* [in] */ ULONG32 count,
            /* [size_is][in] */ __RPC__in_ecount_full(count) ULONG32 yieldOffsets[  ],
            /* [size_is][in] */ __RPC__in_ecount_full(count) ULONG32 breakpointOffset[  ],
            /* [size_is][in] */ __RPC__in_ecount_full(count) mdToken breakpointMethod[  ]) = 0;
        
    };
    
    
#else 	/* C style interface */

    typedef struct ISymUnmanagedAsyncMethodPropertiesWriterVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            __RPC__in ISymUnmanagedAsyncMethodPropertiesWriter * This,
            /* [in] */ __RPC__in REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            __RPC__in ISymUnmanagedAsyncMethodPropertiesWriter * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            __RPC__in ISymUnmanagedAsyncMethodPropertiesWriter * This);
        
        HRESULT ( STDMETHODCALLTYPE *DefineKickoffMethod )( 
            __RPC__in ISymUnmanagedAsyncMethodPropertiesWriter * This,
            /* [in] */ mdToken kickoffMethod);
        
        HRESULT ( STDMETHODCALLTYPE *DefineCatchHandlerILOffset )( 
            __RPC__in ISymUnmanagedAsyncMethodPropertiesWriter * This,
            /* [in] */ ULONG32 catchHandlerOffset);
        
        HRESULT ( STDMETHODCALLTYPE *DefineAsyncStepInfo )( 
            __RPC__in ISymUnmanagedAsyncMethodPropertiesWriter * This,
            /* [in] */ ULONG32 count,
            /* [size_is][in] */ __RPC__in_ecount_full(count) ULONG32 yieldOffsets[  ],
            /* [size_is][in] */ __RPC__in_ecount_full(count) ULONG32 breakpointOffset[  ],
            /* [size_is][in] */ __RPC__in_ecount_full(count) mdToken breakpointMethod[  ]);
        
        END_INTERFACE
    } ISymUnmanagedAsyncMethodPropertiesWriterVtbl;

    interface ISymUnmanagedAsyncMethodPropertiesWriter
    {
        CONST_VTBL struct ISymUnmanagedAsyncMethodPropertiesWriterVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define ISymUnmanagedAsyncMethodPropertiesWriter_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define ISymUnmanagedAsyncMethodPropertiesWriter_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define ISymUnmanagedAsyncMethodPropertiesWriter_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define ISymUnmanagedAsyncMethodPropertiesWriter_DefineKickoffMethod(This,kickoffMethod)	\
    ( (This)->lpVtbl -> DefineKickoffMethod(This,kickoffMethod) ) 

#define ISymUnmanagedAsyncMethodPropertiesWriter_DefineCatchHandlerILOffset(This,catchHandlerOffset)	\
    ( (This)->lpVtbl -> DefineCatchHandlerILOffset(This,catchHandlerOffset) ) 

#define ISymUnmanagedAsyncMethodPropertiesWriter_DefineAsyncStepInfo(This,count,yieldOffsets,breakpointOffset,breakpointMethod)	\
    ( (This)->lpVtbl -> DefineAsyncStepInfo(This,count,yieldOffsets,breakpointOffset,breakpointMethod) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ISymUnmanagedAsyncMethodPropertiesWriter_INTERFACE_DEFINED__ */


#ifndef __ISymUnmanagedAsyncMethod_INTERFACE_DEFINED__
#define __ISymUnmanagedAsyncMethod_INTERFACE_DEFINED__

/* interface ISymUnmanagedAsyncMethod */
/* [unique][uuid][object] */ 


EXTERN_C const IID IID_ISymUnmanagedAsyncMethod;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("B20D55B3-532E-4906-87E7-25BD5734ABD2")
    ISymUnmanagedAsyncMethod : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE IsAsyncMethod( 
            /* [retval][out] */ __RPC__out BOOL *pRetVal) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetKickoffMethod( 
            /* [retval][out] */ __RPC__out mdToken *kickoffMethod) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE HasCatchHandlerILOffset( 
            /* [retval][out] */ __RPC__out BOOL *pRetVal) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetCatchHandlerILOffset( 
            /* [retval][out] */ __RPC__out ULONG32 *pRetVal) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetAsyncStepInfoCount( 
            /* [retval][out] */ __RPC__out ULONG32 *pRetVal) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetAsyncStepInfo( 
            /* [in] */ ULONG32 cStepInfo,
            /* [out] */ __RPC__out ULONG32 *pcStepInfo,
            /* [size_is][in] */ __RPC__in_ecount_full(cStepInfo) ULONG32 yieldOffsets[  ],
            /* [size_is][in] */ __RPC__in_ecount_full(cStepInfo) ULONG32 breakpointOffset[  ],
            /* [size_is][in] */ __RPC__in_ecount_full(cStepInfo) mdToken breakpointMethod[  ]) = 0;
        
    };
    
    
#else 	/* C style interface */

    typedef struct ISymUnmanagedAsyncMethodVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            __RPC__in ISymUnmanagedAsyncMethod * This,
            /* [in] */ __RPC__in REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            __RPC__in ISymUnmanagedAsyncMethod * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            __RPC__in ISymUnmanagedAsyncMethod * This);
        
        HRESULT ( STDMETHODCALLTYPE *IsAsyncMethod )( 
            __RPC__in ISymUnmanagedAsyncMethod * This,
            /* [retval][out] */ __RPC__out BOOL *pRetVal);
        
        HRESULT ( STDMETHODCALLTYPE *GetKickoffMethod )( 
            __RPC__in ISymUnmanagedAsyncMethod * This,
            /* [retval][out] */ __RPC__out mdToken *kickoffMethod);
        
        HRESULT ( STDMETHODCALLTYPE *HasCatchHandlerILOffset )( 
            __RPC__in ISymUnmanagedAsyncMethod * This,
            /* [retval][out] */ __RPC__out BOOL *pRetVal);
        
        HRESULT ( STDMETHODCALLTYPE *GetCatchHandlerILOffset )( 
            __RPC__in ISymUnmanagedAsyncMethod * This,
            /* [retval][out] */ __RPC__out ULONG32 *pRetVal);
        
        HRESULT ( STDMETHODCALLTYPE *GetAsyncStepInfoCount )( 
            __RPC__in ISymUnmanagedAsyncMethod * This,
            /* [retval][out] */ __RPC__out ULONG32 *pRetVal);
        
        HRESULT ( STDMETHODCALLTYPE *GetAsyncStepInfo )( 
            __RPC__in ISymUnmanagedAsyncMethod * This,
            /* [in] */ ULONG32 cStepInfo,
            /* [out] */ __RPC__out ULONG32 *pcStepInfo,
            /* [size_is][in] */ __RPC__in_ecount_full(cStepInfo) ULONG32 yieldOffsets[  ],
            /* [size_is][in] */ __RPC__in_ecount_full(cStepInfo) ULONG32 breakpointOffset[  ],
            /* [size_is][in] */ __RPC__in_ecount_full(cStepInfo) mdToken breakpointMethod[  ]);
        
        END_INTERFACE
    } ISymUnmanagedAsyncMethodVtbl;

    interface ISymUnmanagedAsyncMethod
    {
        CONST_VTBL struct ISymUnmanagedAsyncMethodVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define ISymUnmanagedAsyncMethod_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define ISymUnmanagedAsyncMethod_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define ISymUnmanagedAsyncMethod_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define ISymUnmanagedAsyncMethod_IsAsyncMethod(This,pRetVal)	\
    ( (This)->lpVtbl -> IsAsyncMethod(This,pRetVal) ) 

#define ISymUnmanagedAsyncMethod_GetKickoffMethod(This,kickoffMethod)	\
    ( (This)->lpVtbl -> GetKickoffMethod(This,kickoffMethod) ) 

#define ISymUnmanagedAsyncMethod_HasCatchHandlerILOffset(This,pRetVal)	\
    ( (This)->lpVtbl -> HasCatchHandlerILOffset(This,pRetVal) ) 

#define ISymUnmanagedAsyncMethod_GetCatchHandlerILOffset(This,pRetVal)	\
    ( (This)->lpVtbl -> GetCatchHandlerILOffset(This,pRetVal) ) 

#define ISymUnmanagedAsyncMethod_GetAsyncStepInfoCount(This,pRetVal)	\
    ( (This)->lpVtbl -> GetAsyncStepInfoCount(This,pRetVal) ) 

#define ISymUnmanagedAsyncMethod_GetAsyncStepInfo(This,cStepInfo,pcStepInfo,yieldOffsets,breakpointOffset,breakpointMethod)	\
    ( (This)->lpVtbl -> GetAsyncStepInfo(This,cStepInfo,pcStepInfo,yieldOffsets,breakpointOffset,breakpointMethod) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ISymUnmanagedAsyncMethod_INTERFACE_DEFINED__ */


/* Additional Prototypes for ALL interfaces */

unsigned long             __RPC_USER  BSTR_UserSize(     __RPC__in unsigned long *, unsigned long            , __RPC__in BSTR * ); 
unsigned char * __RPC_USER  BSTR_UserMarshal(  __RPC__in unsigned long *, __RPC__inout_xcount(0) unsigned char *, __RPC__in BSTR * ); 
unsigned char * __RPC_USER  BSTR_UserUnmarshal(__RPC__in unsigned long *, __RPC__in_xcount(0) unsigned char *, __RPC__out BSTR * ); 
void                      __RPC_USER  BSTR_UserFree(     __RPC__in unsigned long *, __RPC__in BSTR * ); 

unsigned long             __RPC_USER  VARIANT_UserSize(     __RPC__in unsigned long *, unsigned long            , __RPC__in VARIANT * ); 
unsigned char * __RPC_USER  VARIANT_UserMarshal(  __RPC__in unsigned long *, __RPC__inout_xcount(0) unsigned char *, __RPC__in VARIANT * ); 
unsigned char * __RPC_USER  VARIANT_UserUnmarshal(__RPC__in unsigned long *, __RPC__in_xcount(0) unsigned char *, __RPC__out VARIANT * ); 
void                      __RPC_USER  VARIANT_UserFree(     __RPC__in unsigned long *, __RPC__in VARIANT * ); 

/* end of Additional Prototypes */

#ifdef __cplusplus
}
#endif

#endif


