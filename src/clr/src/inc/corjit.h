//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

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

#include <corinfo.h>

#define CORINFO_STACKPROBE_DEPTH        256*sizeof(UINT_PTR)          // Guaranteed stack until an fcall/unmanaged
                                                    // code can set up a frame. Please make sure
                                                    // this is less than a page. This is due to
                                                    // 2 reasons:
                                                    //
                                                    // If we need to probe more than a page
                                                    // size, we need one instruction per page
                                                    // (7 bytes per instruction)
                                                    //
                                                    // The JIT wants some safe space so it doesn't
                                                    // have to put a probe on every call site. It achieves
                                                    // this by probing n bytes more than CORINFO_STACKPROBE_DEPTH
                                                    // If it hasn't used more than n for its own stuff, it
                                                    // can do a call without doing any other probe
                                                    //
                                                    // In any case, we do really expect this define to be
                                                    // small, as setting up a frame should be only pushing
                                                    // a couple of bytes on the stack
                                                    //
                                                    // There is a compile time assert
                                                    // in the x86 jit to protect you from this
                                                    //




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
    CORJIT_SKIPMDIL      =     MAKE_HRESULT(SEVERITY_ERROR,FACILITY_NULL, 6)
};


/* values for flags in compileMethod */

enum CorJitFlag
{
    CORJIT_FLG_SPEED_OPT           = 0x00000001,
    CORJIT_FLG_SIZE_OPT            = 0x00000002,
    CORJIT_FLG_DEBUG_CODE          = 0x00000004, // generate "debuggable" code (no code-mangling optimizations)
    CORJIT_FLG_DEBUG_EnC           = 0x00000008, // We are in Edit-n-Continue mode
    CORJIT_FLG_DEBUG_INFO          = 0x00000010, // generate line and local-var info
    CORJIT_FLG_MIN_OPT             = 0x00000020, // disable all jit optimizations (not necesarily debuggable code)
    CORJIT_FLG_GCPOLL_CALLS        = 0x00000040, // Emit calls to JIT_POLLGC for thread suspension.
    CORJIT_FLG_MCJIT_BACKGROUND    = 0x00000080, // Calling from multicore JIT background thread, do not call JitComplete

#ifdef FEATURE_LEGACYNETCF

    CORJIT_FLG_NETCF_QUIRKS        = 0x00000100, // Mimic .NetCF JIT's quirks for generated code (currently just inlining heuristics)

#else // FEATURE_LEGACYNETCF

    CORJIT_FLG_UNUSED1             = 0x00000100,

#endif // FEATURE_LEGACYNETCF

#if defined(_TARGET_X86_)

    CORJIT_FLG_PINVOKE_RESTORE_ESP = 0x00000200, // Restore ESP after returning from inlined PInvoke
    CORJIT_FLG_TARGET_P4           = 0x00000400,
    CORJIT_FLG_USE_FCOMI           = 0x00000800, // Generated code may use fcomi(p) instruction
    CORJIT_FLG_USE_CMOV            = 0x00001000, // Generated code may use cmov instruction
    CORJIT_FLG_USE_SSE2            = 0x00002000, // Generated code may use SSE-2 instructions

#elif defined(_TARGET_AMD64_)

    CORJIT_FLG_USE_SSE3_4          = 0x00000200,
    CORJIT_FLG_USE_AVX             = 0x00000400,
    CORJIT_FLG_USE_AVX2            = 0x00000800,
    CORJIT_FLG_USE_AVX_512         = 0x00001000,
    CORJIT_FLG_FEATURE_SIMD        = 0x00002000,

#else // !defined(_TARGET_X86_) && !defined(_TARGET_AMD64_)

    CORJIT_FLG_UNUSED2             = 0x00000200,
    CORJIT_FLG_UNUSED3             = 0x00000400,
    CORJIT_FLG_UNUSED4             = 0x00000800,
    CORJIT_FLG_UNUSED5             = 0x00001000,
    CORJIT_FLG_UNUSED6             = 0x00002000,

#endif // !defined(_TARGET_X86_) && !defined(_TARGET_AMD64_)

#ifdef MDIL
    CORJIT_FLG_MDIL                = 0x00004000, // Generate MDIL code instead of machine code

    // Safe to overlap with CORJIT_FLG_MAKEFINALCODE below. Not used by the JIT, used internally by NGen only.
    CORJIT_FLG_MINIMAL_MDIL        = 0x00008000, // Generate MDIL code suitable for use to bind other assemblies.

