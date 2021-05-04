// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*****************************************************************************\
*                                                                             *
* CorJit.h -    EE / JIT interface                                            *
*                                                                             *
*               Version 1.0                                                   *
*******************************************************************************
*                                                                             *
*                                                                     *
*                                                                             *
\*****************************************************************************/

//////////////////////////////////////////////////////////////////////////////////////////////////////////
//
// NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE
//
// The JIT/EE interface is versioned. By "interface", we mean mean any and all communication between the
// JIT and the EE. Any time a change is made to the interface, the JIT/EE interface version identifier
// must be updated. See code:JITEEVersionIdentifier for more information.
//
// NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE
//
//////////////////////////////////////////////////////////////////////////////////////////////////////////

#ifndef _COR_JIT_H_
#define _COR_JIT_H_

#include "corinfo.h"

#include <stdarg.h>

#include "corjitflags.h"


#ifndef MAKE_HRESULT
// If this header is included without including the windows or PAL headers, then define
// MAKE_HRESULT, and associated macros

/******************* HRESULT types ****************************************/

#define FACILITY_WINDOWS                 8
#define FACILITY_URT                     19
#define FACILITY_UMI                     22
#define FACILITY_SXS                     23
#define FACILITY_STORAGE                 3
#define FACILITY_SSPI                    9
#define FACILITY_SCARD                   16
#define FACILITY_SETUPAPI                15
#define FACILITY_SECURITY                9
#define FACILITY_RPC                     1
#define FACILITY_WIN32                   7
#define FACILITY_CONTROL                 10
#define FACILITY_NULL                    0
#define FACILITY_MSMQ                    14
#define FACILITY_MEDIASERVER             13
#define FACILITY_INTERNET                12
#define FACILITY_ITF                     4
#define FACILITY_DPLAY                   21
#define FACILITY_DISPATCH                2
#define FACILITY_COMPLUS                 17
#define FACILITY_CERT                    11
#define FACILITY_ACS                     20
#define FACILITY_AAF                     18

#define NO_ERROR 0L

#define SEVERITY_SUCCESS    0
#define SEVERITY_ERROR      1

#define SUCCEEDED(Status) ((JITINTERFACE_HRESULT)(Status) >= 0)
#define FAILED(Status) ((JITINTERFACE_HRESULT)(Status)<0)
#define IS_ERROR(Status) ((uint32_t)(Status) >> 31 == SEVERITY_ERROR) // diff from win32
#define HRESULT_CODE(hr)    ((hr) & 0xFFFF)
#define SCODE_CODE(sc)      ((sc) & 0xFFFF)
#define HRESULT_FACILITY(hr)  (((hr) >> 16) & 0x1fff)
#define SCODE_FACILITY(sc)    (((sc) >> 16) & 0x1fff)
#define HRESULT_SEVERITY(hr)  (((hr) >> 31) & 0x1)
#define SCODE_SEVERITY(sc)    (((sc) >> 31) & 0x1)

// both macros diff from Win32
#define MAKE_HRESULT(sev,fac,code) \
    ((JITINTERFACE_HRESULT) (((uint32_t)(sev)<<31) | ((uint32_t)(fac)<<16) | ((uint32_t)(code))) )
#define MAKE_SCODE(sev,fac,code) \
    ((SCODE) (((uint32_t)(sev)<<31) | ((uint32_t)(fac)<<16) | ((LONG)(code))) )

#define FACILITY_NT_BIT                 0x10000000
#define HRESULT_FROM_WIN32(x) ((JITINTERFACE_HRESULT)(x) <= 0 ? ((JITINTERFACE_HRESULT)(x)) : ((JITINTERFACE_HRESULT) (((x) & 0x0000FFFF) | (FACILITY_WIN32 << 16) | 0x80000000)))
#define __HRESULT_FROM_WIN32(x) HRESULT_FROM_WIN32(x)

#define HRESULT_FROM_NT(x)      ((JITINTERFACE_HRESULT) ((x) | FACILITY_NT_BIT))
#endif // MAKE_HRESULT

/*****************************************************************************/
    // These are error codes returned by CompileMethod
