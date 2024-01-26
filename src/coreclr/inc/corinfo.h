// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//

/*****************************************************************************\
*                                                                             *
* CorInfo.h -    EE / Code generator interface                                *
*                                                                             *
*******************************************************************************
*
* This file exposes CLR runtime functionality. It can be used by compilers,
* both Just-in-time and ahead-of-time, to generate native code which
* executes in the runtime environment.
*******************************************************************************

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

#EEJitContractDetails

The semantic contract between the EE and the JIT should be documented here It is incomplete, but as time goes
on, that hopefully will change

See file:../../doc/BookOfTheRuntime/JIT/JIT%20Design.doc for details on the JIT compiler. See
code:EEStartup#TableOfContents for information on the runtime as a whole.

-------------------------------------------------------------------------------
#Tokens

The tokens in IL stream needs to be resolved to EE handles (CORINFO_CLASS/METHOD/FIELD_HANDLE) that
the runtime operates with. ICorStaticInfo::resolveToken is the method that resolves the found in IL stream
to set of EE handles (CORINFO_RESOLVED_TOKEN). All other APIs take resolved token as input. This design
avoids redundant token resolutions.

The token validation is done as part of token resolution. The JIT is not required to do explicit upfront
token validation.

-------------------------------------------------------------------------------
#ClassConstruction

First of all class construction comes in two flavors precise and 'beforeFieldInit'. In C# you get the former
if you declare an explicit class constructor method and the later if you declaratively initialize static
fields. Precise class construction guarantees that the .cctor is run precisely before the first access to any
method or field of the class. 'beforeFieldInit' semantics guarantees only that the .cctor will be run some
time before the first static field access (note that calling methods (static or instance) or accessing
instance fields does not cause .cctors to be run).

Next you need to know that there are two kinds of code generation that can happen in the JIT: appdomain
neutral and appdomain specialized. The difference between these two kinds of code is how statics are handled.
For appdomain specific code, the address of a particular static variable is embedded in the code. This makes
it usable only for one appdomain (since every appdomain gets a own copy of its statics). Appdomain neutral
code calls a helper that looks up static variables off of a thread local variable. Thus the same code can be
used by multiple appdomains in the same process.

Generics also introduce a similar issue. Code for generic classes might be specialized for a particular set
of type arguments, or it could use helpers to access data that depends on type parameters and thus be shared
across several instantiations of the generic type.

Thus there four cases

    * BeforeFieldInitCCtor - Unshared code. Cctors are only called when static fields are fetched. At the
        time the method that touches the static field is JITed (or fixed up in the case of NGENed code), the
        .cctor is called.
    * BeforeFieldInitCCtor - Shared code. Since the same code is used for multiple classes, the act of JITing
        the code can not be used as a hook. However, it is also the case that since the code is shared, it
        can not wire in a particular address for the static and thus needs to use a helper that looks up the
        correct address based on the thread ID. This helper does the .cctor check, and thus no additional
        cctor logic is needed.
    * PreciseCCtor - Unshared code. Any time a method is JITTed (or fixed up in the case of NGEN), a cctor
        check for the class of the method being JITTed is done. In addition the JIT inserts explicit checks
        before any static field accesses. Instance methods and fields do NOT have hooks because a .ctor
        method must be called before the instance can be created.
    * PreciseCctor - Shared code .cctor checks are placed in the prolog of every .ctor and static method. All
        methods that access static fields have an explicit .cctor check before use. Again instance methods
        don't have hooks because a .ctor would have to be called first.

Technically speaking, however the optimization of avoiding checks on instance methods is flawed. It requires
that a .ctor always precede a call to an instance methods. This break down when

    * A NULL is passed to an instance method.
    * A .ctor does not call its superclasses .ctor. This allows an instance to be created without necessarily
        calling all the .cctors of all the superclasses. A virtual call can then be made to a instance of a
        superclass without necessarily calling the superclass's .cctor.
    * The class is a value class (which exists without a .ctor being called)

Nevertheless, the cost of plugging these holes is considered to high and the benefit is low.

----------------------------------------------------------------------

#ClassConstructionFlags

Thus the JIT's cctor responsibilities require it to check with the EE on every static field access using
initClass and before jitting any method to see if a .cctor check must be placed in the prolog.

    * CORINFO_FLG_BEFOREFIELDINIT indicate the class has beforeFieldInit semantics. The jit does not strictly
        need this information however, it is valuable in optimizing static field fetch helper calls. Helper
        call for classes with BeforeFieldInit semantics can be hoisted before other side effects where
        classes with precise .cctor semantics do not allow this optimization.

Inlining also complicates things. Because the class could have precise semantics it is also required that the
inlining of any constructor or static method must also do the initClass check. The inliner has the option of
inserting any required runtime check or simply not inlining the function.

-------------------------------------------------------------------------------

#StaticFields

The first 4 options are mutually exclusive

    * CORINFO_FLG_HELPER If the field has this set, then the JIT must call getFieldHelper and call the
        returned helper with the object ref (for an instance field) and a fieldDesc. Note that this should be
        able to handle ANY field so to get a JIT up quickly, it has the option of using helper calls for all
        field access (and skip the complexity below). Note that for statics it is assumed that you will
        always ask for the ADDRESS helper and to the fetch in the JIT.

    * CORINFO_FLG_SHARED_HELPER This is currently only used for static fields. If this bit is set it means
        that the field is feched by a helper call that takes a module identifier (see getModuleDomainID) and
        a class identifier (see getClassDomainID) as arguments. The exact helper to call is determined by
        getSharedStaticBaseHelper. The return value is of this function is the base of all statics in the
        module. The offset from getFieldOffset must be added to this value to get the address of the field
        itself. (see also CORINFO_FLG_STATIC_IN_HEAP).


    * CORINFO_FLG_GENERICS_STATIC This is currently only used for static fields (of generic type). This
        function is intended to be called with a Generic handle as a argument (from embedGenericHandle). The
        exact helper to call is determined by getSharedStaticBaseHelper. The returned value is the base of
        all statics in the class. The offset from getFieldOffset must be added to this value to get the
        address of the (see also CORINFO_FLG_STATIC_IN_HEAP).

    * CORINFO_FLG_TLS This indicate that the static field is a Windows style Thread Local Static. (We also
        have managed thread local statics, which work through the HELPER. Support for this is considered
        legacy, and going forward, the EE should

    * <NONE> This is a normal static field. Its address in memory is determined by getFieldInfo. (see
        also CORINFO_FLG_STATIC_IN_HEAP).


This last field can modify any of the cases above except CORINFO_FLG_HELPER

CORINFO_FLG_STATIC_IN_HEAP This is currently only used for static fields of value classes. If the field has
this set then after computing what would normally be the field, what you actually get is a object pointer
(that must be reported to the GC) to a boxed version of the value. Thus the actual field address is computed
by addr = (*addr+sizeof(OBJECTREF))

Instance fields

    * CORINFO_FLG_HELPER This is used if the class is MarshalByRef, which means that the object might be a
        proxy to the real object in some other appdomain or process. If the field has this set, then the JIT
        must call getFieldHelper and call the returned helper with the object ref. If the helper returned is
        helpers that are for structures the args are as follows

    * CORINFO_HELP_GETFIELDSTRUCT - args are: retBuff, object, fieldDesc
    * CORINFO_HELP_SETFIELDSTRUCT - args are object fieldDesc value

The other GET helpers take an object fieldDesc and return the value The other SET helpers take an object
fieldDesc and value

    Note that unlike static fields there is no helper to take the address of a field because in general there
    is no address for proxies (LDFLDA is illegal on proxies).

    CORINFO_FLG_EnC This is to support adding new field for edit and continue. This field also indicates that
    a helper is needed to access this field. However this helper is always CORINFO_HELP_GETFIELDADDR, and
    this helper always takes the object and field handle and returns the address of the field. It is the
                            JIT's responsibility to do the fetch or set.

-------------------------------------------------------------------------------

TODO: Talk about initializing strutures before use


*******************************************************************************
*/

#ifndef _COR_INFO_H_
#define _COR_INFO_H_

#include "corhdr.h"

#if !defined(_In_)
// Minimum set of SAL annotations so that non Windows builds work
#define _In_
#define _In_reads_(size)
#define _Inout_updates_(size)
#define _Out_
#define _Out_writes_(size)
#define _Outptr_
#define _Outptr_opt_
#define _Outptr_opt_result_maybenull_
#define _Outptr_result_z_
#define _Outptr_result_buffer_(size)
#define _Outptr_opt_result_buffer_(size)
#endif

#include "jiteeversionguid.h"

#ifdef _MSC_VER
typedef long JITINTERFACE_HRESULT;
#else
typedef int JITINTERFACE_HRESULT;
#endif // _MSC_VER

// For System V on the CLR type system number of registers to pass in and return a struct is the same.
// The CLR type system allows only up to 2 eightbytes to be passed in registers. There is no SSEUP classification types.
#define CLR_SYSTEMV_MAX_EIGHTBYTES_COUNT_TO_PASS_IN_REGISTERS   2
#define CLR_SYSTEMV_MAX_EIGHTBYTES_COUNT_TO_RETURN_IN_REGISTERS 2
#define CLR_SYSTEMV_MAX_STRUCT_BYTES_TO_PASS_IN_REGISTERS       16

// System V struct passing
// The Classification types are described in the ABI spec at https://software.intel.com/sites/default/files/article/402129/mpx-linux64-abi.pdf
enum SystemVClassificationType : uint8_t
{
    SystemVClassificationTypeUnknown            = 0,
    SystemVClassificationTypeStruct             = 1,
    SystemVClassificationTypeNoClass            = 2,
    SystemVClassificationTypeMemory             = 3,
    SystemVClassificationTypeInteger            = 4,
    SystemVClassificationTypeIntegerReference   = 5,
    SystemVClassificationTypeIntegerByRef       = 6,
    SystemVClassificationTypeSSE                = 7,
    // SystemVClassificationTypeSSEUp           = Unused, // Not supported by the CLR.
    // SystemVClassificationTypeX87             = Unused, // Not supported by the CLR.
    // SystemVClassificationTypeX87Up           = Unused, // Not supported by the CLR.
    // SystemVClassificationTypeComplexX87      = Unused, // Not supported by the CLR.
};

// Represents classification information for a struct.
struct SYSTEMV_AMD64_CORINFO_STRUCT_REG_PASSING_DESCRIPTOR
{
    SYSTEMV_AMD64_CORINFO_STRUCT_REG_PASSING_DESCRIPTOR()
    {
        Initialize();
    }

    bool                        passedInRegisters; // Whether the struct is passable/passed (this includes struct returning) in registers.
    uint8_t                     eightByteCount;    // Number of eightbytes for this struct.
    SystemVClassificationType   eightByteClassifications[CLR_SYSTEMV_MAX_EIGHTBYTES_COUNT_TO_PASS_IN_REGISTERS]; // The eightbytes type classification.
    uint8_t                     eightByteSizes[CLR_SYSTEMV_MAX_EIGHTBYTES_COUNT_TO_PASS_IN_REGISTERS];           // The size of the eightbytes (an eightbyte could include padding. This represents the no padding size of the eightbyte).
    uint8_t                     eightByteOffsets[CLR_SYSTEMV_MAX_EIGHTBYTES_COUNT_TO_PASS_IN_REGISTERS];         // The start offset of the eightbytes (in bytes).

    // Members

    //------------------------------------------------------------------------
    // CopyFrom: Copies a struct classification into this one.
    //
    // Arguments:
    //    'copyFrom' the struct classification to copy from.
    //
    void CopyFrom(const SYSTEMV_AMD64_CORINFO_STRUCT_REG_PASSING_DESCRIPTOR& copyFrom)
    {
        passedInRegisters = copyFrom.passedInRegisters;
        eightByteCount = copyFrom.eightByteCount;

        for (int i = 0; i < CLR_SYSTEMV_MAX_EIGHTBYTES_COUNT_TO_PASS_IN_REGISTERS; i++)
        {
            eightByteClassifications[i] = copyFrom.eightByteClassifications[i];
            eightByteSizes[i] = copyFrom.eightByteSizes[i];
            eightByteOffsets[i] = copyFrom.eightByteOffsets[i];
        }
    }

    //------------------------------------------------------------------------
    // IsIntegralSlot: Returns whether the eightbyte at slotIndex is of integral type.
    //
    // Arguments:
    //    'slotIndex' the slot number we are determining if it is of integral type.
    //
    // Return value:
    //     returns true if we the eightbyte at index slotIndex is of integral type.
    //

    bool IsIntegralSlot(unsigned slotIndex) const
    {
        return ((eightByteClassifications[slotIndex] == SystemVClassificationTypeInteger) ||
                (eightByteClassifications[slotIndex] == SystemVClassificationTypeIntegerReference) ||
                (eightByteClassifications[slotIndex] == SystemVClassificationTypeIntegerByRef));
    }

    //------------------------------------------------------------------------
    // IsSseSlot: Returns whether the eightbyte at slotIndex is SSE type.
    //
    // Arguments:
    //    'slotIndex' the slot number we are determining if it is of SSE type.
    //
    // Return value:
    //     returns true if we the eightbyte at index slotIndex is of SSE type.
    //
    // Follows the rules of the AMD64 System V ABI specification at https://software.intel.com/sites/default/files/article/402129/mpx-linux64-abi.pdf.
    // Please refer to it for definitions/examples.
    //
    bool IsSseSlot(unsigned slotIndex) const
    {
        return (eightByteClassifications[slotIndex] == SystemVClassificationTypeSSE);
    }

private:
    void Initialize()
    {
        passedInRegisters = false;
        eightByteCount = 0;

        for (int i = 0; i < CLR_SYSTEMV_MAX_EIGHTBYTES_COUNT_TO_PASS_IN_REGISTERS; i++)
        {
            eightByteClassifications[i] = SystemVClassificationTypeUnknown;
            eightByteSizes[i] = 0;
            eightByteOffsets[i] = 0;
        }
    }
};

// StructFloadFieldInfoFlags: used on LoongArch64 architecture by `getLoongArch64PassStructInRegisterFlags` and
// `getRISCV64PassStructInRegisterFlags` API to convey struct argument passing information.
//
// `STRUCT_NO_FLOAT_FIELD` means structs are not passed using the float register(s).
//
// Otherwise, and only for structs with no more than two fields and a total struct size no larger
// than two pointers:
//
// The lowest four bits denote the floating-point info:
//   bit 0: `1` means there is only one float or double field within the struct.
//   bit 1: `1` means only the first field is floating-point type.
//   bit 2: `1` means only the second field is floating-point type.
//   bit 3: `1` means the two fields are both floating-point type.
// The bits[5:4] denoting whether the field size is 8-bytes:
//   bit 4: `1` means the first field's size is 8.
//   bit 5: `1` means the second field's size is 8.
//
// Note that bit 0 and 3 cannot both be set.
enum StructFloatFieldInfoFlags
{
    STRUCT_NO_FLOAT_FIELD         = 0x0,
    STRUCT_FLOAT_FIELD_ONLY_ONE   = 0x1,
    STRUCT_FLOAT_FIELD_ONLY_TWO   = 0x8,
    STRUCT_FLOAT_FIELD_FIRST      = 0x2,
    STRUCT_FLOAT_FIELD_SECOND     = 0x4,
    STRUCT_FIRST_FIELD_SIZE_IS8   = 0x10,
    STRUCT_SECOND_FIELD_SIZE_IS8  = 0x20,

    STRUCT_FIRST_FIELD_DOUBLE     = (STRUCT_FLOAT_FIELD_FIRST | STRUCT_FIRST_FIELD_SIZE_IS8),
    STRUCT_SECOND_FIELD_DOUBLE    = (STRUCT_FLOAT_FIELD_SECOND | STRUCT_SECOND_FIELD_SIZE_IS8),
    STRUCT_FIELD_TWO_DOUBLES      = (STRUCT_FIRST_FIELD_SIZE_IS8 | STRUCT_SECOND_FIELD_SIZE_IS8 | STRUCT_FLOAT_FIELD_ONLY_TWO),

    STRUCT_MERGE_FIRST_SECOND     = (STRUCT_FLOAT_FIELD_FIRST | STRUCT_FLOAT_FIELD_ONLY_TWO),
    STRUCT_MERGE_FIRST_SECOND_8   = (STRUCT_FLOAT_FIELD_FIRST | STRUCT_FLOAT_FIELD_ONLY_TWO | STRUCT_SECOND_FIELD_SIZE_IS8),

    STRUCT_HAS_FLOAT_FIELDS_MASK  = (STRUCT_FLOAT_FIELD_FIRST | STRUCT_FLOAT_FIELD_SECOND | STRUCT_FLOAT_FIELD_ONLY_TWO | STRUCT_FLOAT_FIELD_ONLY_ONE),
    STRUCT_HAS_8BYTES_FIELDS_MASK = (STRUCT_FIRST_FIELD_SIZE_IS8 | STRUCT_SECOND_FIELD_SIZE_IS8),
};

#include "corinfoinstructionset.h"

// CorInfoHelpFunc defines the set of helpers (accessed via the ICorDynamicInfo::getHelperFtn())
// These helpers can be called by native code which executes in the runtime.
// Compilers can emit calls to these helpers.
//
// The signatures of the helpers are below (see RuntimeHelperArgumentCheck)