    // Safe to overlap with CORJIT_FLG_READYTORUN below. Not used by the JIT, used internally by NGen only.
    CORJIT_FLG_NO_MDIL             = 0x00010000, // Generate an MDIL section but no code or CTL. Not used by the JIT, used internally by NGen only.
#else // MDIL
    CORJIT_FLG_PINVOKE_USE_HELPERS = 0x00004000, // Use JIT_PINVOKE_{BEGIN,END} helpers instead of generating transitions inline.
#endif

#if defined(FEATURE_INTERPRETER)
    CORJIT_FLG_MAKEFINALCODE       = 0x00008000, // Use the final code generator, i.e., not the interpreter.
#endif // FEATURE_INTERPRETER

#ifdef FEATURE_READYTORUN_COMPILER
    CORJIT_FLG_READYTORUN          = 0x00010000, // Use version-resilient code generation
#endif

    CORJIT_FLG_PROF_ENTERLEAVE     = 0x00020000, // Instrument prologues/epilogues
    CORJIT_FLG_PROF_REJIT_NOPS     = 0x00040000, // Insert NOPs to ensure code is re-jitable
    CORJIT_FLG_PROF_NO_PINVOKE_INLINE
                                   = 0x00080000, // Disables PInvoke inlining
    CORJIT_FLG_SKIP_VERIFICATION   = 0x00100000, // (lazy) skip verification - determined without doing a full resolve. See comment below
    CORJIT_FLG_PREJIT              = 0x00200000, // jit or prejit is the execution engine.
    CORJIT_FLG_RELOC               = 0x00400000, // Generate relocatable code
    CORJIT_FLG_IMPORT_ONLY         = 0x00800000, // Only import the function
    CORJIT_FLG_IL_STUB             = 0x01000000, // method is an IL stub
    CORJIT_FLG_PROCSPLIT           = 0x02000000, // JIT should separate code into hot and cold sections
    CORJIT_FLG_BBINSTR             = 0x04000000, // Collect basic block profile information
    CORJIT_FLG_BBOPT               = 0x08000000, // Optimize method based on profile information
    CORJIT_FLG_FRAMED              = 0x10000000, // All methods have an EBP frame
    CORJIT_FLG_ALIGN_LOOPS         = 0x20000000, // add NOPs before loops to align them at 16 byte boundaries
    CORJIT_FLG_PUBLISH_SECRET_PARAM= 0x40000000, // JIT must place stub secret param into local 0.  (used by IL stubs)
    CORJIT_FLG_GCPOLL_INLINE       = 0x80000000, // JIT must inline calls to GCPoll when possible
};

enum CorJitFlag2
{
#ifdef FEATURE_STACK_SAMPLING
    CORJIT_FLG2_SAMPLING_JIT_BACKGROUND  
                                   = 0x00000001, // JIT is being invoked as a result of stack sampling for hot methods in the background
#endif
};

