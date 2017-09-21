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

#ifndef __xclrdata_h__
#define __xclrdata_h__

#if defined(_MSC_VER) && (_MSC_VER >= 1020)
#pragma once
#endif

/* Forward Declarations */ 

#ifndef __IXCLRDataTarget3_FWD_DEFINED__
#define __IXCLRDataTarget3_FWD_DEFINED__
typedef interface IXCLRDataTarget3 IXCLRDataTarget3;

#endif 	/* __IXCLRDataTarget3_FWD_DEFINED__ */


#ifndef __IXCLRLibrarySupport_FWD_DEFINED__
#define __IXCLRLibrarySupport_FWD_DEFINED__
typedef interface IXCLRLibrarySupport IXCLRLibrarySupport;

#endif 	/* __IXCLRLibrarySupport_FWD_DEFINED__ */


#ifndef __IXCLRDisassemblySupport_FWD_DEFINED__
#define __IXCLRDisassemblySupport_FWD_DEFINED__
typedef interface IXCLRDisassemblySupport IXCLRDisassemblySupport;

#endif 	/* __IXCLRDisassemblySupport_FWD_DEFINED__ */


#ifndef __IXCLRDataDisplay_FWD_DEFINED__
#define __IXCLRDataDisplay_FWD_DEFINED__
typedef interface IXCLRDataDisplay IXCLRDataDisplay;

#endif 	/* __IXCLRDataDisplay_FWD_DEFINED__ */


#ifndef __IXCLRDataProcess_FWD_DEFINED__
#define __IXCLRDataProcess_FWD_DEFINED__
typedef interface IXCLRDataProcess IXCLRDataProcess;

#endif 	/* __IXCLRDataProcess_FWD_DEFINED__ */


#ifndef __IXCLRDataProcess2_FWD_DEFINED__
#define __IXCLRDataProcess2_FWD_DEFINED__
typedef interface IXCLRDataProcess2 IXCLRDataProcess2;

#endif 	/* __IXCLRDataProcess2_FWD_DEFINED__ */


#ifndef __IXCLRDataAppDomain_FWD_DEFINED__
#define __IXCLRDataAppDomain_FWD_DEFINED__
typedef interface IXCLRDataAppDomain IXCLRDataAppDomain;

#endif 	/* __IXCLRDataAppDomain_FWD_DEFINED__ */


#ifndef __IXCLRDataAssembly_FWD_DEFINED__
#define __IXCLRDataAssembly_FWD_DEFINED__
typedef interface IXCLRDataAssembly IXCLRDataAssembly;

#endif 	/* __IXCLRDataAssembly_FWD_DEFINED__ */


#ifndef __IXCLRDataModule_FWD_DEFINED__
#define __IXCLRDataModule_FWD_DEFINED__
typedef interface IXCLRDataModule IXCLRDataModule;

#endif 	/* __IXCLRDataModule_FWD_DEFINED__ */


#ifndef __IXCLRDataModule2_FWD_DEFINED__
#define __IXCLRDataModule2_FWD_DEFINED__
typedef interface IXCLRDataModule2 IXCLRDataModule2;

#endif 	/* __IXCLRDataModule2_FWD_DEFINED__ */


#ifndef __IXCLRDataTypeDefinition_FWD_DEFINED__
#define __IXCLRDataTypeDefinition_FWD_DEFINED__
typedef interface IXCLRDataTypeDefinition IXCLRDataTypeDefinition;

#endif 	/* __IXCLRDataTypeDefinition_FWD_DEFINED__ */


#ifndef __IXCLRDataTypeInstance_FWD_DEFINED__
#define __IXCLRDataTypeInstance_FWD_DEFINED__
typedef interface IXCLRDataTypeInstance IXCLRDataTypeInstance;

#endif 	/* __IXCLRDataTypeInstance_FWD_DEFINED__ */


#ifndef __IXCLRDataMethodDefinition_FWD_DEFINED__
#define __IXCLRDataMethodDefinition_FWD_DEFINED__
typedef interface IXCLRDataMethodDefinition IXCLRDataMethodDefinition;

#endif 	/* __IXCLRDataMethodDefinition_FWD_DEFINED__ */


#ifndef __IXCLRDataMethodInstance_FWD_DEFINED__
#define __IXCLRDataMethodInstance_FWD_DEFINED__
typedef interface IXCLRDataMethodInstance IXCLRDataMethodInstance;

#endif 	/* __IXCLRDataMethodInstance_FWD_DEFINED__ */


#ifndef __IXCLRDataTask_FWD_DEFINED__
#define __IXCLRDataTask_FWD_DEFINED__
typedef interface IXCLRDataTask IXCLRDataTask;

#endif 	/* __IXCLRDataTask_FWD_DEFINED__ */


#ifndef __IXCLRDataStackWalk_FWD_DEFINED__
#define __IXCLRDataStackWalk_FWD_DEFINED__
typedef interface IXCLRDataStackWalk IXCLRDataStackWalk;

#endif 	/* __IXCLRDataStackWalk_FWD_DEFINED__ */


#ifndef __IXCLRDataFrame_FWD_DEFINED__
#define __IXCLRDataFrame_FWD_DEFINED__
typedef interface IXCLRDataFrame IXCLRDataFrame;

#endif 	/* __IXCLRDataFrame_FWD_DEFINED__ */


#ifndef __IXCLRDataFrame2_FWD_DEFINED__
#define __IXCLRDataFrame2_FWD_DEFINED__
typedef interface IXCLRDataFrame2 IXCLRDataFrame2;

#endif 	/* __IXCLRDataFrame2_FWD_DEFINED__ */


#ifndef __IXCLRDataExceptionState_FWD_DEFINED__
#define __IXCLRDataExceptionState_FWD_DEFINED__
typedef interface IXCLRDataExceptionState IXCLRDataExceptionState;

#endif 	/* __IXCLRDataExceptionState_FWD_DEFINED__ */


#ifndef __IXCLRDataValue_FWD_DEFINED__
#define __IXCLRDataValue_FWD_DEFINED__
typedef interface IXCLRDataValue IXCLRDataValue;

#endif 	/* __IXCLRDataValue_FWD_DEFINED__ */


#ifndef __IXCLRDataExceptionNotification_FWD_DEFINED__
#define __IXCLRDataExceptionNotification_FWD_DEFINED__
typedef interface IXCLRDataExceptionNotification IXCLRDataExceptionNotification;

#endif 	/* __IXCLRDataExceptionNotification_FWD_DEFINED__ */


#ifndef __IXCLRDataExceptionNotification2_FWD_DEFINED__
#define __IXCLRDataExceptionNotification2_FWD_DEFINED__
typedef interface IXCLRDataExceptionNotification2 IXCLRDataExceptionNotification2;

#endif 	/* __IXCLRDataExceptionNotification2_FWD_DEFINED__ */


#ifndef __IXCLRDataExceptionNotification3_FWD_DEFINED__
#define __IXCLRDataExceptionNotification3_FWD_DEFINED__
typedef interface IXCLRDataExceptionNotification3 IXCLRDataExceptionNotification3;

#endif 	/* __IXCLRDataExceptionNotification3_FWD_DEFINED__ */


#ifndef __IXCLRDataExceptionNotification4_FWD_DEFINED__
#define __IXCLRDataExceptionNotification4_FWD_DEFINED__
typedef interface IXCLRDataExceptionNotification4 IXCLRDataExceptionNotification4;

#endif 	/* __IXCLRDataExceptionNotification4_FWD_DEFINED__ */


/* header files for imported files */
#include "clrdata.h"

#ifdef __cplusplus
extern "C"{
#endif 


/* interface __MIDL_itf_xclrdata_0000_0000 */
/* [local] */ 

#if 0
typedef UINT32 mdToken;

typedef mdToken mdTypeDef;

typedef mdToken mdMethodDef;

typedef mdToken mdFieldDef;

typedef ULONG CorElementType;

typedef struct _EXCEPTION_RECORD64
    {
    DWORD ExceptionCode;
    DWORD ExceptionFlags;
    DWORD64 ExceptionRecord;
    DWORD64 ExceptionAddress;
    DWORD NumberParameters;
    DWORD __unusedAlignment;
    DWORD64 ExceptionInformation[ 15 ];
    } 	EXCEPTION_RECORD64;

typedef struct _EXCEPTION_RECORD64 *PEXCEPTION_RECORD64;

#endif
#pragma warning(push)
#pragma warning(disable:28718)    


















#pragma warning(pop)
typedef /* [public][public][public] */ struct __MIDL___MIDL_itf_xclrdata_0000_0000_0001
    {
    CLRDATA_ADDRESS startAddress;
    CLRDATA_ADDRESS endAddress;
    } 	CLRDATA_ADDRESS_RANGE;

typedef ULONG64 CLRDATA_ENUM;

#define CLRDATA_NOTIFY_EXCEPTION 0xe0444143
typedef /* [public] */ 
enum __MIDL___MIDL_itf_xclrdata_0000_0000_0002
    {
        CLRDATA_REQUEST_REVISION	= 0xe0000000
    } 	CLRDataGeneralRequest;

typedef /* [public] */ 
enum __MIDL___MIDL_itf_xclrdata_0000_0000_0003
    {
        CLRDATA_TYPE_DEFAULT	= 0,
        CLRDATA_TYPE_IS_PRIMITIVE	= 0x1,
        CLRDATA_TYPE_IS_VALUE_TYPE	= 0x2,
        CLRDATA_TYPE_IS_STRING	= 0x4,
        CLRDATA_TYPE_IS_ARRAY	= 0x8,
        CLRDATA_TYPE_IS_REFERENCE	= 0x10,
        CLRDATA_TYPE_IS_POINTER	= 0x20,
        CLRDATA_TYPE_IS_ENUM	= 0x40,
        CLRDATA_TYPE_ALL_KINDS	= 0x7f
    } 	CLRDataTypeFlag;

typedef /* [public] */ 
enum __MIDL___MIDL_itf_xclrdata_0000_0000_0004
    {
        CLRDATA_FIELD_DEFAULT	= 0,
        CLRDATA_FIELD_IS_PRIMITIVE	= CLRDATA_TYPE_IS_PRIMITIVE,
        CLRDATA_FIELD_IS_VALUE_TYPE	= CLRDATA_TYPE_IS_VALUE_TYPE,
        CLRDATA_FIELD_IS_STRING	= CLRDATA_TYPE_IS_STRING,
        CLRDATA_FIELD_IS_ARRAY	= CLRDATA_TYPE_IS_ARRAY,
        CLRDATA_FIELD_IS_REFERENCE	= CLRDATA_TYPE_IS_REFERENCE,
        CLRDATA_FIELD_IS_POINTER	= CLRDATA_TYPE_IS_POINTER,
        CLRDATA_FIELD_IS_ENUM	= CLRDATA_TYPE_IS_ENUM,
        CLRDATA_FIELD_ALL_KINDS	= CLRDATA_TYPE_ALL_KINDS,
        CLRDATA_FIELD_IS_INHERITED	= 0x80,
        CLRDATA_FIELD_IS_LITERAL	= 0x100,
        CLRDATA_FIELD_FROM_INSTANCE	= 0x200,
        CLRDATA_FIELD_FROM_TASK_LOCAL	= 0x400,
        CLRDATA_FIELD_FROM_STATIC	= 0x800,
        CLRDATA_FIELD_ALL_LOCATIONS	= 0xe00,
        CLRDATA_FIELD_ALL_FIELDS	= 0xeff
    } 	CLRDataFieldFlag;

typedef /* [public] */ 
enum __MIDL___MIDL_itf_xclrdata_0000_0000_0005
    {
        CLRDATA_VALUE_DEFAULT	= 0,
        CLRDATA_VALUE_IS_PRIMITIVE	= CLRDATA_TYPE_IS_PRIMITIVE,
        CLRDATA_VALUE_IS_VALUE_TYPE	= CLRDATA_TYPE_IS_VALUE_TYPE,
        CLRDATA_VALUE_IS_STRING	= CLRDATA_TYPE_IS_STRING,
        CLRDATA_VALUE_IS_ARRAY	= CLRDATA_TYPE_IS_ARRAY,
        CLRDATA_VALUE_IS_REFERENCE	= CLRDATA_TYPE_IS_REFERENCE,
        CLRDATA_VALUE_IS_POINTER	= CLRDATA_TYPE_IS_POINTER,
        CLRDATA_VALUE_IS_ENUM	= CLRDATA_TYPE_IS_ENUM,
        CLRDATA_VALUE_ALL_KINDS	= CLRDATA_TYPE_ALL_KINDS,
        CLRDATA_VALUE_IS_INHERITED	= CLRDATA_FIELD_IS_INHERITED,
        CLRDATA_VALUE_IS_LITERAL	= CLRDATA_FIELD_IS_LITERAL,
        CLRDATA_VALUE_FROM_INSTANCE	= CLRDATA_FIELD_FROM_INSTANCE,
        CLRDATA_VALUE_FROM_TASK_LOCAL	= CLRDATA_FIELD_FROM_TASK_LOCAL,
        CLRDATA_VALUE_FROM_STATIC	= CLRDATA_FIELD_FROM_STATIC,
        CLRDATA_VALUE_ALL_LOCATIONS	= CLRDATA_FIELD_ALL_LOCATIONS,
        CLRDATA_VALUE_ALL_FIELDS	= CLRDATA_FIELD_ALL_FIELDS,
        CLRDATA_VALUE_IS_BOXED	= 0x1000
    } 	CLRDataValueFlag;



extern RPC_IF_HANDLE __MIDL_itf_xclrdata_0000_0000_v0_0_c_ifspec;
extern RPC_IF_HANDLE __MIDL_itf_xclrdata_0000_0000_v0_0_s_ifspec;

#ifndef __IXCLRDataTarget3_INTERFACE_DEFINED__
#define __IXCLRDataTarget3_INTERFACE_DEFINED__

/* interface IXCLRDataTarget3 */
/* [unique][uuid][local][object] */ 


EXTERN_C const IID IID_IXCLRDataTarget3;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("59d9b5e1-4a6f-4531-84c3-51d12da22fd4")
    IXCLRDataTarget3 : public ICLRDataTarget2
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE GetMetaData( 
            /* [in] */ LPCWSTR imagePath,
            /* [in] */ ULONG32 imageTimestamp,
            /* [in] */ ULONG32 imageSize,
            /* [in] */ GUID *mvid,
            /* [in] */ ULONG32 mdRva,
            /* [in] */ ULONG32 flags,
            /* [in] */ ULONG32 bufferSize,
            /* [length_is][size_is][out] */ BYTE *buffer,
            /* [out] */ ULONG32 *dataSize) = 0;
        
    };
    
    