enum CorJitResult
{
    // Note that I dont use FACILITY_NULL for the facility number,
    // we may want to get a 'real' facility number
    CORJIT_OK            =     NO_ERROR,
    CORJIT_BADCODE       =     MAKE_HRESULT(SEVERITY_ERROR,FACILITY_NULL, 1),
    CORJIT_OUTOFMEM      =     MAKE_HRESULT(SEVERITY_ERROR,FACILITY_NULL, 2),
    CORJIT_INTERNALERROR =     MAKE_HRESULT(SEVERITY_ERROR,FACILITY_NULL, 3),
    CORJIT_SKIPPED       =     MAKE_HRESULT(SEVERITY_ERROR,FACILITY_NULL, 4),
    CORJIT_RECOVERABLEERROR =  MAKE_HRESULT(SEVERITY_ERROR,FACILITY_NULL, 5),
    CORJIT_IMPLLIMITATION=     MAKE_HRESULT(SEVERITY_ERROR,FACILITY_NULL, 6),
};

/*****************************************************************************/
// These are flags passed to ICorJitInfo::allocMem
// to guide the memory allocation for the code, readonly data, and read-write data
enum CorJitAllocMemFlag
{
    CORJIT_ALLOCMEM_DEFAULT_CODE_ALIGN = 0x00000000, // The code will be use the normal alignment
    CORJIT_ALLOCMEM_FLG_16BYTE_ALIGN   = 0x00000001, // The code will be 16-byte aligned
    CORJIT_ALLOCMEM_FLG_RODATA_16BYTE_ALIGN = 0x00000002, // The read-only data will be 16-byte aligned
    CORJIT_ALLOCMEM_FLG_32BYTE_ALIGN   = 0x00000004, // The code will be 32-byte aligned
    CORJIT_ALLOCMEM_FLG_RODATA_32BYTE_ALIGN = 0x00000008, // The read-only data will be 32-byte aligned
};

inline CorJitAllocMemFlag operator |(CorJitAllocMemFlag a, CorJitAllocMemFlag b)
{
    return static_cast<CorJitAllocMemFlag>(static_cast<int>(a) | static_cast<int>(b));
}

enum CorJitFuncKind
{
    CORJIT_FUNC_ROOT,          // The main/root function (always id==0)
    CORJIT_FUNC_HANDLER,       // a funclet associated with an EH handler (finally, fault, catch, filter handler)
    CORJIT_FUNC_FILTER         // a funclet associated with an EH filter
};

// We have a performance-investigation mode (defined by the FEATURE_USE_ASM_GC_WRITE_BARRIERS and
// FEATURE_COUNT_GC_WRITE_BARRIER preprocessor symbols) in which the JIT adds an argument of this
// enumeration to checked write barrier calls in order to classify them.
enum CheckedWriteBarrierKinds {
    CWBKind_Unclassified,    // Not one of the ones below.
    CWBKind_RetBuf,          // Store through a return buffer pointer argument.
    CWBKind_ByRefArg,        // Store through a by-ref argument (not an implicit return buffer).
    CWBKind_OtherByRefLocal, // Store through a by-ref local variable.
    CWBKind_AddrOfLocal,     // Store through the address of a local (arguably a bug that this happens at all).
};

#include "corjithost.h"

extern "C" void jitStartup(ICorJitHost* host);

class ICorJitCompiler;
class ICorJitInfo;

extern "C" ICorJitCompiler* getJit();

// #EEToJitInterface
// ICorJitCompiler is the interface that the EE uses to get IL bytecode converted to native code. Note that
// to accomplish this the JIT has to call back to the EE to get symbolic information.  The code:ICorJitInfo
// type passed as 'comp' to compileMethod is the mechanism to get this information.  This is often the more
// interesting interface.
//
//
class ICorJitCompiler
{
public:
    // compileMethod is the main routine to ask the JIT Compiler to create native code for a method. The
    // method to be compiled is passed in the 'info' parameter, and the code:ICorJitInfo is used to allow the
    // JIT to resolve tokens, and make any other callbacks needed to create the code. nativeEntry, and
    // nativeSizeOfCode are just for convenience because the JIT asks the EE for the memory to emit code into
    // (see code:ICorJitInfo.allocMem), so really the EE already knows where the method starts and how big
    // it is (in fact, it could be in more than one chunk).
    //
    // * In the 32 bit jit this is implemented by code:CILJit.compileMethod
    // * For the 64 bit jit this is implemented by code:PreJit.compileMethod
    virtual CorJitResult compileMethod (
            ICorJitInfo                 *comp,               /* IN */
            struct CORINFO_METHOD_INFO  *info,               /* IN */
            unsigned /* code:CorJitFlag */   flags,          /* IN */
            uint8_t                        **nativeEntry,       /* OUT */
            uint32_t                       *nativeSizeOfCode    /* OUT */
            ) = 0;