/*****************************************************************************
Here is how CORJIT_FLG_SKIP_VERIFICATION should be interepreted.
Note that even if any method is inlined, it need not be verified.

if (CORJIT_FLG_SKIP_VERIFICATION is passed in to ICorJitCompiler::compileMethod())
{
    No verification needs to be done.
    Just compile the method, generating unverifiable code if necessary
}
else
{
    switch(ICorMethodInfo::isInstantiationOfVerifiedGeneric())
    {
    case INSTVER_NOT_INSTANTIATION:

        //
        // Non-generic case, or open generic instantiation
        //

        switch(canSkipMethodVerification())
        {
        case CORINFO_VERIFICATION_CANNOT_SKIP:
            {
                ICorMethodInfo::initConstraintsForVerification(&circularConstraints)
                if (circularConstraints)
                {
                    Just emit code to call CORINFO_HELP_VERIFICATION
                    The IL will not be compiled
                }
                else
                {
                    Verify the method.
                    if (unverifiable code is detected)
                    {
                        In place of branches with unverifiable code, emit code to call CORINFO_HELP_VERIFICATION
                        Mark the method (and any of its instantiations) as unverifiable
                    }
                    Compile the rest of the verifiable code
                }
            }

        case CORINFO_VERIFICATION_CAN_SKIP:
            {
                No verification needs to be done.
                Just compile the method, generating unverifiable code if necessary
            }

        case CORINFO_VERIFICATION_RUNTIME_CHECK:
            {
                ICorMethodInfo::initConstraintsForVerification(&circularConstraints)
                if (circularConstraints)
                {
                    Just emit code to call CORINFO_HELP_VERIFICATION
                    The IL will not be compiled

                    TODO: This could be changed to call CORINFO_HELP_VERIFICATION_RUNTIME_CHECK
                }
                else
                {
                    Verify the method.
                    if (unverifiable code is detected)
                    {
                        In the prolog, emit code to call CORINFO_HELP_VERIFICATION_RUNTIME_CHECK
                        Mark the method (and any of its instantiations) as unverifiable
                    }
                    Compile the method, generating unverifiable code if necessary
                }
            }
        case CORINFO_VERIFICATION_DONT_JIT:
            {
                ICorMethodInfo::initConstraintsForVerification(&circularConstraints)
                if (circularConstraints)
                {
                    Just emit code to call CORINFO_HELP_VERIFICATION
                    The IL will not be compiled
                }
                else
                {
                    Verify the method.
                    if (unverifiable code is detected)
                    {
                        Fail the jit
                    }
                }
            }
        }

    case INSTVER_GENERIC_PASSED_VERIFICATION:
        {
            This cannot ever happen because the VM would pass in CORJIT_FLG_SKIP_VERIFICATION.
        }

    case INSTVER_GENERIC_FAILED_VERIFICATION:

        switch(canSkipMethodVerification())
        {
            case CORINFO_VERIFICATION_CANNOT_SKIP:
                {
                    This cannot be supported because the compiler does not know which branches should call CORINFO_HELP_VERIFICATION.
                    The CLR will throw a VerificationException instead of trying to compile this method
                }

            case CORINFO_VERIFICATION_CAN_SKIP:
                {
                    This cannot ever happen because the CLR would pass in CORJIT_FLG_SKIP_VERIFICATION.
                }

            case CORINFO_VERIFICATION_RUNTIME_CHECK:
                {
                    No verification needs to be done.
                    In the prolog, emit code to call CORINFO_HELP_VERIFICATION_RUNTIME_CHECK
                    Compile the method, generating unverifiable code if necessary
                }
            case CORINFO_VERIFICATION_DONT_JIT:
                {
                    Fail the jit
                }
        }
    }
}

*/

/*****************************************************************************/
// These are flags passed to ICorJitInfo::allocMem
// to guide the memory allocation for the code, readonly data, and read-write data
enum CorJitAllocMemFlag
{
    CORJIT_ALLOCMEM_DEFAULT_CODE_ALIGN = 0x00000000, // The code will be use the normal alignment
    CORJIT_ALLOCMEM_FLG_16BYTE_ALIGN   = 0x00000001, // The code will be 16-byte aligned
    CORJIT_ALLOCMEM_FLG_RODATA_16BYTE_ALIGN = 0x00000002, // The read-only data will be 16-byte aligned
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

#if !defined(FEATURE_USE_ASM_GC_WRITE_BARRIERS) && defined(FEATURE_COUNT_GC_WRITE_BARRIERS)
// We have a performance-investigation mode (defined by the FEATURE settings above) in which the
// JIT adds an argument of this enumeration to checked write barrier calls, to classify them.
enum CheckedWriteBarrierKinds {
    CWBKind_Unclassified,    // Not one of the ones below.
    CWBKind_RetBuf,          // Store through a return buffer pointer argument.
    CWBKind_ByRefArg,        // Store through a by-ref argument (not an implicit return buffer).
    CWBKind_OtherByRefLocal, // Store through a by-ref local variable.
    CWBKind_AddrOfLocal,     // Store through the address of a local (arguably a bug that this happens at all).
};
#endif

class ICorJitCompiler;
class ICorJitInfo;

struct IEEMemoryManager;

extern "C" ICorJitCompiler* __stdcall getJit();

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
    // 
    // Note: Obfuscators that are hacking the JIT depend on this method having __stdcall calling convention
    virtual CorJitResult __stdcall compileMethod (
            ICorJitInfo                 *comp,               /* IN */
            struct CORINFO_METHOD_INFO  *info,               /* IN */
            unsigned /* code:CorJitFlag */   flags,          /* IN */
            BYTE                        **nativeEntry,       /* OUT */
            ULONG                       *nativeSizeOfCode    /* OUT */
            ) = 0;

    // Some JIT compilers (most notably Phoenix), cache information about EE structures from one invocation
    // of the compiler to the next. This can be a problem when appdomains are unloaded, as some of this
    // cached information becomes stale. The code:ICorJitCompiler.isCacheCleanupRequired is called by the EE
    // early first to see if jit needs these notifications, and if so, the EE will call ClearCache is called
    // whenever the compiler should abandon its cache (eg on appdomain unload)
    virtual void clearCache() = 0;
    virtual BOOL isCacheCleanupRequired() = 0;

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
    virtual unsigned getMaxIntrinsicSIMDVectorLength(DWORD cpuCompileFlags) { return 0; }