enum CorInfoHelpFunc
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
    CORINFO_HELP_NEWFAST_MAYBEFROZEN, // allocator for objects that *might* allocate them on a frozen segment
    CORINFO_HELP_NEWSFAST,          // allocator for small, non-finalizer, non-array object
    CORINFO_HELP_NEWSFAST_FINALIZE, // allocator for small, finalizable, non-array object
    CORINFO_HELP_NEWSFAST_ALIGN8,   // allocator for small, non-finalizer, non-array object, 8 byte aligned
    CORINFO_HELP_NEWSFAST_ALIGN8_VC,// allocator for small, value class, 8 byte aligned
    CORINFO_HELP_NEWSFAST_ALIGN8_FINALIZE, // allocator for small, finalizable, non-array object, 8 byte aligned
    CORINFO_HELP_NEW_MDARR,// multi-dim array helper for arrays Rank != 1 (with or without lower bounds - dimensions passed in as unmanaged array)
    CORINFO_HELP_NEW_MDARR_RARE,// rare multi-dim array helper (Rank == 1)
    CORINFO_HELP_NEWARR_1_DIRECT,   // helper for any one dimensional array creation
    CORINFO_HELP_NEWARR_1_MAYBEFROZEN, // allocator for arrays that *might* allocate them on a frozen segment
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

    CORINFO_HELP_METHOD_ACCESS_EXCEPTION,//Throw an access exception due to a failed member/class access check.
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
    CORINFO_HELP_GETSHARED_GCTHREADSTATIC_BASE_DYNAMICCLASS,
    CORINFO_HELP_GETSHARED_NONGCTHREADSTATIC_BASE_DYNAMICCLASS,
    CORINFO_HELP_GETSHARED_GCTHREADSTATIC_BASE_NOCTOR_OPTIMIZED,
    CORINFO_HELP_GETSHARED_NONGCTHREADSTATIC_BASE_NOCTOR_OPTIMIZED,

    /* Debugger */

    CORINFO_HELP_DBG_IS_JUST_MY_CODE,    // Check if this is "JustMyCode" and needs to be stepped through.

    /* Profiling enter/leave probe addresses */
    CORINFO_HELP_PROF_FCN_ENTER,        // record the entry to a method (caller)
    CORINFO_HELP_PROF_FCN_LEAVE,        // record the completion of current method (caller)
    CORINFO_HELP_PROF_FCN_TAILCALL,     // record the completion of current method through tailcall (caller)

    /* Miscellaneous */

    CORINFO_HELP_BBT_FCN_ENTER,         // record the entry to a method for collecting Tuning data

    CORINFO_HELP_PINVOKE_CALLI,         // Indirect pinvoke call
    CORINFO_HELP_TAILCALL,              // Perform a tail call

    CORINFO_HELP_GETCURRENTMANAGEDTHREADID,

    CORINFO_HELP_INIT_PINVOKE_FRAME,   // initialize an inlined PInvoke Frame for the JIT-compiler

    CORINFO_HELP_MEMSET,                // Init block of memory
    CORINFO_HELP_MEMCPY,                // Copy block of memory

    CORINFO_HELP_RUNTIMEHANDLE_METHOD,          // determine a type/field/method handle at run-time
    CORINFO_HELP_RUNTIMEHANDLE_METHOD_LOG,      // determine a type/field/method handle at run-time, with IBC logging
    CORINFO_HELP_RUNTIMEHANDLE_CLASS,           // determine a type/field/method handle at run-time
    CORINFO_HELP_RUNTIMEHANDLE_CLASS_LOG,       // determine a type/field/method handle at run-time, with IBC logging

    CORINFO_HELP_TYPEHANDLE_TO_RUNTIMETYPE, // Convert from a TypeHandle (native structure pointer) to RuntimeType at run-time
    CORINFO_HELP_TYPEHANDLE_TO_RUNTIMETYPE_MAYBENULL, // Convert from a TypeHandle (native structure pointer) to RuntimeType at run-time, the type may be null
    CORINFO_HELP_METHODDESC_TO_STUBRUNTIMEMETHOD, // Convert from a MethodDesc (native structure pointer) to RuntimeMethodHandle at run-time
    CORINFO_HELP_FIELDDESC_TO_STUBRUNTIMEFIELD, // Convert from a FieldDesc (native structure pointer) to RuntimeFieldHandle at run-time
    CORINFO_HELP_TYPEHANDLE_TO_RUNTIMETYPEHANDLE, // Convert from a TypeHandle (native structure pointer) to RuntimeTypeHandle at run-time
    CORINFO_HELP_TYPEHANDLE_TO_RUNTIMETYPEHANDLE_MAYBENULL, // Convert from a TypeHandle (native structure pointer) to RuntimeTypeHandle at run-time, handle might point to a null type

    CORINFO_HELP_VIRTUAL_FUNC_PTR,      // look up a virtual method at run-time

    // Not a real helpers. Instead of taking handle arguments, these helpers point to a small stub that loads the handle argument and calls the static helper.
    CORINFO_HELP_READYTORUN_NEW,
    CORINFO_HELP_READYTORUN_NEWARR_1,
    CORINFO_HELP_READYTORUN_ISINSTANCEOF,
    CORINFO_HELP_READYTORUN_CHKCAST,
    CORINFO_HELP_READYTORUN_GCSTATIC_BASE,           // static gc field access
    CORINFO_HELP_READYTORUN_NONGCSTATIC_BASE,        // static non gc field access
    CORINFO_HELP_READYTORUN_THREADSTATIC_BASE,
    CORINFO_HELP_READYTORUN_THREADSTATIC_BASE_NOCTOR,
    CORINFO_HELP_READYTORUN_NONGCTHREADSTATIC_BASE,
    CORINFO_HELP_READYTORUN_VIRTUAL_FUNC_PTR,
    CORINFO_HELP_READYTORUN_GENERIC_HANDLE,
    CORINFO_HELP_READYTORUN_DELEGATE_CTOR,
    CORINFO_HELP_READYTORUN_GENERIC_STATIC_BASE,

    CORINFO_HELP_EE_PERSONALITY_ROUTINE,// Not real JIT helper. Used in native images.
    CORINFO_HELP_EE_PERSONALITY_ROUTINE_FILTER_FUNCLET,// Not real JIT helper. Used in native images to detect filter funclets.

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
    CORINFO_HELP_THROW_ENTRYPOINT_NOT_FOUND_EXCEPTION, // throw EntryPointNotFoundException for failed static virtual method resolution

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
    CORINFO_HELP_VALUEPROFILE32,            // Update 32-bit value profile
    CORINFO_HELP_VALUEPROFILE64,            // Update 64-bit value profile

    CORINFO_HELP_VALIDATE_INDIRECT_CALL,    // CFG: Validate function pointer
    CORINFO_HELP_DISPATCH_INDIRECT_CALL,    // CFG: Validate and dispatch to pointer

    CORINFO_HELP_COUNT,
};

//This describes the signature for a helper method.
enum CorInfoHelpSig
{
    CORINFO_HELP_SIG_UNDEF,
    CORINFO_HELP_SIG_NO_ALIGN_STUB,
    CORINFO_HELP_SIG_NO_UNWIND_STUB,
    CORINFO_HELP_SIG_REG_ONLY,
    CORINFO_HELP_SIG_4_STACK,
    CORINFO_HELP_SIG_8_STACK,
    CORINFO_HELP_SIG_12_STACK,
    CORINFO_HELP_SIG_16_STACK,

    CORINFO_HELP_SIG_EBPCALL, //special calling convention that uses EDX and
                              //EBP as arguments

    CORINFO_HELP_SIG_CANNOT_USE_ALIGN_STUB,

    CORINFO_HELP_SIG_COUNT
};

// The enumeration is returned in 'getSig','getType', getArgType methods
enum CorInfoType
{
    CORINFO_TYPE_UNDEF           = 0x0,
    CORINFO_TYPE_VOID            = 0x1,
    CORINFO_TYPE_BOOL            = 0x2,
    CORINFO_TYPE_CHAR            = 0x3,
    CORINFO_TYPE_BYTE            = 0x4,
    CORINFO_TYPE_UBYTE           = 0x5,
    CORINFO_TYPE_SHORT           = 0x6,
    CORINFO_TYPE_USHORT          = 0x7,
    CORINFO_TYPE_INT             = 0x8,
    CORINFO_TYPE_UINT            = 0x9,
    CORINFO_TYPE_LONG            = 0xa,
    CORINFO_TYPE_ULONG           = 0xb,
    CORINFO_TYPE_NATIVEINT       = 0xc,
    CORINFO_TYPE_NATIVEUINT      = 0xd,
    CORINFO_TYPE_FLOAT           = 0xe,
    CORINFO_TYPE_DOUBLE          = 0xf,
    CORINFO_TYPE_STRING          = 0x10,         // Not used, should remove
    CORINFO_TYPE_PTR             = 0x11,
    CORINFO_TYPE_BYREF           = 0x12,
    CORINFO_TYPE_VALUECLASS      = 0x13,
    CORINFO_TYPE_CLASS           = 0x14,
    CORINFO_TYPE_REFANY          = 0x15,

    // CORINFO_TYPE_VAR is for a generic type variable.
    // Generic type variables only appear when the JIT is doing
    // verification (not NOT compilation) of generic code
    // for the EE, in which case we're running
    // the JIT in "import only" mode.

    CORINFO_TYPE_VAR             = 0x16,
    CORINFO_TYPE_COUNT,                         // number of jit types
};

enum CorInfoTypeWithMod
{
    CORINFO_TYPE_MASK            = 0x3F,        // lower 6 bits are type mask
    CORINFO_TYPE_MOD_PINNED      = 0x40,        // can be applied to CLASS, or BYREF to indicate pinned
};

inline CorInfoType strip(CorInfoTypeWithMod val) {
    return CorInfoType(val & CORINFO_TYPE_MASK);
}

// The enumeration is returned in 'getSig'

enum CorInfoCallConv
{
    // These correspond to CorCallingConvention

    CORINFO_CALLCONV_DEFAULT    = 0x0,
    // Instead of using the below values, use the CorInfoCallConvExtension enum for unmanaged calling conventions.
    // CORINFO_CALLCONV_C          = 0x1,
    // CORINFO_CALLCONV_STDCALL    = 0x2,
    // CORINFO_CALLCONV_THISCALL   = 0x3,
    // CORINFO_CALLCONV_FASTCALL   = 0x4,
    CORINFO_CALLCONV_VARARG     = 0x5,
    CORINFO_CALLCONV_FIELD      = 0x6,
    CORINFO_CALLCONV_LOCAL_SIG  = 0x7,
    CORINFO_CALLCONV_PROPERTY   = 0x8,
    CORINFO_CALLCONV_UNMANAGED  = 0x9,
    CORINFO_CALLCONV_NATIVEVARARG = 0xb,    // used ONLY for IL stub PInvoke vararg calls

    CORINFO_CALLCONV_MASK       = 0x0f,     // Calling convention is bottom 4 bits
    CORINFO_CALLCONV_GENERIC    = 0x10,
    CORINFO_CALLCONV_HASTHIS    = 0x20,
    CORINFO_CALLCONV_EXPLICITTHIS=0x40,
    CORINFO_CALLCONV_PARAMTYPE  = 0x80,     // Passed last. Same as CORINFO_GENERICS_CTXT_FROM_PARAMTYPEARG
};

// Represents the calling conventions supported with the extensible calling convention syntax
// as well as the original metadata-encoded calling conventions.
enum class CorInfoCallConvExtension
{
    Managed,
    C,
    Stdcall,
    Thiscall,
    Fastcall,
    // New calling conventions supported with the extensible calling convention encoding go here.
    CMemberFunction,
    StdcallMemberFunction,
    FastcallMemberFunction
};

#ifdef TARGET_X86
inline bool IsCallerPop(CorInfoCallConvExtension callConv)
{
#ifdef UNIX_X86_ABI
    return callConv == CorInfoCallConvExtension::Managed || callConv == CorInfoCallConvExtension::C || callConv == CorInfoCallConvExtension::CMemberFunction;
#else
    return callConv == CorInfoCallConvExtension::C || callConv == CorInfoCallConvExtension::CMemberFunction;
#endif // UNIX_X86_ABI
}
#endif

// Determines whether or not this calling convention is an instance method calling convention.
inline bool callConvIsInstanceMethodCallConv(CorInfoCallConvExtension callConv)
{
    return callConv == CorInfoCallConvExtension::Thiscall || callConv == CorInfoCallConvExtension::CMemberFunction || callConv == CorInfoCallConvExtension::StdcallMemberFunction || callConv == CorInfoCallConvExtension::FastcallMemberFunction;
}

// These are returned from getMethodOptions
enum CorInfoOptions
{
    CORINFO_OPT_INIT_LOCALS                 = 0x00000010, // zero initialize all variables

    CORINFO_GENERICS_CTXT_FROM_THIS         = 0x00000020, // is this shared generic code that access the generic context from the this pointer?  If so, then if the method has SEH then the 'this' pointer must always be reported and kept alive.
    CORINFO_GENERICS_CTXT_FROM_METHODDESC   = 0x00000040, // is this shared generic code that access the generic context from the ParamTypeArg(that is a MethodDesc)?  If so, then if the method has SEH then the 'ParamTypeArg' must always be reported and kept alive. Same as CORINFO_CALLCONV_PARAMTYPE
    CORINFO_GENERICS_CTXT_FROM_METHODTABLE  = 0x00000080, // is this shared generic code that access the generic context from the ParamTypeArg(that is a MethodTable)?  If so, then if the method has SEH then the 'ParamTypeArg' must always be reported and kept alive. Same as CORINFO_CALLCONV_PARAMTYPE
    CORINFO_GENERICS_CTXT_MASK              = (CORINFO_GENERICS_CTXT_FROM_THIS |
                                               CORINFO_GENERICS_CTXT_FROM_METHODDESC |
                                               CORINFO_GENERICS_CTXT_FROM_METHODTABLE),
    CORINFO_GENERICS_CTXT_KEEP_ALIVE        = 0x00000100, // Keep the generics context alive throughout the method even if there is no explicit use, and report its location to the CLR

};

//
// what type of code region we are in
//
enum CorInfoRegionKind
{
    CORINFO_REGION_NONE,
    CORINFO_REGION_HOT,
    CORINFO_REGION_COLD,
    CORINFO_REGION_JIT,
};


// these are the attribute flags for fields and methods (getMethodAttribs)
enum CorInfoFlag
{
//  CORINFO_FLG_UNUSED                = 0x00000001,
//  CORINFO_FLG_UNUSED                = 0x00000002,
//  CORINFO_FLG_UNUSED                = 0x00000004,
    CORINFO_FLG_STATIC                = 0x00000008,
    CORINFO_FLG_FINAL                 = 0x00000010,
    CORINFO_FLG_SYNCH                 = 0x00000020,
    CORINFO_FLG_VIRTUAL               = 0x00000040,
//  CORINFO_FLG_UNUSED                = 0x00000080,
//  CORINFO_FLG_UNUSED                = 0x00000100,
    CORINFO_FLG_INTRINSIC_TYPE        = 0x00000200, // This type is marked by [Intrinsic]
    CORINFO_FLG_ABSTRACT              = 0x00000400,

    CORINFO_FLG_EnC                   = 0x00000800, // member was added by Edit'n'Continue

    // These are internal flags that can only be on methods
    CORINFO_FLG_FORCEINLINE           = 0x00010000, // The method should be inlined if possible.
    CORINFO_FLG_SHAREDINST            = 0x00020000, // the code for this method is shared between different generic instantiations (also set on classes/types)
    CORINFO_FLG_DELEGATE_INVOKE       = 0x00040000, // "Delegate
    CORINFO_FLG_PINVOKE               = 0x00080000, // Is a P/Invoke call
//  CORINFO_FLG_UNUSED                = 0x00100000,
    CORINFO_FLG_NOGCCHECK             = 0x00200000, // This method is FCALL that has no GC check.  Don't put alone in loops
    CORINFO_FLG_INTRINSIC             = 0x00400000, // This method MAY have an intrinsic ID
    CORINFO_FLG_CONSTRUCTOR           = 0x00800000, // This method is an instance or type initializer
    CORINFO_FLG_AGGRESSIVE_OPT        = 0x01000000, // The method may contain hot code and should be aggressively optimized if possible
    CORINFO_FLG_DISABLE_TIER0_FOR_LOOPS = 0x02000000, // Indicates that tier 0 JIT should not be used for a method that contains a loop
//  CORINFO_FLG_UNUSED                = 0x04000000,
//  CORINFO_FLG_UNUSED                = 0x08000000,
    CORINFO_FLG_DONT_INLINE           = 0x10000000, // The method should not be inlined
    CORINFO_FLG_DONT_INLINE_CALLER    = 0x20000000, // The method should not be inlined, nor should its callers. It cannot be tail called.
//  CORINFO_FLG_UNUSED                = 0x40000000,

    // These are internal flags that can only be on Classes
    CORINFO_FLG_VALUECLASS            = 0x00010000, // is the class a value class
//  This flag is define din the Methods section, but is also valid on classes.
//  CORINFO_FLG_SHAREDINST            = 0x00020000, // This class is satisfies TypeHandle::IsCanonicalSubtype
    CORINFO_FLG_VAROBJSIZE            = 0x00040000, // the object size varies depending of constructor args
    CORINFO_FLG_ARRAY                 = 0x00080000, // class is an array class (initialized differently)
    CORINFO_FLG_OVERLAPPING_FIELDS    = 0x00100000, // struct or class has fields that overlap (aka union)
    CORINFO_FLG_INTERFACE             = 0x00200000, // it is an interface
    CORINFO_FLG_CONTAINS_GC_PTR       = 0x01000000, // does the class contain a gc ptr ?
    CORINFO_FLG_DELEGATE              = 0x02000000, // is this a subclass of delegate or multicast delegate ?
    CORINFO_FLG_INDEXABLE_FIELDS      = 0x04000000, // struct fields may be accessed via indexing (used for inline arrays)
    CORINFO_FLG_BYREF_LIKE            = 0x08000000, // it is byref-like value type
//  CORINFO_FLG_UNUSED                = 0x10000000,
    CORINFO_FLG_BEFOREFIELDINIT       = 0x20000000, // Additional flexibility for when to run .cctor (see code:#ClassConstructionFlags)
    CORINFO_FLG_GENERIC_TYPE_VARIABLE = 0x40000000, // This is really a handle for a variable type
    CORINFO_FLG_UNSAFE_VALUECLASS     = 0x80000000, // Unsafe (C++'s /GS) value type
};

// Flags computed by a runtime compiler
enum CorInfoMethodRuntimeFlags
{
    CORINFO_FLG_BAD_INLINEE         = 0x00000001, // The method is not suitable for inlining
    // unused                       = 0x00000002,
    // unused                       = 0x00000004,
    CORINFO_FLG_SWITCHED_TO_MIN_OPT = 0x00000008, // The JIT decided to switch to MinOpt for this method, when it was not requested
    CORINFO_FLG_SWITCHED_TO_OPTIMIZED = 0x00000010, // The JIT decided to switch to tier 1 for this method, when a different tier was requested
};


