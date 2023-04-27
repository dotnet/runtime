// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Internal.JitInterface
{
    // CorInfoHelpFunc defines the set of helpers (accessed via the ICorDynamicInfo::getHelperFtn())
    // These helpers can be called by native code which executes in the runtime.
    // Compilers can emit calls to these helpers.

    public enum CorInfoHelpFunc
    {
        CORINFO_HELP_UNDEF,         // invalid value. This should never be used

        /* Arithmetic helpers */

        CORINFO_HELP_DIV,           // For the ARM 32-bit integer divide uses a helper call :-(
        CORINFO_HELP_MOD,
        CORINFO_HELP_UDIV,
        CORINFO_HELP_UMOD,

        CORINFO_HELP_LLSH,
        CORINFO_HELP_LRSH,
        CORINFO_HELP_LRSZ,
        CORINFO_HELP_LMUL,
        CORINFO_HELP_LMUL_OVF,
        CORINFO_HELP_ULMUL_OVF,
        CORINFO_HELP_LDIV,
        CORINFO_HELP_LMOD,
        CORINFO_HELP_ULDIV,
        CORINFO_HELP_ULMOD,
        CORINFO_HELP_LNG2DBL,               // Convert a signed int64 to a double
        CORINFO_HELP_ULNG2DBL,              // Convert a unsigned int64 to a double
        CORINFO_HELP_DBL2INT,
        CORINFO_HELP_DBL2INT_OVF,
        CORINFO_HELP_DBL2LNG,
        CORINFO_HELP_DBL2LNG_OVF,
        CORINFO_HELP_DBL2UINT,
        CORINFO_HELP_DBL2UINT_OVF,
        CORINFO_HELP_DBL2ULNG,
        CORINFO_HELP_DBL2ULNG_OVF,
        CORINFO_HELP_FLTREM,
        CORINFO_HELP_DBLREM,
        CORINFO_HELP_FLTROUND,
        CORINFO_HELP_DBLROUND,

        /* Allocating a new object. Always use ICorClassInfo::getNewHelper() to decide
           which is the right helper to use to allocate an object of a given type. */

        CORINFO_HELP_NEWFAST,
        CORINFO_HELP_NEWSFAST,          // allocator for small, non-finalizer, non-array object
        CORINFO_HELP_NEWSFAST_FINALIZE, // allocator for small, finalizable, non-array object
        CORINFO_HELP_NEWSFAST_ALIGN8,   // allocator for small, non-finalizer, non-array object, 8 byte aligned
        CORINFO_HELP_NEWSFAST_ALIGN8_VC, // allocator for small, value class, 8 byte aligned
        CORINFO_HELP_NEWSFAST_ALIGN8_FINALIZE, // allocator for small, finalizable, non-array object, 8 byte aligned
        CORINFO_HELP_NEW_MDARR, // multi-dim array helper for arrays Rank != 1 (with or without lower bounds - dimensions passed in as unmanaged array)
        CORINFO_HELP_NEW_MDARR_RARE, // rare multi-dim array helper (Rank == 1)
        CORINFO_HELP_NEWARR_1_DIRECT,   // helper for any one dimensional array creation
        CORINFO_HELP_NEWARR_1_OBJ,      // optimized 1-D object arrays
        CORINFO_HELP_NEWARR_1_VC,       // optimized 1-D value class arrays
        CORINFO_HELP_NEWARR_1_ALIGN8,   // like VC, but aligns the array start

        CORINFO_HELP_STRCNS,            // create a new string literal
        /* Object model */

        CORINFO_HELP_INITCLASS,         // Initialize class if not already initialized
        CORINFO_HELP_INITINSTCLASS,     // Initialize class for instantiated type

        // Use ICorClassInfo::getCastingHelper to determine
        // the right helper to use

        CORINFO_HELP_ISINSTANCEOFINTERFACE, // Optimized helper for interfaces
        CORINFO_HELP_ISINSTANCEOFARRAY,  // Optimized helper for arrays
        CORINFO_HELP_ISINSTANCEOFCLASS, // Optimized helper for classes
        CORINFO_HELP_ISINSTANCEOFANY,   // Slow helper for any type

        CORINFO_HELP_CHKCASTINTERFACE,
        CORINFO_HELP_CHKCASTARRAY,
        CORINFO_HELP_CHKCASTCLASS,
        CORINFO_HELP_CHKCASTANY,
        CORINFO_HELP_CHKCASTCLASS_SPECIAL, // Optimized helper for classes. Assumes that the trivial cases
                                           // has been taken care of by the inlined check

        CORINFO_HELP_ISINSTANCEOF_EXCEPTION,

        CORINFO_HELP_BOX,               // Fast box helper. Only possible exception is OutOfMemory
        CORINFO_HELP_BOX_NULLABLE,      // special form of boxing for Nullable<T>
        CORINFO_HELP_UNBOX,
        CORINFO_HELP_UNBOX_NULLABLE,    // special form of unboxing for Nullable<T>
        CORINFO_HELP_GETREFANY,         // Extract the byref from a TypedReference, checking that it is the expected type

        CORINFO_HELP_ARRADDR_ST,        // assign to element of object array with type-checking
        CORINFO_HELP_LDELEMA_REF,       // does a precise type comparison and returns address

        /* Exceptions */

        CORINFO_HELP_THROW,             // Throw an exception object
        CORINFO_HELP_RETHROW,           // Rethrow the currently active exception
        CORINFO_HELP_USER_BREAKPOINT,   // For a user program to break to the debugger
        CORINFO_HELP_RNGCHKFAIL,        // array bounds check failed
        CORINFO_HELP_OVERFLOW,          // throw an overflow exception
        CORINFO_HELP_THROWDIVZERO,      // throw a divide by zero exception
        CORINFO_HELP_THROWNULLREF,      // throw a null reference exception

        CORINFO_HELP_VERIFICATION,      // Throw a VerificationException
        CORINFO_HELP_FAIL_FAST,         // Kill the process avoiding any exceptions or stack and data dependencies (use for GuardStack unsafe buffer checks)

        CORINFO_HELP_METHOD_ACCESS_EXCEPTION, //Throw an access exception due to a failed member/class access check.
        CORINFO_HELP_FIELD_ACCESS_EXCEPTION,
        CORINFO_HELP_CLASS_ACCESS_EXCEPTION,

        CORINFO_HELP_ENDCATCH,          // call back into the EE at the end of a catch block

        /* Synchronization */

        CORINFO_HELP_MON_ENTER,
        CORINFO_HELP_MON_EXIT,
        CORINFO_HELP_MON_ENTER_STATIC,
        CORINFO_HELP_MON_EXIT_STATIC,

        CORINFO_HELP_GETCLASSFROMMETHODPARAM, // Given a generics method handle, returns a class handle
        CORINFO_HELP_GETSYNCFROMCLASSHANDLE,  // Given a generics class handle, returns the sync monitor
                                              // in its ManagedClassObject

        /* GC support */

        CORINFO_HELP_STOP_FOR_GC,       // Call GC (force a GC)
        CORINFO_HELP_POLL_GC,           // Ask GC if it wants to collect

        CORINFO_HELP_STRESS_GC,         // Force a GC, but then update the JITTED code to be a noop call
        CORINFO_HELP_CHECK_OBJ,         // confirm that ECX is a valid object pointer (debugging only)

        /* GC Write barrier support */

        CORINFO_HELP_ASSIGN_REF,        // universal helpers with F_CALL_CONV calling convention
        CORINFO_HELP_CHECKED_ASSIGN_REF,
        CORINFO_HELP_ASSIGN_REF_ENSURE_NONHEAP,  // Do the store, and ensure that the target was not in the heap.

        CORINFO_HELP_ASSIGN_BYREF,
        CORINFO_HELP_ASSIGN_STRUCT,


        /* Accessing fields */

        // For COM object support (using COM get/set routines to update object)
        // and EnC and cross-context support
        CORINFO_HELP_GETFIELD8,
        CORINFO_HELP_SETFIELD8,
        CORINFO_HELP_GETFIELD16,
        CORINFO_HELP_SETFIELD16,
        CORINFO_HELP_GETFIELD32,
        CORINFO_HELP_SETFIELD32,
        CORINFO_HELP_GETFIELD64,
        CORINFO_HELP_SETFIELD64,
        CORINFO_HELP_GETFIELDOBJ,
        CORINFO_HELP_SETFIELDOBJ,
        CORINFO_HELP_GETFIELDSTRUCT,
        CORINFO_HELP_SETFIELDSTRUCT,
        CORINFO_HELP_GETFIELDFLOAT,
        CORINFO_HELP_SETFIELDFLOAT,
        CORINFO_HELP_GETFIELDDOUBLE,
        CORINFO_HELP_SETFIELDDOUBLE,

        CORINFO_HELP_GETFIELDADDR,
        CORINFO_HELP_GETSTATICFIELDADDR,
        CORINFO_HELP_GETSTATICFIELDADDR_TLS,        // Helper for PE TLS fields

        // There are a variety of specialized helpers for accessing static fields. The JIT should use
        // ICorClassInfo::getSharedStaticsOrCCtorHelper to determine which helper to use

        // Helpers for regular statics
        CORINFO_HELP_GETGENERICS_GCSTATIC_BASE,
        CORINFO_HELP_GETGENERICS_NONGCSTATIC_BASE,
        CORINFO_HELP_GETSHARED_GCSTATIC_BASE,
        CORINFO_HELP_GETSHARED_NONGCSTATIC_BASE,
        CORINFO_HELP_GETSHARED_GCSTATIC_BASE_NOCTOR,
        CORINFO_HELP_GETSHARED_NONGCSTATIC_BASE_NOCTOR,
        CORINFO_HELP_GETSHARED_GCSTATIC_BASE_DYNAMICCLASS,
        CORINFO_HELP_GETSHARED_NONGCSTATIC_BASE_DYNAMICCLASS,
        // Helper to class initialize shared generic with dynamicclass, but not get static field address
        CORINFO_HELP_CLASSINIT_SHARED_DYNAMICCLASS,

        // Helpers for thread statics
        CORINFO_HELP_GETGENERICS_GCTHREADSTATIC_BASE,
        CORINFO_HELP_GETGENERICS_NONGCTHREADSTATIC_BASE,
        CORINFO_HELP_GETSHARED_GCTHREADSTATIC_BASE,
        CORINFO_HELP_GETSHARED_NONGCTHREADSTATIC_BASE,
        CORINFO_HELP_GETSHARED_GCTHREADSTATIC_BASE_NOCTOR,
        CORINFO_HELP_GETSHARED_NONGCTHREADSTATIC_BASE_NOCTOR,
        CORINFO_HELP_GETSHARED_NONGCTHREADSTATIC_BASE_NOCTOR_OPTIMIZED,
        CORINFO_HELP_GETSHARED_GCTHREADSTATIC_BASE_DYNAMICCLASS,
        CORINFO_HELP_GETSHARED_NONGCTHREADSTATIC_BASE_DYNAMICCLASS,

        /* Debugger */

        CORINFO_HELP_DBG_IS_JUST_MY_CODE,    // Check if this is "JustMyCode" and needs to be stepped through.

        /* Profiling enter/leave probe addresses */
        CORINFO_HELP_PROF_FCN_ENTER,        // record the entry to a method (caller)
        CORINFO_HELP_PROF_FCN_LEAVE,        // record the completion of current method (caller)
        CORINFO_HELP_PROF_FCN_TAILCALL,     // record the completionof current method through tailcall (caller)

        /* Miscellaneous */

        CORINFO_HELP_BBT_FCN_ENTER,         // record the entry to a method for collecting Tuning data

        CORINFO_HELP_PINVOKE_CALLI,         // Indirect pinvoke call
        CORINFO_HELP_TAILCALL,              // Perform a tail call

        CORINFO_HELP_GETCURRENTMANAGEDTHREADID,

        CORINFO_HELP_INIT_PINVOKE_FRAME,   // initialize an inlined PInvoke Frame for the JIT-compiler

        CORINFO_HELP_MEMSET,                // Init block of memory
        CORINFO_HELP_MEMCPY,                // Copy block of memory

        CORINFO_HELP_RUNTIMEHANDLE_METHOD,  // determine a type/field/method handle at run-time
        CORINFO_HELP_RUNTIMEHANDLE_METHOD_LOG, // determine a type/field/method handle at run-time, with IBC logging
        CORINFO_HELP_RUNTIMEHANDLE_CLASS,    // determine a type/field/method handle at run-time
        CORINFO_HELP_RUNTIMEHANDLE_CLASS_LOG, // determine a type/field/method handle at run-time, with IBC logging

        CORINFO_HELP_TYPEHANDLE_TO_RUNTIMETYPE, // Convert from a TypeHandle (native structure pointer) to RuntimeType at run-time
        CORINFO_HELP_TYPEHANDLE_TO_RUNTIMETYPE_MAYBENULL, // Convert from a TypeHandle (native structure pointer) to RuntimeType at run-time, the type may be null
        CORINFO_HELP_METHODDESC_TO_STUBRUNTIMEMETHOD, // Convert from a MethodDesc (native structure pointer) to RuntimeMethodHandle at run-time
        CORINFO_HELP_FIELDDESC_TO_STUBRUNTIMEFIELD, // Convert from a FieldDesc (native structure pointer) to RuntimeFieldHandle at run-time
        CORINFO_HELP_TYPEHANDLE_TO_RUNTIMETYPEHANDLE, // Convert from a TypeHandle (native structure pointer) to RuntimeType at run-time
        CORINFO_HELP_TYPEHANDLE_TO_RUNTIMETYPEHANDLE_MAYBENULL, // Convert from a TypeHandle (native structure pointer) to RuntimeTypeHandle at run-time, handle might point to a null type

        CORINFO_HELP_ARE_TYPES_EQUIVALENT, // Check whether two TypeHandles (native structure pointers) are equivalent

        CORINFO_HELP_VIRTUAL_FUNC_PTR,      // look up a virtual method at run-time

        // Not a real helpers. Instead of taking handle arguments, these helpers point to a small stub that loads the handle argument and calls the static helper.
        CORINFO_HELP_READYTORUN_NEW,
        CORINFO_HELP_READYTORUN_NEWARR_1,
        CORINFO_HELP_READYTORUN_ISINSTANCEOF,
        CORINFO_HELP_READYTORUN_CHKCAST,
        CORINFO_HELP_READYTORUN_GCSTATIC_BASE,
        CORINFO_HELP_READYTORUN_NONGCSTATIC_BASE,
        CORINFO_HELP_READYTORUN_THREADSTATIC_BASE,
        CORINFO_HELP_READYTORUN_NONGCTHREADSTATIC_BASE,
        CORINFO_HELP_READYTORUN_VIRTUAL_FUNC_PTR,
        CORINFO_HELP_READYTORUN_GENERIC_HANDLE,
        CORINFO_HELP_READYTORUN_DELEGATE_CTOR,
        CORINFO_HELP_READYTORUN_GENERIC_STATIC_BASE,

        CORINFO_HELP_EE_PERSONALITY_ROUTINE, // Not real JIT helper. Used in native images.
        CORINFO_HELP_EE_PERSONALITY_ROUTINE_FILTER_FUNCLET, // Not real JIT helper. Used in native images to detect filter funclets.

        // ASSIGN_REF_EAX - CHECKED_ASSIGN_REF_EBP: NOGC_WRITE_BARRIERS JIT helper calls
        //
        // For unchecked versions EDX is required to point into GC heap.
        //
        // NOTE: these helpers are only used for x86.
        CORINFO_HELP_ASSIGN_REF_EAX,    // EAX holds GC ptr, do a 'mov [EDX], EAX' and inform GC
        CORINFO_HELP_ASSIGN_REF_EBX,    // EBX holds GC ptr, do a 'mov [EDX], EBX' and inform GC
        CORINFO_HELP_ASSIGN_REF_ECX,    // ECX holds GC ptr, do a 'mov [EDX], ECX' and inform GC
        CORINFO_HELP_ASSIGN_REF_ESI,    // ESI holds GC ptr, do a 'mov [EDX], ESI' and inform GC
        CORINFO_HELP_ASSIGN_REF_EDI,    // EDI holds GC ptr, do a 'mov [EDX], EDI' and inform GC
        CORINFO_HELP_ASSIGN_REF_EBP,    // EBP holds GC ptr, do a 'mov [EDX], EBP' and inform GC

        CORINFO_HELP_CHECKED_ASSIGN_REF_EAX,  // These are the same as ASSIGN_REF above ...
        CORINFO_HELP_CHECKED_ASSIGN_REF_EBX,  // ... but also check if EDX points into heap.
        CORINFO_HELP_CHECKED_ASSIGN_REF_ECX,
        CORINFO_HELP_CHECKED_ASSIGN_REF_ESI,
        CORINFO_HELP_CHECKED_ASSIGN_REF_EDI,
        CORINFO_HELP_CHECKED_ASSIGN_REF_EBP,

        CORINFO_HELP_LOOP_CLONE_CHOICE_ADDR, // Return the reference to a counter to decide to take cloned path in debug stress.
        CORINFO_HELP_DEBUG_LOG_LOOP_CLONING, // Print a message that a loop cloning optimization has occurred in debug mode.

        CORINFO_HELP_THROW_ARGUMENTEXCEPTION,           // throw ArgumentException
        CORINFO_HELP_THROW_ARGUMENTOUTOFRANGEEXCEPTION, // throw ArgumentOutOfRangeException
        CORINFO_HELP_THROW_NOT_IMPLEMENTED,             // throw NotImplementedException
        CORINFO_HELP_THROW_PLATFORM_NOT_SUPPORTED,      // throw PlatformNotSupportedException
        CORINFO_HELP_THROW_TYPE_NOT_SUPPORTED,          // throw TypeNotSupportedException
        CORINFO_HELP_THROW_AMBIGUOUS_RESOLUTION_EXCEPTION, // throw AmbiguousResolutionException for failed static virtual method resolution

        CORINFO_HELP_JIT_PINVOKE_BEGIN, // Transition to preemptive mode before a P/Invoke, frame is the first argument
        CORINFO_HELP_JIT_PINVOKE_END,   // Transition to cooperative mode after a P/Invoke, frame is the first argument

        CORINFO_HELP_JIT_REVERSE_PINVOKE_ENTER, // Transition to cooperative mode in reverse P/Invoke prolog, frame is the first argument
        CORINFO_HELP_JIT_REVERSE_PINVOKE_ENTER_TRACK_TRANSITIONS, // Transition to cooperative mode and track transitions in reverse P/Invoke prolog.
        CORINFO_HELP_JIT_REVERSE_PINVOKE_EXIT,  // Transition to preemptive mode in reverse P/Invoke epilog, frame is the first argument
        CORINFO_HELP_JIT_REVERSE_PINVOKE_EXIT_TRACK_TRANSITIONS, // Transition to preemptive mode and track transitions in reverse P/Invoke prolog.

        CORINFO_HELP_GVMLOOKUP_FOR_SLOT,        // Resolve a generic virtual method target from this pointer and runtime method handle

        CORINFO_HELP_STACK_PROBE,               // Probes each page of the allocated stack frame

        CORINFO_HELP_PATCHPOINT,                // Notify runtime that code has reached a patchpoint
        CORINFO_HELP_PARTIAL_COMPILATION_PATCHPOINT,  // Notify runtime that code has reached a part of the method that wasn't originally jitted.

        CORINFO_HELP_CLASSPROFILE32,            // Update 32-bit class profile for a call site
        CORINFO_HELP_CLASSPROFILE64,            // Update 64-bit class profile for a call site
        CORINFO_HELP_DELEGATEPROFILE32,         // Update 32-bit method profile for a delegate call site
        CORINFO_HELP_DELEGATEPROFILE64,         // Update 64-bit method profile for a delegate call site
        CORINFO_HELP_VTABLEPROFILE32,           // Update 32-bit method profile for a vtable call site
        CORINFO_HELP_VTABLEPROFILE64,           // Update 64-bit method profile for a vtable call site
        CORINFO_HELP_COUNTPROFILE32,            // Update 32-bit block or edge count profile
        CORINFO_HELP_COUNTPROFILE64,            // Update 64-bit block or edge count profile

        CORINFO_HELP_VALIDATE_INDIRECT_CALL,    // CFG: Validate function pointer
        CORINFO_HELP_DISPATCH_INDIRECT_CALL,    // CFG: Validate and dispatch to pointer

        CORINFO_HELP_COUNT,
    }
}
