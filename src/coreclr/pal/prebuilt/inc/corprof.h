

/* this ALWAYS GENERATED file contains the definitions for the interfaces */


 /* File created by MIDL compiler version 8.01.0622 */
/* Compiler settings for corprof.idl:
    Oicf, W1, Zp8, env=Win64 (32b run), target_arch=AMD64 8.01.0622 
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

#ifndef __corprof_h__
#define __corprof_h__

#if defined(_MSC_VER) && (_MSC_VER >= 1020)
#pragma once
#endif

/* Forward Declarations */ 

#ifndef __ICorProfilerCallback_FWD_DEFINED__
#define __ICorProfilerCallback_FWD_DEFINED__
typedef interface ICorProfilerCallback ICorProfilerCallback;

#endif  /* __ICorProfilerCallback_FWD_DEFINED__ */


#ifndef __ICorProfilerCallback2_FWD_DEFINED__
#define __ICorProfilerCallback2_FWD_DEFINED__
typedef interface ICorProfilerCallback2 ICorProfilerCallback2;

#endif  /* __ICorProfilerCallback2_FWD_DEFINED__ */


#ifndef __ICorProfilerCallback3_FWD_DEFINED__
#define __ICorProfilerCallback3_FWD_DEFINED__
typedef interface ICorProfilerCallback3 ICorProfilerCallback3;

#endif  /* __ICorProfilerCallback3_FWD_DEFINED__ */


#ifndef __ICorProfilerCallback4_FWD_DEFINED__
#define __ICorProfilerCallback4_FWD_DEFINED__
typedef interface ICorProfilerCallback4 ICorProfilerCallback4;

#endif  /* __ICorProfilerCallback4_FWD_DEFINED__ */


#ifndef __ICorProfilerCallback5_FWD_DEFINED__
#define __ICorProfilerCallback5_FWD_DEFINED__
typedef interface ICorProfilerCallback5 ICorProfilerCallback5;

#endif  /* __ICorProfilerCallback5_FWD_DEFINED__ */


#ifndef __ICorProfilerCallback6_FWD_DEFINED__
#define __ICorProfilerCallback6_FWD_DEFINED__
typedef interface ICorProfilerCallback6 ICorProfilerCallback6;

#endif  /* __ICorProfilerCallback6_FWD_DEFINED__ */


#ifndef __ICorProfilerCallback7_FWD_DEFINED__
#define __ICorProfilerCallback7_FWD_DEFINED__
typedef interface ICorProfilerCallback7 ICorProfilerCallback7;

#endif  /* __ICorProfilerCallback7_FWD_DEFINED__ */


#ifndef __ICorProfilerCallback8_FWD_DEFINED__
#define __ICorProfilerCallback8_FWD_DEFINED__
typedef interface ICorProfilerCallback8 ICorProfilerCallback8;

#endif  /* __ICorProfilerCallback8_FWD_DEFINED__ */


#ifndef __ICorProfilerCallback9_FWD_DEFINED__
#define __ICorProfilerCallback9_FWD_DEFINED__
typedef interface ICorProfilerCallback9 ICorProfilerCallback9;

#endif  /* __ICorProfilerCallback9_FWD_DEFINED__ */


#ifndef __ICorProfilerCallback10_FWD_DEFINED__
#define __ICorProfilerCallback10_FWD_DEFINED__
typedef interface ICorProfilerCallback10 ICorProfilerCallback10;

#endif  /* __ICorProfilerCallback10_FWD_DEFINED__ */


#ifndef __ICorProfilerCallback11_FWD_DEFINED__
#define __ICorProfilerCallback11_FWD_DEFINED__
typedef interface ICorProfilerCallback11 ICorProfilerCallback11;

#endif  /* __ICorProfilerCallback11_FWD_DEFINED__ */


#ifndef __ICorProfilerInfo_FWD_DEFINED__
#define __ICorProfilerInfo_FWD_DEFINED__
typedef interface ICorProfilerInfo ICorProfilerInfo;

#endif  /* __ICorProfilerInfo_FWD_DEFINED__ */


#ifndef __ICorProfilerInfo2_FWD_DEFINED__
#define __ICorProfilerInfo2_FWD_DEFINED__
typedef interface ICorProfilerInfo2 ICorProfilerInfo2;

#endif  /* __ICorProfilerInfo2_FWD_DEFINED__ */


#ifndef __ICorProfilerInfo3_FWD_DEFINED__
#define __ICorProfilerInfo3_FWD_DEFINED__
typedef interface ICorProfilerInfo3 ICorProfilerInfo3;

#endif  /* __ICorProfilerInfo3_FWD_DEFINED__ */


#ifndef __ICorProfilerObjectEnum_FWD_DEFINED__
#define __ICorProfilerObjectEnum_FWD_DEFINED__
typedef interface ICorProfilerObjectEnum ICorProfilerObjectEnum;

#endif  /* __ICorProfilerObjectEnum_FWD_DEFINED__ */


#ifndef __ICorProfilerFunctionEnum_FWD_DEFINED__
#define __ICorProfilerFunctionEnum_FWD_DEFINED__
typedef interface ICorProfilerFunctionEnum ICorProfilerFunctionEnum;

#endif  /* __ICorProfilerFunctionEnum_FWD_DEFINED__ */


#ifndef __ICorProfilerModuleEnum_FWD_DEFINED__
#define __ICorProfilerModuleEnum_FWD_DEFINED__
typedef interface ICorProfilerModuleEnum ICorProfilerModuleEnum;

#endif  /* __ICorProfilerModuleEnum_FWD_DEFINED__ */


#ifndef __IMethodMalloc_FWD_DEFINED__
#define __IMethodMalloc_FWD_DEFINED__
typedef interface IMethodMalloc IMethodMalloc;

#endif  /* __IMethodMalloc_FWD_DEFINED__ */


#ifndef __ICorProfilerFunctionControl_FWD_DEFINED__
#define __ICorProfilerFunctionControl_FWD_DEFINED__
typedef interface ICorProfilerFunctionControl ICorProfilerFunctionControl;

#endif  /* __ICorProfilerFunctionControl_FWD_DEFINED__ */


#ifndef __ICorProfilerInfo4_FWD_DEFINED__
#define __ICorProfilerInfo4_FWD_DEFINED__
typedef interface ICorProfilerInfo4 ICorProfilerInfo4;

#endif  /* __ICorProfilerInfo4_FWD_DEFINED__ */


#ifndef __ICorProfilerInfo5_FWD_DEFINED__
#define __ICorProfilerInfo5_FWD_DEFINED__
typedef interface ICorProfilerInfo5 ICorProfilerInfo5;

#endif  /* __ICorProfilerInfo5_FWD_DEFINED__ */


#ifndef __ICorProfilerInfo6_FWD_DEFINED__
#define __ICorProfilerInfo6_FWD_DEFINED__
typedef interface ICorProfilerInfo6 ICorProfilerInfo6;

#endif  /* __ICorProfilerInfo6_FWD_DEFINED__ */


#ifndef __ICorProfilerInfo7_FWD_DEFINED__
#define __ICorProfilerInfo7_FWD_DEFINED__
typedef interface ICorProfilerInfo7 ICorProfilerInfo7;

#endif  /* __ICorProfilerInfo7_FWD_DEFINED__ */


#ifndef __ICorProfilerInfo8_FWD_DEFINED__
#define __ICorProfilerInfo8_FWD_DEFINED__
typedef interface ICorProfilerInfo8 ICorProfilerInfo8;

#endif  /* __ICorProfilerInfo8_FWD_DEFINED__ */


#ifndef __ICorProfilerInfo9_FWD_DEFINED__
#define __ICorProfilerInfo9_FWD_DEFINED__
typedef interface ICorProfilerInfo9 ICorProfilerInfo9;

#endif  /* __ICorProfilerInfo9_FWD_DEFINED__ */


#ifndef __ICorProfilerInfo10_FWD_DEFINED__
#define __ICorProfilerInfo10_FWD_DEFINED__
typedef interface ICorProfilerInfo10 ICorProfilerInfo10;

#endif  /* __ICorProfilerInfo10_FWD_DEFINED__ */


#ifndef __ICorProfilerInfo11_FWD_DEFINED__
#define __ICorProfilerInfo11_FWD_DEFINED__
typedef interface ICorProfilerInfo11 ICorProfilerInfo11;

#endif  /* __ICorProfilerInfo11_FWD_DEFINED__ */


#ifndef __ICorProfilerInfo12_FWD_DEFINED__
#define __ICorProfilerInfo12_FWD_DEFINED__
typedef interface ICorProfilerInfo12 ICorProfilerInfo12;

#endif  /* __ICorProfilerInfo12_FWD_DEFINED__ */


#ifndef __ICorProfilerMethodEnum_FWD_DEFINED__
#define __ICorProfilerMethodEnum_FWD_DEFINED__
typedef interface ICorProfilerMethodEnum ICorProfilerMethodEnum;

#endif  /* __ICorProfilerMethodEnum_FWD_DEFINED__ */


#ifndef __ICorProfilerThreadEnum_FWD_DEFINED__
#define __ICorProfilerThreadEnum_FWD_DEFINED__
typedef interface ICorProfilerThreadEnum ICorProfilerThreadEnum;

#endif  /* __ICorProfilerThreadEnum_FWD_DEFINED__ */


#ifndef __ICorProfilerAssemblyReferenceProvider_FWD_DEFINED__
#define __ICorProfilerAssemblyReferenceProvider_FWD_DEFINED__
typedef interface ICorProfilerAssemblyReferenceProvider ICorProfilerAssemblyReferenceProvider;

#endif  /* __ICorProfilerAssemblyReferenceProvider_FWD_DEFINED__ */


/* header files for imported files */
#include "unknwn.h"

#ifdef __cplusplus
extern "C"{
#endif 


/* interface __MIDL_itf_corprof_0000_0000 */
/* [local] */ 

#define CorDB_CONTROL_Profiling         "Cor_Enable_Profiling"
#define CorDB_CONTROL_ProfilingL       L"Cor_Enable_Profiling"
#if 0
typedef LONG32 mdToken;

typedef mdToken mdModule;

typedef mdToken mdTypeDef;

typedef mdToken mdMethodDef;

typedef mdToken mdFieldDef;

typedef ULONG CorElementType;


typedef /* [public][public][public][public] */ struct __MIDL___MIDL_itf_corprof_0000_0000_0001
    {
    DWORD dwOSPlatformId;
    DWORD dwOSMajorVersion;
    DWORD dwOSMinorVersion;
    }   OSINFO;

typedef /* [public][public][public] */ struct __MIDL___MIDL_itf_corprof_0000_0000_0002
    {
    USHORT usMajorVersion;
    USHORT usMinorVersion;
    USHORT usBuildNumber;
    USHORT usRevisionNumber;
    LPWSTR szLocale;
    ULONG cbLocale;
    DWORD *rProcessor;
    ULONG ulProcessor;
    OSINFO *rOS;
    ULONG ulOS;
    }   ASSEMBLYMETADATA;

#endif
typedef const BYTE *LPCBYTE;

typedef BYTE *LPBYTE;

typedef BYTE COR_SIGNATURE;

typedef COR_SIGNATURE *PCOR_SIGNATURE;

typedef const COR_SIGNATURE *PCCOR_SIGNATURE;

#ifndef _COR_IL_MAP
#define _COR_IL_MAP
typedef struct _COR_IL_MAP
    {
    ULONG32 oldOffset;
    ULONG32 newOffset;
    BOOL fAccurate;
    }   COR_IL_MAP;

#endif //_COR_IL_MAP
#ifndef _COR_DEBUG_IL_TO_NATIVE_MAP_
#define _COR_DEBUG_IL_TO_NATIVE_MAP_
typedef 
enum CorDebugIlToNativeMappingTypes
    {
        NO_MAPPING  = -1,
        PROLOG  = -2,
        EPILOG  = -3
    }   CorDebugIlToNativeMappingTypes;

typedef struct COR_DEBUG_IL_TO_NATIVE_MAP
    {
    ULONG32 ilOffset;
    ULONG32 nativeStartOffset;
    ULONG32 nativeEndOffset;
    }   COR_DEBUG_IL_TO_NATIVE_MAP;

#endif // _COR_DEBUG_IL_TO_NATIVE_MAP_
#ifndef _COR_FIELD_OFFSET_
#define _COR_FIELD_OFFSET_
typedef struct _COR_FIELD_OFFSET
    {
    mdFieldDef ridOfField;
    ULONG ulOffset;
    }   COR_FIELD_OFFSET;

#endif // _COR_FIELD_OFFSET_
typedef UINT_PTR ProcessID;

typedef UINT_PTR AssemblyID;

typedef UINT_PTR AppDomainID;

typedef UINT_PTR ModuleID;

typedef UINT_PTR ClassID;

typedef UINT_PTR ThreadID;

typedef UINT_PTR ContextID;

typedef UINT_PTR FunctionID;

typedef UINT_PTR ObjectID;

typedef UINT_PTR GCHandleID;

typedef UINT_PTR COR_PRF_ELT_INFO;

typedef UINT_PTR ReJITID;

typedef /* [public][public][public][public][public][public][public][public][public][public][public][public][public] */ union __MIDL___MIDL_itf_corprof_0000_0000_0003
    {
    FunctionID functionID;
    UINT_PTR clientID;
    }   FunctionIDOrClientID;

typedef UINT_PTR __stdcall __stdcall FunctionIDMapper( 
    FunctionID funcId,
    BOOL *pbHookFunction);

typedef UINT_PTR __stdcall __stdcall FunctionIDMapper2( 
    FunctionID funcId,
    void *clientData,
    BOOL *pbHookFunction);

typedef 
enum _COR_PRF_SNAPSHOT_INFO
    {
        COR_PRF_SNAPSHOT_DEFAULT    = 0,
        COR_PRF_SNAPSHOT_REGISTER_CONTEXT   = 0x1,
        COR_PRF_SNAPSHOT_X86_OPTIMIZED  = 0x2
    }   COR_PRF_SNAPSHOT_INFO;

typedef UINT_PTR COR_PRF_FRAME_INFO;

typedef struct _COR_PRF_FUNCTION_ARGUMENT_RANGE
    {
    UINT_PTR startAddress;
    ULONG length;
    }   COR_PRF_FUNCTION_ARGUMENT_RANGE;

typedef struct _COR_PRF_FUNCTION_ARGUMENT_INFO
    {
    ULONG numRanges;
    ULONG totalArgumentSize;
    COR_PRF_FUNCTION_ARGUMENT_RANGE ranges[ 1 ];
    }   COR_PRF_FUNCTION_ARGUMENT_INFO;

typedef struct _COR_PRF_CODE_INFO
    {
    UINT_PTR startAddress;
    SIZE_T size;
    }   COR_PRF_CODE_INFO;

typedef /* [public][public] */ 
enum __MIDL___MIDL_itf_corprof_0000_0000_0004
    {
        COR_PRF_FIELD_NOT_A_STATIC  = 0,
        COR_PRF_FIELD_APP_DOMAIN_STATIC = 0x1,
        COR_PRF_FIELD_THREAD_STATIC = 0x2,
        COR_PRF_FIELD_CONTEXT_STATIC    = 0x4,
        COR_PRF_FIELD_RVA_STATIC    = 0x8
    }   COR_PRF_STATIC_TYPE;

typedef struct _COR_PRF_FUNCTION
    {
    FunctionID functionId;
    ReJITID reJitId;
    }   COR_PRF_FUNCTION;

typedef struct _COR_PRF_ASSEMBLY_REFERENCE_INFO
    {
    void *pbPublicKeyOrToken;
    ULONG cbPublicKeyOrToken;
    LPCWSTR szName;
    ASSEMBLYMETADATA *pMetaData;
    void *pbHashValue;
    ULONG cbHashValue;
    DWORD dwAssemblyRefFlags;
    }   COR_PRF_ASSEMBLY_REFERENCE_INFO;

typedef struct _COR_PRF_METHOD
    {
    ModuleID moduleId;
    mdMethodDef methodId;
    }   COR_PRF_METHOD;

typedef void FunctionEnter( 
    FunctionID funcID);

typedef void FunctionLeave( 
    FunctionID funcID);

typedef void FunctionTailcall( 
    FunctionID funcID);

typedef void FunctionEnter2( 
    FunctionID funcId,
    UINT_PTR clientData,
    COR_PRF_FRAME_INFO func,
    COR_PRF_FUNCTION_ARGUMENT_INFO *argumentInfo);

typedef void FunctionLeave2( 
    FunctionID funcId,
    UINT_PTR clientData,
    COR_PRF_FRAME_INFO func,
    COR_PRF_FUNCTION_ARGUMENT_RANGE *retvalRange);

typedef void FunctionTailcall2( 
    FunctionID funcId,
    UINT_PTR clientData,
    COR_PRF_FRAME_INFO func);

typedef void FunctionEnter3( 
    FunctionIDOrClientID functionIDOrClientID);

typedef void FunctionLeave3( 
    FunctionIDOrClientID functionIDOrClientID);

typedef void FunctionTailcall3( 
    FunctionIDOrClientID functionIDOrClientID);

typedef void FunctionEnter3WithInfo( 
    FunctionIDOrClientID functionIDOrClientID,
    COR_PRF_ELT_INFO eltInfo);

typedef void FunctionLeave3WithInfo( 
    FunctionIDOrClientID functionIDOrClientID,
    COR_PRF_ELT_INFO eltInfo);

typedef void FunctionTailcall3WithInfo( 
    FunctionIDOrClientID functionIDOrClientID,
    COR_PRF_ELT_INFO eltInfo);

typedef HRESULT __stdcall __stdcall StackSnapshotCallback( 
    FunctionID funcId,
    UINT_PTR ip,
    COR_PRF_FRAME_INFO frameInfo,
    ULONG32 contextSize,
    BYTE context[  ],
    void *clientData);

typedef BOOL ObjectReferenceCallback( 
    ObjectID root,
    ObjectID *reference,
    void *clientData);

typedef /* [public] */ 
enum __MIDL___MIDL_itf_corprof_0000_0000_0005
    {
        COR_PRF_MONITOR_NONE    = 0,
        COR_PRF_MONITOR_FUNCTION_UNLOADS    = 0x1,
        COR_PRF_MONITOR_CLASS_LOADS = 0x2,
        COR_PRF_MONITOR_MODULE_LOADS    = 0x4,
        COR_PRF_MONITOR_ASSEMBLY_LOADS  = 0x8,
        COR_PRF_MONITOR_APPDOMAIN_LOADS = 0x10,
        COR_PRF_MONITOR_JIT_COMPILATION = 0x20,
        COR_PRF_MONITOR_EXCEPTIONS  = 0x40,
        COR_PRF_MONITOR_GC  = 0x80,
        COR_PRF_MONITOR_OBJECT_ALLOCATED    = 0x100,
        COR_PRF_MONITOR_THREADS = 0x200,
        COR_PRF_MONITOR_REMOTING    = 0x400,
        COR_PRF_MONITOR_CODE_TRANSITIONS    = 0x800,
        COR_PRF_MONITOR_ENTERLEAVE  = 0x1000,
        COR_PRF_MONITOR_CCW = 0x2000,
        COR_PRF_MONITOR_REMOTING_COOKIE = ( 0x4000 | COR_PRF_MONITOR_REMOTING ) ,
        COR_PRF_MONITOR_REMOTING_ASYNC  = ( 0x8000 | COR_PRF_MONITOR_REMOTING ) ,
        COR_PRF_MONITOR_SUSPENDS    = 0x10000,
        COR_PRF_MONITOR_CACHE_SEARCHES  = 0x20000,
        COR_PRF_ENABLE_REJIT    = 0x40000,
        COR_PRF_ENABLE_INPROC_DEBUGGING = 0x80000,
        COR_PRF_ENABLE_JIT_MAPS = 0x100000,
        COR_PRF_DISABLE_INLINING    = 0x200000,
        COR_PRF_DISABLE_OPTIMIZATIONS   = 0x400000,
        COR_PRF_ENABLE_OBJECT_ALLOCATED = 0x800000,
        COR_PRF_MONITOR_CLR_EXCEPTIONS  = 0x1000000,
        COR_PRF_MONITOR_ALL = 0x107ffff,
        COR_PRF_ENABLE_FUNCTION_ARGS    = 0x2000000,
        COR_PRF_ENABLE_FUNCTION_RETVAL  = 0x4000000,
        COR_PRF_ENABLE_FRAME_INFO   = 0x8000000,
        COR_PRF_ENABLE_STACK_SNAPSHOT   = 0x10000000,
        COR_PRF_USE_PROFILE_IMAGES  = 0x20000000,
        COR_PRF_DISABLE_TRANSPARENCY_CHECKS_UNDER_FULL_TRUST    = 0x40000000,
        COR_PRF_DISABLE_ALL_NGEN_IMAGES = 0x80000000,
        COR_PRF_ALL = 0x8fffffff,
        COR_PRF_REQUIRE_PROFILE_IMAGE   = ( ( COR_PRF_USE_PROFILE_IMAGES | COR_PRF_MONITOR_CODE_TRANSITIONS )  | COR_PRF_MONITOR_ENTERLEAVE ) ,
        COR_PRF_ALLOWABLE_AFTER_ATTACH  = ( ( ( ( ( ( ( ( ( ( COR_PRF_MONITOR_THREADS | COR_PRF_MONITOR_MODULE_LOADS )  | COR_PRF_MONITOR_ASSEMBLY_LOADS )  | COR_PRF_MONITOR_APPDOMAIN_LOADS )  | COR_PRF_ENABLE_STACK_SNAPSHOT )  | COR_PRF_MONITOR_GC )  | COR_PRF_MONITOR_SUSPENDS )  | COR_PRF_MONITOR_CLASS_LOADS )  | COR_PRF_MONITOR_EXCEPTIONS )  | COR_PRF_MONITOR_JIT_COMPILATION )  | COR_PRF_ENABLE_REJIT ) ,
        COR_PRF_ALLOWABLE_NOTIFICATION_PROFILER = ( ( ( ( ( ( ( ( ( ( ( ( ( ( ( ( ( ( ( COR_PRF_MONITOR_FUNCTION_UNLOADS | COR_PRF_MONITOR_CLASS_LOADS )  | COR_PRF_MONITOR_MODULE_LOADS )  | COR_PRF_MONITOR_ASSEMBLY_LOADS )  | COR_PRF_MONITOR_APPDOMAIN_LOADS )  | COR_PRF_MONITOR_JIT_COMPILATION )  | COR_PRF_MONITOR_EXCEPTIONS )  | COR_PRF_MONITOR_OBJECT_ALLOCATED )  | COR_PRF_MONITOR_THREADS )  | COR_PRF_MONITOR_CODE_TRANSITIONS )  | COR_PRF_MONITOR_CCW )  | COR_PRF_MONITOR_SUSPENDS )  | COR_PRF_MONITOR_CACHE_SEARCHES )  | COR_PRF_DISABLE_INLINING )  | COR_PRF_DISABLE_OPTIMIZATIONS )  | COR_PRF_ENABLE_OBJECT_ALLOCATED )  | COR_PRF_MONITOR_CLR_EXCEPTIONS )  | COR_PRF_ENABLE_STACK_SNAPSHOT )  | COR_PRF_USE_PROFILE_IMAGES )  | COR_PRF_DISABLE_ALL_NGEN_IMAGES ) ,
        COR_PRF_MONITOR_IMMUTABLE   = ( ( ( ( ( ( ( ( ( ( ( ( ( ( COR_PRF_MONITOR_CODE_TRANSITIONS | COR_PRF_MONITOR_REMOTING )  | COR_PRF_MONITOR_REMOTING_COOKIE )  | COR_PRF_MONITOR_REMOTING_ASYNC )  | COR_PRF_ENABLE_INPROC_DEBUGGING )  | COR_PRF_ENABLE_JIT_MAPS )  | COR_PRF_DISABLE_OPTIMIZATIONS )  | COR_PRF_DISABLE_INLINING )  | COR_PRF_ENABLE_OBJECT_ALLOCATED )  | COR_PRF_ENABLE_FUNCTION_ARGS )  | COR_PRF_ENABLE_FUNCTION_RETVAL )  | COR_PRF_ENABLE_FRAME_INFO )  | COR_PRF_USE_PROFILE_IMAGES )  | COR_PRF_DISABLE_TRANSPARENCY_CHECKS_UNDER_FULL_TRUST )  | COR_PRF_DISABLE_ALL_NGEN_IMAGES ) 
    }   COR_PRF_MONITOR;

typedef /* [public] */ 
enum __MIDL___MIDL_itf_corprof_0000_0000_0006
    {
        COR_PRF_HIGH_MONITOR_NONE   = 0,
        COR_PRF_HIGH_ADD_ASSEMBLY_REFERENCES    = 0x1,
        COR_PRF_HIGH_IN_MEMORY_SYMBOLS_UPDATED  = 0x2,
        COR_PRF_HIGH_MONITOR_DYNAMIC_FUNCTION_UNLOADS   = 0x4,
        COR_PRF_HIGH_DISABLE_TIERED_COMPILATION = 0x8,
        COR_PRF_HIGH_BASIC_GC   = 0x10,
        COR_PRF_HIGH_MONITOR_GC_MOVED_OBJECTS   = 0x20,
        COR_PRF_HIGH_REQUIRE_PROFILE_IMAGE  = 0,
        COR_PRF_HIGH_MONITOR_LARGEOBJECT_ALLOCATED  = 0x40,
        COR_PRF_HIGH_MONITOR_EVENT_PIPE = 0x80,
        COR_PRF_HIGH_ALLOWABLE_AFTER_ATTACH = ( ( ( ( ( COR_PRF_HIGH_IN_MEMORY_SYMBOLS_UPDATED | COR_PRF_HIGH_MONITOR_DYNAMIC_FUNCTION_UNLOADS )  | COR_PRF_HIGH_BASIC_GC )  | COR_PRF_HIGH_MONITOR_GC_MOVED_OBJECTS )  | COR_PRF_HIGH_MONITOR_LARGEOBJECT_ALLOCATED )  | COR_PRF_HIGH_MONITOR_EVENT_PIPE ) ,
        COR_PRF_HIGH_ALLOWABLE_NOTIFICATION_PROFILER    = ( ( ( ( ( ( COR_PRF_HIGH_IN_MEMORY_SYMBOLS_UPDATED | COR_PRF_HIGH_MONITOR_DYNAMIC_FUNCTION_UNLOADS )  | COR_PRF_HIGH_DISABLE_TIERED_COMPILATION )  | COR_PRF_HIGH_BASIC_GC )  | COR_PRF_HIGH_MONITOR_GC_MOVED_OBJECTS )  | COR_PRF_HIGH_MONITOR_LARGEOBJECT_ALLOCATED )  | COR_PRF_HIGH_MONITOR_EVENT_PIPE ) ,
        COR_PRF_HIGH_MONITOR_IMMUTABLE  = COR_PRF_HIGH_DISABLE_TIERED_COMPILATION
    }   COR_PRF_HIGH_MONITOR;

typedef /* [public] */ 
enum __MIDL___MIDL_itf_corprof_0000_0000_0007
    {
        PROFILER_PARENT_UNKNOWN = 0xfffffffd,
        PROFILER_GLOBAL_CLASS   = 0xfffffffe,
        PROFILER_GLOBAL_MODULE  = 0xffffffff
    }   COR_PRF_MISC;

typedef /* [public][public] */ 
enum __MIDL___MIDL_itf_corprof_0000_0000_0008
    {
        COR_PRF_CACHED_FUNCTION_FOUND   = 0,
        COR_PRF_CACHED_FUNCTION_NOT_FOUND   = ( COR_PRF_CACHED_FUNCTION_FOUND + 1 ) 
    }   COR_PRF_JIT_CACHE;

typedef /* [public][public][public] */ 
enum __MIDL___MIDL_itf_corprof_0000_0000_0009
    {
        COR_PRF_TRANSITION_CALL = 0,
        COR_PRF_TRANSITION_RETURN   = ( COR_PRF_TRANSITION_CALL + 1 ) 
    }   COR_PRF_TRANSITION_REASON;

typedef /* [public][public] */ 
enum __MIDL___MIDL_itf_corprof_0000_0000_0010
    {
        COR_PRF_SUSPEND_OTHER   = 0,
        COR_PRF_SUSPEND_FOR_GC  = 1,
        COR_PRF_SUSPEND_FOR_APPDOMAIN_SHUTDOWN  = 2,
        COR_PRF_SUSPEND_FOR_CODE_PITCHING   = 3,
        COR_PRF_SUSPEND_FOR_SHUTDOWN    = 4,
        COR_PRF_SUSPEND_FOR_INPROC_DEBUGGER = 6,
        COR_PRF_SUSPEND_FOR_GC_PREP = 7,
        COR_PRF_SUSPEND_FOR_REJIT   = 8,
        COR_PRF_SUSPEND_FOR_PROFILER    = 9
    }   COR_PRF_SUSPEND_REASON;

typedef /* [public][public] */ 
enum __MIDL___MIDL_itf_corprof_0000_0000_0011
    {
        COR_PRF_DESKTOP_CLR = 0x1,
        COR_PRF_CORE_CLR    = 0x2
    }   COR_PRF_RUNTIME_TYPE;

typedef /* [public] */ 
enum __MIDL___MIDL_itf_corprof_0000_0000_0012
    {
        COR_PRF_REJIT_BLOCK_INLINING    = 0x1,
        COR_PRF_REJIT_INLINING_CALLBACKS    = 0x2
    }   COR_PRF_REJIT_FLAGS;

typedef UINT_PTR EVENTPIPE_PROVIDER;

typedef UINT_PTR EVENTPIPE_EVENT;

typedef UINT64 EVENTPIPE_SESSION;

typedef /* [public] */ 
enum __MIDL___MIDL_itf_corprof_0000_0000_0013
    {
        COR_PRF_EVENTPIPE_OBJECT    = 1,
        COR_PRF_EVENTPIPE_BOOLEAN   = 3,
        COR_PRF_EVENTPIPE_CHAR  = 4,
        COR_PRF_EVENTPIPE_SBYTE = 5,
        COR_PRF_EVENTPIPE_BYTE  = 6,
        COR_PRF_EVENTPIPE_INT16 = 7,
        COR_PRF_EVENTPIPE_UINT16    = 8,
        COR_PRF_EVENTPIPE_INT32 = 9,
        COR_PRF_EVENTPIPE_UINT32    = 10,
        COR_PRF_EVENTPIPE_INT64 = 11,
        COR_PRF_EVENTPIPE_UINT64    = 12,
        COR_PRF_EVENTPIPE_SINGLE    = 13,
        COR_PRF_EVENTPIPE_DOUBLE    = 14,
        COR_PRF_EVENTPIPE_DECIMAL   = 15,
        COR_PRF_EVENTPIPE_DATETIME  = 16,
        COR_PRF_EVENTPIPE_GUID  = 17,
        COR_PRF_EVENTPIPE_STRING    = 18,
        COR_PRF_EVENTPIPE_ARRAY = 19
    }   COR_PRF_EVENTPIPE_PARAM_TYPE;

typedef /* [public] */ 
enum __MIDL___MIDL_itf_corprof_0000_0000_0014
    {
        COR_PRF_EVENTPIPE_LOGALWAYS = 0,
        COR_PRF_EVENTPIPE_CRITICAL  = 1,
        COR_PRF_EVENTPIPE_ERROR = 2,
        COR_PRF_EVENTPIPE_WARNING   = 3,
        COR_PRF_EVENTPIPE_INFORMATIONAL = 4,
        COR_PRF_EVENTPIPE_VERBOSE   = 5
    }   COR_PRF_EVENTPIPE_LEVEL;

typedef /* [public][public][public] */ struct __MIDL___MIDL_itf_corprof_0000_0000_0015
    {
    const WCHAR *providerName;
    UINT64 keywords;
    UINT32 loggingLevel;
    const WCHAR *filterData;
    }   COR_PRF_EVENTPIPE_PROVIDER_CONFIG;

typedef /* [public][public] */ struct __MIDL___MIDL_itf_corprof_0000_0000_0016
    {
    UINT32 type;
    UINT32 elementType;
    const WCHAR *name;
    }   COR_PRF_EVENTPIPE_PARAM_DESC;

typedef /* [public][public] */ struct __MIDL___MIDL_itf_corprof_0000_0000_0017
    {
    UINT64 ptr;
    UINT32 size;
    UINT32 reserved;
    }   COR_PRF_EVENT_DATA;



















extern RPC_IF_HANDLE __MIDL_itf_corprof_0000_0000_v0_0_c_ifspec;
extern RPC_IF_HANDLE __MIDL_itf_corprof_0000_0000_v0_0_s_ifspec;

#ifndef __ICorProfilerCallback_INTERFACE_DEFINED__
#define __ICorProfilerCallback_INTERFACE_DEFINED__

/* interface ICorProfilerCallback */
/* [local][unique][uuid][object] */ 


EXTERN_C const IID IID_ICorProfilerCallback;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("176FBED1-A55C-4796-98CA-A9DA0EF883E7")
    ICorProfilerCallback : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE Initialize( 
            /* [in] */ IUnknown *pICorProfilerInfoUnk) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE Shutdown( void) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE AppDomainCreationStarted( 
            /* [in] */ AppDomainID appDomainId) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE AppDomainCreationFinished( 
            /* [in] */ AppDomainID appDomainId,
            /* [in] */ HRESULT hrStatus) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE AppDomainShutdownStarted( 
            /* [in] */ AppDomainID appDomainId) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE AppDomainShutdownFinished( 
            /* [in] */ AppDomainID appDomainId,
            /* [in] */ HRESULT hrStatus) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE AssemblyLoadStarted( 
            /* [in] */ AssemblyID assemblyId) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE AssemblyLoadFinished( 
            /* [in] */ AssemblyID assemblyId,
            /* [in] */ HRESULT hrStatus) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE AssemblyUnloadStarted( 
            /* [in] */ AssemblyID assemblyId) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE AssemblyUnloadFinished( 
            /* [in] */ AssemblyID assemblyId,
            /* [in] */ HRESULT hrStatus) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE ModuleLoadStarted( 
            /* [in] */ ModuleID moduleId) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE ModuleLoadFinished( 
            /* [in] */ ModuleID moduleId,
            /* [in] */ HRESULT hrStatus) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE ModuleUnloadStarted( 
            /* [in] */ ModuleID moduleId) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE ModuleUnloadFinished( 
            /* [in] */ ModuleID moduleId,
            /* [in] */ HRESULT hrStatus) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE ModuleAttachedToAssembly( 
            /* [in] */ ModuleID moduleId,
            /* [in] */ AssemblyID AssemblyId) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE ClassLoadStarted( 
            /* [in] */ ClassID classId) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE ClassLoadFinished( 
            /* [in] */ ClassID classId,
            /* [in] */ HRESULT hrStatus) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE ClassUnloadStarted( 
            /* [in] */ ClassID classId) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE ClassUnloadFinished( 
            /* [in] */ ClassID classId,
            /* [in] */ HRESULT hrStatus) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE FunctionUnloadStarted( 
            /* [in] */ FunctionID functionId) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE JITCompilationStarted( 
            /* [in] */ FunctionID functionId,
            /* [in] */ BOOL fIsSafeToBlock) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE JITCompilationFinished( 
            /* [in] */ FunctionID functionId,
            /* [in] */ HRESULT hrStatus,
            /* [in] */ BOOL fIsSafeToBlock) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE JITCachedFunctionSearchStarted( 
            /* [in] */ FunctionID functionId,
            /* [out] */ BOOL *pbUseCachedFunction) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE JITCachedFunctionSearchFinished( 
            /* [in] */ FunctionID functionId,
            /* [in] */ COR_PRF_JIT_CACHE result) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE JITFunctionPitched( 
            /* [in] */ FunctionID functionId) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE JITInlining( 
            /* [in] */ FunctionID callerId,
            /* [in] */ FunctionID calleeId,
            /* [out] */ BOOL *pfShouldInline) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE ThreadCreated( 
            /* [in] */ ThreadID threadId) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE ThreadDestroyed( 
            /* [in] */ ThreadID threadId) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE ThreadAssignedToOSThread( 
            /* [in] */ ThreadID managedThreadId,
            /* [in] */ DWORD osThreadId) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE RemotingClientInvocationStarted( void) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE RemotingClientSendingMessage( 
            /* [in] */ GUID *pCookie,
            /* [in] */ BOOL fIsAsync) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE RemotingClientReceivingReply( 
            /* [in] */ GUID *pCookie,
            /* [in] */ BOOL fIsAsync) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE RemotingClientInvocationFinished( void) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE RemotingServerReceivingMessage( 
            /* [in] */ GUID *pCookie,
            /* [in] */ BOOL fIsAsync) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE RemotingServerInvocationStarted( void) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE RemotingServerInvocationReturned( void) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE RemotingServerSendingReply( 
            /* [in] */ GUID *pCookie,
            /* [in] */ BOOL fIsAsync) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE UnmanagedToManagedTransition( 
            /* [in] */ FunctionID functionId,
            /* [in] */ COR_PRF_TRANSITION_REASON reason) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE ManagedToUnmanagedTransition( 
            /* [in] */ FunctionID functionId,
            /* [in] */ COR_PRF_TRANSITION_REASON reason) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE RuntimeSuspendStarted( 
            /* [in] */ COR_PRF_SUSPEND_REASON suspendReason) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE RuntimeSuspendFinished( void) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE RuntimeSuspendAborted( void) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE RuntimeResumeStarted( void) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE RuntimeResumeFinished( void) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE RuntimeThreadSuspended( 
            /* [in] */ ThreadID threadId) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE RuntimeThreadResumed( 
            /* [in] */ ThreadID threadId) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE MovedReferences( 
            /* [in] */ ULONG cMovedObjectIDRanges,
            /* [size_is][in] */ ObjectID oldObjectIDRangeStart[  ],
            /* [size_is][in] */ ObjectID newObjectIDRangeStart[  ],
            /* [size_is][in] */ ULONG cObjectIDRangeLength[  ]) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE ObjectAllocated( 
            /* [in] */ ObjectID objectId,
            /* [in] */ ClassID classId) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE ObjectsAllocatedByClass( 
            /* [in] */ ULONG cClassCount,
            /* [size_is][in] */ ClassID classIds[  ],
            /* [size_is][in] */ ULONG cObjects[  ]) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE ObjectReferences( 
            /* [in] */ ObjectID objectId,
            /* [in] */ ClassID classId,
            /* [in] */ ULONG cObjectRefs,
            /* [size_is][in] */ ObjectID objectRefIds[  ]) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE RootReferences( 
            /* [in] */ ULONG cRootRefs,
            /* [size_is][in] */ ObjectID rootRefIds[  ]) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE ExceptionThrown( 
            /* [in] */ ObjectID thrownObjectId) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE ExceptionSearchFunctionEnter( 
            /* [in] */ FunctionID functionId) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE ExceptionSearchFunctionLeave( void) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE ExceptionSearchFilterEnter( 
            /* [in] */ FunctionID functionId) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE ExceptionSearchFilterLeave( void) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE ExceptionSearchCatcherFound( 
            /* [in] */ FunctionID functionId) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE ExceptionOSHandlerEnter( 
            /* [in] */ UINT_PTR __unused) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE ExceptionOSHandlerLeave( 
            /* [in] */ UINT_PTR __unused) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE ExceptionUnwindFunctionEnter( 
            /* [in] */ FunctionID functionId) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE ExceptionUnwindFunctionLeave( void) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE ExceptionUnwindFinallyEnter( 
            /* [in] */ FunctionID functionId) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE ExceptionUnwindFinallyLeave( void) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE ExceptionCatcherEnter( 
            /* [in] */ FunctionID functionId,
            /* [in] */ ObjectID objectId) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE ExceptionCatcherLeave( void) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE COMClassicVTableCreated( 
            /* [in] */ ClassID wrappedClassId,
            /* [in] */ REFGUID implementedIID,
            /* [in] */ void *pVTable,
            /* [in] */ ULONG cSlots) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE COMClassicVTableDestroyed( 
            /* [in] */ ClassID wrappedClassId,
            /* [in] */ REFGUID implementedIID,
            /* [in] */ void *pVTable) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE ExceptionCLRCatcherFound( void) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE ExceptionCLRCatcherExecute( void) = 0;
        
    };
    
    
#else   /* C style interface */

    typedef struct ICorProfilerCallbackVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            ICorProfilerCallback * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            ICorProfilerCallback * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            ICorProfilerCallback * This);
        
        HRESULT ( STDMETHODCALLTYPE *Initialize )( 
            ICorProfilerCallback * This,
            /* [in] */ IUnknown *pICorProfilerInfoUnk);
        
        HRESULT ( STDMETHODCALLTYPE *Shutdown )( 
            ICorProfilerCallback * This);
        
        HRESULT ( STDMETHODCALLTYPE *AppDomainCreationStarted )( 
            ICorProfilerCallback * This,
            /* [in] */ AppDomainID appDomainId);
        
        HRESULT ( STDMETHODCALLTYPE *AppDomainCreationFinished )( 
            ICorProfilerCallback * This,
            /* [in] */ AppDomainID appDomainId,
            /* [in] */ HRESULT hrStatus);
        
        HRESULT ( STDMETHODCALLTYPE *AppDomainShutdownStarted )( 
            ICorProfilerCallback * This,
            /* [in] */ AppDomainID appDomainId);
        
        HRESULT ( STDMETHODCALLTYPE *AppDomainShutdownFinished )( 
            ICorProfilerCallback * This,
            /* [in] */ AppDomainID appDomainId,
            /* [in] */ HRESULT hrStatus);
        
        HRESULT ( STDMETHODCALLTYPE *AssemblyLoadStarted )( 
            ICorProfilerCallback * This,
            /* [in] */ AssemblyID assemblyId);
        
        HRESULT ( STDMETHODCALLTYPE *AssemblyLoadFinished )( 
            ICorProfilerCallback * This,
            /* [in] */ AssemblyID assemblyId,
            /* [in] */ HRESULT hrStatus);
        
        HRESULT ( STDMETHODCALLTYPE *AssemblyUnloadStarted )( 
            ICorProfilerCallback * This,
            /* [in] */ AssemblyID assemblyId);
        
        HRESULT ( STDMETHODCALLTYPE *AssemblyUnloadFinished )( 
            ICorProfilerCallback * This,
            /* [in] */ AssemblyID assemblyId,
            /* [in] */ HRESULT hrStatus);
        
        HRESULT ( STDMETHODCALLTYPE *ModuleLoadStarted )( 
            ICorProfilerCallback * This,
            /* [in] */ ModuleID moduleId);
        
        HRESULT ( STDMETHODCALLTYPE *ModuleLoadFinished )( 
            ICorProfilerCallback * This,
            /* [in] */ ModuleID moduleId,
            /* [in] */ HRESULT hrStatus);
        
        HRESULT ( STDMETHODCALLTYPE *ModuleUnloadStarted )( 
            ICorProfilerCallback * This,
            /* [in] */ ModuleID moduleId);
        
        HRESULT ( STDMETHODCALLTYPE *ModuleUnloadFinished )( 
            ICorProfilerCallback * This,
            /* [in] */ ModuleID moduleId,
            /* [in] */ HRESULT hrStatus);
        
        HRESULT ( STDMETHODCALLTYPE *ModuleAttachedToAssembly )( 
            ICorProfilerCallback * This,
            /* [in] */ ModuleID moduleId,
            /* [in] */ AssemblyID AssemblyId);
        
        HRESULT ( STDMETHODCALLTYPE *ClassLoadStarted )( 
            ICorProfilerCallback * This,
            /* [in] */ ClassID classId);
        
        HRESULT ( STDMETHODCALLTYPE *ClassLoadFinished )( 
            ICorProfilerCallback * This,
            /* [in] */ ClassID classId,
            /* [in] */ HRESULT hrStatus);
        
        HRESULT ( STDMETHODCALLTYPE *ClassUnloadStarted )( 
            ICorProfilerCallback * This,
            /* [in] */ ClassID classId);
        
        HRESULT ( STDMETHODCALLTYPE *ClassUnloadFinished )( 
            ICorProfilerCallback * This,
            /* [in] */ ClassID classId,
            /* [in] */ HRESULT hrStatus);
        
        HRESULT ( STDMETHODCALLTYPE *FunctionUnloadStarted )( 
            ICorProfilerCallback * This,
            /* [in] */ FunctionID functionId);
        
        HRESULT ( STDMETHODCALLTYPE *JITCompilationStarted )( 
            ICorProfilerCallback * This,
            /* [in] */ FunctionID functionId,
            /* [in] */ BOOL fIsSafeToBlock);
        
        HRESULT ( STDMETHODCALLTYPE *JITCompilationFinished )( 
            ICorProfilerCallback * This,
            /* [in] */ FunctionID functionId,
            /* [in] */ HRESULT hrStatus,
            /* [in] */ BOOL fIsSafeToBlock);
        
        HRESULT ( STDMETHODCALLTYPE *JITCachedFunctionSearchStarted )( 
            ICorProfilerCallback * This,
            /* [in] */ FunctionID functionId,
            /* [out] */ BOOL *pbUseCachedFunction);
        
        HRESULT ( STDMETHODCALLTYPE *JITCachedFunctionSearchFinished )( 
            ICorProfilerCallback * This,
            /* [in] */ FunctionID functionId,
            /* [in] */ COR_PRF_JIT_CACHE result);
        
        HRESULT ( STDMETHODCALLTYPE *JITFunctionPitched )( 
            ICorProfilerCallback * This,
            /* [in] */ FunctionID functionId);
        
        HRESULT ( STDMETHODCALLTYPE *JITInlining )( 
            ICorProfilerCallback * This,
            /* [in] */ FunctionID callerId,
            /* [in] */ FunctionID calleeId,
            /* [out] */ BOOL *pfShouldInline);
        
        HRESULT ( STDMETHODCALLTYPE *ThreadCreated )( 
            ICorProfilerCallback * This,
            /* [in] */ ThreadID threadId);
        
        HRESULT ( STDMETHODCALLTYPE *ThreadDestroyed )( 
            ICorProfilerCallback * This,
            /* [in] */ ThreadID threadId);
        
        HRESULT ( STDMETHODCALLTYPE *ThreadAssignedToOSThread )( 
            ICorProfilerCallback * This,
            /* [in] */ ThreadID managedThreadId,
            /* [in] */ DWORD osThreadId);
        
        HRESULT ( STDMETHODCALLTYPE *RemotingClientInvocationStarted )( 
            ICorProfilerCallback * This);
        
        HRESULT ( STDMETHODCALLTYPE *RemotingClientSendingMessage )( 
            ICorProfilerCallback * This,
            /* [in] */ GUID *pCookie,
            /* [in] */ BOOL fIsAsync);
        
        HRESULT ( STDMETHODCALLTYPE *RemotingClientReceivingReply )( 
            ICorProfilerCallback * This,
            /* [in] */ GUID *pCookie,
            /* [in] */ BOOL fIsAsync);
        
        HRESULT ( STDMETHODCALLTYPE *RemotingClientInvocationFinished )( 
            ICorProfilerCallback * This);
        
        HRESULT ( STDMETHODCALLTYPE *RemotingServerReceivingMessage )( 
            ICorProfilerCallback * This,
            /* [in] */ GUID *pCookie,
            /* [in] */ BOOL fIsAsync);
        
        HRESULT ( STDMETHODCALLTYPE *RemotingServerInvocationStarted )( 
            ICorProfilerCallback * This);
        
        HRESULT ( STDMETHODCALLTYPE *RemotingServerInvocationReturned )( 
            ICorProfilerCallback * This);
        
        HRESULT ( STDMETHODCALLTYPE *RemotingServerSendingReply )( 
            ICorProfilerCallback * This,
            /* [in] */ GUID *pCookie,
            /* [in] */ BOOL fIsAsync);
        
        HRESULT ( STDMETHODCALLTYPE *UnmanagedToManagedTransition )( 
            ICorProfilerCallback * This,
            /* [in] */ FunctionID functionId,
            /* [in] */ COR_PRF_TRANSITION_REASON reason);
        
        HRESULT ( STDMETHODCALLTYPE *ManagedToUnmanagedTransition )( 
            ICorProfilerCallback * This,
            /* [in] */ FunctionID functionId,
            /* [in] */ COR_PRF_TRANSITION_REASON reason);
        
        HRESULT ( STDMETHODCALLTYPE *RuntimeSuspendStarted )( 
            ICorProfilerCallback * This,
            /* [in] */ COR_PRF_SUSPEND_REASON suspendReason);
        
        HRESULT ( STDMETHODCALLTYPE *RuntimeSuspendFinished )( 
            ICorProfilerCallback * This);
        
        HRESULT ( STDMETHODCALLTYPE *RuntimeSuspendAborted )( 
            ICorProfilerCallback * This);
        
        HRESULT ( STDMETHODCALLTYPE *RuntimeResumeStarted )( 
            ICorProfilerCallback * This);
        
        HRESULT ( STDMETHODCALLTYPE *RuntimeResumeFinished )( 
            ICorProfilerCallback * This);
        
        HRESULT ( STDMETHODCALLTYPE *RuntimeThreadSuspended )( 
            ICorProfilerCallback * This,
            /* [in] */ ThreadID threadId);
        
        HRESULT ( STDMETHODCALLTYPE *RuntimeThreadResumed )( 
            ICorProfilerCallback * This,
            /* [in] */ ThreadID threadId);
        
        HRESULT ( STDMETHODCALLTYPE *MovedReferences )( 
            ICorProfilerCallback * This,
            /* [in] */ ULONG cMovedObjectIDRanges,
            /* [size_is][in] */ ObjectID oldObjectIDRangeStart[  ],
            /* [size_is][in] */ ObjectID newObjectIDRangeStart[  ],
            /* [size_is][in] */ ULONG cObjectIDRangeLength[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *ObjectAllocated )( 
            ICorProfilerCallback * This,
            /* [in] */ ObjectID objectId,
            /* [in] */ ClassID classId);
        
        HRESULT ( STDMETHODCALLTYPE *ObjectsAllocatedByClass )( 
            ICorProfilerCallback * This,
            /* [in] */ ULONG cClassCount,
            /* [size_is][in] */ ClassID classIds[  ],
            /* [size_is][in] */ ULONG cObjects[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *ObjectReferences )( 
            ICorProfilerCallback * This,
            /* [in] */ ObjectID objectId,
            /* [in] */ ClassID classId,
            /* [in] */ ULONG cObjectRefs,
            /* [size_is][in] */ ObjectID objectRefIds[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *RootReferences )( 
            ICorProfilerCallback * This,
            /* [in] */ ULONG cRootRefs,
            /* [size_is][in] */ ObjectID rootRefIds[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *ExceptionThrown )( 
            ICorProfilerCallback * This,
            /* [in] */ ObjectID thrownObjectId);
        
        HRESULT ( STDMETHODCALLTYPE *ExceptionSearchFunctionEnter )( 
            ICorProfilerCallback * This,
            /* [in] */ FunctionID functionId);
        
        HRESULT ( STDMETHODCALLTYPE *ExceptionSearchFunctionLeave )( 
            ICorProfilerCallback * This);
        
        HRESULT ( STDMETHODCALLTYPE *ExceptionSearchFilterEnter )( 
            ICorProfilerCallback * This,
            /* [in] */ FunctionID functionId);
        
        HRESULT ( STDMETHODCALLTYPE *ExceptionSearchFilterLeave )( 
            ICorProfilerCallback * This);
        
        HRESULT ( STDMETHODCALLTYPE *ExceptionSearchCatcherFound )( 
            ICorProfilerCallback * This,
            /* [in] */ FunctionID functionId);
        
        HRESULT ( STDMETHODCALLTYPE *ExceptionOSHandlerEnter )( 
            ICorProfilerCallback * This,
            /* [in] */ UINT_PTR __unused);
        
        HRESULT ( STDMETHODCALLTYPE *ExceptionOSHandlerLeave )( 
            ICorProfilerCallback * This,
            /* [in] */ UINT_PTR __unused);
        
        HRESULT ( STDMETHODCALLTYPE *ExceptionUnwindFunctionEnter )( 
            ICorProfilerCallback * This,
            /* [in] */ FunctionID functionId);
        
        HRESULT ( STDMETHODCALLTYPE *ExceptionUnwindFunctionLeave )( 
            ICorProfilerCallback * This);
        
        HRESULT ( STDMETHODCALLTYPE *ExceptionUnwindFinallyEnter )( 
            ICorProfilerCallback * This,
            /* [in] */ FunctionID functionId);
        
        HRESULT ( STDMETHODCALLTYPE *ExceptionUnwindFinallyLeave )( 
            ICorProfilerCallback * This);
        
        HRESULT ( STDMETHODCALLTYPE *ExceptionCatcherEnter )( 
            ICorProfilerCallback * This,
            /* [in] */ FunctionID functionId,
            /* [in] */ ObjectID objectId);
        
        HRESULT ( STDMETHODCALLTYPE *ExceptionCatcherLeave )( 
            ICorProfilerCallback * This);
        
        HRESULT ( STDMETHODCALLTYPE *COMClassicVTableCreated )( 
            ICorProfilerCallback * This,
            /* [in] */ ClassID wrappedClassId,
            /* [in] */ REFGUID implementedIID,
            /* [in] */ void *pVTable,
            /* [in] */ ULONG cSlots);
        
        HRESULT ( STDMETHODCALLTYPE *COMClassicVTableDestroyed )( 
            ICorProfilerCallback * This,
            /* [in] */ ClassID wrappedClassId,
            /* [in] */ REFGUID implementedIID,
            /* [in] */ void *pVTable);
        
        HRESULT ( STDMETHODCALLTYPE *ExceptionCLRCatcherFound )( 
            ICorProfilerCallback * This);
        
        HRESULT ( STDMETHODCALLTYPE *ExceptionCLRCatcherExecute )( 
            ICorProfilerCallback * This);
        
        END_INTERFACE
    } ICorProfilerCallbackVtbl;

    interface ICorProfilerCallback
    {
        CONST_VTBL struct ICorProfilerCallbackVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define ICorProfilerCallback_QueryInterface(This,riid,ppvObject)    \
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define ICorProfilerCallback_AddRef(This)   \
    ( (This)->lpVtbl -> AddRef(This) ) 

#define ICorProfilerCallback_Release(This)  \
    ( (This)->lpVtbl -> Release(This) ) 


#define ICorProfilerCallback_Initialize(This,pICorProfilerInfoUnk)  \
    ( (This)->lpVtbl -> Initialize(This,pICorProfilerInfoUnk) ) 

#define ICorProfilerCallback_Shutdown(This) \
    ( (This)->lpVtbl -> Shutdown(This) ) 

#define ICorProfilerCallback_AppDomainCreationStarted(This,appDomainId) \
    ( (This)->lpVtbl -> AppDomainCreationStarted(This,appDomainId) ) 

#define ICorProfilerCallback_AppDomainCreationFinished(This,appDomainId,hrStatus)   \
    ( (This)->lpVtbl -> AppDomainCreationFinished(This,appDomainId,hrStatus) ) 

#define ICorProfilerCallback_AppDomainShutdownStarted(This,appDomainId) \
    ( (This)->lpVtbl -> AppDomainShutdownStarted(This,appDomainId) ) 

#define ICorProfilerCallback_AppDomainShutdownFinished(This,appDomainId,hrStatus)   \
    ( (This)->lpVtbl -> AppDomainShutdownFinished(This,appDomainId,hrStatus) ) 

#define ICorProfilerCallback_AssemblyLoadStarted(This,assemblyId)   \
    ( (This)->lpVtbl -> AssemblyLoadStarted(This,assemblyId) ) 

#define ICorProfilerCallback_AssemblyLoadFinished(This,assemblyId,hrStatus) \
    ( (This)->lpVtbl -> AssemblyLoadFinished(This,assemblyId,hrStatus) ) 

#define ICorProfilerCallback_AssemblyUnloadStarted(This,assemblyId) \
    ( (This)->lpVtbl -> AssemblyUnloadStarted(This,assemblyId) ) 

#define ICorProfilerCallback_AssemblyUnloadFinished(This,assemblyId,hrStatus)   \
    ( (This)->lpVtbl -> AssemblyUnloadFinished(This,assemblyId,hrStatus) ) 

#define ICorProfilerCallback_ModuleLoadStarted(This,moduleId)   \
    ( (This)->lpVtbl -> ModuleLoadStarted(This,moduleId) ) 

#define ICorProfilerCallback_ModuleLoadFinished(This,moduleId,hrStatus) \
    ( (This)->lpVtbl -> ModuleLoadFinished(This,moduleId,hrStatus) ) 

#define ICorProfilerCallback_ModuleUnloadStarted(This,moduleId) \
    ( (This)->lpVtbl -> ModuleUnloadStarted(This,moduleId) ) 

#define ICorProfilerCallback_ModuleUnloadFinished(This,moduleId,hrStatus)   \
    ( (This)->lpVtbl -> ModuleUnloadFinished(This,moduleId,hrStatus) ) 

#define ICorProfilerCallback_ModuleAttachedToAssembly(This,moduleId,AssemblyId) \
    ( (This)->lpVtbl -> ModuleAttachedToAssembly(This,moduleId,AssemblyId) ) 

#define ICorProfilerCallback_ClassLoadStarted(This,classId) \
    ( (This)->lpVtbl -> ClassLoadStarted(This,classId) ) 

#define ICorProfilerCallback_ClassLoadFinished(This,classId,hrStatus)   \
    ( (This)->lpVtbl -> ClassLoadFinished(This,classId,hrStatus) ) 

#define ICorProfilerCallback_ClassUnloadStarted(This,classId)   \
    ( (This)->lpVtbl -> ClassUnloadStarted(This,classId) ) 

#define ICorProfilerCallback_ClassUnloadFinished(This,classId,hrStatus) \
    ( (This)->lpVtbl -> ClassUnloadFinished(This,classId,hrStatus) ) 

#define ICorProfilerCallback_FunctionUnloadStarted(This,functionId) \
    ( (This)->lpVtbl -> FunctionUnloadStarted(This,functionId) ) 

#define ICorProfilerCallback_JITCompilationStarted(This,functionId,fIsSafeToBlock)  \
    ( (This)->lpVtbl -> JITCompilationStarted(This,functionId,fIsSafeToBlock) ) 

#define ICorProfilerCallback_JITCompilationFinished(This,functionId,hrStatus,fIsSafeToBlock)    \
    ( (This)->lpVtbl -> JITCompilationFinished(This,functionId,hrStatus,fIsSafeToBlock) ) 

#define ICorProfilerCallback_JITCachedFunctionSearchStarted(This,functionId,pbUseCachedFunction)    \
    ( (This)->lpVtbl -> JITCachedFunctionSearchStarted(This,functionId,pbUseCachedFunction) ) 

#define ICorProfilerCallback_JITCachedFunctionSearchFinished(This,functionId,result)    \
    ( (This)->lpVtbl -> JITCachedFunctionSearchFinished(This,functionId,result) ) 

#define ICorProfilerCallback_JITFunctionPitched(This,functionId)    \
    ( (This)->lpVtbl -> JITFunctionPitched(This,functionId) ) 

#define ICorProfilerCallback_JITInlining(This,callerId,calleeId,pfShouldInline) \
    ( (This)->lpVtbl -> JITInlining(This,callerId,calleeId,pfShouldInline) ) 

#define ICorProfilerCallback_ThreadCreated(This,threadId)   \
    ( (This)->lpVtbl -> ThreadCreated(This,threadId) ) 

#define ICorProfilerCallback_ThreadDestroyed(This,threadId) \
    ( (This)->lpVtbl -> ThreadDestroyed(This,threadId) ) 

#define ICorProfilerCallback_ThreadAssignedToOSThread(This,managedThreadId,osThreadId)  \
    ( (This)->lpVtbl -> ThreadAssignedToOSThread(This,managedThreadId,osThreadId) ) 

#define ICorProfilerCallback_RemotingClientInvocationStarted(This)  \
    ( (This)->lpVtbl -> RemotingClientInvocationStarted(This) ) 

#define ICorProfilerCallback_RemotingClientSendingMessage(This,pCookie,fIsAsync)    \
    ( (This)->lpVtbl -> RemotingClientSendingMessage(This,pCookie,fIsAsync) ) 

#define ICorProfilerCallback_RemotingClientReceivingReply(This,pCookie,fIsAsync)    \
    ( (This)->lpVtbl -> RemotingClientReceivingReply(This,pCookie,fIsAsync) ) 

#define ICorProfilerCallback_RemotingClientInvocationFinished(This) \
    ( (This)->lpVtbl -> RemotingClientInvocationFinished(This) ) 

#define ICorProfilerCallback_RemotingServerReceivingMessage(This,pCookie,fIsAsync)  \
    ( (This)->lpVtbl -> RemotingServerReceivingMessage(This,pCookie,fIsAsync) ) 

#define ICorProfilerCallback_RemotingServerInvocationStarted(This)  \
    ( (This)->lpVtbl -> RemotingServerInvocationStarted(This) ) 

#define ICorProfilerCallback_RemotingServerInvocationReturned(This) \
    ( (This)->lpVtbl -> RemotingServerInvocationReturned(This) ) 

#define ICorProfilerCallback_RemotingServerSendingReply(This,pCookie,fIsAsync)  \
    ( (This)->lpVtbl -> RemotingServerSendingReply(This,pCookie,fIsAsync) ) 

#define ICorProfilerCallback_UnmanagedToManagedTransition(This,functionId,reason)   \
    ( (This)->lpVtbl -> UnmanagedToManagedTransition(This,functionId,reason) ) 

#define ICorProfilerCallback_ManagedToUnmanagedTransition(This,functionId,reason)   \
    ( (This)->lpVtbl -> ManagedToUnmanagedTransition(This,functionId,reason) ) 

#define ICorProfilerCallback_RuntimeSuspendStarted(This,suspendReason)  \
    ( (This)->lpVtbl -> RuntimeSuspendStarted(This,suspendReason) ) 

#define ICorProfilerCallback_RuntimeSuspendFinished(This)   \
    ( (This)->lpVtbl -> RuntimeSuspendFinished(This) ) 

#define ICorProfilerCallback_RuntimeSuspendAborted(This)    \
    ( (This)->lpVtbl -> RuntimeSuspendAborted(This) ) 

#define ICorProfilerCallback_RuntimeResumeStarted(This) \
    ( (This)->lpVtbl -> RuntimeResumeStarted(This) ) 

#define ICorProfilerCallback_RuntimeResumeFinished(This)    \
    ( (This)->lpVtbl -> RuntimeResumeFinished(This) ) 

#define ICorProfilerCallback_RuntimeThreadSuspended(This,threadId)  \
    ( (This)->lpVtbl -> RuntimeThreadSuspended(This,threadId) ) 

#define ICorProfilerCallback_RuntimeThreadResumed(This,threadId)    \
    ( (This)->lpVtbl -> RuntimeThreadResumed(This,threadId) ) 

#define ICorProfilerCallback_MovedReferences(This,cMovedObjectIDRanges,oldObjectIDRangeStart,newObjectIDRangeStart,cObjectIDRangeLength)    \
    ( (This)->lpVtbl -> MovedReferences(This,cMovedObjectIDRanges,oldObjectIDRangeStart,newObjectIDRangeStart,cObjectIDRangeLength) ) 

#define ICorProfilerCallback_ObjectAllocated(This,objectId,classId) \
    ( (This)->lpVtbl -> ObjectAllocated(This,objectId,classId) ) 

#define ICorProfilerCallback_ObjectsAllocatedByClass(This,cClassCount,classIds,cObjects)    \
    ( (This)->lpVtbl -> ObjectsAllocatedByClass(This,cClassCount,classIds,cObjects) ) 

#define ICorProfilerCallback_ObjectReferences(This,objectId,classId,cObjectRefs,objectRefIds)   \
    ( (This)->lpVtbl -> ObjectReferences(This,objectId,classId,cObjectRefs,objectRefIds) ) 

#define ICorProfilerCallback_RootReferences(This,cRootRefs,rootRefIds)  \
    ( (This)->lpVtbl -> RootReferences(This,cRootRefs,rootRefIds) ) 

#define ICorProfilerCallback_ExceptionThrown(This,thrownObjectId)   \
    ( (This)->lpVtbl -> ExceptionThrown(This,thrownObjectId) ) 

#define ICorProfilerCallback_ExceptionSearchFunctionEnter(This,functionId)  \
    ( (This)->lpVtbl -> ExceptionSearchFunctionEnter(This,functionId) ) 

#define ICorProfilerCallback_ExceptionSearchFunctionLeave(This) \
    ( (This)->lpVtbl -> ExceptionSearchFunctionLeave(This) ) 

#define ICorProfilerCallback_ExceptionSearchFilterEnter(This,functionId)    \
    ( (This)->lpVtbl -> ExceptionSearchFilterEnter(This,functionId) ) 

#define ICorProfilerCallback_ExceptionSearchFilterLeave(This)   \
    ( (This)->lpVtbl -> ExceptionSearchFilterLeave(This) ) 

#define ICorProfilerCallback_ExceptionSearchCatcherFound(This,functionId)   \
    ( (This)->lpVtbl -> ExceptionSearchCatcherFound(This,functionId) ) 

#define ICorProfilerCallback_ExceptionOSHandlerEnter(This,__unused) \
    ( (This)->lpVtbl -> ExceptionOSHandlerEnter(This,__unused) ) 

#define ICorProfilerCallback_ExceptionOSHandlerLeave(This,__unused) \
    ( (This)->lpVtbl -> ExceptionOSHandlerLeave(This,__unused) ) 

#define ICorProfilerCallback_ExceptionUnwindFunctionEnter(This,functionId)  \
    ( (This)->lpVtbl -> ExceptionUnwindFunctionEnter(This,functionId) ) 

#define ICorProfilerCallback_ExceptionUnwindFunctionLeave(This) \
    ( (This)->lpVtbl -> ExceptionUnwindFunctionLeave(This) ) 

#define ICorProfilerCallback_ExceptionUnwindFinallyEnter(This,functionId)   \
    ( (This)->lpVtbl -> ExceptionUnwindFinallyEnter(This,functionId) ) 

#define ICorProfilerCallback_ExceptionUnwindFinallyLeave(This)  \
    ( (This)->lpVtbl -> ExceptionUnwindFinallyLeave(This) ) 

#define ICorProfilerCallback_ExceptionCatcherEnter(This,functionId,objectId)    \
    ( (This)->lpVtbl -> ExceptionCatcherEnter(This,functionId,objectId) ) 

#define ICorProfilerCallback_ExceptionCatcherLeave(This)    \
    ( (This)->lpVtbl -> ExceptionCatcherLeave(This) ) 

#define ICorProfilerCallback_COMClassicVTableCreated(This,wrappedClassId,implementedIID,pVTable,cSlots) \
    ( (This)->lpVtbl -> COMClassicVTableCreated(This,wrappedClassId,implementedIID,pVTable,cSlots) ) 

#define ICorProfilerCallback_COMClassicVTableDestroyed(This,wrappedClassId,implementedIID,pVTable)  \
    ( (This)->lpVtbl -> COMClassicVTableDestroyed(This,wrappedClassId,implementedIID,pVTable) ) 

#define ICorProfilerCallback_ExceptionCLRCatcherFound(This) \
    ( (This)->lpVtbl -> ExceptionCLRCatcherFound(This) ) 

#define ICorProfilerCallback_ExceptionCLRCatcherExecute(This)   \
    ( (This)->lpVtbl -> ExceptionCLRCatcherExecute(This) ) 

#endif /* COBJMACROS */


#endif  /* C style interface */




#endif  /* __ICorProfilerCallback_INTERFACE_DEFINED__ */


/* interface __MIDL_itf_corprof_0000_0001 */
/* [local] */ 

typedef /* [public][public] */ 
enum __MIDL___MIDL_itf_corprof_0000_0001_0001
    {
        COR_PRF_GC_ROOT_STACK   = 1,
        COR_PRF_GC_ROOT_FINALIZER   = 2,
        COR_PRF_GC_ROOT_HANDLE  = 3,
        COR_PRF_GC_ROOT_OTHER   = 0
    }   COR_PRF_GC_ROOT_KIND;

typedef /* [public][public] */ 
enum __MIDL___MIDL_itf_corprof_0000_0001_0002
    {
        COR_PRF_GC_ROOT_PINNING = 0x1,
        COR_PRF_GC_ROOT_WEAKREF = 0x2,
        COR_PRF_GC_ROOT_INTERIOR    = 0x4,
        COR_PRF_GC_ROOT_REFCOUNTED  = 0x8
    }   COR_PRF_GC_ROOT_FLAGS;

typedef /* [public] */ 
enum __MIDL___MIDL_itf_corprof_0000_0001_0003
    {
        COR_PRF_FINALIZER_CRITICAL  = 0x1
    }   COR_PRF_FINALIZER_FLAGS;

typedef /* [public][public][public][public] */ 
enum __MIDL___MIDL_itf_corprof_0000_0001_0004
    {
        COR_PRF_GC_GEN_0    = 0,
        COR_PRF_GC_GEN_1    = 1,
        COR_PRF_GC_GEN_2    = 2,
        COR_PRF_GC_LARGE_OBJECT_HEAP    = 3,
        COR_PRF_GC_PINNED_OBJECT_HEAP   = 4
    }   COR_PRF_GC_GENERATION;

typedef struct COR_PRF_GC_GENERATION_RANGE
    {
    COR_PRF_GC_GENERATION generation;
    ObjectID rangeStart;
    UINT_PTR rangeLength;
    UINT_PTR rangeLengthReserved;
    }   COR_PRF_GC_GENERATION_RANGE;

typedef /* [public][public][public] */ 
enum __MIDL___MIDL_itf_corprof_0000_0001_0005
    {
        COR_PRF_CLAUSE_NONE = 0,
        COR_PRF_CLAUSE_FILTER   = 1,
        COR_PRF_CLAUSE_CATCH    = 2,
        COR_PRF_CLAUSE_FINALLY  = 3
    }   COR_PRF_CLAUSE_TYPE;

typedef struct COR_PRF_EX_CLAUSE_INFO
    {
    COR_PRF_CLAUSE_TYPE clauseType;
    UINT_PTR programCounter;
    UINT_PTR framePointer;
    UINT_PTR shadowStackPointer;
    }   COR_PRF_EX_CLAUSE_INFO;

typedef /* [public][public] */ 
enum __MIDL___MIDL_itf_corprof_0000_0001_0006
    {
        COR_PRF_GC_INDUCED  = 1,
        COR_PRF_GC_OTHER    = 0
    }   COR_PRF_GC_REASON;

typedef /* [public] */ 
enum __MIDL___MIDL_itf_corprof_0000_0001_0007
    {
        COR_PRF_MODULE_DISK = 0x1,
        COR_PRF_MODULE_NGEN = 0x2,
        COR_PRF_MODULE_DYNAMIC  = 0x4,
        COR_PRF_MODULE_COLLECTIBLE  = 0x8,
        COR_PRF_MODULE_RESOURCE = 0x10,
        COR_PRF_MODULE_FLAT_LAYOUT  = 0x20,
        COR_PRF_MODULE_WINDOWS_RUNTIME  = 0x40
    }   COR_PRF_MODULE_FLAGS;



extern RPC_IF_HANDLE __MIDL_itf_corprof_0000_0001_v0_0_c_ifspec;
extern RPC_IF_HANDLE __MIDL_itf_corprof_0000_0001_v0_0_s_ifspec;

#ifndef __ICorProfilerCallback2_INTERFACE_DEFINED__
#define __ICorProfilerCallback2_INTERFACE_DEFINED__

/* interface ICorProfilerCallback2 */
/* [local][unique][uuid][object] */ 


EXTERN_C const IID IID_ICorProfilerCallback2;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("8A8CC829-CCF2-49fe-BBAE-0F022228071A")
    ICorProfilerCallback2 : public ICorProfilerCallback
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE ThreadNameChanged( 
            /* [in] */ ThreadID threadId,
            /* [in] */ ULONG cchName,
            /* [annotation][in] */ 
            _In_reads_opt_(cchName)  WCHAR name[  ]) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GarbageCollectionStarted( 
            /* [in] */ int cGenerations,
            /* [size_is][in] */ BOOL generationCollected[  ],
            /* [in] */ COR_PRF_GC_REASON reason) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE SurvivingReferences( 
            /* [in] */ ULONG cSurvivingObjectIDRanges,
            /* [size_is][in] */ ObjectID objectIDRangeStart[  ],
            /* [size_is][in] */ ULONG cObjectIDRangeLength[  ]) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GarbageCollectionFinished( void) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE FinalizeableObjectQueued( 
            /* [in] */ DWORD finalizerFlags,
            /* [in] */ ObjectID objectID) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE RootReferences2( 
            /* [in] */ ULONG cRootRefs,
            /* [size_is][in] */ ObjectID rootRefIds[  ],
            /* [size_is][in] */ COR_PRF_GC_ROOT_KIND rootKinds[  ],
            /* [size_is][in] */ COR_PRF_GC_ROOT_FLAGS rootFlags[  ],
            /* [size_is][in] */ UINT_PTR rootIds[  ]) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE HandleCreated( 
            /* [in] */ GCHandleID handleId,
            /* [in] */ ObjectID initialObjectId) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE HandleDestroyed( 
            /* [in] */ GCHandleID handleId) = 0;
        
    };
    
    
#else   /* C style interface */

    typedef struct ICorProfilerCallback2Vtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            ICorProfilerCallback2 * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            ICorProfilerCallback2 * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            ICorProfilerCallback2 * This);
        
        HRESULT ( STDMETHODCALLTYPE *Initialize )( 
            ICorProfilerCallback2 * This,
            /* [in] */ IUnknown *pICorProfilerInfoUnk);
        
        HRESULT ( STDMETHODCALLTYPE *Shutdown )( 
            ICorProfilerCallback2 * This);
        
        HRESULT ( STDMETHODCALLTYPE *AppDomainCreationStarted )( 
            ICorProfilerCallback2 * This,
            /* [in] */ AppDomainID appDomainId);
        
        HRESULT ( STDMETHODCALLTYPE *AppDomainCreationFinished )( 
            ICorProfilerCallback2 * This,
            /* [in] */ AppDomainID appDomainId,
            /* [in] */ HRESULT hrStatus);
        
        HRESULT ( STDMETHODCALLTYPE *AppDomainShutdownStarted )( 
            ICorProfilerCallback2 * This,
            /* [in] */ AppDomainID appDomainId);
        
        HRESULT ( STDMETHODCALLTYPE *AppDomainShutdownFinished )( 
            ICorProfilerCallback2 * This,
            /* [in] */ AppDomainID appDomainId,
            /* [in] */ HRESULT hrStatus);
        
        HRESULT ( STDMETHODCALLTYPE *AssemblyLoadStarted )( 
            ICorProfilerCallback2 * This,
            /* [in] */ AssemblyID assemblyId);
        
        HRESULT ( STDMETHODCALLTYPE *AssemblyLoadFinished )( 
            ICorProfilerCallback2 * This,
            /* [in] */ AssemblyID assemblyId,
            /* [in] */ HRESULT hrStatus);
        
        HRESULT ( STDMETHODCALLTYPE *AssemblyUnloadStarted )( 
            ICorProfilerCallback2 * This,
            /* [in] */ AssemblyID assemblyId);
        
        HRESULT ( STDMETHODCALLTYPE *AssemblyUnloadFinished )( 
            ICorProfilerCallback2 * This,
            /* [in] */ AssemblyID assemblyId,
            /* [in] */ HRESULT hrStatus);
        
        HRESULT ( STDMETHODCALLTYPE *ModuleLoadStarted )( 
            ICorProfilerCallback2 * This,
            /* [in] */ ModuleID moduleId);
        
        HRESULT ( STDMETHODCALLTYPE *ModuleLoadFinished )( 
            ICorProfilerCallback2 * This,
            /* [in] */ ModuleID moduleId,
            /* [in] */ HRESULT hrStatus);
        
        HRESULT ( STDMETHODCALLTYPE *ModuleUnloadStarted )( 
            ICorProfilerCallback2 * This,
            /* [in] */ ModuleID moduleId);
        
        HRESULT ( STDMETHODCALLTYPE *ModuleUnloadFinished )( 
            ICorProfilerCallback2 * This,
            /* [in] */ ModuleID moduleId,
            /* [in] */ HRESULT hrStatus);
        
        HRESULT ( STDMETHODCALLTYPE *ModuleAttachedToAssembly )( 
            ICorProfilerCallback2 * This,
            /* [in] */ ModuleID moduleId,
            /* [in] */ AssemblyID AssemblyId);
        
        HRESULT ( STDMETHODCALLTYPE *ClassLoadStarted )( 
            ICorProfilerCallback2 * This,
            /* [in] */ ClassID classId);
        
        HRESULT ( STDMETHODCALLTYPE *ClassLoadFinished )( 
            ICorProfilerCallback2 * This,
            /* [in] */ ClassID classId,
            /* [in] */ HRESULT hrStatus);
        
        HRESULT ( STDMETHODCALLTYPE *ClassUnloadStarted )( 
            ICorProfilerCallback2 * This,
            /* [in] */ ClassID classId);
        
        HRESULT ( STDMETHODCALLTYPE *ClassUnloadFinished )( 
            ICorProfilerCallback2 * This,
            /* [in] */ ClassID classId,
            /* [in] */ HRESULT hrStatus);
        
        HRESULT ( STDMETHODCALLTYPE *FunctionUnloadStarted )( 
            ICorProfilerCallback2 * This,
            /* [in] */ FunctionID functionId);
        
        HRESULT ( STDMETHODCALLTYPE *JITCompilationStarted )( 
            ICorProfilerCallback2 * This,
            /* [in] */ FunctionID functionId,
            /* [in] */ BOOL fIsSafeToBlock);
        
        HRESULT ( STDMETHODCALLTYPE *JITCompilationFinished )( 
            ICorProfilerCallback2 * This,
            /* [in] */ FunctionID functionId,
            /* [in] */ HRESULT hrStatus,
            /* [in] */ BOOL fIsSafeToBlock);
        
        HRESULT ( STDMETHODCALLTYPE *JITCachedFunctionSearchStarted )( 
            ICorProfilerCallback2 * This,
            /* [in] */ FunctionID functionId,
            /* [out] */ BOOL *pbUseCachedFunction);
        
        HRESULT ( STDMETHODCALLTYPE *JITCachedFunctionSearchFinished )( 
            ICorProfilerCallback2 * This,
            /* [in] */ FunctionID functionId,
            /* [in] */ COR_PRF_JIT_CACHE result);
        
        HRESULT ( STDMETHODCALLTYPE *JITFunctionPitched )( 
            ICorProfilerCallback2 * This,
            /* [in] */ FunctionID functionId);
        
        HRESULT ( STDMETHODCALLTYPE *JITInlining )( 
            ICorProfilerCallback2 * This,
            /* [in] */ FunctionID callerId,
            /* [in] */ FunctionID calleeId,
            /* [out] */ BOOL *pfShouldInline);
        
        HRESULT ( STDMETHODCALLTYPE *ThreadCreated )( 
            ICorProfilerCallback2 * This,
            /* [in] */ ThreadID threadId);
        
        HRESULT ( STDMETHODCALLTYPE *ThreadDestroyed )( 
            ICorProfilerCallback2 * This,
            /* [in] */ ThreadID threadId);
        
        HRESULT ( STDMETHODCALLTYPE *ThreadAssignedToOSThread )( 
            ICorProfilerCallback2 * This,
            /* [in] */ ThreadID managedThreadId,
            /* [in] */ DWORD osThreadId);
        
        HRESULT ( STDMETHODCALLTYPE *RemotingClientInvocationStarted )( 
            ICorProfilerCallback2 * This);
        
        HRESULT ( STDMETHODCALLTYPE *RemotingClientSendingMessage )( 
            ICorProfilerCallback2 * This,
            /* [in] */ GUID *pCookie,
            /* [in] */ BOOL fIsAsync);
        
        HRESULT ( STDMETHODCALLTYPE *RemotingClientReceivingReply )( 
            ICorProfilerCallback2 * This,
            /* [in] */ GUID *pCookie,
            /* [in] */ BOOL fIsAsync);
        
        HRESULT ( STDMETHODCALLTYPE *RemotingClientInvocationFinished )( 
            ICorProfilerCallback2 * This);
        
        HRESULT ( STDMETHODCALLTYPE *RemotingServerReceivingMessage )( 
            ICorProfilerCallback2 * This,
            /* [in] */ GUID *pCookie,
            /* [in] */ BOOL fIsAsync);
        
        HRESULT ( STDMETHODCALLTYPE *RemotingServerInvocationStarted )( 
            ICorProfilerCallback2 * This);
        
        HRESULT ( STDMETHODCALLTYPE *RemotingServerInvocationReturned )( 
            ICorProfilerCallback2 * This);
        
        HRESULT ( STDMETHODCALLTYPE *RemotingServerSendingReply )( 
            ICorProfilerCallback2 * This,
            /* [in] */ GUID *pCookie,
            /* [in] */ BOOL fIsAsync);
        
        HRESULT ( STDMETHODCALLTYPE *UnmanagedToManagedTransition )( 
            ICorProfilerCallback2 * This,
            /* [in] */ FunctionID functionId,
            /* [in] */ COR_PRF_TRANSITION_REASON reason);
        
        HRESULT ( STDMETHODCALLTYPE *ManagedToUnmanagedTransition )( 
            ICorProfilerCallback2 * This,
            /* [in] */ FunctionID functionId,
            /* [in] */ COR_PRF_TRANSITION_REASON reason);
        
        HRESULT ( STDMETHODCALLTYPE *RuntimeSuspendStarted )( 
            ICorProfilerCallback2 * This,
            /* [in] */ COR_PRF_SUSPEND_REASON suspendReason);
        
        HRESULT ( STDMETHODCALLTYPE *RuntimeSuspendFinished )( 
            ICorProfilerCallback2 * This);
        
        HRESULT ( STDMETHODCALLTYPE *RuntimeSuspendAborted )( 
            ICorProfilerCallback2 * This);
        
        HRESULT ( STDMETHODCALLTYPE *RuntimeResumeStarted )( 
            ICorProfilerCallback2 * This);
        
        HRESULT ( STDMETHODCALLTYPE *RuntimeResumeFinished )( 
            ICorProfilerCallback2 * This);
        
        HRESULT ( STDMETHODCALLTYPE *RuntimeThreadSuspended )( 
            ICorProfilerCallback2 * This,
            /* [in] */ ThreadID threadId);
        
        HRESULT ( STDMETHODCALLTYPE *RuntimeThreadResumed )( 
            ICorProfilerCallback2 * This,
            /* [in] */ ThreadID threadId);
        
        HRESULT ( STDMETHODCALLTYPE *MovedReferences )( 
            ICorProfilerCallback2 * This,
            /* [in] */ ULONG cMovedObjectIDRanges,
            /* [size_is][in] */ ObjectID oldObjectIDRangeStart[  ],
            /* [size_is][in] */ ObjectID newObjectIDRangeStart[  ],
            /* [size_is][in] */ ULONG cObjectIDRangeLength[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *ObjectAllocated )( 
            ICorProfilerCallback2 * This,
            /* [in] */ ObjectID objectId,
            /* [in] */ ClassID classId);
        
        HRESULT ( STDMETHODCALLTYPE *ObjectsAllocatedByClass )( 
            ICorProfilerCallback2 * This,
            /* [in] */ ULONG cClassCount,
            /* [size_is][in] */ ClassID classIds[  ],
            /* [size_is][in] */ ULONG cObjects[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *ObjectReferences )( 
            ICorProfilerCallback2 * This,
            /* [in] */ ObjectID objectId,
            /* [in] */ ClassID classId,
            /* [in] */ ULONG cObjectRefs,
            /* [size_is][in] */ ObjectID objectRefIds[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *RootReferences )( 
            ICorProfilerCallback2 * This,
            /* [in] */ ULONG cRootRefs,
            /* [size_is][in] */ ObjectID rootRefIds[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *ExceptionThrown )( 
            ICorProfilerCallback2 * This,
            /* [in] */ ObjectID thrownObjectId);
        
        HRESULT ( STDMETHODCALLTYPE *ExceptionSearchFunctionEnter )( 
            ICorProfilerCallback2 * This,
            /* [in] */ FunctionID functionId);
        
        HRESULT ( STDMETHODCALLTYPE *ExceptionSearchFunctionLeave )( 
            ICorProfilerCallback2 * This);
        
        HRESULT ( STDMETHODCALLTYPE *ExceptionSearchFilterEnter )( 
            ICorProfilerCallback2 * This,
            /* [in] */ FunctionID functionId);
        
        HRESULT ( STDMETHODCALLTYPE *ExceptionSearchFilterLeave )( 
            ICorProfilerCallback2 * This);
        
        HRESULT ( STDMETHODCALLTYPE *ExceptionSearchCatcherFound )( 
            ICorProfilerCallback2 * This,
            /* [in] */ FunctionID functionId);
        
        HRESULT ( STDMETHODCALLTYPE *ExceptionOSHandlerEnter )( 
            ICorProfilerCallback2 * This,
            /* [in] */ UINT_PTR __unused);
        
        HRESULT ( STDMETHODCALLTYPE *ExceptionOSHandlerLeave )( 
            ICorProfilerCallback2 * This,
            /* [in] */ UINT_PTR __unused);
        
        HRESULT ( STDMETHODCALLTYPE *ExceptionUnwindFunctionEnter )( 
            ICorProfilerCallback2 * This,
            /* [in] */ FunctionID functionId);
        
        HRESULT ( STDMETHODCALLTYPE *ExceptionUnwindFunctionLeave )( 
            ICorProfilerCallback2 * This);
        
        HRESULT ( STDMETHODCALLTYPE *ExceptionUnwindFinallyEnter )( 
            ICorProfilerCallback2 * This,
            /* [in] */ FunctionID functionId);
        
        HRESULT ( STDMETHODCALLTYPE *ExceptionUnwindFinallyLeave )( 
            ICorProfilerCallback2 * This);
        
        HRESULT ( STDMETHODCALLTYPE *ExceptionCatcherEnter )( 
            ICorProfilerCallback2 * This,
            /* [in] */ FunctionID functionId,
            /* [in] */ ObjectID objectId);
        
        HRESULT ( STDMETHODCALLTYPE *ExceptionCatcherLeave )( 
            ICorProfilerCallback2 * This);
        
        HRESULT ( STDMETHODCALLTYPE *COMClassicVTableCreated )( 
            ICorProfilerCallback2 * This,
            /* [in] */ ClassID wrappedClassId,
            /* [in] */ REFGUID implementedIID,
            /* [in] */ void *pVTable,
            /* [in] */ ULONG cSlots);
        
        HRESULT ( STDMETHODCALLTYPE *COMClassicVTableDestroyed )( 
            ICorProfilerCallback2 * This,
            /* [in] */ ClassID wrappedClassId,
            /* [in] */ REFGUID implementedIID,
            /* [in] */ void *pVTable);
        
        HRESULT ( STDMETHODCALLTYPE *ExceptionCLRCatcherFound )( 
            ICorProfilerCallback2 * This);
        
        HRESULT ( STDMETHODCALLTYPE *ExceptionCLRCatcherExecute )( 
            ICorProfilerCallback2 * This);
        
        HRESULT ( STDMETHODCALLTYPE *ThreadNameChanged )( 
            ICorProfilerCallback2 * This,
            /* [in] */ ThreadID threadId,
            /* [in] */ ULONG cchName,
            /* [annotation][in] */ 
            _In_reads_opt_(cchName)  WCHAR name[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *GarbageCollectionStarted )( 
            ICorProfilerCallback2 * This,
            /* [in] */ int cGenerations,
            /* [size_is][in] */ BOOL generationCollected[  ],
            /* [in] */ COR_PRF_GC_REASON reason);
        
        HRESULT ( STDMETHODCALLTYPE *SurvivingReferences )( 
            ICorProfilerCallback2 * This,
            /* [in] */ ULONG cSurvivingObjectIDRanges,
            /* [size_is][in] */ ObjectID objectIDRangeStart[  ],
            /* [size_is][in] */ ULONG cObjectIDRangeLength[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *GarbageCollectionFinished )( 
            ICorProfilerCallback2 * This);
        
        HRESULT ( STDMETHODCALLTYPE *FinalizeableObjectQueued )( 
            ICorProfilerCallback2 * This,
            /* [in] */ DWORD finalizerFlags,
            /* [in] */ ObjectID objectID);
        
        HRESULT ( STDMETHODCALLTYPE *RootReferences2 )( 
            ICorProfilerCallback2 * This,
            /* [in] */ ULONG cRootRefs,
            /* [size_is][in] */ ObjectID rootRefIds[  ],
            /* [size_is][in] */ COR_PRF_GC_ROOT_KIND rootKinds[  ],
            /* [size_is][in] */ COR_PRF_GC_ROOT_FLAGS rootFlags[  ],
            /* [size_is][in] */ UINT_PTR rootIds[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *HandleCreated )( 
            ICorProfilerCallback2 * This,
            /* [in] */ GCHandleID handleId,
            /* [in] */ ObjectID initialObjectId);
        
        HRESULT ( STDMETHODCALLTYPE *HandleDestroyed )( 
            ICorProfilerCallback2 * This,
            /* [in] */ GCHandleID handleId);
        
        END_INTERFACE
    } ICorProfilerCallback2Vtbl;

    interface ICorProfilerCallback2
    {
        CONST_VTBL struct ICorProfilerCallback2Vtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define ICorProfilerCallback2_QueryInterface(This,riid,ppvObject)   \
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define ICorProfilerCallback2_AddRef(This)  \
    ( (This)->lpVtbl -> AddRef(This) ) 

#define ICorProfilerCallback2_Release(This) \
    ( (This)->lpVtbl -> Release(This) ) 


#define ICorProfilerCallback2_Initialize(This,pICorProfilerInfoUnk) \
    ( (This)->lpVtbl -> Initialize(This,pICorProfilerInfoUnk) ) 

#define ICorProfilerCallback2_Shutdown(This)    \
    ( (This)->lpVtbl -> Shutdown(This) ) 

#define ICorProfilerCallback2_AppDomainCreationStarted(This,appDomainId)    \
    ( (This)->lpVtbl -> AppDomainCreationStarted(This,appDomainId) ) 

#define ICorProfilerCallback2_AppDomainCreationFinished(This,appDomainId,hrStatus)  \
    ( (This)->lpVtbl -> AppDomainCreationFinished(This,appDomainId,hrStatus) ) 

#define ICorProfilerCallback2_AppDomainShutdownStarted(This,appDomainId)    \
    ( (This)->lpVtbl -> AppDomainShutdownStarted(This,appDomainId) ) 

#define ICorProfilerCallback2_AppDomainShutdownFinished(This,appDomainId,hrStatus)  \
    ( (This)->lpVtbl -> AppDomainShutdownFinished(This,appDomainId,hrStatus) ) 

#define ICorProfilerCallback2_AssemblyLoadStarted(This,assemblyId)  \
    ( (This)->lpVtbl -> AssemblyLoadStarted(This,assemblyId) ) 

#define ICorProfilerCallback2_AssemblyLoadFinished(This,assemblyId,hrStatus)    \
    ( (This)->lpVtbl -> AssemblyLoadFinished(This,assemblyId,hrStatus) ) 

#define ICorProfilerCallback2_AssemblyUnloadStarted(This,assemblyId)    \
    ( (This)->lpVtbl -> AssemblyUnloadStarted(This,assemblyId) ) 

#define ICorProfilerCallback2_AssemblyUnloadFinished(This,assemblyId,hrStatus)  \
    ( (This)->lpVtbl -> AssemblyUnloadFinished(This,assemblyId,hrStatus) ) 

#define ICorProfilerCallback2_ModuleLoadStarted(This,moduleId)  \
    ( (This)->lpVtbl -> ModuleLoadStarted(This,moduleId) ) 

#define ICorProfilerCallback2_ModuleLoadFinished(This,moduleId,hrStatus)    \
    ( (This)->lpVtbl -> ModuleLoadFinished(This,moduleId,hrStatus) ) 

#define ICorProfilerCallback2_ModuleUnloadStarted(This,moduleId)    \
    ( (This)->lpVtbl -> ModuleUnloadStarted(This,moduleId) ) 

#define ICorProfilerCallback2_ModuleUnloadFinished(This,moduleId,hrStatus)  \
    ( (This)->lpVtbl -> ModuleUnloadFinished(This,moduleId,hrStatus) ) 

#define ICorProfilerCallback2_ModuleAttachedToAssembly(This,moduleId,AssemblyId)    \
    ( (This)->lpVtbl -> ModuleAttachedToAssembly(This,moduleId,AssemblyId) ) 

#define ICorProfilerCallback2_ClassLoadStarted(This,classId)    \
    ( (This)->lpVtbl -> ClassLoadStarted(This,classId) ) 

#define ICorProfilerCallback2_ClassLoadFinished(This,classId,hrStatus)  \
    ( (This)->lpVtbl -> ClassLoadFinished(This,classId,hrStatus) ) 

#define ICorProfilerCallback2_ClassUnloadStarted(This,classId)  \
    ( (This)->lpVtbl -> ClassUnloadStarted(This,classId) ) 

#define ICorProfilerCallback2_ClassUnloadFinished(This,classId,hrStatus)    \
    ( (This)->lpVtbl -> ClassUnloadFinished(This,classId,hrStatus) ) 

#define ICorProfilerCallback2_FunctionUnloadStarted(This,functionId)    \
    ( (This)->lpVtbl -> FunctionUnloadStarted(This,functionId) ) 

#define ICorProfilerCallback2_JITCompilationStarted(This,functionId,fIsSafeToBlock) \
    ( (This)->lpVtbl -> JITCompilationStarted(This,functionId,fIsSafeToBlock) ) 

#define ICorProfilerCallback2_JITCompilationFinished(This,functionId,hrStatus,fIsSafeToBlock)   \
    ( (This)->lpVtbl -> JITCompilationFinished(This,functionId,hrStatus,fIsSafeToBlock) ) 

#define ICorProfilerCallback2_JITCachedFunctionSearchStarted(This,functionId,pbUseCachedFunction)   \
    ( (This)->lpVtbl -> JITCachedFunctionSearchStarted(This,functionId,pbUseCachedFunction) ) 

#define ICorProfilerCallback2_JITCachedFunctionSearchFinished(This,functionId,result)   \
    ( (This)->lpVtbl -> JITCachedFunctionSearchFinished(This,functionId,result) ) 

#define ICorProfilerCallback2_JITFunctionPitched(This,functionId)   \
    ( (This)->lpVtbl -> JITFunctionPitched(This,functionId) ) 

#define ICorProfilerCallback2_JITInlining(This,callerId,calleeId,pfShouldInline)    \
    ( (This)->lpVtbl -> JITInlining(This,callerId,calleeId,pfShouldInline) ) 

#define ICorProfilerCallback2_ThreadCreated(This,threadId)  \
    ( (This)->lpVtbl -> ThreadCreated(This,threadId) ) 

#define ICorProfilerCallback2_ThreadDestroyed(This,threadId)    \
    ( (This)->lpVtbl -> ThreadDestroyed(This,threadId) ) 

#define ICorProfilerCallback2_ThreadAssignedToOSThread(This,managedThreadId,osThreadId) \
    ( (This)->lpVtbl -> ThreadAssignedToOSThread(This,managedThreadId,osThreadId) ) 

#define ICorProfilerCallback2_RemotingClientInvocationStarted(This) \
    ( (This)->lpVtbl -> RemotingClientInvocationStarted(This) ) 

#define ICorProfilerCallback2_RemotingClientSendingMessage(This,pCookie,fIsAsync)   \
    ( (This)->lpVtbl -> RemotingClientSendingMessage(This,pCookie,fIsAsync) ) 

#define ICorProfilerCallback2_RemotingClientReceivingReply(This,pCookie,fIsAsync)   \
    ( (This)->lpVtbl -> RemotingClientReceivingReply(This,pCookie,fIsAsync) ) 

#define ICorProfilerCallback2_RemotingClientInvocationFinished(This)    \
    ( (This)->lpVtbl -> RemotingClientInvocationFinished(This) ) 

#define ICorProfilerCallback2_RemotingServerReceivingMessage(This,pCookie,fIsAsync) \
    ( (This)->lpVtbl -> RemotingServerReceivingMessage(This,pCookie,fIsAsync) ) 

#define ICorProfilerCallback2_RemotingServerInvocationStarted(This) \
    ( (This)->lpVtbl -> RemotingServerInvocationStarted(This) ) 

#define ICorProfilerCallback2_RemotingServerInvocationReturned(This)    \
    ( (This)->lpVtbl -> RemotingServerInvocationReturned(This) ) 

#define ICorProfilerCallback2_RemotingServerSendingReply(This,pCookie,fIsAsync) \
    ( (This)->lpVtbl -> RemotingServerSendingReply(This,pCookie,fIsAsync) ) 

#define ICorProfilerCallback2_UnmanagedToManagedTransition(This,functionId,reason)  \
    ( (This)->lpVtbl -> UnmanagedToManagedTransition(This,functionId,reason) ) 

#define ICorProfilerCallback2_ManagedToUnmanagedTransition(This,functionId,reason)  \
    ( (This)->lpVtbl -> ManagedToUnmanagedTransition(This,functionId,reason) ) 

#define ICorProfilerCallback2_RuntimeSuspendStarted(This,suspendReason) \
    ( (This)->lpVtbl -> RuntimeSuspendStarted(This,suspendReason) ) 

#define ICorProfilerCallback2_RuntimeSuspendFinished(This)  \
    ( (This)->lpVtbl -> RuntimeSuspendFinished(This) ) 

#define ICorProfilerCallback2_RuntimeSuspendAborted(This)   \
    ( (This)->lpVtbl -> RuntimeSuspendAborted(This) ) 

#define ICorProfilerCallback2_RuntimeResumeStarted(This)    \
    ( (This)->lpVtbl -> RuntimeResumeStarted(This) ) 

#define ICorProfilerCallback2_RuntimeResumeFinished(This)   \
    ( (This)->lpVtbl -> RuntimeResumeFinished(This) ) 

#define ICorProfilerCallback2_RuntimeThreadSuspended(This,threadId) \
    ( (This)->lpVtbl -> RuntimeThreadSuspended(This,threadId) ) 

#define ICorProfilerCallback2_RuntimeThreadResumed(This,threadId)   \
    ( (This)->lpVtbl -> RuntimeThreadResumed(This,threadId) ) 

#define ICorProfilerCallback2_MovedReferences(This,cMovedObjectIDRanges,oldObjectIDRangeStart,newObjectIDRangeStart,cObjectIDRangeLength)   \
    ( (This)->lpVtbl -> MovedReferences(This,cMovedObjectIDRanges,oldObjectIDRangeStart,newObjectIDRangeStart,cObjectIDRangeLength) ) 

#define ICorProfilerCallback2_ObjectAllocated(This,objectId,classId)    \
    ( (This)->lpVtbl -> ObjectAllocated(This,objectId,classId) ) 

#define ICorProfilerCallback2_ObjectsAllocatedByClass(This,cClassCount,classIds,cObjects)   \
    ( (This)->lpVtbl -> ObjectsAllocatedByClass(This,cClassCount,classIds,cObjects) ) 

#define ICorProfilerCallback2_ObjectReferences(This,objectId,classId,cObjectRefs,objectRefIds)  \
    ( (This)->lpVtbl -> ObjectReferences(This,objectId,classId,cObjectRefs,objectRefIds) ) 

#define ICorProfilerCallback2_RootReferences(This,cRootRefs,rootRefIds) \
    ( (This)->lpVtbl -> RootReferences(This,cRootRefs,rootRefIds) ) 

#define ICorProfilerCallback2_ExceptionThrown(This,thrownObjectId)  \
    ( (This)->lpVtbl -> ExceptionThrown(This,thrownObjectId) ) 

#define ICorProfilerCallback2_ExceptionSearchFunctionEnter(This,functionId) \
    ( (This)->lpVtbl -> ExceptionSearchFunctionEnter(This,functionId) ) 

#define ICorProfilerCallback2_ExceptionSearchFunctionLeave(This)    \
    ( (This)->lpVtbl -> ExceptionSearchFunctionLeave(This) ) 

#define ICorProfilerCallback2_ExceptionSearchFilterEnter(This,functionId)   \
    ( (This)->lpVtbl -> ExceptionSearchFilterEnter(This,functionId) ) 

#define ICorProfilerCallback2_ExceptionSearchFilterLeave(This)  \
    ( (This)->lpVtbl -> ExceptionSearchFilterLeave(This) ) 

#define ICorProfilerCallback2_ExceptionSearchCatcherFound(This,functionId)  \
    ( (This)->lpVtbl -> ExceptionSearchCatcherFound(This,functionId) ) 

#define ICorProfilerCallback2_ExceptionOSHandlerEnter(This,__unused)    \
    ( (This)->lpVtbl -> ExceptionOSHandlerEnter(This,__unused) ) 

#define ICorProfilerCallback2_ExceptionOSHandlerLeave(This,__unused)    \
    ( (This)->lpVtbl -> ExceptionOSHandlerLeave(This,__unused) ) 

#define ICorProfilerCallback2_ExceptionUnwindFunctionEnter(This,functionId) \
    ( (This)->lpVtbl -> ExceptionUnwindFunctionEnter(This,functionId) ) 

#define ICorProfilerCallback2_ExceptionUnwindFunctionLeave(This)    \
    ( (This)->lpVtbl -> ExceptionUnwindFunctionLeave(This) ) 

#define ICorProfilerCallback2_ExceptionUnwindFinallyEnter(This,functionId)  \
    ( (This)->lpVtbl -> ExceptionUnwindFinallyEnter(This,functionId) ) 

#define ICorProfilerCallback2_ExceptionUnwindFinallyLeave(This) \
    ( (This)->lpVtbl -> ExceptionUnwindFinallyLeave(This) ) 

#define ICorProfilerCallback2_ExceptionCatcherEnter(This,functionId,objectId)   \
    ( (This)->lpVtbl -> ExceptionCatcherEnter(This,functionId,objectId) ) 

#define ICorProfilerCallback2_ExceptionCatcherLeave(This)   \
    ( (This)->lpVtbl -> ExceptionCatcherLeave(This) ) 

#define ICorProfilerCallback2_COMClassicVTableCreated(This,wrappedClassId,implementedIID,pVTable,cSlots)    \
    ( (This)->lpVtbl -> COMClassicVTableCreated(This,wrappedClassId,implementedIID,pVTable,cSlots) ) 

#define ICorProfilerCallback2_COMClassicVTableDestroyed(This,wrappedClassId,implementedIID,pVTable) \
    ( (This)->lpVtbl -> COMClassicVTableDestroyed(This,wrappedClassId,implementedIID,pVTable) ) 

#define ICorProfilerCallback2_ExceptionCLRCatcherFound(This)    \
    ( (This)->lpVtbl -> ExceptionCLRCatcherFound(This) ) 

#define ICorProfilerCallback2_ExceptionCLRCatcherExecute(This)  \
    ( (This)->lpVtbl -> ExceptionCLRCatcherExecute(This) ) 


#define ICorProfilerCallback2_ThreadNameChanged(This,threadId,cchName,name) \
    ( (This)->lpVtbl -> ThreadNameChanged(This,threadId,cchName,name) ) 

#define ICorProfilerCallback2_GarbageCollectionStarted(This,cGenerations,generationCollected,reason)    \
    ( (This)->lpVtbl -> GarbageCollectionStarted(This,cGenerations,generationCollected,reason) ) 

#define ICorProfilerCallback2_SurvivingReferences(This,cSurvivingObjectIDRanges,objectIDRangeStart,cObjectIDRangeLength)    \
    ( (This)->lpVtbl -> SurvivingReferences(This,cSurvivingObjectIDRanges,objectIDRangeStart,cObjectIDRangeLength) ) 

#define ICorProfilerCallback2_GarbageCollectionFinished(This)   \
    ( (This)->lpVtbl -> GarbageCollectionFinished(This) ) 

#define ICorProfilerCallback2_FinalizeableObjectQueued(This,finalizerFlags,objectID)    \
    ( (This)->lpVtbl -> FinalizeableObjectQueued(This,finalizerFlags,objectID) ) 

#define ICorProfilerCallback2_RootReferences2(This,cRootRefs,rootRefIds,rootKinds,rootFlags,rootIds)    \
    ( (This)->lpVtbl -> RootReferences2(This,cRootRefs,rootRefIds,rootKinds,rootFlags,rootIds) ) 

#define ICorProfilerCallback2_HandleCreated(This,handleId,initialObjectId)  \
    ( (This)->lpVtbl -> HandleCreated(This,handleId,initialObjectId) ) 

#define ICorProfilerCallback2_HandleDestroyed(This,handleId)    \
    ( (This)->lpVtbl -> HandleDestroyed(This,handleId) ) 

#endif /* COBJMACROS */


#endif  /* C style interface */




#endif  /* __ICorProfilerCallback2_INTERFACE_DEFINED__ */


#ifndef __ICorProfilerCallback3_INTERFACE_DEFINED__
#define __ICorProfilerCallback3_INTERFACE_DEFINED__

/* interface ICorProfilerCallback3 */
/* [local][unique][uuid][object] */ 


EXTERN_C const IID IID_ICorProfilerCallback3;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("4FD2ED52-7731-4b8d-9469-03D2CC3086C5")
    ICorProfilerCallback3 : public ICorProfilerCallback2
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE InitializeForAttach( 
            /* [in] */ IUnknown *pCorProfilerInfoUnk,
            /* [in] */ void *pvClientData,
            /* [in] */ UINT cbClientData) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE ProfilerAttachComplete( void) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE ProfilerDetachSucceeded( void) = 0;
        
    };
    
    
#else   /* C style interface */

    typedef struct ICorProfilerCallback3Vtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            ICorProfilerCallback3 * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            ICorProfilerCallback3 * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            ICorProfilerCallback3 * This);
        
        HRESULT ( STDMETHODCALLTYPE *Initialize )( 
            ICorProfilerCallback3 * This,
            /* [in] */ IUnknown *pICorProfilerInfoUnk);
        
        HRESULT ( STDMETHODCALLTYPE *Shutdown )( 
            ICorProfilerCallback3 * This);
        
        HRESULT ( STDMETHODCALLTYPE *AppDomainCreationStarted )( 
            ICorProfilerCallback3 * This,
            /* [in] */ AppDomainID appDomainId);
        
        HRESULT ( STDMETHODCALLTYPE *AppDomainCreationFinished )( 
            ICorProfilerCallback3 * This,
            /* [in] */ AppDomainID appDomainId,
            /* [in] */ HRESULT hrStatus);
        
        HRESULT ( STDMETHODCALLTYPE *AppDomainShutdownStarted )( 
            ICorProfilerCallback3 * This,
            /* [in] */ AppDomainID appDomainId);
        
        HRESULT ( STDMETHODCALLTYPE *AppDomainShutdownFinished )( 
            ICorProfilerCallback3 * This,
            /* [in] */ AppDomainID appDomainId,
            /* [in] */ HRESULT hrStatus);
        
        HRESULT ( STDMETHODCALLTYPE *AssemblyLoadStarted )( 
            ICorProfilerCallback3 * This,
            /* [in] */ AssemblyID assemblyId);
        
        HRESULT ( STDMETHODCALLTYPE *AssemblyLoadFinished )( 
            ICorProfilerCallback3 * This,
            /* [in] */ AssemblyID assemblyId,
            /* [in] */ HRESULT hrStatus);
        
        HRESULT ( STDMETHODCALLTYPE *AssemblyUnloadStarted )( 
            ICorProfilerCallback3 * This,
            /* [in] */ AssemblyID assemblyId);
        
        HRESULT ( STDMETHODCALLTYPE *AssemblyUnloadFinished )( 
            ICorProfilerCallback3 * This,
            /* [in] */ AssemblyID assemblyId,
            /* [in] */ HRESULT hrStatus);
        
        HRESULT ( STDMETHODCALLTYPE *ModuleLoadStarted )( 
            ICorProfilerCallback3 * This,
            /* [in] */ ModuleID moduleId);
        
        HRESULT ( STDMETHODCALLTYPE *ModuleLoadFinished )( 
            ICorProfilerCallback3 * This,
            /* [in] */ ModuleID moduleId,
            /* [in] */ HRESULT hrStatus);
        
        HRESULT ( STDMETHODCALLTYPE *ModuleUnloadStarted )( 
            ICorProfilerCallback3 * This,
            /* [in] */ ModuleID moduleId);
        
        HRESULT ( STDMETHODCALLTYPE *ModuleUnloadFinished )( 
            ICorProfilerCallback3 * This,
            /* [in] */ ModuleID moduleId,
            /* [in] */ HRESULT hrStatus);
        
        HRESULT ( STDMETHODCALLTYPE *ModuleAttachedToAssembly )( 
            ICorProfilerCallback3 * This,
            /* [in] */ ModuleID moduleId,
            /* [in] */ AssemblyID AssemblyId);
        
        HRESULT ( STDMETHODCALLTYPE *ClassLoadStarted )( 
            ICorProfilerCallback3 * This,
            /* [in] */ ClassID classId);
        
        HRESULT ( STDMETHODCALLTYPE *ClassLoadFinished )( 
            ICorProfilerCallback3 * This,
            /* [in] */ ClassID classId,
            /* [in] */ HRESULT hrStatus);
        
        HRESULT ( STDMETHODCALLTYPE *ClassUnloadStarted )( 
            ICorProfilerCallback3 * This,
            /* [in] */ ClassID classId);
        
        HRESULT ( STDMETHODCALLTYPE *ClassUnloadFinished )( 
            ICorProfilerCallback3 * This,
            /* [in] */ ClassID classId,
            /* [in] */ HRESULT hrStatus);
        
        HRESULT ( STDMETHODCALLTYPE *FunctionUnloadStarted )( 
            ICorProfilerCallback3 * This,
            /* [in] */ FunctionID functionId);
        
        HRESULT ( STDMETHODCALLTYPE *JITCompilationStarted )( 
            ICorProfilerCallback3 * This,
            /* [in] */ FunctionID functionId,
            /* [in] */ BOOL fIsSafeToBlock);
        
        HRESULT ( STDMETHODCALLTYPE *JITCompilationFinished )( 
            ICorProfilerCallback3 * This,
            /* [in] */ FunctionID functionId,
            /* [in] */ HRESULT hrStatus,
            /* [in] */ BOOL fIsSafeToBlock);
        
        HRESULT ( STDMETHODCALLTYPE *JITCachedFunctionSearchStarted )( 
            ICorProfilerCallback3 * This,
            /* [in] */ FunctionID functionId,
            /* [out] */ BOOL *pbUseCachedFunction);
        
        HRESULT ( STDMETHODCALLTYPE *JITCachedFunctionSearchFinished )( 
            ICorProfilerCallback3 * This,
            /* [in] */ FunctionID functionId,
            /* [in] */ COR_PRF_JIT_CACHE result);
        
        HRESULT ( STDMETHODCALLTYPE *JITFunctionPitched )( 
            ICorProfilerCallback3 * This,
            /* [in] */ FunctionID functionId);
        
        HRESULT ( STDMETHODCALLTYPE *JITInlining )( 
            ICorProfilerCallback3 * This,
            /* [in] */ FunctionID callerId,
            /* [in] */ FunctionID calleeId,
            /* [out] */ BOOL *pfShouldInline);
        
        HRESULT ( STDMETHODCALLTYPE *ThreadCreated )( 
            ICorProfilerCallback3 * This,
            /* [in] */ ThreadID threadId);
        
        HRESULT ( STDMETHODCALLTYPE *ThreadDestroyed )( 
            ICorProfilerCallback3 * This,
            /* [in] */ ThreadID threadId);
        
        HRESULT ( STDMETHODCALLTYPE *ThreadAssignedToOSThread )( 
            ICorProfilerCallback3 * This,
            /* [in] */ ThreadID managedThreadId,
            /* [in] */ DWORD osThreadId);
        
        HRESULT ( STDMETHODCALLTYPE *RemotingClientInvocationStarted )( 
            ICorProfilerCallback3 * This);
        
        HRESULT ( STDMETHODCALLTYPE *RemotingClientSendingMessage )( 
            ICorProfilerCallback3 * This,
            /* [in] */ GUID *pCookie,
            /* [in] */ BOOL fIsAsync);
        
        HRESULT ( STDMETHODCALLTYPE *RemotingClientReceivingReply )( 
            ICorProfilerCallback3 * This,
            /* [in] */ GUID *pCookie,
            /* [in] */ BOOL fIsAsync);
        
        HRESULT ( STDMETHODCALLTYPE *RemotingClientInvocationFinished )( 
            ICorProfilerCallback3 * This);
        
        HRESULT ( STDMETHODCALLTYPE *RemotingServerReceivingMessage )( 
            ICorProfilerCallback3 * This,
            /* [in] */ GUID *pCookie,
            /* [in] */ BOOL fIsAsync);
        
        HRESULT ( STDMETHODCALLTYPE *RemotingServerInvocationStarted )( 
            ICorProfilerCallback3 * This);
        
        HRESULT ( STDMETHODCALLTYPE *RemotingServerInvocationReturned )( 
            ICorProfilerCallback3 * This);
        
        HRESULT ( STDMETHODCALLTYPE *RemotingServerSendingReply )( 
            ICorProfilerCallback3 * This,
            /* [in] */ GUID *pCookie,
            /* [in] */ BOOL fIsAsync);
        
        HRESULT ( STDMETHODCALLTYPE *UnmanagedToManagedTransition )( 
            ICorProfilerCallback3 * This,
            /* [in] */ FunctionID functionId,
            /* [in] */ COR_PRF_TRANSITION_REASON reason);
        
        HRESULT ( STDMETHODCALLTYPE *ManagedToUnmanagedTransition )( 
            ICorProfilerCallback3 * This,
            /* [in] */ FunctionID functionId,
            /* [in] */ COR_PRF_TRANSITION_REASON reason);
        
        HRESULT ( STDMETHODCALLTYPE *RuntimeSuspendStarted )( 
            ICorProfilerCallback3 * This,
            /* [in] */ COR_PRF_SUSPEND_REASON suspendReason);
        
        HRESULT ( STDMETHODCALLTYPE *RuntimeSuspendFinished )( 
            ICorProfilerCallback3 * This);
        
        HRESULT ( STDMETHODCALLTYPE *RuntimeSuspendAborted )( 
            ICorProfilerCallback3 * This);
        
        HRESULT ( STDMETHODCALLTYPE *RuntimeResumeStarted )( 
            ICorProfilerCallback3 * This);
        
        HRESULT ( STDMETHODCALLTYPE *RuntimeResumeFinished )( 
            ICorProfilerCallback3 * This);
        
        HRESULT ( STDMETHODCALLTYPE *RuntimeThreadSuspended )( 
            ICorProfilerCallback3 * This,
            /* [in] */ ThreadID threadId);
        
        HRESULT ( STDMETHODCALLTYPE *RuntimeThreadResumed )( 
            ICorProfilerCallback3 * This,
            /* [in] */ ThreadID threadId);
        
        HRESULT ( STDMETHODCALLTYPE *MovedReferences )( 
            ICorProfilerCallback3 * This,
            /* [in] */ ULONG cMovedObjectIDRanges,
            /* [size_is][in] */ ObjectID oldObjectIDRangeStart[  ],
            /* [size_is][in] */ ObjectID newObjectIDRangeStart[  ],
            /* [size_is][in] */ ULONG cObjectIDRangeLength[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *ObjectAllocated )( 
            ICorProfilerCallback3 * This,
            /* [in] */ ObjectID objectId,
            /* [in] */ ClassID classId);
        
        HRESULT ( STDMETHODCALLTYPE *ObjectsAllocatedByClass )( 
            ICorProfilerCallback3 * This,
            /* [in] */ ULONG cClassCount,
            /* [size_is][in] */ ClassID classIds[  ],
            /* [size_is][in] */ ULONG cObjects[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *ObjectReferences )( 
            ICorProfilerCallback3 * This,
            /* [in] */ ObjectID objectId,
            /* [in] */ ClassID classId,
            /* [in] */ ULONG cObjectRefs,
            /* [size_is][in] */ ObjectID objectRefIds[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *RootReferences )( 
            ICorProfilerCallback3 * This,
            /* [in] */ ULONG cRootRefs,
            /* [size_is][in] */ ObjectID rootRefIds[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *ExceptionThrown )( 
            ICorProfilerCallback3 * This,
            /* [in] */ ObjectID thrownObjectId);
        
        HRESULT ( STDMETHODCALLTYPE *ExceptionSearchFunctionEnter )( 
            ICorProfilerCallback3 * This,
            /* [in] */ FunctionID functionId);
        
        HRESULT ( STDMETHODCALLTYPE *ExceptionSearchFunctionLeave )( 
            ICorProfilerCallback3 * This);
        
        HRESULT ( STDMETHODCALLTYPE *ExceptionSearchFilterEnter )( 
            ICorProfilerCallback3 * This,
            /* [in] */ FunctionID functionId);
        
        HRESULT ( STDMETHODCALLTYPE *ExceptionSearchFilterLeave )( 
            ICorProfilerCallback3 * This);
        
        HRESULT ( STDMETHODCALLTYPE *ExceptionSearchCatcherFound )( 
            ICorProfilerCallback3 * This,
            /* [in] */ FunctionID functionId);
        
        HRESULT ( STDMETHODCALLTYPE *ExceptionOSHandlerEnter )( 
            ICorProfilerCallback3 * This,
            /* [in] */ UINT_PTR __unused);
        
        HRESULT ( STDMETHODCALLTYPE *ExceptionOSHandlerLeave )( 
            ICorProfilerCallback3 * This,
            /* [in] */ UINT_PTR __unused);
        
        HRESULT ( STDMETHODCALLTYPE *ExceptionUnwindFunctionEnter )( 
            ICorProfilerCallback3 * This,
            /* [in] */ FunctionID functionId);
        
        HRESULT ( STDMETHODCALLTYPE *ExceptionUnwindFunctionLeave )( 
            ICorProfilerCallback3 * This);
        
        HRESULT ( STDMETHODCALLTYPE *ExceptionUnwindFinallyEnter )( 
            ICorProfilerCallback3 * This,
            /* [in] */ FunctionID functionId);
        
        HRESULT ( STDMETHODCALLTYPE *ExceptionUnwindFinallyLeave )( 
            ICorProfilerCallback3 * This);
        
        HRESULT ( STDMETHODCALLTYPE *ExceptionCatcherEnter )( 
            ICorProfilerCallback3 * This,
            /* [in] */ FunctionID functionId,
            /* [in] */ ObjectID objectId);
        
        HRESULT ( STDMETHODCALLTYPE *ExceptionCatcherLeave )( 
            ICorProfilerCallback3 * This);
        
        HRESULT ( STDMETHODCALLTYPE *COMClassicVTableCreated )( 
            ICorProfilerCallback3 * This,
            /* [in] */ ClassID wrappedClassId,
            /* [in] */ REFGUID implementedIID,
            /* [in] */ void *pVTable,
            /* [in] */ ULONG cSlots);
        
        HRESULT ( STDMETHODCALLTYPE *COMClassicVTableDestroyed )( 
            ICorProfilerCallback3 * This,
            /* [in] */ ClassID wrappedClassId,
            /* [in] */ REFGUID implementedIID,
            /* [in] */ void *pVTable);
        
        HRESULT ( STDMETHODCALLTYPE *ExceptionCLRCatcherFound )( 
            ICorProfilerCallback3 * This);
        
        HRESULT ( STDMETHODCALLTYPE *ExceptionCLRCatcherExecute )( 
            ICorProfilerCallback3 * This);
        
        HRESULT ( STDMETHODCALLTYPE *ThreadNameChanged )( 
            ICorProfilerCallback3 * This,
            /* [in] */ ThreadID threadId,
            /* [in] */ ULONG cchName,
            /* [annotation][in] */ 
            _In_reads_opt_(cchName)  WCHAR name[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *GarbageCollectionStarted )( 
            ICorProfilerCallback3 * This,
            /* [in] */ int cGenerations,
            /* [size_is][in] */ BOOL generationCollected[  ],
            /* [in] */ COR_PRF_GC_REASON reason);
        
        HRESULT ( STDMETHODCALLTYPE *SurvivingReferences )( 
            ICorProfilerCallback3 * This,
            /* [in] */ ULONG cSurvivingObjectIDRanges,
            /* [size_is][in] */ ObjectID objectIDRangeStart[  ],
            /* [size_is][in] */ ULONG cObjectIDRangeLength[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *GarbageCollectionFinished )( 
            ICorProfilerCallback3 * This);
        
        HRESULT ( STDMETHODCALLTYPE *FinalizeableObjectQueued )( 
            ICorProfilerCallback3 * This,
            /* [in] */ DWORD finalizerFlags,
            /* [in] */ ObjectID objectID);
        
        HRESULT ( STDMETHODCALLTYPE *RootReferences2 )( 
            ICorProfilerCallback3 * This,
            /* [in] */ ULONG cRootRefs,
            /* [size_is][in] */ ObjectID rootRefIds[  ],
            /* [size_is][in] */ COR_PRF_GC_ROOT_KIND rootKinds[  ],
            /* [size_is][in] */ COR_PRF_GC_ROOT_FLAGS rootFlags[  ],
            /* [size_is][in] */ UINT_PTR rootIds[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *HandleCreated )( 
            ICorProfilerCallback3 * This,
            /* [in] */ GCHandleID handleId,
            /* [in] */ ObjectID initialObjectId);
        
        HRESULT ( STDMETHODCALLTYPE *HandleDestroyed )( 
            ICorProfilerCallback3 * This,
            /* [in] */ GCHandleID handleId);
        
        HRESULT ( STDMETHODCALLTYPE *InitializeForAttach )( 
            ICorProfilerCallback3 * This,
            /* [in] */ IUnknown *pCorProfilerInfoUnk,
            /* [in] */ void *pvClientData,
            /* [in] */ UINT cbClientData);
        
        HRESULT ( STDMETHODCALLTYPE *ProfilerAttachComplete )( 
            ICorProfilerCallback3 * This);
        
        HRESULT ( STDMETHODCALLTYPE *ProfilerDetachSucceeded )( 
            ICorProfilerCallback3 * This);
        
        END_INTERFACE
    } ICorProfilerCallback3Vtbl;

    interface ICorProfilerCallback3
    {
        CONST_VTBL struct ICorProfilerCallback3Vtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define ICorProfilerCallback3_QueryInterface(This,riid,ppvObject)   \
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define ICorProfilerCallback3_AddRef(This)  \
    ( (This)->lpVtbl -> AddRef(This) ) 

#define ICorProfilerCallback3_Release(This) \
    ( (This)->lpVtbl -> Release(This) ) 


#define ICorProfilerCallback3_Initialize(This,pICorProfilerInfoUnk) \
    ( (This)->lpVtbl -> Initialize(This,pICorProfilerInfoUnk) ) 

#define ICorProfilerCallback3_Shutdown(This)    \
    ( (This)->lpVtbl -> Shutdown(This) ) 

#define ICorProfilerCallback3_AppDomainCreationStarted(This,appDomainId)    \
    ( (This)->lpVtbl -> AppDomainCreationStarted(This,appDomainId) ) 

#define ICorProfilerCallback3_AppDomainCreationFinished(This,appDomainId,hrStatus)  \
    ( (This)->lpVtbl -> AppDomainCreationFinished(This,appDomainId,hrStatus) ) 

#define ICorProfilerCallback3_AppDomainShutdownStarted(This,appDomainId)    \
    ( (This)->lpVtbl -> AppDomainShutdownStarted(This,appDomainId) ) 

#define ICorProfilerCallback3_AppDomainShutdownFinished(This,appDomainId,hrStatus)  \
    ( (This)->lpVtbl -> AppDomainShutdownFinished(This,appDomainId,hrStatus) ) 

#define ICorProfilerCallback3_AssemblyLoadStarted(This,assemblyId)  \
    ( (This)->lpVtbl -> AssemblyLoadStarted(This,assemblyId) ) 

#define ICorProfilerCallback3_AssemblyLoadFinished(This,assemblyId,hrStatus)    \
    ( (This)->lpVtbl -> AssemblyLoadFinished(This,assemblyId,hrStatus) ) 

#define ICorProfilerCallback3_AssemblyUnloadStarted(This,assemblyId)    \
    ( (This)->lpVtbl -> AssemblyUnloadStarted(This,assemblyId) ) 

#define ICorProfilerCallback3_AssemblyUnloadFinished(This,assemblyId,hrStatus)  \
    ( (This)->lpVtbl -> AssemblyUnloadFinished(This,assemblyId,hrStatus) ) 

#define ICorProfilerCallback3_ModuleLoadStarted(This,moduleId)  \
    ( (This)->lpVtbl -> ModuleLoadStarted(This,moduleId) ) 

#define ICorProfilerCallback3_ModuleLoadFinished(This,moduleId,hrStatus)    \
    ( (This)->lpVtbl -> ModuleLoadFinished(This,moduleId,hrStatus) ) 

#define ICorProfilerCallback3_ModuleUnloadStarted(This,moduleId)    \
    ( (This)->lpVtbl -> ModuleUnloadStarted(This,moduleId) ) 

#define ICorProfilerCallback3_ModuleUnloadFinished(This,moduleId,hrStatus)  \
    ( (This)->lpVtbl -> ModuleUnloadFinished(This,moduleId,hrStatus) ) 

#define ICorProfilerCallback3_ModuleAttachedToAssembly(This,moduleId,AssemblyId)    \
    ( (This)->lpVtbl -> ModuleAttachedToAssembly(This,moduleId,AssemblyId) ) 

#define ICorProfilerCallback3_ClassLoadStarted(This,classId)    \
    ( (This)->lpVtbl -> ClassLoadStarted(This,classId) ) 

#define ICorProfilerCallback3_ClassLoadFinished(This,classId,hrStatus)  \
    ( (This)->lpVtbl -> ClassLoadFinished(This,classId,hrStatus) ) 

#define ICorProfilerCallback3_ClassUnloadStarted(This,classId)  \
    ( (This)->lpVtbl -> ClassUnloadStarted(This,classId) ) 

#define ICorProfilerCallback3_ClassUnloadFinished(This,classId,hrStatus)    \
    ( (This)->lpVtbl -> ClassUnloadFinished(This,classId,hrStatus) ) 

#define ICorProfilerCallback3_FunctionUnloadStarted(This,functionId)    \
    ( (This)->lpVtbl -> FunctionUnloadStarted(This,functionId) ) 

#define ICorProfilerCallback3_JITCompilationStarted(This,functionId,fIsSafeToBlock) \
    ( (This)->lpVtbl -> JITCompilationStarted(This,functionId,fIsSafeToBlock) ) 

#define ICorProfilerCallback3_JITCompilationFinished(This,functionId,hrStatus,fIsSafeToBlock)   \
    ( (This)->lpVtbl -> JITCompilationFinished(This,functionId,hrStatus,fIsSafeToBlock) ) 

#define ICorProfilerCallback3_JITCachedFunctionSearchStarted(This,functionId,pbUseCachedFunction)   \
    ( (This)->lpVtbl -> JITCachedFunctionSearchStarted(This,functionId,pbUseCachedFunction) ) 

#define ICorProfilerCallback3_JITCachedFunctionSearchFinished(This,functionId,result)   \
    ( (This)->lpVtbl -> JITCachedFunctionSearchFinished(This,functionId,result) ) 

#define ICorProfilerCallback3_JITFunctionPitched(This,functionId)   \
    ( (This)->lpVtbl -> JITFunctionPitched(This,functionId) ) 

#define ICorProfilerCallback3_JITInlining(This,callerId,calleeId,pfShouldInline)    \
    ( (This)->lpVtbl -> JITInlining(This,callerId,calleeId,pfShouldInline) ) 

#define ICorProfilerCallback3_ThreadCreated(This,threadId)  \
    ( (This)->lpVtbl -> ThreadCreated(This,threadId) ) 

#define ICorProfilerCallback3_ThreadDestroyed(This,threadId)    \
    ( (This)->lpVtbl -> ThreadDestroyed(This,threadId) ) 

#define ICorProfilerCallback3_ThreadAssignedToOSThread(This,managedThreadId,osThreadId) \
    ( (This)->lpVtbl -> ThreadAssignedToOSThread(This,managedThreadId,osThreadId) ) 

#define ICorProfilerCallback3_RemotingClientInvocationStarted(This) \
    ( (This)->lpVtbl -> RemotingClientInvocationStarted(This) ) 

#define ICorProfilerCallback3_RemotingClientSendingMessage(This,pCookie,fIsAsync)   \
    ( (This)->lpVtbl -> RemotingClientSendingMessage(This,pCookie,fIsAsync) ) 

#define ICorProfilerCallback3_RemotingClientReceivingReply(This,pCookie,fIsAsync)   \
    ( (This)->lpVtbl -> RemotingClientReceivingReply(This,pCookie,fIsAsync) ) 

#define ICorProfilerCallback3_RemotingClientInvocationFinished(This)    \
    ( (This)->lpVtbl -> RemotingClientInvocationFinished(This) ) 

#define ICorProfilerCallback3_RemotingServerReceivingMessage(This,pCookie,fIsAsync) \
    ( (This)->lpVtbl -> RemotingServerReceivingMessage(This,pCookie,fIsAsync) ) 

#define ICorProfilerCallback3_RemotingServerInvocationStarted(This) \
    ( (This)->lpVtbl -> RemotingServerInvocationStarted(This) ) 

#define ICorProfilerCallback3_RemotingServerInvocationReturned(This)    \
    ( (This)->lpVtbl -> RemotingServerInvocationReturned(This) ) 

#define ICorProfilerCallback3_RemotingServerSendingReply(This,pCookie,fIsAsync) \
    ( (This)->lpVtbl -> RemotingServerSendingReply(This,pCookie,fIsAsync) ) 

#define ICorProfilerCallback3_UnmanagedToManagedTransition(This,functionId,reason)  \
    ( (This)->lpVtbl -> UnmanagedToManagedTransition(This,functionId,reason) ) 

#define ICorProfilerCallback3_ManagedToUnmanagedTransition(This,functionId,reason)  \
    ( (This)->lpVtbl -> ManagedToUnmanagedTransition(This,functionId,reason) ) 

#define ICorProfilerCallback3_RuntimeSuspendStarted(This,suspendReason) \
    ( (This)->lpVtbl -> RuntimeSuspendStarted(This,suspendReason) ) 

#define ICorProfilerCallback3_RuntimeSuspendFinished(This)  \
    ( (This)->lpVtbl -> RuntimeSuspendFinished(This) ) 

#define ICorProfilerCallback3_RuntimeSuspendAborted(This)   \
    ( (This)->lpVtbl -> RuntimeSuspendAborted(This) ) 

#define ICorProfilerCallback3_RuntimeResumeStarted(This)    \
    ( (This)->lpVtbl -> RuntimeResumeStarted(This) ) 

#define ICorProfilerCallback3_RuntimeResumeFinished(This)   \
    ( (This)->lpVtbl -> RuntimeResumeFinished(This) ) 

#define ICorProfilerCallback3_RuntimeThreadSuspended(This,threadId) \
    ( (This)->lpVtbl -> RuntimeThreadSuspended(This,threadId) ) 

#define ICorProfilerCallback3_RuntimeThreadResumed(This,threadId)   \
    ( (This)->lpVtbl -> RuntimeThreadResumed(This,threadId) ) 

#define ICorProfilerCallback3_MovedReferences(This,cMovedObjectIDRanges,oldObjectIDRangeStart,newObjectIDRangeStart,cObjectIDRangeLength)   \
    ( (This)->lpVtbl -> MovedReferences(This,cMovedObjectIDRanges,oldObjectIDRangeStart,newObjectIDRangeStart,cObjectIDRangeLength) ) 

#define ICorProfilerCallback3_ObjectAllocated(This,objectId,classId)    \
    ( (This)->lpVtbl -> ObjectAllocated(This,objectId,classId) ) 

#define ICorProfilerCallback3_ObjectsAllocatedByClass(This,cClassCount,classIds,cObjects)   \
    ( (This)->lpVtbl -> ObjectsAllocatedByClass(This,cClassCount,classIds,cObjects) ) 

#define ICorProfilerCallback3_ObjectReferences(This,objectId,classId,cObjectRefs,objectRefIds)  \
    ( (This)->lpVtbl -> ObjectReferences(This,objectId,classId,cObjectRefs,objectRefIds) ) 

#define ICorProfilerCallback3_RootReferences(This,cRootRefs,rootRefIds) \
    ( (This)->lpVtbl -> RootReferences(This,cRootRefs,rootRefIds) ) 

#define ICorProfilerCallback3_ExceptionThrown(This,thrownObjectId)  \
    ( (This)->lpVtbl -> ExceptionThrown(This,thrownObjectId) ) 

#define ICorProfilerCallback3_ExceptionSearchFunctionEnter(This,functionId) \
    ( (This)->lpVtbl -> ExceptionSearchFunctionEnter(This,functionId) ) 

#define ICorProfilerCallback3_ExceptionSearchFunctionLeave(This)    \
    ( (This)->lpVtbl -> ExceptionSearchFunctionLeave(This) ) 

#define ICorProfilerCallback3_ExceptionSearchFilterEnter(This,functionId)   \
    ( (This)->lpVtbl -> ExceptionSearchFilterEnter(This,functionId) ) 

#define ICorProfilerCallback3_ExceptionSearchFilterLeave(This)  \
    ( (This)->lpVtbl -> ExceptionSearchFilterLeave(This) ) 

#define ICorProfilerCallback3_ExceptionSearchCatcherFound(This,functionId)  \
    ( (This)->lpVtbl -> ExceptionSearchCatcherFound(This,functionId) ) 

#define ICorProfilerCallback3_ExceptionOSHandlerEnter(This,__unused)    \
    ( (This)->lpVtbl -> ExceptionOSHandlerEnter(This,__unused) ) 

#define ICorProfilerCallback3_ExceptionOSHandlerLeave(This,__unused)    \
    ( (This)->lpVtbl -> ExceptionOSHandlerLeave(This,__unused) ) 

#define ICorProfilerCallback3_ExceptionUnwindFunctionEnter(This,functionId) \
    ( (This)->lpVtbl -> ExceptionUnwindFunctionEnter(This,functionId) ) 

#define ICorProfilerCallback3_ExceptionUnwindFunctionLeave(This)    \
    ( (This)->lpVtbl -> ExceptionUnwindFunctionLeave(This) ) 

#define ICorProfilerCallback3_ExceptionUnwindFinallyEnter(This,functionId)  \
    ( (This)->lpVtbl -> ExceptionUnwindFinallyEnter(This,functionId) ) 

#define ICorProfilerCallback3_ExceptionUnwindFinallyLeave(This) \
    ( (This)->lpVtbl -> ExceptionUnwindFinallyLeave(This) ) 

#define ICorProfilerCallback3_ExceptionCatcherEnter(This,functionId,objectId)   \
    ( (This)->lpVtbl -> ExceptionCatcherEnter(This,functionId,objectId) ) 

#define ICorProfilerCallback3_ExceptionCatcherLeave(This)   \
    ( (This)->lpVtbl -> ExceptionCatcherLeave(This) ) 

#define ICorProfilerCallback3_COMClassicVTableCreated(This,wrappedClassId,implementedIID,pVTable,cSlots)    \
    ( (This)->lpVtbl -> COMClassicVTableCreated(This,wrappedClassId,implementedIID,pVTable,cSlots) ) 

#define ICorProfilerCallback3_COMClassicVTableDestroyed(This,wrappedClassId,implementedIID,pVTable) \
    ( (This)->lpVtbl -> COMClassicVTableDestroyed(This,wrappedClassId,implementedIID,pVTable) ) 

#define ICorProfilerCallback3_ExceptionCLRCatcherFound(This)    \
    ( (This)->lpVtbl -> ExceptionCLRCatcherFound(This) ) 

#define ICorProfilerCallback3_ExceptionCLRCatcherExecute(This)  \
    ( (This)->lpVtbl -> ExceptionCLRCatcherExecute(This) ) 


#define ICorProfilerCallback3_ThreadNameChanged(This,threadId,cchName,name) \
    ( (This)->lpVtbl -> ThreadNameChanged(This,threadId,cchName,name) ) 

#define ICorProfilerCallback3_GarbageCollectionStarted(This,cGenerations,generationCollected,reason)    \
    ( (This)->lpVtbl -> GarbageCollectionStarted(This,cGenerations,generationCollected,reason) ) 

#define ICorProfilerCallback3_SurvivingReferences(This,cSurvivingObjectIDRanges,objectIDRangeStart,cObjectIDRangeLength)    \
    ( (This)->lpVtbl -> SurvivingReferences(This,cSurvivingObjectIDRanges,objectIDRangeStart,cObjectIDRangeLength) ) 

#define ICorProfilerCallback3_GarbageCollectionFinished(This)   \
    ( (This)->lpVtbl -> GarbageCollectionFinished(This) ) 

#define ICorProfilerCallback3_FinalizeableObjectQueued(This,finalizerFlags,objectID)    \
    ( (This)->lpVtbl -> FinalizeableObjectQueued(This,finalizerFlags,objectID) ) 

#define ICorProfilerCallback3_RootReferences2(This,cRootRefs,rootRefIds,rootKinds,rootFlags,rootIds)    \
    ( (This)->lpVtbl -> RootReferences2(This,cRootRefs,rootRefIds,rootKinds,rootFlags,rootIds) ) 

#define ICorProfilerCallback3_HandleCreated(This,handleId,initialObjectId)  \
    ( (This)->lpVtbl -> HandleCreated(This,handleId,initialObjectId) ) 

#define ICorProfilerCallback3_HandleDestroyed(This,handleId)    \
    ( (This)->lpVtbl -> HandleDestroyed(This,handleId) ) 


#define ICorProfilerCallback3_InitializeForAttach(This,pCorProfilerInfoUnk,pvClientData,cbClientData)   \
    ( (This)->lpVtbl -> InitializeForAttach(This,pCorProfilerInfoUnk,pvClientData,cbClientData) ) 

#define ICorProfilerCallback3_ProfilerAttachComplete(This)  \
    ( (This)->lpVtbl -> ProfilerAttachComplete(This) ) 

#define ICorProfilerCallback3_ProfilerDetachSucceeded(This) \
    ( (This)->lpVtbl -> ProfilerDetachSucceeded(This) ) 

#endif /* COBJMACROS */


#endif  /* C style interface */




#endif  /* __ICorProfilerCallback3_INTERFACE_DEFINED__ */


#ifndef __ICorProfilerCallback4_INTERFACE_DEFINED__
#define __ICorProfilerCallback4_INTERFACE_DEFINED__

/* interface ICorProfilerCallback4 */
/* [local][unique][uuid][object] */ 


EXTERN_C const IID IID_ICorProfilerCallback4;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("7B63B2E3-107D-4d48-B2F6-F61E229470D2")
    ICorProfilerCallback4 : public ICorProfilerCallback3
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE ReJITCompilationStarted( 
            /* [in] */ FunctionID functionId,
            /* [in] */ ReJITID rejitId,
            /* [in] */ BOOL fIsSafeToBlock) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetReJITParameters( 
            /* [in] */ ModuleID moduleId,
            /* [in] */ mdMethodDef methodId,
            /* [in] */ ICorProfilerFunctionControl *pFunctionControl) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE ReJITCompilationFinished( 
            /* [in] */ FunctionID functionId,
            /* [in] */ ReJITID rejitId,
            /* [in] */ HRESULT hrStatus,
            /* [in] */ BOOL fIsSafeToBlock) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE ReJITError( 
            /* [in] */ ModuleID moduleId,
            /* [in] */ mdMethodDef methodId,
            /* [in] */ FunctionID functionId,
            /* [in] */ HRESULT hrStatus) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE MovedReferences2( 
            /* [in] */ ULONG cMovedObjectIDRanges,
            /* [size_is][in] */ ObjectID oldObjectIDRangeStart[  ],
            /* [size_is][in] */ ObjectID newObjectIDRangeStart[  ],
            /* [size_is][in] */ SIZE_T cObjectIDRangeLength[  ]) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE SurvivingReferences2( 
            /* [in] */ ULONG cSurvivingObjectIDRanges,
            /* [size_is][in] */ ObjectID objectIDRangeStart[  ],
            /* [size_is][in] */ SIZE_T cObjectIDRangeLength[  ]) = 0;
        
    };
    
    
#else   /* C style interface */

    typedef struct ICorProfilerCallback4Vtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            ICorProfilerCallback4 * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            ICorProfilerCallback4 * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            ICorProfilerCallback4 * This);
        
        HRESULT ( STDMETHODCALLTYPE *Initialize )( 
            ICorProfilerCallback4 * This,
            /* [in] */ IUnknown *pICorProfilerInfoUnk);
        
        HRESULT ( STDMETHODCALLTYPE *Shutdown )( 
            ICorProfilerCallback4 * This);
        
        HRESULT ( STDMETHODCALLTYPE *AppDomainCreationStarted )( 
            ICorProfilerCallback4 * This,
            /* [in] */ AppDomainID appDomainId);
        
        HRESULT ( STDMETHODCALLTYPE *AppDomainCreationFinished )( 
            ICorProfilerCallback4 * This,
            /* [in] */ AppDomainID appDomainId,
            /* [in] */ HRESULT hrStatus);
        
        HRESULT ( STDMETHODCALLTYPE *AppDomainShutdownStarted )( 
            ICorProfilerCallback4 * This,
            /* [in] */ AppDomainID appDomainId);
        
        HRESULT ( STDMETHODCALLTYPE *AppDomainShutdownFinished )( 
            ICorProfilerCallback4 * This,
            /* [in] */ AppDomainID appDomainId,
            /* [in] */ HRESULT hrStatus);
        
        HRESULT ( STDMETHODCALLTYPE *AssemblyLoadStarted )( 
            ICorProfilerCallback4 * This,
            /* [in] */ AssemblyID assemblyId);
        
        HRESULT ( STDMETHODCALLTYPE *AssemblyLoadFinished )( 
            ICorProfilerCallback4 * This,
            /* [in] */ AssemblyID assemblyId,
            /* [in] */ HRESULT hrStatus);
        
        HRESULT ( STDMETHODCALLTYPE *AssemblyUnloadStarted )( 
            ICorProfilerCallback4 * This,
            /* [in] */ AssemblyID assemblyId);
        
        HRESULT ( STDMETHODCALLTYPE *AssemblyUnloadFinished )( 
            ICorProfilerCallback4 * This,
            /* [in] */ AssemblyID assemblyId,
            /* [in] */ HRESULT hrStatus);
        
        HRESULT ( STDMETHODCALLTYPE *ModuleLoadStarted )( 
            ICorProfilerCallback4 * This,
            /* [in] */ ModuleID moduleId);
        
        HRESULT ( STDMETHODCALLTYPE *ModuleLoadFinished )( 
            ICorProfilerCallback4 * This,
            /* [in] */ ModuleID moduleId,
            /* [in] */ HRESULT hrStatus);
        
        HRESULT ( STDMETHODCALLTYPE *ModuleUnloadStarted )( 
            ICorProfilerCallback4 * This,
            /* [in] */ ModuleID moduleId);
        
        HRESULT ( STDMETHODCALLTYPE *ModuleUnloadFinished )( 
            ICorProfilerCallback4 * This,
            /* [in] */ ModuleID moduleId,
            /* [in] */ HRESULT hrStatus);
        
        HRESULT ( STDMETHODCALLTYPE *ModuleAttachedToAssembly )( 
            ICorProfilerCallback4 * This,
            /* [in] */ ModuleID moduleId,
            /* [in] */ AssemblyID AssemblyId);
        
        HRESULT ( STDMETHODCALLTYPE *ClassLoadStarted )( 
            ICorProfilerCallback4 * This,
            /* [in] */ ClassID classId);
        
        HRESULT ( STDMETHODCALLTYPE *ClassLoadFinished )( 
            ICorProfilerCallback4 * This,
            /* [in] */ ClassID classId,
            /* [in] */ HRESULT hrStatus);
        
        HRESULT ( STDMETHODCALLTYPE *ClassUnloadStarted )( 
            ICorProfilerCallback4 * This,
            /* [in] */ ClassID classId);
        
        HRESULT ( STDMETHODCALLTYPE *ClassUnloadFinished )( 
            ICorProfilerCallback4 * This,
            /* [in] */ ClassID classId,
            /* [in] */ HRESULT hrStatus);
        
        HRESULT ( STDMETHODCALLTYPE *FunctionUnloadStarted )( 
            ICorProfilerCallback4 * This,
            /* [in] */ FunctionID functionId);
        
        HRESULT ( STDMETHODCALLTYPE *JITCompilationStarted )( 
            ICorProfilerCallback4 * This,
            /* [in] */ FunctionID functionId,
            /* [in] */ BOOL fIsSafeToBlock);
        
        HRESULT ( STDMETHODCALLTYPE *JITCompilationFinished )( 
            ICorProfilerCallback4 * This,
            /* [in] */ FunctionID functionId,
            /* [in] */ HRESULT hrStatus,
            /* [in] */ BOOL fIsSafeToBlock);
        
        HRESULT ( STDMETHODCALLTYPE *JITCachedFunctionSearchStarted )( 
            ICorProfilerCallback4 * This,
            /* [in] */ FunctionID functionId,
            /* [out] */ BOOL *pbUseCachedFunction);
        
        HRESULT ( STDMETHODCALLTYPE *JITCachedFunctionSearchFinished )( 
            ICorProfilerCallback4 * This,
            /* [in] */ FunctionID functionId,
            /* [in] */ COR_PRF_JIT_CACHE result);
        
        HRESULT ( STDMETHODCALLTYPE *JITFunctionPitched )( 
            ICorProfilerCallback4 * This,
            /* [in] */ FunctionID functionId);
        
        HRESULT ( STDMETHODCALLTYPE *JITInlining )( 
            ICorProfilerCallback4 * This,
            /* [in] */ FunctionID callerId,
            /* [in] */ FunctionID calleeId,
            /* [out] */ BOOL *pfShouldInline);
        
        HRESULT ( STDMETHODCALLTYPE *ThreadCreated )( 
            ICorProfilerCallback4 * This,
            /* [in] */ ThreadID threadId);
        
        HRESULT ( STDMETHODCALLTYPE *ThreadDestroyed )( 
            ICorProfilerCallback4 * This,
            /* [in] */ ThreadID threadId);
        
        HRESULT ( STDMETHODCALLTYPE *ThreadAssignedToOSThread )( 
            ICorProfilerCallback4 * This,
            /* [in] */ ThreadID managedThreadId,
            /* [in] */ DWORD osThreadId);
        
        HRESULT ( STDMETHODCALLTYPE *RemotingClientInvocationStarted )( 
            ICorProfilerCallback4 * This);
        
        HRESULT ( STDMETHODCALLTYPE *RemotingClientSendingMessage )( 
            ICorProfilerCallback4 * This,
            /* [in] */ GUID *pCookie,
            /* [in] */ BOOL fIsAsync);
        
        HRESULT ( STDMETHODCALLTYPE *RemotingClientReceivingReply )( 
            ICorProfilerCallback4 * This,
            /* [in] */ GUID *pCookie,
            /* [in] */ BOOL fIsAsync);
        
        HRESULT ( STDMETHODCALLTYPE *RemotingClientInvocationFinished )( 
            ICorProfilerCallback4 * This);
        
        HRESULT ( STDMETHODCALLTYPE *RemotingServerReceivingMessage )( 
            ICorProfilerCallback4 * This,
            /* [in] */ GUID *pCookie,
            /* [in] */ BOOL fIsAsync);
        
        HRESULT ( STDMETHODCALLTYPE *RemotingServerInvocationStarted )( 
            ICorProfilerCallback4 * This);
        
        HRESULT ( STDMETHODCALLTYPE *RemotingServerInvocationReturned )( 
            ICorProfilerCallback4 * This);
        
        HRESULT ( STDMETHODCALLTYPE *RemotingServerSendingReply )( 
            ICorProfilerCallback4 * This,
            /* [in] */ GUID *pCookie,
            /* [in] */ BOOL fIsAsync);
        
        HRESULT ( STDMETHODCALLTYPE *UnmanagedToManagedTransition )( 
            ICorProfilerCallback4 * This,
            /* [in] */ FunctionID functionId,
            /* [in] */ COR_PRF_TRANSITION_REASON reason);
        
        HRESULT ( STDMETHODCALLTYPE *ManagedToUnmanagedTransition )( 
            ICorProfilerCallback4 * This,
            /* [in] */ FunctionID functionId,
            /* [in] */ COR_PRF_TRANSITION_REASON reason);
        
        HRESULT ( STDMETHODCALLTYPE *RuntimeSuspendStarted )( 
            ICorProfilerCallback4 * This,
            /* [in] */ COR_PRF_SUSPEND_REASON suspendReason);
        
        HRESULT ( STDMETHODCALLTYPE *RuntimeSuspendFinished )( 
            ICorProfilerCallback4 * This);
        
        HRESULT ( STDMETHODCALLTYPE *RuntimeSuspendAborted )( 
            ICorProfilerCallback4 * This);
        
        HRESULT ( STDMETHODCALLTYPE *RuntimeResumeStarted )( 
            ICorProfilerCallback4 * This);
        
        HRESULT ( STDMETHODCALLTYPE *RuntimeResumeFinished )( 
            ICorProfilerCallback4 * This);
        
        HRESULT ( STDMETHODCALLTYPE *RuntimeThreadSuspended )( 
            ICorProfilerCallback4 * This,
            /* [in] */ ThreadID threadId);
        
        HRESULT ( STDMETHODCALLTYPE *RuntimeThreadResumed )( 
            ICorProfilerCallback4 * This,
            /* [in] */ ThreadID threadId);
        
        HRESULT ( STDMETHODCALLTYPE *MovedReferences )( 
            ICorProfilerCallback4 * This,
            /* [in] */ ULONG cMovedObjectIDRanges,
            /* [size_is][in] */ ObjectID oldObjectIDRangeStart[  ],
            /* [size_is][in] */ ObjectID newObjectIDRangeStart[  ],
            /* [size_is][in] */ ULONG cObjectIDRangeLength[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *ObjectAllocated )( 
            ICorProfilerCallback4 * This,
            /* [in] */ ObjectID objectId,
            /* [in] */ ClassID classId);
        
        HRESULT ( STDMETHODCALLTYPE *ObjectsAllocatedByClass )( 
            ICorProfilerCallback4 * This,
            /* [in] */ ULONG cClassCount,
            /* [size_is][in] */ ClassID classIds[  ],
            /* [size_is][in] */ ULONG cObjects[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *ObjectReferences )( 
            ICorProfilerCallback4 * This,
            /* [in] */ ObjectID objectId,
            /* [in] */ ClassID classId,
            /* [in] */ ULONG cObjectRefs,
            /* [size_is][in] */ ObjectID objectRefIds[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *RootReferences )( 
            ICorProfilerCallback4 * This,
            /* [in] */ ULONG cRootRefs,
            /* [size_is][in] */ ObjectID rootRefIds[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *ExceptionThrown )( 
            ICorProfilerCallback4 * This,
            /* [in] */ ObjectID thrownObjectId);
        
        HRESULT ( STDMETHODCALLTYPE *ExceptionSearchFunctionEnter )( 
            ICorProfilerCallback4 * This,
            /* [in] */ FunctionID functionId);
        
        HRESULT ( STDMETHODCALLTYPE *ExceptionSearchFunctionLeave )( 
            ICorProfilerCallback4 * This);
        
        HRESULT ( STDMETHODCALLTYPE *ExceptionSearchFilterEnter )( 
            ICorProfilerCallback4 * This,
            /* [in] */ FunctionID functionId);
        
        HRESULT ( STDMETHODCALLTYPE *ExceptionSearchFilterLeave )( 
            ICorProfilerCallback4 * This);
        
        HRESULT ( STDMETHODCALLTYPE *ExceptionSearchCatcherFound )( 
            ICorProfilerCallback4 * This,
            /* [in] */ FunctionID functionId);
        
        HRESULT ( STDMETHODCALLTYPE *ExceptionOSHandlerEnter )( 
            ICorProfilerCallback4 * This,
            /* [in] */ UINT_PTR __unused);
        
        HRESULT ( STDMETHODCALLTYPE *ExceptionOSHandlerLeave )( 
            ICorProfilerCallback4 * This,
            /* [in] */ UINT_PTR __unused);
        
        HRESULT ( STDMETHODCALLTYPE *ExceptionUnwindFunctionEnter )( 
            ICorProfilerCallback4 * This,
            /* [in] */ FunctionID functionId);
        
        HRESULT ( STDMETHODCALLTYPE *ExceptionUnwindFunctionLeave )( 
            ICorProfilerCallback4 * This);
        
        HRESULT ( STDMETHODCALLTYPE *ExceptionUnwindFinallyEnter )( 
            ICorProfilerCallback4 * This,
            /* [in] */ FunctionID functionId);
        
        HRESULT ( STDMETHODCALLTYPE *ExceptionUnwindFinallyLeave )( 
            ICorProfilerCallback4 * This);
        
        HRESULT ( STDMETHODCALLTYPE *ExceptionCatcherEnter )( 
            ICorProfilerCallback4 * This,
            /* [in] */ FunctionID functionId,
            /* [in] */ ObjectID objectId);
        
        HRESULT ( STDMETHODCALLTYPE *ExceptionCatcherLeave )( 
            ICorProfilerCallback4 * This);
        
        HRESULT ( STDMETHODCALLTYPE *COMClassicVTableCreated )( 
            ICorProfilerCallback4 * This,
            /* [in] */ ClassID wrappedClassId,
            /* [in] */ REFGUID implementedIID,
            /* [in] */ void *pVTable,
            /* [in] */ ULONG cSlots);
        
        HRESULT ( STDMETHODCALLTYPE *COMClassicVTableDestroyed )( 
            ICorProfilerCallback4 * This,
            /* [in] */ ClassID wrappedClassId,
            /* [in] */ REFGUID implementedIID,
            /* [in] */ void *pVTable);
        
        HRESULT ( STDMETHODCALLTYPE *ExceptionCLRCatcherFound )( 
            ICorProfilerCallback4 * This);
        
        HRESULT ( STDMETHODCALLTYPE *ExceptionCLRCatcherExecute )( 
            ICorProfilerCallback4 * This);
        
        HRESULT ( STDMETHODCALLTYPE *ThreadNameChanged )( 
            ICorProfilerCallback4 * This,
            /* [in] */ ThreadID threadId,
            /* [in] */ ULONG cchName,
            /* [annotation][in] */ 
            _In_reads_opt_(cchName)  WCHAR name[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *GarbageCollectionStarted )( 
            ICorProfilerCallback4 * This,
            /* [in] */ int cGenerations,
            /* [size_is][in] */ BOOL generationCollected[  ],
            /* [in] */ COR_PRF_GC_REASON reason);
        
        HRESULT ( STDMETHODCALLTYPE *SurvivingReferences )( 
            ICorProfilerCallback4 * This,
            /* [in] */ ULONG cSurvivingObjectIDRanges,
            /* [size_is][in] */ ObjectID objectIDRangeStart[  ],
            /* [size_is][in] */ ULONG cObjectIDRangeLength[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *GarbageCollectionFinished )( 
            ICorProfilerCallback4 * This);
        
        HRESULT ( STDMETHODCALLTYPE *FinalizeableObjectQueued )( 
            ICorProfilerCallback4 * This,
            /* [in] */ DWORD finalizerFlags,
            /* [in] */ ObjectID objectID);
        
        HRESULT ( STDMETHODCALLTYPE *RootReferences2 )( 
            ICorProfilerCallback4 * This,
            /* [in] */ ULONG cRootRefs,
            /* [size_is][in] */ ObjectID rootRefIds[  ],
            /* [size_is][in] */ COR_PRF_GC_ROOT_KIND rootKinds[  ],
            /* [size_is][in] */ COR_PRF_GC_ROOT_FLAGS rootFlags[  ],
            /* [size_is][in] */ UINT_PTR rootIds[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *HandleCreated )( 
            ICorProfilerCallback4 * This,
            /* [in] */ GCHandleID handleId,
            /* [in] */ ObjectID initialObjectId);
        
        HRESULT ( STDMETHODCALLTYPE *HandleDestroyed )( 
            ICorProfilerCallback4 * This,
            /* [in] */ GCHandleID handleId);
        
        HRESULT ( STDMETHODCALLTYPE *InitializeForAttach )( 
            ICorProfilerCallback4 * This,
            /* [in] */ IUnknown *pCorProfilerInfoUnk,
            /* [in] */ void *pvClientData,
            /* [in] */ UINT cbClientData);
        
        HRESULT ( STDMETHODCALLTYPE *ProfilerAttachComplete )( 
            ICorProfilerCallback4 * This);
        
        HRESULT ( STDMETHODCALLTYPE *ProfilerDetachSucceeded )( 
            ICorProfilerCallback4 * This);
        
        HRESULT ( STDMETHODCALLTYPE *ReJITCompilationStarted )( 
            ICorProfilerCallback4 * This,
            /* [in] */ FunctionID functionId,
            /* [in] */ ReJITID rejitId,
            /* [in] */ BOOL fIsSafeToBlock);
        
        HRESULT ( STDMETHODCALLTYPE *GetReJITParameters )( 
            ICorProfilerCallback4 * This,
            /* [in] */ ModuleID moduleId,
            /* [in] */ mdMethodDef methodId,
            /* [in] */ ICorProfilerFunctionControl *pFunctionControl);
        
        HRESULT ( STDMETHODCALLTYPE *ReJITCompilationFinished )( 
            ICorProfilerCallback4 * This,
            /* [in] */ FunctionID functionId,
            /* [in] */ ReJITID rejitId,
            /* [in] */ HRESULT hrStatus,
            /* [in] */ BOOL fIsSafeToBlock);
        
        HRESULT ( STDMETHODCALLTYPE *ReJITError )( 
            ICorProfilerCallback4 * This,
            /* [in] */ ModuleID moduleId,
            /* [in] */ mdMethodDef methodId,
            /* [in] */ FunctionID functionId,
            /* [in] */ HRESULT hrStatus);
        
        HRESULT ( STDMETHODCALLTYPE *MovedReferences2 )( 
            ICorProfilerCallback4 * This,
            /* [in] */ ULONG cMovedObjectIDRanges,
            /* [size_is][in] */ ObjectID oldObjectIDRangeStart[  ],
            /* [size_is][in] */ ObjectID newObjectIDRangeStart[  ],
            /* [size_is][in] */ SIZE_T cObjectIDRangeLength[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *SurvivingReferences2 )( 
            ICorProfilerCallback4 * This,
            /* [in] */ ULONG cSurvivingObjectIDRanges,
            /* [size_is][in] */ ObjectID objectIDRangeStart[  ],
            /* [size_is][in] */ SIZE_T cObjectIDRangeLength[  ]);
        
        END_INTERFACE
    } ICorProfilerCallback4Vtbl;

    interface ICorProfilerCallback4
    {
        CONST_VTBL struct ICorProfilerCallback4Vtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define ICorProfilerCallback4_QueryInterface(This,riid,ppvObject)   \
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define ICorProfilerCallback4_AddRef(This)  \
    ( (This)->lpVtbl -> AddRef(This) ) 

#define ICorProfilerCallback4_Release(This) \
    ( (This)->lpVtbl -> Release(This) ) 


#define ICorProfilerCallback4_Initialize(This,pICorProfilerInfoUnk) \
    ( (This)->lpVtbl -> Initialize(This,pICorProfilerInfoUnk) ) 

#define ICorProfilerCallback4_Shutdown(This)    \
    ( (This)->lpVtbl -> Shutdown(This) ) 

#define ICorProfilerCallback4_AppDomainCreationStarted(This,appDomainId)    \
    ( (This)->lpVtbl -> AppDomainCreationStarted(This,appDomainId) ) 

#define ICorProfilerCallback4_AppDomainCreationFinished(This,appDomainId,hrStatus)  \
    ( (This)->lpVtbl -> AppDomainCreationFinished(This,appDomainId,hrStatus) ) 

#define ICorProfilerCallback4_AppDomainShutdownStarted(This,appDomainId)    \
    ( (This)->lpVtbl -> AppDomainShutdownStarted(This,appDomainId) ) 

#define ICorProfilerCallback4_AppDomainShutdownFinished(This,appDomainId,hrStatus)  \
    ( (This)->lpVtbl -> AppDomainShutdownFinished(This,appDomainId,hrStatus) ) 

#define ICorProfilerCallback4_AssemblyLoadStarted(This,assemblyId)  \
    ( (This)->lpVtbl -> AssemblyLoadStarted(This,assemblyId) ) 

#define ICorProfilerCallback4_AssemblyLoadFinished(This,assemblyId,hrStatus)    \
    ( (This)->lpVtbl -> AssemblyLoadFinished(This,assemblyId,hrStatus) ) 

#define ICorProfilerCallback4_AssemblyUnloadStarted(This,assemblyId)    \
    ( (This)->lpVtbl -> AssemblyUnloadStarted(This,assemblyId) ) 

#define ICorProfilerCallback4_AssemblyUnloadFinished(This,assemblyId,hrStatus)  \
    ( (This)->lpVtbl -> AssemblyUnloadFinished(This,assemblyId,hrStatus) ) 

#define ICorProfilerCallback4_ModuleLoadStarted(This,moduleId)  \
    ( (This)->lpVtbl -> ModuleLoadStarted(This,moduleId) ) 

#define ICorProfilerCallback4_ModuleLoadFinished(This,moduleId,hrStatus)    \
    ( (This)->lpVtbl -> ModuleLoadFinished(This,moduleId,hrStatus) ) 

#define ICorProfilerCallback4_ModuleUnloadStarted(This,moduleId)    \
    ( (This)->lpVtbl -> ModuleUnloadStarted(This,moduleId) ) 

#define ICorProfilerCallback4_ModuleUnloadFinished(This,moduleId,hrStatus)  \
    ( (This)->lpVtbl -> ModuleUnloadFinished(This,moduleId,hrStatus) ) 

#define ICorProfilerCallback4_ModuleAttachedToAssembly(This,moduleId,AssemblyId)    \
    ( (This)->lpVtbl -> ModuleAttachedToAssembly(This,moduleId,AssemblyId) ) 

#define ICorProfilerCallback4_ClassLoadStarted(This,classId)    \
    ( (This)->lpVtbl -> ClassLoadStarted(This,classId) ) 

#define ICorProfilerCallback4_ClassLoadFinished(This,classId,hrStatus)  \
    ( (This)->lpVtbl -> ClassLoadFinished(This,classId,hrStatus) ) 

#define ICorProfilerCallback4_ClassUnloadStarted(This,classId)  \
    ( (This)->lpVtbl -> ClassUnloadStarted(This,classId) ) 

#define ICorProfilerCallback4_ClassUnloadFinished(This,classId,hrStatus)    \
    ( (This)->lpVtbl -> ClassUnloadFinished(This,classId,hrStatus) ) 

#define ICorProfilerCallback4_FunctionUnloadStarted(This,functionId)    \
    ( (This)->lpVtbl -> FunctionUnloadStarted(This,functionId) ) 

#define ICorProfilerCallback4_JITCompilationStarted(This,functionId,fIsSafeToBlock) \
    ( (This)->lpVtbl -> JITCompilationStarted(This,functionId,fIsSafeToBlock) ) 

#define ICorProfilerCallback4_JITCompilationFinished(This,functionId,hrStatus,fIsSafeToBlock)   \
    ( (This)->lpVtbl -> JITCompilationFinished(This,functionId,hrStatus,fIsSafeToBlock) ) 

#define ICorProfilerCallback4_JITCachedFunctionSearchStarted(This,functionId,pbUseCachedFunction)   \
    ( (This)->lpVtbl -> JITCachedFunctionSearchStarted(This,functionId,pbUseCachedFunction) ) 

#define ICorProfilerCallback4_JITCachedFunctionSearchFinished(This,functionId,result)   \
    ( (This)->lpVtbl -> JITCachedFunctionSearchFinished(This,functionId,result) ) 

#define ICorProfilerCallback4_JITFunctionPitched(This,functionId)   \
    ( (This)->lpVtbl -> JITFunctionPitched(This,functionId) ) 

#define ICorProfilerCallback4_JITInlining(This,callerId,calleeId,pfShouldInline)    \
    ( (This)->lpVtbl -> JITInlining(This,callerId,calleeId,pfShouldInline) ) 

#define ICorProfilerCallback4_ThreadCreated(This,threadId)  \
    ( (This)->lpVtbl -> ThreadCreated(This,threadId) ) 

#define ICorProfilerCallback4_ThreadDestroyed(This,threadId)    \
    ( (This)->lpVtbl -> ThreadDestroyed(This,threadId) ) 

#define ICorProfilerCallback4_ThreadAssignedToOSThread(This,managedThreadId,osThreadId) \
    ( (This)->lpVtbl -> ThreadAssignedToOSThread(This,managedThreadId,osThreadId) ) 

#define ICorProfilerCallback4_RemotingClientInvocationStarted(This) \
    ( (This)->lpVtbl -> RemotingClientInvocationStarted(This) ) 

#define ICorProfilerCallback4_RemotingClientSendingMessage(This,pCookie,fIsAsync)   \
    ( (This)->lpVtbl -> RemotingClientSendingMessage(This,pCookie,fIsAsync) ) 

#define ICorProfilerCallback4_RemotingClientReceivingReply(This,pCookie,fIsAsync)   \
    ( (This)->lpVtbl -> RemotingClientReceivingReply(This,pCookie,fIsAsync) ) 

#define ICorProfilerCallback4_RemotingClientInvocationFinished(This)    \
    ( (This)->lpVtbl -> RemotingClientInvocationFinished(This) ) 

#define ICorProfilerCallback4_RemotingServerReceivingMessage(This,pCookie,fIsAsync) \
    ( (This)->lpVtbl -> RemotingServerReceivingMessage(This,pCookie,fIsAsync) ) 

#define ICorProfilerCallback4_RemotingServerInvocationStarted(This) \
    ( (This)->lpVtbl -> RemotingServerInvocationStarted(This) ) 

#define ICorProfilerCallback4_RemotingServerInvocationReturned(This)    \
    ( (This)->lpVtbl -> RemotingServerInvocationReturned(This) ) 

#define ICorProfilerCallback4_RemotingServerSendingReply(This,pCookie,fIsAsync) \
    ( (This)->lpVtbl -> RemotingServerSendingReply(This,pCookie,fIsAsync) ) 

#define ICorProfilerCallback4_UnmanagedToManagedTransition(This,functionId,reason)  \
    ( (This)->lpVtbl -> UnmanagedToManagedTransition(This,functionId,reason) ) 

#define ICorProfilerCallback4_ManagedToUnmanagedTransition(This,functionId,reason)  \
    ( (This)->lpVtbl -> ManagedToUnmanagedTransition(This,functionId,reason) ) 

#define ICorProfilerCallback4_RuntimeSuspendStarted(This,suspendReason) \
    ( (This)->lpVtbl -> RuntimeSuspendStarted(This,suspendReason) ) 

#define ICorProfilerCallback4_RuntimeSuspendFinished(This)  \
    ( (This)->lpVtbl -> RuntimeSuspendFinished(This) ) 

#define ICorProfilerCallback4_RuntimeSuspendAborted(This)   \
    ( (This)->lpVtbl -> RuntimeSuspendAborted(This) ) 

#define ICorProfilerCallback4_RuntimeResumeStarted(This)    \
    ( (This)->lpVtbl -> RuntimeResumeStarted(This) ) 

#define ICorProfilerCallback4_RuntimeResumeFinished(This)   \
    ( (This)->lpVtbl -> RuntimeResumeFinished(This) ) 

#define ICorProfilerCallback4_RuntimeThreadSuspended(This,threadId) \
    ( (This)->lpVtbl -> RuntimeThreadSuspended(This,threadId) ) 

#define ICorProfilerCallback4_RuntimeThreadResumed(This,threadId)   \
    ( (This)->lpVtbl -> RuntimeThreadResumed(This,threadId) ) 

#define ICorProfilerCallback4_MovedReferences(This,cMovedObjectIDRanges,oldObjectIDRangeStart,newObjectIDRangeStart,cObjectIDRangeLength)   \
    ( (This)->lpVtbl -> MovedReferences(This,cMovedObjectIDRanges,oldObjectIDRangeStart,newObjectIDRangeStart,cObjectIDRangeLength) ) 

#define ICorProfilerCallback4_ObjectAllocated(This,objectId,classId)    \
    ( (This)->lpVtbl -> ObjectAllocated(This,objectId,classId) ) 

#define ICorProfilerCallback4_ObjectsAllocatedByClass(This,cClassCount,classIds,cObjects)   \
    ( (This)->lpVtbl -> ObjectsAllocatedByClass(This,cClassCount,classIds,cObjects) ) 

#define ICorProfilerCallback4_ObjectReferences(This,objectId,classId,cObjectRefs,objectRefIds)  \
    ( (This)->lpVtbl -> ObjectReferences(This,objectId,classId,cObjectRefs,objectRefIds) ) 

#define ICorProfilerCallback4_RootReferences(This,cRootRefs,rootRefIds) \
    ( (This)->lpVtbl -> RootReferences(This,cRootRefs,rootRefIds) ) 

#define ICorProfilerCallback4_ExceptionThrown(This,thrownObjectId)  \
    ( (This)->lpVtbl -> ExceptionThrown(This,thrownObjectId) ) 

#define ICorProfilerCallback4_ExceptionSearchFunctionEnter(This,functionId) \
    ( (This)->lpVtbl -> ExceptionSearchFunctionEnter(This,functionId) ) 

#define ICorProfilerCallback4_ExceptionSearchFunctionLeave(This)    \
    ( (This)->lpVtbl -> ExceptionSearchFunctionLeave(This) ) 

#define ICorProfilerCallback4_ExceptionSearchFilterEnter(This,functionId)   \
    ( (This)->lpVtbl -> ExceptionSearchFilterEnter(This,functionId) ) 

#define ICorProfilerCallback4_ExceptionSearchFilterLeave(This)  \
    ( (This)->lpVtbl -> ExceptionSearchFilterLeave(This) ) 

#define ICorProfilerCallback4_ExceptionSearchCatcherFound(This,functionId)  \
    ( (This)->lpVtbl -> ExceptionSearchCatcherFound(This,functionId) ) 

#define ICorProfilerCallback4_ExceptionOSHandlerEnter(This,__unused)    \
    ( (This)->lpVtbl -> ExceptionOSHandlerEnter(This,__unused) ) 

#define ICorProfilerCallback4_ExceptionOSHandlerLeave(This,__unused)    \
    ( (This)->lpVtbl -> ExceptionOSHandlerLeave(This,__unused) ) 

#define ICorProfilerCallback4_ExceptionUnwindFunctionEnter(This,functionId) \
    ( (This)->lpVtbl -> ExceptionUnwindFunctionEnter(This,functionId) ) 

#define ICorProfilerCallback4_ExceptionUnwindFunctionLeave(This)    \
    ( (This)->lpVtbl -> ExceptionUnwindFunctionLeave(This) ) 

#define ICorProfilerCallback4_ExceptionUnwindFinallyEnter(This,functionId)  \
    ( (This)->lpVtbl -> ExceptionUnwindFinallyEnter(This,functionId) ) 

#define ICorProfilerCallback4_ExceptionUnwindFinallyLeave(This) \
    ( (This)->lpVtbl -> ExceptionUnwindFinallyLeave(This) ) 

#define ICorProfilerCallback4_ExceptionCatcherEnter(This,functionId,objectId)   \
    ( (This)->lpVtbl -> ExceptionCatcherEnter(This,functionId,objectId) ) 

#define ICorProfilerCallback4_ExceptionCatcherLeave(This)   \
    ( (This)->lpVtbl -> ExceptionCatcherLeave(This) ) 

#define ICorProfilerCallback4_COMClassicVTableCreated(This,wrappedClassId,implementedIID,pVTable,cSlots)    \
    ( (This)->lpVtbl -> COMClassicVTableCreated(This,wrappedClassId,implementedIID,pVTable,cSlots) ) 

#define ICorProfilerCallback4_COMClassicVTableDestroyed(This,wrappedClassId,implementedIID,pVTable) \
    ( (This)->lpVtbl -> COMClassicVTableDestroyed(This,wrappedClassId,implementedIID,pVTable) ) 

#define ICorProfilerCallback4_ExceptionCLRCatcherFound(This)    \
    ( (This)->lpVtbl -> ExceptionCLRCatcherFound(This) ) 

#define ICorProfilerCallback4_ExceptionCLRCatcherExecute(This)  \
    ( (This)->lpVtbl -> ExceptionCLRCatcherExecute(This) ) 


#define ICorProfilerCallback4_ThreadNameChanged(This,threadId,cchName,name) \
    ( (This)->lpVtbl -> ThreadNameChanged(This,threadId,cchName,name) ) 

#define ICorProfilerCallback4_GarbageCollectionStarted(This,cGenerations,generationCollected,reason)    \
    ( (This)->lpVtbl -> GarbageCollectionStarted(This,cGenerations,generationCollected,reason) ) 

#define ICorProfilerCallback4_SurvivingReferences(This,cSurvivingObjectIDRanges,objectIDRangeStart,cObjectIDRangeLength)    \
    ( (This)->lpVtbl -> SurvivingReferences(This,cSurvivingObjectIDRanges,objectIDRangeStart,cObjectIDRangeLength) ) 

#define ICorProfilerCallback4_GarbageCollectionFinished(This)   \
    ( (This)->lpVtbl -> GarbageCollectionFinished(This) ) 

#define ICorProfilerCallback4_FinalizeableObjectQueued(This,finalizerFlags,objectID)    \
    ( (This)->lpVtbl -> FinalizeableObjectQueued(This,finalizerFlags,objectID) ) 

#define ICorProfilerCallback4_RootReferences2(This,cRootRefs,rootRefIds,rootKinds,rootFlags,rootIds)    \
    ( (This)->lpVtbl -> RootReferences2(This,cRootRefs,rootRefIds,rootKinds,rootFlags,rootIds) ) 

#define ICorProfilerCallback4_HandleCreated(This,handleId,initialObjectId)  \
    ( (This)->lpVtbl -> HandleCreated(This,handleId,initialObjectId) ) 

#define ICorProfilerCallback4_HandleDestroyed(This,handleId)    \
    ( (This)->lpVtbl -> HandleDestroyed(This,handleId) ) 


#define ICorProfilerCallback4_InitializeForAttach(This,pCorProfilerInfoUnk,pvClientData,cbClientData)   \
    ( (This)->lpVtbl -> InitializeForAttach(This,pCorProfilerInfoUnk,pvClientData,cbClientData) ) 

#define ICorProfilerCallback4_ProfilerAttachComplete(This)  \
    ( (This)->lpVtbl -> ProfilerAttachComplete(This) ) 

#define ICorProfilerCallback4_ProfilerDetachSucceeded(This) \
    ( (This)->lpVtbl -> ProfilerDetachSucceeded(This) ) 


#define ICorProfilerCallback4_ReJITCompilationStarted(This,functionId,rejitId,fIsSafeToBlock)   \
    ( (This)->lpVtbl -> ReJITCompilationStarted(This,functionId,rejitId,fIsSafeToBlock) ) 

#define ICorProfilerCallback4_GetReJITParameters(This,moduleId,methodId,pFunctionControl)   \
    ( (This)->lpVtbl -> GetReJITParameters(This,moduleId,methodId,pFunctionControl) ) 

#define ICorProfilerCallback4_ReJITCompilationFinished(This,functionId,rejitId,hrStatus,fIsSafeToBlock) \
    ( (This)->lpVtbl -> ReJITCompilationFinished(This,functionId,rejitId,hrStatus,fIsSafeToBlock) ) 

#define ICorProfilerCallback4_ReJITError(This,moduleId,methodId,functionId,hrStatus)    \
    ( (This)->lpVtbl -> ReJITError(This,moduleId,methodId,functionId,hrStatus) ) 

#define ICorProfilerCallback4_MovedReferences2(This,cMovedObjectIDRanges,oldObjectIDRangeStart,newObjectIDRangeStart,cObjectIDRangeLength)  \
    ( (This)->lpVtbl -> MovedReferences2(This,cMovedObjectIDRanges,oldObjectIDRangeStart,newObjectIDRangeStart,cObjectIDRangeLength) ) 

#define ICorProfilerCallback4_SurvivingReferences2(This,cSurvivingObjectIDRanges,objectIDRangeStart,cObjectIDRangeLength)   \
    ( (This)->lpVtbl -> SurvivingReferences2(This,cSurvivingObjectIDRanges,objectIDRangeStart,cObjectIDRangeLength) ) 

#endif /* COBJMACROS */


#endif  /* C style interface */




#endif  /* __ICorProfilerCallback4_INTERFACE_DEFINED__ */


#ifndef __ICorProfilerCallback5_INTERFACE_DEFINED__
#define __ICorProfilerCallback5_INTERFACE_DEFINED__

/* interface ICorProfilerCallback5 */
/* [local][unique][uuid][object] */ 


EXTERN_C const IID IID_ICorProfilerCallback5;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("8DFBA405-8C9F-45F8-BFFA-83B14CEF78B5")
    ICorProfilerCallback5 : public ICorProfilerCallback4
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE ConditionalWeakTableElementReferences( 
            /* [in] */ ULONG cRootRefs,
            /* [size_is][in] */ ObjectID keyRefIds[  ],
            /* [size_is][in] */ ObjectID valueRefIds[  ],
            /* [size_is][in] */ GCHandleID rootIds[  ]) = 0;
        
    };
    
    
#else   /* C style interface */

    typedef struct ICorProfilerCallback5Vtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            ICorProfilerCallback5 * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            ICorProfilerCallback5 * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            ICorProfilerCallback5 * This);
        
        HRESULT ( STDMETHODCALLTYPE *Initialize )( 
            ICorProfilerCallback5 * This,
            /* [in] */ IUnknown *pICorProfilerInfoUnk);
        
        HRESULT ( STDMETHODCALLTYPE *Shutdown )( 
            ICorProfilerCallback5 * This);
        
        HRESULT ( STDMETHODCALLTYPE *AppDomainCreationStarted )( 
            ICorProfilerCallback5 * This,
            /* [in] */ AppDomainID appDomainId);
        
        HRESULT ( STDMETHODCALLTYPE *AppDomainCreationFinished )( 
            ICorProfilerCallback5 * This,
            /* [in] */ AppDomainID appDomainId,
            /* [in] */ HRESULT hrStatus);
        
        HRESULT ( STDMETHODCALLTYPE *AppDomainShutdownStarted )( 
            ICorProfilerCallback5 * This,
            /* [in] */ AppDomainID appDomainId);
        
        HRESULT ( STDMETHODCALLTYPE *AppDomainShutdownFinished )( 
            ICorProfilerCallback5 * This,
            /* [in] */ AppDomainID appDomainId,
            /* [in] */ HRESULT hrStatus);
        
        HRESULT ( STDMETHODCALLTYPE *AssemblyLoadStarted )( 
            ICorProfilerCallback5 * This,
            /* [in] */ AssemblyID assemblyId);
        
        HRESULT ( STDMETHODCALLTYPE *AssemblyLoadFinished )( 
            ICorProfilerCallback5 * This,
            /* [in] */ AssemblyID assemblyId,
            /* [in] */ HRESULT hrStatus);
        
        HRESULT ( STDMETHODCALLTYPE *AssemblyUnloadStarted )( 
            ICorProfilerCallback5 * This,
            /* [in] */ AssemblyID assemblyId);
        
        HRESULT ( STDMETHODCALLTYPE *AssemblyUnloadFinished )( 
            ICorProfilerCallback5 * This,
            /* [in] */ AssemblyID assemblyId,
            /* [in] */ HRESULT hrStatus);
        
        HRESULT ( STDMETHODCALLTYPE *ModuleLoadStarted )( 
            ICorProfilerCallback5 * This,
            /* [in] */ ModuleID moduleId);
        
        HRESULT ( STDMETHODCALLTYPE *ModuleLoadFinished )( 
            ICorProfilerCallback5 * This,
            /* [in] */ ModuleID moduleId,
            /* [in] */ HRESULT hrStatus);
        
        HRESULT ( STDMETHODCALLTYPE *ModuleUnloadStarted )( 
            ICorProfilerCallback5 * This,
            /* [in] */ ModuleID moduleId);
        
        HRESULT ( STDMETHODCALLTYPE *ModuleUnloadFinished )( 
            ICorProfilerCallback5 * This,
            /* [in] */ ModuleID moduleId,
            /* [in] */ HRESULT hrStatus);
        
        HRESULT ( STDMETHODCALLTYPE *ModuleAttachedToAssembly )( 
            ICorProfilerCallback5 * This,
            /* [in] */ ModuleID moduleId,
            /* [in] */ AssemblyID AssemblyId);
        
        HRESULT ( STDMETHODCALLTYPE *ClassLoadStarted )( 
            ICorProfilerCallback5 * This,
            /* [in] */ ClassID classId);
        
        HRESULT ( STDMETHODCALLTYPE *ClassLoadFinished )( 
            ICorProfilerCallback5 * This,
            /* [in] */ ClassID classId,
            /* [in] */ HRESULT hrStatus);
        
        HRESULT ( STDMETHODCALLTYPE *ClassUnloadStarted )( 
            ICorProfilerCallback5 * This,
            /* [in] */ ClassID classId);
        
        HRESULT ( STDMETHODCALLTYPE *ClassUnloadFinished )( 
            ICorProfilerCallback5 * This,
            /* [in] */ ClassID classId,
            /* [in] */ HRESULT hrStatus);
        
        HRESULT ( STDMETHODCALLTYPE *FunctionUnloadStarted )( 
            ICorProfilerCallback5 * This,
            /* [in] */ FunctionID functionId);
        
        HRESULT ( STDMETHODCALLTYPE *JITCompilationStarted )( 
            ICorProfilerCallback5 * This,
            /* [in] */ FunctionID functionId,
            /* [in] */ BOOL fIsSafeToBlock);
        
        HRESULT ( STDMETHODCALLTYPE *JITCompilationFinished )( 
            ICorProfilerCallback5 * This,
            /* [in] */ FunctionID functionId,
            /* [in] */ HRESULT hrStatus,
            /* [in] */ BOOL fIsSafeToBlock);
        
        HRESULT ( STDMETHODCALLTYPE *JITCachedFunctionSearchStarted )( 
            ICorProfilerCallback5 * This,
            /* [in] */ FunctionID functionId,
            /* [out] */ BOOL *pbUseCachedFunction);
        
        HRESULT ( STDMETHODCALLTYPE *JITCachedFunctionSearchFinished )( 
            ICorProfilerCallback5 * This,
            /* [in] */ FunctionID functionId,
            /* [in] */ COR_PRF_JIT_CACHE result);
        
        HRESULT ( STDMETHODCALLTYPE *JITFunctionPitched )( 
            ICorProfilerCallback5 * This,
            /* [in] */ FunctionID functionId);
        
        HRESULT ( STDMETHODCALLTYPE *JITInlining )( 
            ICorProfilerCallback5 * This,
            /* [in] */ FunctionID callerId,
            /* [in] */ FunctionID calleeId,
            /* [out] */ BOOL *pfShouldInline);
        
        HRESULT ( STDMETHODCALLTYPE *ThreadCreated )( 
            ICorProfilerCallback5 * This,
            /* [in] */ ThreadID threadId);
        
        HRESULT ( STDMETHODCALLTYPE *ThreadDestroyed )( 
            ICorProfilerCallback5 * This,
            /* [in] */ ThreadID threadId);
        
        HRESULT ( STDMETHODCALLTYPE *ThreadAssignedToOSThread )( 
            ICorProfilerCallback5 * This,
            /* [in] */ ThreadID managedThreadId,
            /* [in] */ DWORD osThreadId);
        
        HRESULT ( STDMETHODCALLTYPE *RemotingClientInvocationStarted )( 
            ICorProfilerCallback5 * This);
        
        HRESULT ( STDMETHODCALLTYPE *RemotingClientSendingMessage )( 
            ICorProfilerCallback5 * This,
            /* [in] */ GUID *pCookie,
            /* [in] */ BOOL fIsAsync);
        
        HRESULT ( STDMETHODCALLTYPE *RemotingClientReceivingReply )( 
            ICorProfilerCallback5 * This,
            /* [in] */ GUID *pCookie,
            /* [in] */ BOOL fIsAsync);
        
        HRESULT ( STDMETHODCALLTYPE *RemotingClientInvocationFinished )( 
            ICorProfilerCallback5 * This);
        
        HRESULT ( STDMETHODCALLTYPE *RemotingServerReceivingMessage )( 
            ICorProfilerCallback5 * This,
            /* [in] */ GUID *pCookie,
            /* [in] */ BOOL fIsAsync);
        
        HRESULT ( STDMETHODCALLTYPE *RemotingServerInvocationStarted )( 
            ICorProfilerCallback5 * This);
        
        HRESULT ( STDMETHODCALLTYPE *RemotingServerInvocationReturned )( 
            ICorProfilerCallback5 * This);
        
        HRESULT ( STDMETHODCALLTYPE *RemotingServerSendingReply )( 
            ICorProfilerCallback5 * This,
            /* [in] */ GUID *pCookie,
            /* [in] */ BOOL fIsAsync);
        
        HRESULT ( STDMETHODCALLTYPE *UnmanagedToManagedTransition )( 
            ICorProfilerCallback5 * This,
            /* [in] */ FunctionID functionId,
            /* [in] */ COR_PRF_TRANSITION_REASON reason);
        
        HRESULT ( STDMETHODCALLTYPE *ManagedToUnmanagedTransition )( 
            ICorProfilerCallback5 * This,
            /* [in] */ FunctionID functionId,
            /* [in] */ COR_PRF_TRANSITION_REASON reason);
        
        HRESULT ( STDMETHODCALLTYPE *RuntimeSuspendStarted )( 
            ICorProfilerCallback5 * This,
            /* [in] */ COR_PRF_SUSPEND_REASON suspendReason);
        
        HRESULT ( STDMETHODCALLTYPE *RuntimeSuspendFinished )( 
            ICorProfilerCallback5 * This);
        
        HRESULT ( STDMETHODCALLTYPE *RuntimeSuspendAborted )( 
            ICorProfilerCallback5 * This);
        
        HRESULT ( STDMETHODCALLTYPE *RuntimeResumeStarted )( 
            ICorProfilerCallback5 * This);
        
        HRESULT ( STDMETHODCALLTYPE *RuntimeResumeFinished )( 
            ICorProfilerCallback5 * This);
        
        HRESULT ( STDMETHODCALLTYPE *RuntimeThreadSuspended )( 
            ICorProfilerCallback5 * This,
            /* [in] */ ThreadID threadId);
        
        HRESULT ( STDMETHODCALLTYPE *RuntimeThreadResumed )( 
            ICorProfilerCallback5 * This,
            /* [in] */ ThreadID threadId);
        
        HRESULT ( STDMETHODCALLTYPE *MovedReferences )( 
            ICorProfilerCallback5 * This,
            /* [in] */ ULONG cMovedObjectIDRanges,
            /* [size_is][in] */ ObjectID oldObjectIDRangeStart[  ],
            /* [size_is][in] */ ObjectID newObjectIDRangeStart[  ],
            /* [size_is][in] */ ULONG cObjectIDRangeLength[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *ObjectAllocated )( 
            ICorProfilerCallback5 * This,
            /* [in] */ ObjectID objectId,
            /* [in] */ ClassID classId);
        
        HRESULT ( STDMETHODCALLTYPE *ObjectsAllocatedByClass )( 
            ICorProfilerCallback5 * This,
            /* [in] */ ULONG cClassCount,
            /* [size_is][in] */ ClassID classIds[  ],
            /* [size_is][in] */ ULONG cObjects[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *ObjectReferences )( 
            ICorProfilerCallback5 * This,
            /* [in] */ ObjectID objectId,
            /* [in] */ ClassID classId,
            /* [in] */ ULONG cObjectRefs,
            /* [size_is][in] */ ObjectID objectRefIds[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *RootReferences )( 
            ICorProfilerCallback5 * This,
            /* [in] */ ULONG cRootRefs,
            /* [size_is][in] */ ObjectID rootRefIds[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *ExceptionThrown )( 
            ICorProfilerCallback5 * This,
            /* [in] */ ObjectID thrownObjectId);
        
        HRESULT ( STDMETHODCALLTYPE *ExceptionSearchFunctionEnter )( 
            ICorProfilerCallback5 * This,
            /* [in] */ FunctionID functionId);
        
        HRESULT ( STDMETHODCALLTYPE *ExceptionSearchFunctionLeave )( 
            ICorProfilerCallback5 * This);
        
        HRESULT ( STDMETHODCALLTYPE *ExceptionSearchFilterEnter )( 
            ICorProfilerCallback5 * This,
            /* [in] */ FunctionID functionId);
        
        HRESULT ( STDMETHODCALLTYPE *ExceptionSearchFilterLeave )( 
            ICorProfilerCallback5 * This);
        
        HRESULT ( STDMETHODCALLTYPE *ExceptionSearchCatcherFound )( 
            ICorProfilerCallback5 * This,
            /* [in] */ FunctionID functionId);
        
        HRESULT ( STDMETHODCALLTYPE *ExceptionOSHandlerEnter )( 
            ICorProfilerCallback5 * This,
            /* [in] */ UINT_PTR __unused);
        
        HRESULT ( STDMETHODCALLTYPE *ExceptionOSHandlerLeave )( 
            ICorProfilerCallback5 * This,
            /* [in] */ UINT_PTR __unused);
        
        HRESULT ( STDMETHODCALLTYPE *ExceptionUnwindFunctionEnter )( 
            ICorProfilerCallback5 * This,
            /* [in] */ FunctionID functionId);
        
        HRESULT ( STDMETHODCALLTYPE *ExceptionUnwindFunctionLeave )( 
            ICorProfilerCallback5 * This);
        
        HRESULT ( STDMETHODCALLTYPE *ExceptionUnwindFinallyEnter )( 
            ICorProfilerCallback5 * This,
            /* [in] */ FunctionID functionId);
        
        HRESULT ( STDMETHODCALLTYPE *ExceptionUnwindFinallyLeave )( 
            ICorProfilerCallback5 * This);
        
        HRESULT ( STDMETHODCALLTYPE *ExceptionCatcherEnter )( 
            ICorProfilerCallback5 * This,
            /* [in] */ FunctionID functionId,
            /* [in] */ ObjectID objectId);
        
        HRESULT ( STDMETHODCALLTYPE *ExceptionCatcherLeave )( 
            ICorProfilerCallback5 * This);
        
        HRESULT ( STDMETHODCALLTYPE *COMClassicVTableCreated )( 
            ICorProfilerCallback5 * This,
            /* [in] */ ClassID wrappedClassId,
            /* [in] */ REFGUID implementedIID,
            /* [in] */ void *pVTable,
            /* [in] */ ULONG cSlots);
        
        HRESULT ( STDMETHODCALLTYPE *COMClassicVTableDestroyed )( 
            ICorProfilerCallback5 * This,
            /* [in] */ ClassID wrappedClassId,
            /* [in] */ REFGUID implementedIID,
            /* [in] */ void *pVTable);
        
        HRESULT ( STDMETHODCALLTYPE *ExceptionCLRCatcherFound )( 
            ICorProfilerCallback5 * This);
        
        HRESULT ( STDMETHODCALLTYPE *ExceptionCLRCatcherExecute )( 
            ICorProfilerCallback5 * This);
        
        HRESULT ( STDMETHODCALLTYPE *ThreadNameChanged )( 
            ICorProfilerCallback5 * This,
            /* [in] */ ThreadID threadId,
            /* [in] */ ULONG cchName,
            /* [annotation][in] */ 
            _In_reads_opt_(cchName)  WCHAR name[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *GarbageCollectionStarted )( 
            ICorProfilerCallback5 * This,
            /* [in] */ int cGenerations,
            /* [size_is][in] */ BOOL generationCollected[  ],
            /* [in] */ COR_PRF_GC_REASON reason);
        
        HRESULT ( STDMETHODCALLTYPE *SurvivingReferences )( 
            ICorProfilerCallback5 * This,
            /* [in] */ ULONG cSurvivingObjectIDRanges,
            /* [size_is][in] */ ObjectID objectIDRangeStart[  ],
            /* [size_is][in] */ ULONG cObjectIDRangeLength[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *GarbageCollectionFinished )( 
            ICorProfilerCallback5 * This);
        
        HRESULT ( STDMETHODCALLTYPE *FinalizeableObjectQueued )( 
            ICorProfilerCallback5 * This,
            /* [in] */ DWORD finalizerFlags,
            /* [in] */ ObjectID objectID);
        
        HRESULT ( STDMETHODCALLTYPE *RootReferences2 )( 
            ICorProfilerCallback5 * This,
            /* [in] */ ULONG cRootRefs,
            /* [size_is][in] */ ObjectID rootRefIds[  ],
            /* [size_is][in] */ COR_PRF_GC_ROOT_KIND rootKinds[  ],
            /* [size_is][in] */ COR_PRF_GC_ROOT_FLAGS rootFlags[  ],
            /* [size_is][in] */ UINT_PTR rootIds[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *HandleCreated )( 
            ICorProfilerCallback5 * This,
            /* [in] */ GCHandleID handleId,
            /* [in] */ ObjectID initialObjectId);
        
        HRESULT ( STDMETHODCALLTYPE *HandleDestroyed )( 
            ICorProfilerCallback5 * This,
            /* [in] */ GCHandleID handleId);
        
        HRESULT ( STDMETHODCALLTYPE *InitializeForAttach )( 
            ICorProfilerCallback5 * This,
            /* [in] */ IUnknown *pCorProfilerInfoUnk,
            /* [in] */ void *pvClientData,
            /* [in] */ UINT cbClientData);
        
        HRESULT ( STDMETHODCALLTYPE *ProfilerAttachComplete )( 
            ICorProfilerCallback5 * This);
        
        HRESULT ( STDMETHODCALLTYPE *ProfilerDetachSucceeded )( 
            ICorProfilerCallback5 * This);
        
        HRESULT ( STDMETHODCALLTYPE *ReJITCompilationStarted )( 
            ICorProfilerCallback5 * This,
            /* [in] */ FunctionID functionId,
            /* [in] */ ReJITID rejitId,
            /* [in] */ BOOL fIsSafeToBlock);
        
        HRESULT ( STDMETHODCALLTYPE *GetReJITParameters )( 
            ICorProfilerCallback5 * This,
            /* [in] */ ModuleID moduleId,
            /* [in] */ mdMethodDef methodId,
            /* [in] */ ICorProfilerFunctionControl *pFunctionControl);
        
        HRESULT ( STDMETHODCALLTYPE *ReJITCompilationFinished )( 
            ICorProfilerCallback5 * This,
            /* [in] */ FunctionID functionId,
            /* [in] */ ReJITID rejitId,
            /* [in] */ HRESULT hrStatus,
            /* [in] */ BOOL fIsSafeToBlock);
        
        HRESULT ( STDMETHODCALLTYPE *ReJITError )( 
            ICorProfilerCallback5 * This,
            /* [in] */ ModuleID moduleId,
            /* [in] */ mdMethodDef methodId,
            /* [in] */ FunctionID functionId,
            /* [in] */ HRESULT hrStatus);
        
        HRESULT ( STDMETHODCALLTYPE *MovedReferences2 )( 
            ICorProfilerCallback5 * This,
            /* [in] */ ULONG cMovedObjectIDRanges,
            /* [size_is][in] */ ObjectID oldObjectIDRangeStart[  ],
            /* [size_is][in] */ ObjectID newObjectIDRangeStart[  ],
            /* [size_is][in] */ SIZE_T cObjectIDRangeLength[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *SurvivingReferences2 )( 
            ICorProfilerCallback5 * This,
            /* [in] */ ULONG cSurvivingObjectIDRanges,
            /* [size_is][in] */ ObjectID objectIDRangeStart[  ],
            /* [size_is][in] */ SIZE_T cObjectIDRangeLength[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *ConditionalWeakTableElementReferences )( 
            ICorProfilerCallback5 * This,
            /* [in] */ ULONG cRootRefs,
            /* [size_is][in] */ ObjectID keyRefIds[  ],
            /* [size_is][in] */ ObjectID valueRefIds[  ],
            /* [size_is][in] */ GCHandleID rootIds[  ]);
        
        END_INTERFACE
    } ICorProfilerCallback5Vtbl;

    interface ICorProfilerCallback5
    {
        CONST_VTBL struct ICorProfilerCallback5Vtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define ICorProfilerCallback5_QueryInterface(This,riid,ppvObject)   \
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define ICorProfilerCallback5_AddRef(This)  \
    ( (This)->lpVtbl -> AddRef(This) ) 

#define ICorProfilerCallback5_Release(This) \
    ( (This)->lpVtbl -> Release(This) ) 


#define ICorProfilerCallback5_Initialize(This,pICorProfilerInfoUnk) \
    ( (This)->lpVtbl -> Initialize(This,pICorProfilerInfoUnk) ) 

#define ICorProfilerCallback5_Shutdown(This)    \
    ( (This)->lpVtbl -> Shutdown(This) ) 

#define ICorProfilerCallback5_AppDomainCreationStarted(This,appDomainId)    \
    ( (This)->lpVtbl -> AppDomainCreationStarted(This,appDomainId) ) 

#define ICorProfilerCallback5_AppDomainCreationFinished(This,appDomainId,hrStatus)  \
    ( (This)->lpVtbl -> AppDomainCreationFinished(This,appDomainId,hrStatus) ) 

#define ICorProfilerCallback5_AppDomainShutdownStarted(This,appDomainId)    \
    ( (This)->lpVtbl -> AppDomainShutdownStarted(This,appDomainId) ) 

#define ICorProfilerCallback5_AppDomainShutdownFinished(This,appDomainId,hrStatus)  \
    ( (This)->lpVtbl -> AppDomainShutdownFinished(This,appDomainId,hrStatus) ) 

#define ICorProfilerCallback5_AssemblyLoadStarted(This,assemblyId)  \
    ( (This)->lpVtbl -> AssemblyLoadStarted(This,assemblyId) ) 

#define ICorProfilerCallback5_AssemblyLoadFinished(This,assemblyId,hrStatus)    \
    ( (This)->lpVtbl -> AssemblyLoadFinished(This,assemblyId,hrStatus) ) 

#define ICorProfilerCallback5_AssemblyUnloadStarted(This,assemblyId)    \
    ( (This)->lpVtbl -> AssemblyUnloadStarted(This,assemblyId) ) 

#define ICorProfilerCallback5_AssemblyUnloadFinished(This,assemblyId,hrStatus)  \
    ( (This)->lpVtbl -> AssemblyUnloadFinished(This,assemblyId,hrStatus) ) 

#define ICorProfilerCallback5_ModuleLoadStarted(This,moduleId)  \
    ( (This)->lpVtbl -> ModuleLoadStarted(This,moduleId) ) 

#define ICorProfilerCallback5_ModuleLoadFinished(This,moduleId,hrStatus)    \
    ( (This)->lpVtbl -> ModuleLoadFinished(This,moduleId,hrStatus) ) 

#define ICorProfilerCallback5_ModuleUnloadStarted(This,moduleId)    \
    ( (This)->lpVtbl -> ModuleUnloadStarted(This,moduleId) ) 

#define ICorProfilerCallback5_ModuleUnloadFinished(This,moduleId,hrStatus)  \
    ( (This)->lpVtbl -> ModuleUnloadFinished(This,moduleId,hrStatus) ) 

#define ICorProfilerCallback5_ModuleAttachedToAssembly(This,moduleId,AssemblyId)    \
    ( (This)->lpVtbl -> ModuleAttachedToAssembly(This,moduleId,AssemblyId) ) 

#define ICorProfilerCallback5_ClassLoadStarted(This,classId)    \
    ( (This)->lpVtbl -> ClassLoadStarted(This,classId) ) 

#define ICorProfilerCallback5_ClassLoadFinished(This,classId,hrStatus)  \
    ( (This)->lpVtbl -> ClassLoadFinished(This,classId,hrStatus) ) 

#define ICorProfilerCallback5_ClassUnloadStarted(This,classId)  \
    ( (This)->lpVtbl -> ClassUnloadStarted(This,classId) ) 

#define ICorProfilerCallback5_ClassUnloadFinished(This,classId,hrStatus)    \
    ( (This)->lpVtbl -> ClassUnloadFinished(This,classId,hrStatus) ) 

#define ICorProfilerCallback5_FunctionUnloadStarted(This,functionId)    \
    ( (This)->lpVtbl -> FunctionUnloadStarted(This,functionId) ) 

#define ICorProfilerCallback5_JITCompilationStarted(This,functionId,fIsSafeToBlock) \
    ( (This)->lpVtbl -> JITCompilationStarted(This,functionId,fIsSafeToBlock) ) 

#define ICorProfilerCallback5_JITCompilationFinished(This,functionId,hrStatus,fIsSafeToBlock)   \
    ( (This)->lpVtbl -> JITCompilationFinished(This,functionId,hrStatus,fIsSafeToBlock) ) 

#define ICorProfilerCallback5_JITCachedFunctionSearchStarted(This,functionId,pbUseCachedFunction)   \
    ( (This)->lpVtbl -> JITCachedFunctionSearchStarted(This,functionId,pbUseCachedFunction) ) 

#define ICorProfilerCallback5_JITCachedFunctionSearchFinished(This,functionId,result)   \
    ( (This)->lpVtbl -> JITCachedFunctionSearchFinished(This,functionId,result) ) 

#define ICorProfilerCallback5_JITFunctionPitched(This,functionId)   \
    ( (This)->lpVtbl -> JITFunctionPitched(This,functionId) ) 

#define ICorProfilerCallback5_JITInlining(This,callerId,calleeId,pfShouldInline)    \
    ( (This)->lpVtbl -> JITInlining(This,callerId,calleeId,pfShouldInline) ) 

#define ICorProfilerCallback5_ThreadCreated(This,threadId)  \
    ( (This)->lpVtbl -> ThreadCreated(This,threadId) ) 

#define ICorProfilerCallback5_ThreadDestroyed(This,threadId)    \
    ( (This)->lpVtbl -> ThreadDestroyed(This,threadId) ) 

#define ICorProfilerCallback5_ThreadAssignedToOSThread(This,managedThreadId,osThreadId) \
    ( (This)->lpVtbl -> ThreadAssignedToOSThread(This,managedThreadId,osThreadId) ) 

#define ICorProfilerCallback5_RemotingClientInvocationStarted(This) \
    ( (This)->lpVtbl -> RemotingClientInvocationStarted(This) ) 

#define ICorProfilerCallback5_RemotingClientSendingMessage(This,pCookie,fIsAsync)   \
    ( (This)->lpVtbl -> RemotingClientSendingMessage(This,pCookie,fIsAsync) ) 

#define ICorProfilerCallback5_RemotingClientReceivingReply(This,pCookie,fIsAsync)   \
    ( (This)->lpVtbl -> RemotingClientReceivingReply(This,pCookie,fIsAsync) ) 

#define ICorProfilerCallback5_RemotingClientInvocationFinished(This)    \
    ( (This)->lpVtbl -> RemotingClientInvocationFinished(This) ) 

#define ICorProfilerCallback5_RemotingServerReceivingMessage(This,pCookie,fIsAsync) \
    ( (This)->lpVtbl -> RemotingServerReceivingMessage(This,pCookie,fIsAsync) ) 

#define ICorProfilerCallback5_RemotingServerInvocationStarted(This) \
    ( (This)->lpVtbl -> RemotingServerInvocationStarted(This) ) 

#define ICorProfilerCallback5_RemotingServerInvocationReturned(This)    \
    ( (This)->lpVtbl -> RemotingServerInvocationReturned(This) ) 

#define ICorProfilerCallback5_RemotingServerSendingReply(This,pCookie,fIsAsync) \
    ( (This)->lpVtbl -> RemotingServerSendingReply(This,pCookie,fIsAsync) ) 

#define ICorProfilerCallback5_UnmanagedToManagedTransition(This,functionId,reason)  \
    ( (This)->lpVtbl -> UnmanagedToManagedTransition(This,functionId,reason) ) 

#define ICorProfilerCallback5_ManagedToUnmanagedTransition(This,functionId,reason)  \
    ( (This)->lpVtbl -> ManagedToUnmanagedTransition(This,functionId,reason) ) 

#define ICorProfilerCallback5_RuntimeSuspendStarted(This,suspendReason) \
    ( (This)->lpVtbl -> RuntimeSuspendStarted(This,suspendReason) ) 

#define ICorProfilerCallback5_RuntimeSuspendFinished(This)  \
    ( (This)->lpVtbl -> RuntimeSuspendFinished(This) ) 

#define ICorProfilerCallback5_RuntimeSuspendAborted(This)   \
    ( (This)->lpVtbl -> RuntimeSuspendAborted(This) ) 

#define ICorProfilerCallback5_RuntimeResumeStarted(This)    \
    ( (This)->lpVtbl -> RuntimeResumeStarted(This) ) 

#define ICorProfilerCallback5_RuntimeResumeFinished(This)   \
    ( (This)->lpVtbl -> RuntimeResumeFinished(This) ) 

#define ICorProfilerCallback5_RuntimeThreadSuspended(This,threadId) \
    ( (This)->lpVtbl -> RuntimeThreadSuspended(This,threadId) ) 

#define ICorProfilerCallback5_RuntimeThreadResumed(This,threadId)   \
    ( (This)->lpVtbl -> RuntimeThreadResumed(This,threadId) ) 

#define ICorProfilerCallback5_MovedReferences(This,cMovedObjectIDRanges,oldObjectIDRangeStart,newObjectIDRangeStart,cObjectIDRangeLength)   \
    ( (This)->lpVtbl -> MovedReferences(This,cMovedObjectIDRanges,oldObjectIDRangeStart,newObjectIDRangeStart,cObjectIDRangeLength) ) 

#define ICorProfilerCallback5_ObjectAllocated(This,objectId,classId)    \
    ( (This)->lpVtbl -> ObjectAllocated(This,objectId,classId) ) 

#define ICorProfilerCallback5_ObjectsAllocatedByClass(This,cClassCount,classIds,cObjects)   \
    ( (This)->lpVtbl -> ObjectsAllocatedByClass(This,cClassCount,classIds,cObjects) ) 

#define ICorProfilerCallback5_ObjectReferences(This,objectId,classId,cObjectRefs,objectRefIds)  \
    ( (This)->lpVtbl -> ObjectReferences(This,objectId,classId,cObjectRefs,objectRefIds) ) 

#define ICorProfilerCallback5_RootReferences(This,cRootRefs,rootRefIds) \
    ( (This)->lpVtbl -> RootReferences(This,cRootRefs,rootRefIds) ) 

#define ICorProfilerCallback5_ExceptionThrown(This,thrownObjectId)  \
    ( (This)->lpVtbl -> ExceptionThrown(This,thrownObjectId) ) 

#define ICorProfilerCallback5_ExceptionSearchFunctionEnter(This,functionId) \
    ( (This)->lpVtbl -> ExceptionSearchFunctionEnter(This,functionId) ) 

#define ICorProfilerCallback5_ExceptionSearchFunctionLeave(This)    \
    ( (This)->lpVtbl -> ExceptionSearchFunctionLeave(This) ) 

#define ICorProfilerCallback5_ExceptionSearchFilterEnter(This,functionId)   \
    ( (This)->lpVtbl -> ExceptionSearchFilterEnter(This,functionId) ) 

#define ICorProfilerCallback5_ExceptionSearchFilterLeave(This)  \
    ( (This)->lpVtbl -> ExceptionSearchFilterLeave(This) ) 

#define ICorProfilerCallback5_ExceptionSearchCatcherFound(This,functionId)  \
    ( (This)->lpVtbl -> ExceptionSearchCatcherFound(This,functionId) ) 

#define ICorProfilerCallback5_ExceptionOSHandlerEnter(This,__unused)    \
    ( (This)->lpVtbl -> ExceptionOSHandlerEnter(This,__unused) ) 

#define ICorProfilerCallback5_ExceptionOSHandlerLeave(This,__unused)    \
    ( (This)->lpVtbl -> ExceptionOSHandlerLeave(This,__unused) ) 

#define ICorProfilerCallback5_ExceptionUnwindFunctionEnter(This,functionId) \
    ( (This)->lpVtbl -> ExceptionUnwindFunctionEnter(This,functionId) ) 

#define ICorProfilerCallback5_ExceptionUnwindFunctionLeave(This)    \
    ( (This)->lpVtbl -> ExceptionUnwindFunctionLeave(This) ) 

#define ICorProfilerCallback5_ExceptionUnwindFinallyEnter(This,functionId)  \
    ( (This)->lpVtbl -> ExceptionUnwindFinallyEnter(This,functionId) ) 

#define ICorProfilerCallback5_ExceptionUnwindFinallyLeave(This) \
    ( (This)->lpVtbl -> ExceptionUnwindFinallyLeave(This) ) 

#define ICorProfilerCallback5_ExceptionCatcherEnter(This,functionId,objectId)   \
    ( (This)->lpVtbl -> ExceptionCatcherEnter(This,functionId,objectId) ) 

#define ICorProfilerCallback5_ExceptionCatcherLeave(This)   \
    ( (This)->lpVtbl -> ExceptionCatcherLeave(This) ) 

#define ICorProfilerCallback5_COMClassicVTableCreated(This,wrappedClassId,implementedIID,pVTable,cSlots)    \
    ( (This)->lpVtbl -> COMClassicVTableCreated(This,wrappedClassId,implementedIID,pVTable,cSlots) ) 

#define ICorProfilerCallback5_COMClassicVTableDestroyed(This,wrappedClassId,implementedIID,pVTable) \
    ( (This)->lpVtbl -> COMClassicVTableDestroyed(This,wrappedClassId,implementedIID,pVTable) ) 

#define ICorProfilerCallback5_ExceptionCLRCatcherFound(This)    \
    ( (This)->lpVtbl -> ExceptionCLRCatcherFound(This) ) 

#define ICorProfilerCallback5_ExceptionCLRCatcherExecute(This)  \
    ( (This)->lpVtbl -> ExceptionCLRCatcherExecute(This) ) 


#define ICorProfilerCallback5_ThreadNameChanged(This,threadId,cchName,name) \
    ( (This)->lpVtbl -> ThreadNameChanged(This,threadId,cchName,name) ) 

#define ICorProfilerCallback5_GarbageCollectionStarted(This,cGenerations,generationCollected,reason)    \
    ( (This)->lpVtbl -> GarbageCollectionStarted(This,cGenerations,generationCollected,reason) ) 

#define ICorProfilerCallback5_SurvivingReferences(This,cSurvivingObjectIDRanges,objectIDRangeStart,cObjectIDRangeLength)    \
    ( (This)->lpVtbl -> SurvivingReferences(This,cSurvivingObjectIDRanges,objectIDRangeStart,cObjectIDRangeLength) ) 

#define ICorProfilerCallback5_GarbageCollectionFinished(This)   \
    ( (This)->lpVtbl -> GarbageCollectionFinished(This) ) 

#define ICorProfilerCallback5_FinalizeableObjectQueued(This,finalizerFlags,objectID)    \
    ( (This)->lpVtbl -> FinalizeableObjectQueued(This,finalizerFlags,objectID) ) 

#define ICorProfilerCallback5_RootReferences2(This,cRootRefs,rootRefIds,rootKinds,rootFlags,rootIds)    \
    ( (This)->lpVtbl -> RootReferences2(This,cRootRefs,rootRefIds,rootKinds,rootFlags,rootIds) ) 

#define ICorProfilerCallback5_HandleCreated(This,handleId,initialObjectId)  \
    ( (This)->lpVtbl -> HandleCreated(This,handleId,initialObjectId) ) 

#define ICorProfilerCallback5_HandleDestroyed(This,handleId)    \
    ( (This)->lpVtbl -> HandleDestroyed(This,handleId) ) 


#define ICorProfilerCallback5_InitializeForAttach(This,pCorProfilerInfoUnk,pvClientData,cbClientData)   \
    ( (This)->lpVtbl -> InitializeForAttach(This,pCorProfilerInfoUnk,pvClientData,cbClientData) ) 

#define ICorProfilerCallback5_ProfilerAttachComplete(This)  \
    ( (This)->lpVtbl -> ProfilerAttachComplete(This) ) 

#define ICorProfilerCallback5_ProfilerDetachSucceeded(This) \
    ( (This)->lpVtbl -> ProfilerDetachSucceeded(This) ) 


#define ICorProfilerCallback5_ReJITCompilationStarted(This,functionId,rejitId,fIsSafeToBlock)   \
    ( (This)->lpVtbl -> ReJITCompilationStarted(This,functionId,rejitId,fIsSafeToBlock) ) 

#define ICorProfilerCallback5_GetReJITParameters(This,moduleId,methodId,pFunctionControl)   \
    ( (This)->lpVtbl -> GetReJITParameters(This,moduleId,methodId,pFunctionControl) ) 

#define ICorProfilerCallback5_ReJITCompilationFinished(This,functionId,rejitId,hrStatus,fIsSafeToBlock) \
    ( (This)->lpVtbl -> ReJITCompilationFinished(This,functionId,rejitId,hrStatus,fIsSafeToBlock) ) 

#define ICorProfilerCallback5_ReJITError(This,moduleId,methodId,functionId,hrStatus)    \
    ( (This)->lpVtbl -> ReJITError(This,moduleId,methodId,functionId,hrStatus) ) 

#define ICorProfilerCallback5_MovedReferences2(This,cMovedObjectIDRanges,oldObjectIDRangeStart,newObjectIDRangeStart,cObjectIDRangeLength)  \
    ( (This)->lpVtbl -> MovedReferences2(This,cMovedObjectIDRanges,oldObjectIDRangeStart,newObjectIDRangeStart,cObjectIDRangeLength) ) 

#define ICorProfilerCallback5_SurvivingReferences2(This,cSurvivingObjectIDRanges,objectIDRangeStart,cObjectIDRangeLength)   \
    ( (This)->lpVtbl -> SurvivingReferences2(This,cSurvivingObjectIDRanges,objectIDRangeStart,cObjectIDRangeLength) ) 


#define ICorProfilerCallback5_ConditionalWeakTableElementReferences(This,cRootRefs,keyRefIds,valueRefIds,rootIds)   \
    ( (This)->lpVtbl -> ConditionalWeakTableElementReferences(This,cRootRefs,keyRefIds,valueRefIds,rootIds) ) 

#endif /* COBJMACROS */


#endif  /* C style interface */




#endif  /* __ICorProfilerCallback5_INTERFACE_DEFINED__ */


#ifndef __ICorProfilerCallback6_INTERFACE_DEFINED__
#define __ICorProfilerCallback6_INTERFACE_DEFINED__

/* interface ICorProfilerCallback6 */
/* [local][unique][uuid][object] */ 


EXTERN_C const IID IID_ICorProfilerCallback6;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("FC13DF4B-4448-4F4F-950C-BA8D19D00C36")
    ICorProfilerCallback6 : public ICorProfilerCallback5
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE GetAssemblyReferences( 
            /* [string][in] */ const WCHAR *wszAssemblyPath,
            /* [in] */ ICorProfilerAssemblyReferenceProvider *pAsmRefProvider) = 0;
        
    };
    
    
#else   /* C style interface */

    typedef struct ICorProfilerCallback6Vtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            ICorProfilerCallback6 * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            ICorProfilerCallback6 * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            ICorProfilerCallback6 * This);
        
        HRESULT ( STDMETHODCALLTYPE *Initialize )( 
            ICorProfilerCallback6 * This,
            /* [in] */ IUnknown *pICorProfilerInfoUnk);
        
        HRESULT ( STDMETHODCALLTYPE *Shutdown )( 
            ICorProfilerCallback6 * This);
        
        HRESULT ( STDMETHODCALLTYPE *AppDomainCreationStarted )( 
            ICorProfilerCallback6 * This,
            /* [in] */ AppDomainID appDomainId);
        
        HRESULT ( STDMETHODCALLTYPE *AppDomainCreationFinished )( 
            ICorProfilerCallback6 * This,
            /* [in] */ AppDomainID appDomainId,
            /* [in] */ HRESULT hrStatus);
        
        HRESULT ( STDMETHODCALLTYPE *AppDomainShutdownStarted )( 
            ICorProfilerCallback6 * This,
            /* [in] */ AppDomainID appDomainId);
        
        HRESULT ( STDMETHODCALLTYPE *AppDomainShutdownFinished )( 
            ICorProfilerCallback6 * This,
            /* [in] */ AppDomainID appDomainId,
            /* [in] */ HRESULT hrStatus);
        
        HRESULT ( STDMETHODCALLTYPE *AssemblyLoadStarted )( 
            ICorProfilerCallback6 * This,
            /* [in] */ AssemblyID assemblyId);
        
        HRESULT ( STDMETHODCALLTYPE *AssemblyLoadFinished )( 
            ICorProfilerCallback6 * This,
            /* [in] */ AssemblyID assemblyId,
            /* [in] */ HRESULT hrStatus);
        
        HRESULT ( STDMETHODCALLTYPE *AssemblyUnloadStarted )( 
            ICorProfilerCallback6 * This,
            /* [in] */ AssemblyID assemblyId);
        
        HRESULT ( STDMETHODCALLTYPE *AssemblyUnloadFinished )( 
            ICorProfilerCallback6 * This,
            /* [in] */ AssemblyID assemblyId,
            /* [in] */ HRESULT hrStatus);
        
        HRESULT ( STDMETHODCALLTYPE *ModuleLoadStarted )( 
            ICorProfilerCallback6 * This,
            /* [in] */ ModuleID moduleId);
        
        HRESULT ( STDMETHODCALLTYPE *ModuleLoadFinished )( 
            ICorProfilerCallback6 * This,
            /* [in] */ ModuleID moduleId,
            /* [in] */ HRESULT hrStatus);
        
        HRESULT ( STDMETHODCALLTYPE *ModuleUnloadStarted )( 
            ICorProfilerCallback6 * This,
            /* [in] */ ModuleID moduleId);
        
        HRESULT ( STDMETHODCALLTYPE *ModuleUnloadFinished )( 
            ICorProfilerCallback6 * This,
            /* [in] */ ModuleID moduleId,
            /* [in] */ HRESULT hrStatus);
        
        HRESULT ( STDMETHODCALLTYPE *ModuleAttachedToAssembly )( 
            ICorProfilerCallback6 * This,
            /* [in] */ ModuleID moduleId,
            /* [in] */ AssemblyID AssemblyId);
        
        HRESULT ( STDMETHODCALLTYPE *ClassLoadStarted )( 
            ICorProfilerCallback6 * This,
            /* [in] */ ClassID classId);
        
        HRESULT ( STDMETHODCALLTYPE *ClassLoadFinished )( 
            ICorProfilerCallback6 * This,
            /* [in] */ ClassID classId,
            /* [in] */ HRESULT hrStatus);
        
        HRESULT ( STDMETHODCALLTYPE *ClassUnloadStarted )( 
            ICorProfilerCallback6 * This,
            /* [in] */ ClassID classId);
        
        HRESULT ( STDMETHODCALLTYPE *ClassUnloadFinished )( 
            ICorProfilerCallback6 * This,
            /* [in] */ ClassID classId,
            /* [in] */ HRESULT hrStatus);
        
        HRESULT ( STDMETHODCALLTYPE *FunctionUnloadStarted )( 
            ICorProfilerCallback6 * This,
            /* [in] */ FunctionID functionId);
        
        HRESULT ( STDMETHODCALLTYPE *JITCompilationStarted )( 
            ICorProfilerCallback6 * This,
            /* [in] */ FunctionID functionId,
            /* [in] */ BOOL fIsSafeToBlock);
        
        HRESULT ( STDMETHODCALLTYPE *JITCompilationFinished )( 
            ICorProfilerCallback6 * This,
            /* [in] */ FunctionID functionId,
            /* [in] */ HRESULT hrStatus,
            /* [in] */ BOOL fIsSafeToBlock);
        
        HRESULT ( STDMETHODCALLTYPE *JITCachedFunctionSearchStarted )( 
            ICorProfilerCallback6 * This,
            /* [in] */ FunctionID functionId,
            /* [out] */ BOOL *pbUseCachedFunction);
        
        HRESULT ( STDMETHODCALLTYPE *JITCachedFunctionSearchFinished )( 
            ICorProfilerCallback6 * This,
            /* [in] */ FunctionID functionId,
            /* [in] */ COR_PRF_JIT_CACHE result);
        
        HRESULT ( STDMETHODCALLTYPE *JITFunctionPitched )( 
            ICorProfilerCallback6 * This,
            /* [in] */ FunctionID functionId);
        
        HRESULT ( STDMETHODCALLTYPE *JITInlining )( 
            ICorProfilerCallback6 * This,
            /* [in] */ FunctionID callerId,
            /* [in] */ FunctionID calleeId,
            /* [out] */ BOOL *pfShouldInline);
        
        HRESULT ( STDMETHODCALLTYPE *ThreadCreated )( 
            ICorProfilerCallback6 * This,
            /* [in] */ ThreadID threadId);
        
        HRESULT ( STDMETHODCALLTYPE *ThreadDestroyed )( 
            ICorProfilerCallback6 * This,
            /* [in] */ ThreadID threadId);
        
        HRESULT ( STDMETHODCALLTYPE *ThreadAssignedToOSThread )( 
            ICorProfilerCallback6 * This,
            /* [in] */ ThreadID managedThreadId,
            /* [in] */ DWORD osThreadId);
        
        HRESULT ( STDMETHODCALLTYPE *RemotingClientInvocationStarted )( 
            ICorProfilerCallback6 * This);
        
        HRESULT ( STDMETHODCALLTYPE *RemotingClientSendingMessage )( 
            ICorProfilerCallback6 * This,
            /* [in] */ GUID *pCookie,
            /* [in] */ BOOL fIsAsync);
        
        HRESULT ( STDMETHODCALLTYPE *RemotingClientReceivingReply )( 
            ICorProfilerCallback6 * This,
            /* [in] */ GUID *pCookie,
            /* [in] */ BOOL fIsAsync);
        
        HRESULT ( STDMETHODCALLTYPE *RemotingClientInvocationFinished )( 
            ICorProfilerCallback6 * This);
        
        HRESULT ( STDMETHODCALLTYPE *RemotingServerReceivingMessage )( 
            ICorProfilerCallback6 * This,
            /* [in] */ GUID *pCookie,
            /* [in] */ BOOL fIsAsync);
        
        HRESULT ( STDMETHODCALLTYPE *RemotingServerInvocationStarted )( 
            ICorProfilerCallback6 * This);
        
        HRESULT ( STDMETHODCALLTYPE *RemotingServerInvocationReturned )( 
            ICorProfilerCallback6 * This);
        
        HRESULT ( STDMETHODCALLTYPE *RemotingServerSendingReply )( 
            ICorProfilerCallback6 * This,
            /* [in] */ GUID *pCookie,
            /* [in] */ BOOL fIsAsync);
        
        HRESULT ( STDMETHODCALLTYPE *UnmanagedToManagedTransition )( 
            ICorProfilerCallback6 * This,
            /* [in] */ FunctionID functionId,
            /* [in] */ COR_PRF_TRANSITION_REASON reason);
        
        HRESULT ( STDMETHODCALLTYPE *ManagedToUnmanagedTransition )( 
            ICorProfilerCallback6 * This,
            /* [in] */ FunctionID functionId,
            /* [in] */ COR_PRF_TRANSITION_REASON reason);
        
        HRESULT ( STDMETHODCALLTYPE *RuntimeSuspendStarted )( 
            ICorProfilerCallback6 * This,
            /* [in] */ COR_PRF_SUSPEND_REASON suspendReason);
        
        HRESULT ( STDMETHODCALLTYPE *RuntimeSuspendFinished )( 
            ICorProfilerCallback6 * This);
        
        HRESULT ( STDMETHODCALLTYPE *RuntimeSuspendAborted )( 
            ICorProfilerCallback6 * This);
        
        HRESULT ( STDMETHODCALLTYPE *RuntimeResumeStarted )( 
            ICorProfilerCallback6 * This);
        
        HRESULT ( STDMETHODCALLTYPE *RuntimeResumeFinished )( 
            ICorProfilerCallback6 * This);
        
        HRESULT ( STDMETHODCALLTYPE *RuntimeThreadSuspended )( 
            ICorProfilerCallback6 * This,
            /* [in] */ ThreadID threadId);
        
        HRESULT ( STDMETHODCALLTYPE *RuntimeThreadResumed )( 
            ICorProfilerCallback6 * This,
            /* [in] */ ThreadID threadId);
        
        HRESULT ( STDMETHODCALLTYPE *MovedReferences )( 
            ICorProfilerCallback6 * This,
            /* [in] */ ULONG cMovedObjectIDRanges,
            /* [size_is][in] */ ObjectID oldObjectIDRangeStart[  ],
            /* [size_is][in] */ ObjectID newObjectIDRangeStart[  ],
            /* [size_is][in] */ ULONG cObjectIDRangeLength[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *ObjectAllocated )( 
            ICorProfilerCallback6 * This,
            /* [in] */ ObjectID objectId,
            /* [in] */ ClassID classId);
        
        HRESULT ( STDMETHODCALLTYPE *ObjectsAllocatedByClass )( 
            ICorProfilerCallback6 * This,
            /* [in] */ ULONG cClassCount,
            /* [size_is][in] */ ClassID classIds[  ],
            /* [size_is][in] */ ULONG cObjects[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *ObjectReferences )( 
            ICorProfilerCallback6 * This,
            /* [in] */ ObjectID objectId,
            /* [in] */ ClassID classId,
            /* [in] */ ULONG cObjectRefs,
            /* [size_is][in] */ ObjectID objectRefIds[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *RootReferences )( 
            ICorProfilerCallback6 * This,
            /* [in] */ ULONG cRootRefs,
            /* [size_is][in] */ ObjectID rootRefIds[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *ExceptionThrown )( 
            ICorProfilerCallback6 * This,
            /* [in] */ ObjectID thrownObjectId);
        
        HRESULT ( STDMETHODCALLTYPE *ExceptionSearchFunctionEnter )( 
            ICorProfilerCallback6 * This,
            /* [in] */ FunctionID functionId);
        
        HRESULT ( STDMETHODCALLTYPE *ExceptionSearchFunctionLeave )( 
            ICorProfilerCallback6 * This);
        
        HRESULT ( STDMETHODCALLTYPE *ExceptionSearchFilterEnter )( 
            ICorProfilerCallback6 * This,
            /* [in] */ FunctionID functionId);
        
        HRESULT ( STDMETHODCALLTYPE *ExceptionSearchFilterLeave )( 
            ICorProfilerCallback6 * This);
        
        HRESULT ( STDMETHODCALLTYPE *ExceptionSearchCatcherFound )( 
            ICorProfilerCallback6 * This,
            /* [in] */ FunctionID functionId);
        
        HRESULT ( STDMETHODCALLTYPE *ExceptionOSHandlerEnter )( 
            ICorProfilerCallback6 * This,
            /* [in] */ UINT_PTR __unused);
        
        HRESULT ( STDMETHODCALLTYPE *ExceptionOSHandlerLeave )( 
            ICorProfilerCallback6 * This,
            /* [in] */ UINT_PTR __unused);
        
        HRESULT ( STDMETHODCALLTYPE *ExceptionUnwindFunctionEnter )( 
            ICorProfilerCallback6 * This,
            /* [in] */ FunctionID functionId);
        
        HRESULT ( STDMETHODCALLTYPE *ExceptionUnwindFunctionLeave )( 
            ICorProfilerCallback6 * This);
        
        HRESULT ( STDMETHODCALLTYPE *ExceptionUnwindFinallyEnter )( 
            ICorProfilerCallback6 * This,
            /* [in] */ FunctionID functionId);
        
        HRESULT ( STDMETHODCALLTYPE *ExceptionUnwindFinallyLeave )( 
            ICorProfilerCallback6 * This);
        
        HRESULT ( STDMETHODCALLTYPE *ExceptionCatcherEnter )( 
            ICorProfilerCallback6 * This,
            /* [in] */ FunctionID functionId,
            /* [in] */ ObjectID objectId);
        
        HRESULT ( STDMETHODCALLTYPE *ExceptionCatcherLeave )( 
            ICorProfilerCallback6 * This);
        
        HRESULT ( STDMETHODCALLTYPE *COMClassicVTableCreated )( 
            ICorProfilerCallback6 * This,
            /* [in] */ ClassID wrappedClassId,
            /* [in] */ REFGUID implementedIID,
            /* [in] */ void *pVTable,
            /* [in] */ ULONG cSlots);
        
        HRESULT ( STDMETHODCALLTYPE *COMClassicVTableDestroyed )( 
            ICorProfilerCallback6 * This,
            /* [in] */ ClassID wrappedClassId,
            /* [in] */ REFGUID implementedIID,
            /* [in] */ void *pVTable);
        
        HRESULT ( STDMETHODCALLTYPE *ExceptionCLRCatcherFound )( 
            ICorProfilerCallback6 * This);
        
        HRESULT ( STDMETHODCALLTYPE *ExceptionCLRCatcherExecute )( 
            ICorProfilerCallback6 * This);
        
        HRESULT ( STDMETHODCALLTYPE *ThreadNameChanged )( 
            ICorProfilerCallback6 * This,
            /* [in] */ ThreadID threadId,
            /* [in] */ ULONG cchName,
            /* [annotation][in] */ 
            _In_reads_opt_(cchName)  WCHAR name[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *GarbageCollectionStarted )( 
            ICorProfilerCallback6 * This,
            /* [in] */ int cGenerations,
            /* [size_is][in] */ BOOL generationCollected[  ],
            /* [in] */ COR_PRF_GC_REASON reason);
        
        HRESULT ( STDMETHODCALLTYPE *SurvivingReferences )( 
            ICorProfilerCallback6 * This,
            /* [in] */ ULONG cSurvivingObjectIDRanges,
            /* [size_is][in] */ ObjectID objectIDRangeStart[  ],
            /* [size_is][in] */ ULONG cObjectIDRangeLength[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *GarbageCollectionFinished )( 
            ICorProfilerCallback6 * This);
        
        HRESULT ( STDMETHODCALLTYPE *FinalizeableObjectQueued )( 
            ICorProfilerCallback6 * This,
            /* [in] */ DWORD finalizerFlags,
            /* [in] */ ObjectID objectID);
        
        HRESULT ( STDMETHODCALLTYPE *RootReferences2 )( 
            ICorProfilerCallback6 * This,
            /* [in] */ ULONG cRootRefs,
            /* [size_is][in] */ ObjectID rootRefIds[  ],
            /* [size_is][in] */ COR_PRF_GC_ROOT_KIND rootKinds[  ],
            /* [size_is][in] */ COR_PRF_GC_ROOT_FLAGS rootFlags[  ],
            /* [size_is][in] */ UINT_PTR rootIds[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *HandleCreated )( 
            ICorProfilerCallback6 * This,
            /* [in] */ GCHandleID handleId,
            /* [in] */ ObjectID initialObjectId);
        
        HRESULT ( STDMETHODCALLTYPE *HandleDestroyed )( 
            ICorProfilerCallback6 * This,
            /* [in] */ GCHandleID handleId);
        
        HRESULT ( STDMETHODCALLTYPE *InitializeForAttach )( 
            ICorProfilerCallback6 * This,
            /* [in] */ IUnknown *pCorProfilerInfoUnk,
            /* [in] */ void *pvClientData,
            /* [in] */ UINT cbClientData);
        
        HRESULT ( STDMETHODCALLTYPE *ProfilerAttachComplete )( 
            ICorProfilerCallback6 * This);
        
        HRESULT ( STDMETHODCALLTYPE *ProfilerDetachSucceeded )( 
            ICorProfilerCallback6 * This);
        
        HRESULT ( STDMETHODCALLTYPE *ReJITCompilationStarted )( 
            ICorProfilerCallback6 * This,
            /* [in] */ FunctionID functionId,
            /* [in] */ ReJITID rejitId,
            /* [in] */ BOOL fIsSafeToBlock);
        
        HRESULT ( STDMETHODCALLTYPE *GetReJITParameters )( 
            ICorProfilerCallback6 * This,
            /* [in] */ ModuleID moduleId,
            /* [in] */ mdMethodDef methodId,
            /* [in] */ ICorProfilerFunctionControl *pFunctionControl);
        
        HRESULT ( STDMETHODCALLTYPE *ReJITCompilationFinished )( 
            ICorProfilerCallback6 * This,
            /* [in] */ FunctionID functionId,
            /* [in] */ ReJITID rejitId,
            /* [in] */ HRESULT hrStatus,
            /* [in] */ BOOL fIsSafeToBlock);
        
        HRESULT ( STDMETHODCALLTYPE *ReJITError )( 
            ICorProfilerCallback6 * This,
            /* [in] */ ModuleID moduleId,
            /* [in] */ mdMethodDef methodId,
            /* [in] */ FunctionID functionId,
            /* [in] */ HRESULT hrStatus);
        
        HRESULT ( STDMETHODCALLTYPE *MovedReferences2 )( 
            ICorProfilerCallback6 * This,
            /* [in] */ ULONG cMovedObjectIDRanges,
            /* [size_is][in] */ ObjectID oldObjectIDRangeStart[  ],
            /* [size_is][in] */ ObjectID newObjectIDRangeStart[  ],
            /* [size_is][in] */ SIZE_T cObjectIDRangeLength[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *SurvivingReferences2 )( 
            ICorProfilerCallback6 * This,
            /* [in] */ ULONG cSurvivingObjectIDRanges,
            /* [size_is][in] */ ObjectID objectIDRangeStart[  ],
            /* [size_is][in] */ SIZE_T cObjectIDRangeLength[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *ConditionalWeakTableElementReferences )( 
            ICorProfilerCallback6 * This,
            /* [in] */ ULONG cRootRefs,
            /* [size_is][in] */ ObjectID keyRefIds[  ],
            /* [size_is][in] */ ObjectID valueRefIds[  ],
            /* [size_is][in] */ GCHandleID rootIds[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *GetAssemblyReferences )( 
            ICorProfilerCallback6 * This,
            /* [string][in] */ const WCHAR *wszAssemblyPath,
            /* [in] */ ICorProfilerAssemblyReferenceProvider *pAsmRefProvider);
        
        END_INTERFACE
    } ICorProfilerCallback6Vtbl;

    interface ICorProfilerCallback6
    {
        CONST_VTBL struct ICorProfilerCallback6Vtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define ICorProfilerCallback6_QueryInterface(This,riid,ppvObject)   \
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define ICorProfilerCallback6_AddRef(This)  \
    ( (This)->lpVtbl -> AddRef(This) ) 

#define ICorProfilerCallback6_Release(This) \
    ( (This)->lpVtbl -> Release(This) ) 


#define ICorProfilerCallback6_Initialize(This,pICorProfilerInfoUnk) \
    ( (This)->lpVtbl -> Initialize(This,pICorProfilerInfoUnk) ) 

#define ICorProfilerCallback6_Shutdown(This)    \
    ( (This)->lpVtbl -> Shutdown(This) ) 

#define ICorProfilerCallback6_AppDomainCreationStarted(This,appDomainId)    \
    ( (This)->lpVtbl -> AppDomainCreationStarted(This,appDomainId) ) 

#define ICorProfilerCallback6_AppDomainCreationFinished(This,appDomainId,hrStatus)  \
    ( (This)->lpVtbl -> AppDomainCreationFinished(This,appDomainId,hrStatus) ) 

#define ICorProfilerCallback6_AppDomainShutdownStarted(This,appDomainId)    \
    ( (This)->lpVtbl -> AppDomainShutdownStarted(This,appDomainId) ) 

#define ICorProfilerCallback6_AppDomainShutdownFinished(This,appDomainId,hrStatus)  \
    ( (This)->lpVtbl -> AppDomainShutdownFinished(This,appDomainId,hrStatus) ) 

#define ICorProfilerCallback6_AssemblyLoadStarted(This,assemblyId)  \
    ( (This)->lpVtbl -> AssemblyLoadStarted(This,assemblyId) ) 

#define ICorProfilerCallback6_AssemblyLoadFinished(This,assemblyId,hrStatus)    \
    ( (This)->lpVtbl -> AssemblyLoadFinished(This,assemblyId,hrStatus) ) 

#define ICorProfilerCallback6_AssemblyUnloadStarted(This,assemblyId)    \
    ( (This)->lpVtbl -> AssemblyUnloadStarted(This,assemblyId) ) 

#define ICorProfilerCallback6_AssemblyUnloadFinished(This,assemblyId,hrStatus)  \
    ( (This)->lpVtbl -> AssemblyUnloadFinished(This,assemblyId,hrStatus) ) 

#define ICorProfilerCallback6_ModuleLoadStarted(This,moduleId)  \
    ( (This)->lpVtbl -> ModuleLoadStarted(This,moduleId) ) 

#define ICorProfilerCallback6_ModuleLoadFinished(This,moduleId,hrStatus)    \
    ( (This)->lpVtbl -> ModuleLoadFinished(This,moduleId,hrStatus) ) 

#define ICorProfilerCallback6_ModuleUnloadStarted(This,moduleId)    \
    ( (This)->lpVtbl -> ModuleUnloadStarted(This,moduleId) ) 

#define ICorProfilerCallback6_ModuleUnloadFinished(This,moduleId,hrStatus)  \
    ( (This)->lpVtbl -> ModuleUnloadFinished(This,moduleId,hrStatus) ) 

#define ICorProfilerCallback6_ModuleAttachedToAssembly(This,moduleId,AssemblyId)    \
    ( (This)->lpVtbl -> ModuleAttachedToAssembly(This,moduleId,AssemblyId) ) 

#define ICorProfilerCallback6_ClassLoadStarted(This,classId)    \
    ( (This)->lpVtbl -> ClassLoadStarted(This,classId) ) 

#define ICorProfilerCallback6_ClassLoadFinished(This,classId,hrStatus)  \
    ( (This)->lpVtbl -> ClassLoadFinished(This,classId,hrStatus) ) 

#define ICorProfilerCallback6_ClassUnloadStarted(This,classId)  \
    ( (This)->lpVtbl -> ClassUnloadStarted(This,classId) ) 

#define ICorProfilerCallback6_ClassUnloadFinished(This,classId,hrStatus)    \
    ( (This)->lpVtbl -> ClassUnloadFinished(This,classId,hrStatus) ) 

#define ICorProfilerCallback6_FunctionUnloadStarted(This,functionId)    \
    ( (This)->lpVtbl -> FunctionUnloadStarted(This,functionId) ) 

#define ICorProfilerCallback6_JITCompilationStarted(This,functionId,fIsSafeToBlock) \
    ( (This)->lpVtbl -> JITCompilationStarted(This,functionId,fIsSafeToBlock) ) 

#define ICorProfilerCallback6_JITCompilationFinished(This,functionId,hrStatus,fIsSafeToBlock)   \
    ( (This)->lpVtbl -> JITCompilationFinished(This,functionId,hrStatus,fIsSafeToBlock) ) 

#define ICorProfilerCallback6_JITCachedFunctionSearchStarted(This,functionId,pbUseCachedFunction)   \
    ( (This)->lpVtbl -> JITCachedFunctionSearchStarted(This,functionId,pbUseCachedFunction) ) 

#define ICorProfilerCallback6_JITCachedFunctionSearchFinished(This,functionId,result)   \
    ( (This)->lpVtbl -> JITCachedFunctionSearchFinished(This,functionId,result) ) 

#define ICorProfilerCallback6_JITFunctionPitched(This,functionId)   \
    ( (This)->lpVtbl -> JITFunctionPitched(This,functionId) ) 

#define ICorProfilerCallback6_JITInlining(This,callerId,calleeId,pfShouldInline)    \
    ( (This)->lpVtbl -> JITInlining(This,callerId,calleeId,pfShouldInline) ) 

#define ICorProfilerCallback6_ThreadCreated(This,threadId)  \
    ( (This)->lpVtbl -> ThreadCreated(This,threadId) ) 

#define ICorProfilerCallback6_ThreadDestroyed(This,threadId)    \
    ( (This)->lpVtbl -> ThreadDestroyed(This,threadId) ) 

#define ICorProfilerCallback6_ThreadAssignedToOSThread(This,managedThreadId,osThreadId) \
    ( (This)->lpVtbl -> ThreadAssignedToOSThread(This,managedThreadId,osThreadId) ) 

#define ICorProfilerCallback6_RemotingClientInvocationStarted(This) \
    ( (This)->lpVtbl -> RemotingClientInvocationStarted(This) ) 

#define ICorProfilerCallback6_RemotingClientSendingMessage(This,pCookie,fIsAsync)   \
    ( (This)->lpVtbl -> RemotingClientSendingMessage(This,pCookie,fIsAsync) ) 

#define ICorProfilerCallback6_RemotingClientReceivingReply(This,pCookie,fIsAsync)   \
    ( (This)->lpVtbl -> RemotingClientReceivingReply(This,pCookie,fIsAsync) ) 

#define ICorProfilerCallback6_RemotingClientInvocationFinished(This)    \
    ( (This)->lpVtbl -> RemotingClientInvocationFinished(This) ) 

#define ICorProfilerCallback6_RemotingServerReceivingMessage(This,pCookie,fIsAsync) \
    ( (This)->lpVtbl -> RemotingServerReceivingMessage(This,pCookie,fIsAsync) ) 

#define ICorProfilerCallback6_RemotingServerInvocationStarted(This) \
    ( (This)->lpVtbl -> RemotingServerInvocationStarted(This) ) 

#define ICorProfilerCallback6_RemotingServerInvocationReturned(This)    \
    ( (This)->lpVtbl -> RemotingServerInvocationReturned(This) ) 

#define ICorProfilerCallback6_RemotingServerSendingReply(This,pCookie,fIsAsync) \
    ( (This)->lpVtbl -> RemotingServerSendingReply(This,pCookie,fIsAsync) ) 

#define ICorProfilerCallback6_UnmanagedToManagedTransition(This,functionId,reason)  \
    ( (This)->lpVtbl -> UnmanagedToManagedTransition(This,functionId,reason) ) 

#define ICorProfilerCallback6_ManagedToUnmanagedTransition(This,functionId,reason)  \
    ( (This)->lpVtbl -> ManagedToUnmanagedTransition(This,functionId,reason) ) 

#define ICorProfilerCallback6_RuntimeSuspendStarted(This,suspendReason) \
    ( (This)->lpVtbl -> RuntimeSuspendStarted(This,suspendReason) ) 

#define ICorProfilerCallback6_RuntimeSuspendFinished(This)  \
    ( (This)->lpVtbl -> RuntimeSuspendFinished(This) ) 

#define ICorProfilerCallback6_RuntimeSuspendAborted(This)   \
    ( (This)->lpVtbl -> RuntimeSuspendAborted(This) ) 

#define ICorProfilerCallback6_RuntimeResumeStarted(This)    \
    ( (This)->lpVtbl -> RuntimeResumeStarted(This) ) 

#define ICorProfilerCallback6_RuntimeResumeFinished(This)   \
    ( (This)->lpVtbl -> RuntimeResumeFinished(This) ) 

#define ICorProfilerCallback6_RuntimeThreadSuspended(This,threadId) \
    ( (This)->lpVtbl -> RuntimeThreadSuspended(This,threadId) ) 

#define ICorProfilerCallback6_RuntimeThreadResumed(This,threadId)   \
    ( (This)->lpVtbl -> RuntimeThreadResumed(This,threadId) ) 

#define ICorProfilerCallback6_MovedReferences(This,cMovedObjectIDRanges,oldObjectIDRangeStart,newObjectIDRangeStart,cObjectIDRangeLength)   \
    ( (This)->lpVtbl -> MovedReferences(This,cMovedObjectIDRanges,oldObjectIDRangeStart,newObjectIDRangeStart,cObjectIDRangeLength) ) 

#define ICorProfilerCallback6_ObjectAllocated(This,objectId,classId)    \
    ( (This)->lpVtbl -> ObjectAllocated(This,objectId,classId) ) 

#define ICorProfilerCallback6_ObjectsAllocatedByClass(This,cClassCount,classIds,cObjects)   \
    ( (This)->lpVtbl -> ObjectsAllocatedByClass(This,cClassCount,classIds,cObjects) ) 

#define ICorProfilerCallback6_ObjectReferences(This,objectId,classId,cObjectRefs,objectRefIds)  \
    ( (This)->lpVtbl -> ObjectReferences(This,objectId,classId,cObjectRefs,objectRefIds) ) 

#define ICorProfilerCallback6_RootReferences(This,cRootRefs,rootRefIds) \
    ( (This)->lpVtbl -> RootReferences(This,cRootRefs,rootRefIds) ) 

#define ICorProfilerCallback6_ExceptionThrown(This,thrownObjectId)  \
    ( (This)->lpVtbl -> ExceptionThrown(This,thrownObjectId) ) 

#define ICorProfilerCallback6_ExceptionSearchFunctionEnter(This,functionId) \
    ( (This)->lpVtbl -> ExceptionSearchFunctionEnter(This,functionId) ) 

#define ICorProfilerCallback6_ExceptionSearchFunctionLeave(This)    \
    ( (This)->lpVtbl -> ExceptionSearchFunctionLeave(This) ) 

#define ICorProfilerCallback6_ExceptionSearchFilterEnter(This,functionId)   \
    ( (This)->lpVtbl -> ExceptionSearchFilterEnter(This,functionId) ) 

#define ICorProfilerCallback6_ExceptionSearchFilterLeave(This)  \
    ( (This)->lpVtbl -> ExceptionSearchFilterLeave(This) ) 

#define ICorProfilerCallback6_ExceptionSearchCatcherFound(This,functionId)  \
    ( (This)->lpVtbl -> ExceptionSearchCatcherFound(This,functionId) ) 

#define ICorProfilerCallback6_ExceptionOSHandlerEnter(This,__unused)    \
    ( (This)->lpVtbl -> ExceptionOSHandlerEnter(This,__unused) ) 

#define ICorProfilerCallback6_ExceptionOSHandlerLeave(This,__unused)    \
    ( (This)->lpVtbl -> ExceptionOSHandlerLeave(This,__unused) ) 

#define ICorProfilerCallback6_ExceptionUnwindFunctionEnter(This,functionId) \
    ( (This)->lpVtbl -> ExceptionUnwindFunctionEnter(This,functionId) ) 

#define ICorProfilerCallback6_ExceptionUnwindFunctionLeave(This)    \
    ( (This)->lpVtbl -> ExceptionUnwindFunctionLeave(This) ) 

#define ICorProfilerCallback6_ExceptionUnwindFinallyEnter(This,functionId)  \
    ( (This)->lpVtbl -> ExceptionUnwindFinallyEnter(This,functionId) ) 

#define ICorProfilerCallback6_ExceptionUnwindFinallyLeave(This) \
    ( (This)->lpVtbl -> ExceptionUnwindFinallyLeave(This) ) 

#define ICorProfilerCallback6_ExceptionCatcherEnter(This,functionId,objectId)   \
    ( (This)->lpVtbl -> ExceptionCatcherEnter(This,functionId,objectId) ) 

#define ICorProfilerCallback6_ExceptionCatcherLeave(This)   \
    ( (This)->lpVtbl -> ExceptionCatcherLeave(This) ) 

#define ICorProfilerCallback6_COMClassicVTableCreated(This,wrappedClassId,implementedIID,pVTable,cSlots)    \
    ( (This)->lpVtbl -> COMClassicVTableCreated(This,wrappedClassId,implementedIID,pVTable,cSlots) ) 

#define ICorProfilerCallback6_COMClassicVTableDestroyed(This,wrappedClassId,implementedIID,pVTable) \
    ( (This)->lpVtbl -> COMClassicVTableDestroyed(This,wrappedClassId,implementedIID,pVTable) ) 

#define ICorProfilerCallback6_ExceptionCLRCatcherFound(This)    \
    ( (This)->lpVtbl -> ExceptionCLRCatcherFound(This) ) 

#define ICorProfilerCallback6_ExceptionCLRCatcherExecute(This)  \
    ( (This)->lpVtbl -> ExceptionCLRCatcherExecute(This) ) 


#define ICorProfilerCallback6_ThreadNameChanged(This,threadId,cchName,name) \
    ( (This)->lpVtbl -> ThreadNameChanged(This,threadId,cchName,name) ) 

#define ICorProfilerCallback6_GarbageCollectionStarted(This,cGenerations,generationCollected,reason)    \
    ( (This)->lpVtbl -> GarbageCollectionStarted(This,cGenerations,generationCollected,reason) ) 

#define ICorProfilerCallback6_SurvivingReferences(This,cSurvivingObjectIDRanges,objectIDRangeStart,cObjectIDRangeLength)    \
    ( (This)->lpVtbl -> SurvivingReferences(This,cSurvivingObjectIDRanges,objectIDRangeStart,cObjectIDRangeLength) ) 

#define ICorProfilerCallback6_GarbageCollectionFinished(This)   \
    ( (This)->lpVtbl -> GarbageCollectionFinished(This) ) 

#define ICorProfilerCallback6_FinalizeableObjectQueued(This,finalizerFlags,objectID)    \
    ( (This)->lpVtbl -> FinalizeableObjectQueued(This,finalizerFlags,objectID) ) 

#define ICorProfilerCallback6_RootReferences2(This,cRootRefs,rootRefIds,rootKinds,rootFlags,rootIds)    \
    ( (This)->lpVtbl -> RootReferences2(This,cRootRefs,rootRefIds,rootKinds,rootFlags,rootIds) ) 

#define ICorProfilerCallback6_HandleCreated(This,handleId,initialObjectId)  \
    ( (This)->lpVtbl -> HandleCreated(This,handleId,initialObjectId) ) 

#define ICorProfilerCallback6_HandleDestroyed(This,handleId)    \
    ( (This)->lpVtbl -> HandleDestroyed(This,handleId) ) 


#define ICorProfilerCallback6_InitializeForAttach(This,pCorProfilerInfoUnk,pvClientData,cbClientData)   \
    ( (This)->lpVtbl -> InitializeForAttach(This,pCorProfilerInfoUnk,pvClientData,cbClientData) ) 

#define ICorProfilerCallback6_ProfilerAttachComplete(This)  \
    ( (This)->lpVtbl -> ProfilerAttachComplete(This) ) 

#define ICorProfilerCallback6_ProfilerDetachSucceeded(This) \
    ( (This)->lpVtbl -> ProfilerDetachSucceeded(This) ) 


#define ICorProfilerCallback6_ReJITCompilationStarted(This,functionId,rejitId,fIsSafeToBlock)   \
    ( (This)->lpVtbl -> ReJITCompilationStarted(This,functionId,rejitId,fIsSafeToBlock) ) 

#define ICorProfilerCallback6_GetReJITParameters(This,moduleId,methodId,pFunctionControl)   \
    ( (This)->lpVtbl -> GetReJITParameters(This,moduleId,methodId,pFunctionControl) ) 

#define ICorProfilerCallback6_ReJITCompilationFinished(This,functionId,rejitId,hrStatus,fIsSafeToBlock) \
    ( (This)->lpVtbl -> ReJITCompilationFinished(This,functionId,rejitId,hrStatus,fIsSafeToBlock) ) 

#define ICorProfilerCallback6_ReJITError(This,moduleId,methodId,functionId,hrStatus)    \
    ( (This)->lpVtbl -> ReJITError(This,moduleId,methodId,functionId,hrStatus) ) 

#define ICorProfilerCallback6_MovedReferences2(This,cMovedObjectIDRanges,oldObjectIDRangeStart,newObjectIDRangeStart,cObjectIDRangeLength)  \
    ( (This)->lpVtbl -> MovedReferences2(This,cMovedObjectIDRanges,oldObjectIDRangeStart,newObjectIDRangeStart,cObjectIDRangeLength) ) 

#define ICorProfilerCallback6_SurvivingReferences2(This,cSurvivingObjectIDRanges,objectIDRangeStart,cObjectIDRangeLength)   \
    ( (This)->lpVtbl -> SurvivingReferences2(This,cSurvivingObjectIDRanges,objectIDRangeStart,cObjectIDRangeLength) ) 


#define ICorProfilerCallback6_ConditionalWeakTableElementReferences(This,cRootRefs,keyRefIds,valueRefIds,rootIds)   \
    ( (This)->lpVtbl -> ConditionalWeakTableElementReferences(This,cRootRefs,keyRefIds,valueRefIds,rootIds) ) 


#define ICorProfilerCallback6_GetAssemblyReferences(This,wszAssemblyPath,pAsmRefProvider)   \
    ( (This)->lpVtbl -> GetAssemblyReferences(This,wszAssemblyPath,pAsmRefProvider) ) 

#endif /* COBJMACROS */


#endif  /* C style interface */




#endif  /* __ICorProfilerCallback6_INTERFACE_DEFINED__ */


#ifndef __ICorProfilerCallback7_INTERFACE_DEFINED__
#define __ICorProfilerCallback7_INTERFACE_DEFINED__

/* interface ICorProfilerCallback7 */
/* [local][unique][uuid][object] */ 


EXTERN_C const IID IID_ICorProfilerCallback7;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("F76A2DBA-1D52-4539-866C-2AA518F9EFC3")
    ICorProfilerCallback7 : public ICorProfilerCallback6
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE ModuleInMemorySymbolsUpdated( 
            ModuleID moduleId) = 0;
        
    };
    
    
#else   /* C style interface */

    typedef struct ICorProfilerCallback7Vtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            ICorProfilerCallback7 * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            ICorProfilerCallback7 * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            ICorProfilerCallback7 * This);
        
        HRESULT ( STDMETHODCALLTYPE *Initialize )( 
            ICorProfilerCallback7 * This,
            /* [in] */ IUnknown *pICorProfilerInfoUnk);
        
        HRESULT ( STDMETHODCALLTYPE *Shutdown )( 
            ICorProfilerCallback7 * This);
        
        HRESULT ( STDMETHODCALLTYPE *AppDomainCreationStarted )( 
            ICorProfilerCallback7 * This,
            /* [in] */ AppDomainID appDomainId);
        
        HRESULT ( STDMETHODCALLTYPE *AppDomainCreationFinished )( 
            ICorProfilerCallback7 * This,
            /* [in] */ AppDomainID appDomainId,
            /* [in] */ HRESULT hrStatus);
        
        HRESULT ( STDMETHODCALLTYPE *AppDomainShutdownStarted )( 
            ICorProfilerCallback7 * This,
            /* [in] */ AppDomainID appDomainId);
        
        HRESULT ( STDMETHODCALLTYPE *AppDomainShutdownFinished )( 
            ICorProfilerCallback7 * This,
            /* [in] */ AppDomainID appDomainId,
            /* [in] */ HRESULT hrStatus);
        
        HRESULT ( STDMETHODCALLTYPE *AssemblyLoadStarted )( 
            ICorProfilerCallback7 * This,
            /* [in] */ AssemblyID assemblyId);
        
        HRESULT ( STDMETHODCALLTYPE *AssemblyLoadFinished )( 
            ICorProfilerCallback7 * This,
            /* [in] */ AssemblyID assemblyId,
            /* [in] */ HRESULT hrStatus);
        
        HRESULT ( STDMETHODCALLTYPE *AssemblyUnloadStarted )( 
            ICorProfilerCallback7 * This,
            /* [in] */ AssemblyID assemblyId);
        
        HRESULT ( STDMETHODCALLTYPE *AssemblyUnloadFinished )( 
            ICorProfilerCallback7 * This,
            /* [in] */ AssemblyID assemblyId,
            /* [in] */ HRESULT hrStatus);
        
        HRESULT ( STDMETHODCALLTYPE *ModuleLoadStarted )( 
            ICorProfilerCallback7 * This,
            /* [in] */ ModuleID moduleId);
        
        HRESULT ( STDMETHODCALLTYPE *ModuleLoadFinished )( 
            ICorProfilerCallback7 * This,
            /* [in] */ ModuleID moduleId,
            /* [in] */ HRESULT hrStatus);
        
        HRESULT ( STDMETHODCALLTYPE *ModuleUnloadStarted )( 
            ICorProfilerCallback7 * This,
            /* [in] */ ModuleID moduleId);
        
        HRESULT ( STDMETHODCALLTYPE *ModuleUnloadFinished )( 
            ICorProfilerCallback7 * This,
            /* [in] */ ModuleID moduleId,
            /* [in] */ HRESULT hrStatus);
        
        HRESULT ( STDMETHODCALLTYPE *ModuleAttachedToAssembly )( 
            ICorProfilerCallback7 * This,
            /* [in] */ ModuleID moduleId,
            /* [in] */ AssemblyID AssemblyId);
        
        HRESULT ( STDMETHODCALLTYPE *ClassLoadStarted )( 
            ICorProfilerCallback7 * This,
            /* [in] */ ClassID classId);
        
        HRESULT ( STDMETHODCALLTYPE *ClassLoadFinished )( 
            ICorProfilerCallback7 * This,
            /* [in] */ ClassID classId,
            /* [in] */ HRESULT hrStatus);
        
        HRESULT ( STDMETHODCALLTYPE *ClassUnloadStarted )( 
            ICorProfilerCallback7 * This,
            /* [in] */ ClassID classId);
        
        HRESULT ( STDMETHODCALLTYPE *ClassUnloadFinished )( 
            ICorProfilerCallback7 * This,
            /* [in] */ ClassID classId,
            /* [in] */ HRESULT hrStatus);
        
        HRESULT ( STDMETHODCALLTYPE *FunctionUnloadStarted )( 
            ICorProfilerCallback7 * This,
            /* [in] */ FunctionID functionId);
        
        HRESULT ( STDMETHODCALLTYPE *JITCompilationStarted )( 
            ICorProfilerCallback7 * This,
            /* [in] */ FunctionID functionId,
            /* [in] */ BOOL fIsSafeToBlock);
        
        HRESULT ( STDMETHODCALLTYPE *JITCompilationFinished )( 
            ICorProfilerCallback7 * This,
            /* [in] */ FunctionID functionId,
            /* [in] */ HRESULT hrStatus,
            /* [in] */ BOOL fIsSafeToBlock);
        
        HRESULT ( STDMETHODCALLTYPE *JITCachedFunctionSearchStarted )( 
            ICorProfilerCallback7 * This,
            /* [in] */ FunctionID functionId,
            /* [out] */ BOOL *pbUseCachedFunction);
        
        HRESULT ( STDMETHODCALLTYPE *JITCachedFunctionSearchFinished )( 
            ICorProfilerCallback7 * This,
            /* [in] */ FunctionID functionId,
            /* [in] */ COR_PRF_JIT_CACHE result);
        
        HRESULT ( STDMETHODCALLTYPE *JITFunctionPitched )( 
            ICorProfilerCallback7 * This,
            /* [in] */ FunctionID functionId);
        
        HRESULT ( STDMETHODCALLTYPE *JITInlining )( 
            ICorProfilerCallback7 * This,
            /* [in] */ FunctionID callerId,
            /* [in] */ FunctionID calleeId,
            /* [out] */ BOOL *pfShouldInline);
        
        HRESULT ( STDMETHODCALLTYPE *ThreadCreated )( 
            ICorProfilerCallback7 * This,
            /* [in] */ ThreadID threadId);
        
        HRESULT ( STDMETHODCALLTYPE *ThreadDestroyed )( 
            ICorProfilerCallback7 * This,
            /* [in] */ ThreadID threadId);
        
        HRESULT ( STDMETHODCALLTYPE *ThreadAssignedToOSThread )( 
            ICorProfilerCallback7 * This,
            /* [in] */ ThreadID managedThreadId,
            /* [in] */ DWORD osThreadId);
        
        HRESULT ( STDMETHODCALLTYPE *RemotingClientInvocationStarted )( 
            ICorProfilerCallback7 * This);
        
        HRESULT ( STDMETHODCALLTYPE *RemotingClientSendingMessage )( 
            ICorProfilerCallback7 * This,
            /* [in] */ GUID *pCookie,
            /* [in] */ BOOL fIsAsync);
        
        HRESULT ( STDMETHODCALLTYPE *RemotingClientReceivingReply )( 
            ICorProfilerCallback7 * This,
            /* [in] */ GUID *pCookie,
            /* [in] */ BOOL fIsAsync);
        
        HRESULT ( STDMETHODCALLTYPE *RemotingClientInvocationFinished )( 
            ICorProfilerCallback7 * This);
        
        HRESULT ( STDMETHODCALLTYPE *RemotingServerReceivingMessage )( 
            ICorProfilerCallback7 * This,
            /* [in] */ GUID *pCookie,
            /* [in] */ BOOL fIsAsync);
        
        HRESULT ( STDMETHODCALLTYPE *RemotingServerInvocationStarted )( 
            ICorProfilerCallback7 * This);
        
        HRESULT ( STDMETHODCALLTYPE *RemotingServerInvocationReturned )( 
            ICorProfilerCallback7 * This);
        
        HRESULT ( STDMETHODCALLTYPE *RemotingServerSendingReply )( 
            ICorProfilerCallback7 * This,
            /* [in] */ GUID *pCookie,
            /* [in] */ BOOL fIsAsync);
        
        HRESULT ( STDMETHODCALLTYPE *UnmanagedToManagedTransition )( 
            ICorProfilerCallback7 * This,
            /* [in] */ FunctionID functionId,
            /* [in] */ COR_PRF_TRANSITION_REASON reason);
        
        HRESULT ( STDMETHODCALLTYPE *ManagedToUnmanagedTransition )( 
            ICorProfilerCallback7 * This,
            /* [in] */ FunctionID functionId,
            /* [in] */ COR_PRF_TRANSITION_REASON reason);
        
        HRESULT ( STDMETHODCALLTYPE *RuntimeSuspendStarted )( 
            ICorProfilerCallback7 * This,
            /* [in] */ COR_PRF_SUSPEND_REASON suspendReason);
        
        HRESULT ( STDMETHODCALLTYPE *RuntimeSuspendFinished )( 
            ICorProfilerCallback7 * This);
        
        HRESULT ( STDMETHODCALLTYPE *RuntimeSuspendAborted )( 
            ICorProfilerCallback7 * This);
        
        HRESULT ( STDMETHODCALLTYPE *RuntimeResumeStarted )( 
            ICorProfilerCallback7 * This);
        
        HRESULT ( STDMETHODCALLTYPE *RuntimeResumeFinished )( 
            ICorProfilerCallback7 * This);
        
        HRESULT ( STDMETHODCALLTYPE *RuntimeThreadSuspended )( 
            ICorProfilerCallback7 * This,
            /* [in] */ ThreadID threadId);
        
        HRESULT ( STDMETHODCALLTYPE *RuntimeThreadResumed )( 
            ICorProfilerCallback7 * This,
            /* [in] */ ThreadID threadId);
        
        HRESULT ( STDMETHODCALLTYPE *MovedReferences )( 
            ICorProfilerCallback7 * This,
            /* [in] */ ULONG cMovedObjectIDRanges,
            /* [size_is][in] */ ObjectID oldObjectIDRangeStart[  ],
            /* [size_is][in] */ ObjectID newObjectIDRangeStart[  ],
            /* [size_is][in] */ ULONG cObjectIDRangeLength[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *ObjectAllocated )( 
            ICorProfilerCallback7 * This,
            /* [in] */ ObjectID objectId,
            /* [in] */ ClassID classId);
        
        HRESULT ( STDMETHODCALLTYPE *ObjectsAllocatedByClass )( 
            ICorProfilerCallback7 * This,
            /* [in] */ ULONG cClassCount,
            /* [size_is][in] */ ClassID classIds[  ],
            /* [size_is][in] */ ULONG cObjects[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *ObjectReferences )( 
            ICorProfilerCallback7 * This,
            /* [in] */ ObjectID objectId,
            /* [in] */ ClassID classId,
            /* [in] */ ULONG cObjectRefs,
            /* [size_is][in] */ ObjectID objectRefIds[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *RootReferences )( 
            ICorProfilerCallback7 * This,
            /* [in] */ ULONG cRootRefs,
            /* [size_is][in] */ ObjectID rootRefIds[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *ExceptionThrown )( 
            ICorProfilerCallback7 * This,
            /* [in] */ ObjectID thrownObjectId);
        
        HRESULT ( STDMETHODCALLTYPE *ExceptionSearchFunctionEnter )( 
            ICorProfilerCallback7 * This,
            /* [in] */ FunctionID functionId);
        
        HRESULT ( STDMETHODCALLTYPE *ExceptionSearchFunctionLeave )( 
            ICorProfilerCallback7 * This);
        
        HRESULT ( STDMETHODCALLTYPE *ExceptionSearchFilterEnter )( 
            ICorProfilerCallback7 * This,
            /* [in] */ FunctionID functionId);
        
        HRESULT ( STDMETHODCALLTYPE *ExceptionSearchFilterLeave )( 
            ICorProfilerCallback7 * This);
        
        HRESULT ( STDMETHODCALLTYPE *ExceptionSearchCatcherFound )( 
            ICorProfilerCallback7 * This,
            /* [in] */ FunctionID functionId);
        
        HRESULT ( STDMETHODCALLTYPE *ExceptionOSHandlerEnter )( 
            ICorProfilerCallback7 * This,
            /* [in] */ UINT_PTR __unused);
        
        HRESULT ( STDMETHODCALLTYPE *ExceptionOSHandlerLeave )( 
            ICorProfilerCallback7 * This,
            /* [in] */ UINT_PTR __unused);
        
        HRESULT ( STDMETHODCALLTYPE *ExceptionUnwindFunctionEnter )( 
            ICorProfilerCallback7 * This,
            /* [in] */ FunctionID functionId);
        
        HRESULT ( STDMETHODCALLTYPE *ExceptionUnwindFunctionLeave )( 
            ICorProfilerCallback7 * This);
        
        HRESULT ( STDMETHODCALLTYPE *ExceptionUnwindFinallyEnter )( 
            ICorProfilerCallback7 * This,
            /* [in] */ FunctionID functionId);
        
        HRESULT ( STDMETHODCALLTYPE *ExceptionUnwindFinallyLeave )( 
            ICorProfilerCallback7 * This);
        
        HRESULT ( STDMETHODCALLTYPE *ExceptionCatcherEnter )( 
            ICorProfilerCallback7 * This,
            /* [in] */ FunctionID functionId,
            /* [in] */ ObjectID objectId);
        
        HRESULT ( STDMETHODCALLTYPE *ExceptionCatcherLeave )( 
            ICorProfilerCallback7 * This);
        
        HRESULT ( STDMETHODCALLTYPE *COMClassicVTableCreated )( 
            ICorProfilerCallback7 * This,
            /* [in] */ ClassID wrappedClassId,
            /* [in] */ REFGUID implementedIID,
            /* [in] */ void *pVTable,
            /* [in] */ ULONG cSlots);
        
        HRESULT ( STDMETHODCALLTYPE *COMClassicVTableDestroyed )( 
            ICorProfilerCallback7 * This,
            /* [in] */ ClassID wrappedClassId,
            /* [in] */ REFGUID implementedIID,
            /* [in] */ void *pVTable);
        
        HRESULT ( STDMETHODCALLTYPE *ExceptionCLRCatcherFound )( 
            ICorProfilerCallback7 * This);
        
        HRESULT ( STDMETHODCALLTYPE *ExceptionCLRCatcherExecute )( 
            ICorProfilerCallback7 * This);
        
        HRESULT ( STDMETHODCALLTYPE *ThreadNameChanged )( 
            ICorProfilerCallback7 * This,
            /* [in] */ ThreadID threadId,
            /* [in] */ ULONG cchName,
            /* [annotation][in] */ 
            _In_reads_opt_(cchName)  WCHAR name[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *GarbageCollectionStarted )( 
            ICorProfilerCallback7 * This,
            /* [in] */ int cGenerations,
            /* [size_is][in] */ BOOL generationCollected[  ],
            /* [in] */ COR_PRF_GC_REASON reason);
        
        HRESULT ( STDMETHODCALLTYPE *SurvivingReferences )( 
            ICorProfilerCallback7 * This,
            /* [in] */ ULONG cSurvivingObjectIDRanges,
            /* [size_is][in] */ ObjectID objectIDRangeStart[  ],
            /* [size_is][in] */ ULONG cObjectIDRangeLength[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *GarbageCollectionFinished )( 
            ICorProfilerCallback7 * This);
        
        HRESULT ( STDMETHODCALLTYPE *FinalizeableObjectQueued )( 
            ICorProfilerCallback7 * This,
            /* [in] */ DWORD finalizerFlags,
            /* [in] */ ObjectID objectID);
        
        HRESULT ( STDMETHODCALLTYPE *RootReferences2 )( 
            ICorProfilerCallback7 * This,
            /* [in] */ ULONG cRootRefs,
            /* [size_is][in] */ ObjectID rootRefIds[  ],
            /* [size_is][in] */ COR_PRF_GC_ROOT_KIND rootKinds[  ],
            /* [size_is][in] */ COR_PRF_GC_ROOT_FLAGS rootFlags[  ],
            /* [size_is][in] */ UINT_PTR rootIds[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *HandleCreated )( 
            ICorProfilerCallback7 * This,
            /* [in] */ GCHandleID handleId,
            /* [in] */ ObjectID initialObjectId);
        
        HRESULT ( STDMETHODCALLTYPE *HandleDestroyed )( 
            ICorProfilerCallback7 * This,
            /* [in] */ GCHandleID handleId);
        
        HRESULT ( STDMETHODCALLTYPE *InitializeForAttach )( 
            ICorProfilerCallback7 * This,
            /* [in] */ IUnknown *pCorProfilerInfoUnk,
            /* [in] */ void *pvClientData,
            /* [in] */ UINT cbClientData);
        
        HRESULT ( STDMETHODCALLTYPE *ProfilerAttachComplete )( 
            ICorProfilerCallback7 * This);
        
        HRESULT ( STDMETHODCALLTYPE *ProfilerDetachSucceeded )( 
            ICorProfilerCallback7 * This);
        
        HRESULT ( STDMETHODCALLTYPE *ReJITCompilationStarted )( 
            ICorProfilerCallback7 * This,
            /* [in] */ FunctionID functionId,
            /* [in] */ ReJITID rejitId,
            /* [in] */ BOOL fIsSafeToBlock);
        
        HRESULT ( STDMETHODCALLTYPE *GetReJITParameters )( 
            ICorProfilerCallback7 * This,
            /* [in] */ ModuleID moduleId,
            /* [in] */ mdMethodDef methodId,
            /* [in] */ ICorProfilerFunctionControl *pFunctionControl);
        
        HRESULT ( STDMETHODCALLTYPE *ReJITCompilationFinished )( 
            ICorProfilerCallback7 * This,
            /* [in] */ FunctionID functionId,
            /* [in] */ ReJITID rejitId,
            /* [in] */ HRESULT hrStatus,
            /* [in] */ BOOL fIsSafeToBlock);
        
        HRESULT ( STDMETHODCALLTYPE *ReJITError )( 
            ICorProfilerCallback7 * This,
            /* [in] */ ModuleID moduleId,
            /* [in] */ mdMethodDef methodId,
            /* [in] */ FunctionID functionId,
            /* [in] */ HRESULT hrStatus);
        
        HRESULT ( STDMETHODCALLTYPE *MovedReferences2 )( 
            ICorProfilerCallback7 * This,
            /* [in] */ ULONG cMovedObjectIDRanges,
            /* [size_is][in] */ ObjectID oldObjectIDRangeStart[  ],
            /* [size_is][in] */ ObjectID newObjectIDRangeStart[  ],
            /* [size_is][in] */ SIZE_T cObjectIDRangeLength[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *SurvivingReferences2 )( 
            ICorProfilerCallback7 * This,
            /* [in] */ ULONG cSurvivingObjectIDRanges,
            /* [size_is][in] */ ObjectID objectIDRangeStart[  ],
            /* [size_is][in] */ SIZE_T cObjectIDRangeLength[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *ConditionalWeakTableElementReferences )( 
            ICorProfilerCallback7 * This,
            /* [in] */ ULONG cRootRefs,
            /* [size_is][in] */ ObjectID keyRefIds[  ],
            /* [size_is][in] */ ObjectID valueRefIds[  ],
            /* [size_is][in] */ GCHandleID rootIds[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *GetAssemblyReferences )( 
            ICorProfilerCallback7 * This,
            /* [string][in] */ const WCHAR *wszAssemblyPath,
            /* [in] */ ICorProfilerAssemblyReferenceProvider *pAsmRefProvider);
        
        HRESULT ( STDMETHODCALLTYPE *ModuleInMemorySymbolsUpdated )( 
            ICorProfilerCallback7 * This,
            ModuleID moduleId);
        
        END_INTERFACE
    } ICorProfilerCallback7Vtbl;

    interface ICorProfilerCallback7
    {
        CONST_VTBL struct ICorProfilerCallback7Vtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define ICorProfilerCallback7_QueryInterface(This,riid,ppvObject)   \
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define ICorProfilerCallback7_AddRef(This)  \
    ( (This)->lpVtbl -> AddRef(This) ) 

#define ICorProfilerCallback7_Release(This) \
    ( (This)->lpVtbl -> Release(This) ) 


#define ICorProfilerCallback7_Initialize(This,pICorProfilerInfoUnk) \
    ( (This)->lpVtbl -> Initialize(This,pICorProfilerInfoUnk) ) 

#define ICorProfilerCallback7_Shutdown(This)    \
    ( (This)->lpVtbl -> Shutdown(This) ) 

#define ICorProfilerCallback7_AppDomainCreationStarted(This,appDomainId)    \
    ( (This)->lpVtbl -> AppDomainCreationStarted(This,appDomainId) ) 

#define ICorProfilerCallback7_AppDomainCreationFinished(This,appDomainId,hrStatus)  \
    ( (This)->lpVtbl -> AppDomainCreationFinished(This,appDomainId,hrStatus) ) 

#define ICorProfilerCallback7_AppDomainShutdownStarted(This,appDomainId)    \
    ( (This)->lpVtbl -> AppDomainShutdownStarted(This,appDomainId) ) 

#define ICorProfilerCallback7_AppDomainShutdownFinished(This,appDomainId,hrStatus)  \
    ( (This)->lpVtbl -> AppDomainShutdownFinished(This,appDomainId,hrStatus) ) 

#define ICorProfilerCallback7_AssemblyLoadStarted(This,assemblyId)  \
    ( (This)->lpVtbl -> AssemblyLoadStarted(This,assemblyId) ) 

#define ICorProfilerCallback7_AssemblyLoadFinished(This,assemblyId,hrStatus)    \
    ( (This)->lpVtbl -> AssemblyLoadFinished(This,assemblyId,hrStatus) ) 

#define ICorProfilerCallback7_AssemblyUnloadStarted(This,assemblyId)    \
    ( (This)->lpVtbl -> AssemblyUnloadStarted(This,assemblyId) ) 

#define ICorProfilerCallback7_AssemblyUnloadFinished(This,assemblyId,hrStatus)  \
    ( (This)->lpVtbl -> AssemblyUnloadFinished(This,assemblyId,hrStatus) ) 

#define ICorProfilerCallback7_ModuleLoadStarted(This,moduleId)  \
    ( (This)->lpVtbl -> ModuleLoadStarted(This,moduleId) ) 

#define ICorProfilerCallback7_ModuleLoadFinished(This,moduleId,hrStatus)    \
    ( (This)->lpVtbl -> ModuleLoadFinished(This,moduleId,hrStatus) ) 

#define ICorProfilerCallback7_ModuleUnloadStarted(This,moduleId)    \
    ( (This)->lpVtbl -> ModuleUnloadStarted(This,moduleId) ) 

#define ICorProfilerCallback7_ModuleUnloadFinished(This,moduleId,hrStatus)  \
    ( (This)->lpVtbl -> ModuleUnloadFinished(This,moduleId,hrStatus) ) 

#define ICorProfilerCallback7_ModuleAttachedToAssembly(This,moduleId,AssemblyId)    \
    ( (This)->lpVtbl -> ModuleAttachedToAssembly(This,moduleId,AssemblyId) ) 

#define ICorProfilerCallback7_ClassLoadStarted(This,classId)    \
    ( (This)->lpVtbl -> ClassLoadStarted(This,classId) ) 

#define ICorProfilerCallback7_ClassLoadFinished(This,classId,hrStatus)  \
    ( (This)->lpVtbl -> ClassLoadFinished(This,classId,hrStatus) ) 

#define ICorProfilerCallback7_ClassUnloadStarted(This,classId)  \
    ( (This)->lpVtbl -> ClassUnloadStarted(This,classId) ) 

#define ICorProfilerCallback7_ClassUnloadFinished(This,classId,hrStatus)    \
    ( (This)->lpVtbl -> ClassUnloadFinished(This,classId,hrStatus) ) 

#define ICorProfilerCallback7_FunctionUnloadStarted(This,functionId)    \
    ( (This)->lpVtbl -> FunctionUnloadStarted(This,functionId) ) 

#define ICorProfilerCallback7_JITCompilationStarted(This,functionId,fIsSafeToBlock) \
    ( (This)->lpVtbl -> JITCompilationStarted(This,functionId,fIsSafeToBlock) ) 

#define ICorProfilerCallback7_JITCompilationFinished(This,functionId,hrStatus,fIsSafeToBlock)   \
    ( (This)->lpVtbl -> JITCompilationFinished(This,functionId,hrStatus,fIsSafeToBlock) ) 

#define ICorProfilerCallback7_JITCachedFunctionSearchStarted(This,functionId,pbUseCachedFunction)   \
    ( (This)->lpVtbl -> JITCachedFunctionSearchStarted(This,functionId,pbUseCachedFunction) ) 

#define ICorProfilerCallback7_JITCachedFunctionSearchFinished(This,functionId,result)   \
    ( (This)->lpVtbl -> JITCachedFunctionSearchFinished(This,functionId,result) ) 

#define ICorProfilerCallback7_JITFunctionPitched(This,functionId)   \
    ( (This)->lpVtbl -> JITFunctionPitched(This,functionId) ) 

#define ICorProfilerCallback7_JITInlining(This,callerId,calleeId,pfShouldInline)    \
    ( (This)->lpVtbl -> JITInlining(This,callerId,calleeId,pfShouldInline) ) 

#define ICorProfilerCallback7_ThreadCreated(This,threadId)  \
    ( (This)->lpVtbl -> ThreadCreated(This,threadId) ) 

#define ICorProfilerCallback7_ThreadDestroyed(This,threadId)    \
    ( (This)->lpVtbl -> ThreadDestroyed(This,threadId) ) 

#define ICorProfilerCallback7_ThreadAssignedToOSThread(This,managedThreadId,osThreadId) \
    ( (This)->lpVtbl -> ThreadAssignedToOSThread(This,managedThreadId,osThreadId) ) 

#define ICorProfilerCallback7_RemotingClientInvocationStarted(This) \
    ( (This)->lpVtbl -> RemotingClientInvocationStarted(This) ) 

#define ICorProfilerCallback7_RemotingClientSendingMessage(This,pCookie,fIsAsync)   \
    ( (This)->lpVtbl -> RemotingClientSendingMessage(This,pCookie,fIsAsync) ) 

#define ICorProfilerCallback7_RemotingClientReceivingReply(This,pCookie,fIsAsync)   \
    ( (This)->lpVtbl -> RemotingClientReceivingReply(This,pCookie,fIsAsync) ) 

#define ICorProfilerCallback7_RemotingClientInvocationFinished(This)    \
    ( (This)->lpVtbl -> RemotingClientInvocationFinished(This) ) 

#define ICorProfilerCallback7_RemotingServerReceivingMessage(This,pCookie,fIsAsync) \
    ( (This)->lpVtbl -> RemotingServerReceivingMessage(This,pCookie,fIsAsync) ) 

#define ICorProfilerCallback7_RemotingServerInvocationStarted(This) \
    ( (This)->lpVtbl -> RemotingServerInvocationStarted(This) ) 

#define ICorProfilerCallback7_RemotingServerInvocationReturned(This)    \
    ( (This)->lpVtbl -> RemotingServerInvocationReturned(This) ) 

#define ICorProfilerCallback7_RemotingServerSendingReply(This,pCookie,fIsAsync) \
    ( (This)->lpVtbl -> RemotingServerSendingReply(This,pCookie,fIsAsync) ) 

#define ICorProfilerCallback7_UnmanagedToManagedTransition(This,functionId,reason)  \
    ( (This)->lpVtbl -> UnmanagedToManagedTransition(This,functionId,reason) ) 

#define ICorProfilerCallback7_ManagedToUnmanagedTransition(This,functionId,reason)  \
    ( (This)->lpVtbl -> ManagedToUnmanagedTransition(This,functionId,reason) ) 

#define ICorProfilerCallback7_RuntimeSuspendStarted(This,suspendReason) \
    ( (This)->lpVtbl -> RuntimeSuspendStarted(This,suspendReason) ) 

#define ICorProfilerCallback7_RuntimeSuspendFinished(This)  \
    ( (This)->lpVtbl -> RuntimeSuspendFinished(This) ) 

#define ICorProfilerCallback7_RuntimeSuspendAborted(This)   \
    ( (This)->lpVtbl -> RuntimeSuspendAborted(This) ) 

#define ICorProfilerCallback7_RuntimeResumeStarted(This)    \
    ( (This)->lpVtbl -> RuntimeResumeStarted(This) ) 

#define ICorProfilerCallback7_RuntimeResumeFinished(This)   \
    ( (This)->lpVtbl -> RuntimeResumeFinished(This) ) 

#define ICorProfilerCallback7_RuntimeThreadSuspended(This,threadId) \
    ( (This)->lpVtbl -> RuntimeThreadSuspended(This,threadId) ) 

#define ICorProfilerCallback7_RuntimeThreadResumed(This,threadId)   \
    ( (This)->lpVtbl -> RuntimeThreadResumed(This,threadId) ) 

#define ICorProfilerCallback7_MovedReferences(This,cMovedObjectIDRanges,oldObjectIDRangeStart,newObjectIDRangeStart,cObjectIDRangeLength)   \
    ( (This)->lpVtbl -> MovedReferences(This,cMovedObjectIDRanges,oldObjectIDRangeStart,newObjectIDRangeStart,cObjectIDRangeLength) ) 

#define ICorProfilerCallback7_ObjectAllocated(This,objectId,classId)    \
    ( (This)->lpVtbl -> ObjectAllocated(This,objectId,classId) ) 

#define ICorProfilerCallback7_ObjectsAllocatedByClass(This,cClassCount,classIds,cObjects)   \
    ( (This)->lpVtbl -> ObjectsAllocatedByClass(This,cClassCount,classIds,cObjects) ) 

#define ICorProfilerCallback7_ObjectReferences(This,objectId,classId,cObjectRefs,objectRefIds)  \
    ( (This)->lpVtbl -> ObjectReferences(This,objectId,classId,cObjectRefs,objectRefIds) ) 

#define ICorProfilerCallback7_RootReferences(This,cRootRefs,rootRefIds) \
    ( (This)->lpVtbl -> RootReferences(This,cRootRefs,rootRefIds) ) 

#define ICorProfilerCallback7_ExceptionThrown(This,thrownObjectId)  \
    ( (This)->lpVtbl -> ExceptionThrown(This,thrownObjectId) ) 

#define ICorProfilerCallback7_ExceptionSearchFunctionEnter(This,functionId) \
    ( (This)->lpVtbl -> ExceptionSearchFunctionEnter(This,functionId) ) 

#define ICorProfilerCallback7_ExceptionSearchFunctionLeave(This)    \
    ( (This)->lpVtbl -> ExceptionSearchFunctionLeave(This) ) 

#define ICorProfilerCallback7_ExceptionSearchFilterEnter(This,functionId)   \
    ( (This)->lpVtbl -> ExceptionSearchFilterEnter(This,functionId) ) 

#define ICorProfilerCallback7_ExceptionSearchFilterLeave(This)  \
    ( (This)->lpVtbl -> ExceptionSearchFilterLeave(This) ) 

#define ICorProfilerCallback7_ExceptionSearchCatcherFound(This,functionId)  \
    ( (This)->lpVtbl -> ExceptionSearchCatcherFound(This,functionId) ) 

#define ICorProfilerCallback7_ExceptionOSHandlerEnter(This,__unused)    \
    ( (This)->lpVtbl -> ExceptionOSHandlerEnter(This,__unused) ) 

#define ICorProfilerCallback7_ExceptionOSHandlerLeave(This,__unused)    \
    ( (This)->lpVtbl -> ExceptionOSHandlerLeave(This,__unused) ) 

#define ICorProfilerCallback7_ExceptionUnwindFunctionEnter(This,functionId) \
    ( (This)->lpVtbl -> ExceptionUnwindFunctionEnter(This,functionId) ) 

#define ICorProfilerCallback7_ExceptionUnwindFunctionLeave(This)    \
    ( (This)->lpVtbl -> ExceptionUnwindFunctionLeave(This) ) 

#define ICorProfilerCallback7_ExceptionUnwindFinallyEnter(This,functionId)  \
    ( (This)->lpVtbl -> ExceptionUnwindFinallyEnter(This,functionId) ) 

#define ICorProfilerCallback7_ExceptionUnwindFinallyLeave(This) \
    ( (This)->lpVtbl -> ExceptionUnwindFinallyLeave(This) ) 

#define ICorProfilerCallback7_ExceptionCatcherEnter(This,functionId,objectId)   \
    ( (This)->lpVtbl -> ExceptionCatcherEnter(This,functionId,objectId) ) 

#define ICorProfilerCallback7_ExceptionCatcherLeave(This)   \
    ( (This)->lpVtbl -> ExceptionCatcherLeave(This) ) 

#define ICorProfilerCallback7_COMClassicVTableCreated(This,wrappedClassId,implementedIID,pVTable,cSlots)    \
    ( (This)->lpVtbl -> COMClassicVTableCreated(This,wrappedClassId,implementedIID,pVTable,cSlots) ) 

#define ICorProfilerCallback7_COMClassicVTableDestroyed(This,wrappedClassId,implementedIID,pVTable) \
    ( (This)->lpVtbl -> COMClassicVTableDestroyed(This,wrappedClassId,implementedIID,pVTable) ) 

#define ICorProfilerCallback7_ExceptionCLRCatcherFound(This)    \
    ( (This)->lpVtbl -> ExceptionCLRCatcherFound(This) ) 

#define ICorProfilerCallback7_ExceptionCLRCatcherExecute(This)  \
    ( (This)->lpVtbl -> ExceptionCLRCatcherExecute(This) ) 


#define ICorProfilerCallback7_ThreadNameChanged(This,threadId,cchName,name) \
    ( (This)->lpVtbl -> ThreadNameChanged(This,threadId,cchName,name) ) 

#define ICorProfilerCallback7_GarbageCollectionStarted(This,cGenerations,generationCollected,reason)    \
    ( (This)->lpVtbl -> GarbageCollectionStarted(This,cGenerations,generationCollected,reason) ) 

#define ICorProfilerCallback7_SurvivingReferences(This,cSurvivingObjectIDRanges,objectIDRangeStart,cObjectIDRangeLength)    \
    ( (This)->lpVtbl -> SurvivingReferences(This,cSurvivingObjectIDRanges,objectIDRangeStart,cObjectIDRangeLength) ) 

#define ICorProfilerCallback7_GarbageCollectionFinished(This)   \
    ( (This)->lpVtbl -> GarbageCollectionFinished(This) ) 

#define ICorProfilerCallback7_FinalizeableObjectQueued(This,finalizerFlags,objectID)    \
    ( (This)->lpVtbl -> FinalizeableObjectQueued(This,finalizerFlags,objectID) ) 

#define ICorProfilerCallback7_RootReferences2(This,cRootRefs,rootRefIds,rootKinds,rootFlags,rootIds)    \
    ( (This)->lpVtbl -> RootReferences2(This,cRootRefs,rootRefIds,rootKinds,rootFlags,rootIds) ) 

#define ICorProfilerCallback7_HandleCreated(This,handleId,initialObjectId)  \
    ( (This)->lpVtbl -> HandleCreated(This,handleId,initialObjectId) ) 

#define ICorProfilerCallback7_HandleDestroyed(This,handleId)    \
    ( (This)->lpVtbl -> HandleDestroyed(This,handleId) ) 


#define ICorProfilerCallback7_InitializeForAttach(This,pCorProfilerInfoUnk,pvClientData,cbClientData)   \
    ( (This)->lpVtbl -> InitializeForAttach(This,pCorProfilerInfoUnk,pvClientData,cbClientData) ) 

#define ICorProfilerCallback7_ProfilerAttachComplete(This)  \
    ( (This)->lpVtbl -> ProfilerAttachComplete(This) ) 

#define ICorProfilerCallback7_ProfilerDetachSucceeded(This) \
    ( (This)->lpVtbl -> ProfilerDetachSucceeded(This) ) 


#define ICorProfilerCallback7_ReJITCompilationStarted(This,functionId,rejitId,fIsSafeToBlock)   \
    ( (This)->lpVtbl -> ReJITCompilationStarted(This,functionId,rejitId,fIsSafeToBlock) ) 

#define ICorProfilerCallback7_GetReJITParameters(This,moduleId,methodId,pFunctionControl)   \
    ( (This)->lpVtbl -> GetReJITParameters(This,moduleId,methodId,pFunctionControl) ) 

#define ICorProfilerCallback7_ReJITCompilationFinished(This,functionId,rejitId,hrStatus,fIsSafeToBlock) \
    ( (This)->lpVtbl -> ReJITCompilationFinished(This,functionId,rejitId,hrStatus,fIsSafeToBlock) ) 

#define ICorProfilerCallback7_ReJITError(This,moduleId,methodId,functionId,hrStatus)    \
    ( (This)->lpVtbl -> ReJITError(This,moduleId,methodId,functionId,hrStatus) ) 

#define ICorProfilerCallback7_MovedReferences2(This,cMovedObjectIDRanges,oldObjectIDRangeStart,newObjectIDRangeStart,cObjectIDRangeLength)  \
    ( (This)->lpVtbl -> MovedReferences2(This,cMovedObjectIDRanges,oldObjectIDRangeStart,newObjectIDRangeStart,cObjectIDRangeLength) ) 

#define ICorProfilerCallback7_SurvivingReferences2(This,cSurvivingObjectIDRanges,objectIDRangeStart,cObjectIDRangeLength)   \
    ( (This)->lpVtbl -> SurvivingReferences2(This,cSurvivingObjectIDRanges,objectIDRangeStart,cObjectIDRangeLength) ) 


#define ICorProfilerCallback7_ConditionalWeakTableElementReferences(This,cRootRefs,keyRefIds,valueRefIds,rootIds)   \
    ( (This)->lpVtbl -> ConditionalWeakTableElementReferences(This,cRootRefs,keyRefIds,valueRefIds,rootIds) ) 


#define ICorProfilerCallback7_GetAssemblyReferences(This,wszAssemblyPath,pAsmRefProvider)   \
    ( (This)->lpVtbl -> GetAssemblyReferences(This,wszAssemblyPath,pAsmRefProvider) ) 


#define ICorProfilerCallback7_ModuleInMemorySymbolsUpdated(This,moduleId)   \
    ( (This)->lpVtbl -> ModuleInMemorySymbolsUpdated(This,moduleId) ) 

#endif /* COBJMACROS */


#endif  /* C style interface */




#endif  /* __ICorProfilerCallback7_INTERFACE_DEFINED__ */


#ifndef __ICorProfilerCallback8_INTERFACE_DEFINED__
#define __ICorProfilerCallback8_INTERFACE_DEFINED__

/* interface ICorProfilerCallback8 */
/* [local][unique][uuid][object] */ 


EXTERN_C const IID IID_ICorProfilerCallback8;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("5BED9B15-C079-4D47-BFE2-215A140C07E0")
    ICorProfilerCallback8 : public ICorProfilerCallback7
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE DynamicMethodJITCompilationStarted( 
            /* [in] */ FunctionID functionId,
            /* [in] */ BOOL fIsSafeToBlock,
            /* [in] */ LPCBYTE pILHeader,
            /* [in] */ ULONG cbILHeader) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE DynamicMethodJITCompilationFinished( 
            /* [in] */ FunctionID functionId,
            /* [in] */ HRESULT hrStatus,
            /* [in] */ BOOL fIsSafeToBlock) = 0;
        
    };
    
    
#else   /* C style interface */

    typedef struct ICorProfilerCallback8Vtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            ICorProfilerCallback8 * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            ICorProfilerCallback8 * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            ICorProfilerCallback8 * This);
        
        HRESULT ( STDMETHODCALLTYPE *Initialize )( 
            ICorProfilerCallback8 * This,
            /* [in] */ IUnknown *pICorProfilerInfoUnk);
        
        HRESULT ( STDMETHODCALLTYPE *Shutdown )( 
            ICorProfilerCallback8 * This);
        
        HRESULT ( STDMETHODCALLTYPE *AppDomainCreationStarted )( 
            ICorProfilerCallback8 * This,
            /* [in] */ AppDomainID appDomainId);
        
        HRESULT ( STDMETHODCALLTYPE *AppDomainCreationFinished )( 
            ICorProfilerCallback8 * This,
            /* [in] */ AppDomainID appDomainId,
            /* [in] */ HRESULT hrStatus);
        
        HRESULT ( STDMETHODCALLTYPE *AppDomainShutdownStarted )( 
            ICorProfilerCallback8 * This,
            /* [in] */ AppDomainID appDomainId);
        
        HRESULT ( STDMETHODCALLTYPE *AppDomainShutdownFinished )( 
            ICorProfilerCallback8 * This,
            /* [in] */ AppDomainID appDomainId,
            /* [in] */ HRESULT hrStatus);
        
        HRESULT ( STDMETHODCALLTYPE *AssemblyLoadStarted )( 
            ICorProfilerCallback8 * This,
            /* [in] */ AssemblyID assemblyId);
        
        HRESULT ( STDMETHODCALLTYPE *AssemblyLoadFinished )( 
            ICorProfilerCallback8 * This,
            /* [in] */ AssemblyID assemblyId,
            /* [in] */ HRESULT hrStatus);
        
        HRESULT ( STDMETHODCALLTYPE *AssemblyUnloadStarted )( 
            ICorProfilerCallback8 * This,
            /* [in] */ AssemblyID assemblyId);
        
        HRESULT ( STDMETHODCALLTYPE *AssemblyUnloadFinished )( 
            ICorProfilerCallback8 * This,
            /* [in] */ AssemblyID assemblyId,
            /* [in] */ HRESULT hrStatus);
        
        HRESULT ( STDMETHODCALLTYPE *ModuleLoadStarted )( 
            ICorProfilerCallback8 * This,
            /* [in] */ ModuleID moduleId);
        
        HRESULT ( STDMETHODCALLTYPE *ModuleLoadFinished )( 
            ICorProfilerCallback8 * This,
            /* [in] */ ModuleID moduleId,
            /* [in] */ HRESULT hrStatus);
        
        HRESULT ( STDMETHODCALLTYPE *ModuleUnloadStarted )( 
            ICorProfilerCallback8 * This,
            /* [in] */ ModuleID moduleId);
        
        HRESULT ( STDMETHODCALLTYPE *ModuleUnloadFinished )( 
            ICorProfilerCallback8 * This,
            /* [in] */ ModuleID moduleId,
            /* [in] */ HRESULT hrStatus);
        
        HRESULT ( STDMETHODCALLTYPE *ModuleAttachedToAssembly )( 
            ICorProfilerCallback8 * This,
            /* [in] */ ModuleID moduleId,
            /* [in] */ AssemblyID AssemblyId);
        
        HRESULT ( STDMETHODCALLTYPE *ClassLoadStarted )( 
            ICorProfilerCallback8 * This,
            /* [in] */ ClassID classId);
        
        HRESULT ( STDMETHODCALLTYPE *ClassLoadFinished )( 
            ICorProfilerCallback8 * This,
            /* [in] */ ClassID classId,
            /* [in] */ HRESULT hrStatus);
        
        HRESULT ( STDMETHODCALLTYPE *ClassUnloadStarted )( 
            ICorProfilerCallback8 * This,
            /* [in] */ ClassID classId);
        
        HRESULT ( STDMETHODCALLTYPE *ClassUnloadFinished )( 
            ICorProfilerCallback8 * This,
            /* [in] */ ClassID classId,
            /* [in] */ HRESULT hrStatus);
        
        HRESULT ( STDMETHODCALLTYPE *FunctionUnloadStarted )( 
            ICorProfilerCallback8 * This,
            /* [in] */ FunctionID functionId);
        
        HRESULT ( STDMETHODCALLTYPE *JITCompilationStarted )( 
            ICorProfilerCallback8 * This,
            /* [in] */ FunctionID functionId,
            /* [in] */ BOOL fIsSafeToBlock);
        
        HRESULT ( STDMETHODCALLTYPE *JITCompilationFinished )( 
            ICorProfilerCallback8 * This,
            /* [in] */ FunctionID functionId,
            /* [in] */ HRESULT hrStatus,
            /* [in] */ BOOL fIsSafeToBlock);
        
        HRESULT ( STDMETHODCALLTYPE *JITCachedFunctionSearchStarted )( 
            ICorProfilerCallback8 * This,
            /* [in] */ FunctionID functionId,
            /* [out] */ BOOL *pbUseCachedFunction);
        
        HRESULT ( STDMETHODCALLTYPE *JITCachedFunctionSearchFinished )( 
            ICorProfilerCallback8 * This,
            /* [in] */ FunctionID functionId,
            /* [in] */ COR_PRF_JIT_CACHE result);
        
        HRESULT ( STDMETHODCALLTYPE *JITFunctionPitched )( 
            ICorProfilerCallback8 * This,
            /* [in] */ FunctionID functionId);
        
        HRESULT ( STDMETHODCALLTYPE *JITInlining )( 
            ICorProfilerCallback8 * This,
            /* [in] */ FunctionID callerId,
            /* [in] */ FunctionID calleeId,
            /* [out] */ BOOL *pfShouldInline);
        
        HRESULT ( STDMETHODCALLTYPE *ThreadCreated )( 
            ICorProfilerCallback8 * This,
            /* [in] */ ThreadID threadId);
        
        HRESULT ( STDMETHODCALLTYPE *ThreadDestroyed )( 
            ICorProfilerCallback8 * This,
            /* [in] */ ThreadID threadId);
        
        HRESULT ( STDMETHODCALLTYPE *ThreadAssignedToOSThread )( 
            ICorProfilerCallback8 * This,
            /* [in] */ ThreadID managedThreadId,
            /* [in] */ DWORD osThreadId);
        
        HRESULT ( STDMETHODCALLTYPE *RemotingClientInvocationStarted )( 
            ICorProfilerCallback8 * This);
        
        HRESULT ( STDMETHODCALLTYPE *RemotingClientSendingMessage )( 
            ICorProfilerCallback8 * This,
            /* [in] */ GUID *pCookie,
            /* [in] */ BOOL fIsAsync);
        
        HRESULT ( STDMETHODCALLTYPE *RemotingClientReceivingReply )( 
            ICorProfilerCallback8 * This,
            /* [in] */ GUID *pCookie,
            /* [in] */ BOOL fIsAsync);
        
        HRESULT ( STDMETHODCALLTYPE *RemotingClientInvocationFinished )( 
            ICorProfilerCallback8 * This);
        
        HRESULT ( STDMETHODCALLTYPE *RemotingServerReceivingMessage )( 
            ICorProfilerCallback8 * This,
            /* [in] */ GUID *pCookie,
            /* [in] */ BOOL fIsAsync);
        
        HRESULT ( STDMETHODCALLTYPE *RemotingServerInvocationStarted )( 
            ICorProfilerCallback8 * This);
        
        HRESULT ( STDMETHODCALLTYPE *RemotingServerInvocationReturned )( 
            ICorProfilerCallback8 * This);
        
        HRESULT ( STDMETHODCALLTYPE *RemotingServerSendingReply )( 
            ICorProfilerCallback8 * This,
            /* [in] */ GUID *pCookie,
            /* [in] */ BOOL fIsAsync);
        
        HRESULT ( STDMETHODCALLTYPE *UnmanagedToManagedTransition )( 
            ICorProfilerCallback8 * This,
            /* [in] */ FunctionID functionId,
            /* [in] */ COR_PRF_TRANSITION_REASON reason);
        
        HRESULT ( STDMETHODCALLTYPE *ManagedToUnmanagedTransition )( 
            ICorProfilerCallback8 * This,
            /* [in] */ FunctionID functionId,
            /* [in] */ COR_PRF_TRANSITION_REASON reason);
        
        HRESULT ( STDMETHODCALLTYPE *RuntimeSuspendStarted )( 
            ICorProfilerCallback8 * This,
            /* [in] */ COR_PRF_SUSPEND_REASON suspendReason);
        
        HRESULT ( STDMETHODCALLTYPE *RuntimeSuspendFinished )( 
            ICorProfilerCallback8 * This);
        
        HRESULT ( STDMETHODCALLTYPE *RuntimeSuspendAborted )( 
            ICorProfilerCallback8 * This);
        
        HRESULT ( STDMETHODCALLTYPE *RuntimeResumeStarted )( 
            ICorProfilerCallback8 * This);
        
        HRESULT ( STDMETHODCALLTYPE *RuntimeResumeFinished )( 
            ICorProfilerCallback8 * This);
        
        HRESULT ( STDMETHODCALLTYPE *RuntimeThreadSuspended )( 
            ICorProfilerCallback8 * This,
            /* [in] */ ThreadID threadId);
        
        HRESULT ( STDMETHODCALLTYPE *RuntimeThreadResumed )( 
            ICorProfilerCallback8 * This,
            /* [in] */ ThreadID threadId);
        
        HRESULT ( STDMETHODCALLTYPE *MovedReferences )( 
            ICorProfilerCallback8 * This,
            /* [in] */ ULONG cMovedObjectIDRanges,
            /* [size_is][in] */ ObjectID oldObjectIDRangeStart[  ],
            /* [size_is][in] */ ObjectID newObjectIDRangeStart[  ],
            /* [size_is][in] */ ULONG cObjectIDRangeLength[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *ObjectAllocated )( 
            ICorProfilerCallback8 * This,
            /* [in] */ ObjectID objectId,
            /* [in] */ ClassID classId);
        
        HRESULT ( STDMETHODCALLTYPE *ObjectsAllocatedByClass )( 
            ICorProfilerCallback8 * This,
            /* [in] */ ULONG cClassCount,
            /* [size_is][in] */ ClassID classIds[  ],
            /* [size_is][in] */ ULONG cObjects[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *ObjectReferences )( 
            ICorProfilerCallback8 * This,
            /* [in] */ ObjectID objectId,
            /* [in] */ ClassID classId,
            /* [in] */ ULONG cObjectRefs,
            /* [size_is][in] */ ObjectID objectRefIds[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *RootReferences )( 
            ICorProfilerCallback8 * This,
            /* [in] */ ULONG cRootRefs,
            /* [size_is][in] */ ObjectID rootRefIds[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *ExceptionThrown )( 
            ICorProfilerCallback8 * This,
            /* [in] */ ObjectID thrownObjectId);
        
        HRESULT ( STDMETHODCALLTYPE *ExceptionSearchFunctionEnter )( 
            ICorProfilerCallback8 * This,
            /* [in] */ FunctionID functionId);
        
        HRESULT ( STDMETHODCALLTYPE *ExceptionSearchFunctionLeave )( 
            ICorProfilerCallback8 * This);
        
        HRESULT ( STDMETHODCALLTYPE *ExceptionSearchFilterEnter )( 
            ICorProfilerCallback8 * This,
            /* [in] */ FunctionID functionId);
        
        HRESULT ( STDMETHODCALLTYPE *ExceptionSearchFilterLeave )( 
            ICorProfilerCallback8 * This);
        
        HRESULT ( STDMETHODCALLTYPE *ExceptionSearchCatcherFound )( 
            ICorProfilerCallback8 * This,
            /* [in] */ FunctionID functionId);
        
        HRESULT ( STDMETHODCALLTYPE *ExceptionOSHandlerEnter )( 
            ICorProfilerCallback8 * This,
            /* [in] */ UINT_PTR __unused);
        
        HRESULT ( STDMETHODCALLTYPE *ExceptionOSHandlerLeave )( 
            ICorProfilerCallback8 * This,
            /* [in] */ UINT_PTR __unused);
        
        HRESULT ( STDMETHODCALLTYPE *ExceptionUnwindFunctionEnter )( 
            ICorProfilerCallback8 * This,
            /* [in] */ FunctionID functionId);
        
        HRESULT ( STDMETHODCALLTYPE *ExceptionUnwindFunctionLeave )( 
            ICorProfilerCallback8 * This);
        
        HRESULT ( STDMETHODCALLTYPE *ExceptionUnwindFinallyEnter )( 
            ICorProfilerCallback8 * This,
            /* [in] */ FunctionID functionId);
        
        HRESULT ( STDMETHODCALLTYPE *ExceptionUnwindFinallyLeave )( 
            ICorProfilerCallback8 * This);
        
        HRESULT ( STDMETHODCALLTYPE *ExceptionCatcherEnter )( 
            ICorProfilerCallback8 * This,
            /* [in] */ FunctionID functionId,
            /* [in] */ ObjectID objectId);
        
        HRESULT ( STDMETHODCALLTYPE *ExceptionCatcherLeave )( 
            ICorProfilerCallback8 * This);
        
        HRESULT ( STDMETHODCALLTYPE *COMClassicVTableCreated )( 
            ICorProfilerCallback8 * This,
            /* [in] */ ClassID wrappedClassId,
            /* [in] */ REFGUID implementedIID,
            /* [in] */ void *pVTable,
            /* [in] */ ULONG cSlots);
        
        HRESULT ( STDMETHODCALLTYPE *COMClassicVTableDestroyed )( 
            ICorProfilerCallback8 * This,
            /* [in] */ ClassID wrappedClassId,
            /* [in] */ REFGUID implementedIID,
            /* [in] */ void *pVTable);
        
        HRESULT ( STDMETHODCALLTYPE *ExceptionCLRCatcherFound )( 
            ICorProfilerCallback8 * This);
        
        HRESULT ( STDMETHODCALLTYPE *ExceptionCLRCatcherExecute )( 
            ICorProfilerCallback8 * This);
        
        HRESULT ( STDMETHODCALLTYPE *ThreadNameChanged )( 
            ICorProfilerCallback8 * This,
            /* [in] */ ThreadID threadId,
            /* [in] */ ULONG cchName,
            /* [annotation][in] */ 
            _In_reads_opt_(cchName)  WCHAR name[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *GarbageCollectionStarted )( 
            ICorProfilerCallback8 * This,
            /* [in] */ int cGenerations,
            /* [size_is][in] */ BOOL generationCollected[  ],
            /* [in] */ COR_PRF_GC_REASON reason);
        
        HRESULT ( STDMETHODCALLTYPE *SurvivingReferences )( 
            ICorProfilerCallback8 * This,
            /* [in] */ ULONG cSurvivingObjectIDRanges,
            /* [size_is][in] */ ObjectID objectIDRangeStart[  ],
            /* [size_is][in] */ ULONG cObjectIDRangeLength[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *GarbageCollectionFinished )( 
            ICorProfilerCallback8 * This);
        
        HRESULT ( STDMETHODCALLTYPE *FinalizeableObjectQueued )( 
            ICorProfilerCallback8 * This,
            /* [in] */ DWORD finalizerFlags,
            /* [in] */ ObjectID objectID);
        
        HRESULT ( STDMETHODCALLTYPE *RootReferences2 )( 
            ICorProfilerCallback8 * This,
            /* [in] */ ULONG cRootRefs,
            /* [size_is][in] */ ObjectID rootRefIds[  ],
            /* [size_is][in] */ COR_PRF_GC_ROOT_KIND rootKinds[  ],
            /* [size_is][in] */ COR_PRF_GC_ROOT_FLAGS rootFlags[  ],
            /* [size_is][in] */ UINT_PTR rootIds[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *HandleCreated )( 
            ICorProfilerCallback8 * This,
            /* [in] */ GCHandleID handleId,
            /* [in] */ ObjectID initialObjectId);
        
        HRESULT ( STDMETHODCALLTYPE *HandleDestroyed )( 
            ICorProfilerCallback8 * This,
            /* [in] */ GCHandleID handleId);
        
        HRESULT ( STDMETHODCALLTYPE *InitializeForAttach )( 
            ICorProfilerCallback8 * This,
            /* [in] */ IUnknown *pCorProfilerInfoUnk,
            /* [in] */ void *pvClientData,
            /* [in] */ UINT cbClientData);
        
        HRESULT ( STDMETHODCALLTYPE *ProfilerAttachComplete )( 
            ICorProfilerCallback8 * This);
        
        HRESULT ( STDMETHODCALLTYPE *ProfilerDetachSucceeded )( 
            ICorProfilerCallback8 * This);
        
        HRESULT ( STDMETHODCALLTYPE *ReJITCompilationStarted )( 
            ICorProfilerCallback8 * This,
            /* [in] */ FunctionID functionId,
            /* [in] */ ReJITID rejitId,
            /* [in] */ BOOL fIsSafeToBlock);
        
        HRESULT ( STDMETHODCALLTYPE *GetReJITParameters )( 
            ICorProfilerCallback8 * This,
            /* [in] */ ModuleID moduleId,
            /* [in] */ mdMethodDef methodId,
            /* [in] */ ICorProfilerFunctionControl *pFunctionControl);
        
        HRESULT ( STDMETHODCALLTYPE *ReJITCompilationFinished )( 
            ICorProfilerCallback8 * This,
            /* [in] */ FunctionID functionId,
            /* [in] */ ReJITID rejitId,
            /* [in] */ HRESULT hrStatus,
            /* [in] */ BOOL fIsSafeToBlock);
        
        HRESULT ( STDMETHODCALLTYPE *ReJITError )( 
            ICorProfilerCallback8 * This,
            /* [in] */ ModuleID moduleId,
            /* [in] */ mdMethodDef methodId,
            /* [in] */ FunctionID functionId,
            /* [in] */ HRESULT hrStatus);
        
        HRESULT ( STDMETHODCALLTYPE *MovedReferences2 )( 
            ICorProfilerCallback8 * This,
            /* [in] */ ULONG cMovedObjectIDRanges,
            /* [size_is][in] */ ObjectID oldObjectIDRangeStart[  ],
            /* [size_is][in] */ ObjectID newObjectIDRangeStart[  ],
            /* [size_is][in] */ SIZE_T cObjectIDRangeLength[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *SurvivingReferences2 )( 
            ICorProfilerCallback8 * This,
            /* [in] */ ULONG cSurvivingObjectIDRanges,
            /* [size_is][in] */ ObjectID objectIDRangeStart[  ],
            /* [size_is][in] */ SIZE_T cObjectIDRangeLength[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *ConditionalWeakTableElementReferences )( 
            ICorProfilerCallback8 * This,
            /* [in] */ ULONG cRootRefs,
            /* [size_is][in] */ ObjectID keyRefIds[  ],
            /* [size_is][in] */ ObjectID valueRefIds[  ],
            /* [size_is][in] */ GCHandleID rootIds[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *GetAssemblyReferences )( 
            ICorProfilerCallback8 * This,
            /* [string][in] */ const WCHAR *wszAssemblyPath,
            /* [in] */ ICorProfilerAssemblyReferenceProvider *pAsmRefProvider);
        
        HRESULT ( STDMETHODCALLTYPE *ModuleInMemorySymbolsUpdated )( 
            ICorProfilerCallback8 * This,
            ModuleID moduleId);
        
        HRESULT ( STDMETHODCALLTYPE *DynamicMethodJITCompilationStarted )( 
            ICorProfilerCallback8 * This,
            /* [in] */ FunctionID functionId,
            /* [in] */ BOOL fIsSafeToBlock,
            /* [in] */ LPCBYTE pILHeader,
            /* [in] */ ULONG cbILHeader);
        
        HRESULT ( STDMETHODCALLTYPE *DynamicMethodJITCompilationFinished )( 
            ICorProfilerCallback8 * This,
            /* [in] */ FunctionID functionId,
            /* [in] */ HRESULT hrStatus,
            /* [in] */ BOOL fIsSafeToBlock);
        
        END_INTERFACE
    } ICorProfilerCallback8Vtbl;

    interface ICorProfilerCallback8
    {
        CONST_VTBL struct ICorProfilerCallback8Vtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define ICorProfilerCallback8_QueryInterface(This,riid,ppvObject)   \
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define ICorProfilerCallback8_AddRef(This)  \
    ( (This)->lpVtbl -> AddRef(This) ) 

#define ICorProfilerCallback8_Release(This) \
    ( (This)->lpVtbl -> Release(This) ) 


#define ICorProfilerCallback8_Initialize(This,pICorProfilerInfoUnk) \
    ( (This)->lpVtbl -> Initialize(This,pICorProfilerInfoUnk) ) 

#define ICorProfilerCallback8_Shutdown(This)    \
    ( (This)->lpVtbl -> Shutdown(This) ) 

#define ICorProfilerCallback8_AppDomainCreationStarted(This,appDomainId)    \
    ( (This)->lpVtbl -> AppDomainCreationStarted(This,appDomainId) ) 

#define ICorProfilerCallback8_AppDomainCreationFinished(This,appDomainId,hrStatus)  \
    ( (This)->lpVtbl -> AppDomainCreationFinished(This,appDomainId,hrStatus) ) 

#define ICorProfilerCallback8_AppDomainShutdownStarted(This,appDomainId)    \
    ( (This)->lpVtbl -> AppDomainShutdownStarted(This,appDomainId) ) 

#define ICorProfilerCallback8_AppDomainShutdownFinished(This,appDomainId,hrStatus)  \
    ( (This)->lpVtbl -> AppDomainShutdownFinished(This,appDomainId,hrStatus) ) 

#define ICorProfilerCallback8_AssemblyLoadStarted(This,assemblyId)  \
    ( (This)->lpVtbl -> AssemblyLoadStarted(This,assemblyId) ) 

#define ICorProfilerCallback8_AssemblyLoadFinished(This,assemblyId,hrStatus)    \
    ( (This)->lpVtbl -> AssemblyLoadFinished(This,assemblyId,hrStatus) ) 

#define ICorProfilerCallback8_AssemblyUnloadStarted(This,assemblyId)    \
    ( (This)->lpVtbl -> AssemblyUnloadStarted(This,assemblyId) ) 

#define ICorProfilerCallback8_AssemblyUnloadFinished(This,assemblyId,hrStatus)  \
    ( (This)->lpVtbl -> AssemblyUnloadFinished(This,assemblyId,hrStatus) ) 

#define ICorProfilerCallback8_ModuleLoadStarted(This,moduleId)  \
    ( (This)->lpVtbl -> ModuleLoadStarted(This,moduleId) ) 

#define ICorProfilerCallback8_ModuleLoadFinished(This,moduleId,hrStatus)    \
    ( (This)->lpVtbl -> ModuleLoadFinished(This,moduleId,hrStatus) ) 

#define ICorProfilerCallback8_ModuleUnloadStarted(This,moduleId)    \
    ( (This)->lpVtbl -> ModuleUnloadStarted(This,moduleId) ) 

#define ICorProfilerCallback8_ModuleUnloadFinished(This,moduleId,hrStatus)  \
    ( (This)->lpVtbl -> ModuleUnloadFinished(This,moduleId,hrStatus) ) 

#define ICorProfilerCallback8_ModuleAttachedToAssembly(This,moduleId,AssemblyId)    \
    ( (This)->lpVtbl -> ModuleAttachedToAssembly(This,moduleId,AssemblyId) ) 

#define ICorProfilerCallback8_ClassLoadStarted(This,classId)    \
    ( (This)->lpVtbl -> ClassLoadStarted(This,classId) ) 

#define ICorProfilerCallback8_ClassLoadFinished(This,classId,hrStatus)  \
    ( (This)->lpVtbl -> ClassLoadFinished(This,classId,hrStatus) ) 

#define ICorProfilerCallback8_ClassUnloadStarted(This,classId)  \
    ( (This)->lpVtbl -> ClassUnloadStarted(This,classId) ) 

#define ICorProfilerCallback8_ClassUnloadFinished(This,classId,hrStatus)    \
    ( (This)->lpVtbl -> ClassUnloadFinished(This,classId,hrStatus) ) 

#define ICorProfilerCallback8_FunctionUnloadStarted(This,functionId)    \
    ( (This)->lpVtbl -> FunctionUnloadStarted(This,functionId) ) 

#define ICorProfilerCallback8_JITCompilationStarted(This,functionId,fIsSafeToBlock) \
    ( (This)->lpVtbl -> JITCompilationStarted(This,functionId,fIsSafeToBlock) ) 

#define ICorProfilerCallback8_JITCompilationFinished(This,functionId,hrStatus,fIsSafeToBlock)   \
    ( (This)->lpVtbl -> JITCompilationFinished(This,functionId,hrStatus,fIsSafeToBlock) ) 

#define ICorProfilerCallback8_JITCachedFunctionSearchStarted(This,functionId,pbUseCachedFunction)   \
    ( (This)->lpVtbl -> JITCachedFunctionSearchStarted(This,functionId,pbUseCachedFunction) ) 

#define ICorProfilerCallback8_JITCachedFunctionSearchFinished(This,functionId,result)   \
    ( (This)->lpVtbl -> JITCachedFunctionSearchFinished(This,functionId,result) ) 

#define ICorProfilerCallback8_JITFunctionPitched(This,functionId)   \
    ( (This)->lpVtbl -> JITFunctionPitched(This,functionId) ) 

#define ICorProfilerCallback8_JITInlining(This,callerId,calleeId,pfShouldInline)    \
    ( (This)->lpVtbl -> JITInlining(This,callerId,calleeId,pfShouldInline) ) 

#define ICorProfilerCallback8_ThreadCreated(This,threadId)  \
    ( (This)->lpVtbl -> ThreadCreated(This,threadId) ) 

#define ICorProfilerCallback8_ThreadDestroyed(This,threadId)    \
    ( (This)->lpVtbl -> ThreadDestroyed(This,threadId) ) 

#define ICorProfilerCallback8_ThreadAssignedToOSThread(This,managedThreadId,osThreadId) \
    ( (This)->lpVtbl -> ThreadAssignedToOSThread(This,managedThreadId,osThreadId) ) 

#define ICorProfilerCallback8_RemotingClientInvocationStarted(This) \
    ( (This)->lpVtbl -> RemotingClientInvocationStarted(This) ) 

#define ICorProfilerCallback8_RemotingClientSendingMessage(This,pCookie,fIsAsync)   \
    ( (This)->lpVtbl -> RemotingClientSendingMessage(This,pCookie,fIsAsync) ) 

#define ICorProfilerCallback8_RemotingClientReceivingReply(This,pCookie,fIsAsync)   \
    ( (This)->lpVtbl -> RemotingClientReceivingReply(This,pCookie,fIsAsync) ) 

#define ICorProfilerCallback8_RemotingClientInvocationFinished(This)    \
    ( (This)->lpVtbl -> RemotingClientInvocationFinished(This) ) 

#define ICorProfilerCallback8_RemotingServerReceivingMessage(This,pCookie,fIsAsync) \
    ( (This)->lpVtbl -> RemotingServerReceivingMessage(This,pCookie,fIsAsync) ) 

#define ICorProfilerCallback8_RemotingServerInvocationStarted(This) \
    ( (This)->lpVtbl -> RemotingServerInvocationStarted(This) ) 

#define ICorProfilerCallback8_RemotingServerInvocationReturned(This)    \
    ( (This)->lpVtbl -> RemotingServerInvocationReturned(This) ) 

#define ICorProfilerCallback8_RemotingServerSendingReply(This,pCookie,fIsAsync) \
    ( (This)->lpVtbl -> RemotingServerSendingReply(This,pCookie,fIsAsync) ) 

#define ICorProfilerCallback8_UnmanagedToManagedTransition(This,functionId,reason)  \
    ( (This)->lpVtbl -> UnmanagedToManagedTransition(This,functionId,reason) ) 

#define ICorProfilerCallback8_ManagedToUnmanagedTransition(This,functionId,reason)  \
    ( (This)->lpVtbl -> ManagedToUnmanagedTransition(This,functionId,reason) ) 

#define ICorProfilerCallback8_RuntimeSuspendStarted(This,suspendReason) \
    ( (This)->lpVtbl -> RuntimeSuspendStarted(This,suspendReason) ) 

#define ICorProfilerCallback8_RuntimeSuspendFinished(This)  \
    ( (This)->lpVtbl -> RuntimeSuspendFinished(This) ) 

#define ICorProfilerCallback8_RuntimeSuspendAborted(This)   \
    ( (This)->lpVtbl -> RuntimeSuspendAborted(This) ) 

#define ICorProfilerCallback8_RuntimeResumeStarted(This)    \
    ( (This)->lpVtbl -> RuntimeResumeStarted(This) ) 

#define ICorProfilerCallback8_RuntimeResumeFinished(This)   \
    ( (This)->lpVtbl -> RuntimeResumeFinished(This) ) 

#define ICorProfilerCallback8_RuntimeThreadSuspended(This,threadId) \
    ( (This)->lpVtbl -> RuntimeThreadSuspended(This,threadId) ) 

#define ICorProfilerCallback8_RuntimeThreadResumed(This,threadId)   \
    ( (This)->lpVtbl -> RuntimeThreadResumed(This,threadId) ) 

#define ICorProfilerCallback8_MovedReferences(This,cMovedObjectIDRanges,oldObjectIDRangeStart,newObjectIDRangeStart,cObjectIDRangeLength)   \
    ( (This)->lpVtbl -> MovedReferences(This,cMovedObjectIDRanges,oldObjectIDRangeStart,newObjectIDRangeStart,cObjectIDRangeLength) ) 

#define ICorProfilerCallback8_ObjectAllocated(This,objectId,classId)    \
    ( (This)->lpVtbl -> ObjectAllocated(This,objectId,classId) ) 

#define ICorProfilerCallback8_ObjectsAllocatedByClass(This,cClassCount,classIds,cObjects)   \
    ( (This)->lpVtbl -> ObjectsAllocatedByClass(This,cClassCount,classIds,cObjects) ) 

#define ICorProfilerCallback8_ObjectReferences(This,objectId,classId,cObjectRefs,objectRefIds)  \
    ( (This)->lpVtbl -> ObjectReferences(This,objectId,classId,cObjectRefs,objectRefIds) ) 

#define ICorProfilerCallback8_RootReferences(This,cRootRefs,rootRefIds) \
    ( (This)->lpVtbl -> RootReferences(This,cRootRefs,rootRefIds) ) 

#define ICorProfilerCallback8_ExceptionThrown(This,thrownObjectId)  \
    ( (This)->lpVtbl -> ExceptionThrown(This,thrownObjectId) ) 

#define ICorProfilerCallback8_ExceptionSearchFunctionEnter(This,functionId) \
    ( (This)->lpVtbl -> ExceptionSearchFunctionEnter(This,functionId) ) 

#define ICorProfilerCallback8_ExceptionSearchFunctionLeave(This)    \
    ( (This)->lpVtbl -> ExceptionSearchFunctionLeave(This) ) 

#define ICorProfilerCallback8_ExceptionSearchFilterEnter(This,functionId)   \
    ( (This)->lpVtbl -> ExceptionSearchFilterEnter(This,functionId) ) 

#define ICorProfilerCallback8_ExceptionSearchFilterLeave(This)  \
    ( (This)->lpVtbl -> ExceptionSearchFilterLeave(This) ) 

#define ICorProfilerCallback8_ExceptionSearchCatcherFound(This,functionId)  \
    ( (This)->lpVtbl -> ExceptionSearchCatcherFound(This,functionId) ) 

#define ICorProfilerCallback8_ExceptionOSHandlerEnter(This,__unused)    \
    ( (This)->lpVtbl -> ExceptionOSHandlerEnter(This,__unused) ) 

#define ICorProfilerCallback8_ExceptionOSHandlerLeave(This,__unused)    \
    ( (This)->lpVtbl -> ExceptionOSHandlerLeave(This,__unused) ) 

#define ICorProfilerCallback8_ExceptionUnwindFunctionEnter(This,functionId) \
    ( (This)->lpVtbl -> ExceptionUnwindFunctionEnter(This,functionId) ) 

#define ICorProfilerCallback8_ExceptionUnwindFunctionLeave(This)    \
    ( (This)->lpVtbl -> ExceptionUnwindFunctionLeave(This) ) 

#define ICorProfilerCallback8_ExceptionUnwindFinallyEnter(This,functionId)  \
    ( (This)->lpVtbl -> ExceptionUnwindFinallyEnter(This,functionId) ) 

#define ICorProfilerCallback8_ExceptionUnwindFinallyLeave(This) \
    ( (This)->lpVtbl -> ExceptionUnwindFinallyLeave(This) ) 

#define ICorProfilerCallback8_ExceptionCatcherEnter(This,functionId,objectId)   \
    ( (This)->lpVtbl -> ExceptionCatcherEnter(This,functionId,objectId) ) 

#define ICorProfilerCallback8_ExceptionCatcherLeave(This)   \
    ( (This)->lpVtbl -> ExceptionCatcherLeave(This) ) 

#define ICorProfilerCallback8_COMClassicVTableCreated(This,wrappedClassId,implementedIID,pVTable,cSlots)    \
    ( (This)->lpVtbl -> COMClassicVTableCreated(This,wrappedClassId,implementedIID,pVTable,cSlots) ) 

#define ICorProfilerCallback8_COMClassicVTableDestroyed(This,wrappedClassId,implementedIID,pVTable) \
    ( (This)->lpVtbl -> COMClassicVTableDestroyed(This,wrappedClassId,implementedIID,pVTable) ) 

#define ICorProfilerCallback8_ExceptionCLRCatcherFound(This)    \
    ( (This)->lpVtbl -> ExceptionCLRCatcherFound(This) ) 

#define ICorProfilerCallback8_ExceptionCLRCatcherExecute(This)  \
    ( (This)->lpVtbl -> ExceptionCLRCatcherExecute(This) ) 


#define ICorProfilerCallback8_ThreadNameChanged(This,threadId,cchName,name) \
    ( (This)->lpVtbl -> ThreadNameChanged(This,threadId,cchName,name) ) 

#define ICorProfilerCallback8_GarbageCollectionStarted(This,cGenerations,generationCollected,reason)    \
    ( (This)->lpVtbl -> GarbageCollectionStarted(This,cGenerations,generationCollected,reason) ) 

#define ICorProfilerCallback8_SurvivingReferences(This,cSurvivingObjectIDRanges,objectIDRangeStart,cObjectIDRangeLength)    \
    ( (This)->lpVtbl -> SurvivingReferences(This,cSurvivingObjectIDRanges,objectIDRangeStart,cObjectIDRangeLength) ) 

#define ICorProfilerCallback8_GarbageCollectionFinished(This)   \
    ( (This)->lpVtbl -> GarbageCollectionFinished(This) ) 

#define ICorProfilerCallback8_FinalizeableObjectQueued(This,finalizerFlags,objectID)    \
    ( (This)->lpVtbl -> FinalizeableObjectQueued(This,finalizerFlags,objectID) ) 

#define ICorProfilerCallback8_RootReferences2(This,cRootRefs,rootRefIds,rootKinds,rootFlags,rootIds)    \
    ( (This)->lpVtbl -> RootReferences2(This,cRootRefs,rootRefIds,rootKinds,rootFlags,rootIds) ) 

#define ICorProfilerCallback8_HandleCreated(This,handleId,initialObjectId)  \
    ( (This)->lpVtbl -> HandleCreated(This,handleId,initialObjectId) ) 

#define ICorProfilerCallback8_HandleDestroyed(This,handleId)    \
    ( (This)->lpVtbl -> HandleDestroyed(This,handleId) ) 


#define ICorProfilerCallback8_InitializeForAttach(This,pCorProfilerInfoUnk,pvClientData,cbClientData)   \
    ( (This)->lpVtbl -> InitializeForAttach(This,pCorProfilerInfoUnk,pvClientData,cbClientData) ) 

#define ICorProfilerCallback8_ProfilerAttachComplete(This)  \
    ( (This)->lpVtbl -> ProfilerAttachComplete(This) ) 

#define ICorProfilerCallback8_ProfilerDetachSucceeded(This) \
    ( (This)->lpVtbl -> ProfilerDetachSucceeded(This) ) 


#define ICorProfilerCallback8_ReJITCompilationStarted(This,functionId,rejitId,fIsSafeToBlock)   \
    ( (This)->lpVtbl -> ReJITCompilationStarted(This,functionId,rejitId,fIsSafeToBlock) ) 

#define ICorProfilerCallback8_GetReJITParameters(This,moduleId,methodId,pFunctionControl)   \
    ( (This)->lpVtbl -> GetReJITParameters(This,moduleId,methodId,pFunctionControl) ) 

#define ICorProfilerCallback8_ReJITCompilationFinished(This,functionId,rejitId,hrStatus,fIsSafeToBlock) \
    ( (This)->lpVtbl -> ReJITCompilationFinished(This,functionId,rejitId,hrStatus,fIsSafeToBlock) ) 

#define ICorProfilerCallback8_ReJITError(This,moduleId,methodId,functionId,hrStatus)    \
    ( (This)->lpVtbl -> ReJITError(This,moduleId,methodId,functionId,hrStatus) ) 

#define ICorProfilerCallback8_MovedReferences2(This,cMovedObjectIDRanges,oldObjectIDRangeStart,newObjectIDRangeStart,cObjectIDRangeLength)  \
    ( (This)->lpVtbl -> MovedReferences2(This,cMovedObjectIDRanges,oldObjectIDRangeStart,newObjectIDRangeStart,cObjectIDRangeLength) ) 

#define ICorProfilerCallback8_SurvivingReferences2(This,cSurvivingObjectIDRanges,objectIDRangeStart,cObjectIDRangeLength)   \
    ( (This)->lpVtbl -> SurvivingReferences2(This,cSurvivingObjectIDRanges,objectIDRangeStart,cObjectIDRangeLength) ) 


#define ICorProfilerCallback8_ConditionalWeakTableElementReferences(This,cRootRefs,keyRefIds,valueRefIds,rootIds)   \
    ( (This)->lpVtbl -> ConditionalWeakTableElementReferences(This,cRootRefs,keyRefIds,valueRefIds,rootIds) ) 


#define ICorProfilerCallback8_GetAssemblyReferences(This,wszAssemblyPath,pAsmRefProvider)   \
    ( (This)->lpVtbl -> GetAssemblyReferences(This,wszAssemblyPath,pAsmRefProvider) ) 


#define ICorProfilerCallback8_ModuleInMemorySymbolsUpdated(This,moduleId)   \
    ( (This)->lpVtbl -> ModuleInMemorySymbolsUpdated(This,moduleId) ) 


#define ICorProfilerCallback8_DynamicMethodJITCompilationStarted(This,functionId,fIsSafeToBlock,pILHeader,cbILHeader)   \
    ( (This)->lpVtbl -> DynamicMethodJITCompilationStarted(This,functionId,fIsSafeToBlock,pILHeader,cbILHeader) ) 

#define ICorProfilerCallback8_DynamicMethodJITCompilationFinished(This,functionId,hrStatus,fIsSafeToBlock)  \
    ( (This)->lpVtbl -> DynamicMethodJITCompilationFinished(This,functionId,hrStatus,fIsSafeToBlock) ) 

#endif /* COBJMACROS */


#endif  /* C style interface */




#endif  /* __ICorProfilerCallback8_INTERFACE_DEFINED__ */


#ifndef __ICorProfilerCallback9_INTERFACE_DEFINED__
#define __ICorProfilerCallback9_INTERFACE_DEFINED__

/* interface ICorProfilerCallback9 */
/* [local][unique][uuid][object] */ 


EXTERN_C const IID IID_ICorProfilerCallback9;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("27583EC3-C8F5-482F-8052-194B8CE4705A")
    ICorProfilerCallback9 : public ICorProfilerCallback8
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE DynamicMethodUnloaded( 
            /* [in] */ FunctionID functionId) = 0;
        
    };
    
    
#else   /* C style interface */

    typedef struct ICorProfilerCallback9Vtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            ICorProfilerCallback9 * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            ICorProfilerCallback9 * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            ICorProfilerCallback9 * This);
        
        HRESULT ( STDMETHODCALLTYPE *Initialize )( 
            ICorProfilerCallback9 * This,
            /* [in] */ IUnknown *pICorProfilerInfoUnk);
        
        HRESULT ( STDMETHODCALLTYPE *Shutdown )( 
            ICorProfilerCallback9 * This);
        
        HRESULT ( STDMETHODCALLTYPE *AppDomainCreationStarted )( 
            ICorProfilerCallback9 * This,
            /* [in] */ AppDomainID appDomainId);
        
        HRESULT ( STDMETHODCALLTYPE *AppDomainCreationFinished )( 
            ICorProfilerCallback9 * This,
            /* [in] */ AppDomainID appDomainId,
            /* [in] */ HRESULT hrStatus);
        
        HRESULT ( STDMETHODCALLTYPE *AppDomainShutdownStarted )( 
            ICorProfilerCallback9 * This,
            /* [in] */ AppDomainID appDomainId);
        
        HRESULT ( STDMETHODCALLTYPE *AppDomainShutdownFinished )( 
            ICorProfilerCallback9 * This,
            /* [in] */ AppDomainID appDomainId,
            /* [in] */ HRESULT hrStatus);
        
        HRESULT ( STDMETHODCALLTYPE *AssemblyLoadStarted )( 
            ICorProfilerCallback9 * This,
            /* [in] */ AssemblyID assemblyId);
        
        HRESULT ( STDMETHODCALLTYPE *AssemblyLoadFinished )( 
            ICorProfilerCallback9 * This,
            /* [in] */ AssemblyID assemblyId,
            /* [in] */ HRESULT hrStatus);
        
        HRESULT ( STDMETHODCALLTYPE *AssemblyUnloadStarted )( 
            ICorProfilerCallback9 * This,
            /* [in] */ AssemblyID assemblyId);
        
        HRESULT ( STDMETHODCALLTYPE *AssemblyUnloadFinished )( 
            ICorProfilerCallback9 * This,
            /* [in] */ AssemblyID assemblyId,
            /* [in] */ HRESULT hrStatus);
        
        HRESULT ( STDMETHODCALLTYPE *ModuleLoadStarted )( 
            ICorProfilerCallback9 * This,
            /* [in] */ ModuleID moduleId);
        
        HRESULT ( STDMETHODCALLTYPE *ModuleLoadFinished )( 
            ICorProfilerCallback9 * This,
            /* [in] */ ModuleID moduleId,
            /* [in] */ HRESULT hrStatus);
        
        HRESULT ( STDMETHODCALLTYPE *ModuleUnloadStarted )( 
            ICorProfilerCallback9 * This,
            /* [in] */ ModuleID moduleId);
        
        HRESULT ( STDMETHODCALLTYPE *ModuleUnloadFinished )( 
            ICorProfilerCallback9 * This,
            /* [in] */ ModuleID moduleId,
            /* [in] */ HRESULT hrStatus);
        
        HRESULT ( STDMETHODCALLTYPE *ModuleAttachedToAssembly )( 
            ICorProfilerCallback9 * This,
            /* [in] */ ModuleID moduleId,
            /* [in] */ AssemblyID AssemblyId);
        
        HRESULT ( STDMETHODCALLTYPE *ClassLoadStarted )( 
            ICorProfilerCallback9 * This,
            /* [in] */ ClassID classId);
        
        HRESULT ( STDMETHODCALLTYPE *ClassLoadFinished )( 
            ICorProfilerCallback9 * This,
            /* [in] */ ClassID classId,
            /* [in] */ HRESULT hrStatus);
        
        HRESULT ( STDMETHODCALLTYPE *ClassUnloadStarted )( 
            ICorProfilerCallback9 * This,
            /* [in] */ ClassID classId);
        
        HRESULT ( STDMETHODCALLTYPE *ClassUnloadFinished )( 
            ICorProfilerCallback9 * This,
            /* [in] */ ClassID classId,
            /* [in] */ HRESULT hrStatus);
        
        HRESULT ( STDMETHODCALLTYPE *FunctionUnloadStarted )( 
            ICorProfilerCallback9 * This,
            /* [in] */ FunctionID functionId);
        
        HRESULT ( STDMETHODCALLTYPE *JITCompilationStarted )( 
            ICorProfilerCallback9 * This,
            /* [in] */ FunctionID functionId,
            /* [in] */ BOOL fIsSafeToBlock);
        
        HRESULT ( STDMETHODCALLTYPE *JITCompilationFinished )( 
            ICorProfilerCallback9 * This,
            /* [in] */ FunctionID functionId,
            /* [in] */ HRESULT hrStatus,
            /* [in] */ BOOL fIsSafeToBlock);
        
        HRESULT ( STDMETHODCALLTYPE *JITCachedFunctionSearchStarted )( 
            ICorProfilerCallback9 * This,
            /* [in] */ FunctionID functionId,
            /* [out] */ BOOL *pbUseCachedFunction);
        
        HRESULT ( STDMETHODCALLTYPE *JITCachedFunctionSearchFinished )( 
            ICorProfilerCallback9 * This,
            /* [in] */ FunctionID functionId,
            /* [in] */ COR_PRF_JIT_CACHE result);
        
        HRESULT ( STDMETHODCALLTYPE *JITFunctionPitched )( 
            ICorProfilerCallback9 * This,
            /* [in] */ FunctionID functionId);
        
        HRESULT ( STDMETHODCALLTYPE *JITInlining )( 
            ICorProfilerCallback9 * This,
            /* [in] */ FunctionID callerId,
            /* [in] */ FunctionID calleeId,
            /* [out] */ BOOL *pfShouldInline);
        
        HRESULT ( STDMETHODCALLTYPE *ThreadCreated )( 
            ICorProfilerCallback9 * This,
            /* [in] */ ThreadID threadId);
        
        HRESULT ( STDMETHODCALLTYPE *ThreadDestroyed )( 
            ICorProfilerCallback9 * This,
            /* [in] */ ThreadID threadId);
        
        HRESULT ( STDMETHODCALLTYPE *ThreadAssignedToOSThread )( 
            ICorProfilerCallback9 * This,
            /* [in] */ ThreadID managedThreadId,
            /* [in] */ DWORD osThreadId);
        
        HRESULT ( STDMETHODCALLTYPE *RemotingClientInvocationStarted )( 
            ICorProfilerCallback9 * This);
        
        HRESULT ( STDMETHODCALLTYPE *RemotingClientSendingMessage )( 
            ICorProfilerCallback9 * This,
            /* [in] */ GUID *pCookie,
            /* [in] */ BOOL fIsAsync);
        
        HRESULT ( STDMETHODCALLTYPE *RemotingClientReceivingReply )( 
            ICorProfilerCallback9 * This,
            /* [in] */ GUID *pCookie,
            /* [in] */ BOOL fIsAsync);
        
        HRESULT ( STDMETHODCALLTYPE *RemotingClientInvocationFinished )( 
            ICorProfilerCallback9 * This);
        
        HRESULT ( STDMETHODCALLTYPE *RemotingServerReceivingMessage )( 
            ICorProfilerCallback9 * This,
            /* [in] */ GUID *pCookie,
            /* [in] */ BOOL fIsAsync);
        
        HRESULT ( STDMETHODCALLTYPE *RemotingServerInvocationStarted )( 
            ICorProfilerCallback9 * This);
        
        HRESULT ( STDMETHODCALLTYPE *RemotingServerInvocationReturned )( 
            ICorProfilerCallback9 * This);
        
        HRESULT ( STDMETHODCALLTYPE *RemotingServerSendingReply )( 
            ICorProfilerCallback9 * This,
            /* [in] */ GUID *pCookie,
            /* [in] */ BOOL fIsAsync);
        
        HRESULT ( STDMETHODCALLTYPE *UnmanagedToManagedTransition )( 
            ICorProfilerCallback9 * This,
            /* [in] */ FunctionID functionId,
            /* [in] */ COR_PRF_TRANSITION_REASON reason);
        
        HRESULT ( STDMETHODCALLTYPE *ManagedToUnmanagedTransition )( 
            ICorProfilerCallback9 * This,
            /* [in] */ FunctionID functionId,
            /* [in] */ COR_PRF_TRANSITION_REASON reason);
        
        HRESULT ( STDMETHODCALLTYPE *RuntimeSuspendStarted )( 
            ICorProfilerCallback9 * This,
            /* [in] */ COR_PRF_SUSPEND_REASON suspendReason);
        
        HRESULT ( STDMETHODCALLTYPE *RuntimeSuspendFinished )( 
            ICorProfilerCallback9 * This);
        
        HRESULT ( STDMETHODCALLTYPE *RuntimeSuspendAborted )( 
            ICorProfilerCallback9 * This);
        
        HRESULT ( STDMETHODCALLTYPE *RuntimeResumeStarted )( 
            ICorProfilerCallback9 * This);
        
        HRESULT ( STDMETHODCALLTYPE *RuntimeResumeFinished )( 
            ICorProfilerCallback9 * This);
        
        HRESULT ( STDMETHODCALLTYPE *RuntimeThreadSuspended )( 
            ICorProfilerCallback9 * This,
            /* [in] */ ThreadID threadId);
        
        HRESULT ( STDMETHODCALLTYPE *RuntimeThreadResumed )( 
            ICorProfilerCallback9 * This,
            /* [in] */ ThreadID threadId);
        
        HRESULT ( STDMETHODCALLTYPE *MovedReferences )( 
            ICorProfilerCallback9 * This,
            /* [in] */ ULONG cMovedObjectIDRanges,
            /* [size_is][in] */ ObjectID oldObjectIDRangeStart[  ],
            /* [size_is][in] */ ObjectID newObjectIDRangeStart[  ],
            /* [size_is][in] */ ULONG cObjectIDRangeLength[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *ObjectAllocated )( 
            ICorProfilerCallback9 * This,
            /* [in] */ ObjectID objectId,
            /* [in] */ ClassID classId);
        
        HRESULT ( STDMETHODCALLTYPE *ObjectsAllocatedByClass )( 
            ICorProfilerCallback9 * This,
            /* [in] */ ULONG cClassCount,
            /* [size_is][in] */ ClassID classIds[  ],
            /* [size_is][in] */ ULONG cObjects[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *ObjectReferences )( 
            ICorProfilerCallback9 * This,
            /* [in] */ ObjectID objectId,
            /* [in] */ ClassID classId,
            /* [in] */ ULONG cObjectRefs,
            /* [size_is][in] */ ObjectID objectRefIds[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *RootReferences )( 
            ICorProfilerCallback9 * This,
            /* [in] */ ULONG cRootRefs,
            /* [size_is][in] */ ObjectID rootRefIds[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *ExceptionThrown )( 
            ICorProfilerCallback9 * This,
            /* [in] */ ObjectID thrownObjectId);
        
        HRESULT ( STDMETHODCALLTYPE *ExceptionSearchFunctionEnter )( 
            ICorProfilerCallback9 * This,
            /* [in] */ FunctionID functionId);
        
        HRESULT ( STDMETHODCALLTYPE *ExceptionSearchFunctionLeave )( 
            ICorProfilerCallback9 * This);
        
        HRESULT ( STDMETHODCALLTYPE *ExceptionSearchFilterEnter )( 
            ICorProfilerCallback9 * This,
            /* [in] */ FunctionID functionId);
        
        HRESULT ( STDMETHODCALLTYPE *ExceptionSearchFilterLeave )( 
            ICorProfilerCallback9 * This);
        
        HRESULT ( STDMETHODCALLTYPE *ExceptionSearchCatcherFound )( 
            ICorProfilerCallback9 * This,
            /* [in] */ FunctionID functionId);
        
        HRESULT ( STDMETHODCALLTYPE *ExceptionOSHandlerEnter )( 
            ICorProfilerCallback9 * This,
            /* [in] */ UINT_PTR __unused);
        
        HRESULT ( STDMETHODCALLTYPE *ExceptionOSHandlerLeave )( 
            ICorProfilerCallback9 * This,
            /* [in] */ UINT_PTR __unused);
        
        HRESULT ( STDMETHODCALLTYPE *ExceptionUnwindFunctionEnter )( 
            ICorProfilerCallback9 * This,
            /* [in] */ FunctionID functionId);
        
        HRESULT ( STDMETHODCALLTYPE *ExceptionUnwindFunctionLeave )( 
            ICorProfilerCallback9 * This);
        
        HRESULT ( STDMETHODCALLTYPE *ExceptionUnwindFinallyEnter )( 
            ICorProfilerCallback9 * This,
            /* [in] */ FunctionID functionId);
        
        HRESULT ( STDMETHODCALLTYPE *ExceptionUnwindFinallyLeave )( 
            ICorProfilerCallback9 * This);
        
        HRESULT ( STDMETHODCALLTYPE *ExceptionCatcherEnter )( 
            ICorProfilerCallback9 * This,
            /* [in] */ FunctionID functionId,
            /* [in] */ ObjectID objectId);
        
        HRESULT ( STDMETHODCALLTYPE *ExceptionCatcherLeave )( 
            ICorProfilerCallback9 * This);
        
        HRESULT ( STDMETHODCALLTYPE *COMClassicVTableCreated )( 
            ICorProfilerCallback9 * This,
            /* [in] */ ClassID wrappedClassId,
            /* [in] */ REFGUID implementedIID,
            /* [in] */ void *pVTable,
            /* [in] */ ULONG cSlots);
        
        HRESULT ( STDMETHODCALLTYPE *COMClassicVTableDestroyed )( 
            ICorProfilerCallback9 * This,
            /* [in] */ ClassID wrappedClassId,
            /* [in] */ REFGUID implementedIID,
            /* [in] */ void *pVTable);
        
        HRESULT ( STDMETHODCALLTYPE *ExceptionCLRCatcherFound )( 
            ICorProfilerCallback9 * This);
        
        HRESULT ( STDMETHODCALLTYPE *ExceptionCLRCatcherExecute )( 
            ICorProfilerCallback9 * This);
        
        HRESULT ( STDMETHODCALLTYPE *ThreadNameChanged )( 
            ICorProfilerCallback9 * This,
            /* [in] */ ThreadID threadId,
            /* [in] */ ULONG cchName,
            /* [annotation][in] */ 
            _In_reads_opt_(cchName)  WCHAR name[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *GarbageCollectionStarted )( 
            ICorProfilerCallback9 * This,
            /* [in] */ int cGenerations,
            /* [size_is][in] */ BOOL generationCollected[  ],
            /* [in] */ COR_PRF_GC_REASON reason);
        
        HRESULT ( STDMETHODCALLTYPE *SurvivingReferences )( 
            ICorProfilerCallback9 * This,
            /* [in] */ ULONG cSurvivingObjectIDRanges,
            /* [size_is][in] */ ObjectID objectIDRangeStart[  ],
            /* [size_is][in] */ ULONG cObjectIDRangeLength[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *GarbageCollectionFinished )( 
            ICorProfilerCallback9 * This);
        
        HRESULT ( STDMETHODCALLTYPE *FinalizeableObjectQueued )( 
            ICorProfilerCallback9 * This,
            /* [in] */ DWORD finalizerFlags,
            /* [in] */ ObjectID objectID);
        
        HRESULT ( STDMETHODCALLTYPE *RootReferences2 )( 
            ICorProfilerCallback9 * This,
            /* [in] */ ULONG cRootRefs,
            /* [size_is][in] */ ObjectID rootRefIds[  ],
            /* [size_is][in] */ COR_PRF_GC_ROOT_KIND rootKinds[  ],
            /* [size_is][in] */ COR_PRF_GC_ROOT_FLAGS rootFlags[  ],
            /* [size_is][in] */ UINT_PTR rootIds[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *HandleCreated )( 
            ICorProfilerCallback9 * This,
            /* [in] */ GCHandleID handleId,
            /* [in] */ ObjectID initialObjectId);
        
        HRESULT ( STDMETHODCALLTYPE *HandleDestroyed )( 
            ICorProfilerCallback9 * This,
            /* [in] */ GCHandleID handleId);
        
        HRESULT ( STDMETHODCALLTYPE *InitializeForAttach )( 
            ICorProfilerCallback9 * This,
            /* [in] */ IUnknown *pCorProfilerInfoUnk,
            /* [in] */ void *pvClientData,
            /* [in] */ UINT cbClientData);
        
        HRESULT ( STDMETHODCALLTYPE *ProfilerAttachComplete )( 
            ICorProfilerCallback9 * This);
        
        HRESULT ( STDMETHODCALLTYPE *ProfilerDetachSucceeded )( 
            ICorProfilerCallback9 * This);
        
        HRESULT ( STDMETHODCALLTYPE *ReJITCompilationStarted )( 
            ICorProfilerCallback9 * This,
            /* [in] */ FunctionID functionId,
            /* [in] */ ReJITID rejitId,
            /* [in] */ BOOL fIsSafeToBlock);
        
        HRESULT ( STDMETHODCALLTYPE *GetReJITParameters )( 
            ICorProfilerCallback9 * This,
            /* [in] */ ModuleID moduleId,
            /* [in] */ mdMethodDef methodId,
            /* [in] */ ICorProfilerFunctionControl *pFunctionControl);
        
        HRESULT ( STDMETHODCALLTYPE *ReJITCompilationFinished )( 
            ICorProfilerCallback9 * This,
            /* [in] */ FunctionID functionId,
            /* [in] */ ReJITID rejitId,
            /* [in] */ HRESULT hrStatus,
            /* [in] */ BOOL fIsSafeToBlock);
        
        HRESULT ( STDMETHODCALLTYPE *ReJITError )( 
            ICorProfilerCallback9 * This,
            /* [in] */ ModuleID moduleId,
            /* [in] */ mdMethodDef methodId,
            /* [in] */ FunctionID functionId,
            /* [in] */ HRESULT hrStatus);
        
        HRESULT ( STDMETHODCALLTYPE *MovedReferences2 )( 
            ICorProfilerCallback9 * This,
            /* [in] */ ULONG cMovedObjectIDRanges,
            /* [size_is][in] */ ObjectID oldObjectIDRangeStart[  ],
            /* [size_is][in] */ ObjectID newObjectIDRangeStart[  ],
            /* [size_is][in] */ SIZE_T cObjectIDRangeLength[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *SurvivingReferences2 )( 
            ICorProfilerCallback9 * This,
            /* [in] */ ULONG cSurvivingObjectIDRanges,
            /* [size_is][in] */ ObjectID objectIDRangeStart[  ],
            /* [size_is][in] */ SIZE_T cObjectIDRangeLength[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *ConditionalWeakTableElementReferences )( 
            ICorProfilerCallback9 * This,
            /* [in] */ ULONG cRootRefs,
            /* [size_is][in] */ ObjectID keyRefIds[  ],
            /* [size_is][in] */ ObjectID valueRefIds[  ],
            /* [size_is][in] */ GCHandleID rootIds[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *GetAssemblyReferences )( 
            ICorProfilerCallback9 * This,
            /* [string][in] */ const WCHAR *wszAssemblyPath,
            /* [in] */ ICorProfilerAssemblyReferenceProvider *pAsmRefProvider);
        
        HRESULT ( STDMETHODCALLTYPE *ModuleInMemorySymbolsUpdated )( 
            ICorProfilerCallback9 * This,
            ModuleID moduleId);
        
        HRESULT ( STDMETHODCALLTYPE *DynamicMethodJITCompilationStarted )( 
            ICorProfilerCallback9 * This,
            /* [in] */ FunctionID functionId,
            /* [in] */ BOOL fIsSafeToBlock,
            /* [in] */ LPCBYTE pILHeader,
            /* [in] */ ULONG cbILHeader);
        
        HRESULT ( STDMETHODCALLTYPE *DynamicMethodJITCompilationFinished )( 
            ICorProfilerCallback9 * This,
            /* [in] */ FunctionID functionId,
            /* [in] */ HRESULT hrStatus,
            /* [in] */ BOOL fIsSafeToBlock);
        
        HRESULT ( STDMETHODCALLTYPE *DynamicMethodUnloaded )( 
            ICorProfilerCallback9 * This,
            /* [in] */ FunctionID functionId);
        
        END_INTERFACE
    } ICorProfilerCallback9Vtbl;

    interface ICorProfilerCallback9
    {
        CONST_VTBL struct ICorProfilerCallback9Vtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define ICorProfilerCallback9_QueryInterface(This,riid,ppvObject)   \
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define ICorProfilerCallback9_AddRef(This)  \
    ( (This)->lpVtbl -> AddRef(This) ) 

#define ICorProfilerCallback9_Release(This) \
    ( (This)->lpVtbl -> Release(This) ) 


#define ICorProfilerCallback9_Initialize(This,pICorProfilerInfoUnk) \
    ( (This)->lpVtbl -> Initialize(This,pICorProfilerInfoUnk) ) 

#define ICorProfilerCallback9_Shutdown(This)    \
    ( (This)->lpVtbl -> Shutdown(This) ) 

#define ICorProfilerCallback9_AppDomainCreationStarted(This,appDomainId)    \
    ( (This)->lpVtbl -> AppDomainCreationStarted(This,appDomainId) ) 

#define ICorProfilerCallback9_AppDomainCreationFinished(This,appDomainId,hrStatus)  \
    ( (This)->lpVtbl -> AppDomainCreationFinished(This,appDomainId,hrStatus) ) 

#define ICorProfilerCallback9_AppDomainShutdownStarted(This,appDomainId)    \
    ( (This)->lpVtbl -> AppDomainShutdownStarted(This,appDomainId) ) 

#define ICorProfilerCallback9_AppDomainShutdownFinished(This,appDomainId,hrStatus)  \
    ( (This)->lpVtbl -> AppDomainShutdownFinished(This,appDomainId,hrStatus) ) 

#define ICorProfilerCallback9_AssemblyLoadStarted(This,assemblyId)  \
    ( (This)->lpVtbl -> AssemblyLoadStarted(This,assemblyId) ) 

#define ICorProfilerCallback9_AssemblyLoadFinished(This,assemblyId,hrStatus)    \
    ( (This)->lpVtbl -> AssemblyLoadFinished(This,assemblyId,hrStatus) ) 

#define ICorProfilerCallback9_AssemblyUnloadStarted(This,assemblyId)    \
    ( (This)->lpVtbl -> AssemblyUnloadStarted(This,assemblyId) ) 

#define ICorProfilerCallback9_AssemblyUnloadFinished(This,assemblyId,hrStatus)  \
    ( (This)->lpVtbl -> AssemblyUnloadFinished(This,assemblyId,hrStatus) ) 

#define ICorProfilerCallback9_ModuleLoadStarted(This,moduleId)  \
    ( (This)->lpVtbl -> ModuleLoadStarted(This,moduleId) ) 

#define ICorProfilerCallback9_ModuleLoadFinished(This,moduleId,hrStatus)    \
    ( (This)->lpVtbl -> ModuleLoadFinished(This,moduleId,hrStatus) ) 

#define ICorProfilerCallback9_ModuleUnloadStarted(This,moduleId)    \
    ( (This)->lpVtbl -> ModuleUnloadStarted(This,moduleId) ) 

#define ICorProfilerCallback9_ModuleUnloadFinished(This,moduleId,hrStatus)  \
    ( (This)->lpVtbl -> ModuleUnloadFinished(This,moduleId,hrStatus) ) 

#define ICorProfilerCallback9_ModuleAttachedToAssembly(This,moduleId,AssemblyId)    \
    ( (This)->lpVtbl -> ModuleAttachedToAssembly(This,moduleId,AssemblyId) ) 

#define ICorProfilerCallback9_ClassLoadStarted(This,classId)    \
    ( (This)->lpVtbl -> ClassLoadStarted(This,classId) ) 

#define ICorProfilerCallback9_ClassLoadFinished(This,classId,hrStatus)  \
    ( (This)->lpVtbl -> ClassLoadFinished(This,classId,hrStatus) ) 

#define ICorProfilerCallback9_ClassUnloadStarted(This,classId)  \
    ( (This)->lpVtbl -> ClassUnloadStarted(This,classId) ) 

#define ICorProfilerCallback9_ClassUnloadFinished(This,classId,hrStatus)    \
    ( (This)->lpVtbl -> ClassUnloadFinished(This,classId,hrStatus) ) 

#define ICorProfilerCallback9_FunctionUnloadStarted(This,functionId)    \
    ( (This)->lpVtbl -> FunctionUnloadStarted(This,functionId) ) 

#define ICorProfilerCallback9_JITCompilationStarted(This,functionId,fIsSafeToBlock) \
    ( (This)->lpVtbl -> JITCompilationStarted(This,functionId,fIsSafeToBlock) ) 

#define ICorProfilerCallback9_JITCompilationFinished(This,functionId,hrStatus,fIsSafeToBlock)   \
    ( (This)->lpVtbl -> JITCompilationFinished(This,functionId,hrStatus,fIsSafeToBlock) ) 

#define ICorProfilerCallback9_JITCachedFunctionSearchStarted(This,functionId,pbUseCachedFunction)   \
    ( (This)->lpVtbl -> JITCachedFunctionSearchStarted(This,functionId,pbUseCachedFunction) ) 

#define ICorProfilerCallback9_JITCachedFunctionSearchFinished(This,functionId,result)   \
    ( (This)->lpVtbl -> JITCachedFunctionSearchFinished(This,functionId,result) ) 

#define ICorProfilerCallback9_JITFunctionPitched(This,functionId)   \
    ( (This)->lpVtbl -> JITFunctionPitched(This,functionId) ) 

#define ICorProfilerCallback9_JITInlining(This,callerId,calleeId,pfShouldInline)    \
    ( (This)->lpVtbl -> JITInlining(This,callerId,calleeId,pfShouldInline) ) 

#define ICorProfilerCallback9_ThreadCreated(This,threadId)  \
    ( (This)->lpVtbl -> ThreadCreated(This,threadId) ) 

#define ICorProfilerCallback9_ThreadDestroyed(This,threadId)    \
    ( (This)->lpVtbl -> ThreadDestroyed(This,threadId) ) 

#define ICorProfilerCallback9_ThreadAssignedToOSThread(This,managedThreadId,osThreadId) \
    ( (This)->lpVtbl -> ThreadAssignedToOSThread(This,managedThreadId,osThreadId) ) 

#define ICorProfilerCallback9_RemotingClientInvocationStarted(This) \
    ( (This)->lpVtbl -> RemotingClientInvocationStarted(This) ) 

#define ICorProfilerCallback9_RemotingClientSendingMessage(This,pCookie,fIsAsync)   \
    ( (This)->lpVtbl -> RemotingClientSendingMessage(This,pCookie,fIsAsync) ) 

#define ICorProfilerCallback9_RemotingClientReceivingReply(This,pCookie,fIsAsync)   \
    ( (This)->lpVtbl -> RemotingClientReceivingReply(This,pCookie,fIsAsync) ) 

#define ICorProfilerCallback9_RemotingClientInvocationFinished(This)    \
    ( (This)->lpVtbl -> RemotingClientInvocationFinished(This) ) 

#define ICorProfilerCallback9_RemotingServerReceivingMessage(This,pCookie,fIsAsync) \
    ( (This)->lpVtbl -> RemotingServerReceivingMessage(This,pCookie,fIsAsync) ) 

#define ICorProfilerCallback9_RemotingServerInvocationStarted(This) \
    ( (This)->lpVtbl -> RemotingServerInvocationStarted(This) ) 

#define ICorProfilerCallback9_RemotingServerInvocationReturned(This)    \
    ( (This)->lpVtbl -> RemotingServerInvocationReturned(This) ) 

#define ICorProfilerCallback9_RemotingServerSendingReply(This,pCookie,fIsAsync) \
    ( (This)->lpVtbl -> RemotingServerSendingReply(This,pCookie,fIsAsync) ) 

#define ICorProfilerCallback9_UnmanagedToManagedTransition(This,functionId,reason)  \
    ( (This)->lpVtbl -> UnmanagedToManagedTransition(This,functionId,reason) ) 

#define ICorProfilerCallback9_ManagedToUnmanagedTransition(This,functionId,reason)  \
    ( (This)->lpVtbl -> ManagedToUnmanagedTransition(This,functionId,reason) ) 

#define ICorProfilerCallback9_RuntimeSuspendStarted(This,suspendReason) \
    ( (This)->lpVtbl -> RuntimeSuspendStarted(This,suspendReason) ) 

#define ICorProfilerCallback9_RuntimeSuspendFinished(This)  \
    ( (This)->lpVtbl -> RuntimeSuspendFinished(This) ) 

#define ICorProfilerCallback9_RuntimeSuspendAborted(This)   \
    ( (This)->lpVtbl -> RuntimeSuspendAborted(This) ) 

#define ICorProfilerCallback9_RuntimeResumeStarted(This)    \
    ( (This)->lpVtbl -> RuntimeResumeStarted(This) ) 

#define ICorProfilerCallback9_RuntimeResumeFinished(This)   \
    ( (This)->lpVtbl -> RuntimeResumeFinished(This) ) 

#define ICorProfilerCallback9_RuntimeThreadSuspended(This,threadId) \
    ( (This)->lpVtbl -> RuntimeThreadSuspended(This,threadId) ) 

#define ICorProfilerCallback9_RuntimeThreadResumed(This,threadId)   \
    ( (This)->lpVtbl -> RuntimeThreadResumed(This,threadId) ) 

#define ICorProfilerCallback9_MovedReferences(This,cMovedObjectIDRanges,oldObjectIDRangeStart,newObjectIDRangeStart,cObjectIDRangeLength)   \
    ( (This)->lpVtbl -> MovedReferences(This,cMovedObjectIDRanges,oldObjectIDRangeStart,newObjectIDRangeStart,cObjectIDRangeLength) ) 

#define ICorProfilerCallback9_ObjectAllocated(This,objectId,classId)    \
    ( (This)->lpVtbl -> ObjectAllocated(This,objectId,classId) ) 

#define ICorProfilerCallback9_ObjectsAllocatedByClass(This,cClassCount,classIds,cObjects)   \
    ( (This)->lpVtbl -> ObjectsAllocatedByClass(This,cClassCount,classIds,cObjects) ) 

#define ICorProfilerCallback9_ObjectReferences(This,objectId,classId,cObjectRefs,objectRefIds)  \
    ( (This)->lpVtbl -> ObjectReferences(This,objectId,classId,cObjectRefs,objectRefIds) ) 

#define ICorProfilerCallback9_RootReferences(This,cRootRefs,rootRefIds) \
    ( (This)->lpVtbl -> RootReferences(This,cRootRefs,rootRefIds) ) 

#define ICorProfilerCallback9_ExceptionThrown(This,thrownObjectId)  \
    ( (This)->lpVtbl -> ExceptionThrown(This,thrownObjectId) ) 

#define ICorProfilerCallback9_ExceptionSearchFunctionEnter(This,functionId) \
    ( (This)->lpVtbl -> ExceptionSearchFunctionEnter(This,functionId) ) 

#define ICorProfilerCallback9_ExceptionSearchFunctionLeave(This)    \
    ( (This)->lpVtbl -> ExceptionSearchFunctionLeave(This) ) 

#define ICorProfilerCallback9_ExceptionSearchFilterEnter(This,functionId)   \
    ( (This)->lpVtbl -> ExceptionSearchFilterEnter(This,functionId) ) 

#define ICorProfilerCallback9_ExceptionSearchFilterLeave(This)  \
    ( (This)->lpVtbl -> ExceptionSearchFilterLeave(This) ) 

#define ICorProfilerCallback9_ExceptionSearchCatcherFound(This,functionId)  \
    ( (This)->lpVtbl -> ExceptionSearchCatcherFound(This,functionId) ) 

#define ICorProfilerCallback9_ExceptionOSHandlerEnter(This,__unused)    \
    ( (This)->lpVtbl -> ExceptionOSHandlerEnter(This,__unused) ) 

#define ICorProfilerCallback9_ExceptionOSHandlerLeave(This,__unused)    \
    ( (This)->lpVtbl -> ExceptionOSHandlerLeave(This,__unused) ) 

#define ICorProfilerCallback9_ExceptionUnwindFunctionEnter(This,functionId) \
    ( (This)->lpVtbl -> ExceptionUnwindFunctionEnter(This,functionId) ) 

#define ICorProfilerCallback9_ExceptionUnwindFunctionLeave(This)    \
    ( (This)->lpVtbl -> ExceptionUnwindFunctionLeave(This) ) 

#define ICorProfilerCallback9_ExceptionUnwindFinallyEnter(This,functionId)  \
    ( (This)->lpVtbl -> ExceptionUnwindFinallyEnter(This,functionId) ) 

#define ICorProfilerCallback9_ExceptionUnwindFinallyLeave(This) \
    ( (This)->lpVtbl -> ExceptionUnwindFinallyLeave(This) ) 

#define ICorProfilerCallback9_ExceptionCatcherEnter(This,functionId,objectId)   \
    ( (This)->lpVtbl -> ExceptionCatcherEnter(This,functionId,objectId) ) 

#define ICorProfilerCallback9_ExceptionCatcherLeave(This)   \
    ( (This)->lpVtbl -> ExceptionCatcherLeave(This) ) 

#define ICorProfilerCallback9_COMClassicVTableCreated(This,wrappedClassId,implementedIID,pVTable,cSlots)    \
    ( (This)->lpVtbl -> COMClassicVTableCreated(This,wrappedClassId,implementedIID,pVTable,cSlots) ) 

#define ICorProfilerCallback9_COMClassicVTableDestroyed(This,wrappedClassId,implementedIID,pVTable) \
    ( (This)->lpVtbl -> COMClassicVTableDestroyed(This,wrappedClassId,implementedIID,pVTable) ) 

#define ICorProfilerCallback9_ExceptionCLRCatcherFound(This)    \
    ( (This)->lpVtbl -> ExceptionCLRCatcherFound(This) ) 

#define ICorProfilerCallback9_ExceptionCLRCatcherExecute(This)  \
    ( (This)->lpVtbl -> ExceptionCLRCatcherExecute(This) ) 


#define ICorProfilerCallback9_ThreadNameChanged(This,threadId,cchName,name) \
    ( (This)->lpVtbl -> ThreadNameChanged(This,threadId,cchName,name) ) 

#define ICorProfilerCallback9_GarbageCollectionStarted(This,cGenerations,generationCollected,reason)    \
    ( (This)->lpVtbl -> GarbageCollectionStarted(This,cGenerations,generationCollected,reason) ) 

#define ICorProfilerCallback9_SurvivingReferences(This,cSurvivingObjectIDRanges,objectIDRangeStart,cObjectIDRangeLength)    \
    ( (This)->lpVtbl -> SurvivingReferences(This,cSurvivingObjectIDRanges,objectIDRangeStart,cObjectIDRangeLength) ) 

#define ICorProfilerCallback9_GarbageCollectionFinished(This)   \
    ( (This)->lpVtbl -> GarbageCollectionFinished(This) ) 

#define ICorProfilerCallback9_FinalizeableObjectQueued(This,finalizerFlags,objectID)    \
    ( (This)->lpVtbl -> FinalizeableObjectQueued(This,finalizerFlags,objectID) ) 

#define ICorProfilerCallback9_RootReferences2(This,cRootRefs,rootRefIds,rootKinds,rootFlags,rootIds)    \
    ( (This)->lpVtbl -> RootReferences2(This,cRootRefs,rootRefIds,rootKinds,rootFlags,rootIds) ) 

#define ICorProfilerCallback9_HandleCreated(This,handleId,initialObjectId)  \
    ( (This)->lpVtbl -> HandleCreated(This,handleId,initialObjectId) ) 

#define ICorProfilerCallback9_HandleDestroyed(This,handleId)    \
    ( (This)->lpVtbl -> HandleDestroyed(This,handleId) ) 


#define ICorProfilerCallback9_InitializeForAttach(This,pCorProfilerInfoUnk,pvClientData,cbClientData)   \
    ( (This)->lpVtbl -> InitializeForAttach(This,pCorProfilerInfoUnk,pvClientData,cbClientData) ) 

#define ICorProfilerCallback9_ProfilerAttachComplete(This)  \
    ( (This)->lpVtbl -> ProfilerAttachComplete(This) ) 

#define ICorProfilerCallback9_ProfilerDetachSucceeded(This) \
    ( (This)->lpVtbl -> ProfilerDetachSucceeded(This) ) 


#define ICorProfilerCallback9_ReJITCompilationStarted(This,functionId,rejitId,fIsSafeToBlock)   \
    ( (This)->lpVtbl -> ReJITCompilationStarted(This,functionId,rejitId,fIsSafeToBlock) ) 

#define ICorProfilerCallback9_GetReJITParameters(This,moduleId,methodId,pFunctionControl)   \
    ( (This)->lpVtbl -> GetReJITParameters(This,moduleId,methodId,pFunctionControl) ) 

#define ICorProfilerCallback9_ReJITCompilationFinished(This,functionId,rejitId,hrStatus,fIsSafeToBlock) \
    ( (This)->lpVtbl -> ReJITCompilationFinished(This,functionId,rejitId,hrStatus,fIsSafeToBlock) ) 

#define ICorProfilerCallback9_ReJITError(This,moduleId,methodId,functionId,hrStatus)    \
    ( (This)->lpVtbl -> ReJITError(This,moduleId,methodId,functionId,hrStatus) ) 

#define ICorProfilerCallback9_MovedReferences2(This,cMovedObjectIDRanges,oldObjectIDRangeStart,newObjectIDRangeStart,cObjectIDRangeLength)  \
    ( (This)->lpVtbl -> MovedReferences2(This,cMovedObjectIDRanges,oldObjectIDRangeStart,newObjectIDRangeStart,cObjectIDRangeLength) ) 

#define ICorProfilerCallback9_SurvivingReferences2(This,cSurvivingObjectIDRanges,objectIDRangeStart,cObjectIDRangeLength)   \
    ( (This)->lpVtbl -> SurvivingReferences2(This,cSurvivingObjectIDRanges,objectIDRangeStart,cObjectIDRangeLength) ) 


#define ICorProfilerCallback9_ConditionalWeakTableElementReferences(This,cRootRefs,keyRefIds,valueRefIds,rootIds)   \
    ( (This)->lpVtbl -> ConditionalWeakTableElementReferences(This,cRootRefs,keyRefIds,valueRefIds,rootIds) ) 


#define ICorProfilerCallback9_GetAssemblyReferences(This,wszAssemblyPath,pAsmRefProvider)   \
    ( (This)->lpVtbl -> GetAssemblyReferences(This,wszAssemblyPath,pAsmRefProvider) ) 


#define ICorProfilerCallback9_ModuleInMemorySymbolsUpdated(This,moduleId)   \
    ( (This)->lpVtbl -> ModuleInMemorySymbolsUpdated(This,moduleId) ) 


#define ICorProfilerCallback9_DynamicMethodJITCompilationStarted(This,functionId,fIsSafeToBlock,pILHeader,cbILHeader)   \
    ( (This)->lpVtbl -> DynamicMethodJITCompilationStarted(This,functionId,fIsSafeToBlock,pILHeader,cbILHeader) ) 

#define ICorProfilerCallback9_DynamicMethodJITCompilationFinished(This,functionId,hrStatus,fIsSafeToBlock)  \
    ( (This)->lpVtbl -> DynamicMethodJITCompilationFinished(This,functionId,hrStatus,fIsSafeToBlock) ) 


#define ICorProfilerCallback9_DynamicMethodUnloaded(This,functionId)    \
    ( (This)->lpVtbl -> DynamicMethodUnloaded(This,functionId) ) 

#endif /* COBJMACROS */


#endif  /* C style interface */




#endif  /* __ICorProfilerCallback9_INTERFACE_DEFINED__ */


#ifndef __ICorProfilerCallback10_INTERFACE_DEFINED__
#define __ICorProfilerCallback10_INTERFACE_DEFINED__

/* interface ICorProfilerCallback10 */
/* [local][unique][uuid][object] */ 


EXTERN_C const IID IID_ICorProfilerCallback10;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("CEC5B60E-C69C-495F-87F6-84D28EE16FFB")
    ICorProfilerCallback10 : public ICorProfilerCallback9
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE EventPipeEventDelivered( 
            /* [in] */ EVENTPIPE_PROVIDER provider,
            /* [in] */ DWORD eventId,
            /* [in] */ DWORD eventVersion,
            /* [in] */ ULONG cbMetadataBlob,
            /* [size_is][in] */ LPCBYTE metadataBlob,
            /* [in] */ ULONG cbEventData,
            /* [size_is][in] */ LPCBYTE eventData,
            /* [in] */ LPCGUID pActivityId,
            /* [in] */ LPCGUID pRelatedActivityId,
            /* [in] */ ThreadID eventThread,
            /* [in] */ ULONG numStackFrames,
            /* [length_is][in] */ UINT_PTR stackFrames[  ]) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE EventPipeProviderCreated( 
            /* [in] */ EVENTPIPE_PROVIDER provider) = 0;
        
    };
    
    
#else   /* C style interface */

    typedef struct ICorProfilerCallback10Vtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            ICorProfilerCallback10 * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            ICorProfilerCallback10 * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            ICorProfilerCallback10 * This);
        
        HRESULT ( STDMETHODCALLTYPE *Initialize )( 
            ICorProfilerCallback10 * This,
            /* [in] */ IUnknown *pICorProfilerInfoUnk);
        
        HRESULT ( STDMETHODCALLTYPE *Shutdown )( 
            ICorProfilerCallback10 * This);
        
        HRESULT ( STDMETHODCALLTYPE *AppDomainCreationStarted )( 
            ICorProfilerCallback10 * This,
            /* [in] */ AppDomainID appDomainId);
        
        HRESULT ( STDMETHODCALLTYPE *AppDomainCreationFinished )( 
            ICorProfilerCallback10 * This,
            /* [in] */ AppDomainID appDomainId,
            /* [in] */ HRESULT hrStatus);
        
        HRESULT ( STDMETHODCALLTYPE *AppDomainShutdownStarted )( 
            ICorProfilerCallback10 * This,
            /* [in] */ AppDomainID appDomainId);
        
        HRESULT ( STDMETHODCALLTYPE *AppDomainShutdownFinished )( 
            ICorProfilerCallback10 * This,
            /* [in] */ AppDomainID appDomainId,
            /* [in] */ HRESULT hrStatus);
        
        HRESULT ( STDMETHODCALLTYPE *AssemblyLoadStarted )( 
            ICorProfilerCallback10 * This,
            /* [in] */ AssemblyID assemblyId);
        
        HRESULT ( STDMETHODCALLTYPE *AssemblyLoadFinished )( 
            ICorProfilerCallback10 * This,
            /* [in] */ AssemblyID assemblyId,
            /* [in] */ HRESULT hrStatus);
        
        HRESULT ( STDMETHODCALLTYPE *AssemblyUnloadStarted )( 
            ICorProfilerCallback10 * This,
            /* [in] */ AssemblyID assemblyId);
        
        HRESULT ( STDMETHODCALLTYPE *AssemblyUnloadFinished )( 
            ICorProfilerCallback10 * This,
            /* [in] */ AssemblyID assemblyId,
            /* [in] */ HRESULT hrStatus);
        
        HRESULT ( STDMETHODCALLTYPE *ModuleLoadStarted )( 
            ICorProfilerCallback10 * This,
            /* [in] */ ModuleID moduleId);
        
        HRESULT ( STDMETHODCALLTYPE *ModuleLoadFinished )( 
            ICorProfilerCallback10 * This,
            /* [in] */ ModuleID moduleId,
            /* [in] */ HRESULT hrStatus);
        
        HRESULT ( STDMETHODCALLTYPE *ModuleUnloadStarted )( 
            ICorProfilerCallback10 * This,
            /* [in] */ ModuleID moduleId);
        
        HRESULT ( STDMETHODCALLTYPE *ModuleUnloadFinished )( 
            ICorProfilerCallback10 * This,
            /* [in] */ ModuleID moduleId,
            /* [in] */ HRESULT hrStatus);
        
        HRESULT ( STDMETHODCALLTYPE *ModuleAttachedToAssembly )( 
            ICorProfilerCallback10 * This,
            /* [in] */ ModuleID moduleId,
            /* [in] */ AssemblyID AssemblyId);
        
        HRESULT ( STDMETHODCALLTYPE *ClassLoadStarted )( 
            ICorProfilerCallback10 * This,
            /* [in] */ ClassID classId);
        
        HRESULT ( STDMETHODCALLTYPE *ClassLoadFinished )( 
            ICorProfilerCallback10 * This,
            /* [in] */ ClassID classId,
            /* [in] */ HRESULT hrStatus);
        
        HRESULT ( STDMETHODCALLTYPE *ClassUnloadStarted )( 
            ICorProfilerCallback10 * This,
            /* [in] */ ClassID classId);
        
        HRESULT ( STDMETHODCALLTYPE *ClassUnloadFinished )( 
            ICorProfilerCallback10 * This,
            /* [in] */ ClassID classId,
            /* [in] */ HRESULT hrStatus);
        
        HRESULT ( STDMETHODCALLTYPE *FunctionUnloadStarted )( 
            ICorProfilerCallback10 * This,
            /* [in] */ FunctionID functionId);
        
        HRESULT ( STDMETHODCALLTYPE *JITCompilationStarted )( 
            ICorProfilerCallback10 * This,
            /* [in] */ FunctionID functionId,
            /* [in] */ BOOL fIsSafeToBlock);
        
        HRESULT ( STDMETHODCALLTYPE *JITCompilationFinished )( 
            ICorProfilerCallback10 * This,
            /* [in] */ FunctionID functionId,
            /* [in] */ HRESULT hrStatus,
            /* [in] */ BOOL fIsSafeToBlock);
        
        HRESULT ( STDMETHODCALLTYPE *JITCachedFunctionSearchStarted )( 
            ICorProfilerCallback10 * This,
            /* [in] */ FunctionID functionId,
            /* [out] */ BOOL *pbUseCachedFunction);
        
        HRESULT ( STDMETHODCALLTYPE *JITCachedFunctionSearchFinished )( 
            ICorProfilerCallback10 * This,
            /* [in] */ FunctionID functionId,
            /* [in] */ COR_PRF_JIT_CACHE result);
        
        HRESULT ( STDMETHODCALLTYPE *JITFunctionPitched )( 
            ICorProfilerCallback10 * This,
            /* [in] */ FunctionID functionId);
        
        HRESULT ( STDMETHODCALLTYPE *JITInlining )( 
            ICorProfilerCallback10 * This,
            /* [in] */ FunctionID callerId,
            /* [in] */ FunctionID calleeId,
            /* [out] */ BOOL *pfShouldInline);
        
        HRESULT ( STDMETHODCALLTYPE *ThreadCreated )( 
            ICorProfilerCallback10 * This,
            /* [in] */ ThreadID threadId);
        
        HRESULT ( STDMETHODCALLTYPE *ThreadDestroyed )( 
            ICorProfilerCallback10 * This,
            /* [in] */ ThreadID threadId);
        
        HRESULT ( STDMETHODCALLTYPE *ThreadAssignedToOSThread )( 
            ICorProfilerCallback10 * This,
            /* [in] */ ThreadID managedThreadId,
            /* [in] */ DWORD osThreadId);
        
        HRESULT ( STDMETHODCALLTYPE *RemotingClientInvocationStarted )( 
            ICorProfilerCallback10 * This);
        
        HRESULT ( STDMETHODCALLTYPE *RemotingClientSendingMessage )( 
            ICorProfilerCallback10 * This,
            /* [in] */ GUID *pCookie,
            /* [in] */ BOOL fIsAsync);
        
        HRESULT ( STDMETHODCALLTYPE *RemotingClientReceivingReply )( 
            ICorProfilerCallback10 * This,
            /* [in] */ GUID *pCookie,
            /* [in] */ BOOL fIsAsync);
        
        HRESULT ( STDMETHODCALLTYPE *RemotingClientInvocationFinished )( 
            ICorProfilerCallback10 * This);
        
        HRESULT ( STDMETHODCALLTYPE *RemotingServerReceivingMessage )( 
            ICorProfilerCallback10 * This,
            /* [in] */ GUID *pCookie,
            /* [in] */ BOOL fIsAsync);
        
        HRESULT ( STDMETHODCALLTYPE *RemotingServerInvocationStarted )( 
            ICorProfilerCallback10 * This);
        
        HRESULT ( STDMETHODCALLTYPE *RemotingServerInvocationReturned )( 
            ICorProfilerCallback10 * This);
        
        HRESULT ( STDMETHODCALLTYPE *RemotingServerSendingReply )( 
            ICorProfilerCallback10 * This,
            /* [in] */ GUID *pCookie,
            /* [in] */ BOOL fIsAsync);
        
        HRESULT ( STDMETHODCALLTYPE *UnmanagedToManagedTransition )( 
            ICorProfilerCallback10 * This,
            /* [in] */ FunctionID functionId,
            /* [in] */ COR_PRF_TRANSITION_REASON reason);
        
        HRESULT ( STDMETHODCALLTYPE *ManagedToUnmanagedTransition )( 
            ICorProfilerCallback10 * This,
            /* [in] */ FunctionID functionId,
            /* [in] */ COR_PRF_TRANSITION_REASON reason);
        
        HRESULT ( STDMETHODCALLTYPE *RuntimeSuspendStarted )( 
            ICorProfilerCallback10 * This,
            /* [in] */ COR_PRF_SUSPEND_REASON suspendReason);
        
        HRESULT ( STDMETHODCALLTYPE *RuntimeSuspendFinished )( 
            ICorProfilerCallback10 * This);
        
        HRESULT ( STDMETHODCALLTYPE *RuntimeSuspendAborted )( 
            ICorProfilerCallback10 * This);
        
        HRESULT ( STDMETHODCALLTYPE *RuntimeResumeStarted )( 
            ICorProfilerCallback10 * This);
        
        HRESULT ( STDMETHODCALLTYPE *RuntimeResumeFinished )( 
            ICorProfilerCallback10 * This);
        
        HRESULT ( STDMETHODCALLTYPE *RuntimeThreadSuspended )( 
            ICorProfilerCallback10 * This,
            /* [in] */ ThreadID threadId);
        
        HRESULT ( STDMETHODCALLTYPE *RuntimeThreadResumed )( 
            ICorProfilerCallback10 * This,
            /* [in] */ ThreadID threadId);
        
        HRESULT ( STDMETHODCALLTYPE *MovedReferences )( 
            ICorProfilerCallback10 * This,
            /* [in] */ ULONG cMovedObjectIDRanges,
            /* [size_is][in] */ ObjectID oldObjectIDRangeStart[  ],
            /* [size_is][in] */ ObjectID newObjectIDRangeStart[  ],
            /* [size_is][in] */ ULONG cObjectIDRangeLength[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *ObjectAllocated )( 
            ICorProfilerCallback10 * This,
            /* [in] */ ObjectID objectId,
            /* [in] */ ClassID classId);
        
        HRESULT ( STDMETHODCALLTYPE *ObjectsAllocatedByClass )( 
            ICorProfilerCallback10 * This,
            /* [in] */ ULONG cClassCount,
            /* [size_is][in] */ ClassID classIds[  ],
            /* [size_is][in] */ ULONG cObjects[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *ObjectReferences )( 
            ICorProfilerCallback10 * This,
            /* [in] */ ObjectID objectId,
            /* [in] */ ClassID classId,
            /* [in] */ ULONG cObjectRefs,
            /* [size_is][in] */ ObjectID objectRefIds[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *RootReferences )( 
            ICorProfilerCallback10 * This,
            /* [in] */ ULONG cRootRefs,
            /* [size_is][in] */ ObjectID rootRefIds[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *ExceptionThrown )( 
            ICorProfilerCallback10 * This,
            /* [in] */ ObjectID thrownObjectId);
        
        HRESULT ( STDMETHODCALLTYPE *ExceptionSearchFunctionEnter )( 
            ICorProfilerCallback10 * This,
            /* [in] */ FunctionID functionId);
        
        HRESULT ( STDMETHODCALLTYPE *ExceptionSearchFunctionLeave )( 
            ICorProfilerCallback10 * This);
        
        HRESULT ( STDMETHODCALLTYPE *ExceptionSearchFilterEnter )( 
            ICorProfilerCallback10 * This,
            /* [in] */ FunctionID functionId);
        
        HRESULT ( STDMETHODCALLTYPE *ExceptionSearchFilterLeave )( 
            ICorProfilerCallback10 * This);
        
        HRESULT ( STDMETHODCALLTYPE *ExceptionSearchCatcherFound )( 
            ICorProfilerCallback10 * This,
            /* [in] */ FunctionID functionId);
        
        HRESULT ( STDMETHODCALLTYPE *ExceptionOSHandlerEnter )( 
            ICorProfilerCallback10 * This,
            /* [in] */ UINT_PTR __unused);
        
        HRESULT ( STDMETHODCALLTYPE *ExceptionOSHandlerLeave )( 
            ICorProfilerCallback10 * This,
            /* [in] */ UINT_PTR __unused);
        
        HRESULT ( STDMETHODCALLTYPE *ExceptionUnwindFunctionEnter )( 
            ICorProfilerCallback10 * This,
            /* [in] */ FunctionID functionId);
        
        HRESULT ( STDMETHODCALLTYPE *ExceptionUnwindFunctionLeave )( 
            ICorProfilerCallback10 * This);
        
        HRESULT ( STDMETHODCALLTYPE *ExceptionUnwindFinallyEnter )( 
            ICorProfilerCallback10 * This,
            /* [in] */ FunctionID functionId);
        
        HRESULT ( STDMETHODCALLTYPE *ExceptionUnwindFinallyLeave )( 
            ICorProfilerCallback10 * This);
        
        HRESULT ( STDMETHODCALLTYPE *ExceptionCatcherEnter )( 
            ICorProfilerCallback10 * This,
            /* [in] */ FunctionID functionId,
            /* [in] */ ObjectID objectId);
        
        HRESULT ( STDMETHODCALLTYPE *ExceptionCatcherLeave )( 
            ICorProfilerCallback10 * This);
        
        HRESULT ( STDMETHODCALLTYPE *COMClassicVTableCreated )( 
            ICorProfilerCallback10 * This,
            /* [in] */ ClassID wrappedClassId,
            /* [in] */ REFGUID implementedIID,
            /* [in] */ void *pVTable,
            /* [in] */ ULONG cSlots);
        
        HRESULT ( STDMETHODCALLTYPE *COMClassicVTableDestroyed )( 
            ICorProfilerCallback10 * This,
            /* [in] */ ClassID wrappedClassId,
            /* [in] */ REFGUID implementedIID,
            /* [in] */ void *pVTable);
        
        HRESULT ( STDMETHODCALLTYPE *ExceptionCLRCatcherFound )( 
            ICorProfilerCallback10 * This);
        
        HRESULT ( STDMETHODCALLTYPE *ExceptionCLRCatcherExecute )( 
            ICorProfilerCallback10 * This);
        
        HRESULT ( STDMETHODCALLTYPE *ThreadNameChanged )( 
            ICorProfilerCallback10 * This,
            /* [in] */ ThreadID threadId,
            /* [in] */ ULONG cchName,
            /* [annotation][in] */ 
            _In_reads_opt_(cchName)  WCHAR name[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *GarbageCollectionStarted )( 
            ICorProfilerCallback10 * This,
            /* [in] */ int cGenerations,
            /* [size_is][in] */ BOOL generationCollected[  ],
            /* [in] */ COR_PRF_GC_REASON reason);
        
        HRESULT ( STDMETHODCALLTYPE *SurvivingReferences )( 
            ICorProfilerCallback10 * This,
            /* [in] */ ULONG cSurvivingObjectIDRanges,
            /* [size_is][in] */ ObjectID objectIDRangeStart[  ],
            /* [size_is][in] */ ULONG cObjectIDRangeLength[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *GarbageCollectionFinished )( 
            ICorProfilerCallback10 * This);
        
        HRESULT ( STDMETHODCALLTYPE *FinalizeableObjectQueued )( 
            ICorProfilerCallback10 * This,
            /* [in] */ DWORD finalizerFlags,
            /* [in] */ ObjectID objectID);
        
        HRESULT ( STDMETHODCALLTYPE *RootReferences2 )( 
            ICorProfilerCallback10 * This,
            /* [in] */ ULONG cRootRefs,
            /* [size_is][in] */ ObjectID rootRefIds[  ],
            /* [size_is][in] */ COR_PRF_GC_ROOT_KIND rootKinds[  ],
            /* [size_is][in] */ COR_PRF_GC_ROOT_FLAGS rootFlags[  ],
            /* [size_is][in] */ UINT_PTR rootIds[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *HandleCreated )( 
            ICorProfilerCallback10 * This,
            /* [in] */ GCHandleID handleId,
            /* [in] */ ObjectID initialObjectId);
        
        HRESULT ( STDMETHODCALLTYPE *HandleDestroyed )( 
            ICorProfilerCallback10 * This,
            /* [in] */ GCHandleID handleId);
        
        HRESULT ( STDMETHODCALLTYPE *InitializeForAttach )( 
            ICorProfilerCallback10 * This,
            /* [in] */ IUnknown *pCorProfilerInfoUnk,
            /* [in] */ void *pvClientData,
            /* [in] */ UINT cbClientData);
        
        HRESULT ( STDMETHODCALLTYPE *ProfilerAttachComplete )( 
            ICorProfilerCallback10 * This);
        
        HRESULT ( STDMETHODCALLTYPE *ProfilerDetachSucceeded )( 
            ICorProfilerCallback10 * This);
        
        HRESULT ( STDMETHODCALLTYPE *ReJITCompilationStarted )( 
            ICorProfilerCallback10 * This,
            /* [in] */ FunctionID functionId,
            /* [in] */ ReJITID rejitId,
            /* [in] */ BOOL fIsSafeToBlock);
        
        HRESULT ( STDMETHODCALLTYPE *GetReJITParameters )( 
            ICorProfilerCallback10 * This,
            /* [in] */ ModuleID moduleId,
            /* [in] */ mdMethodDef methodId,
            /* [in] */ ICorProfilerFunctionControl *pFunctionControl);
        
        HRESULT ( STDMETHODCALLTYPE *ReJITCompilationFinished )( 
            ICorProfilerCallback10 * This,
            /* [in] */ FunctionID functionId,
            /* [in] */ ReJITID rejitId,
            /* [in] */ HRESULT hrStatus,
            /* [in] */ BOOL fIsSafeToBlock);
        
        HRESULT ( STDMETHODCALLTYPE *ReJITError )( 
            ICorProfilerCallback10 * This,
            /* [in] */ ModuleID moduleId,
            /* [in] */ mdMethodDef methodId,
            /* [in] */ FunctionID functionId,
            /* [in] */ HRESULT hrStatus);
        
        HRESULT ( STDMETHODCALLTYPE *MovedReferences2 )( 
            ICorProfilerCallback10 * This,
            /* [in] */ ULONG cMovedObjectIDRanges,
            /* [size_is][in] */ ObjectID oldObjectIDRangeStart[  ],
            /* [size_is][in] */ ObjectID newObjectIDRangeStart[  ],
            /* [size_is][in] */ SIZE_T cObjectIDRangeLength[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *SurvivingReferences2 )( 
            ICorProfilerCallback10 * This,
            /* [in] */ ULONG cSurvivingObjectIDRanges,
            /* [size_is][in] */ ObjectID objectIDRangeStart[  ],
            /* [size_is][in] */ SIZE_T cObjectIDRangeLength[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *ConditionalWeakTableElementReferences )( 
            ICorProfilerCallback10 * This,
            /* [in] */ ULONG cRootRefs,
            /* [size_is][in] */ ObjectID keyRefIds[  ],
            /* [size_is][in] */ ObjectID valueRefIds[  ],
            /* [size_is][in] */ GCHandleID rootIds[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *GetAssemblyReferences )( 
            ICorProfilerCallback10 * This,
            /* [string][in] */ const WCHAR *wszAssemblyPath,
            /* [in] */ ICorProfilerAssemblyReferenceProvider *pAsmRefProvider);
        
        HRESULT ( STDMETHODCALLTYPE *ModuleInMemorySymbolsUpdated )( 
            ICorProfilerCallback10 * This,
            ModuleID moduleId);
        
        HRESULT ( STDMETHODCALLTYPE *DynamicMethodJITCompilationStarted )( 
            ICorProfilerCallback10 * This,
            /* [in] */ FunctionID functionId,
            /* [in] */ BOOL fIsSafeToBlock,
            /* [in] */ LPCBYTE pILHeader,
            /* [in] */ ULONG cbILHeader);
        
        HRESULT ( STDMETHODCALLTYPE *DynamicMethodJITCompilationFinished )( 
            ICorProfilerCallback10 * This,
            /* [in] */ FunctionID functionId,
            /* [in] */ HRESULT hrStatus,
            /* [in] */ BOOL fIsSafeToBlock);
        
        HRESULT ( STDMETHODCALLTYPE *DynamicMethodUnloaded )( 
            ICorProfilerCallback10 * This,
            /* [in] */ FunctionID functionId);
        
        HRESULT ( STDMETHODCALLTYPE *EventPipeEventDelivered )( 
            ICorProfilerCallback10 * This,
            /* [in] */ EVENTPIPE_PROVIDER provider,
            /* [in] */ DWORD eventId,
            /* [in] */ DWORD eventVersion,
            /* [in] */ ULONG cbMetadataBlob,
            /* [size_is][in] */ LPCBYTE metadataBlob,
            /* [in] */ ULONG cbEventData,
            /* [size_is][in] */ LPCBYTE eventData,
            /* [in] */ LPCGUID pActivityId,
            /* [in] */ LPCGUID pRelatedActivityId,
            /* [in] */ ThreadID eventThread,
            /* [in] */ ULONG numStackFrames,
            /* [length_is][in] */ UINT_PTR stackFrames[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *EventPipeProviderCreated )( 
            ICorProfilerCallback10 * This,
            /* [in] */ EVENTPIPE_PROVIDER provider);
        
        END_INTERFACE
    } ICorProfilerCallback10Vtbl;

    interface ICorProfilerCallback10
    {
        CONST_VTBL struct ICorProfilerCallback10Vtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define ICorProfilerCallback10_QueryInterface(This,riid,ppvObject)  \
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define ICorProfilerCallback10_AddRef(This) \
    ( (This)->lpVtbl -> AddRef(This) ) 

#define ICorProfilerCallback10_Release(This)    \
    ( (This)->lpVtbl -> Release(This) ) 


#define ICorProfilerCallback10_Initialize(This,pICorProfilerInfoUnk)    \
    ( (This)->lpVtbl -> Initialize(This,pICorProfilerInfoUnk) ) 

#define ICorProfilerCallback10_Shutdown(This)   \
    ( (This)->lpVtbl -> Shutdown(This) ) 

#define ICorProfilerCallback10_AppDomainCreationStarted(This,appDomainId)   \
    ( (This)->lpVtbl -> AppDomainCreationStarted(This,appDomainId) ) 

#define ICorProfilerCallback10_AppDomainCreationFinished(This,appDomainId,hrStatus) \
    ( (This)->lpVtbl -> AppDomainCreationFinished(This,appDomainId,hrStatus) ) 

#define ICorProfilerCallback10_AppDomainShutdownStarted(This,appDomainId)   \
    ( (This)->lpVtbl -> AppDomainShutdownStarted(This,appDomainId) ) 

#define ICorProfilerCallback10_AppDomainShutdownFinished(This,appDomainId,hrStatus) \
    ( (This)->lpVtbl -> AppDomainShutdownFinished(This,appDomainId,hrStatus) ) 

#define ICorProfilerCallback10_AssemblyLoadStarted(This,assemblyId) \
    ( (This)->lpVtbl -> AssemblyLoadStarted(This,assemblyId) ) 

#define ICorProfilerCallback10_AssemblyLoadFinished(This,assemblyId,hrStatus)   \
    ( (This)->lpVtbl -> AssemblyLoadFinished(This,assemblyId,hrStatus) ) 

#define ICorProfilerCallback10_AssemblyUnloadStarted(This,assemblyId)   \
    ( (This)->lpVtbl -> AssemblyUnloadStarted(This,assemblyId) ) 

#define ICorProfilerCallback10_AssemblyUnloadFinished(This,assemblyId,hrStatus) \
    ( (This)->lpVtbl -> AssemblyUnloadFinished(This,assemblyId,hrStatus) ) 

#define ICorProfilerCallback10_ModuleLoadStarted(This,moduleId) \
    ( (This)->lpVtbl -> ModuleLoadStarted(This,moduleId) ) 

#define ICorProfilerCallback10_ModuleLoadFinished(This,moduleId,hrStatus)   \
    ( (This)->lpVtbl -> ModuleLoadFinished(This,moduleId,hrStatus) ) 

#define ICorProfilerCallback10_ModuleUnloadStarted(This,moduleId)   \
    ( (This)->lpVtbl -> ModuleUnloadStarted(This,moduleId) ) 

#define ICorProfilerCallback10_ModuleUnloadFinished(This,moduleId,hrStatus) \
    ( (This)->lpVtbl -> ModuleUnloadFinished(This,moduleId,hrStatus) ) 

#define ICorProfilerCallback10_ModuleAttachedToAssembly(This,moduleId,AssemblyId)   \
    ( (This)->lpVtbl -> ModuleAttachedToAssembly(This,moduleId,AssemblyId) ) 

#define ICorProfilerCallback10_ClassLoadStarted(This,classId)   \
    ( (This)->lpVtbl -> ClassLoadStarted(This,classId) ) 

#define ICorProfilerCallback10_ClassLoadFinished(This,classId,hrStatus) \
    ( (This)->lpVtbl -> ClassLoadFinished(This,classId,hrStatus) ) 

#define ICorProfilerCallback10_ClassUnloadStarted(This,classId) \
    ( (This)->lpVtbl -> ClassUnloadStarted(This,classId) ) 

#define ICorProfilerCallback10_ClassUnloadFinished(This,classId,hrStatus)   \
    ( (This)->lpVtbl -> ClassUnloadFinished(This,classId,hrStatus) ) 

#define ICorProfilerCallback10_FunctionUnloadStarted(This,functionId)   \
    ( (This)->lpVtbl -> FunctionUnloadStarted(This,functionId) ) 

#define ICorProfilerCallback10_JITCompilationStarted(This,functionId,fIsSafeToBlock)    \
    ( (This)->lpVtbl -> JITCompilationStarted(This,functionId,fIsSafeToBlock) ) 

#define ICorProfilerCallback10_JITCompilationFinished(This,functionId,hrStatus,fIsSafeToBlock)  \
    ( (This)->lpVtbl -> JITCompilationFinished(This,functionId,hrStatus,fIsSafeToBlock) ) 

#define ICorProfilerCallback10_JITCachedFunctionSearchStarted(This,functionId,pbUseCachedFunction)  \
    ( (This)->lpVtbl -> JITCachedFunctionSearchStarted(This,functionId,pbUseCachedFunction) ) 

#define ICorProfilerCallback10_JITCachedFunctionSearchFinished(This,functionId,result)  \
    ( (This)->lpVtbl -> JITCachedFunctionSearchFinished(This,functionId,result) ) 

#define ICorProfilerCallback10_JITFunctionPitched(This,functionId)  \
    ( (This)->lpVtbl -> JITFunctionPitched(This,functionId) ) 

#define ICorProfilerCallback10_JITInlining(This,callerId,calleeId,pfShouldInline)   \
    ( (This)->lpVtbl -> JITInlining(This,callerId,calleeId,pfShouldInline) ) 

#define ICorProfilerCallback10_ThreadCreated(This,threadId) \
    ( (This)->lpVtbl -> ThreadCreated(This,threadId) ) 

#define ICorProfilerCallback10_ThreadDestroyed(This,threadId)   \
    ( (This)->lpVtbl -> ThreadDestroyed(This,threadId) ) 

#define ICorProfilerCallback10_ThreadAssignedToOSThread(This,managedThreadId,osThreadId)    \
    ( (This)->lpVtbl -> ThreadAssignedToOSThread(This,managedThreadId,osThreadId) ) 

#define ICorProfilerCallback10_RemotingClientInvocationStarted(This)    \
    ( (This)->lpVtbl -> RemotingClientInvocationStarted(This) ) 

#define ICorProfilerCallback10_RemotingClientSendingMessage(This,pCookie,fIsAsync)  \
    ( (This)->lpVtbl -> RemotingClientSendingMessage(This,pCookie,fIsAsync) ) 

#define ICorProfilerCallback10_RemotingClientReceivingReply(This,pCookie,fIsAsync)  \
    ( (This)->lpVtbl -> RemotingClientReceivingReply(This,pCookie,fIsAsync) ) 

#define ICorProfilerCallback10_RemotingClientInvocationFinished(This)   \
    ( (This)->lpVtbl -> RemotingClientInvocationFinished(This) ) 

#define ICorProfilerCallback10_RemotingServerReceivingMessage(This,pCookie,fIsAsync)    \
    ( (This)->lpVtbl -> RemotingServerReceivingMessage(This,pCookie,fIsAsync) ) 

#define ICorProfilerCallback10_RemotingServerInvocationStarted(This)    \
    ( (This)->lpVtbl -> RemotingServerInvocationStarted(This) ) 

#define ICorProfilerCallback10_RemotingServerInvocationReturned(This)   \
    ( (This)->lpVtbl -> RemotingServerInvocationReturned(This) ) 

#define ICorProfilerCallback10_RemotingServerSendingReply(This,pCookie,fIsAsync)    \
    ( (This)->lpVtbl -> RemotingServerSendingReply(This,pCookie,fIsAsync) ) 

#define ICorProfilerCallback10_UnmanagedToManagedTransition(This,functionId,reason) \
    ( (This)->lpVtbl -> UnmanagedToManagedTransition(This,functionId,reason) ) 

#define ICorProfilerCallback10_ManagedToUnmanagedTransition(This,functionId,reason) \
    ( (This)->lpVtbl -> ManagedToUnmanagedTransition(This,functionId,reason) ) 

#define ICorProfilerCallback10_RuntimeSuspendStarted(This,suspendReason)    \
    ( (This)->lpVtbl -> RuntimeSuspendStarted(This,suspendReason) ) 

#define ICorProfilerCallback10_RuntimeSuspendFinished(This) \
    ( (This)->lpVtbl -> RuntimeSuspendFinished(This) ) 

#define ICorProfilerCallback10_RuntimeSuspendAborted(This)  \
    ( (This)->lpVtbl -> RuntimeSuspendAborted(This) ) 

#define ICorProfilerCallback10_RuntimeResumeStarted(This)   \
    ( (This)->lpVtbl -> RuntimeResumeStarted(This) ) 

#define ICorProfilerCallback10_RuntimeResumeFinished(This)  \
    ( (This)->lpVtbl -> RuntimeResumeFinished(This) ) 

#define ICorProfilerCallback10_RuntimeThreadSuspended(This,threadId)    \
    ( (This)->lpVtbl -> RuntimeThreadSuspended(This,threadId) ) 

#define ICorProfilerCallback10_RuntimeThreadResumed(This,threadId)  \
    ( (This)->lpVtbl -> RuntimeThreadResumed(This,threadId) ) 

#define ICorProfilerCallback10_MovedReferences(This,cMovedObjectIDRanges,oldObjectIDRangeStart,newObjectIDRangeStart,cObjectIDRangeLength)  \
    ( (This)->lpVtbl -> MovedReferences(This,cMovedObjectIDRanges,oldObjectIDRangeStart,newObjectIDRangeStart,cObjectIDRangeLength) ) 

#define ICorProfilerCallback10_ObjectAllocated(This,objectId,classId)   \
    ( (This)->lpVtbl -> ObjectAllocated(This,objectId,classId) ) 

#define ICorProfilerCallback10_ObjectsAllocatedByClass(This,cClassCount,classIds,cObjects)  \
    ( (This)->lpVtbl -> ObjectsAllocatedByClass(This,cClassCount,classIds,cObjects) ) 

#define ICorProfilerCallback10_ObjectReferences(This,objectId,classId,cObjectRefs,objectRefIds) \
    ( (This)->lpVtbl -> ObjectReferences(This,objectId,classId,cObjectRefs,objectRefIds) ) 

#define ICorProfilerCallback10_RootReferences(This,cRootRefs,rootRefIds)    \
    ( (This)->lpVtbl -> RootReferences(This,cRootRefs,rootRefIds) ) 

#define ICorProfilerCallback10_ExceptionThrown(This,thrownObjectId) \
    ( (This)->lpVtbl -> ExceptionThrown(This,thrownObjectId) ) 

#define ICorProfilerCallback10_ExceptionSearchFunctionEnter(This,functionId)    \
    ( (This)->lpVtbl -> ExceptionSearchFunctionEnter(This,functionId) ) 

#define ICorProfilerCallback10_ExceptionSearchFunctionLeave(This)   \
    ( (This)->lpVtbl -> ExceptionSearchFunctionLeave(This) ) 

#define ICorProfilerCallback10_ExceptionSearchFilterEnter(This,functionId)  \
    ( (This)->lpVtbl -> ExceptionSearchFilterEnter(This,functionId) ) 

#define ICorProfilerCallback10_ExceptionSearchFilterLeave(This) \
    ( (This)->lpVtbl -> ExceptionSearchFilterLeave(This) ) 

#define ICorProfilerCallback10_ExceptionSearchCatcherFound(This,functionId) \
    ( (This)->lpVtbl -> ExceptionSearchCatcherFound(This,functionId) ) 

#define ICorProfilerCallback10_ExceptionOSHandlerEnter(This,__unused)   \
    ( (This)->lpVtbl -> ExceptionOSHandlerEnter(This,__unused) ) 

#define ICorProfilerCallback10_ExceptionOSHandlerLeave(This,__unused)   \
    ( (This)->lpVtbl -> ExceptionOSHandlerLeave(This,__unused) ) 

#define ICorProfilerCallback10_ExceptionUnwindFunctionEnter(This,functionId)    \
    ( (This)->lpVtbl -> ExceptionUnwindFunctionEnter(This,functionId) ) 

#define ICorProfilerCallback10_ExceptionUnwindFunctionLeave(This)   \
    ( (This)->lpVtbl -> ExceptionUnwindFunctionLeave(This) ) 

#define ICorProfilerCallback10_ExceptionUnwindFinallyEnter(This,functionId) \
    ( (This)->lpVtbl -> ExceptionUnwindFinallyEnter(This,functionId) ) 

#define ICorProfilerCallback10_ExceptionUnwindFinallyLeave(This)    \
    ( (This)->lpVtbl -> ExceptionUnwindFinallyLeave(This) ) 

#define ICorProfilerCallback10_ExceptionCatcherEnter(This,functionId,objectId)  \
    ( (This)->lpVtbl -> ExceptionCatcherEnter(This,functionId,objectId) ) 

#define ICorProfilerCallback10_ExceptionCatcherLeave(This)  \
    ( (This)->lpVtbl -> ExceptionCatcherLeave(This) ) 

#define ICorProfilerCallback10_COMClassicVTableCreated(This,wrappedClassId,implementedIID,pVTable,cSlots)   \
    ( (This)->lpVtbl -> COMClassicVTableCreated(This,wrappedClassId,implementedIID,pVTable,cSlots) ) 

#define ICorProfilerCallback10_COMClassicVTableDestroyed(This,wrappedClassId,implementedIID,pVTable)    \
    ( (This)->lpVtbl -> COMClassicVTableDestroyed(This,wrappedClassId,implementedIID,pVTable) ) 

#define ICorProfilerCallback10_ExceptionCLRCatcherFound(This)   \
    ( (This)->lpVtbl -> ExceptionCLRCatcherFound(This) ) 

#define ICorProfilerCallback10_ExceptionCLRCatcherExecute(This) \
    ( (This)->lpVtbl -> ExceptionCLRCatcherExecute(This) ) 


#define ICorProfilerCallback10_ThreadNameChanged(This,threadId,cchName,name)    \
    ( (This)->lpVtbl -> ThreadNameChanged(This,threadId,cchName,name) ) 

#define ICorProfilerCallback10_GarbageCollectionStarted(This,cGenerations,generationCollected,reason)   \
    ( (This)->lpVtbl -> GarbageCollectionStarted(This,cGenerations,generationCollected,reason) ) 

#define ICorProfilerCallback10_SurvivingReferences(This,cSurvivingObjectIDRanges,objectIDRangeStart,cObjectIDRangeLength)   \
    ( (This)->lpVtbl -> SurvivingReferences(This,cSurvivingObjectIDRanges,objectIDRangeStart,cObjectIDRangeLength) ) 

#define ICorProfilerCallback10_GarbageCollectionFinished(This)  \
    ( (This)->lpVtbl -> GarbageCollectionFinished(This) ) 

#define ICorProfilerCallback10_FinalizeableObjectQueued(This,finalizerFlags,objectID)   \
    ( (This)->lpVtbl -> FinalizeableObjectQueued(This,finalizerFlags,objectID) ) 

#define ICorProfilerCallback10_RootReferences2(This,cRootRefs,rootRefIds,rootKinds,rootFlags,rootIds)   \
    ( (This)->lpVtbl -> RootReferences2(This,cRootRefs,rootRefIds,rootKinds,rootFlags,rootIds) ) 

#define ICorProfilerCallback10_HandleCreated(This,handleId,initialObjectId) \
    ( (This)->lpVtbl -> HandleCreated(This,handleId,initialObjectId) ) 

#define ICorProfilerCallback10_HandleDestroyed(This,handleId)   \
    ( (This)->lpVtbl -> HandleDestroyed(This,handleId) ) 


#define ICorProfilerCallback10_InitializeForAttach(This,pCorProfilerInfoUnk,pvClientData,cbClientData)  \
    ( (This)->lpVtbl -> InitializeForAttach(This,pCorProfilerInfoUnk,pvClientData,cbClientData) ) 

#define ICorProfilerCallback10_ProfilerAttachComplete(This) \
    ( (This)->lpVtbl -> ProfilerAttachComplete(This) ) 

#define ICorProfilerCallback10_ProfilerDetachSucceeded(This)    \
    ( (This)->lpVtbl -> ProfilerDetachSucceeded(This) ) 


#define ICorProfilerCallback10_ReJITCompilationStarted(This,functionId,rejitId,fIsSafeToBlock)  \
    ( (This)->lpVtbl -> ReJITCompilationStarted(This,functionId,rejitId,fIsSafeToBlock) ) 

#define ICorProfilerCallback10_GetReJITParameters(This,moduleId,methodId,pFunctionControl)  \
    ( (This)->lpVtbl -> GetReJITParameters(This,moduleId,methodId,pFunctionControl) ) 

#define ICorProfilerCallback10_ReJITCompilationFinished(This,functionId,rejitId,hrStatus,fIsSafeToBlock)    \
    ( (This)->lpVtbl -> ReJITCompilationFinished(This,functionId,rejitId,hrStatus,fIsSafeToBlock) ) 

#define ICorProfilerCallback10_ReJITError(This,moduleId,methodId,functionId,hrStatus)   \
    ( (This)->lpVtbl -> ReJITError(This,moduleId,methodId,functionId,hrStatus) ) 

#define ICorProfilerCallback10_MovedReferences2(This,cMovedObjectIDRanges,oldObjectIDRangeStart,newObjectIDRangeStart,cObjectIDRangeLength) \
    ( (This)->lpVtbl -> MovedReferences2(This,cMovedObjectIDRanges,oldObjectIDRangeStart,newObjectIDRangeStart,cObjectIDRangeLength) ) 

#define ICorProfilerCallback10_SurvivingReferences2(This,cSurvivingObjectIDRanges,objectIDRangeStart,cObjectIDRangeLength)  \
    ( (This)->lpVtbl -> SurvivingReferences2(This,cSurvivingObjectIDRanges,objectIDRangeStart,cObjectIDRangeLength) ) 


#define ICorProfilerCallback10_ConditionalWeakTableElementReferences(This,cRootRefs,keyRefIds,valueRefIds,rootIds)  \
    ( (This)->lpVtbl -> ConditionalWeakTableElementReferences(This,cRootRefs,keyRefIds,valueRefIds,rootIds) ) 


#define ICorProfilerCallback10_GetAssemblyReferences(This,wszAssemblyPath,pAsmRefProvider)  \
    ( (This)->lpVtbl -> GetAssemblyReferences(This,wszAssemblyPath,pAsmRefProvider) ) 


#define ICorProfilerCallback10_ModuleInMemorySymbolsUpdated(This,moduleId)  \
    ( (This)->lpVtbl -> ModuleInMemorySymbolsUpdated(This,moduleId) ) 


#define ICorProfilerCallback10_DynamicMethodJITCompilationStarted(This,functionId,fIsSafeToBlock,pILHeader,cbILHeader)  \
    ( (This)->lpVtbl -> DynamicMethodJITCompilationStarted(This,functionId,fIsSafeToBlock,pILHeader,cbILHeader) ) 

#define ICorProfilerCallback10_DynamicMethodJITCompilationFinished(This,functionId,hrStatus,fIsSafeToBlock) \
    ( (This)->lpVtbl -> DynamicMethodJITCompilationFinished(This,functionId,hrStatus,fIsSafeToBlock) ) 


#define ICorProfilerCallback10_DynamicMethodUnloaded(This,functionId)   \
    ( (This)->lpVtbl -> DynamicMethodUnloaded(This,functionId) ) 


#define ICorProfilerCallback10_EventPipeEventDelivered(This,provider,eventId,eventVersion,cbMetadataBlob,metadataBlob,cbEventData,eventData,pActivityId,pRelatedActivityId,eventThread,numStackFrames,stackFrames)  \
    ( (This)->lpVtbl -> EventPipeEventDelivered(This,provider,eventId,eventVersion,cbMetadataBlob,metadataBlob,cbEventData,eventData,pActivityId,pRelatedActivityId,eventThread,numStackFrames,stackFrames) ) 

#define ICorProfilerCallback10_EventPipeProviderCreated(This,provider)  \
    ( (This)->lpVtbl -> EventPipeProviderCreated(This,provider) ) 

#endif /* COBJMACROS */


#endif  /* C style interface */




#endif  /* __ICorProfilerCallback10_INTERFACE_DEFINED__ */


#ifndef __ICorProfilerCallback11_INTERFACE_DEFINED__
#define __ICorProfilerCallback11_INTERFACE_DEFINED__

/* interface ICorProfilerCallback11 */
/* [local][unique][uuid][object] */ 


EXTERN_C const IID IID_ICorProfilerCallback11;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("42350846-AAED-47F7-B128-FD0C98881CDE")
    ICorProfilerCallback11 : public ICorProfilerCallback10
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE LoadAsNotficationOnly( 
            BOOL *pbNotificationOnly) = 0;
        
    };
    
    
#else   /* C style interface */

    typedef struct ICorProfilerCallback11Vtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            ICorProfilerCallback11 * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            ICorProfilerCallback11 * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            ICorProfilerCallback11 * This);
        
        HRESULT ( STDMETHODCALLTYPE *Initialize )( 
            ICorProfilerCallback11 * This,
            /* [in] */ IUnknown *pICorProfilerInfoUnk);
        
        HRESULT ( STDMETHODCALLTYPE *Shutdown )( 
            ICorProfilerCallback11 * This);
        
        HRESULT ( STDMETHODCALLTYPE *AppDomainCreationStarted )( 
            ICorProfilerCallback11 * This,
            /* [in] */ AppDomainID appDomainId);
        
        HRESULT ( STDMETHODCALLTYPE *AppDomainCreationFinished )( 
            ICorProfilerCallback11 * This,
            /* [in] */ AppDomainID appDomainId,
            /* [in] */ HRESULT hrStatus);
        
        HRESULT ( STDMETHODCALLTYPE *AppDomainShutdownStarted )( 
            ICorProfilerCallback11 * This,
            /* [in] */ AppDomainID appDomainId);
        
        HRESULT ( STDMETHODCALLTYPE *AppDomainShutdownFinished )( 
            ICorProfilerCallback11 * This,
            /* [in] */ AppDomainID appDomainId,
            /* [in] */ HRESULT hrStatus);
        
        HRESULT ( STDMETHODCALLTYPE *AssemblyLoadStarted )( 
            ICorProfilerCallback11 * This,
            /* [in] */ AssemblyID assemblyId);
        
        HRESULT ( STDMETHODCALLTYPE *AssemblyLoadFinished )( 
            ICorProfilerCallback11 * This,
            /* [in] */ AssemblyID assemblyId,
            /* [in] */ HRESULT hrStatus);
        
        HRESULT ( STDMETHODCALLTYPE *AssemblyUnloadStarted )( 
            ICorProfilerCallback11 * This,
            /* [in] */ AssemblyID assemblyId);
        
        HRESULT ( STDMETHODCALLTYPE *AssemblyUnloadFinished )( 
            ICorProfilerCallback11 * This,
            /* [in] */ AssemblyID assemblyId,
            /* [in] */ HRESULT hrStatus);
        
        HRESULT ( STDMETHODCALLTYPE *ModuleLoadStarted )( 
            ICorProfilerCallback11 * This,
            /* [in] */ ModuleID moduleId);
        
        HRESULT ( STDMETHODCALLTYPE *ModuleLoadFinished )( 
            ICorProfilerCallback11 * This,
            /* [in] */ ModuleID moduleId,
            /* [in] */ HRESULT hrStatus);
        
        HRESULT ( STDMETHODCALLTYPE *ModuleUnloadStarted )( 
            ICorProfilerCallback11 * This,
            /* [in] */ ModuleID moduleId);
        
        HRESULT ( STDMETHODCALLTYPE *ModuleUnloadFinished )( 
            ICorProfilerCallback11 * This,
            /* [in] */ ModuleID moduleId,
            /* [in] */ HRESULT hrStatus);
        
        HRESULT ( STDMETHODCALLTYPE *ModuleAttachedToAssembly )( 
            ICorProfilerCallback11 * This,
            /* [in] */ ModuleID moduleId,
            /* [in] */ AssemblyID AssemblyId);
        
        HRESULT ( STDMETHODCALLTYPE *ClassLoadStarted )( 
            ICorProfilerCallback11 * This,
            /* [in] */ ClassID classId);
        
        HRESULT ( STDMETHODCALLTYPE *ClassLoadFinished )( 
            ICorProfilerCallback11 * This,
            /* [in] */ ClassID classId,
            /* [in] */ HRESULT hrStatus);
        
        HRESULT ( STDMETHODCALLTYPE *ClassUnloadStarted )( 
            ICorProfilerCallback11 * This,
            /* [in] */ ClassID classId);
        
        HRESULT ( STDMETHODCALLTYPE *ClassUnloadFinished )( 
            ICorProfilerCallback11 * This,
            /* [in] */ ClassID classId,
            /* [in] */ HRESULT hrStatus);
        
        HRESULT ( STDMETHODCALLTYPE *FunctionUnloadStarted )( 
            ICorProfilerCallback11 * This,
            /* [in] */ FunctionID functionId);
        
        HRESULT ( STDMETHODCALLTYPE *JITCompilationStarted )( 
            ICorProfilerCallback11 * This,
            /* [in] */ FunctionID functionId,
            /* [in] */ BOOL fIsSafeToBlock);
        
        HRESULT ( STDMETHODCALLTYPE *JITCompilationFinished )( 
            ICorProfilerCallback11 * This,
            /* [in] */ FunctionID functionId,
            /* [in] */ HRESULT hrStatus,
            /* [in] */ BOOL fIsSafeToBlock);
        
        HRESULT ( STDMETHODCALLTYPE *JITCachedFunctionSearchStarted )( 
            ICorProfilerCallback11 * This,
            /* [in] */ FunctionID functionId,
            /* [out] */ BOOL *pbUseCachedFunction);
        
        HRESULT ( STDMETHODCALLTYPE *JITCachedFunctionSearchFinished )( 
            ICorProfilerCallback11 * This,
            /* [in] */ FunctionID functionId,
            /* [in] */ COR_PRF_JIT_CACHE result);
        
        HRESULT ( STDMETHODCALLTYPE *JITFunctionPitched )( 
            ICorProfilerCallback11 * This,
            /* [in] */ FunctionID functionId);
        
        HRESULT ( STDMETHODCALLTYPE *JITInlining )( 
            ICorProfilerCallback11 * This,
            /* [in] */ FunctionID callerId,
            /* [in] */ FunctionID calleeId,
            /* [out] */ BOOL *pfShouldInline);
        
        HRESULT ( STDMETHODCALLTYPE *ThreadCreated )( 
            ICorProfilerCallback11 * This,
            /* [in] */ ThreadID threadId);
        
        HRESULT ( STDMETHODCALLTYPE *ThreadDestroyed )( 
            ICorProfilerCallback11 * This,
            /* [in] */ ThreadID threadId);
        
        HRESULT ( STDMETHODCALLTYPE *ThreadAssignedToOSThread )( 
            ICorProfilerCallback11 * This,
            /* [in] */ ThreadID managedThreadId,
            /* [in] */ DWORD osThreadId);
        
        HRESULT ( STDMETHODCALLTYPE *RemotingClientInvocationStarted )( 
            ICorProfilerCallback11 * This);
        
        HRESULT ( STDMETHODCALLTYPE *RemotingClientSendingMessage )( 
            ICorProfilerCallback11 * This,
            /* [in] */ GUID *pCookie,
            /* [in] */ BOOL fIsAsync);
        
        HRESULT ( STDMETHODCALLTYPE *RemotingClientReceivingReply )( 
            ICorProfilerCallback11 * This,
            /* [in] */ GUID *pCookie,
            /* [in] */ BOOL fIsAsync);
        
        HRESULT ( STDMETHODCALLTYPE *RemotingClientInvocationFinished )( 
            ICorProfilerCallback11 * This);
        
        HRESULT ( STDMETHODCALLTYPE *RemotingServerReceivingMessage )( 
            ICorProfilerCallback11 * This,
            /* [in] */ GUID *pCookie,
            /* [in] */ BOOL fIsAsync);
        
        HRESULT ( STDMETHODCALLTYPE *RemotingServerInvocationStarted )( 
            ICorProfilerCallback11 * This);
        
        HRESULT ( STDMETHODCALLTYPE *RemotingServerInvocationReturned )( 
            ICorProfilerCallback11 * This);
        
        HRESULT ( STDMETHODCALLTYPE *RemotingServerSendingReply )( 
            ICorProfilerCallback11 * This,
            /* [in] */ GUID *pCookie,
            /* [in] */ BOOL fIsAsync);
        
        HRESULT ( STDMETHODCALLTYPE *UnmanagedToManagedTransition )( 
            ICorProfilerCallback11 * This,
            /* [in] */ FunctionID functionId,
            /* [in] */ COR_PRF_TRANSITION_REASON reason);
        
        HRESULT ( STDMETHODCALLTYPE *ManagedToUnmanagedTransition )( 
            ICorProfilerCallback11 * This,
            /* [in] */ FunctionID functionId,
            /* [in] */ COR_PRF_TRANSITION_REASON reason);
        
        HRESULT ( STDMETHODCALLTYPE *RuntimeSuspendStarted )( 
            ICorProfilerCallback11 * This,
            /* [in] */ COR_PRF_SUSPEND_REASON suspendReason);
        
        HRESULT ( STDMETHODCALLTYPE *RuntimeSuspendFinished )( 
            ICorProfilerCallback11 * This);
        
        HRESULT ( STDMETHODCALLTYPE *RuntimeSuspendAborted )( 
            ICorProfilerCallback11 * This);
        
        HRESULT ( STDMETHODCALLTYPE *RuntimeResumeStarted )( 
            ICorProfilerCallback11 * This);
        
        HRESULT ( STDMETHODCALLTYPE *RuntimeResumeFinished )( 
            ICorProfilerCallback11 * This);
        
        HRESULT ( STDMETHODCALLTYPE *RuntimeThreadSuspended )( 
            ICorProfilerCallback11 * This,
            /* [in] */ ThreadID threadId);
        
        HRESULT ( STDMETHODCALLTYPE *RuntimeThreadResumed )( 
            ICorProfilerCallback11 * This,
            /* [in] */ ThreadID threadId);
        
        HRESULT ( STDMETHODCALLTYPE *MovedReferences )( 
            ICorProfilerCallback11 * This,
            /* [in] */ ULONG cMovedObjectIDRanges,
            /* [size_is][in] */ ObjectID oldObjectIDRangeStart[  ],
            /* [size_is][in] */ ObjectID newObjectIDRangeStart[  ],
            /* [size_is][in] */ ULONG cObjectIDRangeLength[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *ObjectAllocated )( 
            ICorProfilerCallback11 * This,
            /* [in] */ ObjectID objectId,
            /* [in] */ ClassID classId);
        
        HRESULT ( STDMETHODCALLTYPE *ObjectsAllocatedByClass )( 
            ICorProfilerCallback11 * This,
            /* [in] */ ULONG cClassCount,
            /* [size_is][in] */ ClassID classIds[  ],
            /* [size_is][in] */ ULONG cObjects[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *ObjectReferences )( 
            ICorProfilerCallback11 * This,
            /* [in] */ ObjectID objectId,
            /* [in] */ ClassID classId,
            /* [in] */ ULONG cObjectRefs,
            /* [size_is][in] */ ObjectID objectRefIds[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *RootReferences )( 
            ICorProfilerCallback11 * This,
            /* [in] */ ULONG cRootRefs,
            /* [size_is][in] */ ObjectID rootRefIds[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *ExceptionThrown )( 
            ICorProfilerCallback11 * This,
            /* [in] */ ObjectID thrownObjectId);
        
        HRESULT ( STDMETHODCALLTYPE *ExceptionSearchFunctionEnter )( 
            ICorProfilerCallback11 * This,
            /* [in] */ FunctionID functionId);
        
        HRESULT ( STDMETHODCALLTYPE *ExceptionSearchFunctionLeave )( 
            ICorProfilerCallback11 * This);
        
        HRESULT ( STDMETHODCALLTYPE *ExceptionSearchFilterEnter )( 
            ICorProfilerCallback11 * This,
            /* [in] */ FunctionID functionId);
        
        HRESULT ( STDMETHODCALLTYPE *ExceptionSearchFilterLeave )( 
            ICorProfilerCallback11 * This);
        
        HRESULT ( STDMETHODCALLTYPE *ExceptionSearchCatcherFound )( 
            ICorProfilerCallback11 * This,
            /* [in] */ FunctionID functionId);
        
        HRESULT ( STDMETHODCALLTYPE *ExceptionOSHandlerEnter )( 
            ICorProfilerCallback11 * This,
            /* [in] */ UINT_PTR __unused);
        
        HRESULT ( STDMETHODCALLTYPE *ExceptionOSHandlerLeave )( 
            ICorProfilerCallback11 * This,
            /* [in] */ UINT_PTR __unused);
        
        HRESULT ( STDMETHODCALLTYPE *ExceptionUnwindFunctionEnter )( 
            ICorProfilerCallback11 * This,
            /* [in] */ FunctionID functionId);
        
        HRESULT ( STDMETHODCALLTYPE *ExceptionUnwindFunctionLeave )( 
            ICorProfilerCallback11 * This);
        
        HRESULT ( STDMETHODCALLTYPE *ExceptionUnwindFinallyEnter )( 
            ICorProfilerCallback11 * This,
            /* [in] */ FunctionID functionId);
        
        HRESULT ( STDMETHODCALLTYPE *ExceptionUnwindFinallyLeave )( 
            ICorProfilerCallback11 * This);
        
        HRESULT ( STDMETHODCALLTYPE *ExceptionCatcherEnter )( 
            ICorProfilerCallback11 * This,
            /* [in] */ FunctionID functionId,
            /* [in] */ ObjectID objectId);
        
        HRESULT ( STDMETHODCALLTYPE *ExceptionCatcherLeave )( 
            ICorProfilerCallback11 * This);
        
        HRESULT ( STDMETHODCALLTYPE *COMClassicVTableCreated )( 
            ICorProfilerCallback11 * This,
            /* [in] */ ClassID wrappedClassId,
            /* [in] */ REFGUID implementedIID,
            /* [in] */ void *pVTable,
            /* [in] */ ULONG cSlots);
        
        HRESULT ( STDMETHODCALLTYPE *COMClassicVTableDestroyed )( 
            ICorProfilerCallback11 * This,
            /* [in] */ ClassID wrappedClassId,
            /* [in] */ REFGUID implementedIID,
            /* [in] */ void *pVTable);
        
        HRESULT ( STDMETHODCALLTYPE *ExceptionCLRCatcherFound )( 
            ICorProfilerCallback11 * This);
        
        HRESULT ( STDMETHODCALLTYPE *ExceptionCLRCatcherExecute )( 
            ICorProfilerCallback11 * This);
        
        HRESULT ( STDMETHODCALLTYPE *ThreadNameChanged )( 
            ICorProfilerCallback11 * This,
            /* [in] */ ThreadID threadId,
            /* [in] */ ULONG cchName,
            /* [annotation][in] */ 
            _In_reads_opt_(cchName)  WCHAR name[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *GarbageCollectionStarted )( 
            ICorProfilerCallback11 * This,
            /* [in] */ int cGenerations,
            /* [size_is][in] */ BOOL generationCollected[  ],
            /* [in] */ COR_PRF_GC_REASON reason);
        
        HRESULT ( STDMETHODCALLTYPE *SurvivingReferences )( 
            ICorProfilerCallback11 * This,
            /* [in] */ ULONG cSurvivingObjectIDRanges,
            /* [size_is][in] */ ObjectID objectIDRangeStart[  ],
            /* [size_is][in] */ ULONG cObjectIDRangeLength[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *GarbageCollectionFinished )( 
            ICorProfilerCallback11 * This);
        
        HRESULT ( STDMETHODCALLTYPE *FinalizeableObjectQueued )( 
            ICorProfilerCallback11 * This,
            /* [in] */ DWORD finalizerFlags,
            /* [in] */ ObjectID objectID);
        
        HRESULT ( STDMETHODCALLTYPE *RootReferences2 )( 
            ICorProfilerCallback11 * This,
            /* [in] */ ULONG cRootRefs,
            /* [size_is][in] */ ObjectID rootRefIds[  ],
            /* [size_is][in] */ COR_PRF_GC_ROOT_KIND rootKinds[  ],
            /* [size_is][in] */ COR_PRF_GC_ROOT_FLAGS rootFlags[  ],
            /* [size_is][in] */ UINT_PTR rootIds[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *HandleCreated )( 
            ICorProfilerCallback11 * This,
            /* [in] */ GCHandleID handleId,
            /* [in] */ ObjectID initialObjectId);
        
        HRESULT ( STDMETHODCALLTYPE *HandleDestroyed )( 
            ICorProfilerCallback11 * This,
            /* [in] */ GCHandleID handleId);
        
        HRESULT ( STDMETHODCALLTYPE *InitializeForAttach )( 
            ICorProfilerCallback11 * This,
            /* [in] */ IUnknown *pCorProfilerInfoUnk,
            /* [in] */ void *pvClientData,
            /* [in] */ UINT cbClientData);
        
        HRESULT ( STDMETHODCALLTYPE *ProfilerAttachComplete )( 
            ICorProfilerCallback11 * This);
        
        HRESULT ( STDMETHODCALLTYPE *ProfilerDetachSucceeded )( 
            ICorProfilerCallback11 * This);
        
        HRESULT ( STDMETHODCALLTYPE *ReJITCompilationStarted )( 
            ICorProfilerCallback11 * This,
            /* [in] */ FunctionID functionId,
            /* [in] */ ReJITID rejitId,
            /* [in] */ BOOL fIsSafeToBlock);
        
        HRESULT ( STDMETHODCALLTYPE *GetReJITParameters )( 
            ICorProfilerCallback11 * This,
            /* [in] */ ModuleID moduleId,
            /* [in] */ mdMethodDef methodId,
            /* [in] */ ICorProfilerFunctionControl *pFunctionControl);
        
        HRESULT ( STDMETHODCALLTYPE *ReJITCompilationFinished )( 
            ICorProfilerCallback11 * This,
            /* [in] */ FunctionID functionId,
            /* [in] */ ReJITID rejitId,
            /* [in] */ HRESULT hrStatus,
            /* [in] */ BOOL fIsSafeToBlock);
        
        HRESULT ( STDMETHODCALLTYPE *ReJITError )( 
            ICorProfilerCallback11 * This,
            /* [in] */ ModuleID moduleId,
            /* [in] */ mdMethodDef methodId,
            /* [in] */ FunctionID functionId,
            /* [in] */ HRESULT hrStatus);
        
        HRESULT ( STDMETHODCALLTYPE *MovedReferences2 )( 
            ICorProfilerCallback11 * This,
            /* [in] */ ULONG cMovedObjectIDRanges,
            /* [size_is][in] */ ObjectID oldObjectIDRangeStart[  ],
            /* [size_is][in] */ ObjectID newObjectIDRangeStart[  ],
            /* [size_is][in] */ SIZE_T cObjectIDRangeLength[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *SurvivingReferences2 )( 
            ICorProfilerCallback11 * This,
            /* [in] */ ULONG cSurvivingObjectIDRanges,
            /* [size_is][in] */ ObjectID objectIDRangeStart[  ],
            /* [size_is][in] */ SIZE_T cObjectIDRangeLength[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *ConditionalWeakTableElementReferences )( 
            ICorProfilerCallback11 * This,
            /* [in] */ ULONG cRootRefs,
            /* [size_is][in] */ ObjectID keyRefIds[  ],
            /* [size_is][in] */ ObjectID valueRefIds[  ],
            /* [size_is][in] */ GCHandleID rootIds[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *GetAssemblyReferences )( 
            ICorProfilerCallback11 * This,
            /* [string][in] */ const WCHAR *wszAssemblyPath,
            /* [in] */ ICorProfilerAssemblyReferenceProvider *pAsmRefProvider);
        
        HRESULT ( STDMETHODCALLTYPE *ModuleInMemorySymbolsUpdated )( 
            ICorProfilerCallback11 * This,
            ModuleID moduleId);
        
        HRESULT ( STDMETHODCALLTYPE *DynamicMethodJITCompilationStarted )( 
            ICorProfilerCallback11 * This,
            /* [in] */ FunctionID functionId,
            /* [in] */ BOOL fIsSafeToBlock,
            /* [in] */ LPCBYTE pILHeader,
            /* [in] */ ULONG cbILHeader);
        
        HRESULT ( STDMETHODCALLTYPE *DynamicMethodJITCompilationFinished )( 
            ICorProfilerCallback11 * This,
            /* [in] */ FunctionID functionId,
            /* [in] */ HRESULT hrStatus,
            /* [in] */ BOOL fIsSafeToBlock);
        
        HRESULT ( STDMETHODCALLTYPE *DynamicMethodUnloaded )( 
            ICorProfilerCallback11 * This,
            /* [in] */ FunctionID functionId);
        
        HRESULT ( STDMETHODCALLTYPE *EventPipeEventDelivered )( 
            ICorProfilerCallback11 * This,
            /* [in] */ EVENTPIPE_PROVIDER provider,
            /* [in] */ DWORD eventId,
            /* [in] */ DWORD eventVersion,
            /* [in] */ ULONG cbMetadataBlob,
            /* [size_is][in] */ LPCBYTE metadataBlob,
            /* [in] */ ULONG cbEventData,
            /* [size_is][in] */ LPCBYTE eventData,
            /* [in] */ LPCGUID pActivityId,
            /* [in] */ LPCGUID pRelatedActivityId,
            /* [in] */ ThreadID eventThread,
            /* [in] */ ULONG numStackFrames,
            /* [length_is][in] */ UINT_PTR stackFrames[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *EventPipeProviderCreated )( 
            ICorProfilerCallback11 * This,
            /* [in] */ EVENTPIPE_PROVIDER provider);
        
        HRESULT ( STDMETHODCALLTYPE *LoadAsNotficationOnly )( 
            ICorProfilerCallback11 * This,
            BOOL *pbNotificationOnly);
        
        END_INTERFACE
    } ICorProfilerCallback11Vtbl;

    interface ICorProfilerCallback11
    {
        CONST_VTBL struct ICorProfilerCallback11Vtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define ICorProfilerCallback11_QueryInterface(This,riid,ppvObject)  \
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define ICorProfilerCallback11_AddRef(This) \
    ( (This)->lpVtbl -> AddRef(This) ) 

#define ICorProfilerCallback11_Release(This)    \
    ( (This)->lpVtbl -> Release(This) ) 


#define ICorProfilerCallback11_Initialize(This,pICorProfilerInfoUnk)    \
    ( (This)->lpVtbl -> Initialize(This,pICorProfilerInfoUnk) ) 

#define ICorProfilerCallback11_Shutdown(This)   \
    ( (This)->lpVtbl -> Shutdown(This) ) 

#define ICorProfilerCallback11_AppDomainCreationStarted(This,appDomainId)   \
    ( (This)->lpVtbl -> AppDomainCreationStarted(This,appDomainId) ) 

#define ICorProfilerCallback11_AppDomainCreationFinished(This,appDomainId,hrStatus) \
    ( (This)->lpVtbl -> AppDomainCreationFinished(This,appDomainId,hrStatus) ) 

#define ICorProfilerCallback11_AppDomainShutdownStarted(This,appDomainId)   \
    ( (This)->lpVtbl -> AppDomainShutdownStarted(This,appDomainId) ) 

#define ICorProfilerCallback11_AppDomainShutdownFinished(This,appDomainId,hrStatus) \
    ( (This)->lpVtbl -> AppDomainShutdownFinished(This,appDomainId,hrStatus) ) 

#define ICorProfilerCallback11_AssemblyLoadStarted(This,assemblyId) \
    ( (This)->lpVtbl -> AssemblyLoadStarted(This,assemblyId) ) 

#define ICorProfilerCallback11_AssemblyLoadFinished(This,assemblyId,hrStatus)   \
    ( (This)->lpVtbl -> AssemblyLoadFinished(This,assemblyId,hrStatus) ) 

#define ICorProfilerCallback11_AssemblyUnloadStarted(This,assemblyId)   \
    ( (This)->lpVtbl -> AssemblyUnloadStarted(This,assemblyId) ) 

#define ICorProfilerCallback11_AssemblyUnloadFinished(This,assemblyId,hrStatus) \
    ( (This)->lpVtbl -> AssemblyUnloadFinished(This,assemblyId,hrStatus) ) 

#define ICorProfilerCallback11_ModuleLoadStarted(This,moduleId) \
    ( (This)->lpVtbl -> ModuleLoadStarted(This,moduleId) ) 

#define ICorProfilerCallback11_ModuleLoadFinished(This,moduleId,hrStatus)   \
    ( (This)->lpVtbl -> ModuleLoadFinished(This,moduleId,hrStatus) ) 

#define ICorProfilerCallback11_ModuleUnloadStarted(This,moduleId)   \
    ( (This)->lpVtbl -> ModuleUnloadStarted(This,moduleId) ) 

#define ICorProfilerCallback11_ModuleUnloadFinished(This,moduleId,hrStatus) \
    ( (This)->lpVtbl -> ModuleUnloadFinished(This,moduleId,hrStatus) ) 

#define ICorProfilerCallback11_ModuleAttachedToAssembly(This,moduleId,AssemblyId)   \
    ( (This)->lpVtbl -> ModuleAttachedToAssembly(This,moduleId,AssemblyId) ) 

#define ICorProfilerCallback11_ClassLoadStarted(This,classId)   \
    ( (This)->lpVtbl -> ClassLoadStarted(This,classId) ) 

#define ICorProfilerCallback11_ClassLoadFinished(This,classId,hrStatus) \
    ( (This)->lpVtbl -> ClassLoadFinished(This,classId,hrStatus) ) 

#define ICorProfilerCallback11_ClassUnloadStarted(This,classId) \
    ( (This)->lpVtbl -> ClassUnloadStarted(This,classId) ) 

#define ICorProfilerCallback11_ClassUnloadFinished(This,classId,hrStatus)   \
    ( (This)->lpVtbl -> ClassUnloadFinished(This,classId,hrStatus) ) 

#define ICorProfilerCallback11_FunctionUnloadStarted(This,functionId)   \
    ( (This)->lpVtbl -> FunctionUnloadStarted(This,functionId) ) 

#define ICorProfilerCallback11_JITCompilationStarted(This,functionId,fIsSafeToBlock)    \
    ( (This)->lpVtbl -> JITCompilationStarted(This,functionId,fIsSafeToBlock) ) 

#define ICorProfilerCallback11_JITCompilationFinished(This,functionId,hrStatus,fIsSafeToBlock)  \
    ( (This)->lpVtbl -> JITCompilationFinished(This,functionId,hrStatus,fIsSafeToBlock) ) 

#define ICorProfilerCallback11_JITCachedFunctionSearchStarted(This,functionId,pbUseCachedFunction)  \
    ( (This)->lpVtbl -> JITCachedFunctionSearchStarted(This,functionId,pbUseCachedFunction) ) 

#define ICorProfilerCallback11_JITCachedFunctionSearchFinished(This,functionId,result)  \
    ( (This)->lpVtbl -> JITCachedFunctionSearchFinished(This,functionId,result) ) 

#define ICorProfilerCallback11_JITFunctionPitched(This,functionId)  \
    ( (This)->lpVtbl -> JITFunctionPitched(This,functionId) ) 

#define ICorProfilerCallback11_JITInlining(This,callerId,calleeId,pfShouldInline)   \
    ( (This)->lpVtbl -> JITInlining(This,callerId,calleeId,pfShouldInline) ) 

#define ICorProfilerCallback11_ThreadCreated(This,threadId) \
    ( (This)->lpVtbl -> ThreadCreated(This,threadId) ) 

#define ICorProfilerCallback11_ThreadDestroyed(This,threadId)   \
    ( (This)->lpVtbl -> ThreadDestroyed(This,threadId) ) 

#define ICorProfilerCallback11_ThreadAssignedToOSThread(This,managedThreadId,osThreadId)    \
    ( (This)->lpVtbl -> ThreadAssignedToOSThread(This,managedThreadId,osThreadId) ) 

#define ICorProfilerCallback11_RemotingClientInvocationStarted(This)    \
    ( (This)->lpVtbl -> RemotingClientInvocationStarted(This) ) 

#define ICorProfilerCallback11_RemotingClientSendingMessage(This,pCookie,fIsAsync)  \
    ( (This)->lpVtbl -> RemotingClientSendingMessage(This,pCookie,fIsAsync) ) 

#define ICorProfilerCallback11_RemotingClientReceivingReply(This,pCookie,fIsAsync)  \
    ( (This)->lpVtbl -> RemotingClientReceivingReply(This,pCookie,fIsAsync) ) 

#define ICorProfilerCallback11_RemotingClientInvocationFinished(This)   \
    ( (This)->lpVtbl -> RemotingClientInvocationFinished(This) ) 

#define ICorProfilerCallback11_RemotingServerReceivingMessage(This,pCookie,fIsAsync)    \
    ( (This)->lpVtbl -> RemotingServerReceivingMessage(This,pCookie,fIsAsync) ) 

#define ICorProfilerCallback11_RemotingServerInvocationStarted(This)    \
    ( (This)->lpVtbl -> RemotingServerInvocationStarted(This) ) 

#define ICorProfilerCallback11_RemotingServerInvocationReturned(This)   \
    ( (This)->lpVtbl -> RemotingServerInvocationReturned(This) ) 

#define ICorProfilerCallback11_RemotingServerSendingReply(This,pCookie,fIsAsync)    \
    ( (This)->lpVtbl -> RemotingServerSendingReply(This,pCookie,fIsAsync) ) 

#define ICorProfilerCallback11_UnmanagedToManagedTransition(This,functionId,reason) \
    ( (This)->lpVtbl -> UnmanagedToManagedTransition(This,functionId,reason) ) 

#define ICorProfilerCallback11_ManagedToUnmanagedTransition(This,functionId,reason) \
    ( (This)->lpVtbl -> ManagedToUnmanagedTransition(This,functionId,reason) ) 

#define ICorProfilerCallback11_RuntimeSuspendStarted(This,suspendReason)    \
    ( (This)->lpVtbl -> RuntimeSuspendStarted(This,suspendReason) ) 

#define ICorProfilerCallback11_RuntimeSuspendFinished(This) \
    ( (This)->lpVtbl -> RuntimeSuspendFinished(This) ) 

#define ICorProfilerCallback11_RuntimeSuspendAborted(This)  \
    ( (This)->lpVtbl -> RuntimeSuspendAborted(This) ) 

#define ICorProfilerCallback11_RuntimeResumeStarted(This)   \
    ( (This)->lpVtbl -> RuntimeResumeStarted(This) ) 

#define ICorProfilerCallback11_RuntimeResumeFinished(This)  \
    ( (This)->lpVtbl -> RuntimeResumeFinished(This) ) 

#define ICorProfilerCallback11_RuntimeThreadSuspended(This,threadId)    \
    ( (This)->lpVtbl -> RuntimeThreadSuspended(This,threadId) ) 

#define ICorProfilerCallback11_RuntimeThreadResumed(This,threadId)  \
    ( (This)->lpVtbl -> RuntimeThreadResumed(This,threadId) ) 

#define ICorProfilerCallback11_MovedReferences(This,cMovedObjectIDRanges,oldObjectIDRangeStart,newObjectIDRangeStart,cObjectIDRangeLength)  \
    ( (This)->lpVtbl -> MovedReferences(This,cMovedObjectIDRanges,oldObjectIDRangeStart,newObjectIDRangeStart,cObjectIDRangeLength) ) 

#define ICorProfilerCallback11_ObjectAllocated(This,objectId,classId)   \
    ( (This)->lpVtbl -> ObjectAllocated(This,objectId,classId) ) 

#define ICorProfilerCallback11_ObjectsAllocatedByClass(This,cClassCount,classIds,cObjects)  \
    ( (This)->lpVtbl -> ObjectsAllocatedByClass(This,cClassCount,classIds,cObjects) ) 

#define ICorProfilerCallback11_ObjectReferences(This,objectId,classId,cObjectRefs,objectRefIds) \
    ( (This)->lpVtbl -> ObjectReferences(This,objectId,classId,cObjectRefs,objectRefIds) ) 

#define ICorProfilerCallback11_RootReferences(This,cRootRefs,rootRefIds)    \
    ( (This)->lpVtbl -> RootReferences(This,cRootRefs,rootRefIds) ) 

#define ICorProfilerCallback11_ExceptionThrown(This,thrownObjectId) \
    ( (This)->lpVtbl -> ExceptionThrown(This,thrownObjectId) ) 

#define ICorProfilerCallback11_ExceptionSearchFunctionEnter(This,functionId)    \
    ( (This)->lpVtbl -> ExceptionSearchFunctionEnter(This,functionId) ) 

#define ICorProfilerCallback11_ExceptionSearchFunctionLeave(This)   \
    ( (This)->lpVtbl -> ExceptionSearchFunctionLeave(This) ) 

#define ICorProfilerCallback11_ExceptionSearchFilterEnter(This,functionId)  \
    ( (This)->lpVtbl -> ExceptionSearchFilterEnter(This,functionId) ) 

#define ICorProfilerCallback11_ExceptionSearchFilterLeave(This) \
    ( (This)->lpVtbl -> ExceptionSearchFilterLeave(This) ) 

#define ICorProfilerCallback11_ExceptionSearchCatcherFound(This,functionId) \
    ( (This)->lpVtbl -> ExceptionSearchCatcherFound(This,functionId) ) 

#define ICorProfilerCallback11_ExceptionOSHandlerEnter(This,__unused)   \
    ( (This)->lpVtbl -> ExceptionOSHandlerEnter(This,__unused) ) 

#define ICorProfilerCallback11_ExceptionOSHandlerLeave(This,__unused)   \
    ( (This)->lpVtbl -> ExceptionOSHandlerLeave(This,__unused) ) 

#define ICorProfilerCallback11_ExceptionUnwindFunctionEnter(This,functionId)    \
    ( (This)->lpVtbl -> ExceptionUnwindFunctionEnter(This,functionId) ) 

#define ICorProfilerCallback11_ExceptionUnwindFunctionLeave(This)   \
    ( (This)->lpVtbl -> ExceptionUnwindFunctionLeave(This) ) 

#define ICorProfilerCallback11_ExceptionUnwindFinallyEnter(This,functionId) \
    ( (This)->lpVtbl -> ExceptionUnwindFinallyEnter(This,functionId) ) 

#define ICorProfilerCallback11_ExceptionUnwindFinallyLeave(This)    \
    ( (This)->lpVtbl -> ExceptionUnwindFinallyLeave(This) ) 

#define ICorProfilerCallback11_ExceptionCatcherEnter(This,functionId,objectId)  \
    ( (This)->lpVtbl -> ExceptionCatcherEnter(This,functionId,objectId) ) 

#define ICorProfilerCallback11_ExceptionCatcherLeave(This)  \
    ( (This)->lpVtbl -> ExceptionCatcherLeave(This) ) 

#define ICorProfilerCallback11_COMClassicVTableCreated(This,wrappedClassId,implementedIID,pVTable,cSlots)   \
    ( (This)->lpVtbl -> COMClassicVTableCreated(This,wrappedClassId,implementedIID,pVTable,cSlots) ) 

#define ICorProfilerCallback11_COMClassicVTableDestroyed(This,wrappedClassId,implementedIID,pVTable)    \
    ( (This)->lpVtbl -> COMClassicVTableDestroyed(This,wrappedClassId,implementedIID,pVTable) ) 

#define ICorProfilerCallback11_ExceptionCLRCatcherFound(This)   \
    ( (This)->lpVtbl -> ExceptionCLRCatcherFound(This) ) 

#define ICorProfilerCallback11_ExceptionCLRCatcherExecute(This) \
    ( (This)->lpVtbl -> ExceptionCLRCatcherExecute(This) ) 


#define ICorProfilerCallback11_ThreadNameChanged(This,threadId,cchName,name)    \
    ( (This)->lpVtbl -> ThreadNameChanged(This,threadId,cchName,name) ) 

#define ICorProfilerCallback11_GarbageCollectionStarted(This,cGenerations,generationCollected,reason)   \
    ( (This)->lpVtbl -> GarbageCollectionStarted(This,cGenerations,generationCollected,reason) ) 

#define ICorProfilerCallback11_SurvivingReferences(This,cSurvivingObjectIDRanges,objectIDRangeStart,cObjectIDRangeLength)   \
    ( (This)->lpVtbl -> SurvivingReferences(This,cSurvivingObjectIDRanges,objectIDRangeStart,cObjectIDRangeLength) ) 

#define ICorProfilerCallback11_GarbageCollectionFinished(This)  \
    ( (This)->lpVtbl -> GarbageCollectionFinished(This) ) 

#define ICorProfilerCallback11_FinalizeableObjectQueued(This,finalizerFlags,objectID)   \
    ( (This)->lpVtbl -> FinalizeableObjectQueued(This,finalizerFlags,objectID) ) 

#define ICorProfilerCallback11_RootReferences2(This,cRootRefs,rootRefIds,rootKinds,rootFlags,rootIds)   \
    ( (This)->lpVtbl -> RootReferences2(This,cRootRefs,rootRefIds,rootKinds,rootFlags,rootIds) ) 

#define ICorProfilerCallback11_HandleCreated(This,handleId,initialObjectId) \
    ( (This)->lpVtbl -> HandleCreated(This,handleId,initialObjectId) ) 

#define ICorProfilerCallback11_HandleDestroyed(This,handleId)   \
    ( (This)->lpVtbl -> HandleDestroyed(This,handleId) ) 


#define ICorProfilerCallback11_InitializeForAttach(This,pCorProfilerInfoUnk,pvClientData,cbClientData)  \
    ( (This)->lpVtbl -> InitializeForAttach(This,pCorProfilerInfoUnk,pvClientData,cbClientData) ) 

#define ICorProfilerCallback11_ProfilerAttachComplete(This) \
    ( (This)->lpVtbl -> ProfilerAttachComplete(This) ) 

#define ICorProfilerCallback11_ProfilerDetachSucceeded(This)    \
    ( (This)->lpVtbl -> ProfilerDetachSucceeded(This) ) 


#define ICorProfilerCallback11_ReJITCompilationStarted(This,functionId,rejitId,fIsSafeToBlock)  \
    ( (This)->lpVtbl -> ReJITCompilationStarted(This,functionId,rejitId,fIsSafeToBlock) ) 

#define ICorProfilerCallback11_GetReJITParameters(This,moduleId,methodId,pFunctionControl)  \
    ( (This)->lpVtbl -> GetReJITParameters(This,moduleId,methodId,pFunctionControl) ) 

#define ICorProfilerCallback11_ReJITCompilationFinished(This,functionId,rejitId,hrStatus,fIsSafeToBlock)    \
    ( (This)->lpVtbl -> ReJITCompilationFinished(This,functionId,rejitId,hrStatus,fIsSafeToBlock) ) 

#define ICorProfilerCallback11_ReJITError(This,moduleId,methodId,functionId,hrStatus)   \
    ( (This)->lpVtbl -> ReJITError(This,moduleId,methodId,functionId,hrStatus) ) 

#define ICorProfilerCallback11_MovedReferences2(This,cMovedObjectIDRanges,oldObjectIDRangeStart,newObjectIDRangeStart,cObjectIDRangeLength) \
    ( (This)->lpVtbl -> MovedReferences2(This,cMovedObjectIDRanges,oldObjectIDRangeStart,newObjectIDRangeStart,cObjectIDRangeLength) ) 

#define ICorProfilerCallback11_SurvivingReferences2(This,cSurvivingObjectIDRanges,objectIDRangeStart,cObjectIDRangeLength)  \
    ( (This)->lpVtbl -> SurvivingReferences2(This,cSurvivingObjectIDRanges,objectIDRangeStart,cObjectIDRangeLength) ) 


#define ICorProfilerCallback11_ConditionalWeakTableElementReferences(This,cRootRefs,keyRefIds,valueRefIds,rootIds)  \
    ( (This)->lpVtbl -> ConditionalWeakTableElementReferences(This,cRootRefs,keyRefIds,valueRefIds,rootIds) ) 


#define ICorProfilerCallback11_GetAssemblyReferences(This,wszAssemblyPath,pAsmRefProvider)  \
    ( (This)->lpVtbl -> GetAssemblyReferences(This,wszAssemblyPath,pAsmRefProvider) ) 


#define ICorProfilerCallback11_ModuleInMemorySymbolsUpdated(This,moduleId)  \
    ( (This)->lpVtbl -> ModuleInMemorySymbolsUpdated(This,moduleId) ) 


#define ICorProfilerCallback11_DynamicMethodJITCompilationStarted(This,functionId,fIsSafeToBlock,pILHeader,cbILHeader)  \
    ( (This)->lpVtbl -> DynamicMethodJITCompilationStarted(This,functionId,fIsSafeToBlock,pILHeader,cbILHeader) ) 

#define ICorProfilerCallback11_DynamicMethodJITCompilationFinished(This,functionId,hrStatus,fIsSafeToBlock) \
    ( (This)->lpVtbl -> DynamicMethodJITCompilationFinished(This,functionId,hrStatus,fIsSafeToBlock) ) 


#define ICorProfilerCallback11_DynamicMethodUnloaded(This,functionId)   \
    ( (This)->lpVtbl -> DynamicMethodUnloaded(This,functionId) ) 


#define ICorProfilerCallback11_EventPipeEventDelivered(This,provider,eventId,eventVersion,cbMetadataBlob,metadataBlob,cbEventData,eventData,pActivityId,pRelatedActivityId,eventThread,numStackFrames,stackFrames)  \
    ( (This)->lpVtbl -> EventPipeEventDelivered(This,provider,eventId,eventVersion,cbMetadataBlob,metadataBlob,cbEventData,eventData,pActivityId,pRelatedActivityId,eventThread,numStackFrames,stackFrames) ) 

#define ICorProfilerCallback11_EventPipeProviderCreated(This,provider)  \
    ( (This)->lpVtbl -> EventPipeProviderCreated(This,provider) ) 


#define ICorProfilerCallback11_LoadAsNotficationOnly(This,pbNotificationOnly)   \
    ( (This)->lpVtbl -> LoadAsNotficationOnly(This,pbNotificationOnly) ) 

#endif /* COBJMACROS */


#endif  /* C style interface */




#endif  /* __ICorProfilerCallback11_INTERFACE_DEFINED__ */


/* interface __MIDL_itf_corprof_0000_0011 */
/* [local] */ 

typedef /* [public] */ 
enum __MIDL___MIDL_itf_corprof_0000_0011_0001
    {
        COR_PRF_CODEGEN_DISABLE_INLINING    = 0x1,
        COR_PRF_CODEGEN_DISABLE_ALL_OPTIMIZATIONS   = 0x2
    }   COR_PRF_CODEGEN_FLAGS;



extern RPC_IF_HANDLE __MIDL_itf_corprof_0000_0011_v0_0_c_ifspec;
extern RPC_IF_HANDLE __MIDL_itf_corprof_0000_0011_v0_0_s_ifspec;

#ifndef __ICorProfilerInfo_INTERFACE_DEFINED__
#define __ICorProfilerInfo_INTERFACE_DEFINED__

/* interface ICorProfilerInfo */
/* [local][unique][uuid][object] */ 


EXTERN_C const IID IID_ICorProfilerInfo;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("28B5557D-3F3F-48b4-90B2-5F9EEA2F6C48")
    ICorProfilerInfo : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE GetClassFromObject( 
            /* [in] */ ObjectID objectId,
            /* [out] */ ClassID *pClassId) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetClassFromToken( 
            /* [in] */ ModuleID moduleId,
            /* [in] */ mdTypeDef typeDef,
            /* [out] */ ClassID *pClassId) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetCodeInfo( 
            /* [in] */ FunctionID functionId,
            /* [out] */ LPCBYTE *pStart,
            /* [out] */ ULONG *pcSize) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetEventMask( 
            /* [out] */ DWORD *pdwEvents) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetFunctionFromIP( 
            /* [in] */ LPCBYTE ip,
            /* [out] */ FunctionID *pFunctionId) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetFunctionFromToken( 
            /* [in] */ ModuleID moduleId,
            /* [in] */ mdToken token,
            /* [out] */ FunctionID *pFunctionId) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetHandleFromThread( 
            /* [in] */ ThreadID threadId,
            /* [out] */ HANDLE *phThread) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetObjectSize( 
            /* [in] */ ObjectID objectId,
            /* [out] */ ULONG *pcSize) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE IsArrayClass( 
            /* [in] */ ClassID classId,
            /* [out] */ CorElementType *pBaseElemType,
            /* [out] */ ClassID *pBaseClassId,
            /* [out] */ ULONG *pcRank) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetThreadInfo( 
            /* [in] */ ThreadID threadId,
            /* [out] */ DWORD *pdwWin32ThreadId) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetCurrentThreadID( 
            /* [out] */ ThreadID *pThreadId) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetClassIDInfo( 
            /* [in] */ ClassID classId,
            /* [out] */ ModuleID *pModuleId,
            /* [out] */ mdTypeDef *pTypeDefToken) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetFunctionInfo( 
            /* [in] */ FunctionID functionId,
            /* [out] */ ClassID *pClassId,
            /* [out] */ ModuleID *pModuleId,
            /* [out] */ mdToken *pToken) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE SetEventMask( 
            /* [in] */ DWORD dwEvents) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE SetEnterLeaveFunctionHooks( 
            /* [in] */ FunctionEnter *pFuncEnter,
            /* [in] */ FunctionLeave *pFuncLeave,
            /* [in] */ FunctionTailcall *pFuncTailcall) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE SetFunctionIDMapper( 
            /* [in] */ FunctionIDMapper *pFunc) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetTokenAndMetaDataFromFunction( 
            /* [in] */ FunctionID functionId,
            /* [in] */ REFIID riid,
            /* [out] */ IUnknown **ppImport,
            /* [out] */ mdToken *pToken) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetModuleInfo( 
            /* [in] */ ModuleID moduleId,
            /* [out] */ LPCBYTE *ppBaseLoadAddress,
            /* [in] */ ULONG cchName,
            /* [out] */ ULONG *pcchName,
            /* [annotation][out] */ 
            _Out_writes_to_(cchName, *pcchName)  WCHAR szName[  ],
            /* [out] */ AssemblyID *pAssemblyId) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetModuleMetaData( 
            /* [in] */ ModuleID moduleId,
            /* [in] */ DWORD dwOpenFlags,
            /* [in] */ REFIID riid,
            /* [out] */ IUnknown **ppOut) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetILFunctionBody( 
            /* [in] */ ModuleID moduleId,
            /* [in] */ mdMethodDef methodId,
            /* [out] */ LPCBYTE *ppMethodHeader,
            /* [out] */ ULONG *pcbMethodSize) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetILFunctionBodyAllocator( 
            /* [in] */ ModuleID moduleId,
            /* [out] */ IMethodMalloc **ppMalloc) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE SetILFunctionBody( 
            /* [in] */ ModuleID moduleId,
            /* [in] */ mdMethodDef methodid,
            /* [in] */ LPCBYTE pbNewILMethodHeader) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetAppDomainInfo( 
            /* [in] */ AppDomainID appDomainId,
            /* [in] */ ULONG cchName,
            /* [out] */ ULONG *pcchName,
            /* [annotation][out] */ 
            _Out_writes_to_(cchName, *pcchName)  WCHAR szName[  ],
            /* [out] */ ProcessID *pProcessId) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetAssemblyInfo( 
            /* [in] */ AssemblyID assemblyId,
            /* [in] */ ULONG cchName,
            /* [out] */ ULONG *pcchName,
            /* [annotation][out] */ 
            _Out_writes_to_(cchName, *pcchName)  WCHAR szName[  ],
            /* [out] */ AppDomainID *pAppDomainId,
            /* [out] */ ModuleID *pModuleId) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE SetFunctionReJIT( 
            /* [in] */ FunctionID functionId) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE ForceGC( void) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE SetILInstrumentedCodeMap( 
            /* [in] */ FunctionID functionId,
            /* [in] */ BOOL fStartJit,
            /* [in] */ ULONG cILMapEntries,
            /* [size_is][in] */ COR_IL_MAP rgILMapEntries[  ]) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetInprocInspectionInterface( 
            /* [out] */ IUnknown **ppicd) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetInprocInspectionIThisThread( 
            /* [out] */ IUnknown **ppicd) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetThreadContext( 
            /* [in] */ ThreadID threadId,
            /* [out] */ ContextID *pContextId) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE BeginInprocDebugging( 
            /* [in] */ BOOL fThisThreadOnly,
            /* [out] */ DWORD *pdwProfilerContext) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE EndInprocDebugging( 
            /* [in] */ DWORD dwProfilerContext) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetILToNativeMapping( 
            /* [in] */ FunctionID functionId,
            /* [in] */ ULONG32 cMap,
            /* [out] */ ULONG32 *pcMap,
            /* [length_is][size_is][out] */ COR_DEBUG_IL_TO_NATIVE_MAP map[  ]) = 0;
        
    };
    
    
#else   /* C style interface */

    typedef struct ICorProfilerInfoVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            ICorProfilerInfo * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            ICorProfilerInfo * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            ICorProfilerInfo * This);
        
        HRESULT ( STDMETHODCALLTYPE *GetClassFromObject )( 
            ICorProfilerInfo * This,
            /* [in] */ ObjectID objectId,
            /* [out] */ ClassID *pClassId);
        
        HRESULT ( STDMETHODCALLTYPE *GetClassFromToken )( 
            ICorProfilerInfo * This,
            /* [in] */ ModuleID moduleId,
            /* [in] */ mdTypeDef typeDef,
            /* [out] */ ClassID *pClassId);
        
        HRESULT ( STDMETHODCALLTYPE *GetCodeInfo )( 
            ICorProfilerInfo * This,
            /* [in] */ FunctionID functionId,
            /* [out] */ LPCBYTE *pStart,
            /* [out] */ ULONG *pcSize);
        
        HRESULT ( STDMETHODCALLTYPE *GetEventMask )( 
            ICorProfilerInfo * This,
            /* [out] */ DWORD *pdwEvents);
        
        HRESULT ( STDMETHODCALLTYPE *GetFunctionFromIP )( 
            ICorProfilerInfo * This,
            /* [in] */ LPCBYTE ip,
            /* [out] */ FunctionID *pFunctionId);
        
        HRESULT ( STDMETHODCALLTYPE *GetFunctionFromToken )( 
            ICorProfilerInfo * This,
            /* [in] */ ModuleID moduleId,
            /* [in] */ mdToken token,
            /* [out] */ FunctionID *pFunctionId);
        
        HRESULT ( STDMETHODCALLTYPE *GetHandleFromThread )( 
            ICorProfilerInfo * This,
            /* [in] */ ThreadID threadId,
            /* [out] */ HANDLE *phThread);
        
        HRESULT ( STDMETHODCALLTYPE *GetObjectSize )( 
            ICorProfilerInfo * This,
            /* [in] */ ObjectID objectId,
            /* [out] */ ULONG *pcSize);
        
        HRESULT ( STDMETHODCALLTYPE *IsArrayClass )( 
            ICorProfilerInfo * This,
            /* [in] */ ClassID classId,
            /* [out] */ CorElementType *pBaseElemType,
            /* [out] */ ClassID *pBaseClassId,
            /* [out] */ ULONG *pcRank);
        
        HRESULT ( STDMETHODCALLTYPE *GetThreadInfo )( 
            ICorProfilerInfo * This,
            /* [in] */ ThreadID threadId,
            /* [out] */ DWORD *pdwWin32ThreadId);
        
        HRESULT ( STDMETHODCALLTYPE *GetCurrentThreadID )( 
            ICorProfilerInfo * This,
            /* [out] */ ThreadID *pThreadId);
        
        HRESULT ( STDMETHODCALLTYPE *GetClassIDInfo )( 
            ICorProfilerInfo * This,
            /* [in] */ ClassID classId,
            /* [out] */ ModuleID *pModuleId,
            /* [out] */ mdTypeDef *pTypeDefToken);
        
        HRESULT ( STDMETHODCALLTYPE *GetFunctionInfo )( 
            ICorProfilerInfo * This,
            /* [in] */ FunctionID functionId,
            /* [out] */ ClassID *pClassId,
            /* [out] */ ModuleID *pModuleId,
            /* [out] */ mdToken *pToken);
        
        HRESULT ( STDMETHODCALLTYPE *SetEventMask )( 
            ICorProfilerInfo * This,
            /* [in] */ DWORD dwEvents);
        
        HRESULT ( STDMETHODCALLTYPE *SetEnterLeaveFunctionHooks )( 
            ICorProfilerInfo * This,
            /* [in] */ FunctionEnter *pFuncEnter,
            /* [in] */ FunctionLeave *pFuncLeave,
            /* [in] */ FunctionTailcall *pFuncTailcall);
        
        HRESULT ( STDMETHODCALLTYPE *SetFunctionIDMapper )( 
            ICorProfilerInfo * This,
            /* [in] */ FunctionIDMapper *pFunc);
        
        HRESULT ( STDMETHODCALLTYPE *GetTokenAndMetaDataFromFunction )( 
            ICorProfilerInfo * This,
            /* [in] */ FunctionID functionId,
            /* [in] */ REFIID riid,
            /* [out] */ IUnknown **ppImport,
            /* [out] */ mdToken *pToken);
        
        HRESULT ( STDMETHODCALLTYPE *GetModuleInfo )( 
            ICorProfilerInfo * This,
            /* [in] */ ModuleID moduleId,
            /* [out] */ LPCBYTE *ppBaseLoadAddress,
            /* [in] */ ULONG cchName,
            /* [out] */ ULONG *pcchName,
            /* [annotation][out] */ 
            _Out_writes_to_(cchName, *pcchName)  WCHAR szName[  ],
            /* [out] */ AssemblyID *pAssemblyId);
        
        HRESULT ( STDMETHODCALLTYPE *GetModuleMetaData )( 
            ICorProfilerInfo * This,
            /* [in] */ ModuleID moduleId,
            /* [in] */ DWORD dwOpenFlags,
            /* [in] */ REFIID riid,
            /* [out] */ IUnknown **ppOut);
        
        HRESULT ( STDMETHODCALLTYPE *GetILFunctionBody )( 
            ICorProfilerInfo * This,
            /* [in] */ ModuleID moduleId,
            /* [in] */ mdMethodDef methodId,
            /* [out] */ LPCBYTE *ppMethodHeader,
            /* [out] */ ULONG *pcbMethodSize);
        
        HRESULT ( STDMETHODCALLTYPE *GetILFunctionBodyAllocator )( 
            ICorProfilerInfo * This,
            /* [in] */ ModuleID moduleId,
            /* [out] */ IMethodMalloc **ppMalloc);
        
        HRESULT ( STDMETHODCALLTYPE *SetILFunctionBody )( 
            ICorProfilerInfo * This,
            /* [in] */ ModuleID moduleId,
            /* [in] */ mdMethodDef methodid,
            /* [in] */ LPCBYTE pbNewILMethodHeader);
        
        HRESULT ( STDMETHODCALLTYPE *GetAppDomainInfo )( 
            ICorProfilerInfo * This,
            /* [in] */ AppDomainID appDomainId,
            /* [in] */ ULONG cchName,
            /* [out] */ ULONG *pcchName,
            /* [annotation][out] */ 
            _Out_writes_to_(cchName, *pcchName)  WCHAR szName[  ],
            /* [out] */ ProcessID *pProcessId);
        
        HRESULT ( STDMETHODCALLTYPE *GetAssemblyInfo )( 
            ICorProfilerInfo * This,
            /* [in] */ AssemblyID assemblyId,
            /* [in] */ ULONG cchName,
            /* [out] */ ULONG *pcchName,
            /* [annotation][out] */ 
            _Out_writes_to_(cchName, *pcchName)  WCHAR szName[  ],
            /* [out] */ AppDomainID *pAppDomainId,
            /* [out] */ ModuleID *pModuleId);
        
        HRESULT ( STDMETHODCALLTYPE *SetFunctionReJIT )( 
            ICorProfilerInfo * This,
            /* [in] */ FunctionID functionId);
        
        HRESULT ( STDMETHODCALLTYPE *ForceGC )( 
            ICorProfilerInfo * This);
        
        HRESULT ( STDMETHODCALLTYPE *SetILInstrumentedCodeMap )( 
            ICorProfilerInfo * This,
            /* [in] */ FunctionID functionId,
            /* [in] */ BOOL fStartJit,
            /* [in] */ ULONG cILMapEntries,
            /* [size_is][in] */ COR_IL_MAP rgILMapEntries[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *GetInprocInspectionInterface )( 
            ICorProfilerInfo * This,
            /* [out] */ IUnknown **ppicd);
        
        HRESULT ( STDMETHODCALLTYPE *GetInprocInspectionIThisThread )( 
            ICorProfilerInfo * This,
            /* [out] */ IUnknown **ppicd);
        
        HRESULT ( STDMETHODCALLTYPE *GetThreadContext )( 
            ICorProfilerInfo * This,
            /* [in] */ ThreadID threadId,
            /* [out] */ ContextID *pContextId);
        
        HRESULT ( STDMETHODCALLTYPE *BeginInprocDebugging )( 
            ICorProfilerInfo * This,
            /* [in] */ BOOL fThisThreadOnly,
            /* [out] */ DWORD *pdwProfilerContext);
        
        HRESULT ( STDMETHODCALLTYPE *EndInprocDebugging )( 
            ICorProfilerInfo * This,
            /* [in] */ DWORD dwProfilerContext);
        
        HRESULT ( STDMETHODCALLTYPE *GetILToNativeMapping )( 
            ICorProfilerInfo * This,
            /* [in] */ FunctionID functionId,
            /* [in] */ ULONG32 cMap,
            /* [out] */ ULONG32 *pcMap,
            /* [length_is][size_is][out] */ COR_DEBUG_IL_TO_NATIVE_MAP map[  ]);
        
        END_INTERFACE
    } ICorProfilerInfoVtbl;

    interface ICorProfilerInfo
    {
        CONST_VTBL struct ICorProfilerInfoVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define ICorProfilerInfo_QueryInterface(This,riid,ppvObject)    \
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define ICorProfilerInfo_AddRef(This)   \
    ( (This)->lpVtbl -> AddRef(This) ) 

#define ICorProfilerInfo_Release(This)  \
    ( (This)->lpVtbl -> Release(This) ) 


#define ICorProfilerInfo_GetClassFromObject(This,objectId,pClassId) \
    ( (This)->lpVtbl -> GetClassFromObject(This,objectId,pClassId) ) 

#define ICorProfilerInfo_GetClassFromToken(This,moduleId,typeDef,pClassId)  \
    ( (This)->lpVtbl -> GetClassFromToken(This,moduleId,typeDef,pClassId) ) 

#define ICorProfilerInfo_GetCodeInfo(This,functionId,pStart,pcSize) \
    ( (This)->lpVtbl -> GetCodeInfo(This,functionId,pStart,pcSize) ) 

#define ICorProfilerInfo_GetEventMask(This,pdwEvents)   \
    ( (This)->lpVtbl -> GetEventMask(This,pdwEvents) ) 

#define ICorProfilerInfo_GetFunctionFromIP(This,ip,pFunctionId) \
    ( (This)->lpVtbl -> GetFunctionFromIP(This,ip,pFunctionId) ) 

#define ICorProfilerInfo_GetFunctionFromToken(This,moduleId,token,pFunctionId)  \
    ( (This)->lpVtbl -> GetFunctionFromToken(This,moduleId,token,pFunctionId) ) 

#define ICorProfilerInfo_GetHandleFromThread(This,threadId,phThread)    \
    ( (This)->lpVtbl -> GetHandleFromThread(This,threadId,phThread) ) 

#define ICorProfilerInfo_GetObjectSize(This,objectId,pcSize)    \
    ( (This)->lpVtbl -> GetObjectSize(This,objectId,pcSize) ) 

#define ICorProfilerInfo_IsArrayClass(This,classId,pBaseElemType,pBaseClassId,pcRank)   \
    ( (This)->lpVtbl -> IsArrayClass(This,classId,pBaseElemType,pBaseClassId,pcRank) ) 

#define ICorProfilerInfo_GetThreadInfo(This,threadId,pdwWin32ThreadId)  \
    ( (This)->lpVtbl -> GetThreadInfo(This,threadId,pdwWin32ThreadId) ) 

#define ICorProfilerInfo_GetCurrentThreadID(This,pThreadId) \
    ( (This)->lpVtbl -> GetCurrentThreadID(This,pThreadId) ) 

#define ICorProfilerInfo_GetClassIDInfo(This,classId,pModuleId,pTypeDefToken)   \
    ( (This)->lpVtbl -> GetClassIDInfo(This,classId,pModuleId,pTypeDefToken) ) 

#define ICorProfilerInfo_GetFunctionInfo(This,functionId,pClassId,pModuleId,pToken) \
    ( (This)->lpVtbl -> GetFunctionInfo(This,functionId,pClassId,pModuleId,pToken) ) 

#define ICorProfilerInfo_SetEventMask(This,dwEvents)    \
    ( (This)->lpVtbl -> SetEventMask(This,dwEvents) ) 

#define ICorProfilerInfo_SetEnterLeaveFunctionHooks(This,pFuncEnter,pFuncLeave,pFuncTailcall)   \
    ( (This)->lpVtbl -> SetEnterLeaveFunctionHooks(This,pFuncEnter,pFuncLeave,pFuncTailcall) ) 

#define ICorProfilerInfo_SetFunctionIDMapper(This,pFunc)    \
    ( (This)->lpVtbl -> SetFunctionIDMapper(This,pFunc) ) 

#define ICorProfilerInfo_GetTokenAndMetaDataFromFunction(This,functionId,riid,ppImport,pToken)  \
    ( (This)->lpVtbl -> GetTokenAndMetaDataFromFunction(This,functionId,riid,ppImport,pToken) ) 

#define ICorProfilerInfo_GetModuleInfo(This,moduleId,ppBaseLoadAddress,cchName,pcchName,szName,pAssemblyId) \
    ( (This)->lpVtbl -> GetModuleInfo(This,moduleId,ppBaseLoadAddress,cchName,pcchName,szName,pAssemblyId) ) 

#define ICorProfilerInfo_GetModuleMetaData(This,moduleId,dwOpenFlags,riid,ppOut)    \
    ( (This)->lpVtbl -> GetModuleMetaData(This,moduleId,dwOpenFlags,riid,ppOut) ) 

#define ICorProfilerInfo_GetILFunctionBody(This,moduleId,methodId,ppMethodHeader,pcbMethodSize) \
    ( (This)->lpVtbl -> GetILFunctionBody(This,moduleId,methodId,ppMethodHeader,pcbMethodSize) ) 

#define ICorProfilerInfo_GetILFunctionBodyAllocator(This,moduleId,ppMalloc) \
    ( (This)->lpVtbl -> GetILFunctionBodyAllocator(This,moduleId,ppMalloc) ) 

#define ICorProfilerInfo_SetILFunctionBody(This,moduleId,methodid,pbNewILMethodHeader)  \
    ( (This)->lpVtbl -> SetILFunctionBody(This,moduleId,methodid,pbNewILMethodHeader) ) 

#define ICorProfilerInfo_GetAppDomainInfo(This,appDomainId,cchName,pcchName,szName,pProcessId)  \
    ( (This)->lpVtbl -> GetAppDomainInfo(This,appDomainId,cchName,pcchName,szName,pProcessId) ) 

#define ICorProfilerInfo_GetAssemblyInfo(This,assemblyId,cchName,pcchName,szName,pAppDomainId,pModuleId)    \
    ( (This)->lpVtbl -> GetAssemblyInfo(This,assemblyId,cchName,pcchName,szName,pAppDomainId,pModuleId) ) 

#define ICorProfilerInfo_SetFunctionReJIT(This,functionId)  \
    ( (This)->lpVtbl -> SetFunctionReJIT(This,functionId) ) 

#define ICorProfilerInfo_ForceGC(This)  \
    ( (This)->lpVtbl -> ForceGC(This) ) 

#define ICorProfilerInfo_SetILInstrumentedCodeMap(This,functionId,fStartJit,cILMapEntries,rgILMapEntries)   \
    ( (This)->lpVtbl -> SetILInstrumentedCodeMap(This,functionId,fStartJit,cILMapEntries,rgILMapEntries) ) 

#define ICorProfilerInfo_GetInprocInspectionInterface(This,ppicd)   \
    ( (This)->lpVtbl -> GetInprocInspectionInterface(This,ppicd) ) 

#define ICorProfilerInfo_GetInprocInspectionIThisThread(This,ppicd) \
    ( (This)->lpVtbl -> GetInprocInspectionIThisThread(This,ppicd) ) 

#define ICorProfilerInfo_GetThreadContext(This,threadId,pContextId) \
    ( (This)->lpVtbl -> GetThreadContext(This,threadId,pContextId) ) 

#define ICorProfilerInfo_BeginInprocDebugging(This,fThisThreadOnly,pdwProfilerContext)  \
    ( (This)->lpVtbl -> BeginInprocDebugging(This,fThisThreadOnly,pdwProfilerContext) ) 

#define ICorProfilerInfo_EndInprocDebugging(This,dwProfilerContext) \
    ( (This)->lpVtbl -> EndInprocDebugging(This,dwProfilerContext) ) 

#define ICorProfilerInfo_GetILToNativeMapping(This,functionId,cMap,pcMap,map)   \
    ( (This)->lpVtbl -> GetILToNativeMapping(This,functionId,cMap,pcMap,map) ) 

#endif /* COBJMACROS */


#endif  /* C style interface */




#endif  /* __ICorProfilerInfo_INTERFACE_DEFINED__ */


#ifndef __ICorProfilerInfo2_INTERFACE_DEFINED__
#define __ICorProfilerInfo2_INTERFACE_DEFINED__

/* interface ICorProfilerInfo2 */
/* [local][unique][uuid][object] */ 


EXTERN_C const IID IID_ICorProfilerInfo2;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("CC0935CD-A518-487d-B0BB-A93214E65478")
    ICorProfilerInfo2 : public ICorProfilerInfo
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE DoStackSnapshot( 
            /* [in] */ ThreadID thread,
            /* [in] */ StackSnapshotCallback *callback,
            /* [in] */ ULONG32 infoFlags,
            /* [in] */ void *clientData,
            /* [size_is][in] */ BYTE context[  ],
            /* [in] */ ULONG32 contextSize) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE SetEnterLeaveFunctionHooks2( 
            /* [in] */ FunctionEnter2 *pFuncEnter,
            /* [in] */ FunctionLeave2 *pFuncLeave,
            /* [in] */ FunctionTailcall2 *pFuncTailcall) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetFunctionInfo2( 
            /* [in] */ FunctionID funcId,
            /* [in] */ COR_PRF_FRAME_INFO frameInfo,
            /* [out] */ ClassID *pClassId,
            /* [out] */ ModuleID *pModuleId,
            /* [out] */ mdToken *pToken,
            /* [in] */ ULONG32 cTypeArgs,
            /* [out] */ ULONG32 *pcTypeArgs,
            /* [out] */ ClassID typeArgs[  ]) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetStringLayout( 
            /* [out] */ ULONG *pBufferLengthOffset,
            /* [out] */ ULONG *pStringLengthOffset,
            /* [out] */ ULONG *pBufferOffset) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetClassLayout( 
            /* [in] */ ClassID classID,
            /* [out][in] */ COR_FIELD_OFFSET rFieldOffset[  ],
            /* [in] */ ULONG cFieldOffset,
            /* [out] */ ULONG *pcFieldOffset,
            /* [out] */ ULONG *pulClassSize) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetClassIDInfo2( 
            /* [in] */ ClassID classId,
            /* [out] */ ModuleID *pModuleId,
            /* [out] */ mdTypeDef *pTypeDefToken,
            /* [out] */ ClassID *pParentClassId,
            /* [in] */ ULONG32 cNumTypeArgs,
            /* [out] */ ULONG32 *pcNumTypeArgs,
            /* [out] */ ClassID typeArgs[  ]) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetCodeInfo2( 
            /* [in] */ FunctionID functionID,
            /* [in] */ ULONG32 cCodeInfos,
            /* [out] */ ULONG32 *pcCodeInfos,
            /* [length_is][size_is][out] */ COR_PRF_CODE_INFO codeInfos[  ]) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetClassFromTokenAndTypeArgs( 
            /* [in] */ ModuleID moduleID,
            /* [in] */ mdTypeDef typeDef,
            /* [in] */ ULONG32 cTypeArgs,
            /* [size_is][in] */ ClassID typeArgs[  ],
            /* [out] */ ClassID *pClassID) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetFunctionFromTokenAndTypeArgs( 
            /* [in] */ ModuleID moduleID,
            /* [in] */ mdMethodDef funcDef,
            /* [in] */ ClassID classId,
            /* [in] */ ULONG32 cTypeArgs,
            /* [size_is][in] */ ClassID typeArgs[  ],
            /* [out] */ FunctionID *pFunctionID) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE EnumModuleFrozenObjects( 
            /* [in] */ ModuleID moduleID,
            /* [out] */ ICorProfilerObjectEnum **ppEnum) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetArrayObjectInfo( 
            /* [in] */ ObjectID objectId,
            /* [in] */ ULONG32 cDimensions,
            /* [size_is][out] */ ULONG32 pDimensionSizes[  ],
            /* [size_is][out] */ int pDimensionLowerBounds[  ],
            /* [out] */ BYTE **ppData) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetBoxClassLayout( 
            /* [in] */ ClassID classId,
            /* [out] */ ULONG32 *pBufferOffset) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetThreadAppDomain( 
            /* [in] */ ThreadID threadId,
            /* [out] */ AppDomainID *pAppDomainId) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetRVAStaticAddress( 
            /* [in] */ ClassID classId,
            /* [in] */ mdFieldDef fieldToken,
            /* [out] */ void **ppAddress) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetAppDomainStaticAddress( 
            /* [in] */ ClassID classId,
            /* [in] */ mdFieldDef fieldToken,
            /* [in] */ AppDomainID appDomainId,
            /* [out] */ void **ppAddress) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetThreadStaticAddress( 
            /* [in] */ ClassID classId,
            /* [in] */ mdFieldDef fieldToken,
            /* [in] */ ThreadID threadId,
            /* [out] */ void **ppAddress) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetContextStaticAddress( 
            /* [in] */ ClassID classId,
            /* [in] */ mdFieldDef fieldToken,
            /* [in] */ ContextID contextId,
            /* [out] */ void **ppAddress) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetStaticFieldInfo( 
            /* [in] */ ClassID classId,
            /* [in] */ mdFieldDef fieldToken,
            /* [out] */ COR_PRF_STATIC_TYPE *pFieldInfo) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetGenerationBounds( 
            /* [in] */ ULONG cObjectRanges,
            /* [out] */ ULONG *pcObjectRanges,
            /* [length_is][size_is][out] */ COR_PRF_GC_GENERATION_RANGE ranges[  ]) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetObjectGeneration( 
            /* [in] */ ObjectID objectId,
            /* [out] */ COR_PRF_GC_GENERATION_RANGE *range) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetNotifiedExceptionClauseInfo( 
            /* [out] */ COR_PRF_EX_CLAUSE_INFO *pinfo) = 0;
        
    };
    
    
#else   /* C style interface */

    typedef struct ICorProfilerInfo2Vtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            ICorProfilerInfo2 * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            ICorProfilerInfo2 * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            ICorProfilerInfo2 * This);
        
        HRESULT ( STDMETHODCALLTYPE *GetClassFromObject )( 
            ICorProfilerInfo2 * This,
            /* [in] */ ObjectID objectId,
            /* [out] */ ClassID *pClassId);
        
        HRESULT ( STDMETHODCALLTYPE *GetClassFromToken )( 
            ICorProfilerInfo2 * This,
            /* [in] */ ModuleID moduleId,
            /* [in] */ mdTypeDef typeDef,
            /* [out] */ ClassID *pClassId);
        
        HRESULT ( STDMETHODCALLTYPE *GetCodeInfo )( 
            ICorProfilerInfo2 * This,
            /* [in] */ FunctionID functionId,
            /* [out] */ LPCBYTE *pStart,
            /* [out] */ ULONG *pcSize);
        
        HRESULT ( STDMETHODCALLTYPE *GetEventMask )( 
            ICorProfilerInfo2 * This,
            /* [out] */ DWORD *pdwEvents);
        
        HRESULT ( STDMETHODCALLTYPE *GetFunctionFromIP )( 
            ICorProfilerInfo2 * This,
            /* [in] */ LPCBYTE ip,
            /* [out] */ FunctionID *pFunctionId);
        
        HRESULT ( STDMETHODCALLTYPE *GetFunctionFromToken )( 
            ICorProfilerInfo2 * This,
            /* [in] */ ModuleID moduleId,
            /* [in] */ mdToken token,
            /* [out] */ FunctionID *pFunctionId);
        
        HRESULT ( STDMETHODCALLTYPE *GetHandleFromThread )( 
            ICorProfilerInfo2 * This,
            /* [in] */ ThreadID threadId,
            /* [out] */ HANDLE *phThread);
        
        HRESULT ( STDMETHODCALLTYPE *GetObjectSize )( 
            ICorProfilerInfo2 * This,
            /* [in] */ ObjectID objectId,
            /* [out] */ ULONG *pcSize);
        
        HRESULT ( STDMETHODCALLTYPE *IsArrayClass )( 
            ICorProfilerInfo2 * This,
            /* [in] */ ClassID classId,
            /* [out] */ CorElementType *pBaseElemType,
            /* [out] */ ClassID *pBaseClassId,
            /* [out] */ ULONG *pcRank);
        
        HRESULT ( STDMETHODCALLTYPE *GetThreadInfo )( 
            ICorProfilerInfo2 * This,
            /* [in] */ ThreadID threadId,
            /* [out] */ DWORD *pdwWin32ThreadId);
        
        HRESULT ( STDMETHODCALLTYPE *GetCurrentThreadID )( 
            ICorProfilerInfo2 * This,
            /* [out] */ ThreadID *pThreadId);
        
        HRESULT ( STDMETHODCALLTYPE *GetClassIDInfo )( 
            ICorProfilerInfo2 * This,
            /* [in] */ ClassID classId,
            /* [out] */ ModuleID *pModuleId,
            /* [out] */ mdTypeDef *pTypeDefToken);
        
        HRESULT ( STDMETHODCALLTYPE *GetFunctionInfo )( 
            ICorProfilerInfo2 * This,
            /* [in] */ FunctionID functionId,
            /* [out] */ ClassID *pClassId,
            /* [out] */ ModuleID *pModuleId,
            /* [out] */ mdToken *pToken);
        
        HRESULT ( STDMETHODCALLTYPE *SetEventMask )( 
            ICorProfilerInfo2 * This,
            /* [in] */ DWORD dwEvents);
        
        HRESULT ( STDMETHODCALLTYPE *SetEnterLeaveFunctionHooks )( 
            ICorProfilerInfo2 * This,
            /* [in] */ FunctionEnter *pFuncEnter,
            /* [in] */ FunctionLeave *pFuncLeave,
            /* [in] */ FunctionTailcall *pFuncTailcall);
        
        HRESULT ( STDMETHODCALLTYPE *SetFunctionIDMapper )( 
            ICorProfilerInfo2 * This,
            /* [in] */ FunctionIDMapper *pFunc);
        
        HRESULT ( STDMETHODCALLTYPE *GetTokenAndMetaDataFromFunction )( 
            ICorProfilerInfo2 * This,
            /* [in] */ FunctionID functionId,
            /* [in] */ REFIID riid,
            /* [out] */ IUnknown **ppImport,
            /* [out] */ mdToken *pToken);
        
        HRESULT ( STDMETHODCALLTYPE *GetModuleInfo )( 
            ICorProfilerInfo2 * This,
            /* [in] */ ModuleID moduleId,
            /* [out] */ LPCBYTE *ppBaseLoadAddress,
            /* [in] */ ULONG cchName,
            /* [out] */ ULONG *pcchName,
            /* [annotation][out] */ 
            _Out_writes_to_(cchName, *pcchName)  WCHAR szName[  ],
            /* [out] */ AssemblyID *pAssemblyId);
        
        HRESULT ( STDMETHODCALLTYPE *GetModuleMetaData )( 
            ICorProfilerInfo2 * This,
            /* [in] */ ModuleID moduleId,
            /* [in] */ DWORD dwOpenFlags,
            /* [in] */ REFIID riid,
            /* [out] */ IUnknown **ppOut);
        
        HRESULT ( STDMETHODCALLTYPE *GetILFunctionBody )( 
            ICorProfilerInfo2 * This,
            /* [in] */ ModuleID moduleId,
            /* [in] */ mdMethodDef methodId,
            /* [out] */ LPCBYTE *ppMethodHeader,
            /* [out] */ ULONG *pcbMethodSize);
        
        HRESULT ( STDMETHODCALLTYPE *GetILFunctionBodyAllocator )( 
            ICorProfilerInfo2 * This,
            /* [in] */ ModuleID moduleId,
            /* [out] */ IMethodMalloc **ppMalloc);
        
        HRESULT ( STDMETHODCALLTYPE *SetILFunctionBody )( 
            ICorProfilerInfo2 * This,
            /* [in] */ ModuleID moduleId,
            /* [in] */ mdMethodDef methodid,
            /* [in] */ LPCBYTE pbNewILMethodHeader);
        
        HRESULT ( STDMETHODCALLTYPE *GetAppDomainInfo )( 
            ICorProfilerInfo2 * This,
            /* [in] */ AppDomainID appDomainId,
            /* [in] */ ULONG cchName,
            /* [out] */ ULONG *pcchName,
            /* [annotation][out] */ 
            _Out_writes_to_(cchName, *pcchName)  WCHAR szName[  ],
            /* [out] */ ProcessID *pProcessId);
        
        HRESULT ( STDMETHODCALLTYPE *GetAssemblyInfo )( 
            ICorProfilerInfo2 * This,
            /* [in] */ AssemblyID assemblyId,
            /* [in] */ ULONG cchName,
            /* [out] */ ULONG *pcchName,
            /* [annotation][out] */ 
            _Out_writes_to_(cchName, *pcchName)  WCHAR szName[  ],
            /* [out] */ AppDomainID *pAppDomainId,
            /* [out] */ ModuleID *pModuleId);
        
        HRESULT ( STDMETHODCALLTYPE *SetFunctionReJIT )( 
            ICorProfilerInfo2 * This,
            /* [in] */ FunctionID functionId);
        
        HRESULT ( STDMETHODCALLTYPE *ForceGC )( 
            ICorProfilerInfo2 * This);
        
        HRESULT ( STDMETHODCALLTYPE *SetILInstrumentedCodeMap )( 
            ICorProfilerInfo2 * This,
            /* [in] */ FunctionID functionId,
            /* [in] */ BOOL fStartJit,
            /* [in] */ ULONG cILMapEntries,
            /* [size_is][in] */ COR_IL_MAP rgILMapEntries[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *GetInprocInspectionInterface )( 
            ICorProfilerInfo2 * This,
            /* [out] */ IUnknown **ppicd);
        
        HRESULT ( STDMETHODCALLTYPE *GetInprocInspectionIThisThread )( 
            ICorProfilerInfo2 * This,
            /* [out] */ IUnknown **ppicd);
        
        HRESULT ( STDMETHODCALLTYPE *GetThreadContext )( 
            ICorProfilerInfo2 * This,
            /* [in] */ ThreadID threadId,
            /* [out] */ ContextID *pContextId);
        
        HRESULT ( STDMETHODCALLTYPE *BeginInprocDebugging )( 
            ICorProfilerInfo2 * This,
            /* [in] */ BOOL fThisThreadOnly,
            /* [out] */ DWORD *pdwProfilerContext);
        
        HRESULT ( STDMETHODCALLTYPE *EndInprocDebugging )( 
            ICorProfilerInfo2 * This,
            /* [in] */ DWORD dwProfilerContext);
        
        HRESULT ( STDMETHODCALLTYPE *GetILToNativeMapping )( 
            ICorProfilerInfo2 * This,
            /* [in] */ FunctionID functionId,
            /* [in] */ ULONG32 cMap,
            /* [out] */ ULONG32 *pcMap,
            /* [length_is][size_is][out] */ COR_DEBUG_IL_TO_NATIVE_MAP map[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *DoStackSnapshot )( 
            ICorProfilerInfo2 * This,
            /* [in] */ ThreadID thread,
            /* [in] */ StackSnapshotCallback *callback,
            /* [in] */ ULONG32 infoFlags,
            /* [in] */ void *clientData,
            /* [size_is][in] */ BYTE context[  ],
            /* [in] */ ULONG32 contextSize);
        
        HRESULT ( STDMETHODCALLTYPE *SetEnterLeaveFunctionHooks2 )( 
            ICorProfilerInfo2 * This,
            /* [in] */ FunctionEnter2 *pFuncEnter,
            /* [in] */ FunctionLeave2 *pFuncLeave,
            /* [in] */ FunctionTailcall2 *pFuncTailcall);
        
        HRESULT ( STDMETHODCALLTYPE *GetFunctionInfo2 )( 
            ICorProfilerInfo2 * This,
            /* [in] */ FunctionID funcId,
            /* [in] */ COR_PRF_FRAME_INFO frameInfo,
            /* [out] */ ClassID *pClassId,
            /* [out] */ ModuleID *pModuleId,
            /* [out] */ mdToken *pToken,
            /* [in] */ ULONG32 cTypeArgs,
            /* [out] */ ULONG32 *pcTypeArgs,
            /* [out] */ ClassID typeArgs[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *GetStringLayout )( 
            ICorProfilerInfo2 * This,
            /* [out] */ ULONG *pBufferLengthOffset,
            /* [out] */ ULONG *pStringLengthOffset,
            /* [out] */ ULONG *pBufferOffset);
        
        HRESULT ( STDMETHODCALLTYPE *GetClassLayout )( 
            ICorProfilerInfo2 * This,
            /* [in] */ ClassID classID,
            /* [out][in] */ COR_FIELD_OFFSET rFieldOffset[  ],
            /* [in] */ ULONG cFieldOffset,
            /* [out] */ ULONG *pcFieldOffset,
            /* [out] */ ULONG *pulClassSize);
        
        HRESULT ( STDMETHODCALLTYPE *GetClassIDInfo2 )( 
            ICorProfilerInfo2 * This,
            /* [in] */ ClassID classId,
            /* [out] */ ModuleID *pModuleId,
            /* [out] */ mdTypeDef *pTypeDefToken,
            /* [out] */ ClassID *pParentClassId,
            /* [in] */ ULONG32 cNumTypeArgs,
            /* [out] */ ULONG32 *pcNumTypeArgs,
            /* [out] */ ClassID typeArgs[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *GetCodeInfo2 )( 
            ICorProfilerInfo2 * This,
            /* [in] */ FunctionID functionID,
            /* [in] */ ULONG32 cCodeInfos,
            /* [out] */ ULONG32 *pcCodeInfos,
            /* [length_is][size_is][out] */ COR_PRF_CODE_INFO codeInfos[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *GetClassFromTokenAndTypeArgs )( 
            ICorProfilerInfo2 * This,
            /* [in] */ ModuleID moduleID,
            /* [in] */ mdTypeDef typeDef,
            /* [in] */ ULONG32 cTypeArgs,
            /* [size_is][in] */ ClassID typeArgs[  ],
            /* [out] */ ClassID *pClassID);
        
        HRESULT ( STDMETHODCALLTYPE *GetFunctionFromTokenAndTypeArgs )( 
            ICorProfilerInfo2 * This,
            /* [in] */ ModuleID moduleID,
            /* [in] */ mdMethodDef funcDef,
            /* [in] */ ClassID classId,
            /* [in] */ ULONG32 cTypeArgs,
            /* [size_is][in] */ ClassID typeArgs[  ],
            /* [out] */ FunctionID *pFunctionID);
        
        HRESULT ( STDMETHODCALLTYPE *EnumModuleFrozenObjects )( 
            ICorProfilerInfo2 * This,
            /* [in] */ ModuleID moduleID,
            /* [out] */ ICorProfilerObjectEnum **ppEnum);
        
        HRESULT ( STDMETHODCALLTYPE *GetArrayObjectInfo )( 
            ICorProfilerInfo2 * This,
            /* [in] */ ObjectID objectId,
            /* [in] */ ULONG32 cDimensions,
            /* [size_is][out] */ ULONG32 pDimensionSizes[  ],
            /* [size_is][out] */ int pDimensionLowerBounds[  ],
            /* [out] */ BYTE **ppData);
        
        HRESULT ( STDMETHODCALLTYPE *GetBoxClassLayout )( 
            ICorProfilerInfo2 * This,
            /* [in] */ ClassID classId,
            /* [out] */ ULONG32 *pBufferOffset);
        
        HRESULT ( STDMETHODCALLTYPE *GetThreadAppDomain )( 
            ICorProfilerInfo2 * This,
            /* [in] */ ThreadID threadId,
            /* [out] */ AppDomainID *pAppDomainId);
        
        HRESULT ( STDMETHODCALLTYPE *GetRVAStaticAddress )( 
            ICorProfilerInfo2 * This,
            /* [in] */ ClassID classId,
            /* [in] */ mdFieldDef fieldToken,
            /* [out] */ void **ppAddress);
        
        HRESULT ( STDMETHODCALLTYPE *GetAppDomainStaticAddress )( 
            ICorProfilerInfo2 * This,
            /* [in] */ ClassID classId,
            /* [in] */ mdFieldDef fieldToken,
            /* [in] */ AppDomainID appDomainId,
            /* [out] */ void **ppAddress);
        
        HRESULT ( STDMETHODCALLTYPE *GetThreadStaticAddress )( 
            ICorProfilerInfo2 * This,
            /* [in] */ ClassID classId,
            /* [in] */ mdFieldDef fieldToken,
            /* [in] */ ThreadID threadId,
            /* [out] */ void **ppAddress);
        
        HRESULT ( STDMETHODCALLTYPE *GetContextStaticAddress )( 
            ICorProfilerInfo2 * This,
            /* [in] */ ClassID classId,
            /* [in] */ mdFieldDef fieldToken,
            /* [in] */ ContextID contextId,
            /* [out] */ void **ppAddress);
        
        HRESULT ( STDMETHODCALLTYPE *GetStaticFieldInfo )( 
            ICorProfilerInfo2 * This,
            /* [in] */ ClassID classId,
            /* [in] */ mdFieldDef fieldToken,
            /* [out] */ COR_PRF_STATIC_TYPE *pFieldInfo);
        
        HRESULT ( STDMETHODCALLTYPE *GetGenerationBounds )( 
            ICorProfilerInfo2 * This,
            /* [in] */ ULONG cObjectRanges,
            /* [out] */ ULONG *pcObjectRanges,
            /* [length_is][size_is][out] */ COR_PRF_GC_GENERATION_RANGE ranges[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *GetObjectGeneration )( 
            ICorProfilerInfo2 * This,
            /* [in] */ ObjectID objectId,
            /* [out] */ COR_PRF_GC_GENERATION_RANGE *range);
        
        HRESULT ( STDMETHODCALLTYPE *GetNotifiedExceptionClauseInfo )( 
            ICorProfilerInfo2 * This,
            /* [out] */ COR_PRF_EX_CLAUSE_INFO *pinfo);
        
        END_INTERFACE
    } ICorProfilerInfo2Vtbl;

    interface ICorProfilerInfo2
    {
        CONST_VTBL struct ICorProfilerInfo2Vtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define ICorProfilerInfo2_QueryInterface(This,riid,ppvObject)   \
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define ICorProfilerInfo2_AddRef(This)  \
    ( (This)->lpVtbl -> AddRef(This) ) 

#define ICorProfilerInfo2_Release(This) \
    ( (This)->lpVtbl -> Release(This) ) 


#define ICorProfilerInfo2_GetClassFromObject(This,objectId,pClassId)    \
    ( (This)->lpVtbl -> GetClassFromObject(This,objectId,pClassId) ) 

#define ICorProfilerInfo2_GetClassFromToken(This,moduleId,typeDef,pClassId) \
    ( (This)->lpVtbl -> GetClassFromToken(This,moduleId,typeDef,pClassId) ) 

#define ICorProfilerInfo2_GetCodeInfo(This,functionId,pStart,pcSize)    \
    ( (This)->lpVtbl -> GetCodeInfo(This,functionId,pStart,pcSize) ) 

#define ICorProfilerInfo2_GetEventMask(This,pdwEvents)  \
    ( (This)->lpVtbl -> GetEventMask(This,pdwEvents) ) 

#define ICorProfilerInfo2_GetFunctionFromIP(This,ip,pFunctionId)    \
    ( (This)->lpVtbl -> GetFunctionFromIP(This,ip,pFunctionId) ) 

#define ICorProfilerInfo2_GetFunctionFromToken(This,moduleId,token,pFunctionId) \
    ( (This)->lpVtbl -> GetFunctionFromToken(This,moduleId,token,pFunctionId) ) 

#define ICorProfilerInfo2_GetHandleFromThread(This,threadId,phThread)   \
    ( (This)->lpVtbl -> GetHandleFromThread(This,threadId,phThread) ) 

#define ICorProfilerInfo2_GetObjectSize(This,objectId,pcSize)   \
    ( (This)->lpVtbl -> GetObjectSize(This,objectId,pcSize) ) 

#define ICorProfilerInfo2_IsArrayClass(This,classId,pBaseElemType,pBaseClassId,pcRank)  \
    ( (This)->lpVtbl -> IsArrayClass(This,classId,pBaseElemType,pBaseClassId,pcRank) ) 

#define ICorProfilerInfo2_GetThreadInfo(This,threadId,pdwWin32ThreadId) \
    ( (This)->lpVtbl -> GetThreadInfo(This,threadId,pdwWin32ThreadId) ) 

#define ICorProfilerInfo2_GetCurrentThreadID(This,pThreadId)    \
    ( (This)->lpVtbl -> GetCurrentThreadID(This,pThreadId) ) 

#define ICorProfilerInfo2_GetClassIDInfo(This,classId,pModuleId,pTypeDefToken)  \
    ( (This)->lpVtbl -> GetClassIDInfo(This,classId,pModuleId,pTypeDefToken) ) 

#define ICorProfilerInfo2_GetFunctionInfo(This,functionId,pClassId,pModuleId,pToken)    \
    ( (This)->lpVtbl -> GetFunctionInfo(This,functionId,pClassId,pModuleId,pToken) ) 

#define ICorProfilerInfo2_SetEventMask(This,dwEvents)   \
    ( (This)->lpVtbl -> SetEventMask(This,dwEvents) ) 

#define ICorProfilerInfo2_SetEnterLeaveFunctionHooks(This,pFuncEnter,pFuncLeave,pFuncTailcall)  \
    ( (This)->lpVtbl -> SetEnterLeaveFunctionHooks(This,pFuncEnter,pFuncLeave,pFuncTailcall) ) 

#define ICorProfilerInfo2_SetFunctionIDMapper(This,pFunc)   \
    ( (This)->lpVtbl -> SetFunctionIDMapper(This,pFunc) ) 

#define ICorProfilerInfo2_GetTokenAndMetaDataFromFunction(This,functionId,riid,ppImport,pToken) \
    ( (This)->lpVtbl -> GetTokenAndMetaDataFromFunction(This,functionId,riid,ppImport,pToken) ) 

#define ICorProfilerInfo2_GetModuleInfo(This,moduleId,ppBaseLoadAddress,cchName,pcchName,szName,pAssemblyId)    \
    ( (This)->lpVtbl -> GetModuleInfo(This,moduleId,ppBaseLoadAddress,cchName,pcchName,szName,pAssemblyId) ) 

#define ICorProfilerInfo2_GetModuleMetaData(This,moduleId,dwOpenFlags,riid,ppOut)   \
    ( (This)->lpVtbl -> GetModuleMetaData(This,moduleId,dwOpenFlags,riid,ppOut) ) 

#define ICorProfilerInfo2_GetILFunctionBody(This,moduleId,methodId,ppMethodHeader,pcbMethodSize)    \
    ( (This)->lpVtbl -> GetILFunctionBody(This,moduleId,methodId,ppMethodHeader,pcbMethodSize) ) 

#define ICorProfilerInfo2_GetILFunctionBodyAllocator(This,moduleId,ppMalloc)    \
    ( (This)->lpVtbl -> GetILFunctionBodyAllocator(This,moduleId,ppMalloc) ) 

#define ICorProfilerInfo2_SetILFunctionBody(This,moduleId,methodid,pbNewILMethodHeader) \
    ( (This)->lpVtbl -> SetILFunctionBody(This,moduleId,methodid,pbNewILMethodHeader) ) 

#define ICorProfilerInfo2_GetAppDomainInfo(This,appDomainId,cchName,pcchName,szName,pProcessId) \
    ( (This)->lpVtbl -> GetAppDomainInfo(This,appDomainId,cchName,pcchName,szName,pProcessId) ) 

#define ICorProfilerInfo2_GetAssemblyInfo(This,assemblyId,cchName,pcchName,szName,pAppDomainId,pModuleId)   \
    ( (This)->lpVtbl -> GetAssemblyInfo(This,assemblyId,cchName,pcchName,szName,pAppDomainId,pModuleId) ) 

#define ICorProfilerInfo2_SetFunctionReJIT(This,functionId) \
    ( (This)->lpVtbl -> SetFunctionReJIT(This,functionId) ) 

#define ICorProfilerInfo2_ForceGC(This) \
    ( (This)->lpVtbl -> ForceGC(This) ) 

#define ICorProfilerInfo2_SetILInstrumentedCodeMap(This,functionId,fStartJit,cILMapEntries,rgILMapEntries)  \
    ( (This)->lpVtbl -> SetILInstrumentedCodeMap(This,functionId,fStartJit,cILMapEntries,rgILMapEntries) ) 

#define ICorProfilerInfo2_GetInprocInspectionInterface(This,ppicd)  \
    ( (This)->lpVtbl -> GetInprocInspectionInterface(This,ppicd) ) 

#define ICorProfilerInfo2_GetInprocInspectionIThisThread(This,ppicd)    \
    ( (This)->lpVtbl -> GetInprocInspectionIThisThread(This,ppicd) ) 

#define ICorProfilerInfo2_GetThreadContext(This,threadId,pContextId)    \
    ( (This)->lpVtbl -> GetThreadContext(This,threadId,pContextId) ) 

#define ICorProfilerInfo2_BeginInprocDebugging(This,fThisThreadOnly,pdwProfilerContext) \
    ( (This)->lpVtbl -> BeginInprocDebugging(This,fThisThreadOnly,pdwProfilerContext) ) 

#define ICorProfilerInfo2_EndInprocDebugging(This,dwProfilerContext)    \
    ( (This)->lpVtbl -> EndInprocDebugging(This,dwProfilerContext) ) 

#define ICorProfilerInfo2_GetILToNativeMapping(This,functionId,cMap,pcMap,map)  \
    ( (This)->lpVtbl -> GetILToNativeMapping(This,functionId,cMap,pcMap,map) ) 


#define ICorProfilerInfo2_DoStackSnapshot(This,thread,callback,infoFlags,clientData,context,contextSize)    \
    ( (This)->lpVtbl -> DoStackSnapshot(This,thread,callback,infoFlags,clientData,context,contextSize) ) 

#define ICorProfilerInfo2_SetEnterLeaveFunctionHooks2(This,pFuncEnter,pFuncLeave,pFuncTailcall) \
    ( (This)->lpVtbl -> SetEnterLeaveFunctionHooks2(This,pFuncEnter,pFuncLeave,pFuncTailcall) ) 

#define ICorProfilerInfo2_GetFunctionInfo2(This,funcId,frameInfo,pClassId,pModuleId,pToken,cTypeArgs,pcTypeArgs,typeArgs)   \
    ( (This)->lpVtbl -> GetFunctionInfo2(This,funcId,frameInfo,pClassId,pModuleId,pToken,cTypeArgs,pcTypeArgs,typeArgs) ) 

#define ICorProfilerInfo2_GetStringLayout(This,pBufferLengthOffset,pStringLengthOffset,pBufferOffset)   \
    ( (This)->lpVtbl -> GetStringLayout(This,pBufferLengthOffset,pStringLengthOffset,pBufferOffset) ) 

#define ICorProfilerInfo2_GetClassLayout(This,classID,rFieldOffset,cFieldOffset,pcFieldOffset,pulClassSize) \
    ( (This)->lpVtbl -> GetClassLayout(This,classID,rFieldOffset,cFieldOffset,pcFieldOffset,pulClassSize) ) 

#define ICorProfilerInfo2_GetClassIDInfo2(This,classId,pModuleId,pTypeDefToken,pParentClassId,cNumTypeArgs,pcNumTypeArgs,typeArgs)  \
    ( (This)->lpVtbl -> GetClassIDInfo2(This,classId,pModuleId,pTypeDefToken,pParentClassId,cNumTypeArgs,pcNumTypeArgs,typeArgs) ) 

#define ICorProfilerInfo2_GetCodeInfo2(This,functionID,cCodeInfos,pcCodeInfos,codeInfos)    \
    ( (This)->lpVtbl -> GetCodeInfo2(This,functionID,cCodeInfos,pcCodeInfos,codeInfos) ) 

#define ICorProfilerInfo2_GetClassFromTokenAndTypeArgs(This,moduleID,typeDef,cTypeArgs,typeArgs,pClassID)   \
    ( (This)->lpVtbl -> GetClassFromTokenAndTypeArgs(This,moduleID,typeDef,cTypeArgs,typeArgs,pClassID) ) 

#define ICorProfilerInfo2_GetFunctionFromTokenAndTypeArgs(This,moduleID,funcDef,classId,cTypeArgs,typeArgs,pFunctionID) \
    ( (This)->lpVtbl -> GetFunctionFromTokenAndTypeArgs(This,moduleID,funcDef,classId,cTypeArgs,typeArgs,pFunctionID) ) 

#define ICorProfilerInfo2_EnumModuleFrozenObjects(This,moduleID,ppEnum) \
    ( (This)->lpVtbl -> EnumModuleFrozenObjects(This,moduleID,ppEnum) ) 

#define ICorProfilerInfo2_GetArrayObjectInfo(This,objectId,cDimensions,pDimensionSizes,pDimensionLowerBounds,ppData)    \
    ( (This)->lpVtbl -> GetArrayObjectInfo(This,objectId,cDimensions,pDimensionSizes,pDimensionLowerBounds,ppData) ) 

#define ICorProfilerInfo2_GetBoxClassLayout(This,classId,pBufferOffset) \
    ( (This)->lpVtbl -> GetBoxClassLayout(This,classId,pBufferOffset) ) 

#define ICorProfilerInfo2_GetThreadAppDomain(This,threadId,pAppDomainId)    \
    ( (This)->lpVtbl -> GetThreadAppDomain(This,threadId,pAppDomainId) ) 

#define ICorProfilerInfo2_GetRVAStaticAddress(This,classId,fieldToken,ppAddress)    \
    ( (This)->lpVtbl -> GetRVAStaticAddress(This,classId,fieldToken,ppAddress) ) 

#define ICorProfilerInfo2_GetAppDomainStaticAddress(This,classId,fieldToken,appDomainId,ppAddress)  \
    ( (This)->lpVtbl -> GetAppDomainStaticAddress(This,classId,fieldToken,appDomainId,ppAddress) ) 

#define ICorProfilerInfo2_GetThreadStaticAddress(This,classId,fieldToken,threadId,ppAddress)    \
    ( (This)->lpVtbl -> GetThreadStaticAddress(This,classId,fieldToken,threadId,ppAddress) ) 

#define ICorProfilerInfo2_GetContextStaticAddress(This,classId,fieldToken,contextId,ppAddress)  \
    ( (This)->lpVtbl -> GetContextStaticAddress(This,classId,fieldToken,contextId,ppAddress) ) 

#define ICorProfilerInfo2_GetStaticFieldInfo(This,classId,fieldToken,pFieldInfo)    \
    ( (This)->lpVtbl -> GetStaticFieldInfo(This,classId,fieldToken,pFieldInfo) ) 

#define ICorProfilerInfo2_GetGenerationBounds(This,cObjectRanges,pcObjectRanges,ranges) \
    ( (This)->lpVtbl -> GetGenerationBounds(This,cObjectRanges,pcObjectRanges,ranges) ) 

#define ICorProfilerInfo2_GetObjectGeneration(This,objectId,range)  \
    ( (This)->lpVtbl -> GetObjectGeneration(This,objectId,range) ) 

#define ICorProfilerInfo2_GetNotifiedExceptionClauseInfo(This,pinfo)    \
    ( (This)->lpVtbl -> GetNotifiedExceptionClauseInfo(This,pinfo) ) 

#endif /* COBJMACROS */


#endif  /* C style interface */




#endif  /* __ICorProfilerInfo2_INTERFACE_DEFINED__ */


#ifndef __ICorProfilerInfo3_INTERFACE_DEFINED__
#define __ICorProfilerInfo3_INTERFACE_DEFINED__

/* interface ICorProfilerInfo3 */
/* [local][unique][uuid][object] */ 


EXTERN_C const IID IID_ICorProfilerInfo3;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("B555ED4F-452A-4E54-8B39-B5360BAD32A0")
    ICorProfilerInfo3 : public ICorProfilerInfo2
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE EnumJITedFunctions( 
            /* [out] */ ICorProfilerFunctionEnum **ppEnum) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE RequestProfilerDetach( 
            /* [in] */ DWORD dwExpectedCompletionMilliseconds) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE SetFunctionIDMapper2( 
            /* [in] */ FunctionIDMapper2 *pFunc,
            /* [in] */ void *clientData) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetStringLayout2( 
            /* [out] */ ULONG *pStringLengthOffset,
            /* [out] */ ULONG *pBufferOffset) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE SetEnterLeaveFunctionHooks3( 
            /* [in] */ FunctionEnter3 *pFuncEnter3,
            /* [in] */ FunctionLeave3 *pFuncLeave3,
            /* [in] */ FunctionTailcall3 *pFuncTailcall3) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE SetEnterLeaveFunctionHooks3WithInfo( 
            /* [in] */ FunctionEnter3WithInfo *pFuncEnter3WithInfo,
            /* [in] */ FunctionLeave3WithInfo *pFuncLeave3WithInfo,
            /* [in] */ FunctionTailcall3WithInfo *pFuncTailcall3WithInfo) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetFunctionEnter3Info( 
            /* [in] */ FunctionID functionId,
            /* [in] */ COR_PRF_ELT_INFO eltInfo,
            /* [out] */ COR_PRF_FRAME_INFO *pFrameInfo,
            /* [out][in] */ ULONG *pcbArgumentInfo,
            /* [size_is][out] */ COR_PRF_FUNCTION_ARGUMENT_INFO *pArgumentInfo) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetFunctionLeave3Info( 
            /* [in] */ FunctionID functionId,
            /* [in] */ COR_PRF_ELT_INFO eltInfo,
            /* [out] */ COR_PRF_FRAME_INFO *pFrameInfo,
            /* [out] */ COR_PRF_FUNCTION_ARGUMENT_RANGE *pRetvalRange) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetFunctionTailcall3Info( 
            /* [in] */ FunctionID functionId,
            /* [in] */ COR_PRF_ELT_INFO eltInfo,
            /* [out] */ COR_PRF_FRAME_INFO *pFrameInfo) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE EnumModules( 
            /* [out] */ ICorProfilerModuleEnum **ppEnum) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetRuntimeInformation( 
            /* [out] */ USHORT *pClrInstanceId,
            /* [out] */ COR_PRF_RUNTIME_TYPE *pRuntimeType,
            /* [out] */ USHORT *pMajorVersion,
            /* [out] */ USHORT *pMinorVersion,
            /* [out] */ USHORT *pBuildNumber,
            /* [out] */ USHORT *pQFEVersion,
            /* [in] */ ULONG cchVersionString,
            /* [out] */ ULONG *pcchVersionString,
            /* [annotation][out] */ 
            _Out_writes_to_(cchVersionString, *pcchVersionString)  WCHAR szVersionString[  ]) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetThreadStaticAddress2( 
            /* [in] */ ClassID classId,
            /* [in] */ mdFieldDef fieldToken,
            /* [in] */ AppDomainID appDomainId,
            /* [in] */ ThreadID threadId,
            /* [out] */ void **ppAddress) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetAppDomainsContainingModule( 
            /* [in] */ ModuleID moduleId,
            /* [in] */ ULONG32 cAppDomainIds,
            /* [out] */ ULONG32 *pcAppDomainIds,
            /* [length_is][size_is][out] */ AppDomainID appDomainIds[  ]) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetModuleInfo2( 
            /* [in] */ ModuleID moduleId,
            /* [out] */ LPCBYTE *ppBaseLoadAddress,
            /* [in] */ ULONG cchName,
            /* [out] */ ULONG *pcchName,
            /* [annotation][out] */ 
            _Out_writes_to_(cchName, *pcchName)  WCHAR szName[  ],
            /* [out] */ AssemblyID *pAssemblyId,
            /* [out] */ DWORD *pdwModuleFlags) = 0;
        
    };
    
    
#else   /* C style interface */

    typedef struct ICorProfilerInfo3Vtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            ICorProfilerInfo3 * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            ICorProfilerInfo3 * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            ICorProfilerInfo3 * This);
        
        HRESULT ( STDMETHODCALLTYPE *GetClassFromObject )( 
            ICorProfilerInfo3 * This,
            /* [in] */ ObjectID objectId,
            /* [out] */ ClassID *pClassId);
        
        HRESULT ( STDMETHODCALLTYPE *GetClassFromToken )( 
            ICorProfilerInfo3 * This,
            /* [in] */ ModuleID moduleId,
            /* [in] */ mdTypeDef typeDef,
            /* [out] */ ClassID *pClassId);
        
        HRESULT ( STDMETHODCALLTYPE *GetCodeInfo )( 
            ICorProfilerInfo3 * This,
            /* [in] */ FunctionID functionId,
            /* [out] */ LPCBYTE *pStart,
            /* [out] */ ULONG *pcSize);
        
        HRESULT ( STDMETHODCALLTYPE *GetEventMask )( 
            ICorProfilerInfo3 * This,
            /* [out] */ DWORD *pdwEvents);
        
        HRESULT ( STDMETHODCALLTYPE *GetFunctionFromIP )( 
            ICorProfilerInfo3 * This,
            /* [in] */ LPCBYTE ip,
            /* [out] */ FunctionID *pFunctionId);
        
        HRESULT ( STDMETHODCALLTYPE *GetFunctionFromToken )( 
            ICorProfilerInfo3 * This,
            /* [in] */ ModuleID moduleId,
            /* [in] */ mdToken token,
            /* [out] */ FunctionID *pFunctionId);
        
        HRESULT ( STDMETHODCALLTYPE *GetHandleFromThread )( 
            ICorProfilerInfo3 * This,
            /* [in] */ ThreadID threadId,
            /* [out] */ HANDLE *phThread);
        
        HRESULT ( STDMETHODCALLTYPE *GetObjectSize )( 
            ICorProfilerInfo3 * This,
            /* [in] */ ObjectID objectId,
            /* [out] */ ULONG *pcSize);
        
        HRESULT ( STDMETHODCALLTYPE *IsArrayClass )( 
            ICorProfilerInfo3 * This,
            /* [in] */ ClassID classId,
            /* [out] */ CorElementType *pBaseElemType,
            /* [out] */ ClassID *pBaseClassId,
            /* [out] */ ULONG *pcRank);
        
        HRESULT ( STDMETHODCALLTYPE *GetThreadInfo )( 
            ICorProfilerInfo3 * This,
            /* [in] */ ThreadID threadId,
            /* [out] */ DWORD *pdwWin32ThreadId);
        
        HRESULT ( STDMETHODCALLTYPE *GetCurrentThreadID )( 
            ICorProfilerInfo3 * This,
            /* [out] */ ThreadID *pThreadId);
        
        HRESULT ( STDMETHODCALLTYPE *GetClassIDInfo )( 
            ICorProfilerInfo3 * This,
            /* [in] */ ClassID classId,
            /* [out] */ ModuleID *pModuleId,
            /* [out] */ mdTypeDef *pTypeDefToken);
        
        HRESULT ( STDMETHODCALLTYPE *GetFunctionInfo )( 
            ICorProfilerInfo3 * This,
            /* [in] */ FunctionID functionId,
            /* [out] */ ClassID *pClassId,
            /* [out] */ ModuleID *pModuleId,
            /* [out] */ mdToken *pToken);
        
        HRESULT ( STDMETHODCALLTYPE *SetEventMask )( 
            ICorProfilerInfo3 * This,
            /* [in] */ DWORD dwEvents);
        
        HRESULT ( STDMETHODCALLTYPE *SetEnterLeaveFunctionHooks )( 
            ICorProfilerInfo3 * This,
            /* [in] */ FunctionEnter *pFuncEnter,
            /* [in] */ FunctionLeave *pFuncLeave,
            /* [in] */ FunctionTailcall *pFuncTailcall);
        
        HRESULT ( STDMETHODCALLTYPE *SetFunctionIDMapper )( 
            ICorProfilerInfo3 * This,
            /* [in] */ FunctionIDMapper *pFunc);
        
        HRESULT ( STDMETHODCALLTYPE *GetTokenAndMetaDataFromFunction )( 
            ICorProfilerInfo3 * This,
            /* [in] */ FunctionID functionId,
            /* [in] */ REFIID riid,
            /* [out] */ IUnknown **ppImport,
            /* [out] */ mdToken *pToken);
        
        HRESULT ( STDMETHODCALLTYPE *GetModuleInfo )( 
            ICorProfilerInfo3 * This,
            /* [in] */ ModuleID moduleId,
            /* [out] */ LPCBYTE *ppBaseLoadAddress,
            /* [in] */ ULONG cchName,
            /* [out] */ ULONG *pcchName,
            /* [annotation][out] */ 
            _Out_writes_to_(cchName, *pcchName)  WCHAR szName[  ],
            /* [out] */ AssemblyID *pAssemblyId);
        
        HRESULT ( STDMETHODCALLTYPE *GetModuleMetaData )( 
            ICorProfilerInfo3 * This,
            /* [in] */ ModuleID moduleId,
            /* [in] */ DWORD dwOpenFlags,
            /* [in] */ REFIID riid,
            /* [out] */ IUnknown **ppOut);
        
        HRESULT ( STDMETHODCALLTYPE *GetILFunctionBody )( 
            ICorProfilerInfo3 * This,
            /* [in] */ ModuleID moduleId,
            /* [in] */ mdMethodDef methodId,
            /* [out] */ LPCBYTE *ppMethodHeader,
            /* [out] */ ULONG *pcbMethodSize);
        
        HRESULT ( STDMETHODCALLTYPE *GetILFunctionBodyAllocator )( 
            ICorProfilerInfo3 * This,
            /* [in] */ ModuleID moduleId,
            /* [out] */ IMethodMalloc **ppMalloc);
        
        HRESULT ( STDMETHODCALLTYPE *SetILFunctionBody )( 
            ICorProfilerInfo3 * This,
            /* [in] */ ModuleID moduleId,
            /* [in] */ mdMethodDef methodid,
            /* [in] */ LPCBYTE pbNewILMethodHeader);
        
        HRESULT ( STDMETHODCALLTYPE *GetAppDomainInfo )( 
            ICorProfilerInfo3 * This,
            /* [in] */ AppDomainID appDomainId,
            /* [in] */ ULONG cchName,
            /* [out] */ ULONG *pcchName,
            /* [annotation][out] */ 
            _Out_writes_to_(cchName, *pcchName)  WCHAR szName[  ],
            /* [out] */ ProcessID *pProcessId);
        
        HRESULT ( STDMETHODCALLTYPE *GetAssemblyInfo )( 
            ICorProfilerInfo3 * This,
            /* [in] */ AssemblyID assemblyId,
            /* [in] */ ULONG cchName,
            /* [out] */ ULONG *pcchName,
            /* [annotation][out] */ 
            _Out_writes_to_(cchName, *pcchName)  WCHAR szName[  ],
            /* [out] */ AppDomainID *pAppDomainId,
            /* [out] */ ModuleID *pModuleId);
        
        HRESULT ( STDMETHODCALLTYPE *SetFunctionReJIT )( 
            ICorProfilerInfo3 * This,
            /* [in] */ FunctionID functionId);
        
        HRESULT ( STDMETHODCALLTYPE *ForceGC )( 
            ICorProfilerInfo3 * This);
        
        HRESULT ( STDMETHODCALLTYPE *SetILInstrumentedCodeMap )( 
            ICorProfilerInfo3 * This,
            /* [in] */ FunctionID functionId,
            /* [in] */ BOOL fStartJit,
            /* [in] */ ULONG cILMapEntries,
            /* [size_is][in] */ COR_IL_MAP rgILMapEntries[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *GetInprocInspectionInterface )( 
            ICorProfilerInfo3 * This,
            /* [out] */ IUnknown **ppicd);
        
        HRESULT ( STDMETHODCALLTYPE *GetInprocInspectionIThisThread )( 
            ICorProfilerInfo3 * This,
            /* [out] */ IUnknown **ppicd);
        
        HRESULT ( STDMETHODCALLTYPE *GetThreadContext )( 
            ICorProfilerInfo3 * This,
            /* [in] */ ThreadID threadId,
            /* [out] */ ContextID *pContextId);
        
        HRESULT ( STDMETHODCALLTYPE *BeginInprocDebugging )( 
            ICorProfilerInfo3 * This,
            /* [in] */ BOOL fThisThreadOnly,
            /* [out] */ DWORD *pdwProfilerContext);
        
        HRESULT ( STDMETHODCALLTYPE *EndInprocDebugging )( 
            ICorProfilerInfo3 * This,
            /* [in] */ DWORD dwProfilerContext);
        
        HRESULT ( STDMETHODCALLTYPE *GetILToNativeMapping )( 
            ICorProfilerInfo3 * This,
            /* [in] */ FunctionID functionId,
            /* [in] */ ULONG32 cMap,
            /* [out] */ ULONG32 *pcMap,
            /* [length_is][size_is][out] */ COR_DEBUG_IL_TO_NATIVE_MAP map[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *DoStackSnapshot )( 
            ICorProfilerInfo3 * This,
            /* [in] */ ThreadID thread,
            /* [in] */ StackSnapshotCallback *callback,
            /* [in] */ ULONG32 infoFlags,
            /* [in] */ void *clientData,
            /* [size_is][in] */ BYTE context[  ],
            /* [in] */ ULONG32 contextSize);
        
        HRESULT ( STDMETHODCALLTYPE *SetEnterLeaveFunctionHooks2 )( 
            ICorProfilerInfo3 * This,
            /* [in] */ FunctionEnter2 *pFuncEnter,
            /* [in] */ FunctionLeave2 *pFuncLeave,
            /* [in] */ FunctionTailcall2 *pFuncTailcall);
        
        HRESULT ( STDMETHODCALLTYPE *GetFunctionInfo2 )( 
            ICorProfilerInfo3 * This,
            /* [in] */ FunctionID funcId,
            /* [in] */ COR_PRF_FRAME_INFO frameInfo,
            /* [out] */ ClassID *pClassId,
            /* [out] */ ModuleID *pModuleId,
            /* [out] */ mdToken *pToken,
            /* [in] */ ULONG32 cTypeArgs,
            /* [out] */ ULONG32 *pcTypeArgs,
            /* [out] */ ClassID typeArgs[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *GetStringLayout )( 
            ICorProfilerInfo3 * This,
            /* [out] */ ULONG *pBufferLengthOffset,
            /* [out] */ ULONG *pStringLengthOffset,
            /* [out] */ ULONG *pBufferOffset);
        
        HRESULT ( STDMETHODCALLTYPE *GetClassLayout )( 
            ICorProfilerInfo3 * This,
            /* [in] */ ClassID classID,
            /* [out][in] */ COR_FIELD_OFFSET rFieldOffset[  ],
            /* [in] */ ULONG cFieldOffset,
            /* [out] */ ULONG *pcFieldOffset,
            /* [out] */ ULONG *pulClassSize);
        
        HRESULT ( STDMETHODCALLTYPE *GetClassIDInfo2 )( 
            ICorProfilerInfo3 * This,
            /* [in] */ ClassID classId,
            /* [out] */ ModuleID *pModuleId,
            /* [out] */ mdTypeDef *pTypeDefToken,
            /* [out] */ ClassID *pParentClassId,
            /* [in] */ ULONG32 cNumTypeArgs,
            /* [out] */ ULONG32 *pcNumTypeArgs,
            /* [out] */ ClassID typeArgs[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *GetCodeInfo2 )( 
            ICorProfilerInfo3 * This,
            /* [in] */ FunctionID functionID,
            /* [in] */ ULONG32 cCodeInfos,
            /* [out] */ ULONG32 *pcCodeInfos,
            /* [length_is][size_is][out] */ COR_PRF_CODE_INFO codeInfos[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *GetClassFromTokenAndTypeArgs )( 
            ICorProfilerInfo3 * This,
            /* [in] */ ModuleID moduleID,
            /* [in] */ mdTypeDef typeDef,
            /* [in] */ ULONG32 cTypeArgs,
            /* [size_is][in] */ ClassID typeArgs[  ],
            /* [out] */ ClassID *pClassID);
        
        HRESULT ( STDMETHODCALLTYPE *GetFunctionFromTokenAndTypeArgs )( 
            ICorProfilerInfo3 * This,
            /* [in] */ ModuleID moduleID,
            /* [in] */ mdMethodDef funcDef,
            /* [in] */ ClassID classId,
            /* [in] */ ULONG32 cTypeArgs,
            /* [size_is][in] */ ClassID typeArgs[  ],
            /* [out] */ FunctionID *pFunctionID);
        
        HRESULT ( STDMETHODCALLTYPE *EnumModuleFrozenObjects )( 
            ICorProfilerInfo3 * This,
            /* [in] */ ModuleID moduleID,
            /* [out] */ ICorProfilerObjectEnum **ppEnum);
        
        HRESULT ( STDMETHODCALLTYPE *GetArrayObjectInfo )( 
            ICorProfilerInfo3 * This,
            /* [in] */ ObjectID objectId,
            /* [in] */ ULONG32 cDimensions,
            /* [size_is][out] */ ULONG32 pDimensionSizes[  ],
            /* [size_is][out] */ int pDimensionLowerBounds[  ],
            /* [out] */ BYTE **ppData);
        
        HRESULT ( STDMETHODCALLTYPE *GetBoxClassLayout )( 
            ICorProfilerInfo3 * This,
            /* [in] */ ClassID classId,
            /* [out] */ ULONG32 *pBufferOffset);
        
        HRESULT ( STDMETHODCALLTYPE *GetThreadAppDomain )( 
            ICorProfilerInfo3 * This,
            /* [in] */ ThreadID threadId,
            /* [out] */ AppDomainID *pAppDomainId);
        
        HRESULT ( STDMETHODCALLTYPE *GetRVAStaticAddress )( 
            ICorProfilerInfo3 * This,
            /* [in] */ ClassID classId,
            /* [in] */ mdFieldDef fieldToken,
            /* [out] */ void **ppAddress);
        
        HRESULT ( STDMETHODCALLTYPE *GetAppDomainStaticAddress )( 
            ICorProfilerInfo3 * This,
            /* [in] */ ClassID classId,
            /* [in] */ mdFieldDef fieldToken,
            /* [in] */ AppDomainID appDomainId,
            /* [out] */ void **ppAddress);
        
        HRESULT ( STDMETHODCALLTYPE *GetThreadStaticAddress )( 
            ICorProfilerInfo3 * This,
            /* [in] */ ClassID classId,
            /* [in] */ mdFieldDef fieldToken,
            /* [in] */ ThreadID threadId,
            /* [out] */ void **ppAddress);
        
        HRESULT ( STDMETHODCALLTYPE *GetContextStaticAddress )( 
            ICorProfilerInfo3 * This,
            /* [in] */ ClassID classId,
            /* [in] */ mdFieldDef fieldToken,
            /* [in] */ ContextID contextId,
            /* [out] */ void **ppAddress);
        
        HRESULT ( STDMETHODCALLTYPE *GetStaticFieldInfo )( 
            ICorProfilerInfo3 * This,
            /* [in] */ ClassID classId,
            /* [in] */ mdFieldDef fieldToken,
            /* [out] */ COR_PRF_STATIC_TYPE *pFieldInfo);
        
        HRESULT ( STDMETHODCALLTYPE *GetGenerationBounds )( 
            ICorProfilerInfo3 * This,
            /* [in] */ ULONG cObjectRanges,
            /* [out] */ ULONG *pcObjectRanges,
            /* [length_is][size_is][out] */ COR_PRF_GC_GENERATION_RANGE ranges[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *GetObjectGeneration )( 
            ICorProfilerInfo3 * This,
            /* [in] */ ObjectID objectId,
            /* [out] */ COR_PRF_GC_GENERATION_RANGE *range);
        
        HRESULT ( STDMETHODCALLTYPE *GetNotifiedExceptionClauseInfo )( 
            ICorProfilerInfo3 * This,
            /* [out] */ COR_PRF_EX_CLAUSE_INFO *pinfo);
        
        HRESULT ( STDMETHODCALLTYPE *EnumJITedFunctions )( 
            ICorProfilerInfo3 * This,
            /* [out] */ ICorProfilerFunctionEnum **ppEnum);
        
        HRESULT ( STDMETHODCALLTYPE *RequestProfilerDetach )( 
            ICorProfilerInfo3 * This,
            /* [in] */ DWORD dwExpectedCompletionMilliseconds);
        
        HRESULT ( STDMETHODCALLTYPE *SetFunctionIDMapper2 )( 
            ICorProfilerInfo3 * This,
            /* [in] */ FunctionIDMapper2 *pFunc,
            /* [in] */ void *clientData);
        
        HRESULT ( STDMETHODCALLTYPE *GetStringLayout2 )( 
            ICorProfilerInfo3 * This,
            /* [out] */ ULONG *pStringLengthOffset,
            /* [out] */ ULONG *pBufferOffset);
        
        HRESULT ( STDMETHODCALLTYPE *SetEnterLeaveFunctionHooks3 )( 
            ICorProfilerInfo3 * This,
            /* [in] */ FunctionEnter3 *pFuncEnter3,
            /* [in] */ FunctionLeave3 *pFuncLeave3,
            /* [in] */ FunctionTailcall3 *pFuncTailcall3);
        
        HRESULT ( STDMETHODCALLTYPE *SetEnterLeaveFunctionHooks3WithInfo )( 
            ICorProfilerInfo3 * This,
            /* [in] */ FunctionEnter3WithInfo *pFuncEnter3WithInfo,
            /* [in] */ FunctionLeave3WithInfo *pFuncLeave3WithInfo,
            /* [in] */ FunctionTailcall3WithInfo *pFuncTailcall3WithInfo);
        
        HRESULT ( STDMETHODCALLTYPE *GetFunctionEnter3Info )( 
            ICorProfilerInfo3 * This,
            /* [in] */ FunctionID functionId,
            /* [in] */ COR_PRF_ELT_INFO eltInfo,
            /* [out] */ COR_PRF_FRAME_INFO *pFrameInfo,
            /* [out][in] */ ULONG *pcbArgumentInfo,
            /* [size_is][out] */ COR_PRF_FUNCTION_ARGUMENT_INFO *pArgumentInfo);
        
        HRESULT ( STDMETHODCALLTYPE *GetFunctionLeave3Info )( 
            ICorProfilerInfo3 * This,
            /* [in] */ FunctionID functionId,
            /* [in] */ COR_PRF_ELT_INFO eltInfo,
            /* [out] */ COR_PRF_FRAME_INFO *pFrameInfo,
            /* [out] */ COR_PRF_FUNCTION_ARGUMENT_RANGE *pRetvalRange);
        
        HRESULT ( STDMETHODCALLTYPE *GetFunctionTailcall3Info )( 
            ICorProfilerInfo3 * This,
            /* [in] */ FunctionID functionId,
            /* [in] */ COR_PRF_ELT_INFO eltInfo,
            /* [out] */ COR_PRF_FRAME_INFO *pFrameInfo);
        
        HRESULT ( STDMETHODCALLTYPE *EnumModules )( 
            ICorProfilerInfo3 * This,
            /* [out] */ ICorProfilerModuleEnum **ppEnum);
        
        HRESULT ( STDMETHODCALLTYPE *GetRuntimeInformation )( 
            ICorProfilerInfo3 * This,
            /* [out] */ USHORT *pClrInstanceId,
            /* [out] */ COR_PRF_RUNTIME_TYPE *pRuntimeType,
            /* [out] */ USHORT *pMajorVersion,
            /* [out] */ USHORT *pMinorVersion,
            /* [out] */ USHORT *pBuildNumber,
            /* [out] */ USHORT *pQFEVersion,
            /* [in] */ ULONG cchVersionString,
            /* [out] */ ULONG *pcchVersionString,
            /* [annotation][out] */ 
            _Out_writes_to_(cchVersionString, *pcchVersionString)  WCHAR szVersionString[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *GetThreadStaticAddress2 )( 
            ICorProfilerInfo3 * This,
            /* [in] */ ClassID classId,
            /* [in] */ mdFieldDef fieldToken,
            /* [in] */ AppDomainID appDomainId,
            /* [in] */ ThreadID threadId,
            /* [out] */ void **ppAddress);
        
        HRESULT ( STDMETHODCALLTYPE *GetAppDomainsContainingModule )( 
            ICorProfilerInfo3 * This,
            /* [in] */ ModuleID moduleId,
            /* [in] */ ULONG32 cAppDomainIds,
            /* [out] */ ULONG32 *pcAppDomainIds,
            /* [length_is][size_is][out] */ AppDomainID appDomainIds[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *GetModuleInfo2 )( 
            ICorProfilerInfo3 * This,
            /* [in] */ ModuleID moduleId,
            /* [out] */ LPCBYTE *ppBaseLoadAddress,
            /* [in] */ ULONG cchName,
            /* [out] */ ULONG *pcchName,
            /* [annotation][out] */ 
            _Out_writes_to_(cchName, *pcchName)  WCHAR szName[  ],
            /* [out] */ AssemblyID *pAssemblyId,
            /* [out] */ DWORD *pdwModuleFlags);
        
        END_INTERFACE
    } ICorProfilerInfo3Vtbl;

    interface ICorProfilerInfo3
    {
        CONST_VTBL struct ICorProfilerInfo3Vtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define ICorProfilerInfo3_QueryInterface(This,riid,ppvObject)   \
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define ICorProfilerInfo3_AddRef(This)  \
    ( (This)->lpVtbl -> AddRef(This) ) 

#define ICorProfilerInfo3_Release(This) \
    ( (This)->lpVtbl -> Release(This) ) 


#define ICorProfilerInfo3_GetClassFromObject(This,objectId,pClassId)    \
    ( (This)->lpVtbl -> GetClassFromObject(This,objectId,pClassId) ) 

#define ICorProfilerInfo3_GetClassFromToken(This,moduleId,typeDef,pClassId) \
    ( (This)->lpVtbl -> GetClassFromToken(This,moduleId,typeDef,pClassId) ) 

#define ICorProfilerInfo3_GetCodeInfo(This,functionId,pStart,pcSize)    \
    ( (This)->lpVtbl -> GetCodeInfo(This,functionId,pStart,pcSize) ) 

#define ICorProfilerInfo3_GetEventMask(This,pdwEvents)  \
    ( (This)->lpVtbl -> GetEventMask(This,pdwEvents) ) 

#define ICorProfilerInfo3_GetFunctionFromIP(This,ip,pFunctionId)    \
    ( (This)->lpVtbl -> GetFunctionFromIP(This,ip,pFunctionId) ) 

#define ICorProfilerInfo3_GetFunctionFromToken(This,moduleId,token,pFunctionId) \
    ( (This)->lpVtbl -> GetFunctionFromToken(This,moduleId,token,pFunctionId) ) 

#define ICorProfilerInfo3_GetHandleFromThread(This,threadId,phThread)   \
    ( (This)->lpVtbl -> GetHandleFromThread(This,threadId,phThread) ) 

#define ICorProfilerInfo3_GetObjectSize(This,objectId,pcSize)   \
    ( (This)->lpVtbl -> GetObjectSize(This,objectId,pcSize) ) 

#define ICorProfilerInfo3_IsArrayClass(This,classId,pBaseElemType,pBaseClassId,pcRank)  \
    ( (This)->lpVtbl -> IsArrayClass(This,classId,pBaseElemType,pBaseClassId,pcRank) ) 

#define ICorProfilerInfo3_GetThreadInfo(This,threadId,pdwWin32ThreadId) \
    ( (This)->lpVtbl -> GetThreadInfo(This,threadId,pdwWin32ThreadId) ) 

#define ICorProfilerInfo3_GetCurrentThreadID(This,pThreadId)    \
    ( (This)->lpVtbl -> GetCurrentThreadID(This,pThreadId) ) 

#define ICorProfilerInfo3_GetClassIDInfo(This,classId,pModuleId,pTypeDefToken)  \
    ( (This)->lpVtbl -> GetClassIDInfo(This,classId,pModuleId,pTypeDefToken) ) 

#define ICorProfilerInfo3_GetFunctionInfo(This,functionId,pClassId,pModuleId,pToken)    \
    ( (This)->lpVtbl -> GetFunctionInfo(This,functionId,pClassId,pModuleId,pToken) ) 

#define ICorProfilerInfo3_SetEventMask(This,dwEvents)   \
    ( (This)->lpVtbl -> SetEventMask(This,dwEvents) ) 

#define ICorProfilerInfo3_SetEnterLeaveFunctionHooks(This,pFuncEnter,pFuncLeave,pFuncTailcall)  \
    ( (This)->lpVtbl -> SetEnterLeaveFunctionHooks(This,pFuncEnter,pFuncLeave,pFuncTailcall) ) 

#define ICorProfilerInfo3_SetFunctionIDMapper(This,pFunc)   \
    ( (This)->lpVtbl -> SetFunctionIDMapper(This,pFunc) ) 

#define ICorProfilerInfo3_GetTokenAndMetaDataFromFunction(This,functionId,riid,ppImport,pToken) \
    ( (This)->lpVtbl -> GetTokenAndMetaDataFromFunction(This,functionId,riid,ppImport,pToken) ) 

#define ICorProfilerInfo3_GetModuleInfo(This,moduleId,ppBaseLoadAddress,cchName,pcchName,szName,pAssemblyId)    \
    ( (This)->lpVtbl -> GetModuleInfo(This,moduleId,ppBaseLoadAddress,cchName,pcchName,szName,pAssemblyId) ) 

#define ICorProfilerInfo3_GetModuleMetaData(This,moduleId,dwOpenFlags,riid,ppOut)   \
    ( (This)->lpVtbl -> GetModuleMetaData(This,moduleId,dwOpenFlags,riid,ppOut) ) 

#define ICorProfilerInfo3_GetILFunctionBody(This,moduleId,methodId,ppMethodHeader,pcbMethodSize)    \
    ( (This)->lpVtbl -> GetILFunctionBody(This,moduleId,methodId,ppMethodHeader,pcbMethodSize) ) 

#define ICorProfilerInfo3_GetILFunctionBodyAllocator(This,moduleId,ppMalloc)    \
    ( (This)->lpVtbl -> GetILFunctionBodyAllocator(This,moduleId,ppMalloc) ) 

#define ICorProfilerInfo3_SetILFunctionBody(This,moduleId,methodid,pbNewILMethodHeader) \
    ( (This)->lpVtbl -> SetILFunctionBody(This,moduleId,methodid,pbNewILMethodHeader) ) 

#define ICorProfilerInfo3_GetAppDomainInfo(This,appDomainId,cchName,pcchName,szName,pProcessId) \
    ( (This)->lpVtbl -> GetAppDomainInfo(This,appDomainId,cchName,pcchName,szName,pProcessId) ) 

#define ICorProfilerInfo3_GetAssemblyInfo(This,assemblyId,cchName,pcchName,szName,pAppDomainId,pModuleId)   \
    ( (This)->lpVtbl -> GetAssemblyInfo(This,assemblyId,cchName,pcchName,szName,pAppDomainId,pModuleId) ) 

#define ICorProfilerInfo3_SetFunctionReJIT(This,functionId) \
    ( (This)->lpVtbl -> SetFunctionReJIT(This,functionId) ) 

#define ICorProfilerInfo3_ForceGC(This) \
    ( (This)->lpVtbl -> ForceGC(This) ) 

#define ICorProfilerInfo3_SetILInstrumentedCodeMap(This,functionId,fStartJit,cILMapEntries,rgILMapEntries)  \
    ( (This)->lpVtbl -> SetILInstrumentedCodeMap(This,functionId,fStartJit,cILMapEntries,rgILMapEntries) ) 

#define ICorProfilerInfo3_GetInprocInspectionInterface(This,ppicd)  \
    ( (This)->lpVtbl -> GetInprocInspectionInterface(This,ppicd) ) 

#define ICorProfilerInfo3_GetInprocInspectionIThisThread(This,ppicd)    \
    ( (This)->lpVtbl -> GetInprocInspectionIThisThread(This,ppicd) ) 

#define ICorProfilerInfo3_GetThreadContext(This,threadId,pContextId)    \
    ( (This)->lpVtbl -> GetThreadContext(This,threadId,pContextId) ) 

#define ICorProfilerInfo3_BeginInprocDebugging(This,fThisThreadOnly,pdwProfilerContext) \
    ( (This)->lpVtbl -> BeginInprocDebugging(This,fThisThreadOnly,pdwProfilerContext) ) 

#define ICorProfilerInfo3_EndInprocDebugging(This,dwProfilerContext)    \
    ( (This)->lpVtbl -> EndInprocDebugging(This,dwProfilerContext) ) 

#define ICorProfilerInfo3_GetILToNativeMapping(This,functionId,cMap,pcMap,map)  \
    ( (This)->lpVtbl -> GetILToNativeMapping(This,functionId,cMap,pcMap,map) ) 


#define ICorProfilerInfo3_DoStackSnapshot(This,thread,callback,infoFlags,clientData,context,contextSize)    \
    ( (This)->lpVtbl -> DoStackSnapshot(This,thread,callback,infoFlags,clientData,context,contextSize) ) 

#define ICorProfilerInfo3_SetEnterLeaveFunctionHooks2(This,pFuncEnter,pFuncLeave,pFuncTailcall) \
    ( (This)->lpVtbl -> SetEnterLeaveFunctionHooks2(This,pFuncEnter,pFuncLeave,pFuncTailcall) ) 

#define ICorProfilerInfo3_GetFunctionInfo2(This,funcId,frameInfo,pClassId,pModuleId,pToken,cTypeArgs,pcTypeArgs,typeArgs)   \
    ( (This)->lpVtbl -> GetFunctionInfo2(This,funcId,frameInfo,pClassId,pModuleId,pToken,cTypeArgs,pcTypeArgs,typeArgs) ) 

#define ICorProfilerInfo3_GetStringLayout(This,pBufferLengthOffset,pStringLengthOffset,pBufferOffset)   \
    ( (This)->lpVtbl -> GetStringLayout(This,pBufferLengthOffset,pStringLengthOffset,pBufferOffset) ) 

#define ICorProfilerInfo3_GetClassLayout(This,classID,rFieldOffset,cFieldOffset,pcFieldOffset,pulClassSize) \
    ( (This)->lpVtbl -> GetClassLayout(This,classID,rFieldOffset,cFieldOffset,pcFieldOffset,pulClassSize) ) 

#define ICorProfilerInfo3_GetClassIDInfo2(This,classId,pModuleId,pTypeDefToken,pParentClassId,cNumTypeArgs,pcNumTypeArgs,typeArgs)  \
    ( (This)->lpVtbl -> GetClassIDInfo2(This,classId,pModuleId,pTypeDefToken,pParentClassId,cNumTypeArgs,pcNumTypeArgs,typeArgs) ) 

#define ICorProfilerInfo3_GetCodeInfo2(This,functionID,cCodeInfos,pcCodeInfos,codeInfos)    \
    ( (This)->lpVtbl -> GetCodeInfo2(This,functionID,cCodeInfos,pcCodeInfos,codeInfos) ) 

#define ICorProfilerInfo3_GetClassFromTokenAndTypeArgs(This,moduleID,typeDef,cTypeArgs,typeArgs,pClassID)   \
    ( (This)->lpVtbl -> GetClassFromTokenAndTypeArgs(This,moduleID,typeDef,cTypeArgs,typeArgs,pClassID) ) 

#define ICorProfilerInfo3_GetFunctionFromTokenAndTypeArgs(This,moduleID,funcDef,classId,cTypeArgs,typeArgs,pFunctionID) \
    ( (This)->lpVtbl -> GetFunctionFromTokenAndTypeArgs(This,moduleID,funcDef,classId,cTypeArgs,typeArgs,pFunctionID) ) 

#define ICorProfilerInfo3_EnumModuleFrozenObjects(This,moduleID,ppEnum) \
    ( (This)->lpVtbl -> EnumModuleFrozenObjects(This,moduleID,ppEnum) ) 

#define ICorProfilerInfo3_GetArrayObjectInfo(This,objectId,cDimensions,pDimensionSizes,pDimensionLowerBounds,ppData)    \
    ( (This)->lpVtbl -> GetArrayObjectInfo(This,objectId,cDimensions,pDimensionSizes,pDimensionLowerBounds,ppData) ) 

#define ICorProfilerInfo3_GetBoxClassLayout(This,classId,pBufferOffset) \
    ( (This)->lpVtbl -> GetBoxClassLayout(This,classId,pBufferOffset) ) 

#define ICorProfilerInfo3_GetThreadAppDomain(This,threadId,pAppDomainId)    \
    ( (This)->lpVtbl -> GetThreadAppDomain(This,threadId,pAppDomainId) ) 

#define ICorProfilerInfo3_GetRVAStaticAddress(This,classId,fieldToken,ppAddress)    \
    ( (This)->lpVtbl -> GetRVAStaticAddress(This,classId,fieldToken,ppAddress) ) 

#define ICorProfilerInfo3_GetAppDomainStaticAddress(This,classId,fieldToken,appDomainId,ppAddress)  \
    ( (This)->lpVtbl -> GetAppDomainStaticAddress(This,classId,fieldToken,appDomainId,ppAddress) ) 

#define ICorProfilerInfo3_GetThreadStaticAddress(This,classId,fieldToken,threadId,ppAddress)    \
    ( (This)->lpVtbl -> GetThreadStaticAddress(This,classId,fieldToken,threadId,ppAddress) ) 

#define ICorProfilerInfo3_GetContextStaticAddress(This,classId,fieldToken,contextId,ppAddress)  \
    ( (This)->lpVtbl -> GetContextStaticAddress(This,classId,fieldToken,contextId,ppAddress) ) 

#define ICorProfilerInfo3_GetStaticFieldInfo(This,classId,fieldToken,pFieldInfo)    \
    ( (This)->lpVtbl -> GetStaticFieldInfo(This,classId,fieldToken,pFieldInfo) ) 

#define ICorProfilerInfo3_GetGenerationBounds(This,cObjectRanges,pcObjectRanges,ranges) \
    ( (This)->lpVtbl -> GetGenerationBounds(This,cObjectRanges,pcObjectRanges,ranges) ) 

#define ICorProfilerInfo3_GetObjectGeneration(This,objectId,range)  \
    ( (This)->lpVtbl -> GetObjectGeneration(This,objectId,range) ) 

#define ICorProfilerInfo3_GetNotifiedExceptionClauseInfo(This,pinfo)    \
    ( (This)->lpVtbl -> GetNotifiedExceptionClauseInfo(This,pinfo) ) 


#define ICorProfilerInfo3_EnumJITedFunctions(This,ppEnum)   \
    ( (This)->lpVtbl -> EnumJITedFunctions(This,ppEnum) ) 

#define ICorProfilerInfo3_RequestProfilerDetach(This,dwExpectedCompletionMilliseconds)  \
    ( (This)->lpVtbl -> RequestProfilerDetach(This,dwExpectedCompletionMilliseconds) ) 

#define ICorProfilerInfo3_SetFunctionIDMapper2(This,pFunc,clientData)   \
    ( (This)->lpVtbl -> SetFunctionIDMapper2(This,pFunc,clientData) ) 

#define ICorProfilerInfo3_GetStringLayout2(This,pStringLengthOffset,pBufferOffset)  \
    ( (This)->lpVtbl -> GetStringLayout2(This,pStringLengthOffset,pBufferOffset) ) 

#define ICorProfilerInfo3_SetEnterLeaveFunctionHooks3(This,pFuncEnter3,pFuncLeave3,pFuncTailcall3)  \
    ( (This)->lpVtbl -> SetEnterLeaveFunctionHooks3(This,pFuncEnter3,pFuncLeave3,pFuncTailcall3) ) 

#define ICorProfilerInfo3_SetEnterLeaveFunctionHooks3WithInfo(This,pFuncEnter3WithInfo,pFuncLeave3WithInfo,pFuncTailcall3WithInfo)  \
    ( (This)->lpVtbl -> SetEnterLeaveFunctionHooks3WithInfo(This,pFuncEnter3WithInfo,pFuncLeave3WithInfo,pFuncTailcall3WithInfo) ) 

#define ICorProfilerInfo3_GetFunctionEnter3Info(This,functionId,eltInfo,pFrameInfo,pcbArgumentInfo,pArgumentInfo)   \
    ( (This)->lpVtbl -> GetFunctionEnter3Info(This,functionId,eltInfo,pFrameInfo,pcbArgumentInfo,pArgumentInfo) ) 

#define ICorProfilerInfo3_GetFunctionLeave3Info(This,functionId,eltInfo,pFrameInfo,pRetvalRange)    \
    ( (This)->lpVtbl -> GetFunctionLeave3Info(This,functionId,eltInfo,pFrameInfo,pRetvalRange) ) 

#define ICorProfilerInfo3_GetFunctionTailcall3Info(This,functionId,eltInfo,pFrameInfo)  \
    ( (This)->lpVtbl -> GetFunctionTailcall3Info(This,functionId,eltInfo,pFrameInfo) ) 

#define ICorProfilerInfo3_EnumModules(This,ppEnum)  \
    ( (This)->lpVtbl -> EnumModules(This,ppEnum) ) 

#define ICorProfilerInfo3_GetRuntimeInformation(This,pClrInstanceId,pRuntimeType,pMajorVersion,pMinorVersion,pBuildNumber,pQFEVersion,cchVersionString,pcchVersionString,szVersionString)   \
    ( (This)->lpVtbl -> GetRuntimeInformation(This,pClrInstanceId,pRuntimeType,pMajorVersion,pMinorVersion,pBuildNumber,pQFEVersion,cchVersionString,pcchVersionString,szVersionString) ) 

#define ICorProfilerInfo3_GetThreadStaticAddress2(This,classId,fieldToken,appDomainId,threadId,ppAddress)   \
    ( (This)->lpVtbl -> GetThreadStaticAddress2(This,classId,fieldToken,appDomainId,threadId,ppAddress) ) 

#define ICorProfilerInfo3_GetAppDomainsContainingModule(This,moduleId,cAppDomainIds,pcAppDomainIds,appDomainIds)    \
    ( (This)->lpVtbl -> GetAppDomainsContainingModule(This,moduleId,cAppDomainIds,pcAppDomainIds,appDomainIds) ) 

#define ICorProfilerInfo3_GetModuleInfo2(This,moduleId,ppBaseLoadAddress,cchName,pcchName,szName,pAssemblyId,pdwModuleFlags)    \
    ( (This)->lpVtbl -> GetModuleInfo2(This,moduleId,ppBaseLoadAddress,cchName,pcchName,szName,pAssemblyId,pdwModuleFlags) ) 

#endif /* COBJMACROS */


#endif  /* C style interface */




#endif  /* __ICorProfilerInfo3_INTERFACE_DEFINED__ */


#ifndef __ICorProfilerObjectEnum_INTERFACE_DEFINED__
#define __ICorProfilerObjectEnum_INTERFACE_DEFINED__

/* interface ICorProfilerObjectEnum */
/* [local][unique][uuid][object] */ 


EXTERN_C const IID IID_ICorProfilerObjectEnum;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("2C6269BD-2D13-4321-AE12-6686365FD6AF")
    ICorProfilerObjectEnum : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE Skip( 
            /* [in] */ ULONG celt) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE Reset( void) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE Clone( 
            /* [out] */ ICorProfilerObjectEnum **ppEnum) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetCount( 
            /* [out] */ ULONG *pcelt) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE Next( 
            /* [in] */ ULONG celt,
            /* [length_is][size_is][out] */ ObjectID objects[  ],
            /* [out] */ ULONG *pceltFetched) = 0;
        
    };
    
    
#else   /* C style interface */

    typedef struct ICorProfilerObjectEnumVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            ICorProfilerObjectEnum * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            ICorProfilerObjectEnum * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            ICorProfilerObjectEnum * This);
        
        HRESULT ( STDMETHODCALLTYPE *Skip )( 
            ICorProfilerObjectEnum * This,
            /* [in] */ ULONG celt);
        
        HRESULT ( STDMETHODCALLTYPE *Reset )( 
            ICorProfilerObjectEnum * This);
        
        HRESULT ( STDMETHODCALLTYPE *Clone )( 
            ICorProfilerObjectEnum * This,
            /* [out] */ ICorProfilerObjectEnum **ppEnum);
        
        HRESULT ( STDMETHODCALLTYPE *GetCount )( 
            ICorProfilerObjectEnum * This,
            /* [out] */ ULONG *pcelt);
        
        HRESULT ( STDMETHODCALLTYPE *Next )( 
            ICorProfilerObjectEnum * This,
            /* [in] */ ULONG celt,
            /* [length_is][size_is][out] */ ObjectID objects[  ],
            /* [out] */ ULONG *pceltFetched);
        
        END_INTERFACE
    } ICorProfilerObjectEnumVtbl;

    interface ICorProfilerObjectEnum
    {
        CONST_VTBL struct ICorProfilerObjectEnumVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define ICorProfilerObjectEnum_QueryInterface(This,riid,ppvObject)  \
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define ICorProfilerObjectEnum_AddRef(This) \
    ( (This)->lpVtbl -> AddRef(This) ) 

#define ICorProfilerObjectEnum_Release(This)    \
    ( (This)->lpVtbl -> Release(This) ) 


#define ICorProfilerObjectEnum_Skip(This,celt)  \
    ( (This)->lpVtbl -> Skip(This,celt) ) 

#define ICorProfilerObjectEnum_Reset(This)  \
    ( (This)->lpVtbl -> Reset(This) ) 

#define ICorProfilerObjectEnum_Clone(This,ppEnum)   \
    ( (This)->lpVtbl -> Clone(This,ppEnum) ) 

#define ICorProfilerObjectEnum_GetCount(This,pcelt) \
    ( (This)->lpVtbl -> GetCount(This,pcelt) ) 

#define ICorProfilerObjectEnum_Next(This,celt,objects,pceltFetched) \
    ( (This)->lpVtbl -> Next(This,celt,objects,pceltFetched) ) 

#endif /* COBJMACROS */


#endif  /* C style interface */




#endif  /* __ICorProfilerObjectEnum_INTERFACE_DEFINED__ */


#ifndef __ICorProfilerFunctionEnum_INTERFACE_DEFINED__
#define __ICorProfilerFunctionEnum_INTERFACE_DEFINED__

/* interface ICorProfilerFunctionEnum */
/* [local][unique][uuid][object] */ 


EXTERN_C const IID IID_ICorProfilerFunctionEnum;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("FF71301A-B994-429D-A10B-B345A65280EF")
    ICorProfilerFunctionEnum : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE Skip( 
            /* [in] */ ULONG celt) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE Reset( void) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE Clone( 
            /* [out] */ ICorProfilerFunctionEnum **ppEnum) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetCount( 
            /* [out] */ ULONG *pcelt) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE Next( 
            /* [in] */ ULONG celt,
            /* [length_is][size_is][out] */ COR_PRF_FUNCTION ids[  ],
            /* [out] */ ULONG *pceltFetched) = 0;
        
    };
    
    
#else   /* C style interface */

    typedef struct ICorProfilerFunctionEnumVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            ICorProfilerFunctionEnum * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            ICorProfilerFunctionEnum * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            ICorProfilerFunctionEnum * This);
        
        HRESULT ( STDMETHODCALLTYPE *Skip )( 
            ICorProfilerFunctionEnum * This,
            /* [in] */ ULONG celt);
        
        HRESULT ( STDMETHODCALLTYPE *Reset )( 
            ICorProfilerFunctionEnum * This);
        
        HRESULT ( STDMETHODCALLTYPE *Clone )( 
            ICorProfilerFunctionEnum * This,
            /* [out] */ ICorProfilerFunctionEnum **ppEnum);
        
        HRESULT ( STDMETHODCALLTYPE *GetCount )( 
            ICorProfilerFunctionEnum * This,
            /* [out] */ ULONG *pcelt);
        
        HRESULT ( STDMETHODCALLTYPE *Next )( 
            ICorProfilerFunctionEnum * This,
            /* [in] */ ULONG celt,
            /* [length_is][size_is][out] */ COR_PRF_FUNCTION ids[  ],
            /* [out] */ ULONG *pceltFetched);
        
        END_INTERFACE
    } ICorProfilerFunctionEnumVtbl;

    interface ICorProfilerFunctionEnum
    {
        CONST_VTBL struct ICorProfilerFunctionEnumVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define ICorProfilerFunctionEnum_QueryInterface(This,riid,ppvObject)    \
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define ICorProfilerFunctionEnum_AddRef(This)   \
    ( (This)->lpVtbl -> AddRef(This) ) 

#define ICorProfilerFunctionEnum_Release(This)  \
    ( (This)->lpVtbl -> Release(This) ) 


#define ICorProfilerFunctionEnum_Skip(This,celt)    \
    ( (This)->lpVtbl -> Skip(This,celt) ) 

#define ICorProfilerFunctionEnum_Reset(This)    \
    ( (This)->lpVtbl -> Reset(This) ) 

#define ICorProfilerFunctionEnum_Clone(This,ppEnum) \
    ( (This)->lpVtbl -> Clone(This,ppEnum) ) 

#define ICorProfilerFunctionEnum_GetCount(This,pcelt)   \
    ( (This)->lpVtbl -> GetCount(This,pcelt) ) 

#define ICorProfilerFunctionEnum_Next(This,celt,ids,pceltFetched)   \
    ( (This)->lpVtbl -> Next(This,celt,ids,pceltFetched) ) 

#endif /* COBJMACROS */


#endif  /* C style interface */




#endif  /* __ICorProfilerFunctionEnum_INTERFACE_DEFINED__ */


#ifndef __ICorProfilerModuleEnum_INTERFACE_DEFINED__
#define __ICorProfilerModuleEnum_INTERFACE_DEFINED__

/* interface ICorProfilerModuleEnum */
/* [local][unique][uuid][object] */ 


EXTERN_C const IID IID_ICorProfilerModuleEnum;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("b0266d75-2081-4493-af7f-028ba34db891")
    ICorProfilerModuleEnum : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE Skip( 
            /* [in] */ ULONG celt) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE Reset( void) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE Clone( 
            /* [out] */ ICorProfilerModuleEnum **ppEnum) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetCount( 
            /* [out] */ ULONG *pcelt) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE Next( 
            /* [in] */ ULONG celt,
            /* [length_is][size_is][out] */ ModuleID ids[  ],
            /* [out] */ ULONG *pceltFetched) = 0;
        
    };
    
    
#else   /* C style interface */

    typedef struct ICorProfilerModuleEnumVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            ICorProfilerModuleEnum * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            ICorProfilerModuleEnum * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            ICorProfilerModuleEnum * This);
        
        HRESULT ( STDMETHODCALLTYPE *Skip )( 
            ICorProfilerModuleEnum * This,
            /* [in] */ ULONG celt);
        
        HRESULT ( STDMETHODCALLTYPE *Reset )( 
            ICorProfilerModuleEnum * This);
        
        HRESULT ( STDMETHODCALLTYPE *Clone )( 
            ICorProfilerModuleEnum * This,
            /* [out] */ ICorProfilerModuleEnum **ppEnum);
        
        HRESULT ( STDMETHODCALLTYPE *GetCount )( 
            ICorProfilerModuleEnum * This,
            /* [out] */ ULONG *pcelt);
        
        HRESULT ( STDMETHODCALLTYPE *Next )( 
            ICorProfilerModuleEnum * This,
            /* [in] */ ULONG celt,
            /* [length_is][size_is][out] */ ModuleID ids[  ],
            /* [out] */ ULONG *pceltFetched);
        
        END_INTERFACE
    } ICorProfilerModuleEnumVtbl;

    interface ICorProfilerModuleEnum
    {
        CONST_VTBL struct ICorProfilerModuleEnumVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define ICorProfilerModuleEnum_QueryInterface(This,riid,ppvObject)  \
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define ICorProfilerModuleEnum_AddRef(This) \
    ( (This)->lpVtbl -> AddRef(This) ) 

#define ICorProfilerModuleEnum_Release(This)    \
    ( (This)->lpVtbl -> Release(This) ) 


#define ICorProfilerModuleEnum_Skip(This,celt)  \
    ( (This)->lpVtbl -> Skip(This,celt) ) 

#define ICorProfilerModuleEnum_Reset(This)  \
    ( (This)->lpVtbl -> Reset(This) ) 

#define ICorProfilerModuleEnum_Clone(This,ppEnum)   \
    ( (This)->lpVtbl -> Clone(This,ppEnum) ) 

#define ICorProfilerModuleEnum_GetCount(This,pcelt) \
    ( (This)->lpVtbl -> GetCount(This,pcelt) ) 

#define ICorProfilerModuleEnum_Next(This,celt,ids,pceltFetched) \
    ( (This)->lpVtbl -> Next(This,celt,ids,pceltFetched) ) 

#endif /* COBJMACROS */


#endif  /* C style interface */




#endif  /* __ICorProfilerModuleEnum_INTERFACE_DEFINED__ */


#ifndef __IMethodMalloc_INTERFACE_DEFINED__
#define __IMethodMalloc_INTERFACE_DEFINED__

/* interface IMethodMalloc */
/* [local][unique][uuid][object] */ 


EXTERN_C const IID IID_IMethodMalloc;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("A0EFB28B-6EE2-4d7b-B983-A75EF7BEEDB8")
    IMethodMalloc : public IUnknown
    {
    public:
        virtual PVOID STDMETHODCALLTYPE Alloc( 
            /* [in] */ ULONG cb) = 0;
        
    };
    
    
#else   /* C style interface */

    typedef struct IMethodMallocVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            IMethodMalloc * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            IMethodMalloc * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            IMethodMalloc * This);
        
        PVOID ( STDMETHODCALLTYPE *Alloc )( 
            IMethodMalloc * This,
            /* [in] */ ULONG cb);
        
        END_INTERFACE
    } IMethodMallocVtbl;

    interface IMethodMalloc
    {
        CONST_VTBL struct IMethodMallocVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define IMethodMalloc_QueryInterface(This,riid,ppvObject)   \
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define IMethodMalloc_AddRef(This)  \
    ( (This)->lpVtbl -> AddRef(This) ) 

#define IMethodMalloc_Release(This) \
    ( (This)->lpVtbl -> Release(This) ) 


#define IMethodMalloc_Alloc(This,cb)    \
    ( (This)->lpVtbl -> Alloc(This,cb) ) 

#endif /* COBJMACROS */


#endif  /* C style interface */




#endif  /* __IMethodMalloc_INTERFACE_DEFINED__ */


#ifndef __ICorProfilerFunctionControl_INTERFACE_DEFINED__
#define __ICorProfilerFunctionControl_INTERFACE_DEFINED__

/* interface ICorProfilerFunctionControl */
/* [local][unique][uuid][object] */ 


EXTERN_C const IID IID_ICorProfilerFunctionControl;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("F0963021-E1EA-4732-8581-E01B0BD3C0C6")
    ICorProfilerFunctionControl : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE SetCodegenFlags( 
            /* [in] */ DWORD flags) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE SetILFunctionBody( 
            /* [in] */ ULONG cbNewILMethodHeader,
            /* [size_is][in] */ LPCBYTE pbNewILMethodHeader) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE SetILInstrumentedCodeMap( 
            /* [in] */ ULONG cILMapEntries,
            /* [size_is][in] */ COR_IL_MAP rgILMapEntries[  ]) = 0;
        
    };
    
    
#else   /* C style interface */

    typedef struct ICorProfilerFunctionControlVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            ICorProfilerFunctionControl * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            ICorProfilerFunctionControl * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            ICorProfilerFunctionControl * This);
        
        HRESULT ( STDMETHODCALLTYPE *SetCodegenFlags )( 
            ICorProfilerFunctionControl * This,
            /* [in] */ DWORD flags);
        
        HRESULT ( STDMETHODCALLTYPE *SetILFunctionBody )( 
            ICorProfilerFunctionControl * This,
            /* [in] */ ULONG cbNewILMethodHeader,
            /* [size_is][in] */ LPCBYTE pbNewILMethodHeader);
        
        HRESULT ( STDMETHODCALLTYPE *SetILInstrumentedCodeMap )( 
            ICorProfilerFunctionControl * This,
            /* [in] */ ULONG cILMapEntries,
            /* [size_is][in] */ COR_IL_MAP rgILMapEntries[  ]);
        
        END_INTERFACE
    } ICorProfilerFunctionControlVtbl;

    interface ICorProfilerFunctionControl
    {
        CONST_VTBL struct ICorProfilerFunctionControlVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define ICorProfilerFunctionControl_QueryInterface(This,riid,ppvObject) \
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define ICorProfilerFunctionControl_AddRef(This)    \
    ( (This)->lpVtbl -> AddRef(This) ) 

#define ICorProfilerFunctionControl_Release(This)   \
    ( (This)->lpVtbl -> Release(This) ) 


#define ICorProfilerFunctionControl_SetCodegenFlags(This,flags) \
    ( (This)->lpVtbl -> SetCodegenFlags(This,flags) ) 

#define ICorProfilerFunctionControl_SetILFunctionBody(This,cbNewILMethodHeader,pbNewILMethodHeader) \
    ( (This)->lpVtbl -> SetILFunctionBody(This,cbNewILMethodHeader,pbNewILMethodHeader) ) 

#define ICorProfilerFunctionControl_SetILInstrumentedCodeMap(This,cILMapEntries,rgILMapEntries) \
    ( (This)->lpVtbl -> SetILInstrumentedCodeMap(This,cILMapEntries,rgILMapEntries) ) 

#endif /* COBJMACROS */


#endif  /* C style interface */




#endif  /* __ICorProfilerFunctionControl_INTERFACE_DEFINED__ */


#ifndef __ICorProfilerInfo4_INTERFACE_DEFINED__
#define __ICorProfilerInfo4_INTERFACE_DEFINED__

/* interface ICorProfilerInfo4 */
/* [local][unique][uuid][object] */ 


EXTERN_C const IID IID_ICorProfilerInfo4;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("0d8fdcaa-6257-47bf-b1bf-94dac88466ee")
    ICorProfilerInfo4 : public ICorProfilerInfo3
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE EnumThreads( 
            /* [out] */ ICorProfilerThreadEnum **ppEnum) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE InitializeCurrentThread( void) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE RequestReJIT( 
            /* [in] */ ULONG cFunctions,
            /* [size_is][in] */ ModuleID moduleIds[  ],
            /* [size_is][in] */ mdMethodDef methodIds[  ]) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE RequestRevert( 
            /* [in] */ ULONG cFunctions,
            /* [size_is][in] */ ModuleID moduleIds[  ],
            /* [size_is][in] */ mdMethodDef methodIds[  ],
            /* [size_is][out] */ HRESULT status[  ]) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetCodeInfo3( 
            /* [in] */ FunctionID functionID,
            /* [in] */ ReJITID reJitId,
            /* [in] */ ULONG32 cCodeInfos,
            /* [out] */ ULONG32 *pcCodeInfos,
            /* [length_is][size_is][out] */ COR_PRF_CODE_INFO codeInfos[  ]) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetFunctionFromIP2( 
            /* [in] */ LPCBYTE ip,
            /* [out] */ FunctionID *pFunctionId,
            /* [out] */ ReJITID *pReJitId) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetReJITIDs( 
            /* [in] */ FunctionID functionId,
            /* [in] */ ULONG cReJitIds,
            /* [out] */ ULONG *pcReJitIds,
            /* [length_is][size_is][out] */ ReJITID reJitIds[  ]) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetILToNativeMapping2( 
            /* [in] */ FunctionID functionId,
            /* [in] */ ReJITID reJitId,
            /* [in] */ ULONG32 cMap,
            /* [out] */ ULONG32 *pcMap,
            /* [length_is][size_is][out] */ COR_DEBUG_IL_TO_NATIVE_MAP map[  ]) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE EnumJITedFunctions2( 
            /* [out] */ ICorProfilerFunctionEnum **ppEnum) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetObjectSize2( 
            /* [in] */ ObjectID objectId,
            /* [out] */ SIZE_T *pcSize) = 0;
        
    };
    
    
#else   /* C style interface */

    typedef struct ICorProfilerInfo4Vtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            ICorProfilerInfo4 * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            ICorProfilerInfo4 * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            ICorProfilerInfo4 * This);
        
        HRESULT ( STDMETHODCALLTYPE *GetClassFromObject )( 
            ICorProfilerInfo4 * This,
            /* [in] */ ObjectID objectId,
            /* [out] */ ClassID *pClassId);
        
        HRESULT ( STDMETHODCALLTYPE *GetClassFromToken )( 
            ICorProfilerInfo4 * This,
            /* [in] */ ModuleID moduleId,
            /* [in] */ mdTypeDef typeDef,
            /* [out] */ ClassID *pClassId);
        
        HRESULT ( STDMETHODCALLTYPE *GetCodeInfo )( 
            ICorProfilerInfo4 * This,
            /* [in] */ FunctionID functionId,
            /* [out] */ LPCBYTE *pStart,
            /* [out] */ ULONG *pcSize);
        
        HRESULT ( STDMETHODCALLTYPE *GetEventMask )( 
            ICorProfilerInfo4 * This,
            /* [out] */ DWORD *pdwEvents);
        
        HRESULT ( STDMETHODCALLTYPE *GetFunctionFromIP )( 
            ICorProfilerInfo4 * This,
            /* [in] */ LPCBYTE ip,
            /* [out] */ FunctionID *pFunctionId);
        
        HRESULT ( STDMETHODCALLTYPE *GetFunctionFromToken )( 
            ICorProfilerInfo4 * This,
            /* [in] */ ModuleID moduleId,
            /* [in] */ mdToken token,
            /* [out] */ FunctionID *pFunctionId);
        
        HRESULT ( STDMETHODCALLTYPE *GetHandleFromThread )( 
            ICorProfilerInfo4 * This,
            /* [in] */ ThreadID threadId,
            /* [out] */ HANDLE *phThread);
        
        HRESULT ( STDMETHODCALLTYPE *GetObjectSize )( 
            ICorProfilerInfo4 * This,
            /* [in] */ ObjectID objectId,
            /* [out] */ ULONG *pcSize);
        
        HRESULT ( STDMETHODCALLTYPE *IsArrayClass )( 
            ICorProfilerInfo4 * This,
            /* [in] */ ClassID classId,
            /* [out] */ CorElementType *pBaseElemType,
            /* [out] */ ClassID *pBaseClassId,
            /* [out] */ ULONG *pcRank);
        
        HRESULT ( STDMETHODCALLTYPE *GetThreadInfo )( 
            ICorProfilerInfo4 * This,
            /* [in] */ ThreadID threadId,
            /* [out] */ DWORD *pdwWin32ThreadId);
        
        HRESULT ( STDMETHODCALLTYPE *GetCurrentThreadID )( 
            ICorProfilerInfo4 * This,
            /* [out] */ ThreadID *pThreadId);
        
        HRESULT ( STDMETHODCALLTYPE *GetClassIDInfo )( 
            ICorProfilerInfo4 * This,
            /* [in] */ ClassID classId,
            /* [out] */ ModuleID *pModuleId,
            /* [out] */ mdTypeDef *pTypeDefToken);
        
        HRESULT ( STDMETHODCALLTYPE *GetFunctionInfo )( 
            ICorProfilerInfo4 * This,
            /* [in] */ FunctionID functionId,
            /* [out] */ ClassID *pClassId,
            /* [out] */ ModuleID *pModuleId,
            /* [out] */ mdToken *pToken);
        
        HRESULT ( STDMETHODCALLTYPE *SetEventMask )( 
            ICorProfilerInfo4 * This,
            /* [in] */ DWORD dwEvents);
        
        HRESULT ( STDMETHODCALLTYPE *SetEnterLeaveFunctionHooks )( 
            ICorProfilerInfo4 * This,
            /* [in] */ FunctionEnter *pFuncEnter,
            /* [in] */ FunctionLeave *pFuncLeave,
            /* [in] */ FunctionTailcall *pFuncTailcall);
        
        HRESULT ( STDMETHODCALLTYPE *SetFunctionIDMapper )( 
            ICorProfilerInfo4 * This,
            /* [in] */ FunctionIDMapper *pFunc);
        
        HRESULT ( STDMETHODCALLTYPE *GetTokenAndMetaDataFromFunction )( 
            ICorProfilerInfo4 * This,
            /* [in] */ FunctionID functionId,
            /* [in] */ REFIID riid,
            /* [out] */ IUnknown **ppImport,
            /* [out] */ mdToken *pToken);
        
        HRESULT ( STDMETHODCALLTYPE *GetModuleInfo )( 
            ICorProfilerInfo4 * This,
            /* [in] */ ModuleID moduleId,
            /* [out] */ LPCBYTE *ppBaseLoadAddress,
            /* [in] */ ULONG cchName,
            /* [out] */ ULONG *pcchName,
            /* [annotation][out] */ 
            _Out_writes_to_(cchName, *pcchName)  WCHAR szName[  ],
            /* [out] */ AssemblyID *pAssemblyId);
        
        HRESULT ( STDMETHODCALLTYPE *GetModuleMetaData )( 
            ICorProfilerInfo4 * This,
            /* [in] */ ModuleID moduleId,
            /* [in] */ DWORD dwOpenFlags,
            /* [in] */ REFIID riid,
            /* [out] */ IUnknown **ppOut);
        
        HRESULT ( STDMETHODCALLTYPE *GetILFunctionBody )( 
            ICorProfilerInfo4 * This,
            /* [in] */ ModuleID moduleId,
            /* [in] */ mdMethodDef methodId,
            /* [out] */ LPCBYTE *ppMethodHeader,
            /* [out] */ ULONG *pcbMethodSize);
        
        HRESULT ( STDMETHODCALLTYPE *GetILFunctionBodyAllocator )( 
            ICorProfilerInfo4 * This,
            /* [in] */ ModuleID moduleId,
            /* [out] */ IMethodMalloc **ppMalloc);
        
        HRESULT ( STDMETHODCALLTYPE *SetILFunctionBody )( 
            ICorProfilerInfo4 * This,
            /* [in] */ ModuleID moduleId,
            /* [in] */ mdMethodDef methodid,
            /* [in] */ LPCBYTE pbNewILMethodHeader);
        
        HRESULT ( STDMETHODCALLTYPE *GetAppDomainInfo )( 
            ICorProfilerInfo4 * This,
            /* [in] */ AppDomainID appDomainId,
            /* [in] */ ULONG cchName,
            /* [out] */ ULONG *pcchName,
            /* [annotation][out] */ 
            _Out_writes_to_(cchName, *pcchName)  WCHAR szName[  ],
            /* [out] */ ProcessID *pProcessId);
        
        HRESULT ( STDMETHODCALLTYPE *GetAssemblyInfo )( 
            ICorProfilerInfo4 * This,
            /* [in] */ AssemblyID assemblyId,
            /* [in] */ ULONG cchName,
            /* [out] */ ULONG *pcchName,
            /* [annotation][out] */ 
            _Out_writes_to_(cchName, *pcchName)  WCHAR szName[  ],
            /* [out] */ AppDomainID *pAppDomainId,
            /* [out] */ ModuleID *pModuleId);
        
        HRESULT ( STDMETHODCALLTYPE *SetFunctionReJIT )( 
            ICorProfilerInfo4 * This,
            /* [in] */ FunctionID functionId);
        
        HRESULT ( STDMETHODCALLTYPE *ForceGC )( 
            ICorProfilerInfo4 * This);
        
        HRESULT ( STDMETHODCALLTYPE *SetILInstrumentedCodeMap )( 
            ICorProfilerInfo4 * This,
            /* [in] */ FunctionID functionId,
            /* [in] */ BOOL fStartJit,
            /* [in] */ ULONG cILMapEntries,
            /* [size_is][in] */ COR_IL_MAP rgILMapEntries[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *GetInprocInspectionInterface )( 
            ICorProfilerInfo4 * This,
            /* [out] */ IUnknown **ppicd);
        
        HRESULT ( STDMETHODCALLTYPE *GetInprocInspectionIThisThread )( 
            ICorProfilerInfo4 * This,
            /* [out] */ IUnknown **ppicd);
        
        HRESULT ( STDMETHODCALLTYPE *GetThreadContext )( 
            ICorProfilerInfo4 * This,
            /* [in] */ ThreadID threadId,
            /* [out] */ ContextID *pContextId);
        
        HRESULT ( STDMETHODCALLTYPE *BeginInprocDebugging )( 
            ICorProfilerInfo4 * This,
            /* [in] */ BOOL fThisThreadOnly,
            /* [out] */ DWORD *pdwProfilerContext);
        
        HRESULT ( STDMETHODCALLTYPE *EndInprocDebugging )( 
            ICorProfilerInfo4 * This,
            /* [in] */ DWORD dwProfilerContext);
        
        HRESULT ( STDMETHODCALLTYPE *GetILToNativeMapping )( 
            ICorProfilerInfo4 * This,
            /* [in] */ FunctionID functionId,
            /* [in] */ ULONG32 cMap,
            /* [out] */ ULONG32 *pcMap,
            /* [length_is][size_is][out] */ COR_DEBUG_IL_TO_NATIVE_MAP map[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *DoStackSnapshot )( 
            ICorProfilerInfo4 * This,
            /* [in] */ ThreadID thread,
            /* [in] */ StackSnapshotCallback *callback,
            /* [in] */ ULONG32 infoFlags,
            /* [in] */ void *clientData,
            /* [size_is][in] */ BYTE context[  ],
            /* [in] */ ULONG32 contextSize);
        
        HRESULT ( STDMETHODCALLTYPE *SetEnterLeaveFunctionHooks2 )( 
            ICorProfilerInfo4 * This,
            /* [in] */ FunctionEnter2 *pFuncEnter,
            /* [in] */ FunctionLeave2 *pFuncLeave,
            /* [in] */ FunctionTailcall2 *pFuncTailcall);
        
        HRESULT ( STDMETHODCALLTYPE *GetFunctionInfo2 )( 
            ICorProfilerInfo4 * This,
            /* [in] */ FunctionID funcId,
            /* [in] */ COR_PRF_FRAME_INFO frameInfo,
            /* [out] */ ClassID *pClassId,
            /* [out] */ ModuleID *pModuleId,
            /* [out] */ mdToken *pToken,
            /* [in] */ ULONG32 cTypeArgs,
            /* [out] */ ULONG32 *pcTypeArgs,
            /* [out] */ ClassID typeArgs[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *GetStringLayout )( 
            ICorProfilerInfo4 * This,
            /* [out] */ ULONG *pBufferLengthOffset,
            /* [out] */ ULONG *pStringLengthOffset,
            /* [out] */ ULONG *pBufferOffset);
        
        HRESULT ( STDMETHODCALLTYPE *GetClassLayout )( 
            ICorProfilerInfo4 * This,
            /* [in] */ ClassID classID,
            /* [out][in] */ COR_FIELD_OFFSET rFieldOffset[  ],
            /* [in] */ ULONG cFieldOffset,
            /* [out] */ ULONG *pcFieldOffset,
            /* [out] */ ULONG *pulClassSize);
        
        HRESULT ( STDMETHODCALLTYPE *GetClassIDInfo2 )( 
            ICorProfilerInfo4 * This,
            /* [in] */ ClassID classId,
            /* [out] */ ModuleID *pModuleId,
            /* [out] */ mdTypeDef *pTypeDefToken,
            /* [out] */ ClassID *pParentClassId,
            /* [in] */ ULONG32 cNumTypeArgs,
            /* [out] */ ULONG32 *pcNumTypeArgs,
            /* [out] */ ClassID typeArgs[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *GetCodeInfo2 )( 
            ICorProfilerInfo4 * This,
            /* [in] */ FunctionID functionID,
            /* [in] */ ULONG32 cCodeInfos,
            /* [out] */ ULONG32 *pcCodeInfos,
            /* [length_is][size_is][out] */ COR_PRF_CODE_INFO codeInfos[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *GetClassFromTokenAndTypeArgs )( 
            ICorProfilerInfo4 * This,
            /* [in] */ ModuleID moduleID,
            /* [in] */ mdTypeDef typeDef,
            /* [in] */ ULONG32 cTypeArgs,
            /* [size_is][in] */ ClassID typeArgs[  ],
            /* [out] */ ClassID *pClassID);
        
        HRESULT ( STDMETHODCALLTYPE *GetFunctionFromTokenAndTypeArgs )( 
            ICorProfilerInfo4 * This,
            /* [in] */ ModuleID moduleID,
            /* [in] */ mdMethodDef funcDef,
            /* [in] */ ClassID classId,
            /* [in] */ ULONG32 cTypeArgs,
            /* [size_is][in] */ ClassID typeArgs[  ],
            /* [out] */ FunctionID *pFunctionID);
        
        HRESULT ( STDMETHODCALLTYPE *EnumModuleFrozenObjects )( 
            ICorProfilerInfo4 * This,
            /* [in] */ ModuleID moduleID,
            /* [out] */ ICorProfilerObjectEnum **ppEnum);
        
        HRESULT ( STDMETHODCALLTYPE *GetArrayObjectInfo )( 
            ICorProfilerInfo4 * This,
            /* [in] */ ObjectID objectId,
            /* [in] */ ULONG32 cDimensions,
            /* [size_is][out] */ ULONG32 pDimensionSizes[  ],
            /* [size_is][out] */ int pDimensionLowerBounds[  ],
            /* [out] */ BYTE **ppData);
        
        HRESULT ( STDMETHODCALLTYPE *GetBoxClassLayout )( 
            ICorProfilerInfo4 * This,
            /* [in] */ ClassID classId,
            /* [out] */ ULONG32 *pBufferOffset);
        
        HRESULT ( STDMETHODCALLTYPE *GetThreadAppDomain )( 
            ICorProfilerInfo4 * This,
            /* [in] */ ThreadID threadId,
            /* [out] */ AppDomainID *pAppDomainId);
        
        HRESULT ( STDMETHODCALLTYPE *GetRVAStaticAddress )( 
            ICorProfilerInfo4 * This,
            /* [in] */ ClassID classId,
            /* [in] */ mdFieldDef fieldToken,
            /* [out] */ void **ppAddress);
        
        HRESULT ( STDMETHODCALLTYPE *GetAppDomainStaticAddress )( 
            ICorProfilerInfo4 * This,
            /* [in] */ ClassID classId,
            /* [in] */ mdFieldDef fieldToken,
            /* [in] */ AppDomainID appDomainId,
            /* [out] */ void **ppAddress);
        
        HRESULT ( STDMETHODCALLTYPE *GetThreadStaticAddress )( 
            ICorProfilerInfo4 * This,
            /* [in] */ ClassID classId,
            /* [in] */ mdFieldDef fieldToken,
            /* [in] */ ThreadID threadId,
            /* [out] */ void **ppAddress);
        
        HRESULT ( STDMETHODCALLTYPE *GetContextStaticAddress )( 
            ICorProfilerInfo4 * This,
            /* [in] */ ClassID classId,
            /* [in] */ mdFieldDef fieldToken,
            /* [in] */ ContextID contextId,
            /* [out] */ void **ppAddress);
        
        HRESULT ( STDMETHODCALLTYPE *GetStaticFieldInfo )( 
            ICorProfilerInfo4 * This,
            /* [in] */ ClassID classId,
            /* [in] */ mdFieldDef fieldToken,
            /* [out] */ COR_PRF_STATIC_TYPE *pFieldInfo);
        
        HRESULT ( STDMETHODCALLTYPE *GetGenerationBounds )( 
            ICorProfilerInfo4 * This,
            /* [in] */ ULONG cObjectRanges,
            /* [out] */ ULONG *pcObjectRanges,
            /* [length_is][size_is][out] */ COR_PRF_GC_GENERATION_RANGE ranges[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *GetObjectGeneration )( 
            ICorProfilerInfo4 * This,
            /* [in] */ ObjectID objectId,
            /* [out] */ COR_PRF_GC_GENERATION_RANGE *range);
        
        HRESULT ( STDMETHODCALLTYPE *GetNotifiedExceptionClauseInfo )( 
            ICorProfilerInfo4 * This,
            /* [out] */ COR_PRF_EX_CLAUSE_INFO *pinfo);
        
        HRESULT ( STDMETHODCALLTYPE *EnumJITedFunctions )( 
            ICorProfilerInfo4 * This,
            /* [out] */ ICorProfilerFunctionEnum **ppEnum);
        
        HRESULT ( STDMETHODCALLTYPE *RequestProfilerDetach )( 
            ICorProfilerInfo4 * This,
            /* [in] */ DWORD dwExpectedCompletionMilliseconds);
        
        HRESULT ( STDMETHODCALLTYPE *SetFunctionIDMapper2 )( 
            ICorProfilerInfo4 * This,
            /* [in] */ FunctionIDMapper2 *pFunc,
            /* [in] */ void *clientData);
        
        HRESULT ( STDMETHODCALLTYPE *GetStringLayout2 )( 
            ICorProfilerInfo4 * This,
            /* [out] */ ULONG *pStringLengthOffset,
            /* [out] */ ULONG *pBufferOffset);
        
        HRESULT ( STDMETHODCALLTYPE *SetEnterLeaveFunctionHooks3 )( 
            ICorProfilerInfo4 * This,
            /* [in] */ FunctionEnter3 *pFuncEnter3,
            /* [in] */ FunctionLeave3 *pFuncLeave3,
            /* [in] */ FunctionTailcall3 *pFuncTailcall3);
        
        HRESULT ( STDMETHODCALLTYPE *SetEnterLeaveFunctionHooks3WithInfo )( 
            ICorProfilerInfo4 * This,
            /* [in] */ FunctionEnter3WithInfo *pFuncEnter3WithInfo,
            /* [in] */ FunctionLeave3WithInfo *pFuncLeave3WithInfo,
            /* [in] */ FunctionTailcall3WithInfo *pFuncTailcall3WithInfo);
        
        HRESULT ( STDMETHODCALLTYPE *GetFunctionEnter3Info )( 
            ICorProfilerInfo4 * This,
            /* [in] */ FunctionID functionId,
            /* [in] */ COR_PRF_ELT_INFO eltInfo,
            /* [out] */ COR_PRF_FRAME_INFO *pFrameInfo,
            /* [out][in] */ ULONG *pcbArgumentInfo,
            /* [size_is][out] */ COR_PRF_FUNCTION_ARGUMENT_INFO *pArgumentInfo);
        
        HRESULT ( STDMETHODCALLTYPE *GetFunctionLeave3Info )( 
            ICorProfilerInfo4 * This,
            /* [in] */ FunctionID functionId,
            /* [in] */ COR_PRF_ELT_INFO eltInfo,
            /* [out] */ COR_PRF_FRAME_INFO *pFrameInfo,
            /* [out] */ COR_PRF_FUNCTION_ARGUMENT_RANGE *pRetvalRange);
        
        HRESULT ( STDMETHODCALLTYPE *GetFunctionTailcall3Info )( 
            ICorProfilerInfo4 * This,
            /* [in] */ FunctionID functionId,
            /* [in] */ COR_PRF_ELT_INFO eltInfo,
            /* [out] */ COR_PRF_FRAME_INFO *pFrameInfo);
        
        HRESULT ( STDMETHODCALLTYPE *EnumModules )( 
            ICorProfilerInfo4 * This,
            /* [out] */ ICorProfilerModuleEnum **ppEnum);
        
        HRESULT ( STDMETHODCALLTYPE *GetRuntimeInformation )( 
            ICorProfilerInfo4 * This,
            /* [out] */ USHORT *pClrInstanceId,
            /* [out] */ COR_PRF_RUNTIME_TYPE *pRuntimeType,
            /* [out] */ USHORT *pMajorVersion,
            /* [out] */ USHORT *pMinorVersion,
            /* [out] */ USHORT *pBuildNumber,
            /* [out] */ USHORT *pQFEVersion,
            /* [in] */ ULONG cchVersionString,
            /* [out] */ ULONG *pcchVersionString,
            /* [annotation][out] */ 
            _Out_writes_to_(cchVersionString, *pcchVersionString)  WCHAR szVersionString[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *GetThreadStaticAddress2 )( 
            ICorProfilerInfo4 * This,
            /* [in] */ ClassID classId,
            /* [in] */ mdFieldDef fieldToken,
            /* [in] */ AppDomainID appDomainId,
            /* [in] */ ThreadID threadId,
            /* [out] */ void **ppAddress);
        
        HRESULT ( STDMETHODCALLTYPE *GetAppDomainsContainingModule )( 
            ICorProfilerInfo4 * This,
            /* [in] */ ModuleID moduleId,
            /* [in] */ ULONG32 cAppDomainIds,
            /* [out] */ ULONG32 *pcAppDomainIds,
            /* [length_is][size_is][out] */ AppDomainID appDomainIds[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *GetModuleInfo2 )( 
            ICorProfilerInfo4 * This,
            /* [in] */ ModuleID moduleId,
            /* [out] */ LPCBYTE *ppBaseLoadAddress,
            /* [in] */ ULONG cchName,
            /* [out] */ ULONG *pcchName,
            /* [annotation][out] */ 
            _Out_writes_to_(cchName, *pcchName)  WCHAR szName[  ],
            /* [out] */ AssemblyID *pAssemblyId,
            /* [out] */ DWORD *pdwModuleFlags);
        
        HRESULT ( STDMETHODCALLTYPE *EnumThreads )( 
            ICorProfilerInfo4 * This,
            /* [out] */ ICorProfilerThreadEnum **ppEnum);
        
        HRESULT ( STDMETHODCALLTYPE *InitializeCurrentThread )( 
            ICorProfilerInfo4 * This);
        
        HRESULT ( STDMETHODCALLTYPE *RequestReJIT )( 
            ICorProfilerInfo4 * This,
            /* [in] */ ULONG cFunctions,
            /* [size_is][in] */ ModuleID moduleIds[  ],
            /* [size_is][in] */ mdMethodDef methodIds[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *RequestRevert )( 
            ICorProfilerInfo4 * This,
            /* [in] */ ULONG cFunctions,
            /* [size_is][in] */ ModuleID moduleIds[  ],
            /* [size_is][in] */ mdMethodDef methodIds[  ],
            /* [size_is][out] */ HRESULT status[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *GetCodeInfo3 )( 
            ICorProfilerInfo4 * This,
            /* [in] */ FunctionID functionID,
            /* [in] */ ReJITID reJitId,
            /* [in] */ ULONG32 cCodeInfos,
            /* [out] */ ULONG32 *pcCodeInfos,
            /* [length_is][size_is][out] */ COR_PRF_CODE_INFO codeInfos[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *GetFunctionFromIP2 )( 
            ICorProfilerInfo4 * This,
            /* [in] */ LPCBYTE ip,
            /* [out] */ FunctionID *pFunctionId,
            /* [out] */ ReJITID *pReJitId);
        
        HRESULT ( STDMETHODCALLTYPE *GetReJITIDs )( 
            ICorProfilerInfo4 * This,
            /* [in] */ FunctionID functionId,
            /* [in] */ ULONG cReJitIds,
            /* [out] */ ULONG *pcReJitIds,
            /* [length_is][size_is][out] */ ReJITID reJitIds[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *GetILToNativeMapping2 )( 
            ICorProfilerInfo4 * This,
            /* [in] */ FunctionID functionId,
            /* [in] */ ReJITID reJitId,
            /* [in] */ ULONG32 cMap,
            /* [out] */ ULONG32 *pcMap,
            /* [length_is][size_is][out] */ COR_DEBUG_IL_TO_NATIVE_MAP map[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *EnumJITedFunctions2 )( 
            ICorProfilerInfo4 * This,
            /* [out] */ ICorProfilerFunctionEnum **ppEnum);
        
        HRESULT ( STDMETHODCALLTYPE *GetObjectSize2 )( 
            ICorProfilerInfo4 * This,
            /* [in] */ ObjectID objectId,
            /* [out] */ SIZE_T *pcSize);
        
        END_INTERFACE
    } ICorProfilerInfo4Vtbl;

    interface ICorProfilerInfo4
    {
        CONST_VTBL struct ICorProfilerInfo4Vtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define ICorProfilerInfo4_QueryInterface(This,riid,ppvObject)   \
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define ICorProfilerInfo4_AddRef(This)  \
    ( (This)->lpVtbl -> AddRef(This) ) 

#define ICorProfilerInfo4_Release(This) \
    ( (This)->lpVtbl -> Release(This) ) 


#define ICorProfilerInfo4_GetClassFromObject(This,objectId,pClassId)    \
    ( (This)->lpVtbl -> GetClassFromObject(This,objectId,pClassId) ) 

#define ICorProfilerInfo4_GetClassFromToken(This,moduleId,typeDef,pClassId) \
    ( (This)->lpVtbl -> GetClassFromToken(This,moduleId,typeDef,pClassId) ) 

#define ICorProfilerInfo4_GetCodeInfo(This,functionId,pStart,pcSize)    \
    ( (This)->lpVtbl -> GetCodeInfo(This,functionId,pStart,pcSize) ) 

#define ICorProfilerInfo4_GetEventMask(This,pdwEvents)  \
    ( (This)->lpVtbl -> GetEventMask(This,pdwEvents) ) 

#define ICorProfilerInfo4_GetFunctionFromIP(This,ip,pFunctionId)    \
    ( (This)->lpVtbl -> GetFunctionFromIP(This,ip,pFunctionId) ) 

#define ICorProfilerInfo4_GetFunctionFromToken(This,moduleId,token,pFunctionId) \
    ( (This)->lpVtbl -> GetFunctionFromToken(This,moduleId,token,pFunctionId) ) 

#define ICorProfilerInfo4_GetHandleFromThread(This,threadId,phThread)   \
    ( (This)->lpVtbl -> GetHandleFromThread(This,threadId,phThread) ) 

#define ICorProfilerInfo4_GetObjectSize(This,objectId,pcSize)   \
    ( (This)->lpVtbl -> GetObjectSize(This,objectId,pcSize) ) 

#define ICorProfilerInfo4_IsArrayClass(This,classId,pBaseElemType,pBaseClassId,pcRank)  \
    ( (This)->lpVtbl -> IsArrayClass(This,classId,pBaseElemType,pBaseClassId,pcRank) ) 

#define ICorProfilerInfo4_GetThreadInfo(This,threadId,pdwWin32ThreadId) \
    ( (This)->lpVtbl -> GetThreadInfo(This,threadId,pdwWin32ThreadId) ) 

#define ICorProfilerInfo4_GetCurrentThreadID(This,pThreadId)    \
    ( (This)->lpVtbl -> GetCurrentThreadID(This,pThreadId) ) 

#define ICorProfilerInfo4_GetClassIDInfo(This,classId,pModuleId,pTypeDefToken)  \
    ( (This)->lpVtbl -> GetClassIDInfo(This,classId,pModuleId,pTypeDefToken) ) 

#define ICorProfilerInfo4_GetFunctionInfo(This,functionId,pClassId,pModuleId,pToken)    \
    ( (This)->lpVtbl -> GetFunctionInfo(This,functionId,pClassId,pModuleId,pToken) ) 

#define ICorProfilerInfo4_SetEventMask(This,dwEvents)   \
    ( (This)->lpVtbl -> SetEventMask(This,dwEvents) ) 

#define ICorProfilerInfo4_SetEnterLeaveFunctionHooks(This,pFuncEnter,pFuncLeave,pFuncTailcall)  \
    ( (This)->lpVtbl -> SetEnterLeaveFunctionHooks(This,pFuncEnter,pFuncLeave,pFuncTailcall) ) 

#define ICorProfilerInfo4_SetFunctionIDMapper(This,pFunc)   \
    ( (This)->lpVtbl -> SetFunctionIDMapper(This,pFunc) ) 

#define ICorProfilerInfo4_GetTokenAndMetaDataFromFunction(This,functionId,riid,ppImport,pToken) \
    ( (This)->lpVtbl -> GetTokenAndMetaDataFromFunction(This,functionId,riid,ppImport,pToken) ) 

#define ICorProfilerInfo4_GetModuleInfo(This,moduleId,ppBaseLoadAddress,cchName,pcchName,szName,pAssemblyId)    \
    ( (This)->lpVtbl -> GetModuleInfo(This,moduleId,ppBaseLoadAddress,cchName,pcchName,szName,pAssemblyId) ) 

#define ICorProfilerInfo4_GetModuleMetaData(This,moduleId,dwOpenFlags,riid,ppOut)   \
    ( (This)->lpVtbl -> GetModuleMetaData(This,moduleId,dwOpenFlags,riid,ppOut) ) 

#define ICorProfilerInfo4_GetILFunctionBody(This,moduleId,methodId,ppMethodHeader,pcbMethodSize)    \
    ( (This)->lpVtbl -> GetILFunctionBody(This,moduleId,methodId,ppMethodHeader,pcbMethodSize) ) 

#define ICorProfilerInfo4_GetILFunctionBodyAllocator(This,moduleId,ppMalloc)    \
    ( (This)->lpVtbl -> GetILFunctionBodyAllocator(This,moduleId,ppMalloc) ) 

#define ICorProfilerInfo4_SetILFunctionBody(This,moduleId,methodid,pbNewILMethodHeader) \
    ( (This)->lpVtbl -> SetILFunctionBody(This,moduleId,methodid,pbNewILMethodHeader) ) 

#define ICorProfilerInfo4_GetAppDomainInfo(This,appDomainId,cchName,pcchName,szName,pProcessId) \
    ( (This)->lpVtbl -> GetAppDomainInfo(This,appDomainId,cchName,pcchName,szName,pProcessId) ) 

#define ICorProfilerInfo4_GetAssemblyInfo(This,assemblyId,cchName,pcchName,szName,pAppDomainId,pModuleId)   \
    ( (This)->lpVtbl -> GetAssemblyInfo(This,assemblyId,cchName,pcchName,szName,pAppDomainId,pModuleId) ) 

#define ICorProfilerInfo4_SetFunctionReJIT(This,functionId) \
    ( (This)->lpVtbl -> SetFunctionReJIT(This,functionId) ) 

#define ICorProfilerInfo4_ForceGC(This) \
    ( (This)->lpVtbl -> ForceGC(This) ) 

#define ICorProfilerInfo4_SetILInstrumentedCodeMap(This,functionId,fStartJit,cILMapEntries,rgILMapEntries)  \
    ( (This)->lpVtbl -> SetILInstrumentedCodeMap(This,functionId,fStartJit,cILMapEntries,rgILMapEntries) ) 

#define ICorProfilerInfo4_GetInprocInspectionInterface(This,ppicd)  \
    ( (This)->lpVtbl -> GetInprocInspectionInterface(This,ppicd) ) 

#define ICorProfilerInfo4_GetInprocInspectionIThisThread(This,ppicd)    \
    ( (This)->lpVtbl -> GetInprocInspectionIThisThread(This,ppicd) ) 

#define ICorProfilerInfo4_GetThreadContext(This,threadId,pContextId)    \
    ( (This)->lpVtbl -> GetThreadContext(This,threadId,pContextId) ) 

#define ICorProfilerInfo4_BeginInprocDebugging(This,fThisThreadOnly,pdwProfilerContext) \
    ( (This)->lpVtbl -> BeginInprocDebugging(This,fThisThreadOnly,pdwProfilerContext) ) 

#define ICorProfilerInfo4_EndInprocDebugging(This,dwProfilerContext)    \
    ( (This)->lpVtbl -> EndInprocDebugging(This,dwProfilerContext) ) 

#define ICorProfilerInfo4_GetILToNativeMapping(This,functionId,cMap,pcMap,map)  \
    ( (This)->lpVtbl -> GetILToNativeMapping(This,functionId,cMap,pcMap,map) ) 


#define ICorProfilerInfo4_DoStackSnapshot(This,thread,callback,infoFlags,clientData,context,contextSize)    \
    ( (This)->lpVtbl -> DoStackSnapshot(This,thread,callback,infoFlags,clientData,context,contextSize) ) 

#define ICorProfilerInfo4_SetEnterLeaveFunctionHooks2(This,pFuncEnter,pFuncLeave,pFuncTailcall) \
    ( (This)->lpVtbl -> SetEnterLeaveFunctionHooks2(This,pFuncEnter,pFuncLeave,pFuncTailcall) ) 

#define ICorProfilerInfo4_GetFunctionInfo2(This,funcId,frameInfo,pClassId,pModuleId,pToken,cTypeArgs,pcTypeArgs,typeArgs)   \
    ( (This)->lpVtbl -> GetFunctionInfo2(This,funcId,frameInfo,pClassId,pModuleId,pToken,cTypeArgs,pcTypeArgs,typeArgs) ) 

#define ICorProfilerInfo4_GetStringLayout(This,pBufferLengthOffset,pStringLengthOffset,pBufferOffset)   \
    ( (This)->lpVtbl -> GetStringLayout(This,pBufferLengthOffset,pStringLengthOffset,pBufferOffset) ) 

#define ICorProfilerInfo4_GetClassLayout(This,classID,rFieldOffset,cFieldOffset,pcFieldOffset,pulClassSize) \
    ( (This)->lpVtbl -> GetClassLayout(This,classID,rFieldOffset,cFieldOffset,pcFieldOffset,pulClassSize) ) 

#define ICorProfilerInfo4_GetClassIDInfo2(This,classId,pModuleId,pTypeDefToken,pParentClassId,cNumTypeArgs,pcNumTypeArgs,typeArgs)  \
    ( (This)->lpVtbl -> GetClassIDInfo2(This,classId,pModuleId,pTypeDefToken,pParentClassId,cNumTypeArgs,pcNumTypeArgs,typeArgs) ) 

#define ICorProfilerInfo4_GetCodeInfo2(This,functionID,cCodeInfos,pcCodeInfos,codeInfos)    \
    ( (This)->lpVtbl -> GetCodeInfo2(This,functionID,cCodeInfos,pcCodeInfos,codeInfos) ) 

#define ICorProfilerInfo4_GetClassFromTokenAndTypeArgs(This,moduleID,typeDef,cTypeArgs,typeArgs,pClassID)   \
    ( (This)->lpVtbl -> GetClassFromTokenAndTypeArgs(This,moduleID,typeDef,cTypeArgs,typeArgs,pClassID) ) 

#define ICorProfilerInfo4_GetFunctionFromTokenAndTypeArgs(This,moduleID,funcDef,classId,cTypeArgs,typeArgs,pFunctionID) \
    ( (This)->lpVtbl -> GetFunctionFromTokenAndTypeArgs(This,moduleID,funcDef,classId,cTypeArgs,typeArgs,pFunctionID) ) 

#define ICorProfilerInfo4_EnumModuleFrozenObjects(This,moduleID,ppEnum) \
    ( (This)->lpVtbl -> EnumModuleFrozenObjects(This,moduleID,ppEnum) ) 

#define ICorProfilerInfo4_GetArrayObjectInfo(This,objectId,cDimensions,pDimensionSizes,pDimensionLowerBounds,ppData)    \
    ( (This)->lpVtbl -> GetArrayObjectInfo(This,objectId,cDimensions,pDimensionSizes,pDimensionLowerBounds,ppData) ) 

#define ICorProfilerInfo4_GetBoxClassLayout(This,classId,pBufferOffset) \
    ( (This)->lpVtbl -> GetBoxClassLayout(This,classId,pBufferOffset) ) 

#define ICorProfilerInfo4_GetThreadAppDomain(This,threadId,pAppDomainId)    \
    ( (This)->lpVtbl -> GetThreadAppDomain(This,threadId,pAppDomainId) ) 

#define ICorProfilerInfo4_GetRVAStaticAddress(This,classId,fieldToken,ppAddress)    \
    ( (This)->lpVtbl -> GetRVAStaticAddress(This,classId,fieldToken,ppAddress) ) 

#define ICorProfilerInfo4_GetAppDomainStaticAddress(This,classId,fieldToken,appDomainId,ppAddress)  \
    ( (This)->lpVtbl -> GetAppDomainStaticAddress(This,classId,fieldToken,appDomainId,ppAddress) ) 

#define ICorProfilerInfo4_GetThreadStaticAddress(This,classId,fieldToken,threadId,ppAddress)    \
    ( (This)->lpVtbl -> GetThreadStaticAddress(This,classId,fieldToken,threadId,ppAddress) ) 

#define ICorProfilerInfo4_GetContextStaticAddress(This,classId,fieldToken,contextId,ppAddress)  \
    ( (This)->lpVtbl -> GetContextStaticAddress(This,classId,fieldToken,contextId,ppAddress) ) 

#define ICorProfilerInfo4_GetStaticFieldInfo(This,classId,fieldToken,pFieldInfo)    \
    ( (This)->lpVtbl -> GetStaticFieldInfo(This,classId,fieldToken,pFieldInfo) ) 

#define ICorProfilerInfo4_GetGenerationBounds(This,cObjectRanges,pcObjectRanges,ranges) \
    ( (This)->lpVtbl -> GetGenerationBounds(This,cObjectRanges,pcObjectRanges,ranges) ) 

#define ICorProfilerInfo4_GetObjectGeneration(This,objectId,range)  \
    ( (This)->lpVtbl -> GetObjectGeneration(This,objectId,range) ) 

#define ICorProfilerInfo4_GetNotifiedExceptionClauseInfo(This,pinfo)    \
    ( (This)->lpVtbl -> GetNotifiedExceptionClauseInfo(This,pinfo) ) 


#define ICorProfilerInfo4_EnumJITedFunctions(This,ppEnum)   \
    ( (This)->lpVtbl -> EnumJITedFunctions(This,ppEnum) ) 

#define ICorProfilerInfo4_RequestProfilerDetach(This,dwExpectedCompletionMilliseconds)  \
    ( (This)->lpVtbl -> RequestProfilerDetach(This,dwExpectedCompletionMilliseconds) ) 

#define ICorProfilerInfo4_SetFunctionIDMapper2(This,pFunc,clientData)   \
    ( (This)->lpVtbl -> SetFunctionIDMapper2(This,pFunc,clientData) ) 

#define ICorProfilerInfo4_GetStringLayout2(This,pStringLengthOffset,pBufferOffset)  \
    ( (This)->lpVtbl -> GetStringLayout2(This,pStringLengthOffset,pBufferOffset) ) 

#define ICorProfilerInfo4_SetEnterLeaveFunctionHooks3(This,pFuncEnter3,pFuncLeave3,pFuncTailcall3)  \
    ( (This)->lpVtbl -> SetEnterLeaveFunctionHooks3(This,pFuncEnter3,pFuncLeave3,pFuncTailcall3) ) 

#define ICorProfilerInfo4_SetEnterLeaveFunctionHooks3WithInfo(This,pFuncEnter3WithInfo,pFuncLeave3WithInfo,pFuncTailcall3WithInfo)  \
    ( (This)->lpVtbl -> SetEnterLeaveFunctionHooks3WithInfo(This,pFuncEnter3WithInfo,pFuncLeave3WithInfo,pFuncTailcall3WithInfo) ) 

#define ICorProfilerInfo4_GetFunctionEnter3Info(This,functionId,eltInfo,pFrameInfo,pcbArgumentInfo,pArgumentInfo)   \
    ( (This)->lpVtbl -> GetFunctionEnter3Info(This,functionId,eltInfo,pFrameInfo,pcbArgumentInfo,pArgumentInfo) ) 

#define ICorProfilerInfo4_GetFunctionLeave3Info(This,functionId,eltInfo,pFrameInfo,pRetvalRange)    \
    ( (This)->lpVtbl -> GetFunctionLeave3Info(This,functionId,eltInfo,pFrameInfo,pRetvalRange) ) 

#define ICorProfilerInfo4_GetFunctionTailcall3Info(This,functionId,eltInfo,pFrameInfo)  \
    ( (This)->lpVtbl -> GetFunctionTailcall3Info(This,functionId,eltInfo,pFrameInfo) ) 

#define ICorProfilerInfo4_EnumModules(This,ppEnum)  \
    ( (This)->lpVtbl -> EnumModules(This,ppEnum) ) 

#define ICorProfilerInfo4_GetRuntimeInformation(This,pClrInstanceId,pRuntimeType,pMajorVersion,pMinorVersion,pBuildNumber,pQFEVersion,cchVersionString,pcchVersionString,szVersionString)   \
    ( (This)->lpVtbl -> GetRuntimeInformation(This,pClrInstanceId,pRuntimeType,pMajorVersion,pMinorVersion,pBuildNumber,pQFEVersion,cchVersionString,pcchVersionString,szVersionString) ) 

#define ICorProfilerInfo4_GetThreadStaticAddress2(This,classId,fieldToken,appDomainId,threadId,ppAddress)   \
    ( (This)->lpVtbl -> GetThreadStaticAddress2(This,classId,fieldToken,appDomainId,threadId,ppAddress) ) 

#define ICorProfilerInfo4_GetAppDomainsContainingModule(This,moduleId,cAppDomainIds,pcAppDomainIds,appDomainIds)    \
    ( (This)->lpVtbl -> GetAppDomainsContainingModule(This,moduleId,cAppDomainIds,pcAppDomainIds,appDomainIds) ) 

#define ICorProfilerInfo4_GetModuleInfo2(This,moduleId,ppBaseLoadAddress,cchName,pcchName,szName,pAssemblyId,pdwModuleFlags)    \
    ( (This)->lpVtbl -> GetModuleInfo2(This,moduleId,ppBaseLoadAddress,cchName,pcchName,szName,pAssemblyId,pdwModuleFlags) ) 


#define ICorProfilerInfo4_EnumThreads(This,ppEnum)  \
    ( (This)->lpVtbl -> EnumThreads(This,ppEnum) ) 

#define ICorProfilerInfo4_InitializeCurrentThread(This) \
    ( (This)->lpVtbl -> InitializeCurrentThread(This) ) 

#define ICorProfilerInfo4_RequestReJIT(This,cFunctions,moduleIds,methodIds) \
    ( (This)->lpVtbl -> RequestReJIT(This,cFunctions,moduleIds,methodIds) ) 

#define ICorProfilerInfo4_RequestRevert(This,cFunctions,moduleIds,methodIds,status) \
    ( (This)->lpVtbl -> RequestRevert(This,cFunctions,moduleIds,methodIds,status) ) 

#define ICorProfilerInfo4_GetCodeInfo3(This,functionID,reJitId,cCodeInfos,pcCodeInfos,codeInfos)    \
    ( (This)->lpVtbl -> GetCodeInfo3(This,functionID,reJitId,cCodeInfos,pcCodeInfos,codeInfos) ) 

#define ICorProfilerInfo4_GetFunctionFromIP2(This,ip,pFunctionId,pReJitId)  \
    ( (This)->lpVtbl -> GetFunctionFromIP2(This,ip,pFunctionId,pReJitId) ) 

#define ICorProfilerInfo4_GetReJITIDs(This,functionId,cReJitIds,pcReJitIds,reJitIds)    \
    ( (This)->lpVtbl -> GetReJITIDs(This,functionId,cReJitIds,pcReJitIds,reJitIds) ) 

#define ICorProfilerInfo4_GetILToNativeMapping2(This,functionId,reJitId,cMap,pcMap,map) \
    ( (This)->lpVtbl -> GetILToNativeMapping2(This,functionId,reJitId,cMap,pcMap,map) ) 

#define ICorProfilerInfo4_EnumJITedFunctions2(This,ppEnum)  \
    ( (This)->lpVtbl -> EnumJITedFunctions2(This,ppEnum) ) 

#define ICorProfilerInfo4_GetObjectSize2(This,objectId,pcSize)  \
    ( (This)->lpVtbl -> GetObjectSize2(This,objectId,pcSize) ) 

#endif /* COBJMACROS */


#endif  /* C style interface */




#endif  /* __ICorProfilerInfo4_INTERFACE_DEFINED__ */


#ifndef __ICorProfilerInfo5_INTERFACE_DEFINED__
#define __ICorProfilerInfo5_INTERFACE_DEFINED__

/* interface ICorProfilerInfo5 */
/* [local][unique][uuid][object] */ 


EXTERN_C const IID IID_ICorProfilerInfo5;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("07602928-CE38-4B83-81E7-74ADAF781214")
    ICorProfilerInfo5 : public ICorProfilerInfo4
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE GetEventMask2( 
            /* [out] */ DWORD *pdwEventsLow,
            /* [out] */ DWORD *pdwEventsHigh) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE SetEventMask2( 
            /* [in] */ DWORD dwEventsLow,
            /* [in] */ DWORD dwEventsHigh) = 0;
        
    };
    
    
#else   /* C style interface */

    typedef struct ICorProfilerInfo5Vtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            ICorProfilerInfo5 * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            ICorProfilerInfo5 * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            ICorProfilerInfo5 * This);
        
        HRESULT ( STDMETHODCALLTYPE *GetClassFromObject )( 
            ICorProfilerInfo5 * This,
            /* [in] */ ObjectID objectId,
            /* [out] */ ClassID *pClassId);
        
        HRESULT ( STDMETHODCALLTYPE *GetClassFromToken )( 
            ICorProfilerInfo5 * This,
            /* [in] */ ModuleID moduleId,
            /* [in] */ mdTypeDef typeDef,
            /* [out] */ ClassID *pClassId);
        
        HRESULT ( STDMETHODCALLTYPE *GetCodeInfo )( 
            ICorProfilerInfo5 * This,
            /* [in] */ FunctionID functionId,
            /* [out] */ LPCBYTE *pStart,
            /* [out] */ ULONG *pcSize);
        
        HRESULT ( STDMETHODCALLTYPE *GetEventMask )( 
            ICorProfilerInfo5 * This,
            /* [out] */ DWORD *pdwEvents);
        
        HRESULT ( STDMETHODCALLTYPE *GetFunctionFromIP )( 
            ICorProfilerInfo5 * This,
            /* [in] */ LPCBYTE ip,
            /* [out] */ FunctionID *pFunctionId);
        
        HRESULT ( STDMETHODCALLTYPE *GetFunctionFromToken )( 
            ICorProfilerInfo5 * This,
            /* [in] */ ModuleID moduleId,
            /* [in] */ mdToken token,
            /* [out] */ FunctionID *pFunctionId);
        
        HRESULT ( STDMETHODCALLTYPE *GetHandleFromThread )( 
            ICorProfilerInfo5 * This,
            /* [in] */ ThreadID threadId,
            /* [out] */ HANDLE *phThread);
        
        HRESULT ( STDMETHODCALLTYPE *GetObjectSize )( 
            ICorProfilerInfo5 * This,
            /* [in] */ ObjectID objectId,
            /* [out] */ ULONG *pcSize);
        
        HRESULT ( STDMETHODCALLTYPE *IsArrayClass )( 
            ICorProfilerInfo5 * This,
            /* [in] */ ClassID classId,
            /* [out] */ CorElementType *pBaseElemType,
            /* [out] */ ClassID *pBaseClassId,
            /* [out] */ ULONG *pcRank);
        
        HRESULT ( STDMETHODCALLTYPE *GetThreadInfo )( 
            ICorProfilerInfo5 * This,
            /* [in] */ ThreadID threadId,
            /* [out] */ DWORD *pdwWin32ThreadId);
        
        HRESULT ( STDMETHODCALLTYPE *GetCurrentThreadID )( 
            ICorProfilerInfo5 * This,
            /* [out] */ ThreadID *pThreadId);
        
        HRESULT ( STDMETHODCALLTYPE *GetClassIDInfo )( 
            ICorProfilerInfo5 * This,
            /* [in] */ ClassID classId,
            /* [out] */ ModuleID *pModuleId,
            /* [out] */ mdTypeDef *pTypeDefToken);
        
        HRESULT ( STDMETHODCALLTYPE *GetFunctionInfo )( 
            ICorProfilerInfo5 * This,
            /* [in] */ FunctionID functionId,
            /* [out] */ ClassID *pClassId,
            /* [out] */ ModuleID *pModuleId,
            /* [out] */ mdToken *pToken);
        
        HRESULT ( STDMETHODCALLTYPE *SetEventMask )( 
            ICorProfilerInfo5 * This,
            /* [in] */ DWORD dwEvents);
        
        HRESULT ( STDMETHODCALLTYPE *SetEnterLeaveFunctionHooks )( 
            ICorProfilerInfo5 * This,
            /* [in] */ FunctionEnter *pFuncEnter,
            /* [in] */ FunctionLeave *pFuncLeave,
            /* [in] */ FunctionTailcall *pFuncTailcall);
        
        HRESULT ( STDMETHODCALLTYPE *SetFunctionIDMapper )( 
            ICorProfilerInfo5 * This,
            /* [in] */ FunctionIDMapper *pFunc);
        
        HRESULT ( STDMETHODCALLTYPE *GetTokenAndMetaDataFromFunction )( 
            ICorProfilerInfo5 * This,
            /* [in] */ FunctionID functionId,
            /* [in] */ REFIID riid,
            /* [out] */ IUnknown **ppImport,
            /* [out] */ mdToken *pToken);
        
        HRESULT ( STDMETHODCALLTYPE *GetModuleInfo )( 
            ICorProfilerInfo5 * This,
            /* [in] */ ModuleID moduleId,
            /* [out] */ LPCBYTE *ppBaseLoadAddress,
            /* [in] */ ULONG cchName,
            /* [out] */ ULONG *pcchName,
            /* [annotation][out] */ 
            _Out_writes_to_(cchName, *pcchName)  WCHAR szName[  ],
            /* [out] */ AssemblyID *pAssemblyId);
        
        HRESULT ( STDMETHODCALLTYPE *GetModuleMetaData )( 
            ICorProfilerInfo5 * This,
            /* [in] */ ModuleID moduleId,
            /* [in] */ DWORD dwOpenFlags,
            /* [in] */ REFIID riid,
            /* [out] */ IUnknown **ppOut);
        
        HRESULT ( STDMETHODCALLTYPE *GetILFunctionBody )( 
            ICorProfilerInfo5 * This,
            /* [in] */ ModuleID moduleId,
            /* [in] */ mdMethodDef methodId,
            /* [out] */ LPCBYTE *ppMethodHeader,
            /* [out] */ ULONG *pcbMethodSize);
        
        HRESULT ( STDMETHODCALLTYPE *GetILFunctionBodyAllocator )( 
            ICorProfilerInfo5 * This,
            /* [in] */ ModuleID moduleId,
            /* [out] */ IMethodMalloc **ppMalloc);
        
        HRESULT ( STDMETHODCALLTYPE *SetILFunctionBody )( 
            ICorProfilerInfo5 * This,
            /* [in] */ ModuleID moduleId,
            /* [in] */ mdMethodDef methodid,
            /* [in] */ LPCBYTE pbNewILMethodHeader);
        
        HRESULT ( STDMETHODCALLTYPE *GetAppDomainInfo )( 
            ICorProfilerInfo5 * This,
            /* [in] */ AppDomainID appDomainId,
            /* [in] */ ULONG cchName,
            /* [out] */ ULONG *pcchName,
            /* [annotation][out] */ 
            _Out_writes_to_(cchName, *pcchName)  WCHAR szName[  ],
            /* [out] */ ProcessID *pProcessId);
        
        HRESULT ( STDMETHODCALLTYPE *GetAssemblyInfo )( 
            ICorProfilerInfo5 * This,
            /* [in] */ AssemblyID assemblyId,
            /* [in] */ ULONG cchName,
            /* [out] */ ULONG *pcchName,
            /* [annotation][out] */ 
            _Out_writes_to_(cchName, *pcchName)  WCHAR szName[  ],
            /* [out] */ AppDomainID *pAppDomainId,
            /* [out] */ ModuleID *pModuleId);
        
        HRESULT ( STDMETHODCALLTYPE *SetFunctionReJIT )( 
            ICorProfilerInfo5 * This,
            /* [in] */ FunctionID functionId);
        
        HRESULT ( STDMETHODCALLTYPE *ForceGC )( 
            ICorProfilerInfo5 * This);
        
        HRESULT ( STDMETHODCALLTYPE *SetILInstrumentedCodeMap )( 
            ICorProfilerInfo5 * This,
            /* [in] */ FunctionID functionId,
            /* [in] */ BOOL fStartJit,
            /* [in] */ ULONG cILMapEntries,
            /* [size_is][in] */ COR_IL_MAP rgILMapEntries[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *GetInprocInspectionInterface )( 
            ICorProfilerInfo5 * This,
            /* [out] */ IUnknown **ppicd);
        
        HRESULT ( STDMETHODCALLTYPE *GetInprocInspectionIThisThread )( 
            ICorProfilerInfo5 * This,
            /* [out] */ IUnknown **ppicd);
        
        HRESULT ( STDMETHODCALLTYPE *GetThreadContext )( 
            ICorProfilerInfo5 * This,
            /* [in] */ ThreadID threadId,
            /* [out] */ ContextID *pContextId);
        
        HRESULT ( STDMETHODCALLTYPE *BeginInprocDebugging )( 
            ICorProfilerInfo5 * This,
            /* [in] */ BOOL fThisThreadOnly,
            /* [out] */ DWORD *pdwProfilerContext);
        
        HRESULT ( STDMETHODCALLTYPE *EndInprocDebugging )( 
            ICorProfilerInfo5 * This,
            /* [in] */ DWORD dwProfilerContext);
        
        HRESULT ( STDMETHODCALLTYPE *GetILToNativeMapping )( 
            ICorProfilerInfo5 * This,
            /* [in] */ FunctionID functionId,
            /* [in] */ ULONG32 cMap,
            /* [out] */ ULONG32 *pcMap,
            /* [length_is][size_is][out] */ COR_DEBUG_IL_TO_NATIVE_MAP map[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *DoStackSnapshot )( 
            ICorProfilerInfo5 * This,
            /* [in] */ ThreadID thread,
            /* [in] */ StackSnapshotCallback *callback,
            /* [in] */ ULONG32 infoFlags,
            /* [in] */ void *clientData,
            /* [size_is][in] */ BYTE context[  ],
            /* [in] */ ULONG32 contextSize);
        
        HRESULT ( STDMETHODCALLTYPE *SetEnterLeaveFunctionHooks2 )( 
            ICorProfilerInfo5 * This,
            /* [in] */ FunctionEnter2 *pFuncEnter,
            /* [in] */ FunctionLeave2 *pFuncLeave,
            /* [in] */ FunctionTailcall2 *pFuncTailcall);
        
        HRESULT ( STDMETHODCALLTYPE *GetFunctionInfo2 )( 
            ICorProfilerInfo5 * This,
            /* [in] */ FunctionID funcId,
            /* [in] */ COR_PRF_FRAME_INFO frameInfo,
            /* [out] */ ClassID *pClassId,
            /* [out] */ ModuleID *pModuleId,
            /* [out] */ mdToken *pToken,
            /* [in] */ ULONG32 cTypeArgs,
            /* [out] */ ULONG32 *pcTypeArgs,
            /* [out] */ ClassID typeArgs[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *GetStringLayout )( 
            ICorProfilerInfo5 * This,
            /* [out] */ ULONG *pBufferLengthOffset,
            /* [out] */ ULONG *pStringLengthOffset,
            /* [out] */ ULONG *pBufferOffset);
        
        HRESULT ( STDMETHODCALLTYPE *GetClassLayout )( 
            ICorProfilerInfo5 * This,
            /* [in] */ ClassID classID,
            /* [out][in] */ COR_FIELD_OFFSET rFieldOffset[  ],
            /* [in] */ ULONG cFieldOffset,
            /* [out] */ ULONG *pcFieldOffset,
            /* [out] */ ULONG *pulClassSize);
        
        HRESULT ( STDMETHODCALLTYPE *GetClassIDInfo2 )( 
            ICorProfilerInfo5 * This,
            /* [in] */ ClassID classId,
            /* [out] */ ModuleID *pModuleId,
            /* [out] */ mdTypeDef *pTypeDefToken,
            /* [out] */ ClassID *pParentClassId,
            /* [in] */ ULONG32 cNumTypeArgs,
            /* [out] */ ULONG32 *pcNumTypeArgs,
            /* [out] */ ClassID typeArgs[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *GetCodeInfo2 )( 
            ICorProfilerInfo5 * This,
            /* [in] */ FunctionID functionID,
            /* [in] */ ULONG32 cCodeInfos,
            /* [out] */ ULONG32 *pcCodeInfos,
            /* [length_is][size_is][out] */ COR_PRF_CODE_INFO codeInfos[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *GetClassFromTokenAndTypeArgs )( 
            ICorProfilerInfo5 * This,
            /* [in] */ ModuleID moduleID,
            /* [in] */ mdTypeDef typeDef,
            /* [in] */ ULONG32 cTypeArgs,
            /* [size_is][in] */ ClassID typeArgs[  ],
            /* [out] */ ClassID *pClassID);
        
        HRESULT ( STDMETHODCALLTYPE *GetFunctionFromTokenAndTypeArgs )( 
            ICorProfilerInfo5 * This,
            /* [in] */ ModuleID moduleID,
            /* [in] */ mdMethodDef funcDef,
            /* [in] */ ClassID classId,
            /* [in] */ ULONG32 cTypeArgs,
            /* [size_is][in] */ ClassID typeArgs[  ],
            /* [out] */ FunctionID *pFunctionID);
        
        HRESULT ( STDMETHODCALLTYPE *EnumModuleFrozenObjects )( 
            ICorProfilerInfo5 * This,
            /* [in] */ ModuleID moduleID,
            /* [out] */ ICorProfilerObjectEnum **ppEnum);
        
        HRESULT ( STDMETHODCALLTYPE *GetArrayObjectInfo )( 
            ICorProfilerInfo5 * This,
            /* [in] */ ObjectID objectId,
            /* [in] */ ULONG32 cDimensions,
            /* [size_is][out] */ ULONG32 pDimensionSizes[  ],
            /* [size_is][out] */ int pDimensionLowerBounds[  ],
            /* [out] */ BYTE **ppData);
        
        HRESULT ( STDMETHODCALLTYPE *GetBoxClassLayout )( 
            ICorProfilerInfo5 * This,
            /* [in] */ ClassID classId,
            /* [out] */ ULONG32 *pBufferOffset);
        
        HRESULT ( STDMETHODCALLTYPE *GetThreadAppDomain )( 
            ICorProfilerInfo5 * This,
            /* [in] */ ThreadID threadId,
            /* [out] */ AppDomainID *pAppDomainId);
        
        HRESULT ( STDMETHODCALLTYPE *GetRVAStaticAddress )( 
            ICorProfilerInfo5 * This,
            /* [in] */ ClassID classId,
            /* [in] */ mdFieldDef fieldToken,
            /* [out] */ void **ppAddress);
        
        HRESULT ( STDMETHODCALLTYPE *GetAppDomainStaticAddress )( 
            ICorProfilerInfo5 * This,
            /* [in] */ ClassID classId,
            /* [in] */ mdFieldDef fieldToken,
            /* [in] */ AppDomainID appDomainId,
            /* [out] */ void **ppAddress);
        
        HRESULT ( STDMETHODCALLTYPE *GetThreadStaticAddress )( 
            ICorProfilerInfo5 * This,
            /* [in] */ ClassID classId,
            /* [in] */ mdFieldDef fieldToken,
            /* [in] */ ThreadID threadId,
            /* [out] */ void **ppAddress);
        
        HRESULT ( STDMETHODCALLTYPE *GetContextStaticAddress )( 
            ICorProfilerInfo5 * This,
            /* [in] */ ClassID classId,
            /* [in] */ mdFieldDef fieldToken,
            /* [in] */ ContextID contextId,
            /* [out] */ void **ppAddress);
        
        HRESULT ( STDMETHODCALLTYPE *GetStaticFieldInfo )( 
            ICorProfilerInfo5 * This,
            /* [in] */ ClassID classId,
            /* [in] */ mdFieldDef fieldToken,
            /* [out] */ COR_PRF_STATIC_TYPE *pFieldInfo);
        
        HRESULT ( STDMETHODCALLTYPE *GetGenerationBounds )( 
            ICorProfilerInfo5 * This,
            /* [in] */ ULONG cObjectRanges,
            /* [out] */ ULONG *pcObjectRanges,
            /* [length_is][size_is][out] */ COR_PRF_GC_GENERATION_RANGE ranges[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *GetObjectGeneration )( 
            ICorProfilerInfo5 * This,
            /* [in] */ ObjectID objectId,
            /* [out] */ COR_PRF_GC_GENERATION_RANGE *range);
        
        HRESULT ( STDMETHODCALLTYPE *GetNotifiedExceptionClauseInfo )( 
            ICorProfilerInfo5 * This,
            /* [out] */ COR_PRF_EX_CLAUSE_INFO *pinfo);
        
        HRESULT ( STDMETHODCALLTYPE *EnumJITedFunctions )( 
            ICorProfilerInfo5 * This,
            /* [out] */ ICorProfilerFunctionEnum **ppEnum);
        
        HRESULT ( STDMETHODCALLTYPE *RequestProfilerDetach )( 
            ICorProfilerInfo5 * This,
            /* [in] */ DWORD dwExpectedCompletionMilliseconds);
        
        HRESULT ( STDMETHODCALLTYPE *SetFunctionIDMapper2 )( 
            ICorProfilerInfo5 * This,
            /* [in] */ FunctionIDMapper2 *pFunc,
            /* [in] */ void *clientData);
        
        HRESULT ( STDMETHODCALLTYPE *GetStringLayout2 )( 
            ICorProfilerInfo5 * This,
            /* [out] */ ULONG *pStringLengthOffset,
            /* [out] */ ULONG *pBufferOffset);
        
        HRESULT ( STDMETHODCALLTYPE *SetEnterLeaveFunctionHooks3 )( 
            ICorProfilerInfo5 * This,
            /* [in] */ FunctionEnter3 *pFuncEnter3,
            /* [in] */ FunctionLeave3 *pFuncLeave3,
            /* [in] */ FunctionTailcall3 *pFuncTailcall3);
        
        HRESULT ( STDMETHODCALLTYPE *SetEnterLeaveFunctionHooks3WithInfo )( 
            ICorProfilerInfo5 * This,
            /* [in] */ FunctionEnter3WithInfo *pFuncEnter3WithInfo,
            /* [in] */ FunctionLeave3WithInfo *pFuncLeave3WithInfo,
            /* [in] */ FunctionTailcall3WithInfo *pFuncTailcall3WithInfo);
        
        HRESULT ( STDMETHODCALLTYPE *GetFunctionEnter3Info )( 
            ICorProfilerInfo5 * This,
            /* [in] */ FunctionID functionId,
            /* [in] */ COR_PRF_ELT_INFO eltInfo,
            /* [out] */ COR_PRF_FRAME_INFO *pFrameInfo,
            /* [out][in] */ ULONG *pcbArgumentInfo,
            /* [size_is][out] */ COR_PRF_FUNCTION_ARGUMENT_INFO *pArgumentInfo);
        
        HRESULT ( STDMETHODCALLTYPE *GetFunctionLeave3Info )( 
            ICorProfilerInfo5 * This,
            /* [in] */ FunctionID functionId,
            /* [in] */ COR_PRF_ELT_INFO eltInfo,
            /* [out] */ COR_PRF_FRAME_INFO *pFrameInfo,
            /* [out] */ COR_PRF_FUNCTION_ARGUMENT_RANGE *pRetvalRange);
        
        HRESULT ( STDMETHODCALLTYPE *GetFunctionTailcall3Info )( 
            ICorProfilerInfo5 * This,
            /* [in] */ FunctionID functionId,
            /* [in] */ COR_PRF_ELT_INFO eltInfo,
            /* [out] */ COR_PRF_FRAME_INFO *pFrameInfo);
        
        HRESULT ( STDMETHODCALLTYPE *EnumModules )( 
            ICorProfilerInfo5 * This,
            /* [out] */ ICorProfilerModuleEnum **ppEnum);
        
        HRESULT ( STDMETHODCALLTYPE *GetRuntimeInformation )( 
            ICorProfilerInfo5 * This,
            /* [out] */ USHORT *pClrInstanceId,
            /* [out] */ COR_PRF_RUNTIME_TYPE *pRuntimeType,
            /* [out] */ USHORT *pMajorVersion,
            /* [out] */ USHORT *pMinorVersion,
            /* [out] */ USHORT *pBuildNumber,
            /* [out] */ USHORT *pQFEVersion,
            /* [in] */ ULONG cchVersionString,
            /* [out] */ ULONG *pcchVersionString,
            /* [annotation][out] */ 
            _Out_writes_to_(cchVersionString, *pcchVersionString)  WCHAR szVersionString[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *GetThreadStaticAddress2 )( 
            ICorProfilerInfo5 * This,
            /* [in] */ ClassID classId,
            /* [in] */ mdFieldDef fieldToken,
            /* [in] */ AppDomainID appDomainId,
            /* [in] */ ThreadID threadId,
            /* [out] */ void **ppAddress);
        
        HRESULT ( STDMETHODCALLTYPE *GetAppDomainsContainingModule )( 
            ICorProfilerInfo5 * This,
            /* [in] */ ModuleID moduleId,
            /* [in] */ ULONG32 cAppDomainIds,
            /* [out] */ ULONG32 *pcAppDomainIds,
            /* [length_is][size_is][out] */ AppDomainID appDomainIds[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *GetModuleInfo2 )( 
            ICorProfilerInfo5 * This,
            /* [in] */ ModuleID moduleId,
            /* [out] */ LPCBYTE *ppBaseLoadAddress,
            /* [in] */ ULONG cchName,
            /* [out] */ ULONG *pcchName,
            /* [annotation][out] */ 
            _Out_writes_to_(cchName, *pcchName)  WCHAR szName[  ],
            /* [out] */ AssemblyID *pAssemblyId,
            /* [out] */ DWORD *pdwModuleFlags);
        
        HRESULT ( STDMETHODCALLTYPE *EnumThreads )( 
            ICorProfilerInfo5 * This,
            /* [out] */ ICorProfilerThreadEnum **ppEnum);
        
        HRESULT ( STDMETHODCALLTYPE *InitializeCurrentThread )( 
            ICorProfilerInfo5 * This);
        
        HRESULT ( STDMETHODCALLTYPE *RequestReJIT )( 
            ICorProfilerInfo5 * This,
            /* [in] */ ULONG cFunctions,
            /* [size_is][in] */ ModuleID moduleIds[  ],
            /* [size_is][in] */ mdMethodDef methodIds[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *RequestRevert )( 
            ICorProfilerInfo5 * This,
            /* [in] */ ULONG cFunctions,
            /* [size_is][in] */ ModuleID moduleIds[  ],
            /* [size_is][in] */ mdMethodDef methodIds[  ],
            /* [size_is][out] */ HRESULT status[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *GetCodeInfo3 )( 
            ICorProfilerInfo5 * This,
            /* [in] */ FunctionID functionID,
            /* [in] */ ReJITID reJitId,
            /* [in] */ ULONG32 cCodeInfos,
            /* [out] */ ULONG32 *pcCodeInfos,
            /* [length_is][size_is][out] */ COR_PRF_CODE_INFO codeInfos[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *GetFunctionFromIP2 )( 
            ICorProfilerInfo5 * This,
            /* [in] */ LPCBYTE ip,
            /* [out] */ FunctionID *pFunctionId,
            /* [out] */ ReJITID *pReJitId);
        
        HRESULT ( STDMETHODCALLTYPE *GetReJITIDs )( 
            ICorProfilerInfo5 * This,
            /* [in] */ FunctionID functionId,
            /* [in] */ ULONG cReJitIds,
            /* [out] */ ULONG *pcReJitIds,
            /* [length_is][size_is][out] */ ReJITID reJitIds[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *GetILToNativeMapping2 )( 
            ICorProfilerInfo5 * This,
            /* [in] */ FunctionID functionId,
            /* [in] */ ReJITID reJitId,
            /* [in] */ ULONG32 cMap,
            /* [out] */ ULONG32 *pcMap,
            /* [length_is][size_is][out] */ COR_DEBUG_IL_TO_NATIVE_MAP map[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *EnumJITedFunctions2 )( 
            ICorProfilerInfo5 * This,
            /* [out] */ ICorProfilerFunctionEnum **ppEnum);
        
        HRESULT ( STDMETHODCALLTYPE *GetObjectSize2 )( 
            ICorProfilerInfo5 * This,
            /* [in] */ ObjectID objectId,
            /* [out] */ SIZE_T *pcSize);
        
        HRESULT ( STDMETHODCALLTYPE *GetEventMask2 )( 
            ICorProfilerInfo5 * This,
            /* [out] */ DWORD *pdwEventsLow,
            /* [out] */ DWORD *pdwEventsHigh);
        
        HRESULT ( STDMETHODCALLTYPE *SetEventMask2 )( 
            ICorProfilerInfo5 * This,
            /* [in] */ DWORD dwEventsLow,
            /* [in] */ DWORD dwEventsHigh);
        
        END_INTERFACE
    } ICorProfilerInfo5Vtbl;

    interface ICorProfilerInfo5
    {
        CONST_VTBL struct ICorProfilerInfo5Vtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define ICorProfilerInfo5_QueryInterface(This,riid,ppvObject)   \
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define ICorProfilerInfo5_AddRef(This)  \
    ( (This)->lpVtbl -> AddRef(This) ) 

#define ICorProfilerInfo5_Release(This) \
    ( (This)->lpVtbl -> Release(This) ) 


#define ICorProfilerInfo5_GetClassFromObject(This,objectId,pClassId)    \
    ( (This)->lpVtbl -> GetClassFromObject(This,objectId,pClassId) ) 

#define ICorProfilerInfo5_GetClassFromToken(This,moduleId,typeDef,pClassId) \
    ( (This)->lpVtbl -> GetClassFromToken(This,moduleId,typeDef,pClassId) ) 

#define ICorProfilerInfo5_GetCodeInfo(This,functionId,pStart,pcSize)    \
    ( (This)->lpVtbl -> GetCodeInfo(This,functionId,pStart,pcSize) ) 

#define ICorProfilerInfo5_GetEventMask(This,pdwEvents)  \
    ( (This)->lpVtbl -> GetEventMask(This,pdwEvents) ) 

#define ICorProfilerInfo5_GetFunctionFromIP(This,ip,pFunctionId)    \
    ( (This)->lpVtbl -> GetFunctionFromIP(This,ip,pFunctionId) ) 

#define ICorProfilerInfo5_GetFunctionFromToken(This,moduleId,token,pFunctionId) \
    ( (This)->lpVtbl -> GetFunctionFromToken(This,moduleId,token,pFunctionId) ) 

#define ICorProfilerInfo5_GetHandleFromThread(This,threadId,phThread)   \
    ( (This)->lpVtbl -> GetHandleFromThread(This,threadId,phThread) ) 

#define ICorProfilerInfo5_GetObjectSize(This,objectId,pcSize)   \
    ( (This)->lpVtbl -> GetObjectSize(This,objectId,pcSize) ) 

#define ICorProfilerInfo5_IsArrayClass(This,classId,pBaseElemType,pBaseClassId,pcRank)  \
    ( (This)->lpVtbl -> IsArrayClass(This,classId,pBaseElemType,pBaseClassId,pcRank) ) 

#define ICorProfilerInfo5_GetThreadInfo(This,threadId,pdwWin32ThreadId) \
    ( (This)->lpVtbl -> GetThreadInfo(This,threadId,pdwWin32ThreadId) ) 

#define ICorProfilerInfo5_GetCurrentThreadID(This,pThreadId)    \
    ( (This)->lpVtbl -> GetCurrentThreadID(This,pThreadId) ) 

#define ICorProfilerInfo5_GetClassIDInfo(This,classId,pModuleId,pTypeDefToken)  \
    ( (This)->lpVtbl -> GetClassIDInfo(This,classId,pModuleId,pTypeDefToken) ) 

#define ICorProfilerInfo5_GetFunctionInfo(This,functionId,pClassId,pModuleId,pToken)    \
    ( (This)->lpVtbl -> GetFunctionInfo(This,functionId,pClassId,pModuleId,pToken) ) 

#define ICorProfilerInfo5_SetEventMask(This,dwEvents)   \
    ( (This)->lpVtbl -> SetEventMask(This,dwEvents) ) 

#define ICorProfilerInfo5_SetEnterLeaveFunctionHooks(This,pFuncEnter,pFuncLeave,pFuncTailcall)  \
    ( (This)->lpVtbl -> SetEnterLeaveFunctionHooks(This,pFuncEnter,pFuncLeave,pFuncTailcall) ) 

#define ICorProfilerInfo5_SetFunctionIDMapper(This,pFunc)   \
    ( (This)->lpVtbl -> SetFunctionIDMapper(This,pFunc) ) 

#define ICorProfilerInfo5_GetTokenAndMetaDataFromFunction(This,functionId,riid,ppImport,pToken) \
    ( (This)->lpVtbl -> GetTokenAndMetaDataFromFunction(This,functionId,riid,ppImport,pToken) ) 

#define ICorProfilerInfo5_GetModuleInfo(This,moduleId,ppBaseLoadAddress,cchName,pcchName,szName,pAssemblyId)    \
    ( (This)->lpVtbl -> GetModuleInfo(This,moduleId,ppBaseLoadAddress,cchName,pcchName,szName,pAssemblyId) ) 

#define ICorProfilerInfo5_GetModuleMetaData(This,moduleId,dwOpenFlags,riid,ppOut)   \
    ( (This)->lpVtbl -> GetModuleMetaData(This,moduleId,dwOpenFlags,riid,ppOut) ) 

#define ICorProfilerInfo5_GetILFunctionBody(This,moduleId,methodId,ppMethodHeader,pcbMethodSize)    \
    ( (This)->lpVtbl -> GetILFunctionBody(This,moduleId,methodId,ppMethodHeader,pcbMethodSize) ) 

#define ICorProfilerInfo5_GetILFunctionBodyAllocator(This,moduleId,ppMalloc)    \
    ( (This)->lpVtbl -> GetILFunctionBodyAllocator(This,moduleId,ppMalloc) ) 

#define ICorProfilerInfo5_SetILFunctionBody(This,moduleId,methodid,pbNewILMethodHeader) \
    ( (This)->lpVtbl -> SetILFunctionBody(This,moduleId,methodid,pbNewILMethodHeader) ) 

#define ICorProfilerInfo5_GetAppDomainInfo(This,appDomainId,cchName,pcchName,szName,pProcessId) \
    ( (This)->lpVtbl -> GetAppDomainInfo(This,appDomainId,cchName,pcchName,szName,pProcessId) ) 

#define ICorProfilerInfo5_GetAssemblyInfo(This,assemblyId,cchName,pcchName,szName,pAppDomainId,pModuleId)   \
    ( (This)->lpVtbl -> GetAssemblyInfo(This,assemblyId,cchName,pcchName,szName,pAppDomainId,pModuleId) ) 

#define ICorProfilerInfo5_SetFunctionReJIT(This,functionId) \
    ( (This)->lpVtbl -> SetFunctionReJIT(This,functionId) ) 

#define ICorProfilerInfo5_ForceGC(This) \
    ( (This)->lpVtbl -> ForceGC(This) ) 

#define ICorProfilerInfo5_SetILInstrumentedCodeMap(This,functionId,fStartJit,cILMapEntries,rgILMapEntries)  \
    ( (This)->lpVtbl -> SetILInstrumentedCodeMap(This,functionId,fStartJit,cILMapEntries,rgILMapEntries) ) 

#define ICorProfilerInfo5_GetInprocInspectionInterface(This,ppicd)  \
    ( (This)->lpVtbl -> GetInprocInspectionInterface(This,ppicd) ) 

#define ICorProfilerInfo5_GetInprocInspectionIThisThread(This,ppicd)    \
    ( (This)->lpVtbl -> GetInprocInspectionIThisThread(This,ppicd) ) 

#define ICorProfilerInfo5_GetThreadContext(This,threadId,pContextId)    \
    ( (This)->lpVtbl -> GetThreadContext(This,threadId,pContextId) ) 

#define ICorProfilerInfo5_BeginInprocDebugging(This,fThisThreadOnly,pdwProfilerContext) \
    ( (This)->lpVtbl -> BeginInprocDebugging(This,fThisThreadOnly,pdwProfilerContext) ) 

#define ICorProfilerInfo5_EndInprocDebugging(This,dwProfilerContext)    \
    ( (This)->lpVtbl -> EndInprocDebugging(This,dwProfilerContext) ) 

#define ICorProfilerInfo5_GetILToNativeMapping(This,functionId,cMap,pcMap,map)  \
    ( (This)->lpVtbl -> GetILToNativeMapping(This,functionId,cMap,pcMap,map) ) 


#define ICorProfilerInfo5_DoStackSnapshot(This,thread,callback,infoFlags,clientData,context,contextSize)    \
    ( (This)->lpVtbl -> DoStackSnapshot(This,thread,callback,infoFlags,clientData,context,contextSize) ) 

#define ICorProfilerInfo5_SetEnterLeaveFunctionHooks2(This,pFuncEnter,pFuncLeave,pFuncTailcall) \
    ( (This)->lpVtbl -> SetEnterLeaveFunctionHooks2(This,pFuncEnter,pFuncLeave,pFuncTailcall) ) 

#define ICorProfilerInfo5_GetFunctionInfo2(This,funcId,frameInfo,pClassId,pModuleId,pToken,cTypeArgs,pcTypeArgs,typeArgs)   \
    ( (This)->lpVtbl -> GetFunctionInfo2(This,funcId,frameInfo,pClassId,pModuleId,pToken,cTypeArgs,pcTypeArgs,typeArgs) ) 

#define ICorProfilerInfo5_GetStringLayout(This,pBufferLengthOffset,pStringLengthOffset,pBufferOffset)   \
    ( (This)->lpVtbl -> GetStringLayout(This,pBufferLengthOffset,pStringLengthOffset,pBufferOffset) ) 

#define ICorProfilerInfo5_GetClassLayout(This,classID,rFieldOffset,cFieldOffset,pcFieldOffset,pulClassSize) \
    ( (This)->lpVtbl -> GetClassLayout(This,classID,rFieldOffset,cFieldOffset,pcFieldOffset,pulClassSize) ) 

#define ICorProfilerInfo5_GetClassIDInfo2(This,classId,pModuleId,pTypeDefToken,pParentClassId,cNumTypeArgs,pcNumTypeArgs,typeArgs)  \
    ( (This)->lpVtbl -> GetClassIDInfo2(This,classId,pModuleId,pTypeDefToken,pParentClassId,cNumTypeArgs,pcNumTypeArgs,typeArgs) ) 

#define ICorProfilerInfo5_GetCodeInfo2(This,functionID,cCodeInfos,pcCodeInfos,codeInfos)    \
    ( (This)->lpVtbl -> GetCodeInfo2(This,functionID,cCodeInfos,pcCodeInfos,codeInfos) ) 

#define ICorProfilerInfo5_GetClassFromTokenAndTypeArgs(This,moduleID,typeDef,cTypeArgs,typeArgs,pClassID)   \
    ( (This)->lpVtbl -> GetClassFromTokenAndTypeArgs(This,moduleID,typeDef,cTypeArgs,typeArgs,pClassID) ) 

#define ICorProfilerInfo5_GetFunctionFromTokenAndTypeArgs(This,moduleID,funcDef,classId,cTypeArgs,typeArgs,pFunctionID) \
    ( (This)->lpVtbl -> GetFunctionFromTokenAndTypeArgs(This,moduleID,funcDef,classId,cTypeArgs,typeArgs,pFunctionID) ) 

#define ICorProfilerInfo5_EnumModuleFrozenObjects(This,moduleID,ppEnum) \
    ( (This)->lpVtbl -> EnumModuleFrozenObjects(This,moduleID,ppEnum) ) 

#define ICorProfilerInfo5_GetArrayObjectInfo(This,objectId,cDimensions,pDimensionSizes,pDimensionLowerBounds,ppData)    \
    ( (This)->lpVtbl -> GetArrayObjectInfo(This,objectId,cDimensions,pDimensionSizes,pDimensionLowerBounds,ppData) ) 

#define ICorProfilerInfo5_GetBoxClassLayout(This,classId,pBufferOffset) \
    ( (This)->lpVtbl -> GetBoxClassLayout(This,classId,pBufferOffset) ) 

#define ICorProfilerInfo5_GetThreadAppDomain(This,threadId,pAppDomainId)    \
    ( (This)->lpVtbl -> GetThreadAppDomain(This,threadId,pAppDomainId) ) 

#define ICorProfilerInfo5_GetRVAStaticAddress(This,classId,fieldToken,ppAddress)    \
    ( (This)->lpVtbl -> GetRVAStaticAddress(This,classId,fieldToken,ppAddress) ) 

#define ICorProfilerInfo5_GetAppDomainStaticAddress(This,classId,fieldToken,appDomainId,ppAddress)  \
    ( (This)->lpVtbl -> GetAppDomainStaticAddress(This,classId,fieldToken,appDomainId,ppAddress) ) 

#define ICorProfilerInfo5_GetThreadStaticAddress(This,classId,fieldToken,threadId,ppAddress)    \
    ( (This)->lpVtbl -> GetThreadStaticAddress(This,classId,fieldToken,threadId,ppAddress) ) 

#define ICorProfilerInfo5_GetContextStaticAddress(This,classId,fieldToken,contextId,ppAddress)  \
    ( (This)->lpVtbl -> GetContextStaticAddress(This,classId,fieldToken,contextId,ppAddress) ) 

#define ICorProfilerInfo5_GetStaticFieldInfo(This,classId,fieldToken,pFieldInfo)    \
    ( (This)->lpVtbl -> GetStaticFieldInfo(This,classId,fieldToken,pFieldInfo) ) 

#define ICorProfilerInfo5_GetGenerationBounds(This,cObjectRanges,pcObjectRanges,ranges) \
    ( (This)->lpVtbl -> GetGenerationBounds(This,cObjectRanges,pcObjectRanges,ranges) ) 

#define ICorProfilerInfo5_GetObjectGeneration(This,objectId,range)  \
    ( (This)->lpVtbl -> GetObjectGeneration(This,objectId,range) ) 

#define ICorProfilerInfo5_GetNotifiedExceptionClauseInfo(This,pinfo)    \
    ( (This)->lpVtbl -> GetNotifiedExceptionClauseInfo(This,pinfo) ) 


#define ICorProfilerInfo5_EnumJITedFunctions(This,ppEnum)   \
    ( (This)->lpVtbl -> EnumJITedFunctions(This,ppEnum) ) 

#define ICorProfilerInfo5_RequestProfilerDetach(This,dwExpectedCompletionMilliseconds)  \
    ( (This)->lpVtbl -> RequestProfilerDetach(This,dwExpectedCompletionMilliseconds) ) 

#define ICorProfilerInfo5_SetFunctionIDMapper2(This,pFunc,clientData)   \
    ( (This)->lpVtbl -> SetFunctionIDMapper2(This,pFunc,clientData) ) 

#define ICorProfilerInfo5_GetStringLayout2(This,pStringLengthOffset,pBufferOffset)  \
    ( (This)->lpVtbl -> GetStringLayout2(This,pStringLengthOffset,pBufferOffset) ) 

#define ICorProfilerInfo5_SetEnterLeaveFunctionHooks3(This,pFuncEnter3,pFuncLeave3,pFuncTailcall3)  \
    ( (This)->lpVtbl -> SetEnterLeaveFunctionHooks3(This,pFuncEnter3,pFuncLeave3,pFuncTailcall3) ) 

#define ICorProfilerInfo5_SetEnterLeaveFunctionHooks3WithInfo(This,pFuncEnter3WithInfo,pFuncLeave3WithInfo,pFuncTailcall3WithInfo)  \
    ( (This)->lpVtbl -> SetEnterLeaveFunctionHooks3WithInfo(This,pFuncEnter3WithInfo,pFuncLeave3WithInfo,pFuncTailcall3WithInfo) ) 

#define ICorProfilerInfo5_GetFunctionEnter3Info(This,functionId,eltInfo,pFrameInfo,pcbArgumentInfo,pArgumentInfo)   \
    ( (This)->lpVtbl -> GetFunctionEnter3Info(This,functionId,eltInfo,pFrameInfo,pcbArgumentInfo,pArgumentInfo) ) 

#define ICorProfilerInfo5_GetFunctionLeave3Info(This,functionId,eltInfo,pFrameInfo,pRetvalRange)    \
    ( (This)->lpVtbl -> GetFunctionLeave3Info(This,functionId,eltInfo,pFrameInfo,pRetvalRange) ) 

#define ICorProfilerInfo5_GetFunctionTailcall3Info(This,functionId,eltInfo,pFrameInfo)  \
    ( (This)->lpVtbl -> GetFunctionTailcall3Info(This,functionId,eltInfo,pFrameInfo) ) 

#define ICorProfilerInfo5_EnumModules(This,ppEnum)  \
    ( (This)->lpVtbl -> EnumModules(This,ppEnum) ) 

#define ICorProfilerInfo5_GetRuntimeInformation(This,pClrInstanceId,pRuntimeType,pMajorVersion,pMinorVersion,pBuildNumber,pQFEVersion,cchVersionString,pcchVersionString,szVersionString)   \
    ( (This)->lpVtbl -> GetRuntimeInformation(This,pClrInstanceId,pRuntimeType,pMajorVersion,pMinorVersion,pBuildNumber,pQFEVersion,cchVersionString,pcchVersionString,szVersionString) ) 

#define ICorProfilerInfo5_GetThreadStaticAddress2(This,classId,fieldToken,appDomainId,threadId,ppAddress)   \
    ( (This)->lpVtbl -> GetThreadStaticAddress2(This,classId,fieldToken,appDomainId,threadId,ppAddress) ) 

#define ICorProfilerInfo5_GetAppDomainsContainingModule(This,moduleId,cAppDomainIds,pcAppDomainIds,appDomainIds)    \
    ( (This)->lpVtbl -> GetAppDomainsContainingModule(This,moduleId,cAppDomainIds,pcAppDomainIds,appDomainIds) ) 

#define ICorProfilerInfo5_GetModuleInfo2(This,moduleId,ppBaseLoadAddress,cchName,pcchName,szName,pAssemblyId,pdwModuleFlags)    \
    ( (This)->lpVtbl -> GetModuleInfo2(This,moduleId,ppBaseLoadAddress,cchName,pcchName,szName,pAssemblyId,pdwModuleFlags) ) 


#define ICorProfilerInfo5_EnumThreads(This,ppEnum)  \
    ( (This)->lpVtbl -> EnumThreads(This,ppEnum) ) 

#define ICorProfilerInfo5_InitializeCurrentThread(This) \
    ( (This)->lpVtbl -> InitializeCurrentThread(This) ) 

#define ICorProfilerInfo5_RequestReJIT(This,cFunctions,moduleIds,methodIds) \
    ( (This)->lpVtbl -> RequestReJIT(This,cFunctions,moduleIds,methodIds) ) 

#define ICorProfilerInfo5_RequestRevert(This,cFunctions,moduleIds,methodIds,status) \
    ( (This)->lpVtbl -> RequestRevert(This,cFunctions,moduleIds,methodIds,status) ) 

#define ICorProfilerInfo5_GetCodeInfo3(This,functionID,reJitId,cCodeInfos,pcCodeInfos,codeInfos)    \
    ( (This)->lpVtbl -> GetCodeInfo3(This,functionID,reJitId,cCodeInfos,pcCodeInfos,codeInfos) ) 

#define ICorProfilerInfo5_GetFunctionFromIP2(This,ip,pFunctionId,pReJitId)  \
    ( (This)->lpVtbl -> GetFunctionFromIP2(This,ip,pFunctionId,pReJitId) ) 

#define ICorProfilerInfo5_GetReJITIDs(This,functionId,cReJitIds,pcReJitIds,reJitIds)    \
    ( (This)->lpVtbl -> GetReJITIDs(This,functionId,cReJitIds,pcReJitIds,reJitIds) ) 

#define ICorProfilerInfo5_GetILToNativeMapping2(This,functionId,reJitId,cMap,pcMap,map) \
    ( (This)->lpVtbl -> GetILToNativeMapping2(This,functionId,reJitId,cMap,pcMap,map) ) 

#define ICorProfilerInfo5_EnumJITedFunctions2(This,ppEnum)  \
    ( (This)->lpVtbl -> EnumJITedFunctions2(This,ppEnum) ) 

#define ICorProfilerInfo5_GetObjectSize2(This,objectId,pcSize)  \
    ( (This)->lpVtbl -> GetObjectSize2(This,objectId,pcSize) ) 


#define ICorProfilerInfo5_GetEventMask2(This,pdwEventsLow,pdwEventsHigh)    \
    ( (This)->lpVtbl -> GetEventMask2(This,pdwEventsLow,pdwEventsHigh) ) 

#define ICorProfilerInfo5_SetEventMask2(This,dwEventsLow,dwEventsHigh)  \
    ( (This)->lpVtbl -> SetEventMask2(This,dwEventsLow,dwEventsHigh) ) 

#endif /* COBJMACROS */


#endif  /* C style interface */




#endif  /* __ICorProfilerInfo5_INTERFACE_DEFINED__ */


#ifndef __ICorProfilerInfo6_INTERFACE_DEFINED__
#define __ICorProfilerInfo6_INTERFACE_DEFINED__

/* interface ICorProfilerInfo6 */
/* [local][unique][uuid][object] */ 


EXTERN_C const IID IID_ICorProfilerInfo6;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("F30A070D-BFFB-46A7-B1D8-8781EF7B698A")
    ICorProfilerInfo6 : public ICorProfilerInfo5
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE EnumNgenModuleMethodsInliningThisMethod( 
            /* [in] */ ModuleID inlinersModuleId,
            /* [in] */ ModuleID inlineeModuleId,
            /* [in] */ mdMethodDef inlineeMethodId,
            /* [out] */ BOOL *incompleteData,
            /* [out] */ ICorProfilerMethodEnum **ppEnum) = 0;
        
    };
    
    
#else   /* C style interface */

    typedef struct ICorProfilerInfo6Vtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            ICorProfilerInfo6 * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            ICorProfilerInfo6 * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            ICorProfilerInfo6 * This);
        
        HRESULT ( STDMETHODCALLTYPE *GetClassFromObject )( 
            ICorProfilerInfo6 * This,
            /* [in] */ ObjectID objectId,
            /* [out] */ ClassID *pClassId);
        
        HRESULT ( STDMETHODCALLTYPE *GetClassFromToken )( 
            ICorProfilerInfo6 * This,
            /* [in] */ ModuleID moduleId,
            /* [in] */ mdTypeDef typeDef,
            /* [out] */ ClassID *pClassId);
        
        HRESULT ( STDMETHODCALLTYPE *GetCodeInfo )( 
            ICorProfilerInfo6 * This,
            /* [in] */ FunctionID functionId,
            /* [out] */ LPCBYTE *pStart,
            /* [out] */ ULONG *pcSize);
        
        HRESULT ( STDMETHODCALLTYPE *GetEventMask )( 
            ICorProfilerInfo6 * This,
            /* [out] */ DWORD *pdwEvents);
        
        HRESULT ( STDMETHODCALLTYPE *GetFunctionFromIP )( 
            ICorProfilerInfo6 * This,
            /* [in] */ LPCBYTE ip,
            /* [out] */ FunctionID *pFunctionId);
        
        HRESULT ( STDMETHODCALLTYPE *GetFunctionFromToken )( 
            ICorProfilerInfo6 * This,
            /* [in] */ ModuleID moduleId,
            /* [in] */ mdToken token,
            /* [out] */ FunctionID *pFunctionId);
        
        HRESULT ( STDMETHODCALLTYPE *GetHandleFromThread )( 
            ICorProfilerInfo6 * This,
            /* [in] */ ThreadID threadId,
            /* [out] */ HANDLE *phThread);
        
        HRESULT ( STDMETHODCALLTYPE *GetObjectSize )( 
            ICorProfilerInfo6 * This,
            /* [in] */ ObjectID objectId,
            /* [out] */ ULONG *pcSize);
        
        HRESULT ( STDMETHODCALLTYPE *IsArrayClass )( 
            ICorProfilerInfo6 * This,
            /* [in] */ ClassID classId,
            /* [out] */ CorElementType *pBaseElemType,
            /* [out] */ ClassID *pBaseClassId,
            /* [out] */ ULONG *pcRank);
        
        HRESULT ( STDMETHODCALLTYPE *GetThreadInfo )( 
            ICorProfilerInfo6 * This,
            /* [in] */ ThreadID threadId,
            /* [out] */ DWORD *pdwWin32ThreadId);
        
        HRESULT ( STDMETHODCALLTYPE *GetCurrentThreadID )( 
            ICorProfilerInfo6 * This,
            /* [out] */ ThreadID *pThreadId);
        
        HRESULT ( STDMETHODCALLTYPE *GetClassIDInfo )( 
            ICorProfilerInfo6 * This,
            /* [in] */ ClassID classId,
            /* [out] */ ModuleID *pModuleId,
            /* [out] */ mdTypeDef *pTypeDefToken);
        
        HRESULT ( STDMETHODCALLTYPE *GetFunctionInfo )( 
            ICorProfilerInfo6 * This,
            /* [in] */ FunctionID functionId,
            /* [out] */ ClassID *pClassId,
            /* [out] */ ModuleID *pModuleId,
            /* [out] */ mdToken *pToken);
        
        HRESULT ( STDMETHODCALLTYPE *SetEventMask )( 
            ICorProfilerInfo6 * This,
            /* [in] */ DWORD dwEvents);
        
        HRESULT ( STDMETHODCALLTYPE *SetEnterLeaveFunctionHooks )( 
            ICorProfilerInfo6 * This,
            /* [in] */ FunctionEnter *pFuncEnter,
            /* [in] */ FunctionLeave *pFuncLeave,
            /* [in] */ FunctionTailcall *pFuncTailcall);
        
        HRESULT ( STDMETHODCALLTYPE *SetFunctionIDMapper )( 
            ICorProfilerInfo6 * This,
            /* [in] */ FunctionIDMapper *pFunc);
        
        HRESULT ( STDMETHODCALLTYPE *GetTokenAndMetaDataFromFunction )( 
            ICorProfilerInfo6 * This,
            /* [in] */ FunctionID functionId,
            /* [in] */ REFIID riid,
            /* [out] */ IUnknown **ppImport,
            /* [out] */ mdToken *pToken);
        
        HRESULT ( STDMETHODCALLTYPE *GetModuleInfo )( 
            ICorProfilerInfo6 * This,
            /* [in] */ ModuleID moduleId,
            /* [out] */ LPCBYTE *ppBaseLoadAddress,
            /* [in] */ ULONG cchName,
            /* [out] */ ULONG *pcchName,
            /* [annotation][out] */ 
            _Out_writes_to_(cchName, *pcchName)  WCHAR szName[  ],
            /* [out] */ AssemblyID *pAssemblyId);
        
        HRESULT ( STDMETHODCALLTYPE *GetModuleMetaData )( 
            ICorProfilerInfo6 * This,
            /* [in] */ ModuleID moduleId,
            /* [in] */ DWORD dwOpenFlags,
            /* [in] */ REFIID riid,
            /* [out] */ IUnknown **ppOut);
        
        HRESULT ( STDMETHODCALLTYPE *GetILFunctionBody )( 
            ICorProfilerInfo6 * This,
            /* [in] */ ModuleID moduleId,
            /* [in] */ mdMethodDef methodId,
            /* [out] */ LPCBYTE *ppMethodHeader,
            /* [out] */ ULONG *pcbMethodSize);
        
        HRESULT ( STDMETHODCALLTYPE *GetILFunctionBodyAllocator )( 
            ICorProfilerInfo6 * This,
            /* [in] */ ModuleID moduleId,
            /* [out] */ IMethodMalloc **ppMalloc);
        
        HRESULT ( STDMETHODCALLTYPE *SetILFunctionBody )( 
            ICorProfilerInfo6 * This,
            /* [in] */ ModuleID moduleId,
            /* [in] */ mdMethodDef methodid,
            /* [in] */ LPCBYTE pbNewILMethodHeader);
        
        HRESULT ( STDMETHODCALLTYPE *GetAppDomainInfo )( 
            ICorProfilerInfo6 * This,
            /* [in] */ AppDomainID appDomainId,
            /* [in] */ ULONG cchName,
            /* [out] */ ULONG *pcchName,
            /* [annotation][out] */ 
            _Out_writes_to_(cchName, *pcchName)  WCHAR szName[  ],
            /* [out] */ ProcessID *pProcessId);
        
        HRESULT ( STDMETHODCALLTYPE *GetAssemblyInfo )( 
            ICorProfilerInfo6 * This,
            /* [in] */ AssemblyID assemblyId,
            /* [in] */ ULONG cchName,
            /* [out] */ ULONG *pcchName,
            /* [annotation][out] */ 
            _Out_writes_to_(cchName, *pcchName)  WCHAR szName[  ],
            /* [out] */ AppDomainID *pAppDomainId,
            /* [out] */ ModuleID *pModuleId);
        
        HRESULT ( STDMETHODCALLTYPE *SetFunctionReJIT )( 
            ICorProfilerInfo6 * This,
            /* [in] */ FunctionID functionId);
        
        HRESULT ( STDMETHODCALLTYPE *ForceGC )( 
            ICorProfilerInfo6 * This);
        
        HRESULT ( STDMETHODCALLTYPE *SetILInstrumentedCodeMap )( 
            ICorProfilerInfo6 * This,
            /* [in] */ FunctionID functionId,
            /* [in] */ BOOL fStartJit,
            /* [in] */ ULONG cILMapEntries,
            /* [size_is][in] */ COR_IL_MAP rgILMapEntries[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *GetInprocInspectionInterface )( 
            ICorProfilerInfo6 * This,
            /* [out] */ IUnknown **ppicd);
        
        HRESULT ( STDMETHODCALLTYPE *GetInprocInspectionIThisThread )( 
            ICorProfilerInfo6 * This,
            /* [out] */ IUnknown **ppicd);
        
        HRESULT ( STDMETHODCALLTYPE *GetThreadContext )( 
            ICorProfilerInfo6 * This,
            /* [in] */ ThreadID threadId,
            /* [out] */ ContextID *pContextId);
        
        HRESULT ( STDMETHODCALLTYPE *BeginInprocDebugging )( 
            ICorProfilerInfo6 * This,
            /* [in] */ BOOL fThisThreadOnly,
            /* [out] */ DWORD *pdwProfilerContext);
        
        HRESULT ( STDMETHODCALLTYPE *EndInprocDebugging )( 
            ICorProfilerInfo6 * This,
            /* [in] */ DWORD dwProfilerContext);
        
        HRESULT ( STDMETHODCALLTYPE *GetILToNativeMapping )( 
            ICorProfilerInfo6 * This,
            /* [in] */ FunctionID functionId,
            /* [in] */ ULONG32 cMap,
            /* [out] */ ULONG32 *pcMap,
            /* [length_is][size_is][out] */ COR_DEBUG_IL_TO_NATIVE_MAP map[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *DoStackSnapshot )( 
            ICorProfilerInfo6 * This,
            /* [in] */ ThreadID thread,
            /* [in] */ StackSnapshotCallback *callback,
            /* [in] */ ULONG32 infoFlags,
            /* [in] */ void *clientData,
            /* [size_is][in] */ BYTE context[  ],
            /* [in] */ ULONG32 contextSize);
        
        HRESULT ( STDMETHODCALLTYPE *SetEnterLeaveFunctionHooks2 )( 
            ICorProfilerInfo6 * This,
            /* [in] */ FunctionEnter2 *pFuncEnter,
            /* [in] */ FunctionLeave2 *pFuncLeave,
            /* [in] */ FunctionTailcall2 *pFuncTailcall);
        
        HRESULT ( STDMETHODCALLTYPE *GetFunctionInfo2 )( 
            ICorProfilerInfo6 * This,
            /* [in] */ FunctionID funcId,
            /* [in] */ COR_PRF_FRAME_INFO frameInfo,
            /* [out] */ ClassID *pClassId,
            /* [out] */ ModuleID *pModuleId,
            /* [out] */ mdToken *pToken,
            /* [in] */ ULONG32 cTypeArgs,
            /* [out] */ ULONG32 *pcTypeArgs,
            /* [out] */ ClassID typeArgs[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *GetStringLayout )( 
            ICorProfilerInfo6 * This,
            /* [out] */ ULONG *pBufferLengthOffset,
            /* [out] */ ULONG *pStringLengthOffset,
            /* [out] */ ULONG *pBufferOffset);
        
        HRESULT ( STDMETHODCALLTYPE *GetClassLayout )( 
            ICorProfilerInfo6 * This,
            /* [in] */ ClassID classID,
            /* [out][in] */ COR_FIELD_OFFSET rFieldOffset[  ],
            /* [in] */ ULONG cFieldOffset,
            /* [out] */ ULONG *pcFieldOffset,
            /* [out] */ ULONG *pulClassSize);
        
        HRESULT ( STDMETHODCALLTYPE *GetClassIDInfo2 )( 
            ICorProfilerInfo6 * This,
            /* [in] */ ClassID classId,
            /* [out] */ ModuleID *pModuleId,
            /* [out] */ mdTypeDef *pTypeDefToken,
            /* [out] */ ClassID *pParentClassId,
            /* [in] */ ULONG32 cNumTypeArgs,
            /* [out] */ ULONG32 *pcNumTypeArgs,
            /* [out] */ ClassID typeArgs[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *GetCodeInfo2 )( 
            ICorProfilerInfo6 * This,
            /* [in] */ FunctionID functionID,
            /* [in] */ ULONG32 cCodeInfos,
            /* [out] */ ULONG32 *pcCodeInfos,
            /* [length_is][size_is][out] */ COR_PRF_CODE_INFO codeInfos[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *GetClassFromTokenAndTypeArgs )( 
            ICorProfilerInfo6 * This,
            /* [in] */ ModuleID moduleID,
            /* [in] */ mdTypeDef typeDef,
            /* [in] */ ULONG32 cTypeArgs,
            /* [size_is][in] */ ClassID typeArgs[  ],
            /* [out] */ ClassID *pClassID);
        
        HRESULT ( STDMETHODCALLTYPE *GetFunctionFromTokenAndTypeArgs )( 
            ICorProfilerInfo6 * This,
            /* [in] */ ModuleID moduleID,
            /* [in] */ mdMethodDef funcDef,
            /* [in] */ ClassID classId,
            /* [in] */ ULONG32 cTypeArgs,
            /* [size_is][in] */ ClassID typeArgs[  ],
            /* [out] */ FunctionID *pFunctionID);
        
        HRESULT ( STDMETHODCALLTYPE *EnumModuleFrozenObjects )( 
            ICorProfilerInfo6 * This,
            /* [in] */ ModuleID moduleID,
            /* [out] */ ICorProfilerObjectEnum **ppEnum);
        
        HRESULT ( STDMETHODCALLTYPE *GetArrayObjectInfo )( 
            ICorProfilerInfo6 * This,
            /* [in] */ ObjectID objectId,
            /* [in] */ ULONG32 cDimensions,
            /* [size_is][out] */ ULONG32 pDimensionSizes[  ],
            /* [size_is][out] */ int pDimensionLowerBounds[  ],
            /* [out] */ BYTE **ppData);
        
        HRESULT ( STDMETHODCALLTYPE *GetBoxClassLayout )( 
            ICorProfilerInfo6 * This,
            /* [in] */ ClassID classId,
            /* [out] */ ULONG32 *pBufferOffset);
        
        HRESULT ( STDMETHODCALLTYPE *GetThreadAppDomain )( 
            ICorProfilerInfo6 * This,
            /* [in] */ ThreadID threadId,
            /* [out] */ AppDomainID *pAppDomainId);
        
        HRESULT ( STDMETHODCALLTYPE *GetRVAStaticAddress )( 
            ICorProfilerInfo6 * This,
            /* [in] */ ClassID classId,
            /* [in] */ mdFieldDef fieldToken,
            /* [out] */ void **ppAddress);
        
        HRESULT ( STDMETHODCALLTYPE *GetAppDomainStaticAddress )( 
            ICorProfilerInfo6 * This,
            /* [in] */ ClassID classId,
            /* [in] */ mdFieldDef fieldToken,
            /* [in] */ AppDomainID appDomainId,
            /* [out] */ void **ppAddress);
        
        HRESULT ( STDMETHODCALLTYPE *GetThreadStaticAddress )( 
            ICorProfilerInfo6 * This,
            /* [in] */ ClassID classId,
            /* [in] */ mdFieldDef fieldToken,
            /* [in] */ ThreadID threadId,
            /* [out] */ void **ppAddress);
        
        HRESULT ( STDMETHODCALLTYPE *GetContextStaticAddress )( 
            ICorProfilerInfo6 * This,
            /* [in] */ ClassID classId,
            /* [in] */ mdFieldDef fieldToken,
            /* [in] */ ContextID contextId,
            /* [out] */ void **ppAddress);
        
        HRESULT ( STDMETHODCALLTYPE *GetStaticFieldInfo )( 
            ICorProfilerInfo6 * This,
            /* [in] */ ClassID classId,
            /* [in] */ mdFieldDef fieldToken,
            /* [out] */ COR_PRF_STATIC_TYPE *pFieldInfo);
        
        HRESULT ( STDMETHODCALLTYPE *GetGenerationBounds )( 
            ICorProfilerInfo6 * This,
            /* [in] */ ULONG cObjectRanges,
            /* [out] */ ULONG *pcObjectRanges,
            /* [length_is][size_is][out] */ COR_PRF_GC_GENERATION_RANGE ranges[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *GetObjectGeneration )( 
            ICorProfilerInfo6 * This,
            /* [in] */ ObjectID objectId,
            /* [out] */ COR_PRF_GC_GENERATION_RANGE *range);
        
        HRESULT ( STDMETHODCALLTYPE *GetNotifiedExceptionClauseInfo )( 
            ICorProfilerInfo6 * This,
            /* [out] */ COR_PRF_EX_CLAUSE_INFO *pinfo);
        
        HRESULT ( STDMETHODCALLTYPE *EnumJITedFunctions )( 
            ICorProfilerInfo6 * This,
            /* [out] */ ICorProfilerFunctionEnum **ppEnum);
        
        HRESULT ( STDMETHODCALLTYPE *RequestProfilerDetach )( 
            ICorProfilerInfo6 * This,
            /* [in] */ DWORD dwExpectedCompletionMilliseconds);
        
        HRESULT ( STDMETHODCALLTYPE *SetFunctionIDMapper2 )( 
            ICorProfilerInfo6 * This,
            /* [in] */ FunctionIDMapper2 *pFunc,
            /* [in] */ void *clientData);
        
        HRESULT ( STDMETHODCALLTYPE *GetStringLayout2 )( 
            ICorProfilerInfo6 * This,
            /* [out] */ ULONG *pStringLengthOffset,
            /* [out] */ ULONG *pBufferOffset);
        
        HRESULT ( STDMETHODCALLTYPE *SetEnterLeaveFunctionHooks3 )( 
            ICorProfilerInfo6 * This,
            /* [in] */ FunctionEnter3 *pFuncEnter3,
            /* [in] */ FunctionLeave3 *pFuncLeave3,
            /* [in] */ FunctionTailcall3 *pFuncTailcall3);
        
        HRESULT ( STDMETHODCALLTYPE *SetEnterLeaveFunctionHooks3WithInfo )( 
            ICorProfilerInfo6 * This,
            /* [in] */ FunctionEnter3WithInfo *pFuncEnter3WithInfo,
            /* [in] */ FunctionLeave3WithInfo *pFuncLeave3WithInfo,
            /* [in] */ FunctionTailcall3WithInfo *pFuncTailcall3WithInfo);
        
        HRESULT ( STDMETHODCALLTYPE *GetFunctionEnter3Info )( 
            ICorProfilerInfo6 * This,
            /* [in] */ FunctionID functionId,
            /* [in] */ COR_PRF_ELT_INFO eltInfo,
            /* [out] */ COR_PRF_FRAME_INFO *pFrameInfo,
            /* [out][in] */ ULONG *pcbArgumentInfo,
            /* [size_is][out] */ COR_PRF_FUNCTION_ARGUMENT_INFO *pArgumentInfo);
        
        HRESULT ( STDMETHODCALLTYPE *GetFunctionLeave3Info )( 
            ICorProfilerInfo6 * This,
            /* [in] */ FunctionID functionId,
            /* [in] */ COR_PRF_ELT_INFO eltInfo,
            /* [out] */ COR_PRF_FRAME_INFO *pFrameInfo,
            /* [out] */ COR_PRF_FUNCTION_ARGUMENT_RANGE *pRetvalRange);
        
        HRESULT ( STDMETHODCALLTYPE *GetFunctionTailcall3Info )( 
            ICorProfilerInfo6 * This,
            /* [in] */ FunctionID functionId,
            /* [in] */ COR_PRF_ELT_INFO eltInfo,
            /* [out] */ COR_PRF_FRAME_INFO *pFrameInfo);
        
        HRESULT ( STDMETHODCALLTYPE *EnumModules )( 
            ICorProfilerInfo6 * This,
            /* [out] */ ICorProfilerModuleEnum **ppEnum);
        
        HRESULT ( STDMETHODCALLTYPE *GetRuntimeInformation )( 
            ICorProfilerInfo6 * This,
            /* [out] */ USHORT *pClrInstanceId,
            /* [out] */ COR_PRF_RUNTIME_TYPE *pRuntimeType,
            /* [out] */ USHORT *pMajorVersion,
            /* [out] */ USHORT *pMinorVersion,
            /* [out] */ USHORT *pBuildNumber,
            /* [out] */ USHORT *pQFEVersion,
            /* [in] */ ULONG cchVersionString,
            /* [out] */ ULONG *pcchVersionString,
            /* [annotation][out] */ 
            _Out_writes_to_(cchVersionString, *pcchVersionString)  WCHAR szVersionString[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *GetThreadStaticAddress2 )( 
            ICorProfilerInfo6 * This,
            /* [in] */ ClassID classId,
            /* [in] */ mdFieldDef fieldToken,
            /* [in] */ AppDomainID appDomainId,
            /* [in] */ ThreadID threadId,
            /* [out] */ void **ppAddress);
        
        HRESULT ( STDMETHODCALLTYPE *GetAppDomainsContainingModule )( 
            ICorProfilerInfo6 * This,
            /* [in] */ ModuleID moduleId,
            /* [in] */ ULONG32 cAppDomainIds,
            /* [out] */ ULONG32 *pcAppDomainIds,
            /* [length_is][size_is][out] */ AppDomainID appDomainIds[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *GetModuleInfo2 )( 
            ICorProfilerInfo6 * This,
            /* [in] */ ModuleID moduleId,
            /* [out] */ LPCBYTE *ppBaseLoadAddress,
            /* [in] */ ULONG cchName,
            /* [out] */ ULONG *pcchName,
            /* [annotation][out] */ 
            _Out_writes_to_(cchName, *pcchName)  WCHAR szName[  ],
            /* [out] */ AssemblyID *pAssemblyId,
            /* [out] */ DWORD *pdwModuleFlags);
        
        HRESULT ( STDMETHODCALLTYPE *EnumThreads )( 
            ICorProfilerInfo6 * This,
            /* [out] */ ICorProfilerThreadEnum **ppEnum);
        
        HRESULT ( STDMETHODCALLTYPE *InitializeCurrentThread )( 
            ICorProfilerInfo6 * This);
        
        HRESULT ( STDMETHODCALLTYPE *RequestReJIT )( 
            ICorProfilerInfo6 * This,
            /* [in] */ ULONG cFunctions,
            /* [size_is][in] */ ModuleID moduleIds[  ],
            /* [size_is][in] */ mdMethodDef methodIds[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *RequestRevert )( 
            ICorProfilerInfo6 * This,
            /* [in] */ ULONG cFunctions,
            /* [size_is][in] */ ModuleID moduleIds[  ],
            /* [size_is][in] */ mdMethodDef methodIds[  ],
            /* [size_is][out] */ HRESULT status[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *GetCodeInfo3 )( 
            ICorProfilerInfo6 * This,
            /* [in] */ FunctionID functionID,
            /* [in] */ ReJITID reJitId,
            /* [in] */ ULONG32 cCodeInfos,
            /* [out] */ ULONG32 *pcCodeInfos,
            /* [length_is][size_is][out] */ COR_PRF_CODE_INFO codeInfos[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *GetFunctionFromIP2 )( 
            ICorProfilerInfo6 * This,
            /* [in] */ LPCBYTE ip,
            /* [out] */ FunctionID *pFunctionId,
            /* [out] */ ReJITID *pReJitId);
        
        HRESULT ( STDMETHODCALLTYPE *GetReJITIDs )( 
            ICorProfilerInfo6 * This,
            /* [in] */ FunctionID functionId,
            /* [in] */ ULONG cReJitIds,
            /* [out] */ ULONG *pcReJitIds,
            /* [length_is][size_is][out] */ ReJITID reJitIds[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *GetILToNativeMapping2 )( 
            ICorProfilerInfo6 * This,
            /* [in] */ FunctionID functionId,
            /* [in] */ ReJITID reJitId,
            /* [in] */ ULONG32 cMap,
            /* [out] */ ULONG32 *pcMap,
            /* [length_is][size_is][out] */ COR_DEBUG_IL_TO_NATIVE_MAP map[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *EnumJITedFunctions2 )( 
            ICorProfilerInfo6 * This,
            /* [out] */ ICorProfilerFunctionEnum **ppEnum);
        
        HRESULT ( STDMETHODCALLTYPE *GetObjectSize2 )( 
            ICorProfilerInfo6 * This,
            /* [in] */ ObjectID objectId,
            /* [out] */ SIZE_T *pcSize);
        
        HRESULT ( STDMETHODCALLTYPE *GetEventMask2 )( 
            ICorProfilerInfo6 * This,
            /* [out] */ DWORD *pdwEventsLow,
            /* [out] */ DWORD *pdwEventsHigh);
        
        HRESULT ( STDMETHODCALLTYPE *SetEventMask2 )( 
            ICorProfilerInfo6 * This,
            /* [in] */ DWORD dwEventsLow,
            /* [in] */ DWORD dwEventsHigh);
        
        HRESULT ( STDMETHODCALLTYPE *EnumNgenModuleMethodsInliningThisMethod )( 
            ICorProfilerInfo6 * This,
            /* [in] */ ModuleID inlinersModuleId,
            /* [in] */ ModuleID inlineeModuleId,
            /* [in] */ mdMethodDef inlineeMethodId,
            /* [out] */ BOOL *incompleteData,
            /* [out] */ ICorProfilerMethodEnum **ppEnum);
        
        END_INTERFACE
    } ICorProfilerInfo6Vtbl;

    interface ICorProfilerInfo6
    {
        CONST_VTBL struct ICorProfilerInfo6Vtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define ICorProfilerInfo6_QueryInterface(This,riid,ppvObject)   \
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define ICorProfilerInfo6_AddRef(This)  \
    ( (This)->lpVtbl -> AddRef(This) ) 

#define ICorProfilerInfo6_Release(This) \
    ( (This)->lpVtbl -> Release(This) ) 


#define ICorProfilerInfo6_GetClassFromObject(This,objectId,pClassId)    \
    ( (This)->lpVtbl -> GetClassFromObject(This,objectId,pClassId) ) 

#define ICorProfilerInfo6_GetClassFromToken(This,moduleId,typeDef,pClassId) \
    ( (This)->lpVtbl -> GetClassFromToken(This,moduleId,typeDef,pClassId) ) 

#define ICorProfilerInfo6_GetCodeInfo(This,functionId,pStart,pcSize)    \
    ( (This)->lpVtbl -> GetCodeInfo(This,functionId,pStart,pcSize) ) 

#define ICorProfilerInfo6_GetEventMask(This,pdwEvents)  \
    ( (This)->lpVtbl -> GetEventMask(This,pdwEvents) ) 

#define ICorProfilerInfo6_GetFunctionFromIP(This,ip,pFunctionId)    \
    ( (This)->lpVtbl -> GetFunctionFromIP(This,ip,pFunctionId) ) 

#define ICorProfilerInfo6_GetFunctionFromToken(This,moduleId,token,pFunctionId) \
    ( (This)->lpVtbl -> GetFunctionFromToken(This,moduleId,token,pFunctionId) ) 

#define ICorProfilerInfo6_GetHandleFromThread(This,threadId,phThread)   \
    ( (This)->lpVtbl -> GetHandleFromThread(This,threadId,phThread) ) 

#define ICorProfilerInfo6_GetObjectSize(This,objectId,pcSize)   \
    ( (This)->lpVtbl -> GetObjectSize(This,objectId,pcSize) ) 

#define ICorProfilerInfo6_IsArrayClass(This,classId,pBaseElemType,pBaseClassId,pcRank)  \
    ( (This)->lpVtbl -> IsArrayClass(This,classId,pBaseElemType,pBaseClassId,pcRank) ) 

#define ICorProfilerInfo6_GetThreadInfo(This,threadId,pdwWin32ThreadId) \
    ( (This)->lpVtbl -> GetThreadInfo(This,threadId,pdwWin32ThreadId) ) 

#define ICorProfilerInfo6_GetCurrentThreadID(This,pThreadId)    \
    ( (This)->lpVtbl -> GetCurrentThreadID(This,pThreadId) ) 

#define ICorProfilerInfo6_GetClassIDInfo(This,classId,pModuleId,pTypeDefToken)  \
    ( (This)->lpVtbl -> GetClassIDInfo(This,classId,pModuleId,pTypeDefToken) ) 

#define ICorProfilerInfo6_GetFunctionInfo(This,functionId,pClassId,pModuleId,pToken)    \
    ( (This)->lpVtbl -> GetFunctionInfo(This,functionId,pClassId,pModuleId,pToken) ) 

#define ICorProfilerInfo6_SetEventMask(This,dwEvents)   \
    ( (This)->lpVtbl -> SetEventMask(This,dwEvents) ) 

#define ICorProfilerInfo6_SetEnterLeaveFunctionHooks(This,pFuncEnter,pFuncLeave,pFuncTailcall)  \
    ( (This)->lpVtbl -> SetEnterLeaveFunctionHooks(This,pFuncEnter,pFuncLeave,pFuncTailcall) ) 

#define ICorProfilerInfo6_SetFunctionIDMapper(This,pFunc)   \
    ( (This)->lpVtbl -> SetFunctionIDMapper(This,pFunc) ) 

#define ICorProfilerInfo6_GetTokenAndMetaDataFromFunction(This,functionId,riid,ppImport,pToken) \
    ( (This)->lpVtbl -> GetTokenAndMetaDataFromFunction(This,functionId,riid,ppImport,pToken) ) 

#define ICorProfilerInfo6_GetModuleInfo(This,moduleId,ppBaseLoadAddress,cchName,pcchName,szName,pAssemblyId)    \
    ( (This)->lpVtbl -> GetModuleInfo(This,moduleId,ppBaseLoadAddress,cchName,pcchName,szName,pAssemblyId) ) 

#define ICorProfilerInfo6_GetModuleMetaData(This,moduleId,dwOpenFlags,riid,ppOut)   \
    ( (This)->lpVtbl -> GetModuleMetaData(This,moduleId,dwOpenFlags,riid,ppOut) ) 

#define ICorProfilerInfo6_GetILFunctionBody(This,moduleId,methodId,ppMethodHeader,pcbMethodSize)    \
    ( (This)->lpVtbl -> GetILFunctionBody(This,moduleId,methodId,ppMethodHeader,pcbMethodSize) ) 

#define ICorProfilerInfo6_GetILFunctionBodyAllocator(This,moduleId,ppMalloc)    \
    ( (This)->lpVtbl -> GetILFunctionBodyAllocator(This,moduleId,ppMalloc) ) 

#define ICorProfilerInfo6_SetILFunctionBody(This,moduleId,methodid,pbNewILMethodHeader) \
    ( (This)->lpVtbl -> SetILFunctionBody(This,moduleId,methodid,pbNewILMethodHeader) ) 

#define ICorProfilerInfo6_GetAppDomainInfo(This,appDomainId,cchName,pcchName,szName,pProcessId) \
    ( (This)->lpVtbl -> GetAppDomainInfo(This,appDomainId,cchName,pcchName,szName,pProcessId) ) 

#define ICorProfilerInfo6_GetAssemblyInfo(This,assemblyId,cchName,pcchName,szName,pAppDomainId,pModuleId)   \
    ( (This)->lpVtbl -> GetAssemblyInfo(This,assemblyId,cchName,pcchName,szName,pAppDomainId,pModuleId) ) 

#define ICorProfilerInfo6_SetFunctionReJIT(This,functionId) \
    ( (This)->lpVtbl -> SetFunctionReJIT(This,functionId) ) 

#define ICorProfilerInfo6_ForceGC(This) \
    ( (This)->lpVtbl -> ForceGC(This) ) 

#define ICorProfilerInfo6_SetILInstrumentedCodeMap(This,functionId,fStartJit,cILMapEntries,rgILMapEntries)  \
    ( (This)->lpVtbl -> SetILInstrumentedCodeMap(This,functionId,fStartJit,cILMapEntries,rgILMapEntries) ) 

#define ICorProfilerInfo6_GetInprocInspectionInterface(This,ppicd)  \
    ( (This)->lpVtbl -> GetInprocInspectionInterface(This,ppicd) ) 

#define ICorProfilerInfo6_GetInprocInspectionIThisThread(This,ppicd)    \
    ( (This)->lpVtbl -> GetInprocInspectionIThisThread(This,ppicd) ) 

#define ICorProfilerInfo6_GetThreadContext(This,threadId,pContextId)    \
    ( (This)->lpVtbl -> GetThreadContext(This,threadId,pContextId) ) 

#define ICorProfilerInfo6_BeginInprocDebugging(This,fThisThreadOnly,pdwProfilerContext) \
    ( (This)->lpVtbl -> BeginInprocDebugging(This,fThisThreadOnly,pdwProfilerContext) ) 

#define ICorProfilerInfo6_EndInprocDebugging(This,dwProfilerContext)    \
    ( (This)->lpVtbl -> EndInprocDebugging(This,dwProfilerContext) ) 

#define ICorProfilerInfo6_GetILToNativeMapping(This,functionId,cMap,pcMap,map)  \
    ( (This)->lpVtbl -> GetILToNativeMapping(This,functionId,cMap,pcMap,map) ) 


#define ICorProfilerInfo6_DoStackSnapshot(This,thread,callback,infoFlags,clientData,context,contextSize)    \
    ( (This)->lpVtbl -> DoStackSnapshot(This,thread,callback,infoFlags,clientData,context,contextSize) ) 

#define ICorProfilerInfo6_SetEnterLeaveFunctionHooks2(This,pFuncEnter,pFuncLeave,pFuncTailcall) \
    ( (This)->lpVtbl -> SetEnterLeaveFunctionHooks2(This,pFuncEnter,pFuncLeave,pFuncTailcall) ) 

#define ICorProfilerInfo6_GetFunctionInfo2(This,funcId,frameInfo,pClassId,pModuleId,pToken,cTypeArgs,pcTypeArgs,typeArgs)   \
    ( (This)->lpVtbl -> GetFunctionInfo2(This,funcId,frameInfo,pClassId,pModuleId,pToken,cTypeArgs,pcTypeArgs,typeArgs) ) 

#define ICorProfilerInfo6_GetStringLayout(This,pBufferLengthOffset,pStringLengthOffset,pBufferOffset)   \
    ( (This)->lpVtbl -> GetStringLayout(This,pBufferLengthOffset,pStringLengthOffset,pBufferOffset) ) 

#define ICorProfilerInfo6_GetClassLayout(This,classID,rFieldOffset,cFieldOffset,pcFieldOffset,pulClassSize) \
    ( (This)->lpVtbl -> GetClassLayout(This,classID,rFieldOffset,cFieldOffset,pcFieldOffset,pulClassSize) ) 

#define ICorProfilerInfo6_GetClassIDInfo2(This,classId,pModuleId,pTypeDefToken,pParentClassId,cNumTypeArgs,pcNumTypeArgs,typeArgs)  \
    ( (This)->lpVtbl -> GetClassIDInfo2(This,classId,pModuleId,pTypeDefToken,pParentClassId,cNumTypeArgs,pcNumTypeArgs,typeArgs) ) 

#define ICorProfilerInfo6_GetCodeInfo2(This,functionID,cCodeInfos,pcCodeInfos,codeInfos)    \
    ( (This)->lpVtbl -> GetCodeInfo2(This,functionID,cCodeInfos,pcCodeInfos,codeInfos) ) 

#define ICorProfilerInfo6_GetClassFromTokenAndTypeArgs(This,moduleID,typeDef,cTypeArgs,typeArgs,pClassID)   \
    ( (This)->lpVtbl -> GetClassFromTokenAndTypeArgs(This,moduleID,typeDef,cTypeArgs,typeArgs,pClassID) ) 

#define ICorProfilerInfo6_GetFunctionFromTokenAndTypeArgs(This,moduleID,funcDef,classId,cTypeArgs,typeArgs,pFunctionID) \
    ( (This)->lpVtbl -> GetFunctionFromTokenAndTypeArgs(This,moduleID,funcDef,classId,cTypeArgs,typeArgs,pFunctionID) ) 

#define ICorProfilerInfo6_EnumModuleFrozenObjects(This,moduleID,ppEnum) \
    ( (This)->lpVtbl -> EnumModuleFrozenObjects(This,moduleID,ppEnum) ) 

#define ICorProfilerInfo6_GetArrayObjectInfo(This,objectId,cDimensions,pDimensionSizes,pDimensionLowerBounds,ppData)    \
    ( (This)->lpVtbl -> GetArrayObjectInfo(This,objectId,cDimensions,pDimensionSizes,pDimensionLowerBounds,ppData) ) 

#define ICorProfilerInfo6_GetBoxClassLayout(This,classId,pBufferOffset) \
    ( (This)->lpVtbl -> GetBoxClassLayout(This,classId,pBufferOffset) ) 

#define ICorProfilerInfo6_GetThreadAppDomain(This,threadId,pAppDomainId)    \
    ( (This)->lpVtbl -> GetThreadAppDomain(This,threadId,pAppDomainId) ) 

#define ICorProfilerInfo6_GetRVAStaticAddress(This,classId,fieldToken,ppAddress)    \
    ( (This)->lpVtbl -> GetRVAStaticAddress(This,classId,fieldToken,ppAddress) ) 

#define ICorProfilerInfo6_GetAppDomainStaticAddress(This,classId,fieldToken,appDomainId,ppAddress)  \
    ( (This)->lpVtbl -> GetAppDomainStaticAddress(This,classId,fieldToken,appDomainId,ppAddress) ) 

#define ICorProfilerInfo6_GetThreadStaticAddress(This,classId,fieldToken,threadId,ppAddress)    \
    ( (This)->lpVtbl -> GetThreadStaticAddress(This,classId,fieldToken,threadId,ppAddress) ) 

#define ICorProfilerInfo6_GetContextStaticAddress(This,classId,fieldToken,contextId,ppAddress)  \
    ( (This)->lpVtbl -> GetContextStaticAddress(This,classId,fieldToken,contextId,ppAddress) ) 

#define ICorProfilerInfo6_GetStaticFieldInfo(This,classId,fieldToken,pFieldInfo)    \
    ( (This)->lpVtbl -> GetStaticFieldInfo(This,classId,fieldToken,pFieldInfo) ) 

#define ICorProfilerInfo6_GetGenerationBounds(This,cObjectRanges,pcObjectRanges,ranges) \
    ( (This)->lpVtbl -> GetGenerationBounds(This,cObjectRanges,pcObjectRanges,ranges) ) 

#define ICorProfilerInfo6_GetObjectGeneration(This,objectId,range)  \
    ( (This)->lpVtbl -> GetObjectGeneration(This,objectId,range) ) 

#define ICorProfilerInfo6_GetNotifiedExceptionClauseInfo(This,pinfo)    \
    ( (This)->lpVtbl -> GetNotifiedExceptionClauseInfo(This,pinfo) ) 


#define ICorProfilerInfo6_EnumJITedFunctions(This,ppEnum)   \
    ( (This)->lpVtbl -> EnumJITedFunctions(This,ppEnum) ) 

#define ICorProfilerInfo6_RequestProfilerDetach(This,dwExpectedCompletionMilliseconds)  \
    ( (This)->lpVtbl -> RequestProfilerDetach(This,dwExpectedCompletionMilliseconds) ) 

#define ICorProfilerInfo6_SetFunctionIDMapper2(This,pFunc,clientData)   \
    ( (This)->lpVtbl -> SetFunctionIDMapper2(This,pFunc,clientData) ) 

#define ICorProfilerInfo6_GetStringLayout2(This,pStringLengthOffset,pBufferOffset)  \
    ( (This)->lpVtbl -> GetStringLayout2(This,pStringLengthOffset,pBufferOffset) ) 

#define ICorProfilerInfo6_SetEnterLeaveFunctionHooks3(This,pFuncEnter3,pFuncLeave3,pFuncTailcall3)  \
    ( (This)->lpVtbl -> SetEnterLeaveFunctionHooks3(This,pFuncEnter3,pFuncLeave3,pFuncTailcall3) ) 

#define ICorProfilerInfo6_SetEnterLeaveFunctionHooks3WithInfo(This,pFuncEnter3WithInfo,pFuncLeave3WithInfo,pFuncTailcall3WithInfo)  \
    ( (This)->lpVtbl -> SetEnterLeaveFunctionHooks3WithInfo(This,pFuncEnter3WithInfo,pFuncLeave3WithInfo,pFuncTailcall3WithInfo) ) 

#define ICorProfilerInfo6_GetFunctionEnter3Info(This,functionId,eltInfo,pFrameInfo,pcbArgumentInfo,pArgumentInfo)   \
    ( (This)->lpVtbl -> GetFunctionEnter3Info(This,functionId,eltInfo,pFrameInfo,pcbArgumentInfo,pArgumentInfo) ) 

#define ICorProfilerInfo6_GetFunctionLeave3Info(This,functionId,eltInfo,pFrameInfo,pRetvalRange)    \
    ( (This)->lpVtbl -> GetFunctionLeave3Info(This,functionId,eltInfo,pFrameInfo,pRetvalRange) ) 

#define ICorProfilerInfo6_GetFunctionTailcall3Info(This,functionId,eltInfo,pFrameInfo)  \
    ( (This)->lpVtbl -> GetFunctionTailcall3Info(This,functionId,eltInfo,pFrameInfo) ) 

#define ICorProfilerInfo6_EnumModules(This,ppEnum)  \
    ( (This)->lpVtbl -> EnumModules(This,ppEnum) ) 

#define ICorProfilerInfo6_GetRuntimeInformation(This,pClrInstanceId,pRuntimeType,pMajorVersion,pMinorVersion,pBuildNumber,pQFEVersion,cchVersionString,pcchVersionString,szVersionString)   \
    ( (This)->lpVtbl -> GetRuntimeInformation(This,pClrInstanceId,pRuntimeType,pMajorVersion,pMinorVersion,pBuildNumber,pQFEVersion,cchVersionString,pcchVersionString,szVersionString) ) 

#define ICorProfilerInfo6_GetThreadStaticAddress2(This,classId,fieldToken,appDomainId,threadId,ppAddress)   \
    ( (This)->lpVtbl -> GetThreadStaticAddress2(This,classId,fieldToken,appDomainId,threadId,ppAddress) ) 

#define ICorProfilerInfo6_GetAppDomainsContainingModule(This,moduleId,cAppDomainIds,pcAppDomainIds,appDomainIds)    \
    ( (This)->lpVtbl -> GetAppDomainsContainingModule(This,moduleId,cAppDomainIds,pcAppDomainIds,appDomainIds) ) 

#define ICorProfilerInfo6_GetModuleInfo2(This,moduleId,ppBaseLoadAddress,cchName,pcchName,szName,pAssemblyId,pdwModuleFlags)    \
    ( (This)->lpVtbl -> GetModuleInfo2(This,moduleId,ppBaseLoadAddress,cchName,pcchName,szName,pAssemblyId,pdwModuleFlags) ) 


#define ICorProfilerInfo6_EnumThreads(This,ppEnum)  \
    ( (This)->lpVtbl -> EnumThreads(This,ppEnum) ) 

#define ICorProfilerInfo6_InitializeCurrentThread(This) \
    ( (This)->lpVtbl -> InitializeCurrentThread(This) ) 

#define ICorProfilerInfo6_RequestReJIT(This,cFunctions,moduleIds,methodIds) \
    ( (This)->lpVtbl -> RequestReJIT(This,cFunctions,moduleIds,methodIds) ) 

#define ICorProfilerInfo6_RequestRevert(This,cFunctions,moduleIds,methodIds,status) \
    ( (This)->lpVtbl -> RequestRevert(This,cFunctions,moduleIds,methodIds,status) ) 

#define ICorProfilerInfo6_GetCodeInfo3(This,functionID,reJitId,cCodeInfos,pcCodeInfos,codeInfos)    \
    ( (This)->lpVtbl -> GetCodeInfo3(This,functionID,reJitId,cCodeInfos,pcCodeInfos,codeInfos) ) 

#define ICorProfilerInfo6_GetFunctionFromIP2(This,ip,pFunctionId,pReJitId)  \
    ( (This)->lpVtbl -> GetFunctionFromIP2(This,ip,pFunctionId,pReJitId) ) 

#define ICorProfilerInfo6_GetReJITIDs(This,functionId,cReJitIds,pcReJitIds,reJitIds)    \
    ( (This)->lpVtbl -> GetReJITIDs(This,functionId,cReJitIds,pcReJitIds,reJitIds) ) 

#define ICorProfilerInfo6_GetILToNativeMapping2(This,functionId,reJitId,cMap,pcMap,map) \
    ( (This)->lpVtbl -> GetILToNativeMapping2(This,functionId,reJitId,cMap,pcMap,map) ) 

#define ICorProfilerInfo6_EnumJITedFunctions2(This,ppEnum)  \
    ( (This)->lpVtbl -> EnumJITedFunctions2(This,ppEnum) ) 

#define ICorProfilerInfo6_GetObjectSize2(This,objectId,pcSize)  \
    ( (This)->lpVtbl -> GetObjectSize2(This,objectId,pcSize) ) 


#define ICorProfilerInfo6_GetEventMask2(This,pdwEventsLow,pdwEventsHigh)    \
    ( (This)->lpVtbl -> GetEventMask2(This,pdwEventsLow,pdwEventsHigh) ) 

#define ICorProfilerInfo6_SetEventMask2(This,dwEventsLow,dwEventsHigh)  \
    ( (This)->lpVtbl -> SetEventMask2(This,dwEventsLow,dwEventsHigh) ) 


#define ICorProfilerInfo6_EnumNgenModuleMethodsInliningThisMethod(This,inlinersModuleId,inlineeModuleId,inlineeMethodId,incompleteData,ppEnum)  \
    ( (This)->lpVtbl -> EnumNgenModuleMethodsInliningThisMethod(This,inlinersModuleId,inlineeModuleId,inlineeMethodId,incompleteData,ppEnum) ) 

#endif /* COBJMACROS */


#endif  /* C style interface */




#endif  /* __ICorProfilerInfo6_INTERFACE_DEFINED__ */


#ifndef __ICorProfilerInfo7_INTERFACE_DEFINED__
#define __ICorProfilerInfo7_INTERFACE_DEFINED__

/* interface ICorProfilerInfo7 */
/* [local][unique][uuid][object] */ 


EXTERN_C const IID IID_ICorProfilerInfo7;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("9AEECC0D-63E0-4187-8C00-E312F503F663")
    ICorProfilerInfo7 : public ICorProfilerInfo6
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE ApplyMetaData( 
            /* [in] */ ModuleID moduleId) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetInMemorySymbolsLength( 
            /* [in] */ ModuleID moduleId,
            /* [out] */ DWORD *pCountSymbolBytes) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE ReadInMemorySymbols( 
            /* [in] */ ModuleID moduleId,
            /* [in] */ DWORD symbolsReadOffset,
            /* [out] */ BYTE *pSymbolBytes,
            /* [in] */ DWORD countSymbolBytes,
            /* [out] */ DWORD *pCountSymbolBytesRead) = 0;
        
    };
    
    
#else   /* C style interface */

    typedef struct ICorProfilerInfo7Vtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            ICorProfilerInfo7 * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            ICorProfilerInfo7 * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            ICorProfilerInfo7 * This);
        
        HRESULT ( STDMETHODCALLTYPE *GetClassFromObject )( 
            ICorProfilerInfo7 * This,
            /* [in] */ ObjectID objectId,
            /* [out] */ ClassID *pClassId);
        
        HRESULT ( STDMETHODCALLTYPE *GetClassFromToken )( 
            ICorProfilerInfo7 * This,
            /* [in] */ ModuleID moduleId,
            /* [in] */ mdTypeDef typeDef,
            /* [out] */ ClassID *pClassId);
        
        HRESULT ( STDMETHODCALLTYPE *GetCodeInfo )( 
            ICorProfilerInfo7 * This,
            /* [in] */ FunctionID functionId,
            /* [out] */ LPCBYTE *pStart,
            /* [out] */ ULONG *pcSize);
        
        HRESULT ( STDMETHODCALLTYPE *GetEventMask )( 
            ICorProfilerInfo7 * This,
            /* [out] */ DWORD *pdwEvents);
        
        HRESULT ( STDMETHODCALLTYPE *GetFunctionFromIP )( 
            ICorProfilerInfo7 * This,
            /* [in] */ LPCBYTE ip,
            /* [out] */ FunctionID *pFunctionId);
        
        HRESULT ( STDMETHODCALLTYPE *GetFunctionFromToken )( 
            ICorProfilerInfo7 * This,
            /* [in] */ ModuleID moduleId,
            /* [in] */ mdToken token,
            /* [out] */ FunctionID *pFunctionId);
        
        HRESULT ( STDMETHODCALLTYPE *GetHandleFromThread )( 
            ICorProfilerInfo7 * This,
            /* [in] */ ThreadID threadId,
            /* [out] */ HANDLE *phThread);
        
        HRESULT ( STDMETHODCALLTYPE *GetObjectSize )( 
            ICorProfilerInfo7 * This,
            /* [in] */ ObjectID objectId,
            /* [out] */ ULONG *pcSize);
        
        HRESULT ( STDMETHODCALLTYPE *IsArrayClass )( 
            ICorProfilerInfo7 * This,
            /* [in] */ ClassID classId,
            /* [out] */ CorElementType *pBaseElemType,
            /* [out] */ ClassID *pBaseClassId,
            /* [out] */ ULONG *pcRank);
        
        HRESULT ( STDMETHODCALLTYPE *GetThreadInfo )( 
            ICorProfilerInfo7 * This,
            /* [in] */ ThreadID threadId,
            /* [out] */ DWORD *pdwWin32ThreadId);
        
        HRESULT ( STDMETHODCALLTYPE *GetCurrentThreadID )( 
            ICorProfilerInfo7 * This,
            /* [out] */ ThreadID *pThreadId);
        
        HRESULT ( STDMETHODCALLTYPE *GetClassIDInfo )( 
            ICorProfilerInfo7 * This,
            /* [in] */ ClassID classId,
            /* [out] */ ModuleID *pModuleId,
            /* [out] */ mdTypeDef *pTypeDefToken);
        
        HRESULT ( STDMETHODCALLTYPE *GetFunctionInfo )( 
            ICorProfilerInfo7 * This,
            /* [in] */ FunctionID functionId,
            /* [out] */ ClassID *pClassId,
            /* [out] */ ModuleID *pModuleId,
            /* [out] */ mdToken *pToken);
        
        HRESULT ( STDMETHODCALLTYPE *SetEventMask )( 
            ICorProfilerInfo7 * This,
            /* [in] */ DWORD dwEvents);
        
        HRESULT ( STDMETHODCALLTYPE *SetEnterLeaveFunctionHooks )( 
            ICorProfilerInfo7 * This,
            /* [in] */ FunctionEnter *pFuncEnter,
            /* [in] */ FunctionLeave *pFuncLeave,
            /* [in] */ FunctionTailcall *pFuncTailcall);
        
        HRESULT ( STDMETHODCALLTYPE *SetFunctionIDMapper )( 
            ICorProfilerInfo7 * This,
            /* [in] */ FunctionIDMapper *pFunc);
        
        HRESULT ( STDMETHODCALLTYPE *GetTokenAndMetaDataFromFunction )( 
            ICorProfilerInfo7 * This,
            /* [in] */ FunctionID functionId,
            /* [in] */ REFIID riid,
            /* [out] */ IUnknown **ppImport,
            /* [out] */ mdToken *pToken);
        
        HRESULT ( STDMETHODCALLTYPE *GetModuleInfo )( 
            ICorProfilerInfo7 * This,
            /* [in] */ ModuleID moduleId,
            /* [out] */ LPCBYTE *ppBaseLoadAddress,
            /* [in] */ ULONG cchName,
            /* [out] */ ULONG *pcchName,
            /* [annotation][out] */ 
            _Out_writes_to_(cchName, *pcchName)  WCHAR szName[  ],
            /* [out] */ AssemblyID *pAssemblyId);
        
        HRESULT ( STDMETHODCALLTYPE *GetModuleMetaData )( 
            ICorProfilerInfo7 * This,
            /* [in] */ ModuleID moduleId,
            /* [in] */ DWORD dwOpenFlags,
            /* [in] */ REFIID riid,
            /* [out] */ IUnknown **ppOut);
        
        HRESULT ( STDMETHODCALLTYPE *GetILFunctionBody )( 
            ICorProfilerInfo7 * This,
            /* [in] */ ModuleID moduleId,
            /* [in] */ mdMethodDef methodId,
            /* [out] */ LPCBYTE *ppMethodHeader,
            /* [out] */ ULONG *pcbMethodSize);
        
        HRESULT ( STDMETHODCALLTYPE *GetILFunctionBodyAllocator )( 
            ICorProfilerInfo7 * This,
            /* [in] */ ModuleID moduleId,
            /* [out] */ IMethodMalloc **ppMalloc);
        
        HRESULT ( STDMETHODCALLTYPE *SetILFunctionBody )( 
            ICorProfilerInfo7 * This,
            /* [in] */ ModuleID moduleId,
            /* [in] */ mdMethodDef methodid,
            /* [in] */ LPCBYTE pbNewILMethodHeader);
        
        HRESULT ( STDMETHODCALLTYPE *GetAppDomainInfo )( 
            ICorProfilerInfo7 * This,
            /* [in] */ AppDomainID appDomainId,
            /* [in] */ ULONG cchName,
            /* [out] */ ULONG *pcchName,
            /* [annotation][out] */ 
            _Out_writes_to_(cchName, *pcchName)  WCHAR szName[  ],
            /* [out] */ ProcessID *pProcessId);
        
        HRESULT ( STDMETHODCALLTYPE *GetAssemblyInfo )( 
            ICorProfilerInfo7 * This,
            /* [in] */ AssemblyID assemblyId,
            /* [in] */ ULONG cchName,
            /* [out] */ ULONG *pcchName,
            /* [annotation][out] */ 
            _Out_writes_to_(cchName, *pcchName)  WCHAR szName[  ],
            /* [out] */ AppDomainID *pAppDomainId,
            /* [out] */ ModuleID *pModuleId);
        
        HRESULT ( STDMETHODCALLTYPE *SetFunctionReJIT )( 
            ICorProfilerInfo7 * This,
            /* [in] */ FunctionID functionId);
        
        HRESULT ( STDMETHODCALLTYPE *ForceGC )( 
            ICorProfilerInfo7 * This);
        
        HRESULT ( STDMETHODCALLTYPE *SetILInstrumentedCodeMap )( 
            ICorProfilerInfo7 * This,
            /* [in] */ FunctionID functionId,
            /* [in] */ BOOL fStartJit,
            /* [in] */ ULONG cILMapEntries,
            /* [size_is][in] */ COR_IL_MAP rgILMapEntries[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *GetInprocInspectionInterface )( 
            ICorProfilerInfo7 * This,
            /* [out] */ IUnknown **ppicd);
        
        HRESULT ( STDMETHODCALLTYPE *GetInprocInspectionIThisThread )( 
            ICorProfilerInfo7 * This,
            /* [out] */ IUnknown **ppicd);
        
        HRESULT ( STDMETHODCALLTYPE *GetThreadContext )( 
            ICorProfilerInfo7 * This,
            /* [in] */ ThreadID threadId,
            /* [out] */ ContextID *pContextId);
        
        HRESULT ( STDMETHODCALLTYPE *BeginInprocDebugging )( 
            ICorProfilerInfo7 * This,
            /* [in] */ BOOL fThisThreadOnly,
            /* [out] */ DWORD *pdwProfilerContext);
        
        HRESULT ( STDMETHODCALLTYPE *EndInprocDebugging )( 
            ICorProfilerInfo7 * This,
            /* [in] */ DWORD dwProfilerContext);
        
        HRESULT ( STDMETHODCALLTYPE *GetILToNativeMapping )( 
            ICorProfilerInfo7 * This,
            /* [in] */ FunctionID functionId,
            /* [in] */ ULONG32 cMap,
            /* [out] */ ULONG32 *pcMap,
            /* [length_is][size_is][out] */ COR_DEBUG_IL_TO_NATIVE_MAP map[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *DoStackSnapshot )( 
            ICorProfilerInfo7 * This,
            /* [in] */ ThreadID thread,
            /* [in] */ StackSnapshotCallback *callback,
            /* [in] */ ULONG32 infoFlags,
            /* [in] */ void *clientData,
            /* [size_is][in] */ BYTE context[  ],
            /* [in] */ ULONG32 contextSize);
        
        HRESULT ( STDMETHODCALLTYPE *SetEnterLeaveFunctionHooks2 )( 
            ICorProfilerInfo7 * This,
            /* [in] */ FunctionEnter2 *pFuncEnter,
            /* [in] */ FunctionLeave2 *pFuncLeave,
            /* [in] */ FunctionTailcall2 *pFuncTailcall);
        
        HRESULT ( STDMETHODCALLTYPE *GetFunctionInfo2 )( 
            ICorProfilerInfo7 * This,
            /* [in] */ FunctionID funcId,
            /* [in] */ COR_PRF_FRAME_INFO frameInfo,
            /* [out] */ ClassID *pClassId,
            /* [out] */ ModuleID *pModuleId,
            /* [out] */ mdToken *pToken,
            /* [in] */ ULONG32 cTypeArgs,
            /* [out] */ ULONG32 *pcTypeArgs,
            /* [out] */ ClassID typeArgs[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *GetStringLayout )( 
            ICorProfilerInfo7 * This,
            /* [out] */ ULONG *pBufferLengthOffset,
            /* [out] */ ULONG *pStringLengthOffset,
            /* [out] */ ULONG *pBufferOffset);
        
        HRESULT ( STDMETHODCALLTYPE *GetClassLayout )( 
            ICorProfilerInfo7 * This,
            /* [in] */ ClassID classID,
            /* [out][in] */ COR_FIELD_OFFSET rFieldOffset[  ],
            /* [in] */ ULONG cFieldOffset,
            /* [out] */ ULONG *pcFieldOffset,
            /* [out] */ ULONG *pulClassSize);
        
        HRESULT ( STDMETHODCALLTYPE *GetClassIDInfo2 )( 
            ICorProfilerInfo7 * This,
            /* [in] */ ClassID classId,
            /* [out] */ ModuleID *pModuleId,
            /* [out] */ mdTypeDef *pTypeDefToken,
            /* [out] */ ClassID *pParentClassId,
            /* [in] */ ULONG32 cNumTypeArgs,
            /* [out] */ ULONG32 *pcNumTypeArgs,
            /* [out] */ ClassID typeArgs[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *GetCodeInfo2 )( 
            ICorProfilerInfo7 * This,
            /* [in] */ FunctionID functionID,
            /* [in] */ ULONG32 cCodeInfos,
            /* [out] */ ULONG32 *pcCodeInfos,
            /* [length_is][size_is][out] */ COR_PRF_CODE_INFO codeInfos[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *GetClassFromTokenAndTypeArgs )( 
            ICorProfilerInfo7 * This,
            /* [in] */ ModuleID moduleID,
            /* [in] */ mdTypeDef typeDef,
            /* [in] */ ULONG32 cTypeArgs,
            /* [size_is][in] */ ClassID typeArgs[  ],
            /* [out] */ ClassID *pClassID);
        
        HRESULT ( STDMETHODCALLTYPE *GetFunctionFromTokenAndTypeArgs )( 
            ICorProfilerInfo7 * This,
            /* [in] */ ModuleID moduleID,
            /* [in] */ mdMethodDef funcDef,
            /* [in] */ ClassID classId,
            /* [in] */ ULONG32 cTypeArgs,
            /* [size_is][in] */ ClassID typeArgs[  ],
            /* [out] */ FunctionID *pFunctionID);
        
        HRESULT ( STDMETHODCALLTYPE *EnumModuleFrozenObjects )( 
            ICorProfilerInfo7 * This,
            /* [in] */ ModuleID moduleID,
            /* [out] */ ICorProfilerObjectEnum **ppEnum);
        
        HRESULT ( STDMETHODCALLTYPE *GetArrayObjectInfo )( 
            ICorProfilerInfo7 * This,
            /* [in] */ ObjectID objectId,
            /* [in] */ ULONG32 cDimensions,
            /* [size_is][out] */ ULONG32 pDimensionSizes[  ],
            /* [size_is][out] */ int pDimensionLowerBounds[  ],
            /* [out] */ BYTE **ppData);
        
        HRESULT ( STDMETHODCALLTYPE *GetBoxClassLayout )( 
            ICorProfilerInfo7 * This,
            /* [in] */ ClassID classId,
            /* [out] */ ULONG32 *pBufferOffset);
        
        HRESULT ( STDMETHODCALLTYPE *GetThreadAppDomain )( 
            ICorProfilerInfo7 * This,
            /* [in] */ ThreadID threadId,
            /* [out] */ AppDomainID *pAppDomainId);
        
        HRESULT ( STDMETHODCALLTYPE *GetRVAStaticAddress )( 
            ICorProfilerInfo7 * This,
            /* [in] */ ClassID classId,
            /* [in] */ mdFieldDef fieldToken,
            /* [out] */ void **ppAddress);
        
        HRESULT ( STDMETHODCALLTYPE *GetAppDomainStaticAddress )( 
            ICorProfilerInfo7 * This,
            /* [in] */ ClassID classId,
            /* [in] */ mdFieldDef fieldToken,
            /* [in] */ AppDomainID appDomainId,
            /* [out] */ void **ppAddress);
        
        HRESULT ( STDMETHODCALLTYPE *GetThreadStaticAddress )( 
            ICorProfilerInfo7 * This,
            /* [in] */ ClassID classId,
            /* [in] */ mdFieldDef fieldToken,
            /* [in] */ ThreadID threadId,
            /* [out] */ void **ppAddress);
        
        HRESULT ( STDMETHODCALLTYPE *GetContextStaticAddress )( 
            ICorProfilerInfo7 * This,
            /* [in] */ ClassID classId,
            /* [in] */ mdFieldDef fieldToken,
            /* [in] */ ContextID contextId,
            /* [out] */ void **ppAddress);
        
        HRESULT ( STDMETHODCALLTYPE *GetStaticFieldInfo )( 
            ICorProfilerInfo7 * This,
            /* [in] */ ClassID classId,
            /* [in] */ mdFieldDef fieldToken,
            /* [out] */ COR_PRF_STATIC_TYPE *pFieldInfo);
        
        HRESULT ( STDMETHODCALLTYPE *GetGenerationBounds )( 
            ICorProfilerInfo7 * This,
            /* [in] */ ULONG cObjectRanges,
            /* [out] */ ULONG *pcObjectRanges,
            /* [length_is][size_is][out] */ COR_PRF_GC_GENERATION_RANGE ranges[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *GetObjectGeneration )( 
            ICorProfilerInfo7 * This,
            /* [in] */ ObjectID objectId,
            /* [out] */ COR_PRF_GC_GENERATION_RANGE *range);
        
        HRESULT ( STDMETHODCALLTYPE *GetNotifiedExceptionClauseInfo )( 
            ICorProfilerInfo7 * This,
            /* [out] */ COR_PRF_EX_CLAUSE_INFO *pinfo);
        
        HRESULT ( STDMETHODCALLTYPE *EnumJITedFunctions )( 
            ICorProfilerInfo7 * This,
            /* [out] */ ICorProfilerFunctionEnum **ppEnum);
        
        HRESULT ( STDMETHODCALLTYPE *RequestProfilerDetach )( 
            ICorProfilerInfo7 * This,
            /* [in] */ DWORD dwExpectedCompletionMilliseconds);
        
        HRESULT ( STDMETHODCALLTYPE *SetFunctionIDMapper2 )( 
            ICorProfilerInfo7 * This,
            /* [in] */ FunctionIDMapper2 *pFunc,
            /* [in] */ void *clientData);
        
        HRESULT ( STDMETHODCALLTYPE *GetStringLayout2 )( 
            ICorProfilerInfo7 * This,
            /* [out] */ ULONG *pStringLengthOffset,
            /* [out] */ ULONG *pBufferOffset);
        
        HRESULT ( STDMETHODCALLTYPE *SetEnterLeaveFunctionHooks3 )( 
            ICorProfilerInfo7 * This,
            /* [in] */ FunctionEnter3 *pFuncEnter3,
            /* [in] */ FunctionLeave3 *pFuncLeave3,
            /* [in] */ FunctionTailcall3 *pFuncTailcall3);
        
        HRESULT ( STDMETHODCALLTYPE *SetEnterLeaveFunctionHooks3WithInfo )( 
            ICorProfilerInfo7 * This,
            /* [in] */ FunctionEnter3WithInfo *pFuncEnter3WithInfo,
            /* [in] */ FunctionLeave3WithInfo *pFuncLeave3WithInfo,
            /* [in] */ FunctionTailcall3WithInfo *pFuncTailcall3WithInfo);
        
        HRESULT ( STDMETHODCALLTYPE *GetFunctionEnter3Info )( 
            ICorProfilerInfo7 * This,
            /* [in] */ FunctionID functionId,
            /* [in] */ COR_PRF_ELT_INFO eltInfo,
            /* [out] */ COR_PRF_FRAME_INFO *pFrameInfo,
            /* [out][in] */ ULONG *pcbArgumentInfo,
            /* [size_is][out] */ COR_PRF_FUNCTION_ARGUMENT_INFO *pArgumentInfo);
        
        HRESULT ( STDMETHODCALLTYPE *GetFunctionLeave3Info )( 
            ICorProfilerInfo7 * This,
            /* [in] */ FunctionID functionId,
            /* [in] */ COR_PRF_ELT_INFO eltInfo,
            /* [out] */ COR_PRF_FRAME_INFO *pFrameInfo,
            /* [out] */ COR_PRF_FUNCTION_ARGUMENT_RANGE *pRetvalRange);
        
        HRESULT ( STDMETHODCALLTYPE *GetFunctionTailcall3Info )( 
            ICorProfilerInfo7 * This,
            /* [in] */ FunctionID functionId,
            /* [in] */ COR_PRF_ELT_INFO eltInfo,
            /* [out] */ COR_PRF_FRAME_INFO *pFrameInfo);
        
        HRESULT ( STDMETHODCALLTYPE *EnumModules )( 
            ICorProfilerInfo7 * This,
            /* [out] */ ICorProfilerModuleEnum **ppEnum);
        
        HRESULT ( STDMETHODCALLTYPE *GetRuntimeInformation )( 
            ICorProfilerInfo7 * This,
            /* [out] */ USHORT *pClrInstanceId,
            /* [out] */ COR_PRF_RUNTIME_TYPE *pRuntimeType,
            /* [out] */ USHORT *pMajorVersion,
            /* [out] */ USHORT *pMinorVersion,
            /* [out] */ USHORT *pBuildNumber,
            /* [out] */ USHORT *pQFEVersion,
            /* [in] */ ULONG cchVersionString,
            /* [out] */ ULONG *pcchVersionString,
            /* [annotation][out] */ 
            _Out_writes_to_(cchVersionString, *pcchVersionString)  WCHAR szVersionString[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *GetThreadStaticAddress2 )( 
            ICorProfilerInfo7 * This,
            /* [in] */ ClassID classId,
            /* [in] */ mdFieldDef fieldToken,
            /* [in] */ AppDomainID appDomainId,
            /* [in] */ ThreadID threadId,
            /* [out] */ void **ppAddress);
        
        HRESULT ( STDMETHODCALLTYPE *GetAppDomainsContainingModule )( 
            ICorProfilerInfo7 * This,
            /* [in] */ ModuleID moduleId,
            /* [in] */ ULONG32 cAppDomainIds,
            /* [out] */ ULONG32 *pcAppDomainIds,
            /* [length_is][size_is][out] */ AppDomainID appDomainIds[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *GetModuleInfo2 )( 
            ICorProfilerInfo7 * This,
            /* [in] */ ModuleID moduleId,
            /* [out] */ LPCBYTE *ppBaseLoadAddress,
            /* [in] */ ULONG cchName,
            /* [out] */ ULONG *pcchName,
            /* [annotation][out] */ 
            _Out_writes_to_(cchName, *pcchName)  WCHAR szName[  ],
            /* [out] */ AssemblyID *pAssemblyId,
            /* [out] */ DWORD *pdwModuleFlags);
        
        HRESULT ( STDMETHODCALLTYPE *EnumThreads )( 
            ICorProfilerInfo7 * This,
            /* [out] */ ICorProfilerThreadEnum **ppEnum);
        
        HRESULT ( STDMETHODCALLTYPE *InitializeCurrentThread )( 
            ICorProfilerInfo7 * This);
        
        HRESULT ( STDMETHODCALLTYPE *RequestReJIT )( 
            ICorProfilerInfo7 * This,
            /* [in] */ ULONG cFunctions,
            /* [size_is][in] */ ModuleID moduleIds[  ],
            /* [size_is][in] */ mdMethodDef methodIds[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *RequestRevert )( 
            ICorProfilerInfo7 * This,
            /* [in] */ ULONG cFunctions,
            /* [size_is][in] */ ModuleID moduleIds[  ],
            /* [size_is][in] */ mdMethodDef methodIds[  ],
            /* [size_is][out] */ HRESULT status[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *GetCodeInfo3 )( 
            ICorProfilerInfo7 * This,
            /* [in] */ FunctionID functionID,
            /* [in] */ ReJITID reJitId,
            /* [in] */ ULONG32 cCodeInfos,
            /* [out] */ ULONG32 *pcCodeInfos,
            /* [length_is][size_is][out] */ COR_PRF_CODE_INFO codeInfos[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *GetFunctionFromIP2 )( 
            ICorProfilerInfo7 * This,
            /* [in] */ LPCBYTE ip,
            /* [out] */ FunctionID *pFunctionId,
            /* [out] */ ReJITID *pReJitId);
        
        HRESULT ( STDMETHODCALLTYPE *GetReJITIDs )( 
            ICorProfilerInfo7 * This,
            /* [in] */ FunctionID functionId,
            /* [in] */ ULONG cReJitIds,
            /* [out] */ ULONG *pcReJitIds,
            /* [length_is][size_is][out] */ ReJITID reJitIds[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *GetILToNativeMapping2 )( 
            ICorProfilerInfo7 * This,
            /* [in] */ FunctionID functionId,
            /* [in] */ ReJITID reJitId,
            /* [in] */ ULONG32 cMap,
            /* [out] */ ULONG32 *pcMap,
            /* [length_is][size_is][out] */ COR_DEBUG_IL_TO_NATIVE_MAP map[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *EnumJITedFunctions2 )( 
            ICorProfilerInfo7 * This,
            /* [out] */ ICorProfilerFunctionEnum **ppEnum);
        
        HRESULT ( STDMETHODCALLTYPE *GetObjectSize2 )( 
            ICorProfilerInfo7 * This,
            /* [in] */ ObjectID objectId,
            /* [out] */ SIZE_T *pcSize);
        
        HRESULT ( STDMETHODCALLTYPE *GetEventMask2 )( 
            ICorProfilerInfo7 * This,
            /* [out] */ DWORD *pdwEventsLow,
            /* [out] */ DWORD *pdwEventsHigh);
        
        HRESULT ( STDMETHODCALLTYPE *SetEventMask2 )( 
            ICorProfilerInfo7 * This,
            /* [in] */ DWORD dwEventsLow,
            /* [in] */ DWORD dwEventsHigh);
        
        HRESULT ( STDMETHODCALLTYPE *EnumNgenModuleMethodsInliningThisMethod )( 
            ICorProfilerInfo7 * This,
            /* [in] */ ModuleID inlinersModuleId,
            /* [in] */ ModuleID inlineeModuleId,
            /* [in] */ mdMethodDef inlineeMethodId,
            /* [out] */ BOOL *incompleteData,
            /* [out] */ ICorProfilerMethodEnum **ppEnum);
        
        HRESULT ( STDMETHODCALLTYPE *ApplyMetaData )( 
            ICorProfilerInfo7 * This,
            /* [in] */ ModuleID moduleId);
        
        HRESULT ( STDMETHODCALLTYPE *GetInMemorySymbolsLength )( 
            ICorProfilerInfo7 * This,
            /* [in] */ ModuleID moduleId,
            /* [out] */ DWORD *pCountSymbolBytes);
        
        HRESULT ( STDMETHODCALLTYPE *ReadInMemorySymbols )( 
            ICorProfilerInfo7 * This,
            /* [in] */ ModuleID moduleId,
            /* [in] */ DWORD symbolsReadOffset,
            /* [out] */ BYTE *pSymbolBytes,
            /* [in] */ DWORD countSymbolBytes,
            /* [out] */ DWORD *pCountSymbolBytesRead);
        
        END_INTERFACE
    } ICorProfilerInfo7Vtbl;

    interface ICorProfilerInfo7
    {
        CONST_VTBL struct ICorProfilerInfo7Vtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define ICorProfilerInfo7_QueryInterface(This,riid,ppvObject)   \
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define ICorProfilerInfo7_AddRef(This)  \
    ( (This)->lpVtbl -> AddRef(This) ) 

#define ICorProfilerInfo7_Release(This) \
    ( (This)->lpVtbl -> Release(This) ) 


#define ICorProfilerInfo7_GetClassFromObject(This,objectId,pClassId)    \
    ( (This)->lpVtbl -> GetClassFromObject(This,objectId,pClassId) ) 

#define ICorProfilerInfo7_GetClassFromToken(This,moduleId,typeDef,pClassId) \
    ( (This)->lpVtbl -> GetClassFromToken(This,moduleId,typeDef,pClassId) ) 

#define ICorProfilerInfo7_GetCodeInfo(This,functionId,pStart,pcSize)    \
    ( (This)->lpVtbl -> GetCodeInfo(This,functionId,pStart,pcSize) ) 

#define ICorProfilerInfo7_GetEventMask(This,pdwEvents)  \
    ( (This)->lpVtbl -> GetEventMask(This,pdwEvents) ) 

#define ICorProfilerInfo7_GetFunctionFromIP(This,ip,pFunctionId)    \
    ( (This)->lpVtbl -> GetFunctionFromIP(This,ip,pFunctionId) ) 

#define ICorProfilerInfo7_GetFunctionFromToken(This,moduleId,token,pFunctionId) \
    ( (This)->lpVtbl -> GetFunctionFromToken(This,moduleId,token,pFunctionId) ) 

#define ICorProfilerInfo7_GetHandleFromThread(This,threadId,phThread)   \
    ( (This)->lpVtbl -> GetHandleFromThread(This,threadId,phThread) ) 

#define ICorProfilerInfo7_GetObjectSize(This,objectId,pcSize)   \
    ( (This)->lpVtbl -> GetObjectSize(This,objectId,pcSize) ) 

#define ICorProfilerInfo7_IsArrayClass(This,classId,pBaseElemType,pBaseClassId,pcRank)  \
    ( (This)->lpVtbl -> IsArrayClass(This,classId,pBaseElemType,pBaseClassId,pcRank) ) 

#define ICorProfilerInfo7_GetThreadInfo(This,threadId,pdwWin32ThreadId) \
    ( (This)->lpVtbl -> GetThreadInfo(This,threadId,pdwWin32ThreadId) ) 

#define ICorProfilerInfo7_GetCurrentThreadID(This,pThreadId)    \
    ( (This)->lpVtbl -> GetCurrentThreadID(This,pThreadId) ) 

#define ICorProfilerInfo7_GetClassIDInfo(This,classId,pModuleId,pTypeDefToken)  \
    ( (This)->lpVtbl -> GetClassIDInfo(This,classId,pModuleId,pTypeDefToken) ) 

#define ICorProfilerInfo7_GetFunctionInfo(This,functionId,pClassId,pModuleId,pToken)    \
    ( (This)->lpVtbl -> GetFunctionInfo(This,functionId,pClassId,pModuleId,pToken) ) 

#define ICorProfilerInfo7_SetEventMask(This,dwEvents)   \
    ( (This)->lpVtbl -> SetEventMask(This,dwEvents) ) 

#define ICorProfilerInfo7_SetEnterLeaveFunctionHooks(This,pFuncEnter,pFuncLeave,pFuncTailcall)  \
    ( (This)->lpVtbl -> SetEnterLeaveFunctionHooks(This,pFuncEnter,pFuncLeave,pFuncTailcall) ) 

#define ICorProfilerInfo7_SetFunctionIDMapper(This,pFunc)   \
    ( (This)->lpVtbl -> SetFunctionIDMapper(This,pFunc) ) 

#define ICorProfilerInfo7_GetTokenAndMetaDataFromFunction(This,functionId,riid,ppImport,pToken) \
    ( (This)->lpVtbl -> GetTokenAndMetaDataFromFunction(This,functionId,riid,ppImport,pToken) ) 

#define ICorProfilerInfo7_GetModuleInfo(This,moduleId,ppBaseLoadAddress,cchName,pcchName,szName,pAssemblyId)    \
    ( (This)->lpVtbl -> GetModuleInfo(This,moduleId,ppBaseLoadAddress,cchName,pcchName,szName,pAssemblyId) ) 

#define ICorProfilerInfo7_GetModuleMetaData(This,moduleId,dwOpenFlags,riid,ppOut)   \
    ( (This)->lpVtbl -> GetModuleMetaData(This,moduleId,dwOpenFlags,riid,ppOut) ) 

#define ICorProfilerInfo7_GetILFunctionBody(This,moduleId,methodId,ppMethodHeader,pcbMethodSize)    \
    ( (This)->lpVtbl -> GetILFunctionBody(This,moduleId,methodId,ppMethodHeader,pcbMethodSize) ) 

#define ICorProfilerInfo7_GetILFunctionBodyAllocator(This,moduleId,ppMalloc)    \
    ( (This)->lpVtbl -> GetILFunctionBodyAllocator(This,moduleId,ppMalloc) ) 

#define ICorProfilerInfo7_SetILFunctionBody(This,moduleId,methodid,pbNewILMethodHeader) \
    ( (This)->lpVtbl -> SetILFunctionBody(This,moduleId,methodid,pbNewILMethodHeader) ) 

#define ICorProfilerInfo7_GetAppDomainInfo(This,appDomainId,cchName,pcchName,szName,pProcessId) \
    ( (This)->lpVtbl -> GetAppDomainInfo(This,appDomainId,cchName,pcchName,szName,pProcessId) ) 

#define ICorProfilerInfo7_GetAssemblyInfo(This,assemblyId,cchName,pcchName,szName,pAppDomainId,pModuleId)   \
    ( (This)->lpVtbl -> GetAssemblyInfo(This,assemblyId,cchName,pcchName,szName,pAppDomainId,pModuleId) ) 

#define ICorProfilerInfo7_SetFunctionReJIT(This,functionId) \
    ( (This)->lpVtbl -> SetFunctionReJIT(This,functionId) ) 

#define ICorProfilerInfo7_ForceGC(This) \
    ( (This)->lpVtbl -> ForceGC(This) ) 

#define ICorProfilerInfo7_SetILInstrumentedCodeMap(This,functionId,fStartJit,cILMapEntries,rgILMapEntries)  \
    ( (This)->lpVtbl -> SetILInstrumentedCodeMap(This,functionId,fStartJit,cILMapEntries,rgILMapEntries) ) 

#define ICorProfilerInfo7_GetInprocInspectionInterface(This,ppicd)  \
    ( (This)->lpVtbl -> GetInprocInspectionInterface(This,ppicd) ) 

#define ICorProfilerInfo7_GetInprocInspectionIThisThread(This,ppicd)    \
    ( (This)->lpVtbl -> GetInprocInspectionIThisThread(This,ppicd) ) 

#define ICorProfilerInfo7_GetThreadContext(This,threadId,pContextId)    \
    ( (This)->lpVtbl -> GetThreadContext(This,threadId,pContextId) ) 

#define ICorProfilerInfo7_BeginInprocDebugging(This,fThisThreadOnly,pdwProfilerContext) \
    ( (This)->lpVtbl -> BeginInprocDebugging(This,fThisThreadOnly,pdwProfilerContext) ) 

#define ICorProfilerInfo7_EndInprocDebugging(This,dwProfilerContext)    \
    ( (This)->lpVtbl -> EndInprocDebugging(This,dwProfilerContext) ) 

#define ICorProfilerInfo7_GetILToNativeMapping(This,functionId,cMap,pcMap,map)  \
    ( (This)->lpVtbl -> GetILToNativeMapping(This,functionId,cMap,pcMap,map) ) 


#define ICorProfilerInfo7_DoStackSnapshot(This,thread,callback,infoFlags,clientData,context,contextSize)    \
    ( (This)->lpVtbl -> DoStackSnapshot(This,thread,callback,infoFlags,clientData,context,contextSize) ) 

#define ICorProfilerInfo7_SetEnterLeaveFunctionHooks2(This,pFuncEnter,pFuncLeave,pFuncTailcall) \
    ( (This)->lpVtbl -> SetEnterLeaveFunctionHooks2(This,pFuncEnter,pFuncLeave,pFuncTailcall) ) 

#define ICorProfilerInfo7_GetFunctionInfo2(This,funcId,frameInfo,pClassId,pModuleId,pToken,cTypeArgs,pcTypeArgs,typeArgs)   \
    ( (This)->lpVtbl -> GetFunctionInfo2(This,funcId,frameInfo,pClassId,pModuleId,pToken,cTypeArgs,pcTypeArgs,typeArgs) ) 

#define ICorProfilerInfo7_GetStringLayout(This,pBufferLengthOffset,pStringLengthOffset,pBufferOffset)   \
    ( (This)->lpVtbl -> GetStringLayout(This,pBufferLengthOffset,pStringLengthOffset,pBufferOffset) ) 

#define ICorProfilerInfo7_GetClassLayout(This,classID,rFieldOffset,cFieldOffset,pcFieldOffset,pulClassSize) \
    ( (This)->lpVtbl -> GetClassLayout(This,classID,rFieldOffset,cFieldOffset,pcFieldOffset,pulClassSize) ) 

#define ICorProfilerInfo7_GetClassIDInfo2(This,classId,pModuleId,pTypeDefToken,pParentClassId,cNumTypeArgs,pcNumTypeArgs,typeArgs)  \
    ( (This)->lpVtbl -> GetClassIDInfo2(This,classId,pModuleId,pTypeDefToken,pParentClassId,cNumTypeArgs,pcNumTypeArgs,typeArgs) ) 

#define ICorProfilerInfo7_GetCodeInfo2(This,functionID,cCodeInfos,pcCodeInfos,codeInfos)    \
    ( (This)->lpVtbl -> GetCodeInfo2(This,functionID,cCodeInfos,pcCodeInfos,codeInfos) ) 

#define ICorProfilerInfo7_GetClassFromTokenAndTypeArgs(This,moduleID,typeDef,cTypeArgs,typeArgs,pClassID)   \
    ( (This)->lpVtbl -> GetClassFromTokenAndTypeArgs(This,moduleID,typeDef,cTypeArgs,typeArgs,pClassID) ) 

#define ICorProfilerInfo7_GetFunctionFromTokenAndTypeArgs(This,moduleID,funcDef,classId,cTypeArgs,typeArgs,pFunctionID) \
    ( (This)->lpVtbl -> GetFunctionFromTokenAndTypeArgs(This,moduleID,funcDef,classId,cTypeArgs,typeArgs,pFunctionID) ) 

#define ICorProfilerInfo7_EnumModuleFrozenObjects(This,moduleID,ppEnum) \
    ( (This)->lpVtbl -> EnumModuleFrozenObjects(This,moduleID,ppEnum) ) 

#define ICorProfilerInfo7_GetArrayObjectInfo(This,objectId,cDimensions,pDimensionSizes,pDimensionLowerBounds,ppData)    \
    ( (This)->lpVtbl -> GetArrayObjectInfo(This,objectId,cDimensions,pDimensionSizes,pDimensionLowerBounds,ppData) ) 

#define ICorProfilerInfo7_GetBoxClassLayout(This,classId,pBufferOffset) \
    ( (This)->lpVtbl -> GetBoxClassLayout(This,classId,pBufferOffset) ) 

#define ICorProfilerInfo7_GetThreadAppDomain(This,threadId,pAppDomainId)    \
    ( (This)->lpVtbl -> GetThreadAppDomain(This,threadId,pAppDomainId) ) 

#define ICorProfilerInfo7_GetRVAStaticAddress(This,classId,fieldToken,ppAddress)    \
    ( (This)->lpVtbl -> GetRVAStaticAddress(This,classId,fieldToken,ppAddress) ) 

#define ICorProfilerInfo7_GetAppDomainStaticAddress(This,classId,fieldToken,appDomainId,ppAddress)  \
    ( (This)->lpVtbl -> GetAppDomainStaticAddress(This,classId,fieldToken,appDomainId,ppAddress) ) 

#define ICorProfilerInfo7_GetThreadStaticAddress(This,classId,fieldToken,threadId,ppAddress)    \
    ( (This)->lpVtbl -> GetThreadStaticAddress(This,classId,fieldToken,threadId,ppAddress) ) 

#define ICorProfilerInfo7_GetContextStaticAddress(This,classId,fieldToken,contextId,ppAddress)  \
    ( (This)->lpVtbl -> GetContextStaticAddress(This,classId,fieldToken,contextId,ppAddress) ) 

#define ICorProfilerInfo7_GetStaticFieldInfo(This,classId,fieldToken,pFieldInfo)    \
    ( (This)->lpVtbl -> GetStaticFieldInfo(This,classId,fieldToken,pFieldInfo) ) 

#define ICorProfilerInfo7_GetGenerationBounds(This,cObjectRanges,pcObjectRanges,ranges) \
    ( (This)->lpVtbl -> GetGenerationBounds(This,cObjectRanges,pcObjectRanges,ranges) ) 

#define ICorProfilerInfo7_GetObjectGeneration(This,objectId,range)  \
    ( (This)->lpVtbl -> GetObjectGeneration(This,objectId,range) ) 

#define ICorProfilerInfo7_GetNotifiedExceptionClauseInfo(This,pinfo)    \
    ( (This)->lpVtbl -> GetNotifiedExceptionClauseInfo(This,pinfo) ) 


#define ICorProfilerInfo7_EnumJITedFunctions(This,ppEnum)   \
    ( (This)->lpVtbl -> EnumJITedFunctions(This,ppEnum) ) 

#define ICorProfilerInfo7_RequestProfilerDetach(This,dwExpectedCompletionMilliseconds)  \
    ( (This)->lpVtbl -> RequestProfilerDetach(This,dwExpectedCompletionMilliseconds) ) 

#define ICorProfilerInfo7_SetFunctionIDMapper2(This,pFunc,clientData)   \
    ( (This)->lpVtbl -> SetFunctionIDMapper2(This,pFunc,clientData) ) 

#define ICorProfilerInfo7_GetStringLayout2(This,pStringLengthOffset,pBufferOffset)  \
    ( (This)->lpVtbl -> GetStringLayout2(This,pStringLengthOffset,pBufferOffset) ) 

#define ICorProfilerInfo7_SetEnterLeaveFunctionHooks3(This,pFuncEnter3,pFuncLeave3,pFuncTailcall3)  \
    ( (This)->lpVtbl -> SetEnterLeaveFunctionHooks3(This,pFuncEnter3,pFuncLeave3,pFuncTailcall3) ) 

#define ICorProfilerInfo7_SetEnterLeaveFunctionHooks3WithInfo(This,pFuncEnter3WithInfo,pFuncLeave3WithInfo,pFuncTailcall3WithInfo)  \
    ( (This)->lpVtbl -> SetEnterLeaveFunctionHooks3WithInfo(This,pFuncEnter3WithInfo,pFuncLeave3WithInfo,pFuncTailcall3WithInfo) ) 

#define ICorProfilerInfo7_GetFunctionEnter3Info(This,functionId,eltInfo,pFrameInfo,pcbArgumentInfo,pArgumentInfo)   \
    ( (This)->lpVtbl -> GetFunctionEnter3Info(This,functionId,eltInfo,pFrameInfo,pcbArgumentInfo,pArgumentInfo) ) 

#define ICorProfilerInfo7_GetFunctionLeave3Info(This,functionId,eltInfo,pFrameInfo,pRetvalRange)    \
    ( (This)->lpVtbl -> GetFunctionLeave3Info(This,functionId,eltInfo,pFrameInfo,pRetvalRange) ) 

#define ICorProfilerInfo7_GetFunctionTailcall3Info(This,functionId,eltInfo,pFrameInfo)  \
    ( (This)->lpVtbl -> GetFunctionTailcall3Info(This,functionId,eltInfo,pFrameInfo) ) 

#define ICorProfilerInfo7_EnumModules(This,ppEnum)  \
    ( (This)->lpVtbl -> EnumModules(This,ppEnum) ) 

#define ICorProfilerInfo7_GetRuntimeInformation(This,pClrInstanceId,pRuntimeType,pMajorVersion,pMinorVersion,pBuildNumber,pQFEVersion,cchVersionString,pcchVersionString,szVersionString)   \
    ( (This)->lpVtbl -> GetRuntimeInformation(This,pClrInstanceId,pRuntimeType,pMajorVersion,pMinorVersion,pBuildNumber,pQFEVersion,cchVersionString,pcchVersionString,szVersionString) ) 

#define ICorProfilerInfo7_GetThreadStaticAddress2(This,classId,fieldToken,appDomainId,threadId,ppAddress)   \
    ( (This)->lpVtbl -> GetThreadStaticAddress2(This,classId,fieldToken,appDomainId,threadId,ppAddress) ) 

#define ICorProfilerInfo7_GetAppDomainsContainingModule(This,moduleId,cAppDomainIds,pcAppDomainIds,appDomainIds)    \
    ( (This)->lpVtbl -> GetAppDomainsContainingModule(This,moduleId,cAppDomainIds,pcAppDomainIds,appDomainIds) ) 

#define ICorProfilerInfo7_GetModuleInfo2(This,moduleId,ppBaseLoadAddress,cchName,pcchName,szName,pAssemblyId,pdwModuleFlags)    \
    ( (This)->lpVtbl -> GetModuleInfo2(This,moduleId,ppBaseLoadAddress,cchName,pcchName,szName,pAssemblyId,pdwModuleFlags) ) 


#define ICorProfilerInfo7_EnumThreads(This,ppEnum)  \
    ( (This)->lpVtbl -> EnumThreads(This,ppEnum) ) 

#define ICorProfilerInfo7_InitializeCurrentThread(This) \
    ( (This)->lpVtbl -> InitializeCurrentThread(This) ) 

#define ICorProfilerInfo7_RequestReJIT(This,cFunctions,moduleIds,methodIds) \
    ( (This)->lpVtbl -> RequestReJIT(This,cFunctions,moduleIds,methodIds) ) 

#define ICorProfilerInfo7_RequestRevert(This,cFunctions,moduleIds,methodIds,status) \
    ( (This)->lpVtbl -> RequestRevert(This,cFunctions,moduleIds,methodIds,status) ) 

#define ICorProfilerInfo7_GetCodeInfo3(This,functionID,reJitId,cCodeInfos,pcCodeInfos,codeInfos)    \
    ( (This)->lpVtbl -> GetCodeInfo3(This,functionID,reJitId,cCodeInfos,pcCodeInfos,codeInfos) ) 

#define ICorProfilerInfo7_GetFunctionFromIP2(This,ip,pFunctionId,pReJitId)  \
    ( (This)->lpVtbl -> GetFunctionFromIP2(This,ip,pFunctionId,pReJitId) ) 

#define ICorProfilerInfo7_GetReJITIDs(This,functionId,cReJitIds,pcReJitIds,reJitIds)    \
    ( (This)->lpVtbl -> GetReJITIDs(This,functionId,cReJitIds,pcReJitIds,reJitIds) ) 

#define ICorProfilerInfo7_GetILToNativeMapping2(This,functionId,reJitId,cMap,pcMap,map) \
    ( (This)->lpVtbl -> GetILToNativeMapping2(This,functionId,reJitId,cMap,pcMap,map) ) 

#define ICorProfilerInfo7_EnumJITedFunctions2(This,ppEnum)  \
    ( (This)->lpVtbl -> EnumJITedFunctions2(This,ppEnum) ) 

#define ICorProfilerInfo7_GetObjectSize2(This,objectId,pcSize)  \
    ( (This)->lpVtbl -> GetObjectSize2(This,objectId,pcSize) ) 


#define ICorProfilerInfo7_GetEventMask2(This,pdwEventsLow,pdwEventsHigh)    \
    ( (This)->lpVtbl -> GetEventMask2(This,pdwEventsLow,pdwEventsHigh) ) 

#define ICorProfilerInfo7_SetEventMask2(This,dwEventsLow,dwEventsHigh)  \
    ( (This)->lpVtbl -> SetEventMask2(This,dwEventsLow,dwEventsHigh) ) 


#define ICorProfilerInfo7_EnumNgenModuleMethodsInliningThisMethod(This,inlinersModuleId,inlineeModuleId,inlineeMethodId,incompleteData,ppEnum)  \
    ( (This)->lpVtbl -> EnumNgenModuleMethodsInliningThisMethod(This,inlinersModuleId,inlineeModuleId,inlineeMethodId,incompleteData,ppEnum) ) 


#define ICorProfilerInfo7_ApplyMetaData(This,moduleId)  \
    ( (This)->lpVtbl -> ApplyMetaData(This,moduleId) ) 

#define ICorProfilerInfo7_GetInMemorySymbolsLength(This,moduleId,pCountSymbolBytes) \
    ( (This)->lpVtbl -> GetInMemorySymbolsLength(This,moduleId,pCountSymbolBytes) ) 

#define ICorProfilerInfo7_ReadInMemorySymbols(This,moduleId,symbolsReadOffset,pSymbolBytes,countSymbolBytes,pCountSymbolBytesRead)  \
    ( (This)->lpVtbl -> ReadInMemorySymbols(This,moduleId,symbolsReadOffset,pSymbolBytes,countSymbolBytes,pCountSymbolBytesRead) ) 

#endif /* COBJMACROS */


#endif  /* C style interface */




#endif  /* __ICorProfilerInfo7_INTERFACE_DEFINED__ */


#ifndef __ICorProfilerInfo8_INTERFACE_DEFINED__
#define __ICorProfilerInfo8_INTERFACE_DEFINED__

/* interface ICorProfilerInfo8 */
/* [local][unique][uuid][object] */ 


EXTERN_C const IID IID_ICorProfilerInfo8;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("C5AC80A6-782E-4716-8044-39598C60CFBF")
    ICorProfilerInfo8 : public ICorProfilerInfo7
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE IsFunctionDynamic( 
            /* [in] */ FunctionID functionId,
            /* [out] */ BOOL *isDynamic) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetFunctionFromIP3( 
            /* [in] */ LPCBYTE ip,
            /* [out] */ FunctionID *functionId,
            /* [out] */ ReJITID *pReJitId) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetDynamicFunctionInfo( 
            /* [in] */ FunctionID functionId,
            /* [out] */ ModuleID *moduleId,
            /* [out] */ PCCOR_SIGNATURE *ppvSig,
            /* [out] */ ULONG *pbSig,
            /* [in] */ ULONG cchName,
            /* [out] */ ULONG *pcchName,
            /* [out] */ WCHAR wszName[  ]) = 0;
        
    };
    
    
#else   /* C style interface */

    typedef struct ICorProfilerInfo8Vtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            ICorProfilerInfo8 * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            ICorProfilerInfo8 * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            ICorProfilerInfo8 * This);
        
        HRESULT ( STDMETHODCALLTYPE *GetClassFromObject )( 
            ICorProfilerInfo8 * This,
            /* [in] */ ObjectID objectId,
            /* [out] */ ClassID *pClassId);
        
        HRESULT ( STDMETHODCALLTYPE *GetClassFromToken )( 
            ICorProfilerInfo8 * This,
            /* [in] */ ModuleID moduleId,
            /* [in] */ mdTypeDef typeDef,
            /* [out] */ ClassID *pClassId);
        
        HRESULT ( STDMETHODCALLTYPE *GetCodeInfo )( 
            ICorProfilerInfo8 * This,
            /* [in] */ FunctionID functionId,
            /* [out] */ LPCBYTE *pStart,
            /* [out] */ ULONG *pcSize);
        
        HRESULT ( STDMETHODCALLTYPE *GetEventMask )( 
            ICorProfilerInfo8 * This,
            /* [out] */ DWORD *pdwEvents);
        
        HRESULT ( STDMETHODCALLTYPE *GetFunctionFromIP )( 
            ICorProfilerInfo8 * This,
            /* [in] */ LPCBYTE ip,
            /* [out] */ FunctionID *pFunctionId);
        
        HRESULT ( STDMETHODCALLTYPE *GetFunctionFromToken )( 
            ICorProfilerInfo8 * This,
            /* [in] */ ModuleID moduleId,
            /* [in] */ mdToken token,
            /* [out] */ FunctionID *pFunctionId);
        
        HRESULT ( STDMETHODCALLTYPE *GetHandleFromThread )( 
            ICorProfilerInfo8 * This,
            /* [in] */ ThreadID threadId,
            /* [out] */ HANDLE *phThread);
        
        HRESULT ( STDMETHODCALLTYPE *GetObjectSize )( 
            ICorProfilerInfo8 * This,
            /* [in] */ ObjectID objectId,
            /* [out] */ ULONG *pcSize);
        
        HRESULT ( STDMETHODCALLTYPE *IsArrayClass )( 
            ICorProfilerInfo8 * This,
            /* [in] */ ClassID classId,
            /* [out] */ CorElementType *pBaseElemType,
            /* [out] */ ClassID *pBaseClassId,
            /* [out] */ ULONG *pcRank);
        
        HRESULT ( STDMETHODCALLTYPE *GetThreadInfo )( 
            ICorProfilerInfo8 * This,
            /* [in] */ ThreadID threadId,
            /* [out] */ DWORD *pdwWin32ThreadId);
        
        HRESULT ( STDMETHODCALLTYPE *GetCurrentThreadID )( 
            ICorProfilerInfo8 * This,
            /* [out] */ ThreadID *pThreadId);
        
        HRESULT ( STDMETHODCALLTYPE *GetClassIDInfo )( 
            ICorProfilerInfo8 * This,
            /* [in] */ ClassID classId,
            /* [out] */ ModuleID *pModuleId,
            /* [out] */ mdTypeDef *pTypeDefToken);
        
        HRESULT ( STDMETHODCALLTYPE *GetFunctionInfo )( 
            ICorProfilerInfo8 * This,
            /* [in] */ FunctionID functionId,
            /* [out] */ ClassID *pClassId,
            /* [out] */ ModuleID *pModuleId,
            /* [out] */ mdToken *pToken);
        
        HRESULT ( STDMETHODCALLTYPE *SetEventMask )( 
            ICorProfilerInfo8 * This,
            /* [in] */ DWORD dwEvents);
        
        HRESULT ( STDMETHODCALLTYPE *SetEnterLeaveFunctionHooks )( 
            ICorProfilerInfo8 * This,
            /* [in] */ FunctionEnter *pFuncEnter,
            /* [in] */ FunctionLeave *pFuncLeave,
            /* [in] */ FunctionTailcall *pFuncTailcall);
        
        HRESULT ( STDMETHODCALLTYPE *SetFunctionIDMapper )( 
            ICorProfilerInfo8 * This,
            /* [in] */ FunctionIDMapper *pFunc);
        
        HRESULT ( STDMETHODCALLTYPE *GetTokenAndMetaDataFromFunction )( 
            ICorProfilerInfo8 * This,
            /* [in] */ FunctionID functionId,
            /* [in] */ REFIID riid,
            /* [out] */ IUnknown **ppImport,
            /* [out] */ mdToken *pToken);
        
        HRESULT ( STDMETHODCALLTYPE *GetModuleInfo )( 
            ICorProfilerInfo8 * This,
            /* [in] */ ModuleID moduleId,
            /* [out] */ LPCBYTE *ppBaseLoadAddress,
            /* [in] */ ULONG cchName,
            /* [out] */ ULONG *pcchName,
            /* [annotation][out] */ 
            _Out_writes_to_(cchName, *pcchName)  WCHAR szName[  ],
            /* [out] */ AssemblyID *pAssemblyId);
        
        HRESULT ( STDMETHODCALLTYPE *GetModuleMetaData )( 
            ICorProfilerInfo8 * This,
            /* [in] */ ModuleID moduleId,
            /* [in] */ DWORD dwOpenFlags,
            /* [in] */ REFIID riid,
            /* [out] */ IUnknown **ppOut);
        
        HRESULT ( STDMETHODCALLTYPE *GetILFunctionBody )( 
            ICorProfilerInfo8 * This,
            /* [in] */ ModuleID moduleId,
            /* [in] */ mdMethodDef methodId,
            /* [out] */ LPCBYTE *ppMethodHeader,
            /* [out] */ ULONG *pcbMethodSize);
        
        HRESULT ( STDMETHODCALLTYPE *GetILFunctionBodyAllocator )( 
            ICorProfilerInfo8 * This,
            /* [in] */ ModuleID moduleId,
            /* [out] */ IMethodMalloc **ppMalloc);
        
        HRESULT ( STDMETHODCALLTYPE *SetILFunctionBody )( 
            ICorProfilerInfo8 * This,
            /* [in] */ ModuleID moduleId,
            /* [in] */ mdMethodDef methodid,
            /* [in] */ LPCBYTE pbNewILMethodHeader);
        
        HRESULT ( STDMETHODCALLTYPE *GetAppDomainInfo )( 
            ICorProfilerInfo8 * This,
            /* [in] */ AppDomainID appDomainId,
            /* [in] */ ULONG cchName,
            /* [out] */ ULONG *pcchName,
            /* [annotation][out] */ 
            _Out_writes_to_(cchName, *pcchName)  WCHAR szName[  ],
            /* [out] */ ProcessID *pProcessId);
        
        HRESULT ( STDMETHODCALLTYPE *GetAssemblyInfo )( 
            ICorProfilerInfo8 * This,
            /* [in] */ AssemblyID assemblyId,
            /* [in] */ ULONG cchName,
            /* [out] */ ULONG *pcchName,
            /* [annotation][out] */ 
            _Out_writes_to_(cchName, *pcchName)  WCHAR szName[  ],
            /* [out] */ AppDomainID *pAppDomainId,
            /* [out] */ ModuleID *pModuleId);
        
        HRESULT ( STDMETHODCALLTYPE *SetFunctionReJIT )( 
            ICorProfilerInfo8 * This,
            /* [in] */ FunctionID functionId);
        
        HRESULT ( STDMETHODCALLTYPE *ForceGC )( 
            ICorProfilerInfo8 * This);
        
        HRESULT ( STDMETHODCALLTYPE *SetILInstrumentedCodeMap )( 
            ICorProfilerInfo8 * This,
            /* [in] */ FunctionID functionId,
            /* [in] */ BOOL fStartJit,
            /* [in] */ ULONG cILMapEntries,
            /* [size_is][in] */ COR_IL_MAP rgILMapEntries[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *GetInprocInspectionInterface )( 
            ICorProfilerInfo8 * This,
            /* [out] */ IUnknown **ppicd);
        
        HRESULT ( STDMETHODCALLTYPE *GetInprocInspectionIThisThread )( 
            ICorProfilerInfo8 * This,
            /* [out] */ IUnknown **ppicd);
        
        HRESULT ( STDMETHODCALLTYPE *GetThreadContext )( 
            ICorProfilerInfo8 * This,
            /* [in] */ ThreadID threadId,
            /* [out] */ ContextID *pContextId);
        
        HRESULT ( STDMETHODCALLTYPE *BeginInprocDebugging )( 
            ICorProfilerInfo8 * This,
            /* [in] */ BOOL fThisThreadOnly,
            /* [out] */ DWORD *pdwProfilerContext);
        
        HRESULT ( STDMETHODCALLTYPE *EndInprocDebugging )( 
            ICorProfilerInfo8 * This,
            /* [in] */ DWORD dwProfilerContext);
        
        HRESULT ( STDMETHODCALLTYPE *GetILToNativeMapping )( 
            ICorProfilerInfo8 * This,
            /* [in] */ FunctionID functionId,
            /* [in] */ ULONG32 cMap,
            /* [out] */ ULONG32 *pcMap,
            /* [length_is][size_is][out] */ COR_DEBUG_IL_TO_NATIVE_MAP map[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *DoStackSnapshot )( 
            ICorProfilerInfo8 * This,
            /* [in] */ ThreadID thread,
            /* [in] */ StackSnapshotCallback *callback,
            /* [in] */ ULONG32 infoFlags,
            /* [in] */ void *clientData,
            /* [size_is][in] */ BYTE context[  ],
            /* [in] */ ULONG32 contextSize);
        
        HRESULT ( STDMETHODCALLTYPE *SetEnterLeaveFunctionHooks2 )( 
            ICorProfilerInfo8 * This,
            /* [in] */ FunctionEnter2 *pFuncEnter,
            /* [in] */ FunctionLeave2 *pFuncLeave,
            /* [in] */ FunctionTailcall2 *pFuncTailcall);
        
        HRESULT ( STDMETHODCALLTYPE *GetFunctionInfo2 )( 
            ICorProfilerInfo8 * This,
            /* [in] */ FunctionID funcId,
            /* [in] */ COR_PRF_FRAME_INFO frameInfo,
            /* [out] */ ClassID *pClassId,
            /* [out] */ ModuleID *pModuleId,
            /* [out] */ mdToken *pToken,
            /* [in] */ ULONG32 cTypeArgs,
            /* [out] */ ULONG32 *pcTypeArgs,
            /* [out] */ ClassID typeArgs[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *GetStringLayout )( 
            ICorProfilerInfo8 * This,
            /* [out] */ ULONG *pBufferLengthOffset,
            /* [out] */ ULONG *pStringLengthOffset,
            /* [out] */ ULONG *pBufferOffset);
        
        HRESULT ( STDMETHODCALLTYPE *GetClassLayout )( 
            ICorProfilerInfo8 * This,
            /* [in] */ ClassID classID,
            /* [out][in] */ COR_FIELD_OFFSET rFieldOffset[  ],
            /* [in] */ ULONG cFieldOffset,
            /* [out] */ ULONG *pcFieldOffset,
            /* [out] */ ULONG *pulClassSize);
        
        HRESULT ( STDMETHODCALLTYPE *GetClassIDInfo2 )( 
            ICorProfilerInfo8 * This,
            /* [in] */ ClassID classId,
            /* [out] */ ModuleID *pModuleId,
            /* [out] */ mdTypeDef *pTypeDefToken,
            /* [out] */ ClassID *pParentClassId,
            /* [in] */ ULONG32 cNumTypeArgs,
            /* [out] */ ULONG32 *pcNumTypeArgs,
            /* [out] */ ClassID typeArgs[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *GetCodeInfo2 )( 
            ICorProfilerInfo8 * This,
            /* [in] */ FunctionID functionID,
            /* [in] */ ULONG32 cCodeInfos,
            /* [out] */ ULONG32 *pcCodeInfos,
            /* [length_is][size_is][out] */ COR_PRF_CODE_INFO codeInfos[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *GetClassFromTokenAndTypeArgs )( 
            ICorProfilerInfo8 * This,
            /* [in] */ ModuleID moduleID,
            /* [in] */ mdTypeDef typeDef,
            /* [in] */ ULONG32 cTypeArgs,
            /* [size_is][in] */ ClassID typeArgs[  ],
            /* [out] */ ClassID *pClassID);
        
        HRESULT ( STDMETHODCALLTYPE *GetFunctionFromTokenAndTypeArgs )( 
            ICorProfilerInfo8 * This,
            /* [in] */ ModuleID moduleID,
            /* [in] */ mdMethodDef funcDef,
            /* [in] */ ClassID classId,
            /* [in] */ ULONG32 cTypeArgs,
            /* [size_is][in] */ ClassID typeArgs[  ],
            /* [out] */ FunctionID *pFunctionID);
        
        HRESULT ( STDMETHODCALLTYPE *EnumModuleFrozenObjects )( 
            ICorProfilerInfo8 * This,
            /* [in] */ ModuleID moduleID,
            /* [out] */ ICorProfilerObjectEnum **ppEnum);
        
        HRESULT ( STDMETHODCALLTYPE *GetArrayObjectInfo )( 
            ICorProfilerInfo8 * This,
            /* [in] */ ObjectID objectId,
            /* [in] */ ULONG32 cDimensions,
            /* [size_is][out] */ ULONG32 pDimensionSizes[  ],
            /* [size_is][out] */ int pDimensionLowerBounds[  ],
            /* [out] */ BYTE **ppData);
        
        HRESULT ( STDMETHODCALLTYPE *GetBoxClassLayout )( 
            ICorProfilerInfo8 * This,
            /* [in] */ ClassID classId,
            /* [out] */ ULONG32 *pBufferOffset);
        
        HRESULT ( STDMETHODCALLTYPE *GetThreadAppDomain )( 
            ICorProfilerInfo8 * This,
            /* [in] */ ThreadID threadId,
            /* [out] */ AppDomainID *pAppDomainId);
        
        HRESULT ( STDMETHODCALLTYPE *GetRVAStaticAddress )( 
            ICorProfilerInfo8 * This,
            /* [in] */ ClassID classId,
            /* [in] */ mdFieldDef fieldToken,
            /* [out] */ void **ppAddress);
        
        HRESULT ( STDMETHODCALLTYPE *GetAppDomainStaticAddress )( 
            ICorProfilerInfo8 * This,
            /* [in] */ ClassID classId,
            /* [in] */ mdFieldDef fieldToken,
            /* [in] */ AppDomainID appDomainId,
            /* [out] */ void **ppAddress);
        
        HRESULT ( STDMETHODCALLTYPE *GetThreadStaticAddress )( 
            ICorProfilerInfo8 * This,
            /* [in] */ ClassID classId,
            /* [in] */ mdFieldDef fieldToken,
            /* [in] */ ThreadID threadId,
            /* [out] */ void **ppAddress);
        
        HRESULT ( STDMETHODCALLTYPE *GetContextStaticAddress )( 
            ICorProfilerInfo8 * This,
            /* [in] */ ClassID classId,
            /* [in] */ mdFieldDef fieldToken,
            /* [in] */ ContextID contextId,
            /* [out] */ void **ppAddress);
        
        HRESULT ( STDMETHODCALLTYPE *GetStaticFieldInfo )( 
            ICorProfilerInfo8 * This,
            /* [in] */ ClassID classId,
            /* [in] */ mdFieldDef fieldToken,
            /* [out] */ COR_PRF_STATIC_TYPE *pFieldInfo);
        
        HRESULT ( STDMETHODCALLTYPE *GetGenerationBounds )( 
            ICorProfilerInfo8 * This,
            /* [in] */ ULONG cObjectRanges,
            /* [out] */ ULONG *pcObjectRanges,
            /* [length_is][size_is][out] */ COR_PRF_GC_GENERATION_RANGE ranges[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *GetObjectGeneration )( 
            ICorProfilerInfo8 * This,
            /* [in] */ ObjectID objectId,
            /* [out] */ COR_PRF_GC_GENERATION_RANGE *range);
        
        HRESULT ( STDMETHODCALLTYPE *GetNotifiedExceptionClauseInfo )( 
            ICorProfilerInfo8 * This,
            /* [out] */ COR_PRF_EX_CLAUSE_INFO *pinfo);
        
        HRESULT ( STDMETHODCALLTYPE *EnumJITedFunctions )( 
            ICorProfilerInfo8 * This,
            /* [out] */ ICorProfilerFunctionEnum **ppEnum);
        
        HRESULT ( STDMETHODCALLTYPE *RequestProfilerDetach )( 
            ICorProfilerInfo8 * This,
            /* [in] */ DWORD dwExpectedCompletionMilliseconds);
        
        HRESULT ( STDMETHODCALLTYPE *SetFunctionIDMapper2 )( 
            ICorProfilerInfo8 * This,
            /* [in] */ FunctionIDMapper2 *pFunc,
            /* [in] */ void *clientData);
        
        HRESULT ( STDMETHODCALLTYPE *GetStringLayout2 )( 
            ICorProfilerInfo8 * This,
            /* [out] */ ULONG *pStringLengthOffset,
            /* [out] */ ULONG *pBufferOffset);
        
        HRESULT ( STDMETHODCALLTYPE *SetEnterLeaveFunctionHooks3 )( 
            ICorProfilerInfo8 * This,
            /* [in] */ FunctionEnter3 *pFuncEnter3,
            /* [in] */ FunctionLeave3 *pFuncLeave3,
            /* [in] */ FunctionTailcall3 *pFuncTailcall3);
        
        HRESULT ( STDMETHODCALLTYPE *SetEnterLeaveFunctionHooks3WithInfo )( 
            ICorProfilerInfo8 * This,
            /* [in] */ FunctionEnter3WithInfo *pFuncEnter3WithInfo,
            /* [in] */ FunctionLeave3WithInfo *pFuncLeave3WithInfo,
            /* [in] */ FunctionTailcall3WithInfo *pFuncTailcall3WithInfo);
        
        HRESULT ( STDMETHODCALLTYPE *GetFunctionEnter3Info )( 
            ICorProfilerInfo8 * This,
            /* [in] */ FunctionID functionId,
            /* [in] */ COR_PRF_ELT_INFO eltInfo,
            /* [out] */ COR_PRF_FRAME_INFO *pFrameInfo,
            /* [out][in] */ ULONG *pcbArgumentInfo,
            /* [size_is][out] */ COR_PRF_FUNCTION_ARGUMENT_INFO *pArgumentInfo);
        
        HRESULT ( STDMETHODCALLTYPE *GetFunctionLeave3Info )( 
            ICorProfilerInfo8 * This,
            /* [in] */ FunctionID functionId,
            /* [in] */ COR_PRF_ELT_INFO eltInfo,
            /* [out] */ COR_PRF_FRAME_INFO *pFrameInfo,
            /* [out] */ COR_PRF_FUNCTION_ARGUMENT_RANGE *pRetvalRange);
        
        HRESULT ( STDMETHODCALLTYPE *GetFunctionTailcall3Info )( 
            ICorProfilerInfo8 * This,
            /* [in] */ FunctionID functionId,
            /* [in] */ COR_PRF_ELT_INFO eltInfo,
            /* [out] */ COR_PRF_FRAME_INFO *pFrameInfo);
        
        HRESULT ( STDMETHODCALLTYPE *EnumModules )( 
            ICorProfilerInfo8 * This,
            /* [out] */ ICorProfilerModuleEnum **ppEnum);
        
        HRESULT ( STDMETHODCALLTYPE *GetRuntimeInformation )( 
            ICorProfilerInfo8 * This,
            /* [out] */ USHORT *pClrInstanceId,
            /* [out] */ COR_PRF_RUNTIME_TYPE *pRuntimeType,
            /* [out] */ USHORT *pMajorVersion,
            /* [out] */ USHORT *pMinorVersion,
            /* [out] */ USHORT *pBuildNumber,
            /* [out] */ USHORT *pQFEVersion,
            /* [in] */ ULONG cchVersionString,
            /* [out] */ ULONG *pcchVersionString,
            /* [annotation][out] */ 
            _Out_writes_to_(cchVersionString, *pcchVersionString)  WCHAR szVersionString[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *GetThreadStaticAddress2 )( 
            ICorProfilerInfo8 * This,
            /* [in] */ ClassID classId,
            /* [in] */ mdFieldDef fieldToken,
            /* [in] */ AppDomainID appDomainId,
            /* [in] */ ThreadID threadId,
            /* [out] */ void **ppAddress);
        
        HRESULT ( STDMETHODCALLTYPE *GetAppDomainsContainingModule )( 
            ICorProfilerInfo8 * This,
            /* [in] */ ModuleID moduleId,
            /* [in] */ ULONG32 cAppDomainIds,
            /* [out] */ ULONG32 *pcAppDomainIds,
            /* [length_is][size_is][out] */ AppDomainID appDomainIds[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *GetModuleInfo2 )( 
            ICorProfilerInfo8 * This,
            /* [in] */ ModuleID moduleId,
            /* [out] */ LPCBYTE *ppBaseLoadAddress,
            /* [in] */ ULONG cchName,
            /* [out] */ ULONG *pcchName,
            /* [annotation][out] */ 
            _Out_writes_to_(cchName, *pcchName)  WCHAR szName[  ],
            /* [out] */ AssemblyID *pAssemblyId,
            /* [out] */ DWORD *pdwModuleFlags);
        
        HRESULT ( STDMETHODCALLTYPE *EnumThreads )( 
            ICorProfilerInfo8 * This,
            /* [out] */ ICorProfilerThreadEnum **ppEnum);
        
        HRESULT ( STDMETHODCALLTYPE *InitializeCurrentThread )( 
            ICorProfilerInfo8 * This);
        
        HRESULT ( STDMETHODCALLTYPE *RequestReJIT )( 
            ICorProfilerInfo8 * This,
            /* [in] */ ULONG cFunctions,
            /* [size_is][in] */ ModuleID moduleIds[  ],
            /* [size_is][in] */ mdMethodDef methodIds[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *RequestRevert )( 
            ICorProfilerInfo8 * This,
            /* [in] */ ULONG cFunctions,
            /* [size_is][in] */ ModuleID moduleIds[  ],
            /* [size_is][in] */ mdMethodDef methodIds[  ],
            /* [size_is][out] */ HRESULT status[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *GetCodeInfo3 )( 
            ICorProfilerInfo8 * This,
            /* [in] */ FunctionID functionID,
            /* [in] */ ReJITID reJitId,
            /* [in] */ ULONG32 cCodeInfos,
            /* [out] */ ULONG32 *pcCodeInfos,
            /* [length_is][size_is][out] */ COR_PRF_CODE_INFO codeInfos[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *GetFunctionFromIP2 )( 
            ICorProfilerInfo8 * This,
            /* [in] */ LPCBYTE ip,
            /* [out] */ FunctionID *pFunctionId,
            /* [out] */ ReJITID *pReJitId);
        
        HRESULT ( STDMETHODCALLTYPE *GetReJITIDs )( 
            ICorProfilerInfo8 * This,
            /* [in] */ FunctionID functionId,
            /* [in] */ ULONG cReJitIds,
            /* [out] */ ULONG *pcReJitIds,
            /* [length_is][size_is][out] */ ReJITID reJitIds[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *GetILToNativeMapping2 )( 
            ICorProfilerInfo8 * This,
            /* [in] */ FunctionID functionId,
            /* [in] */ ReJITID reJitId,
            /* [in] */ ULONG32 cMap,
            /* [out] */ ULONG32 *pcMap,
            /* [length_is][size_is][out] */ COR_DEBUG_IL_TO_NATIVE_MAP map[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *EnumJITedFunctions2 )( 
            ICorProfilerInfo8 * This,
            /* [out] */ ICorProfilerFunctionEnum **ppEnum);
        
        HRESULT ( STDMETHODCALLTYPE *GetObjectSize2 )( 
            ICorProfilerInfo8 * This,
            /* [in] */ ObjectID objectId,
            /* [out] */ SIZE_T *pcSize);
        
        HRESULT ( STDMETHODCALLTYPE *GetEventMask2 )( 
            ICorProfilerInfo8 * This,
            /* [out] */ DWORD *pdwEventsLow,
            /* [out] */ DWORD *pdwEventsHigh);
        
        HRESULT ( STDMETHODCALLTYPE *SetEventMask2 )( 
            ICorProfilerInfo8 * This,
            /* [in] */ DWORD dwEventsLow,
            /* [in] */ DWORD dwEventsHigh);
        
        HRESULT ( STDMETHODCALLTYPE *EnumNgenModuleMethodsInliningThisMethod )( 
            ICorProfilerInfo8 * This,
            /* [in] */ ModuleID inlinersModuleId,
            /* [in] */ ModuleID inlineeModuleId,
            /* [in] */ mdMethodDef inlineeMethodId,
            /* [out] */ BOOL *incompleteData,
            /* [out] */ ICorProfilerMethodEnum **ppEnum);
        
        HRESULT ( STDMETHODCALLTYPE *ApplyMetaData )( 
            ICorProfilerInfo8 * This,
            /* [in] */ ModuleID moduleId);
        
        HRESULT ( STDMETHODCALLTYPE *GetInMemorySymbolsLength )( 
            ICorProfilerInfo8 * This,
            /* [in] */ ModuleID moduleId,
            /* [out] */ DWORD *pCountSymbolBytes);
        
        HRESULT ( STDMETHODCALLTYPE *ReadInMemorySymbols )( 
            ICorProfilerInfo8 * This,
            /* [in] */ ModuleID moduleId,
            /* [in] */ DWORD symbolsReadOffset,
            /* [out] */ BYTE *pSymbolBytes,
            /* [in] */ DWORD countSymbolBytes,
            /* [out] */ DWORD *pCountSymbolBytesRead);
        
        HRESULT ( STDMETHODCALLTYPE *IsFunctionDynamic )( 
            ICorProfilerInfo8 * This,
            /* [in] */ FunctionID functionId,
            /* [out] */ BOOL *isDynamic);
        
        HRESULT ( STDMETHODCALLTYPE *GetFunctionFromIP3 )( 
            ICorProfilerInfo8 * This,
            /* [in] */ LPCBYTE ip,
            /* [out] */ FunctionID *functionId,
            /* [out] */ ReJITID *pReJitId);
        
        HRESULT ( STDMETHODCALLTYPE *GetDynamicFunctionInfo )( 
            ICorProfilerInfo8 * This,
            /* [in] */ FunctionID functionId,
            /* [out] */ ModuleID *moduleId,
            /* [out] */ PCCOR_SIGNATURE *ppvSig,
            /* [out] */ ULONG *pbSig,
            /* [in] */ ULONG cchName,
            /* [out] */ ULONG *pcchName,
            /* [out] */ WCHAR wszName[  ]);
        
        END_INTERFACE
    } ICorProfilerInfo8Vtbl;

    interface ICorProfilerInfo8
    {
        CONST_VTBL struct ICorProfilerInfo8Vtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define ICorProfilerInfo8_QueryInterface(This,riid,ppvObject)   \
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define ICorProfilerInfo8_AddRef(This)  \
    ( (This)->lpVtbl -> AddRef(This) ) 

#define ICorProfilerInfo8_Release(This) \
    ( (This)->lpVtbl -> Release(This) ) 


#define ICorProfilerInfo8_GetClassFromObject(This,objectId,pClassId)    \
    ( (This)->lpVtbl -> GetClassFromObject(This,objectId,pClassId) ) 

#define ICorProfilerInfo8_GetClassFromToken(This,moduleId,typeDef,pClassId) \
    ( (This)->lpVtbl -> GetClassFromToken(This,moduleId,typeDef,pClassId) ) 

#define ICorProfilerInfo8_GetCodeInfo(This,functionId,pStart,pcSize)    \
    ( (This)->lpVtbl -> GetCodeInfo(This,functionId,pStart,pcSize) ) 

#define ICorProfilerInfo8_GetEventMask(This,pdwEvents)  \
    ( (This)->lpVtbl -> GetEventMask(This,pdwEvents) ) 

#define ICorProfilerInfo8_GetFunctionFromIP(This,ip,pFunctionId)    \
    ( (This)->lpVtbl -> GetFunctionFromIP(This,ip,pFunctionId) ) 

#define ICorProfilerInfo8_GetFunctionFromToken(This,moduleId,token,pFunctionId) \
    ( (This)->lpVtbl -> GetFunctionFromToken(This,moduleId,token,pFunctionId) ) 

#define ICorProfilerInfo8_GetHandleFromThread(This,threadId,phThread)   \
    ( (This)->lpVtbl -> GetHandleFromThread(This,threadId,phThread) ) 

#define ICorProfilerInfo8_GetObjectSize(This,objectId,pcSize)   \
    ( (This)->lpVtbl -> GetObjectSize(This,objectId,pcSize) ) 

#define ICorProfilerInfo8_IsArrayClass(This,classId,pBaseElemType,pBaseClassId,pcRank)  \
    ( (This)->lpVtbl -> IsArrayClass(This,classId,pBaseElemType,pBaseClassId,pcRank) ) 

#define ICorProfilerInfo8_GetThreadInfo(This,threadId,pdwWin32ThreadId) \
    ( (This)->lpVtbl -> GetThreadInfo(This,threadId,pdwWin32ThreadId) ) 

#define ICorProfilerInfo8_GetCurrentThreadID(This,pThreadId)    \
    ( (This)->lpVtbl -> GetCurrentThreadID(This,pThreadId) ) 

#define ICorProfilerInfo8_GetClassIDInfo(This,classId,pModuleId,pTypeDefToken)  \
    ( (This)->lpVtbl -> GetClassIDInfo(This,classId,pModuleId,pTypeDefToken) ) 

#define ICorProfilerInfo8_GetFunctionInfo(This,functionId,pClassId,pModuleId,pToken)    \
    ( (This)->lpVtbl -> GetFunctionInfo(This,functionId,pClassId,pModuleId,pToken) ) 

#define ICorProfilerInfo8_SetEventMask(This,dwEvents)   \
    ( (This)->lpVtbl -> SetEventMask(This,dwEvents) ) 

#define ICorProfilerInfo8_SetEnterLeaveFunctionHooks(This,pFuncEnter,pFuncLeave,pFuncTailcall)  \
    ( (This)->lpVtbl -> SetEnterLeaveFunctionHooks(This,pFuncEnter,pFuncLeave,pFuncTailcall) ) 

#define ICorProfilerInfo8_SetFunctionIDMapper(This,pFunc)   \
    ( (This)->lpVtbl -> SetFunctionIDMapper(This,pFunc) ) 

#define ICorProfilerInfo8_GetTokenAndMetaDataFromFunction(This,functionId,riid,ppImport,pToken) \
    ( (This)->lpVtbl -> GetTokenAndMetaDataFromFunction(This,functionId,riid,ppImport,pToken) ) 

#define ICorProfilerInfo8_GetModuleInfo(This,moduleId,ppBaseLoadAddress,cchName,pcchName,szName,pAssemblyId)    \
    ( (This)->lpVtbl -> GetModuleInfo(This,moduleId,ppBaseLoadAddress,cchName,pcchName,szName,pAssemblyId) ) 

#define ICorProfilerInfo8_GetModuleMetaData(This,moduleId,dwOpenFlags,riid,ppOut)   \
    ( (This)->lpVtbl -> GetModuleMetaData(This,moduleId,dwOpenFlags,riid,ppOut) ) 

#define ICorProfilerInfo8_GetILFunctionBody(This,moduleId,methodId,ppMethodHeader,pcbMethodSize)    \
    ( (This)->lpVtbl -> GetILFunctionBody(This,moduleId,methodId,ppMethodHeader,pcbMethodSize) ) 

#define ICorProfilerInfo8_GetILFunctionBodyAllocator(This,moduleId,ppMalloc)    \
    ( (This)->lpVtbl -> GetILFunctionBodyAllocator(This,moduleId,ppMalloc) ) 

#define ICorProfilerInfo8_SetILFunctionBody(This,moduleId,methodid,pbNewILMethodHeader) \
    ( (This)->lpVtbl -> SetILFunctionBody(This,moduleId,methodid,pbNewILMethodHeader) ) 

#define ICorProfilerInfo8_GetAppDomainInfo(This,appDomainId,cchName,pcchName,szName,pProcessId) \
    ( (This)->lpVtbl -> GetAppDomainInfo(This,appDomainId,cchName,pcchName,szName,pProcessId) ) 

#define ICorProfilerInfo8_GetAssemblyInfo(This,assemblyId,cchName,pcchName,szName,pAppDomainId,pModuleId)   \
    ( (This)->lpVtbl -> GetAssemblyInfo(This,assemblyId,cchName,pcchName,szName,pAppDomainId,pModuleId) ) 

#define ICorProfilerInfo8_SetFunctionReJIT(This,functionId) \
    ( (This)->lpVtbl -> SetFunctionReJIT(This,functionId) ) 

#define ICorProfilerInfo8_ForceGC(This) \
    ( (This)->lpVtbl -> ForceGC(This) ) 

#define ICorProfilerInfo8_SetILInstrumentedCodeMap(This,functionId,fStartJit,cILMapEntries,rgILMapEntries)  \
    ( (This)->lpVtbl -> SetILInstrumentedCodeMap(This,functionId,fStartJit,cILMapEntries,rgILMapEntries) ) 

#define ICorProfilerInfo8_GetInprocInspectionInterface(This,ppicd)  \
    ( (This)->lpVtbl -> GetInprocInspectionInterface(This,ppicd) ) 

#define ICorProfilerInfo8_GetInprocInspectionIThisThread(This,ppicd)    \
    ( (This)->lpVtbl -> GetInprocInspectionIThisThread(This,ppicd) ) 

#define ICorProfilerInfo8_GetThreadContext(This,threadId,pContextId)    \
    ( (This)->lpVtbl -> GetThreadContext(This,threadId,pContextId) ) 

#define ICorProfilerInfo8_BeginInprocDebugging(This,fThisThreadOnly,pdwProfilerContext) \
    ( (This)->lpVtbl -> BeginInprocDebugging(This,fThisThreadOnly,pdwProfilerContext) ) 

#define ICorProfilerInfo8_EndInprocDebugging(This,dwProfilerContext)    \
    ( (This)->lpVtbl -> EndInprocDebugging(This,dwProfilerContext) ) 

#define ICorProfilerInfo8_GetILToNativeMapping(This,functionId,cMap,pcMap,map)  \
    ( (This)->lpVtbl -> GetILToNativeMapping(This,functionId,cMap,pcMap,map) ) 


#define ICorProfilerInfo8_DoStackSnapshot(This,thread,callback,infoFlags,clientData,context,contextSize)    \
    ( (This)->lpVtbl -> DoStackSnapshot(This,thread,callback,infoFlags,clientData,context,contextSize) ) 

#define ICorProfilerInfo8_SetEnterLeaveFunctionHooks2(This,pFuncEnter,pFuncLeave,pFuncTailcall) \
    ( (This)->lpVtbl -> SetEnterLeaveFunctionHooks2(This,pFuncEnter,pFuncLeave,pFuncTailcall) ) 

#define ICorProfilerInfo8_GetFunctionInfo2(This,funcId,frameInfo,pClassId,pModuleId,pToken,cTypeArgs,pcTypeArgs,typeArgs)   \
    ( (This)->lpVtbl -> GetFunctionInfo2(This,funcId,frameInfo,pClassId,pModuleId,pToken,cTypeArgs,pcTypeArgs,typeArgs) ) 

#define ICorProfilerInfo8_GetStringLayout(This,pBufferLengthOffset,pStringLengthOffset,pBufferOffset)   \
    ( (This)->lpVtbl -> GetStringLayout(This,pBufferLengthOffset,pStringLengthOffset,pBufferOffset) ) 

#define ICorProfilerInfo8_GetClassLayout(This,classID,rFieldOffset,cFieldOffset,pcFieldOffset,pulClassSize) \
    ( (This)->lpVtbl -> GetClassLayout(This,classID,rFieldOffset,cFieldOffset,pcFieldOffset,pulClassSize) ) 

#define ICorProfilerInfo8_GetClassIDInfo2(This,classId,pModuleId,pTypeDefToken,pParentClassId,cNumTypeArgs,pcNumTypeArgs,typeArgs)  \
    ( (This)->lpVtbl -> GetClassIDInfo2(This,classId,pModuleId,pTypeDefToken,pParentClassId,cNumTypeArgs,pcNumTypeArgs,typeArgs) ) 

#define ICorProfilerInfo8_GetCodeInfo2(This,functionID,cCodeInfos,pcCodeInfos,codeInfos)    \
    ( (This)->lpVtbl -> GetCodeInfo2(This,functionID,cCodeInfos,pcCodeInfos,codeInfos) ) 

#define ICorProfilerInfo8_GetClassFromTokenAndTypeArgs(This,moduleID,typeDef,cTypeArgs,typeArgs,pClassID)   \
    ( (This)->lpVtbl -> GetClassFromTokenAndTypeArgs(This,moduleID,typeDef,cTypeArgs,typeArgs,pClassID) ) 

#define ICorProfilerInfo8_GetFunctionFromTokenAndTypeArgs(This,moduleID,funcDef,classId,cTypeArgs,typeArgs,pFunctionID) \
    ( (This)->lpVtbl -> GetFunctionFromTokenAndTypeArgs(This,moduleID,funcDef,classId,cTypeArgs,typeArgs,pFunctionID) ) 

#define ICorProfilerInfo8_EnumModuleFrozenObjects(This,moduleID,ppEnum) \
    ( (This)->lpVtbl -> EnumModuleFrozenObjects(This,moduleID,ppEnum) ) 

#define ICorProfilerInfo8_GetArrayObjectInfo(This,objectId,cDimensions,pDimensionSizes,pDimensionLowerBounds,ppData)    \
    ( (This)->lpVtbl -> GetArrayObjectInfo(This,objectId,cDimensions,pDimensionSizes,pDimensionLowerBounds,ppData) ) 

#define ICorProfilerInfo8_GetBoxClassLayout(This,classId,pBufferOffset) \
    ( (This)->lpVtbl -> GetBoxClassLayout(This,classId,pBufferOffset) ) 

#define ICorProfilerInfo8_GetThreadAppDomain(This,threadId,pAppDomainId)    \
    ( (This)->lpVtbl -> GetThreadAppDomain(This,threadId,pAppDomainId) ) 

#define ICorProfilerInfo8_GetRVAStaticAddress(This,classId,fieldToken,ppAddress)    \
    ( (This)->lpVtbl -> GetRVAStaticAddress(This,classId,fieldToken,ppAddress) ) 

#define ICorProfilerInfo8_GetAppDomainStaticAddress(This,classId,fieldToken,appDomainId,ppAddress)  \
    ( (This)->lpVtbl -> GetAppDomainStaticAddress(This,classId,fieldToken,appDomainId,ppAddress) ) 

#define ICorProfilerInfo8_GetThreadStaticAddress(This,classId,fieldToken,threadId,ppAddress)    \
    ( (This)->lpVtbl -> GetThreadStaticAddress(This,classId,fieldToken,threadId,ppAddress) ) 

#define ICorProfilerInfo8_GetContextStaticAddress(This,classId,fieldToken,contextId,ppAddress)  \
    ( (This)->lpVtbl -> GetContextStaticAddress(This,classId,fieldToken,contextId,ppAddress) ) 

#define ICorProfilerInfo8_GetStaticFieldInfo(This,classId,fieldToken,pFieldInfo)    \
    ( (This)->lpVtbl -> GetStaticFieldInfo(This,classId,fieldToken,pFieldInfo) ) 

#define ICorProfilerInfo8_GetGenerationBounds(This,cObjectRanges,pcObjectRanges,ranges) \
    ( (This)->lpVtbl -> GetGenerationBounds(This,cObjectRanges,pcObjectRanges,ranges) ) 

#define ICorProfilerInfo8_GetObjectGeneration(This,objectId,range)  \
    ( (This)->lpVtbl -> GetObjectGeneration(This,objectId,range) ) 

#define ICorProfilerInfo8_GetNotifiedExceptionClauseInfo(This,pinfo)    \
    ( (This)->lpVtbl -> GetNotifiedExceptionClauseInfo(This,pinfo) ) 


#define ICorProfilerInfo8_EnumJITedFunctions(This,ppEnum)   \
    ( (This)->lpVtbl -> EnumJITedFunctions(This,ppEnum) ) 

#define ICorProfilerInfo8_RequestProfilerDetach(This,dwExpectedCompletionMilliseconds)  \
    ( (This)->lpVtbl -> RequestProfilerDetach(This,dwExpectedCompletionMilliseconds) ) 

#define ICorProfilerInfo8_SetFunctionIDMapper2(This,pFunc,clientData)   \
    ( (This)->lpVtbl -> SetFunctionIDMapper2(This,pFunc,clientData) ) 

#define ICorProfilerInfo8_GetStringLayout2(This,pStringLengthOffset,pBufferOffset)  \
    ( (This)->lpVtbl -> GetStringLayout2(This,pStringLengthOffset,pBufferOffset) ) 

#define ICorProfilerInfo8_SetEnterLeaveFunctionHooks3(This,pFuncEnter3,pFuncLeave3,pFuncTailcall3)  \
    ( (This)->lpVtbl -> SetEnterLeaveFunctionHooks3(This,pFuncEnter3,pFuncLeave3,pFuncTailcall3) ) 

#define ICorProfilerInfo8_SetEnterLeaveFunctionHooks3WithInfo(This,pFuncEnter3WithInfo,pFuncLeave3WithInfo,pFuncTailcall3WithInfo)  \
    ( (This)->lpVtbl -> SetEnterLeaveFunctionHooks3WithInfo(This,pFuncEnter3WithInfo,pFuncLeave3WithInfo,pFuncTailcall3WithInfo) ) 

#define ICorProfilerInfo8_GetFunctionEnter3Info(This,functionId,eltInfo,pFrameInfo,pcbArgumentInfo,pArgumentInfo)   \
    ( (This)->lpVtbl -> GetFunctionEnter3Info(This,functionId,eltInfo,pFrameInfo,pcbArgumentInfo,pArgumentInfo) ) 

#define ICorProfilerInfo8_GetFunctionLeave3Info(This,functionId,eltInfo,pFrameInfo,pRetvalRange)    \
    ( (This)->lpVtbl -> GetFunctionLeave3Info(This,functionId,eltInfo,pFrameInfo,pRetvalRange) ) 

#define ICorProfilerInfo8_GetFunctionTailcall3Info(This,functionId,eltInfo,pFrameInfo)  \
    ( (This)->lpVtbl -> GetFunctionTailcall3Info(This,functionId,eltInfo,pFrameInfo) ) 

#define ICorProfilerInfo8_EnumModules(This,ppEnum)  \
    ( (This)->lpVtbl -> EnumModules(This,ppEnum) ) 

#define ICorProfilerInfo8_GetRuntimeInformation(This,pClrInstanceId,pRuntimeType,pMajorVersion,pMinorVersion,pBuildNumber,pQFEVersion,cchVersionString,pcchVersionString,szVersionString)   \
    ( (This)->lpVtbl -> GetRuntimeInformation(This,pClrInstanceId,pRuntimeType,pMajorVersion,pMinorVersion,pBuildNumber,pQFEVersion,cchVersionString,pcchVersionString,szVersionString) ) 

#define ICorProfilerInfo8_GetThreadStaticAddress2(This,classId,fieldToken,appDomainId,threadId,ppAddress)   \
    ( (This)->lpVtbl -> GetThreadStaticAddress2(This,classId,fieldToken,appDomainId,threadId,ppAddress) ) 

#define ICorProfilerInfo8_GetAppDomainsContainingModule(This,moduleId,cAppDomainIds,pcAppDomainIds,appDomainIds)    \
    ( (This)->lpVtbl -> GetAppDomainsContainingModule(This,moduleId,cAppDomainIds,pcAppDomainIds,appDomainIds) ) 

#define ICorProfilerInfo8_GetModuleInfo2(This,moduleId,ppBaseLoadAddress,cchName,pcchName,szName,pAssemblyId,pdwModuleFlags)    \
    ( (This)->lpVtbl -> GetModuleInfo2(This,moduleId,ppBaseLoadAddress,cchName,pcchName,szName,pAssemblyId,pdwModuleFlags) ) 


#define ICorProfilerInfo8_EnumThreads(This,ppEnum)  \
    ( (This)->lpVtbl -> EnumThreads(This,ppEnum) ) 

#define ICorProfilerInfo8_InitializeCurrentThread(This) \
    ( (This)->lpVtbl -> InitializeCurrentThread(This) ) 

#define ICorProfilerInfo8_RequestReJIT(This,cFunctions,moduleIds,methodIds) \
    ( (This)->lpVtbl -> RequestReJIT(This,cFunctions,moduleIds,methodIds) ) 

#define ICorProfilerInfo8_RequestRevert(This,cFunctions,moduleIds,methodIds,status) \
    ( (This)->lpVtbl -> RequestRevert(This,cFunctions,moduleIds,methodIds,status) ) 

#define ICorProfilerInfo8_GetCodeInfo3(This,functionID,reJitId,cCodeInfos,pcCodeInfos,codeInfos)    \
    ( (This)->lpVtbl -> GetCodeInfo3(This,functionID,reJitId,cCodeInfos,pcCodeInfos,codeInfos) ) 

#define ICorProfilerInfo8_GetFunctionFromIP2(This,ip,pFunctionId,pReJitId)  \
    ( (This)->lpVtbl -> GetFunctionFromIP2(This,ip,pFunctionId,pReJitId) ) 

#define ICorProfilerInfo8_GetReJITIDs(This,functionId,cReJitIds,pcReJitIds,reJitIds)    \
    ( (This)->lpVtbl -> GetReJITIDs(This,functionId,cReJitIds,pcReJitIds,reJitIds) ) 

#define ICorProfilerInfo8_GetILToNativeMapping2(This,functionId,reJitId,cMap,pcMap,map) \
    ( (This)->lpVtbl -> GetILToNativeMapping2(This,functionId,reJitId,cMap,pcMap,map) ) 

#define ICorProfilerInfo8_EnumJITedFunctions2(This,ppEnum)  \
    ( (This)->lpVtbl -> EnumJITedFunctions2(This,ppEnum) ) 

#define ICorProfilerInfo8_GetObjectSize2(This,objectId,pcSize)  \
    ( (This)->lpVtbl -> GetObjectSize2(This,objectId,pcSize) ) 


#define ICorProfilerInfo8_GetEventMask2(This,pdwEventsLow,pdwEventsHigh)    \
    ( (This)->lpVtbl -> GetEventMask2(This,pdwEventsLow,pdwEventsHigh) ) 

#define ICorProfilerInfo8_SetEventMask2(This,dwEventsLow,dwEventsHigh)  \
    ( (This)->lpVtbl -> SetEventMask2(This,dwEventsLow,dwEventsHigh) ) 


#define ICorProfilerInfo8_EnumNgenModuleMethodsInliningThisMethod(This,inlinersModuleId,inlineeModuleId,inlineeMethodId,incompleteData,ppEnum)  \
    ( (This)->lpVtbl -> EnumNgenModuleMethodsInliningThisMethod(This,inlinersModuleId,inlineeModuleId,inlineeMethodId,incompleteData,ppEnum) ) 


#define ICorProfilerInfo8_ApplyMetaData(This,moduleId)  \
    ( (This)->lpVtbl -> ApplyMetaData(This,moduleId) ) 

#define ICorProfilerInfo8_GetInMemorySymbolsLength(This,moduleId,pCountSymbolBytes) \
    ( (This)->lpVtbl -> GetInMemorySymbolsLength(This,moduleId,pCountSymbolBytes) ) 

#define ICorProfilerInfo8_ReadInMemorySymbols(This,moduleId,symbolsReadOffset,pSymbolBytes,countSymbolBytes,pCountSymbolBytesRead)  \
    ( (This)->lpVtbl -> ReadInMemorySymbols(This,moduleId,symbolsReadOffset,pSymbolBytes,countSymbolBytes,pCountSymbolBytesRead) ) 


#define ICorProfilerInfo8_IsFunctionDynamic(This,functionId,isDynamic)  \
    ( (This)->lpVtbl -> IsFunctionDynamic(This,functionId,isDynamic) ) 

#define ICorProfilerInfo8_GetFunctionFromIP3(This,ip,functionId,pReJitId)   \
    ( (This)->lpVtbl -> GetFunctionFromIP3(This,ip,functionId,pReJitId) ) 

#define ICorProfilerInfo8_GetDynamicFunctionInfo(This,functionId,moduleId,ppvSig,pbSig,cchName,pcchName,wszName)    \
    ( (This)->lpVtbl -> GetDynamicFunctionInfo(This,functionId,moduleId,ppvSig,pbSig,cchName,pcchName,wszName) ) 

#endif /* COBJMACROS */


#endif  /* C style interface */




#endif  /* __ICorProfilerInfo8_INTERFACE_DEFINED__ */


#ifndef __ICorProfilerInfo9_INTERFACE_DEFINED__
#define __ICorProfilerInfo9_INTERFACE_DEFINED__

/* interface ICorProfilerInfo9 */
/* [local][unique][uuid][object] */ 


EXTERN_C const IID IID_ICorProfilerInfo9;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("008170DB-F8CC-4796-9A51-DC8AA0B47012")
    ICorProfilerInfo9 : public ICorProfilerInfo8
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE GetNativeCodeStartAddresses( 
            FunctionID functionID,
            ReJITID reJitId,
            ULONG32 cCodeStartAddresses,
            ULONG32 *pcCodeStartAddresses,
            UINT_PTR codeStartAddresses[  ]) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetILToNativeMapping3( 
            UINT_PTR pNativeCodeStartAddress,
            ULONG32 cMap,
            ULONG32 *pcMap,
            COR_DEBUG_IL_TO_NATIVE_MAP map[  ]) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetCodeInfo4( 
            UINT_PTR pNativeCodeStartAddress,
            ULONG32 cCodeInfos,
            ULONG32 *pcCodeInfos,
            COR_PRF_CODE_INFO codeInfos[  ]) = 0;
        
    };
    
    
#else   /* C style interface */

    typedef struct ICorProfilerInfo9Vtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            ICorProfilerInfo9 * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            ICorProfilerInfo9 * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            ICorProfilerInfo9 * This);
        
        HRESULT ( STDMETHODCALLTYPE *GetClassFromObject )( 
            ICorProfilerInfo9 * This,
            /* [in] */ ObjectID objectId,
            /* [out] */ ClassID *pClassId);
        
        HRESULT ( STDMETHODCALLTYPE *GetClassFromToken )( 
            ICorProfilerInfo9 * This,
            /* [in] */ ModuleID moduleId,
            /* [in] */ mdTypeDef typeDef,
            /* [out] */ ClassID *pClassId);
        
        HRESULT ( STDMETHODCALLTYPE *GetCodeInfo )( 
            ICorProfilerInfo9 * This,
            /* [in] */ FunctionID functionId,
            /* [out] */ LPCBYTE *pStart,
            /* [out] */ ULONG *pcSize);
        
        HRESULT ( STDMETHODCALLTYPE *GetEventMask )( 
            ICorProfilerInfo9 * This,
            /* [out] */ DWORD *pdwEvents);
        
        HRESULT ( STDMETHODCALLTYPE *GetFunctionFromIP )( 
            ICorProfilerInfo9 * This,
            /* [in] */ LPCBYTE ip,
            /* [out] */ FunctionID *pFunctionId);
        
        HRESULT ( STDMETHODCALLTYPE *GetFunctionFromToken )( 
            ICorProfilerInfo9 * This,
            /* [in] */ ModuleID moduleId,
            /* [in] */ mdToken token,
            /* [out] */ FunctionID *pFunctionId);
        
        HRESULT ( STDMETHODCALLTYPE *GetHandleFromThread )( 
            ICorProfilerInfo9 * This,
            /* [in] */ ThreadID threadId,
            /* [out] */ HANDLE *phThread);
        
        HRESULT ( STDMETHODCALLTYPE *GetObjectSize )( 
            ICorProfilerInfo9 * This,
            /* [in] */ ObjectID objectId,
            /* [out] */ ULONG *pcSize);
        
        HRESULT ( STDMETHODCALLTYPE *IsArrayClass )( 
            ICorProfilerInfo9 * This,
            /* [in] */ ClassID classId,
            /* [out] */ CorElementType *pBaseElemType,
            /* [out] */ ClassID *pBaseClassId,
            /* [out] */ ULONG *pcRank);
        
        HRESULT ( STDMETHODCALLTYPE *GetThreadInfo )( 
            ICorProfilerInfo9 * This,
            /* [in] */ ThreadID threadId,
            /* [out] */ DWORD *pdwWin32ThreadId);
        
        HRESULT ( STDMETHODCALLTYPE *GetCurrentThreadID )( 
            ICorProfilerInfo9 * This,
            /* [out] */ ThreadID *pThreadId);
        
        HRESULT ( STDMETHODCALLTYPE *GetClassIDInfo )( 
            ICorProfilerInfo9 * This,
            /* [in] */ ClassID classId,
            /* [out] */ ModuleID *pModuleId,
            /* [out] */ mdTypeDef *pTypeDefToken);
        
        HRESULT ( STDMETHODCALLTYPE *GetFunctionInfo )( 
            ICorProfilerInfo9 * This,
            /* [in] */ FunctionID functionId,
            /* [out] */ ClassID *pClassId,
            /* [out] */ ModuleID *pModuleId,
            /* [out] */ mdToken *pToken);
        
        HRESULT ( STDMETHODCALLTYPE *SetEventMask )( 
            ICorProfilerInfo9 * This,
            /* [in] */ DWORD dwEvents);
        
        HRESULT ( STDMETHODCALLTYPE *SetEnterLeaveFunctionHooks )( 
            ICorProfilerInfo9 * This,
            /* [in] */ FunctionEnter *pFuncEnter,
            /* [in] */ FunctionLeave *pFuncLeave,
            /* [in] */ FunctionTailcall *pFuncTailcall);
        
        HRESULT ( STDMETHODCALLTYPE *SetFunctionIDMapper )( 
            ICorProfilerInfo9 * This,
            /* [in] */ FunctionIDMapper *pFunc);
        
        HRESULT ( STDMETHODCALLTYPE *GetTokenAndMetaDataFromFunction )( 
            ICorProfilerInfo9 * This,
            /* [in] */ FunctionID functionId,
            /* [in] */ REFIID riid,
            /* [out] */ IUnknown **ppImport,
            /* [out] */ mdToken *pToken);
        
        HRESULT ( STDMETHODCALLTYPE *GetModuleInfo )( 
            ICorProfilerInfo9 * This,
            /* [in] */ ModuleID moduleId,
            /* [out] */ LPCBYTE *ppBaseLoadAddress,
            /* [in] */ ULONG cchName,
            /* [out] */ ULONG *pcchName,
            /* [annotation][out] */ 
            _Out_writes_to_(cchName, *pcchName)  WCHAR szName[  ],
            /* [out] */ AssemblyID *pAssemblyId);
        
        HRESULT ( STDMETHODCALLTYPE *GetModuleMetaData )( 
            ICorProfilerInfo9 * This,
            /* [in] */ ModuleID moduleId,
            /* [in] */ DWORD dwOpenFlags,
            /* [in] */ REFIID riid,
            /* [out] */ IUnknown **ppOut);
        
        HRESULT ( STDMETHODCALLTYPE *GetILFunctionBody )( 
            ICorProfilerInfo9 * This,
            /* [in] */ ModuleID moduleId,
            /* [in] */ mdMethodDef methodId,
            /* [out] */ LPCBYTE *ppMethodHeader,
            /* [out] */ ULONG *pcbMethodSize);
        
        HRESULT ( STDMETHODCALLTYPE *GetILFunctionBodyAllocator )( 
            ICorProfilerInfo9 * This,
            /* [in] */ ModuleID moduleId,
            /* [out] */ IMethodMalloc **ppMalloc);
        
        HRESULT ( STDMETHODCALLTYPE *SetILFunctionBody )( 
            ICorProfilerInfo9 * This,
            /* [in] */ ModuleID moduleId,
            /* [in] */ mdMethodDef methodid,
            /* [in] */ LPCBYTE pbNewILMethodHeader);
        
        HRESULT ( STDMETHODCALLTYPE *GetAppDomainInfo )( 
            ICorProfilerInfo9 * This,
            /* [in] */ AppDomainID appDomainId,
            /* [in] */ ULONG cchName,
            /* [out] */ ULONG *pcchName,
            /* [annotation][out] */ 
            _Out_writes_to_(cchName, *pcchName)  WCHAR szName[  ],
            /* [out] */ ProcessID *pProcessId);
        
        HRESULT ( STDMETHODCALLTYPE *GetAssemblyInfo )( 
            ICorProfilerInfo9 * This,
            /* [in] */ AssemblyID assemblyId,
            /* [in] */ ULONG cchName,
            /* [out] */ ULONG *pcchName,
            /* [annotation][out] */ 
            _Out_writes_to_(cchName, *pcchName)  WCHAR szName[  ],
            /* [out] */ AppDomainID *pAppDomainId,
            /* [out] */ ModuleID *pModuleId);
        
        HRESULT ( STDMETHODCALLTYPE *SetFunctionReJIT )( 
            ICorProfilerInfo9 * This,
            /* [in] */ FunctionID functionId);
        
        HRESULT ( STDMETHODCALLTYPE *ForceGC )( 
            ICorProfilerInfo9 * This);
        
        HRESULT ( STDMETHODCALLTYPE *SetILInstrumentedCodeMap )( 
            ICorProfilerInfo9 * This,
            /* [in] */ FunctionID functionId,
            /* [in] */ BOOL fStartJit,
            /* [in] */ ULONG cILMapEntries,
            /* [size_is][in] */ COR_IL_MAP rgILMapEntries[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *GetInprocInspectionInterface )( 
            ICorProfilerInfo9 * This,
            /* [out] */ IUnknown **ppicd);
        
        HRESULT ( STDMETHODCALLTYPE *GetInprocInspectionIThisThread )( 
            ICorProfilerInfo9 * This,
            /* [out] */ IUnknown **ppicd);
        
        HRESULT ( STDMETHODCALLTYPE *GetThreadContext )( 
            ICorProfilerInfo9 * This,
            /* [in] */ ThreadID threadId,
            /* [out] */ ContextID *pContextId);
        
        HRESULT ( STDMETHODCALLTYPE *BeginInprocDebugging )( 
            ICorProfilerInfo9 * This,
            /* [in] */ BOOL fThisThreadOnly,
            /* [out] */ DWORD *pdwProfilerContext);
        
        HRESULT ( STDMETHODCALLTYPE *EndInprocDebugging )( 
            ICorProfilerInfo9 * This,
            /* [in] */ DWORD dwProfilerContext);
        
        HRESULT ( STDMETHODCALLTYPE *GetILToNativeMapping )( 
            ICorProfilerInfo9 * This,
            /* [in] */ FunctionID functionId,
            /* [in] */ ULONG32 cMap,
            /* [out] */ ULONG32 *pcMap,
            /* [length_is][size_is][out] */ COR_DEBUG_IL_TO_NATIVE_MAP map[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *DoStackSnapshot )( 
            ICorProfilerInfo9 * This,
            /* [in] */ ThreadID thread,
            /* [in] */ StackSnapshotCallback *callback,
            /* [in] */ ULONG32 infoFlags,
            /* [in] */ void *clientData,
            /* [size_is][in] */ BYTE context[  ],
            /* [in] */ ULONG32 contextSize);
        
        HRESULT ( STDMETHODCALLTYPE *SetEnterLeaveFunctionHooks2 )( 
            ICorProfilerInfo9 * This,
            /* [in] */ FunctionEnter2 *pFuncEnter,
            /* [in] */ FunctionLeave2 *pFuncLeave,
            /* [in] */ FunctionTailcall2 *pFuncTailcall);
        
        HRESULT ( STDMETHODCALLTYPE *GetFunctionInfo2 )( 
            ICorProfilerInfo9 * This,
            /* [in] */ FunctionID funcId,
            /* [in] */ COR_PRF_FRAME_INFO frameInfo,
            /* [out] */ ClassID *pClassId,
            /* [out] */ ModuleID *pModuleId,
            /* [out] */ mdToken *pToken,
            /* [in] */ ULONG32 cTypeArgs,
            /* [out] */ ULONG32 *pcTypeArgs,
            /* [out] */ ClassID typeArgs[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *GetStringLayout )( 
            ICorProfilerInfo9 * This,
            /* [out] */ ULONG *pBufferLengthOffset,
            /* [out] */ ULONG *pStringLengthOffset,
            /* [out] */ ULONG *pBufferOffset);
        
        HRESULT ( STDMETHODCALLTYPE *GetClassLayout )( 
            ICorProfilerInfo9 * This,
            /* [in] */ ClassID classID,
            /* [out][in] */ COR_FIELD_OFFSET rFieldOffset[  ],
            /* [in] */ ULONG cFieldOffset,
            /* [out] */ ULONG *pcFieldOffset,
            /* [out] */ ULONG *pulClassSize);
        
        HRESULT ( STDMETHODCALLTYPE *GetClassIDInfo2 )( 
            ICorProfilerInfo9 * This,
            /* [in] */ ClassID classId,
            /* [out] */ ModuleID *pModuleId,
            /* [out] */ mdTypeDef *pTypeDefToken,
            /* [out] */ ClassID *pParentClassId,
            /* [in] */ ULONG32 cNumTypeArgs,
            /* [out] */ ULONG32 *pcNumTypeArgs,
            /* [out] */ ClassID typeArgs[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *GetCodeInfo2 )( 
            ICorProfilerInfo9 * This,
            /* [in] */ FunctionID functionID,
            /* [in] */ ULONG32 cCodeInfos,
            /* [out] */ ULONG32 *pcCodeInfos,
            /* [length_is][size_is][out] */ COR_PRF_CODE_INFO codeInfos[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *GetClassFromTokenAndTypeArgs )( 
            ICorProfilerInfo9 * This,
            /* [in] */ ModuleID moduleID,
            /* [in] */ mdTypeDef typeDef,
            /* [in] */ ULONG32 cTypeArgs,
            /* [size_is][in] */ ClassID typeArgs[  ],
            /* [out] */ ClassID *pClassID);
        
        HRESULT ( STDMETHODCALLTYPE *GetFunctionFromTokenAndTypeArgs )( 
            ICorProfilerInfo9 * This,
            /* [in] */ ModuleID moduleID,
            /* [in] */ mdMethodDef funcDef,
            /* [in] */ ClassID classId,
            /* [in] */ ULONG32 cTypeArgs,
            /* [size_is][in] */ ClassID typeArgs[  ],
            /* [out] */ FunctionID *pFunctionID);
        
        HRESULT ( STDMETHODCALLTYPE *EnumModuleFrozenObjects )( 
            ICorProfilerInfo9 * This,
            /* [in] */ ModuleID moduleID,
            /* [out] */ ICorProfilerObjectEnum **ppEnum);
        
        HRESULT ( STDMETHODCALLTYPE *GetArrayObjectInfo )( 
            ICorProfilerInfo9 * This,
            /* [in] */ ObjectID objectId,
            /* [in] */ ULONG32 cDimensions,
            /* [size_is][out] */ ULONG32 pDimensionSizes[  ],
            /* [size_is][out] */ int pDimensionLowerBounds[  ],
            /* [out] */ BYTE **ppData);
        
        HRESULT ( STDMETHODCALLTYPE *GetBoxClassLayout )( 
            ICorProfilerInfo9 * This,
            /* [in] */ ClassID classId,
            /* [out] */ ULONG32 *pBufferOffset);
        
        HRESULT ( STDMETHODCALLTYPE *GetThreadAppDomain )( 
            ICorProfilerInfo9 * This,
            /* [in] */ ThreadID threadId,
            /* [out] */ AppDomainID *pAppDomainId);
        
        HRESULT ( STDMETHODCALLTYPE *GetRVAStaticAddress )( 
            ICorProfilerInfo9 * This,
            /* [in] */ ClassID classId,
            /* [in] */ mdFieldDef fieldToken,
            /* [out] */ void **ppAddress);
        
        HRESULT ( STDMETHODCALLTYPE *GetAppDomainStaticAddress )( 
            ICorProfilerInfo9 * This,
            /* [in] */ ClassID classId,
            /* [in] */ mdFieldDef fieldToken,
            /* [in] */ AppDomainID appDomainId,
            /* [out] */ void **ppAddress);
        
        HRESULT ( STDMETHODCALLTYPE *GetThreadStaticAddress )( 
            ICorProfilerInfo9 * This,
            /* [in] */ ClassID classId,
            /* [in] */ mdFieldDef fieldToken,
            /* [in] */ ThreadID threadId,
            /* [out] */ void **ppAddress);
        
        HRESULT ( STDMETHODCALLTYPE *GetContextStaticAddress )( 
            ICorProfilerInfo9 * This,
            /* [in] */ ClassID classId,
            /* [in] */ mdFieldDef fieldToken,
            /* [in] */ ContextID contextId,
            /* [out] */ void **ppAddress);
        
        HRESULT ( STDMETHODCALLTYPE *GetStaticFieldInfo )( 
            ICorProfilerInfo9 * This,
            /* [in] */ ClassID classId,
            /* [in] */ mdFieldDef fieldToken,
            /* [out] */ COR_PRF_STATIC_TYPE *pFieldInfo);
        
        HRESULT ( STDMETHODCALLTYPE *GetGenerationBounds )( 
            ICorProfilerInfo9 * This,
            /* [in] */ ULONG cObjectRanges,
            /* [out] */ ULONG *pcObjectRanges,
            /* [length_is][size_is][out] */ COR_PRF_GC_GENERATION_RANGE ranges[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *GetObjectGeneration )( 
            ICorProfilerInfo9 * This,
            /* [in] */ ObjectID objectId,
            /* [out] */ COR_PRF_GC_GENERATION_RANGE *range);
        
        HRESULT ( STDMETHODCALLTYPE *GetNotifiedExceptionClauseInfo )( 
            ICorProfilerInfo9 * This,
            /* [out] */ COR_PRF_EX_CLAUSE_INFO *pinfo);
        
        HRESULT ( STDMETHODCALLTYPE *EnumJITedFunctions )( 
            ICorProfilerInfo9 * This,
            /* [out] */ ICorProfilerFunctionEnum **ppEnum);
        
        HRESULT ( STDMETHODCALLTYPE *RequestProfilerDetach )( 
            ICorProfilerInfo9 * This,
            /* [in] */ DWORD dwExpectedCompletionMilliseconds);
        
        HRESULT ( STDMETHODCALLTYPE *SetFunctionIDMapper2 )( 
            ICorProfilerInfo9 * This,
            /* [in] */ FunctionIDMapper2 *pFunc,
            /* [in] */ void *clientData);
        
        HRESULT ( STDMETHODCALLTYPE *GetStringLayout2 )( 
            ICorProfilerInfo9 * This,
            /* [out] */ ULONG *pStringLengthOffset,
            /* [out] */ ULONG *pBufferOffset);
        
        HRESULT ( STDMETHODCALLTYPE *SetEnterLeaveFunctionHooks3 )( 
            ICorProfilerInfo9 * This,
            /* [in] */ FunctionEnter3 *pFuncEnter3,
            /* [in] */ FunctionLeave3 *pFuncLeave3,
            /* [in] */ FunctionTailcall3 *pFuncTailcall3);
        
        HRESULT ( STDMETHODCALLTYPE *SetEnterLeaveFunctionHooks3WithInfo )( 
            ICorProfilerInfo9 * This,
            /* [in] */ FunctionEnter3WithInfo *pFuncEnter3WithInfo,
            /* [in] */ FunctionLeave3WithInfo *pFuncLeave3WithInfo,
            /* [in] */ FunctionTailcall3WithInfo *pFuncTailcall3WithInfo);
        
        HRESULT ( STDMETHODCALLTYPE *GetFunctionEnter3Info )( 
            ICorProfilerInfo9 * This,
            /* [in] */ FunctionID functionId,
            /* [in] */ COR_PRF_ELT_INFO eltInfo,
            /* [out] */ COR_PRF_FRAME_INFO *pFrameInfo,
            /* [out][in] */ ULONG *pcbArgumentInfo,
            /* [size_is][out] */ COR_PRF_FUNCTION_ARGUMENT_INFO *pArgumentInfo);
        
        HRESULT ( STDMETHODCALLTYPE *GetFunctionLeave3Info )( 
            ICorProfilerInfo9 * This,
            /* [in] */ FunctionID functionId,
            /* [in] */ COR_PRF_ELT_INFO eltInfo,
            /* [out] */ COR_PRF_FRAME_INFO *pFrameInfo,
            /* [out] */ COR_PRF_FUNCTION_ARGUMENT_RANGE *pRetvalRange);
        
        HRESULT ( STDMETHODCALLTYPE *GetFunctionTailcall3Info )( 
            ICorProfilerInfo9 * This,
            /* [in] */ FunctionID functionId,
            /* [in] */ COR_PRF_ELT_INFO eltInfo,
            /* [out] */ COR_PRF_FRAME_INFO *pFrameInfo);
        
        HRESULT ( STDMETHODCALLTYPE *EnumModules )( 
            ICorProfilerInfo9 * This,
            /* [out] */ ICorProfilerModuleEnum **ppEnum);
        
        HRESULT ( STDMETHODCALLTYPE *GetRuntimeInformation )( 
            ICorProfilerInfo9 * This,
            /* [out] */ USHORT *pClrInstanceId,
            /* [out] */ COR_PRF_RUNTIME_TYPE *pRuntimeType,
            /* [out] */ USHORT *pMajorVersion,
            /* [out] */ USHORT *pMinorVersion,
            /* [out] */ USHORT *pBuildNumber,
            /* [out] */ USHORT *pQFEVersion,
            /* [in] */ ULONG cchVersionString,
            /* [out] */ ULONG *pcchVersionString,
            /* [annotation][out] */ 
            _Out_writes_to_(cchVersionString, *pcchVersionString)  WCHAR szVersionString[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *GetThreadStaticAddress2 )( 
            ICorProfilerInfo9 * This,
            /* [in] */ ClassID classId,
            /* [in] */ mdFieldDef fieldToken,
            /* [in] */ AppDomainID appDomainId,
            /* [in] */ ThreadID threadId,
            /* [out] */ void **ppAddress);
        
        HRESULT ( STDMETHODCALLTYPE *GetAppDomainsContainingModule )( 
            ICorProfilerInfo9 * This,
            /* [in] */ ModuleID moduleId,
            /* [in] */ ULONG32 cAppDomainIds,
            /* [out] */ ULONG32 *pcAppDomainIds,
            /* [length_is][size_is][out] */ AppDomainID appDomainIds[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *GetModuleInfo2 )( 
            ICorProfilerInfo9 * This,
            /* [in] */ ModuleID moduleId,
            /* [out] */ LPCBYTE *ppBaseLoadAddress,
            /* [in] */ ULONG cchName,
            /* [out] */ ULONG *pcchName,
            /* [annotation][out] */ 
            _Out_writes_to_(cchName, *pcchName)  WCHAR szName[  ],
            /* [out] */ AssemblyID *pAssemblyId,
            /* [out] */ DWORD *pdwModuleFlags);
        
        HRESULT ( STDMETHODCALLTYPE *EnumThreads )( 
            ICorProfilerInfo9 * This,
            /* [out] */ ICorProfilerThreadEnum **ppEnum);
        
        HRESULT ( STDMETHODCALLTYPE *InitializeCurrentThread )( 
            ICorProfilerInfo9 * This);
        
        HRESULT ( STDMETHODCALLTYPE *RequestReJIT )( 
            ICorProfilerInfo9 * This,
            /* [in] */ ULONG cFunctions,
            /* [size_is][in] */ ModuleID moduleIds[  ],
            /* [size_is][in] */ mdMethodDef methodIds[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *RequestRevert )( 
            ICorProfilerInfo9 * This,
            /* [in] */ ULONG cFunctions,
            /* [size_is][in] */ ModuleID moduleIds[  ],
            /* [size_is][in] */ mdMethodDef methodIds[  ],
            /* [size_is][out] */ HRESULT status[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *GetCodeInfo3 )( 
            ICorProfilerInfo9 * This,
            /* [in] */ FunctionID functionID,
            /* [in] */ ReJITID reJitId,
            /* [in] */ ULONG32 cCodeInfos,
            /* [out] */ ULONG32 *pcCodeInfos,
            /* [length_is][size_is][out] */ COR_PRF_CODE_INFO codeInfos[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *GetFunctionFromIP2 )( 
            ICorProfilerInfo9 * This,
            /* [in] */ LPCBYTE ip,
            /* [out] */ FunctionID *pFunctionId,
            /* [out] */ ReJITID *pReJitId);
        
        HRESULT ( STDMETHODCALLTYPE *GetReJITIDs )( 
            ICorProfilerInfo9 * This,
            /* [in] */ FunctionID functionId,
            /* [in] */ ULONG cReJitIds,
            /* [out] */ ULONG *pcReJitIds,
            /* [length_is][size_is][out] */ ReJITID reJitIds[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *GetILToNativeMapping2 )( 
            ICorProfilerInfo9 * This,
            /* [in] */ FunctionID functionId,
            /* [in] */ ReJITID reJitId,
            /* [in] */ ULONG32 cMap,
            /* [out] */ ULONG32 *pcMap,
            /* [length_is][size_is][out] */ COR_DEBUG_IL_TO_NATIVE_MAP map[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *EnumJITedFunctions2 )( 
            ICorProfilerInfo9 * This,
            /* [out] */ ICorProfilerFunctionEnum **ppEnum);
        
        HRESULT ( STDMETHODCALLTYPE *GetObjectSize2 )( 
            ICorProfilerInfo9 * This,
            /* [in] */ ObjectID objectId,
            /* [out] */ SIZE_T *pcSize);
        
        HRESULT ( STDMETHODCALLTYPE *GetEventMask2 )( 
            ICorProfilerInfo9 * This,
            /* [out] */ DWORD *pdwEventsLow,
            /* [out] */ DWORD *pdwEventsHigh);
        
        HRESULT ( STDMETHODCALLTYPE *SetEventMask2 )( 
            ICorProfilerInfo9 * This,
            /* [in] */ DWORD dwEventsLow,
            /* [in] */ DWORD dwEventsHigh);
        
        HRESULT ( STDMETHODCALLTYPE *EnumNgenModuleMethodsInliningThisMethod )( 
            ICorProfilerInfo9 * This,
            /* [in] */ ModuleID inlinersModuleId,
            /* [in] */ ModuleID inlineeModuleId,
            /* [in] */ mdMethodDef inlineeMethodId,
            /* [out] */ BOOL *incompleteData,
            /* [out] */ ICorProfilerMethodEnum **ppEnum);
        
        HRESULT ( STDMETHODCALLTYPE *ApplyMetaData )( 
            ICorProfilerInfo9 * This,
            /* [in] */ ModuleID moduleId);
        
        HRESULT ( STDMETHODCALLTYPE *GetInMemorySymbolsLength )( 
            ICorProfilerInfo9 * This,
            /* [in] */ ModuleID moduleId,
            /* [out] */ DWORD *pCountSymbolBytes);
        
        HRESULT ( STDMETHODCALLTYPE *ReadInMemorySymbols )( 
            ICorProfilerInfo9 * This,
            /* [in] */ ModuleID moduleId,
            /* [in] */ DWORD symbolsReadOffset,
            /* [out] */ BYTE *pSymbolBytes,
            /* [in] */ DWORD countSymbolBytes,
            /* [out] */ DWORD *pCountSymbolBytesRead);
        
        HRESULT ( STDMETHODCALLTYPE *IsFunctionDynamic )( 
            ICorProfilerInfo9 * This,
            /* [in] */ FunctionID functionId,
            /* [out] */ BOOL *isDynamic);
        
        HRESULT ( STDMETHODCALLTYPE *GetFunctionFromIP3 )( 
            ICorProfilerInfo9 * This,
            /* [in] */ LPCBYTE ip,
            /* [out] */ FunctionID *functionId,
            /* [out] */ ReJITID *pReJitId);
        
        HRESULT ( STDMETHODCALLTYPE *GetDynamicFunctionInfo )( 
            ICorProfilerInfo9 * This,
            /* [in] */ FunctionID functionId,
            /* [out] */ ModuleID *moduleId,
            /* [out] */ PCCOR_SIGNATURE *ppvSig,
            /* [out] */ ULONG *pbSig,
            /* [in] */ ULONG cchName,
            /* [out] */ ULONG *pcchName,
            /* [out] */ WCHAR wszName[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *GetNativeCodeStartAddresses )( 
            ICorProfilerInfo9 * This,
            FunctionID functionID,
            ReJITID reJitId,
            ULONG32 cCodeStartAddresses,
            ULONG32 *pcCodeStartAddresses,
            UINT_PTR codeStartAddresses[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *GetILToNativeMapping3 )( 
            ICorProfilerInfo9 * This,
            UINT_PTR pNativeCodeStartAddress,
            ULONG32 cMap,
            ULONG32 *pcMap,
            COR_DEBUG_IL_TO_NATIVE_MAP map[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *GetCodeInfo4 )( 
            ICorProfilerInfo9 * This,
            UINT_PTR pNativeCodeStartAddress,
            ULONG32 cCodeInfos,
            ULONG32 *pcCodeInfos,
            COR_PRF_CODE_INFO codeInfos[  ]);
        
        END_INTERFACE
    } ICorProfilerInfo9Vtbl;

    interface ICorProfilerInfo9
    {
        CONST_VTBL struct ICorProfilerInfo9Vtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define ICorProfilerInfo9_QueryInterface(This,riid,ppvObject)   \
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define ICorProfilerInfo9_AddRef(This)  \
    ( (This)->lpVtbl -> AddRef(This) ) 

#define ICorProfilerInfo9_Release(This) \
    ( (This)->lpVtbl -> Release(This) ) 


#define ICorProfilerInfo9_GetClassFromObject(This,objectId,pClassId)    \
    ( (This)->lpVtbl -> GetClassFromObject(This,objectId,pClassId) ) 

#define ICorProfilerInfo9_GetClassFromToken(This,moduleId,typeDef,pClassId) \
    ( (This)->lpVtbl -> GetClassFromToken(This,moduleId,typeDef,pClassId) ) 

#define ICorProfilerInfo9_GetCodeInfo(This,functionId,pStart,pcSize)    \
    ( (This)->lpVtbl -> GetCodeInfo(This,functionId,pStart,pcSize) ) 

#define ICorProfilerInfo9_GetEventMask(This,pdwEvents)  \
    ( (This)->lpVtbl -> GetEventMask(This,pdwEvents) ) 

#define ICorProfilerInfo9_GetFunctionFromIP(This,ip,pFunctionId)    \
    ( (This)->lpVtbl -> GetFunctionFromIP(This,ip,pFunctionId) ) 

#define ICorProfilerInfo9_GetFunctionFromToken(This,moduleId,token,pFunctionId) \
    ( (This)->lpVtbl -> GetFunctionFromToken(This,moduleId,token,pFunctionId) ) 

#define ICorProfilerInfo9_GetHandleFromThread(This,threadId,phThread)   \
    ( (This)->lpVtbl -> GetHandleFromThread(This,threadId,phThread) ) 

#define ICorProfilerInfo9_GetObjectSize(This,objectId,pcSize)   \
    ( (This)->lpVtbl -> GetObjectSize(This,objectId,pcSize) ) 

#define ICorProfilerInfo9_IsArrayClass(This,classId,pBaseElemType,pBaseClassId,pcRank)  \
    ( (This)->lpVtbl -> IsArrayClass(This,classId,pBaseElemType,pBaseClassId,pcRank) ) 

#define ICorProfilerInfo9_GetThreadInfo(This,threadId,pdwWin32ThreadId) \
    ( (This)->lpVtbl -> GetThreadInfo(This,threadId,pdwWin32ThreadId) ) 

#define ICorProfilerInfo9_GetCurrentThreadID(This,pThreadId)    \
    ( (This)->lpVtbl -> GetCurrentThreadID(This,pThreadId) ) 

#define ICorProfilerInfo9_GetClassIDInfo(This,classId,pModuleId,pTypeDefToken)  \
    ( (This)->lpVtbl -> GetClassIDInfo(This,classId,pModuleId,pTypeDefToken) ) 

#define ICorProfilerInfo9_GetFunctionInfo(This,functionId,pClassId,pModuleId,pToken)    \
    ( (This)->lpVtbl -> GetFunctionInfo(This,functionId,pClassId,pModuleId,pToken) ) 

#define ICorProfilerInfo9_SetEventMask(This,dwEvents)   \
    ( (This)->lpVtbl -> SetEventMask(This,dwEvents) ) 

#define ICorProfilerInfo9_SetEnterLeaveFunctionHooks(This,pFuncEnter,pFuncLeave,pFuncTailcall)  \
    ( (This)->lpVtbl -> SetEnterLeaveFunctionHooks(This,pFuncEnter,pFuncLeave,pFuncTailcall) ) 

#define ICorProfilerInfo9_SetFunctionIDMapper(This,pFunc)   \
    ( (This)->lpVtbl -> SetFunctionIDMapper(This,pFunc) ) 

#define ICorProfilerInfo9_GetTokenAndMetaDataFromFunction(This,functionId,riid,ppImport,pToken) \
    ( (This)->lpVtbl -> GetTokenAndMetaDataFromFunction(This,functionId,riid,ppImport,pToken) ) 

#define ICorProfilerInfo9_GetModuleInfo(This,moduleId,ppBaseLoadAddress,cchName,pcchName,szName,pAssemblyId)    \
    ( (This)->lpVtbl -> GetModuleInfo(This,moduleId,ppBaseLoadAddress,cchName,pcchName,szName,pAssemblyId) ) 

#define ICorProfilerInfo9_GetModuleMetaData(This,moduleId,dwOpenFlags,riid,ppOut)   \
    ( (This)->lpVtbl -> GetModuleMetaData(This,moduleId,dwOpenFlags,riid,ppOut) ) 

#define ICorProfilerInfo9_GetILFunctionBody(This,moduleId,methodId,ppMethodHeader,pcbMethodSize)    \
    ( (This)->lpVtbl -> GetILFunctionBody(This,moduleId,methodId,ppMethodHeader,pcbMethodSize) ) 

#define ICorProfilerInfo9_GetILFunctionBodyAllocator(This,moduleId,ppMalloc)    \
    ( (This)->lpVtbl -> GetILFunctionBodyAllocator(This,moduleId,ppMalloc) ) 

#define ICorProfilerInfo9_SetILFunctionBody(This,moduleId,methodid,pbNewILMethodHeader) \
    ( (This)->lpVtbl -> SetILFunctionBody(This,moduleId,methodid,pbNewILMethodHeader) ) 

#define ICorProfilerInfo9_GetAppDomainInfo(This,appDomainId,cchName,pcchName,szName,pProcessId) \
    ( (This)->lpVtbl -> GetAppDomainInfo(This,appDomainId,cchName,pcchName,szName,pProcessId) ) 

#define ICorProfilerInfo9_GetAssemblyInfo(This,assemblyId,cchName,pcchName,szName,pAppDomainId,pModuleId)   \
    ( (This)->lpVtbl -> GetAssemblyInfo(This,assemblyId,cchName,pcchName,szName,pAppDomainId,pModuleId) ) 

#define ICorProfilerInfo9_SetFunctionReJIT(This,functionId) \
    ( (This)->lpVtbl -> SetFunctionReJIT(This,functionId) ) 

#define ICorProfilerInfo9_ForceGC(This) \
    ( (This)->lpVtbl -> ForceGC(This) ) 

#define ICorProfilerInfo9_SetILInstrumentedCodeMap(This,functionId,fStartJit,cILMapEntries,rgILMapEntries)  \
    ( (This)->lpVtbl -> SetILInstrumentedCodeMap(This,functionId,fStartJit,cILMapEntries,rgILMapEntries) ) 

#define ICorProfilerInfo9_GetInprocInspectionInterface(This,ppicd)  \
    ( (This)->lpVtbl -> GetInprocInspectionInterface(This,ppicd) ) 

#define ICorProfilerInfo9_GetInprocInspectionIThisThread(This,ppicd)    \
    ( (This)->lpVtbl -> GetInprocInspectionIThisThread(This,ppicd) ) 

#define ICorProfilerInfo9_GetThreadContext(This,threadId,pContextId)    \
    ( (This)->lpVtbl -> GetThreadContext(This,threadId,pContextId) ) 

#define ICorProfilerInfo9_BeginInprocDebugging(This,fThisThreadOnly,pdwProfilerContext) \
    ( (This)->lpVtbl -> BeginInprocDebugging(This,fThisThreadOnly,pdwProfilerContext) ) 

#define ICorProfilerInfo9_EndInprocDebugging(This,dwProfilerContext)    \
    ( (This)->lpVtbl -> EndInprocDebugging(This,dwProfilerContext) ) 

#define ICorProfilerInfo9_GetILToNativeMapping(This,functionId,cMap,pcMap,map)  \
    ( (This)->lpVtbl -> GetILToNativeMapping(This,functionId,cMap,pcMap,map) ) 


#define ICorProfilerInfo9_DoStackSnapshot(This,thread,callback,infoFlags,clientData,context,contextSize)    \
    ( (This)->lpVtbl -> DoStackSnapshot(This,thread,callback,infoFlags,clientData,context,contextSize) ) 

#define ICorProfilerInfo9_SetEnterLeaveFunctionHooks2(This,pFuncEnter,pFuncLeave,pFuncTailcall) \
    ( (This)->lpVtbl -> SetEnterLeaveFunctionHooks2(This,pFuncEnter,pFuncLeave,pFuncTailcall) ) 

#define ICorProfilerInfo9_GetFunctionInfo2(This,funcId,frameInfo,pClassId,pModuleId,pToken,cTypeArgs,pcTypeArgs,typeArgs)   \
    ( (This)->lpVtbl -> GetFunctionInfo2(This,funcId,frameInfo,pClassId,pModuleId,pToken,cTypeArgs,pcTypeArgs,typeArgs) ) 

#define ICorProfilerInfo9_GetStringLayout(This,pBufferLengthOffset,pStringLengthOffset,pBufferOffset)   \
    ( (This)->lpVtbl -> GetStringLayout(This,pBufferLengthOffset,pStringLengthOffset,pBufferOffset) ) 

#define ICorProfilerInfo9_GetClassLayout(This,classID,rFieldOffset,cFieldOffset,pcFieldOffset,pulClassSize) \
    ( (This)->lpVtbl -> GetClassLayout(This,classID,rFieldOffset,cFieldOffset,pcFieldOffset,pulClassSize) ) 

#define ICorProfilerInfo9_GetClassIDInfo2(This,classId,pModuleId,pTypeDefToken,pParentClassId,cNumTypeArgs,pcNumTypeArgs,typeArgs)  \
    ( (This)->lpVtbl -> GetClassIDInfo2(This,classId,pModuleId,pTypeDefToken,pParentClassId,cNumTypeArgs,pcNumTypeArgs,typeArgs) ) 

#define ICorProfilerInfo9_GetCodeInfo2(This,functionID,cCodeInfos,pcCodeInfos,codeInfos)    \
    ( (This)->lpVtbl -> GetCodeInfo2(This,functionID,cCodeInfos,pcCodeInfos,codeInfos) ) 

#define ICorProfilerInfo9_GetClassFromTokenAndTypeArgs(This,moduleID,typeDef,cTypeArgs,typeArgs,pClassID)   \
    ( (This)->lpVtbl -> GetClassFromTokenAndTypeArgs(This,moduleID,typeDef,cTypeArgs,typeArgs,pClassID) ) 

#define ICorProfilerInfo9_GetFunctionFromTokenAndTypeArgs(This,moduleID,funcDef,classId,cTypeArgs,typeArgs,pFunctionID) \
    ( (This)->lpVtbl -> GetFunctionFromTokenAndTypeArgs(This,moduleID,funcDef,classId,cTypeArgs,typeArgs,pFunctionID) ) 

#define ICorProfilerInfo9_EnumModuleFrozenObjects(This,moduleID,ppEnum) \
    ( (This)->lpVtbl -> EnumModuleFrozenObjects(This,moduleID,ppEnum) ) 

#define ICorProfilerInfo9_GetArrayObjectInfo(This,objectId,cDimensions,pDimensionSizes,pDimensionLowerBounds,ppData)    \
    ( (This)->lpVtbl -> GetArrayObjectInfo(This,objectId,cDimensions,pDimensionSizes,pDimensionLowerBounds,ppData) ) 

#define ICorProfilerInfo9_GetBoxClassLayout(This,classId,pBufferOffset) \
    ( (This)->lpVtbl -> GetBoxClassLayout(This,classId,pBufferOffset) ) 

#define ICorProfilerInfo9_GetThreadAppDomain(This,threadId,pAppDomainId)    \
    ( (This)->lpVtbl -> GetThreadAppDomain(This,threadId,pAppDomainId) ) 

#define ICorProfilerInfo9_GetRVAStaticAddress(This,classId,fieldToken,ppAddress)    \
    ( (This)->lpVtbl -> GetRVAStaticAddress(This,classId,fieldToken,ppAddress) ) 

#define ICorProfilerInfo9_GetAppDomainStaticAddress(This,classId,fieldToken,appDomainId,ppAddress)  \
    ( (This)->lpVtbl -> GetAppDomainStaticAddress(This,classId,fieldToken,appDomainId,ppAddress) ) 

#define ICorProfilerInfo9_GetThreadStaticAddress(This,classId,fieldToken,threadId,ppAddress)    \
    ( (This)->lpVtbl -> GetThreadStaticAddress(This,classId,fieldToken,threadId,ppAddress) ) 

#define ICorProfilerInfo9_GetContextStaticAddress(This,classId,fieldToken,contextId,ppAddress)  \
    ( (This)->lpVtbl -> GetContextStaticAddress(This,classId,fieldToken,contextId,ppAddress) ) 

#define ICorProfilerInfo9_GetStaticFieldInfo(This,classId,fieldToken,pFieldInfo)    \
    ( (This)->lpVtbl -> GetStaticFieldInfo(This,classId,fieldToken,pFieldInfo) ) 

#define ICorProfilerInfo9_GetGenerationBounds(This,cObjectRanges,pcObjectRanges,ranges) \
    ( (This)->lpVtbl -> GetGenerationBounds(This,cObjectRanges,pcObjectRanges,ranges) ) 

#define ICorProfilerInfo9_GetObjectGeneration(This,objectId,range)  \
    ( (This)->lpVtbl -> GetObjectGeneration(This,objectId,range) ) 

#define ICorProfilerInfo9_GetNotifiedExceptionClauseInfo(This,pinfo)    \
    ( (This)->lpVtbl -> GetNotifiedExceptionClauseInfo(This,pinfo) ) 


#define ICorProfilerInfo9_EnumJITedFunctions(This,ppEnum)   \
    ( (This)->lpVtbl -> EnumJITedFunctions(This,ppEnum) ) 

#define ICorProfilerInfo9_RequestProfilerDetach(This,dwExpectedCompletionMilliseconds)  \
    ( (This)->lpVtbl -> RequestProfilerDetach(This,dwExpectedCompletionMilliseconds) ) 

#define ICorProfilerInfo9_SetFunctionIDMapper2(This,pFunc,clientData)   \
    ( (This)->lpVtbl -> SetFunctionIDMapper2(This,pFunc,clientData) ) 

#define ICorProfilerInfo9_GetStringLayout2(This,pStringLengthOffset,pBufferOffset)  \
    ( (This)->lpVtbl -> GetStringLayout2(This,pStringLengthOffset,pBufferOffset) ) 

#define ICorProfilerInfo9_SetEnterLeaveFunctionHooks3(This,pFuncEnter3,pFuncLeave3,pFuncTailcall3)  \
    ( (This)->lpVtbl -> SetEnterLeaveFunctionHooks3(This,pFuncEnter3,pFuncLeave3,pFuncTailcall3) ) 

#define ICorProfilerInfo9_SetEnterLeaveFunctionHooks3WithInfo(This,pFuncEnter3WithInfo,pFuncLeave3WithInfo,pFuncTailcall3WithInfo)  \
    ( (This)->lpVtbl -> SetEnterLeaveFunctionHooks3WithInfo(This,pFuncEnter3WithInfo,pFuncLeave3WithInfo,pFuncTailcall3WithInfo) ) 

#define ICorProfilerInfo9_GetFunctionEnter3Info(This,functionId,eltInfo,pFrameInfo,pcbArgumentInfo,pArgumentInfo)   \
    ( (This)->lpVtbl -> GetFunctionEnter3Info(This,functionId,eltInfo,pFrameInfo,pcbArgumentInfo,pArgumentInfo) ) 

#define ICorProfilerInfo9_GetFunctionLeave3Info(This,functionId,eltInfo,pFrameInfo,pRetvalRange)    \
    ( (This)->lpVtbl -> GetFunctionLeave3Info(This,functionId,eltInfo,pFrameInfo,pRetvalRange) ) 

#define ICorProfilerInfo9_GetFunctionTailcall3Info(This,functionId,eltInfo,pFrameInfo)  \
    ( (This)->lpVtbl -> GetFunctionTailcall3Info(This,functionId,eltInfo,pFrameInfo) ) 

#define ICorProfilerInfo9_EnumModules(This,ppEnum)  \
    ( (This)->lpVtbl -> EnumModules(This,ppEnum) ) 

#define ICorProfilerInfo9_GetRuntimeInformation(This,pClrInstanceId,pRuntimeType,pMajorVersion,pMinorVersion,pBuildNumber,pQFEVersion,cchVersionString,pcchVersionString,szVersionString)   \
    ( (This)->lpVtbl -> GetRuntimeInformation(This,pClrInstanceId,pRuntimeType,pMajorVersion,pMinorVersion,pBuildNumber,pQFEVersion,cchVersionString,pcchVersionString,szVersionString) ) 

#define ICorProfilerInfo9_GetThreadStaticAddress2(This,classId,fieldToken,appDomainId,threadId,ppAddress)   \
    ( (This)->lpVtbl -> GetThreadStaticAddress2(This,classId,fieldToken,appDomainId,threadId,ppAddress) ) 

#define ICorProfilerInfo9_GetAppDomainsContainingModule(This,moduleId,cAppDomainIds,pcAppDomainIds,appDomainIds)    \
    ( (This)->lpVtbl -> GetAppDomainsContainingModule(This,moduleId,cAppDomainIds,pcAppDomainIds,appDomainIds) ) 

#define ICorProfilerInfo9_GetModuleInfo2(This,moduleId,ppBaseLoadAddress,cchName,pcchName,szName,pAssemblyId,pdwModuleFlags)    \
    ( (This)->lpVtbl -> GetModuleInfo2(This,moduleId,ppBaseLoadAddress,cchName,pcchName,szName,pAssemblyId,pdwModuleFlags) ) 


#define ICorProfilerInfo9_EnumThreads(This,ppEnum)  \
    ( (This)->lpVtbl -> EnumThreads(This,ppEnum) ) 

#define ICorProfilerInfo9_InitializeCurrentThread(This) \
    ( (This)->lpVtbl -> InitializeCurrentThread(This) ) 

#define ICorProfilerInfo9_RequestReJIT(This,cFunctions,moduleIds,methodIds) \
    ( (This)->lpVtbl -> RequestReJIT(This,cFunctions,moduleIds,methodIds) ) 

#define ICorProfilerInfo9_RequestRevert(This,cFunctions,moduleIds,methodIds,status) \
    ( (This)->lpVtbl -> RequestRevert(This,cFunctions,moduleIds,methodIds,status) ) 

#define ICorProfilerInfo9_GetCodeInfo3(This,functionID,reJitId,cCodeInfos,pcCodeInfos,codeInfos)    \
    ( (This)->lpVtbl -> GetCodeInfo3(This,functionID,reJitId,cCodeInfos,pcCodeInfos,codeInfos) ) 

#define ICorProfilerInfo9_GetFunctionFromIP2(This,ip,pFunctionId,pReJitId)  \
    ( (This)->lpVtbl -> GetFunctionFromIP2(This,ip,pFunctionId,pReJitId) ) 

#define ICorProfilerInfo9_GetReJITIDs(This,functionId,cReJitIds,pcReJitIds,reJitIds)    \
    ( (This)->lpVtbl -> GetReJITIDs(This,functionId,cReJitIds,pcReJitIds,reJitIds) ) 

#define ICorProfilerInfo9_GetILToNativeMapping2(This,functionId,reJitId,cMap,pcMap,map) \
    ( (This)->lpVtbl -> GetILToNativeMapping2(This,functionId,reJitId,cMap,pcMap,map) ) 

#define ICorProfilerInfo9_EnumJITedFunctions2(This,ppEnum)  \
    ( (This)->lpVtbl -> EnumJITedFunctions2(This,ppEnum) ) 

#define ICorProfilerInfo9_GetObjectSize2(This,objectId,pcSize)  \
    ( (This)->lpVtbl -> GetObjectSize2(This,objectId,pcSize) ) 


#define ICorProfilerInfo9_GetEventMask2(This,pdwEventsLow,pdwEventsHigh)    \
    ( (This)->lpVtbl -> GetEventMask2(This,pdwEventsLow,pdwEventsHigh) ) 

#define ICorProfilerInfo9_SetEventMask2(This,dwEventsLow,dwEventsHigh)  \
    ( (This)->lpVtbl -> SetEventMask2(This,dwEventsLow,dwEventsHigh) ) 


#define ICorProfilerInfo9_EnumNgenModuleMethodsInliningThisMethod(This,inlinersModuleId,inlineeModuleId,inlineeMethodId,incompleteData,ppEnum)  \
    ( (This)->lpVtbl -> EnumNgenModuleMethodsInliningThisMethod(This,inlinersModuleId,inlineeModuleId,inlineeMethodId,incompleteData,ppEnum) ) 


#define ICorProfilerInfo9_ApplyMetaData(This,moduleId)  \
    ( (This)->lpVtbl -> ApplyMetaData(This,moduleId) ) 

#define ICorProfilerInfo9_GetInMemorySymbolsLength(This,moduleId,pCountSymbolBytes) \
    ( (This)->lpVtbl -> GetInMemorySymbolsLength(This,moduleId,pCountSymbolBytes) ) 

#define ICorProfilerInfo9_ReadInMemorySymbols(This,moduleId,symbolsReadOffset,pSymbolBytes,countSymbolBytes,pCountSymbolBytesRead)  \
    ( (This)->lpVtbl -> ReadInMemorySymbols(This,moduleId,symbolsReadOffset,pSymbolBytes,countSymbolBytes,pCountSymbolBytesRead) ) 


#define ICorProfilerInfo9_IsFunctionDynamic(This,functionId,isDynamic)  \
    ( (This)->lpVtbl -> IsFunctionDynamic(This,functionId,isDynamic) ) 

#define ICorProfilerInfo9_GetFunctionFromIP3(This,ip,functionId,pReJitId)   \
    ( (This)->lpVtbl -> GetFunctionFromIP3(This,ip,functionId,pReJitId) ) 

#define ICorProfilerInfo9_GetDynamicFunctionInfo(This,functionId,moduleId,ppvSig,pbSig,cchName,pcchName,wszName)    \
    ( (This)->lpVtbl -> GetDynamicFunctionInfo(This,functionId,moduleId,ppvSig,pbSig,cchName,pcchName,wszName) ) 


#define ICorProfilerInfo9_GetNativeCodeStartAddresses(This,functionID,reJitId,cCodeStartAddresses,pcCodeStartAddresses,codeStartAddresses)  \
    ( (This)->lpVtbl -> GetNativeCodeStartAddresses(This,functionID,reJitId,cCodeStartAddresses,pcCodeStartAddresses,codeStartAddresses) ) 

#define ICorProfilerInfo9_GetILToNativeMapping3(This,pNativeCodeStartAddress,cMap,pcMap,map)    \
    ( (This)->lpVtbl -> GetILToNativeMapping3(This,pNativeCodeStartAddress,cMap,pcMap,map) ) 

#define ICorProfilerInfo9_GetCodeInfo4(This,pNativeCodeStartAddress,cCodeInfos,pcCodeInfos,codeInfos)   \
    ( (This)->lpVtbl -> GetCodeInfo4(This,pNativeCodeStartAddress,cCodeInfos,pcCodeInfos,codeInfos) ) 

#endif /* COBJMACROS */


#endif  /* C style interface */




#endif  /* __ICorProfilerInfo9_INTERFACE_DEFINED__ */


#ifndef __ICorProfilerInfo10_INTERFACE_DEFINED__
#define __ICorProfilerInfo10_INTERFACE_DEFINED__

/* interface ICorProfilerInfo10 */
/* [local][unique][uuid][object] */ 


EXTERN_C const IID IID_ICorProfilerInfo10;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("2F1B5152-C869-40C9-AA5F-3ABE026BD720")
    ICorProfilerInfo10 : public ICorProfilerInfo9
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE EnumerateObjectReferences( 
            ObjectID objectId,
            ObjectReferenceCallback callback,
            void *clientData) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE IsFrozenObject( 
            ObjectID objectId,
            BOOL *pbFrozen) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetLOHObjectSizeThreshold( 
            DWORD *pThreshold) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE RequestReJITWithInliners( 
            /* [in] */ DWORD dwRejitFlags,
            /* [in] */ ULONG cFunctions,
            /* [size_is][in] */ ModuleID moduleIds[  ],
            /* [size_is][in] */ mdMethodDef methodIds[  ]) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE SuspendRuntime( void) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE ResumeRuntime( void) = 0;
        
    };
    
    
#else   /* C style interface */

    typedef struct ICorProfilerInfo10Vtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            ICorProfilerInfo10 * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            ICorProfilerInfo10 * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            ICorProfilerInfo10 * This);
        
        HRESULT ( STDMETHODCALLTYPE *GetClassFromObject )( 
            ICorProfilerInfo10 * This,
            /* [in] */ ObjectID objectId,
            /* [out] */ ClassID *pClassId);
        
        HRESULT ( STDMETHODCALLTYPE *GetClassFromToken )( 
            ICorProfilerInfo10 * This,
            /* [in] */ ModuleID moduleId,
            /* [in] */ mdTypeDef typeDef,
            /* [out] */ ClassID *pClassId);
        
        HRESULT ( STDMETHODCALLTYPE *GetCodeInfo )( 
            ICorProfilerInfo10 * This,
            /* [in] */ FunctionID functionId,
            /* [out] */ LPCBYTE *pStart,
            /* [out] */ ULONG *pcSize);
        
        HRESULT ( STDMETHODCALLTYPE *GetEventMask )( 
            ICorProfilerInfo10 * This,
            /* [out] */ DWORD *pdwEvents);
        
        HRESULT ( STDMETHODCALLTYPE *GetFunctionFromIP )( 
            ICorProfilerInfo10 * This,
            /* [in] */ LPCBYTE ip,
            /* [out] */ FunctionID *pFunctionId);
        
        HRESULT ( STDMETHODCALLTYPE *GetFunctionFromToken )( 
            ICorProfilerInfo10 * This,
            /* [in] */ ModuleID moduleId,
            /* [in] */ mdToken token,
            /* [out] */ FunctionID *pFunctionId);
        
        HRESULT ( STDMETHODCALLTYPE *GetHandleFromThread )( 
            ICorProfilerInfo10 * This,
            /* [in] */ ThreadID threadId,
            /* [out] */ HANDLE *phThread);
        
        HRESULT ( STDMETHODCALLTYPE *GetObjectSize )( 
            ICorProfilerInfo10 * This,
            /* [in] */ ObjectID objectId,
            /* [out] */ ULONG *pcSize);
        
        HRESULT ( STDMETHODCALLTYPE *IsArrayClass )( 
            ICorProfilerInfo10 * This,
            /* [in] */ ClassID classId,
            /* [out] */ CorElementType *pBaseElemType,
            /* [out] */ ClassID *pBaseClassId,
            /* [out] */ ULONG *pcRank);
        
        HRESULT ( STDMETHODCALLTYPE *GetThreadInfo )( 
            ICorProfilerInfo10 * This,
            /* [in] */ ThreadID threadId,
            /* [out] */ DWORD *pdwWin32ThreadId);
        
        HRESULT ( STDMETHODCALLTYPE *GetCurrentThreadID )( 
            ICorProfilerInfo10 * This,
            /* [out] */ ThreadID *pThreadId);
        
        HRESULT ( STDMETHODCALLTYPE *GetClassIDInfo )( 
            ICorProfilerInfo10 * This,
            /* [in] */ ClassID classId,
            /* [out] */ ModuleID *pModuleId,
            /* [out] */ mdTypeDef *pTypeDefToken);
        
        HRESULT ( STDMETHODCALLTYPE *GetFunctionInfo )( 
            ICorProfilerInfo10 * This,
            /* [in] */ FunctionID functionId,
            /* [out] */ ClassID *pClassId,
            /* [out] */ ModuleID *pModuleId,
            /* [out] */ mdToken *pToken);
        
        HRESULT ( STDMETHODCALLTYPE *SetEventMask )( 
            ICorProfilerInfo10 * This,
            /* [in] */ DWORD dwEvents);
        
        HRESULT ( STDMETHODCALLTYPE *SetEnterLeaveFunctionHooks )( 
            ICorProfilerInfo10 * This,
            /* [in] */ FunctionEnter *pFuncEnter,
            /* [in] */ FunctionLeave *pFuncLeave,
            /* [in] */ FunctionTailcall *pFuncTailcall);
        
        HRESULT ( STDMETHODCALLTYPE *SetFunctionIDMapper )( 
            ICorProfilerInfo10 * This,
            /* [in] */ FunctionIDMapper *pFunc);
        
        HRESULT ( STDMETHODCALLTYPE *GetTokenAndMetaDataFromFunction )( 
            ICorProfilerInfo10 * This,
            /* [in] */ FunctionID functionId,
            /* [in] */ REFIID riid,
            /* [out] */ IUnknown **ppImport,
            /* [out] */ mdToken *pToken);
        
        HRESULT ( STDMETHODCALLTYPE *GetModuleInfo )( 
            ICorProfilerInfo10 * This,
            /* [in] */ ModuleID moduleId,
            /* [out] */ LPCBYTE *ppBaseLoadAddress,
            /* [in] */ ULONG cchName,
            /* [out] */ ULONG *pcchName,
            /* [annotation][out] */ 
            _Out_writes_to_(cchName, *pcchName)  WCHAR szName[  ],
            /* [out] */ AssemblyID *pAssemblyId);
        
        HRESULT ( STDMETHODCALLTYPE *GetModuleMetaData )( 
            ICorProfilerInfo10 * This,
            /* [in] */ ModuleID moduleId,
            /* [in] */ DWORD dwOpenFlags,
            /* [in] */ REFIID riid,
            /* [out] */ IUnknown **ppOut);
        
        HRESULT ( STDMETHODCALLTYPE *GetILFunctionBody )( 
            ICorProfilerInfo10 * This,
            /* [in] */ ModuleID moduleId,
            /* [in] */ mdMethodDef methodId,
            /* [out] */ LPCBYTE *ppMethodHeader,
            /* [out] */ ULONG *pcbMethodSize);
        
        HRESULT ( STDMETHODCALLTYPE *GetILFunctionBodyAllocator )( 
            ICorProfilerInfo10 * This,
            /* [in] */ ModuleID moduleId,
            /* [out] */ IMethodMalloc **ppMalloc);
        
        HRESULT ( STDMETHODCALLTYPE *SetILFunctionBody )( 
            ICorProfilerInfo10 * This,
            /* [in] */ ModuleID moduleId,
            /* [in] */ mdMethodDef methodid,
            /* [in] */ LPCBYTE pbNewILMethodHeader);
        
        HRESULT ( STDMETHODCALLTYPE *GetAppDomainInfo )( 
            ICorProfilerInfo10 * This,
            /* [in] */ AppDomainID appDomainId,
            /* [in] */ ULONG cchName,
            /* [out] */ ULONG *pcchName,
            /* [annotation][out] */ 
            _Out_writes_to_(cchName, *pcchName)  WCHAR szName[  ],
            /* [out] */ ProcessID *pProcessId);
        
        HRESULT ( STDMETHODCALLTYPE *GetAssemblyInfo )( 
            ICorProfilerInfo10 * This,
            /* [in] */ AssemblyID assemblyId,
            /* [in] */ ULONG cchName,
            /* [out] */ ULONG *pcchName,
            /* [annotation][out] */ 
            _Out_writes_to_(cchName, *pcchName)  WCHAR szName[  ],
            /* [out] */ AppDomainID *pAppDomainId,
            /* [out] */ ModuleID *pModuleId);
        
        HRESULT ( STDMETHODCALLTYPE *SetFunctionReJIT )( 
            ICorProfilerInfo10 * This,
            /* [in] */ FunctionID functionId);
        
        HRESULT ( STDMETHODCALLTYPE *ForceGC )( 
            ICorProfilerInfo10 * This);
        
        HRESULT ( STDMETHODCALLTYPE *SetILInstrumentedCodeMap )( 
            ICorProfilerInfo10 * This,
            /* [in] */ FunctionID functionId,
            /* [in] */ BOOL fStartJit,
            /* [in] */ ULONG cILMapEntries,
            /* [size_is][in] */ COR_IL_MAP rgILMapEntries[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *GetInprocInspectionInterface )( 
            ICorProfilerInfo10 * This,
            /* [out] */ IUnknown **ppicd);
        
        HRESULT ( STDMETHODCALLTYPE *GetInprocInspectionIThisThread )( 
            ICorProfilerInfo10 * This,
            /* [out] */ IUnknown **ppicd);
        
        HRESULT ( STDMETHODCALLTYPE *GetThreadContext )( 
            ICorProfilerInfo10 * This,
            /* [in] */ ThreadID threadId,
            /* [out] */ ContextID *pContextId);
        
        HRESULT ( STDMETHODCALLTYPE *BeginInprocDebugging )( 
            ICorProfilerInfo10 * This,
            /* [in] */ BOOL fThisThreadOnly,
            /* [out] */ DWORD *pdwProfilerContext);
        
        HRESULT ( STDMETHODCALLTYPE *EndInprocDebugging )( 
            ICorProfilerInfo10 * This,
            /* [in] */ DWORD dwProfilerContext);
        
        HRESULT ( STDMETHODCALLTYPE *GetILToNativeMapping )( 
            ICorProfilerInfo10 * This,
            /* [in] */ FunctionID functionId,
            /* [in] */ ULONG32 cMap,
            /* [out] */ ULONG32 *pcMap,
            /* [length_is][size_is][out] */ COR_DEBUG_IL_TO_NATIVE_MAP map[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *DoStackSnapshot )( 
            ICorProfilerInfo10 * This,
            /* [in] */ ThreadID thread,
            /* [in] */ StackSnapshotCallback *callback,
            /* [in] */ ULONG32 infoFlags,
            /* [in] */ void *clientData,
            /* [size_is][in] */ BYTE context[  ],
            /* [in] */ ULONG32 contextSize);
        
        HRESULT ( STDMETHODCALLTYPE *SetEnterLeaveFunctionHooks2 )( 
            ICorProfilerInfo10 * This,
            /* [in] */ FunctionEnter2 *pFuncEnter,
            /* [in] */ FunctionLeave2 *pFuncLeave,
            /* [in] */ FunctionTailcall2 *pFuncTailcall);
        
        HRESULT ( STDMETHODCALLTYPE *GetFunctionInfo2 )( 
            ICorProfilerInfo10 * This,
            /* [in] */ FunctionID funcId,
            /* [in] */ COR_PRF_FRAME_INFO frameInfo,
            /* [out] */ ClassID *pClassId,
            /* [out] */ ModuleID *pModuleId,
            /* [out] */ mdToken *pToken,
            /* [in] */ ULONG32 cTypeArgs,
            /* [out] */ ULONG32 *pcTypeArgs,
            /* [out] */ ClassID typeArgs[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *GetStringLayout )( 
            ICorProfilerInfo10 * This,
            /* [out] */ ULONG *pBufferLengthOffset,
            /* [out] */ ULONG *pStringLengthOffset,
            /* [out] */ ULONG *pBufferOffset);
        
        HRESULT ( STDMETHODCALLTYPE *GetClassLayout )( 
            ICorProfilerInfo10 * This,
            /* [in] */ ClassID classID,
            /* [out][in] */ COR_FIELD_OFFSET rFieldOffset[  ],
            /* [in] */ ULONG cFieldOffset,
            /* [out] */ ULONG *pcFieldOffset,
            /* [out] */ ULONG *pulClassSize);
        
        HRESULT ( STDMETHODCALLTYPE *GetClassIDInfo2 )( 
            ICorProfilerInfo10 * This,
            /* [in] */ ClassID classId,
            /* [out] */ ModuleID *pModuleId,
            /* [out] */ mdTypeDef *pTypeDefToken,
            /* [out] */ ClassID *pParentClassId,
            /* [in] */ ULONG32 cNumTypeArgs,
            /* [out] */ ULONG32 *pcNumTypeArgs,
            /* [out] */ ClassID typeArgs[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *GetCodeInfo2 )( 
            ICorProfilerInfo10 * This,
            /* [in] */ FunctionID functionID,
            /* [in] */ ULONG32 cCodeInfos,
            /* [out] */ ULONG32 *pcCodeInfos,
            /* [length_is][size_is][out] */ COR_PRF_CODE_INFO codeInfos[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *GetClassFromTokenAndTypeArgs )( 
            ICorProfilerInfo10 * This,
            /* [in] */ ModuleID moduleID,
            /* [in] */ mdTypeDef typeDef,
            /* [in] */ ULONG32 cTypeArgs,
            /* [size_is][in] */ ClassID typeArgs[  ],
            /* [out] */ ClassID *pClassID);
        
        HRESULT ( STDMETHODCALLTYPE *GetFunctionFromTokenAndTypeArgs )( 
            ICorProfilerInfo10 * This,
            /* [in] */ ModuleID moduleID,
            /* [in] */ mdMethodDef funcDef,
            /* [in] */ ClassID classId,
            /* [in] */ ULONG32 cTypeArgs,
            /* [size_is][in] */ ClassID typeArgs[  ],
            /* [out] */ FunctionID *pFunctionID);
        
        HRESULT ( STDMETHODCALLTYPE *EnumModuleFrozenObjects )( 
            ICorProfilerInfo10 * This,
            /* [in] */ ModuleID moduleID,
            /* [out] */ ICorProfilerObjectEnum **ppEnum);
        
        HRESULT ( STDMETHODCALLTYPE *GetArrayObjectInfo )( 
            ICorProfilerInfo10 * This,
            /* [in] */ ObjectID objectId,
            /* [in] */ ULONG32 cDimensions,
            /* [size_is][out] */ ULONG32 pDimensionSizes[  ],
            /* [size_is][out] */ int pDimensionLowerBounds[  ],
            /* [out] */ BYTE **ppData);
        
        HRESULT ( STDMETHODCALLTYPE *GetBoxClassLayout )( 
            ICorProfilerInfo10 * This,
            /* [in] */ ClassID classId,
            /* [out] */ ULONG32 *pBufferOffset);
        
        HRESULT ( STDMETHODCALLTYPE *GetThreadAppDomain )( 
            ICorProfilerInfo10 * This,
            /* [in] */ ThreadID threadId,
            /* [out] */ AppDomainID *pAppDomainId);
        
        HRESULT ( STDMETHODCALLTYPE *GetRVAStaticAddress )( 
            ICorProfilerInfo10 * This,
            /* [in] */ ClassID classId,
            /* [in] */ mdFieldDef fieldToken,
            /* [out] */ void **ppAddress);
        
        HRESULT ( STDMETHODCALLTYPE *GetAppDomainStaticAddress )( 
            ICorProfilerInfo10 * This,
            /* [in] */ ClassID classId,
            /* [in] */ mdFieldDef fieldToken,
            /* [in] */ AppDomainID appDomainId,
            /* [out] */ void **ppAddress);
        
        HRESULT ( STDMETHODCALLTYPE *GetThreadStaticAddress )( 
            ICorProfilerInfo10 * This,
            /* [in] */ ClassID classId,
            /* [in] */ mdFieldDef fieldToken,
            /* [in] */ ThreadID threadId,
            /* [out] */ void **ppAddress);
        
        HRESULT ( STDMETHODCALLTYPE *GetContextStaticAddress )( 
            ICorProfilerInfo10 * This,
            /* [in] */ ClassID classId,
            /* [in] */ mdFieldDef fieldToken,
            /* [in] */ ContextID contextId,
            /* [out] */ void **ppAddress);
        
        HRESULT ( STDMETHODCALLTYPE *GetStaticFieldInfo )( 
            ICorProfilerInfo10 * This,
            /* [in] */ ClassID classId,
            /* [in] */ mdFieldDef fieldToken,
            /* [out] */ COR_PRF_STATIC_TYPE *pFieldInfo);
        
        HRESULT ( STDMETHODCALLTYPE *GetGenerationBounds )( 
            ICorProfilerInfo10 * This,
            /* [in] */ ULONG cObjectRanges,
            /* [out] */ ULONG *pcObjectRanges,
            /* [length_is][size_is][out] */ COR_PRF_GC_GENERATION_RANGE ranges[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *GetObjectGeneration )( 
            ICorProfilerInfo10 * This,
            /* [in] */ ObjectID objectId,
            /* [out] */ COR_PRF_GC_GENERATION_RANGE *range);
        
        HRESULT ( STDMETHODCALLTYPE *GetNotifiedExceptionClauseInfo )( 
            ICorProfilerInfo10 * This,
            /* [out] */ COR_PRF_EX_CLAUSE_INFO *pinfo);
        
        HRESULT ( STDMETHODCALLTYPE *EnumJITedFunctions )( 
            ICorProfilerInfo10 * This,
            /* [out] */ ICorProfilerFunctionEnum **ppEnum);
        
        HRESULT ( STDMETHODCALLTYPE *RequestProfilerDetach )( 
            ICorProfilerInfo10 * This,
            /* [in] */ DWORD dwExpectedCompletionMilliseconds);
        
        HRESULT ( STDMETHODCALLTYPE *SetFunctionIDMapper2 )( 
            ICorProfilerInfo10 * This,
            /* [in] */ FunctionIDMapper2 *pFunc,
            /* [in] */ void *clientData);
        
        HRESULT ( STDMETHODCALLTYPE *GetStringLayout2 )( 
            ICorProfilerInfo10 * This,
            /* [out] */ ULONG *pStringLengthOffset,
            /* [out] */ ULONG *pBufferOffset);
        
        HRESULT ( STDMETHODCALLTYPE *SetEnterLeaveFunctionHooks3 )( 
            ICorProfilerInfo10 * This,
            /* [in] */ FunctionEnter3 *pFuncEnter3,
            /* [in] */ FunctionLeave3 *pFuncLeave3,
            /* [in] */ FunctionTailcall3 *pFuncTailcall3);
        
        HRESULT ( STDMETHODCALLTYPE *SetEnterLeaveFunctionHooks3WithInfo )( 
            ICorProfilerInfo10 * This,
            /* [in] */ FunctionEnter3WithInfo *pFuncEnter3WithInfo,
            /* [in] */ FunctionLeave3WithInfo *pFuncLeave3WithInfo,
            /* [in] */ FunctionTailcall3WithInfo *pFuncTailcall3WithInfo);
        
        HRESULT ( STDMETHODCALLTYPE *GetFunctionEnter3Info )( 
            ICorProfilerInfo10 * This,
            /* [in] */ FunctionID functionId,
            /* [in] */ COR_PRF_ELT_INFO eltInfo,
            /* [out] */ COR_PRF_FRAME_INFO *pFrameInfo,
            /* [out][in] */ ULONG *pcbArgumentInfo,
            /* [size_is][out] */ COR_PRF_FUNCTION_ARGUMENT_INFO *pArgumentInfo);
        
        HRESULT ( STDMETHODCALLTYPE *GetFunctionLeave3Info )( 
            ICorProfilerInfo10 * This,
            /* [in] */ FunctionID functionId,
            /* [in] */ COR_PRF_ELT_INFO eltInfo,
            /* [out] */ COR_PRF_FRAME_INFO *pFrameInfo,
            /* [out] */ COR_PRF_FUNCTION_ARGUMENT_RANGE *pRetvalRange);
        
        HRESULT ( STDMETHODCALLTYPE *GetFunctionTailcall3Info )( 
            ICorProfilerInfo10 * This,
            /* [in] */ FunctionID functionId,
            /* [in] */ COR_PRF_ELT_INFO eltInfo,
            /* [out] */ COR_PRF_FRAME_INFO *pFrameInfo);
        
        HRESULT ( STDMETHODCALLTYPE *EnumModules )( 
            ICorProfilerInfo10 * This,
            /* [out] */ ICorProfilerModuleEnum **ppEnum);
        
        HRESULT ( STDMETHODCALLTYPE *GetRuntimeInformation )( 
            ICorProfilerInfo10 * This,
            /* [out] */ USHORT *pClrInstanceId,
            /* [out] */ COR_PRF_RUNTIME_TYPE *pRuntimeType,
            /* [out] */ USHORT *pMajorVersion,
            /* [out] */ USHORT *pMinorVersion,
            /* [out] */ USHORT *pBuildNumber,
            /* [out] */ USHORT *pQFEVersion,
            /* [in] */ ULONG cchVersionString,
            /* [out] */ ULONG *pcchVersionString,
            /* [annotation][out] */ 
            _Out_writes_to_(cchVersionString, *pcchVersionString)  WCHAR szVersionString[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *GetThreadStaticAddress2 )( 
            ICorProfilerInfo10 * This,
            /* [in] */ ClassID classId,
            /* [in] */ mdFieldDef fieldToken,
            /* [in] */ AppDomainID appDomainId,
            /* [in] */ ThreadID threadId,
            /* [out] */ void **ppAddress);
        
        HRESULT ( STDMETHODCALLTYPE *GetAppDomainsContainingModule )( 
            ICorProfilerInfo10 * This,
            /* [in] */ ModuleID moduleId,
            /* [in] */ ULONG32 cAppDomainIds,
            /* [out] */ ULONG32 *pcAppDomainIds,
            /* [length_is][size_is][out] */ AppDomainID appDomainIds[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *GetModuleInfo2 )( 
            ICorProfilerInfo10 * This,
            /* [in] */ ModuleID moduleId,
            /* [out] */ LPCBYTE *ppBaseLoadAddress,
            /* [in] */ ULONG cchName,
            /* [out] */ ULONG *pcchName,
            /* [annotation][out] */ 
            _Out_writes_to_(cchName, *pcchName)  WCHAR szName[  ],
            /* [out] */ AssemblyID *pAssemblyId,
            /* [out] */ DWORD *pdwModuleFlags);
        
        HRESULT ( STDMETHODCALLTYPE *EnumThreads )( 
            ICorProfilerInfo10 * This,
            /* [out] */ ICorProfilerThreadEnum **ppEnum);
        
        HRESULT ( STDMETHODCALLTYPE *InitializeCurrentThread )( 
            ICorProfilerInfo10 * This);
        
        HRESULT ( STDMETHODCALLTYPE *RequestReJIT )( 
            ICorProfilerInfo10 * This,
            /* [in] */ ULONG cFunctions,
            /* [size_is][in] */ ModuleID moduleIds[  ],
            /* [size_is][in] */ mdMethodDef methodIds[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *RequestRevert )( 
            ICorProfilerInfo10 * This,
            /* [in] */ ULONG cFunctions,
            /* [size_is][in] */ ModuleID moduleIds[  ],
            /* [size_is][in] */ mdMethodDef methodIds[  ],
            /* [size_is][out] */ HRESULT status[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *GetCodeInfo3 )( 
            ICorProfilerInfo10 * This,
            /* [in] */ FunctionID functionID,
            /* [in] */ ReJITID reJitId,
            /* [in] */ ULONG32 cCodeInfos,
            /* [out] */ ULONG32 *pcCodeInfos,
            /* [length_is][size_is][out] */ COR_PRF_CODE_INFO codeInfos[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *GetFunctionFromIP2 )( 
            ICorProfilerInfo10 * This,
            /* [in] */ LPCBYTE ip,
            /* [out] */ FunctionID *pFunctionId,
            /* [out] */ ReJITID *pReJitId);
        
        HRESULT ( STDMETHODCALLTYPE *GetReJITIDs )( 
            ICorProfilerInfo10 * This,
            /* [in] */ FunctionID functionId,
            /* [in] */ ULONG cReJitIds,
            /* [out] */ ULONG *pcReJitIds,
            /* [length_is][size_is][out] */ ReJITID reJitIds[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *GetILToNativeMapping2 )( 
            ICorProfilerInfo10 * This,
            /* [in] */ FunctionID functionId,
            /* [in] */ ReJITID reJitId,
            /* [in] */ ULONG32 cMap,
            /* [out] */ ULONG32 *pcMap,
            /* [length_is][size_is][out] */ COR_DEBUG_IL_TO_NATIVE_MAP map[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *EnumJITedFunctions2 )( 
            ICorProfilerInfo10 * This,
            /* [out] */ ICorProfilerFunctionEnum **ppEnum);
        
        HRESULT ( STDMETHODCALLTYPE *GetObjectSize2 )( 
            ICorProfilerInfo10 * This,
            /* [in] */ ObjectID objectId,
            /* [out] */ SIZE_T *pcSize);
        
        HRESULT ( STDMETHODCALLTYPE *GetEventMask2 )( 
            ICorProfilerInfo10 * This,
            /* [out] */ DWORD *pdwEventsLow,
            /* [out] */ DWORD *pdwEventsHigh);
        
        HRESULT ( STDMETHODCALLTYPE *SetEventMask2 )( 
            ICorProfilerInfo10 * This,
            /* [in] */ DWORD dwEventsLow,
            /* [in] */ DWORD dwEventsHigh);
        
        HRESULT ( STDMETHODCALLTYPE *EnumNgenModuleMethodsInliningThisMethod )( 
            ICorProfilerInfo10 * This,
            /* [in] */ ModuleID inlinersModuleId,
            /* [in] */ ModuleID inlineeModuleId,
            /* [in] */ mdMethodDef inlineeMethodId,
            /* [out] */ BOOL *incompleteData,
            /* [out] */ ICorProfilerMethodEnum **ppEnum);
        
        HRESULT ( STDMETHODCALLTYPE *ApplyMetaData )( 
            ICorProfilerInfo10 * This,
            /* [in] */ ModuleID moduleId);
        
        HRESULT ( STDMETHODCALLTYPE *GetInMemorySymbolsLength )( 
            ICorProfilerInfo10 * This,
            /* [in] */ ModuleID moduleId,
            /* [out] */ DWORD *pCountSymbolBytes);
        
        HRESULT ( STDMETHODCALLTYPE *ReadInMemorySymbols )( 
            ICorProfilerInfo10 * This,
            /* [in] */ ModuleID moduleId,
            /* [in] */ DWORD symbolsReadOffset,
            /* [out] */ BYTE *pSymbolBytes,
            /* [in] */ DWORD countSymbolBytes,
            /* [out] */ DWORD *pCountSymbolBytesRead);
        
        HRESULT ( STDMETHODCALLTYPE *IsFunctionDynamic )( 
            ICorProfilerInfo10 * This,
            /* [in] */ FunctionID functionId,
            /* [out] */ BOOL *isDynamic);
        
        HRESULT ( STDMETHODCALLTYPE *GetFunctionFromIP3 )( 
            ICorProfilerInfo10 * This,
            /* [in] */ LPCBYTE ip,
            /* [out] */ FunctionID *functionId,
            /* [out] */ ReJITID *pReJitId);
        
        HRESULT ( STDMETHODCALLTYPE *GetDynamicFunctionInfo )( 
            ICorProfilerInfo10 * This,
            /* [in] */ FunctionID functionId,
            /* [out] */ ModuleID *moduleId,
            /* [out] */ PCCOR_SIGNATURE *ppvSig,
            /* [out] */ ULONG *pbSig,
            /* [in] */ ULONG cchName,
            /* [out] */ ULONG *pcchName,
            /* [out] */ WCHAR wszName[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *GetNativeCodeStartAddresses )( 
            ICorProfilerInfo10 * This,
            FunctionID functionID,
            ReJITID reJitId,
            ULONG32 cCodeStartAddresses,
            ULONG32 *pcCodeStartAddresses,
            UINT_PTR codeStartAddresses[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *GetILToNativeMapping3 )( 
            ICorProfilerInfo10 * This,
            UINT_PTR pNativeCodeStartAddress,
            ULONG32 cMap,
            ULONG32 *pcMap,
            COR_DEBUG_IL_TO_NATIVE_MAP map[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *GetCodeInfo4 )( 
            ICorProfilerInfo10 * This,
            UINT_PTR pNativeCodeStartAddress,
            ULONG32 cCodeInfos,
            ULONG32 *pcCodeInfos,
            COR_PRF_CODE_INFO codeInfos[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *EnumerateObjectReferences )( 
            ICorProfilerInfo10 * This,
            ObjectID objectId,
            ObjectReferenceCallback callback,
            void *clientData);
        
        HRESULT ( STDMETHODCALLTYPE *IsFrozenObject )( 
            ICorProfilerInfo10 * This,
            ObjectID objectId,
            BOOL *pbFrozen);
        
        HRESULT ( STDMETHODCALLTYPE *GetLOHObjectSizeThreshold )( 
            ICorProfilerInfo10 * This,
            DWORD *pThreshold);
        
        HRESULT ( STDMETHODCALLTYPE *RequestReJITWithInliners )( 
            ICorProfilerInfo10 * This,
            /* [in] */ DWORD dwRejitFlags,
            /* [in] */ ULONG cFunctions,
            /* [size_is][in] */ ModuleID moduleIds[  ],
            /* [size_is][in] */ mdMethodDef methodIds[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *SuspendRuntime )( 
            ICorProfilerInfo10 * This);
        
        HRESULT ( STDMETHODCALLTYPE *ResumeRuntime )( 
            ICorProfilerInfo10 * This);
        
        END_INTERFACE
    } ICorProfilerInfo10Vtbl;

    interface ICorProfilerInfo10
    {
        CONST_VTBL struct ICorProfilerInfo10Vtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define ICorProfilerInfo10_QueryInterface(This,riid,ppvObject)  \
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define ICorProfilerInfo10_AddRef(This) \
    ( (This)->lpVtbl -> AddRef(This) ) 

#define ICorProfilerInfo10_Release(This)    \
    ( (This)->lpVtbl -> Release(This) ) 


#define ICorProfilerInfo10_GetClassFromObject(This,objectId,pClassId)   \
    ( (This)->lpVtbl -> GetClassFromObject(This,objectId,pClassId) ) 

#define ICorProfilerInfo10_GetClassFromToken(This,moduleId,typeDef,pClassId)    \
    ( (This)->lpVtbl -> GetClassFromToken(This,moduleId,typeDef,pClassId) ) 

#define ICorProfilerInfo10_GetCodeInfo(This,functionId,pStart,pcSize)   \
    ( (This)->lpVtbl -> GetCodeInfo(This,functionId,pStart,pcSize) ) 

#define ICorProfilerInfo10_GetEventMask(This,pdwEvents) \
    ( (This)->lpVtbl -> GetEventMask(This,pdwEvents) ) 

#define ICorProfilerInfo10_GetFunctionFromIP(This,ip,pFunctionId)   \
    ( (This)->lpVtbl -> GetFunctionFromIP(This,ip,pFunctionId) ) 

#define ICorProfilerInfo10_GetFunctionFromToken(This,moduleId,token,pFunctionId)    \
    ( (This)->lpVtbl -> GetFunctionFromToken(This,moduleId,token,pFunctionId) ) 

#define ICorProfilerInfo10_GetHandleFromThread(This,threadId,phThread)  \
    ( (This)->lpVtbl -> GetHandleFromThread(This,threadId,phThread) ) 

#define ICorProfilerInfo10_GetObjectSize(This,objectId,pcSize)  \
    ( (This)->lpVtbl -> GetObjectSize(This,objectId,pcSize) ) 

#define ICorProfilerInfo10_IsArrayClass(This,classId,pBaseElemType,pBaseClassId,pcRank) \
    ( (This)->lpVtbl -> IsArrayClass(This,classId,pBaseElemType,pBaseClassId,pcRank) ) 

#define ICorProfilerInfo10_GetThreadInfo(This,threadId,pdwWin32ThreadId)    \
    ( (This)->lpVtbl -> GetThreadInfo(This,threadId,pdwWin32ThreadId) ) 

#define ICorProfilerInfo10_GetCurrentThreadID(This,pThreadId)   \
    ( (This)->lpVtbl -> GetCurrentThreadID(This,pThreadId) ) 

#define ICorProfilerInfo10_GetClassIDInfo(This,classId,pModuleId,pTypeDefToken) \
    ( (This)->lpVtbl -> GetClassIDInfo(This,classId,pModuleId,pTypeDefToken) ) 

#define ICorProfilerInfo10_GetFunctionInfo(This,functionId,pClassId,pModuleId,pToken)   \
    ( (This)->lpVtbl -> GetFunctionInfo(This,functionId,pClassId,pModuleId,pToken) ) 

#define ICorProfilerInfo10_SetEventMask(This,dwEvents)  \
    ( (This)->lpVtbl -> SetEventMask(This,dwEvents) ) 

#define ICorProfilerInfo10_SetEnterLeaveFunctionHooks(This,pFuncEnter,pFuncLeave,pFuncTailcall) \
    ( (This)->lpVtbl -> SetEnterLeaveFunctionHooks(This,pFuncEnter,pFuncLeave,pFuncTailcall) ) 

#define ICorProfilerInfo10_SetFunctionIDMapper(This,pFunc)  \
    ( (This)->lpVtbl -> SetFunctionIDMapper(This,pFunc) ) 

#define ICorProfilerInfo10_GetTokenAndMetaDataFromFunction(This,functionId,riid,ppImport,pToken)    \
    ( (This)->lpVtbl -> GetTokenAndMetaDataFromFunction(This,functionId,riid,ppImport,pToken) ) 

#define ICorProfilerInfo10_GetModuleInfo(This,moduleId,ppBaseLoadAddress,cchName,pcchName,szName,pAssemblyId)   \
    ( (This)->lpVtbl -> GetModuleInfo(This,moduleId,ppBaseLoadAddress,cchName,pcchName,szName,pAssemblyId) ) 

#define ICorProfilerInfo10_GetModuleMetaData(This,moduleId,dwOpenFlags,riid,ppOut)  \
    ( (This)->lpVtbl -> GetModuleMetaData(This,moduleId,dwOpenFlags,riid,ppOut) ) 

#define ICorProfilerInfo10_GetILFunctionBody(This,moduleId,methodId,ppMethodHeader,pcbMethodSize)   \
    ( (This)->lpVtbl -> GetILFunctionBody(This,moduleId,methodId,ppMethodHeader,pcbMethodSize) ) 

#define ICorProfilerInfo10_GetILFunctionBodyAllocator(This,moduleId,ppMalloc)   \
    ( (This)->lpVtbl -> GetILFunctionBodyAllocator(This,moduleId,ppMalloc) ) 

#define ICorProfilerInfo10_SetILFunctionBody(This,moduleId,methodid,pbNewILMethodHeader)    \
    ( (This)->lpVtbl -> SetILFunctionBody(This,moduleId,methodid,pbNewILMethodHeader) ) 

#define ICorProfilerInfo10_GetAppDomainInfo(This,appDomainId,cchName,pcchName,szName,pProcessId)    \
    ( (This)->lpVtbl -> GetAppDomainInfo(This,appDomainId,cchName,pcchName,szName,pProcessId) ) 

#define ICorProfilerInfo10_GetAssemblyInfo(This,assemblyId,cchName,pcchName,szName,pAppDomainId,pModuleId)  \
    ( (This)->lpVtbl -> GetAssemblyInfo(This,assemblyId,cchName,pcchName,szName,pAppDomainId,pModuleId) ) 

#define ICorProfilerInfo10_SetFunctionReJIT(This,functionId)    \
    ( (This)->lpVtbl -> SetFunctionReJIT(This,functionId) ) 

#define ICorProfilerInfo10_ForceGC(This)    \
    ( (This)->lpVtbl -> ForceGC(This) ) 

#define ICorProfilerInfo10_SetILInstrumentedCodeMap(This,functionId,fStartJit,cILMapEntries,rgILMapEntries) \
    ( (This)->lpVtbl -> SetILInstrumentedCodeMap(This,functionId,fStartJit,cILMapEntries,rgILMapEntries) ) 

#define ICorProfilerInfo10_GetInprocInspectionInterface(This,ppicd) \
    ( (This)->lpVtbl -> GetInprocInspectionInterface(This,ppicd) ) 

#define ICorProfilerInfo10_GetInprocInspectionIThisThread(This,ppicd)   \
    ( (This)->lpVtbl -> GetInprocInspectionIThisThread(This,ppicd) ) 

#define ICorProfilerInfo10_GetThreadContext(This,threadId,pContextId)   \
    ( (This)->lpVtbl -> GetThreadContext(This,threadId,pContextId) ) 

#define ICorProfilerInfo10_BeginInprocDebugging(This,fThisThreadOnly,pdwProfilerContext)    \
    ( (This)->lpVtbl -> BeginInprocDebugging(This,fThisThreadOnly,pdwProfilerContext) ) 

#define ICorProfilerInfo10_EndInprocDebugging(This,dwProfilerContext)   \
    ( (This)->lpVtbl -> EndInprocDebugging(This,dwProfilerContext) ) 

#define ICorProfilerInfo10_GetILToNativeMapping(This,functionId,cMap,pcMap,map) \
    ( (This)->lpVtbl -> GetILToNativeMapping(This,functionId,cMap,pcMap,map) ) 


#define ICorProfilerInfo10_DoStackSnapshot(This,thread,callback,infoFlags,clientData,context,contextSize)   \
    ( (This)->lpVtbl -> DoStackSnapshot(This,thread,callback,infoFlags,clientData,context,contextSize) ) 

#define ICorProfilerInfo10_SetEnterLeaveFunctionHooks2(This,pFuncEnter,pFuncLeave,pFuncTailcall)    \
    ( (This)->lpVtbl -> SetEnterLeaveFunctionHooks2(This,pFuncEnter,pFuncLeave,pFuncTailcall) ) 

#define ICorProfilerInfo10_GetFunctionInfo2(This,funcId,frameInfo,pClassId,pModuleId,pToken,cTypeArgs,pcTypeArgs,typeArgs)  \
    ( (This)->lpVtbl -> GetFunctionInfo2(This,funcId,frameInfo,pClassId,pModuleId,pToken,cTypeArgs,pcTypeArgs,typeArgs) ) 

#define ICorProfilerInfo10_GetStringLayout(This,pBufferLengthOffset,pStringLengthOffset,pBufferOffset)  \
    ( (This)->lpVtbl -> GetStringLayout(This,pBufferLengthOffset,pStringLengthOffset,pBufferOffset) ) 

#define ICorProfilerInfo10_GetClassLayout(This,classID,rFieldOffset,cFieldOffset,pcFieldOffset,pulClassSize)    \
    ( (This)->lpVtbl -> GetClassLayout(This,classID,rFieldOffset,cFieldOffset,pcFieldOffset,pulClassSize) ) 

#define ICorProfilerInfo10_GetClassIDInfo2(This,classId,pModuleId,pTypeDefToken,pParentClassId,cNumTypeArgs,pcNumTypeArgs,typeArgs) \
    ( (This)->lpVtbl -> GetClassIDInfo2(This,classId,pModuleId,pTypeDefToken,pParentClassId,cNumTypeArgs,pcNumTypeArgs,typeArgs) ) 

#define ICorProfilerInfo10_GetCodeInfo2(This,functionID,cCodeInfos,pcCodeInfos,codeInfos)   \
    ( (This)->lpVtbl -> GetCodeInfo2(This,functionID,cCodeInfos,pcCodeInfos,codeInfos) ) 

#define ICorProfilerInfo10_GetClassFromTokenAndTypeArgs(This,moduleID,typeDef,cTypeArgs,typeArgs,pClassID)  \
    ( (This)->lpVtbl -> GetClassFromTokenAndTypeArgs(This,moduleID,typeDef,cTypeArgs,typeArgs,pClassID) ) 

#define ICorProfilerInfo10_GetFunctionFromTokenAndTypeArgs(This,moduleID,funcDef,classId,cTypeArgs,typeArgs,pFunctionID)    \
    ( (This)->lpVtbl -> GetFunctionFromTokenAndTypeArgs(This,moduleID,funcDef,classId,cTypeArgs,typeArgs,pFunctionID) ) 

#define ICorProfilerInfo10_EnumModuleFrozenObjects(This,moduleID,ppEnum)    \
    ( (This)->lpVtbl -> EnumModuleFrozenObjects(This,moduleID,ppEnum) ) 

#define ICorProfilerInfo10_GetArrayObjectInfo(This,objectId,cDimensions,pDimensionSizes,pDimensionLowerBounds,ppData)   \
    ( (This)->lpVtbl -> GetArrayObjectInfo(This,objectId,cDimensions,pDimensionSizes,pDimensionLowerBounds,ppData) ) 

#define ICorProfilerInfo10_GetBoxClassLayout(This,classId,pBufferOffset)    \
    ( (This)->lpVtbl -> GetBoxClassLayout(This,classId,pBufferOffset) ) 

#define ICorProfilerInfo10_GetThreadAppDomain(This,threadId,pAppDomainId)   \
    ( (This)->lpVtbl -> GetThreadAppDomain(This,threadId,pAppDomainId) ) 

#define ICorProfilerInfo10_GetRVAStaticAddress(This,classId,fieldToken,ppAddress)   \
    ( (This)->lpVtbl -> GetRVAStaticAddress(This,classId,fieldToken,ppAddress) ) 

#define ICorProfilerInfo10_GetAppDomainStaticAddress(This,classId,fieldToken,appDomainId,ppAddress) \
    ( (This)->lpVtbl -> GetAppDomainStaticAddress(This,classId,fieldToken,appDomainId,ppAddress) ) 

#define ICorProfilerInfo10_GetThreadStaticAddress(This,classId,fieldToken,threadId,ppAddress)   \
    ( (This)->lpVtbl -> GetThreadStaticAddress(This,classId,fieldToken,threadId,ppAddress) ) 

#define ICorProfilerInfo10_GetContextStaticAddress(This,classId,fieldToken,contextId,ppAddress) \
    ( (This)->lpVtbl -> GetContextStaticAddress(This,classId,fieldToken,contextId,ppAddress) ) 

#define ICorProfilerInfo10_GetStaticFieldInfo(This,classId,fieldToken,pFieldInfo)   \
    ( (This)->lpVtbl -> GetStaticFieldInfo(This,classId,fieldToken,pFieldInfo) ) 

#define ICorProfilerInfo10_GetGenerationBounds(This,cObjectRanges,pcObjectRanges,ranges)    \
    ( (This)->lpVtbl -> GetGenerationBounds(This,cObjectRanges,pcObjectRanges,ranges) ) 

#define ICorProfilerInfo10_GetObjectGeneration(This,objectId,range) \
    ( (This)->lpVtbl -> GetObjectGeneration(This,objectId,range) ) 

#define ICorProfilerInfo10_GetNotifiedExceptionClauseInfo(This,pinfo)   \
    ( (This)->lpVtbl -> GetNotifiedExceptionClauseInfo(This,pinfo) ) 


#define ICorProfilerInfo10_EnumJITedFunctions(This,ppEnum)  \
    ( (This)->lpVtbl -> EnumJITedFunctions(This,ppEnum) ) 

#define ICorProfilerInfo10_RequestProfilerDetach(This,dwExpectedCompletionMilliseconds) \
    ( (This)->lpVtbl -> RequestProfilerDetach(This,dwExpectedCompletionMilliseconds) ) 

#define ICorProfilerInfo10_SetFunctionIDMapper2(This,pFunc,clientData)  \
    ( (This)->lpVtbl -> SetFunctionIDMapper2(This,pFunc,clientData) ) 

#define ICorProfilerInfo10_GetStringLayout2(This,pStringLengthOffset,pBufferOffset) \
    ( (This)->lpVtbl -> GetStringLayout2(This,pStringLengthOffset,pBufferOffset) ) 

#define ICorProfilerInfo10_SetEnterLeaveFunctionHooks3(This,pFuncEnter3,pFuncLeave3,pFuncTailcall3) \
    ( (This)->lpVtbl -> SetEnterLeaveFunctionHooks3(This,pFuncEnter3,pFuncLeave3,pFuncTailcall3) ) 

#define ICorProfilerInfo10_SetEnterLeaveFunctionHooks3WithInfo(This,pFuncEnter3WithInfo,pFuncLeave3WithInfo,pFuncTailcall3WithInfo) \
    ( (This)->lpVtbl -> SetEnterLeaveFunctionHooks3WithInfo(This,pFuncEnter3WithInfo,pFuncLeave3WithInfo,pFuncTailcall3WithInfo) ) 

#define ICorProfilerInfo10_GetFunctionEnter3Info(This,functionId,eltInfo,pFrameInfo,pcbArgumentInfo,pArgumentInfo)  \
    ( (This)->lpVtbl -> GetFunctionEnter3Info(This,functionId,eltInfo,pFrameInfo,pcbArgumentInfo,pArgumentInfo) ) 

#define ICorProfilerInfo10_GetFunctionLeave3Info(This,functionId,eltInfo,pFrameInfo,pRetvalRange)   \
    ( (This)->lpVtbl -> GetFunctionLeave3Info(This,functionId,eltInfo,pFrameInfo,pRetvalRange) ) 

#define ICorProfilerInfo10_GetFunctionTailcall3Info(This,functionId,eltInfo,pFrameInfo) \
    ( (This)->lpVtbl -> GetFunctionTailcall3Info(This,functionId,eltInfo,pFrameInfo) ) 

#define ICorProfilerInfo10_EnumModules(This,ppEnum) \
    ( (This)->lpVtbl -> EnumModules(This,ppEnum) ) 

#define ICorProfilerInfo10_GetRuntimeInformation(This,pClrInstanceId,pRuntimeType,pMajorVersion,pMinorVersion,pBuildNumber,pQFEVersion,cchVersionString,pcchVersionString,szVersionString)  \
    ( (This)->lpVtbl -> GetRuntimeInformation(This,pClrInstanceId,pRuntimeType,pMajorVersion,pMinorVersion,pBuildNumber,pQFEVersion,cchVersionString,pcchVersionString,szVersionString) ) 

#define ICorProfilerInfo10_GetThreadStaticAddress2(This,classId,fieldToken,appDomainId,threadId,ppAddress)  \
    ( (This)->lpVtbl -> GetThreadStaticAddress2(This,classId,fieldToken,appDomainId,threadId,ppAddress) ) 

#define ICorProfilerInfo10_GetAppDomainsContainingModule(This,moduleId,cAppDomainIds,pcAppDomainIds,appDomainIds)   \
    ( (This)->lpVtbl -> GetAppDomainsContainingModule(This,moduleId,cAppDomainIds,pcAppDomainIds,appDomainIds) ) 

#define ICorProfilerInfo10_GetModuleInfo2(This,moduleId,ppBaseLoadAddress,cchName,pcchName,szName,pAssemblyId,pdwModuleFlags)   \
    ( (This)->lpVtbl -> GetModuleInfo2(This,moduleId,ppBaseLoadAddress,cchName,pcchName,szName,pAssemblyId,pdwModuleFlags) ) 


#define ICorProfilerInfo10_EnumThreads(This,ppEnum) \
    ( (This)->lpVtbl -> EnumThreads(This,ppEnum) ) 

#define ICorProfilerInfo10_InitializeCurrentThread(This)    \
    ( (This)->lpVtbl -> InitializeCurrentThread(This) ) 

#define ICorProfilerInfo10_RequestReJIT(This,cFunctions,moduleIds,methodIds)    \
    ( (This)->lpVtbl -> RequestReJIT(This,cFunctions,moduleIds,methodIds) ) 

#define ICorProfilerInfo10_RequestRevert(This,cFunctions,moduleIds,methodIds,status)    \
    ( (This)->lpVtbl -> RequestRevert(This,cFunctions,moduleIds,methodIds,status) ) 

#define ICorProfilerInfo10_GetCodeInfo3(This,functionID,reJitId,cCodeInfos,pcCodeInfos,codeInfos)   \
    ( (This)->lpVtbl -> GetCodeInfo3(This,functionID,reJitId,cCodeInfos,pcCodeInfos,codeInfos) ) 

#define ICorProfilerInfo10_GetFunctionFromIP2(This,ip,pFunctionId,pReJitId) \
    ( (This)->lpVtbl -> GetFunctionFromIP2(This,ip,pFunctionId,pReJitId) ) 

#define ICorProfilerInfo10_GetReJITIDs(This,functionId,cReJitIds,pcReJitIds,reJitIds)   \
    ( (This)->lpVtbl -> GetReJITIDs(This,functionId,cReJitIds,pcReJitIds,reJitIds) ) 

#define ICorProfilerInfo10_GetILToNativeMapping2(This,functionId,reJitId,cMap,pcMap,map)    \
    ( (This)->lpVtbl -> GetILToNativeMapping2(This,functionId,reJitId,cMap,pcMap,map) ) 

#define ICorProfilerInfo10_EnumJITedFunctions2(This,ppEnum) \
    ( (This)->lpVtbl -> EnumJITedFunctions2(This,ppEnum) ) 

#define ICorProfilerInfo10_GetObjectSize2(This,objectId,pcSize) \
    ( (This)->lpVtbl -> GetObjectSize2(This,objectId,pcSize) ) 


#define ICorProfilerInfo10_GetEventMask2(This,pdwEventsLow,pdwEventsHigh)   \
    ( (This)->lpVtbl -> GetEventMask2(This,pdwEventsLow,pdwEventsHigh) ) 

#define ICorProfilerInfo10_SetEventMask2(This,dwEventsLow,dwEventsHigh) \
    ( (This)->lpVtbl -> SetEventMask2(This,dwEventsLow,dwEventsHigh) ) 


#define ICorProfilerInfo10_EnumNgenModuleMethodsInliningThisMethod(This,inlinersModuleId,inlineeModuleId,inlineeMethodId,incompleteData,ppEnum) \
    ( (This)->lpVtbl -> EnumNgenModuleMethodsInliningThisMethod(This,inlinersModuleId,inlineeModuleId,inlineeMethodId,incompleteData,ppEnum) ) 


#define ICorProfilerInfo10_ApplyMetaData(This,moduleId) \
    ( (This)->lpVtbl -> ApplyMetaData(This,moduleId) ) 

#define ICorProfilerInfo10_GetInMemorySymbolsLength(This,moduleId,pCountSymbolBytes)    \
    ( (This)->lpVtbl -> GetInMemorySymbolsLength(This,moduleId,pCountSymbolBytes) ) 

#define ICorProfilerInfo10_ReadInMemorySymbols(This,moduleId,symbolsReadOffset,pSymbolBytes,countSymbolBytes,pCountSymbolBytesRead) \
    ( (This)->lpVtbl -> ReadInMemorySymbols(This,moduleId,symbolsReadOffset,pSymbolBytes,countSymbolBytes,pCountSymbolBytesRead) ) 


#define ICorProfilerInfo10_IsFunctionDynamic(This,functionId,isDynamic) \
    ( (This)->lpVtbl -> IsFunctionDynamic(This,functionId,isDynamic) ) 

#define ICorProfilerInfo10_GetFunctionFromIP3(This,ip,functionId,pReJitId)  \
    ( (This)->lpVtbl -> GetFunctionFromIP3(This,ip,functionId,pReJitId) ) 

#define ICorProfilerInfo10_GetDynamicFunctionInfo(This,functionId,moduleId,ppvSig,pbSig,cchName,pcchName,wszName)   \
    ( (This)->lpVtbl -> GetDynamicFunctionInfo(This,functionId,moduleId,ppvSig,pbSig,cchName,pcchName,wszName) ) 


#define ICorProfilerInfo10_GetNativeCodeStartAddresses(This,functionID,reJitId,cCodeStartAddresses,pcCodeStartAddresses,codeStartAddresses) \
    ( (This)->lpVtbl -> GetNativeCodeStartAddresses(This,functionID,reJitId,cCodeStartAddresses,pcCodeStartAddresses,codeStartAddresses) ) 

#define ICorProfilerInfo10_GetILToNativeMapping3(This,pNativeCodeStartAddress,cMap,pcMap,map)   \
    ( (This)->lpVtbl -> GetILToNativeMapping3(This,pNativeCodeStartAddress,cMap,pcMap,map) ) 

#define ICorProfilerInfo10_GetCodeInfo4(This,pNativeCodeStartAddress,cCodeInfos,pcCodeInfos,codeInfos)  \
    ( (This)->lpVtbl -> GetCodeInfo4(This,pNativeCodeStartAddress,cCodeInfos,pcCodeInfos,codeInfos) ) 


#define ICorProfilerInfo10_EnumerateObjectReferences(This,objectId,callback,clientData) \
    ( (This)->lpVtbl -> EnumerateObjectReferences(This,objectId,callback,clientData) ) 

#define ICorProfilerInfo10_IsFrozenObject(This,objectId,pbFrozen)   \
    ( (This)->lpVtbl -> IsFrozenObject(This,objectId,pbFrozen) ) 

#define ICorProfilerInfo10_GetLOHObjectSizeThreshold(This,pThreshold)   \
    ( (This)->lpVtbl -> GetLOHObjectSizeThreshold(This,pThreshold) ) 

#define ICorProfilerInfo10_RequestReJITWithInliners(This,dwRejitFlags,cFunctions,moduleIds,methodIds)   \
    ( (This)->lpVtbl -> RequestReJITWithInliners(This,dwRejitFlags,cFunctions,moduleIds,methodIds) ) 

#define ICorProfilerInfo10_SuspendRuntime(This) \
    ( (This)->lpVtbl -> SuspendRuntime(This) ) 

#define ICorProfilerInfo10_ResumeRuntime(This)  \
    ( (This)->lpVtbl -> ResumeRuntime(This) ) 

#endif /* COBJMACROS */


#endif  /* C style interface */




#endif  /* __ICorProfilerInfo10_INTERFACE_DEFINED__ */


#ifndef __ICorProfilerInfo11_INTERFACE_DEFINED__
#define __ICorProfilerInfo11_INTERFACE_DEFINED__

/* interface ICorProfilerInfo11 */
/* [local][unique][uuid][object] */ 


EXTERN_C const IID IID_ICorProfilerInfo11;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("06398876-8987-4154-B621-40A00D6E4D04")
    ICorProfilerInfo11 : public ICorProfilerInfo10
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE GetEnvironmentVariable( 
            /* [string][in] */ const WCHAR *szName,
            /* [in] */ ULONG cchValue,
            /* [out] */ ULONG *pcchValue,
            /* [annotation][out] */ 
            _Out_writes_to_(cchValue, *pcchValue)  WCHAR szValue[  ]) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE SetEnvironmentVariable( 
            /* [string][in] */ const WCHAR *szName,
            /* [string][in] */ const WCHAR *szValue) = 0;
        
    };
    
    
#else   /* C style interface */

    typedef struct ICorProfilerInfo11Vtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            ICorProfilerInfo11 * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            ICorProfilerInfo11 * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            ICorProfilerInfo11 * This);
        
        HRESULT ( STDMETHODCALLTYPE *GetClassFromObject )( 
            ICorProfilerInfo11 * This,
            /* [in] */ ObjectID objectId,
            /* [out] */ ClassID *pClassId);
        
        HRESULT ( STDMETHODCALLTYPE *GetClassFromToken )( 
            ICorProfilerInfo11 * This,
            /* [in] */ ModuleID moduleId,
            /* [in] */ mdTypeDef typeDef,
            /* [out] */ ClassID *pClassId);
        
        HRESULT ( STDMETHODCALLTYPE *GetCodeInfo )( 
            ICorProfilerInfo11 * This,
            /* [in] */ FunctionID functionId,
            /* [out] */ LPCBYTE *pStart,
            /* [out] */ ULONG *pcSize);
        
        HRESULT ( STDMETHODCALLTYPE *GetEventMask )( 
            ICorProfilerInfo11 * This,
            /* [out] */ DWORD *pdwEvents);
        
        HRESULT ( STDMETHODCALLTYPE *GetFunctionFromIP )( 
            ICorProfilerInfo11 * This,
            /* [in] */ LPCBYTE ip,
            /* [out] */ FunctionID *pFunctionId);
        
        HRESULT ( STDMETHODCALLTYPE *GetFunctionFromToken )( 
            ICorProfilerInfo11 * This,
            /* [in] */ ModuleID moduleId,
            /* [in] */ mdToken token,
            /* [out] */ FunctionID *pFunctionId);
        
        HRESULT ( STDMETHODCALLTYPE *GetHandleFromThread )( 
            ICorProfilerInfo11 * This,
            /* [in] */ ThreadID threadId,
            /* [out] */ HANDLE *phThread);
        
        HRESULT ( STDMETHODCALLTYPE *GetObjectSize )( 
            ICorProfilerInfo11 * This,
            /* [in] */ ObjectID objectId,
            /* [out] */ ULONG *pcSize);
        
        HRESULT ( STDMETHODCALLTYPE *IsArrayClass )( 
            ICorProfilerInfo11 * This,
            /* [in] */ ClassID classId,
            /* [out] */ CorElementType *pBaseElemType,
            /* [out] */ ClassID *pBaseClassId,
            /* [out] */ ULONG *pcRank);
        
        HRESULT ( STDMETHODCALLTYPE *GetThreadInfo )( 
            ICorProfilerInfo11 * This,
            /* [in] */ ThreadID threadId,
            /* [out] */ DWORD *pdwWin32ThreadId);
        
        HRESULT ( STDMETHODCALLTYPE *GetCurrentThreadID )( 
            ICorProfilerInfo11 * This,
            /* [out] */ ThreadID *pThreadId);
        
        HRESULT ( STDMETHODCALLTYPE *GetClassIDInfo )( 
            ICorProfilerInfo11 * This,
            /* [in] */ ClassID classId,
            /* [out] */ ModuleID *pModuleId,
            /* [out] */ mdTypeDef *pTypeDefToken);
        
        HRESULT ( STDMETHODCALLTYPE *GetFunctionInfo )( 
            ICorProfilerInfo11 * This,
            /* [in] */ FunctionID functionId,
            /* [out] */ ClassID *pClassId,
            /* [out] */ ModuleID *pModuleId,
            /* [out] */ mdToken *pToken);
        
        HRESULT ( STDMETHODCALLTYPE *SetEventMask )( 
            ICorProfilerInfo11 * This,
            /* [in] */ DWORD dwEvents);
        
        HRESULT ( STDMETHODCALLTYPE *SetEnterLeaveFunctionHooks )( 
            ICorProfilerInfo11 * This,
            /* [in] */ FunctionEnter *pFuncEnter,
            /* [in] */ FunctionLeave *pFuncLeave,
            /* [in] */ FunctionTailcall *pFuncTailcall);
        
        HRESULT ( STDMETHODCALLTYPE *SetFunctionIDMapper )( 
            ICorProfilerInfo11 * This,
            /* [in] */ FunctionIDMapper *pFunc);
        
        HRESULT ( STDMETHODCALLTYPE *GetTokenAndMetaDataFromFunction )( 
            ICorProfilerInfo11 * This,
            /* [in] */ FunctionID functionId,
            /* [in] */ REFIID riid,
            /* [out] */ IUnknown **ppImport,
            /* [out] */ mdToken *pToken);
        
        HRESULT ( STDMETHODCALLTYPE *GetModuleInfo )( 
            ICorProfilerInfo11 * This,
            /* [in] */ ModuleID moduleId,
            /* [out] */ LPCBYTE *ppBaseLoadAddress,
            /* [in] */ ULONG cchName,
            /* [out] */ ULONG *pcchName,
            /* [annotation][out] */ 
            _Out_writes_to_(cchName, *pcchName)  WCHAR szName[  ],
            /* [out] */ AssemblyID *pAssemblyId);
        
        HRESULT ( STDMETHODCALLTYPE *GetModuleMetaData )( 
            ICorProfilerInfo11 * This,
            /* [in] */ ModuleID moduleId,
            /* [in] */ DWORD dwOpenFlags,
            /* [in] */ REFIID riid,
            /* [out] */ IUnknown **ppOut);
        
        HRESULT ( STDMETHODCALLTYPE *GetILFunctionBody )( 
            ICorProfilerInfo11 * This,
            /* [in] */ ModuleID moduleId,
            /* [in] */ mdMethodDef methodId,
            /* [out] */ LPCBYTE *ppMethodHeader,
            /* [out] */ ULONG *pcbMethodSize);
        
        HRESULT ( STDMETHODCALLTYPE *GetILFunctionBodyAllocator )( 
            ICorProfilerInfo11 * This,
            /* [in] */ ModuleID moduleId,
            /* [out] */ IMethodMalloc **ppMalloc);
        
        HRESULT ( STDMETHODCALLTYPE *SetILFunctionBody )( 
            ICorProfilerInfo11 * This,
            /* [in] */ ModuleID moduleId,
            /* [in] */ mdMethodDef methodid,
            /* [in] */ LPCBYTE pbNewILMethodHeader);
        
        HRESULT ( STDMETHODCALLTYPE *GetAppDomainInfo )( 
            ICorProfilerInfo11 * This,
            /* [in] */ AppDomainID appDomainId,
            /* [in] */ ULONG cchName,
            /* [out] */ ULONG *pcchName,
            /* [annotation][out] */ 
            _Out_writes_to_(cchName, *pcchName)  WCHAR szName[  ],
            /* [out] */ ProcessID *pProcessId);
        
        HRESULT ( STDMETHODCALLTYPE *GetAssemblyInfo )( 
            ICorProfilerInfo11 * This,
            /* [in] */ AssemblyID assemblyId,
            /* [in] */ ULONG cchName,
            /* [out] */ ULONG *pcchName,
            /* [annotation][out] */ 
            _Out_writes_to_(cchName, *pcchName)  WCHAR szName[  ],
            /* [out] */ AppDomainID *pAppDomainId,
            /* [out] */ ModuleID *pModuleId);
        
        HRESULT ( STDMETHODCALLTYPE *SetFunctionReJIT )( 
            ICorProfilerInfo11 * This,
            /* [in] */ FunctionID functionId);
        
        HRESULT ( STDMETHODCALLTYPE *ForceGC )( 
            ICorProfilerInfo11 * This);
        
        HRESULT ( STDMETHODCALLTYPE *SetILInstrumentedCodeMap )( 
            ICorProfilerInfo11 * This,
            /* [in] */ FunctionID functionId,
            /* [in] */ BOOL fStartJit,
            /* [in] */ ULONG cILMapEntries,
            /* [size_is][in] */ COR_IL_MAP rgILMapEntries[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *GetInprocInspectionInterface )( 
            ICorProfilerInfo11 * This,
            /* [out] */ IUnknown **ppicd);
        
        HRESULT ( STDMETHODCALLTYPE *GetInprocInspectionIThisThread )( 
            ICorProfilerInfo11 * This,
            /* [out] */ IUnknown **ppicd);
        
        HRESULT ( STDMETHODCALLTYPE *GetThreadContext )( 
            ICorProfilerInfo11 * This,
            /* [in] */ ThreadID threadId,
            /* [out] */ ContextID *pContextId);
        
        HRESULT ( STDMETHODCALLTYPE *BeginInprocDebugging )( 
            ICorProfilerInfo11 * This,
            /* [in] */ BOOL fThisThreadOnly,
            /* [out] */ DWORD *pdwProfilerContext);
        
        HRESULT ( STDMETHODCALLTYPE *EndInprocDebugging )( 
            ICorProfilerInfo11 * This,
            /* [in] */ DWORD dwProfilerContext);
        
        HRESULT ( STDMETHODCALLTYPE *GetILToNativeMapping )( 
            ICorProfilerInfo11 * This,
            /* [in] */ FunctionID functionId,
            /* [in] */ ULONG32 cMap,
            /* [out] */ ULONG32 *pcMap,
            /* [length_is][size_is][out] */ COR_DEBUG_IL_TO_NATIVE_MAP map[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *DoStackSnapshot )( 
            ICorProfilerInfo11 * This,
            /* [in] */ ThreadID thread,
            /* [in] */ StackSnapshotCallback *callback,
            /* [in] */ ULONG32 infoFlags,
            /* [in] */ void *clientData,
            /* [size_is][in] */ BYTE context[  ],
            /* [in] */ ULONG32 contextSize);
        
        HRESULT ( STDMETHODCALLTYPE *SetEnterLeaveFunctionHooks2 )( 
            ICorProfilerInfo11 * This,
            /* [in] */ FunctionEnter2 *pFuncEnter,
            /* [in] */ FunctionLeave2 *pFuncLeave,
            /* [in] */ FunctionTailcall2 *pFuncTailcall);
        
        HRESULT ( STDMETHODCALLTYPE *GetFunctionInfo2 )( 
            ICorProfilerInfo11 * This,
            /* [in] */ FunctionID funcId,
            /* [in] */ COR_PRF_FRAME_INFO frameInfo,
            /* [out] */ ClassID *pClassId,
            /* [out] */ ModuleID *pModuleId,
            /* [out] */ mdToken *pToken,
            /* [in] */ ULONG32 cTypeArgs,
            /* [out] */ ULONG32 *pcTypeArgs,
            /* [out] */ ClassID typeArgs[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *GetStringLayout )( 
            ICorProfilerInfo11 * This,
            /* [out] */ ULONG *pBufferLengthOffset,
            /* [out] */ ULONG *pStringLengthOffset,
            /* [out] */ ULONG *pBufferOffset);
        
        HRESULT ( STDMETHODCALLTYPE *GetClassLayout )( 
            ICorProfilerInfo11 * This,
            /* [in] */ ClassID classID,
            /* [out][in] */ COR_FIELD_OFFSET rFieldOffset[  ],
            /* [in] */ ULONG cFieldOffset,
            /* [out] */ ULONG *pcFieldOffset,
            /* [out] */ ULONG *pulClassSize);
        
        HRESULT ( STDMETHODCALLTYPE *GetClassIDInfo2 )( 
            ICorProfilerInfo11 * This,
            /* [in] */ ClassID classId,
            /* [out] */ ModuleID *pModuleId,
            /* [out] */ mdTypeDef *pTypeDefToken,
            /* [out] */ ClassID *pParentClassId,
            /* [in] */ ULONG32 cNumTypeArgs,
            /* [out] */ ULONG32 *pcNumTypeArgs,
            /* [out] */ ClassID typeArgs[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *GetCodeInfo2 )( 
            ICorProfilerInfo11 * This,
            /* [in] */ FunctionID functionID,
            /* [in] */ ULONG32 cCodeInfos,
            /* [out] */ ULONG32 *pcCodeInfos,
            /* [length_is][size_is][out] */ COR_PRF_CODE_INFO codeInfos[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *GetClassFromTokenAndTypeArgs )( 
            ICorProfilerInfo11 * This,
            /* [in] */ ModuleID moduleID,
            /* [in] */ mdTypeDef typeDef,
            /* [in] */ ULONG32 cTypeArgs,
            /* [size_is][in] */ ClassID typeArgs[  ],
            /* [out] */ ClassID *pClassID);
        
        HRESULT ( STDMETHODCALLTYPE *GetFunctionFromTokenAndTypeArgs )( 
            ICorProfilerInfo11 * This,
            /* [in] */ ModuleID moduleID,
            /* [in] */ mdMethodDef funcDef,
            /* [in] */ ClassID classId,
            /* [in] */ ULONG32 cTypeArgs,
            /* [size_is][in] */ ClassID typeArgs[  ],
            /* [out] */ FunctionID *pFunctionID);
        
        HRESULT ( STDMETHODCALLTYPE *EnumModuleFrozenObjects )( 
            ICorProfilerInfo11 * This,
            /* [in] */ ModuleID moduleID,
            /* [out] */ ICorProfilerObjectEnum **ppEnum);
        
        HRESULT ( STDMETHODCALLTYPE *GetArrayObjectInfo )( 
            ICorProfilerInfo11 * This,
            /* [in] */ ObjectID objectId,
            /* [in] */ ULONG32 cDimensions,
            /* [size_is][out] */ ULONG32 pDimensionSizes[  ],
            /* [size_is][out] */ int pDimensionLowerBounds[  ],
            /* [out] */ BYTE **ppData);
        
        HRESULT ( STDMETHODCALLTYPE *GetBoxClassLayout )( 
            ICorProfilerInfo11 * This,
            /* [in] */ ClassID classId,
            /* [out] */ ULONG32 *pBufferOffset);
        
        HRESULT ( STDMETHODCALLTYPE *GetThreadAppDomain )( 
            ICorProfilerInfo11 * This,
            /* [in] */ ThreadID threadId,
            /* [out] */ AppDomainID *pAppDomainId);
        
        HRESULT ( STDMETHODCALLTYPE *GetRVAStaticAddress )( 
            ICorProfilerInfo11 * This,
            /* [in] */ ClassID classId,
            /* [in] */ mdFieldDef fieldToken,
            /* [out] */ void **ppAddress);
        
        HRESULT ( STDMETHODCALLTYPE *GetAppDomainStaticAddress )( 
            ICorProfilerInfo11 * This,
            /* [in] */ ClassID classId,
            /* [in] */ mdFieldDef fieldToken,
            /* [in] */ AppDomainID appDomainId,
            /* [out] */ void **ppAddress);
        
        HRESULT ( STDMETHODCALLTYPE *GetThreadStaticAddress )( 
            ICorProfilerInfo11 * This,
            /* [in] */ ClassID classId,
            /* [in] */ mdFieldDef fieldToken,
            /* [in] */ ThreadID threadId,
            /* [out] */ void **ppAddress);
        
        HRESULT ( STDMETHODCALLTYPE *GetContextStaticAddress )( 
            ICorProfilerInfo11 * This,
            /* [in] */ ClassID classId,
            /* [in] */ mdFieldDef fieldToken,
            /* [in] */ ContextID contextId,
            /* [out] */ void **ppAddress);
        
        HRESULT ( STDMETHODCALLTYPE *GetStaticFieldInfo )( 
            ICorProfilerInfo11 * This,
            /* [in] */ ClassID classId,
            /* [in] */ mdFieldDef fieldToken,
            /* [out] */ COR_PRF_STATIC_TYPE *pFieldInfo);
        
        HRESULT ( STDMETHODCALLTYPE *GetGenerationBounds )( 
            ICorProfilerInfo11 * This,
            /* [in] */ ULONG cObjectRanges,
            /* [out] */ ULONG *pcObjectRanges,
            /* [length_is][size_is][out] */ COR_PRF_GC_GENERATION_RANGE ranges[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *GetObjectGeneration )( 
            ICorProfilerInfo11 * This,
            /* [in] */ ObjectID objectId,
            /* [out] */ COR_PRF_GC_GENERATION_RANGE *range);
        
        HRESULT ( STDMETHODCALLTYPE *GetNotifiedExceptionClauseInfo )( 
            ICorProfilerInfo11 * This,
            /* [out] */ COR_PRF_EX_CLAUSE_INFO *pinfo);
        
        HRESULT ( STDMETHODCALLTYPE *EnumJITedFunctions )( 
            ICorProfilerInfo11 * This,
            /* [out] */ ICorProfilerFunctionEnum **ppEnum);
        
        HRESULT ( STDMETHODCALLTYPE *RequestProfilerDetach )( 
            ICorProfilerInfo11 * This,
            /* [in] */ DWORD dwExpectedCompletionMilliseconds);
        
        HRESULT ( STDMETHODCALLTYPE *SetFunctionIDMapper2 )( 
            ICorProfilerInfo11 * This,
            /* [in] */ FunctionIDMapper2 *pFunc,
            /* [in] */ void *clientData);
        
        HRESULT ( STDMETHODCALLTYPE *GetStringLayout2 )( 
            ICorProfilerInfo11 * This,
            /* [out] */ ULONG *pStringLengthOffset,
            /* [out] */ ULONG *pBufferOffset);
        
        HRESULT ( STDMETHODCALLTYPE *SetEnterLeaveFunctionHooks3 )( 
            ICorProfilerInfo11 * This,
            /* [in] */ FunctionEnter3 *pFuncEnter3,
            /* [in] */ FunctionLeave3 *pFuncLeave3,
            /* [in] */ FunctionTailcall3 *pFuncTailcall3);
        
        HRESULT ( STDMETHODCALLTYPE *SetEnterLeaveFunctionHooks3WithInfo )( 
            ICorProfilerInfo11 * This,
            /* [in] */ FunctionEnter3WithInfo *pFuncEnter3WithInfo,
            /* [in] */ FunctionLeave3WithInfo *pFuncLeave3WithInfo,
            /* [in] */ FunctionTailcall3WithInfo *pFuncTailcall3WithInfo);
        
        HRESULT ( STDMETHODCALLTYPE *GetFunctionEnter3Info )( 
            ICorProfilerInfo11 * This,
            /* [in] */ FunctionID functionId,
            /* [in] */ COR_PRF_ELT_INFO eltInfo,
            /* [out] */ COR_PRF_FRAME_INFO *pFrameInfo,
            /* [out][in] */ ULONG *pcbArgumentInfo,
            /* [size_is][out] */ COR_PRF_FUNCTION_ARGUMENT_INFO *pArgumentInfo);
        
        HRESULT ( STDMETHODCALLTYPE *GetFunctionLeave3Info )( 
            ICorProfilerInfo11 * This,
            /* [in] */ FunctionID functionId,
            /* [in] */ COR_PRF_ELT_INFO eltInfo,
            /* [out] */ COR_PRF_FRAME_INFO *pFrameInfo,
            /* [out] */ COR_PRF_FUNCTION_ARGUMENT_RANGE *pRetvalRange);
        
        HRESULT ( STDMETHODCALLTYPE *GetFunctionTailcall3Info )( 
            ICorProfilerInfo11 * This,
            /* [in] */ FunctionID functionId,
            /* [in] */ COR_PRF_ELT_INFO eltInfo,
            /* [out] */ COR_PRF_FRAME_INFO *pFrameInfo);
        
        HRESULT ( STDMETHODCALLTYPE *EnumModules )( 
            ICorProfilerInfo11 * This,
            /* [out] */ ICorProfilerModuleEnum **ppEnum);
        
        HRESULT ( STDMETHODCALLTYPE *GetRuntimeInformation )( 
            ICorProfilerInfo11 * This,
            /* [out] */ USHORT *pClrInstanceId,
            /* [out] */ COR_PRF_RUNTIME_TYPE *pRuntimeType,
            /* [out] */ USHORT *pMajorVersion,
            /* [out] */ USHORT *pMinorVersion,
            /* [out] */ USHORT *pBuildNumber,
            /* [out] */ USHORT *pQFEVersion,
            /* [in] */ ULONG cchVersionString,
            /* [out] */ ULONG *pcchVersionString,
            /* [annotation][out] */ 
            _Out_writes_to_(cchVersionString, *pcchVersionString)  WCHAR szVersionString[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *GetThreadStaticAddress2 )( 
            ICorProfilerInfo11 * This,
            /* [in] */ ClassID classId,
            /* [in] */ mdFieldDef fieldToken,
            /* [in] */ AppDomainID appDomainId,
            /* [in] */ ThreadID threadId,
            /* [out] */ void **ppAddress);
        
        HRESULT ( STDMETHODCALLTYPE *GetAppDomainsContainingModule )( 
            ICorProfilerInfo11 * This,
            /* [in] */ ModuleID moduleId,
            /* [in] */ ULONG32 cAppDomainIds,
            /* [out] */ ULONG32 *pcAppDomainIds,
            /* [length_is][size_is][out] */ AppDomainID appDomainIds[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *GetModuleInfo2 )( 
            ICorProfilerInfo11 * This,
            /* [in] */ ModuleID moduleId,
            /* [out] */ LPCBYTE *ppBaseLoadAddress,
            /* [in] */ ULONG cchName,
            /* [out] */ ULONG *pcchName,
            /* [annotation][out] */ 
            _Out_writes_to_(cchName, *pcchName)  WCHAR szName[  ],
            /* [out] */ AssemblyID *pAssemblyId,
            /* [out] */ DWORD *pdwModuleFlags);
        
        HRESULT ( STDMETHODCALLTYPE *EnumThreads )( 
            ICorProfilerInfo11 * This,
            /* [out] */ ICorProfilerThreadEnum **ppEnum);
        
        HRESULT ( STDMETHODCALLTYPE *InitializeCurrentThread )( 
            ICorProfilerInfo11 * This);
        
        HRESULT ( STDMETHODCALLTYPE *RequestReJIT )( 
            ICorProfilerInfo11 * This,
            /* [in] */ ULONG cFunctions,
            /* [size_is][in] */ ModuleID moduleIds[  ],
            /* [size_is][in] */ mdMethodDef methodIds[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *RequestRevert )( 
            ICorProfilerInfo11 * This,
            /* [in] */ ULONG cFunctions,
            /* [size_is][in] */ ModuleID moduleIds[  ],
            /* [size_is][in] */ mdMethodDef methodIds[  ],
            /* [size_is][out] */ HRESULT status[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *GetCodeInfo3 )( 
            ICorProfilerInfo11 * This,
            /* [in] */ FunctionID functionID,
            /* [in] */ ReJITID reJitId,
            /* [in] */ ULONG32 cCodeInfos,
            /* [out] */ ULONG32 *pcCodeInfos,
            /* [length_is][size_is][out] */ COR_PRF_CODE_INFO codeInfos[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *GetFunctionFromIP2 )( 
            ICorProfilerInfo11 * This,
            /* [in] */ LPCBYTE ip,
            /* [out] */ FunctionID *pFunctionId,
            /* [out] */ ReJITID *pReJitId);
        
        HRESULT ( STDMETHODCALLTYPE *GetReJITIDs )( 
            ICorProfilerInfo11 * This,
            /* [in] */ FunctionID functionId,
            /* [in] */ ULONG cReJitIds,
            /* [out] */ ULONG *pcReJitIds,
            /* [length_is][size_is][out] */ ReJITID reJitIds[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *GetILToNativeMapping2 )( 
            ICorProfilerInfo11 * This,
            /* [in] */ FunctionID functionId,
            /* [in] */ ReJITID reJitId,
            /* [in] */ ULONG32 cMap,
            /* [out] */ ULONG32 *pcMap,
            /* [length_is][size_is][out] */ COR_DEBUG_IL_TO_NATIVE_MAP map[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *EnumJITedFunctions2 )( 
            ICorProfilerInfo11 * This,
            /* [out] */ ICorProfilerFunctionEnum **ppEnum);
        
        HRESULT ( STDMETHODCALLTYPE *GetObjectSize2 )( 
            ICorProfilerInfo11 * This,
            /* [in] */ ObjectID objectId,
            /* [out] */ SIZE_T *pcSize);
        
        HRESULT ( STDMETHODCALLTYPE *GetEventMask2 )( 
            ICorProfilerInfo11 * This,
            /* [out] */ DWORD *pdwEventsLow,
            /* [out] */ DWORD *pdwEventsHigh);
        
        HRESULT ( STDMETHODCALLTYPE *SetEventMask2 )( 
            ICorProfilerInfo11 * This,
            /* [in] */ DWORD dwEventsLow,
            /* [in] */ DWORD dwEventsHigh);
        
        HRESULT ( STDMETHODCALLTYPE *EnumNgenModuleMethodsInliningThisMethod )( 
            ICorProfilerInfo11 * This,
            /* [in] */ ModuleID inlinersModuleId,
            /* [in] */ ModuleID inlineeModuleId,
            /* [in] */ mdMethodDef inlineeMethodId,
            /* [out] */ BOOL *incompleteData,
            /* [out] */ ICorProfilerMethodEnum **ppEnum);
        
        HRESULT ( STDMETHODCALLTYPE *ApplyMetaData )( 
            ICorProfilerInfo11 * This,
            /* [in] */ ModuleID moduleId);
        
        HRESULT ( STDMETHODCALLTYPE *GetInMemorySymbolsLength )( 
            ICorProfilerInfo11 * This,
            /* [in] */ ModuleID moduleId,
            /* [out] */ DWORD *pCountSymbolBytes);
        
        HRESULT ( STDMETHODCALLTYPE *ReadInMemorySymbols )( 
            ICorProfilerInfo11 * This,
            /* [in] */ ModuleID moduleId,
            /* [in] */ DWORD symbolsReadOffset,
            /* [out] */ BYTE *pSymbolBytes,
            /* [in] */ DWORD countSymbolBytes,
            /* [out] */ DWORD *pCountSymbolBytesRead);
        
        HRESULT ( STDMETHODCALLTYPE *IsFunctionDynamic )( 
            ICorProfilerInfo11 * This,
            /* [in] */ FunctionID functionId,
            /* [out] */ BOOL *isDynamic);
        
        HRESULT ( STDMETHODCALLTYPE *GetFunctionFromIP3 )( 
            ICorProfilerInfo11 * This,
            /* [in] */ LPCBYTE ip,
            /* [out] */ FunctionID *functionId,
            /* [out] */ ReJITID *pReJitId);
        
        HRESULT ( STDMETHODCALLTYPE *GetDynamicFunctionInfo )( 
            ICorProfilerInfo11 * This,
            /* [in] */ FunctionID functionId,
            /* [out] */ ModuleID *moduleId,
            /* [out] */ PCCOR_SIGNATURE *ppvSig,
            /* [out] */ ULONG *pbSig,
            /* [in] */ ULONG cchName,
            /* [out] */ ULONG *pcchName,
            /* [out] */ WCHAR wszName[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *GetNativeCodeStartAddresses )( 
            ICorProfilerInfo11 * This,
            FunctionID functionID,
            ReJITID reJitId,
            ULONG32 cCodeStartAddresses,
            ULONG32 *pcCodeStartAddresses,
            UINT_PTR codeStartAddresses[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *GetILToNativeMapping3 )( 
            ICorProfilerInfo11 * This,
            UINT_PTR pNativeCodeStartAddress,
            ULONG32 cMap,
            ULONG32 *pcMap,
            COR_DEBUG_IL_TO_NATIVE_MAP map[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *GetCodeInfo4 )( 
            ICorProfilerInfo11 * This,
            UINT_PTR pNativeCodeStartAddress,
            ULONG32 cCodeInfos,
            ULONG32 *pcCodeInfos,
            COR_PRF_CODE_INFO codeInfos[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *EnumerateObjectReferences )( 
            ICorProfilerInfo11 * This,
            ObjectID objectId,
            ObjectReferenceCallback callback,
            void *clientData);
        
        HRESULT ( STDMETHODCALLTYPE *IsFrozenObject )( 
            ICorProfilerInfo11 * This,
            ObjectID objectId,
            BOOL *pbFrozen);
        
        HRESULT ( STDMETHODCALLTYPE *GetLOHObjectSizeThreshold )( 
            ICorProfilerInfo11 * This,
            DWORD *pThreshold);
        
        HRESULT ( STDMETHODCALLTYPE *RequestReJITWithInliners )( 
            ICorProfilerInfo11 * This,
            /* [in] */ DWORD dwRejitFlags,
            /* [in] */ ULONG cFunctions,
            /* [size_is][in] */ ModuleID moduleIds[  ],
            /* [size_is][in] */ mdMethodDef methodIds[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *SuspendRuntime )( 
            ICorProfilerInfo11 * This);
        
        HRESULT ( STDMETHODCALLTYPE *ResumeRuntime )( 
            ICorProfilerInfo11 * This);
        
        HRESULT ( STDMETHODCALLTYPE *GetEnvironmentVariable )( 
            ICorProfilerInfo11 * This,
            /* [string][in] */ const WCHAR *szName,
            /* [in] */ ULONG cchValue,
            /* [out] */ ULONG *pcchValue,
            /* [annotation][out] */ 
            _Out_writes_to_(cchValue, *pcchValue)  WCHAR szValue[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *SetEnvironmentVariable )( 
            ICorProfilerInfo11 * This,
            /* [string][in] */ const WCHAR *szName,
            /* [string][in] */ const WCHAR *szValue);
        
        END_INTERFACE
    } ICorProfilerInfo11Vtbl;

    interface ICorProfilerInfo11
    {
        CONST_VTBL struct ICorProfilerInfo11Vtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define ICorProfilerInfo11_QueryInterface(This,riid,ppvObject)  \
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define ICorProfilerInfo11_AddRef(This) \
    ( (This)->lpVtbl -> AddRef(This) ) 

#define ICorProfilerInfo11_Release(This)    \
    ( (This)->lpVtbl -> Release(This) ) 


#define ICorProfilerInfo11_GetClassFromObject(This,objectId,pClassId)   \
    ( (This)->lpVtbl -> GetClassFromObject(This,objectId,pClassId) ) 

#define ICorProfilerInfo11_GetClassFromToken(This,moduleId,typeDef,pClassId)    \
    ( (This)->lpVtbl -> GetClassFromToken(This,moduleId,typeDef,pClassId) ) 

#define ICorProfilerInfo11_GetCodeInfo(This,functionId,pStart,pcSize)   \
    ( (This)->lpVtbl -> GetCodeInfo(This,functionId,pStart,pcSize) ) 

#define ICorProfilerInfo11_GetEventMask(This,pdwEvents) \
    ( (This)->lpVtbl -> GetEventMask(This,pdwEvents) ) 

#define ICorProfilerInfo11_GetFunctionFromIP(This,ip,pFunctionId)   \
    ( (This)->lpVtbl -> GetFunctionFromIP(This,ip,pFunctionId) ) 

#define ICorProfilerInfo11_GetFunctionFromToken(This,moduleId,token,pFunctionId)    \
    ( (This)->lpVtbl -> GetFunctionFromToken(This,moduleId,token,pFunctionId) ) 

#define ICorProfilerInfo11_GetHandleFromThread(This,threadId,phThread)  \
    ( (This)->lpVtbl -> GetHandleFromThread(This,threadId,phThread) ) 

#define ICorProfilerInfo11_GetObjectSize(This,objectId,pcSize)  \
    ( (This)->lpVtbl -> GetObjectSize(This,objectId,pcSize) ) 

#define ICorProfilerInfo11_IsArrayClass(This,classId,pBaseElemType,pBaseClassId,pcRank) \
    ( (This)->lpVtbl -> IsArrayClass(This,classId,pBaseElemType,pBaseClassId,pcRank) ) 

#define ICorProfilerInfo11_GetThreadInfo(This,threadId,pdwWin32ThreadId)    \
    ( (This)->lpVtbl -> GetThreadInfo(This,threadId,pdwWin32ThreadId) ) 

#define ICorProfilerInfo11_GetCurrentThreadID(This,pThreadId)   \
    ( (This)->lpVtbl -> GetCurrentThreadID(This,pThreadId) ) 

#define ICorProfilerInfo11_GetClassIDInfo(This,classId,pModuleId,pTypeDefToken) \
    ( (This)->lpVtbl -> GetClassIDInfo(This,classId,pModuleId,pTypeDefToken) ) 

#define ICorProfilerInfo11_GetFunctionInfo(This,functionId,pClassId,pModuleId,pToken)   \
    ( (This)->lpVtbl -> GetFunctionInfo(This,functionId,pClassId,pModuleId,pToken) ) 

#define ICorProfilerInfo11_SetEventMask(This,dwEvents)  \
    ( (This)->lpVtbl -> SetEventMask(This,dwEvents) ) 

#define ICorProfilerInfo11_SetEnterLeaveFunctionHooks(This,pFuncEnter,pFuncLeave,pFuncTailcall) \
    ( (This)->lpVtbl -> SetEnterLeaveFunctionHooks(This,pFuncEnter,pFuncLeave,pFuncTailcall) ) 

#define ICorProfilerInfo11_SetFunctionIDMapper(This,pFunc)  \
    ( (This)->lpVtbl -> SetFunctionIDMapper(This,pFunc) ) 

#define ICorProfilerInfo11_GetTokenAndMetaDataFromFunction(This,functionId,riid,ppImport,pToken)    \
    ( (This)->lpVtbl -> GetTokenAndMetaDataFromFunction(This,functionId,riid,ppImport,pToken) ) 

#define ICorProfilerInfo11_GetModuleInfo(This,moduleId,ppBaseLoadAddress,cchName,pcchName,szName,pAssemblyId)   \
    ( (This)->lpVtbl -> GetModuleInfo(This,moduleId,ppBaseLoadAddress,cchName,pcchName,szName,pAssemblyId) ) 

#define ICorProfilerInfo11_GetModuleMetaData(This,moduleId,dwOpenFlags,riid,ppOut)  \
    ( (This)->lpVtbl -> GetModuleMetaData(This,moduleId,dwOpenFlags,riid,ppOut) ) 

#define ICorProfilerInfo11_GetILFunctionBody(This,moduleId,methodId,ppMethodHeader,pcbMethodSize)   \
    ( (This)->lpVtbl -> GetILFunctionBody(This,moduleId,methodId,ppMethodHeader,pcbMethodSize) ) 

#define ICorProfilerInfo11_GetILFunctionBodyAllocator(This,moduleId,ppMalloc)   \
    ( (This)->lpVtbl -> GetILFunctionBodyAllocator(This,moduleId,ppMalloc) ) 

#define ICorProfilerInfo11_SetILFunctionBody(This,moduleId,methodid,pbNewILMethodHeader)    \
    ( (This)->lpVtbl -> SetILFunctionBody(This,moduleId,methodid,pbNewILMethodHeader) ) 

#define ICorProfilerInfo11_GetAppDomainInfo(This,appDomainId,cchName,pcchName,szName,pProcessId)    \
    ( (This)->lpVtbl -> GetAppDomainInfo(This,appDomainId,cchName,pcchName,szName,pProcessId) ) 

#define ICorProfilerInfo11_GetAssemblyInfo(This,assemblyId,cchName,pcchName,szName,pAppDomainId,pModuleId)  \
    ( (This)->lpVtbl -> GetAssemblyInfo(This,assemblyId,cchName,pcchName,szName,pAppDomainId,pModuleId) ) 

#define ICorProfilerInfo11_SetFunctionReJIT(This,functionId)    \
    ( (This)->lpVtbl -> SetFunctionReJIT(This,functionId) ) 

#define ICorProfilerInfo11_ForceGC(This)    \
    ( (This)->lpVtbl -> ForceGC(This) ) 

#define ICorProfilerInfo11_SetILInstrumentedCodeMap(This,functionId,fStartJit,cILMapEntries,rgILMapEntries) \
    ( (This)->lpVtbl -> SetILInstrumentedCodeMap(This,functionId,fStartJit,cILMapEntries,rgILMapEntries) ) 

#define ICorProfilerInfo11_GetInprocInspectionInterface(This,ppicd) \
    ( (This)->lpVtbl -> GetInprocInspectionInterface(This,ppicd) ) 

#define ICorProfilerInfo11_GetInprocInspectionIThisThread(This,ppicd)   \
    ( (This)->lpVtbl -> GetInprocInspectionIThisThread(This,ppicd) ) 

#define ICorProfilerInfo11_GetThreadContext(This,threadId,pContextId)   \
    ( (This)->lpVtbl -> GetThreadContext(This,threadId,pContextId) ) 

#define ICorProfilerInfo11_BeginInprocDebugging(This,fThisThreadOnly,pdwProfilerContext)    \
    ( (This)->lpVtbl -> BeginInprocDebugging(This,fThisThreadOnly,pdwProfilerContext) ) 

#define ICorProfilerInfo11_EndInprocDebugging(This,dwProfilerContext)   \
    ( (This)->lpVtbl -> EndInprocDebugging(This,dwProfilerContext) ) 

#define ICorProfilerInfo11_GetILToNativeMapping(This,functionId,cMap,pcMap,map) \
    ( (This)->lpVtbl -> GetILToNativeMapping(This,functionId,cMap,pcMap,map) ) 


#define ICorProfilerInfo11_DoStackSnapshot(This,thread,callback,infoFlags,clientData,context,contextSize)   \
    ( (This)->lpVtbl -> DoStackSnapshot(This,thread,callback,infoFlags,clientData,context,contextSize) ) 

#define ICorProfilerInfo11_SetEnterLeaveFunctionHooks2(This,pFuncEnter,pFuncLeave,pFuncTailcall)    \
    ( (This)->lpVtbl -> SetEnterLeaveFunctionHooks2(This,pFuncEnter,pFuncLeave,pFuncTailcall) ) 

#define ICorProfilerInfo11_GetFunctionInfo2(This,funcId,frameInfo,pClassId,pModuleId,pToken,cTypeArgs,pcTypeArgs,typeArgs)  \
    ( (This)->lpVtbl -> GetFunctionInfo2(This,funcId,frameInfo,pClassId,pModuleId,pToken,cTypeArgs,pcTypeArgs,typeArgs) ) 

#define ICorProfilerInfo11_GetStringLayout(This,pBufferLengthOffset,pStringLengthOffset,pBufferOffset)  \
    ( (This)->lpVtbl -> GetStringLayout(This,pBufferLengthOffset,pStringLengthOffset,pBufferOffset) ) 

#define ICorProfilerInfo11_GetClassLayout(This,classID,rFieldOffset,cFieldOffset,pcFieldOffset,pulClassSize)    \
    ( (This)->lpVtbl -> GetClassLayout(This,classID,rFieldOffset,cFieldOffset,pcFieldOffset,pulClassSize) ) 

#define ICorProfilerInfo11_GetClassIDInfo2(This,classId,pModuleId,pTypeDefToken,pParentClassId,cNumTypeArgs,pcNumTypeArgs,typeArgs) \
    ( (This)->lpVtbl -> GetClassIDInfo2(This,classId,pModuleId,pTypeDefToken,pParentClassId,cNumTypeArgs,pcNumTypeArgs,typeArgs) ) 

#define ICorProfilerInfo11_GetCodeInfo2(This,functionID,cCodeInfos,pcCodeInfos,codeInfos)   \
    ( (This)->lpVtbl -> GetCodeInfo2(This,functionID,cCodeInfos,pcCodeInfos,codeInfos) ) 

#define ICorProfilerInfo11_GetClassFromTokenAndTypeArgs(This,moduleID,typeDef,cTypeArgs,typeArgs,pClassID)  \
    ( (This)->lpVtbl -> GetClassFromTokenAndTypeArgs(This,moduleID,typeDef,cTypeArgs,typeArgs,pClassID) ) 

#define ICorProfilerInfo11_GetFunctionFromTokenAndTypeArgs(This,moduleID,funcDef,classId,cTypeArgs,typeArgs,pFunctionID)    \
    ( (This)->lpVtbl -> GetFunctionFromTokenAndTypeArgs(This,moduleID,funcDef,classId,cTypeArgs,typeArgs,pFunctionID) ) 

#define ICorProfilerInfo11_EnumModuleFrozenObjects(This,moduleID,ppEnum)    \
    ( (This)->lpVtbl -> EnumModuleFrozenObjects(This,moduleID,ppEnum) ) 

#define ICorProfilerInfo11_GetArrayObjectInfo(This,objectId,cDimensions,pDimensionSizes,pDimensionLowerBounds,ppData)   \
    ( (This)->lpVtbl -> GetArrayObjectInfo(This,objectId,cDimensions,pDimensionSizes,pDimensionLowerBounds,ppData) ) 

#define ICorProfilerInfo11_GetBoxClassLayout(This,classId,pBufferOffset)    \
    ( (This)->lpVtbl -> GetBoxClassLayout(This,classId,pBufferOffset) ) 

#define ICorProfilerInfo11_GetThreadAppDomain(This,threadId,pAppDomainId)   \
    ( (This)->lpVtbl -> GetThreadAppDomain(This,threadId,pAppDomainId) ) 

#define ICorProfilerInfo11_GetRVAStaticAddress(This,classId,fieldToken,ppAddress)   \
    ( (This)->lpVtbl -> GetRVAStaticAddress(This,classId,fieldToken,ppAddress) ) 

#define ICorProfilerInfo11_GetAppDomainStaticAddress(This,classId,fieldToken,appDomainId,ppAddress) \
    ( (This)->lpVtbl -> GetAppDomainStaticAddress(This,classId,fieldToken,appDomainId,ppAddress) ) 

#define ICorProfilerInfo11_GetThreadStaticAddress(This,classId,fieldToken,threadId,ppAddress)   \
    ( (This)->lpVtbl -> GetThreadStaticAddress(This,classId,fieldToken,threadId,ppAddress) ) 

#define ICorProfilerInfo11_GetContextStaticAddress(This,classId,fieldToken,contextId,ppAddress) \
    ( (This)->lpVtbl -> GetContextStaticAddress(This,classId,fieldToken,contextId,ppAddress) ) 

#define ICorProfilerInfo11_GetStaticFieldInfo(This,classId,fieldToken,pFieldInfo)   \
    ( (This)->lpVtbl -> GetStaticFieldInfo(This,classId,fieldToken,pFieldInfo) ) 

#define ICorProfilerInfo11_GetGenerationBounds(This,cObjectRanges,pcObjectRanges,ranges)    \
    ( (This)->lpVtbl -> GetGenerationBounds(This,cObjectRanges,pcObjectRanges,ranges) ) 

#define ICorProfilerInfo11_GetObjectGeneration(This,objectId,range) \
    ( (This)->lpVtbl -> GetObjectGeneration(This,objectId,range) ) 

#define ICorProfilerInfo11_GetNotifiedExceptionClauseInfo(This,pinfo)   \
    ( (This)->lpVtbl -> GetNotifiedExceptionClauseInfo(This,pinfo) ) 


#define ICorProfilerInfo11_EnumJITedFunctions(This,ppEnum)  \
    ( (This)->lpVtbl -> EnumJITedFunctions(This,ppEnum) ) 

#define ICorProfilerInfo11_RequestProfilerDetach(This,dwExpectedCompletionMilliseconds) \
    ( (This)->lpVtbl -> RequestProfilerDetach(This,dwExpectedCompletionMilliseconds) ) 

#define ICorProfilerInfo11_SetFunctionIDMapper2(This,pFunc,clientData)  \
    ( (This)->lpVtbl -> SetFunctionIDMapper2(This,pFunc,clientData) ) 

#define ICorProfilerInfo11_GetStringLayout2(This,pStringLengthOffset,pBufferOffset) \
    ( (This)->lpVtbl -> GetStringLayout2(This,pStringLengthOffset,pBufferOffset) ) 

#define ICorProfilerInfo11_SetEnterLeaveFunctionHooks3(This,pFuncEnter3,pFuncLeave3,pFuncTailcall3) \
    ( (This)->lpVtbl -> SetEnterLeaveFunctionHooks3(This,pFuncEnter3,pFuncLeave3,pFuncTailcall3) ) 

#define ICorProfilerInfo11_SetEnterLeaveFunctionHooks3WithInfo(This,pFuncEnter3WithInfo,pFuncLeave3WithInfo,pFuncTailcall3WithInfo) \
    ( (This)->lpVtbl -> SetEnterLeaveFunctionHooks3WithInfo(This,pFuncEnter3WithInfo,pFuncLeave3WithInfo,pFuncTailcall3WithInfo) ) 

#define ICorProfilerInfo11_GetFunctionEnter3Info(This,functionId,eltInfo,pFrameInfo,pcbArgumentInfo,pArgumentInfo)  \
    ( (This)->lpVtbl -> GetFunctionEnter3Info(This,functionId,eltInfo,pFrameInfo,pcbArgumentInfo,pArgumentInfo) ) 

#define ICorProfilerInfo11_GetFunctionLeave3Info(This,functionId,eltInfo,pFrameInfo,pRetvalRange)   \
    ( (This)->lpVtbl -> GetFunctionLeave3Info(This,functionId,eltInfo,pFrameInfo,pRetvalRange) ) 

#define ICorProfilerInfo11_GetFunctionTailcall3Info(This,functionId,eltInfo,pFrameInfo) \
    ( (This)->lpVtbl -> GetFunctionTailcall3Info(This,functionId,eltInfo,pFrameInfo) ) 

#define ICorProfilerInfo11_EnumModules(This,ppEnum) \
    ( (This)->lpVtbl -> EnumModules(This,ppEnum) ) 

#define ICorProfilerInfo11_GetRuntimeInformation(This,pClrInstanceId,pRuntimeType,pMajorVersion,pMinorVersion,pBuildNumber,pQFEVersion,cchVersionString,pcchVersionString,szVersionString)  \
    ( (This)->lpVtbl -> GetRuntimeInformation(This,pClrInstanceId,pRuntimeType,pMajorVersion,pMinorVersion,pBuildNumber,pQFEVersion,cchVersionString,pcchVersionString,szVersionString) ) 

#define ICorProfilerInfo11_GetThreadStaticAddress2(This,classId,fieldToken,appDomainId,threadId,ppAddress)  \
    ( (This)->lpVtbl -> GetThreadStaticAddress2(This,classId,fieldToken,appDomainId,threadId,ppAddress) ) 

#define ICorProfilerInfo11_GetAppDomainsContainingModule(This,moduleId,cAppDomainIds,pcAppDomainIds,appDomainIds)   \
    ( (This)->lpVtbl -> GetAppDomainsContainingModule(This,moduleId,cAppDomainIds,pcAppDomainIds,appDomainIds) ) 

#define ICorProfilerInfo11_GetModuleInfo2(This,moduleId,ppBaseLoadAddress,cchName,pcchName,szName,pAssemblyId,pdwModuleFlags)   \
    ( (This)->lpVtbl -> GetModuleInfo2(This,moduleId,ppBaseLoadAddress,cchName,pcchName,szName,pAssemblyId,pdwModuleFlags) ) 


#define ICorProfilerInfo11_EnumThreads(This,ppEnum) \
    ( (This)->lpVtbl -> EnumThreads(This,ppEnum) ) 

#define ICorProfilerInfo11_InitializeCurrentThread(This)    \
    ( (This)->lpVtbl -> InitializeCurrentThread(This) ) 

#define ICorProfilerInfo11_RequestReJIT(This,cFunctions,moduleIds,methodIds)    \
    ( (This)->lpVtbl -> RequestReJIT(This,cFunctions,moduleIds,methodIds) ) 

#define ICorProfilerInfo11_RequestRevert(This,cFunctions,moduleIds,methodIds,status)    \
    ( (This)->lpVtbl -> RequestRevert(This,cFunctions,moduleIds,methodIds,status) ) 

#define ICorProfilerInfo11_GetCodeInfo3(This,functionID,reJitId,cCodeInfos,pcCodeInfos,codeInfos)   \
    ( (This)->lpVtbl -> GetCodeInfo3(This,functionID,reJitId,cCodeInfos,pcCodeInfos,codeInfos) ) 

#define ICorProfilerInfo11_GetFunctionFromIP2(This,ip,pFunctionId,pReJitId) \
    ( (This)->lpVtbl -> GetFunctionFromIP2(This,ip,pFunctionId,pReJitId) ) 

#define ICorProfilerInfo11_GetReJITIDs(This,functionId,cReJitIds,pcReJitIds,reJitIds)   \
    ( (This)->lpVtbl -> GetReJITIDs(This,functionId,cReJitIds,pcReJitIds,reJitIds) ) 

#define ICorProfilerInfo11_GetILToNativeMapping2(This,functionId,reJitId,cMap,pcMap,map)    \
    ( (This)->lpVtbl -> GetILToNativeMapping2(This,functionId,reJitId,cMap,pcMap,map) ) 

#define ICorProfilerInfo11_EnumJITedFunctions2(This,ppEnum) \
    ( (This)->lpVtbl -> EnumJITedFunctions2(This,ppEnum) ) 

#define ICorProfilerInfo11_GetObjectSize2(This,objectId,pcSize) \
    ( (This)->lpVtbl -> GetObjectSize2(This,objectId,pcSize) ) 


#define ICorProfilerInfo11_GetEventMask2(This,pdwEventsLow,pdwEventsHigh)   \
    ( (This)->lpVtbl -> GetEventMask2(This,pdwEventsLow,pdwEventsHigh) ) 

#define ICorProfilerInfo11_SetEventMask2(This,dwEventsLow,dwEventsHigh) \
    ( (This)->lpVtbl -> SetEventMask2(This,dwEventsLow,dwEventsHigh) ) 


#define ICorProfilerInfo11_EnumNgenModuleMethodsInliningThisMethod(This,inlinersModuleId,inlineeModuleId,inlineeMethodId,incompleteData,ppEnum) \
    ( (This)->lpVtbl -> EnumNgenModuleMethodsInliningThisMethod(This,inlinersModuleId,inlineeModuleId,inlineeMethodId,incompleteData,ppEnum) ) 


#define ICorProfilerInfo11_ApplyMetaData(This,moduleId) \
    ( (This)->lpVtbl -> ApplyMetaData(This,moduleId) ) 

#define ICorProfilerInfo11_GetInMemorySymbolsLength(This,moduleId,pCountSymbolBytes)    \
    ( (This)->lpVtbl -> GetInMemorySymbolsLength(This,moduleId,pCountSymbolBytes) ) 

#define ICorProfilerInfo11_ReadInMemorySymbols(This,moduleId,symbolsReadOffset,pSymbolBytes,countSymbolBytes,pCountSymbolBytesRead) \
    ( (This)->lpVtbl -> ReadInMemorySymbols(This,moduleId,symbolsReadOffset,pSymbolBytes,countSymbolBytes,pCountSymbolBytesRead) ) 


#define ICorProfilerInfo11_IsFunctionDynamic(This,functionId,isDynamic) \
    ( (This)->lpVtbl -> IsFunctionDynamic(This,functionId,isDynamic) ) 

#define ICorProfilerInfo11_GetFunctionFromIP3(This,ip,functionId,pReJitId)  \
    ( (This)->lpVtbl -> GetFunctionFromIP3(This,ip,functionId,pReJitId) ) 

#define ICorProfilerInfo11_GetDynamicFunctionInfo(This,functionId,moduleId,ppvSig,pbSig,cchName,pcchName,wszName)   \
    ( (This)->lpVtbl -> GetDynamicFunctionInfo(This,functionId,moduleId,ppvSig,pbSig,cchName,pcchName,wszName) ) 


#define ICorProfilerInfo11_GetNativeCodeStartAddresses(This,functionID,reJitId,cCodeStartAddresses,pcCodeStartAddresses,codeStartAddresses) \
    ( (This)->lpVtbl -> GetNativeCodeStartAddresses(This,functionID,reJitId,cCodeStartAddresses,pcCodeStartAddresses,codeStartAddresses) ) 

#define ICorProfilerInfo11_GetILToNativeMapping3(This,pNativeCodeStartAddress,cMap,pcMap,map)   \
    ( (This)->lpVtbl -> GetILToNativeMapping3(This,pNativeCodeStartAddress,cMap,pcMap,map) ) 

#define ICorProfilerInfo11_GetCodeInfo4(This,pNativeCodeStartAddress,cCodeInfos,pcCodeInfos,codeInfos)  \
    ( (This)->lpVtbl -> GetCodeInfo4(This,pNativeCodeStartAddress,cCodeInfos,pcCodeInfos,codeInfos) ) 


#define ICorProfilerInfo11_EnumerateObjectReferences(This,objectId,callback,clientData) \
    ( (This)->lpVtbl -> EnumerateObjectReferences(This,objectId,callback,clientData) ) 

#define ICorProfilerInfo11_IsFrozenObject(This,objectId,pbFrozen)   \
    ( (This)->lpVtbl -> IsFrozenObject(This,objectId,pbFrozen) ) 

#define ICorProfilerInfo11_GetLOHObjectSizeThreshold(This,pThreshold)   \
    ( (This)->lpVtbl -> GetLOHObjectSizeThreshold(This,pThreshold) ) 

#define ICorProfilerInfo11_RequestReJITWithInliners(This,dwRejitFlags,cFunctions,moduleIds,methodIds)   \
    ( (This)->lpVtbl -> RequestReJITWithInliners(This,dwRejitFlags,cFunctions,moduleIds,methodIds) ) 

#define ICorProfilerInfo11_SuspendRuntime(This) \
    ( (This)->lpVtbl -> SuspendRuntime(This) ) 

#define ICorProfilerInfo11_ResumeRuntime(This)  \
    ( (This)->lpVtbl -> ResumeRuntime(This) ) 


#define ICorProfilerInfo11_GetEnvironmentVariable(This,szName,cchValue,pcchValue,szValue)   \
    ( (This)->lpVtbl -> GetEnvironmentVariable(This,szName,cchValue,pcchValue,szValue) ) 

#define ICorProfilerInfo11_SetEnvironmentVariable(This,szName,szValue)  \
    ( (This)->lpVtbl -> SetEnvironmentVariable(This,szName,szValue) ) 

#endif /* COBJMACROS */


#endif  /* C style interface */




#endif  /* __ICorProfilerInfo11_INTERFACE_DEFINED__ */


#ifndef __ICorProfilerInfo12_INTERFACE_DEFINED__
#define __ICorProfilerInfo12_INTERFACE_DEFINED__

/* interface ICorProfilerInfo12 */
/* [local][unique][uuid][object] */ 


EXTERN_C const IID IID_ICorProfilerInfo12;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("27b24ccd-1cb1-47c5-96ee-98190dc30959")
    ICorProfilerInfo12 : public ICorProfilerInfo11
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE EventPipeStartSession( 
            /* [in] */ UINT32 cProviderConfigs,
            /* [size_is][in] */ COR_PRF_EVENTPIPE_PROVIDER_CONFIG pProviderConfigs[  ],
            /* [in] */ BOOL requestRundown,
            /* [out] */ EVENTPIPE_SESSION *pSession) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE EventPipeAddProviderToSession( 
            /* [in] */ EVENTPIPE_SESSION session,
            /* [in] */ COR_PRF_EVENTPIPE_PROVIDER_CONFIG providerConfig) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE EventPipeStopSession( 
            /* [in] */ EVENTPIPE_SESSION session) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE EventPipeCreateProvider( 
            /* [string][in] */ const WCHAR *providerName,
            /* [out] */ EVENTPIPE_PROVIDER *pProvider) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE EventPipeGetProviderInfo( 
            /* [in] */ EVENTPIPE_PROVIDER provider,
            /* [in] */ ULONG cchName,
            /* [out] */ ULONG *pcchName,
            /* [annotation][out] */ 
            _Out_writes_to_(cchName, *pcchName)  WCHAR providerName[  ]) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE EventPipeDefineEvent( 
            /* [in] */ EVENTPIPE_PROVIDER provider,
            /* [string][in] */ const WCHAR *eventName,
            /* [in] */ UINT32 eventID,
            /* [in] */ UINT64 keywords,
            /* [in] */ UINT32 eventVersion,
            /* [in] */ UINT32 level,
            /* [in] */ UINT8 opcode,
            /* [in] */ BOOL needStack,
            /* [in] */ UINT32 cParamDescs,
            /* [size_is][in] */ COR_PRF_EVENTPIPE_PARAM_DESC pParamDescs[  ],
            /* [out] */ EVENTPIPE_EVENT *pEvent) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE EventPipeWriteEvent( 
            /* [in] */ EVENTPIPE_EVENT event,
            /* [in] */ UINT32 cData,
            /* [size_is][in] */ COR_PRF_EVENT_DATA data[  ],
            /* [in] */ LPCGUID pActivityId,
            /* [in] */ LPCGUID pRelatedActivityId) = 0;
        
    };
    
    
#else   /* C style interface */

    typedef struct ICorProfilerInfo12Vtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            ICorProfilerInfo12 * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            ICorProfilerInfo12 * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            ICorProfilerInfo12 * This);
        
        HRESULT ( STDMETHODCALLTYPE *GetClassFromObject )( 
            ICorProfilerInfo12 * This,
            /* [in] */ ObjectID objectId,
            /* [out] */ ClassID *pClassId);
        
        HRESULT ( STDMETHODCALLTYPE *GetClassFromToken )( 
            ICorProfilerInfo12 * This,
            /* [in] */ ModuleID moduleId,
            /* [in] */ mdTypeDef typeDef,
            /* [out] */ ClassID *pClassId);
        
        HRESULT ( STDMETHODCALLTYPE *GetCodeInfo )( 
            ICorProfilerInfo12 * This,
            /* [in] */ FunctionID functionId,
            /* [out] */ LPCBYTE *pStart,
            /* [out] */ ULONG *pcSize);
        
        HRESULT ( STDMETHODCALLTYPE *GetEventMask )( 
            ICorProfilerInfo12 * This,
            /* [out] */ DWORD *pdwEvents);
        
        HRESULT ( STDMETHODCALLTYPE *GetFunctionFromIP )( 
            ICorProfilerInfo12 * This,
            /* [in] */ LPCBYTE ip,
            /* [out] */ FunctionID *pFunctionId);
        
        HRESULT ( STDMETHODCALLTYPE *GetFunctionFromToken )( 
            ICorProfilerInfo12 * This,
            /* [in] */ ModuleID moduleId,
            /* [in] */ mdToken token,
            /* [out] */ FunctionID *pFunctionId);
        
        HRESULT ( STDMETHODCALLTYPE *GetHandleFromThread )( 
            ICorProfilerInfo12 * This,
            /* [in] */ ThreadID threadId,
            /* [out] */ HANDLE *phThread);
        
        HRESULT ( STDMETHODCALLTYPE *GetObjectSize )( 
            ICorProfilerInfo12 * This,
            /* [in] */ ObjectID objectId,
            /* [out] */ ULONG *pcSize);
        
        HRESULT ( STDMETHODCALLTYPE *IsArrayClass )( 
            ICorProfilerInfo12 * This,
            /* [in] */ ClassID classId,
            /* [out] */ CorElementType *pBaseElemType,
            /* [out] */ ClassID *pBaseClassId,
            /* [out] */ ULONG *pcRank);
        
        HRESULT ( STDMETHODCALLTYPE *GetThreadInfo )( 
            ICorProfilerInfo12 * This,
            /* [in] */ ThreadID threadId,
            /* [out] */ DWORD *pdwWin32ThreadId);
        
        HRESULT ( STDMETHODCALLTYPE *GetCurrentThreadID )( 
            ICorProfilerInfo12 * This,
            /* [out] */ ThreadID *pThreadId);
        
        HRESULT ( STDMETHODCALLTYPE *GetClassIDInfo )( 
            ICorProfilerInfo12 * This,
            /* [in] */ ClassID classId,
            /* [out] */ ModuleID *pModuleId,
            /* [out] */ mdTypeDef *pTypeDefToken);
        
        HRESULT ( STDMETHODCALLTYPE *GetFunctionInfo )( 
            ICorProfilerInfo12 * This,
            /* [in] */ FunctionID functionId,
            /* [out] */ ClassID *pClassId,
            /* [out] */ ModuleID *pModuleId,
            /* [out] */ mdToken *pToken);
        
        HRESULT ( STDMETHODCALLTYPE *SetEventMask )( 
            ICorProfilerInfo12 * This,
            /* [in] */ DWORD dwEvents);
        
        HRESULT ( STDMETHODCALLTYPE *SetEnterLeaveFunctionHooks )( 
            ICorProfilerInfo12 * This,
            /* [in] */ FunctionEnter *pFuncEnter,
            /* [in] */ FunctionLeave *pFuncLeave,
            /* [in] */ FunctionTailcall *pFuncTailcall);
        
        HRESULT ( STDMETHODCALLTYPE *SetFunctionIDMapper )( 
            ICorProfilerInfo12 * This,
            /* [in] */ FunctionIDMapper *pFunc);
        
        HRESULT ( STDMETHODCALLTYPE *GetTokenAndMetaDataFromFunction )( 
            ICorProfilerInfo12 * This,
            /* [in] */ FunctionID functionId,
            /* [in] */ REFIID riid,
            /* [out] */ IUnknown **ppImport,
            /* [out] */ mdToken *pToken);
        
        HRESULT ( STDMETHODCALLTYPE *GetModuleInfo )( 
            ICorProfilerInfo12 * This,
            /* [in] */ ModuleID moduleId,
            /* [out] */ LPCBYTE *ppBaseLoadAddress,
            /* [in] */ ULONG cchName,
            /* [out] */ ULONG *pcchName,
            /* [annotation][out] */ 
            _Out_writes_to_(cchName, *pcchName)  WCHAR szName[  ],
            /* [out] */ AssemblyID *pAssemblyId);
        
        HRESULT ( STDMETHODCALLTYPE *GetModuleMetaData )( 
            ICorProfilerInfo12 * This,
            /* [in] */ ModuleID moduleId,
            /* [in] */ DWORD dwOpenFlags,
            /* [in] */ REFIID riid,
            /* [out] */ IUnknown **ppOut);
        
        HRESULT ( STDMETHODCALLTYPE *GetILFunctionBody )( 
            ICorProfilerInfo12 * This,
            /* [in] */ ModuleID moduleId,
            /* [in] */ mdMethodDef methodId,
            /* [out] */ LPCBYTE *ppMethodHeader,
            /* [out] */ ULONG *pcbMethodSize);
        
        HRESULT ( STDMETHODCALLTYPE *GetILFunctionBodyAllocator )( 
            ICorProfilerInfo12 * This,
            /* [in] */ ModuleID moduleId,
            /* [out] */ IMethodMalloc **ppMalloc);
        
        HRESULT ( STDMETHODCALLTYPE *SetILFunctionBody )( 
            ICorProfilerInfo12 * This,
            /* [in] */ ModuleID moduleId,
            /* [in] */ mdMethodDef methodid,
            /* [in] */ LPCBYTE pbNewILMethodHeader);
        
        HRESULT ( STDMETHODCALLTYPE *GetAppDomainInfo )( 
            ICorProfilerInfo12 * This,
            /* [in] */ AppDomainID appDomainId,
            /* [in] */ ULONG cchName,
            /* [out] */ ULONG *pcchName,
            /* [annotation][out] */ 
            _Out_writes_to_(cchName, *pcchName)  WCHAR szName[  ],
            /* [out] */ ProcessID *pProcessId);
        
        HRESULT ( STDMETHODCALLTYPE *GetAssemblyInfo )( 
            ICorProfilerInfo12 * This,
            /* [in] */ AssemblyID assemblyId,
            /* [in] */ ULONG cchName,
            /* [out] */ ULONG *pcchName,
            /* [annotation][out] */ 
            _Out_writes_to_(cchName, *pcchName)  WCHAR szName[  ],
            /* [out] */ AppDomainID *pAppDomainId,
            /* [out] */ ModuleID *pModuleId);
        
        HRESULT ( STDMETHODCALLTYPE *SetFunctionReJIT )( 
            ICorProfilerInfo12 * This,
            /* [in] */ FunctionID functionId);
        
        HRESULT ( STDMETHODCALLTYPE *ForceGC )( 
            ICorProfilerInfo12 * This);
        
        HRESULT ( STDMETHODCALLTYPE *SetILInstrumentedCodeMap )( 
            ICorProfilerInfo12 * This,
            /* [in] */ FunctionID functionId,
            /* [in] */ BOOL fStartJit,
            /* [in] */ ULONG cILMapEntries,
            /* [size_is][in] */ COR_IL_MAP rgILMapEntries[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *GetInprocInspectionInterface )( 
            ICorProfilerInfo12 * This,
            /* [out] */ IUnknown **ppicd);
        
        HRESULT ( STDMETHODCALLTYPE *GetInprocInspectionIThisThread )( 
            ICorProfilerInfo12 * This,
            /* [out] */ IUnknown **ppicd);
        
        HRESULT ( STDMETHODCALLTYPE *GetThreadContext )( 
            ICorProfilerInfo12 * This,
            /* [in] */ ThreadID threadId,
            /* [out] */ ContextID *pContextId);
        
        HRESULT ( STDMETHODCALLTYPE *BeginInprocDebugging )( 
            ICorProfilerInfo12 * This,
            /* [in] */ BOOL fThisThreadOnly,
            /* [out] */ DWORD *pdwProfilerContext);
        
        HRESULT ( STDMETHODCALLTYPE *EndInprocDebugging )( 
            ICorProfilerInfo12 * This,
            /* [in] */ DWORD dwProfilerContext);
        
        HRESULT ( STDMETHODCALLTYPE *GetILToNativeMapping )( 
            ICorProfilerInfo12 * This,
            /* [in] */ FunctionID functionId,
            /* [in] */ ULONG32 cMap,
            /* [out] */ ULONG32 *pcMap,
            /* [length_is][size_is][out] */ COR_DEBUG_IL_TO_NATIVE_MAP map[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *DoStackSnapshot )( 
            ICorProfilerInfo12 * This,
            /* [in] */ ThreadID thread,
            /* [in] */ StackSnapshotCallback *callback,
            /* [in] */ ULONG32 infoFlags,
            /* [in] */ void *clientData,
            /* [size_is][in] */ BYTE context[  ],
            /* [in] */ ULONG32 contextSize);
        
        HRESULT ( STDMETHODCALLTYPE *SetEnterLeaveFunctionHooks2 )( 
            ICorProfilerInfo12 * This,
            /* [in] */ FunctionEnter2 *pFuncEnter,
            /* [in] */ FunctionLeave2 *pFuncLeave,
            /* [in] */ FunctionTailcall2 *pFuncTailcall);
        
        HRESULT ( STDMETHODCALLTYPE *GetFunctionInfo2 )( 
            ICorProfilerInfo12 * This,
            /* [in] */ FunctionID funcId,
            /* [in] */ COR_PRF_FRAME_INFO frameInfo,
            /* [out] */ ClassID *pClassId,
            /* [out] */ ModuleID *pModuleId,
            /* [out] */ mdToken *pToken,
            /* [in] */ ULONG32 cTypeArgs,
            /* [out] */ ULONG32 *pcTypeArgs,
            /* [out] */ ClassID typeArgs[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *GetStringLayout )( 
            ICorProfilerInfo12 * This,
            /* [out] */ ULONG *pBufferLengthOffset,
            /* [out] */ ULONG *pStringLengthOffset,
            /* [out] */ ULONG *pBufferOffset);
        
        HRESULT ( STDMETHODCALLTYPE *GetClassLayout )( 
            ICorProfilerInfo12 * This,
            /* [in] */ ClassID classID,
            /* [out][in] */ COR_FIELD_OFFSET rFieldOffset[  ],
            /* [in] */ ULONG cFieldOffset,
            /* [out] */ ULONG *pcFieldOffset,
            /* [out] */ ULONG *pulClassSize);
        
        HRESULT ( STDMETHODCALLTYPE *GetClassIDInfo2 )( 
            ICorProfilerInfo12 * This,
            /* [in] */ ClassID classId,
            /* [out] */ ModuleID *pModuleId,
            /* [out] */ mdTypeDef *pTypeDefToken,
            /* [out] */ ClassID *pParentClassId,
            /* [in] */ ULONG32 cNumTypeArgs,
            /* [out] */ ULONG32 *pcNumTypeArgs,
            /* [out] */ ClassID typeArgs[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *GetCodeInfo2 )( 
            ICorProfilerInfo12 * This,
            /* [in] */ FunctionID functionID,
            /* [in] */ ULONG32 cCodeInfos,
            /* [out] */ ULONG32 *pcCodeInfos,
            /* [length_is][size_is][out] */ COR_PRF_CODE_INFO codeInfos[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *GetClassFromTokenAndTypeArgs )( 
            ICorProfilerInfo12 * This,
            /* [in] */ ModuleID moduleID,
            /* [in] */ mdTypeDef typeDef,
            /* [in] */ ULONG32 cTypeArgs,
            /* [size_is][in] */ ClassID typeArgs[  ],
            /* [out] */ ClassID *pClassID);
        
        HRESULT ( STDMETHODCALLTYPE *GetFunctionFromTokenAndTypeArgs )( 
            ICorProfilerInfo12 * This,
            /* [in] */ ModuleID moduleID,
            /* [in] */ mdMethodDef funcDef,
            /* [in] */ ClassID classId,
            /* [in] */ ULONG32 cTypeArgs,
            /* [size_is][in] */ ClassID typeArgs[  ],
            /* [out] */ FunctionID *pFunctionID);
        
        HRESULT ( STDMETHODCALLTYPE *EnumModuleFrozenObjects )( 
            ICorProfilerInfo12 * This,
            /* [in] */ ModuleID moduleID,
            /* [out] */ ICorProfilerObjectEnum **ppEnum);
        
        HRESULT ( STDMETHODCALLTYPE *GetArrayObjectInfo )( 
            ICorProfilerInfo12 * This,
            /* [in] */ ObjectID objectId,
            /* [in] */ ULONG32 cDimensions,
            /* [size_is][out] */ ULONG32 pDimensionSizes[  ],
            /* [size_is][out] */ int pDimensionLowerBounds[  ],
            /* [out] */ BYTE **ppData);
        
        HRESULT ( STDMETHODCALLTYPE *GetBoxClassLayout )( 
            ICorProfilerInfo12 * This,
            /* [in] */ ClassID classId,
            /* [out] */ ULONG32 *pBufferOffset);
        
        HRESULT ( STDMETHODCALLTYPE *GetThreadAppDomain )( 
            ICorProfilerInfo12 * This,
            /* [in] */ ThreadID threadId,
            /* [out] */ AppDomainID *pAppDomainId);
        
        HRESULT ( STDMETHODCALLTYPE *GetRVAStaticAddress )( 
            ICorProfilerInfo12 * This,
            /* [in] */ ClassID classId,
            /* [in] */ mdFieldDef fieldToken,
            /* [out] */ void **ppAddress);
        
        HRESULT ( STDMETHODCALLTYPE *GetAppDomainStaticAddress )( 
            ICorProfilerInfo12 * This,
            /* [in] */ ClassID classId,
            /* [in] */ mdFieldDef fieldToken,
            /* [in] */ AppDomainID appDomainId,
            /* [out] */ void **ppAddress);
        
        HRESULT ( STDMETHODCALLTYPE *GetThreadStaticAddress )( 
            ICorProfilerInfo12 * This,
            /* [in] */ ClassID classId,
            /* [in] */ mdFieldDef fieldToken,
            /* [in] */ ThreadID threadId,
            /* [out] */ void **ppAddress);
        
        HRESULT ( STDMETHODCALLTYPE *GetContextStaticAddress )( 
            ICorProfilerInfo12 * This,
            /* [in] */ ClassID classId,
            /* [in] */ mdFieldDef fieldToken,
            /* [in] */ ContextID contextId,
            /* [out] */ void **ppAddress);
        
        HRESULT ( STDMETHODCALLTYPE *GetStaticFieldInfo )( 
            ICorProfilerInfo12 * This,
            /* [in] */ ClassID classId,
            /* [in] */ mdFieldDef fieldToken,
            /* [out] */ COR_PRF_STATIC_TYPE *pFieldInfo);
        
        HRESULT ( STDMETHODCALLTYPE *GetGenerationBounds )( 
            ICorProfilerInfo12 * This,
            /* [in] */ ULONG cObjectRanges,
            /* [out] */ ULONG *pcObjectRanges,
            /* [length_is][size_is][out] */ COR_PRF_GC_GENERATION_RANGE ranges[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *GetObjectGeneration )( 
            ICorProfilerInfo12 * This,
            /* [in] */ ObjectID objectId,
            /* [out] */ COR_PRF_GC_GENERATION_RANGE *range);
        
        HRESULT ( STDMETHODCALLTYPE *GetNotifiedExceptionClauseInfo )( 
            ICorProfilerInfo12 * This,
            /* [out] */ COR_PRF_EX_CLAUSE_INFO *pinfo);
        
        HRESULT ( STDMETHODCALLTYPE *EnumJITedFunctions )( 
            ICorProfilerInfo12 * This,
            /* [out] */ ICorProfilerFunctionEnum **ppEnum);
        
        HRESULT ( STDMETHODCALLTYPE *RequestProfilerDetach )( 
            ICorProfilerInfo12 * This,
            /* [in] */ DWORD dwExpectedCompletionMilliseconds);
        
        HRESULT ( STDMETHODCALLTYPE *SetFunctionIDMapper2 )( 
            ICorProfilerInfo12 * This,
            /* [in] */ FunctionIDMapper2 *pFunc,
            /* [in] */ void *clientData);
        
        HRESULT ( STDMETHODCALLTYPE *GetStringLayout2 )( 
            ICorProfilerInfo12 * This,
            /* [out] */ ULONG *pStringLengthOffset,
            /* [out] */ ULONG *pBufferOffset);
        
        HRESULT ( STDMETHODCALLTYPE *SetEnterLeaveFunctionHooks3 )( 
            ICorProfilerInfo12 * This,
            /* [in] */ FunctionEnter3 *pFuncEnter3,
            /* [in] */ FunctionLeave3 *pFuncLeave3,
            /* [in] */ FunctionTailcall3 *pFuncTailcall3);
        
        HRESULT ( STDMETHODCALLTYPE *SetEnterLeaveFunctionHooks3WithInfo )( 
            ICorProfilerInfo12 * This,
            /* [in] */ FunctionEnter3WithInfo *pFuncEnter3WithInfo,
            /* [in] */ FunctionLeave3WithInfo *pFuncLeave3WithInfo,
            /* [in] */ FunctionTailcall3WithInfo *pFuncTailcall3WithInfo);
        
        HRESULT ( STDMETHODCALLTYPE *GetFunctionEnter3Info )( 
            ICorProfilerInfo12 * This,
            /* [in] */ FunctionID functionId,
            /* [in] */ COR_PRF_ELT_INFO eltInfo,
            /* [out] */ COR_PRF_FRAME_INFO *pFrameInfo,
            /* [out][in] */ ULONG *pcbArgumentInfo,
            /* [size_is][out] */ COR_PRF_FUNCTION_ARGUMENT_INFO *pArgumentInfo);
        
        HRESULT ( STDMETHODCALLTYPE *GetFunctionLeave3Info )( 
            ICorProfilerInfo12 * This,
            /* [in] */ FunctionID functionId,
            /* [in] */ COR_PRF_ELT_INFO eltInfo,
            /* [out] */ COR_PRF_FRAME_INFO *pFrameInfo,
            /* [out] */ COR_PRF_FUNCTION_ARGUMENT_RANGE *pRetvalRange);
        
        HRESULT ( STDMETHODCALLTYPE *GetFunctionTailcall3Info )( 
            ICorProfilerInfo12 * This,
            /* [in] */ FunctionID functionId,
            /* [in] */ COR_PRF_ELT_INFO eltInfo,
            /* [out] */ COR_PRF_FRAME_INFO *pFrameInfo);
        
        HRESULT ( STDMETHODCALLTYPE *EnumModules )( 
            ICorProfilerInfo12 * This,
            /* [out] */ ICorProfilerModuleEnum **ppEnum);
        
        HRESULT ( STDMETHODCALLTYPE *GetRuntimeInformation )( 
            ICorProfilerInfo12 * This,
            /* [out] */ USHORT *pClrInstanceId,
            /* [out] */ COR_PRF_RUNTIME_TYPE *pRuntimeType,
            /* [out] */ USHORT *pMajorVersion,
            /* [out] */ USHORT *pMinorVersion,
            /* [out] */ USHORT *pBuildNumber,
            /* [out] */ USHORT *pQFEVersion,
            /* [in] */ ULONG cchVersionString,
            /* [out] */ ULONG *pcchVersionString,
            /* [annotation][out] */ 
            _Out_writes_to_(cchVersionString, *pcchVersionString)  WCHAR szVersionString[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *GetThreadStaticAddress2 )( 
            ICorProfilerInfo12 * This,
            /* [in] */ ClassID classId,
            /* [in] */ mdFieldDef fieldToken,
            /* [in] */ AppDomainID appDomainId,
            /* [in] */ ThreadID threadId,
            /* [out] */ void **ppAddress);
        
        HRESULT ( STDMETHODCALLTYPE *GetAppDomainsContainingModule )( 
            ICorProfilerInfo12 * This,
            /* [in] */ ModuleID moduleId,
            /* [in] */ ULONG32 cAppDomainIds,
            /* [out] */ ULONG32 *pcAppDomainIds,
            /* [length_is][size_is][out] */ AppDomainID appDomainIds[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *GetModuleInfo2 )( 
            ICorProfilerInfo12 * This,
            /* [in] */ ModuleID moduleId,
            /* [out] */ LPCBYTE *ppBaseLoadAddress,
            /* [in] */ ULONG cchName,
            /* [out] */ ULONG *pcchName,
            /* [annotation][out] */ 
            _Out_writes_to_(cchName, *pcchName)  WCHAR szName[  ],
            /* [out] */ AssemblyID *pAssemblyId,
            /* [out] */ DWORD *pdwModuleFlags);
        
        HRESULT ( STDMETHODCALLTYPE *EnumThreads )( 
            ICorProfilerInfo12 * This,
            /* [out] */ ICorProfilerThreadEnum **ppEnum);
        
        HRESULT ( STDMETHODCALLTYPE *InitializeCurrentThread )( 
            ICorProfilerInfo12 * This);
        
        HRESULT ( STDMETHODCALLTYPE *RequestReJIT )( 
            ICorProfilerInfo12 * This,
            /* [in] */ ULONG cFunctions,
            /* [size_is][in] */ ModuleID moduleIds[  ],
            /* [size_is][in] */ mdMethodDef methodIds[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *RequestRevert )( 
            ICorProfilerInfo12 * This,
            /* [in] */ ULONG cFunctions,
            /* [size_is][in] */ ModuleID moduleIds[  ],
            /* [size_is][in] */ mdMethodDef methodIds[  ],
            /* [size_is][out] */ HRESULT status[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *GetCodeInfo3 )( 
            ICorProfilerInfo12 * This,
            /* [in] */ FunctionID functionID,
            /* [in] */ ReJITID reJitId,
            /* [in] */ ULONG32 cCodeInfos,
            /* [out] */ ULONG32 *pcCodeInfos,
            /* [length_is][size_is][out] */ COR_PRF_CODE_INFO codeInfos[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *GetFunctionFromIP2 )( 
            ICorProfilerInfo12 * This,
            /* [in] */ LPCBYTE ip,
            /* [out] */ FunctionID *pFunctionId,
            /* [out] */ ReJITID *pReJitId);
        
        HRESULT ( STDMETHODCALLTYPE *GetReJITIDs )( 
            ICorProfilerInfo12 * This,
            /* [in] */ FunctionID functionId,
            /* [in] */ ULONG cReJitIds,
            /* [out] */ ULONG *pcReJitIds,
            /* [length_is][size_is][out] */ ReJITID reJitIds[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *GetILToNativeMapping2 )( 
            ICorProfilerInfo12 * This,
            /* [in] */ FunctionID functionId,
            /* [in] */ ReJITID reJitId,
            /* [in] */ ULONG32 cMap,
            /* [out] */ ULONG32 *pcMap,
            /* [length_is][size_is][out] */ COR_DEBUG_IL_TO_NATIVE_MAP map[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *EnumJITedFunctions2 )( 
            ICorProfilerInfo12 * This,
            /* [out] */ ICorProfilerFunctionEnum **ppEnum);
        
        HRESULT ( STDMETHODCALLTYPE *GetObjectSize2 )( 
            ICorProfilerInfo12 * This,
            /* [in] */ ObjectID objectId,
            /* [out] */ SIZE_T *pcSize);
        
        HRESULT ( STDMETHODCALLTYPE *GetEventMask2 )( 
            ICorProfilerInfo12 * This,
            /* [out] */ DWORD *pdwEventsLow,
            /* [out] */ DWORD *pdwEventsHigh);
        
        HRESULT ( STDMETHODCALLTYPE *SetEventMask2 )( 
            ICorProfilerInfo12 * This,
            /* [in] */ DWORD dwEventsLow,
            /* [in] */ DWORD dwEventsHigh);
        
        HRESULT ( STDMETHODCALLTYPE *EnumNgenModuleMethodsInliningThisMethod )( 
            ICorProfilerInfo12 * This,
            /* [in] */ ModuleID inlinersModuleId,
            /* [in] */ ModuleID inlineeModuleId,
            /* [in] */ mdMethodDef inlineeMethodId,
            /* [out] */ BOOL *incompleteData,
            /* [out] */ ICorProfilerMethodEnum **ppEnum);
        
        HRESULT ( STDMETHODCALLTYPE *ApplyMetaData )( 
            ICorProfilerInfo12 * This,
            /* [in] */ ModuleID moduleId);
        
        HRESULT ( STDMETHODCALLTYPE *GetInMemorySymbolsLength )( 
            ICorProfilerInfo12 * This,
            /* [in] */ ModuleID moduleId,
            /* [out] */ DWORD *pCountSymbolBytes);
        
        HRESULT ( STDMETHODCALLTYPE *ReadInMemorySymbols )( 
            ICorProfilerInfo12 * This,
            /* [in] */ ModuleID moduleId,
            /* [in] */ DWORD symbolsReadOffset,
            /* [out] */ BYTE *pSymbolBytes,
            /* [in] */ DWORD countSymbolBytes,
            /* [out] */ DWORD *pCountSymbolBytesRead);
        
        HRESULT ( STDMETHODCALLTYPE *IsFunctionDynamic )( 
            ICorProfilerInfo12 * This,
            /* [in] */ FunctionID functionId,
            /* [out] */ BOOL *isDynamic);
        
        HRESULT ( STDMETHODCALLTYPE *GetFunctionFromIP3 )( 
            ICorProfilerInfo12 * This,
            /* [in] */ LPCBYTE ip,
            /* [out] */ FunctionID *functionId,
            /* [out] */ ReJITID *pReJitId);
        
        HRESULT ( STDMETHODCALLTYPE *GetDynamicFunctionInfo )( 
            ICorProfilerInfo12 * This,
            /* [in] */ FunctionID functionId,
            /* [out] */ ModuleID *moduleId,
            /* [out] */ PCCOR_SIGNATURE *ppvSig,
            /* [out] */ ULONG *pbSig,
            /* [in] */ ULONG cchName,
            /* [out] */ ULONG *pcchName,
            /* [out] */ WCHAR wszName[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *GetNativeCodeStartAddresses )( 
            ICorProfilerInfo12 * This,
            FunctionID functionID,
            ReJITID reJitId,
            ULONG32 cCodeStartAddresses,
            ULONG32 *pcCodeStartAddresses,
            UINT_PTR codeStartAddresses[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *GetILToNativeMapping3 )( 
            ICorProfilerInfo12 * This,
            UINT_PTR pNativeCodeStartAddress,
            ULONG32 cMap,
            ULONG32 *pcMap,
            COR_DEBUG_IL_TO_NATIVE_MAP map[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *GetCodeInfo4 )( 
            ICorProfilerInfo12 * This,
            UINT_PTR pNativeCodeStartAddress,
            ULONG32 cCodeInfos,
            ULONG32 *pcCodeInfos,
            COR_PRF_CODE_INFO codeInfos[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *EnumerateObjectReferences )( 
            ICorProfilerInfo12 * This,
            ObjectID objectId,
            ObjectReferenceCallback callback,
            void *clientData);
        
        HRESULT ( STDMETHODCALLTYPE *IsFrozenObject )( 
            ICorProfilerInfo12 * This,
            ObjectID objectId,
            BOOL *pbFrozen);
        
        HRESULT ( STDMETHODCALLTYPE *GetLOHObjectSizeThreshold )( 
            ICorProfilerInfo12 * This,
            DWORD *pThreshold);
        
        HRESULT ( STDMETHODCALLTYPE *RequestReJITWithInliners )( 
            ICorProfilerInfo12 * This,
            /* [in] */ DWORD dwRejitFlags,
            /* [in] */ ULONG cFunctions,
            /* [size_is][in] */ ModuleID moduleIds[  ],
            /* [size_is][in] */ mdMethodDef methodIds[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *SuspendRuntime )( 
            ICorProfilerInfo12 * This);
        
        HRESULT ( STDMETHODCALLTYPE *ResumeRuntime )( 
            ICorProfilerInfo12 * This);
        
        HRESULT ( STDMETHODCALLTYPE *GetEnvironmentVariable )( 
            ICorProfilerInfo12 * This,
            /* [string][in] */ const WCHAR *szName,
            /* [in] */ ULONG cchValue,
            /* [out] */ ULONG *pcchValue,
            /* [annotation][out] */ 
            _Out_writes_to_(cchValue, *pcchValue)  WCHAR szValue[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *SetEnvironmentVariable )( 
            ICorProfilerInfo12 * This,
            /* [string][in] */ const WCHAR *szName,
            /* [string][in] */ const WCHAR *szValue);
        
        HRESULT ( STDMETHODCALLTYPE *EventPipeStartSession )( 
            ICorProfilerInfo12 * This,
            /* [in] */ UINT32 cProviderConfigs,
            /* [size_is][in] */ COR_PRF_EVENTPIPE_PROVIDER_CONFIG pProviderConfigs[  ],
            /* [in] */ BOOL requestRundown,
            /* [out] */ EVENTPIPE_SESSION *pSession);
        
        HRESULT ( STDMETHODCALLTYPE *EventPipeAddProviderToSession )( 
            ICorProfilerInfo12 * This,
            /* [in] */ EVENTPIPE_SESSION session,
            /* [in] */ COR_PRF_EVENTPIPE_PROVIDER_CONFIG providerConfig);
        
        HRESULT ( STDMETHODCALLTYPE *EventPipeStopSession )( 
            ICorProfilerInfo12 * This,
            /* [in] */ EVENTPIPE_SESSION session);
        
        HRESULT ( STDMETHODCALLTYPE *EventPipeCreateProvider )( 
            ICorProfilerInfo12 * This,
            /* [string][in] */ const WCHAR *providerName,
            /* [out] */ EVENTPIPE_PROVIDER *pProvider);
        
        HRESULT ( STDMETHODCALLTYPE *EventPipeGetProviderInfo )( 
            ICorProfilerInfo12 * This,
            /* [in] */ EVENTPIPE_PROVIDER provider,
            /* [in] */ ULONG cchName,
            /* [out] */ ULONG *pcchName,
            /* [annotation][out] */ 
            _Out_writes_to_(cchName, *pcchName)  WCHAR providerName[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *EventPipeDefineEvent )( 
            ICorProfilerInfo12 * This,
            /* [in] */ EVENTPIPE_PROVIDER provider,
            /* [string][in] */ const WCHAR *eventName,
            /* [in] */ UINT32 eventID,
            /* [in] */ UINT64 keywords,
            /* [in] */ UINT32 eventVersion,
            /* [in] */ UINT32 level,
            /* [in] */ UINT8 opcode,
            /* [in] */ BOOL needStack,
            /* [in] */ UINT32 cParamDescs,
            /* [size_is][in] */ COR_PRF_EVENTPIPE_PARAM_DESC pParamDescs[  ],
            /* [out] */ EVENTPIPE_EVENT *pEvent);
        
        HRESULT ( STDMETHODCALLTYPE *EventPipeWriteEvent )( 
            ICorProfilerInfo12 * This,
            /* [in] */ EVENTPIPE_EVENT event,
            /* [in] */ UINT32 cData,
            /* [size_is][in] */ COR_PRF_EVENT_DATA data[  ],
            /* [in] */ LPCGUID pActivityId,
            /* [in] */ LPCGUID pRelatedActivityId);
        
        END_INTERFACE
    } ICorProfilerInfo12Vtbl;

    interface ICorProfilerInfo12
    {
        CONST_VTBL struct ICorProfilerInfo12Vtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define ICorProfilerInfo12_QueryInterface(This,riid,ppvObject)  \
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define ICorProfilerInfo12_AddRef(This) \
    ( (This)->lpVtbl -> AddRef(This) ) 

#define ICorProfilerInfo12_Release(This)    \
    ( (This)->lpVtbl -> Release(This) ) 


#define ICorProfilerInfo12_GetClassFromObject(This,objectId,pClassId)   \
    ( (This)->lpVtbl -> GetClassFromObject(This,objectId,pClassId) ) 

#define ICorProfilerInfo12_GetClassFromToken(This,moduleId,typeDef,pClassId)    \
    ( (This)->lpVtbl -> GetClassFromToken(This,moduleId,typeDef,pClassId) ) 

#define ICorProfilerInfo12_GetCodeInfo(This,functionId,pStart,pcSize)   \
    ( (This)->lpVtbl -> GetCodeInfo(This,functionId,pStart,pcSize) ) 

#define ICorProfilerInfo12_GetEventMask(This,pdwEvents) \
    ( (This)->lpVtbl -> GetEventMask(This,pdwEvents) ) 

#define ICorProfilerInfo12_GetFunctionFromIP(This,ip,pFunctionId)   \
    ( (This)->lpVtbl -> GetFunctionFromIP(This,ip,pFunctionId) ) 

#define ICorProfilerInfo12_GetFunctionFromToken(This,moduleId,token,pFunctionId)    \
    ( (This)->lpVtbl -> GetFunctionFromToken(This,moduleId,token,pFunctionId) ) 

#define ICorProfilerInfo12_GetHandleFromThread(This,threadId,phThread)  \
    ( (This)->lpVtbl -> GetHandleFromThread(This,threadId,phThread) ) 

#define ICorProfilerInfo12_GetObjectSize(This,objectId,pcSize)  \
    ( (This)->lpVtbl -> GetObjectSize(This,objectId,pcSize) ) 

#define ICorProfilerInfo12_IsArrayClass(This,classId,pBaseElemType,pBaseClassId,pcRank) \
    ( (This)->lpVtbl -> IsArrayClass(This,classId,pBaseElemType,pBaseClassId,pcRank) ) 

#define ICorProfilerInfo12_GetThreadInfo(This,threadId,pdwWin32ThreadId)    \
    ( (This)->lpVtbl -> GetThreadInfo(This,threadId,pdwWin32ThreadId) ) 

#define ICorProfilerInfo12_GetCurrentThreadID(This,pThreadId)   \
    ( (This)->lpVtbl -> GetCurrentThreadID(This,pThreadId) ) 

#define ICorProfilerInfo12_GetClassIDInfo(This,classId,pModuleId,pTypeDefToken) \
    ( (This)->lpVtbl -> GetClassIDInfo(This,classId,pModuleId,pTypeDefToken) ) 

#define ICorProfilerInfo12_GetFunctionInfo(This,functionId,pClassId,pModuleId,pToken)   \
    ( (This)->lpVtbl -> GetFunctionInfo(This,functionId,pClassId,pModuleId,pToken) ) 

#define ICorProfilerInfo12_SetEventMask(This,dwEvents)  \
    ( (This)->lpVtbl -> SetEventMask(This,dwEvents) ) 

#define ICorProfilerInfo12_SetEnterLeaveFunctionHooks(This,pFuncEnter,pFuncLeave,pFuncTailcall) \
    ( (This)->lpVtbl -> SetEnterLeaveFunctionHooks(This,pFuncEnter,pFuncLeave,pFuncTailcall) ) 

#define ICorProfilerInfo12_SetFunctionIDMapper(This,pFunc)  \
    ( (This)->lpVtbl -> SetFunctionIDMapper(This,pFunc) ) 

#define ICorProfilerInfo12_GetTokenAndMetaDataFromFunction(This,functionId,riid,ppImport,pToken)    \
    ( (This)->lpVtbl -> GetTokenAndMetaDataFromFunction(This,functionId,riid,ppImport,pToken) ) 

#define ICorProfilerInfo12_GetModuleInfo(This,moduleId,ppBaseLoadAddress,cchName,pcchName,szName,pAssemblyId)   \
    ( (This)->lpVtbl -> GetModuleInfo(This,moduleId,ppBaseLoadAddress,cchName,pcchName,szName,pAssemblyId) ) 

#define ICorProfilerInfo12_GetModuleMetaData(This,moduleId,dwOpenFlags,riid,ppOut)  \
    ( (This)->lpVtbl -> GetModuleMetaData(This,moduleId,dwOpenFlags,riid,ppOut) ) 

#define ICorProfilerInfo12_GetILFunctionBody(This,moduleId,methodId,ppMethodHeader,pcbMethodSize)   \
    ( (This)->lpVtbl -> GetILFunctionBody(This,moduleId,methodId,ppMethodHeader,pcbMethodSize) ) 

#define ICorProfilerInfo12_GetILFunctionBodyAllocator(This,moduleId,ppMalloc)   \
    ( (This)->lpVtbl -> GetILFunctionBodyAllocator(This,moduleId,ppMalloc) ) 

#define ICorProfilerInfo12_SetILFunctionBody(This,moduleId,methodid,pbNewILMethodHeader)    \
    ( (This)->lpVtbl -> SetILFunctionBody(This,moduleId,methodid,pbNewILMethodHeader) ) 

#define ICorProfilerInfo12_GetAppDomainInfo(This,appDomainId,cchName,pcchName,szName,pProcessId)    \
    ( (This)->lpVtbl -> GetAppDomainInfo(This,appDomainId,cchName,pcchName,szName,pProcessId) ) 

#define ICorProfilerInfo12_GetAssemblyInfo(This,assemblyId,cchName,pcchName,szName,pAppDomainId,pModuleId)  \
    ( (This)->lpVtbl -> GetAssemblyInfo(This,assemblyId,cchName,pcchName,szName,pAppDomainId,pModuleId) ) 

#define ICorProfilerInfo12_SetFunctionReJIT(This,functionId)    \
    ( (This)->lpVtbl -> SetFunctionReJIT(This,functionId) ) 

#define ICorProfilerInfo12_ForceGC(This)    \
    ( (This)->lpVtbl -> ForceGC(This) ) 

#define ICorProfilerInfo12_SetILInstrumentedCodeMap(This,functionId,fStartJit,cILMapEntries,rgILMapEntries) \
    ( (This)->lpVtbl -> SetILInstrumentedCodeMap(This,functionId,fStartJit,cILMapEntries,rgILMapEntries) ) 

#define ICorProfilerInfo12_GetInprocInspectionInterface(This,ppicd) \
    ( (This)->lpVtbl -> GetInprocInspectionInterface(This,ppicd) ) 

#define ICorProfilerInfo12_GetInprocInspectionIThisThread(This,ppicd)   \
    ( (This)->lpVtbl -> GetInprocInspectionIThisThread(This,ppicd) ) 

#define ICorProfilerInfo12_GetThreadContext(This,threadId,pContextId)   \
    ( (This)->lpVtbl -> GetThreadContext(This,threadId,pContextId) ) 

#define ICorProfilerInfo12_BeginInprocDebugging(This,fThisThreadOnly,pdwProfilerContext)    \
    ( (This)->lpVtbl -> BeginInprocDebugging(This,fThisThreadOnly,pdwProfilerContext) ) 

#define ICorProfilerInfo12_EndInprocDebugging(This,dwProfilerContext)   \
    ( (This)->lpVtbl -> EndInprocDebugging(This,dwProfilerContext) ) 

#define ICorProfilerInfo12_GetILToNativeMapping(This,functionId,cMap,pcMap,map) \
    ( (This)->lpVtbl -> GetILToNativeMapping(This,functionId,cMap,pcMap,map) ) 


#define ICorProfilerInfo12_DoStackSnapshot(This,thread,callback,infoFlags,clientData,context,contextSize)   \
    ( (This)->lpVtbl -> DoStackSnapshot(This,thread,callback,infoFlags,clientData,context,contextSize) ) 

#define ICorProfilerInfo12_SetEnterLeaveFunctionHooks2(This,pFuncEnter,pFuncLeave,pFuncTailcall)    \
    ( (This)->lpVtbl -> SetEnterLeaveFunctionHooks2(This,pFuncEnter,pFuncLeave,pFuncTailcall) ) 

#define ICorProfilerInfo12_GetFunctionInfo2(This,funcId,frameInfo,pClassId,pModuleId,pToken,cTypeArgs,pcTypeArgs,typeArgs)  \
    ( (This)->lpVtbl -> GetFunctionInfo2(This,funcId,frameInfo,pClassId,pModuleId,pToken,cTypeArgs,pcTypeArgs,typeArgs) ) 

#define ICorProfilerInfo12_GetStringLayout(This,pBufferLengthOffset,pStringLengthOffset,pBufferOffset)  \
    ( (This)->lpVtbl -> GetStringLayout(This,pBufferLengthOffset,pStringLengthOffset,pBufferOffset) ) 

#define ICorProfilerInfo12_GetClassLayout(This,classID,rFieldOffset,cFieldOffset,pcFieldOffset,pulClassSize)    \
    ( (This)->lpVtbl -> GetClassLayout(This,classID,rFieldOffset,cFieldOffset,pcFieldOffset,pulClassSize) ) 

#define ICorProfilerInfo12_GetClassIDInfo2(This,classId,pModuleId,pTypeDefToken,pParentClassId,cNumTypeArgs,pcNumTypeArgs,typeArgs) \
    ( (This)->lpVtbl -> GetClassIDInfo2(This,classId,pModuleId,pTypeDefToken,pParentClassId,cNumTypeArgs,pcNumTypeArgs,typeArgs) ) 

#define ICorProfilerInfo12_GetCodeInfo2(This,functionID,cCodeInfos,pcCodeInfos,codeInfos)   \
    ( (This)->lpVtbl -> GetCodeInfo2(This,functionID,cCodeInfos,pcCodeInfos,codeInfos) ) 

#define ICorProfilerInfo12_GetClassFromTokenAndTypeArgs(This,moduleID,typeDef,cTypeArgs,typeArgs,pClassID)  \
    ( (This)->lpVtbl -> GetClassFromTokenAndTypeArgs(This,moduleID,typeDef,cTypeArgs,typeArgs,pClassID) ) 

#define ICorProfilerInfo12_GetFunctionFromTokenAndTypeArgs(This,moduleID,funcDef,classId,cTypeArgs,typeArgs,pFunctionID)    \
    ( (This)->lpVtbl -> GetFunctionFromTokenAndTypeArgs(This,moduleID,funcDef,classId,cTypeArgs,typeArgs,pFunctionID) ) 

#define ICorProfilerInfo12_EnumModuleFrozenObjects(This,moduleID,ppEnum)    \
    ( (This)->lpVtbl -> EnumModuleFrozenObjects(This,moduleID,ppEnum) ) 

#define ICorProfilerInfo12_GetArrayObjectInfo(This,objectId,cDimensions,pDimensionSizes,pDimensionLowerBounds,ppData)   \
    ( (This)->lpVtbl -> GetArrayObjectInfo(This,objectId,cDimensions,pDimensionSizes,pDimensionLowerBounds,ppData) ) 

#define ICorProfilerInfo12_GetBoxClassLayout(This,classId,pBufferOffset)    \
    ( (This)->lpVtbl -> GetBoxClassLayout(This,classId,pBufferOffset) ) 

#define ICorProfilerInfo12_GetThreadAppDomain(This,threadId,pAppDomainId)   \
    ( (This)->lpVtbl -> GetThreadAppDomain(This,threadId,pAppDomainId) ) 

#define ICorProfilerInfo12_GetRVAStaticAddress(This,classId,fieldToken,ppAddress)   \
    ( (This)->lpVtbl -> GetRVAStaticAddress(This,classId,fieldToken,ppAddress) ) 

#define ICorProfilerInfo12_GetAppDomainStaticAddress(This,classId,fieldToken,appDomainId,ppAddress) \
    ( (This)->lpVtbl -> GetAppDomainStaticAddress(This,classId,fieldToken,appDomainId,ppAddress) ) 

#define ICorProfilerInfo12_GetThreadStaticAddress(This,classId,fieldToken,threadId,ppAddress)   \
    ( (This)->lpVtbl -> GetThreadStaticAddress(This,classId,fieldToken,threadId,ppAddress) ) 

#define ICorProfilerInfo12_GetContextStaticAddress(This,classId,fieldToken,contextId,ppAddress) \
    ( (This)->lpVtbl -> GetContextStaticAddress(This,classId,fieldToken,contextId,ppAddress) ) 

#define ICorProfilerInfo12_GetStaticFieldInfo(This,classId,fieldToken,pFieldInfo)   \
    ( (This)->lpVtbl -> GetStaticFieldInfo(This,classId,fieldToken,pFieldInfo) ) 

#define ICorProfilerInfo12_GetGenerationBounds(This,cObjectRanges,pcObjectRanges,ranges)    \
    ( (This)->lpVtbl -> GetGenerationBounds(This,cObjectRanges,pcObjectRanges,ranges) ) 

#define ICorProfilerInfo12_GetObjectGeneration(This,objectId,range) \
    ( (This)->lpVtbl -> GetObjectGeneration(This,objectId,range) ) 

#define ICorProfilerInfo12_GetNotifiedExceptionClauseInfo(This,pinfo)   \
    ( (This)->lpVtbl -> GetNotifiedExceptionClauseInfo(This,pinfo) ) 


#define ICorProfilerInfo12_EnumJITedFunctions(This,ppEnum)  \
    ( (This)->lpVtbl -> EnumJITedFunctions(This,ppEnum) ) 

#define ICorProfilerInfo12_RequestProfilerDetach(This,dwExpectedCompletionMilliseconds) \
    ( (This)->lpVtbl -> RequestProfilerDetach(This,dwExpectedCompletionMilliseconds) ) 

#define ICorProfilerInfo12_SetFunctionIDMapper2(This,pFunc,clientData)  \
    ( (This)->lpVtbl -> SetFunctionIDMapper2(This,pFunc,clientData) ) 

#define ICorProfilerInfo12_GetStringLayout2(This,pStringLengthOffset,pBufferOffset) \
    ( (This)->lpVtbl -> GetStringLayout2(This,pStringLengthOffset,pBufferOffset) ) 

#define ICorProfilerInfo12_SetEnterLeaveFunctionHooks3(This,pFuncEnter3,pFuncLeave3,pFuncTailcall3) \
    ( (This)->lpVtbl -> SetEnterLeaveFunctionHooks3(This,pFuncEnter3,pFuncLeave3,pFuncTailcall3) ) 

#define ICorProfilerInfo12_SetEnterLeaveFunctionHooks3WithInfo(This,pFuncEnter3WithInfo,pFuncLeave3WithInfo,pFuncTailcall3WithInfo) \
    ( (This)->lpVtbl -> SetEnterLeaveFunctionHooks3WithInfo(This,pFuncEnter3WithInfo,pFuncLeave3WithInfo,pFuncTailcall3WithInfo) ) 

#define ICorProfilerInfo12_GetFunctionEnter3Info(This,functionId,eltInfo,pFrameInfo,pcbArgumentInfo,pArgumentInfo)  \
    ( (This)->lpVtbl -> GetFunctionEnter3Info(This,functionId,eltInfo,pFrameInfo,pcbArgumentInfo,pArgumentInfo) ) 

#define ICorProfilerInfo12_GetFunctionLeave3Info(This,functionId,eltInfo,pFrameInfo,pRetvalRange)   \
    ( (This)->lpVtbl -> GetFunctionLeave3Info(This,functionId,eltInfo,pFrameInfo,pRetvalRange) ) 

#define ICorProfilerInfo12_GetFunctionTailcall3Info(This,functionId,eltInfo,pFrameInfo) \
    ( (This)->lpVtbl -> GetFunctionTailcall3Info(This,functionId,eltInfo,pFrameInfo) ) 

#define ICorProfilerInfo12_EnumModules(This,ppEnum) \
    ( (This)->lpVtbl -> EnumModules(This,ppEnum) ) 

#define ICorProfilerInfo12_GetRuntimeInformation(This,pClrInstanceId,pRuntimeType,pMajorVersion,pMinorVersion,pBuildNumber,pQFEVersion,cchVersionString,pcchVersionString,szVersionString)  \
    ( (This)->lpVtbl -> GetRuntimeInformation(This,pClrInstanceId,pRuntimeType,pMajorVersion,pMinorVersion,pBuildNumber,pQFEVersion,cchVersionString,pcchVersionString,szVersionString) ) 

#define ICorProfilerInfo12_GetThreadStaticAddress2(This,classId,fieldToken,appDomainId,threadId,ppAddress)  \
    ( (This)->lpVtbl -> GetThreadStaticAddress2(This,classId,fieldToken,appDomainId,threadId,ppAddress) ) 

#define ICorProfilerInfo12_GetAppDomainsContainingModule(This,moduleId,cAppDomainIds,pcAppDomainIds,appDomainIds)   \
    ( (This)->lpVtbl -> GetAppDomainsContainingModule(This,moduleId,cAppDomainIds,pcAppDomainIds,appDomainIds) ) 

#define ICorProfilerInfo12_GetModuleInfo2(This,moduleId,ppBaseLoadAddress,cchName,pcchName,szName,pAssemblyId,pdwModuleFlags)   \
    ( (This)->lpVtbl -> GetModuleInfo2(This,moduleId,ppBaseLoadAddress,cchName,pcchName,szName,pAssemblyId,pdwModuleFlags) ) 


#define ICorProfilerInfo12_EnumThreads(This,ppEnum) \
    ( (This)->lpVtbl -> EnumThreads(This,ppEnum) ) 

#define ICorProfilerInfo12_InitializeCurrentThread(This)    \
    ( (This)->lpVtbl -> InitializeCurrentThread(This) ) 

#define ICorProfilerInfo12_RequestReJIT(This,cFunctions,moduleIds,methodIds)    \
    ( (This)->lpVtbl -> RequestReJIT(This,cFunctions,moduleIds,methodIds) ) 

#define ICorProfilerInfo12_RequestRevert(This,cFunctions,moduleIds,methodIds,status)    \
    ( (This)->lpVtbl -> RequestRevert(This,cFunctions,moduleIds,methodIds,status) ) 

#define ICorProfilerInfo12_GetCodeInfo3(This,functionID,reJitId,cCodeInfos,pcCodeInfos,codeInfos)   \
    ( (This)->lpVtbl -> GetCodeInfo3(This,functionID,reJitId,cCodeInfos,pcCodeInfos,codeInfos) ) 

#define ICorProfilerInfo12_GetFunctionFromIP2(This,ip,pFunctionId,pReJitId) \
    ( (This)->lpVtbl -> GetFunctionFromIP2(This,ip,pFunctionId,pReJitId) ) 

#define ICorProfilerInfo12_GetReJITIDs(This,functionId,cReJitIds,pcReJitIds,reJitIds)   \
    ( (This)->lpVtbl -> GetReJITIDs(This,functionId,cReJitIds,pcReJitIds,reJitIds) ) 

#define ICorProfilerInfo12_GetILToNativeMapping2(This,functionId,reJitId,cMap,pcMap,map)    \
    ( (This)->lpVtbl -> GetILToNativeMapping2(This,functionId,reJitId,cMap,pcMap,map) ) 

#define ICorProfilerInfo12_EnumJITedFunctions2(This,ppEnum) \
    ( (This)->lpVtbl -> EnumJITedFunctions2(This,ppEnum) ) 

#define ICorProfilerInfo12_GetObjectSize2(This,objectId,pcSize) \
    ( (This)->lpVtbl -> GetObjectSize2(This,objectId,pcSize) ) 


#define ICorProfilerInfo12_GetEventMask2(This,pdwEventsLow,pdwEventsHigh)   \
    ( (This)->lpVtbl -> GetEventMask2(This,pdwEventsLow,pdwEventsHigh) ) 

#define ICorProfilerInfo12_SetEventMask2(This,dwEventsLow,dwEventsHigh) \
    ( (This)->lpVtbl -> SetEventMask2(This,dwEventsLow,dwEventsHigh) ) 


#define ICorProfilerInfo12_EnumNgenModuleMethodsInliningThisMethod(This,inlinersModuleId,inlineeModuleId,inlineeMethodId,incompleteData,ppEnum) \
    ( (This)->lpVtbl -> EnumNgenModuleMethodsInliningThisMethod(This,inlinersModuleId,inlineeModuleId,inlineeMethodId,incompleteData,ppEnum) ) 


#define ICorProfilerInfo12_ApplyMetaData(This,moduleId) \
    ( (This)->lpVtbl -> ApplyMetaData(This,moduleId) ) 

#define ICorProfilerInfo12_GetInMemorySymbolsLength(This,moduleId,pCountSymbolBytes)    \
    ( (This)->lpVtbl -> GetInMemorySymbolsLength(This,moduleId,pCountSymbolBytes) ) 

#define ICorProfilerInfo12_ReadInMemorySymbols(This,moduleId,symbolsReadOffset,pSymbolBytes,countSymbolBytes,pCountSymbolBytesRead) \
    ( (This)->lpVtbl -> ReadInMemorySymbols(This,moduleId,symbolsReadOffset,pSymbolBytes,countSymbolBytes,pCountSymbolBytesRead) ) 


#define ICorProfilerInfo12_IsFunctionDynamic(This,functionId,isDynamic) \
    ( (This)->lpVtbl -> IsFunctionDynamic(This,functionId,isDynamic) ) 

#define ICorProfilerInfo12_GetFunctionFromIP3(This,ip,functionId,pReJitId)  \
    ( (This)->lpVtbl -> GetFunctionFromIP3(This,ip,functionId,pReJitId) ) 

#define ICorProfilerInfo12_GetDynamicFunctionInfo(This,functionId,moduleId,ppvSig,pbSig,cchName,pcchName,wszName)   \
    ( (This)->lpVtbl -> GetDynamicFunctionInfo(This,functionId,moduleId,ppvSig,pbSig,cchName,pcchName,wszName) ) 


#define ICorProfilerInfo12_GetNativeCodeStartAddresses(This,functionID,reJitId,cCodeStartAddresses,pcCodeStartAddresses,codeStartAddresses) \
    ( (This)->lpVtbl -> GetNativeCodeStartAddresses(This,functionID,reJitId,cCodeStartAddresses,pcCodeStartAddresses,codeStartAddresses) ) 

#define ICorProfilerInfo12_GetILToNativeMapping3(This,pNativeCodeStartAddress,cMap,pcMap,map)   \
    ( (This)->lpVtbl -> GetILToNativeMapping3(This,pNativeCodeStartAddress,cMap,pcMap,map) ) 

#define ICorProfilerInfo12_GetCodeInfo4(This,pNativeCodeStartAddress,cCodeInfos,pcCodeInfos,codeInfos)  \
    ( (This)->lpVtbl -> GetCodeInfo4(This,pNativeCodeStartAddress,cCodeInfos,pcCodeInfos,codeInfos) ) 


#define ICorProfilerInfo12_EnumerateObjectReferences(This,objectId,callback,clientData) \
    ( (This)->lpVtbl -> EnumerateObjectReferences(This,objectId,callback,clientData) ) 

#define ICorProfilerInfo12_IsFrozenObject(This,objectId,pbFrozen)   \
    ( (This)->lpVtbl -> IsFrozenObject(This,objectId,pbFrozen) ) 

#define ICorProfilerInfo12_GetLOHObjectSizeThreshold(This,pThreshold)   \
    ( (This)->lpVtbl -> GetLOHObjectSizeThreshold(This,pThreshold) ) 

#define ICorProfilerInfo12_RequestReJITWithInliners(This,dwRejitFlags,cFunctions,moduleIds,methodIds)   \
    ( (This)->lpVtbl -> RequestReJITWithInliners(This,dwRejitFlags,cFunctions,moduleIds,methodIds) ) 

#define ICorProfilerInfo12_SuspendRuntime(This) \
    ( (This)->lpVtbl -> SuspendRuntime(This) ) 

#define ICorProfilerInfo12_ResumeRuntime(This)  \
    ( (This)->lpVtbl -> ResumeRuntime(This) ) 


#define ICorProfilerInfo12_GetEnvironmentVariable(This,szName,cchValue,pcchValue,szValue)   \
    ( (This)->lpVtbl -> GetEnvironmentVariable(This,szName,cchValue,pcchValue,szValue) ) 

#define ICorProfilerInfo12_SetEnvironmentVariable(This,szName,szValue)  \
    ( (This)->lpVtbl -> SetEnvironmentVariable(This,szName,szValue) ) 


#define ICorProfilerInfo12_EventPipeStartSession(This,cProviderConfigs,pProviderConfigs,requestRundown,pSession)    \
    ( (This)->lpVtbl -> EventPipeStartSession(This,cProviderConfigs,pProviderConfigs,requestRundown,pSession) ) 

#define ICorProfilerInfo12_EventPipeAddProviderToSession(This,session,providerConfig)   \
    ( (This)->lpVtbl -> EventPipeAddProviderToSession(This,session,providerConfig) ) 

#define ICorProfilerInfo12_EventPipeStopSession(This,session)   \
    ( (This)->lpVtbl -> EventPipeStopSession(This,session) ) 

#define ICorProfilerInfo12_EventPipeCreateProvider(This,providerName,pProvider) \
    ( (This)->lpVtbl -> EventPipeCreateProvider(This,providerName,pProvider) ) 

#define ICorProfilerInfo12_EventPipeGetProviderInfo(This,provider,cchName,pcchName,providerName)    \
    ( (This)->lpVtbl -> EventPipeGetProviderInfo(This,provider,cchName,pcchName,providerName) ) 

#define ICorProfilerInfo12_EventPipeDefineEvent(This,provider,eventName,eventID,keywords,eventVersion,level,opcode,needStack,cParamDescs,pParamDescs,pEvent)    \
    ( (This)->lpVtbl -> EventPipeDefineEvent(This,provider,eventName,eventID,keywords,eventVersion,level,opcode,needStack,cParamDescs,pParamDescs,pEvent) ) 

#define ICorProfilerInfo12_EventPipeWriteEvent(This,event,cData,data,pActivityId,pRelatedActivityId)    \
    ( (This)->lpVtbl -> EventPipeWriteEvent(This,event,cData,data,pActivityId,pRelatedActivityId) ) 

#endif /* COBJMACROS */


#endif  /* C style interface */




#endif  /* __ICorProfilerInfo12_INTERFACE_DEFINED__ */


#ifndef __ICorProfilerMethodEnum_INTERFACE_DEFINED__
#define __ICorProfilerMethodEnum_INTERFACE_DEFINED__

/* interface ICorProfilerMethodEnum */
/* [local][unique][uuid][object] */ 


EXTERN_C const IID IID_ICorProfilerMethodEnum;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("FCCEE788-0088-454B-A811-C99F298D1942")
    ICorProfilerMethodEnum : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE Skip( 
            /* [in] */ ULONG celt) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE Reset( void) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE Clone( 
            /* [out] */ ICorProfilerMethodEnum **ppEnum) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetCount( 
            /* [out] */ ULONG *pcelt) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE Next( 
            /* [in] */ ULONG celt,
            /* [length_is][size_is][out] */ COR_PRF_METHOD elements[  ],
            /* [out] */ ULONG *pceltFetched) = 0;
        
    };
    
    
#else   /* C style interface */

    typedef struct ICorProfilerMethodEnumVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            ICorProfilerMethodEnum * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            ICorProfilerMethodEnum * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            ICorProfilerMethodEnum * This);
        
        HRESULT ( STDMETHODCALLTYPE *Skip )( 
            ICorProfilerMethodEnum * This,
            /* [in] */ ULONG celt);
        
        HRESULT ( STDMETHODCALLTYPE *Reset )( 
            ICorProfilerMethodEnum * This);
        
        HRESULT ( STDMETHODCALLTYPE *Clone )( 
            ICorProfilerMethodEnum * This,
            /* [out] */ ICorProfilerMethodEnum **ppEnum);
        
        HRESULT ( STDMETHODCALLTYPE *GetCount )( 
            ICorProfilerMethodEnum * This,
            /* [out] */ ULONG *pcelt);
        
        HRESULT ( STDMETHODCALLTYPE *Next )( 
            ICorProfilerMethodEnum * This,
            /* [in] */ ULONG celt,
            /* [length_is][size_is][out] */ COR_PRF_METHOD elements[  ],
            /* [out] */ ULONG *pceltFetched);
        
        END_INTERFACE
    } ICorProfilerMethodEnumVtbl;

    interface ICorProfilerMethodEnum
    {
        CONST_VTBL struct ICorProfilerMethodEnumVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define ICorProfilerMethodEnum_QueryInterface(This,riid,ppvObject)  \
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define ICorProfilerMethodEnum_AddRef(This) \
    ( (This)->lpVtbl -> AddRef(This) ) 

#define ICorProfilerMethodEnum_Release(This)    \
    ( (This)->lpVtbl -> Release(This) ) 


#define ICorProfilerMethodEnum_Skip(This,celt)  \
    ( (This)->lpVtbl -> Skip(This,celt) ) 

#define ICorProfilerMethodEnum_Reset(This)  \
    ( (This)->lpVtbl -> Reset(This) ) 

#define ICorProfilerMethodEnum_Clone(This,ppEnum)   \
    ( (This)->lpVtbl -> Clone(This,ppEnum) ) 

#define ICorProfilerMethodEnum_GetCount(This,pcelt) \
    ( (This)->lpVtbl -> GetCount(This,pcelt) ) 

#define ICorProfilerMethodEnum_Next(This,celt,elements,pceltFetched)    \
    ( (This)->lpVtbl -> Next(This,celt,elements,pceltFetched) ) 

#endif /* COBJMACROS */


#endif  /* C style interface */




#endif  /* __ICorProfilerMethodEnum_INTERFACE_DEFINED__ */


#ifndef __ICorProfilerThreadEnum_INTERFACE_DEFINED__
#define __ICorProfilerThreadEnum_INTERFACE_DEFINED__

/* interface ICorProfilerThreadEnum */
/* [local][unique][uuid][object] */ 


EXTERN_C const IID IID_ICorProfilerThreadEnum;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("571194f7-25ed-419f-aa8b-7016b3159701")
    ICorProfilerThreadEnum : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE Skip( 
            /* [in] */ ULONG celt) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE Reset( void) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE Clone( 
            /* [out] */ ICorProfilerThreadEnum **ppEnum) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetCount( 
            /* [out] */ ULONG *pcelt) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE Next( 
            /* [in] */ ULONG celt,
            /* [length_is][size_is][out] */ ThreadID ids[  ],
            /* [out] */ ULONG *pceltFetched) = 0;
        
    };
    
    
#else   /* C style interface */

    typedef struct ICorProfilerThreadEnumVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            ICorProfilerThreadEnum * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            ICorProfilerThreadEnum * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            ICorProfilerThreadEnum * This);
        
        HRESULT ( STDMETHODCALLTYPE *Skip )( 
            ICorProfilerThreadEnum * This,
            /* [in] */ ULONG celt);
        
        HRESULT ( STDMETHODCALLTYPE *Reset )( 
            ICorProfilerThreadEnum * This);
        
        HRESULT ( STDMETHODCALLTYPE *Clone )( 
            ICorProfilerThreadEnum * This,
            /* [out] */ ICorProfilerThreadEnum **ppEnum);
        
        HRESULT ( STDMETHODCALLTYPE *GetCount )( 
            ICorProfilerThreadEnum * This,
            /* [out] */ ULONG *pcelt);
        
        HRESULT ( STDMETHODCALLTYPE *Next )( 
            ICorProfilerThreadEnum * This,
            /* [in] */ ULONG celt,
            /* [length_is][size_is][out] */ ThreadID ids[  ],
            /* [out] */ ULONG *pceltFetched);
        
        END_INTERFACE
    } ICorProfilerThreadEnumVtbl;

    interface ICorProfilerThreadEnum
    {
        CONST_VTBL struct ICorProfilerThreadEnumVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define ICorProfilerThreadEnum_QueryInterface(This,riid,ppvObject)  \
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define ICorProfilerThreadEnum_AddRef(This) \
    ( (This)->lpVtbl -> AddRef(This) ) 

#define ICorProfilerThreadEnum_Release(This)    \
    ( (This)->lpVtbl -> Release(This) ) 


#define ICorProfilerThreadEnum_Skip(This,celt)  \
    ( (This)->lpVtbl -> Skip(This,celt) ) 

#define ICorProfilerThreadEnum_Reset(This)  \
    ( (This)->lpVtbl -> Reset(This) ) 

#define ICorProfilerThreadEnum_Clone(This,ppEnum)   \
    ( (This)->lpVtbl -> Clone(This,ppEnum) ) 

#define ICorProfilerThreadEnum_GetCount(This,pcelt) \
    ( (This)->lpVtbl -> GetCount(This,pcelt) ) 

#define ICorProfilerThreadEnum_Next(This,celt,ids,pceltFetched) \
    ( (This)->lpVtbl -> Next(This,celt,ids,pceltFetched) ) 

#endif /* COBJMACROS */


#endif  /* C style interface */




#endif  /* __ICorProfilerThreadEnum_INTERFACE_DEFINED__ */


#ifndef __ICorProfilerAssemblyReferenceProvider_INTERFACE_DEFINED__
#define __ICorProfilerAssemblyReferenceProvider_INTERFACE_DEFINED__

/* interface ICorProfilerAssemblyReferenceProvider */
/* [local][unique][uuid][object] */ 


EXTERN_C const IID IID_ICorProfilerAssemblyReferenceProvider;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("66A78C24-2EEF-4F65-B45F-DD1D8038BF3C")
    ICorProfilerAssemblyReferenceProvider : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE AddAssemblyReference( 
            const COR_PRF_ASSEMBLY_REFERENCE_INFO *pAssemblyRefInfo) = 0;
        
    };
    
    
#else   /* C style interface */

    typedef struct ICorProfilerAssemblyReferenceProviderVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            ICorProfilerAssemblyReferenceProvider * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            ICorProfilerAssemblyReferenceProvider * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            ICorProfilerAssemblyReferenceProvider * This);
        
        HRESULT ( STDMETHODCALLTYPE *AddAssemblyReference )( 
            ICorProfilerAssemblyReferenceProvider * This,
            const COR_PRF_ASSEMBLY_REFERENCE_INFO *pAssemblyRefInfo);
        
        END_INTERFACE
    } ICorProfilerAssemblyReferenceProviderVtbl;

    interface ICorProfilerAssemblyReferenceProvider
    {
        CONST_VTBL struct ICorProfilerAssemblyReferenceProviderVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define ICorProfilerAssemblyReferenceProvider_QueryInterface(This,riid,ppvObject)   \
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define ICorProfilerAssemblyReferenceProvider_AddRef(This)  \
    ( (This)->lpVtbl -> AddRef(This) ) 

#define ICorProfilerAssemblyReferenceProvider_Release(This) \
    ( (This)->lpVtbl -> Release(This) ) 


#define ICorProfilerAssemblyReferenceProvider_AddAssemblyReference(This,pAssemblyRefInfo)   \
    ( (This)->lpVtbl -> AddAssemblyReference(This,pAssemblyRefInfo) ) 

#endif /* COBJMACROS */


#endif  /* C style interface */




#endif  /* __ICorProfilerAssemblyReferenceProvider_INTERFACE_DEFINED__ */


/* Additional Prototypes for ALL interfaces */

/* end of Additional Prototypes */

#ifdef __cplusplus
}
#endif

#endif