enum CORINFO_ACCESS_FLAGS
{
    CORINFO_ACCESS_ANY        = 0x0000, // Normal access
    CORINFO_ACCESS_THIS       = 0x0001, // Accessed via the this reference
    // UNUSED                 = 0x0002,

    CORINFO_ACCESS_NONNULL    = 0x0004, // Instance is guaranteed non-null

    CORINFO_ACCESS_LDFTN      = 0x0010, // Accessed via ldftn

    // Field access flags
    CORINFO_ACCESS_GET        = 0x0100, // Field get (ldfld)
    CORINFO_ACCESS_SET        = 0x0200, // Field set (stfld)
    CORINFO_ACCESS_ADDRESS    = 0x0400, // Field address (ldflda)
    CORINFO_ACCESS_INIT_ARRAY = 0x0800, // Field use for InitializeArray
    // UNUSED                 = 0x4000,
    CORINFO_ACCESS_INLINECHECK= 0x8000, // Return fieldFlags and fieldAccessor only. Used by JIT64 during inlining.
};

// These are the flags set on an CORINFO_EH_CLAUSE
enum CORINFO_EH_CLAUSE_FLAGS
{
    CORINFO_EH_CLAUSE_NONE      = 0,
    CORINFO_EH_CLAUSE_FILTER    = 0x0001, // If this bit is on, then this EH entry is for a filter
    CORINFO_EH_CLAUSE_FINALLY   = 0x0002, // This clause is a finally clause
    CORINFO_EH_CLAUSE_FAULT     = 0x0004, // This clause is a fault clause
    CORINFO_EH_CLAUSE_DUPLICATE = 0x0008, // Duplicated clause. This clause was duplicated to a funclet which was pulled out of line
    CORINFO_EH_CLAUSE_SAMETRY   = 0x0010, // This clause covers same try block as the previous one
};

// This enumeration is passed to InternalThrow
enum CorInfoException
{
    CORINFO_NullReferenceException,
    CORINFO_DivideByZeroException,
    CORINFO_InvalidCastException,
    CORINFO_IndexOutOfRangeException,
    CORINFO_OverflowException,
    CORINFO_SynchronizationLockException,
    CORINFO_ArrayTypeMismatchException,
    CORINFO_RankException,
    CORINFO_ArgumentNullException,
    CORINFO_ArgumentException,
    CORINFO_Exception_Count,
};

// These are used to detect array methods as NamedIntrinsic in JIT importer,
// which otherwise don't have a name.
enum class CorInfoArrayIntrinsic
{
    GET = 0,
    SET = 1,
    ADDRESS = 2,

    ILLEGAL
};

// Can a value be accessed directly from JITed code.
enum InfoAccessType
{
    IAT_VALUE,      // The info value is directly available
    IAT_PVALUE,     // The value needs to be accessed via an         indirection
    IAT_PPVALUE,    // The value needs to be accessed via a double   indirection
    IAT_RELPVALUE   // The value needs to be accessed via a relative indirection
};

enum CorInfoGCType
{
    TYPE_GC_NONE,   // no embedded objectrefs
    TYPE_GC_REF,    // Is an object ref
    TYPE_GC_BYREF,  // Is an interior pointer - promote it but don't scan it
    TYPE_GC_OTHER   // requires type-specific treatment
};

enum CorInfoClassId
{
    CLASSID_SYSTEM_OBJECT,
    CLASSID_TYPED_BYREF,
    CLASSID_TYPE_HANDLE,
    CLASSID_FIELD_HANDLE,
    CLASSID_METHOD_HANDLE,
    CLASSID_STRING,
    CLASSID_ARGUMENT_HANDLE,
    CLASSID_RUNTIME_TYPE,
};

enum CorInfoInline
{
    INLINE_PASS                     = 0,    // Inlining OK
    INLINE_PREJIT_SUCCESS           = 1,    // Inline check for prejit checking usage succeeded
    INLINE_CHECK_CAN_INLINE_SUCCESS = 2,    // JIT detected it is permitted to try to actually inline
    INLINE_CHECK_CAN_INLINE_VMFAIL  = 3,    // VM specified that inline must fail via the CanInline api

    // failures are negative
    INLINE_FAIL                     = -1,   // Inlining not OK for this case only
    INLINE_NEVER                    = -2,   // This method should never be inlined, regardless of context
};

// If you add more values here, keep it in sync with TailCallTypeMap in ..\vm\ClrEtwAll.man
// and the string enum in CEEInfo::reportTailCallDecision in ..\vm\JITInterface.cpp
enum CorInfoTailCall
{
    TAILCALL_OPTIMIZED      = 0,    // Optimized tail call (epilog + jmp)
    TAILCALL_RECURSIVE      = 1,    // Optimized into a loop (only when a method tail calls itself)
    TAILCALL_HELPER         = 2,    // Helper assisted tail call (call to JIT_TailCall)

    // failures are negative
    TAILCALL_FAIL           = -1,   // Couldn't do a tail call
};

enum CorInfoInitClassResult
{
    CORINFO_INITCLASS_NOT_REQUIRED  = 0x00, // No class initialization required, but the class is not actually initialized yet
                                            // (e.g. we are guaranteed to run the static constructor in method prolog)
    CORINFO_INITCLASS_INITIALIZED   = 0x01, // Class initialized
    CORINFO_INITCLASS_USE_HELPER    = 0x02, // The JIT must insert class initialization helper call.
    CORINFO_INITCLASS_DONT_INLINE   = 0x04, // The JIT should not inline the method requesting the class initialization. The class
                                            // initialization requires helper class now, but will not require initialization
                                            // if the method is compiled standalone. Or the method cannot be inlined due to some
                                            // requirement around class initialization such as shared generics.
};

// Reason codes for making indirect calls
#define INDIRECT_CALL_REASONS() \
    INDIRECT_CALL_REASON_FUNC(CORINFO_INDIRECT_CALL_UNKNOWN) \
    INDIRECT_CALL_REASON_FUNC(CORINFO_INDIRECT_CALL_EXOTIC) \
    INDIRECT_CALL_REASON_FUNC(CORINFO_INDIRECT_CALL_PINVOKE) \
    INDIRECT_CALL_REASON_FUNC(CORINFO_INDIRECT_CALL_GENERIC) \
    INDIRECT_CALL_REASON_FUNC(CORINFO_INDIRECT_CALL_NO_CODE) \
    INDIRECT_CALL_REASON_FUNC(CORINFO_INDIRECT_CALL_FIXUPS) \
    INDIRECT_CALL_REASON_FUNC(CORINFO_INDIRECT_CALL_STUB) \
    INDIRECT_CALL_REASON_FUNC(CORINFO_INDIRECT_CALL_REMOTING) \
    INDIRECT_CALL_REASON_FUNC(CORINFO_INDIRECT_CALL_CER) \
    INDIRECT_CALL_REASON_FUNC(CORINFO_INDIRECT_CALL_RESTORE_METHOD) \
    INDIRECT_CALL_REASON_FUNC(CORINFO_INDIRECT_CALL_RESTORE_FIRST_CALL) \
    INDIRECT_CALL_REASON_FUNC(CORINFO_INDIRECT_CALL_RESTORE_VALUE_TYPE) \
    INDIRECT_CALL_REASON_FUNC(CORINFO_INDIRECT_CALL_RESTORE) \
    INDIRECT_CALL_REASON_FUNC(CORINFO_INDIRECT_CALL_CANT_PATCH) \
    INDIRECT_CALL_REASON_FUNC(CORINFO_INDIRECT_CALL_PROFILING) \
    INDIRECT_CALL_REASON_FUNC(CORINFO_INDIRECT_CALL_OTHER_LOADER_MODULE) \

enum CorInfoIndirectCallReason
{
    #undef INDIRECT_CALL_REASON_FUNC
    #define INDIRECT_CALL_REASON_FUNC(x) x,
    INDIRECT_CALL_REASONS()

    #undef INDIRECT_CALL_REASON_FUNC

    CORINFO_INDIRECT_CALL_COUNT
};

inline bool dontInline(CorInfoInline val) {
    return(val < 0);
}

// Patchpoint info is passed back and forth across the interface
// but is opaque.

struct PatchpointInfo;

// Cookie types consumed by the code generator (these are opaque values
// not inspected by the code generator):

typedef struct CORINFO_ASSEMBLY_STRUCT_*    CORINFO_ASSEMBLY_HANDLE;
typedef struct CORINFO_MODULE_STRUCT_*      CORINFO_MODULE_HANDLE;
typedef struct CORINFO_DEPENDENCY_STRUCT_*  CORINFO_DEPENDENCY_HANDLE;
typedef struct CORINFO_CLASS_STRUCT_*       CORINFO_CLASS_HANDLE;
typedef struct CORINFO_METHOD_STRUCT_*      CORINFO_METHOD_HANDLE;
typedef struct CORINFO_FIELD_STRUCT_*       CORINFO_FIELD_HANDLE;
typedef struct CORINFO_OBJECT_STRUCT_*      CORINFO_OBJECT_HANDLE;
typedef struct CORINFO_ARG_LIST_STRUCT_*    CORINFO_ARG_LIST_HANDLE;    // represents a list of argument types
typedef struct CORINFO_JUST_MY_CODE_HANDLE_*CORINFO_JUST_MY_CODE_HANDLE;
typedef struct CORINFO_PROFILING_STRUCT_*   CORINFO_PROFILING_HANDLE;   // a handle guaranteed to be unique per process
typedef struct CORINFO_GENERIC_STRUCT_*     CORINFO_GENERIC_HANDLE;     // a generic handle (could be any of the above)

// what is actually passed on the varargs call
typedef struct CORINFO_VarArgInfo *         CORINFO_VARARGS_HANDLE;

// Generic tokens are resolved with respect to a context, which is usually the method
// being compiled. The CORINFO_CONTEXT_HANDLE indicates which exact instantiation
// (or the open instantiation) is being referred to.
typedef struct CORINFO_CONTEXT_STRUCT_*     CORINFO_CONTEXT_HANDLE;

// MethodSignatureInfo is an opaque handle for passing method signature information across the Jit/EE interface
struct MethodSignatureInfo;

typedef struct CORINFO_DEPENDENCY_STRUCT_
{
    CORINFO_MODULE_HANDLE moduleFrom;
    CORINFO_MODULE_HANDLE moduleTo;
} CORINFO_DEPENDENCY;

// Bit-twiddling of contexts assumes word-alignment of method handles and type handles
// If this ever changes, some other encoding will be needed
enum CorInfoContextFlags
{
    CORINFO_CONTEXTFLAGS_METHOD = 0x00, // CORINFO_CONTEXT_HANDLE is really a CORINFO_METHOD_HANDLE
    CORINFO_CONTEXTFLAGS_CLASS  = 0x01, // CORINFO_CONTEXT_HANDLE is really a CORINFO_CLASS_HANDLE
    CORINFO_CONTEXTFLAGS_MASK   = 0x01
};

#define METHOD_BEING_COMPILED_CONTEXT() ((CORINFO_CONTEXT_HANDLE)1)
#define MAKE_CLASSCONTEXT(c)  (CORINFO_CONTEXT_HANDLE((size_t) (c) | CORINFO_CONTEXTFLAGS_CLASS))
#define MAKE_METHODCONTEXT(m) (CORINFO_CONTEXT_HANDLE((size_t) (m) | CORINFO_CONTEXTFLAGS_METHOD))

enum CorInfoSigInfoFlags
{
    CORINFO_SIGFLAG_IS_LOCAL_SIG           = 0x01,
    CORINFO_SIGFLAG_IL_STUB                = 0x02,
    // unused                              = 0x04,
    CORINFO_SIGFLAG_FAT_CALL               = 0x08,
};

struct CORINFO_SIG_INST
{
    unsigned                classInstCount;
    CORINFO_CLASS_HANDLE *  classInst; // (representative, not exact) instantiation for class type variables in signature
    unsigned                methInstCount;
    CORINFO_CLASS_HANDLE *  methInst; // (representative, not exact) instantiation for method type variables in signature
};

struct CORINFO_SIG_INFO
{
    CorInfoCallConv         callConv;
    CORINFO_CLASS_HANDLE    retTypeClass;   // if the return type is a value class, this is its handle (enums are normalized)
    CORINFO_CLASS_HANDLE    retTypeSigClass;// returns the value class as it is in the sig (enums are not converted to primitives)
    CorInfoType             retType : 8;
    unsigned                flags   : 8;    // used by IL stubs code
    unsigned                numArgs : 16;
    struct CORINFO_SIG_INST sigInst;        // information about how type variables are being instantiated in generic code
    CORINFO_ARG_LIST_HANDLE args;
    PCCOR_SIGNATURE         pSig;
    unsigned                cbSig;
    MethodSignatureInfo*    methodSignature;// used in place of pSig and cbSig to reference a method signature object handle
    CORINFO_MODULE_HANDLE   scope;          // passed to getArgClass
    mdToken                 token;

    CorInfoCallConv     getCallConv()       { return CorInfoCallConv((callConv & CORINFO_CALLCONV_MASK)); }
    bool                hasThis()           { return ((callConv & CORINFO_CALLCONV_HASTHIS) != 0); }
    bool                hasExplicitThis()   { return ((callConv & CORINFO_CALLCONV_EXPLICITTHIS) != 0); }
    bool                hasImplicitThis()   { return ((callConv & (CORINFO_CALLCONV_HASTHIS | CORINFO_CALLCONV_EXPLICITTHIS)) == CORINFO_CALLCONV_HASTHIS); }
    unsigned            totalILArgs()       { return (numArgs + (hasImplicitThis() ? 1 : 0)); }
    bool                isVarArg()          { return ((getCallConv() == CORINFO_CALLCONV_VARARG) || (getCallConv() == CORINFO_CALLCONV_NATIVEVARARG)); }
    bool                hasTypeArg()        { return ((callConv & CORINFO_CALLCONV_PARAMTYPE) != 0); }
};

struct CORINFO_METHOD_INFO
{
    CORINFO_METHOD_HANDLE       ftn;
    CORINFO_MODULE_HANDLE       scope;
    uint8_t *                   ILCode;
    unsigned                    ILCodeSize;
    unsigned                    maxStack;
    unsigned                    EHcount;
    CorInfoOptions              options;
    CorInfoRegionKind           regionKind;
    CORINFO_SIG_INFO            args;
    CORINFO_SIG_INFO            locals;
};

//----------------------------------------------------------------------------
// Looking up handles and addresses.
//
// When the JIT requests a handle, the EE may direct the JIT that it must
// access the handle in a variety of ways.  These are packed as
//    CORINFO_CONST_LOOKUP
// or CORINFO_LOOKUP (contains either a CORINFO_CONST_LOOKUP or a CORINFO_RUNTIME_LOOKUP)
//
// Constant Lookups v. Runtime Lookups (i.e. when will Runtime Lookups be generated?)
// -----------------------------------------------------------------------------------
//
// CORINFO_LOOKUP_KIND is part of the result type of embedGenericHandle,
// getVirtualCallInfo and any other functions that may require a
// runtime lookup when compiling shared generic code.
//
// CORINFO_LOOKUP_KIND indicates whether a particular token in the instruction stream can be:
// (a) Mapped to a handle (type, field or method) at compile-time (!needsRuntimeLookup)
// (b) Must be looked up at run-time, and if so which runtime lookup technique should be used (see below)
//
// If the JIT or EE does not support code sharing for generic code, then
// all CORINFO_LOOKUP results will be "constant lookups", i.e.
// the needsRuntimeLookup of CORINFO_LOOKUP.lookupKind.needsRuntimeLookup
// will be false.
//
// Constant Lookups
// ----------------
//
// Constant Lookups are either:
//     IAT_VALUE: immediate (relocatable) values,
//     IAT_PVALUE: immediate values access via an indirection through an immediate (relocatable) address
//     IAT_RELPVALUE: immediate values access via a relative indirection through an immediate offset
//     IAT_PPVALUE: immediate values access via a double indirection through an immediate (relocatable) address
//
// Runtime Lookups
// ---------------
//
// CORINFO_LOOKUP_KIND is part of the result type of embedGenericHandle,
// getVirtualCallInfo and any other functions that may require a
// runtime lookup when compiling shared generic code.
//
// CORINFO_LOOKUP_KIND indicates whether a particular token in the instruction stream can be:
// (a) Mapped to a handle (type, field or method) at compile-time (!needsRuntimeLookup)
// (b) Must be looked up at run-time using the class dictionary
//     stored in the vtable of the this pointer (needsRuntimeLookup && THISOBJ)
// (c) Must be looked up at run-time using the method dictionary
//     stored in the method descriptor parameter passed to a generic
//     method (needsRuntimeLookup && METHODPARAM)
// (d) Must be looked up at run-time using the class dictionary stored
//     in the vtable parameter passed to a method in a generic
//     struct (needsRuntimeLookup && CLASSPARAM)

struct CORINFO_CONST_LOOKUP
{
    // If the handle is obtained at compile-time, then this handle is the "exact" handle (class, method, or field)
    // Otherwise, it's a representative...
    // If accessType is
    //     IAT_VALUE     --> "handle" stores the real handle or "addr " stores the computed address
    //     IAT_PVALUE    --> "addr" stores a pointer to a location which will hold the real handle
    //     IAT_RELPVALUE --> "addr" stores a relative pointer to a location which will hold the real handle
    //     IAT_PPVALUE   --> "addr" stores a double indirection to a location which will hold the real handle

    InfoAccessType              accessType;
    union
    {
        CORINFO_GENERIC_HANDLE  handle;
        void *                  addr;
    };
};

enum CORINFO_RUNTIME_LOOKUP_KIND
{
    CORINFO_LOOKUP_THISOBJ,
    CORINFO_LOOKUP_METHODPARAM,
    CORINFO_LOOKUP_CLASSPARAM,
    CORINFO_LOOKUP_NOT_SUPPORTED, // Returned for attempts to inline dictionary lookups
};