    // Do any appropriate work at process shutdown.  Default impl is to do nothing.
    virtual void ProcessShutdownWork(ICorStaticInfo* info) {};

    // The EE asks the JIT for a "version identifier". This represents the version of the JIT/EE interface.
    // If the JIT doesn't implement the same JIT/EE interface expected by the EE (because the JIT doesn't
    // return the version identifier that the EE expects), then the EE fails to load the JIT.
    //
    virtual void getVersionIdentifier(
            GUID*   versionIdentifier   /* OUT */
            ) = 0;

    // When the EE loads the System.Numerics.Vectors assembly, it asks the JIT what length (in bytes) of
    // SIMD vector it supports as an intrinsic type.  Zero means that the JIT does not support SIMD
    // intrinsics, so the EE should use the default size (i.e. the size of the IL implementation).
    virtual unsigned getMaxIntrinsicSIMDVectorLength(CORJIT_FLAGS cpuCompileFlags) { return 0; }
};

//------------------------------------------------------------------------------------------
// #JitToEEInterface
//
// ICorJitInfo is the main interface that the JIT uses to call back to the EE and get information. It is
// the companion to code:ICorJitCompiler#EEToJitInterface. The concrete implementation of this in the
// runtime is the code:CEEJitInfo type.  There is also a version of this for the NGEN case.
//
// See code:ICorMethodInfo#EEJitContractDetails for subtle conventions used by this interface.
//
// There is more information on the JIT in the book of the runtime entry
// http://devdiv/sites/CLR/Product%20Documentation/2.0/BookOfTheRuntime/JIT/JIT%20Design.doc
//
class ICorJitInfo : public ICorDynamicInfo
{
public:
    // get a block of memory for the code, readonly data, and read-write data
    virtual void allocMem (
            uint32_t               hotCodeSize,    /* IN */
            uint32_t               coldCodeSize,   /* IN */
            uint32_t               roDataSize,     /* IN */
            uint32_t               xcptnsCount,    /* IN */
            CorJitAllocMemFlag  flag,           /* IN */
            void **             hotCodeBlock,   /* OUT */
            void **             coldCodeBlock,  /* OUT */
            void **             roDataBlock     /* OUT */
            ) = 0;

    // Reserve memory for the method/funclet's unwind information.
    // Note that this must be called before allocMem. It should be
    // called once for the main method, once for every funclet, and
    // once for every block of cold code for which allocUnwindInfo
    // will be called.
    //
    // This is necessary because jitted code must allocate all the
    // memory needed for the unwindInfo at the allocMem call.
    // For prejitted code we split up the unwinding information into
    // separate sections .rdata and .pdata.
    //
    virtual void reserveUnwindInfo (
            bool                isFunclet,             /* IN */
            bool                isColdCode,            /* IN */
            uint32_t               unwindSize             /* IN */
            ) = 0;

    // Allocate and initialize the .rdata and .pdata for this method or
    // funclet, and get the block of memory needed for the machine-specific
    // unwind information (the info for crawling the stack frame).
    // Note that allocMem must be called first.
    //
    // Parameters:
    //
    //    pHotCode        main method code buffer, always filled in
    //    pColdCode       cold code buffer, only filled in if this is cold code,
    //                      null otherwise
    //    startOffset     start of code block, relative to appropriate code buffer
    //                      (e.g. pColdCode if cold, pHotCode if hot).
    //    endOffset       end of code block, relative to appropriate code buffer
    //    unwindSize      size of unwind info pointed to by pUnwindBlock
    //    pUnwindBlock    pointer to unwind info
    //    funcKind        type of funclet (main method code, handler, filter)
    //
    virtual void allocUnwindInfo (
            uint8_t *              pHotCode,              /* IN */
            uint8_t *              pColdCode,             /* IN */
            uint32_t               startOffset,           /* IN */
            uint32_t               endOffset,             /* IN */
            uint32_t               unwindSize,            /* IN */
            uint8_t *              pUnwindBlock,          /* IN */
            CorJitFuncKind      funcKind               /* IN */
            ) = 0;

