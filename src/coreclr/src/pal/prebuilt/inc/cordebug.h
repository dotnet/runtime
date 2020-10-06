

/* this ALWAYS GENERATED file contains the definitions for the interfaces */


 /* File created by MIDL compiler version 8.01.0622 */
/* at Mon Jan 18 19:14:07 2038
 */
/* Compiler settings for runtime/src/coreclr/src/inc/cordebug.idl:
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

#ifndef __cordebug_h__
#define __cordebug_h__

#if defined(_MSC_VER) && (_MSC_VER >= 1020)
#pragma once
#endif

/* Forward Declarations */

#ifndef __ICorDebugDataTarget_FWD_DEFINED__
#define __ICorDebugDataTarget_FWD_DEFINED__
typedef interface ICorDebugDataTarget ICorDebugDataTarget;

#endif 	/* __ICorDebugDataTarget_FWD_DEFINED__ */


#ifndef __ICorDebugStaticFieldSymbol_FWD_DEFINED__
#define __ICorDebugStaticFieldSymbol_FWD_DEFINED__
typedef interface ICorDebugStaticFieldSymbol ICorDebugStaticFieldSymbol;

#endif 	/* __ICorDebugStaticFieldSymbol_FWD_DEFINED__ */


#ifndef __ICorDebugInstanceFieldSymbol_FWD_DEFINED__
#define __ICorDebugInstanceFieldSymbol_FWD_DEFINED__
typedef interface ICorDebugInstanceFieldSymbol ICorDebugInstanceFieldSymbol;

#endif 	/* __ICorDebugInstanceFieldSymbol_FWD_DEFINED__ */


#ifndef __ICorDebugVariableSymbol_FWD_DEFINED__
#define __ICorDebugVariableSymbol_FWD_DEFINED__
typedef interface ICorDebugVariableSymbol ICorDebugVariableSymbol;

#endif 	/* __ICorDebugVariableSymbol_FWD_DEFINED__ */


#ifndef __ICorDebugMemoryBuffer_FWD_DEFINED__
#define __ICorDebugMemoryBuffer_FWD_DEFINED__
typedef interface ICorDebugMemoryBuffer ICorDebugMemoryBuffer;

#endif 	/* __ICorDebugMemoryBuffer_FWD_DEFINED__ */


#ifndef __ICorDebugMergedAssemblyRecord_FWD_DEFINED__
#define __ICorDebugMergedAssemblyRecord_FWD_DEFINED__
typedef interface ICorDebugMergedAssemblyRecord ICorDebugMergedAssemblyRecord;

#endif 	/* __ICorDebugMergedAssemblyRecord_FWD_DEFINED__ */


#ifndef __ICorDebugSymbolProvider_FWD_DEFINED__
#define __ICorDebugSymbolProvider_FWD_DEFINED__
typedef interface ICorDebugSymbolProvider ICorDebugSymbolProvider;

#endif 	/* __ICorDebugSymbolProvider_FWD_DEFINED__ */


#ifndef __ICorDebugSymbolProvider2_FWD_DEFINED__
#define __ICorDebugSymbolProvider2_FWD_DEFINED__
typedef interface ICorDebugSymbolProvider2 ICorDebugSymbolProvider2;

#endif 	/* __ICorDebugSymbolProvider2_FWD_DEFINED__ */


#ifndef __ICorDebugVirtualUnwinder_FWD_DEFINED__
#define __ICorDebugVirtualUnwinder_FWD_DEFINED__
typedef interface ICorDebugVirtualUnwinder ICorDebugVirtualUnwinder;

#endif 	/* __ICorDebugVirtualUnwinder_FWD_DEFINED__ */


#ifndef __ICorDebugDataTarget2_FWD_DEFINED__
#define __ICorDebugDataTarget2_FWD_DEFINED__
typedef interface ICorDebugDataTarget2 ICorDebugDataTarget2;

#endif 	/* __ICorDebugDataTarget2_FWD_DEFINED__ */


#ifndef __ICorDebugLoadedModule_FWD_DEFINED__
#define __ICorDebugLoadedModule_FWD_DEFINED__
typedef interface ICorDebugLoadedModule ICorDebugLoadedModule;

#endif 	/* __ICorDebugLoadedModule_FWD_DEFINED__ */


#ifndef __ICorDebugDataTarget3_FWD_DEFINED__
#define __ICorDebugDataTarget3_FWD_DEFINED__
typedef interface ICorDebugDataTarget3 ICorDebugDataTarget3;

#endif 	/* __ICorDebugDataTarget3_FWD_DEFINED__ */


#ifndef __ICorDebugDataTarget4_FWD_DEFINED__
#define __ICorDebugDataTarget4_FWD_DEFINED__
typedef interface ICorDebugDataTarget4 ICorDebugDataTarget4;

#endif 	/* __ICorDebugDataTarget4_FWD_DEFINED__ */


#ifndef __ICorDebugMutableDataTarget_FWD_DEFINED__
#define __ICorDebugMutableDataTarget_FWD_DEFINED__
typedef interface ICorDebugMutableDataTarget ICorDebugMutableDataTarget;

#endif 	/* __ICorDebugMutableDataTarget_FWD_DEFINED__ */


#ifndef __ICorDebugMetaDataLocator_FWD_DEFINED__
#define __ICorDebugMetaDataLocator_FWD_DEFINED__
typedef interface ICorDebugMetaDataLocator ICorDebugMetaDataLocator;

#endif 	/* __ICorDebugMetaDataLocator_FWD_DEFINED__ */


#ifndef __ICorDebugManagedCallback_FWD_DEFINED__
#define __ICorDebugManagedCallback_FWD_DEFINED__
typedef interface ICorDebugManagedCallback ICorDebugManagedCallback;

#endif 	/* __ICorDebugManagedCallback_FWD_DEFINED__ */


#ifndef __ICorDebugManagedCallback3_FWD_DEFINED__
#define __ICorDebugManagedCallback3_FWD_DEFINED__
typedef interface ICorDebugManagedCallback3 ICorDebugManagedCallback3;

#endif 	/* __ICorDebugManagedCallback3_FWD_DEFINED__ */


#ifndef __ICorDebugManagedCallback4_FWD_DEFINED__
#define __ICorDebugManagedCallback4_FWD_DEFINED__
typedef interface ICorDebugManagedCallback4 ICorDebugManagedCallback4;

#endif 	/* __ICorDebugManagedCallback4_FWD_DEFINED__ */


#ifndef __ICorDebugManagedCallback2_FWD_DEFINED__
#define __ICorDebugManagedCallback2_FWD_DEFINED__
typedef interface ICorDebugManagedCallback2 ICorDebugManagedCallback2;

#endif 	/* __ICorDebugManagedCallback2_FWD_DEFINED__ */


#ifndef __ICorDebugUnmanagedCallback_FWD_DEFINED__
#define __ICorDebugUnmanagedCallback_FWD_DEFINED__
typedef interface ICorDebugUnmanagedCallback ICorDebugUnmanagedCallback;

#endif 	/* __ICorDebugUnmanagedCallback_FWD_DEFINED__ */


#ifndef __ICorDebug_FWD_DEFINED__
#define __ICorDebug_FWD_DEFINED__
typedef interface ICorDebug ICorDebug;

#endif 	/* __ICorDebug_FWD_DEFINED__ */


#ifndef __ICorDebugRemoteTarget_FWD_DEFINED__
#define __ICorDebugRemoteTarget_FWD_DEFINED__
typedef interface ICorDebugRemoteTarget ICorDebugRemoteTarget;

#endif 	/* __ICorDebugRemoteTarget_FWD_DEFINED__ */


#ifndef __ICorDebugRemote_FWD_DEFINED__
#define __ICorDebugRemote_FWD_DEFINED__
typedef interface ICorDebugRemote ICorDebugRemote;

#endif 	/* __ICorDebugRemote_FWD_DEFINED__ */


#ifndef __ICorDebug2_FWD_DEFINED__
#define __ICorDebug2_FWD_DEFINED__
typedef interface ICorDebug2 ICorDebug2;

#endif 	/* __ICorDebug2_FWD_DEFINED__ */


#ifndef __ICorDebugController_FWD_DEFINED__
#define __ICorDebugController_FWD_DEFINED__
typedef interface ICorDebugController ICorDebugController;

#endif 	/* __ICorDebugController_FWD_DEFINED__ */


#ifndef __ICorDebugAppDomain_FWD_DEFINED__
#define __ICorDebugAppDomain_FWD_DEFINED__
typedef interface ICorDebugAppDomain ICorDebugAppDomain;

#endif 	/* __ICorDebugAppDomain_FWD_DEFINED__ */


#ifndef __ICorDebugAppDomain2_FWD_DEFINED__
#define __ICorDebugAppDomain2_FWD_DEFINED__
typedef interface ICorDebugAppDomain2 ICorDebugAppDomain2;

#endif 	/* __ICorDebugAppDomain2_FWD_DEFINED__ */


#ifndef __ICorDebugEnum_FWD_DEFINED__
#define __ICorDebugEnum_FWD_DEFINED__
typedef interface ICorDebugEnum ICorDebugEnum;

#endif 	/* __ICorDebugEnum_FWD_DEFINED__ */


#ifndef __ICorDebugGuidToTypeEnum_FWD_DEFINED__
#define __ICorDebugGuidToTypeEnum_FWD_DEFINED__
typedef interface ICorDebugGuidToTypeEnum ICorDebugGuidToTypeEnum;

#endif 	/* __ICorDebugGuidToTypeEnum_FWD_DEFINED__ */


#ifndef __ICorDebugAppDomain3_FWD_DEFINED__
#define __ICorDebugAppDomain3_FWD_DEFINED__
typedef interface ICorDebugAppDomain3 ICorDebugAppDomain3;

#endif 	/* __ICorDebugAppDomain3_FWD_DEFINED__ */


#ifndef __ICorDebugAppDomain4_FWD_DEFINED__
#define __ICorDebugAppDomain4_FWD_DEFINED__
typedef interface ICorDebugAppDomain4 ICorDebugAppDomain4;

#endif 	/* __ICorDebugAppDomain4_FWD_DEFINED__ */


#ifndef __ICorDebugAssembly_FWD_DEFINED__
#define __ICorDebugAssembly_FWD_DEFINED__
typedef interface ICorDebugAssembly ICorDebugAssembly;

#endif 	/* __ICorDebugAssembly_FWD_DEFINED__ */


#ifndef __ICorDebugAssembly2_FWD_DEFINED__
#define __ICorDebugAssembly2_FWD_DEFINED__
typedef interface ICorDebugAssembly2 ICorDebugAssembly2;

#endif 	/* __ICorDebugAssembly2_FWD_DEFINED__ */


#ifndef __ICorDebugAssembly3_FWD_DEFINED__
#define __ICorDebugAssembly3_FWD_DEFINED__
typedef interface ICorDebugAssembly3 ICorDebugAssembly3;

#endif 	/* __ICorDebugAssembly3_FWD_DEFINED__ */


#ifndef __ICorDebugHeapEnum_FWD_DEFINED__
#define __ICorDebugHeapEnum_FWD_DEFINED__
typedef interface ICorDebugHeapEnum ICorDebugHeapEnum;

#endif 	/* __ICorDebugHeapEnum_FWD_DEFINED__ */


#ifndef __ICorDebugHeapSegmentEnum_FWD_DEFINED__
#define __ICorDebugHeapSegmentEnum_FWD_DEFINED__
typedef interface ICorDebugHeapSegmentEnum ICorDebugHeapSegmentEnum;

#endif 	/* __ICorDebugHeapSegmentEnum_FWD_DEFINED__ */


#ifndef __ICorDebugGCReferenceEnum_FWD_DEFINED__
#define __ICorDebugGCReferenceEnum_FWD_DEFINED__
typedef interface ICorDebugGCReferenceEnum ICorDebugGCReferenceEnum;

#endif 	/* __ICorDebugGCReferenceEnum_FWD_DEFINED__ */


#ifndef __ICorDebugProcess_FWD_DEFINED__
#define __ICorDebugProcess_FWD_DEFINED__
typedef interface ICorDebugProcess ICorDebugProcess;

#endif 	/* __ICorDebugProcess_FWD_DEFINED__ */


#ifndef __ICorDebugProcess2_FWD_DEFINED__
#define __ICorDebugProcess2_FWD_DEFINED__
typedef interface ICorDebugProcess2 ICorDebugProcess2;

#endif 	/* __ICorDebugProcess2_FWD_DEFINED__ */


#ifndef __ICorDebugProcess3_FWD_DEFINED__
#define __ICorDebugProcess3_FWD_DEFINED__
typedef interface ICorDebugProcess3 ICorDebugProcess3;

#endif 	/* __ICorDebugProcess3_FWD_DEFINED__ */


#ifndef __ICorDebugProcess5_FWD_DEFINED__
#define __ICorDebugProcess5_FWD_DEFINED__
typedef interface ICorDebugProcess5 ICorDebugProcess5;

#endif 	/* __ICorDebugProcess5_FWD_DEFINED__ */


#ifndef __ICorDebugDebugEvent_FWD_DEFINED__
#define __ICorDebugDebugEvent_FWD_DEFINED__
typedef interface ICorDebugDebugEvent ICorDebugDebugEvent;

#endif 	/* __ICorDebugDebugEvent_FWD_DEFINED__ */


#ifndef __ICorDebugProcess6_FWD_DEFINED__
#define __ICorDebugProcess6_FWD_DEFINED__
typedef interface ICorDebugProcess6 ICorDebugProcess6;

#endif 	/* __ICorDebugProcess6_FWD_DEFINED__ */


#ifndef __ICorDebugProcess7_FWD_DEFINED__
#define __ICorDebugProcess7_FWD_DEFINED__
typedef interface ICorDebugProcess7 ICorDebugProcess7;

#endif 	/* __ICorDebugProcess7_FWD_DEFINED__ */


#ifndef __ICorDebugProcess8_FWD_DEFINED__
#define __ICorDebugProcess8_FWD_DEFINED__
typedef interface ICorDebugProcess8 ICorDebugProcess8;

#endif 	/* __ICorDebugProcess8_FWD_DEFINED__ */


#ifndef __ICorDebugProcess10_FWD_DEFINED__
#define __ICorDebugProcess10_FWD_DEFINED__
typedef interface ICorDebugProcess10 ICorDebugProcess10;

#endif 	/* __ICorDebugProcess10_FWD_DEFINED__ */


#ifndef __ICorDebugMemoryRangeEnum_FWD_DEFINED__
#define __ICorDebugMemoryRangeEnum_FWD_DEFINED__
typedef interface ICorDebugMemoryRangeEnum ICorDebugMemoryRangeEnum;

#endif 	/* __ICorDebugMemoryRangeEnum_FWD_DEFINED__ */


#ifndef __ICorDebugProcess11_FWD_DEFINED__
#define __ICorDebugProcess11_FWD_DEFINED__
typedef interface ICorDebugProcess11 ICorDebugProcess11;

#endif 	/* __ICorDebugProcess11_FWD_DEFINED__ */


#ifndef __ICorDebugModuleDebugEvent_FWD_DEFINED__
#define __ICorDebugModuleDebugEvent_FWD_DEFINED__
typedef interface ICorDebugModuleDebugEvent ICorDebugModuleDebugEvent;

#endif 	/* __ICorDebugModuleDebugEvent_FWD_DEFINED__ */


#ifndef __ICorDebugExceptionDebugEvent_FWD_DEFINED__
#define __ICorDebugExceptionDebugEvent_FWD_DEFINED__
typedef interface ICorDebugExceptionDebugEvent ICorDebugExceptionDebugEvent;

#endif 	/* __ICorDebugExceptionDebugEvent_FWD_DEFINED__ */


#ifndef __ICorDebugBreakpoint_FWD_DEFINED__
#define __ICorDebugBreakpoint_FWD_DEFINED__
typedef interface ICorDebugBreakpoint ICorDebugBreakpoint;

#endif 	/* __ICorDebugBreakpoint_FWD_DEFINED__ */


#ifndef __ICorDebugFunctionBreakpoint_FWD_DEFINED__
#define __ICorDebugFunctionBreakpoint_FWD_DEFINED__
typedef interface ICorDebugFunctionBreakpoint ICorDebugFunctionBreakpoint;

#endif 	/* __ICorDebugFunctionBreakpoint_FWD_DEFINED__ */


#ifndef __ICorDebugModuleBreakpoint_FWD_DEFINED__
#define __ICorDebugModuleBreakpoint_FWD_DEFINED__
typedef interface ICorDebugModuleBreakpoint ICorDebugModuleBreakpoint;

#endif 	/* __ICorDebugModuleBreakpoint_FWD_DEFINED__ */


#ifndef __ICorDebugValueBreakpoint_FWD_DEFINED__
#define __ICorDebugValueBreakpoint_FWD_DEFINED__
typedef interface ICorDebugValueBreakpoint ICorDebugValueBreakpoint;

#endif 	/* __ICorDebugValueBreakpoint_FWD_DEFINED__ */


#ifndef __ICorDebugStepper_FWD_DEFINED__
#define __ICorDebugStepper_FWD_DEFINED__
typedef interface ICorDebugStepper ICorDebugStepper;

#endif 	/* __ICorDebugStepper_FWD_DEFINED__ */


#ifndef __ICorDebugStepper2_FWD_DEFINED__
#define __ICorDebugStepper2_FWD_DEFINED__
typedef interface ICorDebugStepper2 ICorDebugStepper2;

#endif 	/* __ICorDebugStepper2_FWD_DEFINED__ */


#ifndef __ICorDebugRegisterSet_FWD_DEFINED__
#define __ICorDebugRegisterSet_FWD_DEFINED__
typedef interface ICorDebugRegisterSet ICorDebugRegisterSet;

#endif 	/* __ICorDebugRegisterSet_FWD_DEFINED__ */


#ifndef __ICorDebugRegisterSet2_FWD_DEFINED__
#define __ICorDebugRegisterSet2_FWD_DEFINED__
typedef interface ICorDebugRegisterSet2 ICorDebugRegisterSet2;

#endif 	/* __ICorDebugRegisterSet2_FWD_DEFINED__ */


#ifndef __ICorDebugThread_FWD_DEFINED__
#define __ICorDebugThread_FWD_DEFINED__
typedef interface ICorDebugThread ICorDebugThread;

#endif 	/* __ICorDebugThread_FWD_DEFINED__ */


#ifndef __ICorDebugThread2_FWD_DEFINED__
#define __ICorDebugThread2_FWD_DEFINED__
typedef interface ICorDebugThread2 ICorDebugThread2;

#endif 	/* __ICorDebugThread2_FWD_DEFINED__ */


#ifndef __ICorDebugThread3_FWD_DEFINED__
#define __ICorDebugThread3_FWD_DEFINED__
typedef interface ICorDebugThread3 ICorDebugThread3;

#endif 	/* __ICorDebugThread3_FWD_DEFINED__ */


#ifndef __ICorDebugThread4_FWD_DEFINED__
#define __ICorDebugThread4_FWD_DEFINED__
typedef interface ICorDebugThread4 ICorDebugThread4;

#endif 	/* __ICorDebugThread4_FWD_DEFINED__ */


#ifndef __ICorDebugStackWalk_FWD_DEFINED__
#define __ICorDebugStackWalk_FWD_DEFINED__
typedef interface ICorDebugStackWalk ICorDebugStackWalk;

#endif 	/* __ICorDebugStackWalk_FWD_DEFINED__ */


#ifndef __ICorDebugChain_FWD_DEFINED__
#define __ICorDebugChain_FWD_DEFINED__
typedef interface ICorDebugChain ICorDebugChain;

#endif 	/* __ICorDebugChain_FWD_DEFINED__ */


#ifndef __ICorDebugFrame_FWD_DEFINED__
#define __ICorDebugFrame_FWD_DEFINED__
typedef interface ICorDebugFrame ICorDebugFrame;

#endif 	/* __ICorDebugFrame_FWD_DEFINED__ */


#ifndef __ICorDebugInternalFrame_FWD_DEFINED__
#define __ICorDebugInternalFrame_FWD_DEFINED__
typedef interface ICorDebugInternalFrame ICorDebugInternalFrame;

#endif 	/* __ICorDebugInternalFrame_FWD_DEFINED__ */


#ifndef __ICorDebugInternalFrame2_FWD_DEFINED__
#define __ICorDebugInternalFrame2_FWD_DEFINED__
typedef interface ICorDebugInternalFrame2 ICorDebugInternalFrame2;

#endif 	/* __ICorDebugInternalFrame2_FWD_DEFINED__ */


#ifndef __ICorDebugILFrame_FWD_DEFINED__
#define __ICorDebugILFrame_FWD_DEFINED__
typedef interface ICorDebugILFrame ICorDebugILFrame;

#endif 	/* __ICorDebugILFrame_FWD_DEFINED__ */


#ifndef __ICorDebugILFrame2_FWD_DEFINED__
#define __ICorDebugILFrame2_FWD_DEFINED__
typedef interface ICorDebugILFrame2 ICorDebugILFrame2;

#endif 	/* __ICorDebugILFrame2_FWD_DEFINED__ */


#ifndef __ICorDebugILFrame3_FWD_DEFINED__
#define __ICorDebugILFrame3_FWD_DEFINED__
typedef interface ICorDebugILFrame3 ICorDebugILFrame3;

#endif 	/* __ICorDebugILFrame3_FWD_DEFINED__ */


#ifndef __ICorDebugILFrame4_FWD_DEFINED__
#define __ICorDebugILFrame4_FWD_DEFINED__
typedef interface ICorDebugILFrame4 ICorDebugILFrame4;

#endif 	/* __ICorDebugILFrame4_FWD_DEFINED__ */


#ifndef __ICorDebugNativeFrame_FWD_DEFINED__
#define __ICorDebugNativeFrame_FWD_DEFINED__
typedef interface ICorDebugNativeFrame ICorDebugNativeFrame;

#endif 	/* __ICorDebugNativeFrame_FWD_DEFINED__ */


#ifndef __ICorDebugNativeFrame2_FWD_DEFINED__
#define __ICorDebugNativeFrame2_FWD_DEFINED__
typedef interface ICorDebugNativeFrame2 ICorDebugNativeFrame2;

#endif 	/* __ICorDebugNativeFrame2_FWD_DEFINED__ */


#ifndef __ICorDebugModule3_FWD_DEFINED__
#define __ICorDebugModule3_FWD_DEFINED__
typedef interface ICorDebugModule3 ICorDebugModule3;

#endif 	/* __ICorDebugModule3_FWD_DEFINED__ */


#ifndef __ICorDebugModule4_FWD_DEFINED__
#define __ICorDebugModule4_FWD_DEFINED__
typedef interface ICorDebugModule4 ICorDebugModule4;

#endif 	/* __ICorDebugModule4_FWD_DEFINED__ */


#ifndef __ICorDebugRuntimeUnwindableFrame_FWD_DEFINED__
#define __ICorDebugRuntimeUnwindableFrame_FWD_DEFINED__
typedef interface ICorDebugRuntimeUnwindableFrame ICorDebugRuntimeUnwindableFrame;

#endif 	/* __ICorDebugRuntimeUnwindableFrame_FWD_DEFINED__ */


#ifndef __ICorDebugModule_FWD_DEFINED__
#define __ICorDebugModule_FWD_DEFINED__
typedef interface ICorDebugModule ICorDebugModule;

#endif 	/* __ICorDebugModule_FWD_DEFINED__ */


#ifndef __ICorDebugModule2_FWD_DEFINED__
#define __ICorDebugModule2_FWD_DEFINED__
typedef interface ICorDebugModule2 ICorDebugModule2;

#endif 	/* __ICorDebugModule2_FWD_DEFINED__ */


#ifndef __ICorDebugFunction_FWD_DEFINED__
#define __ICorDebugFunction_FWD_DEFINED__
typedef interface ICorDebugFunction ICorDebugFunction;

#endif 	/* __ICorDebugFunction_FWD_DEFINED__ */


#ifndef __ICorDebugFunction2_FWD_DEFINED__
#define __ICorDebugFunction2_FWD_DEFINED__
typedef interface ICorDebugFunction2 ICorDebugFunction2;

#endif 	/* __ICorDebugFunction2_FWD_DEFINED__ */


#ifndef __ICorDebugFunction3_FWD_DEFINED__
#define __ICorDebugFunction3_FWD_DEFINED__
typedef interface ICorDebugFunction3 ICorDebugFunction3;

#endif 	/* __ICorDebugFunction3_FWD_DEFINED__ */


#ifndef __ICorDebugFunction4_FWD_DEFINED__
#define __ICorDebugFunction4_FWD_DEFINED__
typedef interface ICorDebugFunction4 ICorDebugFunction4;

#endif 	/* __ICorDebugFunction4_FWD_DEFINED__ */


#ifndef __ICorDebugCode_FWD_DEFINED__
#define __ICorDebugCode_FWD_DEFINED__
typedef interface ICorDebugCode ICorDebugCode;

#endif 	/* __ICorDebugCode_FWD_DEFINED__ */


#ifndef __ICorDebugCode2_FWD_DEFINED__
#define __ICorDebugCode2_FWD_DEFINED__
typedef interface ICorDebugCode2 ICorDebugCode2;

#endif 	/* __ICorDebugCode2_FWD_DEFINED__ */


#ifndef __ICorDebugCode3_FWD_DEFINED__
#define __ICorDebugCode3_FWD_DEFINED__
typedef interface ICorDebugCode3 ICorDebugCode3;

#endif 	/* __ICorDebugCode3_FWD_DEFINED__ */


#ifndef __ICorDebugCode4_FWD_DEFINED__
#define __ICorDebugCode4_FWD_DEFINED__
typedef interface ICorDebugCode4 ICorDebugCode4;

#endif 	/* __ICorDebugCode4_FWD_DEFINED__ */


#ifndef __ICorDebugILCode_FWD_DEFINED__
#define __ICorDebugILCode_FWD_DEFINED__
typedef interface ICorDebugILCode ICorDebugILCode;

#endif 	/* __ICorDebugILCode_FWD_DEFINED__ */


#ifndef __ICorDebugILCode2_FWD_DEFINED__
#define __ICorDebugILCode2_FWD_DEFINED__
typedef interface ICorDebugILCode2 ICorDebugILCode2;

#endif 	/* __ICorDebugILCode2_FWD_DEFINED__ */


#ifndef __ICorDebugClass_FWD_DEFINED__
#define __ICorDebugClass_FWD_DEFINED__
typedef interface ICorDebugClass ICorDebugClass;

#endif 	/* __ICorDebugClass_FWD_DEFINED__ */


#ifndef __ICorDebugClass2_FWD_DEFINED__
#define __ICorDebugClass2_FWD_DEFINED__
typedef interface ICorDebugClass2 ICorDebugClass2;

#endif 	/* __ICorDebugClass2_FWD_DEFINED__ */


#ifndef __ICorDebugEval_FWD_DEFINED__
#define __ICorDebugEval_FWD_DEFINED__
typedef interface ICorDebugEval ICorDebugEval;

#endif 	/* __ICorDebugEval_FWD_DEFINED__ */


#ifndef __ICorDebugEval2_FWD_DEFINED__
#define __ICorDebugEval2_FWD_DEFINED__
typedef interface ICorDebugEval2 ICorDebugEval2;

#endif 	/* __ICorDebugEval2_FWD_DEFINED__ */


#ifndef __ICorDebugValue_FWD_DEFINED__
#define __ICorDebugValue_FWD_DEFINED__
typedef interface ICorDebugValue ICorDebugValue;

#endif 	/* __ICorDebugValue_FWD_DEFINED__ */


#ifndef __ICorDebugValue2_FWD_DEFINED__
#define __ICorDebugValue2_FWD_DEFINED__
typedef interface ICorDebugValue2 ICorDebugValue2;

#endif 	/* __ICorDebugValue2_FWD_DEFINED__ */


#ifndef __ICorDebugValue3_FWD_DEFINED__
#define __ICorDebugValue3_FWD_DEFINED__
typedef interface ICorDebugValue3 ICorDebugValue3;

#endif 	/* __ICorDebugValue3_FWD_DEFINED__ */


#ifndef __ICorDebugGenericValue_FWD_DEFINED__
#define __ICorDebugGenericValue_FWD_DEFINED__
typedef interface ICorDebugGenericValue ICorDebugGenericValue;

#endif 	/* __ICorDebugGenericValue_FWD_DEFINED__ */


#ifndef __ICorDebugReferenceValue_FWD_DEFINED__
#define __ICorDebugReferenceValue_FWD_DEFINED__
typedef interface ICorDebugReferenceValue ICorDebugReferenceValue;

#endif 	/* __ICorDebugReferenceValue_FWD_DEFINED__ */


#ifndef __ICorDebugHeapValue_FWD_DEFINED__
#define __ICorDebugHeapValue_FWD_DEFINED__
typedef interface ICorDebugHeapValue ICorDebugHeapValue;

#endif 	/* __ICorDebugHeapValue_FWD_DEFINED__ */


#ifndef __ICorDebugHeapValue2_FWD_DEFINED__
#define __ICorDebugHeapValue2_FWD_DEFINED__
typedef interface ICorDebugHeapValue2 ICorDebugHeapValue2;

#endif 	/* __ICorDebugHeapValue2_FWD_DEFINED__ */


#ifndef __ICorDebugHeapValue3_FWD_DEFINED__
#define __ICorDebugHeapValue3_FWD_DEFINED__
typedef interface ICorDebugHeapValue3 ICorDebugHeapValue3;

#endif 	/* __ICorDebugHeapValue3_FWD_DEFINED__ */


#ifndef __ICorDebugObjectValue_FWD_DEFINED__
#define __ICorDebugObjectValue_FWD_DEFINED__
typedef interface ICorDebugObjectValue ICorDebugObjectValue;

#endif 	/* __ICorDebugObjectValue_FWD_DEFINED__ */


#ifndef __ICorDebugObjectValue2_FWD_DEFINED__
#define __ICorDebugObjectValue2_FWD_DEFINED__
typedef interface ICorDebugObjectValue2 ICorDebugObjectValue2;

#endif 	/* __ICorDebugObjectValue2_FWD_DEFINED__ */


#ifndef __ICorDebugDelegateObjectValue_FWD_DEFINED__
#define __ICorDebugDelegateObjectValue_FWD_DEFINED__
typedef interface ICorDebugDelegateObjectValue ICorDebugDelegateObjectValue;

#endif 	/* __ICorDebugDelegateObjectValue_FWD_DEFINED__ */


#ifndef __ICorDebugBoxValue_FWD_DEFINED__
#define __ICorDebugBoxValue_FWD_DEFINED__
typedef interface ICorDebugBoxValue ICorDebugBoxValue;

#endif 	/* __ICorDebugBoxValue_FWD_DEFINED__ */


#ifndef __ICorDebugStringValue_FWD_DEFINED__
#define __ICorDebugStringValue_FWD_DEFINED__
typedef interface ICorDebugStringValue ICorDebugStringValue;

#endif 	/* __ICorDebugStringValue_FWD_DEFINED__ */


#ifndef __ICorDebugArrayValue_FWD_DEFINED__
#define __ICorDebugArrayValue_FWD_DEFINED__
typedef interface ICorDebugArrayValue ICorDebugArrayValue;

#endif 	/* __ICorDebugArrayValue_FWD_DEFINED__ */


#ifndef __ICorDebugVariableHome_FWD_DEFINED__
#define __ICorDebugVariableHome_FWD_DEFINED__
typedef interface ICorDebugVariableHome ICorDebugVariableHome;

#endif 	/* __ICorDebugVariableHome_FWD_DEFINED__ */


#ifndef __ICorDebugHandleValue_FWD_DEFINED__
#define __ICorDebugHandleValue_FWD_DEFINED__
typedef interface ICorDebugHandleValue ICorDebugHandleValue;

#endif 	/* __ICorDebugHandleValue_FWD_DEFINED__ */


#ifndef __ICorDebugContext_FWD_DEFINED__
#define __ICorDebugContext_FWD_DEFINED__
typedef interface ICorDebugContext ICorDebugContext;

#endif 	/* __ICorDebugContext_FWD_DEFINED__ */


#ifndef __ICorDebugComObjectValue_FWD_DEFINED__
#define __ICorDebugComObjectValue_FWD_DEFINED__
typedef interface ICorDebugComObjectValue ICorDebugComObjectValue;

#endif 	/* __ICorDebugComObjectValue_FWD_DEFINED__ */


#ifndef __ICorDebugObjectEnum_FWD_DEFINED__
#define __ICorDebugObjectEnum_FWD_DEFINED__
typedef interface ICorDebugObjectEnum ICorDebugObjectEnum;

#endif 	/* __ICorDebugObjectEnum_FWD_DEFINED__ */


#ifndef __ICorDebugBreakpointEnum_FWD_DEFINED__
#define __ICorDebugBreakpointEnum_FWD_DEFINED__
typedef interface ICorDebugBreakpointEnum ICorDebugBreakpointEnum;

#endif 	/* __ICorDebugBreakpointEnum_FWD_DEFINED__ */


#ifndef __ICorDebugStepperEnum_FWD_DEFINED__
#define __ICorDebugStepperEnum_FWD_DEFINED__
typedef interface ICorDebugStepperEnum ICorDebugStepperEnum;

#endif 	/* __ICorDebugStepperEnum_FWD_DEFINED__ */


#ifndef __ICorDebugProcessEnum_FWD_DEFINED__
#define __ICorDebugProcessEnum_FWD_DEFINED__
typedef interface ICorDebugProcessEnum ICorDebugProcessEnum;

#endif 	/* __ICorDebugProcessEnum_FWD_DEFINED__ */


#ifndef __ICorDebugThreadEnum_FWD_DEFINED__
#define __ICorDebugThreadEnum_FWD_DEFINED__
typedef interface ICorDebugThreadEnum ICorDebugThreadEnum;

#endif 	/* __ICorDebugThreadEnum_FWD_DEFINED__ */


#ifndef __ICorDebugFrameEnum_FWD_DEFINED__
#define __ICorDebugFrameEnum_FWD_DEFINED__
typedef interface ICorDebugFrameEnum ICorDebugFrameEnum;

#endif 	/* __ICorDebugFrameEnum_FWD_DEFINED__ */


#ifndef __ICorDebugChainEnum_FWD_DEFINED__
#define __ICorDebugChainEnum_FWD_DEFINED__
typedef interface ICorDebugChainEnum ICorDebugChainEnum;

#endif 	/* __ICorDebugChainEnum_FWD_DEFINED__ */


#ifndef __ICorDebugModuleEnum_FWD_DEFINED__
#define __ICorDebugModuleEnum_FWD_DEFINED__
typedef interface ICorDebugModuleEnum ICorDebugModuleEnum;

#endif 	/* __ICorDebugModuleEnum_FWD_DEFINED__ */


#ifndef __ICorDebugValueEnum_FWD_DEFINED__
#define __ICorDebugValueEnum_FWD_DEFINED__
typedef interface ICorDebugValueEnum ICorDebugValueEnum;

#endif 	/* __ICorDebugValueEnum_FWD_DEFINED__ */


#ifndef __ICorDebugVariableHomeEnum_FWD_DEFINED__
#define __ICorDebugVariableHomeEnum_FWD_DEFINED__
typedef interface ICorDebugVariableHomeEnum ICorDebugVariableHomeEnum;

#endif 	/* __ICorDebugVariableHomeEnum_FWD_DEFINED__ */


#ifndef __ICorDebugCodeEnum_FWD_DEFINED__
#define __ICorDebugCodeEnum_FWD_DEFINED__
typedef interface ICorDebugCodeEnum ICorDebugCodeEnum;

#endif 	/* __ICorDebugCodeEnum_FWD_DEFINED__ */


#ifndef __ICorDebugTypeEnum_FWD_DEFINED__
#define __ICorDebugTypeEnum_FWD_DEFINED__
typedef interface ICorDebugTypeEnum ICorDebugTypeEnum;

#endif 	/* __ICorDebugTypeEnum_FWD_DEFINED__ */


#ifndef __ICorDebugType_FWD_DEFINED__
#define __ICorDebugType_FWD_DEFINED__
typedef interface ICorDebugType ICorDebugType;

#endif 	/* __ICorDebugType_FWD_DEFINED__ */


#ifndef __ICorDebugType2_FWD_DEFINED__
#define __ICorDebugType2_FWD_DEFINED__
typedef interface ICorDebugType2 ICorDebugType2;

#endif 	/* __ICorDebugType2_FWD_DEFINED__ */


#ifndef __ICorDebugErrorInfoEnum_FWD_DEFINED__
#define __ICorDebugErrorInfoEnum_FWD_DEFINED__
typedef interface ICorDebugErrorInfoEnum ICorDebugErrorInfoEnum;

#endif 	/* __ICorDebugErrorInfoEnum_FWD_DEFINED__ */


#ifndef __ICorDebugAppDomainEnum_FWD_DEFINED__
#define __ICorDebugAppDomainEnum_FWD_DEFINED__
typedef interface ICorDebugAppDomainEnum ICorDebugAppDomainEnum;

#endif 	/* __ICorDebugAppDomainEnum_FWD_DEFINED__ */


#ifndef __ICorDebugAssemblyEnum_FWD_DEFINED__
#define __ICorDebugAssemblyEnum_FWD_DEFINED__
typedef interface ICorDebugAssemblyEnum ICorDebugAssemblyEnum;

#endif 	/* __ICorDebugAssemblyEnum_FWD_DEFINED__ */


#ifndef __ICorDebugBlockingObjectEnum_FWD_DEFINED__
#define __ICorDebugBlockingObjectEnum_FWD_DEFINED__
typedef interface ICorDebugBlockingObjectEnum ICorDebugBlockingObjectEnum;

#endif 	/* __ICorDebugBlockingObjectEnum_FWD_DEFINED__ */


#ifndef __ICorDebugMDA_FWD_DEFINED__
#define __ICorDebugMDA_FWD_DEFINED__
typedef interface ICorDebugMDA ICorDebugMDA;

#endif 	/* __ICorDebugMDA_FWD_DEFINED__ */


#ifndef __ICorDebugEditAndContinueErrorInfo_FWD_DEFINED__
#define __ICorDebugEditAndContinueErrorInfo_FWD_DEFINED__
typedef interface ICorDebugEditAndContinueErrorInfo ICorDebugEditAndContinueErrorInfo;

#endif 	/* __ICorDebugEditAndContinueErrorInfo_FWD_DEFINED__ */


#ifndef __ICorDebugEditAndContinueSnapshot_FWD_DEFINED__
#define __ICorDebugEditAndContinueSnapshot_FWD_DEFINED__
typedef interface ICorDebugEditAndContinueSnapshot ICorDebugEditAndContinueSnapshot;

#endif 	/* __ICorDebugEditAndContinueSnapshot_FWD_DEFINED__ */


#ifndef __ICorDebugExceptionObjectCallStackEnum_FWD_DEFINED__
#define __ICorDebugExceptionObjectCallStackEnum_FWD_DEFINED__
typedef interface ICorDebugExceptionObjectCallStackEnum ICorDebugExceptionObjectCallStackEnum;

#endif 	/* __ICorDebugExceptionObjectCallStackEnum_FWD_DEFINED__ */


#ifndef __ICorDebugExceptionObjectValue_FWD_DEFINED__
#define __ICorDebugExceptionObjectValue_FWD_DEFINED__
typedef interface ICorDebugExceptionObjectValue ICorDebugExceptionObjectValue;

#endif 	/* __ICorDebugExceptionObjectValue_FWD_DEFINED__ */


#ifndef __CorDebug_FWD_DEFINED__
#define __CorDebug_FWD_DEFINED__

#ifdef __cplusplus
typedef class CorDebug CorDebug;
#else
typedef struct CorDebug CorDebug;
#endif /* __cplusplus */

#endif 	/* __CorDebug_FWD_DEFINED__ */


#ifndef __EmbeddedCLRCorDebug_FWD_DEFINED__
#define __EmbeddedCLRCorDebug_FWD_DEFINED__

#ifdef __cplusplus
typedef class EmbeddedCLRCorDebug EmbeddedCLRCorDebug;
#else
typedef struct EmbeddedCLRCorDebug EmbeddedCLRCorDebug;
#endif /* __cplusplus */

#endif 	/* __EmbeddedCLRCorDebug_FWD_DEFINED__ */


#ifndef __ICorDebugValue_FWD_DEFINED__
#define __ICorDebugValue_FWD_DEFINED__
typedef interface ICorDebugValue ICorDebugValue;

#endif 	/* __ICorDebugValue_FWD_DEFINED__ */


#ifndef __ICorDebugReferenceValue_FWD_DEFINED__
#define __ICorDebugReferenceValue_FWD_DEFINED__
typedef interface ICorDebugReferenceValue ICorDebugReferenceValue;

#endif 	/* __ICorDebugReferenceValue_FWD_DEFINED__ */


#ifndef __ICorDebugHeapValue_FWD_DEFINED__
#define __ICorDebugHeapValue_FWD_DEFINED__
typedef interface ICorDebugHeapValue ICorDebugHeapValue;

#endif 	/* __ICorDebugHeapValue_FWD_DEFINED__ */


#ifndef __ICorDebugStringValue_FWD_DEFINED__
#define __ICorDebugStringValue_FWD_DEFINED__
typedef interface ICorDebugStringValue ICorDebugStringValue;

#endif 	/* __ICorDebugStringValue_FWD_DEFINED__ */


#ifndef __ICorDebugGenericValue_FWD_DEFINED__
#define __ICorDebugGenericValue_FWD_DEFINED__
typedef interface ICorDebugGenericValue ICorDebugGenericValue;

#endif 	/* __ICorDebugGenericValue_FWD_DEFINED__ */


#ifndef __ICorDebugBoxValue_FWD_DEFINED__
#define __ICorDebugBoxValue_FWD_DEFINED__
typedef interface ICorDebugBoxValue ICorDebugBoxValue;

#endif 	/* __ICorDebugBoxValue_FWD_DEFINED__ */


#ifndef __ICorDebugArrayValue_FWD_DEFINED__
#define __ICorDebugArrayValue_FWD_DEFINED__
typedef interface ICorDebugArrayValue ICorDebugArrayValue;

#endif 	/* __ICorDebugArrayValue_FWD_DEFINED__ */


#ifndef __ICorDebugFrame_FWD_DEFINED__
#define __ICorDebugFrame_FWD_DEFINED__
typedef interface ICorDebugFrame ICorDebugFrame;

#endif 	/* __ICorDebugFrame_FWD_DEFINED__ */


#ifndef __ICorDebugILFrame_FWD_DEFINED__
#define __ICorDebugILFrame_FWD_DEFINED__
typedef interface ICorDebugILFrame ICorDebugILFrame;

#endif 	/* __ICorDebugILFrame_FWD_DEFINED__ */


#ifndef __ICorDebugInternalFrame_FWD_DEFINED__
#define __ICorDebugInternalFrame_FWD_DEFINED__
typedef interface ICorDebugInternalFrame ICorDebugInternalFrame;

#endif 	/* __ICorDebugInternalFrame_FWD_DEFINED__ */


#ifndef __ICorDebugInternalFrame2_FWD_DEFINED__
#define __ICorDebugInternalFrame2_FWD_DEFINED__
typedef interface ICorDebugInternalFrame2 ICorDebugInternalFrame2;

#endif 	/* __ICorDebugInternalFrame2_FWD_DEFINED__ */


#ifndef __ICorDebugNativeFrame_FWD_DEFINED__
#define __ICorDebugNativeFrame_FWD_DEFINED__
typedef interface ICorDebugNativeFrame ICorDebugNativeFrame;

#endif 	/* __ICorDebugNativeFrame_FWD_DEFINED__ */


#ifndef __ICorDebugNativeFrame2_FWD_DEFINED__
#define __ICorDebugNativeFrame2_FWD_DEFINED__
typedef interface ICorDebugNativeFrame2 ICorDebugNativeFrame2;

#endif 	/* __ICorDebugNativeFrame2_FWD_DEFINED__ */


#ifndef __ICorDebugRuntimeUnwindableFrame_FWD_DEFINED__
#define __ICorDebugRuntimeUnwindableFrame_FWD_DEFINED__
typedef interface ICorDebugRuntimeUnwindableFrame ICorDebugRuntimeUnwindableFrame;

#endif 	/* __ICorDebugRuntimeUnwindableFrame_FWD_DEFINED__ */


#ifndef __ICorDebugManagedCallback2_FWD_DEFINED__
#define __ICorDebugManagedCallback2_FWD_DEFINED__
typedef interface ICorDebugManagedCallback2 ICorDebugManagedCallback2;

#endif 	/* __ICorDebugManagedCallback2_FWD_DEFINED__ */


#ifndef __ICorDebugAppDomain2_FWD_DEFINED__
#define __ICorDebugAppDomain2_FWD_DEFINED__
typedef interface ICorDebugAppDomain2 ICorDebugAppDomain2;

#endif 	/* __ICorDebugAppDomain2_FWD_DEFINED__ */


#ifndef __ICorDebugAppDomain3_FWD_DEFINED__
#define __ICorDebugAppDomain3_FWD_DEFINED__
typedef interface ICorDebugAppDomain3 ICorDebugAppDomain3;

#endif 	/* __ICorDebugAppDomain3_FWD_DEFINED__ */


#ifndef __ICorDebugAssembly2_FWD_DEFINED__
#define __ICorDebugAssembly2_FWD_DEFINED__
typedef interface ICorDebugAssembly2 ICorDebugAssembly2;

#endif 	/* __ICorDebugAssembly2_FWD_DEFINED__ */


#ifndef __ICorDebugProcess2_FWD_DEFINED__
#define __ICorDebugProcess2_FWD_DEFINED__
typedef interface ICorDebugProcess2 ICorDebugProcess2;

#endif 	/* __ICorDebugProcess2_FWD_DEFINED__ */


#ifndef __ICorDebugStepper2_FWD_DEFINED__
#define __ICorDebugStepper2_FWD_DEFINED__
typedef interface ICorDebugStepper2 ICorDebugStepper2;

#endif 	/* __ICorDebugStepper2_FWD_DEFINED__ */


#ifndef __ICorDebugThread2_FWD_DEFINED__
#define __ICorDebugThread2_FWD_DEFINED__
typedef interface ICorDebugThread2 ICorDebugThread2;

#endif 	/* __ICorDebugThread2_FWD_DEFINED__ */


#ifndef __ICorDebugThread3_FWD_DEFINED__
#define __ICorDebugThread3_FWD_DEFINED__
typedef interface ICorDebugThread3 ICorDebugThread3;

#endif 	/* __ICorDebugThread3_FWD_DEFINED__ */


#ifndef __ICorDebugILFrame2_FWD_DEFINED__
#define __ICorDebugILFrame2_FWD_DEFINED__
typedef interface ICorDebugILFrame2 ICorDebugILFrame2;

#endif 	/* __ICorDebugILFrame2_FWD_DEFINED__ */


#ifndef __ICorDebugModule2_FWD_DEFINED__
#define __ICorDebugModule2_FWD_DEFINED__
typedef interface ICorDebugModule2 ICorDebugModule2;

#endif 	/* __ICorDebugModule2_FWD_DEFINED__ */


#ifndef __ICorDebugFunction2_FWD_DEFINED__
#define __ICorDebugFunction2_FWD_DEFINED__
typedef interface ICorDebugFunction2 ICorDebugFunction2;

#endif 	/* __ICorDebugFunction2_FWD_DEFINED__ */


#ifndef __ICorDebugClass2_FWD_DEFINED__
#define __ICorDebugClass2_FWD_DEFINED__
typedef interface ICorDebugClass2 ICorDebugClass2;

#endif 	/* __ICorDebugClass2_FWD_DEFINED__ */


#ifndef __ICorDebugEval2_FWD_DEFINED__
#define __ICorDebugEval2_FWD_DEFINED__
typedef interface ICorDebugEval2 ICorDebugEval2;

#endif 	/* __ICorDebugEval2_FWD_DEFINED__ */


#ifndef __ICorDebugValue2_FWD_DEFINED__
#define __ICorDebugValue2_FWD_DEFINED__
typedef interface ICorDebugValue2 ICorDebugValue2;

#endif 	/* __ICorDebugValue2_FWD_DEFINED__ */


#ifndef __ICorDebugObjectValue2_FWD_DEFINED__
#define __ICorDebugObjectValue2_FWD_DEFINED__
typedef interface ICorDebugObjectValue2 ICorDebugObjectValue2;

#endif 	/* __ICorDebugObjectValue2_FWD_DEFINED__ */


#ifndef __ICorDebugHandleValue_FWD_DEFINED__
#define __ICorDebugHandleValue_FWD_DEFINED__
typedef interface ICorDebugHandleValue ICorDebugHandleValue;

#endif 	/* __ICorDebugHandleValue_FWD_DEFINED__ */


#ifndef __ICorDebugHeapValue2_FWD_DEFINED__
#define __ICorDebugHeapValue2_FWD_DEFINED__
typedef interface ICorDebugHeapValue2 ICorDebugHeapValue2;

#endif 	/* __ICorDebugHeapValue2_FWD_DEFINED__ */


#ifndef __ICorDebugComObjectValue_FWD_DEFINED__
#define __ICorDebugComObjectValue_FWD_DEFINED__
typedef interface ICorDebugComObjectValue ICorDebugComObjectValue;

#endif 	/* __ICorDebugComObjectValue_FWD_DEFINED__ */


#ifndef __ICorDebugModule3_FWD_DEFINED__
#define __ICorDebugModule3_FWD_DEFINED__
typedef interface ICorDebugModule3 ICorDebugModule3;

#endif 	/* __ICorDebugModule3_FWD_DEFINED__ */


/* header files for imported files */
#include "unknwn.h"
#include "objidl.h"

#ifdef __cplusplus
extern "C"{
#endif


/* interface __MIDL_itf_cordebug_0000_0000 */
/* [local] */

#if 0
typedef UINT32 mdToken;

typedef mdToken mdModule;

typedef SIZE_T mdScope;

typedef mdToken mdTypeDef;

typedef mdToken mdSourceFile;

typedef mdToken mdMemberRef;

typedef mdToken mdMethodDef;

typedef mdToken mdFieldDef;

typedef mdToken mdSignature;

typedef ULONG CorElementType;

typedef SIZE_T PCCOR_SIGNATURE;

typedef SIZE_T LPDEBUG_EVENT;

typedef SIZE_T LPSTARTUPINFOW;

typedef SIZE_T LPPROCESS_INFORMATION;

typedef const void *LPCVOID;

#endif
typedef /* [wire_marshal] */ void *HPROCESS;

typedef /* [wire_marshal] */ void *HTHREAD;

typedef UINT64 TASKID;

typedef DWORD CONNID;

#ifndef _COR_IL_MAP
#define _COR_IL_MAP
typedef struct _COR_IL_MAP
    {
    ULONG32 oldOffset;
    ULONG32 newOffset;
    BOOL fAccurate;
    } 	COR_IL_MAP;

#endif //_COR_IL_MAP
#ifndef _COR_DEBUG_IL_TO_NATIVE_MAP_
#define _COR_DEBUG_IL_TO_NATIVE_MAP_
typedef
enum CorDebugIlToNativeMappingTypes
    {
        NO_MAPPING	= -1,
        PROLOG	= -2,
        EPILOG	= -3
    } 	CorDebugIlToNativeMappingTypes;

typedef struct COR_DEBUG_IL_TO_NATIVE_MAP
    {
    ULONG32 ilOffset;
    ULONG32 nativeStartOffset;
    ULONG32 nativeEndOffset;
    } 	COR_DEBUG_IL_TO_NATIVE_MAP;

#endif // _COR_DEBUG_IL_TO_NATIVE_MAP_
#define REMOTE_DEBUGGING_DLL_ENTRY L"Software\\Microsoft\\.NETFramework\\Debugger\\ActivateRemoteDebugging"
typedef
enum CorDebugJITCompilerFlags
    {
        CORDEBUG_JIT_DEFAULT	= 0x1,
        CORDEBUG_JIT_DISABLE_OPTIMIZATION	= 0x3,
        CORDEBUG_JIT_ENABLE_ENC	= 0x7
    } 	CorDebugJITCompilerFlags;

typedef
enum CorDebugJITCompilerFlagsDecprecated
    {
        CORDEBUG_JIT_TRACK_DEBUG_INFO	= 0x1
    } 	CorDebugJITCompilerFlagsDeprecated;

typedef
enum CorDebugNGENPolicy
    {
        DISABLE_LOCAL_NIC	= 1
    } 	CorDebugNGENPolicy;

#pragma warning(push)
#pragma warning(disable:28718)

































































#pragma warning(pop)
typedef ULONG64 CORDB_ADDRESS;

typedef ULONG64 CORDB_REGISTER;

typedef DWORD CORDB_CONTINUE_STATUS;

typedef
enum CorDebugBlockingReason
    {
        BLOCKING_NONE	= 0,
        BLOCKING_MONITOR_CRITICAL_SECTION	= 0x1,
        BLOCKING_MONITOR_EVENT	= 0x2
    } 	CorDebugBlockingReason;

typedef struct CorDebugBlockingObject
    {
    ICorDebugValue *pBlockingObject;
    DWORD dwTimeout;
    CorDebugBlockingReason blockingReason;
    } 	CorDebugBlockingObject;

typedef struct CorDebugExceptionObjectStackFrame
    {
    ICorDebugModule *pModule;
    CORDB_ADDRESS ip;
    mdMethodDef methodDef;
    BOOL isLastForeignExceptionFrame;
    } 	CorDebugExceptionObjectStackFrame;

typedef struct CorDebugGuidToTypeMapping
    {
    GUID iid;
    ICorDebugType *pType;
    } 	CorDebugGuidToTypeMapping;



extern RPC_IF_HANDLE __MIDL_itf_cordebug_0000_0000_v0_0_c_ifspec;
extern RPC_IF_HANDLE __MIDL_itf_cordebug_0000_0000_v0_0_s_ifspec;

#ifndef __ICorDebugDataTarget_INTERFACE_DEFINED__
#define __ICorDebugDataTarget_INTERFACE_DEFINED__

/* interface ICorDebugDataTarget */
/* [unique][uuid][local][object] */

typedef
enum CorDebugPlatform
    {
        CORDB_PLATFORM_WINDOWS_X86	= 0,
        CORDB_PLATFORM_WINDOWS_AMD64	= ( CORDB_PLATFORM_WINDOWS_X86 + 1 ) ,
        CORDB_PLATFORM_WINDOWS_IA64	= ( CORDB_PLATFORM_WINDOWS_AMD64 + 1 ) ,
        CORDB_PLATFORM_MAC_PPC	= ( CORDB_PLATFORM_WINDOWS_IA64 + 1 ) ,
        CORDB_PLATFORM_MAC_X86	= ( CORDB_PLATFORM_MAC_PPC + 1 ) ,
        CORDB_PLATFORM_WINDOWS_ARM	= ( CORDB_PLATFORM_MAC_X86 + 1 ) ,
        CORDB_PLATFORM_MAC_AMD64	= ( CORDB_PLATFORM_WINDOWS_ARM + 1 ) ,
        CORDB_PLATFORM_WINDOWS_ARM64	= ( CORDB_PLATFORM_MAC_AMD64 + 1 ) ,
        CORDB_PLATFORM_POSIX_AMD64	= ( CORDB_PLATFORM_WINDOWS_ARM64 + 1 ) ,
        CORDB_PLATFORM_POSIX_X86	= ( CORDB_PLATFORM_POSIX_AMD64 + 1 ) ,
        CORDB_PLATFORM_POSIX_ARM	= ( CORDB_PLATFORM_POSIX_X86 + 1 ) ,
        CORDB_PLATFORM_POSIX_ARM64	= ( CORDB_PLATFORM_POSIX_ARM + 1 )
    } 	CorDebugPlatform;


EXTERN_C const IID IID_ICorDebugDataTarget;

#if defined(__cplusplus) && !defined(CINTERFACE)

    MIDL_INTERFACE("FE06DC28-49FB-4636-A4A3-E80DB4AE116C")
    ICorDebugDataTarget : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE GetPlatform(
            /* [out] */ CorDebugPlatform *pTargetPlatform) = 0;

        virtual HRESULT STDMETHODCALLTYPE ReadVirtual(
            /* [in] */ CORDB_ADDRESS address,
            /* [length_is][size_is][out] */ BYTE *pBuffer,
            /* [in] */ ULONG32 bytesRequested,
            /* [out] */ ULONG32 *pBytesRead) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetThreadContext(
            /* [in] */ DWORD dwThreadID,
            /* [in] */ ULONG32 contextFlags,
            /* [in] */ ULONG32 contextSize,
            /* [size_is][out] */ BYTE *pContext) = 0;

    };


#else 	/* C style interface */

    typedef struct ICorDebugDataTargetVtbl
    {
        BEGIN_INTERFACE

        HRESULT ( STDMETHODCALLTYPE *QueryInterface )(
            ICorDebugDataTarget * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */
            _COM_Outptr_  void **ppvObject);

        ULONG ( STDMETHODCALLTYPE *AddRef )(
            ICorDebugDataTarget * This);

        ULONG ( STDMETHODCALLTYPE *Release )(
            ICorDebugDataTarget * This);

        HRESULT ( STDMETHODCALLTYPE *GetPlatform )(
            ICorDebugDataTarget * This,
            /* [out] */ CorDebugPlatform *pTargetPlatform);

        HRESULT ( STDMETHODCALLTYPE *ReadVirtual )(
            ICorDebugDataTarget * This,
            /* [in] */ CORDB_ADDRESS address,
            /* [length_is][size_is][out] */ BYTE *pBuffer,
            /* [in] */ ULONG32 bytesRequested,
            /* [out] */ ULONG32 *pBytesRead);

        HRESULT ( STDMETHODCALLTYPE *GetThreadContext )(
            ICorDebugDataTarget * This,
            /* [in] */ DWORD dwThreadID,
            /* [in] */ ULONG32 contextFlags,
            /* [in] */ ULONG32 contextSize,
            /* [size_is][out] */ BYTE *pContext);

        END_INTERFACE
    } ICorDebugDataTargetVtbl;

    interface ICorDebugDataTarget
    {
        CONST_VTBL struct ICorDebugDataTargetVtbl *lpVtbl;
    };



#ifdef COBJMACROS


#define ICorDebugDataTarget_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) )

#define ICorDebugDataTarget_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) )

#define ICorDebugDataTarget_Release(This)	\
    ( (This)->lpVtbl -> Release(This) )


#define ICorDebugDataTarget_GetPlatform(This,pTargetPlatform)	\
    ( (This)->lpVtbl -> GetPlatform(This,pTargetPlatform) )

#define ICorDebugDataTarget_ReadVirtual(This,address,pBuffer,bytesRequested,pBytesRead)	\
    ( (This)->lpVtbl -> ReadVirtual(This,address,pBuffer,bytesRequested,pBytesRead) )

#define ICorDebugDataTarget_GetThreadContext(This,dwThreadID,contextFlags,contextSize,pContext)	\
    ( (This)->lpVtbl -> GetThreadContext(This,dwThreadID,contextFlags,contextSize,pContext) )

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ICorDebugDataTarget_INTERFACE_DEFINED__ */


#ifndef __ICorDebugStaticFieldSymbol_INTERFACE_DEFINED__
#define __ICorDebugStaticFieldSymbol_INTERFACE_DEFINED__

/* interface ICorDebugStaticFieldSymbol */
/* [unique][uuid][local][object] */


EXTERN_C const IID IID_ICorDebugStaticFieldSymbol;

#if defined(__cplusplus) && !defined(CINTERFACE)

    MIDL_INTERFACE("CBF9DA63-F68D-4BBB-A21C-15A45EAADF5B")
    ICorDebugStaticFieldSymbol : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE GetName(
            /* [in] */ ULONG32 cchName,
            /* [out] */ ULONG32 *pcchName,
            /* [length_is][size_is][out] */ WCHAR szName[  ]) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetSize(
            /* [out] */ ULONG32 *pcbSize) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetAddress(
            /* [out] */ CORDB_ADDRESS *pRVA) = 0;

    };


#else 	/* C style interface */

    typedef struct ICorDebugStaticFieldSymbolVtbl
    {
        BEGIN_INTERFACE

        HRESULT ( STDMETHODCALLTYPE *QueryInterface )(
            ICorDebugStaticFieldSymbol * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */
            _COM_Outptr_  void **ppvObject);

        ULONG ( STDMETHODCALLTYPE *AddRef )(
            ICorDebugStaticFieldSymbol * This);

        ULONG ( STDMETHODCALLTYPE *Release )(
            ICorDebugStaticFieldSymbol * This);

        HRESULT ( STDMETHODCALLTYPE *GetName )(
            ICorDebugStaticFieldSymbol * This,
            /* [in] */ ULONG32 cchName,
            /* [out] */ ULONG32 *pcchName,
            /* [length_is][size_is][out] */ WCHAR szName[  ]);

        HRESULT ( STDMETHODCALLTYPE *GetSize )(
            ICorDebugStaticFieldSymbol * This,
            /* [out] */ ULONG32 *pcbSize);

        HRESULT ( STDMETHODCALLTYPE *GetAddress )(
            ICorDebugStaticFieldSymbol * This,
            /* [out] */ CORDB_ADDRESS *pRVA);

        END_INTERFACE
    } ICorDebugStaticFieldSymbolVtbl;

    interface ICorDebugStaticFieldSymbol
    {
        CONST_VTBL struct ICorDebugStaticFieldSymbolVtbl *lpVtbl;
    };



#ifdef COBJMACROS


#define ICorDebugStaticFieldSymbol_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) )

#define ICorDebugStaticFieldSymbol_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) )

#define ICorDebugStaticFieldSymbol_Release(This)	\
    ( (This)->lpVtbl -> Release(This) )


#define ICorDebugStaticFieldSymbol_GetName(This,cchName,pcchName,szName)	\
    ( (This)->lpVtbl -> GetName(This,cchName,pcchName,szName) )

#define ICorDebugStaticFieldSymbol_GetSize(This,pcbSize)	\
    ( (This)->lpVtbl -> GetSize(This,pcbSize) )

#define ICorDebugStaticFieldSymbol_GetAddress(This,pRVA)	\
    ( (This)->lpVtbl -> GetAddress(This,pRVA) )

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ICorDebugStaticFieldSymbol_INTERFACE_DEFINED__ */


#ifndef __ICorDebugInstanceFieldSymbol_INTERFACE_DEFINED__
#define __ICorDebugInstanceFieldSymbol_INTERFACE_DEFINED__

/* interface ICorDebugInstanceFieldSymbol */
/* [unique][uuid][local][object] */


EXTERN_C const IID IID_ICorDebugInstanceFieldSymbol;

#if defined(__cplusplus) && !defined(CINTERFACE)

    MIDL_INTERFACE("A074096B-3ADC-4485-81DA-68C7A4EA52DB")
    ICorDebugInstanceFieldSymbol : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE GetName(
            /* [in] */ ULONG32 cchName,
            /* [out] */ ULONG32 *pcchName,
            /* [length_is][size_is][out] */ WCHAR szName[  ]) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetSize(
            /* [out] */ ULONG32 *pcbSize) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetOffset(
            /* [out] */ ULONG32 *pcbOffset) = 0;

    };


#else 	/* C style interface */

    typedef struct ICorDebugInstanceFieldSymbolVtbl
    {
        BEGIN_INTERFACE

        HRESULT ( STDMETHODCALLTYPE *QueryInterface )(
            ICorDebugInstanceFieldSymbol * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */
            _COM_Outptr_  void **ppvObject);

        ULONG ( STDMETHODCALLTYPE *AddRef )(
            ICorDebugInstanceFieldSymbol * This);

        ULONG ( STDMETHODCALLTYPE *Release )(
            ICorDebugInstanceFieldSymbol * This);

        HRESULT ( STDMETHODCALLTYPE *GetName )(
            ICorDebugInstanceFieldSymbol * This,
            /* [in] */ ULONG32 cchName,
            /* [out] */ ULONG32 *pcchName,
            /* [length_is][size_is][out] */ WCHAR szName[  ]);

        HRESULT ( STDMETHODCALLTYPE *GetSize )(
            ICorDebugInstanceFieldSymbol * This,
            /* [out] */ ULONG32 *pcbSize);

        HRESULT ( STDMETHODCALLTYPE *GetOffset )(
            ICorDebugInstanceFieldSymbol * This,
            /* [out] */ ULONG32 *pcbOffset);

        END_INTERFACE
    } ICorDebugInstanceFieldSymbolVtbl;

    interface ICorDebugInstanceFieldSymbol
    {
        CONST_VTBL struct ICorDebugInstanceFieldSymbolVtbl *lpVtbl;
    };



#ifdef COBJMACROS


#define ICorDebugInstanceFieldSymbol_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) )

#define ICorDebugInstanceFieldSymbol_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) )

#define ICorDebugInstanceFieldSymbol_Release(This)	\
    ( (This)->lpVtbl -> Release(This) )


#define ICorDebugInstanceFieldSymbol_GetName(This,cchName,pcchName,szName)	\
    ( (This)->lpVtbl -> GetName(This,cchName,pcchName,szName) )

#define ICorDebugInstanceFieldSymbol_GetSize(This,pcbSize)	\
    ( (This)->lpVtbl -> GetSize(This,pcbSize) )

#define ICorDebugInstanceFieldSymbol_GetOffset(This,pcbOffset)	\
    ( (This)->lpVtbl -> GetOffset(This,pcbOffset) )

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ICorDebugInstanceFieldSymbol_INTERFACE_DEFINED__ */


#ifndef __ICorDebugVariableSymbol_INTERFACE_DEFINED__
#define __ICorDebugVariableSymbol_INTERFACE_DEFINED__

/* interface ICorDebugVariableSymbol */
/* [unique][uuid][local][object] */


EXTERN_C const IID IID_ICorDebugVariableSymbol;

#if defined(__cplusplus) && !defined(CINTERFACE)

    MIDL_INTERFACE("707E8932-1163-48D9-8A93-F5B1F480FBB7")
    ICorDebugVariableSymbol : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE GetName(
            /* [in] */ ULONG32 cchName,
            /* [out] */ ULONG32 *pcchName,
            /* [length_is][size_is][out] */ WCHAR szName[  ]) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetSize(
            /* [out] */ ULONG32 *pcbValue) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetValue(
            /* [in] */ ULONG32 offset,
            /* [in] */ ULONG32 cbContext,
            /* [size_is][in] */ BYTE context[  ],
            /* [in] */ ULONG32 cbValue,
            /* [out] */ ULONG32 *pcbValue,
            /* [length_is][size_is][out] */ BYTE pValue[  ]) = 0;

        virtual HRESULT STDMETHODCALLTYPE SetValue(
            /* [in] */ ULONG32 offset,
            /* [in] */ DWORD threadID,
            /* [in] */ ULONG32 cbContext,
            /* [size_is][in] */ BYTE context[  ],
            /* [in] */ ULONG32 cbValue,
            /* [size_is][in] */ BYTE pValue[  ]) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetSlotIndex(
            /* [out] */ ULONG32 *pSlotIndex) = 0;

    };


#else 	/* C style interface */

    typedef struct ICorDebugVariableSymbolVtbl
    {
        BEGIN_INTERFACE

        HRESULT ( STDMETHODCALLTYPE *QueryInterface )(
            ICorDebugVariableSymbol * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */
            _COM_Outptr_  void **ppvObject);

        ULONG ( STDMETHODCALLTYPE *AddRef )(
            ICorDebugVariableSymbol * This);

        ULONG ( STDMETHODCALLTYPE *Release )(
            ICorDebugVariableSymbol * This);

        HRESULT ( STDMETHODCALLTYPE *GetName )(
            ICorDebugVariableSymbol * This,
            /* [in] */ ULONG32 cchName,
            /* [out] */ ULONG32 *pcchName,
            /* [length_is][size_is][out] */ WCHAR szName[  ]);

        HRESULT ( STDMETHODCALLTYPE *GetSize )(
            ICorDebugVariableSymbol * This,
            /* [out] */ ULONG32 *pcbValue);

        HRESULT ( STDMETHODCALLTYPE *GetValue )(
            ICorDebugVariableSymbol * This,
            /* [in] */ ULONG32 offset,
            /* [in] */ ULONG32 cbContext,
            /* [size_is][in] */ BYTE context[  ],
            /* [in] */ ULONG32 cbValue,
            /* [out] */ ULONG32 *pcbValue,
            /* [length_is][size_is][out] */ BYTE pValue[  ]);

        HRESULT ( STDMETHODCALLTYPE *SetValue )(
            ICorDebugVariableSymbol * This,
            /* [in] */ ULONG32 offset,
            /* [in] */ DWORD threadID,
            /* [in] */ ULONG32 cbContext,
            /* [size_is][in] */ BYTE context[  ],
            /* [in] */ ULONG32 cbValue,
            /* [size_is][in] */ BYTE pValue[  ]);

        HRESULT ( STDMETHODCALLTYPE *GetSlotIndex )(
            ICorDebugVariableSymbol * This,
            /* [out] */ ULONG32 *pSlotIndex);

        END_INTERFACE
    } ICorDebugVariableSymbolVtbl;

    interface ICorDebugVariableSymbol
    {
        CONST_VTBL struct ICorDebugVariableSymbolVtbl *lpVtbl;
    };



#ifdef COBJMACROS


#define ICorDebugVariableSymbol_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) )

#define ICorDebugVariableSymbol_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) )

#define ICorDebugVariableSymbol_Release(This)	\
    ( (This)->lpVtbl -> Release(This) )


#define ICorDebugVariableSymbol_GetName(This,cchName,pcchName,szName)	\
    ( (This)->lpVtbl -> GetName(This,cchName,pcchName,szName) )

#define ICorDebugVariableSymbol_GetSize(This,pcbValue)	\
    ( (This)->lpVtbl -> GetSize(This,pcbValue) )

#define ICorDebugVariableSymbol_GetValue(This,offset,cbContext,context,cbValue,pcbValue,pValue)	\
    ( (This)->lpVtbl -> GetValue(This,offset,cbContext,context,cbValue,pcbValue,pValue) )

#define ICorDebugVariableSymbol_SetValue(This,offset,threadID,cbContext,context,cbValue,pValue)	\
    ( (This)->lpVtbl -> SetValue(This,offset,threadID,cbContext,context,cbValue,pValue) )

#define ICorDebugVariableSymbol_GetSlotIndex(This,pSlotIndex)	\
    ( (This)->lpVtbl -> GetSlotIndex(This,pSlotIndex) )

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ICorDebugVariableSymbol_INTERFACE_DEFINED__ */


#ifndef __ICorDebugMemoryBuffer_INTERFACE_DEFINED__
#define __ICorDebugMemoryBuffer_INTERFACE_DEFINED__

/* interface ICorDebugMemoryBuffer */
/* [unique][local][uuid][object] */


EXTERN_C const IID IID_ICorDebugMemoryBuffer;

#if defined(__cplusplus) && !defined(CINTERFACE)

    MIDL_INTERFACE("677888B3-D160-4B8C-A73B-D79E6AAA1D13")
    ICorDebugMemoryBuffer : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE GetStartAddress(
            /* [out] */ LPCVOID *address) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetSize(
            /* [out] */ ULONG32 *pcbBufferLength) = 0;

    };


#else 	/* C style interface */

    typedef struct ICorDebugMemoryBufferVtbl
    {
        BEGIN_INTERFACE

        HRESULT ( STDMETHODCALLTYPE *QueryInterface )(
            ICorDebugMemoryBuffer * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */
            _COM_Outptr_  void **ppvObject);

        ULONG ( STDMETHODCALLTYPE *AddRef )(
            ICorDebugMemoryBuffer * This);

        ULONG ( STDMETHODCALLTYPE *Release )(
            ICorDebugMemoryBuffer * This);

        HRESULT ( STDMETHODCALLTYPE *GetStartAddress )(
            ICorDebugMemoryBuffer * This,
            /* [out] */ LPCVOID *address);

        HRESULT ( STDMETHODCALLTYPE *GetSize )(
            ICorDebugMemoryBuffer * This,
            /* [out] */ ULONG32 *pcbBufferLength);

        END_INTERFACE
    } ICorDebugMemoryBufferVtbl;

    interface ICorDebugMemoryBuffer
    {
        CONST_VTBL struct ICorDebugMemoryBufferVtbl *lpVtbl;
    };



#ifdef COBJMACROS


#define ICorDebugMemoryBuffer_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) )

#define ICorDebugMemoryBuffer_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) )

#define ICorDebugMemoryBuffer_Release(This)	\
    ( (This)->lpVtbl -> Release(This) )


#define ICorDebugMemoryBuffer_GetStartAddress(This,address)	\
    ( (This)->lpVtbl -> GetStartAddress(This,address) )

#define ICorDebugMemoryBuffer_GetSize(This,pcbBufferLength)	\
    ( (This)->lpVtbl -> GetSize(This,pcbBufferLength) )

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ICorDebugMemoryBuffer_INTERFACE_DEFINED__ */


#ifndef __ICorDebugMergedAssemblyRecord_INTERFACE_DEFINED__
#define __ICorDebugMergedAssemblyRecord_INTERFACE_DEFINED__

/* interface ICorDebugMergedAssemblyRecord */
/* [unique][local][uuid][object] */


EXTERN_C const IID IID_ICorDebugMergedAssemblyRecord;

#if defined(__cplusplus) && !defined(CINTERFACE)

    MIDL_INTERFACE("FAA8637B-3BBE-4671-8E26-3B59875B922A")
    ICorDebugMergedAssemblyRecord : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE GetSimpleName(
            /* [in] */ ULONG32 cchName,
            /* [out] */ ULONG32 *pcchName,
            /* [length_is][size_is][out] */ WCHAR szName[  ]) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetVersion(
            /* [out] */ USHORT *pMajor,
            /* [out] */ USHORT *pMinor,
            /* [out] */ USHORT *pBuild,
            /* [out] */ USHORT *pRevision) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetCulture(
            /* [in] */ ULONG32 cchCulture,
            /* [out] */ ULONG32 *pcchCulture,
            /* [length_is][size_is][out] */ WCHAR szCulture[  ]) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetPublicKey(
            /* [in] */ ULONG32 cbPublicKey,
            /* [out] */ ULONG32 *pcbPublicKey,
            /* [length_is][size_is][out] */ BYTE pbPublicKey[  ]) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetPublicKeyToken(
            /* [in] */ ULONG32 cbPublicKeyToken,
            /* [out] */ ULONG32 *pcbPublicKeyToken,
            /* [length_is][size_is][out] */ BYTE pbPublicKeyToken[  ]) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetIndex(
            /* [out] */ ULONG32 *pIndex) = 0;

    };


#else 	/* C style interface */

    typedef struct ICorDebugMergedAssemblyRecordVtbl
    {
        BEGIN_INTERFACE

        HRESULT ( STDMETHODCALLTYPE *QueryInterface )(
            ICorDebugMergedAssemblyRecord * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */
            _COM_Outptr_  void **ppvObject);

        ULONG ( STDMETHODCALLTYPE *AddRef )(
            ICorDebugMergedAssemblyRecord * This);

        ULONG ( STDMETHODCALLTYPE *Release )(
            ICorDebugMergedAssemblyRecord * This);

        HRESULT ( STDMETHODCALLTYPE *GetSimpleName )(
            ICorDebugMergedAssemblyRecord * This,
            /* [in] */ ULONG32 cchName,
            /* [out] */ ULONG32 *pcchName,
            /* [length_is][size_is][out] */ WCHAR szName[  ]);

        HRESULT ( STDMETHODCALLTYPE *GetVersion )(
            ICorDebugMergedAssemblyRecord * This,
            /* [out] */ USHORT *pMajor,
            /* [out] */ USHORT *pMinor,
            /* [out] */ USHORT *pBuild,
            /* [out] */ USHORT *pRevision);

        HRESULT ( STDMETHODCALLTYPE *GetCulture )(
            ICorDebugMergedAssemblyRecord * This,
            /* [in] */ ULONG32 cchCulture,
            /* [out] */ ULONG32 *pcchCulture,
            /* [length_is][size_is][out] */ WCHAR szCulture[  ]);

        HRESULT ( STDMETHODCALLTYPE *GetPublicKey )(
            ICorDebugMergedAssemblyRecord * This,
            /* [in] */ ULONG32 cbPublicKey,
            /* [out] */ ULONG32 *pcbPublicKey,
            /* [length_is][size_is][out] */ BYTE pbPublicKey[  ]);

        HRESULT ( STDMETHODCALLTYPE *GetPublicKeyToken )(
            ICorDebugMergedAssemblyRecord * This,
            /* [in] */ ULONG32 cbPublicKeyToken,
            /* [out] */ ULONG32 *pcbPublicKeyToken,
            /* [length_is][size_is][out] */ BYTE pbPublicKeyToken[  ]);

        HRESULT ( STDMETHODCALLTYPE *GetIndex )(
            ICorDebugMergedAssemblyRecord * This,
            /* [out] */ ULONG32 *pIndex);

        END_INTERFACE
    } ICorDebugMergedAssemblyRecordVtbl;

    interface ICorDebugMergedAssemblyRecord
    {
        CONST_VTBL struct ICorDebugMergedAssemblyRecordVtbl *lpVtbl;
    };



#ifdef COBJMACROS


#define ICorDebugMergedAssemblyRecord_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) )

#define ICorDebugMergedAssemblyRecord_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) )

#define ICorDebugMergedAssemblyRecord_Release(This)	\
    ( (This)->lpVtbl -> Release(This) )


#define ICorDebugMergedAssemblyRecord_GetSimpleName(This,cchName,pcchName,szName)	\
    ( (This)->lpVtbl -> GetSimpleName(This,cchName,pcchName,szName) )

#define ICorDebugMergedAssemblyRecord_GetVersion(This,pMajor,pMinor,pBuild,pRevision)	\
    ( (This)->lpVtbl -> GetVersion(This,pMajor,pMinor,pBuild,pRevision) )

#define ICorDebugMergedAssemblyRecord_GetCulture(This,cchCulture,pcchCulture,szCulture)	\
    ( (This)->lpVtbl -> GetCulture(This,cchCulture,pcchCulture,szCulture) )

#define ICorDebugMergedAssemblyRecord_GetPublicKey(This,cbPublicKey,pcbPublicKey,pbPublicKey)	\
    ( (This)->lpVtbl -> GetPublicKey(This,cbPublicKey,pcbPublicKey,pbPublicKey) )

#define ICorDebugMergedAssemblyRecord_GetPublicKeyToken(This,cbPublicKeyToken,pcbPublicKeyToken,pbPublicKeyToken)	\
    ( (This)->lpVtbl -> GetPublicKeyToken(This,cbPublicKeyToken,pcbPublicKeyToken,pbPublicKeyToken) )

#define ICorDebugMergedAssemblyRecord_GetIndex(This,pIndex)	\
    ( (This)->lpVtbl -> GetIndex(This,pIndex) )

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ICorDebugMergedAssemblyRecord_INTERFACE_DEFINED__ */


#ifndef __ICorDebugSymbolProvider_INTERFACE_DEFINED__
#define __ICorDebugSymbolProvider_INTERFACE_DEFINED__

/* interface ICorDebugSymbolProvider */
/* [unique][local][uuid][object] */


EXTERN_C const IID IID_ICorDebugSymbolProvider;

#if defined(__cplusplus) && !defined(CINTERFACE)

    MIDL_INTERFACE("3948A999-FD8A-4C38-A708-8A71E9B04DBB")
    ICorDebugSymbolProvider : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE GetStaticFieldSymbols(
            /* [in] */ ULONG32 cbSignature,
            /* [size_is][in] */ BYTE typeSig[  ],
            /* [in] */ ULONG32 cRequestedSymbols,
            /* [out] */ ULONG32 *pcFetchedSymbols,
            /* [length_is][size_is][out] */ ICorDebugStaticFieldSymbol *pSymbols[  ]) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetInstanceFieldSymbols(
            /* [in] */ ULONG32 cbSignature,
            /* [size_is][in] */ BYTE typeSig[  ],
            /* [in] */ ULONG32 cRequestedSymbols,
            /* [out] */ ULONG32 *pcFetchedSymbols,
            /* [length_is][size_is][out] */ ICorDebugInstanceFieldSymbol *pSymbols[  ]) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetMethodLocalSymbols(
            /* [in] */ ULONG32 nativeRVA,
            /* [in] */ ULONG32 cRequestedSymbols,
            /* [out] */ ULONG32 *pcFetchedSymbols,
            /* [length_is][size_is][out] */ ICorDebugVariableSymbol *pSymbols[  ]) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetMethodParameterSymbols(
            /* [in] */ ULONG32 nativeRVA,
            /* [in] */ ULONG32 cRequestedSymbols,
            /* [out] */ ULONG32 *pcFetchedSymbols,
            /* [length_is][size_is][out] */ ICorDebugVariableSymbol *pSymbols[  ]) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetMergedAssemblyRecords(
            /* [in] */ ULONG32 cRequestedRecords,
            /* [out] */ ULONG32 *pcFetchedRecords,
            /* [length_is][size_is][out] */ ICorDebugMergedAssemblyRecord *pRecords[  ]) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetMethodProps(
            /* [in] */ ULONG32 codeRva,
            /* [out] */ mdToken *pMethodToken,
            /* [out] */ ULONG32 *pcGenericParams,
            /* [in] */ ULONG32 cbSignature,
            /* [out] */ ULONG32 *pcbSignature,
            /* [length_is][size_is][out] */ BYTE signature[  ]) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetTypeProps(
            /* [in] */ ULONG32 vtableRva,
            /* [in] */ ULONG32 cbSignature,
            /* [out] */ ULONG32 *pcbSignature,
            /* [length_is][size_is][out] */ BYTE signature[  ]) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetCodeRange(
            /* [in] */ ULONG32 codeRva,
            /* [out] */ ULONG32 *pCodeStartAddress,
            ULONG32 *pCodeSize) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetAssemblyImageBytes(
            /* [in] */ CORDB_ADDRESS rva,
            /* [in] */ ULONG32 length,
            /* [out] */ ICorDebugMemoryBuffer **ppMemoryBuffer) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetObjectSize(
            /* [in] */ ULONG32 cbSignature,
            /* [size_is][in] */ BYTE typeSig[  ],
            /* [out] */ ULONG32 *pObjectSize) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetAssemblyImageMetadata(
            /* [out] */ ICorDebugMemoryBuffer **ppMemoryBuffer) = 0;

    };


#else 	/* C style interface */

    typedef struct ICorDebugSymbolProviderVtbl
    {
        BEGIN_INTERFACE

        HRESULT ( STDMETHODCALLTYPE *QueryInterface )(
            ICorDebugSymbolProvider * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */
            _COM_Outptr_  void **ppvObject);

        ULONG ( STDMETHODCALLTYPE *AddRef )(
            ICorDebugSymbolProvider * This);

        ULONG ( STDMETHODCALLTYPE *Release )(
            ICorDebugSymbolProvider * This);

        HRESULT ( STDMETHODCALLTYPE *GetStaticFieldSymbols )(
            ICorDebugSymbolProvider * This,
            /* [in] */ ULONG32 cbSignature,
            /* [size_is][in] */ BYTE typeSig[  ],
            /* [in] */ ULONG32 cRequestedSymbols,
            /* [out] */ ULONG32 *pcFetchedSymbols,
            /* [length_is][size_is][out] */ ICorDebugStaticFieldSymbol *pSymbols[  ]);

        HRESULT ( STDMETHODCALLTYPE *GetInstanceFieldSymbols )(
            ICorDebugSymbolProvider * This,
            /* [in] */ ULONG32 cbSignature,
            /* [size_is][in] */ BYTE typeSig[  ],
            /* [in] */ ULONG32 cRequestedSymbols,
            /* [out] */ ULONG32 *pcFetchedSymbols,
            /* [length_is][size_is][out] */ ICorDebugInstanceFieldSymbol *pSymbols[  ]);

        HRESULT ( STDMETHODCALLTYPE *GetMethodLocalSymbols )(
            ICorDebugSymbolProvider * This,
            /* [in] */ ULONG32 nativeRVA,
            /* [in] */ ULONG32 cRequestedSymbols,
            /* [out] */ ULONG32 *pcFetchedSymbols,
            /* [length_is][size_is][out] */ ICorDebugVariableSymbol *pSymbols[  ]);

        HRESULT ( STDMETHODCALLTYPE *GetMethodParameterSymbols )(
            ICorDebugSymbolProvider * This,
            /* [in] */ ULONG32 nativeRVA,
            /* [in] */ ULONG32 cRequestedSymbols,
            /* [out] */ ULONG32 *pcFetchedSymbols,
            /* [length_is][size_is][out] */ ICorDebugVariableSymbol *pSymbols[  ]);

        HRESULT ( STDMETHODCALLTYPE *GetMergedAssemblyRecords )(
            ICorDebugSymbolProvider * This,
            /* [in] */ ULONG32 cRequestedRecords,
            /* [out] */ ULONG32 *pcFetchedRecords,
            /* [length_is][size_is][out] */ ICorDebugMergedAssemblyRecord *pRecords[  ]);

        HRESULT ( STDMETHODCALLTYPE *GetMethodProps )(
            ICorDebugSymbolProvider * This,
            /* [in] */ ULONG32 codeRva,
            /* [out] */ mdToken *pMethodToken,
            /* [out] */ ULONG32 *pcGenericParams,
            /* [in] */ ULONG32 cbSignature,
            /* [out] */ ULONG32 *pcbSignature,
            /* [length_is][size_is][out] */ BYTE signature[  ]);

        HRESULT ( STDMETHODCALLTYPE *GetTypeProps )(
            ICorDebugSymbolProvider * This,
            /* [in] */ ULONG32 vtableRva,
            /* [in] */ ULONG32 cbSignature,
            /* [out] */ ULONG32 *pcbSignature,
            /* [length_is][size_is][out] */ BYTE signature[  ]);

        HRESULT ( STDMETHODCALLTYPE *GetCodeRange )(
            ICorDebugSymbolProvider * This,
            /* [in] */ ULONG32 codeRva,
            /* [out] */ ULONG32 *pCodeStartAddress,
            ULONG32 *pCodeSize);

        HRESULT ( STDMETHODCALLTYPE *GetAssemblyImageBytes )(
            ICorDebugSymbolProvider * This,
            /* [in] */ CORDB_ADDRESS rva,
            /* [in] */ ULONG32 length,
            /* [out] */ ICorDebugMemoryBuffer **ppMemoryBuffer);

        HRESULT ( STDMETHODCALLTYPE *GetObjectSize )(
            ICorDebugSymbolProvider * This,
            /* [in] */ ULONG32 cbSignature,
            /* [size_is][in] */ BYTE typeSig[  ],
            /* [out] */ ULONG32 *pObjectSize);

        HRESULT ( STDMETHODCALLTYPE *GetAssemblyImageMetadata )(
            ICorDebugSymbolProvider * This,
            /* [out] */ ICorDebugMemoryBuffer **ppMemoryBuffer);

        END_INTERFACE
    } ICorDebugSymbolProviderVtbl;

    interface ICorDebugSymbolProvider
    {
        CONST_VTBL struct ICorDebugSymbolProviderVtbl *lpVtbl;
    };



#ifdef COBJMACROS


#define ICorDebugSymbolProvider_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) )

#define ICorDebugSymbolProvider_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) )

#define ICorDebugSymbolProvider_Release(This)	\
    ( (This)->lpVtbl -> Release(This) )


#define ICorDebugSymbolProvider_GetStaticFieldSymbols(This,cbSignature,typeSig,cRequestedSymbols,pcFetchedSymbols,pSymbols)	\
    ( (This)->lpVtbl -> GetStaticFieldSymbols(This,cbSignature,typeSig,cRequestedSymbols,pcFetchedSymbols,pSymbols) )

#define ICorDebugSymbolProvider_GetInstanceFieldSymbols(This,cbSignature,typeSig,cRequestedSymbols,pcFetchedSymbols,pSymbols)	\
    ( (This)->lpVtbl -> GetInstanceFieldSymbols(This,cbSignature,typeSig,cRequestedSymbols,pcFetchedSymbols,pSymbols) )

#define ICorDebugSymbolProvider_GetMethodLocalSymbols(This,nativeRVA,cRequestedSymbols,pcFetchedSymbols,pSymbols)	\
    ( (This)->lpVtbl -> GetMethodLocalSymbols(This,nativeRVA,cRequestedSymbols,pcFetchedSymbols,pSymbols) )

#define ICorDebugSymbolProvider_GetMethodParameterSymbols(This,nativeRVA,cRequestedSymbols,pcFetchedSymbols,pSymbols)	\
    ( (This)->lpVtbl -> GetMethodParameterSymbols(This,nativeRVA,cRequestedSymbols,pcFetchedSymbols,pSymbols) )

#define ICorDebugSymbolProvider_GetMergedAssemblyRecords(This,cRequestedRecords,pcFetchedRecords,pRecords)	\
    ( (This)->lpVtbl -> GetMergedAssemblyRecords(This,cRequestedRecords,pcFetchedRecords,pRecords) )

#define ICorDebugSymbolProvider_GetMethodProps(This,codeRva,pMethodToken,pcGenericParams,cbSignature,pcbSignature,signature)	\
    ( (This)->lpVtbl -> GetMethodProps(This,codeRva,pMethodToken,pcGenericParams,cbSignature,pcbSignature,signature) )

#define ICorDebugSymbolProvider_GetTypeProps(This,vtableRva,cbSignature,pcbSignature,signature)	\
    ( (This)->lpVtbl -> GetTypeProps(This,vtableRva,cbSignature,pcbSignature,signature) )

#define ICorDebugSymbolProvider_GetCodeRange(This,codeRva,pCodeStartAddress,pCodeSize)	\
    ( (This)->lpVtbl -> GetCodeRange(This,codeRva,pCodeStartAddress,pCodeSize) )

#define ICorDebugSymbolProvider_GetAssemblyImageBytes(This,rva,length,ppMemoryBuffer)	\
    ( (This)->lpVtbl -> GetAssemblyImageBytes(This,rva,length,ppMemoryBuffer) )

#define ICorDebugSymbolProvider_GetObjectSize(This,cbSignature,typeSig,pObjectSize)	\
    ( (This)->lpVtbl -> GetObjectSize(This,cbSignature,typeSig,pObjectSize) )

#define ICorDebugSymbolProvider_GetAssemblyImageMetadata(This,ppMemoryBuffer)	\
    ( (This)->lpVtbl -> GetAssemblyImageMetadata(This,ppMemoryBuffer) )

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ICorDebugSymbolProvider_INTERFACE_DEFINED__ */


#ifndef __ICorDebugSymbolProvider2_INTERFACE_DEFINED__
#define __ICorDebugSymbolProvider2_INTERFACE_DEFINED__

/* interface ICorDebugSymbolProvider2 */
/* [unique][local][uuid][object] */


EXTERN_C const IID IID_ICorDebugSymbolProvider2;

#if defined(__cplusplus) && !defined(CINTERFACE)

    MIDL_INTERFACE("F9801807-4764-4330-9E67-4F685094165E")
    ICorDebugSymbolProvider2 : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE GetGenericDictionaryInfo(
            /* [out] */ ICorDebugMemoryBuffer **ppMemoryBuffer) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetFrameProps(
            /* [in] */ ULONG32 codeRva,
            /* [out] */ ULONG32 *pCodeStartRva,
            /* [out] */ ULONG32 *pParentFrameStartRva) = 0;

    };


#else 	/* C style interface */

    typedef struct ICorDebugSymbolProvider2Vtbl
    {
        BEGIN_INTERFACE

        HRESULT ( STDMETHODCALLTYPE *QueryInterface )(
            ICorDebugSymbolProvider2 * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */
            _COM_Outptr_  void **ppvObject);

        ULONG ( STDMETHODCALLTYPE *AddRef )(
            ICorDebugSymbolProvider2 * This);

        ULONG ( STDMETHODCALLTYPE *Release )(
            ICorDebugSymbolProvider2 * This);

        HRESULT ( STDMETHODCALLTYPE *GetGenericDictionaryInfo )(
            ICorDebugSymbolProvider2 * This,
            /* [out] */ ICorDebugMemoryBuffer **ppMemoryBuffer);

        HRESULT ( STDMETHODCALLTYPE *GetFrameProps )(
            ICorDebugSymbolProvider2 * This,
            /* [in] */ ULONG32 codeRva,
            /* [out] */ ULONG32 *pCodeStartRva,
            /* [out] */ ULONG32 *pParentFrameStartRva);

        END_INTERFACE
    } ICorDebugSymbolProvider2Vtbl;

    interface ICorDebugSymbolProvider2
    {
        CONST_VTBL struct ICorDebugSymbolProvider2Vtbl *lpVtbl;
    };



#ifdef COBJMACROS


#define ICorDebugSymbolProvider2_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) )

#define ICorDebugSymbolProvider2_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) )

#define ICorDebugSymbolProvider2_Release(This)	\
    ( (This)->lpVtbl -> Release(This) )


#define ICorDebugSymbolProvider2_GetGenericDictionaryInfo(This,ppMemoryBuffer)	\
    ( (This)->lpVtbl -> GetGenericDictionaryInfo(This,ppMemoryBuffer) )

#define ICorDebugSymbolProvider2_GetFrameProps(This,codeRva,pCodeStartRva,pParentFrameStartRva)	\
    ( (This)->lpVtbl -> GetFrameProps(This,codeRva,pCodeStartRva,pParentFrameStartRva) )

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ICorDebugSymbolProvider2_INTERFACE_DEFINED__ */


#ifndef __ICorDebugVirtualUnwinder_INTERFACE_DEFINED__
#define __ICorDebugVirtualUnwinder_INTERFACE_DEFINED__

/* interface ICorDebugVirtualUnwinder */
/* [unique][local][uuid][object] */


EXTERN_C const IID IID_ICorDebugVirtualUnwinder;

#if defined(__cplusplus) && !defined(CINTERFACE)

    MIDL_INTERFACE("F69126B7-C787-4F6B-AE96-A569786FC670")
    ICorDebugVirtualUnwinder : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE GetContext(
            /* [in] */ ULONG32 contextFlags,
            /* [in] */ ULONG32 cbContextBuf,
            /* [out] */ ULONG32 *contextSize,
            /* [size_is][out] */ BYTE contextBuf[  ]) = 0;

        virtual HRESULT STDMETHODCALLTYPE Next( void) = 0;

    };


#else 	/* C style interface */

    typedef struct ICorDebugVirtualUnwinderVtbl
    {
        BEGIN_INTERFACE

        HRESULT ( STDMETHODCALLTYPE *QueryInterface )(
            ICorDebugVirtualUnwinder * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */
            _COM_Outptr_  void **ppvObject);

        ULONG ( STDMETHODCALLTYPE *AddRef )(
            ICorDebugVirtualUnwinder * This);

        ULONG ( STDMETHODCALLTYPE *Release )(
            ICorDebugVirtualUnwinder * This);

        HRESULT ( STDMETHODCALLTYPE *GetContext )(
            ICorDebugVirtualUnwinder * This,
            /* [in] */ ULONG32 contextFlags,
            /* [in] */ ULONG32 cbContextBuf,
            /* [out] */ ULONG32 *contextSize,
            /* [size_is][out] */ BYTE contextBuf[  ]);

        HRESULT ( STDMETHODCALLTYPE *Next )(
            ICorDebugVirtualUnwinder * This);

        END_INTERFACE
    } ICorDebugVirtualUnwinderVtbl;

    interface ICorDebugVirtualUnwinder
    {
        CONST_VTBL struct ICorDebugVirtualUnwinderVtbl *lpVtbl;
    };



#ifdef COBJMACROS


#define ICorDebugVirtualUnwinder_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) )

#define ICorDebugVirtualUnwinder_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) )

#define ICorDebugVirtualUnwinder_Release(This)	\
    ( (This)->lpVtbl -> Release(This) )


#define ICorDebugVirtualUnwinder_GetContext(This,contextFlags,cbContextBuf,contextSize,contextBuf)	\
    ( (This)->lpVtbl -> GetContext(This,contextFlags,cbContextBuf,contextSize,contextBuf) )

#define ICorDebugVirtualUnwinder_Next(This)	\
    ( (This)->lpVtbl -> Next(This) )

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ICorDebugVirtualUnwinder_INTERFACE_DEFINED__ */


#ifndef __ICorDebugDataTarget2_INTERFACE_DEFINED__
#define __ICorDebugDataTarget2_INTERFACE_DEFINED__

/* interface ICorDebugDataTarget2 */
/* [unique][local][uuid][object] */


EXTERN_C const IID IID_ICorDebugDataTarget2;

#if defined(__cplusplus) && !defined(CINTERFACE)

    MIDL_INTERFACE("2eb364da-605b-4e8d-b333-3394c4828d41")
    ICorDebugDataTarget2 : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE GetImageFromPointer(
            /* [in] */ CORDB_ADDRESS addr,
            /* [out] */ CORDB_ADDRESS *pImageBase,
            /* [out] */ ULONG32 *pSize) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetImageLocation(
            /* [in] */ CORDB_ADDRESS baseAddress,
            /* [in] */ ULONG32 cchName,
            /* [out] */ ULONG32 *pcchName,
            /* [length_is][size_is][out] */ WCHAR szName[  ]) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetSymbolProviderForImage(
            /* [in] */ CORDB_ADDRESS imageBaseAddress,
            /* [out] */ ICorDebugSymbolProvider **ppSymProvider) = 0;

        virtual HRESULT STDMETHODCALLTYPE EnumerateThreadIDs(
            /* [in] */ ULONG32 cThreadIds,
            /* [out] */ ULONG32 *pcThreadIds,
            /* [length_is][size_is][out] */ ULONG32 pThreadIds[  ]) = 0;

        virtual HRESULT STDMETHODCALLTYPE CreateVirtualUnwinder(
            /* [in] */ DWORD nativeThreadID,
            /* [in] */ ULONG32 contextFlags,
            /* [in] */ ULONG32 cbContext,
            /* [size_is][in] */ BYTE initialContext[  ],
            /* [out] */ ICorDebugVirtualUnwinder **ppUnwinder) = 0;

    };


#else 	/* C style interface */

    typedef struct ICorDebugDataTarget2Vtbl
    {
        BEGIN_INTERFACE

        HRESULT ( STDMETHODCALLTYPE *QueryInterface )(
            ICorDebugDataTarget2 * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */
            _COM_Outptr_  void **ppvObject);

        ULONG ( STDMETHODCALLTYPE *AddRef )(
            ICorDebugDataTarget2 * This);

        ULONG ( STDMETHODCALLTYPE *Release )(
            ICorDebugDataTarget2 * This);

        HRESULT ( STDMETHODCALLTYPE *GetImageFromPointer )(
            ICorDebugDataTarget2 * This,
            /* [in] */ CORDB_ADDRESS addr,
            /* [out] */ CORDB_ADDRESS *pImageBase,
            /* [out] */ ULONG32 *pSize);

        HRESULT ( STDMETHODCALLTYPE *GetImageLocation )(
            ICorDebugDataTarget2 * This,
            /* [in] */ CORDB_ADDRESS baseAddress,
            /* [in] */ ULONG32 cchName,
            /* [out] */ ULONG32 *pcchName,
            /* [length_is][size_is][out] */ WCHAR szName[  ]);

        HRESULT ( STDMETHODCALLTYPE *GetSymbolProviderForImage )(
            ICorDebugDataTarget2 * This,
            /* [in] */ CORDB_ADDRESS imageBaseAddress,
            /* [out] */ ICorDebugSymbolProvider **ppSymProvider);

        HRESULT ( STDMETHODCALLTYPE *EnumerateThreadIDs )(
            ICorDebugDataTarget2 * This,
            /* [in] */ ULONG32 cThreadIds,
            /* [out] */ ULONG32 *pcThreadIds,
            /* [length_is][size_is][out] */ ULONG32 pThreadIds[  ]);

        HRESULT ( STDMETHODCALLTYPE *CreateVirtualUnwinder )(
            ICorDebugDataTarget2 * This,
            /* [in] */ DWORD nativeThreadID,
            /* [in] */ ULONG32 contextFlags,
            /* [in] */ ULONG32 cbContext,
            /* [size_is][in] */ BYTE initialContext[  ],
            /* [out] */ ICorDebugVirtualUnwinder **ppUnwinder);

        END_INTERFACE
    } ICorDebugDataTarget2Vtbl;

    interface ICorDebugDataTarget2
    {
        CONST_VTBL struct ICorDebugDataTarget2Vtbl *lpVtbl;
    };



#ifdef COBJMACROS


#define ICorDebugDataTarget2_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) )

#define ICorDebugDataTarget2_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) )

#define ICorDebugDataTarget2_Release(This)	\
    ( (This)->lpVtbl -> Release(This) )


#define ICorDebugDataTarget2_GetImageFromPointer(This,addr,pImageBase,pSize)	\
    ( (This)->lpVtbl -> GetImageFromPointer(This,addr,pImageBase,pSize) )

#define ICorDebugDataTarget2_GetImageLocation(This,baseAddress,cchName,pcchName,szName)	\
    ( (This)->lpVtbl -> GetImageLocation(This,baseAddress,cchName,pcchName,szName) )

#define ICorDebugDataTarget2_GetSymbolProviderForImage(This,imageBaseAddress,ppSymProvider)	\
    ( (This)->lpVtbl -> GetSymbolProviderForImage(This,imageBaseAddress,ppSymProvider) )

#define ICorDebugDataTarget2_EnumerateThreadIDs(This,cThreadIds,pcThreadIds,pThreadIds)	\
    ( (This)->lpVtbl -> EnumerateThreadIDs(This,cThreadIds,pcThreadIds,pThreadIds) )

#define ICorDebugDataTarget2_CreateVirtualUnwinder(This,nativeThreadID,contextFlags,cbContext,initialContext,ppUnwinder)	\
    ( (This)->lpVtbl -> CreateVirtualUnwinder(This,nativeThreadID,contextFlags,cbContext,initialContext,ppUnwinder) )

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ICorDebugDataTarget2_INTERFACE_DEFINED__ */


#ifndef __ICorDebugLoadedModule_INTERFACE_DEFINED__
#define __ICorDebugLoadedModule_INTERFACE_DEFINED__

/* interface ICorDebugLoadedModule */
/* [unique][local][uuid][object] */


EXTERN_C const IID IID_ICorDebugLoadedModule;

#if defined(__cplusplus) && !defined(CINTERFACE)

    MIDL_INTERFACE("817F343A-6630-4578-96C5-D11BC0EC5EE2")
    ICorDebugLoadedModule : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE GetBaseAddress(
            /* [out] */ CORDB_ADDRESS *pAddress) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetName(
            /* [in] */ ULONG32 cchName,
            /* [out] */ ULONG32 *pcchName,
            /* [length_is][size_is][out] */ WCHAR szName[  ]) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetSize(
            /* [out] */ ULONG32 *pcBytes) = 0;

    };


#else 	/* C style interface */

    typedef struct ICorDebugLoadedModuleVtbl
    {
        BEGIN_INTERFACE

        HRESULT ( STDMETHODCALLTYPE *QueryInterface )(
            ICorDebugLoadedModule * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */
            _COM_Outptr_  void **ppvObject);

        ULONG ( STDMETHODCALLTYPE *AddRef )(
            ICorDebugLoadedModule * This);

        ULONG ( STDMETHODCALLTYPE *Release )(
            ICorDebugLoadedModule * This);

        HRESULT ( STDMETHODCALLTYPE *GetBaseAddress )(
            ICorDebugLoadedModule * This,
            /* [out] */ CORDB_ADDRESS *pAddress);

        HRESULT ( STDMETHODCALLTYPE *GetName )(
            ICorDebugLoadedModule * This,
            /* [in] */ ULONG32 cchName,
            /* [out] */ ULONG32 *pcchName,
            /* [length_is][size_is][out] */ WCHAR szName[  ]);

        HRESULT ( STDMETHODCALLTYPE *GetSize )(
            ICorDebugLoadedModule * This,
            /* [out] */ ULONG32 *pcBytes);

        END_INTERFACE
    } ICorDebugLoadedModuleVtbl;

    interface ICorDebugLoadedModule
    {
        CONST_VTBL struct ICorDebugLoadedModuleVtbl *lpVtbl;
    };



#ifdef COBJMACROS


#define ICorDebugLoadedModule_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) )

#define ICorDebugLoadedModule_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) )

#define ICorDebugLoadedModule_Release(This)	\
    ( (This)->lpVtbl -> Release(This) )


#define ICorDebugLoadedModule_GetBaseAddress(This,pAddress)	\
    ( (This)->lpVtbl -> GetBaseAddress(This,pAddress) )

#define ICorDebugLoadedModule_GetName(This,cchName,pcchName,szName)	\
    ( (This)->lpVtbl -> GetName(This,cchName,pcchName,szName) )

#define ICorDebugLoadedModule_GetSize(This,pcBytes)	\
    ( (This)->lpVtbl -> GetSize(This,pcBytes) )

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ICorDebugLoadedModule_INTERFACE_DEFINED__ */


#ifndef __ICorDebugDataTarget3_INTERFACE_DEFINED__
#define __ICorDebugDataTarget3_INTERFACE_DEFINED__

/* interface ICorDebugDataTarget3 */
/* [unique][local][uuid][object] */


EXTERN_C const IID IID_ICorDebugDataTarget3;

#if defined(__cplusplus) && !defined(CINTERFACE)

    MIDL_INTERFACE("D05E60C3-848C-4E7D-894E-623320FF6AFA")
    ICorDebugDataTarget3 : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE GetLoadedModules(
            /* [in] */ ULONG32 cRequestedModules,
            /* [out] */ ULONG32 *pcFetchedModules,
            /* [length_is][size_is][out] */ ICorDebugLoadedModule *pLoadedModules[  ]) = 0;

    };


#else 	/* C style interface */

    typedef struct ICorDebugDataTarget3Vtbl
    {
        BEGIN_INTERFACE

        HRESULT ( STDMETHODCALLTYPE *QueryInterface )(
            ICorDebugDataTarget3 * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */
            _COM_Outptr_  void **ppvObject);

        ULONG ( STDMETHODCALLTYPE *AddRef )(
            ICorDebugDataTarget3 * This);

        ULONG ( STDMETHODCALLTYPE *Release )(
            ICorDebugDataTarget3 * This);

        HRESULT ( STDMETHODCALLTYPE *GetLoadedModules )(
            ICorDebugDataTarget3 * This,
            /* [in] */ ULONG32 cRequestedModules,
            /* [out] */ ULONG32 *pcFetchedModules,
            /* [length_is][size_is][out] */ ICorDebugLoadedModule *pLoadedModules[  ]);

        END_INTERFACE
    } ICorDebugDataTarget3Vtbl;

    interface ICorDebugDataTarget3
    {
        CONST_VTBL struct ICorDebugDataTarget3Vtbl *lpVtbl;
    };



#ifdef COBJMACROS


#define ICorDebugDataTarget3_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) )

#define ICorDebugDataTarget3_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) )

#define ICorDebugDataTarget3_Release(This)	\
    ( (This)->lpVtbl -> Release(This) )


#define ICorDebugDataTarget3_GetLoadedModules(This,cRequestedModules,pcFetchedModules,pLoadedModules)	\
    ( (This)->lpVtbl -> GetLoadedModules(This,cRequestedModules,pcFetchedModules,pLoadedModules) )

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ICorDebugDataTarget3_INTERFACE_DEFINED__ */


#ifndef __ICorDebugDataTarget4_INTERFACE_DEFINED__
#define __ICorDebugDataTarget4_INTERFACE_DEFINED__

/* interface ICorDebugDataTarget4 */
/* [unique][local][uuid][object] */


EXTERN_C const IID IID_ICorDebugDataTarget4;

#if defined(__cplusplus) && !defined(CINTERFACE)

    MIDL_INTERFACE("E799DC06-E099-4713-BDD9-906D3CC02CF2")
    ICorDebugDataTarget4 : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE VirtualUnwind(
            /* [in] */ DWORD threadId,
            /* [in] */ ULONG32 contextSize,
            /* [size_is][out][in] */ BYTE *context) = 0;

    };


#else 	/* C style interface */

    typedef struct ICorDebugDataTarget4Vtbl
    {
        BEGIN_INTERFACE

        HRESULT ( STDMETHODCALLTYPE *QueryInterface )(
            ICorDebugDataTarget4 * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */
            _COM_Outptr_  void **ppvObject);

        ULONG ( STDMETHODCALLTYPE *AddRef )(
            ICorDebugDataTarget4 * This);

        ULONG ( STDMETHODCALLTYPE *Release )(
            ICorDebugDataTarget4 * This);

        HRESULT ( STDMETHODCALLTYPE *VirtualUnwind )(
            ICorDebugDataTarget4 * This,
            /* [in] */ DWORD threadId,
            /* [in] */ ULONG32 contextSize,
            /* [size_is][out][in] */ BYTE *context);

        END_INTERFACE
    } ICorDebugDataTarget4Vtbl;

    interface ICorDebugDataTarget4
    {
        CONST_VTBL struct ICorDebugDataTarget4Vtbl *lpVtbl;
    };



#ifdef COBJMACROS


#define ICorDebugDataTarget4_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) )

#define ICorDebugDataTarget4_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) )

#define ICorDebugDataTarget4_Release(This)	\
    ( (This)->lpVtbl -> Release(This) )


#define ICorDebugDataTarget4_VirtualUnwind(This,threadId,contextSize,context)	\
    ( (This)->lpVtbl -> VirtualUnwind(This,threadId,contextSize,context) )

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ICorDebugDataTarget4_INTERFACE_DEFINED__ */


#ifndef __ICorDebugMutableDataTarget_INTERFACE_DEFINED__
#define __ICorDebugMutableDataTarget_INTERFACE_DEFINED__

/* interface ICorDebugMutableDataTarget */
/* [unique][local][uuid][object] */


EXTERN_C const IID IID_ICorDebugMutableDataTarget;

#if defined(__cplusplus) && !defined(CINTERFACE)

    MIDL_INTERFACE("A1B8A756-3CB6-4CCB-979F-3DF999673A59")
    ICorDebugMutableDataTarget : public ICorDebugDataTarget
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE WriteVirtual(
            /* [in] */ CORDB_ADDRESS address,
            /* [size_is][in] */ const BYTE *pBuffer,
            /* [in] */ ULONG32 bytesRequested) = 0;

        virtual HRESULT STDMETHODCALLTYPE SetThreadContext(
            /* [in] */ DWORD dwThreadID,
            /* [in] */ ULONG32 contextSize,
            /* [size_is][in] */ const BYTE *pContext) = 0;

        virtual HRESULT STDMETHODCALLTYPE ContinueStatusChanged(
            /* [in] */ DWORD dwThreadId,
            /* [in] */ CORDB_CONTINUE_STATUS continueStatus) = 0;

    };


#else 	/* C style interface */

    typedef struct ICorDebugMutableDataTargetVtbl
    {
        BEGIN_INTERFACE

        HRESULT ( STDMETHODCALLTYPE *QueryInterface )(
            ICorDebugMutableDataTarget * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */
            _COM_Outptr_  void **ppvObject);

        ULONG ( STDMETHODCALLTYPE *AddRef )(
            ICorDebugMutableDataTarget * This);

        ULONG ( STDMETHODCALLTYPE *Release )(
            ICorDebugMutableDataTarget * This);

        HRESULT ( STDMETHODCALLTYPE *GetPlatform )(
            ICorDebugMutableDataTarget * This,
            /* [out] */ CorDebugPlatform *pTargetPlatform);

        HRESULT ( STDMETHODCALLTYPE *ReadVirtual )(
            ICorDebugMutableDataTarget * This,
            /* [in] */ CORDB_ADDRESS address,
            /* [length_is][size_is][out] */ BYTE *pBuffer,
            /* [in] */ ULONG32 bytesRequested,
            /* [out] */ ULONG32 *pBytesRead);

        HRESULT ( STDMETHODCALLTYPE *GetThreadContext )(
            ICorDebugMutableDataTarget * This,
            /* [in] */ DWORD dwThreadID,
            /* [in] */ ULONG32 contextFlags,
            /* [in] */ ULONG32 contextSize,
            /* [size_is][out] */ BYTE *pContext);

        HRESULT ( STDMETHODCALLTYPE *WriteVirtual )(
            ICorDebugMutableDataTarget * This,
            /* [in] */ CORDB_ADDRESS address,
            /* [size_is][in] */ const BYTE *pBuffer,
            /* [in] */ ULONG32 bytesRequested);

        HRESULT ( STDMETHODCALLTYPE *SetThreadContext )(
            ICorDebugMutableDataTarget * This,
            /* [in] */ DWORD dwThreadID,
            /* [in] */ ULONG32 contextSize,
            /* [size_is][in] */ const BYTE *pContext);

        HRESULT ( STDMETHODCALLTYPE *ContinueStatusChanged )(
            ICorDebugMutableDataTarget * This,
            /* [in] */ DWORD dwThreadId,
            /* [in] */ CORDB_CONTINUE_STATUS continueStatus);

        END_INTERFACE
    } ICorDebugMutableDataTargetVtbl;

    interface ICorDebugMutableDataTarget
    {
        CONST_VTBL struct ICorDebugMutableDataTargetVtbl *lpVtbl;
    };



#ifdef COBJMACROS


#define ICorDebugMutableDataTarget_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) )

#define ICorDebugMutableDataTarget_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) )

#define ICorDebugMutableDataTarget_Release(This)	\
    ( (This)->lpVtbl -> Release(This) )


#define ICorDebugMutableDataTarget_GetPlatform(This,pTargetPlatform)	\
    ( (This)->lpVtbl -> GetPlatform(This,pTargetPlatform) )

#define ICorDebugMutableDataTarget_ReadVirtual(This,address,pBuffer,bytesRequested,pBytesRead)	\
    ( (This)->lpVtbl -> ReadVirtual(This,address,pBuffer,bytesRequested,pBytesRead) )

#define ICorDebugMutableDataTarget_GetThreadContext(This,dwThreadID,contextFlags,contextSize,pContext)	\
    ( (This)->lpVtbl -> GetThreadContext(This,dwThreadID,contextFlags,contextSize,pContext) )


#define ICorDebugMutableDataTarget_WriteVirtual(This,address,pBuffer,bytesRequested)	\
    ( (This)->lpVtbl -> WriteVirtual(This,address,pBuffer,bytesRequested) )

#define ICorDebugMutableDataTarget_SetThreadContext(This,dwThreadID,contextSize,pContext)	\
    ( (This)->lpVtbl -> SetThreadContext(This,dwThreadID,contextSize,pContext) )

#define ICorDebugMutableDataTarget_ContinueStatusChanged(This,dwThreadId,continueStatus)	\
    ( (This)->lpVtbl -> ContinueStatusChanged(This,dwThreadId,continueStatus) )

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ICorDebugMutableDataTarget_INTERFACE_DEFINED__ */


#ifndef __ICorDebugMetaDataLocator_INTERFACE_DEFINED__
#define __ICorDebugMetaDataLocator_INTERFACE_DEFINED__

/* interface ICorDebugMetaDataLocator */
/* [unique][uuid][local][object] */


EXTERN_C const IID IID_ICorDebugMetaDataLocator;

#if defined(__cplusplus) && !defined(CINTERFACE)

    MIDL_INTERFACE("7cef8ba9-2ef7-42bf-973f-4171474f87d9")
    ICorDebugMetaDataLocator : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE GetMetaData(
            /* [in] */ LPCWSTR wszImagePath,
            /* [in] */ DWORD dwImageTimeStamp,
            /* [in] */ DWORD dwImageSize,
            /* [in] */ ULONG32 cchPathBuffer,
            /* [annotation][out] */
            _Out_  ULONG32 *pcchPathBuffer,
            /* [annotation][length_is][size_is][out] */
            _Out_writes_to_(cchPathBuffer, *pcchPathBuffer)   WCHAR wszPathBuffer[  ]) = 0;

    };


#else 	/* C style interface */

    typedef struct ICorDebugMetaDataLocatorVtbl
    {
        BEGIN_INTERFACE

        HRESULT ( STDMETHODCALLTYPE *QueryInterface )(
            ICorDebugMetaDataLocator * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */
            _COM_Outptr_  void **ppvObject);

        ULONG ( STDMETHODCALLTYPE *AddRef )(
            ICorDebugMetaDataLocator * This);

        ULONG ( STDMETHODCALLTYPE *Release )(
            ICorDebugMetaDataLocator * This);

        HRESULT ( STDMETHODCALLTYPE *GetMetaData )(
            ICorDebugMetaDataLocator * This,
            /* [in] */ LPCWSTR wszImagePath,
            /* [in] */ DWORD dwImageTimeStamp,
            /* [in] */ DWORD dwImageSize,
            /* [in] */ ULONG32 cchPathBuffer,
            /* [annotation][out] */
            _Out_  ULONG32 *pcchPathBuffer,
            /* [annotation][length_is][size_is][out] */
            _Out_writes_to_(cchPathBuffer, *pcchPathBuffer)   WCHAR wszPathBuffer[  ]);

        END_INTERFACE
    } ICorDebugMetaDataLocatorVtbl;

    interface ICorDebugMetaDataLocator
    {
        CONST_VTBL struct ICorDebugMetaDataLocatorVtbl *lpVtbl;
    };



#ifdef COBJMACROS


#define ICorDebugMetaDataLocator_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) )

#define ICorDebugMetaDataLocator_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) )

#define ICorDebugMetaDataLocator_Release(This)	\
    ( (This)->lpVtbl -> Release(This) )


#define ICorDebugMetaDataLocator_GetMetaData(This,wszImagePath,dwImageTimeStamp,dwImageSize,cchPathBuffer,pcchPathBuffer,wszPathBuffer)	\
    ( (This)->lpVtbl -> GetMetaData(This,wszImagePath,dwImageTimeStamp,dwImageSize,cchPathBuffer,pcchPathBuffer,wszPathBuffer) )

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ICorDebugMetaDataLocator_INTERFACE_DEFINED__ */


/* interface __MIDL_itf_cordebug_0000_0015 */
/* [local] */

#pragma warning(push)
#pragma warning(disable:28718)


extern RPC_IF_HANDLE __MIDL_itf_cordebug_0000_0015_v0_0_c_ifspec;
extern RPC_IF_HANDLE __MIDL_itf_cordebug_0000_0015_v0_0_s_ifspec;

#ifndef __ICorDebugManagedCallback_INTERFACE_DEFINED__
#define __ICorDebugManagedCallback_INTERFACE_DEFINED__

/* interface ICorDebugManagedCallback */
/* [unique][uuid][local][object] */

typedef
enum CorDebugStepReason
    {
        STEP_NORMAL	= 0,
        STEP_RETURN	= ( STEP_NORMAL + 1 ) ,
        STEP_CALL	= ( STEP_RETURN + 1 ) ,
        STEP_EXCEPTION_FILTER	= ( STEP_CALL + 1 ) ,
        STEP_EXCEPTION_HANDLER	= ( STEP_EXCEPTION_FILTER + 1 ) ,
        STEP_INTERCEPT	= ( STEP_EXCEPTION_HANDLER + 1 ) ,
        STEP_EXIT	= ( STEP_INTERCEPT + 1 )
    } 	CorDebugStepReason;

typedef
enum LoggingLevelEnum
    {
        LTraceLevel0	= 0,
        LTraceLevel1	= ( LTraceLevel0 + 1 ) ,
        LTraceLevel2	= ( LTraceLevel1 + 1 ) ,
        LTraceLevel3	= ( LTraceLevel2 + 1 ) ,
        LTraceLevel4	= ( LTraceLevel3 + 1 ) ,
        LStatusLevel0	= 20,
        LStatusLevel1	= ( LStatusLevel0 + 1 ) ,
        LStatusLevel2	= ( LStatusLevel1 + 1 ) ,
        LStatusLevel3	= ( LStatusLevel2 + 1 ) ,
        LStatusLevel4	= ( LStatusLevel3 + 1 ) ,
        LWarningLevel	= 40,
        LErrorLevel	= 50,
        LPanicLevel	= 100
    } 	LoggingLevelEnum;

typedef
enum LogSwitchCallReason
    {
        SWITCH_CREATE	= 0,
        SWITCH_MODIFY	= ( SWITCH_CREATE + 1 ) ,
        SWITCH_DELETE	= ( SWITCH_MODIFY + 1 )
    } 	LogSwitchCallReason;


EXTERN_C const IID IID_ICorDebugManagedCallback;

#if defined(__cplusplus) && !defined(CINTERFACE)

    MIDL_INTERFACE("3d6f5f60-7538-11d3-8d5b-00104b35e7ef")
    ICorDebugManagedCallback : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE Breakpoint(
            /* [in] */ ICorDebugAppDomain *pAppDomain,
            /* [in] */ ICorDebugThread *pThread,
            /* [in] */ ICorDebugBreakpoint *pBreakpoint) = 0;

        virtual HRESULT STDMETHODCALLTYPE StepComplete(
            /* [in] */ ICorDebugAppDomain *pAppDomain,
            /* [in] */ ICorDebugThread *pThread,
            /* [in] */ ICorDebugStepper *pStepper,
            /* [in] */ CorDebugStepReason reason) = 0;

        virtual HRESULT STDMETHODCALLTYPE Break(
            /* [in] */ ICorDebugAppDomain *pAppDomain,
            /* [in] */ ICorDebugThread *thread) = 0;

        virtual HRESULT STDMETHODCALLTYPE Exception(
            /* [in] */ ICorDebugAppDomain *pAppDomain,
            /* [in] */ ICorDebugThread *pThread,
            /* [in] */ BOOL unhandled) = 0;

        virtual HRESULT STDMETHODCALLTYPE EvalComplete(
            /* [in] */ ICorDebugAppDomain *pAppDomain,
            /* [in] */ ICorDebugThread *pThread,
            /* [in] */ ICorDebugEval *pEval) = 0;

        virtual HRESULT STDMETHODCALLTYPE EvalException(
            /* [in] */ ICorDebugAppDomain *pAppDomain,
            /* [in] */ ICorDebugThread *pThread,
            /* [in] */ ICorDebugEval *pEval) = 0;

        virtual HRESULT STDMETHODCALLTYPE CreateProcess(
            /* [in] */ ICorDebugProcess *pProcess) = 0;

        virtual HRESULT STDMETHODCALLTYPE ExitProcess(
            /* [in] */ ICorDebugProcess *pProcess) = 0;

        virtual HRESULT STDMETHODCALLTYPE CreateThread(
            /* [in] */ ICorDebugAppDomain *pAppDomain,
            /* [in] */ ICorDebugThread *thread) = 0;

        virtual HRESULT STDMETHODCALLTYPE ExitThread(
            /* [in] */ ICorDebugAppDomain *pAppDomain,
            /* [in] */ ICorDebugThread *thread) = 0;

        virtual HRESULT STDMETHODCALLTYPE LoadModule(
            /* [in] */ ICorDebugAppDomain *pAppDomain,
            /* [in] */ ICorDebugModule *pModule) = 0;

        virtual HRESULT STDMETHODCALLTYPE UnloadModule(
            /* [in] */ ICorDebugAppDomain *pAppDomain,
            /* [in] */ ICorDebugModule *pModule) = 0;

        virtual HRESULT STDMETHODCALLTYPE LoadClass(
            /* [in] */ ICorDebugAppDomain *pAppDomain,
            /* [in] */ ICorDebugClass *c) = 0;

        virtual HRESULT STDMETHODCALLTYPE UnloadClass(
            /* [in] */ ICorDebugAppDomain *pAppDomain,
            /* [in] */ ICorDebugClass *c) = 0;

        virtual HRESULT STDMETHODCALLTYPE DebuggerError(
            /* [in] */ ICorDebugProcess *pProcess,
            /* [in] */ HRESULT errorHR,
            /* [in] */ DWORD errorCode) = 0;

        virtual HRESULT STDMETHODCALLTYPE LogMessage(
            /* [in] */ ICorDebugAppDomain *pAppDomain,
            /* [in] */ ICorDebugThread *pThread,
            /* [in] */ LONG lLevel,
            /* [in] */ WCHAR *pLogSwitchName,
            /* [in] */ WCHAR *pMessage) = 0;

        virtual HRESULT STDMETHODCALLTYPE LogSwitch(
            /* [in] */ ICorDebugAppDomain *pAppDomain,
            /* [in] */ ICorDebugThread *pThread,
            /* [in] */ LONG lLevel,
            /* [in] */ ULONG ulReason,
            /* [in] */ WCHAR *pLogSwitchName,
            /* [in] */ WCHAR *pParentName) = 0;

        virtual HRESULT STDMETHODCALLTYPE CreateAppDomain(
            /* [in] */ ICorDebugProcess *pProcess,
            /* [in] */ ICorDebugAppDomain *pAppDomain) = 0;

        virtual HRESULT STDMETHODCALLTYPE ExitAppDomain(
            /* [in] */ ICorDebugProcess *pProcess,
            /* [in] */ ICorDebugAppDomain *pAppDomain) = 0;

        virtual HRESULT STDMETHODCALLTYPE LoadAssembly(
            /* [in] */ ICorDebugAppDomain *pAppDomain,
            /* [in] */ ICorDebugAssembly *pAssembly) = 0;

        virtual HRESULT STDMETHODCALLTYPE UnloadAssembly(
            /* [in] */ ICorDebugAppDomain *pAppDomain,
            /* [in] */ ICorDebugAssembly *pAssembly) = 0;

        virtual HRESULT STDMETHODCALLTYPE ControlCTrap(
            /* [in] */ ICorDebugProcess *pProcess) = 0;

        virtual HRESULT STDMETHODCALLTYPE NameChange(
            /* [in] */ ICorDebugAppDomain *pAppDomain,
            /* [in] */ ICorDebugThread *pThread) = 0;

        virtual HRESULT STDMETHODCALLTYPE UpdateModuleSymbols(
            /* [in] */ ICorDebugAppDomain *pAppDomain,
            /* [in] */ ICorDebugModule *pModule,
            /* [in] */ IStream *pSymbolStream) = 0;

        virtual HRESULT STDMETHODCALLTYPE EditAndContinueRemap(
            /* [in] */ ICorDebugAppDomain *pAppDomain,
            /* [in] */ ICorDebugThread *pThread,
            /* [in] */ ICorDebugFunction *pFunction,
            /* [in] */ BOOL fAccurate) = 0;

        virtual HRESULT STDMETHODCALLTYPE BreakpointSetError(
            /* [in] */ ICorDebugAppDomain *pAppDomain,
            /* [in] */ ICorDebugThread *pThread,
            /* [in] */ ICorDebugBreakpoint *pBreakpoint,
            /* [in] */ DWORD dwError) = 0;

    };


#else 	/* C style interface */

    typedef struct ICorDebugManagedCallbackVtbl
    {
        BEGIN_INTERFACE

        HRESULT ( STDMETHODCALLTYPE *QueryInterface )(
            ICorDebugManagedCallback * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */
            _COM_Outptr_  void **ppvObject);

        ULONG ( STDMETHODCALLTYPE *AddRef )(
            ICorDebugManagedCallback * This);

        ULONG ( STDMETHODCALLTYPE *Release )(
            ICorDebugManagedCallback * This);

        HRESULT ( STDMETHODCALLTYPE *Breakpoint )(
            ICorDebugManagedCallback * This,
            /* [in] */ ICorDebugAppDomain *pAppDomain,
            /* [in] */ ICorDebugThread *pThread,
            /* [in] */ ICorDebugBreakpoint *pBreakpoint);

        HRESULT ( STDMETHODCALLTYPE *StepComplete )(
            ICorDebugManagedCallback * This,
            /* [in] */ ICorDebugAppDomain *pAppDomain,
            /* [in] */ ICorDebugThread *pThread,
            /* [in] */ ICorDebugStepper *pStepper,
            /* [in] */ CorDebugStepReason reason);

        HRESULT ( STDMETHODCALLTYPE *Break )(
            ICorDebugManagedCallback * This,
            /* [in] */ ICorDebugAppDomain *pAppDomain,
            /* [in] */ ICorDebugThread *thread);

        HRESULT ( STDMETHODCALLTYPE *Exception )(
            ICorDebugManagedCallback * This,
            /* [in] */ ICorDebugAppDomain *pAppDomain,
            /* [in] */ ICorDebugThread *pThread,
            /* [in] */ BOOL unhandled);

        HRESULT ( STDMETHODCALLTYPE *EvalComplete )(
            ICorDebugManagedCallback * This,
            /* [in] */ ICorDebugAppDomain *pAppDomain,
            /* [in] */ ICorDebugThread *pThread,
            /* [in] */ ICorDebugEval *pEval);

        HRESULT ( STDMETHODCALLTYPE *EvalException )(
            ICorDebugManagedCallback * This,
            /* [in] */ ICorDebugAppDomain *pAppDomain,
            /* [in] */ ICorDebugThread *pThread,
            /* [in] */ ICorDebugEval *pEval);

        HRESULT ( STDMETHODCALLTYPE *CreateProcess )(
            ICorDebugManagedCallback * This,
            /* [in] */ ICorDebugProcess *pProcess);

        HRESULT ( STDMETHODCALLTYPE *ExitProcess )(
            ICorDebugManagedCallback * This,
            /* [in] */ ICorDebugProcess *pProcess);

        HRESULT ( STDMETHODCALLTYPE *CreateThread )(
            ICorDebugManagedCallback * This,
            /* [in] */ ICorDebugAppDomain *pAppDomain,
            /* [in] */ ICorDebugThread *thread);

        HRESULT ( STDMETHODCALLTYPE *ExitThread )(
            ICorDebugManagedCallback * This,
            /* [in] */ ICorDebugAppDomain *pAppDomain,
            /* [in] */ ICorDebugThread *thread);

        HRESULT ( STDMETHODCALLTYPE *LoadModule )(
            ICorDebugManagedCallback * This,
            /* [in] */ ICorDebugAppDomain *pAppDomain,
            /* [in] */ ICorDebugModule *pModule);

        HRESULT ( STDMETHODCALLTYPE *UnloadModule )(
            ICorDebugManagedCallback * This,
            /* [in] */ ICorDebugAppDomain *pAppDomain,
            /* [in] */ ICorDebugModule *pModule);

        HRESULT ( STDMETHODCALLTYPE *LoadClass )(
            ICorDebugManagedCallback * This,
            /* [in] */ ICorDebugAppDomain *pAppDomain,
            /* [in] */ ICorDebugClass *c);

        HRESULT ( STDMETHODCALLTYPE *UnloadClass )(
            ICorDebugManagedCallback * This,
            /* [in] */ ICorDebugAppDomain *pAppDomain,
            /* [in] */ ICorDebugClass *c);

        HRESULT ( STDMETHODCALLTYPE *DebuggerError )(
            ICorDebugManagedCallback * This,
            /* [in] */ ICorDebugProcess *pProcess,
            /* [in] */ HRESULT errorHR,
            /* [in] */ DWORD errorCode);

        HRESULT ( STDMETHODCALLTYPE *LogMessage )(
            ICorDebugManagedCallback * This,
            /* [in] */ ICorDebugAppDomain *pAppDomain,
            /* [in] */ ICorDebugThread *pThread,
            /* [in] */ LONG lLevel,
            /* [in] */ WCHAR *pLogSwitchName,
            /* [in] */ WCHAR *pMessage);

        HRESULT ( STDMETHODCALLTYPE *LogSwitch )(
            ICorDebugManagedCallback * This,
            /* [in] */ ICorDebugAppDomain *pAppDomain,
            /* [in] */ ICorDebugThread *pThread,
            /* [in] */ LONG lLevel,
            /* [in] */ ULONG ulReason,
            /* [in] */ WCHAR *pLogSwitchName,
            /* [in] */ WCHAR *pParentName);

        HRESULT ( STDMETHODCALLTYPE *CreateAppDomain )(
            ICorDebugManagedCallback * This,
            /* [in] */ ICorDebugProcess *pProcess,
            /* [in] */ ICorDebugAppDomain *pAppDomain);

        HRESULT ( STDMETHODCALLTYPE *ExitAppDomain )(
            ICorDebugManagedCallback * This,
            /* [in] */ ICorDebugProcess *pProcess,
            /* [in] */ ICorDebugAppDomain *pAppDomain);

        HRESULT ( STDMETHODCALLTYPE *LoadAssembly )(
            ICorDebugManagedCallback * This,
            /* [in] */ ICorDebugAppDomain *pAppDomain,
            /* [in] */ ICorDebugAssembly *pAssembly);

        HRESULT ( STDMETHODCALLTYPE *UnloadAssembly )(
            ICorDebugManagedCallback * This,
            /* [in] */ ICorDebugAppDomain *pAppDomain,
            /* [in] */ ICorDebugAssembly *pAssembly);

        HRESULT ( STDMETHODCALLTYPE *ControlCTrap )(
            ICorDebugManagedCallback * This,
            /* [in] */ ICorDebugProcess *pProcess);

        HRESULT ( STDMETHODCALLTYPE *NameChange )(
            ICorDebugManagedCallback * This,
            /* [in] */ ICorDebugAppDomain *pAppDomain,
            /* [in] */ ICorDebugThread *pThread);

        HRESULT ( STDMETHODCALLTYPE *UpdateModuleSymbols )(
            ICorDebugManagedCallback * This,
            /* [in] */ ICorDebugAppDomain *pAppDomain,
            /* [in] */ ICorDebugModule *pModule,
            /* [in] */ IStream *pSymbolStream);

        HRESULT ( STDMETHODCALLTYPE *EditAndContinueRemap )(
            ICorDebugManagedCallback * This,
            /* [in] */ ICorDebugAppDomain *pAppDomain,
            /* [in] */ ICorDebugThread *pThread,
            /* [in] */ ICorDebugFunction *pFunction,
            /* [in] */ BOOL fAccurate);

        HRESULT ( STDMETHODCALLTYPE *BreakpointSetError )(
            ICorDebugManagedCallback * This,
            /* [in] */ ICorDebugAppDomain *pAppDomain,
            /* [in] */ ICorDebugThread *pThread,
            /* [in] */ ICorDebugBreakpoint *pBreakpoint,
            /* [in] */ DWORD dwError);

        END_INTERFACE
    } ICorDebugManagedCallbackVtbl;

    interface ICorDebugManagedCallback
    {
        CONST_VTBL struct ICorDebugManagedCallbackVtbl *lpVtbl;
    };



#ifdef COBJMACROS


#define ICorDebugManagedCallback_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) )

#define ICorDebugManagedCallback_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) )

#define ICorDebugManagedCallback_Release(This)	\
    ( (This)->lpVtbl -> Release(This) )


#define ICorDebugManagedCallback_Breakpoint(This,pAppDomain,pThread,pBreakpoint)	\
    ( (This)->lpVtbl -> Breakpoint(This,pAppDomain,pThread,pBreakpoint) )

#define ICorDebugManagedCallback_StepComplete(This,pAppDomain,pThread,pStepper,reason)	\
    ( (This)->lpVtbl -> StepComplete(This,pAppDomain,pThread,pStepper,reason) )

#define ICorDebugManagedCallback_Break(This,pAppDomain,thread)	\
    ( (This)->lpVtbl -> Break(This,pAppDomain,thread) )

#define ICorDebugManagedCallback_Exception(This,pAppDomain,pThread,unhandled)	\
    ( (This)->lpVtbl -> Exception(This,pAppDomain,pThread,unhandled) )

#define ICorDebugManagedCallback_EvalComplete(This,pAppDomain,pThread,pEval)	\
    ( (This)->lpVtbl -> EvalComplete(This,pAppDomain,pThread,pEval) )

#define ICorDebugManagedCallback_EvalException(This,pAppDomain,pThread,pEval)	\
    ( (This)->lpVtbl -> EvalException(This,pAppDomain,pThread,pEval) )

#define ICorDebugManagedCallback_CreateProcess(This,pProcess)	\
    ( (This)->lpVtbl -> CreateProcess(This,pProcess) )

#define ICorDebugManagedCallback_ExitProcess(This,pProcess)	\
    ( (This)->lpVtbl -> ExitProcess(This,pProcess) )

#define ICorDebugManagedCallback_CreateThread(This,pAppDomain,thread)	\
    ( (This)->lpVtbl -> CreateThread(This,pAppDomain,thread) )

#define ICorDebugManagedCallback_ExitThread(This,pAppDomain,thread)	\
    ( (This)->lpVtbl -> ExitThread(This,pAppDomain,thread) )

#define ICorDebugManagedCallback_LoadModule(This,pAppDomain,pModule)	\
    ( (This)->lpVtbl -> LoadModule(This,pAppDomain,pModule) )

#define ICorDebugManagedCallback_UnloadModule(This,pAppDomain,pModule)	\
    ( (This)->lpVtbl -> UnloadModule(This,pAppDomain,pModule) )

#define ICorDebugManagedCallback_LoadClass(This,pAppDomain,c)	\
    ( (This)->lpVtbl -> LoadClass(This,pAppDomain,c) )

#define ICorDebugManagedCallback_UnloadClass(This,pAppDomain,c)	\
    ( (This)->lpVtbl -> UnloadClass(This,pAppDomain,c) )

#define ICorDebugManagedCallback_DebuggerError(This,pProcess,errorHR,errorCode)	\
    ( (This)->lpVtbl -> DebuggerError(This,pProcess,errorHR,errorCode) )

#define ICorDebugManagedCallback_LogMessage(This,pAppDomain,pThread,lLevel,pLogSwitchName,pMessage)	\
    ( (This)->lpVtbl -> LogMessage(This,pAppDomain,pThread,lLevel,pLogSwitchName,pMessage) )

#define ICorDebugManagedCallback_LogSwitch(This,pAppDomain,pThread,lLevel,ulReason,pLogSwitchName,pParentName)	\
    ( (This)->lpVtbl -> LogSwitch(This,pAppDomain,pThread,lLevel,ulReason,pLogSwitchName,pParentName) )

#define ICorDebugManagedCallback_CreateAppDomain(This,pProcess,pAppDomain)	\
    ( (This)->lpVtbl -> CreateAppDomain(This,pProcess,pAppDomain) )

#define ICorDebugManagedCallback_ExitAppDomain(This,pProcess,pAppDomain)	\
    ( (This)->lpVtbl -> ExitAppDomain(This,pProcess,pAppDomain) )

#define ICorDebugManagedCallback_LoadAssembly(This,pAppDomain,pAssembly)	\
    ( (This)->lpVtbl -> LoadAssembly(This,pAppDomain,pAssembly) )

#define ICorDebugManagedCallback_UnloadAssembly(This,pAppDomain,pAssembly)	\
    ( (This)->lpVtbl -> UnloadAssembly(This,pAppDomain,pAssembly) )

#define ICorDebugManagedCallback_ControlCTrap(This,pProcess)	\
    ( (This)->lpVtbl -> ControlCTrap(This,pProcess) )

#define ICorDebugManagedCallback_NameChange(This,pAppDomain,pThread)	\
    ( (This)->lpVtbl -> NameChange(This,pAppDomain,pThread) )

#define ICorDebugManagedCallback_UpdateModuleSymbols(This,pAppDomain,pModule,pSymbolStream)	\
    ( (This)->lpVtbl -> UpdateModuleSymbols(This,pAppDomain,pModule,pSymbolStream) )

#define ICorDebugManagedCallback_EditAndContinueRemap(This,pAppDomain,pThread,pFunction,fAccurate)	\
    ( (This)->lpVtbl -> EditAndContinueRemap(This,pAppDomain,pThread,pFunction,fAccurate) )

#define ICorDebugManagedCallback_BreakpointSetError(This,pAppDomain,pThread,pBreakpoint,dwError)	\
    ( (This)->lpVtbl -> BreakpointSetError(This,pAppDomain,pThread,pBreakpoint,dwError) )

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ICorDebugManagedCallback_INTERFACE_DEFINED__ */


/* interface __MIDL_itf_cordebug_0000_0016 */
/* [local] */

#pragma warning(pop)
#pragma warning(push)


extern RPC_IF_HANDLE __MIDL_itf_cordebug_0000_0016_v0_0_c_ifspec;
extern RPC_IF_HANDLE __MIDL_itf_cordebug_0000_0016_v0_0_s_ifspec;

#ifndef __ICorDebugManagedCallback3_INTERFACE_DEFINED__
#define __ICorDebugManagedCallback3_INTERFACE_DEFINED__

/* interface ICorDebugManagedCallback3 */
/* [unique][uuid][local][object] */


EXTERN_C const IID IID_ICorDebugManagedCallback3;

#if defined(__cplusplus) && !defined(CINTERFACE)

    MIDL_INTERFACE("264EA0FC-2591-49AA-868E-835E6515323F")
    ICorDebugManagedCallback3 : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE CustomNotification(
            /* [in] */ ICorDebugThread *pThread,
            /* [in] */ ICorDebugAppDomain *pAppDomain) = 0;

    };


#else 	/* C style interface */

    typedef struct ICorDebugManagedCallback3Vtbl
    {
        BEGIN_INTERFACE

        HRESULT ( STDMETHODCALLTYPE *QueryInterface )(
            ICorDebugManagedCallback3 * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */
            _COM_Outptr_  void **ppvObject);

        ULONG ( STDMETHODCALLTYPE *AddRef )(
            ICorDebugManagedCallback3 * This);

        ULONG ( STDMETHODCALLTYPE *Release )(
            ICorDebugManagedCallback3 * This);

        HRESULT ( STDMETHODCALLTYPE *CustomNotification )(
            ICorDebugManagedCallback3 * This,
            /* [in] */ ICorDebugThread *pThread,
            /* [in] */ ICorDebugAppDomain *pAppDomain);

        END_INTERFACE
    } ICorDebugManagedCallback3Vtbl;

    interface ICorDebugManagedCallback3
    {
        CONST_VTBL struct ICorDebugManagedCallback3Vtbl *lpVtbl;
    };



#ifdef COBJMACROS


#define ICorDebugManagedCallback3_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) )

#define ICorDebugManagedCallback3_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) )

#define ICorDebugManagedCallback3_Release(This)	\
    ( (This)->lpVtbl -> Release(This) )


#define ICorDebugManagedCallback3_CustomNotification(This,pThread,pAppDomain)	\
    ( (This)->lpVtbl -> CustomNotification(This,pThread,pAppDomain) )

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ICorDebugManagedCallback3_INTERFACE_DEFINED__ */


#ifndef __ICorDebugManagedCallback4_INTERFACE_DEFINED__
#define __ICorDebugManagedCallback4_INTERFACE_DEFINED__

/* interface ICorDebugManagedCallback4 */
/* [unique][uuid][local][object] */


EXTERN_C const IID IID_ICorDebugManagedCallback4;

#if defined(__cplusplus) && !defined(CINTERFACE)

    MIDL_INTERFACE("322911AE-16A5-49BA-84A3-ED69678138A3")
    ICorDebugManagedCallback4 : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE BeforeGarbageCollection(
            /* [in] */ ICorDebugProcess *pProcess) = 0;

        virtual HRESULT STDMETHODCALLTYPE AfterGarbageCollection(
            /* [in] */ ICorDebugProcess *pProcess) = 0;

        virtual HRESULT STDMETHODCALLTYPE DataBreakpoint(
            /* [in] */ ICorDebugProcess *pProcess,
            /* [in] */ ICorDebugThread *pThread,
            /* [in] */ BYTE *pContext,
            /* [in] */ ULONG32 contextSize) = 0;

    };


#else 	/* C style interface */

    typedef struct ICorDebugManagedCallback4Vtbl
    {
        BEGIN_INTERFACE

        HRESULT ( STDMETHODCALLTYPE *QueryInterface )(
            ICorDebugManagedCallback4 * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */
            _COM_Outptr_  void **ppvObject);

        ULONG ( STDMETHODCALLTYPE *AddRef )(
            ICorDebugManagedCallback4 * This);

        ULONG ( STDMETHODCALLTYPE *Release )(
            ICorDebugManagedCallback4 * This);

        HRESULT ( STDMETHODCALLTYPE *BeforeGarbageCollection )(
            ICorDebugManagedCallback4 * This,
            /* [in] */ ICorDebugProcess *pProcess);

        HRESULT ( STDMETHODCALLTYPE *AfterGarbageCollection )(
            ICorDebugManagedCallback4 * This,
            /* [in] */ ICorDebugProcess *pProcess);

        HRESULT ( STDMETHODCALLTYPE *DataBreakpoint )(
            ICorDebugManagedCallback4 * This,
            /* [in] */ ICorDebugProcess *pProcess,
            /* [in] */ ICorDebugThread *pThread,
            /* [in] */ BYTE *pContext,
            /* [in] */ ULONG32 contextSize);

        END_INTERFACE
    } ICorDebugManagedCallback4Vtbl;

    interface ICorDebugManagedCallback4
    {
        CONST_VTBL struct ICorDebugManagedCallback4Vtbl *lpVtbl;
    };



#ifdef COBJMACROS


#define ICorDebugManagedCallback4_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) )

#define ICorDebugManagedCallback4_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) )

#define ICorDebugManagedCallback4_Release(This)	\
    ( (This)->lpVtbl -> Release(This) )


#define ICorDebugManagedCallback4_BeforeGarbageCollection(This,pProcess)	\
    ( (This)->lpVtbl -> BeforeGarbageCollection(This,pProcess) )

#define ICorDebugManagedCallback4_AfterGarbageCollection(This,pProcess)	\
    ( (This)->lpVtbl -> AfterGarbageCollection(This,pProcess) )

#define ICorDebugManagedCallback4_DataBreakpoint(This,pProcess,pThread,pContext,contextSize)	\
    ( (This)->lpVtbl -> DataBreakpoint(This,pProcess,pThread,pContext,contextSize) )

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ICorDebugManagedCallback4_INTERFACE_DEFINED__ */


/* interface __MIDL_itf_cordebug_0000_0018 */
/* [local] */

#pragma warning(disable:28718)


extern RPC_IF_HANDLE __MIDL_itf_cordebug_0000_0018_v0_0_c_ifspec;
extern RPC_IF_HANDLE __MIDL_itf_cordebug_0000_0018_v0_0_s_ifspec;

#ifndef __ICorDebugManagedCallback2_INTERFACE_DEFINED__
#define __ICorDebugManagedCallback2_INTERFACE_DEFINED__

/* interface ICorDebugManagedCallback2 */
/* [unique][uuid][local][object] */

typedef
enum CorDebugExceptionCallbackType
    {
        DEBUG_EXCEPTION_FIRST_CHANCE	= 1,
        DEBUG_EXCEPTION_USER_FIRST_CHANCE	= 2,
        DEBUG_EXCEPTION_CATCH_HANDLER_FOUND	= 3,
        DEBUG_EXCEPTION_UNHANDLED	= 4
    } 	CorDebugExceptionCallbackType;

typedef
enum CorDebugExceptionFlags
    {
        DEBUG_EXCEPTION_NONE	= 0,
        DEBUG_EXCEPTION_CAN_BE_INTERCEPTED	= 0x1
    } 	CorDebugExceptionFlags;

typedef
enum CorDebugExceptionUnwindCallbackType
    {
        DEBUG_EXCEPTION_UNWIND_BEGIN	= 1,
        DEBUG_EXCEPTION_INTERCEPTED	= 2
    } 	CorDebugExceptionUnwindCallbackType;


EXTERN_C const IID IID_ICorDebugManagedCallback2;

#if defined(__cplusplus) && !defined(CINTERFACE)

    MIDL_INTERFACE("250E5EEA-DB5C-4C76-B6F3-8C46F12E3203")
    ICorDebugManagedCallback2 : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE FunctionRemapOpportunity(
            /* [in] */ ICorDebugAppDomain *pAppDomain,
            /* [in] */ ICorDebugThread *pThread,
            /* [in] */ ICorDebugFunction *pOldFunction,
            /* [in] */ ICorDebugFunction *pNewFunction,
            /* [in] */ ULONG32 oldILOffset) = 0;

        virtual HRESULT STDMETHODCALLTYPE CreateConnection(
            /* [in] */ ICorDebugProcess *pProcess,
            /* [in] */ CONNID dwConnectionId,
            /* [in] */ WCHAR *pConnName) = 0;

        virtual HRESULT STDMETHODCALLTYPE ChangeConnection(
            /* [in] */ ICorDebugProcess *pProcess,
            /* [in] */ CONNID dwConnectionId) = 0;

        virtual HRESULT STDMETHODCALLTYPE DestroyConnection(
            /* [in] */ ICorDebugProcess *pProcess,
            /* [in] */ CONNID dwConnectionId) = 0;

        virtual HRESULT STDMETHODCALLTYPE Exception(
            /* [in] */ ICorDebugAppDomain *pAppDomain,
            /* [in] */ ICorDebugThread *pThread,
            /* [in] */ ICorDebugFrame *pFrame,
            /* [in] */ ULONG32 nOffset,
            /* [in] */ CorDebugExceptionCallbackType dwEventType,
            /* [in] */ DWORD dwFlags) = 0;

        virtual HRESULT STDMETHODCALLTYPE ExceptionUnwind(
            /* [in] */ ICorDebugAppDomain *pAppDomain,
            /* [in] */ ICorDebugThread *pThread,
            /* [in] */ CorDebugExceptionUnwindCallbackType dwEventType,
            /* [in] */ DWORD dwFlags) = 0;

        virtual HRESULT STDMETHODCALLTYPE FunctionRemapComplete(
            /* [in] */ ICorDebugAppDomain *pAppDomain,
            /* [in] */ ICorDebugThread *pThread,
            /* [in] */ ICorDebugFunction *pFunction) = 0;

        virtual HRESULT STDMETHODCALLTYPE MDANotification(
            /* [in] */ ICorDebugController *pController,
            /* [in] */ ICorDebugThread *pThread,
            /* [in] */ ICorDebugMDA *pMDA) = 0;

    };


#else 	/* C style interface */

    typedef struct ICorDebugManagedCallback2Vtbl
    {
        BEGIN_INTERFACE

        HRESULT ( STDMETHODCALLTYPE *QueryInterface )(
            ICorDebugManagedCallback2 * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */
            _COM_Outptr_  void **ppvObject);

        ULONG ( STDMETHODCALLTYPE *AddRef )(
            ICorDebugManagedCallback2 * This);

        ULONG ( STDMETHODCALLTYPE *Release )(
            ICorDebugManagedCallback2 * This);

        HRESULT ( STDMETHODCALLTYPE *FunctionRemapOpportunity )(
            ICorDebugManagedCallback2 * This,
            /* [in] */ ICorDebugAppDomain *pAppDomain,
            /* [in] */ ICorDebugThread *pThread,
            /* [in] */ ICorDebugFunction *pOldFunction,
            /* [in] */ ICorDebugFunction *pNewFunction,
            /* [in] */ ULONG32 oldILOffset);

        HRESULT ( STDMETHODCALLTYPE *CreateConnection )(
            ICorDebugManagedCallback2 * This,
            /* [in] */ ICorDebugProcess *pProcess,
            /* [in] */ CONNID dwConnectionId,
            /* [in] */ WCHAR *pConnName);

        HRESULT ( STDMETHODCALLTYPE *ChangeConnection )(
            ICorDebugManagedCallback2 * This,
            /* [in] */ ICorDebugProcess *pProcess,
            /* [in] */ CONNID dwConnectionId);

        HRESULT ( STDMETHODCALLTYPE *DestroyConnection )(
            ICorDebugManagedCallback2 * This,
            /* [in] */ ICorDebugProcess *pProcess,
            /* [in] */ CONNID dwConnectionId);

        HRESULT ( STDMETHODCALLTYPE *Exception )(
            ICorDebugManagedCallback2 * This,
            /* [in] */ ICorDebugAppDomain *pAppDomain,
            /* [in] */ ICorDebugThread *pThread,
            /* [in] */ ICorDebugFrame *pFrame,
            /* [in] */ ULONG32 nOffset,
            /* [in] */ CorDebugExceptionCallbackType dwEventType,
            /* [in] */ DWORD dwFlags);

        HRESULT ( STDMETHODCALLTYPE *ExceptionUnwind )(
            ICorDebugManagedCallback2 * This,
            /* [in] */ ICorDebugAppDomain *pAppDomain,
            /* [in] */ ICorDebugThread *pThread,
            /* [in] */ CorDebugExceptionUnwindCallbackType dwEventType,
            /* [in] */ DWORD dwFlags);

        HRESULT ( STDMETHODCALLTYPE *FunctionRemapComplete )(
            ICorDebugManagedCallback2 * This,
            /* [in] */ ICorDebugAppDomain *pAppDomain,
            /* [in] */ ICorDebugThread *pThread,
            /* [in] */ ICorDebugFunction *pFunction);

        HRESULT ( STDMETHODCALLTYPE *MDANotification )(
            ICorDebugManagedCallback2 * This,
            /* [in] */ ICorDebugController *pController,
            /* [in] */ ICorDebugThread *pThread,
            /* [in] */ ICorDebugMDA *pMDA);

        END_INTERFACE
    } ICorDebugManagedCallback2Vtbl;

    interface ICorDebugManagedCallback2
    {
        CONST_VTBL struct ICorDebugManagedCallback2Vtbl *lpVtbl;
    };



#ifdef COBJMACROS


#define ICorDebugManagedCallback2_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) )

#define ICorDebugManagedCallback2_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) )

#define ICorDebugManagedCallback2_Release(This)	\
    ( (This)->lpVtbl -> Release(This) )


#define ICorDebugManagedCallback2_FunctionRemapOpportunity(This,pAppDomain,pThread,pOldFunction,pNewFunction,oldILOffset)	\
    ( (This)->lpVtbl -> FunctionRemapOpportunity(This,pAppDomain,pThread,pOldFunction,pNewFunction,oldILOffset) )

#define ICorDebugManagedCallback2_CreateConnection(This,pProcess,dwConnectionId,pConnName)	\
    ( (This)->lpVtbl -> CreateConnection(This,pProcess,dwConnectionId,pConnName) )

#define ICorDebugManagedCallback2_ChangeConnection(This,pProcess,dwConnectionId)	\
    ( (This)->lpVtbl -> ChangeConnection(This,pProcess,dwConnectionId) )

#define ICorDebugManagedCallback2_DestroyConnection(This,pProcess,dwConnectionId)	\
    ( (This)->lpVtbl -> DestroyConnection(This,pProcess,dwConnectionId) )

#define ICorDebugManagedCallback2_Exception(This,pAppDomain,pThread,pFrame,nOffset,dwEventType,dwFlags)	\
    ( (This)->lpVtbl -> Exception(This,pAppDomain,pThread,pFrame,nOffset,dwEventType,dwFlags) )

#define ICorDebugManagedCallback2_ExceptionUnwind(This,pAppDomain,pThread,dwEventType,dwFlags)	\
    ( (This)->lpVtbl -> ExceptionUnwind(This,pAppDomain,pThread,dwEventType,dwFlags) )

#define ICorDebugManagedCallback2_FunctionRemapComplete(This,pAppDomain,pThread,pFunction)	\
    ( (This)->lpVtbl -> FunctionRemapComplete(This,pAppDomain,pThread,pFunction) )

#define ICorDebugManagedCallback2_MDANotification(This,pController,pThread,pMDA)	\
    ( (This)->lpVtbl -> MDANotification(This,pController,pThread,pMDA) )

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ICorDebugManagedCallback2_INTERFACE_DEFINED__ */


/* interface __MIDL_itf_cordebug_0000_0019 */
/* [local] */

#pragma warning(pop)


extern RPC_IF_HANDLE __MIDL_itf_cordebug_0000_0019_v0_0_c_ifspec;
extern RPC_IF_HANDLE __MIDL_itf_cordebug_0000_0019_v0_0_s_ifspec;

#ifndef __ICorDebugUnmanagedCallback_INTERFACE_DEFINED__
#define __ICorDebugUnmanagedCallback_INTERFACE_DEFINED__

/* interface ICorDebugUnmanagedCallback */
/* [unique][uuid][local][object] */


EXTERN_C const IID IID_ICorDebugUnmanagedCallback;

#if defined(__cplusplus) && !defined(CINTERFACE)

    MIDL_INTERFACE("5263E909-8CB5-11d3-BD2F-0000F80849BD")
    ICorDebugUnmanagedCallback : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE DebugEvent(
            /* [in] */ LPDEBUG_EVENT pDebugEvent,
            /* [in] */ BOOL fOutOfBand) = 0;

    };


#else 	/* C style interface */

    typedef struct ICorDebugUnmanagedCallbackVtbl
    {
        BEGIN_INTERFACE

        HRESULT ( STDMETHODCALLTYPE *QueryInterface )(
            ICorDebugUnmanagedCallback * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */
            _COM_Outptr_  void **ppvObject);

        ULONG ( STDMETHODCALLTYPE *AddRef )(
            ICorDebugUnmanagedCallback * This);

        ULONG ( STDMETHODCALLTYPE *Release )(
            ICorDebugUnmanagedCallback * This);

        HRESULT ( STDMETHODCALLTYPE *DebugEvent )(
            ICorDebugUnmanagedCallback * This,
            /* [in] */ LPDEBUG_EVENT pDebugEvent,
            /* [in] */ BOOL fOutOfBand);

        END_INTERFACE
    } ICorDebugUnmanagedCallbackVtbl;

    interface ICorDebugUnmanagedCallback
    {
        CONST_VTBL struct ICorDebugUnmanagedCallbackVtbl *lpVtbl;
    };



#ifdef COBJMACROS


#define ICorDebugUnmanagedCallback_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) )

#define ICorDebugUnmanagedCallback_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) )

#define ICorDebugUnmanagedCallback_Release(This)	\
    ( (This)->lpVtbl -> Release(This) )


#define ICorDebugUnmanagedCallback_DebugEvent(This,pDebugEvent,fOutOfBand)	\
    ( (This)->lpVtbl -> DebugEvent(This,pDebugEvent,fOutOfBand) )

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ICorDebugUnmanagedCallback_INTERFACE_DEFINED__ */


/* interface __MIDL_itf_cordebug_0000_0020 */
/* [local] */

typedef
enum CorDebugCreateProcessFlags
    {
        DEBUG_NO_SPECIAL_OPTIONS	= 0
    } 	CorDebugCreateProcessFlags;

typedef
enum CorDebugHandleType
    {
        HANDLE_STRONG	= 1,
        HANDLE_WEAK_TRACK_RESURRECTION	= 2
    } 	CorDebugHandleType;

#pragma warning(push)
#pragma warning(disable:28718)


extern RPC_IF_HANDLE __MIDL_itf_cordebug_0000_0020_v0_0_c_ifspec;
extern RPC_IF_HANDLE __MIDL_itf_cordebug_0000_0020_v0_0_s_ifspec;

#ifndef __ICorDebug_INTERFACE_DEFINED__
#define __ICorDebug_INTERFACE_DEFINED__

/* interface ICorDebug */
/* [unique][uuid][local][object] */


EXTERN_C const IID IID_ICorDebug;

#if defined(__cplusplus) && !defined(CINTERFACE)

    MIDL_INTERFACE("3d6f5f61-7538-11d3-8d5b-00104b35e7ef")
    ICorDebug : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE Initialize( void) = 0;

        virtual HRESULT STDMETHODCALLTYPE Terminate( void) = 0;

        virtual HRESULT STDMETHODCALLTYPE SetManagedHandler(
            /* [in] */ ICorDebugManagedCallback *pCallback) = 0;

        virtual HRESULT STDMETHODCALLTYPE SetUnmanagedHandler(
            /* [in] */ ICorDebugUnmanagedCallback *pCallback) = 0;

        virtual HRESULT STDMETHODCALLTYPE CreateProcess(
            /* [in] */ LPCWSTR lpApplicationName,
            /* [in] */ LPWSTR lpCommandLine,
            /* [in] */ LPSECURITY_ATTRIBUTES lpProcessAttributes,
            /* [in] */ LPSECURITY_ATTRIBUTES lpThreadAttributes,
            /* [in] */ BOOL bInheritHandles,
            /* [in] */ DWORD dwCreationFlags,
            /* [in] */ PVOID lpEnvironment,
            /* [in] */ LPCWSTR lpCurrentDirectory,
            /* [in] */ LPSTARTUPINFOW lpStartupInfo,
            /* [in] */ LPPROCESS_INFORMATION lpProcessInformation,
            /* [in] */ CorDebugCreateProcessFlags debuggingFlags,
            /* [out] */ ICorDebugProcess **ppProcess) = 0;

        virtual HRESULT STDMETHODCALLTYPE DebugActiveProcess(
            /* [in] */ DWORD id,
            /* [in] */ BOOL win32Attach,
            /* [out] */ ICorDebugProcess **ppProcess) = 0;

        virtual HRESULT STDMETHODCALLTYPE EnumerateProcesses(
            /* [out] */ ICorDebugProcessEnum **ppProcess) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetProcess(
            /* [in] */ DWORD dwProcessId,
            /* [out] */ ICorDebugProcess **ppProcess) = 0;

        virtual HRESULT STDMETHODCALLTYPE CanLaunchOrAttach(
            /* [in] */ DWORD dwProcessId,
            /* [in] */ BOOL win32DebuggingEnabled) = 0;

    };


#else 	/* C style interface */

    typedef struct ICorDebugVtbl
    {
        BEGIN_INTERFACE

        HRESULT ( STDMETHODCALLTYPE *QueryInterface )(
            ICorDebug * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */
            _COM_Outptr_  void **ppvObject);

        ULONG ( STDMETHODCALLTYPE *AddRef )(
            ICorDebug * This);

        ULONG ( STDMETHODCALLTYPE *Release )(
            ICorDebug * This);

        HRESULT ( STDMETHODCALLTYPE *Initialize )(
            ICorDebug * This);

        HRESULT ( STDMETHODCALLTYPE *Terminate )(
            ICorDebug * This);

        HRESULT ( STDMETHODCALLTYPE *SetManagedHandler )(
            ICorDebug * This,
            /* [in] */ ICorDebugManagedCallback *pCallback);

        HRESULT ( STDMETHODCALLTYPE *SetUnmanagedHandler )(
            ICorDebug * This,
            /* [in] */ ICorDebugUnmanagedCallback *pCallback);

        HRESULT ( STDMETHODCALLTYPE *CreateProcess )(
            ICorDebug * This,
            /* [in] */ LPCWSTR lpApplicationName,
            /* [in] */ LPWSTR lpCommandLine,
            /* [in] */ LPSECURITY_ATTRIBUTES lpProcessAttributes,
            /* [in] */ LPSECURITY_ATTRIBUTES lpThreadAttributes,
            /* [in] */ BOOL bInheritHandles,
            /* [in] */ DWORD dwCreationFlags,
            /* [in] */ PVOID lpEnvironment,
            /* [in] */ LPCWSTR lpCurrentDirectory,
            /* [in] */ LPSTARTUPINFOW lpStartupInfo,
            /* [in] */ LPPROCESS_INFORMATION lpProcessInformation,
            /* [in] */ CorDebugCreateProcessFlags debuggingFlags,
            /* [out] */ ICorDebugProcess **ppProcess);

        HRESULT ( STDMETHODCALLTYPE *DebugActiveProcess )(
            ICorDebug * This,
            /* [in] */ DWORD id,
            /* [in] */ BOOL win32Attach,
            /* [out] */ ICorDebugProcess **ppProcess);

        HRESULT ( STDMETHODCALLTYPE *EnumerateProcesses )(
            ICorDebug * This,
            /* [out] */ ICorDebugProcessEnum **ppProcess);

        HRESULT ( STDMETHODCALLTYPE *GetProcess )(
            ICorDebug * This,
            /* [in] */ DWORD dwProcessId,
            /* [out] */ ICorDebugProcess **ppProcess);

        HRESULT ( STDMETHODCALLTYPE *CanLaunchOrAttach )(
            ICorDebug * This,
            /* [in] */ DWORD dwProcessId,
            /* [in] */ BOOL win32DebuggingEnabled);

        END_INTERFACE
    } ICorDebugVtbl;

    interface ICorDebug
    {
        CONST_VTBL struct ICorDebugVtbl *lpVtbl;
    };



#ifdef COBJMACROS


#define ICorDebug_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) )

#define ICorDebug_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) )

#define ICorDebug_Release(This)	\
    ( (This)->lpVtbl -> Release(This) )


#define ICorDebug_Initialize(This)	\
    ( (This)->lpVtbl -> Initialize(This) )

#define ICorDebug_Terminate(This)	\
    ( (This)->lpVtbl -> Terminate(This) )

#define ICorDebug_SetManagedHandler(This,pCallback)	\
    ( (This)->lpVtbl -> SetManagedHandler(This,pCallback) )

#define ICorDebug_SetUnmanagedHandler(This,pCallback)	\
    ( (This)->lpVtbl -> SetUnmanagedHandler(This,pCallback) )

#define ICorDebug_CreateProcess(This,lpApplicationName,lpCommandLine,lpProcessAttributes,lpThreadAttributes,bInheritHandles,dwCreationFlags,lpEnvironment,lpCurrentDirectory,lpStartupInfo,lpProcessInformation,debuggingFlags,ppProcess)	\
    ( (This)->lpVtbl -> CreateProcess(This,lpApplicationName,lpCommandLine,lpProcessAttributes,lpThreadAttributes,bInheritHandles,dwCreationFlags,lpEnvironment,lpCurrentDirectory,lpStartupInfo,lpProcessInformation,debuggingFlags,ppProcess) )

#define ICorDebug_DebugActiveProcess(This,id,win32Attach,ppProcess)	\
    ( (This)->lpVtbl -> DebugActiveProcess(This,id,win32Attach,ppProcess) )

#define ICorDebug_EnumerateProcesses(This,ppProcess)	\
    ( (This)->lpVtbl -> EnumerateProcesses(This,ppProcess) )

#define ICorDebug_GetProcess(This,dwProcessId,ppProcess)	\
    ( (This)->lpVtbl -> GetProcess(This,dwProcessId,ppProcess) )

#define ICorDebug_CanLaunchOrAttach(This,dwProcessId,win32DebuggingEnabled)	\
    ( (This)->lpVtbl -> CanLaunchOrAttach(This,dwProcessId,win32DebuggingEnabled) )

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ICorDebug_INTERFACE_DEFINED__ */


/* interface __MIDL_itf_cordebug_0000_0021 */
/* [local] */

#pragma warning(pop)


extern RPC_IF_HANDLE __MIDL_itf_cordebug_0000_0021_v0_0_c_ifspec;
extern RPC_IF_HANDLE __MIDL_itf_cordebug_0000_0021_v0_0_s_ifspec;

#ifndef __ICorDebugRemoteTarget_INTERFACE_DEFINED__
#define __ICorDebugRemoteTarget_INTERFACE_DEFINED__

/* interface ICorDebugRemoteTarget */
/* [unique][uuid][local][object] */


EXTERN_C const IID IID_ICorDebugRemoteTarget;

#if defined(__cplusplus) && !defined(CINTERFACE)

    MIDL_INTERFACE("C3ED8383-5A49-4cf5-B4B7-01864D9E582D")
    ICorDebugRemoteTarget : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE GetHostName(
            /* [in] */ ULONG32 cchHostName,
            /* [annotation][out] */
            _Out_  ULONG32 *pcchHostName,
            /* [annotation][length_is][size_is][out] */
            _Out_writes_to_opt_(cchHostName, *pcchHostName)  WCHAR szHostName[  ]) = 0;

    };


#else 	/* C style interface */

    typedef struct ICorDebugRemoteTargetVtbl
    {
        BEGIN_INTERFACE

        HRESULT ( STDMETHODCALLTYPE *QueryInterface )(
            ICorDebugRemoteTarget * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */
            _COM_Outptr_  void **ppvObject);

        ULONG ( STDMETHODCALLTYPE *AddRef )(
            ICorDebugRemoteTarget * This);

        ULONG ( STDMETHODCALLTYPE *Release )(
            ICorDebugRemoteTarget * This);

        HRESULT ( STDMETHODCALLTYPE *GetHostName )(
            ICorDebugRemoteTarget * This,
            /* [in] */ ULONG32 cchHostName,
            /* [annotation][out] */
            _Out_  ULONG32 *pcchHostName,
            /* [annotation][length_is][size_is][out] */
            _Out_writes_to_opt_(cchHostName, *pcchHostName)  WCHAR szHostName[  ]);

        END_INTERFACE
    } ICorDebugRemoteTargetVtbl;

    interface ICorDebugRemoteTarget
    {
        CONST_VTBL struct ICorDebugRemoteTargetVtbl *lpVtbl;
    };



#ifdef COBJMACROS


#define ICorDebugRemoteTarget_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) )

#define ICorDebugRemoteTarget_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) )

#define ICorDebugRemoteTarget_Release(This)	\
    ( (This)->lpVtbl -> Release(This) )


#define ICorDebugRemoteTarget_GetHostName(This,cchHostName,pcchHostName,szHostName)	\
    ( (This)->lpVtbl -> GetHostName(This,cchHostName,pcchHostName,szHostName) )

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ICorDebugRemoteTarget_INTERFACE_DEFINED__ */


#ifndef __ICorDebugRemote_INTERFACE_DEFINED__
#define __ICorDebugRemote_INTERFACE_DEFINED__

/* interface ICorDebugRemote */
/* [unique][uuid][local][object] */


EXTERN_C const IID IID_ICorDebugRemote;

#if defined(__cplusplus) && !defined(CINTERFACE)

    MIDL_INTERFACE("D5EBB8E2-7BBE-4c1d-98A6-A3C04CBDEF64")
    ICorDebugRemote : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE CreateProcessEx(
            /* [in] */ ICorDebugRemoteTarget *pRemoteTarget,
            /* [in] */ LPCWSTR lpApplicationName,
            /* [annotation][in] */
            _In_  LPWSTR lpCommandLine,
            /* [in] */ LPSECURITY_ATTRIBUTES lpProcessAttributes,
            /* [in] */ LPSECURITY_ATTRIBUTES lpThreadAttributes,
            /* [in] */ BOOL bInheritHandles,
            /* [in] */ DWORD dwCreationFlags,
            /* [in] */ PVOID lpEnvironment,
            /* [in] */ LPCWSTR lpCurrentDirectory,
            /* [in] */ LPSTARTUPINFOW lpStartupInfo,
            /* [in] */ LPPROCESS_INFORMATION lpProcessInformation,
            /* [in] */ CorDebugCreateProcessFlags debuggingFlags,
            /* [out] */ ICorDebugProcess **ppProcess) = 0;

        virtual HRESULT STDMETHODCALLTYPE DebugActiveProcessEx(
            /* [in] */ ICorDebugRemoteTarget *pRemoteTarget,
            /* [in] */ DWORD dwProcessId,
            /* [in] */ BOOL fWin32Attach,
            /* [out] */ ICorDebugProcess **ppProcess) = 0;

    };


#else 	/* C style interface */

    typedef struct ICorDebugRemoteVtbl
    {
        BEGIN_INTERFACE

        HRESULT ( STDMETHODCALLTYPE *QueryInterface )(
            ICorDebugRemote * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */
            _COM_Outptr_  void **ppvObject);

        ULONG ( STDMETHODCALLTYPE *AddRef )(
            ICorDebugRemote * This);

        ULONG ( STDMETHODCALLTYPE *Release )(
            ICorDebugRemote * This);

        HRESULT ( STDMETHODCALLTYPE *CreateProcessEx )(
            ICorDebugRemote * This,
            /* [in] */ ICorDebugRemoteTarget *pRemoteTarget,
            /* [in] */ LPCWSTR lpApplicationName,
            /* [annotation][in] */
            _In_  LPWSTR lpCommandLine,
            /* [in] */ LPSECURITY_ATTRIBUTES lpProcessAttributes,
            /* [in] */ LPSECURITY_ATTRIBUTES lpThreadAttributes,
            /* [in] */ BOOL bInheritHandles,
            /* [in] */ DWORD dwCreationFlags,
            /* [in] */ PVOID lpEnvironment,
            /* [in] */ LPCWSTR lpCurrentDirectory,
            /* [in] */ LPSTARTUPINFOW lpStartupInfo,
            /* [in] */ LPPROCESS_INFORMATION lpProcessInformation,
            /* [in] */ CorDebugCreateProcessFlags debuggingFlags,
            /* [out] */ ICorDebugProcess **ppProcess);

        HRESULT ( STDMETHODCALLTYPE *DebugActiveProcessEx )(
            ICorDebugRemote * This,
            /* [in] */ ICorDebugRemoteTarget *pRemoteTarget,
            /* [in] */ DWORD dwProcessId,
            /* [in] */ BOOL fWin32Attach,
            /* [out] */ ICorDebugProcess **ppProcess);

        END_INTERFACE
    } ICorDebugRemoteVtbl;

    interface ICorDebugRemote
    {
        CONST_VTBL struct ICorDebugRemoteVtbl *lpVtbl;
    };



#ifdef COBJMACROS


#define ICorDebugRemote_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) )

#define ICorDebugRemote_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) )

#define ICorDebugRemote_Release(This)	\
    ( (This)->lpVtbl -> Release(This) )


#define ICorDebugRemote_CreateProcessEx(This,pRemoteTarget,lpApplicationName,lpCommandLine,lpProcessAttributes,lpThreadAttributes,bInheritHandles,dwCreationFlags,lpEnvironment,lpCurrentDirectory,lpStartupInfo,lpProcessInformation,debuggingFlags,ppProcess)	\
    ( (This)->lpVtbl -> CreateProcessEx(This,pRemoteTarget,lpApplicationName,lpCommandLine,lpProcessAttributes,lpThreadAttributes,bInheritHandles,dwCreationFlags,lpEnvironment,lpCurrentDirectory,lpStartupInfo,lpProcessInformation,debuggingFlags,ppProcess) )

#define ICorDebugRemote_DebugActiveProcessEx(This,pRemoteTarget,dwProcessId,fWin32Attach,ppProcess)	\
    ( (This)->lpVtbl -> DebugActiveProcessEx(This,pRemoteTarget,dwProcessId,fWin32Attach,ppProcess) )

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ICorDebugRemote_INTERFACE_DEFINED__ */


/* interface __MIDL_itf_cordebug_0000_0023 */
/* [local] */

typedef struct _COR_VERSION
    {
    DWORD dwMajor;
    DWORD dwMinor;
    DWORD dwBuild;
    DWORD dwSubBuild;
    } 	COR_VERSION;



extern RPC_IF_HANDLE __MIDL_itf_cordebug_0000_0023_v0_0_c_ifspec;
extern RPC_IF_HANDLE __MIDL_itf_cordebug_0000_0023_v0_0_s_ifspec;

#ifndef __ICorDebug2_INTERFACE_DEFINED__
#define __ICorDebug2_INTERFACE_DEFINED__

/* interface ICorDebug2 */
/* [unique][uuid][local][object] */

typedef
enum CorDebugInterfaceVersion
    {
        CorDebugInvalidVersion	= 0,
        CorDebugVersion_1_0	= ( CorDebugInvalidVersion + 1 ) ,
        ver_ICorDebugManagedCallback	= CorDebugVersion_1_0,
        ver_ICorDebugUnmanagedCallback	= CorDebugVersion_1_0,
        ver_ICorDebug	= CorDebugVersion_1_0,
        ver_ICorDebugController	= CorDebugVersion_1_0,
        ver_ICorDebugAppDomain	= CorDebugVersion_1_0,
        ver_ICorDebugAssembly	= CorDebugVersion_1_0,
        ver_ICorDebugProcess	= CorDebugVersion_1_0,
        ver_ICorDebugBreakpoint	= CorDebugVersion_1_0,
        ver_ICorDebugFunctionBreakpoint	= CorDebugVersion_1_0,
        ver_ICorDebugModuleBreakpoint	= CorDebugVersion_1_0,
        ver_ICorDebugValueBreakpoint	= CorDebugVersion_1_0,
        ver_ICorDebugStepper	= CorDebugVersion_1_0,
        ver_ICorDebugRegisterSet	= CorDebugVersion_1_0,
        ver_ICorDebugThread	= CorDebugVersion_1_0,
        ver_ICorDebugChain	= CorDebugVersion_1_0,
        ver_ICorDebugFrame	= CorDebugVersion_1_0,
        ver_ICorDebugILFrame	= CorDebugVersion_1_0,
        ver_ICorDebugNativeFrame	= CorDebugVersion_1_0,
        ver_ICorDebugModule	= CorDebugVersion_1_0,
        ver_ICorDebugFunction	= CorDebugVersion_1_0,
        ver_ICorDebugCode	= CorDebugVersion_1_0,
        ver_ICorDebugClass	= CorDebugVersion_1_0,
        ver_ICorDebugEval	= CorDebugVersion_1_0,
        ver_ICorDebugValue	= CorDebugVersion_1_0,
        ver_ICorDebugGenericValue	= CorDebugVersion_1_0,
        ver_ICorDebugReferenceValue	= CorDebugVersion_1_0,
        ver_ICorDebugHeapValue	= CorDebugVersion_1_0,
        ver_ICorDebugObjectValue	= CorDebugVersion_1_0,
        ver_ICorDebugBoxValue	= CorDebugVersion_1_0,
        ver_ICorDebugStringValue	= CorDebugVersion_1_0,
        ver_ICorDebugArrayValue	= CorDebugVersion_1_0,
        ver_ICorDebugContext	= CorDebugVersion_1_0,
        ver_ICorDebugEnum	= CorDebugVersion_1_0,
        ver_ICorDebugObjectEnum	= CorDebugVersion_1_0,
        ver_ICorDebugBreakpointEnum	= CorDebugVersion_1_0,
        ver_ICorDebugStepperEnum	= CorDebugVersion_1_0,
        ver_ICorDebugProcessEnum	= CorDebugVersion_1_0,
        ver_ICorDebugThreadEnum	= CorDebugVersion_1_0,
        ver_ICorDebugFrameEnum	= CorDebugVersion_1_0,
        ver_ICorDebugChainEnum	= CorDebugVersion_1_0,
        ver_ICorDebugModuleEnum	= CorDebugVersion_1_0,
        ver_ICorDebugValueEnum	= CorDebugVersion_1_0,
        ver_ICorDebugCodeEnum	= CorDebugVersion_1_0,
        ver_ICorDebugTypeEnum	= CorDebugVersion_1_0,
        ver_ICorDebugErrorInfoEnum	= CorDebugVersion_1_0,
        ver_ICorDebugAppDomainEnum	= CorDebugVersion_1_0,
        ver_ICorDebugAssemblyEnum	= CorDebugVersion_1_0,
        ver_ICorDebugEditAndContinueErrorInfo	= CorDebugVersion_1_0,
        ver_ICorDebugEditAndContinueSnapshot	= CorDebugVersion_1_0,
        CorDebugVersion_1_1	= ( CorDebugVersion_1_0 + 1 ) ,
        CorDebugVersion_2_0	= ( CorDebugVersion_1_1 + 1 ) ,
        ver_ICorDebugManagedCallback2	= CorDebugVersion_2_0,
        ver_ICorDebugAppDomain2	= CorDebugVersion_2_0,
        ver_ICorDebugAssembly2	= CorDebugVersion_2_0,
        ver_ICorDebugProcess2	= CorDebugVersion_2_0,
        ver_ICorDebugStepper2	= CorDebugVersion_2_0,
        ver_ICorDebugRegisterSet2	= CorDebugVersion_2_0,
        ver_ICorDebugThread2	= CorDebugVersion_2_0,
        ver_ICorDebugILFrame2	= CorDebugVersion_2_0,
        ver_ICorDebugInternalFrame	= CorDebugVersion_2_0,
        ver_ICorDebugModule2	= CorDebugVersion_2_0,
        ver_ICorDebugFunction2	= CorDebugVersion_2_0,
        ver_ICorDebugCode2	= CorDebugVersion_2_0,
        ver_ICorDebugClass2	= CorDebugVersion_2_0,
        ver_ICorDebugValue2	= CorDebugVersion_2_0,
        ver_ICorDebugEval2	= CorDebugVersion_2_0,
        ver_ICorDebugObjectValue2	= CorDebugVersion_2_0,
        CorDebugVersion_4_0	= ( CorDebugVersion_2_0 + 1 ) ,
        ver_ICorDebugThread3	= CorDebugVersion_4_0,
        ver_ICorDebugThread4	= CorDebugVersion_4_0,
        ver_ICorDebugStackWalk	= CorDebugVersion_4_0,
        ver_ICorDebugNativeFrame2	= CorDebugVersion_4_0,
        ver_ICorDebugInternalFrame2	= CorDebugVersion_4_0,
        ver_ICorDebugRuntimeUnwindableFrame	= CorDebugVersion_4_0,
        ver_ICorDebugHeapValue3	= CorDebugVersion_4_0,
        ver_ICorDebugBlockingObjectEnum	= CorDebugVersion_4_0,
        ver_ICorDebugValue3	= CorDebugVersion_4_0,
        CorDebugVersion_4_5	= ( CorDebugVersion_4_0 + 1 ) ,
        ver_ICorDebugComObjectValue	= CorDebugVersion_4_5,
        ver_ICorDebugAppDomain3	= CorDebugVersion_4_5,
        ver_ICorDebugCode3	= CorDebugVersion_4_5,
        ver_ICorDebugILFrame3	= CorDebugVersion_4_5,
        CorDebugLatestVersion	= CorDebugVersion_4_5
    } 	CorDebugInterfaceVersion;


EXTERN_C const IID IID_ICorDebug2;

#if defined(__cplusplus) && !defined(CINTERFACE)

    MIDL_INTERFACE("ECCCCF2E-B286-4b3e-A983-860A8793D105")
    ICorDebug2 : public IUnknown
    {
    public:
    };


#else 	/* C style interface */

    typedef struct ICorDebug2Vtbl
    {
        BEGIN_INTERFACE

        HRESULT ( STDMETHODCALLTYPE *QueryInterface )(
            ICorDebug2 * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */
            _COM_Outptr_  void **ppvObject);

        ULONG ( STDMETHODCALLTYPE *AddRef )(
            ICorDebug2 * This);

        ULONG ( STDMETHODCALLTYPE *Release )(
            ICorDebug2 * This);

        END_INTERFACE
    } ICorDebug2Vtbl;

    interface ICorDebug2
    {
        CONST_VTBL struct ICorDebug2Vtbl *lpVtbl;
    };



#ifdef COBJMACROS


#define ICorDebug2_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) )

#define ICorDebug2_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) )

#define ICorDebug2_Release(This)	\
    ( (This)->lpVtbl -> Release(This) )


#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ICorDebug2_INTERFACE_DEFINED__ */


/* interface __MIDL_itf_cordebug_0000_0024 */
/* [local] */

typedef
enum CorDebugThreadState
    {
        THREAD_RUN	= 0,
        THREAD_SUSPEND	= ( THREAD_RUN + 1 )
    } 	CorDebugThreadState;



extern RPC_IF_HANDLE __MIDL_itf_cordebug_0000_0024_v0_0_c_ifspec;
extern RPC_IF_HANDLE __MIDL_itf_cordebug_0000_0024_v0_0_s_ifspec;

#ifndef __ICorDebugController_INTERFACE_DEFINED__
#define __ICorDebugController_INTERFACE_DEFINED__

/* interface ICorDebugController */
/* [unique][uuid][local][object] */


EXTERN_C const IID IID_ICorDebugController;

#if defined(__cplusplus) && !defined(CINTERFACE)

    MIDL_INTERFACE("3d6f5f62-7538-11d3-8d5b-00104b35e7ef")
    ICorDebugController : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE Stop(
            /* [in] */ DWORD dwTimeoutIgnored) = 0;

        virtual HRESULT STDMETHODCALLTYPE Continue(
            /* [in] */ BOOL fIsOutOfBand) = 0;

        virtual HRESULT STDMETHODCALLTYPE IsRunning(
            /* [out] */ BOOL *pbRunning) = 0;

        virtual HRESULT STDMETHODCALLTYPE HasQueuedCallbacks(
            /* [in] */ ICorDebugThread *pThread,
            /* [out] */ BOOL *pbQueued) = 0;

        virtual HRESULT STDMETHODCALLTYPE EnumerateThreads(
            /* [out] */ ICorDebugThreadEnum **ppThreads) = 0;

        virtual HRESULT STDMETHODCALLTYPE SetAllThreadsDebugState(
            /* [in] */ CorDebugThreadState state,
            /* [in] */ ICorDebugThread *pExceptThisThread) = 0;

        virtual HRESULT STDMETHODCALLTYPE Detach( void) = 0;

        virtual HRESULT STDMETHODCALLTYPE Terminate(
            /* [in] */ UINT exitCode) = 0;

        virtual HRESULT STDMETHODCALLTYPE CanCommitChanges(
            /* [in] */ ULONG cSnapshots,
            /* [size_is][in] */ ICorDebugEditAndContinueSnapshot *pSnapshots[  ],
            /* [out] */ ICorDebugErrorInfoEnum **pError) = 0;

        virtual HRESULT STDMETHODCALLTYPE CommitChanges(
            /* [in] */ ULONG cSnapshots,
            /* [size_is][in] */ ICorDebugEditAndContinueSnapshot *pSnapshots[  ],
            /* [out] */ ICorDebugErrorInfoEnum **pError) = 0;

    };


#else 	/* C style interface */

    typedef struct ICorDebugControllerVtbl
    {
        BEGIN_INTERFACE

        HRESULT ( STDMETHODCALLTYPE *QueryInterface )(
            ICorDebugController * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */
            _COM_Outptr_  void **ppvObject);

        ULONG ( STDMETHODCALLTYPE *AddRef )(
            ICorDebugController * This);

        ULONG ( STDMETHODCALLTYPE *Release )(
            ICorDebugController * This);

        HRESULT ( STDMETHODCALLTYPE *Stop )(
            ICorDebugController * This,
            /* [in] */ DWORD dwTimeoutIgnored);

        HRESULT ( STDMETHODCALLTYPE *Continue )(
            ICorDebugController * This,
            /* [in] */ BOOL fIsOutOfBand);

        HRESULT ( STDMETHODCALLTYPE *IsRunning )(
            ICorDebugController * This,
            /* [out] */ BOOL *pbRunning);

        HRESULT ( STDMETHODCALLTYPE *HasQueuedCallbacks )(
            ICorDebugController * This,
            /* [in] */ ICorDebugThread *pThread,
            /* [out] */ BOOL *pbQueued);

        HRESULT ( STDMETHODCALLTYPE *EnumerateThreads )(
            ICorDebugController * This,
            /* [out] */ ICorDebugThreadEnum **ppThreads);

        HRESULT ( STDMETHODCALLTYPE *SetAllThreadsDebugState )(
            ICorDebugController * This,
            /* [in] */ CorDebugThreadState state,
            /* [in] */ ICorDebugThread *pExceptThisThread);

        HRESULT ( STDMETHODCALLTYPE *Detach )(
            ICorDebugController * This);

        HRESULT ( STDMETHODCALLTYPE *Terminate )(
            ICorDebugController * This,
            /* [in] */ UINT exitCode);

        HRESULT ( STDMETHODCALLTYPE *CanCommitChanges )(
            ICorDebugController * This,
            /* [in] */ ULONG cSnapshots,
            /* [size_is][in] */ ICorDebugEditAndContinueSnapshot *pSnapshots[  ],
            /* [out] */ ICorDebugErrorInfoEnum **pError);

        HRESULT ( STDMETHODCALLTYPE *CommitChanges )(
            ICorDebugController * This,
            /* [in] */ ULONG cSnapshots,
            /* [size_is][in] */ ICorDebugEditAndContinueSnapshot *pSnapshots[  ],
            /* [out] */ ICorDebugErrorInfoEnum **pError);

        END_INTERFACE
    } ICorDebugControllerVtbl;

    interface ICorDebugController
    {
        CONST_VTBL struct ICorDebugControllerVtbl *lpVtbl;
    };



#ifdef COBJMACROS


#define ICorDebugController_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) )

#define ICorDebugController_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) )

#define ICorDebugController_Release(This)	\
    ( (This)->lpVtbl -> Release(This) )


#define ICorDebugController_Stop(This,dwTimeoutIgnored)	\
    ( (This)->lpVtbl -> Stop(This,dwTimeoutIgnored) )

#define ICorDebugController_Continue(This,fIsOutOfBand)	\
    ( (This)->lpVtbl -> Continue(This,fIsOutOfBand) )

#define ICorDebugController_IsRunning(This,pbRunning)	\
    ( (This)->lpVtbl -> IsRunning(This,pbRunning) )

#define ICorDebugController_HasQueuedCallbacks(This,pThread,pbQueued)	\
    ( (This)->lpVtbl -> HasQueuedCallbacks(This,pThread,pbQueued) )

#define ICorDebugController_EnumerateThreads(This,ppThreads)	\
    ( (This)->lpVtbl -> EnumerateThreads(This,ppThreads) )

#define ICorDebugController_SetAllThreadsDebugState(This,state,pExceptThisThread)	\
    ( (This)->lpVtbl -> SetAllThreadsDebugState(This,state,pExceptThisThread) )

#define ICorDebugController_Detach(This)	\
    ( (This)->lpVtbl -> Detach(This) )

#define ICorDebugController_Terminate(This,exitCode)	\
    ( (This)->lpVtbl -> Terminate(This,exitCode) )

#define ICorDebugController_CanCommitChanges(This,cSnapshots,pSnapshots,pError)	\
    ( (This)->lpVtbl -> CanCommitChanges(This,cSnapshots,pSnapshots,pError) )

#define ICorDebugController_CommitChanges(This,cSnapshots,pSnapshots,pError)	\
    ( (This)->lpVtbl -> CommitChanges(This,cSnapshots,pSnapshots,pError) )

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ICorDebugController_INTERFACE_DEFINED__ */


/* interface __MIDL_itf_cordebug_0000_0025 */
/* [local] */

#pragma warning(push)
#pragma warning(disable:28718)


extern RPC_IF_HANDLE __MIDL_itf_cordebug_0000_0025_v0_0_c_ifspec;
extern RPC_IF_HANDLE __MIDL_itf_cordebug_0000_0025_v0_0_s_ifspec;

#ifndef __ICorDebugAppDomain_INTERFACE_DEFINED__
#define __ICorDebugAppDomain_INTERFACE_DEFINED__

/* interface ICorDebugAppDomain */
/* [unique][uuid][local][object] */


EXTERN_C const IID IID_ICorDebugAppDomain;

#if defined(__cplusplus) && !defined(CINTERFACE)

    MIDL_INTERFACE("3d6f5f63-7538-11d3-8d5b-00104b35e7ef")
    ICorDebugAppDomain : public ICorDebugController
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE GetProcess(
            /* [out] */ ICorDebugProcess **ppProcess) = 0;

        virtual HRESULT STDMETHODCALLTYPE EnumerateAssemblies(
            /* [out] */ ICorDebugAssemblyEnum **ppAssemblies) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetModuleFromMetaDataInterface(
            /* [in] */ IUnknown *pIMetaData,
            /* [out] */ ICorDebugModule **ppModule) = 0;

        virtual HRESULT STDMETHODCALLTYPE EnumerateBreakpoints(
            /* [out] */ ICorDebugBreakpointEnum **ppBreakpoints) = 0;

        virtual HRESULT STDMETHODCALLTYPE EnumerateSteppers(
            /* [out] */ ICorDebugStepperEnum **ppSteppers) = 0;

        virtual HRESULT STDMETHODCALLTYPE IsAttached(
            /* [out] */ BOOL *pbAttached) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetName(
            /* [in] */ ULONG32 cchName,
            /* [out] */ ULONG32 *pcchName,
            /* [length_is][size_is][out] */ WCHAR szName[  ]) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetObject(
            /* [out] */ ICorDebugValue **ppObject) = 0;

        virtual HRESULT STDMETHODCALLTYPE Attach( void) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetID(
            /* [out] */ ULONG32 *pId) = 0;

    };


#else 	/* C style interface */

    typedef struct ICorDebugAppDomainVtbl
    {
        BEGIN_INTERFACE

        HRESULT ( STDMETHODCALLTYPE *QueryInterface )(
            ICorDebugAppDomain * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */
            _COM_Outptr_  void **ppvObject);

        ULONG ( STDMETHODCALLTYPE *AddRef )(
            ICorDebugAppDomain * This);

        ULONG ( STDMETHODCALLTYPE *Release )(
            ICorDebugAppDomain * This);

        HRESULT ( STDMETHODCALLTYPE *Stop )(
            ICorDebugAppDomain * This,
            /* [in] */ DWORD dwTimeoutIgnored);

        HRESULT ( STDMETHODCALLTYPE *Continue )(
            ICorDebugAppDomain * This,
            /* [in] */ BOOL fIsOutOfBand);

        HRESULT ( STDMETHODCALLTYPE *IsRunning )(
            ICorDebugAppDomain * This,
            /* [out] */ BOOL *pbRunning);

        HRESULT ( STDMETHODCALLTYPE *HasQueuedCallbacks )(
            ICorDebugAppDomain * This,
            /* [in] */ ICorDebugThread *pThread,
            /* [out] */ BOOL *pbQueued);

        HRESULT ( STDMETHODCALLTYPE *EnumerateThreads )(
            ICorDebugAppDomain * This,
            /* [out] */ ICorDebugThreadEnum **ppThreads);

        HRESULT ( STDMETHODCALLTYPE *SetAllThreadsDebugState )(
            ICorDebugAppDomain * This,
            /* [in] */ CorDebugThreadState state,
            /* [in] */ ICorDebugThread *pExceptThisThread);

        HRESULT ( STDMETHODCALLTYPE *Detach )(
            ICorDebugAppDomain * This);

        HRESULT ( STDMETHODCALLTYPE *Terminate )(
            ICorDebugAppDomain * This,
            /* [in] */ UINT exitCode);

        HRESULT ( STDMETHODCALLTYPE *CanCommitChanges )(
            ICorDebugAppDomain * This,
            /* [in] */ ULONG cSnapshots,
            /* [size_is][in] */ ICorDebugEditAndContinueSnapshot *pSnapshots[  ],
            /* [out] */ ICorDebugErrorInfoEnum **pError);

        HRESULT ( STDMETHODCALLTYPE *CommitChanges )(
            ICorDebugAppDomain * This,
            /* [in] */ ULONG cSnapshots,
            /* [size_is][in] */ ICorDebugEditAndContinueSnapshot *pSnapshots[  ],
            /* [out] */ ICorDebugErrorInfoEnum **pError);

        HRESULT ( STDMETHODCALLTYPE *GetProcess )(
            ICorDebugAppDomain * This,
            /* [out] */ ICorDebugProcess **ppProcess);

        HRESULT ( STDMETHODCALLTYPE *EnumerateAssemblies )(
            ICorDebugAppDomain * This,
            /* [out] */ ICorDebugAssemblyEnum **ppAssemblies);

        HRESULT ( STDMETHODCALLTYPE *GetModuleFromMetaDataInterface )(
            ICorDebugAppDomain * This,
            /* [in] */ IUnknown *pIMetaData,
            /* [out] */ ICorDebugModule **ppModule);

        HRESULT ( STDMETHODCALLTYPE *EnumerateBreakpoints )(
            ICorDebugAppDomain * This,
            /* [out] */ ICorDebugBreakpointEnum **ppBreakpoints);

        HRESULT ( STDMETHODCALLTYPE *EnumerateSteppers )(
            ICorDebugAppDomain * This,
            /* [out] */ ICorDebugStepperEnum **ppSteppers);

        HRESULT ( STDMETHODCALLTYPE *IsAttached )(
            ICorDebugAppDomain * This,
            /* [out] */ BOOL *pbAttached);

        HRESULT ( STDMETHODCALLTYPE *GetName )(
            ICorDebugAppDomain * This,
            /* [in] */ ULONG32 cchName,
            /* [out] */ ULONG32 *pcchName,
            /* [length_is][size_is][out] */ WCHAR szName[  ]);

        HRESULT ( STDMETHODCALLTYPE *GetObject )(
            ICorDebugAppDomain * This,
            /* [out] */ ICorDebugValue **ppObject);

        HRESULT ( STDMETHODCALLTYPE *Attach )(
            ICorDebugAppDomain * This);

        HRESULT ( STDMETHODCALLTYPE *GetID )(
            ICorDebugAppDomain * This,
            /* [out] */ ULONG32 *pId);

        END_INTERFACE
    } ICorDebugAppDomainVtbl;

    interface ICorDebugAppDomain
    {
        CONST_VTBL struct ICorDebugAppDomainVtbl *lpVtbl;
    };



#ifdef COBJMACROS


#define ICorDebugAppDomain_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) )

#define ICorDebugAppDomain_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) )

#define ICorDebugAppDomain_Release(This)	\
    ( (This)->lpVtbl -> Release(This) )


#define ICorDebugAppDomain_Stop(This,dwTimeoutIgnored)	\
    ( (This)->lpVtbl -> Stop(This,dwTimeoutIgnored) )

#define ICorDebugAppDomain_Continue(This,fIsOutOfBand)	\
    ( (This)->lpVtbl -> Continue(This,fIsOutOfBand) )

#define ICorDebugAppDomain_IsRunning(This,pbRunning)	\
    ( (This)->lpVtbl -> IsRunning(This,pbRunning) )

#define ICorDebugAppDomain_HasQueuedCallbacks(This,pThread,pbQueued)	\
    ( (This)->lpVtbl -> HasQueuedCallbacks(This,pThread,pbQueued) )

#define ICorDebugAppDomain_EnumerateThreads(This,ppThreads)	\
    ( (This)->lpVtbl -> EnumerateThreads(This,ppThreads) )

#define ICorDebugAppDomain_SetAllThreadsDebugState(This,state,pExceptThisThread)	\
    ( (This)->lpVtbl -> SetAllThreadsDebugState(This,state,pExceptThisThread) )

#define ICorDebugAppDomain_Detach(This)	\
    ( (This)->lpVtbl -> Detach(This) )

#define ICorDebugAppDomain_Terminate(This,exitCode)	\
    ( (This)->lpVtbl -> Terminate(This,exitCode) )

#define ICorDebugAppDomain_CanCommitChanges(This,cSnapshots,pSnapshots,pError)	\
    ( (This)->lpVtbl -> CanCommitChanges(This,cSnapshots,pSnapshots,pError) )

#define ICorDebugAppDomain_CommitChanges(This,cSnapshots,pSnapshots,pError)	\
    ( (This)->lpVtbl -> CommitChanges(This,cSnapshots,pSnapshots,pError) )


#define ICorDebugAppDomain_GetProcess(This,ppProcess)	\
    ( (This)->lpVtbl -> GetProcess(This,ppProcess) )

#define ICorDebugAppDomain_EnumerateAssemblies(This,ppAssemblies)	\
    ( (This)->lpVtbl -> EnumerateAssemblies(This,ppAssemblies) )

#define ICorDebugAppDomain_GetModuleFromMetaDataInterface(This,pIMetaData,ppModule)	\
    ( (This)->lpVtbl -> GetModuleFromMetaDataInterface(This,pIMetaData,ppModule) )

#define ICorDebugAppDomain_EnumerateBreakpoints(This,ppBreakpoints)	\
    ( (This)->lpVtbl -> EnumerateBreakpoints(This,ppBreakpoints) )

#define ICorDebugAppDomain_EnumerateSteppers(This,ppSteppers)	\
    ( (This)->lpVtbl -> EnumerateSteppers(This,ppSteppers) )

#define ICorDebugAppDomain_IsAttached(This,pbAttached)	\
    ( (This)->lpVtbl -> IsAttached(This,pbAttached) )

#define ICorDebugAppDomain_GetName(This,cchName,pcchName,szName)	\
    ( (This)->lpVtbl -> GetName(This,cchName,pcchName,szName) )

#define ICorDebugAppDomain_GetObject(This,ppObject)	\
    ( (This)->lpVtbl -> GetObject(This,ppObject) )

#define ICorDebugAppDomain_Attach(This)	\
    ( (This)->lpVtbl -> Attach(This) )

#define ICorDebugAppDomain_GetID(This,pId)	\
    ( (This)->lpVtbl -> GetID(This,pId) )

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ICorDebugAppDomain_INTERFACE_DEFINED__ */


/* interface __MIDL_itf_cordebug_0000_0026 */
/* [local] */

#pragma warning(pop)


extern RPC_IF_HANDLE __MIDL_itf_cordebug_0000_0026_v0_0_c_ifspec;
extern RPC_IF_HANDLE __MIDL_itf_cordebug_0000_0026_v0_0_s_ifspec;

#ifndef __ICorDebugAppDomain2_INTERFACE_DEFINED__
#define __ICorDebugAppDomain2_INTERFACE_DEFINED__

/* interface ICorDebugAppDomain2 */
/* [unique][uuid][local][object] */


EXTERN_C const IID IID_ICorDebugAppDomain2;

#if defined(__cplusplus) && !defined(CINTERFACE)

    MIDL_INTERFACE("096E81D5-ECDA-4202-83F5-C65980A9EF75")
    ICorDebugAppDomain2 : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE GetArrayOrPointerType(
            /* [in] */ CorElementType elementType,
            /* [in] */ ULONG32 nRank,
            /* [in] */ ICorDebugType *pTypeArg,
            /* [out] */ ICorDebugType **ppType) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetFunctionPointerType(
            /* [in] */ ULONG32 nTypeArgs,
            /* [size_is][in] */ ICorDebugType *ppTypeArgs[  ],
            /* [out] */ ICorDebugType **ppType) = 0;

    };


#else 	/* C style interface */

    typedef struct ICorDebugAppDomain2Vtbl
    {
        BEGIN_INTERFACE

        HRESULT ( STDMETHODCALLTYPE *QueryInterface )(
            ICorDebugAppDomain2 * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */
            _COM_Outptr_  void **ppvObject);

        ULONG ( STDMETHODCALLTYPE *AddRef )(
            ICorDebugAppDomain2 * This);

        ULONG ( STDMETHODCALLTYPE *Release )(
            ICorDebugAppDomain2 * This);

        HRESULT ( STDMETHODCALLTYPE *GetArrayOrPointerType )(
            ICorDebugAppDomain2 * This,
            /* [in] */ CorElementType elementType,
            /* [in] */ ULONG32 nRank,
            /* [in] */ ICorDebugType *pTypeArg,
            /* [out] */ ICorDebugType **ppType);

        HRESULT ( STDMETHODCALLTYPE *GetFunctionPointerType )(
            ICorDebugAppDomain2 * This,
            /* [in] */ ULONG32 nTypeArgs,
            /* [size_is][in] */ ICorDebugType *ppTypeArgs[  ],
            /* [out] */ ICorDebugType **ppType);

        END_INTERFACE
    } ICorDebugAppDomain2Vtbl;

    interface ICorDebugAppDomain2
    {
        CONST_VTBL struct ICorDebugAppDomain2Vtbl *lpVtbl;
    };



#ifdef COBJMACROS


#define ICorDebugAppDomain2_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) )

#define ICorDebugAppDomain2_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) )

#define ICorDebugAppDomain2_Release(This)	\
    ( (This)->lpVtbl -> Release(This) )


#define ICorDebugAppDomain2_GetArrayOrPointerType(This,elementType,nRank,pTypeArg,ppType)	\
    ( (This)->lpVtbl -> GetArrayOrPointerType(This,elementType,nRank,pTypeArg,ppType) )

#define ICorDebugAppDomain2_GetFunctionPointerType(This,nTypeArgs,ppTypeArgs,ppType)	\
    ( (This)->lpVtbl -> GetFunctionPointerType(This,nTypeArgs,ppTypeArgs,ppType) )

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ICorDebugAppDomain2_INTERFACE_DEFINED__ */


#ifndef __ICorDebugEnum_INTERFACE_DEFINED__
#define __ICorDebugEnum_INTERFACE_DEFINED__

/* interface ICorDebugEnum */
/* [unique][uuid][local][object] */


EXTERN_C const IID IID_ICorDebugEnum;

#if defined(__cplusplus) && !defined(CINTERFACE)

    MIDL_INTERFACE("CC7BCB01-8A68-11d2-983C-0000F808342D")
    ICorDebugEnum : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE Skip(
            /* [in] */ ULONG celt) = 0;

        virtual HRESULT STDMETHODCALLTYPE Reset( void) = 0;

        virtual HRESULT STDMETHODCALLTYPE Clone(
            /* [out] */ ICorDebugEnum **ppEnum) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetCount(
            /* [out] */ ULONG *pcelt) = 0;

    };


#else 	/* C style interface */

    typedef struct ICorDebugEnumVtbl
    {
        BEGIN_INTERFACE

        HRESULT ( STDMETHODCALLTYPE *QueryInterface )(
            ICorDebugEnum * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */
            _COM_Outptr_  void **ppvObject);

        ULONG ( STDMETHODCALLTYPE *AddRef )(
            ICorDebugEnum * This);

        ULONG ( STDMETHODCALLTYPE *Release )(
            ICorDebugEnum * This);

        HRESULT ( STDMETHODCALLTYPE *Skip )(
            ICorDebugEnum * This,
            /* [in] */ ULONG celt);

        HRESULT ( STDMETHODCALLTYPE *Reset )(
            ICorDebugEnum * This);

        HRESULT ( STDMETHODCALLTYPE *Clone )(
            ICorDebugEnum * This,
            /* [out] */ ICorDebugEnum **ppEnum);

        HRESULT ( STDMETHODCALLTYPE *GetCount )(
            ICorDebugEnum * This,
            /* [out] */ ULONG *pcelt);

        END_INTERFACE
    } ICorDebugEnumVtbl;

    interface ICorDebugEnum
    {
        CONST_VTBL struct ICorDebugEnumVtbl *lpVtbl;
    };



#ifdef COBJMACROS


#define ICorDebugEnum_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) )

#define ICorDebugEnum_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) )

#define ICorDebugEnum_Release(This)	\
    ( (This)->lpVtbl -> Release(This) )


#define ICorDebugEnum_Skip(This,celt)	\
    ( (This)->lpVtbl -> Skip(This,celt) )

#define ICorDebugEnum_Reset(This)	\
    ( (This)->lpVtbl -> Reset(This) )

#define ICorDebugEnum_Clone(This,ppEnum)	\
    ( (This)->lpVtbl -> Clone(This,ppEnum) )

#define ICorDebugEnum_GetCount(This,pcelt)	\
    ( (This)->lpVtbl -> GetCount(This,pcelt) )

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ICorDebugEnum_INTERFACE_DEFINED__ */


#ifndef __ICorDebugGuidToTypeEnum_INTERFACE_DEFINED__
#define __ICorDebugGuidToTypeEnum_INTERFACE_DEFINED__

/* interface ICorDebugGuidToTypeEnum */
/* [unique][uuid][local][object] */


EXTERN_C const IID IID_ICorDebugGuidToTypeEnum;

#if defined(__cplusplus) && !defined(CINTERFACE)

    MIDL_INTERFACE("6164D242-1015-4BD6-8CBE-D0DBD4B8275A")
    ICorDebugGuidToTypeEnum : public ICorDebugEnum
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE Next(
            /* [in] */ ULONG celt,
            /* [length_is][size_is][out] */ CorDebugGuidToTypeMapping values[  ],
            /* [out] */ ULONG *pceltFetched) = 0;

    };


#else 	/* C style interface */

    typedef struct ICorDebugGuidToTypeEnumVtbl
    {
        BEGIN_INTERFACE

        HRESULT ( STDMETHODCALLTYPE *QueryInterface )(
            ICorDebugGuidToTypeEnum * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */
            _COM_Outptr_  void **ppvObject);

        ULONG ( STDMETHODCALLTYPE *AddRef )(
            ICorDebugGuidToTypeEnum * This);

        ULONG ( STDMETHODCALLTYPE *Release )(
            ICorDebugGuidToTypeEnum * This);

        HRESULT ( STDMETHODCALLTYPE *Skip )(
            ICorDebugGuidToTypeEnum * This,
            /* [in] */ ULONG celt);

        HRESULT ( STDMETHODCALLTYPE *Reset )(
            ICorDebugGuidToTypeEnum * This);

        HRESULT ( STDMETHODCALLTYPE *Clone )(
            ICorDebugGuidToTypeEnum * This,
            /* [out] */ ICorDebugEnum **ppEnum);

        HRESULT ( STDMETHODCALLTYPE *GetCount )(
            ICorDebugGuidToTypeEnum * This,
            /* [out] */ ULONG *pcelt);

        HRESULT ( STDMETHODCALLTYPE *Next )(
            ICorDebugGuidToTypeEnum * This,
            /* [in] */ ULONG celt,
            /* [length_is][size_is][out] */ CorDebugGuidToTypeMapping values[  ],
            /* [out] */ ULONG *pceltFetched);

        END_INTERFACE
    } ICorDebugGuidToTypeEnumVtbl;

    interface ICorDebugGuidToTypeEnum
    {
        CONST_VTBL struct ICorDebugGuidToTypeEnumVtbl *lpVtbl;
    };



#ifdef COBJMACROS


#define ICorDebugGuidToTypeEnum_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) )

#define ICorDebugGuidToTypeEnum_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) )

#define ICorDebugGuidToTypeEnum_Release(This)	\
    ( (This)->lpVtbl -> Release(This) )


#define ICorDebugGuidToTypeEnum_Skip(This,celt)	\
    ( (This)->lpVtbl -> Skip(This,celt) )

#define ICorDebugGuidToTypeEnum_Reset(This)	\
    ( (This)->lpVtbl -> Reset(This) )

#define ICorDebugGuidToTypeEnum_Clone(This,ppEnum)	\
    ( (This)->lpVtbl -> Clone(This,ppEnum) )

#define ICorDebugGuidToTypeEnum_GetCount(This,pcelt)	\
    ( (This)->lpVtbl -> GetCount(This,pcelt) )


#define ICorDebugGuidToTypeEnum_Next(This,celt,values,pceltFetched)	\
    ( (This)->lpVtbl -> Next(This,celt,values,pceltFetched) )

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ICorDebugGuidToTypeEnum_INTERFACE_DEFINED__ */


#ifndef __ICorDebugAppDomain3_INTERFACE_DEFINED__
#define __ICorDebugAppDomain3_INTERFACE_DEFINED__

/* interface ICorDebugAppDomain3 */
/* [unique][uuid][local][object] */


EXTERN_C const IID IID_ICorDebugAppDomain3;

#if defined(__cplusplus) && !defined(CINTERFACE)

    MIDL_INTERFACE("8CB96A16-B588-42E2-B71C-DD849FC2ECCC")
    ICorDebugAppDomain3 : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE GetCachedWinRTTypesForIIDs(
            /* [in] */ ULONG32 cReqTypes,
            /* [size_is][in] */ GUID *iidsToResolve,
            /* [out] */ ICorDebugTypeEnum **ppTypesEnum) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetCachedWinRTTypes(
            /* [out] */ ICorDebugGuidToTypeEnum **ppGuidToTypeEnum) = 0;

    };


#else 	/* C style interface */

    typedef struct ICorDebugAppDomain3Vtbl
    {
        BEGIN_INTERFACE

        HRESULT ( STDMETHODCALLTYPE *QueryInterface )(
            ICorDebugAppDomain3 * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */
            _COM_Outptr_  void **ppvObject);

        ULONG ( STDMETHODCALLTYPE *AddRef )(
            ICorDebugAppDomain3 * This);

        ULONG ( STDMETHODCALLTYPE *Release )(
            ICorDebugAppDomain3 * This);

        HRESULT ( STDMETHODCALLTYPE *GetCachedWinRTTypesForIIDs )(
            ICorDebugAppDomain3 * This,
            /* [in] */ ULONG32 cReqTypes,
            /* [size_is][in] */ GUID *iidsToResolve,
            /* [out] */ ICorDebugTypeEnum **ppTypesEnum);

        HRESULT ( STDMETHODCALLTYPE *GetCachedWinRTTypes )(
            ICorDebugAppDomain3 * This,
            /* [out] */ ICorDebugGuidToTypeEnum **ppGuidToTypeEnum);

        END_INTERFACE
    } ICorDebugAppDomain3Vtbl;

    interface ICorDebugAppDomain3
    {
        CONST_VTBL struct ICorDebugAppDomain3Vtbl *lpVtbl;
    };



#ifdef COBJMACROS


#define ICorDebugAppDomain3_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) )

#define ICorDebugAppDomain3_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) )

#define ICorDebugAppDomain3_Release(This)	\
    ( (This)->lpVtbl -> Release(This) )


#define ICorDebugAppDomain3_GetCachedWinRTTypesForIIDs(This,cReqTypes,iidsToResolve,ppTypesEnum)	\
    ( (This)->lpVtbl -> GetCachedWinRTTypesForIIDs(This,cReqTypes,iidsToResolve,ppTypesEnum) )

#define ICorDebugAppDomain3_GetCachedWinRTTypes(This,ppGuidToTypeEnum)	\
    ( (This)->lpVtbl -> GetCachedWinRTTypes(This,ppGuidToTypeEnum) )

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ICorDebugAppDomain3_INTERFACE_DEFINED__ */


#ifndef __ICorDebugAppDomain4_INTERFACE_DEFINED__
#define __ICorDebugAppDomain4_INTERFACE_DEFINED__

/* interface ICorDebugAppDomain4 */
/* [unique][uuid][local][object] */


EXTERN_C const IID IID_ICorDebugAppDomain4;

#if defined(__cplusplus) && !defined(CINTERFACE)

    MIDL_INTERFACE("FB99CC40-83BE-4724-AB3B-768E796EBAC2")
    ICorDebugAppDomain4 : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE GetObjectForCCW(
            /* [in] */ CORDB_ADDRESS ccwPointer,
            /* [out] */ ICorDebugValue **ppManagedObject) = 0;

    };


#else 	/* C style interface */

    typedef struct ICorDebugAppDomain4Vtbl
    {
        BEGIN_INTERFACE

        HRESULT ( STDMETHODCALLTYPE *QueryInterface )(
            ICorDebugAppDomain4 * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */
            _COM_Outptr_  void **ppvObject);

        ULONG ( STDMETHODCALLTYPE *AddRef )(
            ICorDebugAppDomain4 * This);

        ULONG ( STDMETHODCALLTYPE *Release )(
            ICorDebugAppDomain4 * This);

        HRESULT ( STDMETHODCALLTYPE *GetObjectForCCW )(
            ICorDebugAppDomain4 * This,
            /* [in] */ CORDB_ADDRESS ccwPointer,
            /* [out] */ ICorDebugValue **ppManagedObject);

        END_INTERFACE
    } ICorDebugAppDomain4Vtbl;

    interface ICorDebugAppDomain4
    {
        CONST_VTBL struct ICorDebugAppDomain4Vtbl *lpVtbl;
    };



#ifdef COBJMACROS


#define ICorDebugAppDomain4_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) )

#define ICorDebugAppDomain4_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) )

#define ICorDebugAppDomain4_Release(This)	\
    ( (This)->lpVtbl -> Release(This) )


#define ICorDebugAppDomain4_GetObjectForCCW(This,ccwPointer,ppManagedObject)	\
    ( (This)->lpVtbl -> GetObjectForCCW(This,ccwPointer,ppManagedObject) )

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ICorDebugAppDomain4_INTERFACE_DEFINED__ */


/* interface __MIDL_itf_cordebug_0000_0030 */
/* [local] */

#pragma warning(push)
#pragma warning(disable:28718)


extern RPC_IF_HANDLE __MIDL_itf_cordebug_0000_0030_v0_0_c_ifspec;
extern RPC_IF_HANDLE __MIDL_itf_cordebug_0000_0030_v0_0_s_ifspec;

#ifndef __ICorDebugAssembly_INTERFACE_DEFINED__
#define __ICorDebugAssembly_INTERFACE_DEFINED__

/* interface ICorDebugAssembly */
/* [unique][uuid][local][object] */


EXTERN_C const IID IID_ICorDebugAssembly;

#if defined(__cplusplus) && !defined(CINTERFACE)

    MIDL_INTERFACE("df59507c-d47a-459e-bce2-6427eac8fd06")
    ICorDebugAssembly : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE GetProcess(
            /* [out] */ ICorDebugProcess **ppProcess) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetAppDomain(
            /* [out] */ ICorDebugAppDomain **ppAppDomain) = 0;

        virtual HRESULT STDMETHODCALLTYPE EnumerateModules(
            /* [out] */ ICorDebugModuleEnum **ppModules) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetCodeBase(
            /* [in] */ ULONG32 cchName,
            /* [out] */ ULONG32 *pcchName,
            /* [length_is][size_is][out] */ WCHAR szName[  ]) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetName(
            /* [in] */ ULONG32 cchName,
            /* [out] */ ULONG32 *pcchName,
            /* [length_is][size_is][out] */ WCHAR szName[  ]) = 0;

    };


#else 	/* C style interface */

    typedef struct ICorDebugAssemblyVtbl
    {
        BEGIN_INTERFACE

        HRESULT ( STDMETHODCALLTYPE *QueryInterface )(
            ICorDebugAssembly * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */
            _COM_Outptr_  void **ppvObject);

        ULONG ( STDMETHODCALLTYPE *AddRef )(
            ICorDebugAssembly * This);

        ULONG ( STDMETHODCALLTYPE *Release )(
            ICorDebugAssembly * This);

        HRESULT ( STDMETHODCALLTYPE *GetProcess )(
            ICorDebugAssembly * This,
            /* [out] */ ICorDebugProcess **ppProcess);

        HRESULT ( STDMETHODCALLTYPE *GetAppDomain )(
            ICorDebugAssembly * This,
            /* [out] */ ICorDebugAppDomain **ppAppDomain);

        HRESULT ( STDMETHODCALLTYPE *EnumerateModules )(
            ICorDebugAssembly * This,
            /* [out] */ ICorDebugModuleEnum **ppModules);

        HRESULT ( STDMETHODCALLTYPE *GetCodeBase )(
            ICorDebugAssembly * This,
            /* [in] */ ULONG32 cchName,
            /* [out] */ ULONG32 *pcchName,
            /* [length_is][size_is][out] */ WCHAR szName[  ]);

        HRESULT ( STDMETHODCALLTYPE *GetName )(
            ICorDebugAssembly * This,
            /* [in] */ ULONG32 cchName,
            /* [out] */ ULONG32 *pcchName,
            /* [length_is][size_is][out] */ WCHAR szName[  ]);

        END_INTERFACE
    } ICorDebugAssemblyVtbl;

    interface ICorDebugAssembly
    {
        CONST_VTBL struct ICorDebugAssemblyVtbl *lpVtbl;
    };



#ifdef COBJMACROS


#define ICorDebugAssembly_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) )

#define ICorDebugAssembly_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) )

#define ICorDebugAssembly_Release(This)	\
    ( (This)->lpVtbl -> Release(This) )


#define ICorDebugAssembly_GetProcess(This,ppProcess)	\
    ( (This)->lpVtbl -> GetProcess(This,ppProcess) )

#define ICorDebugAssembly_GetAppDomain(This,ppAppDomain)	\
    ( (This)->lpVtbl -> GetAppDomain(This,ppAppDomain) )

#define ICorDebugAssembly_EnumerateModules(This,ppModules)	\
    ( (This)->lpVtbl -> EnumerateModules(This,ppModules) )

#define ICorDebugAssembly_GetCodeBase(This,cchName,pcchName,szName)	\
    ( (This)->lpVtbl -> GetCodeBase(This,cchName,pcchName,szName) )

#define ICorDebugAssembly_GetName(This,cchName,pcchName,szName)	\
    ( (This)->lpVtbl -> GetName(This,cchName,pcchName,szName) )

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ICorDebugAssembly_INTERFACE_DEFINED__ */


/* interface __MIDL_itf_cordebug_0000_0031 */
/* [local] */

#pragma warning(pop)


extern RPC_IF_HANDLE __MIDL_itf_cordebug_0000_0031_v0_0_c_ifspec;
extern RPC_IF_HANDLE __MIDL_itf_cordebug_0000_0031_v0_0_s_ifspec;

#ifndef __ICorDebugAssembly2_INTERFACE_DEFINED__
#define __ICorDebugAssembly2_INTERFACE_DEFINED__

/* interface ICorDebugAssembly2 */
/* [unique][uuid][local][object] */


EXTERN_C const IID IID_ICorDebugAssembly2;

#if defined(__cplusplus) && !defined(CINTERFACE)

    MIDL_INTERFACE("426d1f9e-6dd4-44c8-aec7-26cdbaf4e398")
    ICorDebugAssembly2 : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE IsFullyTrusted(
            /* [out] */ BOOL *pbFullyTrusted) = 0;

    };


#else 	/* C style interface */

    typedef struct ICorDebugAssembly2Vtbl
    {
        BEGIN_INTERFACE

        HRESULT ( STDMETHODCALLTYPE *QueryInterface )(
            ICorDebugAssembly2 * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */
            _COM_Outptr_  void **ppvObject);

        ULONG ( STDMETHODCALLTYPE *AddRef )(
            ICorDebugAssembly2 * This);

        ULONG ( STDMETHODCALLTYPE *Release )(
            ICorDebugAssembly2 * This);

        HRESULT ( STDMETHODCALLTYPE *IsFullyTrusted )(
            ICorDebugAssembly2 * This,
            /* [out] */ BOOL *pbFullyTrusted);

        END_INTERFACE
    } ICorDebugAssembly2Vtbl;

    interface ICorDebugAssembly2
    {
        CONST_VTBL struct ICorDebugAssembly2Vtbl *lpVtbl;
    };



#ifdef COBJMACROS


#define ICorDebugAssembly2_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) )

#define ICorDebugAssembly2_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) )

#define ICorDebugAssembly2_Release(This)	\
    ( (This)->lpVtbl -> Release(This) )


#define ICorDebugAssembly2_IsFullyTrusted(This,pbFullyTrusted)	\
    ( (This)->lpVtbl -> IsFullyTrusted(This,pbFullyTrusted) )

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ICorDebugAssembly2_INTERFACE_DEFINED__ */


#ifndef __ICorDebugAssembly3_INTERFACE_DEFINED__
#define __ICorDebugAssembly3_INTERFACE_DEFINED__

/* interface ICorDebugAssembly3 */
/* [unique][uuid][local][object] */


EXTERN_C const IID IID_ICorDebugAssembly3;

#if defined(__cplusplus) && !defined(CINTERFACE)

    MIDL_INTERFACE("76361AB2-8C86-4FE9-96F2-F73D8843570A")
    ICorDebugAssembly3 : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE GetContainerAssembly(
            ICorDebugAssembly **ppAssembly) = 0;

        virtual HRESULT STDMETHODCALLTYPE EnumerateContainedAssemblies(
            ICorDebugAssemblyEnum **ppAssemblies) = 0;

    };


#else 	/* C style interface */

    typedef struct ICorDebugAssembly3Vtbl
    {
        BEGIN_INTERFACE

        HRESULT ( STDMETHODCALLTYPE *QueryInterface )(
            ICorDebugAssembly3 * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */
            _COM_Outptr_  void **ppvObject);

        ULONG ( STDMETHODCALLTYPE *AddRef )(
            ICorDebugAssembly3 * This);

        ULONG ( STDMETHODCALLTYPE *Release )(
            ICorDebugAssembly3 * This);

        HRESULT ( STDMETHODCALLTYPE *GetContainerAssembly )(
            ICorDebugAssembly3 * This,
            ICorDebugAssembly **ppAssembly);

        HRESULT ( STDMETHODCALLTYPE *EnumerateContainedAssemblies )(
            ICorDebugAssembly3 * This,
            ICorDebugAssemblyEnum **ppAssemblies);

        END_INTERFACE
    } ICorDebugAssembly3Vtbl;

    interface ICorDebugAssembly3
    {
        CONST_VTBL struct ICorDebugAssembly3Vtbl *lpVtbl;
    };



#ifdef COBJMACROS


#define ICorDebugAssembly3_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) )

#define ICorDebugAssembly3_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) )

#define ICorDebugAssembly3_Release(This)	\
    ( (This)->lpVtbl -> Release(This) )


#define ICorDebugAssembly3_GetContainerAssembly(This,ppAssembly)	\
    ( (This)->lpVtbl -> GetContainerAssembly(This,ppAssembly) )

#define ICorDebugAssembly3_EnumerateContainedAssemblies(This,ppAssemblies)	\
    ( (This)->lpVtbl -> EnumerateContainedAssemblies(This,ppAssemblies) )

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ICorDebugAssembly3_INTERFACE_DEFINED__ */


/* interface __MIDL_itf_cordebug_0000_0033 */
/* [local] */

#ifndef _DEF_COR_TYPEID_
#define _DEF_COR_TYPEID_
typedef struct COR_TYPEID
    {
    UINT64 token1;
    UINT64 token2;
    } 	COR_TYPEID;

#endif // _DEF_COR_TYPEID_
typedef struct _COR_HEAPOBJECT
    {
    CORDB_ADDRESS address;
    ULONG64 size;
    COR_TYPEID type;
    } 	COR_HEAPOBJECT;



extern RPC_IF_HANDLE __MIDL_itf_cordebug_0000_0033_v0_0_c_ifspec;
extern RPC_IF_HANDLE __MIDL_itf_cordebug_0000_0033_v0_0_s_ifspec;

#ifndef __ICorDebugHeapEnum_INTERFACE_DEFINED__
#define __ICorDebugHeapEnum_INTERFACE_DEFINED__

/* interface ICorDebugHeapEnum */
/* [unique][uuid][local][object] */


EXTERN_C const IID IID_ICorDebugHeapEnum;

#if defined(__cplusplus) && !defined(CINTERFACE)

    MIDL_INTERFACE("76D7DAB8-D044-11DF-9A15-7E29DFD72085")
    ICorDebugHeapEnum : public ICorDebugEnum
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE Next(
            /* [in] */ ULONG celt,
            /* [length_is][size_is][out] */ COR_HEAPOBJECT objects[  ],
            /* [out] */ ULONG *pceltFetched) = 0;

    };


#else 	/* C style interface */

    typedef struct ICorDebugHeapEnumVtbl
    {
        BEGIN_INTERFACE

        HRESULT ( STDMETHODCALLTYPE *QueryInterface )(
            ICorDebugHeapEnum * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */
            _COM_Outptr_  void **ppvObject);

        ULONG ( STDMETHODCALLTYPE *AddRef )(
            ICorDebugHeapEnum * This);

        ULONG ( STDMETHODCALLTYPE *Release )(
            ICorDebugHeapEnum * This);

        HRESULT ( STDMETHODCALLTYPE *Skip )(
            ICorDebugHeapEnum * This,
            /* [in] */ ULONG celt);

        HRESULT ( STDMETHODCALLTYPE *Reset )(
            ICorDebugHeapEnum * This);

        HRESULT ( STDMETHODCALLTYPE *Clone )(
            ICorDebugHeapEnum * This,
            /* [out] */ ICorDebugEnum **ppEnum);

        HRESULT ( STDMETHODCALLTYPE *GetCount )(
            ICorDebugHeapEnum * This,
            /* [out] */ ULONG *pcelt);

        HRESULT ( STDMETHODCALLTYPE *Next )(
            ICorDebugHeapEnum * This,
            /* [in] */ ULONG celt,
            /* [length_is][size_is][out] */ COR_HEAPOBJECT objects[  ],
            /* [out] */ ULONG *pceltFetched);

        END_INTERFACE
    } ICorDebugHeapEnumVtbl;

    interface ICorDebugHeapEnum
    {
        CONST_VTBL struct ICorDebugHeapEnumVtbl *lpVtbl;
    };



#ifdef COBJMACROS


#define ICorDebugHeapEnum_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) )

#define ICorDebugHeapEnum_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) )

#define ICorDebugHeapEnum_Release(This)	\
    ( (This)->lpVtbl -> Release(This) )


#define ICorDebugHeapEnum_Skip(This,celt)	\
    ( (This)->lpVtbl -> Skip(This,celt) )

#define ICorDebugHeapEnum_Reset(This)	\
    ( (This)->lpVtbl -> Reset(This) )

#define ICorDebugHeapEnum_Clone(This,ppEnum)	\
    ( (This)->lpVtbl -> Clone(This,ppEnum) )

#define ICorDebugHeapEnum_GetCount(This,pcelt)	\
    ( (This)->lpVtbl -> GetCount(This,pcelt) )


#define ICorDebugHeapEnum_Next(This,celt,objects,pceltFetched)	\
    ( (This)->lpVtbl -> Next(This,celt,objects,pceltFetched) )

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ICorDebugHeapEnum_INTERFACE_DEFINED__ */


/* interface __MIDL_itf_cordebug_0000_0034 */
/* [local] */

typedef
enum CorDebugGenerationTypes
    {
        CorDebug_Gen0	= 0,
        CorDebug_Gen1	= 1,
        CorDebug_Gen2	= 2,
        CorDebug_LOH	= 3,
        CorDebug_POH	= 4
    } 	CorDebugGenerationTypes;

typedef struct _COR_SEGMENT
    {
    CORDB_ADDRESS start;
    CORDB_ADDRESS end;
    CorDebugGenerationTypes type;
    ULONG heap;
    } 	COR_SEGMENT;

typedef
enum CorDebugGCType
    {
        CorDebugWorkstationGC	= 0,
        CorDebugServerGC	= ( CorDebugWorkstationGC + 1 )
    } 	CorDebugGCType;

typedef struct _COR_HEAPINFO
    {
    BOOL areGCStructuresValid;
    DWORD pointerSize;
    DWORD numHeaps;
    BOOL concurrent;
    CorDebugGCType gcType;
    } 	COR_HEAPINFO;



extern RPC_IF_HANDLE __MIDL_itf_cordebug_0000_0034_v0_0_c_ifspec;
extern RPC_IF_HANDLE __MIDL_itf_cordebug_0000_0034_v0_0_s_ifspec;

#ifndef __ICorDebugHeapSegmentEnum_INTERFACE_DEFINED__
#define __ICorDebugHeapSegmentEnum_INTERFACE_DEFINED__

/* interface ICorDebugHeapSegmentEnum */
/* [unique][uuid][local][object] */


EXTERN_C const IID IID_ICorDebugHeapSegmentEnum;

#if defined(__cplusplus) && !defined(CINTERFACE)

    MIDL_INTERFACE("A2FA0F8E-D045-11DF-AC8E-CE2ADFD72085")
    ICorDebugHeapSegmentEnum : public ICorDebugEnum
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE Next(
            /* [in] */ ULONG celt,
            /* [length_is][size_is][out] */ COR_SEGMENT segments[  ],
            /* [out] */ ULONG *pceltFetched) = 0;

    };


#else 	/* C style interface */

    typedef struct ICorDebugHeapSegmentEnumVtbl
    {
        BEGIN_INTERFACE

        HRESULT ( STDMETHODCALLTYPE *QueryInterface )(
            ICorDebugHeapSegmentEnum * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */
            _COM_Outptr_  void **ppvObject);

        ULONG ( STDMETHODCALLTYPE *AddRef )(
            ICorDebugHeapSegmentEnum * This);

        ULONG ( STDMETHODCALLTYPE *Release )(
            ICorDebugHeapSegmentEnum * This);

        HRESULT ( STDMETHODCALLTYPE *Skip )(
            ICorDebugHeapSegmentEnum * This,
            /* [in] */ ULONG celt);

        HRESULT ( STDMETHODCALLTYPE *Reset )(
            ICorDebugHeapSegmentEnum * This);

        HRESULT ( STDMETHODCALLTYPE *Clone )(
            ICorDebugHeapSegmentEnum * This,
            /* [out] */ ICorDebugEnum **ppEnum);

        HRESULT ( STDMETHODCALLTYPE *GetCount )(
            ICorDebugHeapSegmentEnum * This,
            /* [out] */ ULONG *pcelt);

        HRESULT ( STDMETHODCALLTYPE *Next )(
            ICorDebugHeapSegmentEnum * This,
            /* [in] */ ULONG celt,
            /* [length_is][size_is][out] */ COR_SEGMENT segments[  ],
            /* [out] */ ULONG *pceltFetched);

        END_INTERFACE
    } ICorDebugHeapSegmentEnumVtbl;

    interface ICorDebugHeapSegmentEnum
    {
        CONST_VTBL struct ICorDebugHeapSegmentEnumVtbl *lpVtbl;
    };



#ifdef COBJMACROS


#define ICorDebugHeapSegmentEnum_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) )

#define ICorDebugHeapSegmentEnum_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) )

#define ICorDebugHeapSegmentEnum_Release(This)	\
    ( (This)->lpVtbl -> Release(This) )


#define ICorDebugHeapSegmentEnum_Skip(This,celt)	\
    ( (This)->lpVtbl -> Skip(This,celt) )

#define ICorDebugHeapSegmentEnum_Reset(This)	\
    ( (This)->lpVtbl -> Reset(This) )

#define ICorDebugHeapSegmentEnum_Clone(This,ppEnum)	\
    ( (This)->lpVtbl -> Clone(This,ppEnum) )

#define ICorDebugHeapSegmentEnum_GetCount(This,pcelt)	\
    ( (This)->lpVtbl -> GetCount(This,pcelt) )


#define ICorDebugHeapSegmentEnum_Next(This,celt,segments,pceltFetched)	\
    ( (This)->lpVtbl -> Next(This,celt,segments,pceltFetched) )

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ICorDebugHeapSegmentEnum_INTERFACE_DEFINED__ */


/* interface __MIDL_itf_cordebug_0000_0035 */
/* [local] */

typedef
enum CorGCReferenceType
    {
        CorHandleStrong	= ( 1 << 0 ) ,
        CorHandleStrongPinning	= ( 1 << 1 ) ,
        CorHandleWeakShort	= ( 1 << 2 ) ,
        CorHandleWeakLong	= ( 1 << 3 ) ,
        CorHandleWeakRefCount	= ( 1 << 4 ) ,
        CorHandleStrongRefCount	= ( 1 << 5 ) ,
        CorHandleStrongDependent	= ( 1 << 6 ) ,
        CorHandleStrongAsyncPinned	= ( 1 << 7 ) ,
        CorHandleStrongSizedByref	= ( 1 << 8 ) ,
        CorHandleWeakNativeCom	= ( 1 << 9 ) ,
        CorHandleWeakWinRT	= CorHandleWeakNativeCom,
        CorReferenceStack	= 0x80000001,
        CorReferenceFinalizer	= 80000002,
        CorHandleStrongOnly	= 0x1e3,
        CorHandleWeakOnly	= 0x21c,
        CorHandleAll	= 0x7fffffff
    } 	CorGCReferenceType;

#ifndef _DEF_COR_GC_REFERENCE_
#define _DEF_COR_GC_REFERENCE_
typedef struct COR_GC_REFERENCE
    {
    ICorDebugAppDomain *Domain;
    ICorDebugValue *Location;
    CorGCReferenceType Type;
    UINT64 ExtraData;
    } 	COR_GC_REFERENCE;

#endif // _DEF_COR_GC_REFERENCE_


extern RPC_IF_HANDLE __MIDL_itf_cordebug_0000_0035_v0_0_c_ifspec;
extern RPC_IF_HANDLE __MIDL_itf_cordebug_0000_0035_v0_0_s_ifspec;

#ifndef __ICorDebugGCReferenceEnum_INTERFACE_DEFINED__
#define __ICorDebugGCReferenceEnum_INTERFACE_DEFINED__

/* interface ICorDebugGCReferenceEnum */
/* [unique][uuid][local][object] */


EXTERN_C const IID IID_ICorDebugGCReferenceEnum;

#if defined(__cplusplus) && !defined(CINTERFACE)

    MIDL_INTERFACE("7F3C24D3-7E1D-4245-AC3A-F72F8859C80C")
    ICorDebugGCReferenceEnum : public ICorDebugEnum
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE Next(
            /* [in] */ ULONG celt,
            /* [length_is][size_is][out] */ COR_GC_REFERENCE roots[  ],
            /* [out] */ ULONG *pceltFetched) = 0;

    };


#else 	/* C style interface */

    typedef struct ICorDebugGCReferenceEnumVtbl
    {
        BEGIN_INTERFACE

        HRESULT ( STDMETHODCALLTYPE *QueryInterface )(
            ICorDebugGCReferenceEnum * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */
            _COM_Outptr_  void **ppvObject);

        ULONG ( STDMETHODCALLTYPE *AddRef )(
            ICorDebugGCReferenceEnum * This);

        ULONG ( STDMETHODCALLTYPE *Release )(
            ICorDebugGCReferenceEnum * This);

        HRESULT ( STDMETHODCALLTYPE *Skip )(
            ICorDebugGCReferenceEnum * This,
            /* [in] */ ULONG celt);

        HRESULT ( STDMETHODCALLTYPE *Reset )(
            ICorDebugGCReferenceEnum * This);

        HRESULT ( STDMETHODCALLTYPE *Clone )(
            ICorDebugGCReferenceEnum * This,
            /* [out] */ ICorDebugEnum **ppEnum);

        HRESULT ( STDMETHODCALLTYPE *GetCount )(
            ICorDebugGCReferenceEnum * This,
            /* [out] */ ULONG *pcelt);

        HRESULT ( STDMETHODCALLTYPE *Next )(
            ICorDebugGCReferenceEnum * This,
            /* [in] */ ULONG celt,
            /* [length_is][size_is][out] */ COR_GC_REFERENCE roots[  ],
            /* [out] */ ULONG *pceltFetched);

        END_INTERFACE
    } ICorDebugGCReferenceEnumVtbl;

    interface ICorDebugGCReferenceEnum
    {
        CONST_VTBL struct ICorDebugGCReferenceEnumVtbl *lpVtbl;
    };



#ifdef COBJMACROS


#define ICorDebugGCReferenceEnum_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) )

#define ICorDebugGCReferenceEnum_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) )

#define ICorDebugGCReferenceEnum_Release(This)	\
    ( (This)->lpVtbl -> Release(This) )


#define ICorDebugGCReferenceEnum_Skip(This,celt)	\
    ( (This)->lpVtbl -> Skip(This,celt) )

#define ICorDebugGCReferenceEnum_Reset(This)	\
    ( (This)->lpVtbl -> Reset(This) )

#define ICorDebugGCReferenceEnum_Clone(This,ppEnum)	\
    ( (This)->lpVtbl -> Clone(This,ppEnum) )

#define ICorDebugGCReferenceEnum_GetCount(This,pcelt)	\
    ( (This)->lpVtbl -> GetCount(This,pcelt) )


#define ICorDebugGCReferenceEnum_Next(This,celt,roots,pceltFetched)	\
    ( (This)->lpVtbl -> Next(This,celt,roots,pceltFetched) )

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ICorDebugGCReferenceEnum_INTERFACE_DEFINED__ */


/* interface __MIDL_itf_cordebug_0000_0036 */
/* [local] */

#ifndef _DEF_COR_ARRAY_LAYOUT_
#define _DEF_COR_ARRAY_LAYOUT_
typedef struct COR_ARRAY_LAYOUT
    {
    COR_TYPEID componentID;
    CorElementType componentType;
    ULONG32 firstElementOffset;
    ULONG32 elementSize;
    ULONG32 countOffset;
    ULONG32 rankSize;
    ULONG32 numRanks;
    ULONG32 rankOffset;
    } 	COR_ARRAY_LAYOUT;

#endif // _DEF_COR_ARRAY_LAYOUT_
#ifndef _DEF_COR_TYPE_LAYOUT_
#define _DEF_COR_TYPE_LAYOUT_
typedef struct COR_TYPE_LAYOUT
    {
    COR_TYPEID parentID;
    ULONG32 objectSize;
    ULONG32 numFields;
    ULONG32 boxOffset;
    CorElementType type;
    } 	COR_TYPE_LAYOUT;

#endif // _DEF_COR_TYPE_LAYOUT_
#ifndef _DEF_COR_FIELD_
#define _DEF_COR_FIELD_
typedef struct COR_FIELD
    {
    mdFieldDef token;
    ULONG32 offset;
    COR_TYPEID id;
    CorElementType fieldType;
    } 	COR_FIELD;

#endif // _DEF_COR_FIELD_
#pragma warning(push)
#pragma warning(disable:28718)


extern RPC_IF_HANDLE __MIDL_itf_cordebug_0000_0036_v0_0_c_ifspec;
extern RPC_IF_HANDLE __MIDL_itf_cordebug_0000_0036_v0_0_s_ifspec;

#ifndef __ICorDebugProcess_INTERFACE_DEFINED__
#define __ICorDebugProcess_INTERFACE_DEFINED__

/* interface ICorDebugProcess */
/* [unique][uuid][local][object] */


EXTERN_C const IID IID_ICorDebugProcess;

#if defined(__cplusplus) && !defined(CINTERFACE)

    MIDL_INTERFACE("3d6f5f64-7538-11d3-8d5b-00104b35e7ef")
    ICorDebugProcess : public ICorDebugController
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE GetID(
            /* [out] */ DWORD *pdwProcessId) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetHandle(
            /* [out] */ HPROCESS *phProcessHandle) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetThread(
            /* [in] */ DWORD dwThreadId,
            /* [out] */ ICorDebugThread **ppThread) = 0;

        virtual HRESULT STDMETHODCALLTYPE EnumerateObjects(
            /* [out] */ ICorDebugObjectEnum **ppObjects) = 0;

        virtual HRESULT STDMETHODCALLTYPE IsTransitionStub(
            /* [in] */ CORDB_ADDRESS address,
            /* [out] */ BOOL *pbTransitionStub) = 0;

        virtual HRESULT STDMETHODCALLTYPE IsOSSuspended(
            /* [in] */ DWORD threadID,
            /* [out] */ BOOL *pbSuspended) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetThreadContext(
            /* [in] */ DWORD threadID,
            /* [in] */ ULONG32 contextSize,
            /* [size_is][length_is][out][in] */ BYTE context[  ]) = 0;

        virtual HRESULT STDMETHODCALLTYPE SetThreadContext(
            /* [in] */ DWORD threadID,
            /* [in] */ ULONG32 contextSize,
            /* [size_is][length_is][in] */ BYTE context[  ]) = 0;

        virtual HRESULT STDMETHODCALLTYPE ReadMemory(
            /* [in] */ CORDB_ADDRESS address,
            /* [in] */ DWORD size,
            /* [length_is][size_is][out] */ BYTE buffer[  ],
            /* [out] */ SIZE_T *read) = 0;

        virtual HRESULT STDMETHODCALLTYPE WriteMemory(
            /* [in] */ CORDB_ADDRESS address,
            /* [in] */ DWORD size,
            /* [size_is][in] */ BYTE buffer[  ],
            /* [out] */ SIZE_T *written) = 0;

        virtual HRESULT STDMETHODCALLTYPE ClearCurrentException(
            /* [in] */ DWORD threadID) = 0;

        virtual HRESULT STDMETHODCALLTYPE EnableLogMessages(
            /* [in] */ BOOL fOnOff) = 0;

        virtual HRESULT STDMETHODCALLTYPE ModifyLogSwitch(
            /* [annotation][in] */
            _In_  WCHAR *pLogSwitchName,
            /* [in] */ LONG lLevel) = 0;

        virtual HRESULT STDMETHODCALLTYPE EnumerateAppDomains(
            /* [out] */ ICorDebugAppDomainEnum **ppAppDomains) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetObject(
            /* [out] */ ICorDebugValue **ppObject) = 0;

        virtual HRESULT STDMETHODCALLTYPE ThreadForFiberCookie(
            /* [in] */ DWORD fiberCookie,
            /* [out] */ ICorDebugThread **ppThread) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetHelperThreadID(
            /* [out] */ DWORD *pThreadID) = 0;

    };


#else 	/* C style interface */

    typedef struct ICorDebugProcessVtbl
    {
        BEGIN_INTERFACE

        HRESULT ( STDMETHODCALLTYPE *QueryInterface )(
            ICorDebugProcess * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */
            _COM_Outptr_  void **ppvObject);

        ULONG ( STDMETHODCALLTYPE *AddRef )(
            ICorDebugProcess * This);

        ULONG ( STDMETHODCALLTYPE *Release )(
            ICorDebugProcess * This);

        HRESULT ( STDMETHODCALLTYPE *Stop )(
            ICorDebugProcess * This,
            /* [in] */ DWORD dwTimeoutIgnored);

        HRESULT ( STDMETHODCALLTYPE *Continue )(
            ICorDebugProcess * This,
            /* [in] */ BOOL fIsOutOfBand);

        HRESULT ( STDMETHODCALLTYPE *IsRunning )(
            ICorDebugProcess * This,
            /* [out] */ BOOL *pbRunning);

        HRESULT ( STDMETHODCALLTYPE *HasQueuedCallbacks )(
            ICorDebugProcess * This,
            /* [in] */ ICorDebugThread *pThread,
            /* [out] */ BOOL *pbQueued);

        HRESULT ( STDMETHODCALLTYPE *EnumerateThreads )(
            ICorDebugProcess * This,
            /* [out] */ ICorDebugThreadEnum **ppThreads);

        HRESULT ( STDMETHODCALLTYPE *SetAllThreadsDebugState )(
            ICorDebugProcess * This,
            /* [in] */ CorDebugThreadState state,
            /* [in] */ ICorDebugThread *pExceptThisThread);

        HRESULT ( STDMETHODCALLTYPE *Detach )(
            ICorDebugProcess * This);

        HRESULT ( STDMETHODCALLTYPE *Terminate )(
            ICorDebugProcess * This,
            /* [in] */ UINT exitCode);

        HRESULT ( STDMETHODCALLTYPE *CanCommitChanges )(
            ICorDebugProcess * This,
            /* [in] */ ULONG cSnapshots,
            /* [size_is][in] */ ICorDebugEditAndContinueSnapshot *pSnapshots[  ],
            /* [out] */ ICorDebugErrorInfoEnum **pError);

        HRESULT ( STDMETHODCALLTYPE *CommitChanges )(
            ICorDebugProcess * This,
            /* [in] */ ULONG cSnapshots,
            /* [size_is][in] */ ICorDebugEditAndContinueSnapshot *pSnapshots[  ],
            /* [out] */ ICorDebugErrorInfoEnum **pError);

        HRESULT ( STDMETHODCALLTYPE *GetID )(
            ICorDebugProcess * This,
            /* [out] */ DWORD *pdwProcessId);

        HRESULT ( STDMETHODCALLTYPE *GetHandle )(
            ICorDebugProcess * This,
            /* [out] */ HPROCESS *phProcessHandle);

        HRESULT ( STDMETHODCALLTYPE *GetThread )(
            ICorDebugProcess * This,
            /* [in] */ DWORD dwThreadId,
            /* [out] */ ICorDebugThread **ppThread);

        HRESULT ( STDMETHODCALLTYPE *EnumerateObjects )(
            ICorDebugProcess * This,
            /* [out] */ ICorDebugObjectEnum **ppObjects);

        HRESULT ( STDMETHODCALLTYPE *IsTransitionStub )(
            ICorDebugProcess * This,
            /* [in] */ CORDB_ADDRESS address,
            /* [out] */ BOOL *pbTransitionStub);

        HRESULT ( STDMETHODCALLTYPE *IsOSSuspended )(
            ICorDebugProcess * This,
            /* [in] */ DWORD threadID,
            /* [out] */ BOOL *pbSuspended);

        HRESULT ( STDMETHODCALLTYPE *GetThreadContext )(
            ICorDebugProcess * This,
            /* [in] */ DWORD threadID,
            /* [in] */ ULONG32 contextSize,
            /* [size_is][length_is][out][in] */ BYTE context[  ]);

        HRESULT ( STDMETHODCALLTYPE *SetThreadContext )(
            ICorDebugProcess * This,
            /* [in] */ DWORD threadID,
            /* [in] */ ULONG32 contextSize,
            /* [size_is][length_is][in] */ BYTE context[  ]);

        HRESULT ( STDMETHODCALLTYPE *ReadMemory )(
            ICorDebugProcess * This,
            /* [in] */ CORDB_ADDRESS address,
            /* [in] */ DWORD size,
            /* [length_is][size_is][out] */ BYTE buffer[  ],
            /* [out] */ SIZE_T *read);

        HRESULT ( STDMETHODCALLTYPE *WriteMemory )(
            ICorDebugProcess * This,
            /* [in] */ CORDB_ADDRESS address,
            /* [in] */ DWORD size,
            /* [size_is][in] */ BYTE buffer[  ],
            /* [out] */ SIZE_T *written);

        HRESULT ( STDMETHODCALLTYPE *ClearCurrentException )(
            ICorDebugProcess * This,
            /* [in] */ DWORD threadID);

        HRESULT ( STDMETHODCALLTYPE *EnableLogMessages )(
            ICorDebugProcess * This,
            /* [in] */ BOOL fOnOff);

        HRESULT ( STDMETHODCALLTYPE *ModifyLogSwitch )(
            ICorDebugProcess * This,
            /* [annotation][in] */
            _In_  WCHAR *pLogSwitchName,
            /* [in] */ LONG lLevel);

        HRESULT ( STDMETHODCALLTYPE *EnumerateAppDomains )(
            ICorDebugProcess * This,
            /* [out] */ ICorDebugAppDomainEnum **ppAppDomains);

        HRESULT ( STDMETHODCALLTYPE *GetObject )(
            ICorDebugProcess * This,
            /* [out] */ ICorDebugValue **ppObject);

        HRESULT ( STDMETHODCALLTYPE *ThreadForFiberCookie )(
            ICorDebugProcess * This,
            /* [in] */ DWORD fiberCookie,
            /* [out] */ ICorDebugThread **ppThread);

        HRESULT ( STDMETHODCALLTYPE *GetHelperThreadID )(
            ICorDebugProcess * This,
            /* [out] */ DWORD *pThreadID);

        END_INTERFACE
    } ICorDebugProcessVtbl;

    interface ICorDebugProcess
    {
        CONST_VTBL struct ICorDebugProcessVtbl *lpVtbl;
    };



#ifdef COBJMACROS


#define ICorDebugProcess_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) )

#define ICorDebugProcess_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) )

#define ICorDebugProcess_Release(This)	\
    ( (This)->lpVtbl -> Release(This) )


#define ICorDebugProcess_Stop(This,dwTimeoutIgnored)	\
    ( (This)->lpVtbl -> Stop(This,dwTimeoutIgnored) )

#define ICorDebugProcess_Continue(This,fIsOutOfBand)	\
    ( (This)->lpVtbl -> Continue(This,fIsOutOfBand) )

#define ICorDebugProcess_IsRunning(This,pbRunning)	\
    ( (This)->lpVtbl -> IsRunning(This,pbRunning) )

#define ICorDebugProcess_HasQueuedCallbacks(This,pThread,pbQueued)	\
    ( (This)->lpVtbl -> HasQueuedCallbacks(This,pThread,pbQueued) )

#define ICorDebugProcess_EnumerateThreads(This,ppThreads)	\
    ( (This)->lpVtbl -> EnumerateThreads(This,ppThreads) )

#define ICorDebugProcess_SetAllThreadsDebugState(This,state,pExceptThisThread)	\
    ( (This)->lpVtbl -> SetAllThreadsDebugState(This,state,pExceptThisThread) )

#define ICorDebugProcess_Detach(This)	\
    ( (This)->lpVtbl -> Detach(This) )

#define ICorDebugProcess_Terminate(This,exitCode)	\
    ( (This)->lpVtbl -> Terminate(This,exitCode) )

#define ICorDebugProcess_CanCommitChanges(This,cSnapshots,pSnapshots,pError)	\
    ( (This)->lpVtbl -> CanCommitChanges(This,cSnapshots,pSnapshots,pError) )

#define ICorDebugProcess_CommitChanges(This,cSnapshots,pSnapshots,pError)	\
    ( (This)->lpVtbl -> CommitChanges(This,cSnapshots,pSnapshots,pError) )


#define ICorDebugProcess_GetID(This,pdwProcessId)	\
    ( (This)->lpVtbl -> GetID(This,pdwProcessId) )

#define ICorDebugProcess_GetHandle(This,phProcessHandle)	\
    ( (This)->lpVtbl -> GetHandle(This,phProcessHandle) )

#define ICorDebugProcess_GetThread(This,dwThreadId,ppThread)	\
    ( (This)->lpVtbl -> GetThread(This,dwThreadId,ppThread) )

#define ICorDebugProcess_EnumerateObjects(This,ppObjects)	\
    ( (This)->lpVtbl -> EnumerateObjects(This,ppObjects) )

#define ICorDebugProcess_IsTransitionStub(This,address,pbTransitionStub)	\
    ( (This)->lpVtbl -> IsTransitionStub(This,address,pbTransitionStub) )

#define ICorDebugProcess_IsOSSuspended(This,threadID,pbSuspended)	\
    ( (This)->lpVtbl -> IsOSSuspended(This,threadID,pbSuspended) )

#define ICorDebugProcess_GetThreadContext(This,threadID,contextSize,context)	\
    ( (This)->lpVtbl -> GetThreadContext(This,threadID,contextSize,context) )

#define ICorDebugProcess_SetThreadContext(This,threadID,contextSize,context)	\
    ( (This)->lpVtbl -> SetThreadContext(This,threadID,contextSize,context) )

#define ICorDebugProcess_ReadMemory(This,address,size,buffer,read)	\
    ( (This)->lpVtbl -> ReadMemory(This,address,size,buffer,read) )

#define ICorDebugProcess_WriteMemory(This,address,size,buffer,written)	\
    ( (This)->lpVtbl -> WriteMemory(This,address,size,buffer,written) )

#define ICorDebugProcess_ClearCurrentException(This,threadID)	\
    ( (This)->lpVtbl -> ClearCurrentException(This,threadID) )

#define ICorDebugProcess_EnableLogMessages(This,fOnOff)	\
    ( (This)->lpVtbl -> EnableLogMessages(This,fOnOff) )

#define ICorDebugProcess_ModifyLogSwitch(This,pLogSwitchName,lLevel)	\
    ( (This)->lpVtbl -> ModifyLogSwitch(This,pLogSwitchName,lLevel) )

#define ICorDebugProcess_EnumerateAppDomains(This,ppAppDomains)	\
    ( (This)->lpVtbl -> EnumerateAppDomains(This,ppAppDomains) )

#define ICorDebugProcess_GetObject(This,ppObject)	\
    ( (This)->lpVtbl -> GetObject(This,ppObject) )

#define ICorDebugProcess_ThreadForFiberCookie(This,fiberCookie,ppThread)	\
    ( (This)->lpVtbl -> ThreadForFiberCookie(This,fiberCookie,ppThread) )

#define ICorDebugProcess_GetHelperThreadID(This,pThreadID)	\
    ( (This)->lpVtbl -> GetHelperThreadID(This,pThreadID) )

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ICorDebugProcess_INTERFACE_DEFINED__ */


/* interface __MIDL_itf_cordebug_0000_0037 */
/* [local] */

#pragma warning(pop)


extern RPC_IF_HANDLE __MIDL_itf_cordebug_0000_0037_v0_0_c_ifspec;
extern RPC_IF_HANDLE __MIDL_itf_cordebug_0000_0037_v0_0_s_ifspec;

#ifndef __ICorDebugProcess2_INTERFACE_DEFINED__
#define __ICorDebugProcess2_INTERFACE_DEFINED__

/* interface ICorDebugProcess2 */
/* [unique][uuid][local][object] */


EXTERN_C const IID IID_ICorDebugProcess2;

#if defined(__cplusplus) && !defined(CINTERFACE)

    MIDL_INTERFACE("AD1B3588-0EF0-4744-A496-AA09A9F80371")
    ICorDebugProcess2 : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE GetThreadForTaskID(
            /* [in] */ TASKID taskid,
            /* [out] */ ICorDebugThread2 **ppThread) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetVersion(
            /* [out] */ COR_VERSION *version) = 0;

        virtual HRESULT STDMETHODCALLTYPE SetUnmanagedBreakpoint(
            /* [in] */ CORDB_ADDRESS address,
            /* [in] */ ULONG32 bufsize,
            /* [length_is][size_is][out] */ BYTE buffer[  ],
            /* [out] */ ULONG32 *bufLen) = 0;

        virtual HRESULT STDMETHODCALLTYPE ClearUnmanagedBreakpoint(
            /* [in] */ CORDB_ADDRESS address) = 0;

        virtual HRESULT STDMETHODCALLTYPE SetDesiredNGENCompilerFlags(
            /* [in] */ DWORD pdwFlags) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetDesiredNGENCompilerFlags(
            /* [out] */ DWORD *pdwFlags) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetReferenceValueFromGCHandle(
            /* [in] */ UINT_PTR handle,
            /* [out] */ ICorDebugReferenceValue **pOutValue) = 0;

    };


#else 	/* C style interface */

    typedef struct ICorDebugProcess2Vtbl
    {
        BEGIN_INTERFACE

        HRESULT ( STDMETHODCALLTYPE *QueryInterface )(
            ICorDebugProcess2 * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */
            _COM_Outptr_  void **ppvObject);

        ULONG ( STDMETHODCALLTYPE *AddRef )(
            ICorDebugProcess2 * This);

        ULONG ( STDMETHODCALLTYPE *Release )(
            ICorDebugProcess2 * This);

        HRESULT ( STDMETHODCALLTYPE *GetThreadForTaskID )(
            ICorDebugProcess2 * This,
            /* [in] */ TASKID taskid,
            /* [out] */ ICorDebugThread2 **ppThread);

        HRESULT ( STDMETHODCALLTYPE *GetVersion )(
            ICorDebugProcess2 * This,
            /* [out] */ COR_VERSION *version);

        HRESULT ( STDMETHODCALLTYPE *SetUnmanagedBreakpoint )(
            ICorDebugProcess2 * This,
            /* [in] */ CORDB_ADDRESS address,
            /* [in] */ ULONG32 bufsize,
            /* [length_is][size_is][out] */ BYTE buffer[  ],
            /* [out] */ ULONG32 *bufLen);

        HRESULT ( STDMETHODCALLTYPE *ClearUnmanagedBreakpoint )(
            ICorDebugProcess2 * This,
            /* [in] */ CORDB_ADDRESS address);

        HRESULT ( STDMETHODCALLTYPE *SetDesiredNGENCompilerFlags )(
            ICorDebugProcess2 * This,
            /* [in] */ DWORD pdwFlags);

        HRESULT ( STDMETHODCALLTYPE *GetDesiredNGENCompilerFlags )(
            ICorDebugProcess2 * This,
            /* [out] */ DWORD *pdwFlags);

        HRESULT ( STDMETHODCALLTYPE *GetReferenceValueFromGCHandle )(
            ICorDebugProcess2 * This,
            /* [in] */ UINT_PTR handle,
            /* [out] */ ICorDebugReferenceValue **pOutValue);

        END_INTERFACE
    } ICorDebugProcess2Vtbl;

    interface ICorDebugProcess2
    {
        CONST_VTBL struct ICorDebugProcess2Vtbl *lpVtbl;
    };



#ifdef COBJMACROS


#define ICorDebugProcess2_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) )

#define ICorDebugProcess2_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) )

#define ICorDebugProcess2_Release(This)	\
    ( (This)->lpVtbl -> Release(This) )


#define ICorDebugProcess2_GetThreadForTaskID(This,taskid,ppThread)	\
    ( (This)->lpVtbl -> GetThreadForTaskID(This,taskid,ppThread) )

#define ICorDebugProcess2_GetVersion(This,version)	\
    ( (This)->lpVtbl -> GetVersion(This,version) )

#define ICorDebugProcess2_SetUnmanagedBreakpoint(This,address,bufsize,buffer,bufLen)	\
    ( (This)->lpVtbl -> SetUnmanagedBreakpoint(This,address,bufsize,buffer,bufLen) )

#define ICorDebugProcess2_ClearUnmanagedBreakpoint(This,address)	\
    ( (This)->lpVtbl -> ClearUnmanagedBreakpoint(This,address) )

#define ICorDebugProcess2_SetDesiredNGENCompilerFlags(This,pdwFlags)	\
    ( (This)->lpVtbl -> SetDesiredNGENCompilerFlags(This,pdwFlags) )

#define ICorDebugProcess2_GetDesiredNGENCompilerFlags(This,pdwFlags)	\
    ( (This)->lpVtbl -> GetDesiredNGENCompilerFlags(This,pdwFlags) )

#define ICorDebugProcess2_GetReferenceValueFromGCHandle(This,handle,pOutValue)	\
    ( (This)->lpVtbl -> GetReferenceValueFromGCHandle(This,handle,pOutValue) )

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ICorDebugProcess2_INTERFACE_DEFINED__ */


#ifndef __ICorDebugProcess3_INTERFACE_DEFINED__
#define __ICorDebugProcess3_INTERFACE_DEFINED__

/* interface ICorDebugProcess3 */
/* [unique][uuid][local][object] */


EXTERN_C const IID IID_ICorDebugProcess3;

#if defined(__cplusplus) && !defined(CINTERFACE)

    MIDL_INTERFACE("2EE06488-C0D4-42B1-B26D-F3795EF606FB")
    ICorDebugProcess3 : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE SetEnableCustomNotification(
            ICorDebugClass *pClass,
            BOOL fEnable) = 0;

    };


#else 	/* C style interface */

    typedef struct ICorDebugProcess3Vtbl
    {
        BEGIN_INTERFACE

        HRESULT ( STDMETHODCALLTYPE *QueryInterface )(
            ICorDebugProcess3 * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */
            _COM_Outptr_  void **ppvObject);

        ULONG ( STDMETHODCALLTYPE *AddRef )(
            ICorDebugProcess3 * This);

        ULONG ( STDMETHODCALLTYPE *Release )(
            ICorDebugProcess3 * This);

        HRESULT ( STDMETHODCALLTYPE *SetEnableCustomNotification )(
            ICorDebugProcess3 * This,
            ICorDebugClass *pClass,
            BOOL fEnable);

        END_INTERFACE
    } ICorDebugProcess3Vtbl;

    interface ICorDebugProcess3
    {
        CONST_VTBL struct ICorDebugProcess3Vtbl *lpVtbl;
    };



#ifdef COBJMACROS


#define ICorDebugProcess3_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) )

#define ICorDebugProcess3_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) )

#define ICorDebugProcess3_Release(This)	\
    ( (This)->lpVtbl -> Release(This) )


#define ICorDebugProcess3_SetEnableCustomNotification(This,pClass,fEnable)	\
    ( (This)->lpVtbl -> SetEnableCustomNotification(This,pClass,fEnable) )

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ICorDebugProcess3_INTERFACE_DEFINED__ */


#ifndef __ICorDebugProcess5_INTERFACE_DEFINED__
#define __ICorDebugProcess5_INTERFACE_DEFINED__

/* interface ICorDebugProcess5 */
/* [unique][uuid][local][object] */


EXTERN_C const IID IID_ICorDebugProcess5;

#if defined(__cplusplus) && !defined(CINTERFACE)

    MIDL_INTERFACE("21e9d9c0-fcb8-11df-8cff-0800200c9a66")
    ICorDebugProcess5 : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE GetGCHeapInformation(
            /* [out] */ COR_HEAPINFO *pHeapInfo) = 0;

        virtual HRESULT STDMETHODCALLTYPE EnumerateHeap(
            /* [out] */ ICorDebugHeapEnum **ppObjects) = 0;

        virtual HRESULT STDMETHODCALLTYPE EnumerateHeapRegions(
            /* [out] */ ICorDebugHeapSegmentEnum **ppRegions) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetObject(
            /* [in] */ CORDB_ADDRESS addr,
            /* [out] */ ICorDebugObjectValue **pObject) = 0;

        virtual HRESULT STDMETHODCALLTYPE EnumerateGCReferences(
            /* [in] */ BOOL enumerateWeakReferences,
            /* [out] */ ICorDebugGCReferenceEnum **ppEnum) = 0;

        virtual HRESULT STDMETHODCALLTYPE EnumerateHandles(
            /* [in] */ CorGCReferenceType types,
            /* [out] */ ICorDebugGCReferenceEnum **ppEnum) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetTypeID(
            /* [in] */ CORDB_ADDRESS obj,
            /* [out] */ COR_TYPEID *pId) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetTypeForTypeID(
            /* [in] */ COR_TYPEID id,
            /* [out] */ ICorDebugType **ppType) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetArrayLayout(
            /* [in] */ COR_TYPEID id,
            /* [out] */ COR_ARRAY_LAYOUT *pLayout) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetTypeLayout(
            /* [in] */ COR_TYPEID id,
            /* [out] */ COR_TYPE_LAYOUT *pLayout) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetTypeFields(
            /* [in] */ COR_TYPEID id,
            ULONG32 celt,
            COR_FIELD fields[  ],
            ULONG32 *pceltNeeded) = 0;

        virtual HRESULT STDMETHODCALLTYPE EnableNGENPolicy(
            /* [in] */ CorDebugNGENPolicy ePolicy) = 0;

    };


#else 	/* C style interface */

    typedef struct ICorDebugProcess5Vtbl
    {
        BEGIN_INTERFACE

        HRESULT ( STDMETHODCALLTYPE *QueryInterface )(
            ICorDebugProcess5 * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */
            _COM_Outptr_  void **ppvObject);

        ULONG ( STDMETHODCALLTYPE *AddRef )(
            ICorDebugProcess5 * This);

        ULONG ( STDMETHODCALLTYPE *Release )(
            ICorDebugProcess5 * This);

        HRESULT ( STDMETHODCALLTYPE *GetGCHeapInformation )(
            ICorDebugProcess5 * This,
            /* [out] */ COR_HEAPINFO *pHeapInfo);

        HRESULT ( STDMETHODCALLTYPE *EnumerateHeap )(
            ICorDebugProcess5 * This,
            /* [out] */ ICorDebugHeapEnum **ppObjects);

        HRESULT ( STDMETHODCALLTYPE *EnumerateHeapRegions )(
            ICorDebugProcess5 * This,
            /* [out] */ ICorDebugHeapSegmentEnum **ppRegions);

        HRESULT ( STDMETHODCALLTYPE *GetObject )(
            ICorDebugProcess5 * This,
            /* [in] */ CORDB_ADDRESS addr,
            /* [out] */ ICorDebugObjectValue **pObject);

        HRESULT ( STDMETHODCALLTYPE *EnumerateGCReferences )(
            ICorDebugProcess5 * This,
            /* [in] */ BOOL enumerateWeakReferences,
            /* [out] */ ICorDebugGCReferenceEnum **ppEnum);

        HRESULT ( STDMETHODCALLTYPE *EnumerateHandles )(
            ICorDebugProcess5 * This,
            /* [in] */ CorGCReferenceType types,
            /* [out] */ ICorDebugGCReferenceEnum **ppEnum);

        HRESULT ( STDMETHODCALLTYPE *GetTypeID )(
            ICorDebugProcess5 * This,
            /* [in] */ CORDB_ADDRESS obj,
            /* [out] */ COR_TYPEID *pId);

        HRESULT ( STDMETHODCALLTYPE *GetTypeForTypeID )(
            ICorDebugProcess5 * This,
            /* [in] */ COR_TYPEID id,
            /* [out] */ ICorDebugType **ppType);

        HRESULT ( STDMETHODCALLTYPE *GetArrayLayout )(
            ICorDebugProcess5 * This,
            /* [in] */ COR_TYPEID id,
            /* [out] */ COR_ARRAY_LAYOUT *pLayout);

        HRESULT ( STDMETHODCALLTYPE *GetTypeLayout )(
            ICorDebugProcess5 * This,
            /* [in] */ COR_TYPEID id,
            /* [out] */ COR_TYPE_LAYOUT *pLayout);

        HRESULT ( STDMETHODCALLTYPE *GetTypeFields )(
            ICorDebugProcess5 * This,
            /* [in] */ COR_TYPEID id,
            ULONG32 celt,
            COR_FIELD fields[  ],
            ULONG32 *pceltNeeded);

        HRESULT ( STDMETHODCALLTYPE *EnableNGENPolicy )(
            ICorDebugProcess5 * This,
            /* [in] */ CorDebugNGENPolicy ePolicy);

        END_INTERFACE
    } ICorDebugProcess5Vtbl;

    interface ICorDebugProcess5
    {
        CONST_VTBL struct ICorDebugProcess5Vtbl *lpVtbl;
    };



#ifdef COBJMACROS


#define ICorDebugProcess5_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) )

#define ICorDebugProcess5_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) )

#define ICorDebugProcess5_Release(This)	\
    ( (This)->lpVtbl -> Release(This) )


#define ICorDebugProcess5_GetGCHeapInformation(This,pHeapInfo)	\
    ( (This)->lpVtbl -> GetGCHeapInformation(This,pHeapInfo) )

#define ICorDebugProcess5_EnumerateHeap(This,ppObjects)	\
    ( (This)->lpVtbl -> EnumerateHeap(This,ppObjects) )

#define ICorDebugProcess5_EnumerateHeapRegions(This,ppRegions)	\
    ( (This)->lpVtbl -> EnumerateHeapRegions(This,ppRegions) )

#define ICorDebugProcess5_GetObject(This,addr,pObject)	\
    ( (This)->lpVtbl -> GetObject(This,addr,pObject) )

#define ICorDebugProcess5_EnumerateGCReferences(This,enumerateWeakReferences,ppEnum)	\
    ( (This)->lpVtbl -> EnumerateGCReferences(This,enumerateWeakReferences,ppEnum) )

#define ICorDebugProcess5_EnumerateHandles(This,types,ppEnum)	\
    ( (This)->lpVtbl -> EnumerateHandles(This,types,ppEnum) )

#define ICorDebugProcess5_GetTypeID(This,obj,pId)	\
    ( (This)->lpVtbl -> GetTypeID(This,obj,pId) )

#define ICorDebugProcess5_GetTypeForTypeID(This,id,ppType)	\
    ( (This)->lpVtbl -> GetTypeForTypeID(This,id,ppType) )

#define ICorDebugProcess5_GetArrayLayout(This,id,pLayout)	\
    ( (This)->lpVtbl -> GetArrayLayout(This,id,pLayout) )

#define ICorDebugProcess5_GetTypeLayout(This,id,pLayout)	\
    ( (This)->lpVtbl -> GetTypeLayout(This,id,pLayout) )

#define ICorDebugProcess5_GetTypeFields(This,id,celt,fields,pceltNeeded)	\
    ( (This)->lpVtbl -> GetTypeFields(This,id,celt,fields,pceltNeeded) )

#define ICorDebugProcess5_EnableNGENPolicy(This,ePolicy)	\
    ( (This)->lpVtbl -> EnableNGENPolicy(This,ePolicy) )

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ICorDebugProcess5_INTERFACE_DEFINED__ */


/* interface __MIDL_itf_cordebug_0000_0040 */
/* [local] */

typedef
enum CorDebugRecordFormat
    {
        FORMAT_WINDOWS_EXCEPTIONRECORD32	= 1,
        FORMAT_WINDOWS_EXCEPTIONRECORD64	= 2
    } 	CorDebugRecordFormat;

typedef
enum CorDebugDecodeEventFlagsWindows
    {
        IS_FIRST_CHANCE	= 1
    } 	CorDebugDecodeEventFlagsWindows;

typedef
enum CorDebugDebugEventKind
    {
        DEBUG_EVENT_KIND_MODULE_LOADED	= 1,
        DEBUG_EVENT_KIND_MODULE_UNLOADED	= 2,
        DEBUG_EVENT_KIND_MANAGED_EXCEPTION_FIRST_CHANCE	= 3,
        DEBUG_EVENT_KIND_MANAGED_EXCEPTION_USER_FIRST_CHANCE	= 4,
        DEBUG_EVENT_KIND_MANAGED_EXCEPTION_CATCH_HANDLER_FOUND	= 5,
        DEBUG_EVENT_KIND_MANAGED_EXCEPTION_UNHANDLED	= 6
    } 	CorDebugDebugEventKind;

typedef
enum CorDebugStateChange
    {
        PROCESS_RUNNING	= 0x1,
        FLUSH_ALL	= 0x2
    } 	CorDebugStateChange;



extern RPC_IF_HANDLE __MIDL_itf_cordebug_0000_0040_v0_0_c_ifspec;
extern RPC_IF_HANDLE __MIDL_itf_cordebug_0000_0040_v0_0_s_ifspec;

#ifndef __ICorDebugDebugEvent_INTERFACE_DEFINED__
#define __ICorDebugDebugEvent_INTERFACE_DEFINED__

/* interface ICorDebugDebugEvent */
/* [unique][uuid][local][object] */


EXTERN_C const IID IID_ICorDebugDebugEvent;

#if defined(__cplusplus) && !defined(CINTERFACE)

    MIDL_INTERFACE("41BD395D-DE99-48F1-BF7A-CC0F44A6D281")
    ICorDebugDebugEvent : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE GetEventKind(
            /* [out] */ CorDebugDebugEventKind *pDebugEventKind) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetThread(
            /* [out] */ ICorDebugThread **ppThread) = 0;

    };


#else 	/* C style interface */

    typedef struct ICorDebugDebugEventVtbl
    {
        BEGIN_INTERFACE

        HRESULT ( STDMETHODCALLTYPE *QueryInterface )(
            ICorDebugDebugEvent * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */
            _COM_Outptr_  void **ppvObject);

        ULONG ( STDMETHODCALLTYPE *AddRef )(
            ICorDebugDebugEvent * This);

        ULONG ( STDMETHODCALLTYPE *Release )(
            ICorDebugDebugEvent * This);

        HRESULT ( STDMETHODCALLTYPE *GetEventKind )(
            ICorDebugDebugEvent * This,
            /* [out] */ CorDebugDebugEventKind *pDebugEventKind);

        HRESULT ( STDMETHODCALLTYPE *GetThread )(
            ICorDebugDebugEvent * This,
            /* [out] */ ICorDebugThread **ppThread);

        END_INTERFACE
    } ICorDebugDebugEventVtbl;

    interface ICorDebugDebugEvent
    {
        CONST_VTBL struct ICorDebugDebugEventVtbl *lpVtbl;
    };



#ifdef COBJMACROS


#define ICorDebugDebugEvent_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) )

#define ICorDebugDebugEvent_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) )

#define ICorDebugDebugEvent_Release(This)	\
    ( (This)->lpVtbl -> Release(This) )


#define ICorDebugDebugEvent_GetEventKind(This,pDebugEventKind)	\
    ( (This)->lpVtbl -> GetEventKind(This,pDebugEventKind) )

#define ICorDebugDebugEvent_GetThread(This,ppThread)	\
    ( (This)->lpVtbl -> GetThread(This,ppThread) )

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ICorDebugDebugEvent_INTERFACE_DEFINED__ */


/* interface __MIDL_itf_cordebug_0000_0041 */
/* [local] */

typedef
enum CorDebugCodeInvokeKind
    {
        CODE_INVOKE_KIND_NONE	= 0,
        CODE_INVOKE_KIND_RETURN	= ( CODE_INVOKE_KIND_NONE + 1 ) ,
        CODE_INVOKE_KIND_TAILCALL	= ( CODE_INVOKE_KIND_RETURN + 1 )
    } 	CorDebugCodeInvokeKind;

typedef
enum CorDebugCodeInvokePurpose
    {
        CODE_INVOKE_PURPOSE_NONE	= 0,
        CODE_INVOKE_PURPOSE_NATIVE_TO_MANAGED_TRANSITION	= ( CODE_INVOKE_PURPOSE_NONE + 1 ) ,
        CODE_INVOKE_PURPOSE_CLASS_INIT	= ( CODE_INVOKE_PURPOSE_NATIVE_TO_MANAGED_TRANSITION + 1 ) ,
        CODE_INVOKE_PURPOSE_INTERFACE_DISPATCH	= ( CODE_INVOKE_PURPOSE_CLASS_INIT + 1 )
    } 	CorDebugCodeInvokePurpose;



extern RPC_IF_HANDLE __MIDL_itf_cordebug_0000_0041_v0_0_c_ifspec;
extern RPC_IF_HANDLE __MIDL_itf_cordebug_0000_0041_v0_0_s_ifspec;

#ifndef __ICorDebugProcess6_INTERFACE_DEFINED__
#define __ICorDebugProcess6_INTERFACE_DEFINED__

/* interface ICorDebugProcess6 */
/* [unique][uuid][local][object] */


EXTERN_C const IID IID_ICorDebugProcess6;

#if defined(__cplusplus) && !defined(CINTERFACE)

    MIDL_INTERFACE("11588775-7205-4CEB-A41A-93753C3153E9")
    ICorDebugProcess6 : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE DecodeEvent(
            /* [size_is][length_is][in] */ const BYTE pRecord[  ],
            /* [in] */ DWORD countBytes,
            /* [in] */ CorDebugRecordFormat format,
            /* [in] */ DWORD dwFlags,
            /* [in] */ DWORD dwThreadId,
            /* [out] */ ICorDebugDebugEvent **ppEvent) = 0;

        virtual HRESULT STDMETHODCALLTYPE ProcessStateChanged(
            /* [in] */ CorDebugStateChange change) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetCode(
            /* [in] */ CORDB_ADDRESS codeAddress,
            /* [out] */ ICorDebugCode **ppCode) = 0;

        virtual HRESULT STDMETHODCALLTYPE EnableVirtualModuleSplitting(
            BOOL enableSplitting) = 0;

        virtual HRESULT STDMETHODCALLTYPE MarkDebuggerAttached(
            BOOL fIsAttached) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetExportStepInfo(
            /* [in] */ LPCWSTR pszExportName,
            /* [out] */ CorDebugCodeInvokeKind *pInvokeKind,
            /* [out] */ CorDebugCodeInvokePurpose *pInvokePurpose) = 0;

    };


#else 	/* C style interface */

    typedef struct ICorDebugProcess6Vtbl
    {
        BEGIN_INTERFACE

        HRESULT ( STDMETHODCALLTYPE *QueryInterface )(
            ICorDebugProcess6 * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */
            _COM_Outptr_  void **ppvObject);

        ULONG ( STDMETHODCALLTYPE *AddRef )(
            ICorDebugProcess6 * This);

        ULONG ( STDMETHODCALLTYPE *Release )(
            ICorDebugProcess6 * This);

        HRESULT ( STDMETHODCALLTYPE *DecodeEvent )(
            ICorDebugProcess6 * This,
            /* [size_is][length_is][in] */ const BYTE pRecord[  ],
            /* [in] */ DWORD countBytes,
            /* [in] */ CorDebugRecordFormat format,
            /* [in] */ DWORD dwFlags,
            /* [in] */ DWORD dwThreadId,
            /* [out] */ ICorDebugDebugEvent **ppEvent);

        HRESULT ( STDMETHODCALLTYPE *ProcessStateChanged )(
            ICorDebugProcess6 * This,
            /* [in] */ CorDebugStateChange change);

        HRESULT ( STDMETHODCALLTYPE *GetCode )(
            ICorDebugProcess6 * This,
            /* [in] */ CORDB_ADDRESS codeAddress,
            /* [out] */ ICorDebugCode **ppCode);

        HRESULT ( STDMETHODCALLTYPE *EnableVirtualModuleSplitting )(
            ICorDebugProcess6 * This,
            BOOL enableSplitting);

        HRESULT ( STDMETHODCALLTYPE *MarkDebuggerAttached )(
            ICorDebugProcess6 * This,
            BOOL fIsAttached);

        HRESULT ( STDMETHODCALLTYPE *GetExportStepInfo )(
            ICorDebugProcess6 * This,
            /* [in] */ LPCWSTR pszExportName,
            /* [out] */ CorDebugCodeInvokeKind *pInvokeKind,
            /* [out] */ CorDebugCodeInvokePurpose *pInvokePurpose);

        END_INTERFACE
    } ICorDebugProcess6Vtbl;

    interface ICorDebugProcess6
    {
        CONST_VTBL struct ICorDebugProcess6Vtbl *lpVtbl;
    };



#ifdef COBJMACROS


#define ICorDebugProcess6_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) )

#define ICorDebugProcess6_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) )

#define ICorDebugProcess6_Release(This)	\
    ( (This)->lpVtbl -> Release(This) )


#define ICorDebugProcess6_DecodeEvent(This,pRecord,countBytes,format,dwFlags,dwThreadId,ppEvent)	\
    ( (This)->lpVtbl -> DecodeEvent(This,pRecord,countBytes,format,dwFlags,dwThreadId,ppEvent) )

#define ICorDebugProcess6_ProcessStateChanged(This,change)	\
    ( (This)->lpVtbl -> ProcessStateChanged(This,change) )

#define ICorDebugProcess6_GetCode(This,codeAddress,ppCode)	\
    ( (This)->lpVtbl -> GetCode(This,codeAddress,ppCode) )

#define ICorDebugProcess6_EnableVirtualModuleSplitting(This,enableSplitting)	\
    ( (This)->lpVtbl -> EnableVirtualModuleSplitting(This,enableSplitting) )

#define ICorDebugProcess6_MarkDebuggerAttached(This,fIsAttached)	\
    ( (This)->lpVtbl -> MarkDebuggerAttached(This,fIsAttached) )

#define ICorDebugProcess6_GetExportStepInfo(This,pszExportName,pInvokeKind,pInvokePurpose)	\
    ( (This)->lpVtbl -> GetExportStepInfo(This,pszExportName,pInvokeKind,pInvokePurpose) )

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ICorDebugProcess6_INTERFACE_DEFINED__ */


/* interface __MIDL_itf_cordebug_0000_0042 */
/* [local] */

typedef
enum WriteableMetadataUpdateMode
    {
        LegacyCompatPolicy	= 0,
        AlwaysShowUpdates	= ( LegacyCompatPolicy + 1 )
    } 	WriteableMetadataUpdateMode;



extern RPC_IF_HANDLE __MIDL_itf_cordebug_0000_0042_v0_0_c_ifspec;
extern RPC_IF_HANDLE __MIDL_itf_cordebug_0000_0042_v0_0_s_ifspec;

#ifndef __ICorDebugProcess7_INTERFACE_DEFINED__
#define __ICorDebugProcess7_INTERFACE_DEFINED__

/* interface ICorDebugProcess7 */
/* [unique][uuid][local][object] */


EXTERN_C const IID IID_ICorDebugProcess7;

#if defined(__cplusplus) && !defined(CINTERFACE)

    MIDL_INTERFACE("9B2C54E4-119F-4D6F-B402-527603266D69")
    ICorDebugProcess7 : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE SetWriteableMetadataUpdateMode(
            WriteableMetadataUpdateMode flags) = 0;

    };


#else 	/* C style interface */

    typedef struct ICorDebugProcess7Vtbl
    {
        BEGIN_INTERFACE

        HRESULT ( STDMETHODCALLTYPE *QueryInterface )(
            ICorDebugProcess7 * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */
            _COM_Outptr_  void **ppvObject);

        ULONG ( STDMETHODCALLTYPE *AddRef )(
            ICorDebugProcess7 * This);

        ULONG ( STDMETHODCALLTYPE *Release )(
            ICorDebugProcess7 * This);

        HRESULT ( STDMETHODCALLTYPE *SetWriteableMetadataUpdateMode )(
            ICorDebugProcess7 * This,
            WriteableMetadataUpdateMode flags);

        END_INTERFACE
    } ICorDebugProcess7Vtbl;

    interface ICorDebugProcess7
    {
        CONST_VTBL struct ICorDebugProcess7Vtbl *lpVtbl;
    };



#ifdef COBJMACROS


#define ICorDebugProcess7_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) )

#define ICorDebugProcess7_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) )

#define ICorDebugProcess7_Release(This)	\
    ( (This)->lpVtbl -> Release(This) )


#define ICorDebugProcess7_SetWriteableMetadataUpdateMode(This,flags)	\
    ( (This)->lpVtbl -> SetWriteableMetadataUpdateMode(This,flags) )

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ICorDebugProcess7_INTERFACE_DEFINED__ */


#ifndef __ICorDebugProcess8_INTERFACE_DEFINED__
#define __ICorDebugProcess8_INTERFACE_DEFINED__

/* interface ICorDebugProcess8 */
/* [unique][uuid][local][object] */


EXTERN_C const IID IID_ICorDebugProcess8;

#if defined(__cplusplus) && !defined(CINTERFACE)

    MIDL_INTERFACE("2E6F28C1-85EB-4141-80AD-0A90944B9639")
    ICorDebugProcess8 : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE EnableExceptionCallbacksOutsideOfMyCode(
            /* [in] */ BOOL enableExceptionsOutsideOfJMC) = 0;

    };


#else 	/* C style interface */

    typedef struct ICorDebugProcess8Vtbl
    {
        BEGIN_INTERFACE

        HRESULT ( STDMETHODCALLTYPE *QueryInterface )(
            ICorDebugProcess8 * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */
            _COM_Outptr_  void **ppvObject);

        ULONG ( STDMETHODCALLTYPE *AddRef )(
            ICorDebugProcess8 * This);

        ULONG ( STDMETHODCALLTYPE *Release )(
            ICorDebugProcess8 * This);

        HRESULT ( STDMETHODCALLTYPE *EnableExceptionCallbacksOutsideOfMyCode )(
            ICorDebugProcess8 * This,
            /* [in] */ BOOL enableExceptionsOutsideOfJMC);

        END_INTERFACE
    } ICorDebugProcess8Vtbl;

    interface ICorDebugProcess8
    {
        CONST_VTBL struct ICorDebugProcess8Vtbl *lpVtbl;
    };



#ifdef COBJMACROS


#define ICorDebugProcess8_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) )

#define ICorDebugProcess8_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) )

#define ICorDebugProcess8_Release(This)	\
    ( (This)->lpVtbl -> Release(This) )


#define ICorDebugProcess8_EnableExceptionCallbacksOutsideOfMyCode(This,enableExceptionsOutsideOfJMC)	\
    ( (This)->lpVtbl -> EnableExceptionCallbacksOutsideOfMyCode(This,enableExceptionsOutsideOfJMC) )

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ICorDebugProcess8_INTERFACE_DEFINED__ */


#ifndef __ICorDebugProcess10_INTERFACE_DEFINED__
#define __ICorDebugProcess10_INTERFACE_DEFINED__

/* interface ICorDebugProcess10 */
/* [unique][uuid][local][object] */


EXTERN_C const IID IID_ICorDebugProcess10;

#if defined(__cplusplus) && !defined(CINTERFACE)

    MIDL_INTERFACE("8F378F6F-1017-4461-9890-ECF64C54079F")
    ICorDebugProcess10 : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE EnableGCNotificationEvents(
            BOOL fEnable) = 0;

    };


#else 	/* C style interface */

    typedef struct ICorDebugProcess10Vtbl
    {
        BEGIN_INTERFACE

        HRESULT ( STDMETHODCALLTYPE *QueryInterface )(
            ICorDebugProcess10 * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */
            _COM_Outptr_  void **ppvObject);

        ULONG ( STDMETHODCALLTYPE *AddRef )(
            ICorDebugProcess10 * This);

        ULONG ( STDMETHODCALLTYPE *Release )(
            ICorDebugProcess10 * This);

        HRESULT ( STDMETHODCALLTYPE *EnableGCNotificationEvents )(
            ICorDebugProcess10 * This,
            BOOL fEnable);

        END_INTERFACE
    } ICorDebugProcess10Vtbl;

    interface ICorDebugProcess10
    {
        CONST_VTBL struct ICorDebugProcess10Vtbl *lpVtbl;
    };



#ifdef COBJMACROS


#define ICorDebugProcess10_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) )

#define ICorDebugProcess10_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) )

#define ICorDebugProcess10_Release(This)	\
    ( (This)->lpVtbl -> Release(This) )


#define ICorDebugProcess10_EnableGCNotificationEvents(This,fEnable)	\
    ( (This)->lpVtbl -> EnableGCNotificationEvents(This,fEnable) )

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ICorDebugProcess10_INTERFACE_DEFINED__ */


/* interface __MIDL_itf_cordebug_0000_0045 */
/* [local] */

typedef struct _COR_MEMORY_RANGE
    {
    CORDB_ADDRESS start;
    CORDB_ADDRESS end;
    } 	COR_MEMORY_RANGE;



extern RPC_IF_HANDLE __MIDL_itf_cordebug_0000_0045_v0_0_c_ifspec;
extern RPC_IF_HANDLE __MIDL_itf_cordebug_0000_0045_v0_0_s_ifspec;

#ifndef __ICorDebugMemoryRangeEnum_INTERFACE_DEFINED__
#define __ICorDebugMemoryRangeEnum_INTERFACE_DEFINED__

/* interface ICorDebugMemoryRangeEnum */
/* [unique][uuid][local][object] */


EXTERN_C const IID IID_ICorDebugMemoryRangeEnum;

#if defined(__cplusplus) && !defined(CINTERFACE)

    MIDL_INTERFACE("D1A0BCFC-5865-4437-BE3F-36F022951F8A")
    ICorDebugMemoryRangeEnum : public ICorDebugEnum
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE Next(
            /* [in] */ ULONG celt,
            /* [length_is][size_is][out] */ COR_MEMORY_RANGE objects[  ],
            /* [out] */ ULONG *pceltFetched) = 0;

    };


#else 	/* C style interface */

    typedef struct ICorDebugMemoryRangeEnumVtbl
    {
        BEGIN_INTERFACE

        HRESULT ( STDMETHODCALLTYPE *QueryInterface )(
            ICorDebugMemoryRangeEnum * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */
            _COM_Outptr_  void **ppvObject);

        ULONG ( STDMETHODCALLTYPE *AddRef )(
            ICorDebugMemoryRangeEnum * This);

        ULONG ( STDMETHODCALLTYPE *Release )(
            ICorDebugMemoryRangeEnum * This);

        HRESULT ( STDMETHODCALLTYPE *Skip )(
            ICorDebugMemoryRangeEnum * This,
            /* [in] */ ULONG celt);

        HRESULT ( STDMETHODCALLTYPE *Reset )(
            ICorDebugMemoryRangeEnum * This);

        HRESULT ( STDMETHODCALLTYPE *Clone )(
            ICorDebugMemoryRangeEnum * This,
            /* [out] */ ICorDebugEnum **ppEnum);

        HRESULT ( STDMETHODCALLTYPE *GetCount )(
            ICorDebugMemoryRangeEnum * This,
            /* [out] */ ULONG *pcelt);

        HRESULT ( STDMETHODCALLTYPE *Next )(
            ICorDebugMemoryRangeEnum * This,
            /* [in] */ ULONG celt,
            /* [length_is][size_is][out] */ COR_MEMORY_RANGE objects[  ],
            /* [out] */ ULONG *pceltFetched);

        END_INTERFACE
    } ICorDebugMemoryRangeEnumVtbl;

    interface ICorDebugMemoryRangeEnum
    {
        CONST_VTBL struct ICorDebugMemoryRangeEnumVtbl *lpVtbl;
    };



#ifdef COBJMACROS


#define ICorDebugMemoryRangeEnum_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) )

#define ICorDebugMemoryRangeEnum_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) )

#define ICorDebugMemoryRangeEnum_Release(This)	\
    ( (This)->lpVtbl -> Release(This) )


#define ICorDebugMemoryRangeEnum_Skip(This,celt)	\
    ( (This)->lpVtbl -> Skip(This,celt) )

#define ICorDebugMemoryRangeEnum_Reset(This)	\
    ( (This)->lpVtbl -> Reset(This) )

#define ICorDebugMemoryRangeEnum_Clone(This,ppEnum)	\
    ( (This)->lpVtbl -> Clone(This,ppEnum) )

#define ICorDebugMemoryRangeEnum_GetCount(This,pcelt)	\
    ( (This)->lpVtbl -> GetCount(This,pcelt) )


#define ICorDebugMemoryRangeEnum_Next(This,celt,objects,pceltFetched)	\
    ( (This)->lpVtbl -> Next(This,celt,objects,pceltFetched) )

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ICorDebugMemoryRangeEnum_INTERFACE_DEFINED__ */


#ifndef __ICorDebugProcess11_INTERFACE_DEFINED__
#define __ICorDebugProcess11_INTERFACE_DEFINED__

/* interface ICorDebugProcess11 */
/* [unique][uuid][local][object] */


EXTERN_C const IID IID_ICorDebugProcess11;

#if defined(__cplusplus) && !defined(CINTERFACE)

    MIDL_INTERFACE("344B37AA-F2C0-4D3B-9909-91CCF787DA8C")
    ICorDebugProcess11 : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE EnumerateLoaderHeapMemoryRegions(
            /* [out] */ ICorDebugMemoryRangeEnum **ppRanges) = 0;

    };


#else 	/* C style interface */

    typedef struct ICorDebugProcess11Vtbl
    {
        BEGIN_INTERFACE

        HRESULT ( STDMETHODCALLTYPE *QueryInterface )(
            ICorDebugProcess11 * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */
            _COM_Outptr_  void **ppvObject);

        ULONG ( STDMETHODCALLTYPE *AddRef )(
            ICorDebugProcess11 * This);

        ULONG ( STDMETHODCALLTYPE *Release )(
            ICorDebugProcess11 * This);

        HRESULT ( STDMETHODCALLTYPE *EnumerateLoaderHeapMemoryRegions )(
            ICorDebugProcess11 * This,
            /* [out] */ ICorDebugMemoryRangeEnum **ppRanges);

        END_INTERFACE
    } ICorDebugProcess11Vtbl;

    interface ICorDebugProcess11
    {
        CONST_VTBL struct ICorDebugProcess11Vtbl *lpVtbl;
    };



#ifdef COBJMACROS


#define ICorDebugProcess11_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) )

#define ICorDebugProcess11_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) )

#define ICorDebugProcess11_Release(This)	\
    ( (This)->lpVtbl -> Release(This) )


#define ICorDebugProcess11_EnumerateLoaderHeapMemoryRegions(This,ppRanges)	\
    ( (This)->lpVtbl -> EnumerateLoaderHeapMemoryRegions(This,ppRanges) )

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ICorDebugProcess11_INTERFACE_DEFINED__ */


#ifndef __ICorDebugModuleDebugEvent_INTERFACE_DEFINED__
#define __ICorDebugModuleDebugEvent_INTERFACE_DEFINED__

/* interface ICorDebugModuleDebugEvent */
/* [unique][uuid][local][object] */


EXTERN_C const IID IID_ICorDebugModuleDebugEvent;

#if defined(__cplusplus) && !defined(CINTERFACE)

    MIDL_INTERFACE("51A15E8D-9FFF-4864-9B87-F4FBDEA747A2")
    ICorDebugModuleDebugEvent : public ICorDebugDebugEvent
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE GetModule(
            /* [out] */ ICorDebugModule **ppModule) = 0;

    };


#else 	/* C style interface */

    typedef struct ICorDebugModuleDebugEventVtbl
    {
        BEGIN_INTERFACE

        HRESULT ( STDMETHODCALLTYPE *QueryInterface )(
            ICorDebugModuleDebugEvent * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */
            _COM_Outptr_  void **ppvObject);

        ULONG ( STDMETHODCALLTYPE *AddRef )(
            ICorDebugModuleDebugEvent * This);

        ULONG ( STDMETHODCALLTYPE *Release )(
            ICorDebugModuleDebugEvent * This);

        HRESULT ( STDMETHODCALLTYPE *GetEventKind )(
            ICorDebugModuleDebugEvent * This,
            /* [out] */ CorDebugDebugEventKind *pDebugEventKind);

        HRESULT ( STDMETHODCALLTYPE *GetThread )(
            ICorDebugModuleDebugEvent * This,
            /* [out] */ ICorDebugThread **ppThread);

        HRESULT ( STDMETHODCALLTYPE *GetModule )(
            ICorDebugModuleDebugEvent * This,
            /* [out] */ ICorDebugModule **ppModule);

        END_INTERFACE
    } ICorDebugModuleDebugEventVtbl;

    interface ICorDebugModuleDebugEvent
    {
        CONST_VTBL struct ICorDebugModuleDebugEventVtbl *lpVtbl;
    };



#ifdef COBJMACROS


#define ICorDebugModuleDebugEvent_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) )

#define ICorDebugModuleDebugEvent_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) )

#define ICorDebugModuleDebugEvent_Release(This)	\
    ( (This)->lpVtbl -> Release(This) )


#define ICorDebugModuleDebugEvent_GetEventKind(This,pDebugEventKind)	\
    ( (This)->lpVtbl -> GetEventKind(This,pDebugEventKind) )

#define ICorDebugModuleDebugEvent_GetThread(This,ppThread)	\
    ( (This)->lpVtbl -> GetThread(This,ppThread) )


#define ICorDebugModuleDebugEvent_GetModule(This,ppModule)	\
    ( (This)->lpVtbl -> GetModule(This,ppModule) )

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ICorDebugModuleDebugEvent_INTERFACE_DEFINED__ */


#ifndef __ICorDebugExceptionDebugEvent_INTERFACE_DEFINED__
#define __ICorDebugExceptionDebugEvent_INTERFACE_DEFINED__

/* interface ICorDebugExceptionDebugEvent */
/* [unique][uuid][local][object] */


EXTERN_C const IID IID_ICorDebugExceptionDebugEvent;

#if defined(__cplusplus) && !defined(CINTERFACE)

    MIDL_INTERFACE("AF79EC94-4752-419C-A626-5FB1CC1A5AB7")
    ICorDebugExceptionDebugEvent : public ICorDebugDebugEvent
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE GetStackPointer(
            /* [out] */ CORDB_ADDRESS *pStackPointer) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetNativeIP(
            /* [out] */ CORDB_ADDRESS *pIP) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetFlags(
            /* [out] */ CorDebugExceptionFlags *pdwFlags) = 0;

    };


#else 	/* C style interface */

    typedef struct ICorDebugExceptionDebugEventVtbl
    {
        BEGIN_INTERFACE

        HRESULT ( STDMETHODCALLTYPE *QueryInterface )(
            ICorDebugExceptionDebugEvent * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */
            _COM_Outptr_  void **ppvObject);

        ULONG ( STDMETHODCALLTYPE *AddRef )(
            ICorDebugExceptionDebugEvent * This);

        ULONG ( STDMETHODCALLTYPE *Release )(
            ICorDebugExceptionDebugEvent * This);

        HRESULT ( STDMETHODCALLTYPE *GetEventKind )(
            ICorDebugExceptionDebugEvent * This,
            /* [out] */ CorDebugDebugEventKind *pDebugEventKind);

        HRESULT ( STDMETHODCALLTYPE *GetThread )(
            ICorDebugExceptionDebugEvent * This,
            /* [out] */ ICorDebugThread **ppThread);

        HRESULT ( STDMETHODCALLTYPE *GetStackPointer )(
            ICorDebugExceptionDebugEvent * This,
            /* [out] */ CORDB_ADDRESS *pStackPointer);

        HRESULT ( STDMETHODCALLTYPE *GetNativeIP )(
            ICorDebugExceptionDebugEvent * This,
            /* [out] */ CORDB_ADDRESS *pIP);

        HRESULT ( STDMETHODCALLTYPE *GetFlags )(
            ICorDebugExceptionDebugEvent * This,
            /* [out] */ CorDebugExceptionFlags *pdwFlags);

        END_INTERFACE
    } ICorDebugExceptionDebugEventVtbl;

    interface ICorDebugExceptionDebugEvent
    {
        CONST_VTBL struct ICorDebugExceptionDebugEventVtbl *lpVtbl;
    };



#ifdef COBJMACROS


#define ICorDebugExceptionDebugEvent_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) )

#define ICorDebugExceptionDebugEvent_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) )

#define ICorDebugExceptionDebugEvent_Release(This)	\
    ( (This)->lpVtbl -> Release(This) )


#define ICorDebugExceptionDebugEvent_GetEventKind(This,pDebugEventKind)	\
    ( (This)->lpVtbl -> GetEventKind(This,pDebugEventKind) )

#define ICorDebugExceptionDebugEvent_GetThread(This,ppThread)	\
    ( (This)->lpVtbl -> GetThread(This,ppThread) )


#define ICorDebugExceptionDebugEvent_GetStackPointer(This,pStackPointer)	\
    ( (This)->lpVtbl -> GetStackPointer(This,pStackPointer) )

#define ICorDebugExceptionDebugEvent_GetNativeIP(This,pIP)	\
    ( (This)->lpVtbl -> GetNativeIP(This,pIP) )

#define ICorDebugExceptionDebugEvent_GetFlags(This,pdwFlags)	\
    ( (This)->lpVtbl -> GetFlags(This,pdwFlags) )

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ICorDebugExceptionDebugEvent_INTERFACE_DEFINED__ */


#ifndef __ICorDebugBreakpoint_INTERFACE_DEFINED__
#define __ICorDebugBreakpoint_INTERFACE_DEFINED__

/* interface ICorDebugBreakpoint */
/* [unique][uuid][local][object] */


EXTERN_C const IID IID_ICorDebugBreakpoint;

#if defined(__cplusplus) && !defined(CINTERFACE)

    MIDL_INTERFACE("CC7BCAE8-8A68-11d2-983C-0000F808342D")
    ICorDebugBreakpoint : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE Activate(
            /* [in] */ BOOL bActive) = 0;

        virtual HRESULT STDMETHODCALLTYPE IsActive(
            /* [out] */ BOOL *pbActive) = 0;

    };


#else 	/* C style interface */

    typedef struct ICorDebugBreakpointVtbl
    {
        BEGIN_INTERFACE

        HRESULT ( STDMETHODCALLTYPE *QueryInterface )(
            ICorDebugBreakpoint * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */
            _COM_Outptr_  void **ppvObject);

        ULONG ( STDMETHODCALLTYPE *AddRef )(
            ICorDebugBreakpoint * This);

        ULONG ( STDMETHODCALLTYPE *Release )(
            ICorDebugBreakpoint * This);

        HRESULT ( STDMETHODCALLTYPE *Activate )(
            ICorDebugBreakpoint * This,
            /* [in] */ BOOL bActive);

        HRESULT ( STDMETHODCALLTYPE *IsActive )(
            ICorDebugBreakpoint * This,
            /* [out] */ BOOL *pbActive);

        END_INTERFACE
    } ICorDebugBreakpointVtbl;

    interface ICorDebugBreakpoint
    {
        CONST_VTBL struct ICorDebugBreakpointVtbl *lpVtbl;
    };



#ifdef COBJMACROS


#define ICorDebugBreakpoint_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) )

#define ICorDebugBreakpoint_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) )

#define ICorDebugBreakpoint_Release(This)	\
    ( (This)->lpVtbl -> Release(This) )


#define ICorDebugBreakpoint_Activate(This,bActive)	\
    ( (This)->lpVtbl -> Activate(This,bActive) )

#define ICorDebugBreakpoint_IsActive(This,pbActive)	\
    ( (This)->lpVtbl -> IsActive(This,pbActive) )

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ICorDebugBreakpoint_INTERFACE_DEFINED__ */


#ifndef __ICorDebugFunctionBreakpoint_INTERFACE_DEFINED__
#define __ICorDebugFunctionBreakpoint_INTERFACE_DEFINED__

/* interface ICorDebugFunctionBreakpoint */
/* [unique][uuid][local][object] */


EXTERN_C const IID IID_ICorDebugFunctionBreakpoint;

#if defined(__cplusplus) && !defined(CINTERFACE)

    MIDL_INTERFACE("CC7BCAE9-8A68-11d2-983C-0000F808342D")
    ICorDebugFunctionBreakpoint : public ICorDebugBreakpoint
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE GetFunction(
            /* [out] */ ICorDebugFunction **ppFunction) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetOffset(
            /* [out] */ ULONG32 *pnOffset) = 0;

    };


#else 	/* C style interface */

    typedef struct ICorDebugFunctionBreakpointVtbl
    {
        BEGIN_INTERFACE

        HRESULT ( STDMETHODCALLTYPE *QueryInterface )(
            ICorDebugFunctionBreakpoint * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */
            _COM_Outptr_  void **ppvObject);

        ULONG ( STDMETHODCALLTYPE *AddRef )(
            ICorDebugFunctionBreakpoint * This);

        ULONG ( STDMETHODCALLTYPE *Release )(
            ICorDebugFunctionBreakpoint * This);

        HRESULT ( STDMETHODCALLTYPE *Activate )(
            ICorDebugFunctionBreakpoint * This,
            /* [in] */ BOOL bActive);

        HRESULT ( STDMETHODCALLTYPE *IsActive )(
            ICorDebugFunctionBreakpoint * This,
            /* [out] */ BOOL *pbActive);

        HRESULT ( STDMETHODCALLTYPE *GetFunction )(
            ICorDebugFunctionBreakpoint * This,
            /* [out] */ ICorDebugFunction **ppFunction);

        HRESULT ( STDMETHODCALLTYPE *GetOffset )(
            ICorDebugFunctionBreakpoint * This,
            /* [out] */ ULONG32 *pnOffset);

        END_INTERFACE
    } ICorDebugFunctionBreakpointVtbl;

    interface ICorDebugFunctionBreakpoint
    {
        CONST_VTBL struct ICorDebugFunctionBreakpointVtbl *lpVtbl;
    };



#ifdef COBJMACROS


#define ICorDebugFunctionBreakpoint_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) )

#define ICorDebugFunctionBreakpoint_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) )

#define ICorDebugFunctionBreakpoint_Release(This)	\
    ( (This)->lpVtbl -> Release(This) )


#define ICorDebugFunctionBreakpoint_Activate(This,bActive)	\
    ( (This)->lpVtbl -> Activate(This,bActive) )

#define ICorDebugFunctionBreakpoint_IsActive(This,pbActive)	\
    ( (This)->lpVtbl -> IsActive(This,pbActive) )


#define ICorDebugFunctionBreakpoint_GetFunction(This,ppFunction)	\
    ( (This)->lpVtbl -> GetFunction(This,ppFunction) )

#define ICorDebugFunctionBreakpoint_GetOffset(This,pnOffset)	\
    ( (This)->lpVtbl -> GetOffset(This,pnOffset) )

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ICorDebugFunctionBreakpoint_INTERFACE_DEFINED__ */


#ifndef __ICorDebugModuleBreakpoint_INTERFACE_DEFINED__
#define __ICorDebugModuleBreakpoint_INTERFACE_DEFINED__

/* interface ICorDebugModuleBreakpoint */
/* [unique][uuid][local][object] */


EXTERN_C const IID IID_ICorDebugModuleBreakpoint;

#if defined(__cplusplus) && !defined(CINTERFACE)

    MIDL_INTERFACE("CC7BCAEA-8A68-11d2-983C-0000F808342D")
    ICorDebugModuleBreakpoint : public ICorDebugBreakpoint
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE GetModule(
            /* [out] */ ICorDebugModule **ppModule) = 0;

    };


#else 	/* C style interface */

    typedef struct ICorDebugModuleBreakpointVtbl
    {
        BEGIN_INTERFACE

        HRESULT ( STDMETHODCALLTYPE *QueryInterface )(
            ICorDebugModuleBreakpoint * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */
            _COM_Outptr_  void **ppvObject);

        ULONG ( STDMETHODCALLTYPE *AddRef )(
            ICorDebugModuleBreakpoint * This);

        ULONG ( STDMETHODCALLTYPE *Release )(
            ICorDebugModuleBreakpoint * This);

        HRESULT ( STDMETHODCALLTYPE *Activate )(
            ICorDebugModuleBreakpoint * This,
            /* [in] */ BOOL bActive);

        HRESULT ( STDMETHODCALLTYPE *IsActive )(
            ICorDebugModuleBreakpoint * This,
            /* [out] */ BOOL *pbActive);

        HRESULT ( STDMETHODCALLTYPE *GetModule )(
            ICorDebugModuleBreakpoint * This,
            /* [out] */ ICorDebugModule **ppModule);

        END_INTERFACE
    } ICorDebugModuleBreakpointVtbl;

    interface ICorDebugModuleBreakpoint
    {
        CONST_VTBL struct ICorDebugModuleBreakpointVtbl *lpVtbl;
    };



#ifdef COBJMACROS


#define ICorDebugModuleBreakpoint_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) )

#define ICorDebugModuleBreakpoint_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) )

#define ICorDebugModuleBreakpoint_Release(This)	\
    ( (This)->lpVtbl -> Release(This) )


#define ICorDebugModuleBreakpoint_Activate(This,bActive)	\
    ( (This)->lpVtbl -> Activate(This,bActive) )

#define ICorDebugModuleBreakpoint_IsActive(This,pbActive)	\
    ( (This)->lpVtbl -> IsActive(This,pbActive) )


#define ICorDebugModuleBreakpoint_GetModule(This,ppModule)	\
    ( (This)->lpVtbl -> GetModule(This,ppModule) )

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ICorDebugModuleBreakpoint_INTERFACE_DEFINED__ */


#ifndef __ICorDebugValueBreakpoint_INTERFACE_DEFINED__
#define __ICorDebugValueBreakpoint_INTERFACE_DEFINED__

/* interface ICorDebugValueBreakpoint */
/* [unique][uuid][local][object] */


EXTERN_C const IID IID_ICorDebugValueBreakpoint;

#if defined(__cplusplus) && !defined(CINTERFACE)

    MIDL_INTERFACE("CC7BCAEB-8A68-11d2-983C-0000F808342D")
    ICorDebugValueBreakpoint : public ICorDebugBreakpoint
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE GetValue(
            /* [out] */ ICorDebugValue **ppValue) = 0;

    };


#else 	/* C style interface */

    typedef struct ICorDebugValueBreakpointVtbl
    {
        BEGIN_INTERFACE

        HRESULT ( STDMETHODCALLTYPE *QueryInterface )(
            ICorDebugValueBreakpoint * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */
            _COM_Outptr_  void **ppvObject);

        ULONG ( STDMETHODCALLTYPE *AddRef )(
            ICorDebugValueBreakpoint * This);

        ULONG ( STDMETHODCALLTYPE *Release )(
            ICorDebugValueBreakpoint * This);

        HRESULT ( STDMETHODCALLTYPE *Activate )(
            ICorDebugValueBreakpoint * This,
            /* [in] */ BOOL bActive);

        HRESULT ( STDMETHODCALLTYPE *IsActive )(
            ICorDebugValueBreakpoint * This,
            /* [out] */ BOOL *pbActive);

        HRESULT ( STDMETHODCALLTYPE *GetValue )(
            ICorDebugValueBreakpoint * This,
            /* [out] */ ICorDebugValue **ppValue);

        END_INTERFACE
    } ICorDebugValueBreakpointVtbl;

    interface ICorDebugValueBreakpoint
    {
        CONST_VTBL struct ICorDebugValueBreakpointVtbl *lpVtbl;
    };



#ifdef COBJMACROS


#define ICorDebugValueBreakpoint_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) )

#define ICorDebugValueBreakpoint_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) )

#define ICorDebugValueBreakpoint_Release(This)	\
    ( (This)->lpVtbl -> Release(This) )


#define ICorDebugValueBreakpoint_Activate(This,bActive)	\
    ( (This)->lpVtbl -> Activate(This,bActive) )

#define ICorDebugValueBreakpoint_IsActive(This,pbActive)	\
    ( (This)->lpVtbl -> IsActive(This,pbActive) )


#define ICorDebugValueBreakpoint_GetValue(This,ppValue)	\
    ( (This)->lpVtbl -> GetValue(This,ppValue) )

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ICorDebugValueBreakpoint_INTERFACE_DEFINED__ */


#ifndef __ICorDebugStepper_INTERFACE_DEFINED__
#define __ICorDebugStepper_INTERFACE_DEFINED__

/* interface ICorDebugStepper */
/* [unique][uuid][local][object] */

typedef
enum CorDebugIntercept
    {
        INTERCEPT_NONE	= 0,
        INTERCEPT_CLASS_INIT	= 0x1,
        INTERCEPT_EXCEPTION_FILTER	= 0x2,
        INTERCEPT_SECURITY	= 0x4,
        INTERCEPT_CONTEXT_POLICY	= 0x8,
        INTERCEPT_INTERCEPTION	= 0x10,
        INTERCEPT_ALL	= 0xffff
    } 	CorDebugIntercept;

typedef
enum CorDebugUnmappedStop
    {
        STOP_NONE	= 0,
        STOP_PROLOG	= 0x1,
        STOP_EPILOG	= 0x2,
        STOP_NO_MAPPING_INFO	= 0x4,
        STOP_OTHER_UNMAPPED	= 0x8,
        STOP_UNMANAGED	= 0x10,
        STOP_ALL	= 0xffff
    } 	CorDebugUnmappedStop;

typedef struct COR_DEBUG_STEP_RANGE
    {
    ULONG32 startOffset;
    ULONG32 endOffset;
    } 	COR_DEBUG_STEP_RANGE;


EXTERN_C const IID IID_ICorDebugStepper;

#if defined(__cplusplus) && !defined(CINTERFACE)

    MIDL_INTERFACE("CC7BCAEC-8A68-11d2-983C-0000F808342D")
    ICorDebugStepper : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE IsActive(
            /* [out] */ BOOL *pbActive) = 0;

        virtual HRESULT STDMETHODCALLTYPE Deactivate( void) = 0;

        virtual HRESULT STDMETHODCALLTYPE SetInterceptMask(
            /* [in] */ CorDebugIntercept mask) = 0;

        virtual HRESULT STDMETHODCALLTYPE SetUnmappedStopMask(
            /* [in] */ CorDebugUnmappedStop mask) = 0;

        virtual HRESULT STDMETHODCALLTYPE Step(
            /* [in] */ BOOL bStepIn) = 0;

        virtual HRESULT STDMETHODCALLTYPE StepRange(
            /* [in] */ BOOL bStepIn,
            /* [size_is][in] */ COR_DEBUG_STEP_RANGE ranges[  ],
            /* [in] */ ULONG32 cRangeCount) = 0;

        virtual HRESULT STDMETHODCALLTYPE StepOut( void) = 0;

        virtual HRESULT STDMETHODCALLTYPE SetRangeIL(
            /* [in] */ BOOL bIL) = 0;

    };


#else 	/* C style interface */

    typedef struct ICorDebugStepperVtbl
    {
        BEGIN_INTERFACE

        HRESULT ( STDMETHODCALLTYPE *QueryInterface )(
            ICorDebugStepper * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */
            _COM_Outptr_  void **ppvObject);

        ULONG ( STDMETHODCALLTYPE *AddRef )(
            ICorDebugStepper * This);

        ULONG ( STDMETHODCALLTYPE *Release )(
            ICorDebugStepper * This);

        HRESULT ( STDMETHODCALLTYPE *IsActive )(
            ICorDebugStepper * This,
            /* [out] */ BOOL *pbActive);

        HRESULT ( STDMETHODCALLTYPE *Deactivate )(
            ICorDebugStepper * This);

        HRESULT ( STDMETHODCALLTYPE *SetInterceptMask )(
            ICorDebugStepper * This,
            /* [in] */ CorDebugIntercept mask);

        HRESULT ( STDMETHODCALLTYPE *SetUnmappedStopMask )(
            ICorDebugStepper * This,
            /* [in] */ CorDebugUnmappedStop mask);

        HRESULT ( STDMETHODCALLTYPE *Step )(
            ICorDebugStepper * This,
            /* [in] */ BOOL bStepIn);

        HRESULT ( STDMETHODCALLTYPE *StepRange )(
            ICorDebugStepper * This,
            /* [in] */ BOOL bStepIn,
            /* [size_is][in] */ COR_DEBUG_STEP_RANGE ranges[  ],
            /* [in] */ ULONG32 cRangeCount);

        HRESULT ( STDMETHODCALLTYPE *StepOut )(
            ICorDebugStepper * This);

        HRESULT ( STDMETHODCALLTYPE *SetRangeIL )(
            ICorDebugStepper * This,
            /* [in] */ BOOL bIL);

        END_INTERFACE
    } ICorDebugStepperVtbl;

    interface ICorDebugStepper
    {
        CONST_VTBL struct ICorDebugStepperVtbl *lpVtbl;
    };



#ifdef COBJMACROS


#define ICorDebugStepper_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) )

#define ICorDebugStepper_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) )

#define ICorDebugStepper_Release(This)	\
    ( (This)->lpVtbl -> Release(This) )


#define ICorDebugStepper_IsActive(This,pbActive)	\
    ( (This)->lpVtbl -> IsActive(This,pbActive) )

#define ICorDebugStepper_Deactivate(This)	\
    ( (This)->lpVtbl -> Deactivate(This) )

#define ICorDebugStepper_SetInterceptMask(This,mask)	\
    ( (This)->lpVtbl -> SetInterceptMask(This,mask) )

#define ICorDebugStepper_SetUnmappedStopMask(This,mask)	\
    ( (This)->lpVtbl -> SetUnmappedStopMask(This,mask) )

#define ICorDebugStepper_Step(This,bStepIn)	\
    ( (This)->lpVtbl -> Step(This,bStepIn) )

#define ICorDebugStepper_StepRange(This,bStepIn,ranges,cRangeCount)	\
    ( (This)->lpVtbl -> StepRange(This,bStepIn,ranges,cRangeCount) )

#define ICorDebugStepper_StepOut(This)	\
    ( (This)->lpVtbl -> StepOut(This) )

#define ICorDebugStepper_SetRangeIL(This,bIL)	\
    ( (This)->lpVtbl -> SetRangeIL(This,bIL) )

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ICorDebugStepper_INTERFACE_DEFINED__ */


#ifndef __ICorDebugStepper2_INTERFACE_DEFINED__
#define __ICorDebugStepper2_INTERFACE_DEFINED__

/* interface ICorDebugStepper2 */
/* [unique][uuid][local][object] */


EXTERN_C const IID IID_ICorDebugStepper2;

#if defined(__cplusplus) && !defined(CINTERFACE)

    MIDL_INTERFACE("C5B6E9C3-E7D1-4a8e-873B-7F047F0706F7")
    ICorDebugStepper2 : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE SetJMC(
            /* [in] */ BOOL fIsJMCStepper) = 0;

    };


#else 	/* C style interface */

    typedef struct ICorDebugStepper2Vtbl
    {
        BEGIN_INTERFACE

        HRESULT ( STDMETHODCALLTYPE *QueryInterface )(
            ICorDebugStepper2 * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */
            _COM_Outptr_  void **ppvObject);

        ULONG ( STDMETHODCALLTYPE *AddRef )(
            ICorDebugStepper2 * This);

        ULONG ( STDMETHODCALLTYPE *Release )(
            ICorDebugStepper2 * This);

        HRESULT ( STDMETHODCALLTYPE *SetJMC )(
            ICorDebugStepper2 * This,
            /* [in] */ BOOL fIsJMCStepper);

        END_INTERFACE
    } ICorDebugStepper2Vtbl;

    interface ICorDebugStepper2
    {
        CONST_VTBL struct ICorDebugStepper2Vtbl *lpVtbl;
    };



#ifdef COBJMACROS


#define ICorDebugStepper2_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) )

#define ICorDebugStepper2_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) )

#define ICorDebugStepper2_Release(This)	\
    ( (This)->lpVtbl -> Release(This) )


#define ICorDebugStepper2_SetJMC(This,fIsJMCStepper)	\
    ( (This)->lpVtbl -> SetJMC(This,fIsJMCStepper) )

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ICorDebugStepper2_INTERFACE_DEFINED__ */


#ifndef __ICorDebugRegisterSet_INTERFACE_DEFINED__
#define __ICorDebugRegisterSet_INTERFACE_DEFINED__

/* interface ICorDebugRegisterSet */
/* [unique][uuid][local][object] */

typedef
enum CorDebugRegister
    {
        REGISTER_INSTRUCTION_POINTER	= 0,
        REGISTER_STACK_POINTER	= ( REGISTER_INSTRUCTION_POINTER + 1 ) ,
        REGISTER_FRAME_POINTER	= ( REGISTER_STACK_POINTER + 1 ) ,
        REGISTER_X86_EIP	= 0,
        REGISTER_X86_ESP	= ( REGISTER_X86_EIP + 1 ) ,
        REGISTER_X86_EBP	= ( REGISTER_X86_ESP + 1 ) ,
        REGISTER_X86_EAX	= ( REGISTER_X86_EBP + 1 ) ,
        REGISTER_X86_ECX	= ( REGISTER_X86_EAX + 1 ) ,
        REGISTER_X86_EDX	= ( REGISTER_X86_ECX + 1 ) ,
        REGISTER_X86_EBX	= ( REGISTER_X86_EDX + 1 ) ,
        REGISTER_X86_ESI	= ( REGISTER_X86_EBX + 1 ) ,
        REGISTER_X86_EDI	= ( REGISTER_X86_ESI + 1 ) ,
        REGISTER_X86_FPSTACK_0	= ( REGISTER_X86_EDI + 1 ) ,
        REGISTER_X86_FPSTACK_1	= ( REGISTER_X86_FPSTACK_0 + 1 ) ,
        REGISTER_X86_FPSTACK_2	= ( REGISTER_X86_FPSTACK_1 + 1 ) ,
        REGISTER_X86_FPSTACK_3	= ( REGISTER_X86_FPSTACK_2 + 1 ) ,
        REGISTER_X86_FPSTACK_4	= ( REGISTER_X86_FPSTACK_3 + 1 ) ,
        REGISTER_X86_FPSTACK_5	= ( REGISTER_X86_FPSTACK_4 + 1 ) ,
        REGISTER_X86_FPSTACK_6	= ( REGISTER_X86_FPSTACK_5 + 1 ) ,
        REGISTER_X86_FPSTACK_7	= ( REGISTER_X86_FPSTACK_6 + 1 ) ,
        REGISTER_AMD64_RIP	= 0,
        REGISTER_AMD64_RSP	= ( REGISTER_AMD64_RIP + 1 ) ,
        REGISTER_AMD64_RBP	= ( REGISTER_AMD64_RSP + 1 ) ,
        REGISTER_AMD64_RAX	= ( REGISTER_AMD64_RBP + 1 ) ,
        REGISTER_AMD64_RCX	= ( REGISTER_AMD64_RAX + 1 ) ,
        REGISTER_AMD64_RDX	= ( REGISTER_AMD64_RCX + 1 ) ,
        REGISTER_AMD64_RBX	= ( REGISTER_AMD64_RDX + 1 ) ,
        REGISTER_AMD64_RSI	= ( REGISTER_AMD64_RBX + 1 ) ,
        REGISTER_AMD64_RDI	= ( REGISTER_AMD64_RSI + 1 ) ,
        REGISTER_AMD64_R8	= ( REGISTER_AMD64_RDI + 1 ) ,
        REGISTER_AMD64_R9	= ( REGISTER_AMD64_R8 + 1 ) ,
        REGISTER_AMD64_R10	= ( REGISTER_AMD64_R9 + 1 ) ,
        REGISTER_AMD64_R11	= ( REGISTER_AMD64_R10 + 1 ) ,
        REGISTER_AMD64_R12	= ( REGISTER_AMD64_R11 + 1 ) ,
        REGISTER_AMD64_R13	= ( REGISTER_AMD64_R12 + 1 ) ,
        REGISTER_AMD64_R14	= ( REGISTER_AMD64_R13 + 1 ) ,
        REGISTER_AMD64_R15	= ( REGISTER_AMD64_R14 + 1 ) ,
        REGISTER_AMD64_XMM0	= ( REGISTER_AMD64_R15 + 1 ) ,
        REGISTER_AMD64_XMM1	= ( REGISTER_AMD64_XMM0 + 1 ) ,
        REGISTER_AMD64_XMM2	= ( REGISTER_AMD64_XMM1 + 1 ) ,
        REGISTER_AMD64_XMM3	= ( REGISTER_AMD64_XMM2 + 1 ) ,
        REGISTER_AMD64_XMM4	= ( REGISTER_AMD64_XMM3 + 1 ) ,
        REGISTER_AMD64_XMM5	= ( REGISTER_AMD64_XMM4 + 1 ) ,
        REGISTER_AMD64_XMM6	= ( REGISTER_AMD64_XMM5 + 1 ) ,
        REGISTER_AMD64_XMM7	= ( REGISTER_AMD64_XMM6 + 1 ) ,
        REGISTER_AMD64_XMM8	= ( REGISTER_AMD64_XMM7 + 1 ) ,
        REGISTER_AMD64_XMM9	= ( REGISTER_AMD64_XMM8 + 1 ) ,
        REGISTER_AMD64_XMM10	= ( REGISTER_AMD64_XMM9 + 1 ) ,
        REGISTER_AMD64_XMM11	= ( REGISTER_AMD64_XMM10 + 1 ) ,
        REGISTER_AMD64_XMM12	= ( REGISTER_AMD64_XMM11 + 1 ) ,
        REGISTER_AMD64_XMM13	= ( REGISTER_AMD64_XMM12 + 1 ) ,
        REGISTER_AMD64_XMM14	= ( REGISTER_AMD64_XMM13 + 1 ) ,
        REGISTER_AMD64_XMM15	= ( REGISTER_AMD64_XMM14 + 1 ) ,
        REGISTER_IA64_BSP	= REGISTER_FRAME_POINTER,
        REGISTER_IA64_R0	= ( REGISTER_IA64_BSP + 1 ) ,
        REGISTER_IA64_F0	= ( REGISTER_IA64_R0 + 128 ) ,
        REGISTER_ARM_PC	= 0,
        REGISTER_ARM_SP	= ( REGISTER_ARM_PC + 1 ) ,
        REGISTER_ARM_R0	= ( REGISTER_ARM_SP + 1 ) ,
        REGISTER_ARM_R1	= ( REGISTER_ARM_R0 + 1 ) ,
        REGISTER_ARM_R2	= ( REGISTER_ARM_R1 + 1 ) ,
        REGISTER_ARM_R3	= ( REGISTER_ARM_R2 + 1 ) ,
        REGISTER_ARM_R4	= ( REGISTER_ARM_R3 + 1 ) ,
        REGISTER_ARM_R5	= ( REGISTER_ARM_R4 + 1 ) ,
        REGISTER_ARM_R6	= ( REGISTER_ARM_R5 + 1 ) ,
        REGISTER_ARM_R7	= ( REGISTER_ARM_R6 + 1 ) ,
        REGISTER_ARM_R8	= ( REGISTER_ARM_R7 + 1 ) ,
        REGISTER_ARM_R9	= ( REGISTER_ARM_R8 + 1 ) ,
        REGISTER_ARM_R10	= ( REGISTER_ARM_R9 + 1 ) ,
        REGISTER_ARM_R11	= ( REGISTER_ARM_R10 + 1 ) ,
        REGISTER_ARM_R12	= ( REGISTER_ARM_R11 + 1 ) ,
        REGISTER_ARM_LR	= ( REGISTER_ARM_R12 + 1 ) ,
        REGISTER_ARM_D0	= ( REGISTER_ARM_LR + 1 ) ,
        REGISTER_ARM_D1	= ( REGISTER_ARM_D0 + 1 ) ,
        REGISTER_ARM_D2	= ( REGISTER_ARM_D1 + 1 ) ,
        REGISTER_ARM_D3	= ( REGISTER_ARM_D2 + 1 ) ,
        REGISTER_ARM_D4	= ( REGISTER_ARM_D3 + 1 ) ,
        REGISTER_ARM_D5	= ( REGISTER_ARM_D4 + 1 ) ,
        REGISTER_ARM_D6	= ( REGISTER_ARM_D5 + 1 ) ,
        REGISTER_ARM_D7	= ( REGISTER_ARM_D6 + 1 ) ,
        REGISTER_ARM_D8	= ( REGISTER_ARM_D7 + 1 ) ,
        REGISTER_ARM_D9	= ( REGISTER_ARM_D8 + 1 ) ,
        REGISTER_ARM_D10	= ( REGISTER_ARM_D9 + 1 ) ,
        REGISTER_ARM_D11	= ( REGISTER_ARM_D10 + 1 ) ,
        REGISTER_ARM_D12	= ( REGISTER_ARM_D11 + 1 ) ,
        REGISTER_ARM_D13	= ( REGISTER_ARM_D12 + 1 ) ,
        REGISTER_ARM_D14	= ( REGISTER_ARM_D13 + 1 ) ,
        REGISTER_ARM_D15	= ( REGISTER_ARM_D14 + 1 ) ,
        REGISTER_ARM_D16	= ( REGISTER_ARM_D15 + 1 ) ,
        REGISTER_ARM_D17	= ( REGISTER_ARM_D16 + 1 ) ,
        REGISTER_ARM_D18	= ( REGISTER_ARM_D17 + 1 ) ,
        REGISTER_ARM_D19	= ( REGISTER_ARM_D18 + 1 ) ,
        REGISTER_ARM_D20	= ( REGISTER_ARM_D19 + 1 ) ,
        REGISTER_ARM_D21	= ( REGISTER_ARM_D20 + 1 ) ,
        REGISTER_ARM_D22	= ( REGISTER_ARM_D21 + 1 ) ,
        REGISTER_ARM_D23	= ( REGISTER_ARM_D22 + 1 ) ,
        REGISTER_ARM_D24	= ( REGISTER_ARM_D23 + 1 ) ,
        REGISTER_ARM_D25	= ( REGISTER_ARM_D24 + 1 ) ,
        REGISTER_ARM_D26	= ( REGISTER_ARM_D25 + 1 ) ,
        REGISTER_ARM_D27	= ( REGISTER_ARM_D26 + 1 ) ,
        REGISTER_ARM_D28	= ( REGISTER_ARM_D27 + 1 ) ,
        REGISTER_ARM_D29	= ( REGISTER_ARM_D28 + 1 ) ,
        REGISTER_ARM_D30	= ( REGISTER_ARM_D29 + 1 ) ,
        REGISTER_ARM_D31	= ( REGISTER_ARM_D30 + 1 ) ,
        REGISTER_ARM64_PC	= 0,
        REGISTER_ARM64_SP	= ( REGISTER_ARM64_PC + 1 ) ,
        REGISTER_ARM64_FP	= ( REGISTER_ARM64_SP + 1 ) ,
        REGISTER_ARM64_X0	= ( REGISTER_ARM64_FP + 1 ) ,
        REGISTER_ARM64_X1	= ( REGISTER_ARM64_X0 + 1 ) ,
        REGISTER_ARM64_X2	= ( REGISTER_ARM64_X1 + 1 ) ,
        REGISTER_ARM64_X3	= ( REGISTER_ARM64_X2 + 1 ) ,
        REGISTER_ARM64_X4	= ( REGISTER_ARM64_X3 + 1 ) ,
        REGISTER_ARM64_X5	= ( REGISTER_ARM64_X4 + 1 ) ,
        REGISTER_ARM64_X6	= ( REGISTER_ARM64_X5 + 1 ) ,
        REGISTER_ARM64_X7	= ( REGISTER_ARM64_X6 + 1 ) ,
        REGISTER_ARM64_X8	= ( REGISTER_ARM64_X7 + 1 ) ,
        REGISTER_ARM64_X9	= ( REGISTER_ARM64_X8 + 1 ) ,
        REGISTER_ARM64_X10	= ( REGISTER_ARM64_X9 + 1 ) ,
        REGISTER_ARM64_X11	= ( REGISTER_ARM64_X10 + 1 ) ,
        REGISTER_ARM64_X12	= ( REGISTER_ARM64_X11 + 1 ) ,
        REGISTER_ARM64_X13	= ( REGISTER_ARM64_X12 + 1 ) ,
        REGISTER_ARM64_X14	= ( REGISTER_ARM64_X13 + 1 ) ,
        REGISTER_ARM64_X15	= ( REGISTER_ARM64_X14 + 1 ) ,
        REGISTER_ARM64_X16	= ( REGISTER_ARM64_X15 + 1 ) ,
        REGISTER_ARM64_X17	= ( REGISTER_ARM64_X16 + 1 ) ,
        REGISTER_ARM64_X18	= ( REGISTER_ARM64_X17 + 1 ) ,
        REGISTER_ARM64_X19	= ( REGISTER_ARM64_X18 + 1 ) ,
        REGISTER_ARM64_X20	= ( REGISTER_ARM64_X19 + 1 ) ,
        REGISTER_ARM64_X21	= ( REGISTER_ARM64_X20 + 1 ) ,
        REGISTER_ARM64_X22	= ( REGISTER_ARM64_X21 + 1 ) ,
        REGISTER_ARM64_X23	= ( REGISTER_ARM64_X22 + 1 ) ,
        REGISTER_ARM64_X24	= ( REGISTER_ARM64_X23 + 1 ) ,
        REGISTER_ARM64_X25	= ( REGISTER_ARM64_X24 + 1 ) ,
        REGISTER_ARM64_X26	= ( REGISTER_ARM64_X25 + 1 ) ,
        REGISTER_ARM64_X27	= ( REGISTER_ARM64_X26 + 1 ) ,
        REGISTER_ARM64_X28	= ( REGISTER_ARM64_X27 + 1 ) ,
        REGISTER_ARM64_LR	= ( REGISTER_ARM64_X28 + 1 ) ,
        REGISTER_ARM64_V0	= ( REGISTER_ARM64_LR + 1 ) ,
        REGISTER_ARM64_V1	= ( REGISTER_ARM64_V0 + 1 ) ,
        REGISTER_ARM64_V2	= ( REGISTER_ARM64_V1 + 1 ) ,
        REGISTER_ARM64_V3	= ( REGISTER_ARM64_V2 + 1 ) ,
        REGISTER_ARM64_V4	= ( REGISTER_ARM64_V3 + 1 ) ,
        REGISTER_ARM64_V5	= ( REGISTER_ARM64_V4 + 1 ) ,
        REGISTER_ARM64_V6	= ( REGISTER_ARM64_V5 + 1 ) ,
        REGISTER_ARM64_V7	= ( REGISTER_ARM64_V6 + 1 ) ,
        REGISTER_ARM64_V8	= ( REGISTER_ARM64_V7 + 1 ) ,
        REGISTER_ARM64_V9	= ( REGISTER_ARM64_V8 + 1 ) ,
        REGISTER_ARM64_V10	= ( REGISTER_ARM64_V9 + 1 ) ,
        REGISTER_ARM64_V11	= ( REGISTER_ARM64_V10 + 1 ) ,
        REGISTER_ARM64_V12	= ( REGISTER_ARM64_V11 + 1 ) ,
        REGISTER_ARM64_V13	= ( REGISTER_ARM64_V12 + 1 ) ,
        REGISTER_ARM64_V14	= ( REGISTER_ARM64_V13 + 1 ) ,
        REGISTER_ARM64_V15	= ( REGISTER_ARM64_V14 + 1 ) ,
        REGISTER_ARM64_V16	= ( REGISTER_ARM64_V15 + 1 ) ,
        REGISTER_ARM64_V17	= ( REGISTER_ARM64_V16 + 1 ) ,
        REGISTER_ARM64_V18	= ( REGISTER_ARM64_V17 + 1 ) ,
        REGISTER_ARM64_V19	= ( REGISTER_ARM64_V18 + 1 ) ,
        REGISTER_ARM64_V20	= ( REGISTER_ARM64_V19 + 1 ) ,
        REGISTER_ARM64_V21	= ( REGISTER_ARM64_V20 + 1 ) ,
        REGISTER_ARM64_V22	= ( REGISTER_ARM64_V21 + 1 ) ,
        REGISTER_ARM64_V23	= ( REGISTER_ARM64_V22 + 1 ) ,
        REGISTER_ARM64_V24	= ( REGISTER_ARM64_V23 + 1 ) ,
        REGISTER_ARM64_V25	= ( REGISTER_ARM64_V24 + 1 ) ,
        REGISTER_ARM64_V26	= ( REGISTER_ARM64_V25 + 1 ) ,
        REGISTER_ARM64_V27	= ( REGISTER_ARM64_V26 + 1 ) ,
        REGISTER_ARM64_V28	= ( REGISTER_ARM64_V27 + 1 ) ,
        REGISTER_ARM64_V29	= ( REGISTER_ARM64_V28 + 1 ) ,
        REGISTER_ARM64_V30	= ( REGISTER_ARM64_V29 + 1 ) ,
        REGISTER_ARM64_V31	= ( REGISTER_ARM64_V30 + 1 )
    } 	CorDebugRegister;


EXTERN_C const IID IID_ICorDebugRegisterSet;

#if defined(__cplusplus) && !defined(CINTERFACE)

    MIDL_INTERFACE("CC7BCB0B-8A68-11d2-983C-0000F808342D")
    ICorDebugRegisterSet : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE GetRegistersAvailable(
            /* [out] */ ULONG64 *pAvailable) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetRegisters(
            /* [in] */ ULONG64 mask,
            /* [in] */ ULONG32 regCount,
            /* [length_is][size_is][out] */ CORDB_REGISTER regBuffer[  ]) = 0;

        virtual HRESULT STDMETHODCALLTYPE SetRegisters(
            /* [in] */ ULONG64 mask,
            /* [in] */ ULONG32 regCount,
            /* [size_is][in] */ CORDB_REGISTER regBuffer[  ]) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetThreadContext(
            /* [in] */ ULONG32 contextSize,
            /* [size_is][length_is][out][in] */ BYTE context[  ]) = 0;

        virtual HRESULT STDMETHODCALLTYPE SetThreadContext(
            /* [in] */ ULONG32 contextSize,
            /* [size_is][length_is][in] */ BYTE context[  ]) = 0;

    };


#else 	/* C style interface */

    typedef struct ICorDebugRegisterSetVtbl
    {
        BEGIN_INTERFACE

        HRESULT ( STDMETHODCALLTYPE *QueryInterface )(
            ICorDebugRegisterSet * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */
            _COM_Outptr_  void **ppvObject);

        ULONG ( STDMETHODCALLTYPE *AddRef )(
            ICorDebugRegisterSet * This);

        ULONG ( STDMETHODCALLTYPE *Release )(
            ICorDebugRegisterSet * This);

        HRESULT ( STDMETHODCALLTYPE *GetRegistersAvailable )(
            ICorDebugRegisterSet * This,
            /* [out] */ ULONG64 *pAvailable);

        HRESULT ( STDMETHODCALLTYPE *GetRegisters )(
            ICorDebugRegisterSet * This,
            /* [in] */ ULONG64 mask,
            /* [in] */ ULONG32 regCount,
            /* [length_is][size_is][out] */ CORDB_REGISTER regBuffer[  ]);

        HRESULT ( STDMETHODCALLTYPE *SetRegisters )(
            ICorDebugRegisterSet * This,
            /* [in] */ ULONG64 mask,
            /* [in] */ ULONG32 regCount,
            /* [size_is][in] */ CORDB_REGISTER regBuffer[  ]);

        HRESULT ( STDMETHODCALLTYPE *GetThreadContext )(
            ICorDebugRegisterSet * This,
            /* [in] */ ULONG32 contextSize,
            /* [size_is][length_is][out][in] */ BYTE context[  ]);

        HRESULT ( STDMETHODCALLTYPE *SetThreadContext )(
            ICorDebugRegisterSet * This,
            /* [in] */ ULONG32 contextSize,
            /* [size_is][length_is][in] */ BYTE context[  ]);

        END_INTERFACE
    } ICorDebugRegisterSetVtbl;

    interface ICorDebugRegisterSet
    {
        CONST_VTBL struct ICorDebugRegisterSetVtbl *lpVtbl;
    };



#ifdef COBJMACROS


#define ICorDebugRegisterSet_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) )

#define ICorDebugRegisterSet_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) )

#define ICorDebugRegisterSet_Release(This)	\
    ( (This)->lpVtbl -> Release(This) )


#define ICorDebugRegisterSet_GetRegistersAvailable(This,pAvailable)	\
    ( (This)->lpVtbl -> GetRegistersAvailable(This,pAvailable) )

#define ICorDebugRegisterSet_GetRegisters(This,mask,regCount,regBuffer)	\
    ( (This)->lpVtbl -> GetRegisters(This,mask,regCount,regBuffer) )

#define ICorDebugRegisterSet_SetRegisters(This,mask,regCount,regBuffer)	\
    ( (This)->lpVtbl -> SetRegisters(This,mask,regCount,regBuffer) )

#define ICorDebugRegisterSet_GetThreadContext(This,contextSize,context)	\
    ( (This)->lpVtbl -> GetThreadContext(This,contextSize,context) )

#define ICorDebugRegisterSet_SetThreadContext(This,contextSize,context)	\
    ( (This)->lpVtbl -> SetThreadContext(This,contextSize,context) )

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ICorDebugRegisterSet_INTERFACE_DEFINED__ */


#ifndef __ICorDebugRegisterSet2_INTERFACE_DEFINED__
#define __ICorDebugRegisterSet2_INTERFACE_DEFINED__

/* interface ICorDebugRegisterSet2 */
/* [unique][uuid][local][object] */


EXTERN_C const IID IID_ICorDebugRegisterSet2;

#if defined(__cplusplus) && !defined(CINTERFACE)

    MIDL_INTERFACE("6DC7BA3F-89BA-4459-9EC1-9D60937B468D")
    ICorDebugRegisterSet2 : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE GetRegistersAvailable(
            /* [in] */ ULONG32 numChunks,
            /* [size_is][out] */ BYTE availableRegChunks[  ]) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetRegisters(
            /* [in] */ ULONG32 maskCount,
            /* [size_is][in] */ BYTE mask[  ],
            /* [in] */ ULONG32 regCount,
            /* [size_is][out] */ CORDB_REGISTER regBuffer[  ]) = 0;

        virtual HRESULT STDMETHODCALLTYPE SetRegisters(
            /* [in] */ ULONG32 maskCount,
            /* [size_is][in] */ BYTE mask[  ],
            /* [in] */ ULONG32 regCount,
            /* [size_is][in] */ CORDB_REGISTER regBuffer[  ]) = 0;

    };


#else 	/* C style interface */

    typedef struct ICorDebugRegisterSet2Vtbl
    {
        BEGIN_INTERFACE

        HRESULT ( STDMETHODCALLTYPE *QueryInterface )(
            ICorDebugRegisterSet2 * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */
            _COM_Outptr_  void **ppvObject);

        ULONG ( STDMETHODCALLTYPE *AddRef )(
            ICorDebugRegisterSet2 * This);

        ULONG ( STDMETHODCALLTYPE *Release )(
            ICorDebugRegisterSet2 * This);

        HRESULT ( STDMETHODCALLTYPE *GetRegistersAvailable )(
            ICorDebugRegisterSet2 * This,
            /* [in] */ ULONG32 numChunks,
            /* [size_is][out] */ BYTE availableRegChunks[  ]);

        HRESULT ( STDMETHODCALLTYPE *GetRegisters )(
            ICorDebugRegisterSet2 * This,
            /* [in] */ ULONG32 maskCount,
            /* [size_is][in] */ BYTE mask[  ],
            /* [in] */ ULONG32 regCount,
            /* [size_is][out] */ CORDB_REGISTER regBuffer[  ]);

        HRESULT ( STDMETHODCALLTYPE *SetRegisters )(
            ICorDebugRegisterSet2 * This,
            /* [in] */ ULONG32 maskCount,
            /* [size_is][in] */ BYTE mask[  ],
            /* [in] */ ULONG32 regCount,
            /* [size_is][in] */ CORDB_REGISTER regBuffer[  ]);

        END_INTERFACE
    } ICorDebugRegisterSet2Vtbl;

    interface ICorDebugRegisterSet2
    {
        CONST_VTBL struct ICorDebugRegisterSet2Vtbl *lpVtbl;
    };



#ifdef COBJMACROS


#define ICorDebugRegisterSet2_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) )

#define ICorDebugRegisterSet2_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) )

#define ICorDebugRegisterSet2_Release(This)	\
    ( (This)->lpVtbl -> Release(This) )


#define ICorDebugRegisterSet2_GetRegistersAvailable(This,numChunks,availableRegChunks)	\
    ( (This)->lpVtbl -> GetRegistersAvailable(This,numChunks,availableRegChunks) )

#define ICorDebugRegisterSet2_GetRegisters(This,maskCount,mask,regCount,regBuffer)	\
    ( (This)->lpVtbl -> GetRegisters(This,maskCount,mask,regCount,regBuffer) )

#define ICorDebugRegisterSet2_SetRegisters(This,maskCount,mask,regCount,regBuffer)	\
    ( (This)->lpVtbl -> SetRegisters(This,maskCount,mask,regCount,regBuffer) )

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ICorDebugRegisterSet2_INTERFACE_DEFINED__ */


#ifndef __ICorDebugThread_INTERFACE_DEFINED__
#define __ICorDebugThread_INTERFACE_DEFINED__

/* interface ICorDebugThread */
/* [unique][uuid][local][object] */

typedef
enum CorDebugUserState
    {
        USER_STOP_REQUESTED	= 0x1,
        USER_SUSPEND_REQUESTED	= 0x2,
        USER_BACKGROUND	= 0x4,
        USER_UNSTARTED	= 0x8,
        USER_STOPPED	= 0x10,
        USER_WAIT_SLEEP_JOIN	= 0x20,
        USER_SUSPENDED	= 0x40,
        USER_UNSAFE_POINT	= 0x80,
        USER_THREADPOOL	= 0x100
    } 	CorDebugUserState;


EXTERN_C const IID IID_ICorDebugThread;

#if defined(__cplusplus) && !defined(CINTERFACE)

    MIDL_INTERFACE("938c6d66-7fb6-4f69-b389-425b8987329b")
    ICorDebugThread : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE GetProcess(
            /* [out] */ ICorDebugProcess **ppProcess) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetID(
            /* [out] */ DWORD *pdwThreadId) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetHandle(
            /* [out] */ HTHREAD *phThreadHandle) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetAppDomain(
            /* [out] */ ICorDebugAppDomain **ppAppDomain) = 0;

        virtual HRESULT STDMETHODCALLTYPE SetDebugState(
            /* [in] */ CorDebugThreadState state) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetDebugState(
            /* [out] */ CorDebugThreadState *pState) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetUserState(
            /* [out] */ CorDebugUserState *pState) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetCurrentException(
            /* [out] */ ICorDebugValue **ppExceptionObject) = 0;

        virtual HRESULT STDMETHODCALLTYPE ClearCurrentException( void) = 0;

        virtual HRESULT STDMETHODCALLTYPE CreateStepper(
            /* [out] */ ICorDebugStepper **ppStepper) = 0;

        virtual HRESULT STDMETHODCALLTYPE EnumerateChains(
            /* [out] */ ICorDebugChainEnum **ppChains) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetActiveChain(
            /* [out] */ ICorDebugChain **ppChain) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetActiveFrame(
            /* [out] */ ICorDebugFrame **ppFrame) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetRegisterSet(
            /* [out] */ ICorDebugRegisterSet **ppRegisters) = 0;

        virtual HRESULT STDMETHODCALLTYPE CreateEval(
            /* [out] */ ICorDebugEval **ppEval) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetObject(
            /* [out] */ ICorDebugValue **ppObject) = 0;

    };


#else 	/* C style interface */

    typedef struct ICorDebugThreadVtbl
    {
        BEGIN_INTERFACE

        HRESULT ( STDMETHODCALLTYPE *QueryInterface )(
            ICorDebugThread * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */
            _COM_Outptr_  void **ppvObject);

        ULONG ( STDMETHODCALLTYPE *AddRef )(
            ICorDebugThread * This);

        ULONG ( STDMETHODCALLTYPE *Release )(
            ICorDebugThread * This);

        HRESULT ( STDMETHODCALLTYPE *GetProcess )(
            ICorDebugThread * This,
            /* [out] */ ICorDebugProcess **ppProcess);

        HRESULT ( STDMETHODCALLTYPE *GetID )(
            ICorDebugThread * This,
            /* [out] */ DWORD *pdwThreadId);

        HRESULT ( STDMETHODCALLTYPE *GetHandle )(
            ICorDebugThread * This,
            /* [out] */ HTHREAD *phThreadHandle);

        HRESULT ( STDMETHODCALLTYPE *GetAppDomain )(
            ICorDebugThread * This,
            /* [out] */ ICorDebugAppDomain **ppAppDomain);

        HRESULT ( STDMETHODCALLTYPE *SetDebugState )(
            ICorDebugThread * This,
            /* [in] */ CorDebugThreadState state);

        HRESULT ( STDMETHODCALLTYPE *GetDebugState )(
            ICorDebugThread * This,
            /* [out] */ CorDebugThreadState *pState);

        HRESULT ( STDMETHODCALLTYPE *GetUserState )(
            ICorDebugThread * This,
            /* [out] */ CorDebugUserState *pState);

        HRESULT ( STDMETHODCALLTYPE *GetCurrentException )(
            ICorDebugThread * This,
            /* [out] */ ICorDebugValue **ppExceptionObject);

        HRESULT ( STDMETHODCALLTYPE *ClearCurrentException )(
            ICorDebugThread * This);

        HRESULT ( STDMETHODCALLTYPE *CreateStepper )(
            ICorDebugThread * This,
            /* [out] */ ICorDebugStepper **ppStepper);

        HRESULT ( STDMETHODCALLTYPE *EnumerateChains )(
            ICorDebugThread * This,
            /* [out] */ ICorDebugChainEnum **ppChains);

        HRESULT ( STDMETHODCALLTYPE *GetActiveChain )(
            ICorDebugThread * This,
            /* [out] */ ICorDebugChain **ppChain);

        HRESULT ( STDMETHODCALLTYPE *GetActiveFrame )(
            ICorDebugThread * This,
            /* [out] */ ICorDebugFrame **ppFrame);

        HRESULT ( STDMETHODCALLTYPE *GetRegisterSet )(
            ICorDebugThread * This,
            /* [out] */ ICorDebugRegisterSet **ppRegisters);

        HRESULT ( STDMETHODCALLTYPE *CreateEval )(
            ICorDebugThread * This,
            /* [out] */ ICorDebugEval **ppEval);

        HRESULT ( STDMETHODCALLTYPE *GetObject )(
            ICorDebugThread * This,
            /* [out] */ ICorDebugValue **ppObject);

        END_INTERFACE
    } ICorDebugThreadVtbl;

    interface ICorDebugThread
    {
        CONST_VTBL struct ICorDebugThreadVtbl *lpVtbl;
    };



#ifdef COBJMACROS


#define ICorDebugThread_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) )

#define ICorDebugThread_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) )

#define ICorDebugThread_Release(This)	\
    ( (This)->lpVtbl -> Release(This) )


#define ICorDebugThread_GetProcess(This,ppProcess)	\
    ( (This)->lpVtbl -> GetProcess(This,ppProcess) )

#define ICorDebugThread_GetID(This,pdwThreadId)	\
    ( (This)->lpVtbl -> GetID(This,pdwThreadId) )

#define ICorDebugThread_GetHandle(This,phThreadHandle)	\
    ( (This)->lpVtbl -> GetHandle(This,phThreadHandle) )

#define ICorDebugThread_GetAppDomain(This,ppAppDomain)	\
    ( (This)->lpVtbl -> GetAppDomain(This,ppAppDomain) )

#define ICorDebugThread_SetDebugState(This,state)	\
    ( (This)->lpVtbl -> SetDebugState(This,state) )

#define ICorDebugThread_GetDebugState(This,pState)	\
    ( (This)->lpVtbl -> GetDebugState(This,pState) )

#define ICorDebugThread_GetUserState(This,pState)	\
    ( (This)->lpVtbl -> GetUserState(This,pState) )

#define ICorDebugThread_GetCurrentException(This,ppExceptionObject)	\
    ( (This)->lpVtbl -> GetCurrentException(This,ppExceptionObject) )

#define ICorDebugThread_ClearCurrentException(This)	\
    ( (This)->lpVtbl -> ClearCurrentException(This) )

#define ICorDebugThread_CreateStepper(This,ppStepper)	\
    ( (This)->lpVtbl -> CreateStepper(This,ppStepper) )

#define ICorDebugThread_EnumerateChains(This,ppChains)	\
    ( (This)->lpVtbl -> EnumerateChains(This,ppChains) )

#define ICorDebugThread_GetActiveChain(This,ppChain)	\
    ( (This)->lpVtbl -> GetActiveChain(This,ppChain) )

#define ICorDebugThread_GetActiveFrame(This,ppFrame)	\
    ( (This)->lpVtbl -> GetActiveFrame(This,ppFrame) )

#define ICorDebugThread_GetRegisterSet(This,ppRegisters)	\
    ( (This)->lpVtbl -> GetRegisterSet(This,ppRegisters) )

#define ICorDebugThread_CreateEval(This,ppEval)	\
    ( (This)->lpVtbl -> CreateEval(This,ppEval) )

#define ICorDebugThread_GetObject(This,ppObject)	\
    ( (This)->lpVtbl -> GetObject(This,ppObject) )

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ICorDebugThread_INTERFACE_DEFINED__ */


#ifndef __ICorDebugThread2_INTERFACE_DEFINED__
#define __ICorDebugThread2_INTERFACE_DEFINED__

/* interface ICorDebugThread2 */
/* [unique][uuid][local][object] */

typedef struct _COR_ACTIVE_FUNCTION
    {
    ICorDebugAppDomain *pAppDomain;
    ICorDebugModule *pModule;
    ICorDebugFunction2 *pFunction;
    ULONG32 ilOffset;
    ULONG32 flags;
    } 	COR_ACTIVE_FUNCTION;


EXTERN_C const IID IID_ICorDebugThread2;

#if defined(__cplusplus) && !defined(CINTERFACE)

    MIDL_INTERFACE("2BD956D9-7B07-4bef-8A98-12AA862417C5")
    ICorDebugThread2 : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE GetActiveFunctions(
            /* [in] */ ULONG32 cFunctions,
            /* [out] */ ULONG32 *pcFunctions,
            /* [length_is][size_is][out][in] */ COR_ACTIVE_FUNCTION pFunctions[  ]) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetConnectionID(
            /* [out] */ CONNID *pdwConnectionId) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetTaskID(
            /* [out] */ TASKID *pTaskId) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetVolatileOSThreadID(
            /* [out] */ DWORD *pdwTid) = 0;

        virtual HRESULT STDMETHODCALLTYPE InterceptCurrentException(
            /* [in] */ ICorDebugFrame *pFrame) = 0;

    };


#else 	/* C style interface */

    typedef struct ICorDebugThread2Vtbl
    {
        BEGIN_INTERFACE

        HRESULT ( STDMETHODCALLTYPE *QueryInterface )(
            ICorDebugThread2 * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */
            _COM_Outptr_  void **ppvObject);

        ULONG ( STDMETHODCALLTYPE *AddRef )(
            ICorDebugThread2 * This);

        ULONG ( STDMETHODCALLTYPE *Release )(
            ICorDebugThread2 * This);

        HRESULT ( STDMETHODCALLTYPE *GetActiveFunctions )(
            ICorDebugThread2 * This,
            /* [in] */ ULONG32 cFunctions,
            /* [out] */ ULONG32 *pcFunctions,
            /* [length_is][size_is][out][in] */ COR_ACTIVE_FUNCTION pFunctions[  ]);

        HRESULT ( STDMETHODCALLTYPE *GetConnectionID )(
            ICorDebugThread2 * This,
            /* [out] */ CONNID *pdwConnectionId);

        HRESULT ( STDMETHODCALLTYPE *GetTaskID )(
            ICorDebugThread2 * This,
            /* [out] */ TASKID *pTaskId);

        HRESULT ( STDMETHODCALLTYPE *GetVolatileOSThreadID )(
            ICorDebugThread2 * This,
            /* [out] */ DWORD *pdwTid);

        HRESULT ( STDMETHODCALLTYPE *InterceptCurrentException )(
            ICorDebugThread2 * This,
            /* [in] */ ICorDebugFrame *pFrame);

        END_INTERFACE
    } ICorDebugThread2Vtbl;

    interface ICorDebugThread2
    {
        CONST_VTBL struct ICorDebugThread2Vtbl *lpVtbl;
    };



#ifdef COBJMACROS


#define ICorDebugThread2_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) )

#define ICorDebugThread2_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) )

#define ICorDebugThread2_Release(This)	\
    ( (This)->lpVtbl -> Release(This) )


#define ICorDebugThread2_GetActiveFunctions(This,cFunctions,pcFunctions,pFunctions)	\
    ( (This)->lpVtbl -> GetActiveFunctions(This,cFunctions,pcFunctions,pFunctions) )

#define ICorDebugThread2_GetConnectionID(This,pdwConnectionId)	\
    ( (This)->lpVtbl -> GetConnectionID(This,pdwConnectionId) )

#define ICorDebugThread2_GetTaskID(This,pTaskId)	\
    ( (This)->lpVtbl -> GetTaskID(This,pTaskId) )

#define ICorDebugThread2_GetVolatileOSThreadID(This,pdwTid)	\
    ( (This)->lpVtbl -> GetVolatileOSThreadID(This,pdwTid) )

#define ICorDebugThread2_InterceptCurrentException(This,pFrame)	\
    ( (This)->lpVtbl -> InterceptCurrentException(This,pFrame) )

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ICorDebugThread2_INTERFACE_DEFINED__ */


#ifndef __ICorDebugThread3_INTERFACE_DEFINED__
#define __ICorDebugThread3_INTERFACE_DEFINED__

/* interface ICorDebugThread3 */
/* [unique][uuid][local][object] */


EXTERN_C const IID IID_ICorDebugThread3;

#if defined(__cplusplus) && !defined(CINTERFACE)

    MIDL_INTERFACE("F8544EC3-5E4E-46c7-8D3E-A52B8405B1F5")
    ICorDebugThread3 : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE CreateStackWalk(
            /* [out] */ ICorDebugStackWalk **ppStackWalk) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetActiveInternalFrames(
            /* [in] */ ULONG32 cInternalFrames,
            /* [out] */ ULONG32 *pcInternalFrames,
            /* [length_is][size_is][out][in] */ ICorDebugInternalFrame2 *ppInternalFrames[  ]) = 0;

    };


#else 	/* C style interface */

    typedef struct ICorDebugThread3Vtbl
    {
        BEGIN_INTERFACE

        HRESULT ( STDMETHODCALLTYPE *QueryInterface )(
            ICorDebugThread3 * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */
            _COM_Outptr_  void **ppvObject);

        ULONG ( STDMETHODCALLTYPE *AddRef )(
            ICorDebugThread3 * This);

        ULONG ( STDMETHODCALLTYPE *Release )(
            ICorDebugThread3 * This);

        HRESULT ( STDMETHODCALLTYPE *CreateStackWalk )(
            ICorDebugThread3 * This,
            /* [out] */ ICorDebugStackWalk **ppStackWalk);

        HRESULT ( STDMETHODCALLTYPE *GetActiveInternalFrames )(
            ICorDebugThread3 * This,
            /* [in] */ ULONG32 cInternalFrames,
            /* [out] */ ULONG32 *pcInternalFrames,
            /* [length_is][size_is][out][in] */ ICorDebugInternalFrame2 *ppInternalFrames[  ]);

        END_INTERFACE
    } ICorDebugThread3Vtbl;

    interface ICorDebugThread3
    {
        CONST_VTBL struct ICorDebugThread3Vtbl *lpVtbl;
    };



#ifdef COBJMACROS


#define ICorDebugThread3_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) )

#define ICorDebugThread3_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) )

#define ICorDebugThread3_Release(This)	\
    ( (This)->lpVtbl -> Release(This) )


#define ICorDebugThread3_CreateStackWalk(This,ppStackWalk)	\
    ( (This)->lpVtbl -> CreateStackWalk(This,ppStackWalk) )

#define ICorDebugThread3_GetActiveInternalFrames(This,cInternalFrames,pcInternalFrames,ppInternalFrames)	\
    ( (This)->lpVtbl -> GetActiveInternalFrames(This,cInternalFrames,pcInternalFrames,ppInternalFrames) )

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ICorDebugThread3_INTERFACE_DEFINED__ */


#ifndef __ICorDebugThread4_INTERFACE_DEFINED__
#define __ICorDebugThread4_INTERFACE_DEFINED__

/* interface ICorDebugThread4 */
/* [unique][uuid][local][object] */


EXTERN_C const IID IID_ICorDebugThread4;

#if defined(__cplusplus) && !defined(CINTERFACE)

    MIDL_INTERFACE("1A1F204B-1C66-4637-823F-3EE6C744A69C")
    ICorDebugThread4 : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE HasUnhandledException( void) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetBlockingObjects(
            /* [out] */ ICorDebugBlockingObjectEnum **ppBlockingObjectEnum) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetCurrentCustomDebuggerNotification(
            /* [out] */ ICorDebugValue **ppNotificationObject) = 0;

    };


#else 	/* C style interface */

    typedef struct ICorDebugThread4Vtbl
    {
        BEGIN_INTERFACE

        HRESULT ( STDMETHODCALLTYPE *QueryInterface )(
            ICorDebugThread4 * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */
            _COM_Outptr_  void **ppvObject);

        ULONG ( STDMETHODCALLTYPE *AddRef )(
            ICorDebugThread4 * This);

        ULONG ( STDMETHODCALLTYPE *Release )(
            ICorDebugThread4 * This);

        HRESULT ( STDMETHODCALLTYPE *HasUnhandledException )(
            ICorDebugThread4 * This);

        HRESULT ( STDMETHODCALLTYPE *GetBlockingObjects )(
            ICorDebugThread4 * This,
            /* [out] */ ICorDebugBlockingObjectEnum **ppBlockingObjectEnum);

        HRESULT ( STDMETHODCALLTYPE *GetCurrentCustomDebuggerNotification )(
            ICorDebugThread4 * This,
            /* [out] */ ICorDebugValue **ppNotificationObject);

        END_INTERFACE
    } ICorDebugThread4Vtbl;

    interface ICorDebugThread4
    {
        CONST_VTBL struct ICorDebugThread4Vtbl *lpVtbl;
    };



#ifdef COBJMACROS


#define ICorDebugThread4_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) )

#define ICorDebugThread4_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) )

#define ICorDebugThread4_Release(This)	\
    ( (This)->lpVtbl -> Release(This) )


#define ICorDebugThread4_HasUnhandledException(This)	\
    ( (This)->lpVtbl -> HasUnhandledException(This) )

#define ICorDebugThread4_GetBlockingObjects(This,ppBlockingObjectEnum)	\
    ( (This)->lpVtbl -> GetBlockingObjects(This,ppBlockingObjectEnum) )

#define ICorDebugThread4_GetCurrentCustomDebuggerNotification(This,ppNotificationObject)	\
    ( (This)->lpVtbl -> GetCurrentCustomDebuggerNotification(This,ppNotificationObject) )

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ICorDebugThread4_INTERFACE_DEFINED__ */


#ifndef __ICorDebugStackWalk_INTERFACE_DEFINED__
#define __ICorDebugStackWalk_INTERFACE_DEFINED__

/* interface ICorDebugStackWalk */
/* [unique][uuid][local][object] */

typedef
enum CorDebugSetContextFlag
    {
        SET_CONTEXT_FLAG_ACTIVE_FRAME	= 0x1,
        SET_CONTEXT_FLAG_UNWIND_FRAME	= 0x2
    } 	CorDebugSetContextFlag;


EXTERN_C const IID IID_ICorDebugStackWalk;

#if defined(__cplusplus) && !defined(CINTERFACE)

    MIDL_INTERFACE("A0647DE9-55DE-4816-929C-385271C64CF7")
    ICorDebugStackWalk : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE GetContext(
            /* [in] */ ULONG32 contextFlags,
            /* [in] */ ULONG32 contextBufSize,
            /* [out] */ ULONG32 *contextSize,
            /* [size_is][out] */ BYTE contextBuf[  ]) = 0;

        virtual HRESULT STDMETHODCALLTYPE SetContext(
            /* [in] */ CorDebugSetContextFlag flag,
            /* [in] */ ULONG32 contextSize,
            /* [size_is][in] */ BYTE context[  ]) = 0;

        virtual HRESULT STDMETHODCALLTYPE Next( void) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetFrame(
            /* [out] */ ICorDebugFrame **pFrame) = 0;

    };


#else 	/* C style interface */

    typedef struct ICorDebugStackWalkVtbl
    {
        BEGIN_INTERFACE

        HRESULT ( STDMETHODCALLTYPE *QueryInterface )(
            ICorDebugStackWalk * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */
            _COM_Outptr_  void **ppvObject);

        ULONG ( STDMETHODCALLTYPE *AddRef )(
            ICorDebugStackWalk * This);

        ULONG ( STDMETHODCALLTYPE *Release )(
            ICorDebugStackWalk * This);

        HRESULT ( STDMETHODCALLTYPE *GetContext )(
            ICorDebugStackWalk * This,
            /* [in] */ ULONG32 contextFlags,
            /* [in] */ ULONG32 contextBufSize,
            /* [out] */ ULONG32 *contextSize,
            /* [size_is][out] */ BYTE contextBuf[  ]);

        HRESULT ( STDMETHODCALLTYPE *SetContext )(
            ICorDebugStackWalk * This,
            /* [in] */ CorDebugSetContextFlag flag,
            /* [in] */ ULONG32 contextSize,
            /* [size_is][in] */ BYTE context[  ]);

        HRESULT ( STDMETHODCALLTYPE *Next )(
            ICorDebugStackWalk * This);

        HRESULT ( STDMETHODCALLTYPE *GetFrame )(
            ICorDebugStackWalk * This,
            /* [out] */ ICorDebugFrame **pFrame);

        END_INTERFACE
    } ICorDebugStackWalkVtbl;

    interface ICorDebugStackWalk
    {
        CONST_VTBL struct ICorDebugStackWalkVtbl *lpVtbl;
    };



#ifdef COBJMACROS


#define ICorDebugStackWalk_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) )

#define ICorDebugStackWalk_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) )

#define ICorDebugStackWalk_Release(This)	\
    ( (This)->lpVtbl -> Release(This) )


#define ICorDebugStackWalk_GetContext(This,contextFlags,contextBufSize,contextSize,contextBuf)	\
    ( (This)->lpVtbl -> GetContext(This,contextFlags,contextBufSize,contextSize,contextBuf) )

#define ICorDebugStackWalk_SetContext(This,flag,contextSize,context)	\
    ( (This)->lpVtbl -> SetContext(This,flag,contextSize,context) )

#define ICorDebugStackWalk_Next(This)	\
    ( (This)->lpVtbl -> Next(This) )

#define ICorDebugStackWalk_GetFrame(This,pFrame)	\
    ( (This)->lpVtbl -> GetFrame(This,pFrame) )

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ICorDebugStackWalk_INTERFACE_DEFINED__ */


#ifndef __ICorDebugChain_INTERFACE_DEFINED__
#define __ICorDebugChain_INTERFACE_DEFINED__

/* interface ICorDebugChain */
/* [unique][uuid][local][object] */

typedef
enum CorDebugChainReason
    {
        CHAIN_NONE	= 0,
        CHAIN_CLASS_INIT	= 0x1,
        CHAIN_EXCEPTION_FILTER	= 0x2,
        CHAIN_SECURITY	= 0x4,
        CHAIN_CONTEXT_POLICY	= 0x8,
        CHAIN_INTERCEPTION	= 0x10,
        CHAIN_PROCESS_START	= 0x20,
        CHAIN_THREAD_START	= 0x40,
        CHAIN_ENTER_MANAGED	= 0x80,
        CHAIN_ENTER_UNMANAGED	= 0x100,
        CHAIN_DEBUGGER_EVAL	= 0x200,
        CHAIN_CONTEXT_SWITCH	= 0x400,
        CHAIN_FUNC_EVAL	= 0x800
    } 	CorDebugChainReason;


EXTERN_C const IID IID_ICorDebugChain;

#if defined(__cplusplus) && !defined(CINTERFACE)

    MIDL_INTERFACE("CC7BCAEE-8A68-11d2-983C-0000F808342D")
    ICorDebugChain : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE GetThread(
            /* [out] */ ICorDebugThread **ppThread) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetStackRange(
            /* [out] */ CORDB_ADDRESS *pStart,
            /* [out] */ CORDB_ADDRESS *pEnd) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetContext(
            /* [out] */ ICorDebugContext **ppContext) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetCaller(
            /* [out] */ ICorDebugChain **ppChain) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetCallee(
            /* [out] */ ICorDebugChain **ppChain) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetPrevious(
            /* [out] */ ICorDebugChain **ppChain) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetNext(
            /* [out] */ ICorDebugChain **ppChain) = 0;

        virtual HRESULT STDMETHODCALLTYPE IsManaged(
            /* [out] */ BOOL *pManaged) = 0;

        virtual HRESULT STDMETHODCALLTYPE EnumerateFrames(
            /* [out] */ ICorDebugFrameEnum **ppFrames) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetActiveFrame(
            /* [out] */ ICorDebugFrame **ppFrame) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetRegisterSet(
            /* [out] */ ICorDebugRegisterSet **ppRegisters) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetReason(
            /* [out] */ CorDebugChainReason *pReason) = 0;

    };


#else 	/* C style interface */

    typedef struct ICorDebugChainVtbl
    {
        BEGIN_INTERFACE

        HRESULT ( STDMETHODCALLTYPE *QueryInterface )(
            ICorDebugChain * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */
            _COM_Outptr_  void **ppvObject);

        ULONG ( STDMETHODCALLTYPE *AddRef )(
            ICorDebugChain * This);

        ULONG ( STDMETHODCALLTYPE *Release )(
            ICorDebugChain * This);

        HRESULT ( STDMETHODCALLTYPE *GetThread )(
            ICorDebugChain * This,
            /* [out] */ ICorDebugThread **ppThread);

        HRESULT ( STDMETHODCALLTYPE *GetStackRange )(
            ICorDebugChain * This,
            /* [out] */ CORDB_ADDRESS *pStart,
            /* [out] */ CORDB_ADDRESS *pEnd);

        HRESULT ( STDMETHODCALLTYPE *GetContext )(
            ICorDebugChain * This,
            /* [out] */ ICorDebugContext **ppContext);

        HRESULT ( STDMETHODCALLTYPE *GetCaller )(
            ICorDebugChain * This,
            /* [out] */ ICorDebugChain **ppChain);

        HRESULT ( STDMETHODCALLTYPE *GetCallee )(
            ICorDebugChain * This,
            /* [out] */ ICorDebugChain **ppChain);

        HRESULT ( STDMETHODCALLTYPE *GetPrevious )(
            ICorDebugChain * This,
            /* [out] */ ICorDebugChain **ppChain);

        HRESULT ( STDMETHODCALLTYPE *GetNext )(
            ICorDebugChain * This,
            /* [out] */ ICorDebugChain **ppChain);

        HRESULT ( STDMETHODCALLTYPE *IsManaged )(
            ICorDebugChain * This,
            /* [out] */ BOOL *pManaged);

        HRESULT ( STDMETHODCALLTYPE *EnumerateFrames )(
            ICorDebugChain * This,
            /* [out] */ ICorDebugFrameEnum **ppFrames);

        HRESULT ( STDMETHODCALLTYPE *GetActiveFrame )(
            ICorDebugChain * This,
            /* [out] */ ICorDebugFrame **ppFrame);

        HRESULT ( STDMETHODCALLTYPE *GetRegisterSet )(
            ICorDebugChain * This,
            /* [out] */ ICorDebugRegisterSet **ppRegisters);

        HRESULT ( STDMETHODCALLTYPE *GetReason )(
            ICorDebugChain * This,
            /* [out] */ CorDebugChainReason *pReason);

        END_INTERFACE
    } ICorDebugChainVtbl;

    interface ICorDebugChain
    {
        CONST_VTBL struct ICorDebugChainVtbl *lpVtbl;
    };



#ifdef COBJMACROS


#define ICorDebugChain_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) )

#define ICorDebugChain_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) )

#define ICorDebugChain_Release(This)	\
    ( (This)->lpVtbl -> Release(This) )


#define ICorDebugChain_GetThread(This,ppThread)	\
    ( (This)->lpVtbl -> GetThread(This,ppThread) )

#define ICorDebugChain_GetStackRange(This,pStart,pEnd)	\
    ( (This)->lpVtbl -> GetStackRange(This,pStart,pEnd) )

#define ICorDebugChain_GetContext(This,ppContext)	\
    ( (This)->lpVtbl -> GetContext(This,ppContext) )

#define ICorDebugChain_GetCaller(This,ppChain)	\
    ( (This)->lpVtbl -> GetCaller(This,ppChain) )

#define ICorDebugChain_GetCallee(This,ppChain)	\
    ( (This)->lpVtbl -> GetCallee(This,ppChain) )

#define ICorDebugChain_GetPrevious(This,ppChain)	\
    ( (This)->lpVtbl -> GetPrevious(This,ppChain) )

#define ICorDebugChain_GetNext(This,ppChain)	\
    ( (This)->lpVtbl -> GetNext(This,ppChain) )

#define ICorDebugChain_IsManaged(This,pManaged)	\
    ( (This)->lpVtbl -> IsManaged(This,pManaged) )

#define ICorDebugChain_EnumerateFrames(This,ppFrames)	\
    ( (This)->lpVtbl -> EnumerateFrames(This,ppFrames) )

#define ICorDebugChain_GetActiveFrame(This,ppFrame)	\
    ( (This)->lpVtbl -> GetActiveFrame(This,ppFrame) )

#define ICorDebugChain_GetRegisterSet(This,ppRegisters)	\
    ( (This)->lpVtbl -> GetRegisterSet(This,ppRegisters) )

#define ICorDebugChain_GetReason(This,pReason)	\
    ( (This)->lpVtbl -> GetReason(This,pReason) )

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ICorDebugChain_INTERFACE_DEFINED__ */


#ifndef __ICorDebugFrame_INTERFACE_DEFINED__
#define __ICorDebugFrame_INTERFACE_DEFINED__

/* interface ICorDebugFrame */
/* [unique][uuid][local][object] */


EXTERN_C const IID IID_ICorDebugFrame;

#if defined(__cplusplus) && !defined(CINTERFACE)

    MIDL_INTERFACE("CC7BCAEF-8A68-11d2-983C-0000F808342D")
    ICorDebugFrame : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE GetChain(
            /* [out] */ ICorDebugChain **ppChain) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetCode(
            /* [out] */ ICorDebugCode **ppCode) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetFunction(
            /* [out] */ ICorDebugFunction **ppFunction) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetFunctionToken(
            /* [out] */ mdMethodDef *pToken) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetStackRange(
            /* [out] */ CORDB_ADDRESS *pStart,
            /* [out] */ CORDB_ADDRESS *pEnd) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetCaller(
            /* [out] */ ICorDebugFrame **ppFrame) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetCallee(
            /* [out] */ ICorDebugFrame **ppFrame) = 0;

        virtual HRESULT STDMETHODCALLTYPE CreateStepper(
            /* [out] */ ICorDebugStepper **ppStepper) = 0;

    };


#else 	/* C style interface */

    typedef struct ICorDebugFrameVtbl
    {
        BEGIN_INTERFACE

        HRESULT ( STDMETHODCALLTYPE *QueryInterface )(
            ICorDebugFrame * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */
            _COM_Outptr_  void **ppvObject);

        ULONG ( STDMETHODCALLTYPE *AddRef )(
            ICorDebugFrame * This);

        ULONG ( STDMETHODCALLTYPE *Release )(
            ICorDebugFrame * This);

        HRESULT ( STDMETHODCALLTYPE *GetChain )(
            ICorDebugFrame * This,
            /* [out] */ ICorDebugChain **ppChain);

        HRESULT ( STDMETHODCALLTYPE *GetCode )(
            ICorDebugFrame * This,
            /* [out] */ ICorDebugCode **ppCode);

        HRESULT ( STDMETHODCALLTYPE *GetFunction )(
            ICorDebugFrame * This,
            /* [out] */ ICorDebugFunction **ppFunction);

        HRESULT ( STDMETHODCALLTYPE *GetFunctionToken )(
            ICorDebugFrame * This,
            /* [out] */ mdMethodDef *pToken);

        HRESULT ( STDMETHODCALLTYPE *GetStackRange )(
            ICorDebugFrame * This,
            /* [out] */ CORDB_ADDRESS *pStart,
            /* [out] */ CORDB_ADDRESS *pEnd);

        HRESULT ( STDMETHODCALLTYPE *GetCaller )(
            ICorDebugFrame * This,
            /* [out] */ ICorDebugFrame **ppFrame);

        HRESULT ( STDMETHODCALLTYPE *GetCallee )(
            ICorDebugFrame * This,
            /* [out] */ ICorDebugFrame **ppFrame);

        HRESULT ( STDMETHODCALLTYPE *CreateStepper )(
            ICorDebugFrame * This,
            /* [out] */ ICorDebugStepper **ppStepper);

        END_INTERFACE
    } ICorDebugFrameVtbl;

    interface ICorDebugFrame
    {
        CONST_VTBL struct ICorDebugFrameVtbl *lpVtbl;
    };



#ifdef COBJMACROS


#define ICorDebugFrame_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) )

#define ICorDebugFrame_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) )

#define ICorDebugFrame_Release(This)	\
    ( (This)->lpVtbl -> Release(This) )


#define ICorDebugFrame_GetChain(This,ppChain)	\
    ( (This)->lpVtbl -> GetChain(This,ppChain) )

#define ICorDebugFrame_GetCode(This,ppCode)	\
    ( (This)->lpVtbl -> GetCode(This,ppCode) )

#define ICorDebugFrame_GetFunction(This,ppFunction)	\
    ( (This)->lpVtbl -> GetFunction(This,ppFunction) )

#define ICorDebugFrame_GetFunctionToken(This,pToken)	\
    ( (This)->lpVtbl -> GetFunctionToken(This,pToken) )

#define ICorDebugFrame_GetStackRange(This,pStart,pEnd)	\
    ( (This)->lpVtbl -> GetStackRange(This,pStart,pEnd) )

#define ICorDebugFrame_GetCaller(This,ppFrame)	\
    ( (This)->lpVtbl -> GetCaller(This,ppFrame) )

#define ICorDebugFrame_GetCallee(This,ppFrame)	\
    ( (This)->lpVtbl -> GetCallee(This,ppFrame) )

#define ICorDebugFrame_CreateStepper(This,ppStepper)	\
    ( (This)->lpVtbl -> CreateStepper(This,ppStepper) )

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ICorDebugFrame_INTERFACE_DEFINED__ */


#ifndef __ICorDebugInternalFrame_INTERFACE_DEFINED__
#define __ICorDebugInternalFrame_INTERFACE_DEFINED__

/* interface ICorDebugInternalFrame */
/* [unique][uuid][local][object] */

typedef
enum CorDebugInternalFrameType
    {
        STUBFRAME_NONE	= 0,
        STUBFRAME_M2U	= 0x1,
        STUBFRAME_U2M	= 0x2,
        STUBFRAME_APPDOMAIN_TRANSITION	= 0x3,
        STUBFRAME_LIGHTWEIGHT_FUNCTION	= 0x4,
        STUBFRAME_FUNC_EVAL	= 0x5,
        STUBFRAME_INTERNALCALL	= 0x6,
        STUBFRAME_CLASS_INIT	= 0x7,
        STUBFRAME_EXCEPTION	= 0x8,
        STUBFRAME_SECURITY	= 0x9,
        STUBFRAME_JIT_COMPILATION	= 0xa
    } 	CorDebugInternalFrameType;


EXTERN_C const IID IID_ICorDebugInternalFrame;

#if defined(__cplusplus) && !defined(CINTERFACE)

    MIDL_INTERFACE("B92CC7F7-9D2D-45c4-BC2B-621FCC9DFBF4")
    ICorDebugInternalFrame : public ICorDebugFrame
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE GetFrameType(
            /* [out] */ CorDebugInternalFrameType *pType) = 0;

    };


#else 	/* C style interface */

    typedef struct ICorDebugInternalFrameVtbl
    {
        BEGIN_INTERFACE

        HRESULT ( STDMETHODCALLTYPE *QueryInterface )(
            ICorDebugInternalFrame * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */
            _COM_Outptr_  void **ppvObject);

        ULONG ( STDMETHODCALLTYPE *AddRef )(
            ICorDebugInternalFrame * This);

        ULONG ( STDMETHODCALLTYPE *Release )(
            ICorDebugInternalFrame * This);

        HRESULT ( STDMETHODCALLTYPE *GetChain )(
            ICorDebugInternalFrame * This,
            /* [out] */ ICorDebugChain **ppChain);

        HRESULT ( STDMETHODCALLTYPE *GetCode )(
            ICorDebugInternalFrame * This,
            /* [out] */ ICorDebugCode **ppCode);

        HRESULT ( STDMETHODCALLTYPE *GetFunction )(
            ICorDebugInternalFrame * This,
            /* [out] */ ICorDebugFunction **ppFunction);

        HRESULT ( STDMETHODCALLTYPE *GetFunctionToken )(
            ICorDebugInternalFrame * This,
            /* [out] */ mdMethodDef *pToken);

        HRESULT ( STDMETHODCALLTYPE *GetStackRange )(
            ICorDebugInternalFrame * This,
            /* [out] */ CORDB_ADDRESS *pStart,
            /* [out] */ CORDB_ADDRESS *pEnd);

        HRESULT ( STDMETHODCALLTYPE *GetCaller )(
            ICorDebugInternalFrame * This,
            /* [out] */ ICorDebugFrame **ppFrame);

        HRESULT ( STDMETHODCALLTYPE *GetCallee )(
            ICorDebugInternalFrame * This,
            /* [out] */ ICorDebugFrame **ppFrame);

        HRESULT ( STDMETHODCALLTYPE *CreateStepper )(
            ICorDebugInternalFrame * This,
            /* [out] */ ICorDebugStepper **ppStepper);

        HRESULT ( STDMETHODCALLTYPE *GetFrameType )(
            ICorDebugInternalFrame * This,
            /* [out] */ CorDebugInternalFrameType *pType);

        END_INTERFACE
    } ICorDebugInternalFrameVtbl;

    interface ICorDebugInternalFrame
    {
        CONST_VTBL struct ICorDebugInternalFrameVtbl *lpVtbl;
    };



#ifdef COBJMACROS


#define ICorDebugInternalFrame_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) )

#define ICorDebugInternalFrame_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) )

#define ICorDebugInternalFrame_Release(This)	\
    ( (This)->lpVtbl -> Release(This) )


#define ICorDebugInternalFrame_GetChain(This,ppChain)	\
    ( (This)->lpVtbl -> GetChain(This,ppChain) )

#define ICorDebugInternalFrame_GetCode(This,ppCode)	\
    ( (This)->lpVtbl -> GetCode(This,ppCode) )

#define ICorDebugInternalFrame_GetFunction(This,ppFunction)	\
    ( (This)->lpVtbl -> GetFunction(This,ppFunction) )

#define ICorDebugInternalFrame_GetFunctionToken(This,pToken)	\
    ( (This)->lpVtbl -> GetFunctionToken(This,pToken) )

#define ICorDebugInternalFrame_GetStackRange(This,pStart,pEnd)	\
    ( (This)->lpVtbl -> GetStackRange(This,pStart,pEnd) )

#define ICorDebugInternalFrame_GetCaller(This,ppFrame)	\
    ( (This)->lpVtbl -> GetCaller(This,ppFrame) )

#define ICorDebugInternalFrame_GetCallee(This,ppFrame)	\
    ( (This)->lpVtbl -> GetCallee(This,ppFrame) )

#define ICorDebugInternalFrame_CreateStepper(This,ppStepper)	\
    ( (This)->lpVtbl -> CreateStepper(This,ppStepper) )


#define ICorDebugInternalFrame_GetFrameType(This,pType)	\
    ( (This)->lpVtbl -> GetFrameType(This,pType) )

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ICorDebugInternalFrame_INTERFACE_DEFINED__ */


#ifndef __ICorDebugInternalFrame2_INTERFACE_DEFINED__
#define __ICorDebugInternalFrame2_INTERFACE_DEFINED__

/* interface ICorDebugInternalFrame2 */
/* [unique][uuid][local][object] */


EXTERN_C const IID IID_ICorDebugInternalFrame2;

#if defined(__cplusplus) && !defined(CINTERFACE)

    MIDL_INTERFACE("C0815BDC-CFAB-447e-A779-C116B454EB5B")
    ICorDebugInternalFrame2 : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE GetAddress(
            /* [out] */ CORDB_ADDRESS *pAddress) = 0;

        virtual HRESULT STDMETHODCALLTYPE IsCloserToLeaf(
            /* [in] */ ICorDebugFrame *pFrameToCompare,
            /* [out] */ BOOL *pIsCloser) = 0;

    };


#else 	/* C style interface */

    typedef struct ICorDebugInternalFrame2Vtbl
    {
        BEGIN_INTERFACE

        HRESULT ( STDMETHODCALLTYPE *QueryInterface )(
            ICorDebugInternalFrame2 * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */
            _COM_Outptr_  void **ppvObject);

        ULONG ( STDMETHODCALLTYPE *AddRef )(
            ICorDebugInternalFrame2 * This);

        ULONG ( STDMETHODCALLTYPE *Release )(
            ICorDebugInternalFrame2 * This);

        HRESULT ( STDMETHODCALLTYPE *GetAddress )(
            ICorDebugInternalFrame2 * This,
            /* [out] */ CORDB_ADDRESS *pAddress);

        HRESULT ( STDMETHODCALLTYPE *IsCloserToLeaf )(
            ICorDebugInternalFrame2 * This,
            /* [in] */ ICorDebugFrame *pFrameToCompare,
            /* [out] */ BOOL *pIsCloser);

        END_INTERFACE
    } ICorDebugInternalFrame2Vtbl;

    interface ICorDebugInternalFrame2
    {
        CONST_VTBL struct ICorDebugInternalFrame2Vtbl *lpVtbl;
    };



#ifdef COBJMACROS


#define ICorDebugInternalFrame2_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) )

#define ICorDebugInternalFrame2_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) )

#define ICorDebugInternalFrame2_Release(This)	\
    ( (This)->lpVtbl -> Release(This) )


#define ICorDebugInternalFrame2_GetAddress(This,pAddress)	\
    ( (This)->lpVtbl -> GetAddress(This,pAddress) )

#define ICorDebugInternalFrame2_IsCloserToLeaf(This,pFrameToCompare,pIsCloser)	\
    ( (This)->lpVtbl -> IsCloserToLeaf(This,pFrameToCompare,pIsCloser) )

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ICorDebugInternalFrame2_INTERFACE_DEFINED__ */


#ifndef __ICorDebugILFrame_INTERFACE_DEFINED__
#define __ICorDebugILFrame_INTERFACE_DEFINED__

/* interface ICorDebugILFrame */
/* [unique][uuid][local][object] */

typedef
enum CorDebugMappingResult
    {
        MAPPING_PROLOG	= 0x1,
        MAPPING_EPILOG	= 0x2,
        MAPPING_NO_INFO	= 0x4,
        MAPPING_UNMAPPED_ADDRESS	= 0x8,
        MAPPING_EXACT	= 0x10,
        MAPPING_APPROXIMATE	= 0x20
    } 	CorDebugMappingResult;


EXTERN_C const IID IID_ICorDebugILFrame;

#if defined(__cplusplus) && !defined(CINTERFACE)

    MIDL_INTERFACE("03E26311-4F76-11d3-88C6-006097945418")
    ICorDebugILFrame : public ICorDebugFrame
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE GetIP(
            /* [out] */ ULONG32 *pnOffset,
            /* [out] */ CorDebugMappingResult *pMappingResult) = 0;

        virtual HRESULT STDMETHODCALLTYPE SetIP(
            /* [in] */ ULONG32 nOffset) = 0;

        virtual HRESULT STDMETHODCALLTYPE EnumerateLocalVariables(
            /* [out] */ ICorDebugValueEnum **ppValueEnum) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetLocalVariable(
            /* [in] */ DWORD dwIndex,
            /* [out] */ ICorDebugValue **ppValue) = 0;

        virtual HRESULT STDMETHODCALLTYPE EnumerateArguments(
            /* [out] */ ICorDebugValueEnum **ppValueEnum) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetArgument(
            /* [in] */ DWORD dwIndex,
            /* [out] */ ICorDebugValue **ppValue) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetStackDepth(
            /* [out] */ ULONG32 *pDepth) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetStackValue(
            /* [in] */ DWORD dwIndex,
            /* [out] */ ICorDebugValue **ppValue) = 0;

        virtual HRESULT STDMETHODCALLTYPE CanSetIP(
            /* [in] */ ULONG32 nOffset) = 0;

    };


#else 	/* C style interface */

    typedef struct ICorDebugILFrameVtbl
    {
        BEGIN_INTERFACE

        HRESULT ( STDMETHODCALLTYPE *QueryInterface )(
            ICorDebugILFrame * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */
            _COM_Outptr_  void **ppvObject);

        ULONG ( STDMETHODCALLTYPE *AddRef )(
            ICorDebugILFrame * This);

        ULONG ( STDMETHODCALLTYPE *Release )(
            ICorDebugILFrame * This);

        HRESULT ( STDMETHODCALLTYPE *GetChain )(
            ICorDebugILFrame * This,
            /* [out] */ ICorDebugChain **ppChain);

        HRESULT ( STDMETHODCALLTYPE *GetCode )(
            ICorDebugILFrame * This,
            /* [out] */ ICorDebugCode **ppCode);

        HRESULT ( STDMETHODCALLTYPE *GetFunction )(
            ICorDebugILFrame * This,
            /* [out] */ ICorDebugFunction **ppFunction);

        HRESULT ( STDMETHODCALLTYPE *GetFunctionToken )(
            ICorDebugILFrame * This,
            /* [out] */ mdMethodDef *pToken);

        HRESULT ( STDMETHODCALLTYPE *GetStackRange )(
            ICorDebugILFrame * This,
            /* [out] */ CORDB_ADDRESS *pStart,
            /* [out] */ CORDB_ADDRESS *pEnd);

        HRESULT ( STDMETHODCALLTYPE *GetCaller )(
            ICorDebugILFrame * This,
            /* [out] */ ICorDebugFrame **ppFrame);

        HRESULT ( STDMETHODCALLTYPE *GetCallee )(
            ICorDebugILFrame * This,
            /* [out] */ ICorDebugFrame **ppFrame);

        HRESULT ( STDMETHODCALLTYPE *CreateStepper )(
            ICorDebugILFrame * This,
            /* [out] */ ICorDebugStepper **ppStepper);

        HRESULT ( STDMETHODCALLTYPE *GetIP )(
            ICorDebugILFrame * This,
            /* [out] */ ULONG32 *pnOffset,
            /* [out] */ CorDebugMappingResult *pMappingResult);

        HRESULT ( STDMETHODCALLTYPE *SetIP )(
            ICorDebugILFrame * This,
            /* [in] */ ULONG32 nOffset);

        HRESULT ( STDMETHODCALLTYPE *EnumerateLocalVariables )(
            ICorDebugILFrame * This,
            /* [out] */ ICorDebugValueEnum **ppValueEnum);

        HRESULT ( STDMETHODCALLTYPE *GetLocalVariable )(
            ICorDebugILFrame * This,
            /* [in] */ DWORD dwIndex,
            /* [out] */ ICorDebugValue **ppValue);

        HRESULT ( STDMETHODCALLTYPE *EnumerateArguments )(
            ICorDebugILFrame * This,
            /* [out] */ ICorDebugValueEnum **ppValueEnum);

        HRESULT ( STDMETHODCALLTYPE *GetArgument )(
            ICorDebugILFrame * This,
            /* [in] */ DWORD dwIndex,
            /* [out] */ ICorDebugValue **ppValue);

        HRESULT ( STDMETHODCALLTYPE *GetStackDepth )(
            ICorDebugILFrame * This,
            /* [out] */ ULONG32 *pDepth);

        HRESULT ( STDMETHODCALLTYPE *GetStackValue )(
            ICorDebugILFrame * This,
            /* [in] */ DWORD dwIndex,
            /* [out] */ ICorDebugValue **ppValue);

        HRESULT ( STDMETHODCALLTYPE *CanSetIP )(
            ICorDebugILFrame * This,
            /* [in] */ ULONG32 nOffset);

        END_INTERFACE
    } ICorDebugILFrameVtbl;

    interface ICorDebugILFrame
    {
        CONST_VTBL struct ICorDebugILFrameVtbl *lpVtbl;
    };



#ifdef COBJMACROS


#define ICorDebugILFrame_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) )

#define ICorDebugILFrame_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) )

#define ICorDebugILFrame_Release(This)	\
    ( (This)->lpVtbl -> Release(This) )


#define ICorDebugILFrame_GetChain(This,ppChain)	\
    ( (This)->lpVtbl -> GetChain(This,ppChain) )

#define ICorDebugILFrame_GetCode(This,ppCode)	\
    ( (This)->lpVtbl -> GetCode(This,ppCode) )

#define ICorDebugILFrame_GetFunction(This,ppFunction)	\
    ( (This)->lpVtbl -> GetFunction(This,ppFunction) )

#define ICorDebugILFrame_GetFunctionToken(This,pToken)	\
    ( (This)->lpVtbl -> GetFunctionToken(This,pToken) )

#define ICorDebugILFrame_GetStackRange(This,pStart,pEnd)	\
    ( (This)->lpVtbl -> GetStackRange(This,pStart,pEnd) )

#define ICorDebugILFrame_GetCaller(This,ppFrame)	\
    ( (This)->lpVtbl -> GetCaller(This,ppFrame) )

#define ICorDebugILFrame_GetCallee(This,ppFrame)	\
    ( (This)->lpVtbl -> GetCallee(This,ppFrame) )

#define ICorDebugILFrame_CreateStepper(This,ppStepper)	\
    ( (This)->lpVtbl -> CreateStepper(This,ppStepper) )


#define ICorDebugILFrame_GetIP(This,pnOffset,pMappingResult)	\
    ( (This)->lpVtbl -> GetIP(This,pnOffset,pMappingResult) )

#define ICorDebugILFrame_SetIP(This,nOffset)	\
    ( (This)->lpVtbl -> SetIP(This,nOffset) )

#define ICorDebugILFrame_EnumerateLocalVariables(This,ppValueEnum)	\
    ( (This)->lpVtbl -> EnumerateLocalVariables(This,ppValueEnum) )

#define ICorDebugILFrame_GetLocalVariable(This,dwIndex,ppValue)	\
    ( (This)->lpVtbl -> GetLocalVariable(This,dwIndex,ppValue) )

#define ICorDebugILFrame_EnumerateArguments(This,ppValueEnum)	\
    ( (This)->lpVtbl -> EnumerateArguments(This,ppValueEnum) )

#define ICorDebugILFrame_GetArgument(This,dwIndex,ppValue)	\
    ( (This)->lpVtbl -> GetArgument(This,dwIndex,ppValue) )

#define ICorDebugILFrame_GetStackDepth(This,pDepth)	\
    ( (This)->lpVtbl -> GetStackDepth(This,pDepth) )

#define ICorDebugILFrame_GetStackValue(This,dwIndex,ppValue)	\
    ( (This)->lpVtbl -> GetStackValue(This,dwIndex,ppValue) )

#define ICorDebugILFrame_CanSetIP(This,nOffset)	\
    ( (This)->lpVtbl -> CanSetIP(This,nOffset) )

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ICorDebugILFrame_INTERFACE_DEFINED__ */


#ifndef __ICorDebugILFrame2_INTERFACE_DEFINED__
#define __ICorDebugILFrame2_INTERFACE_DEFINED__

/* interface ICorDebugILFrame2 */
/* [unique][uuid][local][object] */


EXTERN_C const IID IID_ICorDebugILFrame2;

#if defined(__cplusplus) && !defined(CINTERFACE)

    MIDL_INTERFACE("5D88A994-6C30-479b-890F-BCEF88B129A5")
    ICorDebugILFrame2 : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE RemapFunction(
            /* [in] */ ULONG32 newILOffset) = 0;

        virtual HRESULT STDMETHODCALLTYPE EnumerateTypeParameters(
            /* [out] */ ICorDebugTypeEnum **ppTyParEnum) = 0;

    };


#else 	/* C style interface */

    typedef struct ICorDebugILFrame2Vtbl
    {
        BEGIN_INTERFACE

        HRESULT ( STDMETHODCALLTYPE *QueryInterface )(
            ICorDebugILFrame2 * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */
            _COM_Outptr_  void **ppvObject);

        ULONG ( STDMETHODCALLTYPE *AddRef )(
            ICorDebugILFrame2 * This);

        ULONG ( STDMETHODCALLTYPE *Release )(
            ICorDebugILFrame2 * This);

        HRESULT ( STDMETHODCALLTYPE *RemapFunction )(
            ICorDebugILFrame2 * This,
            /* [in] */ ULONG32 newILOffset);

        HRESULT ( STDMETHODCALLTYPE *EnumerateTypeParameters )(
            ICorDebugILFrame2 * This,
            /* [out] */ ICorDebugTypeEnum **ppTyParEnum);

        END_INTERFACE
    } ICorDebugILFrame2Vtbl;

    interface ICorDebugILFrame2
    {
        CONST_VTBL struct ICorDebugILFrame2Vtbl *lpVtbl;
    };



#ifdef COBJMACROS


#define ICorDebugILFrame2_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) )

#define ICorDebugILFrame2_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) )

#define ICorDebugILFrame2_Release(This)	\
    ( (This)->lpVtbl -> Release(This) )


#define ICorDebugILFrame2_RemapFunction(This,newILOffset)	\
    ( (This)->lpVtbl -> RemapFunction(This,newILOffset) )

#define ICorDebugILFrame2_EnumerateTypeParameters(This,ppTyParEnum)	\
    ( (This)->lpVtbl -> EnumerateTypeParameters(This,ppTyParEnum) )

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ICorDebugILFrame2_INTERFACE_DEFINED__ */


#ifndef __ICorDebugILFrame3_INTERFACE_DEFINED__
#define __ICorDebugILFrame3_INTERFACE_DEFINED__

/* interface ICorDebugILFrame3 */
/* [unique][uuid][local][object] */


EXTERN_C const IID IID_ICorDebugILFrame3;

#if defined(__cplusplus) && !defined(CINTERFACE)

    MIDL_INTERFACE("9A9E2ED6-04DF-4FE0-BB50-CAB64126AD24")
    ICorDebugILFrame3 : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE GetReturnValueForILOffset(
            ULONG32 ILoffset,
            /* [out] */ ICorDebugValue **ppReturnValue) = 0;

    };


#else 	/* C style interface */

    typedef struct ICorDebugILFrame3Vtbl
    {
        BEGIN_INTERFACE

        HRESULT ( STDMETHODCALLTYPE *QueryInterface )(
            ICorDebugILFrame3 * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */
            _COM_Outptr_  void **ppvObject);

        ULONG ( STDMETHODCALLTYPE *AddRef )(
            ICorDebugILFrame3 * This);

        ULONG ( STDMETHODCALLTYPE *Release )(
            ICorDebugILFrame3 * This);

        HRESULT ( STDMETHODCALLTYPE *GetReturnValueForILOffset )(
            ICorDebugILFrame3 * This,
            ULONG32 ILoffset,
            /* [out] */ ICorDebugValue **ppReturnValue);

        END_INTERFACE
    } ICorDebugILFrame3Vtbl;

    interface ICorDebugILFrame3
    {
        CONST_VTBL struct ICorDebugILFrame3Vtbl *lpVtbl;
    };



#ifdef COBJMACROS


#define ICorDebugILFrame3_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) )

#define ICorDebugILFrame3_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) )

#define ICorDebugILFrame3_Release(This)	\
    ( (This)->lpVtbl -> Release(This) )


#define ICorDebugILFrame3_GetReturnValueForILOffset(This,ILoffset,ppReturnValue)	\
    ( (This)->lpVtbl -> GetReturnValueForILOffset(This,ILoffset,ppReturnValue) )

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ICorDebugILFrame3_INTERFACE_DEFINED__ */


/* interface __MIDL_itf_cordebug_0000_0069 */
/* [local] */

typedef
enum ILCodeKind
    {
        ILCODE_ORIGINAL_IL	= 0x1,
        ILCODE_REJIT_IL	= 0x2
    } 	ILCodeKind;



extern RPC_IF_HANDLE __MIDL_itf_cordebug_0000_0069_v0_0_c_ifspec;
extern RPC_IF_HANDLE __MIDL_itf_cordebug_0000_0069_v0_0_s_ifspec;

#ifndef __ICorDebugILFrame4_INTERFACE_DEFINED__
#define __ICorDebugILFrame4_INTERFACE_DEFINED__

/* interface ICorDebugILFrame4 */
/* [unique][uuid][local][object] */


EXTERN_C const IID IID_ICorDebugILFrame4;

#if defined(__cplusplus) && !defined(CINTERFACE)

    MIDL_INTERFACE("AD914A30-C6D1-4AC5-9C5E-577F3BAA8A45")
    ICorDebugILFrame4 : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE EnumerateLocalVariablesEx(
            /* [in] */ ILCodeKind flags,
            /* [out] */ ICorDebugValueEnum **ppValueEnum) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetLocalVariableEx(
            /* [in] */ ILCodeKind flags,
            /* [in] */ DWORD dwIndex,
            /* [out] */ ICorDebugValue **ppValue) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetCodeEx(
            /* [in] */ ILCodeKind flags,
            /* [out] */ ICorDebugCode **ppCode) = 0;

    };


#else 	/* C style interface */

    typedef struct ICorDebugILFrame4Vtbl
    {
        BEGIN_INTERFACE

        HRESULT ( STDMETHODCALLTYPE *QueryInterface )(
            ICorDebugILFrame4 * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */
            _COM_Outptr_  void **ppvObject);

        ULONG ( STDMETHODCALLTYPE *AddRef )(
            ICorDebugILFrame4 * This);

        ULONG ( STDMETHODCALLTYPE *Release )(
            ICorDebugILFrame4 * This);

        HRESULT ( STDMETHODCALLTYPE *EnumerateLocalVariablesEx )(
            ICorDebugILFrame4 * This,
            /* [in] */ ILCodeKind flags,
            /* [out] */ ICorDebugValueEnum **ppValueEnum);

        HRESULT ( STDMETHODCALLTYPE *GetLocalVariableEx )(
            ICorDebugILFrame4 * This,
            /* [in] */ ILCodeKind flags,
            /* [in] */ DWORD dwIndex,
            /* [out] */ ICorDebugValue **ppValue);

        HRESULT ( STDMETHODCALLTYPE *GetCodeEx )(
            ICorDebugILFrame4 * This,
            /* [in] */ ILCodeKind flags,
            /* [out] */ ICorDebugCode **ppCode);

        END_INTERFACE
    } ICorDebugILFrame4Vtbl;

    interface ICorDebugILFrame4
    {
        CONST_VTBL struct ICorDebugILFrame4Vtbl *lpVtbl;
    };



#ifdef COBJMACROS


#define ICorDebugILFrame4_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) )

#define ICorDebugILFrame4_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) )

#define ICorDebugILFrame4_Release(This)	\
    ( (This)->lpVtbl -> Release(This) )


#define ICorDebugILFrame4_EnumerateLocalVariablesEx(This,flags,ppValueEnum)	\
    ( (This)->lpVtbl -> EnumerateLocalVariablesEx(This,flags,ppValueEnum) )

#define ICorDebugILFrame4_GetLocalVariableEx(This,flags,dwIndex,ppValue)	\
    ( (This)->lpVtbl -> GetLocalVariableEx(This,flags,dwIndex,ppValue) )

#define ICorDebugILFrame4_GetCodeEx(This,flags,ppCode)	\
    ( (This)->lpVtbl -> GetCodeEx(This,flags,ppCode) )

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ICorDebugILFrame4_INTERFACE_DEFINED__ */


#ifndef __ICorDebugNativeFrame_INTERFACE_DEFINED__
#define __ICorDebugNativeFrame_INTERFACE_DEFINED__

/* interface ICorDebugNativeFrame */
/* [unique][uuid][local][object] */


EXTERN_C const IID IID_ICorDebugNativeFrame;

#if defined(__cplusplus) && !defined(CINTERFACE)

    MIDL_INTERFACE("03E26314-4F76-11d3-88C6-006097945418")
    ICorDebugNativeFrame : public ICorDebugFrame
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE GetIP(
            /* [out] */ ULONG32 *pnOffset) = 0;

        virtual HRESULT STDMETHODCALLTYPE SetIP(
            /* [in] */ ULONG32 nOffset) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetRegisterSet(
            /* [out] */ ICorDebugRegisterSet **ppRegisters) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetLocalRegisterValue(
            /* [in] */ CorDebugRegister reg,
            /* [in] */ ULONG cbSigBlob,
            /* [in] */ PCCOR_SIGNATURE pvSigBlob,
            /* [out] */ ICorDebugValue **ppValue) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetLocalDoubleRegisterValue(
            /* [in] */ CorDebugRegister highWordReg,
            /* [in] */ CorDebugRegister lowWordReg,
            /* [in] */ ULONG cbSigBlob,
            /* [in] */ PCCOR_SIGNATURE pvSigBlob,
            /* [out] */ ICorDebugValue **ppValue) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetLocalMemoryValue(
            /* [in] */ CORDB_ADDRESS address,
            /* [in] */ ULONG cbSigBlob,
            /* [in] */ PCCOR_SIGNATURE pvSigBlob,
            /* [out] */ ICorDebugValue **ppValue) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetLocalRegisterMemoryValue(
            /* [in] */ CorDebugRegister highWordReg,
            /* [in] */ CORDB_ADDRESS lowWordAddress,
            /* [in] */ ULONG cbSigBlob,
            /* [in] */ PCCOR_SIGNATURE pvSigBlob,
            /* [out] */ ICorDebugValue **ppValue) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetLocalMemoryRegisterValue(
            /* [in] */ CORDB_ADDRESS highWordAddress,
            /* [in] */ CorDebugRegister lowWordRegister,
            /* [in] */ ULONG cbSigBlob,
            /* [in] */ PCCOR_SIGNATURE pvSigBlob,
            /* [out] */ ICorDebugValue **ppValue) = 0;

        virtual HRESULT STDMETHODCALLTYPE CanSetIP(
            /* [in] */ ULONG32 nOffset) = 0;

    };


#else 	/* C style interface */

    typedef struct ICorDebugNativeFrameVtbl
    {
        BEGIN_INTERFACE

        HRESULT ( STDMETHODCALLTYPE *QueryInterface )(
            ICorDebugNativeFrame * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */
            _COM_Outptr_  void **ppvObject);

        ULONG ( STDMETHODCALLTYPE *AddRef )(
            ICorDebugNativeFrame * This);

        ULONG ( STDMETHODCALLTYPE *Release )(
            ICorDebugNativeFrame * This);

        HRESULT ( STDMETHODCALLTYPE *GetChain )(
            ICorDebugNativeFrame * This,
            /* [out] */ ICorDebugChain **ppChain);

        HRESULT ( STDMETHODCALLTYPE *GetCode )(
            ICorDebugNativeFrame * This,
            /* [out] */ ICorDebugCode **ppCode);

        HRESULT ( STDMETHODCALLTYPE *GetFunction )(
            ICorDebugNativeFrame * This,
            /* [out] */ ICorDebugFunction **ppFunction);

        HRESULT ( STDMETHODCALLTYPE *GetFunctionToken )(
            ICorDebugNativeFrame * This,
            /* [out] */ mdMethodDef *pToken);

        HRESULT ( STDMETHODCALLTYPE *GetStackRange )(
            ICorDebugNativeFrame * This,
            /* [out] */ CORDB_ADDRESS *pStart,
            /* [out] */ CORDB_ADDRESS *pEnd);

        HRESULT ( STDMETHODCALLTYPE *GetCaller )(
            ICorDebugNativeFrame * This,
            /* [out] */ ICorDebugFrame **ppFrame);

        HRESULT ( STDMETHODCALLTYPE *GetCallee )(
            ICorDebugNativeFrame * This,
            /* [out] */ ICorDebugFrame **ppFrame);

        HRESULT ( STDMETHODCALLTYPE *CreateStepper )(
            ICorDebugNativeFrame * This,
            /* [out] */ ICorDebugStepper **ppStepper);

        HRESULT ( STDMETHODCALLTYPE *GetIP )(
            ICorDebugNativeFrame * This,
            /* [out] */ ULONG32 *pnOffset);

        HRESULT ( STDMETHODCALLTYPE *SetIP )(
            ICorDebugNativeFrame * This,
            /* [in] */ ULONG32 nOffset);

        HRESULT ( STDMETHODCALLTYPE *GetRegisterSet )(
            ICorDebugNativeFrame * This,
            /* [out] */ ICorDebugRegisterSet **ppRegisters);

        HRESULT ( STDMETHODCALLTYPE *GetLocalRegisterValue )(
            ICorDebugNativeFrame * This,
            /* [in] */ CorDebugRegister reg,
            /* [in] */ ULONG cbSigBlob,
            /* [in] */ PCCOR_SIGNATURE pvSigBlob,
            /* [out] */ ICorDebugValue **ppValue);

        HRESULT ( STDMETHODCALLTYPE *GetLocalDoubleRegisterValue )(
            ICorDebugNativeFrame * This,
            /* [in] */ CorDebugRegister highWordReg,
            /* [in] */ CorDebugRegister lowWordReg,
            /* [in] */ ULONG cbSigBlob,
            /* [in] */ PCCOR_SIGNATURE pvSigBlob,
            /* [out] */ ICorDebugValue **ppValue);

        HRESULT ( STDMETHODCALLTYPE *GetLocalMemoryValue )(
            ICorDebugNativeFrame * This,
            /* [in] */ CORDB_ADDRESS address,
            /* [in] */ ULONG cbSigBlob,
            /* [in] */ PCCOR_SIGNATURE pvSigBlob,
            /* [out] */ ICorDebugValue **ppValue);

        HRESULT ( STDMETHODCALLTYPE *GetLocalRegisterMemoryValue )(
            ICorDebugNativeFrame * This,
            /* [in] */ CorDebugRegister highWordReg,
            /* [in] */ CORDB_ADDRESS lowWordAddress,
            /* [in] */ ULONG cbSigBlob,
            /* [in] */ PCCOR_SIGNATURE pvSigBlob,
            /* [out] */ ICorDebugValue **ppValue);

        HRESULT ( STDMETHODCALLTYPE *GetLocalMemoryRegisterValue )(
            ICorDebugNativeFrame * This,
            /* [in] */ CORDB_ADDRESS highWordAddress,
            /* [in] */ CorDebugRegister lowWordRegister,
            /* [in] */ ULONG cbSigBlob,
            /* [in] */ PCCOR_SIGNATURE pvSigBlob,
            /* [out] */ ICorDebugValue **ppValue);

        HRESULT ( STDMETHODCALLTYPE *CanSetIP )(
            ICorDebugNativeFrame * This,
            /* [in] */ ULONG32 nOffset);

        END_INTERFACE
    } ICorDebugNativeFrameVtbl;

    interface ICorDebugNativeFrame
    {
        CONST_VTBL struct ICorDebugNativeFrameVtbl *lpVtbl;
    };



#ifdef COBJMACROS


#define ICorDebugNativeFrame_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) )

#define ICorDebugNativeFrame_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) )

#define ICorDebugNativeFrame_Release(This)	\
    ( (This)->lpVtbl -> Release(This) )


#define ICorDebugNativeFrame_GetChain(This,ppChain)	\
    ( (This)->lpVtbl -> GetChain(This,ppChain) )

#define ICorDebugNativeFrame_GetCode(This,ppCode)	\
    ( (This)->lpVtbl -> GetCode(This,ppCode) )

#define ICorDebugNativeFrame_GetFunction(This,ppFunction)	\
    ( (This)->lpVtbl -> GetFunction(This,ppFunction) )

#define ICorDebugNativeFrame_GetFunctionToken(This,pToken)	\
    ( (This)->lpVtbl -> GetFunctionToken(This,pToken) )

#define ICorDebugNativeFrame_GetStackRange(This,pStart,pEnd)	\
    ( (This)->lpVtbl -> GetStackRange(This,pStart,pEnd) )

#define ICorDebugNativeFrame_GetCaller(This,ppFrame)	\
    ( (This)->lpVtbl -> GetCaller(This,ppFrame) )

#define ICorDebugNativeFrame_GetCallee(This,ppFrame)	\
    ( (This)->lpVtbl -> GetCallee(This,ppFrame) )

#define ICorDebugNativeFrame_CreateStepper(This,ppStepper)	\
    ( (This)->lpVtbl -> CreateStepper(This,ppStepper) )


#define ICorDebugNativeFrame_GetIP(This,pnOffset)	\
    ( (This)->lpVtbl -> GetIP(This,pnOffset) )

#define ICorDebugNativeFrame_SetIP(This,nOffset)	\
    ( (This)->lpVtbl -> SetIP(This,nOffset) )

#define ICorDebugNativeFrame_GetRegisterSet(This,ppRegisters)	\
    ( (This)->lpVtbl -> GetRegisterSet(This,ppRegisters) )

#define ICorDebugNativeFrame_GetLocalRegisterValue(This,reg,cbSigBlob,pvSigBlob,ppValue)	\
    ( (This)->lpVtbl -> GetLocalRegisterValue(This,reg,cbSigBlob,pvSigBlob,ppValue) )

#define ICorDebugNativeFrame_GetLocalDoubleRegisterValue(This,highWordReg,lowWordReg,cbSigBlob,pvSigBlob,ppValue)	\
    ( (This)->lpVtbl -> GetLocalDoubleRegisterValue(This,highWordReg,lowWordReg,cbSigBlob,pvSigBlob,ppValue) )

#define ICorDebugNativeFrame_GetLocalMemoryValue(This,address,cbSigBlob,pvSigBlob,ppValue)	\
    ( (This)->lpVtbl -> GetLocalMemoryValue(This,address,cbSigBlob,pvSigBlob,ppValue) )

#define ICorDebugNativeFrame_GetLocalRegisterMemoryValue(This,highWordReg,lowWordAddress,cbSigBlob,pvSigBlob,ppValue)	\
    ( (This)->lpVtbl -> GetLocalRegisterMemoryValue(This,highWordReg,lowWordAddress,cbSigBlob,pvSigBlob,ppValue) )

#define ICorDebugNativeFrame_GetLocalMemoryRegisterValue(This,highWordAddress,lowWordRegister,cbSigBlob,pvSigBlob,ppValue)	\
    ( (This)->lpVtbl -> GetLocalMemoryRegisterValue(This,highWordAddress,lowWordRegister,cbSigBlob,pvSigBlob,ppValue) )

#define ICorDebugNativeFrame_CanSetIP(This,nOffset)	\
    ( (This)->lpVtbl -> CanSetIP(This,nOffset) )

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ICorDebugNativeFrame_INTERFACE_DEFINED__ */


/* interface __MIDL_itf_cordebug_0000_0071 */
/* [local] */

#pragma warning(push)
#pragma warning(disable:28718)


extern RPC_IF_HANDLE __MIDL_itf_cordebug_0000_0071_v0_0_c_ifspec;
extern RPC_IF_HANDLE __MIDL_itf_cordebug_0000_0071_v0_0_s_ifspec;

#ifndef __ICorDebugNativeFrame2_INTERFACE_DEFINED__
#define __ICorDebugNativeFrame2_INTERFACE_DEFINED__

/* interface ICorDebugNativeFrame2 */
/* [unique][uuid][local][object] */


EXTERN_C const IID IID_ICorDebugNativeFrame2;

#if defined(__cplusplus) && !defined(CINTERFACE)

    MIDL_INTERFACE("35389FF1-3684-4c55-A2EE-210F26C60E5E")
    ICorDebugNativeFrame2 : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE IsChild(
            /* [out] */ BOOL *pIsChild) = 0;

        virtual HRESULT STDMETHODCALLTYPE IsMatchingParentFrame(
            /* [in] */ ICorDebugNativeFrame2 *pPotentialParentFrame,
            /* [out] */ BOOL *pIsParent) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetStackParameterSize(
            /* [out] */ ULONG32 *pSize) = 0;

    };


#else 	/* C style interface */

    typedef struct ICorDebugNativeFrame2Vtbl
    {
        BEGIN_INTERFACE

        HRESULT ( STDMETHODCALLTYPE *QueryInterface )(
            ICorDebugNativeFrame2 * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */
            _COM_Outptr_  void **ppvObject);

        ULONG ( STDMETHODCALLTYPE *AddRef )(
            ICorDebugNativeFrame2 * This);

        ULONG ( STDMETHODCALLTYPE *Release )(
            ICorDebugNativeFrame2 * This);

        HRESULT ( STDMETHODCALLTYPE *IsChild )(
            ICorDebugNativeFrame2 * This,
            /* [out] */ BOOL *pIsChild);

        HRESULT ( STDMETHODCALLTYPE *IsMatchingParentFrame )(
            ICorDebugNativeFrame2 * This,
            /* [in] */ ICorDebugNativeFrame2 *pPotentialParentFrame,
            /* [out] */ BOOL *pIsParent);

        HRESULT ( STDMETHODCALLTYPE *GetStackParameterSize )(
            ICorDebugNativeFrame2 * This,
            /* [out] */ ULONG32 *pSize);

        END_INTERFACE
    } ICorDebugNativeFrame2Vtbl;

    interface ICorDebugNativeFrame2
    {
        CONST_VTBL struct ICorDebugNativeFrame2Vtbl *lpVtbl;
    };



#ifdef COBJMACROS


#define ICorDebugNativeFrame2_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) )

#define ICorDebugNativeFrame2_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) )

#define ICorDebugNativeFrame2_Release(This)	\
    ( (This)->lpVtbl -> Release(This) )


#define ICorDebugNativeFrame2_IsChild(This,pIsChild)	\
    ( (This)->lpVtbl -> IsChild(This,pIsChild) )

#define ICorDebugNativeFrame2_IsMatchingParentFrame(This,pPotentialParentFrame,pIsParent)	\
    ( (This)->lpVtbl -> IsMatchingParentFrame(This,pPotentialParentFrame,pIsParent) )

#define ICorDebugNativeFrame2_GetStackParameterSize(This,pSize)	\
    ( (This)->lpVtbl -> GetStackParameterSize(This,pSize) )

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ICorDebugNativeFrame2_INTERFACE_DEFINED__ */


#ifndef __ICorDebugModule3_INTERFACE_DEFINED__
#define __ICorDebugModule3_INTERFACE_DEFINED__

/* interface ICorDebugModule3 */
/* [unique][uuid][local][object] */


EXTERN_C const IID IID_ICorDebugModule3;

#if defined(__cplusplus) && !defined(CINTERFACE)

    MIDL_INTERFACE("86F012BF-FF15-4372-BD30-B6F11CAAE1DD")
    ICorDebugModule3 : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE CreateReaderForInMemorySymbols(
            /* [in] */ REFIID riid,
            /* [iid_is][out] */ void **ppObj) = 0;

    };


#else 	/* C style interface */

    typedef struct ICorDebugModule3Vtbl
    {
        BEGIN_INTERFACE

        HRESULT ( STDMETHODCALLTYPE *QueryInterface )(
            ICorDebugModule3 * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */
            _COM_Outptr_  void **ppvObject);

        ULONG ( STDMETHODCALLTYPE *AddRef )(
            ICorDebugModule3 * This);

        ULONG ( STDMETHODCALLTYPE *Release )(
            ICorDebugModule3 * This);

        HRESULT ( STDMETHODCALLTYPE *CreateReaderForInMemorySymbols )(
            ICorDebugModule3 * This,
            /* [in] */ REFIID riid,
            /* [iid_is][out] */ void **ppObj);

        END_INTERFACE
    } ICorDebugModule3Vtbl;

    interface ICorDebugModule3
    {
        CONST_VTBL struct ICorDebugModule3Vtbl *lpVtbl;
    };



#ifdef COBJMACROS


#define ICorDebugModule3_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) )

#define ICorDebugModule3_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) )

#define ICorDebugModule3_Release(This)	\
    ( (This)->lpVtbl -> Release(This) )


#define ICorDebugModule3_CreateReaderForInMemorySymbols(This,riid,ppObj)	\
    ( (This)->lpVtbl -> CreateReaderForInMemorySymbols(This,riid,ppObj) )

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ICorDebugModule3_INTERFACE_DEFINED__ */


#ifndef __ICorDebugModule4_INTERFACE_DEFINED__
#define __ICorDebugModule4_INTERFACE_DEFINED__

/* interface ICorDebugModule4 */
/* [unique][uuid][local][object] */


EXTERN_C const IID IID_ICorDebugModule4;

#if defined(__cplusplus) && !defined(CINTERFACE)

    MIDL_INTERFACE("FF8B8EAF-25CD-4316-8859-84416DE4402E")
    ICorDebugModule4 : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE IsMappedLayout(
            /* [out] */ BOOL *pIsMapped) = 0;

    };


#else 	/* C style interface */

    typedef struct ICorDebugModule4Vtbl
    {
        BEGIN_INTERFACE

        HRESULT ( STDMETHODCALLTYPE *QueryInterface )(
            ICorDebugModule4 * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */
            _COM_Outptr_  void **ppvObject);

        ULONG ( STDMETHODCALLTYPE *AddRef )(
            ICorDebugModule4 * This);

        ULONG ( STDMETHODCALLTYPE *Release )(
            ICorDebugModule4 * This);

        HRESULT ( STDMETHODCALLTYPE *IsMappedLayout )(
            ICorDebugModule4 * This,
            /* [out] */ BOOL *pIsMapped);

        END_INTERFACE
    } ICorDebugModule4Vtbl;

    interface ICorDebugModule4
    {
        CONST_VTBL struct ICorDebugModule4Vtbl *lpVtbl;
    };



#ifdef COBJMACROS


#define ICorDebugModule4_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) )

#define ICorDebugModule4_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) )

#define ICorDebugModule4_Release(This)	\
    ( (This)->lpVtbl -> Release(This) )


#define ICorDebugModule4_IsMappedLayout(This,pIsMapped)	\
    ( (This)->lpVtbl -> IsMappedLayout(This,pIsMapped) )

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ICorDebugModule4_INTERFACE_DEFINED__ */


#ifndef __ICorDebugRuntimeUnwindableFrame_INTERFACE_DEFINED__
#define __ICorDebugRuntimeUnwindableFrame_INTERFACE_DEFINED__

/* interface ICorDebugRuntimeUnwindableFrame */
/* [unique][uuid][local][object] */


EXTERN_C const IID IID_ICorDebugRuntimeUnwindableFrame;

#if defined(__cplusplus) && !defined(CINTERFACE)

    MIDL_INTERFACE("879CAC0A-4A53-4668-B8E3-CB8473CB187F")
    ICorDebugRuntimeUnwindableFrame : public ICorDebugFrame
    {
    public:
    };


#else 	/* C style interface */

    typedef struct ICorDebugRuntimeUnwindableFrameVtbl
    {
        BEGIN_INTERFACE

        HRESULT ( STDMETHODCALLTYPE *QueryInterface )(
            ICorDebugRuntimeUnwindableFrame * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */
            _COM_Outptr_  void **ppvObject);

        ULONG ( STDMETHODCALLTYPE *AddRef )(
            ICorDebugRuntimeUnwindableFrame * This);

        ULONG ( STDMETHODCALLTYPE *Release )(
            ICorDebugRuntimeUnwindableFrame * This);

        HRESULT ( STDMETHODCALLTYPE *GetChain )(
            ICorDebugRuntimeUnwindableFrame * This,
            /* [out] */ ICorDebugChain **ppChain);

        HRESULT ( STDMETHODCALLTYPE *GetCode )(
            ICorDebugRuntimeUnwindableFrame * This,
            /* [out] */ ICorDebugCode **ppCode);

        HRESULT ( STDMETHODCALLTYPE *GetFunction )(
            ICorDebugRuntimeUnwindableFrame * This,
            /* [out] */ ICorDebugFunction **ppFunction);

        HRESULT ( STDMETHODCALLTYPE *GetFunctionToken )(
            ICorDebugRuntimeUnwindableFrame * This,
            /* [out] */ mdMethodDef *pToken);

        HRESULT ( STDMETHODCALLTYPE *GetStackRange )(
            ICorDebugRuntimeUnwindableFrame * This,
            /* [out] */ CORDB_ADDRESS *pStart,
            /* [out] */ CORDB_ADDRESS *pEnd);

        HRESULT ( STDMETHODCALLTYPE *GetCaller )(
            ICorDebugRuntimeUnwindableFrame * This,
            /* [out] */ ICorDebugFrame **ppFrame);

        HRESULT ( STDMETHODCALLTYPE *GetCallee )(
            ICorDebugRuntimeUnwindableFrame * This,
            /* [out] */ ICorDebugFrame **ppFrame);

        HRESULT ( STDMETHODCALLTYPE *CreateStepper )(
            ICorDebugRuntimeUnwindableFrame * This,
            /* [out] */ ICorDebugStepper **ppStepper);

        END_INTERFACE
    } ICorDebugRuntimeUnwindableFrameVtbl;

    interface ICorDebugRuntimeUnwindableFrame
    {
        CONST_VTBL struct ICorDebugRuntimeUnwindableFrameVtbl *lpVtbl;
    };



#ifdef COBJMACROS


#define ICorDebugRuntimeUnwindableFrame_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) )

#define ICorDebugRuntimeUnwindableFrame_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) )

#define ICorDebugRuntimeUnwindableFrame_Release(This)	\
    ( (This)->lpVtbl -> Release(This) )


#define ICorDebugRuntimeUnwindableFrame_GetChain(This,ppChain)	\
    ( (This)->lpVtbl -> GetChain(This,ppChain) )

#define ICorDebugRuntimeUnwindableFrame_GetCode(This,ppCode)	\
    ( (This)->lpVtbl -> GetCode(This,ppCode) )

#define ICorDebugRuntimeUnwindableFrame_GetFunction(This,ppFunction)	\
    ( (This)->lpVtbl -> GetFunction(This,ppFunction) )

#define ICorDebugRuntimeUnwindableFrame_GetFunctionToken(This,pToken)	\
    ( (This)->lpVtbl -> GetFunctionToken(This,pToken) )

#define ICorDebugRuntimeUnwindableFrame_GetStackRange(This,pStart,pEnd)	\
    ( (This)->lpVtbl -> GetStackRange(This,pStart,pEnd) )

#define ICorDebugRuntimeUnwindableFrame_GetCaller(This,ppFrame)	\
    ( (This)->lpVtbl -> GetCaller(This,ppFrame) )

#define ICorDebugRuntimeUnwindableFrame_GetCallee(This,ppFrame)	\
    ( (This)->lpVtbl -> GetCallee(This,ppFrame) )

#define ICorDebugRuntimeUnwindableFrame_CreateStepper(This,ppStepper)	\
    ( (This)->lpVtbl -> CreateStepper(This,ppStepper) )


#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ICorDebugRuntimeUnwindableFrame_INTERFACE_DEFINED__ */


#ifndef __ICorDebugModule_INTERFACE_DEFINED__
#define __ICorDebugModule_INTERFACE_DEFINED__

/* interface ICorDebugModule */
/* [unique][uuid][local][object] */


EXTERN_C const IID IID_ICorDebugModule;

#if defined(__cplusplus) && !defined(CINTERFACE)

    MIDL_INTERFACE("dba2d8c1-e5c5-4069-8c13-10a7c6abf43d")
    ICorDebugModule : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE GetProcess(
            /* [out] */ ICorDebugProcess **ppProcess) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetBaseAddress(
            /* [out] */ CORDB_ADDRESS *pAddress) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetAssembly(
            /* [out] */ ICorDebugAssembly **ppAssembly) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetName(
            /* [in] */ ULONG32 cchName,
            /* [out] */ ULONG32 *pcchName,
            /* [length_is][size_is][out] */ WCHAR szName[  ]) = 0;

        virtual HRESULT STDMETHODCALLTYPE EnableJITDebugging(
            /* [in] */ BOOL bTrackJITInfo,
            /* [in] */ BOOL bAllowJitOpts) = 0;

        virtual HRESULT STDMETHODCALLTYPE EnableClassLoadCallbacks(
            /* [in] */ BOOL bClassLoadCallbacks) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetFunctionFromToken(
            /* [in] */ mdMethodDef methodDef,
            /* [out] */ ICorDebugFunction **ppFunction) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetFunctionFromRVA(
            /* [in] */ CORDB_ADDRESS rva,
            /* [out] */ ICorDebugFunction **ppFunction) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetClassFromToken(
            /* [in] */ mdTypeDef typeDef,
            /* [out] */ ICorDebugClass **ppClass) = 0;

        virtual HRESULT STDMETHODCALLTYPE CreateBreakpoint(
            /* [out] */ ICorDebugModuleBreakpoint **ppBreakpoint) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetEditAndContinueSnapshot(
            /* [out] */ ICorDebugEditAndContinueSnapshot **ppEditAndContinueSnapshot) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetMetaDataInterface(
            /* [in] */ REFIID riid,
            /* [out] */ IUnknown **ppObj) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetToken(
            /* [out] */ mdModule *pToken) = 0;

        virtual HRESULT STDMETHODCALLTYPE IsDynamic(
            /* [out] */ BOOL *pDynamic) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetGlobalVariableValue(
            /* [in] */ mdFieldDef fieldDef,
            /* [out] */ ICorDebugValue **ppValue) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetSize(
            /* [out] */ ULONG32 *pcBytes) = 0;

        virtual HRESULT STDMETHODCALLTYPE IsInMemory(
            /* [out] */ BOOL *pInMemory) = 0;

    };


#else 	/* C style interface */

    typedef struct ICorDebugModuleVtbl
    {
        BEGIN_INTERFACE

        HRESULT ( STDMETHODCALLTYPE *QueryInterface )(
            ICorDebugModule * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */
            _COM_Outptr_  void **ppvObject);

        ULONG ( STDMETHODCALLTYPE *AddRef )(
            ICorDebugModule * This);

        ULONG ( STDMETHODCALLTYPE *Release )(
            ICorDebugModule * This);

        HRESULT ( STDMETHODCALLTYPE *GetProcess )(
            ICorDebugModule * This,
            /* [out] */ ICorDebugProcess **ppProcess);

        HRESULT ( STDMETHODCALLTYPE *GetBaseAddress )(
            ICorDebugModule * This,
            /* [out] */ CORDB_ADDRESS *pAddress);

        HRESULT ( STDMETHODCALLTYPE *GetAssembly )(
            ICorDebugModule * This,
            /* [out] */ ICorDebugAssembly **ppAssembly);

        HRESULT ( STDMETHODCALLTYPE *GetName )(
            ICorDebugModule * This,
            /* [in] */ ULONG32 cchName,
            /* [out] */ ULONG32 *pcchName,
            /* [length_is][size_is][out] */ WCHAR szName[  ]);

        HRESULT ( STDMETHODCALLTYPE *EnableJITDebugging )(
            ICorDebugModule * This,
            /* [in] */ BOOL bTrackJITInfo,
            /* [in] */ BOOL bAllowJitOpts);

        HRESULT ( STDMETHODCALLTYPE *EnableClassLoadCallbacks )(
            ICorDebugModule * This,
            /* [in] */ BOOL bClassLoadCallbacks);

        HRESULT ( STDMETHODCALLTYPE *GetFunctionFromToken )(
            ICorDebugModule * This,
            /* [in] */ mdMethodDef methodDef,
            /* [out] */ ICorDebugFunction **ppFunction);

        HRESULT ( STDMETHODCALLTYPE *GetFunctionFromRVA )(
            ICorDebugModule * This,
            /* [in] */ CORDB_ADDRESS rva,
            /* [out] */ ICorDebugFunction **ppFunction);

        HRESULT ( STDMETHODCALLTYPE *GetClassFromToken )(
            ICorDebugModule * This,
            /* [in] */ mdTypeDef typeDef,
            /* [out] */ ICorDebugClass **ppClass);

        HRESULT ( STDMETHODCALLTYPE *CreateBreakpoint )(
            ICorDebugModule * This,
            /* [out] */ ICorDebugModuleBreakpoint **ppBreakpoint);

        HRESULT ( STDMETHODCALLTYPE *GetEditAndContinueSnapshot )(
            ICorDebugModule * This,
            /* [out] */ ICorDebugEditAndContinueSnapshot **ppEditAndContinueSnapshot);

        HRESULT ( STDMETHODCALLTYPE *GetMetaDataInterface )(
            ICorDebugModule * This,
            /* [in] */ REFIID riid,
            /* [out] */ IUnknown **ppObj);

        HRESULT ( STDMETHODCALLTYPE *GetToken )(
            ICorDebugModule * This,
            /* [out] */ mdModule *pToken);

        HRESULT ( STDMETHODCALLTYPE *IsDynamic )(
            ICorDebugModule * This,
            /* [out] */ BOOL *pDynamic);

        HRESULT ( STDMETHODCALLTYPE *GetGlobalVariableValue )(
            ICorDebugModule * This,
            /* [in] */ mdFieldDef fieldDef,
            /* [out] */ ICorDebugValue **ppValue);

        HRESULT ( STDMETHODCALLTYPE *GetSize )(
            ICorDebugModule * This,
            /* [out] */ ULONG32 *pcBytes);

        HRESULT ( STDMETHODCALLTYPE *IsInMemory )(
            ICorDebugModule * This,
            /* [out] */ BOOL *pInMemory);

        END_INTERFACE
    } ICorDebugModuleVtbl;

    interface ICorDebugModule
    {
        CONST_VTBL struct ICorDebugModuleVtbl *lpVtbl;
    };



#ifdef COBJMACROS


#define ICorDebugModule_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) )

#define ICorDebugModule_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) )

#define ICorDebugModule_Release(This)	\
    ( (This)->lpVtbl -> Release(This) )


#define ICorDebugModule_GetProcess(This,ppProcess)	\
    ( (This)->lpVtbl -> GetProcess(This,ppProcess) )

#define ICorDebugModule_GetBaseAddress(This,pAddress)	\
    ( (This)->lpVtbl -> GetBaseAddress(This,pAddress) )

#define ICorDebugModule_GetAssembly(This,ppAssembly)	\
    ( (This)->lpVtbl -> GetAssembly(This,ppAssembly) )

#define ICorDebugModule_GetName(This,cchName,pcchName,szName)	\
    ( (This)->lpVtbl -> GetName(This,cchName,pcchName,szName) )

#define ICorDebugModule_EnableJITDebugging(This,bTrackJITInfo,bAllowJitOpts)	\
    ( (This)->lpVtbl -> EnableJITDebugging(This,bTrackJITInfo,bAllowJitOpts) )

#define ICorDebugModule_EnableClassLoadCallbacks(This,bClassLoadCallbacks)	\
    ( (This)->lpVtbl -> EnableClassLoadCallbacks(This,bClassLoadCallbacks) )

#define ICorDebugModule_GetFunctionFromToken(This,methodDef,ppFunction)	\
    ( (This)->lpVtbl -> GetFunctionFromToken(This,methodDef,ppFunction) )

#define ICorDebugModule_GetFunctionFromRVA(This,rva,ppFunction)	\
    ( (This)->lpVtbl -> GetFunctionFromRVA(This,rva,ppFunction) )

#define ICorDebugModule_GetClassFromToken(This,typeDef,ppClass)	\
    ( (This)->lpVtbl -> GetClassFromToken(This,typeDef,ppClass) )

#define ICorDebugModule_CreateBreakpoint(This,ppBreakpoint)	\
    ( (This)->lpVtbl -> CreateBreakpoint(This,ppBreakpoint) )

#define ICorDebugModule_GetEditAndContinueSnapshot(This,ppEditAndContinueSnapshot)	\
    ( (This)->lpVtbl -> GetEditAndContinueSnapshot(This,ppEditAndContinueSnapshot) )

#define ICorDebugModule_GetMetaDataInterface(This,riid,ppObj)	\
    ( (This)->lpVtbl -> GetMetaDataInterface(This,riid,ppObj) )

#define ICorDebugModule_GetToken(This,pToken)	\
    ( (This)->lpVtbl -> GetToken(This,pToken) )

#define ICorDebugModule_IsDynamic(This,pDynamic)	\
    ( (This)->lpVtbl -> IsDynamic(This,pDynamic) )

#define ICorDebugModule_GetGlobalVariableValue(This,fieldDef,ppValue)	\
    ( (This)->lpVtbl -> GetGlobalVariableValue(This,fieldDef,ppValue) )

#define ICorDebugModule_GetSize(This,pcBytes)	\
    ( (This)->lpVtbl -> GetSize(This,pcBytes) )

#define ICorDebugModule_IsInMemory(This,pInMemory)	\
    ( (This)->lpVtbl -> IsInMemory(This,pInMemory) )

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ICorDebugModule_INTERFACE_DEFINED__ */


/* interface __MIDL_itf_cordebug_0000_0076 */
/* [local] */

#pragma warning(pop)


extern RPC_IF_HANDLE __MIDL_itf_cordebug_0000_0076_v0_0_c_ifspec;
extern RPC_IF_HANDLE __MIDL_itf_cordebug_0000_0076_v0_0_s_ifspec;

#ifndef __ICorDebugModule2_INTERFACE_DEFINED__
#define __ICorDebugModule2_INTERFACE_DEFINED__

/* interface ICorDebugModule2 */
/* [unique][uuid][local][object] */


EXTERN_C const IID IID_ICorDebugModule2;

#if defined(__cplusplus) && !defined(CINTERFACE)

    MIDL_INTERFACE("7FCC5FB5-49C0-41de-9938-3B88B5B9ADD7")
    ICorDebugModule2 : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE SetJMCStatus(
            /* [in] */ BOOL bIsJustMyCode,
            /* [in] */ ULONG32 cTokens,
            /* [size_is][in] */ mdToken pTokens[  ]) = 0;

        virtual HRESULT STDMETHODCALLTYPE ApplyChanges(
            /* [in] */ ULONG cbMetadata,
            /* [size_is][in] */ BYTE pbMetadata[  ],
            /* [in] */ ULONG cbIL,
            /* [size_is][in] */ BYTE pbIL[  ]) = 0;

        virtual HRESULT STDMETHODCALLTYPE SetJITCompilerFlags(
            /* [in] */ DWORD dwFlags) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetJITCompilerFlags(
            /* [out] */ DWORD *pdwFlags) = 0;

        virtual HRESULT STDMETHODCALLTYPE ResolveAssembly(
            /* [in] */ mdToken tkAssemblyRef,
            /* [out] */ ICorDebugAssembly **ppAssembly) = 0;

    };


#else 	/* C style interface */

    typedef struct ICorDebugModule2Vtbl
    {
        BEGIN_INTERFACE

        HRESULT ( STDMETHODCALLTYPE *QueryInterface )(
            ICorDebugModule2 * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */
            _COM_Outptr_  void **ppvObject);

        ULONG ( STDMETHODCALLTYPE *AddRef )(
            ICorDebugModule2 * This);

        ULONG ( STDMETHODCALLTYPE *Release )(
            ICorDebugModule2 * This);

        HRESULT ( STDMETHODCALLTYPE *SetJMCStatus )(
            ICorDebugModule2 * This,
            /* [in] */ BOOL bIsJustMyCode,
            /* [in] */ ULONG32 cTokens,
            /* [size_is][in] */ mdToken pTokens[  ]);

        HRESULT ( STDMETHODCALLTYPE *ApplyChanges )(
            ICorDebugModule2 * This,
            /* [in] */ ULONG cbMetadata,
            /* [size_is][in] */ BYTE pbMetadata[  ],
            /* [in] */ ULONG cbIL,
            /* [size_is][in] */ BYTE pbIL[  ]);

        HRESULT ( STDMETHODCALLTYPE *SetJITCompilerFlags )(
            ICorDebugModule2 * This,
            /* [in] */ DWORD dwFlags);

        HRESULT ( STDMETHODCALLTYPE *GetJITCompilerFlags )(
            ICorDebugModule2 * This,
            /* [out] */ DWORD *pdwFlags);

        HRESULT ( STDMETHODCALLTYPE *ResolveAssembly )(
            ICorDebugModule2 * This,
            /* [in] */ mdToken tkAssemblyRef,
            /* [out] */ ICorDebugAssembly **ppAssembly);

        END_INTERFACE
    } ICorDebugModule2Vtbl;

    interface ICorDebugModule2
    {
        CONST_VTBL struct ICorDebugModule2Vtbl *lpVtbl;
    };



#ifdef COBJMACROS


#define ICorDebugModule2_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) )

#define ICorDebugModule2_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) )

#define ICorDebugModule2_Release(This)	\
    ( (This)->lpVtbl -> Release(This) )


#define ICorDebugModule2_SetJMCStatus(This,bIsJustMyCode,cTokens,pTokens)	\
    ( (This)->lpVtbl -> SetJMCStatus(This,bIsJustMyCode,cTokens,pTokens) )

#define ICorDebugModule2_ApplyChanges(This,cbMetadata,pbMetadata,cbIL,pbIL)	\
    ( (This)->lpVtbl -> ApplyChanges(This,cbMetadata,pbMetadata,cbIL,pbIL) )

#define ICorDebugModule2_SetJITCompilerFlags(This,dwFlags)	\
    ( (This)->lpVtbl -> SetJITCompilerFlags(This,dwFlags) )

#define ICorDebugModule2_GetJITCompilerFlags(This,pdwFlags)	\
    ( (This)->lpVtbl -> GetJITCompilerFlags(This,pdwFlags) )

#define ICorDebugModule2_ResolveAssembly(This,tkAssemblyRef,ppAssembly)	\
    ( (This)->lpVtbl -> ResolveAssembly(This,tkAssemblyRef,ppAssembly) )

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ICorDebugModule2_INTERFACE_DEFINED__ */


#ifndef __ICorDebugFunction_INTERFACE_DEFINED__
#define __ICorDebugFunction_INTERFACE_DEFINED__

/* interface ICorDebugFunction */
/* [unique][uuid][local][object] */


EXTERN_C const IID IID_ICorDebugFunction;

#if defined(__cplusplus) && !defined(CINTERFACE)

    MIDL_INTERFACE("CC7BCAF3-8A68-11d2-983C-0000F808342D")
    ICorDebugFunction : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE GetModule(
            /* [out] */ ICorDebugModule **ppModule) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetClass(
            /* [out] */ ICorDebugClass **ppClass) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetToken(
            /* [out] */ mdMethodDef *pMethodDef) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetILCode(
            /* [out] */ ICorDebugCode **ppCode) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetNativeCode(
            /* [out] */ ICorDebugCode **ppCode) = 0;

        virtual HRESULT STDMETHODCALLTYPE CreateBreakpoint(
            /* [out] */ ICorDebugFunctionBreakpoint **ppBreakpoint) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetLocalVarSigToken(
            /* [out] */ mdSignature *pmdSig) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetCurrentVersionNumber(
            /* [out] */ ULONG32 *pnCurrentVersion) = 0;

    };


#else 	/* C style interface */

    typedef struct ICorDebugFunctionVtbl
    {
        BEGIN_INTERFACE

        HRESULT ( STDMETHODCALLTYPE *QueryInterface )(
            ICorDebugFunction * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */
            _COM_Outptr_  void **ppvObject);

        ULONG ( STDMETHODCALLTYPE *AddRef )(
            ICorDebugFunction * This);

        ULONG ( STDMETHODCALLTYPE *Release )(
            ICorDebugFunction * This);

        HRESULT ( STDMETHODCALLTYPE *GetModule )(
            ICorDebugFunction * This,
            /* [out] */ ICorDebugModule **ppModule);

        HRESULT ( STDMETHODCALLTYPE *GetClass )(
            ICorDebugFunction * This,
            /* [out] */ ICorDebugClass **ppClass);

        HRESULT ( STDMETHODCALLTYPE *GetToken )(
            ICorDebugFunction * This,
            /* [out] */ mdMethodDef *pMethodDef);

        HRESULT ( STDMETHODCALLTYPE *GetILCode )(
            ICorDebugFunction * This,
            /* [out] */ ICorDebugCode **ppCode);

        HRESULT ( STDMETHODCALLTYPE *GetNativeCode )(
            ICorDebugFunction * This,
            /* [out] */ ICorDebugCode **ppCode);

        HRESULT ( STDMETHODCALLTYPE *CreateBreakpoint )(
            ICorDebugFunction * This,
            /* [out] */ ICorDebugFunctionBreakpoint **ppBreakpoint);

        HRESULT ( STDMETHODCALLTYPE *GetLocalVarSigToken )(
            ICorDebugFunction * This,
            /* [out] */ mdSignature *pmdSig);

        HRESULT ( STDMETHODCALLTYPE *GetCurrentVersionNumber )(
            ICorDebugFunction * This,
            /* [out] */ ULONG32 *pnCurrentVersion);

        END_INTERFACE
    } ICorDebugFunctionVtbl;

    interface ICorDebugFunction
    {
        CONST_VTBL struct ICorDebugFunctionVtbl *lpVtbl;
    };



#ifdef COBJMACROS


#define ICorDebugFunction_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) )

#define ICorDebugFunction_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) )

#define ICorDebugFunction_Release(This)	\
    ( (This)->lpVtbl -> Release(This) )


#define ICorDebugFunction_GetModule(This,ppModule)	\
    ( (This)->lpVtbl -> GetModule(This,ppModule) )

#define ICorDebugFunction_GetClass(This,ppClass)	\
    ( (This)->lpVtbl -> GetClass(This,ppClass) )

#define ICorDebugFunction_GetToken(This,pMethodDef)	\
    ( (This)->lpVtbl -> GetToken(This,pMethodDef) )

#define ICorDebugFunction_GetILCode(This,ppCode)	\
    ( (This)->lpVtbl -> GetILCode(This,ppCode) )

#define ICorDebugFunction_GetNativeCode(This,ppCode)	\
    ( (This)->lpVtbl -> GetNativeCode(This,ppCode) )

#define ICorDebugFunction_CreateBreakpoint(This,ppBreakpoint)	\
    ( (This)->lpVtbl -> CreateBreakpoint(This,ppBreakpoint) )

#define ICorDebugFunction_GetLocalVarSigToken(This,pmdSig)	\
    ( (This)->lpVtbl -> GetLocalVarSigToken(This,pmdSig) )

#define ICorDebugFunction_GetCurrentVersionNumber(This,pnCurrentVersion)	\
    ( (This)->lpVtbl -> GetCurrentVersionNumber(This,pnCurrentVersion) )

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ICorDebugFunction_INTERFACE_DEFINED__ */


#ifndef __ICorDebugFunction2_INTERFACE_DEFINED__
#define __ICorDebugFunction2_INTERFACE_DEFINED__

/* interface ICorDebugFunction2 */
/* [unique][uuid][local][object] */


EXTERN_C const IID IID_ICorDebugFunction2;

#if defined(__cplusplus) && !defined(CINTERFACE)

    MIDL_INTERFACE("EF0C490B-94C3-4e4d-B629-DDC134C532D8")
    ICorDebugFunction2 : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE SetJMCStatus(
            /* [in] */ BOOL bIsJustMyCode) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetJMCStatus(
            /* [out] */ BOOL *pbIsJustMyCode) = 0;

        virtual HRESULT STDMETHODCALLTYPE EnumerateNativeCode(
            /* [out] */ ICorDebugCodeEnum **ppCodeEnum) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetVersionNumber(
            /* [out] */ ULONG32 *pnVersion) = 0;

    };


#else 	/* C style interface */

    typedef struct ICorDebugFunction2Vtbl
    {
        BEGIN_INTERFACE

        HRESULT ( STDMETHODCALLTYPE *QueryInterface )(
            ICorDebugFunction2 * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */
            _COM_Outptr_  void **ppvObject);

        ULONG ( STDMETHODCALLTYPE *AddRef )(
            ICorDebugFunction2 * This);

        ULONG ( STDMETHODCALLTYPE *Release )(
            ICorDebugFunction2 * This);

        HRESULT ( STDMETHODCALLTYPE *SetJMCStatus )(
            ICorDebugFunction2 * This,
            /* [in] */ BOOL bIsJustMyCode);

        HRESULT ( STDMETHODCALLTYPE *GetJMCStatus )(
            ICorDebugFunction2 * This,
            /* [out] */ BOOL *pbIsJustMyCode);

        HRESULT ( STDMETHODCALLTYPE *EnumerateNativeCode )(
            ICorDebugFunction2 * This,
            /* [out] */ ICorDebugCodeEnum **ppCodeEnum);

        HRESULT ( STDMETHODCALLTYPE *GetVersionNumber )(
            ICorDebugFunction2 * This,
            /* [out] */ ULONG32 *pnVersion);

        END_INTERFACE
    } ICorDebugFunction2Vtbl;

    interface ICorDebugFunction2
    {
        CONST_VTBL struct ICorDebugFunction2Vtbl *lpVtbl;
    };



#ifdef COBJMACROS


#define ICorDebugFunction2_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) )

#define ICorDebugFunction2_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) )

#define ICorDebugFunction2_Release(This)	\
    ( (This)->lpVtbl -> Release(This) )


#define ICorDebugFunction2_SetJMCStatus(This,bIsJustMyCode)	\
    ( (This)->lpVtbl -> SetJMCStatus(This,bIsJustMyCode) )

#define ICorDebugFunction2_GetJMCStatus(This,pbIsJustMyCode)	\
    ( (This)->lpVtbl -> GetJMCStatus(This,pbIsJustMyCode) )

#define ICorDebugFunction2_EnumerateNativeCode(This,ppCodeEnum)	\
    ( (This)->lpVtbl -> EnumerateNativeCode(This,ppCodeEnum) )

#define ICorDebugFunction2_GetVersionNumber(This,pnVersion)	\
    ( (This)->lpVtbl -> GetVersionNumber(This,pnVersion) )

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ICorDebugFunction2_INTERFACE_DEFINED__ */


#ifndef __ICorDebugFunction3_INTERFACE_DEFINED__
#define __ICorDebugFunction3_INTERFACE_DEFINED__

/* interface ICorDebugFunction3 */
/* [unique][uuid][local][object] */


EXTERN_C const IID IID_ICorDebugFunction3;

#if defined(__cplusplus) && !defined(CINTERFACE)

    MIDL_INTERFACE("09B70F28-E465-482D-99E0-81A165EB0532")
    ICorDebugFunction3 : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE GetActiveReJitRequestILCode(
            ICorDebugILCode **ppReJitedILCode) = 0;

    };


#else 	/* C style interface */

    typedef struct ICorDebugFunction3Vtbl
    {
        BEGIN_INTERFACE

        HRESULT ( STDMETHODCALLTYPE *QueryInterface )(
            ICorDebugFunction3 * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */
            _COM_Outptr_  void **ppvObject);

        ULONG ( STDMETHODCALLTYPE *AddRef )(
            ICorDebugFunction3 * This);

        ULONG ( STDMETHODCALLTYPE *Release )(
            ICorDebugFunction3 * This);

        HRESULT ( STDMETHODCALLTYPE *GetActiveReJitRequestILCode )(
            ICorDebugFunction3 * This,
            ICorDebugILCode **ppReJitedILCode);

        END_INTERFACE
    } ICorDebugFunction3Vtbl;

    interface ICorDebugFunction3
    {
        CONST_VTBL struct ICorDebugFunction3Vtbl *lpVtbl;
    };



#ifdef COBJMACROS


#define ICorDebugFunction3_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) )

#define ICorDebugFunction3_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) )

#define ICorDebugFunction3_Release(This)	\
    ( (This)->lpVtbl -> Release(This) )


#define ICorDebugFunction3_GetActiveReJitRequestILCode(This,ppReJitedILCode)	\
    ( (This)->lpVtbl -> GetActiveReJitRequestILCode(This,ppReJitedILCode) )

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ICorDebugFunction3_INTERFACE_DEFINED__ */


#ifndef __ICorDebugFunction4_INTERFACE_DEFINED__
#define __ICorDebugFunction4_INTERFACE_DEFINED__

/* interface ICorDebugFunction4 */
/* [unique][uuid][local][object] */


EXTERN_C const IID IID_ICorDebugFunction4;

#if defined(__cplusplus) && !defined(CINTERFACE)

    MIDL_INTERFACE("72965963-34fd-46e9-9434-b817fe6e7f43")
    ICorDebugFunction4 : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE CreateNativeBreakpoint(
            ICorDebugFunctionBreakpoint **ppBreakpoint) = 0;

    };


#else 	/* C style interface */

    typedef struct ICorDebugFunction4Vtbl
    {
        BEGIN_INTERFACE

        HRESULT ( STDMETHODCALLTYPE *QueryInterface )(
            ICorDebugFunction4 * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */
            _COM_Outptr_  void **ppvObject);

        ULONG ( STDMETHODCALLTYPE *AddRef )(
            ICorDebugFunction4 * This);

        ULONG ( STDMETHODCALLTYPE *Release )(
            ICorDebugFunction4 * This);

        HRESULT ( STDMETHODCALLTYPE *CreateNativeBreakpoint )(
            ICorDebugFunction4 * This,
            ICorDebugFunctionBreakpoint **ppBreakpoint);

        END_INTERFACE
    } ICorDebugFunction4Vtbl;

    interface ICorDebugFunction4
    {
        CONST_VTBL struct ICorDebugFunction4Vtbl *lpVtbl;
    };



#ifdef COBJMACROS


#define ICorDebugFunction4_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) )

#define ICorDebugFunction4_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) )

#define ICorDebugFunction4_Release(This)	\
    ( (This)->lpVtbl -> Release(This) )


#define ICorDebugFunction4_CreateNativeBreakpoint(This,ppBreakpoint)	\
    ( (This)->lpVtbl -> CreateNativeBreakpoint(This,ppBreakpoint) )

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ICorDebugFunction4_INTERFACE_DEFINED__ */


#ifndef __ICorDebugCode_INTERFACE_DEFINED__
#define __ICorDebugCode_INTERFACE_DEFINED__

/* interface ICorDebugCode */
/* [unique][uuid][local][object] */


EXTERN_C const IID IID_ICorDebugCode;

#if defined(__cplusplus) && !defined(CINTERFACE)

    MIDL_INTERFACE("CC7BCAF4-8A68-11d2-983C-0000F808342D")
    ICorDebugCode : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE IsIL(
            /* [out] */ BOOL *pbIL) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetFunction(
            /* [out] */ ICorDebugFunction **ppFunction) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetAddress(
            /* [out] */ CORDB_ADDRESS *pStart) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetSize(
            /* [out] */ ULONG32 *pcBytes) = 0;

        virtual HRESULT STDMETHODCALLTYPE CreateBreakpoint(
            /* [in] */ ULONG32 offset,
            /* [out] */ ICorDebugFunctionBreakpoint **ppBreakpoint) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetCode(
            /* [in] */ ULONG32 startOffset,
            /* [in] */ ULONG32 endOffset,
            /* [in] */ ULONG32 cBufferAlloc,
            /* [length_is][size_is][out] */ BYTE buffer[  ],
            /* [out] */ ULONG32 *pcBufferSize) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetVersionNumber(
            /* [out] */ ULONG32 *nVersion) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetILToNativeMapping(
            /* [in] */ ULONG32 cMap,
            /* [out] */ ULONG32 *pcMap,
            /* [length_is][size_is][out] */ COR_DEBUG_IL_TO_NATIVE_MAP map[  ]) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetEnCRemapSequencePoints(
            /* [in] */ ULONG32 cMap,
            /* [out] */ ULONG32 *pcMap,
            /* [length_is][size_is][out] */ ULONG32 offsets[  ]) = 0;

    };


#else 	/* C style interface */

    typedef struct ICorDebugCodeVtbl
    {
        BEGIN_INTERFACE

        HRESULT ( STDMETHODCALLTYPE *QueryInterface )(
            ICorDebugCode * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */
            _COM_Outptr_  void **ppvObject);

        ULONG ( STDMETHODCALLTYPE *AddRef )(
            ICorDebugCode * This);

        ULONG ( STDMETHODCALLTYPE *Release )(
            ICorDebugCode * This);

        HRESULT ( STDMETHODCALLTYPE *IsIL )(
            ICorDebugCode * This,
            /* [out] */ BOOL *pbIL);

        HRESULT ( STDMETHODCALLTYPE *GetFunction )(
            ICorDebugCode * This,
            /* [out] */ ICorDebugFunction **ppFunction);

        HRESULT ( STDMETHODCALLTYPE *GetAddress )(
            ICorDebugCode * This,
            /* [out] */ CORDB_ADDRESS *pStart);

        HRESULT ( STDMETHODCALLTYPE *GetSize )(
            ICorDebugCode * This,
            /* [out] */ ULONG32 *pcBytes);

        HRESULT ( STDMETHODCALLTYPE *CreateBreakpoint )(
            ICorDebugCode * This,
            /* [in] */ ULONG32 offset,
            /* [out] */ ICorDebugFunctionBreakpoint **ppBreakpoint);

        HRESULT ( STDMETHODCALLTYPE *GetCode )(
            ICorDebugCode * This,
            /* [in] */ ULONG32 startOffset,
            /* [in] */ ULONG32 endOffset,
            /* [in] */ ULONG32 cBufferAlloc,
            /* [length_is][size_is][out] */ BYTE buffer[  ],
            /* [out] */ ULONG32 *pcBufferSize);

        HRESULT ( STDMETHODCALLTYPE *GetVersionNumber )(
            ICorDebugCode * This,
            /* [out] */ ULONG32 *nVersion);

        HRESULT ( STDMETHODCALLTYPE *GetILToNativeMapping )(
            ICorDebugCode * This,
            /* [in] */ ULONG32 cMap,
            /* [out] */ ULONG32 *pcMap,
            /* [length_is][size_is][out] */ COR_DEBUG_IL_TO_NATIVE_MAP map[  ]);

        HRESULT ( STDMETHODCALLTYPE *GetEnCRemapSequencePoints )(
            ICorDebugCode * This,
            /* [in] */ ULONG32 cMap,
            /* [out] */ ULONG32 *pcMap,
            /* [length_is][size_is][out] */ ULONG32 offsets[  ]);

        END_INTERFACE
    } ICorDebugCodeVtbl;

    interface ICorDebugCode
    {
        CONST_VTBL struct ICorDebugCodeVtbl *lpVtbl;
    };



#ifdef COBJMACROS


#define ICorDebugCode_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) )

#define ICorDebugCode_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) )

#define ICorDebugCode_Release(This)	\
    ( (This)->lpVtbl -> Release(This) )


#define ICorDebugCode_IsIL(This,pbIL)	\
    ( (This)->lpVtbl -> IsIL(This,pbIL) )

#define ICorDebugCode_GetFunction(This,ppFunction)	\
    ( (This)->lpVtbl -> GetFunction(This,ppFunction) )

#define ICorDebugCode_GetAddress(This,pStart)	\
    ( (This)->lpVtbl -> GetAddress(This,pStart) )

#define ICorDebugCode_GetSize(This,pcBytes)	\
    ( (This)->lpVtbl -> GetSize(This,pcBytes) )

#define ICorDebugCode_CreateBreakpoint(This,offset,ppBreakpoint)	\
    ( (This)->lpVtbl -> CreateBreakpoint(This,offset,ppBreakpoint) )

#define ICorDebugCode_GetCode(This,startOffset,endOffset,cBufferAlloc,buffer,pcBufferSize)	\
    ( (This)->lpVtbl -> GetCode(This,startOffset,endOffset,cBufferAlloc,buffer,pcBufferSize) )

#define ICorDebugCode_GetVersionNumber(This,nVersion)	\
    ( (This)->lpVtbl -> GetVersionNumber(This,nVersion) )

#define ICorDebugCode_GetILToNativeMapping(This,cMap,pcMap,map)	\
    ( (This)->lpVtbl -> GetILToNativeMapping(This,cMap,pcMap,map) )

#define ICorDebugCode_GetEnCRemapSequencePoints(This,cMap,pcMap,offsets)	\
    ( (This)->lpVtbl -> GetEnCRemapSequencePoints(This,cMap,pcMap,offsets) )

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ICorDebugCode_INTERFACE_DEFINED__ */


#ifndef __ICorDebugCode2_INTERFACE_DEFINED__
#define __ICorDebugCode2_INTERFACE_DEFINED__

/* interface ICorDebugCode2 */
/* [unique][uuid][local][object] */

typedef struct _CodeChunkInfo
    {
    CORDB_ADDRESS startAddr;
    ULONG32 length;
    } 	CodeChunkInfo;


EXTERN_C const IID IID_ICorDebugCode2;

#if defined(__cplusplus) && !defined(CINTERFACE)

    MIDL_INTERFACE("5F696509-452F-4436-A3FE-4D11FE7E2347")
    ICorDebugCode2 : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE GetCodeChunks(
            /* [in] */ ULONG32 cbufSize,
            /* [out] */ ULONG32 *pcnumChunks,
            /* [length_is][size_is][out] */ CodeChunkInfo chunks[  ]) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetCompilerFlags(
            /* [out] */ DWORD *pdwFlags) = 0;

    };


#else 	/* C style interface */

    typedef struct ICorDebugCode2Vtbl
    {
        BEGIN_INTERFACE

        HRESULT ( STDMETHODCALLTYPE *QueryInterface )(
            ICorDebugCode2 * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */
            _COM_Outptr_  void **ppvObject);

        ULONG ( STDMETHODCALLTYPE *AddRef )(
            ICorDebugCode2 * This);

        ULONG ( STDMETHODCALLTYPE *Release )(
            ICorDebugCode2 * This);

        HRESULT ( STDMETHODCALLTYPE *GetCodeChunks )(
            ICorDebugCode2 * This,
            /* [in] */ ULONG32 cbufSize,
            /* [out] */ ULONG32 *pcnumChunks,
            /* [length_is][size_is][out] */ CodeChunkInfo chunks[  ]);

        HRESULT ( STDMETHODCALLTYPE *GetCompilerFlags )(
            ICorDebugCode2 * This,
            /* [out] */ DWORD *pdwFlags);

        END_INTERFACE
    } ICorDebugCode2Vtbl;

    interface ICorDebugCode2
    {
        CONST_VTBL struct ICorDebugCode2Vtbl *lpVtbl;
    };



#ifdef COBJMACROS


#define ICorDebugCode2_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) )

#define ICorDebugCode2_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) )

#define ICorDebugCode2_Release(This)	\
    ( (This)->lpVtbl -> Release(This) )


#define ICorDebugCode2_GetCodeChunks(This,cbufSize,pcnumChunks,chunks)	\
    ( (This)->lpVtbl -> GetCodeChunks(This,cbufSize,pcnumChunks,chunks) )

#define ICorDebugCode2_GetCompilerFlags(This,pdwFlags)	\
    ( (This)->lpVtbl -> GetCompilerFlags(This,pdwFlags) )

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ICorDebugCode2_INTERFACE_DEFINED__ */


#ifndef __ICorDebugCode3_INTERFACE_DEFINED__
#define __ICorDebugCode3_INTERFACE_DEFINED__

/* interface ICorDebugCode3 */
/* [unique][uuid][local][object] */


EXTERN_C const IID IID_ICorDebugCode3;

#if defined(__cplusplus) && !defined(CINTERFACE)

    MIDL_INTERFACE("D13D3E88-E1F2-4020-AA1D-3D162DCBE966")
    ICorDebugCode3 : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE GetReturnValueLiveOffset(
            /* [in] */ ULONG32 ILoffset,
            /* [in] */ ULONG32 bufferSize,
            /* [out] */ ULONG32 *pFetched,
            /* [length_is][size_is][out] */ ULONG32 pOffsets[  ]) = 0;

    };


#else 	/* C style interface */

    typedef struct ICorDebugCode3Vtbl
    {
        BEGIN_INTERFACE

        HRESULT ( STDMETHODCALLTYPE *QueryInterface )(
            ICorDebugCode3 * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */
            _COM_Outptr_  void **ppvObject);

        ULONG ( STDMETHODCALLTYPE *AddRef )(
            ICorDebugCode3 * This);

        ULONG ( STDMETHODCALLTYPE *Release )(
            ICorDebugCode3 * This);

        HRESULT ( STDMETHODCALLTYPE *GetReturnValueLiveOffset )(
            ICorDebugCode3 * This,
            /* [in] */ ULONG32 ILoffset,
            /* [in] */ ULONG32 bufferSize,
            /* [out] */ ULONG32 *pFetched,
            /* [length_is][size_is][out] */ ULONG32 pOffsets[  ]);

        END_INTERFACE
    } ICorDebugCode3Vtbl;

    interface ICorDebugCode3
    {
        CONST_VTBL struct ICorDebugCode3Vtbl *lpVtbl;
    };



#ifdef COBJMACROS


#define ICorDebugCode3_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) )

#define ICorDebugCode3_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) )

#define ICorDebugCode3_Release(This)	\
    ( (This)->lpVtbl -> Release(This) )


#define ICorDebugCode3_GetReturnValueLiveOffset(This,ILoffset,bufferSize,pFetched,pOffsets)	\
    ( (This)->lpVtbl -> GetReturnValueLiveOffset(This,ILoffset,bufferSize,pFetched,pOffsets) )

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ICorDebugCode3_INTERFACE_DEFINED__ */


#ifndef __ICorDebugCode4_INTERFACE_DEFINED__
#define __ICorDebugCode4_INTERFACE_DEFINED__

/* interface ICorDebugCode4 */
/* [unique][uuid][local][object] */


EXTERN_C const IID IID_ICorDebugCode4;

#if defined(__cplusplus) && !defined(CINTERFACE)

    MIDL_INTERFACE("18221fa4-20cb-40fa-b19d-9f91c4fa8c14")
    ICorDebugCode4 : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE EnumerateVariableHomes(
            /* [out] */ ICorDebugVariableHomeEnum **ppEnum) = 0;

    };


#else 	/* C style interface */

    typedef struct ICorDebugCode4Vtbl
    {
        BEGIN_INTERFACE

        HRESULT ( STDMETHODCALLTYPE *QueryInterface )(
            ICorDebugCode4 * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */
            _COM_Outptr_  void **ppvObject);

        ULONG ( STDMETHODCALLTYPE *AddRef )(
            ICorDebugCode4 * This);

        ULONG ( STDMETHODCALLTYPE *Release )(
            ICorDebugCode4 * This);

        HRESULT ( STDMETHODCALLTYPE *EnumerateVariableHomes )(
            ICorDebugCode4 * This,
            /* [out] */ ICorDebugVariableHomeEnum **ppEnum);

        END_INTERFACE
    } ICorDebugCode4Vtbl;

    interface ICorDebugCode4
    {
        CONST_VTBL struct ICorDebugCode4Vtbl *lpVtbl;
    };



#ifdef COBJMACROS


#define ICorDebugCode4_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) )

#define ICorDebugCode4_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) )

#define ICorDebugCode4_Release(This)	\
    ( (This)->lpVtbl -> Release(This) )


#define ICorDebugCode4_EnumerateVariableHomes(This,ppEnum)	\
    ( (This)->lpVtbl -> EnumerateVariableHomes(This,ppEnum) )

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ICorDebugCode4_INTERFACE_DEFINED__ */


#ifndef __ICorDebugILCode_INTERFACE_DEFINED__
#define __ICorDebugILCode_INTERFACE_DEFINED__

/* interface ICorDebugILCode */
/* [unique][uuid][local][object] */

typedef struct _CorDebugEHClause
    {
    ULONG32 Flags;
    ULONG32 TryOffset;
    ULONG32 TryLength;
    ULONG32 HandlerOffset;
    ULONG32 HandlerLength;
    ULONG32 ClassToken;
    ULONG32 FilterOffset;
    } 	CorDebugEHClause;


EXTERN_C const IID IID_ICorDebugILCode;

#if defined(__cplusplus) && !defined(CINTERFACE)

    MIDL_INTERFACE("598D46C2-C877-42A7-89D2-3D0C7F1C1264")
    ICorDebugILCode : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE GetEHClauses(
            /* [in] */ ULONG32 cClauses,
            /* [out] */ ULONG32 *pcClauses,
            /* [length_is][size_is][out] */ CorDebugEHClause clauses[  ]) = 0;

    };


#else 	/* C style interface */

    typedef struct ICorDebugILCodeVtbl
    {
        BEGIN_INTERFACE

        HRESULT ( STDMETHODCALLTYPE *QueryInterface )(
            ICorDebugILCode * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */
            _COM_Outptr_  void **ppvObject);

        ULONG ( STDMETHODCALLTYPE *AddRef )(
            ICorDebugILCode * This);

        ULONG ( STDMETHODCALLTYPE *Release )(
            ICorDebugILCode * This);

        HRESULT ( STDMETHODCALLTYPE *GetEHClauses )(
            ICorDebugILCode * This,
            /* [in] */ ULONG32 cClauses,
            /* [out] */ ULONG32 *pcClauses,
            /* [length_is][size_is][out] */ CorDebugEHClause clauses[  ]);

        END_INTERFACE
    } ICorDebugILCodeVtbl;

    interface ICorDebugILCode
    {
        CONST_VTBL struct ICorDebugILCodeVtbl *lpVtbl;
    };



#ifdef COBJMACROS


#define ICorDebugILCode_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) )

#define ICorDebugILCode_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) )

#define ICorDebugILCode_Release(This)	\
    ( (This)->lpVtbl -> Release(This) )


#define ICorDebugILCode_GetEHClauses(This,cClauses,pcClauses,clauses)	\
    ( (This)->lpVtbl -> GetEHClauses(This,cClauses,pcClauses,clauses) )

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ICorDebugILCode_INTERFACE_DEFINED__ */


#ifndef __ICorDebugILCode2_INTERFACE_DEFINED__
#define __ICorDebugILCode2_INTERFACE_DEFINED__

/* interface ICorDebugILCode2 */
/* [unique][uuid][local][object] */


EXTERN_C const IID IID_ICorDebugILCode2;

#if defined(__cplusplus) && !defined(CINTERFACE)

    MIDL_INTERFACE("46586093-D3F5-4DB6-ACDB-955BCE228C15")
    ICorDebugILCode2 : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE GetLocalVarSigToken(
            /* [out] */ mdSignature *pmdSig) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetInstrumentedILMap(
            /* [in] */ ULONG32 cMap,
            /* [out] */ ULONG32 *pcMap,
            /* [length_is][size_is][out] */ COR_IL_MAP map[  ]) = 0;

    };


#else 	/* C style interface */

    typedef struct ICorDebugILCode2Vtbl
    {
        BEGIN_INTERFACE

        HRESULT ( STDMETHODCALLTYPE *QueryInterface )(
            ICorDebugILCode2 * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */
            _COM_Outptr_  void **ppvObject);

        ULONG ( STDMETHODCALLTYPE *AddRef )(
            ICorDebugILCode2 * This);

        ULONG ( STDMETHODCALLTYPE *Release )(
            ICorDebugILCode2 * This);

        HRESULT ( STDMETHODCALLTYPE *GetLocalVarSigToken )(
            ICorDebugILCode2 * This,
            /* [out] */ mdSignature *pmdSig);

        HRESULT ( STDMETHODCALLTYPE *GetInstrumentedILMap )(
            ICorDebugILCode2 * This,
            /* [in] */ ULONG32 cMap,
            /* [out] */ ULONG32 *pcMap,
            /* [length_is][size_is][out] */ COR_IL_MAP map[  ]);

        END_INTERFACE
    } ICorDebugILCode2Vtbl;

    interface ICorDebugILCode2
    {
        CONST_VTBL struct ICorDebugILCode2Vtbl *lpVtbl;
    };



#ifdef COBJMACROS


#define ICorDebugILCode2_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) )

#define ICorDebugILCode2_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) )

#define ICorDebugILCode2_Release(This)	\
    ( (This)->lpVtbl -> Release(This) )


#define ICorDebugILCode2_GetLocalVarSigToken(This,pmdSig)	\
    ( (This)->lpVtbl -> GetLocalVarSigToken(This,pmdSig) )

#define ICorDebugILCode2_GetInstrumentedILMap(This,cMap,pcMap,map)	\
    ( (This)->lpVtbl -> GetInstrumentedILMap(This,cMap,pcMap,map) )

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ICorDebugILCode2_INTERFACE_DEFINED__ */


#ifndef __ICorDebugClass_INTERFACE_DEFINED__
#define __ICorDebugClass_INTERFACE_DEFINED__

/* interface ICorDebugClass */
/* [unique][uuid][local][object] */


EXTERN_C const IID IID_ICorDebugClass;

#if defined(__cplusplus) && !defined(CINTERFACE)

    MIDL_INTERFACE("CC7BCAF5-8A68-11d2-983C-0000F808342D")
    ICorDebugClass : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE GetModule(
            /* [out] */ ICorDebugModule **pModule) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetToken(
            /* [out] */ mdTypeDef *pTypeDef) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetStaticFieldValue(
            /* [in] */ mdFieldDef fieldDef,
            /* [in] */ ICorDebugFrame *pFrame,
            /* [out] */ ICorDebugValue **ppValue) = 0;

    };


#else 	/* C style interface */

    typedef struct ICorDebugClassVtbl
    {
        BEGIN_INTERFACE

        HRESULT ( STDMETHODCALLTYPE *QueryInterface )(
            ICorDebugClass * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */
            _COM_Outptr_  void **ppvObject);

        ULONG ( STDMETHODCALLTYPE *AddRef )(
            ICorDebugClass * This);

        ULONG ( STDMETHODCALLTYPE *Release )(
            ICorDebugClass * This);

        HRESULT ( STDMETHODCALLTYPE *GetModule )(
            ICorDebugClass * This,
            /* [out] */ ICorDebugModule **pModule);

        HRESULT ( STDMETHODCALLTYPE *GetToken )(
            ICorDebugClass * This,
            /* [out] */ mdTypeDef *pTypeDef);

        HRESULT ( STDMETHODCALLTYPE *GetStaticFieldValue )(
            ICorDebugClass * This,
            /* [in] */ mdFieldDef fieldDef,
            /* [in] */ ICorDebugFrame *pFrame,
            /* [out] */ ICorDebugValue **ppValue);

        END_INTERFACE
    } ICorDebugClassVtbl;

    interface ICorDebugClass
    {
        CONST_VTBL struct ICorDebugClassVtbl *lpVtbl;
    };



#ifdef COBJMACROS


#define ICorDebugClass_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) )

#define ICorDebugClass_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) )

#define ICorDebugClass_Release(This)	\
    ( (This)->lpVtbl -> Release(This) )


#define ICorDebugClass_GetModule(This,pModule)	\
    ( (This)->lpVtbl -> GetModule(This,pModule) )

#define ICorDebugClass_GetToken(This,pTypeDef)	\
    ( (This)->lpVtbl -> GetToken(This,pTypeDef) )

#define ICorDebugClass_GetStaticFieldValue(This,fieldDef,pFrame,ppValue)	\
    ( (This)->lpVtbl -> GetStaticFieldValue(This,fieldDef,pFrame,ppValue) )

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ICorDebugClass_INTERFACE_DEFINED__ */


#ifndef __ICorDebugClass2_INTERFACE_DEFINED__
#define __ICorDebugClass2_INTERFACE_DEFINED__

/* interface ICorDebugClass2 */
/* [unique][uuid][local][object] */


EXTERN_C const IID IID_ICorDebugClass2;

#if defined(__cplusplus) && !defined(CINTERFACE)

    MIDL_INTERFACE("B008EA8D-7AB1-43f7-BB20-FBB5A04038AE")
    ICorDebugClass2 : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE GetParameterizedType(
            /* [in] */ CorElementType elementType,
            /* [in] */ ULONG32 nTypeArgs,
            /* [size_is][in] */ ICorDebugType *ppTypeArgs[  ],
            /* [out] */ ICorDebugType **ppType) = 0;

        virtual HRESULT STDMETHODCALLTYPE SetJMCStatus(
            /* [in] */ BOOL bIsJustMyCode) = 0;

    };


#else 	/* C style interface */

    typedef struct ICorDebugClass2Vtbl
    {
        BEGIN_INTERFACE

        HRESULT ( STDMETHODCALLTYPE *QueryInterface )(
            ICorDebugClass2 * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */
            _COM_Outptr_  void **ppvObject);

        ULONG ( STDMETHODCALLTYPE *AddRef )(
            ICorDebugClass2 * This);

        ULONG ( STDMETHODCALLTYPE *Release )(
            ICorDebugClass2 * This);

        HRESULT ( STDMETHODCALLTYPE *GetParameterizedType )(
            ICorDebugClass2 * This,
            /* [in] */ CorElementType elementType,
            /* [in] */ ULONG32 nTypeArgs,
            /* [size_is][in] */ ICorDebugType *ppTypeArgs[  ],
            /* [out] */ ICorDebugType **ppType);

        HRESULT ( STDMETHODCALLTYPE *SetJMCStatus )(
            ICorDebugClass2 * This,
            /* [in] */ BOOL bIsJustMyCode);

        END_INTERFACE
    } ICorDebugClass2Vtbl;

    interface ICorDebugClass2
    {
        CONST_VTBL struct ICorDebugClass2Vtbl *lpVtbl;
    };



#ifdef COBJMACROS


#define ICorDebugClass2_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) )

#define ICorDebugClass2_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) )

#define ICorDebugClass2_Release(This)	\
    ( (This)->lpVtbl -> Release(This) )


#define ICorDebugClass2_GetParameterizedType(This,elementType,nTypeArgs,ppTypeArgs,ppType)	\
    ( (This)->lpVtbl -> GetParameterizedType(This,elementType,nTypeArgs,ppTypeArgs,ppType) )

#define ICorDebugClass2_SetJMCStatus(This,bIsJustMyCode)	\
    ( (This)->lpVtbl -> SetJMCStatus(This,bIsJustMyCode) )

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ICorDebugClass2_INTERFACE_DEFINED__ */


#ifndef __ICorDebugEval_INTERFACE_DEFINED__
#define __ICorDebugEval_INTERFACE_DEFINED__

/* interface ICorDebugEval */
/* [unique][uuid][local][object] */


EXTERN_C const IID IID_ICorDebugEval;

#if defined(__cplusplus) && !defined(CINTERFACE)

    MIDL_INTERFACE("CC7BCAF6-8A68-11d2-983C-0000F808342D")
    ICorDebugEval : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE CallFunction(
            /* [in] */ ICorDebugFunction *pFunction,
            /* [in] */ ULONG32 nArgs,
            /* [size_is][in] */ ICorDebugValue *ppArgs[  ]) = 0;

        virtual HRESULT STDMETHODCALLTYPE NewObject(
            /* [in] */ ICorDebugFunction *pConstructor,
            /* [in] */ ULONG32 nArgs,
            /* [size_is][in] */ ICorDebugValue *ppArgs[  ]) = 0;

        virtual HRESULT STDMETHODCALLTYPE NewObjectNoConstructor(
            /* [in] */ ICorDebugClass *pClass) = 0;

        virtual HRESULT STDMETHODCALLTYPE NewString(
            /* [in] */ LPCWSTR string) = 0;

        virtual HRESULT STDMETHODCALLTYPE NewArray(
            /* [in] */ CorElementType elementType,
            /* [in] */ ICorDebugClass *pElementClass,
            /* [in] */ ULONG32 rank,
            /* [size_is][in] */ ULONG32 dims[  ],
            /* [size_is][in] */ ULONG32 lowBounds[  ]) = 0;

        virtual HRESULT STDMETHODCALLTYPE IsActive(
            /* [out] */ BOOL *pbActive) = 0;

        virtual HRESULT STDMETHODCALLTYPE Abort( void) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetResult(
            /* [out] */ ICorDebugValue **ppResult) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetThread(
            /* [out] */ ICorDebugThread **ppThread) = 0;

        virtual HRESULT STDMETHODCALLTYPE CreateValue(
            /* [in] */ CorElementType elementType,
            /* [in] */ ICorDebugClass *pElementClass,
            /* [out] */ ICorDebugValue **ppValue) = 0;

    };


#else 	/* C style interface */

    typedef struct ICorDebugEvalVtbl
    {
        BEGIN_INTERFACE

        HRESULT ( STDMETHODCALLTYPE *QueryInterface )(
            ICorDebugEval * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */
            _COM_Outptr_  void **ppvObject);

        ULONG ( STDMETHODCALLTYPE *AddRef )(
            ICorDebugEval * This);

        ULONG ( STDMETHODCALLTYPE *Release )(
            ICorDebugEval * This);

        HRESULT ( STDMETHODCALLTYPE *CallFunction )(
            ICorDebugEval * This,
            /* [in] */ ICorDebugFunction *pFunction,
            /* [in] */ ULONG32 nArgs,
            /* [size_is][in] */ ICorDebugValue *ppArgs[  ]);

        HRESULT ( STDMETHODCALLTYPE *NewObject )(
            ICorDebugEval * This,
            /* [in] */ ICorDebugFunction *pConstructor,
            /* [in] */ ULONG32 nArgs,
            /* [size_is][in] */ ICorDebugValue *ppArgs[  ]);

        HRESULT ( STDMETHODCALLTYPE *NewObjectNoConstructor )(
            ICorDebugEval * This,
            /* [in] */ ICorDebugClass *pClass);

        HRESULT ( STDMETHODCALLTYPE *NewString )(
            ICorDebugEval * This,
            /* [in] */ LPCWSTR string);

        HRESULT ( STDMETHODCALLTYPE *NewArray )(
            ICorDebugEval * This,
            /* [in] */ CorElementType elementType,
            /* [in] */ ICorDebugClass *pElementClass,
            /* [in] */ ULONG32 rank,
            /* [size_is][in] */ ULONG32 dims[  ],
            /* [size_is][in] */ ULONG32 lowBounds[  ]);

        HRESULT ( STDMETHODCALLTYPE *IsActive )(
            ICorDebugEval * This,
            /* [out] */ BOOL *pbActive);

        HRESULT ( STDMETHODCALLTYPE *Abort )(
            ICorDebugEval * This);

        HRESULT ( STDMETHODCALLTYPE *GetResult )(
            ICorDebugEval * This,
            /* [out] */ ICorDebugValue **ppResult);

        HRESULT ( STDMETHODCALLTYPE *GetThread )(
            ICorDebugEval * This,
            /* [out] */ ICorDebugThread **ppThread);

        HRESULT ( STDMETHODCALLTYPE *CreateValue )(
            ICorDebugEval * This,
            /* [in] */ CorElementType elementType,
            /* [in] */ ICorDebugClass *pElementClass,
            /* [out] */ ICorDebugValue **ppValue);

        END_INTERFACE
    } ICorDebugEvalVtbl;

    interface ICorDebugEval
    {
        CONST_VTBL struct ICorDebugEvalVtbl *lpVtbl;
    };



#ifdef COBJMACROS


#define ICorDebugEval_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) )

#define ICorDebugEval_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) )

#define ICorDebugEval_Release(This)	\
    ( (This)->lpVtbl -> Release(This) )


#define ICorDebugEval_CallFunction(This,pFunction,nArgs,ppArgs)	\
    ( (This)->lpVtbl -> CallFunction(This,pFunction,nArgs,ppArgs) )

#define ICorDebugEval_NewObject(This,pConstructor,nArgs,ppArgs)	\
    ( (This)->lpVtbl -> NewObject(This,pConstructor,nArgs,ppArgs) )

#define ICorDebugEval_NewObjectNoConstructor(This,pClass)	\
    ( (This)->lpVtbl -> NewObjectNoConstructor(This,pClass) )

#define ICorDebugEval_NewString(This,string)	\
    ( (This)->lpVtbl -> NewString(This,string) )

#define ICorDebugEval_NewArray(This,elementType,pElementClass,rank,dims,lowBounds)	\
    ( (This)->lpVtbl -> NewArray(This,elementType,pElementClass,rank,dims,lowBounds) )

#define ICorDebugEval_IsActive(This,pbActive)	\
    ( (This)->lpVtbl -> IsActive(This,pbActive) )

#define ICorDebugEval_Abort(This)	\
    ( (This)->lpVtbl -> Abort(This) )

#define ICorDebugEval_GetResult(This,ppResult)	\
    ( (This)->lpVtbl -> GetResult(This,ppResult) )

#define ICorDebugEval_GetThread(This,ppThread)	\
    ( (This)->lpVtbl -> GetThread(This,ppThread) )

#define ICorDebugEval_CreateValue(This,elementType,pElementClass,ppValue)	\
    ( (This)->lpVtbl -> CreateValue(This,elementType,pElementClass,ppValue) )

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ICorDebugEval_INTERFACE_DEFINED__ */


#ifndef __ICorDebugEval2_INTERFACE_DEFINED__
#define __ICorDebugEval2_INTERFACE_DEFINED__

/* interface ICorDebugEval2 */
/* [unique][uuid][local][object] */


EXTERN_C const IID IID_ICorDebugEval2;

#if defined(__cplusplus) && !defined(CINTERFACE)

    MIDL_INTERFACE("FB0D9CE7-BE66-4683-9D32-A42A04E2FD91")
    ICorDebugEval2 : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE CallParameterizedFunction(
            /* [in] */ ICorDebugFunction *pFunction,
            /* [in] */ ULONG32 nTypeArgs,
            /* [size_is][in] */ ICorDebugType *ppTypeArgs[  ],
            /* [in] */ ULONG32 nArgs,
            /* [size_is][in] */ ICorDebugValue *ppArgs[  ]) = 0;

        virtual HRESULT STDMETHODCALLTYPE CreateValueForType(
            /* [in] */ ICorDebugType *pType,
            /* [out] */ ICorDebugValue **ppValue) = 0;

        virtual HRESULT STDMETHODCALLTYPE NewParameterizedObject(
            /* [in] */ ICorDebugFunction *pConstructor,
            /* [in] */ ULONG32 nTypeArgs,
            /* [size_is][in] */ ICorDebugType *ppTypeArgs[  ],
            /* [in] */ ULONG32 nArgs,
            /* [size_is][in] */ ICorDebugValue *ppArgs[  ]) = 0;

        virtual HRESULT STDMETHODCALLTYPE NewParameterizedObjectNoConstructor(
            /* [in] */ ICorDebugClass *pClass,
            /* [in] */ ULONG32 nTypeArgs,
            /* [size_is][in] */ ICorDebugType *ppTypeArgs[  ]) = 0;

        virtual HRESULT STDMETHODCALLTYPE NewParameterizedArray(
            /* [in] */ ICorDebugType *pElementType,
            /* [in] */ ULONG32 rank,
            /* [size_is][in] */ ULONG32 dims[  ],
            /* [size_is][in] */ ULONG32 lowBounds[  ]) = 0;

        virtual HRESULT STDMETHODCALLTYPE NewStringWithLength(
            /* [in] */ LPCWSTR string,
            /* [in] */ UINT uiLength) = 0;

        virtual HRESULT STDMETHODCALLTYPE RudeAbort( void) = 0;

    };


#else 	/* C style interface */

    typedef struct ICorDebugEval2Vtbl
    {
        BEGIN_INTERFACE

        HRESULT ( STDMETHODCALLTYPE *QueryInterface )(
            ICorDebugEval2 * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */
            _COM_Outptr_  void **ppvObject);

        ULONG ( STDMETHODCALLTYPE *AddRef )(
            ICorDebugEval2 * This);

        ULONG ( STDMETHODCALLTYPE *Release )(
            ICorDebugEval2 * This);

        HRESULT ( STDMETHODCALLTYPE *CallParameterizedFunction )(
            ICorDebugEval2 * This,
            /* [in] */ ICorDebugFunction *pFunction,
            /* [in] */ ULONG32 nTypeArgs,
            /* [size_is][in] */ ICorDebugType *ppTypeArgs[  ],
            /* [in] */ ULONG32 nArgs,
            /* [size_is][in] */ ICorDebugValue *ppArgs[  ]);

        HRESULT ( STDMETHODCALLTYPE *CreateValueForType )(
            ICorDebugEval2 * This,
            /* [in] */ ICorDebugType *pType,
            /* [out] */ ICorDebugValue **ppValue);

        HRESULT ( STDMETHODCALLTYPE *NewParameterizedObject )(
            ICorDebugEval2 * This,
            /* [in] */ ICorDebugFunction *pConstructor,
            /* [in] */ ULONG32 nTypeArgs,
            /* [size_is][in] */ ICorDebugType *ppTypeArgs[  ],
            /* [in] */ ULONG32 nArgs,
            /* [size_is][in] */ ICorDebugValue *ppArgs[  ]);

        HRESULT ( STDMETHODCALLTYPE *NewParameterizedObjectNoConstructor )(
            ICorDebugEval2 * This,
            /* [in] */ ICorDebugClass *pClass,
            /* [in] */ ULONG32 nTypeArgs,
            /* [size_is][in] */ ICorDebugType *ppTypeArgs[  ]);

        HRESULT ( STDMETHODCALLTYPE *NewParameterizedArray )(
            ICorDebugEval2 * This,
            /* [in] */ ICorDebugType *pElementType,
            /* [in] */ ULONG32 rank,
            /* [size_is][in] */ ULONG32 dims[  ],
            /* [size_is][in] */ ULONG32 lowBounds[  ]);

        HRESULT ( STDMETHODCALLTYPE *NewStringWithLength )(
            ICorDebugEval2 * This,
            /* [in] */ LPCWSTR string,
            /* [in] */ UINT uiLength);

        HRESULT ( STDMETHODCALLTYPE *RudeAbort )(
            ICorDebugEval2 * This);

        END_INTERFACE
    } ICorDebugEval2Vtbl;

    interface ICorDebugEval2
    {
        CONST_VTBL struct ICorDebugEval2Vtbl *lpVtbl;
    };



#ifdef COBJMACROS


#define ICorDebugEval2_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) )

#define ICorDebugEval2_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) )

#define ICorDebugEval2_Release(This)	\
    ( (This)->lpVtbl -> Release(This) )


#define ICorDebugEval2_CallParameterizedFunction(This,pFunction,nTypeArgs,ppTypeArgs,nArgs,ppArgs)	\
    ( (This)->lpVtbl -> CallParameterizedFunction(This,pFunction,nTypeArgs,ppTypeArgs,nArgs,ppArgs) )

#define ICorDebugEval2_CreateValueForType(This,pType,ppValue)	\
    ( (This)->lpVtbl -> CreateValueForType(This,pType,ppValue) )

#define ICorDebugEval2_NewParameterizedObject(This,pConstructor,nTypeArgs,ppTypeArgs,nArgs,ppArgs)	\
    ( (This)->lpVtbl -> NewParameterizedObject(This,pConstructor,nTypeArgs,ppTypeArgs,nArgs,ppArgs) )

#define ICorDebugEval2_NewParameterizedObjectNoConstructor(This,pClass,nTypeArgs,ppTypeArgs)	\
    ( (This)->lpVtbl -> NewParameterizedObjectNoConstructor(This,pClass,nTypeArgs,ppTypeArgs) )

#define ICorDebugEval2_NewParameterizedArray(This,pElementType,rank,dims,lowBounds)	\
    ( (This)->lpVtbl -> NewParameterizedArray(This,pElementType,rank,dims,lowBounds) )

#define ICorDebugEval2_NewStringWithLength(This,string,uiLength)	\
    ( (This)->lpVtbl -> NewStringWithLength(This,string,uiLength) )

#define ICorDebugEval2_RudeAbort(This)	\
    ( (This)->lpVtbl -> RudeAbort(This) )

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ICorDebugEval2_INTERFACE_DEFINED__ */


#ifndef __ICorDebugValue_INTERFACE_DEFINED__
#define __ICorDebugValue_INTERFACE_DEFINED__

/* interface ICorDebugValue */
/* [unique][uuid][local][object] */


EXTERN_C const IID IID_ICorDebugValue;

#if defined(__cplusplus) && !defined(CINTERFACE)

    MIDL_INTERFACE("CC7BCAF7-8A68-11d2-983C-0000F808342D")
    ICorDebugValue : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE GetType(
            /* [out] */ CorElementType *pType) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetSize(
            /* [out] */ ULONG32 *pSize) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetAddress(
            /* [out] */ CORDB_ADDRESS *pAddress) = 0;

        virtual HRESULT STDMETHODCALLTYPE CreateBreakpoint(
            /* [out] */ ICorDebugValueBreakpoint **ppBreakpoint) = 0;

    };


#else 	/* C style interface */

    typedef struct ICorDebugValueVtbl
    {
        BEGIN_INTERFACE

        HRESULT ( STDMETHODCALLTYPE *QueryInterface )(
            ICorDebugValue * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */
            _COM_Outptr_  void **ppvObject);

        ULONG ( STDMETHODCALLTYPE *AddRef )(
            ICorDebugValue * This);

        ULONG ( STDMETHODCALLTYPE *Release )(
            ICorDebugValue * This);

        HRESULT ( STDMETHODCALLTYPE *GetType )(
            ICorDebugValue * This,
            /* [out] */ CorElementType *pType);

        HRESULT ( STDMETHODCALLTYPE *GetSize )(
            ICorDebugValue * This,
            /* [out] */ ULONG32 *pSize);

        HRESULT ( STDMETHODCALLTYPE *GetAddress )(
            ICorDebugValue * This,
            /* [out] */ CORDB_ADDRESS *pAddress);

        HRESULT ( STDMETHODCALLTYPE *CreateBreakpoint )(
            ICorDebugValue * This,
            /* [out] */ ICorDebugValueBreakpoint **ppBreakpoint);

        END_INTERFACE
    } ICorDebugValueVtbl;

    interface ICorDebugValue
    {
        CONST_VTBL struct ICorDebugValueVtbl *lpVtbl;
    };



#ifdef COBJMACROS


#define ICorDebugValue_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) )

#define ICorDebugValue_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) )

#define ICorDebugValue_Release(This)	\
    ( (This)->lpVtbl -> Release(This) )


#define ICorDebugValue_GetType(This,pType)	\
    ( (This)->lpVtbl -> GetType(This,pType) )

#define ICorDebugValue_GetSize(This,pSize)	\
    ( (This)->lpVtbl -> GetSize(This,pSize) )

#define ICorDebugValue_GetAddress(This,pAddress)	\
    ( (This)->lpVtbl -> GetAddress(This,pAddress) )

#define ICorDebugValue_CreateBreakpoint(This,ppBreakpoint)	\
    ( (This)->lpVtbl -> CreateBreakpoint(This,ppBreakpoint) )

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ICorDebugValue_INTERFACE_DEFINED__ */


#ifndef __ICorDebugValue2_INTERFACE_DEFINED__
#define __ICorDebugValue2_INTERFACE_DEFINED__

/* interface ICorDebugValue2 */
/* [unique][uuid][local][object] */


EXTERN_C const IID IID_ICorDebugValue2;

#if defined(__cplusplus) && !defined(CINTERFACE)

    MIDL_INTERFACE("5E0B54E7-D88A-4626-9420-A691E0A78B49")
    ICorDebugValue2 : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE GetExactType(
            /* [out] */ ICorDebugType **ppType) = 0;

    };


#else 	/* C style interface */

    typedef struct ICorDebugValue2Vtbl
    {
        BEGIN_INTERFACE

        HRESULT ( STDMETHODCALLTYPE *QueryInterface )(
            ICorDebugValue2 * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */
            _COM_Outptr_  void **ppvObject);

        ULONG ( STDMETHODCALLTYPE *AddRef )(
            ICorDebugValue2 * This);

        ULONG ( STDMETHODCALLTYPE *Release )(
            ICorDebugValue2 * This);

        HRESULT ( STDMETHODCALLTYPE *GetExactType )(
            ICorDebugValue2 * This,
            /* [out] */ ICorDebugType **ppType);

        END_INTERFACE
    } ICorDebugValue2Vtbl;

    interface ICorDebugValue2
    {
        CONST_VTBL struct ICorDebugValue2Vtbl *lpVtbl;
    };



#ifdef COBJMACROS


#define ICorDebugValue2_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) )

#define ICorDebugValue2_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) )

#define ICorDebugValue2_Release(This)	\
    ( (This)->lpVtbl -> Release(This) )


#define ICorDebugValue2_GetExactType(This,ppType)	\
    ( (This)->lpVtbl -> GetExactType(This,ppType) )

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ICorDebugValue2_INTERFACE_DEFINED__ */


#ifndef __ICorDebugValue3_INTERFACE_DEFINED__
#define __ICorDebugValue3_INTERFACE_DEFINED__

/* interface ICorDebugValue3 */
/* [unique][uuid][local][object] */


EXTERN_C const IID IID_ICorDebugValue3;

#if defined(__cplusplus) && !defined(CINTERFACE)

    MIDL_INTERFACE("565005FC-0F8A-4F3E-9EDB-83102B156595")
    ICorDebugValue3 : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE GetSize64(
            /* [out] */ ULONG64 *pSize) = 0;

    };


#else 	/* C style interface */

    typedef struct ICorDebugValue3Vtbl
    {
        BEGIN_INTERFACE

        HRESULT ( STDMETHODCALLTYPE *QueryInterface )(
            ICorDebugValue3 * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */
            _COM_Outptr_  void **ppvObject);

        ULONG ( STDMETHODCALLTYPE *AddRef )(
            ICorDebugValue3 * This);

        ULONG ( STDMETHODCALLTYPE *Release )(
            ICorDebugValue3 * This);

        HRESULT ( STDMETHODCALLTYPE *GetSize64 )(
            ICorDebugValue3 * This,
            /* [out] */ ULONG64 *pSize);

        END_INTERFACE
    } ICorDebugValue3Vtbl;

    interface ICorDebugValue3
    {
        CONST_VTBL struct ICorDebugValue3Vtbl *lpVtbl;
    };



#ifdef COBJMACROS


#define ICorDebugValue3_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) )

#define ICorDebugValue3_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) )

#define ICorDebugValue3_Release(This)	\
    ( (This)->lpVtbl -> Release(This) )


#define ICorDebugValue3_GetSize64(This,pSize)	\
    ( (This)->lpVtbl -> GetSize64(This,pSize) )

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ICorDebugValue3_INTERFACE_DEFINED__ */


#ifndef __ICorDebugGenericValue_INTERFACE_DEFINED__
#define __ICorDebugGenericValue_INTERFACE_DEFINED__

/* interface ICorDebugGenericValue */
/* [unique][uuid][local][object] */


EXTERN_C const IID IID_ICorDebugGenericValue;

#if defined(__cplusplus) && !defined(CINTERFACE)

    MIDL_INTERFACE("CC7BCAF8-8A68-11d2-983C-0000F808342D")
    ICorDebugGenericValue : public ICorDebugValue
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE GetValue(
            /* [out] */ void *pTo) = 0;

        virtual HRESULT STDMETHODCALLTYPE SetValue(
            /* [in] */ void *pFrom) = 0;

    };


#else 	/* C style interface */

    typedef struct ICorDebugGenericValueVtbl
    {
        BEGIN_INTERFACE

        HRESULT ( STDMETHODCALLTYPE *QueryInterface )(
            ICorDebugGenericValue * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */
            _COM_Outptr_  void **ppvObject);

        ULONG ( STDMETHODCALLTYPE *AddRef )(
            ICorDebugGenericValue * This);

        ULONG ( STDMETHODCALLTYPE *Release )(
            ICorDebugGenericValue * This);

        HRESULT ( STDMETHODCALLTYPE *GetType )(
            ICorDebugGenericValue * This,
            /* [out] */ CorElementType *pType);

        HRESULT ( STDMETHODCALLTYPE *GetSize )(
            ICorDebugGenericValue * This,
            /* [out] */ ULONG32 *pSize);

        HRESULT ( STDMETHODCALLTYPE *GetAddress )(
            ICorDebugGenericValue * This,
            /* [out] */ CORDB_ADDRESS *pAddress);

        HRESULT ( STDMETHODCALLTYPE *CreateBreakpoint )(
            ICorDebugGenericValue * This,
            /* [out] */ ICorDebugValueBreakpoint **ppBreakpoint);

        HRESULT ( STDMETHODCALLTYPE *GetValue )(
            ICorDebugGenericValue * This,
            /* [out] */ void *pTo);

        HRESULT ( STDMETHODCALLTYPE *SetValue )(
            ICorDebugGenericValue * This,
            /* [in] */ void *pFrom);

        END_INTERFACE
    } ICorDebugGenericValueVtbl;

    interface ICorDebugGenericValue
    {
        CONST_VTBL struct ICorDebugGenericValueVtbl *lpVtbl;
    };



#ifdef COBJMACROS


#define ICorDebugGenericValue_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) )

#define ICorDebugGenericValue_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) )

#define ICorDebugGenericValue_Release(This)	\
    ( (This)->lpVtbl -> Release(This) )


#define ICorDebugGenericValue_GetType(This,pType)	\
    ( (This)->lpVtbl -> GetType(This,pType) )

#define ICorDebugGenericValue_GetSize(This,pSize)	\
    ( (This)->lpVtbl -> GetSize(This,pSize) )

#define ICorDebugGenericValue_GetAddress(This,pAddress)	\
    ( (This)->lpVtbl -> GetAddress(This,pAddress) )

#define ICorDebugGenericValue_CreateBreakpoint(This,ppBreakpoint)	\
    ( (This)->lpVtbl -> CreateBreakpoint(This,ppBreakpoint) )


#define ICorDebugGenericValue_GetValue(This,pTo)	\
    ( (This)->lpVtbl -> GetValue(This,pTo) )

#define ICorDebugGenericValue_SetValue(This,pFrom)	\
    ( (This)->lpVtbl -> SetValue(This,pFrom) )

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ICorDebugGenericValue_INTERFACE_DEFINED__ */


#ifndef __ICorDebugReferenceValue_INTERFACE_DEFINED__
#define __ICorDebugReferenceValue_INTERFACE_DEFINED__

/* interface ICorDebugReferenceValue */
/* [unique][uuid][local][object] */


EXTERN_C const IID IID_ICorDebugReferenceValue;

#if defined(__cplusplus) && !defined(CINTERFACE)

    MIDL_INTERFACE("CC7BCAF9-8A68-11d2-983C-0000F808342D")
    ICorDebugReferenceValue : public ICorDebugValue
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE IsNull(
            /* [out] */ BOOL *pbNull) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetValue(
            /* [out] */ CORDB_ADDRESS *pValue) = 0;

        virtual HRESULT STDMETHODCALLTYPE SetValue(
            /* [in] */ CORDB_ADDRESS value) = 0;

        virtual HRESULT STDMETHODCALLTYPE Dereference(
            /* [out] */ ICorDebugValue **ppValue) = 0;

        virtual HRESULT STDMETHODCALLTYPE DereferenceStrong(
            /* [out] */ ICorDebugValue **ppValue) = 0;

    };


#else 	/* C style interface */

    typedef struct ICorDebugReferenceValueVtbl
    {
        BEGIN_INTERFACE

        HRESULT ( STDMETHODCALLTYPE *QueryInterface )(
            ICorDebugReferenceValue * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */
            _COM_Outptr_  void **ppvObject);

        ULONG ( STDMETHODCALLTYPE *AddRef )(
            ICorDebugReferenceValue * This);

        ULONG ( STDMETHODCALLTYPE *Release )(
            ICorDebugReferenceValue * This);

        HRESULT ( STDMETHODCALLTYPE *GetType )(
            ICorDebugReferenceValue * This,
            /* [out] */ CorElementType *pType);

        HRESULT ( STDMETHODCALLTYPE *GetSize )(
            ICorDebugReferenceValue * This,
            /* [out] */ ULONG32 *pSize);

        HRESULT ( STDMETHODCALLTYPE *GetAddress )(
            ICorDebugReferenceValue * This,
            /* [out] */ CORDB_ADDRESS *pAddress);

        HRESULT ( STDMETHODCALLTYPE *CreateBreakpoint )(
            ICorDebugReferenceValue * This,
            /* [out] */ ICorDebugValueBreakpoint **ppBreakpoint);

        HRESULT ( STDMETHODCALLTYPE *IsNull )(
            ICorDebugReferenceValue * This,
            /* [out] */ BOOL *pbNull);

        HRESULT ( STDMETHODCALLTYPE *GetValue )(
            ICorDebugReferenceValue * This,
            /* [out] */ CORDB_ADDRESS *pValue);

        HRESULT ( STDMETHODCALLTYPE *SetValue )(
            ICorDebugReferenceValue * This,
            /* [in] */ CORDB_ADDRESS value);

        HRESULT ( STDMETHODCALLTYPE *Dereference )(
            ICorDebugReferenceValue * This,
            /* [out] */ ICorDebugValue **ppValue);

        HRESULT ( STDMETHODCALLTYPE *DereferenceStrong )(
            ICorDebugReferenceValue * This,
            /* [out] */ ICorDebugValue **ppValue);

        END_INTERFACE
    } ICorDebugReferenceValueVtbl;

    interface ICorDebugReferenceValue
    {
        CONST_VTBL struct ICorDebugReferenceValueVtbl *lpVtbl;
    };



#ifdef COBJMACROS


#define ICorDebugReferenceValue_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) )

#define ICorDebugReferenceValue_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) )

#define ICorDebugReferenceValue_Release(This)	\
    ( (This)->lpVtbl -> Release(This) )


#define ICorDebugReferenceValue_GetType(This,pType)	\
    ( (This)->lpVtbl -> GetType(This,pType) )

#define ICorDebugReferenceValue_GetSize(This,pSize)	\
    ( (This)->lpVtbl -> GetSize(This,pSize) )

#define ICorDebugReferenceValue_GetAddress(This,pAddress)	\
    ( (This)->lpVtbl -> GetAddress(This,pAddress) )

#define ICorDebugReferenceValue_CreateBreakpoint(This,ppBreakpoint)	\
    ( (This)->lpVtbl -> CreateBreakpoint(This,ppBreakpoint) )


#define ICorDebugReferenceValue_IsNull(This,pbNull)	\
    ( (This)->lpVtbl -> IsNull(This,pbNull) )

#define ICorDebugReferenceValue_GetValue(This,pValue)	\
    ( (This)->lpVtbl -> GetValue(This,pValue) )

#define ICorDebugReferenceValue_SetValue(This,value)	\
    ( (This)->lpVtbl -> SetValue(This,value) )

#define ICorDebugReferenceValue_Dereference(This,ppValue)	\
    ( (This)->lpVtbl -> Dereference(This,ppValue) )

#define ICorDebugReferenceValue_DereferenceStrong(This,ppValue)	\
    ( (This)->lpVtbl -> DereferenceStrong(This,ppValue) )

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ICorDebugReferenceValue_INTERFACE_DEFINED__ */


#ifndef __ICorDebugHeapValue_INTERFACE_DEFINED__
#define __ICorDebugHeapValue_INTERFACE_DEFINED__

/* interface ICorDebugHeapValue */
/* [unique][uuid][local][object] */


EXTERN_C const IID IID_ICorDebugHeapValue;

#if defined(__cplusplus) && !defined(CINTERFACE)

    MIDL_INTERFACE("CC7BCAFA-8A68-11d2-983C-0000F808342D")
    ICorDebugHeapValue : public ICorDebugValue
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE IsValid(
            /* [out] */ BOOL *pbValid) = 0;

        virtual HRESULT STDMETHODCALLTYPE CreateRelocBreakpoint(
            /* [out] */ ICorDebugValueBreakpoint **ppBreakpoint) = 0;

    };


#else 	/* C style interface */

    typedef struct ICorDebugHeapValueVtbl
    {
        BEGIN_INTERFACE

        HRESULT ( STDMETHODCALLTYPE *QueryInterface )(
            ICorDebugHeapValue * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */
            _COM_Outptr_  void **ppvObject);

        ULONG ( STDMETHODCALLTYPE *AddRef )(
            ICorDebugHeapValue * This);

        ULONG ( STDMETHODCALLTYPE *Release )(
            ICorDebugHeapValue * This);

        HRESULT ( STDMETHODCALLTYPE *GetType )(
            ICorDebugHeapValue * This,
            /* [out] */ CorElementType *pType);

        HRESULT ( STDMETHODCALLTYPE *GetSize )(
            ICorDebugHeapValue * This,
            /* [out] */ ULONG32 *pSize);

        HRESULT ( STDMETHODCALLTYPE *GetAddress )(
            ICorDebugHeapValue * This,
            /* [out] */ CORDB_ADDRESS *pAddress);

        HRESULT ( STDMETHODCALLTYPE *CreateBreakpoint )(
            ICorDebugHeapValue * This,
            /* [out] */ ICorDebugValueBreakpoint **ppBreakpoint);

        HRESULT ( STDMETHODCALLTYPE *IsValid )(
            ICorDebugHeapValue * This,
            /* [out] */ BOOL *pbValid);

        HRESULT ( STDMETHODCALLTYPE *CreateRelocBreakpoint )(
            ICorDebugHeapValue * This,
            /* [out] */ ICorDebugValueBreakpoint **ppBreakpoint);

        END_INTERFACE
    } ICorDebugHeapValueVtbl;

    interface ICorDebugHeapValue
    {
        CONST_VTBL struct ICorDebugHeapValueVtbl *lpVtbl;
    };



#ifdef COBJMACROS


#define ICorDebugHeapValue_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) )

#define ICorDebugHeapValue_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) )

#define ICorDebugHeapValue_Release(This)	\
    ( (This)->lpVtbl -> Release(This) )


#define ICorDebugHeapValue_GetType(This,pType)	\
    ( (This)->lpVtbl -> GetType(This,pType) )

#define ICorDebugHeapValue_GetSize(This,pSize)	\
    ( (This)->lpVtbl -> GetSize(This,pSize) )

#define ICorDebugHeapValue_GetAddress(This,pAddress)	\
    ( (This)->lpVtbl -> GetAddress(This,pAddress) )

#define ICorDebugHeapValue_CreateBreakpoint(This,ppBreakpoint)	\
    ( (This)->lpVtbl -> CreateBreakpoint(This,ppBreakpoint) )


#define ICorDebugHeapValue_IsValid(This,pbValid)	\
    ( (This)->lpVtbl -> IsValid(This,pbValid) )

#define ICorDebugHeapValue_CreateRelocBreakpoint(This,ppBreakpoint)	\
    ( (This)->lpVtbl -> CreateRelocBreakpoint(This,ppBreakpoint) )

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ICorDebugHeapValue_INTERFACE_DEFINED__ */


#ifndef __ICorDebugHeapValue2_INTERFACE_DEFINED__
#define __ICorDebugHeapValue2_INTERFACE_DEFINED__

/* interface ICorDebugHeapValue2 */
/* [unique][uuid][local][object] */


EXTERN_C const IID IID_ICorDebugHeapValue2;

#if defined(__cplusplus) && !defined(CINTERFACE)

    MIDL_INTERFACE("E3AC4D6C-9CB7-43e6-96CC-B21540E5083C")
    ICorDebugHeapValue2 : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE CreateHandle(
            /* [in] */ CorDebugHandleType type,
            /* [out] */ ICorDebugHandleValue **ppHandle) = 0;

    };


#else 	/* C style interface */

    typedef struct ICorDebugHeapValue2Vtbl
    {
        BEGIN_INTERFACE

        HRESULT ( STDMETHODCALLTYPE *QueryInterface )(
            ICorDebugHeapValue2 * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */
            _COM_Outptr_  void **ppvObject);

        ULONG ( STDMETHODCALLTYPE *AddRef )(
            ICorDebugHeapValue2 * This);

        ULONG ( STDMETHODCALLTYPE *Release )(
            ICorDebugHeapValue2 * This);

        HRESULT ( STDMETHODCALLTYPE *CreateHandle )(
            ICorDebugHeapValue2 * This,
            /* [in] */ CorDebugHandleType type,
            /* [out] */ ICorDebugHandleValue **ppHandle);

        END_INTERFACE
    } ICorDebugHeapValue2Vtbl;

    interface ICorDebugHeapValue2
    {
        CONST_VTBL struct ICorDebugHeapValue2Vtbl *lpVtbl;
    };



#ifdef COBJMACROS


#define ICorDebugHeapValue2_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) )

#define ICorDebugHeapValue2_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) )

#define ICorDebugHeapValue2_Release(This)	\
    ( (This)->lpVtbl -> Release(This) )


#define ICorDebugHeapValue2_CreateHandle(This,type,ppHandle)	\
    ( (This)->lpVtbl -> CreateHandle(This,type,ppHandle) )

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ICorDebugHeapValue2_INTERFACE_DEFINED__ */


#ifndef __ICorDebugHeapValue3_INTERFACE_DEFINED__
#define __ICorDebugHeapValue3_INTERFACE_DEFINED__

/* interface ICorDebugHeapValue3 */
/* [unique][uuid][local][object] */


EXTERN_C const IID IID_ICorDebugHeapValue3;

#if defined(__cplusplus) && !defined(CINTERFACE)

    MIDL_INTERFACE("A69ACAD8-2374-46e9-9FF8-B1F14120D296")
    ICorDebugHeapValue3 : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE GetThreadOwningMonitorLock(
            /* [out] */ ICorDebugThread **ppThread,
            /* [out] */ DWORD *pAcquisitionCount) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetMonitorEventWaitList(
            /* [out] */ ICorDebugThreadEnum **ppThreadEnum) = 0;

    };


#else 	/* C style interface */

    typedef struct ICorDebugHeapValue3Vtbl
    {
        BEGIN_INTERFACE

        HRESULT ( STDMETHODCALLTYPE *QueryInterface )(
            ICorDebugHeapValue3 * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */
            _COM_Outptr_  void **ppvObject);

        ULONG ( STDMETHODCALLTYPE *AddRef )(
            ICorDebugHeapValue3 * This);

        ULONG ( STDMETHODCALLTYPE *Release )(
            ICorDebugHeapValue3 * This);

        HRESULT ( STDMETHODCALLTYPE *GetThreadOwningMonitorLock )(
            ICorDebugHeapValue3 * This,
            /* [out] */ ICorDebugThread **ppThread,
            /* [out] */ DWORD *pAcquisitionCount);

        HRESULT ( STDMETHODCALLTYPE *GetMonitorEventWaitList )(
            ICorDebugHeapValue3 * This,
            /* [out] */ ICorDebugThreadEnum **ppThreadEnum);

        END_INTERFACE
    } ICorDebugHeapValue3Vtbl;

    interface ICorDebugHeapValue3
    {
        CONST_VTBL struct ICorDebugHeapValue3Vtbl *lpVtbl;
    };



#ifdef COBJMACROS


#define ICorDebugHeapValue3_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) )

#define ICorDebugHeapValue3_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) )

#define ICorDebugHeapValue3_Release(This)	\
    ( (This)->lpVtbl -> Release(This) )


#define ICorDebugHeapValue3_GetThreadOwningMonitorLock(This,ppThread,pAcquisitionCount)	\
    ( (This)->lpVtbl -> GetThreadOwningMonitorLock(This,ppThread,pAcquisitionCount) )

#define ICorDebugHeapValue3_GetMonitorEventWaitList(This,ppThreadEnum)	\
    ( (This)->lpVtbl -> GetMonitorEventWaitList(This,ppThreadEnum) )

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ICorDebugHeapValue3_INTERFACE_DEFINED__ */


#ifndef __ICorDebugObjectValue_INTERFACE_DEFINED__
#define __ICorDebugObjectValue_INTERFACE_DEFINED__

/* interface ICorDebugObjectValue */
/* [unique][uuid][local][object] */


EXTERN_C const IID IID_ICorDebugObjectValue;

#if defined(__cplusplus) && !defined(CINTERFACE)

    MIDL_INTERFACE("18AD3D6E-B7D2-11d2-BD04-0000F80849BD")
    ICorDebugObjectValue : public ICorDebugValue
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE GetClass(
            /* [out] */ ICorDebugClass **ppClass) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetFieldValue(
            /* [in] */ ICorDebugClass *pClass,
            /* [in] */ mdFieldDef fieldDef,
            /* [out] */ ICorDebugValue **ppValue) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetVirtualMethod(
            /* [in] */ mdMemberRef memberRef,
            /* [out] */ ICorDebugFunction **ppFunction) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetContext(
            /* [out] */ ICorDebugContext **ppContext) = 0;

        virtual HRESULT STDMETHODCALLTYPE IsValueClass(
            /* [out] */ BOOL *pbIsValueClass) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetManagedCopy(
            /* [out] */ IUnknown **ppObject) = 0;

        virtual HRESULT STDMETHODCALLTYPE SetFromManagedCopy(
            /* [in] */ IUnknown *pObject) = 0;

    };


#else 	/* C style interface */

    typedef struct ICorDebugObjectValueVtbl
    {
        BEGIN_INTERFACE

        HRESULT ( STDMETHODCALLTYPE *QueryInterface )(
            ICorDebugObjectValue * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */
            _COM_Outptr_  void **ppvObject);

        ULONG ( STDMETHODCALLTYPE *AddRef )(
            ICorDebugObjectValue * This);

        ULONG ( STDMETHODCALLTYPE *Release )(
            ICorDebugObjectValue * This);

        HRESULT ( STDMETHODCALLTYPE *GetType )(
            ICorDebugObjectValue * This,
            /* [out] */ CorElementType *pType);

        HRESULT ( STDMETHODCALLTYPE *GetSize )(
            ICorDebugObjectValue * This,
            /* [out] */ ULONG32 *pSize);

        HRESULT ( STDMETHODCALLTYPE *GetAddress )(
            ICorDebugObjectValue * This,
            /* [out] */ CORDB_ADDRESS *pAddress);

        HRESULT ( STDMETHODCALLTYPE *CreateBreakpoint )(
            ICorDebugObjectValue * This,
            /* [out] */ ICorDebugValueBreakpoint **ppBreakpoint);

        HRESULT ( STDMETHODCALLTYPE *GetClass )(
            ICorDebugObjectValue * This,
            /* [out] */ ICorDebugClass **ppClass);

        HRESULT ( STDMETHODCALLTYPE *GetFieldValue )(
            ICorDebugObjectValue * This,
            /* [in] */ ICorDebugClass *pClass,
            /* [in] */ mdFieldDef fieldDef,
            /* [out] */ ICorDebugValue **ppValue);

        HRESULT ( STDMETHODCALLTYPE *GetVirtualMethod )(
            ICorDebugObjectValue * This,
            /* [in] */ mdMemberRef memberRef,
            /* [out] */ ICorDebugFunction **ppFunction);

        HRESULT ( STDMETHODCALLTYPE *GetContext )(
            ICorDebugObjectValue * This,
            /* [out] */ ICorDebugContext **ppContext);

        HRESULT ( STDMETHODCALLTYPE *IsValueClass )(
            ICorDebugObjectValue * This,
            /* [out] */ BOOL *pbIsValueClass);

        HRESULT ( STDMETHODCALLTYPE *GetManagedCopy )(
            ICorDebugObjectValue * This,
            /* [out] */ IUnknown **ppObject);

        HRESULT ( STDMETHODCALLTYPE *SetFromManagedCopy )(
            ICorDebugObjectValue * This,
            /* [in] */ IUnknown *pObject);

        END_INTERFACE
    } ICorDebugObjectValueVtbl;

    interface ICorDebugObjectValue
    {
        CONST_VTBL struct ICorDebugObjectValueVtbl *lpVtbl;
    };



#ifdef COBJMACROS


#define ICorDebugObjectValue_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) )

#define ICorDebugObjectValue_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) )

#define ICorDebugObjectValue_Release(This)	\
    ( (This)->lpVtbl -> Release(This) )


#define ICorDebugObjectValue_GetType(This,pType)	\
    ( (This)->lpVtbl -> GetType(This,pType) )

#define ICorDebugObjectValue_GetSize(This,pSize)	\
    ( (This)->lpVtbl -> GetSize(This,pSize) )

#define ICorDebugObjectValue_GetAddress(This,pAddress)	\
    ( (This)->lpVtbl -> GetAddress(This,pAddress) )

#define ICorDebugObjectValue_CreateBreakpoint(This,ppBreakpoint)	\
    ( (This)->lpVtbl -> CreateBreakpoint(This,ppBreakpoint) )


#define ICorDebugObjectValue_GetClass(This,ppClass)	\
    ( (This)->lpVtbl -> GetClass(This,ppClass) )

#define ICorDebugObjectValue_GetFieldValue(This,pClass,fieldDef,ppValue)	\
    ( (This)->lpVtbl -> GetFieldValue(This,pClass,fieldDef,ppValue) )

#define ICorDebugObjectValue_GetVirtualMethod(This,memberRef,ppFunction)	\
    ( (This)->lpVtbl -> GetVirtualMethod(This,memberRef,ppFunction) )

#define ICorDebugObjectValue_GetContext(This,ppContext)	\
    ( (This)->lpVtbl -> GetContext(This,ppContext) )

#define ICorDebugObjectValue_IsValueClass(This,pbIsValueClass)	\
    ( (This)->lpVtbl -> IsValueClass(This,pbIsValueClass) )

#define ICorDebugObjectValue_GetManagedCopy(This,ppObject)	\
    ( (This)->lpVtbl -> GetManagedCopy(This,ppObject) )

#define ICorDebugObjectValue_SetFromManagedCopy(This,pObject)	\
    ( (This)->lpVtbl -> SetFromManagedCopy(This,pObject) )

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ICorDebugObjectValue_INTERFACE_DEFINED__ */


#ifndef __ICorDebugObjectValue2_INTERFACE_DEFINED__
#define __ICorDebugObjectValue2_INTERFACE_DEFINED__

/* interface ICorDebugObjectValue2 */
/* [unique][uuid][local][object] */


EXTERN_C const IID IID_ICorDebugObjectValue2;

#if defined(__cplusplus) && !defined(CINTERFACE)

    MIDL_INTERFACE("49E4A320-4A9B-4eca-B105-229FB7D5009F")
    ICorDebugObjectValue2 : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE GetVirtualMethodAndType(
            /* [in] */ mdMemberRef memberRef,
            /* [out] */ ICorDebugFunction **ppFunction,
            /* [out] */ ICorDebugType **ppType) = 0;

    };


#else 	/* C style interface */

    typedef struct ICorDebugObjectValue2Vtbl
    {
        BEGIN_INTERFACE

        HRESULT ( STDMETHODCALLTYPE *QueryInterface )(
            ICorDebugObjectValue2 * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */
            _COM_Outptr_  void **ppvObject);

        ULONG ( STDMETHODCALLTYPE *AddRef )(
            ICorDebugObjectValue2 * This);

        ULONG ( STDMETHODCALLTYPE *Release )(
            ICorDebugObjectValue2 * This);

        HRESULT ( STDMETHODCALLTYPE *GetVirtualMethodAndType )(
            ICorDebugObjectValue2 * This,
            /* [in] */ mdMemberRef memberRef,
            /* [out] */ ICorDebugFunction **ppFunction,
            /* [out] */ ICorDebugType **ppType);

        END_INTERFACE
    } ICorDebugObjectValue2Vtbl;

    interface ICorDebugObjectValue2
    {
        CONST_VTBL struct ICorDebugObjectValue2Vtbl *lpVtbl;
    };



#ifdef COBJMACROS


#define ICorDebugObjectValue2_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) )

#define ICorDebugObjectValue2_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) )

#define ICorDebugObjectValue2_Release(This)	\
    ( (This)->lpVtbl -> Release(This) )


#define ICorDebugObjectValue2_GetVirtualMethodAndType(This,memberRef,ppFunction,ppType)	\
    ( (This)->lpVtbl -> GetVirtualMethodAndType(This,memberRef,ppFunction,ppType) )

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ICorDebugObjectValue2_INTERFACE_DEFINED__ */


#ifndef __ICorDebugDelegateObjectValue_INTERFACE_DEFINED__
#define __ICorDebugDelegateObjectValue_INTERFACE_DEFINED__

/* interface ICorDebugDelegateObjectValue */
/* [unique][uuid][local][object] */


EXTERN_C const IID IID_ICorDebugDelegateObjectValue;

#if defined(__cplusplus) && !defined(CINTERFACE)

    MIDL_INTERFACE("3AF70CC7-6047-47F6-A5C5-090A1A622638")
    ICorDebugDelegateObjectValue : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE GetTarget(
            /* [out] */ ICorDebugReferenceValue **ppObject) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetFunction(
            /* [out] */ ICorDebugFunction **ppFunction) = 0;

    };


#else 	/* C style interface */

    typedef struct ICorDebugDelegateObjectValueVtbl
    {
        BEGIN_INTERFACE

        HRESULT ( STDMETHODCALLTYPE *QueryInterface )(
            ICorDebugDelegateObjectValue * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */
            _COM_Outptr_  void **ppvObject);

        ULONG ( STDMETHODCALLTYPE *AddRef )(
            ICorDebugDelegateObjectValue * This);

        ULONG ( STDMETHODCALLTYPE *Release )(
            ICorDebugDelegateObjectValue * This);

        HRESULT ( STDMETHODCALLTYPE *GetTarget )(
            ICorDebugDelegateObjectValue * This,
            /* [out] */ ICorDebugReferenceValue **ppObject);

        HRESULT ( STDMETHODCALLTYPE *GetFunction )(
            ICorDebugDelegateObjectValue * This,
            /* [out] */ ICorDebugFunction **ppFunction);

        END_INTERFACE
    } ICorDebugDelegateObjectValueVtbl;

    interface ICorDebugDelegateObjectValue
    {
        CONST_VTBL struct ICorDebugDelegateObjectValueVtbl *lpVtbl;
    };



#ifdef COBJMACROS


#define ICorDebugDelegateObjectValue_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) )

#define ICorDebugDelegateObjectValue_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) )

#define ICorDebugDelegateObjectValue_Release(This)	\
    ( (This)->lpVtbl -> Release(This) )


#define ICorDebugDelegateObjectValue_GetTarget(This,ppObject)	\
    ( (This)->lpVtbl -> GetTarget(This,ppObject) )

#define ICorDebugDelegateObjectValue_GetFunction(This,ppFunction)	\
    ( (This)->lpVtbl -> GetFunction(This,ppFunction) )

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ICorDebugDelegateObjectValue_INTERFACE_DEFINED__ */


#ifndef __ICorDebugBoxValue_INTERFACE_DEFINED__
#define __ICorDebugBoxValue_INTERFACE_DEFINED__

/* interface ICorDebugBoxValue */
/* [unique][uuid][local][object] */


EXTERN_C const IID IID_ICorDebugBoxValue;

#if defined(__cplusplus) && !defined(CINTERFACE)

    MIDL_INTERFACE("CC7BCAFC-8A68-11d2-983C-0000F808342D")
    ICorDebugBoxValue : public ICorDebugHeapValue
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE GetObject(
            /* [out] */ ICorDebugObjectValue **ppObject) = 0;

    };


#else 	/* C style interface */

    typedef struct ICorDebugBoxValueVtbl
    {
        BEGIN_INTERFACE

        HRESULT ( STDMETHODCALLTYPE *QueryInterface )(
            ICorDebugBoxValue * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */
            _COM_Outptr_  void **ppvObject);

        ULONG ( STDMETHODCALLTYPE *AddRef )(
            ICorDebugBoxValue * This);

        ULONG ( STDMETHODCALLTYPE *Release )(
            ICorDebugBoxValue * This);

        HRESULT ( STDMETHODCALLTYPE *GetType )(
            ICorDebugBoxValue * This,
            /* [out] */ CorElementType *pType);

        HRESULT ( STDMETHODCALLTYPE *GetSize )(
            ICorDebugBoxValue * This,
            /* [out] */ ULONG32 *pSize);

        HRESULT ( STDMETHODCALLTYPE *GetAddress )(
            ICorDebugBoxValue * This,
            /* [out] */ CORDB_ADDRESS *pAddress);

        HRESULT ( STDMETHODCALLTYPE *CreateBreakpoint )(
            ICorDebugBoxValue * This,
            /* [out] */ ICorDebugValueBreakpoint **ppBreakpoint);

        HRESULT ( STDMETHODCALLTYPE *IsValid )(
            ICorDebugBoxValue * This,
            /* [out] */ BOOL *pbValid);

        HRESULT ( STDMETHODCALLTYPE *CreateRelocBreakpoint )(
            ICorDebugBoxValue * This,
            /* [out] */ ICorDebugValueBreakpoint **ppBreakpoint);

        HRESULT ( STDMETHODCALLTYPE *GetObject )(
            ICorDebugBoxValue * This,
            /* [out] */ ICorDebugObjectValue **ppObject);

        END_INTERFACE
    } ICorDebugBoxValueVtbl;

    interface ICorDebugBoxValue
    {
        CONST_VTBL struct ICorDebugBoxValueVtbl *lpVtbl;
    };



#ifdef COBJMACROS


#define ICorDebugBoxValue_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) )

#define ICorDebugBoxValue_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) )

#define ICorDebugBoxValue_Release(This)	\
    ( (This)->lpVtbl -> Release(This) )


#define ICorDebugBoxValue_GetType(This,pType)	\
    ( (This)->lpVtbl -> GetType(This,pType) )

#define ICorDebugBoxValue_GetSize(This,pSize)	\
    ( (This)->lpVtbl -> GetSize(This,pSize) )

#define ICorDebugBoxValue_GetAddress(This,pAddress)	\
    ( (This)->lpVtbl -> GetAddress(This,pAddress) )

#define ICorDebugBoxValue_CreateBreakpoint(This,ppBreakpoint)	\
    ( (This)->lpVtbl -> CreateBreakpoint(This,ppBreakpoint) )


#define ICorDebugBoxValue_IsValid(This,pbValid)	\
    ( (This)->lpVtbl -> IsValid(This,pbValid) )

#define ICorDebugBoxValue_CreateRelocBreakpoint(This,ppBreakpoint)	\
    ( (This)->lpVtbl -> CreateRelocBreakpoint(This,ppBreakpoint) )


#define ICorDebugBoxValue_GetObject(This,ppObject)	\
    ( (This)->lpVtbl -> GetObject(This,ppObject) )

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ICorDebugBoxValue_INTERFACE_DEFINED__ */


/* interface __MIDL_itf_cordebug_0000_0103 */
/* [local] */

#pragma warning(push)
#pragma warning(disable:28718)


extern RPC_IF_HANDLE __MIDL_itf_cordebug_0000_0103_v0_0_c_ifspec;
extern RPC_IF_HANDLE __MIDL_itf_cordebug_0000_0103_v0_0_s_ifspec;

#ifndef __ICorDebugStringValue_INTERFACE_DEFINED__
#define __ICorDebugStringValue_INTERFACE_DEFINED__

/* interface ICorDebugStringValue */
/* [unique][uuid][local][object] */


EXTERN_C const IID IID_ICorDebugStringValue;

#if defined(__cplusplus) && !defined(CINTERFACE)

    MIDL_INTERFACE("CC7BCAFD-8A68-11d2-983C-0000F808342D")
    ICorDebugStringValue : public ICorDebugHeapValue
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE GetLength(
            /* [out] */ ULONG32 *pcchString) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetString(
            /* [in] */ ULONG32 cchString,
            /* [out] */ ULONG32 *pcchString,
            /* [length_is][size_is][out] */ WCHAR szString[  ]) = 0;

    };


#else 	/* C style interface */

    typedef struct ICorDebugStringValueVtbl
    {
        BEGIN_INTERFACE

        HRESULT ( STDMETHODCALLTYPE *QueryInterface )(
            ICorDebugStringValue * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */
            _COM_Outptr_  void **ppvObject);

        ULONG ( STDMETHODCALLTYPE *AddRef )(
            ICorDebugStringValue * This);

        ULONG ( STDMETHODCALLTYPE *Release )(
            ICorDebugStringValue * This);

        HRESULT ( STDMETHODCALLTYPE *GetType )(
            ICorDebugStringValue * This,
            /* [out] */ CorElementType *pType);

        HRESULT ( STDMETHODCALLTYPE *GetSize )(
            ICorDebugStringValue * This,
            /* [out] */ ULONG32 *pSize);

        HRESULT ( STDMETHODCALLTYPE *GetAddress )(
            ICorDebugStringValue * This,
            /* [out] */ CORDB_ADDRESS *pAddress);

        HRESULT ( STDMETHODCALLTYPE *CreateBreakpoint )(
            ICorDebugStringValue * This,
            /* [out] */ ICorDebugValueBreakpoint **ppBreakpoint);

        HRESULT ( STDMETHODCALLTYPE *IsValid )(
            ICorDebugStringValue * This,
            /* [out] */ BOOL *pbValid);

        HRESULT ( STDMETHODCALLTYPE *CreateRelocBreakpoint )(
            ICorDebugStringValue * This,
            /* [out] */ ICorDebugValueBreakpoint **ppBreakpoint);

        HRESULT ( STDMETHODCALLTYPE *GetLength )(
            ICorDebugStringValue * This,
            /* [out] */ ULONG32 *pcchString);

        HRESULT ( STDMETHODCALLTYPE *GetString )(
            ICorDebugStringValue * This,
            /* [in] */ ULONG32 cchString,
            /* [out] */ ULONG32 *pcchString,
            /* [length_is][size_is][out] */ WCHAR szString[  ]);

        END_INTERFACE
    } ICorDebugStringValueVtbl;

    interface ICorDebugStringValue
    {
        CONST_VTBL struct ICorDebugStringValueVtbl *lpVtbl;
    };



#ifdef COBJMACROS


#define ICorDebugStringValue_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) )

#define ICorDebugStringValue_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) )

#define ICorDebugStringValue_Release(This)	\
    ( (This)->lpVtbl -> Release(This) )


#define ICorDebugStringValue_GetType(This,pType)	\
    ( (This)->lpVtbl -> GetType(This,pType) )

#define ICorDebugStringValue_GetSize(This,pSize)	\
    ( (This)->lpVtbl -> GetSize(This,pSize) )

#define ICorDebugStringValue_GetAddress(This,pAddress)	\
    ( (This)->lpVtbl -> GetAddress(This,pAddress) )

#define ICorDebugStringValue_CreateBreakpoint(This,ppBreakpoint)	\
    ( (This)->lpVtbl -> CreateBreakpoint(This,ppBreakpoint) )


#define ICorDebugStringValue_IsValid(This,pbValid)	\
    ( (This)->lpVtbl -> IsValid(This,pbValid) )

#define ICorDebugStringValue_CreateRelocBreakpoint(This,ppBreakpoint)	\
    ( (This)->lpVtbl -> CreateRelocBreakpoint(This,ppBreakpoint) )


#define ICorDebugStringValue_GetLength(This,pcchString)	\
    ( (This)->lpVtbl -> GetLength(This,pcchString) )

#define ICorDebugStringValue_GetString(This,cchString,pcchString,szString)	\
    ( (This)->lpVtbl -> GetString(This,cchString,pcchString,szString) )

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ICorDebugStringValue_INTERFACE_DEFINED__ */


/* interface __MIDL_itf_cordebug_0000_0104 */
/* [local] */

#pragma warning(pop)


extern RPC_IF_HANDLE __MIDL_itf_cordebug_0000_0104_v0_0_c_ifspec;
extern RPC_IF_HANDLE __MIDL_itf_cordebug_0000_0104_v0_0_s_ifspec;

#ifndef __ICorDebugArrayValue_INTERFACE_DEFINED__
#define __ICorDebugArrayValue_INTERFACE_DEFINED__

/* interface ICorDebugArrayValue */
/* [unique][uuid][local][object] */


EXTERN_C const IID IID_ICorDebugArrayValue;

#if defined(__cplusplus) && !defined(CINTERFACE)

    MIDL_INTERFACE("0405B0DF-A660-11d2-BD02-0000F80849BD")
    ICorDebugArrayValue : public ICorDebugHeapValue
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE GetElementType(
            /* [out] */ CorElementType *pType) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetRank(
            /* [out] */ ULONG32 *pnRank) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetCount(
            /* [out] */ ULONG32 *pnCount) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetDimensions(
            /* [in] */ ULONG32 cdim,
            /* [length_is][size_is][out] */ ULONG32 dims[  ]) = 0;

        virtual HRESULT STDMETHODCALLTYPE HasBaseIndicies(
            /* [out] */ BOOL *pbHasBaseIndicies) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetBaseIndicies(
            /* [in] */ ULONG32 cdim,
            /* [length_is][size_is][out] */ ULONG32 indicies[  ]) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetElement(
            /* [in] */ ULONG32 cdim,
            /* [length_is][size_is][in] */ ULONG32 indices[  ],
            /* [out] */ ICorDebugValue **ppValue) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetElementAtPosition(
            /* [in] */ ULONG32 nPosition,
            /* [out] */ ICorDebugValue **ppValue) = 0;

    };


#else 	/* C style interface */

    typedef struct ICorDebugArrayValueVtbl
    {
        BEGIN_INTERFACE

        HRESULT ( STDMETHODCALLTYPE *QueryInterface )(
            ICorDebugArrayValue * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */
            _COM_Outptr_  void **ppvObject);

        ULONG ( STDMETHODCALLTYPE *AddRef )(
            ICorDebugArrayValue * This);

        ULONG ( STDMETHODCALLTYPE *Release )(
            ICorDebugArrayValue * This);

        HRESULT ( STDMETHODCALLTYPE *GetType )(
            ICorDebugArrayValue * This,
            /* [out] */ CorElementType *pType);

        HRESULT ( STDMETHODCALLTYPE *GetSize )(
            ICorDebugArrayValue * This,
            /* [out] */ ULONG32 *pSize);

        HRESULT ( STDMETHODCALLTYPE *GetAddress )(
            ICorDebugArrayValue * This,
            /* [out] */ CORDB_ADDRESS *pAddress);

        HRESULT ( STDMETHODCALLTYPE *CreateBreakpoint )(
            ICorDebugArrayValue * This,
            /* [out] */ ICorDebugValueBreakpoint **ppBreakpoint);

        HRESULT ( STDMETHODCALLTYPE *IsValid )(
            ICorDebugArrayValue * This,
            /* [out] */ BOOL *pbValid);

        HRESULT ( STDMETHODCALLTYPE *CreateRelocBreakpoint )(
            ICorDebugArrayValue * This,
            /* [out] */ ICorDebugValueBreakpoint **ppBreakpoint);

        HRESULT ( STDMETHODCALLTYPE *GetElementType )(
            ICorDebugArrayValue * This,
            /* [out] */ CorElementType *pType);

        HRESULT ( STDMETHODCALLTYPE *GetRank )(
            ICorDebugArrayValue * This,
            /* [out] */ ULONG32 *pnRank);

        HRESULT ( STDMETHODCALLTYPE *GetCount )(
            ICorDebugArrayValue * This,
            /* [out] */ ULONG32 *pnCount);

        HRESULT ( STDMETHODCALLTYPE *GetDimensions )(
            ICorDebugArrayValue * This,
            /* [in] */ ULONG32 cdim,
            /* [length_is][size_is][out] */ ULONG32 dims[  ]);

        HRESULT ( STDMETHODCALLTYPE *HasBaseIndicies )(
            ICorDebugArrayValue * This,
            /* [out] */ BOOL *pbHasBaseIndicies);

        HRESULT ( STDMETHODCALLTYPE *GetBaseIndicies )(
            ICorDebugArrayValue * This,
            /* [in] */ ULONG32 cdim,
            /* [length_is][size_is][out] */ ULONG32 indicies[  ]);

        HRESULT ( STDMETHODCALLTYPE *GetElement )(
            ICorDebugArrayValue * This,
            /* [in] */ ULONG32 cdim,
            /* [length_is][size_is][in] */ ULONG32 indices[  ],
            /* [out] */ ICorDebugValue **ppValue);

        HRESULT ( STDMETHODCALLTYPE *GetElementAtPosition )(
            ICorDebugArrayValue * This,
            /* [in] */ ULONG32 nPosition,
            /* [out] */ ICorDebugValue **ppValue);

        END_INTERFACE
    } ICorDebugArrayValueVtbl;

    interface ICorDebugArrayValue
    {
        CONST_VTBL struct ICorDebugArrayValueVtbl *lpVtbl;
    };



#ifdef COBJMACROS


#define ICorDebugArrayValue_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) )

#define ICorDebugArrayValue_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) )

#define ICorDebugArrayValue_Release(This)	\
    ( (This)->lpVtbl -> Release(This) )


#define ICorDebugArrayValue_GetType(This,pType)	\
    ( (This)->lpVtbl -> GetType(This,pType) )

#define ICorDebugArrayValue_GetSize(This,pSize)	\
    ( (This)->lpVtbl -> GetSize(This,pSize) )

#define ICorDebugArrayValue_GetAddress(This,pAddress)	\
    ( (This)->lpVtbl -> GetAddress(This,pAddress) )

#define ICorDebugArrayValue_CreateBreakpoint(This,ppBreakpoint)	\
    ( (This)->lpVtbl -> CreateBreakpoint(This,ppBreakpoint) )


#define ICorDebugArrayValue_IsValid(This,pbValid)	\
    ( (This)->lpVtbl -> IsValid(This,pbValid) )

#define ICorDebugArrayValue_CreateRelocBreakpoint(This,ppBreakpoint)	\
    ( (This)->lpVtbl -> CreateRelocBreakpoint(This,ppBreakpoint) )


#define ICorDebugArrayValue_GetElementType(This,pType)	\
    ( (This)->lpVtbl -> GetElementType(This,pType) )

#define ICorDebugArrayValue_GetRank(This,pnRank)	\
    ( (This)->lpVtbl -> GetRank(This,pnRank) )

#define ICorDebugArrayValue_GetCount(This,pnCount)	\
    ( (This)->lpVtbl -> GetCount(This,pnCount) )

#define ICorDebugArrayValue_GetDimensions(This,cdim,dims)	\
    ( (This)->lpVtbl -> GetDimensions(This,cdim,dims) )

#define ICorDebugArrayValue_HasBaseIndicies(This,pbHasBaseIndicies)	\
    ( (This)->lpVtbl -> HasBaseIndicies(This,pbHasBaseIndicies) )

#define ICorDebugArrayValue_GetBaseIndicies(This,cdim,indicies)	\
    ( (This)->lpVtbl -> GetBaseIndicies(This,cdim,indicies) )

#define ICorDebugArrayValue_GetElement(This,cdim,indices,ppValue)	\
    ( (This)->lpVtbl -> GetElement(This,cdim,indices,ppValue) )

#define ICorDebugArrayValue_GetElementAtPosition(This,nPosition,ppValue)	\
    ( (This)->lpVtbl -> GetElementAtPosition(This,nPosition,ppValue) )

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ICorDebugArrayValue_INTERFACE_DEFINED__ */


#ifndef __ICorDebugVariableHome_INTERFACE_DEFINED__
#define __ICorDebugVariableHome_INTERFACE_DEFINED__

/* interface ICorDebugVariableHome */
/* [unique][uuid][local][object] */

typedef
enum VariableLocationType
    {
        VLT_REGISTER	= 0,
        VLT_REGISTER_RELATIVE	= ( VLT_REGISTER + 1 ) ,
        VLT_INVALID	= ( VLT_REGISTER_RELATIVE + 1 )
    } 	VariableLocationType;


EXTERN_C const IID IID_ICorDebugVariableHome;

#if defined(__cplusplus) && !defined(CINTERFACE)

    MIDL_INTERFACE("50847b8d-f43f-41b0-924c-6383a5f2278b")
    ICorDebugVariableHome : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE GetCode(
            /* [out] */ ICorDebugCode **ppCode) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetSlotIndex(
            /* [out] */ ULONG32 *pSlotIndex) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetArgumentIndex(
            /* [out] */ ULONG32 *pArgumentIndex) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetLiveRange(
            /* [out] */ ULONG32 *pStartOffset,
            /* [out] */ ULONG32 *pEndOffset) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetLocationType(
            /* [out] */ VariableLocationType *pLocationType) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetRegister(
            /* [out] */ CorDebugRegister *pRegister) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetOffset(
            /* [out] */ LONG *pOffset) = 0;

    };


#else 	/* C style interface */

    typedef struct ICorDebugVariableHomeVtbl
    {
        BEGIN_INTERFACE

        HRESULT ( STDMETHODCALLTYPE *QueryInterface )(
            ICorDebugVariableHome * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */
            _COM_Outptr_  void **ppvObject);

        ULONG ( STDMETHODCALLTYPE *AddRef )(
            ICorDebugVariableHome * This);

        ULONG ( STDMETHODCALLTYPE *Release )(
            ICorDebugVariableHome * This);

        HRESULT ( STDMETHODCALLTYPE *GetCode )(
            ICorDebugVariableHome * This,
            /* [out] */ ICorDebugCode **ppCode);

        HRESULT ( STDMETHODCALLTYPE *GetSlotIndex )(
            ICorDebugVariableHome * This,
            /* [out] */ ULONG32 *pSlotIndex);

        HRESULT ( STDMETHODCALLTYPE *GetArgumentIndex )(
            ICorDebugVariableHome * This,
            /* [out] */ ULONG32 *pArgumentIndex);

        HRESULT ( STDMETHODCALLTYPE *GetLiveRange )(
            ICorDebugVariableHome * This,
            /* [out] */ ULONG32 *pStartOffset,
            /* [out] */ ULONG32 *pEndOffset);

        HRESULT ( STDMETHODCALLTYPE *GetLocationType )(
            ICorDebugVariableHome * This,
            /* [out] */ VariableLocationType *pLocationType);

        HRESULT ( STDMETHODCALLTYPE *GetRegister )(
            ICorDebugVariableHome * This,
            /* [out] */ CorDebugRegister *pRegister);

        HRESULT ( STDMETHODCALLTYPE *GetOffset )(
            ICorDebugVariableHome * This,
            /* [out] */ LONG *pOffset);

        END_INTERFACE
    } ICorDebugVariableHomeVtbl;

    interface ICorDebugVariableHome
    {
        CONST_VTBL struct ICorDebugVariableHomeVtbl *lpVtbl;
    };



#ifdef COBJMACROS


#define ICorDebugVariableHome_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) )

#define ICorDebugVariableHome_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) )

#define ICorDebugVariableHome_Release(This)	\
    ( (This)->lpVtbl -> Release(This) )


#define ICorDebugVariableHome_GetCode(This,ppCode)	\
    ( (This)->lpVtbl -> GetCode(This,ppCode) )

#define ICorDebugVariableHome_GetSlotIndex(This,pSlotIndex)	\
    ( (This)->lpVtbl -> GetSlotIndex(This,pSlotIndex) )

#define ICorDebugVariableHome_GetArgumentIndex(This,pArgumentIndex)	\
    ( (This)->lpVtbl -> GetArgumentIndex(This,pArgumentIndex) )

#define ICorDebugVariableHome_GetLiveRange(This,pStartOffset,pEndOffset)	\
    ( (This)->lpVtbl -> GetLiveRange(This,pStartOffset,pEndOffset) )

#define ICorDebugVariableHome_GetLocationType(This,pLocationType)	\
    ( (This)->lpVtbl -> GetLocationType(This,pLocationType) )

#define ICorDebugVariableHome_GetRegister(This,pRegister)	\
    ( (This)->lpVtbl -> GetRegister(This,pRegister) )

#define ICorDebugVariableHome_GetOffset(This,pOffset)	\
    ( (This)->lpVtbl -> GetOffset(This,pOffset) )

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ICorDebugVariableHome_INTERFACE_DEFINED__ */


#ifndef __ICorDebugHandleValue_INTERFACE_DEFINED__
#define __ICorDebugHandleValue_INTERFACE_DEFINED__

/* interface ICorDebugHandleValue */
/* [unique][uuid][local][object] */


EXTERN_C const IID IID_ICorDebugHandleValue;

#if defined(__cplusplus) && !defined(CINTERFACE)

    MIDL_INTERFACE("029596E8-276B-46a1-9821-732E96BBB00B")
    ICorDebugHandleValue : public ICorDebugReferenceValue
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE GetHandleType(
            /* [out] */ CorDebugHandleType *pType) = 0;

        virtual HRESULT STDMETHODCALLTYPE Dispose( void) = 0;

    };


#else 	/* C style interface */

    typedef struct ICorDebugHandleValueVtbl
    {
        BEGIN_INTERFACE

        HRESULT ( STDMETHODCALLTYPE *QueryInterface )(
            ICorDebugHandleValue * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */
            _COM_Outptr_  void **ppvObject);

        ULONG ( STDMETHODCALLTYPE *AddRef )(
            ICorDebugHandleValue * This);

        ULONG ( STDMETHODCALLTYPE *Release )(
            ICorDebugHandleValue * This);

        HRESULT ( STDMETHODCALLTYPE *GetType )(
            ICorDebugHandleValue * This,
            /* [out] */ CorElementType *pType);

        HRESULT ( STDMETHODCALLTYPE *GetSize )(
            ICorDebugHandleValue * This,
            /* [out] */ ULONG32 *pSize);

        HRESULT ( STDMETHODCALLTYPE *GetAddress )(
            ICorDebugHandleValue * This,
            /* [out] */ CORDB_ADDRESS *pAddress);

        HRESULT ( STDMETHODCALLTYPE *CreateBreakpoint )(
            ICorDebugHandleValue * This,
            /* [out] */ ICorDebugValueBreakpoint **ppBreakpoint);

        HRESULT ( STDMETHODCALLTYPE *IsNull )(
            ICorDebugHandleValue * This,
            /* [out] */ BOOL *pbNull);

        HRESULT ( STDMETHODCALLTYPE *GetValue )(
            ICorDebugHandleValue * This,
            /* [out] */ CORDB_ADDRESS *pValue);

        HRESULT ( STDMETHODCALLTYPE *SetValue )(
            ICorDebugHandleValue * This,
            /* [in] */ CORDB_ADDRESS value);

        HRESULT ( STDMETHODCALLTYPE *Dereference )(
            ICorDebugHandleValue * This,
            /* [out] */ ICorDebugValue **ppValue);

        HRESULT ( STDMETHODCALLTYPE *DereferenceStrong )(
            ICorDebugHandleValue * This,
            /* [out] */ ICorDebugValue **ppValue);

        HRESULT ( STDMETHODCALLTYPE *GetHandleType )(
            ICorDebugHandleValue * This,
            /* [out] */ CorDebugHandleType *pType);

        HRESULT ( STDMETHODCALLTYPE *Dispose )(
            ICorDebugHandleValue * This);

        END_INTERFACE
    } ICorDebugHandleValueVtbl;

    interface ICorDebugHandleValue
    {
        CONST_VTBL struct ICorDebugHandleValueVtbl *lpVtbl;
    };



#ifdef COBJMACROS


#define ICorDebugHandleValue_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) )

#define ICorDebugHandleValue_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) )

#define ICorDebugHandleValue_Release(This)	\
    ( (This)->lpVtbl -> Release(This) )


#define ICorDebugHandleValue_GetType(This,pType)	\
    ( (This)->lpVtbl -> GetType(This,pType) )

#define ICorDebugHandleValue_GetSize(This,pSize)	\
    ( (This)->lpVtbl -> GetSize(This,pSize) )

#define ICorDebugHandleValue_GetAddress(This,pAddress)	\
    ( (This)->lpVtbl -> GetAddress(This,pAddress) )

#define ICorDebugHandleValue_CreateBreakpoint(This,ppBreakpoint)	\
    ( (This)->lpVtbl -> CreateBreakpoint(This,ppBreakpoint) )


#define ICorDebugHandleValue_IsNull(This,pbNull)	\
    ( (This)->lpVtbl -> IsNull(This,pbNull) )

#define ICorDebugHandleValue_GetValue(This,pValue)	\
    ( (This)->lpVtbl -> GetValue(This,pValue) )

#define ICorDebugHandleValue_SetValue(This,value)	\
    ( (This)->lpVtbl -> SetValue(This,value) )

#define ICorDebugHandleValue_Dereference(This,ppValue)	\
    ( (This)->lpVtbl -> Dereference(This,ppValue) )

#define ICorDebugHandleValue_DereferenceStrong(This,ppValue)	\
    ( (This)->lpVtbl -> DereferenceStrong(This,ppValue) )


#define ICorDebugHandleValue_GetHandleType(This,pType)	\
    ( (This)->lpVtbl -> GetHandleType(This,pType) )

#define ICorDebugHandleValue_Dispose(This)	\
    ( (This)->lpVtbl -> Dispose(This) )

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ICorDebugHandleValue_INTERFACE_DEFINED__ */


#ifndef __ICorDebugContext_INTERFACE_DEFINED__
#define __ICorDebugContext_INTERFACE_DEFINED__

/* interface ICorDebugContext */
/* [unique][uuid][local][object] */


EXTERN_C const IID IID_ICorDebugContext;

#if defined(__cplusplus) && !defined(CINTERFACE)

    MIDL_INTERFACE("CC7BCB00-8A68-11d2-983C-0000F808342D")
    ICorDebugContext : public ICorDebugObjectValue
    {
    public:
    };


#else 	/* C style interface */

    typedef struct ICorDebugContextVtbl
    {
        BEGIN_INTERFACE

        HRESULT ( STDMETHODCALLTYPE *QueryInterface )(
            ICorDebugContext * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */
            _COM_Outptr_  void **ppvObject);

        ULONG ( STDMETHODCALLTYPE *AddRef )(
            ICorDebugContext * This);

        ULONG ( STDMETHODCALLTYPE *Release )(
            ICorDebugContext * This);

        HRESULT ( STDMETHODCALLTYPE *GetType )(
            ICorDebugContext * This,
            /* [out] */ CorElementType *pType);

        HRESULT ( STDMETHODCALLTYPE *GetSize )(
            ICorDebugContext * This,
            /* [out] */ ULONG32 *pSize);

        HRESULT ( STDMETHODCALLTYPE *GetAddress )(
            ICorDebugContext * This,
            /* [out] */ CORDB_ADDRESS *pAddress);

        HRESULT ( STDMETHODCALLTYPE *CreateBreakpoint )(
            ICorDebugContext * This,
            /* [out] */ ICorDebugValueBreakpoint **ppBreakpoint);

        HRESULT ( STDMETHODCALLTYPE *GetClass )(
            ICorDebugContext * This,
            /* [out] */ ICorDebugClass **ppClass);

        HRESULT ( STDMETHODCALLTYPE *GetFieldValue )(
            ICorDebugContext * This,
            /* [in] */ ICorDebugClass *pClass,
            /* [in] */ mdFieldDef fieldDef,
            /* [out] */ ICorDebugValue **ppValue);

        HRESULT ( STDMETHODCALLTYPE *GetVirtualMethod )(
            ICorDebugContext * This,
            /* [in] */ mdMemberRef memberRef,
            /* [out] */ ICorDebugFunction **ppFunction);

        HRESULT ( STDMETHODCALLTYPE *GetContext )(
            ICorDebugContext * This,
            /* [out] */ ICorDebugContext **ppContext);

        HRESULT ( STDMETHODCALLTYPE *IsValueClass )(
            ICorDebugContext * This,
            /* [out] */ BOOL *pbIsValueClass);

        HRESULT ( STDMETHODCALLTYPE *GetManagedCopy )(
            ICorDebugContext * This,
            /* [out] */ IUnknown **ppObject);

        HRESULT ( STDMETHODCALLTYPE *SetFromManagedCopy )(
            ICorDebugContext * This,
            /* [in] */ IUnknown *pObject);

        END_INTERFACE
    } ICorDebugContextVtbl;

    interface ICorDebugContext
    {
        CONST_VTBL struct ICorDebugContextVtbl *lpVtbl;
    };



#ifdef COBJMACROS


#define ICorDebugContext_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) )

#define ICorDebugContext_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) )

#define ICorDebugContext_Release(This)	\
    ( (This)->lpVtbl -> Release(This) )


#define ICorDebugContext_GetType(This,pType)	\
    ( (This)->lpVtbl -> GetType(This,pType) )

#define ICorDebugContext_GetSize(This,pSize)	\
    ( (This)->lpVtbl -> GetSize(This,pSize) )

#define ICorDebugContext_GetAddress(This,pAddress)	\
    ( (This)->lpVtbl -> GetAddress(This,pAddress) )

#define ICorDebugContext_CreateBreakpoint(This,ppBreakpoint)	\
    ( (This)->lpVtbl -> CreateBreakpoint(This,ppBreakpoint) )


#define ICorDebugContext_GetClass(This,ppClass)	\
    ( (This)->lpVtbl -> GetClass(This,ppClass) )

#define ICorDebugContext_GetFieldValue(This,pClass,fieldDef,ppValue)	\
    ( (This)->lpVtbl -> GetFieldValue(This,pClass,fieldDef,ppValue) )

#define ICorDebugContext_GetVirtualMethod(This,memberRef,ppFunction)	\
    ( (This)->lpVtbl -> GetVirtualMethod(This,memberRef,ppFunction) )

#define ICorDebugContext_GetContext(This,ppContext)	\
    ( (This)->lpVtbl -> GetContext(This,ppContext) )

#define ICorDebugContext_IsValueClass(This,pbIsValueClass)	\
    ( (This)->lpVtbl -> IsValueClass(This,pbIsValueClass) )

#define ICorDebugContext_GetManagedCopy(This,ppObject)	\
    ( (This)->lpVtbl -> GetManagedCopy(This,ppObject) )

#define ICorDebugContext_SetFromManagedCopy(This,pObject)	\
    ( (This)->lpVtbl -> SetFromManagedCopy(This,pObject) )


#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ICorDebugContext_INTERFACE_DEFINED__ */


#ifndef __ICorDebugComObjectValue_INTERFACE_DEFINED__
#define __ICorDebugComObjectValue_INTERFACE_DEFINED__

/* interface ICorDebugComObjectValue */
/* [unique][uuid][local][object] */


EXTERN_C const IID IID_ICorDebugComObjectValue;

#if defined(__cplusplus) && !defined(CINTERFACE)

    MIDL_INTERFACE("5F69C5E5-3E12-42DF-B371-F9D761D6EE24")
    ICorDebugComObjectValue : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE GetCachedInterfaceTypes(
            /* [in] */ BOOL bIInspectableOnly,
            /* [out] */ ICorDebugTypeEnum **ppInterfacesEnum) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetCachedInterfacePointers(
            /* [in] */ BOOL bIInspectableOnly,
            /* [in] */ ULONG32 celt,
            /* [out] */ ULONG32 *pcEltFetched,
            /* [length_is][size_is][out] */ CORDB_ADDRESS *ptrs) = 0;

    };


#else 	/* C style interface */

    typedef struct ICorDebugComObjectValueVtbl
    {
        BEGIN_INTERFACE

        HRESULT ( STDMETHODCALLTYPE *QueryInterface )(
            ICorDebugComObjectValue * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */
            _COM_Outptr_  void **ppvObject);

        ULONG ( STDMETHODCALLTYPE *AddRef )(
            ICorDebugComObjectValue * This);

        ULONG ( STDMETHODCALLTYPE *Release )(
            ICorDebugComObjectValue * This);

        HRESULT ( STDMETHODCALLTYPE *GetCachedInterfaceTypes )(
            ICorDebugComObjectValue * This,
            /* [in] */ BOOL bIInspectableOnly,
            /* [out] */ ICorDebugTypeEnum **ppInterfacesEnum);

        HRESULT ( STDMETHODCALLTYPE *GetCachedInterfacePointers )(
            ICorDebugComObjectValue * This,
            /* [in] */ BOOL bIInspectableOnly,
            /* [in] */ ULONG32 celt,
            /* [out] */ ULONG32 *pcEltFetched,
            /* [length_is][size_is][out] */ CORDB_ADDRESS *ptrs);

        END_INTERFACE
    } ICorDebugComObjectValueVtbl;

    interface ICorDebugComObjectValue
    {
        CONST_VTBL struct ICorDebugComObjectValueVtbl *lpVtbl;
    };



#ifdef COBJMACROS


#define ICorDebugComObjectValue_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) )

#define ICorDebugComObjectValue_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) )

#define ICorDebugComObjectValue_Release(This)	\
    ( (This)->lpVtbl -> Release(This) )


#define ICorDebugComObjectValue_GetCachedInterfaceTypes(This,bIInspectableOnly,ppInterfacesEnum)	\
    ( (This)->lpVtbl -> GetCachedInterfaceTypes(This,bIInspectableOnly,ppInterfacesEnum) )

#define ICorDebugComObjectValue_GetCachedInterfacePointers(This,bIInspectableOnly,celt,pcEltFetched,ptrs)	\
    ( (This)->lpVtbl -> GetCachedInterfacePointers(This,bIInspectableOnly,celt,pcEltFetched,ptrs) )

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ICorDebugComObjectValue_INTERFACE_DEFINED__ */


#ifndef __ICorDebugObjectEnum_INTERFACE_DEFINED__
#define __ICorDebugObjectEnum_INTERFACE_DEFINED__

/* interface ICorDebugObjectEnum */
/* [unique][uuid][local][object] */


EXTERN_C const IID IID_ICorDebugObjectEnum;

#if defined(__cplusplus) && !defined(CINTERFACE)

    MIDL_INTERFACE("CC7BCB02-8A68-11d2-983C-0000F808342D")
    ICorDebugObjectEnum : public ICorDebugEnum
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE Next(
            /* [in] */ ULONG celt,
            /* [length_is][size_is][out] */ CORDB_ADDRESS objects[  ],
            /* [out] */ ULONG *pceltFetched) = 0;

    };


#else 	/* C style interface */

    typedef struct ICorDebugObjectEnumVtbl
    {
        BEGIN_INTERFACE

        HRESULT ( STDMETHODCALLTYPE *QueryInterface )(
            ICorDebugObjectEnum * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */
            _COM_Outptr_  void **ppvObject);

        ULONG ( STDMETHODCALLTYPE *AddRef )(
            ICorDebugObjectEnum * This);

        ULONG ( STDMETHODCALLTYPE *Release )(
            ICorDebugObjectEnum * This);

        HRESULT ( STDMETHODCALLTYPE *Skip )(
            ICorDebugObjectEnum * This,
            /* [in] */ ULONG celt);

        HRESULT ( STDMETHODCALLTYPE *Reset )(
            ICorDebugObjectEnum * This);

        HRESULT ( STDMETHODCALLTYPE *Clone )(
            ICorDebugObjectEnum * This,
            /* [out] */ ICorDebugEnum **ppEnum);

        HRESULT ( STDMETHODCALLTYPE *GetCount )(
            ICorDebugObjectEnum * This,
            /* [out] */ ULONG *pcelt);

        HRESULT ( STDMETHODCALLTYPE *Next )(
            ICorDebugObjectEnum * This,
            /* [in] */ ULONG celt,
            /* [length_is][size_is][out] */ CORDB_ADDRESS objects[  ],
            /* [out] */ ULONG *pceltFetched);

        END_INTERFACE
    } ICorDebugObjectEnumVtbl;

    interface ICorDebugObjectEnum
    {
        CONST_VTBL struct ICorDebugObjectEnumVtbl *lpVtbl;
    };



#ifdef COBJMACROS


#define ICorDebugObjectEnum_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) )

#define ICorDebugObjectEnum_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) )

#define ICorDebugObjectEnum_Release(This)	\
    ( (This)->lpVtbl -> Release(This) )


#define ICorDebugObjectEnum_Skip(This,celt)	\
    ( (This)->lpVtbl -> Skip(This,celt) )

#define ICorDebugObjectEnum_Reset(This)	\
    ( (This)->lpVtbl -> Reset(This) )

#define ICorDebugObjectEnum_Clone(This,ppEnum)	\
    ( (This)->lpVtbl -> Clone(This,ppEnum) )

#define ICorDebugObjectEnum_GetCount(This,pcelt)	\
    ( (This)->lpVtbl -> GetCount(This,pcelt) )


#define ICorDebugObjectEnum_Next(This,celt,objects,pceltFetched)	\
    ( (This)->lpVtbl -> Next(This,celt,objects,pceltFetched) )

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ICorDebugObjectEnum_INTERFACE_DEFINED__ */


#ifndef __ICorDebugBreakpointEnum_INTERFACE_DEFINED__
#define __ICorDebugBreakpointEnum_INTERFACE_DEFINED__

/* interface ICorDebugBreakpointEnum */
/* [unique][uuid][local][object] */


EXTERN_C const IID IID_ICorDebugBreakpointEnum;

#if defined(__cplusplus) && !defined(CINTERFACE)

    MIDL_INTERFACE("CC7BCB03-8A68-11d2-983C-0000F808342D")
    ICorDebugBreakpointEnum : public ICorDebugEnum
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE Next(
            /* [in] */ ULONG celt,
            /* [length_is][size_is][out] */ ICorDebugBreakpoint *breakpoints[  ],
            /* [out] */ ULONG *pceltFetched) = 0;

    };


#else 	/* C style interface */

    typedef struct ICorDebugBreakpointEnumVtbl
    {
        BEGIN_INTERFACE

        HRESULT ( STDMETHODCALLTYPE *QueryInterface )(
            ICorDebugBreakpointEnum * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */
            _COM_Outptr_  void **ppvObject);

        ULONG ( STDMETHODCALLTYPE *AddRef )(
            ICorDebugBreakpointEnum * This);

        ULONG ( STDMETHODCALLTYPE *Release )(
            ICorDebugBreakpointEnum * This);

        HRESULT ( STDMETHODCALLTYPE *Skip )(
            ICorDebugBreakpointEnum * This,
            /* [in] */ ULONG celt);

        HRESULT ( STDMETHODCALLTYPE *Reset )(
            ICorDebugBreakpointEnum * This);

        HRESULT ( STDMETHODCALLTYPE *Clone )(
            ICorDebugBreakpointEnum * This,
            /* [out] */ ICorDebugEnum **ppEnum);

        HRESULT ( STDMETHODCALLTYPE *GetCount )(
            ICorDebugBreakpointEnum * This,
            /* [out] */ ULONG *pcelt);

        HRESULT ( STDMETHODCALLTYPE *Next )(
            ICorDebugBreakpointEnum * This,
            /* [in] */ ULONG celt,
            /* [length_is][size_is][out] */ ICorDebugBreakpoint *breakpoints[  ],
            /* [out] */ ULONG *pceltFetched);

        END_INTERFACE
    } ICorDebugBreakpointEnumVtbl;

    interface ICorDebugBreakpointEnum
    {
        CONST_VTBL struct ICorDebugBreakpointEnumVtbl *lpVtbl;
    };



#ifdef COBJMACROS


#define ICorDebugBreakpointEnum_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) )

#define ICorDebugBreakpointEnum_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) )

#define ICorDebugBreakpointEnum_Release(This)	\
    ( (This)->lpVtbl -> Release(This) )


#define ICorDebugBreakpointEnum_Skip(This,celt)	\
    ( (This)->lpVtbl -> Skip(This,celt) )

#define ICorDebugBreakpointEnum_Reset(This)	\
    ( (This)->lpVtbl -> Reset(This) )

#define ICorDebugBreakpointEnum_Clone(This,ppEnum)	\
    ( (This)->lpVtbl -> Clone(This,ppEnum) )

#define ICorDebugBreakpointEnum_GetCount(This,pcelt)	\
    ( (This)->lpVtbl -> GetCount(This,pcelt) )


#define ICorDebugBreakpointEnum_Next(This,celt,breakpoints,pceltFetched)	\
    ( (This)->lpVtbl -> Next(This,celt,breakpoints,pceltFetched) )

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ICorDebugBreakpointEnum_INTERFACE_DEFINED__ */


#ifndef __ICorDebugStepperEnum_INTERFACE_DEFINED__
#define __ICorDebugStepperEnum_INTERFACE_DEFINED__

/* interface ICorDebugStepperEnum */
/* [unique][uuid][local][object] */


EXTERN_C const IID IID_ICorDebugStepperEnum;

#if defined(__cplusplus) && !defined(CINTERFACE)

    MIDL_INTERFACE("CC7BCB04-8A68-11d2-983C-0000F808342D")
    ICorDebugStepperEnum : public ICorDebugEnum
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE Next(
            /* [in] */ ULONG celt,
            /* [length_is][size_is][out] */ ICorDebugStepper *steppers[  ],
            /* [out] */ ULONG *pceltFetched) = 0;

    };


#else 	/* C style interface */

    typedef struct ICorDebugStepperEnumVtbl
    {
        BEGIN_INTERFACE

        HRESULT ( STDMETHODCALLTYPE *QueryInterface )(
            ICorDebugStepperEnum * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */
            _COM_Outptr_  void **ppvObject);

        ULONG ( STDMETHODCALLTYPE *AddRef )(
            ICorDebugStepperEnum * This);

        ULONG ( STDMETHODCALLTYPE *Release )(
            ICorDebugStepperEnum * This);

        HRESULT ( STDMETHODCALLTYPE *Skip )(
            ICorDebugStepperEnum * This,
            /* [in] */ ULONG celt);

        HRESULT ( STDMETHODCALLTYPE *Reset )(
            ICorDebugStepperEnum * This);

        HRESULT ( STDMETHODCALLTYPE *Clone )(
            ICorDebugStepperEnum * This,
            /* [out] */ ICorDebugEnum **ppEnum);

        HRESULT ( STDMETHODCALLTYPE *GetCount )(
            ICorDebugStepperEnum * This,
            /* [out] */ ULONG *pcelt);

        HRESULT ( STDMETHODCALLTYPE *Next )(
            ICorDebugStepperEnum * This,
            /* [in] */ ULONG celt,
            /* [length_is][size_is][out] */ ICorDebugStepper *steppers[  ],
            /* [out] */ ULONG *pceltFetched);

        END_INTERFACE
    } ICorDebugStepperEnumVtbl;

    interface ICorDebugStepperEnum
    {
        CONST_VTBL struct ICorDebugStepperEnumVtbl *lpVtbl;
    };



#ifdef COBJMACROS


#define ICorDebugStepperEnum_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) )

#define ICorDebugStepperEnum_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) )

#define ICorDebugStepperEnum_Release(This)	\
    ( (This)->lpVtbl -> Release(This) )


#define ICorDebugStepperEnum_Skip(This,celt)	\
    ( (This)->lpVtbl -> Skip(This,celt) )

#define ICorDebugStepperEnum_Reset(This)	\
    ( (This)->lpVtbl -> Reset(This) )

#define ICorDebugStepperEnum_Clone(This,ppEnum)	\
    ( (This)->lpVtbl -> Clone(This,ppEnum) )

#define ICorDebugStepperEnum_GetCount(This,pcelt)	\
    ( (This)->lpVtbl -> GetCount(This,pcelt) )


#define ICorDebugStepperEnum_Next(This,celt,steppers,pceltFetched)	\
    ( (This)->lpVtbl -> Next(This,celt,steppers,pceltFetched) )

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ICorDebugStepperEnum_INTERFACE_DEFINED__ */


#ifndef __ICorDebugProcessEnum_INTERFACE_DEFINED__
#define __ICorDebugProcessEnum_INTERFACE_DEFINED__

/* interface ICorDebugProcessEnum */
/* [unique][uuid][local][object] */


EXTERN_C const IID IID_ICorDebugProcessEnum;

#if defined(__cplusplus) && !defined(CINTERFACE)

    MIDL_INTERFACE("CC7BCB05-8A68-11d2-983C-0000F808342D")
    ICorDebugProcessEnum : public ICorDebugEnum
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE Next(
            /* [in] */ ULONG celt,
            /* [length_is][size_is][out] */ ICorDebugProcess *processes[  ],
            /* [out] */ ULONG *pceltFetched) = 0;

    };


#else 	/* C style interface */

    typedef struct ICorDebugProcessEnumVtbl
    {
        BEGIN_INTERFACE

        HRESULT ( STDMETHODCALLTYPE *QueryInterface )(
            ICorDebugProcessEnum * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */
            _COM_Outptr_  void **ppvObject);

        ULONG ( STDMETHODCALLTYPE *AddRef )(
            ICorDebugProcessEnum * This);

        ULONG ( STDMETHODCALLTYPE *Release )(
            ICorDebugProcessEnum * This);

        HRESULT ( STDMETHODCALLTYPE *Skip )(
            ICorDebugProcessEnum * This,
            /* [in] */ ULONG celt);

        HRESULT ( STDMETHODCALLTYPE *Reset )(
            ICorDebugProcessEnum * This);

        HRESULT ( STDMETHODCALLTYPE *Clone )(
            ICorDebugProcessEnum * This,
            /* [out] */ ICorDebugEnum **ppEnum);

        HRESULT ( STDMETHODCALLTYPE *GetCount )(
            ICorDebugProcessEnum * This,
            /* [out] */ ULONG *pcelt);

        HRESULT ( STDMETHODCALLTYPE *Next )(
            ICorDebugProcessEnum * This,
            /* [in] */ ULONG celt,
            /* [length_is][size_is][out] */ ICorDebugProcess *processes[  ],
            /* [out] */ ULONG *pceltFetched);

        END_INTERFACE
    } ICorDebugProcessEnumVtbl;

    interface ICorDebugProcessEnum
    {
        CONST_VTBL struct ICorDebugProcessEnumVtbl *lpVtbl;
    };



#ifdef COBJMACROS


#define ICorDebugProcessEnum_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) )

#define ICorDebugProcessEnum_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) )

#define ICorDebugProcessEnum_Release(This)	\
    ( (This)->lpVtbl -> Release(This) )


#define ICorDebugProcessEnum_Skip(This,celt)	\
    ( (This)->lpVtbl -> Skip(This,celt) )

#define ICorDebugProcessEnum_Reset(This)	\
    ( (This)->lpVtbl -> Reset(This) )

#define ICorDebugProcessEnum_Clone(This,ppEnum)	\
    ( (This)->lpVtbl -> Clone(This,ppEnum) )

#define ICorDebugProcessEnum_GetCount(This,pcelt)	\
    ( (This)->lpVtbl -> GetCount(This,pcelt) )


#define ICorDebugProcessEnum_Next(This,celt,processes,pceltFetched)	\
    ( (This)->lpVtbl -> Next(This,celt,processes,pceltFetched) )

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ICorDebugProcessEnum_INTERFACE_DEFINED__ */


#ifndef __ICorDebugThreadEnum_INTERFACE_DEFINED__
#define __ICorDebugThreadEnum_INTERFACE_DEFINED__

/* interface ICorDebugThreadEnum */
/* [unique][uuid][local][object] */


EXTERN_C const IID IID_ICorDebugThreadEnum;

#if defined(__cplusplus) && !defined(CINTERFACE)

    MIDL_INTERFACE("CC7BCB06-8A68-11d2-983C-0000F808342D")
    ICorDebugThreadEnum : public ICorDebugEnum
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE Next(
            /* [in] */ ULONG celt,
            /* [length_is][size_is][out] */ ICorDebugThread *threads[  ],
            /* [out] */ ULONG *pceltFetched) = 0;

    };


#else 	/* C style interface */

    typedef struct ICorDebugThreadEnumVtbl
    {
        BEGIN_INTERFACE

        HRESULT ( STDMETHODCALLTYPE *QueryInterface )(
            ICorDebugThreadEnum * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */
            _COM_Outptr_  void **ppvObject);

        ULONG ( STDMETHODCALLTYPE *AddRef )(
            ICorDebugThreadEnum * This);

        ULONG ( STDMETHODCALLTYPE *Release )(
            ICorDebugThreadEnum * This);

        HRESULT ( STDMETHODCALLTYPE *Skip )(
            ICorDebugThreadEnum * This,
            /* [in] */ ULONG celt);

        HRESULT ( STDMETHODCALLTYPE *Reset )(
            ICorDebugThreadEnum * This);

        HRESULT ( STDMETHODCALLTYPE *Clone )(
            ICorDebugThreadEnum * This,
            /* [out] */ ICorDebugEnum **ppEnum);

        HRESULT ( STDMETHODCALLTYPE *GetCount )(
            ICorDebugThreadEnum * This,
            /* [out] */ ULONG *pcelt);

        HRESULT ( STDMETHODCALLTYPE *Next )(
            ICorDebugThreadEnum * This,
            /* [in] */ ULONG celt,
            /* [length_is][size_is][out] */ ICorDebugThread *threads[  ],
            /* [out] */ ULONG *pceltFetched);

        END_INTERFACE
    } ICorDebugThreadEnumVtbl;

    interface ICorDebugThreadEnum
    {
        CONST_VTBL struct ICorDebugThreadEnumVtbl *lpVtbl;
    };



#ifdef COBJMACROS


#define ICorDebugThreadEnum_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) )

#define ICorDebugThreadEnum_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) )

#define ICorDebugThreadEnum_Release(This)	\
    ( (This)->lpVtbl -> Release(This) )


#define ICorDebugThreadEnum_Skip(This,celt)	\
    ( (This)->lpVtbl -> Skip(This,celt) )

#define ICorDebugThreadEnum_Reset(This)	\
    ( (This)->lpVtbl -> Reset(This) )

#define ICorDebugThreadEnum_Clone(This,ppEnum)	\
    ( (This)->lpVtbl -> Clone(This,ppEnum) )

#define ICorDebugThreadEnum_GetCount(This,pcelt)	\
    ( (This)->lpVtbl -> GetCount(This,pcelt) )


#define ICorDebugThreadEnum_Next(This,celt,threads,pceltFetched)	\
    ( (This)->lpVtbl -> Next(This,celt,threads,pceltFetched) )

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ICorDebugThreadEnum_INTERFACE_DEFINED__ */


#ifndef __ICorDebugFrameEnum_INTERFACE_DEFINED__
#define __ICorDebugFrameEnum_INTERFACE_DEFINED__

/* interface ICorDebugFrameEnum */
/* [unique][uuid][local][object] */


EXTERN_C const IID IID_ICorDebugFrameEnum;

#if defined(__cplusplus) && !defined(CINTERFACE)

    MIDL_INTERFACE("CC7BCB07-8A68-11d2-983C-0000F808342D")
    ICorDebugFrameEnum : public ICorDebugEnum
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE Next(
            /* [in] */ ULONG celt,
            /* [length_is][size_is][out] */ ICorDebugFrame *frames[  ],
            /* [out] */ ULONG *pceltFetched) = 0;

    };


#else 	/* C style interface */

    typedef struct ICorDebugFrameEnumVtbl
    {
        BEGIN_INTERFACE

        HRESULT ( STDMETHODCALLTYPE *QueryInterface )(
            ICorDebugFrameEnum * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */
            _COM_Outptr_  void **ppvObject);

        ULONG ( STDMETHODCALLTYPE *AddRef )(
            ICorDebugFrameEnum * This);

        ULONG ( STDMETHODCALLTYPE *Release )(
            ICorDebugFrameEnum * This);

        HRESULT ( STDMETHODCALLTYPE *Skip )(
            ICorDebugFrameEnum * This,
            /* [in] */ ULONG celt);

        HRESULT ( STDMETHODCALLTYPE *Reset )(
            ICorDebugFrameEnum * This);

        HRESULT ( STDMETHODCALLTYPE *Clone )(
            ICorDebugFrameEnum * This,
            /* [out] */ ICorDebugEnum **ppEnum);

        HRESULT ( STDMETHODCALLTYPE *GetCount )(
            ICorDebugFrameEnum * This,
            /* [out] */ ULONG *pcelt);

        HRESULT ( STDMETHODCALLTYPE *Next )(
            ICorDebugFrameEnum * This,
            /* [in] */ ULONG celt,
            /* [length_is][size_is][out] */ ICorDebugFrame *frames[  ],
            /* [out] */ ULONG *pceltFetched);

        END_INTERFACE
    } ICorDebugFrameEnumVtbl;

    interface ICorDebugFrameEnum
    {
        CONST_VTBL struct ICorDebugFrameEnumVtbl *lpVtbl;
    };



#ifdef COBJMACROS


#define ICorDebugFrameEnum_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) )

#define ICorDebugFrameEnum_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) )

#define ICorDebugFrameEnum_Release(This)	\
    ( (This)->lpVtbl -> Release(This) )


#define ICorDebugFrameEnum_Skip(This,celt)	\
    ( (This)->lpVtbl -> Skip(This,celt) )

#define ICorDebugFrameEnum_Reset(This)	\
    ( (This)->lpVtbl -> Reset(This) )

#define ICorDebugFrameEnum_Clone(This,ppEnum)	\
    ( (This)->lpVtbl -> Clone(This,ppEnum) )

#define ICorDebugFrameEnum_GetCount(This,pcelt)	\
    ( (This)->lpVtbl -> GetCount(This,pcelt) )


#define ICorDebugFrameEnum_Next(This,celt,frames,pceltFetched)	\
    ( (This)->lpVtbl -> Next(This,celt,frames,pceltFetched) )

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ICorDebugFrameEnum_INTERFACE_DEFINED__ */


#ifndef __ICorDebugChainEnum_INTERFACE_DEFINED__
#define __ICorDebugChainEnum_INTERFACE_DEFINED__

/* interface ICorDebugChainEnum */
/* [unique][uuid][local][object] */


EXTERN_C const IID IID_ICorDebugChainEnum;

#if defined(__cplusplus) && !defined(CINTERFACE)

    MIDL_INTERFACE("CC7BCB08-8A68-11d2-983C-0000F808342D")
    ICorDebugChainEnum : public ICorDebugEnum
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE Next(
            /* [in] */ ULONG celt,
            /* [length_is][size_is][out] */ ICorDebugChain *chains[  ],
            /* [out] */ ULONG *pceltFetched) = 0;

    };


#else 	/* C style interface */

    typedef struct ICorDebugChainEnumVtbl
    {
        BEGIN_INTERFACE

        HRESULT ( STDMETHODCALLTYPE *QueryInterface )(
            ICorDebugChainEnum * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */
            _COM_Outptr_  void **ppvObject);

        ULONG ( STDMETHODCALLTYPE *AddRef )(
            ICorDebugChainEnum * This);

        ULONG ( STDMETHODCALLTYPE *Release )(
            ICorDebugChainEnum * This);

        HRESULT ( STDMETHODCALLTYPE *Skip )(
            ICorDebugChainEnum * This,
            /* [in] */ ULONG celt);

        HRESULT ( STDMETHODCALLTYPE *Reset )(
            ICorDebugChainEnum * This);

        HRESULT ( STDMETHODCALLTYPE *Clone )(
            ICorDebugChainEnum * This,
            /* [out] */ ICorDebugEnum **ppEnum);

        HRESULT ( STDMETHODCALLTYPE *GetCount )(
            ICorDebugChainEnum * This,
            /* [out] */ ULONG *pcelt);

        HRESULT ( STDMETHODCALLTYPE *Next )(
            ICorDebugChainEnum * This,
            /* [in] */ ULONG celt,
            /* [length_is][size_is][out] */ ICorDebugChain *chains[  ],
            /* [out] */ ULONG *pceltFetched);

        END_INTERFACE
    } ICorDebugChainEnumVtbl;

    interface ICorDebugChainEnum
    {
        CONST_VTBL struct ICorDebugChainEnumVtbl *lpVtbl;
    };



#ifdef COBJMACROS


#define ICorDebugChainEnum_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) )

#define ICorDebugChainEnum_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) )

#define ICorDebugChainEnum_Release(This)	\
    ( (This)->lpVtbl -> Release(This) )


#define ICorDebugChainEnum_Skip(This,celt)	\
    ( (This)->lpVtbl -> Skip(This,celt) )

#define ICorDebugChainEnum_Reset(This)	\
    ( (This)->lpVtbl -> Reset(This) )

#define ICorDebugChainEnum_Clone(This,ppEnum)	\
    ( (This)->lpVtbl -> Clone(This,ppEnum) )

#define ICorDebugChainEnum_GetCount(This,pcelt)	\
    ( (This)->lpVtbl -> GetCount(This,pcelt) )


#define ICorDebugChainEnum_Next(This,celt,chains,pceltFetched)	\
    ( (This)->lpVtbl -> Next(This,celt,chains,pceltFetched) )

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ICorDebugChainEnum_INTERFACE_DEFINED__ */


#ifndef __ICorDebugModuleEnum_INTERFACE_DEFINED__
#define __ICorDebugModuleEnum_INTERFACE_DEFINED__

/* interface ICorDebugModuleEnum */
/* [unique][uuid][local][object] */


EXTERN_C const IID IID_ICorDebugModuleEnum;

#if defined(__cplusplus) && !defined(CINTERFACE)

    MIDL_INTERFACE("CC7BCB09-8A68-11d2-983C-0000F808342D")
    ICorDebugModuleEnum : public ICorDebugEnum
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE Next(
            /* [in] */ ULONG celt,
            /* [length_is][size_is][out] */ ICorDebugModule *modules[  ],
            /* [out] */ ULONG *pceltFetched) = 0;

    };


#else 	/* C style interface */

    typedef struct ICorDebugModuleEnumVtbl
    {
        BEGIN_INTERFACE

        HRESULT ( STDMETHODCALLTYPE *QueryInterface )(
            ICorDebugModuleEnum * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */
            _COM_Outptr_  void **ppvObject);

        ULONG ( STDMETHODCALLTYPE *AddRef )(
            ICorDebugModuleEnum * This);

        ULONG ( STDMETHODCALLTYPE *Release )(
            ICorDebugModuleEnum * This);

        HRESULT ( STDMETHODCALLTYPE *Skip )(
            ICorDebugModuleEnum * This,
            /* [in] */ ULONG celt);

        HRESULT ( STDMETHODCALLTYPE *Reset )(
            ICorDebugModuleEnum * This);

        HRESULT ( STDMETHODCALLTYPE *Clone )(
            ICorDebugModuleEnum * This,
            /* [out] */ ICorDebugEnum **ppEnum);

        HRESULT ( STDMETHODCALLTYPE *GetCount )(
            ICorDebugModuleEnum * This,
            /* [out] */ ULONG *pcelt);

        HRESULT ( STDMETHODCALLTYPE *Next )(
            ICorDebugModuleEnum * This,
            /* [in] */ ULONG celt,
            /* [length_is][size_is][out] */ ICorDebugModule *modules[  ],
            /* [out] */ ULONG *pceltFetched);

        END_INTERFACE
    } ICorDebugModuleEnumVtbl;

    interface ICorDebugModuleEnum
    {
        CONST_VTBL struct ICorDebugModuleEnumVtbl *lpVtbl;
    };



#ifdef COBJMACROS


#define ICorDebugModuleEnum_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) )

#define ICorDebugModuleEnum_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) )

#define ICorDebugModuleEnum_Release(This)	\
    ( (This)->lpVtbl -> Release(This) )


#define ICorDebugModuleEnum_Skip(This,celt)	\
    ( (This)->lpVtbl -> Skip(This,celt) )

#define ICorDebugModuleEnum_Reset(This)	\
    ( (This)->lpVtbl -> Reset(This) )

#define ICorDebugModuleEnum_Clone(This,ppEnum)	\
    ( (This)->lpVtbl -> Clone(This,ppEnum) )

#define ICorDebugModuleEnum_GetCount(This,pcelt)	\
    ( (This)->lpVtbl -> GetCount(This,pcelt) )


#define ICorDebugModuleEnum_Next(This,celt,modules,pceltFetched)	\
    ( (This)->lpVtbl -> Next(This,celt,modules,pceltFetched) )

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ICorDebugModuleEnum_INTERFACE_DEFINED__ */


#ifndef __ICorDebugValueEnum_INTERFACE_DEFINED__
#define __ICorDebugValueEnum_INTERFACE_DEFINED__

/* interface ICorDebugValueEnum */
/* [unique][uuid][local][object] */


EXTERN_C const IID IID_ICorDebugValueEnum;

#if defined(__cplusplus) && !defined(CINTERFACE)

    MIDL_INTERFACE("CC7BCB0A-8A68-11d2-983C-0000F808342D")
    ICorDebugValueEnum : public ICorDebugEnum
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE Next(
            /* [in] */ ULONG celt,
            /* [length_is][size_is][out] */ ICorDebugValue *values[  ],
            /* [out] */ ULONG *pceltFetched) = 0;

    };


#else 	/* C style interface */

    typedef struct ICorDebugValueEnumVtbl
    {
        BEGIN_INTERFACE

        HRESULT ( STDMETHODCALLTYPE *QueryInterface )(
            ICorDebugValueEnum * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */
            _COM_Outptr_  void **ppvObject);

        ULONG ( STDMETHODCALLTYPE *AddRef )(
            ICorDebugValueEnum * This);

        ULONG ( STDMETHODCALLTYPE *Release )(
            ICorDebugValueEnum * This);

        HRESULT ( STDMETHODCALLTYPE *Skip )(
            ICorDebugValueEnum * This,
            /* [in] */ ULONG celt);

        HRESULT ( STDMETHODCALLTYPE *Reset )(
            ICorDebugValueEnum * This);

        HRESULT ( STDMETHODCALLTYPE *Clone )(
            ICorDebugValueEnum * This,
            /* [out] */ ICorDebugEnum **ppEnum);

        HRESULT ( STDMETHODCALLTYPE *GetCount )(
            ICorDebugValueEnum * This,
            /* [out] */ ULONG *pcelt);

        HRESULT ( STDMETHODCALLTYPE *Next )(
            ICorDebugValueEnum * This,
            /* [in] */ ULONG celt,
            /* [length_is][size_is][out] */ ICorDebugValue *values[  ],
            /* [out] */ ULONG *pceltFetched);

        END_INTERFACE
    } ICorDebugValueEnumVtbl;

    interface ICorDebugValueEnum
    {
        CONST_VTBL struct ICorDebugValueEnumVtbl *lpVtbl;
    };



#ifdef COBJMACROS


#define ICorDebugValueEnum_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) )

#define ICorDebugValueEnum_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) )

#define ICorDebugValueEnum_Release(This)	\
    ( (This)->lpVtbl -> Release(This) )


#define ICorDebugValueEnum_Skip(This,celt)	\
    ( (This)->lpVtbl -> Skip(This,celt) )

#define ICorDebugValueEnum_Reset(This)	\
    ( (This)->lpVtbl -> Reset(This) )

#define ICorDebugValueEnum_Clone(This,ppEnum)	\
    ( (This)->lpVtbl -> Clone(This,ppEnum) )

#define ICorDebugValueEnum_GetCount(This,pcelt)	\
    ( (This)->lpVtbl -> GetCount(This,pcelt) )


#define ICorDebugValueEnum_Next(This,celt,values,pceltFetched)	\
    ( (This)->lpVtbl -> Next(This,celt,values,pceltFetched) )

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ICorDebugValueEnum_INTERFACE_DEFINED__ */


#ifndef __ICorDebugVariableHomeEnum_INTERFACE_DEFINED__
#define __ICorDebugVariableHomeEnum_INTERFACE_DEFINED__

/* interface ICorDebugVariableHomeEnum */
/* [unique][uuid][local][object] */


EXTERN_C const IID IID_ICorDebugVariableHomeEnum;

#if defined(__cplusplus) && !defined(CINTERFACE)

    MIDL_INTERFACE("e76b7a57-4f7a-4309-85a7-5d918c3deaf7")
    ICorDebugVariableHomeEnum : public ICorDebugEnum
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE Next(
            /* [in] */ ULONG celt,
            /* [length_is][size_is][out] */ ICorDebugVariableHome *homes[  ],
            /* [out] */ ULONG *pceltFetched) = 0;

    };


#else 	/* C style interface */

    typedef struct ICorDebugVariableHomeEnumVtbl
    {
        BEGIN_INTERFACE

        HRESULT ( STDMETHODCALLTYPE *QueryInterface )(
            ICorDebugVariableHomeEnum * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */
            _COM_Outptr_  void **ppvObject);

        ULONG ( STDMETHODCALLTYPE *AddRef )(
            ICorDebugVariableHomeEnum * This);

        ULONG ( STDMETHODCALLTYPE *Release )(
            ICorDebugVariableHomeEnum * This);

        HRESULT ( STDMETHODCALLTYPE *Skip )(
            ICorDebugVariableHomeEnum * This,
            /* [in] */ ULONG celt);

        HRESULT ( STDMETHODCALLTYPE *Reset )(
            ICorDebugVariableHomeEnum * This);

        HRESULT ( STDMETHODCALLTYPE *Clone )(
            ICorDebugVariableHomeEnum * This,
            /* [out] */ ICorDebugEnum **ppEnum);

        HRESULT ( STDMETHODCALLTYPE *GetCount )(
            ICorDebugVariableHomeEnum * This,
            /* [out] */ ULONG *pcelt);

        HRESULT ( STDMETHODCALLTYPE *Next )(
            ICorDebugVariableHomeEnum * This,
            /* [in] */ ULONG celt,
            /* [length_is][size_is][out] */ ICorDebugVariableHome *homes[  ],
            /* [out] */ ULONG *pceltFetched);

        END_INTERFACE
    } ICorDebugVariableHomeEnumVtbl;

    interface ICorDebugVariableHomeEnum
    {
        CONST_VTBL struct ICorDebugVariableHomeEnumVtbl *lpVtbl;
    };



#ifdef COBJMACROS


#define ICorDebugVariableHomeEnum_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) )

#define ICorDebugVariableHomeEnum_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) )

#define ICorDebugVariableHomeEnum_Release(This)	\
    ( (This)->lpVtbl -> Release(This) )


#define ICorDebugVariableHomeEnum_Skip(This,celt)	\
    ( (This)->lpVtbl -> Skip(This,celt) )

#define ICorDebugVariableHomeEnum_Reset(This)	\
    ( (This)->lpVtbl -> Reset(This) )

#define ICorDebugVariableHomeEnum_Clone(This,ppEnum)	\
    ( (This)->lpVtbl -> Clone(This,ppEnum) )

#define ICorDebugVariableHomeEnum_GetCount(This,pcelt)	\
    ( (This)->lpVtbl -> GetCount(This,pcelt) )


#define ICorDebugVariableHomeEnum_Next(This,celt,homes,pceltFetched)	\
    ( (This)->lpVtbl -> Next(This,celt,homes,pceltFetched) )

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ICorDebugVariableHomeEnum_INTERFACE_DEFINED__ */


#ifndef __ICorDebugCodeEnum_INTERFACE_DEFINED__
#define __ICorDebugCodeEnum_INTERFACE_DEFINED__

/* interface ICorDebugCodeEnum */
/* [unique][uuid][local][object] */


EXTERN_C const IID IID_ICorDebugCodeEnum;

#if defined(__cplusplus) && !defined(CINTERFACE)

    MIDL_INTERFACE("55E96461-9645-45e4-A2FF-0367877ABCDE")
    ICorDebugCodeEnum : public ICorDebugEnum
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE Next(
            /* [in] */ ULONG celt,
            /* [length_is][size_is][out] */ ICorDebugCode *values[  ],
            /* [out] */ ULONG *pceltFetched) = 0;

    };


#else 	/* C style interface */

    typedef struct ICorDebugCodeEnumVtbl
    {
        BEGIN_INTERFACE

        HRESULT ( STDMETHODCALLTYPE *QueryInterface )(
            ICorDebugCodeEnum * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */
            _COM_Outptr_  void **ppvObject);

        ULONG ( STDMETHODCALLTYPE *AddRef )(
            ICorDebugCodeEnum * This);

        ULONG ( STDMETHODCALLTYPE *Release )(
            ICorDebugCodeEnum * This);

        HRESULT ( STDMETHODCALLTYPE *Skip )(
            ICorDebugCodeEnum * This,
            /* [in] */ ULONG celt);

        HRESULT ( STDMETHODCALLTYPE *Reset )(
            ICorDebugCodeEnum * This);

        HRESULT ( STDMETHODCALLTYPE *Clone )(
            ICorDebugCodeEnum * This,
            /* [out] */ ICorDebugEnum **ppEnum);

        HRESULT ( STDMETHODCALLTYPE *GetCount )(
            ICorDebugCodeEnum * This,
            /* [out] */ ULONG *pcelt);

        HRESULT ( STDMETHODCALLTYPE *Next )(
            ICorDebugCodeEnum * This,
            /* [in] */ ULONG celt,
            /* [length_is][size_is][out] */ ICorDebugCode *values[  ],
            /* [out] */ ULONG *pceltFetched);

        END_INTERFACE
    } ICorDebugCodeEnumVtbl;

    interface ICorDebugCodeEnum
    {
        CONST_VTBL struct ICorDebugCodeEnumVtbl *lpVtbl;
    };



#ifdef COBJMACROS


#define ICorDebugCodeEnum_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) )

#define ICorDebugCodeEnum_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) )

#define ICorDebugCodeEnum_Release(This)	\
    ( (This)->lpVtbl -> Release(This) )


#define ICorDebugCodeEnum_Skip(This,celt)	\
    ( (This)->lpVtbl -> Skip(This,celt) )

#define ICorDebugCodeEnum_Reset(This)	\
    ( (This)->lpVtbl -> Reset(This) )

#define ICorDebugCodeEnum_Clone(This,ppEnum)	\
    ( (This)->lpVtbl -> Clone(This,ppEnum) )

#define ICorDebugCodeEnum_GetCount(This,pcelt)	\
    ( (This)->lpVtbl -> GetCount(This,pcelt) )


#define ICorDebugCodeEnum_Next(This,celt,values,pceltFetched)	\
    ( (This)->lpVtbl -> Next(This,celt,values,pceltFetched) )

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ICorDebugCodeEnum_INTERFACE_DEFINED__ */


#ifndef __ICorDebugTypeEnum_INTERFACE_DEFINED__
#define __ICorDebugTypeEnum_INTERFACE_DEFINED__

/* interface ICorDebugTypeEnum */
/* [unique][uuid][local][object] */


EXTERN_C const IID IID_ICorDebugTypeEnum;

#if defined(__cplusplus) && !defined(CINTERFACE)

    MIDL_INTERFACE("10F27499-9DF2-43ce-8333-A321D7C99CB4")
    ICorDebugTypeEnum : public ICorDebugEnum
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE Next(
            /* [in] */ ULONG celt,
            /* [length_is][size_is][out] */ ICorDebugType *values[  ],
            /* [out] */ ULONG *pceltFetched) = 0;

    };


#else 	/* C style interface */

    typedef struct ICorDebugTypeEnumVtbl
    {
        BEGIN_INTERFACE

        HRESULT ( STDMETHODCALLTYPE *QueryInterface )(
            ICorDebugTypeEnum * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */
            _COM_Outptr_  void **ppvObject);

        ULONG ( STDMETHODCALLTYPE *AddRef )(
            ICorDebugTypeEnum * This);

        ULONG ( STDMETHODCALLTYPE *Release )(
            ICorDebugTypeEnum * This);

        HRESULT ( STDMETHODCALLTYPE *Skip )(
            ICorDebugTypeEnum * This,
            /* [in] */ ULONG celt);

        HRESULT ( STDMETHODCALLTYPE *Reset )(
            ICorDebugTypeEnum * This);

        HRESULT ( STDMETHODCALLTYPE *Clone )(
            ICorDebugTypeEnum * This,
            /* [out] */ ICorDebugEnum **ppEnum);

        HRESULT ( STDMETHODCALLTYPE *GetCount )(
            ICorDebugTypeEnum * This,
            /* [out] */ ULONG *pcelt);

        HRESULT ( STDMETHODCALLTYPE *Next )(
            ICorDebugTypeEnum * This,
            /* [in] */ ULONG celt,
            /* [length_is][size_is][out] */ ICorDebugType *values[  ],
            /* [out] */ ULONG *pceltFetched);

        END_INTERFACE
    } ICorDebugTypeEnumVtbl;

    interface ICorDebugTypeEnum
    {
        CONST_VTBL struct ICorDebugTypeEnumVtbl *lpVtbl;
    };



#ifdef COBJMACROS


#define ICorDebugTypeEnum_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) )

#define ICorDebugTypeEnum_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) )

#define ICorDebugTypeEnum_Release(This)	\
    ( (This)->lpVtbl -> Release(This) )


#define ICorDebugTypeEnum_Skip(This,celt)	\
    ( (This)->lpVtbl -> Skip(This,celt) )

#define ICorDebugTypeEnum_Reset(This)	\
    ( (This)->lpVtbl -> Reset(This) )

#define ICorDebugTypeEnum_Clone(This,ppEnum)	\
    ( (This)->lpVtbl -> Clone(This,ppEnum) )

#define ICorDebugTypeEnum_GetCount(This,pcelt)	\
    ( (This)->lpVtbl -> GetCount(This,pcelt) )


#define ICorDebugTypeEnum_Next(This,celt,values,pceltFetched)	\
    ( (This)->lpVtbl -> Next(This,celt,values,pceltFetched) )

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ICorDebugTypeEnum_INTERFACE_DEFINED__ */


#ifndef __ICorDebugType_INTERFACE_DEFINED__
#define __ICorDebugType_INTERFACE_DEFINED__

/* interface ICorDebugType */
/* [unique][uuid][local][object] */


EXTERN_C const IID IID_ICorDebugType;

#if defined(__cplusplus) && !defined(CINTERFACE)

    MIDL_INTERFACE("D613F0BB-ACE1-4c19-BD72-E4C08D5DA7F5")
    ICorDebugType : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE GetType(
            /* [out] */ CorElementType *ty) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetClass(
            /* [out] */ ICorDebugClass **ppClass) = 0;

        virtual HRESULT STDMETHODCALLTYPE EnumerateTypeParameters(
            /* [out] */ ICorDebugTypeEnum **ppTyParEnum) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetFirstTypeParameter(
            /* [out] */ ICorDebugType **value) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetBase(
            /* [out] */ ICorDebugType **pBase) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetStaticFieldValue(
            /* [in] */ mdFieldDef fieldDef,
            /* [in] */ ICorDebugFrame *pFrame,
            /* [out] */ ICorDebugValue **ppValue) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetRank(
            /* [out] */ ULONG32 *pnRank) = 0;

    };


#else 	/* C style interface */

    typedef struct ICorDebugTypeVtbl
    {
        BEGIN_INTERFACE

        HRESULT ( STDMETHODCALLTYPE *QueryInterface )(
            ICorDebugType * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */
            _COM_Outptr_  void **ppvObject);

        ULONG ( STDMETHODCALLTYPE *AddRef )(
            ICorDebugType * This);

        ULONG ( STDMETHODCALLTYPE *Release )(
            ICorDebugType * This);

        HRESULT ( STDMETHODCALLTYPE *GetType )(
            ICorDebugType * This,
            /* [out] */ CorElementType *ty);

        HRESULT ( STDMETHODCALLTYPE *GetClass )(
            ICorDebugType * This,
            /* [out] */ ICorDebugClass **ppClass);

        HRESULT ( STDMETHODCALLTYPE *EnumerateTypeParameters )(
            ICorDebugType * This,
            /* [out] */ ICorDebugTypeEnum **ppTyParEnum);

        HRESULT ( STDMETHODCALLTYPE *GetFirstTypeParameter )(
            ICorDebugType * This,
            /* [out] */ ICorDebugType **value);

        HRESULT ( STDMETHODCALLTYPE *GetBase )(
            ICorDebugType * This,
            /* [out] */ ICorDebugType **pBase);

        HRESULT ( STDMETHODCALLTYPE *GetStaticFieldValue )(
            ICorDebugType * This,
            /* [in] */ mdFieldDef fieldDef,
            /* [in] */ ICorDebugFrame *pFrame,
            /* [out] */ ICorDebugValue **ppValue);

        HRESULT ( STDMETHODCALLTYPE *GetRank )(
            ICorDebugType * This,
            /* [out] */ ULONG32 *pnRank);

        END_INTERFACE
    } ICorDebugTypeVtbl;

    interface ICorDebugType
    {
        CONST_VTBL struct ICorDebugTypeVtbl *lpVtbl;
    };



#ifdef COBJMACROS


#define ICorDebugType_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) )

#define ICorDebugType_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) )

#define ICorDebugType_Release(This)	\
    ( (This)->lpVtbl -> Release(This) )


#define ICorDebugType_GetType(This,ty)	\
    ( (This)->lpVtbl -> GetType(This,ty) )

#define ICorDebugType_GetClass(This,ppClass)	\
    ( (This)->lpVtbl -> GetClass(This,ppClass) )

#define ICorDebugType_EnumerateTypeParameters(This,ppTyParEnum)	\
    ( (This)->lpVtbl -> EnumerateTypeParameters(This,ppTyParEnum) )

#define ICorDebugType_GetFirstTypeParameter(This,value)	\
    ( (This)->lpVtbl -> GetFirstTypeParameter(This,value) )

#define ICorDebugType_GetBase(This,pBase)	\
    ( (This)->lpVtbl -> GetBase(This,pBase) )

#define ICorDebugType_GetStaticFieldValue(This,fieldDef,pFrame,ppValue)	\
    ( (This)->lpVtbl -> GetStaticFieldValue(This,fieldDef,pFrame,ppValue) )

#define ICorDebugType_GetRank(This,pnRank)	\
    ( (This)->lpVtbl -> GetRank(This,pnRank) )

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ICorDebugType_INTERFACE_DEFINED__ */


#ifndef __ICorDebugType2_INTERFACE_DEFINED__
#define __ICorDebugType2_INTERFACE_DEFINED__

/* interface ICorDebugType2 */
/* [unique][uuid][local][object] */


EXTERN_C const IID IID_ICorDebugType2;

#if defined(__cplusplus) && !defined(CINTERFACE)

    MIDL_INTERFACE("e6e91d79-693d-48bc-b417-8284b4f10fb5")
    ICorDebugType2 : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE GetTypeID(
            /* [out] */ COR_TYPEID *id) = 0;

    };


#else 	/* C style interface */

    typedef struct ICorDebugType2Vtbl
    {
        BEGIN_INTERFACE

        HRESULT ( STDMETHODCALLTYPE *QueryInterface )(
            ICorDebugType2 * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */
            _COM_Outptr_  void **ppvObject);

        ULONG ( STDMETHODCALLTYPE *AddRef )(
            ICorDebugType2 * This);

        ULONG ( STDMETHODCALLTYPE *Release )(
            ICorDebugType2 * This);

        HRESULT ( STDMETHODCALLTYPE *GetTypeID )(
            ICorDebugType2 * This,
            /* [out] */ COR_TYPEID *id);

        END_INTERFACE
    } ICorDebugType2Vtbl;

    interface ICorDebugType2
    {
        CONST_VTBL struct ICorDebugType2Vtbl *lpVtbl;
    };



#ifdef COBJMACROS


#define ICorDebugType2_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) )

#define ICorDebugType2_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) )

#define ICorDebugType2_Release(This)	\
    ( (This)->lpVtbl -> Release(This) )


#define ICorDebugType2_GetTypeID(This,id)	\
    ( (This)->lpVtbl -> GetTypeID(This,id) )

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ICorDebugType2_INTERFACE_DEFINED__ */


#ifndef __ICorDebugErrorInfoEnum_INTERFACE_DEFINED__
#define __ICorDebugErrorInfoEnum_INTERFACE_DEFINED__

/* interface ICorDebugErrorInfoEnum */
/* [unique][uuid][local][object] */


EXTERN_C const IID IID_ICorDebugErrorInfoEnum;

#if defined(__cplusplus) && !defined(CINTERFACE)

    MIDL_INTERFACE("F0E18809-72B5-11d2-976F-00A0C9B4D50C")
    ICorDebugErrorInfoEnum : public ICorDebugEnum
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE Next(
            /* [in] */ ULONG celt,
            /* [length_is][size_is][out] */ ICorDebugEditAndContinueErrorInfo *errors[  ],
            /* [out] */ ULONG *pceltFetched) = 0;

    };


#else 	/* C style interface */

    typedef struct ICorDebugErrorInfoEnumVtbl
    {
        BEGIN_INTERFACE

        HRESULT ( STDMETHODCALLTYPE *QueryInterface )(
            ICorDebugErrorInfoEnum * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */
            _COM_Outptr_  void **ppvObject);

        ULONG ( STDMETHODCALLTYPE *AddRef )(
            ICorDebugErrorInfoEnum * This);

        ULONG ( STDMETHODCALLTYPE *Release )(
            ICorDebugErrorInfoEnum * This);

        HRESULT ( STDMETHODCALLTYPE *Skip )(
            ICorDebugErrorInfoEnum * This,
            /* [in] */ ULONG celt);

        HRESULT ( STDMETHODCALLTYPE *Reset )(
            ICorDebugErrorInfoEnum * This);

        HRESULT ( STDMETHODCALLTYPE *Clone )(
            ICorDebugErrorInfoEnum * This,
            /* [out] */ ICorDebugEnum **ppEnum);

        HRESULT ( STDMETHODCALLTYPE *GetCount )(
            ICorDebugErrorInfoEnum * This,
            /* [out] */ ULONG *pcelt);

        HRESULT ( STDMETHODCALLTYPE *Next )(
            ICorDebugErrorInfoEnum * This,
            /* [in] */ ULONG celt,
            /* [length_is][size_is][out] */ ICorDebugEditAndContinueErrorInfo *errors[  ],
            /* [out] */ ULONG *pceltFetched);

        END_INTERFACE
    } ICorDebugErrorInfoEnumVtbl;

    interface ICorDebugErrorInfoEnum
    {
        CONST_VTBL struct ICorDebugErrorInfoEnumVtbl *lpVtbl;
    };



#ifdef COBJMACROS


#define ICorDebugErrorInfoEnum_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) )

#define ICorDebugErrorInfoEnum_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) )

#define ICorDebugErrorInfoEnum_Release(This)	\
    ( (This)->lpVtbl -> Release(This) )


#define ICorDebugErrorInfoEnum_Skip(This,celt)	\
    ( (This)->lpVtbl -> Skip(This,celt) )

#define ICorDebugErrorInfoEnum_Reset(This)	\
    ( (This)->lpVtbl -> Reset(This) )

#define ICorDebugErrorInfoEnum_Clone(This,ppEnum)	\
    ( (This)->lpVtbl -> Clone(This,ppEnum) )

#define ICorDebugErrorInfoEnum_GetCount(This,pcelt)	\
    ( (This)->lpVtbl -> GetCount(This,pcelt) )


#define ICorDebugErrorInfoEnum_Next(This,celt,errors,pceltFetched)	\
    ( (This)->lpVtbl -> Next(This,celt,errors,pceltFetched) )

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ICorDebugErrorInfoEnum_INTERFACE_DEFINED__ */


#ifndef __ICorDebugAppDomainEnum_INTERFACE_DEFINED__
#define __ICorDebugAppDomainEnum_INTERFACE_DEFINED__

/* interface ICorDebugAppDomainEnum */
/* [unique][uuid][local][object] */


EXTERN_C const IID IID_ICorDebugAppDomainEnum;

#if defined(__cplusplus) && !defined(CINTERFACE)

    MIDL_INTERFACE("63ca1b24-4359-4883-bd57-13f815f58744")
    ICorDebugAppDomainEnum : public ICorDebugEnum
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE Next(
            /* [in] */ ULONG celt,
            /* [length_is][size_is][out] */ ICorDebugAppDomain *values[  ],
            /* [out] */ ULONG *pceltFetched) = 0;

    };


#else 	/* C style interface */

    typedef struct ICorDebugAppDomainEnumVtbl
    {
        BEGIN_INTERFACE

        HRESULT ( STDMETHODCALLTYPE *QueryInterface )(
            ICorDebugAppDomainEnum * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */
            _COM_Outptr_  void **ppvObject);

        ULONG ( STDMETHODCALLTYPE *AddRef )(
            ICorDebugAppDomainEnum * This);

        ULONG ( STDMETHODCALLTYPE *Release )(
            ICorDebugAppDomainEnum * This);

        HRESULT ( STDMETHODCALLTYPE *Skip )(
            ICorDebugAppDomainEnum * This,
            /* [in] */ ULONG celt);

        HRESULT ( STDMETHODCALLTYPE *Reset )(
            ICorDebugAppDomainEnum * This);

        HRESULT ( STDMETHODCALLTYPE *Clone )(
            ICorDebugAppDomainEnum * This,
            /* [out] */ ICorDebugEnum **ppEnum);

        HRESULT ( STDMETHODCALLTYPE *GetCount )(
            ICorDebugAppDomainEnum * This,
            /* [out] */ ULONG *pcelt);

        HRESULT ( STDMETHODCALLTYPE *Next )(
            ICorDebugAppDomainEnum * This,
            /* [in] */ ULONG celt,
            /* [length_is][size_is][out] */ ICorDebugAppDomain *values[  ],
            /* [out] */ ULONG *pceltFetched);

        END_INTERFACE
    } ICorDebugAppDomainEnumVtbl;

    interface ICorDebugAppDomainEnum
    {
        CONST_VTBL struct ICorDebugAppDomainEnumVtbl *lpVtbl;
    };



#ifdef COBJMACROS


#define ICorDebugAppDomainEnum_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) )

#define ICorDebugAppDomainEnum_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) )

#define ICorDebugAppDomainEnum_Release(This)	\
    ( (This)->lpVtbl -> Release(This) )


#define ICorDebugAppDomainEnum_Skip(This,celt)	\
    ( (This)->lpVtbl -> Skip(This,celt) )

#define ICorDebugAppDomainEnum_Reset(This)	\
    ( (This)->lpVtbl -> Reset(This) )

#define ICorDebugAppDomainEnum_Clone(This,ppEnum)	\
    ( (This)->lpVtbl -> Clone(This,ppEnum) )

#define ICorDebugAppDomainEnum_GetCount(This,pcelt)	\
    ( (This)->lpVtbl -> GetCount(This,pcelt) )


#define ICorDebugAppDomainEnum_Next(This,celt,values,pceltFetched)	\
    ( (This)->lpVtbl -> Next(This,celt,values,pceltFetched) )

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ICorDebugAppDomainEnum_INTERFACE_DEFINED__ */


#ifndef __ICorDebugAssemblyEnum_INTERFACE_DEFINED__
#define __ICorDebugAssemblyEnum_INTERFACE_DEFINED__

/* interface ICorDebugAssemblyEnum */
/* [unique][uuid][local][object] */


EXTERN_C const IID IID_ICorDebugAssemblyEnum;

#if defined(__cplusplus) && !defined(CINTERFACE)

    MIDL_INTERFACE("4a2a1ec9-85ec-4bfb-9f15-a89fdfe0fe83")
    ICorDebugAssemblyEnum : public ICorDebugEnum
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE Next(
            /* [in] */ ULONG celt,
            /* [length_is][size_is][out] */ ICorDebugAssembly *values[  ],
            /* [out] */ ULONG *pceltFetched) = 0;

    };


#else 	/* C style interface */

    typedef struct ICorDebugAssemblyEnumVtbl
    {
        BEGIN_INTERFACE

        HRESULT ( STDMETHODCALLTYPE *QueryInterface )(
            ICorDebugAssemblyEnum * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */
            _COM_Outptr_  void **ppvObject);

        ULONG ( STDMETHODCALLTYPE *AddRef )(
            ICorDebugAssemblyEnum * This);

        ULONG ( STDMETHODCALLTYPE *Release )(
            ICorDebugAssemblyEnum * This);

        HRESULT ( STDMETHODCALLTYPE *Skip )(
            ICorDebugAssemblyEnum * This,
            /* [in] */ ULONG celt);

        HRESULT ( STDMETHODCALLTYPE *Reset )(
            ICorDebugAssemblyEnum * This);

        HRESULT ( STDMETHODCALLTYPE *Clone )(
            ICorDebugAssemblyEnum * This,
            /* [out] */ ICorDebugEnum **ppEnum);

        HRESULT ( STDMETHODCALLTYPE *GetCount )(
            ICorDebugAssemblyEnum * This,
            /* [out] */ ULONG *pcelt);

        HRESULT ( STDMETHODCALLTYPE *Next )(
            ICorDebugAssemblyEnum * This,
            /* [in] */ ULONG celt,
            /* [length_is][size_is][out] */ ICorDebugAssembly *values[  ],
            /* [out] */ ULONG *pceltFetched);

        END_INTERFACE
    } ICorDebugAssemblyEnumVtbl;

    interface ICorDebugAssemblyEnum
    {
        CONST_VTBL struct ICorDebugAssemblyEnumVtbl *lpVtbl;
    };



#ifdef COBJMACROS


#define ICorDebugAssemblyEnum_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) )

#define ICorDebugAssemblyEnum_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) )

#define ICorDebugAssemblyEnum_Release(This)	\
    ( (This)->lpVtbl -> Release(This) )


#define ICorDebugAssemblyEnum_Skip(This,celt)	\
    ( (This)->lpVtbl -> Skip(This,celt) )

#define ICorDebugAssemblyEnum_Reset(This)	\
    ( (This)->lpVtbl -> Reset(This) )

#define ICorDebugAssemblyEnum_Clone(This,ppEnum)	\
    ( (This)->lpVtbl -> Clone(This,ppEnum) )

#define ICorDebugAssemblyEnum_GetCount(This,pcelt)	\
    ( (This)->lpVtbl -> GetCount(This,pcelt) )


#define ICorDebugAssemblyEnum_Next(This,celt,values,pceltFetched)	\
    ( (This)->lpVtbl -> Next(This,celt,values,pceltFetched) )

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ICorDebugAssemblyEnum_INTERFACE_DEFINED__ */


#ifndef __ICorDebugBlockingObjectEnum_INTERFACE_DEFINED__
#define __ICorDebugBlockingObjectEnum_INTERFACE_DEFINED__

/* interface ICorDebugBlockingObjectEnum */
/* [unique][uuid][local][object] */


EXTERN_C const IID IID_ICorDebugBlockingObjectEnum;

#if defined(__cplusplus) && !defined(CINTERFACE)

    MIDL_INTERFACE("976A6278-134A-4a81-81A3-8F277943F4C3")
    ICorDebugBlockingObjectEnum : public ICorDebugEnum
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE Next(
            /* [in] */ ULONG celt,
            /* [length_is][size_is][out] */ CorDebugBlockingObject values[  ],
            /* [out] */ ULONG *pceltFetched) = 0;

    };


#else 	/* C style interface */

    typedef struct ICorDebugBlockingObjectEnumVtbl
    {
        BEGIN_INTERFACE

        HRESULT ( STDMETHODCALLTYPE *QueryInterface )(
            ICorDebugBlockingObjectEnum * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */
            _COM_Outptr_  void **ppvObject);

        ULONG ( STDMETHODCALLTYPE *AddRef )(
            ICorDebugBlockingObjectEnum * This);

        ULONG ( STDMETHODCALLTYPE *Release )(
            ICorDebugBlockingObjectEnum * This);

        HRESULT ( STDMETHODCALLTYPE *Skip )(
            ICorDebugBlockingObjectEnum * This,
            /* [in] */ ULONG celt);

        HRESULT ( STDMETHODCALLTYPE *Reset )(
            ICorDebugBlockingObjectEnum * This);

        HRESULT ( STDMETHODCALLTYPE *Clone )(
            ICorDebugBlockingObjectEnum * This,
            /* [out] */ ICorDebugEnum **ppEnum);

        HRESULT ( STDMETHODCALLTYPE *GetCount )(
            ICorDebugBlockingObjectEnum * This,
            /* [out] */ ULONG *pcelt);

        HRESULT ( STDMETHODCALLTYPE *Next )(
            ICorDebugBlockingObjectEnum * This,
            /* [in] */ ULONG celt,
            /* [length_is][size_is][out] */ CorDebugBlockingObject values[  ],
            /* [out] */ ULONG *pceltFetched);

        END_INTERFACE
    } ICorDebugBlockingObjectEnumVtbl;

    interface ICorDebugBlockingObjectEnum
    {
        CONST_VTBL struct ICorDebugBlockingObjectEnumVtbl *lpVtbl;
    };



#ifdef COBJMACROS


#define ICorDebugBlockingObjectEnum_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) )

#define ICorDebugBlockingObjectEnum_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) )

#define ICorDebugBlockingObjectEnum_Release(This)	\
    ( (This)->lpVtbl -> Release(This) )


#define ICorDebugBlockingObjectEnum_Skip(This,celt)	\
    ( (This)->lpVtbl -> Skip(This,celt) )

#define ICorDebugBlockingObjectEnum_Reset(This)	\
    ( (This)->lpVtbl -> Reset(This) )

#define ICorDebugBlockingObjectEnum_Clone(This,ppEnum)	\
    ( (This)->lpVtbl -> Clone(This,ppEnum) )

#define ICorDebugBlockingObjectEnum_GetCount(This,pcelt)	\
    ( (This)->lpVtbl -> GetCount(This,pcelt) )


#define ICorDebugBlockingObjectEnum_Next(This,celt,values,pceltFetched)	\
    ( (This)->lpVtbl -> Next(This,celt,values,pceltFetched) )

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ICorDebugBlockingObjectEnum_INTERFACE_DEFINED__ */


/* interface __MIDL_itf_cordebug_0000_0128 */
/* [local] */

#pragma warning(push)
#pragma warning(disable:28718)


extern RPC_IF_HANDLE __MIDL_itf_cordebug_0000_0128_v0_0_c_ifspec;
extern RPC_IF_HANDLE __MIDL_itf_cordebug_0000_0128_v0_0_s_ifspec;

#ifndef __ICorDebugMDA_INTERFACE_DEFINED__
#define __ICorDebugMDA_INTERFACE_DEFINED__

/* interface ICorDebugMDA */
/* [unique][uuid][local][object] */

typedef
enum CorDebugMDAFlags
    {
        MDA_FLAG_SLIP	= 0x2
    } 	CorDebugMDAFlags;


EXTERN_C const IID IID_ICorDebugMDA;

#if defined(__cplusplus) && !defined(CINTERFACE)

    MIDL_INTERFACE("CC726F2F-1DB7-459b-B0EC-05F01D841B42")
    ICorDebugMDA : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE GetName(
            /* [in] */ ULONG32 cchName,
            /* [out] */ ULONG32 *pcchName,
            /* [length_is][size_is][out] */ WCHAR szName[  ]) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetDescription(
            /* [in] */ ULONG32 cchName,
            /* [out] */ ULONG32 *pcchName,
            /* [length_is][size_is][out] */ WCHAR szName[  ]) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetXML(
            /* [in] */ ULONG32 cchName,
            /* [out] */ ULONG32 *pcchName,
            /* [length_is][size_is][out] */ WCHAR szName[  ]) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetFlags(
            /* [in] */ CorDebugMDAFlags *pFlags) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetOSThreadId(
            /* [out] */ DWORD *pOsTid) = 0;

    };


#else 	/* C style interface */

    typedef struct ICorDebugMDAVtbl
    {
        BEGIN_INTERFACE

        HRESULT ( STDMETHODCALLTYPE *QueryInterface )(
            ICorDebugMDA * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */
            _COM_Outptr_  void **ppvObject);

        ULONG ( STDMETHODCALLTYPE *AddRef )(
            ICorDebugMDA * This);

        ULONG ( STDMETHODCALLTYPE *Release )(
            ICorDebugMDA * This);

        HRESULT ( STDMETHODCALLTYPE *GetName )(
            ICorDebugMDA * This,
            /* [in] */ ULONG32 cchName,
            /* [out] */ ULONG32 *pcchName,
            /* [length_is][size_is][out] */ WCHAR szName[  ]);

        HRESULT ( STDMETHODCALLTYPE *GetDescription )(
            ICorDebugMDA * This,
            /* [in] */ ULONG32 cchName,
            /* [out] */ ULONG32 *pcchName,
            /* [length_is][size_is][out] */ WCHAR szName[  ]);

        HRESULT ( STDMETHODCALLTYPE *GetXML )(
            ICorDebugMDA * This,
            /* [in] */ ULONG32 cchName,
            /* [out] */ ULONG32 *pcchName,
            /* [length_is][size_is][out] */ WCHAR szName[  ]);

        HRESULT ( STDMETHODCALLTYPE *GetFlags )(
            ICorDebugMDA * This,
            /* [in] */ CorDebugMDAFlags *pFlags);

        HRESULT ( STDMETHODCALLTYPE *GetOSThreadId )(
            ICorDebugMDA * This,
            /* [out] */ DWORD *pOsTid);

        END_INTERFACE
    } ICorDebugMDAVtbl;

    interface ICorDebugMDA
    {
        CONST_VTBL struct ICorDebugMDAVtbl *lpVtbl;
    };



#ifdef COBJMACROS


#define ICorDebugMDA_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) )

#define ICorDebugMDA_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) )

#define ICorDebugMDA_Release(This)	\
    ( (This)->lpVtbl -> Release(This) )


#define ICorDebugMDA_GetName(This,cchName,pcchName,szName)	\
    ( (This)->lpVtbl -> GetName(This,cchName,pcchName,szName) )

#define ICorDebugMDA_GetDescription(This,cchName,pcchName,szName)	\
    ( (This)->lpVtbl -> GetDescription(This,cchName,pcchName,szName) )

#define ICorDebugMDA_GetXML(This,cchName,pcchName,szName)	\
    ( (This)->lpVtbl -> GetXML(This,cchName,pcchName,szName) )

#define ICorDebugMDA_GetFlags(This,pFlags)	\
    ( (This)->lpVtbl -> GetFlags(This,pFlags) )

#define ICorDebugMDA_GetOSThreadId(This,pOsTid)	\
    ( (This)->lpVtbl -> GetOSThreadId(This,pOsTid) )

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ICorDebugMDA_INTERFACE_DEFINED__ */


/* interface __MIDL_itf_cordebug_0000_0129 */
/* [local] */

#pragma warning(pop)
#pragma warning(push)
#pragma warning(disable:28718)


extern RPC_IF_HANDLE __MIDL_itf_cordebug_0000_0129_v0_0_c_ifspec;
extern RPC_IF_HANDLE __MIDL_itf_cordebug_0000_0129_v0_0_s_ifspec;

#ifndef __ICorDebugEditAndContinueErrorInfo_INTERFACE_DEFINED__
#define __ICorDebugEditAndContinueErrorInfo_INTERFACE_DEFINED__

/* interface ICorDebugEditAndContinueErrorInfo */
/* [unique][uuid][local][object] */


EXTERN_C const IID IID_ICorDebugEditAndContinueErrorInfo;

#if defined(__cplusplus) && !defined(CINTERFACE)

    MIDL_INTERFACE("8D600D41-F4F6-4cb3-B7EC-7BD164944036")
    ICorDebugEditAndContinueErrorInfo : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE GetModule(
            /* [out] */ ICorDebugModule **ppModule) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetToken(
            /* [out] */ mdToken *pToken) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetErrorCode(
            /* [out] */ HRESULT *pHr) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetString(
            /* [in] */ ULONG32 cchString,
            /* [out] */ ULONG32 *pcchString,
            /* [length_is][size_is][out] */ WCHAR szString[  ]) = 0;

    };


#else 	/* C style interface */

    typedef struct ICorDebugEditAndContinueErrorInfoVtbl
    {
        BEGIN_INTERFACE

        HRESULT ( STDMETHODCALLTYPE *QueryInterface )(
            ICorDebugEditAndContinueErrorInfo * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */
            _COM_Outptr_  void **ppvObject);

        ULONG ( STDMETHODCALLTYPE *AddRef )(
            ICorDebugEditAndContinueErrorInfo * This);

        ULONG ( STDMETHODCALLTYPE *Release )(
            ICorDebugEditAndContinueErrorInfo * This);

        HRESULT ( STDMETHODCALLTYPE *GetModule )(
            ICorDebugEditAndContinueErrorInfo * This,
            /* [out] */ ICorDebugModule **ppModule);

        HRESULT ( STDMETHODCALLTYPE *GetToken )(
            ICorDebugEditAndContinueErrorInfo * This,
            /* [out] */ mdToken *pToken);

        HRESULT ( STDMETHODCALLTYPE *GetErrorCode )(
            ICorDebugEditAndContinueErrorInfo * This,
            /* [out] */ HRESULT *pHr);

        HRESULT ( STDMETHODCALLTYPE *GetString )(
            ICorDebugEditAndContinueErrorInfo * This,
            /* [in] */ ULONG32 cchString,
            /* [out] */ ULONG32 *pcchString,
            /* [length_is][size_is][out] */ WCHAR szString[  ]);

        END_INTERFACE
    } ICorDebugEditAndContinueErrorInfoVtbl;

    interface ICorDebugEditAndContinueErrorInfo
    {
        CONST_VTBL struct ICorDebugEditAndContinueErrorInfoVtbl *lpVtbl;
    };



#ifdef COBJMACROS


#define ICorDebugEditAndContinueErrorInfo_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) )

#define ICorDebugEditAndContinueErrorInfo_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) )

#define ICorDebugEditAndContinueErrorInfo_Release(This)	\
    ( (This)->lpVtbl -> Release(This) )


#define ICorDebugEditAndContinueErrorInfo_GetModule(This,ppModule)	\
    ( (This)->lpVtbl -> GetModule(This,ppModule) )

#define ICorDebugEditAndContinueErrorInfo_GetToken(This,pToken)	\
    ( (This)->lpVtbl -> GetToken(This,pToken) )

#define ICorDebugEditAndContinueErrorInfo_GetErrorCode(This,pHr)	\
    ( (This)->lpVtbl -> GetErrorCode(This,pHr) )

#define ICorDebugEditAndContinueErrorInfo_GetString(This,cchString,pcchString,szString)	\
    ( (This)->lpVtbl -> GetString(This,cchString,pcchString,szString) )

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ICorDebugEditAndContinueErrorInfo_INTERFACE_DEFINED__ */


/* interface __MIDL_itf_cordebug_0000_0130 */
/* [local] */

#pragma warning(pop)


extern RPC_IF_HANDLE __MIDL_itf_cordebug_0000_0130_v0_0_c_ifspec;
extern RPC_IF_HANDLE __MIDL_itf_cordebug_0000_0130_v0_0_s_ifspec;

#ifndef __ICorDebugEditAndContinueSnapshot_INTERFACE_DEFINED__
#define __ICorDebugEditAndContinueSnapshot_INTERFACE_DEFINED__

/* interface ICorDebugEditAndContinueSnapshot */
/* [unique][uuid][local][object] */


EXTERN_C const IID IID_ICorDebugEditAndContinueSnapshot;

#if defined(__cplusplus) && !defined(CINTERFACE)

    MIDL_INTERFACE("6DC3FA01-D7CB-11d2-8A95-0080C792E5D8")
    ICorDebugEditAndContinueSnapshot : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE CopyMetaData(
            /* [in] */ IStream *pIStream,
            /* [out] */ GUID *pMvid) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetMvid(
            /* [out] */ GUID *pMvid) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetRoDataRVA(
            /* [out] */ ULONG32 *pRoDataRVA) = 0;

        virtual HRESULT STDMETHODCALLTYPE GetRwDataRVA(
            /* [out] */ ULONG32 *pRwDataRVA) = 0;

        virtual HRESULT STDMETHODCALLTYPE SetPEBytes(
            /* [in] */ IStream *pIStream) = 0;

        virtual HRESULT STDMETHODCALLTYPE SetILMap(
            /* [in] */ mdToken mdFunction,
            /* [in] */ ULONG cMapSize,
            /* [size_is][in] */ COR_IL_MAP map[  ]) = 0;

        virtual HRESULT STDMETHODCALLTYPE SetPESymbolBytes(
            /* [in] */ IStream *pIStream) = 0;

    };


#else 	/* C style interface */

    typedef struct ICorDebugEditAndContinueSnapshotVtbl
    {
        BEGIN_INTERFACE

        HRESULT ( STDMETHODCALLTYPE *QueryInterface )(
            ICorDebugEditAndContinueSnapshot * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */
            _COM_Outptr_  void **ppvObject);

        ULONG ( STDMETHODCALLTYPE *AddRef )(
            ICorDebugEditAndContinueSnapshot * This);

        ULONG ( STDMETHODCALLTYPE *Release )(
            ICorDebugEditAndContinueSnapshot * This);

        HRESULT ( STDMETHODCALLTYPE *CopyMetaData )(
            ICorDebugEditAndContinueSnapshot * This,
            /* [in] */ IStream *pIStream,
            /* [out] */ GUID *pMvid);

        HRESULT ( STDMETHODCALLTYPE *GetMvid )(
            ICorDebugEditAndContinueSnapshot * This,
            /* [out] */ GUID *pMvid);

        HRESULT ( STDMETHODCALLTYPE *GetRoDataRVA )(
            ICorDebugEditAndContinueSnapshot * This,
            /* [out] */ ULONG32 *pRoDataRVA);

        HRESULT ( STDMETHODCALLTYPE *GetRwDataRVA )(
            ICorDebugEditAndContinueSnapshot * This,
            /* [out] */ ULONG32 *pRwDataRVA);

        HRESULT ( STDMETHODCALLTYPE *SetPEBytes )(
            ICorDebugEditAndContinueSnapshot * This,
            /* [in] */ IStream *pIStream);

        HRESULT ( STDMETHODCALLTYPE *SetILMap )(
            ICorDebugEditAndContinueSnapshot * This,
            /* [in] */ mdToken mdFunction,
            /* [in] */ ULONG cMapSize,
            /* [size_is][in] */ COR_IL_MAP map[  ]);

        HRESULT ( STDMETHODCALLTYPE *SetPESymbolBytes )(
            ICorDebugEditAndContinueSnapshot * This,
            /* [in] */ IStream *pIStream);

        END_INTERFACE
    } ICorDebugEditAndContinueSnapshotVtbl;

    interface ICorDebugEditAndContinueSnapshot
    {
        CONST_VTBL struct ICorDebugEditAndContinueSnapshotVtbl *lpVtbl;
    };



#ifdef COBJMACROS


#define ICorDebugEditAndContinueSnapshot_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) )

#define ICorDebugEditAndContinueSnapshot_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) )

#define ICorDebugEditAndContinueSnapshot_Release(This)	\
    ( (This)->lpVtbl -> Release(This) )


#define ICorDebugEditAndContinueSnapshot_CopyMetaData(This,pIStream,pMvid)	\
    ( (This)->lpVtbl -> CopyMetaData(This,pIStream,pMvid) )

#define ICorDebugEditAndContinueSnapshot_GetMvid(This,pMvid)	\
    ( (This)->lpVtbl -> GetMvid(This,pMvid) )

#define ICorDebugEditAndContinueSnapshot_GetRoDataRVA(This,pRoDataRVA)	\
    ( (This)->lpVtbl -> GetRoDataRVA(This,pRoDataRVA) )

#define ICorDebugEditAndContinueSnapshot_GetRwDataRVA(This,pRwDataRVA)	\
    ( (This)->lpVtbl -> GetRwDataRVA(This,pRwDataRVA) )

#define ICorDebugEditAndContinueSnapshot_SetPEBytes(This,pIStream)	\
    ( (This)->lpVtbl -> SetPEBytes(This,pIStream) )

#define ICorDebugEditAndContinueSnapshot_SetILMap(This,mdFunction,cMapSize,map)	\
    ( (This)->lpVtbl -> SetILMap(This,mdFunction,cMapSize,map) )

#define ICorDebugEditAndContinueSnapshot_SetPESymbolBytes(This,pIStream)	\
    ( (This)->lpVtbl -> SetPESymbolBytes(This,pIStream) )

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ICorDebugEditAndContinueSnapshot_INTERFACE_DEFINED__ */


#ifndef __ICorDebugExceptionObjectCallStackEnum_INTERFACE_DEFINED__
#define __ICorDebugExceptionObjectCallStackEnum_INTERFACE_DEFINED__

/* interface ICorDebugExceptionObjectCallStackEnum */
/* [unique][uuid][local][object] */


EXTERN_C const IID IID_ICorDebugExceptionObjectCallStackEnum;

#if defined(__cplusplus) && !defined(CINTERFACE)

    MIDL_INTERFACE("ED775530-4DC4-41F7-86D0-9E2DEF7DFC66")
    ICorDebugExceptionObjectCallStackEnum : public ICorDebugEnum
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE Next(
            /* [in] */ ULONG celt,
            /* [length_is][size_is][out] */ CorDebugExceptionObjectStackFrame values[  ],
            /* [out] */ ULONG *pceltFetched) = 0;

    };


#else 	/* C style interface */

    typedef struct ICorDebugExceptionObjectCallStackEnumVtbl
    {
        BEGIN_INTERFACE

        HRESULT ( STDMETHODCALLTYPE *QueryInterface )(
            ICorDebugExceptionObjectCallStackEnum * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */
            _COM_Outptr_  void **ppvObject);

        ULONG ( STDMETHODCALLTYPE *AddRef )(
            ICorDebugExceptionObjectCallStackEnum * This);

        ULONG ( STDMETHODCALLTYPE *Release )(
            ICorDebugExceptionObjectCallStackEnum * This);

        HRESULT ( STDMETHODCALLTYPE *Skip )(
            ICorDebugExceptionObjectCallStackEnum * This,
            /* [in] */ ULONG celt);

        HRESULT ( STDMETHODCALLTYPE *Reset )(
            ICorDebugExceptionObjectCallStackEnum * This);

        HRESULT ( STDMETHODCALLTYPE *Clone )(
            ICorDebugExceptionObjectCallStackEnum * This,
            /* [out] */ ICorDebugEnum **ppEnum);

        HRESULT ( STDMETHODCALLTYPE *GetCount )(
            ICorDebugExceptionObjectCallStackEnum * This,
            /* [out] */ ULONG *pcelt);

        HRESULT ( STDMETHODCALLTYPE *Next )(
            ICorDebugExceptionObjectCallStackEnum * This,
            /* [in] */ ULONG celt,
            /* [length_is][size_is][out] */ CorDebugExceptionObjectStackFrame values[  ],
            /* [out] */ ULONG *pceltFetched);

        END_INTERFACE
    } ICorDebugExceptionObjectCallStackEnumVtbl;

    interface ICorDebugExceptionObjectCallStackEnum
    {
        CONST_VTBL struct ICorDebugExceptionObjectCallStackEnumVtbl *lpVtbl;
    };



#ifdef COBJMACROS


#define ICorDebugExceptionObjectCallStackEnum_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) )

#define ICorDebugExceptionObjectCallStackEnum_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) )

#define ICorDebugExceptionObjectCallStackEnum_Release(This)	\
    ( (This)->lpVtbl -> Release(This) )


#define ICorDebugExceptionObjectCallStackEnum_Skip(This,celt)	\
    ( (This)->lpVtbl -> Skip(This,celt) )

#define ICorDebugExceptionObjectCallStackEnum_Reset(This)	\
    ( (This)->lpVtbl -> Reset(This) )

#define ICorDebugExceptionObjectCallStackEnum_Clone(This,ppEnum)	\
    ( (This)->lpVtbl -> Clone(This,ppEnum) )

#define ICorDebugExceptionObjectCallStackEnum_GetCount(This,pcelt)	\
    ( (This)->lpVtbl -> GetCount(This,pcelt) )


#define ICorDebugExceptionObjectCallStackEnum_Next(This,celt,values,pceltFetched)	\
    ( (This)->lpVtbl -> Next(This,celt,values,pceltFetched) )

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ICorDebugExceptionObjectCallStackEnum_INTERFACE_DEFINED__ */


#ifndef __ICorDebugExceptionObjectValue_INTERFACE_DEFINED__
#define __ICorDebugExceptionObjectValue_INTERFACE_DEFINED__

/* interface ICorDebugExceptionObjectValue */
/* [unique][uuid][local][object] */


EXTERN_C const IID IID_ICorDebugExceptionObjectValue;

#if defined(__cplusplus) && !defined(CINTERFACE)

    MIDL_INTERFACE("AE4CA65D-59DD-42A2-83A5-57E8A08D8719")
    ICorDebugExceptionObjectValue : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE EnumerateExceptionCallStack(
            /* [out] */ ICorDebugExceptionObjectCallStackEnum **ppCallStackEnum) = 0;

    };


#else 	/* C style interface */

    typedef struct ICorDebugExceptionObjectValueVtbl
    {
        BEGIN_INTERFACE

        HRESULT ( STDMETHODCALLTYPE *QueryInterface )(
            ICorDebugExceptionObjectValue * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */
            _COM_Outptr_  void **ppvObject);

        ULONG ( STDMETHODCALLTYPE *AddRef )(
            ICorDebugExceptionObjectValue * This);

        ULONG ( STDMETHODCALLTYPE *Release )(
            ICorDebugExceptionObjectValue * This);

        HRESULT ( STDMETHODCALLTYPE *EnumerateExceptionCallStack )(
            ICorDebugExceptionObjectValue * This,
            /* [out] */ ICorDebugExceptionObjectCallStackEnum **ppCallStackEnum);

        END_INTERFACE
    } ICorDebugExceptionObjectValueVtbl;

    interface ICorDebugExceptionObjectValue
    {
        CONST_VTBL struct ICorDebugExceptionObjectValueVtbl *lpVtbl;
    };



#ifdef COBJMACROS


#define ICorDebugExceptionObjectValue_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) )

#define ICorDebugExceptionObjectValue_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) )

#define ICorDebugExceptionObjectValue_Release(This)	\
    ( (This)->lpVtbl -> Release(This) )


#define ICorDebugExceptionObjectValue_EnumerateExceptionCallStack(This,ppCallStackEnum)	\
    ( (This)->lpVtbl -> EnumerateExceptionCallStack(This,ppCallStackEnum) )

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ICorDebugExceptionObjectValue_INTERFACE_DEFINED__ */



#ifndef __CORDBLib_LIBRARY_DEFINED__
#define __CORDBLib_LIBRARY_DEFINED__

/* library CORDBLib */
/* [helpstring][version][uuid] */
































EXTERN_C const IID LIBID_CORDBLib;

EXTERN_C const CLSID CLSID_CorDebug;

#ifdef __cplusplus

class DECLSPEC_UUID("6fef44d0-39e7-4c77-be8e-c9f8cf988630")
CorDebug;
#endif

EXTERN_C const CLSID CLSID_EmbeddedCLRCorDebug;

#ifdef __cplusplus

class DECLSPEC_UUID("211f1254-bc7e-4af5-b9aa-067308d83dd1")
EmbeddedCLRCorDebug;
#endif
#endif /* __CORDBLib_LIBRARY_DEFINED__ */

/* Additional Prototypes for ALL interfaces */

/* end of Additional Prototypes */

#ifdef __cplusplus
}
#endif

#endif