struct CORINFO_LOOKUP_KIND
{
    bool                        needsRuntimeLookup;
    CORINFO_RUNTIME_LOOKUP_KIND runtimeLookupKind;

    // The 'runtimeLookupFlags' and 'runtimeLookupArgs' fields
    // are just for internal VM / ZAP communication, not to be used by the JIT.
    uint16_t                    runtimeLookupFlags;
    void *                      runtimeLookupArgs;
} ;


// CORINFO_RUNTIME_LOOKUP indicates the details of the runtime lookup
// operation to be performed.
//
// CORINFO_MAXINDIRECTIONS is the maximum number of
// indirections used by runtime lookups.
// This accounts for up to 2 indirections to get at a dictionary followed by a possible spill slot
//
#define CORINFO_MAXINDIRECTIONS 4
#define CORINFO_USEHELPER ((uint16_t) 0xffff)
#define CORINFO_USENULL ((uint16_t) 0xfffe)
#define CORINFO_NO_SIZE_CHECK ((uint16_t) 0xffff)

struct CORINFO_RUNTIME_LOOKUP
{
    // This is signature you must pass back to the runtime lookup helper
    void*                   signature;

    // Here is the helper you must call. It is one of CORINFO_HELP_RUNTIMEHANDLE_* helpers.
    CorInfoHelpFunc         helper;

    // Number of indirections to get there
    // CORINFO_USEHELPER = don't know how to get it, so use helper function at run-time instead
    // CORINFO_USENULL = the context should be null because the callee doesn't actually use it
    // 0 = use the this pointer itself (e.g. token is C<!0> inside code in sealed class C)
    //     or method desc itself (e.g. token is method void M::mymeth<!!0>() inside code in M::mymeth)
    // Otherwise, follow each byte-offset stored in the "offsets[]" array (may be negative)
    uint16_t                indirections;

    // If set, test for null and branch to helper if null
    bool                    testForNull;

    uint16_t                sizeOffset;
    size_t                  offsets[CORINFO_MAXINDIRECTIONS];

    // If set, first offset is indirect.
    // 0 means that value stored at first offset (offsets[0]) from pointer is next pointer, to which the next offset
    // (offsets[1]) is added and so on.
    // 1 means that value stored at first offset (offsets[0]) from pointer is offset1, and the next pointer is
    // stored at pointer+offsets[0]+offset1.
    bool                indirectFirstOffset;

    // If set, second offset is indirect.
    // 0 means that value stored at second offset (offsets[1]) from pointer is next pointer, to which the next offset
    // (offsets[2]) is added and so on.
    // 1 means that value stored at second offset (offsets[1]) from pointer is offset2, and the next pointer is
    // stored at pointer+offsets[1]+offset2.
    bool                indirectSecondOffset;
} ;

// Result of calling embedGenericHandle
struct CORINFO_LOOKUP
{
    CORINFO_LOOKUP_KIND     lookupKind;

    union
    {
        // If kind.needsRuntimeLookup then this indicates how to do the lookup
        CORINFO_RUNTIME_LOOKUP  runtimeLookup;

        // If the handle is obtained at compile-time, then this handle is the "exact" handle (class, method, or field)
        // Otherwise, it's a representative...  If accessType is
        //     IAT_VALUE --> "handle" stores the real handle or "addr " stores the computed address
        //     IAT_PVALUE --> "addr" stores a pointer to a location which will hold the real handle
        //     IAT_RELPVALUE --> "addr" stores a relative pointer to a location which will hold the real handle
        //     IAT_PPVALUE --> "addr" stores a double indirection to a location which will hold the real handle
        CORINFO_CONST_LOOKUP    constLookup;
    };
};

enum CorInfoGenericHandleType
{
    CORINFO_HANDLETYPE_UNKNOWN,
    CORINFO_HANDLETYPE_CLASS,
    CORINFO_HANDLETYPE_METHOD,
    CORINFO_HANDLETYPE_FIELD
};

//----------------------------------------------------------------------------
// Embedding type, method and field handles (for "ldtoken" or to pass back to helpers)

// Result of calling embedGenericHandle
struct CORINFO_GENERICHANDLE_RESULT
{
    CORINFO_LOOKUP          lookup;

    // compileTimeHandle is guaranteed to be either NULL or a handle that is usable during compile time.
    // It must not be embedded in the code because it might not be valid at run-time.
    CORINFO_GENERIC_HANDLE  compileTimeHandle;

    // Type of the result
    CorInfoGenericHandleType handleType;
};

#define CORINFO_ACCESS_ALLOWED_MAX_ARGS 4

enum CorInfoAccessAllowedHelperArgType
{
    CORINFO_HELPER_ARG_TYPE_Invalid = 0,
    CORINFO_HELPER_ARG_TYPE_Field   = 1,
    CORINFO_HELPER_ARG_TYPE_Method  = 2,
    CORINFO_HELPER_ARG_TYPE_Class   = 3,
    CORINFO_HELPER_ARG_TYPE_Module  = 4,
    CORINFO_HELPER_ARG_TYPE_Const   = 5,
};
struct CORINFO_HELPER_ARG
{
    union
    {
        CORINFO_FIELD_HANDLE fieldHandle;
        CORINFO_METHOD_HANDLE methodHandle;
        CORINFO_CLASS_HANDLE classHandle;
        CORINFO_MODULE_HANDLE moduleHandle;
        size_t constant;
    };
    CorInfoAccessAllowedHelperArgType argType;

    void Set(CORINFO_METHOD_HANDLE handle)
    {
        argType = CORINFO_HELPER_ARG_TYPE_Method;
        methodHandle = handle;
    }

    void Set(CORINFO_FIELD_HANDLE handle)
    {
        argType = CORINFO_HELPER_ARG_TYPE_Field;
        fieldHandle = handle;
    }

    void Set(CORINFO_CLASS_HANDLE handle)
    {
        argType = CORINFO_HELPER_ARG_TYPE_Class;
        classHandle = handle;
    }

    void Set(size_t value)
    {
        argType = CORINFO_HELPER_ARG_TYPE_Const;
        constant = value;
    }
};

struct CORINFO_HELPER_DESC
{
    CorInfoHelpFunc helperNum;
    unsigned numArgs;
    CORINFO_HELPER_ARG args[CORINFO_ACCESS_ALLOWED_MAX_ARGS];
};

//----------------------------------------------------------------------------
// getCallInfo and CORINFO_CALL_INFO: The EE instructs the JIT about how to make a call
//
// callKind
// --------
//
// CORINFO_CALL :
//   Indicates that the JIT can use getFunctionEntryPoint to make a call,
//   i.e. there is nothing abnormal about the call.  The JITs know what to do if they get this.
//   Except in the case of constraint calls (see below), [targetMethodHandle] will hold
//   the CORINFO_METHOD_HANDLE that a call to findMethod would
//   have returned.
//   This flag may be combined with nullInstanceCheck=TRUE for uses of callvirt on methods that can
//   be resolved at compile-time (non-virtual, final or sealed).
//
// CORINFO_CALL_CODE_POINTER (shared generic code only) :
//   Indicates that the JIT should do an indirect call to the entrypoint given by address, which may be specified
//   as a runtime lookup by CORINFO_CALL_INFO::codePointerLookup.
//   [targetMethodHandle] will not hold a valid value.
//   This flag may be combined with nullInstanceCheck=TRUE for uses of callvirt on methods whose target method can
//   be resolved at compile-time but whose instantiation can be resolved only through runtime lookup.
//
// CORINFO_VIRTUALCALL_STUB (interface calls) :
//   Indicates that the EE supports "stub dispatch" and request the JIT to make a
//   "stub dispatch" call (an indirect call through CORINFO_CALL_INFO::stubLookup,
//   similar to CORINFO_CALL_CODE_POINTER).
//   "Stub dispatch" is a specialized calling sequence (that may require use of NOPs)
//   which allow the runtime to determine the call-site after the call has been dispatched.
//   If the call is too complex for the JIT (e.g. because
//   fetching the dispatch stub requires a runtime lookup, i.e. lookupKind.needsRuntimeLookup
//   is set) then the JIT is allowed to implement the call as if it were CORINFO_VIRTUALCALL_LDVIRTFTN
//   [targetMethodHandle] will hold the CORINFO_METHOD_HANDLE that a call to findMethod would
//   have returned.
//   This flag is always accompanied by nullInstanceCheck=TRUE.
//
// CORINFO_VIRTUALCALL_LDVIRTFTN (virtual generic methods) :
//   Indicates that the EE provides no way to implement the call directly and
//   that the JIT should use a LDVIRTFTN sequence (as implemented by CORINFO_HELP_VIRTUAL_FUNC_PTR)
//   followed by an indirect call.
//   [targetMethodHandle] will hold the CORINFO_METHOD_HANDLE that a call to findMethod would
//   have returned.
//   This flag is always accompanied by nullInstanceCheck=TRUE though typically the null check will
//   be implicit in the access through the instance pointer.
//
//  CORINFO_VIRTUALCALL_VTABLE (regular virtual methods) :
//   Indicates that the EE supports vtable dispatch and that the JIT should use getVTableOffset etc.
//   to implement the call.
//   [targetMethodHandle] will hold the CORINFO_METHOD_HANDLE that a call to findMethod would
//   have returned.
//   This flag is always accompanied by nullInstanceCheck=TRUE though typically the null check will
//   be implicit in the access through the instance pointer.
//
// thisTransform and constraint calls
// ----------------------------------
//
// For everything besides "constrained." calls "thisTransform" is set to
// CORINFO_NO_THIS_TRANSFORM.
//
// For "constrained." calls the EE attempts to resolve the call at compile
// time to a more specific method, or (shared generic code only) to a runtime lookup
// for a code pointer for the more specific method.
//
// In order to permit this, the "this" pointer supplied for a "constrained." call
// is a byref to an arbitrary type (see the IL spec). The "thisTransform" field
// will indicate how the JIT must transform the "this" pointer in order
// to be able to call the resolved method:
//
//  CORINFO_NO_THIS_TRANSFORM --> Leave it as a byref to an unboxed value type
//  CORINFO_BOX_THIS          --> Box it to produce an object
//  CORINFO_DEREF_THIS        --> Deref the byref to get an object reference
//
// In addition, the "kind" field will be set as follows for constraint calls:
//
//    CORINFO_CALL              --> the call was resolved at compile time, and
//                                  can be compiled like a normal call.
//    CORINFO_CALL_CODE_POINTER --> the call was resolved, but the target address will be
//                                  computed at runtime.  Only returned for shared generic code.
//    CORINFO_VIRTUALCALL_STUB,
//    CORINFO_VIRTUALCALL_LDVIRTFTN,
//    CORINFO_VIRTUALCALL_VTABLE   --> usual values indicating that a virtual call must be made

enum CORINFO_CALL_KIND
{
    CORINFO_CALL,
    CORINFO_CALL_CODE_POINTER,
    CORINFO_VIRTUALCALL_STUB,
    CORINFO_VIRTUALCALL_LDVIRTFTN,
    CORINFO_VIRTUALCALL_VTABLE
};

// Indicates that the CORINFO_VIRTUALCALL_VTABLE lookup needn't do a chunk indirection
#define CORINFO_VIRTUALCALL_NO_CHUNK 0xFFFFFFFF

enum CORINFO_THIS_TRANSFORM
{
    CORINFO_NO_THIS_TRANSFORM,
    CORINFO_BOX_THIS,
    CORINFO_DEREF_THIS
};

enum CORINFO_CALLINFO_FLAGS
{
    CORINFO_CALLINFO_NONE           = 0x0000,
    CORINFO_CALLINFO_ALLOWINSTPARAM = 0x0001,   // Can the compiler generate code to pass an instantiation parameters? Simple compilers should not use this flag
    CORINFO_CALLINFO_CALLVIRT       = 0x0002,   // Is it a virtual call?
    // UNUSED                       = 0x0004,
    // UNUSED                       = 0x0008,
    CORINFO_CALLINFO_SECURITYCHECKS = 0x0010,   // Perform security checks.
    CORINFO_CALLINFO_LDFTN          = 0x0020,   // Resolving target of LDFTN
    // UNUSED                       = 0x0040,
};

enum CorInfoIsAccessAllowedResult
{
    CORINFO_ACCESS_ALLOWED = 0,           // Call allowed
    CORINFO_ACCESS_ILLEGAL = 1,           // Call not allowed
};


// This enum is used for JIT to tell EE where this token comes from.
// E.g. Depending on different opcodes, we might allow/disallow certain types of tokens or
// return different types of handles (e.g. boxed vs. regular entrypoints)
enum CorInfoTokenKind
{
    CORINFO_TOKENKIND_Class     = 0x01,
    CORINFO_TOKENKIND_Method    = 0x02,
    CORINFO_TOKENKIND_Field     = 0x04,
    CORINFO_TOKENKIND_Mask      = 0x07,

    // token comes from CEE_LDTOKEN
    CORINFO_TOKENKIND_Ldtoken   = 0x10 | CORINFO_TOKENKIND_Class | CORINFO_TOKENKIND_Method | CORINFO_TOKENKIND_Field,

    // token comes from CEE_CASTCLASS or CEE_ISINST
    CORINFO_TOKENKIND_Casting   = 0x20 | CORINFO_TOKENKIND_Class,

    // token comes from CEE_NEWARR
    CORINFO_TOKENKIND_Newarr    = 0x40 | CORINFO_TOKENKIND_Class,

    // token comes from CEE_BOX
    CORINFO_TOKENKIND_Box       = 0x80 | CORINFO_TOKENKIND_Class,

    // token comes from CEE_CONSTRAINED
    CORINFO_TOKENKIND_Constrained = 0x100 | CORINFO_TOKENKIND_Class,

    // token comes from CEE_NEWOBJ
    CORINFO_TOKENKIND_NewObj    = 0x200 | CORINFO_TOKENKIND_Method,

    // token comes from CEE_LDVIRTFTN
    CORINFO_TOKENKIND_Ldvirtftn = 0x400 | CORINFO_TOKENKIND_Method,

    // token comes from devirtualizing a method
    CORINFO_TOKENKIND_DevirtualizedMethod = 0x800 | CORINFO_TOKENKIND_Method,
};

struct CORINFO_RESOLVED_TOKEN
{
    //
    // [In] arguments of resolveToken
    //
    CORINFO_CONTEXT_HANDLE  tokenContext;       //Context for resolution of generic arguments
    CORINFO_MODULE_HANDLE   tokenScope;
    mdToken                 token;              //The source token
    CorInfoTokenKind        tokenType;

    //
    // [Out] arguments of resolveToken.
    // - Type handle is always non-NULL.
    // - At most one of method and field handles is non-NULL (according to the token type).
    // - Method handle is an instantiating stub only for generic methods. Type handle
    //   is required to provide the full context for methods in generic types.
    //
    CORINFO_CLASS_HANDLE    hClass;
    CORINFO_METHOD_HANDLE   hMethod;
    CORINFO_FIELD_HANDLE    hField;

    //
    // [Out] TypeSpec and MethodSpec signatures for generics. NULL otherwise.
    //
    PCCOR_SIGNATURE         pTypeSpec;
    uint32_t                cbTypeSpec;
    PCCOR_SIGNATURE         pMethodSpec;
    uint32_t                cbMethodSpec;
};

struct CORINFO_CALL_INFO
{
    CORINFO_METHOD_HANDLE   hMethod;            //target method handle
    unsigned                methodFlags;        //flags for the target method

    unsigned                classFlags;         //flags for CORINFO_RESOLVED_TOKEN::hClass

    CORINFO_SIG_INFO        sig;

    //If set to:
    //  - CORINFO_ACCESS_ALLOWED - The access is allowed.
    //  - CORINFO_ACCESS_ILLEGAL - This access cannot be allowed (i.e. it is public calling private).  The
    //      JIT may either insert the callsiteCalloutHelper into the code (as per a verification error) or
    //      call throwExceptionFromHelper on the callsiteCalloutHelper.  In this case callsiteCalloutHelper
    //      is guaranteed not to return.
    CorInfoIsAccessAllowedResult accessAllowed;
    CORINFO_HELPER_DESC     callsiteCalloutHelper;

    // See above section on constraintCalls to understand when these are set to unusual values.
    CORINFO_THIS_TRANSFORM  thisTransform;

    CORINFO_CALL_KIND       kind;
    bool                    nullInstanceCheck;

    // Context for inlining and hidden arg
    CORINFO_CONTEXT_HANDLE  contextHandle;
    bool                    exactContextNeedsRuntimeLookup; // Set if contextHandle is approx handle. Runtime lookup is required to get the exact handle.

    // If kind.CORINFO_VIRTUALCALL_STUB then stubLookup will be set.
    // If kind.CORINFO_CALL_CODE_POINTER then entryPointLookup will be set.
    union
    {
        CORINFO_LOOKUP      stubLookup;

        CORINFO_LOOKUP      codePointerLookup;
    };

    CORINFO_CONST_LOOKUP    instParamLookup;

    bool                    wrapperDelegateInvoke;
};