    // IL obfuscators sometimes interpose on the EE-JIT interface. This function allows the VM to
    // tell the JIT to use a particular ICorJitCompiler to implement the methods of this interface,
    // and not to implement those methods itself. The JIT must not return this method when getJit()
    // is called. Instead, it must pass along all calls to this interface from within its own
    // ICorJitCompiler implementation. If 'realJitCompiler' is nullptr, then the JIT should resume
    // executing all the functions itself.
    virtual void setRealJit(ICorJitCompiler* realJitCompiler) { }

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
    // return memory manager that the JIT can use to allocate a regular memory
    virtual IEEMemoryManager* getMemoryManager() = 0;

    // get a block of memory for the code, readonly data, and read-write data
    virtual void allocMem (
            ULONG               hotCodeSize,    /* IN */
            ULONG               coldCodeSize,   /* IN */
            ULONG               roDataSize,     /* IN */
            ULONG               xcptnsCount,    /* IN */
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
            BOOL                isFunclet,             /* IN */
            BOOL                isColdCode,            /* IN */
            ULONG               unwindSize             /* IN */
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
            BYTE *              pHotCode,              /* IN */
            BYTE *              pColdCode,             /* IN */
            ULONG               startOffset,           /* IN */
            ULONG               endOffset,             /* IN */
            ULONG               unwindSize,            /* IN */
            BYTE *              pUnwindBlock,          /* IN */
            CorJitFuncKind      funcKind               /* IN */
            ) = 0;

        // Get a block of memory needed for the code manager information,
        // (the info for enumerating the GC pointers while crawling the
        // stack frame).
        // Note that allocMem must be called first
    virtual void * allocGCInfo (
            size_t                  size        /* IN */
            ) = 0;

    virtual void yieldExecution() = 0;

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
    virtual BOOL logMsg(unsigned level, const char* fmt, va_list args) = 0;

    // do an assert.  will return true if the code should retry (DebugBreak)
    // returns false, if the assert should be igored.
    virtual int doAssert(const char* szFile, int iLine, const char* szExpr) = 0;
    
    virtual void reportFatalError(CorJitResult result) = 0;

    struct ProfileBuffer  // Also defined here: code:CORBBTPROF_BLOCK_DATA
    {
        ULONG ILOffset;
        ULONG ExecutionCount;
    };

    // allocate a basic block profile buffer where execution counts will be stored
    // for jitted basic blocks.
    virtual HRESULT allocBBProfileBuffer (
            ULONG                 count,           // The number of basic blocks that we have
            ProfileBuffer **      profileBuffer
            ) = 0;

    // get profile information to be used for optimizing the current method.  The format
    // of the buffer is the same as the format the JIT passes to allocBBProfileBuffer.
    virtual HRESULT getBBProfileData(
            CORINFO_METHOD_HANDLE ftnHnd,
            ULONG *               count,           // The number of basic blocks that we have
            ProfileBuffer **      profileBuffer,
            ULONG *               numRuns
            ) = 0;

    // Associates a native call site, identified by its offset in the native code stream, with
    // the signature information and method handle the JIT used to lay out the call site. If
    // the call site has no signature information (e.g. a helper call) or has no method handle
    // (e.g. a CALLI P/Invoke), then null should be passed instead.
    virtual void recordCallSite(
            ULONG                 instrOffset,  /* IN */
            CORINFO_SIG_INFO *    callSig,      /* IN */
            CORINFO_METHOD_HANDLE methodHandle  /* IN */
            ) = 0;

    // A relocation is recorded if we are pre-jitting.
    // A jump thunk may be inserted if we are jitting
    virtual void recordRelocation(
            void *                 location,   /* IN  */
            void *                 target,     /* IN  */
            WORD                   fRelocType, /* IN  */
            WORD                   slotNum = 0,  /* IN  */
            INT32                  addlDelta = 0 /* IN  */
            ) = 0;

    virtual WORD getRelocTypeHint(void * target) = 0;

    // A callback to identify the range of address known to point to
    // compiler-generated native entry points that call back into
    // MSIL.
    virtual void getModuleNativeEntryPointRange(
            void ** pStart, /* OUT */
            void ** pEnd    /* OUT */
            ) = 0;

    // For what machine does the VM expect the JIT to generate code? The VM
    // returns one of the IMAGE_FILE_MACHINE_* values. Note that if the VM
    // is cross-compiling (such as the case for crossgen), it will return a
    // different value than if it was compiling for the host architecture.
    // 
    virtual DWORD getExpectedTargetArchitecture() = 0;
};

/**********************************************************************************/
#endif // _COR_CORJIT_H_