#else 	/* C style interface */

    typedef struct IXCLRDataTarget3Vtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            IXCLRDataTarget3 * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            IXCLRDataTarget3 * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            IXCLRDataTarget3 * This);
        
        HRESULT ( STDMETHODCALLTYPE *GetMachineType )( 
            IXCLRDataTarget3 * This,
            /* [out] */ ULONG32 *machineType);
        
        HRESULT ( STDMETHODCALLTYPE *GetPointerSize )( 
            IXCLRDataTarget3 * This,
            /* [out] */ ULONG32 *pointerSize);
        
        HRESULT ( STDMETHODCALLTYPE *GetImageBase )( 
            IXCLRDataTarget3 * This,
            /* [string][in] */ LPCWSTR imagePath,
            /* [out] */ CLRDATA_ADDRESS *baseAddress);
        
        HRESULT ( STDMETHODCALLTYPE *ReadVirtual )( 
            IXCLRDataTarget3 * This,
            /* [in] */ CLRDATA_ADDRESS address,
            /* [length_is][size_is][out] */ BYTE *buffer,
            /* [in] */ ULONG32 bytesRequested,
            /* [out] */ ULONG32 *bytesRead);
        
        HRESULT ( STDMETHODCALLTYPE *WriteVirtual )( 
            IXCLRDataTarget3 * This,
            /* [in] */ CLRDATA_ADDRESS address,
            /* [size_is][in] */ BYTE *buffer,
            /* [in] */ ULONG32 bytesRequested,
            /* [out] */ ULONG32 *bytesWritten);
        
        HRESULT ( STDMETHODCALLTYPE *GetTLSValue )( 
            IXCLRDataTarget3 * This,
            /* [in] */ ULONG32 threadID,
            /* [in] */ ULONG32 index,
            /* [out] */ CLRDATA_ADDRESS *value);
        
        HRESULT ( STDMETHODCALLTYPE *SetTLSValue )( 
            IXCLRDataTarget3 * This,
            /* [in] */ ULONG32 threadID,
            /* [in] */ ULONG32 index,
            /* [in] */ CLRDATA_ADDRESS value);
        
        HRESULT ( STDMETHODCALLTYPE *GetCurrentThreadID )( 
            IXCLRDataTarget3 * This,
            /* [out] */ ULONG32 *threadID);
        
        HRESULT ( STDMETHODCALLTYPE *GetThreadContext )( 
            IXCLRDataTarget3 * This,
            /* [in] */ ULONG32 threadID,
            /* [in] */ ULONG32 contextFlags,
            /* [in] */ ULONG32 contextSize,
            /* [size_is][out] */ BYTE *context);
        
        HRESULT ( STDMETHODCALLTYPE *SetThreadContext )( 
            IXCLRDataTarget3 * This,
            /* [in] */ ULONG32 threadID,
            /* [in] */ ULONG32 contextSize,
            /* [size_is][in] */ BYTE *context);
        
        HRESULT ( STDMETHODCALLTYPE *Request )( 
            IXCLRDataTarget3 * This,
            /* [in] */ ULONG32 reqCode,
            /* [in] */ ULONG32 inBufferSize,
            /* [size_is][in] */ BYTE *inBuffer,
            /* [in] */ ULONG32 outBufferSize,
            /* [size_is][out] */ BYTE *outBuffer);
        
        HRESULT ( STDMETHODCALLTYPE *AllocVirtual )( 
            IXCLRDataTarget3 * This,
            /* [in] */ CLRDATA_ADDRESS addr,
            /* [in] */ ULONG32 size,
            /* [in] */ ULONG32 typeFlags,
            /* [in] */ ULONG32 protectFlags,
            /* [out] */ CLRDATA_ADDRESS *virt);
        
        HRESULT ( STDMETHODCALLTYPE *FreeVirtual )( 
            IXCLRDataTarget3 * This,
            /* [in] */ CLRDATA_ADDRESS addr,
            /* [in] */ ULONG32 size,
            /* [in] */ ULONG32 typeFlags);
        
        HRESULT ( STDMETHODCALLTYPE *GetMetaData )( 
            IXCLRDataTarget3 * This,
            /* [in] */ LPCWSTR imagePath,
            /* [in] */ ULONG32 imageTimestamp,
            /* [in] */ ULONG32 imageSize,
            /* [in] */ GUID *mvid,
            /* [in] */ ULONG32 mdRva,
            /* [in] */ ULONG32 flags,
            /* [in] */ ULONG32 bufferSize,
            /* [length_is][size_is][out] */ BYTE *buffer,
            /* [out] */ ULONG32 *dataSize);
        
        END_INTERFACE
    } IXCLRDataTarget3Vtbl;

    interface IXCLRDataTarget3
    {
        CONST_VTBL struct IXCLRDataTarget3Vtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define IXCLRDataTarget3_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define IXCLRDataTarget3_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define IXCLRDataTarget3_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define IXCLRDataTarget3_GetMachineType(This,machineType)	\
    ( (This)->lpVtbl -> GetMachineType(This,machineType) ) 

#define IXCLRDataTarget3_GetPointerSize(This,pointerSize)	\
    ( (This)->lpVtbl -> GetPointerSize(This,pointerSize) ) 

#define IXCLRDataTarget3_GetImageBase(This,imagePath,baseAddress)	\
    ( (This)->lpVtbl -> GetImageBase(This,imagePath,baseAddress) ) 

#define IXCLRDataTarget3_ReadVirtual(This,address,buffer,bytesRequested,bytesRead)	\
    ( (This)->lpVtbl -> ReadVirtual(This,address,buffer,bytesRequested,bytesRead) ) 

#define IXCLRDataTarget3_WriteVirtual(This,address,buffer,bytesRequested,bytesWritten)	\
    ( (This)->lpVtbl -> WriteVirtual(This,address,buffer,bytesRequested,bytesWritten) ) 

#define IXCLRDataTarget3_GetTLSValue(This,threadID,index,value)	\
    ( (This)->lpVtbl -> GetTLSValue(This,threadID,index,value) ) 

#define IXCLRDataTarget3_SetTLSValue(This,threadID,index,value)	\
    ( (This)->lpVtbl -> SetTLSValue(This,threadID,index,value) ) 

#define IXCLRDataTarget3_GetCurrentThreadID(This,threadID)	\
    ( (This)->lpVtbl -> GetCurrentThreadID(This,threadID) ) 

#define IXCLRDataTarget3_GetThreadContext(This,threadID,contextFlags,contextSize,context)	\
    ( (This)->lpVtbl -> GetThreadContext(This,threadID,contextFlags,contextSize,context) ) 

#define IXCLRDataTarget3_SetThreadContext(This,threadID,contextSize,context)	\
    ( (This)->lpVtbl -> SetThreadContext(This,threadID,contextSize,context) ) 

#define IXCLRDataTarget3_Request(This,reqCode,inBufferSize,inBuffer,outBufferSize,outBuffer)	\
    ( (This)->lpVtbl -> Request(This,reqCode,inBufferSize,inBuffer,outBufferSize,outBuffer) ) 


#define IXCLRDataTarget3_AllocVirtual(This,addr,size,typeFlags,protectFlags,virt)	\
    ( (This)->lpVtbl -> AllocVirtual(This,addr,size,typeFlags,protectFlags,virt) ) 

#define IXCLRDataTarget3_FreeVirtual(This,addr,size,typeFlags)	\
    ( (This)->lpVtbl -> FreeVirtual(This,addr,size,typeFlags) ) 


#define IXCLRDataTarget3_GetMetaData(This,imagePath,imageTimestamp,imageSize,mvid,mdRva,flags,bufferSize,buffer,dataSize)	\
    ( (This)->lpVtbl -> GetMetaData(This,imagePath,imageTimestamp,imageSize,mvid,mdRva,flags,bufferSize,buffer,dataSize) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __IXCLRDataTarget3_INTERFACE_DEFINED__ */


/* interface __MIDL_itf_xclrdata_0000_0001 */
/* [local] */ 

typedef /* [public] */ 
enum __MIDL___MIDL_itf_xclrdata_0000_0001_0001
    {
        CLRDATA_BYNAME_CASE_SENSITIVE	= 0,
        CLRDATA_BYNAME_CASE_INSENSITIVE	= 0x1
    } 	CLRDataByNameFlag;

typedef /* [public] */ 
enum __MIDL___MIDL_itf_xclrdata_0000_0001_0002
    {
        CLRDATA_GETNAME_DEFAULT	= 0,
        CLRDATA_GETNAME_NO_NAMESPACES	= 0x1,
        CLRDATA_GETNAME_NO_PARAMETERS	= 0x2
    } 	CLRDataGetNameFlag;

typedef /* [public] */ 
enum __MIDL___MIDL_itf_xclrdata_0000_0001_0003
    {
        CLRDATA_PROCESS_DEFAULT	= 0,
        CLRDATA_PROCESS_IN_GC	= 0x1
    } 	CLRDataProcessFlag;

typedef /* [public][public] */ 
enum __MIDL___MIDL_itf_xclrdata_0000_0001_0004
    {
        CLRDATA_ADDRESS_UNRECOGNIZED	= 0,
        CLRDATA_ADDRESS_MANAGED_METHOD	= ( CLRDATA_ADDRESS_UNRECOGNIZED + 1 ) ,
        CLRDATA_ADDRESS_RUNTIME_MANAGED_CODE	= ( CLRDATA_ADDRESS_MANAGED_METHOD + 1 ) ,
        CLRDATA_ADDRESS_RUNTIME_UNMANAGED_CODE	= ( CLRDATA_ADDRESS_RUNTIME_MANAGED_CODE + 1 ) ,
        CLRDATA_ADDRESS_GC_DATA	= ( CLRDATA_ADDRESS_RUNTIME_UNMANAGED_CODE + 1 ) ,
        CLRDATA_ADDRESS_RUNTIME_MANAGED_STUB	= ( CLRDATA_ADDRESS_GC_DATA + 1 ) ,
        CLRDATA_ADDRESS_RUNTIME_UNMANAGED_STUB	= ( CLRDATA_ADDRESS_RUNTIME_MANAGED_STUB + 1 ) 
    } 	CLRDataAddressType;

typedef /* [public] */ 
enum __MIDL___MIDL_itf_xclrdata_0000_0001_0005
    {
        CLRDATA_NOTIFY_ON_MODULE_LOAD	= 0x1,
        CLRDATA_NOTIFY_ON_MODULE_UNLOAD	= 0x2,
        CLRDATA_NOTIFY_ON_EXCEPTION	= 0x4,
        CLRDATA_NOTIFY_ON_EXCEPTION_CATCH_ENTER	= 0x8
    } 	CLRDataOtherNotifyFlag;

typedef /* [public][public][public][public][public] */ struct __MIDL___MIDL_itf_xclrdata_0000_0001_0006
    {
    ULONG64 Data[ 8 ];
    } 	CLRDATA_FOLLOW_STUB_BUFFER;

typedef /* [public] */ 
enum __MIDL___MIDL_itf_xclrdata_0000_0001_0007
    {
        CLRDATA_FOLLOW_STUB_DEFAULT	= 0
    } 	CLRDataFollowStubInFlag;

typedef /* [public] */ 
enum __MIDL___MIDL_itf_xclrdata_0000_0001_0008
    {
        CLRDATA_FOLLOW_STUB_INTERMEDIATE	= 0,
        CLRDATA_FOLLOW_STUB_EXIT	= 0x1
    } 	CLRDataFollowStubOutFlag;

typedef /* [public][public] */ 
enum __MIDL___MIDL_itf_xclrdata_0000_0001_0009
    {
        CLRNATIVEIMAGE_PE_INFO	= 0x1,
        CLRNATIVEIMAGE_COR_INFO	= 0x2,
        CLRNATIVEIMAGE_FIXUP_TABLES	= 0x4,
        CLRNATIVEIMAGE_FIXUP_HISTOGRAM	= 0x8,
        CLRNATIVEIMAGE_MODULE	= 0x10,
        CLRNATIVEIMAGE_METHODS	= 0x20,
        CLRNATIVEIMAGE_DISASSEMBLE_CODE	= 0x40,
        CLRNATIVEIMAGE_IL	= 0x80,
        CLRNATIVEIMAGE_METHODTABLES	= 0x100,
        CLRNATIVEIMAGE_NATIVE_INFO	= 0x200,
        CLRNATIVEIMAGE_MODULE_TABLES	= 0x400,
        CLRNATIVEIMAGE_FROZEN_SEGMENT	= 0x800,
        CLRNATIVEIMAGE_PE_FILE	= 0x1000,
        CLRNATIVEIMAGE_GC_INFO	= 0x2000,
        CLRNATIVEIMAGE_EECLASSES	= 0x4000,
        CLRNATIVEIMAGE_NATIVE_TABLES	= 0x8000,
        CLRNATIVEIMAGE_PRECODES	= 0x10000,
        CLRNATIVEIMAGE_TYPEDESCS	= 0x20000,
        CLRNATIVEIMAGE_VERBOSE_TYPES	= 0x40000,
        CLRNATIVEIMAGE_METHODDESCS	= 0x80000,
        CLRNATIVEIMAGE_METADATA	= 0x100000,
        CLRNATIVEIMAGE_DISABLE_NAMES	= 0x200000,
        CLRNATIVEIMAGE_DISABLE_REBASING	= 0x400000,
        CLRNATIVEIMAGE_SLIM_MODULE_TBLS	= 0x800000,
        CLRNATIVEIMAGE_RESOURCES	= 0x1000000,
        CLRNATIVEIMAGE_FILE_OFFSET	= 0x2000000,
        CLRNATIVEIMAGE_DEBUG_TRACE	= 0x4000000,
        CLRNATIVEIMAGE_RELOCATIONS	= 0x8000000,
        CLRNATIVEIMAGE_FIXUP_THUNKS	= 0x10000000,
        CLRNATIVEIMAGE_DEBUG_COVERAGE	= 0x80000000
    } 	CLRNativeImageDumpOptions;

#ifdef __cplusplus
inline CLRNativeImageDumpOptions operator|=(CLRNativeImageDumpOptions& lhs, CLRNativeImageDumpOptions rhs) { return (lhs = (CLRNativeImageDumpOptions)( ((unsigned)lhs) | ((unsigned)rhs) )); }
#endif
typedef /* [public] */ 
enum __MIDL___MIDL_itf_xclrdata_0000_0001_0010
    {
        CLRDATAHINT_DISPLAY_HINTS_NONE	= 0,
        CLRDATAHINT_DISPLAY_ARRAY_AS_TABLE	= 0x1,
        CLRDATAHINT_DISPLAY_ARRAY_AS_ARRAY	= 0x2,
        CLRDATAHINT_DISPLAY_ARRAY_AS_ARRAY_IDX	= 0x3,
        CLRDATAHINT_DISPLAY_ARRAY_AS_MAP	= 0x4,
        CLRDATAHINT_DISPLAY_ARRAY_HINT_MASK	= 0xff,
        CLRDATAHINT_DISPLAY_STRUCT_AS_TABLE	= 0x100,
        CLRDATAHINT_DISPLAY_STRUCT_HINT_MASK	= 0xff00,
        CLRDATAHINT_DISPLAY_SEP_TAB	= 0,
        CLRDATAHINT_DISPLAY_SEP_SPACE	= 0x1000000,
        CLRDATAHINT_DISPLAY_SEP_TAB_SPACE	= 0x2000000,
        CLRDATAHINT_DISPLAY_SEP_MASK	= 0xff000000
    } 	CLRDataDisplayHints;

#pragma warning(push)
#pragma warning(disable:28718)	


extern RPC_IF_HANDLE __MIDL_itf_xclrdata_0000_0001_v0_0_c_ifspec;
extern RPC_IF_HANDLE __MIDL_itf_xclrdata_0000_0001_v0_0_s_ifspec;

#ifndef __IXCLRLibrarySupport_INTERFACE_DEFINED__
#define __IXCLRLibrarySupport_INTERFACE_DEFINED__

/* interface IXCLRLibrarySupport */
/* [uuid][local][object] */ 


EXTERN_C const IID IID_IXCLRLibrarySupport;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("E5F3039D-2C0C-4230-A69E-12AF1C3E563C")
    IXCLRLibrarySupport : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE LoadHardboundDependency( 
            const WCHAR *name,
            REFGUID mvid,
            /* [out] */ SIZE_T *loadedBase) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE LoadSoftboundDependency( 
            const WCHAR *name,
            const BYTE *assemblymetadataBinding,
            const BYTE *hash,
            ULONG hashLength,
            /* [out] */ SIZE_T *loadedBase) = 0;
        
    };
    
    
#else 	/* C style interface */

    typedef struct IXCLRLibrarySupportVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            IXCLRLibrarySupport * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            IXCLRLibrarySupport * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            IXCLRLibrarySupport * This);
        
        HRESULT ( STDMETHODCALLTYPE *LoadHardboundDependency )( 
            IXCLRLibrarySupport * This,
            const WCHAR *name,
            REFGUID mvid,
            /* [out] */ SIZE_T *loadedBase);
        
        HRESULT ( STDMETHODCALLTYPE *LoadSoftboundDependency )( 
            IXCLRLibrarySupport * This,
            const WCHAR *name,
            const BYTE *assemblymetadataBinding,
            const BYTE *hash,
            ULONG hashLength,
            /* [out] */ SIZE_T *loadedBase);
        
        END_INTERFACE
    } IXCLRLibrarySupportVtbl;

    interface IXCLRLibrarySupport
    {
        CONST_VTBL struct IXCLRLibrarySupportVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define IXCLRLibrarySupport_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define IXCLRLibrarySupport_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define IXCLRLibrarySupport_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define IXCLRLibrarySupport_LoadHardboundDependency(This,name,mvid,loadedBase)	\
    ( (This)->lpVtbl -> LoadHardboundDependency(This,name,mvid,loadedBase) ) 

#define IXCLRLibrarySupport_LoadSoftboundDependency(This,name,assemblymetadataBinding,hash,hashLength,loadedBase)	\
    ( (This)->lpVtbl -> LoadSoftboundDependency(This,name,assemblymetadataBinding,hash,hashLength,loadedBase) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __IXCLRLibrarySupport_INTERFACE_DEFINED__ */


/* interface __MIDL_itf_xclrdata_0000_0002 */
/* [local] */ 


typedef SIZE_T ( __stdcall *CDSTranslateAddrCB )( 
    IXCLRDisassemblySupport *__MIDL____MIDL_itf_xclrdata_0000_00020000,
    CLRDATA_ADDRESS __MIDL____MIDL_itf_xclrdata_0000_00020001,
    wchar_t *__MIDL____MIDL_itf_xclrdata_0000_00020002,
    SIZE_T __MIDL____MIDL_itf_xclrdata_0000_00020003,
    DWORDLONG *__MIDL____MIDL_itf_xclrdata_0000_00020004);

typedef SIZE_T ( __stdcall *CDSTranslateFixupCB )( 
    IXCLRDisassemblySupport *__MIDL____MIDL_itf_xclrdata_0000_00020006,
    CLRDATA_ADDRESS __MIDL____MIDL_itf_xclrdata_0000_00020007,
    SIZE_T __MIDL____MIDL_itf_xclrdata_0000_00020008,
    wchar_t *__MIDL____MIDL_itf_xclrdata_0000_00020009,
    SIZE_T __MIDL____MIDL_itf_xclrdata_0000_00020010,
    DWORDLONG *__MIDL____MIDL_itf_xclrdata_0000_00020011);

typedef SIZE_T ( __stdcall *CDSTranslateConstCB )( 
    IXCLRDisassemblySupport *__MIDL____MIDL_itf_xclrdata_0000_00020013,
    DWORD __MIDL____MIDL_itf_xclrdata_0000_00020014,
    wchar_t *__MIDL____MIDL_itf_xclrdata_0000_00020015,
    SIZE_T __MIDL____MIDL_itf_xclrdata_0000_00020016);

typedef SIZE_T ( __stdcall *CDSTranslateRegrelCB )( 
    IXCLRDisassemblySupport *__MIDL____MIDL_itf_xclrdata_0000_00020018,
    unsigned int rega,
    CLRDATA_ADDRESS __MIDL____MIDL_itf_xclrdata_0000_00020019,
    wchar_t *__MIDL____MIDL_itf_xclrdata_0000_00020020,
    SIZE_T __MIDL____MIDL_itf_xclrdata_0000_00020021,
    DWORD *__MIDL____MIDL_itf_xclrdata_0000_00020022);



extern RPC_IF_HANDLE __MIDL_itf_xclrdata_0000_0002_v0_0_c_ifspec;
extern RPC_IF_HANDLE __MIDL_itf_xclrdata_0000_0002_v0_0_s_ifspec;

#ifndef __IXCLRDisassemblySupport_INTERFACE_DEFINED__
#define __IXCLRDisassemblySupport_INTERFACE_DEFINED__

/* interface IXCLRDisassemblySupport */
/* [uuid][local][object] */ 


EXTERN_C const IID IID_IXCLRDisassemblySupport;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("1F0F7134-D3F3-47DE-8E9B-C2FD358A2936")
    IXCLRDisassemblySupport : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE SetTranslateAddrCallback( 
            /* [in] */ CDSTranslateAddrCB cb) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE PvClientSet( 
            /* [in] */ void *pv) = 0;
        
        virtual SIZE_T STDMETHODCALLTYPE CbDisassemble( 
            CLRDATA_ADDRESS __MIDL__IXCLRDisassemblySupport0000,
            const void *__MIDL__IXCLRDisassemblySupport0001,
            SIZE_T __MIDL__IXCLRDisassemblySupport0002) = 0;
        
        virtual SIZE_T STDMETHODCALLTYPE Cinstruction( void) = 0;
        
        virtual BOOL STDMETHODCALLTYPE FSelectInstruction( 
            SIZE_T __MIDL__IXCLRDisassemblySupport0003) = 0;
        
        virtual SIZE_T STDMETHODCALLTYPE CchFormatInstr( 
            wchar_t *__MIDL__IXCLRDisassemblySupport0004,
            SIZE_T __MIDL__IXCLRDisassemblySupport0005) = 0;
        
        virtual void *STDMETHODCALLTYPE PvClient( void) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE SetTranslateFixupCallback( 
            /* [in] */ CDSTranslateFixupCB cb) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE SetTranslateConstCallback( 
            /* [in] */ CDSTranslateConstCB cb) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE SetTranslateRegrelCallback( 
            /* [in] */ CDSTranslateRegrelCB cb) = 0;
        
        virtual BOOL STDMETHODCALLTYPE TargetIsAddress( void) = 0;
        
    };
    
    
#else 	/* C style interface */

    typedef struct IXCLRDisassemblySupportVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            IXCLRDisassemblySupport * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            IXCLRDisassemblySupport * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            IXCLRDisassemblySupport * This);
        
        HRESULT ( STDMETHODCALLTYPE *SetTranslateAddrCallback )( 
            IXCLRDisassemblySupport * This,
            /* [in] */ CDSTranslateAddrCB cb);
        
        HRESULT ( STDMETHODCALLTYPE *PvClientSet )( 
            IXCLRDisassemblySupport * This,
            /* [in] */ void *pv);
        
        SIZE_T ( STDMETHODCALLTYPE *CbDisassemble )( 
            IXCLRDisassemblySupport * This,
            CLRDATA_ADDRESS __MIDL__IXCLRDisassemblySupport0000,
            const void *__MIDL__IXCLRDisassemblySupport0001,
            SIZE_T __MIDL__IXCLRDisassemblySupport0002);
        
        SIZE_T ( STDMETHODCALLTYPE *Cinstruction )( 
            IXCLRDisassemblySupport * This);
        
        BOOL ( STDMETHODCALLTYPE *FSelectInstruction )( 
            IXCLRDisassemblySupport * This,
            SIZE_T __MIDL__IXCLRDisassemblySupport0003);
        
        SIZE_T ( STDMETHODCALLTYPE *CchFormatInstr )( 
            IXCLRDisassemblySupport * This,
            wchar_t *__MIDL__IXCLRDisassemblySupport0004,
            SIZE_T __MIDL__IXCLRDisassemblySupport0005);
        
        void *( STDMETHODCALLTYPE *PvClient )( 
            IXCLRDisassemblySupport * This);
        
        HRESULT ( STDMETHODCALLTYPE *SetTranslateFixupCallback )( 
            IXCLRDisassemblySupport * This,
            /* [in] */ CDSTranslateFixupCB cb);
        
        HRESULT ( STDMETHODCALLTYPE *SetTranslateConstCallback )( 
            IXCLRDisassemblySupport * This,
            /* [in] */ CDSTranslateConstCB cb);
        
        HRESULT ( STDMETHODCALLTYPE *SetTranslateRegrelCallback )( 
            IXCLRDisassemblySupport * This,
            /* [in] */ CDSTranslateRegrelCB cb);
        
        BOOL ( STDMETHODCALLTYPE *TargetIsAddress )( 
            IXCLRDisassemblySupport * This);
        
        END_INTERFACE
    } IXCLRDisassemblySupportVtbl;

    interface IXCLRDisassemblySupport
    {
        CONST_VTBL struct IXCLRDisassemblySupportVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define IXCLRDisassemblySupport_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define IXCLRDisassemblySupport_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define IXCLRDisassemblySupport_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define IXCLRDisassemblySupport_SetTranslateAddrCallback(This,cb)	\
    ( (This)->lpVtbl -> SetTranslateAddrCallback(This,cb) ) 

#define IXCLRDisassemblySupport_PvClientSet(This,pv)	\
    ( (This)->lpVtbl -> PvClientSet(This,pv) ) 

#define IXCLRDisassemblySupport_CbDisassemble(This,__MIDL__IXCLRDisassemblySupport0000,__MIDL__IXCLRDisassemblySupport0001,__MIDL__IXCLRDisassemblySupport0002)	\
    ( (This)->lpVtbl -> CbDisassemble(This,__MIDL__IXCLRDisassemblySupport0000,__MIDL__IXCLRDisassemblySupport0001,__MIDL__IXCLRDisassemblySupport0002) ) 

#define IXCLRDisassemblySupport_Cinstruction(This)	\
    ( (This)->lpVtbl -> Cinstruction(This) ) 

#define IXCLRDisassemblySupport_FSelectInstruction(This,__MIDL__IXCLRDisassemblySupport0003)	\
    ( (This)->lpVtbl -> FSelectInstruction(This,__MIDL__IXCLRDisassemblySupport0003) ) 

#define IXCLRDisassemblySupport_CchFormatInstr(This,__MIDL__IXCLRDisassemblySupport0004,__MIDL__IXCLRDisassemblySupport0005)	\
    ( (This)->lpVtbl -> CchFormatInstr(This,__MIDL__IXCLRDisassemblySupport0004,__MIDL__IXCLRDisassemblySupport0005) ) 

#define IXCLRDisassemblySupport_PvClient(This)	\
    ( (This)->lpVtbl -> PvClient(This) ) 

#define IXCLRDisassemblySupport_SetTranslateFixupCallback(This,cb)	\
    ( (This)->lpVtbl -> SetTranslateFixupCallback(This,cb) ) 

#define IXCLRDisassemblySupport_SetTranslateConstCallback(This,cb)	\
    ( (This)->lpVtbl -> SetTranslateConstCallback(This,cb) ) 

#define IXCLRDisassemblySupport_SetTranslateRegrelCallback(This,cb)	\
    ( (This)->lpVtbl -> SetTranslateRegrelCallback(This,cb) ) 

#define IXCLRDisassemblySupport_TargetIsAddress(This)	\
    ( (This)->lpVtbl -> TargetIsAddress(This) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __IXCLRDisassemblySupport_INTERFACE_DEFINED__ */


#ifndef __IXCLRDataDisplay_INTERFACE_DEFINED__
#define __IXCLRDataDisplay_INTERFACE_DEFINED__

/* interface IXCLRDataDisplay */
/* [uuid][local][object] */ 


EXTERN_C const IID IID_IXCLRDataDisplay;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("A3C1704A-4559-4a67-8D28-E8F4FE3B3F62")
    IXCLRDataDisplay : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODVCALLTYPE ErrorPrintF(
            const char *const fmt,
            ...) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE NativeImageDimensions( 
            SIZE_T base,
            SIZE_T size,
            DWORD sectionAlign) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE Section( 
            const char *const name,
            SIZE_T rva,
            SIZE_T size) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetDumpOptions( 
            /* [out] */ CLRNativeImageDumpOptions *pOptions) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE StartDocument( void) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE EndDocument( void) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE StartCategory( 
            const char *const name) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE EndCategory( void) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE StartElement( 
            const char *const name) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE EndElement( void) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE StartVStructure( 
            const char *const name) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE StartVStructureWithOffset( 
            const char *const name,
            unsigned int fieldOffset,
            unsigned int fieldSize) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE EndVStructure( void) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE StartTextElement( 
            const char *const name) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE EndTextElement( void) = 0;
        
        virtual HRESULT STDMETHODVCALLTYPE WriteXmlText(
            const char *const fmt,
            ...) = 0;
        
        virtual HRESULT STDMETHODVCALLTYPE WriteXmlTextBlock(
            const char *const fmt,
            ...) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE WriteEmptyElement( 
            const char *const element) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE WriteElementPointer( 
            const char *const element,
            SIZE_T ptr) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE WriteElementPointerAnnotated( 
            const char *const element,
            SIZE_T ptr,
            const WCHAR *const annotation) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE WriteElementAddress( 
            const char *const element,
            SIZE_T base,
            SIZE_T size) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE WriteElementAddressNamed( 
            const char *const element,
            const char *const name,
            SIZE_T base,
            SIZE_T size) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE WriteElementAddressNamedW( 
            const char *const element,
            const WCHAR *const name,
            SIZE_T base,
            SIZE_T size) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE WriteElementString( 
            const char *const element,
            const char *const data) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE WriteElementStringW( 
            const char *const element,
            const WCHAR *const data) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE WriteElementInt( 
            const char *const element,
            int value) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE WriteElementUInt( 
            const char *const element,
            DWORD value) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE WriteElementEnumerated( 
            const char *const element,
            DWORD value,
            const WCHAR *const mnemonic) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE WriteElementIntWithSuppress( 
            const char *const element,
            int value,
            int suppressIfEqual) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE WriteElementFlag( 
            const char *const element,
            BOOL flag) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE StartArray( 
            const char *const name,
            const WCHAR *const fmt) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE EndArray( 
            const char *const countPrefix) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE StartList( 
            const WCHAR *const fmt) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE EndList( void) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE StartArrayWithOffset( 
            const char *const name,
            unsigned int fieldOffset,
            unsigned int fieldSize,
            const WCHAR *const fmt) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE WriteFieldString( 
            const char *const element,
            unsigned int fieldOffset,
            unsigned int fieldSize,
            const char *const data) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE WriteFieldStringW( 
            const char *const element,
            unsigned int fieldOffset,
            unsigned int fieldSize,
            const WCHAR *const data) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE WriteFieldPointer( 
            const char *const element,
            unsigned int fieldOffset,
            unsigned int fieldSize,
            SIZE_T ptr) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE WriteFieldPointerWithSize( 
            const char *const element,
            unsigned int fieldOffset,
            unsigned int fieldSize,
            SIZE_T ptr,
            SIZE_T size) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE WriteFieldInt( 
            const char *const element,
            unsigned int fieldOffset,
            unsigned int fieldSize,
            int value) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE WriteFieldUInt( 
            const char *const element,
            unsigned int fieldOffset,
            unsigned int fieldSize,
            DWORD value) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE WriteFieldEnumerated( 
            const char *const element,
            unsigned int fieldOffset,
            unsigned int fieldSize,
            DWORD value,
            const WCHAR *const mnemonic) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE WriteFieldEmpty( 
            const char *const element,
            unsigned int fieldOffset,
            unsigned int fieldSize) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE WriteFieldFlag( 
            const char *const element,
            unsigned int fieldOffset,
            unsigned int fieldSize,
            BOOL flag) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE WriteFieldPointerAnnotated( 
            const char *const element,
            unsigned int fieldOffset,
            unsigned int fieldSize,
            SIZE_T ptr,
            const WCHAR *const annotation) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE WriteFieldAddress( 
            const char *const element,
            unsigned int fieldOffset,
            unsigned int fieldSize,
            SIZE_T base,
            SIZE_T size) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE StartStructure( 
            const char *const name,
            SIZE_T ptr,
            SIZE_T size) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE StartStructureWithNegSpace( 
            const char *const name,
            SIZE_T ptr,
            SIZE_T startPtr,
            SIZE_T totalSize) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE StartStructureWithOffset( 
            const char *const name,
            unsigned int fieldOffset,
            unsigned int fieldSize,
            SIZE_T ptr,
            SIZE_T size) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE EndStructure( void) = 0;
        
    };
    
    
#else 	/* C style interface */

    typedef struct IXCLRDataDisplayVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            IXCLRDataDisplay * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            IXCLRDataDisplay * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            IXCLRDataDisplay * This);
        
        HRESULT ( STDMETHODVCALLTYPE *ErrorPrintF )(
            IXCLRDataDisplay * This,
            const char *const fmt,
            ...);
        
        HRESULT ( STDMETHODCALLTYPE *NativeImageDimensions )( 
            IXCLRDataDisplay * This,
            SIZE_T base,
            SIZE_T size,
            DWORD sectionAlign);
        
        HRESULT ( STDMETHODCALLTYPE *Section )( 
            IXCLRDataDisplay * This,
            const char *const name,
            SIZE_T rva,
            SIZE_T size);
        
        HRESULT ( STDMETHODCALLTYPE *GetDumpOptions )( 
            IXCLRDataDisplay * This,
            /* [out] */ CLRNativeImageDumpOptions *pOptions);
        
        HRESULT ( STDMETHODCALLTYPE *StartDocument )( 
            IXCLRDataDisplay * This);
        
        HRESULT ( STDMETHODCALLTYPE *EndDocument )( 
            IXCLRDataDisplay * This);
        
        HRESULT ( STDMETHODCALLTYPE *StartCategory )( 
            IXCLRDataDisplay * This,
            const char *const name);
        
        HRESULT ( STDMETHODCALLTYPE *EndCategory )( 
            IXCLRDataDisplay * This);
        
        HRESULT ( STDMETHODCALLTYPE *StartElement )( 
            IXCLRDataDisplay * This,
            const char *const name);
        
        HRESULT ( STDMETHODCALLTYPE *EndElement )( 
            IXCLRDataDisplay * This);
        
        HRESULT ( STDMETHODCALLTYPE *StartVStructure )( 
            IXCLRDataDisplay * This,
            const char *const name);
        
        HRESULT ( STDMETHODCALLTYPE *StartVStructureWithOffset )( 
            IXCLRDataDisplay * This,
            const char *const name,
            unsigned int fieldOffset,
            unsigned int fieldSize);
        
        HRESULT ( STDMETHODCALLTYPE *EndVStructure )( 
            IXCLRDataDisplay * This);
        
        HRESULT ( STDMETHODCALLTYPE *StartTextElement )( 
            IXCLRDataDisplay * This,
            const char *const name);
        
        HRESULT ( STDMETHODCALLTYPE *EndTextElement )( 
            IXCLRDataDisplay * This);
        
        HRESULT ( STDMETHODVCALLTYPE *WriteXmlText )(
            IXCLRDataDisplay * This,
            const char *const fmt,
            ...);
        
        HRESULT ( STDMETHODVCALLTYPE *WriteXmlTextBlock )(
            IXCLRDataDisplay * This,
            const char *const fmt,
            ...);
        
        HRESULT ( STDMETHODCALLTYPE *WriteEmptyElement )( 
            IXCLRDataDisplay * This,
            const char *const element);
        
        HRESULT ( STDMETHODCALLTYPE *WriteElementPointer )( 
            IXCLRDataDisplay * This,
            const char *const element,
            SIZE_T ptr);
        
        HRESULT ( STDMETHODCALLTYPE *WriteElementPointerAnnotated )( 
            IXCLRDataDisplay * This,
            const char *const element,
            SIZE_T ptr,
            const WCHAR *const annotation);
        
        HRESULT ( STDMETHODCALLTYPE *WriteElementAddress )( 
            IXCLRDataDisplay * This,
            const char *const element,
            SIZE_T base,
            SIZE_T size);
        
        HRESULT ( STDMETHODCALLTYPE *WriteElementAddressNamed )( 
            IXCLRDataDisplay * This,
            const char *const element,
            const char *const name,
            SIZE_T base,
            SIZE_T size);
        
        HRESULT ( STDMETHODCALLTYPE *WriteElementAddressNamedW )( 
            IXCLRDataDisplay * This,
            const char *const element,
            const WCHAR *const name,
            SIZE_T base,
            SIZE_T size);
        
        HRESULT ( STDMETHODCALLTYPE *WriteElementString )( 
            IXCLRDataDisplay * This,
            const char *const element,
            const char *const data);
        
        HRESULT ( STDMETHODCALLTYPE *WriteElementStringW )( 
            IXCLRDataDisplay * This,
            const char *const element,
            const WCHAR *const data);
        
        HRESULT ( STDMETHODCALLTYPE *WriteElementInt )( 
            IXCLRDataDisplay * This,
            const char *const element,
            int value);
        
        HRESULT ( STDMETHODCALLTYPE *WriteElementUInt )( 
            IXCLRDataDisplay * This,
            const char *const element,
            DWORD value);
        
        HRESULT ( STDMETHODCALLTYPE *WriteElementEnumerated )( 
            IXCLRDataDisplay * This,
            const char *const element,
            DWORD value,
            const WCHAR *const mnemonic);
        
        HRESULT ( STDMETHODCALLTYPE *WriteElementIntWithSuppress )( 
            IXCLRDataDisplay * This,
            const char *const element,
            int value,
            int suppressIfEqual);
        
        HRESULT ( STDMETHODCALLTYPE *WriteElementFlag )( 
            IXCLRDataDisplay * This,
            const char *const element,
            BOOL flag);
        
        HRESULT ( STDMETHODCALLTYPE *StartArray )( 
            IXCLRDataDisplay * This,
            const char *const name,
            const WCHAR *const fmt);
        
        HRESULT ( STDMETHODCALLTYPE *EndArray )( 
            IXCLRDataDisplay * This,
            const char *const countPrefix);
        
        HRESULT ( STDMETHODCALLTYPE *StartList )( 
            IXCLRDataDisplay * This,
            const WCHAR *const fmt);
        
        HRESULT ( STDMETHODCALLTYPE *EndList )( 
            IXCLRDataDisplay * This);
        
        HRESULT ( STDMETHODCALLTYPE *StartArrayWithOffset )( 
            IXCLRDataDisplay * This,
            const char *const name,
            unsigned int fieldOffset,
            unsigned int fieldSize,
            const WCHAR *const fmt);
        
        HRESULT ( STDMETHODCALLTYPE *WriteFieldString )( 
            IXCLRDataDisplay * This,
            const char *const element,
            unsigned int fieldOffset,
            unsigned int fieldSize,
            const char *const data);
        
        HRESULT ( STDMETHODCALLTYPE *WriteFieldStringW )( 
            IXCLRDataDisplay * This,
            const char *const element,
            unsigned int fieldOffset,
            unsigned int fieldSize,
            const WCHAR *const data);
        
        HRESULT ( STDMETHODCALLTYPE *WriteFieldPointer )( 
            IXCLRDataDisplay * This,
            const char *const element,
            unsigned int fieldOffset,
            unsigned int fieldSize,
            SIZE_T ptr);
        
        HRESULT ( STDMETHODCALLTYPE *WriteFieldPointerWithSize )( 
            IXCLRDataDisplay * This,
            const char *const element,
            unsigned int fieldOffset,
            unsigned int fieldSize,
            SIZE_T ptr,
            SIZE_T size);
        
        HRESULT ( STDMETHODCALLTYPE *WriteFieldInt )( 
            IXCLRDataDisplay * This,
            const char *const element,
            unsigned int fieldOffset,
            unsigned int fieldSize,
            int value);
        
        HRESULT ( STDMETHODCALLTYPE *WriteFieldUInt )( 
            IXCLRDataDisplay * This,
            const char *const element,
            unsigned int fieldOffset,
            unsigned int fieldSize,
            DWORD value);
        
        HRESULT ( STDMETHODCALLTYPE *WriteFieldEnumerated )( 
            IXCLRDataDisplay * This,
            const char *const element,
            unsigned int fieldOffset,
            unsigned int fieldSize,
            DWORD value,
            const WCHAR *const mnemonic);
        
        HRESULT ( STDMETHODCALLTYPE *WriteFieldEmpty )( 
            IXCLRDataDisplay * This,
            const char *const element,
            unsigned int fieldOffset,
            unsigned int fieldSize);
        
        HRESULT ( STDMETHODCALLTYPE *WriteFieldFlag )( 
            IXCLRDataDisplay * This,
            const char *const element,
            unsigned int fieldOffset,
            unsigned int fieldSize,
            BOOL flag);
        
        HRESULT ( STDMETHODCALLTYPE *WriteFieldPointerAnnotated )( 
            IXCLRDataDisplay * This,
            const char *const element,
            unsigned int fieldOffset,
            unsigned int fieldSize,
            SIZE_T ptr,
            const WCHAR *const annotation);
        
        HRESULT ( STDMETHODCALLTYPE *WriteFieldAddress )( 
            IXCLRDataDisplay * This,
            const char *const element,
            unsigned int fieldOffset,
            unsigned int fieldSize,
            SIZE_T base,
            SIZE_T size);
        
        HRESULT ( STDMETHODCALLTYPE *StartStructure )( 
            IXCLRDataDisplay * This,
            const char *const name,
            SIZE_T ptr,
            SIZE_T size);
        
        HRESULT ( STDMETHODCALLTYPE *StartStructureWithNegSpace )( 
            IXCLRDataDisplay * This,
            const char *const name,
            SIZE_T ptr,
            SIZE_T startPtr,
            SIZE_T totalSize);
        
        HRESULT ( STDMETHODCALLTYPE *StartStructureWithOffset )( 
            IXCLRDataDisplay * This,
            const char *const name,
            unsigned int fieldOffset,
            unsigned int fieldSize,
            SIZE_T ptr,
            SIZE_T size);
        
        HRESULT ( STDMETHODCALLTYPE *EndStructure )( 
            IXCLRDataDisplay * This);
        
        END_INTERFACE
    } IXCLRDataDisplayVtbl;

    interface IXCLRDataDisplay
    {
        CONST_VTBL struct IXCLRDataDisplayVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define IXCLRDataDisplay_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define IXCLRDataDisplay_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define IXCLRDataDisplay_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define IXCLRDataDisplay_ErrorPrintF(This,fmt,...)	\
    ( (This)->lpVtbl -> ErrorPrintF(This,fmt,...) ) 

#define IXCLRDataDisplay_NativeImageDimensions(This,base,size,sectionAlign)	\
    ( (This)->lpVtbl -> NativeImageDimensions(This,base,size,sectionAlign) ) 

#define IXCLRDataDisplay_Section(This,name,rva,size)	\
    ( (This)->lpVtbl -> Section(This,name,rva,size) ) 

#define IXCLRDataDisplay_GetDumpOptions(This,pOptions)	\
    ( (This)->lpVtbl -> GetDumpOptions(This,pOptions) ) 

#define IXCLRDataDisplay_StartDocument(This)	\
    ( (This)->lpVtbl -> StartDocument(This) ) 

#define IXCLRDataDisplay_EndDocument(This)	\
    ( (This)->lpVtbl -> EndDocument(This) ) 

#define IXCLRDataDisplay_StartCategory(This,name)	\
    ( (This)->lpVtbl -> StartCategory(This,name) ) 

#define IXCLRDataDisplay_EndCategory(This)	\
    ( (This)->lpVtbl -> EndCategory(This) ) 

#define IXCLRDataDisplay_StartElement(This,name)	\
    ( (This)->lpVtbl -> StartElement(This,name) ) 

#define IXCLRDataDisplay_EndElement(This)	\
    ( (This)->lpVtbl -> EndElement(This) ) 

#define IXCLRDataDisplay_StartVStructure(This,name)	\
    ( (This)->lpVtbl -> StartVStructure(This,name) ) 

#define IXCLRDataDisplay_StartVStructureWithOffset(This,name,fieldOffset,fieldSize)	\
    ( (This)->lpVtbl -> StartVStructureWithOffset(This,name,fieldOffset,fieldSize) ) 

#define IXCLRDataDisplay_EndVStructure(This)	\
    ( (This)->lpVtbl -> EndVStructure(This) ) 

#define IXCLRDataDisplay_StartTextElement(This,name)	\
    ( (This)->lpVtbl -> StartTextElement(This,name) ) 

#define IXCLRDataDisplay_EndTextElement(This)	\
    ( (This)->lpVtbl -> EndTextElement(This) ) 

#define IXCLRDataDisplay_WriteXmlText(This,fmt,...)	\
    ( (This)->lpVtbl -> WriteXmlText(This,fmt,...) ) 

#define IXCLRDataDisplay_WriteXmlTextBlock(This,fmt,...)	\
    ( (This)->lpVtbl -> WriteXmlTextBlock(This,fmt,...) ) 

#define IXCLRDataDisplay_WriteEmptyElement(This,element)	\
    ( (This)->lpVtbl -> WriteEmptyElement(This,element) ) 

#define IXCLRDataDisplay_WriteElementPointer(This,element,ptr)	\
    ( (This)->lpVtbl -> WriteElementPointer(This,element,ptr) ) 

#define IXCLRDataDisplay_WriteElementPointerAnnotated(This,element,ptr,annotation)	\
    ( (This)->lpVtbl -> WriteElementPointerAnnotated(This,element,ptr,annotation) ) 

#define IXCLRDataDisplay_WriteElementAddress(This,element,base,size)	\
    ( (This)->lpVtbl -> WriteElementAddress(This,element,base,size) ) 

#define IXCLRDataDisplay_WriteElementAddressNamed(This,element,name,base,size)	\
    ( (This)->lpVtbl -> WriteElementAddressNamed(This,element,name,base,size) ) 

#define IXCLRDataDisplay_WriteElementAddressNamedW(This,element,name,base,size)	\
    ( (This)->lpVtbl -> WriteElementAddressNamedW(This,element,name,base,size) ) 

#define IXCLRDataDisplay_WriteElementString(This,element,data)	\
    ( (This)->lpVtbl -> WriteElementString(This,element,data) ) 

#define IXCLRDataDisplay_WriteElementStringW(This,element,data)	\
    ( (This)->lpVtbl -> WriteElementStringW(This,element,data) ) 

#define IXCLRDataDisplay_WriteElementInt(This,element,value)	\
    ( (This)->lpVtbl -> WriteElementInt(This,element,value) ) 

#define IXCLRDataDisplay_WriteElementUInt(This,element,value)	\
    ( (This)->lpVtbl -> WriteElementUInt(This,element,value) ) 

#define IXCLRDataDisplay_WriteElementEnumerated(This,element,value,mnemonic)	\
    ( (This)->lpVtbl -> WriteElementEnumerated(This,element,value,mnemonic) ) 

#define IXCLRDataDisplay_WriteElementIntWithSuppress(This,element,value,suppressIfEqual)	\
    ( (This)->lpVtbl -> WriteElementIntWithSuppress(This,element,value,suppressIfEqual) ) 

#define IXCLRDataDisplay_WriteElementFlag(This,element,flag)	\
    ( (This)->lpVtbl -> WriteElementFlag(This,element,flag) ) 

#define IXCLRDataDisplay_StartArray(This,name,fmt)	\
    ( (This)->lpVtbl -> StartArray(This,name,fmt) ) 

#define IXCLRDataDisplay_EndArray(This,countPrefix)	\
    ( (This)->lpVtbl -> EndArray(This,countPrefix) ) 

#define IXCLRDataDisplay_StartList(This,fmt)	\
    ( (This)->lpVtbl -> StartList(This,fmt) ) 

#define IXCLRDataDisplay_EndList(This)	\
    ( (This)->lpVtbl -> EndList(This) ) 

#define IXCLRDataDisplay_StartArrayWithOffset(This,name,fieldOffset,fieldSize,fmt)	\
    ( (This)->lpVtbl -> StartArrayWithOffset(This,name,fieldOffset,fieldSize,fmt) ) 

#define IXCLRDataDisplay_WriteFieldString(This,element,fieldOffset,fieldSize,data)	\
    ( (This)->lpVtbl -> WriteFieldString(This,element,fieldOffset,fieldSize,data) ) 

#define IXCLRDataDisplay_WriteFieldStringW(This,element,fieldOffset,fieldSize,data)	\
    ( (This)->lpVtbl -> WriteFieldStringW(This,element,fieldOffset,fieldSize,data) ) 

#define IXCLRDataDisplay_WriteFieldPointer(This,element,fieldOffset,fieldSize,ptr)	\
    ( (This)->lpVtbl -> WriteFieldPointer(This,element,fieldOffset,fieldSize,ptr) ) 

#define IXCLRDataDisplay_WriteFieldPointerWithSize(This,element,fieldOffset,fieldSize,ptr,size)	\
    ( (This)->lpVtbl -> WriteFieldPointerWithSize(This,element,fieldOffset,fieldSize,ptr,size) ) 

#define IXCLRDataDisplay_WriteFieldInt(This,element,fieldOffset,fieldSize,value)	\
    ( (This)->lpVtbl -> WriteFieldInt(This,element,fieldOffset,fieldSize,value) ) 

#define IXCLRDataDisplay_WriteFieldUInt(This,element,fieldOffset,fieldSize,value)	\
    ( (This)->lpVtbl -> WriteFieldUInt(This,element,fieldOffset,fieldSize,value) ) 

#define IXCLRDataDisplay_WriteFieldEnumerated(This,element,fieldOffset,fieldSize,value,mnemonic)	\
    ( (This)->lpVtbl -> WriteFieldEnumerated(This,element,fieldOffset,fieldSize,value,mnemonic) ) 

#define IXCLRDataDisplay_WriteFieldEmpty(This,element,fieldOffset,fieldSize)	\
    ( (This)->lpVtbl -> WriteFieldEmpty(This,element,fieldOffset,fieldSize) ) 

#define IXCLRDataDisplay_WriteFieldFlag(This,element,fieldOffset,fieldSize,flag)	\
    ( (This)->lpVtbl -> WriteFieldFlag(This,element,fieldOffset,fieldSize,flag) ) 

#define IXCLRDataDisplay_WriteFieldPointerAnnotated(This,element,fieldOffset,fieldSize,ptr,annotation)	\
    ( (This)->lpVtbl -> WriteFieldPointerAnnotated(This,element,fieldOffset,fieldSize,ptr,annotation) ) 

#define IXCLRDataDisplay_WriteFieldAddress(This,element,fieldOffset,fieldSize,base,size)	\
    ( (This)->lpVtbl -> WriteFieldAddress(This,element,fieldOffset,fieldSize,base,size) ) 

#define IXCLRDataDisplay_StartStructure(This,name,ptr,size)	\
    ( (This)->lpVtbl -> StartStructure(This,name,ptr,size) ) 

#define IXCLRDataDisplay_StartStructureWithNegSpace(This,name,ptr,startPtr,totalSize)	\
    ( (This)->lpVtbl -> StartStructureWithNegSpace(This,name,ptr,startPtr,totalSize) ) 

#define IXCLRDataDisplay_StartStructureWithOffset(This,name,fieldOffset,fieldSize,ptr,size)	\
    ( (This)->lpVtbl -> StartStructureWithOffset(This,name,fieldOffset,fieldSize,ptr,size) ) 

#define IXCLRDataDisplay_EndStructure(This)	\
    ( (This)->lpVtbl -> EndStructure(This) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __IXCLRDataDisplay_INTERFACE_DEFINED__ */


#ifndef __IXCLRDataProcess_INTERFACE_DEFINED__
#define __IXCLRDataProcess_INTERFACE_DEFINED__

/* interface IXCLRDataProcess */
/* [uuid][local][object] */ 


EXTERN_C const IID IID_IXCLRDataProcess;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("5c552ab6-fc09-4cb3-8e36-22fa03c798b7")
    IXCLRDataProcess : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE Flush( void) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE StartEnumTasks( 
            /* [out] */ CLRDATA_ENUM *handle) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE EnumTask( 
            /* [out][in] */ CLRDATA_ENUM *handle,
            /* [out] */ IXCLRDataTask **task) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE EndEnumTasks( 
            /* [in] */ CLRDATA_ENUM handle) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetTaskByOSThreadID( 
            /* [in] */ ULONG32 osThreadID,
            /* [out] */ IXCLRDataTask **task) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetTaskByUniqueID( 
            /* [in] */ ULONG64 taskID,
            /* [out] */ IXCLRDataTask **task) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetFlags( 
            /* [out] */ ULONG32 *flags) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE IsSameObject( 
            /* [in] */ IXCLRDataProcess *process) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetManagedObject( 
            /* [out] */ IXCLRDataValue **value) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetDesiredExecutionState( 
            /* [out] */ ULONG32 *state) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE SetDesiredExecutionState( 
            /* [in] */ ULONG32 state) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetAddressType( 
            /* [in] */ CLRDATA_ADDRESS address,
            /* [out] */ CLRDataAddressType *type) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetRuntimeNameByAddress( 
            /* [in] */ CLRDATA_ADDRESS address,
            /* [in] */ ULONG32 flags,
            /* [in] */ ULONG32 bufLen,
            /* [out] */ ULONG32 *nameLen,
            /* [size_is][out] */ WCHAR nameBuf[  ],
            /* [out] */ CLRDATA_ADDRESS *displacement) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE StartEnumAppDomains( 
            /* [out] */ CLRDATA_ENUM *handle) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE EnumAppDomain( 
            /* [out][in] */ CLRDATA_ENUM *handle,
            /* [out] */ IXCLRDataAppDomain **appDomain) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE EndEnumAppDomains( 
            /* [in] */ CLRDATA_ENUM handle) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetAppDomainByUniqueID( 
            /* [in] */ ULONG64 id,
            /* [out] */ IXCLRDataAppDomain **appDomain) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE StartEnumAssemblies( 
            /* [out] */ CLRDATA_ENUM *handle) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE EnumAssembly( 
            /* [out][in] */ CLRDATA_ENUM *handle,
            /* [out] */ IXCLRDataAssembly **assembly) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE EndEnumAssemblies( 
            /* [in] */ CLRDATA_ENUM handle) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE StartEnumModules( 
            /* [out] */ CLRDATA_ENUM *handle) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE EnumModule( 
            /* [out][in] */ CLRDATA_ENUM *handle,
            /* [out] */ IXCLRDataModule **mod) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE EndEnumModules( 
            /* [in] */ CLRDATA_ENUM handle) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetModuleByAddress( 
            /* [in] */ CLRDATA_ADDRESS address,
            /* [out] */ IXCLRDataModule **mod) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE StartEnumMethodInstancesByAddress( 
            /* [in] */ CLRDATA_ADDRESS address,
            /* [in] */ IXCLRDataAppDomain *appDomain,
            /* [out] */ CLRDATA_ENUM *handle) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE EnumMethodInstanceByAddress( 
            /* [in] */ CLRDATA_ENUM *handle,
            /* [out] */ IXCLRDataMethodInstance **method) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE EndEnumMethodInstancesByAddress( 
            /* [in] */ CLRDATA_ENUM handle) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetDataByAddress( 
            /* [in] */ CLRDATA_ADDRESS address,
            /* [in] */ ULONG32 flags,
            /* [in] */ IXCLRDataAppDomain *appDomain,
            /* [in] */ IXCLRDataTask *tlsTask,
            /* [in] */ ULONG32 bufLen,
            /* [out] */ ULONG32 *nameLen,
            /* [size_is][out] */ WCHAR nameBuf[  ],
            /* [out] */ IXCLRDataValue **value,
            /* [out] */ CLRDATA_ADDRESS *displacement) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetExceptionStateByExceptionRecord( 
            /* [in] */ EXCEPTION_RECORD64 *record,
            /* [out] */ IXCLRDataExceptionState **exState) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE TranslateExceptionRecordToNotification( 
            /* [in] */ EXCEPTION_RECORD64 *record,
            /* [in] */ IXCLRDataExceptionNotification *notify) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE Request( 
            /* [in] */ ULONG32 reqCode,
            /* [in] */ ULONG32 inBufferSize,
            /* [size_is][in] */ BYTE *inBuffer,
            /* [in] */ ULONG32 outBufferSize,
            /* [size_is][out] */ BYTE *outBuffer) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE CreateMemoryValue( 
            /* [in] */ IXCLRDataAppDomain *appDomain,
            /* [in] */ IXCLRDataTask *tlsTask,
            /* [in] */ IXCLRDataTypeInstance *type,
            /* [in] */ CLRDATA_ADDRESS addr,
            /* [out] */ IXCLRDataValue **value) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE SetAllTypeNotifications( 
            IXCLRDataModule *mod,
            ULONG32 flags) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE SetAllCodeNotifications( 
            IXCLRDataModule *mod,
            ULONG32 flags) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetTypeNotifications( 
            /* [in] */ ULONG32 numTokens,
            /* [size_is][in] */ IXCLRDataModule *mods[  ],
            /* [in] */ IXCLRDataModule *singleMod,
            /* [size_is][in] */ mdTypeDef tokens[  ],
            /* [size_is][out] */ ULONG32 flags[  ]) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE SetTypeNotifications( 
            /* [in] */ ULONG32 numTokens,
            /* [size_is][in] */ IXCLRDataModule *mods[  ],
            /* [in] */ IXCLRDataModule *singleMod,
            /* [size_is][in] */ mdTypeDef tokens[  ],
            /* [size_is][in] */ ULONG32 flags[  ],
            /* [in] */ ULONG32 singleFlags) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetCodeNotifications( 
            /* [in] */ ULONG32 numTokens,
            /* [size_is][in] */ IXCLRDataModule *mods[  ],
            /* [in] */ IXCLRDataModule *singleMod,
            /* [size_is][in] */ mdMethodDef tokens[  ],
            /* [size_is][out] */ ULONG32 flags[  ]) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE SetCodeNotifications( 
            /* [in] */ ULONG32 numTokens,
            /* [size_is][in] */ IXCLRDataModule *mods[  ],
            /* [in] */ IXCLRDataModule *singleMod,
            /* [size_is][in] */ mdMethodDef tokens[  ],
            /* [size_is][in] */ ULONG32 flags[  ],
            /* [in] */ ULONG32 singleFlags) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetOtherNotificationFlags( 
            /* [out] */ ULONG32 *flags) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE SetOtherNotificationFlags( 
            /* [in] */ ULONG32 flags) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE StartEnumMethodDefinitionsByAddress( 
            /* [in] */ CLRDATA_ADDRESS address,
            /* [out] */ CLRDATA_ENUM *handle) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE EnumMethodDefinitionByAddress( 
            /* [in] */ CLRDATA_ENUM *handle,
            /* [out] */ IXCLRDataMethodDefinition **method) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE EndEnumMethodDefinitionsByAddress( 
            /* [in] */ CLRDATA_ENUM handle) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE FollowStub( 
            /* [in] */ ULONG32 inFlags,
            /* [in] */ CLRDATA_ADDRESS inAddr,
            /* [in] */ CLRDATA_FOLLOW_STUB_BUFFER *inBuffer,
            /* [out] */ CLRDATA_ADDRESS *outAddr,
            /* [out] */ CLRDATA_FOLLOW_STUB_BUFFER *outBuffer,
            /* [out] */ ULONG32 *outFlags) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE FollowStub2( 
            /* [in] */ IXCLRDataTask *task,
            /* [in] */ ULONG32 inFlags,
            /* [in] */ CLRDATA_ADDRESS inAddr,
            /* [in] */ CLRDATA_FOLLOW_STUB_BUFFER *inBuffer,
            /* [out] */ CLRDATA_ADDRESS *outAddr,
            /* [out] */ CLRDATA_FOLLOW_STUB_BUFFER *outBuffer,
            /* [out] */ ULONG32 *outFlags) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE DumpNativeImage( 
            /* [in] */ CLRDATA_ADDRESS loadedBase,
            /* [in] */ LPCWSTR name,
            /* [in] */ IXCLRDataDisplay *display,
            /* [in] */ IXCLRLibrarySupport *libSupport,
            /* [in] */ IXCLRDisassemblySupport *dis) = 0;
        
    };
    
    
#else 	/* C style interface */

    typedef struct IXCLRDataProcessVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            IXCLRDataProcess * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            IXCLRDataProcess * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            IXCLRDataProcess * This);
        
        HRESULT ( STDMETHODCALLTYPE *Flush )( 
            IXCLRDataProcess * This);
        
        HRESULT ( STDMETHODCALLTYPE *StartEnumTasks )( 
            IXCLRDataProcess * This,
            /* [out] */ CLRDATA_ENUM *handle);
        
        HRESULT ( STDMETHODCALLTYPE *EnumTask )( 
            IXCLRDataProcess * This,
            /* [out][in] */ CLRDATA_ENUM *handle,
            /* [out] */ IXCLRDataTask **task);
        
        HRESULT ( STDMETHODCALLTYPE *EndEnumTasks )( 
            IXCLRDataProcess * This,
            /* [in] */ CLRDATA_ENUM handle);
        
        HRESULT ( STDMETHODCALLTYPE *GetTaskByOSThreadID )( 
            IXCLRDataProcess * This,
            /* [in] */ ULONG32 osThreadID,
            /* [out] */ IXCLRDataTask **task);
        
        HRESULT ( STDMETHODCALLTYPE *GetTaskByUniqueID )( 
            IXCLRDataProcess * This,
            /* [in] */ ULONG64 taskID,
            /* [out] */ IXCLRDataTask **task);
        
        HRESULT ( STDMETHODCALLTYPE *GetFlags )( 
            IXCLRDataProcess * This,
            /* [out] */ ULONG32 *flags);
        
        HRESULT ( STDMETHODCALLTYPE *IsSameObject )( 
            IXCLRDataProcess * This,
            /* [in] */ IXCLRDataProcess *process);
        
        HRESULT ( STDMETHODCALLTYPE *GetManagedObject )( 
            IXCLRDataProcess * This,
            /* [out] */ IXCLRDataValue **value);
        
        HRESULT ( STDMETHODCALLTYPE *GetDesiredExecutionState )( 
            IXCLRDataProcess * This,
            /* [out] */ ULONG32 *state);
        
        HRESULT ( STDMETHODCALLTYPE *SetDesiredExecutionState )( 
            IXCLRDataProcess * This,
            /* [in] */ ULONG32 state);
        
        HRESULT ( STDMETHODCALLTYPE *GetAddressType )( 
            IXCLRDataProcess * This,
            /* [in] */ CLRDATA_ADDRESS address,
            /* [out] */ CLRDataAddressType *type);
        
        HRESULT ( STDMETHODCALLTYPE *GetRuntimeNameByAddress )( 
            IXCLRDataProcess * This,
            /* [in] */ CLRDATA_ADDRESS address,
            /* [in] */ ULONG32 flags,
            /* [in] */ ULONG32 bufLen,
            /* [out] */ ULONG32 *nameLen,
            /* [size_is][out] */ WCHAR nameBuf[  ],
            /* [out] */ CLRDATA_ADDRESS *displacement);
        
        HRESULT ( STDMETHODCALLTYPE *StartEnumAppDomains )( 
            IXCLRDataProcess * This,
            /* [out] */ CLRDATA_ENUM *handle);
        
        HRESULT ( STDMETHODCALLTYPE *EnumAppDomain )( 
            IXCLRDataProcess * This,
            /* [out][in] */ CLRDATA_ENUM *handle,
            /* [out] */ IXCLRDataAppDomain **appDomain);
        
        HRESULT ( STDMETHODCALLTYPE *EndEnumAppDomains )( 
            IXCLRDataProcess * This,
            /* [in] */ CLRDATA_ENUM handle);
        
        HRESULT ( STDMETHODCALLTYPE *GetAppDomainByUniqueID )( 
            IXCLRDataProcess * This,
            /* [in] */ ULONG64 id,
            /* [out] */ IXCLRDataAppDomain **appDomain);
        
        HRESULT ( STDMETHODCALLTYPE *StartEnumAssemblies )( 
            IXCLRDataProcess * This,
            /* [out] */ CLRDATA_ENUM *handle);
        
        HRESULT ( STDMETHODCALLTYPE *EnumAssembly )( 
            IXCLRDataProcess * This,
            /* [out][in] */ CLRDATA_ENUM *handle,
            /* [out] */ IXCLRDataAssembly **assembly);
        
        HRESULT ( STDMETHODCALLTYPE *EndEnumAssemblies )( 
            IXCLRDataProcess * This,
            /* [in] */ CLRDATA_ENUM handle);
        
        HRESULT ( STDMETHODCALLTYPE *StartEnumModules )( 
            IXCLRDataProcess * This,
            /* [out] */ CLRDATA_ENUM *handle);
        
        HRESULT ( STDMETHODCALLTYPE *EnumModule )( 
            IXCLRDataProcess * This,
            /* [out][in] */ CLRDATA_ENUM *handle,
            /* [out] */ IXCLRDataModule **mod);
        
        HRESULT ( STDMETHODCALLTYPE *EndEnumModules )( 
            IXCLRDataProcess * This,
            /* [in] */ CLRDATA_ENUM handle);
        
        HRESULT ( STDMETHODCALLTYPE *GetModuleByAddress )( 
            IXCLRDataProcess * This,
            /* [in] */ CLRDATA_ADDRESS address,
            /* [out] */ IXCLRDataModule **mod);
        
        HRESULT ( STDMETHODCALLTYPE *StartEnumMethodInstancesByAddress )( 
            IXCLRDataProcess * This,
            /* [in] */ CLRDATA_ADDRESS address,
            /* [in] */ IXCLRDataAppDomain *appDomain,
            /* [out] */ CLRDATA_ENUM *handle);
        
        HRESULT ( STDMETHODCALLTYPE *EnumMethodInstanceByAddress )( 
            IXCLRDataProcess * This,
            /* [in] */ CLRDATA_ENUM *handle,
            /* [out] */ IXCLRDataMethodInstance **method);
        
        HRESULT ( STDMETHODCALLTYPE *EndEnumMethodInstancesByAddress )( 
            IXCLRDataProcess * This,
            /* [in] */ CLRDATA_ENUM handle);
        
        HRESULT ( STDMETHODCALLTYPE *GetDataByAddress )( 
            IXCLRDataProcess * This,
            /* [in] */ CLRDATA_ADDRESS address,
            /* [in] */ ULONG32 flags,
            /* [in] */ IXCLRDataAppDomain *appDomain,
            /* [in] */ IXCLRDataTask *tlsTask,
            /* [in] */ ULONG32 bufLen,
            /* [out] */ ULONG32 *nameLen,
            /* [size_is][out] */ WCHAR nameBuf[  ],
            /* [out] */ IXCLRDataValue **value,
            /* [out] */ CLRDATA_ADDRESS *displacement);
        
        HRESULT ( STDMETHODCALLTYPE *GetExceptionStateByExceptionRecord )( 
            IXCLRDataProcess * This,
            /* [in] */ EXCEPTION_RECORD64 *record,
            /* [out] */ IXCLRDataExceptionState **exState);
        
        HRESULT ( STDMETHODCALLTYPE *TranslateExceptionRecordToNotification )( 
            IXCLRDataProcess * This,
            /* [in] */ EXCEPTION_RECORD64 *record,
            /* [in] */ IXCLRDataExceptionNotification *notify);
        
        HRESULT ( STDMETHODCALLTYPE *Request )( 
            IXCLRDataProcess * This,
            /* [in] */ ULONG32 reqCode,
            /* [in] */ ULONG32 inBufferSize,
            /* [size_is][in] */ BYTE *inBuffer,
            /* [in] */ ULONG32 outBufferSize,
            /* [size_is][out] */ BYTE *outBuffer);
        
        HRESULT ( STDMETHODCALLTYPE *CreateMemoryValue )( 
            IXCLRDataProcess * This,
            /* [in] */ IXCLRDataAppDomain *appDomain,
            /* [in] */ IXCLRDataTask *tlsTask,
            /* [in] */ IXCLRDataTypeInstance *type,
            /* [in] */ CLRDATA_ADDRESS addr,
            /* [out] */ IXCLRDataValue **value);
        
        HRESULT ( STDMETHODCALLTYPE *SetAllTypeNotifications )( 
            IXCLRDataProcess * This,
            IXCLRDataModule *mod,
            ULONG32 flags);
        
        HRESULT ( STDMETHODCALLTYPE *SetAllCodeNotifications )( 
            IXCLRDataProcess * This,
            IXCLRDataModule *mod,
            ULONG32 flags);
        
        HRESULT ( STDMETHODCALLTYPE *GetTypeNotifications )( 
            IXCLRDataProcess * This,
            /* [in] */ ULONG32 numTokens,
            /* [size_is][in] */ IXCLRDataModule *mods[  ],
            /* [in] */ IXCLRDataModule *singleMod,
            /* [size_is][in] */ mdTypeDef tokens[  ],
            /* [size_is][out] */ ULONG32 flags[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *SetTypeNotifications )( 
            IXCLRDataProcess * This,
            /* [in] */ ULONG32 numTokens,
            /* [size_is][in] */ IXCLRDataModule *mods[  ],
            /* [in] */ IXCLRDataModule *singleMod,
            /* [size_is][in] */ mdTypeDef tokens[  ],
            /* [size_is][in] */ ULONG32 flags[  ],
            /* [in] */ ULONG32 singleFlags);
        
        HRESULT ( STDMETHODCALLTYPE *GetCodeNotifications )( 
            IXCLRDataProcess * This,
            /* [in] */ ULONG32 numTokens,
            /* [size_is][in] */ IXCLRDataModule *mods[  ],
            /* [in] */ IXCLRDataModule *singleMod,
            /* [size_is][in] */ mdMethodDef tokens[  ],
            /* [size_is][out] */ ULONG32 flags[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *SetCodeNotifications )( 
            IXCLRDataProcess * This,
            /* [in] */ ULONG32 numTokens,
            /* [size_is][in] */ IXCLRDataModule *mods[  ],
            /* [in] */ IXCLRDataModule *singleMod,
            /* [size_is][in] */ mdMethodDef tokens[  ],
            /* [size_is][in] */ ULONG32 flags[  ],
            /* [in] */ ULONG32 singleFlags);
        
        HRESULT ( STDMETHODCALLTYPE *GetOtherNotificationFlags )( 
            IXCLRDataProcess * This,
            /* [out] */ ULONG32 *flags);
        
        HRESULT ( STDMETHODCALLTYPE *SetOtherNotificationFlags )( 
            IXCLRDataProcess * This,
            /* [in] */ ULONG32 flags);
        
        HRESULT ( STDMETHODCALLTYPE *StartEnumMethodDefinitionsByAddress )( 
            IXCLRDataProcess * This,
            /* [in] */ CLRDATA_ADDRESS address,
            /* [out] */ CLRDATA_ENUM *handle);
        
        HRESULT ( STDMETHODCALLTYPE *EnumMethodDefinitionByAddress )( 
            IXCLRDataProcess * This,
            /* [in] */ CLRDATA_ENUM *handle,
            /* [out] */ IXCLRDataMethodDefinition **method);
        
        HRESULT ( STDMETHODCALLTYPE *EndEnumMethodDefinitionsByAddress )( 
            IXCLRDataProcess * This,
            /* [in] */ CLRDATA_ENUM handle);
        
        HRESULT ( STDMETHODCALLTYPE *FollowStub )( 
            IXCLRDataProcess * This,
            /* [in] */ ULONG32 inFlags,
            /* [in] */ CLRDATA_ADDRESS inAddr,
            /* [in] */ CLRDATA_FOLLOW_STUB_BUFFER *inBuffer,
            /* [out] */ CLRDATA_ADDRESS *outAddr,
            /* [out] */ CLRDATA_FOLLOW_STUB_BUFFER *outBuffer,
            /* [out] */ ULONG32 *outFlags);
        
        HRESULT ( STDMETHODCALLTYPE *FollowStub2 )( 
            IXCLRDataProcess * This,
            /* [in] */ IXCLRDataTask *task,
            /* [in] */ ULONG32 inFlags,
            /* [in] */ CLRDATA_ADDRESS inAddr,
            /* [in] */ CLRDATA_FOLLOW_STUB_BUFFER *inBuffer,
            /* [out] */ CLRDATA_ADDRESS *outAddr,
            /* [out] */ CLRDATA_FOLLOW_STUB_BUFFER *outBuffer,
            /* [out] */ ULONG32 *outFlags);
        
        HRESULT ( STDMETHODCALLTYPE *DumpNativeImage )( 
            IXCLRDataProcess * This,
            /* [in] */ CLRDATA_ADDRESS loadedBase,
            /* [in] */ LPCWSTR name,
            /* [in] */ IXCLRDataDisplay *display,
            /* [in] */ IXCLRLibrarySupport *libSupport,
            /* [in] */ IXCLRDisassemblySupport *dis);
        
        END_INTERFACE
    } IXCLRDataProcessVtbl;

    interface IXCLRDataProcess
    {
        CONST_VTBL struct IXCLRDataProcessVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define IXCLRDataProcess_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define IXCLRDataProcess_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define IXCLRDataProcess_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define IXCLRDataProcess_Flush(This)	\
    ( (This)->lpVtbl -> Flush(This) ) 

#define IXCLRDataProcess_StartEnumTasks(This,handle)	\
    ( (This)->lpVtbl -> StartEnumTasks(This,handle) ) 

#define IXCLRDataProcess_EnumTask(This,handle,task)	\
    ( (This)->lpVtbl -> EnumTask(This,handle,task) ) 

#define IXCLRDataProcess_EndEnumTasks(This,handle)	\
    ( (This)->lpVtbl -> EndEnumTasks(This,handle) ) 

#define IXCLRDataProcess_GetTaskByOSThreadID(This,osThreadID,task)	\
    ( (This)->lpVtbl -> GetTaskByOSThreadID(This,osThreadID,task) ) 

#define IXCLRDataProcess_GetTaskByUniqueID(This,taskID,task)	\
    ( (This)->lpVtbl -> GetTaskByUniqueID(This,taskID,task) ) 

#define IXCLRDataProcess_GetFlags(This,flags)	\
    ( (This)->lpVtbl -> GetFlags(This,flags) ) 

#define IXCLRDataProcess_IsSameObject(This,process)	\
    ( (This)->lpVtbl -> IsSameObject(This,process) ) 

#define IXCLRDataProcess_GetManagedObject(This,value)	\
    ( (This)->lpVtbl -> GetManagedObject(This,value) ) 

#define IXCLRDataProcess_GetDesiredExecutionState(This,state)	\
    ( (This)->lpVtbl -> GetDesiredExecutionState(This,state) ) 

#define IXCLRDataProcess_SetDesiredExecutionState(This,state)	\
    ( (This)->lpVtbl -> SetDesiredExecutionState(This,state) ) 

#define IXCLRDataProcess_GetAddressType(This,address,type)	\
    ( (This)->lpVtbl -> GetAddressType(This,address,type) ) 

#define IXCLRDataProcess_GetRuntimeNameByAddress(This,address,flags,bufLen,nameLen,nameBuf,displacement)	\
    ( (This)->lpVtbl -> GetRuntimeNameByAddress(This,address,flags,bufLen,nameLen,nameBuf,displacement) ) 

#define IXCLRDataProcess_StartEnumAppDomains(This,handle)	\
    ( (This)->lpVtbl -> StartEnumAppDomains(This,handle) ) 

#define IXCLRDataProcess_EnumAppDomain(This,handle,appDomain)	\
    ( (This)->lpVtbl -> EnumAppDomain(This,handle,appDomain) ) 

#define IXCLRDataProcess_EndEnumAppDomains(This,handle)	\
    ( (This)->lpVtbl -> EndEnumAppDomains(This,handle) ) 

#define IXCLRDataProcess_GetAppDomainByUniqueID(This,id,appDomain)	\
    ( (This)->lpVtbl -> GetAppDomainByUniqueID(This,id,appDomain) ) 

#define IXCLRDataProcess_StartEnumAssemblies(This,handle)	\
    ( (This)->lpVtbl -> StartEnumAssemblies(This,handle) ) 

#define IXCLRDataProcess_EnumAssembly(This,handle,assembly)	\
    ( (This)->lpVtbl -> EnumAssembly(This,handle,assembly) ) 

#define IXCLRDataProcess_EndEnumAssemblies(This,handle)	\
    ( (This)->lpVtbl -> EndEnumAssemblies(This,handle) ) 

#define IXCLRDataProcess_StartEnumModules(This,handle)	\
    ( (This)->lpVtbl -> StartEnumModules(This,handle) ) 

#define IXCLRDataProcess_EnumModule(This,handle,mod)	\
    ( (This)->lpVtbl -> EnumModule(This,handle,mod) ) 

#define IXCLRDataProcess_EndEnumModules(This,handle)	\
    ( (This)->lpVtbl -> EndEnumModules(This,handle) ) 

#define IXCLRDataProcess_GetModuleByAddress(This,address,mod)	\
    ( (This)->lpVtbl -> GetModuleByAddress(This,address,mod) ) 

#define IXCLRDataProcess_StartEnumMethodInstancesByAddress(This,address,appDomain,handle)	\
    ( (This)->lpVtbl -> StartEnumMethodInstancesByAddress(This,address,appDomain,handle) ) 

#define IXCLRDataProcess_EnumMethodInstanceByAddress(This,handle,method)	\
    ( (This)->lpVtbl -> EnumMethodInstanceByAddress(This,handle,method) ) 

#define IXCLRDataProcess_EndEnumMethodInstancesByAddress(This,handle)	\
    ( (This)->lpVtbl -> EndEnumMethodInstancesByAddress(This,handle) ) 

#define IXCLRDataProcess_GetDataByAddress(This,address,flags,appDomain,tlsTask,bufLen,nameLen,nameBuf,value,displacement)	\
    ( (This)->lpVtbl -> GetDataByAddress(This,address,flags,appDomain,tlsTask,bufLen,nameLen,nameBuf,value,displacement) ) 

#define IXCLRDataProcess_GetExceptionStateByExceptionRecord(This,record,exState)	\
    ( (This)->lpVtbl -> GetExceptionStateByExceptionRecord(This,record,exState) ) 

#define IXCLRDataProcess_TranslateExceptionRecordToNotification(This,record,notify)	\
    ( (This)->lpVtbl -> TranslateExceptionRecordToNotification(This,record,notify) ) 

#define IXCLRDataProcess_Request(This,reqCode,inBufferSize,inBuffer,outBufferSize,outBuffer)	\
    ( (This)->lpVtbl -> Request(This,reqCode,inBufferSize,inBuffer,outBufferSize,outBuffer) ) 

#define IXCLRDataProcess_CreateMemoryValue(This,appDomain,tlsTask,type,addr,value)	\
    ( (This)->lpVtbl -> CreateMemoryValue(This,appDomain,tlsTask,type,addr,value) ) 

#define IXCLRDataProcess_SetAllTypeNotifications(This,mod,flags)	\
    ( (This)->lpVtbl -> SetAllTypeNotifications(This,mod,flags) ) 

#define IXCLRDataProcess_SetAllCodeNotifications(This,mod,flags)	\
    ( (This)->lpVtbl -> SetAllCodeNotifications(This,mod,flags) ) 

#define IXCLRDataProcess_GetTypeNotifications(This,numTokens,mods,singleMod,tokens,flags)	\
    ( (This)->lpVtbl -> GetTypeNotifications(This,numTokens,mods,singleMod,tokens,flags) ) 

#define IXCLRDataProcess_SetTypeNotifications(This,numTokens,mods,singleMod,tokens,flags,singleFlags)	\
    ( (This)->lpVtbl -> SetTypeNotifications(This,numTokens,mods,singleMod,tokens,flags,singleFlags) ) 

#define IXCLRDataProcess_GetCodeNotifications(This,numTokens,mods,singleMod,tokens,flags)	\
    ( (This)->lpVtbl -> GetCodeNotifications(This,numTokens,mods,singleMod,tokens,flags) ) 

#define IXCLRDataProcess_SetCodeNotifications(This,numTokens,mods,singleMod,tokens,flags,singleFlags)	\
    ( (This)->lpVtbl -> SetCodeNotifications(This,numTokens,mods,singleMod,tokens,flags,singleFlags) ) 

#define IXCLRDataProcess_GetOtherNotificationFlags(This,flags)	\
    ( (This)->lpVtbl -> GetOtherNotificationFlags(This,flags) ) 

#define IXCLRDataProcess_SetOtherNotificationFlags(This,flags)	\
    ( (This)->lpVtbl -> SetOtherNotificationFlags(This,flags) ) 

#define IXCLRDataProcess_StartEnumMethodDefinitionsByAddress(This,address,handle)	\
    ( (This)->lpVtbl -> StartEnumMethodDefinitionsByAddress(This,address,handle) ) 

#define IXCLRDataProcess_EnumMethodDefinitionByAddress(This,handle,method)	\
    ( (This)->lpVtbl -> EnumMethodDefinitionByAddress(This,handle,method) ) 

#define IXCLRDataProcess_EndEnumMethodDefinitionsByAddress(This,handle)	\
    ( (This)->lpVtbl -> EndEnumMethodDefinitionsByAddress(This,handle) ) 

#define IXCLRDataProcess_FollowStub(This,inFlags,inAddr,inBuffer,outAddr,outBuffer,outFlags)	\
    ( (This)->lpVtbl -> FollowStub(This,inFlags,inAddr,inBuffer,outAddr,outBuffer,outFlags) ) 

#define IXCLRDataProcess_FollowStub2(This,task,inFlags,inAddr,inBuffer,outAddr,outBuffer,outFlags)	\
    ( (This)->lpVtbl -> FollowStub2(This,task,inFlags,inAddr,inBuffer,outAddr,outBuffer,outFlags) ) 

#define IXCLRDataProcess_DumpNativeImage(This,loadedBase,name,display,libSupport,dis)	\
    ( (This)->lpVtbl -> DumpNativeImage(This,loadedBase,name,display,libSupport,dis) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __IXCLRDataProcess_INTERFACE_DEFINED__ */


/* interface __MIDL_itf_xclrdata_0000_0005 */
/* [local] */ 

#pragma warning(pop)
typedef /* [public][public][public][public] */ 
enum __MIDL___MIDL_itf_xclrdata_0000_0005_0001
    {
        GC_MARK_END	= 1,
        GC_EVENT_TYPE_MAX	= ( GC_MARK_END + 1 ) 
    } 	GcEvt_t;

typedef /* [public][public][public][public] */ struct __MIDL___MIDL_itf_xclrdata_0000_0005_0002
    {
    GcEvt_t typ;
    /* [switch_is] */ /* [switch_type] */ union 
        {
        /* [case()] */ int condemnedGeneration;
        } 	;
    } 	GcEvtArgs;



extern RPC_IF_HANDLE __MIDL_itf_xclrdata_0000_0005_v0_0_c_ifspec;
extern RPC_IF_HANDLE __MIDL_itf_xclrdata_0000_0005_v0_0_s_ifspec;

#ifndef __IXCLRDataProcess2_INTERFACE_DEFINED__
#define __IXCLRDataProcess2_INTERFACE_DEFINED__

/* interface IXCLRDataProcess2 */
/* [uuid][local][object] */ 


EXTERN_C const IID IID_IXCLRDataProcess2;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("5c552ab6-fc09-4cb3-8e36-22fa03c798b8")
    IXCLRDataProcess2 : public IXCLRDataProcess
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE GetGcNotification( 
            /* [out][in] */ GcEvtArgs *gcEvtArgs) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE SetGcNotification( 
            /* [in] */ GcEvtArgs gcEvtArgs) = 0;
        
    };
    
    
#else 	/* C style interface */

    typedef struct IXCLRDataProcess2Vtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            IXCLRDataProcess2 * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            IXCLRDataProcess2 * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            IXCLRDataProcess2 * This);
        
        HRESULT ( STDMETHODCALLTYPE *Flush )( 
            IXCLRDataProcess2 * This);
        
        HRESULT ( STDMETHODCALLTYPE *StartEnumTasks )( 
            IXCLRDataProcess2 * This,
            /* [out] */ CLRDATA_ENUM *handle);
        
        HRESULT ( STDMETHODCALLTYPE *EnumTask )( 
            IXCLRDataProcess2 * This,
            /* [out][in] */ CLRDATA_ENUM *handle,
            /* [out] */ IXCLRDataTask **task);
        
        HRESULT ( STDMETHODCALLTYPE *EndEnumTasks )( 
            IXCLRDataProcess2 * This,
            /* [in] */ CLRDATA_ENUM handle);
        
        HRESULT ( STDMETHODCALLTYPE *GetTaskByOSThreadID )( 
            IXCLRDataProcess2 * This,
            /* [in] */ ULONG32 osThreadID,
            /* [out] */ IXCLRDataTask **task);
        
        HRESULT ( STDMETHODCALLTYPE *GetTaskByUniqueID )( 
            IXCLRDataProcess2 * This,
            /* [in] */ ULONG64 taskID,
            /* [out] */ IXCLRDataTask **task);
        
        HRESULT ( STDMETHODCALLTYPE *GetFlags )( 
            IXCLRDataProcess2 * This,
            /* [out] */ ULONG32 *flags);
        
        HRESULT ( STDMETHODCALLTYPE *IsSameObject )( 
            IXCLRDataProcess2 * This,
            /* [in] */ IXCLRDataProcess *process);
        
        HRESULT ( STDMETHODCALLTYPE *GetManagedObject )( 
            IXCLRDataProcess2 * This,
            /* [out] */ IXCLRDataValue **value);
        
        HRESULT ( STDMETHODCALLTYPE *GetDesiredExecutionState )( 
            IXCLRDataProcess2 * This,
            /* [out] */ ULONG32 *state);
        
        HRESULT ( STDMETHODCALLTYPE *SetDesiredExecutionState )( 
            IXCLRDataProcess2 * This,
            /* [in] */ ULONG32 state);
        
        HRESULT ( STDMETHODCALLTYPE *GetAddressType )( 
            IXCLRDataProcess2 * This,
            /* [in] */ CLRDATA_ADDRESS address,
            /* [out] */ CLRDataAddressType *type);
        
        HRESULT ( STDMETHODCALLTYPE *GetRuntimeNameByAddress )( 
            IXCLRDataProcess2 * This,
            /* [in] */ CLRDATA_ADDRESS address,
            /* [in] */ ULONG32 flags,
            /* [in] */ ULONG32 bufLen,
            /* [out] */ ULONG32 *nameLen,
            /* [size_is][out] */ WCHAR nameBuf[  ],
            /* [out] */ CLRDATA_ADDRESS *displacement);
        
        HRESULT ( STDMETHODCALLTYPE *StartEnumAppDomains )( 
            IXCLRDataProcess2 * This,
            /* [out] */ CLRDATA_ENUM *handle);
        
        HRESULT ( STDMETHODCALLTYPE *EnumAppDomain )( 
            IXCLRDataProcess2 * This,
            /* [out][in] */ CLRDATA_ENUM *handle,
            /* [out] */ IXCLRDataAppDomain **appDomain);
        
        HRESULT ( STDMETHODCALLTYPE *EndEnumAppDomains )( 
            IXCLRDataProcess2 * This,
            /* [in] */ CLRDATA_ENUM handle);
        
        HRESULT ( STDMETHODCALLTYPE *GetAppDomainByUniqueID )( 
            IXCLRDataProcess2 * This,
            /* [in] */ ULONG64 id,
            /* [out] */ IXCLRDataAppDomain **appDomain);
        
        HRESULT ( STDMETHODCALLTYPE *StartEnumAssemblies )( 
            IXCLRDataProcess2 * This,
            /* [out] */ CLRDATA_ENUM *handle);
        
        HRESULT ( STDMETHODCALLTYPE *EnumAssembly )( 
            IXCLRDataProcess2 * This,
            /* [out][in] */ CLRDATA_ENUM *handle,
            /* [out] */ IXCLRDataAssembly **assembly);
        
        HRESULT ( STDMETHODCALLTYPE *EndEnumAssemblies )( 
            IXCLRDataProcess2 * This,
            /* [in] */ CLRDATA_ENUM handle);
        
        HRESULT ( STDMETHODCALLTYPE *StartEnumModules )( 
            IXCLRDataProcess2 * This,
            /* [out] */ CLRDATA_ENUM *handle);
        
        HRESULT ( STDMETHODCALLTYPE *EnumModule )( 
            IXCLRDataProcess2 * This,
            /* [out][in] */ CLRDATA_ENUM *handle,
            /* [out] */ IXCLRDataModule **mod);
        
        HRESULT ( STDMETHODCALLTYPE *EndEnumModules )( 
            IXCLRDataProcess2 * This,
            /* [in] */ CLRDATA_ENUM handle);
        
        HRESULT ( STDMETHODCALLTYPE *GetModuleByAddress )( 
            IXCLRDataProcess2 * This,
            /* [in] */ CLRDATA_ADDRESS address,
            /* [out] */ IXCLRDataModule **mod);
        
        HRESULT ( STDMETHODCALLTYPE *StartEnumMethodInstancesByAddress )( 
            IXCLRDataProcess2 * This,
            /* [in] */ CLRDATA_ADDRESS address,
            /* [in] */ IXCLRDataAppDomain *appDomain,
            /* [out] */ CLRDATA_ENUM *handle);
        
        HRESULT ( STDMETHODCALLTYPE *EnumMethodInstanceByAddress )( 
            IXCLRDataProcess2 * This,
            /* [in] */ CLRDATA_ENUM *handle,
            /* [out] */ IXCLRDataMethodInstance **method);
        
        HRESULT ( STDMETHODCALLTYPE *EndEnumMethodInstancesByAddress )( 
            IXCLRDataProcess2 * This,
            /* [in] */ CLRDATA_ENUM handle);
        
        HRESULT ( STDMETHODCALLTYPE *GetDataByAddress )( 
            IXCLRDataProcess2 * This,
            /* [in] */ CLRDATA_ADDRESS address,
            /* [in] */ ULONG32 flags,
            /* [in] */ IXCLRDataAppDomain *appDomain,
            /* [in] */ IXCLRDataTask *tlsTask,
            /* [in] */ ULONG32 bufLen,
            /* [out] */ ULONG32 *nameLen,
            /* [size_is][out] */ WCHAR nameBuf[  ],
            /* [out] */ IXCLRDataValue **value,
            /* [out] */ CLRDATA_ADDRESS *displacement);
        
        HRESULT ( STDMETHODCALLTYPE *GetExceptionStateByExceptionRecord )( 
            IXCLRDataProcess2 * This,
            /* [in] */ EXCEPTION_RECORD64 *record,
            /* [out] */ IXCLRDataExceptionState **exState);
        
        HRESULT ( STDMETHODCALLTYPE *TranslateExceptionRecordToNotification )( 
            IXCLRDataProcess2 * This,
            /* [in] */ EXCEPTION_RECORD64 *record,
            /* [in] */ IXCLRDataExceptionNotification *notify);
        
        HRESULT ( STDMETHODCALLTYPE *Request )( 
            IXCLRDataProcess2 * This,
            /* [in] */ ULONG32 reqCode,
            /* [in] */ ULONG32 inBufferSize,
            /* [size_is][in] */ BYTE *inBuffer,
            /* [in] */ ULONG32 outBufferSize,
            /* [size_is][out] */ BYTE *outBuffer);
        
        HRESULT ( STDMETHODCALLTYPE *CreateMemoryValue )( 
            IXCLRDataProcess2 * This,
            /* [in] */ IXCLRDataAppDomain *appDomain,
            /* [in] */ IXCLRDataTask *tlsTask,
            /* [in] */ IXCLRDataTypeInstance *type,
            /* [in] */ CLRDATA_ADDRESS addr,
            /* [out] */ IXCLRDataValue **value);
        
        HRESULT ( STDMETHODCALLTYPE *SetAllTypeNotifications )( 
            IXCLRDataProcess2 * This,
            IXCLRDataModule *mod,
            ULONG32 flags);
        
        HRESULT ( STDMETHODCALLTYPE *SetAllCodeNotifications )( 
            IXCLRDataProcess2 * This,
            IXCLRDataModule *mod,
            ULONG32 flags);
        
        HRESULT ( STDMETHODCALLTYPE *GetTypeNotifications )( 
            IXCLRDataProcess2 * This,
            /* [in] */ ULONG32 numTokens,
            /* [size_is][in] */ IXCLRDataModule *mods[  ],
            /* [in] */ IXCLRDataModule *singleMod,
            /* [size_is][in] */ mdTypeDef tokens[  ],
            /* [size_is][out] */ ULONG32 flags[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *SetTypeNotifications )( 
            IXCLRDataProcess2 * This,
            /* [in] */ ULONG32 numTokens,
            /* [size_is][in] */ IXCLRDataModule *mods[  ],
            /* [in] */ IXCLRDataModule *singleMod,
            /* [size_is][in] */ mdTypeDef tokens[  ],
            /* [size_is][in] */ ULONG32 flags[  ],
            /* [in] */ ULONG32 singleFlags);
        
        HRESULT ( STDMETHODCALLTYPE *GetCodeNotifications )( 
            IXCLRDataProcess2 * This,
            /* [in] */ ULONG32 numTokens,
            /* [size_is][in] */ IXCLRDataModule *mods[  ],
            /* [in] */ IXCLRDataModule *singleMod,
            /* [size_is][in] */ mdMethodDef tokens[  ],
            /* [size_is][out] */ ULONG32 flags[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *SetCodeNotifications )( 
            IXCLRDataProcess2 * This,
            /* [in] */ ULONG32 numTokens,
            /* [size_is][in] */ IXCLRDataModule *mods[  ],
            /* [in] */ IXCLRDataModule *singleMod,
            /* [size_is][in] */ mdMethodDef tokens[  ],
            /* [size_is][in] */ ULONG32 flags[  ],
            /* [in] */ ULONG32 singleFlags);
        
        HRESULT ( STDMETHODCALLTYPE *GetOtherNotificationFlags )( 
            IXCLRDataProcess2 * This,
            /* [out] */ ULONG32 *flags);
        
        HRESULT ( STDMETHODCALLTYPE *SetOtherNotificationFlags )( 
            IXCLRDataProcess2 * This,
            /* [in] */ ULONG32 flags);
        
        HRESULT ( STDMETHODCALLTYPE *StartEnumMethodDefinitionsByAddress )( 
            IXCLRDataProcess2 * This,
            /* [in] */ CLRDATA_ADDRESS address,
            /* [out] */ CLRDATA_ENUM *handle);
        
        HRESULT ( STDMETHODCALLTYPE *EnumMethodDefinitionByAddress )( 
            IXCLRDataProcess2 * This,
            /* [in] */ CLRDATA_ENUM *handle,
            /* [out] */ IXCLRDataMethodDefinition **method);
        
        HRESULT ( STDMETHODCALLTYPE *EndEnumMethodDefinitionsByAddress )( 
            IXCLRDataProcess2 * This,
            /* [in] */ CLRDATA_ENUM handle);
        
        HRESULT ( STDMETHODCALLTYPE *FollowStub )( 
            IXCLRDataProcess2 * This,
            /* [in] */ ULONG32 inFlags,
            /* [in] */ CLRDATA_ADDRESS inAddr,
            /* [in] */ CLRDATA_FOLLOW_STUB_BUFFER *inBuffer,
            /* [out] */ CLRDATA_ADDRESS *outAddr,
            /* [out] */ CLRDATA_FOLLOW_STUB_BUFFER *outBuffer,
            /* [out] */ ULONG32 *outFlags);
        
        HRESULT ( STDMETHODCALLTYPE *FollowStub2 )( 
            IXCLRDataProcess2 * This,
            /* [in] */ IXCLRDataTask *task,
            /* [in] */ ULONG32 inFlags,
            /* [in] */ CLRDATA_ADDRESS inAddr,
            /* [in] */ CLRDATA_FOLLOW_STUB_BUFFER *inBuffer,
            /* [out] */ CLRDATA_ADDRESS *outAddr,
            /* [out] */ CLRDATA_FOLLOW_STUB_BUFFER *outBuffer,
            /* [out] */ ULONG32 *outFlags);
        
        HRESULT ( STDMETHODCALLTYPE *DumpNativeImage )( 
            IXCLRDataProcess2 * This,
            /* [in] */ CLRDATA_ADDRESS loadedBase,
            /* [in] */ LPCWSTR name,
            /* [in] */ IXCLRDataDisplay *display,
            /* [in] */ IXCLRLibrarySupport *libSupport,
            /* [in] */ IXCLRDisassemblySupport *dis);
        
        HRESULT ( STDMETHODCALLTYPE *GetGcNotification )( 
            IXCLRDataProcess2 * This,
            /* [out][in] */ GcEvtArgs *gcEvtArgs);
        
        HRESULT ( STDMETHODCALLTYPE *SetGcNotification )( 
            IXCLRDataProcess2 * This,
            /* [in] */ GcEvtArgs gcEvtArgs);
        
        END_INTERFACE
    } IXCLRDataProcess2Vtbl;

    interface IXCLRDataProcess2
    {
        CONST_VTBL struct IXCLRDataProcess2Vtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define IXCLRDataProcess2_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define IXCLRDataProcess2_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define IXCLRDataProcess2_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define IXCLRDataProcess2_Flush(This)	\
    ( (This)->lpVtbl -> Flush(This) ) 

#define IXCLRDataProcess2_StartEnumTasks(This,handle)	\
    ( (This)->lpVtbl -> StartEnumTasks(This,handle) ) 

#define IXCLRDataProcess2_EnumTask(This,handle,task)	\
    ( (This)->lpVtbl -> EnumTask(This,handle,task) ) 

#define IXCLRDataProcess2_EndEnumTasks(This,handle)	\
    ( (This)->lpVtbl -> EndEnumTasks(This,handle) ) 

#define IXCLRDataProcess2_GetTaskByOSThreadID(This,osThreadID,task)	\
    ( (This)->lpVtbl -> GetTaskByOSThreadID(This,osThreadID,task) ) 

#define IXCLRDataProcess2_GetTaskByUniqueID(This,taskID,task)	\
    ( (This)->lpVtbl -> GetTaskByUniqueID(This,taskID,task) ) 

#define IXCLRDataProcess2_GetFlags(This,flags)	\
    ( (This)->lpVtbl -> GetFlags(This,flags) ) 

#define IXCLRDataProcess2_IsSameObject(This,process)	\
    ( (This)->lpVtbl -> IsSameObject(This,process) ) 

#define IXCLRDataProcess2_GetManagedObject(This,value)	\
    ( (This)->lpVtbl -> GetManagedObject(This,value) ) 

#define IXCLRDataProcess2_GetDesiredExecutionState(This,state)	\
    ( (This)->lpVtbl -> GetDesiredExecutionState(This,state) ) 

#define IXCLRDataProcess2_SetDesiredExecutionState(This,state)	\
    ( (This)->lpVtbl -> SetDesiredExecutionState(This,state) ) 

#define IXCLRDataProcess2_GetAddressType(This,address,type)	\
    ( (This)->lpVtbl -> GetAddressType(This,address,type) ) 

#define IXCLRDataProcess2_GetRuntimeNameByAddress(This,address,flags,bufLen,nameLen,nameBuf,displacement)	\
    ( (This)->lpVtbl -> GetRuntimeNameByAddress(This,address,flags,bufLen,nameLen,nameBuf,displacement) ) 

#define IXCLRDataProcess2_StartEnumAppDomains(This,handle)	\
    ( (This)->lpVtbl -> StartEnumAppDomains(This,handle) ) 

#define IXCLRDataProcess2_EnumAppDomain(This,handle,appDomain)	\
    ( (This)->lpVtbl -> EnumAppDomain(This,handle,appDomain) ) 

#define IXCLRDataProcess2_EndEnumAppDomains(This,handle)	\
    ( (This)->lpVtbl -> EndEnumAppDomains(This,handle) ) 

#define IXCLRDataProcess2_GetAppDomainByUniqueID(This,id,appDomain)	\
    ( (This)->lpVtbl -> GetAppDomainByUniqueID(This,id,appDomain) ) 

#define IXCLRDataProcess2_StartEnumAssemblies(This,handle)	\
    ( (This)->lpVtbl -> StartEnumAssemblies(This,handle) ) 

#define IXCLRDataProcess2_EnumAssembly(This,handle,assembly)	\
    ( (This)->lpVtbl -> EnumAssembly(This,handle,assembly) ) 

#define IXCLRDataProcess2_EndEnumAssemblies(This,handle)	\
    ( (This)->lpVtbl -> EndEnumAssemblies(This,handle) ) 

#define IXCLRDataProcess2_StartEnumModules(This,handle)	\
    ( (This)->lpVtbl -> StartEnumModules(This,handle) ) 

#define IXCLRDataProcess2_EnumModule(This,handle,mod)	\
    ( (This)->lpVtbl -> EnumModule(This,handle,mod) ) 

#define IXCLRDataProcess2_EndEnumModules(This,handle)	\
    ( (This)->lpVtbl -> EndEnumModules(This,handle) ) 

#define IXCLRDataProcess2_GetModuleByAddress(This,address,mod)	\
    ( (This)->lpVtbl -> GetModuleByAddress(This,address,mod) ) 

#define IXCLRDataProcess2_StartEnumMethodInstancesByAddress(This,address,appDomain,handle)	\
    ( (This)->lpVtbl -> StartEnumMethodInstancesByAddress(This,address,appDomain,handle) ) 

#define IXCLRDataProcess2_EnumMethodInstanceByAddress(This,handle,method)	\
    ( (This)->lpVtbl -> EnumMethodInstanceByAddress(This,handle,method) ) 

#define IXCLRDataProcess2_EndEnumMethodInstancesByAddress(This,handle)	\
    ( (This)->lpVtbl -> EndEnumMethodInstancesByAddress(This,handle) ) 

#define IXCLRDataProcess2_GetDataByAddress(This,address,flags,appDomain,tlsTask,bufLen,nameLen,nameBuf,value,displacement)	\
    ( (This)->lpVtbl -> GetDataByAddress(This,address,flags,appDomain,tlsTask,bufLen,nameLen,nameBuf,value,displacement) ) 

#define IXCLRDataProcess2_GetExceptionStateByExceptionRecord(This,record,exState)	\
    ( (This)->lpVtbl -> GetExceptionStateByExceptionRecord(This,record,exState) ) 

#define IXCLRDataProcess2_TranslateExceptionRecordToNotification(This,record,notify)	\
    ( (This)->lpVtbl -> TranslateExceptionRecordToNotification(This,record,notify) ) 

#define IXCLRDataProcess2_Request(This,reqCode,inBufferSize,inBuffer,outBufferSize,outBuffer)	\
    ( (This)->lpVtbl -> Request(This,reqCode,inBufferSize,inBuffer,outBufferSize,outBuffer) ) 

#define IXCLRDataProcess2_CreateMemoryValue(This,appDomain,tlsTask,type,addr,value)	\
    ( (This)->lpVtbl -> CreateMemoryValue(This,appDomain,tlsTask,type,addr,value) ) 

#define IXCLRDataProcess2_SetAllTypeNotifications(This,mod,flags)	\
    ( (This)->lpVtbl -> SetAllTypeNotifications(This,mod,flags) ) 

#define IXCLRDataProcess2_SetAllCodeNotifications(This,mod,flags)	\
    ( (This)->lpVtbl -> SetAllCodeNotifications(This,mod,flags) ) 

#define IXCLRDataProcess2_GetTypeNotifications(This,numTokens,mods,singleMod,tokens,flags)	\
    ( (This)->lpVtbl -> GetTypeNotifications(This,numTokens,mods,singleMod,tokens,flags) ) 

#define IXCLRDataProcess2_SetTypeNotifications(This,numTokens,mods,singleMod,tokens,flags,singleFlags)	\
    ( (This)->lpVtbl -> SetTypeNotifications(This,numTokens,mods,singleMod,tokens,flags,singleFlags) ) 

#define IXCLRDataProcess2_GetCodeNotifications(This,numTokens,mods,singleMod,tokens,flags)	\
    ( (This)->lpVtbl -> GetCodeNotifications(This,numTokens,mods,singleMod,tokens,flags) ) 

#define IXCLRDataProcess2_SetCodeNotifications(This,numTokens,mods,singleMod,tokens,flags,singleFlags)	\
    ( (This)->lpVtbl -> SetCodeNotifications(This,numTokens,mods,singleMod,tokens,flags,singleFlags) ) 

#define IXCLRDataProcess2_GetOtherNotificationFlags(This,flags)	\
    ( (This)->lpVtbl -> GetOtherNotificationFlags(This,flags) ) 

#define IXCLRDataProcess2_SetOtherNotificationFlags(This,flags)	\
    ( (This)->lpVtbl -> SetOtherNotificationFlags(This,flags) ) 

#define IXCLRDataProcess2_StartEnumMethodDefinitionsByAddress(This,address,handle)	\
    ( (This)->lpVtbl -> StartEnumMethodDefinitionsByAddress(This,address,handle) ) 

#define IXCLRDataProcess2_EnumMethodDefinitionByAddress(This,handle,method)	\
    ( (This)->lpVtbl -> EnumMethodDefinitionByAddress(This,handle,method) ) 

#define IXCLRDataProcess2_EndEnumMethodDefinitionsByAddress(This,handle)	\
    ( (This)->lpVtbl -> EndEnumMethodDefinitionsByAddress(This,handle) ) 

#define IXCLRDataProcess2_FollowStub(This,inFlags,inAddr,inBuffer,outAddr,outBuffer,outFlags)	\
    ( (This)->lpVtbl -> FollowStub(This,inFlags,inAddr,inBuffer,outAddr,outBuffer,outFlags) ) 

#define IXCLRDataProcess2_FollowStub2(This,task,inFlags,inAddr,inBuffer,outAddr,outBuffer,outFlags)	\
    ( (This)->lpVtbl -> FollowStub2(This,task,inFlags,inAddr,inBuffer,outAddr,outBuffer,outFlags) ) 

#define IXCLRDataProcess2_DumpNativeImage(This,loadedBase,name,display,libSupport,dis)	\
    ( (This)->lpVtbl -> DumpNativeImage(This,loadedBase,name,display,libSupport,dis) ) 


#define IXCLRDataProcess2_GetGcNotification(This,gcEvtArgs)	\
    ( (This)->lpVtbl -> GetGcNotification(This,gcEvtArgs) ) 

#define IXCLRDataProcess2_SetGcNotification(This,gcEvtArgs)	\
    ( (This)->lpVtbl -> SetGcNotification(This,gcEvtArgs) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __IXCLRDataProcess2_INTERFACE_DEFINED__ */


/* interface __MIDL_itf_xclrdata_0000_0006 */
/* [local] */ 

typedef /* [public] */ 
enum __MIDL___MIDL_itf_xclrdata_0000_0006_0001
    {
        CLRDATA_DOMAIN_DEFAULT	= 0
    } 	CLRDataAppDomainFlag;

#pragma warning(push)
#pragma warning(disable:28718)	


extern RPC_IF_HANDLE __MIDL_itf_xclrdata_0000_0006_v0_0_c_ifspec;
extern RPC_IF_HANDLE __MIDL_itf_xclrdata_0000_0006_v0_0_s_ifspec;

#ifndef __IXCLRDataAppDomain_INTERFACE_DEFINED__
#define __IXCLRDataAppDomain_INTERFACE_DEFINED__

/* interface IXCLRDataAppDomain */
/* [uuid][local][object] */ 


EXTERN_C const IID IID_IXCLRDataAppDomain;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("7CA04601-C702-4670-A63C-FA44F7DA7BD5")
    IXCLRDataAppDomain : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE GetProcess( 
            /* [out] */ IXCLRDataProcess **process) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetName( 
            /* [in] */ ULONG32 bufLen,
            /* [out] */ ULONG32 *nameLen,
            /* [size_is][out] */ WCHAR name[  ]) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetUniqueID( 
            /* [out] */ ULONG64 *id) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetFlags( 
            /* [out] */ ULONG32 *flags) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE IsSameObject( 
            /* [in] */ IXCLRDataAppDomain *appDomain) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetManagedObject( 
            /* [out] */ IXCLRDataValue **value) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE Request( 
            /* [in] */ ULONG32 reqCode,
            /* [in] */ ULONG32 inBufferSize,
            /* [size_is][in] */ BYTE *inBuffer,
            /* [in] */ ULONG32 outBufferSize,
            /* [size_is][out] */ BYTE *outBuffer) = 0;
        
    };
    
    
#else 	/* C style interface */

    typedef struct IXCLRDataAppDomainVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            IXCLRDataAppDomain * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            IXCLRDataAppDomain * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            IXCLRDataAppDomain * This);
        
        HRESULT ( STDMETHODCALLTYPE *GetProcess )( 
            IXCLRDataAppDomain * This,
            /* [out] */ IXCLRDataProcess **process);
        
        HRESULT ( STDMETHODCALLTYPE *GetName )( 
            IXCLRDataAppDomain * This,
            /* [in] */ ULONG32 bufLen,
            /* [out] */ ULONG32 *nameLen,
            /* [size_is][out] */ WCHAR name[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *GetUniqueID )( 
            IXCLRDataAppDomain * This,
            /* [out] */ ULONG64 *id);
        
        HRESULT ( STDMETHODCALLTYPE *GetFlags )( 
            IXCLRDataAppDomain * This,
            /* [out] */ ULONG32 *flags);
        
        HRESULT ( STDMETHODCALLTYPE *IsSameObject )( 
            IXCLRDataAppDomain * This,
            /* [in] */ IXCLRDataAppDomain *appDomain);
        
        HRESULT ( STDMETHODCALLTYPE *GetManagedObject )( 
            IXCLRDataAppDomain * This,
            /* [out] */ IXCLRDataValue **value);
        
        HRESULT ( STDMETHODCALLTYPE *Request )( 
            IXCLRDataAppDomain * This,
            /* [in] */ ULONG32 reqCode,
            /* [in] */ ULONG32 inBufferSize,
            /* [size_is][in] */ BYTE *inBuffer,
            /* [in] */ ULONG32 outBufferSize,
            /* [size_is][out] */ BYTE *outBuffer);
        
        END_INTERFACE
    } IXCLRDataAppDomainVtbl;

    interface IXCLRDataAppDomain
    {
        CONST_VTBL struct IXCLRDataAppDomainVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define IXCLRDataAppDomain_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define IXCLRDataAppDomain_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define IXCLRDataAppDomain_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define IXCLRDataAppDomain_GetProcess(This,process)	\
    ( (This)->lpVtbl -> GetProcess(This,process) ) 

#define IXCLRDataAppDomain_GetName(This,bufLen,nameLen,name)	\
    ( (This)->lpVtbl -> GetName(This,bufLen,nameLen,name) ) 

#define IXCLRDataAppDomain_GetUniqueID(This,id)	\
    ( (This)->lpVtbl -> GetUniqueID(This,id) ) 

#define IXCLRDataAppDomain_GetFlags(This,flags)	\
    ( (This)->lpVtbl -> GetFlags(This,flags) ) 

#define IXCLRDataAppDomain_IsSameObject(This,appDomain)	\
    ( (This)->lpVtbl -> IsSameObject(This,appDomain) ) 

#define IXCLRDataAppDomain_GetManagedObject(This,value)	\
    ( (This)->lpVtbl -> GetManagedObject(This,value) ) 

#define IXCLRDataAppDomain_Request(This,reqCode,inBufferSize,inBuffer,outBufferSize,outBuffer)	\
    ( (This)->lpVtbl -> Request(This,reqCode,inBufferSize,inBuffer,outBufferSize,outBuffer) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __IXCLRDataAppDomain_INTERFACE_DEFINED__ */


/* interface __MIDL_itf_xclrdata_0000_0007 */
/* [local] */ 

#pragma warning(pop)
typedef /* [public] */ 
enum __MIDL___MIDL_itf_xclrdata_0000_0007_0001
    {
        CLRDATA_ASSEMBLY_DEFAULT	= 0
    } 	CLRDataAssemblyFlag;

#pragma warning(push)
#pragma warning(disable:28718)


extern RPC_IF_HANDLE __MIDL_itf_xclrdata_0000_0007_v0_0_c_ifspec;
extern RPC_IF_HANDLE __MIDL_itf_xclrdata_0000_0007_v0_0_s_ifspec;

#ifndef __IXCLRDataAssembly_INTERFACE_DEFINED__
#define __IXCLRDataAssembly_INTERFACE_DEFINED__

/* interface IXCLRDataAssembly */
/* [uuid][local][object] */ 


EXTERN_C const IID IID_IXCLRDataAssembly;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("2FA17588-43C2-46ab-9B51-C8F01E39C9AC")
    IXCLRDataAssembly : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE StartEnumModules( 
            /* [out] */ CLRDATA_ENUM *handle) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE EnumModule( 
            /* [out][in] */ CLRDATA_ENUM *handle,
            /* [out] */ IXCLRDataModule **mod) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE EndEnumModules( 
            /* [in] */ CLRDATA_ENUM handle) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetName( 
            /* [in] */ ULONG32 bufLen,
            /* [out] */ ULONG32 *nameLen,
            /* [size_is][out] */ WCHAR name[  ]) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetFileName( 
            /* [in] */ ULONG32 bufLen,
            /* [out] */ ULONG32 *nameLen,
            /* [size_is][out] */ WCHAR name[  ]) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetFlags( 
            /* [out] */ ULONG32 *flags) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE IsSameObject( 
            /* [in] */ IXCLRDataAssembly *assembly) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE Request( 
            /* [in] */ ULONG32 reqCode,
            /* [in] */ ULONG32 inBufferSize,
            /* [size_is][in] */ BYTE *inBuffer,
            /* [in] */ ULONG32 outBufferSize,
            /* [size_is][out] */ BYTE *outBuffer) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE StartEnumAppDomains( 
            /* [out] */ CLRDATA_ENUM *handle) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE EnumAppDomain( 
            /* [out][in] */ CLRDATA_ENUM *handle,
            /* [out] */ IXCLRDataAppDomain **appDomain) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE EndEnumAppDomains( 
            /* [in] */ CLRDATA_ENUM handle) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetDisplayName( 
            /* [in] */ ULONG32 bufLen,
            /* [out] */ ULONG32 *nameLen,
            /* [size_is][out] */ WCHAR name[  ]) = 0;
        
    };
    
    
#else 	/* C style interface */

    typedef struct IXCLRDataAssemblyVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            IXCLRDataAssembly * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            IXCLRDataAssembly * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            IXCLRDataAssembly * This);
        
        HRESULT ( STDMETHODCALLTYPE *StartEnumModules )( 
            IXCLRDataAssembly * This,
            /* [out] */ CLRDATA_ENUM *handle);
        
        HRESULT ( STDMETHODCALLTYPE *EnumModule )( 
            IXCLRDataAssembly * This,
            /* [out][in] */ CLRDATA_ENUM *handle,
            /* [out] */ IXCLRDataModule **mod);
        
        HRESULT ( STDMETHODCALLTYPE *EndEnumModules )( 
            IXCLRDataAssembly * This,
            /* [in] */ CLRDATA_ENUM handle);
        
        HRESULT ( STDMETHODCALLTYPE *GetName )( 
            IXCLRDataAssembly * This,
            /* [in] */ ULONG32 bufLen,
            /* [out] */ ULONG32 *nameLen,
            /* [size_is][out] */ WCHAR name[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *GetFileName )( 
            IXCLRDataAssembly * This,
            /* [in] */ ULONG32 bufLen,
            /* [out] */ ULONG32 *nameLen,
            /* [size_is][out] */ WCHAR name[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *GetFlags )( 
            IXCLRDataAssembly * This,
            /* [out] */ ULONG32 *flags);
        
        HRESULT ( STDMETHODCALLTYPE *IsSameObject )( 
            IXCLRDataAssembly * This,
            /* [in] */ IXCLRDataAssembly *assembly);
        
        HRESULT ( STDMETHODCALLTYPE *Request )( 
            IXCLRDataAssembly * This,
            /* [in] */ ULONG32 reqCode,
            /* [in] */ ULONG32 inBufferSize,
            /* [size_is][in] */ BYTE *inBuffer,
            /* [in] */ ULONG32 outBufferSize,
            /* [size_is][out] */ BYTE *outBuffer);
        
        HRESULT ( STDMETHODCALLTYPE *StartEnumAppDomains )( 
            IXCLRDataAssembly * This,
            /* [out] */ CLRDATA_ENUM *handle);
        
        HRESULT ( STDMETHODCALLTYPE *EnumAppDomain )( 
            IXCLRDataAssembly * This,
            /* [out][in] */ CLRDATA_ENUM *handle,
            /* [out] */ IXCLRDataAppDomain **appDomain);
        
        HRESULT ( STDMETHODCALLTYPE *EndEnumAppDomains )( 
            IXCLRDataAssembly * This,
            /* [in] */ CLRDATA_ENUM handle);
        
        HRESULT ( STDMETHODCALLTYPE *GetDisplayName )( 
            IXCLRDataAssembly * This,
            /* [in] */ ULONG32 bufLen,
            /* [out] */ ULONG32 *nameLen,
            /* [size_is][out] */ WCHAR name[  ]);
        
        END_INTERFACE
    } IXCLRDataAssemblyVtbl;

    interface IXCLRDataAssembly
    {
        CONST_VTBL struct IXCLRDataAssemblyVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define IXCLRDataAssembly_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define IXCLRDataAssembly_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define IXCLRDataAssembly_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define IXCLRDataAssembly_StartEnumModules(This,handle)	\
    ( (This)->lpVtbl -> StartEnumModules(This,handle) ) 

#define IXCLRDataAssembly_EnumModule(This,handle,mod)	\
    ( (This)->lpVtbl -> EnumModule(This,handle,mod) ) 

#define IXCLRDataAssembly_EndEnumModules(This,handle)	\
    ( (This)->lpVtbl -> EndEnumModules(This,handle) ) 

#define IXCLRDataAssembly_GetName(This,bufLen,nameLen,name)	\
    ( (This)->lpVtbl -> GetName(This,bufLen,nameLen,name) ) 

#define IXCLRDataAssembly_GetFileName(This,bufLen,nameLen,name)	\
    ( (This)->lpVtbl -> GetFileName(This,bufLen,nameLen,name) ) 

#define IXCLRDataAssembly_GetFlags(This,flags)	\
    ( (This)->lpVtbl -> GetFlags(This,flags) ) 

#define IXCLRDataAssembly_IsSameObject(This,assembly)	\
    ( (This)->lpVtbl -> IsSameObject(This,assembly) ) 

#define IXCLRDataAssembly_Request(This,reqCode,inBufferSize,inBuffer,outBufferSize,outBuffer)	\
    ( (This)->lpVtbl -> Request(This,reqCode,inBufferSize,inBuffer,outBufferSize,outBuffer) ) 

#define IXCLRDataAssembly_StartEnumAppDomains(This,handle)	\
    ( (This)->lpVtbl -> StartEnumAppDomains(This,handle) ) 

#define IXCLRDataAssembly_EnumAppDomain(This,handle,appDomain)	\
    ( (This)->lpVtbl -> EnumAppDomain(This,handle,appDomain) ) 

#define IXCLRDataAssembly_EndEnumAppDomains(This,handle)	\
    ( (This)->lpVtbl -> EndEnumAppDomains(This,handle) ) 

#define IXCLRDataAssembly_GetDisplayName(This,bufLen,nameLen,name)	\
    ( (This)->lpVtbl -> GetDisplayName(This,bufLen,nameLen,name) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __IXCLRDataAssembly_INTERFACE_DEFINED__ */


/* interface __MIDL_itf_xclrdata_0000_0008 */
/* [local] */ 

#pragma warning(pop)
typedef /* [public] */ 
enum __MIDL___MIDL_itf_xclrdata_0000_0008_0001
    {
        CLRDATA_MODULE_DEFAULT	= 0,
        CLRDATA_MODULE_IS_DYNAMIC	= 0x1,
        CLRDATA_MODULE_IS_MEMORY_STREAM	= 0x2
    } 	CLRDataModuleFlag;

typedef /* [public][public][public] */ 
enum __MIDL___MIDL_itf_xclrdata_0000_0008_0002
    {
        CLRDATA_MODULE_PE_FILE	= 0,
        CLRDATA_MODULE_PREJIT_FILE	= ( CLRDATA_MODULE_PE_FILE + 1 ) ,
        CLRDATA_MODULE_MEMORY_STREAM	= ( CLRDATA_MODULE_PREJIT_FILE + 1 ) ,
        CLRDATA_MODULE_OTHER	= ( CLRDATA_MODULE_MEMORY_STREAM + 1 ) 
    } 	CLRDataModuleExtentType;

typedef /* [public][public] */ struct __MIDL___MIDL_itf_xclrdata_0000_0008_0003
    {
    CLRDATA_ADDRESS base;
    ULONG32 length;
    CLRDataModuleExtentType type;
    } 	CLRDATA_MODULE_EXTENT;

typedef /* [public] */ 
enum __MIDL___MIDL_itf_xclrdata_0000_0008_0004
    {
        CLRDATA_TYPENOTIFY_NONE	= 0,
        CLRDATA_TYPENOTIFY_LOADED	= 0x1,
        CLRDATA_TYPENOTIFY_UNLOADED	= 0x2
    } 	CLRDataTypeNotification;

typedef /* [public] */ 
enum __MIDL___MIDL_itf_xclrdata_0000_0008_0005
    {
        CLRDATA_METHNOTIFY_NONE	= 0,
        CLRDATA_METHNOTIFY_GENERATED	= 0x1,
        CLRDATA_METHNOTIFY_DISCARDED	= 0x2
    } 	CLRDataMethodCodeNotification;

#pragma warning(push)
#pragma warning(disable:28718)


extern RPC_IF_HANDLE __MIDL_itf_xclrdata_0000_0008_v0_0_c_ifspec;
extern RPC_IF_HANDLE __MIDL_itf_xclrdata_0000_0008_v0_0_s_ifspec;

#ifndef __IXCLRDataModule_INTERFACE_DEFINED__
#define __IXCLRDataModule_INTERFACE_DEFINED__

/* interface IXCLRDataModule */
/* [uuid][local][object] */ 


EXTERN_C const IID IID_IXCLRDataModule;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("88E32849-0A0A-4cb0-9022-7CD2E9E139E2")
    IXCLRDataModule : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE StartEnumAssemblies( 
            /* [out] */ CLRDATA_ENUM *handle) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE EnumAssembly( 
            /* [out][in] */ CLRDATA_ENUM *handle,
            /* [out] */ IXCLRDataAssembly **assembly) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE EndEnumAssemblies( 
            /* [in] */ CLRDATA_ENUM handle) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE StartEnumTypeDefinitions( 
            /* [out] */ CLRDATA_ENUM *handle) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE EnumTypeDefinition( 
            /* [out][in] */ CLRDATA_ENUM *handle,
            /* [out] */ IXCLRDataTypeDefinition **typeDefinition) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE EndEnumTypeDefinitions( 
            /* [in] */ CLRDATA_ENUM handle) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE StartEnumTypeInstances( 
            /* [in] */ IXCLRDataAppDomain *appDomain,
            /* [out] */ CLRDATA_ENUM *handle) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE EnumTypeInstance( 
            /* [out][in] */ CLRDATA_ENUM *handle,
            /* [out] */ IXCLRDataTypeInstance **typeInstance) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE EndEnumTypeInstances( 
            /* [in] */ CLRDATA_ENUM handle) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE StartEnumTypeDefinitionsByName( 
            /* [in] */ LPCWSTR name,
            /* [in] */ ULONG32 flags,
            /* [out] */ CLRDATA_ENUM *handle) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE EnumTypeDefinitionByName( 
            /* [out][in] */ CLRDATA_ENUM *handle,
            /* [out] */ IXCLRDataTypeDefinition **type) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE EndEnumTypeDefinitionsByName( 
            /* [in] */ CLRDATA_ENUM handle) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE StartEnumTypeInstancesByName( 
            /* [in] */ LPCWSTR name,
            /* [in] */ ULONG32 flags,
            /* [in] */ IXCLRDataAppDomain *appDomain,
            /* [out] */ CLRDATA_ENUM *handle) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE EnumTypeInstanceByName( 
            /* [out][in] */ CLRDATA_ENUM *handle,
            /* [out] */ IXCLRDataTypeInstance **type) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE EndEnumTypeInstancesByName( 
            /* [in] */ CLRDATA_ENUM handle) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetTypeDefinitionByToken( 
            /* [in] */ mdTypeDef token,
            /* [out] */ IXCLRDataTypeDefinition **typeDefinition) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE StartEnumMethodDefinitionsByName( 
            /* [in] */ LPCWSTR name,
            /* [in] */ ULONG32 flags,
            /* [out] */ CLRDATA_ENUM *handle) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE EnumMethodDefinitionByName( 
            /* [out][in] */ CLRDATA_ENUM *handle,
            /* [out] */ IXCLRDataMethodDefinition **method) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE EndEnumMethodDefinitionsByName( 
            /* [in] */ CLRDATA_ENUM handle) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE StartEnumMethodInstancesByName( 
            /* [in] */ LPCWSTR name,
            /* [in] */ ULONG32 flags,
            /* [in] */ IXCLRDataAppDomain *appDomain,
            /* [out] */ CLRDATA_ENUM *handle) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE EnumMethodInstanceByName( 
            /* [out][in] */ CLRDATA_ENUM *handle,
            /* [out] */ IXCLRDataMethodInstance **method) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE EndEnumMethodInstancesByName( 
            /* [in] */ CLRDATA_ENUM handle) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetMethodDefinitionByToken( 
            /* [in] */ mdMethodDef token,
            /* [out] */ IXCLRDataMethodDefinition **methodDefinition) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE StartEnumDataByName( 
            /* [in] */ LPCWSTR name,
            /* [in] */ ULONG32 flags,
            /* [in] */ IXCLRDataAppDomain *appDomain,
            /* [in] */ IXCLRDataTask *tlsTask,
            /* [out] */ CLRDATA_ENUM *handle) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE EnumDataByName( 
            /* [out][in] */ CLRDATA_ENUM *handle,
            /* [out] */ IXCLRDataValue **value) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE EndEnumDataByName( 
            /* [in] */ CLRDATA_ENUM handle) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetName( 
            /* [in] */ ULONG32 bufLen,
            /* [out] */ ULONG32 *nameLen,
            /* [size_is][out] */ WCHAR name[  ]) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetFileName( 
            /* [in] */ ULONG32 bufLen,
            /* [out] */ ULONG32 *nameLen,
            /* [size_is][out] */ WCHAR name[  ]) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetFlags( 
            /* [out] */ ULONG32 *flags) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE IsSameObject( 
            /* [in] */ IXCLRDataModule *mod) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE StartEnumExtents( 
            /* [out] */ CLRDATA_ENUM *handle) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE EnumExtent( 
            /* [out][in] */ CLRDATA_ENUM *handle,
            /* [out] */ CLRDATA_MODULE_EXTENT *extent) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE EndEnumExtents( 
            /* [in] */ CLRDATA_ENUM handle) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE Request( 
            /* [in] */ ULONG32 reqCode,
            /* [in] */ ULONG32 inBufferSize,
            /* [size_is][in] */ BYTE *inBuffer,
            /* [in] */ ULONG32 outBufferSize,
            /* [size_is][out] */ BYTE *outBuffer) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE StartEnumAppDomains( 
            /* [out] */ CLRDATA_ENUM *handle) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE EnumAppDomain( 
            /* [out][in] */ CLRDATA_ENUM *handle,
            /* [out] */ IXCLRDataAppDomain **appDomain) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE EndEnumAppDomains( 
            /* [in] */ CLRDATA_ENUM handle) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetVersionId( 
            /* [out] */ GUID *vid) = 0;
        
    };
    
    
#else 	/* C style interface */

    typedef struct IXCLRDataModuleVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            IXCLRDataModule * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            IXCLRDataModule * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            IXCLRDataModule * This);
        
        HRESULT ( STDMETHODCALLTYPE *StartEnumAssemblies )( 
            IXCLRDataModule * This,
            /* [out] */ CLRDATA_ENUM *handle);
        
        HRESULT ( STDMETHODCALLTYPE *EnumAssembly )( 
            IXCLRDataModule * This,
            /* [out][in] */ CLRDATA_ENUM *handle,
            /* [out] */ IXCLRDataAssembly **assembly);
        
        HRESULT ( STDMETHODCALLTYPE *EndEnumAssemblies )( 
            IXCLRDataModule * This,
            /* [in] */ CLRDATA_ENUM handle);
        
        HRESULT ( STDMETHODCALLTYPE *StartEnumTypeDefinitions )( 
            IXCLRDataModule * This,
            /* [out] */ CLRDATA_ENUM *handle);
        
        HRESULT ( STDMETHODCALLTYPE *EnumTypeDefinition )( 
            IXCLRDataModule * This,
            /* [out][in] */ CLRDATA_ENUM *handle,
            /* [out] */ IXCLRDataTypeDefinition **typeDefinition);
        
        HRESULT ( STDMETHODCALLTYPE *EndEnumTypeDefinitions )( 
            IXCLRDataModule * This,
            /* [in] */ CLRDATA_ENUM handle);
        
        HRESULT ( STDMETHODCALLTYPE *StartEnumTypeInstances )( 
            IXCLRDataModule * This,
            /* [in] */ IXCLRDataAppDomain *appDomain,
            /* [out] */ CLRDATA_ENUM *handle);
        
        HRESULT ( STDMETHODCALLTYPE *EnumTypeInstance )( 
            IXCLRDataModule * This,
            /* [out][in] */ CLRDATA_ENUM *handle,
            /* [out] */ IXCLRDataTypeInstance **typeInstance);
        
        HRESULT ( STDMETHODCALLTYPE *EndEnumTypeInstances )( 
            IXCLRDataModule * This,
            /* [in] */ CLRDATA_ENUM handle);
        
        HRESULT ( STDMETHODCALLTYPE *StartEnumTypeDefinitionsByName )( 
            IXCLRDataModule * This,
            /* [in] */ LPCWSTR name,
            /* [in] */ ULONG32 flags,
            /* [out] */ CLRDATA_ENUM *handle);
        
        HRESULT ( STDMETHODCALLTYPE *EnumTypeDefinitionByName )( 
            IXCLRDataModule * This,
            /* [out][in] */ CLRDATA_ENUM *handle,
            /* [out] */ IXCLRDataTypeDefinition **type);
        
        HRESULT ( STDMETHODCALLTYPE *EndEnumTypeDefinitionsByName )( 
            IXCLRDataModule * This,
            /* [in] */ CLRDATA_ENUM handle);
        
        HRESULT ( STDMETHODCALLTYPE *StartEnumTypeInstancesByName )( 
            IXCLRDataModule * This,
            /* [in] */ LPCWSTR name,
            /* [in] */ ULONG32 flags,
            /* [in] */ IXCLRDataAppDomain *appDomain,
            /* [out] */ CLRDATA_ENUM *handle);
        
        HRESULT ( STDMETHODCALLTYPE *EnumTypeInstanceByName )( 
            IXCLRDataModule * This,
            /* [out][in] */ CLRDATA_ENUM *handle,
            /* [out] */ IXCLRDataTypeInstance **type);
        
        HRESULT ( STDMETHODCALLTYPE *EndEnumTypeInstancesByName )( 
            IXCLRDataModule * This,
            /* [in] */ CLRDATA_ENUM handle);
        
        HRESULT ( STDMETHODCALLTYPE *GetTypeDefinitionByToken )( 
            IXCLRDataModule * This,
            /* [in] */ mdTypeDef token,
            /* [out] */ IXCLRDataTypeDefinition **typeDefinition);
        
        HRESULT ( STDMETHODCALLTYPE *StartEnumMethodDefinitionsByName )( 
            IXCLRDataModule * This,
            /* [in] */ LPCWSTR name,
            /* [in] */ ULONG32 flags,
            /* [out] */ CLRDATA_ENUM *handle);
        
        HRESULT ( STDMETHODCALLTYPE *EnumMethodDefinitionByName )( 
            IXCLRDataModule * This,
            /* [out][in] */ CLRDATA_ENUM *handle,
            /* [out] */ IXCLRDataMethodDefinition **method);
        
        HRESULT ( STDMETHODCALLTYPE *EndEnumMethodDefinitionsByName )( 
            IXCLRDataModule * This,
            /* [in] */ CLRDATA_ENUM handle);
        
        HRESULT ( STDMETHODCALLTYPE *StartEnumMethodInstancesByName )( 
            IXCLRDataModule * This,
            /* [in] */ LPCWSTR name,
            /* [in] */ ULONG32 flags,
            /* [in] */ IXCLRDataAppDomain *appDomain,
            /* [out] */ CLRDATA_ENUM *handle);
        
        HRESULT ( STDMETHODCALLTYPE *EnumMethodInstanceByName )( 
            IXCLRDataModule * This,
            /* [out][in] */ CLRDATA_ENUM *handle,
            /* [out] */ IXCLRDataMethodInstance **method);
        
        HRESULT ( STDMETHODCALLTYPE *EndEnumMethodInstancesByName )( 
            IXCLRDataModule * This,
            /* [in] */ CLRDATA_ENUM handle);
        
        HRESULT ( STDMETHODCALLTYPE *GetMethodDefinitionByToken )( 
            IXCLRDataModule * This,
            /* [in] */ mdMethodDef token,
            /* [out] */ IXCLRDataMethodDefinition **methodDefinition);
        
        HRESULT ( STDMETHODCALLTYPE *StartEnumDataByName )( 
            IXCLRDataModule * This,
            /* [in] */ LPCWSTR name,
            /* [in] */ ULONG32 flags,
            /* [in] */ IXCLRDataAppDomain *appDomain,
            /* [in] */ IXCLRDataTask *tlsTask,
            /* [out] */ CLRDATA_ENUM *handle);
        
        HRESULT ( STDMETHODCALLTYPE *EnumDataByName )( 
            IXCLRDataModule * This,
            /* [out][in] */ CLRDATA_ENUM *handle,
            /* [out] */ IXCLRDataValue **value);
        
        HRESULT ( STDMETHODCALLTYPE *EndEnumDataByName )( 
            IXCLRDataModule * This,
            /* [in] */ CLRDATA_ENUM handle);
        
        HRESULT ( STDMETHODCALLTYPE *GetName )( 
            IXCLRDataModule * This,
            /* [in] */ ULONG32 bufLen,
            /* [out] */ ULONG32 *nameLen,
            /* [size_is][out] */ WCHAR name[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *GetFileName )( 
            IXCLRDataModule * This,
            /* [in] */ ULONG32 bufLen,
            /* [out] */ ULONG32 *nameLen,
            /* [size_is][out] */ WCHAR name[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *GetFlags )( 
            IXCLRDataModule * This,
            /* [out] */ ULONG32 *flags);
        
        HRESULT ( STDMETHODCALLTYPE *IsSameObject )( 
            IXCLRDataModule * This,
            /* [in] */ IXCLRDataModule *mod);
        
        HRESULT ( STDMETHODCALLTYPE *StartEnumExtents )( 
            IXCLRDataModule * This,
            /* [out] */ CLRDATA_ENUM *handle);
        
        HRESULT ( STDMETHODCALLTYPE *EnumExtent )( 
            IXCLRDataModule * This,
            /* [out][in] */ CLRDATA_ENUM *handle,
            /* [out] */ CLRDATA_MODULE_EXTENT *extent);
        
        HRESULT ( STDMETHODCALLTYPE *EndEnumExtents )( 
            IXCLRDataModule * This,
            /* [in] */ CLRDATA_ENUM handle);
        
        HRESULT ( STDMETHODCALLTYPE *Request )( 
            IXCLRDataModule * This,
            /* [in] */ ULONG32 reqCode,
            /* [in] */ ULONG32 inBufferSize,
            /* [size_is][in] */ BYTE *inBuffer,
            /* [in] */ ULONG32 outBufferSize,
            /* [size_is][out] */ BYTE *outBuffer);
        
        HRESULT ( STDMETHODCALLTYPE *StartEnumAppDomains )( 
            IXCLRDataModule * This,
            /* [out] */ CLRDATA_ENUM *handle);
        
        HRESULT ( STDMETHODCALLTYPE *EnumAppDomain )( 
            IXCLRDataModule * This,
            /* [out][in] */ CLRDATA_ENUM *handle,
            /* [out] */ IXCLRDataAppDomain **appDomain);
        
        HRESULT ( STDMETHODCALLTYPE *EndEnumAppDomains )( 
            IXCLRDataModule * This,
            /* [in] */ CLRDATA_ENUM handle);
        
        HRESULT ( STDMETHODCALLTYPE *GetVersionId )( 
            IXCLRDataModule * This,
            /* [out] */ GUID *vid);
        
        END_INTERFACE
    } IXCLRDataModuleVtbl;

    interface IXCLRDataModule
    {
        CONST_VTBL struct IXCLRDataModuleVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define IXCLRDataModule_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define IXCLRDataModule_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define IXCLRDataModule_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define IXCLRDataModule_StartEnumAssemblies(This,handle)	\
    ( (This)->lpVtbl -> StartEnumAssemblies(This,handle) ) 

#define IXCLRDataModule_EnumAssembly(This,handle,assembly)	\
    ( (This)->lpVtbl -> EnumAssembly(This,handle,assembly) ) 

#define IXCLRDataModule_EndEnumAssemblies(This,handle)	\
    ( (This)->lpVtbl -> EndEnumAssemblies(This,handle) ) 

#define IXCLRDataModule_StartEnumTypeDefinitions(This,handle)	\
    ( (This)->lpVtbl -> StartEnumTypeDefinitions(This,handle) ) 

#define IXCLRDataModule_EnumTypeDefinition(This,handle,typeDefinition)	\
    ( (This)->lpVtbl -> EnumTypeDefinition(This,handle,typeDefinition) ) 

#define IXCLRDataModule_EndEnumTypeDefinitions(This,handle)	\
    ( (This)->lpVtbl -> EndEnumTypeDefinitions(This,handle) ) 

#define IXCLRDataModule_StartEnumTypeInstances(This,appDomain,handle)	\
    ( (This)->lpVtbl -> StartEnumTypeInstances(This,appDomain,handle) ) 

#define IXCLRDataModule_EnumTypeInstance(This,handle,typeInstance)	\
    ( (This)->lpVtbl -> EnumTypeInstance(This,handle,typeInstance) ) 

#define IXCLRDataModule_EndEnumTypeInstances(This,handle)	\
    ( (This)->lpVtbl -> EndEnumTypeInstances(This,handle) ) 

#define IXCLRDataModule_StartEnumTypeDefinitionsByName(This,name,flags,handle)	\
    ( (This)->lpVtbl -> StartEnumTypeDefinitionsByName(This,name,flags,handle) ) 

#define IXCLRDataModule_EnumTypeDefinitionByName(This,handle,type)	\
    ( (This)->lpVtbl -> EnumTypeDefinitionByName(This,handle,type) ) 

#define IXCLRDataModule_EndEnumTypeDefinitionsByName(This,handle)	\
    ( (This)->lpVtbl -> EndEnumTypeDefinitionsByName(This,handle) ) 

#define IXCLRDataModule_StartEnumTypeInstancesByName(This,name,flags,appDomain,handle)	\
    ( (This)->lpVtbl -> StartEnumTypeInstancesByName(This,name,flags,appDomain,handle) ) 

#define IXCLRDataModule_EnumTypeInstanceByName(This,handle,type)	\
    ( (This)->lpVtbl -> EnumTypeInstanceByName(This,handle,type) ) 

#define IXCLRDataModule_EndEnumTypeInstancesByName(This,handle)	\
    ( (This)->lpVtbl -> EndEnumTypeInstancesByName(This,handle) ) 

#define IXCLRDataModule_GetTypeDefinitionByToken(This,token,typeDefinition)	\
    ( (This)->lpVtbl -> GetTypeDefinitionByToken(This,token,typeDefinition) ) 

#define IXCLRDataModule_StartEnumMethodDefinitionsByName(This,name,flags,handle)	\
    ( (This)->lpVtbl -> StartEnumMethodDefinitionsByName(This,name,flags,handle) ) 

#define IXCLRDataModule_EnumMethodDefinitionByName(This,handle,method)	\
    ( (This)->lpVtbl -> EnumMethodDefinitionByName(This,handle,method) ) 

#define IXCLRDataModule_EndEnumMethodDefinitionsByName(This,handle)	\
    ( (This)->lpVtbl -> EndEnumMethodDefinitionsByName(This,handle) ) 

#define IXCLRDataModule_StartEnumMethodInstancesByName(This,name,flags,appDomain,handle)	\
    ( (This)->lpVtbl -> StartEnumMethodInstancesByName(This,name,flags,appDomain,handle) ) 

#define IXCLRDataModule_EnumMethodInstanceByName(This,handle,method)	\
    ( (This)->lpVtbl -> EnumMethodInstanceByName(This,handle,method) ) 

#define IXCLRDataModule_EndEnumMethodInstancesByName(This,handle)	\
    ( (This)->lpVtbl -> EndEnumMethodInstancesByName(This,handle) ) 

#define IXCLRDataModule_GetMethodDefinitionByToken(This,token,methodDefinition)	\
    ( (This)->lpVtbl -> GetMethodDefinitionByToken(This,token,methodDefinition) ) 

#define IXCLRDataModule_StartEnumDataByName(This,name,flags,appDomain,tlsTask,handle)	\
    ( (This)->lpVtbl -> StartEnumDataByName(This,name,flags,appDomain,tlsTask,handle) ) 

#define IXCLRDataModule_EnumDataByName(This,handle,value)	\
    ( (This)->lpVtbl -> EnumDataByName(This,handle,value) ) 

#define IXCLRDataModule_EndEnumDataByName(This,handle)	\
    ( (This)->lpVtbl -> EndEnumDataByName(This,handle) ) 

#define IXCLRDataModule_GetName(This,bufLen,nameLen,name)	\
    ( (This)->lpVtbl -> GetName(This,bufLen,nameLen,name) ) 

#define IXCLRDataModule_GetFileName(This,bufLen,nameLen,name)	\
    ( (This)->lpVtbl -> GetFileName(This,bufLen,nameLen,name) ) 

#define IXCLRDataModule_GetFlags(This,flags)	\
    ( (This)->lpVtbl -> GetFlags(This,flags) ) 

#define IXCLRDataModule_IsSameObject(This,mod)	\
    ( (This)->lpVtbl -> IsSameObject(This,mod) ) 

#define IXCLRDataModule_StartEnumExtents(This,handle)	\
    ( (This)->lpVtbl -> StartEnumExtents(This,handle) ) 

#define IXCLRDataModule_EnumExtent(This,handle,extent)	\
    ( (This)->lpVtbl -> EnumExtent(This,handle,extent) ) 

#define IXCLRDataModule_EndEnumExtents(This,handle)	\
    ( (This)->lpVtbl -> EndEnumExtents(This,handle) ) 

#define IXCLRDataModule_Request(This,reqCode,inBufferSize,inBuffer,outBufferSize,outBuffer)	\
    ( (This)->lpVtbl -> Request(This,reqCode,inBufferSize,inBuffer,outBufferSize,outBuffer) ) 

#define IXCLRDataModule_StartEnumAppDomains(This,handle)	\
    ( (This)->lpVtbl -> StartEnumAppDomains(This,handle) ) 

#define IXCLRDataModule_EnumAppDomain(This,handle,appDomain)	\
    ( (This)->lpVtbl -> EnumAppDomain(This,handle,appDomain) ) 

#define IXCLRDataModule_EndEnumAppDomains(This,handle)	\
    ( (This)->lpVtbl -> EndEnumAppDomains(This,handle) ) 

#define IXCLRDataModule_GetVersionId(This,vid)	\
    ( (This)->lpVtbl -> GetVersionId(This,vid) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __IXCLRDataModule_INTERFACE_DEFINED__ */


/* interface __MIDL_itf_xclrdata_0000_0009 */
/* [local] */ 

#pragma warning(pop)


extern RPC_IF_HANDLE __MIDL_itf_xclrdata_0000_0009_v0_0_c_ifspec;
extern RPC_IF_HANDLE __MIDL_itf_xclrdata_0000_0009_v0_0_s_ifspec;

#ifndef __IXCLRDataModule2_INTERFACE_DEFINED__
#define __IXCLRDataModule2_INTERFACE_DEFINED__

/* interface IXCLRDataModule2 */
/* [uuid][local][object] */ 


EXTERN_C const IID IID_IXCLRDataModule2;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("34625881-7EB3-4524-817B-8DB9D064C760")
    IXCLRDataModule2 : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE SetJITCompilerFlags( 
            /* [in] */ DWORD dwFlags) = 0;
        
    };
    
    
#else 	/* C style interface */

    typedef struct IXCLRDataModule2Vtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            IXCLRDataModule2 * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            IXCLRDataModule2 * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            IXCLRDataModule2 * This);
        
        HRESULT ( STDMETHODCALLTYPE *SetJITCompilerFlags )( 
            IXCLRDataModule2 * This,
            /* [in] */ DWORD dwFlags);
        
        END_INTERFACE
    } IXCLRDataModule2Vtbl;

    interface IXCLRDataModule2
    {
        CONST_VTBL struct IXCLRDataModule2Vtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define IXCLRDataModule2_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define IXCLRDataModule2_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define IXCLRDataModule2_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define IXCLRDataModule2_SetJITCompilerFlags(This,dwFlags)	\
    ( (This)->lpVtbl -> SetJITCompilerFlags(This,dwFlags) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __IXCLRDataModule2_INTERFACE_DEFINED__ */


/* interface __MIDL_itf_xclrdata_0000_0010 */
/* [local] */ 

#pragma warning(push)
#pragma warning(disable:28718)	


extern RPC_IF_HANDLE __MIDL_itf_xclrdata_0000_0010_v0_0_c_ifspec;
extern RPC_IF_HANDLE __MIDL_itf_xclrdata_0000_0010_v0_0_s_ifspec;

#ifndef __IXCLRDataTypeDefinition_INTERFACE_DEFINED__
#define __IXCLRDataTypeDefinition_INTERFACE_DEFINED__

/* interface IXCLRDataTypeDefinition */
/* [uuid][local][object] */ 


EXTERN_C const IID IID_IXCLRDataTypeDefinition;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("4675666C-C275-45b8-9F6C-AB165D5C1E09")
    IXCLRDataTypeDefinition : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE GetModule( 
            /* [out] */ IXCLRDataModule **mod) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE StartEnumMethodDefinitions( 
            /* [out] */ CLRDATA_ENUM *handle) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE EnumMethodDefinition( 
            /* [out][in] */ CLRDATA_ENUM *handle,
            /* [out] */ IXCLRDataMethodDefinition **methodDefinition) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE EndEnumMethodDefinitions( 
            /* [in] */ CLRDATA_ENUM handle) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE StartEnumMethodDefinitionsByName( 
            /* [in] */ LPCWSTR name,
            /* [in] */ ULONG32 flags,
            /* [out] */ CLRDATA_ENUM *handle) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE EnumMethodDefinitionByName( 
            /* [out][in] */ CLRDATA_ENUM *handle,
            /* [out] */ IXCLRDataMethodDefinition **method) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE EndEnumMethodDefinitionsByName( 
            /* [in] */ CLRDATA_ENUM handle) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetMethodDefinitionByToken( 
            /* [in] */ mdMethodDef token,
            /* [out] */ IXCLRDataMethodDefinition **methodDefinition) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE StartEnumInstances( 
            /* [in] */ IXCLRDataAppDomain *appDomain,
            /* [out] */ CLRDATA_ENUM *handle) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE EnumInstance( 
            /* [out][in] */ CLRDATA_ENUM *handle,
            /* [out] */ IXCLRDataTypeInstance **instance) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE EndEnumInstances( 
            /* [in] */ CLRDATA_ENUM handle) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetName( 
            /* [in] */ ULONG32 flags,
            /* [in] */ ULONG32 bufLen,
            /* [out] */ ULONG32 *nameLen,
            /* [size_is][out] */ WCHAR nameBuf[  ]) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetTokenAndScope( 
            /* [out] */ mdTypeDef *token,
            /* [out] */ IXCLRDataModule **mod) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetCorElementType( 
            /* [out] */ CorElementType *type) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetFlags( 
            /* [out] */ ULONG32 *flags) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE IsSameObject( 
            /* [in] */ IXCLRDataTypeDefinition *type) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE Request( 
            /* [in] */ ULONG32 reqCode,
            /* [in] */ ULONG32 inBufferSize,
            /* [size_is][in] */ BYTE *inBuffer,
            /* [in] */ ULONG32 outBufferSize,
            /* [size_is][out] */ BYTE *outBuffer) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetArrayRank( 
            /* [out] */ ULONG32 *rank) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetBase( 
            /* [out] */ IXCLRDataTypeDefinition **base) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetNumFields( 
            /* [in] */ ULONG32 flags,
            /* [out] */ ULONG32 *numFields) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE StartEnumFields( 
            /* [in] */ ULONG32 flags,
            /* [out] */ CLRDATA_ENUM *handle) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE EnumField( 
            /* [out][in] */ CLRDATA_ENUM *handle,
            /* [in] */ ULONG32 nameBufLen,
            /* [out] */ ULONG32 *nameLen,
            /* [size_is][out] */ WCHAR nameBuf[  ],
            /* [out] */ IXCLRDataTypeDefinition **type,
            /* [out] */ ULONG32 *flags,
            /* [out] */ mdFieldDef *token) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE EndEnumFields( 
            /* [in] */ CLRDATA_ENUM handle) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE StartEnumFieldsByName( 
            /* [in] */ LPCWSTR name,
            /* [in] */ ULONG32 nameFlags,
            /* [in] */ ULONG32 fieldFlags,
            /* [out] */ CLRDATA_ENUM *handle) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE EnumFieldByName( 
            /* [out][in] */ CLRDATA_ENUM *handle,
            /* [out] */ IXCLRDataTypeDefinition **type,
            /* [out] */ ULONG32 *flags,
            /* [out] */ mdFieldDef *token) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE EndEnumFieldsByName( 
            /* [in] */ CLRDATA_ENUM handle) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetFieldByToken( 
            /* [in] */ mdFieldDef token,
            /* [in] */ ULONG32 nameBufLen,
            /* [out] */ ULONG32 *nameLen,
            /* [size_is][out] */ WCHAR nameBuf[  ],
            /* [out] */ IXCLRDataTypeDefinition **type,
            /* [out] */ ULONG32 *flags) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetTypeNotification( 
            /* [out] */ ULONG32 *flags) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE SetTypeNotification( 
            /* [in] */ ULONG32 flags) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE EnumField2( 
            /* [out][in] */ CLRDATA_ENUM *handle,
            /* [in] */ ULONG32 nameBufLen,
            /* [out] */ ULONG32 *nameLen,
            /* [size_is][out] */ WCHAR nameBuf[  ],
            /* [out] */ IXCLRDataTypeDefinition **type,
            /* [out] */ ULONG32 *flags,
            /* [out] */ IXCLRDataModule **tokenScope,
            /* [out] */ mdFieldDef *token) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE EnumFieldByName2( 
            /* [out][in] */ CLRDATA_ENUM *handle,
            /* [out] */ IXCLRDataTypeDefinition **type,
            /* [out] */ ULONG32 *flags,
            /* [out] */ IXCLRDataModule **tokenScope,
            /* [out] */ mdFieldDef *token) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetFieldByToken2( 
            /* [in] */ IXCLRDataModule *tokenScope,
            /* [in] */ mdFieldDef token,
            /* [in] */ ULONG32 nameBufLen,
            /* [out] */ ULONG32 *nameLen,
            /* [size_is][out] */ WCHAR nameBuf[  ],
            /* [out] */ IXCLRDataTypeDefinition **type,
            /* [out] */ ULONG32 *flags) = 0;
        
    };
    
    
#else 	/* C style interface */

    typedef struct IXCLRDataTypeDefinitionVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            IXCLRDataTypeDefinition * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            IXCLRDataTypeDefinition * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            IXCLRDataTypeDefinition * This);
        
        HRESULT ( STDMETHODCALLTYPE *GetModule )( 
            IXCLRDataTypeDefinition * This,
            /* [out] */ IXCLRDataModule **mod);
        
        HRESULT ( STDMETHODCALLTYPE *StartEnumMethodDefinitions )( 
            IXCLRDataTypeDefinition * This,
            /* [out] */ CLRDATA_ENUM *handle);
        
        HRESULT ( STDMETHODCALLTYPE *EnumMethodDefinition )( 
            IXCLRDataTypeDefinition * This,
            /* [out][in] */ CLRDATA_ENUM *handle,
            /* [out] */ IXCLRDataMethodDefinition **methodDefinition);
        
        HRESULT ( STDMETHODCALLTYPE *EndEnumMethodDefinitions )( 
            IXCLRDataTypeDefinition * This,
            /* [in] */ CLRDATA_ENUM handle);
        
        HRESULT ( STDMETHODCALLTYPE *StartEnumMethodDefinitionsByName )( 
            IXCLRDataTypeDefinition * This,
            /* [in] */ LPCWSTR name,
            /* [in] */ ULONG32 flags,
            /* [out] */ CLRDATA_ENUM *handle);
        
        HRESULT ( STDMETHODCALLTYPE *EnumMethodDefinitionByName )( 
            IXCLRDataTypeDefinition * This,
            /* [out][in] */ CLRDATA_ENUM *handle,
            /* [out] */ IXCLRDataMethodDefinition **method);
        
        HRESULT ( STDMETHODCALLTYPE *EndEnumMethodDefinitionsByName )( 
            IXCLRDataTypeDefinition * This,
            /* [in] */ CLRDATA_ENUM handle);
        
        HRESULT ( STDMETHODCALLTYPE *GetMethodDefinitionByToken )( 
            IXCLRDataTypeDefinition * This,
            /* [in] */ mdMethodDef token,
            /* [out] */ IXCLRDataMethodDefinition **methodDefinition);
        
        HRESULT ( STDMETHODCALLTYPE *StartEnumInstances )( 
            IXCLRDataTypeDefinition * This,
            /* [in] */ IXCLRDataAppDomain *appDomain,
            /* [out] */ CLRDATA_ENUM *handle);
        
        HRESULT ( STDMETHODCALLTYPE *EnumInstance )( 
            IXCLRDataTypeDefinition * This,
            /* [out][in] */ CLRDATA_ENUM *handle,
            /* [out] */ IXCLRDataTypeInstance **instance);
        
        HRESULT ( STDMETHODCALLTYPE *EndEnumInstances )( 
            IXCLRDataTypeDefinition * This,
            /* [in] */ CLRDATA_ENUM handle);
        
        HRESULT ( STDMETHODCALLTYPE *GetName )( 
            IXCLRDataTypeDefinition * This,
            /* [in] */ ULONG32 flags,
            /* [in] */ ULONG32 bufLen,
            /* [out] */ ULONG32 *nameLen,
            /* [size_is][out] */ WCHAR nameBuf[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *GetTokenAndScope )( 
            IXCLRDataTypeDefinition * This,
            /* [out] */ mdTypeDef *token,
            /* [out] */ IXCLRDataModule **mod);
        
        HRESULT ( STDMETHODCALLTYPE *GetCorElementType )( 
            IXCLRDataTypeDefinition * This,
            /* [out] */ CorElementType *type);
        
        HRESULT ( STDMETHODCALLTYPE *GetFlags )( 
            IXCLRDataTypeDefinition * This,
            /* [out] */ ULONG32 *flags);
        
        HRESULT ( STDMETHODCALLTYPE *IsSameObject )( 
            IXCLRDataTypeDefinition * This,
            /* [in] */ IXCLRDataTypeDefinition *type);
        
        HRESULT ( STDMETHODCALLTYPE *Request )( 
            IXCLRDataTypeDefinition * This,
            /* [in] */ ULONG32 reqCode,
            /* [in] */ ULONG32 inBufferSize,
            /* [size_is][in] */ BYTE *inBuffer,
            /* [in] */ ULONG32 outBufferSize,
            /* [size_is][out] */ BYTE *outBuffer);
        
        HRESULT ( STDMETHODCALLTYPE *GetArrayRank )( 
            IXCLRDataTypeDefinition * This,
            /* [out] */ ULONG32 *rank);
        
        HRESULT ( STDMETHODCALLTYPE *GetBase )( 
            IXCLRDataTypeDefinition * This,
            /* [out] */ IXCLRDataTypeDefinition **base);
        
        HRESULT ( STDMETHODCALLTYPE *GetNumFields )( 
            IXCLRDataTypeDefinition * This,
            /* [in] */ ULONG32 flags,
            /* [out] */ ULONG32 *numFields);
        
        HRESULT ( STDMETHODCALLTYPE *StartEnumFields )( 
            IXCLRDataTypeDefinition * This,
            /* [in] */ ULONG32 flags,
            /* [out] */ CLRDATA_ENUM *handle);
        
        HRESULT ( STDMETHODCALLTYPE *EnumField )( 
            IXCLRDataTypeDefinition * This,
            /* [out][in] */ CLRDATA_ENUM *handle,
            /* [in] */ ULONG32 nameBufLen,
            /* [out] */ ULONG32 *nameLen,
            /* [size_is][out] */ WCHAR nameBuf[  ],
            /* [out] */ IXCLRDataTypeDefinition **type,
            /* [out] */ ULONG32 *flags,
            /* [out] */ mdFieldDef *token);
        
        HRESULT ( STDMETHODCALLTYPE *EndEnumFields )( 
            IXCLRDataTypeDefinition * This,
            /* [in] */ CLRDATA_ENUM handle);
        
        HRESULT ( STDMETHODCALLTYPE *StartEnumFieldsByName )( 
            IXCLRDataTypeDefinition * This,
            /* [in] */ LPCWSTR name,
            /* [in] */ ULONG32 nameFlags,
            /* [in] */ ULONG32 fieldFlags,
            /* [out] */ CLRDATA_ENUM *handle);
        
        HRESULT ( STDMETHODCALLTYPE *EnumFieldByName )( 
            IXCLRDataTypeDefinition * This,
            /* [out][in] */ CLRDATA_ENUM *handle,
            /* [out] */ IXCLRDataTypeDefinition **type,
            /* [out] */ ULONG32 *flags,
            /* [out] */ mdFieldDef *token);
        
        HRESULT ( STDMETHODCALLTYPE *EndEnumFieldsByName )( 
            IXCLRDataTypeDefinition * This,
            /* [in] */ CLRDATA_ENUM handle);
        
        HRESULT ( STDMETHODCALLTYPE *GetFieldByToken )( 
            IXCLRDataTypeDefinition * This,
            /* [in] */ mdFieldDef token,
            /* [in] */ ULONG32 nameBufLen,
            /* [out] */ ULONG32 *nameLen,
            /* [size_is][out] */ WCHAR nameBuf[  ],
            /* [out] */ IXCLRDataTypeDefinition **type,
            /* [out] */ ULONG32 *flags);
        
        HRESULT ( STDMETHODCALLTYPE *GetTypeNotification )( 
            IXCLRDataTypeDefinition * This,
            /* [out] */ ULONG32 *flags);
        
        HRESULT ( STDMETHODCALLTYPE *SetTypeNotification )( 
            IXCLRDataTypeDefinition * This,
            /* [in] */ ULONG32 flags);
        
        HRESULT ( STDMETHODCALLTYPE *EnumField2 )( 
            IXCLRDataTypeDefinition * This,
            /* [out][in] */ CLRDATA_ENUM *handle,
            /* [in] */ ULONG32 nameBufLen,
            /* [out] */ ULONG32 *nameLen,
            /* [size_is][out] */ WCHAR nameBuf[  ],
            /* [out] */ IXCLRDataTypeDefinition **type,
            /* [out] */ ULONG32 *flags,
            /* [out] */ IXCLRDataModule **tokenScope,
            /* [out] */ mdFieldDef *token);
        
        HRESULT ( STDMETHODCALLTYPE *EnumFieldByName2 )( 
            IXCLRDataTypeDefinition * This,
            /* [out][in] */ CLRDATA_ENUM *handle,
            /* [out] */ IXCLRDataTypeDefinition **type,
            /* [out] */ ULONG32 *flags,
            /* [out] */ IXCLRDataModule **tokenScope,
            /* [out] */ mdFieldDef *token);
        
        HRESULT ( STDMETHODCALLTYPE *GetFieldByToken2 )( 
            IXCLRDataTypeDefinition * This,
            /* [in] */ IXCLRDataModule *tokenScope,
            /* [in] */ mdFieldDef token,
            /* [in] */ ULONG32 nameBufLen,
            /* [out] */ ULONG32 *nameLen,
            /* [size_is][out] */ WCHAR nameBuf[  ],
            /* [out] */ IXCLRDataTypeDefinition **type,
            /* [out] */ ULONG32 *flags);
        
        END_INTERFACE
    } IXCLRDataTypeDefinitionVtbl;

    interface IXCLRDataTypeDefinition
    {
        CONST_VTBL struct IXCLRDataTypeDefinitionVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define IXCLRDataTypeDefinition_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define IXCLRDataTypeDefinition_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define IXCLRDataTypeDefinition_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define IXCLRDataTypeDefinition_GetModule(This,mod)	\
    ( (This)->lpVtbl -> GetModule(This,mod) ) 

#define IXCLRDataTypeDefinition_StartEnumMethodDefinitions(This,handle)	\
    ( (This)->lpVtbl -> StartEnumMethodDefinitions(This,handle) ) 

#define IXCLRDataTypeDefinition_EnumMethodDefinition(This,handle,methodDefinition)	\
    ( (This)->lpVtbl -> EnumMethodDefinition(This,handle,methodDefinition) ) 

#define IXCLRDataTypeDefinition_EndEnumMethodDefinitions(This,handle)	\
    ( (This)->lpVtbl -> EndEnumMethodDefinitions(This,handle) ) 

#define IXCLRDataTypeDefinition_StartEnumMethodDefinitionsByName(This,name,flags,handle)	\
    ( (This)->lpVtbl -> StartEnumMethodDefinitionsByName(This,name,flags,handle) ) 

#define IXCLRDataTypeDefinition_EnumMethodDefinitionByName(This,handle,method)	\
    ( (This)->lpVtbl -> EnumMethodDefinitionByName(This,handle,method) ) 

#define IXCLRDataTypeDefinition_EndEnumMethodDefinitionsByName(This,handle)	\
    ( (This)->lpVtbl -> EndEnumMethodDefinitionsByName(This,handle) ) 

#define IXCLRDataTypeDefinition_GetMethodDefinitionByToken(This,token,methodDefinition)	\
    ( (This)->lpVtbl -> GetMethodDefinitionByToken(This,token,methodDefinition) ) 

#define IXCLRDataTypeDefinition_StartEnumInstances(This,appDomain,handle)	\
    ( (This)->lpVtbl -> StartEnumInstances(This,appDomain,handle) ) 

#define IXCLRDataTypeDefinition_EnumInstance(This,handle,instance)	\
    ( (This)->lpVtbl -> EnumInstance(This,handle,instance) ) 

#define IXCLRDataTypeDefinition_EndEnumInstances(This,handle)	\
    ( (This)->lpVtbl -> EndEnumInstances(This,handle) ) 

#define IXCLRDataTypeDefinition_GetName(This,flags,bufLen,nameLen,nameBuf)	\
    ( (This)->lpVtbl -> GetName(This,flags,bufLen,nameLen,nameBuf) ) 

#define IXCLRDataTypeDefinition_GetTokenAndScope(This,token,mod)	\
    ( (This)->lpVtbl -> GetTokenAndScope(This,token,mod) ) 

#define IXCLRDataTypeDefinition_GetCorElementType(This,type)	\
    ( (This)->lpVtbl -> GetCorElementType(This,type) ) 

#define IXCLRDataTypeDefinition_GetFlags(This,flags)	\
    ( (This)->lpVtbl -> GetFlags(This,flags) ) 

#define IXCLRDataTypeDefinition_IsSameObject(This,type)	\
    ( (This)->lpVtbl -> IsSameObject(This,type) ) 

#define IXCLRDataTypeDefinition_Request(This,reqCode,inBufferSize,inBuffer,outBufferSize,outBuffer)	\
    ( (This)->lpVtbl -> Request(This,reqCode,inBufferSize,inBuffer,outBufferSize,outBuffer) ) 

#define IXCLRDataTypeDefinition_GetArrayRank(This,rank)	\
    ( (This)->lpVtbl -> GetArrayRank(This,rank) ) 

#define IXCLRDataTypeDefinition_GetBase(This,base)	\
    ( (This)->lpVtbl -> GetBase(This,base) ) 

#define IXCLRDataTypeDefinition_GetNumFields(This,flags,numFields)	\
    ( (This)->lpVtbl -> GetNumFields(This,flags,numFields) ) 

#define IXCLRDataTypeDefinition_StartEnumFields(This,flags,handle)	\
    ( (This)->lpVtbl -> StartEnumFields(This,flags,handle) ) 

#define IXCLRDataTypeDefinition_EnumField(This,handle,nameBufLen,nameLen,nameBuf,type,flags,token)	\
    ( (This)->lpVtbl -> EnumField(This,handle,nameBufLen,nameLen,nameBuf,type,flags,token) ) 

#define IXCLRDataTypeDefinition_EndEnumFields(This,handle)	\
    ( (This)->lpVtbl -> EndEnumFields(This,handle) ) 

#define IXCLRDataTypeDefinition_StartEnumFieldsByName(This,name,nameFlags,fieldFlags,handle)	\
    ( (This)->lpVtbl -> StartEnumFieldsByName(This,name,nameFlags,fieldFlags,handle) ) 

#define IXCLRDataTypeDefinition_EnumFieldByName(This,handle,type,flags,token)	\
    ( (This)->lpVtbl -> EnumFieldByName(This,handle,type,flags,token) ) 

#define IXCLRDataTypeDefinition_EndEnumFieldsByName(This,handle)	\
    ( (This)->lpVtbl -> EndEnumFieldsByName(This,handle) ) 

#define IXCLRDataTypeDefinition_GetFieldByToken(This,token,nameBufLen,nameLen,nameBuf,type,flags)	\
    ( (This)->lpVtbl -> GetFieldByToken(This,token,nameBufLen,nameLen,nameBuf,type,flags) ) 

#define IXCLRDataTypeDefinition_GetTypeNotification(This,flags)	\
    ( (This)->lpVtbl -> GetTypeNotification(This,flags) ) 

#define IXCLRDataTypeDefinition_SetTypeNotification(This,flags)	\
    ( (This)->lpVtbl -> SetTypeNotification(This,flags) ) 

#define IXCLRDataTypeDefinition_EnumField2(This,handle,nameBufLen,nameLen,nameBuf,type,flags,tokenScope,token)	\
    ( (This)->lpVtbl -> EnumField2(This,handle,nameBufLen,nameLen,nameBuf,type,flags,tokenScope,token) ) 

#define IXCLRDataTypeDefinition_EnumFieldByName2(This,handle,type,flags,tokenScope,token)	\
    ( (This)->lpVtbl -> EnumFieldByName2(This,handle,type,flags,tokenScope,token) ) 

#define IXCLRDataTypeDefinition_GetFieldByToken2(This,tokenScope,token,nameBufLen,nameLen,nameBuf,type,flags)	\
    ( (This)->lpVtbl -> GetFieldByToken2(This,tokenScope,token,nameBufLen,nameLen,nameBuf,type,flags) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __IXCLRDataTypeDefinition_INTERFACE_DEFINED__ */


/* interface __MIDL_itf_xclrdata_0000_0011 */
/* [local] */ 

#pragma warning(pop)
#pragma warning(push)
#pragma warning(disable:28718)


extern RPC_IF_HANDLE __MIDL_itf_xclrdata_0000_0011_v0_0_c_ifspec;
extern RPC_IF_HANDLE __MIDL_itf_xclrdata_0000_0011_v0_0_s_ifspec;

#ifndef __IXCLRDataTypeInstance_INTERFACE_DEFINED__
#define __IXCLRDataTypeInstance_INTERFACE_DEFINED__

/* interface IXCLRDataTypeInstance */
/* [uuid][local][object] */ 


EXTERN_C const IID IID_IXCLRDataTypeInstance;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("4D078D91-9CB3-4b0d-97AC-28C8A5A82597")
    IXCLRDataTypeInstance : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE StartEnumMethodInstances( 
            /* [out] */ CLRDATA_ENUM *handle) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE EnumMethodInstance( 
            /* [out][in] */ CLRDATA_ENUM *handle,
            /* [out] */ IXCLRDataMethodInstance **methodInstance) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE EndEnumMethodInstances( 
            /* [in] */ CLRDATA_ENUM handle) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE StartEnumMethodInstancesByName( 
            /* [in] */ LPCWSTR name,
            /* [in] */ ULONG32 flags,
            /* [out] */ CLRDATA_ENUM *handle) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE EnumMethodInstanceByName( 
            /* [out][in] */ CLRDATA_ENUM *handle,
            /* [out] */ IXCLRDataMethodInstance **method) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE EndEnumMethodInstancesByName( 
            /* [in] */ CLRDATA_ENUM handle) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetNumStaticFields( 
            /* [out] */ ULONG32 *numFields) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetStaticFieldByIndex( 
            /* [in] */ ULONG32 index,
            /* [in] */ IXCLRDataTask *tlsTask,
            /* [out] */ IXCLRDataValue **field,
            /* [in] */ ULONG32 bufLen,
            /* [out] */ ULONG32 *nameLen,
            /* [size_is][out] */ WCHAR nameBuf[  ],
            /* [out] */ mdFieldDef *token) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE StartEnumStaticFieldsByName( 
            /* [in] */ LPCWSTR name,
            /* [in] */ ULONG32 flags,
            /* [in] */ IXCLRDataTask *tlsTask,
            /* [out] */ CLRDATA_ENUM *handle) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE EnumStaticFieldByName( 
            /* [out][in] */ CLRDATA_ENUM *handle,
            /* [out] */ IXCLRDataValue **value) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE EndEnumStaticFieldsByName( 
            /* [in] */ CLRDATA_ENUM handle) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetNumTypeArguments( 
            /* [out] */ ULONG32 *numTypeArgs) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetTypeArgumentByIndex( 
            /* [in] */ ULONG32 index,
            /* [out] */ IXCLRDataTypeInstance **typeArg) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetName( 
            /* [in] */ ULONG32 flags,
            /* [in] */ ULONG32 bufLen,
            /* [out] */ ULONG32 *nameLen,
            /* [size_is][out] */ WCHAR nameBuf[  ]) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetModule( 
            /* [out] */ IXCLRDataModule **mod) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetDefinition( 
            /* [out] */ IXCLRDataTypeDefinition **typeDefinition) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetFlags( 
            /* [out] */ ULONG32 *flags) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE IsSameObject( 
            /* [in] */ IXCLRDataTypeInstance *type) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE Request( 
            /* [in] */ ULONG32 reqCode,
            /* [in] */ ULONG32 inBufferSize,
            /* [size_is][in] */ BYTE *inBuffer,
            /* [in] */ ULONG32 outBufferSize,
            /* [size_is][out] */ BYTE *outBuffer) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetNumStaticFields2( 
            /* [in] */ ULONG32 flags,
            /* [out] */ ULONG32 *numFields) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE StartEnumStaticFields( 
            /* [in] */ ULONG32 flags,
            /* [in] */ IXCLRDataTask *tlsTask,
            /* [out] */ CLRDATA_ENUM *handle) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE EnumStaticField( 
            /* [out][in] */ CLRDATA_ENUM *handle,
            /* [out] */ IXCLRDataValue **value) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE EndEnumStaticFields( 
            /* [in] */ CLRDATA_ENUM handle) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE StartEnumStaticFieldsByName2( 
            /* [in] */ LPCWSTR name,
            /* [in] */ ULONG32 nameFlags,
            /* [in] */ ULONG32 fieldFlags,
            /* [in] */ IXCLRDataTask *tlsTask,
            /* [out] */ CLRDATA_ENUM *handle) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE EnumStaticFieldByName2( 
            /* [out][in] */ CLRDATA_ENUM *handle,
            /* [out] */ IXCLRDataValue **value) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE EndEnumStaticFieldsByName2( 
            /* [in] */ CLRDATA_ENUM handle) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetStaticFieldByToken( 
            /* [in] */ mdFieldDef token,
            /* [in] */ IXCLRDataTask *tlsTask,
            /* [out] */ IXCLRDataValue **field,
            /* [in] */ ULONG32 bufLen,
            /* [out] */ ULONG32 *nameLen,
            /* [size_is][out] */ WCHAR nameBuf[  ]) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetBase( 
            /* [out] */ IXCLRDataTypeInstance **base) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE EnumStaticField2( 
            /* [out][in] */ CLRDATA_ENUM *handle,
            /* [out] */ IXCLRDataValue **value,
            /* [in] */ ULONG32 bufLen,
            /* [out] */ ULONG32 *nameLen,
            /* [size_is][out] */ WCHAR nameBuf[  ],
            /* [out] */ IXCLRDataModule **tokenScope,
            /* [out] */ mdFieldDef *token) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE EnumStaticFieldByName3( 
            /* [out][in] */ CLRDATA_ENUM *handle,
            /* [out] */ IXCLRDataValue **value,
            /* [out] */ IXCLRDataModule **tokenScope,
            /* [out] */ mdFieldDef *token) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetStaticFieldByToken2( 
            /* [in] */ IXCLRDataModule *tokenScope,
            /* [in] */ mdFieldDef token,
            /* [in] */ IXCLRDataTask *tlsTask,
            /* [out] */ IXCLRDataValue **field,
            /* [in] */ ULONG32 bufLen,
            /* [out] */ ULONG32 *nameLen,
            /* [size_is][out] */ WCHAR nameBuf[  ]) = 0;
        
    };
    
    
#else 	/* C style interface */

    typedef struct IXCLRDataTypeInstanceVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            IXCLRDataTypeInstance * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            IXCLRDataTypeInstance * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            IXCLRDataTypeInstance * This);
        
        HRESULT ( STDMETHODCALLTYPE *StartEnumMethodInstances )( 
            IXCLRDataTypeInstance * This,
            /* [out] */ CLRDATA_ENUM *handle);
        
        HRESULT ( STDMETHODCALLTYPE *EnumMethodInstance )( 
            IXCLRDataTypeInstance * This,
            /* [out][in] */ CLRDATA_ENUM *handle,
            /* [out] */ IXCLRDataMethodInstance **methodInstance);
        
        HRESULT ( STDMETHODCALLTYPE *EndEnumMethodInstances )( 
            IXCLRDataTypeInstance * This,
            /* [in] */ CLRDATA_ENUM handle);
        
        HRESULT ( STDMETHODCALLTYPE *StartEnumMethodInstancesByName )( 
            IXCLRDataTypeInstance * This,
            /* [in] */ LPCWSTR name,
            /* [in] */ ULONG32 flags,
            /* [out] */ CLRDATA_ENUM *handle);
        
        HRESULT ( STDMETHODCALLTYPE *EnumMethodInstanceByName )( 
            IXCLRDataTypeInstance * This,
            /* [out][in] */ CLRDATA_ENUM *handle,
            /* [out] */ IXCLRDataMethodInstance **method);
        
        HRESULT ( STDMETHODCALLTYPE *EndEnumMethodInstancesByName )( 
            IXCLRDataTypeInstance * This,
            /* [in] */ CLRDATA_ENUM handle);
        
        HRESULT ( STDMETHODCALLTYPE *GetNumStaticFields )( 
            IXCLRDataTypeInstance * This,
            /* [out] */ ULONG32 *numFields);
        
        HRESULT ( STDMETHODCALLTYPE *GetStaticFieldByIndex )( 
            IXCLRDataTypeInstance * This,
            /* [in] */ ULONG32 index,
            /* [in] */ IXCLRDataTask *tlsTask,
            /* [out] */ IXCLRDataValue **field,
            /* [in] */ ULONG32 bufLen,
            /* [out] */ ULONG32 *nameLen,
            /* [size_is][out] */ WCHAR nameBuf[  ],
            /* [out] */ mdFieldDef *token);
        
        HRESULT ( STDMETHODCALLTYPE *StartEnumStaticFieldsByName )( 
            IXCLRDataTypeInstance * This,
            /* [in] */ LPCWSTR name,
            /* [in] */ ULONG32 flags,
            /* [in] */ IXCLRDataTask *tlsTask,
            /* [out] */ CLRDATA_ENUM *handle);
        
        HRESULT ( STDMETHODCALLTYPE *EnumStaticFieldByName )( 
            IXCLRDataTypeInstance * This,
            /* [out][in] */ CLRDATA_ENUM *handle,
            /* [out] */ IXCLRDataValue **value);
        
        HRESULT ( STDMETHODCALLTYPE *EndEnumStaticFieldsByName )( 
            IXCLRDataTypeInstance * This,
            /* [in] */ CLRDATA_ENUM handle);
        
        HRESULT ( STDMETHODCALLTYPE *GetNumTypeArguments )( 
            IXCLRDataTypeInstance * This,
            /* [out] */ ULONG32 *numTypeArgs);
        
        HRESULT ( STDMETHODCALLTYPE *GetTypeArgumentByIndex )( 
            IXCLRDataTypeInstance * This,
            /* [in] */ ULONG32 index,
            /* [out] */ IXCLRDataTypeInstance **typeArg);
        
        HRESULT ( STDMETHODCALLTYPE *GetName )( 
            IXCLRDataTypeInstance * This,
            /* [in] */ ULONG32 flags,
            /* [in] */ ULONG32 bufLen,
            /* [out] */ ULONG32 *nameLen,
            /* [size_is][out] */ WCHAR nameBuf[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *GetModule )( 
            IXCLRDataTypeInstance * This,
            /* [out] */ IXCLRDataModule **mod);
        
        HRESULT ( STDMETHODCALLTYPE *GetDefinition )( 
            IXCLRDataTypeInstance * This,
            /* [out] */ IXCLRDataTypeDefinition **typeDefinition);
        
        HRESULT ( STDMETHODCALLTYPE *GetFlags )( 
            IXCLRDataTypeInstance * This,
            /* [out] */ ULONG32 *flags);
        
        HRESULT ( STDMETHODCALLTYPE *IsSameObject )( 
            IXCLRDataTypeInstance * This,
            /* [in] */ IXCLRDataTypeInstance *type);
        
        HRESULT ( STDMETHODCALLTYPE *Request )( 
            IXCLRDataTypeInstance * This,
            /* [in] */ ULONG32 reqCode,
            /* [in] */ ULONG32 inBufferSize,
            /* [size_is][in] */ BYTE *inBuffer,
            /* [in] */ ULONG32 outBufferSize,
            /* [size_is][out] */ BYTE *outBuffer);
        
        HRESULT ( STDMETHODCALLTYPE *GetNumStaticFields2 )( 
            IXCLRDataTypeInstance * This,
            /* [in] */ ULONG32 flags,
            /* [out] */ ULONG32 *numFields);
        
        HRESULT ( STDMETHODCALLTYPE *StartEnumStaticFields )( 
            IXCLRDataTypeInstance * This,
            /* [in] */ ULONG32 flags,
            /* [in] */ IXCLRDataTask *tlsTask,
            /* [out] */ CLRDATA_ENUM *handle);
        
        HRESULT ( STDMETHODCALLTYPE *EnumStaticField )( 
            IXCLRDataTypeInstance * This,
            /* [out][in] */ CLRDATA_ENUM *handle,
            /* [out] */ IXCLRDataValue **value);
        
        HRESULT ( STDMETHODCALLTYPE *EndEnumStaticFields )( 
            IXCLRDataTypeInstance * This,
            /* [in] */ CLRDATA_ENUM handle);
        
        HRESULT ( STDMETHODCALLTYPE *StartEnumStaticFieldsByName2 )( 
            IXCLRDataTypeInstance * This,
            /* [in] */ LPCWSTR name,
            /* [in] */ ULONG32 nameFlags,
            /* [in] */ ULONG32 fieldFlags,
            /* [in] */ IXCLRDataTask *tlsTask,
            /* [out] */ CLRDATA_ENUM *handle);
        
        HRESULT ( STDMETHODCALLTYPE *EnumStaticFieldByName2 )( 
            IXCLRDataTypeInstance * This,
            /* [out][in] */ CLRDATA_ENUM *handle,
            /* [out] */ IXCLRDataValue **value);
        
        HRESULT ( STDMETHODCALLTYPE *EndEnumStaticFieldsByName2 )( 
            IXCLRDataTypeInstance * This,
            /* [in] */ CLRDATA_ENUM handle);
        
        HRESULT ( STDMETHODCALLTYPE *GetStaticFieldByToken )( 
            IXCLRDataTypeInstance * This,
            /* [in] */ mdFieldDef token,
            /* [in] */ IXCLRDataTask *tlsTask,
            /* [out] */ IXCLRDataValue **field,
            /* [in] */ ULONG32 bufLen,
            /* [out] */ ULONG32 *nameLen,
            /* [size_is][out] */ WCHAR nameBuf[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *GetBase )( 
            IXCLRDataTypeInstance * This,
            /* [out] */ IXCLRDataTypeInstance **base);
        
        HRESULT ( STDMETHODCALLTYPE *EnumStaticField2 )( 
            IXCLRDataTypeInstance * This,
            /* [out][in] */ CLRDATA_ENUM *handle,
            /* [out] */ IXCLRDataValue **value,
            /* [in] */ ULONG32 bufLen,
            /* [out] */ ULONG32 *nameLen,
            /* [size_is][out] */ WCHAR nameBuf[  ],
            /* [out] */ IXCLRDataModule **tokenScope,
            /* [out] */ mdFieldDef *token);
        
        HRESULT ( STDMETHODCALLTYPE *EnumStaticFieldByName3 )( 
            IXCLRDataTypeInstance * This,
            /* [out][in] */ CLRDATA_ENUM *handle,
            /* [out] */ IXCLRDataValue **value,
            /* [out] */ IXCLRDataModule **tokenScope,
            /* [out] */ mdFieldDef *token);
        
        HRESULT ( STDMETHODCALLTYPE *GetStaticFieldByToken2 )( 
            IXCLRDataTypeInstance * This,
            /* [in] */ IXCLRDataModule *tokenScope,
            /* [in] */ mdFieldDef token,
            /* [in] */ IXCLRDataTask *tlsTask,
            /* [out] */ IXCLRDataValue **field,
            /* [in] */ ULONG32 bufLen,
            /* [out] */ ULONG32 *nameLen,
            /* [size_is][out] */ WCHAR nameBuf[  ]);
        
        END_INTERFACE
    } IXCLRDataTypeInstanceVtbl;

    interface IXCLRDataTypeInstance
    {
        CONST_VTBL struct IXCLRDataTypeInstanceVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define IXCLRDataTypeInstance_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define IXCLRDataTypeInstance_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define IXCLRDataTypeInstance_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define IXCLRDataTypeInstance_StartEnumMethodInstances(This,handle)	\
    ( (This)->lpVtbl -> StartEnumMethodInstances(This,handle) ) 

#define IXCLRDataTypeInstance_EnumMethodInstance(This,handle,methodInstance)	\
    ( (This)->lpVtbl -> EnumMethodInstance(This,handle,methodInstance) ) 

#define IXCLRDataTypeInstance_EndEnumMethodInstances(This,handle)	\
    ( (This)->lpVtbl -> EndEnumMethodInstances(This,handle) ) 

#define IXCLRDataTypeInstance_StartEnumMethodInstancesByName(This,name,flags,handle)	\
    ( (This)->lpVtbl -> StartEnumMethodInstancesByName(This,name,flags,handle) ) 

#define IXCLRDataTypeInstance_EnumMethodInstanceByName(This,handle,method)	\
    ( (This)->lpVtbl -> EnumMethodInstanceByName(This,handle,method) ) 

#define IXCLRDataTypeInstance_EndEnumMethodInstancesByName(This,handle)	\
    ( (This)->lpVtbl -> EndEnumMethodInstancesByName(This,handle) ) 

#define IXCLRDataTypeInstance_GetNumStaticFields(This,numFields)	\
    ( (This)->lpVtbl -> GetNumStaticFields(This,numFields) ) 

#define IXCLRDataTypeInstance_GetStaticFieldByIndex(This,index,tlsTask,field,bufLen,nameLen,nameBuf,token)	\
    ( (This)->lpVtbl -> GetStaticFieldByIndex(This,index,tlsTask,field,bufLen,nameLen,nameBuf,token) ) 

#define IXCLRDataTypeInstance_StartEnumStaticFieldsByName(This,name,flags,tlsTask,handle)	\
    ( (This)->lpVtbl -> StartEnumStaticFieldsByName(This,name,flags,tlsTask,handle) ) 

#define IXCLRDataTypeInstance_EnumStaticFieldByName(This,handle,value)	\
    ( (This)->lpVtbl -> EnumStaticFieldByName(This,handle,value) ) 

#define IXCLRDataTypeInstance_EndEnumStaticFieldsByName(This,handle)	\
    ( (This)->lpVtbl -> EndEnumStaticFieldsByName(This,handle) ) 

#define IXCLRDataTypeInstance_GetNumTypeArguments(This,numTypeArgs)	\
    ( (This)->lpVtbl -> GetNumTypeArguments(This,numTypeArgs) ) 

#define IXCLRDataTypeInstance_GetTypeArgumentByIndex(This,index,typeArg)	\
    ( (This)->lpVtbl -> GetTypeArgumentByIndex(This,index,typeArg) ) 

#define IXCLRDataTypeInstance_GetName(This,flags,bufLen,nameLen,nameBuf)	\
    ( (This)->lpVtbl -> GetName(This,flags,bufLen,nameLen,nameBuf) ) 

#define IXCLRDataTypeInstance_GetModule(This,mod)	\
    ( (This)->lpVtbl -> GetModule(This,mod) ) 

#define IXCLRDataTypeInstance_GetDefinition(This,typeDefinition)	\
    ( (This)->lpVtbl -> GetDefinition(This,typeDefinition) ) 

#define IXCLRDataTypeInstance_GetFlags(This,flags)	\
    ( (This)->lpVtbl -> GetFlags(This,flags) ) 

#define IXCLRDataTypeInstance_IsSameObject(This,type)	\
    ( (This)->lpVtbl -> IsSameObject(This,type) ) 

#define IXCLRDataTypeInstance_Request(This,reqCode,inBufferSize,inBuffer,outBufferSize,outBuffer)	\
    ( (This)->lpVtbl -> Request(This,reqCode,inBufferSize,inBuffer,outBufferSize,outBuffer) ) 

#define IXCLRDataTypeInstance_GetNumStaticFields2(This,flags,numFields)	\
    ( (This)->lpVtbl -> GetNumStaticFields2(This,flags,numFields) ) 

#define IXCLRDataTypeInstance_StartEnumStaticFields(This,flags,tlsTask,handle)	\
    ( (This)->lpVtbl -> StartEnumStaticFields(This,flags,tlsTask,handle) ) 

#define IXCLRDataTypeInstance_EnumStaticField(This,handle,value)	\
    ( (This)->lpVtbl -> EnumStaticField(This,handle,value) ) 

#define IXCLRDataTypeInstance_EndEnumStaticFields(This,handle)	\
    ( (This)->lpVtbl -> EndEnumStaticFields(This,handle) ) 

#define IXCLRDataTypeInstance_StartEnumStaticFieldsByName2(This,name,nameFlags,fieldFlags,tlsTask,handle)	\
    ( (This)->lpVtbl -> StartEnumStaticFieldsByName2(This,name,nameFlags,fieldFlags,tlsTask,handle) ) 

#define IXCLRDataTypeInstance_EnumStaticFieldByName2(This,handle,value)	\
    ( (This)->lpVtbl -> EnumStaticFieldByName2(This,handle,value) ) 

#define IXCLRDataTypeInstance_EndEnumStaticFieldsByName2(This,handle)	\
    ( (This)->lpVtbl -> EndEnumStaticFieldsByName2(This,handle) ) 

#define IXCLRDataTypeInstance_GetStaticFieldByToken(This,token,tlsTask,field,bufLen,nameLen,nameBuf)	\
    ( (This)->lpVtbl -> GetStaticFieldByToken(This,token,tlsTask,field,bufLen,nameLen,nameBuf) ) 

#define IXCLRDataTypeInstance_GetBase(This,base)	\
    ( (This)->lpVtbl -> GetBase(This,base) ) 

#define IXCLRDataTypeInstance_EnumStaticField2(This,handle,value,bufLen,nameLen,nameBuf,tokenScope,token)	\
    ( (This)->lpVtbl -> EnumStaticField2(This,handle,value,bufLen,nameLen,nameBuf,tokenScope,token) ) 

#define IXCLRDataTypeInstance_EnumStaticFieldByName3(This,handle,value,tokenScope,token)	\
    ( (This)->lpVtbl -> EnumStaticFieldByName3(This,handle,value,tokenScope,token) ) 

#define IXCLRDataTypeInstance_GetStaticFieldByToken2(This,tokenScope,token,tlsTask,field,bufLen,nameLen,nameBuf)	\
    ( (This)->lpVtbl -> GetStaticFieldByToken2(This,tokenScope,token,tlsTask,field,bufLen,nameLen,nameBuf) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __IXCLRDataTypeInstance_INTERFACE_DEFINED__ */


/* interface __MIDL_itf_xclrdata_0000_0012 */
/* [local] */ 

#pragma warning(pop)
typedef /* [public][public][public] */ 
enum __MIDL___MIDL_itf_xclrdata_0000_0012_0001
    {
        CLRDATA_SOURCE_TYPE_INVALID	= 0
    } 	CLRDataSourceType;

typedef /* [public] */ 
enum __MIDL___MIDL_itf_xclrdata_0000_0012_0002
    {
        CLRDATA_IL_OFFSET_NO_MAPPING	= -1,
        CLRDATA_IL_OFFSET_PROLOG	= -2,
        CLRDATA_IL_OFFSET_EPILOG	= -3
    } 	CLRDATA_IL_OFFSET_MARKER;

typedef /* [public][public] */ struct __MIDL___MIDL_itf_xclrdata_0000_0012_0003
    {
    ULONG32 ilOffset;
    CLRDATA_ADDRESS startAddress;
    CLRDATA_ADDRESS endAddress;
    CLRDataSourceType type;
    } 	CLRDATA_IL_ADDRESS_MAP;

typedef /* [public] */ 
enum __MIDL___MIDL_itf_xclrdata_0000_0012_0004
    {
        CLRDATA_METHOD_DEFAULT	= 0,
        CLRDATA_METHOD_HAS_THIS	= 0x1
    } 	CLRDataMethodFlag;

typedef /* [public][public][public] */ 
enum __MIDL___MIDL_itf_xclrdata_0000_0012_0005
    {
        CLRDATA_METHDEF_IL	= 0
    } 	CLRDataMethodDefinitionExtentType;

typedef /* [public][public] */ struct __MIDL___MIDL_itf_xclrdata_0000_0012_0006
    {
    CLRDATA_ADDRESS startAddress;
    CLRDATA_ADDRESS endAddress;
    ULONG32 enCVersion;
    CLRDataMethodDefinitionExtentType type;
    } 	CLRDATA_METHDEF_EXTENT;

#pragma warning(push)
#pragma warning(disable:28718)	


extern RPC_IF_HANDLE __MIDL_itf_xclrdata_0000_0012_v0_0_c_ifspec;
extern RPC_IF_HANDLE __MIDL_itf_xclrdata_0000_0012_v0_0_s_ifspec;

#ifndef __IXCLRDataMethodDefinition_INTERFACE_DEFINED__
#define __IXCLRDataMethodDefinition_INTERFACE_DEFINED__

/* interface IXCLRDataMethodDefinition */
/* [uuid][local][object] */ 


EXTERN_C const IID IID_IXCLRDataMethodDefinition;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("AAF60008-FB2C-420b-8FB1-42D244A54A97")
    IXCLRDataMethodDefinition : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE GetTypeDefinition( 
            /* [out] */ IXCLRDataTypeDefinition **typeDefinition) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE StartEnumInstances( 
            /* [in] */ IXCLRDataAppDomain *appDomain,
            /* [out] */ CLRDATA_ENUM *handle) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE EnumInstance( 
            /* [out][in] */ CLRDATA_ENUM *handle,
            /* [out] */ IXCLRDataMethodInstance **instance) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE EndEnumInstances( 
            /* [in] */ CLRDATA_ENUM handle) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetName( 
            /* [in] */ ULONG32 flags,
            /* [in] */ ULONG32 bufLen,
            /* [out] */ ULONG32 *nameLen,
            /* [size_is][out] */ WCHAR name[  ]) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetTokenAndScope( 
            /* [out] */ mdMethodDef *token,
            /* [out] */ IXCLRDataModule **mod) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetFlags( 
            /* [out] */ ULONG32 *flags) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE IsSameObject( 
            /* [in] */ IXCLRDataMethodDefinition *method) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetLatestEnCVersion( 
            /* [out] */ ULONG32 *version) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE StartEnumExtents( 
            /* [out] */ CLRDATA_ENUM *handle) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE EnumExtent( 
            /* [out][in] */ CLRDATA_ENUM *handle,
            /* [out] */ CLRDATA_METHDEF_EXTENT *extent) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE EndEnumExtents( 
            /* [in] */ CLRDATA_ENUM handle) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetCodeNotification( 
            /* [out] */ ULONG32 *flags) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE SetCodeNotification( 
            /* [in] */ ULONG32 flags) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE Request( 
            /* [in] */ ULONG32 reqCode,
            /* [in] */ ULONG32 inBufferSize,
            /* [size_is][in] */ BYTE *inBuffer,
            /* [in] */ ULONG32 outBufferSize,
            /* [size_is][out] */ BYTE *outBuffer) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetRepresentativeEntryAddress( 
            /* [out] */ CLRDATA_ADDRESS *addr) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE HasClassOrMethodInstantiation( 
            /* [out] */ BOOL *bGeneric) = 0;
        
    };
    
    
#else 	/* C style interface */

    typedef struct IXCLRDataMethodDefinitionVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            IXCLRDataMethodDefinition * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            IXCLRDataMethodDefinition * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            IXCLRDataMethodDefinition * This);
        
        HRESULT ( STDMETHODCALLTYPE *GetTypeDefinition )( 
            IXCLRDataMethodDefinition * This,
            /* [out] */ IXCLRDataTypeDefinition **typeDefinition);
        
        HRESULT ( STDMETHODCALLTYPE *StartEnumInstances )( 
            IXCLRDataMethodDefinition * This,
            /* [in] */ IXCLRDataAppDomain *appDomain,
            /* [out] */ CLRDATA_ENUM *handle);
        
        HRESULT ( STDMETHODCALLTYPE *EnumInstance )( 
            IXCLRDataMethodDefinition * This,
            /* [out][in] */ CLRDATA_ENUM *handle,
            /* [out] */ IXCLRDataMethodInstance **instance);
        
        HRESULT ( STDMETHODCALLTYPE *EndEnumInstances )( 
            IXCLRDataMethodDefinition * This,
            /* [in] */ CLRDATA_ENUM handle);
        
        HRESULT ( STDMETHODCALLTYPE *GetName )( 
            IXCLRDataMethodDefinition * This,
            /* [in] */ ULONG32 flags,
            /* [in] */ ULONG32 bufLen,
            /* [out] */ ULONG32 *nameLen,
            /* [size_is][out] */ WCHAR name[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *GetTokenAndScope )( 
            IXCLRDataMethodDefinition * This,
            /* [out] */ mdMethodDef *token,
            /* [out] */ IXCLRDataModule **mod);
        
        HRESULT ( STDMETHODCALLTYPE *GetFlags )( 
            IXCLRDataMethodDefinition * This,
            /* [out] */ ULONG32 *flags);
        
        HRESULT ( STDMETHODCALLTYPE *IsSameObject )( 
            IXCLRDataMethodDefinition * This,
            /* [in] */ IXCLRDataMethodDefinition *method);
        
        HRESULT ( STDMETHODCALLTYPE *GetLatestEnCVersion )( 
            IXCLRDataMethodDefinition * This,
            /* [out] */ ULONG32 *version);
        
        HRESULT ( STDMETHODCALLTYPE *StartEnumExtents )( 
            IXCLRDataMethodDefinition * This,
            /* [out] */ CLRDATA_ENUM *handle);
        
        HRESULT ( STDMETHODCALLTYPE *EnumExtent )( 
            IXCLRDataMethodDefinition * This,
            /* [out][in] */ CLRDATA_ENUM *handle,
            /* [out] */ CLRDATA_METHDEF_EXTENT *extent);
        
        HRESULT ( STDMETHODCALLTYPE *EndEnumExtents )( 
            IXCLRDataMethodDefinition * This,
            /* [in] */ CLRDATA_ENUM handle);
        
        HRESULT ( STDMETHODCALLTYPE *GetCodeNotification )( 
            IXCLRDataMethodDefinition * This,
            /* [out] */ ULONG32 *flags);
        
        HRESULT ( STDMETHODCALLTYPE *SetCodeNotification )( 
            IXCLRDataMethodDefinition * This,
            /* [in] */ ULONG32 flags);
        
        HRESULT ( STDMETHODCALLTYPE *Request )( 
            IXCLRDataMethodDefinition * This,
            /* [in] */ ULONG32 reqCode,
            /* [in] */ ULONG32 inBufferSize,
            /* [size_is][in] */ BYTE *inBuffer,
            /* [in] */ ULONG32 outBufferSize,
            /* [size_is][out] */ BYTE *outBuffer);
        
        HRESULT ( STDMETHODCALLTYPE *GetRepresentativeEntryAddress )( 
            IXCLRDataMethodDefinition * This,
            /* [out] */ CLRDATA_ADDRESS *addr);
        
        HRESULT ( STDMETHODCALLTYPE *HasClassOrMethodInstantiation )( 
            IXCLRDataMethodDefinition * This,
            /* [out] */ BOOL *bGeneric);
        
        END_INTERFACE
    } IXCLRDataMethodDefinitionVtbl;

    interface IXCLRDataMethodDefinition
    {
        CONST_VTBL struct IXCLRDataMethodDefinitionVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define IXCLRDataMethodDefinition_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define IXCLRDataMethodDefinition_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define IXCLRDataMethodDefinition_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define IXCLRDataMethodDefinition_GetTypeDefinition(This,typeDefinition)	\
    ( (This)->lpVtbl -> GetTypeDefinition(This,typeDefinition) ) 

#define IXCLRDataMethodDefinition_StartEnumInstances(This,appDomain,handle)	\
    ( (This)->lpVtbl -> StartEnumInstances(This,appDomain,handle) ) 

#define IXCLRDataMethodDefinition_EnumInstance(This,handle,instance)	\
    ( (This)->lpVtbl -> EnumInstance(This,handle,instance) ) 

#define IXCLRDataMethodDefinition_EndEnumInstances(This,handle)	\
    ( (This)->lpVtbl -> EndEnumInstances(This,handle) ) 

#define IXCLRDataMethodDefinition_GetName(This,flags,bufLen,nameLen,name)	\
    ( (This)->lpVtbl -> GetName(This,flags,bufLen,nameLen,name) ) 

#define IXCLRDataMethodDefinition_GetTokenAndScope(This,token,mod)	\
    ( (This)->lpVtbl -> GetTokenAndScope(This,token,mod) ) 

#define IXCLRDataMethodDefinition_GetFlags(This,flags)	\
    ( (This)->lpVtbl -> GetFlags(This,flags) ) 

#define IXCLRDataMethodDefinition_IsSameObject(This,method)	\
    ( (This)->lpVtbl -> IsSameObject(This,method) ) 

#define IXCLRDataMethodDefinition_GetLatestEnCVersion(This,version)	\
    ( (This)->lpVtbl -> GetLatestEnCVersion(This,version) ) 

#define IXCLRDataMethodDefinition_StartEnumExtents(This,handle)	\
    ( (This)->lpVtbl -> StartEnumExtents(This,handle) ) 

#define IXCLRDataMethodDefinition_EnumExtent(This,handle,extent)	\
    ( (This)->lpVtbl -> EnumExtent(This,handle,extent) ) 

#define IXCLRDataMethodDefinition_EndEnumExtents(This,handle)	\
    ( (This)->lpVtbl -> EndEnumExtents(This,handle) ) 

#define IXCLRDataMethodDefinition_GetCodeNotification(This,flags)	\
    ( (This)->lpVtbl -> GetCodeNotification(This,flags) ) 

#define IXCLRDataMethodDefinition_SetCodeNotification(This,flags)	\
    ( (This)->lpVtbl -> SetCodeNotification(This,flags) ) 

#define IXCLRDataMethodDefinition_Request(This,reqCode,inBufferSize,inBuffer,outBufferSize,outBuffer)	\
    ( (This)->lpVtbl -> Request(This,reqCode,inBufferSize,inBuffer,outBufferSize,outBuffer) ) 

#define IXCLRDataMethodDefinition_GetRepresentativeEntryAddress(This,addr)	\
    ( (This)->lpVtbl -> GetRepresentativeEntryAddress(This,addr) ) 

#define IXCLRDataMethodDefinition_HasClassOrMethodInstantiation(This,bGeneric)	\
    ( (This)->lpVtbl -> HasClassOrMethodInstantiation(This,bGeneric) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __IXCLRDataMethodDefinition_INTERFACE_DEFINED__ */


/* interface __MIDL_itf_xclrdata_0000_0013 */
/* [local] */ 

#pragma warning(pop)
#pragma warning(push)
#pragma warning(disable:28718)	


extern RPC_IF_HANDLE __MIDL_itf_xclrdata_0000_0013_v0_0_c_ifspec;
extern RPC_IF_HANDLE __MIDL_itf_xclrdata_0000_0013_v0_0_s_ifspec;

#ifndef __IXCLRDataMethodInstance_INTERFACE_DEFINED__
#define __IXCLRDataMethodInstance_INTERFACE_DEFINED__

/* interface IXCLRDataMethodInstance */
/* [uuid][local][object] */ 


EXTERN_C const IID IID_IXCLRDataMethodInstance;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("ECD73800-22CA-4b0d-AB55-E9BA7E6318A5")
    IXCLRDataMethodInstance : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE GetTypeInstance( 
            /* [out] */ IXCLRDataTypeInstance **typeInstance) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetDefinition( 
            /* [out] */ IXCLRDataMethodDefinition **methodDefinition) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetTokenAndScope( 
            /* [out] */ mdMethodDef *token,
            /* [out] */ IXCLRDataModule **mod) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetName( 
            /* [in] */ ULONG32 flags,
            /* [in] */ ULONG32 bufLen,
            /* [out] */ ULONG32 *nameLen,
            /* [size_is][out] */ WCHAR nameBuf[  ]) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetFlags( 
            /* [out] */ ULONG32 *flags) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE IsSameObject( 
            /* [in] */ IXCLRDataMethodInstance *method) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetEnCVersion( 
            /* [out] */ ULONG32 *version) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetNumTypeArguments( 
            /* [out] */ ULONG32 *numTypeArgs) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetTypeArgumentByIndex( 
            /* [in] */ ULONG32 index,
            /* [out] */ IXCLRDataTypeInstance **typeArg) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetILOffsetsByAddress( 
            /* [in] */ CLRDATA_ADDRESS address,
            /* [in] */ ULONG32 offsetsLen,
            /* [out] */ ULONG32 *offsetsNeeded,
            /* [size_is][out] */ ULONG32 ilOffsets[  ]) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetAddressRangesByILOffset( 
            /* [in] */ ULONG32 ilOffset,
            /* [in] */ ULONG32 rangesLen,
            /* [out] */ ULONG32 *rangesNeeded,
            /* [size_is][out] */ CLRDATA_ADDRESS_RANGE addressRanges[  ]) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetILAddressMap( 
            /* [in] */ ULONG32 mapLen,
            /* [out] */ ULONG32 *mapNeeded,
            /* [size_is][out] */ CLRDATA_IL_ADDRESS_MAP maps[  ]) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE StartEnumExtents( 
            /* [out] */ CLRDATA_ENUM *handle) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE EnumExtent( 
            /* [out][in] */ CLRDATA_ENUM *handle,
            /* [out] */ CLRDATA_ADDRESS_RANGE *extent) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE EndEnumExtents( 
            /* [in] */ CLRDATA_ENUM handle) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE Request( 
            /* [in] */ ULONG32 reqCode,
            /* [in] */ ULONG32 inBufferSize,
            /* [size_is][in] */ BYTE *inBuffer,
            /* [in] */ ULONG32 outBufferSize,
            /* [size_is][out] */ BYTE *outBuffer) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetRepresentativeEntryAddress( 
            /* [out] */ CLRDATA_ADDRESS *addr) = 0;
        
    };
    
    
#else 	/* C style interface */

    typedef struct IXCLRDataMethodInstanceVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            IXCLRDataMethodInstance * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            IXCLRDataMethodInstance * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            IXCLRDataMethodInstance * This);
        
        HRESULT ( STDMETHODCALLTYPE *GetTypeInstance )( 
            IXCLRDataMethodInstance * This,
            /* [out] */ IXCLRDataTypeInstance **typeInstance);
        
        HRESULT ( STDMETHODCALLTYPE *GetDefinition )( 
            IXCLRDataMethodInstance * This,
            /* [out] */ IXCLRDataMethodDefinition **methodDefinition);
        
        HRESULT ( STDMETHODCALLTYPE *GetTokenAndScope )( 
            IXCLRDataMethodInstance * This,
            /* [out] */ mdMethodDef *token,
            /* [out] */ IXCLRDataModule **mod);
        
        HRESULT ( STDMETHODCALLTYPE *GetName )( 
            IXCLRDataMethodInstance * This,
            /* [in] */ ULONG32 flags,
            /* [in] */ ULONG32 bufLen,
            /* [out] */ ULONG32 *nameLen,
            /* [size_is][out] */ WCHAR nameBuf[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *GetFlags )( 
            IXCLRDataMethodInstance * This,
            /* [out] */ ULONG32 *flags);
        
        HRESULT ( STDMETHODCALLTYPE *IsSameObject )( 
            IXCLRDataMethodInstance * This,
            /* [in] */ IXCLRDataMethodInstance *method);
        
        HRESULT ( STDMETHODCALLTYPE *GetEnCVersion )( 
            IXCLRDataMethodInstance * This,
            /* [out] */ ULONG32 *version);
        
        HRESULT ( STDMETHODCALLTYPE *GetNumTypeArguments )( 
            IXCLRDataMethodInstance * This,
            /* [out] */ ULONG32 *numTypeArgs);
        
        HRESULT ( STDMETHODCALLTYPE *GetTypeArgumentByIndex )( 
            IXCLRDataMethodInstance * This,
            /* [in] */ ULONG32 index,
            /* [out] */ IXCLRDataTypeInstance **typeArg);
        
        HRESULT ( STDMETHODCALLTYPE *GetILOffsetsByAddress )( 
            IXCLRDataMethodInstance * This,
            /* [in] */ CLRDATA_ADDRESS address,
            /* [in] */ ULONG32 offsetsLen,
            /* [out] */ ULONG32 *offsetsNeeded,
            /* [size_is][out] */ ULONG32 ilOffsets[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *GetAddressRangesByILOffset )( 
            IXCLRDataMethodInstance * This,
            /* [in] */ ULONG32 ilOffset,
            /* [in] */ ULONG32 rangesLen,
            /* [out] */ ULONG32 *rangesNeeded,
            /* [size_is][out] */ CLRDATA_ADDRESS_RANGE addressRanges[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *GetILAddressMap )( 
            IXCLRDataMethodInstance * This,
            /* [in] */ ULONG32 mapLen,
            /* [out] */ ULONG32 *mapNeeded,
            /* [size_is][out] */ CLRDATA_IL_ADDRESS_MAP maps[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *StartEnumExtents )( 
            IXCLRDataMethodInstance * This,
            /* [out] */ CLRDATA_ENUM *handle);
        
        HRESULT ( STDMETHODCALLTYPE *EnumExtent )( 
            IXCLRDataMethodInstance * This,
            /* [out][in] */ CLRDATA_ENUM *handle,
            /* [out] */ CLRDATA_ADDRESS_RANGE *extent);
        
        HRESULT ( STDMETHODCALLTYPE *EndEnumExtents )( 
            IXCLRDataMethodInstance * This,
            /* [in] */ CLRDATA_ENUM handle);
        
        HRESULT ( STDMETHODCALLTYPE *Request )( 
            IXCLRDataMethodInstance * This,
            /* [in] */ ULONG32 reqCode,
            /* [in] */ ULONG32 inBufferSize,
            /* [size_is][in] */ BYTE *inBuffer,
            /* [in] */ ULONG32 outBufferSize,
            /* [size_is][out] */ BYTE *outBuffer);
        
        HRESULT ( STDMETHODCALLTYPE *GetRepresentativeEntryAddress )( 
            IXCLRDataMethodInstance * This,
            /* [out] */ CLRDATA_ADDRESS *addr);
        
        END_INTERFACE
    } IXCLRDataMethodInstanceVtbl;

    interface IXCLRDataMethodInstance
    {
        CONST_VTBL struct IXCLRDataMethodInstanceVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define IXCLRDataMethodInstance_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define IXCLRDataMethodInstance_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define IXCLRDataMethodInstance_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define IXCLRDataMethodInstance_GetTypeInstance(This,typeInstance)	\
    ( (This)->lpVtbl -> GetTypeInstance(This,typeInstance) ) 

#define IXCLRDataMethodInstance_GetDefinition(This,methodDefinition)	\
    ( (This)->lpVtbl -> GetDefinition(This,methodDefinition) ) 

#define IXCLRDataMethodInstance_GetTokenAndScope(This,token,mod)	\
    ( (This)->lpVtbl -> GetTokenAndScope(This,token,mod) ) 

#define IXCLRDataMethodInstance_GetName(This,flags,bufLen,nameLen,nameBuf)	\
    ( (This)->lpVtbl -> GetName(This,flags,bufLen,nameLen,nameBuf) ) 

#define IXCLRDataMethodInstance_GetFlags(This,flags)	\
    ( (This)->lpVtbl -> GetFlags(This,flags) ) 

#define IXCLRDataMethodInstance_IsSameObject(This,method)	\
    ( (This)->lpVtbl -> IsSameObject(This,method) ) 

#define IXCLRDataMethodInstance_GetEnCVersion(This,version)	\
    ( (This)->lpVtbl -> GetEnCVersion(This,version) ) 

#define IXCLRDataMethodInstance_GetNumTypeArguments(This,numTypeArgs)	\
    ( (This)->lpVtbl -> GetNumTypeArguments(This,numTypeArgs) ) 

#define IXCLRDataMethodInstance_GetTypeArgumentByIndex(This,index,typeArg)	\
    ( (This)->lpVtbl -> GetTypeArgumentByIndex(This,index,typeArg) ) 

#define IXCLRDataMethodInstance_GetILOffsetsByAddress(This,address,offsetsLen,offsetsNeeded,ilOffsets)	\
    ( (This)->lpVtbl -> GetILOffsetsByAddress(This,address,offsetsLen,offsetsNeeded,ilOffsets) ) 

#define IXCLRDataMethodInstance_GetAddressRangesByILOffset(This,ilOffset,rangesLen,rangesNeeded,addressRanges)	\
    ( (This)->lpVtbl -> GetAddressRangesByILOffset(This,ilOffset,rangesLen,rangesNeeded,addressRanges) ) 

#define IXCLRDataMethodInstance_GetILAddressMap(This,mapLen,mapNeeded,maps)	\
    ( (This)->lpVtbl -> GetILAddressMap(This,mapLen,mapNeeded,maps) ) 

#define IXCLRDataMethodInstance_StartEnumExtents(This,handle)	\
    ( (This)->lpVtbl -> StartEnumExtents(This,handle) ) 

#define IXCLRDataMethodInstance_EnumExtent(This,handle,extent)	\
    ( (This)->lpVtbl -> EnumExtent(This,handle,extent) ) 

#define IXCLRDataMethodInstance_EndEnumExtents(This,handle)	\
    ( (This)->lpVtbl -> EndEnumExtents(This,handle) ) 

#define IXCLRDataMethodInstance_Request(This,reqCode,inBufferSize,inBuffer,outBufferSize,outBuffer)	\
    ( (This)->lpVtbl -> Request(This,reqCode,inBufferSize,inBuffer,outBufferSize,outBuffer) ) 

#define IXCLRDataMethodInstance_GetRepresentativeEntryAddress(This,addr)	\
    ( (This)->lpVtbl -> GetRepresentativeEntryAddress(This,addr) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __IXCLRDataMethodInstance_INTERFACE_DEFINED__ */


/* interface __MIDL_itf_xclrdata_0000_0014 */
/* [local] */ 

#pragma warning(pop)
typedef /* [public] */ 
enum __MIDL___MIDL_itf_xclrdata_0000_0014_0001
    {
        CLRDATA_TASK_DEFAULT	= 0,
        CLRDATA_TASK_WAITING_FOR_GC	= 0x1
    } 	CLRDataTaskFlag;

#pragma warning(push)
#pragma warning(disable:28718)	


extern RPC_IF_HANDLE __MIDL_itf_xclrdata_0000_0014_v0_0_c_ifspec;
extern RPC_IF_HANDLE __MIDL_itf_xclrdata_0000_0014_v0_0_s_ifspec;

#ifndef __IXCLRDataTask_INTERFACE_DEFINED__
#define __IXCLRDataTask_INTERFACE_DEFINED__

/* interface IXCLRDataTask */
/* [uuid][local][object] */ 


EXTERN_C const IID IID_IXCLRDataTask;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("A5B0BEEA-EC62-4618-8012-A24FFC23934C")
    IXCLRDataTask : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE GetProcess( 
            /* [out] */ IXCLRDataProcess **process) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetCurrentAppDomain( 
            /* [out] */ IXCLRDataAppDomain **appDomain) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetUniqueID( 
            /* [out] */ ULONG64 *id) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetFlags( 
            /* [out] */ ULONG32 *flags) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE IsSameObject( 
            /* [in] */ IXCLRDataTask *task) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetManagedObject( 
            /* [out] */ IXCLRDataValue **value) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetDesiredExecutionState( 
            /* [out] */ ULONG32 *state) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE SetDesiredExecutionState( 
            /* [in] */ ULONG32 state) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE CreateStackWalk( 
            /* [in] */ ULONG32 flags,
            /* [out] */ IXCLRDataStackWalk **stackWalk) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetOSThreadID( 
            /* [out] */ ULONG32 *id) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetContext( 
            /* [in] */ ULONG32 contextFlags,
            /* [in] */ ULONG32 contextBufSize,
            /* [out] */ ULONG32 *contextSize,
            /* [size_is][out] */ BYTE contextBuf[  ]) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE SetContext( 
            /* [in] */ ULONG32 contextSize,
            /* [size_is][in] */ BYTE context[  ]) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetCurrentExceptionState( 
            /* [out] */ IXCLRDataExceptionState **exception) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE Request( 
            /* [in] */ ULONG32 reqCode,
            /* [in] */ ULONG32 inBufferSize,
            /* [size_is][in] */ BYTE *inBuffer,
            /* [in] */ ULONG32 outBufferSize,
            /* [size_is][out] */ BYTE *outBuffer) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetName( 
            /* [in] */ ULONG32 bufLen,
            /* [out] */ ULONG32 *nameLen,
            /* [size_is][out] */ WCHAR name[  ]) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetLastExceptionState( 
            /* [out] */ IXCLRDataExceptionState **exception) = 0;
        
    };
    
    
#else 	/* C style interface */

    typedef struct IXCLRDataTaskVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            IXCLRDataTask * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            IXCLRDataTask * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            IXCLRDataTask * This);
        
        HRESULT ( STDMETHODCALLTYPE *GetProcess )( 
            IXCLRDataTask * This,
            /* [out] */ IXCLRDataProcess **process);
        
        HRESULT ( STDMETHODCALLTYPE *GetCurrentAppDomain )( 
            IXCLRDataTask * This,
            /* [out] */ IXCLRDataAppDomain **appDomain);
        
        HRESULT ( STDMETHODCALLTYPE *GetUniqueID )( 
            IXCLRDataTask * This,
            /* [out] */ ULONG64 *id);
        
        HRESULT ( STDMETHODCALLTYPE *GetFlags )( 
            IXCLRDataTask * This,
            /* [out] */ ULONG32 *flags);
        
        HRESULT ( STDMETHODCALLTYPE *IsSameObject )( 
            IXCLRDataTask * This,
            /* [in] */ IXCLRDataTask *task);
        
        HRESULT ( STDMETHODCALLTYPE *GetManagedObject )( 
            IXCLRDataTask * This,
            /* [out] */ IXCLRDataValue **value);
        
        HRESULT ( STDMETHODCALLTYPE *GetDesiredExecutionState )( 
            IXCLRDataTask * This,
            /* [out] */ ULONG32 *state);
        
        HRESULT ( STDMETHODCALLTYPE *SetDesiredExecutionState )( 
            IXCLRDataTask * This,
            /* [in] */ ULONG32 state);
        
        HRESULT ( STDMETHODCALLTYPE *CreateStackWalk )( 
            IXCLRDataTask * This,
            /* [in] */ ULONG32 flags,
            /* [out] */ IXCLRDataStackWalk **stackWalk);
        
        HRESULT ( STDMETHODCALLTYPE *GetOSThreadID )( 
            IXCLRDataTask * This,
            /* [out] */ ULONG32 *id);
        
        HRESULT ( STDMETHODCALLTYPE *GetContext )( 
            IXCLRDataTask * This,
            /* [in] */ ULONG32 contextFlags,
            /* [in] */ ULONG32 contextBufSize,
            /* [out] */ ULONG32 *contextSize,
            /* [size_is][out] */ BYTE contextBuf[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *SetContext )( 
            IXCLRDataTask * This,
            /* [in] */ ULONG32 contextSize,
            /* [size_is][in] */ BYTE context[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *GetCurrentExceptionState )( 
            IXCLRDataTask * This,
            /* [out] */ IXCLRDataExceptionState **exception);
        
        HRESULT ( STDMETHODCALLTYPE *Request )( 
            IXCLRDataTask * This,
            /* [in] */ ULONG32 reqCode,
            /* [in] */ ULONG32 inBufferSize,
            /* [size_is][in] */ BYTE *inBuffer,
            /* [in] */ ULONG32 outBufferSize,
            /* [size_is][out] */ BYTE *outBuffer);
        
        HRESULT ( STDMETHODCALLTYPE *GetName )( 
            IXCLRDataTask * This,
            /* [in] */ ULONG32 bufLen,
            /* [out] */ ULONG32 *nameLen,
            /* [size_is][out] */ WCHAR name[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *GetLastExceptionState )( 
            IXCLRDataTask * This,
            /* [out] */ IXCLRDataExceptionState **exception);
        
        END_INTERFACE
    } IXCLRDataTaskVtbl;

    interface IXCLRDataTask
    {
        CONST_VTBL struct IXCLRDataTaskVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define IXCLRDataTask_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define IXCLRDataTask_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define IXCLRDataTask_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define IXCLRDataTask_GetProcess(This,process)	\
    ( (This)->lpVtbl -> GetProcess(This,process) ) 

#define IXCLRDataTask_GetCurrentAppDomain(This,appDomain)	\
    ( (This)->lpVtbl -> GetCurrentAppDomain(This,appDomain) ) 

#define IXCLRDataTask_GetUniqueID(This,id)	\
    ( (This)->lpVtbl -> GetUniqueID(This,id) ) 

#define IXCLRDataTask_GetFlags(This,flags)	\
    ( (This)->lpVtbl -> GetFlags(This,flags) ) 

#define IXCLRDataTask_IsSameObject(This,task)	\
    ( (This)->lpVtbl -> IsSameObject(This,task) ) 

#define IXCLRDataTask_GetManagedObject(This,value)	\
    ( (This)->lpVtbl -> GetManagedObject(This,value) ) 

#define IXCLRDataTask_GetDesiredExecutionState(This,state)	\
    ( (This)->lpVtbl -> GetDesiredExecutionState(This,state) ) 

#define IXCLRDataTask_SetDesiredExecutionState(This,state)	\
    ( (This)->lpVtbl -> SetDesiredExecutionState(This,state) ) 

#define IXCLRDataTask_CreateStackWalk(This,flags,stackWalk)	\
    ( (This)->lpVtbl -> CreateStackWalk(This,flags,stackWalk) ) 

#define IXCLRDataTask_GetOSThreadID(This,id)	\
    ( (This)->lpVtbl -> GetOSThreadID(This,id) ) 

#define IXCLRDataTask_GetContext(This,contextFlags,contextBufSize,contextSize,contextBuf)	\
    ( (This)->lpVtbl -> GetContext(This,contextFlags,contextBufSize,contextSize,contextBuf) ) 

#define IXCLRDataTask_SetContext(This,contextSize,context)	\
    ( (This)->lpVtbl -> SetContext(This,contextSize,context) ) 

#define IXCLRDataTask_GetCurrentExceptionState(This,exception)	\
    ( (This)->lpVtbl -> GetCurrentExceptionState(This,exception) ) 

#define IXCLRDataTask_Request(This,reqCode,inBufferSize,inBuffer,outBufferSize,outBuffer)	\
    ( (This)->lpVtbl -> Request(This,reqCode,inBufferSize,inBuffer,outBufferSize,outBuffer) ) 

#define IXCLRDataTask_GetName(This,bufLen,nameLen,name)	\
    ( (This)->lpVtbl -> GetName(This,bufLen,nameLen,name) ) 

#define IXCLRDataTask_GetLastExceptionState(This,exception)	\
    ( (This)->lpVtbl -> GetLastExceptionState(This,exception) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __IXCLRDataTask_INTERFACE_DEFINED__ */


/* interface __MIDL_itf_xclrdata_0000_0015 */
/* [local] */ 

#pragma warning(pop)
typedef /* [public][public][public] */ 
enum __MIDL___MIDL_itf_xclrdata_0000_0015_0001
    {
        CLRDATA_SIMPFRAME_UNRECOGNIZED	= 0x1,
        CLRDATA_SIMPFRAME_MANAGED_METHOD	= 0x2,
        CLRDATA_SIMPFRAME_RUNTIME_MANAGED_CODE	= 0x4,
        CLRDATA_SIMPFRAME_RUNTIME_UNMANAGED_CODE	= 0x8
    } 	CLRDataSimpleFrameType;

typedef /* [public][public][public] */ 
enum __MIDL___MIDL_itf_xclrdata_0000_0015_0002
    {
        CLRDATA_DETFRAME_UNRECOGNIZED	= 0,
        CLRDATA_DETFRAME_UNKNOWN_STUB	= ( CLRDATA_DETFRAME_UNRECOGNIZED + 1 ) ,
        CLRDATA_DETFRAME_CLASS_INIT	= ( CLRDATA_DETFRAME_UNKNOWN_STUB + 1 ) ,
        CLRDATA_DETFRAME_EXCEPTION_FILTER	= ( CLRDATA_DETFRAME_CLASS_INIT + 1 ) ,
        CLRDATA_DETFRAME_SECURITY	= ( CLRDATA_DETFRAME_EXCEPTION_FILTER + 1 ) ,
        CLRDATA_DETFRAME_CONTEXT_POLICY	= ( CLRDATA_DETFRAME_SECURITY + 1 ) ,
        CLRDATA_DETFRAME_INTERCEPTION	= ( CLRDATA_DETFRAME_CONTEXT_POLICY + 1 ) ,
        CLRDATA_DETFRAME_PROCESS_START	= ( CLRDATA_DETFRAME_INTERCEPTION + 1 ) ,
        CLRDATA_DETFRAME_THREAD_START	= ( CLRDATA_DETFRAME_PROCESS_START + 1 ) ,
        CLRDATA_DETFRAME_TRANSITION_TO_MANAGED	= ( CLRDATA_DETFRAME_THREAD_START + 1 ) ,
        CLRDATA_DETFRAME_TRANSITION_TO_UNMANAGED	= ( CLRDATA_DETFRAME_TRANSITION_TO_MANAGED + 1 ) ,
        CLRDATA_DETFRAME_COM_INTEROP_STUB	= ( CLRDATA_DETFRAME_TRANSITION_TO_UNMANAGED + 1 ) ,
        CLRDATA_DETFRAME_DEBUGGER_EVAL	= ( CLRDATA_DETFRAME_COM_INTEROP_STUB + 1 ) ,
        CLRDATA_DETFRAME_CONTEXT_SWITCH	= ( CLRDATA_DETFRAME_DEBUGGER_EVAL + 1 ) ,
        CLRDATA_DETFRAME_FUNC_EVAL	= ( CLRDATA_DETFRAME_CONTEXT_SWITCH + 1 ) ,
        CLRDATA_DETFRAME_FINALLY	= ( CLRDATA_DETFRAME_FUNC_EVAL + 1 ) 
    } 	CLRDataDetailedFrameType;

typedef /* [public] */ 
enum __MIDL___MIDL_itf_xclrdata_0000_0015_0003
    {
        CLRDATA_STACK_WALK_REQUEST_SET_FIRST_FRAME	= 0xe1000000
    } 	CLRDataStackWalkRequest;

typedef /* [public] */ 
enum __MIDL___MIDL_itf_xclrdata_0000_0015_0004
    {
        CLRDATA_STACK_SET_UNWIND_CONTEXT	= 0,
        CLRDATA_STACK_SET_CURRENT_CONTEXT	= 0x1
    } 	CLRDataStackSetContextFlag;



extern RPC_IF_HANDLE __MIDL_itf_xclrdata_0000_0015_v0_0_c_ifspec;
extern RPC_IF_HANDLE __MIDL_itf_xclrdata_0000_0015_v0_0_s_ifspec;

#ifndef __IXCLRDataStackWalk_INTERFACE_DEFINED__
#define __IXCLRDataStackWalk_INTERFACE_DEFINED__

/* interface IXCLRDataStackWalk */
/* [uuid][local][object] */ 


EXTERN_C const IID IID_IXCLRDataStackWalk;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("E59D8D22-ADA7-49a2-89B5-A415AFCFC95F")
    IXCLRDataStackWalk : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE GetContext( 
            /* [in] */ ULONG32 contextFlags,
            /* [in] */ ULONG32 contextBufSize,
            /* [out] */ ULONG32 *contextSize,
            /* [size_is][out] */ BYTE contextBuf[  ]) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE SetContext( 
            /* [in] */ ULONG32 contextSize,
            /* [size_is][in] */ BYTE context[  ]) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE Next( void) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetStackSizeSkipped( 
            /* [out] */ ULONG64 *stackSizeSkipped) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetFrameType( 
            /* [out] */ CLRDataSimpleFrameType *simpleType,
            /* [out] */ CLRDataDetailedFrameType *detailedType) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetFrame( 
            /* [out] */ IXCLRDataFrame **frame) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE Request( 
            /* [in] */ ULONG32 reqCode,
            /* [in] */ ULONG32 inBufferSize,
            /* [size_is][in] */ BYTE *inBuffer,
            /* [in] */ ULONG32 outBufferSize,
            /* [size_is][out] */ BYTE *outBuffer) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE SetContext2( 
            /* [in] */ ULONG32 flags,
            /* [in] */ ULONG32 contextSize,
            /* [size_is][in] */ BYTE context[  ]) = 0;
        
    };
    
    
#else 	/* C style interface */

    typedef struct IXCLRDataStackWalkVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            IXCLRDataStackWalk * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            IXCLRDataStackWalk * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            IXCLRDataStackWalk * This);
        
        HRESULT ( STDMETHODCALLTYPE *GetContext )( 
            IXCLRDataStackWalk * This,
            /* [in] */ ULONG32 contextFlags,
            /* [in] */ ULONG32 contextBufSize,
            /* [out] */ ULONG32 *contextSize,
            /* [size_is][out] */ BYTE contextBuf[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *SetContext )( 
            IXCLRDataStackWalk * This,
            /* [in] */ ULONG32 contextSize,
            /* [size_is][in] */ BYTE context[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *Next )( 
            IXCLRDataStackWalk * This);
        
        HRESULT ( STDMETHODCALLTYPE *GetStackSizeSkipped )( 
            IXCLRDataStackWalk * This,
            /* [out] */ ULONG64 *stackSizeSkipped);
        
        HRESULT ( STDMETHODCALLTYPE *GetFrameType )( 
            IXCLRDataStackWalk * This,
            /* [out] */ CLRDataSimpleFrameType *simpleType,
            /* [out] */ CLRDataDetailedFrameType *detailedType);
        
        HRESULT ( STDMETHODCALLTYPE *GetFrame )( 
            IXCLRDataStackWalk * This,
            /* [out] */ IXCLRDataFrame **frame);
        
        HRESULT ( STDMETHODCALLTYPE *Request )( 
            IXCLRDataStackWalk * This,
            /* [in] */ ULONG32 reqCode,
            /* [in] */ ULONG32 inBufferSize,
            /* [size_is][in] */ BYTE *inBuffer,
            /* [in] */ ULONG32 outBufferSize,
            /* [size_is][out] */ BYTE *outBuffer);
        
        HRESULT ( STDMETHODCALLTYPE *SetContext2 )( 
            IXCLRDataStackWalk * This,
            /* [in] */ ULONG32 flags,
            /* [in] */ ULONG32 contextSize,
            /* [size_is][in] */ BYTE context[  ]);
        
        END_INTERFACE
    } IXCLRDataStackWalkVtbl;

    interface IXCLRDataStackWalk
    {
        CONST_VTBL struct IXCLRDataStackWalkVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define IXCLRDataStackWalk_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define IXCLRDataStackWalk_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define IXCLRDataStackWalk_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define IXCLRDataStackWalk_GetContext(This,contextFlags,contextBufSize,contextSize,contextBuf)	\
    ( (This)->lpVtbl -> GetContext(This,contextFlags,contextBufSize,contextSize,contextBuf) ) 

#define IXCLRDataStackWalk_SetContext(This,contextSize,context)	\
    ( (This)->lpVtbl -> SetContext(This,contextSize,context) ) 

#define IXCLRDataStackWalk_Next(This)	\
    ( (This)->lpVtbl -> Next(This) ) 

#define IXCLRDataStackWalk_GetStackSizeSkipped(This,stackSizeSkipped)	\
    ( (This)->lpVtbl -> GetStackSizeSkipped(This,stackSizeSkipped) ) 

#define IXCLRDataStackWalk_GetFrameType(This,simpleType,detailedType)	\
    ( (This)->lpVtbl -> GetFrameType(This,simpleType,detailedType) ) 

#define IXCLRDataStackWalk_GetFrame(This,frame)	\
    ( (This)->lpVtbl -> GetFrame(This,frame) ) 

#define IXCLRDataStackWalk_Request(This,reqCode,inBufferSize,inBuffer,outBufferSize,outBuffer)	\
    ( (This)->lpVtbl -> Request(This,reqCode,inBufferSize,inBuffer,outBufferSize,outBuffer) ) 

#define IXCLRDataStackWalk_SetContext2(This,flags,contextSize,context)	\
    ( (This)->lpVtbl -> SetContext2(This,flags,contextSize,context) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __IXCLRDataStackWalk_INTERFACE_DEFINED__ */


/* interface __MIDL_itf_xclrdata_0000_0016 */
/* [local] */ 

#pragma warning(push)
#pragma warning(disable:28718)	


extern RPC_IF_HANDLE __MIDL_itf_xclrdata_0000_0016_v0_0_c_ifspec;
extern RPC_IF_HANDLE __MIDL_itf_xclrdata_0000_0016_v0_0_s_ifspec;

#ifndef __IXCLRDataFrame_INTERFACE_DEFINED__
#define __IXCLRDataFrame_INTERFACE_DEFINED__

/* interface IXCLRDataFrame */
/* [uuid][local][object] */ 


EXTERN_C const IID IID_IXCLRDataFrame;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("271498C2-4085-4766-BC3A-7F8ED188A173")
    IXCLRDataFrame : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE GetFrameType( 
            /* [out] */ CLRDataSimpleFrameType *simpleType,
            /* [out] */ CLRDataDetailedFrameType *detailedType) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetContext( 
            /* [in] */ ULONG32 contextFlags,
            /* [in] */ ULONG32 contextBufSize,
            /* [out] */ ULONG32 *contextSize,
            /* [size_is][out] */ BYTE contextBuf[  ]) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetAppDomain( 
            /* [out] */ IXCLRDataAppDomain **appDomain) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetNumArguments( 
            /* [out] */ ULONG32 *numArgs) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetArgumentByIndex( 
            /* [in] */ ULONG32 index,
            /* [out] */ IXCLRDataValue **arg,
            /* [in] */ ULONG32 bufLen,
            /* [out] */ ULONG32 *nameLen,
            /* [size_is][out] */ WCHAR name[  ]) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetNumLocalVariables( 
            /* [out] */ ULONG32 *numLocals) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetLocalVariableByIndex( 
            /* [in] */ ULONG32 index,
            /* [out] */ IXCLRDataValue **localVariable,
            /* [in] */ ULONG32 bufLen,
            /* [out] */ ULONG32 *nameLen,
            /* [size_is][out] */ WCHAR name[  ]) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetCodeName( 
            /* [in] */ ULONG32 flags,
            /* [in] */ ULONG32 bufLen,
            /* [out] */ ULONG32 *nameLen,
            /* [size_is][out] */ WCHAR nameBuf[  ]) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetMethodInstance( 
            /* [out] */ IXCLRDataMethodInstance **method) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE Request( 
            /* [in] */ ULONG32 reqCode,
            /* [in] */ ULONG32 inBufferSize,
            /* [size_is][in] */ BYTE *inBuffer,
            /* [in] */ ULONG32 outBufferSize,
            /* [size_is][out] */ BYTE *outBuffer) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetNumTypeArguments( 
            /* [out] */ ULONG32 *numTypeArgs) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetTypeArgumentByIndex( 
            /* [in] */ ULONG32 index,
            /* [out] */ IXCLRDataTypeInstance **typeArg) = 0;
        
    };
    
    
#else 	/* C style interface */

    typedef struct IXCLRDataFrameVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            IXCLRDataFrame * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            IXCLRDataFrame * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            IXCLRDataFrame * This);
        
        HRESULT ( STDMETHODCALLTYPE *GetFrameType )( 
            IXCLRDataFrame * This,
            /* [out] */ CLRDataSimpleFrameType *simpleType,
            /* [out] */ CLRDataDetailedFrameType *detailedType);
        
        HRESULT ( STDMETHODCALLTYPE *GetContext )( 
            IXCLRDataFrame * This,
            /* [in] */ ULONG32 contextFlags,
            /* [in] */ ULONG32 contextBufSize,
            /* [out] */ ULONG32 *contextSize,
            /* [size_is][out] */ BYTE contextBuf[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *GetAppDomain )( 
            IXCLRDataFrame * This,
            /* [out] */ IXCLRDataAppDomain **appDomain);
        
        HRESULT ( STDMETHODCALLTYPE *GetNumArguments )( 
            IXCLRDataFrame * This,
            /* [out] */ ULONG32 *numArgs);
        
        HRESULT ( STDMETHODCALLTYPE *GetArgumentByIndex )( 
            IXCLRDataFrame * This,
            /* [in] */ ULONG32 index,
            /* [out] */ IXCLRDataValue **arg,
            /* [in] */ ULONG32 bufLen,
            /* [out] */ ULONG32 *nameLen,
            /* [size_is][out] */ WCHAR name[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *GetNumLocalVariables )( 
            IXCLRDataFrame * This,
            /* [out] */ ULONG32 *numLocals);
        
        HRESULT ( STDMETHODCALLTYPE *GetLocalVariableByIndex )( 
            IXCLRDataFrame * This,
            /* [in] */ ULONG32 index,
            /* [out] */ IXCLRDataValue **localVariable,
            /* [in] */ ULONG32 bufLen,
            /* [out] */ ULONG32 *nameLen,
            /* [size_is][out] */ WCHAR name[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *GetCodeName )( 
            IXCLRDataFrame * This,
            /* [in] */ ULONG32 flags,
            /* [in] */ ULONG32 bufLen,
            /* [out] */ ULONG32 *nameLen,
            /* [size_is][out] */ WCHAR nameBuf[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *GetMethodInstance )( 
            IXCLRDataFrame * This,
            /* [out] */ IXCLRDataMethodInstance **method);
        
        HRESULT ( STDMETHODCALLTYPE *Request )( 
            IXCLRDataFrame * This,
            /* [in] */ ULONG32 reqCode,
            /* [in] */ ULONG32 inBufferSize,
            /* [size_is][in] */ BYTE *inBuffer,
            /* [in] */ ULONG32 outBufferSize,
            /* [size_is][out] */ BYTE *outBuffer);
        
        HRESULT ( STDMETHODCALLTYPE *GetNumTypeArguments )( 
            IXCLRDataFrame * This,
            /* [out] */ ULONG32 *numTypeArgs);
        
        HRESULT ( STDMETHODCALLTYPE *GetTypeArgumentByIndex )( 
            IXCLRDataFrame * This,
            /* [in] */ ULONG32 index,
            /* [out] */ IXCLRDataTypeInstance **typeArg);
        
        END_INTERFACE
    } IXCLRDataFrameVtbl;

    interface IXCLRDataFrame
    {
        CONST_VTBL struct IXCLRDataFrameVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define IXCLRDataFrame_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define IXCLRDataFrame_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define IXCLRDataFrame_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define IXCLRDataFrame_GetFrameType(This,simpleType,detailedType)	\
    ( (This)->lpVtbl -> GetFrameType(This,simpleType,detailedType) ) 

#define IXCLRDataFrame_GetContext(This,contextFlags,contextBufSize,contextSize,contextBuf)	\
    ( (This)->lpVtbl -> GetContext(This,contextFlags,contextBufSize,contextSize,contextBuf) ) 

#define IXCLRDataFrame_GetAppDomain(This,appDomain)	\
    ( (This)->lpVtbl -> GetAppDomain(This,appDomain) ) 

#define IXCLRDataFrame_GetNumArguments(This,numArgs)	\
    ( (This)->lpVtbl -> GetNumArguments(This,numArgs) ) 

#define IXCLRDataFrame_GetArgumentByIndex(This,index,arg,bufLen,nameLen,name)	\
    ( (This)->lpVtbl -> GetArgumentByIndex(This,index,arg,bufLen,nameLen,name) ) 

#define IXCLRDataFrame_GetNumLocalVariables(This,numLocals)	\
    ( (This)->lpVtbl -> GetNumLocalVariables(This,numLocals) ) 

#define IXCLRDataFrame_GetLocalVariableByIndex(This,index,localVariable,bufLen,nameLen,name)	\
    ( (This)->lpVtbl -> GetLocalVariableByIndex(This,index,localVariable,bufLen,nameLen,name) ) 

#define IXCLRDataFrame_GetCodeName(This,flags,bufLen,nameLen,nameBuf)	\
    ( (This)->lpVtbl -> GetCodeName(This,flags,bufLen,nameLen,nameBuf) ) 

#define IXCLRDataFrame_GetMethodInstance(This,method)	\
    ( (This)->lpVtbl -> GetMethodInstance(This,method) ) 

#define IXCLRDataFrame_Request(This,reqCode,inBufferSize,inBuffer,outBufferSize,outBuffer)	\
    ( (This)->lpVtbl -> Request(This,reqCode,inBufferSize,inBuffer,outBufferSize,outBuffer) ) 

#define IXCLRDataFrame_GetNumTypeArguments(This,numTypeArgs)	\
    ( (This)->lpVtbl -> GetNumTypeArguments(This,numTypeArgs) ) 

#define IXCLRDataFrame_GetTypeArgumentByIndex(This,index,typeArg)	\
    ( (This)->lpVtbl -> GetTypeArgumentByIndex(This,index,typeArg) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __IXCLRDataFrame_INTERFACE_DEFINED__ */


/* interface __MIDL_itf_xclrdata_0000_0017 */
/* [local] */ 

#pragma warning(pop)


extern RPC_IF_HANDLE __MIDL_itf_xclrdata_0000_0017_v0_0_c_ifspec;
extern RPC_IF_HANDLE __MIDL_itf_xclrdata_0000_0017_v0_0_s_ifspec;

#ifndef __IXCLRDataFrame2_INTERFACE_DEFINED__
#define __IXCLRDataFrame2_INTERFACE_DEFINED__

/* interface IXCLRDataFrame2 */
/* [uuid][local][object] */ 


EXTERN_C const IID IID_IXCLRDataFrame2;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("1C4D9A4B-702D-4CF6-B290-1DB6F43050D0")
    IXCLRDataFrame2 : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE GetExactGenericArgsToken( 
            /* [out] */ IXCLRDataValue **genericToken) = 0;
        
    };
    
    
#else 	/* C style interface */

    typedef struct IXCLRDataFrame2Vtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            IXCLRDataFrame2 * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            IXCLRDataFrame2 * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            IXCLRDataFrame2 * This);
        
        HRESULT ( STDMETHODCALLTYPE *GetExactGenericArgsToken )( 
            IXCLRDataFrame2 * This,
            /* [out] */ IXCLRDataValue **genericToken);
        
        END_INTERFACE
    } IXCLRDataFrame2Vtbl;

    interface IXCLRDataFrame2
    {
        CONST_VTBL struct IXCLRDataFrame2Vtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define IXCLRDataFrame2_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define IXCLRDataFrame2_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define IXCLRDataFrame2_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define IXCLRDataFrame2_GetExactGenericArgsToken(This,genericToken)	\
    ( (This)->lpVtbl -> GetExactGenericArgsToken(This,genericToken) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __IXCLRDataFrame2_INTERFACE_DEFINED__ */


/* interface __MIDL_itf_xclrdata_0000_0018 */
/* [local] */ 

typedef /* [public] */ 
enum __MIDL___MIDL_itf_xclrdata_0000_0018_0001
    {
        CLRDATA_EXCEPTION_DEFAULT	= 0,
        CLRDATA_EXCEPTION_NESTED	= 0x1,
        CLRDATA_EXCEPTION_PARTIAL	= 0x2
    } 	CLRDataExceptionStateFlag;

typedef /* [public][public] */ 
enum __MIDL___MIDL_itf_xclrdata_0000_0018_0002
    {
        CLRDATA_EXBASE_EXCEPTION	= 0,
        CLRDATA_EXBASE_OUT_OF_MEMORY	= ( CLRDATA_EXBASE_EXCEPTION + 1 ) ,
        CLRDATA_EXBASE_INVALID_ARGUMENT	= ( CLRDATA_EXBASE_OUT_OF_MEMORY + 1 ) 
    } 	CLRDataBaseExceptionType;

typedef /* [public] */ 
enum __MIDL___MIDL_itf_xclrdata_0000_0018_0003
    {
        CLRDATA_EXSAME_SECOND_CHANCE	= 0,
        CLRDATA_EXSAME_FIRST_CHANCE	= 0x1
    } 	CLRDataExceptionSameFlag;

#pragma warning(push)
#pragma warning(disable:28718)	


extern RPC_IF_HANDLE __MIDL_itf_xclrdata_0000_0018_v0_0_c_ifspec;
extern RPC_IF_HANDLE __MIDL_itf_xclrdata_0000_0018_v0_0_s_ifspec;

#ifndef __IXCLRDataExceptionState_INTERFACE_DEFINED__
#define __IXCLRDataExceptionState_INTERFACE_DEFINED__

/* interface IXCLRDataExceptionState */
/* [uuid][local][object] */ 


EXTERN_C const IID IID_IXCLRDataExceptionState;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("75DA9E4C-BD33-43C8-8F5C-96E8A5241F57")
    IXCLRDataExceptionState : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE GetFlags( 
            /* [out] */ ULONG32 *flags) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetPrevious( 
            /* [out] */ IXCLRDataExceptionState **exState) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetManagedObject( 
            /* [out] */ IXCLRDataValue **value) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetBaseType( 
            /* [out] */ CLRDataBaseExceptionType *type) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetCode( 
            /* [out] */ ULONG32 *code) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetString( 
            /* [in] */ ULONG32 bufLen,
            /* [out] */ ULONG32 *strLen,
            /* [size_is][out] */ WCHAR str[  ]) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE Request( 
            /* [in] */ ULONG32 reqCode,
            /* [in] */ ULONG32 inBufferSize,
            /* [size_is][in] */ BYTE *inBuffer,
            /* [in] */ ULONG32 outBufferSize,
            /* [size_is][out] */ BYTE *outBuffer) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE IsSameState( 
            /* [in] */ EXCEPTION_RECORD64 *exRecord,
            /* [in] */ ULONG32 contextSize,
            /* [size_is][in] */ BYTE cxRecord[  ]) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE IsSameState2( 
            /* [in] */ ULONG32 flags,
            /* [in] */ EXCEPTION_RECORD64 *exRecord,
            /* [in] */ ULONG32 contextSize,
            /* [size_is][in] */ BYTE cxRecord[  ]) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetTask( 
            /* [out] */ IXCLRDataTask **task) = 0;
        
    };
    
    
#else 	/* C style interface */

    typedef struct IXCLRDataExceptionStateVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            IXCLRDataExceptionState * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            IXCLRDataExceptionState * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            IXCLRDataExceptionState * This);
        
        HRESULT ( STDMETHODCALLTYPE *GetFlags )( 
            IXCLRDataExceptionState * This,
            /* [out] */ ULONG32 *flags);
        
        HRESULT ( STDMETHODCALLTYPE *GetPrevious )( 
            IXCLRDataExceptionState * This,
            /* [out] */ IXCLRDataExceptionState **exState);
        
        HRESULT ( STDMETHODCALLTYPE *GetManagedObject )( 
            IXCLRDataExceptionState * This,
            /* [out] */ IXCLRDataValue **value);
        
        HRESULT ( STDMETHODCALLTYPE *GetBaseType )( 
            IXCLRDataExceptionState * This,
            /* [out] */ CLRDataBaseExceptionType *type);
        
        HRESULT ( STDMETHODCALLTYPE *GetCode )( 
            IXCLRDataExceptionState * This,
            /* [out] */ ULONG32 *code);
        
        HRESULT ( STDMETHODCALLTYPE *GetString )( 
            IXCLRDataExceptionState * This,
            /* [in] */ ULONG32 bufLen,
            /* [out] */ ULONG32 *strLen,
            /* [size_is][out] */ WCHAR str[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *Request )( 
            IXCLRDataExceptionState * This,
            /* [in] */ ULONG32 reqCode,
            /* [in] */ ULONG32 inBufferSize,
            /* [size_is][in] */ BYTE *inBuffer,
            /* [in] */ ULONG32 outBufferSize,
            /* [size_is][out] */ BYTE *outBuffer);
        
        HRESULT ( STDMETHODCALLTYPE *IsSameState )( 
            IXCLRDataExceptionState * This,
            /* [in] */ EXCEPTION_RECORD64 *exRecord,
            /* [in] */ ULONG32 contextSize,
            /* [size_is][in] */ BYTE cxRecord[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *IsSameState2 )( 
            IXCLRDataExceptionState * This,
            /* [in] */ ULONG32 flags,
            /* [in] */ EXCEPTION_RECORD64 *exRecord,
            /* [in] */ ULONG32 contextSize,
            /* [size_is][in] */ BYTE cxRecord[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *GetTask )( 
            IXCLRDataExceptionState * This,
            /* [out] */ IXCLRDataTask **task);
        
        END_INTERFACE
    } IXCLRDataExceptionStateVtbl;

    interface IXCLRDataExceptionState
    {
        CONST_VTBL struct IXCLRDataExceptionStateVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define IXCLRDataExceptionState_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define IXCLRDataExceptionState_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define IXCLRDataExceptionState_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define IXCLRDataExceptionState_GetFlags(This,flags)	\
    ( (This)->lpVtbl -> GetFlags(This,flags) ) 

#define IXCLRDataExceptionState_GetPrevious(This,exState)	\
    ( (This)->lpVtbl -> GetPrevious(This,exState) ) 

#define IXCLRDataExceptionState_GetManagedObject(This,value)	\
    ( (This)->lpVtbl -> GetManagedObject(This,value) ) 

#define IXCLRDataExceptionState_GetBaseType(This,type)	\
    ( (This)->lpVtbl -> GetBaseType(This,type) ) 

#define IXCLRDataExceptionState_GetCode(This,code)	\
    ( (This)->lpVtbl -> GetCode(This,code) ) 

#define IXCLRDataExceptionState_GetString(This,bufLen,strLen,str)	\
    ( (This)->lpVtbl -> GetString(This,bufLen,strLen,str) ) 

#define IXCLRDataExceptionState_Request(This,reqCode,inBufferSize,inBuffer,outBufferSize,outBuffer)	\
    ( (This)->lpVtbl -> Request(This,reqCode,inBufferSize,inBuffer,outBufferSize,outBuffer) ) 

#define IXCLRDataExceptionState_IsSameState(This,exRecord,contextSize,cxRecord)	\
    ( (This)->lpVtbl -> IsSameState(This,exRecord,contextSize,cxRecord) ) 

#define IXCLRDataExceptionState_IsSameState2(This,flags,exRecord,contextSize,cxRecord)	\
    ( (This)->lpVtbl -> IsSameState2(This,flags,exRecord,contextSize,cxRecord) ) 

#define IXCLRDataExceptionState_GetTask(This,task)	\
    ( (This)->lpVtbl -> GetTask(This,task) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __IXCLRDataExceptionState_INTERFACE_DEFINED__ */


/* interface __MIDL_itf_xclrdata_0000_0019 */
/* [local] */ 

#pragma warning(pop)
typedef /* [public] */ 
enum __MIDL___MIDL_itf_xclrdata_0000_0019_0001
    {
        CLRDATA_VLOC_MEMORY	= 0,
        CLRDATA_VLOC_REGISTER	= 0x1
    } 	ClrDataValueLocationFlag;

#pragma warning(push)
#pragma warning(disable:28718)	


extern RPC_IF_HANDLE __MIDL_itf_xclrdata_0000_0019_v0_0_c_ifspec;
extern RPC_IF_HANDLE __MIDL_itf_xclrdata_0000_0019_v0_0_s_ifspec;

#ifndef __IXCLRDataValue_INTERFACE_DEFINED__
#define __IXCLRDataValue_INTERFACE_DEFINED__

/* interface IXCLRDataValue */
/* [uuid][local][object] */ 


EXTERN_C const IID IID_IXCLRDataValue;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("96EC93C7-1000-4e93-8991-98D8766E6666")
    IXCLRDataValue : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE GetFlags( 
            /* [out] */ ULONG32 *flags) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetAddress( 
            /* [out] */ CLRDATA_ADDRESS *address) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetSize( 
            /* [out] */ ULONG64 *size) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetBytes( 
            /* [in] */ ULONG32 bufLen,
            /* [out] */ ULONG32 *dataSize,
            /* [size_is][out] */ BYTE buffer[  ]) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE SetBytes( 
            /* [in] */ ULONG32 bufLen,
            /* [out] */ ULONG32 *dataSize,
            /* [size_is][in] */ BYTE buffer[  ]) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetType( 
            /* [out] */ IXCLRDataTypeInstance **typeInstance) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetNumFields( 
            /* [out] */ ULONG32 *numFields) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetFieldByIndex( 
            /* [in] */ ULONG32 index,
            /* [out] */ IXCLRDataValue **field,
            /* [in] */ ULONG32 bufLen,
            /* [out] */ ULONG32 *nameLen,
            /* [size_is][out] */ WCHAR nameBuf[  ],
            /* [out] */ mdFieldDef *token) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE Request( 
            /* [in] */ ULONG32 reqCode,
            /* [in] */ ULONG32 inBufferSize,
            /* [size_is][in] */ BYTE *inBuffer,
            /* [in] */ ULONG32 outBufferSize,
            /* [size_is][out] */ BYTE *outBuffer) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetNumFields2( 
            /* [in] */ ULONG32 flags,
            /* [in] */ IXCLRDataTypeInstance *fromType,
            /* [out] */ ULONG32 *numFields) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE StartEnumFields( 
            /* [in] */ ULONG32 flags,
            /* [in] */ IXCLRDataTypeInstance *fromType,
            /* [out] */ CLRDATA_ENUM *handle) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE EnumField( 
            /* [out][in] */ CLRDATA_ENUM *handle,
            /* [out] */ IXCLRDataValue **field,
            /* [in] */ ULONG32 nameBufLen,
            /* [out] */ ULONG32 *nameLen,
            /* [size_is][out] */ WCHAR nameBuf[  ],
            /* [out] */ mdFieldDef *token) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE EndEnumFields( 
            /* [in] */ CLRDATA_ENUM handle) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE StartEnumFieldsByName( 
            /* [in] */ LPCWSTR name,
            /* [in] */ ULONG32 nameFlags,
            /* [in] */ ULONG32 fieldFlags,
            /* [in] */ IXCLRDataTypeInstance *fromType,
            /* [out] */ CLRDATA_ENUM *handle) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE EnumFieldByName( 
            /* [out][in] */ CLRDATA_ENUM *handle,
            /* [out] */ IXCLRDataValue **field,
            /* [out] */ mdFieldDef *token) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE EndEnumFieldsByName( 
            /* [in] */ CLRDATA_ENUM handle) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetFieldByToken( 
            /* [in] */ mdFieldDef token,
            /* [out] */ IXCLRDataValue **field,
            /* [in] */ ULONG32 bufLen,
            /* [out] */ ULONG32 *nameLen,
            /* [size_is][out] */ WCHAR nameBuf[  ]) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetAssociatedValue( 
            /* [out] */ IXCLRDataValue **assocValue) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetAssociatedType( 
            /* [out] */ IXCLRDataTypeInstance **assocType) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetString( 
            /* [in] */ ULONG32 bufLen,
            /* [out] */ ULONG32 *strLen,
            /* [size_is][out] */ WCHAR str[  ]) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetArrayProperties( 
            /* [out] */ ULONG32 *rank,
            /* [out] */ ULONG32 *totalElements,
            /* [in] */ ULONG32 numDim,
            /* [size_is][out] */ ULONG32 dims[  ],
            /* [in] */ ULONG32 numBases,
            /* [size_is][out] */ LONG32 bases[  ]) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetArrayElement( 
            /* [in] */ ULONG32 numInd,
            /* [size_is][in] */ LONG32 indices[  ],
            /* [out] */ IXCLRDataValue **value) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE EnumField2( 
            /* [out][in] */ CLRDATA_ENUM *handle,
            /* [out] */ IXCLRDataValue **field,
            /* [in] */ ULONG32 nameBufLen,
            /* [out] */ ULONG32 *nameLen,
            /* [size_is][out] */ WCHAR nameBuf[  ],
            /* [out] */ IXCLRDataModule **tokenScope,
            /* [out] */ mdFieldDef *token) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE EnumFieldByName2( 
            /* [out][in] */ CLRDATA_ENUM *handle,
            /* [out] */ IXCLRDataValue **field,
            /* [out] */ IXCLRDataModule **tokenScope,
            /* [out] */ mdFieldDef *token) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetFieldByToken2( 
            /* [in] */ IXCLRDataModule *tokenScope,
            /* [in] */ mdFieldDef token,
            /* [out] */ IXCLRDataValue **field,
            /* [in] */ ULONG32 bufLen,
            /* [out] */ ULONG32 *nameLen,
            /* [size_is][out] */ WCHAR nameBuf[  ]) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetNumLocations( 
            /* [out] */ ULONG32 *numLocs) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetLocationByIndex( 
            /* [in] */ ULONG32 loc,
            /* [out] */ ULONG32 *flags,
            /* [out] */ CLRDATA_ADDRESS *arg) = 0;
        
    };
    
    
#else 	/* C style interface */

    typedef struct IXCLRDataValueVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            IXCLRDataValue * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            IXCLRDataValue * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            IXCLRDataValue * This);
        
        HRESULT ( STDMETHODCALLTYPE *GetFlags )( 
            IXCLRDataValue * This,
            /* [out] */ ULONG32 *flags);
        
        HRESULT ( STDMETHODCALLTYPE *GetAddress )( 
            IXCLRDataValue * This,
            /* [out] */ CLRDATA_ADDRESS *address);
        
        HRESULT ( STDMETHODCALLTYPE *GetSize )( 
            IXCLRDataValue * This,
            /* [out] */ ULONG64 *size);
        
        HRESULT ( STDMETHODCALLTYPE *GetBytes )( 
            IXCLRDataValue * This,
            /* [in] */ ULONG32 bufLen,
            /* [out] */ ULONG32 *dataSize,
            /* [size_is][out] */ BYTE buffer[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *SetBytes )( 
            IXCLRDataValue * This,
            /* [in] */ ULONG32 bufLen,
            /* [out] */ ULONG32 *dataSize,
            /* [size_is][in] */ BYTE buffer[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *GetType )( 
            IXCLRDataValue * This,
            /* [out] */ IXCLRDataTypeInstance **typeInstance);
        
        HRESULT ( STDMETHODCALLTYPE *GetNumFields )( 
            IXCLRDataValue * This,
            /* [out] */ ULONG32 *numFields);
        
        HRESULT ( STDMETHODCALLTYPE *GetFieldByIndex )( 
            IXCLRDataValue * This,
            /* [in] */ ULONG32 index,
            /* [out] */ IXCLRDataValue **field,
            /* [in] */ ULONG32 bufLen,
            /* [out] */ ULONG32 *nameLen,
            /* [size_is][out] */ WCHAR nameBuf[  ],
            /* [out] */ mdFieldDef *token);
        
        HRESULT ( STDMETHODCALLTYPE *Request )( 
            IXCLRDataValue * This,
            /* [in] */ ULONG32 reqCode,
            /* [in] */ ULONG32 inBufferSize,
            /* [size_is][in] */ BYTE *inBuffer,
            /* [in] */ ULONG32 outBufferSize,
            /* [size_is][out] */ BYTE *outBuffer);
        
        HRESULT ( STDMETHODCALLTYPE *GetNumFields2 )( 
            IXCLRDataValue * This,
            /* [in] */ ULONG32 flags,
            /* [in] */ IXCLRDataTypeInstance *fromType,
            /* [out] */ ULONG32 *numFields);
        
        HRESULT ( STDMETHODCALLTYPE *StartEnumFields )( 
            IXCLRDataValue * This,
            /* [in] */ ULONG32 flags,
            /* [in] */ IXCLRDataTypeInstance *fromType,
            /* [out] */ CLRDATA_ENUM *handle);
        
        HRESULT ( STDMETHODCALLTYPE *EnumField )( 
            IXCLRDataValue * This,
            /* [out][in] */ CLRDATA_ENUM *handle,
            /* [out] */ IXCLRDataValue **field,
            /* [in] */ ULONG32 nameBufLen,
            /* [out] */ ULONG32 *nameLen,
            /* [size_is][out] */ WCHAR nameBuf[  ],
            /* [out] */ mdFieldDef *token);
        
        HRESULT ( STDMETHODCALLTYPE *EndEnumFields )( 
            IXCLRDataValue * This,
            /* [in] */ CLRDATA_ENUM handle);
        
        HRESULT ( STDMETHODCALLTYPE *StartEnumFieldsByName )( 
            IXCLRDataValue * This,
            /* [in] */ LPCWSTR name,
            /* [in] */ ULONG32 nameFlags,
            /* [in] */ ULONG32 fieldFlags,
            /* [in] */ IXCLRDataTypeInstance *fromType,
            /* [out] */ CLRDATA_ENUM *handle);
        
        HRESULT ( STDMETHODCALLTYPE *EnumFieldByName )( 
            IXCLRDataValue * This,
            /* [out][in] */ CLRDATA_ENUM *handle,
            /* [out] */ IXCLRDataValue **field,
            /* [out] */ mdFieldDef *token);
        
        HRESULT ( STDMETHODCALLTYPE *EndEnumFieldsByName )( 
            IXCLRDataValue * This,
            /* [in] */ CLRDATA_ENUM handle);
        
        HRESULT ( STDMETHODCALLTYPE *GetFieldByToken )( 
            IXCLRDataValue * This,
            /* [in] */ mdFieldDef token,
            /* [out] */ IXCLRDataValue **field,
            /* [in] */ ULONG32 bufLen,
            /* [out] */ ULONG32 *nameLen,
            /* [size_is][out] */ WCHAR nameBuf[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *GetAssociatedValue )( 
            IXCLRDataValue * This,
            /* [out] */ IXCLRDataValue **assocValue);
        
        HRESULT ( STDMETHODCALLTYPE *GetAssociatedType )( 
            IXCLRDataValue * This,
            /* [out] */ IXCLRDataTypeInstance **assocType);
        
        HRESULT ( STDMETHODCALLTYPE *GetString )( 
            IXCLRDataValue * This,
            /* [in] */ ULONG32 bufLen,
            /* [out] */ ULONG32 *strLen,
            /* [size_is][out] */ WCHAR str[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *GetArrayProperties )( 
            IXCLRDataValue * This,
            /* [out] */ ULONG32 *rank,
            /* [out] */ ULONG32 *totalElements,
            /* [in] */ ULONG32 numDim,
            /* [size_is][out] */ ULONG32 dims[  ],
            /* [in] */ ULONG32 numBases,
            /* [size_is][out] */ LONG32 bases[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *GetArrayElement )( 
            IXCLRDataValue * This,
            /* [in] */ ULONG32 numInd,
            /* [size_is][in] */ LONG32 indices[  ],
            /* [out] */ IXCLRDataValue **value);
        
        HRESULT ( STDMETHODCALLTYPE *EnumField2 )( 
            IXCLRDataValue * This,
            /* [out][in] */ CLRDATA_ENUM *handle,
            /* [out] */ IXCLRDataValue **field,
            /* [in] */ ULONG32 nameBufLen,
            /* [out] */ ULONG32 *nameLen,
            /* [size_is][out] */ WCHAR nameBuf[  ],
            /* [out] */ IXCLRDataModule **tokenScope,
            /* [out] */ mdFieldDef *token);
        
        HRESULT ( STDMETHODCALLTYPE *EnumFieldByName2 )( 
            IXCLRDataValue * This,
            /* [out][in] */ CLRDATA_ENUM *handle,
            /* [out] */ IXCLRDataValue **field,
            /* [out] */ IXCLRDataModule **tokenScope,
            /* [out] */ mdFieldDef *token);
        
        HRESULT ( STDMETHODCALLTYPE *GetFieldByToken2 )( 
            IXCLRDataValue * This,
            /* [in] */ IXCLRDataModule *tokenScope,
            /* [in] */ mdFieldDef token,
            /* [out] */ IXCLRDataValue **field,
            /* [in] */ ULONG32 bufLen,
            /* [out] */ ULONG32 *nameLen,
            /* [size_is][out] */ WCHAR nameBuf[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *GetNumLocations )( 
            IXCLRDataValue * This,
            /* [out] */ ULONG32 *numLocs);
        
        HRESULT ( STDMETHODCALLTYPE *GetLocationByIndex )( 
            IXCLRDataValue * This,
            /* [in] */ ULONG32 loc,
            /* [out] */ ULONG32 *flags,
            /* [out] */ CLRDATA_ADDRESS *arg);
        
        END_INTERFACE
    } IXCLRDataValueVtbl;

    interface IXCLRDataValue
    {
        CONST_VTBL struct IXCLRDataValueVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define IXCLRDataValue_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define IXCLRDataValue_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define IXCLRDataValue_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define IXCLRDataValue_GetFlags(This,flags)	\
    ( (This)->lpVtbl -> GetFlags(This,flags) ) 

#define IXCLRDataValue_GetAddress(This,address)	\
    ( (This)->lpVtbl -> GetAddress(This,address) ) 

#define IXCLRDataValue_GetSize(This,size)	\
    ( (This)->lpVtbl -> GetSize(This,size) ) 

#define IXCLRDataValue_GetBytes(This,bufLen,dataSize,buffer)	\
    ( (This)->lpVtbl -> GetBytes(This,bufLen,dataSize,buffer) ) 

#define IXCLRDataValue_SetBytes(This,bufLen,dataSize,buffer)	\
    ( (This)->lpVtbl -> SetBytes(This,bufLen,dataSize,buffer) ) 

#define IXCLRDataValue_GetType(This,typeInstance)	\
    ( (This)->lpVtbl -> GetType(This,typeInstance) ) 

#define IXCLRDataValue_GetNumFields(This,numFields)	\
    ( (This)->lpVtbl -> GetNumFields(This,numFields) ) 

#define IXCLRDataValue_GetFieldByIndex(This,index,field,bufLen,nameLen,nameBuf,token)	\
    ( (This)->lpVtbl -> GetFieldByIndex(This,index,field,bufLen,nameLen,nameBuf,token) ) 

#define IXCLRDataValue_Request(This,reqCode,inBufferSize,inBuffer,outBufferSize,outBuffer)	\
    ( (This)->lpVtbl -> Request(This,reqCode,inBufferSize,inBuffer,outBufferSize,outBuffer) ) 

#define IXCLRDataValue_GetNumFields2(This,flags,fromType,numFields)	\
    ( (This)->lpVtbl -> GetNumFields2(This,flags,fromType,numFields) ) 

#define IXCLRDataValue_StartEnumFields(This,flags,fromType,handle)	\
    ( (This)->lpVtbl -> StartEnumFields(This,flags,fromType,handle) ) 

#define IXCLRDataValue_EnumField(This,handle,field,nameBufLen,nameLen,nameBuf,token)	\
    ( (This)->lpVtbl -> EnumField(This,handle,field,nameBufLen,nameLen,nameBuf,token) ) 

#define IXCLRDataValue_EndEnumFields(This,handle)	\
    ( (This)->lpVtbl -> EndEnumFields(This,handle) ) 

#define IXCLRDataValue_StartEnumFieldsByName(This,name,nameFlags,fieldFlags,fromType,handle)	\
    ( (This)->lpVtbl -> StartEnumFieldsByName(This,name,nameFlags,fieldFlags,fromType,handle) ) 

#define IXCLRDataValue_EnumFieldByName(This,handle,field,token)	\
    ( (This)->lpVtbl -> EnumFieldByName(This,handle,field,token) ) 

#define IXCLRDataValue_EndEnumFieldsByName(This,handle)	\
    ( (This)->lpVtbl -> EndEnumFieldsByName(This,handle) ) 

#define IXCLRDataValue_GetFieldByToken(This,token,field,bufLen,nameLen,nameBuf)	\
    ( (This)->lpVtbl -> GetFieldByToken(This,token,field,bufLen,nameLen,nameBuf) ) 

#define IXCLRDataValue_GetAssociatedValue(This,assocValue)	\
    ( (This)->lpVtbl -> GetAssociatedValue(This,assocValue) ) 

#define IXCLRDataValue_GetAssociatedType(This,assocType)	\
    ( (This)->lpVtbl -> GetAssociatedType(This,assocType) ) 

#define IXCLRDataValue_GetString(This,bufLen,strLen,str)	\
    ( (This)->lpVtbl -> GetString(This,bufLen,strLen,str) ) 

#define IXCLRDataValue_GetArrayProperties(This,rank,totalElements,numDim,dims,numBases,bases)	\
    ( (This)->lpVtbl -> GetArrayProperties(This,rank,totalElements,numDim,dims,numBases,bases) ) 

#define IXCLRDataValue_GetArrayElement(This,numInd,indices,value)	\
    ( (This)->lpVtbl -> GetArrayElement(This,numInd,indices,value) ) 

#define IXCLRDataValue_EnumField2(This,handle,field,nameBufLen,nameLen,nameBuf,tokenScope,token)	\
    ( (This)->lpVtbl -> EnumField2(This,handle,field,nameBufLen,nameLen,nameBuf,tokenScope,token) ) 

#define IXCLRDataValue_EnumFieldByName2(This,handle,field,tokenScope,token)	\
    ( (This)->lpVtbl -> EnumFieldByName2(This,handle,field,tokenScope,token) ) 

#define IXCLRDataValue_GetFieldByToken2(This,tokenScope,token,field,bufLen,nameLen,nameBuf)	\
    ( (This)->lpVtbl -> GetFieldByToken2(This,tokenScope,token,field,bufLen,nameLen,nameBuf) ) 

#define IXCLRDataValue_GetNumLocations(This,numLocs)	\
    ( (This)->lpVtbl -> GetNumLocations(This,numLocs) ) 

#define IXCLRDataValue_GetLocationByIndex(This,loc,flags,arg)	\
    ( (This)->lpVtbl -> GetLocationByIndex(This,loc,flags,arg) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __IXCLRDataValue_INTERFACE_DEFINED__ */


/* interface __MIDL_itf_xclrdata_0000_0020 */
/* [local] */ 

#pragma warning(pop)


extern RPC_IF_HANDLE __MIDL_itf_xclrdata_0000_0020_v0_0_c_ifspec;
extern RPC_IF_HANDLE __MIDL_itf_xclrdata_0000_0020_v0_0_s_ifspec;

#ifndef __IXCLRDataExceptionNotification_INTERFACE_DEFINED__
#define __IXCLRDataExceptionNotification_INTERFACE_DEFINED__

/* interface IXCLRDataExceptionNotification */
/* [uuid][local][object] */ 


EXTERN_C const IID IID_IXCLRDataExceptionNotification;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("2D95A079-42A1-4837-818F-0B97D7048E0E")
    IXCLRDataExceptionNotification : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE OnCodeGenerated( 
            /* [in] */ IXCLRDataMethodInstance *method) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE OnCodeDiscarded( 
            /* [in] */ IXCLRDataMethodInstance *method) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE OnProcessExecution( 
            /* [in] */ ULONG32 state) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE OnTaskExecution( 
            /* [in] */ IXCLRDataTask *task,
            /* [in] */ ULONG32 state) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE OnModuleLoaded( 
            /* [in] */ IXCLRDataModule *mod) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE OnModuleUnloaded( 
            /* [in] */ IXCLRDataModule *mod) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE OnTypeLoaded( 
            /* [in] */ IXCLRDataTypeInstance *typeInst) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE OnTypeUnloaded( 
            /* [in] */ IXCLRDataTypeInstance *typeInst) = 0;
        
    };
    
    
#else 	/* C style interface */

    typedef struct IXCLRDataExceptionNotificationVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            IXCLRDataExceptionNotification * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            IXCLRDataExceptionNotification * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            IXCLRDataExceptionNotification * This);
        
        HRESULT ( STDMETHODCALLTYPE *OnCodeGenerated )( 
            IXCLRDataExceptionNotification * This,
            /* [in] */ IXCLRDataMethodInstance *method);
        
        HRESULT ( STDMETHODCALLTYPE *OnCodeDiscarded )( 
            IXCLRDataExceptionNotification * This,
            /* [in] */ IXCLRDataMethodInstance *method);
        
        HRESULT ( STDMETHODCALLTYPE *OnProcessExecution )( 
            IXCLRDataExceptionNotification * This,
            /* [in] */ ULONG32 state);
        
        HRESULT ( STDMETHODCALLTYPE *OnTaskExecution )( 
            IXCLRDataExceptionNotification * This,
            /* [in] */ IXCLRDataTask *task,
            /* [in] */ ULONG32 state);
        
        HRESULT ( STDMETHODCALLTYPE *OnModuleLoaded )( 
            IXCLRDataExceptionNotification * This,
            /* [in] */ IXCLRDataModule *mod);
        
        HRESULT ( STDMETHODCALLTYPE *OnModuleUnloaded )( 
            IXCLRDataExceptionNotification * This,
            /* [in] */ IXCLRDataModule *mod);
        
        HRESULT ( STDMETHODCALLTYPE *OnTypeLoaded )( 
            IXCLRDataExceptionNotification * This,
            /* [in] */ IXCLRDataTypeInstance *typeInst);
        
        HRESULT ( STDMETHODCALLTYPE *OnTypeUnloaded )( 
            IXCLRDataExceptionNotification * This,
            /* [in] */ IXCLRDataTypeInstance *typeInst);
        
        END_INTERFACE
    } IXCLRDataExceptionNotificationVtbl;

    interface IXCLRDataExceptionNotification
    {
        CONST_VTBL struct IXCLRDataExceptionNotificationVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define IXCLRDataExceptionNotification_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define IXCLRDataExceptionNotification_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define IXCLRDataExceptionNotification_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define IXCLRDataExceptionNotification_OnCodeGenerated(This,method)	\
    ( (This)->lpVtbl -> OnCodeGenerated(This,method) ) 

#define IXCLRDataExceptionNotification_OnCodeDiscarded(This,method)	\
    ( (This)->lpVtbl -> OnCodeDiscarded(This,method) ) 

#define IXCLRDataExceptionNotification_OnProcessExecution(This,state)	\
    ( (This)->lpVtbl -> OnProcessExecution(This,state) ) 

#define IXCLRDataExceptionNotification_OnTaskExecution(This,task,state)	\
    ( (This)->lpVtbl -> OnTaskExecution(This,task,state) ) 

#define IXCLRDataExceptionNotification_OnModuleLoaded(This,mod)	\
    ( (This)->lpVtbl -> OnModuleLoaded(This,mod) ) 

#define IXCLRDataExceptionNotification_OnModuleUnloaded(This,mod)	\
    ( (This)->lpVtbl -> OnModuleUnloaded(This,mod) ) 

#define IXCLRDataExceptionNotification_OnTypeLoaded(This,typeInst)	\
    ( (This)->lpVtbl -> OnTypeLoaded(This,typeInst) ) 

#define IXCLRDataExceptionNotification_OnTypeUnloaded(This,typeInst)	\
    ( (This)->lpVtbl -> OnTypeUnloaded(This,typeInst) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __IXCLRDataExceptionNotification_INTERFACE_DEFINED__ */


#ifndef __IXCLRDataExceptionNotification2_INTERFACE_DEFINED__
#define __IXCLRDataExceptionNotification2_INTERFACE_DEFINED__

/* interface IXCLRDataExceptionNotification2 */
/* [uuid][local][object] */ 


EXTERN_C const IID IID_IXCLRDataExceptionNotification2;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("31201a94-4337-49b7-aef7-0c755054091f")
    IXCLRDataExceptionNotification2 : public IXCLRDataExceptionNotification
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE OnAppDomainLoaded( 
            /* [in] */ IXCLRDataAppDomain *domain) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE OnAppDomainUnloaded( 
            /* [in] */ IXCLRDataAppDomain *domain) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE OnException( 
            /* [in] */ IXCLRDataExceptionState *exception) = 0;
        
    };
    
    
#else 	/* C style interface */

    typedef struct IXCLRDataExceptionNotification2Vtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            IXCLRDataExceptionNotification2 * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            IXCLRDataExceptionNotification2 * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            IXCLRDataExceptionNotification2 * This);
        
        HRESULT ( STDMETHODCALLTYPE *OnCodeGenerated )( 
            IXCLRDataExceptionNotification2 * This,
            /* [in] */ IXCLRDataMethodInstance *method);
        
        HRESULT ( STDMETHODCALLTYPE *OnCodeDiscarded )( 
            IXCLRDataExceptionNotification2 * This,
            /* [in] */ IXCLRDataMethodInstance *method);
        
        HRESULT ( STDMETHODCALLTYPE *OnProcessExecution )( 
            IXCLRDataExceptionNotification2 * This,
            /* [in] */ ULONG32 state);
        
        HRESULT ( STDMETHODCALLTYPE *OnTaskExecution )( 
            IXCLRDataExceptionNotification2 * This,
            /* [in] */ IXCLRDataTask *task,
            /* [in] */ ULONG32 state);
        
        HRESULT ( STDMETHODCALLTYPE *OnModuleLoaded )( 
            IXCLRDataExceptionNotification2 * This,
            /* [in] */ IXCLRDataModule *mod);
        
        HRESULT ( STDMETHODCALLTYPE *OnModuleUnloaded )( 
            IXCLRDataExceptionNotification2 * This,
            /* [in] */ IXCLRDataModule *mod);
        
        HRESULT ( STDMETHODCALLTYPE *OnTypeLoaded )( 
            IXCLRDataExceptionNotification2 * This,
            /* [in] */ IXCLRDataTypeInstance *typeInst);
        
        HRESULT ( STDMETHODCALLTYPE *OnTypeUnloaded )( 
            IXCLRDataExceptionNotification2 * This,
            /* [in] */ IXCLRDataTypeInstance *typeInst);
        
        HRESULT ( STDMETHODCALLTYPE *OnAppDomainLoaded )( 
            IXCLRDataExceptionNotification2 * This,
            /* [in] */ IXCLRDataAppDomain *domain);
        
        HRESULT ( STDMETHODCALLTYPE *OnAppDomainUnloaded )( 
            IXCLRDataExceptionNotification2 * This,
            /* [in] */ IXCLRDataAppDomain *domain);
        
        HRESULT ( STDMETHODCALLTYPE *OnException )( 
            IXCLRDataExceptionNotification2 * This,
            /* [in] */ IXCLRDataExceptionState *exception);
        
        END_INTERFACE
    } IXCLRDataExceptionNotification2Vtbl;

    interface IXCLRDataExceptionNotification2
    {
        CONST_VTBL struct IXCLRDataExceptionNotification2Vtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define IXCLRDataExceptionNotification2_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define IXCLRDataExceptionNotification2_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define IXCLRDataExceptionNotification2_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define IXCLRDataExceptionNotification2_OnCodeGenerated(This,method)	\
    ( (This)->lpVtbl -> OnCodeGenerated(This,method) ) 

#define IXCLRDataExceptionNotification2_OnCodeDiscarded(This,method)	\
    ( (This)->lpVtbl -> OnCodeDiscarded(This,method) ) 

#define IXCLRDataExceptionNotification2_OnProcessExecution(This,state)	\
    ( (This)->lpVtbl -> OnProcessExecution(This,state) ) 

#define IXCLRDataExceptionNotification2_OnTaskExecution(This,task,state)	\
    ( (This)->lpVtbl -> OnTaskExecution(This,task,state) ) 

#define IXCLRDataExceptionNotification2_OnModuleLoaded(This,mod)	\
    ( (This)->lpVtbl -> OnModuleLoaded(This,mod) ) 

#define IXCLRDataExceptionNotification2_OnModuleUnloaded(This,mod)	\
    ( (This)->lpVtbl -> OnModuleUnloaded(This,mod) ) 

#define IXCLRDataExceptionNotification2_OnTypeLoaded(This,typeInst)	\
    ( (This)->lpVtbl -> OnTypeLoaded(This,typeInst) ) 

#define IXCLRDataExceptionNotification2_OnTypeUnloaded(This,typeInst)	\
    ( (This)->lpVtbl -> OnTypeUnloaded(This,typeInst) ) 


#define IXCLRDataExceptionNotification2_OnAppDomainLoaded(This,domain)	\
    ( (This)->lpVtbl -> OnAppDomainLoaded(This,domain) ) 

#define IXCLRDataExceptionNotification2_OnAppDomainUnloaded(This,domain)	\
    ( (This)->lpVtbl -> OnAppDomainUnloaded(This,domain) ) 

#define IXCLRDataExceptionNotification2_OnException(This,exception)	\
    ( (This)->lpVtbl -> OnException(This,exception) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __IXCLRDataExceptionNotification2_INTERFACE_DEFINED__ */


#ifndef __IXCLRDataExceptionNotification3_INTERFACE_DEFINED__
#define __IXCLRDataExceptionNotification3_INTERFACE_DEFINED__

/* interface IXCLRDataExceptionNotification3 */
/* [uuid][local][object] */ 


EXTERN_C const IID IID_IXCLRDataExceptionNotification3;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("31201a94-4337-49b7-aef7-0c7550540920")
    IXCLRDataExceptionNotification3 : public IXCLRDataExceptionNotification2
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE OnGcEvent( 
            /* [in] */ GcEvtArgs gcEvtArgs) = 0;
        
    };
    
    
#else 	/* C style interface */

    typedef struct IXCLRDataExceptionNotification3Vtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            IXCLRDataExceptionNotification3 * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            IXCLRDataExceptionNotification3 * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            IXCLRDataExceptionNotification3 * This);
        
        HRESULT ( STDMETHODCALLTYPE *OnCodeGenerated )( 
            IXCLRDataExceptionNotification3 * This,
            /* [in] */ IXCLRDataMethodInstance *method);
        
        HRESULT ( STDMETHODCALLTYPE *OnCodeDiscarded )( 
            IXCLRDataExceptionNotification3 * This,
            /* [in] */ IXCLRDataMethodInstance *method);
        
        HRESULT ( STDMETHODCALLTYPE *OnProcessExecution )( 
            IXCLRDataExceptionNotification3 * This,
            /* [in] */ ULONG32 state);
        
        HRESULT ( STDMETHODCALLTYPE *OnTaskExecution )( 
            IXCLRDataExceptionNotification3 * This,
            /* [in] */ IXCLRDataTask *task,
            /* [in] */ ULONG32 state);
        
        HRESULT ( STDMETHODCALLTYPE *OnModuleLoaded )( 
            IXCLRDataExceptionNotification3 * This,
            /* [in] */ IXCLRDataModule *mod);
        
        HRESULT ( STDMETHODCALLTYPE *OnModuleUnloaded )( 
            IXCLRDataExceptionNotification3 * This,
            /* [in] */ IXCLRDataModule *mod);
        
        HRESULT ( STDMETHODCALLTYPE *OnTypeLoaded )( 
            IXCLRDataExceptionNotification3 * This,
            /* [in] */ IXCLRDataTypeInstance *typeInst);
        
        HRESULT ( STDMETHODCALLTYPE *OnTypeUnloaded )( 
            IXCLRDataExceptionNotification3 * This,
            /* [in] */ IXCLRDataTypeInstance *typeInst);
        
        HRESULT ( STDMETHODCALLTYPE *OnAppDomainLoaded )( 
            IXCLRDataExceptionNotification3 * This,
            /* [in] */ IXCLRDataAppDomain *domain);
        
        HRESULT ( STDMETHODCALLTYPE *OnAppDomainUnloaded )( 
            IXCLRDataExceptionNotification3 * This,
            /* [in] */ IXCLRDataAppDomain *domain);
        
        HRESULT ( STDMETHODCALLTYPE *OnException )( 
            IXCLRDataExceptionNotification3 * This,
            /* [in] */ IXCLRDataExceptionState *exception);
        
        HRESULT ( STDMETHODCALLTYPE *OnGcEvent )( 
            IXCLRDataExceptionNotification3 * This,
            /* [in] */ GcEvtArgs gcEvtArgs);
        
        END_INTERFACE
    } IXCLRDataExceptionNotification3Vtbl;

    interface IXCLRDataExceptionNotification3
    {
        CONST_VTBL struct IXCLRDataExceptionNotification3Vtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define IXCLRDataExceptionNotification3_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define IXCLRDataExceptionNotification3_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define IXCLRDataExceptionNotification3_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define IXCLRDataExceptionNotification3_OnCodeGenerated(This,method)	\
    ( (This)->lpVtbl -> OnCodeGenerated(This,method) ) 

#define IXCLRDataExceptionNotification3_OnCodeDiscarded(This,method)	\
    ( (This)->lpVtbl -> OnCodeDiscarded(This,method) ) 

#define IXCLRDataExceptionNotification3_OnProcessExecution(This,state)	\
    ( (This)->lpVtbl -> OnProcessExecution(This,state) ) 

#define IXCLRDataExceptionNotification3_OnTaskExecution(This,task,state)	\
    ( (This)->lpVtbl -> OnTaskExecution(This,task,state) ) 

#define IXCLRDataExceptionNotification3_OnModuleLoaded(This,mod)	\
    ( (This)->lpVtbl -> OnModuleLoaded(This,mod) ) 

#define IXCLRDataExceptionNotification3_OnModuleUnloaded(This,mod)	\
    ( (This)->lpVtbl -> OnModuleUnloaded(This,mod) ) 

#define IXCLRDataExceptionNotification3_OnTypeLoaded(This,typeInst)	\
    ( (This)->lpVtbl -> OnTypeLoaded(This,typeInst) ) 

#define IXCLRDataExceptionNotification3_OnTypeUnloaded(This,typeInst)	\
    ( (This)->lpVtbl -> OnTypeUnloaded(This,typeInst) ) 


#define IXCLRDataExceptionNotification3_OnAppDomainLoaded(This,domain)	\
    ( (This)->lpVtbl -> OnAppDomainLoaded(This,domain) ) 

#define IXCLRDataExceptionNotification3_OnAppDomainUnloaded(This,domain)	\
    ( (This)->lpVtbl -> OnAppDomainUnloaded(This,domain) ) 

#define IXCLRDataExceptionNotification3_OnException(This,exception)	\
    ( (This)->lpVtbl -> OnException(This,exception) ) 


#define IXCLRDataExceptionNotification3_OnGcEvent(This,gcEvtArgs)	\
    ( (This)->lpVtbl -> OnGcEvent(This,gcEvtArgs) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __IXCLRDataExceptionNotification3_INTERFACE_DEFINED__ */


#ifndef __IXCLRDataExceptionNotification4_INTERFACE_DEFINED__
#define __IXCLRDataExceptionNotification4_INTERFACE_DEFINED__

/* interface IXCLRDataExceptionNotification4 */
/* [uuid][local][object] */ 


EXTERN_C const IID IID_IXCLRDataExceptionNotification4;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("C25E926E-5F09-4AA2-BBAD-B7FC7F10CFD7")
    IXCLRDataExceptionNotification4 : public IXCLRDataExceptionNotification3
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE ExceptionCatcherEnter( 
            /* [in] */ IXCLRDataMethodInstance *catchingMethod,
            DWORD catcherNativeOffset) = 0;
        
    };
    
    
#else 	/* C style interface */

    typedef struct IXCLRDataExceptionNotification4Vtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            IXCLRDataExceptionNotification4 * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            IXCLRDataExceptionNotification4 * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            IXCLRDataExceptionNotification4 * This);
        
        HRESULT ( STDMETHODCALLTYPE *OnCodeGenerated )( 
            IXCLRDataExceptionNotification4 * This,
            /* [in] */ IXCLRDataMethodInstance *method);
        
        HRESULT ( STDMETHODCALLTYPE *OnCodeDiscarded )( 
            IXCLRDataExceptionNotification4 * This,
            /* [in] */ IXCLRDataMethodInstance *method);
        
        HRESULT ( STDMETHODCALLTYPE *OnProcessExecution )( 
            IXCLRDataExceptionNotification4 * This,
            /* [in] */ ULONG32 state);
        
        HRESULT ( STDMETHODCALLTYPE *OnTaskExecution )( 
            IXCLRDataExceptionNotification4 * This,
            /* [in] */ IXCLRDataTask *task,
            /* [in] */ ULONG32 state);
        
        HRESULT ( STDMETHODCALLTYPE *OnModuleLoaded )( 
            IXCLRDataExceptionNotification4 * This,
            /* [in] */ IXCLRDataModule *mod);
        
        HRESULT ( STDMETHODCALLTYPE *OnModuleUnloaded )( 
            IXCLRDataExceptionNotification4 * This,
            /* [in] */ IXCLRDataModule *mod);
        
        HRESULT ( STDMETHODCALLTYPE *OnTypeLoaded )( 
            IXCLRDataExceptionNotification4 * This,
            /* [in] */ IXCLRDataTypeInstance *typeInst);
        
        HRESULT ( STDMETHODCALLTYPE *OnTypeUnloaded )( 
            IXCLRDataExceptionNotification4 * This,
            /* [in] */ IXCLRDataTypeInstance *typeInst);
        
        HRESULT ( STDMETHODCALLTYPE *OnAppDomainLoaded )( 
            IXCLRDataExceptionNotification4 * This,
            /* [in] */ IXCLRDataAppDomain *domain);
        
        HRESULT ( STDMETHODCALLTYPE *OnAppDomainUnloaded )( 
            IXCLRDataExceptionNotification4 * This,
            /* [in] */ IXCLRDataAppDomain *domain);
        
        HRESULT ( STDMETHODCALLTYPE *OnException )( 
            IXCLRDataExceptionNotification4 * This,
            /* [in] */ IXCLRDataExceptionState *exception);
        
        HRESULT ( STDMETHODCALLTYPE *OnGcEvent )( 
            IXCLRDataExceptionNotification4 * This,
            /* [in] */ GcEvtArgs gcEvtArgs);
        
        HRESULT ( STDMETHODCALLTYPE *ExceptionCatcherEnter )( 
            IXCLRDataExceptionNotification4 * This,
            /* [in] */ IXCLRDataMethodInstance *catchingMethod,
            DWORD catcherNativeOffset);
        
        END_INTERFACE
    } IXCLRDataExceptionNotification4Vtbl;

    interface IXCLRDataExceptionNotification4
    {
        CONST_VTBL struct IXCLRDataExceptionNotification4Vtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define IXCLRDataExceptionNotification4_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define IXCLRDataExceptionNotification4_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define IXCLRDataExceptionNotification4_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define IXCLRDataExceptionNotification4_OnCodeGenerated(This,method)	\
    ( (This)->lpVtbl -> OnCodeGenerated(This,method) ) 

#define IXCLRDataExceptionNotification4_OnCodeDiscarded(This,method)	\
    ( (This)->lpVtbl -> OnCodeDiscarded(This,method) ) 

#define IXCLRDataExceptionNotification4_OnProcessExecution(This,state)	\
    ( (This)->lpVtbl -> OnProcessExecution(This,state) ) 

#define IXCLRDataExceptionNotification4_OnTaskExecution(This,task,state)	\
    ( (This)->lpVtbl -> OnTaskExecution(This,task,state) ) 

#define IXCLRDataExceptionNotification4_OnModuleLoaded(This,mod)	\
    ( (This)->lpVtbl -> OnModuleLoaded(This,mod) ) 

#define IXCLRDataExceptionNotification4_OnModuleUnloaded(This,mod)	\
    ( (This)->lpVtbl -> OnModuleUnloaded(This,mod) ) 

#define IXCLRDataExceptionNotification4_OnTypeLoaded(This,typeInst)	\
    ( (This)->lpVtbl -> OnTypeLoaded(This,typeInst) ) 

#define IXCLRDataExceptionNotification4_OnTypeUnloaded(This,typeInst)	\
    ( (This)->lpVtbl -> OnTypeUnloaded(This,typeInst) ) 


#define IXCLRDataExceptionNotification4_OnAppDomainLoaded(This,domain)	\
    ( (This)->lpVtbl -> OnAppDomainLoaded(This,domain) ) 

#define IXCLRDataExceptionNotification4_OnAppDomainUnloaded(This,domain)	\
    ( (This)->lpVtbl -> OnAppDomainUnloaded(This,domain) ) 

#define IXCLRDataExceptionNotification4_OnException(This,exception)	\
    ( (This)->lpVtbl -> OnException(This,exception) ) 


#define IXCLRDataExceptionNotification4_OnGcEvent(This,gcEvtArgs)	\
    ( (This)->lpVtbl -> OnGcEvent(This,gcEvtArgs) ) 


#define IXCLRDataExceptionNotification4_ExceptionCatcherEnter(This,catchingMethod,catcherNativeOffset)	\
    ( (This)->lpVtbl -> ExceptionCatcherEnter(This,catchingMethod,catcherNativeOffset) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */


#ifndef __IXCLRDataExceptionNotification5_INTERFACE_DEFINED__
#define __IXCLRDataExceptionNotification5_INTERFACE_DEFINED__

/* interface IXCLRDataExceptionNotification5 */
/* [uuid][local][object] */ 


EXTERN_C const IID IID_IXCLRDataExceptionNotification5;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("e77a39ea-3548-44d9-b171-8569ed1a9423")
    IXCLRDataExceptionNotification5 : public IXCLRDataExceptionNotification4
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE OnCodeGenerated2( 
            /* [in] */ IXCLRDataMethodInstance *method,
            /* [in] */ CLRDATA_ADDRESS nativeCodeLocation) = 0;
        
    };
    
    
#else   /* C style interface */

    typedef struct IXCLRDataExceptionNotification5Vtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            IXCLRDataExceptionNotification5 * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            IXCLRDataExceptionNotification5 * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            IXCLRDataExceptionNotification5 * This);
        
        HRESULT ( STDMETHODCALLTYPE *OnCodeGenerated )( 
            IXCLRDataExceptionNotification5 * This,
            /* [in] */ IXCLRDataMethodInstance *method);
        
        HRESULT ( STDMETHODCALLTYPE *OnCodeDiscarded )( 
            IXCLRDataExceptionNotification5 * This,
            /* [in] */ IXCLRDataMethodInstance *method);
        
        HRESULT ( STDMETHODCALLTYPE *OnProcessExecution )( 
            IXCLRDataExceptionNotification5 * This,
            /* [in] */ ULONG32 state);
        
        HRESULT ( STDMETHODCALLTYPE *OnTaskExecution )( 
            IXCLRDataExceptionNotification5 * This,
            /* [in] */ IXCLRDataTask *task,
            /* [in] */ ULONG32 state);
        
        HRESULT ( STDMETHODCALLTYPE *OnModuleLoaded )( 
            IXCLRDataExceptionNotification5 * This,
            /* [in] */ IXCLRDataModule *mod);
        
        HRESULT ( STDMETHODCALLTYPE *OnModuleUnloaded )( 
            IXCLRDataExceptionNotification5 * This,
            /* [in] */ IXCLRDataModule *mod);
        
        HRESULT ( STDMETHODCALLTYPE *OnTypeLoaded )( 
            IXCLRDataExceptionNotification5 * This,
            /* [in] */ IXCLRDataTypeInstance *typeInst);
        
        HRESULT ( STDMETHODCALLTYPE *OnTypeUnloaded )( 
            IXCLRDataExceptionNotification5 * This,
            /* [in] */ IXCLRDataTypeInstance *typeInst);
        
        HRESULT ( STDMETHODCALLTYPE *OnAppDomainLoaded )( 
            IXCLRDataExceptionNotification5 * This,
            /* [in] */ IXCLRDataAppDomain *domain);
        
        HRESULT ( STDMETHODCALLTYPE *OnAppDomainUnloaded )( 
            IXCLRDataExceptionNotification5 * This,
            /* [in] */ IXCLRDataAppDomain *domain);
        
        HRESULT ( STDMETHODCALLTYPE *OnException )( 
            IXCLRDataExceptionNotification5 * This,
            /* [in] */ IXCLRDataExceptionState *exception);
        
        HRESULT ( STDMETHODCALLTYPE *OnGcEvent )( 
            IXCLRDataExceptionNotification5 * This,
            /* [in] */ GcEvtArgs gcEvtArgs);
        
        HRESULT ( STDMETHODCALLTYPE *ExceptionCatcherEnter )( 
            IXCLRDataExceptionNotification5 * This,
            /* [in] */ IXCLRDataMethodInstance *catchingMethod,
            DWORD catcherNativeOffset);
        
        HRESULT ( STDMETHODCALLTYPE *OnCodeGenerated2 )( 
            IXCLRDataExceptionNotification5 * This,
            /* [in] */ IXCLRDataMethodInstance *method,
            /* [in] */ CLRDATA_ADDRESS nativeCodeLocation);
        
        END_INTERFACE
    } IXCLRDataExceptionNotification5Vtbl;

    interface IXCLRDataExceptionNotification5
    {
        CONST_VTBL struct IXCLRDataExceptionNotification5Vtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define IXCLRDataExceptionNotification5_QueryInterface(This,riid,ppvObject) \
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define IXCLRDataExceptionNotification5_AddRef(This)    \
    ( (This)->lpVtbl -> AddRef(This) ) 

#define IXCLRDataExceptionNotification5_Release(This)   \
    ( (This)->lpVtbl -> Release(This) ) 


#define IXCLRDataExceptionNotification5_OnCodeGenerated(This,method)    \
    ( (This)->lpVtbl -> OnCodeGenerated(This,method) ) 

#define IXCLRDataExceptionNotification5_OnCodeDiscarded(This,method)    \
    ( (This)->lpVtbl -> OnCodeDiscarded(This,method) ) 

#define IXCLRDataExceptionNotification5_OnProcessExecution(This,state)  \
    ( (This)->lpVtbl -> OnProcessExecution(This,state) ) 

#define IXCLRDataExceptionNotification5_OnTaskExecution(This,task,state)    \
    ( (This)->lpVtbl -> OnTaskExecution(This,task,state) ) 

#define IXCLRDataExceptionNotification5_OnModuleLoaded(This,mod)    \
    ( (This)->lpVtbl -> OnModuleLoaded(This,mod) ) 

#define IXCLRDataExceptionNotification5_OnModuleUnloaded(This,mod)  \
    ( (This)->lpVtbl -> OnModuleUnloaded(This,mod) ) 

#define IXCLRDataExceptionNotification5_OnTypeLoaded(This,typeInst) \
    ( (This)->lpVtbl -> OnTypeLoaded(This,typeInst) ) 

#define IXCLRDataExceptionNotification5_OnTypeUnloaded(This,typeInst)   \
    ( (This)->lpVtbl -> OnTypeUnloaded(This,typeInst) ) 


#define IXCLRDataExceptionNotification5_OnAppDomainLoaded(This,domain)  \
    ( (This)->lpVtbl -> OnAppDomainLoaded(This,domain) ) 

#define IXCLRDataExceptionNotification5_OnAppDomainUnloaded(This,domain)    \
    ( (This)->lpVtbl -> OnAppDomainUnloaded(This,domain) ) 

#define IXCLRDataExceptionNotification5_OnException(This,exception) \
    ( (This)->lpVtbl -> OnException(This,exception) ) 


#define IXCLRDataExceptionNotification5_OnGcEvent(This,gcEvtArgs)   \
    ( (This)->lpVtbl -> OnGcEvent(This,gcEvtArgs) ) 


#define IXCLRDataExceptionNotification5_ExceptionCatcherEnter(This,catchingMethod,catcherNativeOffset)  \
    ( (This)->lpVtbl -> ExceptionCatcherEnter(This,catchingMethod,catcherNativeOffset) ) 


#define IXCLRDataExceptionNotification5_OnCodeGenerated2(This,method,nativeCodeLocation)    \
    ( (This)->lpVtbl -> OnCodeGenerated2(This,method,nativeCodeLocation) ) 

#endif /* COBJMACROS */


#endif  /* C style interface */




#endif  /* __IXCLRDataExceptionNotification5_INTERFACE_DEFINED__ */




#endif 	/* __IXCLRDataExceptionNotification4_INTERFACE_DEFINED__ */


/* Additional Prototypes for ALL interfaces */

/* end of Additional Prototypes */

#ifdef __cplusplus
}
#endif

#endif