enum CORINFO_DEVIRTUALIZATION_DETAIL
{
    CORINFO_DEVIRTUALIZATION_UNKNOWN,                              // no details available
    CORINFO_DEVIRTUALIZATION_SUCCESS,                              // devirtualization was successful
    CORINFO_DEVIRTUALIZATION_FAILED_CANON,                         // object class was canonical
    CORINFO_DEVIRTUALIZATION_FAILED_COM,                           // object class was com
    CORINFO_DEVIRTUALIZATION_FAILED_CAST,                          // object class could not be cast to interface class
    CORINFO_DEVIRTUALIZATION_FAILED_LOOKUP,                        // interface method could not be found
    CORINFO_DEVIRTUALIZATION_FAILED_DIM,                           // interface method was default interface method
    CORINFO_DEVIRTUALIZATION_FAILED_SUBCLASS,                      // object not subclass of base class
    CORINFO_DEVIRTUALIZATION_FAILED_SLOT,                          // virtual method installed via explicit override
    CORINFO_DEVIRTUALIZATION_FAILED_BUBBLE,                        // devirtualization crossed version bubble
    CORINFO_DEVIRTUALIZATION_MULTIPLE_IMPL,                        // object has multiple implementations of interface class
    CORINFO_DEVIRTUALIZATION_FAILED_BUBBLE_CLASS_DECL,             // decl method is defined on class and decl method not in version bubble, and decl method not in closest to version bubble
    CORINFO_DEVIRTUALIZATION_FAILED_BUBBLE_INTERFACE_DECL,         // decl method is defined on interface and not in version bubble, and implementation type not entirely defined in bubble
    CORINFO_DEVIRTUALIZATION_FAILED_BUBBLE_IMPL,                   // object class not defined within version bubble
    CORINFO_DEVIRTUALIZATION_FAILED_BUBBLE_IMPL_NOT_REFERENCEABLE, // object class cannot be referenced from R2R code due to missing tokens
    CORINFO_DEVIRTUALIZATION_FAILED_DUPLICATE_INTERFACE,           // crossgen2 virtual method algorithm and runtime algorithm differ in the presence of duplicate interface implementations
    CORINFO_DEVIRTUALIZATION_FAILED_DECL_NOT_REPRESENTABLE,        // Decl method cannot be represented in R2R image
    CORINFO_DEVIRTUALIZATION_FAILED_TYPE_EQUIVALENCE,              // Support for type equivalence in devirtualization is not yet implemented in crossgen2
    CORINFO_DEVIRTUALIZATION_COUNT,                                // sentinel for maximum value
};

struct CORINFO_DEVIRTUALIZATION_INFO
{
    //
    // [In] arguments of resolveVirtualMethod
    //
    CORINFO_METHOD_HANDLE       virtualMethod;
    CORINFO_CLASS_HANDLE        objClass;
    CORINFO_CONTEXT_HANDLE      context;
    CORINFO_RESOLVED_TOKEN     *pResolvedTokenVirtualMethod;

    //
    // [Out] results of resolveVirtualMethod.
    // - devirtualizedMethod is set to MethodDesc of devirt'ed method iff we were able to devirtualize.
    //      invariant is `resolveVirtualMethod(...) == (devirtualizedMethod != nullptr)`.
    // - requiresInstMethodTableArg is set to TRUE if the devirtualized method requires a type handle arg.
    // - exactContext is set to wrapped CORINFO_CLASS_HANDLE of devirt'ed method table.
    // - details on the computation done by the jit host
    // - If pResolvedTokenDevirtualizedMethod is not set to NULL and targeting an R2R image
    //   use it as the parameter to getCallInfo
    //
    CORINFO_METHOD_HANDLE           devirtualizedMethod;
    bool                            requiresInstMethodTableArg;
    CORINFO_CONTEXT_HANDLE          exactContext;
    CORINFO_DEVIRTUALIZATION_DETAIL detail;
    CORINFO_RESOLVED_TOKEN          resolvedTokenDevirtualizedMethod;
    CORINFO_RESOLVED_TOKEN          resolvedTokenDevirtualizedUnboxedMethod;
};

//----------------------------------------------------------------------------
// getFieldInfo and CORINFO_FIELD_INFO: The EE instructs the JIT about how to access a field

enum CORINFO_FIELD_ACCESSOR
{
    CORINFO_FIELD_INSTANCE,                 // regular instance field at given offset from this-ptr
    CORINFO_FIELD_INSTANCE_WITH_BASE,       // instance field with base offset (used by Ready-to-Run)
    CORINFO_FIELD_INSTANCE_HELPER,          // instance field accessed using helper (arguments are this, FieldDesc * and the value)
    CORINFO_FIELD_INSTANCE_ADDR_HELPER,     // instance field accessed using address-of helper (arguments are this and FieldDesc *)

    CORINFO_FIELD_STATIC_ADDRESS,           // field at given address
    CORINFO_FIELD_STATIC_RVA_ADDRESS,       // RVA field at given address
    CORINFO_FIELD_STATIC_SHARED_STATIC_HELPER, // static field accessed using the "shared static" helper (arguments are ModuleID + ClassID)
    CORINFO_FIELD_STATIC_GENERICS_STATIC_HELPER, // static field access using the "generic static" helper (argument is MethodTable *)
    CORINFO_FIELD_STATIC_ADDR_HELPER,       // static field accessed using address-of helper (argument is FieldDesc *)
    CORINFO_FIELD_STATIC_TLS,               // unmanaged TLS access
    CORINFO_FIELD_STATIC_TLS_MANAGED,       // managed TLS access
    CORINFO_FIELD_STATIC_READYTORUN_HELPER, // static field access using a runtime lookup helper
    CORINFO_FIELD_STATIC_RELOCATABLE,       // static field access using relocation (used in AOT)
    CORINFO_FIELD_INTRINSIC_ZERO,           // intrinsic zero (IntPtr.Zero, UIntPtr.Zero)
    CORINFO_FIELD_INTRINSIC_EMPTY_STRING,   // intrinsic emptry string (String.Empty)
    CORINFO_FIELD_INTRINSIC_ISLITTLEENDIAN, // intrinsic BitConverter.IsLittleEndian
};

// Set of flags returned in CORINFO_FIELD_INFO::fieldFlags
enum CORINFO_FIELD_FLAGS
{
    CORINFO_FLG_FIELD_STATIC                    = 0x00000001,
    CORINFO_FLG_FIELD_UNMANAGED                 = 0x00000002, // RVA field
    CORINFO_FLG_FIELD_FINAL                     = 0x00000004,
    CORINFO_FLG_FIELD_STATIC_IN_HEAP            = 0x00000008, // See code:#StaticFields. This static field is in the GC heap as a boxed object
    CORINFO_FLG_FIELD_INITCLASS                 = 0x00000020, // initClass has to be called before accessing the field
};

struct CORINFO_FIELD_INFO
{
    CORINFO_FIELD_ACCESSOR  fieldAccessor;
    unsigned                fieldFlags;

    // Helper to use if the field access requires it
    CorInfoHelpFunc         helper;

    // Field offset if there is one
    uint32_t                offset;

    CorInfoType             fieldType;
    CORINFO_CLASS_HANDLE    structType; //possibly null

    //See CORINFO_CALL_INFO.accessAllowed
    CorInfoIsAccessAllowedResult accessAllowed;
    CORINFO_HELPER_DESC     accessCalloutHelper;

    CORINFO_CONST_LOOKUP    fieldLookup;        // Used by Ready-to-Run
};

//----------------------------------------------------------------------------
// getThreadLocalStaticBlocksInfo and CORINFO_THREAD_STATIC_BLOCKS_INFO: The EE instructs the JIT about how to access a thread local field

struct CORINFO_THREAD_STATIC_BLOCKS_INFO
{
    CORINFO_CONST_LOOKUP tlsIndex;              // windows specific
    void* tlsGetAddrFtnPtr;                     // linux/x64 specific - address of __tls_get_addr() function
    void* tlsIndexObject;                       // linux/x64 specific - address of tls_index object
    void* threadVarsSection;                    // osx x64/arm64 specific - address of __thread_vars section of `t_ThreadStatics`
    uint32_t offsetOfThreadLocalStoragePointer; // windows specific
    uint32_t offsetOfMaxThreadStaticBlocks;
    uint32_t offsetOfThreadStaticBlocks;
    uint32_t offsetOfGCDataPointer;
};

//----------------------------------------------------------------------------
// getThreadLocalStaticInfo_NativeAOT and CORINFO_THREAD_STATIC_INFO_NATIVEAOT: The EE instructs the JIT about how to access a thread local field

struct CORINFO_THREAD_STATIC_INFO_NATIVEAOT
{
    uint32_t offsetOfThreadLocalStoragePointer;
    CORINFO_CONST_LOOKUP tlsRootObject;
    CORINFO_CONST_LOOKUP tlsIndexObject;
    CORINFO_CONST_LOOKUP threadStaticBaseSlow;
};

//----------------------------------------------------------------------------
// Exception handling

struct CORINFO_EH_CLAUSE
{
    CORINFO_EH_CLAUSE_FLAGS     Flags;
    uint32_t                    TryOffset;
    uint32_t                    TryLength;
    uint32_t                    HandlerOffset;
    uint32_t                    HandlerLength;
    union
    {
        uint32_t                ClassToken;       // use for type-based exception handlers
        uint32_t                FilterOffset;     // use for filter-based exception handlers (COR_ILEXCEPTION_FILTER is set)
    };
};

enum CORINFO_OS
{
    CORINFO_WINNT,
    CORINFO_UNIX,
    CORINFO_APPLE,
};

enum CORINFO_RUNTIME_ABI
{
    CORINFO_CORECLR_ABI = 0x200,
    CORINFO_NATIVEAOT_ABI = 0x300,
};

// For some highly optimized paths, the JIT must generate code that directly
// manipulates internal EE data structures. The getEEInfo() helper returns
// this structure containing the needed offsets and values.
struct CORINFO_EE_INFO
{
    // Information about the InlinedCallFrame structure layout
    struct InlinedCallFrameInfo
    {
        // Size of the Frame structure
        unsigned    size;

        unsigned    offsetOfGSCookie;
        unsigned    offsetOfFrameVptr;
        unsigned    offsetOfFrameLink;
        unsigned    offsetOfCallSiteSP;
        unsigned    offsetOfCalleeSavedFP;
        unsigned    offsetOfCallTarget;
        unsigned    offsetOfReturnAddress;
        // This offset is used only for ARM
        unsigned    offsetOfSPAfterProlog;
    }
    inlinedCallFrameInfo;

    // Offsets into the Thread structure
    unsigned    offsetOfThreadFrame;            // offset of the current Frame
    unsigned    offsetOfGCState;                // offset of the preemptive/cooperative state of the Thread

    // Delegate offsets
    unsigned    offsetOfDelegateInstance;
    unsigned    offsetOfDelegateFirstTarget;

    // Wrapper delegate offsets
    unsigned    offsetOfWrapperDelegateIndirectCell;

    // Reverse PInvoke offsets
    unsigned    sizeOfReversePInvokeFrame;

    // OS Page size
    size_t      osPageSize;

    // Null object offset
    size_t      maxUncheckedOffsetForNullObject;

    // Target ABI. Combined with target architecture and OS to determine
    // GC, EH, and unwind styles.
    CORINFO_RUNTIME_ABI targetAbi;

    CORINFO_OS  osType;
};

// Flags passed from JIT to runtime.
enum CORINFO_GET_TAILCALL_HELPERS_FLAGS
{
    // The callsite is a callvirt instruction.
    CORINFO_TAILCALL_IS_CALLVIRT       = 0x00000001,
    CORINFO_TAILCALL_THIS_ARG_IS_BYREF = 0x00000002,
};

// Flags passed from runtime to JIT.
enum CORINFO_TAILCALL_HELPERS_FLAGS
{
    // The StoreArgs stub needs to be passed the target function pointer as the
    // first argument.
    CORINFO_TAILCALL_STORE_TARGET = 0x00000001,
};

struct CORINFO_TAILCALL_HELPERS
{
    CORINFO_TAILCALL_HELPERS_FLAGS flags;
    CORINFO_METHOD_HANDLE          hStoreArgs;
    CORINFO_METHOD_HANDLE          hCallTarget;
    CORINFO_METHOD_HANDLE          hDispatcher;
};

// This is used to indicate that a finally has been called
// "locally" by the try block
enum { LCL_FINALLY_MARK = 0xFC }; // FC = "Finally Call"

/**********************************************************************************
 * The following is the internal structure of an object that the compiler knows about
 * when it generates code
 **********************************************************************************/

typedef void* CORINFO_MethodPtr;            // a generic method pointer

struct CORINFO_Object
{
    CORINFO_MethodPtr      *methTable;      // the vtable for the object
};

struct CORINFO_String : public CORINFO_Object
{
    unsigned                stringLen;
    char16_t                chars[1];       // actually of variable size
};

struct CORINFO_Array : public CORINFO_Object
{
    unsigned                length;
#ifdef HOST_64BIT
    unsigned                alignpad;
#endif // HOST_64BIT

#if 0
    // Multi-dimensional arrays have the dimension lengths and bounds here.
    // The element count of these arrays is the array rank (the number of dimensions in the
    // multi-dimensional array). So, there is one element for each dimension. The upper bound
    // of a dimension is `dimBound[d] + dimLength[d] - 1`.
    int                     dimLength[rank]; // Number of array elements in each dimension.
    int                     dimBound[rank];  // Lower bound of each dimension (possibly negative).
#endif

    union
    {
        int8_t              i1Elems[1];    // actually of variable size
        uint8_t             u1Elems[1];
        int16_t             i2Elems[1];
        uint16_t            u2Elems[1];
        int32_t             i4Elems[1];
        uint32_t            u4Elems[1];
        float               r4Elems[1];
    };
};

struct CORINFO_Array8 : public CORINFO_Object
{
    unsigned                length;
#ifdef HOST_64BIT
    unsigned                alignpad;
#endif // HOST_64BIT

    union
    {
        double              r8Elems[1];
        int64_t             i8Elems[1];
        uint64_t            u8Elems[1];
    };
};


struct CORINFO_RefArray : public CORINFO_Object
{
    unsigned                length;
#ifdef HOST_64BIT
    unsigned                alignpad;
#endif // HOST_64BIT

#if 0
    /* Multi-dimensional arrays have the lengths and bounds here */
    unsigned                dimLength[length];
    unsigned                dimBound[length];
#endif

    CORINFO_Object*         refElems[1];    // actually of variable size;
};

struct CORINFO_RefAny
{
    void                      * dataPtr;
    CORINFO_CLASS_HANDLE        type;
};

// The jit assumes the CORINFO_VARARGS_HANDLE is a pointer to a subclass of this
struct CORINFO_VarArgInfo
{
    unsigned                argBytes;       // number of bytes the arguments take up.
                                            // (The CORINFO_VARARGS_HANDLE counts as an arg)
};

struct CORINFO_TYPE_LAYOUT_NODE
{
    // Type handle if this is a SIMD type, i.e. for intrinsic types in
    // System.Numerics and System.Runtime.Intrinsics namespaces. This handle
    // should be used for SIMD type recognition ONLY. During prejitting the
    // returned handle cannot safely be used for arbitrary JIT-EE calls. The
    // safe operations on this handle are:
    // - getClassNameFromMetadata
    // - getClassSize
    // - getHfaType
    // - getTypeInstantiationArgument, but only under the assumption that the returned type handle
    //   is used for primitive type recognition via getTypeForPrimitiveNumericClass
    CORINFO_CLASS_HANDLE simdTypeHnd;
    // Field handle that should only be used for diagnostic purposes. During
    // prejit we cannot allow arbitrary JIT-EE calls with this field handle, but it can be used
    // for diagnostic purposes (e.g. to obtain the field name).
    CORINFO_FIELD_HANDLE diagFieldHnd;
    // Index of parent node in the tree
    unsigned parent;
    // Offset into the root type of the field
    unsigned offset;
    // Size of the type.
    unsigned size;
    // Number of fields for type == CORINFO_TYPE_VALUECLASS. This is the number of nodes added.
    unsigned numFields;
    // Type of the field.
    CorInfoType type;
    // For type == CORINFO_TYPE_VALUECLASS indicates whether the type has significant padding.
    // That is, whether or not the JIT always needs to preserve data stored in
    // the parts that are not covered by fields.
    bool hasSignificantPadding;
};

enum class GetTypeLayoutResult
{
    Success,
    Partial,
    Failure,
};

#define SIZEOF__CORINFO_Object                            TARGET_POINTER_SIZE /* methTable */

#define CORINFO_Array_MaxLength                           0x7FFFFFC7
#define CORINFO_String_MaxLength                          0x3FFFFFDF

#define OFFSETOF__CORINFO_Array__length                   SIZEOF__CORINFO_Object
#ifdef TARGET_64BIT
#define OFFSETOF__CORINFO_Array__data                     (OFFSETOF__CORINFO_Array__length + sizeof(uint32_t) /* length */ + sizeof(uint32_t) /* alignpad */)
#else
#define OFFSETOF__CORINFO_Array__data                     (OFFSETOF__CORINFO_Array__length + sizeof(uint32_t) /* length */)
#endif

#define OFFSETOF__CORINFO_TypedReference__dataPtr         0
#define OFFSETOF__CORINFO_TypedReference__type            (OFFSETOF__CORINFO_TypedReference__dataPtr + TARGET_POINTER_SIZE /* dataPtr */)

#define OFFSETOF__CORINFO_String__stringLen               SIZEOF__CORINFO_Object
#define OFFSETOF__CORINFO_String__chars                   (OFFSETOF__CORINFO_String__stringLen + sizeof(uint32_t) /* stringLen */)

#define OFFSETOF__CORINFO_NullableOfT__hasValue           0

#define OFFSETOF__CORINFO_Span__reference                 0
#define OFFSETOF__CORINFO_Span__length                    TARGET_POINTER_SIZE


/* data to optimize delegate construction */
struct DelegateCtorArgs
{
    void * pMethod;
    void * pArg3;
    void * pArg4;
    void * pArg5;
};

// use offsetof to get the offset of the fields above
#include <stddef.h> // offsetof

// Guard-stack cookie for preventing against stack buffer overruns
typedef size_t GSCookie;

#include "cordebuginfo.h"

/**********************************************************************************/
// Some compilers cannot arbitrarily allow the handler nesting level to grow
// arbitrarily during Edit'n'Continue.
// This is the maximum nesting level that a compiler needs to support for EnC

const int MAX_EnC_HANDLER_NESTING_LEVEL = 6;

// Results from type comparison queries
enum class TypeCompareState
{
    MustNot = -1, // types are not equal
    May = 0,      // types may be equal (must test at runtime)
    Must = 1,     // type are equal
};

//
// This interface is logically split into sections for each class of information
// (ICorMethodInfo, ICorModuleInfo, etc.). This split used to exist physically as well
// using virtual inheritance, but was eliminated to improve efficiency of the JIT-EE
// interface calls.
//
class ICorStaticInfo
{
public:
    /**********************************************************************************/
    //
    // ICorMethodInfo
    //
    /**********************************************************************************/

    // Quick check whether the method is a jit intrinsic. Returns the same value as getMethodAttribs(ftn) & CORINFO_FLG_INTRINSIC, except faster.
    virtual bool isIntrinsic(CORINFO_METHOD_HANDLE ftn) = 0;