        // Get a block of memory needed for the code manager information,
        // (the info for enumerating the GC pointers while crawling the
        // stack frame).
        // Note that allocMem must be called first
    virtual void * allocGCInfo (
            size_t                  size        /* IN */
            ) = 0;

    // Indicate how many exception handler blocks are to be returned.
    // This is guaranteed to be called before any 'setEHinfo' call.
    // Note that allocMem must be called before this method can be called.
    virtual void setEHcount (
            unsigned                cEH          /* IN */
            ) = 0;

    // Set the values for one particular exception handler block.
    //
    // Handler regions should be lexically contiguous.
    // This is because FinallyIsUnwinding() uses lexicality to
    // determine if a "finally" clause is executing.
    virtual void setEHinfo (
            unsigned                 EHnumber,   /* IN  */
            const CORINFO_EH_CLAUSE *clause      /* IN */
            ) = 0;

    // Level -> fatalError, Level 2 -> Error, Level 3 -> Warning
    // Level 4 means happens 10 times in a run, level 5 means 100, level 6 means 1000 ...
    // returns non-zero if the logging succeeded
    virtual bool logMsg(unsigned level, const char* fmt, va_list args) = 0;

    // do an assert.  will return true if the code should retry (DebugBreak)
    // returns false, if the assert should be igored.
    virtual int doAssert(const char* szFile, int iLine, const char* szExpr) = 0;

    virtual void reportFatalError(CorJitResult result) = 0;

    struct BlockCounts  // Also defined by:  CORBBTPROF_BLOCK_DATA
    {
        uint32_t ILOffset;
        uint32_t ExecutionCount;
    };

    // Data structure for a single class probe.
    //
    // CLASS_FLAG and INTERFACE_FLAG are placed into the Other field in the schema
    //
    // Count is the number of times a call was made at that call site.
    //
    // SIZE is the number of entries in the table.
    //
    // SAMPLE_INTERVAL must be >= SIZE. SAMPLE_INTERVAL / SIZE
    // gives the average number of calls between table updates.
    // 
    struct ClassProfile
    {
        enum { 
            SIZE = 8, 
            SAMPLE_INTERVAL = 32, 
            CLASS_FLAG     = 0x80000000, 
            INTERFACE_FLAG = 0x40000000,
            OFFSET_MASK    = 0x3FFFFFFF
        };

        uint32_t Count;
        CORINFO_CLASS_HANDLE ClassTable[SIZE];
    };

    enum class PgoInstrumentationKind
    {
        // This must be kept in sync with PgoInstrumentationKind in PgoFormat.cs

        // Schema data types
        None = 0,
        FourByte = 1,
        EightByte = 2,
        TypeHandle = 3,

        // Mask of all schema data types
        MarshalMask = 0xF,

        // ExcessAlignment
        Align4Byte = 0x10,
        Align8Byte = 0x20,
        AlignPointer = 0x30,

        // Mask of all schema alignment types
        AlignMask = 0x30,

        DescriptorMin = 0x40,

        Done = None, // All instrumentation schemas must end with a record which is "Done"
        BasicBlockIntCount = (DescriptorMin * 1) | FourByte, // basic block counter using unsigned 4 byte int
        BasicBlockLongCount = (DescriptorMin * 1) | EightByte, // basic block counter using unsigned 8 byte int
        TypeHandleHistogramCount = (DescriptorMin * 2) | FourByte | AlignPointer, // 4 byte counter that is part of a type histogram
        TypeHandleHistogramTypeHandle = (DescriptorMin * 3) | TypeHandle, // TypeHandle that is part of a type histogram
        Version = (DescriptorMin * 4) | None, // Version is encoded in the Other field of the schema
        NumRuns = (DescriptorMin * 5) | None, // Number of runs is encoded in the Other field of the schema
        EdgeIntCount = (DescriptorMin * 6) | FourByte, // edge counter using unsigned 4 byte int
        EdgeLongCount = (DescriptorMin * 6) | EightByte, // edge counter using unsigned 8 byte int
        GetLikelyClass = (DescriptorMin * 7) | TypeHandle, // Compressed get likely class data
    };

