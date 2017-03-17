// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//
// See makefile.inc.  During the build, this file is converted into a .inc
// file for inclusion by .asm files.  The #defines are converted into EQU's.
//
// Allow multiple inclusion.


#ifndef _TARGET_AMD64_
#error this file should only be used on an AMD64 platform
#endif // _TARGET_AMD64_

#include "../../inc/switches.h"

#ifndef ASMCONSTANTS_C_ASSERT
#define ASMCONSTANTS_C_ASSERT(cond)
#endif

#ifndef ASMCONSTANTS_RUNTIME_ASSERT
#define ASMCONSTANTS_RUNTIME_ASSERT(cond)
#endif


// Some contants are different in _DEBUG builds.  This macro factors out
// ifdefs from below.
#ifdef _DEBUG
#define DBG_FRE(dbg,fre) dbg
#else
#define DBG_FRE(dbg,fre) fre
#endif

#define DynamicHelperFrameFlags_Default     0
#define DynamicHelperFrameFlags_ObjectArg   1
#define DynamicHelperFrameFlags_ObjectArg2  2

#define ASMCONSTANT_OFFSETOF_ASSERT(struct, member) \
ASMCONSTANTS_C_ASSERT(OFFSETOF__##struct##__##member == offsetof(struct, member));

#define ASMCONSTANT_SIZEOF_ASSERT(classname) \
ASMCONSTANTS_C_ASSERT(SIZEOF__##classname == sizeof(classname));

#define               ASM_ELEMENT_TYPE_R4                 0xC
ASMCONSTANTS_C_ASSERT(ASM_ELEMENT_TYPE_R4 == ELEMENT_TYPE_R4);

#define               ASM_ELEMENT_TYPE_R8                 0xD
ASMCONSTANTS_C_ASSERT(ASM_ELEMENT_TYPE_R8 == ELEMENT_TYPE_R8);


#define METHODDESC_REGNUM                    10
#define METHODDESC_REGISTER                 r10

#define PINVOKE_CALLI_TARGET_REGNUM          10
#define PINVOKE_CALLI_TARGET_REGISTER       r10

#define PINVOKE_CALLI_SIGTOKEN_REGNUM        11
#define PINVOKE_CALLI_SIGTOKEN_REGISTER     r11

#ifdef UNIX_AMD64_ABI
// rdi, rsi, rdx, rcx, r8, r9
#define SIZEOF_MAX_INT_ARG_SPILL  0x30
// xmm0...xmm7
#define SIZEOF_MAX_FP_ARG_SPILL             0x80
#else
// rcx, rdx, r8, r9
#define SIZEOF_MAX_OUTGOING_ARGUMENT_HOMES  0x20
// xmm0...xmm3
#define SIZEOF_MAX_FP_ARG_SPILL             0x40
#endif



#ifndef UNIX_AMD64_ABI
#define SIZEOF_CalleeSavedRegisters         0x40
ASMCONSTANTS_C_ASSERT(SIZEOF_CalleeSavedRegisters == sizeof(CalleeSavedRegisters));
#else
#define SIZEOF_CalleeSavedRegisters         0x30
ASMCONSTANTS_C_ASSERT(SIZEOF_CalleeSavedRegisters == sizeof(CalleeSavedRegisters));
#endif

#define SIZEOF_GSCookie                             0x8
ASMCONSTANTS_C_ASSERT(SIZEOF_GSCookie == sizeof(GSCookie));

#define               OFFSETOF__Frame____VFN_table  0

#define               OFFSETOF__Frame__m_Next       0x8
ASMCONSTANTS_C_ASSERT(OFFSETOF__Frame__m_Next
                    == offsetof(Frame, m_Next));

#define               SIZEOF__Frame                 0x10

#ifdef FEATURE_COMINTEROP
#define               SIZEOF__ComPrestubMethodFrame                 0x20
ASMCONSTANTS_C_ASSERT(SIZEOF__ComPrestubMethodFrame
                    == sizeof(ComPrestubMethodFrame));

#define               SIZEOF__ComMethodFrame                        0x20
ASMCONSTANTS_C_ASSERT(SIZEOF__ComMethodFrame
                    == sizeof(ComMethodFrame));
#endif // FEATURE_COMINTEROP

#define               OFFSETOF__UMEntryThunk__m_pUMThunkMarshInfo   0x18
ASMCONSTANTS_C_ASSERT(OFFSETOF__UMEntryThunk__m_pUMThunkMarshInfo
                    == offsetof(UMEntryThunk, m_pUMThunkMarshInfo));

#define               OFFSETOF__UMEntryThunk__m_dwDomainId   0x20
ASMCONSTANTS_C_ASSERT(OFFSETOF__UMEntryThunk__m_dwDomainId
                    == offsetof(UMEntryThunk, m_dwDomainId));

#define               OFFSETOF__UMThunkMarshInfo__m_pILStub         0x00
ASMCONSTANTS_C_ASSERT(OFFSETOF__UMThunkMarshInfo__m_pILStub
                    == offsetof(UMThunkMarshInfo, m_pILStub));

#define               OFFSETOF__UMThunkMarshInfo__m_cbActualArgSize 0x08
ASMCONSTANTS_C_ASSERT(OFFSETOF__UMThunkMarshInfo__m_cbActualArgSize
                    == offsetof(UMThunkMarshInfo, m_cbActualArgSize));

#ifdef FEATURE_COMINTEROP

#define               OFFSETOF__ComPlusCallMethodDesc__m_pComPlusCallInfo        DBG_FRE(0x30, 0x08)
ASMCONSTANTS_C_ASSERT(OFFSETOF__ComPlusCallMethodDesc__m_pComPlusCallInfo
                    == offsetof(ComPlusCallMethodDesc, m_pComPlusCallInfo));

#define               OFFSETOF__ComPlusCallInfo__m_pILStub                       0x0
ASMCONSTANTS_C_ASSERT(OFFSETOF__ComPlusCallInfo__m_pILStub
                      == offsetof(ComPlusCallInfo, m_pILStub));

#endif // FEATURE_COMINTEROP

#define               OFFSETOF__Thread__m_fPreemptiveGCDisabled     0x0C
#ifndef CROSSGEN_COMPILE
ASMCONSTANTS_C_ASSERT(OFFSETOF__Thread__m_fPreemptiveGCDisabled
                    == offsetof(Thread, m_fPreemptiveGCDisabled));
#endif
#define Thread_m_fPreemptiveGCDisabled OFFSETOF__Thread__m_fPreemptiveGCDisabled

#define               OFFSETOF__Thread__m_pFrame                    0x10
#ifndef CROSSGEN_COMPILE
ASMCONSTANTS_C_ASSERT(OFFSETOF__Thread__m_pFrame
                    == offsetof(Thread, m_pFrame));
#endif
#define Thread_m_pFrame OFFSETOF__Thread__m_pFrame

#ifndef CROSSGEN_COMPILE
#define               OFFSETOF__Thread__m_State                     0x8
ASMCONSTANTS_C_ASSERT(OFFSETOF__Thread__m_State
                    == offsetof(Thread, m_State));

#define               OFFSETOF__Thread__m_pDomain                   0x20
ASMCONSTANTS_C_ASSERT(OFFSETOF__Thread__m_pDomain
                    == offsetof(Thread, m_pDomain));

#define               OFFSETOF__Thread__m_dwLockCount               0x28
ASMCONSTANTS_C_ASSERT(OFFSETOF__Thread__m_dwLockCount
                    == offsetof(Thread, m_dwLockCount));

#define               OFFSETOF__Thread__m_ThreadId                  0x2C
ASMCONSTANTS_C_ASSERT(OFFSETOF__Thread__m_ThreadId
                    == offsetof(Thread, m_ThreadId));

#define               OFFSET__Thread__m_alloc_context__alloc_ptr 0x60
ASMCONSTANTS_C_ASSERT(OFFSET__Thread__m_alloc_context__alloc_ptr == offsetof(Thread, m_alloc_context) + offsetof(gc_alloc_context, alloc_ptr));

#define               OFFSET__Thread__m_alloc_context__alloc_limit 0x68
ASMCONSTANTS_C_ASSERT(OFFSET__Thread__m_alloc_context__alloc_limit == offsetof(Thread, m_alloc_context) + offsetof(gc_alloc_context, alloc_limit));

#define               OFFSETOF__gc_alloc_context__alloc_ptr 0x0
ASMCONSTANT_OFFSETOF_ASSERT(gc_alloc_context, alloc_ptr);

#define               OFFSETOF__gc_alloc_context__alloc_limit 0x8
ASMCONSTANT_OFFSETOF_ASSERT(gc_alloc_context, alloc_limit);

#define               OFFSETOF__ThreadExceptionState__m_pCurrentTracker 0x000
ASMCONSTANTS_C_ASSERT(OFFSETOF__ThreadExceptionState__m_pCurrentTracker
                    == offsetof(ThreadExceptionState, m_pCurrentTracker));

#define               THREAD_CATCHATSAFEPOINT_BITS 0x5F
ASMCONSTANTS_C_ASSERT(THREAD_CATCHATSAFEPOINT_BITS == Thread::TS_CatchAtSafePoint);
#endif // CROSSGEN_COMPILE



#define               OFFSETOF__NDirectMethodDesc__m_pWriteableData DBG_FRE(0x48, 0x20)
ASMCONSTANTS_C_ASSERT(OFFSETOF__NDirectMethodDesc__m_pWriteableData == offsetof(NDirectMethodDesc, ndirect.m_pWriteableData));

#define               OFFSETOF__ObjHeader__SyncBlkIndex         0x4
ASMCONSTANTS_C_ASSERT(OFFSETOF__ObjHeader__SyncBlkIndex
                    == (sizeof(ObjHeader) - offsetof(ObjHeader, m_SyncBlockValue)));

#define           SIZEOF__SyncTableEntry                    0x10
ASMCONSTANT_SIZEOF_ASSERT(SyncTableEntry);

#define           OFFSETOF__SyncTableEntry__m_SyncBlock     0x0
ASMCONSTANT_OFFSETOF_ASSERT(SyncTableEntry, m_SyncBlock);

#define           OFFSETOF__SyncBlock__m_Monitor            0x0
ASMCONSTANT_OFFSETOF_ASSERT(SyncBlock, m_Monitor);

#define           OFFSETOF__DelegateObject___methodPtr      0x18
ASMCONSTANT_OFFSETOF_ASSERT(DelegateObject, _methodPtr);

#define           OFFSETOF__DelegateObject___target         0x08
ASMCONSTANT_OFFSETOF_ASSERT(DelegateObject, _target);

#define               OFFSETOF__AwareLock__m_MonitorHeld        0x0
ASMCONSTANTS_C_ASSERT(OFFSETOF__AwareLock__m_MonitorHeld
                    == offsetof(AwareLock, m_MonitorHeld));

#define               OFFSETOF__AwareLock__m_Recursion          0x4
ASMCONSTANTS_C_ASSERT(OFFSETOF__AwareLock__m_Recursion
                    == offsetof(AwareLock, m_Recursion));

#define               OFFSETOF__AwareLock__m_HoldingThread      0x8
ASMCONSTANTS_C_ASSERT(OFFSETOF__AwareLock__m_HoldingThread
                    == offsetof(AwareLock, m_HoldingThread));

#define               OFFSETOF__g_SystemInfo__dwNumberOfProcessors  0x20
ASMCONSTANTS_C_ASSERT(OFFSETOF__g_SystemInfo__dwNumberOfProcessors
                    == offsetof(SYSTEM_INFO, dwNumberOfProcessors));

#define               OFFSETOF__g_SpinConstants__dwInitialDuration  0x0
ASMCONSTANTS_C_ASSERT(OFFSETOF__g_SpinConstants__dwInitialDuration
                    == offsetof(SpinConstants, dwInitialDuration));

#define               OFFSETOF__g_SpinConstants__dwMaximumDuration  0x4
ASMCONSTANTS_C_ASSERT(OFFSETOF__g_SpinConstants__dwMaximumDuration
                    == offsetof(SpinConstants, dwMaximumDuration));

#define               OFFSETOF__g_SpinConstants__dwBackoffFactor  0x8
ASMCONSTANTS_C_ASSERT(OFFSETOF__g_SpinConstants__dwBackoffFactor
                    == offsetof(SpinConstants, dwBackoffFactor));

#define               OFFSETOF__MethodTable__m_dwFlags              0x00
ASMCONSTANTS_C_ASSERT(OFFSETOF__MethodTable__m_dwFlags
                    == offsetof(MethodTable, m_dwFlags));

#define               OFFSET__MethodTable__m_BaseSize               0x04
ASMCONSTANTS_C_ASSERT(OFFSET__MethodTable__m_BaseSize
                  == offsetof(MethodTable, m_BaseSize));

#define               OFFSETOF__MethodTable__m_wNumInterfaces       0x0E
ASMCONSTANTS_C_ASSERT(OFFSETOF__MethodTable__m_wNumInterfaces
                    == offsetof(MethodTable, m_wNumInterfaces));

#define               OFFSETOF__MethodTable__m_pParentMethodTable   DBG_FRE(0x18, 0x10)
ASMCONSTANTS_C_ASSERT(OFFSETOF__MethodTable__m_pParentMethodTable
                    == offsetof(MethodTable, m_pParentMethodTable));

#define               OFFSETOF__MethodTable__m_pWriteableData       DBG_FRE(0x28, 0x20)
ASMCONSTANTS_C_ASSERT(OFFSETOF__MethodTable__m_pWriteableData
                    == offsetof(MethodTable, m_pWriteableData));

#define               OFFSETOF__MethodTable__m_pEEClass             DBG_FRE(0x30, 0x28)
ASMCONSTANTS_C_ASSERT(OFFSETOF__MethodTable__m_pEEClass
                    == offsetof(MethodTable, m_pEEClass));

#define               METHODTABLE_OFFSET_VTABLE          DBG_FRE(0x48, 0x40)
ASMCONSTANTS_C_ASSERT(METHODTABLE_OFFSET_VTABLE == sizeof(MethodTable));

#define               OFFSETOF__MethodTable__m_ElementType      DBG_FRE(0x38, 0x30)
ASMCONSTANTS_C_ASSERT(OFFSETOF__MethodTable__m_ElementType
                    == offsetof(MethodTable, m_pMultipurposeSlot1));

#define               OFFSETOF__MethodTable__m_pInterfaceMap    DBG_FRE(0x40, 0x38)
ASMCONSTANTS_C_ASSERT(OFFSETOF__MethodTable__m_pInterfaceMap
                    == offsetof(MethodTable, m_pMultipurposeSlot2));


#define MethodTable_VtableSlotsPerChunk     8
ASMCONSTANTS_C_ASSERT(MethodTable_VtableSlotsPerChunk == VTABLE_SLOTS_PER_CHUNK)

#define MethodTable_VtableSlotsPerChunkLog2 3
ASMCONSTANTS_C_ASSERT(MethodTable_VtableSlotsPerChunkLog2 == VTABLE_SLOTS_PER_CHUNK_LOG2)

#if defined(FEATURE_TYPEEQUIVALENCE)
#define               METHODTABLE_EQUIVALENCE_FLAGS 0x02000000
ASMCONSTANTS_C_ASSERT(METHODTABLE_EQUIVALENCE_FLAGS
                    == MethodTable::enum_flag_HasTypeEquivalence);
#else
#define               METHODTABLE_EQUIVALENCE_FLAGS 0x0
#endif

#define               METHODTABLE_NONTRIVIALINTERFACECAST_FLAGS (0x00080000 + 0x40000000 + 0x00400000)
ASMCONSTANTS_C_ASSERT(METHODTABLE_NONTRIVIALINTERFACECAST_FLAGS
                    == MethodTable::enum_flag_NonTrivialInterfaceCast);

#define               MethodTable__enum_flag_ContainsPointers 0x01000000
ASMCONSTANTS_C_ASSERT(MethodTable__enum_flag_ContainsPointers
                    == MethodTable::enum_flag_ContainsPointers);

#define               OFFSETOF__MethodTableWriteableData__m_dwFlags 0
ASMCONSTANTS_C_ASSERT(OFFSETOF__MethodTableWriteableData__m_dwFlags
                    == offsetof(MethodTableWriteableData, m_dwFlags));

#define               MethodTableWriteableData__enum_flag_Unrestored 0x04
ASMCONSTANTS_C_ASSERT(MethodTableWriteableData__enum_flag_Unrestored
                    == MethodTableWriteableData::enum_flag_Unrestored);

#define               OFFSETOF__InterfaceInfo_t__m_pMethodTable  0
ASMCONSTANTS_C_ASSERT(OFFSETOF__InterfaceInfo_t__m_pMethodTable
                    == offsetof(InterfaceInfo_t, m_pMethodTable));

#define               SIZEOF__InterfaceInfo_t   0x8
ASMCONSTANTS_C_ASSERT(SIZEOF__InterfaceInfo_t
                    == sizeof(InterfaceInfo_t));

#define               OFFSETOF__AppDomain__m_dwId   0x8
ASMCONSTANTS_C_ASSERT(OFFSETOF__AppDomain__m_dwId 
                    == offsetof(AppDomain, m_dwId));

#define               OFFSETOF__AppDomain__m_sDomainLocalBlock   DBG_FRE(0x10, 0x10)
ASMCONSTANTS_C_ASSERT(OFFSETOF__AppDomain__m_sDomainLocalBlock
                    == offsetof(AppDomain, m_sDomainLocalBlock));

#define               OFFSETOF__DomainLocalBlock__m_pModuleSlots   0x8 
ASMCONSTANTS_C_ASSERT(OFFSETOF__DomainLocalBlock__m_pModuleSlots
                    == offsetof(DomainLocalBlock, m_pModuleSlots));

#define               OFFSETOF__DomainLocalModule__m_pDataBlob   0x030
ASMCONSTANTS_C_ASSERT(OFFSETOF__DomainLocalModule__m_pDataBlob  
                    == offsetof(DomainLocalModule, m_pDataBlob));

// If this changes then we can't just test one bit in the assembly code.
ASMCONSTANTS_C_ASSERT(ClassInitFlags::INITIALIZED_FLAG == 1);

// End for JIT_GetSharedNonGCStaticBaseWorker

// For JIT_GetSharedGCStaticBaseWorker

#define               OFFSETOF__DomainLocalModule__m_pGCStatics 0x020 
ASMCONSTANTS_C_ASSERT(OFFSETOF__DomainLocalModule__m_pGCStatics
                    == offsetof(DomainLocalModule, m_pGCStatics));

// End for JIT_GetSharedGCStaticBaseWorker

#define                  CORINFO_NullReferenceException_ASM 0
ASMCONSTANTS_C_ASSERT(   CORINFO_NullReferenceException_ASM
                      == CORINFO_NullReferenceException);

#define                  CORINFO_InvalidCastException_ASM 2
ASMCONSTANTS_C_ASSERT(   CORINFO_InvalidCastException_ASM
                      == CORINFO_InvalidCastException);

#define                  CORINFO_IndexOutOfRangeException_ASM 3
ASMCONSTANTS_C_ASSERT(   CORINFO_IndexOutOfRangeException_ASM
                      == CORINFO_IndexOutOfRangeException);

#define                  CORINFO_SynchronizationLockException_ASM 5
ASMCONSTANTS_C_ASSERT(   CORINFO_SynchronizationLockException_ASM
                      == CORINFO_SynchronizationLockException);

#define                  CORINFO_ArrayTypeMismatchException_ASM 6
ASMCONSTANTS_C_ASSERT(   CORINFO_ArrayTypeMismatchException_ASM
                      == CORINFO_ArrayTypeMismatchException);

#define                  CORINFO_ArgumentNullException_ASM 8
ASMCONSTANTS_C_ASSERT(   CORINFO_ArgumentNullException_ASM
                      == CORINFO_ArgumentNullException);
                      
#define                  CORINFO_ArgumentException_ASM 9
ASMCONSTANTS_C_ASSERT(   CORINFO_ArgumentException_ASM
                      == CORINFO_ArgumentException);


// MachState offsets (AMD64\gmscpu.h)

#define               OFFSETOF__MachState__m_Rip            0x00
ASMCONSTANTS_C_ASSERT(OFFSETOF__MachState__m_Rip
                    == offsetof(MachState, m_Rip));

#define               OFFSETOF__MachState__m_Rsp            0x08
ASMCONSTANTS_C_ASSERT(OFFSETOF__MachState__m_Rsp
                    == offsetof(MachState, m_Rsp));

#define               OFFSETOF__MachState__m_Capture        0x10
ASMCONSTANTS_C_ASSERT(OFFSETOF__MachState__m_Capture
                    == offsetof(MachState, m_Capture));

#ifdef UNIX_AMD64_ABI
#define               OFFSETOF__MachState__m_Ptrs           0x40
#define               OFFSETOF__MachState___pRetAddr        0x70
#define               OFFSETOF__LazyMachState__m_CaptureRip 0xA8
#define               OFFSETOF__LazyMachState__m_CaptureRsp 0xB0
#else
#define               OFFSETOF__MachState__m_Ptrs           0x50
#define               OFFSETOF__MachState___pRetAddr        0x90
#define               OFFSETOF__LazyMachState__m_CaptureRip 0x98
#define               OFFSETOF__LazyMachState__m_CaptureRsp 0xA0
#endif
ASMCONSTANTS_C_ASSERT(OFFSETOF__MachState__m_Ptrs
                    == offsetof(MachState, m_Ptrs));
ASMCONSTANTS_C_ASSERT(OFFSETOF__MachState___pRetAddr
                    == offsetof(MachState, _pRetAddr));
ASMCONSTANTS_C_ASSERT(OFFSETOF__LazyMachState__m_CaptureRip
                    == offsetof(LazyMachState, m_CaptureRip));
ASMCONSTANTS_C_ASSERT(OFFSETOF__LazyMachState__m_CaptureRsp
                    == offsetof(LazyMachState, m_CaptureRsp));

#define               OFFSETOF__MethodDesc__m_wFlags        DBG_FRE(0x2E, 0x06)
ASMCONSTANTS_C_ASSERT(OFFSETOF__MethodDesc__m_wFlags == offsetof(MethodDesc, m_wFlags));
                    
#define               OFFSETOF__VASigCookie__pNDirectILStub     0x8
ASMCONSTANTS_C_ASSERT(OFFSETOF__VASigCookie__pNDirectILStub
                    == offsetof(VASigCookie, pNDirectILStub));

#define               SIZEOF__CONTEXT                 (8*6 + 4*2 + 2*6 + 4 + 8*6 + 8*16 + 8 + /*XMM_SAVE_AREA32*/(2*2 + 1*2 + 2 + 4 + 2*2 + 4 + 2*2 + 4*2 + 16*8 + 16*16 + 1*96) + 26*16 + 8 + 8*5)
ASMCONSTANTS_C_ASSERT(SIZEOF__CONTEXT
                    == sizeof(CONTEXT));

#define               OFFSETOF__CONTEXT__Rax          (8*6 + 4*2 + 2*6 + 4 + 8*6)
ASMCONSTANTS_C_ASSERT(OFFSETOF__CONTEXT__Rax
                    == offsetof(CONTEXT, Rax));

#define               OFFSETOF__CONTEXT__Rcx          (8*6 + 4*2 + 2*6 + 4 + 8*6 + 8)
ASMCONSTANTS_C_ASSERT(OFFSETOF__CONTEXT__Rcx
                    == offsetof(CONTEXT, Rcx));

#define               OFFSETOF__CONTEXT__Rdx          (8*6 + 4*2 + 2*6 + 4 + 8*6 + 8*2)
ASMCONSTANTS_C_ASSERT(OFFSETOF__CONTEXT__Rdx
                    == offsetof(CONTEXT, Rdx));

#define               OFFSETOF__CONTEXT__Rbx          (8*6 + 4*2 + 2*6 + 4 + 8*6 + 8*3)
ASMCONSTANTS_C_ASSERT(OFFSETOF__CONTEXT__Rbx
                    == offsetof(CONTEXT, Rbx));

#define               OFFSETOF__CONTEXT__Rsp          (8*6 + 4*2 + 2*6 + 4 + 8*6 + 8*4)
ASMCONSTANTS_C_ASSERT(OFFSETOF__CONTEXT__Rsp
                    == offsetof(CONTEXT, Rsp));

#define               OFFSETOF__CONTEXT__Rbp          (8*6 + 4*2 + 2*6 + 4 + 8*6 + 8*5)
ASMCONSTANTS_C_ASSERT(OFFSETOF__CONTEXT__Rbp
                    == offsetof(CONTEXT, Rbp));

#define               OFFSETOF__CONTEXT__Rsi          (8*6 + 4*2 + 2*6 + 4 + 8*6 + 8*6)
ASMCONSTANTS_C_ASSERT(OFFSETOF__CONTEXT__Rsi
                    == offsetof(CONTEXT, Rsi));

#define               OFFSETOF__CONTEXT__Rdi          (8*6 + 4*2 + 2*6 + 4 + 8*6 + 8*7)
ASMCONSTANTS_C_ASSERT(OFFSETOF__CONTEXT__Rdi
                    == offsetof(CONTEXT, Rdi));

#define               OFFSETOF__CONTEXT__R8           (8*6 + 4*2 + 2*6 + 4 + 8*6 + 8*8)
ASMCONSTANTS_C_ASSERT(OFFSETOF__CONTEXT__R8
                    == offsetof(CONTEXT, R8));

#define               OFFSETOF__CONTEXT__R9          (8*6 + 4*2 + 2*6 + 4 + 8*6 + 8*9)
ASMCONSTANTS_C_ASSERT(OFFSETOF__CONTEXT__R9
                    == offsetof(CONTEXT, R9));

#define               OFFSETOF__CONTEXT__R10          (8*6 + 4*2 + 2*6 + 4 + 8*6 + 8*10)
ASMCONSTANTS_C_ASSERT(OFFSETOF__CONTEXT__R10
                    == offsetof(CONTEXT, R10));

#define               OFFSETOF__CONTEXT__R11          (8*6 + 4*2 + 2*6 + 4 + 8*6 + 8*11)
ASMCONSTANTS_C_ASSERT(OFFSETOF__CONTEXT__R11
                    == offsetof(CONTEXT, R11));

#define               OFFSETOF__CONTEXT__R12          (8*6 + 4*2 + 2*6 + 4 + 8*6 + 8*12)
ASMCONSTANTS_C_ASSERT(OFFSETOF__CONTEXT__R12
                    == offsetof(CONTEXT, R12));

#define               OFFSETOF__CONTEXT__R13          (8*6 + 4*2 + 2*6 + 4 + 8*6 + 8*13)
ASMCONSTANTS_C_ASSERT(OFFSETOF__CONTEXT__R13
                    == offsetof(CONTEXT, R13));

#define               OFFSETOF__CONTEXT__R14          (8*6 + 4*2 + 2*6 + 4 + 8*6 + 8*14)
ASMCONSTANTS_C_ASSERT(OFFSETOF__CONTEXT__R14
                    == offsetof(CONTEXT, R14));

#define               OFFSETOF__CONTEXT__R15          (8*6 + 4*2 + 2*6 + 4 + 8*6 + 8*15)
ASMCONSTANTS_C_ASSERT(OFFSETOF__CONTEXT__R15
                    == offsetof(CONTEXT, R15));

#define               OFFSETOF__CONTEXT__Rip          (8*6 + 4*2 + 2*6 + 4 + 8*6 + 8*16)
ASMCONSTANTS_C_ASSERT(OFFSETOF__CONTEXT__Rip
                    == offsetof(CONTEXT, Rip));

#define               OFFSETOF__CONTEXT__Xmm0         (8*6 + 4*2 + 2*6 + 4 + 8*6 + 8*16 + 8 + 2*16 + 8*16)
ASMCONSTANTS_C_ASSERT(OFFSETOF__CONTEXT__Xmm0
                    == offsetof(CONTEXT, Xmm0));

#define               OFFSETOF__CONTEXT__Xmm1         (8*6 + 4*2 + 2*6 + 4 + 8*6 + 8*16 + 8 + 2*16 + 8*16 + 16)
ASMCONSTANTS_C_ASSERT(OFFSETOF__CONTEXT__Xmm1
                    == offsetof(CONTEXT, Xmm1));

#define               OFFSETOF__CONTEXT__Xmm2         (8*6 + 4*2 + 2*6 + 4 + 8*6 + 8*16 + 8 + 2*16 + 8*16 + 16*2)
ASMCONSTANTS_C_ASSERT(OFFSETOF__CONTEXT__Xmm2
                    == offsetof(CONTEXT, Xmm2));

#define               OFFSETOF__CONTEXT__Xmm3         (8*6 + 4*2 + 2*6 + 4 + 8*6 + 8*16 + 8 + 2*16 + 8*16 + 16*3)
ASMCONSTANTS_C_ASSERT(OFFSETOF__CONTEXT__Xmm3
                    == offsetof(CONTEXT, Xmm3));

#define               OFFSETOF__CONTEXT__Xmm4         (8*6 + 4*2 + 2*6 + 4 + 8*6 + 8*16 + 8 + 2*16 + 8*16 + 16*4)
ASMCONSTANTS_C_ASSERT(OFFSETOF__CONTEXT__Xmm4
                    == offsetof(CONTEXT, Xmm4));

#define               OFFSETOF__CONTEXT__Xmm5         (8*6 + 4*2 + 2*6 + 4 + 8*6 + 8*16 + 8 + 2*16 + 8*16 + 16*5)
ASMCONSTANTS_C_ASSERT(OFFSETOF__CONTEXT__Xmm5
                    == offsetof(CONTEXT, Xmm5));

#define               OFFSETOF__CONTEXT__Xmm6         (8*6 + 4*2 + 2*6 + 4 + 8*6 + 8*16 + 8 + 2*16 + 8*16 + 16*6)
ASMCONSTANTS_C_ASSERT(OFFSETOF__CONTEXT__Xmm6
                    == offsetof(CONTEXT, Xmm6));

#define               OFFSETOF__CONTEXT__Xmm7         (8*6 + 4*2 + 2*6 + 4 + 8*6 + 8*16 + 8 + 2*16 + 8*16 + 16*7)
ASMCONSTANTS_C_ASSERT(OFFSETOF__CONTEXT__Xmm7
                    == offsetof(CONTEXT, Xmm7));

#define               OFFSETOF__CONTEXT__Xmm8         (8*6 + 4*2 + 2*6 + 4 + 8*6 + 8*16 + 8 + 2*16 + 8*16 + 16*8)
ASMCONSTANTS_C_ASSERT(OFFSETOF__CONTEXT__Xmm8
                    == offsetof(CONTEXT, Xmm8));

#define               OFFSETOF__CONTEXT__Xmm9         (8*6 + 4*2 + 2*6 + 4 + 8*6 + 8*16 + 8 + 2*16 + 8*16 + 16*9)
ASMCONSTANTS_C_ASSERT(OFFSETOF__CONTEXT__Xmm9
                    == offsetof(CONTEXT, Xmm9));

#define               OFFSETOF__CONTEXT__Xmm10        (8*6 + 4*2 + 2*6 + 4 + 8*6 + 8*16 + 8 + 2*16 + 8*16 + 16*10)
ASMCONSTANTS_C_ASSERT(OFFSETOF__CONTEXT__Xmm10
                    == offsetof(CONTEXT, Xmm10));

#define               OFFSETOF__CONTEXT__Xmm11        (8*6 + 4*2 + 2*6 + 4 + 8*6 + 8*16 + 8 + 2*16 + 8*16 + 16*11)
ASMCONSTANTS_C_ASSERT(OFFSETOF__CONTEXT__Xmm11
                    == offsetof(CONTEXT, Xmm11));

#define               OFFSETOF__CONTEXT__Xmm12        (8*6 + 4*2 + 2*6 + 4 + 8*6 + 8*16 + 8 + 2*16 + 8*16 + 16*12)
ASMCONSTANTS_C_ASSERT(OFFSETOF__CONTEXT__Xmm12
                    == offsetof(CONTEXT, Xmm12));

#define               OFFSETOF__CONTEXT__Xmm13        (8*6 + 4*2 + 2*6 + 4 + 8*6 + 8*16 + 8 + 2*16 + 8*16 + 16*13)
ASMCONSTANTS_C_ASSERT(OFFSETOF__CONTEXT__Xmm13
                    == offsetof(CONTEXT, Xmm13));

#define               OFFSETOF__CONTEXT__Xmm14        (8*6 + 4*2 + 2*6 + 4 + 8*6 + 8*16 + 8 + 2*16 + 8*16 + 16*14)
ASMCONSTANTS_C_ASSERT(OFFSETOF__CONTEXT__Xmm14
                    == offsetof(CONTEXT, Xmm14));

#define               OFFSETOF__CONTEXT__Xmm15        (8*6 + 4*2 + 2*6 + 4 + 8*6 + 8*16 + 8 + 2*16 + 8*16 + 16*15)
ASMCONSTANTS_C_ASSERT(OFFSETOF__CONTEXT__Xmm15
                    == offsetof(CONTEXT, Xmm15));

#define               SIZEOF__FaultingExceptionFrame  (0x20 + SIZEOF__CONTEXT)
ASMCONSTANTS_C_ASSERT(SIZEOF__FaultingExceptionFrame 
                    == sizeof(FaultingExceptionFrame));

#define               OFFSETOF__FaultingExceptionFrame__m_fFilterExecuted 0x10
ASMCONSTANTS_C_ASSERT(OFFSETOF__FaultingExceptionFrame__m_fFilterExecuted 
                    == offsetof(FaultingExceptionFrame, m_fFilterExecuted));

#define               OFFSETOF__PtrArray__m_NumComponents 0x8
ASMCONSTANTS_C_ASSERT(OFFSETOF__PtrArray__m_NumComponents
                    == offsetof(PtrArray, m_NumComponents));

#define               OFFSETOF__PtrArray__m_Array 0x10
ASMCONSTANTS_C_ASSERT(OFFSETOF__PtrArray__m_Array
                    == offsetof(PtrArray, m_Array));


#define MethodDescClassification__mdcClassification 0x7
ASMCONSTANTS_C_ASSERT(MethodDescClassification__mdcClassification == mdcClassification);

#define MethodDescClassification__mcInstantiated 0x5
ASMCONSTANTS_C_ASSERT(MethodDescClassification__mcInstantiated == mcInstantiated);

#ifndef FEATURE_PAL

#define OFFSET__TEB__TlsSlots 0x1480
ASMCONSTANTS_C_ASSERT(OFFSET__TEB__TlsSlots == offsetof(TEB, TlsSlots));

#define OFFSETOF__TEB__LastErrorValue 0x68
ASMCONSTANTS_C_ASSERT(OFFSETOF__TEB__LastErrorValue == offsetof(TEB, LastErrorValue));

#endif // !FEATURE_PAL

#ifdef _DEBUG
#define TLS_GETTER_MAX_SIZE_ASM 0x30
#else
#define TLS_GETTER_MAX_SIZE_ASM 0x18
#endif
ASMCONSTANTS_C_ASSERT(TLS_GETTER_MAX_SIZE_ASM == TLS_GETTER_MAX_SIZE)


// If you change these constants, you need to update code in
// RedirectHandledJITCase.asm and ExcepAMD64.cpp.
#define REDIRECTSTUB_ESTABLISHER_OFFSET_RBP 0
#define REDIRECTSTUB_RBP_OFFSET_CONTEXT     0x20

#define THROWSTUB_ESTABLISHER_OFFSET_FaultingExceptionFrame 0x30

#define UMTHUNKSTUB_HOST_NOTIFY_FLAG_RBPOFFSET (0x40)   // xmm save size

#define Thread__ObjectRefFlush  ?ObjectRefFlush@Thread@@SAXPEAV1@@Z 


#define                     DELEGATE_FIELD_OFFSET__METHOD_AUX           0x20
ASMCONSTANTS_RUNTIME_ASSERT(DELEGATE_FIELD_OFFSET__METHOD_AUX == Object::GetOffsetOfFirstField() +
        MscorlibBinder::GetFieldOffset(FIELD__DELEGATE__METHOD_PTR_AUX));


#define ASM_LARGE_OBJECT_SIZE 85000
ASMCONSTANTS_C_ASSERT(ASM_LARGE_OBJECT_SIZE == LARGE_OBJECT_SIZE);

#define               OFFSETOF__ArrayBase__m_NumComponents 8
ASMCONSTANTS_C_ASSERT(OFFSETOF__ArrayBase__m_NumComponents
                    == offsetof(ArrayBase, m_NumComponents));

#define               OFFSETOF__StringObject__m_StringLength 0x8
ASMCONSTANTS_C_ASSERT(OFFSETOF__StringObject__m_StringLength
                    == offsetof(StringObject, m_StringLength));

#define               OFFSETOF__ArrayTypeDesc__m_TemplateMT 8
ASMCONSTANTS_C_ASSERT(OFFSETOF__ArrayTypeDesc__m_TemplateMT
                    == offsetof(ArrayTypeDesc, m_TemplateMT));

#define               OFFSETOF__ArrayTypeDesc__m_Arg 0x10
ASMCONSTANTS_C_ASSERT(OFFSETOF__ArrayTypeDesc__m_Arg
                    == offsetof(ArrayTypeDesc, m_Arg));

#define               SYNCBLOCKINDEX_OFFSET 0x4
ASMCONSTANTS_C_ASSERT(SYNCBLOCKINDEX_OFFSET
                    == (sizeof(ObjHeader) - offsetof(ObjHeader, m_SyncBlockValue)));

#define CallDescrData__pSrc                0x00
#define CallDescrData__numStackSlots       0x08
#ifdef UNIX_AMD64_ABI
#define CallDescrData__pArgumentRegisters  0x10
#define CallDescrData__pFloatArgumentRegisters 0x18
#define CallDescrData__fpReturnSize        0x20
#define CallDescrData__pTarget             0x28
#define CallDescrData__returnValue         0x30
#else
#define CallDescrData__dwRegTypeMap        0x10
#define CallDescrData__fpReturnSize        0x18
#define CallDescrData__pTarget             0x20
#define CallDescrData__returnValue         0x28
#endif

ASMCONSTANTS_C_ASSERT(CallDescrData__pSrc                 == offsetof(CallDescrData, pSrc))
ASMCONSTANTS_C_ASSERT(CallDescrData__numStackSlots        == offsetof(CallDescrData, numStackSlots))
#ifdef UNIX_AMD64_ABI
ASMCONSTANTS_C_ASSERT(CallDescrData__pArgumentRegisters   == offsetof(CallDescrData, pArgumentRegisters))
ASMCONSTANTS_C_ASSERT(CallDescrData__pFloatArgumentRegisters == offsetof(CallDescrData, pFloatArgumentRegisters))
#else
ASMCONSTANTS_C_ASSERT(CallDescrData__dwRegTypeMap         == offsetof(CallDescrData, dwRegTypeMap))
#endif
ASMCONSTANTS_C_ASSERT(CallDescrData__fpReturnSize         == offsetof(CallDescrData, fpReturnSize))
ASMCONSTANTS_C_ASSERT(CallDescrData__pTarget              == offsetof(CallDescrData, pTarget))
ASMCONSTANTS_C_ASSERT(CallDescrData__returnValue          == offsetof(CallDescrData, returnValue))

#ifdef UNIX_AMD64_ABI
#define OFFSETOF__TransitionBlock__m_argumentRegisters    0x00
ASMCONSTANTS_C_ASSERT(OFFSETOF__TransitionBlock__m_argumentRegisters == offsetof(TransitionBlock, m_argumentRegisters))
#endif // UNIX_AMD64_ABI

#undef ASMCONSTANTS_RUNTIME_ASSERT
#undef ASMCONSTANTS_C_ASSERT
#ifndef UNIX_AMD64_ABI
#undef DBG_FRE
#endif // UNIX_AMD64_ABI


//#define USE_COMPILE_TIME_CONSTANT_FINDER // Uncomment this line to use the constant finder
#if defined(__cplusplus) && defined(USE_COMPILE_TIME_CONSTANT_FINDER)
// This class causes the compiler to emit an error with the constant we're interested in
// in the error message. This is useful if a size or offset changes. To use, comment out
// the compile-time assert that is firing, enable the constant finder, add the appropriate
// constant to find to BogusFunction(), and build.
// 
// Here's a sample compiler error:
// d:\dd\clr\src\ndp\clr\src\vm\i386\asmconstants.h(326) : error C2248: 'FindCompileTimeConstant<N>::FindCompileTimeConstant' : cannot access private member declared in class 'FindCompileTimeConstant<N>'
//         with
//         [
//             N=1520
//         ]
//         d:\dd\clr\src\ndp\clr\src\vm\i386\asmconstants.h(321) : see declaration of 'FindCompileTimeConstant<N>::FindCompileTimeConstant'
//         with
//         [
//             N=1520
//         ]
template<size_t N>
class FindCompileTimeConstant
{
private:
	FindCompileTimeConstant();
};

void BogusFunction()
{
	// Sample usage to generate the error
	FindCompileTimeConstant<offsetof(Thread, m_pDomain)> bogus_variable;
	FindCompileTimeConstant<offsetof(Thread, m_ExceptionState)> bogus_variable2;
}
#endif // defined(__cplusplus) && defined(USE_COMPILE_TIME_CONSTANT_FINDER)