    // Notify EE about intent to rely on given MethodInfo in the current method
    // EE returns false if we're not allowed to do so and the methodinfo may change.
    // Example of a scenario addressed by notifyMethodInfoUsage:
    //  1) Crossgen (with --opt-cross-module=MyLib) attempts to inline a call from MyLib.dll into MyApp.dll
    //     and realizes that the call always throws.
    //  2) JIT aborts the inlining attempt and marks the call as no-return instead. The code that follows the call is 
    //     replaced with a breakpoint instruction that is expected to be unreachable.
    //  3) MyLib is updated to a new version so it's no longer within the same version bubble with MyApp.dll
    //     and the new version of the call no longer throws and does some work.
    //  4) The breakpoint instruction is now reachable in the MyApp.dll.
    //
    virtual bool notifyMethodInfoUsage(CORINFO_METHOD_HANDLE ftn) = 0;

    // return flags (a bitfield of CorInfoFlags values)
    virtual uint32_t getMethodAttribs (
            CORINFO_METHOD_HANDLE       ftn         /* IN */
            ) = 0;

    // sets private JIT flags, which can be, retrieved using getAttrib.
    virtual void setMethodAttribs (
            CORINFO_METHOD_HANDLE       ftn,        /* IN */
            CorInfoMethodRuntimeFlags   attribs     /* IN */
            ) = 0;

    // Given a method descriptor ftnHnd, extract signature information into sigInfo
    //
    // 'memberParent' is typically only set when verifying.  It should be the
    // result of calling getMemberParent.
    virtual void getMethodSig (
             CORINFO_METHOD_HANDLE      ftn,        /* IN  */
             CORINFO_SIG_INFO          *sig,        /* OUT */
             CORINFO_CLASS_HANDLE       memberParent = NULL /* IN */
             ) = 0;

    /*********************************************************************
     * Note the following methods can only be used on functions known
     * to be IL.  This includes the method being compiled and any method
     * that 'getMethodInfo' returns true for
     *********************************************************************/

    // return information about a method private to the implementation
    //      returns false if method is not IL, or is otherwise unavailable.
    //      This method is used to fetch data needed to inline functions
    virtual bool getMethodInfo (
            CORINFO_METHOD_HANDLE   ftn,            /* IN  */
            CORINFO_METHOD_INFO*    info,           /* OUT */
            CORINFO_CONTEXT_HANDLE  context = NULL  /* IN  */
            ) = 0;

    //------------------------------------------------------------------------------
    // haveSameMethodDefinition: Check if two method handles have the same
    // method definition.
    //
    // Arguments:
    //    meth1 - First method handle
    //    meth2 - Second method handle
    //
    // Return Value:
    //   True if the methods share definitions.
    //
    // Remarks:
    //   For example, Foo<int> and Foo<uint> have different method handles but
    //   share the same method definition.
    //
    virtual bool haveSameMethodDefinition(
        CORINFO_METHOD_HANDLE meth1Hnd,
        CORINFO_METHOD_HANDLE meth2Hnd) = 0;

    // Decides if you have any limitations for inlining. If everything's OK, it will return
    // INLINE_PASS.
    //
    // The callerHnd must be the immediate caller (i.e. when we have a chain of inlined calls)

    virtual CorInfoInline canInline (
            CORINFO_METHOD_HANDLE       callerHnd,                  /* IN  */
            CORINFO_METHOD_HANDLE       calleeHnd                   /* IN  */
            ) = 0;

    // Report that an inlining related process has begun. This will always be paired with
    // a call to reportInliningDecision unless the jit fails.
    virtual void beginInlining (CORINFO_METHOD_HANDLE inlinerHnd,
                                CORINFO_METHOD_HANDLE inlineeHnd) = 0;

    // Reports whether or not a method can be inlined, and why.  canInline is responsible for reporting all
    // inlining results when it returns INLINE_FAIL and INLINE_NEVER.  All other results are reported by the
    // JIT.
    virtual void reportInliningDecision (CORINFO_METHOD_HANDLE inlinerHnd,
                                         CORINFO_METHOD_HANDLE inlineeHnd,
                                         CorInfoInline inlineResult,
                                         const char * reason) = 0;


    // Returns false if the call is across security boundaries thus we cannot tailcall
    //
    // The callerHnd must be the immediate caller (i.e. when we have a chain of inlined calls)
    virtual bool canTailCall (
            CORINFO_METHOD_HANDLE   callerHnd,          /* IN */
            CORINFO_METHOD_HANDLE   declaredCalleeHnd,  /* IN */
            CORINFO_METHOD_HANDLE   exactCalleeHnd,     /* IN */
            bool                    fIsTailPrefix       /* IN */
            ) = 0;

    // Reports whether or not a method can be tail called, and why.
    // canTailCall is responsible for reporting all results when it returns
    // false.  All other results are reported by the JIT.
    virtual void reportTailCallDecision (CORINFO_METHOD_HANDLE callerHnd,
                                         CORINFO_METHOD_HANDLE calleeHnd,
                                         bool fIsTailPrefix,
                                         CorInfoTailCall tailCallResult,
                                         const char * reason) = 0;

    // get individual exception handler
    virtual void getEHinfo(
            CORINFO_METHOD_HANDLE ftn,              /* IN  */
            unsigned              EHnumber,         /* IN */
            CORINFO_EH_CLAUSE*    clause            /* OUT */
            ) = 0;

    // return class it belongs to
    virtual CORINFO_CLASS_HANDLE getMethodClass (
            CORINFO_METHOD_HANDLE       method
            ) = 0;

    // This function returns the offset of the specified method in the
    // vtable of it's owning class or interface.
    virtual void getMethodVTableOffset (
            CORINFO_METHOD_HANDLE       method,                 /* IN */
            unsigned*                   offsetOfIndirection,    /* OUT */
            unsigned*                   offsetAfterIndirection, /* OUT */
            bool*                       isRelative              /* OUT */
            ) = 0;

    // Finds the virtual method in info->objClass that overrides info->virtualMethod,
    // or the method in info->objClass that implements the interface method
    // represented by info->virtualMethod.
    //
    // Returns false if devirtualization is not possible.
    virtual bool resolveVirtualMethod(CORINFO_DEVIRTUALIZATION_INFO * info) = 0;

    // Get the unboxed entry point for a method, if possible.
    virtual CORINFO_METHOD_HANDLE getUnboxedEntry(
        CORINFO_METHOD_HANDLE ftn,
        bool*                 requiresInstMethodTableArg
        ) = 0;

    // Given T, return the type of the default Comparer<T>.
    // Returns null if the type can't be determined exactly.
    virtual CORINFO_CLASS_HANDLE getDefaultComparerClass(
            CORINFO_CLASS_HANDLE elemType
            ) = 0;

    // Given T, return the type of the default EqualityComparer<T>.
    // Returns null if the type can't be determined exactly.
    virtual CORINFO_CLASS_HANDLE getDefaultEqualityComparerClass(
            CORINFO_CLASS_HANDLE elemType
            ) = 0;

    // Given resolved token that corresponds to an intrinsic classified to
    // get a raw handle (NI_System_Activator_AllocatorOf etc.), fetch the
    // handle associated with the token. If this is not possible at
    // compile-time (because the current method's code is shared and the
    // token contains generic parameters) then indicate how the handle
    // should be looked up at runtime.
    virtual void expandRawHandleIntrinsic(
        CORINFO_RESOLVED_TOKEN *        pResolvedToken,
        CORINFO_GENERICHANDLE_RESULT *  pResult) = 0;

    // Is the given type in System.Private.Corelib and marked with IntrinsicAttribute?
    // This defaults to false.
    virtual bool isIntrinsicType(
            CORINFO_CLASS_HANDLE        classHnd
            ) { return false; }

    // return the entry point calling convention for any of the following
    // - a P/Invoke
    // - a method marked with UnmanagedCallersOnly
    // - a function pointer with the CORINFO_CALLCONV_UNMANAGED calling convention.
    virtual CorInfoCallConvExtension getUnmanagedCallConv(
            CORINFO_METHOD_HANDLE       method,
            CORINFO_SIG_INFO*           callSiteSig,
            bool*                       pSuppressGCTransition /* OUT */
            ) = 0;

    // return if any marshaling is required for PInvoke methods.  Note that
    // method == 0 => calli.  The call site sig is only needed for the varargs or calli case
    virtual bool pInvokeMarshalingRequired(
            CORINFO_METHOD_HANDLE       method,
            CORINFO_SIG_INFO*           callSiteSig
            ) = 0;

    // Check constraints on method type arguments (only).
    virtual bool satisfiesMethodConstraints(
            CORINFO_CLASS_HANDLE        parent, // the exact parent of the method
            CORINFO_METHOD_HANDLE       method
            ) = 0;

    // load and restore the method
    virtual void methodMustBeLoadedBeforeCodeIsRun(
            CORINFO_METHOD_HANDLE       method
            ) = 0;

    virtual CORINFO_METHOD_HANDLE mapMethodDeclToMethodImpl(
            CORINFO_METHOD_HANDLE       method
            ) = 0;

    // Returns the global cookie for the /GS unsafe buffer checks
    // The cookie might be a constant value (JIT), or a handle to memory location (Ngen)
    virtual void getGSCookie(
            GSCookie *  pCookieVal,                    // OUT
            GSCookie ** ppCookieVal                    // OUT
            ) = 0;

    // Provide patchpoint info for the method currently being jitted.
    virtual void setPatchpointInfo(
            PatchpointInfo* patchpointInfo
            ) = 0;

    // Get patchpoint info and il offset for the method currently being jitted.
    virtual PatchpointInfo* getOSRInfo(
            unsigned                       *ilOffset        // [OUT] il offset of OSR entry point
            ) = 0;

    /**********************************************************************************/
    //
    // ICorModuleInfo
    //
    /**********************************************************************************/

    // Resolve metadata token into runtime method handles. This function may not
    // return normally (e.g. it may throw) if it encounters invalid metadata or other
    // failures during token resolution.
    virtual void resolveToken(/* IN, OUT */ CORINFO_RESOLVED_TOKEN * pResolvedToken) = 0;

    // Signature information about the call sig
    virtual void findSig (
            CORINFO_MODULE_HANDLE       module,     /* IN */
            unsigned                    sigTOK,     /* IN */
            CORINFO_CONTEXT_HANDLE      context,    /* IN */
            CORINFO_SIG_INFO           *sig         /* OUT */
            ) = 0;

    // for Varargs, the signature at the call site may differ from
    // the signature at the definition.  Thus we need a way of
    // fetching the call site information
    virtual void findCallSiteSig (
            CORINFO_MODULE_HANDLE       module,     /* IN */
            unsigned                    methTOK,    /* IN */
            CORINFO_CONTEXT_HANDLE      context,    /* IN */
            CORINFO_SIG_INFO           *sig         /* OUT */
            ) = 0;

    virtual CORINFO_CLASS_HANDLE getTokenTypeAsHandle (
            CORINFO_RESOLVED_TOKEN *    pResolvedToken /* IN  */) = 0;

    // Returns (sub)string length and content (can be null for dynamic context)
    // for given metaTOK and module, length `-1` means input is incorrect
    virtual int getStringLiteral (
            CORINFO_MODULE_HANDLE       module,     /* IN  */
            unsigned                    metaTOK,    /* IN  */
            char16_t*                   buffer,     /* OUT */
            int                         bufferSize, /* IN  */
            int                         startIndex = 0 /* IN  */
            ) = 0;


    //------------------------------------------------------------------------------
    // printObjectDescription: Prints a (possibly truncated) textual UTF8 representation of the given
    //    object to a preallocated buffer. It's intended to be used only for debug/diagnostic
    //    purposes such as JitDisasm. The buffer is null-terminated (even if truncated).
    //
    // Arguments:
    //    handle     -          Direct object handle
    //    buffer     -          Pointer to buffer. Can be nullptr.
    //    bufferSize -          Buffer size (in bytes).
    //    pRequiredBufferSize - Full length of the textual UTF8 representation, in bytes.
    //                          Includes the null terminator, so the value is always at least 1,
    //                          where 1 indicates an empty string.
    //                          Can be used to call this API again with a bigger buffer to get the full
    //                          string.
    //
    // Return Value:
    //    Bytes written to the buffer, excluding the null terminator. The range is [0..bufferSize).
    //    If bufferSize is 0, returns 0.
    //
    // Remarks:
    //    buffer and bufferSize can be respectively nullptr and 0 to query just the required buffer size.
    //
    //    If the return value is less than bufferSize - 1 then the full string was written. In this case
    //    it is guaranteed that return value == *pRequiredBufferSize - 1.
    //
    virtual size_t printObjectDescription (
            CORINFO_OBJECT_HANDLE       handle,                       /* IN  */
            char*                       buffer,                       /* OUT */
            size_t                      bufferSize,                   /* IN  */
            size_t*                     pRequiredBufferSize = nullptr /* OUT */
            ) = 0;

    /**********************************************************************************/
    //
    // ICorClassInfo
    //
    /**********************************************************************************/

    // If the value class 'cls' is isomorphic to a primitive type it will
    // return that type, otherwise it will return CORINFO_TYPE_VALUECLASS
    virtual CorInfoType asCorInfoType (
            CORINFO_CLASS_HANDLE    cls
            ) = 0;

    // Return class name as in metadata, or nullptr if there is none.
    // Suitable for non-debugging use.
    virtual const char* getClassNameFromMetadata (
            CORINFO_CLASS_HANDLE    cls,
            const char            **namespaceName   /* OUT */
            ) = 0;

    // Return the type argument of the instantiated generic class,
    // which is specified by the index
    virtual CORINFO_CLASS_HANDLE getTypeInstantiationArgument(
            CORINFO_CLASS_HANDLE cls,
            unsigned             index
            ) = 0;

    // Prints the name for a specified class including namespaces and enclosing
    // classes.
    // See printObjectDescription for documentation for the parameters.
    virtual size_t printClassName(
            CORINFO_CLASS_HANDLE cls,                          /* IN  */
            char*                buffer,                       /* OUT */
            size_t               bufferSize,                   /* IN  */
            size_t*              pRequiredBufferSize = nullptr /* OUT */
            ) = 0;

    // Quick check whether the type is a value class. Returns the same value as getClassAttribs(cls) & CORINFO_FLG_VALUECLASS, except faster.
    virtual bool isValueClass(CORINFO_CLASS_HANDLE cls) = 0;

    // return flags (a bitfield of CorInfoFlags values)
    virtual uint32_t getClassAttribs (
            CORINFO_CLASS_HANDLE    cls
            ) = 0;

    virtual CORINFO_MODULE_HANDLE getClassModule (
            CORINFO_CLASS_HANDLE    cls
            ) = 0;

    // Returns the assembly that contains the module "mod".
    virtual CORINFO_ASSEMBLY_HANDLE getModuleAssembly (
            CORINFO_MODULE_HANDLE   mod
            ) = 0;

    // Returns the name of the assembly "assem".
    virtual const char* getAssemblyName (
            CORINFO_ASSEMBLY_HANDLE assem
            ) = 0;

    // Allocate and delete process-lifetime objects.  Should only be
    // referred to from static fields, lest a leak occur.
    // Note that "LongLifetimeFree" does not execute destructors, if "obj"
    // is an array of a struct type with a destructor.
    virtual void* LongLifetimeMalloc(size_t sz) = 0;
    virtual void LongLifetimeFree(void* obj) = 0;

    virtual size_t getClassModuleIdForStatics (
            CORINFO_CLASS_HANDLE    cls,
            CORINFO_MODULE_HANDLE * pModule,
            void **                 ppIndirection
            ) = 0;

    virtual bool getIsClassInitedFlagAddress(
            CORINFO_CLASS_HANDLE  cls,
            CORINFO_CONST_LOOKUP* addr,
            int*                  offset
            ) = 0;

    virtual bool getStaticBaseAddress(
            CORINFO_CLASS_HANDLE  cls,
            bool                  isGc,
            CORINFO_CONST_LOOKUP* addr
            ) = 0;

    // return the number of bytes needed by an instance of the class
    virtual unsigned getClassSize (
            CORINFO_CLASS_HANDLE    cls
            ) = 0;

    // return the number of bytes needed by an instance of the class allocated on the heap
    virtual unsigned getHeapClassSize(
            CORINFO_CLASS_HANDLE     cls
            ) = 0;

    virtual bool canAllocateOnStack(
            CORINFO_CLASS_HANDLE cls
            ) = 0;

    virtual unsigned getClassAlignmentRequirement (
            CORINFO_CLASS_HANDLE        cls,
            bool                        fDoubleAlignHint = false
            ) = 0;

    // This is only called for Value classes.  It returns a boolean array
    // in representing of 'cls' from a GC perspective.  The class is
    // assumed to be an array of machine words
    // (of length // getClassSize(cls) / TARGET_POINTER_SIZE),
    // 'gcPtrs' is a pointer to an array of uint8_ts of this length.
    // getClassGClayout fills in this array so that gcPtrs[i] is set
    // to one of the CorInfoGCType values which is the GC type of
    // the i-th machine word of an object of type 'cls'
    // returns the number of GC pointers in the array
    virtual unsigned getClassGClayout (
            CORINFO_CLASS_HANDLE        cls,        /* IN */
            uint8_t                    *gcPtrs      /* OUT */
            ) = 0;

    // returns the number of instance fields in a class
    virtual unsigned getClassNumInstanceFields (
            CORINFO_CLASS_HANDLE        cls        /* IN */
            ) = 0;

    virtual CORINFO_FIELD_HANDLE getFieldInClass(
            CORINFO_CLASS_HANDLE        clsHnd,
            int32_t                     num
            ) = 0;