    struct PgoInstrumentationSchema
    {
        size_t Offset;
        PgoInstrumentationKind InstrumentationKind;
        int32_t ILOffset;
        int32_t Count;
        int32_t Other;
    };

#define DEFAULT_UNKNOWN_TYPEHANDLE 1
#define UNKNOWN_TYPEHANDLE_MIN 1
#define UNKNOWN_TYPEHANDLE_MAX 33

    static inline bool IsUnknownTypeHandle(intptr_t typeHandle)
    {
        return ((typeHandle >= UNKNOWN_TYPEHANDLE_MIN) && (typeHandle <= UNKNOWN_TYPEHANDLE_MAX));
    }

    // get profile information to be used for optimizing a current method.  The format
    // of the buffer is the same as the format the JIT passes to allocPgoInstrumentationBySchema.
    virtual JITINTERFACE_HRESULT getPgoInstrumentationResults(
            CORINFO_METHOD_HANDLE      ftnHnd,
            PgoInstrumentationSchema **pSchema,                    // OUT: pointer to the schema table (array) which describes the instrumentation results
                                                                   // (pointer will not remain valid after jit completes).
            uint32_t *                 pCountSchemaItems,          // OUT: pointer to the count of schema items in `pSchema` array.
            uint8_t **                 pInstrumentationData        // OUT: `*pInstrumentationData` is set to the address of the instrumentation data
                                                                   // (pointer will not remain valid after jit completes).
            ) = 0;

    // Allocate a profile buffer for use in the current process
    // The JIT shall call this api with the schema entries other than Offset filled in.
    // The VM is responsible for allocating the buffer, and computing the various offsets
    // The offset calculation shall obey the following rules
    //  1. All data fields shall be naturally aligned.
    //  2. The first offset may be arbitrarily large.
    //  3. The JIT may mark a schema item with an alignment flag. This may be used to increase the alignment of a field.
    //  4. Each data entry shall be laid out without extra padding.
    //
    //  The intention here is that it becomes possible to describe a C data structure with the alignment for ease of use with 
    //  instrumentation helper functions
    virtual JITINTERFACE_HRESULT allocPgoInstrumentationBySchema(
            CORINFO_METHOD_HANDLE     ftnHnd,
            PgoInstrumentationSchema *pSchema,                     // IN OUT: pointer to the schema table (array) which describes the instrumentation results. `Offset` field
                                                                   // is filled in by VM; other fields are set and passed in by caller.
            uint32_t                  countSchemaItems,            // IN: count of schema items in `pSchema` array.
            uint8_t **                pInstrumentationData         // OUT: `*pInstrumentationData` is set to the address of the instrumentation data.
            ) = 0;

    // Associates a native call site, identified by its offset in the native code stream, with
    // the signature information and method handle the JIT used to lay out the call site. If
    // the call site has no signature information (e.g. a helper call) or has no method handle
    // (e.g. a CALLI P/Invoke), then null should be passed instead.
    virtual void recordCallSite(
            uint32_t                 instrOffset,  /* IN */
            CORINFO_SIG_INFO *    callSig,      /* IN */
            CORINFO_METHOD_HANDLE methodHandle  /* IN */
            ) = 0;

    // A relocation is recorded if we are pre-jitting.
    // A jump thunk may be inserted if we are jitting
    virtual void recordRelocation(
            void *                 location,   /* IN  */
            void *                 target,     /* IN  */
            uint16_t                   fRelocType, /* IN  */
            uint16_t                   slotNum = 0,  /* IN  */
            int32_t                  addlDelta = 0 /* IN  */
            ) = 0;

    virtual uint16_t getRelocTypeHint(void * target) = 0;

    // For what machine does the VM expect the JIT to generate code? The VM
    // returns one of the IMAGE_FILE_MACHINE_* values. Note that if the VM
    // is cross-compiling (such as the case for crossgen), it will return a
    // different value than if it was compiling for the host architecture.
    //
    virtual uint32_t getExpectedTargetArchitecture() = 0;

    // Fetches extended flags for a particular compilation instance. Returns
    // the number of bytes written to the provided buffer.
    virtual uint32_t getJitFlags(
        CORJIT_FLAGS* flags,       /* IN: Points to a buffer that will hold the extended flags. */
        uint32_t        sizeInBytes   /* IN: The size of the buffer. Note that this is effectively a
                                          version number for the CORJIT_FLAGS value. */
        ) = 0;
};

/**********************************************************************************/
#endif // _COR_CORJIT_H_