    //------------------------------------------------------------------------------
    // getTypeLayout: Obtain a tree describing the layout of a type.
    //
    // Parameters:
    //   typeHnd            - Handle of the type.
    //   treeNodes          - [in, out] Pointer to tree node entries to write.
    //   numTreeNodes       - [in, out] Size of 'treeNodes' on entry. Updated to contain
    //                         the number of entries written in 'treeNodes'.
    //
    // Returns:
    //   A result indicating whether the type layout was successfully
    //   retrieved and whether the result is partial or not.
    //
    // Remarks:
    //   The type layout should be stored in preorder in 'treeNodes': the root
    //   node is always at index 0, and the first child of any node is at its
    //   own index + 1. The fields returned are NOT guaranteed to be ordered
    //   by offset.
    //
    //   SIMD and HW SIMD types are returned as a single entry without any
    //   children. For those, CORINFO_TYPE_LAYOUT_NODE::simdTypeHnd is set, but
    //   can only be used in a very restricted capacity, see
    //   CORINFO_TYPE_LAYOUT_NODE. Note that this special treatment is only for
    //   fields; if typeHnd itself is a SIMD type this function will treat it
    //   like a normal struct type and expand its fields.
    //
    //   IMPORTANT: except for GC pointers the fields returned to the JIT by
    //   this function should be considered as a hint only. The JIT CANNOT make
    //   assumptions in its codegen that the specified fields are actually part
    //   of the type when the code finally runs. This means the JIT should not
    //   make optimizations based on the field information returned by this
    //   function that would break if those fields were removed or shifted
    //   around.
    //
    virtual GetTypeLayoutResult getTypeLayout(
            CORINFO_CLASS_HANDLE typeHnd,
            CORINFO_TYPE_LAYOUT_NODE* treeNodes,
            size_t* numTreeNodes) = 0;

    virtual bool checkMethodModifier(
            CORINFO_METHOD_HANDLE       hMethod,
            const char *                modifier,
            bool                        fOptional
            ) = 0;

    //------------------------------------------------------------------------------
    // getNewHelper: Returns the allocation helper optimized for a specific class.
    //
    // Parameters:
    //   classHandle     - Handle of the type.
    //   pHasSideEffects - [out] Whether or not the allocation of the specified
    //                     type can have user-visible side effects; for example,
    //                     because a finalizer may run as a result.
    //
    // Returns:
    //   Helper to call to allocate the specified type.
    //
    virtual CorInfoHelpFunc getNewHelper(
            CORINFO_CLASS_HANDLE  classHandle,
            bool*                 pHasSideEffects
            ) = 0;

    // returns the newArr (1-Dim array) helper optimized for "arrayCls."
    virtual CorInfoHelpFunc getNewArrHelper(
            CORINFO_CLASS_HANDLE        arrayCls
            ) = 0;

    // returns the optimized "IsInstanceOf" or "ChkCast" helper
    virtual CorInfoHelpFunc getCastingHelper(
            CORINFO_RESOLVED_TOKEN *    pResolvedToken,
            bool                        fThrowing
            ) = 0;

    // returns helper to trigger static constructor
    virtual CorInfoHelpFunc getSharedCCtorHelper(
            CORINFO_CLASS_HANDLE        clsHnd
            ) = 0;

    // Boxing nullable<T> actually returns a boxed<T> not a boxed Nullable<T>.
    virtual CORINFO_CLASS_HANDLE getTypeForBox(
            CORINFO_CLASS_HANDLE        cls
            ) = 0;

    // returns the correct box helper for a particular class.  Note
    // that if this returns CORINFO_HELP_BOX, the JIT can assume
    // 'standard' boxing (allocate object and copy), and optimize
    virtual CorInfoHelpFunc getBoxHelper(
            CORINFO_CLASS_HANDLE        cls
            ) = 0;

    // returns the unbox helper.  If 'helperCopies' points to a true
    // value it means the JIT is requesting a helper that unboxes the
    // value into a particular location and thus has the signature
    //     void unboxHelper(void* dest, CORINFO_CLASS_HANDLE cls, Object* obj)
    // Otherwise (it is null or points at a FALSE value) it is requesting
    // a helper that returns a pointer to the unboxed data
    //     void* unboxHelper(CORINFO_CLASS_HANDLE cls, Object* obj)
    // The EE has the option of NOT returning the copy style helper
    // (But must be able to always honor the non-copy style helper)
    // The EE set 'helperCopies' on return to indicate what kind of
    // helper has been created.

    virtual CorInfoHelpFunc getUnBoxHelper(
            CORINFO_CLASS_HANDLE        cls
            ) = 0;

    virtual CORINFO_OBJECT_HANDLE getRuntimeTypePointer(
            CORINFO_CLASS_HANDLE        cls
            ) = 0;

    //------------------------------------------------------------------------------
    // isObjectImmutable: checks whether given object is known to be immutable or not
    //
    // Arguments:
    //    objPtr - Direct object handle
    //
    // Return Value:
    //    Returns true if object is known to be immutable
    //
    virtual bool isObjectImmutable(
            CORINFO_OBJECT_HANDLE       objPtr
            ) = 0;

    //------------------------------------------------------------------------------
    // getStringChar: returns char at the given index if the given object handle
    //    represents String and index is not out of bounds.
    //
    // Arguments:
    //    strObj - object handle
    //    index  - index of the char to return
    //    value  - output char
    //
    // Return Value:
    //    Returns true if value was successfully obtained
    //
    virtual bool getStringChar(
            CORINFO_OBJECT_HANDLE strObj,
            int                   index,
            uint16_t*             value
            ) = 0;

    //------------------------------------------------------------------------------
    // getObjectType: obtains type handle for given object
    //
    // Arguments:
    //    objPtr - Direct object handle
    //
    // Return Value:
    //    Returns CORINFO_CLASS_HANDLE handle that represents given object's type
    //
    virtual CORINFO_CLASS_HANDLE getObjectType(
            CORINFO_OBJECT_HANDLE       objPtr
            ) = 0;

    virtual bool getReadyToRunHelper(
            CORINFO_RESOLVED_TOKEN *        pResolvedToken,
            CORINFO_LOOKUP_KIND *           pGenericLookupKind,
            CorInfoHelpFunc                 id,
            CORINFO_CONST_LOOKUP *          pLookup
            ) = 0;

    virtual void getReadyToRunDelegateCtorHelper(
            CORINFO_RESOLVED_TOKEN *    pTargetMethod,
            mdToken                     targetConstraint,
            CORINFO_CLASS_HANDLE        delegateType,
            CORINFO_LOOKUP *            pLookup
            ) = 0;

    // This function tries to initialize the class (run the class constructor).
    // this function returns whether the JIT must insert helper calls before
    // accessing static field or method.
    //
    // See code:ICorClassInfo#ClassConstruction.
    virtual CorInfoInitClassResult initClass(
            CORINFO_FIELD_HANDLE    field,          // Non-NULL - inquire about cctor trigger before static field access
                                                    // NULL - inquire about cctor trigger in method prolog
            CORINFO_METHOD_HANDLE   method,         // Method referencing the field or prolog
                                                    // NULL - method being compiled
            CORINFO_CONTEXT_HANDLE  context         // Exact context of method
            ) = 0;

    // This used to be called "loadClass".  This records the fact
    // that the class must be loaded (including restored if necessary) before we execute the
    // code that we are currently generating.  When jitting code
    // the function loads the class immediately.  When zapping code
    // the zapper will if necessary use the call to record the fact that we have
    // to do a fixup/restore before running the method currently being generated.
    //
    // This is typically used to ensure value types are loaded before zapped
    // code that manipulates them is executed, so that the GC can access information
    // about those value types.
    virtual void classMustBeLoadedBeforeCodeIsRun(
            CORINFO_CLASS_HANDLE        cls
            ) = 0;

    // returns the class handle for the special builtin classes
    virtual CORINFO_CLASS_HANDLE getBuiltinClass (
            CorInfoClassId              classId
            ) = 0;

    // "System.Int32" ==> CORINFO_TYPE_INT..
    virtual CorInfoType getTypeForPrimitiveValueClass(
            CORINFO_CLASS_HANDLE        cls
            ) = 0;

    // "System.Int32" ==> CORINFO_TYPE_INT..
    // "System.UInt32" ==> CORINFO_TYPE_UINT..
    virtual CorInfoType getTypeForPrimitiveNumericClass(
            CORINFO_CLASS_HANDLE        cls
            ) = 0;

    // `true` if child is a subtype of parent
    // if parent is an interface, then does child implement / extend parent
    virtual bool canCast(
            CORINFO_CLASS_HANDLE        child,  // subtype (extends parent)
            CORINFO_CLASS_HANDLE        parent  // base type
            ) = 0;

    // See if a cast from fromClass to toClass will succeed, fail, or needs
    // to be resolved at runtime.
    virtual TypeCompareState compareTypesForCast(
            CORINFO_CLASS_HANDLE        fromClass,
            CORINFO_CLASS_HANDLE        toClass
            ) = 0;

    // See if types represented by cls1 and cls2 compare equal, not
    // equal, or the comparison needs to be resolved at runtime.
    virtual TypeCompareState compareTypesForEquality(
            CORINFO_CLASS_HANDLE        cls1,
            CORINFO_CLASS_HANDLE        cls2
            ) = 0;

    // Returns true if cls2 is known to be a more specific type
    // than cls1 (a subtype or more restrictive shared type)
    // for purposes of jit type tracking. This is a hint to the
    // jit for optimization; it does not have correctness
    // implications.
    virtual bool isMoreSpecificType(
            CORINFO_CLASS_HANDLE        cls1,
            CORINFO_CLASS_HANDLE        cls2
            ) = 0;

    // Returns true if a class handle can only describe values of exactly one type.
    virtual bool isExactType(
            CORINFO_CLASS_HANDLE        cls
            ) = 0;

    // Returns TypeCompareState::Must if cls is known to be an enum.
    // For enums with known exact type returns the underlying
    // type in underlyingType when the provided pointer is
    // non-NULL.
    // Returns TypeCompareState::May when a runtime check is required.
    virtual TypeCompareState isEnum(
            CORINFO_CLASS_HANDLE        cls,
            CORINFO_CLASS_HANDLE*       underlyingType
            ) = 0;

    // Given a class handle, returns the Parent type.
    // For COMObjectType, it returns Class Handle of System.Object.
    // Returns 0 if System.Object is passed in.
    virtual CORINFO_CLASS_HANDLE getParentType (
            CORINFO_CLASS_HANDLE        cls
            ) = 0;

    // Returns the CorInfoType of the "child type". If the child type is
    // not a primitive type, *clsRet will be set.
    // Given an Array of Type Foo, returns Foo.
    // Given BYREF Foo, returns Foo
    virtual CorInfoType getChildType (
            CORINFO_CLASS_HANDLE        clsHnd,
            CORINFO_CLASS_HANDLE*       clsRet
            ) = 0;

    // Check if this is a single dimensional array type
    virtual bool isSDArray(
            CORINFO_CLASS_HANDLE        cls
            ) = 0;

    // Get the number of dimensions in an array
    virtual unsigned getArrayRank(
            CORINFO_CLASS_HANDLE        cls
            ) = 0;

    // Get the index of runtime provided array method
    virtual CorInfoArrayIntrinsic getArrayIntrinsicID(
            CORINFO_METHOD_HANDLE       ftn
            ) = 0;

    // Get static field data for an array
    virtual void * getArrayInitializationData(
            CORINFO_FIELD_HANDLE        field,
            uint32_t                    size
            ) = 0;

    // Check Visibility rules.
    virtual CorInfoIsAccessAllowedResult canAccessClass(
            CORINFO_RESOLVED_TOKEN * pResolvedToken,
            CORINFO_METHOD_HANDLE    callerHandle,
            CORINFO_HELPER_DESC *    pAccessHelper /* If canAccessMethod returns something other
                                                      than ALLOWED, then this is filled in. */
            ) = 0;

    /**********************************************************************************/
    //
    // ICorFieldInfo
    //
    /**********************************************************************************/

    // Prints the name of a field into a buffer. See printObjectDescription for more documentation.
    virtual size_t printFieldName(
                        CORINFO_FIELD_HANDLE field,
                        char* buffer,
                        size_t bufferSize,
                        size_t* pRequiredBufferSize = nullptr
                        ) = 0;

    // return class it belongs to
    virtual CORINFO_CLASS_HANDLE getFieldClass (
                        CORINFO_FIELD_HANDLE    field
                        ) = 0;

    // Return the field's type, if it is CORINFO_TYPE_VALUECLASS 'structType' is set
    // the field's value class (if 'structType' == 0, then don't bother
    // the structure info).
    //
    // 'memberParent' is typically only set when verifying.  It should be the
    // result of calling getMemberParent.
    virtual CorInfoType getFieldType(
            CORINFO_FIELD_HANDLE        field,
            CORINFO_CLASS_HANDLE *      structType = NULL,
            CORINFO_CLASS_HANDLE        memberParent = NULL /* IN */
            ) = 0;

    // return the data member's instance offset
    virtual unsigned getFieldOffset(
            CORINFO_FIELD_HANDLE        field
            ) = 0;

    virtual void getFieldInfo(
            CORINFO_RESOLVED_TOKEN *    pResolvedToken,
            CORINFO_METHOD_HANDLE       callerHandle,
            CORINFO_ACCESS_FLAGS        flags,
            CORINFO_FIELD_INFO *        pResult
            ) = 0;

    // Returns the index against which the field's thread static block in stored in TLS.
    virtual uint32_t getThreadLocalFieldInfo (
            CORINFO_FIELD_HANDLE        field,
            bool                        isGCType
            ) = 0;

    // Returns the thread static block information like offsets, etc. from current TLS.
    virtual void getThreadLocalStaticBlocksInfo (
            CORINFO_THREAD_STATIC_BLOCKS_INFO*  pInfo,
            bool                                isGCType
            ) = 0;

    virtual void getThreadLocalStaticInfo_NativeAOT(
            CORINFO_THREAD_STATIC_INFO_NATIVEAOT* pInfo
            ) = 0;

    // Returns true iff "fldHnd" represents a static field.
    virtual bool isFieldStatic(CORINFO_FIELD_HANDLE fldHnd) = 0;

    // Returns Length of an Array or of a String object, otherwise -1.
    // objHnd must not be null.
    virtual int getArrayOrStringLength(CORINFO_OBJECT_HANDLE objHnd) = 0;

    /*********************************************************************************/
    //
    // ICorDebugInfo
    //
    /*********************************************************************************/

    // Query the EE to find out where interesting break points
    // in the code are.  The native compiler will ensure that these places
    // have a corresponding break point in native code.
    //
    // Note that unless CORJIT_FLAG_DEBUG_CODE is specified, this function will
    // be used only as a hint and the native compiler should not change its
    // code generation.
    virtual void getBoundaries(
            CORINFO_METHOD_HANDLE   ftn,                // [IN] method of interest
            unsigned int           *cILOffsets,         // [OUT] size of pILOffsets
            uint32_t              **pILOffsets,         // [OUT] IL offsets of interest
                                                        //       jit MUST free with freeArray!
            ICorDebugInfo::BoundaryTypes *implicitBoundaries // [OUT] tell jit, all boundaries of this type
            ) = 0;

    // Report back the mapping from IL to native code,
    // this map should include all boundaries that 'getBoundaries'
    // reported as interesting to the debugger.

    // Note that debugger (and profiler) is assuming that all of the
    // offsets form a contiguous block of memory, and that the
    // OffsetMapping is sorted in order of increasing native offset.
    virtual void setBoundaries(
            CORINFO_METHOD_HANDLE         ftn,      // [IN] method of interest
            uint32_t                      cMap,     // [IN] size of pMap
            ICorDebugInfo::OffsetMapping *pMap      // [IN] map including all points of interest.
                                                    //      jit allocated with allocateArray, EE frees
            ) = 0;

    // Query the EE to find out the scope of local variables.
    // normally the JIT would trash variables after last use, but
    // under debugging, the JIT needs to keep them live over their
    // entire scope so that they can be inspected.
    //
    // Note that unless CORJIT_FLAG_DEBUG_CODE is specified, this function will
    // be used only as a hint and the native compiler should not change its
    // code generation.
    virtual void getVars(
            CORINFO_METHOD_HANDLE           ftn,            // [IN]  method of interest
            uint32_t                       *cVars,          // [OUT] size of 'vars'
            ICorDebugInfo::ILVarInfo      **vars,           // [OUT] scopes of variables of interest
                                                            //       jit MUST free with freeArray!
            bool                           *extendOthers    // [OUT] if `true`, then assume the scope
                                                            //       of unmentioned vars is entire method
            ) = 0;

    // Report back to the EE the location of every variable.
    // note that the JIT might split lifetimes into different
    // locations etc.
    virtual void setVars(
            CORINFO_METHOD_HANDLE           ftn,            // [IN] method of interest
            uint32_t                        cVars,          // [IN] size of 'vars'
            ICorDebugInfo::NativeVarInfo   *vars            // [IN] map telling where local vars are stored at what points
                                                            //      jit allocated with allocateArray, EE frees
            ) = 0;

    // Report inline tree and rich offset mappings to EE.
    // The arrays are expected to be allocated with allocateArray
    // and ownership is transferred to the EE with this call.
    virtual void reportRichMappings(
            ICorDebugInfo::InlineTreeNode*    inlineTreeNodes,    // [IN] Nodes of the inline tree
            uint32_t                          numInlineTreeNodes, // [IN] Number of nodes in the inline tree
            ICorDebugInfo::RichOffsetMapping* mappings,           // [IN] Rich mappings
            uint32_t                          numMappings         // [IN] Number of rich mappings
            ) = 0;

    /*-------------------------- Misc ---------------------------------------*/

    // Used to allocate memory that needs to handed to the EE.
    // For eg, use this to allocated memory for reporting debug info,
    // which will be handed to the EE by setVars() and setBoundaries()
    virtual void * allocateArray(
            size_t              cBytes
            ) = 0;

    // JitCompiler will free arrays passed by the EE using this
    // For eg, The EE returns memory in getVars() and getBoundaries()
    // to the JitCompiler, which the JitCompiler should release using
    // freeArray()
    virtual void freeArray(
            void *              array
            ) = 0;

    /*********************************************************************************/
    //
    // ICorArgInfo
    //
    /*********************************************************************************/

    // advance the pointer to the argument list.
    // a ptr of 0, is special and always means the first argument
    virtual CORINFO_ARG_LIST_HANDLE getArgNext (
            CORINFO_ARG_LIST_HANDLE     args            /* IN */
            ) = 0;

    // Get the type of a particular argument
    // CORINFO_TYPE_UNDEF is returned when there are no more arguments
    // If the type returned is a primitive type (or an enum) *vcTypeRet set to NULL
    // otherwise it is set to the TypeHandle associted with the type
    // Enumerations will always look their underlying type (probably should fix this)
    // Otherwise vcTypeRet is the type as would be seen by the IL,
    // The return value is the type that is used for calling convention purposes
    // (Thus if the EE wants a value class to be passed like an int, then it will
    // return CORINFO_TYPE_INT
    virtual CorInfoTypeWithMod getArgType (
            CORINFO_SIG_INFO*           sig,            /* IN */
            CORINFO_ARG_LIST_HANDLE     args,           /* IN */
            CORINFO_CLASS_HANDLE*       vcTypeRet       /* OUT */
            ) = 0;

    // Obtains a list of exact classes for a given base type. Returns 0 if the number of
    // the exact classes is greater than maxExactClasses or if more types might be loaded
    // in future.
    virtual int getExactClasses(
            CORINFO_CLASS_HANDLE        baseType,            /* IN */
            int                         maxExactClasses,     /* IN */
            CORINFO_CLASS_HANDLE*       exactClsRet          /* OUT */
            ) = 0;

    // If the Arg is a CORINFO_TYPE_CLASS fetch the class handle associated with it
    virtual CORINFO_CLASS_HANDLE getArgClass (
            CORINFO_SIG_INFO*           sig,            /* IN */
            CORINFO_ARG_LIST_HANDLE     args            /* IN */
            ) = 0;

    // Returns type of HFA for valuetype
    virtual CorInfoHFAElemType getHFAType (
            CORINFO_CLASS_HANDLE        hClass
            ) = 0;

    // Runs the given function under an error trap. This allows the JIT to make calls
    // to interface functions that may throw exceptions without needing to be aware of
    // the EH ABI, exception types, etc. Returns true if the given function completed
    // successfully and false otherwise.
    typedef void (*errorTrapFunction)(void*);
    virtual bool runWithErrorTrap(
            errorTrapFunction           function, // The function to run
            void*                       parameter // The context parameter that will be passed to the function and the handler
            ) = 0;

    // Runs the given function under an error trap. This allows the JIT to make calls
    // to interface functions that may throw exceptions without needing to be aware of
    // the EH ABI, exception types, etc. Returns true if the given function completed
    // successfully and false otherwise. This error trap checks for SuperPMI exceptions
    virtual bool runWithSPMIErrorTrap(
            errorTrapFunction           function, // The function to run
            void*                       parameter // The context parameter that will be passed to the function and the handler
            ) = 0;

/*****************************************************************************
 * ICorStaticInfo contains EE interface methods which return values that are
 * constant from invocation to invocation.  Thus they may be embedded in
 * persisted information like statically generated code. (This is of course
 * assuming that all code versions are identical each time.)
 *****************************************************************************/

    // Return details about EE internal data structures
    virtual void getEEInfo(
            CORINFO_EE_INFO            *pEEInfoOut
            ) = 0;

    // Returns name of the JIT timer log
    virtual const char16_t *getJitTimeLogFilename() = 0;

    /*********************************************************************************/
    //
    // Diagnostic methods
    //
    /*********************************************************************************/

    // this function is for debugging only. Returns method token.
    // Returns mdMethodDefNil for dynamic methods.
    virtual mdMethodDef getMethodDefFromMethod(
            CORINFO_METHOD_HANDLE hMethod
            ) = 0;

    // This is similar to getMethodNameFromMetadata except that it also returns
    // reasonable names for functions without metadata.
    // See printObjectDescription for documentation of parameters.
    virtual size_t printMethodName(
            CORINFO_METHOD_HANDLE ftn,
            char*                 buffer,
            size_t                bufferSize,
            size_t*               pRequiredBufferSize = nullptr
            ) = 0;

    // Return method name as in metadata, or nullptr if there is none,
    // and optionally return the class, enclosing class, and namespace names
    // as in metadata.
    // Suitable for non-debugging use.
    virtual const char* getMethodNameFromMetadata(
            CORINFO_METHOD_HANDLE       ftn,                  /* IN */
            const char                **className,            /* OUT */
            const char                **namespaceName,        /* OUT */
            const char                **enclosingClassName    /* OUT */
            ) = 0;

    // this function is for debugging only.  It returns a value that
    // is will always be the same for a given method.  It is used
    // to implement the 'jitRange' functionality
    virtual unsigned getMethodHash (
            CORINFO_METHOD_HANDLE       ftn         /* IN */
            ) = 0;

    // returns whether the struct is enregisterable. Only valid on a System V VM. Returns true on success, false on failure.
    virtual bool getSystemVAmd64PassStructInRegisterDescriptor(
            CORINFO_CLASS_HANDLE                                    structHnd,              /* IN */
            SYSTEMV_AMD64_CORINFO_STRUCT_REG_PASSING_DESCRIPTOR*    structPassInRegDescPtr  /* OUT */
            ) = 0;

    virtual uint32_t getLoongArch64PassStructInRegisterFlags(CORINFO_CLASS_HANDLE cls) = 0;
    virtual uint32_t getRISCV64PassStructInRegisterFlags(CORINFO_CLASS_HANDLE cls) = 0;
};

/*****************************************************************************
 * ICorDynamicInfo contains EE interface methods which return values that may
 * change from invocation to invocation.  They cannot be embedded in persisted
 * data; they must be requeried each time the EE is run.
 *****************************************************************************/

class ICorDynamicInfo : public ICorStaticInfo
{
public:

    //
    // These methods return values to the JIT which are not constant
    // from session to session.
    //
    // These methods take an extra parameter : void **ppIndirection.
    // If a JIT supports generation of prejit code (install-o-jit), it
    // must pass a non-null value for this parameter, and check the
    // resulting value.  If *ppIndirection is NULL, code should be
    // generated normally.  If non-null, then the value of
    // *ppIndirection is an address in the cookie table, and the code
    // generator needs to generate an indirection through the table to
    // get the resulting value.  In this case, the return result of the
    // function must NOT be directly embedded in the generated code.
    //
    // Note that if a JIT does not support prejit code generation, it
    // may ignore the extra parameter & pass the default of NULL - the
    // prejit ICorDynamicInfo implementation will see this & generate
    // an error if the jitter is used in a prejit scenario.
    //

    // Return details about EE internal data structures

    virtual uint32_t getThreadTLSIndex(
            void                  **ppIndirection = NULL
            ) = 0;

    virtual int32_t * getAddrOfCaptureThreadGlobal(
            void                  **ppIndirection = NULL
            ) = 0;

    // return the native entry point to an EE helper (see CorInfoHelpFunc)
    virtual void* getHelperFtn (
            CorInfoHelpFunc         ftnNum,
            void                  **ppIndirection = NULL
            ) = 0;

    // return a callable address of the function (native code). This function
    // may return a different value (depending on whether the method has
    // been JITed or not.
    virtual void getFunctionEntryPoint(
            CORINFO_METHOD_HANDLE   ftn,                 /* IN  */
            CORINFO_CONST_LOOKUP *  pResult,             /* OUT */
            CORINFO_ACCESS_FLAGS    accessFlags = CORINFO_ACCESS_ANY
            ) = 0;

    // return a directly callable address. This can be used similarly to the
    // value returned by getFunctionEntryPoint() except that it is
    // guaranteed to be multi callable entrypoint.
    virtual void getFunctionFixedEntryPoint(
            CORINFO_METHOD_HANDLE   ftn,
            bool                    isUnsafeFunctionPointer,
            CORINFO_CONST_LOOKUP *  pResult
            ) = 0;

    // get the synchronization handle that is passed to monXstatic function
    virtual void* getMethodSync(
            CORINFO_METHOD_HANDLE   ftn,
            void**                  ppIndirection = NULL
            ) = 0;

    // get slow lazy string literal helper to use (CORINFO_HELP_STRCNS*).
    // Returns CORINFO_HELP_UNDEF if lazy string literal helper cannot be used.
    virtual CorInfoHelpFunc getLazyStringLiteralHelper(
            CORINFO_MODULE_HANDLE   handle
            ) = 0;

    virtual CORINFO_MODULE_HANDLE embedModuleHandle(
            CORINFO_MODULE_HANDLE   handle,
            void                  **ppIndirection = NULL
            ) = 0;

    virtual CORINFO_CLASS_HANDLE embedClassHandle(
            CORINFO_CLASS_HANDLE    handle,
            void                  **ppIndirection = NULL
            ) = 0;

    virtual CORINFO_METHOD_HANDLE embedMethodHandle(
            CORINFO_METHOD_HANDLE   handle,
            void                  **ppIndirection = NULL
            ) = 0;

    virtual CORINFO_FIELD_HANDLE embedFieldHandle(
            CORINFO_FIELD_HANDLE    handle,
            void                  **ppIndirection = NULL
            ) = 0;

    // Given a module scope (module), a method handle (context) and
    // a metadata token (metaTOK), fetch the handle
    // (type, field or method) associated with the token.
    // If this is not possible at compile-time (because the current method's
    // code is shared and the token contains generic parameters)
    // then indicate how the handle should be looked up at run-time.
    //
    virtual void embedGenericHandle(
            CORINFO_RESOLVED_TOKEN *        pResolvedToken,
            bool                            fEmbedParent, // `true` - embeds parent type handle of the field/method handle
            CORINFO_GENERICHANDLE_RESULT *  pResult
            ) = 0;

    // Return information used to locate the exact enclosing type of the current method.
    // Used only to invoke .cctor method from code shared across generic instantiations
    //   !needsRuntimeLookup       statically known (enclosing type of method itself)
    //   needsRuntimeLookup:
    //      CORINFO_LOOKUP_THISOBJ     use vtable pointer of 'this' param
    //      CORINFO_LOOKUP_CLASSPARAM  use vtable hidden param
    //      CORINFO_LOOKUP_METHODPARAM use enclosing type of method-desc hidden param
    virtual void getLocationOfThisType(
            CORINFO_METHOD_HANDLE   context,
            CORINFO_LOOKUP_KIND*    pLookupKind
            ) = 0;

    // return the address of the PInvoke target. May be a fixup area in the
    // case of late-bound PInvoke calls.
    virtual void getAddressOfPInvokeTarget(
            CORINFO_METHOD_HANDLE   method,
            CORINFO_CONST_LOOKUP *  pLookup
            ) = 0;

    // Generate a cookie based on the signature that would needs to be passed
    // to CORINFO_HELP_PINVOKE_CALLI
    virtual void* GetCookieForPInvokeCalliSig(
            CORINFO_SIG_INFO*   szMetaSig,
            void**              ppIndirection = NULL
            ) = 0;

    // returns true if a VM cookie can be generated for it (might be false due to cross-module
    // inlining, in which case the inlining should be aborted)
    virtual bool canGetCookieForPInvokeCalliSig(
            CORINFO_SIG_INFO*   szMetaSig
            ) = 0;

    // Gets a handle that is checked to see if the current method is
    // included in "JustMyCode"
    virtual CORINFO_JUST_MY_CODE_HANDLE getJustMyCodeHandle(
            CORINFO_METHOD_HANDLE           method,
            CORINFO_JUST_MY_CODE_HANDLE**   ppIndirection = NULL
            ) = 0;

    // Gets a method handle that can be used to correlate profiling data.
    // This is the IP of a native method, or the address of the descriptor struct
    // for IL.  Always guaranteed to be unique per process, and not to move. */
    virtual void GetProfilingHandle(
            bool                      *pbHookFunction,
            void                     **pProfilerHandle,
            bool                      *pbIndirectedHandles
            ) = 0;

    // Returns instructions on how to make the call. See code:CORINFO_CALL_INFO for possible return values.
    virtual void getCallInfo(
            // Token info (in)
            CORINFO_RESOLVED_TOKEN * pResolvedToken,

            // Generics info (in)
            CORINFO_RESOLVED_TOKEN * pConstrainedResolvedToken,

            // Security info (in)
            CORINFO_METHOD_HANDLE   callerHandle,

            // Jit info (in)
            CORINFO_CALLINFO_FLAGS  flags,

            // out params
            CORINFO_CALL_INFO       *pResult
            ) = 0;

    // Returns the class's domain ID for accessing shared statics
    virtual unsigned getClassDomainID (
            CORINFO_CLASS_HANDLE    cls,
            void                  **ppIndirection = NULL
            ) = 0;

    //------------------------------------------------------------------------------
    // getStaticFieldContent: returns true and the actual field's value if the given
    //    field represents a statically initialized readonly field of any type.
    //
    // Arguments:
    //    field                - field handle
    //    buffer               - buffer field's value will be stored to
    //    bufferSize           - size of buffer
    //    ignoreMovableObjects - ignore movable reference types or not
    //
    // Return Value:
    //    Returns true if field's constant value was available and successfully copied to buffer
    //
    virtual bool getStaticFieldContent(
            CORINFO_FIELD_HANDLE    field,
            uint8_t                *buffer,
            int                     bufferSize,
            int                     valueOffset = 0,
            bool                    ignoreMovableObjects = true
            ) = 0;

    virtual bool getObjectContent(
            CORINFO_OBJECT_HANDLE   obj,
            uint8_t*                buffer,
            int                     bufferSize,
            int                     valueOffset
            ) = 0;

    // If pIsSpeculative is NULL, return the class handle for the value of ref-class typed
    // static readonly fields, if there is a unique location for the static and the class
    // is already initialized.
    //
    // If pIsSpeculative is not NULL, fetch the class handle for the value of all ref-class
    // typed static fields, if there is a unique location for the static and the field is
    // not null.
    //
    // Set *pIsSpeculative true if this type may change over time (field is not readonly or
    // is readonly but class has not yet finished initialization). Set *pIsSpeculative false
    // if this type will not change.
    virtual CORINFO_CLASS_HANDLE getStaticFieldCurrentClass(
            CORINFO_FIELD_HANDLE    field,
            bool                   *pIsSpeculative = NULL
            ) = 0;

    // registers a vararg sig & returns a VM cookie for it (which can contain other stuff)
    virtual CORINFO_VARARGS_HANDLE getVarArgsHandle(
            CORINFO_SIG_INFO       *pSig,
            void                  **ppIndirection = NULL
            ) = 0;

    // returns true if a VM cookie can be generated for it (might be false due to cross-module
    // inlining, in which case the inlining should be aborted)
    virtual bool canGetVarArgsHandle(
            CORINFO_SIG_INFO       *pSig
            ) = 0;

    // Allocate a string literal on the heap and return a handle to it
    virtual InfoAccessType constructStringLiteral(
            CORINFO_MODULE_HANDLE   module,
            mdToken                 metaTok,
            void                  **ppValue
            ) = 0;

    virtual InfoAccessType emptyStringLiteral(
            void                  **ppValue
            ) = 0;

    // (static fields only) given that 'field' refers to thread local store,
    // return the ID (TLS index), which is used to find the beginning of the
    // TLS data area for the particular DLL 'field' is associated with.
    virtual uint32_t getFieldThreadLocalStoreID (
            CORINFO_FIELD_HANDLE    field,
            void                  **ppIndirection = NULL
            ) = 0;

    virtual CORINFO_METHOD_HANDLE GetDelegateCtor(
            CORINFO_METHOD_HANDLE  methHnd,
            CORINFO_CLASS_HANDLE   clsHnd,
            CORINFO_METHOD_HANDLE  targetMethodHnd,
            DelegateCtorArgs *     pCtorData
            ) = 0;

    virtual void MethodCompileComplete(
            CORINFO_METHOD_HANDLE methHnd
            ) = 0;

    // Obtain tailcall help for the specified call site.
    virtual bool getTailCallHelpers(
            // The resolved token for the call. Can be null for calli.
            CORINFO_RESOLVED_TOKEN* callToken,

            // The signature at the callsite.
            CORINFO_SIG_INFO* sig,

            // Flags for the tailcall site.
            CORINFO_GET_TAILCALL_HELPERS_FLAGS flags,

            // The resulting help.
            CORINFO_TAILCALL_HELPERS* pResult
            ) = 0;

    // Optionally, convert calli to regular method call. This is for PInvoke argument marshalling.
    virtual bool convertPInvokeCalliToCall(
            CORINFO_RESOLVED_TOKEN *    pResolvedToken,
            bool                        fMustConvert
            ) = 0;

    // Notify EE about intent to use or not to use instruction set in the method. Returns true if the instruction set is supported unconditionally.
    virtual bool notifyInstructionSetUsage(
            CORINFO_InstructionSet      instructionSet,
            bool                        supportEnabled
            ) = 0;

    // Notify EE that JIT needs an entry-point that is tail-callable.
    // This is used for AOT on x64 to support delay loaded fast tailcalls.
    // Normally the indirection cell is retrieved from the return address,
    // but for tailcalls, the contract is that JIT leaves the indirection cell in
    // a register during tailcall.
    virtual void updateEntryPointForTailCall(CORINFO_CONST_LOOKUP* entryPoint) = 0;
};

/**********************************************************************************/

// It would be nicer to use existing IMAGE_REL_XXX constants instead of defining our own here...
#define IMAGE_REL_BASED_REL32           0x10
#define IMAGE_REL_BASED_THUMB_BRANCH24  0x13
#define IMAGE_REL_SECREL                0x104

// The identifier for ARM32-specific PC-relative address
// computation corresponds to the following instruction
// sequence:
//  l0: movw rX, #imm_lo  // 4 byte
//  l4: movt rX, #imm_hi  // 4 byte
//  l8: add  rX, pc <- after this instruction rX = relocTarget
//
// Program counter at l8 is address of l8 + 4
// Address of relocated movw/movt is l0
// So, imm should be calculated as the following:
//  imm = relocTarget - (l8 + 4) = relocTarget - (l0 + 8 + 4) = relocTarget - (l_0 + 12)
// So, the value of offset correction is 12
//
#define IMAGE_REL_BASED_REL_THUMB_MOV32_PCREL   0x14

/**********************************************************************************/
#ifdef TARGET_64BIT
#define USE_PER_FRAME_PINVOKE_INIT
#endif

#endif // _COR_INFO_H_
